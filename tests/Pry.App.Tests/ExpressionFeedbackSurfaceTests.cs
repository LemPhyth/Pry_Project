using Pry.Contracts;
using Pry.Core.Runtime;
using Xunit;

namespace Pry.App.Tests;

/// <summary>
/// <c>DEF-P4LEARN-01</c> 的界面与出口面。四条判的不是"这个功能有没有"，而是"这条线会不会在后续某次
/// 顺手改动里被挪走"：
/// 一，那两个动作只在高级开关后面出现（作者原话"作为高级选项"，默认看不见）；
/// 二，一次表态只有一个来源——用户按下去的那一下，<c>FeedbackIsExplicit = true</c> 在整个 <c>src/</c> 只有一处，
/// 而认可的那份形状只能来自"她这一条真的说过"的那句（§3.1"不许从上下文猜态度"的化身）；
/// 三，归属解析只认投递凭据，不退化成"按最近一轮"，也不为反馈新增端口方法（§3.4、契约 §7.1 结论 1）；
/// 四，这条线经过界面的地方一个内部数值都不许有（§2.8）。
/// <see cref="AffinityNumbersStayInsideTests"/> 那三面已在扫那些标识符，这里补上"回执本身也不能带数"。
///
/// <para>
/// 与 <c>Pry.Api.Tests</c> 那两份的分工：那边判行为（点了学到什么、对不上怎么拒），这里判形状与出口。
/// 源码文本扫描的口径与 <see cref="UserFacingNoticeBoundaryTests"/> 一致：注释里出现同样的字样也算——
/// 判的是"这个形状有没有出现在这一层"，不是"这行会不会执行"。
/// </para>
/// </summary>
public sealed class ExpressionFeedbackSurfaceTests
{
    private const string ApprovedLabel = "就是这样";
    private const string RejectedLabel = "别这样说";
    private const string SwitchLabel = "在她的回复上显示「就是这样／别这样说」";

    /// <summary>§2.6 界面那一半：两个动作只由那条开关放出来，开关名就是设置里持久化的那一个。</summary>
    [Fact]
    public void The_two_reaction_items_only_exist_behind_the_advanced_switch()
    {
        var window = Read("src", "Pry.App", "PryMessengerWindow.axaml.cs");
        var guard = "if (!scripted && _preferences?.ExplicitExpressionFeedback == true)";
        Assert.Equal(1, Occurrences(window, guard));
        // 加两项的代码必须排在开关判定之后：先加再判等于默认就露出来。
        var guardedAt = window.IndexOf(guard, StringComparison.Ordinal);
        var firstItem = window.IndexOf("ExplicitFeedbackItem(message,", StringComparison.Ordinal);
        Assert.True(guardedAt >= 0 && firstItem > guardedAt,
            "两个表态动作没有排在开关判定之后，默认状态就会露出来。");
        Assert.Equal(2, Occurrences(window, "ExplicitFeedbackItem(message,"));
        Assert.Equal(1, Occurrences(window, $"\"{ApprovedLabel}\", true"));
        Assert.Equal(1, Occurrences(window, $"\"{RejectedLabel}\", false"));
        // 失败只能走呈现层组织的措辞，异常原文不许进界面（UI-027 立项要消灭的形状）。
        Assert.Equal(1, Occurrences(window, "ShowNoticeAsync(\"这条反应没有收下\", ex)"));
    }

