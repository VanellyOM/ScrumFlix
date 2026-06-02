/*
 * File:      /ScrumFlix/ViewModels/LayoutViewModel.cs
 * Purpose:   Strongly-typed model for data shared across all consumer-facing
 *            views via _Layout.cshtml. Replaces ViewBag.CartCount and the
 *            raw Session/Cookie reads that were previously inlined in the layout.
 *
 *            Populated by ConsumerControllerBase.OnActionExecuted() on every
 *            request that returns a ViewResult, and stored in
 *            HttpContext.Items["LayoutViewModel"] so the layout can retrieve it
 *            without coupling to any specific page ViewModel.
 */

namespace ScrumFlix.ViewModels;

/// <summary>
/// Carries the cross-cutting data that <c>_Layout.cshtml</c> needs on every
/// consumer-facing page: cart count, session identity, and active theme.
/// </summary>
public class LayoutViewModel
{
    /// <summary>
    /// Total number of ticket/concession line-items currently in the session cart.
    /// Drives the navbar badge count.
    /// </summary>
    public int CartCount { get; set; }

    /// <summary>
    /// Display name of the currently signed-in staff member, or <c>null</c> for
    /// unauthenticated / WebUser sessions.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// RoleId of the currently signed-in staff member, or <c>null</c> for
    /// unauthenticated / WebUser sessions.
    /// </summary>
    public int? RoleId { get; set; }

    /// <summary>
    /// The active UI theme token (<c>"dark"</c>, <c>"light"</c>, or <c>"red"</c>).
    /// Resolved from the <c>sf-theme</c> cookie; falls back to <c>"dark"</c>.
    /// </summary>
    public string ActiveTheme { get; set; } = "dark";
}
