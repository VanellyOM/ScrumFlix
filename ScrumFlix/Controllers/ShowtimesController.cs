/*
 * File: /ScrumFlix/Controllers/ShowtimesController.cs
 * Description: Controller for viewing showtime details, seat selection, and adding tickets to cart.
 *
 * Phase 3 — Backend Alignment (#28 / P3-2):
 *   REMOVED:
 *     - _db.ScheduledShows, _db.PriceTiers → phantom tables not in canonical schema
 *     - GuestEmail / ConfirmEmail validation → replaced by auth requirement
 *     - PriceTier lookup → pricing is Showtime.PricePerTicket (single price per showtime)
 *     - Location / TheaterRoom includes → replaced with TheaterScreen + ThenInclude(Location)
 *
 *   ADDED:
 *     - Auth guard: requires active session (UserId in session) before booking
 *     - PricePerTicket sourced directly from Showtime entity
 *     - CartItem built with ShowtimeId (int), UnitPrice from Showtime.PricePerTicket
 *     - Location conflict check now keyed on TheaterScreen.LocationId
 *
 * S4 Patch — WebSale purchase bug fix:
 *   FIXED: GetSessionUserId() was using Session.GetString("UserId") but
 *          ConsumerControllerBase.OnActionExecuting() writes the WebUser id via
 *          Session.SetInt32(AuthService.SessionUserId, ...). ASP.NET Core stores
 *          ints and strings in separate session slots, so GetString() always
 *          returned null for a WebUser session, blocking every guest purchase.
 *          Fix: delegate to the base class SessionUserId property (uses GetInt32)
 *          so both staff and WebUser sessions resolve through the same code path.
 *
 * ViewBag removal:
 *   CartService is now also injected into ConsumerControllerBase and passed via
 *   base(systemAccounts, cart). The local _cart field is retained because
 *   ShowtimesController uses CartService directly (AddItem, GetConcessionLocationId).
 *
 * Seat selection additions:
 *   - ShowtimeSeats included in all Showtime queries so AvailableSeats uses the
 *     accurate per-seat status check rather than the Capacity - Tickets.Count fallback.
 *   - vm.SeatNumbers forwarded to CartItem.SeatNumbers so the cart and downstream
 *     reservation logic know which specific seats were chosen.
 *   - ILogger<ShowtimesController> injected for POST diagnostics: logs all bound
 *     form values on entry and all ModelState errors on validation failure.
 */

namespace ScrumFlix.Controllers;

/// <summary>
/// Handles showtime detail viewing, seat layout display, and adding tickets to the cart.
/// Authentication is required — Ticket.UserAtSale is a NOT NULL FK to Users.UserId.
/// </summary>
public class ShowtimesController : ConsumerControllerBase
{
    private readonly AppDbContext _db;
    private readonly CartService _cart;
    private readonly SeatService _seatService;
    private readonly ILogger<ShowtimesController> _logger;

