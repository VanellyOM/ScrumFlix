/*
 * File:    tests/ScrumFlix.Tests/QrTicketPayloadTests.cs
 * Purpose: Exercises the REAL QrCodeService.BuildTicketPayload static builder
 *          (no DB, no QR rendering) — verifies the pipe-delimited payload
 *          format, the "N/A" / "GA" fallbacks, and that a UTC showtime is
 *          converted to the theater's local (Central) time for display.
 *
 * Expected format:
 *   SCRUMFLIX|CODE:..|MOVIE:..|DATE:..|TIME:.. <ABBR>|SEAT:..|SCREEN:..|LOCATION:..
 *
 * Assertions stick to the assertion surface shared by xUnit v2 and v3
 * (Fact/Theory/InlineData/Assert.*) so the project compiles regardless of which
 * v3-specific assertion helpers are adopted later.
 */

using ScrumFlix.Infrastructure;
using ScrumFlix.Services;
using System;
using System.Globalization;
using Xunit;

namespace ScrumFlix.Tests;

public class QrTicketPayloadTests
{
    [Fact]
    public void BuildTicketPayload_HasScrumflixPrefix_AndAllEightSegments()
    {
        // showTime = null keeps DATE/TIME tz-independent for the structural checks.
        var payload = QrCodeService.BuildTicketPayload(
            ticketCode: 847203,
            movieName: "Inception",
            showTime: null,
            seatLabel: "B7",
            screenName: "N Screen 1",
            locationName: "Mesquite",
            timeZoneId: null);

        Assert.StartsWith("SCRUMFLIX|CODE:847203|", payload);
        Assert.Contains("|MOVIE:Inception|", payload);
        Assert.Contains("|DATE:N/A|", payload);   // null showtime → N/A
        Assert.Contains("|TIME:N/A|", payload);
        Assert.Contains("|SEAT:B7|", payload);
        Assert.Contains("|SCREEN:N Screen 1|", payload);
        Assert.EndsWith("|LOCATION:Mesquite", payload);

        // "SCRUMFLIX" header + 7 key:value fields = 8 pipe-delimited segments.
        Assert.Equal(8, payload.Split('|').Length);
    }

    [Theory]
    [InlineData(null, "|MOVIE:N/A|")]
    [InlineData("", "|MOVIE:N/A|")]
    [InlineData("Dune", "|MOVIE:Dune|")]
    public void BuildTicketPayload_MovieName_FallsBackToNA(string? movie, string expected)
    {
        var payload = QrCodeService.BuildTicketPayload(1, movie, null, "A1", "S1", "Loc");
        Assert.Contains(expected, payload);
    }

    [Theory]
    [InlineData(null, "|SEAT:GA|")]   // null seat → general admission
    [InlineData("", "|SEAT:GA|")]
    [InlineData("C4", "|SEAT:C4|")]
    public void BuildTicketPayload_Seat_DefaultsToGeneralAdmission(string? seat, string expected)
    {
        var payload = QrCodeService.BuildTicketPayload(1, "M", null, seat, "S1", "Loc");
        Assert.Contains(expected, payload);
    }

    [Theory]
    [InlineData(null, "|SCREEN:N/A|", "|LOCATION:N/A")]
    [InlineData("", "|SCREEN:N/A|", "|LOCATION:N/A")]
    public void BuildTicketPayload_ScreenAndLocation_FallBackToNA(
        string? value, string expectedScreen, string expectedLocation)
    {
        var payload = QrCodeService.BuildTicketPayload(1, "M", null, "A1", value, value);
        Assert.Contains(expectedScreen, payload);
        Assert.EndsWith(expectedLocation, payload);
    }

    [Fact]
    public void BuildTicketPayload_ConvertsUtcShowtimeToCentralLocalTime()
    {
        // 2026-06-16 02:00 UTC → 2026-06-15 9:00 PM CDT in America/Chicago.
        // The UTC calendar date is the 16th, so a DATE of 2026-06-15 in the
        // payload proves the value was converted out of UTC, not formatted raw.
        var utc = new DateTime(2026, 6, 16, 2, 0, 0, DateTimeKind.Utc);

        // FIX: Replaced obsolete TimeZoneTestHelper with TimeZoneHelper
        var tz = TimeZoneHelper.Resolve(TimeZoneHelper.CentralWindowsId);
        if (tz is null) return;   // OS without Central tz data (not the CI/Linux case) — no-op.

        var payload = QrCodeService.BuildTicketPayload(
            847203, "Inception", utc, "B7", "N Screen 1", "Mesquite", "Central Standard Time");

        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);

        Assert.Contains("|DATE:" + local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "|", payload);
        // TIME field is "<h:mm tt> <ABBR>" — assert the time portion plus its trailing space.
        Assert.Contains("|TIME:" + local.ToString("h:mm tt", CultureInfo.InvariantCulture) + " ", payload);
        Assert.Contains("CDT", payload);   // mid-June → daylight abbreviation
    }
}
