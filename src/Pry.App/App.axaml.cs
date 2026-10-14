using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
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

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Pry.App.Services.PryVisualTheme.Apply(new Pry.Core.Models.ThemePreferences());
    }

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
            // 配置根只建一次：发现用的数据目录与内嵌后端写的必须出自同一个 IConfiguration（F-1）。
            // 独立进程模式优先：本机已有别的 Pry 实例在跑后端时接上它，而不是再起一个宿主争同一份数据目录。
            // 发现只认那份目录里 runtime.json 的端口与令牌，界面层没有备用端口；文件缺席、进程已退出或连不上
            // 都回落到内嵌，那条路径由 Kestrel 自己选端口（:0）再读回来，用的还是这同一个 builder。
            var hostBuilder = BackendApplication.CreateHostBuilder();
            var discovered = await Pry.App.Services.BackendLocator.TryConnectAsync(hostBuilder);
            _backendHttpClient = discovered ?? await StartEmbeddedBackendAsync(hostBuilder);
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

    private async Task<HttpClient> StartEmbeddedBackendAsync(WebApplicationBuilder hostBuilder)
    {
        // 端口交给 Kestrel 自己选（:0）再读回来。先占一个空闲端口再让后端去绑会留一个窗口：
        // 别的进程可以在那个窗口里抢走同一个端口，于是发现文件写的是一个连不上的入口。
        hostBuilder.Configuration["Pry:Url"] = "http://127.0.0.1:0";
        _backend = await BackendApplication.BuildAsync(hostBuilder);
        await _backend.StartAsync();
        // 同进程内嵌时读同一个服务对象，不读 runtime.json：那份文件是给独立进程用的发现入口。
        return _backend.CreateBackendHttpClient();
    }

    private async Task CreateMainWindowAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var api = new PryBackendClient(_backendHttpClient ?? throw new InvalidOperationException("本地 API 客户端尚未建立。"));
        var preferences = await api.GetPreferencesAsync();
        // Both historical layout preferences open the new unified companion workspace.
        _mainWindow = new PryMessengerWindow(api);
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
            Icon = CreateApplicationIcon(),
            Width = 560,
            Height = 300,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = new Border
            {
                Padding = new Thickness(24),
                Child = new StackPanel
                {
                    Spacing = 16,
                    Children =
                    {
                        new TextBlock { Text = Services.NoticeContentFactory.StartupHeadline(PryErrorPresenter.Describe(exception)), FontSize = 18, FontWeight = Avalonia.Media.FontWeight.SemiBold },
                        Services.NoticeContentFactory.Create(exception).Content,
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
        _trayIcon = new TrayIcon { ToolTipText = "Pry 本地陪伴助手", Menu = menu, Icon = CreateApplicationIcon(), IsVisible = true };
        if (_mainWindow is PryMessengerWindow messenger)
            messenger.UnreadChanged += count =>
            {
                if (_trayIcon is not null) _trayIcon.ToolTipText = count > 0 ? $"Pry · {count} 条未读消息" : "Pry 本地陪伴助手";
                open.Header = count > 0 ? $"打开聊天窗口（{count} 条未读）" : "打开聊天窗口";
            };
        _trayIcon.Clicked += (_, _) => _mainWindow.ShowFromTray(); TrayIcon.SetIcons(this, new TrayIcons { _trayIcon });
    }

    private static WindowIcon CreateApplicationIcon()
    {
        using var stream = AssetLoader.Open(new Uri("avares://Pry.App/Assets/Pry.ico"));
        return new WindowIcon(stream);
    }

    /// <summary>接上别人的后端时，退出这一扇窗口不会关掉那个进程——提示必须说清事实，否则用户会以为模型服务已经停了。</summary>
    private string ExitNotice => _backend is not null
        ? "退出会同时关闭当前内嵌后端和本地模型服务。"
        : "本机另一个 Pry 实例正在运行后端；退出只会断开本窗口的连接，那个后端与它的模型服务会继续运行。";

    private async Task AskExitAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_mainWindow is null) return; _mainWindow.ShowFromTray();
        if (_mainWindow is PryMessengerWindow messenger)
        {
            var result = await PryMessengerConfirmDialog.ShowAsync(messenger, "退出 Pry", "退出 Pry",
                ExitNotice,
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
                new TextBlock { Text = ExitNotice, FontWeight = Avalonia.Media.FontWeight.SemiBold }
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
}
