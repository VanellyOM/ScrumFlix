/*
 * File: /ScrumFlix/Domain/MovieTmdbMetadata.cs
 * Description: Canonical MovieTmdbMetadata entity — maps to the MovieTmdbMetadata table in defaultdb.
 *              Net-new for Phase 2 (TMDB integration).
 *
 *              One-to-one with Movie (UQ_MovieTmdbMetadata_MovieId). Stores enrichment
 *              data pulled from the TMDb API by TmdbSyncService. The service must
 *              UPSERT by TMDbMovieId (UQ_MovieTmdbMetadata_TMDbMovieId) so repeated
 *              syncs are idempotent.
 *
 *              PosterPath and BackdropPath store the TMDb path segment (e.g., "/abc123.jpg").
 *              Prepend the TMDb image base URL ("https://image.tmdb.org/t/p/w500") at
 *              render time — do NOT store the full URL, as the base can change.
 *              SixLabors.ImageSharp.Web is configured as the image processing middleware;
 *              poster images may be cached and resized via the /images/ proxy endpoint.
 *
 *              TrailerYouTubeKey stores only the YouTube video key (e.g., "dQw4w9WgXcQ"),
 *              not the full URL. Build the embed URL in the view:
 *                "https://www.youtube.com/embed/{TrailerYouTubeKey}"
 *
 *              LastSyncedUtc tracks when TmdbSyncService last refreshed this record.
 *              TmdbSyncService should skip records synced within the last 24 hours
 *              unless a force-sync flag is passed.
 *
 *              ON DELETE CASCADE: deleting the parent Movie removes this metadata row.
 */

namespace ScrumFlix.Domain;

