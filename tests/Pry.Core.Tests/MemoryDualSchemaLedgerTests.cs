using Microsoft.Data.Sqlite;
using Pry.Contracts;
using Pry.Core.Memory;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// DEF-MEMDUAL-01：同一份 SQLite 里并存着两套结构版本账——<c>schema_info</c>（平台登记账，
/// 写侧唯一编号 <see cref="MemoryDatabase.SchemaVersion"/>）与 <c>memory_schema</c>
/// （记忆组件账，合法区间 1..2，由 <see cref="SqliteMemoryRepository"/> 维护）。
/// 本文件钉的是"两账并存这件事本身 + 各道门拦下谁 + 两道拒绝都走可呈现链"：
/// 归属用异常类型区分（类型互斥），呈现面断 <see cref="IUserPresentableError"/> 的
/// Headline/Body/RetryAllowed——不许用异常消息的字符串匹配过门。
/// 两账合并、删表、给 memory_schema 分配新版本号都属于迁移决定（归 HDS-005），本文件不预支。
/// </summary>
public sealed class MemoryDualSchemaLedgerTests : IAsyncLifetime
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "pry-dual-schema-ledger-" + Guid.NewGuid().ToString("N"));
    private string Database => Path.Combine(_directory, "memory.db");
    private MemoryDatabase _db = null!;
    private CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_directory);
        _db = new MemoryDatabase(Database);
        await _db.InitializeAsync(Token);
    }

    public ValueTask DisposeAsync()
    {
        SqliteTestPools.Clear(Database);
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task The_two_schema_ledgers_coexist_and_each_high_version_refusal_names_its_own_gate()
    {
        // 两账并存的事实面：一次正常初始化后，同一库里两张账各自有行、各自可查，问的是两个问题。
        Assert.Equal(MemoryDatabase.SchemaVersion, (int)await Number("SELECT MAX(version) FROM schema_info"));
        Assert.Equal(2, (int)await Number("SELECT MAX(version) FROM memory_schema"));

        // 只把组件账抬过区间 ⇒ 拦下的是第二道门（类型即归属，与第一道门的类型互斥）。
        await Run("UPDATE memory_schema SET version=3");
        var component = await Assert.ThrowsAsync<MemoryComponentSchemaRejectedException>(
            () => new MemoryDatabase(Database).InitializeAsync(Token));
        Assert.NotEqual(typeof(SchemaVersionTooNewException), component.GetType());
        Assert.Equal(3, component.ComponentVersion);
        await Run("UPDATE memory_schema SET version=2");

        // 只把平台账抬高 ⇒ 拦下的是第一道门。版本编号读常量，不再落第二个字面量。
        await Run("UPDATE schema_info SET version=" + (MemoryDatabase.SchemaVersion + 1));
        var platform = await Assert.ThrowsAsync<SchemaVersionTooNewException>(
            () => new MemoryDatabase(Database).InitializeAsync(Token));
        Assert.NotEqual(typeof(MemoryComponentSchemaRejectedException), platform.GetType());
        Assert.Equal(MemoryDatabase.SchemaVersion, platform.BinarySchemaVersion);
        Assert.Equal(MemoryDatabase.SchemaVersion + 1, platform.DatabaseSchemaVersion);

        // 两账同时抬高 ⇒ 归属依然可判：平台账的门在任何写入之前，先于组件账拦下。
        await Run("UPDATE memory_schema SET version=3");
        var bothHigh = await Assert.ThrowsAsync<SchemaVersionTooNewException>(
            () => new MemoryDatabase(Database).InitializeAsync(Token));
        Assert.NotEqual(typeof(MemoryComponentSchemaRejectedException), bothHigh.GetType());
    }

    [Fact]
    public async Task Both_refusals_travel_the_presentable_chain_and_neither_invites_a_retry()
    {
        // 第二道账的拒绝形状（DEF-MEMDUAL-01 本体）：可呈现、禁重试。
        await Run("UPDATE memory_schema SET version=3");
        var component = await Assert.ThrowsAsync<MemoryComponentSchemaRejectedException>(
            () => new MemoryDatabase(Database).InitializeAsync(Token));
        var componentGuard = Assert.IsAssignableFrom<IUserPresentableError>(component);
        Assert.False(componentGuard.RetryAllowed);
        Assert.False(string.IsNullOrWhiteSpace(componentGuard.Headline));
        Assert.False(string.IsNullOrWhiteSpace(componentGuard.Body));
        // 拒绝不落账：组件账仍是被拒的那个版本，初始化没有把半套结构写进去。
        Assert.Equal(3, (int)await Number("SELECT MAX(version) FROM memory_schema"));
        await Run("UPDATE memory_schema SET version=2");

        // 第一道账同链（既有语义，这里只核对它也在呈现链上、同样禁重试）。
        await Run("UPDATE schema_info SET version=" + (MemoryDatabase.SchemaVersion + 1));
        var platform = await Assert.ThrowsAsync<SchemaVersionTooNewException>(
            () => new MemoryDatabase(Database).InitializeAsync(Token));
        var platformGuard = Assert.IsAssignableFrom<IUserPresentableError>(platform);
        Assert.False(platformGuard.RetryAllowed);
        Assert.False(string.IsNullOrWhiteSpace(platformGuard.Headline));
        Assert.False(string.IsNullOrWhiteSpace(platformGuard.Body));
    }

    private async Task Run(string sql)
    {
        await using var connection = new SqliteConnection($"Data Source={Database}");
        await connection.OpenAsync(Token);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(Token);
    }

    private async Task<long> Number(string sql)
    {
        await using var connection = new SqliteConnection($"Data Source={Database}");
        await connection.OpenAsync(Token);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync(Token) ?? 0L);
    }
}
