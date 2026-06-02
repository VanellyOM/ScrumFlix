/*
 * File:      /ScrumFlix/Areas/Admin/ViewModels/TmdbSyncPageViewModel.cs
 * Namespace: ScrumFlix.Areas.Admin.ViewModels
 * Purpose:   Strongly-typed ViewModel for the dedicated TMDb Sync management page.
 *
 *            Provides everything needed to render:
 *              - Coverage summary panel (stat cards + progress bar)
 *              - Bulk sync trigger controls (Sync Stale / Force Full Re-sync)
 *              - Per-movie sync status table with individual sync triggers
 *              - Last sync result banner (shown after a POST)
 *              - API key warning banner
 *
 *            Differs from TmdbSyncViewModel (which is a nested panel sub-model
 *            embedded in AdminDashboardViewModel) — this is a full page ViewModel.
 *
 * Phase:     5 — TMDb Sync Dashboard
 * Author:    ScrumFlix Rebuild Team
 */

namespace ScrumFlix.Areas.Admin.ViewModels;

/// <summary>
/// ViewModel for the dedicated TMDb Sync management page.
/// Carries coverage stats, per-movie sync rows, and the result of the last operation.
/// </summary>
public class TmdbSyncPageViewModel
{
    // ── Coverage summary ───────────────────────────────────────────────────

    /// <summary>Total movies in the local Movies table.</summary>
    public int TotalMovies { get; set; }

    /// <summary>Movies that have at least one MovieTmdbMetadata row.</summary>
    public int MoviesWithMetadata { get; set; }

    /// <summary>Movies with a non-null PosterPath.</summary>
    public int MoviesWithPoster { get; set; }

    /// <summary>Movies with a non-null TrailerYouTubeKey.</summary>
    public int MoviesWithTrailer { get; set; }

    /// <summary>Movies whose metadata IsStale (no sync in last 24 h, or never synced).</summary>
    public int StaleMovies { get; set; }

    /// <summary>Whether the TMDb API key is present in configuration.</summary>
    public bool ApiKeyConfigured { get; set; }

    /// <summary>Metadata coverage as a percentage (0–100).</summary>
    public int CoveragePercent =>
        TotalMovies == 0 ? 0 : (int)Math.Round((double)MoviesWithMetadata / TotalMovies * 100);

    // ── Per-movie table ────────────────────────────────────────────────────

    /// <summary>
    /// One row per movie, ordered by sync status (unsynced first, then stale,
    /// then fresh) so the most actionable items appear at the top.
    /// </summary>
    public List<TmdbSyncMovieRow> Movies { get; set; } = new();

    // ── Filter / search ────────────────────────────────────────────────────

    /// <summary>Current search term applied to the movie title column.</summary>
    public string? SearchTerm { get; set; }

    /// <summary>Current status filter: "all" | "unsynced" | "stale" | "fresh".</summary>
    public string StatusFilter { get; set; } = "all";

    // ── Pagination ─────────────────────────────────────────────────────────

    /// <summary>Current page number (1-based).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Rows per page.</summary>
    public int PageSize { get; set; } = 25;

    /// <summary>Total rows matching the current filter (before paging).</summary>
    public int TotalCount { get; set; }

    /// <summary>Total pages derived from TotalCount and PageSize.</summary>
    public int TotalPages => PageSize == 0 ? 1 : (int)Math.Ceiling((double)TotalCount / PageSize);

    // ── Last operation result ──────────────────────────────────────────────

    /// <summary>
    /// Result of the most recent sync operation triggered on this page.
    /// Null when the page is loaded without a prior POST in this request.
    /// </summary>
    public TmdbSyncResult? LastSyncResult { get; set; }

    /// <summary>The MovieId targeted by the last single-movie sync, if applicable.</summary>
    public int? LastSyncedMovieId { get; set; }

    /// <summary>The title of the movie targeted by the last single-movie sync.</summary>
    public string? LastSyncedMovieTitle { get; set; }

    /// <summary>Whether the last bulk sync was a force-all run.</summary>
    public bool LastSyncWasForced { get; set; }
}

/// <summary>
/// One row in the per-movie sync status table.
/// </summary>
public class TmdbSyncMovieRow
{
    /// <summary>Local ScrumFlix movie identifier.</summary>
    public int MovieId { get; set; }

    /// <summary>Movie title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>MPA rating (G, PG, PG-13, R, etc.).</summary>
    public string Rating { get; set; } = string.Empty;

    /// <summary>Runtime in minutes.</summary>
    public short RuntimeMinutes { get; set; }

    /// <summary>TMDb movie identifier. Null if never synced.</summary>
    public int? TMDbMovieId { get; set; }

    /// <summary>Proxied poster thumbnail URL. Null if no poster synced yet.</summary>
    public string? PosterUrl { get; set; }

    /// <summary>Whether a YouTube trailer key has been synced.</summary>
    public bool HasTrailer { get; set; }

    /// <summary>TMDb vote average (0.0–10.0). Null if not synced.</summary>
    public decimal? VoteAverage { get; set; }

    /// <summary>TMDb popularity score. Null if not synced.</summary>
    public decimal? Popularity { get; set; }

    /// <summary>TMDb release date. Null if not synced or not available.</summary>
    public DateOnly? ReleaseDate { get; set; }

    /// <summary>UTC timestamp of the last successful sync. Null if never synced.</summary>
    public DateTime? LastSyncedUtc { get; set; }

    /// <summary>
    /// Sync status for display and row colouring.
    /// "Never" — no metadata row exists.
    /// "Stale" — metadata exists but older than 24 h.
    /// "Fresh" — metadata exists and within 24 h.
    /// </summary>
    public string SyncStatus =>
        LastSyncedUtc is null ? "Never" :
        (DateTime.UtcNow - LastSyncedUtc.Value).TotalHours > 24 ? "Stale" : "Fresh";

    /// <summary>Bootstrap badge CSS class driven by SyncStatus.</summary>
    public string StatusBadgeClass => SyncStatus switch
    {
        "Fresh"  => "sf-badge-ok",
        "Stale"  => "sf-badge-low-stock",
        "Never"  => "sf-badge-oos",
        _        => "bg-secondary"
    };

    /// <summary>Human-readable last synced timestamp.</summary>
    public string LastSyncedDisplay =>
        LastSyncedUtc is null
            ? "Never"
            : LastSyncedUtc.Value.ToLocalTime().ToString("MM/dd/yy h:mm tt");
}
