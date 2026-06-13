/*
 * File:      /ScrumFlix/Services/Progress/ProgressState.cs
 * Namespace: ScrumFlix.Services.Progress
 * Purpose:   Single over-the-wire contract for the generic long-running-operation
 *            progress framework (Phase 4.0).
 *
 * Architecture:
 *   - One event ("ProgressUpdate") carries this entire object so the client
 *     subscribes to a single SignalR event per operation, mirroring the
 *     existing TmdbSyncProgress payload shape but generalised for reuse by
 *     TMDb sync, database backup, and future long-running admin operations.
 *   - Percent is the primary driver for sf-spinner.js; Current/Total are
 *     provided for status-line display (e.g. "Table 4 of 12").
 *   - Succeeded/Skipped/Failed mirror the TmdbSyncProgressReport counters so
 *     the TMDb migration (Phase 4.1) is a straight field-for-field mapping.
 *   - IsComplete / IsError are terminal-state flags. A consumer that sees
 *     either flag set should stop listening for further updates for this
 *     OperationId.
 *
 * Phase: 4.0 — Shared progress framework
 */

namespace ScrumFlix.Services.Progress;

/// <summary>
/// Over-the-wire progress payload for a single long-running admin operation
/// (TMDb sync, database backup, etc.). Broadcast via
/// <see cref="ScrumFlix.Hubs.ProgressHub"/> to all clients subscribed to
/// <see cref="OperationId"/>'s SignalR group.
/// </summary>
/// <param name="OperationId">
/// Unique identifier for this operation run (e.g. a GUID string). Doubles as
/// the SignalR group name and the key used by
/// <see cref="IProgressReporterFactory"/> for its cancellation registry.
/// </param>
/// <param name="OperationName">
/// Human-readable operation name for display (e.g. "TMDb Sync", "Database Backup").
/// </param>
/// <param name="Status">
/// Human-readable status line for the spinner (e.g. "Syncing movie 4 of 12…",
/// "Capturing schema for Movies…").
/// </param>
/// <param name="Percent">Overall completion, 0–100.</param>
/// <param name="Current">
/// The current unit of work (1-based), e.g. the index of the table/movie
/// currently being processed. Used for "X of Y" status display.
/// </param>
/// <param name="Total">Total units of work (denominator for Percent and Current).</param>
/// <param name="Succeeded">Running total of successfully processed units.</param>
/// <param name="Skipped">Running total of skipped units.</param>
/// <param name="Failed">Running total of failed units.</param>
/// <param name="IsComplete">
/// <see langword="true"/> once the operation has finished successfully.
/// A terminal state — no further updates for this OperationId will follow.
/// </param>
/// <param name="IsError">
/// <see langword="true"/> if the operation terminated due to an error.
/// A terminal state — no further updates for this OperationId will follow.
/// </param>
/// <param name="CompletionSummary">
/// Optional human-readable summary shown once <see cref="IsComplete"/> or
/// <see cref="IsError"/> is set (e.g. "12 synced, 1 skipped, 0 failed.").
/// </param>
public sealed record ProgressState(
    string  OperationId,
    string  OperationName,
    string  Status,
    int     Percent,
    int     Current,
    int     Total,
    int     Succeeded,
    int     Skipped,
    int     Failed,
    bool    IsComplete        = false,
    bool    IsError           = false,
    string? CompletionSummary = null)
{
    /// <summary>
    /// Computes a clamped 0–100 percentage from <paramref name="current"/> /
    /// <paramref name="total"/>. Returns 0 when <paramref name="total"/> is
    /// zero or negative (avoids divide-by-zero for empty operations).
    /// </summary>
    public static int ComputePercent(int current, int total)
    {
        if (total <= 0) return 0;

        var pct = (int)Math.Round(current * 100.0 / total);
        return Math.Min(Math.Max(pct, 0), 100);
    }

    /// <summary>
    /// Convenience factory for an in-progress update where Percent is derived
    /// from <paramref name="current"/> and <paramref name="total"/>.
    /// </summary>
    public static ProgressState InProgress(
        string operationId,
        string operationName,
        string status,
        int    current,
        int    total,
        int    succeeded = 0,
        int    skipped   = 0,
        int    failed    = 0)
        => new(
            OperationId:   operationId,
            OperationName: operationName,
            Status:        status,
            Percent:       ComputePercent(current, total),
            Current:       current,
            Total:         total,
            Succeeded:     succeeded,
            Skipped:       skipped,
            Failed:        failed);

    /// <summary>
    /// Convenience factory for a terminal "complete" update at 100%.
    /// </summary>
    public static ProgressState Completed(
        string  operationId,
        string  operationName,
        int     total,
        int     succeeded,
        int     skipped,
        int     failed,
        string? summary = null)
        => new(
            OperationId:       operationId,
            OperationName:     operationName,
            Status:            summary ?? "Complete!",
            Percent:           100,
            Current:           total,
            Total:             total,
            Succeeded:         succeeded,
            Skipped:           skipped,
            Failed:            failed,
            IsComplete:        true,
            CompletionSummary: summary);

    /// <summary>
    /// Convenience factory for a terminal "error" update. Percent is left at
    /// whatever progress had been made so the spinner can show where the
    /// operation failed, with <see cref="IsError"/> driving the visual state.
    /// </summary>
    public static ProgressState ErrorState(
        string operationId,
        string operationName,
        string message,
        int    current = 0,
        int    total   = 0,
        int    succeeded = 0,
        int    skipped   = 0,
        int    failed    = 0)
        => new(
            OperationId:       operationId,
            OperationName:     operationName,
            Status:            message,
            Percent:           ComputePercent(current, total),
            Current:           current,
            Total:             total,
            Succeeded:         succeeded,
            Skipped:           skipped,
            Failed:            failed,
            IsError:           true,
            CompletionSummary: message);
}
