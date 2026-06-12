/*
 * File: /ScrumFlix/Controllers/CartController.cs
 * Description: Controller for the shopping cart review page, quantity management, and checkout.
 *
 * Phase 3 — Backend Alignment (#31 / P3-5 + #33 / P3-7):
 *   REMOVED (phantom schema):
 *     - _db.Customers, _db.PriceTiers, _db.ScheduledShows → phantom tables
 *     - PriceTier price lookup → Showtime.PricePerTicket used directly (stored in CartItem.UnitPrice)
 *     - TicketsSold increment on ScheduledShow → canonical availability is COUNT(Ticket)
 *     - GuestEmail on Ticket → not in canonical schema (UserAtSale FK replaces it)
 *     - ShowId / PriceTierId on Ticket → replaced with ShowtimeId / UserAtSale
 *     - ConcessionsSale / ConcessionsSalesItem (phantom) → ConcessionSale / ConcessionSaleItem
 *
 *   ADDED (#31 / P3-5):
 *     - Auth guard on Checkout: requires session UserId
 *     - TicketCode generation: random 6-digit int with DB uniqueness check
 *     - Ticket written with: TicketCode, ShowtimeId, UserAtSale, TimeOfSale
 *     - Availability guard: COUNT(Ticket WHERE ShowtimeId=X) < Showtime.Capacity
 *
 *   ADDED (#33 / P3-7):
 *     - ConcessionSale written with: UserId, CustomerEmail (from session), TimeOfSale, Total
 *     - ConcessionSaleItem written with: ConcessionSaleId, ConcessionItemId, Quantity, UnitPrice, LineTotal
 *     - ConcessionItem.QuantityInStock decremented atomically inside transaction
 *     - Low-stock check after decrement (QuantityInStock <= Minimum → admin alert flag)
 *
 * Phase 2 Audit — F-01 Fix:
 *   ADDED:
 *     - SeatService injected into constructor (fixes F-01: FinalizeSeatsAsync not wired)
 *     - Inside ticket transaction: after all Ticket rows are written, any cart items that
 *       carry a ShowtimeSeatId (assigned-seat flows) are collected and passed to
 *       SeatService.FinalizeSeatsAsync before CommitAsync.
 *       This atomically flips ShowtimeSeat.Status from 'Reserved' → 'Sold' and removes
 *       the corresponding SeatReservation rows within the same transaction.
 *     - General-admission items (ShowtimeSeatId == null) are unaffected — the finalization
 *       call is a no-op if the collected ID list is empty.
 *
 * Known Constraint (F-05 / TOCTOU):
 *   The availability guard for general-admission flows reads Showtime.Tickets.Count at
 *   transaction start without a row lock. Two concurrent sessions could both pass the check
 *   before either commits (slight overbooking risk). This is accepted per spec §4 which
 *   mandates COUNT(Ticket) — not a SELECT FOR UPDATE lock. For assigned-seat flows this
 *   risk is eliminated entirely by the SeatService.ReserveSeatAsync atomic UPDATE strategy.
 *   
 * SPRINT S1 PATCH — CartController.cs (OrderConfirmation action only)
 *
 * Change summary:
 *   - Removed: ViewBag.OrderTotal, ViewBag.IssuedCodes, ViewBag.QrCodes
 *   - Added:   OrderConfirmationViewModel populated from TempData
 *   - Returns: View(vm) with strongly-typed model
 *   - View:    Views/Cart/OrderConfirmation.cshtml updated to @model OrderConfirmationViewModel
 *
 * SPRINT S4 PATCH — CartController.cs
 *
 * Change summary:
 *   - FIXED: GetSessionUserId() now delegates to ConsumerControllerBase.SessionUserId
 *            (GetInt32) instead of Session.GetString(). GetString always returned null
 *            for the WebUser session set by ConsumerControllerBase via SetInt32,
 *            causing the checkout auth guard to block every anonymous/WebUser purchase.
 *   - FIXED: CurrentUserEmail now reads AuthService.SessionUserName (the key
 *            actually written on login) instead of the non-existent "UserEmail" key.
 *   - ADDED: IEmailService injected; SendOrderConfirmationAsync() called after a
 *            successful ticket checkout. Non-fatal — failure is logged, not thrown.
 */

