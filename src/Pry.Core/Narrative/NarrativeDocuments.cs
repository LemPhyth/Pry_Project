namespace Pry.Core.Narrative;

/// <summary>契约版本常量。这些数字是"文档形状"的版本，不是 SQLite 的 schema 版本号，
/// 也不与任何表名绑定；预占表版本是 HDS-001/005 的落点，本波刻意不碰。</summary>
public static class NarrativeContractVersions
{
    /// <summary>User × Character → 稳定实例的映射契约版本。</summary>
    public const long IdentityMapping = 1;

    /// <summary>叙事文档（Request/Draft/Commit）的契约版本。</summary>
    public const long NarrativeDocument = 1;

    /// <summary>投递段与 outbox 的契约版本。</summary>
    public const long DeliveryContract = 1;

    /// <summary>实例写入权与引擎模式交接的契约版本。</summary>
    public const long OutboxOwnership = 1;

    /// <summary>实例合法的叙事契约版本集合。后续加版本必须同时加对应迁移说明。</summary>
    public static bool IsSupportedNarrativeVersion(long version) => version == NarrativeDocument;
}

/// <summary>
/// 宿主签发的一条输入引用。<see cref="Seq"/> 是实例内单调序号，由宿主赋值；
/// 模型或客户端自报的序号一律不采信。
/// </summary>
public sealed record InputRef(long Seq, string MessageId, string ConversationId)
{
    public InputRef Validate()
    {
        if (Seq < 1)
        {
            throw new NarrativeContractException("输入序号必须从 1 开始。", NarrativeContractReason.PerceivedInputUnknown);
        }

        NarrativeGuard.RequireText(MessageId, nameof(MessageId), NarrativeContractReason.PerceivedInputUnknown);
        NarrativeGuard.RequireText(ConversationId, nameof(ConversationId), NarrativeContractReason.PerceivedInputUnknown);
        return this;
    }
}

/// <summary>
/// 消费水位：宿主已经成功处理到的批次前缀（HDS-002 核心区分之一）。
/// 它是宿主的账，只随有效提交前进，与"角色注意到什么"没有任何推导关系。
/// </summary>
public sealed record ProcessedThroughSeq(long Seq)
{
    public static ProcessedThroughSeq Nothing { get; } = new(0);

    public ProcessedThroughSeq Validate()
    {
        if (Seq < 0)
        {
            throw new NarrativeContractException("消费水位不能为负。", NarrativeContractReason.WatermarkNotAdvancing);
        }

        return this;
    }

    /// <summary>批次上界：水位最多报到本批次最后一条输入，越过即凭空消费。</summary>
    public bool WithinBatch(long batchHighestSeq) => Seq <= batchHighestSeq;

    /// <summary>水位只能前进；相等也算拒绝，因为它意味着重复消费同一前缀。</summary>
    public bool AdvancesPast(ProcessedThroughSeq previous) => Seq > previous.Seq;

    public override string ToString() => Seq.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>
/// 感知引用：这一条叙事感知到了哪些输入（HDS-002 核心区分之二）。
/// 它与 <see cref="ProcessedThroughSeq"/> 是两个独立类型，就是为了让人无法把一个直接赋给另一个：
/// 沉默的合法提交可以有水位而无感知；越界感知不能抬高水位，水位也不能"顺带确认"感知。
/// </summary>
public sealed record PerceivedInputRefs
{
    public required IReadOnlyList<InputRef> Refs { get; init; }

    public IReadOnlySet<long> Seqs => new HashSet<long>(Refs.Select(r => r.Seq));

    public static PerceivedInputRefs None { get; } = new() { Refs = [] };

    public bool IsEmpty => Refs.Count == 0;

