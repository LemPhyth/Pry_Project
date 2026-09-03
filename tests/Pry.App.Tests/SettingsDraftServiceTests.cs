using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class SettingsDraftServiceTests
{
    [Theory]
    [InlineData(3, 2, 100, 200, "#B148C6", "最多消息数不能小于最少消息数。")]
    [InlineData(1, 2, 300, 200, "#B148C6", "最长延迟不能小于最短延迟。")]
    [InlineData(1, 2, 100, 200, "purple", "请填写 #RRGGBB 格式的颜色，例如 #B148C6。")]
    [InlineData(1, 2, 100, 200, "#FFF", "请填写 #RRGGBB 格式的颜色，例如 #B148C6。")]
    public void Validate_reports_specific_input_error(int minReplies, int maxReplies,
        decimal minDelay, decimal maxDelay, string accent, string message)
    {
        var error = SettingsDraftService.Validate(minReplies, maxReplies, minDelay, maxDelay, accent);

        Assert.NotNull(error);
        Assert.Equal(message, error.Message);
    }

    [Fact]
    public void Validate_accepts_consistent_ranges_and_six_digit_hex_color() =>
        Assert.Null(SettingsDraftService.Validate(1, 4, 450, 3200, " #06A6a6 "));

    [Fact]
    public void ValidateShortcuts_rejects_invalid_and_conflicting_bindings()
    {
        var invalid = SettingsDraftService.ValidateShortcuts(new ShortcutSettings { Send = "Foo+E" });
        var conflict = SettingsDraftService.ValidateShortcuts(new ShortcutSettings
        {
            Send = "Ctrl+Enter",
            SendImmediately = "ctrl+enter"
        });

        Assert.Equal("快捷键无效", invalid?.Title);
        Assert.Equal("快捷键冲突", conflict?.Title);
    }

    [Fact]
    public void ValidateShortcuts_accepts_default_bindings() =>
        Assert.Null(SettingsDraftService.ValidateShortcuts(new ShortcutSettings()));

    [Fact]
    public void BuildTheme_clamps_values_and_copies_mutable_collections()
    {
        var history = new List<string> { "background.png" };
        var displays = new Dictionary<string, ImageDisplayPreferences>
        {
            ["background.png"] = new() { Zoom = 1.5 }
        };
        var source = new ThemePreferences { CharacterAvatarPath = "character.png" };

        var result = SettingsDraftService.BuildTheme(source, new ThemeSettingsDraft
        {
            BackgroundHistory = history,
            BackgroundDisplays = displays,
            UserAvatarHistory = [],
            UserAvatarDisplays = new Dictionary<string, ImageDisplayPreferences>(),
            ThemeModeIndex = 2,
            AccentColor = " #B148C6 ",
            BackgroundDimOpacity = 2,
            BackgroundImageOpacity = -1,
            BackgroundBlurRadius = 80,
            AvatarSize = 10,
            BubbleFontSize = 30,
            BubbleMaxWidth = 100,
            BubbleSpacing = 80
        });
        history.Add("later.png");
        displays.Clear();

        Assert.Equal("light", result.ThemeMode);
        Assert.Equal("#B148C6", result.AccentColor);
        Assert.Equal("character.png", result.CharacterAvatarPath);
        Assert.Equal(.85, result.BackgroundDimOpacity);
        Assert.Equal(0, result.BackgroundImageOpacity);
        Assert.Equal(32, result.BackgroundBlurRadius);
        Assert.Equal("blur", result.BackgroundBlurMode);
        Assert.Equal(28, result.AvatarSize);
        Assert.Equal(24, result.BubbleFontSize);
        Assert.Equal(280, result.BubbleMaxWidth);
        Assert.Equal(36, result.BubbleSpacing);
        Assert.Single(result.BackgroundHistory);
        Assert.Single(result.BackgroundDisplays);
    }
}
