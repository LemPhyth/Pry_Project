using Pry.Core.Abstractions;
using Pry.Core.Expression;
using Pry.Core.Models;

namespace Pry.Core.Intelligence;

/// <summary>What happened after the character actually spoke.</summary>
public sealed record TurnOutcome(
    SituationSnapshot Situation,
    CharacterStateSnapshot State,
    BehaviorDecision Decision,
    IReadOnlyList<ExpressionHint> UsedHints,
    InteractionOutcome Outcome,
    string DeliveredAssistantText,
    DateTimeOffset CompletedAt)
{
    /// <summary>False for cancelled or replaced output: it must not teach habits or patterns.</summary>
    public bool DeliveredToUser { get; init; } = true;
    public MoodSignal? MoodEffect { get; init; }
    public double SharedContextDelta { get; init; }
    public string? FollowUpEvidence { get; init; }
    public bool FeedbackIsExplicit { get; init; }
    public string? ExpressionFeedbackId { get; init; }
    public IReadOnlyList<string> AcceptedExpressionStyles { get; init; } = [];
    public IReadOnlyList<string> RejectedExpressionStyles { get; init; } = [];
}

/// <summary>
/// The two calls Runtime makes in this domain: ask before speaking, report after delivery.
/// Deciding when to speak, and whether a turn is still current, stays with Runtime.
/// </summary>
public sealed class CharacterIntelligenceService : ICharacterIntelligence
{
    private readonly CharacterStateService _characterState;
    private readonly BehaviorPlanner _planner;
    private readonly ExpressionService _expressions;
    private readonly IBehaviorPatternRepository _patterns;
    private readonly IBehaviorExperienceRepository _experiences;
    private readonly BehaviorPatternRetriever _patternRetriever;

    public CharacterIntelligenceService(CharacterStateService characterState, ExpressionService expressions,
        IBehaviorPatternRepository patterns, BehaviorPlanner? planner = null,
        BehaviorPatternRetriever? patternRetriever = null,
        IBehaviorExperienceRepository? experiences = null)
    {
        _characterState = characterState;
        _expressions = expressions;
        _patterns = patterns;
        _experiences = experiences ?? new InMemoryBehaviorExperienceRepository();
        _planner = planner ?? new BehaviorPlanner();
        _patternRetriever = patternRetriever ?? new BehaviorPatternRetriever();
    }

    public CharacterStateService State => _characterState;

    public async Task<ReplyGenerationContext> DecideAsync(SituationSnapshot situation,
        CharacterStateSnapshot state, CancellationToken cancellationToken = default)
    {
        var scenarioTag = SituationReader.Read(situation).Tag;
        var stored = await _patterns.ListForCharacterAsync(situation.CharacterInstanceId, cancellationToken);
        var decision = _planner.Plan(situation, state, _patternRetriever.Retrieve(stored, scenarioTag));
        var selection = await _expressions.SelectForTurnAsync(situation, state, decision, cancellationToken);

        return new ReplyGenerationContext
        {
            TurnId = situation.TurnId,
            Decision = decision,
            StateVersion = state.StateVersion,
            ExpressionHints = selection.Hints,
            Memory = situation.Memory.Observations,
            MemoryDegraded = situation.Memory.IsDegraded,
            FamiliarityGate = state.Relationship.ExpressionGate,
            AllowsAffectionateAddress = state.Relationship.AllowsAffectionateAddress,
            Mood = new MoodSummary(state.Mood.DescriptiveLabel, state.Mood.SupportsPlayfulRegister,
                Math.Round(state.Mood.LengthBudget, 2)),
            Avoidances = BuildAvoidances(decision, selection.Rejections, situation)
        };
    }

    public async Task CompleteTurnAsync(TurnOutcome outcome, CancellationToken cancellationToken = default)
    {
        if (!outcome.DeliveredToUser) return;

        var now = outcome.CompletedAt;
        if (outcome.Decision.RequestedRelationshipCorrection is { } correction)
            await _characterState.NoteRelationshipCorrectedAsync(now, correction.Nearer, outcome.MoodEffect,
                cancellationToken);
        else if (outcome.Outcome is InteractionOutcome.Rejected && outcome.State.Relationship.BoundaryFriction >= 0.2)
            await _characterState.NoteBoundaryPushedAsync(now, cancellationToken);
        else
            await _characterState.NoteExchangeAsync(now, outcome.MoodEffect ?? DefaultSignal(outcome),
                outcome.SharedContextDelta, cancellationToken);

        if (outcome.Decision.Intent != ResponseIntent.DoNothing)
            await RecordExperienceAsync(outcome, cancellationToken);

        await _expressions.NoteUsedAsync(outcome.UsedHints, now, cancellationToken);
        _ = EnqueueExpressionObservation(outcome);
    }

