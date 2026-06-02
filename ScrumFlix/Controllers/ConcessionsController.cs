/*
 * File: /ScrumFlix/Controllers/ConcessionsController.cs
 * Description: Controller for the concessions catalog page and adding concession items to the cart.
 *
 * Phase 3 — Backend Alignment (#32 / P3-6):
 *   REMOVED (phantom schema):
 *     - _db.ConcessionsPricing (phantom table) → replaced with _db.ConcessionItems
 *     - Inventory include → ConcessionItem is now the direct entity
 *     - Location-specific pricing → single Price on ConcessionItem
 *     - base64 InventoryItemId → ConcessionItemId (int)
 *     - locationId parameter on AddConcessionToCart (catalog has no location switcher)
 *
 *   ADDED:
 *     - Query ConcessionItem WHERE IsActive = true directly
 *     - ConcessionItemId (int) passed to cart instead of base64 binary ID
 *     - Stock check before adding to cart (QuantityInStock >= quantity)
 *     - BuildPendingTicketFromTempData updated for canonical CartItem fields
 *     
 * SPRINT S1 PATCH — ConcessionsController.cs (LocationConflict action only)
 *
 * Replace the existing LocationConflict() action body with the version below.
 * The rest of ConcessionsController.cs is UNCHANGED.
 *
 * Change summary:
 *   - Removed: ViewBag.TicketLocationId, ViewBag.TicketLocationName,
 *              ViewBag.ConcessionLocationId
 *   - Added:   LocationConflictViewModel populated from TempData.Peek()
 *   - Returns: View(vm) with strongly-typed model
 *   - View:    Views/Concessions/LocationConflict.cshtml must be updated to
 *              @model LocationConflictViewModel (see updated view file)
 *
 */

namespace ScrumFlix.Controllers;

/// <summary>
/// Handles the concessions catalog page and cart add operations for food and beverage items.
/// </summary>
public class ConcessionsController : ConsumerControllerBase
{
    private readonly AppDbContext _db;
    private readonly CartService _cart;
    private readonly ILogger<ConcessionsController> _logger;

    /// <summary>Initializes ConcessionsController with database context and cart service.</summary>
    public ConcessionsController(AppDbContext db, CartService cart, ISystemAccountProvider systemAccounts,
        ILogger<ConcessionsController> logger)
        : base(systemAccounts, cart)          // ADD cart here
    {
        _db = db;
        _cart = cart;
        _logger = logger;
    }

    // ── GET: ConcessionsCatalog ────────────────────────────────────────────

    /// <summary>
    /// Displays the concessions catalog showing all active ConcessionItems with their
    /// canonical single prices. Location-specific pricing has been removed — canonical
    /// ConcessionItem has one Price field across all locations.
    ///
    /// When <paramref name="fromTicketLocationId"/> is supplied the page renders in
    /// "ticket flow" mode and displays a contextual banner.
    /// </summary>
    /// <param name="locationId">Optional location context (used for banner only, not pricing).</param>
    /// <param name="fromTicketLocationId">Set by ShowtimesController when "Add Concessions Too" clicked.</param>
    public async Task<IActionResult> ConcessionsCatalog(int? locationId, int? fromTicketLocationId)
    {
        // Active concession items — single price per item (no location-specific pricing)
        var items = await _db.ConcessionItems
            .Where(ci => ci.IsActive)
            .OrderBy(ci => ci.ItemName)
            .AsNoTracking()
            .ToListAsync();

        // Locations used only for the banner / context display (not for pricing)
        var locations = await _db.Locations
            .Where(l => l.IsActive)
            .OrderBy(l => l.LocationName)
            .AsNoTracking()
            .ToListAsync();

        string? ticketLocationName = null;
        if (fromTicketLocationId.HasValue)
        {
            ticketLocationName = locations
                .FirstOrDefault(l => l.LocationId == fromTicketLocationId.Value)?.LocationName;
        }

        var vm = new ConcessionsCatalogViewModel
        {
            ConcessionItems = items,
            Locations = locations,
            SelectedLocationId = locationId ?? fromTicketLocationId,
            TicketLocationId = fromTicketLocationId,
            TicketLocationName = ticketLocationName
        };

        return View(vm);
    }

    // ── Location Conflict pages (unchanged logic, updated CartItem fields) ─

    /// <summary>
    /// Displays the location conflict resolution page when the user tries to add a ticket
    /// at a theater that differs from the location of concessions already in their cart.
    /// Returns a strongly-typed <see cref="LocationConflictViewModel"/> — no ViewBag.
    /// </summary>
    public IActionResult LocationConflict()
    {
        if (TempData["ConflictTicketLocationId"] == null)
            return RedirectToAction("CartReview", "Cart");

        var vm = new LocationConflictViewModel
        {
            TicketLocationId = TempData.Peek("ConflictTicketLocationId") as int?,
            TicketLocationName = TempData.Peek("ConflictTicketLocationName") as string,
            ConcessionLocationId = TempData.Peek("ConflictConcessionLocationId") as int?
        };

        return View(vm);
    }

