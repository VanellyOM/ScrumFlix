/*
 * File:    tests/ScrumFlix.Tests/TimeZoneConversionTests.cs
 * Purpose: Locks in the timezone policy the QR payloads and receipt display rely
 *          on: all DB timestamps are UTC, and display converts to the correct
 *          local timezone with proper daylight/standard handling.
 *          Mirrors the conversion the app performs (TimeZoneHelper.ConvertFromUtc)
 *          so a regression in the .NET runtime tz mapping or in our assumptions
 *          would surface here.
 *
 * Migration note: previously called TimeZoneTestHelper.ResolveCentral() (a
 * test-project-only helper). Now uses TimeZoneHelper.Resolve() directly, which
 * is the same code path the application uses at runtime — meaning these tests
 * exercise the real helper rather than a parallel re-implementation of it.
 */

using System;
using System.Globalization;
using ScrumFlix.Infrastructure;
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
        // Use TimeZoneHelper.Resolve() — the same code path the application takes.
        // Returns the Central fallback on any OS that can resolve the ID.
        var tz = TimeZoneHelper.Resolve(TimeZoneHelper.CentralWindowsId);

        var utc = DateTime.Parse(
            utcIso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        var local = TimeZoneHelper.ConvertFromUtc(utc, tz);

        Assert.Equal(expectedLocalTime, local.ToString("h:mm tt", CultureInfo.InvariantCulture));
        Assert.Equal(expectDaylight, tz.IsDaylightSavingTime(local));
    }

    [Fact]
    public void UnspecifiedKind_IsTreatedAsUtc_NotLocal()
    {
        // Pomelo/MySQL strips DateTimeKind on read-back, so values come out as
        // Unspecified. TimeZoneHelper.ConvertFromUtc() re-specifies them as UTC
        // before converting; this test documents that an Unspecified value at the
        // same wall-clock as a UTC value converts identically once re-specified.
        var unspecified = new DateTime(2026, 6, 15, 19, 0, 0, DateTimeKind.Unspecified);
        var asUtc       = DateTime.SpecifyKind(unspecified, DateTimeKind.Utc);

        Assert.Equal(DateTimeKind.Utc, asUtc.Kind);
        Assert.Equal(unspecified.Ticks, asUtc.Ticks); // re-specifying Kind does not shift the value

        // Confirm ConvertFromUtc handles Unspecified input without throwing
        var tz    = TimeZoneHelper.Resolve(TimeZoneHelper.CentralWindowsId);
        var local = TimeZoneHelper.ConvertFromUtc(unspecified, tz); // Unspecified input
        Assert.Equal(TimeZoneHelper.ConvertFromUtc(asUtc, tz), local);
    }

    [Theory]
    [InlineData("Pacific Standard Time",  false, "PST")]
    [InlineData("Pacific Standard Time",  true,  "PDT")]
    [InlineData("Central Standard Time",  false, "CST")]
    [InlineData("Central Standard Time",  true,  "CDT")]
    [InlineData("Eastern Standard Time",  false, "EST")]
    [InlineData("Eastern Standard Time",  true,  "EDT")]
    [InlineData("Mountain Standard Time", false, "MST")]
    [InlineData("Mountain Standard Time", true,  "MDT")]
    public void BuildAbbreviation_ProducesCorrectInitials(
        string windowsId, bool isDst, string expected)
    {
        var tz   = TimeZoneHelper.Resolve(windowsId);
        var abbr = TimeZoneHelper.BuildAbbreviation(tz, isDst);
        Assert.Equal(expected, abbr);
    }

    [Fact]
    public void Resolve_UnknownId_FallsBackToCentral()
    {
        var tz      = TimeZoneHelper.Resolve("Not A Real Timezone ID");
        var central = TimeZoneHelper.Resolve(TimeZoneHelper.CentralWindowsId);
        Assert.Equal(central.Id, tz.Id);
    }

    [Fact]
    public void Resolve_NullOrEmpty_FallsBackToCentral()
    {
        var central = TimeZoneHelper.Resolve(TimeZoneHelper.CentralWindowsId);
        Assert.Equal(central.Id, TimeZoneHelper.Resolve(null).Id);
        Assert.Equal(central.Id, TimeZoneHelper.Resolve("").Id);
        Assert.Equal(central.Id, TimeZoneHelper.Resolve("   ").Id);
    }

    [Theory]
    [InlineData("2026-06-15T02:00:00Z", "Pacific Standard Time",  "2026-06-14")]  // PDT UTC-7 → June 14
    [InlineData("2026-01-15T02:00:00Z", "Pacific Standard Time",  "2026-01-14")]  // PST UTC-8 → Jan 14
    [InlineData("2026-06-15T02:00:00Z", "Central Standard Time",  "2026-06-14")]  // CDT UTC-5 → June 14
    [InlineData("2026-06-15T07:00:00Z", "Central Standard Time",  "2026-06-15")]  // CDT 2AM → June 15
    public void ConvertFromUtc_CrossesMidnight_CorrectDate(
        string utcIso, string windowsId, string expectedDate)
    {
        var utc   = DateTime.Parse(utcIso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        var tz    = TimeZoneHelper.Resolve(windowsId);
        var local = TimeZoneHelper.ConvertFromUtc(utc, tz);
        Assert.Equal(expectedDate, local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }
}
