/*
 * File:        /ScrumFlix/Services/AuditService.cs
 * Namespace:   ScrumFlix.Services
 * Purpose:     Concrete implementation of IAuditService.
 *
 *              Writes AuditLog rows directly via AppDbContext.
 *              Each call is a self-contained SaveChangesAsync — audit writes
 *              must NOT be rolled back if the surrounding business transaction
 *              fails. For that reason, AuditService intentionally does NOT
 *              participate in any caller's transaction scope.
 *
 *              Failures are logged at Warning level but do NOT propagate as
 *              exceptions — an audit write failure must never block a user action.
 *
 * Phase:   2
 * Author:  ScrumFlix Rebuild Team
 * Updated: 2026-05-04
 */


namespace ScrumFlix.Services;

/// <summary>
/// Writes immutable audit records to the AuditLog table.
/// Registered as scoped in Program.cs.
/// </summary>
public sealed class AuditService : IAuditService
{
    private readonly AppDbContext           _db;
    private readonly ILogger<AuditService>  _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AuditService"/>.
    /// </summary>
    public AuditService(AppDbContext db, ILogger<AuditService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task LogAsync(
        int     userId,
        string  actionType,
        string  tableName,
        int?    objectId    = null,
        string? description = null,
        string? oldValues   = null,
        string? newValues   = null)
    {
        try
        {
            var entry = new AuditLog
            {
                UserId      = userId,
                ActionType  = actionType,
                TableName   = tableName,
                ObjectId    = objectId,
                ActionTime  = DateTime.UtcNow,
                OldValues   = oldValues,
                NewValues   = newValues,
                Description = description,
            };

            _db.AuditLogs.Add(entry);
            await _db.SaveChangesAsync();

            _logger.LogDebug(
                "AuditLog written — UserId: {UserId} | Action: {ActionType} | " +
                "Table: {TableName} | ObjectId: {ObjectId}",
                userId, actionType, tableName, objectId);
        }
        catch (Exception ex)
        {
            // Audit failures must never propagate — log and continue.
            _logger.LogWarning(ex,
                "AuditLog write failed for UserId {UserId} / {ActionType} / {TableName}. " +
                "The user action will proceed but the audit record was not stored.",
                userId, actionType, tableName);
        }
    }
}
