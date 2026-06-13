/*
 * File:      /ScrumFlix/Services/DatabaseBackupService.cs
 * Namespace: ScrumFlix.Services
 * Purpose:   Produces a .zip archive containing JSON and INSERT SQL
 *            representations of selected ScrumFlix database tables.
 *
 * Architecture:
 *   - Scoped service (one instance per HTTP request — safe because it wraps
 *     a scoped AppDbContext).
 *   - Each table is serialised to two in-memory streams: a JSON stream and a
 *     SQL INSERT stream. Both are added to a single ZipArchive written to a
 *     MemoryStream. The MemoryStream is returned as the download payload.
 *   - AsNoTracking() is used on every query — backup reads must never dirty
 *     EF's identity map or add change-tracker overhead.
 *   - Large tables (ShowtimeSeat, AppLogs) are paged in chunks of PageSize
 *     rows to prevent OOM on Somee.com shared hosting.
 *   - The Users table strips UserPassword and PasswordHash before serialisation.
 *     Backup files must not be credential distribution vectors.
 *   - Navigation properties are not included — only scalar columns that map
 *     directly to DB columns. This ensures the output is importable without
 *     circular-reference issues and produces clean INSERT statements.
 *
 * Output layout inside the .zip:
 *   scrumflix_backup_20260610_213000/
 *     manifest.json              — metadata: timestamp, table list, row counts
 *     json/
 *       Movies.json
 *       Showtime.json
 *       ...
 *     sql/
 *       00_import_order.sql      — SET FOREIGN_KEY_CHECKS=0; source each file; SET=1;
 *       01_Movies.sql
 *       02_Showtime.sql
 *       ...
 *
 * System.Text.Json is used for JSON serialisation — already a BCL dependency,
 * no additional NuGet package needed.
 */

using System.IO.Compression;
using System.Text;
using System.Text.Json;
using ScrumFlix.Infrastructure;
using ScrumFlix.Services.Backup;
using ScrumFlix.Services.Progress;

namespace ScrumFlix.Services;

/// <inheritdoc />
public sealed class DatabaseBackupService : IDatabaseBackupService
{
    // ── Constants ──────────────────────────────────────────────────────────

    /// <summary>Row page size for large-table queries. 5 K rows ≈ safe for Somee.</summary>
    private const int PageSize = 5_000;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,   // keep PascalCase to match C# property names
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    // ── Table registry ─────────────────────────────────────────────────────

    /// <summary>
    /// Ordered list of all tables available for backup.
    /// Import order matters — tables with FK dependencies must appear after
    /// the tables they reference (same order as a correct SQL reseed script).
    /// </summary>
    private static readonly IReadOnlyList<BackupTableDescriptor> TableRegistry =
    [
        // ── System ────────────────────────────────────────────────────────
        new() { Key="Roles",             DisplayName="Roles",               Category="System",    Description="Staff role definitions (Admin, Manager, Employee)." },
        new() { Key="Users",             DisplayName="Users",               Category="System",    Description="Staff user accounts. Passwords are excluded from the export." },
        new() { Key="AuditLog",          DisplayName="Audit log",           Category="System",    Description="Immutable audit trail of all admin actions." },
        new() { Key="Logs",              DisplayName="Application logs",    Category="System",    Description="Serilog structured log events.",
                IsLargeTable=true, ExcludedByDefault=true },

        // ── Cinema ────────────────────────────────────────────────────────
        new() { Key="Location",          DisplayName="Locations",           Category="Cinema",    Description="Theater locations with name, address, and timezone." },
        new() { Key="TheaterScreen",     DisplayName="Screens",             Category="Cinema",    Description="Individual screens within each location." },
        new() { Key="Genres",            DisplayName="Genres",              Category="Cinema",    Description="Movie genre lookup values." },
        new() { Key="Movies",            DisplayName="Movies",              Category="Cinema",    Description="Movie catalog — title, rating, runtime, description." },
        new() { Key="MovieGenres",       DisplayName="Movie genres",        Category="Cinema",    Description="Many-to-many join between movies and genres." },
        new() { Key="MovieTmdbMetadata", DisplayName="TMDb metadata",       Category="Cinema",    Description="Poster paths, trailers, and TMDb sync timestamps." },
        new() { Key="Showtime",          DisplayName="Showtimes",           Category="Cinema",    Description="Scheduled showings with screen, time, price, and capacity." },
        new() { Key="Seat",              DisplayName="Seats",               Category="Cinema",    Description="Physical seat definitions per screen (row, number)." },
        new() { Key="ShowtimeSeat",      DisplayName="Showtime seats",      Category="Cinema",    Description="Per-showtime seat availability status.",
                IsLargeTable=true },
        new() { Key="SeatReservation",   DisplayName="Seat reservations",   Category="Cinema",    Description="Active short-lived seat holds (10-min checkout locks).",
                ExcludedByDefault=true },
        new() { Key="Ticket",            DisplayName="Tickets",             Category="Cinema",    Description="Sold tickets with code, showtime, user, and seat." },

        // ── Concessions ───────────────────────────────────────────────────
        new() { Key="ConcessionItem",    DisplayName="Concession items",    Category="Concessions", Description="Menu items with price and stock level." },
        new() { Key="ConcessionSale",    DisplayName="Concession sales",    Category="Concessions", Description="Concession purchase orders." },
        new() { Key="ConcessionSaleItem",DisplayName="Concession line items",Category="Concessions",Description="Individual items within each concession sale." },

        // ── Workforce ─────────────────────────────────────────────────────
        new() { Key="Employees",         DisplayName="Employees",           Category="Workforce", Description="Employee records linked to staff user accounts." },
        new() { Key="Shifts",            DisplayName="Shifts",              Category="Workforce", Description="Shift templates by role and location." },
        new() { Key="AssignmentAreas",   DisplayName="Assignment areas",    Category="Workforce", Description="Normalized lookup of assignment areas (Box Office, Concessions, ...)." },
        new() { Key="ScheduleAssignments",DisplayName="Schedule assignments",Category="Workforce",Description="Shift assignments to employees per week." },
        new() { Key="TimeEntries",       DisplayName="Time entries",        Category="Workforce", Description="Clock-in/clock-out records." },
        new() { Key="PayPeriods",        DisplayName="Pay periods",         Category="Workforce", Description="Payroll period definitions." },
        new() { Key="Timesheets",        DisplayName="Timesheets",          Category="Workforce", Description="Aggregated hours per employee per pay period." },
        new() { Key="Payrolls",          DisplayName="Payrolls",            Category="Workforce", Description="Payroll run header records." },
        new() { Key="PayStubs",          DisplayName="Pay stubs",           Category="Workforce", Description="Individual employee pay stubs." },
    ];

