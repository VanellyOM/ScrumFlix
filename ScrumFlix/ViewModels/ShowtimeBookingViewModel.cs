/*
 * File: /ScrumFlix/ViewModels/ShowtimeBookingViewModel.cs
 * Description: ViewModel for the showtime booking / seat-selection page.
 *
 * Phase 3 — Backend Alignment (#29 / P3-3):
 *   REMOVED (phantom schema / not in canonical):
 *     - ScheduledShow Show property → replaced with Showtime? Showtime
 *     - List<PriceTier> PriceTiers → pricing is Showtime.PricePerTicket (single price)
 *     - int SelectedPriceTierId → no price tiers in canonical schema
 *     - string GuestEmail + ConfirmEmail → ticket requires authenticated user (UserAtSale FK)
 *
 *   ADDED:
 *     - Showtime? Showtime: canonical Showtime entity with Movie + TheaterScreen nav props loaded
 *     - int ShowtimeId: posted back on form submit so controller can re-load showtime
 *     - int Quantity: retained (1–20 tickets per transaction)
 *     - string? SeatNumbers: optional comma-separated seat labels from the seat picker UI
 *
 * Nullable fix:
 *   Showtime is declared as Showtime? (nullable) rather than Showtime = null!.
 *   The = null! null-forgiveness operator suppresses the compiler warning but does
 *   NOT prevent the ASP.NET Core model binder from treating the non-nullable reference
 *   type as implicitly required. On POST the binder finds no value for Showtime,
 *   adds "The Showtime field is required." to ModelState, and the form always bounces
 *   back — even with [BindNever] present. Making the property nullable removes the
 *   implicit required constraint so [BindNever] can do its job correctly.
 */

namespace ScrumFlix.ViewModels;

/// <summary>
/// ViewModel for the showtime booking page, presenting show details, seating info,
/// and a quantity selector. Authentication is required before this page is reached;
/// the logged-in user's UserId is used as UserAtSale on ticket creation.
/// </summary>
public class ShowtimeBookingViewModel
{
    /// <summary>
    /// The canonical Showtime being booked.
    /// Must be loaded with .Include(st => st.Movie)
    ///                      .Include(st => st.TheaterScreen).ThenInclude(ts => ts.Location)
    ///                      .Include(st => st.Tickets)
    ///                      .Include(st => st.ShowtimeSeats).ThenInclude(ss => ss.Seat)
    ///
    /// Declared nullable (Showtime?) so the model binder does not treat it as an
    /// implicitly required field on POST. [BindNever] prevents binding attempts;
    /// the nullable declaration prevents the "field is required" ModelState error
    /// that fires before [BindNever] can suppress it.
    /// </summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public Showtime? Showtime { get; set; }

    /// <summary>
    /// Showtime ID posted back on form submit so the controller can re-load data.
    /// Bound from a hidden input: <input type="hidden" asp-for="ShowtimeId" />.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "A valid showtime must be selected.")]
    public int ShowtimeId { get; set; }

    /// <summary>
    /// Number of tickets to purchase (1–20).
    /// Validated server-side against Showtime.AvailableSeats before committing.
    /// </summary>
    [Range(1, 20, ErrorMessage = "You can purchase between 1 and 20 tickets.")]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Optional comma-separated seat labels selected in the seat-picker UI (e.g. "A3,A4").
    /// When present, CartController will attempt to reserve ShowtimeSeats by label.
    /// When null/empty, no seat-specific reservation is made.
    /// </summary>
    public string? SeatNumbers { get; set; }

    // ── Computed helpers (view-side) ─────────────────────────────────────

    /// <summary>Formatted start time string for display.</summary>
    public string FormattedStartTime =>
        Showtime?.StartTime.ToString("dddd, MMMM d 'at' h:mm tt") ?? string.Empty;

    /// <summary>Price per ticket from the canonical Showtime.PricePerTicket field.</summary>
    public decimal PricePerTicket => Showtime?.PricePerTicket ?? 0m;

    /// <summary>Line total for display: PricePerTicket × Quantity.</summary>
    public decimal LineTotal => PricePerTicket * Quantity;
}