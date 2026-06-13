/*
 * File:        /ScrumFlix/Program.cs
 * Namespace:   (top-level statements)
 * Purpose:     Application entry point.
 *
  * Purpose:     Application entry point — Phase 1E canonical rebuild.
 *
 *              CHANGES FROM PHASE 1C/1D:
 *              - Serilog two-stage logging pipeline added via
 *                LoggingConfiguration.ConfigureLogging().  Replaces the default
 *                Microsoft.Extensions.Logging pipeline entirely.
 *              - NetEscapades.AspNetCore.SecurityHeaders middleware added via
 *                SecurityHeadersConfiguration.BuildPolicy().  Placed first in the
 *                pipeline so headers are applied to every response including errors.
 *              - SerilogRequestLogging middleware added after UseStaticFiles so
 *                static file hits do not generate request log events (reduces noise).
 *              - EF Core .LogTo(Console.WriteLine) removed — Serilog now owns all
 *                log output; EF events flow through ILogger<T> automatically.
 *
 *              CHANGES FROM PHASE 1E (Phase 2):
 *              - ISystemAccountProvider / SystemAccountProvider registered as
 *                singleton and initialized in the startup block.
 *              - RoleAuthorizationFilter registered as scoped and added to
 *                the global MVC filter pipeline.
 *              - SERVICE REGISTRATION ORDER updated to include new Phase 2 entries.
 *
 *              MIDDLEWARE ORDER (must not be changed):
 *                UseSecurityHeaders                     ← Phase 1E (new, must be first)
 *                UseHttpsRedirection
 *                UseStaticFiles
 *                UseSerilogRequestLogging               ← Phase 1E (new, after static files)
 *                UseRouting
 *                UseSession
 *                UseAuthorization
 *                MapControllerRoute
 *
 *              SERVICE REGISTRATION ORDER:
 *                1. Logging (Serilog — must be before all other services)
 *                2. Database (EF Core / Pomelo)
 *                3. Session + DistributedMemoryCache
 *                4. HttpContextAccessor
 *                5. Application services (Cart, Auth, Audit)
 *                6. Filters
 *                7. MVC
 *
 * Phase 3 changes (backend alignment patch):
 *   - SeatService registered as Scoped (#30 / P3-4)
 *   - QrCodeService registered as Singleton (P3-9 — stateless, safe to share)
 *
 * Sprint S4 changes (email service):
 *   - IEmailService / EmailService registered as Scoped
 *   - Startup log updated to report transactional email config status
 *   - SMTP config lives under Email:* keys (separate from Logging:Email:* Serilog sink)
 *
 * Sprint S5 Serilog upgrades:
 *   - ConfigureLogging() now uses builder.Services.AddSerilog() (modern .NET 8+ pattern)
 *     instead of builder.Host.UseSerilog(). Improves DI integration.
 *   - Minimum levels moved to appsettings.json "Serilog" section; read via
 *     ReadFrom.Configuration() so production verbosity is tunable via env vars.
 *   - Serilog.Exceptions added — WithExceptionDetails() enriches exception events
 *     with inner exceptions, custom properties, and structured stack frames.
 *   - CorrelationIdMiddleware added — pushes X-Correlation-Id into LogContext for
 *     every request so all log events for a request share one traceable ID.
 *     Registered BEFORE UseSerilogRequestLogging() so request log events carry it.
 *   - Unreachable app.Logger line after app.Run() removed (app.Run() is blocking).
 *
 *              MIDDLEWARE ORDER (must not be changed):
 *                UseSecurityHeaders                     ← Phase 1E (must be first)
 *                UseHttpsRedirection
 *                UseStaticFiles
 *                CorrelationIdMiddleware                 ← Phase 5 (before request logging)
 *                UseSerilogRequestLogging               ← Phase 1E (after static files)
 *                UseRouting
 *                UseSession
 *                UseAuthorization
 *                MapControllerRoute
 *
 * All prior Phase 1/2/3/4 registrations and middleware order are otherwise unchanged.
 */

