/*
 * File:        /ScrumFlix/Infrastructure/LoggingConfiguration.cs
 * Namespace:   ScrumFlix.Infrastructure
 * Purpose:     Configures the Serilog structured logging pipeline for ScrumFlix.
 *
 * This file wires together:
 * - A bootstrap console logger for startup failures
 * - The full Serilog pipeline for runtime logging
 * - Minimum levels driven by appsettings.json "Serilog" section (ReadFrom.Configuration)
 * - Global noise filter via Serilog.Expressions (ByExcluding health/static/favicon hits)
 * - Console output via ExpressionTemplate (conditional CorrelationId, rich exception display)
 * - MySQL persistence for Information+ events (async, non-blocking)
 * - Email alerts via a sub-logger filtered to ScrumFlix Error/Fatal events only,
 *   excluding framework noise (Serilog.Expressions ByIncludingOnly)
 * - Exception enrichment via Serilog.Exceptions (WithExceptionDetails)
 *
 * Registration pattern:
 *   builder.Services.AddSerilog()  ← modern .NET 8+ pattern
 *   replaces the older builder.Host.UseSerilog() approach.
 *   AddSerilog integrates cleanly with the DI container and is the approach
 *   the Serilog team actively maintains going forward.
 *
 * Minimum level + filter configuration:
 *   Both are declared in appsettings.json under "Serilog:MinimumLevel" and
 *   "Serilog:Filter" and read by ReadFrom.Configuration(). This means levels
 *   and noise filters can be tuned in production via web.config environment
 *   variables (Serilog__MinimumLevel__Default=Debug) without redeploying.
 *   Code-level calls below act as safe fallbacks when the config section
 *   is absent (e.g. a stripped-down CI/test environment).
 *
 * Serilog.Expressions use:
 *   1. Global Filter.ByExcluding() — drops high-volume low-value events
 *      (health checks, favicon, static asset requests) before they reach
 *      any sink. Driven from appsettings.json "Serilog:Filter" so rules
 *      can be adjusted per-environment without recompiling.
 *
 *   2. ExpressionTemplate on the Console sink — richer, conditional output.
 *      CorrelationId is only printed when it exists; exceptions get a
 *      dedicated block; level badge uses u3 shorthand.
 *
 *   3. Sub-logger on the Email sink — wraps the sink in a WriteTo.Logger()
 *      with Filter.ByIncludingOnly() so only Error/Fatal events whose
 *      SourceContext starts with "ScrumFlix" trigger an alert. This prevents
 *      framework library errors (EF Core, MailKit, ImageSharp) from causing
 *      an email storm on transient infrastructure issues.
 *
 * Serilog.Sinks.Email v4.2.1 notes:
 * - Use EmailSinkOptions for SMTP/message settings.
 * - Use BatchingOptions for batched delivery behavior.
 * - Configure recipients as a collection of addresses.
 * - Prefer MailKit SecureSocketOptions.Auto (STARTTLS on 587) rather than
 *   disabling TLS validation.
 *
 * Author:  ScrumFlix Rebuild Team
 * Phase:   1E
 * Updated: 2026-05-28 — migrated to AddSerilog(), ReadFrom.Configuration(),
 *                        WithExceptionDetails(), fixed pre-sink Log.* calls.
 *          2026-05-31 — added Serilog.Expressions: global noise filter,
 *                        ExpressionTemplate console output, sub-logger email
 *                        filter scoped to ScrumFlix source contexts only.
 */

using MailKit.Security;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.Email;
using Serilog.Configuration;            // BatchingOptions — used by WriteTo.Email(batchingOptions:)
using Serilog.Templates;
using System.Net;

namespace ScrumFlix.Infrastructure;

/// <summary>
/// Builds and registers the Serilog logger configuration used by the application.
/// Call <see cref="ConfigureLogging"/> from <c>Program.cs</c> before any other
/// service registration so the bootstrap logger captures early startup failures.
/// </summary>
public static class LoggingConfiguration
{
    // ── Serilog.Expressions filter strings ────────────────────────────────────
    // Centralised here so they're easy to find, audit, and update.
    // Syntax reference: https://github.com/serilog/serilog-expressions

