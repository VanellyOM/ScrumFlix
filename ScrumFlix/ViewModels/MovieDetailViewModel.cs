/*
 * File: /ScrumFlix/ViewModels/MovieDetailViewModel.cs
 * Description: ViewModel for the movie detail page showing metadata and showtimes.
 *
 * Sprint 5 — Movie Catalog Fix:
 *   - Removed age-restriction fields: ViewerIsGuest, ViewerIsUnder17, ViewerIsUnder18.
 *   - Removed RatingState computed property.
 *   - Age overlay removal from MovieDetail.cshtml is a separate follow-up task.
 *
 * Phase 3 — Backend Alignment (#27 / P3-1):
 *   - UpcomingShows changed from List<ScheduledShow> → List<Showtime>
 *   - RatingState switch updated: Movie.MpaRating → Movie.Rating (canonical column)
 *
 * Phase 2 — TMDB Wiring Fix:
 *   Added TmdbMetadata property so MovieDetail.cshtml can access computed URL helpers
 *   (PosterUrl, BackdropUrl, TrailerEmbedUrl) and display fields (VoteAverage,
 *   Popularity, OriginalTitle, ReleaseDate) without coupling the view to the
 *   nav-property load state of Model.Movie.TmdbMetadata.
 *
 *   The view should always null-check this property:
 *     @if (Model.TmdbMetadata is not null) { ... }
 *   It will be null for movies not yet synced by TmdbSyncService. The poster
 *   and trailer areas should render their placeholder fallbacks in that case.
 *
 *   Helper properties HasPoster and HasTrailer are provided so the view does
 *   not need to repeat null chains.
 */

namespace ScrumFlix.ViewModels;

/// <summary>
/// ViewModel for the movie detail page, including movie metadata, TMDb enrichment,
/// and upcoming showtimes.
/// </summary>
public class MovieDetailViewModel
{
    /// <summary>Gets or sets the movie being displayed.</summary>
    public Movie Movie { get; set; } = null!;

    /// <summary>
    /// Gets or sets the TMDb enrichment metadata for this movie.
    /// Null if TmdbSyncService has not yet run for this movie — the view
    /// must render poster and trailer placeholders when this is null.
    /// Populated from <c>Movie.TmdbMetadata</c> after the query includes it.
    /// </summary>
    public MovieTmdbMetadata? TmdbMetadata { get; set; }

    /// <summary>
    /// Gets or sets the upcoming showtimes for this movie.
    /// Queried from the canonical Showtime table, ordered by StartTime.
    /// Includes ShowtimeSeats so the view can check HasSeatsSeeded per showtime.
    /// </summary>
    public List<Showtime> UpcomingShows { get; set; } = new();

    /// <summary>
    /// Gets or sets the active theater locations that have at least one upcoming
    /// showtime for this movie. Used to render the location filter tab strip.
    /// Empty when no location filtering is available.
    /// </summary>
    public List<Location> AvailableLocations { get; set; } = new();

    /// <summary>
    /// Gets or sets the currently selected location filter.
    /// Null means all locations are shown (unfiltered).
    /// </summary>
    public int? SelectedLocationId { get; set; }

    // ── Location / seat helpers ──────────────────────────────────────────

    /// <summary>
    /// True when a location filter is active.
    /// </summary>
    public bool IsLocationFiltered => SelectedLocationId.HasValue;

    /// <summary>
    /// Returns true when the given showtime has ShowtimeSeat rows seeded.
    /// Showtimes without seat rows cannot accept seat-picker bookings —
    /// the view uses this to render a disabled Buy Tickets button with a
    /// "Seat selection unavailable" tooltip rather than sending the user
    /// to a booking page that always bounces back with seat errors.
    /// </summary>
    public static bool HasSeatsSeeded(Showtime showtime)
        => showtime.ShowtimeSeats.Any();

    // ── TMDB convenience helpers ─────────────────────────────────────────

    /// <summary>
    /// True if a real poster image is available from TMDb.
    /// False if metadata is not yet synced or TMDb had no poster for this movie.
    /// Use to switch between the real poster img and the sf-poster-placeholder div.
    /// </summary>
    public bool HasPoster => TmdbMetadata?.PosterUrl is not null;

    /// <summary>
    /// True if a YouTube trailer key is available from TMDb.
    /// False if metadata is not yet synced or TMDb had no trailer.
    /// Use to switch between the iframe embed and the sf-trailer-placeholder div.
    /// </summary>
    public bool HasTrailer => TmdbMetadata?.TrailerEmbedUrl is not null;

    /// <summary>
    /// True if a backdrop image is available from TMDb.
    /// Useful for a hero banner behind the movie title / metadata block.
    /// </summary>
    public bool HasBackdrop => TmdbMetadata?.BackdropUrl is not null;

    /// <summary>
    /// The TMDb vote average formatted for display (e.g. "7.4").
    /// Returns null if metadata is not available or VoteAverage is null.
    /// </summary>
    public string? VoteAverageDisplay =>
        TmdbMetadata?.VoteAverage?.ToString("F1");

    /// <summary>
    /// True if the TMDb vote count is sufficient to display the rating
    /// (suppresses ratings with fewer than 10 votes, which are statistically meaningless).
    /// </summary>
    public bool HasReliableRating =>
        TmdbMetadata?.VoteCount >= 10 && TmdbMetadata?.VoteAverage is not null;
}
