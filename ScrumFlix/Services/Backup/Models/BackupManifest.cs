/*
 * File:      /ScrumFlix/Services/Backup/Models/BackupManifest.cs
 * Namespace: ScrumFlix.Services
 * Purpose:   Strongly-typed manifest written to manifest.json at the root of
 *            every backup archive. Replaces the previous anonymous object so the
 *            shape is documented, testable, and stable for any future restore
 *            tooling that reads it back.
 *
 * Consumers:
 *   - Written by ManifestBuilder during zip assembly.
 *   - Intended to be machine-read by a future restore wizard; also human-read
 *     when inspecting an archive.
 */

namespace ScrumFlix.Services;

/// <summary>Top-level metadata describing the contents of a backup archive.</summary>
public sealed class BackupManifest
{
    /// <summary>ISO-8601 UTC timestamp of the snapshot.</summary>
    public required string GeneratedAtUtc { get; init; }

    /// <summary>Always "ScrumFlix".</summary>
    public string Application { get; init; } = "ScrumFlix";

    /// <summary>Schema version of this manifest format, for forward compatibility.</summary>
    public int ManifestVersion { get; init; } = 2;

    /// <summary>Which sections this archive actually contains.</summary>
    public required BackupSections Sections { get; init; }

    /// <summary>Per-table row counts for the data section (absent tables omitted).</summary>
    public required IReadOnlyList<TableRowCount> Tables { get; init; }

    /// <summary>Table keys that errored during data serialisation and were skipped.</summary>
    public required IReadOnlyList<string> TablesErrored { get; init; }

    /// <summary>Names of stored procedures captured.</summary>
    public IReadOnlyList<string> Procedures { get; init; } = [];

    /// <summary>Names of stored functions captured.</summary>
    public IReadOnlyList<string> Functions { get; init; } = [];

    /// <summary>Names of views captured.</summary>
    public IReadOnlyList<string> Views { get; init; } = [];

    /// <summary>Names of triggers captured.</summary>
    public IReadOnlyList<string> Triggers { get; init; } = [];

    /// <summary>Total rows across all successfully serialised tables.</summary>
    public int TotalRows { get; init; }

    /// <summary>Human-readable notes (e.g. the password-exclusion and DEFINER-strip caveats).</summary>
    public IReadOnlyList<string> Notes { get; init; } = [];

    /// <summary>Flags indicating which sections are present in the archive.</summary>
    public sealed class BackupSections
    {
        /// <summary>schema/ folder with CREATE TABLE DDL.</summary>
        public bool Schema { get; init; }

        /// <summary>json/ and sql/ folders with row data.</summary>
        public bool Data { get; init; }

        /// <summary>routines/ folder includes stored procedures and functions.</summary>
        public bool StoredProcedures { get; init; }

        /// <summary>routines/ folder includes views.</summary>
        public bool Views { get; init; }

        /// <summary>routines/ folder includes triggers.</summary>
        public bool Triggers { get; init; }
    }

    /// <summary>A single table's row count in the data section.</summary>
    public sealed class TableRowCount
    {
        /// <summary>Canonical table key / SQL table name.</summary>
        public required string Table { get; init; }

        /// <summary>Row count, or -1 if the table errored during serialisation.</summary>
        public required int Rows { get; init; }
    }
}
