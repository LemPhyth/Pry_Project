using System.Text;
using Pry.Core.Models;
using Pry.Core.TurnTaking;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// DEF-P1WALK-01 的判据：模型信封里的内部草稿不得成为落库的助手内容，抽取失败也不得回落成整段原文。
/// 用例逐条对应真机走查记下的形状（W1：草稿正文 + 闭合推理标签 + 信封里的 JSON；第九/十一轮：没有信封的
/// 裸 JSON 体；W3：复述指令式自述收尾在半截标签；简化 JSON 带代码围栏），另加三条反向用例——
/// 正文里合法的尖括号、正文里引用的小段 JSON、以及正常散文答复，都不许被这条判据改成沉默。
/// 落库侧的统一判据只有一条：写进去的内容比原始输出短，且不含任何信封标签。
/// </summary>
public sealed class ModelReplyFormatTests
{
    private const char Begin = (char)0x3C;
    private const char End = (char)0x3E;
    private const char Slash = (char)0x2F;
    private static readonly string ReasoningOpen = $"{Begin}thinking{End}";
    private static readonly string ReasoningClose = $"{Begin}/thinking{End}";
    private static readonly string ResponseOpen = $"{Begin}RESPONSE{End}";
    private static readonly string ResponseClose = $"{Begin}/RESPONSE{End}";

    private static readonly IReadOnlyCollection<StickerDefinition> NoStickers = [];

    [Fact]
    public void Draft_followed_by_enveloped_plan_yields_only_the_plan()
    {
        var raw = "以下是对角色的分析和总结：\n\n1. **角色定位**：你被设定为魔界公主。\n"
            + "7. **潜在挑战**：记住并提醒用户会比较困难。" + ReasoningClose + ResponseOpen
            + "{ \"messages\": [ { \"type\": \"text\", \"content\": \"好的，小王！我记下了你周三要去医院复诊。\", \"stickerId\": null } ] }"
            + ResponseClose;

        var messages = ReplyPlanner.Parse(raw, new TurnTakingSettings(), NoStickers, 4);

        AssertVisible(messages, raw);
        Assert.Equal("好的，小王！我记下了你周三要去医院复诊。", Assert.Single(messages).Content);
    }

    [Fact]
    public void Bare_plan_without_envelope_still_parses()
    {
        var messages = ReplyPlanner.Parse("[{\"type\":\"text\",\"content\":\"我在的\",\"stickerId\":null}]",
            new TurnTakingSettings(), NoStickers, 4);

        Assert.Equal("我在的", Assert.Single(messages).Content);
    }

    [Fact]
    public void Fenced_simplified_plan_still_parses()
    {
        var messages = ReplyPlanner.Parse("```json\n[{\"content\":\"思考链路正常\"}]\n```",
            new TurnTakingSettings(), NoStickers, 4);

        Assert.Equal("思考链路正常", Assert.Single(messages).Content);
    }

    [Fact]
    public void Prose_inside_the_reply_envelope_survives_the_reasoning_strip()
    {
        var raw = "让我先梳理一下这次对话的要点。" + ReasoningOpen + "\n用户想确认我是否在听。\n"
            + ReasoningClose + ResponseOpen + "嗯，我在听，你说。" + ResponseClose;

        var messages = ReplyPlanner.Parse(raw, new TurnTakingSettings(), NoStickers, 4);

        AssertVisible(messages, raw);
        Assert.Equal("嗯，我在听，你说。", Assert.Single(messages).Content);
    }

    [Fact]
    public void Prose_answer_without_any_envelope_is_kept()
    {
        // 本地小模型经常无视结构化要求直接说话，这一档不能因为它没有 JSON 就丢掉答复。
        var messages = ReplyPlanner.Parse("今天天气确实不错，要不要出去走走？",
            new TurnTakingSettings(), NoStickers, 4);

        Assert.Equal("今天天气确实不错，要不要出去走走？", Assert.Single(messages).Content);
    }

    [Fact]
    public void Unparsable_plan_never_falls_back_to_the_whole_raw_output()
    {
        // 旧的回落把 691 字草稿原样落库；抽取失败就是失败，这一档判据是"短、且不含草稿"。
        var raw = ResponseOpen + "{\"messages\": [{\"type\": \"text\", \"content\": \"被截断的";

        var messages = ReplyPlanner.Parse(raw, new TurnTakingSettings(), NoStickers, 4);

        AssertVisible(messages, raw);
        Assert.Equal("……", Assert.Single(messages).Content);
    }

