using Avalonia.Input;

namespace Pry.App.Services;

public static class ShortcutGestureMatcher
{
    public static bool Matches(Key key, KeyModifiers actualModifiers, string gesture)
    {
        var parts = gesture.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;

        var expectedModifiers = KeyModifiers.None;
        foreach (var part in parts[..^1])
        {
            expectedModifiers |= part.ToLowerInvariant() switch
            {
                "ctrl" or "control" => KeyModifiers.Control,
                "shift" => KeyModifiers.Shift,
                "alt" => KeyModifiers.Alt,
                "meta" or "win" => KeyModifiers.Meta,
                _ => KeyModifiers.None
            };
        }

        var keyName = parts[^1];
        var keyMatches = keyName.Equals("Enter", StringComparison.OrdinalIgnoreCase)
            ? key is Key.Enter or Key.Return
            : Enum.TryParse<Key>(keyName, true, out var expectedKey) && key == expectedKey;
        return keyMatches && actualModifiers == expectedModifiers;
    }
}
