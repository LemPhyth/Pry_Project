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
using NAudio.Wave;
using Pry.Client;
using Pry.Contracts;
using Pry.Core.Models;

namespace Pry.App;

public sealed partial class PryMessengerWindow : Window
{
    private readonly PryBackendClient _api;
    private readonly List<ConversationRoom> _rooms = [];
    private readonly List<ConversationFolder> _folders = [];
    private readonly List<CharacterSummaryResponse> _characters = [];
    private readonly List<MediaAssetResponse> _attachments = [];
    private readonly List<StickerResponse> _stickers = [];
    private readonly Dictionary<string, string> _lastMessagePreviews = new(StringComparer.Ordinal);
    private ClientPreferencesResponse? _preferences;
    private CharacterSummaryResponse? _character;
    private string? _conversationId;
    private CancellationTokenSource? _eventCancellation;
    private bool _changingRoom;
    private bool _sending;
    private bool _allowClose;
    private bool _sidebarCollapsed;
    private double _sidebarWidth = 306;
    private WaveInEvent? _waveInput;
    private WaveFileWriter? _waveWriter;
    private TaskCompletionSource? _recordingStopped;
    private string? _recordingPath;
    private bool _speechBusy;
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
        _waveInput?.StopRecording();
        _waveWriter?.Dispose();
        _waveInput?.Dispose();
        if (_recordingPath is not null) { try { File.Delete(_recordingPath); } catch { } }
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
            _folders.AddRange(await _api.GetFoldersAsync());
            _stickers.AddRange(await _api.GetStickersAsync());
            _character = FindCharacter(_preferences.SelectedCharacterId) ?? _characters.FirstOrDefault();
            await ApplyCharacterAsync();
            await RefreshRoomsAsync(_preferences.ActiveConversationId);
            if (_conversationId is null) await CreateConversationAsync();
            var runtime = await _api.GetRuntimeAsync();
            HeaderConnectionText.Text = runtime.State == "ready" ? "本地 API 与模型服务已连接" : "本地 API 已连接 · 模型服务启动失败";
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

