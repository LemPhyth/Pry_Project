using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class ShortcutNormalizationTests
{
    [Theory]
    [InlineData("Ctrl++E")]
    [InlineData("+Enter")]
    [InlineData("Enter+")]
    [InlineData("999999")]
    [InlineData("1")]
    [InlineData("A, B")]
    [InlineData("Ctrl+Control+E")]
    public void Rejects_malformed_gestures(string gesture) =>
        Assert.False(ShortcutGestureMatcher.IsValid(gesture));

    [Theory]
    [InlineData("Ctrl+Enter", "Control+Return")]
    [InlineData("Ctrl+Shift+E", " shift + control + e ")]
    [InlineData("Win+E", "Meta+E")]
    public void Rejects_semantically_equivalent_shortcuts(string first, string second)
    {
        var error = SettingsDraftService.ValidateShortcuts(new ShortcutSettings
        {
            Send = first,
            SendImmediately = second
        });
        Assert.Equal("快捷键冲突", error?.Title);
    }
}
