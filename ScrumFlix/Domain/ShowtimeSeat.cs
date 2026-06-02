/*
 * File:        /ScrumFlix/Domain/ShowtimeSeat.cs
 * Namespace:   ScrumFlix.Domain
 * Purpose:     Canonical ShowtimeSeat entity — maps to the ShowtimeSeat table in defaultdb.
 *
 *              Tracks the availability status of a specific physical seat for a
 *              specific showtime. One ShowtimeSeat row exists per Seat × Showtime
 *              combination for each screen.
 *
 *              STATUS VALUES:
 *                'Available'  — seat is open for selection
 *                'Reserved'   — seat is held by a SeatReservation (temporary, has ExpiresAt)
 *                'Sold'       — seat has a completed Ticket; permanent
 *
 *              CONCURRENCY:
 *                The canonical concurrency strategy is an atomic conditional UPDATE:
 *                  UPDATE ShowtimeSeat SET Status='Reserved'
 *                  WHERE ShowtimeSeatId=@id AND Status='Available'
 *                This is enforced in SeatService.ReserveSeatAsync() and prevents
 *                double-booking without application-level locking.
 *
 *              The UNIQUE(ShowtimeId, SeatId) constraint in the live schema is mapped
 *              via HasIndex in AppDbContext.OnModelCreating().
 *
 *              Seeded by SampleDataSeederFull Block 4-SS (cross-join Seat × Showtime
 *              by TheaterScreenId, bulk insert, Status = 'Available').
 *
 * Phase:   2 (Patch)
 * Author:  ScrumFlix Rebuild Team
 * Updated: 2026-05-05
 */

namespace ScrumFlix.Domain;

/// <summary>
/// Tracks the availability of a physical seat for a specific showtime.
/// Maps to: ShowtimeSeat (ShowtimeSeatId, ShowtimeId, SeatId, Status)
/// </summary>
[Table("ShowtimeSeat")]
public class ShowtimeSeat
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("ShowtimeSeatId")]
    public int ShowtimeSeatId { get; set; }

    /// <summary>The showtime this availability record belongs to.</summary>
    [Column("ShowtimeId")]
    public int ShowtimeId { get; set; }

    /// <summary>The physical seat this record tracks.</summary>
    [Column("SeatId")]
    public int SeatId { get; set; }

    /// <summary>
    /// Current availability status of this seat for this showtime.
    /// Valid values: 'Available', 'Reserved', 'Sold'.
    /// Use the <see cref="SeatStatus"/> constants to avoid magic strings.
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("Status")]
    [Display(Name = "Status")]
    public string Status { get; set; } = SeatStatus.Available;

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>The showtime this record belongs to.</summary>
    [ForeignKey(nameof(ShowtimeId))]
    public Showtime? Showtime { get; set; }

    /// <summary>The physical seat being tracked.</summary>
    [ForeignKey(nameof(SeatId))]
    public Seat? Seat { get; set; }

    /// <summary>Active reservation record for this seat (if Status = 'Reserved').</summary>
    public SeatReservation? Reservation { get; set; }

    /// <summary>Ticket issued for this seat (if Status = 'Sold').</summary>
    public Ticket? Ticket { get; set; }

    // ── Computed helpers ───────────────────────────────────────────────────

    /// <summary>True when the seat is open for selection.</summary>
    [NotMapped]
    public bool IsAvailable => Status == SeatStatus.Available;

    /// <summary>True when the seat is held by an active reservation.</summary>
    [NotMapped]
    public bool IsReserved => Status == SeatStatus.Reserved;

    /// <summary>True when the seat has a completed ticket purchase.</summary>
    [NotMapped]
    public bool IsSold => Status == SeatStatus.Sold;
}

/// <summary>
/// String constants for the ShowtimeSeat.Status column.
/// Use these instead of inline string literals to prevent typo-driven bugs.
/// Valid values match the canonical DB CHECK constraint exactly.
/// </summary>
public static class SeatStatus
{
    /// <summary>Seat is open for selection. Default state after seeding.</summary>
    public const string Available = "Available";

    /// <summary>Seat is held by a timed reservation. Not yet purchased.</summary>
    public const string Reserved = "Reserved";

    /// <summary>Seat has a completed Ticket. Cannot be released back to Available.</summary>
    public const string Sold = "Sold";

    /// <summary>
    /// Seat is administratively blocked for this showtime (broken seat, VIP hold, etc.).
    /// Can be released back to Available by an admin — does not represent a sale.
    /// </summary>
    public const string Blocked = "Blocked";
}
