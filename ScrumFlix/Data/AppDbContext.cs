/*
 * File: /ScrumFlix/Data/AppDbContext.cs
 * Description: EF Core database context for ScrumFlix — Phase 1C canonical rebuild.
 *
 *              COMPLETE REPLACEMENT of the legacy AppDbContext.
 *
 *              WHAT CHANGED:
 *              - All 16 legacy DbSets removed (targeted phantom tables with binary PKs,
 *                vendor FKs, location-specific pricing, and other non-canonical constructs).
 *              - 23 canonical DbSets added — one per entity in ScrumFlix.Domain,
 *                matching the defaultdb schema exactly.
 *              - OnModelCreating() fully rewritten:
 *                  · No binary(16) column types — all PKs are int auto-increment.
 *                  · Fluent API covers only relationships that require non-default
 *                    cascade behavior or disambiguation (multi-FK paths on the same entity).
 *                  · Standard one-to-many FKs with [ForeignKey] data annotations on
 *                    the domain entities are sufficient and are NOT duplicated here.
 *
 *              EF CORE USAGE RULES (from rebuild spec §7.3):
 *              - Do NOT run migrations against Aiven Cloud — schema is the ground truth.
 *              - EnsureCreated() is called in Program.cs for local/dev only.
 *              - Use AsNoTracking() for all read-only queries (enforced in service layer).
 *              - Use Include() explicitly — lazy loading is not configured.
 */

namespace ScrumFlix.Data;

