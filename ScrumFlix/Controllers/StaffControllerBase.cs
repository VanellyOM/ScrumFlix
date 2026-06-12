/*
 * File:      /ScrumFlix/Controllers/StaffControllerBase.cs
 * Namespace: ScrumFlix.Controllers
 * Purpose:   Abstract base controller for all staff-area controllers.
 *            Provides RoleGuard(int minimumRoleId) — a one-line role enforcement
 *            helper generalized for all role levels.
 *
 *            Staff controllers that inherit this class:
 *              - Areas/Admin/Controllers/AdminHomeController  (minimumRoleId: 1)
 *              - Areas/Admin/Controllers/ScheduleController  (minimumRoleId: 2)
 *              - (future) ManagerController                  (minimumRoleId: 2)
 *              - (future) EmployeeController                 (minimumRoleId: 3)
 *
 *            Role hierarchy:
 *              RoleId 1 = Admin     — full access to all staff areas
 *              RoleId 2 = Manager   — access to schedule, reports, concession management
 *              RoleId 3 = Employee  — access to ticket sales, concession sales, own schedule
 *              RoleId null / 0      — WebUser / no session — no staff access whatsoever
 *
 *            Usage in a staff controller action:
 *              if (RoleGuard(2) is { } redirect) return redirect; // Manager+ required
 *              if (RoleGuard(1) is { } redirect) return redirect; // Admin only
 *
 * Dependencies: none beyond the framework (reads from HttpContext.Session).
 *
 * Sprint: S2 — Base Controller Classes
 *
 * Phase:   2
 * Author:  ScrumFlix Rebuild Team
 */


namespace ScrumFlix.Controllers;

/// <summary>
/// Abstract base controller for all staff-area controllers.
/// Provides a shared <see cref="RoleGuard"/> helper for role-level enforcement,
/// and populates <c>HttpContext.Items["LayoutViewModel"]</c> for every view
/// result so the Admin sidebar can read the current session RoleId.
/// </summary>
public abstract class StaffControllerBase : Controller
{
    // ── Layout data ────────────────────────────────────────────────────────