using Microsoft.AspNetCore.Http.Features;
using QuestPDF.Infrastructure;
using ScrumFlix.Filters;
using ScrumFlix.Hubs;
using ScrumFlix.Services.BackgroundQueue;
using ScrumFlix.Services.Progress;
using ScrumFlix.Services.TMDB;
using Serilog;
using SixLabors.ImageSharp.Web.Caching;
//using SixLabors.ImageSharp.Web.DependencyInjection;
//using SixLabors.ImageSharp.Web.Providers;

// ── Stage 1 bootstrap logger ───────────────────────────────────────────────
// Configured inside LoggingConfiguration.ConfigureLogging().
// Any exception thrown before builder.Build() is captured here.
var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// QuestPDF — Community license declaration
// Must be called once before any IDocument.GeneratePdf() call.
// Community license is free for open-source and internal projects.
// See: https://www.questpdf.com/license/
// ============================================================================
QuestPDF.Settings.License = LicenseType.Community;

// ── Logging — Serilog (must be registered before all other services) ───────
//
// ConfigureLogging() performs two-stage Serilog initialization:
//   Stage 1: bootstrap console logger (active during DI wiring)
//   Stage 2: full pipeline — Console (dev) + MySQL (async) + Email alerts (async)
//
// After this call, all ILogger<T> injections resolve through Serilog.
// See Infrastructure/LoggingConfiguration.cs for sink and enricher details.
LoggingConfiguration.ConfigureLogging(builder);

// ── Database — EF Core / Pomelo MySQL ─────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("MySQLConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'MySQLConnection' not found. " +
        "Run: dotnet user-secrets set \"ConnectionStrings:MySQLConnection\" \"Server=...\"");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    // MySqlServerVersion is hardcoded to 8.0.45 to avoid ServerVersion.AutoDetect(),
    // which opens a live TCP connection to Aiven at startup/DI-resolve time.
    // AutoDetect caused "Connect Timeout expired" errors under cold start or
    // when Aiven is briefly unreachable. The hardcoded version is safe —
    // Pomelo only uses it to select SQL dialect features, not for runtime queries.
    // Update the patch version here if Aiven upgrades the MySQL engine.
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 45)),
           // Transient-failure resiliency: Somee → Aiven traverses the public
           // internet, so brief connection drops (network blips, Aiven
           // maintenance/failover) are expected. Retry up to 3 times with
           // exponential backoff capped at 5s before surfacing the exception.
           // First observed: SeatReservationExpiryService email alert,
           // June 11 2026 ("Unable to connect to any of the specified MySQL hosts").
           // NOTE: a retrying execution strategy FORBIDS bare BeginTransaction —
           // every explicit transaction must be wrapped in
           // Database.CreateExecutionStrategy().ExecuteAsync(...).
           // CartController (checkout) does this; follow that pattern for any
           // new explicit transactions.
           mySqlOptions => mySqlOptions.EnableRetryOnFailure(
               maxRetryCount: 3,
               maxRetryDelay: TimeSpan.FromSeconds(5),
               errorNumbersToAdd: null))

           // EnableSensitiveDataLogging: shows parameter values in EF log output.
           // Development only — never enable in Production (leaks PII).
           .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())

           // EF Core log events now flow through Serilog via ILoggerFactory.
           // The old .LogTo(Console.WriteLine) is removed — Serilog owns all output.
           .UseLoggerFactory(LoggerFactory.Create(lb => lb.AddSerilog()));
});

// ── Session — shopping cart persistence ───────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = ".ScrumFlix.Session";
});

// ── HTTP context accessor — required by CartService and AuthService ────────
builder.Services.AddHttpContextAccessor();

/// ============================================================================
/// HttpClientFactory Registration:
/// Enables dependency injection support for HttpClient usage.
/// ============================================================================
builder.Services.AddHttpClient();

// ── Application services ──────────────────────────────────────────────────

// Phase 1 / 2 services (unchanged)
// CartService: session-based shopping cart (retained from legacy — no schema changes).
builder.Services.AddScoped<CartService>();

