/*
 * File: /ScrumFlix/Services/QrCodeService.cs
 * Description: Service that generates Base64-encoded PNG QR code images.
 *
 * Phase 3 — P3-9:
 *   Uses QRCoder 1.6.0 (PngByteQRCode renderer).
 *   PngByteQRCode has no System.Drawing / GDI+ dependency — safe on Linux.
 *
 * Structured payload addition:
 *   GenerateBase64Png(long) retained for backward compatibility.
 *   GenerateBase64PngWithPayload(string) added so callers can encode a richer
 *   structured string (e.g. movie, date, seat) rather than just the ticket code.
 *   GenerateBase64PngBatch overloaded to accept pre-built payload strings.
 *
 * Recommended payload format (pipe-delimited, scannable by any QR reader):
 *   SCRUMFLIX|CODE:847203|MOVIE:Inception|DATE:2026-05-13|TIME:2:00 PM|SEAT:B7|SCREEN:N Screen 1
 */

using QRCoder;

namespace ScrumFlix.Services;

/// <summary>
/// Generates QR code PNG images as Base64 strings for embedding in HTML.
/// Safe for Linux and Windows — uses PngByteQRCode (no GDI+ dependency).
/// </summary>
public class QrCodeService
{
    // Pixel size of each QR module. 10px ≈ 250×250px output at ECC_M.
    private const int PixelsPerModule = 10;

    /// <summary>
    /// Generates a Base64-encoded PNG QR code from an arbitrary string payload.
    /// Use this overload when encoding structured ticket info.
    /// </summary>
    /// <param name="payload">The string to encode in the QR code.</param>
    public string GenerateBase64PngWithPayload(string payload)
    {
        using var generator = new QRCodeGenerator();
        var data     = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        var qrCode   = new PngByteQRCode(data);
        var pngBytes = qrCode.GetGraphic(PixelsPerModule);
        return Convert.ToBase64String(pngBytes);
    }

    /// <summary>
    /// Generates a Base64-encoded PNG QR code whose payload is the ticket code string.
    /// Retained for backward compatibility — prefer GenerateBase64PngWithPayload for
    /// new code so the QR contains full ticket context.
    /// </summary>
    /// <param name="ticketCode">The long TicketCode to encode.</param>
    public string GenerateBase64Png(long ticketCode)
        => GenerateBase64PngWithPayload(ticketCode.ToString());

    /// <summary>
    /// Generates Base64 QR codes from a list of pre-built payload strings.
    /// Used by OrderConfirmation when structured payloads are available.
    /// </summary>
    public List<string> GenerateBase64PngBatch(IEnumerable<string> payloads)
        => payloads.Select(GenerateBase64PngWithPayload).ToList();

    /// <summary>
    /// Generates Base64 QR codes for multiple ticket codes.
    /// Retained for backward compatibility — encodes code number only.
    /// </summary>
    public List<string> GenerateBase64PngBatch(IEnumerable<long> ticketCodes)
        => ticketCodes.Select(GenerateBase64Png).ToList();

    /// <summary>
    /// Builds the structured QR payload string for a ticket.
    /// Format: SCRUMFLIX|CODE:xxx|MOVIE:xxx|DATE:xxx|TIME:xxx|SEAT:xxx|SCREEN:xxx
    /// All fields present — "N/A" used when data is unavailable.
    /// </summary>
    public static string BuildTicketPayload(
        long ticketCode,
        string? movieName,
        DateTime? showTime,
        string? seatLabel,
        string? screenName,
        string? locationName)
    {
        var date   = showTime?.ToString("yyyy-MM-dd") ?? "N/A";
        var time   = showTime?.ToString("h:mm tt")    ?? "N/A";
        var seat   = string.IsNullOrEmpty(seatLabel)  ? "GA"  : seatLabel;
        var screen = string.IsNullOrEmpty(screenName) ? "N/A" : screenName;
        var loc    = string.IsNullOrEmpty(locationName) ? "N/A" : locationName;
        var movie  = string.IsNullOrEmpty(movieName)  ? "N/A" : movieName;

        return $"SCRUMFLIX|CODE:{ticketCode}|MOVIE:{movie}|DATE:{date}|TIME:{time}|SEAT:{seat}|SCREEN:{screen}|LOCATION:{loc}";
    }

    /// <summary>
    /// Builds the structured QR payload string for a concession order receipt.
    /// Format: SCRUMFLIX-CONCESSIONS|ORDER:xxx|DATE:xxx|TIME:xxx|{Item}x{Qty},...|TOTAL:$xx.xx
    /// Customers show this QR code at the concession stand to collect pre-purchased items.
    /// </summary>
    public static string BuildConcessionPayload(
        int orderId,
        DateTime timeOfSale,
        IEnumerable<(string ItemName, int Quantity)> items,
        decimal total)
    {
        var date     = timeOfSale.ToString("yyyy-MM-dd");
        var time     = timeOfSale.ToString("h:mm tt");
        var itemList = string.Join(",", items.Select(i => $"{i.ItemName}x{i.Quantity}"));
        var totalStr = total.ToString("C");

        return $"SCRUMFLIX-CONCESSIONS|ORDER:{orderId}|DATE:{date}|TIME:{time}|{itemList}|TOTAL:{totalStr}";
    }
}
