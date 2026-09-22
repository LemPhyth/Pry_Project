using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Pry.Contracts;
using Pry.Core.Models;

namespace Pry.App;

public sealed partial class PryMessengerWindow
{
    private long _backgroundRevision, _messageLoadRevision;
    private IReadOnlyList<ChatMessage> _visibleMessages = [];
    private readonly DispatcherTimer _companionTimer = new() { Interval = TimeSpan.FromSeconds(15) };
    private readonly SemaphoreSlim _activityGate = new(1, 1);
    private readonly string _activityClientId = Guid.NewGuid().ToString("N");
    private long _activitySequence;
    private DateTimeOffset _lastActivity = DateTimeOffset.UtcNow;
    private CancellationTokenSource? _gesture;
    private Flyout? _interactionFlyout;
    private bool _interactionBusy, _companionPolling, _markingRead, _updatingTimeline;
    private string? _retryInteractionKey, _retryInteractionAction, _retryInteractionRoom;
    private readonly Dictionary<string, long> _acknowledgedRead = new();

    private void InitializeCompanionExperience()
    {
        AddHandler(PointerPressedEvent, (_, _) => RecordActivity(), RoutingStrategies.Tunnel, true);
        AddHandler(KeyDownEvent, (_, _) => RecordActivity(), RoutingStrategies.Tunnel, true);
        ComposerTextBox.TextChanged += (_, _) => RecordActivity();
        MessageScroll.ScrollChanged += (_, _) => { _ = MarkVisibleReadAsync(); };
        Activated += (_, _) => { _ = MarkVisibleReadAsync(); };
        SizeChanged += (_, _) => UpdateHistoryLayout();
        _companionTimer.Tick += async (_, _) =>
        {
            if (_companionPolling) return;
            _companionPolling = true;
            try { await ReportActivityAsync(); await RefreshRoomsWithoutSwitchAsync(); }
            catch { HeaderConnectionText.Text = "状态同步暂不可用，稍后自动重试"; }
            finally { _companionPolling = false; }
        };
        InteractionButton.AddHandler(KeyDownEvent, async (_, e) =>
        {
            if (e.Key is not (Key.Enter or Key.Space)) return;
            e.Handled = true; CancelInteractionGesture(); await OpenInteractionAsync();
        }, RoutingStrategies.Tunnel);
    }

    private void StartCompanionExperience() { _companionTimer.Start(); _ = ReportActivityAsync(); }
    private async Task StopCompanionExperienceAsync()
    {
        _companionTimer.Stop(); CancelInteractionGesture();
        try { await RemoveActivityAsync().WaitAsync(TimeSpan.FromSeconds(3)); } catch (TimeoutException) { }
    }
    private async Task RemoveActivityAsync()
    {
        await _activityGate.WaitAsync();
        try { await _api.RemoveCompanionActivityAsync(_activityClientId); } catch { }
        finally { _activityGate.Release(); }
    }
    private void RecordActivity() { _lastActivity = DateTimeOffset.UtcNow; _ = ReportActivityAsync(); }
    private async Task ReportActivityAsync()
    {
        if (_preferences is null || _allowClose) return;
        if (!await _activityGate.WaitAsync(0)) return;
        try
        {
            if (_allowClose) return;
            await _api.ReportCompanionActivityAsync(_activityClientId, new ReportCompanionActivityRequest(
                ++_activitySequence, _conversationId,
                (int)Math.Clamp((DateTimeOffset.UtcNow - _lastActivity).TotalSeconds, 0, 86400),
                IsTyping: (DateTimeOffset.UtcNow - _lastActivity).TotalSeconds < 3 || _sending,
                IsRecording: _waveInput is not null, IsTranscribing: _speechBusy,
                IsInteractionOpen: _interactionFlyout?.IsOpen == true || WorkspacePage.IsVisible));
        }
        catch { }
        finally { _activityGate.Release(); }
    }

    private bool IsAtMessageBottom() => MessageScroll.Extent.Height - MessageScroll.Viewport.Height - MessageScroll.Offset.Y < 24;
    private async Task MarkVisibleReadAsync()
    {
        if (_updatingTimeline || _markingRead || !IsActive || !IsVisible || WorkspacePage.IsVisible ||
            (Bounds.Width < 1100 && !_sidebarCollapsed) || !IsAtMessageBottom() || _conversationId is not { } room) return;
        var through = _visibleMessages.LastOrDefault()?.Id ?? 0;
        if (through <= _acknowledgedRead.GetValueOrDefault(room)) return;
        _markingRead = true;
        try
        {
            var result = await _api.MarkConversationReadAsync(room, through);
            _acknowledgedRead[room] = Math.Max(_acknowledgedRead.GetValueOrDefault(room), result.LastReadMessageId);
            await RefreshRoomsWithoutSwitchAsync();
        }
        catch { }
        finally { _markingRead = false; }
    }
    private void NewMessages_Click(object? sender, RoutedEventArgs e)
    { MessageScroll.ScrollToEnd(); NewMessagesButton.IsVisible = false; _ = MarkVisibleReadAsync(); }

