using System.Collections.ObjectModel;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class ThemeImageDraft
{
    private readonly List<string> _history;
    private readonly Dictionary<string, ImageDisplayPreferences> _displays;

    public ThemeImageDraft(string? selectedPath, IEnumerable<string> history,
        IReadOnlyDictionary<string, ImageDisplayPreferences> displays, Func<string, bool>? exists = null)
    {
        exists ??= File.Exists;
        SelectedPath = selectedPath;
        _history = history.Append(selectedPath).Where(path => !string.IsNullOrWhiteSpace(path) && exists(path))
            .Select(path => path!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        _displays = new Dictionary<string, ImageDisplayPreferences>(displays, StringComparer.OrdinalIgnoreCase);
        History = _history.AsReadOnly();
        Displays = new ReadOnlyDictionary<string, ImageDisplayPreferences>(_displays);
    }

    public string? SelectedPath { get; private set; }
    public IReadOnlyList<string> History { get; }
    public IReadOnlyDictionary<string, ImageDisplayPreferences> Displays { get; }

    public void StoreDisplay(ImageDisplayPreferences display)
    {
        if (SelectedPath is not null) _displays[SelectedPath] = display;
    }

    public void Select(string path, ImageDisplayPreferences previousDisplay)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        StoreDisplay(previousDisplay);
        if (!_history.Contains(path, StringComparer.OrdinalIgnoreCase)) _history.Add(path);
        SelectedPath = path;
    }

    public void ClearSelection(ImageDisplayPreferences previousDisplay)
    {
        StoreDisplay(previousDisplay);
        SelectedPath = null;
    }

    public void RemoveSelected()
    {
        if (SelectedPath is not { } path) return;
        _history.RemoveAll(item => string.Equals(item, path, StringComparison.OrdinalIgnoreCase));
        _displays.Remove(path);
        SelectedPath = null;
    }
}
