/*
 * File:        /ScrumFlix/Controllers/AccountController.cs
 * Namespace:   ScrumFlix.Controllers
 * Purpose:     Handles all session-based authentication actions.
 *
 *              ACTIONS:
 *                GET  /Account/Login                 — render login form
 *                POST /Account/Login                 — authenticate; redirect or show errors
 *                GET  /Account/Logout                — clear session; redirect to login
 *                GET  /Account/ChangePassword        — render change-password form
 *                POST /Account/ChangePassword        — validate and commit new password
 *
 *              POST LOGIN ROUTING:
 *                Success               → ReturnUrl if safe, else HomeDashboard
 *                MustChangePassword    → ChangePassword (forced; IsForced=true in VM)
 *                SystemAccountBlocked  → "This account cannot log in interactively."
 *                LockedOut             → "Account is locked. Try again after {time}."
 *                AccountInactive       → "Account is inactive. Contact an administrator."
 *                InvalidCredentials /
 *                  InvalidPassword     → "Invalid username or password."  (intentionally
 *                                        combined — never reveal which field was wrong)
 *
 *              AUDIT EVENTS (via IAuthService → IAuditService):
 *                LOGIN, LOGIN_FAILED, LOGOUT, LOCKOUT, PASSWORD_CHANGE
 *                All written inside AuthService — AccountController does NOT
 *                call IAuditService directly.
 *
 * Phase:   2
 * Author:  ScrumFlix Rebuild Team
 * Updated: 2026-05-04
 */

namespace ScrumFlix.Controllers;

/// <summary>
/// Manages login, logout, and password-change flows for ScrumFlix employees.
/// No [RequireRole] attribute — all actions must be publicly accessible so that
/// unauthenticated users can reach the login page.
/// </summary>
public sealed class AccountController : Controller
{
    private readonly IAuthService _auth;
    private readonly ILogger<AccountController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AccountController"/>.
    /// </summary>
    public AccountController(IAuthService auth, ILogger<AccountController> logger)
    {
        _auth = auth;
        _logger = logger;
    }

    // ── Login ──────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /Account/Login
    /// Renders the login form. Redirects to HomeDashboard if a session is already active.
    /// </summary>
    /// <param name="returnUrl">
    /// The URL the user was trying to reach before being redirected here.
    /// Preserved through the form so POST can redirect back after success.
    /// </param>
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // Already logged in — don't show the login form again.
        // Staff sessions return to their role-appropriate portal landing
        // (mirror of StaffControllerBase.StaffHome(); keep in sync);
        // consumer/WebUser sessions go to the public HomeDashboard.
        if (HttpContext.Session.GetInt32(AuthService.SessionRoleId) is { } activeRoleId)
        {
            return activeRoleId switch
            {
                1 => RedirectToAction("AdminDashboard", "AdminHome", new { area = "Admin" }),
                2 => RedirectToAction("Index", "Schedule", new { area = "Admin" }),
                // TODO Phase 4: Employee area scaffold — RoleId 3 → Employee home
                _ => RedirectToAction("HomeDashboard", "Home")
            };
        }

