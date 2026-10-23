using Pry.Core.Memory;
using Pry.Core.Narrative.Storage;

namespace Pry.Core.Narrative;

/// <summary>
/// 回合穿线的停段档：这一回合走到哪一段就不再往下走。
/// <see cref="Completed"/> 之外的每一档都对应链上一个既有的拒绝出口，
/// 本组合件只决定"停在哪一段、后面各段一律不执行"，不新增任何拒绝判定本身。
/// </summary>
public enum NarrativeTurnStop
{
    /// <summary>一至八段按顺序全部走完。</summary>
    Completed = 0,
    /// <summary>攒批段没能在给定批次标识下拿到封好的批次。</summary>
    BatchNotSealed = 1,
    /// <summary>同一批次已经领过唯一可发送真相：同一批绝不二次叙事。</summary>
    SendableTruthAlreadyClaimed = 2,
    /// <summary>编译段没通过既有装配器：一次模型调用都不发生。</summary>
    ContextRejected = 3,
    /// <summary>叙事与校验段没给出可提交的轮（提供方拒绝／契约拒绝／历史拒绝）。</summary>
    RoundRejected = 4,
    /// <summary>原子提交段被落库面拒收：表达换形与节奏都不执行。</summary>
    CommitRejected = 5,
}

/// <summary>
/// 一次回合的全部必填入参。批次封顶、节奏参数、超时时界、所有权代次、水位基线、
/// 表达信号与场景挡清单一律由宿主传入：本组合件不含任何数字门槛，也不含任何默认节奏／批量值
/// （登记为"待标定（押到标定轮）"的取值只存在于入参，不存在于源码）。
/// </summary>
public sealed record NarrativeTurnSpec
{
    // —— 段 1 攒批 ——
    public required NarrativeInputBuffer Buffer { get; init; }

    /// <summary>要领取的批次标识。批次标识由攒批件自身确定性推导，本件不复制它的推导式，只按名取批。</summary>
    public required string BatchId { get; init; }

    /// <summary>到达序列：与攒批入队同一顺序的输入引用（序号唯一，由宿主签发）。</summary>
    public required IReadOnlyList<InputRef> Inputs { get; init; }

    public required IReadOnlyList<DateTimeOffset> ArrivalTimes { get; init; }

    /// <summary>封批时点：先用既有的到期封批尝试，没到期则由调用方一侧的强制封批收口。</summary>
    public required DateTimeOffset BatchSealAttemptUtc { get; init; }

    // —— 请求组装（身份与基线全部走必填入参，不是本件自造）——
    public required CharacterInstanceId InstanceId { get; init; }
    public required long StoryVersion { get; init; }
    public required long OwnershipEpoch { get; init; }
    public required long BaselineProcessedThroughSeq { get; init; }
    public required DateTimeOffset IssuedAtUtc { get; init; }
    public required int MaxOutboundSegments { get; init; }
    public NarrativeTriggerKind Trigger { get; init; } = NarrativeTriggerKind.UserInput;

    // —— 段 2 分类 ＋ 段 3 打断 ——
    public required InterruptCue? Cue { get; init; }

    /// <summary>打断候选段（来自既有投递账本的当前真相形状）。空清单时打断件自己会按"无可处理"拒绝，本件不代拒。</summary>
    public IReadOnlyList<InterruptibleSegment> SegmentsUnderway { get; init; } = [];

    /// <summary>分类结论为"新话题"时宿主给出的可点名打断理由；给不出理由就不打断，本件不替宿主猜理由。</summary>
    public InterruptionReasonKind? InterruptionReasonWhenNewTopic { get; init; }

    /// <summary>作废未送达段时写进投递账本的可点名取消理由（账本要求取消必须带理由）。</summary>
    public string InterruptionCancelReason { get; init; } = "";

    // —— 段 4 编译 ＋ 段 5 叙事与校验 ——
    public required NarrativeContextPayloads Payloads { get; init; }
    public required TimeSpan NarratorTimeout { get; init; }
    public required DateTimeOffset Now { get; init; }

    // —— 段 6 原子提交 ——
    /// <summary>落库面：真实 SQLite（临时库文件由宿主创建与删除，本件不建库不删库）。为 null 时跳过提交段。</summary>
    public NarrativeSqliteStore? Store { get; init; }

    /// <summary>
    /// 草稿到提交的装配入口。既有盘上唯一的草稿→提交构造在 NarrativeRoundRunner 里且为私有
    /// （本刀禁改既有文件），所以由宿主侧给出映射委托：本件只把它产出的提交喂给落库面，不自行拼提交。
    /// </summary>
    public Func<NarrativeRoundResult, NarrativeRequest, NarrativeCommit>? CommitFactory { get; init; }

    // —— 段 7 表达换形 ＋ 段 8 节奏 ——
    public BehaviorCue? BehaviorCue { get; init; }
    public ReplyPacingInput? Pacing { get; init; }
    public DateTimeOffset? PacingStartUtc { get; init; }
}

