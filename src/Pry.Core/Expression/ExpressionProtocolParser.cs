using System.Text.Json;
using Pry.Core.Models;

namespace Pry.Core.Expression;

public sealed class ExpressionProtocolParser
{
    public const string StartMarker = "[PRY_EXPRESSION]";
    public const string EndMarker = "[/PRY_EXPRESSION]";

    public static (ExpressionIntent? Intent, string Text) ParseHeader(string content,
        IReadOnlyCollection<StickerDefinition> allowedStickers)
    {
        if (!content.StartsWith(StartMarker, StringComparison.Ordinal)) return (null, content);
        var end = content.IndexOf(EndMarker, StringComparison.Ordinal);
        if (end < 0) return (null, content);
        try
        {
            var json = content[StartMarker.Length..end];
            var raw = JsonSerializer.Deserialize<RawIntent>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (raw is null) return (null, content[(end + EndMarker.Length)..].TrimStart('\r', '\n'));
            var stickerId = allowedStickers.Any(x => x.Enabled && x.Id == raw.StickerId) ? raw.StickerId : null;
            return (new ExpressionIntent(raw.Emotion, Math.Clamp(raw.Intensity, 0, 1), stickerId,
                raw.Live2DExpression, raw.Live2DMotion), content[(end + EndMarker.Length)..].TrimStart('\r', '\n'));
        }
        catch (JsonException) { return (null, content[(end + EndMarker.Length)..].TrimStart('\r', '\n')); }
    }

    private sealed record RawIntent(string? Emotion, double Intensity, string? StickerId,
        string? Live2DExpression, string? Live2DMotion);
}
