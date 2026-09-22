using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// UI-027 的门：禁令（服务端写明"重试也不会变好"）只有一个判据处。
/// 收敛之前它是两套形状——两处界面循环各自 <c>when (ex.RetryForbidden)</c> 加 <c>BackendBanNotice</c>，
/// 与客户端 Runner 加统一呈现表各说一遍；漏掉任意一处都不会有测试变红（QA-P4-01/02）。
/// 现在退避、终止、文案都由 <c>Pry.Client</c> 决定，界面只消费连接状态与终止出口，
/// 所以产品源码里除客户端层之外再出现这个判据，就是有人又开始自己判该不该重连。
/// </summary>
public sealed class SingleBanJudgeBoundaryTests
{
    [Fact]
    public void Only_the_client_transport_layer_reads_the_retry_ban()
    {
        var source = Path.Combine(RepositoryRoot(), "src");
        Assert.True(Directory.Exists(source), $"找不到产品源码目录 {source}，本门禁的结论无效。");

        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(source, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            if (relative.StartsWith(Path.Combine("Pry.Client") + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                continue;
            var lines = File.ReadAllLines(file);
            for (var index = 0; index < lines.Length; index++)
                if (lines[index].Contains("RetryForbidden", StringComparison.Ordinal))
                    offenders.Add($"{relative}:{index + 1}");
        }

        Assert.Empty(offenders);
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Pry.slnx"))) return directory.FullName;
        throw new InvalidOperationException("从测试程序集出发找不到仓库根，无法核对源码约束。");
    }
}
