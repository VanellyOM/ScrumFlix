/*
 * File:      /ScrumFlix/Areas/Admin/Controllers/AdminBackupController.cs
 * Namespace: ScrumFlix.Areas.Admin.Controllers
 * Purpose:   Admin controller for the Database Backup module.
 *
 *            GET  Backup         — renders the backup page with table checklist.
 *            POST TriggerBackup  — runs IDatabaseBackupService.GenerateAsync with
 *                                  real-time progress reporting (Phase 4.0 shared
 *                                  progress framework), writes an AuditLog entry,
 *                                  optionally emails the .zip, stages the bytes in
 *                                  IMemoryCache, and returns { operationId, fileName }.
 *            GET  DownloadBackup — streams the staged .zip for a completed
 *                                  TriggerBackup operation and evicts it from cache.
 *
 * Access: Admin only (RoleId == 1) via RoleGuard(1).
 *
 * Design notes (Phase 4.2):
 *   - Two-phase flow: TriggerBackup generates the archive while broadcasting
 *     per-table/per-section ProgressUpdate events via ProgressHub, then stages
 *     the bytes server-side under the operation id (IMemoryCache, 5-minute TTL).
 *     DownloadBackup streams those bytes on a separate GET once the client's
 *     progress tracker reports completion. This replaces the previous
 *     synchronous File() response, which gave no feedback during generation.
 *   - Email delivery remains fire-and-forget (no await on the send task) so a
 *     misconfigured SMTP server never blocks the response.
 */

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using ScrumFlix.Infrastructure;
using ScrumFlix.Services.BackgroundQueue;
using ScrumFlix.Services.Progress;

namespace ScrumFlix.Areas.Admin.Controllers;

[Area("Admin")]
public class AdminBackupController : StaffControllerBase
{
    private readonly IDatabaseBackupService          _backup;
    private readonly IAuditService                   _audit;
    private readonly IEmailService                   _email;
    private readonly IConfiguration                  _config;
    private readonly ILogger<AdminBackupController>  _logger;
    private readonly IProgressReporterFactory        _reporterFactory;
    private readonly IMemoryCache                    _cache;
    private readonly IBackgroundTaskQueue            _taskQueue;

    /// <summary>
    /// How long a generated backup .zip stays in <see cref="_cache"/> after
    /// generation, waiting for the client's DownloadBackup GET. Generous
    /// enough to cover a slow client-side download click after the spinner
    /// shows "Complete!", short enough to avoid pinning large .zip byte
    /// arrays in memory on Somee.com shared hosting.
    /// </summary>
    private static readonly TimeSpan BackupCacheTtl = TimeSpan.FromMinutes(5);

    /// <summary>Cache key prefix for staged backup downloads. Combined with the operation id.</summary>
    private const string BackupCacheKeyPrefix = "backup-download:";

    public AdminBackupController(
        IDatabaseBackupService         backup,
        IAuditService                  audit,
        IEmailService                  email,
        IConfiguration                 config,
        ILogger<AdminBackupController> logger,
        IProgressReporterFactory       reporterFactory,
        IMemoryCache                   cache,
        IBackgroundTaskQueue           taskQueue)
    {
        _backup = backup;
        _audit  = audit;
        _email  = email;
        _config = config;
        _logger = logger;
        _reporterFactory = reporterFactory;
        _cache  = cache;
        _taskQueue = taskQueue;
    }

    // ── GET: Backup ────────────────────────────────────────────────────────

    /// <summary>
    /// Renders the database backup page with a table-selection checklist and
    /// any status messages from a previous backup run.
    /// </summary>
    public IActionResult Backup()
    {
        if (RoleGuard(1) is { } r) return r;

        var vm = new BackupViewModel
        {
            AvailableTables     = _backup.GetAvailableTables(),
            // Pre-select all non-excluded tables
            SelectedTableKeys   = _backup.GetAvailableTables()
                                         .Where(t => !t.ExcludedByDefault)
                                         .Select(t => t.Key)
                                         .ToList(),
        };

        return View(vm);
    }

