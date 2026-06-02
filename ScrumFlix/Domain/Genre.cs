/*
 * File: /ScrumFlix/Domain/Genre.cs
 * Description: Canonical Genre entity — maps to the Genres table in defaultdb.
 *              Net-new for Phase 2 (TMDB integration).
 *
 *              Genres are seeded from the TMDb genre list via TmdbSyncService.
 *              TMDbGenreId links back to the TMDb API genre identifier so syncs
 *              can be idempotent (upsert by TMDbGenreId).
 *
 *              Slug is a URL-safe lowercase version of Name used for routing
 *              (e.g., "action", "science-fiction"). Must be unique.
 *
 *              A Movie can belong to many Genres via the MovieGenres join table.
 *              The legacy Movies.Genre (varchar 30) column is preserved for
 *              backward-compat display; the normalized Genre relationship is the
 *              authoritative source for filtering.
 *
 *              DB constraints:
 *                UQ_Genres_Name         — Name must be unique
 *                UQ_Genres_Slug         — Slug must be unique
 *                UQ_Genres_TMDbGenreId  — TMDbGenreId must be unique (nullable)
 */

namespace ScrumFlix.Domain;

/// <summary>
/// A movie genre sourced from TMDb and used for catalog filtering.
/// Maps to: Genres (GenreId, TMDbGenreId, Name, Slug, IsActive, CreatedUtc)
/// </summary>
[Table("Genres")]
public class Genre
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("GenreId")]
    public int GenreId { get; set; }

    /// <summary>
    /// The TMDb genre identifier for this genre (e.g., 28 = Action, 35 = Comedy).
    /// Nullable — local genres created outside TMDb sync will have null here.
    /// Must be unique when non-null (UQ_Genres_TMDbGenreId).
    /// </summary>
    [Column("TMDbGenreId")]
    [Display(Name = "TMDb Genre ID")]
    public int? TMDbGenreId { get; set; }

    /// <summary>
    /// Display name of the genre (e.g., "Action", "Science Fiction").
    /// Must be unique across all genres (UQ_Genres_Name).
    /// Max 50 characters per schema.
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("Name")]
    [Display(Name = "Genre Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-safe lowercase slug used in routing and filtering
    /// (e.g., "action", "science-fiction").
    /// Must be unique across all genres (UQ_Genres_Slug).
    /// Generated from Name on creation; max 50 characters.
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("Slug")]
    [Display(Name = "Slug")]
    public string Slug { get; set; } = string.Empty;

    /// <summary>Whether this genre is active and appears in catalog filters.</summary>
    [Column("IsActive")]
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    /// <summary>UTC timestamp when this genre record was created.</summary>
    [Column("CreatedUtc")]
    [Display(Name = "Created (UTC)")]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>Join records linking movies to this genre.</summary>
    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
}
