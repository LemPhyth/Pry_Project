namespace Pry.Core.Runtime;

/// <summary>Reply Gate 判断所需的状态类事实。IsCurrentTurn 由 TurnIdentity 与失效 epoch 比较得出。</summary>
public sealed record ReplyGateInputs(TurnIdentity Turn, bool IsCurrentTurn, bool UserTyping, bool BufferHasNewInput,
    bool CapabilityPending, string CombinedInput, IReadOnlyList<string> CharacterNames,
    DateTimeOffset? LastCharacterActivity, TimeSpan SinceLastCharacterActivity, int ConsecutiveCharacterTurns,
    RuntimePhase Phase, int PendingInputCount, int PendingOutputCount, bool ProactiveOrigin = false);

/// <summary>
/// Reply Gate：Pry 这次要不要说话。Runtime 只在这里放置状态闸门——
/// 本轮是否已失效、是否仍在输入、是否有更新的事件、是否仍有排队输出、是否在等 Capability。
/// 真正的社交取舍由 <see cref="IReplyAdvisor"/>（宿主适配到 ICharacterIntelligence）提供，
/// DoNothing 与 Defer 都是一等结果，不是失败路径。
/// </summary>
public sealed class ReplyGate(IReplyAdvisor advisor)
{
    public string AdvisorName => advisor.Name;

    public async Task<ReplyGateResult> DecideAsync(ReplyGateInputs inputs, CancellationToken cancellationToken)
    {
        if (!inputs.IsCurrentTurn) return ReplyGateResult.CancelStale("turn_no_longer_current");
        if (inputs.BufferHasNewInput) return ReplyGateResult.CancelStale("superseded_by_new_input");
        if (inputs.UserTyping) return ReplyGateResult.Defer("user_typing");
        if (inputs.PendingOutputCount > 0) return ReplyGateResult.Defer("output_still_pending");
        if (inputs.CapabilityPending) return ReplyGateResult.DeferForCapability("capability_running");

        var context = new ReplyGateContext(inputs.Turn, inputs.CombinedInput,
            InputProbe.AddressesCharacter(inputs.CombinedInput, inputs.CharacterNames),
            InputProbe.AsksQuestion(inputs.CombinedInput), InputProbe.LooksIncomplete(inputs.CombinedInput),
            inputs.Phase, inputs.UserTyping, inputs.PendingInputCount, inputs.PendingOutputCount,
            inputs.LastCharacterActivity, inputs.SinceLastCharacterActivity, inputs.ConsecutiveCharacterTurns,
            inputs.ProactiveOrigin);

        var advice = await advisor.AdviseAsync(context, cancellationToken);
        return advice.ShouldReply && advice.Intent != Pry.Core.Intelligence.ResponseIntent.DoNothing
            ? new ReplyGateResult(ReplyGateDisposition.Allow, advice.Reason, advice.Intent) { Advice = advice }
            : ReplyGateResult.DoNothing(advice.Reason);
    }
}
