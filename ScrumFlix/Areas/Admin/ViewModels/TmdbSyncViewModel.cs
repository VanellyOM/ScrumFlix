/*
 * File: /ScrumFlix/Areas/Admin/ViewModels/TmdbSyncViewModel.cs
 * Namespace: ScrumFlix.Areas.Admin.ViewModels
 * Purpose: ViewModel for the TMDb sync panel on the Admin Dashboard.
 *          Carries current sync state and the result of the last sync operation
 *          so the view can show both a trigger form and a result summary.
 *
 * Phase: 2
 * Author: ScrumFlix Rebuild Team
 */

namespace ScrumFlix.Areas.Admin.ViewModels;

/// <summary>
/// Carries TMDb sync status for the admin dashboard sync panel.
/// Populated by AdminHomeController after running or reading the sync state.
/// </summary>
public class TmdbSyncViewModel
{
    /// <summary>Number of movies in the local Movies table.</summary>
    public int TotalMovies { get; set; }

    /// <summary>Number of movies that already have a MovieTmdbMetadata row.</summary>
    public int MoviesWithMetadata { get; set; }

    /// <summary>Number of movies with a non-null PosterPath.</summary>
    public int MoviesWithPoster { get; set; }

    /// <summary>Number of movies with a non-null TrailerYouTubeKey.</summary>
    public int MoviesWithTrailer { get; set; }

    /// <summary>
    /// Whether the TMDb API key is configured.
    /// Drives the "API key missing" warning in the view.
    /// </summary>
    public bool ApiKeyConfigured { get; set; }

    /// <summary>
    /// The result of the most recent sync operation.
    /// Null if no sync has been triggered in this request (page load without POST).
    /// </summary>
    public TmdbSyncResult? LastSyncResult { get; set; }

    /// <summary>
    /// Whether the last sync was a force-all run (showed on result panel).
    /// </summary>
    public bool LastSyncWasForced { get; set; }

    /// <summary>Movies with metadata as a percentage of total, for the progress bar.</summary>
    public int CoveragePercent =>
        TotalMovies == 0 ? 0 : (int)Math.Round((double)MoviesWithMetadata / TotalMovies * 100);
}
