using Pry.Core.Memory;

namespace Pry.Core.Backup;

/// <summary>
/// 恢复到隔离目录（HDS-001 D/E 条 / v4 §9.3）。方向与直觉相反的两条硬规则：
/// <list type="number">
///   <item><description>恢复目标必须是**另一个目录**，且当场为空（不存在或零条目）。
///   回退不许覆盖用户迁移后的新数据——"就地回滚"恰恰是这条要防的事故。</description></item>
///   <item><description>先核对再落地：包内逐文件散列不符、快照 <c>integrity_check</c> 不过、
///   或快照的登记版本高于当前二进制（<see cref="SchemaVersionTooNewException"/>，拒写门在恢复入口的同一道语义）
///   都在写入任何文件之前拦下。</description></item>
/// </list>
/// 落地过程写进目标旁的临时目录，核对全部通过后才整棵改名到位——中途被打断时
/// 最终目标从未存在过，重试是干净的重来而不是"从中间接上"。
/// </summary>
public sealed class DataRestoreService(IBackupFileCopier? copier = null)
{
    private readonly IBackupFileCopier _copier = copier ?? new DiskBackupFileCopier();

    public async Task<RestoreResult> RestoreAsync(string bundlePath, string targetDataDirectory,
        CancellationToken cancellationToken = default)
    {
        bundlePath = Path.GetFullPath(bundlePath);
        targetDataDirectory = Path.GetFullPath(targetDataDirectory);
        var manifest = await GuardTargetAndLoadManifestAsync(bundlePath, targetDataDirectory, cancellationToken);

        // 第一步：恢复前逐文件核对（散列不符就不该有一个字节流向目标）。
        await DataBackupService.VerifyBundleAsync(bundlePath, cancellationToken);

        var parent = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(targetDataDirectory))!;
        Directory.CreateDirectory(parent);
        var staging = Path.Combine(parent,
            $".{Path.GetFileName(targetDataDirectory)}.restoring-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(staging);
            foreach (var entry in manifest.Files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var source = Path.Combine(bundlePath, BackupInclusionPolicy.NormalizeBundleRelativePath(entry.RelativePath));
                var target = Path.Combine(staging, BackupInclusionPolicy.NormalizeBundleRelativePath(entry.TargetRelativePath));
                await _copier.CopyAsync(source, target, cancellationToken);
            }
            string? restoredDatabase = null;
            if (manifest.Database is not null)
            {
                restoredDatabase = Path.Combine(staging, BackupInclusionPolicy.DatabaseFileName);
                await _copier.CopyAsync(Path.Combine(bundlePath, manifest.Database.RelativePath),
                    restoredDatabase, cancellationToken);
            }
            var report = await VerifyRestoredAsync(manifest, staging, restoredDatabase, cancellationToken);
            if (!report.AllPassed)
                throw new BackupIntegrityException(
                    $"恢复核对未通过（{report.Failures.Count} 格）：{string.Join("；", report.Failures.Select(f => $"{f.Name} 期望 {f.Expected}，实测 {f.Actual}"))}。" +
                    "隔离目录未被写入，原目标保持 untouched。");
            // 最后一刻再查一次目标仍为空：与并发写者的竞争窗口必须收在改名之前。
            if (Directory.Exists(targetDataDirectory) && Directory.EnumerateFileSystemEntries(targetDataDirectory).Any())
                throw new RestoreTargetRejectedException($"恢复目标 {targetDataDirectory} 在落地瞬间已有内容，拒绝覆盖。");
            if (Directory.Exists(targetDataDirectory)) Directory.Delete(targetDataDirectory);
            Directory.Move(staging, targetDataDirectory);
            return new RestoreResult(targetDataDirectory, report);
        }
        catch
        {
            if (Directory.Exists(staging))
                try { Directory.Delete(staging, recursive: true); }
                catch (IOException) { /* 临时目录留在 .restoring-* 名字下也不会被当成数据目录；不掩盖原始异常。 */ }
            throw;
        }
    }

    private async Task<BackupBundleManifest> GuardTargetAndLoadManifestAsync(string bundlePath,
        string targetDataDirectory, CancellationToken cancellationToken)
    {
        var manifest = await BackupBundleManifest.LoadAsync(bundlePath, cancellationToken);
        var trimmedBundle = Path.TrimEndingDirectorySeparator(bundlePath);
        var trimmedTarget = Path.TrimEndingDirectorySeparator(targetDataDirectory);
        if (string.Equals(trimmedBundle, trimmedTarget, StringComparison.OrdinalIgnoreCase)
            || trimmedTarget.StartsWith(trimmedBundle + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new RestoreTargetRejectedException("恢复目标不能是备份包自身或其内部：那会把备份原地拆成数据。");
        if (trimmedBundle.StartsWith(trimmedTarget + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new RestoreTargetRejectedException("恢复目标不能包含备份包：先确定两个互不嵌套的位置。");
        if (Directory.Exists(targetDataDirectory) && Directory.EnumerateFileSystemEntries(targetDataDirectory).Any())
            throw new RestoreTargetRejectedException(
                $"恢复目标 {targetDataDirectory} 非空。回退的目标必须是另一个隔离目录——" +
                "不许覆盖用户迁移后的新数据（v4 §9.3）。");
        // 快照里的登记版本高于当前二进制：这个包属于"未来的 Pry"，交给现在的我们写就是破坏。
        if (manifest.Database is not null && manifest.CreatedByBinarySchemaVersion > MemoryDatabase.SchemaVersion)
            throw new SchemaVersionTooNewException(MemoryDatabase.SchemaVersion,
                manifest.CreatedByBinarySchemaVersion, "恢复核对清单声明的版本")
            {
                Context = "恢复",
            };
        return manifest;
    }

    private async Task<RestoreVerificationReport> VerifyRestoredAsync(BackupBundleManifest manifest, string staging,
        string? restoredDatabase, CancellationToken cancellationToken)
    {
        var items = new List<RestoreCheckItem>();
        foreach (var entry in manifest.Files)
        {
            var path = Path.Combine(staging, BackupInclusionPolicy.NormalizeBundleRelativePath(entry.TargetRelativePath));
            var (sha, length) = File.Exists(path)
                ? await _copier.HashFileAsync(path, cancellationToken)
                : ("缺失", -1L);
            items.Add(new RestoreCheckItem($"散列:{entry.TargetRelativePath.Replace('\\', '/')}",
                $"{entry.Sha256[..Math.Min(12, entry.Sha256.Length)]}…/{entry.LengthBytes}B",
                sha == "缺失" ? "缺失" : $"{sha[..Math.Min(12, sha.Length)]}…/{length}B",
                string.Equals(sha, entry.Sha256, StringComparison.OrdinalIgnoreCase) && length == entry.LengthBytes));
        }
        if (manifest.Database is null)
        {
            items.Add(new RestoreCheckItem("数据库", "无（备份时未建库）", "无", true));
            return new RestoreVerificationReport(items);
        }
        var expected = manifest.Database.Fingerprint;
        var (ok, report) = await SqliteBackupDiagnostics.IntegrityCheckAsync(restoredDatabase!, cancellationToken);
        items.Add(new RestoreCheckItem("完整性:integrity_check", manifest.Database.IntegrityCheck, report, ok));
        var actual = await SqliteBackupDiagnostics.CaptureFingerprintAsync(restoredDatabase!, cancellationToken);
        items.Add(new RestoreCheckItem("数据库结构版本",
            expected.SchemaVersion.ToString(), actual.SchemaVersion.ToString(),
            actual.SchemaVersion == expected.SchemaVersion && actual.SchemaVersion <= MemoryDatabase.SchemaVersion));
        foreach (var (table, count) in expected.RowCounts)
            items.Add(new RestoreCheckItem($"行数:{table}", count.ToString(),
                actual.RowCounts.TryGetValue(table, out var found) ? found.ToString() : "表缺失",
                actual.RowCounts.TryGetValue(table, out var value) && value == count));
        foreach (var (key, digest) in expected.StableIdDigests)
            items.Add(new RestoreCheckItem($"稳定ID:{key}", Short(digest), Short(actual.StableIdDigests.TryGetValue(key, out var f) ? f : "缺失"),
                actual.StableIdDigests.TryGetValue(key, out var found) && found == digest));
        items.Add(new RestoreCheckItem("索引集合", Short(expected.IndexDigest), Short(actual.IndexDigest),
            actual.IndexDigest == expected.IndexDigest));
        items.Add(new RestoreCheckItem("外键检查", expected.ForeignKeyViolations.ToString(),
            actual.ForeignKeyViolations.ToString(),
            actual.ForeignKeyViolations == 0 && expected.ForeignKeyViolations == 0));
        items.Add(new RestoreCheckItem("墓碑数量", expected.TombstoneCount.ToString(), actual.TombstoneCount.ToString(),
            actual.TombstoneCount == expected.TombstoneCount));
        items.Add(new RestoreCheckItem("墓碑摘要", Short(expected.TombstoneDigest), Short(actual.TombstoneDigest),
            actual.TombstoneDigest == expected.TombstoneDigest));
        items.Add(new RestoreCheckItem("删除水位(epoch/forget_before)", Short(expected.ScopeWatermarkDigest),
            Short(actual.ScopeWatermarkDigest), actual.ScopeWatermarkDigest == expected.ScopeWatermarkDigest));
        foreach (var (status, count) in expected.MemoryStatusCounts)
            items.Add(new RestoreCheckItem($"孤立态:memories.{status}", count.ToString(),
                actual.MemoryStatusCounts.TryGetValue(status, out var value) ? value.ToString() : "0",
                actual.MemoryStatusCounts.TryGetValue(status, out var found) && found == count));
        foreach (var (name, count) in expected.OrphanCounters)
            items.Add(new RestoreCheckItem($"引用计数:{name}", count.ToString(),
                actual.OrphanCounters.TryGetValue(name, out var value) ? value.ToString() : "0",
                actual.OrphanCounters.TryGetValue(name, out var found) && found == count));
        var preferencesPath = Path.Combine(staging, "preferences.json");
        var manifestHasPreferences = manifest.Files.Any(entry =>
            entry.TargetRelativePath.Equals("preferences.json", StringComparison.OrdinalIgnoreCase));
        var parseable = SqliteBackupDiagnostics.PreferencesParseable(preferencesPath);
        items.Add(new RestoreCheckItem("用户偏好可解析",
            manifestHasPreferences ? "必须可解析为设置对象" : "备份时没有偏好文件",
            File.Exists(preferencesPath) ? (parseable ? "可解析" : "解析失败") : "缺席",
            manifestHasPreferences ? File.Exists(preferencesPath) && parseable : !File.Exists(preferencesPath) || parseable));
        return new RestoreVerificationReport(items);
    }

    private static string Short(string value) =>
        string.IsNullOrEmpty(value) ? "(空)" : value.Length <= 12 ? value : value[..12] + "…";
}