    /// <summary>引用的序号必须全部落在宿主签发的批次里；模型不能制造批次外的序号。</summary>
    public NarrativeContractReason CheckSubsetOf(IReadOnlyCollection<long> batchSeqs)
    {
        var batch = new HashSet<long>(batchSeqs);
        return Refs.All(r => batch.Contains(r.Seq)) ? NarrativeContractReason.None : NarrativeContractReason.PerceivedInputUnknown;
    }
}

/// <summary>叙事可以采用的事件种类。三类都走同一个入口，不存在第二条后台回复生成器。</summary>
public enum NarrativeTriggerKind
{
    UserInput = 0,
    IntentDue = 1,
    SceneAdvance = 2,
}

/// <summary>通信模式。沉默与延后是合法产物，不是失败。</summary>
public enum CommunicationMode
{
    /// <summary>合法无外发：本次叙事不产生任何要发送的消息。</summary>
    Silence = 0,
    /// <summary>即时正文，必须绑定当前叙事原文。</summary>
    Immediate = 1,
    /// <summary>延后：只落意图，不进真实历史。</summary>
    Deferred = 2,
}

/// <summary>投递段的账本状态。Delivered 与 Cancelled 是终态。</summary>
public enum DeliverySegmentState
{
    Pending = 0,
    Delivered = 1,
    Cancelled = 2,
    Unknown = 3,
}

/// <summary>意图状态。到期不等于已经发生，兑现必须由真实投递记录支撑。</summary>
public enum NarrativeIntentStatus
{
    Pending = 0,
    Fulfilled = 1,
    Cancelled = 2,
    Expired = 3,
    Superseded = 4,
}

/// <summary>认知类型：v4 §5 的四类资料在这里显式化，防止把虚构当现实。</summary>
public enum CognitionKind
{
    /// <summary>真实用户说过的事实（是报告，不是宿主观察）。</summary>
    UserReportedFact = 0,
    /// <summary>角色在虚构场景中的经历。</summary>
    CharacterFiction = 1,
    /// <summary>角色的信念或推测。</summary>
    CharacterBelief = 2,
    /// <summary>尚未发生的提议。</summary>
    Proposal = 3,
    /// <summary>已由真实投递或显式反馈支撑的约定。</summary>
    ConfirmedAgreement = 4,
    /// <summary>叙事原文本身，只作导航材料。</summary>
    NarrativeProse = 5,
}

/// <summary>认知候选的来源。来源决定它能支撑哪种断言，见 <see cref="KnowledgeCandidatePolicy"/>。</summary>
public enum CognitionOrigin
{
    UserMessageEvidence = 0,
    HostObservation = 1,
    ExplicitFeedback = 2,
    NarrativeProse = 3,
    ModelSelfReport = 4,
    Intent = 5,
}

/// <summary>
/// 宿主签发的叙事请求：一次主叙事的完整资格凭据。身份、会话、批次、版本、时间窗与上限
/// 都在这里定死，模型侧只能在它划出的范围内提提案。
/// </summary>
public sealed record NarrativeRequest
{
    public required CharacterInstanceId InstanceId { get; init; }
    public required string ConversationId { get; init; }
    public required string BatchId { get; init; }
    public required IReadOnlyList<InputRef> Inputs { get; init; }
    public required NarrativeTriggerKind Trigger { get; init; }

    /// <summary>签发时的实例故事版本，提交的乐观条件基线。</summary>
    public long StoryVersion { get; init; }

    /// <summary>删除 epoch：会话或数据被删除后提升，迟到结果据此永久失去资格。</summary>
    public long DeletionEpoch { get; init; }

    /// <summary>宿主所有权代次（fencing token）。进程重启或接管后递增。</summary>
    public long OwnershipEpoch { get; init; }

    /// <summary>签发本次请求的写者标识，必须与持有写入权的票一致。</summary>
    public required string IssuedByWriter { get; init; }

    /// <summary>叙事契约版本。</summary>
    public long ContractVersion { get; init; } = NarrativeContractVersions.NarrativeDocument;

    /// <summary>实例叙事作用域的最大外发段数（宿主资源与体验上限，不是模型可自报的量）。</summary>
    public int MaxOutboundSegments { get; init; } = 4;

    /// <summary>到期推进与场景续接可以没有输入；带输入的回合必须显式声明这一点。</summary>
    public bool RequiresInput { get; init; } = true;

    public DateTimeOffset IssuedAtUtc { get; init; }

    /// <summary>签发时刻的消费基线（宿主已处理到的前缀）。</summary>
    public ProcessedThroughSeq ProcessedThroughAtIssue { get; init; } = ProcessedThroughSeq.Nothing;

    public IReadOnlySet<long> InputSeqs => new HashSet<long>(Inputs.Select(i => i.Seq));

    /// <summary>批次上界；无输入的推进批次为 0，意味着这一轮不消费任何输入。</summary>
    public long BatchHighestSeq => Inputs.Count == 0 ? 0 : Inputs.Max(i => i.Seq);

