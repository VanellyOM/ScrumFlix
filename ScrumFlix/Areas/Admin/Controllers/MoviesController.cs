// File: Areas/Admin/Controllers/MoviesController.cs
// Staff Portal movie management — catalog view, create, edit, and delete.
// All actions stay within the Admin area; no consumer views are used.

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
    /// Staff portal view of the movie catalog — identical data to the customer view,
    /// but rendered inside the Admin layout with edit/delete action columns.
    /// Requires Manager or Admin role.
    /// </summary>
    public async Task<IActionResult> MovieCatalog(string? search)
    {
        if (RoleGuard(2) is { } r) return r;

        var query = _db.Movies
            .Include(m => m.TmdbMetadata)
            .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => m.Title.Contains(search));

        var movies = await query.OrderBy(m => m.Title).ToListAsync();

        var byGenre = movies
            .GroupBy(m => string.IsNullOrWhiteSpace(m.Genre) ? "Other" : m.Genre)
            .ToDictionary(
                g => g.Key,
                g => g.Select(m => (m.MovieId, m.Title)).ToList());

        var vm = new MovieCatalogViewModel
        {
            Movies        = movies,
            MoviesByGenre = byGenre,
            SearchTerm    = search
        };

        _logger.LogInformation("Staff {User} viewed Admin MovieCatalog ({Count} movies).",
            CurrentUserName, movies.Count);

        return View(vm);
    }

    // ── Create ─────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /Admin/Movies/MovieCreate
    /// Renders the add-movie form inside the Admin layout.
    /// Requires Manager or Admin role.
    /// </summary>
    public IActionResult MovieCreate()
    {
        if (RoleGuard(2) is { } r) return r;
        return View(new Movie());
    }

    /// <summary>
    /// POST /Admin/Movies/MovieCreate
    /// Persists a new movie and redirects back to the Admin MovieCatalog.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MovieCreate(Movie movie)
    {
        if (RoleGuard(2) is { } r) return r;
        if (!ModelState.IsValid) return View(movie);

        _db.Movies.Add(movie);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Staff {User} created MovieId={MovieId} '{Title}'.",
            CurrentUserName, movie.MovieId, movie.Title);

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
        return movie == null ? NotFound() : View(movie);
    }

    /// <summary>
    /// POST /Admin/Movies/MovieEdit/{id}
    /// Saves edits to an existing movie and redirects back to the Admin MovieCatalog.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MovieEdit(int id, Movie movie)
    {
        if (RoleGuard(2) is { } r) return r;
        if (id != movie.MovieId) return BadRequest();
        if (!ModelState.IsValid) return View(movie);

        _db.Movies.Update(movie);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Staff {User} updated MovieId={MovieId} '{Title}'.",
            CurrentUserName, movie.MovieId, movie.Title);

        TempData["SuccessMessage"] = $"'{movie.Title}' has been updated.";
        return RedirectToAction(nameof(MovieCatalog));
    }

    // ── Delete ─────────────────────────────────────────────────────────────

    /// <summary>
    /// POST /Admin/Movies/MovieDelete
    /// Deletes a movie permanently and redirects back to the Admin MovieCatalog.
    /// Admin role required (destructive action).
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MovieDelete(int id)
    {
        if (RoleGuard(1) is { } r) return r;   // Admin only — destructive

        var movie = await _db.Movies.FindAsync(id);
        if (movie != null)
        {
            _db.Movies.Remove(movie);
            await _db.SaveChangesAsync();

            _logger.LogWarning("Staff {User} deleted MovieId={MovieId} '{Title}'.",
                CurrentUserName, id, movie.Title);

            TempData["SuccessMessage"] = $"'{movie.Title}' has been removed from the catalog.";
        }

        return RedirectToAction(nameof(MovieCatalog));
    }
}
