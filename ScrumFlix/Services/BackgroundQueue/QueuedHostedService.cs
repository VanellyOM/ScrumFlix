/*
 * File:      /ScrumFlix/Services/BackgroundQueue/QueuedHostedService.cs
 * Namespace: ScrumFlix.Services.BackgroundQueue
 * Purpose:   BackgroundService that drains IBackgroundTaskQueue and runs each
 *            work item inside its own DI scope (Phase 4.3).
 *
 * Architecture:
 *   - Registered via AddHostedService<QueuedHostedService>() — runs for the
 *     lifetime of the application (alongside SeatReservationExpiryService).
 *   - For each dequeued work item, creates a fresh IServiceScope via
 *     IServiceScopeFactory and passes scope.ServiceProvider into the delegate,
 *     so scoped services (AppDbContext, ITmdbSyncService, IDatabaseBackupService,
 *     IAuditService, IEmailService) resolve correctly even though the HTTP
 *     request that enqueued the item has long since completed.
 *   - Exceptions thrown by a single work item are logged and SWALLOWED so one
 *     failed operation never tears down the host loop (which would silently
 *     stop all future background work). The work items themselves are expected
 *     to translate failures into reporter.Error(...) for the client; this
 *     catch is the last line of defence.
 *   - The stopping token handed to each work item is ExecuteAsync's
 *     stoppingToken (graceful shutdown / app-pool recycle). OPERATION-level
 *     cancellation is independent — see IBackgroundTaskQueue remarks.
 *
 * Somee.com / app-pool-recycle caveat (documented per Phase 4.3 plan):
 *   On recycle, the host stops, the in-memory channel is discarded, and any
 *   queued/in-flight item is lost. This matches the pre-4.3 behaviour where a
 *   synchronous operation died with its recycled request. No persistence is
 *   added — out of scope.
 *
 * Phase: 4.3 — Background-queue redesign
 */

namespace ScrumFlix.Services.BackgroundQueue;

/// <summary>
/// Long-running hosted service that sequentially drains
/// <see cref="IBackgroundTaskQueue"/>, executing each work item in its own DI
/// scope and isolating per-item failures.
/// </summary>
public sealed class QueuedHostedService : BackgroundService
{
    private readonly IBackgroundTaskQueue          _queue;
    private readonly IServiceScopeFactory          _scopeFactory;
    private readonly ILogger<QueuedHostedService>  _logger;

    public QueuedHostedService(
        IBackgroundTaskQueue         queue,
        IServiceScopeFactory         scopeFactory,
        ILogger<QueuedHostedService> logger)
    {
        _queue        = queue;
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("QueuedHostedService started — draining background task queue.");

        while (!stoppingToken.IsCancellationRequested)
        {
            Func<IServiceProvider, CancellationToken, Task> workItem;

            try
            {
                workItem = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown — exit the loop quietly.
                break;
            }

            // Each item runs in its own scope so scoped services resolve and are
            // disposed per-operation. One bad item must never kill the host.
            using var scope = _scopeFactory.CreateScope();

            try
            {
                await workItem(scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "QueuedHostedService: work item cancelled by host shutdown.");
                break;
            }
            catch (Exception ex)
            {
                // Swallow — the work item is expected to have already reported a
                // terminal error to its client via IProgressReporter.Error(...).
                // This is the safety net so the drain loop keeps running.
                _logger.LogError(ex,
                    "QueuedHostedService: background work item threw an unhandled exception. " +
                    "The host loop continues.");
            }
        }

        _logger.LogInformation("QueuedHostedService stopping — host shutdown requested.");
    }
}
