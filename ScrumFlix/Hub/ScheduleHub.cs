using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ScrumFlix.Hubs;

/// <summary>
/// SignalR hub for real-time schedule synchronization across connected clients.
///
/// Architecture:
///   - Clients join location-scoped groups on connect so broadcasts are
///     targeted — Manager A at "Main St Theater" doesn't receive noise from
///     a different location.
///   - All mutating controller actions (AddShift, UpdateShift, DeleteShift,
///     AddAssignment, etc.) inject IHubContext&lt;ScheduleHub&gt; and call
///     SendAsync after a successful DB save.
///   - The client-side JS listener calls htmx.ajax() to pull the refreshed
///     partial view, keeping all rendering server-side (Razor partials).
///
/// Client events emitted by the server:
///   "ShiftsUpdated"       — shifts grid + visual Gantt panel should refresh
///   "AssignmentsUpdated"  — assignments grid should refresh
///
/// Client → server methods (called from JS):
///   JoinLocationGroup(locationId)   — subscribe to a location's broadcast group
///   LeaveLocationGroup(locationId)  — unsubscribe (called on location combo change)
/// </summary>
[Authorize]
public class ScheduleHub : Hub
{
    private readonly ILogger<ScheduleHub> _logger;

    public ScheduleHub(ILogger<ScheduleHub> logger)
    {
        _logger = logger;
    }

    // ── Group management ────────────────────────────────────────────────────

    /// <summary>
    /// Adds the calling connection to a location-scoped broadcast group.
    /// Called from the client when the page loads or the location combo changes.
    /// </summary>
    /// <param name="locationId">The LocationId the client is currently viewing.</param>
    public async Task JoinLocationGroup(int locationId)
    {
        var groupName = LocationGroup(locationId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogDebug(
            "Connection {ConnectionId} joined group {Group}",
            Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Removes the calling connection from a location-scoped broadcast group.
    /// Called from the client before joining a different location group.
    /// </summary>
    /// <param name="locationId">The LocationId the client is leaving.</param>
    public async Task LeaveLocationGroup(int locationId)
    {
        var groupName = LocationGroup(locationId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogDebug(
            "Connection {ConnectionId} left group {Group}",
            Context.ConnectionId, groupName);
    }

    // ── Lifecycle ───────────────────────────────────────────────────────────

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "ScheduleHub: client connected — ConnectionId={ConnectionId}, User={User}",
            Context.ConnectionId,
            Context.User?.Identity?.Name ?? "anonymous");

        await base.OnConnectedAsync();
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is not null)
        {
            _logger.LogWarning(exception,
                "ScheduleHub: client disconnected with error — ConnectionId={ConnectionId}",
                Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation(
                "ScheduleHub: client disconnected cleanly — ConnectionId={ConnectionId}",
                Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Canonical group name for a given location, shared between the hub
    /// and any controller using IHubContext&lt;ScheduleHub&gt; to broadcast.
    /// </summary>
    public static string LocationGroup(int locationId) =>
        $"schedule-location-{locationId}";
}
