/*
 * File: /ScrumFlix/Data/CartService.cs
 * Description: Service for managing the session-based shopping cart, supporting both ticket
 *              and concession items with quantity management and tax calculation.
 *
 * Sprint S3 — F-03 Fix:
 *   The XML doc comment on GetConcessionLocationId() contained a stale remark
 *   saying "If that property does not exist yet on CartItem, add it as a nullable int".
 *   CartItem.LocationId (int?) was added in Phase 3 (F-02 fix), so this remark is
 *   incorrect and misleading. Removed the stale conditional instruction and updated
 *   the doc comment to accurately reflect the current CartItem schema.
 *   No logic changes — behaviour is identical.
 *
 * Seat picker fix:
 *   AddItem() previously merged ticket items that shared a ShowtimeId by incrementing
 *   Quantity on the existing cart item. This silently dropped SeatNumbers and
 *   ShowtimeSeatIds from the incoming item, meaning seat selections were lost whenever
 *   a ticket for the same showtime was already in the cart (e.g. conflict-replay flow).
 *
 *   Fix: ticket items that carry seat selections (SeatNumbers is not null/empty) are
 *   NEVER merged — they are always added as a new line item. This preserves the exact
 *   seat labels and resolved IDs on each cart item so CartController.Checkout can
 *   correctly assign ShowtimeSeatId per Ticket row and finalize the reserved seats.
 *
 *   General-admission ticket items (SeatNumbers null/empty) retain the original merge
 *   behaviour — adding the same GA showtime twice just increments the quantity.
 *   Concession items are unaffected.
 */

using System.Text.Json;

namespace ScrumFlix.Data;

/// <summary>
/// Manages a user's shopping cart stored in the ASP.NET Core HTTP session.
/// Supports adding/removing tickets and concessions and calculates location-based sales tax.
/// </summary>
public class CartService
{
    private const string CartKey = "ScrumFlix_Cart";

    // Texas base sales tax rate applied to all transactions
    private const decimal BaseTaxRate = 0.0825m;

    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new CartService with the HTTP context accessor for session access.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor for the current HTTP context and session.</param>
    public CartService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // ── Session Helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves the current cart from the session.
    /// </summary>
    /// <returns>A list of CartItems currently in the session cart.</returns>
    public List<CartItem> GetCart()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        var json = session?.GetString(CartKey);
        return string.IsNullOrEmpty(json)
            ? new List<CartItem>()
            : JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
    }

    /// <summary>
    /// Saves the updated cart back to the session.
    /// </summary>
    /// <param name="cart">The cart list to persist.</param>
    private void SaveCart(List<CartItem> cart)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        session?.SetString(CartKey, JsonSerializer.Serialize(cart));
    }

    // ── Cart Operations ────────────────────────────────────────────────────────

    /// <summary>
    /// Adds a new item to the cart, merging with an existing line item where appropriate.
    ///
    /// Merge rules:
    ///   - Ticket with seat selections (SeatNumbers not null/empty): NEVER merged.
    ///     Each seat-picker cart item is a distinct line with its own SeatNumbers and
    ///     ShowtimeSeatIds. Merging would silently drop the seat data.
    ///   - Ticket without seat selections (general admission): merged by ShowtimeId —
    ///     adding the same GA showtime twice increments quantity on the existing item.
    ///   - Concession: merged by ConcessionItemId — same behaviour as before.
    /// </summary>
    /// <param name="item">The CartItem to add.</param>
    public void AddItem(CartItem item)
    {
        var cart = GetCart();

        CartItem? existing = null;

        if (item.ItemType == CartItemType.Ticket)
        {
            // Seat-picker items are never merged — each carries specific seat data
            // that must be preserved intact for checkout to assign ShowtimeSeatId
            // correctly on each Ticket row and finalize the reserved seats.
            bool hasSeatSelection = !string.IsNullOrWhiteSpace(item.SeatNumbers);

            if (!hasSeatSelection)
            {
                // General-admission: merge by ShowtimeId as before
                existing = cart.FirstOrDefault(c =>
                    c.ItemType == CartItemType.Ticket
                    && c.ShowtimeId == item.ShowtimeId
                    && string.IsNullOrWhiteSpace(c.SeatNumbers));
            }
            // else: hasSeatSelection == true → existing stays null → always add new line
        }
        else
        {
            // Concession: merge by ConcessionItemId
            existing = cart.FirstOrDefault(c =>
                c.ItemType == CartItemType.Concession
                && c.ConcessionItemId == item.ConcessionItemId);
        }

        if (existing != null)
            existing.Quantity += item.Quantity;
        else
            cart.Add(item);

        SaveCart(cart);
    }

    /// <summary>
    /// Removes a specific line item from the cart by its CartItemId.
    /// </summary>
    /// <param name="cartItemId">The unique identifier of the cart line item to remove.</param>
    public void RemoveItem(string cartItemId)
    {
        var cart = GetCart();
        cart.RemoveAll(c => c.CartItemId == cartItemId);
        SaveCart(cart);
    }

    /// <summary>
    /// Updates the quantity of a specific cart line item. Removes the item if quantity reaches zero.
    /// </summary>
    /// <param name="cartItemId">The cart item to update.</param>
    /// <param name="quantity">The new quantity value.</param>
    public void UpdateQuantity(string cartItemId, int quantity)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(c => c.CartItemId == cartItemId);
        if (item == null) return;

        if (quantity <= 0)
            cart.Remove(item);
        else
            item.Quantity = quantity;

        SaveCart(cart);
    }

    /// <summary>
    /// Clears all items from the cart.
    /// </summary>
    public void ClearCart()
    {
        _httpContextAccessor.HttpContext?.Session.Remove(CartKey);
    }

    // ── Totals & Tax ───────────────────────────────────────────────────────────

    /// <summary>
    /// Calculates the subtotal (pre-tax) of all items in the cart.
    /// </summary>
    /// <returns>Decimal subtotal amount.</returns>
    public decimal GetSubtotal()
    {
        return GetCart().Sum(c => c.LineTotal);
    }

    /// <summary>
    /// Calculates sales tax based on the Texas base rate.
    /// </summary>
    /// <returns>Decimal tax amount.</returns>
    public decimal GetTax()
    {
        return Math.Round(GetSubtotal() * BaseTaxRate, 2);
    }

    /// <summary>
    /// Calculates the grand total including sales tax.
    /// </summary>
    /// <returns>Decimal total amount with tax applied.</returns>
    public decimal GetTotal()
    {
        return GetSubtotal() + GetTax();
    }

    /// <summary>
    /// Returns the total number of individual items (sum of quantities) in the cart.
    /// </summary>
    /// <returns>Integer count of items.</returns>
    public int GetItemCount()
    {
        return GetCart().Sum(c => c.Quantity);
    }

    /// <summary>
    /// Returns the <see cref="CartItem.LocationId"/> of the first concession item in the cart,
    /// or <see langword="null"/> if there are no concession items.
    /// Used by <c>ShowtimesController</c> to detect a cross-location conflict before adding
    /// a ticket at a different theater.
    /// </summary>
    /// <remarks>
    /// <c>CartItem.LocationId</c> (int?, nullable) is populated by
    /// <c>ConcessionsController.AddConcessionToCart</c> from
    /// <c>ConcessionItem.LocationId</c> when the item is added to the cart.
    /// </remarks>
    public int? GetConcessionLocationId()
    {
        return GetCart()
            .Where(c => c.ItemType == CartItemType.Concession && c.LocationId.HasValue)
            .Select(c => c.LocationId)
            .FirstOrDefault();
    }
}
