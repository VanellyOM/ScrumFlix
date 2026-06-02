/*
 * File: /ScrumFlix/Domain/Employee.cs
 * Description: Canonical Employee entity — maps to the Employees table in defaultdb.
 *
 *              KEY DIFFERENCES from legacy model:
 *              - Address is a single varchar(200), not split city/state/zip fields.
 *              - DOB is the canonical column name (not employee_dob).
 *              - Phone is varchar(20), Email is varchar(100).
 *              - PayRate is decimal(10,2), nullable.
 *              - LocationId FK references Location table.
 *              - No EmployeeStartDate or EmployeeEndDate — not in canonical schema.
 */

namespace ScrumFlix.Domain;

/// <summary>
/// A ScrumFlix theater employee.
/// Maps to: Employees (EmployeeId, FirstName, MiddleName, LastName, DOB, Phone, Email, Address, PayRate, LocationId)
/// </summary>
[Table("Employees")]
public class Employee
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("EmployeeId")]
    public int EmployeeId { get; set; }

    /// <summary>Employee's first name.</summary>
    [Required]
    [MaxLength(50)]
    [Column("FirstName")]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Employee's optional middle name.</summary>
    [MaxLength(50)]
    [Column("MiddleName")]
    [Display(Name = "Middle Name")]
    public string? MiddleName { get; set; }

    /// <summary>Employee's last name.</summary>
    [Required]
    [MaxLength(50)]
    [Column("LastName")]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Date of birth.</summary>
    [Column("DOB")]
    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateOnly DOB { get; set; }

    /// <summary>Contact phone number.</summary>
    [Required]
    [MaxLength(20)]
    [Column("Phone")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>Work email address. Must be unique across all employees.</summary>
    [Required]
    [MaxLength(100)]
    [EmailAddress]
    [Column("Email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Full mailing address as a single field (e.g., "123 Main St, City NY 12345").
    /// Maximum 200 characters per canonical schema.
    /// </summary>
    [MaxLength(200)]
    [Column("Address")]
    public string? Address { get; set; }

    /// <summary>Hourly pay rate. Used to calculate GrossPay in payroll.</summary>
    [Column("PayRate")]
    [DataType(DataType.Currency)]
    [Display(Name = "Pay Rate")]
    public decimal? PayRate { get; set; }

    /// <summary>The location this employee is primarily assigned to.</summary>
    [Column("LocationId")]
    public int? LocationId { get; set; }

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>Primary location assignment.</summary>
    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }

    /// <summary>The system user account linked to this employee.</summary>
    public User? User { get; set; }

    /// <summary>Clock-in/out time entries for this employee.</summary>
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();

    /// <summary>Timesheets generated for this employee.</summary>
    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();

    /// <summary>Payroll records for this employee.</summary>
    public ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();

    // NOTE: ScheduleAssignments are linked to Users.UserId, not EmployeeId.
    // Use User.ScheduleAssignments to navigate assignments for this employee's account.

    // ── Computed helpers ───────────────────────────────────────────────────

    /// <summary>Full name for display purposes.</summary>
    [NotMapped]
    public string FullName => string.IsNullOrWhiteSpace(MiddleName)
        ? $"{FirstName} {LastName}"
        : $"{FirstName} {MiddleName} {LastName}";
}