    /// <summary>
    /// Resolves a location conflict — either drop concessions and keep the ticket,
    /// or discard the ticket and keep concessions at their current location.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult RelocateConcessions(string resolution)
    {
        var ticketLocationId = TempData["ConflictTicketLocationId"] is int tli ? tli : (int?)null;
        var concessionLocationId = TempData["ConflictConcessionLocationId"] is int cli ? cli : (int?)null;

        if (string.Equals(resolution, "switchConcessions", StringComparison.OrdinalIgnoreCase))
        {
            // Remove concession items; keep tickets
            var cart = _cart.GetCart();
            cart.RemoveAll(c => c.ItemType == CartItemType.Concession);
            _cart.ClearCart();
            foreach (var item in cart) _cart.AddItem(item);

            // Re-add the pending ticket
            var pending = BuildPendingTicketFromTempData();
            if (pending != null) _cart.AddItem(pending);

            TempData["SuccessMessage"] =
                "Ticket added! Concessions cleared — please re-add them for your theater.";

            return RedirectToAction(nameof(ConcessionsCatalog), new
            {
                locationId = ticketLocationId,
                fromTicketLocationId = ticketLocationId
            });
        }
        else // switchMovies — keep concessions, discard pending ticket
        {
            TempData["SuccessMessage"] =
                "Showing movies at your concessions theater. Select a showtime there.";
            return RedirectToAction("MovieCatalog", "Movies",
                new { locationId = concessionLocationId });
        }
    }

    // ── POST: AddConcessionToCart ──────────────────────────────────────────

    /// <summary>
    /// Adds a canonical ConcessionItem to the cart by its integer ConcessionItemId.
    /// Validates stock availability before adding.
    /// Responds with JSON for AJAX calls (X-Requested-With: XMLHttpRequest).
    /// </summary>
    /// <param name="concessionItemId">Canonical ConcessionItem.ConcessionItemId (int).</param>
    /// <param name="quantity">Quantity to add.</param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddConcessionToCart(int concessionItemId, int quantity)
    {
        if (quantity < 1) quantity = 1;

        var item = await _db.ConcessionItems.FindAsync(concessionItemId);
        if (item == null || !item.IsActive)
        {
            TempData["ErrorMessage"] = "Item not available.";
            return RedirectToAction(nameof(ConcessionsCatalog));
        }

        if (item.QuantityInStock < quantity)
        {
            TempData["ErrorMessage"] =
                $"Only {item.QuantityInStock} unit(s) of {item.ItemName} in stock.";

            var isAjaxStock = string.Equals(
                Request.Headers["X-Requested-With"], "XMLHttpRequest",
                StringComparison.OrdinalIgnoreCase);

            if (isAjaxStock)
                return Json(new { success = false, message = TempData["ErrorMessage"] });

            return RedirectToAction(nameof(ConcessionsCatalog));
        }

        var cartItem = new CartItem
        {
            ItemType = CartItemType.Concession,
            ConcessionItemId = item.ConcessionItemId,
            DisplayName = item.ItemName,
            UnitPrice = item.Price,
            Quantity = quantity,
            LocationId = item.LocationId   // canonical: ConcessionItem.LocationId FK
        };

        _cart.AddItem(cartItem);
        _logger.LogInformation("UserId={UserId} added {Qty}x ConcessionItemId={ItemId} '{Name}' to cart.",
            SessionUserId, quantity, item.ConcessionItemId, item.ItemName);
        TempData["SuccessMessage"] = $"{quantity}x {item.ItemName} added to cart!";

        var cartCount = _cart.GetCart().Sum(ci => ci.Quantity);

        var isAjax = string.Equals(
            Request.Headers["X-Requested-With"], "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase);

        if (isAjax)
            return Json(new { success = true, cartCount, message = TempData["SuccessMessage"] });

        return RedirectToAction("CartReview", "Cart");
    }

    // ── Private helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Reconstructs a pending ticket CartItem from TempData set by ShowtimesController
    /// before the location conflict redirect. Returns null if required fields are missing.
    /// </summary>
    private CartItem? BuildPendingTicketFromTempData()
    {
        if (TempData["PendingTicket_ShowtimeId"] is not int showtimeId) return null;

        decimal unitPrice = TempData["PendingTicket_UnitPrice"] is decimal p ? p : 0m;
        int quantity = TempData["PendingTicket_Quantity"] is int q ? q : 1;
        int userAtSale = TempData["PendingTicket_UserAtSale"] is int u ? u : 0;
        DateTime? showTime = DateTime.TryParse(
            TempData["PendingTicket_ShowTime"] as string, out var dt) ? dt : null;

        return new CartItem
        {
            ItemType    = CartItemType.Ticket,
            ShowtimeId  = showtimeId,
            UserAtSale  = userAtSale,
            MovieName   = TempData["PendingTicket_MovieName"] as string,
            ShowTime    = showTime,
            LocationId  = TempData["PendingTicket_LocationId"] is int lid ? lid : null,
            LocationName = TempData["PendingTicket_LocationName"] as string,
            DisplayName = TempData["PendingTicket_DisplayName"] as string ?? "Ticket",
            UnitPrice   = unitPrice,
            Quantity    = quantity,
            // Restore seat picker selections so the replayed ticket carries the
            // same seat labels the user originally chose before the conflict.
            // ShowtimeSeatIds stays empty (default) — seats will be re-resolved
            // by ShowtimesController if the user re-submits the booking form,
            // but in the conflict-replay path the cart item is added directly
            // so SeatNumbers is preserved for display / downstream reference.
            SeatNumbers = TempData["PendingTicket_SeatNumbers"] as string
        };
    }
}
