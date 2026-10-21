using Microsoft.Data.Sqlite;
using Pry.Contracts;
using Pry.Core.Memory;
using Pry.Core.Models;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// HDS-001 E 条（未知高版本拒写，v4 §9.3 / AUD-05 / NR-23）：数据库登记的结构版本高于当前二进制时，
/// 初始化必须在**写入任何一字节之前**停下——不是"告警后继续写"。这份 SQLite 是用户机器上的唯一一份，
/// 旧二进制按旧口径写新结构，坏掉的是新结构自己还没验证过的数据。
/// 每条判据都配了故意弄红的反证：门摘掉后"库被写过"的证据会留在断言里，不是靠消息文本判断拦截发生过。
/// </summary>
public sealed class SchemaVersionGuardTests : IAsyncLifetime
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"pry-schema-guard-{Guid.NewGuid():N}");
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

    /// <summary>造一个"结构版本登记为 future、但表形状与当前一致"的库：先正常建库，再把登记值抬高。</summary>
    private async Task SeedDatabaseRegisteredAtAsync(int registeredVersion)
    {
        var database = new MemoryDatabase(_databasePath);
        await database.InitializeAsync(Token);
        await database.AddMessageAsync("room-a", ChatRole.User, "旧结构时代的用户消息", null, Token);
        SqliteTestPools.Clear(_databasePath);
        await using (var connection = new SqliteConnection($"Data Source={_databasePath};Pooling=False"))
        {
            await connection.OpenAsync(Token);
            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE schema_info SET version=$v;";
            command.Parameters.AddWithValue("$v", registeredVersion);
            await command.ExecuteNonQueryAsync(Token);
        }
    }

    private static string DatabaseSha256(string path) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(ReadBytesShared(path))).ToLowerInvariant();

    /// <summary>活连接仍持有句柄时，File.ReadAllBytes 在 Windows 上会撞共享冲突；测试只旁观不写，放开共享读。</summary>
    private static byte[] ReadBytesShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    [Fact]
    public async Task Initialize_refuses_a_database_registered_above_the_binaries_schema_version()
    {
        await SeedDatabaseRegisteredAtAsync(MemoryDatabase.SchemaVersion + 1);
        var before = DatabaseSha256(_databasePath);

        var error = await Assert.ThrowsAsync<SchemaVersionTooNewException>(
            () => new MemoryDatabase(_databasePath).InitializeAsync(Token));

        // 主文件一个字节都不许变。侧文件不在这里判——这个库本来就是 WAL 口径，
        // 活连接在场时 -wal/-shm 的出现与否不构成"写过"的证据；那条判据由下面的非 WAL 种子负责。
        Assert.Equal(before, DatabaseSha256(_databasePath));
        Assert.Equal(MemoryDatabase.SchemaVersion, error.BinarySchemaVersion);
        Assert.Equal(MemoryDatabase.SchemaVersion + 1, error.DatabaseSchemaVersion);
        SqliteTestPools.Clear(_databasePath);
    }

    [Fact]
    public async Task The_rejection_sits_before_the_first_write_even_when_that_write_would_be_the_wal_pragma()
    {
        // 反证锚点的可执行形状：库不是 WAL 口径时，初始化的第一个写盘动作就是 PRAGMA journal_mode=WAL。
        // 门若摘掉，这里会看到主文件字节变化 + 侧文件被创建 + journal_mode 变 wal；三条现在都必须不动。
        await using (var build = new SqliteConnection($"Data Source={_databasePath};Pooling=False"))
        {
            await build.OpenAsync(Token);
            await using var command = build.CreateCommand();
            command.CommandText = """
                CREATE TABLE schema_info(version INTEGER NOT NULL);
                INSERT INTO schema_info(version) VALUES($v);
                """;
            command.Parameters.AddWithValue("$v", MemoryDatabase.SchemaVersion + 5);
            await command.ExecuteNonQueryAsync(Token);
        }
        SqliteTestPools.Clear(_databasePath);
        var before = DatabaseSha256(_databasePath);

        await Assert.ThrowsAsync<SchemaVersionTooNewException>(
            () => new MemoryDatabase(_databasePath).InitializeAsync(Token));

        Assert.Equal(before, DatabaseSha256(_databasePath));
        Assert.False(File.Exists(_databasePath + "-wal"));
        Assert.False(File.Exists(_databasePath + "-shm"));
        await using var probe = new SqliteConnection($"Data Source={_databasePath};Mode=ReadOnly;Pooling=False");
        await probe.OpenAsync(Token);
        await using var pragma = probe.CreateCommand();
        pragma.CommandText = "PRAGMA journal_mode;";
        Assert.NotEqual("wal", (await pragma.ExecuteScalarAsync(Token))?.ToString()?.ToLowerInvariant());
        SqliteTestPools.Clear(_databasePath);
    }

    [Fact]
    public async Task Refusal_carries_user_presentable_error_semantics_and_forbids_retry()
    {
        await SeedDatabaseRegisteredAtAsync(99);
        var error = await Assert.ThrowsAsync<SchemaVersionTooNewException>(
            () => new MemoryDatabase(_databasePath).InitializeAsync(Token));

        // 错误语义要能原样呈现给用户：标题说处境、正文给动作、禁行位必须为真——
        // "重启重试"对这个场景是伤害而不是恢复。
        Assert.IsAssignableFrom<IUserPresentableError>(error);
        var presentable = (IUserPresentableError)error;
        Assert.False(presentable.RetryAllowed);
        Assert.Contains("版本", presentable.Headline);
        Assert.Contains("99", presentable.Body);
        Assert.Contains("没有", presentable.Body + error.Message);
        Assert.Contains("备份", presentable.Body);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(MemoryDatabase.SchemaVersion)]
    public async Task The_current_and_every_older_registered_version_still_walks_the_migration_path(int registered)
    {
        await SeedDatabaseRegisteredAtAsync(registered);
        var database = new MemoryDatabase(_databasePath);
        await database.InitializeAsync(Token); // 不抛就是放行；抛了用例自然红。
        Assert.Equal(MemoryDatabase.SchemaVersion, await database.GetSchemaVersionAsync(Token));
        var messages = await database.GetConversationAsync("room-a", Token);
        Assert.NotNull(messages);
        SqliteTestPools.Clear(_databasePath);
    }

    [Fact]
    public async Task A_future_version_database_never_gets_even_the_repair_pass()
    {
        // 表都还没建的"裸登记"库（模拟未来版本改了表集合）：拒写门同样必须在建表之前拦下。
        await using (var connection = new SqliteConnection($"Data Source={_databasePath};Pooling=False"))
        {
            await connection.OpenAsync(Token);
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE schema_info(version INTEGER NOT NULL); INSERT INTO schema_info VALUES(42);";
            await command.ExecuteNonQueryAsync(Token);
        }
        var bare = DatabaseSha256(_databasePath);

        await Assert.ThrowsAsync<SchemaVersionTooNewException>(
            () => new MemoryDatabase(_databasePath).InitializeAsync(Token));

        Assert.Equal(bare, DatabaseSha256(_databasePath));
        await using (var probe = new SqliteConnection($"Data Source={_databasePath};Pooling=False"))
        {
            await probe.OpenAsync(Token);
            await using var command = probe.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name<>'schema_info';";
            Assert.Equal(0L, Convert.ToInt64(await command.ExecuteScalarAsync(Token)));
        }
        SqliteConnection.ClearPool(new SqliteConnection($"Data Source={_databasePath};Pooling=False"));
    }
}
