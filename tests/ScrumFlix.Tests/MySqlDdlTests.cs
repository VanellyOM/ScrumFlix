/*
 * File:    tests/ScrumFlix.Tests/MySqlDdlTests.cs
 * Purpose: Unit tests for the pure DDL helpers used by the schema backup:
 *            - MySqlDdl.Quote: identifier back-tick quoting and escaping
 *            - MySqlDdl.StripDefiner: DEFINER-clause removal for portability
 *
 * These guard the two most error-prone string transforms in the backup path.
 * No database required.
 */

using ScrumFlix.Services.Backup;
using Xunit;

namespace ScrumFlix.Tests;

public class MySqlDdlTests
{
    // ── Quote ──────────────────────────────────────────────────────────────

    [Fact]
    public void Quote_WrapsIdentifierInBackticks()
    {
        Assert.Equal("`Movies`", MySqlDdl.Quote("Movies"));
    }

    [Fact]
    public void Quote_EscapesEmbeddedBacktickByDoubling()
    {
        // A back-tick inside the identifier must be doubled so the quoting is safe.
        Assert.Equal("`we`` ird`", MySqlDdl.Quote("we` ird"));
    }

    // ── StripDefiner ───────────────────────────────────────────────────────

    [Fact]
    public void StripDefiner_RemovesBacktickQuotedClause()
    {
        const string ddl = "CREATE DEFINER=`admin`@`%` PROCEDURE `GenerateSeatsForScreen`() BEGIN END";
        var result = MySqlDdl.StripDefiner(ddl)!;

        Assert.DoesNotContain("DEFINER", result);
        Assert.StartsWith("CREATE PROCEDURE `GenerateSeatsForScreen`", result);
    }

    [Fact]
    public void StripDefiner_RemovesQuotedHostClause()
    {
        const string ddl = "CREATE DEFINER=`svc`@`10.0.0.1` TRIGGER trg BEFORE INSERT ON t FOR EACH ROW BEGIN END";
        var result = MySqlDdl.StripDefiner(ddl)!;

        Assert.DoesNotContain("DEFINER", result);
        Assert.Contains("TRIGGER trg", result);
    }

    [Fact]
    public void StripDefiner_RemovesClauseInView()
    {
        const string ddl =
            "CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`localhost` SQL SECURITY DEFINER VIEW `v` AS SELECT 1";
        var result = MySqlDdl.StripDefiner(ddl)!;

        Assert.DoesNotContain("DEFINER=", result);
        // The rest of the statement (including the SQL SECURITY clause) is preserved.
        Assert.Contains("VIEW `v` AS SELECT 1", result);
    }

    [Fact]
    public void StripDefiner_LeavesDdlWithoutDefinerUnchanged()
    {
        const string ddl = "CREATE PROCEDURE `p`() BEGIN SELECT 1; END";
        Assert.Equal(ddl, MySqlDdl.StripDefiner(ddl));
    }

    [Fact]
    public void StripDefiner_NullIn_NullOut()
    {
        Assert.Null(MySqlDdl.StripDefiner(null));
    }

    [Fact]
    public void StripDefiner_OnlyRemovesFirstOccurrence()
    {
        // A DEFINER token appearing in a string literal in the body should not be
        // collateral damage — only the leading object DEFINER clause is removed.
        const string ddl =
            "CREATE DEFINER=`a`@`b` PROCEDURE `p`() BEGIN SELECT 'DEFINER=`x`@`y` text'; END";
        var result = MySqlDdl.StripDefiner(ddl)!;

        Assert.StartsWith("CREATE PROCEDURE `p`", result);
        Assert.Contains("'DEFINER=`x`@`y` text'", result);   // literal untouched
    }
}
