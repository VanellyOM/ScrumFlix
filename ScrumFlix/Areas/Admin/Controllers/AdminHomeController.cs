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


using Microsoft.Extensions.DependencyInjection;
using ScrumFlix.Services.BackgroundQueue;
using ScrumFlix.Services.Progress;

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
    private readonly IProgressReporterFactory _reporterFactory;
    private readonly IBackgroundTaskQueue     _taskQueue;

    public AdminHomeController(
        AppDbContext db,
        ITmdbSyncService tmdb,
        IConfiguration config,
        ILogger<AdminHomeController> logger,
        IProgressReporterFactory reporterFactory,
        IBackgroundTaskQueue taskQueue)
    {
        _db      = db;
        _tmdb    = tmdb;
        _config  = config;
        _logger  = logger;
        _reporterFactory = reporterFactory;
        _taskQueue       = taskQueue;
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
    /// Triggers an on-demand TMDb sync with real-time progress broadcast via
    /// the Phase 4.0 shared progress framework (ProgressHub / sf-progress.js).
    ///
    /// The client (sf-tmdb-sync.js) generates an <paramref name="operationId"/>
    /// before submitting, joins the corresponding ProgressHub group, then
    /// posts the form. Each movie processed adapts the legacy
    /// <see cref="TmdbSyncProgressReport"/> to a <see cref="ProgressState"/>
    /// and reports it via the minted <see cref="IProgressReporter"/>, which
    /// broadcasts a "ProgressUpdate" event to that group.
    ///
    /// On completion or error, reporter.Complete()/Error() sends a terminal
    /// ProgressUpdate so the spinner transitions without polling.
    ///
    /// Single-movie syncs (movieId provided) do not emit per-movie progress
    /// events — they complete too quickly. The redirect back to
    /// TmdbSyncPage carries the result in TempData as before.
    ///
    /// NOTE: /tmdbSyncHub and TmdbProgressHub remain mapped (unused by this
    /// action) until the Phase 4.1 migration is verified end-to-end, per the
    /// Phase 4.0 implementation plan.
    /// </summary>
    /// <param name="forceAll">When <see langword="true"/>, re-syncs all movies regardless of existing metadata.</param>
    /// <param name="movieId">Optional single-movie TMDb sync target.</param>
    /// <param name="operationId">
    /// Client-generated operation id (GUID) used to scope ProgressHub
    /// broadcasts. Required for full-catalog syncs so the client can join the
    /// SignalR group before progress begins; ignored for single-movie syncs.
    /// </param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TmdbSyncRun(bool forceAll = false, int? movieId = null, string? operationId = null)
    {
        if (RoleGuard(1) is { } redirect) return redirect;

        var isAjax = string.Equals(
            Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

        _logger.LogInformation(
            "Admin User {UserId} triggered TMDb sync (forceAll={ForceAll}, movieId={MovieId}, " +
            "operationId={OperationId}, ajax={Ajax}).",
            CurrentUserId, forceAll, movieId, operationId, isAjax);

        // ── Single-movie sync: fast, synchronous, no progress framework. ──────
        // Kept on the original synchronous path (no queue, no reporter) because
        // a single movie completes in well under a second and has no progress UI.
        if (movieId.HasValue)
        {
            TmdbSyncResult singleResult;
            try
            {
                await _tmdb.SyncGenresAsync();
                var success = await _tmdb.SyncMovieAsync(movieId.Value);
                singleResult = success ? new TmdbSyncResult(1, 0, 0) : new TmdbSyncResult(0, 0, 1);

                TempData[success ? "SuccessMessage" : "InfoMessage"] = success
                    ? "Movie TMDb metadata synced successfully."
                    : "No TMDb match found for that movie.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Single-movie TMDb sync failed for MovieId={MovieId}.", movieId);
                singleResult = new TmdbSyncResult(0, 0, 1);
                TempData["ErrorMessage"] = "TMDb sync encountered an unexpected error. Check the application logs.";
            }

            if (isAjax)
            {
                return Json(new
                {
                    operationId = (string?)null,
                    succeeded   = singleResult.Synced,
                    skipped     = singleResult.Skipped,
                    failed      = singleResult.Failed,
                    total       = singleResult.Total,
                });
            }

            var singleVm = await BuildDashboardViewModelAsync();
            singleVm.TmdbSync.LastSyncResult    = singleResult;
            singleVm.TmdbSync.LastSyncWasForced = false;
            return View("AdminDashboard", singleVm);
        }

        // ── Full-catalog sync, no JavaScript (no X-Requested-With) ────────────
        // Graceful degradation: run synchronously inline (the pre-4.3 behaviour)
        // and return the full dashboard view. No reporter is minted because no
        // client is subscribed to ProgressHub. This keeps the feature usable
        // with JS disabled, just without a live progress spinner.
        if (!isAjax)
        {
            TmdbSyncResult? syncResult = null;
            try
            {
                await _tmdb.SyncGenresAsync();
                syncResult = await _tmdb.SyncAllMoviesAsync(forceAll);

                TempData[syncResult.Failed == 0 ? "SuccessMessage" : "InfoMessage"] =
                    syncResult.Failed == 0
                        ? $"TMDb sync complete — {syncResult.Synced} movie(s) synced, {syncResult.Skipped} skipped."
                        : $"TMDb sync finished with {syncResult.Failed} failure(s). " +
                          $"Synced: {syncResult.Synced}, Skipped: {syncResult.Skipped}. Check logs for details.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TMDb sync (non-AJAX) triggered by Admin User {UserId} failed.", CurrentUserId);
                TempData["ErrorMessage"] = "TMDb sync encountered an unexpected error. Check the application logs.";
            }

            var fallbackVm = await BuildDashboardViewModelAsync();
            fallbackVm.TmdbSync.LastSyncResult    = syncResult;
            fallbackVm.TmdbSync.LastSyncWasForced = forceAll;
            return View("AdminDashboard", fallbackVm);
        }

        // ── Full-catalog sync, AJAX: background-queue path (Phase 4.3) ────────
        // Mint the reporter NOW so its operation id is known immediately and the
        // client (already connected to /progressHub on page load) can join the
        // group. The reporter is created with a NON-request-bound token
        // (CancellationToken.None): the HTTP request returns before the work
        // runs, so binding to HttpContext.RequestAborted would cancel the sync
        // the instant the response is sent. User cancellation flows solely via
        // ProgressHub.ClientCancel → IProgressReporterFactory.Cancel →
        // reporter.CancellationToken.
        var reporter = string.IsNullOrWhiteSpace(operationId)
            ? _reporterFactory.Create("TMDb Sync")
            : _reporterFactory.Create(operationId, "TMDb Sync");

        // Capture primitives needed inside the background closure — no HttpContext
        // (and therefore no CurrentUserId/session) is available once enqueued.
        var triggeredByUserId = CurrentUserId ?? 0;
        var force             = forceAll;
        var reporterFactory   = _reporterFactory;  // singleton — safe to use after this request ends

        await _taskQueue.QueueBackgroundWorkItemAsync(async (sp, _) =>
        {
            // Scoped services resolve from the per-item scope created by the host.
            var tmdb   = sp.GetRequiredService<ITmdbSyncService>();
            var logger = sp.GetRequiredService<ILogger<AdminHomeController>>();

            try
            {
                // Always sync genres first — MovieGenres cannot resolve without Genres.
                await tmdb.SyncGenresAsync(reporter.CancellationToken);

                var progress = new Progress<TmdbSyncProgressReport>(report =>
                {
                    reporter.Report(ProgressState.InProgress(
                        operationId:   reporter.OperationId,
                        operationName: "TMDb Sync",
                        status:        report.Message,
                        current:       report.Synced + report.Skipped + report.Failed,
                        total:         report.Total,
                        succeeded:     report.Synced,
                        skipped:       report.Skipped,
                        failed:        report.Failed));
                });

                var result = await tmdb.SyncAllMoviesAsync(force, progress, reporter.CancellationToken);

                logger.LogInformation(
                    "TMDb sync (queued) complete for Admin User {UserId} — {Synced} synced, " +
                    "{Skipped} skipped, {Failed} failed (operationId={OperationId}).",
                    triggeredByUserId, result.Synced, result.Skipped, result.Failed, reporter.OperationId);

                reporter.Complete(
                    $"TMDb sync complete — {result.Synced} synced, {result.Skipped} skipped, {result.Failed} failed.");
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation(
                    "TMDb sync (queued) cancelled (operationId={OperationId}).", reporter.OperationId);
                reporter.Error("Sync cancelled.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "TMDb sync (queued) failed for Admin User {UserId} (operationId={OperationId}).",
                    triggeredByUserId, reporter.OperationId);
                reporter.Error("Sync failed — check application logs.");
            }
            finally
            {
                // Release the cancellation-registry entry now the operation is terminal.
                reporterFactory.Release(reporter.OperationId);
            }
        });

        // Return immediately — the work runs on QueuedHostedService. The client
        // joins reporter.OperationId's ProgressHub group and watches the spinner,
        // then htmx-swaps the coverage stats panel on the terminal ProgressUpdate.
        return Json(new { operationId = reporter.OperationId });
    }

    // ── TMDb Sync coverage stats partial (HTMX swap target) ─────────────────

    /// <summary>
    /// GET /Admin/AdminHome/TmdbSyncStatsPartial
    /// Returns the coverage stat-cards panel as a partial view so the TMDb sync
    /// page can refresh it in place (HTMX outerHTML swap into
    /// <c>#tmdb-coverage-stats</c>) once a queued sync completes — replacing the
    /// previous full-page <c>window.location.reload()</c>.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> TmdbSyncStatsPartial()
    {
        if (RoleGuard(1) is { } redirect) return redirect;

        var vm = await BuildTmdbCoverageStatsAsync();
        return PartialView("_TmdbSyncStatsPartial", vm);
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

    /// <summary>
    /// Builds a coverage-only <see cref="TmdbSyncPageViewModel"/> for the
    /// <c>_TmdbSyncStatsPartial</c> HTMX swap. Computes just the catalog-wide
    /// coverage counters (no per-movie paged query, no API call) — the partial
    /// renders only the stat cards + progress bar, so the heavier per-movie list
    /// built by <see cref="BuildTmdbSyncPageViewModelAsync"/> is intentionally
    /// skipped here.
    /// </summary>
    private async Task<TmdbSyncPageViewModel> BuildTmdbCoverageStatsAsync()
    {
        var staleThreshold = DateTime.UtcNow.AddHours(-24);

        return new TmdbSyncPageViewModel
        {
            TotalMovies        = await _db.Movies.CountAsync(),
            MoviesWithMetadata = await _db.MovieTmdbMetadata.CountAsync(),
            MoviesWithPoster   = await _db.MovieTmdbMetadata.CountAsync(m => m.PosterPath != null),
            MoviesWithTrailer  = await _db.MovieTmdbMetadata.CountAsync(m => m.TrailerYouTubeKey != null),
            StaleMovies        = await _db.MovieTmdbMetadata
                                     .CountAsync(m => m.LastSyncedUtc == null || m.LastSyncedUtc < staleThreshold),
            ApiKeyConfigured   = !string.IsNullOrWhiteSpace(_config["Tmdb:ApiKey"]),
            // Movies left empty — the stats partial does not render the per-movie table.
        };
    }
}
