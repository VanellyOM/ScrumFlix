/*
 * File: /ScrumFlix/Domain/ScheduleAssignment.cs
 * Description: Canonical ScheduleAssignment entity — maps to the ScheduleAssignments table in defaultdb.
 *              Net-new — no equivalent existed in the legacy application.
 *
 *              Links a User (login account) to a Shift, and optionally to a specific Showtime.
 *              ShowtimeId is nullable — general shift assignments do not require a showtime.
 *
 *              CORRECTION (2026-05-08):
 *                The DB column is UserId (FK → Users.UserId), NOT EmployeeId (FK → Employees).
 *                The model previously declared EmployeeId/Employee, which would map to the wrong
 *                column and the wrong table. Updated to match the canonical schema exactly.
 *
 *              PHASE 3 (2026-06-11):
 *                AssignmentName (raw varchar) replaced by AssignmentAreaId FK → AssignmentAreas.
 *                Migration: phase3_database_prep_v1.sql Step 2. Display name now comes from
 *                AssignmentArea.AreaName via the navigation property.
 */

namespace ScrumFlix.Domain;

/// <summary>
/// An assignment linking a user account to a scheduled shift, optionally tied to a showtime.
/// Maps to: ScheduleAssignments (AssignmentId, UserId, AssignmentAreaId, ShiftId, ShowtimeId)
/// </summary>
[Table("ScheduleAssignments")]
public class ScheduleAssignment
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("AssignmentId")]
    public int AssignmentId { get; set; }

    /// <summary>
    /// FK → AssignmentAreas.AssignmentAreaId. Replaces the raw AssignmentName
    /// varchar (dropped in the Phase 3 migration) with a normalized lookup.
    /// </summary>
    [Column("AssignmentAreaId")]
    [Display(Name = "Assignment Area")]
    public int AssignmentAreaId { get; set; }

    /// <summary>
    /// The Users.UserId of the user assigned to this shift.
    /// FK → Users.UserId (not Employees.EmployeeId — these are different tables).
    /// </summary>
    [Column("UserId")]
    public int UserId { get; set; }

    /// <summary>The shift this assignment covers.</summary>
    [Column("ShiftId")]
    public int ShiftId { get; set; }

    /// <summary>
    /// Optional showtime this assignment is tied to.
    /// Null for general assignments (e.g., concessions, janitorial).
    /// Set when the user is specifically assigned to support a screening.
    /// </summary>
    [Column("ShowtimeId")]
    public int? ShowtimeId { get; set; }

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>The user account assigned to this shift.</summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>The normalized assignment area (Box Office, Concessions, …).</summary>
    [ForeignKey(nameof(AssignmentAreaId))]
    public AssignmentArea? AssignmentArea { get; set; }

    /// <summary>The shift being covered.</summary>
    [ForeignKey(nameof(ShiftId))]
    public Shift? Shift { get; set; }

    /// <summary>The optional showtime this user is supporting.</summary>
    [ForeignKey(nameof(ShowtimeId))]
    public Showtime? Showtime { get; set; }
}
