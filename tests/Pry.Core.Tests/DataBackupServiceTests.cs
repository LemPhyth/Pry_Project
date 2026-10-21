using System.Text;
using Microsoft.Data.Sqlite;
using Pry.Core.Backup;
using Pry.Core.Memory;
using Pry.Core.Models;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// HDS-001 A/B/C 条（一致性备份、散列与完整性、内容边界；v4 §9.3 / AUD-05 / NR-23 的前半段）。
/// 全部落在合成数据目录里：绝不触碰 <c>%LOCALAPPDATA%/PryCompanion</c>——数据目录由用例自建，
/// 收尾自清（只清自己建的树）。故障矩阵（磁盘满、响应丢失、WAL 侧文件缺失/不一致）各自独立成条。
/// </summary>
public sealed class DataBackupServiceTests : IAsyncLifetime
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"pry-backup-{Guid.NewGuid():N}");
    private string _dataDirectory = null!;
    private string _backupsDirectory = null!;
    private string _databasePath = null!;

    public ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        _dataDirectory = Path.Combine(_root, "PryData");
        _backupsDirectory = Path.Combine(_root, "backups");
        _databasePath = Path.Combine(_dataDirectory, "memory.db");
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        SqliteTestPools.Clear(_databasePath);
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        return ValueTask.CompletedTask;
    }

    /// <summary>造一份"有角色、有设置、有历史、有记忆、有受管媒体"的合成数据目录，外加全部禁止入包的东西。</summary>
    private async Task<MemoryDatabase> SeedSyntheticDataDirectoryAsync()
    {
        var database = new MemoryDatabase(_databasePath);
        await database.InitializeAsync(Token);
        await database.EnsureConversationAsync("room-a", "aria", Token);
        await database.AddMessageAsync("room-a", ChatRole.User, "你好普莱", null, Token);
        await database.AddMessageAsync("room-a", ChatRole.Assistant, "你好呀", null, Token);

        await using (var connection = new SqliteConnection($"Data Source={_databasePath};Pooling=False"))
        {
            await connection.OpenAsync(Token);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO memory_scopes(scope_key,character_id,epoch,forget_before) VALUES('character:aria','aria',3,5);
                INSERT INTO memories(character_id,kind,summary,tags,importance,source_message_id,created_at,
                  scope_key,status,fingerprint)
                VALUES('aria','fact','她喜欢深夜聊天','',0.7,1,'2026-10-21T00:00:00+00:00','character:aria','active','AA');
                INSERT INTO memories(character_id,kind,summary,tags,importance,source_message_id,created_at,
                  scope_key,status,fingerprint)
                VALUES('aria','fact','这条的证据已经脱链','',0.4,NULL,'2026-10-21T00:00:00+00:00','character:aria','orphaned','BB');
                INSERT INTO memory_tombstones(memory_id,scope_key,fingerprint) VALUES(999999,'character:aria','CC');
                """;
            await command.ExecuteNonQueryAsync(Token);
        }

        File.WriteAllText(Path.Combine(_dataDirectory, "preferences.json"), """
            {"activeModelId":"model-1","enableThinking":true,"selectedCharacterId":"aria"}
            """);
        File.WriteAllText(Path.Combine(_dataDirectory, "character.json"), """{"current":"aria"}""");
        WriteFileUnder("characters", "aria.json", """{"id":"aria","name":"普莱"}""");
        WriteFileUnder("interaction-profiles", "aria.json", """{"tone":"温柔"}""");
        WriteFileUnder("media", "0ba97852dcaa46fbb7f916c3ecb11431.png", "PNG-ish bytes");
        WriteFileUnder("media", "0ba97852dcaa46fbb7f916c3ecb11431.json", """{"kind":"image"}""");
        WriteFileUnder("stickers", "manifest.json", "[]");
        WriteFileUnder("character-media", "aria-avatar.png", "avatar bytes");

        // 禁止入包的东西——每一样都带同一个可识别的假秘密串，出包即红。
        // 哨兵刻意不写成 `sk-…` 真键形状：仓库扫描（verify-repository 的密钥规则）禁止任何受跟踪文件
        // 携带该形状，而排除判定依据的是文件名／扩展名（runtime.json、*.log、*.key），与内容形状无关。
        File.WriteAllText(Path.Combine(_dataDirectory, "runtime.json"),
            """{"port":5000,"token":"FAKE-SESSION-TOKEN-DO-NOT-BACKUP"}""");
        Directory.CreateDirectory(Path.Combine(_dataDirectory, "logs"));
        File.WriteAllText(Path.Combine(_dataDirectory, "logs", "pry-backend.log"),
            "2026-10-21 ERROR key=FAKE-API-KEY-DO-NOT-BACKUP");
        Directory.CreateDirectory(Path.Combine(_dataDirectory, "attachments"));
        File.WriteAllText(Path.Combine(_dataDirectory, "attachments", "user-import.txt"),
            "用户导入内容 FAKE-API-KEY-DO-NOT-BACKUP");
        Directory.CreateDirectory(Path.Combine(_dataDirectory, "temp"));
        File.WriteAllText(Path.Combine(_dataDirectory, "temp", "scratch.tmp"), "FAKE-TEMP");
        // 纵深防御样本：假密钥伪装成受管媒体目录里的文件——白名单目录不等于免检。
        WriteFileUnder("media", "runtime.json", """{"token":"FAKE-INSIDE-MEDIA-DO-NOT-BACKUP"}""");
        WriteFileUnder("media", "server.key", "FAKE-KEY-INSIDE-MEDIA-DO-NOT-BACKUP");
        return database;
    }

    private void WriteFileUnder(string directory, string name, string content)
    {
        var dir = Path.Combine(_dataDirectory, directory);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, name), content);
    }

    [Fact]
    public async Task Online_snapshot_carries_committed_rows_that_still_rest_in_the_write_ahead_log()
    {
        // v4 §9.3 点名禁止的形状在这里给出反证：同一份库，"只复制主 .db"会丢行，在线快照不丢。
        await SeedSyntheticDataDirectoryAsync();
        var marker = $"WAL-ONLY-COMMIT-{Guid.NewGuid():N}";
        await using var holder = new SqliteConnection($"Data Source={_databasePath};Pooling=False");
        await holder.OpenAsync(Token); // 留一条连接：最后关闭时的自动 checkpoint 不许提前发生。
        await using (var writer = new SqliteConnection($"Data Source={_databasePath};Pooling=False"))
        {
            await writer.OpenAsync(Token);
            await using var command = writer.CreateCommand();
            command.CommandText = """
                INSERT INTO messages(conversation_id,role,content,source,created_at)
                VALUES('room-a','user',$marker,'user_input','2026-10-21T01:00:00+00:00');
                """;
            command.Parameters.AddWithValue("$marker", marker);
            await command.ExecuteNonQueryAsync(Token);
        }
        // holder 连接还开着：普通 ReadAllBytes 会撞 Windows 共享冲突；这里只旁观，放开共享读。
        byte[] mainFileBytes;
        {
            using var probe = new FileStream(_databasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var peek = new MemoryStream();
            probe.CopyTo(peek);
            mainFileBytes = peek.ToArray();
        }
        var walPath = _databasePath + "-wal";
        Assert.True(File.Exists(walPath) && new FileInfo(walPath).Length > 0);
        Assert.DoesNotContain(marker, Encoding.UTF8.GetString(mainFileBytes)); // 主文件此刻确实没有这行——照搬文件必缺。

        var result = await new DataBackupService().CreateBackupAsync(_dataDirectory, _backupsDirectory, Token);

        var snapshotPath = Path.Combine(result.BundlePath, "database", "memory.db");
        Assert.Contains(marker, Encoding.UTF8.GetString(await File.ReadAllBytesAsync(snapshotPath, Token)));
        await using (var check = new SqliteConnection($"Data Source={snapshotPath};Mode=ReadOnly;Pooling=False"))
        {
            await check.OpenAsync(Token);
            await using var command = check.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM messages WHERE content=$marker;";
            command.Parameters.AddWithValue("$marker", marker);
            Assert.Equal(1L, Convert.ToInt64(await command.ExecuteScalarAsync(Token)));
        }
    }

    [Fact]
    public async Task Bundle_never_ships_wal_or_shm_sidecars_and_the_snapshot_is_a_single_self_contained_file()
    {
        await SeedSyntheticDataDirectoryAsync();
        var result = await new DataBackupService().CreateBackupAsync(_dataDirectory, _backupsDirectory, Token);

        var sidecars = Directory.EnumerateFiles(result.BundlePath, "*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith("-wal", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith("-shm", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith("-journal", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.Empty(sidecars);
        var journalMode = await QueryScalarAsync(
            Path.Combine(result.BundlePath, "database", "memory.db"), "PRAGMA journal_mode;");
        Assert.NotEqual("wal", journalMode); // 快照自带全部已提交内容，恢复时不需要任何侧文件。
    }

    [Fact]
    public async Task Backup_records_a_hash_per_file_and_passes_pragma_integrity_check()
    {
        await SeedSyntheticDataDirectoryAsync();
        var result = await new DataBackupService().CreateBackupAsync(_dataDirectory, _backupsDirectory, Token);

        Assert.Equal("ok", result.Manifest.Database!.IntegrityCheck);
        var snapshot = await SqliteBackupDiagnostics.CaptureFingerprintAsync(
            Path.Combine(result.BundlePath, "database", "memory.db"), Token);
        Assert.Equal(result.Manifest.Database.Fingerprint.RowCounts["messages"],
            snapshot.RowCounts["messages"]);
        foreach (var entry in result.Manifest.Files)
        {
            var path = Path.Combine(result.BundlePath, entry.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            var (sha, length) = await new DiskBackupFileCopier().HashFileAsync(path, Token);
            Assert.Equal(entry.Sha256, sha);
            Assert.Equal(entry.LengthBytes, length);
            Assert.Matches("^[0-9a-f]{64}$", entry.Sha256);
        }
        // 备份侧的独立体检通道也必须过。
        var manifest = await DataBackupService.VerifyBundleAsync(result.BundlePath, Token);
        Assert.Equal(result.Manifest.BundleId, manifest.BundleId);
    }

    [Fact]
    public async Task Secrets_logs_runtime_and_user_imports_never_enter_or_leave_the_bundle()
    {
        await SeedSyntheticDataDirectoryAsync();
        var result = await new DataBackupService().CreateBackupAsync(_dataDirectory, _backupsDirectory, Token);

        foreach (var forbidden in new[] { "runtime.json", "logs", "attachments", "temp", ".key", "memory.db-wal", "memory.db-shm" })
            Assert.DoesNotContain(result.Manifest.Files, entry =>
                entry.RelativePath.Contains(forbidden, StringComparison.OrdinalIgnoreCase));
        Assert.Contains("files/preferences.json", result.Manifest.Files.Select(entry => entry.RelativePath));
        Assert.Contains("files/characters/aria.json", result.Manifest.Files.Select(entry => entry.RelativePath));
        Assert.Contains("files/media/0ba97852dcaa46fbb7f916c3ecb11431.png", result.Manifest.Files.Select(entry => entry.RelativePath));
        Assert.Contains("files/interaction-profiles/aria.json", result.Manifest.Files.Select(entry => entry.RelativePath));
        // 假秘密串既不许出现在清单里，也不许出现在任何一个包内文件中（连白名单目录里的伪装件也不行）。
        var bundleText = new StringBuilder();
        foreach (var path in Directory.EnumerateFiles(result.BundlePath, "*", SearchOption.AllDirectories)
                     .Where(path => !path.EndsWith("memory.db", StringComparison.OrdinalIgnoreCase)))
            bundleText.Append(File.ReadAllText(path));
        Assert.DoesNotContain("FAKE", bundleText.ToString(), StringComparison.Ordinal);
        // 排除项必须如实披露：恢复后"少了什么"不能靠猜。
        Assert.NotEmpty(result.Manifest.ExcludedRootEntries);
        Assert.Contains(result.Manifest.ExcludedRootEntries,
            reason => reason.Contains("日志", StringComparison.Ordinal));
        Assert.Contains(result.Manifest.ExcludedRootEntries,
            reason => reason.Contains("令牌", StringComparison.Ordinal));
        Assert.Contains(result.Manifest.ExcludedRootEntries,
            reason => reason.Contains("用户导入", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Verification_fails_loudly_when_any_bundled_file_was_tampered_afterwards()
    {
        await SeedSyntheticDataDirectoryAsync();
        var result = await new DataBackupService().CreateBackupAsync(_dataDirectory, _backupsDirectory, Token);
        var victim = Path.Combine(result.BundlePath, "files", "characters", "aria.json");
        File.WriteAllText(victim, """{"id":"aria","name":"被人改过了"}""");

        var error = await Assert.ThrowsAsync<BackupIntegrityException>(
            () => DataBackupService.VerifyBundleAsync(result.BundlePath, Token));
        Assert.Contains("characters/aria.json", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Disk_full_midway_leaves_no_claimable_backup_and_a_retry_afterwards_succeeds()
    {
        // 磁盘满形状：拷贝通道在第 3 个文件上抛 "No space left on device"。
        // 判据不是"报了什么错"，而是：源目录一字未动、没有可被认领的半成品包、腾出空间后重跑成功。
        await SeedSyntheticDataDirectoryAsync();
        var sourceFingerprintBefore = Directory.EnumerateFiles(_dataDirectory, "*", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => $"{Path.GetRelativePath(_dataDirectory, path)}:{new FileInfo(path).Length}")
            .ToList();

        var error = await Assert.ThrowsAsync<IOException>(() => new DataBackupService(copier: new FailOnNthFileCopier(3))
            .CreateBackupAsync(_dataDirectory, _backupsDirectory, Token));
        Assert.Contains("space", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(DataBackupService.FindCompleteBundles(_backupsDirectory));
        Assert.Equal(sourceFingerprintBefore, Directory.EnumerateFiles(_dataDirectory, "*", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => $"{Path.GetRelativePath(_dataDirectory, path)}:{new FileInfo(path).Length}")
            .ToList());

        var result = await new DataBackupService().CreateBackupAsync(_dataDirectory, _backupsDirectory, Token);
        Assert.NotNull(await BackupBundleManifest.LoadAsync(result.BundlePath, Token));
    }

    [Fact]
    public async Task A_backup_whose_response_was_lost_is_still_discovered_and_verifiable()
    {
        // "备份成功但响应丢失"（v4 §9.2 同款形状）：调用方没拿到返回值不等于备份没成。
        // 发现通道以清单存在为准；没清单的半成品目录不得被当成备份。
        await SeedSyntheticDataDirectoryAsync();
        var result = await new DataBackupService().CreateBackupAsync(_dataDirectory, _backupsDirectory, Token);
        var halfWritten = Path.Combine(_backupsDirectory, "pry-backup-20261021-000000-deadbeef");
        Directory.CreateDirectory(Path.Combine(halfWritten, "files")); // 崩溃留下的半截目录：有数据没清单。

        var found = DataBackupService.FindCompleteBundles(_backupsDirectory);
        Assert.Equal(new[] { result.BundlePath }, found);
        var manifest = await DataBackupService.VerifyBundleAsync(found[0], Token);
        Assert.Equal(result.Manifest.BundleId, manifest.BundleId);
        Assert.NotNull(manifest.Database);
    }

    [Fact]
    public async Task Backing_up_into_the_data_directory_itself_is_refused()
    {
        await SeedSyntheticDataDirectoryAsync();
        await Assert.ThrowsAsync<RestoreTargetRejectedException>(() =>
            new DataBackupService().CreateBackupAsync(_dataDirectory, _dataDirectory, Token));
        await Assert.ThrowsAsync<RestoreTargetRejectedException>(() =>
            new DataBackupService().CreateBackupAsync(_dataDirectory,
                Path.Combine(_dataDirectory, "backups"), Token));
    }

    [Fact]
    public async Task A_missing_wal_sidecar_still_yields_a_consistent_snapshot_of_what_the_main_file_holds()
    {
        // WAL 侧文件缺失：sqlite 把库当成"上次 checkpoint 的状态"打开。备份必须照常给出
        // 自洽快照并通过完整性检查——同时如实反映库此刻的真实内容，不许无中生有。
        await SeedSyntheticDataDirectoryAsync();
        SqliteTestPools.Clear(_databasePath); // 池清空=最后关连接，自动 checkpoint 落盘并删除 -wal。
        Assert.False(File.Exists(_databasePath + "-wal"));

        var result = await new DataBackupService().CreateBackupAsync(_dataDirectory, _backupsDirectory, Token);
        var (ok, report) = await SqliteBackupDiagnostics.IntegrityCheckAsync(
            Path.Combine(result.BundlePath, "database", "memory.db"), Token);
        Assert.True(ok, report);
        var fingerprint = result.Manifest.Database!.Fingerprint;
        Assert.True(fingerprint.RowCounts["messages"] >= 2);
    }

    [Fact]
    public async Task An_inconsistent_wal_sidefile_never_travels_into_the_bundle()
    {
        // WAL 侧文件不一致（头部损坏的 -wal + 无主 -shm）：损坏的 WAL 会被 SQLite 判定无效并按主文件口径打开。
        // 备份的判据是：不被垃圾侧文件带崩、快照自洽、且这些文件一个都不进包。
        await SeedSyntheticDataDirectoryAsync();
        SqliteTestPools.Clear(_databasePath);
        File.WriteAllBytes(_databasePath + "-wal", new byte[4096]); // 全零头部：非法 WAL，等同于不存在。
        File.WriteAllBytes(_databasePath + "-shm", new byte[4096]);

        var result = await new DataBackupService().CreateBackupAsync(_dataDirectory, _backupsDirectory, Token);
        var (ok, report) = await SqliteBackupDiagnostics.IntegrityCheckAsync(
            Path.Combine(result.BundlePath, "database", "memory.db"), Token);
        Assert.True(ok, report);
        Assert.DoesNotContain(Directory.EnumerateFiles(result.BundlePath, "*", SearchOption.AllDirectories),
            path => path.EndsWith("-wal", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith("-shm", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<string> QueryScalarAsync(string databasePath, string sql)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly;Pooling=False");
        await connection.OpenAsync(Token);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (await command.ExecuteScalarAsync(Token))?.ToString() ?? "";
    }

    /// <summary>故障注入通道：第 N 次拷贝抛磁盘满，其余照常。生产路径不传它，行为与直拷一致。</summary>
    private sealed class FailOnNthFileCopier(int failOnCall) : IBackupFileCopier
    {
        private int _calls;

        public async Task CopyAsync(string sourcePath, string targetPath, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _calls) == failOnCall)
                throw new IOException("No space left on device（用例注入：磁盘满）");
            await new DiskBackupFileCopier().CopyAsync(sourcePath, targetPath, cancellationToken);
        }

        public Task<(string Sha256, long LengthBytes)> HashFileAsync(string path, CancellationToken cancellationToken) =>
            new DiskBackupFileCopier().HashFileAsync(path, cancellationToken);
    }
}
