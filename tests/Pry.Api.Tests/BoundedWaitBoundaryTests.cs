using System.Text.RegularExpressions;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// PLAT-001 的门：产品源码里不得再出现"把 HTTP 客户端的超时显式设成无限"的写法。
/// 改一次挡不住后来者——同一段 <c>Timeout.InfiniteTimeSpan</c> 曾经在三个界面、一个内嵌入口和两个
/// 模型注册表里各写过一遍，每一处都让前台请求在 Provider 挂起时永远等不到结果也等不到错误。
/// </summary>
public sealed class BoundedWaitBoundaryTests
{
    private static readonly Regex ExplicitlyInfinite = new(@"Timeout\s*=\s*Timeout\.InfiniteTimeSpan",
        RegexOptions.CultureInvariant);

    [Fact]
    public void No_http_client_sets_an_explicit_infinite_timeout()
    {
        var source = Path.Combine(RepositoryRoot(), "src");
        Assert.True(Directory.Exists(source), $"找不到产品源码目录 {source}，本门禁的结论无效。");

        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(source, "*.cs", SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(file);
            for (var index = 0; index < lines.Length; index++)
                if (ExplicitlyInfinite.IsMatch(lines[index]))
                    offenders.Add($"{Path.GetRelativePath(source, file)}:{index + 1}");
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
