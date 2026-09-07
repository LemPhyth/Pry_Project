using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Pry.App;

internal sealed record MessengerDialogAction(string Id, string Label, bool Primary = false, bool Destructive = false);

internal sealed class PryMessengerConfirmDialog : Window
{
    private string? _result;

    private PryMessengerConfirmDialog(string title, string heading, string message,
        IReadOnlyList<MessengerDialogAction> actions, string accentColor)
    {
        Title = title;
        Width = 540;
        Height = 260;
        CanResize = false;
        SystemDecorations = SystemDecorations.None;
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse("#0E1623");

        var close = Button("×", Brushes.Transparent, Brush.Parse("#D7DFEC"));
        close.Width = 40; close.Height = 40; close.Padding = new Thickness(0); close.FontSize = 17;
        close.Click += (_, _) => Close();
        var titleBar = new Border
        {
            Height = 42, Background = Brush.Parse("#0F1725"), BorderBrush = Brush.Parse("#253149"), BorderThickness = new Thickness(0, 0, 0, 1),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(18, 0, 5, 0),
                Children = { new TextBlock { Text = title, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center }, close }
            }
        };
        Grid.SetColumn(close, 1);
        titleBar.PointerPressed += (_, e) => { if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e); };

        var actionBar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
        foreach (var action in actions)
        {
            var background = action.Primary ? Brush.Parse(accentColor) : Brush.Parse("#243247");
            if (action.Destructive) background = Brush.Parse("#C84F5A");
            var button = Button(action.Label, background, Brushes.White);
            button.Click += (_, _) => { _result = action.Id; Close(); };
            actionBar.Children.Add(button);
        }
        var body = new Grid
        {
            RowDefinitions = new RowDefinitions("*,Auto"), Margin = new Thickness(22), RowSpacing = 20,
            Children =
            {
                new Border
                {
                    Background = Brush.Parse("#182235"), BorderBrush = Brush.Parse("#2C3B54"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(16), Padding = new Thickness(20),
                    Child = new StackPanel { Spacing = 11, Children = { new TextBlock { Text = heading, FontSize = 20, FontWeight = FontWeight.SemiBold }, new TextBlock { Text = message, Foreground = Brush.Parse("#AAB6C8"), TextWrapping = TextWrapping.Wrap } } }
                },
                actionBar
            }
        };
        Grid.SetRow(actionBar, 1);
        Content = new Grid { RowDefinitions = new RowDefinitions("42,*"), Children = { titleBar, body } };
        Grid.SetRow(body, 1);
        KeyDown += (_, e) => { if (e.Key == Key.Escape) { e.Handled = true; Close(); } };
    }

    public static async Task<string?> ShowAsync(Window owner, string title, string heading, string message,
        IReadOnlyList<MessengerDialogAction> actions, string accentColor)
    {
        var dialog = new PryMessengerConfirmDialog(title, heading, message, actions, accentColor);
        await dialog.ShowDialog(owner);
        return dialog._result;
    }

    private static Button Button(string text, IBrush background, IBrush foreground) => new()
    {
        Content = text, Background = background, Foreground = foreground, BorderThickness = new Thickness(0),
        CornerRadius = new CornerRadius(11), Padding = new Thickness(16, 9), MinHeight = 38
    };
}