    [Fact]
    public void Half_opened_envelope_at_the_end_of_a_truncated_stream_yields_no_draft()
    {
        // W3：复述指令式自述，收尾停在半截标签上，真答复根本没来。
        var raw = "用户的问题是：现在是几点？我需要理解用户的需求，并提供准确答案"
            + $"{Begin}/thinkin";

        var messages = ReplyPlanner.Parse(raw, new TurnTakingSettings(), NoStickers, 4);

        AssertVisible(messages, raw);
        Assert.Equal("……", Assert.Single(messages).Content);
    }

    [Fact]
    public void Empty_output_still_produces_the_placeholder_reply()
    {
        var messages = ReplyPlanner.Parse("   ", new TurnTakingSettings(), NoStickers, 4);

        Assert.Equal("……", Assert.Single(messages).Content);
    }

    [Fact]
    public void Angle_brackets_in_ordinary_prose_are_not_treated_as_an_envelope()
    {
        // 反向用例：正文里的尖括号不该被当信封吃掉，否则这条判据会把正常答复改成沉默。
        const string raw = "如果 a < b 且 b < c，那这局我肯定赢你，1<3 的那种赢。";

        var messages = ReplyPlanner.Parse(raw, new TurnTakingSettings(), NoStickers, 4);

        Assert.Equal(raw, Assert.Single(messages).Content);
    }

    [Fact]
    public void Configuration_json_quoted_inside_prose_is_not_mistaken_for_a_plan()
    {
        const string raw = "配置我写成 {\"a\": 1} 这样了，能跑。";

        var messages = ReplyPlanner.Parse(raw, new TurnTakingSettings(), NoStickers, 4);

        Assert.Equal(raw, Assert.Single(messages).Content);
    }

    [Fact]
    public void Reasoning_inside_a_plan_item_content_is_dropped_with_that_item()
    {
        var raw = "[{\"type\":\"text\",\"content\":\"" + ReasoningOpen + "先接住情绪再给建议" + ReasoningClose
            + "辛苦你了，要不要先歇会儿？\",\"stickerId\":null}]";

        var messages = ReplyPlanner.Parse(raw, new TurnTakingSettings(), NoStickers, 4);

        Assert.Equal("辛苦你了，要不要先歇会儿？", Assert.Single(messages).Content);
    }

    [Fact]
    public void Prose_answer_over_the_message_budget_is_not_silently_truncated()
    {
        var body = new string('长', 200);

        var messages = ReplyPlanner.Parse(body, new TurnTakingSettings { MaxMessageCharacters = 90 }, NoStickers, 4);

        // 半截话与内部草稿落在同一列里长得很像，所以这一档判空而不是截断。
        Assert.NotEqual(body, Assert.Single(messages).Content);
        Assert.Equal("……", Assert.Single(messages).Content);
    }

    [Fact]
    public void Merged_plan_keeps_each_message_text_and_drops_blank_ones()
    {
        var raw = "[{\"type\":\"text\",\"content\":\"第一句\"},{\"type\":\"text\",\"content\":\"   \"},"
            + "{\"type\":\"text\",\"content\":\"第二句\"}]";

        var messages = ReplyPlanner.Parse(raw, new TurnTakingSettings { SplitReplies = false }, NoStickers, 4);

        Assert.Equal("第一句\n第二句", Assert.Single(messages).Content);
    }

    [Fact]
    public void Content_that_still_carries_a_tag_after_the_pass_budget_is_dropped()
    {
        // 剥离只有 16 趟预算：超出预算还留着标签的条目宁可整条丢弃，也不带着信封落库。
        var raw = string.Concat(Enumerable.Repeat(ReasoningOpen + "笔记" + ReasoningClose, 17)) + "这一句本来是正文";

        Assert.Null(ModelReplyFormat.SanitizeMessageContent(raw));
    }

