namespace Pry.Core.Narrative;

/// <summary>
/// 一次主叙事调用的结果形状。四类之一，互斥且必须点明是哪一类：
/// <see cref="DraftProduced"/> 才可能进契约校验；<see cref="MalformedResponse"/> 与
/// <see cref="TimedOut"/>、<see cref="ProviderFailed"/> 都是宿主侧的明确错误，
/// 绝不允许被解释成"角色选择了沉默"（v4 §7.1 第四行）。
/// </summary>
public enum NarratorOutcomeKind
{
    DraftProduced = 0,
    MalformedResponse = 1,
    TimedOut = 2,
    ProviderFailed = 3,
}

/// <summary>调用的凭据载荷：宿主签发的请求＋唯一的上下文装配体＋这一轮的调用额度。</summary>
public sealed record NarratorCall
{
    public required NarrativeRequest Request { get; init; }
    public required NarrativeContextAssembly Assembly { get; init; }

    /// <summary>本轮唯一允许的调用额度。第二次索要即被拒（工单 §二.2 的形状判据）。</summary>
    public required NarratorCallBudget Budget { get; init; }

    /// <summary>
    /// 喂进模型的必须是"宿主这一份请求刚装配出来的那一份上下文"：
    /// 实例、会话、批次任一项对不上，就是把别的回合或别的角色的上下文混进本次叙事。
    /// </summary>
    public NarratorCall Validate()
    {
        ArgumentNullException.ThrowIfNull(Request);
        ArgumentNullException.ThrowIfNull(Assembly);
        ArgumentNullException.ThrowIfNull(Budget);

        if (!Assembly.Accepted)
        {
            throw new NarrativeContextAssemblyRejectedException(
                $"上下文装配体已经带着拒绝理由 {Assembly.ReasonCode}，不允许喂给 Narrator。",
                Assembly.ReasonCode);
        }

        if (!Assembly.InstanceId.Equals(Request.InstanceId))
        {
            throw new NarrativeContextAssemblyRejectedException(
                "上下文装配体的实例与宿主请求的不是同一个：作用域不能靠装配体自己扩大。",
                NarrativeCompileReason.CallScopeMismatch);
        }

        if (!string.Equals(Assembly.ConversationId, Request.ConversationId, StringComparison.Ordinal))
        {
            throw new NarrativeContextAssemblyRejectedException(
                "上下文装配体的会话与宿主请求的不是同一个：跨会话上下文会串短期记忆。",
                NarrativeCompileReason.CallScopeMismatch);
        }

        if (!string.Equals(Assembly.BatchId, Request.BatchId, StringComparison.Ordinal))
        {
            throw new NarrativeContextAssemblyRejectedException(
                "上下文装配体的批次与宿主请求的不是同一个：本轮不能消费别的批次的料。",
                NarrativeCompileReason.CallScopeMismatch);
        }

        return this;
    }
}

/// <summary>一次调用的产物。<see cref="Draft"/> 只在 <see cref="Kind"/> 为 DraftProduced 时有值。</summary>
public sealed record NarratorOutcome(NarratorOutcomeKind Kind, NarrativeDraft? Draft, string? RawPayload, string? Detail)
{
    public static NarratorOutcome Produced(NarrativeDraft draft) => new(NarratorOutcomeKind.DraftProduced, draft, null, null);

    public static NarratorOutcome Malformed(string rawPayload, string detail) =>
        new(NarratorOutcomeKind.MalformedResponse, null, rawPayload, detail);

    public static NarratorOutcome Timeout(string detail) => new(NarratorOutcomeKind.TimedOut, null, null, detail);

    public static NarratorOutcome Failed(string detail) => new(NarratorOutcomeKind.ProviderFailed, null, null, detail);

    public bool IsUsable => Kind == NarratorOutcomeKind.DraftProduced && Draft is not null;
}

/// <summary>
/// 唯一的模型侧端口。生产者由宿主注入：本波的隔离原型只接确定性假实现，
/// 真实本地小模型与 API 的对照不在本波（工单 §〇.4 第 3 条），一律登记为未验证。
/// </summary>
public interface INarrator
{
    Task<NarratorOutcome> ProposeAsync(NarratorCall call, CancellationToken cancellationToken = default);
}

/// <summary>
/// 一次叙事的调用额度。它是"同一次叙事只允许一次调用"这条形状的强制点：
/// 上下文装配器产出的那一份上下文只能喂一次，之后的第二次独立改写、第二个后台生成器都拿不到额度。
/// </summary>
public sealed class NarratorCallBudget
{
    private int _calls;

    /// <summary>本轮已经用掉的调用次数。</summary>
    public int CallCount => Volatile.Read(ref _calls);

    /// <summary>索要一次调用。第二次索要抛异常并且不把额度交出去。</summary>
    public void Consume()
    {
        var next = Interlocked.Increment(ref _calls);
        if (next != 1)
        {
            throw new NarrativeContextAssemblyRejectedException(
                $"同一次叙事只允许一次主叙事调用，这是第 {next} 次：第二次独立改写或第二个后台生成器都不存在。",
                NarrativeCompileReason.SecondCallNotPermitted);
        }
    }
}

/// <summary>
/// 装配体/调用额度层面的拒绝。带 <see cref="NarrativeCompileReason"/> 作为稳定分类，
/// 消息只给人看——与 <see cref="NarrativeContractException"/> 的分工一致。
/// </summary>
public sealed class NarrativeContextAssemblyRejectedException(string message, NarrativeCompileReason reason)
    : InvalidOperationException(message)
{
    public NarrativeCompileReason Reason { get; } = reason;
}
