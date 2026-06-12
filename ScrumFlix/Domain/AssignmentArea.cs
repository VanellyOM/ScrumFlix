/*
 * File: /ScrumFlix/Domain/AssignmentArea.cs
 * Description: Canonical AssignmentArea entity — maps to the AssignmentAreas table in defaultdb.
 *              Net-new (Phase 3, Revision 3 Part 3A) — normalized lookup that replaces the raw
 *              AssignmentName varchar previously stored on ScheduleAssignments.
 *
 *              Why normalized: renaming an area becomes a data change instead of a code
 *              deploy, eliminates casing/spelling drift ("Box Office" vs "box office"),
 *              and enables future per-area features (capacity limits, reporting).
 *
 *              Seven canonical rows are seeded by phase3_database_prep_v1.sql:
 *                1 Box Office, 2 Concessions, 3 Usher, 4 Projection, 5 Cleaning,
 *                6 Manager On Duty, 7 Operations Lead.
 *
 *              Soft delete only: areas referenced by assignments are protected by an
 *              ON DELETE RESTRICT FK — set IsActive = false to retire an area instead.
 */

namespace ScrumFlix.Domain;

/// <summary>
/// A normalized lookup value for schedule assignment areas (e.g., "Box Office").
/// Maps to: AssignmentAreas (AssignmentAreaId, AreaName, IsActive)
/// DB constraint: UNIQUE (AreaName)
/// </summary>
[Table("AssignmentAreas")]
public class AssignmentArea
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("AssignmentAreaId")]
    public int AssignmentAreaId { get; set; }

    /// <summary>Display name of the area. Unique across the table.</summary>
    [Required]
    [MaxLength(50)]
    [Column("AreaName")]
    [Display(Name = "Area")]
    public string AreaName { get; set; } = string.Empty;

    /// <summary>
    /// Soft-delete flag. Inactive areas are hidden from the assignment form
    /// dropdown but remain valid on historical assignments.
    /// </summary>
    [Column("IsActive")]
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>Schedule assignments referencing this area.</summary>
    public ICollection<ScheduleAssignment> ScheduleAssignments { get; set; } = new List<ScheduleAssignment>();
}
