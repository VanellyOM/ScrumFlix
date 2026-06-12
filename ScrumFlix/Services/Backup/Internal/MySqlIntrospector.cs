/*
 * File:      /ScrumFlix/Services/Backup/Internal/MySqlIntrospector.cs
 * Namespace: ScrumFlix.Services.Backup
 * Purpose:   Thin, provider-agnostic helper for reading DDL out of the live
 *            MySQL database via the connection EF Core already owns.
 *
 *            This is the single place that drops below EF Core to ADO.NET
 *            (System.Data.Common). Every schema/routine/view/trigger provider
 *            goes through here so connection-lifetime handling and identifier
 *            quoting live in exactly one spot.
 *
 * Why raw ADO instead of EF Core:
 *            EF Core models entities, not the database's own DDL. There is no
 *            EF API that yields "the CREATE TABLE statement MySQL would emit",
 *            and the canonical Aiven schema is the ground truth (created out of
 *            band, never via migrations — see AppDbContext header). The portable
 *            way to capture it is the server's own SHOW CREATE ... output.
 *
 * Why not mysqldump:
 *            Somee.com shared hosting gives no shell access and no mysqldump
 *            binary. SHOW CREATE over the existing pooled connection is the only
 *            mechanism available from inside the web process.
 *
 * Connection discipline:
 *            EF's DbConnection may already be open and mid-scope. OpenIfClosed
 *            opens it only if needed and reports whether it did, so the caller
 *            closes it only if it was the one to open it. We never dispose the
 *            connection — it belongs to the scoped AppDbContext.
 *
 * Security:
 *            Identifiers obtained from INFORMATION_SCHEMA cannot be passed as
 *            parameters to SHOW CREATE (MySQL forbids it), so they are validated
 *            and back-tick quoted. Schema *values* (the database name) are passed
 *            as real parameters wherever a normal query allows it.
 *
 * Dependencies: System.Data, System.Data.Common only (BCL — no NuGet).
 */

using System.Data;
using System.Data.Common;

namespace ScrumFlix.Services.Backup;

/// <summary>
/// Reads schema-level DDL (tables, routines, views, triggers) from the live
/// MySQL database using the connection owned by <see cref="AppDbContext"/>.
/// </summary>
/// <remarks>
/// Instances are cheap and stateless beyond the borrowed connection; create one
/// per backup run. All methods are safe to call sequentially on the same
/// connection. Not thread-safe — a single <see cref="DbConnection"/> cannot run
/// concurrent commands.
/// </remarks>
internal sealed class MySqlIntrospector
{
    private readonly DbConnection _conn;

    public MySqlIntrospector(DbConnection conn) => _conn = conn;

    // ── Connection lifetime ────────────────────────────────────────────────

    /// <summary>
    /// Opens the borrowed connection if it is not already open.
    /// </summary>
    /// <returns>
    /// True if this call opened the connection (the caller is then responsible
    /// for closing it); false if it was already open (leave it alone).
    /// </returns>
    public async Task<bool> OpenIfClosedAsync(CancellationToken ct)
    {
        if (_conn.State == ConnectionState.Open) return false;
        await _conn.OpenAsync(ct);
        return true;
    }

    /// <summary>Closes the connection. Call only if <see cref="OpenIfClosedAsync"/> returned true.</summary>
    public Task CloseAsync() => _conn.CloseAsync();

