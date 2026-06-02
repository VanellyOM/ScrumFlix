/*
 * File:        /ScrumFlix/Services/LoginResult.cs
 * Namespace:   ScrumFlix.Services
 * Purpose:     Discriminated result type returned by IAuthService.LoginAsync.
 *
 *              AccountController uses the Outcome enum to decide which error
 *              message to display without coupling the controller to AuthService
 *              implementation details.
 *
 * Phase:   2
 * Author:  ScrumFlix Rebuild Team
 * Updated: 2026-05-04
 */

namespace ScrumFlix.Services;

/// <summary>
/// Describes the specific outcome of a login attempt.
/// </summary>
public enum LoginOutcome
{
    /// <summary>Authentication succeeded; session is populated.</summary>
    Success,

    /// <summary>Username not found or account does not exist.</summary>
    InvalidCredentials,

    /// <summary>Password did not match stored hash (or legacy plaintext).</summary>
    InvalidPassword,

    /// <summary>Account is locked out. LockoutEndUtc contains the expiry time.</summary>
    LockedOut,

    /// <summary>Account exists but IsActive = false (manually deactivated).</summary>
    AccountInactive,

    /// <summary>
    /// The submitted username matches the web.sales system account.
    /// This account is permanently blocked from interactive login regardless
    /// of IsActive or LockoutEndUtc state.
    /// </summary>
    SystemAccountBlocked,

    /// <summary>
    /// Authentication succeeded but MustChangePassword = true.
    /// Controller should redirect to ChangePassword before allowing navigation.
    /// </summary>
    MustChangePassword,
}

/// <summary>
/// The result of a call to <see cref="IAuthService.LoginAsync"/>.
/// </summary>
public sealed class LoginResult
{
    /// <summary>The specific outcome of the login attempt.</summary>
    public LoginOutcome Outcome { get; init; }

    /// <summary>True when <see cref="Outcome"/> is Success or MustChangePassword.</summary>
    public bool Succeeded =>
        Outcome is LoginOutcome.Success or LoginOutcome.MustChangePassword;

    /// <summary>
    /// The authenticated user's ID. Populated only when <see cref="Succeeded"/> is true.
    /// </summary>
    public int? UserId { get; init; }

    /// <summary>
    /// UTC time when the lockout expires.
    /// Populated only when <see cref="Outcome"/> is <see cref="LoginOutcome.LockedOut"/>.
    /// </summary>
    public DateTime? LockoutEnd { get; init; }

    // ── Static factory helpers ─────────────────────────────────────────────

    /// <summary>Creates a successful login result.</summary>
    public static LoginResult Ok(int userId) =>
        new() { Outcome = LoginOutcome.Success, UserId = userId };

    /// <summary>Creates a success result that requires an immediate password change.</summary>
    public static LoginResult ForceChange(int userId) =>
        new() { Outcome = LoginOutcome.MustChangePassword, UserId = userId };

    /// <summary>Creates a failure result with the given outcome.</summary>
    public static LoginResult Fail(LoginOutcome outcome, DateTime? lockoutEnd = null) =>
        new() { Outcome = outcome, LockoutEnd = lockoutEnd };
}