    /// <summary>
    /// Fires after every action on all inheriting staff controllers.
    /// When the result is a <see cref="ViewResult"/>, builds a
    /// <see cref="LayoutViewModel"/> and stores it in
    /// <c>HttpContext.Items["LayoutViewModel"]</c> so <c>_AdminSidebar.cshtml</c>
    /// can read the current session RoleId and render the correct nav sections.
    ///
    /// Staff views do not need a cart count — that field is left at its
    /// default (0). All other fields are read from the session.
    /// </summary>
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);

        if (context.Result is not ViewResult) return;

        var cookieTheme = HttpContext.Request.Cookies["sf-theme"];
        var validThemes = new[] { "dark", "light", "red" };

        context.HttpContext.Items["LayoutViewModel"] = new LayoutViewModel
        {
            CartCount   = 0,
            UserName    = HttpContext.Session.GetString(AuthService.SessionUserName),
            RoleId      = HttpContext.Session.GetInt32(AuthService.SessionRoleId),
            ActiveTheme = validThemes.Contains(cookieTheme) ? cookieTheme! : "dark"
        };
    }

    // ── Role guard ─────────────────────────────────────────────────────────

    /// <summary>
    /// Checks whether the current session satisfies the minimum role requirement.
    /// Returns <see langword="null"/> if authorized; returns a redirect
    /// <see cref="IActionResult"/> if not, so callers can do:
    /// <code>if (RoleGuard(2) is { } redirect) return redirect;</code>
    /// </summary>
    /// <param name="minimumRoleId">
    /// The lowest RoleId that is permitted access.
    /// <list type="bullet">
    ///   <item><description>1 = Admin only</description></item>
    ///   <item><description>2 = Manager or Admin</description></item>
    ///   <item><description>3 = Employee, Manager, or Admin</description></item>
    /// </list>
    /// </param>
    /// <returns>
    /// <see langword="null"/> when the session RoleId is &lt;= <paramref name="minimumRoleId"/>
    /// (i.e. sufficient privilege); an <see cref="IActionResult"/> redirect when not.
    /// </returns>
    protected IActionResult? RoleGuard(int minimumRoleId)
    {
        var roleId = HttpContext.Session.GetInt32(AuthService.SessionRoleId);

        // Authorized if: a RoleId is in session AND it is <= the minimum required.
        // (Lower RoleId numbers = higher privilege: 1 > 2 > 3)
        if (roleId is not null && roleId <= minimumRoleId)
            return null;

        if (roleId is null)
        {
            // Unauthenticated — send to Login and preserve the original URL so
            // the user lands back on the page they wanted after signing in.
            TempData["ErrorMessage"] = "Please sign in to access the staff portal.";
            return RedirectToAction("Login", "Account", new
            {
                area      = "",
                returnUrl = Request.Path + Request.QueryString
            });
        }

        // Authenticated but insufficient privilege — do NOT bounce to Login
        // (GET Login redirects active sessions to the consumer HomeDashboard,
        // which dumps staff out of the portal). Send them to their own
        // role-appropriate staff landing page instead.
        TempData["ErrorMessage"] =
            "Access denied. You do not have sufficient permissions for this area.";
        return StaffHome(roleId);
    }

    /// <summary>
    /// Returns the role-appropriate Staff Portal landing redirect:
    /// <list type="bullet">
    ///   <item><description>RoleId 1 (Admin) — Admin/AdminHome/AdminDashboard</description></item>
    ///   <item><description>RoleId 2 (Manager) — Admin/Schedule/Index (highest page a Manager can access)</description></item>
    ///   <item><description>RoleId 3 (Employee) — consumer HomeDashboard until the Employee
    ///     area ships (Phase 4); TODO: switch to Employee/Home/Index</description></item>
    ///   <item><description>null / unknown — Login</description></item>
    /// </list>
    /// Used by <see cref="RoleGuard"/> for insufficient-privilege redirects and by
    /// <c>AccountController</c> for post-login routing, so the two always agree.
    /// </summary>
    protected RedirectToActionResult StaffHome(int? roleId) => roleId switch
    {
        1 => RedirectToAction("AdminDashboard", "AdminHome", new { area = "Admin" }),
        2 => RedirectToAction("Index", "Schedule", new { area = "Admin" }),
        // TODO Phase 4: Employee area scaffold — change to
        //   RedirectToAction("Index", "EmployeeHome", new { area = "Employee" })
        3 => RedirectToAction("HomeDashboard", "Home", new { area = "" }),
        _ => RedirectToAction("Login", "Account", new { area = "" })
    };

    // ── Session helpers ────────────────────────────────────────────────────

    /// <summary>Gets the currently authenticated staff member's UserId from the session.</summary>
    protected int? CurrentUserId =>
        HttpContext.Session.GetInt32(AuthService.SessionUserId);

    /// <summary>Gets the currently authenticated staff member's RoleId from the session.</summary>
    protected int? CurrentRoleId =>
        HttpContext.Session.GetInt32(AuthService.SessionRoleId);

    /// <summary>Gets the currently authenticated staff member's username from the session.</summary>
    protected string? CurrentUserName =>
        HttpContext.Session.GetString(AuthService.SessionUserName);

    /// <summary>
    /// Gets the current staff session login identifier as an email address.
    /// Reads <see cref="AuthService.SessionUserEmail"/> (same key as SessionUserName).
    /// Returns null for unauthenticated or non-email username accounts.
    /// </summary>
    protected string? CurrentUserEmail =>
        HttpContext.Session.GetString(AuthService.SessionUserEmail);

    /// <summary>
    /// Returns <see langword="true"/> if the current session belongs to an Admin (RoleId == 1).
    /// Convenience shorthand for views that conditionally show admin-only UI.
    /// </summary>
    protected bool IsAdmin => CurrentRoleId == 1;

    /// <summary>
    /// Returns <see langword="true"/> if the current session belongs to a Manager or Admin
    /// (RoleId &lt;= 2). Convenience shorthand for views.
    /// </summary>
    protected bool IsManagerOrAbove => CurrentRoleId is not null && CurrentRoleId <= 2;
}