    [Fact]
    public void Shape_extraction_separates_plan_from_draft_from_prose()
    {
        Assert.Equal(ModelReplyShape.StructuredPlan,
            ModelReplyFormat.Extract("草稿正文" + ReasoningClose + ResponseOpen + "[{\"content\":\"好\"}]" + ResponseClose).Shape);
        Assert.Equal(ModelReplyShape.OnlyInternalDraft,
            ModelReplyFormat.Extract(ReasoningOpen + "整段都是草稿").Shape);
        Assert.Equal(ModelReplyShape.PlainText, ModelReplyFormat.Extract("我在。").Shape);
        Assert.Equal(ModelReplyShape.Empty, ModelReplyFormat.Extract(null).Shape);
    }

    /// <summary>
    /// 判据 ③ 的守线：角色输出格式的知识只许写在 <c>Pry.Core/TurnTaking</c> 一处。
    /// 传输层、界面与持久化层各自认一次信封，就是第二条平行处理链的开头（QA-P4-02 的形状）。
    /// 扫描器先自证：自己造一个信封必须被判为含标签，否则"零命中"只是瞎了而不是干净（见 [[falsify-new-guards]]）。
    /// </summary>
    [Fact]
    public void No_other_layer_recognises_the_reply_envelope()
    {
        Assert.True(ContainsEnvelopeTag($"前文{Begin}/thinking{End}后文"), "扫描器看不见信封标签，本门禁的结论无效。");
        Assert.False(ContainsEnvelopeTag("Task<ResponsePlan> 与 Dictionary<ReplyKind, string> 不是信封"),
            "扫描器把泛型参数当成了信封标签，本门禁的结论无效。");

        var source = Path.Combine(RepositoryRoot(), "src");
        Assert.True(Directory.Exists(source), $"找不到产品源码目录 {source}，本门禁的结论无效。");
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(source, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file).Replace('\\', '/');
            if (relative.StartsWith("Pry.Core/TurnTaking/", StringComparison.Ordinal)) continue;
            if (ContainsEnvelopeTag(File.ReadAllText(file))) offenders.Add(relative);
        }

        Assert.Empty(offenders);
    }

    /// <summary>认"源码里出现过一个信封标签"，要求标签名后紧跟闭合符、斜杠或空白，避开泛型与注释误报。</summary>
    private static bool ContainsEnvelopeTag(string text)
    {
        foreach (var name in TagNames)
            for (var index = text.IndexOf(name, StringComparison.OrdinalIgnoreCase); index >= 0;
                     index = text.IndexOf(name, index + 1, StringComparison.OrdinalIgnoreCase))
            {
                var opened = index >= 1 && text[index - 1] == Begin;
                var closed = index >= 2 && text[index - 1] == Slash && text[index - 2] == Begin;
                if (!opened && !closed) continue;
                var after = index + name.Length;
                if (after < text.Length &&
                    text[after] != End && text[after] != Slash && !char.IsWhiteSpace(text[after])) continue;
                return true;
            }
        return false;
    }

    private static readonly string[] TagNames =
        ["think", "thinking", "thought", "thoughts", "reasoning", "analysis", "analyse", "analyze",
         "plan", "scratchpad", "response", "answer", "reply"];

    /// <summary>落库内容的统一判据：比原始输出短，且不再含任何信封标签。</summary>
    private static void AssertVisible(IReadOnlyList<PlannedReplyMessage> messages, string raw)
    {
        var content = Assert.Single(messages).Content;
        Assert.NotNull(content);
        Assert.True(content!.Length < raw.Length,
            $"可见答复没有比原始输出更短，说明整段原文被回落了（{content.Length} / {raw.Length}）。");
        Assert.False(ContainsTag(content, "thinking"), $"落库内容仍含推理标签：{content}");
        Assert.False(ContainsTag(content, "RESPONSE"), $"落库内容仍含答复信封：{content}");
        Assert.DoesNotContain("\"messages\"", content, StringComparison.Ordinal);
    }

    private static bool ContainsTag(string text, string name)
        => text.Contains($"{Begin}{name}", StringComparison.OrdinalIgnoreCase)
        || text.Contains($"{Begin}/{name}", StringComparison.OrdinalIgnoreCase);

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Pry.slnx")) && Directory.Exists(Path.Combine(directory.FullName, "src")))
                return directory.FullName;
        throw new InvalidOperationException("从测试程序集出发找不到仓库根，无法核对源码约束。");
    }
}
