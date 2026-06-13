/*
 * File:      /ScrumFlix/Hub/ProgressHub.cs
 * Namespace: ScrumFlix.Hubs
 * Purpose:   Generic SignalR hub for broadcasting real-time long-running
 *            operation progress (Phase 4.0 shared progress framework).
 *
 * Architecture:
 *   - One group per operation id. Clients call JoinOperation(operationId)
 *     after connecting to subscribe to that operation's "ProgressUpdate"
 *     events (see ProgressReporter.EventName).
 *   - Session guard: ScrumFlix uses session-based auth, NOT ASP.NET Core
 *     Identity, so a bare [Authorize] attribute is insufficient (it has no
 *     session-cookie-backed authentication handler to evaluate). Instead,
 *     OnConnectedAsync reads HttpContext.Session directly — mirroring
 *     StaffControllerBase.RoleGuard(1) — and aborts the connection if the
 *     session RoleId is missing or > 1 (i.e. not Admin). This is the same
 *     pattern TmdbProgressHub's doc comments describe but actually enforce
 *     it, since neither TmdbProgressHub nor ScheduleHub currently perform a
 *     real session check.
 *   - ClientCancel(operationId) lets the client request cancellation of a
 *     running operation via IProgressReporterFactory.Cancel — wired to the
 *     Cancel button in sf-progress.js.
 *
 * Events emitted by the server (via IHubContext<ProgressHub>):
 *   "ProgressUpdate" — full ProgressState object (see ProgressReporter).
 *
 * Client → server methods (called from sf-progress.js):
 *   JoinOperation(operationId)   — subscribe to an operation's group
 *   LeaveOperation(operationId)  — unsubscribe
 *   ClientCancel(operationId)    — request cancellation of the operation
 *
 * Route: /progressHub (registered in Program.cs)
 *
 * Phase: 4.0 — Shared progress framework
 */

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using ScrumFlix.Services;
using ScrumFlix.Services.Progress;

namespace ScrumFlix.Hubs;

/// <summary>
/// Generic SignalR hub for the Phase 4.0 shared progress framework. Admin-only
/// (RoleId == 1), enforced via the session-cookie role check in
/// <see cref="OnConnectedAsync"/> rather than <c>[Authorize]</c>.
/// </summary>
public class ProgressHub : Hub
{
    private readonly ILogger<ProgressHub>       _logger;
    private readonly IProgressReporterFactory   _reporterFactory;

    public ProgressHub(ILogger<ProgressHub> logger, IProgressReporterFactory reporterFactory)
    {
        _logger          = logger;
        _reporterFactory = reporterFactory;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Aborts the connection unless the session RoleId is present and == 1
    /// (Admin). ScrumFlix's session cookie is configured with
    /// <c>SameSite=Lax</c> and flows with the SignalR negotiate/connect
    /// requests by default, so <c>HttpContext.Session</c> is populated here
    /// exactly as it is in StaffControllerBase-derived controllers.
    /// </remarks>
    public override async Task OnConnectedAsync()
    {
        try
        {
            var httpContext = Context.GetHttpContext();

            if (httpContext is null)
            {
                _logger.LogWarning(
                    "ProgressHub: HttpContext unavailable for ConnectionId={ConnectionId} — " +
                    "rejecting connection (session cannot be evaluated).",
                    Context.ConnectionId);

                Context.Abort();
                return;
            }

            var hasSession = httpContext.Features.Get<ISessionFeature>()?.Session is not null;
            var roleId     = httpContext.Session.GetInt32(AuthService.SessionRoleId);
            var userId     = httpContext.Session.GetInt32(AuthService.SessionUserId);

            _logger.LogDebug(
                "ProgressHub: OnConnectedAsync — ConnectionId={ConnectionId}, HasSession={HasSession}, " +
                "UserId={UserId}, RoleId={RoleId}.",
                Context.ConnectionId, hasSession, userId, roleId);

            if (roleId is null || roleId != 1)
            {
                _logger.LogWarning(
                    "ProgressHub: rejected connection — ConnectionId={ConnectionId}, RoleId={RoleId}.",
                    Context.ConnectionId, roleId);

                Context.Abort();
                return;
            }

            _logger.LogDebug(
                "ProgressHub: client connected — ConnectionId={ConnectionId}, UserId={UserId}.",
                Context.ConnectionId, userId);

            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "ProgressHub: unhandled exception in OnConnectedAsync — ConnectionId={ConnectionId}.",
                Context.ConnectionId);
            throw;
        }
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is not null)
        {
            _logger.LogWarning(exception,
                "ProgressHub: client disconnected with error — ConnectionId={ConnectionId}.",
                Context.ConnectionId);
        }
        else
        {
            _logger.LogDebug(
                "ProgressHub: client disconnected cleanly — ConnectionId={ConnectionId}.",
                Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ── Group management ────────────────────────────────────────────────────

    /// <summary>
    /// Subscribes the calling connection to <paramref name="operationId"/>'s
    /// broadcast group. Called by sf-progress.js immediately after the hub
    /// connection starts.
    /// </summary>
    public async Task JoinOperation(string operationId)
    {
        if (string.IsNullOrWhiteSpace(operationId)) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, operationId);

        _logger.LogDebug(
            "ProgressHub: connection {ConnectionId} joined operation {OperationId}.",
            Context.ConnectionId, operationId);
    }

    /// <summary>
    /// Unsubscribes the calling connection from <paramref name="operationId"/>'s
    /// broadcast group.
    /// </summary>
    public async Task LeaveOperation(string operationId)
    {
        if (string.IsNullOrWhiteSpace(operationId)) return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, operationId);

        _logger.LogDebug(
            "ProgressHub: connection {ConnectionId} left operation {OperationId}.",
            Context.ConnectionId, operationId);
    }

    // ── Cancellation ─────────────────────────────────────────────────────────

    /// <summary>
    /// Requests cancellation of the operation identified by
    /// <paramref name="operationId"/> via
    /// <see cref="IProgressReporterFactory.Cancel"/>. Wired to the Cancel
    /// button in sf-progress.js. No-op if the operation is unknown or has
    /// already finished.
    /// </summary>
    public Task ClientCancel(string operationId)
    {
        if (string.IsNullOrWhiteSpace(operationId)) return Task.CompletedTask;

        var cancelled = _reporterFactory.Cancel(operationId);

        _logger.LogInformation(
            "ProgressHub: client {ConnectionId} requested cancel for operation {OperationId} (accepted={Accepted}).",
            Context.ConnectionId, operationId, cancelled);

        return Task.CompletedTask;
    }
}
