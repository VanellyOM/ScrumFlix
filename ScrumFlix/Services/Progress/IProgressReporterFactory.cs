/*
 * File:      /ScrumFlix/Services/Progress/IProgressReporterFactory.cs
 * Namespace: ScrumFlix.Services.Progress
 * Purpose:   Mints IProgressReporter instances for the Phase 4.0 shared
 *            progress framework and owns the cross-operation cancellation
 *            registry.
 *
 * Architecture:
 *   - Registered as a SINGLETON (see Program.cs) because the cancellation
 *     registry must outlive any single HTTP request or SignalR connection —
 *     a Cancel(operationId) call typically arrives on a different request
 *     than the one running the operation.
 *   - Create() generates (or accepts) an operation id, registers a linked
 *     CancellationTokenSource, and returns a ProgressReporter scoped to that
 *     id's SignalR group.
 *   - Cancel(operationId) signals the registered CancellationTokenSource (if
 *     any) so the long-running loop's IProgressReporter.CancellationToken
 *     observes the request.
 *   - Release(operationId) removes the registry entry once the operation has
 *     finished (success, error, or cancellation) to avoid an unbounded
 *     ConcurrentDictionary.
 *
 * Phase: 4.0 — Shared progress framework
 */

namespace ScrumFlix.Services.Progress;

/// <summary>
/// Factory for operation-scoped <see cref="IProgressReporter"/> instances.
/// Also owns the cancellation registry shared across all in-flight
/// long-running operations.
/// </summary>
public interface IProgressReporterFactory
{
    /// <summary>
    /// Creates a new <see cref="IProgressReporter"/> for a freshly generated
    /// operation id (GUID), registering a cancellation token source for it.
    /// </summary>
    /// <param name="operationName">
    /// Human-readable operation name (e.g. "TMDb Sync", "Database Backup")
    /// used to populate <see cref="ProgressState.OperationName"/>.
    /// </param>
    /// <param name="externalCancellationToken">
    /// Optional caller-supplied token (e.g. <c>HttpContext.RequestAborted</c>)
    /// linked into the reporter's <see cref="IProgressReporter.CancellationToken"/>
    /// alongside the factory's own cancellation registry entry.
    /// </param>
    IProgressReporter Create(
        string             operationName,
        CancellationToken  externalCancellationToken = default);

    /// <summary>
    /// Creates a new <see cref="IProgressReporter"/> for a caller-supplied
    /// operation id, registering a cancellation token source for it.
    /// Use this when the operation id must be known before the reporter is
    /// minted (e.g. returned to the client before work begins).
    /// </summary>
    /// <param name="operationId">Caller-supplied unique operation id.</param>
    /// <param name="operationName">
    /// Human-readable operation name (e.g. "TMDb Sync", "Database Backup")
    /// used to populate <see cref="ProgressState.OperationName"/>.
    /// </param>
    /// <param name="externalCancellationToken">
    /// Optional caller-supplied token (e.g. <c>HttpContext.RequestAborted</c>)
    /// linked into the reporter's <see cref="IProgressReporter.CancellationToken"/>
    /// alongside the factory's own cancellation registry entry.
    /// </param>
    IProgressReporter Create(
        string             operationId,
        string             operationName,
        CancellationToken  externalCancellationToken = default);

    /// <summary>
    /// Requests cancellation of the operation identified by
    /// <paramref name="operationId"/>. No-op (returns <see langword="false"/>)
    /// if the operation id is unknown or already completed.
    /// </summary>
    /// <returns><see langword="true"/> if a cancellation was signalled.</returns>
    bool Cancel(string operationId);

    /// <summary>
    /// Removes the cancellation registry entry for
    /// <paramref name="operationId"/>. Should be called once an operation
    /// reaches a terminal state (success, error, or cancellation) to avoid
    /// an unbounded registry. Safe to call multiple times.
    /// </summary>
    void Release(string operationId);
}
