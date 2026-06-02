/*
 * File: /ScrumFlix/Domain/Shift.cs
 * Description: Canonical Shift entity — maps to the Shifts table in defaultdb.
 *              Net-new — no equivalent existed in the legacy application.
 *
 *              The canonical schema includes a CHECK constraint: EndTime > StartTime.
 *              This must be enforced at both the service layer and via a ViewModel validation
 *              attribute — EF Core does not enforce CHECK constraints on the client side.
 *
 *              8 shifts are seeded for May 1–2 2026 across Locations 1 and 2,
 *              for RoleId 2 (Manager) and 3 (Employee).
 */

namespace ScrumFlix.Domain;

/// <summary>
/// A scheduled work shift at a specific theater location, requiring a specific role.
/// Maps to: Shifts (ShiftId, StartTime, EndTime, RoleId, LocationId)
/// DB constraint: EndTime > StartTime (enforced at service layer too).
/// </summary>
[Table("Shifts")]
public class Shift
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("ShiftId")]
    public int ShiftId { get; set; }

    /// <summary>Date and time the shift begins.</summary>
    [Column("StartTime")]
    [Display(Name = "Start Time")]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Date and time the shift ends.
    /// Must be strictly greater than StartTime (canonical CHECK constraint).
    /// </summary>
    [Column("EndTime")]
    [Display(Name = "End Time")]
    public DateTime EndTime { get; set; }

    /// <summary>The role required to fill this shift (Admin=1, Manager=2, Employee=3).</summary>
    [Column("RoleId")]
    public int RoleId { get; set; }

    /// <summary>The theater location where this shift takes place.</summary>
    [Column("LocationId")]
    public int LocationId { get; set; }

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>The role required for this shift.</summary>
    [ForeignKey(nameof(RoleId))]
    public Role? Role { get; set; }

    /// <summary>The location this shift is at.</summary>
    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }

    /// <summary>Employee assignments for this shift.</summary>
    public ICollection<ScheduleAssignment> ScheduleAssignments { get; set; } = new List<ScheduleAssignment>();

    // ── Computed helpers ───────────────────────────────────────────────────

    /// <summary>Duration of the shift.</summary>
    [NotMapped]
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>Duration formatted as a readable string (e.g., "4h 0m").</summary>
    [NotMapped]
    public string FormattedDuration =>
        $"{(int)Duration.TotalHours}h {Duration.Minutes}m";
}
