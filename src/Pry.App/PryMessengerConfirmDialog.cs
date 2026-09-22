using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Pry.App.Services;

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
        Height = 300;
        CanResize = false;
        SystemDecorations = SystemDecorations.None;
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = PryVisualTheme.Get("Canvas");
        Icon = CreateApplicationIcon();

        var close = new Button { Content = "×", Background = Brushes.Transparent };
        close.Width = 40; close.Height = 40; close.Padding = new Thickness(0); close.FontSize = 17;
        close.Click += (_, _) => Close();
        var titleBar = new Border
        {
            Height = 48, Background = PryVisualTheme.Get("Panel"), BorderBrush = PryVisualTheme.Get("Border"), BorderThickness = new Thickness(0, 0, 0, 1),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(24, 0, 8, 0),
                Children =
                {
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center,
                        Children =
                        {
                            CreateLogoImage(22),
                            new TextBlock { Text = title, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center }
                        }
                    },
                    close
                }
            }
        };
        Grid.SetColumn(close, 1);
        titleBar.PointerPressed += (_, e) => { if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e); };

        var actionBar = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        foreach (var action in actions)
        {
            var button = new Button { Content = action.Label, Margin = new Thickness(8, 8, 0, 0) };
            if (action.Destructive) button.Classes.Add("danger");
            else if (action.Primary) button.Classes.Add("primary");
            button.Click += (_, _) => { _result = action.Id; Close(); };
            actionBar.Children.Add(button);
        }
        var body = new Grid
        {
            RowDefinitions = new RowDefinitions("*,Auto"), Margin = new Thickness(24), RowSpacing = 16,
            Children =
            {
                new ScrollViewer { Content = new StackPanel { Spacing = 12, Children = { PryUi.Heading(heading), PryUi.Hint(message) } } },
                actionBar
            }
        };
        Grid.SetRow(actionBar, 1);
        Content = new Grid { RowDefinitions = new RowDefinitions("48,*"), Children = { titleBar, body } };
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

    private static Image CreateLogoImage(double size)
    {
        using var stream = AssetLoader.Open(new Uri("avares://Pry.App/Assets/P_logo_2048.png"));
        return new Image { Width = size, Height = size, Stretch = Stretch.Uniform, Source = new Bitmap(stream) };
    }

    private static WindowIcon CreateApplicationIcon()
    {
        using var stream = AssetLoader.Open(new Uri("avares://Pry.App/Assets/Pry.ico"));
        return new WindowIcon(stream);
    }
}
