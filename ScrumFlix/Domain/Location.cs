/*
 * File: /ScrumFlix/Domain/Location.cs
 * Description: Canonical Location entity — maps to the Location table in defaultdb.
 */

namespace ScrumFlix.Domain;

/// <summary>
/// A physical ScrumFlix theater location.
/// Maps to: Location (LocationId, LocationName, LocationAddress, IsActive)
/// </summary>
[Table("Location")]
public class Location
{
    /// <summary>Primary key — auto-increment.</summary>
    [Key]
    [Column("LocationId")]
    public int LocationId { get; set; }

    /// <summary>Display name of the theater (e.g., "North Theater"). Must be unique.</summary>
    [Required]
    [MaxLength(100)]
    [Column("LocationName")]
    [Display(Name = "Location Name")]
    public string LocationName { get; set; } = string.Empty;

    /// <summary>Street address of the theater.</summary>
    [MaxLength(255)]
    [Column("LocationAddress")]
    [Display(Name = "Address")]
    public string? LocationAddress { get; set; }

    /// <summary>Whether this location is currently operational.</summary>
    [Column("IsActive")]
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Windows timezone ID for this theater location (e.g. "Central Standard Time").
    /// Used to convert UTC showtimes to local display time and to label QR code
    /// timestamps with the correct offset abbreviation (CDT, CST, PDT, etc.).
    /// On Linux, .NET automatically maps Windows IDs to IANA equivalents
    /// (e.g. "Central Standard Time" → "America/Chicago") — no tzdata required.
    /// Defaults to Central Time — the ScrumFlix home region.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("TimeZoneId", TypeName = "varchar(100)")]
    [Display(Name = "Time Zone")]
    public string TimeZoneId { get; set; } = "Central Standard Time";

    // ── Navigation properties ──────────────────────────────────────────────

    /// <summary>Screening rooms inside this location.</summary>
    public ICollection<TheaterScreen> TheaterScreens { get; set; } = new List<TheaterScreen>();

    /// <summary>Employees assigned to this location.</summary>
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();

    /// <summary>Shifts scheduled at this location.</summary>
    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();

    /// <summary>Concession items stocked at this location.</summary>
    public ICollection<ConcessionItem> ConcessionItems { get; set; } = new List<ConcessionItem>();

    /// <summary>Concession sales recorded at this location.</summary>
    public ICollection<ConcessionSale> ConcessionSales { get; set; } = new List<ConcessionSale>();
}
