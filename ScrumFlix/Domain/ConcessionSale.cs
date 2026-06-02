/*
 * File: /ScrumFlix/Domain/ConcessionSale.cs
 * Description: Canonical ConcessionSale entity — maps to the ConcessionSale table in defaultdb.
 *
 *              REPLACES the legacy ConcessionsSale model, which used phantom columns
 *              (location_id, employee_id as separate concepts) and targeted the wrong table.
 *
 *              KEY DIFFERENCES from legacy ConcessionsSale:
 *              - UserId FK references Users.UserId (the logged-in staff member processing the sale).
 *              - CustomerEmail (varchar 100) captures the guest/customer email at point of sale.
 *              - Total is the canonical summary amount; line items are in ConcessionSaleItem.
 *              - No LocationId on the sale — location is implicit via the logged-in user's employee.
 *
 *              ConcessionService must create ConcessionSale + all ConcessionSaleItems
 *              in a single database transaction and decrement QuantityInStock atomically.
 */

namespace ScrumFlix.Domain;

/// <summary>
/// A completed concession purchase transaction.
/// Maps to: ConcessionSale (ConcessionSaleId, UserId, CustomerEmail, TimeOfSale, Total)
/// </summary>
[Table("ConcessionSale")]
public class ConcessionSale
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("ConcessionSaleId")]
    public int ConcessionSaleId { get; set; }

    /// <summary>The Users.UserId of the staff member who processed this sale.</summary>
    [Column("UserId")]
    public int UserId { get; set; }

    /// <summary>
    /// The customer's email address captured at the point of sale.
    /// Used for receipt delivery. Not linked to any customer table (no customer table in schema).
    /// </summary>
    [Required]
    [MaxLength(100)]
    [EmailAddress]
    [Column("CustomerEmail")]
    [Display(Name = "Customer Email")]
    public string CustomerEmail { get; set; } = string.Empty;

    /// <summary>Date and time the sale was completed.</summary>
    [Column("TimeOfSale")]
    [Display(Name = "Time of Sale")]
    public DateTime TimeOfSale { get; set; }

    /// <summary>Grand total of all line items in this sale.</summary>
    [Column("Total")]
    [DataType(DataType.Currency)]
    [Display(Name = "Total")]
    public decimal Total { get; set; }

    /// <summary>
    /// The location where this concession sale took place.
    /// Matches the live schema: LocationId INT NOT NULL FK → Location.LocationId.
    /// Defaults to LocationId = 1 per the canonical seed data.
    /// Set to the logged-in user's Employee.LocationId at point of sale.
    /// </summary>
    [Column("LocationId")]
    [Display(Name = "Location")]
    public int LocationId { get; set; } = 1;

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>The staff user who processed this sale.</summary>
    [ForeignKey(nameof(UserId))]
    public User? ProcessedByUser { get; set; }

    /// <summary>The location where this sale occurred.</summary>
    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }

    /// <summary>Individual item lines that make up this sale.</summary>
    public ICollection<ConcessionSaleItem> ConcessionSaleItems { get; set; } = new List<ConcessionSaleItem>();
}
