using Pry.Core.Models;

namespace Pry.Core.Intelligence;

public enum ScenarioKind
{
    LowContent,
    Greeting,
    Farewell,
    PlayfulProvoke,
    Hostile,
    EmotionalDisclosure,
    DirectQuestion,
    RequestForAction,
    CorrectionOfFraming,
    RelationshipCorrection,
    TopicContinuation,
    Unknown
}

/// <summary>
/// A deliberately shallow reading of the moment. It is not a theory of mind: every flag exists
/// because some behavior gate downstream asks for it.
/// </summary>
public sealed record SituationReading
{
    public required ScenarioKind Scenario { get; init; }
    public double Confidence { get; init; } = 0.6;
    public bool SeeksComfort { get; init; }
    public bool BlockingAmbiguity { get; init; }
    public bool WarrantsCuriosity { get; init; }
    public bool ContradictsRecalledMemory { get; init; }
    public bool RelationshipCorrectionNearer { get; init; }
    public string NormalizedInput { get; init; } = string.Empty;

    public string Tag => Scenario.ToString();
}

public static class SituationReader
{
    private static readonly string[] LowContentMarkers =
    [
        "草", "好", "好的", "行", "嗯", "嗯嗯", "哦", "噢", "喔", "对", "对的", "是", "是的", "可以", "可",
        "哈哈", "哈哈哈", "哈哈哈哈", "嘿嘿", "嘻嘻", "emm", "emmm", "em", "233", "2333", "666", "nb", "牛",
        "xswl", "笑死", "无语", "啧", "哇", "呀", "ok", "okok", "k", "1", "2", "?"
    ];

    private static readonly string[] GreetingMarkers =
        ["在吗", "在不在", "早安", "早上好", "晚安", "晚上好", "你好", "hi", "hello", "嗨", "哈喽", "human?"];

    private static readonly string[] FarewellMarkers =
        ["睡了", "去睡", "先睡", "下线", "再见", "拜拜", "回头聊", "下次", "先这样", "去忙", "洗澡", "困了"];

    private static readonly string[] HostileMarkers =
        ["滚", "傻逼", "sb", "白痴", "闭嘴", "恶心", "废物", "真烦", "别说话", "安静点", "吵死了", "讨厌你"];

    private static readonly string[] PlayfulMarkers =
        ["就这", "略略", "笨蛋", "傻瓜", "菜狗", "菜", "你行不行", "是不是傻", "憨", "皮", "逗我", "笑死你",
            "羞羞脸", "笨笨", "小废物", "你猜", "打我呀", "气你"];

    private static readonly string[] DisclosureMarkers =
        ["难受", "好累", "累死", "烦", "烦躁", "压力", "焦虑", "emo", "不开心", "难过", "哭", "崩溃", "郁闷",
            "孤独", "失眠", "心情", "委屈", "迷茫", "无力", "没意思", "空虚", "心慌", "心态崩"];

    private static readonly string[] ComfortRequestMarkers =
        ["陪陪我", "安慰", "抱抱", "抱一下", "哄哄", "帮帮我", "怎么办", "谁陪我说说话", "和我说说话", "陪我聊",
            "骂醒我", "夸夸我"];

    private static readonly string[] ActionRequestMarkers =
        ["打开", "帮我查", "帮我搜", "查一下", "搜一下", "生成一", "画一", "写一", "播放", "启动", "提醒我",
            "定个", "截个", "看看天气", "找一下"];

    private static readonly string[] FramingCorrectionMarkers =
        ["不是这样", "你说错了", "说错了", "我说过", "又忘了", "你忘了", "乱说", "瞎说", "不是我说的是", "搞错了",
            "听错了", "不是这个意思"];

    private static readonly string[] RelationshipFarMarkers =
        ["我们只是", "别叫我", "别这么", "没那么熟", "不要这样叫", "我们什么关系", "你不是我", "别乱认", "保持距离",
            "太近了", "生分点", "别腻歪"];

    private static readonly string[] RelationshipNearMarkers =
        ["我们关系", "别那么生分", "不用客气", "随便说", "你可以直接", "我们是朋友", "自己人", "不用这么见外",
            "以后直说"];

    private static readonly string[] AnaphoraMarkers =
        ["这个", "那个", "它", "他们", "上面", "刚才", "刚刚", "那件事", "之前说"];

    private static readonly string[] NegationMarkers =
        ["不是", "没有", "不再", "已经不", "现在不", "变了", "才不是", "从来没有"];

    private const int LowContentCeilingCharacters = 6;

