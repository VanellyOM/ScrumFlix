/*
 * File:        /ScrumFlix/Domain/Seat.cs
 * Namespace:   ScrumFlix.Domain
 * Purpose:     Canonical Seat entity — maps to the Seat table in defaultdb.
 *
 *              Represents a single physical seat within a theater screen.
 *              Added to schema in the Seat Selection Migration (2026-05-05).
 *
 *              LAYOUT FIELDS:
 *                RowLabel    — display label (A, B, C …)
 *                SeatNumber  — position within the row (1, 2, 3 …)
 *                RowNumber   — numeric row index for grid rendering (1-based, NOT NULL)
 *                ColumnNumber— numeric column index for grid rendering (1-based, NOT NULL)
 *
 *              SeatType defaults to 'Standard'. Future values: 'Premium', 'Accessible'.
 *
 *              Seeded by SampleDataSeederFull Block 4-S:
 *                Capacity 50  → rows A–E,   seats 1–10
 *                Capacity 60  → rows A–F,   seats 1–10
 *                Capacity 70  → rows A–G,   seats 1–10
 *
 * Phase:   2 (Patch)
 * Author:  ScrumFlix Rebuild Team
 * Updated: 2026-05-05
 */

namespace ScrumFlix.Domain;

/// <summary>
/// A physical seat inside a theater screen.
/// Maps to: Seat (SeatId, TheaterScreenId, RowLabel, SeatNumber, SeatType, IsActive,
///          ColumnNumber, RowNumber)
/// </summary>
[Table("Seat")]
public class Seat
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("SeatId")]
    public int SeatId { get; set; }

    /// <summary>The theater screen this seat belongs to.</summary>
    [Column("TheaterScreenId")]
    public int TheaterScreenId { get; set; }

    /// <summary>
    /// Alphabetic row label displayed on the ticket and seat map (e.g., "A", "B", "C").
    /// </summary>
    [Required]
    [MaxLength(10)]
    [Column("RowLabel")]
    [Display(Name = "Row")]
    public string RowLabel { get; set; } = string.Empty;

    /// <summary>
    /// Numeric seat position within the row (1-based).
    /// Displayed on the ticket as row/seat (e.g., "B7").
    /// </summary>
    [Column("SeatNumber")]
    [Display(Name = "Seat")]
    [Range(1, int.MaxValue)]
    public int SeatNumber { get; set; }

    /// <summary>
    /// Seat category. Default is 'Standard'.
    /// Future values: 'Premium', 'Accessible', 'VIP'.
    /// </summary>
    [MaxLength(50)]
    [Column("SeatType")]
    [Display(Name = "Seat Type")]
    public string SeatType { get; set; } = "Standard";

    /// <summary>Whether this seat is available for booking. False = permanently removed.</summary>
    [Column("IsActive")]
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Numeric column index for grid rendering (1-based).
    /// Used to position the seat in a visual seat-map grid.
    /// </summary>
    [Column("ColumnNumber")]
    [Display(Name = "Column")]
    public int ColumnNumber { get; set; }

    /// <summary>
    /// Numeric row index for grid rendering (1-based).
    /// Used to position the seat in a visual seat-map grid.
    /// </summary>
    [Column("RowNumber")]
    [Display(Name = "Row Number")]
    public int RowNumber { get; set; }

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>The theater screen this seat is located in.</summary>
    [ForeignKey(nameof(TheaterScreenId))]
    public TheaterScreen? TheaterScreen { get; set; }

    /// <summary>Per-showtime availability records for this seat.</summary>
    public ICollection<ShowtimeSeat> ShowtimeSeats { get; set; } = new List<ShowtimeSeat>();

    // ── Computed helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Display label combining row and seat number (e.g., "B7").
    /// Used on tickets and in the seat-selection UI.
    /// </summary>
    [NotMapped]
    public string DisplayLabel => $"{RowLabel}{SeatNumber}";
}
