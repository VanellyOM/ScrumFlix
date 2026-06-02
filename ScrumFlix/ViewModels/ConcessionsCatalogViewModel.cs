/*
 * File: /ScrumFlix/ViewModels/ConcessionsCatalogViewModel.cs
 * Description: ViewModel for the concessions browsing page.
 *
 * Phase 3 — Backend Alignment (#32 / P3-6):
 *   REMOVED:
 *     - ConcessionItemDisplayModel wrapper class (was needed to marry Inventory + location price)
 *     - List<ConcessionItemDisplayModel> Items → replaced with List<ConcessionItem> ConcessionItems
 *     - SelectedLocationId (int) → now int? since location context is optional
 *   RATIONALE:
 *     ConcessionItem in the canonical schema already carries Price directly,
 *     so the intermediate display model is no longer needed. The view binds
 *     directly against ConcessionItem properties.
 */

namespace ScrumFlix.ViewModels;

/// <summary>
/// ViewModel for the concessions catalog page listing all active items.
/// Location context (if present) is used only for the ticket-flow banner —
/// not for pricing, since ConcessionItem.Price is location-independent.
/// </summary>
public class ConcessionsCatalogViewModel
{
    /// <summary>
    /// Gets or sets the active concession items available for purchase.
    /// Sourced directly from the canonical ConcessionItem table.
    /// </summary>
    public List<ConcessionItem> ConcessionItems { get; set; } = new();

    /// <summary>Gets or sets all active theater locations (for banner display only).</summary>
    public List<Location> Locations { get; set; } = new();

    /// <summary>Gets or sets the optional location context (from ticket flow or URL param).</summary>
    public int? SelectedLocationId { get; set; }

    /// <summary>
    /// When the user arrives here via "Add Concessions Too" from the booking page,
    /// this is the LocationId of the ticket they just added. The view uses this to
    /// render a contextual banner. Null when the user arrives independently.
    /// </summary>
    public int? TicketLocationId { get; set; }

    /// <summary>Display name of the ticket's theater, shown in the contextual banner.</summary>
    public string? TicketLocationName { get; set; }

    /// <summary>True when the page was reached via the ticket booking flow.</summary>
    public bool IsTicketFlow => TicketLocationId.HasValue;
}
