using Pry.Core.Memory;

namespace Pry.Core.Backup;

/// <summary>一次一致性备份的产出：备份包路径 + 当时落盘的清单。</summary>
public sealed record BackupResult(string BundlePath, BackupBundleManifest Manifest);

/// <summary>
/// 在线一致性备份（HDS-001 A/B/C 条 / v4 §9.3 / AUD-05）。
/// <para>
/// 数据库走 <c>VACUUM INTO</c> 官方在线快照通道——WAL 模式下这是唯一能把"已提交但还躺在 -wal 里"
/// 的内容一并带走的方式；只复制主 <c>.db</c> 会得到缺行残骸，正是计划书点名禁止的形状。
/// 文件集合按白名单收（角色卡、设置、受管媒体依赖），token／<c>runtime.json</c>／日志／用户导入内容
/// 在机制上进不了包，且排除项逐条记入清单如实披露。
/// </para>
/// <para>
/// 备份包的"完成"以 <see cref="BackupBundleManifest.ManifestFileName"/> 最后落盘为准：
/// 中途断电/崩溃留下的半截目录没有清单，发现通道与恢复通道都不认它。
/// </para>
/// </summary>
public sealed class DataBackupService(BackupInclusionPolicy? policy = null, IBackupFileCopier? copier = null)
{
    private readonly BackupInclusionPolicy _policy = policy ?? new BackupInclusionPolicy();
    private readonly IBackupFileCopier _copier = copier ?? new DiskBackupFileCopier();

