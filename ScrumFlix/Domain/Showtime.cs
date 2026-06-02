/*
 * File: /ScrumFlix/Domain/Showtime.cs
 * Description: Canonical Showtime entity — maps to the Showtime table in defaultdb.
 *              Previously named ScheduledShow in the legacy app, which targeted the wrong
 *              table (scheduled_shows) with completely different columns including the
 *              denormalized TicketsSold column (not in canonical schema).
 *
 *              IMPORTANT: seat availability must be computed as:
 *                  Capacity - COUNT(Ticket WHERE ShowtimeId = this.ShowtimeId)
 *              Do NOT add a TicketsSold property — it is not in the canonical schema.
 */

namespace ScrumFlix.Domain;

/// <summary>
/// A scheduled screening of a movie at a specific theater screen.
/// Maps to: Showtime (ShowtimeId, MovieId, TheaterScreenId, StartTime, Capacity, PricePerTicket, IsActive)
/// </summary>
[Table("Showtime")]
public class Showtime
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("ShowtimeId")]
    public int ShowtimeId { get; set; }

    /// <summary>The movie being screened.</summary>
    [Column("MovieId")]
    public int MovieId { get; set; }

    /// <summary>The theater screen where the movie plays.</summary>
    [Column("TheaterScreenId")]
    public int TheaterScreenId { get; set; }

    /// <summary>Date and time the screening begins.</summary>
    [Column("StartTime")]
    [Display(Name = "Start Time")]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Maximum number of tickets available for this showtime.
    /// Defaults to 50. Seat availability = Capacity - COUNT(Tickets).
    /// </summary>
    [Column("Capacity")]
    [Display(Name = "Capacity")]
    [Range(1, int.MaxValue)]
    public int Capacity { get; set; } = 50;

    /// <summary>Ticket price for this specific showtime.</summary>
    [Column("PricePerTicket")]
    [DataType(DataType.Currency)]
    [Display(Name = "Price Per Ticket")]
    public decimal PricePerTicket { get; set; } = 0.00m;

    /// <summary>Whether this showtime is available for booking.</summary>
    [Column("IsActive")]
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>The movie being shown.</summary>
    [ForeignKey(nameof(MovieId))]
    public Movie? Movie { get; set; }

    /// <summary>The screen this showtime is in.</summary>
    [ForeignKey(nameof(TheaterScreenId))]
    public TheaterScreen? TheaterScreen { get; set; }

    /// <summary>Tickets sold for this showtime.</summary>
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    /// <summary>
    /// Per-seat availability records for this showtime.
    /// One ShowtimeSeat row exists per physical seat in the screen.
    /// Used by SeatService and the seat-selection grid.
    /// </summary>
    public ICollection<ShowtimeSeat> ShowtimeSeats { get; set; } = new List<ShowtimeSeat>();

    /// <summary>Schedule assignments linking employees to this showtime.</summary>
    public ICollection<ScheduleAssignment> ScheduleAssignments { get; set; } = new List<ScheduleAssignment>();

    // ── Computed helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Remaining seats based on the loaded ShowtimeSeats collection.
    /// Counts entries with Status = 'Available' when ShowtimeSeats is loaded.
    /// Falls back to Capacity - Tickets.Count for contexts where ShowtimeSeats
    /// is not included (e.g., legacy queries).
    /// For accurate real-time counts, use SeatService.GetAvailableCountAsync()
    /// which queries ShowtimeSeat directly with the atomic status check.
    /// </summary>
    [NotMapped]
    public int AvailableSeats => ShowtimeSeats.Any()
        ? ShowtimeSeats.Count(ss => ss.Status == SeatStatus.Available)
        : Capacity - Tickets.Count;
}
