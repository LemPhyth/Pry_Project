using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.AspNetCore.Builder;
using Pry.Api;
using Pry.Client;

namespace Pry.App;

public sealed partial class App : Application
{
    private TrayIcon? _trayIcon;
    private IPryMainWindow? _mainWindow;
    private WebApplication? _backend;
    private HttpClient? _backendHttpClient;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            base.OnFrameworkInitializationCompleted();
            Dispatcher.UIThread.Post(() => _ = InitializeDesktopAsync(desktop));
            return;
        }
        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitializeDesktopAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            var backendUrl = $"http://127.0.0.1:{ReserveLoopbackPort()}";
            _backend = await BackendApplication.BuildAsync(
                configure: builder => builder.Configuration["Pry:Url"] = backendUrl);
            await _backend.StartAsync();
            _backendHttpClient = new HttpClient
            {
                BaseAddress = new Uri(backendUrl + "/"),
                Timeout = Timeout.InfiniteTimeSpan
            };
            await CreateMainWindowAsync(desktop);
            CreateTrayIcon(desktop);
            _mainWindow!.HostWindow.Show();
        }
        catch (Exception ex)
        {
            await DisposeBackendAsync();
            ShowStartupFailure(desktop, ex);
        }
    }

    private async Task CreateMainWindowAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var api = new PryBackendClient(_backendHttpClient ?? throw new InvalidOperationException("本地 API 客户端尚未建立。"));
        var preferences = await api.GetPreferencesAsync();
        _mainWindow = preferences.Theme.MainWindowLayoutMode == Pry.Core.Models.MainWindowLayoutModes.Card
            ? new MainWindow(api)
            : new PryMessengerWindow(api);
        switch (_mainWindow)
        {
            case PryMessengerWindow messenger: messenger.RestartRequested += () => RestartFrontendAsync(desktop); break;
            case MainWindow classic: classic.RestartRequested += () => RestartFrontendAsync(desktop); break;
        }
        desktop.MainWindow = _mainWindow.HostWindow;
    }

    private async Task RestartFrontendAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_mainWindow is null) return;
        var previous = _mainWindow;
        await previous.PrepareForExitAsync(false);
        previous.HostWindow.Close();
        await CreateMainWindowAsync(desktop);
        _mainWindow!.HostWindow.Show();
        _mainWindow.HostWindow.Activate();
    }

    private void ShowStartupFailure(IClassicDesktopStyleApplicationLifetime desktop, Exception exception)
    {
        var exit = new Button { Content = "退出", HorizontalAlignment = HorizontalAlignment.Right };
        var dialog = new Window
        {
            Title = "Pry 启动失败",
            Width = 560,
            Height = 260,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = new Border
            {
                Padding = new Thickness(24),
                Child = new StackPanel
                {
                    Spacing = 16,
                    Children =
                    {
                        new TextBlock { Text = "Pry 无法启动本地后端", FontSize = 18, FontWeight = Avalonia.Media.FontWeight.SemiBold },
                        new TextBlock { Text = exception.Message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                        new TextBlock { Text = "请检查本地数据目录和程序资源是否完整。", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                        exit
                    }
                }
            }
        };
        exit.Click += (_, _) => dialog.Close();
        dialog.Closed += (_, _) => desktop.Shutdown(1);
        desktop.MainWindow = dialog;
        dialog.Show();
    }

    private async Task DisposeBackendAsync()
    {
        if (_backend is not null)
        {
            try { await _backend.StopAsync(); }
            catch { }
            try { await _backend.DisposeAsync(); }
            catch { }
            _backend = null;
        }
        _backendHttpClient?.Dispose();
        _backendHttpClient = null;
    }

    private void CreateTrayIcon(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_mainWindow is null) return;
        var open = new NativeMenuItem("打开聊天窗口"); var model = new NativeMenuItem("查看模型链接信息");
        var topmost = new NativeMenuItem("聊天窗口始终置顶") { ToggleType = NativeMenuItemToggleType.CheckBox };
        var pet = new NativeMenuItem("桌宠模式（等待美术素材）") { IsEnabled = false }; var exit = new NativeMenuItem("退出…");
        open.Click += (_, _) => _mainWindow.ShowFromTray(); model.Click += async (_, _) => await ShowInfoAsync("模型链接信息", _mainWindow.ActiveModelLink);
        topmost.Click += (_, _) => { topmost.IsChecked = !topmost.IsChecked; _mainWindow.HostWindow.Topmost = topmost.IsChecked; }; exit.Click += async (_, _) => await AskExitAsync(desktop);
        var menu = new NativeMenu { Items = { open, model, new NativeMenuItemSeparator(), pet, topmost, new NativeMenuItemSeparator(), exit } };
        var iconBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        _trayIcon = new TrayIcon { ToolTipText = "Pry 本地陪伴助手", Menu = menu, Icon = new WindowIcon(new MemoryStream(iconBytes)), IsVisible = true };
        _trayIcon.Clicked += (_, _) => _mainWindow.ShowFromTray(); TrayIcon.SetIcons(this, new TrayIcons { _trayIcon });
    }

    private async Task AskExitAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_mainWindow is null) return; _mainWindow.ShowFromTray();
        if (_mainWindow is PryMessengerWindow messenger)
        {
            var result = await PryMessengerConfirmDialog.ShowAsync(messenger, "退出 Pry", "退出 Pry",
                "退出会同时关闭当前内嵌后端和本地模型服务。",
                [
                    new("cancel", "取消"),
                    new("exit", "退出", Destructive: true)
                ], messenger.AccentColor);
            if (result is null or "cancel") return;
            await _mainWindow.PrepareForExitAsync(true);
            await DisposeBackendAsync();
            _trayIcon!.IsVisible = false; desktop.Shutdown();
            return;
        }
        var dialog = new Window { Title = "退出 Pry", Width = 540, Height = 280, Background = Avalonia.Media.Brushes.Transparent, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var exit = new Button { Content = "退出", Classes = { "primary" } }; var cancel = new Button { Content = "取消" }; var confirmed = false;
        exit.Click += (_, _) => { confirmed = true; dialog.Close(); }; cancel.Click += (_, _) => dialog.Close();
        dialog.KeyDown += (_, args) =>
        {
            if (args.Key == Key.Escape) { args.Handled = true; dialog.Close(); }
            else if (args.Key == Key.Enter) { args.Handled = true; confirmed = true; dialog.Close(); }
        };
        var message = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = "退出 Pry", FontSize = 17, FontWeight = Avalonia.Media.FontWeight.SemiBold },
                new TextBlock { Text = "退出会同时关闭当前内嵌后端和本地模型服务。", FontWeight = Avalonia.Media.FontWeight.SemiBold }
            }
        };
        var messageCard = CreateDialogCard(message);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Children = { cancel, exit } };
        dialog.Content = CreateDialogSurface(PryMessengerWindow.CreateCompactDialogLayout(messageCard, actions));
        await dialog.ShowDialog(_mainWindow.HostWindow); if (!confirmed) return; await _mainWindow.PrepareForExitAsync(true);
        await DisposeBackendAsync();
        _trayIcon!.IsVisible = false; desktop.Shutdown();
    }

    private async Task ShowInfoAsync(string title, string message)
    {
        if (_mainWindow is null) return; _mainWindow.ShowFromTray(); var dialog = new Window { Title = title, Width = 500, Height = 190, WindowStartupLocation = WindowStartupLocation.CenterOwner }; var close = new Button { Content = "关闭", HorizontalAlignment = HorizontalAlignment.Right }; close.Click += (_, _) => dialog.Close();
        dialog.Content = CreateDialogSurface(new StackPanel { Margin = new Thickness(24), Spacing = 18, Children = { new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap }, close } }); await dialog.ShowDialog(_mainWindow.HostWindow);
    }

    private static Border CreateDialogCard(Control content) => new() { Background = Avalonia.Media.Brush.Parse("#182235"), BorderBrush = Avalonia.Media.Brush.Parse("#34495E"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(13), Padding = new Thickness(18), Child = content };
    private static Control CreateDialogSurface(Control content) => new Grid { Background = Avalonia.Media.Brush.Parse("#101827"), Children = { content } };

    private static int ReserveLoopbackPort()
    {
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start(); return ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
    }
}
