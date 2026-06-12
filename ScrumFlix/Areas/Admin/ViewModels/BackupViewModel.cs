// ============================================================================
// BackupViewModel — ADD TO END of AdminManageViewModels.cs
//
// Or place in a new file:
//   /ScrumFlix/Areas/Admin/ViewModels/BackupViewModel.cs
//   namespace ScrumFlix.Areas.Admin.ViewModels;
// ============================================================================

namespace ScrumFlix.Areas.Admin.ViewModels;

// ── Backup ────────────────────────────────────────────────────────────────────

/// <summary>
/// ViewModel for the Admin Database Backup page.
/// Carries the table checklist state posted back from the form,
/// and the available tables list re-populated by the controller.
/// </summary>
public class BackupViewModel
{
    /// <summary>
    /// Full table registry — populated by the controller on GET and re-populated
    /// on POST (not round-tripped in the form). Read-only in the view.
    /// </summary>
    public IReadOnlyList<BackupTableDescriptor> AvailableTables { get; set; } =
        Array.Empty<BackupTableDescriptor>();

    /// <summary>
    /// Table keys checked by the admin in the checklist.
    /// Bound from the posted form checkboxes. Null/empty means "all non-excluded".
    /// </summary>
    public List<string> SelectedTableKeys { get; set; } = new();

    /// <summary>
    /// When true, the controller emails the .zip to the Email:AdminTo address
    /// in addition to returning it as a file download.
    /// </summary>
    public bool SendEmail { get; set; } = false;

    // ── Section toggles (Phase 4+ schema-backup upgrade) ───────────────────
    // Defaults below describe a Full disaster-recovery backup. The admin can
    // narrow this to data-only, schema-only, or any custom combination.

    /// <summary>Include CREATE TABLE DDL (indexes/constraints inline) for selected tables.</summary>
    public bool IncludeSchema { get; set; } = true;

    /// <summary>Include table rows as JSON files and batched INSERT scripts.</summary>
    public bool IncludeData { get; set; } = true;

    /// <summary>Include CREATE PROCEDURE / CREATE FUNCTION DDL for all stored routines.</summary>
    public bool IncludeStoredProcedures { get; set; } = true;

    /// <summary>Include CREATE VIEW DDL for all views.</summary>
    public bool IncludeViews { get; set; } = true;

    /// <summary>Include CREATE TRIGGER DDL for all triggers.</summary>
    public bool IncludeTriggers { get; set; } = true;

    /// <summary>Prepend DROP ... IF EXISTS before each CREATE so a restore replaces cleanly.</summary>
    public bool DropBeforeCreate { get; set; } = true;
}
