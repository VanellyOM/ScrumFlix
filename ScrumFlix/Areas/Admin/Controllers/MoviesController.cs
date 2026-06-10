// File: Areas/Admin/Controllers/MoviesController.cs
// Staff Portal movie management — catalog view, create, edit, and delete.
// Updated: genre management via MovieFormViewModel with multiselect genre dropdown
// and MovieGenres join table instead of the legacy plain-text Genre field.
// Updated: MovieDelete performs a safe ordered cascade before removing the movie,
// because all FK relationships are configured with OnDelete(DeleteBehavior.Restrict).

namespace ScrumFlix.Areas.Admin.Controllers;

[Area("Admin")]
public class MoviesController : StaffControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<MoviesController> _logger;

    public MoviesController(AppDbContext db, ILogger<MoviesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// GET /Admin/Movies/MovieCatalog
    /// Staff portal table view of the movie catalog with server-side sort,
    /// filter (title/genre/rating/runtime/poster/trailer), and pagination.
    /// Requires Manager or Admin role.
    /// </summary>
    public async Task<IActionResult> MovieCatalog(
        string? search,
        string? genre,
        string? rating,
        int?    runtimeMin,
        int?    runtimeMax,
        string? poster,
        string? trailer,
        string? sortBy   = null,
        bool    sortDesc = false,
        int     page     = 1)
    {
        if (RoleGuard(2) is { } r) return r;

        const int pageSize = 25;

        var query = _db.Movies
            .Include(m => m.TmdbMetadata)
            .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .AsNoTracking();

        // ── Filters ───────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => m.Title.Contains(search));

        if (!string.IsNullOrWhiteSpace(genre))
            query = query.Where(m => m.Genre == genre);

        if (!string.IsNullOrWhiteSpace(rating))
            query = query.Where(m => m.Rating == rating);

        if (runtimeMin.HasValue)
            query = query.Where(m => m.RuntimeMinutes >= runtimeMin.Value);

        if (runtimeMax.HasValue)
            query = query.Where(m => m.RuntimeMinutes <= runtimeMax.Value);

        // poster / trailer: "yes" = has metadata field, "no" = null
        bool? filterPoster  = poster  == "yes" ? true : poster  == "no" ? false : null;
        bool? filterTrailer = trailer == "yes" ? true : trailer == "no" ? false : null;

        if (filterPoster.HasValue)
            query = filterPoster.Value
                ? query.Where(m => m.TmdbMetadata != null && m.TmdbMetadata.PosterPath != null)
                : query.Where(m => m.TmdbMetadata == null || m.TmdbMetadata.PosterPath == null);

        if (filterTrailer.HasValue)
            query = filterTrailer.Value
                ? query.Where(m => m.TmdbMetadata != null && m.TmdbMetadata.TrailerYouTubeKey != null)
                : query.Where(m => m.TmdbMetadata == null || m.TmdbMetadata.TrailerYouTubeKey == null);

        // ── Sort ──────────────────────────────────────────────────────────
        query = sortBy switch
        {
            "genre"    => sortDesc ? query.OrderByDescending(m => m.Genre).ThenBy(m => m.Title)
                                   : query.OrderBy(m => m.Genre).ThenBy(m => m.Title),
            "rating"   => sortDesc ? query.OrderByDescending(m => m.Rating).ThenBy(m => m.Title)
                                   : query.OrderBy(m => m.Rating).ThenBy(m => m.Title),
            "runtime"  => sortDesc ? query.OrderByDescending(m => m.RuntimeMinutes).ThenBy(m => m.Title)
                                   : query.OrderBy(m => m.RuntimeMinutes).ThenBy(m => m.Title),
            // poster/trailer: true (has it) sorts before false (missing) in ascending = has-it-first
            "poster"   => sortDesc ? query.OrderBy(m => m.TmdbMetadata!.PosterPath == null).ThenBy(m => m.Title)
                                   : query.OrderByDescending(m => m.TmdbMetadata!.PosterPath == null).ThenBy(m => m.Title),
            "trailer"  => sortDesc ? query.OrderBy(m => m.TmdbMetadata!.TrailerYouTubeKey == null).ThenBy(m => m.Title)
                                   : query.OrderByDescending(m => m.TmdbMetadata!.TrailerYouTubeKey == null).ThenBy(m => m.Title),
            _          => sortDesc ? query.OrderByDescending(m => m.Title)
                                   : query.OrderBy(m => m.Title)
        };

        // ── Pagination ────────────────────────────────────────────────────
        var total  = await query.CountAsync();
        var movies = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        // Populate filter dropdowns from the full unfiltered catalog so options
        // don't disappear when a filter is active.
        var allMovies = await _db.Movies.AsNoTracking().ToListAsync();

        var vm = new AdminMovieCatalogViewModel
        {
            Movies          = movies,
            Genres          = allMovies.Select(m => m.Genre).Where(g => !string.IsNullOrWhiteSpace(g))
                                       .Distinct().OrderBy(g => g).ToList(),
            Ratings         = allMovies.Select(m => m.Rating).Where(r => !string.IsNullOrWhiteSpace(r))
                                       .Distinct()
                                       .OrderBy(r => r switch { "G" => 0, "PG" => 1, "PG-13" => 2, "R" => 3, "NC-17" => 4, _ => 5 })
                                       .ToList(),
            SearchTerm      = search,
            FilterGenre     = genre,
            FilterRating    = rating,
            FilterRuntimeMin = runtimeMin,
            FilterRuntimeMax = runtimeMax,
            FilterPoster    = filterPoster,
            FilterTrailer   = filterTrailer,
            Page            = page,
            PageSize        = pageSize,
            TotalCount      = total,
            SortBy          = sortBy,
            SortDesc        = sortDesc
        };

        _logger.LogInformation("Staff {User} viewed Admin MovieCatalog ({Count} movies, page {Page}).",
            CurrentUserName, total, page);

        return View(vm);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads all active genres for the form dropdown, ordered alphabetically.
    /// </summary>
    private async Task<List<Genre>> LoadGenresAsync() =>
        await _db.Genres
            .Where(g => g.IsActive)
            .OrderBy(g => g.Name)
            .AsNoTracking()
            .ToListAsync();

    /// <summary>
    /// Builds a <see cref="MovieFormViewModel"/> from an existing <see cref="Movie"/>
    /// with its current genre associations loaded for pre-selection in the edit form.
    /// </summary>
    private async Task<MovieFormViewModel> BuildEditVmAsync(Movie movie)
    {
        var genres          = await LoadGenresAsync();
        var movieGenres     = await _db.MovieGenres
            .Where(mg => mg.MovieId == movie.MovieId)
            .AsNoTracking()
            .ToListAsync();

        var primaryGenre    = movieGenres.FirstOrDefault(mg => mg.IsPrimaryGenre);
        var selectedIds     = movieGenres.Select(mg => mg.GenreId).ToList();

        return new MovieFormViewModel
        {
            MovieId          = movie.MovieId,
            Title            = movie.Title,
            Genre            = movie.Genre,
            Rating           = movie.Rating,
            RuntimeMinutes   = movie.RuntimeMinutes,
            Description      = movie.Description,
            PrimaryGenreId   = primaryGenre?.GenreId ?? (selectedIds.FirstOrDefault()),
            SelectedGenreIds = selectedIds,
            AvailableGenres  = genres
        };
    }

    // ── Create ─────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /Admin/Movies/MovieCreate
    /// Renders the add-movie form inside the Admin layout.
    /// Requires Manager or Admin role.
    /// </summary>
    public async Task<IActionResult> MovieCreate()
    {
        if (RoleGuard(2) is { } r) return r;
        return View(new MovieFormViewModel { AvailableGenres = await LoadGenresAsync() });
    }

    /// <summary>
    /// POST /Admin/Movies/MovieCreate
    /// Persists a new movie with its genre associations and redirects to Admin MovieCatalog.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MovieCreate(MovieFormViewModel vm)
    {
        if (RoleGuard(2) is { } r) return r;

        if (vm.SelectedGenreIds.Count == 0)
            ModelState.AddModelError(nameof(vm.SelectedGenreIds), "Select at least one genre.");

        if (vm.PrimaryGenreId == 0 || !vm.SelectedGenreIds.Contains(vm.PrimaryGenreId))
            ModelState.AddModelError(nameof(vm.PrimaryGenreId), "Primary genre must be one of the selected genres.");

        if (!ModelState.IsValid)
        {
            vm.AvailableGenres = await LoadGenresAsync();
            return View(vm);
        }

        // Resolve primary genre name for the legacy Movie.Genre column.
        var primaryGenre = await _db.Genres.FindAsync(vm.PrimaryGenreId);

        var movie = new Movie
        {
            Title          = vm.Title,
            Genre          = primaryGenre?.Name ?? vm.Genre,
            Rating         = vm.Rating,
            RuntimeMinutes = vm.RuntimeMinutes,
            Description    = vm.Description
        };

        _db.Movies.Add(movie);
        await _db.SaveChangesAsync(); // get MovieId

        // Write MovieGenres join rows
        // Deduplicate and validate genre IDs before adding MovieGenre rows
        var validGenreIds = _db.Genres
            .Where(g => vm.SelectedGenreIds.Contains(g.GenreId))
            .Select(g => g.GenreId)
            .ToHashSet();

        foreach (var genreId in vm.SelectedGenreIds.Distinct())
        {
            if (!validGenreIds.Contains(genreId))
                continue;

            _db.MovieGenres.Add(new MovieGenre
            {
                MovieId        = movie.MovieId,
                GenreId        = genreId,
                IsPrimaryGenre = genreId == vm.PrimaryGenreId,
                CreatedUtc     = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();

        _logger.LogInformation("Staff {User} created MovieId={MovieId} '{Title}' with {Count} genre(s).",
            CurrentUserName, movie.MovieId, movie.Title, vm.SelectedGenreIds.Count);

        TempData["SuccessMessage"] = $"'{movie.Title}' has been added to the catalog.";
        return RedirectToAction(nameof(MovieCatalog));
    }

    // ── Edit ───────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /Admin/Movies/MovieEdit/{id}
    /// Renders the edit form for an existing movie inside the Admin layout.
    /// </summary>
    public async Task<IActionResult> MovieEdit(int id)
    {
        if (RoleGuard(2) is { } r) return r;
        var movie = await _db.Movies.FindAsync(id);
        if (movie == null) return NotFound();
        return View(await BuildEditVmAsync(movie));
    }

    /// <summary>
    /// POST /Admin/Movies/MovieEdit/{id}
    /// Saves edits to an existing movie and its genre associations,
    /// then redirects back to the Admin MovieCatalog.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MovieEdit(int id, MovieFormViewModel vm)
    {
        if (RoleGuard(2) is { } r) return r;
        if (id != vm.MovieId) return BadRequest();

        if (vm.SelectedGenreIds.Count == 0)
            ModelState.AddModelError(nameof(vm.SelectedGenreIds), "Select at least one genre.");

        if (vm.PrimaryGenreId == 0 || !vm.SelectedGenreIds.Contains(vm.PrimaryGenreId))
            ModelState.AddModelError(nameof(vm.PrimaryGenreId), "Primary genre must be one of the selected genres.");

        if (!ModelState.IsValid)
        {
            vm.AvailableGenres = await LoadGenresAsync();
            return View(vm);
        }

        var movie = await _db.Movies.FindAsync(id);
        if (movie == null) return NotFound();

        var primaryGenre = await _db.Genres.FindAsync(vm.PrimaryGenreId);

        movie.Title          = vm.Title;
        movie.Genre          = primaryGenre?.Name ?? vm.Genre;
        movie.Rating         = vm.Rating;
        movie.RuntimeMinutes = vm.RuntimeMinutes;
        movie.Description    = vm.Description;

        // Replace genre associations: remove all existing, add the new selection.
        var existing = await _db.MovieGenres
            .Where(mg => mg.MovieId == id)
            .ToListAsync();
        _db.MovieGenres.RemoveRange(existing);

        // Deduplicate and validate genre IDs before adding MovieGenre rows
        var validGenreIds = _db.Genres
            .Where(g => vm.SelectedGenreIds.Contains(g.GenreId))
            .Select(g => g.GenreId)
            .ToHashSet();

        foreach (var genreId in vm.SelectedGenreIds.Distinct())
        {
            if (!validGenreIds.Contains(genreId))
                continue;

            _db.MovieGenres.Add(new MovieGenre
            {
                MovieId        = movie.MovieId,
                GenreId        = genreId,
                IsPrimaryGenre = genreId == vm.PrimaryGenreId,
                CreatedUtc     = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Staff {User} updated MovieId={MovieId} '{Title}' with {Count} genre(s).",
            CurrentUserName, movie.MovieId, movie.Title, vm.SelectedGenreIds.Count);

        TempData["SuccessMessage"] = $"'{movie.Title}' has been updated.";
        return RedirectToAction(nameof(MovieCatalog));
    }

    // ── Delete ─────────────────────────────────────────────────────────────

    /// <summary>
    /// POST /Admin/Movies/MovieDelete
    /// Permanently removes a movie and all its dependent data in the correct
    /// FK order, since every relationship uses OnDelete(DeleteBehavior.Restrict).
    ///
    /// Deletion order:
    ///   1. SeatReservations  (child of ShowtimeSeat — must go first)
    ///   2. Tickets           (child of Showtime + ShowtimeSeat)
    ///   3. ShowtimeSeats     (child of Showtime)
    ///   4. ScheduleAssignments (child of Showtime)
    ///   5. Showtimes
    ///   6. MovieGenres       (join rows — child of Movie)
    ///   7. MovieTmdbMetadata (one-to-one enrichment)
    ///   8. Movie             (the root)
    ///
    /// Admin role required (destructive action).
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MovieDelete(int id)
    {
        if (RoleGuard(1) is { } r) return r;   // Admin only — destructive

        var movie = await _db.Movies.FindAsync(id);
        if (movie == null)
        {
            TempData["ErrorMessage"] = "Movie not found.";
            return RedirectToAction(nameof(MovieCatalog));
        }

        var title = movie.Title;

        // ── 1. Collect showtime IDs for this movie ─────────────────────────
        var showtimeIds = await _db.Showtimes
            .Where(s => s.MovieId == id)
            .Select(s => s.ShowtimeId)
            .ToListAsync();

        if (showtimeIds.Count > 0)
        {
            // ── 2. ShowtimeSeat IDs (needed to find SeatReservations) ───────
            var showtimeSeatIds = await _db.ShowtimeSeats
                .Where(ss => showtimeIds.Contains(ss.ShowtimeId))
                .Select(ss => ss.ShowtimeSeatId)
                .ToListAsync();

            // ── 3. Delete SeatReservations (child of ShowtimeSeat) ──────────
            if (showtimeSeatIds.Count > 0)
            {
                var reservations = await _db.SeatReservations
                    .Where(r => showtimeSeatIds.Contains(r.ShowtimeSeatId))
                    .ToListAsync();
                _db.SeatReservations.RemoveRange(reservations);
                await _db.SaveChangesAsync();
            }

            // ── 4. Delete Tickets (child of Showtime + ShowtimeSeat) ────────
            var tickets = await _db.Tickets
                .Where(t => showtimeIds.Contains(t.ShowtimeId))
                .ToListAsync();
            _db.Tickets.RemoveRange(tickets);
            await _db.SaveChangesAsync();

            // ── 5. Delete ShowtimeSeats ─────────────────────────────────────
            var showtimeSeats = await _db.ShowtimeSeats
                .Where(ss => showtimeIds.Contains(ss.ShowtimeId))
                .ToListAsync();
            _db.ShowtimeSeats.RemoveRange(showtimeSeats);
            await _db.SaveChangesAsync();

            // ── 6. Delete ScheduleAssignments ──────────────────────────────
            var assignments = await _db.ScheduleAssignments
                .Where(a => a.ShowtimeId.HasValue && showtimeIds.Contains(a.ShowtimeId.Value))
                .ToListAsync();
            _db.ScheduleAssignments.RemoveRange(assignments);
            await _db.SaveChangesAsync();

            // ── 7. Delete Showtimes ─────────────────────────────────────────
            var showtimes = await _db.Showtimes
                .Where(s => showtimeIds.Contains(s.ShowtimeId))
                .ToListAsync();
            _db.Showtimes.RemoveRange(showtimes);
            await _db.SaveChangesAsync();
        }

        // ── 8. Delete MovieGenres (join rows) ──────────────────────────────
        var movieGenres = await _db.MovieGenres
            .Where(mg => mg.MovieId == id)
            .ToListAsync();
        _db.MovieGenres.RemoveRange(movieGenres);
        await _db.SaveChangesAsync();

        // ── 9. Delete TmdbMetadata (one-to-one) ────────────────────────────
        var tmdbMeta = await _db.MovieTmdbMetadata
            .FirstOrDefaultAsync(m => m.MovieId == id);
        if (tmdbMeta != null)
        {
            _db.MovieTmdbMetadata.Remove(tmdbMeta);
            await _db.SaveChangesAsync();
        }

        // ── 10. Delete the Movie itself ────────────────────────────────────
        _db.Movies.Remove(movie);
        await _db.SaveChangesAsync();

        _logger.LogWarning(
            "Staff {User} deleted MovieId={MovieId} '{Title}' along with {ShowtimeCount} showtime(s).",
            CurrentUserName, id, title, showtimeIds.Count);

        TempData["SuccessMessage"] = $"'{title}' and all associated showtimes have been permanently removed.";
        return RedirectToAction(nameof(MovieCatalog));
    }

    // ── Preview (modal) ────────────────────────────────────────────────────

    /// <summary>
    /// GET /Admin/Movies/MovieDetailPreview/{id}
    /// Returns the _MovieDetailPartial rendered without a layout, for injection
    /// into the Admin MovieCatalog preview modal via fetch().
    /// Requires Manager or Admin role (same as MovieCatalog).
    /// </summary>
    public async Task<IActionResult> MovieDetailPreview(int id)
    {
        if (RoleGuard(2) is { } r) return r;

        var movie = await _db.Movies
            .Include(m => m.TmdbMetadata)
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MovieId == id);

        if (movie == null) return NotFound();

        var shows = await _db.Showtimes
            .Where(st => st.MovieId == id && st.IsActive && st.StartTime >= DateTime.Today)
            .Include(st => st.TheaterScreen)
                .ThenInclude(ts => ts!.Location)
            .Include(st => st.Tickets)
            .Include(st => st.ShowtimeSeats)
            .AsNoTracking()
            .OrderBy(st => st.StartTime)
            .ToListAsync();

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
            SelectedLocationId = null   // modal always shows all locations
        };

        // Signal the partial to suppress back links and adjust CTA behaviour
        ViewData["IsModalPreview"] = true;

        return PartialView("~/Views/Movies/_MovieDetailPartial.cshtml", vm);
    }
}
