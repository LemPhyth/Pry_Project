using Pry.Core.Intelligence;

namespace Pry.Core.Runtime;

/// <summary>
/// 过渡期建议器：它刻意保持宽松——不替 Intelligence 做社交取舍，
/// 只把"这轮该做什么"的意图按输入形态机械映射，并把是否沉默交给生成端（空计划即 DoNothing）。
/// 追踪里会带上本类名字，便于 Developer Mode 看出这一轮并没有真正的行为决策。
/// 发行链路已不再使用它：宿主在 <c>ConversationSessionService</c> 里换成适配到
/// <see cref="Pry.Core.Abstractions.ICharacterIntelligence"/> 的建议器。这里保留下来是因为
/// "行为决策不可用时也要能把话说完"仍然是真实分支，宿主在决策抛错时按同样的形状回落。
/// </summary>
public sealed class TransitionalReplyAdvisor : IReplyAdvisor
{
    public string Name => "transitional_mechanical";

    public Task<ReplyAdvice> AdviseAsync(ReplyGateContext context, CancellationToken cancellationToken) =>
        Task.FromResult(new ReplyAdvice(true, SuggestIntent(context), "transitional_no_social_judgement"));

    private static ResponseIntent SuggestIntent(ReplyGateContext context) => context switch
    {
        { AsksQuestion: true } => ResponseIntent.Ask,
        { LooksIncomplete: true } => ResponseIntent.Clarify,
        { ProactiveOrigin: true } => ResponseIntent.ContinueTopic,
        { DirectAddress: true } => ResponseIntent.ContinueTopic,
        _ => ResponseIntent.Acknowledge
    };
}

/// <summary>
/// 沉默建议器：任何一轮都返回 DoNothing。用于宿主显式关闭自动回复的场景
/// （例如只读会话或维护模式），而不是把"不回复"实现成一条假消息。
/// </summary>
public sealed class SilentReplyAdvisor : IReplyAdvisor
{
    public string Name => "silent";

    public Task<ReplyAdvice> AdviseAsync(ReplyGateContext context, CancellationToken cancellationToken) =>
        Task.FromResult(new ReplyAdvice(false, ResponseIntent.DoNothing, "silenced_by_host"));
}
