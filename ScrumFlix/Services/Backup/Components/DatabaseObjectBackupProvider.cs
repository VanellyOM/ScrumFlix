/*
 * File:      /ScrumFlix/Services/Backup/Components/DatabaseObjectBackupProvider.cs
 * Namespace: ScrumFlix.Services.Backup
 * Purpose:   Captures non-table schema objects — stored procedures, functions,
 *            views, and triggers — that the previous data-only backup ignored.
 *            This is the component the Phase 4+ plan calls
 *            "StoredProcedureBackupProvider", widened to cover views and triggers
 *            because they share the same SHOW CREATE + DEFINER/DELIMITER mechanics.
 *
 * Output (inside the archive):
 *   routines/00_routines.sql        — SOURCEs every object file in safe order
 *                                     (procedures, functions, views, triggers).
 *   routines/NN_proc_<name>.sql      — DROP + DELIMITER-wrapped CREATE PROCEDURE.
 *   routines/NN_func_<name>.sql      — DROP + DELIMITER-wrapped CREATE FUNCTION.
 *   routines/NN_view_<name>.sql      — DROP + CREATE VIEW (no delimiter needed).
 *   routines/NN_trig_<name>.sql      — DROP + DELIMITER-wrapped CREATE TRIGGER.
 *
 * Portability decisions:
 *   - DEFINER clauses are stripped (in MySqlIntrospector) so objects restore
 *     under whatever account runs the import, not a source-host account that may
 *     not exist on the target.
 *   - Procedure/function/trigger bodies contain semicolons, so each is wrapped in
 *     DELIMITER $$ … $$ / DELIMITER ;. DELIMITER is a mysql-client directive, so
 *     these files must be restored via the mysql CLI (interactive SOURCE), not by
 *     a raw multi-statement driver call. The archive's restore guide says so.
 *   - Relevant for ScrumFlix specifically: this captures GenerateSeatsForScreen
 *     and SeedShowtimesByTheater, which the data-only backup could never restore.
 */

using System.Text;

namespace ScrumFlix.Services.Backup;

/// <summary>Builds the <c>routines/</c> section: procedures, functions, views, triggers.</summary>
internal sealed class DatabaseObjectBackupProvider
{
    private readonly MySqlIntrospector _introspector;
    private readonly ILogger _logger;

    public DatabaseObjectBackupProvider(MySqlIntrospector introspector, ILogger logger)
    {
        _introspector = introspector;
        _logger = logger;
    }

