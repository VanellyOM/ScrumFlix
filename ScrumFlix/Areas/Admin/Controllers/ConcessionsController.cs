// File: Areas/Admin/Controllers/ConcessionsController.cs
// Staff Portal concessions catalog view — shows what movie goers see, with admin controls.

namespace ScrumFlix.Areas.Admin.Controllers;

[Area("Admin")]
public class ConcessionsController : StaffControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ConcessionsController> _logger;

    public ConcessionsController(AppDbContext db, ILogger<ConcessionsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// GET /Admin/Concessions/ConcessionsCatalog
    /// Staff portal view of the concessions catalog — identical data to the customer view,
    /// rendered inside the Admin layout with restock and edit action columns.
    /// Requires Manager or Admin role.
    /// </summary>
    public async Task<IActionResult> ConcessionsCatalog(int? locationId)
    {
        if (RoleGuard(2) is { } r) return r;

        var query = _db.ConcessionItems
            .Include(ci => ci.Location)
            .AsNoTracking();

        if (locationId.HasValue)
            query = query.Where(ci => ci.LocationId == locationId.Value);

        var vm = new ConcessionIndexViewModel
        {
            Items       = await query.OrderBy(ci => ci.ItemName).ToListAsync(),
            Locations   = await _db.Locations.Where(l => l.IsActive)
                              .OrderBy(l => l.LocationName).AsNoTracking().ToListAsync(),
            FilterLocationId = locationId,
            ShowInactive = true
        };

        _logger.LogInformation("Staff {User} viewed Admin ConcessionsCatalog ({Count} items).",
            CurrentUserName, vm.Items.Count);

        return View(vm);
    }
}
