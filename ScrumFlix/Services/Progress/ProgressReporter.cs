/*
 * File:      /ScrumFlix/Services/Progress/ProgressReporter.cs
 * Namespace: ScrumFlix.Services.Progress
 * Purpose:   Default IProgressReporter implementation. Wraps
 *            IHubContext<ProgressHub> + an operation id/group and pushes
 *            ProgressState updates to all clients subscribed to that group.
 *
 * Architecture:
 *   - One ProgressReporter instance per operation, minted by
 *     ProgressReporterFactory.Create(operationId, operationName).
 *   - Throttling: Report() updates are rate-limited to at most one broadcast
 *     per ThrottleInterval, EXCEPT the very first update (so the client sees
 *     immediate feedback) and any update where Percent reaches 100, which
 *     always sends immediately. Complete()/Error() always send immediately
 *     and bypass the throttle entirely — terminal states must never be
 *     dropped.
 *   - Hub-transport exceptions are logged and swallowed (mirrors the
 *     try/catch around _tmdbHub.Clients...SendAsync in
 *     AdminHomeController.TmdbSyncRun) — a broken SignalR connection must
 *     never abort the underlying operation.
 *   - This type does not own the CancellationTokenSource; it is handed a
 *     CancellationToken by ProgressReporterFactory so the factory remains
 *     the single owner of the cancellation registry.
 *
 * Event name: "ProgressUpdate" — single event carrying the full ProgressState
 * object, per the Phase 4.0 contract.
 *
 * Phase: 4.0 — Shared progress framework
 */

using Microsoft.AspNetCore.SignalR;
using ScrumFlix.Hubs;

namespace ScrumFlix.Services.Progress;

/// <inheritdoc />
internal sealed class ProgressReporter : IProgressReporter
{
    /// <summary>Minimum time between non-terminal broadcasts for the same operation.</summary>
    private static readonly TimeSpan ThrottleInterval = TimeSpan.FromMilliseconds(400);

    /// <summary>SignalR event name carrying the full ProgressState payload.</summary>
    public const string EventName = "ProgressUpdate";

    private readonly IHubContext<ProgressHub> _hub;
    private readonly ILogger                  _logger;
    private readonly string                   _operationName;

    private DateTime _lastSentUtc = DateTime.MinValue;
    private bool     _sentAny;
    private bool     _terminalSent;

    public ProgressReporter(
        IHubContext<ProgressHub> hub,
        ILogger                  logger,
        string                   operationId,
        string                   operationName,
        CancellationToken        cancellationToken)
    {
        _hub               = hub;
        _logger            = logger;
        _operationName     = operationName;
        OperationId        = operationId;
        CancellationToken  = cancellationToken;
    }

    /// <inheritdoc />
    public string OperationId { get; }

    /// <inheritdoc />
    public CancellationToken CancellationToken { get; }

    /// <inheritdoc />
    public void Report(ProgressState state)
    {
        if (_terminalSent) return;

        var now           = DateTime.UtcNow;
        var elapsed       = now - _lastSentUtc;
        var isMilestone   = !_sentAny || state.Percent >= 100;

        if (!isMilestone && elapsed < ThrottleInterval)
            return;

        _sentAny     = true;
        _lastSentUtc = now;

        Send(state);
    }

    /// <inheritdoc />
    public void Complete(string? summary = null)
    {
        if (_terminalSent) return;
        _terminalSent = true;

        Send(ProgressState.Completed(
            operationId:   OperationId,
            operationName: _operationName,
            total:         0,
            succeeded:     0,
            skipped:       0,
            failed:        0,
            summary:       summary));
    }

    /// <inheritdoc />
    public void Error(string message)
    {
        if (_terminalSent) return;
        _terminalSent = true;

        Send(ProgressState.ErrorState(
            operationId:   OperationId,
            operationName: _operationName,
            message:       message));
    }

    /// <summary>
    /// Fire-and-forget broadcast to the operation's SignalR group. Hub
    /// transport exceptions are logged and swallowed — they must never
    /// propagate into the caller's long-running operation.
    /// </summary>
    private void Send(ProgressState state)
    {
        try
        {
            // Fire-and-forget: progress broadcasts must not block the
            // underlying operation on SignalR transport latency.
            _ = _hub.Clients
                .Group(OperationId)
                .SendAsync(EventName, state)
                .ContinueWith(t =>
                {
                    if (t.Exception is not null)
                    {
                        _logger.LogWarning(t.Exception,
                            "ProgressReporter: failed to broadcast {Event} for operation {OperationId}.",
                            EventName, OperationId);
                    }
                }, TaskScheduler.Default);
        }
        catch (Exception ex)
        {
            // Synchronous failures (e.g. hub context disposed) — non-fatal.
            _logger.LogWarning(ex,
                "ProgressReporter: failed to send {Event} for operation {OperationId}.",
                EventName, OperationId);
        }
    }
}
