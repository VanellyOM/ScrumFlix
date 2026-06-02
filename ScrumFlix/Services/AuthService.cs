/*
 * File:        /ScrumFlix/Services/AuthService.cs
 * Namespace:   ScrumFlix.Services
 * Purpose:     Custom session-based authentication service for ScrumFlix.
 *
 *              AUTHENTICATION FLOW (LoginAsync):
 *                1. Block web.sales system account unconditionally.
 *                2. Look up User by UserName. Return InvalidCredentials if not found.
 *                3. Check IsActive. Return AccountInactive if false.
 *                4. Check LockoutEndUtc. Return LockedOut if still in effect.
 *                5. Verify password:
 *                   a. If PasswordHash is set → BCrypt.Verify(submitted, hash).
 *                   b. If PasswordHash is null → compare UserPassword (plaintext legacy).
 *                      On match: hash it, store in PasswordHash, null out UserPassword.
 *                6. On failure: increment FailedAccessCount. Lock if >= threshold.
 *                   Write LOGIN_FAILED to AuditLog.
 *                7. On success: reset FailedAccessCount, update LastLoginUtc.
 *                   Write LOGIN to AuditLog.
 *                   If MustChangePassword → return MustChangePassword outcome.
 *
 *              LOCKOUT POLICY:
 *                - Threshold: 5 consecutive failures.
 *                - Duration:  15 minutes.
 *                - Reset on:  any successful login.
 *
 *              SESSION KEYS (written on successful login):
 *                "UserId"     → int
 *                "RoleId"     → int
 *                "UserName"   → string
 *                "EmployeeId" → int
 *
 * Dependencies: BCrypt.Net-Next (NuGet), IAuditService, IHttpContextAccessor
 *
 * Phase:   2
 * Author:  ScrumFlix Rebuild Team
 * Updated: 2026-05-04
 */

using BCrypt.Net;
using Microsoft.AspNetCore.Http;

namespace ScrumFlix.Services;

/// <summary>
/// Concrete implementation of <see cref="IAuthService"/>.
/// Registered as scoped in Program.cs.
/// </summary>
public sealed class AuthService : IAuthService
{
    // ── Constants ──────────────────────────────────────────────────────────

    /// <summary>Username of the synthetic web-sales system account that may never log in.</summary>
    private const string WebSalesUserName = "web.sales";

    /// <summary>Consecutive failures before lockout is applied.</summary>
    private const int LockoutThreshold = 5;

    /// <summary>How long a locked account stays locked.</summary>
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    // ── Session key constants ──────────────────────────────────────────────

    /// <summary>Session key for the authenticated user's primary key.</summary>
    public const string SessionUserId = "UserId";

    /// <summary>Session key for the authenticated user's role.</summary>
    public const string SessionRoleId = "RoleId";

    /// <summary>Session key for the authenticated user's display name.</summary>
    public const string SessionUserName = "UserName";

    /// <summary>
    /// Session key alias used when the UserName field holds an email address.
    /// ScrumFlix stores the login identifier under a single session key — for accounts
    /// whose UserName is an email address this alias makes the intent explicit at call
    /// sites without introducing a separate session value.
    /// Points to the same underlying key as <see cref="SessionUserName"/>.
    /// </summary>
    public const string SessionUserEmail = SessionUserName;

    /// <summary>Session key for the authenticated user's linked employee record.</summary>
    public const string SessionEmployeeId = "EmployeeId";

    // ── Dependencies ───────────────────────────────────────────────────────

    private readonly AppDbContext         _db;
    private readonly IAuditService        _audit;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<AuthService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AuthService"/>.
    /// </summary>
    public AuthService(
        AppDbContext         db,
        IAuditService        audit,
        IHttpContextAccessor http,
        ILogger<AuthService> logger)
    {
        _db     = db;
        _audit  = audit;
        _http   = http;
        _logger = logger;
    }

    // ── IAuthService implementation ────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<LoginResult> LoginAsync(string userName, string password)
    {
        // ── Guard: system account block ────────────────────────────────────
        // web.sales must NEVER be able to log in interactively, regardless of
        // IsActive or LockoutEndUtc state. This is the primary enforcement point.
        if (string.Equals(userName, WebSalesUserName, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Login attempt for blocked system account '{UserName}' was rejected.",
                userName);
            return LoginResult.Fail(LoginOutcome.SystemAccountBlocked);
        }

        // ── Lookup ─────────────────────────────────────────────────────────
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == userName);

        if (user is null)
        {
            _logger.LogInformation("Login failed — username '{UserName}' not found.", userName);
            return LoginResult.Fail(LoginOutcome.InvalidCredentials);
        }

        // ── Active check ───────────────────────────────────────────────────
        if (!user.IsActive)
        {
            _logger.LogInformation(
                "Login rejected — UserId {UserId} account is inactive.", user.UserId);
            return LoginResult.Fail(LoginOutcome.AccountInactive);
        }

        // ── Lockout check ──────────────────────────────────────────────────
        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
        {
            _logger.LogInformation(
                "Login rejected — UserId {UserId} is locked out until {LockoutEnd} UTC.",
                user.UserId, user.LockoutEndUtc.Value);

            await _audit.LogAsync(
                userId:      user.UserId,
                actionType:  "LOGIN_FAILED",
                tableName:   "Users",
                objectId:    user.UserId,
                description: $"Login blocked — account locked until {user.LockoutEndUtc.Value:u}");

            return LoginResult.Fail(LoginOutcome.LockedOut, user.LockoutEndUtc);
        }

