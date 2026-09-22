namespace Pry.Core.Expression;

public enum ExpressionPatternSource { Authored, Learned }

/// <summary>
/// How a habit reads, so a joke catchphrase can be kept out of a sad moment. This is a property
/// of the style itself, not one of the FUTURE applicability fields.
/// </summary>
public enum ExpressionTone { Neutral, Playful, Affectionate, Sharp }

/// <summary>
/// Minimal shape from the architecture plan. Source message text is not copied in by default;
/// EvidenceCount only records how many separate turns supported the habit.
/// </summary>
public sealed record ExpressionPattern
{
    public required string Id { get; init; }
    public required string CharacterInstanceId { get; init; }

    /// <summary>Short description of when it fits, e.g. "轻微吐槽".</summary>
    public required string Situation { get; init; }
    public required string Style { get; init; }
    public ExpressionTone Tone { get; init; } = ExpressionTone.Neutral;

    public double Confidence { get; set; } = 0.5;
    public long UsageCount { get; set; }
    public long EvidenceCount { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public ExpressionPatternSource Source { get; init; } = ExpressionPatternSource.Learned;
    public bool Suppressed { get; set; }
    /// <summary>Distinct explicit feedback references; assistant repetition is not evidence.</summary>
    public IReadOnlyList<string> AcceptedEvidenceIds { get; init; } = [];
    public IReadOnlyList<string> AcceptedTurnKeys { get; init; } = [];

    /// <summary>Only a replay guard; confidence ceilings out well before this window fills, so
    /// dropping the oldest references cannot change a selection.</summary>
    public const int AcceptedEvidenceWindow = 64;

    public static IReadOnlyList<string> Confirmed(IEnumerable<string> values) =>
        [.. values.Distinct(StringComparer.Ordinal).TakeLast(AcceptedEvidenceWindow)];

    public string DedupKey => $"{Tone}|{ExpressionGuard.Normalize(Style)}";
}

public sealed record ExpressionHint(string PatternId, string Situation, string Style, ExpressionTone Tone,
    double Strength)
{
    public override string ToString() => $"{Situation}→{Style}";
}

/// <summary>
/// Guards shared by retrieval, selection and learning. Everything here is a blacklist for a
/// failure mode that was actually observed in companion chatbots, not a general rule engine.
/// </summary>
public static class ExpressionGuard
{
    private static readonly string[] AssistantBoilerplate =
    [
        "请问", "有什么可以帮", "希望能帮到你", "希望对您", "作为ai", "作为一个", "我是人工智能", "我理解你的感受",
        "感受到你的", "如果你愿意", "随时告诉我", "首先", "其次", "综上所述", "让我们一起", "加油哦", "哦加油"
    ];

    private static readonly string[] IdentityClaims =
    [
        "我是ai", "我是模型", "我是助手", "我无法", "抱歉，我不", "我没有真实"
    ];

    private static readonly string[] CrudeMarkers =
    ["傻逼", " sb", "艹", "操", "草泥马", "妈的", "他妈", "狗东西", "废物", "滚蛋"];

    /// <summary>Anything that reads like a fact claim is not a verbal habit.</summary>
    private static readonly string[] FactualMarkers = ["https://", "http://", "=>", "元", "公斤", "摄氏度"];

    public const int MaxHabitCharacters = 24;

    public static bool LooksLikeAssistance(string candidate) =>
        AssistantBoilerplate.Any(x => candidate.Contains(x, StringComparison.OrdinalIgnoreCase));

    public static bool ClaimsIdentity(string candidate) =>
        IdentityClaims.Any(x => candidate.Contains(x, StringComparison.OrdinalIgnoreCase));

    public static bool IsCrude(string candidate) =>
        CrudeMarkers.Any(x => candidate.Contains(x, StringComparison.OrdinalIgnoreCase));

    public static bool MakesFactualClaim(string candidate) =>
        FactualMarkers.Any(x => candidate.Contains(x, StringComparison.OrdinalIgnoreCase))
        || candidate.Any(char.IsDigit);

    public static bool IsTooLong(string candidate) => candidate.Length > MaxHabitCharacters;

    public static string Normalize(string value) =>
        new string((value ?? string.Empty).Where(ch => !char.IsPunctuation(ch) && !char.IsSymbol(ch)
            && !char.IsWhiteSpace(ch)).ToArray()).ToLowerInvariant();
}
