/*
 * File:      /ScrumFlix/ViewModels/LocationConflictViewModel.cs
 * Namespace: ScrumFlix.ViewModels
 * Purpose:   Strongly-typed ViewModel for the location conflict resolution page.
 *            Replaces ViewBag.TicketLocationId, ViewBag.TicketLocationName, and
 *            ViewBag.ConcessionLocationId — all previously pulled from TempData.Peek().
 *
 *            Data shape:
 *              TicketLocationId     — LocationId of the theater the user selected
 *                                     a showtime ticket for.
 *              TicketLocationName   — Human-readable name of that theater.
 *              ConcessionLocationId — LocationId tied to concession items already
 *                                     in the cart (the "home" location for those items).
 *
 *            All three are nullable because they originate from TempData, which can
 *            be absent if the user navigates to this page out of sequence.
 *            ConcessionsController.LocationConflict() redirects to CartReview when
 *            ConflictTicketLocationId TempData is missing, so in normal flow all
 *            three values will be populated.
 *
 * Sprint: S1 — ViewBag Purge
 */

namespace ScrumFlix.ViewModels;

/// <summary>
/// ViewModel for the location conflict resolution page.
/// Carries the theater IDs and display name needed to present both resolution options.
/// </summary>
public class LocationConflictViewModel
{
    /// <summary>
    /// The <c>LocationId</c> of the theater the user wants to buy a ticket for.
    /// </summary>
    public int? TicketLocationId { get; set; }

    /// <summary>
    /// The display name of the theater the user wants to buy a ticket for.
    /// Used in the conflict description and button labels.
    /// Falls back to <c>"the selected theater"</c> in the view when null.
    /// </summary>
    public string? TicketLocationName { get; set; }

    /// <summary>
    /// The <c>LocationId</c> of the theater that concession items already in the
    /// cart are associated with.
    /// </summary>
    public int? ConcessionLocationId { get; set; }

    /// <summary>
    /// Convenience accessor returning <see cref="TicketLocationName"/> or a safe
    /// fallback, eliminating null-coalescing boilerplate in the view.
    /// </summary>
    public string TicketLocationDisplayName =>
        TicketLocationName ?? "the selected theater";
}
