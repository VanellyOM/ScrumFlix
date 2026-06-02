/*
 * File: /ScrumFlix/Infrastructure/TmdbConfiguration.cs
 * Namespace: ScrumFlix.Infrastructure
 * Purpose: Registers the TMDb HTTP client pipeline and TmdbSyncService with
 *          the DI container. Mirrors the pattern of LoggingConfiguration and
 *          SecurityHeadersConfiguration — one static class per infrastructure concern,
 *          called from Program.cs, keeps Program.cs clean.
 *
 * PACKAGES WIRED HERE:
 *   TMDbLib 3.0.0                              — TMDbClient (constructed in TmdbSyncService)
 *   Microsoft.Extensions.Http.Resilience       — AddStandardResilienceHandler()
 *
 * HTTP CLIENT REGISTRATION:
 *   TMDbLib constructs its own HttpClient internally using the default HttpClientFactory
 *   when no custom handler is provided. To hook the resilience pipeline, we register a
 *   named HttpClient "TmdbClient" with AddStandardResilienceHandler() and pass its
 *   handler into TMDbClient via the httpMessageHandler constructor parameter.
 *
 *   However, TMDbLib's httpMessageHandler constructor is marked internal — it is
 *   intended for testing only. The recommended production approach is to let
 *   TMDbLib manage its own HttpClient (which it does by default) and register the
 *   resilience handler at the IHttpClientFactory level as a named client. If TMDbLib
 *   ever exposes a public IHttpClientFactory integration, switch to that.
 *
 *   CURRENT APPROACH (pragmatic for TMDbLib 3.0):
 *     Register the named client + resilience handler in DI for documentation and
 *     future use. TmdbSyncService constructs TMDbClient with just the API key —
 *     TMDbLib's own internal retry/timeout is active (3s timeout, no automatic
 *     retry). For production resilience, upgrade to a TMDbLib version that
 *     exposes IHttpClientFactory integration, or wrap TMDbClient calls with
 *     Polly directly in TmdbSyncService.
 *
 *   TODO (Phase 3): Evaluate TMDbLib 4.x or use Refit + AddStandardResilienceHandler
 *   on a typed client pointing at api.themoviedb.org directly.
 *
 * USER SECRETS KEY:
 *   Development:  dotnet user-secrets set "Tmdb:ApiKey" "your_v3_api_key"
 *   Production:   Environment variable Tmdb__ApiKey (double-underscore)
 *   TMDb v3 API keys are obtained at: https://www.themoviedb.org/settings/api
 *
 * CSP UPDATE REQUIRED:
 *   If poster images are rendered directly from TMDb (not via ImageSharp.Web proxy):
 *   Add "image.tmdb.org" to the img-src directive in SecurityHeadersConfiguration.cs.
 *   If using ImageSharp.Web as a proxy (recommended — images served from 'self'),
 *   no CSP change is needed.
 *
 * Phase: 2 (new)
 * Author: ScrumFlix Rebuild Team
 */

using Microsoft.Extensions.Http.Resilience;

namespace ScrumFlix.Infrastructure;

/// <summary>
/// Registers TMDb API client infrastructure and TmdbSyncService with the DI container.
/// Call <see cref="ConfigureTmdb"/> from Program.cs in the service registration block.
/// </summary>
public static class TmdbConfiguration
{
    /// <summary>
    /// Adds the named TMDb HttpClient (with resilience handler) and registers
    /// TmdbSyncService as a scoped service.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    /// <param name="configuration">The application configuration (reads Tmdb:ApiKey).</param>
    public static IServiceCollection ConfigureTmdb(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        // ── Named HttpClient for TMDb API ──────────────────────────────────────
        //
        // AddStandardResilienceHandler() adds the Microsoft.Extensions.Http.Resilience
        // pipeline which provides:
        //   - Total request timeout: 30s
        //   - Retry: up to 3 attempts with exponential backoff (2s, 4s, 8s)
        //   - Circuit breaker: opens after 5 consecutive failures, resets after 30s
        //   - Attempt timeout: 10s per individual attempt
        //
        // This named client is available for injection via IHttpClientFactory.
        // TmdbSyncService uses TMDbClient directly (which manages its own HttpClient),
        // but registering here documents the intent and enables future typed-client
        // migration without touching TmdbSyncService.
        services.AddHttpClient("TmdbClient", client =>
        {
            client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "ScrumFlix/2.0");
        })
        .AddStandardResilienceHandler(options =>
        {
            // Total timeout across all retry attempts
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);

            // Per-attempt timeout (must be less than TotalRequestTimeout)
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);

            // Retry: 3 additional attempts after the initial failure
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.UseJitter        = true; // avoids thundering herd on retry

            // Circuit breaker: pause after repeated failures to avoid hammering TMDb
            options.CircuitBreaker.SamplingDuration           = TimeSpan.FromSeconds(60);
            options.CircuitBreaker.FailureRatio               = 0.5;   // 50% failure rate
            options.CircuitBreaker.MinimumThroughput          = 5;     // min requests before breaking
            options.CircuitBreaker.BreakDuration              = TimeSpan.FromSeconds(30);
        });

        // ── TmdbSyncService ────────────────────────────────────────────────────
        //
        // Scoped: a fresh TMDbClient (and AppDbContext) per request or per
        // background tick. TMDbLib is not thread-safe across concurrent callers
        // so Scoped is the correct lifetime here.
        services.AddScoped<ITmdbSyncService, TmdbSyncService>();

        // Log API key presence at startup (value is never logged — only presence)
        var hasKey = !string.IsNullOrWhiteSpace(configuration["Tmdb:ApiKey"]);
        if (!hasKey)
        {
            // Warning is also emitted by TmdbSyncService itself on first use,
            // but surface it here too so it appears at startup rather than on
            // first sync request.
            Console.WriteLine(
                "[TmdbConfiguration] WARNING: Tmdb:ApiKey is not set. " +
                "Movie poster and genre syncing will be disabled. " +
                "Set the key via: dotnet user-secrets set \"Tmdb:ApiKey\" \"<your_key>\"");
        }

        return services;
    }
}
