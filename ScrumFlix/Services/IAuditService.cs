/*
 * File:        /ScrumFlix/Services/IAuditService.cs
 * Namespace:   ScrumFlix.Services
 * Purpose:     Contract for the AuditLog write service.
 *
 *              Every security-sensitive action in the rebuild must call LogAsync.
 *              Standard ActionType values:
 *                LOGIN, LOGIN_FAILED, LOGOUT, APP_CLOSE,
 *                LOCKOUT, PASSWORD_CHANGE,
 *                CREATE, UPDATE, DELETE
 *
 *              OldValues / NewValues are JSON-serialized entity snapshots.
 *              They are null for session-level events (LOGIN, LOGOUT, etc.)
 *              and populated for entity CRUD operations.
 *
 * Phase:   2
 * Author:  ScrumFlix Rebuild Team
 * Updated: 2026-05-04
 */

namespace ScrumFlix.Services;

/// <summary>
/// Writes immutable audit records to the AuditLog table for security-sensitive actions.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Appends a new row to the AuditLog table.
    /// </summary>
    /// <param name="userId">The UserId of the user who performed the action.</param>
    /// <param name="actionType">
    /// The action category. Use the standard values defined in the rebuild spec:
    /// LOGIN, LOGIN_FAILED, LOGOUT, APP_CLOSE, LOCKOUT, PASSWORD_CHANGE,
    /// CREATE, UPDATE, DELETE.
    /// </param>
    /// <param name="tableName">
    /// The canonical table name affected (e.g., "Users", "Movies", "Ticket").
    /// Use the exact Pascal-case table name from the schema.
    /// </param>
    /// <param name="objectId">
    /// The primary key of the affected record. Null for session-level events.
    /// </param>
    /// <param name="description">A human-readable summary of the action.</param>
    /// <param name="oldValues">
    /// JSON snapshot of the entity before the action. Null for non-mutation events.
    /// </param>
    /// <param name="newValues">
    /// JSON snapshot of the entity after the action. Null for DELETE and session events.
    /// </param>
    Task LogAsync(
        int     userId,
        string  actionType,
        string  tableName,
        int?    objectId    = null,
        string? description = null,
        string? oldValues   = null,
        string? newValues   = null);
}
