using System.Text;
using Pry.Core.Memory;
using Pry.Core.Prompting;

namespace Pry.Core.Narrative;

/// <summary>
/// HDS-003 新生成中心的上下文装配器（隔离原型）。
///
/// 段数与段名照盘上唯一装配器 <c>src/Pry.Core/Prompting/ContextBuilder.cs</c> 逐段对齐（报告 §一.2 的 U1–U8）：
/// <b>盘上是 8 个装配单元</b>，其中 7 个各带一个 <see cref="ContextBudget"/> 的按段预算字段，
/// 第 8 个（必需前奏）没有预算字段、也不允许被裁。v4 §6 说的"七段"是语义分区名，
/// 与这 8 个装配单元不是一套；本类以盘上为准取 8 段，并把 v4 分区名作为只读参考挂在段上。
///
/// 本类不接生产库、不接 API/Client、不改动旧装配器：它是 HDS-010 之前独立可测的新链形状。
/// </summary>
public sealed class NarrativeContextCompiler(ITokenEstimator? estimator = null)
{
    /// <summary>
    /// 每条消息在旧装配器里的固定框架开销（<c>ContextBuilder.MessageCost</c>＝<c>Count(content) + 8</c>，
    /// 见 <c>ContextBuilder.cs:195</c>）。这里照盘上取同一个数，不新造第二条尺度。
    /// </summary>
    public const int MessageFramingOverhead = 8;

    /// <summary>盘上装配单元数＝8（报告 §一.2 的 U1–U8）。</summary>
    public const int SegmentCount = 8;

    /// <summary>段名顺序与旧装配器的写入顺序一一对应。</summary>
    public static IReadOnlyList<string> SegmentNames { get; } =
    [
        "Identity", "Advisory", "State", "RecentMessages", "Memory",
        "BehaviorReferences", "ExpressionPatterns", "CapabilityResult",
    ];

    private readonly ITokenEstimator _tokens = estimator ?? new ConservativeTokenEstimator();

    /// <summary>
    /// 装配一次上下文。身份三元组一律取宿主签发的 <paramref name="request"/>，载荷只带内容与来源引用。
    /// 返回的装配体永远含 8 段（未提供的段以 <see cref="NarrativeSegmentDisposition.NotSupplied"/> 显式存在），
    /// 失败时 <see cref="NarrativeContextAssembly.Accepted"/> 为 false 并带确切理由——不抛、不吞、不静默少给。
    /// </summary>
    public NarrativeContextAssembly Compile(NarrativeRequest request, NarrativeContextPayloads payloads)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(payloads);
        request.Validate();

        var budget = payloads.Budget;
        var segments = new List<NarrativeContextSegment>(SegmentCount);

        // K1：预算字段本身非法（对齐 ContextBuilder.cs:107-109 的同一判据形状）。
        if (budget.ContextTokens < 1 || budget.OutputTokens < 0 || budget.SafetyTokens < 0
            || new[] { budget.RecentTokens, budget.MemoryTokens, budget.StateTokens, budget.BehaviorTokens,
                budget.ExpressionTokens, budget.CapabilityTokens, budget.AdvisoryTokens }.Any(x => x < 0))
        {
            return Fail(request, budget, segments, NarrativeCompileReason.InvalidBudget,
                "上下文预算字段非法：窗口至少 1，其余按段份额不得为负。");
        }

        // K2：统一预算不等式的左半边先立起来（对齐 ContextBuilder.cs:114-115）。
        var total = (long)budget.ContextTokens - budget.OutputTokens - budget.SafetyTokens;
        if (total <= 0)
        {
            return Fail(request, budget, segments, NarrativeCompileReason.WindowTooSmall,
                $"窗口 {budget.ContextTokens} 扣掉输出保留 {budget.OutputTokens} 与安全余量 "
                + $"{budget.SafetyTokens} 后没有正的输入预算。");
        }

        // U1 必需前奏：Identity + OutputRules + CurrentEvent，共用一次核算、一道门（对齐 :116-118）。
        // v4 §6 的那句"必需，不能截掉身份/事实边界来凑窗口"在这里落地：装不下就整轮不装配。
        if (string.IsNullOrWhiteSpace(payloads.Identity))
        {
            return Fail(request, budget, segments, NarrativeCompileReason.MandatoryContentMissing,
                "身份与设定缺失：没有身份边界就没有可装配的主叙事。");
        }

