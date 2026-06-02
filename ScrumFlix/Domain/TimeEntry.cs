/*
 * File: /ScrumFlix/Domain/TimeEntry.cs
 * Description: Canonical TimeEntry entity — maps to the TimeEntries table in defaultdb.
 *              Net-new — no equivalent existed in the legacy application.
 *
 *              Represents a single clock-in/clock-out pair for an employee.
 *              ClockOut is nullable — a null ClockOut means the employee is currently clocked in.
 *              Only one open TimeEntry (ClockOut IS NULL) should exist per employee at a time;
 *              the time clock service must enforce this.
 *
 *              The canonical schema includes a CHECK constraint:
 *                ClockOut IS NULL OR ClockOut >= ClockIn
 *              This must also be enforced at the service layer.
 *
 *              TimeEntries are aggregated per PayPeriod into Timesheets by the payroll engine.
 *
 *              Schema update: LocationId INT NOT NULL FK → Location added to match
 *              the canonical TimeEntries table definition.
 */

namespace ScrumFlix.Domain;

/// <summary>
/// A single clock-in / clock-out record for an employee.
/// Maps to: TimeEntries (TimeEntryId, EmployeeId, LocationId, ClockIn, ClockOut)
/// DB constraint: ClockOut IS NULL OR ClockOut >= ClockIn
/// </summary>
[Table("TimeEntries")]
public class TimeEntry
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("TimeEntryId")]
    public int TimeEntryId { get; set; }

    /// <summary>The employee who clocked in/out.</summary>
    [Column("EmployeeId")]
    public int EmployeeId { get; set; }

    /// <summary>The location where the employee clocked in/out.</summary>
    [Column("LocationId")]
    public int LocationId { get; set; }

    /// <summary>When the employee clocked in.</summary>
    [Column("ClockIn")]
    [Display(Name = "Clock In")]
    public DateTime ClockIn { get; set; }

    /// <summary>
    /// When the employee clocked out.
    /// Null indicates the employee is still clocked in.
    /// Must be >= ClockIn when set (canonical CHECK constraint).
    /// </summary>
    [Column("ClockOut")]
    [Display(Name = "Clock Out")]
    public DateTime? ClockOut { get; set; }

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>The employee this time entry belongs to.</summary>
    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    /// <summary>The location where this time entry was recorded.</summary>
    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }

    // ── Computed helpers ───────────────────────────────────────────────────

    /// <summary>True when the employee has not yet clocked out.</summary>
    [NotMapped]
    public bool IsOpen => ClockOut is null;

    /// <summary>
    /// Hours worked on this entry. Null if still clocked in.
    /// Used by the payroll engine to sum into Timesheet.TotalHours.
    /// </summary>
    [NotMapped]
    public decimal? HoursWorked =>
        ClockOut.HasValue
            ? (decimal)(ClockOut.Value - ClockIn).TotalHours
            : null;
}
