/*
 * File:      /ScrumFlix/Services/IDatabaseBackupService.cs
 * Namespace: ScrumFlix.Services
 * Purpose:   Contract for the database backup service.
 *
 *            Produces a .zip archive containing one JSON file and one INSERT
 *            SQL script per requested table. The output is self-contained and
 *            importable into any MySQL 8 instance (including a fresh Aiven DB).
 *
 *            Format choice:
 *              - JSON  — human-readable, diff-friendly, re-seedable via
 *                        SampleDataSeederFull or a custom import script.
 *              - SQL   — standard INSERT statements; directly executable via
 *                        mysql CLI, DBeaver, or the Aiven console query runner.
 *              - Both formats are included in every .zip so the admin has
 *                immediate options without re-running the backup.
 *
 *            Large-table policy:
 *              Tables flagged as LargeTable in BackupTableDescriptor are
 *              queried in pages of PageSize rows. All other tables are read
 *              in a single query. This prevents OOM on Somee.com shared hosting
 *              for tables like ShowtimeSeat (44 K+ rows) and Logs (unbounded).
 *
 *            Security:
 *              The Users table serialization OMITS UserPassword (legacy plaintext)
 *              and PasswordHash (BCrypt hash). Backup files should not be
 *              distribution vectors for credential data. UserId, UserName,
 *              RoleId, IsActive, and audit timestamps are included.
 */

namespace ScrumFlix.Services;

/// <summary>
/// Produces a .zip archive snapshot of selected ScrumFlix database tables.
/// </summary>
public interface IDatabaseBackupService
{
    /// <summary>
    /// Generates a .zip archive containing JSON and INSERT SQL representations
    /// of every table listed in <paramref name="tableKeys"/>.
    /// </summary>
    /// <param name="tableKeys">
    /// The set of table keys to include. Keys are the canonical table names
    /// returned by <see cref="GetAvailableTables"/>. Pass null or empty to
    /// include all non-excluded tables.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="BackupResult"/> containing the .zip bytes, filename,
    /// per-table row counts, and the UTC timestamp of the snapshot.
    /// </returns>
    Task<BackupResult> GenerateAsync(
        IEnumerable<string>? tableKeys,
        CancellationToken    cancellationToken = default);

    /// <summary>
    /// Generates a backup archive whose contents are governed by
    /// <paramref name="options"/> — any combination of table schema (CREATE TABLE),
    /// table data (JSON + INSERT), stored procedures, functions, views, and triggers.
    /// This is the full-capability entry point used by the Admin backup page.
    /// </summary>
    /// <param name="options">
    /// Which sections to capture and which tables to scope schema/data to.
    /// See <see cref="DatabaseBackupOptions"/> and <see cref="BackupMode"/>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="BackupResult"/> describing the archive: zip bytes, filename,
    /// per-table row counts, and counts of captured schema objects.
    /// </returns>
    Task<BackupResult> GenerateAsync(
        DatabaseBackupOptions options,
        CancellationToken     cancellationToken = default);

    /// <summary>
    /// Returns the full list of tables available for backup, with metadata
    /// (display name, row estimate, whether the table is large/paged).
    /// Used to populate the table-selection checklist on the backup page.
    /// </summary>
    IReadOnlyList<BackupTableDescriptor> GetAvailableTables();
}

// ── Result and descriptor types ───────────────────────────────────────────────

/// <summary>
/// The result of a <see cref="IDatabaseBackupService.GenerateAsync"/> call.
/// </summary>
public sealed class BackupResult
{
    /// <summary>The .zip archive as a byte array, ready for File() download.</summary>
    public required byte[]   ZipBytes       { get; init; }

    /// <summary>Suggested download filename, e.g. "scrumflix_backup_20260610_213000.zip".</summary>
    public required string   FileName       { get; init; }

    /// <summary>UTC timestamp when the backup snapshot was taken.</summary>
    public required DateTime TakenAtUtc     { get; init; }

    /// <summary>Row counts per table included in this backup.</summary>
    public required IReadOnlyDictionary<string, int> RowCounts { get; init; }

    /// <summary>Total rows across all tables.</summary>
    public int TotalRows => RowCounts.Values.Where(v => v >= 0).Sum();

    /// <summary>Number of tables whose CREATE TABLE DDL was captured (schema section).</summary>
    public int SchemaTableCount { get; init; }

    /// <summary>Number of stored procedures captured.</summary>
    public int ProcedureCount { get; init; }

    /// <summary>Number of stored functions captured.</summary>
    public int FunctionCount { get; init; }

    /// <summary>Number of views captured.</summary>
    public int ViewCount { get; init; }

    /// <summary>Number of triggers captured.</summary>
    public int TriggerCount { get; init; }

    /// <summary>Human-readable list of the sections present in the archive (e.g. "Schema", "Data", "Triggers").</summary>
    public IReadOnlyList<string> IncludedSections { get; init; } = [];

    /// <summary>True when the archive contains any non-table-data DDL (schema/routines/views/triggers).</summary>
    public bool HasSchemaObjects =>
        SchemaTableCount > 0 || ProcedureCount > 0 || FunctionCount > 0 || ViewCount > 0 || TriggerCount > 0;
}

/// <summary>
/// Metadata for a single table available for backup selection.
/// </summary>
public sealed class BackupTableDescriptor
{
    /// <summary>
    /// Canonical key used to identify this table in backup requests.
    /// Matches the SQL table name (e.g. "ShowtimeSeat", "Movies").
    /// </summary>
    public required string Key         { get; init; }

    /// <summary>Human-readable display name shown in the UI checklist.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Brief description of what the table contains.</summary>
    public required string Description { get; init; }

    /// <summary>
    /// Category used to group tables in the UI (e.g. "Cinema", "Workforce", "System").
    /// </summary>
    public required string Category    { get; init; }

    /// <summary>
    /// When true, the table is queried in pages rather than all at once.
    /// Set for tables known to have high row counts (ShowtimeSeat, Logs).
    /// </summary>
    public bool IsLargeTable { get; init; }

    /// <summary>
    /// When true, this table is excluded from the default selection because
    /// it contains transient or ephemeral data (e.g. SeatReservation, Logs).
    /// The admin can still manually include it.
    /// </summary>
    public bool ExcludedByDefault { get; init; }
}
