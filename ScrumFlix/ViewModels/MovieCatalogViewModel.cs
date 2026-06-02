/*
 * File: /ScrumFlix/ViewModels/MovieCatalogViewModel.cs
 * Description: ViewModel for the movie catalog page. Supports keyword title search,
 *              cascading Genre→Title dropdowns, and genre chip pill filtering.
 *
 * Sprint 5 — Movie Catalog Fix:
 *   - Removed age-restriction fields: ViewerIsGuest, ViewerIsUnder17, ViewerIsUnder18.
 *   - Removed GetRatingState() method — age blocking removed from catalog view.
 *   - Added SelectedMovieId for the AJAX cascade dropdown's data-selected-movie attribute.
 *
 * Phase 3 — Backend Alignment (#27 / P3-1):
 *   - MoviesByGenre tuple updated: MovieName → Title to match canonical Movie.Title
 */

namespace ScrumFlix.ViewModels;

/// <summary>
/// ViewModel for rendering the movie catalog page with title search,
/// cascading genre/title dropdowns, and genre chip pill filters.
/// </summary>
public class MovieCatalogViewModel
{
    /// <summary>Gets or sets the list of movies matching the current filter.</summary>
    public List<Movie> Movies { get; set; } = new();

    /// <summary>
    /// Flat list of all movies for admin catalog table views.
    /// Falls back to <see cref="Movies"/> when not separately populated.
    /// </summary>
    public List<Movie> AllMovies => Movies;

    /// <summary>Gets or sets all distinct genres for the genre chip pills and Genre dropdown.</summary>
    public List<string> Genres { get; set; } = new();

    /// <summary>
    /// Gets or sets movies grouped by genre for the cascading dropdown.
    /// Key = genre name, Value = list of (MovieId, Title) tuples in that genre.
    /// </summary>
    public Dictionary<string, List<(int MovieId, string Title)>> MoviesByGenre { get; set; } = new();

    /// <summary>Gets or sets the current keyword search term (title search bar).</summary>
    public string? SearchTerm { get; set; }

    /// <summary>Gets or sets the currently selected genre filter.</summary>
    public string? SelectedGenre { get; set; }

    /// <summary>
    /// Gets or sets the currently selected movie ID from the cascade dropdown.
    /// Rendered as <c>data-selected-movie</c> on the movie dropdown element so
    /// sf-movie-catalog.js can restore selection on page load (back-navigation,
    /// bookmarked URLs). Zero means no movie is selected.
    /// </summary>
    public int SelectedMovieId { get; set; }
}