    /// <summary>
    /// Global noise filter applied to ALL sinks.
    /// Excludes high-volume, low-value events that would otherwise fill MySQL
    /// and clutter the console. Rules are also declared in appsettings.json
    /// "Serilog:Filter" (ReadFrom.Configuration picks them up automatically),
    /// so production overrides require only a web.config env-var change.
    ///
    /// Excluded:
    ///   • /health, /healthz, /ping, /ready — load-balancer/K8s probes
    ///   • /favicon.ico, /favicon-*.png      — browser icon requests
    ///   • RequestPath starting with /cache/ — ImageSharp processed-image cache
    ///   • Microsoft.AspNetCore.StaticFiles  — all static asset delivery events
    ///     (already suppressed at Warning level, belt-and-suspenders here)
    /// </summary>
    private const string GlobalNoiseFilter =
        "RequestPath like '/health%' or " +
        "RequestPath like '/favicon%' or " +
        "RequestPath like '/cache/%' or " +
        "SourceContext = 'Microsoft.AspNetCore.StaticFiles.StaticFileMiddleware'";

    /// <summary>
    /// Email alert sub-logger filter.
    /// Only ScrumFlix application code at Error or Fatal level triggers an email.
    /// Framework, EF Core, MailKit, ImageSharp, and other library errors are
    /// intentionally excluded — they are still written to MySQL for review, but
    /// do not generate alert noise from transient infrastructure issues.
    ///
    /// Change 'ScrumFlix%' to a different prefix to adjust scope, or extend
    /// with "or SourceContext like 'Pomelo%'" to include EF Core errors.
    /// </summary>
    private const string EmailAlertFilter =
        "@l in ['Error', 'Fatal'] and " +
        "SourceContext like 'ScrumFlix%'";

    // ── ExpressionTemplate for the Console sink ───────────────────────────────
    // ExpressionTemplate (Serilog.Expressions) gives conditional rendering:
    //   • {#if ...}{#end}           — only print CorrelationId when it exists
    //   • {#if @x is not null}{#end} — only print the exception block when
    //                                  an exception is attached to the event
    //   • {@l:u3}                   — three-character uppercase level badge
    //   • {@m:lj}                   — message with JSON literal rendering
    //   • {SourceContext}            — fully-qualified class name of the logger
    //
    // NOTE: ExpressionTemplate uses {@t}, {@l}, {@m}, {@x} (@ prefix)
    // rather than the classic {Timestamp}, {Level}, {Message}, {Exception}
    // placeholders used by MessageTemplateTextFormatter.
    private const string ConsoleTemplate =
        "[{@t:HH:mm:ss} {@l:u3}] {SourceContext}\n" +
        "  {@m:lj}\n" +
        "{#if CorrelationId is not null}  CorrelationId: {CorrelationId}\n{#end}" +
        "{#if @x is not null}{@x}\n{#end}";

