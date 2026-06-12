/*
 * File: /ScrumFlix/Areas/Admin/Controllers/AdminHomeController.cs
 * Description: Admin area controller — populates AdminDashboardViewModel for the
 *              dashboard and handles on-demand TMDb sync operations.
 *
 * Phase 3 — P3-UI-6:
 *   Replaces no-op stub with live queries against canonical DbSets.
 *   All queries are AsNoTracking() read-only aggregates.
 *
 * Phase 2 Addition — TMDb Sync:
 *   Added TmdbSync (GET) and TmdbSyncRun (POST) actions.
 *   GET  → builds TmdbSyncViewModel with current coverage stats; no API call.
 *   POST → calls ITmdbSyncService.SyncGenresAsync then SyncAllMoviesAsync (or
 *          SyncMovieAsync for single-movie trigger); result embedded in ViewModel.
 *
 * Sprint S1 — ViewBag Purge:
 *   - All 10 ViewBag stat keys removed from AdminDashboard action.
 *   - ViewBag.TmdbSync removed from both AdminDashboard and AdminDashboardWithSyncResult.
 *   - New AdminDashboardViewModel carries all stats + TmdbSync as nested property.
 *   - Private BuildDashboardViewModelAsync() extracts shared stat-loading logic so
 *     both AdminDashboard (GET) and AdminDashboardWithSyncResult (post-sync) use the
 *     same builder without duplication.
 *   - Both actions return View("AdminDashboard", vm) with the typed ViewModel.
 *
 * Role guard: all actions require RoleId == 1 (Admin). Implemented via session
 * check rather than [Authorize] attribute since ScrumFlix uses session-based auth.
 * RoleGuard() (inherited from StaffControllerBase) returns a redirect if the check fails.
 *
 * NOTE: AdminDashboard.cshtml must be updated to declare:
 *   @model ScrumFlix.Areas.Admin.ViewModels.AdminDashboardViewModel
 * and replace all ViewBag.X references with Model.X (see view update notes).
 */


using Microsoft.AspNetCore.SignalR;
using ScrumFlix.Hubs;

namespace ScrumFlix.Areas.Admin.Controllers;