    public static SituationReading Read(SituationSnapshot situation)
    {
        var raw = situation.UserInput ?? string.Empty;
        var text = Normalize(raw);
        var memoryFacts = situation.Memory.Observations;

        var seeksComfort = ComfortRequestMarkers.Any(text.Contains);
        var disclosure = DisclosureMarkers.Any(text.Contains);
        var hostile = HostileMarkers.Any(text.Contains);
        var playful = PlayfulMarkers.Any(text.Contains);
        var relationshipFar = RelationshipFarMarkers.Any(text.Contains);
        var relationshipNear = RelationshipNearMarkers.Any(text.Contains);
        var framing = FramingCorrectionMarkers.Any(text.Contains);
        var action = ActionRequestMarkers.Any(text.Contains);
        var greeting = GreetingMarkers.Any(text.Contains);
        var farewell = FarewellMarkers.Any(text.Contains);
        var isQuestion = EndsAsQuestion(text) || raw.TrimEnd().EndsWith('?') || raw.TrimEnd().EndsWith('？')
            || ContainsQuestionWord(text);

        var lowContent = IsLowContent(raw);

        var contradiction = memoryFacts.Count > 0 && NegationMarkers.Any(text.Contains)
            && memoryFacts.Any(fact => SharesTopicWord(text, fact.Summary));

        var scenario =
            lowContent ? ScenarioKind.LowContent :
            hostile ? ScenarioKind.Hostile :
            relationshipFar || relationshipNear ? ScenarioKind.RelationshipCorrection :
            disclosure && seeksComfort ? ScenarioKind.EmotionalDisclosure :
            disclosure ? ScenarioKind.EmotionalDisclosure :
            farewell ? ScenarioKind.Farewell :
            greeting && isQuestion == false ? ScenarioKind.Greeting :
            framing ? ScenarioKind.CorrectionOfFraming :
            action ? ScenarioKind.RequestForAction :
            playful ? ScenarioKind.PlayfulProvoke :
            isQuestion ? ScenarioKind.DirectQuestion :
            raw.Trim().Length > 4 ? ScenarioKind.TopicContinuation : ScenarioKind.Unknown;

        var previousUserText = situation.LastUserTextBeforeCurrent;
        // Anaphora is everywhere in Chinese, so this only fires when there is genuinely nothing
        // else to stand on: a short utterance, no earlier turn to resolve it against, no recalled
        // memory, and no clarification already outstanding from the character.
        var blockingAmbiguity = text.Length <= 10 && AnaphoraMarkers.Any(text.Contains)
            && previousUserText.Length == 0 && memoryFacts.Count == 0 && !isQuestion
            && situation.TrailingAssistantQuestionStreak == 0;

        var warrantsCuriosity = scenario == ScenarioKind.TopicContinuation
            && raw.Trim().Length >= 14 && situation.TrailingAssistantQuestionStreak == 0;

        return new SituationReading
        {
            Scenario = scenario,
            NormalizedInput = text,
            SeeksComfort = seeksComfort,
            BlockingAmbiguity = blockingAmbiguity,
            WarrantsCuriosity = warrantsCuriosity,
            ContradictsRecalledMemory = contradiction,
            RelationshipCorrectionNearer = relationshipNear && !relationshipFar,
            Confidence = scenario == ScenarioKind.Unknown ? 0.35 : lowContent || hostile ? 0.8 : 0.6
        };
    }

    /// <summary>
    /// 低信息量判据本体。宿主手里只有对话记录时用它数"连着几句都是嗯哦好"，
    /// 不必为每条历史消息各造一个 <see cref="SituationSnapshot"/>。
    /// </summary>
    public static bool IsLowContent(string raw)
    {
        var text = Normalize(raw);
        return text.Length == 0
            || (text.Length <= LowContentCeilingCharacters && LowContentMarkers.Contains(text))
            || (raw.Trim().Length <= LowContentCeilingCharacters && IsRepeatedNoise(raw.Trim()));
    }

    /// <summary>
    /// 记录末尾连续几句低信息量的用户消息（从最新往回数，遇到角色自己的话或非低信息量输入就停）。
    /// Runtime 来问的时候本轮那句用户消息已经落库，所以单独一句"嗯"读作 1。
    /// </summary>
    public static int TrailingLowContentUserInputs(IReadOnlyList<ChatMessage> recent)
    {
        var streak = 0;
        for (var i = recent.Count - 1; i >= 0; i--)
        {
            if (recent[i].Role != ChatRole.User || !IsLowContent(recent[i].Content ?? string.Empty)) break;
            streak++;
        }
        return streak;
    }

    private static bool IsRepeatedNoise(string trimmed) =>
        trimmed.Length is > 0 and <= LowContentCeilingCharacters
        && trimmed.Distinct().Count() == 1 && !char.IsLetterOrDigit(trimmed[0]);

    private static bool EndsAsQuestion(string text) =>
        text.EndsWith("吗") || text.EndsWith("呢") || text.EndsWith("么") || text.EndsWith("吧");

    private static bool ContainsQuestionWord(string text) =>
        text.Contains("什么") || text.Contains("怎么") || text.Contains("为啥") || text.Contains("为什么")
        || text.Contains("能不能") || text.Contains("是不是") || text.Contains("哪个") || text.Contains("有没有");

    private static bool SharesTopicWord(string input, string memorySummary) =>
        SplitWords(memorySummary).Any(word => word.Length >= 2
            && input.Contains(word, StringComparison.Ordinal));

    private static string[] SplitWords(string text) =>
        text.Split(['，', '。', '、', '；', ' ', '\n', '：'], StringSplitOptions.RemoveEmptyEntries)
            .Select(Normalize).Where(x => x.Length > 0).ToArray();

    public static string Normalize(string value) =>
        new string((value ?? string.Empty).Where(ch => !char.IsPunctuation(ch) && !char.IsSymbol(ch)
            && !char.IsWhiteSpace(ch)).ToArray()).ToLowerInvariant();
}
