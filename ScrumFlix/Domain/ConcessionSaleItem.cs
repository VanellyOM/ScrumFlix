/*
 * File: /ScrumFlix/Domain/ConcessionSaleItem.cs
 * Description: Canonical ConcessionSaleItem entity — maps to the ConcessionSaleItem table in defaultdb.
 *
 *              REPLACES the legacy ConcessionsSalesItem model, which used a binary item identifier
 *              and targeted the phantom concessions_sales_items table.
 *
 *              This is the line-item detail record for a ConcessionSale.
 *              UnitPrice captures the price at time of sale (in case ConcessionItem.Price changes later).
 *              LineTotal = Quantity × UnitPrice, stored for reporting accuracy.
 */

namespace ScrumFlix.Domain;

/// <summary>
/// A single line item within a concession sale transaction.
/// Maps to: ConcessionSaleItem (ConcessionSaleItemId, ConcessionSaleId, ConcessionItemId,
///          Quantity, UnitPrice, LineTotal)
/// </summary>
[Table("ConcessionSaleItem")]
public class ConcessionSaleItem
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("ConcessionSaleItemId")]
    public int ConcessionSaleItemId { get; set; }

    /// <summary>The parent sale this line item belongs to.</summary>
    [Column("ConcessionSaleId")]
    public int ConcessionSaleId { get; set; }

    /// <summary>The concession product sold on this line.</summary>
    [Column("ConcessionItemId")]
    public int ConcessionItemId { get; set; }

    /// <summary>Number of units purchased on this line.</summary>
    [Column("Quantity")]
    [Display(Name = "Qty")]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    /// <summary>
    /// Price per unit at the time of sale.
    /// Captured from ConcessionItem.Price at checkout so that future price changes
    /// do not retroactively alter historical sale records.
    /// </summary>
    [Column("UnitPrice")]
    [DataType(DataType.Currency)]
    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Total for this line: Quantity × UnitPrice.
    /// Stored explicitly for fast reporting — do not recompute at query time.
    /// </summary>
    [Column("LineTotal")]
    [DataType(DataType.Currency)]
    [Display(Name = "Line Total")]
    public decimal LineTotal { get; set; }

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>The parent concession sale transaction.</summary>
    [ForeignKey(nameof(ConcessionSaleId))]
    public ConcessionSale? ConcessionSale { get; set; }

    /// <summary>The concession item sold on this line.</summary>
    [ForeignKey(nameof(ConcessionItemId))]
    public ConcessionItem? ConcessionItem { get; set; }
}
