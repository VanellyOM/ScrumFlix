/*
 * File:      /ScrumFlix/Services/Backup/Components/ManifestBuilder.cs
 * Namespace: ScrumFlix.Services.Backup
 * Purpose:   Assembles the typed BackupManifest written to manifest.json. Pulls
 *            together the data row counts and the captured schema-object name
 *            lists into one machine-readable record describing the archive.
 *
 *            Centralising this keeps DatabaseBackupService a thin assembler and
 *            gives a single place to evolve the manifest schema (ManifestVersion).
 */

namespace ScrumFlix.Services.Backup;

/// <summary>Builds the <see cref="BackupManifest"/> for a completed backup run.</summary>
internal static class ManifestBuilder
{
    /// <summary>
    /// Composes the manifest from the data row counts and the optional schema /
    /// object sections.
    /// </summary>
    public static BackupManifest Build(
        DatabaseBackupOptions options,
        IReadOnlyDictionary<string, int> rowCounts,
        SchemaSection? schema,
        DatabaseObjectSection? objects,
        DateTime generatedAtUtc)
    {
        var notes = new List<string>
        {
            "Users table: UserPassword and PasswordHash are excluded for security.",
        };
        if (objects is { IsEmpty: false })
            notes.Add("Routine/view/trigger DEFINER clauses were stripped for portability; "
                    + "restore the routines/ files via the mysql client (SOURCE) so DELIMITER is honoured.");
        if (schema is { TableNames.Count: > 0 })
            notes.Add("Schema files use SET FOREIGN_KEY_CHECKS=0 during restore; run schema/00_schema.sql before data.");

        return new BackupManifest
        {
            GeneratedAtUtc = generatedAtUtc.ToString("o"),
            Sections = new BackupManifest.BackupSections
            {
                Schema           = schema is { TableNames.Count: > 0 },
                Data             = options.IncludeData,
                StoredProcedures = objects is not null && (objects.Procedures.Count + objects.Functions.Count) > 0,
                Views            = objects is { Views.Count: > 0 },
                Triggers         = objects is { Triggers.Count: > 0 },
            },
            Tables = rowCounts
                .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .Select(kv => new BackupManifest.TableRowCount { Table = kv.Key, Rows = kv.Value })
                .ToList(),
            TablesErrored = rowCounts.Where(kv => kv.Value < 0).Select(kv => kv.Key).ToList(),
            Procedures = objects?.Procedures ?? [],
            Functions  = objects?.Functions ?? [],
            Views      = objects?.Views ?? [],
            Triggers   = objects?.Triggers ?? [],
            TotalRows  = rowCounts.Values.Where(v => v >= 0).Sum(),
            Notes = notes,
        };
    }
}