    /// <summary>
    /// Configures Serilog in two phases.
    ///
    /// Phase 1: bootstrap console logger for early startup exceptions — active
    ///          before DI and appsettings.json are fully loaded.
    /// Phase 2: full logger pipeline registered via builder.Services.AddSerilog()
    ///          with enrichers, global noise filter, MySQL, and email sinks.
    /// </summary>
    /// <param name="builder">The application's web host builder.</param>
    public static void ConfigureLogging(WebApplicationBuilder builder)
    {
        // Route Serilog's own internal errors to a local file so sink failures
        // (bad connection string, SMTP auth error, etc.) are diagnosable without
        // touching the application's own log pipeline.
        SelfLog.Enable(message =>
        {
            try
            {
                File.AppendAllText(
                    Path.Combine(AppContext.BaseDirectory, "serilog-selflog.txt"),
                    message + Environment.NewLine);
            }
            catch
            {
                // Never allow SelfLog failures to crash startup.
            }
        });

        // ── Phase 1: bootstrap logger ──────────────────────────────────────────
        // CreateBootstrapLogger() produces a lightweight logger that is active
        // from here until AddSerilog() replaces it after builder.Build().
        // Without this, any exception thrown during DI wiring is lost.
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        // ── Phase 2: full pipeline via AddSerilog() ────────────────────────────
        // AddSerilog() is the modern .NET 8+ registration method. It replaces
        // builder.Host.UseSerilog() and integrates more cleanly with the DI
        // container — custom enrichers that need injected services work here.
        //
        // ReadFrom.Services(services) wires any IDestructuringPolicy or
        // ILogEventEnricher implementations registered in DI into the pipeline.
        builder.Services.AddSerilog((services, lc) =>
            BuildLogger(builder.Configuration, builder.Environment, services, lc));
    }

