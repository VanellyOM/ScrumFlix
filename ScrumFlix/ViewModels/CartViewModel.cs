/*
 * File: /ScrumFlix/ViewModels/CartViewModel.cs
 * Description: ViewModel for the cart page summarizing all items, pricing, tax, and checkout totals.
 */

namespace ScrumFlix.ViewModels;

/// <summary>
/// ViewModel for the shopping cart page with items, subtotal, tax, and total calculations.
/// </summary>
public class CartViewModel
{
    /// <summary>Gets or sets all items currently in the cart.</summary>
    public List<CartItem> Items { get; set; } = new();

    /// <summary>Gets or sets the pre-tax subtotal of all items.</summary>
    public decimal Subtotal { get; set; }

    /// <summary>Gets or sets the calculated sales tax amount.</summary>
    public decimal Tax { get; set; }

    /// <summary>Gets or sets the grand total including tax.</summary>
    public decimal Total { get; set; }

    /// <summary>Gets or sets the Texas sales tax rate displayed to the customer.</summary>
    public decimal TaxRate { get; set; } = 0.0825m;

    /// <summary>
    /// Optional customer email address for sending a purchase confirmation receipt.
    /// Collected on the CartReview page and passed through checkout for one-time use.
    /// NOT persisted after the receipt is sent, and completely separate from
    /// any Web Sales System / staff account email address.
    /// </summary>
    public string? CustomerReceiptEmail { get; set; }

    /// <summary>Returns true if the cart is empty.</summary>
    public bool IsEmpty => !Items.Any();
}
