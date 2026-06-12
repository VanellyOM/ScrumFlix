/*
 * File:      /ScrumFlix/Services/Backup/Internal/MySqlDdl.cs
 * Namespace: ScrumFlix.Services.Backup
 * Purpose:   Pure (no-I/O) string helpers for working with MySQL DDL: identifier
 *            quoting and DEFINER-clause stripping. Separated from MySqlIntrospector
 *            (which does the actual database I/O) so this bug-prone string logic
 *            can be unit-tested directly without a live connection.
 */

using System.Text.RegularExpressions;

namespace ScrumFlix.Services.Backup;

/// <summary>Pure string transforms over MySQL DDL. No database access.</summary>
public static class MySqlDdl
{
    /// <summary>
    /// Matches a MySQL <c>DEFINER=`user`@`host`</c> clause (back-tick, single-quote,
    /// or bare forms) so it can be stripped from routine/view/trigger DDL.
    /// </summary>
    private static readonly Regex DefinerClause = new(
        @"DEFINER\s*=\s*(`(?:[^`]|``)+`|'[^']*'|\S+?)@(`(?:[^`]|``)+`|'[^']*'|\S+)\s*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Back-tick quotes a MySQL identifier, escaping embedded back-ticks by doubling.
    /// Used because SHOW CREATE statements cannot bind the object name as a parameter.
    /// </summary>
    public static string Quote(string identifier) => "`" + identifier.Replace("`", "``") + "`";

    /// <summary>
    /// Removes the first <c>DEFINER=`u`@`h`</c> clause from a DDL string so the object
    /// restores under the importing account rather than a (possibly non-existent)
    /// source-host account. Returns null if <paramref name="ddl"/> is null.
    /// </summary>
    public static string? StripDefiner(string? ddl) =>
        ddl is null ? null : DefinerClause.Replace(ddl, string.Empty, 1);
}
