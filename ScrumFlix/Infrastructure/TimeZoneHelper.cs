/*
 * File:      /ScrumFlix/Infrastructure/TimeZoneHelper.cs
 * Namespace: ScrumFlix.Infrastructure
 * Purpose:   Single source of truth for all UTC ↔ local-time conversion and
 *            display-formatting logic used across controllers, services, and
 *            view-models.
 *
 * Replaces the following duplicated private statics that existed before this file:
 *   • AdminManageController.TryFindTimeZone(string)           — 4 call sites
 *   • AdminManageController.BuildTimezoneList()               — 4 call sites
 *   • AdminManageController.ExportTickets / ExportConcessions — inline FindSystemTimeZoneById
 *   • QrCodeService.InitCentralTimeZone()
 *   • QrCodeService.ResolveTimeZone(string?)
 *   • QrCodeService.ToLocal(DateTime, TimeZoneInfo)
 *   • QrCodeService.FormatLocalTime(DateTime, string, TimeZoneInfo)
 *   • QrCodeService.BuildAbbreviation(TimeZoneInfo, bool)
 *   • EmailService.FormatCentralTime(DateTime)               — hardcoded Central only
 *
 * Callers after migration:
 *   • AdminManageController  — Resolve(), ConvertToUtc(), ConvertFromUtc(), BuildTimezoneSelectList()
 *   • ShowtimesController    — Resolve(), ConvertFromUtc()                  ← fixes consumer UTC bug
 *   • EmailService           — FormatWithAbbreviation()
 *   • QrCodeService          — Resolve(), ToLocal(), FormatWithAbbreviation()
 *   • TimeZoneConversionTests (test project) — Resolve() replaces TimeZoneTestHelper.ResolveCentral()
 *
 * Timezone ID policy:
 *   All IDs are Windows-style (e.g. "Central Standard Time"). On Linux, .NET
 *   automatically maps these to IANA equivalents via TimeZoneInfo — no tzdata
 *   package is required on modern .NET runtimes. The fallback is always Central.
 */

using Microsoft.AspNetCore.Mvc.Rendering;

namespace ScrumFlix.Infrastructure;

/// <summary>
/// Shared helpers for UTC ↔ local-time conversion, display formatting,
/// and building timezone select lists across all ScrumFlix layers.
/// </summary>
public static class TimeZoneHelper
{
    // ── Constants ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Windows timezone ID for US Central Time (the ScrumFlix home region).
    /// Used as the fallback whenever a location has no TimeZoneId configured
    /// or the stored ID cannot be resolved.
    /// </summary>
    public const string CentralWindowsId = "Central Standard Time";

    // ── Cached fallback ───────────────────────────────────────────────────────

    /// <summary>
    /// Lazily resolved Central Time zone — used as the default fallback.
    /// Falls back to UTC only if Central cannot be resolved (should never
    /// happen on any supported deployment target).
    /// </summary>
    private static readonly TimeZoneInfo FallbackTz = ResolveFallback();

    private static TimeZoneInfo ResolveFallback()
    {
        try   { return TimeZoneInfo.FindSystemTimeZoneById(CentralWindowsId); }
        catch { return TimeZoneInfo.Utc; }
    }

    // ── Resolution ────────────────────────────────────────────────────────────

