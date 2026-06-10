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
    public List<Movie>    Movies    { get; set; } = new();
    public List<Location> Locations { get; set; } = new();
    // Filters
    public int?  FilterMovieId      { get; set; }
    public int?  FilterLocationId   { get; set; }
    /// <summary>null = all, true = active only, false = inactive only</summary>
    public bool? FilterStatusActive { get; set; }
    public bool ShowInactive        { get; set; }
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

    /// <summary>Only used on create — hashed before storage. Null on edit means keep existing.</summary>
    [MinLength(6), Display(Name = "Password")]
    public string? Password { get; set; }

    /// <summary>
    /// Confirmation field — must match <see cref="Password"/> when a new password is being set.
    /// Validated in the controller (not via [Compare]) because Password is nullable and
    /// [Compare] does not support conditional matching on nullable source properties.
    /// </summary>
    [Display(Name = "Confirm Password")]
    public string? ConfirmPassword { get; set; }

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

    /// <summary>
    /// Selected Windows timezone ID (e.g. "Central Standard Time").
    /// Bound to the timezone dropdown. Required — every location must have a timezone
    /// so UTC showtime conversion and QR code timestamps are always correct.
    /// </summary>
    [Required, MaxLength(100), Display(Name = "Time Zone")]
    public string TimeZoneId { get; set; } = "Central Standard Time";

    /// <summary>
    /// US timezone options for the dropdown, ordered west-to-east.
    /// Populated by the controller GET action; not posted back (re-built on validation failure).
    /// Each entry uses the Windows timezone ID as Value and a friendly label as Text.
    /// </summary>
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> AvailableTimeZones { get; set; } = new();
}

// ── Movies ────────────────────────────────────────────────────────────────────

/// <summary>
/// ViewModel for the Admin MovieCreate and MovieEdit forms.
/// Separates the form data from the <see cref="Movie"/> domain entity so that
/// genre assignments (many-to-many via <c>MovieGenres</c>) can be handled
/// via a multiselect dropdown rather than a plain text field.
/// </summary>
public class MovieFormViewModel
{
    public int MovieId { get; set; }

    [Required, MaxLength(200), Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Legacy single-genre column (Movies.Genre VARCHAR 30).
    /// Still written on save for backward compatibility with any code that
    /// reads Movie.Genre directly. Set to the primary genre's name on save.
    /// Not bound from the form — populated by the controller from PrimaryGenreId.
    /// </summary>
    [MaxLength(30)]
    public string Genre { get; set; } = string.Empty;

    [Required, MaxLength(20), Display(Name = "Rating")]
    public string Rating { get; set; } = string.Empty;

    [Required, Range(1, 9999), Display(Name = "Runtime (min)")]
    public short RuntimeMinutes { get; set; }

    [Required, MaxLength(1000), Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The GenreId designated as the primary genre for this movie.
    /// Must be one of the IDs also present in <see cref="SelectedGenreIds"/>.
    /// Required — every movie must have exactly one primary genre.
    /// </summary>
    [Required, Display(Name = "Primary Genre")]
    public int PrimaryGenreId { get; set; }

    /// <summary>
    /// All GenreIds selected for this movie (including the primary one).
    /// Maps to <c>MovieGenres</c> rows on save.
    /// Rendered as a multiselect <c>&lt;select multiple&gt;</c> in the view.
    /// </summary>
    [Display(Name = "Genres")]
    public List<int> SelectedGenreIds { get; set; } = new();

    /// <summary>
    /// All active genres available for selection, ordered alphabetically.
    /// Populated by the controller GET action; not posted back.
    /// </summary>
    public List<Genre> AvailableGenres { get; set; } = new();

    /// <summary>True when editing an existing movie (MovieId > 0).</summary>
    public bool IsEdit => MovieId > 0;
}

// ── Theater Screens ───────────────────────────────────────────────────────────

public class ScreenIndexViewModel
{
    public List<TheaterScreen> Screens   { get; set; } = new();
    public List<Location>      Locations { get; set; } = new();
    public int? FilterLocationId         { get; set; }
    // Pagination + sort
    public int  Page       { get; set; } = 1;
    public int  PageSize   { get; set; } = 25;
    public int  TotalCount { get; set; }
    public int  TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public string? SortBy  { get; set; }
    public bool SortDesc   { get; set; }
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
