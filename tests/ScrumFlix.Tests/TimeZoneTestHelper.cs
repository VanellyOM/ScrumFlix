/*
 * File:    tests/ScrumFlix.Tests/TimeZoneTestHelper.cs
 * Purpose: Resolves US Central time the same way QrCodeService does — accepting
 *          either the IANA id ("America/Chicago") or the Windows id
 *          ("Central Standard Time"). .NET maps between them on both Linux and
 *          Windows, but returns null if neither resolves so tests can no-op
 *          gracefully on an OS without tz data instead of failing spuriously.
 */

using System;

namespace ScrumFlix.Tests;

internal static class TimeZoneTestHelper
{
    public static TimeZoneInfo? ResolveCentral()
    {
        foreach (var id in new[] { "America/Chicago", "Central Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch { /* try the next identifier */ }
        }
        return null;
    }
}
