// Ported from Mai-with-u/MaiBot@fb4a8bd917d6fe373df9e3c22289d229e7f1b344:
//   src/maisaka/reply_necessity.py (score_reply_necessity and its helpers)
// Type: ported (Python -> C#). Upstream is GPL-3.0-only; see
//   licenses/upstream/MaiBot_fb4a8bd917d6fe373df9e3c22289d229e7f1b344/LICENSE.
// Pry modifications: 2026-09-23 — 分层结构、曲线形状与多数数值量级取自上游 reply_necessity.py 的
//   模块级常量；Pry 只改了触发分（80→70）与私聊/焦点相关度（40→45），其余与上游同值。
//   因此这些数字按"沿用上游、未经 Pry 标定"登记（见 licenses/THIRD-PARTY-NOTICES.md 与
//   docs/phase2-runtime-contract.md §5），不得作为发行默认值：整条链默认惰性。
//   角色名从输入快照取得，不再硬编码上游角色名；媒体/引用占位前缀改用 Pry 自身的标记；
//   句首点名其他助手的名单改为宿主注入（默认空）。提问/请求/征询/短反应四张词表沿用上游
//   reply_necessity.py:16-20 的群聊语料：随 ported 类型合法入仓，但属未审校的借来措辞，同样待 Eval 复核。
//   取整改为 AwayFromZero。
using System.Text.RegularExpressions;

namespace Pry.Core.Runtime;

/// <summary>
/// 回复必要性评分参数。分数量级固定在 0~100，各层的相对关系是这套机制的设计意图，
/// 具体数值不是：除 <see cref="TriggerScore"/> 与私聊/焦点两项相关度由 Pry 改动外，
/// 本组默认值与上游 reply_necessity.py 的模块级常量同值，属"沿用上游、未经 Pry 标定"，
/// 必须由 Eval 标定替换后才能作为发行默认值（默认路径上整条链是惰性的，见文件头）。
/// </summary>
public sealed record ReplyNecessityOptions
{
    /// <summary>达到该分数才允许进入一轮规划。</summary>
    public int TriggerScore { get; init; } = 70;

    /// <summary>被 @ 时的相关度。必须单独越过 <see cref="TriggerScore"/>：被点名一定要回应。</summary>
    public int RelevanceAt { get; init; } = 100;

    /// <summary>被提及（未 @ 但叫到名字）时的相关度。</summary>
    public int RelevanceMention { get; init; } = 80;

    /// <summary>私聊会话的基础相关度。私聊没有旁人，每条都是对角色说的。</summary>
    public int RelevanceDirect { get; init; } = 45;

    /// <summary>处于焦点会话时的相关度。</summary>
    public int RelevanceFocus { get; init; } = 45;

    public int ContentQuestion { get; init; } = 15;

    public int ContentRequest { get; init; } = 20;

    public int ContentOpinion { get; init; } = 20;

    public int ContentLong { get; init; } = 5;

    public int ContentVeryLong { get; init; } = 10;

    /// <summary>整批都是"哈哈/6/？"这类反应时的减分。短反应不值得打断用户。</summary>
    public int ContentShortReactionPenalty { get; init; } = 25;

    /// <summary>积压量达到阈值时的压力分；阈值内按二次增长，之后按对数增长。</summary>
    public int PressureAtThreshold { get; init; } = 50;

    /// <summary>压力分上限，也是整个压力层的量程。</summary>
    public int PressureMax { get; init; } = 100;

    /// <summary>积压达到阈值的该倍数时，压力分封顶。</summary>
    public double PressureFullRatio { get; init; } = 5.0;

    /// <summary>沉默已超过该会话平均到达间隔时的额外压力。</summary>
    public int PressureIdleBonus { get; init; } = 15;

    /// <summary>最近窗口内角色发言占比不超过该值时不扣存在感。</summary>
    public double PresenceFreeRatio { get; init; } = 0.25;

    /// <summary>占比达到该值时存在感惩罚取满。</summary>
    public double PresenceFullRatio { get; init; } = 0.60;

