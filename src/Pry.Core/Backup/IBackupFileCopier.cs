namespace Pry.Core.Backup;

/// <summary>
/// 备份侧的文件拷贝通道。默认实现就是 <see cref="File.Copy"/>；
/// 把它做成注入点是给故障矩阵留的仪器接口——"磁盘满""拷贝中途被打断"这类形状
/// 无法可靠地用真磁盘制造，必须能在第 N 个文件上精确注入一次 IO 故障，
/// 否则对应的守卫就是一条常绿门（HDS-001 测试要求）。生产调用方不传，行为与直拷完全一致。
/// </summary>
public interface IBackupFileCopier
{
    Task CopyAsync(string sourcePath, string targetPath, CancellationToken cancellationToken);

    /// <summary>同一份内容的散列与长度，拷贝时顺手算出，不再二次读文件。</summary>
    Task<(string Sha256, long LengthBytes)> HashFileAsync(string path, CancellationToken cancellationToken);
}

/// <summary>默认实现：普通文件拷贝 + 流式 SHA-256。</summary>
public sealed class DiskBackupFileCopier : IBackupFileCopier
{
    public async Task CopyAsync(string sourcePath, string targetPath, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(targetPath))!);
        using var source = File.OpenRead(sourcePath);
        await using var target = File.Create(targetPath);
        await source.CopyToAsync(target, cancellationToken);
    }

    public async Task<(string Sha256, long LengthBytes)> HashFileAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(stream, cancellationToken))
            .ToLowerInvariant();
        return (hash, new FileInfo(path).Length);
    }
}
