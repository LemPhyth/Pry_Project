using Pry.Core.Memory;
using Pry.Core.Narrative;
using Pry.Core.Prompting;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// HDS-003「统一预算不等式」（v4 §6）的按名判据（第二十五波片 A，工单 §一 的<b>答乙路</b>）。
///
/// <para>
/// 本格守的是 <c>src/Pry.Core/Narrative/NarrativeContextCompiler.cs:203</c> 里
/// <c>used + cost &gt; total</c> 这<b>后半项</b>。那道门有两半：<c>cost &gt; cap</c>（该段自身份额）与
/// <c>used + cost &gt; total</c>（剩余输入预算）。A1 那轮在载体里只删后半项、跑完整个
/// <c>Pry.Core.Tests</c>（712 枚节点），<c>failed = 0</c>，即后半项今天在盘上无人守（报告 §一）。
/// 于是载荷按工单 §一 答乙① 的形状构造：State 段的成本 16 装得下自身份额 240（前半项不响），
/// 但 <c>used(47) + 16 = 63</c> 超剩余 <c>total = 50</c>（只有后半项响）。
/// </para>
///
/// <para>
/// <b>本格不守 <c>:178</c>。</b><c>:178</c> 的 <c>if (!assembly.WithinUnifiedBudget)</c> 在 <c>:203</c> 完整时
/// 拿不到可达输入：<c>used</c> 的四个变更点各由 <c>:87</c>／<c>:110</c>／<c>:203</c>／<c>:246</c> 守住，
/// <c>total</c> 本身由 <c>:61-62</c> 守住，所以 <c>used &lt;= total</c> 由构造成立、不等式恒真。
/// 唯一能让 <c>:178</c> 变红的办法是删掉某道门（本刀禁止，工单 §〇.6.1），那"按名钉
/// <c>BudgetInequalityViolated</c>"在盘上没有可判红的实现路径；它登记为不可达，而不是造一枚永远绿的断言。
/// </para>
/// </summary>
public sealed class NarrativeBudgetInequalityCellTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 20, 0, 0, TimeSpan.Zero);
    private const string Conversation = "room-budget-inequality";
    private const string Batch = "batch-budget-inequality";
    private const string WriterId = "writer-budget-inequality";

    // 必需前奏的确切成本（ConservativeTokenEstimator＝UTF-8 字节数，见 Memory/MemoryContracts.cs:114）：
    //   "Pry canon"(9) + "\n"(1) + "json rules"(10) = 20，加框架开销 8；
    //   "hello there"(11)，加框架开销 8 ⇒ 20 + 8 + 11 + 8 = 47。
    private const string Identity = "Pry canon";
    private const string OutputRules = "json rules";
    private const string CurrentEventText = "hello there";
    private const int PreludeCost = 47;

    // 被测段的确切成本："\n"(1) + "State"(5) + "："(U+FF1A＝3 字节) + "\n"(1) + "studio"(6) = 16。
    private const string StateContent = "studio";
    private const int SectionCost = 16;

    // 窗口三项：total = 200 - 100 - 50 = 50（:61）；前奏 47 装得下（:87 不响），
    // State 的自身份额 cap 取默认 240（前半项 cost > cap 不响），47 + 16 = 63 > 50（后半项响）。
    private const int ContextTokens = 200;
    private const int OutputTokens = 100;
    private const int SafetyTokens = 50;
    private const int RemainingInputBudget = 50;

    private static readonly CharacterInstanceId Alpha = CharacterInstanceId.Create("ci-instance-alpha");
    private static readonly InputRef Input101 = new(101, "msg-101", Conversation);
    private static readonly ConservativeTokenEstimator Estimator = new();

    [Fact]
    public void Compiler_drops_a_whole_section_that_fits_its_own_share_but_not_the_remaining_input_budget()
    {
        var assembly = new NarrativeContextCompiler(Estimator).Compile(Request(), Payloads());
        var state = assembly.Segments.Single(segment => segment.Name == "State");

        // 整轮仍然合法收口：这一项的执行形状是"整段丢弃＋used 不变"，不是拒轮（:203–:207）。
        Assert.Equal(NarrativeCompileReason.Ok, assembly.ReasonCode);
        Assert.True(assembly.Accepted, assembly.Detail);

        // :203 的可观测面：段被整段丢弃，理由码点名 budget，且没有 emit 任何文本。
        Assert.Equal(NarrativeSegmentDisposition.Dropped, state.Disposition);
        Assert.Equal("budget", state.DispositionReason);
        Assert.Equal("", state.EmittedText);

        // 估算口径一致：丢弃段仍带它自己的确切成本（16），并且 16 既不超自身份额（240）
        // 也大于剩余预算（50 - 47 = 3）——响的确实是后半项，不是前半项。
        Assert.Equal(SectionCost, state.UsedTokens);
        Assert.Equal(SectionCost, Estimator.Count("\nState：\n" + StateContent));
        Assert.True(SectionCost <= state.BudgetTokens, $"前半项不得响：{SectionCost} <= {state.BudgetTokens}");
        Assert.True(PreludeCost + SectionCost > RemainingInputBudget,
            $"后半项必须是唯一能挡住这一段的东西：{PreludeCost} + {SectionCost} > {RemainingInputBudget}");

        // 不等式左边取确切数字（工单 §一 答乙②，不许 Assert.True 一句带过）：
        // used 停在必需前奏的 47，被丢弃的 16 一分都不进账。
        Assert.Equal(PreludeCost, assembly.InputEstimateTokens);
        Assert.Equal(RemainingInputBudget, assembly.InputBudgetTokens);
        Assert.Equal(197, assembly.InputEstimateTokens + assembly.OutputReservationTokens + assembly.SafetyTokens);
        Assert.Equal(ContextTokens, assembly.ContextTokens);
        Assert.True(assembly.WithinUnifiedBudget, assembly.Detail);
        Assert.Equal(NarrativeContextCompiler.SegmentCount, assembly.SegmentCount);
    }

    private static NarrativeRequest Request() => new()
    {
        InstanceId = Alpha,
        ConversationId = Conversation,
        BatchId = Batch,
        Inputs = [Input101],
        Trigger = NarrativeTriggerKind.UserInput,
        StoryVersion = 7,
        DeletionEpoch = 0,
        OwnershipEpoch = 1,
        IssuedByWriter = WriterId,
        MaxOutboundSegments = 4,
        IssuedAtUtc = Now,
        ProcessedThroughAtIssue = new ProcessedThroughSeq(100),
    };

    /// <summary>
    /// 载荷只带必需前奏与一段 State，其余六段 NotSupplied，保证这格唯一在判的事就是 :203 的后半项。
    /// 预算三项是测试夹具的入参（走既有 <see cref="ContextBudget"/> record 的构造参数），
    /// 不是产品里新增的数字门槛。
    /// </summary>
    private static NarrativeContextPayloads Payloads() => new()
    {
        Budget = new ContextBudget(ContextTokens: ContextTokens, OutputTokens: OutputTokens,
            SafetyTokens: SafetyTokens),
        Identity = Identity,
        OutputRules = OutputRules,
        CurrentEvent = new NarrativeContextUnit("input:101", Conversation, CurrentEventText),
        State = StateContent,
    };
}