        // ── Password verification ──────────────────────────────────────────
        // We need a tracked entity for the update path, so re-fetch without AsNoTracking.
        var trackedUser = await _db.Users.FindAsync(user.UserId);
        if (trackedUser is null)
            return LoginResult.Fail(LoginOutcome.InvalidCredentials);

        bool passwordValid;

        if (trackedUser.PasswordHash is not null)
        {
            // Normal path: BCrypt hash is present.
            passwordValid = BCrypt.Net.BCrypt.Verify(password, trackedUser.PasswordHash);
        }
        else if (!string.IsNullOrEmpty(trackedUser.UserPassword))
        {
            // Migration path: account still has legacy plaintext password.
            passwordValid = string.Equals(password, trackedUser.UserPassword,
                StringComparison.Ordinal);

            if (passwordValid)
            {
                // Migrate to BCrypt on first successful plaintext match.
                trackedUser.PasswordHash    = BCrypt.Net.BCrypt.HashPassword(password);
                trackedUser.UserPassword    = string.Empty;   // clear plaintext — never store again
                trackedUser.PasswordChangedUtc = DateTime.UtcNow;
                _logger.LogInformation(
                    "Plaintext password migrated to BCrypt hash for UserId {UserId}.",
                    trackedUser.UserId);
            }
        }
        else
        {
            // No credentials at all — account cannot authenticate.
            _logger.LogWarning(
                "UserId {UserId} has neither PasswordHash nor UserPassword. " +
                "Account requires admin reset.",
                trackedUser.UserId);
            passwordValid = false;
        }

        // ── Failure path ───────────────────────────────────────────────────
        if (!passwordValid)
        {
            trackedUser.FailedAccessCount++;

            if (trackedUser.FailedAccessCount >= LockoutThreshold)
            {
                trackedUser.LockoutEndUtc = DateTime.UtcNow.Add(LockoutDuration);
                _logger.LogWarning(
                    "UserId {UserId} locked out after {Count} failed attempts until {End} UTC.",
                    trackedUser.UserId, trackedUser.FailedAccessCount, trackedUser.LockoutEndUtc);

                await _audit.LogAsync(
                    userId:      trackedUser.UserId,
                    actionType:  "LOCKOUT",
                    tableName:   "Users",
                    objectId:    trackedUser.UserId,
                    description: $"Account locked after {trackedUser.FailedAccessCount} " +
                                 $"failed attempts. Unlocks at {trackedUser.LockoutEndUtc:u}");
            }

            await _audit.LogAsync(
                userId:      trackedUser.UserId,
                actionType:  "LOGIN_FAILED",
                tableName:   "Users",
                objectId:    trackedUser.UserId,
                description: $"Failed login attempt #{trackedUser.FailedAccessCount} " +
                             $"for user '{trackedUser.UserName}'");

            await _db.SaveChangesAsync();
            return LoginResult.Fail(LoginOutcome.InvalidPassword);
        }

        // ── Success path ───────────────────────────────────────────────────
        trackedUser.FailedAccessCount = 0;
        trackedUser.LockoutEndUtc     = null;
        trackedUser.LastLoginUtc      = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Populate session.
        var session = _http.HttpContext!.Session;
        session.SetInt32(SessionUserId,     trackedUser.UserId);
        session.SetInt32(SessionRoleId,     trackedUser.RoleId);
        session.SetString(SessionUserName,  trackedUser.UserName);
        session.SetInt32(SessionEmployeeId, trackedUser.EmployeeId);

        await _audit.LogAsync(
            userId:      trackedUser.UserId,
            actionType:  "LOGIN",
            tableName:   "Users",
            objectId:    trackedUser.UserId,
            description: $"User '{trackedUser.UserName}' logged in successfully");

        _logger.LogInformation(
            "User '{UserName}' (UserId {UserId}, RoleId {RoleId}) authenticated.",
            trackedUser.UserName, trackedUser.UserId, trackedUser.RoleId);

        return trackedUser.MustChangePassword
            ? LoginResult.ForceChange(trackedUser.UserId)
            : LoginResult.Ok(trackedUser.UserId);
    }

    /// <inheritdoc/>
    public async Task LogoutAsync(int userId)
    {
        var session = _http.HttpContext?.Session;
        session?.Clear();

        await _audit.LogAsync(
            userId:      userId,
            actionType:  "LOGOUT",
            tableName:   "Users",
            objectId:    userId,
            description: $"UserId {userId} session ended");

        _logger.LogInformation("UserId {UserId} logged out.", userId);
    }

    /// <inheritdoc/>
    public async Task<bool> ChangePasswordAsync(
        int userId, string currentPassword, string newPassword)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return false;

        // Verify current password (supports both hash and legacy plaintext paths).
        bool currentValid;
        if (user.PasswordHash is not null)
            currentValid = BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash);
        else
            currentValid = string.Equals(currentPassword, user.UserPassword, StringComparison.Ordinal);

        if (!currentValid)
        {
            _logger.LogInformation(
                "ChangePassword rejected for UserId {UserId} — current password mismatch.", userId);
            return false;
        }

        user.PasswordHash          = BCrypt.Net.BCrypt.HashPassword(newPassword);
        // Clear legacy plaintext password. Use empty string to avoid writing NULL
        // when the database column is non-nullable.
        user.UserPassword          = string.Empty;
        user.MustChangePassword    = false;
        user.PasswordChangedUtc    = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            userId:      userId,
            actionType:  "PASSWORD_CHANGE",
            tableName:   "Users",
            objectId:    userId,
            description: $"UserId {userId} changed their password");

        _logger.LogInformation("UserId {UserId} successfully changed password.", userId);
        return true;
    }
}
