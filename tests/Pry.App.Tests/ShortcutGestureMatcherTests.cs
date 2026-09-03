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
    [InlineData(Key.E, KeyModifiers.None, "Foo+E")]
    public void Rejects_wrong_key_or_modifiers(Key key, KeyModifiers modifiers, string gesture) =>
        Assert.False(ShortcutGestureMatcher.Matches(key, modifiers, gesture));

    [Theory]
    [InlineData("Enter")]
    [InlineData("Ctrl+Shift+C")]
    [InlineData("Win+E")]
    public void Accepts_supported_gesture_formats(string gesture) =>
        Assert.True(ShortcutGestureMatcher.IsValid(gesture));

    [Theory]
    [InlineData("")]
    [InlineData("Foo+E")]
    [InlineData("Ctrl+NotAKey")]
    public void Rejects_unsupported_gesture_formats(string gesture) =>
        Assert.False(ShortcutGestureMatcher.IsValid(gesture));
}
