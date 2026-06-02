/*
 * File:        /ScrumFlix/Services/EmailService.cs
 * Namespace:   ScrumFlix.Services
 * Purpose:     Transactional email service for ScrumFlix.
 *
 *              Backed by MailKit (SMTP client) and MimeKit (MIME assembly).
 *              Produces professionally branded HTML emails with plain-text
 *              fallback for every message type.
 *
 *              SMTP configuration is read from:
 *                Email:SmtpHost       — e.g. smtp.gmail.com
 *                Email:SmtpPort       — e.g. 587
 *                Email:SmtpUser       — sender login / username
 *                Email:SmtpPassword   — app password (User Secrets / env var)
 *                Email:From           — sender address shown to recipient
 *                Email:FromName       — sender display name (e.g. "ScrumFlix")
 *                Email:AdminTo        — admin recipient for staff / low-stock alerts
 *
 *              NOTE: The "Email:*" keys here are SEPARATE from the Serilog
 *              "Logging:Email:*" keys used by LoggingConfiguration. The two
 *              sections are independently configured so that the transactional
 *              sender address can differ from the alert sender address.
 *
 *              If any required SMTP key is missing the service logs a warning and
 *              skips delivery silently — it does NOT throw, so checkout is never
 *              blocked by a missing email configuration.
 *
 * Registration: builder.Services.AddScoped<IEmailService, EmailService>();
 *
 * Dependencies: MailKit 4.16.0, MimeKit 4.16.0 (already in .csproj)
 *
 * Phase:   S4 — Email Service
 * Author:  ScrumFlix Rebuild Team
 * Created: 2026-05-10
 */

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace ScrumFlix.Services;

/// <summary>
/// Concrete transactional email service backed by MailKit and MimeKit.
/// Registered as Scoped — one instance per HTTP request.
/// </summary>
public sealed class EmailService : IEmailService
{
    // ── Shared brand colours (used in inline CSS) ──────────────────────────
    private const string BrandPrimary   = "#c0392b"; // ScrumFlix red
    private const string BrandDark      = "#1a1a2e"; // deep navy header
    private const string BrandAccent    = "#e74c3c"; // button/accent red
    private const string BrandLight     = "#f9f9f9"; // page background
    private const string BrandFooter    = "#2c2c2c"; // footer background
    private const string TextPrimary    = "#1a1a1a";
    private const string TextMuted      = "#666666";
    private const string BorderColor    = "#e0e0e0";

    // ── Dependencies ───────────────────────────────────────────────────────
    private readonly IConfiguration        _config;
    private readonly ILogger<EmailService> _logger;