        if (string.IsNullOrWhiteSpace(payloads.CurrentEvent.Content)
            || !string.Equals(payloads.CurrentEvent.ConversationId, request.ConversationId, StringComparison.Ordinal))
        {
            return Fail(request, budget, segments, NarrativeCompileReason.MandatoryContentMissing,
                "当前事件缺失或落在宿主签发会话之外：写作窗口没有可锚定的事件。");
        }

        var prelude = payloads.Identity + "\n" + payloads.OutputRules;
        var preludeCost = _tokens.Count(prelude) + MessageFramingOverhead
            + _tokens.Count(payloads.CurrentEvent.Content) + MessageFramingOverhead;
        if (preludeCost > total)
        {
            return Fail(request, budget, segments, NarrativeCompileReason.MandatoryExceedsBudget,
                $"身份与当前事件的成本 {preludeCost} 超过输入预算 {total}。");
        }

        segments.Add(new NarrativeContextSegment
        {
            Name = "Identity", SourceRef = "src/Pry.Core/Prompting/ContextBuilder.cs:116-118",
            V4Partition = "storyIdentity + authoringWindow + incomingEvent（必需前奏，盘上三段合一）",
            Trim = NarrativeSegmentTrim.Never, Mandatory = true, BudgetTokens = 0,
            UsedTokens = preludeCost, Disposition = NarrativeSegmentDisposition.Included,
            SourceRefs = [payloads.IdentitySourceRef, payloads.CurrentEvent.SourceRef],
            EmittedText = prelude,
        });

        var used = preludeCost;

        // U2 Advisory：必需指令提前预留、逐字送达，装不下即明确失败（对齐 :132-143 与 :190）。
        if (!string.IsNullOrWhiteSpace(payloads.Advisory))
        {
            var addition = "\n" + payloads.Advisory;
            var cost = _tokens.Count(addition);
            if (cost > budget.AdvisoryTokens || used + cost > total)
            {
                segments.Add(new NarrativeContextAssemblySegmentFactory
                {
                    Name = "Advisory", SourceRef = "src/Pry.Core/Prompting/ContextBuilder.cs:132-143",
                    V4Partition = "ongoingThreads + availableNearFuture（本轮决策块，盘上走 Advisory 通道）",
                    Trim = NarrativeSegmentTrim.VerbatimOrReject, BudgetTokens = budget.AdvisoryTokens,
                    UsedTokens = cost, Disposition = NarrativeSegmentDisposition.Dropped,
                    DispositionReason = "reserved_directive_does_not_fit",
                    SourceRefs = [payloads.AdvisorySourceRef], EmittedText = "",
                }.ToSegment());
                return Fail(request, budget, segments, NarrativeCompileReason.ReservedDirectiveDoesNotFit,
                    $"决策块成本 {cost} 装不进预留份额 {budget.AdvisoryTokens}（或剩余输入预算 {total - used}）："
                    + "必需指令不截短、不静默丢弃。");
            }

            used += cost;
            segments.Add(new NarrativeContextAssemblySegmentFactory
            {
                Name = "Advisory", SourceRef = "src/Pry.Core/Prompting/ContextBuilder.cs:132-143",
                V4Partition = "ongoingThreads + availableNearFuture（本轮决策块，盘上走 Advisory 通道）",
                Trim = NarrativeSegmentTrim.VerbatimOrReject, BudgetTokens = budget.AdvisoryTokens,
                UsedTokens = cost, Disposition = NarrativeSegmentDisposition.Included,
                SourceRefs = [payloads.AdvisorySourceRef], EmittedText = addition,
            }.ToSegment());
        }
        else
        {
            segments.Add(NotSupplied("Advisory", "src/Pry.Core/Prompting/ContextBuilder.cs:132-143",
                NarrativeSegmentTrim.VerbatimOrReject, budget.AdvisoryTokens));
        }

