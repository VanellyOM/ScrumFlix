/*
 * File: /ScrumFlix/Domain/Ticket.cs
 * Description: Canonical Ticket entity — maps to the Ticket table in defaultdb.
 *
 *              KEY DIFFERENCES from legacy model:
 *              - TicketCode (bigint/long) is a canonical column — must be generated on purchase.
 *              - UserAtSale is an FK to Users.UserId — authentication is required to buy tickets.
 *              - There is NO GuestEmail, NO PriceTierId, NO CustomerId in the canonical schema.
 *              - Price at purchase is derived from Showtime.PricePerTicket at time of sale.
 */

namespace ScrumFlix.Domain;

/// <summary>
/// A purchased ticket for a specific showtime, linked to the employee/user who processed the sale.
/// Maps to: Ticket (TicketId, TicketCode, ShowtimeId, UserAtSale, TimeOfSale)
/// </summary>
[Table("Ticket")]
public class Ticket
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("TicketId")]
    public int TicketId { get; set; }

    /// <summary>
    /// A unique code printed on the ticket for validation at the door.
    /// Generated at purchase time. Must be unique across all tickets.
    /// Maps to BIGINT in the canonical schema — stored as long to prevent overflow.
    /// </summary>
    [Column("TicketCode")]
    [Display(Name = "Ticket Code")]
    public long TicketCode { get; set; }

    /// <summary>The showtime this ticket grants entry to.</summary>
    [Column("ShowtimeId")]
    public int ShowtimeId { get; set; }

    /// <summary>
    /// The Users.UserId of the staff member or account that processed this ticket sale.
    /// Authentication is required to purchase — there is no guest checkout in the canonical schema.
    /// </summary>
    [Column("UserAtSale")]
    [Display(Name = "Sold By (UserId)")]
    public int UserAtSale { get; set; }

    /// <summary>The date and time this ticket was issued.</summary>
    [Column("TimeOfSale")]
    [Display(Name = "Time of Sale")]
    public DateTime TimeOfSale { get; set; }

    /// <summary>
    /// The ShowtimeSeat this ticket is assigned to.
    /// Nullable — legacy tickets (ShowtimeSeatId = NULL) pre-date assigned seating.
    /// Set to a valid ShowtimeSeatId when the customer selects a seat at checkout.
    /// When populated, the corresponding ShowtimeSeat.Status must be 'Sold'.
    /// </summary>
    [Column("ShowtimeSeatId")]
    [Display(Name = "Seat")]
    public int? ShowtimeSeatId { get; set; }

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>The showtime this ticket is for.</summary>
    [ForeignKey(nameof(ShowtimeId))]
    public Showtime? Showtime { get; set; }

    /// <summary>The user (staff account) who processed the sale.</summary>
    [ForeignKey(nameof(UserAtSale))]
    public User? SoldByUser { get; set; }

    /// <summary>
    /// The assigned seat for this ticket.
    /// Null for legacy tickets that pre-date the assigned-seating feature.
    /// </summary>
    [ForeignKey(nameof(ShowtimeSeatId))]
    public ShowtimeSeat? ShowtimeSeat { get; set; }
}