    private ListBoxItem CreateRoomItem(ConversationRoom room)
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
                new TextBlock { Text = RoomSubtitle(room), Foreground = Brush.Parse("#728198"), FontSize = 11 }
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
        var item = new ListBoxItem { Tag = room, Content = layout };
        var settings = new MenuItem { Header = "会话设置" };
        settings.Click += async (_, _) => { await OpenRoomAsync(room); ConversationMenu_Click(item, new RoutedEventArgs()); };
        var pin = new MenuItem { Header = room.IsPinned ? "取消置顶" : "置顶会话" };
        pin.Click += async (_, _) => { await _api.UpdateConversationAsync(room.Id, new UpdateConversationRequest(null, !room.IsPinned, null)); await RefreshRoomsWithoutSwitchAsync(); };
        item.ContextMenu = new ContextMenu { ItemsSource = new[] { settings, pin } };
        item.PointerPressed += (_, args) =>
        {
            if (!args.GetCurrentPoint(item).Properties.IsRightButtonPressed) return;
            args.Handled = true; item.ContextMenu?.Open(item);
        };
        return item;
    }

    private string RoomSubtitle(ConversationRoom room)
    {
        var count = _lastMessagePreviews.GetValueOrDefault(room.Id, room.MessageCount == 0 ? "还没有消息" : "打开会话以读取最近消息");
        var folder = _folders.FirstOrDefault(item => item.Id == room.FolderId)?.Name;
        return folder is null ? count : $"{folder} · {count}";
    }

    private static string MessagePreview(ChatMessage message)
    {
        var value = !string.IsNullOrWhiteSpace(message.Content) ? message.Content.Trim()
            : message.StickerId is not null ? "[表情]" : message.ImagePath is not null ? "[图片]" : "[附件]";
        return value.Length > 34 ? value[..34] + "…" : value;
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
        var lastMessage = messages.LastOrDefault(item => item.Role != ChatRole.System);
        _lastMessagePreviews[roomId] = lastMessage is null ? "还没有消息" : MessagePreview(lastMessage);
        ApplyRoomFilter();
        MessageEmptyState.IsVisible = messages.Count == 0;
        ConversationStatusText.Text = _sending ? "Pry 正在回复…" : "本地私密对话";
        await Dispatcher.UIThread.InvokeAsync(() => MessageScroll.ScrollToEnd(), DispatcherPriority.Background);
    }

    private Control CreateMessageRow(ChatMessage message)
    {
        if (message.Role == ChatRole.System)
            return new TextBlock { Text = message.Content, Foreground = Brush.Parse("#728198"), FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        var isUser = message.Role == ChatRole.User;
        var content = new StackPanel { Spacing = 8 };
        if (!string.IsNullOrWhiteSpace(message.Content))
            content.Children.Add(new TextBlock { Text = message.Content, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap, FontSize = 14 });
        var mediaUrl = message.StickerId is { Length: > 0 } stickerId
            ? _stickers.FirstOrDefault(item => item.Id == stickerId)?.ContentUrl
            : message.ImagePath;
        if (!string.IsNullOrWhiteSpace(mediaUrl))
        {
            var image = new Image { MaxWidth = 360, MaxHeight = 280, Stretch = Stretch.Uniform, IsVisible = false };
            content.Children.Add(image);
            _ = LoadMessageImageAsync(image, mediaUrl);
        }
        var bubble = new Border
        {
            MaxWidth = 650, Padding = new Thickness(14, 10), CornerRadius = isUser ? new CornerRadius(16, 5, 16, 16) : new CornerRadius(5, 16, 16, 16),
            Background = isUser ? Brush.Parse("#6C63FF") : Brush.Parse("#1B2638"),
            BorderBrush = isUser ? Brushes.Transparent : Brush.Parse("#2A3850"), BorderThickness = new Thickness(1),
            Child = content
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

    private async Task LoadMessageImageAsync(Image image, string relativeUrl)
    {
        try
        {
            var downloaded = await _api.DownloadAsync(relativeUrl);
            var bitmap = new Bitmap(new MemoryStream(downloaded.Bytes));
            await Dispatcher.UIThread.InvokeAsync(() => { image.Source = bitmap; image.IsVisible = true; });
        }
        catch { }
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
        if (e.Key == Key.N && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            e.Handled = true; await CreateConversationAsync(); return;
        }
        if (e.Key == Key.E && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            e.Handled = true; Sticker_Click(sender, new RoutedEventArgs()); return;
        }
        if (e.Key == Key.Escape && _sending && _conversationId is not null)
        {
            e.Handled = true;
            try { await _api.CancelTurnAsync(_conversationId); SetSending(false); }
            catch (Exception ex) { await ShowNoticeAsync("无法停止回复", ex.Message); }
            return;
        }
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

    private void ConversationMenu_Click(object? sender, RoutedEventArgs e)
    {
        if (_conversationId is null || _rooms.FirstOrDefault(item => item.Id == _conversationId) is not { } room) return;

        var titleInput = new TextBox { Text = room.Title, Watermark = "会话名称", MaxLength = 80 };
        var save = new Button { Content = "保存名称", Classes = { "primary" }, Width = 100 };
        var pin = new Button { Content = room.IsPinned ? "取消置顶" : "置顶会话", Width = 100 };
        var folder = new ComboBox { ItemsSource = new[] { new FolderChoice(null, "未分组") }.Concat(_folders.Select(item => new FolderChoice(item.Id, item.Name))).ToArray() };
        folder.SelectedItem = folder.Items.Cast<FolderChoice>().FirstOrDefault(item => item.Id == room.FolderId) ?? folder.Items.Cast<FolderChoice>().First();
        var move = new Button { Content = "移动到分组", Width = 110 };
        var delete = new Button { Content = "删除会话", Foreground = Brush.Parse("#FF9A9A"), Width = 100 };
        var close = new Button { Content = "完成", Width = 80 };
        var deleteArmed = false;

        save.Click += async (_, _) =>
        {
            var title = titleInput.Text?.Trim();
            if (string.IsNullOrWhiteSpace(title)) { titleInput.Focus(); return; }
            await RunConversationMutationAsync("无法保存名称", async () =>
            {
                await _api.UpdateConversationAsync(room.Id, new UpdateConversationRequest(title, null, null));
                await RefreshRoomsWithoutSwitchAsync();
                CloseWorkspacePage();
            });
        };
        pin.Click += async (_, _) => await RunConversationMutationAsync("无法更改置顶状态", async () =>
        {
            await _api.UpdateConversationAsync(room.Id, new UpdateConversationRequest(null, !room.IsPinned, null));
            await RefreshRoomsWithoutSwitchAsync();
            CloseWorkspacePage();
        });
        move.Click += async (_, _) => await RunConversationMutationAsync("无法移动会话", async () =>
        {
            var target = folder.SelectedItem as FolderChoice;
            await _api.UpdateConversationAsync(room.Id, new UpdateConversationRequest(null, null, target?.Id, target?.Id is null));
            await RefreshRoomsWithoutSwitchAsync();
            CloseWorkspacePage();
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
            await RunConversationMutationAsync("无法删除会话", async () =>
            {
                _eventCancellation?.Cancel();
                await _api.DeleteConversationAsync(room.Id);
                _conversationId = null;
                CloseWorkspacePage();
                await RefreshRoomsAsync();
                if (_conversationId is null) await CreateConversationAsync();
            });
        };
        close.Click += (_, _) => CloseWorkspacePage();

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
                new TextBlock { Text = "所在分组", Foreground = Brush.Parse("#8492A8"), FontSize = 11 },
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { folder, move } },
                new TextBlock { Text = "删除后，该会话与其中的消息将无法从界面恢复。", Foreground = Brush.Parse("#78879C"), TextWrapping = TextWrapping.Wrap },
                actions
            }
        };
        ShowWorkspacePage("会话设置", panel);
    }

    private void Folders_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control anchor) return;
        var list = new ListBox { MinHeight = 220 };
        var input = new TextBox { Watermark = "分组名称", MaxLength = 60 };
        var add = new Button { Content = "新建", Classes = { "primary" } };
        var rename = new Button { Content = "重命名" };
        var remove = new Button { Content = "删除" };
        void Render() => list.ItemsSource = _folders.Select(item => new ListBoxItem { Content = item.Name, Tag = item, Padding = new Thickness(11) }).ToArray();
        list.SelectionChanged += (_, _) => { if (list.SelectedItem is ListBoxItem { Tag: ConversationFolder item }) input.Text = item.Name; };
        add.Click += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(input.Text)) return;
            await RunFolderMutationAsync(async () => await _api.CreateFolderAsync(input.Text.Trim()), "无法创建分组");
            input.Text = ""; Render();
        };
        rename.Click += async (_, _) =>
        {
            if (list.SelectedItem is not ListBoxItem { Tag: ConversationFolder item } || string.IsNullOrWhiteSpace(input.Text)) return;
            await RunFolderMutationAsync(async () => await _api.RenameFolderAsync(item.Id, input.Text.Trim()), "无法重命名分组"); Render();
        };
        remove.Click += async (_, _) =>
        {
            if (list.SelectedItem is not ListBoxItem { Tag: ConversationFolder item }) return;
            await RunFolderMutationAsync(async () => await _api.DeleteFolderAsync(item.Id), "无法删除分组"); input.Text = ""; Render(); await RefreshRoomsWithoutSwitchAsync();
        };
        Render();
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { add, rename, remove } };
        var panel = new Grid { Width = 340, Height = 390, RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"), RowSpacing = 10, Margin = new Thickness(16), Children = { new TextBlock { Text = "会话分组", FontSize = 18, FontWeight = FontWeight.SemiBold }, list, input, buttons } };
        Grid.SetRow(list, 1); Grid.SetRow(input, 2); Grid.SetRow(buttons, 3);
        new Flyout { Content = new Border { Background = Brush.Parse("#182235"), BorderBrush = Brush.Parse("#33425D"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(14), Child = panel } }.ShowAt(anchor);
    }

    private async Task RunFolderMutationAsync(Func<Task> action, string errorTitle)
    {
        try { await action(); _folders.Clear(); _folders.AddRange(await _api.GetFoldersAsync()); }
        catch (Exception ex) { await ShowNoticeAsync(errorTitle, ex.Message); }
    }

    private async Task RunConversationMutationAsync(string errorTitle, Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex)
        {
            await ShowNoticeAsync(errorTitle, ex.Message);
        }
    }

    private sealed record FolderChoice(string? Id, string Name) { public override string ToString() => Name; }

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

    private async void VoiceButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_speechBusy) return;
        if (_waveInput is null) await StartRecordingAsync();
        else await StopRecordingAndRecognizeAsync();
    }

    private async Task StartRecordingAsync()
    {
        try
        {
            var speechId = _preferences?.ActiveSpeechModelId ?? throw new InvalidOperationException("请先在设置中选择语音识别模型。");
            var profile = (await _api.GetSpeechModelsAsync()).FirstOrDefault(item => item.Id == speechId);
            if (profile is null || !profile.Available) throw new InvalidOperationException("所选语音识别模型当前不可用。");
            if (WaveInEvent.DeviceCount == 0) throw new InvalidOperationException("没有检测到可用麦克风。");
            var tempDirectory = Path.Combine(Path.GetTempPath(), "PryCompanion"); Directory.CreateDirectory(tempDirectory);
            _recordingPath = Path.Combine(tempDirectory, $"voice-{Guid.NewGuid():N}.wav");
            _recordingStopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _waveInput = new WaveInEvent { WaveFormat = new WaveFormat(profile.SampleRate, 16, 1), BufferMilliseconds = 80 };
            _waveWriter = new WaveFileWriter(_recordingPath, _waveInput.WaveFormat);
            _waveInput.DataAvailable += (_, args) => _waveWriter?.Write(args.Buffer, 0, args.BytesRecorded);
            _waveInput.RecordingStopped += (_, args) =>
            {
                _waveWriter?.Dispose(); _waveWriter = null;
                if (args.Exception is not null) _recordingStopped?.TrySetException(args.Exception); else _recordingStopped?.TrySetResult();
            };
            _waveInput.StartRecording(); VoiceButton.Content = "■"; VoiceButton.Classes.Add("active"); ConversationStatusText.Text = "正在录音，再次点击结束";
        }
        catch (Exception ex) { await ResetRecordingAsync(); await ShowNoticeAsync("无法开始录音", ex.Message); }
    }

    private async Task StopRecordingAndRecognizeAsync()
    {
        _speechBusy = true; VoiceButton.IsEnabled = false; ConversationStatusText.Text = "正在识别语音…";
        try
        {
            _waveInput!.StopRecording(); if (_recordingStopped is not null) await _recordingStopped.Task;
            _waveInput.Dispose(); _waveInput = null;
            var path = _recordingPath ?? throw new InvalidOperationException("没有可转写的录音。");
            await using var recording = File.OpenRead(path);
            var uploaded = await _api.UploadAsync(recording, Path.GetFileName(path), "audio/wav");
            var text = (await _api.TranscribeAsync(uploaded.Id)).Text;
            if (string.IsNullOrWhiteSpace(text)) await ShowNoticeAsync("没有识别到文字", "请靠近麦克风后重试。");
            else
            {
                var existing = ComposerTextBox.Text ?? "";
                ComposerTextBox.Text = string.IsNullOrWhiteSpace(existing) ? text : $"{existing.TrimEnd()} {text}";
                ComposerTextBox.CaretIndex = ComposerTextBox.Text.Length; ComposerTextBox.Focus();
            }
        }
        catch (Exception ex) { await ShowNoticeAsync("语音识别失败", ex.Message); }
        finally { await ResetRecordingAsync(); }
    }

    private Task ResetRecordingAsync()
    {
        _waveWriter?.Dispose(); _waveWriter = null; _waveInput?.Dispose(); _waveInput = null;
        if (_recordingPath is not null) { try { File.Delete(_recordingPath); } catch { } }
        _recordingPath = null; _recordingStopped = null; _speechBusy = false; VoiceButton.Content = new TextBlock { FontFamily = new FontFamily("Segoe Fluent Icons"), Text = "\uE720", FontSize = 16 }; VoiceButton.Classes.Remove("active"); VoiceButton.IsEnabled = true;
        if (!_sending) ConversationStatusText.Text = "本地私密对话";
        return Task.CompletedTask;
    }

    private void UpdateAttachmentButton()
    {
        AttachButton.Content = _attachments.Count == 0 ? "＋" : $"＋ {_attachments.Count}";
        AttachmentDraftPanel.Children.Clear();
        foreach (var attachment in _attachments.ToArray())
        {
            var remove = new Button { Content = "×", FontSize = 11, Width = 24, Height = 24, Padding = new Thickness(0), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
            remove.Click += (_, _) => { _attachments.Remove(attachment); UpdateAttachmentButton(); };
            var preview = new Grid { Width = attachment.Kind.Equals("Image", StringComparison.OrdinalIgnoreCase) ? 86 : 150, Height = 58, Margin = new Thickness(0, 0, 7, 5) };
            var background = new Border { Background = Brush.Parse("#1B2638"), BorderBrush = Brush.Parse("#33425D"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Padding = new Thickness(7), Child = new TextBlock { Text = attachment.Name, FontSize = 9, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Bottom } };
            preview.Children.Add(background); preview.Children.Add(remove); AttachmentDraftPanel.Children.Add(preview);
            if (attachment.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) _ = LoadAttachmentPreviewAsync(background, attachment.DownloadUrl);
        }
        AttachmentDraftPanel.IsVisible = _attachments.Count > 0;
    }

    private async Task LoadAttachmentPreviewAsync(Border host, string url)
    {
        try
        {
            var content = await _api.DownloadAsync(url);
            var image = new Image { Source = new Bitmap(new MemoryStream(content.Bytes)), Stretch = Stretch.UniformToFill };
            RenderOptions.SetBitmapInterpolationMode(image, BitmapInterpolationMode.HighQuality);
            await Dispatcher.UIThread.InvokeAsync(() => { host.Padding = new Thickness(0); host.ClipToBounds = true; host.Child = image; });
        }
        catch { }
    }

    private static string ContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png", ".jpg" or ".jpeg" => "image/jpeg", ".webp" => "image/webp",
        ".txt" or ".md" => "text/plain", ".csv" => "text/csv", ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        _ => "application/octet-stream"
    };

    private async void Sticker_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control anchor) return;
        try
        {
            var stickers = (await _api.GetStickersAsync()).Where(item => item.Enabled).ToArray();
            _stickers.Clear(); _stickers.AddRange(stickers);
            if (stickers.Length == 0) { await ShowNoticeAsync("没有可用表情", "可以稍后在内容管理中导入表情。"); return; }
            var list = new ListBox { ItemsSource = stickers.Select(item => new ListBoxItem { Content = item.Name, Tag = item }).ToArray() };
            list.DoubleTapped += async (_, _) =>
            {
                if (list.SelectedItem is ListBoxItem { Tag: StickerResponse sticker }) await SendAsync(sticker.Id);
            };
            var manage = new Button { Content = "管理表情", HorizontalAlignment = HorizontalAlignment.Right };
            manage.Click += async (_, _) =>
            {
                var manager = new PryMessengerStickerWindow(_api);
                manager.CloseRequested += () => { CloseWorkspacePage(); Sticker_Click(StickerButton, new RoutedEventArgs()); };
                ShowWorkspacePage("管理表情", manager);
            };
            var content = new Grid { Width = 360, Height = 400, RowDefinitions = new RowDefinitions("Auto,*,Auto"), RowSpacing = 10, Margin = new Thickness(16), Children = { new TextBlock { Text = "选择表情", FontSize = 18, FontWeight = FontWeight.SemiBold }, list, manage } }; Grid.SetRow(list, 1); Grid.SetRow(manage, 2);
            new Flyout { Content = new Border { Background = Brush.Parse("#182235"), BorderBrush = Brush.Parse("#33425D"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(14), Child = content } }.ShowAt(anchor);
        }
        catch (Exception ex) { await ShowNoticeAsync("表情加载失败", ex.Message); }
    }

    private void ShowCharacters_Click(object? sender, RoutedEventArgs e)
    {
        SetActiveNav(CharactersNavButton);
        var page = new PryMessengerCharacterWindow(_api);
        page.CloseRequested += () => CloseWorkspacePage();
        page.CharacterActivated += async character => { _character = character; await ApplyCharacterAsync(); CloseWorkspacePage(); await CreateConversationAsync(); };
        ShowWorkspacePage("角色", page);
    }

    private void ShowMemories_Click(object? sender, RoutedEventArgs e)
    {
        if (_character is null) return;
        SetActiveNav(MemoriesNavButton);
        ShowWorkspacePage("长期记忆", new PryMessengerMemoryWindow(_api, _character));
    }

    private void OpenSettings_Click(object? sender, RoutedEventArgs e)
    {
        SetActiveNav(SettingsNavButton);
        var page = new PryMessengerSettingsWindow(_api);
        page.CloseRequested += async () => { if (page.Saved) _preferences = await _api.GetPreferencesAsync(); CloseWorkspacePage(); };
        ShowWorkspacePage("设置", page);
    }

    private void ShowChats_Click(object? sender, RoutedEventArgs e) { SetActiveNav(ChatsNavButton); CloseWorkspacePage(false); ConversationSearchBox.Focus(); }
    private void ShowWorkspacePage(string title, Control content)
    {
        WorkspacePageTitle.Text = title; WorkspacePageContent.Content = content; WorkspacePage.IsVisible = true;
    }
    private void CloseWorkspacePage_Click(object? sender, RoutedEventArgs e) => CloseWorkspacePage();
    private void CloseWorkspacePage(bool selectChats = true) { WorkspacePage.IsVisible = false; WorkspacePageContent.Content = null; if (selectChats) SetActiveNav(ChatsNavButton); }
    private void SetActiveNav(Button active)
    {
        foreach (var button in new[] { ChatsNavButton, CharactersNavButton, MemoriesNavButton, SettingsNavButton })
            button.Classes.Set("active", ReferenceEquals(button, active));
    }
    private void ToggleSidebar_Click(object? sender, RoutedEventArgs e)
    {
        var column = MainLayout.ColumnDefinitions[1];
        if (_sidebarCollapsed) { column.MinWidth = 238; column.Width = new GridLength(_sidebarWidth); }
        else { _sidebarWidth = Math.Max(column.ActualWidth, 238); column.MinWidth = 0; column.Width = new GridLength(0); }
        _sidebarCollapsed = !_sidebarCollapsed;
    }
    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (e.ClickCount == 2) ToggleMaximize(); else BeginMoveDrag(e);
    }
    private void Minimize_Click(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object? sender, RoutedEventArgs e) => ToggleMaximize();
    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
    private void ToggleMaximize() => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void BeginResize(WindowEdge edge, PointerPressedEventArgs e) { if (WindowState == WindowState.Normal && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginResizeDrag(edge, e); }
    private void ResizeLeft_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResize(WindowEdge.West, e);
    private void ResizeRight_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResize(WindowEdge.East, e);
    private void ResizeTop_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResize(WindowEdge.North, e);
    private void ResizeBottom_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResize(WindowEdge.South, e);
    private void ResizeTopLeft_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResize(WindowEdge.NorthWest, e);
    private void ResizeTopRight_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResize(WindowEdge.NorthEast, e);
    private void ResizeBottomLeft_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResize(WindowEdge.SouthWest, e);
    private void ResizeBottomRight_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResize(WindowEdge.SouthEast, e);

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

    private Task ShowNoticeAsync(string title, string detail)
    {
        var close = new Button { Content = "知道了", Classes = { "primary" }, HorizontalAlignment = HorizontalAlignment.Right };
        close.Click += (_, _) => CloseWorkspacePage();
        var content = new StackPanel { Margin = new Thickness(22), Spacing = 16, Children = { new TextBlock { Text = title, FontSize = 20, FontWeight = FontWeight.SemiBold }, new TextBlock { Text = detail, TextWrapping = TextWrapping.Wrap }, close } };
        ShowWorkspacePage(title, content);
        return Task.CompletedTask;
    }

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
