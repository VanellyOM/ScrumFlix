/*
 * File: /ScrumFlix/Domain/Timesheet.cs
 * Description: Canonical Timesheet entity — maps to the Timesheets table in defaultdb.
 *              Net-new — no equivalent existed in the legacy application.
 *
 *              A Timesheet is the aggregated summary of an employee's TimeEntries for a
 *              specific PayPeriod. The payroll engine reads TotalHours from approved
 *              timesheets to compute GrossPay = Employee.PayRate × TotalHours.
 *
 *              Approval workflow:
 *                1. Timesheet is generated (Approved=false, ApprovedByUserId=null).
 *                2. Manager reviews and approves: sets Approved=true, ApprovedByUserId=their UserId.
 *                3. Payroll engine only processes timesheets where Approved=true.
 *
 *              The canonical schema includes a CHECK constraint: TotalHours >= 0.
 *
 *              Schema update: LocationId INT NOT NULL FK → Location added to match
 *              the canonical Timesheets table definition.
 */

namespace ScrumFlix.Domain;

/// <summary>
/// A pay-period summary of an employee's total hours worked, subject to manager approval.
/// Maps to: Timesheets (TimesheetId, EmployeeId, PayPeriodId, LocationId, TotalHours, Approved, ApprovedByUserId)
/// DB constraint: TotalHours >= 0
/// </summary>
[Table("Timesheets")]
public class Timesheet
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("TimesheetId")]
    public int TimesheetId { get; set; }

    /// <summary>The employee whose hours this timesheet summarizes.</summary>
    [Column("EmployeeId")]
    public int EmployeeId { get; set; }

    /// <summary>The pay period this timesheet covers.</summary>
    [Column("PayPeriodId")]
    public int PayPeriodId { get; set; }

    /// <summary>The location this timesheet is associated with.</summary>
    [Column("LocationId")]
    public int LocationId { get; set; }

    /// <summary>
    /// Total hours worked during the pay period.
    /// Summed from all closed TimeEntries for this employee within the PayPeriod date range.
    /// Decimal(5,2) — supports up to 999.99 hours.
    /// </summary>
    [Column("TotalHours")]
    [Display(Name = "Total Hours")]
    [Range(0, 999.99)]
    public decimal TotalHours { get; set; } = 0.00m;

    /// <summary>
    /// Whether a manager has approved this timesheet for payroll processing.
    /// Only approved timesheets are included in payroll runs.
    /// </summary>
    [Column("Approved")]
    [Display(Name = "Approved")]
    public bool Approved { get; set; } = false;

    /// <summary>
    /// The Users.UserId of the manager who approved this timesheet.
    /// Null until approved. Must reference a user with RoleId = 1 (Admin) or 2 (Manager).
    /// </summary>
    [Column("ApprovedByUserId")]
    [Display(Name = "Approved By")]
    public int? ApprovedByUserId { get; set; }

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>The employee this timesheet belongs to.</summary>
    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    /// <summary>The pay period this timesheet covers.</summary>
    [ForeignKey(nameof(PayPeriodId))]
    public PayPeriod? PayPeriod { get; set; }

    /// <summary>The location this timesheet is associated with.</summary>
    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }

    /// <summary>The user (manager/admin) who approved this timesheet.</summary>
    [ForeignKey(nameof(ApprovedByUserId))]
    public User? ApprovedByUser { get; set; }
}
