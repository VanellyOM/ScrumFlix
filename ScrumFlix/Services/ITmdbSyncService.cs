/*
 * File: /ScrumFlix/Services/ITmdbSyncService.cs
 * Namespace: ScrumFlix.Services
 * Purpose: Contract for the TMDb metadata synchronization service.
 *
 *          TmdbSyncService is the sole writer to the following tables:
 *            Genres            — seeded from TMDb genre list; upserted by TMDbGenreId
 *            MovieGenres       — genre associations per movie; replaced on each sync
 *            MovieTmdbMetadata — one-to-one enrichment per movie; upserted by TMDbMovieId
 *
 *          All public operations are intentionally coarse-grained:
 *            SyncAllMoviesAsync   — full catalog sync (scheduled nightly or on-demand)
 *            SyncMovieAsync(id)   — single movie sync (triggered from Admin UI)
 *            SyncGenresAsync      — refresh genre master list from TMDb
 *
 *          Callers NEVER write to the above three tables directly.
 *          Use TmdbSyncService exclusively.
 *
 * Phase: 2 (new)
 * Author: ScrumFlix Rebuild Team
 */

namespace ScrumFlix.Services;

/// <summary>
/// Contract for syncing movie metadata and genre data from the TMDb API
/// into the MovieTmdbMetadata, Genres, and MovieGenres tables.
/// </summary>
public interface ITmdbSyncService
{
    /// <summary>
    /// Syncs TMDb metadata for every movie in the Movies table.
    /// Skips movies whose metadata was synced within the last 24 hours
    /// unless <paramref name="forceAll"/> is true.
    /// </summary>
    /// <param name="forceAll">When true, re-syncs all movies regardless of LastSyncedUtc.</param>
    /// <param name="cancellationToken">Propagated from the caller or background host.</param>
    /// <returns>A result summarising movies synced, skipped, and failed.</returns>
    Task<TmdbSyncResult> SyncAllMoviesAsync(
        bool              forceAll          = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs TMDb metadata for a single movie identified by its local MovieId.
    /// Always performs a fresh fetch regardless of LastSyncedUtc.
    /// </summary>
    /// <param name="movieId">The local Movies.MovieId to sync.</param>
    /// <param name="cancellationToken">Propagated from the caller.</param>
    /// <returns>True if the sync succeeded; false if TMDb returned no match.</returns>
    Task<bool> SyncMovieAsync(int movieId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the canonical TMDb genre list and upserts into the Genres table.
    /// Should be called before SyncAllMoviesAsync on first run to populate
    /// the Genres master table that MovieGenres rows reference.
    /// </summary>
    /// <param name="cancellationToken">Propagated from the caller.</param>
    /// <returns>The number of genres added or updated.</returns>
    Task<int> SyncGenresAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Summary of a full-catalog sync operation.
/// Returned by <see cref="ITmdbSyncService.SyncAllMoviesAsync"/>.
/// </summary>
public record TmdbSyncResult(int Synced, int Skipped, int Failed)
{
    /// <summary>Total movies processed (Synced + Skipped + Failed).</summary>
    public int Total => Synced + Skipped + Failed;
}
