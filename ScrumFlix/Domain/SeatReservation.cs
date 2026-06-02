/*
 * File:        /ScrumFlix/Domain/SeatReservation.cs
 * Namespace:   ScrumFlix.Domain
 * Purpose:     Canonical SeatReservation entity — maps to the SeatReservation table in defaultdb.
 *
 *              A SeatReservation is a temporary hold on a ShowtimeSeat created when a
 *              customer begins checkout. The hold expires at ExpiresAt, after which the
 *              SeatReservationExpiryService background worker resets the seat's status
 *              back to 'Available' and deletes the reservation row.
 *
 *              LIFECYCLE:
 *                1. Customer selects a seat on the ShowtimeBooking view.
 *                2. SeatService.ReserveSeatAsync() atomically flips Status → 'Reserved'
 *                   and inserts a SeatReservation row with ExpiresAt = UtcNow + 10 min.
 *                3. Customer completes checkout: CartController writes a Ticket row,
 *                   flips Status → 'Sold', and deletes the SeatReservation row.
 *                4. OR customer abandons checkout: the IHostedService background worker
 *                   polls every 60 seconds, finds expired rows, and resets seats.
 *
 *              NOTE ON UserId:
 *                The live SeatReservation table has a UserId column (NOT NULL) with
 *                FK_SeatReservation_User FOREIGN KEY (UserId) REFERENCES Users(UserId).
 *                Mapped here with a User navigation property and configured in AppDbContext.
 *
 * Phase:   2 (Patch)
 * Author:  ScrumFlix Rebuild Team
 * Updated: 2026-05-05
 */

namespace ScrumFlix.Domain;

/// <summary>
/// A temporary hold on a showtime seat, created at the start of the checkout flow.
/// Maps to: SeatReservation (ReservationId, ShowtimeSeatId, UserId, ReservedAt, ExpiresAt)
/// </summary>
[Table("SeatReservation")]
public class SeatReservation
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("ReservationId")]
    public int ReservationId { get; set; }

    /// <summary>The specific showtime-seat combination being held.</summary>
    [Column("ShowtimeSeatId")]
    public int ShowtimeSeatId { get; set; }

    /// <summary>
    /// The Users.UserId of the customer or system account that created the reservation.
    /// For web purchases, this is the web.sales UserId (from ISystemAccountProvider).
    /// </summary>
    [Column("UserId")]
    public int UserId { get; set; }

    /// <summary>
    /// Current lifecycle state of this reservation.
    /// Valid values: 'Active', 'Expired', 'Cancelled', 'Converted'.
    /// Use <see cref="ReservationStatus"/> constants — never inline string literals.
    /// Default is 'Active' per canonical schema.
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Column("ReservationStatus")]
    [Display(Name = "Status")]
    public string ReservationStatus { get; set; } = SeatReservationStatuses.Active;

    /// <summary>UTC timestamp when the reservation was created.</summary>
    [Column("ReservedAt")]
    [Display(Name = "Reserved At (UTC)")]
    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp when the reservation expires.
    /// After this time the seat is released back to 'Available' by the
    /// SeatReservationExpiryService background worker.
    /// Standard hold duration: 10 minutes from ReservedAt.
    /// </summary>
    [Column("ExpiresAt")]
    [Display(Name = "Expires At (UTC)")]
    public DateTime ExpiresAt { get; set; }

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>The showtime-seat record being held.</summary>
    [ForeignKey(nameof(ShowtimeSeatId))]
    public ShowtimeSeat? ShowtimeSeat { get; set; }

    /// <summary>The user who created this reservation.</summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    // ── Computed helpers ───────────────────────────────────────────────────

    /// <summary>True when the reservation has passed its expiry time.</summary>
    [NotMapped]
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>Remaining hold time. Negative when expired.</summary>
    [NotMapped]
    public TimeSpan TimeRemaining => ExpiresAt - DateTime.UtcNow;
}

/// <summary>
/// String constants for the SeatReservation.ReservationStatus column.
/// Matches the canonical DB CHECK constraint exactly.
/// </summary>
public static class SeatReservationStatuses
{
    /// <summary>
    /// Reservation is live. Seat is held. ExpiresAt is in the future.
    /// Default state on creation.
    /// </summary>
    public const string Active = "Active";

    /// <summary>
    /// Reservation expired without checkout. SeatReservationExpiryService set this
    /// and reset the corresponding ShowtimeSeat.Status back to 'Available'.
    /// </summary>
    public const string Expired = "Expired";

    /// <summary>
    /// Customer or staff explicitly cancelled the reservation before expiry.
    /// Seat was released back to 'Available'.
    /// </summary>
    public const string Cancelled = "Cancelled";

    /// <summary>
    /// Customer completed checkout. A Ticket row was written and
    /// ShowtimeSeat.Status was set to 'Sold'. Reservation row is retained for audit.
    /// </summary>
    public const string Converted = "Converted";
}
