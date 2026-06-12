/*
 * File:      /ScrumFlix/Areas/Admin/Controllers/AdminBackupController.cs
 * Namespace: ScrumFlix.Areas.Admin.Controllers
 * Purpose:   Admin controller for the Database Backup module.
 *
 *            GET  Backup        — renders the backup page with table checklist
 *                                 and last-backup metadata from TempData.
 *            POST TriggerBackup — runs IDatabaseBackupService.GenerateAsync,
 *                                 writes an AuditLog entry, optionally emails
 *                                 the .zip to the admin address, and returns
 *                                 a file download.
 *
 * Access: Admin only (RoleId == 1) via RoleGuard(1).
 *
 * Design notes:
 *   - The backup is generated synchronously within the HTTP request so the
 *     browser gets a file download response directly. For the current dataset
 *     size (~44 K ShowtimeSeat rows, small other tables) this completes in
 *     well under Somee.com's 120-second IIS request timeout.
 *   - If table selection grows to the point where generation takes > 30 seconds,
 *     migrate to a background IHostedService + SignalR progress pattern
 *     (already established by TmdbSyncService + TmdbProgressHub).
 *   - Email delivery is fire-and-forget (no await on the send task) so a
 *     misconfigured SMTP server never blocks the file download response.
 */

using ScrumFlix.Infrastructure;

namespace ScrumFlix.Areas.Admin.Controllers;

[Area("Admin")]
public class AdminBackupController : StaffControllerBase
{
    private readonly IDatabaseBackupService          _backup;
    private readonly IAuditService                   _audit;
    private readonly IEmailService                   _email;
    private readonly IConfiguration                  _config;
    private readonly ILogger<AdminBackupController>  _logger;

    public AdminBackupController(
        IDatabaseBackupService         backup,
        IAuditService                  audit,
        IEmailService                  email,
        IConfiguration                 config,
        ILogger<AdminBackupController> logger)
    {
        _backup = backup;
        _audit  = audit;
        _email  = email;
        _config = config;
        _logger = logger;
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
    /// Generates the backup .zip and returns it as a file download.
    /// Writes an AuditLog entry regardless of outcome.
    /// Optionally emails the .zip if <paramref name="vm"/>.SendEmail is true.
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
            TempData["ErrorMessage"] =
                "Select at least one section to back up (schema, data, routines, views, or triggers).";
            return RedirectToAction(nameof(Backup));
        }

        _logger.LogInformation(
            "Admin {User} triggered database backup — {TableCount} tables; " +
            "schema={Schema}, data={Data}, routines={Routines}, views={Views}, triggers={Triggers}.",
            CurrentUserName, selectedKeys.Count,
            options.IncludeSchema, options.IncludeData, options.IncludeStoredProcedures,
            options.IncludeViews, options.IncludeTriggers);

        BackupResult result;
        try
        {
            result = await _backup.GenerateAsync(options, HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database backup failed for Admin {User}.", CurrentUserName);

            await _audit.LogAsync(
                userId,
                actionType:  "BACKUP_FAILED",
                tableName:   "Backup",
                description: $"Database backup failed: {ex.Message}");

            TempData["ErrorMessage"] = $"Backup failed: {ex.Message}. Check application logs for details.";
            return RedirectToAction(nameof(Backup));
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
                      $"{result.RowCounts.Count} tables.{objectSummary} File: {result.FileName}.";

        await _audit.LogAsync(
            userId,
            actionType:  "BACKUP",
            tableName:   "Backup",
            description: summary,
            newValues:   System.Text.Json.JsonSerializer.Serialize(new
            {
                Sections   = result.IncludedSections,
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
            "Backup complete — {TotalRows} rows, {TableCount} tables, file: {FileName}.",
            result.TotalRows, result.RowCounts.Count, result.FileName);

        // ── Optional email delivery ────────────────────────────────────────
        if (vm.SendEmail)
        {
            var adminTo = _config["Email:AdminTo"];
            if (!string.IsNullOrWhiteSpace(adminTo))
            {
                var localTime   = TimeZoneHelper.ConvertFromUtc(result.TakenAtUtc, TimeZoneHelper.CentralWindowsId);
                var bodyHtml    = $"<p>ScrumFlix database backup completed successfully.</p>" +
                                  $"<p><strong>Taken at:</strong> {localTime:ddd MMM d, yyyy h:mm tt} CDT</p>" +
                                  $"<p><strong>Tables:</strong> {result.RowCounts.Count}</p>" +
                                  $"<p><strong>Total rows:</strong> {result.TotalRows:N0}</p>" +
                                  $"<p>The backup archive is attached.</p>";

                // Fire-and-forget — do not block the file download on SMTP
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

        // ── Return the .zip file download ──────────────────────────────────
        return File(result.ZipBytes, "application/zip", result.FileName);
    }
}
