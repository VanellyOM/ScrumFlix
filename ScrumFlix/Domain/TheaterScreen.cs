/*
 * File: /ScrumFlix/Domain/TheaterScreen.cs
 * Description: Canonical TheaterScreen entity — maps to the TheaterScreen table in defaultdb.
 *              Previously named TheaterRoom in the legacy app; that model targeted the wrong
 *              table (theater_rooms) with wrong column names.
 */

namespace ScrumFlix.Domain;

/// <summary>
/// A physical screening room inside a ScrumFlix theater location.
/// Maps to: TheaterScreen (TheaterScreenId, LocationId, ScreenName, Capacity, IsActive)
/// </summary>
[Table("TheaterScreen")]
public class TheaterScreen
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("TheaterScreenId")]
    public int TheaterScreenId { get; set; }

    /// <summary>The location this screen belongs to.</summary>
    [Column("LocationId")]
    public int LocationId { get; set; }

    /// <summary>Display name of the screen (e.g., "N Screen 1 Small").</summary>
    [Required]
    [MaxLength(100)]
    [Column("ScreenName")]
    [Display(Name = "Screen Name")]
    public string ScreenName { get; set; } = string.Empty;

    /// <summary>Maximum number of seats available in this screen. Defaults to 50.</summary>
    [Column("Capacity")]
    [Display(Name = "Capacity")]
    [Range(1, int.MaxValue)]
    public int Capacity { get; set; } = 50;

    /// <summary>Whether this screen is currently available for scheduling.</summary>
    [Column("IsActive")]
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>The theater location this screen belongs to.</summary>
    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }

    /// <summary>Showtimes scheduled in this screen.</summary>
    public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();

    /// <summary>
    /// Physical seats in this screen.
    /// Populated by SampleDataSeederFull Block 4-S from TheaterScreen.Capacity.
    /// Used by the seat-selection grid and SeatService.
    /// </summary>
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
