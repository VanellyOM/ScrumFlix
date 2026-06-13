/*
 * File:      /ScrumFlix/Services/Progress/IProgressReporter.cs
 * Namespace: ScrumFlix.Services.Progress
 * Purpose:   Operation-scoped progress reporter contract for the Phase 4.0
 *            shared progress framework.
 *
 * Architecture:
 *   - Minted per-operation by IProgressReporterFactory so callers (TMDb sync,
 *     database backup, etc.) never touch IHubContext or SignalR group names
 *     directly.
 *   - Report/Complete/Error all push a ProgressState to the operation's
 *     SignalR group. Implementations MUST swallow hub-transport exceptions
 *     (log + continue) — a broken SignalR connection must never abort the
 *     underlying long-running operation.
 *   - CancellationToken is exposed so long-running loops can observe a
 *     Cancel(operationId) request from IProgressReporterFactory without the
 *     caller needing to manage its own CancellationTokenSource.
 *   - Deliberately execution-agnostic: nothing here assumes the operation
 *     runs synchronously inside an HTTP request. A future background-queue
 *     implementation can use the same reporter/factory pair unchanged.
 *
 * Phase: 4.0 — Shared progress framework
 */

namespace ScrumFlix.Services.Progress;

/// <summary>
/// Operation-scoped sink for progress updates. Obtained from
/// <see cref="IProgressReporterFactory"/> for a specific operation id.
/// </summary>
public interface IProgressReporter
{
    /// <summary>
    /// The operation id this reporter is scoped to. Matches the SignalR
    /// group name clients join via <see cref="ScrumFlix.Hubs.ProgressHub"/>.
    /// </summary>
    string OperationId { get; }

    /// <summary>
    /// Cancellation token that is signalled when
    /// <see cref="IProgressReporterFactory.Cancel"/> is called for this
    /// operation id. Long-running loops should observe this in addition to
    /// (or linked with) any caller-supplied <see cref="CancellationToken"/>.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Broadcasts an in-progress <see cref="ProgressState"/> to the
    /// operation's SignalR group. Implementations throttle frequent updates
    /// and swallow hub-transport exceptions.
    /// </summary>
    void Report(ProgressState state);

    /// <summary>
    /// Broadcasts a terminal "complete" <see cref="ProgressState"/> (100%,
    /// <see cref="ProgressState.IsComplete"/> = true) with an optional
    /// human-readable summary.
    /// </summary>
    /// <param name="summary">
    /// Optional completion summary shown by the client (e.g.
    /// "12 synced, 1 skipped, 0 failed.").
    /// </param>
    void Complete(string? summary = null);

    /// <summary>
    /// Broadcasts a terminal "error" <see cref="ProgressState"/>
    /// (<see cref="ProgressState.IsError"/> = true) with the given message.
    /// </summary>
    /// <param name="message">Human-readable error message for the client.</param>
    void Error(string message);
}