/// <summary>
/// 一次回合的穿线轨迹：每一段的输出成员都在场，供判据核对"上一段的哪个成员喂给了下一段的哪个参数"。
/// 停段之后的字段一律保持默认空值——没执行过的段不许留下假装执行过的痕迹。
/// </summary>
public sealed record NarrativeTurnTrace
{
    public required NarrativeTurnStop StoppedAt { get; init; }
    public string? Detail { get; init; }

    // 段 1
    public IReadOnlyList<InputBufferAppendDecision> AppendDecisions { get; init; } = [];
    public NarratedInputBatch? Batch { get; init; }

    /// <summary>由批次成员组装出的叙事请求：批次的会话／批次标识／到达次序在这里喂给后续各段。</summary>
    public NarrativeRequest? AssembledRequest { get; init; }

    // 段 2
    public InterruptClass? Classification { get; init; }

    // 段 3
    public InterruptionOutcome? Interruption { get; init; }

    /// <summary>被打断段作废时既有投递账本逐段返回的结果（键与理由都在账本上）。</summary>
    public IReadOnlyList<SegmentOutcome> LedgerCancellations { get; init; } = [];

    // 段 4
    public NarrativeContextAssembly? Assembly { get; init; }

    // 段 5
    public NarrativeRoundResult? Round { get; init; }

    // 段 6
    public NarrativeCommit? BuiltCommit { get; init; }
    public ConditionalCommitOutcome? Commit { get; init; }

    /// <summary>提交生效后从落库面读回的待发段行（段 8 的字符数取自这里，不取自内存）。</summary>
    public IReadOnlyList<DeliverySegment>? OutboxReadback { get; init; }

    // 段 7
    public BehaviorExpressionOutcome? Expression { get; init; }

    // 段 8
    public IReadOnlyList<DateTimeOffset>? SendTimes { get; init; }
}

/// <summary>
/// 回合穿线组合件（HDS-WAVE23-1023 活单①）。只做编排：按 1 攒批 → 2 分类 → 3 打断 → 4 编译
/// → 5 叙事与校验 → 6 原子提交 → 7 表达换形 → 8 节奏 的顺序调用既有件，把上一段输出的
/// 具名成员喂给下一段入参；不实现任何被组合件自己的判定，不改任何被组合件，零新数字门槛。
/// 停段规则（哪一段拒绝后不再往下走）是编排选择，逐条登记在工单报告的衔接表里。
/// 本件不接生产宿主、不接任何端点或界面、不落生产库；盘上没有任何生产调用方（登记：未验证）。
/// 不引用旧主链的装配／渲染／规划／必要性四件（该断言由判据组的源码扫描格钉住，门带阳性探针）。
/// </summary>
public sealed class NarrativeTurnPipeline
{
    private readonly NarrativeContextCompiler _compiler;
    private readonly NarrativeRoundRunner _runner;
    private readonly DeliveryLedger _ledger;
    private readonly string _writerId;

    public NarrativeTurnPipeline(INarrator narrator, DeliveryLedger ledger, string writerId,
        ITokenEstimator? estimator = null)
    {
        ArgumentNullException.ThrowIfNull(narrator);
        ArgumentNullException.ThrowIfNull(ledger);
        NarrativeGuard.RequireText(writerId, nameof(writerId), NarrativeContractReason.OwnershipNotHeld);
        _compiler = new NarrativeContextCompiler(estimator);
        _runner = new NarrativeRoundRunner(narrator, ledger, writerId, estimator);
        _ledger = ledger;
        _writerId = writerId;
    }

