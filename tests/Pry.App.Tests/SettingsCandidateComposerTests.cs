using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class SettingsCandidateComposerTests
{
    [Fact]
    public void Compose_preserves_unedited_preferences_and_normalizes_active_tuning()
    {
        var custom = new ModelProfile { Id = "custom", DisplayName = "Custom" };
        var source = new UserPreferences
        {
            ActiveConversationId = "room", CustomModels = [custom],
            UserProfile = new UserProfilePreferences { DisplayName = "User" }
        };
        var theme = new ThemePreferences { AccentColor = "#112233" };
        var draft = new SettingsCandidateDraft("text", "vision", "speech",
            new Dictionary<string, ModelTuningPreferences>
            {
                ["text"] = new() { ActiveModelId = "stale", EnableThinking = true, Temperature = .4 }
            }, new TurnTakingSettings { MaxReplyMessages = 5 },
            new DesktopPetPreferences { Enabled = true }, theme,
            new ShortcutSettings { Send = "Ctrl+Enter" }, "character");
        var result = SettingsCandidateComposer.Compose(source, draft);
        Assert.Equal("text", result.ActiveModelId);
        Assert.Equal("text", result.ModelTuning.ActiveModelId);
        Assert.Same(result.ModelTuning, result.ModelTunings["text"]);
        Assert.True(result.EnableThinking);
        Assert.Equal("room", result.ActiveConversationId);
        Assert.Same(custom, Assert.Single(result.CustomModels));
        Assert.Equal("User", result.UserProfile.DisplayName);
        Assert.Same(theme, result.Theme);
    }

    [Fact]
    public void Missing_tuning_gets_safe_active_default_without_mutating_input()
    {
        var tunings = new Dictionary<string, ModelTuningPreferences>();
        var result = SettingsCandidateComposer.Compose(new UserPreferences(),
            new SettingsCandidateDraft("text", null, null, tunings, new(), new(), new(), new(), null));
        Assert.Empty(tunings);
        Assert.Equal("text", result.ModelTunings["text"].ActiveModelId);
        Assert.False(result.EnableThinking);
    }

    [Fact]
    public void Empty_text_model_is_rejected() => Assert.Throws<ArgumentException>(() =>
        SettingsCandidateComposer.Compose(new UserPreferences(),
            new SettingsCandidateDraft(" ", null, null, new Dictionary<string, ModelTuningPreferences>(),
                new(), new(), new(), new(), null)));
}
