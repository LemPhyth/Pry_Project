using Pry.Core.Memory;
using Pry.Core.Narrative;
using Pry.Core.Prompting;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// HDS-010 数据口 5→6 的提交构造门（第二十五波片 B，工单 §二）。
///
/// <para>
/// 被测对象＝<c>src/Pry.Core/Narrative/NarrativeRoundRunner.cs</c> 里被从 <c>private</c> 提成
/// <c>internal static</c> 的 <c>BuildCommit(NarrativeDraft, NarrativeRequest, DateTimeOffset, string writerId)</c>。
/// 本刀只改可见性与 <c>_writerId</c> 的来路（改成入参），九项哈希输入与 <see cref="NarrativeCommit"/> 的
/// 字段赋值逐字节未动。
/// </para>
///
/// <para>
/// 下面那几枚十六进制串不是"跑出来的期望值"，是这一手当场用独立仪器（<c>.local-notes/wave25-1023/out/
/// compute-commit-hashes.ps1</c>，与产品码不同的实现路径）算出的绝对字面量，形状与
/// <c>NarrativeRoundRunner.cs</c> 的 <c>SHA256.HashData(UTF8(string.Join(九项, 分隔符 U+241F)))</c> 对齐，
/// 小写、无分隔。九个成员按源码里的顺序是：
/// ① <c>commitId</c>（＝"commit-" + ProposalId）② <c>draft.InstanceId.Value</c> ③ <c>draft.ConversationId</c>
/// ④ <c>draft.BatchId</c> ⑤ <c>draft.NarrativeProse</c> ⑥ <c>draft.ProcessedThrough.ToString()</c>（水位数字）
/// ⑦ 感知引用的序号以逗号连接 ⑧ 每条动作的 "ActionId:(int)Mode" 以逗号连接
/// ⑨ 每条意图的 IntentId 以逗号连接（本夹具为空 ⇒ 末尾只有一个分隔符）。
/// 分隔符是 U+241F（␟，UTF-8 三字节 E2 90 9F）。
/// </para>
///
/// <para>
/// 工单 §〇.6.4 那句话照登：**"两侧都走同一个工厂"不算覆盖证据**——所以本文件真正的门是上面这些绝对
/// 字面量（字段集合或顺序被偷改即红），不是 <c>…_same_commit_…</c> 那格的同一性比较。
/// </para>
/// </summary>
public sealed class NarrativeCommitFactoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
    private const string Conversation = "room-wave25";
    private const string Batch = "batch-wave25";
    private const string WriterId = "writer-narrative-1";

    private const string Prose = "She set the brush down and answered him.";
    private const string ProseVariant = "She set the brush down and answered him, then kept painting.";
    private const string BoundQuote = "answered him";

    /// <summary>绝对字面量：只有 ①–⑨ 这九项按上面的顺序与分隔符参与，本夹具的基线摘要。</summary>
    private const string BaseHash = "99a0cc4bed9061763112b52bdb545df3d5457082887f757ae45100582c12eda4";

    private static readonly CharacterInstanceId Alpha = CharacterInstanceId.Create("ci-instance-wave25");
    private static readonly InputRef Input101 = new(101, "msg-101", Conversation);
    private static readonly ConservativeTokenEstimator Estimator = new();

    // —— 判据 1：绝对字面量（唯一能响的那扇门）——

    [Fact]
    public void BuildCommit_hashes_the_nine_inputs_into_the_absolute_digest()
    {
        var commit = NarrativeRoundRunner.BuildCommit(Draft(), Request(), Now, WriterId);

        Assert.Equal(BaseHash, commit.ContentHash);

        // 顺带钉住可见性提取没改的三条宿主事实：写者来自入参、时间是入参、提交号由 ProposalId 拼出。
        Assert.Equal(WriterId, commit.CommittedByWriter);
        Assert.Equal(Now, commit.CommittedAtUtc);
        Assert.Equal("commit-proposal-25", commit.CommitId);
        Assert.Equal(Alpha, commit.InstanceId);
        Assert.Equal(Conversation, commit.ConversationId);
        Assert.Equal(Batch, commit.BatchId);
        Assert.Equal(Prose, commit.NarrativeProse);
    }

    // —— 判据 2：三条区分性探针，各自钉自己的绝对字面量（只改一个输入 ⇒ 必然得到不同摘要）——

    [Theory]
    [InlineData("narrative-prose", "508fda3f3f4b3ac3ec6d17d3a8fab9fd992d130ca85f9da3623d28595c7f6e17")]
    [InlineData("action-mode", "8667f316fc4b04115cb13ab0a1ff948e3b176605bc26b6b7f680c0aede13144e")]
    [InlineData("processed-through", "ce3c9175ce887194f67576031754bdec249cd9ed8811e73286cba84b9b73a136")]
    public void BuildCommit_digest_moves_when_exactly_one_hash_input_moves(string shape, string expectedHash)
    {
        var draft = shape switch
        {
            // 只改第⑤项 NarrativeProse。
            "narrative-prose" => Draft(prose: ProseVariant),
            // 只改第⑧项里那条动作的 Mode（1＝Immediate → 2＝Deferred），ActionId 与其它成员不动。
            "action-mode" => Draft(actions: [ImmediateAction(mode: CommunicationMode.Deferred)]),
            // 只推进第⑥项消费水位；第⑦项感知引用序列照旧是 "101"，两者本就是两回事。
            _ => Draft(processedThrough: 102),
        };

        var commit = NarrativeRoundRunner.BuildCommit(draft, Request(), Now, WriterId);

        Assert.Equal(expectedHash, commit.ContentHash);
        Assert.NotEqual(BaseHash, commit.ContentHash);
    }

    // —— 判据 3：零行为漂移（只证明"提取没改语义"，不是覆盖证据，见类头与工单 §〇.6.4）——

    [Fact]
    public async Task BuildCommit_static_extraction_yields_the_same_commit_the_runner_registers()
    {
        var ownership = new OutboxOwnershipRegistry();
        var grant = ownership.TryAcquire(Alpha, EngineMode.NarrativeV1, WriterId, Now);
        Assert.True(grant.Accepted, grant.Detail);

        var ledger = new DeliveryLedger(ownership);
        var request = Request();
        var runner = new NarrativeRoundRunner(new OneShotNarrator(Draft()), ledger, WriterId, Estimator);

        var result = await runner.RunAsync(request, Payloads(), TimeSpan.FromMilliseconds(40), Now,
            TestContext.Current.CancellationToken);

        Assert.Equal(NarrativeRoundDisposition.Immediate, result.Disposition);
        Assert.NotNull(result.Draft);
        var direct = NarrativeRoundRunner.BuildCommit(result.Draft!, request, Now, WriterId);

        // 工厂直调的产出与账本里那三段（＝runner 自己那条正常路径注册进去的提交）逐字段对得上：
        Assert.Equal(BaseHash, direct.ContentHash);
        Assert.Equal(direct.CommitId, result.HistoryEntries[0].CommitId);
        Assert.Equal(direct.InstanceId, result.HistoryEntries[0].InstanceId);
        Assert.Equal(direct.ConversationId, result.HistoryEntries[0].ConversationId);
        Assert.Equal(direct.OwnershipEpoch, result.HistoryEntries[0].OwnershipEpoch);
        Assert.Equal(WriterId, direct.CommittedByWriter);

        // 段文本必须能由该提交的 NarrativeProse 按绑定偏移逐字还原（这是账本自己的尺子，本刀只是复述）。
        var binding = result.Actions.Single(action => action.Mode == CommunicationMode.Immediate).Binding!;
        var slice = direct.NarrativeProse.Substring(binding.ResolvedOffset!.Value, binding.QuotedText.Length);
        Assert.Equal(slice, string.Concat(result.HistoryEntries.Select(segment => segment.Text)));
    }

    private sealed class OneShotNarrator(NarrativeDraft draft) : INarrator
    {
        public Task<NarratorOutcome> ProposeAsync(NarratorCall call, CancellationToken cancellationToken) =>
            Task.FromResult(NarratorOutcome.Produced(draft));
    }

    // —— 夹具 ——

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

    private static CommunicationAction ImmediateAction(CommunicationMode mode = CommunicationMode.Immediate,
        int segments = 3) => new()
    {
        ActionId = "action-now", Mode = mode, TargetConversationId = Conversation,
        ProseQuote = BoundQuote, PlannedSegmentCount = mode == CommunicationMode.Immediate ? segments : 0,
    };

    private static NarrativeDraft Draft(IReadOnlyList<CommunicationAction>? actions = null,
        long processedThrough = 101, string prose = Prose) => new()
    {
        InstanceId = Alpha, ConversationId = Conversation, BatchId = Batch, NarrativeProse = prose,
        ProcessedThrough = new ProcessedThroughSeq(processedThrough),
        PerceivedInputRefs = new PerceivedInputRefs { Refs = [Input101] },
        Actions = actions ?? [ImmediateAction()],
        Intents = [],
        StoryVersionBase = 7, OwnershipEpoch = 1, DeletionEpoch = 0,
        ReceivedThroughSeqAtIssue = 100, ProposalId = "proposal-25", ProposedAtUtc = Now,
    };

    private static NarrativeContextPayloads Payloads() => new()
    {
        Identity = "Pry canon",
        OutputRules = "json rules",
        CurrentEvent = new NarrativeContextUnit("input:101", Conversation, "hello there"),
    };
}