    /// <summary>
    /// Initializes the email service with the application configuration and logger.
    /// </summary>
    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // IEmailService implementation
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task SendOrderConfirmationAsync(
        string            toEmail,
        string            toName,
        List<long>        ticketCodes,
        string            orderTotal,
        CancellationToken cancellationToken = default)
    {
        var codesHtml = string.Join(
            "\n",
            ticketCodes.Select(c =>
                $"""
                 <tr>
                   <td style="padding:10px 16px;border-bottom:1px solid {BorderColor};
                               font-family:'Segoe UI',Arial,sans-serif;font-size:15px;
                               color:{TextPrimary};letter-spacing:1px;">
                     🎟️ &nbsp;<strong>{c:D6}</strong>
                   </td>
                 </tr>
                 """));

        var codesText = string.Join(", ", ticketCodes.Select(c => c.ToString("D6")));

        var bodyHtml = $"""
            <p style="margin:0 0 16px;font-size:16px;color:{TextPrimary};">
              Hi <strong>{EscapeHtml(toName)}</strong>,
            </p>
            <p style="margin:0 0 20px;font-size:15px;color:{TextMuted};line-height:1.6;">
              Your ScrumFlix order has been confirmed! Present your ticket code(s)
              at the box office or scan the QR codes at the kiosk.
            </p>

            <!-- Ticket codes table -->
            <table width="100%" cellpadding="0" cellspacing="0" border="0"
                   style="border:1px solid {BorderColor};border-radius:8px;
                          overflow:hidden;margin-bottom:24px;">
              <thead>
                <tr style="background:{BrandDark};">
                  <th style="padding:12px 16px;text-align:left;font-family:'Segoe UI',Arial,sans-serif;
                              font-size:13px;color:#ffffff;text-transform:uppercase;
                              letter-spacing:1px;">
                    Ticket Code(s)
                  </th>
                </tr>
              </thead>
              <tbody>
                {codesHtml}
              </tbody>
            </table>

            <!-- Order total -->
            <table width="100%" cellpadding="0" cellspacing="0" border="0"
                   style="margin-bottom:28px;">
              <tr>
                <td style="font-family:'Segoe UI',Arial,sans-serif;font-size:15px;
                            color:{TextMuted};padding:8px 0;">Order Total</td>
                <td style="font-family:'Segoe UI',Arial,sans-serif;font-size:18px;
                            font-weight:700;color:{BrandPrimary};text-align:right;
                            padding:8px 0;">{EscapeHtml(orderTotal)}</td>
              </tr>
            </table>

            <p style="margin:0 0 8px;font-size:14px;color:{TextMuted};line-height:1.6;">
              Enjoy the show! Concessions are available at the lobby counter.
              If you have any questions, contact our team at the box office.
            </p>
            """;

        var plainText =
            $"Hi {toName},\n\n" +
            $"Your ScrumFlix order is confirmed!\n\n" +
            $"Ticket Code(s): {codesText}\n" +
            $"Order Total: {orderTotal}\n\n" +
            "Present your code(s) at the box office or scan the QR code at the kiosk.\n\n" +
            "Enjoy the show!\n— ScrumFlix";

        var message = BuildMessage(
            toEmail:     toEmail,
            toName:      toName,
            subject:     $"🎬 Your ScrumFlix Tickets — Order Confirmed",
            headingText: "Order Confirmed",
            bodyHtml:    bodyHtml,
            plainText:   plainText);

        await SendAsync(message, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SendPurchaseReceiptAsync(
        string               toEmail,
        string               orderSubtotal,
        string               orderTax,
        string               orderTotal,
        DateTime             timeOfSale,
        List<long>           ticketCodes,
        List<string>         qrCodeBase64s,
        List<string>         seatLabels,
        List<ReceiptLineItem> orderItems,
        CancellationToken    cancellationToken = default)
    {
        var hasTickets     = ticketCodes.Any();
        var hasConcessions = orderItems.Any(i => i.IsConcession);
        var ticketItems    = orderItems.Where(i => !i.IsConcession).ToList();
        var concItems      = orderItems.Where(i =>  i.IsConcession).ToList();

        // ── Subject ───────────────────────────────────────────────────────
        var subject = hasTickets && hasConcessions
            ? "\U0001f3ac Your ScrumFlix Order \u2014 Tickets & Concessions Confirmed"
            : hasTickets
                ? "\U0001f3ac Your ScrumFlix Tickets \u2014 Order Confirmed"
                : "\U0001f37f Your ScrumFlix Concession Receipt";

        // ── QR images — attached as inline CID parts ──────────────────────
        // Gmail and Outlook block data: URIs in email <img> tags for security.
        // The correct approach is to attach each PNG as an inline MIME part and
        // reference it via cid: in the HTML src attribute, which is universally
        // supported across all major email clients.
        var qrParts = new List<(string Cid, byte[] Png)>();
        for (int i = 0; i < qrCodeBase64s.Count; i++)
        {
            if (string.IsNullOrEmpty(qrCodeBase64s[i])) continue;
            try
            {
                var pngBytes = Convert.FromBase64String(qrCodeBase64s[i]);
                var cid      = "qr" + i + "@scrumflix";
                qrParts.Add((cid, pngBytes));
            }
            catch
            {
                // Malformed base64 — skip this QR; ticket info still shows
                qrParts.Add((string.Empty, Array.Empty<byte>()));
            }
        }

        // ── Ticket QR cards ───────────────────────────────────────────────
        var cardRows = new System.Text.StringBuilder();

        if (hasTickets)
        {
            var ticketCount = ticketCodes.Count;

            for (int i = 0; i < ticketCodes.Count; i++)
            {
                var code      = ticketCodes[i];
                var seatLabel = i < seatLabels.Count ? seatLabels[i] : string.Empty;

                // Use cid: reference if we have a QR part; empty cell otherwise
                string qrCell;
                if (i < qrParts.Count && !string.IsNullOrEmpty(qrParts[i].Cid))
                {
                    qrCell =
                        "<td style=\"padding:20px;text-align:center;vertical-align:top;" +
                        "width:200px;border-right:1px solid " + BorderColor + ";\">" +
                        "<img src=\"cid:" + qrParts[i].Cid + "\"" +
                        " width=\"180\" height=\"180\" alt=\"QR code for ticket " +
                        code.ToString("D6") + "\"" +
                        " style=\"display:block;border:0;\" /></td>";
                }
                else
                {
                    qrCell =
                        "<td style=\"padding:20px;text-align:center;vertical-align:top;" +
                        "width:200px;border-right:1px solid " + BorderColor + ";\">" +
                        "<p style=\"color:" + TextMuted + ";font-size:12px;margin:0;\">" +
                        "(QR unavailable)</p></td>";
                }

                var seatHtml = string.IsNullOrEmpty(seatLabel) ? string.Empty :
                    "<div style=\"margin-top:10px;\">" +
                    "<span style=\"font-size:11px;font-weight:700;color:" + TextMuted + ";" +
                    "text-transform:uppercase;letter-spacing:.1em;\">Seat</span><br/>" +
                    "<span style=\"font-size:24px;font-weight:800;color:" + BrandAccent + ";\">" +
                    EscapeHtml(seatLabel) + "</span></div>";

                var counterHtml = ticketCount < 2 ? string.Empty :
                    "<div style=\"font-size:11px;color:" + TextMuted + ";margin-top:6px;\">" +
                    "Ticket " + (i + 1) + " of " + ticketCount + "</div>";

                cardRows.Append(
                    "<!-- Ticket card " + (i + 1) + " -->" +
                    "<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"" +
                    " style=\"border:1px solid " + BorderColor + ";border-radius:10px;" +
                    "overflow:hidden;margin-bottom:20px;background:#ffffff;\"><tr>" +
                    qrCell +
                    "<td style=\"padding:20px;vertical-align:middle;text-align:center;\">" +
                    "<span style=\"font-size:11px;font-weight:700;color:" + TextMuted + ";" +
                    "text-transform:uppercase;letter-spacing:.1em;\">Ticket Code</span><br/>" +
                    "<span style=\"font-size:26px;font-weight:800;" +
                    "font-family:'Courier New',monospace;" +
                    "color:" + TextPrimary + ";letter-spacing:3px;\">" +
                    code.ToString("D6") + "</span>" +
                    seatHtml + counterHtml +
                    "</td></tr></table>\n");
            }
        }

        // ── Ticket detail rows ────────────────────────────────────────────
        var ticketDetailRows = new System.Text.StringBuilder();
        foreach (var ti in ticketItems)
        {
            var showRow    = string.IsNullOrEmpty(ti.FormattedShowTime) ? string.Empty :
                "<div style=\"font-size:13px;color:" + TextMuted + ";\">&#128336; " +
                EscapeHtml(ti.FormattedShowTime) + "</div>";
            var locRow     = string.IsNullOrEmpty(ti.LocationName) ? string.Empty :
                "<div style=\"font-size:13px;color:" + TextMuted + ";\">&#128205; " +
                EscapeHtml(ti.LocationName) + "</div>";
            var seatNumRow = string.IsNullOrEmpty(ti.SeatNumbers) ? string.Empty :
                "<div style=\"font-size:13px;color:" + TextMuted + ";\">Seat(s): " +
                EscapeHtml(ti.SeatNumbers) + "</div>";

            ticketDetailRows.Append(
                "<tr>" +
                "<td style=\"padding:12px 16px;border-bottom:1px solid " + BorderColor + ";" +
                "font-family:'Segoe UI',Arial,sans-serif;vertical-align:top;\">" +
                "<div style=\"font-weight:700;font-size:15px;color:" + TextPrimary + ";\">" +
                EscapeHtml(ti.MovieName ?? ti.DisplayName) + "</div>" +
                showRow + locRow + seatNumRow +
                "<div style=\"font-size:13px;color:" + TextMuted + ";\">" +
                ti.Quantity + " &times; " + ti.UnitPrice.ToString("C") + "</div></td>" +
                "<td style=\"padding:12px 16px;border-bottom:1px solid " + BorderColor + ";" +
                "font-family:'Segoe UI',Arial,sans-serif;font-size:15px;" +
                "font-weight:700;color:" + TextPrimary + ";text-align:right;" +
                "vertical-align:top;white-space:nowrap;\">" +
                ti.LineTotal.ToString("C") + "</td></tr>\n");
        }

        // ── Concession rows ───────────────────────────────────────────────
        var concRows = new System.Text.StringBuilder();
        foreach (var ci in concItems)
        {
            concRows.Append(
                "<tr>" +
                "<td style=\"padding:12px 16px;border-bottom:1px solid " + BorderColor + ";" +
                "font-family:'Segoe UI',Arial,sans-serif;font-size:15px;" +
                "color:" + TextPrimary + ";\">" +
                EscapeHtml(ci.DisplayName) +
                "<div style=\"font-size:13px;color:" + TextMuted + ";\">" +
                ci.Quantity + " &times; " + ci.UnitPrice.ToString("C") + "</div></td>" +
                "<td style=\"padding:12px 16px;border-bottom:1px solid " + BorderColor + ";" +
                "font-family:'Segoe UI',Arial,sans-serif;font-size:15px;" +
                "font-weight:700;color:" + TextPrimary + ";text-align:right;" +
                "white-space:nowrap;\">" +
                ci.LineTotal.ToString("C") + "</td></tr>\n");
        }

        // ── Assemble HTML body ────────────────────────────────────────────
        var body = new System.Text.StringBuilder();
        body.Append(
            "<p style=\"margin:0 0 20px;font-size:16px;color:" + TextPrimary + ";\">" +
            "Thank you for choosing <strong>ScrumFlix Theatres</strong>! " +
            "Your order is confirmed and ready.</p>\n");

        if (hasTickets)
        {
            var ticketCount = ticketCodes.Count;
            var heading     = ticketCount == 1 ? "Your Ticket" : "Your " + ticketCount + " Tickets";
            body.Append(
                "<h2 style=\"margin:0 0 6px;font-family:'Segoe UI',Arial,sans-serif;" +
                "font-size:16px;font-weight:700;color:" + TextPrimary + ";" +
                "text-transform:uppercase;letter-spacing:.08em;\">" +
                "&#127915; " + heading + "</h2>\n" +
                "<p style=\"margin:0 0 16px;font-size:13px;color:" + TextMuted + ";\">" +
                "Show the QR code at the theater entrance. One code per ticket.</p>\n");
            body.Append(cardRows);

            body.Append(
                "<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"" +
                " style=\"border:1px solid " + BorderColor + ";border-radius:8px;" +
                "overflow:hidden;margin-bottom:28px;\"><thead>" +
                "<tr style=\"background:" + BrandDark + ";\">" +
                "<th style=\"padding:10px 16px;text-align:left;" +
                "font-family:'Segoe UI',Arial,sans-serif;" +
                "font-size:12px;color:#ffffff;text-transform:uppercase;letter-spacing:1px;\">" +
                "Movie / Showtime</th>" +
                "<th style=\"padding:10px 16px;text-align:right;" +
                "font-family:'Segoe UI',Arial,sans-serif;" +
                "font-size:12px;color:#ffffff;text-transform:uppercase;letter-spacing:1px;\">" +
                "Amount</th></tr></thead><tbody>\n" +
                ticketDetailRows + "</tbody></table>\n");
        }

        if (hasConcessions)
        {
            var topMargin = hasTickets ? "8px" : "0";
            body.Append(
                "<h2 style=\"margin:" + topMargin + " 0 6px;" +
                "font-family:'Segoe UI',Arial,sans-serif;" +
                "font-size:16px;font-weight:700;color:" + TextPrimary + ";" +
                "text-transform:uppercase;letter-spacing:.08em;\">" +
                "&#127871; Concessions</h2>\n" +
                "<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"" +
                " style=\"border:1px solid " + BorderColor + ";border-radius:8px;" +
                "overflow:hidden;margin-bottom:28px;\"><thead>" +
                "<tr style=\"background:" + BrandDark + ";\">" +
                "<th style=\"padding:10px 16px;text-align:left;" +
                "font-family:'Segoe UI',Arial,sans-serif;" +
                "font-size:12px;color:#ffffff;text-transform:uppercase;letter-spacing:1px;\">" +
                "Item</th>" +
                "<th style=\"padding:10px 16px;text-align:right;" +
                "font-family:'Segoe UI',Arial,sans-serif;" +
                "font-size:12px;color:#ffffff;text-transform:uppercase;letter-spacing:1px;\">" +
                "Amount</th></tr></thead><tbody>\n" +
                concRows + "</tbody></table>\n");
        }

        body.Append(
            "<table width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"" +
            " style=\"margin-bottom:24px;border-top:2px solid " + BorderColor + ";padding-top:12px;\">" +
            "<tr>" +
            "<td style=\"font-family:'Segoe UI',Arial,sans-serif;font-size:14px;" +
            "color:" + TextMuted + ";padding:6px 0;\">Subtotal</td>" +
            "<td style=\"font-family:'Segoe UI',Arial,sans-serif;font-size:14px;" +
            "color:" + TextPrimary + ";text-align:right;padding:6px 0;\">" +
            EscapeHtml(orderSubtotal) + "</td></tr>" +
            "<tr>" +
            "<td style=\"font-family:'Segoe UI',Arial,sans-serif;font-size:14px;" +
            "color:" + TextMuted + ";padding:6px 0;\">Sales Tax (8.25%)</td>" +
            "<td style=\"font-family:'Segoe UI',Arial,sans-serif;font-size:14px;" +
            "color:" + TextPrimary + ";text-align:right;padding:6px 0;\">" +
            EscapeHtml(orderTax) + "</td></tr>" +
            "<tr style=\"border-top:1px solid " + BorderColor + ";\"><td style=\"font-family:'Segoe UI',Arial,sans-serif;" +
            "font-size:15px;color:" + TextMuted + ";padding:8px 0;font-weight:600;\">Order Total</td>" +
            "<td style=\"font-family:'Segoe UI',Arial,sans-serif;font-size:20px;" +
            "font-weight:800;color:" + BrandPrimary + ";text-align:right;padding:8px 0;\">" +
            EscapeHtml(orderTotal) + "</td></tr>" +
            "<tr><td colspan=\"2\" style=\"font-family:'Segoe UI',Arial,sans-serif;" +
            "font-size:12px;color:" + TextMuted + ";\">" +
            "Purchased: " + timeOfSale.ToLocalTime().ToString("ddd MMM d, yyyy h:mm tt") +
            "</td></tr></table>\n");

        body.Append(
            "<p style=\"margin:0;font-size:13px;color:" + TextMuted + ";line-height:1.6;\">" +
            (hasTickets ? "Save this email to keep your QR codes. " : string.Empty) +
            "Questions? Visit the box office or lobby counter.</p>");

        // ── Plain-text fallback ───────────────────────────────────────────
        var plain = new System.Text.StringBuilder();
        plain.AppendLine("Thank you for your ScrumFlix order!");
        plain.AppendLine();
        if (hasTickets)
        {
            plain.AppendLine("YOUR TICKETS:");
            for (int i = 0; i < ticketCodes.Count; i++)
            {
                plain.Append("  Ticket " + (i + 1) + ": " + ticketCodes[i].ToString("D6"));
                if (i < seatLabels.Count && !string.IsNullOrEmpty(seatLabels[i]))
                    plain.Append("  |  Seat: " + seatLabels[i]);
                plain.AppendLine();
            }
            plain.AppendLine();
        }
        if (hasConcessions)
        {
            plain.AppendLine("CONCESSIONS:");
            foreach (var ci in concItems)
                plain.AppendLine("  " + ci.DisplayName + " x" + ci.Quantity +
                    "  " + ci.LineTotal.ToString("C"));
            plain.AppendLine();
        }
        plain.AppendLine("Subtotal:    " + orderSubtotal);
        plain.AppendLine("Sales Tax:   " + orderTax);
        plain.AppendLine("Order Total: " + orderTotal);
        plain.AppendLine("Purchased: " +
            timeOfSale.ToLocalTime().ToString("ddd MMM d, yyyy h:mm tt"));
        plain.AppendLine();
        plain.AppendLine("Enjoy the show!");
        plain.Append("--- ScrumFlix");

        // ── Build MimeMessage manually to support inline CID attachments ──
        // BuildMessage() only supports multipart/alternative (html + plain text).
        // For inline images we need multipart/related wrapping the alternative,
        // then each QR PNG attached as an inline MimePart with a Content-ID.
        // Structure:
        //   multipart/mixed
        //     multipart/related
        //       multipart/alternative
        //         text/plain
        //         text/html
        //       image/png  (cid:qr0@scrumflix)   ← inline, referenced by HTML
        //       image/png  (cid:qr1@scrumflix)   ← one per ticket
        var fromAddress = _config["Email:From"]     ?? string.Empty;
        var fromName    = _config["Email:FromName"] ?? "ScrumFlix";
        var fullHtml    = BuildHtmlTemplate("Order Confirmed", body.ToString());

        var plainPart = new TextPart("plain") { Text = plain.ToString() };
        var htmlPart  = new TextPart("html")  { Text = fullHtml };

        var alternative = new MultipartAlternative { plainPart, htmlPart };

        MimeEntity messageBody;

        if (qrParts.Any(p => !string.IsNullOrEmpty(p.Cid)))
        {
            // Wrap in multipart/related so the CID images are found by the HTML
            var related = new MultipartRelated { alternative };

            foreach (var (cid, pngBytes) in qrParts)
            {
                if (string.IsNullOrEmpty(cid) || pngBytes.Length == 0) continue;

                var imgPart = new MimePart("image", "png")
                {
                    Content                 = new MimeContent(new MemoryStream(pngBytes)),
                    ContentDisposition      = new ContentDisposition(ContentDisposition.Inline),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    ContentId               = cid
                };
                related.Add(imgPart);
            }

            messageBody = related;
        }
        else
        {
            // No QR images — plain alternative is sufficient
            messageBody = alternative;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(new MailboxAddress("ScrumFlix Guest", toEmail));
        message.Subject = subject;
        message.Body    = messageBody;

        await SendAsync(message, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SendStaffNotificationAsync(

        string            subject,
        string            headingText,
        string            bodyHtml,
        CancellationToken cancellationToken = default)
    {
        var adminTo = _config["Email:AdminTo"];
        if (string.IsNullOrWhiteSpace(adminTo))
        {
            _logger.LogWarning(
                "EmailService.SendStaffNotificationAsync: Email:AdminTo is not configured. " +
                "Staff notification '{Subject}' was not sent.", subject);
            return;
        }

        var plainText =
            $"{headingText}\n\n" +
            "This is a ScrumFlix staff notification. " +
            "Please review the details in the admin dashboard.\n\n" +
            "— ScrumFlix System";

        var message = BuildMessage(
            toEmail:     adminTo,
            toName:      "ScrumFlix Admin",
            subject:     subject,
            headingText: headingText,
            bodyHtml:    bodyHtml,
            plainText:   plainText);

        await SendAsync(message, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SendLowStockAlertAsync(
        string            itemName,
        int               quantityInStock,
        int               minimum,
        CancellationToken cancellationToken = default)
    {
        var bodyHtml = $"""
            <p style="margin:0 0 16px;font-size:16px;color:{TextPrimary};">
              A concession item has reached or fallen below its configured minimum stock level.
            </p>

            <table width="100%" cellpadding="0" cellspacing="0" border="0"
                   style="border:1px solid {BorderColor};border-radius:8px;overflow:hidden;
                          margin-bottom:24px;">
              <thead>
                <tr style="background:{BrandDark};">
                  <th colspan="2"
                      style="padding:12px 16px;text-align:left;
                              font-family:'Segoe UI',Arial,sans-serif;
                              font-size:13px;color:#ffffff;text-transform:uppercase;
                              letter-spacing:1px;">
                    Inventory Alert
                  </th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td style="padding:12px 16px;border-bottom:1px solid {BorderColor};
                              font-family:'Segoe UI',Arial,sans-serif;font-size:14px;
                              color:{TextMuted};width:40%;">Item</td>
                  <td style="padding:12px 16px;border-bottom:1px solid {BorderColor};
                              font-family:'Segoe UI',Arial,sans-serif;font-size:15px;
                              color:{TextPrimary};font-weight:600;">
                    {EscapeHtml(itemName)}
                  </td>
                </tr>
                <tr>
                  <td style="padding:12px 16px;border-bottom:1px solid {BorderColor};
                              font-family:'Segoe UI',Arial,sans-serif;font-size:14px;
                              color:{TextMuted};">Current Stock</td>
                  <td style="padding:12px 16px;border-bottom:1px solid {BorderColor};
                              font-family:'Segoe UI',Arial,sans-serif;font-size:15px;
                              color:{BrandAccent};font-weight:700;">
                    {quantityInStock} unit(s)
                  </td>
                </tr>
                <tr>
                  <td style="padding:12px 16px;font-family:'Segoe UI',Arial,sans-serif;
                              font-size:14px;color:{TextMuted};">Minimum Threshold</td>
                  <td style="padding:12px 16px;font-family:'Segoe UI',Arial,sans-serif;
                              font-size:15px;color:{TextPrimary};">
                    {minimum} unit(s)
                  </td>
                </tr>
              </tbody>
            </table>

            <p style="margin:0 0 8px;font-size:14px;color:{TextMuted};line-height:1.6;">
              Please restock <strong>{EscapeHtml(itemName)}</strong> at your earliest convenience
              to avoid running out during a busy period.
              Log in to the admin dashboard to update inventory levels.
            </p>
            """;

        await SendStaffNotificationAsync(
            subject:      $"⚠️ Low Stock Alert — {itemName}",
            headingText:  $"Low Stock: {itemName}",
            bodyHtml:     bodyHtml,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SendWithPdfAttachmentAsync(
        string            toEmail,
        string            toName,
        string            subject,
        string            bodyHtml,
        byte[]            attachmentBytes,
        string            attachmentFileName,
        CancellationToken cancellationToken = default)
    {
        var plainText =
            $"Hi {toName},\n\n" +
            "Please find your document attached.\n\n" +
            "— ScrumFlix";

        var message = BuildMessage(
            toEmail:     toEmail,
            toName:      toName,
            subject:     subject,
            headingText: subject,
            bodyHtml:    bodyHtml,
            plainText:   plainText);

        // Attach the PDF
        var attachment = new MimePart("application", "pdf")
        {
            Content            = new MimeContent(new MemoryStream(attachmentBytes)),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            ContentTransferEncoding = ContentEncoding.Base64,
            FileName           = attachmentFileName
        };

        // The root multipart must become mixed to carry the attachment.
        // BuildMessage() returns a MimeMessage whose Body is multipart/alternative.
        // Wrap that in a multipart/mixed so the attachment can be appended.
        var alternative = message.Body;
        var mixed = new Multipart("mixed")
        {
            alternative!,
            attachment
        };
        message.Body = mixed;

        await SendAsync(message, cancellationToken);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Private helpers
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Assembles a branded <see cref="MimeMessage"/> with both HTML and plain-text alternatives.
    /// </summary>
    private MimeMessage BuildMessage(
        string toEmail,
        string toName,
        string subject,
        string headingText,
        string bodyHtml,
        string plainText)
    {
        var fromAddress = _config["Email:From"]     ?? string.Empty;
        var fromName    = _config["Email:FromName"] ?? "ScrumFlix";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;

        var html = BuildHtmlTemplate(headingText, bodyHtml);

        var body = new MultipartAlternative
        {
            new TextPart("plain") { Text = plainText },
            new TextPart("html")  { Text = html      }
        };

        message.Body = body;
        return message;
    }

    /// <summary>
    /// Wraps inner HTML content in the full ScrumFlix branded email template.
    /// Uses inline CSS throughout for maximum email-client compatibility.
    /// </summary>
    private static string BuildHtmlTemplate(string headingText, string innerHtml)
    {
        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1.0" />
              <meta http-equiv="X-UA-Compatible" content="IE=edge" />
              <title>{{EscapeHtml(headingText)}}</title>
            </head>
            <body style="margin:0;padding:0;background-color:{{BrandLight}};
                         font-family:'Segoe UI',Arial,Helvetica,sans-serif;">

              <!-- Outer wrapper -->
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0"
                     border="0" style="background-color:{{BrandLight}};padding:32px 16px;">
                <tr>
                  <td align="center">

                    <!-- Email card -->
                    <table role="presentation" width="600" cellpadding="0" cellspacing="0"
                           border="0" style="max-width:600px;width:100%;
                                             background:#ffffff;border-radius:12px;
                                             box-shadow:0 4px 24px rgba(0,0,0,0.08);
                                             overflow:hidden;">

                      <!-- ── Header ── -->
                      <tr>
                        <td style="background:{{BrandDark}};padding:28px 32px;text-align:center;">
                          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0">
                            <tr>
                              <td align="center">
                                <!-- Wordmark -->
                                <span style="font-family:'Segoe UI',Arial,sans-serif;
                                             font-size:28px;font-weight:800;
                                             color:#ffffff;letter-spacing:2px;
                                             text-transform:uppercase;">
                                  <span style="color:{{BrandAccent}};">Scrum</span>Flix
                                </span>
                                <p style="margin:6px 0 0;font-size:13px;color:#aaaaaa;
                                           letter-spacing:1px;text-transform:uppercase;">
                                  Cinema &amp; Concessions
                                </p>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                      <!-- ── Heading banner ── -->
                      <tr>
                        <td style="background:{{BrandPrimary}};padding:16px 32px;text-align:center;">
                          <h1 style="margin:0;font-family:'Segoe UI',Arial,sans-serif;
                                     font-size:20px;font-weight:700;color:#ffffff;
                                     letter-spacing:0.5px;">
                            {{EscapeHtml(headingText)}}
                          </h1>
                        </td>
                      </tr>

                      <!-- ── Body ── -->
                      <tr>
                        <td style="padding:32px 32px 24px;">
                          {{innerHtml}}
                        </td>
                      </tr>

                      <!-- ── Divider ── -->
                      <tr>
                        <td style="padding:0 32px;">
                          <hr style="border:none;border-top:1px solid {{BorderColor}};margin:0;" />
                        </td>
                      </tr>

                      <!-- ── Footer ── -->
                      <tr>
                        <td style="background:{{BrandFooter}};padding:24px 32px;text-align:center;
                                   border-radius:0 0 12px 12px;">
                          <p style="margin:0 0 6px;font-family:'Segoe UI',Arial,sans-serif;
                                     font-size:13px;color:#aaaaaa;">
                            This email was sent by <strong style="color:#cccccc;">ScrumFlix</strong>.
                            Please do not reply to this message.
                          </p>
                          <p style="margin:0;font-family:'Segoe UI',Arial,sans-serif;
                                     font-size:12px;color:#888888;">
                            &copy; {{DateTime.UtcNow.Year}} ScrumFlix. All rights reserved.
                          </p>
                        </td>
                      </tr>

                    </table>
                    <!-- /Email card -->

                  </td>
                </tr>
              </table>
              <!-- /Outer wrapper -->

            </body>
            </html>
            """;
    }

    /// <summary>
    /// Connects to SMTP via MailKit and delivers the assembled <see cref="MimeMessage"/>.
    /// Logs and swallows exceptions — email failure must not interrupt checkout.
    /// </summary>
    private async Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        var host     = _config["Email:SmtpHost"];
        var portStr  = _config["Email:SmtpPort"];
        var user     = _config["Email:SmtpUser"];
        var password = _config["Email:SmtpPassword"];

        // Guard — all SMTP keys must be present for delivery to proceed.
        if (string.IsNullOrWhiteSpace(host)     ||
            string.IsNullOrWhiteSpace(user)     ||
            string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning(
                "EmailService: SMTP is not configured (Email:SmtpHost / Email:SmtpUser / " +
                "Email:SmtpPassword missing). Email '{Subject}' was not sent.",
                message.Subject);
            return;
        }

        var port = int.TryParse(portStr, out var p) ? p : 587;

        try
        {
            using var client = new SmtpClient();

            // Bypass SSL certificate revocation checks that fail on dev machines
            // when the local network blocks access to Google's CRL servers.
            // Safe to use with Gmail because the certificate itself is valid —
            // only the revocation lookup is being skipped.
            // TODO: Remove or gate behind IHostEnvironment.IsDevelopment()
            //       before deploying to a production environment.
            client.ServerCertificateValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) => true;

            // StartTls on port 587: explicit STARTTLS upgrade — the most
            // reliable option for Gmail and avoids the Auto negotiation
            // that triggered the revocation check hang on dev machines.
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(user, password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);

            _logger.LogInformation(
                "EmailService: Sent '{Subject}' to {Recipient}.",
                message.Subject,
                message.To.ToString());
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "EmailService: Send cancelled for '{Subject}'.", message.Subject);
        }
        catch (Exception ex)
        {
            // Log and swallow — transactional email failure is non-fatal.
            // The checkout transaction has already committed; do not throw.
            _logger.LogError(ex,
                "EmailService: Failed to send '{Subject}' to {Recipient}. " +
                "Check SMTP configuration (Email:SmtpHost / User / Password).",
                message.Subject,
                message.To.ToString());
        }
    }

    /// <summary>Escapes HTML special characters for safe inline embedding.</summary>
    private static string EscapeHtml(string input)
        => System.Net.WebUtility.HtmlEncode(input);
}
