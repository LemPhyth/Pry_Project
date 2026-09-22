namespace Pry.Core.Runtime;

/// <summary>
/// Interruption Controller：插话是一等运行时能力，而不是错误路径。
/// 它只裁决"正在规划/正在投递的东西怎么处理"——继续、取消剩余气泡、还是取消整轮重规划；
/// 具体"这句话算不算插话"由 <see cref="IInterruptClassifier"/> 提供。
/// </summary>
public sealed class InterruptionController(IInterruptClassifier classifier)
{
    public InterruptionDecision OnUserInput(RuntimeUserInput input, RuntimeObservedState observed)
    {
        var active = observed.ActiveTurn;
        var delivering = observed.DeliveringTurn;

        if (observed.Phase == RuntimePhase.Delivering && delivering is not null)
        {
            var deliveryAssessment = classifier.Classify(input, observed);
            return deliveryAssessment.Strength switch
            {
                InterruptStrength.Backchannel => new InterruptionDecision(InterruptionDisposition.ContinueDelivery,
                    $"backchannel_during_delivery:{deliveryAssessment.Reason}", []),
                _ => new InterruptionDecision(InterruptionDisposition.CancelRemainingBubbles,
                    $"{DeliveryReason(deliveryAssessment)}:{deliveryAssessment.Reason}", [delivering])
            };
        }

        if (observed.Phase != RuntimePhase.Planning || active is null)
            return InterruptionDecision.None;

        var assessment = classifier.Classify(input, observed);
        return assessment.Strength switch
        {
            InterruptStrength.Backchannel => new InterruptionDecision(InterruptionDisposition.MergeIntoCurrentBuffer,
                $"backchannel:{assessment.Reason}", []),
            InterruptStrength.Soft => new InterruptionDecision(
                InterruptionDisposition.CancelTurnAndReplan, $"new_content:{assessment.Reason}", [active]),
            _ => new InterruptionDecision(InterruptionDisposition.CancelTurnAndReplan,
                $"hard_interrupt:{assessment.Reason}", [active])
        };
    }

    /// <summary>Capability 执行期间用户改口：无论软硬都作废当前计划并重规划。</summary>
    public InterruptionDecision OnCapabilityUserInput(RuntimeUserInput input, RuntimeObservedState observed)
    {
        var active = observed.ActiveTurn;
        if (active is null) return InterruptionDecision.None;
        var assessment = classifier.Classify(input, observed);
        if (assessment.Strength == InterruptStrength.Backchannel)
            return new InterruptionDecision(InterruptionDisposition.MergeIntoCurrentBuffer,
                $"backchannel_while_capability:{assessment.Reason}", []);
        return new InterruptionDecision(InterruptionDisposition.CancelTurnAndReplan,
            $"intent_changed_while_capability:{assessment.Reason}", [active]);
    }

    /// <summary>回复已投递完、只剩排队气泡时，非背书式插话应停掉未发送部分。</summary>
    public InterruptionDecision OnProactivePending(RuntimeUserInput input, RuntimeObservedState observed)
    {
        var assessment = classifier.Classify(input, observed);
        if (assessment.Strength == InterruptStrength.Backchannel)
            return new InterruptionDecision(InterruptionDisposition.ContinueDelivery,
                $"proactive_backchannel:{assessment.Reason}", []);
        return new InterruptionDecision(InterruptionDisposition.CancelRemainingBubbles,
            $"proactive_superseded_by_user:{assessment.Reason}", observed.DeliveringTurn is null ? [] : [observed.DeliveringTurn]);
    }

    private static string DeliveryReason(InterruptAssessment assessment) =>
        assessment.Strength == InterruptStrength.Hard ? "hard_interrupt_during_delivery" : "soft_interrupt_during_delivery";

}
