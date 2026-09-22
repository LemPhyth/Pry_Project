namespace Pry.Core.Intelligence;

/// <summary>
/// Decides what Pry should do if she speaks. Rule-based and deterministic on purpose: a 2B
/// local model cannot be trusted with gate decisions, and every branch here needs to be
/// explainable in Developer Mode and reproducible in an eval.
/// </summary>
public sealed class BehaviorPlanner
{
    public BehaviorDecision Plan(SituationSnapshot situation, CharacterStateSnapshot state,
        IReadOnlyList<BehaviorPattern>? relevantPatterns = null)
    {
        var reading = SituationReader.Read(situation);
        var rationale = new List<string>();
        var gates = new List<string>();

        var (intent, confidence) = ChooseBase(reading, situation, state, rationale);
        (intent, confidence) = ApplyLearnedPatterns(reading, intent, confidence, relevantPatterns, rationale);
        (intent, confidence) = ApplyGates(reading, situation, state, intent, confidence, gates, rationale);

        var register = ChooseRegister(reading, intent);
        var habits = ChooseHabitTone(register, intent, state);
        var budget = ChooseQuestionBudget(reading, situation, intent, gates, rationale);
        if (budget == 0 && intent is ResponseIntent.Ask or ResponseIntent.Clarify)
        {
            gates.Add("behavior:no_question_slot");
            rationale.Add("近期已经在追问，改为先把话接住");
            intent = reading.Scenario == ScenarioKind.LowContent ? ResponseIntent.Acknowledge : ResponseIntent.ContinueTopic;
        }

        return new BehaviorDecision
        {
            Intent = intent,
            ScenarioTag = reading.Tag,
            Confidence = Math.Round(Math.Clamp(confidence, 0.1, 0.95), 3),
            Register = register,
            HabitToneAllowed = habits,
            QuestionBudget = budget,
            RelativeLengthBudget = ChooseLengthBudget(intent, reading, situation, state),
            ParaphraseAllowed = reading.Scenario is not (ScenarioKind.EmotionalDisclosure or ScenarioKind.CorrectionOfFraming),
            RequestedRelationshipCorrection = reading.Scenario == ScenarioKind.RelationshipCorrection
                ? new RelationshipCorrection(reading.RelationshipCorrectionNearer, Truncate(situation.UserInput))
                : null,
            RequestedMemoryCorrection = reading.ContradictsRecalledMemory,
            CapabilityHint = intent == ResponseIntent.UseCapability
                ? MatchCapability(situation) : null,
            Rationale = rationale,
            Gates = gates
        };
    }

