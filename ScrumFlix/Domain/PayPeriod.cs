/*
 * File: /ScrumFlix/Domain/PayPeriod.cs
 * Description: Canonical PayPeriod entity — maps to the PayPeriods table in defaultdb.
 *              Net-new — no equivalent existed in the legacy application.
 *
 *              A PayPeriod defines the date range for a payroll cycle.
 *              Timesheets and Payrolls are both scoped to a PayPeriod.
 *
 *              EndDate >= StartDate is enforced at the service layer and ViewModel
 *              validation only — the live schema has no CHECK constraint on this.
 *
 *              Admin creates PayPeriods before the payroll run. The payroll engine then
 *              aggregates TimeEntries within [StartDate, EndDate] into Timesheets,
 *              and approved Timesheets into Payrolls.
 */

namespace ScrumFlix.Domain;

/// <summary>
/// A defined date range representing one payroll cycle.
/// Maps to: PayPeriods (PayPeriodId, StartDate, EndDate)
/// Service-layer rule: EndDate >= StartDate (no DB CHECK constraint exists)
/// </summary>
[Table("PayPeriods")]
public class PayPeriod
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("PayPeriodId")]
    public int PayPeriodId { get; set; }

    /// <summary>First day of the pay period (inclusive).</summary>
    [Column("StartDate")]
    [DataType(DataType.Date)]
    [Display(Name = "Start Date")]
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// Last day of the pay period (inclusive).
    /// Must be >= StartDate (enforced at the service layer; no DB CHECK constraint).
    /// </summary>
    [Column("EndDate")]
    [DataType(DataType.Date)]
    [Display(Name = "End Date")]
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Concurrency lock for payroll generation (Revision 3 Part 3C / Phase 8).
    /// Set true while a payroll run is in progress for this period so a second
    /// admin cannot start a concurrent run; reset to false in a finally block.
    /// Column added by phase3_database_prep_v1.sql Step 3 (default 0).
    /// </summary>
    [Column("IsGenerating")]
    [Display(Name = "Generating")]
    public bool IsGenerating { get; set; } = false;

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>Timesheets generated for this pay period.</summary>
    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();

    /// <summary>Payroll records generated for this pay period.</summary>
    public ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();

    // ── Computed helpers ───────────────────────────────────────────────────

    /// <summary>Number of days in the pay period.</summary>
    [NotMapped]
    public int TotalDays => EndDate.DayNumber - StartDate.DayNumber + 1;

    /// <summary>Human-readable display label for dropdowns and headers.</summary>
    [NotMapped]
    public string DisplayLabel => $"{StartDate:MMM d} – {EndDate:MMM d, yyyy}";
}