    public int PresencePenaltyMax { get; init; } = 25;

    /// <summary>存在感统计窗口。</summary>
    public TimeSpan PresenceWindow { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>长度分层所用的字符数（按感知长度计）。</summary>
    public int LongTextCharacters { get; init; } = 40;

    public int VeryLongTextCharacters { get; init; } = 120;

    /// <summary>短反应批量的单条长度上限。</summary>
    public int ShortReactionMaxCharacters { get; init; } = 8;

    /// <summary>纯表情/附件/引用等占位文本的前缀。整条只剩占位时视为没有内容。</summary>
    public IReadOnlyList<string> PlaceholderPrefixes { get; init; } =
    [
        "[sticker:", "[attachment:", "[图片", "[表情", "[文件]", "[语音", "[视频", "[卡片"
    ];

    /// <summary>整条直接忽略的文本前缀（转述、榜单等非用户当前发言）。</summary>
    public IReadOnlyList<string> IgnoredPrefixes { get; init; } = ["[转述", "[合并转发"];

    /// <summary>提问线索词。</summary>
    public IReadOnlyList<string> QuestionTerms { get; init; } = ["怎么", "如何", "为什么", "有没有"];

    /// <summary>明确请求句式，任何上下文都计入。</summary>
    public IReadOnlyList<string> DirectRequestTerms { get; init; } = ["帮我", "帮忙", "能不能", "可以吗", "要不要"];

    /// <summary>弱请求句式，只在已经是对角色说话的上下文里计入，避免抢下别人的闲聊。</summary>
    public IReadOnlyList<string> WeakRequestTerms { get; init; } = ["需要", "求", "看看", "试试"];

    /// <summary>征询意见句式。</summary>
    public IReadOnlyList<string> OpinionTerms { get; init; } = ["你觉得", "你认为", "咋看", "有什么建议"];

    /// <summary>短反应集合。</summary>
    public IReadOnlySet<string> ShortReactions { get; init; } =
        new HashSet<string>(StringComparer.Ordinal) { "哈哈", "哈哈哈", "草", "笑死", "好", "嗯", "啊", "哦", "6", "666", "？", "?" };

    /// <summary>
    /// 句首点名其他助手的模式。这类消息是在向别的模型提问，角色不应插话。
    /// 由宿主按实际接入的助手名单注入。
    /// </summary>
    public IReadOnlyList<string> OtherAssistantNames { get; init; } = [];
}

/// <summary>评分输入：全部是 Runtime 已经掌握的事实，由宿主填好传入，Runtime 不自查记忆库。</summary>
public sealed record ReplyNecessitySnapshot(
    IReadOnlyList<string> ExternalTexts,
    int PendingCount,
    int TriggerThreshold,
    bool HasAt,
    bool HasMention,
    bool IsGroupChat,
    bool FocusActive,
    int RecentSelfReplies,
    int RecentWindowMessages,
    double EffectiveFrequency,
    TimeSpan IdleFor,
    bool IdleReachedAverage,
    IReadOnlyList<string> CharacterNames);

/// <summary>评分结果。Detail 面向 Developer Mode 与轨迹，不参与判定。</summary>
public sealed record ReplyNecessityResult(int Score, string Detail, int PressureScore);

/// <summary>
/// 回复必要性评分：把"该不该说话"从一次模型调用变成一层可复算、可回归的确定性计算。
/// Runtime 因此可以在没有真正 Intelligence 接线时先具备可验收的沉默能力，
/// 而模型只需要在分数够高的那一轮决定"说什么"。
/// 四层相加再整体乘以频率系数：相关度决定"是不是对我说的"，内容决定"这事值不值得接"，
/// 压力决定"用户已经等着了"，存在感决定"我刚说过话，别抢麦"。
/// </summary>
public static class ReplyNecessityScorer
{
    private static readonly Regex ReplyPrefix = new(@"^\[(回复了.+?的消息[:：].+?\]|回复消息\]|CQ:reply[^\]]*\]|reply\])\s*",
        RegexOptions.CultureInvariant);

