using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class ThemeAppearanceEditorTests
{
    [Fact]
    public void BuildTheme_preserves_controls_and_asset_drafts()
    {
        var source = new ThemePreferences
        {
            ThemeMode = "light", AccentColor = "#123456", UseGlassEffects = false,
            LiveSidebarResize = true, BackgroundDimOpacity = .5, BackgroundImageOpacity = .8,
            BackgroundBlurRadius = 5, AvatarSize = 60, BubbleFontSize = 18,
            BubbleMaxWidth = 700, BubbleSpacing = 12, CharacterAvatarPath = "character.png"
        };
        var ui = new SettingsUiFactory();
        var editor = new ThemeAppearanceEditor(source, ui);
        var background = Draft("background.png");
        background.StoreDisplay(new() { Zoom = 2 });
        var result = editor.BuildTheme(source, background, Draft("avatar.png"));
        Assert.Equal("light", result.ThemeMode);
        Assert.Equal("#123456", result.AccentColor);
        Assert.False(result.UseGlassEffects);
        Assert.True(result.LiveSidebarResize);
        Assert.Equal(.5, result.BackgroundDimOpacity);
        Assert.Equal(.8, result.BackgroundImageOpacity);
        Assert.Equal(5, result.BackgroundBlurRadius);
        Assert.Equal(60, result.AvatarSize);
        Assert.Equal(18, result.BubbleFontSize);
        Assert.Equal(700, result.BubbleMaxWidth);
        Assert.Equal(12, result.BubbleSpacing);
        Assert.Equal("background.png", result.BackgroundImagePath);
        Assert.Equal(2, result.BackgroundDisplays["background.png"].Zoom);
        Assert.Equal("avatar.png", result.UserAvatarPath);
        Assert.Equal("character.png", result.CharacterAvatarPath);
        Assert.Equal(4, ui.Cards.Count);
    }

    [Fact]
    public void Changing_background_value_notifies_and_updates_draft()
    {
        var source = new ThemePreferences();
        var editor = new ThemeAppearanceEditor(source, new SettingsUiFactory());
        var notifications = 0;
        editor.Changed += () => notifications++;
        editor.BackgroundBlur.Value = 12;
        Assert.True(notifications > 0);
        var result = editor.BuildTheme(source, Draft(null), Draft(null));
        Assert.Equal(12, result.BackgroundBlurRadius);
        Assert.Equal("blur", result.BackgroundBlurMode);
    }

    private static ThemeImageDraft Draft(string? path) => new(path, [],
        new Dictionary<string, ImageDisplayPreferences>(), _ => true);
}