    /// <summary>把一回合按八段顺序走完；任何一段按既有拒绝出口停下时，后续段一律不执行。</summary>
    public async Task<NarrativeTurnTrace> RunTurnAsync(NarrativeTurnSpec spec,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var trace = new NarrativeTurnTrace { StoppedAt = NarrativeTurnStop.Completed };

        // —— 段 1 攒批：逐条按到达次序并入，先试到期封批、没到期则强制封批，再按批次标识领取 ——
        var decisions = new List<InputBufferAppendDecision>(spec.Inputs.Count);
        for (var i = 0; i < spec.Inputs.Count; i++)
        {
            decisions.Add(spec.Buffer.Append(spec.Inputs[i].Seq, spec.ArrivalTimes[i]));
        }

        if (!spec.Buffer.TryCloseDueAt(spec.BatchSealAttemptUtc))
        {
            spec.Buffer.ForceClose();
        }

        var batch = spec.Buffer.FindSealed(spec.BatchId);
        trace = trace with
        {
            AppendDecisions = decisions,
            Batch = batch,
        };
        if (batch is null)
        {
            return trace with
            {
                StoppedAt = NarrativeTurnStop.BatchNotSealed,
                Detail = $"批次 {spec.BatchId} 没有封好的批次可领，回合不进入分类。",
            };
        }

        if (!spec.Buffer.TryTakeSendableTruth(batch.BatchId))
        {
            return trace with
            {
                StoppedAt = NarrativeTurnStop.SendableTruthAlreadyClaimed,
                Detail = $"批次 {batch.BatchId} 的唯一可发送真相已被领取：同一批次不二次叙事。",
            };
        }

        // 批次的三个成员（会话／批次标识／到达次序）逐字进请求；请求再喂给段 4 与段 5。
        var request = new NarrativeRequest
        {
            InstanceId = spec.InstanceId,
            ConversationId = batch.ConversationId,
            BatchId = batch.BatchId,
            Inputs = [.. batch.OrderedSeqs.Select(seq => spec.Inputs.Single(r => r.Seq == seq))],
            Trigger = spec.Trigger,
            StoryVersion = spec.StoryVersion,
            OwnershipEpoch = spec.OwnershipEpoch,
            IssuedByWriter = _writerId,
            MaxOutboundSegments = spec.MaxOutboundSegments,
            IssuedAtUtc = spec.IssuedAtUtc,
            ProcessedThroughAtIssue = new ProcessedThroughSeq(spec.BaselineProcessedThroughSeq),
        }.Validate();
        trace = trace with { AssembledRequest = request };

        // —— 段 2 分类：三档不折叠，不可判与接着说都不触发打断 ——
        var klass = NarrativeInterruptClassifier.Classify(spec.Cue);
        trace = trace with { Classification = klass };

        // —— 段 3 打断：分类结论为新话题才触发既有打断件；作废清单逐段交给既有投递账本落终态 ——
        InterruptionOutcome? interruption = null;
        var cancellations = new List<SegmentOutcome>();
        if (klass == InterruptClass.NewTopic && spec.InterruptionReasonWhenNewTopic is { } reason)
        {
            interruption = NarrativeInterruption.Apply(spec.SegmentsUnderway, reason);
            if (interruption.Result == InterruptionApplyResult.Applied)
            {
                foreach (var segment in interruption.Cancelled)
                {
                    cancellations.Add(_ledger.TryMarkCancelled(
                        new DeliverySegmentKey(segment.CommitId, segment.ActionId, segment.SegmentIndex),
                        spec.InterruptionCancelReason, spec.OwnershipEpoch, _writerId));
                }
            }
        }

        trace = trace with { Interruption = interruption, LedgerCancellations = cancellations };

        // —— 段 4 编译：装配器拒绝即停，一次模型调用都不发生 ——
        var assembly = _compiler.Compile(request, spec.Payloads);
        trace = trace with { Assembly = assembly };
        if (!assembly.Accepted)
        {
            return trace with
            {
                StoppedAt = NarrativeTurnStop.ContextRejected,
                Detail = assembly.Detail ?? assembly.ReasonCode.ToString(),
            };
        }

        // —— 段 5 叙事与校验：整轮交给既有编排者（一次调用＋草稿校验＋绑定），本件不复制其判定 ——
        var round = await _runner.RunAsync(request, spec.Payloads, spec.NarratorTimeout, spec.Now,
            cancellationToken);
        trace = trace with { Round = round };
        if (!round.Accepted)
        {
            return trace with
            {
                StoppedAt = NarrativeTurnStop.RoundRejected,
                Detail = round.Detail ?? round.Disposition.ToString(),
            };
        }

        // —— 段 6 原子提交：轮的五具成员（草稿＋绑定动作＋历史段＋请求）喂给落库面的条件写 ——
        NarrativeCommit? commit = null;
        ConditionalCommitOutcome? commitOutcome = null;
        IReadOnlyList<DeliverySegment>? readback = null;
        if (spec.Store is { } store)
        {
            commit = spec.CommitFactory?.Invoke(round, request)
                ?? throw new ArgumentNullException(nameof(spec),
                    "给了落库面却没给草稿到提交的装配入口：本组合件不自行拼提交。");
            commitOutcome = await store.TryCommitAtomicAsync(request, commit, round.HistoryEntries,
                cancellationToken);
            trace = trace with { BuiltCommit = commit, Commit = commitOutcome };
            if (!commitOutcome.Accepted)
            {
                return trace with
                {
                    StoppedAt = NarrativeTurnStop.CommitRejected,
                    Detail = commitOutcome.Detail ?? commitOutcome.Reason.ToString(),
                };
            }

            readback = await store.ListPendingSegmentsAsync(commit.CommitId, cancellationToken);
        }

        trace = trace with { OutboxReadback = readback };

        // —— 段 7 表达换形：缺决策不默认放行、被场景挡理由码原样透传 ——
        var expression = NarrativeBehaviorExpressionMapping.Map(spec.BehaviorCue);
        trace = trace with { Expression = expression };

        // —— 段 8 节奏：只有换形放行才排可发时刻；字符数取自段 6 读回的落库行 ——
        IReadOnlyList<DateTimeOffset>? sendTimes = null;
        if (expression.Result == BehaviorMappingResult.Mapped && spec.Pacing is { } pacing
            && spec.PacingStartUtc is { } startAt)
        {
            var counts = (readback ?? round.HistoryEntries).Select(s => s.Text.Length).ToArray();
            sendTimes = NarrativeReplyPacing.PlanSendTimes(startAt, counts, pacing);
        }

        return trace with { SendTimes = sendTimes };
    }
}
