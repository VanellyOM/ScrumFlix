/*
 * File:      /ScrumFlix/Services/Backup/Models/DatabaseBackupOptions.cs
 * Namespace: ScrumFlix.Services
 * Purpose:   Describes WHAT a backup run should capture — schema, data,
 *            stored routines, views, triggers — and over which tables.
 *
 *            Placed in namespace ScrumFlix.Services (not ...Backup) so it sits
 *            alongside BackupResult/BackupTableDescriptor on the public contract
 *            surface and is reachable through the existing global using.
 *
 * Design:
 *   - The five Include* flags are the source of truth. Granular UI toggles map
 *     straight onto them.
 *   - BackupMode is sugar: From(mode) produces a ready-made options object for
 *     the four common presets in the Phase 4+ plan (Schema Only, Data Only,
 *     Full, Individual Table). The UI can offer presets and still let an admin
 *     fine-tune the individual flags afterward.
 *   - SelectedTableKeys scopes the table-bound work (schema CREATE + data). When
 *     null/empty, the service falls back to "all non-excluded tables", matching
 *     the long-standing data-export behaviour.
 *   - Routines/views/triggers are schema-global, not table-scoped, so they are
 *     governed purely by their flags and ignore SelectedTableKeys.
 */

namespace ScrumFlix.Services;

/// <summary>
/// Preset shapes for a backup run. Maps to a set of <see cref="DatabaseBackupOptions"/>
/// flags via <see cref="DatabaseBackupOptions.From"/>.
/// </summary>
public enum BackupMode
{
    /// <summary>Reconstruction DDL only — CREATE TABLE for every selected table, no rows.</summary>
    SchemaOnly,

    /// <summary>Rows only (JSON + INSERT scripts). The historical default behaviour.</summary>
    DataOnly,

    /// <summary>Everything: schema, data, stored procedures, functions, views, and triggers.</summary>
    Full,

    /// <summary>A single table's schema and data, for independent restore.</summary>
    IndividualTable,
}

/// <summary>
/// Options controlling a single <see cref="IDatabaseBackupService.GenerateAsync(DatabaseBackupOptions, System.Threading.CancellationToken)"/> run.
/// </summary>
public sealed class DatabaseBackupOptions
{
    /// <summary>Emit CREATE TABLE DDL (indexes and constraints included inline by MySQL).</summary>
    public bool IncludeSchema { get; set; }

    /// <summary>Emit table rows as JSON files and batched INSERT scripts.</summary>
    public bool IncludeData { get; set; } = true;

    /// <summary>Emit CREATE PROCEDURE / CREATE FUNCTION DDL for all stored routines.</summary>
    public bool IncludeStoredProcedures { get; set; }

    /// <summary>Emit CREATE VIEW DDL for all views.</summary>
    public bool IncludeViews { get; set; }

    /// <summary>Emit CREATE TRIGGER DDL for all triggers.</summary>
    public bool IncludeTriggers { get; set; }

    /// <summary>
    /// Prepend DROP ... IF EXISTS before each CREATE so a restore replaces existing
    /// objects rather than failing on "already exists". Recommended for a clean
    /// disaster-recovery restore into a populated database.
    /// </summary>
    public bool DropBeforeCreate { get; set; } = true;

    /// <summary>
    /// Table keys (canonical names from <see cref="IDatabaseBackupService.GetAvailableTables"/>)
    /// to include for schema and data. Null/empty means all non-excluded tables.
    /// Ignored by routine/view/trigger capture, which is schema-global.
    /// </summary>
    public IReadOnlyList<string>? SelectedTableKeys { get; set; }

    /// <summary>True when at least one section is requested; a no-op backup is rejected.</summary>
    public bool HasAnySection =>
        IncludeSchema || IncludeData || IncludeStoredProcedures || IncludeViews || IncludeTriggers;

    /// <summary>
    /// Builds an options object for a preset <paramref name="mode"/>.
    /// </summary>
    /// <param name="mode">The preset shape.</param>
    /// <param name="selectedTableKeys">
    /// Tables to scope schema/data to. For <see cref="BackupMode.IndividualTable"/>
    /// this is expected to contain exactly one key.
    /// </param>
    /// <param name="dropBeforeCreate">Whether CREATE statements are preceded by DROP ... IF EXISTS.</param>
    public static DatabaseBackupOptions From(
        BackupMode mode,
        IReadOnlyList<string>? selectedTableKeys = null,
        bool dropBeforeCreate = true) => mode switch
    {
        BackupMode.SchemaOnly => new DatabaseBackupOptions
        {
            IncludeSchema = true,
            IncludeData = false,
            IncludeStoredProcedures = true,
            IncludeViews = true,
            IncludeTriggers = true,
            DropBeforeCreate = dropBeforeCreate,
            SelectedTableKeys = selectedTableKeys,
        },
        BackupMode.DataOnly => new DatabaseBackupOptions
        {
            IncludeData = true,
            DropBeforeCreate = dropBeforeCreate,
            SelectedTableKeys = selectedTableKeys,
        },
        BackupMode.Full => new DatabaseBackupOptions
        {
            IncludeSchema = true,
            IncludeData = true,
            IncludeStoredProcedures = true,
            IncludeViews = true,
            IncludeTriggers = true,
            DropBeforeCreate = dropBeforeCreate,
            SelectedTableKeys = selectedTableKeys,
        },
        BackupMode.IndividualTable => new DatabaseBackupOptions
        {
            IncludeSchema = true,
            IncludeData = true,
            IncludeStoredProcedures = false,
            IncludeViews = false,
            IncludeTriggers = false,
            DropBeforeCreate = dropBeforeCreate,
            SelectedTableKeys = selectedTableKeys,
        },
        _ => new DatabaseBackupOptions { SelectedTableKeys = selectedTableKeys },
    };
}
