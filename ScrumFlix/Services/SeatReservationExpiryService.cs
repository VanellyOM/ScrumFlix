/*
 * File: /ScrumFlix/Services/SeatReservationExpiryService.cs
 * Namespace: ScrumFlix.Services
 * Purpose: Background worker that periodically releases expired seat reservations.
 *
 *          Inherits BackgroundService (IHostedService) — runs for the lifetime of the
 *          application on a 60-second polling interval. On each tick it calls
 *          SeatService.ReleaseExpiredReservationsAsync(), which:
 *            1. Finds SeatReservation rows where ExpiresAt <= UtcNow.
 *            2. Resets the corresponding ShowtimeSeat.Status back to 'Available'.
 *            3. Deletes the expired SeatReservation rows.
 *
 *          SCOPED SERVICE PATTERN:
 *          BackgroundService is a Singleton (one instance per app lifetime). SeatService
 *          is Scoped (one instance per request). A singleton cannot directly inject a
 *          scoped service — doing so would pin a single DbContext for the lifetime of
 *          the app, causing stale reads and connection exhaustion.
 *
 *          Resolution: inject IServiceScopeFactory (always Singleton-safe) and create
 *          a fresh IServiceScope + SeatService on every polling tick. The scope is
 *          disposed after each run, releasing the DbContext cleanly.
 *
 *          CANCELLATION:
 *          The CancellationToken provided by BackgroundService is triggered when the
 *          host begins shutting down. The polling delay respects it via
 *          Task.Delay(..., stoppingToken) so the worker stops immediately on shutdown
 *          rather than waiting for the next tick.
 *
 *          ERROR HANDLING:
 *          Exceptions inside the polling loop are caught, logged at Error level, and
 *          swallowed — a transient DB failure on one tick must not crash the host.
 *          The worker resumes on the next scheduled tick automatically.
 *
 *          REGISTRATION (Program.cs):
 *            builder.Services.AddHostedService<SeatReservationExpiryService>();
 *          Add this line alongside the existing SeatService and QrCodeService
 *          registrations. SeatService must already be registered as Scoped — this
 *          service resolves it via IServiceScopeFactory, not directly.
 *
 *          AUDIT NOTE (F-01 / ReservationStatus):
 *          ReleaseExpiredReservationsAsync deletes the SeatReservation row rather than
 *          updating its ReservationStatus to 'Expired' first. This is intentional —
 *          the row is removed to keep the SeatReservation table lean (only active holds
 *          should be present). If an audit trail of expired reservations is required in
 *          a future phase, the strategy should change to:
 *            reservation.ReservationStatus = ReservationStatus.Expired;
 *            await _db.SaveChangesAsync();
 *            _db.SeatReservations.Remove(reservation);   // then archive or soft-delete
 *          For now, expiry events are recorded in the Serilog Logs table via the
 *          structured log event emitted by this worker on each successful release batch.
 *
 * Phase: 2 (missing service — created during Phase 2 audit)
 * Author: ScrumFlix Rebuild Team
 */

namespace ScrumFlix.Services;

/// <summary>
/// Hosted background service that periodically releases expired seat reservations.
/// Polls every 60 seconds and calls SeatService.ReleaseExpiredReservationsAsync()
/// to reset ShowtimeSeat.Status back to 'Available' for any timed-out holds.
/// </summary>
public class SeatReservationExpiryService : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(60);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SeatReservationExpiryService> _logger;

    /// <summary>
    /// Initializes the expiry service with a scope factory and logger.
    /// IServiceScopeFactory is Singleton-safe and is the correct way to consume
    /// Scoped services (SeatService) from a Singleton host (BackgroundService).
    /// </summary>
    public SeatReservationExpiryService(
        IServiceScopeFactory scopeFactory,
        ILogger<SeatReservationExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    /// <summary>
    /// Core execution loop. Runs until the host signals cancellation (app shutdown).
    /// Waits 60 seconds between each expiry sweep.
    /// </summary>
    /// <param name="stoppingToken">
    /// Triggered by the host when the application is shutting down.
    /// Passed to Task.Delay so the worker exits immediately on shutdown
    /// rather than waiting for the next polling tick.
    /// </param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "SeatReservationExpiryService started — polling every {Interval}s.",
            PollingInterval.TotalSeconds);

        // Run until the host requests shutdown
        while (!stoppingToken.IsCancellationRequested)
        {
            // Wait first — avoids hammering the DB immediately at startup before
            // any reservations could possibly have been created.
            try
            {
                await Task.Delay(PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Shutdown was requested during the delay — exit the loop cleanly.
                break;
            }

            await RunExpiryPassAsync(stoppingToken);
        }

        _logger.LogInformation("SeatReservationExpiryService stopped.");
    }

    /// <summary>
    /// Creates a fresh DI scope, resolves SeatService, and calls
    /// ReleaseExpiredReservationsAsync. Catches and logs all exceptions so a
    /// transient DB failure does not crash the host process.
    /// </summary>
    private async Task RunExpiryPassAsync(CancellationToken stoppingToken)
    {
        // A new scope gives us a fresh DbContext — prevents connection exhaustion
        // and stale EF change-tracker state from building up over the app lifetime.
        await using var scope = _scopeFactory.CreateAsyncScope();

        try
        {
            var seatService = scope.ServiceProvider.GetRequiredService<SeatService>();

            var released = await seatService.ReleaseExpiredReservationsAsync();

            if (released > 0)
            {
                // Structured log — queryable in the Serilog MySQL Logs table.
                // {ReleasedCount} can be aggregated in the admin dashboard to
                // track seat hold abandonment rates over time.
                _logger.LogInformation(
                    "SeatReservationExpiryService released {ReleasedCount} expired seat reservation(s).",
                    released);
            }
            else
            {
                // Debug level — not written to MySQL sink in production
                // (Serilog minimum level for MySQL is typically Information).
                // Keeps the Logs table clean when there is nothing to release.
                _logger.LogDebug(
                    "SeatReservationExpiryService tick — no expired reservations found.");
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutdown requested mid-sweep — exit without logging an error.
        }
        catch (Exception ex)
        {
            // Transient failure (DB unavailable, connection timeout, etc.).
            // Log at Error so Serilog's Email sink fires an alert.
            // Worker continues — next tick will retry automatically.
            _logger.LogError(
                ex,
                "SeatReservationExpiryService encountered an error during expiry sweep. " +
                "Will retry on next polling interval ({Interval}s).",
                PollingInterval.TotalSeconds);
        }
    }
}
