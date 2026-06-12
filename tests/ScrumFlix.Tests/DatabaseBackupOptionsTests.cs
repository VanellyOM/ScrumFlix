/*
 * File:    tests/ScrumFlix.Tests/DatabaseBackupOptionsTests.cs
 * Purpose: Unit tests for the Phase 4+ backup schema-upgrade contract:
 *            - DatabaseBackupOptions.From() preset → flag mapping
 *            - HasAnySection no-op detection
 *            - BackupResult schema-object counters and HasSchemaObjects
 *            - TotalRows ignores the -1 serialisation-error sentinel
 *
 * Pure unit tests — no database, no EF, no connection.
 */

using ScrumFlix.Services;
using Xunit;

namespace ScrumFlix.Tests;

public class DatabaseBackupOptionsTests
{
    [Fact]
    public void From_DataOnly_EnablesDataOnly()
    {
        var o = DatabaseBackupOptions.From(BackupMode.DataOnly);

        Assert.True(o.IncludeData);
        Assert.False(o.IncludeSchema);
        Assert.False(o.IncludeStoredProcedures);
        Assert.False(o.IncludeViews);
        Assert.False(o.IncludeTriggers);
        Assert.True(o.HasAnySection);
    }

    [Fact]
    public void From_SchemaOnly_EnablesAllDdlButNotData()
    {
        var o = DatabaseBackupOptions.From(BackupMode.SchemaOnly);

        Assert.True(o.IncludeSchema);
        Assert.False(o.IncludeData);
        Assert.True(o.IncludeStoredProcedures);
        Assert.True(o.IncludeViews);
        Assert.True(o.IncludeTriggers);
    }

    [Fact]
    public void From_Full_EnablesEverything()
    {
        var o = DatabaseBackupOptions.From(BackupMode.Full);

        Assert.True(o.IncludeSchema);
        Assert.True(o.IncludeData);
        Assert.True(o.IncludeStoredProcedures);
        Assert.True(o.IncludeViews);
        Assert.True(o.IncludeTriggers);
    }

    [Fact]
    public void From_IndividualTable_IsSchemaPlusDataOnly()
    {
        var o = DatabaseBackupOptions.From(BackupMode.IndividualTable, new[] { "Movies" });

        Assert.True(o.IncludeSchema);
        Assert.True(o.IncludeData);
        Assert.False(o.IncludeStoredProcedures);
        Assert.False(o.IncludeViews);
        Assert.False(o.IncludeTriggers);
        Assert.Equal(new[] { "Movies" }, o.SelectedTableKeys);
    }

    [Fact]
    public void From_PropagatesDropBeforeCreate()
    {
        Assert.False(DatabaseBackupOptions.From(BackupMode.Full, dropBeforeCreate: false).DropBeforeCreate);
        Assert.True(DatabaseBackupOptions.From(BackupMode.Full, dropBeforeCreate: true).DropBeforeCreate);
    }

    [Fact]
    public void HasAnySection_IsFalse_WhenNothingSelected()
    {
        var o = new DatabaseBackupOptions
        {
            IncludeSchema = false,
            IncludeData = false,
            IncludeStoredProcedures = false,
            IncludeViews = false,
            IncludeTriggers = false,
        };

        Assert.False(o.HasAnySection);
    }

    [Fact]
    public void HasAnySection_IsTrue_WithOnlyTriggers()
    {
        var o = new DatabaseBackupOptions
        {
            IncludeData = false,
            IncludeTriggers = true,
        };

        Assert.True(o.HasAnySection);
    }

    [Fact]
    public void BackupResult_SchemaObjectCounts_DefaultToZero()
    {
        var r = NewResult();

        Assert.Equal(0, r.SchemaTableCount);
        Assert.Equal(0, r.ProcedureCount);
        Assert.Equal(0, r.FunctionCount);
        Assert.Equal(0, r.ViewCount);
        Assert.Equal(0, r.TriggerCount);
        Assert.False(r.HasSchemaObjects);
        Assert.Empty(r.IncludedSections);
    }

    [Fact]
    public void BackupResult_HasSchemaObjects_TrueWhenAnyCountPositive()
    {
        var r = new BackupResult
        {
            ZipBytes = System.Array.Empty<byte>(),
            FileName = "x.zip",
            TakenAtUtc = System.DateTime.UtcNow,
            RowCounts = new System.Collections.Generic.Dictionary<string, int>(),
            TriggerCount = 2,
        };

        Assert.True(r.HasSchemaObjects);
    }

    [Fact]
    public void BackupResult_TotalRows_IgnoresErrorSentinel()
    {
        var r = new BackupResult
        {
            ZipBytes = System.Array.Empty<byte>(),
            FileName = "x.zip",
            TakenAtUtc = System.DateTime.UtcNow,
            RowCounts = new System.Collections.Generic.Dictionary<string, int>
            {
                ["Movies"] = 25,
                ["Showtime"] = 100,
                ["BrokenTable"] = -1,   // serialisation error sentinel
            },
        };

        // -1 must not subtract from the total.
        Assert.Equal(125, r.TotalRows);
    }

    private static BackupResult NewResult() => new()
    {
        ZipBytes = System.Array.Empty<byte>(),
        FileName = "scrumflix_backup_20260612_000000.zip",
        TakenAtUtc = System.DateTime.UtcNow,
        RowCounts = new System.Collections.Generic.Dictionary<string, int>(),
    };
}