    // ── POST: TriggerBackup ────────────────────────────────────────────────

    /// <summary>
    /// Generates the backup .zip while reporting per-table/per-section progress
    /// via the Phase 4.0 shared progress framework (ProgressHub), stashes the
    /// resulting bytes in <see cref="_cache"/> under the operation id, writes
    /// an AuditLog entry, optionally emails the .zip, and returns a small JSON
    /// payload describing where the client can download the finished archive.
    ///
    /// Two-phase flow (see Phase 4.2 plan):
    ///   1. POST TriggerBackup — generates the .zip, reports progress, caches
    ///      the bytes, returns { operationId, fileName }.
    ///   2. GET  DownloadBackup(operationId) — streams the cached .zip and
    ///      evicts it from the cache.
    ///
    /// The client (sf-backup.js) generates <paramref name="vm"/>.OperationId
    /// and joins the corresponding ProgressHub group BEFORE submitting this
    /// request via fetch, so progress updates are visible for the full
    /// duration of generation — including on Somee.com, where the previous
    /// synchronous File() response gave no feedback until the download
    /// finished.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TriggerBackup(BackupViewModel vm)
    {
        if (RoleGuard(1) is { } r) return r;

        var userId   = CurrentUserId ?? 0;
        var userName = CurrentUserName ?? $"User #{userId}";

        // Re-populate AvailableTables (not posted back, needs to be rebuilt)
        vm.AvailableTables = _backup.GetAvailableTables();

        // Use all non-excluded tables if nothing was checked
        var selectedKeys = (vm.SelectedTableKeys is { Count: > 0 })
            ? vm.SelectedTableKeys
            : vm.AvailableTables.Where(t => !t.ExcludedByDefault).Select(t => t.Key).ToList();

        // Build the capture options from the form toggles.
        var options = new DatabaseBackupOptions
        {
            IncludeSchema           = vm.IncludeSchema,
            IncludeData             = vm.IncludeData,
            IncludeStoredProcedures = vm.IncludeStoredProcedures,
            IncludeViews            = vm.IncludeViews,
            IncludeTriggers         = vm.IncludeTriggers,
            DropBeforeCreate        = vm.DropBeforeCreate,
            SelectedTableKeys       = selectedKeys,
        };

        // Guard: at least one section must be selected.
        if (!options.HasAnySection)
        {
            return BadRequest(new
            {
                error = "Select at least one section to back up (schema, data, routines, views, or triggers)."
            });
        }

        _logger.LogInformation(
            "Admin {User} triggered database backup — {TableCount} tables; " +
            "schema={Schema}, data={Data}, routines={Routines}, views={Views}, triggers={Triggers}.",
            userName, selectedKeys.Count,
            options.IncludeSchema, options.IncludeData, options.IncludeStoredProcedures,
            options.IncludeViews, options.IncludeTriggers);

        // ── Mint reporter + enqueue (Phase 4.3 background-queue path) ─────────
        // The reporter is created with a NON-request-bound token: the HTTP
        // request returns before the work runs, so binding to
        // HttpContext.RequestAborted would cancel the backup the instant the
        // response is sent. User cancellation flows via ProgressHub.ClientCancel
        // → IProgressReporterFactory.Cancel → reporter.CancellationToken.
        var reporter = string.IsNullOrWhiteSpace(vm.OperationId)
            ? _reporterFactory.Create("Database Backup")
            : _reporterFactory.Create(vm.OperationId, "Database Backup");

        // Capture primitives the background closure needs — no HttpContext/session
        // is available once the work is dequeued.
        var sendEmail        = vm.SendEmail;
        var cacheKey         = BackupCacheKeyPrefix + reporter.OperationId;
        var cacheTtl         = BackupCacheTtl;
        var reporterFactory  = _reporterFactory;  // singleton — safe to use after this request ends

        await _taskQueue.QueueBackgroundWorkItemAsync(async (sp, _) =>
        {
            // Resolve services from the per-item DI scope. IMemoryCache is a
            // singleton, but it is still resolved through the scope provider for
            // consistency — a scope resolves singletons from the root container,
            // so the same shared cache instance is returned either way.
            var backup = sp.GetRequiredService<IDatabaseBackupService>();
            var audit  = sp.GetRequiredService<IAuditService>();
            var email  = sp.GetRequiredService<IEmailService>();
            var config = sp.GetRequiredService<IConfiguration>();
            var cache  = sp.GetRequiredService<IMemoryCache>();
            var logger = sp.GetRequiredService<ILogger<AdminBackupController>>();

            BackupResult result;
            try
            {
                result = await backup.GenerateAsync(options, reporter, reporter.CancellationToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation(
                    "Database backup cancelled by Admin {User} (operationId={OperationId}).",
                    userName, reporter.OperationId);
                reporter.Error("Backup cancelled.");
                reporterFactory.Release(reporter.OperationId);
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database backup failed for Admin {User}.", userName);
                reporter.Error("Backup failed — check application logs.");
                reporterFactory.Release(reporter.OperationId);

                await audit.LogAsync(
                    userId,
                    actionType:  "BACKUP_FAILED",
                    tableName:   "Backup",
                    description: $"Database backup failed: {ex.Message}");
                return;
            }

            // ── Audit log ──────────────────────────────────────────────────
            var sectionList = result.IncludedSections.Count > 0
                ? string.Join(", ", result.IncludedSections)
                : "none";
            var objectSummary = result.HasSchemaObjects
                ? $" Schema objects: {result.SchemaTableCount} tables, {result.ProcedureCount} procedures, " +
                  $"{result.FunctionCount} functions, {result.ViewCount} views, {result.TriggerCount} triggers."
                : string.Empty;

            var summary = $"Backup generated [{sectionList}]: {result.TotalRows:N0} rows across " +
                          $"{result.TableCount} table(s).{objectSummary} File: {result.FileName}.";

            await audit.LogAsync(
                userId,
                actionType:  "BACKUP",
                tableName:   "Backup",
                description: summary,
                newValues:   System.Text.Json.JsonSerializer.Serialize(new
                {
                    Sections   = result.IncludedSections,
                    TableCount = result.TableCount,
                    Tables     = result.RowCounts.OrderBy(kv => kv.Key)
                                                 .Select(kv => new { Table = kv.Key, Rows = kv.Value }),
                    Schema     = new
                    {
                        Tables     = result.SchemaTableCount,
                        Procedures = result.ProcedureCount,
                        Functions  = result.FunctionCount,
                        Views      = result.ViewCount,
                        Triggers   = result.TriggerCount,
                    }
                }));

            logger.LogInformation(
                "Backup complete — {TotalRows} rows, {TableCount} table(s), file: {FileName}.",
                result.TotalRows, result.TableCount, result.FileName);

            // ── Optional email delivery ────────────────────────────────────
            if (sendEmail)
            {
                var adminTo = config["Email:AdminTo"];
                if (!string.IsNullOrWhiteSpace(adminTo))
                {
                    var localTime = TimeZoneHelper.ConvertFromUtc(result.TakenAtUtc, TimeZoneHelper.CentralWindowsId);
                    var bodyHtml  = $"<p>ScrumFlix database backup completed successfully.</p>" +
                                    $"<p><strong>Taken at:</strong> {localTime:ddd MMM d, yyyy h:mm tt} CDT</p>" +
                                    $"<p><strong>Tables:</strong> {result.TableCount}</p>" +
                                    $"<p><strong>Total rows:</strong> {result.TotalRows:N0}</p>" +
                                    $"<p>The backup archive is attached.</p>";

                    var sendTask = email.SendWithPdfAttachmentAsync(
                        toEmail:            adminTo,
                        toName:             "ScrumFlix Admin",
                        subject:            $"ScrumFlix DB Backup — {result.FileName}",
                        bodyHtml:           bodyHtml,
                        attachmentBytes:    result.ZipBytes,
                        attachmentFileName: result.FileName,
                        cancellationToken:  CancellationToken.None);

                    // Fire-and-forget but safe: run a background task that awaits the sendTask and logs outcome.
                    await Task.Run(async () =>
                    {
                        try
                        {
                            await sendTask.ConfigureAwait(false);
                            logger.LogInformation("Backup email sent to {AdminTo} — {FileName}.", adminTo, result.FileName);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Backup email delivery failed for {FileName}.", result.FileName);
                        }
                    });
                }
                else
                {
                    logger.LogWarning(
                        "Backup email requested but Email:AdminTo is not configured. Skipping email.");
                }
            }

            // ── Stage the .zip for download and signal completion ──────────
            cache.Set(cacheKey, (result.ZipBytes, result.FileName), cacheTtl);

            reporter.Complete(
                $"Backup complete — {result.TotalRows:N0} rows across {result.TableCount} table(s).");
            reporterFactory.Release(reporter.OperationId);
        });

        // Return immediately — generation runs on QueuedHostedService. The client
        // (already connected to /progressHub on page load) joins this operation's
        // group, watches the spinner, then htmx-swaps in the download panel on the
        // terminal ProgressUpdate.
        return Json(new { operationId = reporter.OperationId });
    }

    // ── GET: BackupResultPanel (HTMX swap target) ───────────────────────────

    /// <summary>
    /// GET /Admin/AdminBackup/BackupResultPanel?operationId=...
    /// Returns the "Backup ready — Download" panel as a partial view so the
    /// backup page can swap it in (HTMX outerHTML into <c>#backup-result-panel</c>)
    /// once a queued backup completes. The Download button points at
    /// <see cref="DownloadBackup"/> for the same operation id.
    /// </summary>
    [HttpGet]
    public IActionResult BackupResultPanel(string operationId)
    {
        if (RoleGuard(1) is { } r) return r;

        if (string.IsNullOrWhiteSpace(operationId))
            return NotFound();

        // Surface the staged file name when still cached, so the panel can label
        // the download. Absence is non-fatal — the button still works until TTL.
        string? fileName = null;
        if (_cache.TryGetValue<(byte[] ZipBytes, string FileName)>(BackupCacheKeyPrefix + operationId, out var staged))
            fileName = staged.FileName;

        return PartialView("_BackupResultPanelPartial",
            new BackupResultPanelViewModel(operationId, fileName));
    }

    // ── GET: DownloadBackup ──────────────────────────────────────────────────

    /// <summary>
    /// Streams the .zip generated by a prior <see cref="TriggerBackup"/> call,
    /// identified by <paramref name="operationId"/>, and evicts it from the
    /// cache. Returns 404 if the operation id is unknown or the cache entry
    /// has expired (see <see cref="BackupCacheTtl"/>).
    /// </summary>
    [HttpGet]
    public IActionResult DownloadBackup(string operationId)
    {
        if (RoleGuard(1) is { } r) return r;

        if (string.IsNullOrWhiteSpace(operationId))
            return NotFound();

        var cacheKey = BackupCacheKeyPrefix + operationId;

        if (!_cache.TryGetValue<(byte[] ZipBytes, string FileName)>(cacheKey, out var staged))
        {
            _logger.LogWarning(
                "DownloadBackup: no staged backup found for operationId={OperationId} (expired or unknown).",
                operationId);
            return NotFound();
        }

        _cache.Remove(cacheKey);

        return File(staged.ZipBytes, "application/zip", staged.FileName);
    }
}
