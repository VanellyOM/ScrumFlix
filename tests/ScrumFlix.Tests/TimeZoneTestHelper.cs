/*
 * File:    tests/ScrumFlix.Tests/TimeZoneTestHelper.cs
 * Purpose: Previously the only way for tests to resolve Central Time in a
 *          try/catch — a parallel re-implementation of app-side logic.
 *
 *          Now that TimeZoneHelper (ScrumFlix.Infrastructure) is part of the
 *          main assembly referenced by this test project, call sites should use
 *          TimeZoneHelper.Resolve(TimeZoneHelper.CentralWindowsId) directly.
 *
 *          This file is retained so the QrTicketPayloadTests and
 *          QrConcessionPayloadTests that still reference ResolveCentral()
 *          continue to compile until they are migrated. Once all callers are
 *          updated, delete this file.
 */

using System;
using ScrumFlix.Infrastructure;

namespace ScrumFlix.Tests;

[Obsolete("Use TimeZoneHelper.Resolve(TimeZoneHelper.CentralWindowsId) directly.")]
internal static class TimeZoneTestHelper
{
    /// <summary>
    /// Resolves US Central Time.
    /// Prefer <see cref="TimeZoneHelper.Resolve"/> for new test code.
    /// </summary>
    public static TimeZoneInfo? ResolveCentral()
        => TimeZoneHelper.Resolve(TimeZoneHelper.CentralWindowsId);
}