        // U3 当前状态（对齐 :144）。
        used = AddSection(segments, request, "State", "src/Pry.Core/Prompting/ContextBuilder.cs:144",
            "currentSceneEvidence", payloads.State, payloads.StateSourceRef, budget.StateTokens, used, total);

        // U4 近期原文：按来源完整单元从最新往回收，预算见底即停（对齐 :145-155）。
        var recentRun = AddUnitRun(segments, request, "RecentMessages",
            "src/Pry.Core/Prompting/ContextBuilder.cs:145-155", "relevantEstablishedEpisodes",
            payloads.RecentMessages, budget.RecentTokens, used, total);
        used = recentRun.UsedAfter;

        // U5 相关记忆：同一条完整单元规则，钱包逐条递减（对齐 :157-185）。
        var memoryRun = AddUnitRun(segments, request, "Memory",
            "src/Pry.Core/Prompting/ContextBuilder.cs:157-185", "relevantEstablishedEpisodes",
            payloads.MemoryExcerpts, budget.MemoryTokens, used, total);
        used = memoryRun.UsedAfter;

        // U6/U7/U8 三段整段留或整段丢（对齐 :186-188）。
        used = AddSection(segments, request, "BehaviorReferences", "src/Pry.Core/Prompting/ContextBuilder.cs:186",
            "（v4 §6 无对应分区：旧链的行为参考）", payloads.BehaviorReferences, payloads.BehaviorSourceRef,
            budget.BehaviorTokens, used, total);
        used = AddSection(segments, request, "ExpressionPatterns", "src/Pry.Core/Prompting/ContextBuilder.cs:187",
            "（v4 §6 无对应分区：旧链的表达参考）", payloads.ExpressionPatterns, payloads.ExpressionSourceRef,
            budget.ExpressionTokens, used, total);
        used = AddSection(segments, request, "CapabilityResult", "src/Pry.Core/Prompting/ContextBuilder.cs:188",
            "（v4 §6 无对应分区：旧链的能力结果／附件观察）", payloads.CapabilityResult, payloads.CapabilitySourceRef,
            budget.CapabilityTokens, used, total);

        var assembly = new NarrativeContextAssembly
        {
            InstanceId = request.InstanceId, ConversationId = request.ConversationId, BatchId = request.BatchId,
            Trigger = request.Trigger, Segments = segments, ContextTokens = budget.ContextTokens,
            OutputReservationTokens = budget.OutputTokens, SafetyTokens = budget.SafetyTokens,
            InputBudgetTokens = (int)total, InputEstimateTokens = used,
        };

        // K3 自检：统一预算不等式必须由构造保证，构造不出来就是装配器的错，不能放行给模型。
        if (!assembly.WithinUnifiedBudget)
        {
            return assembly with
            {
                ReasonCode = NarrativeCompileReason.BudgetInequalityViolated,
                Detail = $"输入估算 {assembly.InputEstimateTokens} + 输出保留 {assembly.OutputReservationTokens} + 安全余量 "
                    + $"{assembly.SafetyTokens} 超出上下文 {assembly.ContextTokens}。",
            };
        }

