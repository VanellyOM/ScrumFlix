/*
 * File: /ScrumFlix/Controllers/MoviesController.cs
 * Description: Controller for browsing movies, viewing details, and managing CRUD operations.
 *              Supports keyword title search, cascading Genre→Title dropdown, genre chip pills,
 *              and an AJAX endpoint for the cascading movie dropdown (sf-movie-catalog.js).
 *
 * Sprint 5 — Movie Catalog Fix:
 *   - Added GetMoviesByGenre() AJAX endpoint for the cascading dropdown.
 *   - Removed GetViewerAgeContext() private method.
 *   - Removed age-restriction fields from MovieCatalog and MovieDetail vm initialisers.
 *   - Age blocking is now handled entirely at the view/UX layer and has been simplified
 *     away from MovieCatalog; MovieDetail age overlay removal is a separate task.
 *
 * Location filter addition:
 *   MovieDetail now accepts an optional locationId parameter.
 *   Showtimes are filtered to the selected location when provided.
 *   ShowtimeSeats is included so the view can disable Buy Tickets for
 *   showtimes that have no seeded ShowtimeSeat rows.
 *   AvailableLocations is loaded for the location filter tab UI.
 *
 * Phase 3 — Backend Alignment (#27 / P3-1):
 *   - All references to phantom Movie.MovieName → Movie.Title
 *   - All references to phantom Movie.MpaRating → Movie.Rating
 *   - All references to phantom Movie.RunTime → Movie.RuntimeMinutes
 *   - MovieDetail: replaced _db.ScheduledShows with _db.Showtimes; ShowDate/StartDateTime → StartTime
 *   - MovieDetail: replaced TheaterRoom include with TheaterScreen+Location ThenInclude
 *   - MovieDetailViewModel.UpcomingShows type changed to List<Showtime>
 *   - MovieCatalogViewModel.MoviesByGenre tuple uses Title instead of MovieName
 *   - Age context session key updated: "CustomerDob" → "UserDob"
 *
 * Phase 2 — TMDB Wiring Fix:
 *   Problem: ITmdbSyncService was injected but never used. It is a write-service
 *   owned by the Admin area; consumer controllers must not call it directly. Removed.
 *
 *   Fixed queries:
 *     MovieCatalog: added .Include(m => m.TmdbMetadata) and
 *       .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre) so posters and
 *       the relational genre model are available to the view without extra queries.
 *     MovieDetail: added .Include(m => m.TmdbMetadata),
 *       .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
 *       so MovieDetailViewModel can surface PosterUrl, BackdropUrl, TrailerEmbedUrl,
 *       and PrimaryGenreDisplay from already-loaded nav properties.
 *
 *   ViewModel change:
 *     MovieDetailViewModel gains a TmdbMetadata property (nullable) to give
 *     MovieDetail.cshtml clean, null-safe access to computed URL helpers
 *     (PosterUrl, BackdropUrl, TrailerEmbedUrl, VoteAverage, Popularity)
 *     without requiring the view to cast through Model.Movie.TmdbMetadata.
 *
 *   Lazy-load on-demand sync (triggering TmdbSyncService from a GET action when
 *   TmdbMetadata is null) is intentionally deferred to a future sprint using the
 *   SyncJobType.LazyLoad background queue pattern. Calling a sync service inline
 *   in a consumer GET request would block the response for seconds.
 *
 * ViewBag removal:
 *   CartService is no longer stored as a private field — it is passed directly to
 *   base(systemAccounts, cart). MoviesController does not use CartService directly;
 *   the base class handles GetItemCount() for the navbar badge via OnActionExecuted().
 *
 * Sprint: S1 base / Phase 2 TMDB wiring fix / Sprint 5 cascade fix
 */

namespace ScrumFlix.Controllers;

