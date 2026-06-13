/*
 * File:      /ScrumFlix/Services/Progress/ProgressReporterFactory.cs
 * Namespace: ScrumFlix.Services.Progress
 * Purpose:   Default IProgressReporterFactory implementation. Owns the
 *            cross-operation cancellation registry for the Phase 4.0 shared
 *            progress framework.
 *
 * Lifetime: Singleton (see Program.cs registration). The cancellation
 *           registry must be shared across requests/connections — a
 *           Cancel(operationId) call from a SignalR hub method arrives on a
 *           different "request" than the one running the operation's loop.
 *
 * Phase: 4.0 — Shared progress framework
 */

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using ScrumFlix.Hubs;

namespace ScrumFlix.Services.Progress;

/// <inheritdoc />
public sealed class ProgressReporterFactory : IProgressReporterFactory
{
    private readonly IHubContext<ProgressHub>          _hub;
    private readonly ILogger<ProgressReporterFactory>  _logger;

    /// <summary>
    /// Cancellation token sources keyed by operation id. Entries are added
    /// by <see cref="Create(string, string, CancellationToken)"/> and removed
    /// by <see cref="Release"/>.
    /// </summary>
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _registry = new();

    public ProgressReporterFactory(
        IHubContext<ProgressHub>         hub,
        ILogger<ProgressReporterFactory> logger)
    {
        _hub    = hub;
        _logger = logger;
    }

    /// <inheritdoc />
    public IProgressReporter Create(
        string            operationName,
        CancellationToken externalCancellationToken = default)
        => Create(Guid.NewGuid().ToString("n"), operationName, externalCancellationToken);

    /// <inheritdoc />
    public IProgressReporter Create(
        string            operationId,
        string            operationName,
        CancellationToken externalCancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);

        // Replace any stale entry for this id (shouldn't normally happen with
        // GUID-generated ids, but a caller-supplied id could collide).
        if (_registry.TryRemove(operationId, out var stale))
        {
            stale.Dispose();
        }

        _registry[operationId] = cts;

        _logger.LogDebug(
            "ProgressReporterFactory: created reporter for operation {OperationId} ({OperationName}).",
            operationId, operationName);

        return new ProgressReporter(_hub, _logger, operationId, operationName, cts.Token);
    }

    /// <inheritdoc />
    public bool Cancel(string operationId)
    {
        if (!_registry.TryGetValue(operationId, out var cts))
            return false;

        try
        {
            cts.Cancel();
            _logger.LogInformation(
                "ProgressReporterFactory: cancellation requested for operation {OperationId}.",
                operationId);
            return true;
        }
        catch (ObjectDisposedException)
        {
            // Already released/disposed — treat as no-op.
            return false;
        }
    }

    /// <inheritdoc />
    public void Release(string operationId)
    {
        if (_registry.TryRemove(operationId, out var cts))
        {
            cts.Dispose();

            _logger.LogDebug(
                "ProgressReporterFactory: released registry entry for operation {OperationId}.",
                operationId);
        }
    }
}
