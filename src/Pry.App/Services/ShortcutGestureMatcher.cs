using Avalonia.Input;

namespace Pry.App.Services;

public static class ShortcutGestureMatcher
{
    public static bool Matches(Key key, KeyModifiers actualModifiers, string gesture)
    {
        if (!TryParse(gesture, out var expectedKey, out var expectedModifiers)) return false;
        var keyMatches = expectedKey is Key.Enter or Key.Return
            ? key is Key.Enter or Key.Return
            : key == expectedKey;
        return keyMatches && actualModifiers == expectedModifiers;
    }

    public static bool IsValid(string gesture) => TryParse(gesture, out _, out _);

    private static bool TryParse(string gesture, out Key key, out KeyModifiers modifiers)
    {
        key = Key.None;
        modifiers = KeyModifiers.None;
        var parts = gesture.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;

        foreach (var part in parts[..^1])
        {
            var modifier = part.ToLowerInvariant() switch
            {
                "ctrl" or "control" => KeyModifiers.Control,
                "shift" => KeyModifiers.Shift,
                "alt" => KeyModifiers.Alt,
                "meta" or "win" => KeyModifiers.Meta,
                _ => (KeyModifiers?)null
            };
            if (modifier is null) return false;
            modifiers |= modifier.Value;
        }

        var keyName = parts[^1];
        if (!Enum.TryParse(keyName, true, out key) || key == Key.None) return false;
        return true;
    }
}
