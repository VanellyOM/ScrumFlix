/*
 * File:      /ScrumFlix/Areas/Admin/ViewModels/AdminManageViewModels.cs
 * Namespace: ScrumFlix.Areas.Admin.ViewModels
 * Purpose:   ViewModels for all admin management screens:
 *              - Showtime list, create, edit
 *              - Concession item list, create, edit, restock
 *              - User list, create, edit
 *              - Location list, create, edit
 *              - TheaterScreen list, create, edit
 *              - CSV export parameters
 */


namespace ScrumFlix.Areas.Admin.ViewModels;

// ── Showtime ──────────────────────────────────────────────────────────────────

public class ShowtimeIndexViewModel
{
    public List<ShowtimeRowViewModel> Showtimes { get; set; } = new();
    public List<Movie>  Movies    { get; set; } = new();
    public int? FilterMovieId     { get; set; }
    public bool ShowInactive      { get; set; }
    // Pagination + sort
    public int  Page       { get; set; } = 1;
    public int  PageSize   { get; set; } = 20;
    public int  TotalCount { get; set; }
    public int  TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public string? SortBy  { get; set; }
    public bool SortDesc   { get; set; }
}

public class ShowtimeRowViewModel
{
    public int      ShowtimeId       { get; set; }
    public string   MovieTitle       { get; set; } = string.Empty;
    public string   ScreenName       { get; set; } = string.Empty;
    public string   LocationName     { get; set; } = string.Empty;
    public DateTime StartTime        { get; set; }
    public int      Capacity         { get; set; }
    public int      TicketsSold      { get; set; }
    public decimal  PricePerTicket   { get; set; }
    public bool     IsActive         { get; set; }
    public int      AvailableSeats   => Capacity - TicketsSold;
    public string   StartTimeDisplay => StartTime.ToString("ddd MMM d · h:mm tt");
}

public class ShowtimeFormViewModel
{
    public int ShowtimeId { get; set; }

    [Required, Display(Name = "Movie")]
    public int MovieId { get; set; }

    [Required, Display(Name = "Theater Screen")]
    public int TheaterScreenId { get; set; }

    [Required, Display(Name = "Start Time")]
    public DateTime StartTime { get; set; } = DateTime.Today.AddDays(1).AddHours(19);

    [Required, Range(1, 1000), Display(Name = "Capacity")]
    public int Capacity { get; set; } = 50;

    [Required, Range(0, 999.99), DataType(DataType.Currency), Display(Name = "Price Per Ticket")]
    public decimal PricePerTicket { get; set; } = 12.00m;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    // For dropdowns
    public List<Movie>        Movies        { get; set; } = new();
    public List<TheaterScreen> TheaterScreens { get; set; } = new();
}

// ── Concession Items ──────────────────────────────────────────────────────────

public class ConcessionIndexViewModel
{
    public List<ConcessionItem> Items     { get; set; } = new();
    public List<Location>       Locations { get; set; } = new();
    public int? FilterLocationId          { get; set; }
    public bool ShowInactive              { get; set; }
    public int  LowStockCount            => Items.Count(i => i.IsLowStock && i.IsActive);
}

public class ConcessionFormViewModel
{
    public int ConcessionItemId { get; set; }

    [Required, MaxLength(100), Display(Name = "Item Name")]
    public string ItemName { get; set; } = string.Empty;

    [Required, Range(0.01, 999.99), DataType(DataType.Currency), Display(Name = "Price")]
    public decimal Price { get; set; }

    [Required, Range(0, 10000), Display(Name = "Quantity In Stock")]
    public int QuantityInStock { get; set; }

    [Required, Range(0, 1000), Display(Name = "Minimum Stock Level")]
    public int Minimum { get; set; } = 5;

    [Required, Display(Name = "Location")]
    public int LocationId { get; set; } = 1;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public List<Location> Locations { get; set; } = new();
}

public class RestockViewModel
{
    public int    ConcessionItemId  { get; set; }
    public string ItemName          { get; set; } = string.Empty;
    public int    CurrentStock      { get; set; }
    public int    Minimum           { get; set; }

    [Required, Range(1, 10000), Display(Name = "Units to Add")]
    public int AddQuantity { get; set; } = 10;
}