    private static readonly Regex MentionToken = new(@"@\S+", RegexOptions.CultureInvariant);

    private static readonly Regex PunctuationOnlyQuestion =
        new(@"^[？?！!~～…\s]+[\w一-鿿]{1,4}[？?！!~～…\s]+$", RegexOptions.CultureInvariant);

    private static readonly Regex WhatQuestion = new(@"(?<![这那没])什么", RegexOptions.CultureInvariant);

    private static readonly Regex ParticleQuestion =
        new(@"[吗呢][？?。！!~～…]*$", RegexOptions.CultureInvariant);

    private static readonly Regex MarkedQuestion = new(@"[？?](?:$|[。！!~～…])", RegexOptions.CultureInvariant);

    public static bool ShouldTrigger(ReplyNecessityResult result, ReplyNecessityOptions options) =>
        result.Score >= options.TriggerScore;

    public static ReplyNecessityResult Score(ReplyNecessitySnapshot snapshot, ReplyNecessityOptions? options = null)
    {
        options ??= new ReplyNecessityOptions();
        var threshold = Math.Max(1, snapshot.TriggerThreshold);

        var (relevanceScore, relevanceReason) = Relevance(snapshot, options);
        var isDirectContext = relevanceScore > 0;
        var cleaned = Clean(snapshot.ExternalTexts, options);
        var combined = string.Join("\n", cleaned.Where(t => t.Length > 0));

        var (contentScore, contentReasons) = ScoreContent(cleaned, combined, isDirectContext, snapshot, options);
        var pressureScore = Pressure(snapshot.PendingCount, threshold, snapshot.IdleReachedAverage, options);
        var presencePenalty = Presence(snapshot.RecentSelfReplies, snapshot.RecentWindowMessages, options);

        var raw = relevanceScore + contentScore + pressureScore - presencePenalty;
        var frequency = Math.Clamp(snapshot.EffectiveFrequency, 0.0, 1.0);
        var factor = 0.5 + 0.5 * frequency;
        var final = Math.Max(0, (int)Math.Round(raw * factor, MidpointRounding.AwayFromZero));

        return new ReplyNecessityResult(final, Detail(final, raw, relevanceScore, relevanceReason, contentScore,
            contentReasons, pressureScore, presencePenalty, snapshot, frequency, factor, options), pressureScore);
    }

    private static (int Score, string Reason) Relevance(ReplyNecessitySnapshot snapshot, ReplyNecessityOptions o) =>
        snapshot.HasAt ? (o.RelevanceAt, "@") :
        snapshot.HasMention ? (o.RelevanceMention, "提及") :
        !snapshot.IsGroupChat ? (o.RelevanceDirect, "私聊") :
        snapshot.FocusActive ? (o.RelevanceFocus, "focus") :
        (0, "普通");

    private static List<string> Clean(IReadOnlyList<string> texts, ReplyNecessityOptions options) =>
        [.. texts.Select(text => StripNoise(text, options))];

    /// <summary>
    /// 剥掉引用外壳、@ 片段与纯占位内容，只留用户这一轮真正说的话。
    /// 不剥的话，一条被转发的长消息会以"长度"和"提问词"的名义凭空抬高分数。
    /// </summary>
    private static string StripNoise(string? text, ReplyNecessityOptions options)
    {
        var value = string.Join(' ', (text ?? "").Split([' ', '\t', '\r', '\n', '\f', '\v'],
            StringSplitOptions.RemoveEmptyEntries));
        if (value.StartsWith("@all", StringComparison.Ordinal)) return "";
        if (options.IgnoredPrefixes.Any(value.StartsWith) ||
            options.PlaceholderPrefixes.Any(value.StartsWith)) return "";

        value = ReplyPrefix.Replace(value, "").Trim();
        value = MentionToken.Replace(value, "").Trim();
        // 引用外壳里往往只剩被转发的整段消息；与上游同口径直接丢弃。
        if (value.StartsWith('[') && value.Contains("转发", StringComparison.Ordinal)) return "";
        return options.PlaceholderPrefixes.Any(value.StartsWith) ? "" : value;
    }

