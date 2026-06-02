/*
 * File: /ScrumFlix/Domain/Payroll.cs
 * Description: Canonical Payroll entity — maps to the Payrolls table in defaultdb.
 *              Net-new — no equivalent existed in the legacy application.
 *
 *              A Payroll record is created by the payroll engine for each employee
 *              who has an approved Timesheet in the given PayPeriod.
 *
 *              GrossPay calculation:
 *                GrossPay = Employee.PayRate × Timesheet.TotalHours
 *
 *              The canonical schema includes a CHECK constraint: GrossPay >= 0.
 *              A PayStub record is created for each Payroll after the payroll run completes.
 *
 *              Schema update: LocationId INT NOT NULL FK → Location added to match
 *              the canonical Payrolls table definition.
 */

namespace ScrumFlix.Domain;

/// <summary>
/// A computed gross pay record for one employee for one pay period.
/// Maps to: Payrolls (PayrollId, EmployeeId, PayPeriodId, LocationId, GrossPay)
/// DB constraint: GrossPay >= 0
/// </summary>
[Table("Payrolls")]
public class Payroll
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("PayrollId")]
    public int PayrollId { get; set; }

    /// <summary>The employee being paid.</summary>
    [Column("EmployeeId")]
    public int EmployeeId { get; set; }

    /// <summary>The pay period this payroll record covers.</summary>
    [Column("PayPeriodId")]
    public int PayPeriodId { get; set; }

    /// <summary>The location this payroll record is associated with.</summary>
    [Column("LocationId")]
    public int LocationId { get; set; }

    /// <summary>
    /// Gross pay for this employee for this period.
    /// Computed as Employee.PayRate × Timesheet.TotalHours by PayrollService.
    /// Canonical CHECK constraint: GrossPay >= 0.
    /// </summary>
    [Column("GrossPay")]
    [DataType(DataType.Currency)]
    [Display(Name = "Gross Pay")]
    [Range(0, double.MaxValue)]
    public decimal GrossPay { get; set; } = 0.00m;

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>The employee receiving this payroll.</summary>
    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    /// <summary>The pay period this payroll covers.</summary>
    [ForeignKey(nameof(PayPeriodId))]
    public PayPeriod? PayPeriod { get; set; }

    /// <summary>The location this payroll record is associated with.</summary>
    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }

    /// <summary>The pay stub issued for this payroll record.</summary>
    public PayStub? PayStub { get; set; }
}