    private static (ResponseIntent, double) ChooseBase(SituationReading reading, SituationSnapshot situation,
        CharacterStateSnapshot state, List<string> rationale)
    {
        switch (reading.Scenario)
        {
            case ScenarioKind.LowContent:
                rationale.Add("输入几乎没有新信息");
                var staySilent = situation.AssistantJustReplied || situation.ConsecutiveLowContentInputs >= 2
                    || state.Relationship.Band == FamiliarityBand.Unfamiliar;
                return staySilent ? (ResponseIntent.DoNothing, 0.7) : (ResponseIntent.Acknowledge, 0.6);

            case ScenarioKind.Greeting:
                rationale.Add("用户在打招呼");
                return (ResponseIntent.Acknowledge, 0.65);

            case ScenarioKind.Farewell:
                rationale.Add("用户在收尾");
                return (ResponseIntent.EndTopic, 0.7);

            case ScenarioKind.Hostile:
                rationale.Add("用户的话带攻击性");
                return state.Relationship.BoundaryFriction >= 0.4
                    ? (ResponseIntent.Refuse, 0.7)
                    : (ResponseIntent.PushBack, 0.55);

            case ScenarioKind.EmotionalDisclosure when reading.SeeksComfort:
                rationale.Add("用户明确提出了需要陪伴");
                return (ResponseIntent.Comfort, 0.7);

            case ScenarioKind.EmotionalDisclosure:
                rationale.Add("用户心情不好但没有提出需要安慰，先接住而不是做情绪工作");
                return (ResponseIntent.Acknowledge, 0.6);

            case ScenarioKind.DirectQuestion:
                rationale.Add("用户要的是一个答案，不是反问");
                return (ResponseIntent.ContinueTopic, 0.65);

            case ScenarioKind.RequestForAction:
                rationale.Add("用户在请求一个动作");
                return (ResponseIntent.UseCapability, 0.6);

            case ScenarioKind.CorrectionOfFraming:
                rationale.Add("用户指出理解偏了，认下来就够了，不要再解释一轮");
                return (ResponseIntent.Acknowledge, 0.6);

            case ScenarioKind.RelationshipCorrection:
                rationale.Add(reading.RelationshipCorrectionNearer
                    ? "用户主动把关系拉近，关系状态按这个更新"
                    : "用户在划定关系边界，关系状态按这个更新");
                return (ResponseIntent.Acknowledge, 0.7);

            case ScenarioKind.PlayfulProvoke:
                rationale.Add("用户在开玩笑挑逗");
                return (ResponseIntent.Tease, 0.6);

            case ScenarioKind.TopicContinuation when reading.WarrantsCuriosity:
                rationale.Add("话题有延伸空间且当前没有欠着问题");
                return (ResponseIntent.Ask, 0.45);

            case ScenarioKind.TopicContinuation:
                rationale.Add("继续当前话题");
                return (ResponseIntent.ContinueTopic, 0.55);

            default:
                rationale.Add("场景不明确，先贴着用户的话回应");
                return (ResponseIntent.ContinueTopic, 0.35);
        }
    }

    private static (ResponseIntent, double) ApplyLearnedPatterns(SituationReading reading, ResponseIntent chosen,
        double confidence, IReadOnlyList<BehaviorPattern>? patterns, List<string> rationale)
    {
        if (patterns is null || patterns.Count == 0) return (chosen, confidence);
        var usable = patterns.Where(x => x.HasEvidence && x.Confidence >= 0.55).ToArray();
        if (usable.Length == 0) return (chosen, confidence);

        if (confidence < 0.5 && usable[0].Strategy != chosen)
        {
            rationale.Add($"历史经验优先：{reading.Tag} 场景下 {usable[0].Strategy} 效果更好（置信 {usable[0].Confidence:F2}，{usable[0].UsageCount} 次观察）");
            return (usable[0].Strategy, usable[0].Confidence);
        }
        if (usable.Any(x => x.Strategy == chosen))
        {
            rationale.Add("历史经验支持当前策略");
            return (chosen, confidence + 0.05);
        }
        return (chosen, confidence);
    }

    private static (ResponseIntent, double) ApplyGates(SituationReading reading, SituationSnapshot situation,
        CharacterStateSnapshot state, ResponseIntent intent, double confidence, List<string> gates,
        List<string> rationale)
    {
        if (intent == ResponseIntent.DoNothing && situation.RequiresReply)
        {
            gates.Add("runtime:reply_required");
            rationale.Add("运行环境要求必须回应，改为最短承接");
            return (ResponseIntent.Acknowledge, confidence);
        }

        if (intent == ResponseIntent.UseCapability && situation.AvailableCapabilityNames.Count == 0)
        {
            gates.Add("capability:none_registered");
            return (ResponseIntent.ContinueTopic, confidence);
        }

        if (intent == ResponseIntent.Tease)
        {
            if (!state.Relationship.AllowsTeasing)
            {
                gates.Add("relationship:tease_locked");
                rationale.Add("还没熟到可以回怼，先接住玩笑但不还手");
                return (ResponseIntent.Acknowledge, confidence);
            }
            if (!state.Mood.SupportsPlayfulRegister)
            {
                gates.Add("mood:playful_unavailable");
                return (ResponseIntent.Acknowledge, confidence);
            }
        }

        if (reading.BlockingAmbiguity && intent != ResponseIntent.DoNothing)
        {
            gates.Add("behavior:ambiguity_blocking");
            rationale.Add("指代无法定位，先问清这一次");
            return (ResponseIntent.Clarify, 0.6);
        }

        if (state.Mood.PrefersDisengagement && intent is ResponseIntent.Ask or ResponseIntent.Tease)
        {
            gates.Add("mood:low_energy");
            rationale.Add("当下没什么精力展开，收短");
            return (ResponseIntent.ContinueTopic, confidence);
        }

        if (situation.Memory.IsDegraded)
            gates.Add("memory:unavailable_do_not_claim_no_history");
        else if (situation.Memory.IsVectorDegraded)
            gates.Add("memory:keyword_only");

        return (intent, confidence);
    }