        return assembly;
    }

    /// <summary>整段留或整段丢的一节（照 <c>ContextBuilder.cs:120-127</c> 的同一判据：成本超自身份额或超剩余即整段丢弃）。</summary>
    private int AddSection(List<NarrativeContextSegment> segments, NarrativeRequest request, string name,
        string sourceRef, string v4Partition, string content, string contentSourceRef, int cap, int used, long total)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            segments.Add(NotSupplied(name, sourceRef, NarrativeSegmentTrim.SectionOrDrop, cap));
            return used;
        }

        var addition = "\n" + name + "：\n" + content;
        var cost = _tokens.Count(addition);
        if (cost > cap || used + cost > total)
        {
            segments.Add(Simple(name, sourceRef, v4Partition, NarrativeSegmentTrim.SectionOrDrop, cap, cost,
                NarrativeSegmentDisposition.Dropped, "budget", contentSourceRef, ""));
            return used;
        }

        segments.Add(Simple(name, sourceRef, v4Partition, NarrativeSegmentTrim.SectionOrDrop, cap, cost,
            NarrativeSegmentDisposition.Included, null, contentSourceRef, addition));
        return used + cost;
    }

    /// <summary>
    /// 只能按"来源完整单元"取舍的一节：跨会话的单元整条剔除（对齐 <c>:148-149</c> 的 continue），
    /// 自身份额或剩余预算见底即停（对齐 <c>:152-153</c> 的 break），绝不截断半条原文。
    /// </summary>
    private (int UsedAfter, int Dropped, int CrossScope) AddUnitRun(List<NarrativeContextSegment> segments,
        NarrativeRequest request, string name, string sourceRef, string v4Partition,
        IReadOnlyList<NarrativeContextUnit> units, int cap, int used, long total)
    {
        if (units.Count == 0)
        {
            segments.Add(NotSupplied(name, sourceRef, NarrativeSegmentTrim.WholeUnitsOnly, cap));
            return (used, 0, 0);
        }

        var wallet = cap;
        var usedBefore = used;
        var included = 0;
        var crossScope = 0;
        var budgetDropped = 0;
        var emitted = new StringBuilder();
        var refs = new List<string>();

        foreach (var unit in units)
        {
            if (!string.Equals(unit.ConversationId, request.ConversationId, StringComparison.Ordinal))
            {
                crossScope++;
                continue;
            }

            var cost = _tokens.Count(unit.Content) + MessageFramingOverhead;
            if (wallet - cost < 0 || used + cost > total)
            {
                budgetDropped += units.Count - included - crossScope;
                break;
            }

            emitted.Append("\n- ").Append(unit.Content);
            refs.Add(unit.SourceRef);
            used += cost;
            wallet -= cost;
            included++;
        }

        var dropped = crossScope + budgetDropped;
        var disposition = included == 0
            ? NarrativeSegmentDisposition.Dropped
            : dropped > 0 ? NarrativeSegmentDisposition.PartiallyIncluded : NarrativeSegmentDisposition.Included;
        var reason = included == 0 ? "budget"
            : dropped > 0 ? (crossScope > 0 && budgetDropped == 0 ? "cross_scope"
                : crossScope == 0 && budgetDropped > 0 ? "unit_budget" : "cross_scope_and_unit_budget")
            : null;

        segments.Add(new NarrativeContextSegment
        {
            Name = name, SourceRef = sourceRef, V4Partition = v4Partition,
            Trim = NarrativeSegmentTrim.WholeUnitsOnly, Mandatory = false, BudgetTokens = cap,
            UsedTokens = used - usedBefore, Disposition = disposition, DroppedUnitCount = dropped,
            CrossScopeUnitCount = crossScope, BudgetDroppedUnitCount = budgetDropped,
            IncludedUnitCount = included, DispositionReason = reason,
            SourceRefs = refs, EmittedText = emitted.ToString(),
        });
        return (used, dropped, crossScope);
    }

    private static NarrativeContextSegment NotSupplied(string name, string sourceRef, NarrativeSegmentTrim trim, int cap) =>
        new()
        {
            Name = name, SourceRef = sourceRef, V4Partition = "", Trim = trim, Mandatory = false,
            BudgetTokens = cap, UsedTokens = 0, Disposition = NarrativeSegmentDisposition.NotSupplied,
            DispositionReason = "not_supplied",
        };

    private static NarrativeContextSegment Simple(string name, string sourceRef, string v4Partition,
        NarrativeSegmentTrim trim, int cap, int cost, NarrativeSegmentDisposition disposition, string? reason,
        string contentSourceRef, string emittedText) => new()
        {
            Name = name, SourceRef = sourceRef, V4Partition = v4Partition, Trim = trim, Mandatory = false,
            BudgetTokens = cap, UsedTokens = cost, Disposition = disposition, DispositionReason = reason,
            SourceRefs = [contentSourceRef], EmittedText = emittedText,
        };

    private static NarrativeContextAssembly Fail(NarrativeRequest request, ContextBudget budget,
        List<NarrativeContextSegment> segments, NarrativeCompileReason reason, string detail) => new()
        {
            InstanceId = request.InstanceId, ConversationId = request.ConversationId, BatchId = request.BatchId,
            Trigger = request.Trigger, Segments = segments, ContextTokens = budget.ContextTokens,
            OutputReservationTokens = budget.OutputTokens, SafetyTokens = budget.SafetyTokens,
            InputBudgetTokens = Math.Max(0, budget.ContextTokens - budget.OutputTokens - budget.SafetyTokens),
            InputEstimateTokens = 0, ReasonCode = reason, Detail = detail,
        };
}

