/*
 * File:        /ScrumFlix/Data/SampleDataSeederFull.cs
 * Namespace:   ScrumFlix.Data
 * Purpose:     Development scaffold — pre-populates the canonical MySQL schema with
 *              representative sample data so the application can be debugged without
 *              manually inserting rows.
 *
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  HOW TO ENABLE SEEDING                                                      ║
 * ║                                                                              ║
 * ║  1. Open Program.cs and un-comment the SampleDataSeederFull.Seed(db) call.  ║
 * ║  2. Run the application locally against a DEV database only.                ║
 * ║  3. Comment the call back out (or remove it) before any production deploy.  ║
 * ║                                                                              ║
 * ║  ⚠  NEVER run this seeder against the Aiven Cloud (production) database.   ║
 * ║     The canonical schema is the ground truth; the seeder is for local dev.  ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 *
 * ╔══════════════════════════════════════════════════════════════════════════════╗
 * ║  WHICH TABLES ARE SEEDED — AND WHY                                          ║
 * ║                                                                              ║
 * ║  SEEDED (safe to pre-populate for dev/debug):                               ║
 * ║    Roles           — 3 fixed roles; app cannot function without them.       ║
 * ║    Location        — 4 theater locations used throughout the schema.        ║
 * ║    Employees       — Representative staff across all locations + roles.     ║
 * ║    Users           — One account per employee; required for authentication. ║
 * ║    Movies          — Catalog of 20 fictional titles.                        ║
 * ║    TheaterScreen   — 3 screens (Small/Medium/Large) per location.           ║
 * ║    Showtime        — Sample showtimes spanning the next 7 days.             ║
 * ║    ConcessionItem  — The 3 canonical items (Popcorn, Candy, Drink).        ║
 * ║    Shifts          — Representative shift templates per location.           ║
 * ║    PayPeriods      — A handful of bi-weekly pay periods for testing.        ║
 * ║                                                                              ║
 * ║  NOT SEEDED (populated by the application in normal use):                   ║
 * ║    Ticket                — Created at point of sale.                        ║
 * ║    ConcessionSale        — Created at point of sale.                        ║
 * ║    ConcessionSaleItem    — Created at point of sale.                        ║
 * ║    AuditLog              — Written by AuditService on every action.         ║
 * ║    ScheduleAssignments   — Assigned by managers in the Employee area.       ║
 * ║    TimeEntries           — Created by clock-in / clock-out workflow.        ║
 * ║    Timesheets            — Aggregated from TimeEntries by payroll engine.   ║
 * ║    Payrolls              — Calculated by payroll engine.                    ║
 * ║    PayStubs              — Issued by payroll engine after run.              ║
 * ║                                                                              ║
 * ║  To enable a specific table's block, un-comment it individually. Each       ║
 * ║  block is self-contained and clearly labelled with a region-style banner.   ║
 * ╚══════════════════════════════════════════════════════════════════════════════╝
 *
 * DEPENDENCY / SEED ORDER (foreign keys must exist before referencing rows):
 *
 *   1. Roles
 *   2. Location
 *   3. Employees          (needs LocationId)
 *   4. Users              (needs EmployeeId, RoleId)
 *   5. Movies
 *   6. TheaterScreen      (needs LocationId)
 *   7. Showtime           (needs MovieId, TheaterScreenId)
 *   8. ConcessionItem
 *   9. Shifts             (needs RoleId, LocationId)
 *  10. PayPeriods         (no FKs — can go anywhere, placed here for readability)
 *
 * Author:  ScrumFlix Rebuild Team
 * Phase:   1D  (scaffold only — all seed calls commented out pending schema review)
 * Updated: 2026-05-04
 */

namespace ScrumFlix.Data;

