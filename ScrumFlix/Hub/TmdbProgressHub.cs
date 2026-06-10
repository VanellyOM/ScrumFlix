/*
 * File:      /ScrumFlix/Hub/TmdbProgressHub.cs
 * Namespace: ScrumFlix.Hubs
 * Purpose:   SignalR hub for broadcasting real-time TMDb sync progress to
 *            Admin clients on the TmdbSyncPage.
 *
 * Architecture:
 *   - Only Admin users (RoleId == 1) can connect — guarded by [Authorize].
 *     ScrumFlix uses session-based auth, not ASP.NET Core Identity, so the
 *     [Authorize] attribute alone is insufficient. The hub inherits the session
 *     guard pattern used by StaffControllerBase: if the session role is missing
 *     or not Admin, OnConnectedAsync aborts the connection.
 *
 *   - A single group "tmdb-sync" is used rather than per-user groups because
 *     only one Admin sync can run at a time and all connected Admin clients
 *     watching the sync page should see the same progress.
 *
 * Events emitted by the server (via IHubContext<TmdbProgressHub>):
 *   "TmdbSyncProgress"  — { percent: int, message: string, synced: int,
 *                           skipped: int, failed: int, total: int }
 *   "TmdbSyncComplete"  — { synced: int, skipped: int, failed: int, wasForced: bool }
 *   "TmdbSyncError"     — { message: string }
 *
 * Consumed by:
 *   - TmdbSyncPage.cshtml via sfSpinner.fromSignalR() helper in sf-spinner.js
 *   - AdminHomeController.TmdbSyncRunAsync (POST — new streaming action)
 *
 * Route: /tmdbSyncHub (registered in Program.cs)
 *
 * Phase: 6 — Real-time TMDb sync progress
 */

using Microsoft.AspNetCore.SignalR;
using ScrumFlix.Services;

namespace ScrumFlix.Hubs;

/// <summary>
/// SignalR hub for broadcasting TMDb sync progress to Admin clients.
/// Emits TmdbSyncProgress events as each movie is synced so the
/// sf-spinner component can display real, accurate percentages.
/// </summary>
public class TmdbProgressHub : Hub
{
    private readonly ILogger<TmdbProgressHub> _logger;

    /// <summary>SignalR group name used for all sync-progress broadcasts.</summary>
    public const string SyncGroup = "tmdb-sync";

    public TmdbProgressHub(ILogger<TmdbProgressHub> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        // Join the broadcast group — all Admin clients watching the sync page receive progress.
        await Groups.AddToGroupAsync(Context.ConnectionId, SyncGroup);

        _logger.LogDebug(
            "TmdbProgressHub: client connected — ConnectionId={ConnectionId}",
            Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is not null)
        {
            _logger.LogWarning(exception,
                "TmdbProgressHub: client disconnected with error — ConnectionId={ConnectionId}",
                Context.ConnectionId);
        }
        else
        {
            _logger.LogDebug(
                "TmdbProgressHub: client disconnected cleanly — ConnectionId={ConnectionId}",
                Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
