/*
 * File: /ScrumFlix/Domain/PayStub.cs
 * Description: Canonical PayStub entity — maps to the PayStubs table in defaultdb.
 *              Net-new — no equivalent existed in the legacy application.
 *
 *              A PayStub is issued after a payroll run completes. One PayStub is created
 *              per Payroll record. It represents the official pay statement presented to
 *              the employee and accessible in the Employee Area.
 *
 *              The stub's display view should pull Payroll + Employee + PayPeriod data
 *              via navigation properties to render a complete pay statement.
 */

namespace ScrumFlix.Domain;

/// <summary>
/// An issued pay statement linked to a completed payroll record.
/// Maps to: PayStubs (PayStubId, PayrollId, IssueDate)
/// </summary>
[Table("PayStubs")]
public class PayStub
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("PayStubId")]
    public int PayStubId { get; set; }

    /// <summary>The payroll record this stub was generated from.</summary>
    [Column("PayrollId")]
    public int PayrollId { get; set; }

    /// <summary>The date and time this pay stub was issued.</summary>
    [Column("IssueDate")]
    [Display(Name = "Issue Date")]
    public DateTime IssueDate { get; set; }

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>
    /// The payroll record backing this stub.
    /// Include Payroll.Employee and Payroll.PayPeriod when rendering the stub view.
    /// </summary>
    [ForeignKey(nameof(PayrollId))]
    public Payroll? Payroll { get; set; }
}