    /// <summary>Initializes ShowtimesController with database context, cart service, seat service, and logger.</summary>
    public ShowtimesController(AppDbContext db, CartService cart,
           SeatService seatService,
           ISystemAccountProvider systemAccounts,
           ILogger<ShowtimesController> logger)
           : base(systemAccounts, cart)
    {
        _db = db;
        _cart = cart;
        _seatService = seatService;
        _logger = logger;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the current session UserId, or null if no session is active.
    /// Delegates to <see cref="ConsumerControllerBase.SessionUserId"/> which uses
    /// <c>Session.GetInt32</c> — consistent with how both AuthService (staff login)
    /// and ConsumerControllerBase (WebUser auto-session) write the value via
    /// <c>Session.SetInt32</c>. Using GetString() here would always return null
    /// for a WebUser session, preventing guest ticket purchases.
    /// </summary>
    private int? GetSessionUserId() => SessionUserId;

    /// <summary>
    /// Centralised Showtime query with all navigation properties required by the
    /// booking view. Keeping includes in one place ensures GET and the POST failure
    /// path load identical data — avoids subtle differences between the two paths
    /// that can cause AvailableSeats to compute differently on re-render.
    ///
    /// ShowtimeSeats is included so AvailableSeats uses the accurate per-seat
    /// status count (SeatStatus.Available) rather than the Capacity - Tickets.Count
    /// fallback, which can drift if seats are held/reserved without a Ticket row.
    /// </summary>
    private async Task<Showtime?> LoadShowtimeAsync(int showtimeId) =>
        await _db.Showtimes
            .Include(st => st.Movie)
            .Include(st => st.TheaterScreen)
                .ThenInclude(ts => ts!.Location)
            .Include(st => st.Tickets)
            .Include(st => st.ShowtimeSeats)   // required for accurate AvailableSeats
                .ThenInclude(ss => ss.Seat)    // required for Seat.DisplayLabel label matching
            .FirstOrDefaultAsync(st => st.ShowtimeId == showtimeId && st.IsActive);

    // ── GET: ShowtimeBooking ───────────────────────────────────────────────

    /// <summary>
    /// Displays the booking page for a specific canonical Showtime.
    /// Requires an authenticated session — redirects to login if not present.
    /// </summary>
    /// <param name="id">ShowtimeId of the canonical Showtime row.</param>
    public async Task<IActionResult> ShowtimeBooking(int id)
    {
        // Auth guard — canonical schema requires UserAtSale (no guest checkout)
        if (GetSessionUserId() == null)
        {
            TempData["InfoMessage"] = "Please sign in to purchase tickets.";
            return RedirectToAction("Login", "Account",
                new { returnUrl = Url.Action(nameof(ShowtimeBooking), new { id }) });
        }

        var showtime = await LoadShowtimeAsync(id);
        if (showtime == null) return NotFound();

        var vm = new ShowtimeBookingViewModel
        {
            Showtime   = showtime,
            ShowtimeId = showtime.ShowtimeId,
            Quantity   = 1
        };

        return View(vm);
    }

    // ── POST: AddTicketToCart ──────────────────────────────────────────────

    /// <summary>
    /// Validates the booking form and adds ticket(s) to the session cart.
    /// Uses Showtime.PricePerTicket — no PriceTier lookup.
    /// UserAtSale is the logged-in user's session UserId.
    /// SeatNumbers (optional comma-separated labels) is forwarded to CartItem.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTicketToCart(ShowtimeBookingViewModel vm, string? postAction)
    {
        // ── Diagnostics: log all bound values on entry ─────────────────────
        _logger.LogDebug(
            "POST AddTicketToCart — ShowtimeId={ShowtimeId} Qty={Qty} Seats={Seats} PostAction={PostAction}",
            vm.ShowtimeId, vm.Quantity, vm.SeatNumbers ?? "(none)", postAction ?? "(none)");

        // Auth guard
        var userId = GetSessionUserId();
        if (userId == null)
        {
            TempData["InfoMessage"] = "Please sign in to purchase tickets.";
            return RedirectToAction("Login", "Account",
                new { returnUrl = Url.Action("ShowtimeBooking", new { id = vm.ShowtimeId }) });
        }

        // Re-load canonical Showtime with all nav props.
        // Never trust posted navigation objects; always re-fetch from the database.
        // LoadShowtimeAsync includes ShowtimeSeats so AvailableSeats is accurate
        // on the re-rendered view if we return early due to validation failure.
        var showtime = await LoadShowtimeAsync(vm.ShowtimeId);

        if (showtime == null)
        {
            TempData["ErrorMessage"] = "The selected showtime could not be found or is no longer active.";
            return RedirectToAction("MovieCatalog", "Movies");
        }

        // Re-attach for view rendering on validation failure
        vm.Showtime = showtime;

        if (!ModelState.IsValid)
        {
            // Log each field error so we can see exactly what failed
            foreach (var kvp in ModelState)
            {
                foreach (var err in kvp.Value.Errors)
                {
                    _logger.LogWarning(
                        "ModelState invalid — Field={Field} Error={Error}",
                        kvp.Key, err.ErrorMessage);
                }
            }

            return View("ShowtimeBooking", vm);
        }

        // Guard: enough seats
        if (showtime.AvailableSeats < vm.Quantity)
        {
            ModelState.AddModelError(string.Empty,
                $"Only {showtime.AvailableSeats} seat(s) remaining for this showing.");
            return View("ShowtimeBooking", vm);
        }

        var locationId = showtime.TheaterScreen?.LocationId;

        if (locationId is null or 0)
        {
            ModelState.AddModelError(string.Empty,
                "This showtime's theater location could not be determined. Please contact staff.");
            return View("ShowtimeBooking", vm);
        }

        // Build cart item — price from Showtime.PricePerTicket, user from session.
        // SeatNumbers forwarded so CartService / reservation logic knows which
        // specific seats to hold. Null/empty means no seat-specific reservation.
        var cartItem = new CartItem
        {
            ItemType     = CartItemType.Ticket,
            ShowtimeId   = showtime.ShowtimeId,
            MovieName    = showtime.Movie?.Title,
            ShowTime     = showtime.StartTime,
            LocationId   = locationId,
            LocationName = showtime.TheaterScreen?.Location?.LocationName,
            ScreenName   = showtime.TheaterScreen?.ScreenName,
            DisplayName  = $"{showtime.Movie?.Title} — {showtime.StartTime:h:mm tt}",
            UnitPrice    = showtime.PricePerTicket,
            Quantity     = vm.Quantity,
            UserAtSale   = userId.Value,
            SeatNumbers  = vm.SeatNumbers   // comma-separated labels, or null if none chosen
        };

        _logger.LogDebug(
            "CartItem built — ShowtimeId={ShowtimeId} Qty={Qty} UnitPrice={Price} Seats={Seats} UserId={UserId}",
            cartItem.ShowtimeId, cartItem.Quantity, cartItem.UnitPrice,
            cartItem.SeatNumbers ?? "(none)", cartItem.UserAtSale);

        // ── Seat reservation (seat-picker flow only) ───────────────────────
        // If the user picked specific seats, resolve each label to a ShowtimeSeat
        // row and attempt to reserve it. Failures are surfaced as ModelState errors
        // so the user can re-pick without losing their other selections.
        // ShowtimeSeatIds stays empty for general-admission (no SeatNumbers posted).
        if (!string.IsNullOrWhiteSpace(vm.SeatNumbers))
        {
            var labels = vm.SeatNumbers
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var label in labels)
            {
                // Query ShowtimeSeat directly with explicit ShowtimeId filter.
                // Do NOT use showtime.ShowtimeSeats (the EF-included collection) —
                // EF's identity map can return rows from a different showtime that
                // shares the same Seat entity, causing the wrong ShowtimeSeatId to
                // be reserved (e.g. showtime 49's B7 instead of showtime 50's B7).
                //
                // Parse the label into RowLabel + SeatNumber so EF can translate
                // the filter to SQL without string concatenation functions.
                var rowLabel   = new string(label.TakeWhile(char.IsLetter).ToArray());
                var seatNumStr = new string(label.SkipWhile(char.IsLetter).ToArray());

                if (!int.TryParse(seatNumStr, out var seatNumber))
                {
                    ModelState.AddModelError(string.Empty,
                        $"Seat label '{label}' is not in a recognised format.");
                    continue;
                }

                var ss = await _db.ShowtimeSeats
                    .FirstOrDefaultAsync(s =>
                        s.ShowtimeId == vm.ShowtimeId
                        && s.Seat!.RowLabel    == rowLabel
                        && s.Seat!.SeatNumber  == seatNumber
                        && s.Status            == SeatStatus.Available);

                if (ss == null)
                {
                    _logger.LogWarning(
                        "Seat not found or unavailable — ShowtimeId={ShowtimeId} Label={Label}",
                        showtime.ShowtimeId, label);

                    ModelState.AddModelError(string.Empty,
                        $"Seat {label} is no longer available. Please choose a different seat.");
                }
                else
                {
                    // Atomically flip Status Available → Reserved in the DB.
                    // Uses a conditional UPDATE so concurrent bookings cannot double-reserve.
                    var result = await _seatService.ReserveSeatAsync(ss.ShowtimeSeatId, userId.Value);

                    if (result == ReserveSeatResult.Success)
                    {
                        cartItem.ShowtimeSeatIds.Add(ss.ShowtimeSeatId);

                        _logger.LogDebug(
                            "Seat reserved — ShowtimeId={ShowtimeId} Label={Label} ShowtimeSeatId={SeatId}",
                            showtime.ShowtimeId, label, ss.ShowtimeSeatId);
                    }
                    else
                    {
                        // Another user grabbed the seat between the GET and POST
                        _logger.LogWarning(
                            "ReserveSeatAsync failed — ShowtimeId={ShowtimeId} Label={Label} Result={Result}",
                            showtime.ShowtimeId, label, result);

                        ModelState.AddModelError(string.Empty,
                            $"Seat {label} was just taken by another customer. Please choose a different seat.");
                    }
                }
            }

            // If any seat was unavailable, return the view with errors.
            // The re-hydration in seatPicker.js will restore the user's selections
            // so they only need to swap out the unavailable seat(s).
            if (!ModelState.IsValid)
                return View("ShowtimeBooking", vm);
        }

        // Location conflict check — concessions locked to a different theater?
        var existingConcessionLocationId = _cart.GetConcessionLocationId();
        if (existingConcessionLocationId.HasValue
            && existingConcessionLocationId.Value != locationId)
        {
            TempData["ConflictTicketShowtimeId"]    = showtime.ShowtimeId;
            TempData["ConflictTicketLocationId"]    = locationId.Value;
            TempData["ConflictTicketLocationName"]  = showtime.TheaterScreen?.Location?.LocationName;
            TempData["ConflictConcessionLocationId"] = existingConcessionLocationId.Value;

            // Cache pending cart item fields for replay after conflict resolution
            TempData["PendingTicket_ShowtimeId"]   = cartItem.ShowtimeId;
            TempData["PendingTicket_MovieName"]    = cartItem.MovieName;
            TempData["PendingTicket_ShowTime"]     = cartItem.ShowTime?.ToString("o");
            TempData["PendingTicket_LocationId"]   = cartItem.LocationId;
            TempData["PendingTicket_LocationName"] = cartItem.LocationName;
            TempData["PendingTicket_DisplayName"]  = cartItem.DisplayName;
            TempData["PendingTicket_UnitPrice"]    = cartItem.UnitPrice;
            TempData["PendingTicket_Quantity"]     = cartItem.Quantity;
            TempData["PendingTicket_UserAtSale"]   = cartItem.UserAtSale;
            TempData["PendingTicket_SeatNumbers"]  = cartItem.SeatNumbers;  // preserve for replay
            TempData["PendingTicket_PostAction"]   = postAction;

            return RedirectToAction("LocationConflict", "Concessions");
        }

        _cart.AddItem(cartItem);
        TempData["SuccessMessage"] = $"{vm.Quantity} ticket(s) added to your cart!";

        if (string.Equals(postAction, "addAndGoToConcessions", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("ConcessionsCatalog", "Concessions",
                new { locationId = locationId.Value, fromTicketLocationId = locationId.Value });
        }

        return RedirectToAction("CartReview", "Cart");
    }
}
