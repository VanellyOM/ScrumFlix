/*
 * File:      /ScrumFlix/Services/Backup/Components/BackupArtifacts.cs
 * Namespace: ScrumFlix.Services.Backup
 * Purpose:   Small value types the DDL providers hand back to the orchestrator:
 *            a single in-archive file, and the per-section result bundles.
 *
 *            Keeping these tiny and immutable lets DatabaseBackupService stay a
 *            thin assembler — providers produce ready-to-zip bytes plus the name
 *            lists the manifest needs; the orchestrator just writes them.
 */

namespace ScrumFlix.Services.Backup;

/// <summary>One file destined for the backup archive, relative to the archive root folder.</summary>
/// <param name="RelativePath">Path inside the backup folder, e.g. "schema/01_Movies.sql".</param>
/// <param name="Content">UTF-8 file bytes.</param>
internal readonly record struct BackupFile(string RelativePath, byte[] Content);

/// <summary>Result of capturing table CREATE DDL.</summary>
/// <param name="Files">Per-table files plus the schema aggregator.</param>
/// <param name="TableNames">Tables successfully captured (for the manifest).</param>
internal sealed record SchemaSection(
    IReadOnlyList<BackupFile> Files,
    IReadOnlyList<string> TableNames);

/// <summary>Result of capturing stored routines, views, and triggers.</summary>
/// <param name="Files">Per-object files plus the routines aggregator.</param>
/// <param name="Procedures">Stored procedure names captured.</param>
/// <param name="Functions">Stored function names captured.</param>
/// <param name="Views">View names captured.</param>
/// <param name="Triggers">Trigger names captured.</param>
internal sealed record DatabaseObjectSection(
    IReadOnlyList<BackupFile> Files,
    IReadOnlyList<string> Procedures,
    IReadOnlyList<string> Functions,
    IReadOnlyList<string> Views,
    IReadOnlyList<string> Triggers)
{
    /// <summary>True when nothing was captured (so the orchestrator can skip the routines/ folder).</summary>
    public bool IsEmpty => Files.Count == 0;
}
