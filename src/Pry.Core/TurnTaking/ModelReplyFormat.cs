using System.Text.Json;

namespace Pry.Core.TurnTaking;

internal enum ModelReplyShape
{
    /// <summary>模型没有产出任何可见内容。</summary>
    Empty,

    /// <summary>剥掉信封与内部草稿后剩下的散文正文：模型忽略了结构化输出要求时走这一档。</summary>
    PlainText,

    /// <summary>待解析的 JSON 计划负载，可能来自答复信封、代码围栏或裸 JSON 体。</summary>
    StructuredPlan,

    /// <summary>整段都是推理信封或草稿，没有兑现出正文。</summary>
    OnlyInternalDraft,
}

/// <summary>
/// 角色输出格式的知识只写在这一处（判据：DEF-P1WALK-01 完成标准 ③，传输层与界面不得自带形状识别）。
/// 判据全是结构信号（信封标签、JSON 形状、代码围栏），不按字数放宽，也不做语义判定：
/// 抽取失败宁可判 <see cref="ModelReplyShape.OnlyInternalDraft"/>，也不把模型原始输出当可见答复——
/// 一次泄漏会逐条落库、进界面历史并被后续轮次当上文引用，事后撤不回；少发一条只是这一轮没说话。
/// 标签字面量按字符码拼装，本文件里不出现会被下游工具误当成标记的原文。
/// </summary>
internal static class ModelReplyFormat
{
    private const char Begin = (char)0x3C;
    private const char End = (char)0x3E;
    private const char Slash = (char)0x2F;

    /// <summary>推理段标签名。带 `antml:` 前缀的是 Claude 系 chat template 原样吐出的形状。</summary>
    private static readonly string[] ReasoningNames =
        ["think", "thinking", "thought", "thoughts", "reasoning", "antml:think", "antml:thinking", "antml:reasoning"];

    /// <summary>按契约只放分析过程的信封：整段丢弃。</summary>
    private static readonly string[] DraftEnvelopeNames = ["analysis", "analyse", "analyze", "plan", "scratchpad"];

    /// <summary>兑现可见答复的信封：只剥标签，保留信封体。</summary>
    private static readonly string[] ReplyEnvelopeNames = ["response", "answer", "reply"];

    private static readonly string[] InvisibleNames = [.. ReasoningNames, .. DraftEnvelopeNames];

    private readonly record struct Tag(int Start, int End, string Name, bool Closing);

    /// <summary>
    /// 把一次生成的原始文本分成"可见答复"与"内部草稿"。配对的答复信封优先：契约规定答复写在信封里，
    /// 信封外的一切（自我分析、复述指令、被裁断的半截草稿）都算草稿。没有信封时才退回去裁推理段——
    /// 闭合的推理标签之前一律算草稿（第七轮走查的 W1 正是这个形状），起始标签之后一律算草稿，
    /// 未闭合就裁到文本末尾。
    /// </summary>
    public static (ModelReplyShape Shape, string Text) Extract(string? raw)
    {
        var text = (raw ?? string.Empty).Trim();
        if (text.Length == 0) return (ModelReplyShape.Empty, string.Empty);

        var source = ExtractReplyEnvelopeBody(text) ?? text;
        var visible = StripInvisibleRegions(source);
        var plan = FindJsonPayload(visible);
        if (plan is not null) return (ModelReplyShape.StructuredPlan, plan);

        var prose = StripTags(visible, ReplyEnvelopeNames).Trim();
        if (prose.Length > 0 && !LooksLikeUnparsedPlan(prose)) return (ModelReplyShape.PlainText, prose);
        return ContainsAnyEnvelope(text) || LooksLikeUnparsedPlan(text)
            ? (ModelReplyShape.OnlyInternalDraft, string.Empty)
            : (ModelReplyShape.Empty, string.Empty);
    }

    /// <summary>
    /// 认不出但明显"想当计划"的残体：以括号或代码围栏开头、或留着计划键。第九、十一轮漏到界面上的
    /// 就是这个形状——它没有信封标签，若按散文放行就等于把没解析的 JSON 当答复落库。
    /// </summary>
    private static bool LooksLikeUnparsedPlan(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Length > 0 && (trimmed[0] == '{' || trimmed[0] == '['
            || trimmed.StartsWith("```", StringComparison.Ordinal)
            || trimmed.Contains("\"messages\"", StringComparison.Ordinal));
    }

