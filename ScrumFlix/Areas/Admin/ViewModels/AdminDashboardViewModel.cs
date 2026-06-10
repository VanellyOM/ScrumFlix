/*
 * File:      /ScrumFlix/Areas/Admin/ViewModels/AdminDashboardViewModel.cs
 * Namespace: ScrumFlix.Areas.Admin.ViewModels
 * Purpose:   Strongly-typed ViewModel for the admin dashboard.
 *            Replaces all 10 ViewBag stat keys and ViewBag.TmdbSync on
 *            AdminHomeController.AdminDashboard.
 *
 *            Stat properties map 1-to-1 with the former ViewBag keys:
 *              TicketsSoldToday      ← ViewBag.TicketsSoldToday
 *              RevenueToday          ← ViewBag.RevenueToday
 *              ActiveShowtimes       ← ViewBag.ActiveShowtimes
 *              ConcessionsSoldToday  ← ViewBag.ConcessionsSoldToday
 *              LowStockCount         ← ViewBag.LowStockCount
 *              TotalMovies           ← ViewBag.TotalMovies
 *              TotalLocations        ← ViewBag.TotalLocations
 *              TotalUsers            ← ViewBag.TotalUsers
 *              ConcessionItems       ← ViewBag.ConcessionItems
 *              RecentUsers           ← ViewBag.RecentUsers
 *              TmdbSync              ← ViewBag.TmdbSync (nested TmdbSyncViewModel)
 *
 * Sprint: S1 — ViewBag Purge
 */


namespace ScrumFlix.Areas.Admin.ViewModels;

/// <summary>
/// ViewModel for the admin dashboard page. Carries all operational stats,
/// inventory data, user list, and the TMDb sync panel state.
/// </summary>
public class AdminDashboardViewModel
{
    // ── Ticket stats ───────────────────────────────────────────────────────

    /// <summary>Total number of tickets sold during the current calendar day (UTC).</summary>
    public int TicketsSoldToday { get; set; }

    /// <summary>
    /// Total ticket revenue for the current calendar day (UTC),
    /// computed as <c>SUM(Showtime.PricePerTicket)</c> for tickets sold today.
    /// </summary>
    public decimal RevenueToday { get; set; }

    /// <summary>
    /// Number of showtimes that are both active and scheduled to start
    /// at or after today's date.
    /// </summary>
    public int ActiveShowtimes { get; set; }

    // ── Concession stats ───────────────────────────────────────────────────

    /// <summary>
    /// Total concession item units sold during the current calendar day (UTC),
    /// computed as <c>SUM(ConcessionSaleItem.Quantity)</c>.
    /// </summary>
    public int ConcessionsSoldToday { get; set; }

    /// <summary>
    /// Number of active ConcessionItems whose <c>QuantityInStock</c> is at or
    /// below their configured <c>Minimum</c> threshold.
    /// </summary>
    public int LowStockCount { get; set; }

    /// <summary>
    /// All active ConcessionItems ordered by name, for the inventory table.
    /// </summary>
    public List<ConcessionItem> ConcessionItems { get; set; } = new();

    // ── Global stats ───────────────────────────────────────────────────────

    /// <summary>Total row count of the Movies table.</summary>
    public int TotalMovies { get; set; }

    /// <summary>Total count of active Location records.</summary>
    public int TotalLocations { get; set; }

    /// <summary>Total row count of the Users table.</summary>
    public int TotalUsers { get; set; }

    // ── User management ────────────────────────────────────────────────────

    /// <summary>
    /// Previously used for a user table on the dashboard.
    /// The dashboard no longer renders a user table — full user management lives
    /// on AdminManage/Users (searchable, paginated, sortable).
    /// Property retained to avoid a breaking change on the ViewModel;
    /// BuildDashboardViewModelAsync no longer populates it.
    /// </summary>
    [Obsolete("No longer rendered on the dashboard. Use AdminManage/Users.")]
    public List<User> RecentUsers { get; set; } = new();

    // ── TMDb sync panel ────────────────────────────────────────────────────

    /// <summary>
    /// TMDb synchronization coverage stats and last-run result.
    /// Nested ViewModel populated by <c>BuildTmdbSyncViewModelAsync()</c> in
    /// <c>AdminHomeController</c>. Never null — initialized to an empty instance
    /// so the view can render the panel without null guards.
    /// </summary>
    public TmdbSyncViewModel TmdbSync { get; set; } = new();

    // ── Computed convenience ───────────────────────────────────────────────

    /// <summary>
    /// Returns <see langword="true"/> if any concession items are below their
    /// minimum stock threshold, driving the low-stock alert banner.
    /// </summary>
    public bool HasLowStockItems => LowStockCount > 0;

    /// <summary>Revenue formatted as currency (e.g. <c>"$1,234.50"</c>).</summary>
    public string RevenueTodayFormatted => RevenueToday.ToString("C");
}