/// <summary>段的裁剪策略：不同段"能怎么省"是三种不同的契约，不能共用一个布尔。</summary>
public enum NarrativeSegmentTrim
{
    /// <summary>必需前奏：不许截断、不许丢弃。</summary>
    Never,
    /// <summary>必需指令：逐字进上下文，装不下就整轮明确失败。</summary>
    VerbatimOrReject,
    /// <summary>只能整条来源单元取舍，不截半条。</summary>
    WholeUnitsOnly,
    /// <summary>整段留或整段丢。</summary>
    SectionOrDrop,
}

/// <summary>段最终进入了什么状态。<see cref="NotSupplied"/> 与 <see cref="Dropped"/> 是两回事：前者宿主没给料，后者给了但装不下。</summary>
public enum NarrativeSegmentDisposition
{
    Included,
    PartiallyIncluded,
    Dropped,
    NotSupplied,
}

/// <summary>装配与调用形状层自己的判定码（不动 <see cref="NarrativeContractReason"/> 的既有取值）。</summary>
public enum NarrativeCompileReason
{
    Ok = 0,
    InvalidBudget = 1,
    WindowTooSmall = 2,
    MandatoryContentMissing = 3,
    MandatoryExceedsBudget = 4,
    ReservedDirectiveDoesNotFit = 5,
    BudgetInequalityViolated = 6,

    /// <summary>同一次叙事的第二次调用：形状上不存在第二个生成器。</summary>
    SecondCallNotPermitted = 7,

    /// <summary>上下文装配体的身份与宿主请求的不是同一份。</summary>
    CallScopeMismatch = 8,
}

/// <summary>一条来源完整单元：来源引用＋它属于哪个会话＋原文。</summary>
public sealed record NarrativeContextUnit(string SourceRef, string ConversationId, string Content)
{
    public NarrativeContextUnit Validate()
    {
        NarrativeGuard.RequireText(SourceRef, nameof(SourceRef), NarrativeContractReason.PerceivedInputUnknown);
        NarrativeGuard.RequireText(ConversationId, nameof(ConversationId), NarrativeContractReason.IdentityValueBlank);
        return this;
    }
}

/// <summary>八段的载荷。身份三元组不在这里，一律取宿主签发的 <see cref="NarrativeRequest"/>。</summary>
public sealed record NarrativeContextPayloads
{
    public ContextBudget Budget { get; init; } = new();

    // U1 必需前奏的三个输入位。
    public required string Identity { get; init; }
    public required string OutputRules { get; init; }
    public required NarrativeContextUnit CurrentEvent { get; init; }

    // U2 决策块。
    public string Advisory { get; init; } = "";

    // U3 当前处境。
    public string State { get; init; } = "";

    // U4/U5 完整单元序列（调用方按"最新在前"给出，装配器从前往后收）。
    public IReadOnlyList<NarrativeContextUnit> RecentMessages { get; init; } = [];
    public IReadOnlyList<NarrativeContextUnit> MemoryExcerpts { get; init; } = [];

    // U6/U7/U8 整段参考。
    public string BehaviorReferences { get; init; } = "";
    public string ExpressionPatterns { get; init; } = "";
    public string CapabilityResult { get; init; } = "";

    // 各段的来源引用（"能指回输入引用"要求的不只是消息，段也要）。
    public string IdentitySourceRef { get; init; } = "identity:canon";
    public string AdvisorySourceRef { get; init; } = "advisory:block";
    public string StateSourceRef { get; init; } = "scene:current";
    public string BehaviorSourceRef { get; init; } = "intelligence:behavior";
    public string ExpressionSourceRef { get; init; } = "intelligence:expression";
    public string CapabilitySourceRef { get; init; } = "capability:result";
}

