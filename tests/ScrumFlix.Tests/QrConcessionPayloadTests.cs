/*
 * File:    tests/ScrumFlix.Tests/QrConcessionPayloadTests.cs
 * Purpose: Exercises the REAL QrCodeService.BuildConcessionPayload static builder.
 *          Verifies the header, ORDER field, the "ItemxQty,..." join, and TOTAL.
 *
 * Expected format:
 *   SCRUMFLIX-CONCESSIONS|ORDER:..|DATE:..|TIME:.. <ABBR>|Itemx2,Otherx1|TOTAL:$x.xx
 *
 * The TOTAL is rendered with decimal.ToString("C"), whose currency symbol is
 * culture-dependent (e.g. "$12.50" on en-US, "¤12.50" on the invariant culture
 * that .NET uses by default on Linux). The numeric portion "12.50" is stable
 * across cultures, so assertions key off that rather than the symbol.
 */

using System;
using System.Collections.Generic;
using ScrumFlix.Services;
using Xunit;

namespace ScrumFlix.Tests;

public class QrConcessionPayloadTests
{
    private static readonly List<(string ItemName, int Quantity)> SampleItems = new()
    {
        ("Popcorn (Large)", 2),
        ("Soda", 1),
    };

    [Fact]
    public void BuildConcessionPayload_HasHeader_OrderAndItemJoin()
    {
        var timeOfSale = new DateTime(2026, 6, 16, 2, 0, 0, DateTimeKind.Utc);

        var payload = QrCodeService.BuildConcessionPayload(
            orderId: 5567,
            timeOfSale: timeOfSale,
            items: SampleItems,
            total: 12.50m,
            timeZoneId: "Central Standard Time");

        Assert.StartsWith("SCRUMFLIX-CONCESSIONS|ORDER:5567|", payload);
        Assert.Contains("|Popcorn (Large)x2,Sodax1|", payload);
        Assert.Contains("|TOTAL:", payload);
        Assert.Contains("12.50", payload);   // culture-independent numeric portion
    }

    [Fact]
    public void BuildConcessionPayload_EmptyItems_ProducesEmptyItemSegment()
    {
        var timeOfSale = new DateTime(2026, 1, 15, 19, 0, 0, DateTimeKind.Utc);

        var payload = QrCodeService.BuildConcessionPayload(
            orderId: 42,
            timeOfSale: timeOfSale,
            items: new List<(string, int)>(),
            total: 0m,
            timeZoneId: "Central Standard Time");

        Assert.StartsWith("SCRUMFLIX-CONCESSIONS|ORDER:42|", payload);
        Assert.Contains("|TOTAL:", payload);
        // Empty item list collapses to two adjacent pipes before TOTAL.
        Assert.Contains("||TOTAL:", payload);
    }

    [Fact]
    public void BuildConcessionPayload_ConvertsUtcSaleTimeToCentralLocalDate()
    {
        // 2026-06-16 02:00 UTC → 2026-06-15 (21:00) CDT in America/Chicago.
        var timeOfSale = new DateTime(2026, 6, 16, 2, 0, 0, DateTimeKind.Utc);

        var tz = TimeZoneTestHelper.ResolveCentral();
        if (tz is null) return;   // OS without Central tz data — no-op.

        var payload = QrCodeService.BuildConcessionPayload(
            5567, timeOfSale, SampleItems, 12.50m, "Central Standard Time");

        var local = TimeZoneInfo.ConvertTimeFromUtc(timeOfSale, tz);
        Assert.Contains("|DATE:" + local.ToString("yyyy-MM-dd") + "|", payload);
        Assert.Contains("CDT", payload);   // mid-June → daylight abbreviation
    }
}
