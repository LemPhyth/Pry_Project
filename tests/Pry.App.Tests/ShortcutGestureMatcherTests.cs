using Avalonia.Input;
using Pry.App.Services;
using Xunit;

namespace Pry.App.Tests;

public sealed class ShortcutGestureMatcherTests
{
    [Theory]
    [InlineData(Key.Enter, KeyModifiers.None, "Enter")]
    [InlineData(Key.Return, KeyModifiers.Shift, "Shift+Enter")]
    [InlineData(Key.E, KeyModifiers.Control, "Ctrl+E")]
    [InlineData(Key.C, KeyModifiers.Control | KeyModifiers.Shift, "Ctrl+Shift+C")]
    public void Matches_valid_gestures(Key key, KeyModifiers modifiers, string gesture) =>
        Assert.True(ShortcutGestureMatcher.Matches(key, modifiers, gesture));

    [Theory]
    [InlineData(Key.Enter, KeyModifiers.Control, "Enter")]
    [InlineData(Key.E, KeyModifiers.None, "Ctrl+E")]
    [InlineData(Key.C, KeyModifiers.Control, "Ctrl+Shift+C")]
    [InlineData(Key.Enter, KeyModifiers.None, "")]
    public void Rejects_wrong_key_or_modifiers(Key key, KeyModifiers modifiers, string gesture) =>
        Assert.False(ShortcutGestureMatcher.Matches(key, modifiers, gesture));
}
