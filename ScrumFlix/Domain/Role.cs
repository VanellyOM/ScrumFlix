/*
 * File: /ScrumFlix/Domain/Role.cs
 * Description: Canonical Role entity — maps to the Roles table in defaultdb.
 *              Previously represented as EmployeeRole with a phantom employee_roles table.
 *              The canonical schema uses only RoleId and RoleName (no RoleDescription).
 *
 *              Seeded roles: 1=Admin, 2=Manager, 3=Employee
 */

namespace ScrumFlix.Domain;

/// <summary>
/// A system role controlling user authorization level.
/// Maps to: Roles (RoleId, RoleName)
/// Seeded values: 1=Admin, 2=Manager, 3=Employee
/// </summary>
[Table("Roles")]
public class Role
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("RoleId")]
    public int RoleId { get; set; }

    /// <summary>Role name (e.g., "Admin", "Manager", "Employee").</summary>
    [MaxLength(30)]
    [Column("RoleName")]
    [Display(Name = "Role")]
    public string? RoleName { get; set; }

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>Users assigned to this role.</summary>
    public ICollection<User> Users { get; set; } = new List<User>();

    /// <summary>Shifts requiring this role.</summary>
    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
}