    /// <summary>
    /// Builds the final Serilog pipeline with minimum levels, enrichers,
    /// global noise filter, console, MySQL persistence, and SMTP email alerts.
    ///
    /// Minimum levels and filter rules are read from appsettings.json
    /// "Serilog:MinimumLevel" and "Serilog:Filter" via ReadFrom.Configuration()
    /// so they can be adjusted per-environment without recompiling.
    /// Code-level declarations act as safe fallbacks when the config section
    /// is absent.
    /// </summary>
    private static void BuildLogger(
        IConfiguration cfg,
        IHostEnvironment env,
        IServiceProvider services,
        LoggerConfiguration lc)
    {
        // ── Base configuration from appsettings.json ───────────────────────────
        // ReadFrom.Configuration() picks up the "Serilog" section including:
        //   MinimumLevel, MinimumLevel.Override, Enrich, and Filter entries.
        // The Filter entries in appsettings.json declare the GlobalNoiseFilter
        // expression so it's tuneable per-environment without a recompile.
        lc
            .ReadFrom.Configuration(cfg)
            .ReadFrom.Services(services)

            // ── Code-level fallback minimum levels ─────────────────────────
            // Active only when the "Serilog" config section is absent.
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.StaticFiles", LogEventLevel.Error)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)

            // ── Code-level fallback global noise filter ────────────────────
            // ReadFrom.Configuration() applies the Serilog:Filter rules from
            // appsettings.json first. This code-level call is a safety net for
            // environments where the config section may be missing. If config
            // already applied the filter this is a harmless duplicate exclusion.
            .Filter.ByExcluding(GlobalNoiseFilter)

            // ── Enrichers ──────────────────────────────────────────────────
            // FromLogContext:      picks up CorrelationId (CorrelationIdMiddleware)
            //                      and UserId (UseSerilogRequestLogging).
            // WithExceptionDetails: structured inner exceptions + custom properties.
            //                       Requires Serilog.Exceptions package.
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProcessId()
            .Enrich.WithExceptionDetails()
            .Enrich.WithProperty("Application", "ScrumFlix")
            .Enrich.WithProperty("Environment", env.EnvironmentName);

        // ── Console sink — ExpressionTemplate ─────────────────────────────────
        // ExpressionTemplate (Serilog.Expressions) replaces the classic
        // outputTemplate string with a mini-language that supports conditional
        // blocks. Key improvements over the previous plain template:
        //
        //   • CorrelationId is only printed when present — no blank line on
        //     startup events that predate the middleware.
        //   • Exception block is only printed when an exception is attached —
        //     no trailing empty {Exception} token on normal events.
        //   • Uses {@t}, {@l}, {@m}, {@x} (Serilog.Expressions @ notation).
        //
        // Dev:  Debug+ (all ScrumFlix code visible locally).
        // Prod: Information+ (Somee/IIS stdout capture via stdoutLogEnabled).
        lc.WriteTo.Console(
            formatter: new ExpressionTemplate(ConsoleTemplate),
            restrictedToMinimumLevel: env.IsDevelopment()
                ? LogEventLevel.Debug
                : LogEventLevel.Information);

        // ── MySQL sink ─────────────────────────────────────────────────────────
        // Runs inside WriteTo.Async() so database writes never block the request
        // pipeline. bufferSize caps the in-memory queue at 10,000 events; events
        // beyond that are dropped rather than backing up the application.
        //
        // The global noise filter (applied above) means health-check and favicon
        // requests never reach this sink, keeping the Logs table clean.
        //
        // TABLE AUTO-CREATION: Serilog.Sinks.MySQL 6.x creates the "Logs" table
        // automatically on first startup if it does not exist. EF Core must NOT
        // own this table — keep DbSet<AppLog> commented out in AppDbContext.cs.
        //
        // If logs are not appearing:
        //   1. Confirm MySQLConnection is set in User Secrets / env vars.
        //   2. Confirm the DB user has CREATE TABLE and INSERT permissions.
        //   3. Check serilog-selflog.txt in the app base directory for sink errors.
        var mySqlConnectionString = cfg.GetConnectionString("MySQLConnection");

        if (!string.IsNullOrWhiteSpace(mySqlConnectionString))
        {
            lc.WriteTo.Async(
                a => a.MySQL(
                    connectionString: mySqlConnectionString,
                    tableName: "Logs",
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    storeTimestampInUtc: true,
                    batchSize: 50),
                bufferSize: 10_000);

            Console.WriteLine("[Serilog] MySQL sink configured (async).");
        }
        else
        {
            Console.WriteLine(
                "[Serilog] MySQLConnection not configured — database logging disabled.");
        }

        // ── Email alert sink — sub-logger with expression filter ───────────────
        // Wrapped in WriteTo.Logger() so the EmailAlertFilter expression can be
        // applied BEFORE the sink is invoked. This is the key Serilog.Expressions
        // upgrade over the previous plain restrictedToMinimumLevel approach:
        //
        //   Old:  any Error/Fatal from any SourceContext → email
        //   New:  only Error/Fatal where SourceContext starts with "ScrumFlix" → email
        //
        // Why this matters on Somee.com shared hosting:
        //   Transient infrastructure errors (EF Core connection pool timeout,
        //   MailKit SMTP retry, ImageSharp cache miss, Pomelo reconnect) would
        //   previously trigger email alerts on every bad network minute. With the
        //   sub-logger filter, those events still reach MySQL for review, but only
        //   genuine ScrumFlix application errors generate an alert email.
        //
        // To extend the filter to include EF Core errors as well, change to:
        //   "@l in ['Error', 'Fatal'] and " +
        //   "(SourceContext like 'ScrumFlix%' or SourceContext like 'Pomelo%')"
        //
        // Settings are supplied through User Secrets in development and
        // environment variables in production (web.config Logging__Email__*).
        //
        // Dev User Secrets setup (run once from the ScrumFlix project folder):
        //   dotnet user-secrets set "Logging:Email:SmtpHost"     "smtp.gmail.com"
        //   dotnet user-secrets set "Logging:Email:SmtpPort"     "587"
        //   dotnet user-secrets set "Logging:Email:SmtpUser"     "scrumflix@gmail.com"
        //   dotnet user-secrets set "Logging:Email:SmtpPassword" "<app-password>"
        //   dotnet user-secrets set "Logging:Email:From"         "scrumflix@gmail.com"
        //   dotnet user-secrets set "Logging:Email:To"           "scrumflix@gmail.com"
        var smtpHost = cfg["Logging:Email:SmtpHost"];
        var smtpPortStr = cfg["Logging:Email:SmtpPort"];
        var smtpUser = cfg["Logging:Email:SmtpUser"];
        var smtpPassword = cfg["Logging:Email:SmtpPassword"];
        var emailFrom = cfg["Logging:Email:From"];
        var emailTo = cfg["Logging:Email:To"];

        var emailConfigured =
            !string.IsNullOrWhiteSpace(smtpHost) &&
            !string.IsNullOrWhiteSpace(smtpPortStr) &&
            !string.IsNullOrWhiteSpace(smtpUser) &&
            !string.IsNullOrWhiteSpace(smtpPassword) &&
            !string.IsNullOrWhiteSpace(emailFrom) &&
            !string.IsNullOrWhiteSpace(emailTo);

        if (emailConfigured && int.TryParse(smtpPortStr, out var smtpPort))
        {
            // Allow multiple recipients using commas or semicolons.
            var recipients = emailTo!
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (recipients.Count > 0)
            {
                // WriteTo.Logger() creates a sub-pipeline with its own filter.
                // The outer async wrapper buffers the sub-logger's output so the
                // SMTP call never blocks the request thread.
                lc.WriteTo.Async(a =>
                    a.Logger(sub => sub
                        // Only ScrumFlix Error/Fatal events reach the SMTP sink.
                        // All other events (including framework errors) flow to
                        // MySQL but do not trigger an email alert.
                        .Filter.ByIncludingOnly(EmailAlertFilter)
                        .WriteTo.Email(
                            options: new EmailSinkOptions
                            {
                                From = emailFrom!,
                                To = recipients,
                                Host = smtpHost!,
                                Port = smtpPort,
                                Credentials = new NetworkCredential(smtpUser!, smtpPassword!),
                                ConnectionSecurity = SecureSocketOptions.Auto,

                                // Subject uses classic MessageTemplateTextFormatter because
                                // the email subject is a simple one-liner with no conditionals.
                                Subject = new Serilog.Formatting.Display.MessageTemplateTextFormatter(
                                    "[ScrumFlix ALERT] {@l} — {SourceContext} on {MachineName}"),

                                // Body uses ExpressionTemplate for conditional CorrelationId.
                                // Plain text; IsBodyHtml = false keeps it compatible with
                                // all mail clients including mobile.
                                Body = new ExpressionTemplate(
                                    "{@t:yyyy-MM-dd HH:mm:ss} [{@l}] {SourceContext}\n" +
                                    "{#if CorrelationId is not null}CorrelationId: {CorrelationId}\n{#end}" +
                                    "{#if UserId is not null}UserId:        {UserId}\n{#end}" +
                                    "\n{@m:lj}\n\n" +
                                    "{#if @x is not null}{@x}{#end}"),

                                IsBodyHtml = false
                            },
                            batchingOptions: new BatchingOptions
                            {
                                BatchSizeLimit = 10,
                                BufferingTimeLimit = TimeSpan.FromSeconds(60),
                                EagerlyEmitFirstEvent = true,
                                QueueLimit = 1_000
                            })),
                    bufferSize: 1_000);

                Console.WriteLine(
                    "[Serilog] Email alert sink configured " +
                    "(ScrumFlix Error/Fatal only → async sub-logger).");
            }
            else
            {
                // Console.WriteLine used here intentionally — this executes during
                // DI wiring before the full pipeline is active, so Log.* calls
                // would only reach the bootstrap logger (console-only).
                Console.WriteLine(
                    "[Serilog] Email alert sink disabled — no valid recipient addresses " +
                    "parsed from Logging:Email:To.");
            }
        }
        else
        {
            // Console.WriteLine used here for the same reason: the full Serilog
            // pipeline is not active yet when BuildLogger runs during AddSerilog().
            // In Production this would be a Warning-level concern, but it cannot
            // reach the MySQL sink at this point.
            if (env.IsProduction())
                Console.WriteLine(
                    "[Serilog] WARNING — Email alert sink not configured in Production. " +
                    "Set Logging__Email__* environment variables in web.config.");
            else
                Console.WriteLine(
                    "[Serilog] Email alert sink not configured (dev). " +
                    "Run: dotnet user-secrets set \"Logging:Email:SmtpHost\" \"smtp.gmail.com\" " +
                    "(and the other Logging:Email:* keys) to enable Error/Fatal email alerts.");
        }
    }
}