        var vm = new LoginViewModel { ReturnUrl = returnUrl };
        return View(vm);
    }

    /// <summary>
    /// POST /Account/Login
    /// Authenticates the submitted credentials via <see cref="IAuthService"/>.
    /// On success, populates session and redirects. On failure, re-renders form with error.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var result = await _auth.LoginAsync(vm.UserName.Trim(), vm.Password);

        switch (result.Outcome)
        {
            case LoginOutcome.Success:
                // ── Post-login routing ─────────────────────────────────────
                // Staff users go to the landing page for THEIR role — never to
                // a page their RoleGuard would reject. (Previously all staff
                // were sent to AdminDashboard, which is RoleGuard(1); Managers
                // immediately tripped the guard and were dumped onto the
                // consumer HomeDashboard with an "Access denied" flash.)
                // A safe returnUrl that points into the Admin area takes
                // precedence (e.g. the user bookmarked a specific staff page);
                // if it is beyond their role, RoleGuard redirects them to their
                // role home — same destination, one extra hop, no error loop.
                var roleId = HttpContext.Session.GetInt32(AuthService.SessionRoleId);
                if (roleId.HasValue)
                {
                    // Staff session — honour a returnUrl only if it points to
                    // the Admin area; otherwise send to the role landing page.
                    if (!string.IsNullOrWhiteSpace(vm.ReturnUrl)
                        && Url.IsLocalUrl(vm.ReturnUrl)
                        && vm.ReturnUrl.Contains("/Admin/", StringComparison.OrdinalIgnoreCase))
                    {
                        return Redirect(vm.ReturnUrl);
                    }

                    // Mirror of StaffControllerBase.StaffHome() — keep in sync.
                    return roleId switch
                    {
                        1 => RedirectToAction("AdminDashboard", "AdminHome", new { area = "Admin" }),
                        2 => RedirectToAction("Index", "Schedule", new { area = "Admin" }),
                        // TODO Phase 4: Employee area scaffold — change to
                        //   RedirectToAction("Index", "EmployeeHome", new { area = "Employee" })
                        _ => RedirectToAction("HomeDashboard", "Home", new { area = "" })
                    };
                }

                // Consumer / no-role session — standard safe-URL redirect
                return RedirectToSafeUrl(vm.ReturnUrl);

            case LoginOutcome.MustChangePassword:
                // Successful auth but password change is required before access.
                TempData["InfoMessage"] =
                    "Your password must be changed before you can continue.";
                return RedirectToAction(nameof(ChangePassword));

            case LoginOutcome.LockedOut:
                var until = result.LockoutEnd.HasValue
                    ? result.LockoutEnd.Value.ToLocalTime().ToString("h:mm tt")
                    : "a few minutes";
                ModelState.AddModelError(string.Empty,
                    $"This account is temporarily locked. Please try again after {until}.");
                return View(vm);

            case LoginOutcome.AccountInactive:
                ModelState.AddModelError(string.Empty,
                    "This account is inactive. Please contact an administrator.");
                return View(vm);

            case LoginOutcome.SystemAccountBlocked:
                // Do not reveal that web.sales exists — treat as generic failure.
                ModelState.AddModelError(string.Empty,
                    "Invalid username or password.");
                return View(vm);

            case LoginOutcome.InvalidCredentials:
            case LoginOutcome.InvalidPassword:
            default:
                // Intentionally identical message for both — never indicate which field failed.
                ModelState.AddModelError(string.Empty,
                    "Invalid username or password.");
                return View(vm);
        }
    }

    // ── Logout ─────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /Account/Logout
    /// Ends the current staff session securely, writes a LOGOUT audit record,
    /// clears all session data (including the WebUser fallback), and redirects to
    /// the public HomeDashboard so the browser lands on the customer-facing page.
    /// The session cookie is abandoned (not just cleared) to prevent fixation attacks.
    /// Back-button after logout reaches HomeDashboard, not the staff portal.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        var userId   = HttpContext.Session.GetInt32(AuthService.SessionUserId) ?? 0;
        var userName = HttpContext.Session.GetString(AuthService.SessionUserName) ?? "unknown";

        // Write audit record before clearing session
        await _auth.LogoutAsync(userId);

        _logger.LogInformation(
            "User '{UserName}' (UserId {UserId}) logged out.", userName, userId);

        // Fully abandon the session (not just clear keys) so the session cookie
        // becomes invalid and cannot be replayed via browser back-button.
        HttpContext.Session.Clear();
        await HttpContext.Session.CommitAsync();

        // Expire the session cookie explicitly
        Response.Cookies.Delete(".ScrumFlix.Session");

        TempData["SuccessMessage"] = "You have been signed out successfully.";

        // Redirect to the public home page — not the staff login — so the
        // browser lands on the movie-goer dashboard after logout.
        return RedirectToAction("HomeDashboard", "Home", new { area = "" });
    }

    // ── ChangePassword ─────────────────────────────────────────────────────

    /// <summary>
    /// GET /Account/ChangePassword
    /// Renders the change-password form.
    /// Requires an active session — unauthenticated users are redirected to Login
    /// by the global <see cref="Data.RoleAuthorizationFilter"/> (no [RequireRole]
    /// needed here because any authenticated role may change their own password).
    /// </summary>
    [HttpGet]
    public IActionResult ChangePassword()
    {
        var userId = HttpContext.Session.GetInt32(AuthService.SessionUserId);
        if (userId is null)
            return RedirectToAction(nameof(Login));

        var vm = new ChangePasswordViewModel
        {
            // IsForced = true when MustChangePassword redirect lands here.
            // TempData key set by POST Login above.
            IsForced = TempData["InfoMessage"] != null
        };

        return View(vm);
    }

    /// <summary>
    /// POST /Account/ChangePassword
    /// Validates form, delegates to <see cref="IAuthService.ChangePasswordAsync"/>,
    /// and redirects to HomeDashboard on success or re-renders on failure.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
    {
        var userId = HttpContext.Session.GetInt32(AuthService.SessionUserId);
        if (userId is null)
            return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid)
            return View(vm);

        var success = await _auth.ChangePasswordAsync(
            userId.Value, vm.CurrentPassword, vm.NewPassword);

        if (!success)
        {
            ModelState.AddModelError(nameof(vm.CurrentPassword),
                "Current password is incorrect.");
            return View(vm);
        }

        TempData["SuccessMessage"] = "Password changed successfully.";
        return RedirectToAction("HomeDashboard", "Home");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Redirects to <paramref name="returnUrl"/> only if it is a local URL,
    /// preventing open-redirect attacks. Falls back to HomeDashboard.
    /// </summary>
    private IActionResult RedirectToSafeUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("HomeDashboard", "Home");
    }
}
