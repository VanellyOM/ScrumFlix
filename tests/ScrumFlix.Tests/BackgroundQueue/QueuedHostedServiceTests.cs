/*
 * File:    tests/ScrumFlix.Tests/BackgroundQueue/QueuedHostedServiceTests.cs
 * Purpose: Unit tests for the Phase 4.3 QueuedHostedService drain loop:
 *            - it invokes enqueued work items
 *            - one work item throwing does NOT stop the host (the exception is
 *              swallowed and the loop keeps draining)
 *            - each work item is handed a non-null IServiceProvider (per-item scope)
 *
 * Uses the real BackgroundTaskQueue as the fake-but-faithful queue and a real
 * (empty) ServiceProvider for the scope factory. Synchronisation is via
 * TaskCompletionSource gates awaited with a generous timeout — no fixed-duration
 * sleeps, so the test is fast and not timing-fragile.
 */

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ScrumFlix.Services.BackgroundQueue;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ScrumFlix.Tests.BackgroundQueue;

public class QueuedHostedServiceTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private static QueuedHostedService CreateService(IBackgroundTaskQueue queue, out ServiceProvider root)
    {
        // A real (empty) provider gives us a genuine IServiceScopeFactory so the
        // host's per-item CreateScope() works exactly as in production.
        root = new ServiceCollection().BuildServiceProvider();
        var scopeFactory = root.GetRequiredService<IServiceScopeFactory>();
        return new QueuedHostedService(queue, scopeFactory, NullLogger<QueuedHostedService>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_InvokesEnqueuedWorkItem_WithNonNullScope()
    {
        var queue = new BackgroundTaskQueue(capacity: 4);
        var ran = new TaskCompletionSource<IServiceProvider?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var svc = CreateService(queue, out var root);
        await using var _ = root;

        await svc.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            await queue.QueueBackgroundWorkItemAsync((sp, _) =>
            {
                ran.TrySetResult(sp);
                return Task.CompletedTask;
            }, TestContext.Current.CancellationToken);

            var completed = await Task.WhenAny(ran.Task, Task.Delay(Timeout, TestContext.Current.CancellationToken));
            Assert.Same(ran.Task, completed); // ran, not the timeout
            Assert.NotNull(await ran.Task);   // a real per-item scope provider
        }
        finally
        {
            await svc.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task ExecuteAsync_SwallowsExceptionFromOneItem_AndKeepsDraining()
    {
        var queue   = new BackgroundTaskQueue(capacity: 8);
        var first   = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var third   = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var svc = CreateService(queue, out var root);
        await using var _ = root;

        await svc.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            // 1: completes a gate.
            await queue.QueueBackgroundWorkItemAsync((_, _) =>
            {
                first.TrySetResult();
                return Task.CompletedTask;
            }, TestContext.Current.CancellationToken);

            // 2: throws — must be swallowed by the host loop.
            await queue.QueueBackgroundWorkItemAsync((_, _) =>
                throw new InvalidOperationException("boom"),
                TestContext.Current.CancellationToken);

            // 3: completes a second gate — only reached if the loop survived item 2.
            await queue.QueueBackgroundWorkItemAsync((_, _) =>
            {
                third.TrySetResult();
                return Task.CompletedTask;
            }, TestContext.Current.CancellationToken);

            var both      = Task.WhenAll(first.Task, third.Task);
            var completed = await Task.WhenAny(both, Task.Delay(Timeout, TestContext.Current.CancellationToken));
            Assert.Same(both, completed); // both gates ran despite item 2 throwing
        }
        finally
        {
            await svc.StopAsync(TestContext.Current.CancellationToken);
        }
    }
}
