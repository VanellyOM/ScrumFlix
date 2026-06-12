/*
 * File: /ScrumFlix/Domain/User.cs
 * Description: Canonical User entity — maps to the Users table in defaultdb.
 *
 *              CRITICAL SECURITY NOTES:
 *              - UserPassword (varchar 100) is a legacy plaintext column present in the
 *                canonical schema. It must be migrated to PasswordHash on first successful
 *                login, then nulled out. Never log or display UserPassword.
 *              - PasswordHash uses BCrypt or PBKDF2. Max 255 chars per schema (varchar(255)).
 *              - FailedAccessCount and LockoutEndUtc drive the lockout subsystem.
 *              - MustChangePassword forces a redirect to ChangePassword on login.
 *              - Every login attempt (success or failure) writes to AuditLog.
 *
 *              Seeded accounts:
 *                UserId=1  UserName="a1"  RoleId=1 (Admin)
 *                UserId=2  UserName="e1"  RoleId=3 (Employee)
 */

namespace ScrumFlix.Domain;

/// <summary>
/// A system user account bound to an employee record.
/// Maps to: Users (UserId, EmployeeId, UserName, UserPassword, PasswordHash,
///          IsActive, MustChangePassword, PasswordChangedUtc, LastLoginUtc,
///          FailedAccessCount, LockoutEndUtc, RoleId)
/// </summary>
[Table("Users")]
public class User
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("UserId")]
    public int UserId { get; set; }

    /// <summary>
    /// The employee this account belongs to.
    /// One-to-one: each Employee may have at most one User account.
    /// </summary>
    [Column("EmployeeId")]
    public int EmployeeId { get; set; }

    /// <summary>Login username. Must be unique across all accounts.</summary>
    [Required]
    [MaxLength(100)]
    [Column("UserName")]
    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Legacy plaintext password column. Present in canonical schema for migration purposes only.
    /// AuthService must hash this on first successful login and set to null.
    /// NEVER display, log, or transmit this value.
    ///
    /// UserPassword is NOT NULL in the live schema. On post-migration write, always
    /// assign string.Empty — never null. AuthService enforces this on both
    /// LoginAsync and ChangePasswordAsync paths.
    /// </summary>
    [MaxLength(100)]
    [Column("UserPassword")]
    public string? UserPassword { get; set; }

    /// <summary>
    /// BCrypt or PBKDF2 password hash. Null until the account's first login triggers migration
    /// from the plaintext UserPassword column.
    /// </summary>
    [MaxLength(255)]
    [Column("PasswordHash")]
    public string? PasswordHash { get; set; }

    /// <summary>Whether this account can log in. Admins can deactivate accounts here.</summary>
    [Column("IsActive")]
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When true, the user is redirected to ChangePassword immediately after login.
    /// Set to true on account creation and after admin-initiated password resets.
    /// </summary>
    [Column("MustChangePassword")]
    [Display(Name = "Must Change Password")]
    public bool MustChangePassword { get; set; } = false;

    /// <summary>UTC timestamp of the last successful password change.</summary>
    [Column("PasswordChangedUtc")]
    [Display(Name = "Password Changed (UTC)")]
    public DateTime? PasswordChangedUtc { get; set; }

    /// <summary>UTC timestamp of the last successful login. Updated on each successful auth.</summary>
    [Column("LastLoginUtc")]
    [Display(Name = "Last Login (UTC)")]
    public DateTime? LastLoginUtc { get; set; }

    /// <summary>
    /// Number of consecutive failed login attempts since the last successful login.
    /// Reset to 0 on successful login. When it reaches the configured threshold,
    /// LockoutEndUtc is set to lock the account.
    /// </summary>
    [Column("FailedAccessCount")]
    [Display(Name = "Failed Access Count")]
    public int FailedAccessCount { get; set; } = 0;

    /// <summary>
    /// UTC timestamp when the account lockout expires. Null means not locked.
    /// AuthService checks this before allowing login attempts.
    /// </summary>
    [Column("LockoutEndUtc")]
    [Display(Name = "Lockout End (UTC)")]
    public DateTime? LockoutEndUtc { get; set; }

    /// <summary>The role controlling this user's authorization level.</summary>
    [Column("RoleId")]
    public int RoleId { get; set; }

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>The employee record this account is bound to.</summary>
    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    /// <summary>The authorization role for this account.</summary>
    [ForeignKey(nameof(RoleId))]
    public Role? Role { get; set; }

    /// <summary>Audit log entries written for this user's actions.</summary>
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    /// <summary>Tickets this user processed at point of sale.</summary>
    public ICollection<Ticket> TicketsSold { get; set; } = new List<Ticket>();

    /// <summary>Concession sales this user processed.</summary>
    public ICollection<ConcessionSale> ConcessionSales { get; set; } = new List<ConcessionSale>();

    /// <summary>Timesheets approved by this user (manager role).</summary>
    public ICollection<Timesheet> ApprovedTimesheets { get; set; } = new List<Timesheet>();

    // ── Computed helpers ───────────────────────────────────────────────────

    /// <summary>Returns true if the account is currently locked out.</summary>
    [NotMapped]
    public bool IsLockedOut => LockoutEndUtc.HasValue && LockoutEndUtc.Value > DateTime.UtcNow;

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>Schedule assignments linked to this user account.</summary>
    public ICollection<ScheduleAssignment> ScheduleAssignments { get; set; } = new List<ScheduleAssignment>();
}