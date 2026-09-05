using Avalonia.Controls;
using Avalonia.Media;

namespace Pry.App.Services;

public sealed class SettingsSurfaceTheme(bool light, bool hasBackground)
{
    public IBrush Canvas { get; } = Brush.Parse(light ? "#EEF3F7" : "#0E1621");
    public IBrush Panel { get; } = Brush.Parse(hasBackground ? (light ? "#B8E3EBF1" : "#A817212B") : (light ? "#E3EBF1" : "#17212B"));
    public IBrush Card { get; } = Brush.Parse(hasBackground ? (light ? "#B8F8FAFC" : "#B0182533") : (light ? "#F8FAFC" : "#182533"));
    public IBrush Border { get; } = Brush.Parse(light ? "#BDCBD6" : "#263746");
    public IBrush Text { get; } = Brush.Parse(light ? "#17212B" : "#F2F6FA");
    public IBrush MutedText { get; } = Brush.Parse(light ? "#526678" : "#8EA2B5");

    public void Apply(IEnumerable<Border> cards, IEnumerable<Control> roots)
    {
        foreach (var card in cards)
        {
            card.Background = Card;
            card.BorderBrush = Border;
        }

        // Include detached settings pages; navigating must not expose stale colors.
        var pending = new Stack<Control>(roots);
        var visited = new HashSet<Control>();
        while (pending.TryPop(out var control))
        {
            if (!visited.Add(control)) continue;
            if (control is TextBlock text)
                text.Foreground = text.FontSize >= 15 || text.FontWeight == FontWeight.SemiBold ? Text : MutedText;
            if (control is Panel panel)
                foreach (var child in panel.Children) pending.Push(child);
            else if (control is Decorator { Child: Control decorated }) pending.Push(decorated);
            else if (control is ContentControl { Content: Control content }) pending.Push(content);
        }
    }
}