    public NarrativeRequest Validate()
    {
        NarrativeGuard.RequireText(ConversationId, nameof(ConversationId), NarrativeContractReason.IdentityValueBlank);
        NarrativeGuard.RequireText(BatchId, nameof(BatchId), NarrativeContractReason.BatchEmpty);
        NarrativeGuard.RequireText(IssuedByWriter, nameof(IssuedByWriter), NarrativeContractReason.OwnershipNotHeld);
        ArgumentNullException.ThrowIfNull(Inputs);
        foreach (var input in Inputs)
        {
            input.Validate();
        }

        if (RequiresInput && Inputs.Count == 0)
        {
            throw new NarrativeContractException("需要输入的叙事请求不能是空批次。", NarrativeContractReason.BatchEmpty);
        }

        if (Inputs.Select(i => i.Seq).Distinct().Count() != Inputs.Count)
        {
            throw new NarrativeContractException("批次里出现重复序号：宿主赋号必须严格单调。",
                NarrativeContractReason.ProcessedBeyondBatch);
        }

        if (MaxOutboundSegments < 1)
        {
            throw new NarrativeContractException("外发段数上限至少为 1。", NarrativeContractReason.ActionSegmentCountInvalid);
        }

        if (!NarrativeContractVersions.IsSupportedNarrativeVersion(ContractVersion))
        {
            throw new NarrativeContractException($"不支持的叙事契约版本：{ContractVersion}。",
                NarrativeContractReason.ContractVersionUnsupported);
        }

        ProcessedThroughAtIssue.Validate();
        return this;
    }
}

/// <summary>
/// 即时正文与叙事原文的绑定：模型只给字面引用，宿主算位置与散列。
/// 绑定缺失或歧义时拒绝该动作，绝不由宿主补一条"合理回复"。
/// </summary>
public sealed record ProseBinding
{
    /// <summary>模型给出的字面片段，必须在原文中唯一出现。</summary>
    public required string QuotedText { get; init; }

    /// <summary>宿主解析出的唯一偏移；绑定失败时为 null。</summary>
    public int? ResolvedOffset { get; init; }

    public NarrativeContractReason Failure { get; init; }

    public bool Bound => Failure == NarrativeContractReason.None && ResolvedOffset.HasValue;
}

/// <summary>
/// 一条通信动作。它是"角色决定怎么说"，不等于实际发言；只有投递段落进真实历史。
/// </summary>
public sealed record CommunicationAction
{
    public required string ActionId { get; init; }
    public required CommunicationMode Mode { get; init; }

    /// <summary>目标会话。由宿主从请求继承，模型自报的一律视为越权。</summary>
    public required string TargetConversationId { get; init; }

    /// <summary>即时正文的字面引用；沉默与延后必须为 null。</summary>
    public string? ProseQuote { get; init; }

    /// <summary>本动作计划分段数量。沉默必须为 0。</summary>
    public int PlannedSegmentCount { get; init; }

    /// <summary>超过此刻动作失效（延后意图与主动许可的时间边界）。</summary>
    public DateTimeOffset? ExpiresAtUtc { get; init; }

    /// <summary>延后模式指向的意图；即时与沉默必须为 null。</summary>
    public string? DeferredIntentId { get; init; }

    /// <summary>宿主解析出的绑定结果；提案自己不能填。</summary>
    public ProseBinding? Binding { get; init; }
}

/// <summary>未来意图：有来源的待处理动机，不是已经发生的事实。</summary>
public sealed record NarrativeIntent
{
    public required string IntentId { get; init; }
    public required CharacterInstanceId InstanceId { get; init; }
    public required string Topic { get; init; }
    public required NarrativeIntentStatus Status { get; init; }

    /// <summary>提出该意图的叙事提交；没有来源的意图不予登记。</summary>
    public required string SourceCommitId { get; init; }

    public DateTimeOffset? EarliestDueAtUtc { get; init; }
    public DateTimeOffset? LatestDueAtUtc { get; init; }

    /// <summary>到期后是否允许主动外发。与"允许后台推进叙事"是两个独立许可。</summary>
    public bool ProactiveSendPermitted { get; init; }
}

/// <summary>知识候选：引用存在不等于语义为真，见 <see cref="KnowledgeCandidatePolicy"/>。</summary>
public sealed record KnowledgeCandidate
{
    public required string CandidateId { get; init; }
    public required CharacterInstanceId InstanceId { get; init; }
    public required CognitionKind Kind { get; init; }
    public required CognitionOrigin Origin { get; init; }
    public required string Claim { get; init; }
    public double Confidence { get; init; } = .5;

    /// <summary>支撑该候选的证据引用（输入序号、原文位置等）。</summary>
    public IReadOnlyList<string> EvidenceRefs { get; init; } = [];

    /// <summary>提出者。模型自评永远不能是自己断言的证据。</summary>
    public required string ProposedBy { get; init; }
}

/// <summary>
/// 模型的一次主叙事提案。它只能引用宿主在 <see cref="NarrativeRequest"/> 里签发的身份与范围；
/// 任何自扩大的范围（换实例、换会话、凭空造序号）都会在 <see cref="NarrativeDraftValidator"/> 被拒。
/// </summary>
public sealed record NarrativeDraft
{
    public required CharacterInstanceId InstanceId { get; init; }
    public required string ConversationId { get; init; }
    public required string BatchId { get; init; }
    public required string NarrativeProse { get; init; }

    /// <summary>本提案认领的消费水位。只表示"我处理到这里"，由宿主在提交时核验。</summary>
    public ProcessedThroughSeq ProcessedThrough { get; init; } = ProcessedThroughSeq.Nothing;

