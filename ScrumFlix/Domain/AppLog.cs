/*
 * File: /ScrumFlix/Domain/AppLog.cs
 * Description: Read-only EF Core entity mapping to the Serilog MySQL sink's Logs table.
 *
 *              Named AppLog (not Log) to avoid collision with Serilog.Log, which is
 *              a static class used throughout the application. ScrumFlix.Domain is
 *              globally imported in GlobalUsings.cs, so a type named Log would shadow
 *              or conflict with Serilog.Log at every call site.
 *
 *              IMPORTANT — Schema source of truth:
 *              This table is owned and created by Serilog.Sinks.MySQL. The schema
 *              below reflects exactly what the sink created. Do NOT alter the table
 *              manually or via EF migrations — the sink will break.
 *
 *              Actual schema (as of 2026-05-22, AUTO_INCREMENT=58 confirming active writes):
 *                id          int PK AUTO_INCREMENT  (lowercase — sink convention)
 *                Timestamp   varchar(100)           (string, not datetime — sink formats it)
 *                Level       varchar(15)
 *                Template    text                   (message template before substitution)
 *                Message     text                   (fully rendered message)
 *                Exception   text
 *                Properties  text                   (JSON blob of structured properties)
 *                _ts         timestamp              (auto-set by MySQL on insert)
 *
 *              READ-ONLY from application code. Never INSERT/UPDATE/DELETE via EF.
 */

namespace ScrumFlix.Domain;

/// <summary>
/// A Serilog structured log event written by the MySQL sink.
/// Maps to the Logs table created and owned by Serilog.Sinks.MySQL.
/// READ-ONLY — never write to this entity via EF Core.
/// Named AppLog to avoid collision with the Serilog.Log static class.
/// </summary>
[Table("Logs")]
public class AppLog
{
    /// <summary>Primary key — auto-increment (lowercase 'id' per sink convention).</summary>
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// Timestamp of the log event as a formatted string (e.g. "2026-05-22 17:46:00").
    /// Stored as varchar(100) by the sink — not a DateTime column.
    /// </summary>
    [MaxLength(100)]
    [Column("Timestamp")]
    [Display(Name = "Timestamp")]
    public string? Timestamp { get; set; }

    /// <summary>
    /// Serilog log level: Verbose, Debug, Information, Warning, Error, Fatal.
    /// </summary>
    [MaxLength(15)]
    [Column("Level")]
    [Display(Name = "Level")]
    public string? Level { get; set; }

    /// <summary>
    /// The raw Serilog message template before property substitution
    /// (e.g., "User {UserId} logged in from {IpAddress}").
    /// </summary>
    [Column("Template")]
    [Display(Name = "Template")]
    public string? Template { get; set; }

    /// <summary>The fully rendered log message with all property values substituted in.</summary>
    [Column("Message")]
    [Display(Name = "Message")]
    public string? Message { get; set; }

    /// <summary>
    /// Full exception string including type, message, and stack trace.
    /// Null for log events not triggered by an exception.
    /// </summary>
    [Column("Exception")]
    [Display(Name = "Exception")]
    public string? Exception { get; set; }

    /// <summary>
    /// JSON blob of all structured properties attached to the log event
    /// (e.g., RequestPath, UserId, ActionName, MachineName, ThreadId).
    /// </summary>
    [Column("Properties")]
    [Display(Name = "Properties")]
    public string? Properties { get; set; }

    /// <summary>
    /// MySQL server-side insert timestamp. Auto-set by the database on every insert.
    /// Use this for reliable chronological sorting rather than Timestamp (which is a string).
    /// </summary>
    [Column("_ts")]
    [Display(Name = "Logged At")]
    public DateTime? Ts { get; set; }
}