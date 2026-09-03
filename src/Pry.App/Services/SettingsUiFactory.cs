using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Pry.App.Services;

public sealed class SettingsUiFactory
{
    private readonly List<Border> _cards = [];

    public IReadOnlyList<Border> Cards => _cards;

    public TextBox CreateTextBox(string value, int height = 34)
    {
        var box = new TextBox
        {
            Text = value,
            MinHeight = height,
            AcceptsReturn = height > 60,
            TextWrapping = TextWrapping.Wrap
        };
        return box;
    }

    public NumericUpDown CreateNumber(decimal value, decimal min, decimal max, decimal step = 1) => new()
    {
        Value = value,
        Minimum = min,
        Maximum = max,
        Increment = step
    };

    public void AddHeader(Panel target, string value)
    {
        var header = new TextBlock
        {
            Text = value,
            FontSize = 21,
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(0, 4, 0, 8)
        };
        AutomationProperties.SetName(header, value);
        target.Children.Add(header);
    }

    public void AddField(Panel target, string label, Control control)
    {
        target.Children.Add(new TextBlock { Text = label, Foreground = Brush.Parse("#B8CCE0") });
        AutomationProperties.SetName(control, label);
        target.Children.Add(control);
    }

    public Border CreateCard(string title, params Control[] controls)
    {
        var panel = new StackPanel { Spacing = 9 };
        panel.Children.Add(new TextBlock { Text = title, FontSize = 15, FontWeight = FontWeight.SemiBold });
        foreach (var control in controls) panel.Children.Add(control);
        var card = new Border
        {
            Background = Brush.Parse("#B017212B"),
            BorderBrush = Brush.Parse("#263746"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(18),
            Child = panel
        };
        AutomationProperties.SetName(card, $"{title}设置");
        _cards.Add(card);
        return card;
    }

    public StackPanel CreateManagerPanel(string title, string description, Button action)
    {
        var panel = new StackPanel { Margin = new Thickness(28), Spacing = 14 };
        AddHeader(panel, title);
        panel.Children.Add(CreateCard(title,
            new TextBlock
            {
                Text = description,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brush.Parse("#B8CCE0")
            }, action));
        return panel;
    }
}
