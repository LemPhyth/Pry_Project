using Avalonia.Input;

namespace Pry.App.Services;

public static class ShortcutGestureMatcher
{
    private static readonly HashSet<string> KeyNames = new(Enum.GetNames<Key>(), StringComparer.OrdinalIgnoreCase);

    public static bool Matches(Key key, KeyModifiers actualModifiers, string gesture)
    {
        if (!TryParse(gesture, out var expectedKey, out var expectedModifiers)) return false;
        var keyMatches = expectedKey is Key.Enter or Key.Return
            ? key is Key.Enter or Key.Return
            : key == expectedKey;
        return keyMatches && actualModifiers == expectedModifiers;
    }

    public static bool IsValid(string gesture) => TryParse(gesture, out _, out _);

    public static string Normalize(string gesture)
    {
        if (!TryParse(gesture, out var key, out var modifiers))
            throw new ArgumentException("无法识别的快捷键。", nameof(gesture));
        var parts = new List<string>();
        if (modifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(KeyModifiers.Meta)) parts.Add("Win");
        parts.Add(key is Key.Enter or Key.Return ? "Enter" : key.ToString());
        return string.Join('+', parts);
    }

    private static bool TryParse(string gesture, out Key key, out KeyModifiers modifiers)
    {
        key = Key.None;
        modifiers = KeyModifiers.None;
        if (string.IsNullOrWhiteSpace(gesture)) return false;
        var parts = gesture.Split('+', StringSplitOptions.TrimEntries);
        if (parts.Any(string.IsNullOrWhiteSpace)) return false;

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
            if (modifier is null || (modifiers & modifier.Value) != 0) return false;
            modifiers |= modifier.Value;
        }

        var keyName = parts[^1];
        if (!KeyNames.Contains(keyName)
            || !Enum.TryParse(keyName, true, out key) || key == Key.None) return false;
        return true;
    }
}
