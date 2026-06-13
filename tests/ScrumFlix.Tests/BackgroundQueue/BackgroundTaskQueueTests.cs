/*
 * File:    tests/ScrumFlix.Tests/BackgroundQueue/BackgroundTaskQueueTests.cs
 * Purpose: Unit tests for the Phase 4.3 Channel-based background queue:
 *            - FIFO enqueue/dequeue ordering
 *            - bounded-channel backpressure (Wait mode) without timing hacks
 *            - constructor + argument guards
 *
 * Pure unit tests — no database, no SignalR, no Task.Delay-based timing
 * assumptions. Backpressure is asserted via ValueTask completion state, which
 * is deterministic for a bounded channel.
 */

using ScrumFlix.Services.BackgroundQueue;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ScrumFlix.Tests.BackgroundQueue;

public class BackgroundTaskQueueTests
{
    private static Func<IServiceProvider, CancellationToken, Task> NoOp =>
        (_, _) => Task.CompletedTask;

    [Fact]
    public void Constructor_RejectsNonPositiveCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BackgroundTaskQueue(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BackgroundTaskQueue(-1));
    }

    [Fact]
    public async Task QueueBackgroundWorkItemAsync_RejectsNullWorkItem()
    {
        var queue = new BackgroundTaskQueue(capacity: 4);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            queue.QueueBackgroundWorkItemAsync(null!, TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task EnqueueDequeue_PreservesFifoOrder()
    {
        var queue = new BackgroundTaskQueue(capacity: 8);
        var ran   = new List<int>();

        for (var i = 1; i <= 3; i++)
        {
            var id = i;
            await queue.QueueBackgroundWorkItemAsync((_, _) =>
            {
                ran.Add(id);
                return Task.CompletedTask;
            }, TestContext.Current.CancellationToken);
        }

        for (var i = 0; i < 3; i++)
        {
            var work = await queue.DequeueAsync(TestContext.Current.CancellationToken);
            await work(null!, TestContext.Current.CancellationToken);
        }

        Assert.Equal(new[] { 1, 2, 3 }, ran);
    }

    [Fact]
    public async Task BoundedChannel_BlocksProducerWhenFull_ThenResumesAfterDequeue()
    {
        // Capacity 1: the first write completes synchronously; the second must
        // wait (BoundedChannelFullMode.Wait) until a slot frees up.
        var queue = new BackgroundTaskQueue(capacity: 1);

        var first = queue.QueueBackgroundWorkItemAsync(NoOp, TestContext.Current.CancellationToken);
        Assert.True(first.IsCompleted, "First enqueue should complete immediately (slot available).");
        await first;

        var second = queue.QueueBackgroundWorkItemAsync(NoOp, TestContext.Current.CancellationToken);
        Assert.False(second.IsCompleted, "Second enqueue should block while the channel is full.");

        // Draining one item frees the slot and lets the pending enqueue complete.
        _ = await queue.DequeueAsync(TestContext.Current.CancellationToken);
        await second; // must now complete without hanging
    }

    [Fact]
    public async Task DequeueAsync_HonoursCancellation_WhenEmpty()
    {
        var queue = new BackgroundTaskQueue(capacity: 2);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            queue.DequeueAsync(cts.Token).AsTask());
    }
}