/// <summary>一段上下文的装配结果。字段就是工单 §二.1 要的三件：名字、来源引用、预算或截断策略。</summary>
public sealed record NarrativeContextSegment
{
    public required string Name { get; init; }
    public required string SourceRef { get; init; }
    public string V4Partition { get; init; } = "";
    public NarrativeSegmentTrim Trim { get; init; }
    public bool Mandatory { get; init; }
    public int BudgetTokens { get; init; }
    public int UsedTokens { get; init; }
    public NarrativeSegmentDisposition Disposition { get; init; }
    public string? DispositionReason { get; init; }
    public int DroppedUnitCount { get; init; }
    public int IncludedUnitCount { get; init; }
    public int CrossScopeUnitCount { get; init; }
    public int BudgetDroppedUnitCount { get; init; }
    public IReadOnlyList<string> SourceRefs { get; init; } = [];
    public string EmittedText { get; init; } = "";

    public bool IsPresent => Disposition is NarrativeSegmentDisposition.Included or NarrativeSegmentDisposition.PartiallyIncluded;
}

/// <summary>八段的装配体。它是一次主叙事唯一的上下文凭据。</summary>
public sealed record NarrativeContextAssembly
{
    public required CharacterInstanceId InstanceId { get; init; }
    public required string ConversationId { get; init; }
    public required string BatchId { get; init; }
    public required NarrativeTriggerKind Trigger { get; init; }
    public required IReadOnlyList<NarrativeContextSegment> Segments { get; init; }
    public required int ContextTokens { get; init; }
    public required int OutputReservationTokens { get; init; }
    public required int SafetyTokens { get; init; }
    public required int InputBudgetTokens { get; init; }
    public required int InputEstimateTokens { get; init; }
    public NarrativeCompileReason ReasonCode { get; init; } = NarrativeCompileReason.Ok;
    public string? Detail { get; init; }

    public bool Accepted => ReasonCode == NarrativeCompileReason.Ok;
    public int SegmentCount => Segments.Count;
    public int DroppedSegmentCount => Segments.Count(s => s.Disposition == NarrativeSegmentDisposition.Dropped);
    public int NotSuppliedSegmentCount => Segments.Count(s => s.Disposition == NarrativeSegmentDisposition.NotSupplied);

    /// <summary>段名序列，供"与盘上装配器逐段对齐"的判据使用。</summary>
    public IReadOnlyList<string> SegmentNames => [.. Segments.Select(s => s.Name)];

    /// <summary>装配体里出现过的全部来源引用（含被丢弃单元之外的实际进入者）。</summary>
    public IReadOnlyList<string> EmittedSourceRefs => [.. Segments.SelectMany(s => s.SourceRefs)];

    /// <summary>v4 §6 的统一预算不等式：输入估算 + 输出保留 + 安全余量 &lt;= 实际可用上下文。</summary>
    public bool WithinUnifiedBudget =>
        (long)InputEstimateTokens + OutputReservationTokens + SafetyTokens <= ContextTokens;
}

/// <summary>
/// 段构造的小工具：Advisory 那种"预留逐字"的段有两个分支（装得下/装不下），
/// 用 record 的 init 形状直接写会重复十个字段，这里收一次。它不是新的契约类型。
/// </summary>
internal sealed record NarrativeContextAssemblySegmentFactory
{
    public required string Name { get; init; }
    public required string SourceRef { get; init; }
    public string V4Partition { get; init; } = "";
    public NarrativeSegmentTrim Trim { get; init; }
    public int BudgetTokens { get; init; }
    public int UsedTokens { get; init; }
    public NarrativeSegmentDisposition Disposition { get; init; }
    public string? DispositionReason { get; init; }
    public IReadOnlyList<string> SourceRefs { get; init; } = [];
    public string EmittedText { get; init; } = "";

    public NarrativeContextSegment ToSegment() => new()
    {
        Name = Name, SourceRef = SourceRef, V4Partition = V4Partition, Trim = Trim, Mandatory = false,
        BudgetTokens = BudgetTokens, UsedTokens = UsedTokens, Disposition = Disposition,
        DispositionReason = DispositionReason, SourceRefs = SourceRefs, EmittedText = EmittedText,
    };
}
