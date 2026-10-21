using Microsoft.Data.Sqlite;
using Pry.Core.Backup;
using Pry.Core.Memory;
using Pry.Core.Models;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// HDS-001 F 条（迁移登记 / 事务边界 / 失败恢复 / 幂等，v4 §9.3、§9.1）：
/// 初始化从"读旧值→建表→写新值"的一条直线改为登记清单执行。本刀只搭治理，不定义任何新结构版本、
/// 不预占版本号——`no_new_schema_version_pinned` 一条把这句话钉成会红的断言。
/// 中断可恢复（NR-23）分别在外层（真库上一步炸掉）与机制层（迁移器自定义清单）各证一次。
/// </summary>
public sealed class MemorySchemaMigrationRegistryTests : IAsyncLifetime
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"pry-schema-migration-{Guid.NewGuid():N}");
    private string _databasePath = null!;

    public ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        _databasePath = Path.Combine(_root, "memory.db");
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        SqliteTestPools.Clear(_databasePath);
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        return ValueTask.CompletedTask;
    }

    private static readonly string ProbeConnection = "Data Source=";

    private async Task<SqliteConnection> RawOpenAsync()
    {
        var connection = new SqliteConnection($"{ProbeConnection}{_databasePath};Pooling=False");
        await connection.OpenAsync(Token);
        return connection;
    }

    private async Task<int> RegisteredVersionAsync()
    {
        await using var connection = await RawOpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(version), 0) FROM schema_info;";
        try { return Convert.ToInt32(await command.ExecuteScalarAsync(Token)); }
        catch (SqliteException) { return 0; }
    }

    [Fact]
    public void Registry_is_named_unique_ordered_and_never_pins_a_future_version()
    {
        var steps = new MemoryDatabase(_databasePath).BuildMigrationSteps();
        Assert.NotEmpty(steps);
        Assert.Equal(steps.Count, steps.Select(step => step.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(steps, step => Assert.False(string.IsNullOrWhiteSpace(step.Name)));
        // 反证锚点：任何一步冒领高于当前二进制的版本，这条必须红（v4 §9.1 不预占 schema 11）。
        Assert.All(steps, step => Assert.True(step.TargetVersion <= MemoryDatabase.SchemaVersion,
            $"步骤 {step.Id} 预占了 {step.TargetVersion} > {MemoryDatabase.SchemaVersion}"));
        Assert.Equal(SchemaMigrationMode.VersionAlign, steps[^1].Mode);
        Assert.Single(steps, step => step.Mode == SchemaMigrationMode.VersionAlign);
        Assert.Contains(steps, step => step.Mode == SchemaMigrationMode.SelfHealingBaseline);
        Assert.Contains(steps, step =>
            step.Mode == SchemaMigrationMode.VersionGatedUpgrade && step.TargetVersion == 2);
        // 迁移器对不合规清单（空、重复号、冒领版本、无对齐收尾）在执行前就炸。
        Assert.Throws<InvalidOperationException>(() =>
            MemorySchemaMigrator.ValidateRegistry([steps[0]]));
        Assert.Throws<InvalidOperationException>(() =>
            MemorySchemaMigrator.ValidateRegistry([steps[0], steps[0]]));
        Assert.Throws<InvalidOperationException>(() =>
            MemorySchemaMigrator.ValidateRegistry([steps[^1], new SchemaMigrationStep(
                MemoryDatabase.SchemaVersion + 1, "greedy", "冒领", SchemaMigrationMode.VersionAlign,
                (_, _) => Task.CompletedTask)]));
    }

    [Fact]
    public async Task Repeated_initialization_is_idempotent_and_leaves_the_fingerprint_identical()
    {
        var database = new MemoryDatabase(_databasePath);
        await database.InitializeAsync(Token);
        await database.AddMessageAsync("room-a", ChatRole.User, "第一条", null, Token);
        var first = await SqliteBackupDiagnostics.CaptureFingerprintAsync(_databasePath, Token);
        await database.InitializeAsync(Token);
        await database.InitializeAsync(Token);
        var later = await SqliteBackupDiagnostics.CaptureFingerprintAsync(_databasePath, Token);

        Assert.NotNull(first);
        Assert.Equal(StableJson(first), StableJson(later));
        Assert.Equal(MemoryDatabase.SchemaVersion, await RegisteredVersionAsync());
        // 三次初始化后数据仍然只有一条：幂等不是"跑过不报错"，是"跑过不多东西"。
        await using var connection = await RawOpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM messages;";
        Assert.Equal(1L, Convert.ToInt64(await command.ExecuteScalarAsync(Token)));
    }

    [Fact]
    public async Task A_step_that_blows_up_halfway_leaves_no_trace_and_a_rerun_completes_it()
    {
        // NR-23 的机制层证据：失败步骤整事务回滚、登记版本不推进、此前已提交的步骤不受影响；
        // 清单幂等，所以修好成因后的重跑就是恢复路径——不需要"从中间接上"的那种玄学。
        await new MemoryDatabase(_databasePath).InitializeAsync(Token);
        await using var connection = await RawOpenAsync();
        var versionBefore = await RegisteredVersionAsync();

        var boom = new SchemaMigrationStep(7, "test.faulty", "故意炸掉的步骤", SchemaMigrationMode.IdempotentRepair,
            static async (context, token) =>
            {
                await using var insert = context.Connection.CreateCommand();
                insert.Transaction = context.Transaction;
                insert.CommandText = "CREATE TABLE IF NOT EXISTS fault_probe(marker INTEGER); INSERT INTO fault_probe VALUES(1);";
                await insert.ExecuteNonQueryAsync(token);
                throw new IOException("模拟：步骤中途断电/磁盘错误");
            });
        var faultedList = new SchemaMigrationStep[] { boom, new MemoryDatabase(_databasePath).BuildMigrationSteps()[^1] };
        var error = await Assert.ThrowsAsync<MemoryMigrationException>(
            () => MemorySchemaMigrator.RunAsync(connection, versionBefore, faultedList, Token));
        Assert.Equal("test.faulty", error.StepId);
        Assert.IsAssignableFrom<IOException>(error.InnerException);

        await using (var probe = connection.CreateCommand())
        {
            probe.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE name='fault_probe';";
            Assert.Equal(0L, Convert.ToInt64(await probe.ExecuteScalarAsync(Token))); // 建表随步骤一起回滚
        }
        Assert.Equal(versionBefore, await RegisteredVersionAsync()); // 版本没被半推进

        var ok = new SchemaMigrationStep(7, "test.faulty", "修好的同一步骤", SchemaMigrationMode.IdempotentRepair,
            static async (context, token) =>
            {
                await using var insert = context.Connection.CreateCommand();
                insert.Transaction = context.Transaction;
                insert.CommandText = "CREATE TABLE IF NOT EXISTS fault_probe(marker INTEGER);";
                await insert.ExecuteNonQueryAsync(token);
            });
        await MemorySchemaMigrator.RunAsync(connection, versionBefore,
            [ok, new MemoryDatabase(_databasePath).BuildMigrationSteps()[^1]], Token);
        await using (var probe = connection.CreateCommand())
        {
            probe.CommandText = "SELECT COUNT(*) FROM fault_probe;";
            Assert.Equal(0L, Convert.ToInt64(await probe.ExecuteScalarAsync(Token)));
        }
        Assert.Equal(MemoryDatabase.SchemaVersion, await RegisteredVersionAsync());
    }

    [Fact]
    public async Task Interrupted_real_initialization_rolls_the_whole_step_back_and_heals_on_next_boot()
    {
        // NR-23 的真库证据：用一个真实会炸的形状打断"baseline"步骤——同名视图让 ALTER 失败。
        // 期望不是"报错"四个字，而是：步骤内所有已写语句整体消失（连 schema_info 都不留），
        // 拆掉病灶后重新初始化得到与正常建库完全一致的指纹。
        await using (var connection = await RawOpenAsync())
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE VIEW messages AS SELECT 1 AS a;";
            await command.ExecuteNonQueryAsync(Token);
        }

        var error = await Assert.ThrowsAsync<MemoryMigrationException>(
            () => new MemoryDatabase(_databasePath).InitializeAsync(Token));
        Assert.Equal("baseline.schema", error.StepId);

        await using (var connection = await RawOpenAsync())
        {
            await using var probe = connection.CreateCommand();
            probe.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table';";
            Assert.Equal(0L, Convert.ToInt64(await probe.ExecuteScalarAsync(Token))); // 整步回滚：一张表都没留下
        }

        await using (var connection = await RawOpenAsync())
        {
            await using var drop = connection.CreateCommand();
            drop.CommandText = "DROP VIEW messages;";
            await drop.ExecuteNonQueryAsync(Token);
        }
        var database = new MemoryDatabase(_databasePath);
        await database.InitializeAsync(Token);
        await database.AddMessageAsync("room-a", ChatRole.User, "愈合后的第一条", null, Token);
        var fingerprint = await SqliteBackupDiagnostics.CaptureFingerprintAsync(_databasePath, Token);
        Assert.Equal(MemoryDatabase.SchemaVersion, fingerprint.SchemaVersion);
        Assert.True(fingerprint.RowCounts["messages"] >= 1);
        Assert.Equal(0, fingerprint.ForeignKeyViolations);
    }

    [Fact]
    public async Task The_version_gated_step_runs_only_for_databases_below_its_gate()
    {
        // 闸门档语义：登记版本 ≥2 的库不再重放 FTS 重建（历史行为原样保留），
        // 低于闸门的（含全新库）会执行并把版本推进到 2——两步都发生在同一个事务边界里。
        var database = new MemoryDatabase(_databasePath);
        await database.InitializeAsync(Token);
        var rebuiltAt = await RegisteredVersionAsync();
        Assert.Equal(MemoryDatabase.SchemaVersion, rebuiltAt); // 新库走完整清单：闸门步骤执行过，随后对齐到当前版本

        await using (var connection = await RawOpenAsync())
        {
            await using var probe = connection.CreateCommand();
            probe.CommandText = "SELECT COUNT(*) FROM memory_fts;";
            Assert.Equal(0L, Convert.ToInt64(await probe.ExecuteScalarAsync(Token))); // 空 memories 的重建结果仍是空
        }
    }

    [Fact]
    public async Task Cancellation_before_a_write_point_leaves_the_database_untouched()
    {
        // 内存丢失/取消形状：迁移器在每个步骤前检查取消；被取消的迁移不得留下半套结构，
        // 登记版本必须保持原值——下一次启动从完好状态重跑清单就是恢复路径（NR-23）。
        await using (var connection = await RawOpenAsync())
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE schema_info(version INTEGER NOT NULL); INSERT INTO schema_info VALUES(3);";
            await command.ExecuteNonQueryAsync(Token);
        }
        var sha = DatabaseSha256(_databasePath);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await using (var live = await RawOpenAsync())
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => MemorySchemaMigrator.RunAsync(
                live, 3, new MemoryDatabase(_databasePath).BuildMigrationSteps(), cancellation.Token));
        }
        Assert.Equal(sha, DatabaseSha256(_databasePath));
        Assert.Equal(3, await RegisteredVersionAsync());
    }

    private static string StableJson(DatabaseFingerprint fingerprint) =>
        System.Text.Json.JsonSerializer.Serialize(fingerprint, BackupBundleManifest.SerializerOptions);

    private static string DatabaseSha256(string path) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
}