    private static ExpressionRegister ChooseRegister(SituationReading reading, ResponseIntent intent) => intent switch
    {
        ResponseIntent.Comfort => ExpressionRegister.Warm,
        ResponseIntent.Tease => ExpressionRegister.Playful,
        ResponseIntent.PushBack or ResponseIntent.Refuse => ExpressionRegister.Serious,
        _ when reading.Scenario is ScenarioKind.CorrectionOfFraming or ScenarioKind.RelationshipCorrection
            => ExpressionRegister.Serious,
        _ when reading.Scenario == ScenarioKind.EmotionalDisclosure => ExpressionRegister.Warm,
        _ => ExpressionRegister.Neutral
    };

    private static HabitToneAllowed ChooseHabitTone(ExpressionRegister register, ResponseIntent intent,
        CharacterStateSnapshot state) => register switch
    {
        ExpressionRegister.Serious => HabitToneAllowed.None,
        ExpressionRegister.Warm => HabitToneAllowed.GroundedOnly,
        _ when !state.Mood.SupportsPlayfulRegister => HabitToneAllowed.GroundedOnly,
        _ when intent == ResponseIntent.Tease => HabitToneAllowed.Any,
        _ when state.Relationship.ExpressionGate == ExpressionFamiliarityGate.Reserved => HabitToneAllowed.GroundedOnly,
        _ => HabitToneAllowed.Any
    };

    private static int ChooseQuestionBudget(SituationReading reading, SituationSnapshot situation,
        ResponseIntent intent, List<string> gates, List<string> rationale)
    {
        if (intent == ResponseIntent.DoNothing) return 0;
        if (reading.BlockingAmbiguity) return 1;
        var streak = situation.TrailingAssistantQuestionStreak;
        if (streak >= 2)
        {
            gates.Add("behavior:question_streak");
            rationale.Add("连续两轮都在提问，这一轮不问");
            return 0;
        }
        if (streak == 1 && (reading.Scenario == ScenarioKind.LowContent || intent == ResponseIntent.Acknowledge))
            return 0;
        return reading.Scenario == ScenarioKind.DirectQuestion ? 0 : 1;
    }

    private static double ChooseLengthBudget(ResponseIntent intent, SituationReading reading,
        SituationSnapshot situation, CharacterStateSnapshot state) => intent switch
    {
        ResponseIntent.DoNothing => 0,
        ResponseIntent.EndTopic => 0.5,
        ResponseIntent.Acknowledge when reading.Scenario == ScenarioKind.LowContent => 0.4,
        ResponseIntent.Acknowledge when reading.Scenario == ScenarioKind.CorrectionOfFraming => 0.5,
        ResponseIntent.Comfort => Math.Min(1.0, state.Mood.LengthBudget),
        _ => state.Mood.LengthBudget
    };

    private static string? MatchCapability(SituationSnapshot situation) =>
        situation.AvailableCapabilityNames.FirstOrDefault(name =>
            situation.UserInput.Contains(name, StringComparison.OrdinalIgnoreCase));

    private static string Truncate(string value) => value.Length <= 48 ? value : value[..48];
}