[Area("Admin")]
public class AdminHomeController : StaffControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITmdbSyncService _tmdb;
    private readonly IConfiguration _config;
    private readonly ILogger<AdminHomeController> _logger;

    /// <summary>
    /// Initializes AdminHomeController with database context, TMDb sync service,
    /// application configuration, and structured logger.
    /// </summary>
    private readonly IHubContext<TmdbProgressHub> _tmdbHub;

    public AdminHomeController(
        AppDbContext db,
        ITmdbSyncService tmdb,
        IConfiguration config,
        ILogger<AdminHomeController> logger,
        IHubContext<TmdbProgressHub> tmdbHub)
    {
        _db      = db;
        _tmdb    = tmdb;
        _config  = config;
        _logger  = logger;
        _tmdbHub = tmdbHub;
    }

    // ── Dashboard ──────────────────────────────────────────────────────────

    /// <summary>
    /// Renders the admin dashboard with live stats, inventory, user table,
    /// and the TMDb sync coverage panel.
    /// Returns a strongly-typed <see cref="AdminDashboardViewModel"/> — no ViewBag.
    /// </summary>
    public async Task<IActionResult> AdminDashboard()
    {
        if (RoleGuard(1) is { } redirect) return redirect;

        var vm = await BuildDashboardViewModelAsync();
        return View(vm);
    }

    // ── TMDb Sync ──────────────────────────────────────────────────────────

    /// <summary>
    /// POST /Admin/AdminHome/TmdbSyncRun
    /// Triggers an on-demand TMDb sync. Accepts form parameters:
    ///   forceAll  (bool) — re-syncs even fresh metadata when true
    ///   movieId   (int?) — if provided, syncs only that specific movie
    ///
    /// Always runs SyncGenresAsync first so genre associations resolve correctly.
    /// Rebuilds the dashboard ViewModel and injects the sync result into
    /// <see cref="AdminDashboardViewModel.TmdbSync"/>.
    ///
    /// This is an intentionally slow action — the full sync of 100+ movies
    /// can take 30-60 seconds due to per-movie API calls and rate-limit delays.
    /// For long syncs, a future phase should use a background queue and poll
    /// for progress via HTMX. For now, the action runs synchronously.
    /// </summary>
    /// <param name="forceAll">When <see langword="true"/>, re-syncs all movies regardless of existing metadata.</param>
    /// <param name="movieId">Optional single-movie TMDb sync target.</param>
    /// <summary>
    /// POST /Admin/AdminHome/TmdbSyncRun
    /// Triggers an on-demand TMDb sync with real-time SignalR progress broadcast.
    ///
    /// Each movie processed calls IProgress&lt;TmdbSyncProgressReport&gt; which pushes
    /// a "TmdbSyncProgress" event to all clients in TmdbProgressHub.SyncGroup.
    /// The TmdbSyncPage.cshtml spinner receives these events via sfSpinner.fromSignalR().
    ///
    /// On completion or error, a "TmdbSyncComplete" or "TmdbSyncError" event is sent
    /// so the spinner can transition to its complete/error state without polling.
    ///
    /// Single-movie syncs (movieId provided) do not emit per-movie progress events —
    /// they complete too quickly. The redirect back to TmdbSyncPage carries the result
    /// in TempData as before.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TmdbSyncRun(bool forceAll = false, int? movieId = null)
    {
        if (RoleGuard(1) is { } redirect) return redirect;

        _logger.LogInformation(
            "Admin User {UserId} triggered TMDb sync (forceAll={ForceAll}, movieId={MovieId}).",
            CurrentUserId, forceAll, movieId);

        TmdbSyncResult? result = null;

        try
        {
            // Always sync genres first — MovieGenres cannot resolve without the Genres table
            await _tmdb.SyncGenresAsync();

            if (movieId.HasValue)
            {
                // Single-movie sync — no per-movie progress needed, fast enough to skip
                var success = await _tmdb.SyncMovieAsync(movieId.Value);
                result = success
                    ? new TmdbSyncResult(1, 0, 0)
                    : new TmdbSyncResult(0, 0, 1);
            }
            else
            {
                // Full catalog sync — wire progress to SignalR hub broadcasts
                var progressReporter = new Progress<TmdbSyncProgressReport>(async report =>
                {
                    try
                    {
                        await _tmdbHub.Clients
                            .Group(TmdbProgressHub.SyncGroup)
                            .SendAsync("TmdbSyncProgress", new
                            {
                                percent = report.Percent,
                                message = report.Message,
                                synced  = report.Synced,
                                skipped = report.Skipped,
                                failed  = report.Failed,
                                total   = report.Total
                            });
                    }
                    catch (Exception ex)
                    {
                        // Non-fatal — log and continue; the sync must not abort for a hub error
                        _logger.LogWarning(ex, "TmdbSyncRun: failed to broadcast progress event.");
                    }
                });

                result = await _tmdb.SyncAllMoviesAsync(forceAll, progressReporter);

                // Broadcast completion so the spinner transitions to its complete state
                await _tmdbHub.Clients
                    .Group(TmdbProgressHub.SyncGroup)
                    .SendAsync("TmdbSyncComplete", new
                    {
                        synced    = result.Synced,
                        skipped   = result.Skipped,
                        failed    = result.Failed,
                        wasForced = forceAll
                    });
            }

            if (result.Failed == 0)
            {
                TempData["SuccessMessage"] = movieId.HasValue
                    ? "Movie TMDb metadata synced successfully."
                    : $"TMDb sync complete — {result.Synced} movie(s) synced, {result.Skipped} skipped.";
            }
            else
            {
                TempData["InfoMessage"] =
                    $"TMDb sync finished with {result.Failed} failure(s). " +
                    $"Synced: {result.Synced}, Skipped: {result.Skipped}. " +
                    "Check Serilog logs for details.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TMDb sync triggered by Admin User {UserId} failed.", CurrentUserId);

            // Broadcast error so the spinner shows the error state immediately
            try
            {
                await _tmdbHub.Clients
                    .Group(TmdbProgressHub.SyncGroup)
                    .SendAsync("TmdbSyncError", new { message = "Sync failed — check application logs." });
            }
            catch { /* swallow — the real error is already logged above */ }

            TempData["ErrorMessage"] = "TMDb sync encountered an unexpected error. Check the application logs.";
        }

        // Rebuild dashboard stats and embed the sync result into the ViewModel
        var vm = await BuildDashboardViewModelAsync();
        vm.TmdbSync.LastSyncResult    = result;
        vm.TmdbSync.LastSyncWasForced = forceAll;

        return View("AdminDashboard", vm);
    }

    // ── Dedicated TMDb Sync Page ───────────────────────────────────────────

    /// <summary>
    /// GET /Admin/AdminHome/TmdbSyncPage
    /// Renders the full-page TMDb sync management view with per-movie status table,
    /// coverage stats, and bulk/individual sync controls.
    /// Supports search, status filter, and pagination.
    /// </summary>
    /// <param name="search">Optional title search string.</param>
    /// <param name="status">Status filter: "all" | "unsynced" | "stale" | "fresh".</param>
    /// <param name="page">Page number (1-based, default 1).</param>
    public async Task<IActionResult> TmdbSyncPage(
        string? search   = null,
        string  status   = "all",
        int     page     = 1)
    {
        if (RoleGuard(1) is { } redirect) return redirect;

        var vm = await BuildTmdbSyncPageViewModelAsync(search, status, page);
        return View(vm);
    }

    /// <summary>
    /// POST /Admin/AdminHome/TmdbSyncMovie
    /// Triggers a single-movie TMDb sync from the per-movie table on the sync page.
    /// Always syncs genres first, then the individual movie.
    /// Redirects back to TmdbSyncPage with the search/filter/page context preserved.
    /// </summary>
    /// <param name="movieId">Local ScrumFlix MovieId to sync.</param>
    /// <param name="movieTitle">Title for the result banner (passed from the form).</param>
    /// <param name="returnSearch">Search term to restore after redirect.</param>
    /// <param name="returnStatus">Status filter to restore after redirect.</param>
    /// <param name="returnPage">Page number to restore after redirect.</param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TmdbSyncMovie(
        int     movieId,
        string? movieTitle    = null,
        string? returnSearch  = null,
        string  returnStatus  = "all",
        int     returnPage    = 1)
    {
        if (RoleGuard(1) is { } redirect) return redirect;

        _logger.LogInformation(
            "Admin User {UserId} triggered single-movie TMDb sync for MovieId={MovieId} ('{Title}').",
            CurrentUserId, movieId, movieTitle);

        try
        {
            await _tmdb.SyncGenresAsync();
            var success = await _tmdb.SyncMovieAsync(movieId);

            TempData["SyncMovieId"]    = movieId;
            TempData["SyncMovieTitle"] = movieTitle ?? $"Movie #{movieId}";
            TempData["SyncSuccess"]    = success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Single-movie TMDb sync failed for MovieId={MovieId}.", movieId);
            TempData["ErrorMessage"] =
                $"Sync failed for '{movieTitle ?? $"Movie #{movieId}"}'. Check application logs.";
        }

        return RedirectToAction(nameof(TmdbSyncPage), new
        {
            area   = "Admin",
            search = returnSearch,
            status = returnStatus,
            page   = returnPage
        });
    }

    // ── Private helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Builds the full <see cref="AdminDashboardViewModel"/> from live database queries.
    /// Shared by <see cref="AdminDashboard"/> (GET) and the post-sync return path in
    /// <see cref="TmdbSyncRun"/> so stat-loading logic is never duplicated.
    /// All queries are <c>AsNoTracking()</c> read-only.
    /// </summary>
    private async Task<AdminDashboardViewModel> BuildDashboardViewModelAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var ticketsToday = await _db.Tickets
            .Where(t => t.TimeOfSale >= today && t.TimeOfSale < tomorrow)
            .AsNoTracking()
            .CountAsync();

        var revenueToday = await _db.Tickets
            .Where(t => t.TimeOfSale >= today && t.TimeOfSale < tomorrow)
            .Join(_db.Showtimes,
                  t => t.ShowtimeId,
                  st => st.ShowtimeId,
                  (t, st) => st.PricePerTicket)
            .SumAsync(p => (decimal?)p) ?? 0m;

        var activeShowtimes = await _db.Showtimes
            .CountAsync(st => st.IsActive && st.StartTime >= today);

        var concessionsSoldToday = await _db.ConcessionSaleItems
            .Join(_db.ConcessionSales,
                  csi => csi.ConcessionSaleId,
                  cs => cs.ConcessionSaleId,
                  (csi, cs) => new { csi.Quantity, cs.TimeOfSale })
            .Where(x => x.TimeOfSale >= today && x.TimeOfSale < tomorrow)
            .SumAsync(x => (int?)x.Quantity) ?? 0;

        var lowStockCount = await _db.ConcessionItems
            .CountAsync(ci => ci.IsActive && ci.QuantityInStock <= ci.Minimum);

        var totalMovies = await _db.Movies.CountAsync();
        var totalLocations = await _db.Locations.CountAsync(l => l.IsActive);
        var totalUsers = await _db.Users.CountAsync();

        var concessionItems = await _db.ConcessionItems
            .Where(ci => ci.IsActive)
            .OrderBy(ci => ci.ItemName)
            .AsNoTracking()
            .ToListAsync();

        return new AdminDashboardViewModel
        {
            TicketsSoldToday     = ticketsToday,
            RevenueToday         = revenueToday,
            ActiveShowtimes      = activeShowtimes,
            ConcessionsSoldToday = concessionsSoldToday,
            LowStockCount        = lowStockCount,
            TotalMovies          = totalMovies,
            TotalLocations       = totalLocations,
            TotalUsers           = totalUsers,
            ConcessionItems      = concessionItems,
            // RecentUsers not loaded — the dashboard no longer renders a user table.
            // Full user management lives on AdminManage/Users.
            TmdbSync             = await BuildTmdbSyncViewModelAsync()
        };
    }

    /// <summary>
    /// Builds the full <see cref="TmdbSyncPageViewModel"/> for the dedicated sync page.
    /// Applies search, status filter, and pagination against a joined Movies +
    /// MovieTmdbMetadata query. Never calls the TMDb API.
    /// </summary>
    private async Task<TmdbSyncPageViewModel> BuildTmdbSyncPageViewModelAsync(
        string? search,
        string  status,
        int     page,
        int     pageSize = 25)
    {
        // ── Coverage totals (whole catalog, no filter) ─────────────────────
        var totalMovies        = await _db.Movies.CountAsync();
        var moviesWithMetadata = await _db.MovieTmdbMetadata.CountAsync();
        var moviesWithPoster   = await _db.MovieTmdbMetadata.CountAsync(m => m.PosterPath != null);
        var moviesWithTrailer  = await _db.MovieTmdbMetadata.CountAsync(m => m.TrailerYouTubeKey != null);
        var staleThreshold     = DateTime.UtcNow.AddHours(-24);
        var staleMovies        = await _db.MovieTmdbMetadata
            .CountAsync(m => m.LastSyncedUtc == null || m.LastSyncedUtc < staleThreshold);

        // ── Per-movie query ────────────────────────────────────────────────
        var query = _db.Movies
            .GroupJoin(
                _db.MovieTmdbMetadata,
                m  => m.MovieId,
                md => md.MovieId,
                (m, mds) => new { Movie = m, Metadata = mds.FirstOrDefault() })
            .AsNoTracking();

        // Search filter
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Movie.Title.Contains(search));

        // Status filter
        query = status switch
        {
            "unsynced" => query.Where(x => x.Metadata == null),
            "stale"    => query.Where(x => x.Metadata != null &&
                              (x.Metadata.LastSyncedUtc == null ||
                               x.Metadata.LastSyncedUtc < staleThreshold)),
            "fresh"    => query.Where(x => x.Metadata != null &&
                              x.Metadata.LastSyncedUtc != null &&
                              x.Metadata.LastSyncedUtc >= staleThreshold),
            _          => query   // "all"
        };

        var totalCount = await query.CountAsync();

        // Sort: unsynced first, then stale, then fresh; within each group by title
        var ordered = query
            .OrderBy(x => x.Metadata != null ? 1 : 0)          // nulls (unsynced) first
            .ThenBy(x => x.Metadata != null &&
                         x.Metadata.LastSyncedUtc != null &&
                         x.Metadata.LastSyncedUtc >= staleThreshold ? 1 : 0) // stale before fresh
            .ThenBy(x => x.Movie.Title);

        var pageNum = Math.Max(1, page);
        var rows = await ordered
            .Skip((pageNum - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TmdbSyncMovieRow
            {
                MovieId       = x.Movie.MovieId,
                Title         = x.Movie.Title,
                Rating        = x.Movie.Rating,
                RuntimeMinutes= x.Movie.RuntimeMinutes,
                TMDbMovieId   = x.Metadata != null ? x.Metadata.TMDbMovieId : (int?)null,
                PosterUrl     = x.Metadata != null && x.Metadata.PosterPath != null
                                    ? "/tmdb/poster/92" + x.Metadata.PosterPath
                                    : null,
                HasTrailer    = x.Metadata != null && x.Metadata.TrailerYouTubeKey != null,
                VoteAverage   = x.Metadata != null ? x.Metadata.VoteAverage : null,
                Popularity    = x.Metadata != null ? x.Metadata.Popularity  : null,
                ReleaseDate   = x.Metadata != null ? x.Metadata.ReleaseDate  : null,
                LastSyncedUtc = x.Metadata != null ? x.Metadata.LastSyncedUtc : null
            })
            .ToListAsync();

        return new TmdbSyncPageViewModel
        {
            TotalMovies        = totalMovies,
            MoviesWithMetadata = moviesWithMetadata,
            MoviesWithPoster   = moviesWithPoster,
            MoviesWithTrailer  = moviesWithTrailer,
            StaleMovies        = staleMovies,
            ApiKeyConfigured   = !string.IsNullOrWhiteSpace(_config["Tmdb:ApiKey"]),
            Movies             = rows,
            SearchTerm         = search,
            StatusFilter       = status,
            Page               = pageNum,
            PageSize           = pageSize,
            TotalCount         = totalCount
        };
    }

    /// <summary>
    /// Builds the <see cref="TmdbSyncViewModel"/> coverage stats from the database.
    /// Never calls the TMDb API — reads only local tables.
    /// </summary>
    private async Task<TmdbSyncViewModel> BuildTmdbSyncViewModelAsync()
    {
        var totalMovies = await _db.Movies.CountAsync();
        var moviesWithMetadata = await _db.MovieTmdbMetadata.CountAsync();
        var moviesWithPoster = await _db.MovieTmdbMetadata.CountAsync(m => m.PosterPath != null);
        var moviesWithTrailer = await _db.MovieTmdbMetadata.CountAsync(m => m.TrailerYouTubeKey != null);

        return new TmdbSyncViewModel
        {
            TotalMovies = totalMovies,
            MoviesWithMetadata = moviesWithMetadata,
            MoviesWithPoster = moviesWithPoster,
            MoviesWithTrailer = moviesWithTrailer,
            ApiKeyConfigured = !string.IsNullOrWhiteSpace(_config["Tmdb:ApiKey"])
        };
    }
}