/// <summary>
/// Handles all movie-related requests: catalog browsing, detail view, and admin CRUD actions.
/// </summary>
public class MoviesController : ConsumerControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<MoviesController> _logger;

    /// <summary>
    /// Initializes MoviesController with the database context and cart service.
    /// CartService is passed to ConsumerControllerBase for navbar badge population;
    /// MoviesController does not use it directly.
    /// ITmdbSyncService is intentionally NOT injected here — it is a write-service
    /// belonging to the Admin area. See TmdbConfiguration.cs and AdminHomeController.
    /// </summary>
    public MoviesController(AppDbContext db, CartService cart,
       ISystemAccountProvider systemAccounts,
       ILogger<MoviesController> logger)
       : base(systemAccounts, cart)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Role guard for staff-only CRUD actions on this consumer controller.
    /// Returns a redirect if the current session does not have Manager or Admin role.
    /// </summary>
    private IActionResult? StaffGuard()
    {
        var roleId = HttpContext.Session.GetInt32(AuthService.SessionRoleId);
        if (roleId is not null && roleId <= RoleId.Manager)
            return null;

        TempData["ErrorMessage"] = roleId is null
            ? "Please sign in to access this feature."
            : "Access denied. Manager role or above required.";

        return RedirectToAction("Login", "Account", new { area = "" });
    }

    // ── Catalog ────────────────────────────────────────────────────────────

    /// <summary>
    /// Displays the movie catalog with optional keyword search and/or genre filter.
    /// Redirects to MovieDetail when a specific movieId is provided (cascade dropdown).
    ///
    /// Includes TmdbMetadata and MovieGenres nav properties on every movie so the
    /// catalog cards can render real poster images (PosterUrl) and the relational
    /// genre model (PrimaryGenreDisplay) without N+1 queries.
    /// </summary>
    public async Task<IActionResult> MovieCatalog(string? search, string? genre, int? movieId)
    {
        if (movieId.HasValue && movieId.Value > 0)
            return RedirectToAction(nameof(MovieDetail), new { id = movieId.Value });

        // Load the full catalog with TMDB enrichment and genre nav properties.
        // AsNoTracking is safe — catalog is read-only.
        var allMoviesQuery = _db.Movies
            .Include(m => m.TmdbMetadata)
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .AsNoTracking()
            .OrderBy(m => m.Title)
            .AsQueryable();

        var allMovies = await allMoviesQuery.ToListAsync();

        // Apply filters in-memory after loading (catalog is small; avoids EF translation
        // issues with nullable includes. For 1000+ movies, push filters back to SQL.)
        var filtered = allMovies.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
            filtered = filtered.Where(m =>
                m.Title.Contains(search, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(genre))
            filtered = filtered.Where(m => m.Genre == genre);

        // Distinct genres from the full catalog (not just filtered results) for the
        // genre chip pills — filtering should not remove chips for unchosen genres.
        var genres = allMovies
            .Select(m => m.Genre)
            .Distinct()
            .OrderBy(g => g)
            .ToList();

        var moviesByGenre = allMovies
            .GroupBy(m => m.Genre)
            .ToDictionary(
                g => g.Key,
                g => g.Select(m => (m.MovieId, m.Title)).ToList()
            );

        var vm = new MovieCatalogViewModel
        {
            Movies = filtered.ToList(),
            Genres = genres,
            MoviesByGenre = moviesByGenre,
            SearchTerm = search,
            SelectedGenre = genre,
            SelectedMovieId = movieId ?? 0
        };

        return View(vm);
    }

    // ── AJAX cascade endpoint ──────────────────────────────────────────────

    /// <summary>
    /// Returns movies for a given genre as JSON.
    /// Used by the AJAX cascading dropdown in sf-movie-catalog.js.
    /// </summary>
    /// <param name="genre">The genre string to filter on.</param>
    /// <returns>
    /// A JSON array of <c>{ id, name }</c> objects ordered alphabetically by title,
    /// or an empty array when <paramref name="genre"/> is null or whitespace.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> GetMoviesByGenre(string genre)
    {
        if (string.IsNullOrWhiteSpace(genre))
            return Json(new List<object>());

        var movies = await _db.Movies
            .AsNoTracking()
            .Where(m => m.Genre == genre)
            .OrderBy(m => m.Title)
            .Select(m => new { id = m.MovieId, name = m.Title })
            .ToListAsync();

        return Json(movies);
    }

    // ── Detail ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Displays full details for a single movie including upcoming showtimes.
    /// Includes TmdbMetadata so the view can render the poster, backdrop, trailer
    /// embed, TMDb vote average, and popularity score.
    /// Includes MovieGenres so PrimaryGenreDisplay resolves from the relational
    /// genre model rather than falling back to the legacy Movies.Genre string.
    /// </summary>
    public async Task<IActionResult> MovieDetail(int id, int? locationId)
    {
        var movie = await _db.Movies
            .Include(m => m.TmdbMetadata)
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MovieId == id);

        if (movie == null) return NotFound();

        // Load all active upcoming showtimes for this movie.
        // ShowtimeSeats is included so HasSeatsSeeded can be computed per showtime
        // in the view — showtimes without ShowtimeSeat rows disable the Buy Tickets button
        // to prevent the user reaching a booking page where seat selection always fails.
        var showsQuery = _db.Showtimes
            .Where(st => st.MovieId == id && st.IsActive && st.StartTime >= DateTime.Today)
            .Include(st => st.TheaterScreen)
                .ThenInclude(ts => ts!.Location)
            .Include(st => st.Tickets)          // for AvailableSeats computed prop
            .Include(st => st.ShowtimeSeats)    // for HasSeatsSeeded check in view
            .AsNoTracking()
            .AsQueryable();

        // If a location is selected, filter showtimes to that location only.
        // When no location is selected all showtimes are shown grouped by location.
        if (locationId.HasValue)
            showsQuery = showsQuery
                .Where(st => st.TheaterScreen!.LocationId == locationId.Value);

        var shows = await showsQuery
            .OrderBy(st => st.StartTime)
            .ToListAsync();

        // Load all active locations that have at least one upcoming showtime for
        // this movie — used to render the location filter tabs in the view.
        var availableLocations = await _db.Locations
            .Where(l => l.IsActive && l.TheaterScreens
                .Any(ts => ts.Showtimes
                    .Any(st => st.MovieId == id
                            && st.IsActive
                            && st.StartTime >= DateTime.Today)))
            .OrderBy(l => l.LocationName)
            .AsNoTracking()
            .ToListAsync();

        var vm = new MovieDetailViewModel
        {
            Movie              = movie,
            TmdbMetadata       = movie.TmdbMetadata,
            UpcomingShows      = shows,
            AvailableLocations = availableLocations,
            SelectedLocationId = locationId
        };

        return View(vm);
    }

    // ── Admin CRUD — delegated to Admin area ──────────────────────────────
    // MovieCreate, MovieEdit, and MovieDelete now live in
    // Areas/Admin/Controllers/MoviesController so staff never leaves the
    // Staff Portal. These shims redirect any legacy links to Admin actions.

    /// <summary>Redirects staff to Admin area MovieCreate.</summary>
    public IActionResult MovieCreate()
    {
        if (StaffGuard() is { } redirect) return redirect;
        return RedirectToAction("MovieCreate", "Movies", new { area = "Admin" });
    }

    /// <summary>Redirects staff to Admin area MovieEdit.</summary>
    public IActionResult MovieEdit(int id)
    {
        if (StaffGuard() is { } redirect) return redirect;
        return RedirectToAction("MovieEdit", "Movies", new { area = "Admin", id });
    }

    /// <summary>Redirects staff to Admin area MovieDelete (POST).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult MovieDelete(int id)
    {
        if (StaffGuard() is { } redirect) return redirect;
        return RedirectToAction("MovieDelete", "Movies", new { area = "Admin", id });
    }
}
