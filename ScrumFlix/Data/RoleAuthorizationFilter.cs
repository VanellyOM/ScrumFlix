/*
 * File:        /ScrumFlix/Data/RoleAuthorizationFilter.cs
 * Namespace:   ScrumFlix.Data
 * Purpose:     Action filter that enforces session-based role authorization.
 *
 *              ScrumFlix does NOT use ASP.NET Core Identity or [Authorize].
 *              Instead, controllers or actions are decorated with [RequireRole(...)]
 *              and this filter reads RoleId from the session to determine access.
 *
 *              ROLE HIERARCHY (from the canonical Roles table):
 *                1 = Admin      — full access
 *                2 = Manager    — access to manager and employee areas
 *                3 = Employee   — access to employee area only
 *
 *              The filter short-circuits to:
 *                - /Account/Login  if the session has no UserId (unauthenticated)
 *                - /Home/AccessDenied  if RoleId is insufficient (authenticated, wrong role)
 *
 * Phase:   2
 * Author:  ScrumFlix Rebuild Team
 * Updated: 2026-05-04
 */


namespace ScrumFlix.Data;

// ── Attribute ──────────────────────────────────────────────────────────────────

/// <summary>
/// Restricts access to authenticated users whose RoleId is less than or equal
/// to <paramref name="minimumRoleId"/> (lower number = higher privilege).
/// </summary>
/// <remarks>
/// Apply to a controller class to protect all actions, or to individual action
/// methods to override. Examples:
/// <code>
/// [RequireRole(RoleId.Admin)]      // Admin only
/// [RequireRole(RoleId.Manager)]    // Admin or Manager
/// [RequireRole(RoleId.Employee)]   // Any authenticated user
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireRoleAttribute : Attribute
{
    /// <summary>The maximum RoleId (most-permissive role) allowed to access this resource.</summary>
    public int MinimumRoleId { get; }

    /// <summary>
    /// Initializes a new <see cref="RequireRoleAttribute"/>.
    /// </summary>
    /// <param name="minimumRoleId">
    /// The least-privileged RoleId permitted. Users with a RoleId less than or equal
    /// to this value are granted access (1=Admin ≤ 2=Manager ≤ 3=Employee).
    /// </param>
    public RequireRoleAttribute(int minimumRoleId) => MinimumRoleId = minimumRoleId;
}

// ── Static role ID constants ───────────────────────────────────────────────────

/// <summary>
/// Named constants for the canonical RoleId values in the Roles table.
/// Use these with <see cref="RequireRoleAttribute"/> instead of bare integers.
/// </summary>
public static class RoleId
{
    /// <summary>RoleId 1 — full administrative access.</summary>
    public const int Admin = 1;

    /// <summary>RoleId 2 — manager-level access (includes Employee areas).</summary>
    public const int Manager = 2;

    /// <summary>RoleId 3 — employee-level access only.</summary>
    public const int Employee = 3;
}

// ── Filter ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Global action filter that enforces <see cref="RequireRoleAttribute"/> constraints.
/// Registered globally in Program.cs so it runs on every request automatically.
/// </summary>
public sealed class RoleAuthorizationFilter : IActionFilter
{
    /// <inheritdoc/>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // ── Determine the effective RequireRole constraint ──────────────────
        // Check method-level attribute first; fall back to controller-level.
        var attribute =
            context.ActionDescriptor.EndpointMetadata
                .OfType<RequireRoleAttribute>()
                .FirstOrDefault();

        // No [RequireRole] on this action or controller → allow through.
        // Public actions (Login, AccessDenied, Error) should have no attribute.
        if (attribute is null) return;

        var session = context.HttpContext.Session;
        var userId  = session.GetInt32(AuthService.SessionUserId);

        // ── Unauthenticated ────────────────────────────────────────────────
        if (userId is null)
        {
            // Preserve the originally-requested URL so Login can redirect back.
            var returnUrl = context.HttpContext.Request.Path +
                            context.HttpContext.Request.QueryString;

            context.Result = new RedirectToActionResult(
                actionName:      "Login",
                controllerName:  "Account",
                routeValues:     new { returnUrl });
            return;
        }

        // ── Insufficient role ──────────────────────────────────────────────
        var roleId = session.GetInt32(AuthService.SessionRoleId) ?? int.MaxValue;

        if (roleId > attribute.MinimumRoleId)
        {
            context.Result = new RedirectToActionResult(
                actionName:     "AccessDenied",
                controllerName: "Home",
                routeValues:    null);
        }

        // Else: authenticated and role is sufficient → allow the action to execute.
    }

    /// <inheritdoc/>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Post-execution hook — nothing required for role enforcement.
    }
}
