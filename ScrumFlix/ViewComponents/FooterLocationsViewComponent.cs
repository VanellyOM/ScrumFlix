/*
 * File: /ScrumFlix/ViewComponents/FooterLocationsViewComponent.cs
 *
 * Queries active Location rows from the database and passes them to the
 * footer locations partial. Using a ViewComponent keeps _Layout.cshtml
 * decoupled from every controller — no controller needs to pass locations
 * down through ViewBag or a base ViewModel.
 *
 * Usage in _Layout.cshtml:
 *   @await Component.InvokeAsync("FooterLocations")
 *
 * View: /Views/Shared/Components/FooterLocations/Default.cshtml
 */

namespace ScrumFlix.ViewComponents;

/// <summary>
/// Supplies the footer Locations column with live data from the database.
/// Only active locations (IsActive == true) are shown, ordered by name.
/// </summary>
public class FooterLocationsViewComponent : ViewComponent
{
    private readonly AppDbContext _db;

    public FooterLocationsViewComponent(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var locations = await _db.Locations
            .Where(l => l.IsActive)
            .OrderBy(l => l.LocationName)
            .Select(l => l.LocationName)
            .AsNoTracking()
            .ToListAsync();

        return View(locations);
    }
}
