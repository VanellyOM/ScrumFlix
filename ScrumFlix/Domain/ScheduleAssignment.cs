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
 */

namespace ScrumFlix.Domain;

/// <summary>
/// An assignment linking a user account to a scheduled shift, optionally tied to a showtime.
/// Maps to: ScheduleAssignments (AssignmentId, AssignmentName, UserId, ShiftId, ShowtimeId)
/// </summary>
[Table("ScheduleAssignments")]
public class ScheduleAssignment
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("AssignmentId")]
    public int AssignmentId { get; set; }

    /// <summary>A descriptive label for this assignment (e.g., "Box Office", "Concession Stand").</summary>
    [Required]
    [MaxLength(50)]
    [Column("AssignmentName")]
    [Display(Name = "Assignment")]
    public string AssignmentName { get; set; } = string.Empty;

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

    /// <summary>The shift being covered.</summary>
    [ForeignKey(nameof(ShiftId))]
    public Shift? Shift { get; set; }

    /// <summary>The optional showtime this user is supporting.</summary>
    [ForeignKey(nameof(ShowtimeId))]
    public Showtime? Showtime { get; set; }
}
