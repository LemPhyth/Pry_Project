using Xunit;

namespace Pry.App.Tests;

/// <summary>
/// UI-027 的防复发门：已经交出去的文件里，<c>.Message</c> 不得再出现在任何位置——界面一旦重新开口转述异常，
/// 用户读到的就是 <c>The response ended prematurely.</c> 这一类原文，而这正是 UI-027 立项要消灭的形状。
/// <para>
/// 文本扫描，与 <see cref="Pry.Api.Tests.DiscoveryDataDirectoryBoundaryTests"/> 同一手法：注释里出现同样的字样也会红，
/// 这是有意的——判的是"这个形状有没有出现在这一层"，不是"这行会不会执行"。
/// </para>
/// <para>
/// 覆盖面是一张<b>显式清单</b>：先改干净、再登记，不靠全仓扫描充数。
/// <c>MainWindow.axaml.cs</c>（10 处）刻意不在清单内，受第 0 层 <c>0-5</c> 反向约束"不许顺手碰 <c>MainWindow</c> 家族"的禁令；
/// 四个旧窗口与三个面板合计 22 处属 UI-021/022/COMP-006 名下文件，登记为下一段。
/// </para>
/// </summary>
public sealed class UserFacingNoticeBoundaryTests
{
    private static readonly string[] ClosedFiles =
    [
        Path.Combine("src", "Pry.App", "PryMessengerWindow.axaml.cs"),
        Path.Combine("src", "Pry.App", "PryMessengerWindow.Companion.cs"),
        Path.Combine("src", "Pry.App", "App.axaml.cs"),
    ];

    [Fact]
    public void Converted_files_never_carry_a_raw_exception_message()
    {
        var root = RepositoryRoot();
        var offenders = new List<string>();

        foreach (var relative in ClosedFiles)
        {
            var path = Path.Combine(root, relative);
            Assert.True(File.Exists(path), $"找不到 {relative}，本门禁的结论无效。");
            var lines = File.ReadAllLines(path);
            for (var index = 0; index < lines.Length; index++)
                if (IsOffender(lines[index])) offenders.Add($"{relative}:{index + 1}");
        }

        Assert.Empty(offenders);
    }

    /// <summary>正向探针：扫描器看不见东西时上一条会静默常绿，所以先证明它认得要禁的形状。</summary>
    [Fact]
    public void The_scanner_sees_the_shape_it_claims_to_ban()
    {
        Assert.True(IsOffender("            await ShowNoticeAsync(\"消息没有发送\", ex.Message);"));
        Assert.True(IsOffender("                        new TextBlock { Text = exception.Message, TextWrapping = Wrap },"));
        Assert.True(IsOffender("            ConversationStatusText.Text = ex.Message;"));
        Assert.False(IsOffender("            await ShowNoticeAsync(\"消息没有发送\", ex);"));
        Assert.False(IsOffender("            ConversationStatusText.Text = NoticeContentFactory.StatusText(ex);"));
    }

    /// <summary>另一半探针：文件在≠接线在。删掉 <c>ShowNoticeAsync(string, Exception)</c> 那一个重载，调用点会静默退回 <c>(string, string)</c>。</summary>
    [Fact]
    public void The_closed_files_actually_take_their_words_from_the_presenting_layer()
    {
        var root = RepositoryRoot();

        foreach (var relative in ClosedFiles)
            Assert.Contains("NoticeContentFactory", File.ReadAllText(Path.Combine(root, relative)));

        var window = File.ReadAllText(Path.Combine(root, "src", "Pry.App", "PryMessengerWindow.axaml.cs"));
        Assert.Contains("ShowNoticeAsync(string title, Exception error)", window);
        Assert.DoesNotContain("ShowNoticeAsync(string title, string detail, string", window);
    }

    private static bool IsOffender(string line) => line.Contains(".Message", StringComparison.Ordinal);

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Pry.slnx"))) return directory.FullName;
        throw new InvalidOperationException("从测试程序集出发找不到仓库根，无法核对源码约束。");
    }
}
