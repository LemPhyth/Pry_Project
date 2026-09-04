using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class ThemePreviewControllerTests
{
    [Fact]
    public void Flush_uses_latest_valid_draft()
    {
        ThemePreferences? draft = null;
        ThemePreferences? applied = null;
        using var preview = new ThemePreviewController(() => draft, value => applied = value, () => { });
        preview.Flush();
        Assert.Null(applied);
        draft = new ThemePreferences { AccentColor = "#112233" };
        preview.Flush();
        Assert.Same(draft, applied);
    }

    [Fact]
    public void Dispose_rolls_back_once_and_prevents_late_preview()
    {
        var applied = 0;
        var rolledBack = 0;
        var preview = new ThemePreviewController(() => new(), _ => applied++, () => rolledBack++);
        preview.Dispose();
        preview.Request();
        preview.Flush();
        preview.Dispose();
        Assert.Equal(1, rolledBack);
        Assert.Equal(0, applied);
    }

    [Fact]
    public void Commit_prevents_rollback_and_further_preview()
    {
        var applied = 0;
        var rolledBack = false;
        var preview = new ThemePreviewController(() => new(), _ => applied++, () => rolledBack = true);
        preview.Flush();
        preview.Commit();
        preview.Flush();
        preview.Dispose();
        Assert.False(rolledBack);
        Assert.Equal(1, applied);
    }

    [Fact]
    public void Theme_only_rollback_preserves_other_updated_preferences()
    {
        var preferences = new UserPreferences();
        var original = preferences.Theme;
        var preview = new ThemePreviewController(() => new() { ThemeMode = "light" },
            theme => preferences = preferences with { Theme = theme },
            () => preferences = preferences with { Theme = original });
        preview.Flush();
        preferences = preferences with { ActiveSpeechModelId = "new-speech" };
        preview.Dispose();
        Assert.Same(original, preferences.Theme);
        Assert.Equal("new-speech", preferences.ActiveSpeechModelId);
    }
}
