using System.Text;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// <c>DEF-P3SEAM-04</c> 完成标准 ③ 的源码门："这一轮有没有可见答复"必须只有一处定义，且在 Core。
/// 宿主（<c>src/Pry.Api</c>）只调用那个判定口，不许自己再认一次占位文本——否则"什么算空"就分成了两套，
/// 而这套判据的价值恰恰在于两处不会各自漂移。
///
/// <para>
/// 三条各有分工：一条数占位文本在源码里出现于哪几个文件（应恰好一个，且在 <c>src/Pry.Core</c>），
/// 一条钉住网关真的在调用 Core 那个口，第三条是<b>扫描器自证</b>——
/// 先例是 <c>QA-TIP1015-01</c>：守卫的扫描面本身也要被钉住，否则"绿灯"只是文件没被读到的别名。
/// </para>
/// </summary>
public sealed class PlaceholderDefinitionStaysInCoreTests
{
    /// <summary>按字符码拼出来，本测试文件自己也不含这个字面量（与 <c>ModelReplyFormat</c> 同一手法）。</summary>
    private static readonly string Placeholder = $"{(char)0x2026}{(char)0x2026}";

    private static readonly string[] ScannedLayers = ["Pry.Core", "Pry.Api", "Pry.Client", "Pry.App"];

    [Fact]
    public void The_placeholder_is_named_by_exactly_one_file_and_that_file_is_in_Core()
    {
        var named = SourceCode()
            .Where(file => StripComments(File.ReadAllText(file)).Contains(Placeholder, StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(RepositoryRoot(), file).Replace('\\', '/'))
            .ToArray();

        Assert.Equal(["src/Pry.Core/TurnTaking/VisibleReply.cs"], named);
    }

    [Fact]
    public void The_host_asks_Core_whether_she_said_anything_instead_of_recognising_the_placeholder()
    {
        // 网关那一道闸换成比较长度、换回字面量、或者整段摘掉，都该由这条抓到——它是接线门，不是行为门。
        var gateway = StripComments(File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Pry.Api", "Runtime",
            "CharacterIntelligenceGateway.cs")));
        Assert.Contains("VisibleReply.HasVisibleContent(entry.Delivered)", gateway, StringComparison.Ordinal);
        Assert.DoesNotContain("delivered.Length == 0", gateway, StringComparison.Ordinal);

        // 生产者那侧也只引用同一个定义，不许在 ReplyPlanner 里留第二份常量。
        var planner = StripComments(File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Pry.Core", "TurnTaking",
            "ReplyPlanner.cs")));
        Assert.Contains("VisibleReply.Unanswered", planner, StringComparison.Ordinal);
        Assert.DoesNotContain("UnansweredPlaceholder", planner, StringComparison.Ordinal);
    }

    [Fact]
    public void The_placeholder_scanner_sees_the_leak_it_claims_to_ban()
    {
        // 正向探针一：扫描谓词认得出这个形状（含被行注释包着的写法），也认不出真不该报的形状。
        Assert.True(NamesPlaceholder($"\tif (text == \"{Placeholder}\") return;"));
        Assert.False(NamesPlaceholder($"\t// {Placeholder} 只是注释里提到，不算第二套判定"));
        Assert.False(NamesPlaceholder("\tif (text.Length == 0) return;"));

        // 正向探针二：扫描面真的看得见那三个承重文件。哪天目录改名或用 filters 排掉它们，
        // 上面那条"恰好一个文件"会因为什么都扫不到而常绿——这条就是禁那个形状的。
        var files = SourceCode().Select(file => Path.GetRelativePath(RepositoryRoot(), file).Replace('\\', '/'))
            .ToArray();
        Assert.Contains("src/Pry.Core/TurnTaking/VisibleReply.cs", files, StringComparer.Ordinal);
        Assert.Contains("src/Pry.Core/TurnTaking/ReplyPlanner.cs", files, StringComparer.Ordinal);
        Assert.Contains("src/Pry.Api/Runtime/CharacterIntelligenceGateway.cs", files, StringComparer.Ordinal);
        Assert.True(files.Length > 100, $"扫描面只剩 {files.Length} 个源文件，本门禁的结论无效。");
    }

    private static bool NamesPlaceholder(string text) =>
        StripComments(text).Contains(Placeholder, StringComparison.Ordinal);

    /// <summary>四个产品层的全部 <c>.cs</c>，排除构建产物；只扫这些，不扫 <c>docs/</c> 与测试面。</summary>
    private static IEnumerable<string> SourceCode()
    {
        var source = Path.Combine(RepositoryRoot(), "src");
        Assert.True(Directory.Exists(source), $"找不到产品源码目录 {source}，本门禁的结论无效。");

        var offenders = new List<string>();
        foreach (var layer in ScannedLayers)
        {
            var root = Path.Combine(source, layer);
            Assert.True(Directory.Exists(root), $"{layer} 目录不存在，扫描面收缩了，本门禁的结论无效。");
            offenders.AddRange(Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                            !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")));
        }

        return offenders;
    }

    private static string StripComments(string text)
    {
        var code = new StringBuilder();
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("//", StringComparison.Ordinal)) continue;
            code.Append(line).Append('\n');
        }

        return code.ToString();
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null;
             directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Pry.slnx")) &&
                Directory.Exists(directory.FullName + Path.DirectorySeparatorChar + "src"))
                return directory.FullName;
        throw new InvalidOperationException("从测试程序集出发找不到仓库根，无法核对源码约束。");
    }
}