    public async Task<BackupResult> CreateBackupAsync(string dataDirectory, string destinationParentDirectory,
        CancellationToken cancellationToken = default)
    {
        dataDirectory = Path.GetFullPath(dataDirectory);
        destinationParentDirectory = Path.GetFullPath(destinationParentDirectory);
        if (!Directory.Exists(dataDirectory))
            throw new DirectoryNotFoundException($"数据目录不存在：{dataDirectory}");
        if (Path.GetFullPath(destinationParentDirectory).StartsWith(
                Path.TrimEndingDirectorySeparator(dataDirectory) + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || Path.GetFullPath(destinationParentDirectory) == Path.TrimEndingDirectorySeparator(dataDirectory))
            throw new RestoreTargetRejectedException("备份目的地不得落在数据目录内部：数据目录里只该放数据，" +
                "否则备份会开始吃掉自己覆盖的范围，恢复时也无法区分原件与副本。");
        Directory.CreateDirectory(destinationParentDirectory);

        var bundleId = $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"[..23];
        var bundlePath = Path.Combine(destinationParentDirectory, $"pry-backup-{bundleId}");
        try
        {
            Directory.CreateDirectory(bundlePath);
            var databaseSection = await SnapshotDatabaseAsync(dataDirectory, bundlePath, cancellationToken);
            var (files, excluded) = await CopyIncludedFilesAsync(dataDirectory, bundlePath, cancellationToken);
            var manifest = new BackupBundleManifest
            {
                ManifestVersion = BackupBundleManifest.CurrentManifestVersion,
                BundleId = bundleId,
                CreatedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
                CreatedByBinarySchemaVersion = MemoryDatabase.SchemaVersion,
                Database = databaseSection,
                Files = files,
                ExcludedRootEntries = excluded,
            };
            // 清单永远是最后一笔：它出现之前，这个目录在恢复侧不存在。
            await manifest.WriteAsync(bundlePath, cancellationToken);
            return new BackupResult(bundlePath, manifest);
        }
        catch
        {
            TryRemoveIncompleteBundle(bundlePath);
            throw;
        }
    }

    /// <summary>发现通道：备份成功但响应丢失（进程被杀、返回值没送达调用方）之后，凭目录重新认定哪些包是完整的。</summary>
    public static IReadOnlyList<string> FindCompleteBundles(string destinationParentDirectory)
    {
        if (!Directory.Exists(destinationParentDirectory)) return [];
        return new DirectoryInfo(destinationParentDirectory)
            .EnumerateDirectories("pry-backup-*", SearchOption.TopDirectoryOnly)
            .Where(directory => File.Exists(Path.Combine(directory.FullName, BackupBundleManifest.ManifestFileName)))
            .Select(directory => directory.FullName)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>逐文件散列核对 + 快照完整性检查。恢复前的强制前置，也可独立用作定期体检。</summary>
    public static async Task<BackupBundleManifest> VerifyBundleAsync(string bundlePath,
        CancellationToken cancellationToken = default)
    {
        var manifest = await BackupBundleManifest.LoadAsync(bundlePath, cancellationToken);
        var mismatches = new List<string>();
        foreach (var entry in manifest.Files)
        {
            var path = Path.Combine(bundlePath, BackupInclusionPolicy.NormalizeBundleRelativePath(entry.RelativePath));
            if (!File.Exists(path)) { mismatches.Add($"{entry.RelativePath}（文件缺失）"); continue; }
            var (sha, length) = await new DiskBackupFileCopier().HashFileAsync(path, cancellationToken);
            if (!string.Equals(sha, entry.Sha256, StringComparison.OrdinalIgnoreCase) || length != entry.LengthBytes)
                mismatches.Add($"{entry.RelativePath}（散列或长度不符）");
        }
        if (manifest.Database is not null)
        {
            var snapshot = Path.Combine(bundlePath, manifest.Database.RelativePath);
            if (!File.Exists(snapshot)) mismatches.Add($"{manifest.Database.RelativePath}（快照缺失）");
            else
            {
                var (ok, report) = await SqliteBackupDiagnostics.IntegrityCheckAsync(snapshot, cancellationToken);
                if (!ok) mismatches.Add($"数据库快照 integrity_check 未通过：{report}");
            }
        }
        if (mismatches.Count > 0)
            throw new BackupIntegrityException(
                $"备份包 {Path.GetFileName(bundlePath)} 核对失败（{mismatches.Count} 项）：{string.Join("；", mismatches)}");
        return manifest;
    }

    private async Task<BackupDatabaseSection?> SnapshotDatabaseAsync(string dataDirectory, string bundlePath,
        CancellationToken cancellationToken)
    {
        var databasePath = Path.Combine(dataDirectory, BackupInclusionPolicy.DatabaseFileName);
        if (!File.Exists(databasePath)) return null; // 尚未建库的安装：没有数据库可快照，文件部分照常备份。
        var snapshotPath = Path.Combine(bundlePath, "database", BackupInclusionPolicy.DatabaseFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
        await SqliteBackupDiagnostics.CreateOnlineSnapshotAsync(databasePath, snapshotPath, cancellationToken);
        var (ok, report) = await SqliteBackupDiagnostics.IntegrityCheckAsync(snapshotPath, cancellationToken);
        if (!ok)
            throw new BackupIntegrityException(
                $"在线快照未通过 PRAGMA integrity_check：{report}。备份已中止——半成品不配当备份。");
        var fingerprint = await SqliteBackupDiagnostics.CaptureFingerprintAsync(snapshotPath, cancellationToken);
        return new BackupDatabaseSection { IntegrityCheck = report, Fingerprint = fingerprint };
    }

    private async Task<(IReadOnlyList<BackupFileEntry> Files, IReadOnlyList<string> Excluded)> CopyIncludedFilesAsync(
        string dataDirectory, string bundlePath, CancellationToken cancellationToken)
    {
        var entries = new List<BackupFileEntry>();
        var excluded = new List<string>();
        foreach (var candidate in Directory.EnumerateFileSystemEntries(dataDirectory))
        {
            var name = Path.GetFileName(candidate);
            var isDirectory = Directory.Exists(candidate);
            if (!_policy.IsRootEntryIncluded(name, isDirectory)) { excluded.Add(_policy.ExclusionReason(name)); continue; }
            if (isDirectory) await CopyDirectoryAsync(dataDirectory, candidate, bundlePath, entries, cancellationToken);
            else await CopySingleFileAsync(candidate, name, bundlePath, entries, cancellationToken);
        }
        return (entries.OrderBy(entry => entry.RelativePath, StringComparer.Ordinal).ToList(),
            excluded.Distinct(StringComparer.Ordinal).ToList());
    }

    private async Task CopyDirectoryAsync(string dataDirectory, string sourceDirectory, string bundlePath,
        List<BackupFileEntry> entries, CancellationToken cancellationToken)
    {
        foreach (var child in Directory.EnumerateFileSystemEntries(sourceDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = Path.GetFileName(child);
            if (File.GetAttributes(child).HasFlag(FileAttributes.ReparsePoint))
                continue; // 符号链接/junction 不跟随：包里只放实体字节，防止备份被诱导读取数据目录之外的内容。
            if (Directory.Exists(child)) { await CopyDirectoryAsync(dataDirectory, child, bundlePath, entries, cancellationToken); continue; }
            if (BackupInclusionPolicy.IsDeniedFile(name)) continue;
            await CopySingleFileAsync(child, Path.GetRelativePath(dataDirectory, child), bundlePath, entries, cancellationToken);
        }
    }

    private async Task CopySingleFileAsync(string sourceFile, string relativeToDataDirectory, string bundlePath,
        List<BackupFileEntry> entries, CancellationToken cancellationToken)
    {
        var bundleRelative = $"files/{relativeToDataDirectory.Replace('\\', '/')}";
        var bundleTarget = Path.Combine(bundlePath, bundleRelative);
        await _copier.CopyAsync(sourceFile, bundleTarget, cancellationToken);
        var (sha, length) = await _copier.HashFileAsync(bundleTarget, cancellationToken);
        entries.Add(new BackupFileEntry
        {
            RelativePath = bundleRelative, Sha256 = sha, LengthBytes = length,
        });
    }

    private static void TryRemoveIncompleteBundle(string bundlePath)
    {
        try { if (Directory.Exists(bundlePath)) Directory.Delete(bundlePath, recursive: true); }
        catch (IOException) { /* 半截包留在原地也没有清单，发现通道不认它；删除失败不掩盖原始异常。 */ }
    }
}