    private void UpdateHistoryLayout()
    {
        var narrow = Bounds.Width < 1100;
        MainLayout.ColumnDefinitions[0].Width = new GridLength(!_sidebarCollapsed && !narrow ? _sidebarWidth : 0);
        Grid.SetColumnSpan(ConversationSurface, narrow ? 2 : 1);
        ConversationSurface.Width = narrow ? _sidebarWidth : double.NaN;
        ConversationSurface.HorizontalAlignment = narrow ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;
        ConversationSurface.ZIndex = narrow ? 10 : 1;
        KeyboardNavigation.SetTabNavigation(ConversationSurface, narrow ? KeyboardNavigationMode.Cycle : KeyboardNavigationMode.Continue);
    }
    private void CancelInteractionGesture()
    { _gesture?.Cancel(); _gesture?.Dispose(); _gesture = null; _interactionFlyout?.Hide(); }

    private async void Interaction_Click(object? sender, RoutedEventArgs e)
    {
        if (_interactionBusy) return;
        if (_gesture is not null)
        {
            CancelInteractionGesture(); await ExecuteInteractionAsync("poke"); return;
        }
        var gesture = _gesture = new CancellationTokenSource();
        try
        {
            await Task.Delay(PlatformSettings?.GetDoubleTapTime(PointerType.Mouse) ?? TimeSpan.FromMilliseconds(500), gesture.Token);
            if (_gesture != gesture) return;
            _gesture = null; await OpenInteractionAsync();
        }
        catch (OperationCanceledException) { }
        finally { gesture.Dispose(); }
    }
    private async Task OpenInteractionAsync()
    {
        if (_character is null || _conversationId is null) return;
        var room = _conversationId; var character = _character.Id;
        var panel = new StackPanel { Spacing = 8, Width = 280 };
        var flyout = _interactionFlyout = new Flyout { Content = panel, Placement = PlacementMode.Bottom };
        panel.AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key != Key.Escape) return;
            e.Handled = true; flyout.Hide();
        }, RoutingStrategies.Tunnel);
        panel.Children.Add(new TextBlock { Text = "正在读取角色动作…" }); flyout.ShowAt(InteractionButton);
        flyout.Closed += (_, _) => { InteractionButton.Focus(); _ = ReportActivityAsync(); };
        await ReportActivityAsync();
        try
        {
            var profile = await _api.GetInteractionProfileAsync(character);
            if (room != _conversationId || character != _character?.Id || !flyout.IsOpen) return;
            panel.Children.Clear();
            panel.Children.Add(Services.PryUi.Heading(_character?.Name ?? "角色互动"));
            panel.Children.Add(Services.PryUi.Hint("选一个动作，回应此刻的陪伴。"));
            void Action(string id, string label)
            {
                var button = new Button { Content = label, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left };
                button.Click += async (_, _) => { flyout.Hide(); await ExecuteInteractionAsync(id); };
                panel.Children.Add(button);
            }
            Action("poke", "戳一戳");
            foreach (var action in profile.Actions.Where(x => x.Enabled)) Action(action.Id, action.DisplayName);
            var settings = new Button { Content = "主动话题设置" };
            panel.Children.Add(new Border { Height = 1, Background = Services.PryVisualTheme.Get("Border"), Margin = new Thickness(0, 8) });
            settings.Click += (_, _) => { flyout.Hide(); ShowWorkspacePage("主动话题", new ProactiveSettingsPanel(_api, character, room)); };
            panel.Children.Add(settings);
            panel.Children.OfType<Button>().FirstOrDefault()?.Focus();
        }
        catch (Exception ex) { panel.Children.Clear(); panel.Children.Add(new TextBlock { Text = $"动作读取失败：{ex.Message}", TextWrapping = Avalonia.Media.TextWrapping.Wrap }); }
    }
    private async Task ExecuteInteractionAsync(string action)
    {
        if (_interactionBusy || _conversationId is not { } room) return;
        var revision = _roomRevision;
        var key = _retryInteractionRoom == room && _retryInteractionAction == action && _retryInteractionKey is { } retry ? retry : Guid.NewGuid().ToString("N");
        _interactionBusy = true; InteractionButton.IsEnabled = false;
        ConversationStatusText.Text = "正在与角色互动…";
        try
        {
            var response = await _api.ExecuteInteractionAsync(room, new ExecuteInteractionRequest(action, key));
            _retryInteractionKey = null;
            if (revision != _roomRevision) return;
            ConversationStatusText.Text = response.Replayed ? "互动结果已恢复" : "角色已收到你的互动";
            await ReloadMessagesAsync(room, revision, true); await RefreshRoomsWithoutSwitchAsync();
        }
        catch (Exception ex)
        {
            _retryInteractionRoom = room; _retryInteractionAction = action; _retryInteractionKey = key;
            if (revision == _roomRevision) ConversationStatusText.Text = $"互动未完成：{ex.Message}。再次点击可重试。";
        }
        finally { _interactionBusy = false; InteractionButton.IsEnabled = true; }
    }
}
