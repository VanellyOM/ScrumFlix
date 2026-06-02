/*
 * File: /ScrumFlix/Domain/Movie.cs
 * Description: Canonical Movie entity — maps to the Movies table in defaultdb.
 *              Column names match the schema dump exactly.
 */

namespace ScrumFlix.Domain;

/// <summary>
/// A movie available for scheduling and ticket purchase at ScrumFlix theaters.
/// Maps to: Movies (MovieId, Title, Rating, Genre, RuntimeMinutes, Description)
/// </summary>
[Table("Movies")]
public class Movie
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("MovieId")]
    public int MovieId { get; set; }

    /// <summary>Movie title. Must be unique across the catalog.</summary>
    [Required]
    [MaxLength(200)]
    [Column("Title")]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>MPA rating (e.g., G, PG, PG-13, R, NC-17).</summary>
    [Required]
    [MaxLength(20)]
    [Column("Rating")]
    [Display(Name = "Rating")]
    public string Rating { get; set; } = string.Empty;

    /// <summary>Genre classification (e.g., Action, Comedy, Horror).</summary>
    [Required]
    [MaxLength(30)]
    [Column("Genre")]
    public string Genre { get; set; } = string.Empty;

    /// <summary>Total runtime in minutes.</summary>
    [Column("RuntimeMinutes")]
    [Display(Name = "Runtime (min)")]
    [Range(1, 9999)]
    public short RuntimeMinutes { get; set; }

    /// <summary>Plot synopsis shown on the detail page.</summary>
    [Required]
    [MaxLength(1000)]
    [Column("Description")]
    public string Description { get; set; } = string.Empty;

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>Showtimes scheduled for this movie.</summary>
    public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();

    /// <summary>
    /// TMDb enrichment metadata for this movie (one-to-one).
    /// Null if TmdbSyncService has not yet synced this movie.
    /// Include with: .Include(m => m.TmdbMetadata)
    /// </summary>
    public MovieTmdbMetadata? TmdbMetadata { get; set; }

    /// <summary>
    /// Genre associations for this movie via the MovieGenres join table.
    /// One entry will have IsPrimaryGenre = true; others are secondary tags.
    /// Include with: .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
    /// </summary>
    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();

    // ── Computed helpers ───────────────────────────────────────────────────

    /// <summary>Formats RuntimeMinutes as a human-readable string (e.g., "2h 6m").</summary>
    [NotMapped]
    public string FormattedRunTime =>
        RuntimeMinutes >= 60
            ? $"{RuntimeMinutes / 60}h {RuntimeMinutes % 60}m"
            : $"{RuntimeMinutes}m";

    /// <summary>
    /// The primary genre name for display (e.g., on movie cards).
    /// Returns the legacy Movies.Genre field if MovieGenres is not loaded.
    /// </summary>
    [NotMapped]
    public string PrimaryGenreDisplay =>
        MovieGenres.FirstOrDefault(mg => mg.IsPrimaryGenre)?.Genre?.Name ?? Genre;
}
