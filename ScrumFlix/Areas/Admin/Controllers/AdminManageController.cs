/*
 * File:      /ScrumFlix/Areas/Admin/Controllers/AdminManageController.cs
 * Namespace: ScrumFlix.Areas.Admin.Controllers
 * Purpose:   Admin CRUD controller covering all management operations:
 *              - Showtimes  (list, create, edit, toggle active)
 *              - Concession items (list, create, edit, restock)
 *              - Users      (list, create, edit, lock/unlock, reset password)
 *              - Locations  (list, create, edit)
 *              - TheaterScreens (list, create, edit)
 *              - CSV exports (tickets sold, concession sales)
 *
 * All actions require RoleId == 1 (Admin) via RoleGuard(1).
 * Inherits StaffControllerBase for RoleGuard, CurrentUserId, CurrentUserName helpers.
 *
 * Sprint: S6 — Admin Management + Reports
 */

using MiniExcelLibs;
using MiniExcelLibs.Attributes;

namespace ScrumFlix.Areas.Admin.Controllers;

[Area("Admin")]
public class AdminManageController : StaffControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<AdminManageController> _logger;

    public AdminManageController(
        AppDbContext db,
        ILogger<AdminManageController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SHOWTIMES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Lists all showtimes with optional movie filter, sort, and pagination.</summary>
    public async Task<IActionResult> Showtimes(int? movieId, int? locationId,
        string? status, bool showInactive = false,
        string? sortBy = null, bool sortDesc = false, int page = 1)
    {
        if (RoleGuard(1) is { } r) return r;

        const int pageSize = 25;
        var query = _db.Showtimes
            .Include(st => st.Movie)
            .Include(st => st.TheaterScreen).ThenInclude(ts => ts!.Location)
            .Include(st => st.Tickets)
            .AsNoTracking();

        if (movieId.HasValue)    query = query.Where(st => st.MovieId == movieId.Value);
        if (locationId.HasValue) query = query.Where(st => st.TheaterScreen!.LocationId == locationId.Value);

        // Status filter: "active" / "inactive" / null (all). showInactive is kept for
        // backward-compat but status takes precedence when supplied.
        bool? filterActive = status switch
        {
            "active"   => true,
            "inactive" => false,
            _          => null
        };
        if (filterActive.HasValue)
            query = query.Where(st => st.IsActive == filterActive.Value);
        else if (!showInactive)
            query = query.Where(st => st.IsActive);

        query = sortBy switch
        {
            "movie"    => sortDesc ? query.OrderByDescending(st => st.Movie!.Title)                            : query.OrderBy(st => st.Movie!.Title),
            "screen"   => sortDesc ? query.OrderByDescending(st => st.TheaterScreen!.ScreenName)               : query.OrderBy(st => st.TheaterScreen!.ScreenName),
            "location" => sortDesc ? query.OrderByDescending(st => st.TheaterScreen!.Location!.LocationName)   : query.OrderBy(st => st.TheaterScreen!.Location!.LocationName),
            "price"    => sortDesc ? query.OrderByDescending(st => st.PricePerTicket)                          : query.OrderBy(st => st.PricePerTicket),
            "capacity" => sortDesc ? query.OrderByDescending(st => st.Capacity)                                : query.OrderBy(st => st.Capacity),
            "status"   => sortDesc ? query.OrderByDescending(st => st.IsActive).ThenBy(st => st.StartTime)     : query.OrderBy(st => st.IsActive).ThenBy(st => st.StartTime),
            _          => sortDesc ? query.OrderByDescending(st => st.StartTime)                               : query.OrderBy(st => st.StartTime)
        };

        var total = await query.CountAsync();
        var showtimes = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var vm = new ShowtimeIndexViewModel
        {
            Showtimes = showtimes.Select(st =>
            {
                // Convert UTC StartTime to the location's local time for display.
                var tz         = st.TheaterScreen?.Location?.TimeZoneId is { } tzId
                    ? TryFindTimeZone(tzId)
                    : TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
                var startLocal = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(st.StartTime, DateTimeKind.Utc), tz);

                return new ShowtimeRowViewModel
                {
                    ShowtimeId     = st.ShowtimeId,
                    MovieTitle     = st.Movie?.Title ?? "—",
                    ScreenName     = st.TheaterScreen?.ScreenName ?? "—",
                    LocationName   = st.TheaterScreen?.Location?.LocationName ?? "—",
                    StartTime      = startLocal,
                    Capacity       = st.Capacity,
                    TicketsSold    = st.Tickets.Count,
                    PricePerTicket = st.PricePerTicket,
                    IsActive       = st.IsActive
                };
            }).ToList(),
            Movies            = await _db.Movies.OrderBy(m => m.Title).AsNoTracking().ToListAsync(),
            Locations         = await _db.Locations.Where(l => l.IsActive).OrderBy(l => l.LocationName).AsNoTracking().ToListAsync(),
            FilterMovieId     = movieId,
            FilterLocationId  = locationId,
            FilterStatusActive = filterActive,
            ShowInactive      = showInactive,
            Page              = page,
            PageSize          = pageSize,
            TotalCount        = total,
            SortBy            = sortBy,
            SortDesc          = sortDesc
        };

        return View(vm);
    }

    /// <summary>GET: render the create-showtime form.</summary>
    public async Task<IActionResult> ShowtimeCreate()
    {
        if (RoleGuard(1) is { } r) return r;
        return View(await BuildShowtimeFormAsync(new ShowtimeFormViewModel()));
    }

    /// <summary>POST: persist a new showtime.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ShowtimeCreate(ShowtimeFormViewModel vm)
    {
        if (RoleGuard(1) is { } r) return r;
        if (!ModelState.IsValid) return View(await BuildShowtimeFormAsync(vm));

        // Convert the admin-entered local StartTime to UTC using the screen's location timezone.
        // TheaterScreen.Location.TimeZoneId holds the Windows timezone ID (e.g. "Central Standard Time").
        // Storing UTC ensures QR codes, exports, and any future multi-timezone display are all correct.
        var screen    = await _db.TheaterScreens
            .Include(ts => ts.Location)
            .FirstOrDefaultAsync(ts => ts.TheaterScreenId == vm.TheaterScreenId);
        var tz        = screen?.Location?.TimeZoneId is { } tzId
            ? TryFindTimeZone(tzId)
            : TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        var startUtc  = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(vm.StartTime, DateTimeKind.Unspecified), tz);

        var showtime = new Showtime
        {
            MovieId         = vm.MovieId,
            TheaterScreenId = vm.TheaterScreenId,
            StartTime       = startUtc,
            Capacity        = vm.Capacity,
            PricePerTicket  = vm.PricePerTicket,
            IsActive        = vm.IsActive
        };

        _db.Showtimes.Add(showtime);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Admin {User} created ShowtimeId={Id} (StartTime UTC={UTC}).",
            CurrentUserName, showtime.ShowtimeId, startUtc);
        TempData["SuccessMessage"] = "Showtime created successfully.";
        return RedirectToAction(nameof(Showtimes));
    }

    /// <summary>GET: render the edit-showtime form.</summary>
    public async Task<IActionResult> ShowtimeEdit(int id)
    {
        if (RoleGuard(1) is { } r) return r;

        var st = await _db.Showtimes
            .Include(s => s.TheaterScreen).ThenInclude(ts => ts!.Location)
            .FirstOrDefaultAsync(s => s.ShowtimeId == id);
        if (st is null) return NotFound();

        // Convert stored UTC back to location local time so the datetime-local
        // input shows the time the admin originally entered.
        var tz        = st.TheaterScreen?.Location?.TimeZoneId is { } tzId
            ? TryFindTimeZone(tzId)
            : TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        var startLocal = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(st.StartTime, DateTimeKind.Utc), tz);

        var vm = new ShowtimeFormViewModel
        {
            ShowtimeId      = st.ShowtimeId,
            MovieId         = st.MovieId,
            TheaterScreenId = st.TheaterScreenId,
            StartTime       = startLocal,   // local time for the form input
            Capacity        = st.Capacity,
            PricePerTicket  = st.PricePerTicket,
            IsActive        = st.IsActive
        };

        return View(await BuildShowtimeFormAsync(vm));
    }

    /// <summary>POST: save showtime edits.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ShowtimeEdit(ShowtimeFormViewModel vm)
    {
        if (RoleGuard(1) is { } r) return r;
        if (!ModelState.IsValid) return View(await BuildShowtimeFormAsync(vm));

        var st = await _db.Showtimes.FindAsync(vm.ShowtimeId);
        if (st is null) return NotFound();

        // Resolve timezone from the (possibly changed) screen selection.
        var screen   = await _db.TheaterScreens
            .Include(ts => ts.Location)
            .FirstOrDefaultAsync(ts => ts.TheaterScreenId == vm.TheaterScreenId);
        var tz       = screen?.Location?.TimeZoneId is { } tzId
            ? TryFindTimeZone(tzId)
            : TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(vm.StartTime, DateTimeKind.Unspecified), tz);

        st.MovieId         = vm.MovieId;
        st.TheaterScreenId = vm.TheaterScreenId;
        st.StartTime       = startUtc;
        st.Capacity        = vm.Capacity;
        st.PricePerTicket  = vm.PricePerTicket;
        st.IsActive        = vm.IsActive;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Admin {User} updated ShowtimeId={Id} (StartTime UTC={UTC}).",
            CurrentUserName, st.ShowtimeId, startUtc);
        TempData["SuccessMessage"] = "Showtime updated.";
        return RedirectToAction(nameof(Showtimes));
    }

    /// <summary>POST: toggle IsActive on a showtime.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ShowtimeToggle(int id)
    {
        if (RoleGuard(1) is { } r) return r;

        var st = await _db.Showtimes.FindAsync(id);
        if (st is null) return NotFound();

        st.IsActive = !st.IsActive;
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Showtime {(st.IsActive ? "activated" : "deactivated")}.";
        return RedirectToAction(nameof(Showtimes));
    }

    /// <summary>POST: delete a showtime (only if no tickets sold).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ShowtimeDelete(int id)
    {
        if (RoleGuard(1) is { } r) return r;

        var ticketCount = await _db.Tickets.CountAsync(t => t.ShowtimeId == id);
        if (ticketCount > 0)
        {
            TempData["ErrorMessage"] = $"Cannot delete — {ticketCount} ticket(s) already sold. Deactivate instead.";
            return RedirectToAction(nameof(Showtimes));
        }

        var st = await _db.Showtimes.FindAsync(id);
        if (st is not null)
        {
            _db.Showtimes.Remove(st);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Admin {User} deleted ShowtimeId={Id}.", CurrentUserName, id);
        }

        TempData["SuccessMessage"] = "Showtime deleted.";
        return RedirectToAction(nameof(Showtimes));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CONCESSION ITEMS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Lists all concession items.</summary>
    public async Task<IActionResult> Concessions(int? locationId = null, bool showInactive = false)
    {
        if (RoleGuard(1) is { } r) return r;

        var query = _db.ConcessionItems
            .Include(ci => ci.Location)
            .AsNoTracking();

        if (locationId.HasValue)
            query = query.Where(ci => ci.LocationId == locationId.Value);
        if (!showInactive)
            query = query.Where(ci => ci.IsActive);

        var vm = new ConcessionIndexViewModel
        {
            Items = await query.OrderBy(ci => ci.ItemName).ToListAsync(),
            Locations = await _db.Locations.Where(l => l.IsActive).OrderBy(l => l.LocationName).AsNoTracking().ToListAsync(),
            FilterLocationId = locationId,
            ShowInactive = showInactive
        };

        return View(vm);
    }

    /// <summary>GET: render create-concession-item form.</summary>
    public async Task<IActionResult> ConcessionCreate()
    {
        if (RoleGuard(1) is { } r) return r;
        return View(await BuildConcessionFormAsync(new ConcessionFormViewModel()));
    }

    /// <summary>POST: persist a new concession item.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConcessionCreate(ConcessionFormViewModel vm)
    {
        if (RoleGuard(1) is { } r) return r;
        if (!ModelState.IsValid) return View(await BuildConcessionFormAsync(vm));

        var item = new ConcessionItem
        {
            ItemName = vm.ItemName,
            Price = vm.Price,
            QuantityInStock = vm.QuantityInStock,
            Minimum = vm.Minimum,
            LocationId = vm.LocationId,
            IsActive = vm.IsActive
        };

        _db.ConcessionItems.Add(item);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Admin {User} created ConcessionItemId={Id} '{Name}'.", CurrentUserName, item.ConcessionItemId, item.ItemName);
        TempData["SuccessMessage"] = $"'{item.ItemName}' added to the concession catalog.";
        return RedirectToAction(nameof(Concessions));
    }

    /// <summary>GET: render edit-concession-item form.</summary>
    public async Task<IActionResult> ConcessionEdit(int id)
    {
        if (RoleGuard(1) is { } r) return r;

        var item = await _db.ConcessionItems.FindAsync(id);
        if (item is null) return NotFound();

        var vm = new ConcessionFormViewModel
        {
            ConcessionItemId = item.ConcessionItemId,
            ItemName = item.ItemName,
            Price = item.Price,
            QuantityInStock = item.QuantityInStock,
            Minimum = item.Minimum,
            LocationId = item.LocationId,
            IsActive = item.IsActive
        };

        return View(await BuildConcessionFormAsync(vm));
    }

    /// <summary>POST: save concession item edits.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConcessionEdit(ConcessionFormViewModel vm)
    {
        if (RoleGuard(1) is { } r) return r;
        if (!ModelState.IsValid) return View(await BuildConcessionFormAsync(vm));

        var item = await _db.ConcessionItems.FindAsync(vm.ConcessionItemId);
        if (item is null) return NotFound();

        item.ItemName = vm.ItemName;
        item.Price = vm.Price;
        item.QuantityInStock = vm.QuantityInStock;
        item.Minimum = vm.Minimum;
        item.LocationId = vm.LocationId;
        item.IsActive = vm.IsActive;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Admin {User} updated ConcessionItemId={Id}.", CurrentUserName, item.ConcessionItemId);
        TempData["SuccessMessage"] = $"'{item.ItemName}' updated.";
        return RedirectToAction(nameof(Concessions));
    }

    /// <summary>GET: render restock form for a concession item.</summary>
    public async Task<IActionResult> ConcessionRestock(int id)
    {
        if (RoleGuard(1) is { } r) return r;

        var item = await _db.ConcessionItems.FindAsync(id);
        if (item is null) return NotFound();

        return View(new RestockViewModel
        {
            ConcessionItemId = item.ConcessionItemId,
            ItemName = item.ItemName,
            CurrentStock = item.QuantityInStock,
            Minimum = item.Minimum
        });
    }

    /// <summary>POST: add stock to a concession item.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConcessionRestock(RestockViewModel vm)
    {
        if (RoleGuard(1) is { } r) return r;
        if (!ModelState.IsValid) return View(vm);

        var item = await _db.ConcessionItems.FindAsync(vm.ConcessionItemId);
        if (item is null) return NotFound();

        item.QuantityInStock += vm.AddQuantity;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Admin {User} restocked ConcessionItemId={Id} +{Qty} (now {Total}).",
            CurrentUserName, item.ConcessionItemId, vm.AddQuantity, item.QuantityInStock);
        TempData["SuccessMessage"] = $"Added {vm.AddQuantity} unit(s) to '{item.ItemName}'. New stock: {item.QuantityInStock}.";
        return RedirectToAction(nameof(Concessions));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // USERS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Lists all users with their roles — searchable, sortable, paginated.</summary>
    public async Task<IActionResult> Users(string? search, int? roleId, string? sortBy, bool sortDesc = false, int page = 1)
    {
        if (RoleGuard(1) is { } r) return r;

        const int pageSize = 20;
        var query = _db.Users.Include(u => u.Role).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.UserName.Contains(search));
        if (roleId.HasValue)
            query = query.Where(u => u.RoleId == roleId.Value);

        // Sorting
        query = sortBy switch
        {
            "role" => sortDesc ? query.OrderByDescending(u => u.Role!.RoleName) : query.OrderBy(u => u.Role!.RoleName),
            "status" => sortDesc ? query.OrderByDescending(u => u.IsActive) : query.OrderBy(u => u.IsActive),
            _ => sortDesc ? query.OrderByDescending(u => u.UserName) : query.OrderBy(u => u.UserName)
        };

        var total = await query.CountAsync();
        var users = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var vm = new UserIndexViewModel
        {
            Users = users,
            Roles = await _db.Roles.OrderBy(r2 => r2.RoleId).AsNoTracking().ToListAsync(),
            SearchTerm = search,
            FilterRoleId = roleId,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            SortBy = sortBy,
            SortDesc = sortDesc
        };

        return View(vm);
    }

    /// <summary>GET: render create-user form.</summary>
    public async Task<IActionResult> UserCreate()
    {
        if (RoleGuard(1) is { } r) return r;
        return View(await BuildUserFormAsync(new UserFormViewModel()));
    }

    /// <summary>POST: create a new user.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UserCreate(UserFormViewModel vm)
    {
        if (RoleGuard(1) is { } r) return r;

        // Password required on create
        if (string.IsNullOrWhiteSpace(vm.Password))
            ModelState.AddModelError(nameof(vm.Password), "Password is required when creating a new user.");

        if (await _db.Users.AnyAsync(u => u.UserName == vm.UserName))
            ModelState.AddModelError(nameof(vm.UserName), "That username is already taken.");

        if (!ModelState.IsValid) return View(await BuildUserFormAsync(vm));

        var user = new User
        {
            UserName = vm.UserName,
            RoleId = vm.RoleId,
            IsActive = vm.IsActive,
            MustChangePassword = vm.MustChangePassword,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.Password!),
            EmployeeId = 0  // Admin-created users have no Employee record by default
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Admin {Admin} created UserId={Id} '{UserName}'.", CurrentUserName, user.UserId, user.UserName);
        TempData["SuccessMessage"] = $"User '{user.UserName}' created.";
        return RedirectToAction(nameof(Users));
    }

    /// <summary>GET: render edit-user form.</summary>
    public async Task<IActionResult> UserEdit(int id)
    {
        if (RoleGuard(1) is { } r) return r;

        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        var vm = new UserFormViewModel
        {
            UserId = user.UserId,
            UserName = user.UserName,
            RoleId = user.RoleId,
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword
        };

        return View(await BuildUserFormAsync(vm));
    }

    /// <summary>POST: save user edits (role, active flag, mustChange).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UserEdit(UserFormViewModel vm)
    {
        if (RoleGuard(1) is { } r) return r;

        // Password validation only when a new password is being set
        if (!string.IsNullOrWhiteSpace(vm.Password) && vm.Password.Length < 6)
            ModelState.AddModelError(nameof(vm.Password), "Password must be at least 6 characters.");

        if (!string.IsNullOrWhiteSpace(vm.Password) && vm.Password != vm.ConfirmPassword)
            ModelState.AddModelError(nameof(vm.ConfirmPassword), "Passwords do not match.");

        if (await _db.Users.AnyAsync(u => u.UserName == vm.UserName && u.UserId != vm.UserId))
            ModelState.AddModelError(nameof(vm.UserName), "That username is already taken.");

        if (!ModelState.IsValid) return View(await BuildUserFormAsync(vm));

        var user = await _db.Users.FindAsync(vm.UserId);
        if (user is null) return NotFound();

        // Guard: prevent admin from locking themselves out
        if (user.UserId == CurrentUserId && !vm.IsActive)
        {
            TempData["ErrorMessage"] = "You cannot deactivate your own account.";
            return RedirectToAction(nameof(Users));
        }

        user.UserName = vm.UserName;
        user.RoleId = vm.RoleId;
        user.IsActive = vm.IsActive;
        user.MustChangePassword = vm.MustChangePassword;

        if (!string.IsNullOrWhiteSpace(vm.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.Password);
            user.UserPassword = null;
            user.PasswordChangedUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Admin {Admin} updated UserId={Id}.", CurrentUserName, user.UserId);
        TempData["SuccessMessage"] = $"User '{user.UserName}' updated.";
        return RedirectToAction(nameof(Users));
    }

    /// <summary>POST: unlock a locked user account.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UserUnlock(int id)
    {
        if (RoleGuard(1) is { } r) return r;

        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        user.LockoutEndUtc = null;
        user.FailedAccessCount = 0;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Admin {Admin} unlocked UserId={Id} '{UserName}'.", CurrentUserName, user.UserId, user.UserName);
        TempData["SuccessMessage"] = $"'{user.UserName}' has been unlocked.";
        return RedirectToAction(nameof(Users));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // LOCATIONS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Lists all locations.</summary>
    public async Task<IActionResult> Locations()
    {
        if (RoleGuard(1) is { } r) return r;

        var locations = await _db.Locations
            .Include(l => l.TheaterScreens)
            .OrderBy(l => l.LocationName)
            .AsNoTracking()
            .ToListAsync();

        return View(new LocationIndexViewModel { Locations = locations });
    }

    // ── Timezone helper ────────────────────────────────────────────────────

    /// <summary>
    /// Resolves a <see cref="TimeZoneInfo"/> from a Windows timezone ID string.
    /// Falls back to Central Time if the ID is unrecognised — prevents a bad
    /// <c>Location.TimeZoneId</c> value from crashing showtime create/edit.
    /// </summary>
    private static TimeZoneInfo TryFindTimeZone(string timeZoneId)
    {
        try   { return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"); }
    }

    /// <summary>
    /// Builds the list of US timezone options for the location form dropdown,
    /// ordered from west to east. Uses Windows timezone IDs which .NET maps
    /// automatically to IANA on Linux — no platform-specific handling needed.
    /// </summary>
    private static List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> BuildTimezoneList() =>
    [
        new() { Value = "Hawaiian Standard Time",      Text = "(UTC-10) Hawaii" },
        new() { Value = "Alaskan Standard Time",       Text = "(UTC-9)  Alaska" },
        new() { Value = "Pacific Standard Time",       Text = "(UTC-8)  Pacific — Los Angeles, Seattle" },
        new() { Value = "US Mountain Standard Time",   Text = "(UTC-7)  Arizona (no DST)" },
        new() { Value = "Mountain Standard Time",      Text = "(UTC-7)  Mountain — Denver, Salt Lake City" },
        new() { Value = "Central Standard Time",       Text = "(UTC-6)  Central — Dallas, Chicago" },
        new() { Value = "Eastern Standard Time",       Text = "(UTC-5)  Eastern — New York, Miami" },
    ];

    /// <summary>GET: create-location form.</summary>
    public IActionResult LocationCreate()
    {
        if (RoleGuard(1) is { } r) return r;
        return View(new LocationFormViewModel { AvailableTimeZones = BuildTimezoneList() });
    }

    /// <summary>POST: persist new location.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LocationCreate(LocationFormViewModel vm)
    {
        if (RoleGuard(1) is { } r) return r;
        if (!ModelState.IsValid)
        {
            vm.AvailableTimeZones = BuildTimezoneList();
            return View(vm);
        }

        var location = new Location
        {
            LocationName    = vm.LocationName,
            LocationAddress = vm.LocationAddress,
            IsActive        = vm.IsActive,
            TimeZoneId      = vm.TimeZoneId
        };

        _db.Locations.Add(location);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Admin {User} created LocationId={Id} '{Name}' (TZ={TZ}).",
            CurrentUserName, location.LocationId, location.LocationName, location.TimeZoneId);
        TempData["SuccessMessage"] = $"Location '{location.LocationName}' created.";
        return RedirectToAction(nameof(Locations));
    }

    /// <summary>GET: edit-location form.</summary>
    public async Task<IActionResult> LocationEdit(int id)
    {
        if (RoleGuard(1) is { } r) return r;

        var loc = await _db.Locations.FindAsync(id);
        if (loc is null) return NotFound();

        return View(new LocationFormViewModel
        {
            LocationId        = loc.LocationId,
            LocationName      = loc.LocationName,
            LocationAddress   = loc.LocationAddress,
            IsActive          = loc.IsActive,
            TimeZoneId        = loc.TimeZoneId,
            AvailableTimeZones = BuildTimezoneList()
        });
    }

    /// <summary>POST: save location edits.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LocationEdit(LocationFormViewModel vm)
    {
        if (RoleGuard(1) is { } r) return r;
        if (!ModelState.IsValid)
        {
            vm.AvailableTimeZones = BuildTimezoneList();
            return View(vm);
        }

        var loc = await _db.Locations.FindAsync(vm.LocationId);
        if (loc is null) return NotFound();

        loc.LocationName    = vm.LocationName;
        loc.LocationAddress = vm.LocationAddress;
        loc.IsActive        = vm.IsActive;
        loc.TimeZoneId      = vm.TimeZoneId;

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Location '{loc.LocationName}' updated.";
        return RedirectToAction(nameof(Locations));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // THEATER SCREENS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Lists all theater screens.</summary>
    public async Task<IActionResult> Screens(int? locationId = null,
        string? sortBy = null, bool sortDesc = false, int page = 1)
    {
        if (RoleGuard(1) is { } r) return r;

        const int pageSize = 25;

        var query = _db.TheaterScreens
            .Include(ts => ts.Location)
            .AsNoTracking();

        if (locationId.HasValue)
            query = query.Where(ts => ts.LocationId == locationId.Value);

        query = sortBy switch
        {
            "location" => sortDesc ? query.OrderByDescending(ts => ts.Location!.LocationName).ThenByDescending(ts => ts.ScreenName)
                                   : query.OrderBy(ts => ts.Location!.LocationName).ThenBy(ts => ts.ScreenName),
            "capacity" => sortDesc ? query.OrderByDescending(ts => ts.Capacity).ThenBy(ts => ts.ScreenName)
                                   : query.OrderBy(ts => ts.Capacity).ThenBy(ts => ts.ScreenName),
            "status"   => sortDesc ? query.OrderByDescending(ts => ts.IsActive).ThenBy(ts => ts.ScreenName)
                                   : query.OrderBy(ts => ts.IsActive).ThenBy(ts => ts.ScreenName),
            _          => sortDesc ? query.OrderByDescending(ts => ts.ScreenName)
                                   : query.OrderBy(ts => ts.ScreenName)
        };

        var total = await query.CountAsync();

        var vm = new ScreenIndexViewModel
        {
            Screens = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(),
            Locations = await _db.Locations.Where(l => l.IsActive).OrderBy(l => l.LocationName).AsNoTracking().ToListAsync(),
            FilterLocationId = locationId,
            Page          = page,
            PageSize      = pageSize,
            TotalCount    = total,
            SortBy        = sortBy,
            SortDesc      = sortDesc
        };

        return View(vm);
    }

    /// <summary>GET: create-screen form.</summary>
    public async Task<IActionResult> ScreenCreate()
    {
        if (RoleGuard(1) is { } r) return r;
        return View(await BuildScreenFormAsync(new ScreenFormViewModel()));
    }

    /// <summary>POST: persist new screen.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ScreenCreate(ScreenFormViewModel vm)
    {
        if (RoleGuard(1) is { } r) return r;
        if (!ModelState.IsValid) return View(await BuildScreenFormAsync(vm));

        var screen = new TheaterScreen
        {
            LocationId = vm.LocationId,
            ScreenName = vm.ScreenName,
            Capacity = vm.Capacity,
            IsActive = vm.IsActive
        };

        _db.TheaterScreens.Add(screen);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Screen '{screen.ScreenName}' created.";
        return RedirectToAction(nameof(Screens));
    }

    /// <summary>GET: edit-screen form.</summary>
    public async Task<IActionResult> ScreenEdit(int id)
    {
        if (RoleGuard(1) is { } r) return r;

        var screen = await _db.TheaterScreens.FindAsync(id);
        if (screen is null) return NotFound();

        var vm = new ScreenFormViewModel
        {
            TheaterScreenId = screen.TheaterScreenId,
            LocationId = screen.LocationId,
            ScreenName = screen.ScreenName,
            Capacity = screen.Capacity,
            IsActive = screen.IsActive
        };

        return View(await BuildScreenFormAsync(vm));
    }

    /// <summary>POST: save screen edits.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ScreenEdit(ScreenFormViewModel vm)
    {
        if (RoleGuard(1) is { } r) return r;
        if (!ModelState.IsValid) return View(await BuildScreenFormAsync(vm));

        var screen = await _db.TheaterScreens.FindAsync(vm.TheaterScreenId);
        if (screen is null) return NotFound();

        screen.LocationId = vm.LocationId;
        screen.ScreenName = vm.ScreenName;
        screen.Capacity = vm.Capacity;
        screen.IsActive = vm.IsActive;

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Screen '{screen.ScreenName}' updated.";
        return RedirectToAction(nameof(Screens));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CSV EXPORTS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// GET: Renders the CSV / Excel export parameters form.
    /// </summary>
    /// <remarks>
    /// Displays <see cref="ExportViewModel"/> with date-range pickers and format selector
    /// (CSV or Excel). The form POSTs to <see cref="ExportTickets"/> or
    /// <see cref="ExportConcessions"/> depending on which submit button is clicked.
    /// Restricted to Admin (RoleId 1).
    /// </remarks>
    /// <returns>The Exports view pre-populated with a default <see cref="ExportViewModel"/>.</returns>
    public IActionResult Exports()
    {
        if (RoleGuard(1) is { } r) return r;
        return View(new ExportViewModel());
    }

    /// <summary>
    /// POST: Exports ticket sales within the requested date range as CSV or Excel.
    /// </summary>
    /// <remarks>
    /// Joins <c>Tickets → Showtimes → Movies</c> and filters by <c>TimeOfSale</c> between
    /// <paramref name="vm"/>.DateFrom (start of day) and DateTo (end of day, 23:59:59).
    /// Results are sorted by <c>TimeOfSale</c> ascending before projection so that EF Core
    /// can translate the <c>ORDER BY</c> to SQL — projecting into <see cref="TicketExportRow"/>
    /// first would make the sort untranslatable.
    ///
    /// On success, delegates to <see cref="ExcelResult{T}"/> or <see cref="CsvResult{T}"/>
    /// and returns a file download named <c>tickets_yyyyMMdd_yyyyMMdd.xlsx/.csv</c>.
    /// Returns the Exports view with an info message if no rows match, or an error message
    /// if the query throws. Restricted to Admin (RoleId 1).
    /// </remarks>
    /// <param name="vm">Date range and format selection submitted from the Exports form.</param>
    /// <returns>
    /// A file download (<c>.xlsx</c> or <c>.csv</c>), or the Exports view with a
    /// TempData status message when there are no results or an error occurs.
    /// </returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportTickets(ExportViewModel vm)
    {
        if (RoleGuard(1) is { } r) return r;
        if (!ModelState.IsValid) return View("Exports", vm);

        var from = vm.DateFrom.ToDateTime(TimeOnly.MinValue);
        var to   = vm.DateTo.ToDateTime(TimeOnly.MaxValue);

        // Central Time for export formatting. TimeOfSale is UTC; ShowtimeStart is
        // stored as local Central time (no UTC conversion at write time yet).
        var centralTz = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");

        List<TicketExportRow> rows;
        try
        {
            // Materialise as anonymous type first (ORDER BY on raw columns, no string ops in SQL).
            var raw = await _db.Tickets
                .Where(t => t.TimeOfSale >= from && t.TimeOfSale <= to)
                .Join(_db.Showtimes,
                      t  => t.ShowtimeId,
                      st => st.ShowtimeId,
                      (t, st) => new { t, st })
                .Join(_db.Movies,
                      x => x.st.MovieId,
                      m => m.MovieId,
                      (x, m) => new { x.t, x.st, m })
                .OrderBy(x => x.t.TimeOfSale)
                .Select(x => new
                {
                    x.t.TicketId,
                    x.t.TicketCode,
                    MovieTitle     = x.m.Title,
                    ShowtimeStart  = x.st.StartTime,
                    x.st.PricePerTicket,
                    x.t.TimeOfSale
                })
                .ToListAsync();

            // Format datetimes in-memory as Central Time strings.
            rows = raw.Select(r =>
            {
                var saleUtc   = DateTime.SpecifyKind(r.TimeOfSale, DateTimeKind.Utc);
                var saleLocal = TimeZoneInfo.ConvertTimeFromUtc(saleUtc, centralTz);
                var saleAbbr  = centralTz.IsDaylightSavingTime(saleLocal) ? "CDT" : "CST";

                // ShowtimeStart stored as local time — format directly, no UTC conversion.
                var showAbbr  = centralTz.IsDaylightSavingTime(r.ShowtimeStart) ? "CDT" : "CST";

                return new TicketExportRow(
                    r.TicketId,
                    r.TicketCode,
                    r.MovieTitle,
                    r.ShowtimeStart.ToString("MM/dd/yyyy h:mm tt") + " " + showAbbr,
                    r.PricePerTicket,
                    saleLocal.ToString("MM/dd/yyyy h:mm tt") + " " + saleAbbr);
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExportTickets failed for {From}–{To}.", from, to);
            TempData["ErrorMessage"] = "Export failed. Check application logs for details.";
            return View("Exports", vm);
        }

        if (rows.Count == 0)
        {
            TempData["InfoMessage"] = $"No tickets found between {vm.DateFrom:MM/dd/yyyy} and {vm.DateTo:MM/dd/yyyy}.";
            return View("Exports", vm);
        }

        _logger.LogInformation("Admin {User} exported {Count} ticket row(s) as {Fmt}.",
            CurrentUserName, rows.Count, vm.Format);

        return ExportResult(rows,
            vm.Format == ExportFormat.Excel
                ? $"tickets_{vm.DateFrom:yyyyMMdd}_{vm.DateTo:yyyyMMdd}.xlsx"
                : $"tickets_{vm.DateFrom:yyyyMMdd}_{vm.DateTo:yyyyMMdd}.csv",
            vm.Format);
    }

    /// <summary>
    /// POST: Exports concession sales within the requested date range as CSV or Excel.
    /// </summary>
    /// <remarks>
    /// Joins <c>ConcessionSaleItems → ConcessionSales → ConcessionItems</c> and filters by
    /// <c>ConcessionSale.TimeOfSale</c> between <paramref name="vm"/>.DateFrom (start of day)
    /// and DateTo (end of day, 23:59:59). Each row represents a single line item within a sale
    /// (one item type and quantity), not the sale header, so a multi-item order produces
    /// multiple export rows.
    ///
    /// Results are sorted by <c>TimeOfSale</c> ascending. On success, delegates to
    /// <see cref="ExcelResult{T}"/> or <see cref="CsvResult{T}"/> and returns a file download
    /// named <c>concessions_yyyyMMdd_yyyyMMdd.xlsx/.csv</c>.
    /// Returns the Exports view with an info message if no rows match, or an error message
    /// if the query throws. Restricted to Admin (RoleId 1).
    /// </remarks>
    /// <param name="vm">Date range and format selection submitted from the Exports form.</param>
    /// <returns>
    /// A file download (<c>.xlsx</c> or <c>.csv</c>), or the Exports view with a
    /// TempData status message when there are no results or an error occurs.
    /// </returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportConcessions(ExportViewModel vm)
    {
        if (RoleGuard(1) is { } r) return r;
        if (!ModelState.IsValid) return View("Exports", vm);

        var from = vm.DateFrom.ToDateTime(TimeOnly.MinValue);
        var to   = vm.DateTo.ToDateTime(TimeOnly.MaxValue);

        var centralTz = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");

        List<ConcessionExportRow> rows;
        try
        {
            var raw = await _db.ConcessionSaleItems
                .Join(_db.ConcessionSales,
                      csi => csi.ConcessionSaleId,
                      cs  => cs.ConcessionSaleId,
                      (csi, cs) => new { csi, cs })
                .Where(x => x.cs.TimeOfSale >= from && x.cs.TimeOfSale <= to)
                .Join(_db.ConcessionItems,
                      x  => x.csi.ConcessionItemId,
                      ci => ci.ConcessionItemId,
                      (x, ci) => new { x.cs, x.csi, ci })
                .OrderBy(x => x.cs.TimeOfSale)
                .Select(x => new
                {
                    SaleId     = x.cs.ConcessionSaleId,
                    x.ci.ItemName,
                    x.csi.Quantity,
                    x.csi.UnitPrice,
                    x.csi.LineTotal,
                    x.cs.TimeOfSale
                })
                .ToListAsync();

            rows = raw.Select(r =>
            {
                var saleUtc   = DateTime.SpecifyKind(r.TimeOfSale, DateTimeKind.Utc);
                var saleLocal = TimeZoneInfo.ConvertTimeFromUtc(saleUtc, centralTz);
                var saleAbbr  = centralTz.IsDaylightSavingTime(saleLocal) ? "CDT" : "CST";

                return new ConcessionExportRow(
                    r.SaleId,
                    r.ItemName,
                    r.Quantity,
                    r.UnitPrice,
                    r.LineTotal,
                    saleLocal.ToString("MM/dd/yyyy h:mm tt") + " " + saleAbbr);
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExportConcessions failed for {From}–{To}.", from, to);
            TempData["ErrorMessage"] = "Export failed. Check application logs for details.";
            return View("Exports", vm);
        }

        if (rows.Count == 0)
        {
            TempData["InfoMessage"] = $"No concession sales found between {vm.DateFrom:MM/dd/yyyy} and {vm.DateTo:MM/dd/yyyy}.";
            return View("Exports", vm);
        }

        _logger.LogInformation("Admin {User} exported {Count} concession row(s) as {Fmt}.",
            CurrentUserName, rows.Count, vm.Format);

        return ExportResult(rows,
            vm.Format == ExportFormat.Excel
                ? $"concessions_{vm.DateFrom:yyyyMMdd}_{vm.DateTo:yyyyMMdd}.xlsx"
                : $"concessions_{vm.DateFrom:yyyyMMdd}_{vm.DateTo:yyyyMMdd}.csv",
            vm.Format);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // STAFF PORTAL TEST PAGE
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// GET: Staff Portal Test Page.
    /// Mirrors the customer-facing Movies and Concessions tabs but runs entirely inside
    /// the Admin area, uses the staff session identity (not the web.sales WebUser), and
    /// links directly to the admin CRUD actions so edits are confirmed "online".
    ///
    /// Accessible to Manager (RoleId &lt;= 2) and Admin (RoleId 1).
    /// </summary>
    public async Task<IActionResult> StaffPortalTest()
    {
        if (RoleGuard(2) is { } r) return r;

        var roleName = CurrentRoleId switch
        {
            1 => "Admin",
            2 => "Manager",
            3 => "Employee",
            _ => "Unknown"
        };

        var movies = await _db.Movies
            .Include(m => m.TmdbMetadata)
            .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .OrderBy(m => m.Title)
            .AsNoTracking()
            .ToListAsync();

        var concessions = await _db.ConcessionItems
            .Include(ci => ci.Location)
            .Where(ci => ci.IsActive)
            .OrderBy(ci => ci.ItemName)
            .AsNoTracking()
            .ToListAsync();

        var vm = new StaffPortalTestViewModel
        {
            StaffUserId = CurrentUserId ?? 0,
            StaffUserName = CurrentUserName ?? "(unknown)",
            StaffRole = roleName,
            Movies = movies,
            ConcessionItems = concessions
        };

        _logger.LogInformation(
            "StaffPortalTest loaded by {Role} UserId={UserId} — {MovieCount} movies, {ConcCount} concession items.",
            roleName, CurrentUserId, movies.Count, concessions.Count);

        return View(vm);
    }



    private async Task<ShowtimeFormViewModel> BuildShowtimeFormAsync(ShowtimeFormViewModel vm)
    {
        vm.Movies = await _db.Movies.OrderBy(m => m.Title).AsNoTracking().ToListAsync();
        vm.TheaterScreens = await _db.TheaterScreens
            .Include(ts => ts.Location)
            .Where(ts => ts.IsActive)
            .OrderBy(ts => ts.Location!.LocationName).ThenBy(ts => ts.ScreenName)
            .AsNoTracking()
            .ToListAsync();
        return vm;
    }

    private async Task<ConcessionFormViewModel> BuildConcessionFormAsync(ConcessionFormViewModel vm)
    {
        vm.Locations = await _db.Locations.Where(l => l.IsActive).OrderBy(l => l.LocationName).AsNoTracking().ToListAsync();
        return vm;
    }

    private async Task<UserFormViewModel> BuildUserFormAsync(UserFormViewModel vm)
    {
        vm.Roles = await _db.Roles.OrderBy(r => r.RoleId).AsNoTracking().ToListAsync();
        return vm;
    }

    private async Task<ScreenFormViewModel> BuildScreenFormAsync(ScreenFormViewModel vm)
    {
        vm.Locations = await _db.Locations.Where(l => l.IsActive).OrderBy(l => l.LocationName).AsNoTracking().ToListAsync();
        return vm;
    }

    private FileContentResult ExportResult<T>(
        IEnumerable<T> records,
        string filename,
        ExportFormat format)
    {
        using var ms = new MemoryStream();

        if (format == ExportFormat.Excel)
        {
            // MiniExcel infers column headers from property names on the record type.
            // [ExcelColumnName] attributes on the record properties override the name.
            MiniExcel.SaveAs(ms, records, excelType: ExcelType.XLSX);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }
        else
        {
            MiniExcel.SaveAs(ms, records, excelType: ExcelType.CSV);
            return File(ms.ToArray(), "text/csv", filename);
        }
    }
}

// ── Export row shapes ─────────────────────────────────────────────────────────
// file record keeps these types invisible outside this compilation unit.
// MiniExcel reads property names as column headers; [ExcelColumnName] overrides them.
//
// DateTime columns are pre-formatted as strings ("MM/dd/yyyy h:mm tt CST/CDT") so
// Excel renders the full date and time in every cell without applying a date-only
// display format. Sorting by the Sale Time column still works correctly because
// MiniExcel writes formatted strings, and alphabetic sort on "MM/dd/yyyy h:mm tt"
// is chronological. For strict sort-by-time needs, use the OrderBy in the query.

file record TicketExportRow(
    [property: ExcelColumnName("Ticket ID")]    int     TicketId,
    [property: ExcelColumnName("Ticket Code")]  long    TicketCode,
    [property: ExcelColumnName("Movie")]        string  MovieTitle,
    [property: ExcelColumnName("Showtime")]     string  ShowtimeStart,
    [property: ExcelColumnName("Price")]        decimal PricePerTicket,
    [property: ExcelColumnName("Sale Time")]    string  TimeOfSale);

file record ConcessionExportRow(
    [property: ExcelColumnName("Sale ID")]      int     SaleId,
    [property: ExcelColumnName("Item")]         string  ItemName,
    [property: ExcelColumnName("Qty")]          int     Quantity,
    [property: ExcelColumnName("Unit Price")]   decimal UnitPrice,
    [property: ExcelColumnName("Line Total")]   decimal LineTotal,
    [property: ExcelColumnName("Sale Time")]    string  TimeOfSale);