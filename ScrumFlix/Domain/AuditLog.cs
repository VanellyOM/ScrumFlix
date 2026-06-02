/*
 * File: /ScrumFlix/Domain/AuditLog.cs
 * Description: Canonical AuditLog entity — maps to the AuditLog table in defaultdb.
 *
 *              AuditLog entries are written by IAuditService for every security-sensitive
 *              action. The 10 seeded rows in the canonical DB show real usage patterns:
 *                - ActionType: LOGIN, LOGOUT, APP_CLOSE
 *                - TableName:  Users
 *                - ObjectId:   the UserId of the subject
 *                - Description: human-readable summary
 *
 *              Additional ActionTypes to implement in the rebuild:
 *                LOGIN_FAILED, CREATE, UPDATE, DELETE, PASSWORD_CHANGE, LOCKOUT
 *
 *              OldValues and NewValues store JSON snapshots of entity state for UPDATE/DELETE.
 *              They are nullable — LOGIN/LOGOUT entries leave them null.
 */

namespace ScrumFlix.Domain;

/// <summary>
/// An immutable audit record capturing a security-sensitive action performed by a user.
/// Maps to: AuditLog (AuditLogId, UserId, ActionType, TableName, ObjectId,
///          ActionTime, OldValues, NewValues, Description)
/// </summary>
[Table("AuditLog")]
public class AuditLog
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("AuditLogId")]
    public int AuditLogId { get; set; }

    /// <summary>The user who performed the action.</summary>
    [Column("UserId")]
    public int UserId { get; set; }

    /// <summary>
    /// Type of action performed.
    /// Standard values: LOGIN, LOGIN_FAILED, LOGOUT, APP_CLOSE,
    ///                  CREATE, UPDATE, DELETE, PASSWORD_CHANGE, LOCKOUT
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("ActionType")]
    [Display(Name = "Action")]
    public string ActionType { get; set; } = string.Empty;

    /// <summary>The database table the action was performed against (e.g., "Users", "Movies").</summary>
    [Required]
    [MaxLength(100)]
    [Column("TableName")]
    [Display(Name = "Table")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// The primary key of the record affected by the action.
    /// Null for session-level actions (LOGIN, LOGOUT, APP_CLOSE).
    /// </summary>
    [Column("ObjectId")]
    [Display(Name = "Record ID")]
    public int? ObjectId { get; set; }

    /// <summary>UTC timestamp when the action occurred. Defaults to current time on insert.</summary>
    [Column("ActionTime")]
    [Display(Name = "Time (UTC)")]
    public DateTime ActionTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// JSON snapshot of the entity's state before the action.
    /// Populated for UPDATE and DELETE actions. Null for all others.
    /// </summary>
    [Column("OldValues")]
    [Display(Name = "Previous Values")]
    public string? OldValues { get; set; }

    /// <summary>
    /// JSON snapshot of the entity's state after the action.
    /// Populated for CREATE and UPDATE actions. Null for all others.
    /// </summary>
    [Column("NewValues")]
    [Display(Name = "New Values")]
    public string? NewValues { get; set; }

    /// <summary>Human-readable description of the action (e.g., "User 'a1' logged in").</summary>
    [MaxLength(255)]
    [Column("Description")]
    public string? Description { get; set; }

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>The user who performed the audited action.</summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
