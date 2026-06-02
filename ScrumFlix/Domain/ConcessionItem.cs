/*
 * File: /ScrumFlix/Domain/ConcessionItem.cs
 * Description: Canonical ConcessionItem entity — maps to the ConcessionItem table in defaultdb.
 *
 *              REPLACES the legacy Inventory model, which targeted the phantom "inventory" table
 *              and included vendor FKs, unit cost, and location-specific pricing — none of which
 *              exist in the canonical schema.
 *
 *              KEY DIFFERENCES from legacy Inventory:
 *              - Single Price field (not location-specific via ConcessionsPricing).
 *              - Minimum is the low-stock alert threshold (default 5).
 *              - QuantityInStock is decremented atomically on each ConcessionSale.
 *              - No vendor relationship in canonical schema.
 *
 *              Seeded items:
 *                1 = Popcorn   $8.00  qty:30  min:5
 *                2 = Candy     $3.00  qty:40  min:10
 *                3 = Drink     $4.00  qty:20  min:5
 */

namespace ScrumFlix.Domain;

/// <summary>
/// A concession product available for purchase at ScrumFlix theaters.
/// Maps to: ConcessionItem (ConcessionItemId, ItemName, Price, QuantityInStock, Minimum, IsActive)
/// </summary>
[Table("ConcessionItem")]
public class ConcessionItem
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("ConcessionItemId")]
    public int ConcessionItemId { get; set; }

    /// <summary>Display name of the item (e.g., "Popcorn"). Must be unique.</summary>
    [Required]
    [MaxLength(100)]
    [Column("ItemName")]
    [Display(Name = "Item Name")]
    public string ItemName { get; set; } = string.Empty;

    /// <summary>Retail price of this item. Single price across all locations.</summary>
    [Column("Price")]
    [DataType(DataType.Currency)]
    [Display(Name = "Price")]
    [Range(0.01, 9999.99)]
    public decimal Price { get; set; }

    /// <summary>
    /// Current inventory count. Decremented atomically by ConcessionService on each sale.
    /// ConcessionService must reject sales that would drop this below zero.
    /// </summary>
    [Column("QuantityInStock")]
    [Display(Name = "Quantity In Stock")]
    [Range(0, int.MaxValue)]
    public int QuantityInStock { get; set; } = 0;

    /// <summary>
    /// Low-stock alert threshold. When QuantityInStock drops to or below this value
    /// after a sale, admin dashboard must surface a replenishment alert.
    /// Default is 5 per canonical schema.
    /// </summary>
    [Column("Minimum")]
    [Display(Name = "Minimum Stock")]
    [Range(0, int.MaxValue)]
    public int Minimum { get; set; } = 5;

    /// <summary>Whether this item is currently available for sale.</summary>
    [Column("IsActive")]
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The location this concession item is stocked at.
    /// Matches the live schema: LocationId INT NOT NULL FK → Location.LocationId.
    /// Defaults to LocationId = 1 per the canonical seed data.
    /// </summary>
    [Column("LocationId")]
    [Display(Name = "Location")]
    public int LocationId { get; set; } = 1;

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>The location where this item is stocked.</summary>
    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }

    /// <summary>Sale line items referencing this concession item.</summary>
    public ICollection<ConcessionSaleItem> ConcessionSaleItems { get; set; } = new List<ConcessionSaleItem>();

    // ── Computed helpers ───────────────────────────────────────────────────

    /// <summary>True when current stock is at or below the minimum threshold.</summary>
    [NotMapped]
    public bool IsLowStock => QuantityInStock <= Minimum;
}