    /// <summary>Returns the current schema (database) name reported by the server.</summary>
    public async Task<string> GetDatabaseNameAsync(CancellationToken ct)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT DATABASE()";
        var result = await cmd.ExecuteScalarAsync(ct);
        return result as string ?? _conn.Database ?? string.Empty;
    }

    // ── Object enumeration (INFORMATION_SCHEMA — parameterised) ─────────────

    /// <summary>Lists base table names in the current schema, alphabetically.</summary>
    public Task<List<string>> GetBaseTableNamesAsync(string schema, CancellationToken ct) =>
        QueryStringsAsync(
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES " +
            "WHERE TABLE_SCHEMA = @schema AND TABLE_TYPE = 'BASE TABLE' " +
            "ORDER BY TABLE_NAME",
            schema, ct);

    /// <summary>Lists view names in the current schema, alphabetically.</summary>
    public Task<List<string>> GetViewNamesAsync(string schema, CancellationToken ct) =>
        QueryStringsAsync(
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.VIEWS " +
            "WHERE TABLE_SCHEMA = @schema ORDER BY TABLE_NAME",
            schema, ct);

    /// <summary>Lists trigger names in the current schema, alphabetically.</summary>
    public Task<List<string>> GetTriggerNamesAsync(string schema, CancellationToken ct) =>
        QueryStringsAsync(
            "SELECT TRIGGER_NAME FROM INFORMATION_SCHEMA.TRIGGERS " +
            "WHERE TRIGGER_SCHEMA = @schema ORDER BY TRIGGER_NAME",
            schema, ct);

    /// <summary>
    /// Lists stored routines in the current schema as (name, type) pairs where
    /// type is "PROCEDURE" or "FUNCTION", ordered by type then name.
    /// </summary>
    public async Task<List<(string Name, string Type)>> GetRoutinesAsync(string schema, CancellationToken ct)
    {
        var routines = new List<(string, string)>();
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText =
            "SELECT ROUTINE_NAME, ROUTINE_TYPE FROM INFORMATION_SCHEMA.ROUTINES " +
            "WHERE ROUTINE_SCHEMA = @schema ORDER BY ROUTINE_TYPE, ROUTINE_NAME";
        AddParam(cmd, "@schema", schema);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            routines.Add((reader.GetString(0), reader.GetString(1)));
        return routines;
    }

    // ── SHOW CREATE … (DDL capture — identifier-quoted) ────────────────────

    /// <summary>Returns the <c>CREATE TABLE</c> statement for a table, or null if unavailable.</summary>
    public Task<string?> ShowCreateTableAsync(string table, CancellationToken ct) =>
        ShowCreateAsync($"SHOW CREATE TABLE {MySqlDdl.Quote(table)}", "Create Table", ct);

    /// <summary>Returns the <c>CREATE VIEW</c> statement with the DEFINER clause stripped.</summary>
    public async Task<string?> ShowCreateViewAsync(string view, CancellationToken ct) =>
        MySqlDdl.StripDefiner(await ShowCreateAsync($"SHOW CREATE VIEW {MySqlDdl.Quote(view)}", "Create View", ct));

    /// <summary>Returns the <c>CREATE PROCEDURE</c> statement with the DEFINER clause stripped.</summary>
    public async Task<string?> ShowCreateProcedureAsync(string name, CancellationToken ct) =>
        MySqlDdl.StripDefiner(await ShowCreateAsync($"SHOW CREATE PROCEDURE {MySqlDdl.Quote(name)}", "Create Procedure", ct));

    /// <summary>Returns the <c>CREATE FUNCTION</c> statement with the DEFINER clause stripped.</summary>
    public async Task<string?> ShowCreateFunctionAsync(string name, CancellationToken ct) =>
        MySqlDdl.StripDefiner(await ShowCreateAsync($"SHOW CREATE FUNCTION {MySqlDdl.Quote(name)}", "Create Function", ct));

    /// <summary>Returns the <c>CREATE TRIGGER</c> statement with the DEFINER clause stripped.</summary>
    public async Task<string?> ShowCreateTriggerAsync(string name, CancellationToken ct) =>
        MySqlDdl.StripDefiner(await ShowCreateAsync($"SHOW CREATE TRIGGER {MySqlDdl.Quote(name)}", "SQL Original Statement", ct));

    // ── Internals ──────────────────────────────────────────────────────────

    /// <summary>
    /// Runs a SHOW CREATE statement and returns the DDL column by name. MySQL
    /// returns the DDL under a column whose name varies by object type
    /// (e.g. "Create Table", "SQL Original Statement"); we look it up by name
    /// and fall back to the last column if the exact name is absent.
    /// </summary>
    private async Task<string?> ShowCreateAsync(string sql, string ddlColumn, CancellationToken ct)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText = sql;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        var ordinal = TryGetOrdinal(reader, ddlColumn);
        if (ordinal < 0) ordinal = reader.FieldCount - 1;   // defensive fallback
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static int TryGetOrdinal(DbDataReader reader, string name)
    {
        for (int i = 0; i < reader.FieldCount; i++)
            if (string.Equals(reader.GetName(i), name, StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    private async Task<List<string>> QueryStringsAsync(string sql, string schema, CancellationToken ct)
    {
        var list = new List<string>();
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText = sql;
        AddParam(cmd, "@schema", schema);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            if (!reader.IsDBNull(0)) list.Add(reader.GetString(0));
        return list;
    }

    private static void AddParam(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
