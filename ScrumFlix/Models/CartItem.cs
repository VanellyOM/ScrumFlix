/*
 * File: /ScrumFlix/Models/CartItem.cs
 * Description: Represents an item in the session-based shopping cart (ticket or concession).
 *
 * Phase 3 — Backend Alignment:
 *   REMOVED (phantom schema fields):
 *     - ShowId (int?) → replaced with ShowtimeId (int?) to match canonical Showtime.ShowtimeId
 *     - PriceTierId (int?) → not in canonical schema; pricing is Showtime.PricePerTicket
 *     - TierName (string?) → no price tiers in canonical schema
 *     - GuestEmail (string?) → Ticket.UserAtSale FK replaces guest checkout
 *     - InventoryItemId (string? base64) → replaced with ConcessionItemId (int?)
 *
 *   ADDED:
 *     - ShowtimeId (int?) — canonical FK to Showtime.ShowtimeId
 *     - UserAtSale (int) — UserId of logged-in user; written from session by ShowtimesController
 *     - ConcessionItemId (int?) — canonical FK to ConcessionItem.ConcessionItemId
 *     - LocationId (int?) — unified location for both ticket and concession conflict detection
 *
 * Phase 2 Audit — F-02 Fix:
 *   CHANGED:
 *     - ShowtimeSeatId (int?) → ShowtimeSeatIds (List<int>) — seat picker can select multiple
 *       seats in a single transaction (up to Quantity). SeatService.ReserveSeatAsync is called
 *       once per seat; each resolved ShowtimeSeatId is appended to this list.
 *       CartController.Checkout passes the full list to SeatService.FinalizeSeatsAsync inside
 *       the ticket transaction, flipping all selected ShowtimeSeat rows from Reserved → Sold.
 *       Empty list (not null) for general-admission flows — simplifies null checks at checkout.
 *
 * Seat picker addition:
 *   ADDED:
 *     - SeatNumbers (string?) — raw comma-separated seat labels posted from the seat picker UI
 *       (e.g. "A3,A4"). Written by ShowtimesController.AddTicketToCart from vm.SeatNumbers.
 *       Used as input to SeatService to resolve ShowtimeSeat rows by label before reservation.
 *       Null/empty for general-admission flows where no specific seats were chosen.
 *       Also stored in TempData["PendingTicket_SeatNumbers"] for location-conflict replay.
 */

namespace ScrumFlix.Models;

/// <summary>Defines the type of item in the cart.</summary>
public enum CartItemType
{
    Ticket,
    Concession
}

/// <summary>
/// A single item held in the user's in-session shopping cart,
/// representing either a ticket or a concession product.
/// </summary>
public class CartItem
{
    /// <summary>Gets or sets a unique identifier for this cart line item.</summary>
    public string CartItemId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the type of this cart item (Ticket or Concession).</summary>
    public CartItemType ItemType { get; set; }

    // ── Ticket fields ───────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the canonical Showtime.ShowtimeId when this is a ticket item.
    /// Replaces legacy ShowId which targeted the phantom scheduled_shows table.
    /// </summary>
    public int? ShowtimeId { get; set; }

    /// <summary>
    /// Gets or sets the raw comma-separated seat labels chosen in the seat picker UI
    /// (e.g. "A3,A4"). Posted from ShowtimeBooking via the hidden SeatNumbers input
    /// and forwarded here by ShowtimesController.AddTicketToCart.
    ///
    /// Used by ShowtimesController as input to SeatService to resolve the seat labels
    /// into ShowtimeSeat rows before calling ReserveSeatAsync on each one. The resolved
    /// IDs are stored in ShowtimeSeatIds for use at checkout.
    ///
    /// Null or empty string for general-admission flows where no seats were picked.
    /// </summary>
    public string? SeatNumbers { get; set; }

    /// <summary>
    /// Gets or sets the resolved ShowtimeSeat.ShowtimeSeatId values for all seats
    /// selected in the seat picker for this cart item.
    ///
    /// LIFECYCLE:
    ///   1. ShowtimesController.AddTicketToCart splits SeatNumbers into individual labels.
    ///   2. SeatService.GetSeatsForShowtimeAsync resolves each label to a ShowtimeSeat row.
    ///   3. SeatService.ReserveSeatAsync is called for each; on Success the ShowtimeSeatId
    ///      is appended to this list.
    ///   4. CartController.Checkout passes the full list to SeatService.FinalizeSeatsAsync
    ///      inside the ticket transaction, flipping all rows from Reserved → Sold and
    ///      removing the corresponding SeatReservation rows.
    ///
    /// Empty list (never null) for general-admission flows — simplifies null checks at
    /// checkout without requiring a null guard before iterating.
    /// </summary>
    public List<int> ShowtimeSeatIds { get; set; } = new();

    /// <summary>
    /// Gets or sets the UserId of the authenticated employee who initiated the sale.
    /// Written from session by ShowtimesController. Maps to Ticket.UserAtSale.
    /// </summary>
    public int UserAtSale { get; set; }

    /// <summary>Gets or sets the movie title for display on ticket items.</summary>
    public string? MovieName { get; set; }

    /// <summary>Gets or sets the showtime start for display on ticket items.</summary>
    public DateTime? ShowTime { get; set; }

    /// <summary>
    /// Gets or sets the theater screen name for display on ticket items
    /// (e.g. "Screen 1", "IMAX"). Populated by ShowtimesController from
    /// Showtime.TheaterScreen.ScreenName. Used on the order confirmation
    /// page and receipt email to show the screen before the seat label.
    /// </summary>
    public string? ScreenName { get; set; }

    // ── Concession fields ───────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the canonical ConcessionItem.ConcessionItemId when this is a concession item.
    /// Replaces legacy InventoryItemId (base64-encoded binary UUID).
    /// </summary>
    public int? ConcessionItemId { get; set; }

    // ── Shared fields ────────────────────────────────────────────────────────

    /// <summary>Gets or sets the display name of this cart item.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the location name for display purposes.</summary>
    public string? LocationName { get; set; }

    /// <summary>
    /// Gets or sets the LocationId for this item.
    /// For tickets: TheaterScreen.LocationId. For concessions: ConcessionItem.LocationId.
    /// Used by CartService.GetConcessionLocationId() for cross-location conflict detection.
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>Gets or sets the unit price for this item.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Gets or sets the quantity of this item in the cart.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Gets the total price for this line item.</summary>
    public decimal LineTotal => UnitPrice * Quantity;
}
