using System.Text;
using Microsoft.Data.Sqlite;
using Pry.Core.Backup;
using Pry.Core.Memory;
using Pry.Core.Models;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// HDS-001 D/E 条（恢复到隔离目录并核对；未来版本包拒收；恢复中断可重试；v4 §9.3 / NR-24）。
/// 方向感来自"换架构不能从唯一一份用户数据直接开刀"：恢复的落点永远是**另一个目录**，
/// 用户迁移后的新数据不许被旧快照就地覆盖——这条不是提示，是会用红的判据。
/// </summary>
public sealed class DataRestoreServiceTests : IAsyncLifetime
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"pry-restore-{Guid.NewGuid():N}");
    private string _dataDirectory = null!;
    private string _backupsDirectory = null!;
    private string _isolatedTarget = null!;
    private string _databasePath = null!;

    public ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        _dataDirectory = Path.Combine(_root, "PryData");
        _backupsDirectory = Path.Combine(_root, "backups");
        _isolatedTarget = Path.Combine(_root, "IsolatedRestore");
        _databasePath = Path.Combine(_dataDirectory, "memory.db");
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        SqliteTestPools.Clear(_databasePath);
        SqliteTestPools.Clear(Path.Combine(_isolatedTarget, "memory.db"));
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        return ValueTask.CompletedTask;
    }

    /// <summary>备份时刻的应用状态：2 会话 3 消息、1 条活跃记忆 + 1 条孤立态记忆 + 墓碑与删除水位、
    /// 角色卡/设置/受管媒体齐活。这是 NR-24 核对清单的"应有之物"。</summary>
    private async Task<MemoryDatabase> SeedAsync()
    {
        var database = new MemoryDatabase(_databasePath);
        await database.InitializeAsync(Token);
        await database.EnsureConversationAsync("room-a", "aria", Token);
        await database.EnsureConversationAsync("room-b", "aria", Token);
        await database.AddMessageAsync("room-a", ChatRole.User, "你好普莱", null, Token);
        await database.AddMessageAsync("room-a", ChatRole.Assistant, "你好呀，今天也在一起", null, Token);
        await database.AddMessageAsync("room-b", ChatRole.User, "第二个会话的消息", null, Token);
        await using (var connection = new SqliteConnection($"Data Source={_databasePath};Pooling=False"))
        {
            await connection.OpenAsync(Token);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO memory_scopes(scope_key,character_id,epoch,forget_before) VALUES('character:aria','aria',4,6);
                INSERT INTO memories(character_id,kind,summary,tags,importance,source_message_id,created_at,
                  scope_key,status,fingerprint)
                VALUES('aria','fact','她喜欢雨天','',0.8,1,'2026-10-21T00:00:00+00:00','character:aria','active','A1');
                INSERT INTO memories(character_id,kind,summary,tags,importance,source_message_id,created_at,
                  scope_key,status,fingerprint)
                VALUES('aria','fact','证据已脱链的旧记忆','',0.3,NULL,'2026-10-21T00:00:00+00:00','character:aria','orphaned','B2');
                INSERT INTO memory_tombstones(memory_id,scope_key,fingerprint) VALUES(12345,'character:aria','T1');
                """;
            await command.ExecuteNonQueryAsync(Token);
        }
        File.WriteAllText(Path.Combine(_dataDirectory, "preferences.json"),
            """{"activeModelId":"model-1","selectedCharacterId":"aria"}""");
        var characters = Path.Combine(_dataDirectory, "characters");
        Directory.CreateDirectory(characters);
        File.WriteAllText(Path.Combine(characters, "aria.json"), """{"id":"aria","name":"普莱"}""");
        var media = Path.Combine(_dataDirectory, "media");
        Directory.CreateDirectory(media);
        File.WriteAllText(Path.Combine(media, "cover.png"), "cover bytes");
        File.WriteAllText(Path.Combine(media, "cover.json"), """{"kind":"image"}""");
        return database;
    }

    private async Task<BackupResult> BackupAsync() =>
        await new DataBackupService().CreateBackupAsync(_dataDirectory, _backupsDirectory, Token);

    [Fact]
    public async Task Restore_lands_in_an_isolated_directory_and_the_checklist_passes_item_by_item()
    {
        // NR-24：角色/设置/历史/记忆/媒体一致 = 核对清单逐格给实测值，且每一格独立判。
        await SeedAsync();
        var backup = await BackupAsync();
        var expected = backup.Manifest.Database!.Fingerprint;

        var result = await new DataRestoreService().RestoreAsync(backup.BundlePath, _isolatedTarget, Token);

        Assert.True(result.Verification.AllPassed,
            string.Join("；", result.Verification.Failures.Select(f => $"{f.Name}: 期望 {f.Expected}，实测 {f.Actual}")));
        Assert.Equal("3", result.Verification.Item("行数:messages").Actual);
        Assert.Equal(expected.RowCounts["messages"].ToString(), result.Verification.Item("行数:messages").Expected);
        Assert.Equal(expected.RowCounts["conversations"].ToString(), result.Verification.Item("行数:conversations").Expected);
        // 摘要类核对项在清单里是截断显示（前 12 位），判据是该项自身通过且显示的前缀对得上。
        AssertPassedWithDigestPrefix(result.Verification.Item("稳定ID:conversations.id"),
            expected.StableIdDigests["conversations.id"]);
        Assert.Equal("1", result.Verification.Item("墓碑数量").Actual);
        AssertPassedWithDigestPrefix(result.Verification.Item("删除水位(epoch/forget_before)"),
            expected.ScopeWatermarkDigest);
        AssertPassedWithDigestPrefix(result.Verification.Item("索引集合"), expected.IndexDigest);
        Assert.Equal("0", result.Verification.Item("外键检查").Actual);
        Assert.Equal("1", result.Verification.Item("孤立态:memories.orphaned").Actual);
        Assert.Equal("ok", result.Verification.Item("完整性:integrity_check").Actual);
        Assert.True(result.Verification.Item("用户偏好可解析").Passed);
        Assert.True(File.Exists(Path.Combine(_isolatedTarget, "memory.db")));
        Assert.True(File.Exists(Path.Combine(_isolatedTarget, "preferences.json")));
        Assert.True(File.Exists(Path.Combine(_isolatedTarget, "characters", "aria.json")));
        Assert.True(File.Exists(Path.Combine(_isolatedTarget, "media", "cover.png")));
        // 恢复不许产生侧文件残留（快照自洽）；核对过的每一格不许是零格空谈。
        Assert.False(File.Exists(Path.Combine(_isolatedTarget, "memory.db-wal")));
        Assert.True(result.Verification.Items.Count >= 20);
    }

    [Fact]
    public async Task Restored_snapshot_reflects_the_backup_moment_not_where_the_live_data_has_moved_to()
    {
        // "回退目标必须是另一个目录、不许覆盖用户迁移后的新数据"的可执行版本：
        // 备份之后实时目录继续长；恢复出来的目录停在备份时刻；实时目录分毫未动。
        var database = await SeedAsync();
        var backup = await BackupAsync();
        await database.AddMessageAsync("room-a", ChatRole.User, "迁移之后新增的消息", null, Token);
        var liveShaBefore = DatabaseSha256(_databasePath);

        var result = await new DataRestoreService().RestoreAsync(backup.BundlePath, _isolatedTarget, Token);

        var restored = await SqliteBackupDiagnostics.CaptureFingerprintAsync(
            Path.Combine(_isolatedTarget, "memory.db"), Token);
        var live = await SqliteBackupDiagnostics.CaptureFingerprintAsync(_databasePath, Token);
        Assert.Equal(3L, restored.RowCounts["messages"]);
        Assert.Equal(4L, live.RowCounts["messages"]);
        Assert.Equal(backup.Manifest.Database!.Fingerprint.RowCounts["messages"].ToString(),
            result.Verification.Item("行数:messages").Actual);
        Assert.Equal(liveShaBefore, DatabaseSha256(_databasePath)); // 隔离恢复全程没碰实时库
    }

    [Fact]
    public async Task The_restored_database_serves_the_application_from_its_isolated_directory()
    {
        // 内存丢失形状的端到端：新进程（这里即新实例）只凭隔离目录就能把应用状态读回来。
        await SeedAsync();
        var backup = await BackupAsync();
        await new DataRestoreService().RestoreAsync(backup.BundlePath, _isolatedTarget, Token);

        var restoredDatabase = new MemoryDatabase(Path.Combine(_isolatedTarget, "memory.db"));
        await restoredDatabase.InitializeAsync(Token);
        var room = await restoredDatabase.GetConversationAsync("room-a", Token);
        Assert.NotNull(room);
        Assert.Equal(MemoryDatabase.SchemaVersion, await restoredDatabase.GetSchemaVersionAsync(Token));
        SqliteTestPools.Clear(Path.Combine(_isolatedTarget, "memory.db"));
    }

    [Fact]
    public async Task Restore_refuses_a_target_that_already_holds_anything()
    {
        await SeedAsync();
        var backup = await BackupAsync();
        Directory.CreateDirectory(_isolatedTarget);
        File.WriteAllText(Path.Combine(_isolatedTarget, "memory.db"), "用户迁移后的新数据，一个字都不许被覆盖");

        var error = await Assert.ThrowsAsync<RestoreTargetRejectedException>(
            () => new DataRestoreService().RestoreAsync(backup.BundlePath, _isolatedTarget, Token));
        Assert.Contains("另一个隔离目录", error.Message, StringComparison.Ordinal);
        Assert.Equal("用户迁移后的新数据，一个字都不许被覆盖",
            await File.ReadAllTextAsync(Path.Combine(_isolatedTarget, "memory.db"), Token));
    }

    [Fact]
    public async Task Restore_refuses_paths_that_swallow_or_land_inside_the_bundle()
    {
        await SeedAsync();
        var backup = await BackupAsync();
        await Assert.ThrowsAsync<RestoreTargetRejectedException>(() =>
            new DataRestoreService().RestoreAsync(backup.BundlePath, backup.BundlePath, Token));
        await Assert.ThrowsAsync<RestoreTargetRejectedException>(() =>
            new DataRestoreService().RestoreAsync(backup.BundlePath,
                Path.Combine(backup.BundlePath, "restored"), Token));
        await Assert.ThrowsAsync<RestoreTargetRejectedException>(() =>
            new DataRestoreService().RestoreAsync(backup.BundlePath, _root, Token)); // 目标把包吞进去
    }

    [Fact]
    public async Task A_bundle_from_a_future_schema_version_is_refused_before_anything_is_written()
    {
        // E 条的恢复面：备份包声明的产出二进制版本高于当前已知版本 → 拒收，目标目录不出现。
        await SeedAsync();
        var backup = await BackupAsync();
        var manifest = await BackupBundleManifest.LoadAsync(backup.BundlePath, Token);
        await (manifest with { CreatedByBinarySchemaVersion = MemoryDatabase.SchemaVersion + 7 })
            .WriteAsync(backup.BundlePath, Token);

        var error = await Assert.ThrowsAsync<SchemaVersionTooNewException>(
            () => new DataRestoreService().RestoreAsync(backup.BundlePath, _isolatedTarget, Token));
        Assert.Equal(MemoryDatabase.SchemaVersion, error.BinarySchemaVersion);
        Assert.False(Directory.Exists(_isolatedTarget));
    }

    [Fact]
    public async Task An_interrupted_restore_never_publishes_the_final_directory_and_retries_clean()
    {
        // 恢复中断形状：落地通道在第 4 个文件被掐断。判据是"最终目标从未出现过"——
        // 半截恢复只能停在上锁的临时目录里，重试是同目标干净重来，不是接着拼。
        await SeedAsync();
        var backup = await BackupAsync();

        await Assert.ThrowsAsync<IOException>(() =>
            new DataRestoreService(copier: new FailOnNthFileCopier(4))
                .RestoreAsync(backup.BundlePath, _isolatedTarget, Token));
        Assert.False(Directory.Exists(_isolatedTarget));
        Assert.Empty(Directory.EnumerateDirectories(_root, "*.restoring-*"));

        var result = await new DataRestoreService().RestoreAsync(backup.BundlePath, _isolatedTarget, Token);
        Assert.True(result.Verification.AllPassed);
        Assert.True(Directory.Exists(_isolatedTarget));
    }

    [Fact]
    public async Task A_tampered_bundle_is_rejected_before_the_first_byte_reaches_the_target()
    {
        await SeedAsync();
        var backup = await BackupAsync();
        File.AppendAllText(Path.Combine(backup.BundlePath, "files", "characters", "aria.json"), "?");

        var error = await Assert.ThrowsAsync<BackupIntegrityException>(
            () => new DataRestoreService().RestoreAsync(backup.BundlePath, _isolatedTarget, Token));
        Assert.Contains("aria.json", error.Message, StringComparison.Ordinal);
        Assert.False(Directory.Exists(_isolatedTarget));
    }

    [Fact]
    public async Task Manifest_paths_trying_to_escape_the_target_are_rejected()
    {
        // 恶意/损坏清单的路径穿越样本：恢复落点必须被钉死在目标目录内。
        await SeedAsync();
        var backup = await BackupAsync();
        var manifest = await BackupBundleManifest.LoadAsync(backup.BundlePath, Token);
        var poisoned = manifest with
        {
            Files = manifest.Files.Select(entry => entry with { RelativePath = "../evil/" + entry.RelativePath }).ToList()
        };
        await poisoned.WriteAsync(backup.BundlePath, Token);

        await Assert.ThrowsAsync<BackupIntegrityException>(
            () => new DataRestoreService().RestoreAsync(backup.BundlePath, _isolatedTarget, Token));
        Assert.False(Directory.Exists(_isolatedTarget));
        Assert.False(File.Exists(Path.Combine(_root, "evil", "characters", "aria.json")));
    }

    private static void AssertPassedWithDigestPrefix(RestoreCheckItem item, string fullExpectedDigest)
    {
        Assert.True(item.Passed, $"{item.Name} 未通过：期望 {item.Expected}，实测 {item.Actual}");
        Assert.Equal(item.Expected, item.Actual);
        Assert.StartsWith(fullExpectedDigest[..12], item.Actual, StringComparison.Ordinal);
    }

    private static string DatabaseSha256(string path)
    {
        // 实时库上仍有连接句柄：普通 ReadAllBytes 会撞 Windows 共享冲突，测试侧只旁观故放开共享读。
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(buffer.ToArray())).ToLowerInvariant();
    }

    private sealed class FailOnNthFileCopier(int failOnCall) : IBackupFileCopier
    {
        private int _calls;

        public async Task CopyAsync(string sourcePath, string targetPath, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _calls) == failOnCall)
                throw new IOException("恢复中途断电/进程被杀（用例注入）");
            await new DiskBackupFileCopier().CopyAsync(sourcePath, targetPath, cancellationToken);
        }

        public Task<(string Sha256, long LengthBytes)> HashFileAsync(string path, CancellationToken cancellationToken) =>
            new DiskBackupFileCopier().HashFileAsync(path, cancellationToken);
    }
}
