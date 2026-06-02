/*
 * File: /ScrumFlix/Services/SeatService.cs
 * Description: Service for querying and atomically reserving seats via the ShowtimeSeat table.
 *
 * Phase 3 — Backend Alignment (#30 / P3-4):
 *   - Implements the canonical seat availability strategy: COUNT(ShowtimeSeat WHERE Status='Available')
 *     rather than the legacy phantom TicketsSold column.
 *   - Atomic reservation uses a conditional UPDATE (Status='Reserved' WHERE Status='Available')
 *     to prevent double-booking without application-level locking.
 *   - ReserveSeatAsync returns ReserveSeatResult indicating success or the specific failure reason.
 *   - FinalizeSeatsAsync called by CartController.Checkout to flip Reserved → Sold and
 *     delete the SeatReservation row atomically with Ticket creation.
 *   - ReleaseExpiredReservationsAsync is called by SeatReservationExpiryService (background worker).
 */

namespace ScrumFlix.Services;

/// <summary>Result codes for a seat reservation attempt.</summary>
public enum ReserveSeatResult
{
    /// <summary>Seat successfully reserved.</summary>
    Success,

    /// <summary>Seat was already Reserved or Sold when the update ran (concurrent booking).</summary>
    AlreadyTaken,

    /// <summary>No ShowtimeSeat row found for the given ShowtimeSeatId.</summary>
    NotFound,

    /// <summary>The showtime associated with this seat is no longer active.</summary>
    ShowtimeInactive
}

/// <summary>
/// Service for querying real-time seat availability and performing atomic seat reservations
/// and finalisations against the canonical ShowtimeSeat table.
/// </summary>
public class SeatService
{
    private readonly AppDbContext _db;
    private static readonly TimeSpan ReservationHoldDuration = TimeSpan.FromMinutes(10);

    /// <summary>Initializes SeatService with the application database context.</summary>
    public SeatService(AppDbContext db) => _db = db;

    // ── Availability queries ────────────────────────────────────────────────

    /// <summary>
    /// Returns the count of seats with Status = 'Available' for the given showtime.
    /// This is the canonical availability computation — does NOT use TicketsSold.
    /// </summary>
    /// <param name="showtimeId">The ShowtimeId to check.</param>
    public Task<int> GetAvailableCountAsync(int showtimeId)
        => _db.ShowtimeSeats
              .CountAsync(ss => ss.ShowtimeId == showtimeId
                             && ss.Status == SeatStatus.Available);

    /// <summary>
    /// Returns all ShowtimeSeats for a given showtime, including physical Seat data,
    /// ordered by row and then column for grid rendering.
    /// </summary>
    public Task<List<ShowtimeSeat>> GetSeatsForShowtimeAsync(int showtimeId)
        => _db.ShowtimeSeats
              .Where(ss => ss.ShowtimeId == showtimeId)
              .Include(ss => ss.Seat)
              .OrderBy(ss => ss.Seat!.RowLabel)
              .ThenBy(ss => ss.Seat!.SeatNumber)
              .AsNoTracking()
              .ToListAsync();

    // ── Reservation ─────────────────────────────────────────────────────────

    /// <summary>
    /// Atomically attempts to reserve a specific seat for the given user.
    /// Uses a conditional UPDATE: Status='Reserved' WHERE Status='Available'.
    /// This prevents double-booking without requiring application-level locking.
    /// On success, inserts a SeatReservation row with a 10-minute hold window.
    /// </summary>
    /// <param name="showtimeSeatId">The ShowtimeSeatId to reserve.</param>
    /// <param name="userId">The UserId placing the hold.</param>
    /// <returns>A <see cref="ReserveSeatResult"/> indicating success or the failure reason.</returns>
    public async Task<ReserveSeatResult> ReserveSeatAsync(int showtimeSeatId, int userId)
    {
        var seat = await _db.ShowtimeSeats
            .Include(ss => ss.Showtime)
            .FirstOrDefaultAsync(ss => ss.ShowtimeSeatId == showtimeSeatId);

        if (seat == null)
            return ReserveSeatResult.NotFound;

        if (seat.Showtime is { IsActive: false })
            return ReserveSeatResult.ShowtimeInactive;

        // Conditional update — only succeeds if Status is still 'Available'
        int rows = await _db.Database.ExecuteSqlRawAsync(
            @"UPDATE ShowtimeSeat
              SET Status = {0}
              WHERE ShowtimeSeatId = {1}
                AND Status = {2}",
            SeatStatus.Reserved,
            showtimeSeatId,
            SeatStatus.Available);

        if (rows == 0)
            return ReserveSeatResult.AlreadyTaken;

        // Insert the timed hold record
        var reservation = new SeatReservation
        {
            ShowtimeSeatId = showtimeSeatId,
            UserId         = userId,
            ReservedAt     = DateTime.UtcNow,
            ExpiresAt      = DateTime.UtcNow.Add(ReservationHoldDuration)
        };

        _db.SeatReservations.Add(reservation);
        await _db.SaveChangesAsync();

        return ReserveSeatResult.Success;
    }

    // ── Finalization ─────────────────────────────────────────────────────────

    /// <summary>
    /// Finalizes a set of reserved seats as Sold, called inside CartController.Checkout
    /// within the same database transaction as Ticket creation.
    /// For each ShowtimeSeatId: flips Status → 'Sold' and deletes its SeatReservation row.
    /// Must be called within an active transaction scope.
    /// </summary>
    /// <param name="showtimeSeatIds">The ShowtimeSeatIds being finalized.</param>
    public async Task FinalizeSeatsAsync(IEnumerable<int> showtimeSeatIds)
    {
        var idList = showtimeSeatIds.ToList();
        if (!idList.Any()) return;

        var seats = await _db.ShowtimeSeats
            .Where(ss => idList.Contains(ss.ShowtimeSeatId))
            .Include(ss => ss.Reservation)
            .ToListAsync();

        foreach (var seat in seats)
        {
            seat.Status = SeatStatus.Sold;

            if (seat.Reservation != null)
                _db.SeatReservations.Remove(seat.Reservation);
        }

        await _db.SaveChangesAsync();
    }

    // ── Expiry cleanup ────────────────────────────────────────────────────────

    /// <summary>
    /// Releases all expired seat reservations, resetting their ShowtimeSeat status back to 'Available'.
    /// Called by SeatReservationExpiryService (IHostedService) on a 60-second polling interval.
    /// </summary>
    /// <returns>The number of expired reservations released.</returns>
    public async Task<int> ReleaseExpiredReservationsAsync()
    {
        var now     = DateTime.UtcNow;
        var expired = await _db.SeatReservations
            .Where(sr => sr.ExpiresAt <= now)
            .Include(sr => sr.ShowtimeSeat)
            .ToListAsync();

        foreach (var reservation in expired)
        {
            if (reservation.ShowtimeSeat != null
                && reservation.ShowtimeSeat.Status == SeatStatus.Reserved)
            {
                reservation.ShowtimeSeat.Status = SeatStatus.Available;
            }

            _db.SeatReservations.Remove(reservation);
        }

        if (expired.Any())
            await _db.SaveChangesAsync();

        return expired.Count;
    }
}
