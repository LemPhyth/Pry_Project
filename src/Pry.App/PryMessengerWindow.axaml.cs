using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Pry.Client;
using Pry.Contracts;
using Pry.Core.Models;

namespace Pry.App;

public sealed partial class PryMessengerWindow : Window
{
    private readonly PryBackendClient _api;
    private readonly List<ConversationRoom> _rooms = [];
    private readonly List<CharacterSummaryResponse> _characters = [];
    private readonly List<MediaAssetResponse> _attachments = [];
    private ClientPreferencesResponse? _preferences;
    private CharacterSummaryResponse? _character;
    private string? _conversationId;
    private CancellationTokenSource? _eventCancellation;
    private bool _changingRoom;
    private bool _sending;
    private bool _allowClose;
    private long _roomRevision;

    public PryMessengerWindow() : this(new PryBackendClient(new HttpClient
    {
        BaseAddress = new Uri("http://127.0.0.1:5078/"), Timeout = Timeout.InfiniteTimeSpan
    })) { }

    public PryMessengerWindow(PryBackendClient api)
    {
        _api = api;
        InitializeComponent();
        Opened += async (_, _) => await InitializeAsync();
        Closing += (_, args) =>
        {
            if (_allowClose) return;
            args.Cancel = true;
            Hide();
        };
    }

    public string ActiveModelLink => _preferences?.ActiveModelId is { Length: > 0 } id
        ? $"当前文字模型：{id}"
        : "模型尚未配置";

    public void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    public Task PrepareForExitAsync(bool stopModelService)
    {
        _allowClose = true;
        _eventCancellation?.Cancel();
        return Task.CompletedTask;
    }

    private async Task InitializeAsync()
    {
        try
        {
            LoadingText.Text = "正在读取本地资料…";
            if (!await _api.IsHealthyAsync()) throw new InvalidOperationException("本地服务尚未就绪。");
            _preferences = await _api.GetPreferencesAsync();
            _characters.AddRange(await _api.GetCharactersAsync());
            _character = FindCharacter(_preferences.SelectedCharacterId) ?? _characters.FirstOrDefault();
            await ApplyCharacterAsync();
            await RefreshRoomsAsync(_preferences.ActiveConversationId);
            if (_conversationId is null) await CreateConversationAsync();
            var runtime = await _api.GetRuntimeAsync();
            HeaderConnectionText.Text = runtime.State == "ready" ? "本地服务已连接" : $"本地服务 · {runtime.State}";
            LoadingOverlay.IsVisible = false;
        }
        catch (Exception ex)
        {
            LoadingOverlay.IsVisible = false;
            HeaderConnectionText.Text = "连接失败";
            ConversationStatusText.Text = ex.Message;
            ShowEmptyError("Pry 暂时无法打开对话", ex.Message);
        }
    }

    private CharacterSummaryResponse? FindCharacter(string? id) =>
        _characters.FirstOrDefault(item => item.Id == id);

    private async Task ApplyCharacterAsync()
    {
        CharacterNameText.Text = _character?.Name ?? "Pry";
        CharacterAvatarFallback.Text = (_character?.Name ?? "P").FirstOrDefault().ToString();
        CharacterAvatarImage.Source = null;
        CharacterAvatarImage.IsVisible = false;
        CharacterAvatarFallback.IsVisible = true;
        if (_character?.AvatarUrl is not { Length: > 0 } avatarUrl) return;
        try
        {
            var content = await _api.DownloadAsync(avatarUrl);
            CharacterAvatarImage.Source = new Bitmap(new MemoryStream(content.Bytes));
            CharacterAvatarImage.IsVisible = true;
            CharacterAvatarFallback.IsVisible = false;
        }
        catch { }
    }

