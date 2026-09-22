using Pry.Core.Expression;

namespace Pry.Core.Intelligence;

/// <summary>
/// Projects one decision into the bounded text the model sees. This is not an assembly point:
/// the context builder owns the overall budget and calls it once. Nothing in this layer appends
/// text to a prompt by itself.
/// </summary>
public static class ReplyGenerationRenderer
{
    public const int DefaultBudgetCharacters = 700;

    private static readonly Dictionary<ResponseIntent, string> IntentGuidance = new()
    {
        [ResponseIntent.Acknowledge] = "接住这句话就够了，不展开、不追问、不要把对方的情绪总结一遍",
        [ResponseIntent.ContinueTopic] = "顺着对方说到一半的地方继续，不要重新开场",
        [ResponseIntent.Ask] = "只问一个你真的想知道的问题，问完就停",
        [ResponseIntent.Tease] = "回怼但不真伤人，短句，不解释自己为什么好笑",
        [ResponseIntent.PushBack] = "直接表达不爽，不道歉，也不说教",
        [ResponseIntent.Comfort] = "先陪着，不分析问题，不给建议清单",
        [ResponseIntent.Clarify] = "只问清挡住理解的那一点，其余先按对方说的理解",
        [ResponseIntent.Refuse] = "简短说明这件事不做，不要讲课",
        [ResponseIntent.Share] = "说你自己这边的一个具体反应，不要泛泛而谈",
        [ResponseIntent.EndTopic] = "自然收尾，不挽留也不用道别模板",
        [ResponseIntent.DoNothing] = "这一轮不需要说话",
        [ResponseIntent.UseCapability] = "先把事情做掉再说，不要把能力名或参数讲给对方听"
    };

    public static string Render(ReplyGenerationContext context, IdentitySnapshot identity,
        int budgetCharacters = DefaultBudgetCharacters)
    {
        var required = new List<string>();
        var optional = new List<string>();

        required.Add("这一轮：" + IntentGuidance[context.Decision.Intent]);
        foreach (var avoidance in context.Avoidances) required.Add("避免：" + avoidance);
        if (context.MemoryDegraded)
            required.Add("（这一轮没查到相关记忆，不要说对方从没提过）");
        if (context.ExpressionHints.Count == 0)
            required.Add("表达：这次不用任何口癖，直接说");

        optional.Add("此刻的你：" + context.Mood.Label);
        optional.Add("你们之间：" + RelationshipPhrase(context));
        if (identity.Boundaries.Count > 0)
            optional.Add("你的底线：" + string.Join("；", identity.Boundaries.Take(3)));
        foreach (var hint in context.ExpressionHints)
            optional.Add($"表达参考（可以用也可以完全不用）：{hint.Situation}→{hint.Style}");
        if (context.Decision.RelativeLengthBudget <= 0.5 && context.Decision.Intent != ResponseIntent.DoNothing)
            optional.Add("长度：几个字就够");

        var cost = required.Sum(x => x.Length + 1);
        foreach (var candidate in optional.ToList())
        {
            if (cost + candidate.Length + 1 > budgetCharacters) optional.Remove(candidate);
            else cost += candidate.Length + 1;
        }
        return string.Join("\n", [.. required, .. optional]);
    }

    /// <summary>Qualitative on purpose: numbers are internal signals and must not reach the prompt.</summary>
    private static string RelationshipPhrase(ReplyGenerationContext context) => context.FamiliarityGate switch
    {
        ExpressionFamiliarityGate.Playful => "很熟了，说话可以随便，也知道哪些玩笑你不会真生气",
        ExpressionFamiliarityGate.Open => "已经熟了，不用客套，但也没到什么都拿来开玩笑",
        _ when context.Decision.Register == ExpressionRegister.Serious => "还不太熟，先把分寸拿稳",
        _ => "还不算熟，保持礼貌的距离更像你"
    };
}
