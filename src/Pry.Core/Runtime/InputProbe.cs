using System.Text;

namespace Pry.Core.Runtime;

/// <summary>
/// 输入文本的轻量启发式判断。1.0 只使用时间规则 + 少量启发式，不做语义模型；
/// 这些判断影响"什么时候形成一个 Turn"，不决定角色该怎么说话。
/// </summary>
public static class InputProbe
{
    private static readonly char[] TerminalPunctuation = ['。', '！', '!', '？', '?', '…', '~', '～', '’', '"', ')', '】'];
    private static readonly string[] TrailingConnectors =
        [",", "，", "、", "：", ":", ";", "；", "然后", "而且", "但是", "因为", "所以", "如果", "就是", "还有", "的", "是", "我", "和", "跟", "以", "把", "被"];
    private static readonly string[] QuestionSignals = ["?", "？", "吗", "呢", "怎么", "为什么", "是不是", "可不可以", "行不行", "什么", "谁", "哪里", "几点"];
    private static readonly HashSet<string> AckOnly = new(StringComparer.OrdinalIgnoreCase)
    {"嗯", "嗯嗯", "嗯呢", "哦", "哦哦", "噢", "对", "对啊", "对呀", "是的", "好", "好的", "好滴", "行", "可以", "收到", "知道了", "了解", "哈哈", "哈哈哈", "哈哈哈哈", "嘿", "呵呵", "6", "666", "草", "笑死", "牛", "厉害", "ok", "okok", "好叭", "得嘞"};

    public static string Normalize(string? text) => string.IsNullOrWhiteSpace(text) ? "" : text.Trim();

    /// <summary>句子看起来还没说完：以连接符/标点收尾，或过短且没有终止标点。</summary>
    public static bool LooksIncomplete(string? text)
    {
        var value = Normalize(text);
        if (value.Length == 0) return false;
        if (EndsWithOpeningBracket(value)) return true;
        var tail = TrimTrailingPunctuation(value);
        if (tail.Length == 0) return true;
        if (TrailingConnectors.Any(x => tail.EndsWith(x, StringComparison.Ordinal)))
        {
            // 单个"的/是/我"这类结尾只在短句上视为半句，长句末尾的"…的。"是完整表达。
            if (tail.Length <= 24 || value.Length <= 24) return true;
        }
        if (value.Length <= 3 && !value.Any(c => TerminalPunctuation.Contains(c))) return true;
        return false;
    }

    public static bool AsksQuestion(string? text)
    {
        var value = Normalize(text);
        return value.Length > 0 && QuestionSignals.Any(value.Contains);
    }

    /// <summary>是否像在对角色本人说话。只用于 Runtime 的节奏判断，不是社交决策。</summary>
    public static bool AddressesCharacter(string? text, IReadOnlyList<string> characterNames, string userName = "你")
    {
        var value = Normalize(text);
        if (value.Length == 0) return false;
        if (value.Contains('@')) return true;
        foreach (var name in characterNames)
            if (!string.IsNullOrWhiteSpace(name) && value.Contains(name, StringComparison.OrdinalIgnoreCase)) return true;
        return value.StartsWith(userName, StringComparison.Ordinal) || value.Contains($"{userName}怎么")
            || value.Contains($"{userName}为什么") || value.Contains($"{userName}觉得");
    }

    public static bool IsLightweightAcknowledgement(string? text, string? stickerId)
    {
        if (!string.IsNullOrWhiteSpace(stickerId)) return false;
        var value = TrimTrailingPunctuation(Normalize(text));
        return value.Length > 0 && value.Length <= 8 && AckOnly.Contains(value);
    }

    /// <summary>
    /// 感知长度：CJK 字符按 1 计，拉丁字符按 0.5 计。用于延迟推导，避免用固定字符数切割气泡。
    /// </summary>
    public static double VisualLength(string? text)
    {
        var value = Normalize(text);
        double length = 0;
        foreach (var c in value)
        {
            if (char.IsWhiteSpace(c)) continue;
            length += c > 0x2E80 ? 1 : 0.5;
        }
        return length;
    }

    public static string FirstLine(string? text, int maxCharacters = 120)
    {
        var value = Normalize(text);
        if (value.Length == 0) return "";
        var line = value.Split('\n')[0].Trim();
        return line.Length <= maxCharacters ? line : string.Concat(line.AsSpan(0, maxCharacters), "…");
    }

    public static string Summarize(IReadOnlyList<RuntimeUserInput> inputs)
    {
        if (inputs.Count == 0) return "";
        var builder = new StringBuilder();
        foreach (var input in inputs)
        {
            var text = Normalize(input.Text);
            if (text.Length > 0) _ = builder.Append(text).Append('\n');
            else if (!string.IsNullOrWhiteSpace(input.StickerId)) _ = builder.Append($"[sticker:{input.StickerId}]").Append('\n');
            foreach (var attachment in input.SafeAttachments)
                _ = builder.Append($"[attachment:{attachment.Kind}:{attachment.Name}]").Append('\n');
        }
        return builder.ToString().TrimEnd();
    }

    private static string TrimTrailingPunctuation(string value) =>
        value.TrimEnd(' ', '\t', '\r', '\n', '。', '！', '!', '？', '?', '…', '~', '～');

    private static bool EndsWithOpeningBracket(string value)
    {
        var opened = value.Count(c => c is '(' or '（' or '[' or '【' or '「');
        var closed = value.Count(c => c is ')' or '）' or ']' or '】' or '」');
        return opened > closed;
    }
}
