/*
 * File:      /ScrumFlix/ViewModels/HomeDashboardViewModel.cs
 * Namespace: ScrumFlix.ViewModels
 * Purpose:   Strongly-typed ViewModel for the home dashboard page.
 *            Replaces ViewBag.FeaturedMovies and ViewBag.NowShowing.
 *
 *            Data shape:
 *              FeaturedMovies    — up to 10 distinct Movie entities that have at
 *                                  least one active Showtime in the next 3 days.
 *              FeaturedShowtimes — next 3 upcoming active showtimes per featured
 *                                  MovieId, keyed by MovieId for O(1) view lookup.
 *              NowShowing        — up to 8 active Showtime entities for today,
 *                                  with Movie, TheaterScreen, Location, and
 *                                  Tickets nav properties loaded (needed for
 *                                  AvailableSeats and location display).
 *
 * Sprint: S1 — ViewBag Purge
 * Carousel: Take cap raised to 10; FeaturedShowtimes added for pill display.
 */

namespace ScrumFlix.ViewModels;

/// <summary>
/// ViewModel for the ScrumFlix home dashboard. Carries featured movies,
/// per-movie upcoming showtime pills, and today's showtimes for display
/// in <c>HomeDashboard.cshtml</c>.
/// </summary>
public class HomeDashboardViewModel
{
    /// <summary>
    /// Distinct movies that have at least one active showtime within
    /// the next three days. Maximum of 10 items for the featured carousel.
    /// </summary>
    public List<Movie> FeaturedMovies { get; set; } = new();

    /// <summary>
    /// Next 3 upcoming active showtimes keyed by <see cref="Movie.MovieId"/>.
    /// Used to render showtime pills inside each featured carousel card.
    /// Showtimes are ordered by <see cref="Showtime.StartTime"/> ascending.
    /// Movies with no upcoming showtimes will have no entry in this dictionary;
    /// the view should guard with <c>TryGetValue</c> or <c>ContainsKey</c>.
    /// </summary>
    public Dictionary<int, List<Showtime>> FeaturedShowtimes { get; set; } = new();

    /// <summary>
    /// Active showtimes starting today, ordered by start time.
    /// Includes <see cref="Movie"/>, <see cref="TheaterScreen"/>,
    /// <see cref="Location"/>, and <see cref="Ticket"/> collections
    /// so the view can display location name and available seat count
    /// without additional queries.
    /// Maximum of 8 items for the now-showing strip.
    /// </summary>
    public List<Showtime> NowShowing { get; set; } = new();

    /// <summary>Returns <see langword="true"/> if there are no featured movies.</summary>
    public bool HasFeaturedMovies => FeaturedMovies.Any();

    /// <summary>Returns <see langword="true"/> if there are showtimes running today.</summary>
    public bool HasNowShowing => NowShowing.Any();
}