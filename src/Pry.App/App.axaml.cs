using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;

namespace Pry.App;

public sealed partial class App : Application
{
    private TrayIcon? _trayIcon;
    private MainWindow? _mainWindow;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _mainWindow = new MainWindow(); desktop.MainWindow = _mainWindow;
            CreateTrayIcon(desktop);
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void CreateTrayIcon(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_mainWindow is null) return;
        var open = new NativeMenuItem("打开聊天窗口"); var model = new NativeMenuItem("查看模型链接信息");
        var topmost = new NativeMenuItem("聊天窗口始终置顶") { ToggleType = NativeMenuItemToggleType.CheckBox };
        var pet = new NativeMenuItem("桌宠模式（等待美术素材）") { IsEnabled = false }; var exit = new NativeMenuItem("退出…");
        open.Click += (_, _) => _mainWindow.ShowFromTray(); model.Click += async (_, _) => await ShowInfoAsync("模型链接信息", _mainWindow.ActiveModelLink);
        topmost.Click += (_, _) => { topmost.IsChecked = !topmost.IsChecked; _mainWindow.Topmost = topmost.IsChecked; }; exit.Click += async (_, _) => await AskExitAsync(desktop);
        var menu = new NativeMenu { Items = { open, model, new NativeMenuItemSeparator(), pet, topmost, new NativeMenuItemSeparator(), exit } };
        var iconBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        _trayIcon = new TrayIcon { ToolTipText = "Pry 本地陪伴助手", Menu = menu, Icon = new WindowIcon(new MemoryStream(iconBytes)), IsVisible = true };
        _trayIcon.Clicked += (_, _) => _mainWindow.ShowFromTray(); TrayIcon.SetIcons(this, new TrayIcons { _trayIcon });
    }

    private async Task AskExitAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_mainWindow is null) return; _mainWindow.ShowFromTray();
        var dialog = new Window { Title = "退出 Pry", Width = 540, Height = 280, Background = Avalonia.Media.Brushes.Transparent, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var stop = new Button { Content = "关闭模型服务并退出", Classes = { "primary" } }; var keep = new Button { Content = "保留模型服务并退出" }; var cancel = new Button { Content = "取消" }; var choice = -1;
        stop.Click += (_, _) => { choice = 1; dialog.Close(); }; keep.Click += (_, _) => { choice = 0; dialog.Close(); }; cancel.Click += (_, _) => dialog.Close();
        dialog.KeyDown += (_, args) =>
        {
            if (args.Key == Key.Escape) { args.Handled = true; dialog.Close(); }
            else if (args.Key == Key.Enter) { args.Handled = true; choice = 1; dialog.Close(); }
        };
        var message = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = "退出 Pry", FontSize = 17, FontWeight = Avalonia.Media.FontWeight.SemiBold },
                new TextBlock { Text = "是否同时关闭后台本地模型服务？", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                new TextBlock { Text = "保留服务会继续占用内存，之后可由其他兼容客户端使用。", TextWrapping = Avalonia.Media.TextWrapping.Wrap }
            }
        };
        var messageCard = _mainWindow.CreateCompactDialogTextCard(message);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Children = { cancel, keep, stop } };
        dialog.Content = _mainWindow.CreateThemedDialogSurface(MainWindow.CreateCompactDialogLayout(messageCard, actions));
        await dialog.ShowDialog(_mainWindow); if (choice < 0) return; await _mainWindow.PrepareForExitAsync(choice == 1); _trayIcon!.IsVisible = false; desktop.Shutdown();
    }

    private async Task ShowInfoAsync(string title, string message)
    {
        if (_mainWindow is null) return; _mainWindow.ShowFromTray(); var dialog = new Window { Title = title, Width = 500, Height = 190, WindowStartupLocation = WindowStartupLocation.CenterOwner }; var close = new Button { Content = "关闭", HorizontalAlignment = HorizontalAlignment.Right }; close.Click += (_, _) => dialog.Close();
        dialog.Content = _mainWindow.CreateThemedDialogSurface(new StackPanel { Margin = new Thickness(24), Spacing = 18, Children = { new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap }, close } }); await dialog.ShowDialog(_mainWindow);
    }
}
