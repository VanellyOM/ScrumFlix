/*
 * File:      /ScrumFlix/Controllers/ConsumerControllerBase.cs
 * Namespace: ScrumFlix.Controllers
 * Purpose:   Abstract base controller for all consumer-facing controllers.
 *            Automatically establishes the web.sales WebUser session when no
 *            staff session is active, so consumer views never need to handle
 *            the case of a missing UserId — it is always populated.
 *
 *            Consumer controllers that inherit this class:
 *              - HomeController
 *              - MoviesController
 *              - ShowtimesController
 *              - ConcessionsController
 *              - CartController
 *
 *            Mechanism:
 *              OnActionExecuting() fires before every action on every inheriting
 *              controller. If no UserId is found in the session (i.e. no staff
 *              member is logged in), the WebUser's UserId is written into the
 *              session. This is transparent to all consumer views and actions —
 *              GetSessionUserId() in CartController will always return a value.
 *
 *            OnActionExecuted() fires after every action that returns a ViewResult
 *              and populates HttpContext.Items["LayoutViewModel"] with the cart
 *              count, session identity, and active theme. This replaces ViewBag
 *              and the raw Session/Cookie reads previously inlined in _Layout.cshtml.
 *
 *            The WebUser session is intentionally minimal: only UserId is set.
 *            RoleId is NOT set (or set to 0) so RoleAuthorizationFilter and
 *            StaffControllerBase.RoleGuard() cannot be satisfied by a WebUser
 *            session — there is no privilege escalation path.
 *
 *            Thread safety:
 *              ISystemAccountProvider.WebSalesUserId is a cached int resolved
 *              at startup. The OnActionExecuting override is per-request and
 *              reads from session, which is also per-request. No shared state.
 *
 * Dependencies:
 *              ISystemAccountProvider — singleton; resolved via constructor DI.
 *              CartService            — scoped; resolved via constructor DI.
 *
 * Sprint: S2 — Base Controller Classes
 * Updated: ViewBag removal — LayoutViewModel via HttpContext.Items
 */


namespace ScrumFlix.Controllers;

/// <summary>
/// Abstract base controller for all consumer-facing controllers.
/// Automatically writes the WebUser session (web.sales UserId) when
/// no staff member is currently signed in, and populates
/// <c>HttpContext.Items["LayoutViewModel"]</c> for <c>_Layout.cshtml</c>
/// on every request that returns a view.
/// </summary>
public abstract class ConsumerControllerBase : Controller
{
    private readonly ISystemAccountProvider _systemAccounts;
    private readonly CartService _cart;

    // Valid theme tokens — kept here so both OnActionExecuted and any
    // future theme-related helpers share the same source of truth.
    private static readonly string[] ValidThemes = { "dark", "light", "red" };

    /// <summary>
    /// Initializes the consumer controller base with the system account provider
    /// and cart service.
    /// </summary>
    /// <param name="systemAccounts">
    /// Singleton provider of the resolved web.sales WebUser UserId.
    /// </param>
    /// <param name="cart">
    /// Scoped cart service used to read the current session cart item count
    /// for the navbar badge.
    /// </param>
    protected ConsumerControllerBase(ISystemAccountProvider systemAccounts,
                                     CartService cart)
    {
        _systemAccounts = systemAccounts;
        _cart = cart;
    }

    /// <summary>
    /// Fires before every action on all inheriting consumer controllers.
    /// Establishes the WebUser session when no staff session is active.
    /// This is transparent to all consumer views and action methods.
    /// </summary>
    /// <param name="context">The action executing context.</param>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = HttpContext.Session;
        var existingUserId = session.GetInt32(AuthService.SessionUserId);

        if (existingUserId is null)
        {
            // No staff session active — establish the WebUser session.
            // Only UserId is set. RoleId is intentionally omitted so the
            // WebUser cannot satisfy any RoleGuard() check in staff controllers.
            session.SetInt32(AuthService.SessionUserId, _systemAccounts.WebSalesUserId);
        }

        base.OnActionExecuting(context);
    }

    /// <summary>
    /// Fires after every action on all inheriting consumer controllers.
    /// When the result is a <see cref="ViewResult"/>, builds a
    /// <see cref="LayoutViewModel"/> and stores it in
    /// <c>HttpContext.Items["LayoutViewModel"]</c> for <c>_Layout.cshtml</c>
    /// to consume — replacing the previous <c>ViewBag.CartCount</c> pattern.
    /// </summary>
    /// <param name="context">The action executed context.</param>
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);

        // Only populate layout data when we are actually rendering a view.
        // Skips redirects, JSON responses, file results, etc.
        if (context.Result is not ViewResult)
            return;

        var cookieTheme = HttpContext.Request.Cookies["sf-theme"];

        context.HttpContext.Items["LayoutViewModel"] = new LayoutViewModel
        {
            CartCount   = _cart.GetItemCount(),
            UserName    = HttpContext.Session.GetString(AuthService.SessionUserName),
            RoleId      = HttpContext.Session.GetInt32(AuthService.SessionRoleId),
            ActiveTheme = ValidThemes.Contains(cookieTheme) ? cookieTheme! : "dark"
        };
    }

    /// <summary>
    /// Returns the current session UserId. For consumer requests without a logged-in
    /// staff member this will be the WebUser's UserId, established by
    /// <see cref="OnActionExecuting"/>. Never returns <see langword="null"/>
    /// after <see cref="OnActionExecuting"/> has run.
    /// </summary>
    protected int? SessionUserId =>
        HttpContext.Session.GetInt32(AuthService.SessionUserId);

    /// <summary>
    /// Gets the current session login identifier as an email address.
    /// Reads <see cref="AuthService.SessionUserEmail"/> (same key as SessionUserName).
    /// Returns null for unauthenticated or WebUser sessions.
    /// </summary>
    protected string? CurrentUserEmail =>
        HttpContext.Session.GetString(AuthService.SessionUserEmail);

    /// <summary>
    /// Returns the current session RoleId, or <see langword="null"/> for WebUser
    /// sessions (WebUser has no RoleId in the session).
    /// </summary>
    protected int? SessionRoleId =>
        HttpContext.Session.GetInt32(AuthService.SessionRoleId);

    /// <summary>
    /// Returns <see langword="true"/> if a real staff member (not the WebUser) is
    /// currently signed in. Useful for consumer views that optionally show
    /// staff-specific UI (e.g., an admin shortcut link in the nav bar).
    /// </summary>
    protected bool IsStaffSignedIn =>
        SessionRoleId is not null;
}
