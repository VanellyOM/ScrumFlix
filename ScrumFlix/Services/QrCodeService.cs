/*
 * File: /ScrumFlix/Services/QrCodeService.cs
 * Description: Service that generates Base64-encoded PNG QR code images.
 *
 * Phase 3 — P3-9:
 *   Uses QRCoder 1.8.0 (PngByteQRCode renderer).
 *   PngByteQRCode has no System.Drawing / GDI+ dependency — safe on Linux.
 *
 * Structured payload addition:
 *   GenerateBase64Png(long) retained for backward compatibility.
 *   GenerateBase64PngWithPayload(string) added so callers can encode a richer
 *   structured string (e.g. movie, date, seat) rather than just the ticket code.
 *   GenerateBase64PngBatch overloaded to accept pre-built payload strings.
 *
 * Recommended payload format (pipe-delimited, scannable by any QR reader):
 *   SCRUMFLIX|CODE:847203|MOVIE:Inception|DATE:2026-05-13|TIME:2:00 PM CDT|SEAT:B7|SCREEN:N Screen 1
 *
 * Timezone policy:
 *   All DateTime values stored in the database are UTC (DateTime.UtcNow at write time).
 *   QR payload strings display times in US Central Time (America/Chicago) so the
 *   printed time matches what Texas customers expect to see on their receipt.
 *   ToCentral() converts any UTC DateTime to Central before formatting.
 *   The timezone ID "Central Standard Time" is the Windows identifier; on Linux
 *   (Docker / Aiven) .NET 6+ automatically maps this to "America/Chicago" via
 *   TimeZoneInfo — no tzdata package required on modern .NET runtimes.
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

    // ── Timezone ──────────────────────────────────────────────────────────────
    // All DB timestamps are stored as UTC. QR payloads display the theater's
    // local time so customers see the correct time for their location.
    // "Central Standard Time" is used as the fallback for concession receipts,
    // which are always processed at a Texas location.
    // TimeZoneInfo.FindSystemTimeZoneById() accepts Windows IDs on both Windows
    // and Linux — .NET auto-maps to IANA (e.g. "Central Standard Time" →
    // "America/Chicago"). Covers CST/CDT, MST/MDT, PST/PDT etc. automatically.
    private static readonly TimeZoneInfo CentralTz = InitCentralTimeZone();

    private static TimeZoneInfo InitCentralTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    /// <summary>
    /// Resolves a <see cref="TimeZoneInfo"/> from a Windows timezone ID string.
    /// Falls back to Central Time if the ID is null, empty, or unrecognised
    /// so a bad Location.TimeZoneId never crashes QR generation.
    /// </summary>
    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId)) return CentralTz;
        try   { return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch { return CentralTz; }
    }

    /// <summary>
    /// Converts a UTC <see cref="DateTime"/> to the given local timezone.
    /// If <paramref name="utc"/> has <see cref="DateTimeKind.Unspecified"/> it is
    /// treated as UTC — Pomelo/MySQL strips the Kind flag on read-back.
    /// </summary>
    private static DateTime ToLocal(DateTime utc, TimeZoneInfo tz)
    {
        if (utc.Kind == DateTimeKind.Unspecified)
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
    }

    /// <summary>
    /// Formats a UTC DateTime in the given timezone, appending the correct
    /// DST/standard abbreviation (e.g. "2:00 PM CDT", "7:00 PM PST").
    /// The abbreviation is derived from the timezone rules, not hardcoded.
    /// </summary>
    private static string FormatLocalTime(DateTime utc, string format, TimeZoneInfo tz)
    {
        var local = ToLocal(utc, tz);
        // Build the abbreviation from the timezone's display name segments.
        // TimeZoneInfo.GetAbbreviation() is not public, so we derive it:
        //   "Central Standard Time" → standard abbr = "CST", DST abbr = "CDT"
        //   "Eastern Standard Time" → "EST" / "EDT"
        //   "Pacific Standard Time" → "PST" / "PDT"
        // Pattern: take each word's first letter except "Time" and "Standard"/"Daylight".
        var isDst  = tz.IsDaylightSavingTime(local);
        var abbr   = BuildAbbreviation(tz, isDst);
        return local.ToString(format) + " " + abbr;
    }

    /// <summary>
    /// Derives a short timezone abbreviation from a <see cref="TimeZoneInfo"/>.
    /// Uses the standard or daylight display name to extract initials
    /// (e.g. "Central Standard Time" → "CST", "Central Daylight Time" → "CDT").
    /// Falls back to the UTC offset string if the name cannot be parsed.
    /// </summary>
    private static string BuildAbbreviation(TimeZoneInfo tz, bool isDst)
    {
        // TimeZoneInfo exposes StandardName and DaylightName (e.g. "Central Standard Time")
        var name  = isDst ? tz.DaylightName : tz.StandardName;
        // Take the first letter of each word, skip generic words
        var skip  = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { "time", "and", "of", "the" };
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var abbr  = string.Concat(parts
            .Where(p => !skip.Contains(p))
            .Select(p => p[0]));
        // Validate: should be 2–4 uppercase letters (CST, CDT, EST, EDT, MST, PST…)
        return abbr.Length is >= 2 and <= 4
            ? abbr.ToUpperInvariant()
            : (isDst ? tz.BaseUtcOffset + TimeSpan.FromHours(1) : tz.BaseUtcOffset)
                .ToString(@"hh\:mm");
    }

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
    /// Format: SCRUMFLIX|CODE:xxx|MOVIE:xxx|DATE:xxx|TIME:xxx CDT|SEAT:xxx|SCREEN:xxx|LOCATION:xxx
    /// All fields present — "N/A" used when data is unavailable.
    /// DATE and TIME are expressed in the theater location's local timezone
    /// (resolved from <paramref name="timeZoneId"/>) so the ticket always shows
    /// the time the customer expects, regardless of where the server runs.
    /// <para>
    /// NOTE: <paramref name="showTime"/> is <c>Showtime.StartTime</c>, currently
    /// stored as the admin-entered local time (no UTC conversion at write time).
    /// The <paramref name="timeZoneId"/> parameter is accepted now so the call
    /// site is already location-aware once the StartTime UTC migration is applied.
    /// Until that migration runs, pass the raw StartTime and the location's ID —
    /// the conversion is a no-op when Kind = Unspecified and the value is already local.
    /// </para>
    /// </summary>
    public static string BuildTicketPayload(
        long ticketCode,
        string? movieName,
        DateTime? showTime,
        string? seatLabel,
        string? screenName,
        string? locationName,
        string? timeZoneId = null)
    {
        // showTime = Showtime.StartTime, now stored as UTC (converted at write time
        // in AdminManageController.ShowtimeCreate/Edit using the location's TimeZoneId).
        // Convert to the location's local time for display on the QR code.
        var tz   = ResolveTimeZone(timeZoneId);
        var date = showTime.HasValue ? ToLocal(showTime.Value, tz).ToString("yyyy-MM-dd")  : "N/A";
        var time = showTime.HasValue ? FormatLocalTime(showTime.Value, "h:mm tt", tz)       : "N/A";

        var seat   = string.IsNullOrEmpty(seatLabel)    ? "GA"  : seatLabel;
        var screen = string.IsNullOrEmpty(screenName)   ? "N/A" : screenName;
        var loc    = string.IsNullOrEmpty(locationName) ? "N/A" : locationName;
        var movie  = string.IsNullOrEmpty(movieName)    ? "N/A" : movieName;

        return $"SCRUMFLIX|CODE:{ticketCode}|MOVIE:{movie}|DATE:{date}|TIME:{time}|SEAT:{seat}|SCREEN:{screen}|LOCATION:{loc}";
    }

    /// <summary>
    /// Builds the structured QR payload string for a concession order receipt.
    /// Format: SCRUMFLIX-CONCESSIONS|ORDER:xxx|DATE:xxx|TIME:xxx CDT|{Item}x{Qty},...|TOTAL:$xx.xx
    /// Customers show this QR code at the concession stand to collect pre-purchased items.
    /// <paramref name="timeOfSale"/> is UTC (<c>DateTime.UtcNow</c> at checkout) and is
    /// converted to the location's local time using <paramref name="timeZoneId"/>.
    /// Defaults to Central Time when <paramref name="timeZoneId"/> is null — appropriate
    /// for the current all-Texas deployment.
    /// </summary>
    public static string BuildConcessionPayload(
        int orderId,
        DateTime timeOfSale,
        IEnumerable<(string ItemName, int Quantity)> items,
        decimal total,
        string? timeZoneId = null)
    {
        var tz       = ResolveTimeZone(timeZoneId);
        var date     = ToLocal(timeOfSale, tz).ToString("yyyy-MM-dd");
        var time     = FormatLocalTime(timeOfSale, "h:mm tt", tz);
        var itemList = string.Join(",", items.Select(i => $"{i.ItemName}x{i.Quantity}"));
        var totalStr = total.ToString("C");

        return $"SCRUMFLIX-CONCESSIONS|ORDER:{orderId}|DATE:{date}|TIME:{time}|{itemList}|TOTAL:{totalStr}";
    }
}