    /// <summary>
    /// Resolves a <see cref="TimeZoneInfo"/> from a Windows or IANA timezone ID.
    /// Returns the Central Time fallback if <paramref name="timeZoneId"/> is null,
    /// empty, or unrecognised — a bad <c>Location.TimeZoneId</c> value will never
    /// crash a showtime create/edit or a QR payload build.
    /// </summary>
    public static TimeZoneInfo Resolve(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId)) return FallbackTz;
        try   { return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch { return FallbackTz; }
    }

    // ── Conversion ────────────────────────────────────────────────────────────

    /// <summary>
    /// Converts a UTC <see cref="DateTime"/> to the specified local timezone.
    /// If <paramref name="utc"/> has <see cref="DateTimeKind.Unspecified"/> it is
    /// re-specified as UTC before conversion — Pomelo/MySQL strips the Kind flag
    /// on read-back, so all values arriving from the DB must be treated this way.
    /// </summary>
    public static DateTime ConvertFromUtc(DateTime utc, TimeZoneInfo tz)
    {
        if (utc.Kind == DateTimeKind.Unspecified)
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
    }

    /// <summary>
    /// Converts a UTC <see cref="DateTime"/> to the timezone identified by
    /// <paramref name="timeZoneId"/>, resolving the ID with the Central fallback.
    /// Convenience overload so callers with only an ID string avoid a separate
    /// <see cref="Resolve"/> call.
    /// </summary>
    public static DateTime ConvertFromUtc(DateTime utc, string? timeZoneId)
        => ConvertFromUtc(utc, Resolve(timeZoneId));

    /// <summary>
    /// Converts a local <see cref="DateTime"/> (entered in the location's timezone)
    /// to UTC for storage. <paramref name="local"/> is treated as
    /// <see cref="DateTimeKind.Unspecified"/> — the timezone is supplied explicitly
    /// so no assumption about the server's local clock is made.
    /// </summary>
    public static DateTime ConvertToUtc(DateTime local, TimeZoneInfo tz)
    {
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, tz);
    }

    /// <summary>
    /// Converts a local <see cref="DateTime"/> to UTC using the timezone identified
    /// by <paramref name="timeZoneId"/>, resolving the ID with the Central fallback.
    /// </summary>
    public static DateTime ConvertToUtc(DateTime local, string? timeZoneId)
        => ConvertToUtc(local, Resolve(timeZoneId));

    // ── Formatting ────────────────────────────────────────────────────────────

    /// <summary>
    /// Formats a UTC <see cref="DateTime"/> in the given timezone using
    /// <paramref name="format"/>, appending the correct DST/standard abbreviation
    /// (e.g. "2:00 PM CDT", "7:00 PM PST").
    /// The abbreviation is derived from the timezone rules, not hardcoded.
    /// </summary>
    public static string FormatWithAbbreviation(DateTime utc, string format, TimeZoneInfo tz)
    {
        var local  = ConvertFromUtc(utc, tz);
        var isDst  = tz.IsDaylightSavingTime(local);
        var abbr   = BuildAbbreviation(tz, isDst);
        return local.ToString(format) + " " + abbr;
    }

    /// <summary>
    /// Formats a UTC <see cref="DateTime"/> using the timezone identified by
    /// <paramref name="timeZoneId"/>, resolving the ID with the Central fallback.
    /// </summary>
    public static string FormatWithAbbreviation(DateTime utc, string format, string? timeZoneId)
        => FormatWithAbbreviation(utc, format, Resolve(timeZoneId));

    // ── Abbreviation builder ──────────────────────────────────────────────────

    /// <summary>
    /// Derives a short timezone abbreviation from a <see cref="TimeZoneInfo"/>.
    /// Uses the standard or daylight display name to extract initials
    /// (e.g. "Central Standard Time" → "CST", "Central Daylight Time" → "CDT",
    ///       "Pacific Standard Time" → "PST", "Pacific Daylight Time" → "PDT").
    /// Falls back to the UTC offset string if the name cannot produce a
    /// 2–4 character result.
    /// </summary>
    public static string BuildAbbreviation(TimeZoneInfo tz, bool isDst)
    {
        var name  = isDst ? tz.DaylightName : tz.StandardName;
        var skip  = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { "time", "and", "of", "the" };
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var abbr  = string.Concat(parts
            .Where(p => !skip.Contains(p))
            .Select(p => p[0]));
        return abbr.Length is >= 2 and <= 4
            ? abbr.ToUpperInvariant()
            : (isDst ? tz.BaseUtcOffset + TimeSpan.FromHours(1) : tz.BaseUtcOffset)
                .ToString(@"hh\:mm");
    }

    // ── Select list ───────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the list of US timezone options for the location create/edit form
    /// dropdown, ordered from west (Hawaii) to east (Eastern).
    /// Uses Windows timezone IDs, which .NET maps automatically to IANA on Linux.
    /// </summary>
    /// <remarks>
    /// Previously lived as a private static in <c>AdminManageController</c>.
    /// Exposed here so any future Staff Portal or API endpoint that creates or
    /// edits locations can reuse the same ordered list without duplicating it.
    /// </remarks>
    public static List<SelectListItem> BuildTimezoneSelectList() =>
    [
        new() { Value = "Hawaiian Standard Time",      Text = "(UTC-10) Hawaii" },
        new() { Value = "Alaskan Standard Time",       Text = "(UTC-9)  Alaska" },
        new() { Value = "Pacific Standard Time",       Text = "(UTC-8)  Pacific — Los Angeles, Seattle" },
        new() { Value = "US Mountain Standard Time",   Text = "(UTC-7)  Arizona (no DST)" },
        new() { Value = "Mountain Standard Time",      Text = "(UTC-7)  Mountain — Denver, Salt Lake City" },
        new() { Value = "Central Standard Time",       Text = "(UTC-6)  Central — Dallas, Chicago" },
        new() { Value = "Eastern Standard Time",       Text = "(UTC-5)  Eastern — New York, Miami" },
    ];
}