    /// <summary>
    /// Reports later user feedback about an already delivered turn. Does not count another
    /// exchange, change mood/relationship, or mark an expression as used again.
    /// Hosts must not replay CompleteTurnAsync merely to attach a user follow-up.
    /// </summary>
    /// <returns>False only if the turn was never delivered or the expression queue is full.</returns>
    public async Task<bool> ObserveFeedbackAsync(TurnOutcome outcome, CancellationToken cancellationToken = default)
    {
        if (!outcome.DeliveredToUser) return false;
        var learnsBehaviour = outcome.FeedbackIsExplicit && !string.IsNullOrWhiteSpace(outcome.FollowUpEvidence);
        var learnsExpression = outcome.AcceptedExpressionStyles.Count > 0
            || outcome.RejectedExpressionStyles.Count > 0;
        // Feedback that carries neither kind of evidence would only duplicate the delivery row.
        if (!learnsBehaviour && !learnsExpression) return true;
        if (learnsBehaviour && outcome.Decision.Intent != ResponseIntent.DoNothing)
            await RecordExperienceAsync(outcome, cancellationToken);
        return !learnsExpression || EnqueueExpressionObservation(outcome);
    }

    private bool EnqueueExpressionObservation(TurnOutcome outcome)
    {
        return _expressions.Learner.TryEnqueue(new DeliveredTurn(
            outcome.Situation.ConversationId, outcome.Situation.TurnId, outcome.CompletedAt,
            outcome.Situation.UserInput, outcome.DeliveredAssistantText,
            UserAskedToStopTalkingLikeThat: false)
        {
            FeedbackEvidenceId = outcome.ExpressionFeedbackId,
            AcceptedStyles = outcome.AcceptedExpressionStyles,
            RejectedStyles = outcome.RejectedExpressionStyles
        });
    }

    private async Task RecordExperienceAsync(TurnOutcome outcome, CancellationToken cancellationToken)
    {
        var experience = new BehaviorExperience(
            outcome.Situation.CharacterInstanceId, outcome.Situation.ConversationId, outcome.Situation.TurnId,
            outcome.Decision.ScenarioTag, outcome.Decision.Intent, outcome.Outcome, outcome.CompletedAt,
            FollowUpEvidence: outcome.FollowUpEvidence)
        { FeedbackIsExplicit = outcome.FeedbackIsExplicit };
        await _experiences.AppendAsync(experience, cancellationToken);
        await new BehaviorPatternUpdater(_patterns, () => outcome.CompletedAt)
            .RecordAsync(experience, cancellationToken);
    }

    private static MoodSignal DefaultSignal(TurnOutcome outcome) => outcome switch
    {
        { Outcome: InteractionOutcome.PositiveReaction } => MoodSignal.LightJokeLanded,
        { Outcome: InteractionOutcome.Rejected } => MoodSignal.IgnoredBoundary,
        { Decision.Intent: ResponseIntent.Comfort } => MoodSignal.WarmExchange,
        { Situation.ConsecutiveLowContentInputs: >= 2 } => MoodSignal.RepeatedPingPong,
        { Situation.Memory.Observations.Count: > 0 } => MoodSignal.InterestingTopic,
        _ => MoodSignal.SustainedConversation
    };

    private static IReadOnlyList<string> BuildAvoidances(BehaviorDecision decision,
        IReadOnlyList<string> selectorRejections, SituationSnapshot situation)
    {
        var avoidances = new List<string>();
        if (!decision.ParaphraseAllowed) avoidances.Add("不要复述用户这句话的措辞，用角色自己的说法接住");
        if (decision.QuestionBudget == 0) avoidances.Add("这一轮不要提问，先把已经说到的内容接住");
        else avoidances.Add("整条回复最多问一个问题");
        if (decision.HabitToneAllowed == HabitToneAllowed.None)
            avoidances.Add("这个场景不要使用任何口癖、玩梗句式或夸张语气词");
        else if (decision.HabitToneAllowed == HabitToneAllowed.GroundedOnly)
            avoidances.Add("只用平实的表达，不要玩笑口癖");
        if (LooksLikeServiceToneRisk(decision))
            avoidances.Add("不要用客服式套话，不要总结用户情绪再给出建议清单");
        if (situation.Memory.IsDegraded)
            avoidances.Add("本轮相关记忆没有查到，不要声称用户从未提过这件事，也不要编造过去");
        if (decision.Intent is ResponseIntent.Acknowledge && decision.RelativeLengthBudget <= 0.5)
            avoidances.Add("几个字就够，不要展开");
        if (selectorRejections.Count > 0) avoidances.Add("未采用的表达候选只是参考，不必补齐");
        return avoidances;
    }

    private static bool LooksLikeServiceToneRisk(BehaviorDecision decision) =>
        decision.Register is ExpressionRegister.Warm or ExpressionRegister.Neutral
        && decision.Intent is ResponseIntent.Acknowledge or ResponseIntent.ContinueTopic or ResponseIntent.Comfort;
}
