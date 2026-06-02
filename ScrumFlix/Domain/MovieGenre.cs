/*
 * File: /ScrumFlix/Domain/MovieGenre.cs
 * Description: Canonical MovieGenre join entity — maps to the MovieGenres table in defaultdb.
 *              Net-new for Phase 2 (TMDB integration).
 *
 *              This is the many-to-many join between Movies and Genres.
 *              A movie has one primary genre (IsPrimaryGenre = true) and may have
 *              additional secondary genre tags.
 *
 *              The UNIQUE constraint UQ_MovieGenres_MovieId_GenreId prevents duplicate
 *              genre assignments and is enforced via HasIndex in AppDbContext.
 *
 *              ON DELETE CASCADE is defined for both FKs in the schema — deleting a
 *              Movie or Genre will remove the corresponding MovieGenre rows automatically.
 *
 *              TmdbSyncService populates this table when syncing movie metadata.
 */

namespace ScrumFlix.Domain;

/// <summary>
/// A many-to-many join between a Movie and a Genre.
/// Maps to: MovieGenres (MovieGenreId, MovieId, GenreId, IsPrimaryGenre, CreatedUtc)
/// </summary>
[Table("MovieGenres")]
public class MovieGenre
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("MovieGenreId")]
    public int MovieGenreId { get; set; }

    /// <summary>The movie being tagged with this genre.</summary>
    [Column("MovieId")]
    public int MovieId { get; set; }

    /// <summary>The genre being applied to this movie.</summary>
    [Column("GenreId")]
    public int GenreId { get; set; }

    /// <summary>
    /// When true, this is the movie's primary genre used for display and
    /// primary filtering. Only one MovieGenre per movie should have this set.
    /// Additional genres are secondary (IsPrimaryGenre = false).
    /// Default is false per canonical schema.
    /// </summary>
    [Column("IsPrimaryGenre")]
    [Display(Name = "Primary Genre")]
    public bool IsPrimaryGenre { get; set; } = false;

    /// <summary>UTC timestamp when this genre association was created.</summary>
    [Column("CreatedUtc")]
    [Display(Name = "Created (UTC)")]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>The movie in this genre association.</summary>
    [ForeignKey(nameof(MovieId))]
    public Movie? Movie { get; set; }

    /// <summary>The genre in this movie association.</summary>
    [ForeignKey(nameof(GenreId))]
    public Genre? Genre { get; set; }
}
