/*
 * File:        /ScrumFlix/Services/IEmailService.cs
 * Namespace:   ScrumFlix.Services
 * Purpose:     Defines the transactional email contract for ScrumFlix.
 *
 *              Covers three primary email flows:
 *                1. Order confirmation — sent to the customer after checkout.
 *                2. Staff notification — sent to admin/manager on significant events.
 *                3. Low-stock alert — sent to admin when concession inventory falls
 *                   to or below its configured minimum.
 *
 *              The interface is intentionally narrow. HTML body construction and
 *              MIME assembly live in the concrete EmailService implementation
 *              (Services/EmailService.cs) — callers pass structured data only.
 *
 * Registration: builder.Services.AddScoped<IEmailService, EmailService>();
 *              (Program.cs — after LoggingConfiguration.ConfigureLogging())
 *
 * Phase:   S4 — Email Service
 * Author:  ScrumFlix Rebuild Team
 * Created: 2026-05-10
 */

namespace ScrumFlix.Services;

/// <summary>
/// Transactional email service for ScrumFlix.
/// Backed by MailKit + MimeKit for reliable SMTP delivery.
/// Registered as Scoped — one instance per request.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a rich-HTML order confirmation to the customer.
    /// </summary>
    /// <param name="toEmail">Recipient email address.</param>
    /// <param name="toName">Recipient display name (used in the salutation).</param>
    /// <param name="ticketCodes">List of issued ticket codes included in the order.</param>
    /// <param name="orderTotal">Formatted order total string (e.g. "$24.00").</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    Task SendOrderConfirmationAsync(
        string       toEmail,
        string       toName,
        List<long>   ticketCodes,
        string       orderTotal,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a full purchase receipt email to the customer-supplied address.
    /// Mirrors the OrderConfirmation page — includes QR codes with seat labels
    /// for tickets, an itemized order breakdown, and concession lines.
    ///
    /// The email address is supplied by the customer at CartReview and is NOT
    /// stored after delivery. It is entirely separate from any Web Sales System
    /// account email.
    /// </summary>
    /// <param name="toEmail">Customer-supplied receipt email address.</param>
    /// <param name="orderSubtotal">Formatted pre-tax subtotal (e.g. "$22.14").</param>
    /// <param name="orderTax">Formatted sales tax amount (e.g. "$1.83").</param>
    /// <param name="orderTotal">Formatted grand total string (e.g. "$23.97").</param>
    /// <param name="timeOfSale">UTC timestamp of the sale.</param>
    /// <param name="ticketCodes">Issued ticket codes (empty for concession-only orders).</param>
    /// <param name="qrCodeBase64s">Base64 PNG QR images, parallel-indexed with ticketCodes.</param>
    /// <param name="seatLabels">Seat labels parallel-indexed with ticketCodes (empty string = GA).</param>
    /// <param name="screenNames">Screen names parallel-indexed with ticketCodes (e.g. "Screen 1").</param>
    /// <param name="orderItems">Full cart snapshot — tickets and concessions.</param>
    /// <param name="concessionQrBase64">Base64 PNG QR for the concession receipt. Null when no concessions.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    Task SendPurchaseReceiptAsync(
        string               toEmail,
        string               orderSubtotal,
        string               orderTax,
        string               orderTotal,
        DateTime             timeOfSale,
        List<long>           ticketCodes,
        List<string>         qrCodeBase64s,
        List<string>         seatLabels,
        List<string>         screenNames,
        List<ReceiptLineItem> orderItems,
        string?              concessionQrBase64 = null,
        CancellationToken    cancellationToken = default);


    /// <param name="subject">Email subject line.</param>
    /// <param name="headingText">Bold heading text shown at the top of the email body.</param>
    /// <param name="bodyHtml">Inner HTML content. Will be embedded in the branded template.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    Task SendStaffNotificationAsync(
        string subject,
        string headingText,
        string bodyHtml,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a rich-HTML low-stock alert to the configured admin recipient.
    /// </summary>
    /// <param name="itemName">Display name of the concession item.</param>
    /// <param name="quantityInStock">Current quantity remaining.</param>
    /// <param name="minimum">Configured minimum threshold.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    Task SendLowStockAlertAsync(
        string itemName,
        int    quantityInStock,
        int    minimum,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a rich-HTML email with an attached PDF (e.g. a report or pay stub).
    /// </summary>
    /// <param name="toEmail">Recipient email address.</param>
    /// <param name="toName">Recipient display name.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="bodyHtml">Inner HTML content embedded in the branded template.</param>
    /// <param name="attachmentBytes">Raw PDF bytes to attach.</param>
    /// <param name="attachmentFileName">File name for the attachment (e.g. "PayStub_May2026.pdf").</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    Task SendWithPdfAttachmentAsync(
        string toEmail,
        string toName,
        string subject,
        string bodyHtml,
        byte[] attachmentBytes,
        string attachmentFileName,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A single line item passed to <see cref="IEmailService.SendPurchaseReceiptAsync"/>.
/// Carries enough data to render ticket details (movie, showtime, seat, location)
/// and concession lines in the customer receipt email.
/// </summary>
public sealed class ReceiptLineItem
{
    public string   DisplayName   { get; init; } = string.Empty;
    public int      Quantity      { get; init; }
    public decimal  UnitPrice     { get; init; }
    public decimal  LineTotal     { get; init; }
    public bool     IsConcession  { get; init; }
    public string?  MovieName     { get; init; }
    public DateTime? ShowTime     { get; init; }
    public string?  LocationName  { get; init; }
    public string?  ScreenName    { get; init; }
    public string?  SeatNumbers   { get; init; }

    public string FormattedShowTime =>
        ShowTime?.ToString("ddd MMM d \u00b7 h:mm tt") ?? string.Empty;
}