// AuthService: login, logout, lockout, and plaintext-to-hash password migration.
// Implementation added in Phase 2 (Services/AuthService.cs + IAuthService interface).
builder.Services.AddScoped<IAuthService, AuthService>();

// AuditService: writes AuditLog rows for all security-sensitive actions.
// Implementation added in Phase 2 (Services/AuditService.cs + IAuditService interface).
builder.Services.AddScoped<IAuditService, AuditService>();

// SystemAccountProvider: singleton that resolves the web.sales UserId once at
// startup and caches it.  Phase 3 checkout services inject this to populate
// Ticket.UserAtSale and ConcessionSale.UserId for public-facing purchases.
builder.Services.AddSingleton<ISystemAccountProvider, SystemAccountProvider>();

// Phase 3 — new service registrations
// SeatService: canonical ShowtimeSeat availability queries and atomic reservation.
// Scoped — needs a fresh DbContext per request (atomic conditional UPDATE pattern).
builder.Services.AddScoped<SeatService>();

builder.Services.AddScoped<IDatabaseBackupService, DatabaseBackupService>();

// Phase 4.0 shared progress framework — singleton because it owns the
// cross-request cancellation registry (Cancel() arrives via a separate
// SignalR connection from the operation that created the reporter).
builder.Services.AddSingleton<IProgressReporterFactory, ProgressReporterFactory>();

// Phase 4.3 background-queue redesign — in-process Channel-based work queue
// (System.Threading.Channels, BCL only; no Hangfire/Quartz). The triggering
// HTTP request (TMDb sync, database backup) enqueues a work item and returns
// immediately; QueuedHostedService drains the channel, running each item in its
// own DI scope. Singleton: the channel must outlive any single request.
// NOTE (Somee.com): an in-process queue loses queued/in-flight items on an
// app-pool recycle — not a regression, since the old synchronous operation died
// with its recycled request just the same. Persistence is intentionally out of scope.
builder.Services.AddSingleton<IBackgroundTaskQueue>(_ =>
    new BackgroundTaskQueue(capacity: 10));
builder.Services.AddHostedService<QueuedHostedService>();

// SeatReservationExpiryService: background worker that polls every 60 seconds
// and releases expired seat holds via SeatService.ReleaseExpiredReservationsAsync().
// Registered as a hosted service — runs for the lifetime of the application.
builder.Services.AddHostedService<SeatReservationExpiryService>();

// QrCodeService: stateless PNG QR code generator (no DbContext dependency).
// Singleton — safe to share across requests; avoids repeated object allocation.
builder.Services.AddSingleton<QrCodeService>();

// IEmailService / EmailService: transactional email for order confirmations,
// staff notifications, low-stock alerts, and PDF report delivery.
// Scoped — reads IConfiguration per-request; no shared mutable state.
// SMTP credentials must be set via User Secrets or environment variables:
//   Email:SmtpHost | Email:SmtpPort | Email:SmtpUser | Email:SmtpPassword
//   Email:From     | Email:FromName | Email:AdminTo
// If any required key is missing, EmailService logs a warning and skips delivery.
builder.Services.AddScoped<IEmailService, EmailService>();

// ── TMDb sync service ────────────────────────────────────────────────
// Registers ITmdbSyncService + TmdbSyncService (Scoped) and the named
// "TmdbClient" HttpClient with AddStandardResilienceHandler().
// API key is read from "Tmdb:ApiKey" (User Secrets / env var).
builder.Services.ConfigureTmdb(builder.Configuration);

/// ============================================================================
/// Service Registration:
/// Registers the TMDb image service for dependency injection.
/// ============================================================================

builder.Services.AddScoped<ITmdbImageService, TmdbImageService>();

// ── Global filters ─────────────────────────────────────────────────────────
// RoleAuthorizationFilter: enforces [RequireRole] attributes across all controllers.
// Uses session RoleId — must run AFTER UseSession() is in the middleware pipeline.
builder.Services.AddScoped<RoleAuthorizationFilter>();
builder.Services.AddScoped<BookingDiagnosticsFilter>();

