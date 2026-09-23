using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// F-1 的门。缺陷的形状不是算错，是<b>漏传</b>：<c>RuntimeDiscovery.TryConnectAsync(string? dataDirectory = null)</c>
/// 的默认值让一处无参调用编译得过、跑得通、连"发现失败回落内嵌"都表现得很正常——配置了数据目录的实例
/// 只是永远发现不了，界面安静地起第二个宿主。所以判据不能只靠用例，得让"漏传"这个动作本身编译不出红不了的门。
/// <para>
/// 三条都是文本扫描（与 <see cref="SingleBanJudgeBoundaryTests"/> 同一手法）：注释里出现同样的字样也会红，
/// 这是有意的——扫描判的是"这个形状有没有出现在这一层"，不是"这行会不会执行"。
/// </para>
/// </summary>
public sealed class DiscoveryDataDirectoryBoundaryTests
{
    /// <summary>发现入口的三个方法：任何一处用空参数调用它们，读侧就退回默认目录。</summary>
    private static readonly string[] EntryPoints = ["TryConnectAsync", "TryRead", "DescriptorPath"];

    [Fact]
    public void No_call_site_asks_for_the_default_data_directory()
    {
        var source = Path.Combine(RepositoryRoot(), "src");
        Assert.True(Directory.Exists(source), $"找不到产品源码目录 {source}，本门禁的结论无效。");

        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(source, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            // 定义与兜底都在客户端层里：那一边有 default 才让调用方必须显式给目录。
            if (relative.StartsWith(Path.Combine("Pry.Client") + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                continue;
            var text = File.ReadAllText(file);
            foreach (var entry in EntryPoints)
                for (var index = text.IndexOf($"RuntimeDiscovery.{entry}(", StringComparison.Ordinal);
                     index >= 0;
                     index = text.IndexOf($"RuntimeDiscovery.{entry}(", index + 1, StringComparison.Ordinal))
                    if (FirstArgumentIsEmpty(text, index + $"RuntimeDiscovery.{entry}(".Length))
                        offenders.Add($"{relative}:{text[..index].Count(c => c == '\n') + 1} RuntimeDiscovery.{entry}()");
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void The_ui_layer_has_one_discovery_entry_and_derives_nothing()
    {
        var source = Path.Combine(RepositoryRoot(), "src", "Pry.App");
        Assert.True(Directory.Exists(source), $"找不到界面源码目录 {source}，本门禁的结论无效。");

        var directCalls = new List<string>();
        var rederived = new List<string>();
        var usesSharedResolution = false;
        foreach (var file in Directory.EnumerateFiles(source, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file).Replace('\\', '/');
            var lines = File.ReadAllLines(file);
            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];
                // 界面里出现第二个发现入口（绕过 BackendLocator）就等于再决定一次"去哪里读"。
                if (line.Contains("RuntimeDiscovery.", StringComparison.Ordinal) &&
                    !relative.Equals("Services/BackendLocator.cs", StringComparison.Ordinal))
                    directCalls.Add($"{relative}:{index + 1}");
                // 键名和默认值都不许在界面里被读出来：那两份都是别人（写侧）已经定义过的事实源。
                // 取索引器写法而不是裸键名，注释里解释缺陷时还要提到这个键。
                if (line.Contains("Pry:DataDirectory\"]", StringComparison.Ordinal) ||
                    line.Contains("DefaultDataDirectory", StringComparison.Ordinal))
                    rederived.Add($"{relative}:{index + 1}");
                if (line.Contains("ResolveDataDirectory", StringComparison.Ordinal)) usesSharedResolution = true;
            }
        }

        Assert.Empty(directCalls);
        Assert.Empty(rederived);
        Assert.True(usesSharedResolution,
            "界面里没有一处调用共用的目录解析点，读侧就又回到各猜各的默认值了。");
    }

    [Fact]
    public void The_client_transport_layer_does_not_infer_the_data_directory()
    {
        var source = Path.Combine(RepositoryRoot(), "src", "Pry.Client");
        Assert.True(Directory.Exists(source), $"找不到客户端源码目录 {source}，本门禁的结论无效。");

        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(source, "*.cs", SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(file);
            for (var index = 0; index < lines.Length; index++)
                // 客户端只认调用方给的那个目录。它在配置层没有任何身份：宿主配置根归后端与界面。
                if (lines[index].Contains("Pry:DataDirectory", StringComparison.Ordinal) ||
                    lines[index].Contains("IConfiguration", StringComparison.Ordinal))
                    offenders.Add($"{Path.GetRelativePath(source, file)}:{index + 1}");
        }

        Assert.Empty(offenders);
    }

    /// <summary>从左括号往后看：先碰到逗号说明有实参，先碰到右括号说明这一处是空参调用。</summary>
    private static bool FirstArgumentIsEmpty(string text, int openParenIndex)
    {
        for (var index = openParenIndex; index < text.Length; index++)
        {
            var c = text[index];
            if (c == ',') return false;
            if (c == ')') return true;
            if (!char.IsWhiteSpace(c)) return false;
        }
        return false;
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Pry.slnx"))) return directory.FullName;
        throw new InvalidOperationException("从测试程序集出发找不到仓库根，无法核对源码约束。");
    }
}