    private static (int Score, List<string> Reasons) ScoreContent(List<string> cleaned, string combined,
        bool isDirectContext, ReplyNecessitySnapshot snapshot, ReplyNecessityOptions o)
    {
        var score = 0;
        var reasons = new List<string>();

        if (cleaned.Any(t => LooksLikeQuestion(t, o)))
        {
            score += o.ContentQuestion;
            reasons.Add("问题");
        }

        var request = RequestReason(combined, isDirectContext, o);
        if (request.Length > 0)
        {
            score += o.ContentRequest;
            reasons.Add($"请求:{request}");
        }

        var opinion = OpinionReason(combined, isDirectContext, snapshot.CharacterNames, o);
        if (opinion.Length > 0)
        {
            score += o.ContentOpinion;
            reasons.Add($"征询:{opinion}");
        }

        var length = InputProbe.VisualLength(combined);
        if (length >= o.LongTextCharacters)
        {
            score += o.ContentLong;
            reasons.Add("长文本");
        }

        if (length >= o.VeryLongTextCharacters)
        {
            score += o.ContentVeryLong;
            reasons.Add("较长文本");
        }

        if (IsShortReactionBatch(cleaned, o))
        {
            score -= o.ContentShortReactionPenalty;
            reasons.Add("短反应");
        }

        return (score, reasons);
    }

    /// <summary>整批都只是"哈哈""嗯""？"时不要打断用户。空批也算，因为没有内容更不值得回复。</summary>
    private static bool IsShortReactionBatch(List<string> cleaned, ReplyNecessityOptions o)
    {
        if (cleaned.Count == 0) return true;
        if (cleaned.Any(t => t.Length > o.ShortReactionMaxCharacters)) return false;
        return cleaned.All(t => t.Length == 0 || o.ShortReactions.Contains(t));
    }

    private static bool LooksLikeQuestion(string text, ReplyNecessityOptions o)
    {
        if (text.Length == 0) return false;
        // "？？？什么？？？" 这种纯标点夹心不是提问，是起哄。
        if (PunctuationOnlyQuestion.IsMatch(text)) return false;
        if (o.QuestionTerms.Any(text.Contains)) return true;
        if (WhatQuestion.IsMatch(text)) return true;
        if (ParticleQuestion.IsMatch(text) && text.Length is >= 4 and <= 80) return true;
        return MarkedQuestion.IsMatch(text) && text.Length is >= 4 and <= 120;
    }

    private static string RequestReason(string text, bool isDirectContext, ReplyNecessityOptions o)
    {
        if (!isDirectContext && AddressesOtherAssistant(text, o)) return "";

        // "能不能"在群里极常出现在人与人的对话中，只有句首或对角色说时才算请求。
        var hits = o.DirectRequestTerms.Where(term => text.Contains(term, StringComparison.Ordinal) &&
            (term != "能不能" || isDirectContext ||
             text.StartsWith(term, StringComparison.Ordinal))).ToList();
        if (hits.Count > 0) return string.Join("/", hits);

        return isDirectContext
            ? string.Join("/", o.WeakRequestTerms.Where(text.Contains))
            : "";
    }

    private static bool AddressesOtherAssistant(string text, ReplyNecessityOptions o)
    {
        foreach (var name in o.OtherAssistantNames)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (!text.StartsWith(name, StringComparison.Ordinal)) continue;
            var rest = text[name.Length..];
            if (rest.Length == 0 || rest[0] is '，' or ',' or '、' or ' ' or '\t') return true;
        }

        return false;
    }