// ── MVC ────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<RoleAuthorizationFilter>();
    options.Filters.Add<BookingDiagnosticsFilter>();
})
.AddJsonOptions(opts =>
    opts.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

// ============================================================================
// Memory Cache
// ============================================================================

builder.Services.AddMemoryCache();

// ============================================================================
// ImageSharp.Web
// ============================================================================

builder.Services.AddImageSharp(options =>
{
    options.BrowserMaxAge = TimeSpan.FromDays(30);
    options.CacheMaxAge = TimeSpan.FromDays(365);
    options.CacheHashLength = 12;
})
.SetCache<PhysicalFileSystemCache>();


// ── SignalR ────────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();


// ═════════════════════════════════════════════════════════════════════════════
var app = builder.Build();
// ═════════════════════════════════════════════════════════════════════════════


// ── Security headers (must be first in the pipeline) ──────────────────────
//
// Applied before everything else so every response — including 404s, 500s,
// and static files — carries the full security header set.
//
// Policy is defined in Infrastructure/SecurityHeadersConfiguration.cs.
// Key headers applied:
//   Strict-Transport-Security  (1 year, includeSubDomains)
//   Content-Security-Policy    (CSP tuned for Bootstrap CDN + Google Fonts)
//   X-Frame-Options            DENY
//   X-Content-Type-Options     nosniff
//   Referrer-Policy            strict-origin-when-cross-origin
//   Permissions-Policy         disables camera, mic, geo, payment, USB, etc.
//   Cross-Origin-Opener-Policy same-origin
//   Cross-Origin-Resource-Policy same-origin
//   Server header              removed
// Program.cs
if (app.Environment.IsDevelopment())
    app.UseSecurityHeaders(SecurityHeadersConfiguration.BuildDevPolicy());
else
    app.UseSecurityHeaders(SecurityHeadersConfiguration.BuildPolicy());

// ── Exception handling ─────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    // Developer exception page — full stack trace, request details, route data
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // HSTS is handled by the security headers middleware above.
    // Do NOT call app.UseHsts() — that would produce a duplicate header.
}

// ── Status code pages (404, 403, etc.) ────────────────────────────────────
// Renders friendly pages for non-exception HTTP errors.
// /Home/Error/{code} handles 404 Not Found, 403 Forbidden, etc.
app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

// ── Standard middleware ────────────────────────────────────────────────────
app.UseHttpsRedirection();
app.UseStaticFiles();

// ── Serilog request logging ───────────────────────────────────────────────
//
// CorrelationIdMiddleware must run first so the CorrelationId property is in
// LogContext before UseSerilogRequestLogging emits the request summary event.
// The middleware reads X-Correlation-Id from the incoming request (or generates
// one), echoes it on the response, and pushes it into Serilog's async-local
// LogContext for the duration of the request.
app.UseMiddleware<CorrelationIdMiddleware>();

// Placed after UseStaticFiles so .css/.js/.png hits are NOT logged —
// only controller action requests produce a request log event.
//
// Each event includes: method, path, status code, elapsed ms, user agent.
// Properties are enriched with all Serilog enrichers (MachineName, ThreadId, etc.)
//
// Output format (console / MySQL):
//   HTTP GET /Home/HomeDashboard responded 200 in 34.7 ms
app.UseSerilogRequestLogging(opts =>
{
    opts.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

    // Attach the request path and method to the log event as structured properties
    // so they can be queried/indexed in the MySQL Logs table.
    opts.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());

        // Attach UserId from session if the user is logged in.
        // This ties every request log event back to a specific user for audit tracing.
        var sessFeature = httpContext.Features.Get<ISessionFeature>();
        if (sessFeature?.Session != null && sessFeature.Session.IsAvailable)
        {
            var userId = sessFeature.Session.GetInt32(ScrumFlix.Services.AuthService.SessionUserId);
            if (userId.HasValue) diagnosticContext.Set("UserId", userId.Value);
        }
    };
});

// ============================================================================
// ImageSharp Middleware
// IMPORTANT:
// Must be BEFORE routing/endpoints.
// ============================================================================

