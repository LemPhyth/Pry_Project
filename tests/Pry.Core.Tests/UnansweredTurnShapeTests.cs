using Pry.Core.Models;
using Pry.Core.TurnTaking;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// <c>DEF-P3SEAM-04</c> 完成标准 ④ 的那一侧：Core 里"这一轮没说上话"的两档形状与那条占位文本
/// 必须是同一个出口，而不是学习侧再写一套"什么算空"。
/// 形状抽取本身有 <c>ModelReplyFormatTests</c> 那批守着（判据归 <c>DEF-P1WALK-01</c>），
/// 这里只钉"形状 → 落库正文 → 可见性判定"这三步落在同一档上；宿主侧的账本另有
/// <c>tests/Pry.Api.Tests/UnansweredTurnLedgerTests.cs</c>，两边读数不互顶。
/// </summary>
public sealed class UnansweredTurnShapeTests
{
    private static readonly IReadOnlyCollection<StickerDefinition> NoStickers = [];

    private const char Begin = (char)0x3C;
    private const char End = (char)0x3E;
    private static readonly string DraftOpen = $"{Begin}analysis{End}";
    private static readonly string DraftClose = $"{Begin}/analysis{End}";

    /// <summary>
    /// 两档"没有可见答复"的形状：整段是草稿，以及一个字都没有。
    /// 形状名写成字符串是因为 <c>ModelReplyShape</c> 是 Core 的 internal 枚举——公开的测试方法
    /// 不能拿它当参数类型（CS0051），而把枚举转 public 正是工单 §3 第 5 条禁的那条捷径。
    /// </summary>
    public static TheoryData<string, string> UnansweredShapes() => new()
    {
        { $"{DraftOpen}先安慰她，再问周末{DraftClose}", "OnlyInternalDraft" },
        { "   ", "Empty" },
        { "", "Empty" }
    };

    [Theory]
    [MemberData(nameof(UnansweredShapes))]
    public void The_shape_that_says_she_said_nothing_exits_through_the_placeholder_and_reads_as_silent(
        string raw, string expected)
    {
        Assert.Equal(expected, ModelReplyFormat.Extract(raw).Shape.ToString());

        var planned = ReplyPlanner.Parse(raw, new TurnTakingSettings(), NoStickers, 4);
        var only = Assert.Single(planned);
        Assert.Equal(VisibleReply.Unanswered, only.Content);
        Assert.False(VisibleReply.HasVisibleContent(planned.Select(x => x.Content ?? string.Empty)));
    }

    [Fact]
    public void A_reply_she_actually_says_reads_as_visible_content()
    {
        var planned = ReplyPlanner.Parse("我在的，周末想去哪儿？", new TurnTakingSettings(), NoStickers, 4);

        Assert.True(VisibleReply.HasVisibleContent(planned.Select(x => x.Content ?? string.Empty)));
    }
}