namespace ScrumFlix.Controllers;

/// <summary>
/// Handles cart review, item removal, quantity updates, and checkout against the canonical schema.
/// </summary>
public class CartController : ConsumerControllerBase   // after S2
{
    private readonly CartService _cart;
    private readonly AppDbContext _db;
    private readonly QrCodeService _qr;
    private readonly SeatService _seatService;
    private readonly IAuditService _audit;            // F-04: added
    private readonly IEmailService _email;            // S4: transactional email
    private readonly Random _rng = new();

    public CartController(CartService cart, AppDbContext db, QrCodeService qr,
        SeatService seatService, IAuditService audit,
        IEmailService email,                           // S4: transactional email
        ISystemAccountProvider systemAccounts)         // added per S2
        : base(systemAccounts, cart)          // ADD cart here
    {
        _cart = cart;
        _db = db;
        _qr = qr;
        _seatService = seatService;
        _audit = audit;                          // F-04: added
        _email = email;                          // S4: added
    }


    // ── Session helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Returns the current session UserId via ConsumerControllerBase.SessionUserId
    /// (uses GetInt32 — consistent with SetInt32 writes from both AuthService
    /// and ConsumerControllerBase). Using GetString() would return null for
    /// WebUser sessions and block guest checkout.
    /// </summary>
    private int? GetSessionUserId() => SessionUserId;


    // ── Cart review ────────────────────────────────────────────────────────

    /// <summary>Displays the cart review page with all items, subtotal, tax, and total.</summary>
    public IActionResult CartReview()
    {
        var vm = new CartViewModel
        {
            Items    = _cart.GetCart(),
            Subtotal = _cart.GetSubtotal(),
            Tax      = _cart.GetTax(),
            Total    = _cart.GetTotal()
            // CustomerReceiptEmail is left null — the customer types it on the page
        };
        return View(vm);
    }

    /// <summary>Removes a specific item from the cart by its line item ID.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult RemoveItem(string cartItemId)
    {
        _cart.RemoveItem(cartItemId);
        return RedirectToAction(nameof(CartReview));
    }

    /// <summary>Updates the quantity of a specific cart line item.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult UpdateQuantity(string cartItemId, int quantity)
    {
        _cart.UpdateQuantity(cartItemId, quantity);
        return RedirectToAction(nameof(CartReview));
    }

    /// <summary>Clears all items from the cart.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ClearCart()
    {
        _cart.ClearCart();
        return RedirectToAction(nameof(CartReview));
    }

    // ── GET: CartCount (AJAX badge refresh) ────────────────────────────────

    /// <summary>Returns the current cart item count as JSON for live badge updates.</summary>
    [HttpGet]
    public IActionResult GetCartCount()
        => Json(new { count = _cart.GetItemCount() });

    // ── POST: Checkout ─────────────────────────────────────────────────────