app.UseImageSharp();

app.UseRouting();

// Session must be activated before UseAuthorization so the role authorization
// filter (Phase 2) can read the authenticated user from session.
app.UseSession();
app.UseAuthorization();


// ── SignalR Hubs ────────────────────────────────────────────────────────────────
app.MapHub<ScheduleHub>("/scheduleHub");
app.MapHub<TmdbProgressHub>("/tmdbSyncHub");
app.MapHub<ProgressHub>("/progressHub");

// ── Routing ────────────────────────────────────────────────────────────────
// Admin area — requires [Area("Admin")] on controllers + [Authorize(Roles="Admin")]
// (authorization attributes applied in Phase 2).
app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=AdminHome}/{action=AdminDashboard}/{id?}");

// Employee area — Phase 4 (time clock, POS, schedule view).
app.MapControllerRoute(
    name: "employee",
    pattern: "{area:exists}/{controller=EmployeeHome}/{action=EmployeeDashboard}/{id?}");

// Default public route.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=HomeDashboard}/{id?}");

// ── Database startup check (development / local only) ─────────────────────
//
// EnsureCreated() is safe for local development against a fresh MySQL instance.
// NEVER call this against the Aiven Cloud defaultdb — the canonical schema
// is already in place and is the source of truth.
//
// TO ENABLE SEEDING:
//   1. Un-comment individual table blocks in SampleDataSeederFull.cs.
//   2. Un-comment SampleDataSeederFull.Seed(db) below.
//   3. Run locally against a dev database only.
//   4. Comment both back out before committing or deploying.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        db.Database.EnsureCreated();
        // SampleDataSeederFull.Seed(db);   // ← un-comment to seed dev database
        logger.LogInformation("Database ready — seeding disabled (Phase 3).");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex,
            "Database startup check failed. " +
            "Ensure the database is reachable and credentials are set via User Secrets. " +
            "If running against Aiven Cloud, EnsureCreated() is a no-op on an existing schema.");
    }
}

// ── SystemAccountProvider initialization ──────────────────────────────────
// Resolves the web.sales UserId once from the database and caches it for the
// application lifetime.  Must run AFTER the startup DB check so EnsureCreated()
// has had a chance to provision the schema on a fresh local database.
// Phase 3 checkout services will read this value via ISystemAccountProvider.
await app.Services
    .GetRequiredService<ISystemAccountProvider>()
    .InitializeAsync();

// ── Startup confirmation ───────────────────────────────────────────────────
// Logged after all middleware is wired and the app is about to start accepting
// requests.  Visible in MySQL Logs table and (in dev) in the console.
Log.Information(
    "ScrumFlix started — Environment: {Environment} | Security headers: active | " +
    "MySQL logging: active | Auth: session-based BCrypt | SeatService: active | " +
    "SeatExpiryService: active | QrCodeService: active | EmailService: active | " +
    "TmdbSync: {TmdbStatus} | Serilog email alerts: {SerilogEmailStatus} | " +
    "Transactional email: {TransactionalEmailStatus}",
    app.Environment.EnvironmentName,
    app.Configuration["Tmdb:ApiKey"] is not null ? "active" : "key not set",
    app.Configuration["Logging:Email:SmtpHost"] is not null ? "active" : "not configured",
    app.Configuration["Email:SmtpHost"] is not null ? "active" : "not configured");

app.Run();

// ── Shutdown flush ─────────────────────────────────────────────────────────
// Ensures the async MySQL and Email sink buffers are flushed before the process
// exits.  Without this, the last few log events before shutdown may be lost.
Log.CloseAndFlush();

// ── WebApplicationFactory test hook ───────────────────────────────────────
// WebApplicationFactory<Program> (Microsoft.AspNetCore.Mvc.Testing) requires
// the Program type to be accessible from the test assembly. Top-level statement
// files in C# generate an implicit internal Program class; this stub makes it
// public so the integration test project can reference it without InternalsVisibleTo.
//
// Place this at the very end of Program.cs, after app.Run() and Log.CloseAndFlush().
public partial class Program { }