    // ── DI ─────────────────────────────────────────────────────────────────

    private readonly AppDbContext _db;
    private readonly ILogger<DatabaseBackupService> _logger;

    public DatabaseBackupService(AppDbContext db, ILogger<DatabaseBackupService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── IDatabaseBackupService ─────────────────────────────────────────────

    /// <inheritdoc />
    public IReadOnlyList<BackupTableDescriptor> GetAvailableTables() => TableRegistry;

    /// <inheritdoc />
    /// <remarks>
    /// Back-compat entry point. Preserves the original behaviour exactly — a
    /// data-only backup (JSON + INSERT scripts) over the given tables — by
    /// delegating to the options overload with <see cref="BackupMode.DataOnly"/>.
    /// </remarks>
    public Task<BackupResult> GenerateAsync(
        IEnumerable<string>? tableKeys,
        CancellationToken cancellationToken = default)
        => GenerateAsync(
            DatabaseBackupOptions.From(BackupMode.DataOnly, tableKeys?.ToList()),
            cancellationToken);

    /// <inheritdoc />
    public Task<BackupResult> GenerateAsync(
        DatabaseBackupOptions options,
        CancellationToken cancellationToken = default)
        => GenerateAsync(options, progress: null, cancellationToken);

    /// <inheritdoc />
    public async Task<BackupResult> GenerateAsync(
        DatabaseBackupOptions options,
        IProgressReporter? progress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.HasAnySection)
            throw new InvalidOperationException("Backup requested with no sections selected.");

        var takenAtUtc = DateTime.UtcNow;
        var timestamp = takenAtUtc.ToString("yyyyMMdd_HHmmss");
        var folderName = $"scrumflix_backup_{timestamp}";

        // Resolve which tables to include, preserving registry order.
        // Registry keys are the SQL table names (see [Table] attributes), so the
        // same list scopes both the data section and the schema CREATE section.
        var keySet = options.SelectedTableKeys?.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var tables = keySet is { Count: > 0 }
            ? TableRegistry.Where(t => keySet.Contains(t.Key)).ToList()
            : TableRegistry.Where(t => !t.ExcludedByDefault).ToList();

        var rowCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var jsonFiles = new Dictionary<string, byte[]>();  // key → JSON UTF-8
        var sqlFiles = new Dictionary<string, byte[]>();  // key → SQL UTF-8

        // ── Progress unit accounting ───────────────────────────────────────
        // Each table serialised in the data section counts as one unit, plus
        // one unit per connection-based section (schema, database objects).
        // This gives the spinner a coarse but real "X of Y" progress signal
        // without instrumenting SchemaBackupProvider/DatabaseObjectBackupProvider
        // internals.
        var needsConnectionForProgress = options.IncludeSchema
            || options.IncludeStoredProcedures
            || options.IncludeViews
            || options.IncludeTriggers;

        var totalUnits = (options.IncludeData ? tables.Count : 0)
            + (options.IncludeSchema ? 1 : 0)
            + (needsConnectionForProgress
               && (options.IncludeStoredProcedures || options.IncludeViews || options.IncludeTriggers) ? 1 : 0);

        var completedUnits = 0;
        var failedUnits = 0;

        // ── Data section (rows → JSON + INSERT) ───────────────────────────
        if (options.IncludeData)
        {
            foreach (var table in tables)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.CancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var (json, sql, rowCount) = await SerialiseTableAsync(table, cancellationToken);
                    jsonFiles[table.Key] = json;
                    sqlFiles[table.Key] = sql;
                    rowCounts[table.Key] = rowCount;

                    _logger.LogDebug("Backup: serialised {Table} — {Rows} rows", table.Key, rowCount);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Backup: failed to serialise table {Table} — skipping", table.Key);
                    rowCounts[table.Key] = -1;  // -1 signals a serialisation error
                    failedUnits++;
                }

                completedUnits++;
                progress?.Report(ProgressState.InProgress(
                    operationId:   progress.OperationId,
                    operationName: "Database Backup",
                    status:        $"Backed up table {table.DisplayName} ({completedUnits} of {totalUnits})…",
                    current:       completedUnits,
                    total:         totalUnits,
                    succeeded:     completedUnits - failedUnits,
                    skipped:       0,
                    failed:        failedUnits));
            }
        }

        // ── Schema + object sections (raw DDL over the EF connection) ──────
        SchemaSection? schemaSection = null;
        DatabaseObjectSection? objectSection = null;

        var needsConnection = options.IncludeSchema
            || options.IncludeStoredProcedures
            || options.IncludeViews
            || options.IncludeTriggers;

        if (needsConnection)
        {
            var connection = _db.Database.GetDbConnection();
            var introspector = new MySqlIntrospector(connection);
            var openedHere = await introspector.OpenIfClosedAsync(cancellationToken);
            try
            {
                var schemaName = await introspector.GetDatabaseNameAsync(cancellationToken);

                if (options.IncludeSchema)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress?.CancellationToken.ThrowIfCancellationRequested();

                    var tableNames = tables.Select(t => t.Key).ToList();
                    schemaSection = await new SchemaBackupProvider(introspector, _logger)
                        .CaptureAsync(tableNames, options.DropBeforeCreate, takenAtUtc, cancellationToken);

                    completedUnits++;
                    progress?.Report(ProgressState.InProgress(
                        operationId:   progress.OperationId,
                        operationName: "Database Backup",
                        status:        $"Captured table structure ({completedUnits} of {totalUnits})…",
                        current:       completedUnits,
                        total:         totalUnits,
                        succeeded:     completedUnits - failedUnits,
                        skipped:       0,
                        failed:        failedUnits));
                }

                if (options.IncludeStoredProcedures || options.IncludeViews || options.IncludeTriggers)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress?.CancellationToken.ThrowIfCancellationRequested();

                    objectSection = await new DatabaseObjectBackupProvider(introspector, _logger)
                        .CaptureAsync(
                            schemaName,
                            options.IncludeStoredProcedures,
                            options.IncludeViews,
                            options.IncludeTriggers,
                            options.DropBeforeCreate,
                            takenAtUtc,
                            cancellationToken);

                    completedUnits++;
                    progress?.Report(ProgressState.InProgress(
                        operationId:   progress.OperationId,
                        operationName: "Database Backup",
                        status:        $"Captured database objects ({completedUnits} of {totalUnits})…",
                        current:       completedUnits,
                        total:         totalUnits,
                        succeeded:     completedUnits - failedUnits,
                        skipped:       0,
                        failed:        failedUnits));
                }
            }
            finally
            {
                if (openedHere) await introspector.CloseAsync();
            }
        }

        // ── Assemble the .zip ─────────────────────────────────────────────
        progress?.Report(ProgressState.InProgress(
            operationId:   progress?.OperationId ?? string.Empty,
            operationName: "Database Backup",
            status:        "Assembling archive…",
            current:       totalUnits,
            total:         totalUnits,
            succeeded:     totalUnits - failedUnits,
            skipped:       0,
            failed:        failedUnits));

        var zipBytes = AssembleArchive(
            folderName, options, tables, jsonFiles, sqlFiles, rowCounts,
            schemaSection, objectSection, takenAtUtc);

        return new BackupResult
        {
            ZipBytes         = zipBytes,
            FileName         = $"scrumflix_backup_{timestamp}.zip",
            TakenAtUtc       = takenAtUtc,
            RowCounts        = rowCounts,
            TableCount       = tables.Count,
            SchemaTableCount = schemaSection?.TableNames.Count ?? 0,
            ProcedureCount   = objectSection?.Procedures.Count ?? 0,
            FunctionCount    = objectSection?.Functions.Count ?? 0,
            ViewCount        = objectSection?.Views.Count ?? 0,
            TriggerCount     = objectSection?.Triggers.Count ?? 0,
            IncludedSections = BuildSectionLabels(options, schemaSection, objectSection),
        };
    }

    /// <summary>Builds the human-readable section labels for the result/audit log.</summary>
    private static IReadOnlyList<string> BuildSectionLabels(
        DatabaseBackupOptions options, SchemaSection? schema, DatabaseObjectSection? objects)
    {
        var labels = new List<string>();
        if (schema is { TableNames.Count: > 0 })       labels.Add("Schema");
        if (options.IncludeData)                        labels.Add("Data");
        if (objects is not null)
        {
            if (objects.Procedures.Count + objects.Functions.Count > 0) labels.Add("Stored routines");
            if (objects.Views.Count > 0)                                labels.Add("Views");
            if (objects.Triggers.Count > 0)                             labels.Add("Triggers");
        }
        return labels;
    }

    // ── Serialisation helpers ──────────────────────────────────────────────

    /// <summary>
    /// Serialises one table to JSON bytes and SQL INSERT bytes.
    /// Large tables are paged in chunks of <see cref="PageSize"/>.
    /// </summary>
    private async Task<(byte[] Json, byte[] Sql, int RowCount)> SerialiseTableAsync(
        BackupTableDescriptor table,
        CancellationToken ct)
    {
        // Fetch rows as anonymous/dictionary projections rather than navigating
        // to typed entities, so we get only the scalar columns that exist in
        // the DB and avoid circular-reference issues in serialisation.
        var rows = await FetchTableRowsAsync(table, ct);

        // JSON
        var json = JsonSerializer.SerializeToUtf8Bytes(rows, JsonOpts);

        // SQL
        var sql = rows.Count > 0
            ? BuildInsertSql(table.Key, rows)
            : Encoding.UTF8.GetBytes($"-- {table.Key}: 0 rows\n");

        return (json, sql, rows.Count);
    }

    /// <summary>
    /// Fetches all rows for a table as a list of string→object? dictionaries.
    /// Each dictionary key is the column name; the value is the raw scalar value.
    /// Navigation properties are excluded. Password fields are redacted.
    /// Large tables are fetched in pages and concatenated.
    /// </summary>
    private async Task<List<Dictionary<string, object?>>> FetchTableRowsAsync(
        BackupTableDescriptor table,
        CancellationToken ct)
    {
        if (table.IsLargeTable)
            return await FetchPagedAsync(table, ct);

        return table.Key switch
        {
            "Roles" => Project(_db.Roles.AsNoTracking().OrderBy(r => r.RoleId),
                                        r => Row("RoleId", r.RoleId, "RoleName", r.RoleName)),

            "Users" => Project(_db.Users.AsNoTracking().OrderBy(u => u.UserId),
                                        u => Row("UserId", u.UserId,
                                                  "EmployeeId", u.EmployeeId,
                                                  "UserName", u.UserName,
                                                  // UserPassword and PasswordHash are intentionally excluded.
                                                  "IsActive", u.IsActive,
                                                  "MustChangePassword", u.MustChangePassword,
                                                  "RoleId", u.RoleId,
                                                  "LockoutEndUtc", u.LockoutEndUtc,
                                                  "FailedAccessCount", u.FailedAccessCount,
                                                  "PasswordChangedUtc", u.PasswordChangedUtc,
                                                  "LastLoginUtc", u.LastLoginUtc)),

            "AuditLog" => await _db.AuditLogs.AsNoTracking().OrderBy(a => a.AuditLogId)
                                        .Select(a => Row("AuditLogId", (object?)a.AuditLogId,
                                                         "UserId", a.UserId,
                                                         "ActionType", a.ActionType,
                                                         "TableName", a.TableName,
                                                         "ObjectId", a.ObjectId,
                                                         "ActionTime", a.ActionTime,
                                                         "Description", a.Description,
                                                         "OldValues", a.OldValues,
                                                         "NewValues", a.NewValues))
                                        .ToListAsync(ct),

            "Location" => Project(_db.Locations.AsNoTracking().OrderBy(l => l.LocationId),
                                        l => Row("LocationId", l.LocationId,
                                                  "LocationName", l.LocationName,
                                                  "LocationAddress", l.LocationAddress,
                                                  "IsActive", l.IsActive,
                                                  "TimeZoneId", l.TimeZoneId)),

            "TheaterScreen" => Project(_db.TheaterScreens.AsNoTracking().OrderBy(s => s.TheaterScreenId),
                                        s => Row("TheaterScreenId", s.TheaterScreenId,
                                                  "LocationId", s.LocationId,
                                                  "ScreenName", s.ScreenName,
                                                  "Capacity", s.Capacity,
                                                  "IsActive", s.IsActive)),

            "Genres" => Project(_db.Genres.AsNoTracking().OrderBy(g => g.GenreId),
                                        g => Row("GenreId", g.GenreId,
                                                  "Name", g.Name,
                                                  "TMDbGenreId", g.TMDbGenreId,
                                                  "Slug", g.Slug,
                                                  "IsActive", g.IsActive,
                                                  "CreatedUtc", g.CreatedUtc)),

            "Movies" => Project(_db.Movies.AsNoTracking().OrderBy(m => m.MovieId),
                                        m => Row("MovieId", m.MovieId,
                                                  "Title", m.Title,
                                                  "Rating", m.Rating,
                                                  "Genre", m.Genre,
                                                  "RuntimeMinutes", m.RuntimeMinutes,
                                                  "Description", m.Description)),

            "MovieGenres" => Project(_db.MovieGenres.AsNoTracking().OrderBy(mg => mg.MovieGenreId),
                                        mg => Row("MovieGenreId", mg.MovieGenreId,
                                                  "MovieId", mg.MovieId,
                                                  "GenreId", mg.GenreId,
                                                  "IsPrimaryGenre", mg.IsPrimaryGenre,
                                                  "CreatedUtc", mg.CreatedUtc)),

            "MovieTmdbMetadata" => Project(_db.MovieTmdbMetadata.AsNoTracking().OrderBy(t => t.MovieId),
                                        t => Row("MovieTmdbMetadataId", t.MovieTmdbMetadataId,
                                                  "MovieId", t.MovieId,
                                                  "TMDbMovieId", t.TMDbMovieId,
                                                  "PosterPath", t.PosterPath,
                                                  "BackdropPath", t.BackdropPath,
                                                  "TrailerYouTubeKey", t.TrailerYouTubeKey,
                                                  "OriginalTitle", t.OriginalTitle,
                                                  "OriginalLanguage", t.OriginalLanguage,
                                                  "ReleaseDate", t.ReleaseDate,
                                                  "Popularity", t.Popularity,
                                                  "VoteAverage", t.VoteAverage,
                                                  "VoteCount", t.VoteCount,
                                                  "LastSyncedUtc", t.LastSyncedUtc,
                                                  "CreatedUtc", t.CreatedUtc,
                                                  "UpdatedUtc", t.UpdatedUtc)),

            "Showtime" => Project(_db.Showtimes.AsNoTracking().OrderBy(s => s.ShowtimeId),
                                        s => Row("ShowtimeId", s.ShowtimeId,
                                                  "MovieId", s.MovieId,
                                                  "TheaterScreenId", s.TheaterScreenId,
                                                  "StartTime", s.StartTime,
                                                  "Capacity", s.Capacity,
                                                  "PricePerTicket", s.PricePerTicket,
                                                  "IsActive", s.IsActive)),

            "Seat" => Project(_db.Seats.AsNoTracking().OrderBy(s => s.SeatId),
                                        s => Row("SeatId", s.SeatId,
                                                  "TheaterScreenId", s.TheaterScreenId,
                                                  "RowLabel", s.RowLabel,
                                                  "SeatNumber", s.SeatNumber,
                                                  "SeatType", s.SeatType,
                                                  "IsActive", s.IsActive,
                                                  "ColumnNumber", s.ColumnNumber,
                                                  "RowNumber", s.RowNumber)),

            "SeatReservation" => Project(_db.SeatReservations.AsNoTracking().OrderBy(r => r.ReservationId),
                                        r => Row("ReservationId", r.ReservationId,
                                                  "ShowtimeSeatId", r.ShowtimeSeatId,
                                                  "UserId", r.UserId,
                                                  "ReservationStatus", r.ReservationStatus,
                                                  "ReservedAt", r.ReservedAt,
                                                  "ExpiresAt", r.ExpiresAt)),

            "Ticket" => Project(_db.Tickets.AsNoTracking().OrderBy(t => t.TicketId),
                                        t => Row("TicketId", t.TicketId,
                                                  "TicketCode", t.TicketCode,
                                                  "ShowtimeId", t.ShowtimeId,
                                                  "UserAtSale", t.UserAtSale,
                                                  "TimeOfSale", t.TimeOfSale,
                                                  "ShowtimeSeatId", t.ShowtimeSeatId)),

            "ConcessionItem" => Project(_db.ConcessionItems.AsNoTracking().OrderBy(c => c.ConcessionItemId),
                                        c => Row("ConcessionItemId", c.ConcessionItemId,
                                                  "ItemName", c.ItemName,
                                                  "Price", c.Price,
                                                  "QuantityInStock", c.QuantityInStock,
                                                  "Minimum", c.Minimum,
                                                  "LocationId", c.LocationId,
                                                  "IsActive", c.IsActive)),

            "ConcessionSale" => Project(_db.ConcessionSales.AsNoTracking().OrderBy(c => c.ConcessionSaleId),
                                        c => Row("ConcessionSaleId", c.ConcessionSaleId,
                                                  "UserId", c.UserId,
                                                  "CustomerEmail", c.CustomerEmail,
                                                  "TimeOfSale", c.TimeOfSale,
                                                  "Total", c.Total,
                                                  "LocationId", c.LocationId)),

            "ConcessionSaleItem" => Project(_db.ConcessionSaleItems.AsNoTracking().OrderBy(c => c.ConcessionSaleItemId),
                                        c => Row("ConcessionSaleItemId", c.ConcessionSaleItemId,
                                                  "ConcessionSaleId", c.ConcessionSaleId,
                                                  "ConcessionItemId", c.ConcessionItemId,
                                                  "Quantity", c.Quantity,
                                                  "UnitPrice", c.UnitPrice,
                                                  "LineTotal", c.LineTotal)),

            "Employees" => Project(_db.Employees.AsNoTracking().OrderBy(e => e.EmployeeId),
                                        e => Row("EmployeeId", e.EmployeeId,
                                                  "FirstName", e.FirstName,
                                                  "MiddleName", e.MiddleName,
                                                  "LastName", e.LastName,
                                                  "DOB", e.DOB,
                                                  "Phone", e.Phone,
                                                  "Email", e.Email,
                                                  "Address", e.Address,
                                                  "PayRate", e.PayRate,
                                                  "LocationId", e.LocationId)),

            "Shifts" => Project(_db.Shifts.AsNoTracking().OrderBy(s => s.ShiftId),
                                        s => Row("ShiftId", s.ShiftId,
                                                  "LocationId", s.LocationId,
                                                  "RoleId", s.RoleId,
                                                  "StartTime", s.StartTime,
                                                  "EndTime", s.EndTime)),

            "AssignmentAreas" => Project(_db.AssignmentAreas.AsNoTracking().OrderBy(aa => aa.AssignmentAreaId),
                                        aa => Row("AssignmentAreaId", aa.AssignmentAreaId,
                                                  "AreaName", aa.AreaName,
                                                  "IsActive", aa.IsActive)),

            "ScheduleAssignments" => Project(_db.ScheduleAssignments.AsNoTracking().OrderBy(a => a.AssignmentId),
                                        a => Row("AssignmentId", a.AssignmentId,
                                                  "UserId", a.UserId,
                                                  "AssignmentAreaId", a.AssignmentAreaId,
                                                  "ShiftId", a.ShiftId,
                                                  "ShowtimeId", a.ShowtimeId)),

            "TimeEntries" => Project(_db.TimeEntries.AsNoTracking().OrderBy(t => t.TimeEntryId),
                                        t => Row("TimeEntryId", t.TimeEntryId,
                                                  "EmployeeId", t.EmployeeId,
                                                  "LocationId", t.LocationId,
                                                  "ClockIn", t.ClockIn,
                                                  "ClockOut", t.ClockOut)),

            "PayPeriods" => Project(_db.PayPeriods.AsNoTracking().OrderBy(p => p.PayPeriodId),
                                        p => Row("PayPeriodId", p.PayPeriodId,
                                                  "StartDate", p.StartDate,
                                                  "EndDate", p.EndDate,
                                                  "IsGenerating", p.IsGenerating)),

            "Timesheets" => Project(_db.Timesheets.AsNoTracking().OrderBy(t => t.TimesheetId),
                                        t => Row("TimesheetId", t.TimesheetId,
                                                  "EmployeeId", t.EmployeeId,
                                                  "PayPeriodId", t.PayPeriodId,
                                                  "LocationId", t.LocationId,
                                                  "TotalHours", t.TotalHours,
                                                  "Approved", t.Approved,
                                                  "ApprovedByUserId", t.ApprovedByUserId)),

            "Payrolls" => Project(_db.Payrolls.AsNoTracking().OrderBy(p => p.PayrollId),
                                        p => Row("PayrollId", p.PayrollId,
                                                  "EmployeeId", p.EmployeeId,
                                                  "PayPeriodId", p.PayPeriodId,
                                                  "LocationId", p.LocationId,
                                                  "GrossPay", p.GrossPay)),

            "PayStubs" => Project(_db.PayStubs.AsNoTracking().OrderBy(p => p.PayStubId),
                                        p => Row("PayStubId", p.PayStubId,
                                                  "PayrollId", p.PayrollId,
                                                  "IssueDate", p.IssueDate)),

            _ => []
        };
    }

    /// <summary>
    /// Fetches ShowtimeSeat and AppLogs in pages to avoid Somee OOM.
    /// </summary>
    private async Task<List<Dictionary<string, object?>>> FetchPagedAsync(
        BackupTableDescriptor table,
        CancellationToken ct)
    {
        var all = new List<Dictionary<string, object?>>();
        int page = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            List<Dictionary<string, object?>> chunk;

            if (table.Key == "ShowtimeSeat")
            {
                chunk = await _db.ShowtimeSeats
                    .AsNoTracking()
                    .OrderBy(s => s.ShowtimeSeatId)
                    .Skip(page * PageSize).Take(PageSize)
                    .Select(s => Row("ShowtimeSeatId", (object?)s.ShowtimeSeatId,
                                     "ShowtimeId", s.ShowtimeId,
                                     "SeatId", s.SeatId,
                                     "Status", s.Status))
                    .ToListAsync(ct);
            }
            else if (table.Key == "Logs")
            {
                chunk = await _db.AppLogs
                    .AsNoTracking()
                    .OrderBy(l => l.Id)
                    .Skip(page * PageSize).Take(PageSize)
                    .Select(l => Row("id", (object?)l.Id,
                                     "Timestamp", l.Timestamp,
                                     "Level", l.Level,
                                     "Message", l.Message,
                                     "Exception", l.Exception,
                                     "Properties", l.Properties,
                                     "_ts", l.Ts))
                    .ToListAsync(ct);
            }
            else break;

            if (chunk.Count == 0) break;
            all.AddRange(chunk);
            if (chunk.Count < PageSize) break;
            page++;
        }

        return all;
    }

    // ── SQL INSERT builder ─────────────────────────────────────────────────

    /// <summary>
    /// Builds a MySQL-compatible INSERT script for a single table.
    /// Uses batched multi-row INSERT syntax (INSERT INTO t (...) VALUES (...),(...),...;)
    /// with a batch size of 500 rows to keep individual statements manageable.
    /// All values are properly escaped. DateTime values are emitted as ISO strings
    /// in single quotes, which MySQL accepts natively.
    /// </summary>
    private static byte[] BuildInsertSql(string tableName, List<Dictionary<string, object?>> rows)
    {
        const int BatchSize = 500;
        var sb = new StringBuilder();

        sb.AppendLine($"-- ScrumFlix backup: `{tableName}` — {rows.Count} rows");
        sb.AppendLine($"-- Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        var columns = rows[0].Keys.ToList();
        var colList = string.Join(", ", columns.Select(c => $"`{c}`"));

        for (int i = 0; i < rows.Count; i += BatchSize)
        {
            var batch = rows.Skip(i).Take(BatchSize).ToList();
            sb.AppendLine($"INSERT INTO `{tableName}` ({colList}) VALUES");

            for (int j = 0; j < batch.Count; j++)
            {
                var values = columns.Select(c => SqlValue(batch[j].GetValueOrDefault(c)));
                sb.Append("  (");
                sb.Append(string.Join(", ", values));
                sb.Append(')');
                sb.AppendLine(j == batch.Count - 1 ? ";" : ",");
            }

            sb.AppendLine();
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    /// <summary>Formats a single value for a SQL INSERT statement.</summary>
    private static string SqlValue(object? v) => v switch
    {
        null => "NULL",
        bool b => b ? "1" : "0",
        DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
        DateOnly d => $"'{d:yyyy-MM-dd}'",
        TimeOnly t => $"'{t:HH:mm:ss}'",
        decimal or float
            or double => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", v),
        int or long
            or short => v.ToString()!,
        string s => $"'{s.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "\\r").Replace("\n", "\\n")}'",
        _ => $"'{v}'"
    };

    // ── Zip builder ────────────────────────────────────────────────────────

    private static byte[] AssembleArchive(
        string folderName,
        DatabaseBackupOptions options,
        List<BackupTableDescriptor> tables,
        Dictionary<string, byte[]> jsonFiles,
        Dictionary<string, byte[]> sqlFiles,
        Dictionary<string, int> rowCounts,
        SchemaSection? schema,
        DatabaseObjectSection? objects,
        DateTime takenAtUtc)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            // ── manifest.json (typed) ──────────────────────────────────────
            var manifest = ManifestBuilder.Build(options, rowCounts, schema, objects, takenAtUtc);
            WriteEntry(zip, $"{folderName}/manifest.json",
                JsonSerializer.SerializeToUtf8Bytes(manifest, JsonOpts));

            // ── restore_full.sql (master script) ───────────────────────────
            WriteEntry(zip, $"{folderName}/restore_full.sql",
                BuildMasterRestoreScript(options, schema, objects, takenAtUtc));

            // ── schema/ ────────────────────────────────────────────────────
            if (schema is not null)
                foreach (var file in schema.Files)
                    WriteEntry(zip, $"{folderName}/{file.RelativePath}", file.Content);

            // ── data: json/ and sql/ (unchanged layout) ────────────────────
            if (options.IncludeData)
            {
                foreach (var table in tables)
                {
                    if (!jsonFiles.TryGetValue(table.Key, out var json)) continue;
                    WriteEntry(zip, $"{folderName}/json/{table.Key}.json", json);
                }

                // Import-order guide — tells the admin in which order to source each file
                var importSb = new StringBuilder();
                importSb.AppendLine("-- ScrumFlix backup import guide (data)");
                importSb.AppendLine($"-- Generated: {takenAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
                importSb.AppendLine("-- Run from the backup ROOT folder in the mysql client:  SOURCE sql/00_import_order.sql;");
                importSb.AppendLine();
                importSb.AppendLine("SET FOREIGN_KEY_CHECKS = 0;");
                importSb.AppendLine();
                int orderIndex = 1;
                foreach (var table in tables)
                {
                    if (!sqlFiles.ContainsKey(table.Key)) continue;
                    importSb.AppendLine($"SOURCE sql/{orderIndex:D2}_{table.Key}.sql;");
                    orderIndex++;
                }
                importSb.AppendLine();
                importSb.AppendLine("SET FOREIGN_KEY_CHECKS = 1;");
                WriteEntry(zip, $"{folderName}/sql/00_import_order.sql",
                    Encoding.UTF8.GetBytes(importSb.ToString()));

                int fileIndex = 1;
                foreach (var table in tables)
                {
                    if (!sqlFiles.TryGetValue(table.Key, out var sql)) continue;
                    WriteEntry(zip, $"{folderName}/sql/{fileIndex:D2}_{table.Key}.sql", sql);
                    fileIndex++;
                }
            }

            // ── routines/ (procedures, functions, views, triggers) ─────────
            if (objects is not null)
                foreach (var file in objects.Files)
                    WriteEntry(zip, $"{folderName}/{file.RelativePath}", file.Content);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Builds restore_full.sql — a master script that SOURCEs each included
    /// section in dependency-safe order (schema → data → routines/views/triggers).
    /// Only references sections actually present in the archive.
    /// </summary>
    private static byte[] BuildMasterRestoreScript(
        DatabaseBackupOptions options,
        SchemaSection? schema,
        DatabaseObjectSection? objects,
        DateTime takenAtUtc)
    {
        var hasSchema   = schema is { TableNames.Count: > 0 };
        var hasObjects  = objects is { IsEmpty: false };

        var sb = new StringBuilder();
        sb.AppendLine("-- ScrumFlix full restore script");
        sb.AppendLine($"-- Generated: {takenAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("--");
        sb.AppendLine("-- SOURCE and DELIMITER are mysql-client directives, so run this");
        sb.AppendLine("-- interactively from the extracted backup folder root:");
        sb.AppendLine("--   mysql -u <user> -p <database>");
        sb.AppendLine("--   mysql> SOURCE restore_full.sql;");
        sb.AppendLine();
        sb.AppendLine("SET FOREIGN_KEY_CHECKS = 0;");
        sb.AppendLine("SET UNIQUE_CHECKS = 0;");
        sb.AppendLine();

        if (hasSchema)
        {
            sb.AppendLine("-- 1) Table structure");
            sb.AppendLine("SOURCE schema/00_schema.sql;");
            sb.AppendLine();
        }
        if (options.IncludeData)
        {
            sb.AppendLine("-- 2) Table data");
            sb.AppendLine("SOURCE sql/00_import_order.sql;");
            sb.AppendLine();
        }
        if (hasObjects)
        {
            sb.AppendLine("-- 3) Stored routines, views, and triggers");
            sb.AppendLine("SOURCE routines/00_routines.sql;");
            sb.AppendLine();
        }

        sb.AppendLine("SET UNIQUE_CHECKS = 1;");
        sb.AppendLine("SET FOREIGN_KEY_CHECKS = 1;");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static void WriteEntry(ZipArchive zip, string entryName, byte[] data)
    {
        var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(data, 0, data.Length);
    }

    // ── Micro-DSL helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Builds a row dictionary from alternating key/value pairs.
    /// Usage: Row("Col1", val1, "Col2", val2, ...)
    /// </summary>
    private static Dictionary<string, object?> Row(params object?[] keyValuePairs)
    {
        if (keyValuePairs.Length % 2 != 0)
            throw new ArgumentException("Row() requires an even number of arguments.");
        var dict = new Dictionary<string, object?>(keyValuePairs.Length / 2);
        for (int i = 0; i < keyValuePairs.Length; i += 2)
            dict[(string)keyValuePairs[i]!] = keyValuePairs[i + 1];
        return dict;
    }

    /// <summary>
    /// Projects a queryable to a list of row dictionaries using a selector function.
    /// Used for tables small enough to load in one query.
    /// </summary>
    private static List<Dictionary<string, object?>> Project<T>(
        IOrderedQueryable<T> query,
        Func<T, Dictionary<string, object?>> selector)
        => [.. query.AsEnumerable().Select(selector)];
}
