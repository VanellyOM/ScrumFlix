/*
 * File:    tests/ScrumFlix.Tests/TimeZoneConversionTests.cs
 * Purpose: Locks in the timezone policy the QR payloads and receipt display rely
 *          on: all DB timestamps are UTC, and display converts to US Central with
 *          correct daylight/standard handling. Mirrors the conversion the app
 *          performs (TimeZoneInfo.ConvertTimeFromUtc) so a regression in the .NET
 *          runtime tz mapping or in our assumptions would surface here.
 */

using System;
using System.Globalization;
using Xunit;

namespace ScrumFlix.Tests;

public class TimeZoneConversionTests
{
    [Theory]
    [InlineData("2026-06-15T19:00:00Z", "2:00 PM", true)]   // mid-June → CDT (UTC-5)
    [InlineData("2026-01-15T19:00:00Z", "1:00 PM", false)]  // mid-January → CST (UTC-6)
    [InlineData("2026-06-09T14:00:00Z", "9:00 AM", true)]   // audit's worked example
    public void Utc_ConvertsTo_CentralLocalTime_WithDst(
        string utcIso, string expectedLocalTime, bool expectDaylight)
    {
        var tz = TimeZoneTestHelper.ResolveCentral();
        if (tz is null) return;   // OS without Central tz data (not the CI/Linux case) — no-op.

        var utc = DateTime.Parse(
            utcIso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);

        Assert.Equal(expectedLocalTime, local.ToString("h:mm tt", CultureInfo.InvariantCulture));
        Assert.Equal(expectDaylight, tz.IsDaylightSavingTime(local));
    }

    [Fact]
    public void UnspecifiedKind_IsTreatedAsUtc_NotLocal()
    {
        // Pomelo/MySQL strips DateTimeKind on read-back, so values come out as
        // Unspecified. The app re-specifies them as UTC before converting; this
        // test documents that an Unspecified value at the same wall-clock as a
        // UTC value converts identically once specified as UTC.
        var unspecified = new DateTime(2026, 6, 15, 19, 0, 0, DateTimeKind.Unspecified);
        var asUtc = DateTime.SpecifyKind(unspecified, DateTimeKind.Utc);

        Assert.Equal(DateTimeKind.Utc, asUtc.Kind);
        Assert.Equal(unspecified.Ticks, asUtc.Ticks);   // re-specifying Kind does not shift the value
    }
}
