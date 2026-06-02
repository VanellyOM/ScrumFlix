/*
 * File:      /ScrumFlix/ViewModels/OrderConfirmationViewModel.cs
 * Namespace: ScrumFlix.ViewModels
 * Purpose:   Strongly-typed ViewModel for the order confirmation page.
 *            Replaces ViewBag.OrderTotal, ViewBag.IssuedCodes, and ViewBag.QrCodes.
 *
 *            Data shape:
 *              OrderTotal  — formatted currency string (e.g. "$14.50") that was
 *                            stored in TempData after checkout completed.
 *              IssuedCodes — list of long TicketCodes generated during checkout,
 *                            one per ticket. May be empty for concession-only orders.
 *              QrCodes     — Base64-encoded PNG QR images, one per IssuedCode entry,
 *                            produced by QrCodeService.GenerateBase64PngBatch().
 *                            Parallel-indexed with IssuedCodes (index i of QrCodes
 *                            corresponds to index i of IssuedCodes).
 *
 * Sprint: S1 — ViewBag Purge
 */

namespace ScrumFlix.ViewModels;

/// <summary>
/// ViewModel for the order confirmation page shown after a successful checkout.
/// Carries the order total, issued ticket codes, and their QR code PNG images.
/// </summary>
public class OrderConfirmationViewModel
{
    /// <summary>Pre-tax subtotal (e.g. <c>"$13.34"</c>). Null for orders
    /// placed before this field was added.</summary>
    public string? OrderSubtotal { get; set; }

    /// <summary>Sales tax amount (e.g. <c>"$1.10"</c>). Null for orders
    /// placed before this field was added.</summary>
    public string? OrderTax { get; set; }

    /// <summary>
    /// The formatted order grand total (e.g. <c>"$14.50"</c>).
    /// Null if the total was not stored in TempData (should not occur in normal flow).
    /// </summary>
    public string? OrderTotal { get; set; }

    /// <summary>
    /// Long ticket codes issued during checkout, one per ticket purchased.
    /// Empty for concession-only orders.
    /// </summary>
    public List<long> IssuedCodes { get; set; } = new();

    /// <summary>
    /// Base64-encoded PNG QR code images produced by
    /// <c>QrCodeService.GenerateBase64PngBatch()</c>.
    /// Parallel-indexed with <see cref="IssuedCodes"/>: index <c>i</c> in this list
    /// corresponds to index <c>i</c> in <see cref="IssuedCodes"/>.
    /// </summary>
    public List<string> QrCodes { get; set; } = new();

    /// <summary>
    /// Seat labels parallel-indexed with IssuedCodes (e.g. "B7", "C3").
    /// Empty string for general-admission tickets with no assigned seat.
    /// Populated by CartController.Checkout from the Ticket.ShowtimeSeatId → Seat lookup.
    /// </summary>
    public List<string> SeatLabels { get; set; } = new();

    /// <summary>
    /// Full cart item snapshot serialized before cart is cleared at checkout.
    /// Used to render the itemized order breakdown on the confirmation page.
    /// Deserialized from TempData["OrderItems"] JSON in CartController.OrderConfirmation.
    /// </summary>
    public List<OrderLineItem> OrderItems { get; set; } = new();

    /// <summary>Returns true when at least one ticket was issued.</summary>
    public bool HasTickets => IssuedCodes.Any();

    /// <summary>Returns the total number of issued tickets.</summary>
    public int TicketCount => IssuedCodes.Count;

    /// <summary>Returns true when the order contains concession items.</summary>
    public bool HasConcessions => OrderItems.Any(i => i.IsConcession);

    /// <summary>
    /// Base64-encoded PNG QR code for the concession order receipt.
    /// Null when the order contains no concession items.
    /// Displayed on the confirmation page as proof of pre-purchase for the
    /// customer to show at the concession stand.
    /// </summary>
    public string? ConcessionQrCode { get; set; }

    /// <summary>
    /// Returns the seat label for ticket at index i, or empty string if none assigned.
    /// </summary>
    public string GetSeatLabel(int i) =>
        i < SeatLabels.Count ? SeatLabels[i] : string.Empty;
}

/// <summary>
/// A single line item in the order confirmation breakdown.
/// Captured from CartItem before the cart is cleared at checkout.
/// </summary>
public class OrderLineItem
{
    public string DisplayName { get; set; } = string.Empty;
    public int    Quantity    { get; set; }
    public decimal UnitPrice  { get; set; }
    public decimal LineTotal  { get; set; }
    public bool   IsConcession { get; set; }
    public string? MovieName  { get; set; }
    public DateTime? ShowTime { get; set; }
    public string? LocationName { get; set; }
    public string? SeatNumbers { get; set; }

    public string FormattedShowTime =>
        ShowTime?.ToString("ddd MMM d · h:mm tt") ?? string.Empty;
}
