/*
 * File:      /ScrumFlix/Services/Backup/Components/SchemaBackupProvider.cs
 * Namespace: ScrumFlix.Services.Backup
 * Purpose:   Captures table-reconstruction DDL (CREATE TABLE) for a set of
 *            tables, using the server's own SHOW CREATE TABLE output so indexes,
 *            constraints, AUTO_INCREMENT, charset and engine all come through
 *            exactly as MySQL would re-emit them.
 *
 * Output (inside the archive):
 *   schema/00_schema.sql        — FK-checks-off wrapper that SOURCEs each table
 *                                 file in dependency order, then FK-checks-on.
 *   schema/NN_<Table>.sql        — optional DROP TABLE IF EXISTS + CREATE TABLE.
 *
 * Ordering:
 *   Tables are emitted in the caller-supplied order (the orchestrator passes the
 *   FK-safe registry order). The aggregator wraps everything in
 *   SET FOREIGN_KEY_CHECKS=0/1 so order is not strictly required for restore, but
 *   keeping it dependency-correct keeps the files readable.
 *
 * Note:
 *   This provider only handles BASE TABLE DDL. Views, routines, and triggers are
 *   captured by DatabaseObjectBackupProvider so each concern stays isolated.
 */

using System.Text;

namespace ScrumFlix.Services.Backup;

/// <summary>Builds the <c>schema/</c> section of a backup from live CREATE TABLE DDL.</summary>
internal sealed class SchemaBackupProvider
{
    private readonly MySqlIntrospector _introspector;
    private readonly ILogger _logger;

    public SchemaBackupProvider(MySqlIntrospector introspector, ILogger logger)
    {
        _introspector = introspector;
        _logger = logger;
    }

    /// <summary>
    /// Captures CREATE TABLE DDL for the given tables.
    /// </summary>
    /// <param name="tableNames">SQL table names, in the order they should appear.</param>
    /// <param name="dropBeforeCreate">When true, each file leads with DROP TABLE IF EXISTS.</param>
    /// <param name="generatedAtUtc">Snapshot timestamp, written into file headers.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<SchemaSection> CaptureAsync(
        IReadOnlyList<string> tableNames,
        bool dropBeforeCreate,
        DateTime generatedAtUtc,
        CancellationToken ct)
    {
        var files = new List<BackupFile>();
        var captured = new List<string>();
        var sourceOrder = new List<string>();

        int index = 1;
        foreach (var table in tableNames)
        {
            ct.ThrowIfCancellationRequested();

            string? ddl;
            try
            {
                ddl = await _introspector.ShowCreateTableAsync(table, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Backup/schema: SHOW CREATE TABLE failed for {Table} — skipping.", table);
                continue;
            }

            if (string.IsNullOrWhiteSpace(ddl))
            {
                _logger.LogWarning("Backup/schema: no DDL returned for {Table} — skipping.", table);
                continue;
            }

            var fileName = $"{index:D2}_{table}.sql";
            var sb = new StringBuilder();
            sb.AppendLine($"-- ScrumFlix backup: schema for `{table}`");
            sb.AppendLine($"-- Generated: {generatedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine();
            if (dropBeforeCreate)
                sb.AppendLine($"DROP TABLE IF EXISTS {MySqlDdl.Quote(table)};");
            sb.AppendLine(ddl.TrimEnd());
            sb.AppendLine(";");

            files.Add(new BackupFile($"schema/{fileName}", Encoding.UTF8.GetBytes(sb.ToString())));
            sourceOrder.Add(fileName);
            captured.Add(table);
            index++;
        }

        if (captured.Count > 0)
            files.Add(BuildAggregator(sourceOrder, generatedAtUtc));

        return new SchemaSection(files, captured);
    }

    /// <summary>Writes schema/00_schema.sql, which SOURCEs each table file inside an FK-off block.</summary>
    private static BackupFile BuildAggregator(IReadOnlyList<string> sourceOrder, DateTime generatedAtUtc)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- ScrumFlix backup: schema reconstruction");
        sb.AppendLine($"-- Generated: {generatedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("-- Run from the backup ROOT folder in the mysql client:  SOURCE schema/00_schema.sql;");
        sb.AppendLine();
        sb.AppendLine("SET FOREIGN_KEY_CHECKS = 0;");
        sb.AppendLine();
        foreach (var file in sourceOrder)
            sb.AppendLine($"SOURCE schema/{file};");
        sb.AppendLine();
        sb.AppendLine("SET FOREIGN_KEY_CHECKS = 1;");
        return new BackupFile("schema/00_schema.sql", Encoding.UTF8.GetBytes(sb.ToString()));
    }
}