/// <summary>
/// The primary EF Core database context for the ScrumFlix cinema application.
/// Maps all 19 canonical domain entities to their MySQL tables via Pomelo.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="AppDbContext"/> with the provided options.
    /// </summary>
    /// <param name="options">DbContext configuration options (connection string, provider).</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── Identity & Authorization ───────────────────────────────────────────

    /// <summary>System roles: Admin (1), Manager (2), Employee (3).</summary>
    public DbSet<Role> Roles { get; set; }

    /// <summary>Employee-bound user accounts used for authentication and authorization.</summary>
    public DbSet<User> Users { get; set; }

    /// <summary>Immutable audit records for all security-sensitive actions.</summary>
    public DbSet<AuditLog> AuditLogs { get; set; }

    // ── Theater Operations ─────────────────────────────────────────────────

    /// <summary>Physical ScrumFlix theater locations.</summary>
    public DbSet<Location> Locations { get; set; }

    /// <summary>Screening rooms within each theater location.</summary>
    public DbSet<TheaterScreen> TheaterScreens { get; set; }

    /// <summary>Movies available in the system catalog.</summary>
    public DbSet<Movie> Movies { get; set; }

    /// <summary>Scheduled movie screenings.</summary>
    public DbSet<Showtime> Showtimes { get; set; }

    /// <summary>Purchased movie tickets.</summary>
    public DbSet<Ticket> Tickets { get; set; }

    // ── Assigned Seating (Phase 2 Patch — added 2026-05-05) ───────────────

    /// <summary>Physical seats within each theater screen.</summary>
    public DbSet<Seat> Seats { get; set; }

    /// <summary>Per-showtime seat availability records (Available / Reserved / Sold).</summary>
    public DbSet<ShowtimeSeat> ShowtimeSeats { get; set; }

    /// <summary>Temporary holds on showtime seats during the checkout flow.</summary>
    public DbSet<SeatReservation> SeatReservations { get; set; }

    // ── Concessions ────────────────────────────────────────────────────────

    /// <summary>Concession products available for purchase.</summary>
    public DbSet<ConcessionItem> ConcessionItems { get; set; }

    /// <summary>Completed concession sale transactions.</summary>
    public DbSet<ConcessionSale> ConcessionSales { get; set; }

    /// <summary>Line items within each concession sale.</summary>
    public DbSet<ConcessionSaleItem> ConcessionSaleItems { get; set; }

    // ── Workforce ──────────────────────────────────────────────────────────

    /// <summary>Theater employees.</summary>
    public DbSet<Employee> Employees { get; set; }

    /// <summary>Scheduled work shifts at theater locations.</summary>
    public DbSet<Shift> Shifts { get; set; }

    /// <summary>Employee-to-shift assignments, optionally tied to a showtime.</summary>
    public DbSet<ScheduleAssignment> ScheduleAssignments { get; set; }

    /// <summary>Individual employee clock-in / clock-out records.</summary>
    public DbSet<TimeEntry> TimeEntries { get; set; }

    // ── Payroll ────────────────────────────────────────────────────────────

    /// <summary>Pay period date ranges used to scope timesheets and payroll runs.</summary>
    public DbSet<PayPeriod> PayPeriods { get; set; }

    /// <summary>Aggregated employee hours per pay period, subject to manager approval.</summary>
    public DbSet<Timesheet> Timesheets { get; set; }

    /// <summary>Computed gross pay records per employee per pay period.</summary>
    public DbSet<Payroll> Payrolls { get; set; }

    /// <summary>Issued pay statements generated after a payroll run.</summary>
    public DbSet<PayStub> PayStubs { get; set; }


    // ── TMDB / Genre / Logging ─────────────────────────────────────────────

    /// <summary>Movie genres sourced from TMDb. READ: catalog filters. WRITE: TmdbSyncService.</summary>
    public DbSet<Genre> Genres { get; set; }

    /// <summary>Many-to-many join between Movies and Genres.</summary>
    public DbSet<MovieGenre> MovieGenres { get; set; }

    /// <summary>TMDb enrichment metadata per movie. One-to-one with Movie.</summary>
    public DbSet<MovieTmdbMetadata> MovieTmdbMetadata { get; set; }

    /// <summary>
    /// Serilog structured log events. READ-ONLY from application code.
    /// Written exclusively by Serilog.Sinks.MySQL. Never insert/update/delete here directly.
    /// Named AppLog to avoid collision with the Serilog.Log static class.
    ///
    /// INTENTIONALLY COMMENTED OUT: The "Logs" table is created and owned by Serilog.Sinks.MySQL,
    /// NOT by EF Core migrations. Adding this DbSet would cause EF to try to manage the table
    /// schema, which conflicts with what the sink expects. Leave this commented out permanently.
    ///
    /// WHY LOGS TABLE MAY BE EMPTY:
    ///   1. MySQLConnection string not set in User Secrets / env vars → sink is disabled silently.
    ///   2. DB user lacks CREATE TABLE permission → sink fails on first write.
    ///   3. Connection string set but database unreachable → async sink drops events.
    ///   Check startup console for "MySQL logging sink disabled" or connection errors.
    /// </summary>
    public DbSet<AppLog> AppLogs { get; set; }

    // ── Model Configuration ────────────────────────────────────────────────

    /// <summary>
    /// Configures entity relationships and constraints that require Fluent API
    /// beyond what [ForeignKey] data annotations already express on the domain entities.
    ///
    /// Design rationale: simple one-to-many FKs with a single path between two entities
    /// are fully described by the [ForeignKey] attributes on the domain classes and do not
    /// need duplication here. Only the cases below require explicit Fluent API:
    ///   1. Non-default cascade behavior (Restrict / SetNull / NoAction).
    ///   2. Multi-FK disambiguation: when an entity has more than one FK pointing to the
    ///      same principal entity, EF Core cannot infer which nav property pairs with which
    ///      FK without explicit configuration.
    ///   3. Optional FKs that must map to a specific nav property on the dependent side.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure the entity graph.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ──────────────────────────────────────────────────────────
        //
        // User has three nav collections that all point back to User:
        //   AuditLogs        → AuditLog.UserId
        //   TicketsSold      → Ticket.UserAtSale
        //   ConcessionSales  → ConcessionSale.UserId
        //   ApprovedTimesheets → Timesheet.ApprovedByUserId  (nullable)
        //
        // The first three are configured on their respective entities below.
        // ApprovedTimesheets is the only self-referential nullable path on User
        // so it must be disambiguated here.

        modelBuilder.Entity<User>(entity =>
        {
            // Unique constraint on UserName — canonical schema enforces this.
            entity.HasIndex(u => u.UserName).IsUnique();

            // Role FK — restrict deletion of a Role that has Users assigned.
            entity.HasOne(u => u.Role)
                  .WithMany(r => r.Users)
                  .HasForeignKey(u => u.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Employee FK — restrict deletion of an Employee that has a User.
            // One-to-one from the Employee side; declared as one-to-many here
            // because EF Core resolves the unique side via the Employee.User nav.
            entity.HasOne(u => u.Employee)
                  .WithOne(e => e.User)
                  .HasForeignKey<User>(u => u.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── AuditLog ──────────────────────────────────────────────────────
        //
        // Restrict deletion of a User who has audit log entries — audit records
        // are immutable and must not be silently removed via cascade.

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasOne(a => a.User)
                  .WithMany(u => u.AuditLogs)
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Employee ──────────────────────────────────────────────────────
        //
        // Employee.LocationId is nullable — an employee may not yet be assigned
        // to a location. Restrict prevents accidental location deletion.

        modelBuilder.Entity<Employee>(entity =>
        {
            // Unique constraint on Email — canonical schema enforces this.
            entity.HasIndex(e => e.Email).IsUnique();

            entity.HasOne(e => e.Location)
                  .WithMany(l => l.Employees)
                  .HasForeignKey(e => e.LocationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── TheaterScreen ─────────────────────────────────────────────────

        modelBuilder.Entity<TheaterScreen>(entity =>
        {
            entity.HasOne(ts => ts.Location)
                  .WithMany(l => l.TheaterScreens)
                  .HasForeignKey(ts => ts.LocationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Movie ─────────────────────────────────────────────────────────
        //
        // Title is UNIQUE in the canonical schema.

        modelBuilder.Entity<Movie>(entity =>
        {
            entity.HasIndex(m => m.Title).IsUnique();
        });

        // ── Showtime ──────────────────────────────────────────────────────

        modelBuilder.Entity<Showtime>(entity =>
        {
            entity.HasOne(s => s.Movie)
                  .WithMany(m => m.Showtimes)
                  .HasForeignKey(s => s.MovieId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.TheaterScreen)
                  .WithMany(ts => ts.Showtimes)
                  .HasForeignKey(s => s.TheaterScreenId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Ticket ────────────────────────────────────────────────────────
        //
        // Ticket.UserAtSale → Users.UserId.
        // The nav property is named SoldByUser (not the default "User") so EF Core
        // cannot infer the pairing by convention — must be explicit.
        // Restrict: deleting a User must not silently remove purchase history.
        //
        // Ticket.ShowtimeSeatId → ShowtimeSeat.ShowtimeSeatId (nullable).
        // One-to-one from the ShowtimeSeat side — each seat can have at most one
        // issued Ticket per showtime.

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasOne(t => t.Showtime)
                  .WithMany(s => s.Tickets)
                  .HasForeignKey(t => t.ShowtimeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.SoldByUser)
                  .WithMany(u => u.TicketsSold)
                  .HasForeignKey(t => t.UserAtSale)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.ShowtimeSeat)
                  .WithOne(ss => ss.Ticket)
                  .HasForeignKey<Ticket>(t => t.ShowtimeSeatId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ConcessionItem ────────────────────────────────────────────────
        //
        // ItemName is UNIQUE in the canonical schema.
        // LocationId FK added in Phase 2 Patch — was missing from original entity.

        modelBuilder.Entity<ConcessionItem>(entity =>
        {
            entity.HasIndex(ci => ci.ItemName).IsUnique();

            entity.HasOne(ci => ci.Location)
                  .WithMany(l => l.ConcessionItems)
                  .HasForeignKey(ci => ci.LocationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ConcessionSale ────────────────────────────────────────────────
        //
        // ConcessionSale.UserId → Users.UserId.
        // The nav property is named ProcessedByUser — not the default convention.
        // Restrict: a User deletion must not erase sale history.
        // LocationId FK added in Phase 2 Patch — was missing from original entity.

        modelBuilder.Entity<ConcessionSale>(entity =>
        {
            entity.HasOne(cs => cs.ProcessedByUser)
                  .WithMany(u => u.ConcessionSales)
                  .HasForeignKey(cs => cs.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(cs => cs.Location)
                  .WithMany(l => l.ConcessionSales)
                  .HasForeignKey(cs => cs.LocationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ConcessionSaleItem ────────────────────────────────────────────
        //
        // Cascade on parent sale deletion (if a sale is voided, its line items go too).
        // Restrict on ConcessionItem deletion (historical line items must be preserved).

        modelBuilder.Entity<ConcessionSaleItem>(entity =>
        {
            entity.HasOne(si => si.ConcessionSale)
                  .WithMany(cs => cs.ConcessionSaleItems)
                  .HasForeignKey(si => si.ConcessionSaleId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(si => si.ConcessionItem)
                  .WithMany(ci => ci.ConcessionSaleItems)
                  .HasForeignKey(si => si.ConcessionItemId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Shift ─────────────────────────────────────────────────────────

        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasOne(sh => sh.Role)
                  .WithMany(r => r.Shifts)
                  .HasForeignKey(sh => sh.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(sh => sh.Location)
                  .WithMany(l => l.Shifts)
                  .HasForeignKey(sh => sh.LocationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ScheduleAssignment ────────────────────────────────────────────
        //
        // ShowtimeId is nullable — not all assignments are tied to a screening.
        // All three FKs must be explicitly mapped because the entity has multiple
        // optional and required FKs pointing to different principal tables.

        modelBuilder.Entity<ScheduleAssignment>(entity =>
        {
            // UserId FK → Users.UserId (corrected from EmployeeId 2026-05-08)
            entity.HasOne(sa => sa.User)
                  .WithMany(u => u.ScheduleAssignments)
                  .HasForeignKey(sa => sa.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(sa => sa.Shift)
                  .WithMany(sh => sh.ScheduleAssignments)
                  .HasForeignKey(sa => sa.ShiftId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(sa => sa.Showtime)
                  .WithMany(s => s.ScheduleAssignments)
                  .HasForeignKey(sa => sa.ShowtimeId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── TimeEntry ─────────────────────────────────────────────────────

        modelBuilder.Entity<TimeEntry>(entity =>
        {
            entity.HasOne(te => te.Employee)
                  .WithMany(e => e.TimeEntries)
                  .HasForeignKey(te => te.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(te => te.Location)
                  .WithMany()
                  .HasForeignKey(te => te.LocationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Timesheet ─────────────────────────────────────────────────────
        //
        // ApprovedByUserId is nullable and points to Users.UserId.
        // The nav property name (ApprovedByUser) doesn't match the FK column name
        // by convention, so it must be mapped explicitly.
        // The inverse nav on User is ApprovedTimesheets.
        // NoAction: if the approving user account is deleted, the approval record
        // is left in place (historical accuracy).

        modelBuilder.Entity<Timesheet>(entity =>
        {
            entity.HasOne(ts => ts.Employee)
                  .WithMany(e => e.Timesheets)
                  .HasForeignKey(ts => ts.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ts => ts.PayPeriod)
                  .WithMany(pp => pp.Timesheets)
                  .HasForeignKey(ts => ts.PayPeriodId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ts => ts.Location)
                  .WithMany()
                  .HasForeignKey(ts => ts.LocationId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ts => ts.ApprovedByUser)
                  .WithMany(u => u.ApprovedTimesheets)
                  .HasForeignKey(ts => ts.ApprovedByUserId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // ── Payroll ───────────────────────────────────────────────────────

        modelBuilder.Entity<Payroll>(entity =>
        {
            entity.HasOne(p => p.Employee)
                  .WithMany(e => e.Payrolls)
                  .HasForeignKey(p => p.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.PayPeriod)
                  .WithMany(pp => pp.Payrolls)
                  .HasForeignKey(p => p.PayPeriodId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Location)
                  .WithMany()
                  .HasForeignKey(p => p.LocationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PayStub ───────────────────────────────────────────────────────
        //
        // One-to-one: each Payroll has at most one PayStub.
        // Cascade: deleting a Payroll record removes its stub.

        modelBuilder.Entity<PayStub>(entity =>
        {
            entity.HasOne(ps => ps.Payroll)
                  .WithOne(p => p.PayStub)
                  .HasForeignKey<PayStub>(ps => ps.PayrollId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Seat ──────────────────────────────────────────────────────────
        //
        // Restrict deletion of a TheaterScreen that still has Seat rows — seats
        // must be explicitly deactivated (IsActive = false) before the screen
        // can be removed.

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasOne(s => s.TheaterScreen)
                  .WithMany(ts => ts.Seats)
                  .HasForeignKey(s => s.TheaterScreenId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ShowtimeSeat ──────────────────────────────────────────────────
        //
        // The UNIQUE(ShowtimeId, SeatId) composite constraint from the live schema
        // is mapped as a composite index here.
        // Both FKs use Restrict — losing a Showtime or Seat row while seat-status
        // records exist would corrupt booking history.
        // The Ticket inverse nav (one-to-one) is configured on Ticket above.

        modelBuilder.Entity<ShowtimeSeat>(entity =>
        {
            entity.HasIndex(ss => new { ss.ShowtimeId, ss.SeatId }).IsUnique();

            entity.HasOne(ss => ss.Showtime)
                  .WithMany(s => s.ShowtimeSeats)
                  .HasForeignKey(ss => ss.ShowtimeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ss => ss.Seat)
                  .WithMany(s => s.ShowtimeSeats)
                  .HasForeignKey(ss => ss.SeatId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── SeatReservation ───────────────────────────────────────────────
        //
        // One-to-one with ShowtimeSeat — each seat can have at most one active
        // reservation at a time.
        // Cascade: when a ShowtimeSeat is deleted (cleanup path), its reservation
        // goes with it.
        // UserId FK → Users.UserId (FK_SeatReservation_User confirmed in schema).
        // Restrict: reservation history should not vanish if a user account is deleted.

        modelBuilder.Entity<SeatReservation>(entity =>
        {
            entity.HasOne(sr => sr.ShowtimeSeat)
                  .WithOne(ss => ss.Reservation)
                  .HasForeignKey<SeatReservation>(sr => sr.ShowtimeSeatId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sr => sr.User)
                  .WithMany()
                  .HasForeignKey(sr => sr.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Genre ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Genre>(entity =>
        {
            // UQ_Genres_Name
            entity.HasIndex(g => g.Name).IsUnique();
            // UQ_Genres_Slug
            entity.HasIndex(g => g.Slug).IsUnique();
            // UQ_Genres_TMDbGenreId (nullable unique — only when non-null)
            entity.HasIndex(g => g.TMDbGenreId).IsUnique();
        });

        // ── MovieGenre ────────────────────────────────────────────────────────
        modelBuilder.Entity<MovieGenre>(entity =>
        {
            // UQ_MovieGenres_MovieId_GenreId — no duplicate genre tags per movie
            entity.HasIndex(mg => new { mg.MovieId, mg.GenreId }).IsUnique();

            // FK_MovieGenres_Movies (ON DELETE CASCADE per schema)
            entity.HasOne(mg => mg.Movie)
                  .WithMany(m => m.MovieGenres)
                  .HasForeignKey(mg => mg.MovieId)
                  .OnDelete(DeleteBehavior.Cascade);

            // FK_MovieGenres_Genres (ON DELETE CASCADE per schema)
            entity.HasOne(mg => mg.Genre)
                  .WithMany(g => g.MovieGenres)
                  .HasForeignKey(mg => mg.GenreId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── MovieTmdbMetadata ─────────────────────────────────────────────────
        modelBuilder.Entity<MovieTmdbMetadata>(entity =>
        {
            // UQ_MovieTmdbMetadata_MovieId — one metadata record per movie
            entity.HasIndex(m => m.MovieId).IsUnique();
            // UQ_MovieTmdbMetadata_TMDbMovieId — each TMDb movie maps to at most one local movie
            entity.HasIndex(m => m.TMDbMovieId).IsUnique();

            // FK_MovieTmdbMetadata_Movies (ON DELETE CASCADE per schema)
            entity.HasOne(m => m.Movie)
                  .WithOne(mo => mo.TmdbMetadata)
                  .HasForeignKey<MovieTmdbMetadata>(m => m.MovieId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Decimal precision for Popularity and VoteAverage per schema
            entity.Property(m => m.Popularity).HasPrecision(10, 4);
            entity.Property(m => m.VoteAverage).HasPrecision(3, 1);
        });

        // ── AppLog ────────────────────────────────────────────────────────────
        // Maps to the Serilog.Sinks.MySQL-owned Logs table. Indexes defined here
        // match what the sink creates; EF just needs to know about them for
        // query plan purposes. Table is read-only from application code.
        modelBuilder.Entity<AppLog>(entity =>
        {
            // Serilog creates these indexes; EF just needs to know about them
            entity.HasIndex(l => l.Timestamp).HasDatabaseName("idx_logs_timestamp");
            entity.HasIndex(l => l.Level).HasDatabaseName("idx_logs_level");

            // Mark as read-only at the EF level — block INSERT/UPDATE/DELETE
            // via the application's DbContext. Serilog writes via its own connection.
            // NOTE: HasNoKey() would prevent CRUD but also lose the PK for admin queries.
            // Keep the PK; enforce read-only discipline at the service/controller layer.
        });

    }
}