/// <summary>
/// Development-only seeder.  Populates the canonical schema with representative
/// sample data for local debugging.  Call <see cref="Seed"/> from Program.cs
/// (see the comment block at the top of this file for instructions).
/// </summary>
public static class SampleDataSeederFull
{
    /// <summary>
    /// Entry point called by Program.cs.  Each table block is individually
    /// commented out — un-comment only the blocks you need for a given debug session.
    /// </summary>
    /// <param name="db">
    /// The scoped <see cref="AppDbContext"/> resolved from the DI container.
    /// Always pass a freshly resolved context; never reuse a context across requests.
    /// </param>
    public static void Seed(AppDbContext db)
    {
        // ═══════════════════════════════════════════════════════════════════════
        //  HOW THIS METHOD WORKS
        //
        //  Each block below follows the same pattern:
        //
        //    1. Query the existing rows and build a HashSet of "keys already present"
        //       (unique natural key, e.g. RoleName, LocationName, Title).
        //    2. Declare candidate rows.
        //    3. Filter out any candidate whose key is already in the HashSet.
        //    4. Bulk-insert the survivors and call SaveChanges().
        //    5. Re-query the table to get EF-assigned PKs for use in subsequent blocks.
        //
        //  This makes the seeder idempotent — safe to run multiple times on the
        //  same database without creating duplicates.
        //
        //  To skip a table entirely: leave its block commented out.
        //  To add a table: copy the pattern from an existing block.
        // ═══════════════════════════════════════════════════════════════════════


        // ───────────────────────────────────────────────────────────────────────
        //  BLOCK 1 — Roles
        //  Maps to: Roles table  |  PK: RoleId (AI)  |  Unique: RoleName
        //
        //  The canonical schema defines exactly 3 roles.  Do not add extras here
        //  unless the schema is updated first.
        //    RoleId 1 = Admin
        //    RoleId 2 = Manager
        //    RoleId 3 = Employee
        //
        //  ⚠  RoleId values are auto-assigned by MySQL.  Do not hard-code them
        //     in this block — always reload from db after SaveChanges() so later
        //     blocks can look up the correct IDs.
        // ───────────────────────────────────────────────────────────────────────

        /*
        var existingRoleNames = db.Roles
            .Select(r => r.RoleName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var roleCandidates = new List<Role>
        {
            new() { RoleName = "Admin"    },
            new() { RoleName = "Manager"  },
            new() { RoleName = "Employee" },
        };

        var newRoles = roleCandidates
            .Where(r => !existingRoleNames.Contains(r.RoleName))
            .ToList();

        if (newRoles.Any())
        {
            db.Roles.AddRange(newRoles);
            db.SaveChanges();
        }

        // Reload with assigned PKs — subsequent blocks need Role.RoleId.
        var roles = db.Roles.ToList();
        */


        // ───────────────────────────────────────────────────────────────────────
        //  BLOCK 2 — Location
        //  Maps to: Location table  |  PK: LocationId (AI)  |  Unique: LocationName
        //
        //  The rebuild spec calls for 4 active locations.  Adjust addresses and
        //  names to match whatever the canonical SQL dump seeds — these values
        //  must stay in sync if the schema seed changes.
        // ───────────────────────────────────────────────────────────────────────

        /*
        var existingLocationNames = db.Locations
            .Select(l => l.LocationName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var locationCandidates = new List<Location>
        {
            new() { LocationName = "Dallas Central",     LocationAddress = "123 Cinema Way, Dallas, TX 75201",          IsActive = true },
            new() { LocationName = "Dallas North",       LocationAddress = "4800 Preston Rd, Dallas, TX 75230",          IsActive = true },
            new() { LocationName = "Fort Worth West",    LocationAddress = "1000 West 7th St, Fort Worth, TX 76102",     IsActive = true },
            new() { LocationName = "Mesquite Town East", LocationAddress = "1220 Town East Blvd, Mesquite, TX 75150",    IsActive = true },
        };

        var newLocations = locationCandidates
            .Where(l => !existingLocationNames.Contains(l.LocationName))
            .ToList();

        if (newLocations.Any())
        {
            db.Locations.AddRange(newLocations);
            db.SaveChanges();
        }

        // Reload with assigned PKs — Employees, TheaterScreens, and Shifts need LocationId.
        var locations = db.Locations.ToList();
        */


        // ───────────────────────────────────────────────────────────────────────
        //  BLOCK 3 — Employees
        //  Maps to: Employees table  |  PK: EmployeeId (AI)  |  Unique: Email
        //
        //  One employee per role per location gives 12 rows (4 locations × 3 roles).
        //  PayRate is hourly in USD.  DOB kept generic — adjust if business rules
        //  require minimum age enforcement in the seeder.
        //
        //  DEPENDS ON:  locations (Block 2 must be un-commented first)
        // ───────────────────────────────────────────────────────────────────────

        /*
        var existingEmails = db.Employees
            .Select(e => e.Email)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Helper: pick the LocationId for a given name (safe — locations were just reloaded).
        int Loc(string name) => locations.First(l => l.LocationName == name).LocationId;

        var employeeCandidates = new List<Employee>
        {
            // ── Dallas Central ─────────────────────────────────────────────────
            new() { FirstName = "Sandra",  MiddleName = "Lee",   LastName = "Nguyen",   DOB = new DateOnly(1985, 3, 12),  Phone = "214-555-0101", Email = "s.nguyen@scrumflix.dev",   Address = "101 Elm St, Dallas, TX",         PayRate = 28.50m, LocationId = Loc("Dallas Central")    },
            new() { FirstName = "Marcus",  MiddleName = null,    LastName = "Delgado",  DOB = new DateOnly(1992, 7, 4),   Phone = "214-555-0102", Email = "m.delgado@scrumflix.dev",  Address = "202 Oak Ave, Dallas, TX",        PayRate = 18.00m, LocationId = Loc("Dallas Central")    },
            new() { FirstName = "Priya",   MiddleName = "K.",    LastName = "Sharma",   DOB = new DateOnly(1998, 11, 30), Phone = "214-555-0103", Email = "p.sharma@scrumflix.dev",   Address = "303 Maple Dr, Dallas, TX",       PayRate = 16.50m, LocationId = Loc("Dallas Central")    },

            // ── Dallas North ───────────────────────────────────────────────────
            new() { FirstName = "Derek",   MiddleName = "Alan",  LastName = "Patel",    DOB = new DateOnly(1980, 5, 20),  Phone = "214-555-0201", Email = "d.patel@scrumflix.dev",    Address = "400 Birch Ln, Dallas, TX",       PayRate = 30.00m, LocationId = Loc("Dallas North")      },
            new() { FirstName = "Latoya",  MiddleName = null,    LastName = "Williams", DOB = new DateOnly(1995, 9, 15),  Phone = "214-555-0202", Email = "l.williams@scrumflix.dev", Address = "500 Cedar Blvd, Dallas, TX",     PayRate = 17.75m, LocationId = Loc("Dallas North")      },
            new() { FirstName = "Jorge",   MiddleName = "Luis",  LastName = "Reyes",    DOB = new DateOnly(2000, 2, 28),  Phone = "214-555-0203", Email = "j.reyes@scrumflix.dev",    Address = "606 Pine Ct, Dallas, TX",        PayRate = 15.50m, LocationId = Loc("Dallas North")      },

            // ── Fort Worth West ────────────────────────────────────────────────
            new() { FirstName = "Angela",  MiddleName = "R.",    LastName = "Torres",   DOB = new DateOnly(1978, 8, 8),   Phone = "817-555-0301", Email = "a.torres@scrumflix.dev",   Address = "700 Walnut St, Fort Worth, TX",  PayRate = 29.25m, LocationId = Loc("Fort Worth West")   },
            new() { FirstName = "Tyrone",  MiddleName = null,    LastName = "Jackson",  DOB = new DateOnly(1993, 4, 17),  Phone = "817-555-0302", Email = "t.jackson@scrumflix.dev",  Address = "808 Spruce Rd, Fort Worth, TX",  PayRate = 17.00m, LocationId = Loc("Fort Worth West")   },
            new() { FirstName = "Emily",   MiddleName = "Grace", LastName = "Chen",     DOB = new DateOnly(2001, 6, 3),   Phone = "817-555-0303", Email = "e.chen@scrumflix.dev",     Address = "909 Aspen Way, Fort Worth, TX",  PayRate = 15.00m, LocationId = Loc("Fort Worth West")   },

            // ── Mesquite Town East ─────────────────────────────────────────────
            new() { FirstName = "Robert",  MiddleName = "James", LastName = "Foster",   DOB = new DateOnly(1975, 12, 1),  Phone = "972-555-0401", Email = "r.foster@scrumflix.dev",   Address = "1001 Pecan Dr, Mesquite, TX",    PayRate = 31.00m, LocationId = Loc("Mesquite Town East") },
            new() { FirstName = "Keisha",  MiddleName = null,    LastName = "Brown",    DOB = new DateOnly(1997, 1, 22),  Phone = "972-555-0402", Email = "k.brown@scrumflix.dev",    Address = "1102 Hickory Ln, Mesquite, TX",  PayRate = 18.25m, LocationId = Loc("Mesquite Town East") },
            new() { FirstName = "Nathan",  MiddleName = "T.",    LastName = "Kim",      DOB = new DateOnly(2002, 10, 10), Phone = "972-555-0403", Email = "n.kim@scrumflix.dev",      Address = "1203 Magnolia Ct, Mesquite, TX", PayRate = 14.75m, LocationId = Loc("Mesquite Town East") },
        };

        var newEmployees = employeeCandidates
            .Where(e => !existingEmails.Contains(e.Email))
            .ToList();

        if (newEmployees.Any())
        {
            db.Employees.AddRange(newEmployees);
            db.SaveChanges();
        }

        // Reload with assigned PKs — Users block needs EmployeeId.
        var employees = db.Employees.ToList();
        */


        // ───────────────────────────────────────────────────────────────────────
        //  BLOCK 4 — Users
        //  Maps to: Users table  |  PK: UserId (AI)  |  Unique: UserName
        //
        //  One user account per employee.  UserName convention: first initial +
        //  last name, lower-case (e.g. "snguyen").
        //
        //  SECURITY NOTE:
        //    UserPassword is seeded as a plain-text temp value ONLY.  Phase 2
        //    AuthService will hash on first login and null out UserPassword.
        //    Never seed PasswordHash directly here — let the auth layer own it.
        //
        //  MustChangePassword = true on every seeded account — forces a password
        //  change at first login before the user can access anything else.
        //
        //  RoleId assignment:
        //    First employee per location  → Manager  (RoleId 2)
        //    All others                   → Employee (RoleId 3)
        //    Admin accounts should be created manually — never seeded.
        //
        //  DEPENDS ON:  employees (Block 3), roles (Block 1)
        // ───────────────────────────────────────────────────────────────────────

        /*
        var existingUserNames = db.Users
            .Select(u => u.UserName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Helper: look up EmployeeId by email (unique — safe to use as a key).
        int EmpId(string email) => employees.First(e => e.Email == email).EmployeeId;

        // Helper: look up RoleId by name.
        int RoleId(string name) => roles.First(r => r.RoleName == name).RoleId;

        var userCandidates = new List<User>
        {
            // ── Dallas Central ─────────────────────────────────────────────────
            new() { UserName = "snguyen",   UserPassword = "TempPass1!", PasswordHash = null, EmployeeId = EmpId("s.nguyen@scrumflix.dev"),   RoleId = RoleId("Manager"),  IsActive = true, MustChangePassword = true, FailedAccessCount = 0 },
            new() { UserName = "mdelgado",  UserPassword = "TempPass1!", PasswordHash = null, EmployeeId = EmpId("m.delgado@scrumflix.dev"),  RoleId = RoleId("Employee"), IsActive = true, MustChangePassword = true, FailedAccessCount = 0 },
            new() { UserName = "psharma",   UserPassword = "TempPass1!", PasswordHash = null, EmployeeId = EmpId("p.sharma@scrumflix.dev"),   RoleId = RoleId("Employee"), IsActive = true, MustChangePassword = true, FailedAccessCount = 0 },

            // ── Dallas North ───────────────────────────────────────────────────
            new() { UserName = "dpatel",    UserPassword = "TempPass1!", PasswordHash = null, EmployeeId = EmpId("d.patel@scrumflix.dev"),    RoleId = RoleId("Manager"),  IsActive = true, MustChangePassword = true, FailedAccessCount = 0 },
            new() { UserName = "lwilliams", UserPassword = "TempPass1!", PasswordHash = null, EmployeeId = EmpId("l.williams@scrumflix.dev"), RoleId = RoleId("Employee"), IsActive = true, MustChangePassword = true, FailedAccessCount = 0 },
            new() { UserName = "jreyes",    UserPassword = "TempPass1!", PasswordHash = null, EmployeeId = EmpId("j.reyes@scrumflix.dev"),    RoleId = RoleId("Employee"), IsActive = true, MustChangePassword = true, FailedAccessCount = 0 },

            // ── Fort Worth West ────────────────────────────────────────────────
            new() { UserName = "atorres",   UserPassword = "TempPass1!", PasswordHash = null, EmployeeId = EmpId("a.torres@scrumflix.dev"),   RoleId = RoleId("Manager"),  IsActive = true, MustChangePassword = true, FailedAccessCount = 0 },
            new() { UserName = "tjackson",  UserPassword = "TempPass1!", PasswordHash = null, EmployeeId = EmpId("t.jackson@scrumflix.dev"),  RoleId = RoleId("Employee"), IsActive = true, MustChangePassword = true, FailedAccessCount = 0 },
            new() { UserName = "echen",     UserPassword = "TempPass1!", PasswordHash = null, EmployeeId = EmpId("e.chen@scrumflix.dev"),     RoleId = RoleId("Employee"), IsActive = true, MustChangePassword = true, FailedAccessCount = 0 },

            // ── Mesquite Town East ─────────────────────────────────────────────
            new() { UserName = "rfoster",   UserPassword = "TempPass1!", PasswordHash = null, EmployeeId = EmpId("r.foster@scrumflix.dev"),   RoleId = RoleId("Manager"),  IsActive = true, MustChangePassword = true, FailedAccessCount = 0 },
            new() { UserName = "kbrown",    UserPassword = "TempPass1!", PasswordHash = null, EmployeeId = EmpId("k.brown@scrumflix.dev"),    RoleId = RoleId("Employee"), IsActive = true, MustChangePassword = true, FailedAccessCount = 0 },
            new() { UserName = "nkim",      UserPassword = "TempPass1!", PasswordHash = null, EmployeeId = EmpId("n.kim@scrumflix.dev"),      RoleId = RoleId("Employee"), IsActive = true, MustChangePassword = true, FailedAccessCount = 0 },
        };

        var newUsers = userCandidates
            .Where(u => !existingUserNames.Contains(u.UserName))
            .ToList();

        if (newUsers.Any())
        {
            db.Users.AddRange(newUsers);
            db.SaveChanges();
        }

        // Reload with assigned PKs — Ticket / ConcessionSale blocks need UserId.
        var users = db.Users.ToList();
        */


        // ───────────────────────────────────────────────────────────────────────
        //  BLOCK 4-W — WebUser (web.sales system account)
        //
        //  MUST run after Block 3 (Employees) so the synthetic Employee record
        //  exists for the NOT NULL FK on Users.EmployeeId.
        //
        //  PURPOSE:
        //    The canonical schema requires Ticket.UserAtSale and
        //    ConcessionSale.UserId to reference a valid UserId.  Public-facing
        //    web purchases have no authenticated employee.  The web.sales account
        //    is the synthetic employee/user pair that satisfies this constraint.
        //
        //  LOCKOUT:
        //    LockoutEndUtc is set to the year 2099 — effectively permanent —
        //    so the lockout check in AuthService fires before the system-account
        //    block check as a belt-and-suspenders defence.
        //    AuthService.LoginAsync ALSO explicitly rejects "web.sales" by name
        //    regardless of lockout state.
        //
        //  IDEMPOTENT:
        //    Both the Employee and User inserts check for an existing record
        //    before inserting.  Safe to run against a pre-seeded database.
        //
        //  PasswordHash and UserPassword are intentionally null — this account
        //  can never authenticate and must never have a usable credential.
        // ───────────────────────────────────────────────────────────────────────

        const string webSalesEmail = "web.sales@scrumflix.local";
        const string webSalesUserName = "web.sales";

        // ── 4-W-1: Synthetic Employee ──────────────────────────────────────────
        var webEmployee = db.Employees
            .FirstOrDefault(e => e.Email == webSalesEmail);

        if (webEmployee is null)
        {
            // Attach to the first location alphabetically as a nominal FK value.
            // LocationId is NOT NULL on Employees — we must supply one.
            var firstLocation = db.Locations.OrderBy(l => l.LocationName).First();

            webEmployee = new Employee
            {
                FirstName = "Web",
                MiddleName = null,
                LastName = "Sales",
                DOB = new DateOnly(2000, 1, 1),   // nominal — never displayed
                Phone = "000-000-0000",
                Email = webSalesEmail,
                Address = "System Account",
                PayRate = 0m,
                LocationId = firstLocation.LocationId,
            };

            db.Employees.Add(webEmployee);
            db.SaveChanges();   // assign EmployeeId before User insert
        }

        // ── 4-W-2: Synthetic User ─────────────────────────────────────────────
        var webUserExists = db.Users.Any(u => u.UserName == webSalesUserName);

        if (!webUserExists)
        {
            var employeeRoleId = db.Roles
                .Where(r => r.RoleName == "Employee")
                .Select(r => r.RoleId)
                .First();

            db.Users.Add(new User
            {
                UserName = webSalesUserName,
                PasswordHash = null,          // intentionally no credentials
                UserPassword = string.Empty,  // NOT NULL in live schema; empty satisfies constraint
                                              // AuthService blocks this account by name regardless
                EmployeeId = webEmployee.EmployeeId,
                RoleId = employeeRoleId,
                IsActive = false,         // excluded from active-user queries
                MustChangePassword = false,
                FailedAccessCount = 0,
                LockoutEndUtc = new DateTime(2099, 12, 31, 0, 0, 0, DateTimeKind.Utc)
            });

            db.SaveChanges();
        }

        // ───────────────────────────────────────────────────────────────────────
        //  BLOCK 4-S — Seats
        //
        //  Generates physical seat rows for every TheaterScreen based on its
        //  Capacity.  Uses a fixed 10-seats-per-row layout:
        //    Capacity 50 → rows A–E  (5 rows × 10 seats)
        //    Capacity 60 → rows A–F  (6 rows × 10 seats)
        //    Capacity 70 → rows A–G  (7 rows × 10 seats)
        //
        //  RowNumber and ColumnNumber are set for grid rendering.
        //  IDEMPOTENT: skips any screen that already has Seat rows.
        // ───────────────────────────────────────────────────────────────────────

        var screens = db.TheaterScreens.ToList();
        const int seatsPerRow = 10;

        foreach (var screen in screens)
        {
            // Skip if already seeded for this screen.
            if (db.Seats.Any(s => s.TheaterScreenId == screen.TheaterScreenId))
                continue;

            int rowCount = screen.Capacity / seatsPerRow;

            var newSeats = new List<Seat>();
            for (int r = 0; r < rowCount; r++)
            {
                string rowLabel = ((char)('A' + r)).ToString();   // A, B, C …
                for (int c = 1; c <= seatsPerRow; c++)
                {
                    newSeats.Add(new Seat
                    {
                        TheaterScreenId = screen.TheaterScreenId,
                        RowLabel = rowLabel,
                        SeatNumber = c,
                        RowNumber = r + 1,
                        ColumnNumber = c,
                        SeatType = "Standard",
                        IsActive = true,
                    });
                }
            }

            db.Seats.AddRange(newSeats);
        }

        db.SaveChanges();   // assign SeatIds before ShowtimeSeat cross-join

        // ───────────────────────────────────────────────────────────────────────
        //  BLOCK 4-SS — ShowtimeSeat cross-join
        //
        //  Creates one ShowtimeSeat row per Seat × Showtime combination, grouped
        //  by TheaterScreenId.  Status defaults to 'Available'.
        //
        //  Uses a bulk-insert strategy: builds the full list in memory, then calls
        //  AddRange + SaveChanges once per showtime batch to avoid N+1 round-trips.
        //
        //  IDEMPOTENT: skips any showtime that already has ShowtimeSeat rows.
        // ───────────────────────────────────────────────────────────────────────

        // Re-query so we have the newly assigned SeatIds.
        var allSeats = db.Seats.ToList();
        var allShowtimes = db.Showtimes.ToList();

        var seatsByScreen = allSeats
            .GroupBy(s => s.TheaterScreenId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var showtime in allShowtimes)
        {
            // Idempotency check — skip if this showtime already has seat records.
            if (db.ShowtimeSeats.Any(ss => ss.ShowtimeId == showtime.ShowtimeId))
                continue;

            if (!seatsByScreen.TryGetValue(showtime.TheaterScreenId, out var seats))
                continue;

            var showtimeSeats = seats.Select(seat => new ShowtimeSeat
            {
                ShowtimeId = showtime.ShowtimeId,
                SeatId = seat.SeatId,
                Status = SeatStatus.Available,
            }).ToList();

            db.ShowtimeSeats.AddRange(showtimeSeats);
        }

        db.SaveChanges();

        // ───────────────────────────────────────────────────────────────────────
        //  BLOCK 5 — Movies
        //  Maps to: Movies table  |  PK: MovieId (AI)  |  Unique: Title
        //
        //  20 fictional titles across all genres in the analysis doc.
        //  RuntimeMinutes maps to short in the domain entity — all values fit.
        //  Description is brief; expand for richer UI display testing.
        //
        //  No FKs — this block can run independently of all others.
        // ───────────────────────────────────────────────────────────────────────

        /*
        var existingTitles = db.Movies
            .Select(m => m.Title)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var movieCandidates = new List<Movie>
        {
            new() { Title = "The Last Orbit",    Rating = "PG-13", Genre = "Sci-Fi",    RuntimeMinutes = 126, Description = "An astronaut stranded beyond the solar system races to find a signal home."                           },
            new() { Title = "Glass Harbor",      Rating = "R",     Genre = "Thriller",  RuntimeMinutes = 109, Description = "A detective unravels a conspiracy buried beneath a sleepy coastal town."                             },
            new() { Title = "Neon Riders",       Rating = "PG-13", Genre = "Action",    RuntimeMinutes = 118, Description = "Street racers discover their city's underground is run by something far worse than gangs."           },
            new() { Title = "Moonlit Harbor",    Rating = "PG",    Genre = "Drama",     RuntimeMinutes = 114, Description = "Two estranged siblings reunite at their late father's fishing village to settle his estate."          },
            new() { Title = "Final Tempo",       Rating = "PG",    Genre = "Musical",   RuntimeMinutes = 121, Description = "A prodigy conductor rediscovers passion for music after a career-ending injury."                     },
            new() { Title = "Signal Lost",       Rating = "PG-13", Genre = "Mystery",   RuntimeMinutes = 112, Description = "A radio operator starts receiving transmissions from a ship that sank forty years ago."               },
            new() { Title = "Copper Skies",      Rating = "PG-13", Genre = "Adventure", RuntimeMinutes = 123, Description = "A treasure hunter and a geologist partner to decode an ancient map before a rival syndicate does."   },
            new() { Title = "After the Ashes",   Rating = "R",     Genre = "Drama",     RuntimeMinutes = 132, Description = "Survivors of a wildfire rebuild their community — and each other — one difficult season at a time."   },
            new() { Title = "Pixel Frontier",    Rating = "PG",    Genre = "Animation", RuntimeMinutes = 97,  Description = "A young coder is pulled into the game she built and must debug it from the inside to escape."         },
            new() { Title = "Midnight Circuit",  Rating = "PG-13", Genre = "Action",    RuntimeMinutes = 115, Description = "An ex-military engineer defuses a city-wide automated security lockdown with minutes to spare."       },
            new() { Title = "Hollow Creek",      Rating = "R",     Genre = "Horror",    RuntimeMinutes = 104, Description = "Hikers in a remote gorge realize the forest is hunting them — and has been for days."                },
            new() { Title = "Sunset Protocol",   Rating = "PG-13", Genre = "Sci-Fi",    RuntimeMinutes = 128, Description = "A government simulation meant to model climate solutions becomes disturbingly self-aware."            },
            new() { Title = "Paper Tigers",      Rating = "PG",    Genre = "Comedy",    RuntimeMinutes = 99,  Description = "Office rivals accidentally swap lives for a week and discover they have more in common than they thought." },
            new() { Title = "Iron Meridian",     Rating = "PG-13", Genre = "Action",    RuntimeMinutes = 138, Description = "A decommissioned submarine crew is called back for one final, classified mission."                   },
            new() { Title = "The Quiet Garden",  Rating = "G",     Genre = "Family",    RuntimeMinutes = 88,  Description = "A girl and a retired botanist restore a forgotten greenhouse and uncover a long-lost secret."         },
            new() { Title = "Fracture Line",     Rating = "R",     Genre = "Thriller",  RuntimeMinutes = 117, Description = "A seismologist discovers the next major earthquake is being deliberately triggered."                 },
            new() { Title = "Ember & Ash",       Rating = "PG-13", Genre = "Romance",   RuntimeMinutes = 108, Description = "Two rival chefs compete on a reality show while slowly falling for each other."                     },
            new() { Title = "Stardust Junction", Rating = "PG",    Genre = "Sci-Fi",    RuntimeMinutes = 105, Description = "A small-town observatory picks up a signal that sends three friends on an impossible journey."       },
            new() { Title = "The Hollow Ones",   Rating = "R",     Genre = "Horror",    RuntimeMinutes = 113, Description = "Archaeologists awaken something beneath an ancient burial site that was sealed for good reason."      },
            new() { Title = "Blue Ridge Run",    Rating = "PG",    Genre = "Adventure", RuntimeMinutes = 101, Description = "A trail runner's solo cross-country attempt becomes a fight for survival after a storm strands her."  },
        };

        var newMovies = movieCandidates
            .Where(m => !existingTitles.Contains(m.Title))
            .ToList();

        if (newMovies.Any())
        {
            db.Movies.AddRange(newMovies);
            db.SaveChanges();
        }

        // Reload with assigned PKs — Showtime block needs MovieId.
        var movies = db.Movies.ToList();
        */


        // ───────────────────────────────────────────────────────────────────────
        //  BLOCK 6 — TheaterScreen
        //  Maps to: TheaterScreen table  |  PK: TheaterScreenId (AI)
        //  Unique composite: (LocationId, ScreenName)  — enforced here, not by DB.
        //
        //  3 screens per location: Small (80 seats), Medium (120), Large (180).
        //  Capacity overrides the entity default of 50.
        //
        //  DEPENDS ON:  locations (Block 2 must be un-commented first)
        // ───────────────────────────────────────────────────────────────────────

        /*
        // Build a HashSet of "LocationId|ScreenName" strings already in the database.
        var existingScreenKeys = db.TheaterScreens
            .Select(s => new { s.LocationId, s.ScreenName })
            .AsEnumerable()
            .Select(s => $"{s.LocationId}|{s.ScreenName}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var screenCandidates = new List<TheaterScreen>();

        foreach (var location in locations)
        {
            var screensForLocation = new[]
            {
                new TheaterScreen { LocationId = location.LocationId, ScreenName = "Small",  Capacity = 80,  IsActive = true },
                new TheaterScreen { LocationId = location.LocationId, ScreenName = "Medium", Capacity = 120, IsActive = true },
                new TheaterScreen { LocationId = location.LocationId, ScreenName = "Large",  Capacity = 180, IsActive = true },
            };

            foreach (var screen in screensForLocation)
            {
                var key = $"{screen.LocationId}|{screen.ScreenName}";
                if (!existingScreenKeys.Contains(key))
                    screenCandidates.Add(screen);
            }
        }

        if (screenCandidates.Any())
        {
            db.TheaterScreens.AddRange(screenCandidates);
            db.SaveChanges();
        }

        // Reload with assigned PKs — Showtime block needs TheaterScreenId.
        var screens = db.TheaterScreens.ToList();
        */


        // ───────────────────────────────────────────────────────────────────────
        //  BLOCK 7 — Showtime
        //  Maps to: Showtime table  |  PK: ShowtimeId (AI)
        //
        //  Showtimes are generated relative to DateTime.Today so they stay
        //  "upcoming" regardless of when the seeder runs — no stale past shows.
        //
        //  Schedule: 4 slots per day (11am / 2pm / 5pm / 8pm) across all screens
        //  at all locations, for the next 7 days.  Movies rotate through the first
        //  5 titles in the catalog.
        //
        //  Pricing:
        //    Small  screen  →  $9.99
        //    Medium screen  →  $12.99
        //    Large  screen  →  $14.99
        //
        //  Idempotency key: (TheaterScreenId, StartTime) — no DB unique constraint,
        //  so we build the composite key here and filter before insert.
        //
        //  DEPENDS ON:  movies (Block 5), screens (Block 6)
        // ───────────────────────────────────────────────────────────────────────

        /*
        var existingShowtimeKeys = db.Showtimes
            .Select(s => new { s.TheaterScreenId, s.StartTime })
            .AsEnumerable()
            .Select(s => $"{s.TheaterScreenId}|{s.StartTime:yyyyMMddHHmm}")
            .ToHashSet();

        var showtimeCandidates = new List<Showtime>();

        int[] startHours = { 11, 14, 17, 20 };

        var priceMap = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            { "Small",  9.99m  },
            { "Medium", 12.99m },
            { "Large",  14.99m },
        };

        for (int dayOffset = 0; dayOffset < 7; dayOffset++)
        {
            var date = DateTime.Today.AddDays(dayOffset);

            foreach (var screen in screens)
            {
                var price = priceMap.TryGetValue(screen.ScreenName, out var p) ? p : 12.99m;

                for (int slot = 0; slot < startHours.Length; slot++)
                {
                    var startTime = date.AddHours(startHours[slot]);
                    var key       = $"{screen.TheaterScreenId}|{startTime:yyyyMMddHHmm}";

                    if (existingShowtimeKeys.Contains(key))
                        continue;

                    // Rotate through the first 5 movies so each screen gets variety.
                    var movie = movies[(dayOffset * startHours.Length + slot) % Math.Min(5, movies.Count)];

                    showtimeCandidates.Add(new Showtime
                    {
                        MovieId         = movie.MovieId,
                        TheaterScreenId = screen.TheaterScreenId,
                        StartTime       = startTime,
                        Capacity        = screen.Capacity,
                        PricePerTicket  = price,
                        IsActive        = true,
                    });
                }
            }
        }

        if (showtimeCandidates.Any())
        {
            db.Showtimes.AddRange(showtimeCandidates);
            db.SaveChanges();
        }
        */


        // ───────────────────────────────────────────────────────────────────────
        //  BLOCK 8 — ConcessionItem
        //  Maps to: ConcessionItem table  |  PK: ConcessionItemId (AI)
        //  Unique: ItemName
        //
        //  Exactly 3 items as defined in the canonical schema reference:
        //    Popcorn  $8.00   |   Candy  $3.00   |   Drink  $4.00
        //
        //  Minimum = 5 (matches entity default) — a low-stock alert fires when
        //  QuantityInStock drops to or below this value.
        //
        //  No FKs — this block can run independently of all others.
        // ───────────────────────────────────────────────────────────────────────

        /*
        var existingItemNames = db.ConcessionItems
            .Select(ci => ci.ItemName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var concessionCandidates = new List<ConcessionItem>
        {
            new() { ItemName = "Popcorn", Price = 8.00m, QuantityInStock = 200, Minimum = 5, IsActive = true },
            new() { ItemName = "Candy",   Price = 3.00m, QuantityInStock = 150, Minimum = 5, IsActive = true },
            new() { ItemName = "Drink",   Price = 4.00m, QuantityInStock = 175, Minimum = 5, IsActive = true },
        };

        var newItems = concessionCandidates
            .Where(ci => !existingItemNames.Contains(ci.ItemName))
            .ToList();

        if (newItems.Any())
        {
            db.ConcessionItems.AddRange(newItems);
            db.SaveChanges();
        }
        */


        // ───────────────────────────────────────────────────────────────────────
        //  BLOCK 9 — Shifts
        //  Maps to: Shifts table  |  PK: ShiftId (AI)
        //  DB CHECK constraint: EndTime > StartTime
        //
        //  3 shift windows per location per role (Morning / Afternoon / Evening).
        //  Dates are anchored to tomorrow so all seeded shifts are upcoming.
        //
        //  Idempotency key: (LocationId, RoleId, StartTime) — no DB unique index,
        //  so we build the composite key here and filter before insert.
        //
        //  The Evening shift ends at midnight, which in C# is represented as
        //  the next calendar day at 00:00 — still satisfies EndTime > StartTime.
        //
        //  DEPENDS ON:  roles (Block 1), locations (Block 2)
        // ───────────────────────────────────────────────────────────────────────

        /*
        var existingShiftKeys = db.Shifts
            .Select(s => new { s.LocationId, s.RoleId, s.StartTime })
            .AsEnumerable()
            .Select(s => $"{s.LocationId}|{s.RoleId}|{s.StartTime:yyyyMMddHHmm}")
            .ToHashSet();

        var shiftCandidates = new List<Shift>();

        // (startHour, endHour) — endHour 24 means midnight next day.
        var windows = new[] { (Start: 7, End: 15), (Start: 13, End: 21), (Start: 17, End: 24) };

        var shiftAnchor = DateTime.Today.AddDays(1);   // always "upcoming"

        foreach (var location in locations)
        {
            foreach (var role in roles)
            {
                foreach (var (startHr, endHr) in windows)
                {
                    var start = shiftAnchor.AddHours(startHr);
                    var end   = endHr < 24
                                    ? shiftAnchor.AddHours(endHr)
                                    : shiftAnchor.AddDays(1);   // midnight = next calendar day

                    var key = $"{location.LocationId}|{role.RoleId}|{start:yyyyMMddHHmm}";

                    if (!existingShiftKeys.Contains(key))
                    {
                        shiftCandidates.Add(new Shift
                        {
                            LocationId = location.LocationId,
                            RoleId     = role.RoleId,
                            StartTime  = start,
                            EndTime    = end,
                        });
                    }
                }
            }
        }

        if (shiftCandidates.Any())
        {
            db.Shifts.AddRange(shiftCandidates);
            db.SaveChanges();
        }
        */


        // ───────────────────────────────────────────────────────────────────────
        //  BLOCK 10 — PayPeriods
        //  Maps to: PayPeriods table  |  PK: PayPeriodId (AI)
        //  DB CHECK constraint: EndDate >= StartDate
        //
        //  6 bi-weekly periods: 2 recently completed, 1 current, 3 upcoming.
        //  Anchor = the most recent Monday on or before today, aligned to a
        //  14-day cycle.  Adjust the anchor logic if your pay cycle starts
        //  on a different day of the week.
        //
        //  Idempotency key: StartDate (each period begins on a unique date).
        //
        //  No FKs — this block can run independently of all others.
        // ───────────────────────────────────────────────────────────────────────

        /*
        var existingPeriodStarts = db.PayPeriods
            .Select(pp => pp.StartDate)
            .ToHashSet();

        var today  = DateOnly.FromDateTime(DateTime.Today);

        // Step back to the most recent Monday.
        var daysFromMonday = ((int)today.DayOfWeek + 6) % 7;   // Mon=0 … Sun=6
        var anchor = today.AddDays(-daysFromMonday);

        var periodCandidates = new List<PayPeriod>();

        // i = -2 gives the period that started 4 weeks ago (2 past periods).
        // i =  3 gives the period starting 6 weeks from now (3 future periods).
        for (int i = -2; i <= 3; i++)
        {
            var start = anchor.AddDays(i * 14);
            var end   = start.AddDays(13);   // 14-day inclusive period

            if (!existingPeriodStarts.Contains(start))
                periodCandidates.Add(new PayPeriod { StartDate = start, EndDate = end });
        }

        if (periodCandidates.Any())
        {
            db.PayPeriods.AddRange(periodCandidates);
            db.SaveChanges();
        }
        */


        // ═══════════════════════════════════════════════════════════════════════
        //  TABLES NOT SEEDED — populated by the application at runtime
        // ═══════════════════════════════════════════════════════════════════════
        //
        //  ScheduleAssignments — managers assign employees to shifts via the
        //                        Employee Area UI; no dev-seed needed.
        //
        //  Ticket              — created by CartService at point of sale.
        //
        //  ConcessionSale      — created by ConcessionService at point of sale.
        //
        //  ConcessionSaleItem  — created alongside ConcessionSale, one row per
        //                        line item in the transaction.
        //
        //  AuditLog            — written by AuditService on every login, logout,
        //                        and CRUD action; must never be pre-populated.
        //
        //  TimeEntries         — created by clock-in / clock-out in the Employee
        //                        Area; seeding these would falsify payroll calcs.
        //
        //  Timesheets          — aggregated from TimeEntries by the payroll engine;
        //                        requires real TimeEntry data to be meaningful.
        //
        //  Payrolls            — calculated from approved Timesheets.
        //
        //  PayStubs            — issued after each payroll run.
        //
        // ═══════════════════════════════════════════════════════════════════════
    }
}