    /// <summary>
    /// Captures the requested object types from the schema.
    /// </summary>
    /// <param name="schema">Database name (from MySqlIntrospector.GetDatabaseNameAsync).</param>
    /// <param name="includeRoutines">Capture stored procedures and functions.</param>
    /// <param name="includeViews">Capture views.</param>
    /// <param name="includeTriggers">Capture triggers.</param>
    /// <param name="dropBeforeCreate">Prepend DROP ... IF EXISTS to each object.</param>
    /// <param name="generatedAtUtc">Snapshot timestamp for file headers.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<DatabaseObjectSection> CaptureAsync(
        string schema,
        bool includeRoutines,
        bool includeViews,
        bool includeTriggers,
        bool dropBeforeCreate,
        DateTime generatedAtUtc,
        CancellationToken ct)
    {
        var files = new List<BackupFile>();
        var sourceOrder = new List<string>();
        var procedures = new List<string>();
        var functions = new List<string>();
        var views = new List<string>();
        var triggers = new List<string>();
        int index = 1;

        // ── Stored procedures and functions ────────────────────────────────
        if (includeRoutines)
        {
            var routines = await SafeListAsync(() => _introspector.GetRoutinesAsync(schema, ct), "routines");

            foreach (var (name, type) in routines)
            {
                ct.ThrowIfCancellationRequested();
                var isFunction = string.Equals(type, "FUNCTION", StringComparison.OrdinalIgnoreCase);

                var ddl = isFunction
                    ? await SafeDdlAsync(() => _introspector.ShowCreateFunctionAsync(name, ct), "function", name)
                    : await SafeDdlAsync(() => _introspector.ShowCreateProcedureAsync(name, ct), "procedure", name);
                if (ddl is null) continue;

                var kind = isFunction ? "FUNCTION" : "PROCEDURE";
                var prefix = isFunction ? "func" : "proc";
                var fileName = $"{index:D2}_{prefix}_{name}.sql";
                var content = WrapDelimited(kind, name, ddl, dropBeforeCreate, generatedAtUtc);

                files.Add(new BackupFile($"routines/{fileName}", content));
                sourceOrder.Add(fileName);
                (isFunction ? functions : procedures).Add(name);
                index++;
            }
        }

        // ── Views ──────────────────────────────────────────────────────────
        if (includeViews)
        {
            var viewNames = await SafeListAsync(() => _introspector.GetViewNamesAsync(schema, ct), "views");
            foreach (var name in viewNames)
            {
                ct.ThrowIfCancellationRequested();
                var ddl = await SafeDdlAsync(() => _introspector.ShowCreateViewAsync(name, ct), "view", name);
                if (ddl is null) continue;

                var fileName = $"{index:D2}_view_{name}.sql";
                var content = WrapPlain("VIEW", name, ddl, dropBeforeCreate, generatedAtUtc);

                files.Add(new BackupFile($"routines/{fileName}", content));
                sourceOrder.Add(fileName);
                views.Add(name);
                index++;
            }
        }

        // ── Triggers ───────────────────────────────────────────────────────
        if (includeTriggers)
        {
            var triggerNames = await SafeListAsync(() => _introspector.GetTriggerNamesAsync(schema, ct), "triggers");
            foreach (var name in triggerNames)
            {
                ct.ThrowIfCancellationRequested();
                var ddl = await SafeDdlAsync(() => _introspector.ShowCreateTriggerAsync(name, ct), "trigger", name);
                if (ddl is null) continue;

                var fileName = $"{index:D2}_trig_{name}.sql";
                var content = WrapDelimited("TRIGGER", name, ddl, dropBeforeCreate, generatedAtUtc);

                files.Add(new BackupFile($"routines/{fileName}", content));
                sourceOrder.Add(fileName);
                triggers.Add(name);
                index++;
            }
        }

        if (files.Count > 0)
            files.Add(BuildAggregator(sourceOrder, generatedAtUtc));

        return new DatabaseObjectSection(files, procedures, functions, views, triggers);
    }

    // ── File builders ──────────────────────────────────────────────────────

    /// <summary>Builds a DELIMITER-wrapped object file (procedure / function / trigger).</summary>
    private static byte[] WrapDelimited(
        string kind, string name, string ddl, bool dropBeforeCreate, DateTime generatedAtUtc)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"-- ScrumFlix backup: {kind.ToLowerInvariant()} `{name}`");
        sb.AppendLine($"-- Generated: {generatedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("-- Restore via the mysql client (SOURCE) — DELIMITER is a client directive.");
        sb.AppendLine();
        if (dropBeforeCreate)
            sb.AppendLine($"DROP {kind} IF EXISTS {MySqlDdl.Quote(name)};");
        sb.AppendLine("DELIMITER $$");
        sb.AppendLine($"{ddl.TrimEnd()}$$");
        sb.AppendLine("DELIMITER ;");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    /// <summary>Builds a plain (non-delimited) object file — used for views.</summary>
    private static byte[] WrapPlain(
        string kind, string name, string ddl, bool dropBeforeCreate, DateTime generatedAtUtc)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"-- ScrumFlix backup: {kind.ToLowerInvariant()} `{name}`");
        sb.AppendLine($"-- Generated: {generatedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        if (dropBeforeCreate)
            sb.AppendLine($"DROP {kind} IF EXISTS {MySqlDdl.Quote(name)};");
        sb.AppendLine(ddl.TrimEnd());
        sb.AppendLine(";");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    /// <summary>Writes routines/00_routines.sql, sourcing every object in capture order.</summary>
    private static BackupFile BuildAggregator(IReadOnlyList<string> sourceOrder, DateTime generatedAtUtc)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- ScrumFlix backup: stored routines, views, and triggers");
        sb.AppendLine($"-- Generated: {generatedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("-- Run from the backup ROOT folder in the mysql client:  SOURCE routines/00_routines.sql;");
        sb.AppendLine("-- Restore AFTER schema + data so referenced tables/columns exist.");
        sb.AppendLine();
        foreach (var file in sourceOrder)
            sb.AppendLine($"SOURCE routines/{file};");
        return new BackupFile("routines/00_routines.sql", Encoding.UTF8.GetBytes(sb.ToString()));
    }

    // ── Defensive wrappers ───────────────────────────────────────────────────

    private async Task<List<T>> SafeListAsync<T>(Func<Task<List<T>>> query, string what)
    {
        try { return await query(); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Backup/objects: failed to enumerate {What} — section skipped.", what);
            return [];
        }
    }

    private async Task<string?> SafeDdlAsync(Func<Task<string?>> query, string what, string name)
    {
        try
        {
            var ddl = await query();
            if (string.IsNullOrWhiteSpace(ddl))
                _logger.LogWarning("Backup/objects: no DDL for {What} {Name} — skipping.", what, name);
            return string.IsNullOrWhiteSpace(ddl) ? null : ddl;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Backup/objects: SHOW CREATE failed for {What} {Name} — skipping.", what, name);
            return null;
        }
    }
}