    private async Task RefreshRoomsAsync(string? preferredId = null)
    {
        var rooms = await _api.GetConversationsAsync();
        _rooms.Clear();
        _rooms.AddRange(rooms.OrderByDescending(room => room.IsPinned).ThenByDescending(room => room.UpdatedAt));
        ConversationCountText.Text = _rooms.Count == 0 ? "还没有私人对话" : $"{_rooms.Count} 个私人对话";
        ApplyRoomFilter();
        var target = _rooms.FirstOrDefault(room => room.Id == preferredId)
                     ?? _rooms.FirstOrDefault(room => room.Id == _conversationId)
                     ?? _rooms.FirstOrDefault();
        if (target is not null) await OpenRoomAsync(target);
    }

    private void ApplyRoomFilter()
    {
        var query = ConversationSearchBox.Text?.Trim();
        var visible = string.IsNullOrWhiteSpace(query)
            ? _rooms
            : _rooms.Where(room => room.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase)).ToList();
        _changingRoom = true;
        ConversationList.ItemsSource = visible.Select(CreateRoomItem).ToArray();
        ConversationList.SelectedItem = ConversationList.Items.Cast<ListBoxItem>()
            .FirstOrDefault(item => item.Tag is ConversationRoom room && room.Id == _conversationId);
        _changingRoom = false;
        ConversationEmptyState.IsVisible = visible.Count == 0;
        ConversationList.IsVisible = visible.Count > 0;
    }

    private static ListBoxItem CreateRoomItem(ConversationRoom room)
    {
        var avatar = new Border
        {
            Width = 46, Height = 46, CornerRadius = new CornerRadius(15), Background = Brush.Parse("#2B3852"),
            Child = new TextBlock
            {
                Text = room.Title.FirstOrDefault().ToString(), Foreground = Brushes.White, FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            }
        };
        var identity = new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new TextBlock { Text = room.Title, Foreground = Brush.Parse("#ECF0F8"), FontWeight = FontWeight.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis },
                new TextBlock { Text = room.MessageCount == 0 ? "还没有消息" : $"{room.MessageCount} 条消息", Foreground = Brush.Parse("#728198"), FontSize = 11 }
            }
        };
        var meta = new StackPanel
        {
            Spacing = 5, HorizontalAlignment = HorizontalAlignment.Right,
            Children =
            {
                new TextBlock { Text = room.UpdatedAt.LocalDateTime.ToString("HH:mm"), Foreground = Brush.Parse("#65758C"), FontSize = 10 },
                new TextBlock { Text = room.IsPinned ? "置顶" : "", Foreground = Brush.Parse("#8D87FF"), FontSize = 9, HorizontalAlignment = HorizontalAlignment.Right }
            }
        };
        var layout = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = 11, Margin = new Thickness(9, 8) };
        Grid.SetColumn(identity, 1); Grid.SetColumn(meta, 2);
        layout.Children.Add(avatar); layout.Children.Add(identity); layout.Children.Add(meta);
        return new ListBoxItem { Tag = room, Content = layout };
    }

    private async Task OpenRoomAsync(ConversationRoom room)
    {
        var revision = ++_roomRevision;
        _conversationId = room.Id;
        if (room.CharacterId is { Length: > 0 } characterId && FindCharacter(characterId) is { } character)
        {
            _character = character;
            await ApplyCharacterAsync();
        }
        _changingRoom = true;
        ConversationList.SelectedItem = ConversationList.Items.Cast<ListBoxItem>()
            .FirstOrDefault(item => item.Tag is ConversationRoom value && value.Id == room.Id);
        _changingRoom = false;
        ConversationStatusText.Text = "正在读取消息…";
        await ReloadMessagesAsync(room.Id, revision);
        StartEventStream(room.Id, revision);
    }

    private async Task ReloadMessagesAsync(string roomId, long revision)
    {
        var messages = await _api.GetMessagesAsync(roomId, 300);
        if (revision != _roomRevision || roomId != _conversationId) return;
        MessagePanel.Children.Clear();
        foreach (var message in messages) MessagePanel.Children.Add(CreateMessageRow(message));
        MessageEmptyState.IsVisible = messages.Count == 0;
        ConversationStatusText.Text = _sending ? "Pry 正在回复…" : "本地私密对话";
        await Dispatcher.UIThread.InvokeAsync(() => MessageScroll.ScrollToEnd(), DispatcherPriority.Background);
    }

    private Control CreateMessageRow(ChatMessage message)
    {
        if (message.Role == ChatRole.System)
            return new TextBlock { Text = message.Content, Foreground = Brush.Parse("#728198"), FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        var isUser = message.Role == ChatRole.User;
        var bubble = new Border
        {
            MaxWidth = 650, Padding = new Thickness(14, 10), CornerRadius = isUser ? new CornerRadius(16, 5, 16, 16) : new CornerRadius(5, 16, 16, 16),
            Background = isUser ? Brush.Parse("#6C63FF") : Brush.Parse("#1B2638"),
            BorderBrush = isUser ? Brushes.Transparent : Brush.Parse("#2A3850"), BorderThickness = new Thickness(1),
            Child = new TextBlock { Text = message.Content, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap, FontSize = 14 }
        };
        var stack = new StackPanel
        {
            Spacing = 4, HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            Children =
            {
                new TextBlock { Text = isUser ? "你" : _character?.Name ?? "Pry", Foreground = Brush.Parse("#718198"), FontSize = 10, HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left },
                bubble,
                new TextBlock { Text = message.CreatedAt.LocalDateTime.ToString("HH:mm"), Foreground = Brush.Parse("#596A80"), FontSize = 9, HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left }
            }
        };
        stack.ContextMenu = CreateMessageMenu(message, isUser);
        return stack;
    }

    private ContextMenu CreateMessageMenu(ChatMessage message, bool isUser)
    {
        var primary = new MenuItem { Header = isUser ? "从这里重新编辑" : "重新生成这条回复" };
        var remove = new MenuItem { Header = "删除消息" };
        primary.Click += async (_, _) =>
        {
            if (_conversationId is null || _sending) return;
            try
            {
                if (isUser)
                {
                    await _api.DeleteMessageAsync(_conversationId, message.Id);
                    ComposerTextBox.Text = message.Content;
                    ComposerTextBox.CaretIndex = message.Content.Length;
                    ComposerTextBox.Focus();
                    ConversationStatusText.Text = "已回到这条消息 · Ctrl+Z 可撤销";
                }
                else
                {
                    SetSending(true);
                    await _api.RegenerateAsync(_conversationId, message.Id);
                    ConversationStatusText.Text = "Pry 正在重新回复…";
                }
                await ReloadMessagesAsync(_conversationId, _roomRevision);
                await RefreshRoomsWithoutSwitchAsync();
            }
            catch (Exception ex)
            {
                SetSending(false);
                await ShowNoticeAsync(isUser ? "无法编辑这条消息" : "无法重新生成回复", ex.Message);
            }
        };
        remove.Click += async (_, _) =>
        {
            if (_conversationId is null || _sending) return;
            try
            {
                var result = await _api.DeleteMessageAsync(_conversationId, message.Id);
                await ReloadMessagesAsync(_conversationId, _roomRevision);
                await RefreshRoomsWithoutSwitchAsync();
                ConversationStatusText.Text = result.CanUndo ? "消息已删除 · Ctrl+Z 可撤销" : "消息已删除";
            }
            catch (Exception ex) { await ShowNoticeAsync("无法删除消息", ex.Message); }
        };
        return new ContextMenu { ItemsSource = new[] { primary, remove } };
    }

    private void StartEventStream(string roomId, long revision)
    {
        _eventCancellation?.Cancel();
        _eventCancellation?.Dispose();
        _eventCancellation = new CancellationTokenSource();
        _ = ObserveEventsAsync(roomId, revision, _eventCancellation.Token);
    }

    private async Task ObserveEventsAsync(string roomId, long revision, CancellationToken token)
    {
        try
        {
            await foreach (var item in _api.ReadEventsAsync(roomId, -1, token))
            {
                if (revision != _roomRevision) return;
                if (item.Type == "message.created")
                    await Dispatcher.UIThread.InvokeAsync(async () => await ReloadMessagesAsync(roomId, revision));
                else if (item.Type is "turn.cancelled" or "turn.completed")
                    await Dispatcher.UIThread.InvokeAsync(() => SetSending(false));
                else if (item.Type == "turn.state" && item.Data is JsonElement data && data.TryGetProperty("state", out var state))
                    await Dispatcher.UIThread.InvokeAsync(() => UpdateTurnState(state.GetString()));
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
                await Dispatcher.UIThread.InvokeAsync(() => ConversationStatusText.Text = $"事件连接中断：{ex.Message}");
        }
    }

    private void UpdateTurnState(string? state)
    {
        var busy = state is not null && !state.Equals("Idle", StringComparison.OrdinalIgnoreCase);
        SetSending(busy);
        ConversationStatusText.Text = state switch
        {
            "ModelThinking" => "Pry 正在思考…",
            "AgentPending" => "Pry 正在组织回复…",
            "AgentSending" => "Pry 正在输入…",
            "UserPending" => "消息已送达",
            _ => "本地私密对话"
        };
    }

    private void SetSending(bool value)
    {
        _sending = value;
        SendButton.IsEnabled = !value;
        CancelReplyButton.IsVisible = value;
    }

    private async Task SendAsync(string? stickerId = null)
    {
        if (_conversationId is null || _sending) return;
        var text = ComposerTextBox.Text?.Trim() ?? "";
        if (text.Length == 0 && stickerId is null && _attachments.Count == 0) return;
        SetSending(true);
        try
        {
            await _api.SubmitTurnAsync(_conversationId, new SubmitTurnRequest(text, stickerId,
                AttachmentIds: _attachments.Select(item => item.Id).ToArray()));
            ComposerTextBox.Text = "";
            _attachments.Clear();
            UpdateAttachmentButton();
            await ReloadMessagesAsync(_conversationId, _roomRevision);
            await RefreshRoomsWithoutSwitchAsync();
        }
        catch (Exception ex)
        {
            SetSending(false);
            await ShowNoticeAsync("消息没有发送", ex.Message);
        }
    }

    private async Task RefreshRoomsWithoutSwitchAsync()
    {
        var rooms = await _api.GetConversationsAsync();
        _rooms.Clear(); _rooms.AddRange(rooms.OrderByDescending(room => room.IsPinned).ThenByDescending(room => room.UpdatedAt));
        ConversationCountText.Text = $"{_rooms.Count} 个私人对话";
        ApplyRoomFilter();
    }

    private async Task CreateConversationAsync()
    {
        try
        {
            var room = await _api.CreateConversationAsync(_character?.Id);
            await RefreshRoomsAsync(room.Id);
        }
        catch (Exception ex) { await ShowNoticeAsync("无法创建对话", ex.Message); }
    }

    private async void ConversationList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_changingRoom || ConversationList.SelectedItem is not ListBoxItem { Tag: ConversationRoom room } || room.Id == _conversationId) return;
        try { await OpenRoomAsync(room); }
        catch (Exception ex) { await ShowNoticeAsync("无法打开对话", ex.Message); }
    }

    private void ConversationSearch_TextChanged(object? sender, TextChangedEventArgs e) => ApplyRoomFilter();
    private async void NewConversation_Click(object? sender, RoutedEventArgs e) => await CreateConversationAsync();
    private async void Send_Click(object? sender, RoutedEventArgs e) => await SendAsync();
    private async void Composer_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || e.KeyModifiers.HasFlag(KeyModifiers.Shift)) return;
        e.Handled = true;
        await SendAsync();
    }

    private async void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Z || !e.KeyModifiers.HasFlag(KeyModifiers.Control) || _conversationId is null || _sending) return;
        e.Handled = true;
        try
        {
            await _api.UndoAsync(_conversationId);
            await ReloadMessagesAsync(_conversationId, _roomRevision);
            await RefreshRoomsWithoutSwitchAsync();
            ConversationStatusText.Text = "已撤销上一条消息操作";
        }
        catch (PryBackendException ex) when (ex.Code == "validation_error")
        {
            ConversationStatusText.Text = "没有可撤销的消息操作";
        }
        catch (Exception ex) { await ShowNoticeAsync("无法撤销", ex.Message); }
    }

    private async void CancelReply_Click(object? sender, RoutedEventArgs e)
    {
        if (_conversationId is null) return;
        try { await _api.CancelTurnAsync(_conversationId); SetSending(false); }
        catch (Exception ex) { await ShowNoticeAsync("无法停止回复", ex.Message); }
    }

    private async void ConversationMenu_Click(object? sender, RoutedEventArgs e)
    {
        if (_conversationId is null || _rooms.FirstOrDefault(item => item.Id == _conversationId) is not { } room) return;

        var window = CreateDialog("会话设置", 520, 400);
        var titleInput = new TextBox { Text = room.Title, Watermark = "会话名称", MaxLength = 80 };
        var save = new Button { Content = "保存名称", Classes = { "primary" }, Width = 100 };
        var pin = new Button { Content = room.IsPinned ? "取消置顶" : "置顶会话", Width = 100 };
        var delete = new Button { Content = "删除会话", Foreground = Brush.Parse("#FF9A9A"), Width = 100 };
        var close = new Button { Content = "完成", Width = 80 };
        var deleteArmed = false;

        save.Click += async (_, _) =>
        {
            var title = titleInput.Text?.Trim();
            if (string.IsNullOrWhiteSpace(title)) { titleInput.Focus(); return; }
            await RunConversationMutationAsync(window, "无法保存名称", async () =>
            {
                await _api.UpdateConversationAsync(room.Id, new UpdateConversationRequest(title, null, null));
                await RefreshRoomsWithoutSwitchAsync();
                window.Close();
            });
        };
        pin.Click += async (_, _) => await RunConversationMutationAsync(window, "无法更改置顶状态", async () =>
        {
            await _api.UpdateConversationAsync(room.Id, new UpdateConversationRequest(null, !room.IsPinned, null));
            await RefreshRoomsWithoutSwitchAsync();
            window.Close();
        });
        delete.Click += async (_, _) =>
        {
            if (!deleteArmed)
            {
                deleteArmed = true;
                delete.Content = "再次点击确认";
                delete.Background = Brush.Parse("#6E2932");
                return;
            }
            await RunConversationMutationAsync(window, "无法删除会话", async () =>
            {
                _eventCancellation?.Cancel();
                await _api.DeleteConversationAsync(room.Id);
                _conversationId = null;
                window.Close();
                await RefreshRoomsAsync();
                if (_conversationId is null) await CreateConversationAsync();
            });
        };
        close.Click += (_, _) => window.Close();

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right,
            Children = { delete, pin, save, close }
        };
        var panel = new StackPanel
        {
            Margin = new Thickness(22), Spacing = 16,
            Children =
            {
                new TextBlock { Text = "会话设置", FontSize = 22, FontWeight = FontWeight.SemiBold },
                new TextBlock { Text = "名称", Foreground = Brush.Parse("#8492A8"), FontSize = 11 },
                titleInput,
                new TextBlock { Text = "删除后，该会话与其中的消息将无法从界面恢复。", Foreground = Brush.Parse("#78879C"), TextWrapping = TextWrapping.Wrap },
                actions
            }
        };
        window.Content = CreateThemedDialogSurface(panel);
        await window.ShowDialog(this);
    }

    private async Task RunConversationMutationAsync(Window owner, string errorTitle, Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex)
        {
            owner.Close();
            await ShowNoticeAsync(errorTitle, ex.Message);
        }
    }

    private async void Attach_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "添加到消息", AllowMultiple = true });
        foreach (var file in files)
        {
            var path = file.TryGetLocalPath();
            if (path is null) continue;
            try
            {
                await using var stream = File.OpenRead(path);
                _attachments.Add(await _api.UploadAsync(stream, Path.GetFileName(path), ContentType(path)));
            }
            catch (Exception ex) { await ShowNoticeAsync("附件没有添加", $"{file.Name}：{ex.Message}"); }
        }
        UpdateAttachmentButton();
    }

    private void UpdateAttachmentButton()
    {
        AttachButton.Content = _attachments.Count == 0 ? "＋" : $"＋ {_attachments.Count}";
    }

    private static string ContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png", ".jpg" or ".jpeg" => "image/jpeg", ".webp" => "image/webp",
        ".txt" or ".md" => "text/plain", ".csv" => "text/csv", ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        _ => "application/octet-stream"
    };

    private async void Sticker_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var stickers = (await _api.GetStickersAsync()).Where(item => item.Enabled).ToArray();
            if (stickers.Length == 0) { await ShowNoticeAsync("没有可用表情", "可以稍后在内容管理中导入表情。"); return; }
            var window = CreateDialog("选择表情", 520, 420);
            var list = new ListBox { ItemsSource = stickers.Select(item => new ListBoxItem { Content = item.Name, Tag = item }).ToArray() };
            list.DoubleTapped += async (_, _) =>
            {
                if (list.SelectedItem is ListBoxItem { Tag: StickerResponse sticker }) { window.Close(); await SendAsync(sticker.Id); }
            };
            window.Content = CreateThemedDialogSurface(new Grid { Margin = new Thickness(18), Children = { list } });
            await window.ShowDialog(this);
        }
        catch (Exception ex) { await ShowNoticeAsync("表情加载失败", ex.Message); }
    }

    private async void ShowCharacters_Click(object? sender, RoutedEventArgs e)
    {
        var window = CreateDialog("选择聊天角色", 520, 460);
        var list = new ListBox { ItemsSource = _characters.Select(item => new ListBoxItem { Content = $"{item.Name}\n{item.CardName}", Tag = item, Padding = new Thickness(12) }).ToArray() };
        list.DoubleTapped += async (_, _) =>
        {
            if (list.SelectedItem is not ListBoxItem { Tag: CharacterSummaryResponse character }) return;
            _character = character; await ApplyCharacterAsync(); window.Close(); await CreateConversationAsync();
        };
        window.Content = CreateThemedDialogSurface(new Grid { Margin = new Thickness(18), Children = { list } });
        await window.ShowDialog(this);
    }

    private async void ShowMemories_Click(object? sender, RoutedEventArgs e)
    {
        if (_character is null) return;
        try
        {
            var memories = await _api.GetMemoriesAsync(_character.Id);
            var window = CreateDialog($"{_character.Name} 的长期记忆", 680, 520);
            var panel = new StackPanel { Margin = new Thickness(20), Spacing = 10 };
            panel.Children.Add(new TextBlock { Text = "长期记忆", FontSize = 21, FontWeight = FontWeight.SemiBold });
            if (memories.Count == 0) panel.Children.Add(new TextBlock { Text = "还没有形成长期记忆。", Foreground = Brush.Parse("#7F8EA5") });
            foreach (var memory in memories) panel.Children.Add(new Border { Padding = new Thickness(13), CornerRadius = new CornerRadius(11), Background = Brush.Parse("#182235"), Child = new TextBlock { Text = memory.Summary, TextWrapping = TextWrapping.Wrap } });
            window.Content = CreateThemedDialogSurface(new ScrollViewer { Content = panel });
            await window.ShowDialog(this);
        }
        catch (Exception ex) { await ShowNoticeAsync("记忆加载失败", ex.Message); }
    }

    private async void OpenSettings_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var runtime = await _api.GetRuntimeAsync();
            var models = await _api.GetModelsAsync();
            var window = CreateDialog("设置", 720, 560);
            var panel = new StackPanel { Margin = new Thickness(24), Spacing = 14 };
            panel.Children.Add(new TextBlock { Text = "设置", FontSize = 24, FontWeight = FontWeight.SemiBold });
            panel.Children.Add(SettingCard("本地运行状态", runtime.State == "ready" ? "服务正常，可以开始聊天" : runtime.Error ?? runtime.State));
            panel.Children.Add(SettingCard("当前文字模型", models.FirstOrDefault(item => item.SelectedForText)?.DisplayName ?? "尚未选择"));
            panel.Children.Add(SettingCard("界面", "全新的 Pry 私聊界面。更多主题、密度和背景设置将在这套前端中重新设计。"));
            var close = new Button { Content = "完成", Classes = { "primary" }, HorizontalAlignment = HorizontalAlignment.Right, Width = 88 };
            close.Click += (_, _) => window.Close(); panel.Children.Add(close);
            window.Content = CreateThemedDialogSurface(new ScrollViewer { Content = panel });
            await window.ShowDialog(this);
        }
        catch (Exception ex) { await ShowNoticeAsync("设置加载失败", ex.Message); }
    }

    private static Border SettingCard(string title, string value) => new()
    {
        Padding = new Thickness(15), CornerRadius = new CornerRadius(12), Background = Brush.Parse("#182235"),
        Child = new StackPanel { Spacing = 5, Children = { new TextBlock { Text = title, FontWeight = FontWeight.SemiBold }, new TextBlock { Text = value, Foreground = Brush.Parse("#8492A8"), TextWrapping = TextWrapping.Wrap } } }
    };

    private void ShowChats_Click(object? sender, RoutedEventArgs e) => ConversationSearchBox.Focus();
    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (e.ClickCount == 2) ToggleMaximize(); else BeginMoveDrag(e);
    }
    private void Minimize_Click(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object? sender, RoutedEventArgs e) => ToggleMaximize();
    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
    private void ToggleMaximize() => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void ShowEmptyError(string title, string detail)
    {
        MessagePanel.Children.Clear();
        MessagePanel.Children.Add(new StackPanel
        {
            Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center,
            Children = { new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeight.SemiBold }, new TextBlock { Text = detail, Foreground = Brush.Parse("#8492A8"), TextWrapping = TextWrapping.Wrap, MaxWidth = 520 } }
        });
        MessageEmptyState.IsVisible = false;
    }

    private async Task ShowNoticeAsync(string title, string detail)
    {
        var window = CreateDialog(title, 500, 260);
        var close = new Button { Content = "知道了", Classes = { "primary" }, HorizontalAlignment = HorizontalAlignment.Right };
        close.Click += (_, _) => window.Close();
        var content = new StackPanel { Margin = new Thickness(22), Spacing = 16, Children = { new TextBlock { Text = title, FontSize = 20, FontWeight = FontWeight.SemiBold }, new TextBlock { Text = detail, TextWrapping = TextWrapping.Wrap }, close } };
        window.Content = CreateThemedDialogSurface(content);
        await window.ShowDialog(this);
    }

    private static Window CreateDialog(string title, double width, double height) => new()
    {
        Title = title, Width = width, Height = height, MinWidth = Math.Min(width, 420), MinHeight = Math.Min(height, 220),
        WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Brush.Parse("#101827")
    };

    internal Border CreateCompactDialogTextCard(Control content) => new()
    {
        Background = Brush.Parse("#182235"), BorderBrush = Brush.Parse("#2A3850"), BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(14), Padding = new Thickness(18), Child = content
    };

    internal static Grid CreateCompactDialogLayout(Control contentCard, Control actions)
    {
        var layout = new Grid { Margin = new Thickness(16), RowDefinitions = new RowDefinitions("*,Auto"), RowSpacing = 14, Children = { contentCard, actions } };
        Grid.SetRow(actions, 1); return layout;
    }

    internal Control CreateThemedDialogSurface(Control content, bool transparentContent = false,
        Action<Image?, Border?>? captureBackground = null)
    {
        var dim = new Border { Background = Brush.Parse("#101827") };
        captureBackground?.Invoke(null, dim);
        return new Grid { Background = Brush.Parse("#101827"), Children = { dim, content } };
    }
}