    /// <summary>取最后一个配对的答复信封体：草稿段里被逐字复读的示例信封不该顶掉真答复。</summary>
    private static string? ExtractReplyEnvelopeBody(string text)
    {
        string? body = null;
        var index = 0;
        while (index < text.Length)
        {
            var found = FindFirstTag(text, index, ReplyEnvelopeNames);
            if (found is null) break;
            var open = found.Value;
            if (open.Closing)
            {
                index = open.End;
                continue;
            }
            var close = FindClosingTag(text, open.End, open.Name);
            if (close is null) break;
            body = text[open.End..close.Value.Start];
            index = close.Value.End;
        }
        return body;
    }

    /// <summary>结构化条目的正文同样不得带推理段；剥不干净就整条丢弃，不做按字数放行的兜底。</summary>
    public static string? SanitizeMessageContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var stripped = StripInvisibleRegions(content);
        if (stripped.Trim().Length == 0) return null;
        return ContainsTag(stripped, ReasoningNames) ? null : stripped.Trim();
    }

    /// <summary>
    /// 散文答复只保留第一条完整可发的行。首行超出预算时判空而不是截断，也不改取后面的短句：
    /// 半截话与内部草稿落在同一列里长得很像，宁可这一轮不说话。
    /// </summary>
    public static string ToVisiblePlainText(string body, int maximumCharacters)
    {
        foreach (var rawLine in body.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0) continue;
            return line.Length > maximumCharacters || ContainsTag(line, ReasoningNames) ? string.Empty : line;
        }
        return string.Empty;
    }

    private static string StripInvisibleRegions(string text)
    {
        var current = text;
        for (var pass = 0; pass < 16 && current.Length > 0; pass++)
        {
            var found = FindFirstTag(current, 0, InvisibleNames);
            if (found is null) break;
            var tag = found.Value;
            if (tag.Closing)
            {
                current = current[tag.End..];
                continue;
            }
            var matching = FindClosingTag(current, tag.End, tag.Name);
            var regionEnd = matching?.End ?? current.Length;
            current = current[..tag.Start] + current[regionEnd..];
        }
        return current;
    }

    private static string StripTags(string text, string[] names)
    {
        var current = text;
        for (var pass = 0; pass < 16 && current.Length > 0; pass++)
        {
            var tag = FindFirstTag(current, 0, names);
            if (tag is null) break;
            current = current[..tag.Value.Start] + current[tag.Value.End..];
        }
        return current;
    }

    /// <summary>
    /// 找第一个可解析、且形如答复计划的 JSON 负载：整段被代码围栏包着时先剥围栏，否则扫描第一个
    /// 括号配平的片段。正文里引用的配置片段也是合法 JSON，所以还要过一遍计划形状；
    /// 解析不了的 JSON 形状一律判 null，由调用方走拒绝档，绝不再回落成原文。
    /// </summary>
    private static string? FindJsonPayload(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0) return null;
        var unwrapped = UnwrapFence(trimmed).Trim();
        if (LooksLikePlan(unwrapped)) return unwrapped;
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] != '{' && text[index] != '[') continue;
            var candidate = ReadBalanced(text, index);
            if (candidate is not null && LooksLikePlan(candidate)) return candidate;
        }
        return null;
    }

    private static bool LooksLikePlan(string value)
    {
        if (value.Length < 2 || (value[0] != '{' && value[0] != '[')) return false;
        try
        {
            using var document = JsonDocument.Parse(value);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
                return root.TryGetProperty("messages", out var messages) && messages.ValueKind == JsonValueKind.Array;
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0) return false;
            foreach (var item in root.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String) continue;
                if (item.ValueKind != JsonValueKind.Object) return false;
                if (!item.TryGetProperty("content", out _) && !item.TryGetProperty("stickerId", out _) &&
                    !item.TryGetProperty("type", out _)) return false;
            }
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>按括号深度与字符串态取一段配平的 JSON；未配平返回 null（多半是被截断的流）。</summary>
    private static string? ReadBalanced(string text, int start)
    {
        var depth = 0;
        var inString = false;
        var escaped = false;
        for (var index = start; index < text.Length; index++)
        {
            var character = text[index];
            if (inString)
            {
                if (escaped) escaped = false;
                else if (character == '\\') escaped = true;
                else if (character == '"') inString = false;
                continue;
            }
            switch (character)
            {
                case '"': inString = true; break;
                case '{': case '[': depth++; break;
                case '}': case ']':
                    depth--;
                    if (depth == 0) return text[start..(index + 1)];
                    if (depth < 0) return null;
                    break;
            }
        }
        return null;
    }

    private static string UnwrapFence(string text)
    {
        if (!text.StartsWith("```", StringComparison.Ordinal)) return text;
        var firstLineEnd = text.IndexOf('\n');
        if (firstLineEnd >= 0) text = text[(firstLineEnd + 1)..];
        var fenceEnd = text.LastIndexOf("```", StringComparison.Ordinal);
        return fenceEnd >= 0 ? text[..fenceEnd] : text;
    }

    private static bool ContainsAnyEnvelope(string text) =>
        ContainsTag(text, InvisibleNames) || ContainsTag(text, ReplyEnvelopeNames);

    private static bool ContainsTag(string text, string[] names) => FindFirstTag(text, 0, names) is not null;

    private static Tag? FindClosingTag(string text, int from, string name)
    {
        var index = from;
        while (index < text.Length)
        {
            var tag = FindFirstTag(text, index, [name]);
            if (tag is null) return null;
            if (tag.Value.Closing) return tag;
            index = tag.Value.End;
        }
        return null;
    }

    /// <summary>
    /// 找到下一个可识别标签。闭合符允许缺失（模型被截断时最常见），此时标签尾算到文本末尾，
    /// 因为"只剩半截信封"正是草稿泄漏的主要形状。
    /// </summary>
    private static Tag? FindFirstTag(string text, int from, string[] names)
    {
        for (var index = from; index < text.Length; index++)
        {
            if (text[index] != Begin) continue;
            var cursor = index + 1;
            var closing = cursor < text.Length && text[cursor] == Slash;
            if (closing) cursor++;
            var name = MatchName(text, cursor, names);
            if (name is null) continue;
            var after = cursor + name.Length;
            if (after < text.Length && text[after] != End && text[after] != Slash && !char.IsWhiteSpace(text[after]))
                continue;
            if (after < text.Length && text[after] == Slash) after += 2;
            var terminator = text.IndexOf(End, after);
            return new Tag(index, terminator < 0 ? text.Length : terminator + 1, name, closing);
        }
        return null;
    }

    private static string? MatchName(string text, int start, string[] names)
    {
        string? longest = null;
        foreach (var candidate in names)
        {
            if (candidate.Length <= (longest?.Length ?? 0)) continue;
            if (start + candidate.Length > text.Length ||
                string.Compare(text, start, candidate, 0, candidate.Length, StringComparison.OrdinalIgnoreCase) != 0)
                continue;
            // 名字后面还接着字母或数字时它不是一个完整标签名：半截的 `thinkin` 会前缀命中 `think`，
            // 而 W3 落库的那条文本正是这样收尾的，判成完整标签就等于放过整段草稿。
            if (start + candidate.Length < text.Length && IsNameCharacter(text[start + candidate.Length])) continue;
            longest = candidate;
        }
        if (longest is not null) return longest;
        // 流被裁断时标签常停在半截：只有整段文本以已知标签名的前缀收尾时才算命中，正文中间的半截不算。
        var tail = text.Length - start;
        if (tail <= 0) return null;
        string? truncated = null;
        foreach (var candidate in names)
        {
            if (candidate.Length <= (truncated?.Length ?? 0) || candidate.Length <= tail) continue;
            if (string.Compare(text, start, candidate, 0, tail, StringComparison.OrdinalIgnoreCase) == 0)
                truncated = candidate[..tail];
        }
        return truncated;
    }

    private static bool IsNameCharacter(char value) => char.IsLetterOrDigit(value) || value is ':' or '-' or '_';
}