// ── Users ─────────────────────────────────────────────────────────────────────

public class UserIndexViewModel
{
    public List<User>  Users { get; set; } = new();
    public List<Role>  Roles { get; set; } = new();
    // Filter + pagination
    public string? SearchTerm { get; set; }
    public int? FilterRoleId  { get; set; }
    public int  Page          { get; set; } = 1;
    public int  PageSize      { get; set; } = 20;
    public int  TotalCount    { get; set; }
    public int  TotalPages    => (int)Math.Ceiling((double)TotalCount / PageSize);
    public string? SortBy     { get; set; }
    public bool SortDesc      { get; set; }
}

public class UserFormViewModel
{
    public int UserId { get; set; }

    [Required, MaxLength(50), Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;

    [Required, Display(Name = "Role")]
    public int RoleId { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Must Change Password on Next Login")]
    public bool MustChangePassword { get; set; } = true;

    /// <summary>Only used on create — hashed before storage. Null on edit.</summary>
    [MinLength(6), Display(Name = "Password")]
    public string? Password { get; set; }

    public List<Role> Roles { get; set; } = new();

    /// <summary>True when editing an existing user (UserId > 0).</summary>
    public bool IsEdit => UserId > 0;
}

// ── Locations ─────────────────────────────────────────────────────────────────

public class LocationIndexViewModel
{
    public List<Location> Locations { get; set; } = new();
}

public class LocationFormViewModel
{
    public int LocationId { get; set; }

    [Required, MaxLength(100), Display(Name = "Location Name")]
    public string LocationName { get; set; } = string.Empty;

    [MaxLength(200), Display(Name = "Address")]
    public string? LocationAddress { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

// ── Theater Screens ───────────────────────────────────────────────────────────

public class ScreenIndexViewModel
{
    public List<TheaterScreen> Screens   { get; set; } = new();
    public List<Location>      Locations { get; set; } = new();
    public int? FilterLocationId         { get; set; }
}

public class ScreenFormViewModel
{
    public int TheaterScreenId { get; set; }

    [Required, Display(Name = "Location")]
    public int LocationId { get; set; }

    [Required, MaxLength(100), Display(Name = "Screen Name")]
    public string ScreenName { get; set; } = string.Empty;

    [Required, Range(1, 1000), Display(Name = "Capacity")]
    public int Capacity { get; set; } = 50;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public List<Location> Locations { get; set; } = new();
}

// ── CSV Exports ───────────────────────────────────────────────────────────────

public enum ExportFormat { Csv, Excel }

public class ExportViewModel
{
    [Required, DataType(DataType.Date), Display(Name = "From")]
    public DateOnly DateFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));

    [Required, DataType(DataType.Date), Display(Name = "To")]
    public DateOnly DateTo { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Format")]
    public ExportFormat Format { get; set; } = ExportFormat.Csv;
}

// ── Staff Portal Test Page ─────────────────────────────────────────────────────

/// <summary>
/// ViewModel for the Staff Portal Test Page (Areas/Admin/Views/AdminManage/StaffPortalTest.cshtml).
/// Surfaces the same data shown in the customer-facing Movies and Concessions tabs,
/// but routed entirely through Admin/Manager actions and using the staff session identity.
/// </summary>
public class StaffPortalTestViewModel
{
    // ── Identity (from staff session, NOT web.sales) ──────────────────────
    public string StaffUserName   { get; set; } = string.Empty;
    public string StaffRole       { get; set; } = string.Empty;
    public int    StaffUserId     { get; set; }

    // ── Movies panel ──────────────────────────────────────────────────────
    /// <summary>All movies with TMDB metadata loaded (same data as customer catalog).</summary>
    public List<Movie>          Movies          { get; set; } = new();

    // ── Concessions panel ─────────────────────────────────────────────────
    /// <summary>All active concession items (same data as customer catalog).</summary>
    public List<ConcessionItem> ConcessionItems { get; set; } = new();

    // ── Computed ──────────────────────────────────────────────────────────
    public int  LowStockCount    => ConcessionItems.Count(ci => ci.IsActive && ci.IsLowStock);
    public bool HasLowStockItems => LowStockCount > 0;
}