    /// <summary>§2.6：开关是真的设置项，走那条既有通路，而且默认关着——作者要的是"慢慢打磨"是默认路径。</summary>
    [Fact]
    public void The_switch_is_a_settings_item_on_the_existing_round_trip()
    {
        var axaml = Read("src", "Pry.App", "PryMessengerSettingsWindow.axaml");
        Assert.Contains(SwitchLabel, axaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ExplicitFeedbackBox\"", axaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ExplicitFeedbackPanel\" IsVisible=\"False\"", axaml, StringComparison.Ordinal);

        var code = Read("src", "Pry.App", "PryMessengerSettingsWindow.axaml.cs");
        Assert.Contains("ExplicitFeedbackBox.IsChecked = _preferences.ExplicitExpressionFeedback", code,
            StringComparison.Ordinal);
        // 保存时是这个 CheckBox 决定那个布尔位，而不是写死一个常量。
        Assert.Equal(1, Occurrences(code, "theme, ExplicitFeedbackBox.IsChecked == true)"));

        Assert.False(new Pry.Core.Models.UserPreferences().ExplicitExpressionFeedback);
        var request = System.Text.Json.JsonSerializer.Serialize(
            new UpdateClientPreferencesRequest(null, null, null, null, null, null, null, true),
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.Contains("\"explicitExpressionFeedback\":true", request, StringComparison.Ordinal);
    }

    /// <summary>
    /// §3.1 的化身：整个 <c>src/</c> 里"这次表态是显式的"只有一处为真，而且就写在归属解析成功之后的那笔
    /// 重放上。把"投递即认可""用户复述过就算认可""没有反对就是认可"写进来都会让这条变红——
    /// 那三条正是作者明令不许的形状。
    /// </summary>
    [Fact]
    public void Only_the_click_itself_can_mark_a_reaction_explicit()
    {
        var sources = ProductionSources();
        var explicitWriters = sources.Where(pair =>
            pair.Text.Contains("FeedbackIsExplicit = true", StringComparison.Ordinal)).ToArray();

        var gateway = Assert.Single(explicitWriters);
        Assert.EndsWith("CharacterIntelligenceGateway.cs", gateway.Path, StringComparison.Ordinal);
        Assert.Contains("ReportExplicitFeedbackAsync", gateway.Text, StringComparison.Ordinal);

        // 认可的那份形状只能来自那一条气泡里真的出现过的说法，并且由那一下点击决定方向。
        Assert.Contains("AcceptedExpressionStyles = approved ? shapes : []", gateway.Text, StringComparison.Ordinal);
        Assert.Contains("RejectedExpressionStyles = approved ? [] : shapes", gateway.Text, StringComparison.Ordinal);
        Assert.Contains("ExpressionLearner.ShapesIn(receipt.Text)", gateway.Text, StringComparison.Ordinal);

        // 反馈入口第一次有生产调用者，而且只有这一个。
        Assert.Equal(1, sources.Sum(pair => Occurrences(pair.Text, ".ObserveFeedbackAsync(")));
    }

    /// <summary>§3.4 点名的两条界：对不上就拒，不许退化成"按最近一轮归属"；也不许给反馈加第七个外发方法。</summary>
    [Fact]
    public void Attribution_refuses_instead_of_guessing_and_adds_no_new_egress()
    {
        var gateway = Read("src", "Pry.Api", "Runtime", "CharacterIntelligenceGateway.cs");
        Assert.Contains("_receipts.TryGetValue(messageId, out receipt)", gateway, StringComparison.Ordinal);
        Assert.Contains("ReceiptLimit", gateway, StringComparison.Ordinal);
        // 任何"取最新那一条凭据"的写法都是把这刀退回 DEF-P3SEAM-02 花一整刀拆掉的形状。
        Assert.DoesNotContain("_receiptOrder[^", gateway, StringComparison.Ordinal);
        Assert.DoesNotContain("LatestTurn", gateway, StringComparison.Ordinal);

        // 端口形状冻结：表态走新增的 Api 服务，不是 sink 的第七个方法。
        var egress = typeof(IRuntimeOutputSink).GetMethods();
        Assert.Equal(6, egress.Length);
        Assert.All(egress, method => Assert.DoesNotContain("ExpressionFeedback", method.Name,
            StringComparison.Ordinal));
    }

    /// <summary>§2.5 + §2.8：客户端与服务端报的是同一串路径；回执里一个数值都不许有。</summary>
    [Fact]
    public void The_route_is_one_string_and_the_receipt_carries_no_number()
    {
        var controller = Read("src", "Pry.Api", "Controllers", "ChatController.cs");
        var client = Read("src", "Pry.Client", "PryBackendClient.cs");
        const string suffix = "}/expression-feedback";
        // 两处各自恰好一次：改掉任何一边，界面点下去就是 404，而 Api 侧的判据只会红在别人的轮次里。
        Assert.Equal(1, Occurrences(controller, suffix));
        Assert.Equal(1, Occurrences(client, suffix));

        // 回执只有"哪一条 + 收到没有"。把置信度、次数、档位塞进对外契约是这一刀最容易被顺手做坏的地方：
        // 消息行 id 是客户端本来就持有的那一个，不是新露出来的一格刻度。
        var response = typeof(ExpressionFeedbackResponse).GetProperties();
        Assert.Equal(2, response.Length);
        Assert.Equal(["MessageId", "Outcome"], response
            .Select(property => property.Name).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.DoesNotContain(response, property => ScalesVocabulary.Any(word =>
            property.Name.Contains(word, StringComparison.Ordinal)));
        var request = Assert.Single(typeof(ExpressionFeedbackRequest).GetProperties());
        Assert.Equal("Approved", request.Name);
        Assert.Equal(typeof(bool), request.PropertyType);
    }

    /// <summary>
    /// §2.7 的源码那一半：表态这条路上一个人都不许顺手排空。行为判据（<c>Pry.Api.Tests</c> 里
    /// "点一下不等慢库"那条）会随文件改名一起漂走，这里守的是形状本身——一次点击把慢仓储搬到
    /// 自己的请求上，等于让表态重新变成会拖慢界面的那条通道。
    /// </summary>
    [Fact]
    public void A_reaction_never_drains_the_learning_queue_inside_the_request()
    {
        foreach (var relative in new[]
        {
            Path.Combine("src", "Pry.Api", "Services", "ExpressionFeedbackService.cs"),
            Path.Combine("src", "Pry.Api", "Runtime", "CharacterIntelligenceGateway.cs"),
            Path.Combine("src", "Pry.Api", "Controllers", "ChatController.cs")
        })
        {
            Assert.True(File.Exists(Path.Combine(RepositoryRoot(), relative)), $"{relative} 不在原位，本门禁结论无效。");
            Assert.DoesNotContain("DrainAsync(", Read(relative), StringComparison.Ordinal);
        }

        // 排空仍然只有宿主那一个后台调用者。
        Assert.Equal(1, Occurrences(Read("src", "Pry.Api", "Services", "ExpressionDrainService.cs"), "DrainAsync("));
    }

    /// <summary>
    /// 这条线上不许出现的词：内部亲密刻度（与 <see cref="AffinityNumbersStayInsideTests"/> 同一族）
    /// 加上"计数／档位／置信度"这一族——工单 §2.8 禁的是把它们摆到用户眼前。
    /// </summary>
    private static readonly string[] ScalesVocabulary =
    [
        "Familiarity", "Trust", "Closeness", "SharedContext", "Valence", "Arousal", "Irritation",
        "Affinity", "Intimacy", "Favorability", "Confidence", "Evidence", "Count", "Score", "Level", "Percent"
    ];

    /// <summary>正向探针：这些扫描器必须先认得要禁的形状，否则门禁可以在全绿里空转。</summary>
    [Fact]
    public void The_scanners_see_the_leaks_they_claim_to_see()
    {
        Assert.Equal(2, Occurrences("a\nFeedbackIsExplicit = true;\nb\nFeedbackIsExplicit = true;\n",
            "FeedbackIsExplicit = true"));
        Assert.Equal(1, Occurrences("await scope.Intelligence.ObserveFeedbackAsync(outcome, token);",
            ".ObserveFeedbackAsync("));
        Assert.Equal(1, Occurrences("api/v1/conversations/{id}/messages/{messageId}/expression-feedback",
            "}/expression-feedback"));
        Assert.Equal(1, Occurrences("if (!scripted && _preferences?.ExplicitExpressionFeedback == true)\n{\n",
            "if (!scripted && _preferences?.ExplicitExpressionFeedback == true)"));
        // 词表得认得要禁的形状，否则"回执不能带刻度"这条门可以在全绿里空转。
        foreach (var planted in new[] { "ConfidenceScore", "EvidenceCount", "AffinityLevel", "SharedContextStrength" })
            Assert.True(ScalesVocabulary.Any(word => planted.Contains(word, StringComparison.Ordinal)),
                $"词表认不出 {planted}，这条门禁的结论无效。");
        Assert.DoesNotContain(ScalesVocabulary, word => "MessageId".Contains(word, StringComparison.Ordinal));
        // 端口方法数不是靠词表：多一个外发方法必须真的数得出来。
        Assert.Equal(6, typeof(IRuntimeOutputSink).GetMethods().Length);
        // 排空调用数得出来（含注释里那一次），否则上面那三条 DoesNotContain 可能只是扫了三个空文件。
        Assert.Equal(2, Occurrences("await learner.DrainAsync(token);\n// DrainAsync(", "DrainAsync("));
    }

    private static int Occurrences(string text, string token)
    {
        var count = 0;
        for (var index = text.IndexOf(token, StringComparison.Ordinal); index >= 0;
             index = text.IndexOf(token, index + 1, StringComparison.Ordinal))
            count++;
        return count;
    }

    private sealed record SourceFile(string Path, string Text);

    /// <summary><c>src/</c> 的全部 C# 生产源码；bin/obj 里的副本不算第二遍。</summary>
    private static SourceFile[] ProductionSources()
    {
        var root = Path.Combine(RepositoryRoot(), "src");
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !ContainsSegment(path, "obj") && !ContainsSegment(path, "bin"))
            .Select(path => new SourceFile(path, File.ReadAllText(path)))
            .ToArray();
    }

    private static bool ContainsSegment(string path, string segment) =>
        path.Contains($"{Path.DirectorySeparatorChar}{segment}{Path.DirectorySeparatorChar}",
            StringComparison.Ordinal);

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine([RepositoryRoot(), .. parts]));

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null;
             directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Pry.slnx"))) return directory.FullName;
        throw new InvalidOperationException("从测试程序集出发找不到仓库根，无法核对源码约束。");
    }
}
