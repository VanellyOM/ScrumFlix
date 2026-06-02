/*
 * File:        /ScrumFlix/Services/ISystemAccountProvider.cs
 * Namespace:   ScrumFlix.Services
 * Purpose:     Contract for the singleton that resolves and caches the
 *              web.sales system account UserId at application startup.
 *
 *              The web.sales account is the synthetic user record that
 *              satisfies the NOT NULL FK on Ticket.UserAtSale for all
 *              anonymous public-facing purchases. No controller or
 *              repository should ever hardcode this ID as a literal integer.
 *
 *              Usage in checkout services:
 *                int webUserId = _systemAccounts.WebSalesUserId;
 *                // supply webUserId to Ticket.UserAtSale and ConcessionSale.UserId
 *
 * Phase:   2
 * Author:  ScrumFlix Rebuild Team
 * Updated: 2026-05-04
 */

namespace ScrumFlix.Services;

/// <summary>
/// Provides access to resolved system-account identifiers.
/// Registered as a singleton; resolved once at application startup.
/// </summary>
public interface ISystemAccountProvider
{
    /// <summary>
    /// The UserId of the <c>web.sales</c> system account.
    /// Used as <c>UserAtSale</c> on <c>Ticket</c> rows and <c>UserId</c>
    /// on <c>ConcessionSale</c> rows created during public web checkout.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the web.sales account has not been seeded and
    /// <see cref="InitializeAsync"/> has not been called successfully.
    /// </exception>
    int WebSalesUserId { get; }

    /// <summary>
    /// Resolves system account identifiers from the database.
    /// Called once from Program.cs immediately after the application starts.
    /// </summary>
    Task InitializeAsync();
}