    private static string OpinionReason(string text, bool isDirectContext, IReadOnlyList<string> names,
        ReplyNecessityOptions o)
    {
        if (text.Contains("不怎么看", StringComparison.Ordinal)) return "";
        // 征询意见必须有指向：要么已经是对角色说话，要么文本里叫到了角色的名字。
        if (!isDirectContext && !names.Any(n => !string.IsNullOrWhiteSpace(n) &&
                                                text.Contains(n, StringComparison.Ordinal))) return "";

        var hits = o.OpinionTerms.Where(text.Contains).ToList();
        if (hits.Count > 0) return string.Join("/", hits);

        var views = Regex.Matches(text, @"怎么看");
        if (views.Count == 0) return "";
        var before = text[..views[0].Index];
        var after = text[(views[0].Index + views[0].Length)..];
        var addressed = before.Contains('你') || names.Any(n => !string.IsNullOrWhiteSpace(n) && before.Contains(n))
            || after.Contains('你') || names.Any(n => !string.IsNullOrWhiteSpace(n) && after.Contains(n));
        return addressed ? "怎么看" : "";
    }

    /// <summary>
    /// 压力分。阈值内按二次增长：一两三条消息不该有压力，积压到阈值附近才陡增。
    /// 超过阈值后转对数增长：再多消息也只是同一句话在重复，线性增长会让刷屏变成必然插话。
    /// </summary>
    private static int Pressure(int pendingCount, int threshold, bool idleReachedAverage, ReplyNecessityOptions o)
    {
        var ratio = Math.Max(0.0, pendingCount / (double)threshold);
        if (ratio <= 1.0)
        {
            var score = o.PressureAtThreshold * ratio * ratio;
            if (idleReachedAverage) score += o.PressureIdleBonus;
            return Math.Min(o.PressureAtThreshold, (int)Math.Round(score, MidpointRounding.AwayFromZero));
        }

        var overflow = ratio - 1.0;
        var span = Math.Max(0.0001, o.PressureFullRatio - 1.0);
        var progress = Math.Min(1.0, Math.Log(1 + overflow) / Math.Log(1 + span));
        var above = o.PressureAtThreshold + (int)Math.Round((o.PressureMax - o.PressureAtThreshold) * progress,
            MidpointRounding.AwayFromZero);
        return Math.Min(o.PressureMax, above);
    }

    /// <summary>
    /// 存在感惩罚。角色在最近窗口里说得越多，这一轮越该闭嘴——自我抑制是长期在线的角色
    /// 最容易被忽略的一条要求：没有它，一个私聊里角色会追着用户连发。
    /// </summary>
    private static int Presence(int selfReplies, int windowMessages, ReplyNecessityOptions o)
    {
        if (selfReplies <= 0 || windowMessages <= 0) return 0;
        var ratio = Math.Min(1.0, selfReplies / (double)windowMessages);
        if (ratio <= o.PresenceFreeRatio) return 0;

        var span = Math.Max(0.0001, o.PresenceFullRatio - o.PresenceFreeRatio);
        var progress = Math.Min(1.0, (ratio - o.PresenceFreeRatio) / span);
        return (int)Math.Round(o.PresencePenaltyMax * progress, MidpointRounding.AwayFromZero);
    }

    private static string Detail(int final, int raw, int relevance, string relevanceReason, int content,
        List<string> contentReasons, int pressure, int presence, ReplyNecessitySnapshot snapshot, double frequency,
        double factor, ReplyNecessityOptions o)
    {
        var parts = new List<string> { $"最终={final}", $"原始={raw}", $"阈值={o.TriggerScore}" };
        if (relevance != 0) parts.Add($"相关={relevance}({relevanceReason})");
        if (content != 0) parts.Add($"内容={content}({string.Join(',', contentReasons)})");
        parts.Add($"压力={pressure}/{snapshot.PendingCount}↑{snapshot.TriggerThreshold}");
        if (presence != 0)
            parts.Add($"存在感=-{presence}({snapshot.RecentSelfReplies}/{snapshot.RecentWindowMessages})");
        parts.Add($"频率={frequency:F3}×{factor:F2}");
        parts.Add($"空窗={snapshot.IdleFor.TotalSeconds:F0}s");
        return string.Join(' ', parts);
    }
}