/// <summary>
/// TMDb-sourced enrichment metadata for a movie. One-to-one with Movie.
/// Maps to: MovieTmdbMetadata (MovieTmdbMetadataId, MovieId, TMDbMovieId,
///          PosterPath, BackdropPath, TrailerYouTubeKey, OriginalTitle,
///          OriginalLanguage, ReleaseDate, Popularity, VoteAverage,
///          VoteCount, LastSyncedUtc, CreatedUtc, UpdatedUtc)
/// </summary>
[Table("MovieTmdbMetadata")]
public class MovieTmdbMetadata
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("MovieTmdbMetadataId")]
    public int MovieTmdbMetadataId { get; set; }

    /// <summary>
    /// FK to the parent Movie. One-to-one; unique per schema (UQ_MovieTmdbMetadata_MovieId).
    /// </summary>
    [Column("MovieId")]
    public int MovieId { get; set; }

    /// <summary>
    /// The TMDb movie identifier used to fetch and refresh this metadata.
    /// Must be unique across all records (UQ_MovieTmdbMetadata_TMDbMovieId).
    /// TmdbSyncService upserts by this value.
    /// </summary>
    [Column("TMDbMovieId")]
    [Display(Name = "TMDb Movie ID")]
    public int TMDbMovieId { get; set; }

    /// <summary>
    /// TMDb poster image path segment (e.g., "/abc123.jpg").
    /// Prepend "https://image.tmdb.org/t/p/w500" at render time.
    /// Null if TMDb has no poster for this movie.
    /// SixLabors.ImageSharp.Web may proxy/cache via the /images/ endpoint.
    /// Max 500 characters per schema.
    /// </summary>
    [MaxLength(500)]
    [Column("PosterPath")]
    [Display(Name = "Poster Path")]
    public string? PosterPath { get; set; }

    /// <summary>
    /// TMDb backdrop image path segment (e.g., "/xyz789.jpg").
    /// Prepend "https://image.tmdb.org/t/p/original" at render time for full-width banners.
    /// Null if TMDb has no backdrop for this movie.
    /// Max 500 characters per schema.
    /// </summary>
    [MaxLength(500)]
    [Column("BackdropPath")]
    [Display(Name = "Backdrop Path")]
    public string? BackdropPath { get; set; }

    /// <summary>
    /// YouTube video key for the official trailer (e.g., "dQw4w9WgXcQ").
    /// Build embed URL as: "https://www.youtube.com/embed/{TrailerYouTubeKey}"
    /// Null if no trailer has been synced from TMDb.
    /// Max 100 characters per schema.
    /// </summary>
    [MaxLength(100)]
    [Column("TrailerYouTubeKey")]
    [Display(Name = "Trailer (YouTube Key)")]
    public string? TrailerYouTubeKey { get; set; }

    /// <summary>
    /// The movie's original title as listed on TMDb (may differ from the localized Movies.Title).
    /// Max 255 characters per schema.
    /// </summary>
    [MaxLength(255)]
    [Column("OriginalTitle")]
    [Display(Name = "Original Title")]
    public string? OriginalTitle { get; set; }

    /// <summary>
    /// ISO 639-1 language code of the movie's original language (e.g., "en", "fr", "ko").
    /// Max 20 characters per schema.
    /// </summary>
    [MaxLength(20)]
    [Column("OriginalLanguage")]
    [Display(Name = "Original Language")]
    public string? OriginalLanguage { get; set; }

    /// <summary>
    /// The movie's theatrical release date as reported by TMDb.
    /// Nullable — unreleased or unknown-date titles may have null.
    /// </summary>
    [Column("ReleaseDate")]
    [DataType(DataType.Date)]
    [Display(Name = "Release Date")]
    public DateOnly? ReleaseDate { get; set; }

    /// <summary>
    /// TMDb popularity score. Decimal(10,4). Higher = more popular.
    /// Used to sort "Now Trending" and similar catalog sections.
    /// Refreshed on each TmdbSyncService run.
    /// </summary>
    [Column("Popularity")]
    [Display(Name = "Popularity")]
    public decimal? Popularity { get; set; }

    /// <summary>
    /// TMDb user vote average on a 0.0–10.0 scale. Decimal(3,1).
    /// Displayed as a star rating or numeric badge on the movie card.
    /// </summary>
    [Column("VoteAverage")]
    [Display(Name = "Vote Average")]
    [Range(0.0, 10.0)]
    public decimal? VoteAverage { get; set; }

    /// <summary>
    /// Total number of TMDb user votes contributing to VoteAverage.
    /// Used to suppress ratings with very low vote counts (e.g., &lt; 10 votes).
    /// </summary>
    [Column("VoteCount")]
    [Display(Name = "Vote Count")]
    public int? VoteCount { get; set; }

    /// <summary>
    /// UTC timestamp of the last successful sync from the TMDb API.
    /// TmdbSyncService skips re-syncing records updated within the last 24 hours
    /// unless force-sync is explicitly requested.
    /// Null on initial insert before the first sync completes.
    /// </summary>
    [Column("LastSyncedUtc")]
    [Display(Name = "Last Synced (UTC)")]
    public DateTime? LastSyncedUtc { get; set; }

    /// <summary>UTC timestamp when this metadata record was first created.</summary>
    [Column("CreatedUtc")]
    [Display(Name = "Created (UTC)")]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp when this record was last updated. Null if never updated post-creation.</summary>
    [Column("UpdatedUtc")]
    [Display(Name = "Updated (UTC)")]
    public DateTime? UpdatedUtc { get; set; }

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>The movie this metadata belongs to.</summary>
    [ForeignKey(nameof(MovieId))]
    public Movie? Movie { get; set; }

    // ── Computed helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Proxied poster URL routed through TmdbImageController at w500 width.
    /// Returns null if PosterPath is not set.
    /// Route: /tmdb/poster/500{PosterPath} → TmdbImageController.Poster()
    /// → fetches from https://image.tmdb.org/t/p/w500{PosterPath} server-side.
    /// Using the proxy keeps image.tmdb.org out of the browser's CSP img-src,
    /// enables server-side caching, and prevents direct TMDb hotlinking.
    /// </summary>
    [NotMapped]
    public string? PosterUrl =>
        PosterPath is not null
            ? $"/tmdb/poster/500{PosterPath}"
            : null;

    /// <summary>
    /// Proxied backdrop URL routed through TmdbImageController at original width.
    /// Returns null if BackdropPath is not set.
    /// Route: /tmdb/backdrop/1280{BackdropPath} → TmdbImageController.Backdrop()
    /// </summary>
    [NotMapped]
    public string? BackdropUrl =>
        BackdropPath is not null
            ? $"/tmdb/backdrop/1280{BackdropPath}"
            : null;

    /// <summary>
    /// Full YouTube embed URL for the trailer.
    /// Returns null if TrailerYouTubeKey is not set.
    /// </summary>
    [NotMapped]
    public string? TrailerEmbedUrl =>
        TrailerYouTubeKey is not null
            ? $"https://www.youtube.com/embed/{TrailerYouTubeKey}"
            : null;

    /// <summary>
    /// True when the metadata is stale and eligible for re-sync.
    /// Stale threshold: not synced within the last 24 hours (or never synced).
    /// </summary>
    [NotMapped]
    public bool IsStale =>
        LastSyncedUtc is null || (DateTime.UtcNow - LastSyncedUtc.Value).TotalHours > 24;
}
