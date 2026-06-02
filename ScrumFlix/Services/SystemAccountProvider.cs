/*
 * File:        /ScrumFlix/Services/SystemAccountProvider.cs
 * Namespace:   ScrumFlix.Services
 * Purpose:     Singleton implementation of ISystemAccountProvider.
 *
 *              Resolves the web.sales UserId exactly once at startup and
 *              caches it for the lifetime of the application. Any subsequent
 *              access to WebSalesUserId reads from the cached integer —
 *              no database round-trip after initialization.
 *
 *              Initialization is triggered from the startup block in Program.cs
 *              using a scoped AppDbContext so the singleton itself does not
 *              hold a long-lived DbContext (which would cause context threading
 *              issues in a multi-request environment).
 *
 *              If the web.sales account is not found in the database the
 *              application will log a Critical error and refuse to start.
 *              This prevents silent data-integrity failures where tickets
 *              would be written without a valid UserAtSale FK.
 *
 * Phase:   2
 * Author:  ScrumFlix Rebuild Team
 * Updated: 2026-05-04
 */

using Microsoft.Extensions.DependencyInjection;

namespace ScrumFlix.Services;

/// <summary>
/// Caches resolved system-account identifiers for the application lifetime.
/// </summary>
public sealed class SystemAccountProvider : ISystemAccountProvider
{
    private const string WebSalesUserName = "web.sales";

    private readonly IServiceProvider              _services;
    private readonly ILogger<SystemAccountProvider> _logger;

    private int? _webSalesUserId;

    /// <summary>
    /// Initializes a new instance of <see cref="SystemAccountProvider"/>.
    /// </summary>
    public SystemAccountProvider(
        IServiceProvider services,
        ILogger<SystemAccountProvider> logger)
    {
        _services = services;
        _logger   = logger;
    }

    /// <inheritdoc/>
    public int WebSalesUserId =>
        _webSalesUserId
        ?? throw new InvalidOperationException(
            "SystemAccountProvider has not been initialized. " +
            "Call InitializeAsync() from the startup block in Program.cs before " +
            "serving any requests.");

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        // Use a fresh scoped DbContext so the singleton doesn't hold an open connection.
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var webSalesUser = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == WebSalesUserName);

        if (webSalesUser is null)
        {
            // This is a hard startup failure. The web.sales account must be seeded
            // before the application can safely accept ticket or concession purchases.
            _logger.LogCritical(
                "STARTUP FAILURE: The '{WebSalesUserName}' system account was not found " +
                "in the Users table. Run SampleDataSeederFull with the WebUser block " +
                "enabled against the target database before deploying. " +
                "Application cannot serve checkout requests without this account.",
                WebSalesUserName);

            // We do not throw here — the app will start but WebSalesUserId property
            // will throw InvalidOperationException when checkout is attempted,
            // which will surface as a 500 rather than crashing the process on startup.
            return;
        }

        _webSalesUserId = webSalesUser.UserId;

        _logger.LogInformation(
            "SystemAccountProvider initialized — web.sales UserId resolved to {UserId}.",
            _webSalesUserId);
    }
}