    /// <summary>本提案感知到的输入。与消费水位是两回事，类型不同、判定不同。</summary>
    public required PerceivedInputRefs PerceivedInputRefs { get; init; }

    public required IReadOnlyList<CommunicationAction> Actions { get; init; }
    public IReadOnlyList<NarrativeIntent> Intents { get; init; } = [];
    public IReadOnlyList<KnowledgeCandidate> KnowledgeCandidates { get; init; } = [];

    /// <summary>提案针对的故事版本；提交时必须与签发请求一致。</summary>
    public long StoryVersionBase { get; init; }

    public long OwnershipEpoch { get; init; }
    public long DeletionEpoch { get; init; }
    public long ReceivedThroughSeqAtIssue { get; init; }
    public required string ProposalId { get; init; }
    public DateTimeOffset ProposedAtUtc { get; init; }
}

/// <summary>
/// 经校验、绑定宿主身份与基线版本后的唯一叙事提交。带契约版本与内容散列；
/// 它是"已经发生的一段角色叙事"，不是待发文本。
/// </summary>
public sealed record NarrativeCommit
{
    public required string CommitId { get; init; }
    public required CharacterInstanceId InstanceId { get; init; }
    public required string ConversationId { get; init; }
    public required string BatchId { get; init; }
    public required string NarrativeProse { get; init; }
    public required IReadOnlyList<CommunicationAction> Actions { get; init; }
    public IReadOnlyList<NarrativeIntent> Intents { get; init; } = [];
    public IReadOnlyList<KnowledgeCandidate> KnowledgeCandidates { get; init; } = [];

    public required ProcessedThroughSeq ProcessedThrough { get; init; }
    public required PerceivedInputRefs PerceivedInputRefs { get; init; }

    /// <summary>乐观条件：提交必须建立在它看到的基线故事版本上。</summary>
    public required long ExpectedStoryVersion { get; init; }

    public long DeletionEpoch { get; init; }
    public required long OwnershipEpoch { get; init; }
    public long ContractVersion { get; init; } = NarrativeContractVersions.NarrativeDocument;

    /// <summary>内容散列由宿主计算，Core 只要求它存在且稳定；缺失即拒绝。</summary>
    public string? ContentHash { get; init; }

    public required string CommittedByWriter { get; init; }
    public required DateTimeOffset CommittedAtUtc { get; init; }

    /// <summary>来自哪条提案：提交与提案一一对应，避免另一个提案的正文混进来。</summary>
    public required string ProposalId { get; init; }

    /// <summary>本次提交实际消费掉的输入序号，由消费水位与批次共同决定，不由感知引用给出。</summary>
    public IReadOnlyList<long> ConsumedInputSeqs { get; init; } = [];
}

/// <summary>
/// 投递段：唯一键 <c>(commitId, actionId, segmentIndex)</c>。
/// 它是"实际送达的证据"，与叙事原文分账；叙事存在不推导投递发生，投递失败也不改写叙事。
/// </summary>
public sealed record DeliverySegment
{
    public required CharacterInstanceId InstanceId { get; init; }
    public required string ConversationId { get; init; }
    public required string CommitId { get; init; }
    public required string ActionId { get; init; }
    public required int SegmentIndex { get; init; }
    public required DeliverySegmentState State { get; init; }

    /// <summary>正文必须由该动作绑定的叙事切片逐字取得，不是第二次改写。</summary>
    public required string Text { get; init; }

    /// <summary>贴纸类段允许正文为空，但必须给出贴纸标识；两者都空即非法。</summary>
    public string? StickerId { get; init; }

    /// <summary>产生该段的写者与其所有权代次；代次落后即无权写终态。</summary>
    public required long OwnershipEpoch { get; init; }

    /// <summary>取消原因；只在 Cancelled 状态下有意义。</summary>
    public string? CancelReason { get; init; }

    /// <summary>实际落到聊天历史后的历史标识，由存储层回填；Core 契约里它是事后证据字段。</summary>
    public string? DeliveredMessageId { get; init; }

    public DeliverySegmentKey Key => new(CommitId, ActionId, SegmentIndex);

    public bool IsTerminal => State is DeliverySegmentState.Delivered or DeliverySegmentState.Cancelled;
}

/// <summary>投递段的唯一键。相等判定就是它自己：三个分量都相同即冲突。</summary>
public readonly record struct DeliverySegmentKey(string CommitId, string ActionId, int SegmentIndex);

internal static class NarrativeGuard
{
    public static void RequireText(string? value, string field, NarrativeContractReason reason)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new NarrativeContractException($"{field} 不能为空。", reason);
        }

        if (value.Length > NarrativeBounds.MaxIdentifierLength)
        {
            throw new NarrativeContractException($"{field} 超过 {NarrativeBounds.MaxIdentifierLength} 个字符。", reason);
        }
    }
}