    /// <summary>
    /// Processes checkout against the canonical schema:
    ///   - Tickets: generates TicketCode, writes Ticket rows with UserAtSale, inside a transaction.
    ///             For assigned-seat flows: calls SeatService.FinalizeSeatsAsync inside the
    ///             same transaction to flip ShowtimeSeat.Status Reserved → Sold (F-01 fix).
    ///   - Concessions: writes ConcessionSale + ConcessionSaleItem rows,
    ///                  decrements ConcessionItem.QuantityInStock atomically.
    /// Authentication required — Ticket.UserAtSale is NOT NULL FK to Users.UserId.
    ///
    /// customerReceiptEmail: optional email supplied by the customer on the CartReview
    /// page for receiving a purchase confirmation receipt. This is NOT the Web Sales
    /// System account email — it is a one-time address used solely for that receipt
    /// and is never stored after the email has been sent.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(string? customerReceiptEmail = null)
    {
        var items = _cart.GetCart();
        if (!items.Any()) return RedirectToAction(nameof(CartReview));

        // Auth guard
        var userId = GetSessionUserId();
        if (userId == null)
        {
            TempData["InfoMessage"] = "Please sign in to complete your purchase.";
            return RedirectToAction("Login", "Account",
                new { returnUrl = Url.Action(nameof(CartReview)) });
        }

        var userEmail = CurrentUserEmail ?? string.Empty;

        // ── Ticket checkout ────────────────────────────────────────────────
        var ticketItems = items.Where(i => i.ItemType == CartItemType.Ticket).ToList();
        var issuedCodes = new List<long>(); // collected for confirmation page — must stay new List for across-scope use

        if (ticketItems.Any())
        {
            // EnableRetryOnFailure (Program.cs) forbids bare BeginTransaction —
            // explicit transactions must run inside the execution strategy so the
            // ENTIRE unit (begin → work → commit) can be replayed on a transient
            // connection failure. The lambda returns an IActionResult for early
            // validation exits, or null on success.
            var ticketStrategy = _db.Database.CreateExecutionStrategy();
            var ticketEarlyExit = await ticketStrategy.ExecuteAsync(async () =>
            {
                // ── Retry-safe state reset ─────────────────────────────────
                // On a retried attempt, the failed attempt's entities are still
                // tracked as Added and issuedCodes still holds its codes —
                // without this reset a retry would double-insert tickets.
                _db.ChangeTracker.Clear();
                issuedCodes.Clear();

                using var tx = await _db.Database.BeginTransactionAsync();
                try
                {
                    foreach (var item in ticketItems)
                    {
                        if (!item.ShowtimeId.HasValue)
                            return (IActionResult?)BadRequest("Cart contains a ticket item with no ShowtimeId.");

                        // Load canonical Showtime + Tickets for availability check
                        var showtime = await _db.Showtimes
                            .Include(st => st.Tickets)
                            .FirstOrDefaultAsync(st => st.ShowtimeId == item.ShowtimeId.Value);

                        if (showtime == null)
                            return NotFound($"Showtime {item.ShowtimeId} not found.");

                        var soldCount = showtime.Tickets.Count;
                        var remaining = showtime.Capacity - soldCount;

                        if (remaining < item.Quantity)
                            return BadRequest(
                                $"Only {remaining} seat(s) remaining for this showing. " +
                                $"Requested: {item.Quantity}.");

                        for (int q = 0; q < item.Quantity; q++)
                        {
                            var code = await GenerateUniqueTicketCodeAsync();
                            issuedCodes.Add(code);

                            // ShowtimeSeatId: assign the q-th resolved seat ID when available
                            // (seat-picker flow). Null for general-admission flows where
                            // ShowtimeSeatIds is empty. Index guard prevents out-of-range
                            // if Quantity somehow exceeds the number of picked seats.
                            int? seatId = q < item.ShowtimeSeatIds.Count
                                ? item.ShowtimeSeatIds[q]
                                : (int?)null;

                            _db.Tickets.Add(new Ticket
                            {
                                TicketCode      = code,
                                ShowtimeId      = item.ShowtimeId.Value,
                                UserAtSale      = item.UserAtSale > 0 ? item.UserAtSale : userId.Value,
                                TimeOfSale      = DateTime.UtcNow,
                                ShowtimeSeatId  = seatId   // null for GA; resolved ID for seat-picker
                            });
                        }
                    }

                    await _db.SaveChangesAsync();

                    // ── F-01 FIX: Finalize reserved seats within the same transaction ──
                    // Flatten ShowtimeSeatIds across all assigned-seat ticket items.
                    // CartItem.ShowtimeSeatIds is a List<int> (empty for GA flows) populated
                    // by ShowtimesController when the seat picker resolves labels to IDs.
                    // Distinct() guards against accidental duplicates across cart items.
                    // FinalizeSeatsAsync is a no-op if the list is empty, so GA flows are
                    // unaffected by this call.
                    var seatIdsToFinalize = ticketItems
                        .SelectMany(i => i.ShowtimeSeatIds)
                        .Distinct()
                        .ToList();

                    if (seatIdsToFinalize.Any())
                    {
                        // FinalizeSeatsAsync: flips Status → 'Sold', removes SeatReservation rows.
                        // Must run within this transaction so Ticket creation and seat finalization
                        // are atomic — a failure rolls back both.
                        await _seatService.FinalizeSeatsAsync(seatIdsToFinalize);
                    }
                    // ── End F-01 fix ───────────────────────────────────────────

                    await tx.CommitAsync();
                    return null; // success — no early exit
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });

            if (ticketEarlyExit is not null) return ticketEarlyExit;
        }

        // ── Concessions checkout ───────────────────────────────────────────
        var concessionItems = items.Where(i => i.ItemType == CartItemType.Concession).ToList();

        // Captured after the concession transaction commits so we can generate the QR receipt.
        ConcessionSale? completedSale = null;

        if (concessionItems.Any())
        {
            // Same execution-strategy wrapper as the ticket block above —
            // required by EnableRetryOnFailure. Lambda returns an early-exit
            // IActionResult or null on success.
            var concessionStrategy = _db.Database.CreateExecutionStrategy();
            var concessionEarlyExit = await concessionStrategy.ExecuteAsync(async () =>
            {
                // ── Retry-safe state reset ─────────────────────────────────
                // Detach any entities left tracked by a failed prior attempt
                // (and the committed ticket block's entities — harmless, they
                // are reloaded fresh where needed below).
                _db.ChangeTracker.Clear();
                completedSale = null;

                using var tx = await _db.Database.BeginTransactionAsync();
                try
                {
                    // Derive LocationId from the cart items — all concession items in the
                    // cart are guaranteed to share the same location (enforced by the
                    // location-conflict check in ShowtimesController). Fall back to the
                    // DB-loaded item's LocationId as a safety net.
                    var saleLocationId = concessionItems
                        .Where(i => i.LocationId.HasValue)
                        .Select(i => i.LocationId!.Value)
                        .FirstOrDefault();

                    var sale = new ConcessionSale
                    {
                        UserId = userId.Value,
                        CustomerEmail = userEmail,
                        TimeOfSale = DateTime.UtcNow,
                        Total = concessionItems.Sum(i => i.LineTotal),
                        LocationId = saleLocationId  // correctly set from cart, not hardcoded default
                    };

                    _db.ConcessionSales.Add(sale);
                    await _db.SaveChangesAsync(); // get ConcessionSaleId

                    foreach (var ci in concessionItems)
                    {
                        if (!ci.ConcessionItemId.HasValue) continue;

                        // Reload with tracking so we can decrement stock atomically
                        var concItem = await _db.ConcessionItems
                            .FirstOrDefaultAsync(x => x.ConcessionItemId == ci.ConcessionItemId.Value);

                        if (concItem == null) continue;

                        // Safety net: if cart LocationId was missing, use the DB item's location
                        if (sale.LocationId == 0)
                            sale.LocationId = concItem.LocationId;

                        if (concItem.QuantityInStock < ci.Quantity)
                            return (IActionResult?)BadRequest(
                                $"Insufficient stock for {concItem.ItemName}. " +
                                $"Available: {concItem.QuantityInStock}, Requested: {ci.Quantity}.");

                        // Atomic stock decrement
                        concItem.QuantityInStock -= ci.Quantity;

                        // F-04: Low-stock admin alert — writes an AuditLog entry so the admin
                        // dashboard can surface inventory warnings without a separate notification
                        // service. Replace with AdminNotificationService.FlagLowStockAsync() when
                        // that pattern is established (Sprint S6+).
                        if (concItem.QuantityInStock <= concItem.Minimum)
                        {
                            await _audit.LogAsync(
                                userId: userId.Value,
                                actionType: "LowStock",
                                tableName: "ConcessionItem",
                                objectId: concItem.ConcessionItemId,
                                description: $"{concItem.ItemName} at or below minimum: " +
                                             $"{concItem.QuantityInStock} remaining " +
                                             $"(minimum: {concItem.Minimum}).");
                        }

                        _db.ConcessionSaleItems.Add(new ConcessionSaleItem
                        {
                            ConcessionSaleId = sale.ConcessionSaleId,
                            ConcessionItemId = concItem.ConcessionItemId,
                            Quantity = ci.Quantity,
                            UnitPrice = ci.UnitPrice,
                            LineTotal = ci.LineTotal
                        });
                    }

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                    completedSale = sale;
                    return null; // success — no early exit
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });

            if (concessionEarlyExit is not null) return concessionEarlyExit;
        }

        // ── Concession QR receipt ──────────────────────────────────────────
        // Build a proof-of-purchase QR code for the concession order so the
        // customer can show it at the stand to collect their pre-purchased items.
        if (completedSale != null)
        {
            var concessionQrItems = concessionItems
                .Where(i => i.ConcessionItemId.HasValue)
                .Select(i => (ItemName: i.DisplayName, Quantity: i.Quantity));

            var concessionPayload = QrCodeService.BuildConcessionPayload(
                orderId:    completedSale.ConcessionSaleId,
                timeOfSale: completedSale.TimeOfSale,
                items:      concessionQrItems,
                total:      completedSale.Total);

            TempData["ConcessionQrCode"] = _qr.GenerateBase64PngWithPayload(concessionPayload);
        }

        // Capture all totals BEFORE clearing the cart
        var subtotal = _cart.GetSubtotal();
        var tax      = _cart.GetTax();
        var total    = _cart.GetTotal();

        // ── Snapshot cart before clearing ─────────────────────────────────
        // Serialize full cart to TempData so OrderConfirmation can render
        // the itemized breakdown after the cart has been cleared.
        var cartSnapshot = _cart.GetCart();
        TempData["OrderItems"] = System.Text.Json.JsonSerializer.Serialize(
            cartSnapshot.Select(i => new
            {
                i.DisplayName, i.Quantity, i.UnitPrice, i.LineTotal,
                IsConcession = i.ItemType == CartItemType.Concession,
                i.MovieName, i.ShowTime, i.LocationName, i.ScreenName, i.SeatNumbers
            }));

        _cart.ClearCart();


        TempData["OrderSubtotal"]  = subtotal.ToString("C");
        TempData["OrderTax"]       = tax.ToString("C");
        TempData["OrderTotal"]     = total.ToString("C");
        TempData["IssuedCodes"]    = string.Join(",", issuedCodes);
        TempData["SuccessMessage"] = "Your order has been placed successfully!";

        // ── Seat labels + structured QR payloads ──────────────────────────
        // Load tickets with seat info for QR payload building and seat display.
        var ticketsWithSeats = await _db.Tickets
            .Where(t => issuedCodes.Contains(t.TicketCode))
            .Include(t => t.ShowtimeSeat)
                .ThenInclude(ss => ss!.Seat)
            .Include(t => t.Showtime)
                .ThenInclude(st => st!.TheaterScreen)
                    .ThenInclude(ts => ts!.Location)   // needed for TimeZoneId
            .OrderBy(t => t.TicketCode)
            .ToListAsync();

        List<string> seatLabels  = [];
        List<string> screenNames = [];
        List<string> qrPayloads  = [];
        var snapshotTicketItems = cartSnapshot.Where(i => i.ItemType == CartItemType.Ticket).ToList();

        foreach (var ticket in ticketsWithSeats)
        {
            var seatLabel  = ticket.ShowtimeSeat?.Seat != null
                ? ticket.ShowtimeSeat.Seat.RowLabel + ticket.ShowtimeSeat.Seat.SeatNumber
                : string.Empty;

            var screenName = ticket.Showtime?.TheaterScreen?.ScreenName ?? string.Empty;

            seatLabels.Add(seatLabel);
            screenNames.Add(screenName);

            // Match cart snapshot item to get movie name and show time for the QR payload
            var cartItem   = snapshotTicketItems.FirstOrDefault(i => i.ShowtimeId == ticket.ShowtimeId);
            var timeZoneId = ticket.Showtime?.TheaterScreen?.Location?.TimeZoneId;

            var payload = QrCodeService.BuildTicketPayload(
                ticketCode:   ticket.TicketCode,
                movieName:    cartItem?.MovieName,
                showTime:     cartItem?.ShowTime,
                seatLabel:    seatLabel,
                screenName:   screenName,
                locationName: cartItem?.LocationName,
                timeZoneId:   timeZoneId);

            qrPayloads.Add(payload);
        }

        TempData["SeatLabels"]  = string.Join(",", seatLabels);
        TempData["ScreenNames"] = string.Join(",", screenNames);
        TempData["QrPayloads"]  = System.Text.Json.JsonSerializer.Serialize(qrPayloads);

        // ── S4 + Customer Receipt: Send confirmation emails ──────────
        // Both blocks run after cart is cleared and TempData is set.
        // Non-fatal — delivery failures are logged but never abort checkout.

        // S4: Ticket order confirmation — sent to the logged-in staff/user
        // account email (the Web Sales System session email, NOT the customer
        // receipt address).
        if (issuedCodes.Any())
        {
            var emailAddress = CurrentUserEmail ?? string.Empty;
            var displayName  = emailAddress.Contains('@')
                ? emailAddress.Split('@')[0]
                : (CurrentUserEmail ?? "Guest");

            if (!string.IsNullOrWhiteSpace(emailAddress))
            {
                await _email.SendOrderConfirmationAsync(
                    toEmail:     emailAddress,
                    toName:      displayName,
                    ticketCodes: issuedCodes,
                    orderTotal:  total.ToString("C"));
            }
        }

        // Customer receipt — sent to the optional address the customer typed
        // on the CartReview page. Covers tickets, concessions, or both.
        // Mirrors the OrderConfirmation page: QR codes, seat labels, itemized breakdown.
        // This address is NOT the Web Sales System account email and is NOT stored.
        if (!string.IsNullOrWhiteSpace(customerReceiptEmail))
        {
            // Generate QR code PNGs from the already-built qrPayloads list
            var receiptQrCodes = _qr.GenerateBase64PngBatch(qrPayloads);

            // Build ReceiptLineItem list from the cart snapshot
            var receiptLines = cartSnapshot
                .Select(i => new ScrumFlix.Services.ReceiptLineItem
                {
                    DisplayName  = i.DisplayName,
                    Quantity     = i.Quantity,
                    UnitPrice    = i.UnitPrice,
                    LineTotal    = i.LineTotal,
                    IsConcession = i.ItemType == CartItemType.Concession,
                    MovieName    = i.MovieName,
                    ShowTime     = i.ShowTime,
                    LocationName = i.LocationName,
                    ScreenName   = i.ScreenName,
                    SeatNumbers  = i.SeatNumbers
                })
                .ToList();

            await _email.SendPurchaseReceiptAsync(
                toEmail:            customerReceiptEmail,
                orderSubtotal:      subtotal.ToString("C"),
                orderTax:           tax.ToString("C"),
                orderTotal:         total.ToString("C"),
                timeOfSale:         DateTime.UtcNow,
                ticketCodes:        issuedCodes,
                qrCodeBase64s:      receiptQrCodes,
                seatLabels:         seatLabels,
                screenNames:        screenNames,
                orderItems:         receiptLines,
                concessionQrBase64: TempData.Peek("ConcessionQrCode") as string);
        }
        // ── End confirmation emails ────────────────────────────────────

        return RedirectToAction(nameof(OrderConfirmation));
    }

    // ── OrderConfirmation ──────────────────────────────────────────────────

    /// <summary>
    /// Displays the order confirmation page after a successful checkout.
    /// Generates a QR code PNG (Base64) for each issued ticket code via QrCodeService.
    /// Returns a strongly-typed <see cref="OrderConfirmationViewModel"/> — no ViewBag.
    /// </summary>
    public IActionResult OrderConfirmation()
    {
        var codes = (TempData["IssuedCodes"] as string ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => long.TryParse(s, out var c) ? c : 0L)
            .Where(c => c > 0)
            .ToList();

        // Restore seat labels — parallel-indexed with codes.
        var seatLabels = (TempData["SeatLabels"] as string ?? string.Empty)
            .Split(',', StringSplitOptions.None)
            .ToList();

        // Restore screen names — parallel-indexed with codes.
        var screenNames = (TempData["ScreenNames"] as string ?? string.Empty)
            .Split(',', StringSplitOptions.None)
            .ToList();

        // Restore structured QR payloads — use code-only fallback if missing.
        var qrPayloadsJson = TempData["QrPayloads"] as string;
        List<string> qrCodes;
        if (!string.IsNullOrEmpty(qrPayloadsJson))
        {
            var payloads = System.Text.Json.JsonSerializer
                .Deserialize<List<string>>(qrPayloadsJson) ?? [];
            qrCodes = _qr.GenerateBase64PngBatch(payloads);
        }
        else
        {
            qrCodes = _qr.GenerateBase64PngBatch(codes);
        }

        // Restore order items for the breakdown display.
        var orderItemsJson = TempData["OrderItems"] as string;
        List<OrderLineItem> orderItems = [];
        if (!string.IsNullOrEmpty(orderItemsJson))
        {
            try
            {
                var raw = System.Text.Json.JsonSerializer
                    .Deserialize<List<System.Text.Json.JsonElement>>(orderItemsJson);
                if (raw != null)
                {
                    foreach (var el in raw)
                    {
                        orderItems.Add(new OrderLineItem
                        {
                            DisplayName  = el.GetProperty("DisplayName").GetString() ?? string.Empty,
                            Quantity     = el.GetProperty("Quantity").GetInt32(),
                            UnitPrice    = el.GetProperty("UnitPrice").GetDecimal(),
                            LineTotal    = el.GetProperty("LineTotal").GetDecimal(),
                            IsConcession = el.GetProperty("IsConcession").GetBoolean(),
                            MovieName    = el.TryGetProperty("MovieName", out var mn) ? mn.GetString() : null,
                            ShowTime     = el.TryGetProperty("ShowTime", out var st) && st.ValueKind != System.Text.Json.JsonValueKind.Null
                                           ? st.GetDateTime() : null,
                            LocationName = el.TryGetProperty("LocationName", out var ln) ? ln.GetString() : null,
                            ScreenName   = el.TryGetProperty("ScreenName", out var scr) ? scr.GetString() : null,
                            SeatNumbers  = el.TryGetProperty("SeatNumbers", out var sn) ? sn.GetString() : null,
                        });
                    }
                }
            }
            catch { /* non-fatal — breakdown simply won't render */ }
        }

        var vm = new OrderConfirmationViewModel
        {
            OrderSubtotal    = TempData["OrderSubtotal"]  as string,
            OrderTax         = TempData["OrderTax"]       as string,
            OrderTotal       = TempData["OrderTotal"]     as string,
            IssuedCodes      = codes,
            QrCodes          = qrCodes,
            SeatLabels       = seatLabels,
            ScreenNames      = screenNames,
            OrderItems       = orderItems,
            ConcessionQrCode = TempData["ConcessionQrCode"] as string
        };

        return View(vm);
    }


    // ── Private helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Generates a unique 6-digit long TicketCode, retrying until one is not already in use.
    /// Canonical schema requires TicketCode BIGINT NOT NULL on the Ticket table.
    /// </summary>
    private async Task<long> GenerateUniqueTicketCodeAsync()
    {
        long code;
        bool exists;
        do
        {
            // 6-digit range: 100000–999999
            code = _rng.NextInt64(100_000L, 1_000_000L);
            exists = await _db.Tickets.AnyAsync(t => t.TicketCode == code);
        }
        while (exists);

        return code;
    }
}
