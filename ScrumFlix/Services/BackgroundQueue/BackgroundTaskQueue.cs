/*
 * File:      /ScrumFlix/Services/BackgroundQueue/BackgroundTaskQueue.cs
 * Namespace: ScrumFlix.Services.BackgroundQueue
 * Purpose:   Default IBackgroundTaskQueue implementation backed by a bounded
 *            System.Threading.Channels.Channel (Phase 4.3).
 *
 * Architecture:
 *   - Bounded channel (capacity supplied at construction, e.g. 10) with
 *     BoundedChannelFullMode.Wait: if a producer enqueues while the channel is
 *     full, the write awaits backpressure rather than throwing or dropping.
 *     For ScrumFlix this is effectively a safety valve — only one Admin runs
 *     one long operation at a time in practice, so the channel is rarely more
 *     than one item deep.
 *   - SingleReader = true: exactly one QueuedHostedService drains the channel.
 *     SingleWriter = false: multiple controller actions (TMDb sync, backup) may
 *     enqueue concurrently.
 *
 * BCL only — no NuGet packages. System.Threading.Channels ships with the BCL.
 *
 * Phase: 4.3 — Background-queue redesign
 */

using System.Threading.Channels;

namespace ScrumFlix.Services.BackgroundQueue;

/// <inheritdoc />
public sealed class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _queue;

    /// <summary>
    /// Creates a bounded background-task queue.
    /// </summary>
    /// <param name="capacity">
    /// Maximum number of queued-but-not-yet-running work items. When the queue
    /// is full, <see cref="QueueBackgroundWorkItemAsync"/> awaits a free slot
    /// (<see cref="BoundedChannelFullMode.Wait"/>) instead of throwing.
    /// </param>
    public BackgroundTaskQueue(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        var options = new BoundedChannelOptions(capacity)
        {
            FullMode     = BoundedChannelFullMode.Wait,
            SingleReader = true,   // one QueuedHostedService drains the channel
            SingleWriter = false,  // multiple controller actions may enqueue
        };

        _queue = Channel.CreateBounded<Func<IServiceProvider, CancellationToken, Task>>(options);
    }

    /// <inheritdoc />
    public async ValueTask QueueBackgroundWorkItemAsync(
        Func<IServiceProvider, CancellationToken, Task> workItem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        await _queue.Writer.WriteAsync(workItem, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(
        CancellationToken cancellationToken)
        => await _queue.Reader.ReadAsync(cancellationToken);
}
