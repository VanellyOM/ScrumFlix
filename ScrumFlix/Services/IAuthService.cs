/*
 * File:        /ScrumFlix/Services/IAuthService.cs
 * Namespace:   ScrumFlix.Services
 * Purpose:     Contract for the custom session-based authentication service.
 *
 *              ScrumFlix does NOT use ASP.NET Core Identity. Authentication is
 *              custom-built against the canonical Users table using BCrypt
 *              password hashing with a plaintext-to-hash migration path.
 *
 *              All implementations must:
 *                - Explicitly block login for the web.sales system account
 *                - Enforce account lockout via FailedAccessCount / LockoutEndUtc
 *                - Migrate plaintext UserPassword to PasswordHash on first login
 *                - Write AuditLog entries for every auth event (via IAuditService)
 *
 * Phase:   2
 * Author:  ScrumFlix Rebuild Team
 * Updated: 2026-05-04
 */

namespace ScrumFlix.Services;

/// <summary>
/// Provides session-based authentication against the canonical Users table.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Attempts to authenticate a user by username and password.
    /// Handles BCrypt verification, plaintext-to-hash migration, lockout
    /// enforcement, and AuditLog writes for LOGIN and LOGIN_FAILED events.
    /// </summary>
    /// <param name="userName">The submitted username.</param>
    /// <param name="password">The submitted plaintext password.</param>
    /// <returns>
    /// A <see cref="LoginResult"/> indicating success or the specific failure reason.
    /// </returns>
    Task<LoginResult> LoginAsync(string userName, string password);

    /// <summary>
    /// Clears the current user's session and writes a LOGOUT AuditLog entry.
    /// Safe to call even if no user is currently authenticated.
    /// </summary>
    /// <param name="userId">The UserId of the session being ended (for audit log).</param>
    Task LogoutAsync(int userId);

    /// <summary>
    /// Changes the authenticated user's password. Validates the current password,
    /// hashes the new password via BCrypt, sets PasswordChangedUtc, clears
    /// MustChangePassword, and writes a PASSWORD_CHANGE AuditLog entry.
    /// </summary>
    /// <param name="userId">The UserId of the account being updated.</param>
    /// <param name="currentPassword">The user's current password (for re-verification).</param>
    /// <param name="newPassword">The new plaintext password to hash and store.</param>
    /// <returns>True if the change succeeded; false if current password verification failed.</returns>
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
}
