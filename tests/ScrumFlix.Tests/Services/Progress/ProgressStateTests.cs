/*
 * File:    tests/ScrumFlix.Tests/Services/Progress/ProgressStateTests.cs
 * Purpose: Unit tests for the Phase 4.0 shared progress framework contract:
 *            - ProgressState.ComputePercent clamping and divide-by-zero safety
 *            - InProgress factory percent math
 *            - Completed factory terminal flags + succeeded/skipped/failed accounting
 *            - ErrorState factory terminal flags
 *
 * Pure unit tests — no database, no EF, no SignalR.
 */

using ScrumFlix.Services.Progress;
using Xunit;

namespace ScrumFlix.Tests;

public class ProgressStateTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(5, 10, 50)]
    [InlineData(10, 10, 100)]
    [InlineData(3, 7, 43)]    // 42.857... rounds to 43
    [InlineData(-1, 10, 0)]   // negative current clamps to 0
    [InlineData(20, 10, 100)] // over-total clamps to 100
    public void ComputePercent_ClampsAndRounds(int current, int total, int expected)
    {
        Assert.Equal(expected, ProgressState.ComputePercent(current, total));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ComputePercent_ZeroOrNegativeTotal_ReturnsZero(int total)
    {
        Assert.Equal(0, ProgressState.ComputePercent(5, total));
    }

    [Fact]
    public void InProgress_ComputesPercentFromCurrentAndTotal()
    {
        var state = ProgressState.InProgress(
            operationId:   "op-1",
            operationName: "Test Op",
            status:        "Working…",
            current:       3,
            total:         12,
            succeeded:     2,
            skipped:       1,
            failed:        0);

        Assert.Equal("op-1", state.OperationId);
        Assert.Equal("Test Op", state.OperationName);
        Assert.Equal(3, state.Current);
        Assert.Equal(12, state.Total);
        Assert.Equal(25, state.Percent); // 3/12 = 25%
        Assert.Equal(2, state.Succeeded);
        Assert.Equal(1, state.Skipped);
        Assert.Equal(0, state.Failed);
        Assert.False(state.IsComplete);
        Assert.False(state.IsError);
    }

    [Fact]
    public void Completed_SetsTerminalFlagsAndFullPercent()
    {
        var state = ProgressState.Completed(
            operationId:   "op-2",
            operationName: "TMDb Sync",
            total:         10,
            succeeded:     8,
            skipped:       2,
            failed:        0,
            summary:       "8 synced, 2 skipped, 0 failed.");

        Assert.True(state.IsComplete);
        Assert.False(state.IsError);
        Assert.Equal(100, state.Percent);
        Assert.Equal(10, state.Current);
        Assert.Equal(10, state.Total);
        Assert.Equal(8, state.Succeeded);
        Assert.Equal(2, state.Skipped);
        Assert.Equal(0, state.Failed);
        Assert.Equal("8 synced, 2 skipped, 0 failed.", state.CompletionSummary);

        // Succeeded + Skipped + Failed should reconcile to Total for a clean run.
        Assert.Equal(state.Total, state.Succeeded + state.Skipped + state.Failed);
    }

    [Fact]
    public void Completed_WithFailures_AccountingStillReconciles()
    {
        var state = ProgressState.Completed(
            operationId:   "op-3",
            operationName: "TMDb Sync",
            total:         5,
            succeeded:     3,
            skipped:       1,
            failed:        1);

        Assert.Equal(5, state.Total);
        Assert.Equal(state.Total, state.Succeeded + state.Skipped + state.Failed);
        Assert.True(state.IsComplete);
    }

    [Fact]
    public void ErrorState_SetsIsErrorAndPreservesProgress()
    {
        var state = ProgressState.ErrorState(
            operationId:   "op-4",
            operationName: "Database Backup",
            message:       "Connection lost.",
            current:       4,
            total:         10,
            succeeded:     3,
            skipped:       0,
            failed:        1);

        Assert.True(state.IsError);
        Assert.False(state.IsComplete);
        Assert.Equal(40, state.Percent); // 4/10
        Assert.Equal("Connection lost.", state.Status);
        Assert.Equal("Connection lost.", state.CompletionSummary);
    }

    [Fact]
    public void ErrorState_DefaultsToZeroProgress()
    {
        var state = ProgressState.ErrorState(
            operationId:   "op-5",
            operationName: "Database Backup",
            message:       "Failed before starting.");

        Assert.True(state.IsError);
        Assert.Equal(0, state.Percent);
        Assert.Equal(0, state.Current);
        Assert.Equal(0, state.Total);
    }
}
