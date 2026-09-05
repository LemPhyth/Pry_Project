using System.Text.RegularExpressions;
using Pry.Core.Models;

namespace Pry.App.Services;

public static partial class SettingsDraftService
{
    public static SettingsValidationError? Validate(int minimumReplies, int maximumReplies,
        decimal minimumDelayMs, decimal maximumDelayMs, string? accentColor)
    {
        if (maximumReplies < minimumReplies)
            return new SettingsValidationError("参数无效", "最多消息数不能小于最少消息数。");
        if (maximumDelayMs < minimumDelayMs)
            return new SettingsValidationError("参数无效", "最长延迟不能小于最短延迟。");
        if (string.IsNullOrWhiteSpace(accentColor) || !HexColorPattern().IsMatch(accentColor.Trim()))
            return new SettingsValidationError("强调色无效", "请填写 #RRGGBB 格式的颜色，例如 #B148C6。");
        return null;
    }

    public static SettingsValidationError? ValidateShortcuts(ShortcutSettings shortcuts)
    {
        var entries = new[]
        {
            ("发送", shortcuts.Send), ("立即回复", shortcuts.SendImmediately),
            ("换行", shortcuts.NewLine), ("打断回复", shortcuts.CancelReply),
            ("新对话", shortcuts.NewConversation), ("打开表情", shortcuts.OpenStickers),
            ("角色卡", shortcuts.OpenCharacterEditor)
        };
        var invalid = entries.FirstOrDefault(entry => !ShortcutGestureMatcher.IsValid(entry.Item2));
        if (invalid != default)
            return new SettingsValidationError("快捷键无效", $"“{invalid.Item1}”的快捷键格式无法识别。");
        var conflict = entries.GroupBy(entry => ShortcutGestureMatcher.Normalize(entry.Item2), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        return conflict is null
            ? null
            : new SettingsValidationError("快捷键冲突", $"快捷键 {conflict.Key} 被多个操作重复使用。");
    }

    public static ThemePreferences BuildTheme(ThemePreferences source, ThemeSettingsDraft draft) => new()
    {
        BackgroundImagePath = draft.BackgroundImagePath,
        BackgroundHistory = draft.BackgroundHistory.ToArray(),
        BackgroundDisplays = new Dictionary<string, ImageDisplayPreferences>(draft.BackgroundDisplays),
        CharacterAvatarPath = source.CharacterAvatarPath,
        UserAvatarPath = draft.UserAvatarPath,
        UserAvatarHistory = draft.UserAvatarHistory.ToArray(),
        UserAvatarDisplays = new Dictionary<string, ImageDisplayPreferences>(draft.UserAvatarDisplays),
        ThemeMode = draft.ThemeModeIndex switch { 1 => "dark", 2 => "light", _ => "system" },
        AccentColor = draft.AccentColor.Trim(),
        UseGlassEffects = draft.UseGlassEffects,
        LiveSidebarResize = draft.LiveSidebarResize,
        BackgroundImageOpacity = Math.Clamp(draft.BackgroundImageOpacity, 0, 1),
        BackgroundDimOpacity = Math.Clamp(draft.BackgroundDimOpacity, 0, .85),
        BackgroundBlurMode = draft.BackgroundBlurRadius > 0 ? "blur" : "none",
        BackgroundBlurRadius = Math.Clamp(draft.BackgroundBlurRadius, 0, 32),
        AvatarSize = Math.Clamp(draft.AvatarSize, 28, 76),
        BubbleFontSize = Math.Clamp(draft.BubbleFontSize, 11, 24),
        BubbleMaxWidth = Math.Clamp(draft.BubbleMaxWidth, 280, 900),
        BubbleSpacing = Math.Clamp(draft.BubbleSpacing, 2, 36)
    };

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex HexColorPattern();
}

public sealed record SettingsValidationError(string Title, string Message);

public sealed record ThemeSettingsDraft
{
    public string? BackgroundImagePath { get; init; }
    public required IReadOnlyList<string> BackgroundHistory { get; init; }
    public required IReadOnlyDictionary<string, ImageDisplayPreferences> BackgroundDisplays { get; init; }
    public string? UserAvatarPath { get; init; }
    public required IReadOnlyList<string> UserAvatarHistory { get; init; }
    public required IReadOnlyDictionary<string, ImageDisplayPreferences> UserAvatarDisplays { get; init; }
    public int ThemeModeIndex { get; init; }
    public required string AccentColor { get; init; }
    public bool UseGlassEffects { get; init; }
    public bool LiveSidebarResize { get; init; }
    public double BackgroundDimOpacity { get; init; }
    public double BackgroundImageOpacity { get; init; }
    public double BackgroundBlurRadius { get; init; }
    public double AvatarSize { get; init; }
    public double BubbleFontSize { get; init; }
    public double BubbleMaxWidth { get; init; }
    public double BubbleSpacing { get; init; }
}
