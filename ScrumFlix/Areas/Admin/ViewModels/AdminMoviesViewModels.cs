/*
 * File: Areas/Admin/ViewModels/AdminMoviesViewModels.cs
 * Purpose: ViewModels for the Admin/Movies area.
 *
 * Kept separate from AdminManageViewModels.cs because Movies are managed
 * by a different controller (MoviesController vs AdminManageController).
 *
 * AdminMovieCatalogViewModel replaces the shared consumer MovieCatalogViewModel
 * for the admin table view, adding server-side sort, filter, and pagination
 * without touching the consumer ViewModel.
 */

namespace ScrumFlix.Areas.Admin.ViewModels;

/// <summary>
/// ViewModel for the Admin MovieCatalog table.
/// Supports server-side sort, filter (title/genre/rating/runtime/poster/trailer),
/// and pagination — all preserved across sort and page navigation.
/// </summary>
public class AdminMovieCatalogViewModel
{
    public List<Movie> Movies { get; set; } = new();

    // ── Filter options (populated by controller for dropdowns) ────────────
    /// <summary>Distinct genre names from the live movie catalog, for the Genre dropdown.</summary>
    public List<string> Genres  { get; set; } = new();
    /// <summary>Distinct MPA ratings from the live movie catalog, for the Rating dropdown.</summary>
    public List<string> Ratings { get; set; } = new();

    // ── Active filter values (round-tripped from querystring) ─────────────
    public string? SearchTerm       { get; set; }   // title contains
    public string? FilterGenre      { get; set; }   // exact genre match
    public string? FilterRating     { get; set; }   // exact MPA rating match
    public int?    FilterRuntimeMin { get; set; }   // runtime >= N minutes
    public int?    FilterRuntimeMax { get; set; }   // runtime <= N minutes
    /// <summary>null = all, true = has poster, false = no poster.</summary>
    public bool?   FilterPoster     { get; set; }
    /// <summary>null = all, true = has trailer, false = no trailer.</summary>
    public bool?   FilterTrailer    { get; set; }

    // ── Pagination ────────────────────────────────────────────────────────
    public int Page       { get; set; } = 1;
    public int PageSize   { get; set; } = 25;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    // ── Sort ──────────────────────────────────────────────────────────────
    public string? SortBy   { get; set; }
    public bool    SortDesc { get; set; }

    // ── Convenience ───────────────────────────────────────────────────────
    /// <summary>True when any filter other than the default sort is active — used to show the Clear link.</summary>
    public bool HasActiveFilter =>
        !string.IsNullOrWhiteSpace(SearchTerm) ||
        !string.IsNullOrWhiteSpace(FilterGenre) ||
        !string.IsNullOrWhiteSpace(FilterRating) ||
        FilterRuntimeMin.HasValue ||
        FilterRuntimeMax.HasValue ||
        FilterPoster.HasValue ||
        FilterTrailer.HasValue;
}
