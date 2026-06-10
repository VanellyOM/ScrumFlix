/*
 * File: /ScrumFlix/Controllers/HomeController.cs
 * Description: Controller for the home/landing page, featuring now-showing movies
 *              and upcoming showtimes.
 *
 * Phase 3 — Backend Alignment (#34 / P3-8):
 *   - Replaced _db.ScheduledShows with _db.Showtimes
 *   - Replaced ShowDate/StartDateTime phantom columns with StartTime (canonical)
 *   - Replaced TheaterRoom include with TheaterScreen include
 *   - AvailableSeats computed from Showtime.AvailableSeats (Capacity - Tickets.Count)
 *
 * Sprint S1 — ViewBag Purge:
 *   - Replaced ViewBag.FeaturedMovies + ViewBag.NowShowing with HomeDashboardViewModel
 *   - Action now returns View(vm) with strongly-typed model
 *   - View updated to @model HomeDashboardViewModel
 *
 * TMDB Fix:
 *   - FeaturedMovies query now includes TmdbMetadata so poster images render
 *     on the home dashboard. Previously the Include was missing, so
 *     movie.TmdbMetadata was always null for featured movies.
 *
 * Carousel upgrade:
 *   - Featured Take cap raised from 6 → 10
 *   - Added FeaturedShowtimes query: next 3 upcoming active showtimes per featured movie,
 *     keyed by MovieId in Dictionary<int, List<Showtime>>
 */

namespace ScrumFlix.Controllers;

/// <summary>
/// Handles requests for the ScrumFlix home dashboard including featured movies
/// and now-showing showtimes.
/// </summary>
public class HomeController : ConsumerControllerBase
{
    private readonly AppDbContext _db;

    /// <summary>
    /// Initializes HomeController with the application database context.
    /// </summary>
    /// <param name="db">The EF Core database context.</param>
    public HomeController(AppDbContext db, CartService cart,
       ISystemAccountProvider systemAccounts)
       : base(systemAccounts, cart)
    {
        _db = db;
    }

    /// <summary>
    /// Renders the home dashboard with featured movies, today's showtimes, and
    /// per-movie upcoming showtime pills, all via a strongly-typed
    /// <see cref="HomeDashboardViewModel"/> — no ViewBag.
    /// </summary>
    /// <returns>The home dashboard view with a typed <see cref="HomeDashboardViewModel"/>.</returns>
    public async Task<IActionResult> HomeDashboard()
    {
        var today = DateTime.UtcNow.Date;
        var todayEnd = today.AddDays(1);
        var windowEnd = today.AddDays(14); // featured = full 14-day showtime window
        var now = DateTime.UtcNow;

        // Featured: distinct movies with any showtime in the next 14 days.
        // Query from Movies table so EF can eager-load TmdbMetadata cleanly —
        // .ThenInclude() after .Select(st => st.Movie!) is not supported by EF Core.
        // The sub-query finds MovieIds that have an active showtime in the window,
        // then the outer query loads those movies with their TMDB poster metadata.
        var featuredMovieIds = await _db.Showtimes
            .Where(st => st.IsActive
                      && st.StartTime >= today
                      && st.StartTime < windowEnd)
            .Select(st => st.MovieId)
            .Distinct()
            .OrderBy(id => id)   // EF Core requires OrderBy before Take to guarantee deterministic results
            .Take(40)            // fetch more than needed — filtered below for complete metadata
            .ToListAsync();

        var featuredMovies = await _db.Movies
            .Where(m => featuredMovieIds.Contains(m.MovieId))
            .Include(m => m.TmdbMetadata)
            // Only feature movies with ALL THREE TMDb fields populated —
            // poster, backdrop, and trailer are all required for a complete
            // carousel card. Movies missing any one field look broken.
            .Where(m => m.TmdbMetadata != null
                     && m.TmdbMetadata.PosterPath        != null
                     && m.TmdbMetadata.BackdropPath      != null
                     && m.TmdbMetadata.TrailerYouTubeKey != null)
            .Take(10)            // carousel supports up to 10 featured movies
            .AsNoTracking()
            .ToListAsync();

        // FeaturedShowtimes: next 3 upcoming active showtimes for each featured movie,
        // starting from now (UTC) so times display correctly across timezones.
        // Keyed by MovieId for O(1) lookup in the Razor view.
        var featuredMovieIdList = featuredMovies.Select(m => m.MovieId).ToList();

        var upcomingShowtimes = await _db.Showtimes
            .Where(st => st.IsActive
                      && st.StartTime >= now
                      && st.StartTime < windowEnd
                      && featuredMovieIdList.Contains(st.MovieId))
            .OrderBy(st => st.StartTime)
            .AsNoTracking()
            .ToListAsync();

        var featuredShowtimes = upcomingShowtimes
            .GroupBy(st => st.MovieId)
            .ToDictionary(
                g => g.Key,
                g => g.Take(3).ToList()
            );

        // Now Showing: active showtimes starting today, with full nav data
        var nowShowing = await _db.Showtimes
            .Where(st => st.IsActive
                      && st.StartTime >= today
                      && st.StartTime < todayEnd)
            .Include(st => st.Movie)
                .ThenInclude(m => m!.TmdbMetadata)    // poster images on showtime cards
            .Include(st => st.TheaterScreen)
                .ThenInclude(ts => ts!.Location)
            .Include(st => st.Tickets)        // needed for AvailableSeats fallback
            .OrderBy(st => st.StartTime)
            .Take(8)
            .AsNoTracking()
            .ToListAsync();

        var vm = new HomeDashboardViewModel
        {
            FeaturedMovies = featuredMovies,
            FeaturedShowtimes = featuredShowtimes,
            NowShowing = nowShowing
        };

        return View(vm);
    }

    // ── Error handlers ─────────────────────────────────────────────────────

    /// <summary>
    /// Handles HTTP errors and unhandled exceptions.
    /// 4xx → Error.cshtml (uses normal layout — navbar/footer visible)
    /// 5xx → Error500.cshtml (standalone HTML, no layout dependency)
    ///
    /// Route matches both /Home/Error (UseExceptionHandler)
    /// and /Home/Error/{code} (UseStatusCodePagesWithReExecute).
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? code)
    {
        var statusCode = code ?? HttpContext.Response.StatusCode;

        var originalPath = HttpContext.Features
            .Get<Microsoft.AspNetCore.Diagnostics.IStatusCodeReExecuteFeature>()?.OriginalPath
            ?? HttpContext.Features
            .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>()?.Path
            ?? "unknown";

        var isStaff = HttpContext.Session.GetInt32(ScrumFlix.Services.AuthService.SessionRoleId) is not null;
        var isDev = HttpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>().IsDevelopment();

        var vm = new ScrumFlix.ViewModels.ErrorViewModel
        {
            StatusCode = statusCode,
            OriginalPath = originalPath,
            IsStaff = isStaff,
            ShowDetail = isDev || isStaff,
            RequestId = System.Diagnostics.Activity.Current?.Id
                             ?? HttpContext.TraceIdentifier
        };

        // 500+ use a standalone view with no layout dependency.
        // 4xx use the normal layout so the navbar and footer remain visible.
        var viewName = statusCode >= 500 ? "Error500" : "Error";
        return View(viewName, vm);
    }

    /// <summary>GET /Home/AccessDenied — shown when a staff user hits a restricted area.</summary>
    public IActionResult AccessDenied() => View();
}