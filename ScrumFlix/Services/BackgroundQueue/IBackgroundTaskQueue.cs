/*
 * File:      /ScrumFlix/Services/BackgroundQueue/IBackgroundTaskQueue.cs
 * Namespace: ScrumFlix.Services.BackgroundQueue
 * Purpose:   In-process background work queue contract for the Phase 4.3
 *            background-queue redesign of the long-running admin operations
 *            (TMDb sync, database backup).
 *
 * Architecture:
 *   - Backed by a bounded System.Threading.Channels.Channel<T> — the standard
 *     "queued background tasks" pattern from Microsoft's ASP.NET Core docs
 *     (no Hangfire, no Quartz, no external queue; BCL only).
 *   - A work item is a Func<IServiceProvider, CancellationToken, Task>. The
 *     IServiceProvider handed to the delegate is a PER-ITEM DI scope's provider
 *     (created by QueuedHostedService), so scoped services such as
 *     AppDbContext, ITmdbSyncService, and IDatabaseBackupService resolve
 *     correctly even though the originating HTTP request has already ended.
 *   - The CancellationToken handed to the delegate is the HOST stopping token
 *     (app-pool / graceful-shutdown). OPERATION-LEVEL cancellation (the Cancel
 *     button → ProgressHub.ClientCancel → IProgressReporterFactory.Cancel) is a
 *     separate concern: the controller captures the minted reporter in the work
 *     item closure and passes reporter.CancellationToken to the long-running
 *     call, so user-initiated cancellation flows through the progress framework
 *     untouched. See AdminHomeController.TmdbSyncRun / AdminBackupController.
 *
 * Singleton lifetime (see Program.cs): the channel must outlive any single HTTP
 * request — the triggering request enqueues and returns immediately while the
 * hosted service drains the channel on its own background loop.
 *
 * Somee.com / app-pool-recycle caveat:
 *   This is an IN-PROCESS queue. Queued or in-flight items are lost on an
 *   app-pool recycle. That is NOT a regression versus the previous design,
 *   where the synchronous operation died with the recycled request just the
 *   same. Persistence (a DB-backed queue) is intentionally out of scope.
 *
 * Phase: 4.3 — Background-queue redesign
 */

namespace ScrumFlix.Services.BackgroundQueue;

/// <summary>
/// In-process queue of background work items, drained by
/// <see cref="QueuedHostedService"/>. Register as a singleton.
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>
    /// Enqueues a work item for background execution. Completes once the item
    /// has been written to the channel (which may briefly block if the bounded
    /// channel is full, per <see cref="System.Threading.Channels.BoundedChannelFullMode.Wait"/>).
    /// </summary>
    /// <param name="workItem">
    /// The work to run on the background host. Receives a per-item DI scope's
    /// <see cref="IServiceProvider"/> (for resolving scoped services) and the
    /// host stopping <see cref="CancellationToken"/>. Operation-level
    /// cancellation should be observed via a captured
    /// <see cref="Progress.IProgressReporter.CancellationToken"/>, not this
    /// token.
    /// </param>
    /// <param name="cancellationToken">
    /// Token observed only while waiting to write to a full channel.
    /// </param>
    ValueTask QueueBackgroundWorkItemAsync(
        Func<IServiceProvider, CancellationToken, Task> workItem,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeues the next work item, awaiting asynchronously until one is
    /// available or <paramref name="cancellationToken"/> is signalled. Called
    /// only by <see cref="QueuedHostedService"/>.
    /// </summary>
    ValueTask<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(
        CancellationToken cancellationToken);
}
