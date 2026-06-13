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
using ScrumFlix.Infrastructure;
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
        IMemoryCache                   cache)
    {
        _backup = backup;
        _audit  = audit;
        _email  = email;
        _config = config;
        _logger = logger;
        _reporterFactory = reporterFactory;
        _cache  = cache;
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

        var userId = CurrentUserId ?? 0;

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
            CurrentUserName, selectedKeys.Count,
            options.IncludeSchema, options.IncludeData, options.IncludeStoredProcedures,
            options.IncludeViews, options.IncludeTriggers);

        var reporter = string.IsNullOrWhiteSpace(vm.OperationId)
            ? _reporterFactory.Create("Database Backup", HttpContext.RequestAborted)
            : _reporterFactory.Create(vm.OperationId, "Database Backup", HttpContext.RequestAborted);

        BackupResult result;
        try
        {
            result = await _backup.GenerateAsync(options, reporter, reporter.CancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "Database backup cancelled by Admin {User} (operationId={OperationId}).",
                CurrentUserName, reporter.OperationId);

            reporter.Error("Backup cancelled.");
            _reporterFactory.Release(reporter.OperationId);

            return BadRequest(new { error = "Backup was cancelled." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database backup failed for Admin {User}.", CurrentUserName);

            reporter.Error("Backup failed — check application logs.");
            _reporterFactory.Release(reporter.OperationId);

            await _audit.LogAsync(
                userId,
                actionType:  "BACKUP_FAILED",
                tableName:   "Backup",
                description: $"Database backup failed: {ex.Message}");

            return BadRequest(new
            {
                error = $"Backup failed: {ex.Message}. Check application logs for details."
            });
        }

        // ── Audit log ──────────────────────────────────────────────────────
        var sectionList = result.IncludedSections.Count > 0
            ? string.Join(", ", result.IncludedSections)
            : "none";
        var objectSummary = result.HasSchemaObjects
            ? $" Schema objects: {result.SchemaTableCount} tables, {result.ProcedureCount} procedures, " +
              $"{result.FunctionCount} functions, {result.ViewCount} views, {result.TriggerCount} triggers."
            : string.Empty;

        var summary = $"Backup generated [{sectionList}]: {result.TotalRows:N0} rows across " +
                      $"{result.TableCount} table(s).{objectSummary} File: {result.FileName}.";

        await _audit.LogAsync(
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

        _logger.LogInformation(
            "Backup complete — {TotalRows} rows, {TableCount} table(s), file: {FileName}.",
            result.TotalRows, result.TableCount, result.FileName);

        // ── Optional email delivery ────────────────────────────────────────
        if (vm.SendEmail)
        {
            var adminTo = _config["Email:AdminTo"];
            if (!string.IsNullOrWhiteSpace(adminTo))
            {
                var localTime   = TimeZoneHelper.ConvertFromUtc(result.TakenAtUtc, TimeZoneHelper.CentralWindowsId);
                var bodyHtml    = $"<p>ScrumFlix database backup completed successfully.</p>" +
                                  $"<p><strong>Taken at:</strong> {localTime:ddd MMM d, yyyy h:mm tt} CDT</p>" +
                                  $"<p><strong>Tables:</strong> {result.TableCount}</p>" +
                                  $"<p><strong>Total rows:</strong> {result.TotalRows:N0}</p>" +
                                  $"<p>The backup archive is attached.</p>";

                // Fire-and-forget — do not block the response on SMTP
                _ = _email.SendWithPdfAttachmentAsync(
                        toEmail:            adminTo,
                        toName:             "ScrumFlix Admin",
                        subject:            $"ScrumFlix DB Backup — {result.FileName}",
                        bodyHtml:           bodyHtml,
                        attachmentBytes:    result.ZipBytes,
                        attachmentFileName: result.FileName,
                        cancellationToken:  CancellationToken.None)
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                            _logger.LogWarning(t.Exception,
                                "Backup email delivery failed for {FileName}.", result.FileName);
                        else
                            _logger.LogInformation(
                                "Backup email sent to {AdminTo} — {FileName}.", adminTo, result.FileName);
                    }, TaskScheduler.Default);
            }
            else
            {
                _logger.LogWarning(
                    "Backup email requested but Email:AdminTo is not configured. Skipping email.");
            }
        }

        // ── Stage the .zip for download and signal completion ──────────────
        _cache.Set(
            BackupCacheKeyPrefix + reporter.OperationId,
            (result.ZipBytes, result.FileName),
            BackupCacheTtl);

        reporter.Complete(
            $"Backup complete — {result.TotalRows:N0} rows across {result.TableCount} table(s).");
        _reporterFactory.Release(reporter.OperationId);

        return Json(new
        {
            operationId = reporter.OperationId,
            fileName    = result.FileName,
        });
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
