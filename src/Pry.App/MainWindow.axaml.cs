using System.Text.Json;
using System.Net;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using NAudio.Wave;
using Pry.Core.Abstractions;
using Pry.Core.Configuration;
using Pry.Core.Expression;
using Pry.Core.Inference;
using Pry.Core.Memory;
using Pry.Core.Models;
using Pry.Core.Prompting;
using Pry.Core.TurnTaking;
using Pry.Client;
using Pry.Contracts;
using Pry.App.Services;

namespace Pry.App;

public sealed partial class MainWindow : Window
{
    private readonly PryBackendClient _api;
    private readonly BackendProjectionService _projectionService;
    private readonly DialogService _dialogs;
    private readonly string _appDirectory = AppContext.BaseDirectory;
    private readonly string _dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PryCompanion");
    private string _conversationId = Guid.NewGuid().ToString("N");
    private readonly AttachmentDraftService _attachmentDraft = new();
    private readonly MessageThemeTracker _messageTheme = new();
    private readonly BackgroundImageCache _backgroundImages = new();
    private string? _currentBackgroundRenderKey;
    private readonly Border _chatBottomAnchor = new() { Height = 64, IsHitTestVisible = false };
    private readonly DispatcherTimer _chatScrollTimer = new() { Interval = TimeSpan.FromMilliseconds(24) };
    private bool _chatScrollPending;
    private int _chatScrollPassesRemaining;
    private double _lastChatMaskHeight = -1;
    private double _lastChatMaskHeaderHeight = -1;
    private double _lastChatMaskComposerHeight = -1;
    private bool _changingConversation;
    private readonly Dictionary<string, string> _conversationDrafts = new(StringComparer.Ordinal);
    private readonly HashSet<string> _collapsedConversationFolders = new(StringComparer.Ordinal);
    private double _expandedSidebarWidth = 300;
    private bool _sidebarCollapsed;
    private CancellationTokenSource? _listeningSignalCancellation;
    private bool _listeningSignalSent;
    private DateTimeOffset _lastListeningSignalAt = DateTimeOffset.MinValue;
    private bool _resizingMainSidebar;
    private double _mainSidebarResizeStartX;
    private double _mainSidebarResizeStartWidth;
    private double _pendingSidebarWidth;
    private bool _suppressInputActivity;
    private CharacterDefinition? _character;
    private readonly List<CharacterDefinition> _characters = [];
    private bool _changingCharacter;
    private UserPreferences _preferences = new();
    private ModelProfile[] _profiles = [];
    private ModelProfile[] _builtInProfiles = [];
    private SpeechModelProfile[] _speechProfiles = [];
    private SpeechModelProfile[] _builtInSpeechProfiles = [];
    private readonly List<StickerDefinition> _stickers = [];
    private RemoteConversationTurnManager? _turnManager;
    private WaveInEvent? _waveInput;
    private WaveFileWriter? _waveWriter;
    private TaskCompletionSource? _recordingStopped;
    private string? _recordingPath;
    private bool _speechBusy;
    private bool _sendBusy;
    private bool _allowClose;

    public MainWindow() : this(new PryBackendClient(new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5078/"), Timeout = Timeout.InfiniteTimeSpan })) { }

    public MainWindow(PryBackendClient api)
    {
        _api = api;
        _projectionService = new BackendProjectionService(api);
        InitializeComponent();
        _dialogs = new DialogService(content => CreateThemedDialogSurface(content), CreateCompactDialogTextCard);
        MessagesPanel.Children.Add(_chatBottomAnchor);
        MessagesPanel.LayoutUpdated += (_, _) =>
        {
            UpdateChatUnderlayMask();
            if (!_chatScrollPending) return;
            ForceChatScrollToEnd();
        };
        ChatScroll.LayoutUpdated += (_, _) => UpdateChatUnderlayMask();
        HeaderGlass.LayoutUpdated += (_, _) => UpdateChatUnderlayMask();
        ComposerGlass.LayoutUpdated += (_, _) => UpdateChatUnderlayMask();
        _chatScrollTimer.Tick += (_, _) =>
        {
            ForceChatScrollToEnd();
            if (--_chatScrollPassesRemaining > 0) return;
            _chatScrollPending = false;
            _chatScrollTimer.Stop();
        };
        InputBox.AddHandler(KeyDownEvent, InputBox_KeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(KeyDownEvent, Window_KeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        ThemeCanvas.AddHandler(PointerPressedEvent, WindowResizeGrip_PointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        Opened += async (_, _) => await InitializeRuntimeAsync();
        Closing += (_, e) => { if (!_allowClose) { e.Cancel = true; Hide(); } };
    }

    public string ActiveModelLink
    {
        get
        {
            var textId = GetActiveModelId(); var visionId = GetVisionModelId();
            var text = _profiles.FirstOrDefault(x => x.Id == textId); var vision = _profiles.FirstOrDefault(x => x.Id == visionId);
            if (text is null) return "模型尚未加载";
            return visionId == textId
                ? $"文字/图片共用：{text.DisplayName} · 单一实例 · {text.BaseUrl}"
                : $"文字：{text.DisplayName} · {text.BaseUrl}\n图片：{vision?.DisplayName ?? "未配置"} · {vision?.BaseUrl ?? "-"}";
        }
    }

    public void ShowFromTray() { Show(); WindowState = WindowState.Normal; Activate(); }

    public async Task PrepareForExitAsync(bool stopModelService)
    {
        _allowClose = true; EndUserComposition(); if (_turnManager is not null) await _turnManager.DisposeAsync();
        _waveInput?.StopRecording(); _waveWriter?.Dispose(); _waveInput?.Dispose();
        _backgroundImages.Dispose();
        _projectionService.Dispose();
    }

    private async Task InitializeRuntimeAsync()
    {
        try
        {
            if (!await _api.IsHealthyAsync()) throw new InvalidOperationException("本地后端未就绪。");
            await LoadBackendProjectionAsync();
            if (!string.IsNullOrWhiteSpace(_preferences.ActiveConversationId)) _conversationId = _preferences.ActiveConversationId;
            ApplyTheme();
            try { _ = await _api.GetConversationAsync(_conversationId); }
            catch (PryBackendException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var created = await _api.CreateConversationAsync(_character?.Id); _conversationId = created.Id;
                _preferences = _preferences with { ActiveConversationId = _conversationId };
                await _api.UpdatePreferencesAsync(new UpdateClientPreferencesRequest(null, _conversationId, null, null, null, null, null));
            }
            var activeId = GetActiveModelId();
            var visionId = GetVisionModelId();
            CreateTurnManager(); RefreshCharacterSelector(); CharacterName.Text = _character!.Name; await RefreshConversationRoomsAsync();
            var runtime = await _api.GetRuntimeAsync(); RuntimeStatus.Text = runtime.State == "ready" ? "本地后端已就绪" : $"后端状态：{runtime.State}";
            var existingMessages = await _api.GetMessagesAsync(_conversationId, 200);
            if (existingMessages.Count == 0) AddTextBubble(_character.Name, _character.Greeting, false); else RenderConversationMessages(existingMessages);
        }
        catch (Exception ex) { RuntimeStatus.Text = $"初始化失败：{ex.Message}"; AddTextBubble("系统", ex.Message, false); }
    }

    private async Task LoadBackendProjectionAsync()
    {
        var projection = await _projectionService.LoadAsync();
        _preferences = projection.Preferences;
        _profiles = projection.Models;
        _builtInProfiles = projection.BuiltInModels;
        _speechProfiles = projection.SpeechModels;
        _builtInSpeechProfiles = projection.BuiltInSpeechModels;
        _characters.Clear(); _characters.AddRange(projection.Characters);
        _stickers.Clear(); _stickers.AddRange(projection.Stickers);
        _character = _characters.FirstOrDefault(x => x.Id == _preferences.SelectedCharacterId) ?? _characters[0];
    }

    private async Task ReloadStickerProjectionAsync()
    {
        _stickers.Clear();
        _stickers.AddRange(await _projectionService.LoadStickersAsync());
    }

    private void RefreshCharacterSelector()
    {
        _changingCharacter = true;
        var items = _characters.Select(x => new CharacterChoice(x.Id, CardLabel(x))).ToArray();
        var selected = items.FirstOrDefault(x => x.Id == _character?.Id);
        CharacterSelectorList.ItemsSource = items; CharacterSelectorList.SelectedItem = selected;
        CharacterSelectorText.Text = selected?.Name ?? "请选择角色卡";
        _changingCharacter = false;
    }

    private void CharacterSelectorButton_Click(object? sender, RoutedEventArgs e)
    {
        CharacterSelectorPopup.IsOpen = !CharacterSelectorPopup.IsOpen;
    }

    private async void CharacterSelectorList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_changingCharacter || CharacterSelectorList.SelectedItem is not CharacterChoice choice) return;
        CharacterSelectorText.Text = choice.Name; CharacterSelectorPopup.IsOpen = false;
        var selected = _characters.FirstOrDefault(x => x.Id == choice.Id); if (selected is null || selected.Id == _character?.Id) return;
        _turnManager?.CancelAgentReply(); _character = selected; CharacterName.Text = selected.Name; ApplyTheme();
        var created = await _api.CreateConversationAsync(selected.Id); _conversationId = created.Id; ResetMessagesPanel(); CreateTurnManager(); AddTextBubble(selected.Name, selected.Greeting, false);
        _preferences = _preferences with { SelectedCharacterId = selected.Id, ActiveConversationId = _conversationId };
        await _api.UpdatePreferencesAsync(new UpdateClientPreferencesRequest(selected.Id, _conversationId, null, null, null, null, null));
        await RefreshConversationRoomsAsync();
    }

    private void CreateTurnManager()
    {
        if (_character is null) return;
        if (_turnManager is not null) _ = _turnManager.DisposeAsync();
        _turnManager = new RemoteConversationTurnManager(_api, _conversationId);
        _turnManager.AgentMessageDelivered += message => Dispatcher.UIThread.InvokeAsync(() => AddAgentMessage(message)).GetTask();
        _turnManager.StateChanged += state => Dispatcher.UIThread.Post(() => TurnStatus.Text = state switch { TurnState.UserPending => "等你把话说完…", TurnState.ModelThinking => "正在想…", TurnState.AgentPending => "正在输入…", TurnState.AgentSending => "正在发送…（Esc 可打断）", _ => "准备就绪" });
        _turnManager.Failed += ex => Dispatcher.UIThread.Post(() => { RuntimeStatus.Text = "模型连接失败"; AddTextBubble("系统", $"暂时无法回应：{ex.Message}", false); });
        _turnManager.Warning += warning => Dispatcher.UIThread.Post(() => RuntimeStatus.Text = warning);
    }

    private string GetActiveModelId(UserPreferences? preferences = null)
    {
        var value = preferences ?? _preferences;
        return value.ActiveModelId ?? value.ModelTuning.ActiveModelId ?? _profiles.FirstOrDefault()?.Id ?? "qwen3-1.7b-local";
    }

    private string? GetVisionModelId(UserPreferences? preferences = null) => (preferences ?? _preferences).ActiveVisionModelId;
    private string? GetSpeechModelId(UserPreferences? preferences = null) => (preferences ?? _preferences).ActiveSpeechModelId ?? _speechProfiles.FirstOrDefault()?.Id;

    private SpeechModelProfile? GetSpeechProfile()
    {
        var id = GetSpeechModelId();
        return _builtInSpeechProfiles.Concat(_preferences.CustomSpeechModels).FirstOrDefault(x => x.Id == id);
    }

    private ModelTuningPreferences? GetModelTuning(string id, UserPreferences? preferences = null)
    {
        var value = preferences ?? _preferences;
        if (value.ModelTunings.TryGetValue(id, out var tuning)) return tuning;
        return id == GetActiveModelId(value) ? value.ModelTuning with { EnableThinking = value.ModelTuning.EnableThinking ?? value.EnableThinking } : null;
    }

    private TurnTakingSettings EffectiveTurnSettings() => _preferences.TurnTakingOverride ?? new TurnTakingSettings();

    private IReadOnlyList<string> LoadTips()
    {
        try
        {
            var path = Path.Combine(_appDirectory, "Resources", "tips.json");
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.GetProperty("tips").EnumerateArray()
                .Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToArray();
        }
        catch { return []; }
    }

    private void BeginUserComposition(bool speech)
    {
        if (_suppressInputActivity) return;
        if (!speech && string.IsNullOrWhiteSpace(InputBox.Text)) { EndUserComposition(); return; }
        if (_listeningSignalSent || _listeningSignalCancellation is not null) return;
        TurnStatus.Text = speech ? "正在听你说话…" : "检测到你正在输入…";
        var settings = EffectiveTurnSettings();
        if (!settings.EnableListeningSignals || DateTimeOffset.UtcNow - _lastListeningSignalAt < TimeSpan.FromSeconds(30)) return;
        _listeningSignalCancellation = new CancellationTokenSource();
        var conversationId = _conversationId;
        _ = MaybeSendListeningSignalAsync(conversationId, settings.ListeningSignalDelayMs, _listeningSignalCancellation.Token);
    }

    private async Task MaybeSendListeningSignalAsync(string conversationId, int delayMs, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(Math.Clamp(delayMs, 1500, 15000), cancellationToken);
            if (conversationId != _conversationId || _turnManager?.State != TurnState.Idle) return;
            var stillComposing = _waveInput is not null || _speechBusy || !string.IsNullOrWhiteSpace(InputBox.Text);
            if (!stillComposing || _character is null) return;
            await _api.SendListeningSignalAsync(_conversationId, cancellationToken);
            _listeningSignalSent = true; _lastListeningSignalAt = DateTimeOffset.UtcNow;
            await RefreshConversationRoomsAsync();
        }
        catch (OperationCanceledException) { }
        finally { _listeningSignalCancellation?.Dispose(); _listeningSignalCancellation = null; }
    }

    private void EndUserComposition()
    {
        _listeningSignalCancellation?.Cancel(); _listeningSignalCancellation?.Dispose(); _listeningSignalCancellation = null;
        _listeningSignalSent = false;
        if (_turnManager?.State == TurnState.Idle && _waveInput is null && !_speechBusy) TurnStatus.Text = "准备就绪";
    }

    private async Task RefreshConversationRoomsAsync()
    {
        var rooms = await _api.GetConversationsAsync();
        var folders = await _api.GetFoldersAsync();
        _changingConversation = true;
        var items = ConversationListItemFactory.Create(rooms, folders, _collapsedConversationFolders,
            async key =>
            {
                if (!_collapsedConversationFolders.Add(key)) _collapsedConversationFolders.Remove(key);
                await RefreshConversationRoomsAsync();
            },
            CreateFolderContextMenu,
            () => ConversationContextMenuFactory.CreateUnfiled(async () =>
            {
                if (await CreateConversationFolderAsync() is not null) await RefreshConversationRoomsAsync();
            }),
            room => CreateConversationContextMenu(room, folders));
        ConversationList.ItemsSource = items;
        ConversationList.SelectedItem = items.FirstOrDefault(x => x.Tag is ConversationRoom room && room.Id == _conversationId);
        var activeRoom = rooms.FirstOrDefault(x => x.Id == _conversationId); if (activeRoom is not null) CharacterName.Text = activeRoom.Title;
        _changingConversation = false;
    }

    private async void ConversationList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_changingConversation || ConversationList.SelectedItem is not ListBoxItem item) return;
        if (item.Tag is ConversationFolderHeader) return;
        if (item.Tag is ConversationRoom room && room.Id != _conversationId) await OpenConversationRoomAsync(room);
    }

    private async Task OpenConversationRoomAsync(ConversationRoom room)
    {
        _conversationDrafts[_conversationId] = InputBox.Text ?? "";
        EndUserComposition(); _turnManager?.CancelAgentReply(); _conversationId = room.Id;
        _suppressInputActivity = true; InputBox.Text = _conversationDrafts.GetValueOrDefault(_conversationId, ""); _suppressInputActivity = false;
        if (!string.IsNullOrWhiteSpace(room.CharacterId))
        {
            var roomCharacter = _characters.FirstOrDefault(x => x.Id == room.CharacterId);
            if (roomCharacter is not null) { _character = roomCharacter; RefreshCharacterSelector(); ApplyTheme(); }
        }
        ResetMessagesPanel(); CreateTurnManager();
        var messages = await _api.GetMessagesAsync(_conversationId, 200);
        if (messages.Count == 0 && _character is not null) AddTextBubble(_character.Name, _character.Greeting, false); else RenderConversationMessages(messages);
        _preferences = _preferences with { ActiveConversationId = _conversationId, SelectedCharacterId = _character?.Id };
        await _api.UpdatePreferencesAsync(new UpdateClientPreferencesRequest(_character?.Id, _conversationId, null, null, null, null, null));
        await RefreshConversationRoomsAsync();
    }

    private ContextMenu CreateConversationContextMenu(ConversationRoom room, IReadOnlyList<ConversationFolder> folders)
        => ConversationContextMenuFactory.CreateRoom(room, folders,
            async () =>
            {
                await _api.UpdateConversationAsync(room.Id, new UpdateConversationRequest(null, !room.IsPinned, null));
                await RefreshConversationRoomsAsync();
            },
            async () =>
            {
                var value = await PromptTextAsync("重命名对话", "对话房间标题", room.Title);
                if (string.IsNullOrWhiteSpace(value)) return;
                await _api.UpdateConversationAsync(room.Id, new UpdateConversationRequest(value, null, null));
                await RefreshConversationRoomsAsync();
            },
            async folderId =>
            {
                await _api.UpdateConversationAsync(room.Id,
                    new UpdateConversationRequest(null, null, folderId, folderId is null));
                await RefreshConversationRoomsAsync();
            },
            async () =>
            {
                var folderId = await CreateConversationFolderAsync();
                if (folderId is null) return;
                await _api.UpdateConversationAsync(room.Id, new UpdateConversationRequest(null, null, folderId));
                await RefreshConversationRoomsAsync();
            },
            () => DeleteConversationAsync(room));

    private ContextMenu CreateFolderContextMenu(ConversationFolder folder) =>
        ConversationContextMenuFactory.CreateFolder(
            async () =>
            {
                var value = await PromptTextAsync("重命名文件夹", "文件夹名称", folder.Name);
                if (string.IsNullOrWhiteSpace(value)) return;
                await _api.RenameFolderAsync(folder.Id, value);
                await RefreshConversationRoomsAsync();
            },
            async () =>
            {
                if (!await ConfirmAsync(this, "删除文件夹", $"确定删除“{folder.Name}”吗？其中的对话会移回未分类，不会被删除。")) return;
                await _api.DeleteFolderAsync(folder.Id);
                _collapsedConversationFolders.Remove(folder.Id);
                await RefreshConversationRoomsAsync();
            });

    private async Task<string?> CreateConversationFolderAsync()
    {
        var name = await PromptTextAsync("新建文件夹", "文件夹名称", "新文件夹"); if (string.IsNullOrWhiteSpace(name)) return null;
        return (await _api.CreateFolderAsync(name)).Id;
    }

    private async void NewConversationFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (await CreateConversationFolderAsync() is not null) await RefreshConversationRoomsAsync();
    }

    private async void DeleteCurrentConversation_Click(object? sender, RoutedEventArgs e)
    {
        var room = (await _api.GetConversationsAsync()).FirstOrDefault(x => x.Id == _conversationId); if (room is not null) await DeleteConversationAsync(room);
    }

    private async Task DeleteConversationAsync(ConversationRoom room)
    {
        if (!await ConfirmAsync(this, "删除对话房间", $"确定删除“{room.Title}”及其中全部消息吗？")) return;
        _turnManager?.CancelAgentReply(); await _api.DeleteConversationAsync(room.Id); _conversationDrafts.Remove(room.Id);
        var remaining = await _api.GetConversationsAsync();
        if (room.Id == _conversationId)
        {
            if (remaining.Count > 0) await OpenConversationRoomAsync(remaining[0]); else NewConversation_Click(null, new RoutedEventArgs());
        }
        else await RefreshConversationRoomsAsync();
    }

    private Task<string?> PromptTextAsync(string title, string label, string initialValue) =>
        _dialogs.PromptTextAsync(this, title, label, initialValue);

    internal Border CreateCompactDialogTextCard(Control content)
    {
        var theme = _preferences.Theme ?? new ThemePreferences();
        var light = theme.ThemeMode.Equals("light", StringComparison.OrdinalIgnoreCase)
                    || (theme.ThemeMode.Equals("system", StringComparison.OrdinalIgnoreCase) && Application.Current?.ActualThemeVariant == ThemeVariant.Light);
        return new Border
        {
            Background = Brush.Parse(light ? "#D8F8FAFC" : "#D0182533"),
            BorderBrush = Brush.Parse(light ? "#BDCBD6" : "#34495E"), BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(13), Padding = new Thickness(18), Child = content
        };
    }

    internal static Grid CreateCompactDialogLayout(Control contentCard, Control actions)
    {
        var layout = new Grid
        {
            Margin = new Thickness(16), RowDefinitions = new RowDefinitions("*,Auto"), RowSpacing = 14,
            Children = { contentCard, actions }
        };
        Grid.SetRow(actions, 1);
        return layout;
    }

    private void ToggleSidebar_Click(object? sender, RoutedEventArgs e)
    {
        _sidebarCollapsed = !_sidebarCollapsed;
        if (_sidebarCollapsed)
        {
            _expandedSidebarWidth = Math.Max(220, MainLayout.ColumnDefinitions[0].ActualWidth);
            MainLayout.ColumnDefinitions[0].Width = new GridLength(0);
            MainLayout.ColumnSpacing = 0;
            MainSidebar.IsVisible = false;
            MainSidebarSplitter.IsVisible = false;
            SidebarResizeGuide.IsVisible = false;
            CollapsedSidebarControls.IsVisible = true;
        }
        else
        {
            MainLayout.ColumnDefinitions[0].Width = new GridLength(_expandedSidebarWidth);
            MainLayout.ColumnSpacing = 12;
            MainSidebar.IsVisible = true;
            MainSidebarSplitter.IsVisible = true;
            CollapsedSidebarControls.IsVisible = false;
        }
    }

    private void RenderConversationMessages(IEnumerable<ChatMessage> messages)
    {
        ResetMessagesPanel();
        foreach (var message in messages)
        {
            var isUser = message.Role == ChatRole.User;
            var author = isUser ? "你" : _character?.Name ?? "角色";
            if (!string.IsNullOrWhiteSpace(message.ImagePath) && File.Exists(message.ImagePath)) AddImageBubble(author, message.ImagePath, isUser, message.CreatedAt, message.Id);
            if (!string.IsNullOrWhiteSpace(message.StickerId)) AddStickerBubble(author, message.StickerId, isUser, message.CreatedAt, message.Id);
            if (!string.IsNullOrWhiteSpace(message.Content)) AddTextBubble(author, message.Content, isUser, message.CreatedAt, message.Id);
        }
        ScrollChatToEnd();
    }

    private void ApplyTheme()
    {
        var theme = _preferences.Theme ?? new ThemePreferences();
        var hasBackground = !string.IsNullOrWhiteSpace(theme.BackgroundImagePath) && File.Exists(theme.BackgroundImagePath);
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = theme.ThemeMode.ToLowerInvariant() switch
            {
                "light" => ThemeVariant.Light,
                "dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
            var accent = Color.TryParse(theme.AccentColor, out var parsedAccent)
                ? parsedAccent
                : Application.Current.PlatformSettings?.GetColorValues().AccentColor1 ?? Color.Parse("#B148C6");
            Application.Current.Resources["PryAccentBrush"] = new SolidColorBrush(accent);
            Application.Current.Resources["PryAccentTextBrush"] = AccentTextBrush(accent);
            Application.Current.Resources["PryButtonHoverBrush"] = new SolidColorBrush(Mix(accent, Colors.White, .18));
            Application.Current.Resources["PrySelectionBrush"] = new SolidColorBrush(Mix(accent, Colors.Transparent, .58));
            Application.Current.Resources["SystemAccentColor"] = accent;
            Application.Current.Resources["SystemControlHighlightAccentBrush"] = new SolidColorBrush(accent);
            Application.Current.Resources["SystemControlHighlightListAccentLowBrush"] = new SolidColorBrush(Mix(accent, Colors.Transparent, .58));
        }
        var light = theme.ThemeMode.Equals("light", StringComparison.OrdinalIgnoreCase)
                    || (theme.ThemeMode.Equals("system", StringComparison.OrdinalIgnoreCase) && Application.Current?.ActualThemeVariant == ThemeVariant.Light);
        var canvasColor = light ? "#EEF3F7" : "#0E1621";
        var panelColor = light ? "#E3EBF1" : "#17212B";
        var borderColor = light ? "#BDCBD6" : "#263746";
        var cardColor = hasBackground ? (light ? "#B8F8FAFC" : "#A8182533") : (light ? "#F8FAFC" : "#182533");
        var textColor = light ? "#17212B" : "#F2F6FA";
        var mutedColor = light ? "#526678" : "#8EA2B5";
        var buttonColor = hasBackground ? (light ? "#A8D8E2EA" : "#A8273442") : (light ? "#D8E2EA" : "#273442");
        var buttonHoverColor = hasBackground ? (light ? "#C5E6EDF2" : "#C5344556") : (light ? "#C9D7E1" : "#344556");
        var buttonPressedColor = hasBackground ? (light ? "#D4CBD8E2" : "#D440566B") : (light ? "#BACAD6" : "#40566B");
        if (Application.Current is not null)
        {
            Application.Current.Resources["PryCanvasBrush"] = Brush.Parse(canvasColor);
            Application.Current.Resources["PryPanelBrush"] = Brush.Parse(panelColor);
            Application.Current.Resources["PryCardBrush"] = Brush.Parse(cardColor);
            Application.Current.Resources["PryPopupBrush"] = Brush.Parse(light ? "#F8FAFC" : "#182533");
            Application.Current.Resources["PryBorderBrush"] = Brush.Parse(borderColor);
            Application.Current.Resources["PryTextBrush"] = Brush.Parse(textColor);
            Application.Current.Resources["PryMutedBrush"] = Brush.Parse(mutedColor);
            Application.Current.Resources["PryButtonBrush"] = Brush.Parse(buttonColor);
            Application.Current.Resources["PryButtonHoverBrush"] = Brush.Parse(buttonHoverColor);
            Application.Current.Resources["PryButtonPressedBrush"] = Brush.Parse(buttonPressedColor);
        }
        TransparencyLevelHint = theme.BackgroundBlurRadius > .5
            ? [WindowTransparencyLevel.Blur, WindowTransparencyLevel.Transparent]
            : [WindowTransparencyLevel.Transparent];
        Background = Brushes.Transparent; Foreground = Brush.Parse(textColor);
        ThemeCanvas.Background = hasBackground ? Brushes.Transparent : Brush.Parse(canvasColor); ChatSurface.Background = Brushes.Transparent;
        SidebarGlassTint.Background = Brush.Parse(panelColor); HeaderGlassTint.Background = Brush.Parse(panelColor); ComposerGlassTint.Background = Brush.Parse(panelColor);
        ConversationList.Background = Brushes.Transparent; MainSidebarSplitter.Background = Brushes.Transparent;
        ChatBackgroundImage.IsVisible = hasBackground;
        ChatBackgroundImage.Opacity = Math.Clamp(theme.BackgroundImageOpacity, 0, 1);
        ChatBackgroundDim.IsVisible = hasBackground;
        ChatBackgroundDim.Opacity = Math.Clamp(theme.BackgroundDimOpacity, 0, .85);
        ChatBackgroundDim.Background = Brush.Parse(canvasColor);
        if (hasBackground)
        {
            try
            {
                var blurRadius = Math.Clamp(theme.BackgroundBlurRadius, 0, 32);
                var renderKey = _backgroundImages.GetKey(theme.BackgroundImagePath!, blurRadius);
                if (!string.Equals(_currentBackgroundRenderKey, renderKey, StringComparison.Ordinal))
                {
                    ChatBackgroundImage.Source = _backgroundImages.Get(theme.BackgroundImagePath!, blurRadius);
                    _currentBackgroundRenderKey = renderKey;
                }
                ChatBackgroundImage.Effect = null;
                ChatBackgroundImage.Margin = new Thickness(0);
                var display = theme.BackgroundDisplays.TryGetValue(theme.BackgroundImagePath!, out var configured) ? configured : new ImageDisplayPreferences();
                ApplyImageDisplay(ChatBackgroundImage, display);
            }
            catch { ChatBackgroundImage.IsVisible = false; ChatBackgroundDim.IsVisible = false; }
        }
        else { ChatBackgroundImage.Source = null; ChatBackgroundImage.Effect = null; ChatBackgroundImage.Margin = new Thickness(0); _currentBackgroundRenderKey = null; }

        var glassEnabled = theme.UseGlassEffects && hasBackground;
        if (glassEnabled)
        {
            var glassTint = light ? "#5CE3EBF1" : "#5C17212B";
            SidebarGlassTint.Background = Brush.Parse(glassTint); HeaderGlassTint.Background = Brush.Parse(glassTint); ComposerGlassTint.Background = Brush.Parse(glassTint);
            MainWindowChrome.Background = Brush.Parse(glassTint);
        }
        else
        {
            SidebarGlassTint.Background = Brush.Parse(panelColor); HeaderGlassTint.Background = Brush.Parse(panelColor); ComposerGlassTint.Background = Brush.Parse(panelColor);
            MainWindowChrome.Background = Brush.Parse(panelColor);
        }

        var avatarPath = _character?.AvatarPath ?? theme.CharacterAvatarPath;
        HeaderAvatarImage.Source = null; HeaderAvatarImage.IsVisible = false; HeaderAvatarFallback.IsVisible = true;
        if (!string.IsNullOrWhiteSpace(avatarPath) && File.Exists(avatarPath))
        {
            try { HeaderAvatarImage.Source = new Bitmap(avatarPath); HeaderAvatarImage.IsVisible = true; HeaderAvatarFallback.IsVisible = false; } catch { }
        }
        HeaderAvatarFallback.Text = _character?.Name.FirstOrDefault().ToString() ?? "P";
        if (HeaderAvatarImage.IsVisible) ApplyImageDisplay(HeaderAvatarImage, _character?.AvatarDisplay ?? new ImageDisplayPreferences());
        var userProfile = _preferences.UserProfile ?? new UserProfilePreferences();
        SidebarUserName.Text = string.IsNullOrWhiteSpace(userProfile.DisplayName) ? "你" : userProfile.DisplayName;
        SidebarUserSignature.Text = string.IsNullOrWhiteSpace(userProfile.Signature) ? "愿今天也有好心情" : userProfile.Signature;
        SidebarAvatarFallback.Text = SidebarUserName.Text.FirstOrDefault().ToString();
        SidebarAvatarImage.Source = null; SidebarAvatarImage.IsVisible = false; SidebarAvatarFallback.IsVisible = true;
        if (!string.IsNullOrWhiteSpace(theme.UserAvatarPath) && File.Exists(theme.UserAvatarPath))
        {
            try
            {
                // 头像始终从素材库原文件解码；不生成或加载降采样副本。
                SidebarAvatarImage.Source = new Bitmap(theme.UserAvatarPath); SidebarAvatarImage.IsVisible = true; SidebarAvatarFallback.IsVisible = false;
                var display = theme.UserAvatarDisplays.TryGetValue(theme.UserAvatarPath, out var savedUserDisplay) ? savedUserDisplay : new ImageDisplayPreferences();
                ApplyImageDisplay(SidebarAvatarImage, display);
            }
            catch { }
        }
        ApplyUiSizing(theme);
    }

    private static Color Mix(Color from, Color to, double amount)
    {
        byte Blend(byte a, byte b) => (byte)Math.Clamp(Math.Round(a + (b - a) * amount), 0, 255);
        return Color.FromArgb(Blend(from.A, to.A), Blend(from.R, to.R), Blend(from.G, to.G), Blend(from.B, to.B));
    }

    private static IBrush AccentTextBrush(Color accent)
    {
        var luminance = (.2126 * accent.R + .7152 * accent.G + .0722 * accent.B) / 255d;
        return Brush.Parse(luminance > .58 ? "#101820" : "#FFFFFF");
    }

    private IBrush AccentTextBrush() => Color.TryParse(_preferences.Theme?.AccentColor, out var accent) ? AccentTextBrush(accent) : Brushes.White;

    private static void ApplyImageDisplay(Image image, ImageDisplayPreferences display)
    {
        image.Stretch = Stretch.UniformToFill;
        RenderOptions.SetBitmapInterpolationMode(image, BitmapInterpolationMode.HighQuality);
        image.RenderTransformOrigin = new RelativePoint(Math.Clamp(display.FocusX, 0, 1), Math.Clamp(display.FocusY, 0, 1), RelativeUnit.Relative);
        image.RenderTransform = new ScaleTransform(Math.Clamp(display.Zoom, 1, 3), Math.Clamp(display.Zoom, 1, 3));
    }

    private static Border CreateVisualCropEditor(Image image, NumericUpDown focusX, NumericUpDown focusY, NumericUpDown zoom, double width, double height, bool circular)
    {
        image.Width = width; image.Height = height; image.Stretch = Stretch.UniformToFill;
        var viewport = new Border
        {
            Width = width, Height = height, CornerRadius = circular ? new CornerRadius(width / 2) : new CornerRadius(12),
            ClipToBounds = true, BorderBrush = Brush.Parse("#6B7F92"), BorderThickness = new Thickness(1),
            Background = Brush.Parse("#121C26"), Cursor = new Cursor(StandardCursorType.SizeAll), Child = image
        };
        var hint = new Border
        {
            Background = Brush.Parse("#AA0E1621"), CornerRadius = new CornerRadius(8), Padding = new Thickness(9, 5),
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(8),
            IsHitTestVisible = false,
            Child = new TextBlock { Text = "拖动取景 · 滚轮缩放", Foreground = Brushes.White, FontSize = 11 }
        };
        var grid = new Grid { Width = width, Height = height, Children = { viewport, hint } };
        var dragging = false; Point dragOrigin = default; decimal originX = .5m; decimal originY = .5m;
        viewport.PointerPressed += (_, args) =>
        {
            if (!args.GetCurrentPoint(viewport).Properties.IsLeftButtonPressed) return;
            dragging = true; dragOrigin = args.GetPosition(viewport); originX = focusX.Value ?? .5m; originY = focusY.Value ?? .5m;
            args.Pointer.Capture(viewport); args.Handled = true;
        };
        viewport.PointerMoved += (_, args) =>
        {
            if (!dragging) return;
            var position = args.GetPosition(viewport); var currentZoom = Math.Max(1, (double)(zoom.Value ?? 1));
            focusX.Value = (decimal)Math.Clamp((double)originX - (position.X - dragOrigin.X) / Math.Max(1, viewport.Bounds.Width) / currentZoom, 0, 1);
            focusY.Value = (decimal)Math.Clamp((double)originY - (position.Y - dragOrigin.Y) / Math.Max(1, viewport.Bounds.Height) / currentZoom, 0, 1);
            args.Handled = true;
        };
        viewport.PointerReleased += (_, args) =>
        {
            if (!dragging) return; dragging = false; args.Pointer.Capture(null); args.Handled = true;
        };
        viewport.PointerWheelChanged += (_, args) =>
        {
            zoom.Value = (decimal)Math.Clamp((double)(zoom.Value ?? 1) + args.Delta.Y * .08, 1, 3); args.Handled = true;
        };
        return new Border { HorizontalAlignment = HorizontalAlignment.Left, Child = grid };
    }

    internal Control CreateThemedDialogSurface(Control content, bool transparentContent = false, Action<Image?, Border?>? captureBackground = null)
    {
        var theme = _preferences.Theme ?? new ThemePreferences();
        var light = theme.ThemeMode.Equals("light", StringComparison.OrdinalIgnoreCase)
                    || (theme.ThemeMode.Equals("system", StringComparison.OrdinalIgnoreCase) && Application.Current?.ActualThemeVariant == ThemeVariant.Light);
        var hasBackground = !string.IsNullOrWhiteSpace(theme.BackgroundImagePath) && File.Exists(theme.BackgroundImagePath);
        var root = new Grid { Background = hasBackground ? Brushes.Transparent : Brush.Parse(light ? "#EEF3F7" : "#0E1621"), RowDefinitions = new RowDefinitions("Auto,*") };
        var dialogBackground = new Image { Stretch = Stretch.UniformToFill, IsVisible = false };
        var dialogBackgroundDim = new Border { Background = Brush.Parse(light ? "#EEF3F7" : "#0E1621"), IsHitTestVisible = false, IsVisible = false };
        Grid.SetRowSpan(dialogBackground, 2); Grid.SetRowSpan(dialogBackgroundDim, 2);
        root.Children.Add(dialogBackground); root.Children.Add(dialogBackgroundDim);
        if (hasBackground)
        {
            try
            {
                var backgroundPath = theme.BackgroundImagePath!;
                dialogBackground.Source = _backgroundImages.Get(backgroundPath, Math.Clamp(theme.BackgroundBlurRadius, 0, 32));
                dialogBackground.Opacity = Math.Clamp(theme.BackgroundImageOpacity, 0, 1);
                dialogBackground.IsVisible = true;
                var display = theme.BackgroundDisplays.TryGetValue(backgroundPath, out var saved) ? saved : new ImageDisplayPreferences();
                ApplyImageDisplay(dialogBackground, display);
                dialogBackgroundDim.Opacity = Math.Clamp(theme.BackgroundDimOpacity, 0, .85);
                dialogBackgroundDim.IsVisible = true;
            }
            catch { }
        }
        var glassEnabled = hasBackground && theme.UseGlassEffects;
        var tint = glassEnabled ? Brush.Parse(light ? "#5CE3EBF1" : "#5C17212B") : Brush.Parse(light ? "#E3EBF1" : "#17212B");
        var chromeTitle = new TextBlock { Text = "Pry", FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center };
        var minimize = new Button { Content = "—", Width = 30, Height = 26, MinWidth = 0, Padding = new Thickness(0) };
        var maximize = new Button { Content = "□", Width = 30, Height = 26, MinWidth = 0, Padding = new Thickness(0) };
        var close = new Button { Content = "×", Width = 30, Height = 26, MinWidth = 0, Padding = new Thickness(0) };
        var chrome = new Border
        {
            Height = 36, Margin = new Thickness(10, 8, 10, 0), CornerRadius = new CornerRadius(13), Background = tint,
            BorderBrush = Brush.Parse(light ? "#BDCBD6" : "#263746"), BorderThickness = new Thickness(1),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(12, 0, 5, 0),
                Children = { chromeTitle, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, VerticalAlignment = VerticalAlignment.Center, Children = { minimize, maximize, close } } }
            }
        };
        Grid.SetColumn(((Grid)chrome.Child).Children[1], 1);
        chrome.PointerPressed += (_, args) =>
        {
            if (!args.GetCurrentPoint(chrome).Properties.IsLeftButtonPressed || args.GetPosition(chrome).X >= chrome.Bounds.Width - 104) return;
            if (TopLevel.GetTopLevel(root) is not Window dialog) return;
            if (args.ClickCount == 2) dialog.WindowState = dialog.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            else dialog.BeginMoveDrag(args);
            args.Handled = true;
        };
        minimize.Click += (_, _) => { if (TopLevel.GetTopLevel(root) is Window dialog) dialog.WindowState = WindowState.Minimized; };
        maximize.Click += (_, _) => { if (TopLevel.GetTopLevel(root) is Window dialog) dialog.WindowState = dialog.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; };
        close.Click += (_, _) => { if (TopLevel.GetTopLevel(root) is Window dialog) dialog.Close(); };
        var shell = new Border
        {
            Margin = new Thickness(10, 8, 10, 10), CornerRadius = new CornerRadius(16),
            Background = transparentContent ? Brushes.Transparent : tint,
            BorderBrush = Brush.Parse(light ? "#BDCBD6" : "#263746"),
            BorderThickness = transparentContent ? new Thickness(0) : new Thickness(1),
            ClipToBounds = true, Child = content
        };
        Grid.SetRow(shell, 1);
        var resizeGrip = new Border
        {
            Width = 26, Height = 26, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom,
            Background = Brushes.Transparent, Cursor = new Cursor(StandardCursorType.BottomRightCorner),
            ZIndex = 100,
            Child = new TextBlock
            {
                Text = "◢", FontSize = 17, Foreground = ThemeAccentBrush(),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            }
        };
        ToolTip.SetTip(resizeGrip, "拖动改变窗口大小");
        Grid.SetRow(resizeGrip, 1);
        resizeGrip.PointerPressed += (_, args) =>
        {
            if (TopLevel.GetTopLevel(root) is Window dialog) BeginSouthEastResize(dialog, resizeGrip, args);
        };
        root.Children.Add(chrome); root.Children.Add(shell); root.Children.Add(resizeGrip);
        captureBackground?.Invoke(dialogBackground, dialogBackgroundDim);
        root.AttachedToVisualTree += (_, _) =>
        {
            if (TopLevel.GetTopLevel(root) is not Window dialog) return;
            dialog.SystemDecorations = SystemDecorations.None; dialog.ExtendClientAreaToDecorationsHint = true;
            dialog.ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome; dialog.Background = Brushes.Transparent;
            dialog.TransparencyLevelHint = theme.BackgroundBlurRadius > .5
                ? [WindowTransparencyLevel.Blur, WindowTransparencyLevel.Transparent]
                : [WindowTransparencyLevel.Transparent];
            chromeTitle.Text = dialog.Title ?? "Pry";
        };
        root.AddHandler(PointerPressedEvent, (_, args) =>
        {
            if (TopLevel.GetTopLevel(root) is Window dialog) TryBeginWindowResize(dialog, root, args);
        }, RoutingStrategies.Tunnel, handledEventsToo: true);
        return root;
    }

    private void ApplyUiSizing(ThemePreferences theme)
    {
        var avatar = Math.Clamp(theme.AvatarSize, 28, 76);
        var header = avatar * 1.25; HeaderAvatarBorder.Width = header; HeaderAvatarBorder.Height = header; HeaderAvatarBorder.CornerRadius = new CornerRadius(header / 2);
        var sidebar = avatar * 1.15; SidebarAvatar.Width = sidebar; SidebarAvatar.Height = sidebar; SidebarAvatar.CornerRadius = new CornerRadius(sidebar / 2);
        _messageTheme.Apply(theme, Brush.Parse(lightTheme() ? "#91A4B5" : "#536C82"),
            ThemeAccentBrush(), AssistantBubbleBrush(), AccentTextBrush(),
            Brush.Parse(lightTheme() ? "#17212B" : "#F2F6FA"));
        MessagesPanel.Spacing = Math.Clamp(theme.BubbleSpacing, 2, 36);
        bool lightTheme() => theme.ThemeMode.Equals("light", StringComparison.OrdinalIgnoreCase) || (theme.ThemeMode.Equals("system", StringComparison.OrdinalIgnoreCase) && Application.Current?.ActualThemeVariant == ThemeVariant.Light);
    }

    private async void SendButton_Click(object? sender, RoutedEventArgs e) => await SendAsync(false);
    private async void InputBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.V && e.KeyModifiers == KeyModifiers.Control)
        {
            e.Handled = true;
            if (!await TryPasteAttachmentsAsync()) await PasteClipboardTextAsync();
            return;
        }
        if (ShortcutGestureMatcher.Matches(e.Key, e.KeyModifiers, _preferences.Shortcuts.NewLine)) return;
        if (ShortcutGestureMatcher.Matches(e.Key, e.KeyModifiers, _preferences.Shortcuts.SendImmediately)) { e.Handled = true; await SendAsync(true); }
        else if (ShortcutGestureMatcher.Matches(e.Key, e.KeyModifiers, _preferences.Shortcuts.Send)) { e.Handled = true; await SendAsync(false); }
    }
    private async void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled) return;
        if (e.Key == Key.Z && e.KeyModifiers == KeyModifiers.Control) { e.Handled = true; await UndoConversationMutationAsync(); }
        else if (ShortcutGestureMatcher.Matches(e.Key, e.KeyModifiers, _preferences.Shortcuts.CancelReply)) { _turnManager?.CancelAgentReply(); TurnStatus.Text = "已打断"; e.Handled = true; }
        else if (ShortcutGestureMatcher.Matches(e.Key, e.KeyModifiers, _preferences.Shortcuts.NewConversation)) { NewConversation_Click(sender, e); e.Handled = true; }
        else if (ShortcutGestureMatcher.Matches(e.Key, e.KeyModifiers, _preferences.Shortcuts.OpenStickers)) { ToggleStickerPopup(); e.Handled = true; }
        else if (ShortcutGestureMatcher.Matches(e.Key, e.KeyModifiers, _preferences.Shortcuts.OpenCharacterEditor)) { await OpenCharacterEditorAsync(); e.Handled = true; }
    }
    private void InputBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressInputActivity) return;
        _turnManager?.NotifyInputActivity();
        if (string.IsNullOrWhiteSpace(InputBox.Text)) EndUserComposition(); else BeginUserComposition(false);
    }

    private async Task SendAsync(bool immediate, string? stickerId = null)
    {
        var text = InputBox.Text?.Trim() ?? "";
        if (_sendBusy || _turnManager is null || (text.Length == 0 && stickerId is null && _attachmentDraft.Items.Count == 0)) return;
        _sendBusy = true;
        SendButton.IsEnabled = false;
        EndUserComposition();
        var sentAttachments = _attachmentDraft.Items.ToArray();
        try
        {
            var messageId = await _turnManager.SubmitUserInputAsync(new UserInputPart(text, stickerId, sentAttachments), immediate);
            if (string.Equals(InputBox.Text?.Trim(), text, StringComparison.Ordinal))
            {
                _suppressInputActivity = true;
                InputBox.Text = "";
                _suppressInputActivity = false;
            }
            _attachmentDraft.RemoveRange(sentAttachments);
            RefreshAttachmentTray();
            _conversationDrafts.Remove(_conversationId);
            if (text.Length > 0) AddTextBubble("你", text, true, messageId: messageId);
            if (stickerId is not null) AddStickerBubble("你", stickerId, true, messageId: messageId);
            foreach (var attachment in sentAttachments) AddAttachmentBubble("你", attachment, true, messageId);
            await RefreshConversationRoomsAsync();
        }
        catch (Exception ex)
        {
            TurnStatus.Text = "发送失败 · 输入内容已保留";
            if (!string.IsNullOrWhiteSpace(InputBox.Text) || _attachmentDraft.Items.Count > 0) BeginUserComposition(false);
            await ShowNoticeAsync("消息发送失败", $"没有发送成功，你的文字和附件仍保留在输入区。\n\n{ex.Message}");
        }
        finally
        {
            _sendBusy = false;
            SendButton.IsEnabled = true;
        }
    }

    private void AddAgentMessage(PlannedReplyMessage message)
    {
        if (message.Type == ReplyMessageType.Sticker && message.StickerId is not null) AddStickerBubble(_character?.Name ?? "角色", message.StickerId, false, messageId: message.Id);
        else if (!string.IsNullOrWhiteSpace(message.Content)) AddTextBubble(_character?.Name ?? "角色", message.Content, false, messageId: message.Id);
        RuntimeStatus.Text = "就绪 · 对话已保存到本地";
        _ = RefreshConversationRoomsAsync();
    }
    private void AddTextBubble(string author, string content, bool isUser, DateTimeOffset? timestamp = null, long? messageId = null)
    {
        var theme = _preferences.Theme ?? new ThemePreferences();
        var view = MessageContentFactory.CreateText(content, isUser, theme, ThemeAccentBrush(), AssistantBubbleBrush(), AccentTextBrush(), ThemeTextBrush());
        TrackMessageContent(view, isUser);
        AddBubble(author, view.Content, isUser, timestamp, messageId, content);
    }
    private void AddStickerBubble(string author, string stickerId, bool isUser, DateTimeOffset? timestamp = null, long? messageId = null)
    {
        var sticker = _stickers.FirstOrDefault(x => x.Id == stickerId && x.Enabled); if (sticker is null) { AddTextBubble(author, "[表情]", isUser); return; }
        try { AddBubble(author, MessageContentFactory.CreateSticker(sticker.FilePath).Content, isUser, timestamp, messageId, ""); }
        catch { AddTextBubble(author, $"[表情：{sticker.Name}]", isUser); }
    }
    private void AddImageBubble(string author, string imagePath, bool isUser, DateTimeOffset? timestamp = null, long? messageId = null)
    {
        try
        {
            var view = MessageContentFactory.CreateImage(imagePath, isUser ? ThemeAccentBrush() : AssistantBubbleBrush());
            TrackMessageContent(view, isUser);
            AddBubble(author, view.Content, isUser, timestamp, messageId, "");
        }
        catch { AddTextBubble(author, $"[图片无法显示：{Path.GetFileName(imagePath)}]", isUser); }
    }
    private void AddAttachmentBubble(string author, ChatAttachment attachment, bool isUser, long? messageId = null)
    {
        if (attachment.IsImage) { AddImageBubble(author, attachment.Path, isUser, messageId: messageId); return; }
        var view = MessageContentFactory.CreateDocument(attachment, isUser ? ThemeAccentBrush() : AssistantBubbleBrush());
        TrackMessageContent(view, isUser);
        AddBubble(author, view.Content, isUser, messageId: messageId, messageText: "");
    }

    private void TrackMessageContent(MessageContentView view, bool isUser)
        => _messageTheme.Track(view, isUser);
    private void AddBubble(string author, Control content, bool isUser, DateTimeOffset? timestamp = null, long? messageId = null, string messageText = "")
    {
        var menu = messageId is long storedId ? CreateMessageContextMenu(storedId, isUser, messageText) : null;
        var view = MessageRowFactory.Create(author, content, isUser, timestamp ?? DateTimeOffset.Now,
            menu, _preferences.Theme ?? new ThemePreferences(), _character, ThemeAccentBrush());
        _messageTheme.TrackAvatar(view.Avatar, isUser);
        MessagesPanel.Children.Insert(Math.Max(0, MessagesPanel.Children.Count - 1), view.Row);
        ScrollChatToEnd();
    }

    private ContextMenu CreateMessageContextMenu(long messageId, bool isUser, string messageText)
        => MessageContextMenuFactory.Create(isUser,
            () => isUser
                ? EditFromUserMessageAsync(messageId, messageText)
                : RegenerateFromAssistantMessageAsync(messageId),
            () => DeleteMessageFromMenuAsync(messageId, isUser));

    private async Task DeleteMessageFromMenuAsync(long messageId, bool isUser)
    {
        _turnManager?.CancelAgentReply();
        await _api.DeleteMessageAsync(_conversationId, messageId);
        await ReloadActiveConversationAsync();
        TurnStatus.Text = isUser ? "已删除此消息及之后内容 · Ctrl+Z 撤销" : "已删除此条回复 · Ctrl+Z 撤销";
    }

    private async Task EditFromUserMessageAsync(long messageId, string messageText)
    {
        _turnManager?.CancelAgentReply();
        await _api.DeleteMessageAsync(_conversationId, messageId);
        await ReloadActiveConversationAsync();
        _suppressInputActivity = true; InputBox.Text = messageText; InputBox.CaretIndex = InputBox.Text?.Length ?? 0; _suppressInputActivity = false;
        InputBox.Focus(); BeginUserComposition(false); TurnStatus.Text = "正在从所选消息处编辑 · Ctrl+Z 撤销";
    }

    private async Task RegenerateFromAssistantMessageAsync(long messageId)
    {
        if (_turnManager is null) return;
        _turnManager.CancelAgentReply();
        await _turnManager.RegenerateAsync(messageId);
        await ReloadActiveConversationAsync();
        TurnStatus.Text = "正在重新回复… · Ctrl+Z 可恢复原分支";
    }

    private async Task UndoConversationMutationAsync()
    {
        try
        {
            _turnManager?.CancelAgentReply(); await _api.UndoAsync(_conversationId);
            await ReloadActiveConversationAsync(); TurnStatus.Text = "已撤销上一条消息操作";
        }
        catch (PryBackendException ex) when (ex.Code == "validation_error")
        {
            TurnStatus.Text = "没有可撤销的消息操作";
        }
    }

    private async Task ReloadActiveConversationAsync()
    {
        var messages = await _api.GetMessagesAsync(_conversationId, 200);
        if (messages.Count == 0) { ResetMessagesPanel(); if (_character is not null) AddTextBubble(_character.Name, _character.Greeting, false); }
        else RenderConversationMessages(messages);
        CreateTurnManager(); await RefreshConversationRoomsAsync();
    }

    private void ScrollChatToEnd()
    {
        _chatScrollPending = true;
        _chatScrollPassesRemaining = 5;
        _chatScrollTimer.Stop();
        _chatScrollTimer.Start();
        Dispatcher.UIThread.Post(ForceChatScrollToEnd, DispatcherPriority.Background);
    }

    private void ForceChatScrollToEnd()
    {
        var bottom = Math.Max(0, ChatScroll.Extent.Height - ChatScroll.Viewport.Height);
        ChatScroll.Offset = new Vector(ChatScroll.Offset.X, bottom);
    }

    private void UpdateChatUnderlayMask()
    {
        var height = ChatScroll.Bounds.Height;
        if (height <= 1) return;
        var headerHeight = HeaderGlass.Bounds.Height;
        var composerHeight = ComposerGlass.Bounds.Height;
        if (Math.Abs(height - _lastChatMaskHeight) < .5 &&
            Math.Abs(headerHeight - _lastChatMaskHeaderHeight) < .5 &&
            Math.Abs(composerHeight - _lastChatMaskComposerHeight) < .5)
        {
            return;
        }

        _lastChatMaskHeight = height;
        _lastChatMaskHeaderHeight = headerHeight;
        _lastChatMaskComposerHeight = composerHeight;
        var feather = Math.Clamp(12 / height, .008, .04);
        var top = Math.Clamp(headerHeight / height, 0, .42);
        var bottom = Math.Clamp(1 - composerHeight / height, .58, 1);
        ChatScroll.OpacityMask = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Colors.Transparent, 0),
                new GradientStop(Colors.Transparent, top),
                new GradientStop(Colors.White, Math.Min(.49, top + feather)),
                new GradientStop(Colors.White, Math.Max(.51, bottom - feather)),
                new GradientStop(Colors.Transparent, bottom),
                new GradientStop(Colors.Transparent, 1)
            }
        };
    }

    private void ResetMessagesPanel()
    {
        MessagesPanel.Children.Clear(); _messageTheme.Clear();
        MessagesPanel.Children.Add(_chatBottomAnchor);
    }

    private void MainSidebarSplitter_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _resizingMainSidebar = true; _mainSidebarResizeStartX = e.GetPosition(MainLayout).X;
        _mainSidebarResizeStartWidth = MainLayout.ColumnDefinitions[0].ActualWidth;
        _pendingSidebarWidth = _mainSidebarResizeStartWidth;
        var liveResize = (_preferences.Theme ?? new ThemePreferences()).LiveSidebarResize;
        SidebarResizeGuide.RenderTransform = new TranslateTransform(_pendingSidebarWidth, 0);
        SidebarResizeGuide.IsVisible = !liveResize;
        e.Pointer.Capture(MainSidebarSplitter); e.Handled = true;
    }

    private void MainSidebarSplitter_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_resizingMainSidebar) return;
        _pendingSidebarWidth = Math.Clamp(_mainSidebarResizeStartWidth + e.GetPosition(MainLayout).X - _mainSidebarResizeStartX, 220, 420);
        if ((_preferences.Theme ?? new ThemePreferences()).LiveSidebarResize)
        {
            MainLayout.ColumnDefinitions[0].Width = new GridLength(_pendingSidebarWidth);
            _expandedSidebarWidth = _pendingSidebarWidth;
        }
        else SidebarResizeGuide.RenderTransform = new TranslateTransform(_pendingSidebarWidth, 0);
        e.Handled = true;
    }

    private void MainSidebarSplitter_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_resizingMainSidebar) return;
        _resizingMainSidebar = false; SidebarResizeGuide.IsVisible = false;
        if (!(_preferences.Theme ?? new ThemePreferences()).LiveSidebarResize)
            MainLayout.ColumnDefinitions[0].Width = new GridLength(_pendingSidebarWidth);
        _expandedSidebarWidth = _pendingSidebarWidth; e.Pointer.Capture(null); e.Handled = true;
    }

    private void WindowChrome_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(MainWindowChrome).Properties.IsLeftButtonPressed) return;
        if (e.ClickCount == 2) ToggleMainWindowMaximized(); else BeginMoveDrag(e);
        e.Handled = true;
    }

    private void MainMinimizeButton_Click(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MainMaximizeButton_Click(object? sender, RoutedEventArgs e) => ToggleMainWindowMaximized();
    private void MainCloseButton_Click(object? sender, RoutedEventArgs e) => Close();
    private void ToggleMainWindowMaximized() => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void WindowResizeGrip_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control surface) TryBeginWindowResize(this, surface, e);
    }

    private void SouthEastResizeGrip_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control grip) BeginSouthEastResize(this, grip, e);
    }

    private static void BeginSouthEastResize(Window window, Control grip, PointerPressedEventArgs e)
    {
        if (window.WindowState == WindowState.Maximized || !e.GetCurrentPoint(grip).Properties.IsLeftButtonPressed) return;
        window.BeginResizeDrag(WindowEdge.SouthEast, e);
        e.Handled = true;
    }

    private static bool TryBeginWindowResize(Window window, Control surface, PointerPressedEventArgs e)
    {
        if (window.WindowState == WindowState.Maximized || !e.GetCurrentPoint(surface).Properties.IsLeftButtonPressed) return false;
        var position = e.GetPosition(surface);
        const double grip = 7;
        var left = position.X <= grip;
        var right = position.X >= surface.Bounds.Width - grip;
        var top = position.Y <= grip;
        var bottom = position.Y >= surface.Bounds.Height - grip;
        WindowEdge? edge = (left, right, top, bottom) switch
        {
            (true, _, true, _) => WindowEdge.NorthWest,
            (_, true, true, _) => WindowEdge.NorthEast,
            (true, _, _, true) => WindowEdge.SouthWest,
            (_, true, _, true) => WindowEdge.SouthEast,
            (_, _, true, _) => WindowEdge.North,
            (_, _, _, true) => WindowEdge.South,
            (true, _, _, _) => WindowEdge.West,
            (_, true, _, _) => WindowEdge.East,
            _ => null
        };
        if (edge is null) return false;
        window.BeginResizeDrag(edge.Value, e);
        e.Handled = true;
        return true;
    }

    private async void ImageButton_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择图片或文件", AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("支持的附件") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.gif", "*.txt", "*.csv", "*.scv", "*.docx"] },
                new FilePickerFileType("图片") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.gif"] },
                new FilePickerFileType("文档") { Patterns = ["*.txt", "*.csv", "*.scv", "*.docx"] }
            ]
        });
        if (files.Count == 0) return;
        await AddAttachmentPathsAsync(files.Select(x => x.TryGetLocalPath()).Where(x => x is not null)!);
    }

    private async Task AddAttachmentPathsAsync(IEnumerable<string> paths)
    {
        var result = _attachmentDraft.AddPaths(paths);
        RefreshAttachmentTray();
        if (_attachmentDraft.Items.Count > 0) _turnManager?.NotifyInputActivity();
        if (result.Rejected.Count > 0) await ShowNoticeAsync("部分附件未添加", string.Join(Environment.NewLine, result.Rejected));
        if (result.Warnings.Count > 0) await ShowNoticeAsync("大文件提醒", string.Join(Environment.NewLine, result.Warnings));
    }

    private void RefreshAttachmentTray()
    {
        AttachmentPreviewPanel.Children.Clear();
        foreach (var attachment in _attachmentDraft.Items.ToArray())
        {
            AttachmentPreviewPanel.Children.Add(AttachmentPreviewFactory.Create(attachment, () =>
            {
                _attachmentDraft.Remove(attachment);
                RefreshAttachmentTray();
            }));
        }
        AttachmentTray.IsVisible = _attachmentDraft.Items.Count > 0;
        ScrollChatToEnd();
    }

    private void Window_DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.TryGetFiles() is { Length: > 0 } ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void Window_Drop(object? sender, DragEventArgs e)
    {
        var paths = e.DataTransfer.TryGetFiles()?.Select(x => x.TryGetLocalPath()).Where(x => x is not null).Cast<string>().ToArray() ?? [];
        if (paths.Length > 0) await AddAttachmentPathsAsync(paths);
        e.Handled = true;
    }

    private async Task<bool> TryPasteAttachmentsAsync()
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null) return false;
        var files = await clipboard.TryGetFilesAsync();
        var paths = files?.Select(x => x.TryGetLocalPath()).Where(x => x is not null).Cast<string>().ToArray() ?? [];
        if (paths.Length > 0)
        {
            await AddAttachmentPathsAsync(paths);
            return true;
        }
        var bitmap = await clipboard.TryGetBitmapAsync();
        if (bitmap is null) return false;
        var directory = Path.Combine(_dataDirectory, "clipboard"); Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"clipboard-{Guid.NewGuid():N}.png");
        bitmap.Save(path);
        await AddAttachmentPathsAsync([path]);
        try { File.Delete(path); } catch { }
        return true;
    }

    private async Task PasteClipboardTextAsync()
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        var pasted = clipboard is null ? null : await clipboard.TryGetTextAsync();
        if (string.IsNullOrEmpty(pasted)) return;
        var current = InputBox.Text ?? "";
        var start = Math.Clamp(Math.Min(InputBox.SelectionStart, InputBox.SelectionEnd), 0, current.Length);
        var end = Math.Clamp(Math.Max(InputBox.SelectionStart, InputBox.SelectionEnd), start, current.Length);
        InputBox.Text = current[..start] + pasted + current[end..];
        InputBox.CaretIndex = start + pasted.Length;
    }
    private void StickerButton_Click(object? sender, RoutedEventArgs e) => ToggleStickerPopup();
    private void ToggleStickerPopup()
    {
        if (StickerPopup.IsOpen) { StickerPopup.IsOpen = false; return; }
        StickerGrid.Children.Clear();
        foreach (var sticker in _stickers.Where(x => x.Enabled && File.Exists(x.FilePath)))
        {
            var preview = new Image { Source = new Bitmap(sticker.FilePath), Width = 72, Height = 72, Stretch = Stretch.Uniform };
            var button = new Button { Width = 82, Height = 96, Padding = new Thickness(4), Content = new StackPanel { Spacing = 3, Children = { preview, new TextBlock { Text = sticker.Name, FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis, HorizontalAlignment = HorizontalAlignment.Center, MaxWidth = 72 } } } };
            ToolTip.SetTip(button, sticker.Name);
            button.Click += async (_, _) => { StickerPopup.IsOpen = false; await SendAsync(false, sticker.Id); };
            StickerGrid.Children.Add(button);
        }
        if (StickerGrid.Children.Count == 0) StickerGrid.Children.Add(new TextBlock { Text = "表情库还是空的，请先在“管理表情包”中导入。", TextWrapping = TextWrapping.Wrap, Width = 340 });
        StickerPopup.IsOpen = true;
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
            var profile = GetSpeechProfile() ?? throw new InvalidOperationException("请先在设置中选择语音识别模型。");
            if (profile.Provider == "sherpa-onnx" && (!File.Exists(Path.Combine(profile.ModelPath, "model.int8.onnx")) || !File.Exists(Path.Combine(profile.ModelPath, "tokens.txt"))))
                throw new FileNotFoundException("本地 SenseVoice 模型文件不完整。", profile.ModelPath);
            if (WaveInEvent.DeviceCount == 0) throw new InvalidOperationException("没有检测到可用麦克风。");
            var tempDirectory = Path.Combine(_dataDirectory, "temp"); Directory.CreateDirectory(tempDirectory);
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
            _waveInput.StartRecording(); VoiceMicIcon.IsVisible = false; VoiceStopIcon.IsVisible = true; VoiceButton.Classes.Add("recording"); BeginUserComposition(true);
            TurnStatus.Text = "正在录音…再次点击结束"; ToolTip.SetTip(VoiceButton, "结束录音并转写");
        }
        catch (Exception ex) { await ResetRecordingAsync(); await ShowNoticeAsync("无法开始录音", ex.Message); }
    }

    private async Task StopRecordingAndRecognizeAsync()
    {
        _speechBusy = true; VoiceButton.IsEnabled = false; TurnStatus.Text = "正在识别语音…";
        try
        {
            _waveInput!.StopRecording(); if (_recordingStopped is not null) await _recordingStopped.Task;
            _waveInput.Dispose(); _waveInput = null;
            var path = _recordingPath ?? throw new InvalidOperationException("没有可转写的录音。");
            await using var recording = File.OpenRead(path);
            var uploaded = await _api.UploadAsync(recording, Path.GetFileName(path), "audio/wav");
            foreach (var warning in uploaded.Warnings) TurnStatus.Text = warning;
            var text = (await _api.TranscribeAsync(uploaded.Id)).Text;
            if (string.IsNullOrWhiteSpace(text)) await ShowNoticeAsync("没有识别到文字", "请靠近麦克风再试一次。录音不会保存到聊天记录。");
            else
            {
                var existing = InputBox.Text ?? ""; InputBox.Text = string.IsNullOrWhiteSpace(existing) ? text : $"{existing.TrimEnd()} {text}";
                InputBox.CaretIndex = InputBox.Text.Length; InputBox.Focus(); _turnManager?.NotifyInputActivity();
            }
        }
        catch (Exception ex) { await ShowNoticeAsync("语音识别失败", ex.Message); }
        finally { await ResetRecordingAsync(); }
    }

    private Task ResetRecordingAsync()
    {
        _waveWriter?.Dispose(); _waveWriter = null; _waveInput?.Dispose(); _waveInput = null;
        if (_recordingPath is not null) { try { File.Delete(_recordingPath); } catch { } }
        _recordingPath = null; _recordingStopped = null; _speechBusy = false;
        VoiceMicIcon.IsVisible = true; VoiceStopIcon.IsVisible = false; VoiceButton.Classes.Remove("recording"); VoiceButton.IsEnabled = true;
        ToolTip.SetTip(VoiceButton, "语音输入");
        if (string.IsNullOrWhiteSpace(InputBox.Text)) EndUserComposition(); else BeginUserComposition(false);
        return Task.CompletedTask;
    }
    private async void NewConversation_Click(object? sender, RoutedEventArgs e)
    {
        _conversationDrafts[_conversationId] = InputBox.Text ?? "";
        EndUserComposition(); _turnManager?.CancelAgentReply();
        var conversation = await _api.CreateConversationAsync(_character?.Id); _conversationId = conversation.Id;
        _suppressInputActivity = true; InputBox.Text = ""; _suppressInputActivity = false; ResetMessagesPanel(); CreateTurnManager();
        if (_character is not null) AddTextBubble(_character.Name, _character.Greeting, false);
        _preferences = _preferences with { ActiveConversationId = _conversationId };
        await _api.UpdatePreferencesAsync(new UpdateClientPreferencesRequest(_character?.Id, _conversationId, null, null, null, null, null));
        await RefreshConversationRoomsAsync();
    }

    private async void CharacterEditor_Click(object? sender, RoutedEventArgs e) => await OpenCharacterEditorAsync();
    private async void HeaderAvatarBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(HeaderAvatarBorder).Properties.IsLeftButtonPressed) return;
        e.Handled = true; await OpenCharacterEditorAsync();
    }

    private async void UserProfileButton_Click(object? sender, RoutedEventArgs e) => await OpenUserProfileEditorAsync();

    private async Task OpenUserProfileEditorAsync()
    {
        var profile = _preferences.UserProfile ?? new UserProfilePreferences();
        var theme = _preferences.Theme ?? new ThemePreferences();
        var displayName = new TextBox { Text = profile.DisplayName, Watermark = "你的显示名" };
        var signature = new TextBox { Text = profile.Signature, Watermark = "一句个性签名" };
        var focusX = new NumericUpDown { Value = .5m, Minimum = 0, Maximum = 1, Increment = .05m };
        var focusY = new NumericUpDown { Value = .5m, Minimum = 0, Maximum = 1, Increment = .05m };
        var zoom = new NumericUpDown { Value = 1, Minimum = 1, Maximum = 3, Increment = .05m };
        var preview = new Image { Width = 200, Height = 200, Stretch = Stretch.UniformToFill };
        string? avatarPath = theme.UserAvatarPath;
        var displays = theme.UserAvatarDisplays.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        void RefreshPreview()
        {
            preview.Source = null;
            if (string.IsNullOrWhiteSpace(avatarPath) || !File.Exists(avatarPath)) return;
            try
            {
                preview.Source = new Bitmap(avatarPath);
                ApplyImageDisplay(preview, new ImageDisplayPreferences { FocusX = (double)(focusX.Value ?? .5m), FocusY = (double)(focusY.Value ?? .5m), Zoom = (double)(zoom.Value ?? 1) });
            }
            catch { }
        }
        if (avatarPath is not null && displays.TryGetValue(avatarPath, out var existingDisplay))
        {
            focusX.Value = (decimal)existingDisplay.FocusX; focusY.Value = (decimal)existingDisplay.FocusY; zoom.Value = (decimal)existingDisplay.Zoom;
        }
        focusX.ValueChanged += (_, _) => RefreshPreview(); focusY.ValueChanged += (_, _) => RefreshPreview(); zoom.ValueChanged += (_, _) => RefreshPreview(); RefreshPreview();
        var choose = new Button { Content = "导入头像…" }; var clear = new Button { Content = "使用默认头像" };
        var save = new Button { Content = "保存资料", Classes = { "primary" }, HorizontalAlignment = HorizontalAlignment.Right };
        var panel = new StackPanel { Margin = new Thickness(26), Spacing = 10 };
        panel.Children.Add(new TextBlock { Text = "用户资料", FontSize = 22, FontWeight = FontWeight.SemiBold });
        panel.Children.Add(new TextBlock { Text = "头像读取原始素材，不生成压缩副本。拖动调整取景，滚轮缩放。", Foreground = Brush.Parse("#8EA2B5"), TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(CreateVisualCropEditor(preview, focusX, focusY, zoom, 200, 200, true));
        panel.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { choose, clear } });
        panel.Children.Add(new TextBlock { Text = "用户名" }); panel.Children.Add(displayName);
        panel.Children.Add(new TextBlock { Text = "个性签名" }); panel.Children.Add(signature); panel.Children.Add(save);
        var window = new Window { Title = "用户资料", Width = 560, Height = 650, WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = CreateThemedDialogSurface(panel) };
        choose.Click += async (_, _) =>
        {
            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "选择用户头像", AllowMultiple = false, FileTypeFilter = [new FilePickerFileType("图片") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp"] }] });
            var source = files.FirstOrDefault()?.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(source)) return;
            avatarPath = source; focusX.Value = .5m; focusY.Value = .5m; zoom.Value = 1; RefreshPreview();
        };
        clear.Click += (_, _) => { avatarPath = null; preview.Source = null; };
        save.Click += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(displayName.Text)) { await ShowNoticeAsync("无法保存", "用户名不能为空。"); return; }
            if (avatarPath is not null) displays[avatarPath] = new ImageDisplayPreferences { FocusX = (double)(focusX.Value ?? .5m), FocusY = (double)(focusY.Value ?? .5m), Zoom = (double)(zoom.Value ?? 1) };
            var history = theme.UserAvatarHistory.Append(avatarPath).Where(x => !string.IsNullOrWhiteSpace(x) && File.Exists(x)).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            _preferences = _preferences with
            {
                UserProfile = new UserProfilePreferences { DisplayName = displayName.Text.Trim(), Signature = signature.Text?.Trim() ?? "" },
                Theme = theme with { UserAvatarPath = avatarPath, UserAvatarHistory = history, UserAvatarDisplays = displays }
            };
            await _api.UpdatePreferencesAsync(new UpdateClientPreferencesRequest(null, null, _preferences.UserProfile,
                null, null, null, null));
            string? avatarMediaId = null;
            if (!string.IsNullOrWhiteSpace(avatarPath) && File.Exists(avatarPath))
            {
                await using var content = File.OpenRead(avatarPath);
                avatarMediaId = (await _api.UploadAsync(content, Path.GetFileName(avatarPath), ImageContentType(avatarPath))).Id;
            }
            await _api.UpdateAppearanceAsync(new UpdateAppearanceMediaRequest(null, false, avatarMediaId,
                avatarPath is null, null, avatarPath is null ? null : displays[avatarPath]));
            ApplyTheme(); window.Close();
        };
        await window.ShowDialog(this);
    }

    private async Task OpenCharacterEditorAsync(Window? owner = null)
    {
        if (_character is null) return;
        TextBox Box(int height = 34) => new() { MinHeight = height, AcceptsReturn = height > 60, TextWrapping = TextWrapping.Wrap };
        var cardName = Box(); var name = Box(); var userName = Box(); var identity = Box(80); var personality = Box(80); var speech = Box(70); var rules = Box(110); var facts = Box(110); var greeting = Box(70); var legacyPrompt = Box(430);
        string? avatarPath = _character.AvatarPath;
        var avatarDisplay = _character.AvatarDisplay;
        var avatarPreview = new Image { Width = 180, Height = 180, Stretch = Stretch.UniformToFill };
        var chooseAvatar = new Button { Content = "导入角色头像…" }; var clearAvatar = new Button { Content = "恢复默认" };
        var avatarFocusX = new NumericUpDown { Value = (decimal)avatarDisplay.FocusX, Minimum = 0, Maximum = 1, Increment = .05m };
        var avatarFocusY = new NumericUpDown { Value = (decimal)avatarDisplay.FocusY, Minimum = 0, Maximum = 1, Increment = .05m };
        var avatarZoom = new NumericUpDown { Value = (decimal)avatarDisplay.Zoom, Minimum = 1, Maximum = 3, Increment = .05m };
        void RefreshAvatarPreview()
        {
            avatarPreview.Source = null;
            avatarDisplay = new ImageDisplayPreferences { FocusX = (double)(avatarFocusX.Value ?? .5m), FocusY = (double)(avatarFocusY.Value ?? .5m), Zoom = (double)(avatarZoom.Value ?? 1) };
            if (!string.IsNullOrWhiteSpace(avatarPath) && File.Exists(avatarPath)) try { avatarPreview.Source = new Bitmap(avatarPath); ApplyImageDisplay(avatarPreview, avatarDisplay); } catch { }
        }
        avatarFocusX.ValueChanged += (_, _) => RefreshAvatarPreview(); avatarFocusY.ValueChanged += (_, _) => RefreshAvatarPreview(); avatarZoom.ValueChanged += (_, _) => RefreshAvatarPreview();
        var cardList = new ListBox { Margin = new Thickness(12), Background = Brushes.Transparent }; var currentHint = new TextBlock { Foreground = ThemeAccentBrush(), TextWrapping = TextWrapping.Wrap };
        var newCard = new Button { Content = "＋ 新建角色卡", Margin = new Thickness(12, 0, 12, 12) };
        var structuredButton = new Button { Content = "结构化" }; var legacyButton = new Button { Content = "Legacy" }; var mode = CharacterPromptMode.Structured; string? editingId = _character.Id; bool loading = false;
        var structuredPanel = new StackPanel { Spacing = 8 }; var legacyPanel = new StackPanel { Spacing = 8 }; var form = new StackPanel { Margin = new Thickness(24), Spacing = 10 };
        void Field(Panel target, string label, Control control) { target.Children.Add(new TextBlock { Text = label, FontWeight = FontWeight.SemiBold }); target.Children.Add(control); }
        Field(structuredPanel, "对用户的称呼", userName); Field(structuredPanel, "身份", identity); Field(structuredPanel, "人格", personality); Field(structuredPanel, "说话风格", speech); Field(structuredPanel, "行为规则（每行一条）", rules); Field(structuredPanel, "世界设定（每行一条）", facts);
        legacyPanel.Children.Add(new TextBlock { Text = "完整 System Prompt", FontWeight = FontWeight.SemiBold }); legacyPanel.Children.Add(new TextBlock { Text = "内容原样作为角色设定；应用只追加记忆、当前状态和回复格式要求。", Foreground = Brush.Parse("#8EA2B5"), TextWrapping = TextWrapping.Wrap }); legacyPanel.Children.Add(legacyPrompt);
        void SetMode(CharacterPromptMode value) { mode = value; structuredPanel.IsVisible = value == CharacterPromptMode.Structured; legacyPanel.IsVisible = value == CharacterPromptMode.Legacy; structuredButton.Classes.Set("primary", value == CharacterPromptMode.Structured); legacyButton.Classes.Set("primary", value == CharacterPromptMode.Legacy); }
        structuredButton.Click += (_, _) => SetMode(CharacterPromptMode.Structured); legacyButton.Click += (_, _) => SetMode(CharacterPromptMode.Legacy);
        Field(form, "角色卡名称（显示名-用途-版本）", cardName); Field(form, "聊天显示名", name);
        form.Children.Add(new TextBlock { Text = "角色头像", FontWeight = FontWeight.SemiBold });
        form.Children.Add(new TextBlock { Text = "从原始图片直接解码；拖动调整取景，滚轮缩放，不会压缩或改写原图。", Foreground = Brush.Parse("#8EA2B5"), TextWrapping = TextWrapping.Wrap });
        form.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, Children = { CreateVisualCropEditor(avatarPreview, avatarFocusX, avatarFocusY, avatarZoom, 180, 180, true), new StackPanel { Spacing = 7, VerticalAlignment = VerticalAlignment.Center, Children = { chooseAvatar, clearAvatar } } } });
        form.Children.Add(currentHint); form.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { structuredButton, legacyButton } }); form.Children.Add(structuredPanel); form.Children.Add(legacyPanel); Field(form, "开场白", greeting);
        var removeCard = new Button { Content = "删除角色卡" };
        var save = new Button { Content = "保存", Classes = { "primary" } }; var saveAndSwitch = new Button { Content = "保存并切换到此角色" }; form.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Margin = new Thickness(0, 10, 0, 0), Children = { removeCard, saveAndSwitch, save } });
        var left = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), Background = Brushes.Transparent, Children = { new TextBlock { Text = "角色卡", FontSize = 21, FontWeight = FontWeight.SemiBold, Margin = new Thickness(16, 18, 12, 4) }, cardList, newCard } }; Grid.SetRow(cardList, 1); Grid.SetRow(newCard, 2);
        var editorScroll = new ScrollViewer { Content = form, Background = Brushes.Transparent };
        var divider = new Border { Width = 1, Background = Brush.Parse("#263746"), HorizontalAlignment = HorizontalAlignment.Right };
        var layout = new Grid { ColumnDefinitions = new ColumnDefinitions("210,*"), Background = Brushes.Transparent, Children = { left, divider, editorScroll } }; Grid.SetColumn(editorScroll, 1);
        var window = new Window { Title = "角色卡管理", Width = 920, Height = 740, Background = Brushes.Transparent, WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = CreateThemedDialogSurface(layout) };
        chooseAvatar.Click += async (_, _) =>
        {
            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "选择角色头像", AllowMultiple = false, FileTypeFilter = [new FilePickerFileType("图片") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp"] }] });
            var source = files.FirstOrDefault()?.TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(source)) { avatarPath = source; RefreshAvatarPreview(); }
        };
        clearAvatar.Click += (_, _) => { avatarPath = null; RefreshAvatarPreview(); };
        void RefreshList(string? selectId)
        {
            loading = true; var items = _characters.Select(x => new CharacterChoice(x.Id, CardLabel(x) + (x.Id == _character?.Id ? "  · 当前" : ""))).ToArray(); cardList.ItemsSource = items; cardList.SelectedItem = items.FirstOrDefault(x => x.Id == selectId); loading = false;
        }
        void LoadCard(CharacterDefinition card)
        {
            editingId = card.Id; cardName.Text = CardLabel(card); name.Text = card.Name; avatarPath = card.AvatarPath; avatarDisplay = card.AvatarDisplay; avatarFocusX.Value = (decimal)avatarDisplay.FocusX; avatarFocusY.Value = (decimal)avatarDisplay.FocusY; avatarZoom.Value = (decimal)avatarDisplay.Zoom; RefreshAvatarPreview(); userName.Text = card.UserName; identity.Text = card.Identity; personality.Text = card.Personality; speech.Text = card.SpeechStyle; rules.Text = string.Join(Environment.NewLine, card.BehavioralRules); facts.Text = string.Join(Environment.NewLine, card.WorldFacts); greeting.Text = card.Greeting; legacyPrompt.Text = card.LegacySystemPrompt; currentHint.Text = card.Id == _character?.Id ? "这是当前聊天角色" : "正在编辑其他角色；保存不会自动切换当前聊天"; SetMode(card.PromptMode);
        }
        cardList.SelectionChanged += (_, _) => { if (!loading && cardList.SelectedItem is CharacterChoice choice) { var card = _characters.FirstOrDefault(x => x.Id == choice.Id); if (card is not null) LoadCard(card); } };
        newCard.Click += (_, _) => { editingId = null; cardName.Text = "新角色-正式-v1"; name.Text = "新角色"; avatarPath = null; avatarDisplay = new ImageDisplayPreferences(); avatarFocusX.Value = .5m; avatarFocusY.Value = .5m; avatarZoom.Value = 1; RefreshAvatarPreview(); userName.Text = "你"; identity.Text = ""; personality.Text = ""; speech.Text = ""; rules.Text = ""; facts.Text = ""; greeting.Text = "你好。"; legacyPrompt.Text = ""; currentHint.Text = "尚未保存的新角色卡"; SetMode(CharacterPromptMode.Structured); loading = true; cardList.SelectedItem = null; loading = false; };
        CharacterDefinition? ReadEditedCard()
        {
            if (string.IsNullOrWhiteSpace(cardName.Text) || string.IsNullOrWhiteSpace(name.Text) || (mode == CharacterPromptMode.Structured && string.IsNullOrWhiteSpace(identity.Text)) || (mode == CharacterPromptMode.Legacy && string.IsNullOrWhiteSpace(legacyPrompt.Text))) return null;
            static string[] Lines(string? value) => (value ?? "").Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var original = editingId is null ? _character : _characters.FirstOrDefault(x => x.Id == editingId) ?? _character;
            return original with { Id = editingId ?? $"character-{Guid.NewGuid():N}", CardName = cardName.Text.Trim(), Name = name.Text.Trim(), AvatarPath = avatarPath, AvatarDisplay = avatarDisplay, PromptMode = mode, LegacySystemPrompt = legacyPrompt.Text?.Trim() ?? "", UserName = userName.Text?.Trim() ?? "你", Identity = identity.Text?.Trim() ?? "", Personality = personality.Text?.Trim() ?? "", SpeechStyle = speech.Text?.Trim() ?? "", BehavioralRules = Lines(rules.Text), WorldFacts = Lines(facts.Text), Greeting = greeting.Text?.Trim() ?? "你好。" };
        }
        async Task SaveAsync(bool switchToCard)
        {
            var edited = ReadEditedCard(); if (edited is null) { await ShowNoticeAsync("无法保存", "角色名以及当前模式的角色设定不能为空。"); return; }
            string? avatarMediaId = null;
            if (!string.IsNullOrWhiteSpace(avatarPath) && File.Exists(avatarPath))
            {
                await using var avatar = File.OpenRead(avatarPath);
                avatarMediaId = (await _api.UploadAsync(avatar, Path.GetFileName(avatarPath), ImageContentType(avatarPath))).Id;
            }
            var request = new SaveCharacterRequest(edited.Name, edited.CardName, edited.UserName, edited.Identity,
                edited.Personality, edited.SpeechStyle, edited.BehavioralRules, edited.WorldFacts, edited.InitialState,
                edited.Greeting, edited.PromptMode, edited.LegacySystemPrompt, avatarMediaId,
                avatarPath is null, edited.AvatarDisplay);
            var saved = editingId is null
                ? await _api.CreateCharacterAsync(request)
                : await _api.UpdateCharacterAsync(editingId, request);
            edited = edited with { Id = saved.Id };
            _characters.RemoveAll(x => x.Id == edited.Id); _characters.Add(edited); editingId = edited.Id;
            if (_character?.Id == edited.Id || switchToCard) { _character = edited; CharacterName.Text = edited.Name; ApplyTheme(); CreateTurnManager(); }
            if (switchToCard)
            {
                var conversation = await _api.CreateConversationAsync(edited.Id); _conversationId = conversation.Id;
                ResetMessagesPanel(); AddTextBubble(edited.Name, edited.Greeting, false);
                _preferences = _preferences with { SelectedCharacterId = edited.Id, ActiveConversationId = _conversationId };
                await _api.UpdatePreferencesAsync(new UpdateClientPreferencesRequest(edited.Id, _conversationId, null, null, null, null, null));
            }
            RefreshCharacterSelector(); RefreshList(edited.Id); currentHint.Text = edited.Id == _character?.Id ? "这是当前聊天角色" : "已保存；当前聊天角色没有改变"; RuntimeStatus.Text = "角色卡已保存";
        }
        removeCard.Click += async (_, _) =>
        {
            if (editingId is null) { await ShowNoticeAsync("无法删除", "这张角色卡尚未保存。"); return; }
            var deletingId = editingId;
            var deleting = _characters.FirstOrDefault(x => x.Id == deletingId);
            if (deleting is null || !await ConfirmAsync(window, "删除角色卡", "确定删除“" + CardLabel(deleting) + "”吗？此操作不可撤销。")) return;
            try
            {
                await _api.DeleteCharacterAsync(deletingId);
                _characters.RemoveAll(x => x.Id == deletingId);
                var next = _characters.FirstOrDefault();
                if (next is null) { window.Close(); return; }
                LoadCard(next); RefreshCharacterSelector(); RefreshList(next.Id); RuntimeStatus.Text = "角色卡已删除";
            }
            catch (PryBackendException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                await ShowNoticeAsync("无法删除角色卡", ex.Message);
            }
        };
        save.Click += async (_, _) => await SaveAsync(false); saveAndSwitch.Click += async (_, _) => await SaveAsync(true);
        RefreshList(_character.Id); LoadCard(_character); await window.ShowDialog(owner ?? this);
    }

    private static string CardLabel(CharacterDefinition card) => string.IsNullOrWhiteSpace(card.CardName) ? $"{card.Name}-正式-v1" : card.CardName;

    private static string ImageContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png", ".webp" => "image/webp", ".gif" => "image/gif", ".bmp" => "image/bmp", _ => "image/jpeg"
    };

    private async void MemoryManager_Click(object? sender, RoutedEventArgs e) => await OpenMemoryManagerAsync();

    private async Task OpenMemoryManagerAsync(Window? owner = null)
    {
        if (_character is null) return;
        var window = new Window { Title = "长期记忆管理", Width = 940, Height = 680, Background = Brushes.Transparent, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var search = new TextBox { Watermark = "搜索内容、标签或类型…" }; var searchButton = new Button { Content = "搜索" };
        var list = new ListBox { Background = Brushes.Transparent };
        var kind = new ComboBox { ItemsSource = new[] { "user_fact", "preference", "promise", "relationship", "event", "other" }, SelectedItem = "user_fact" };
        var summary = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 150 };
        var tags = new TextBox { Watermark = "用逗号分隔" }; var importance = new NumericUpDown { Value = .65m, Minimum = 0, Maximum = 1, Increment = .05m };
        var metadata = new TextBlock { Foreground = Brush.Parse("#7F91A4"), TextWrapping = TextWrapping.Wrap };
        var create = new Button { Content = "＋ 新建记忆" }; var save = new Button { Content = "保存", Classes = { "primary" } }; var remove = new Button { Content = "删除" };
        long? editingId = null;
        async Task RefreshAsync(long? selectId = null)
        {
            var memories = await _api.GetMemoriesAsync(_character.Id, search.Text);
            var items = memories.Select(x => new MemoryListItem(x)).ToArray(); list.ItemsSource = items;
            list.SelectedItem = items.FirstOrDefault(x => x.Memory.Id == selectId);
        }
        void Load(MemoryRecord memory)
        {
            editingId = memory.Id; kind.SelectedItem = memory.Kind; summary.Text = memory.Summary; tags.Text = memory.Tags; importance.Value = (decimal)memory.Importance;
            metadata.Text = $"创建：{memory.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm}　更新：{memory.UpdatedAt?.ToLocalTime():yyyy-MM-dd HH:mm}　最近调用：{memory.LastUsedAt?.ToLocalTime():yyyy-MM-dd HH:mm}";
        }
        void NewMemory() { editingId = null; kind.SelectedItem = "user_fact"; summary.Text = ""; tags.Text = ""; importance.Value = .65m; metadata.Text = "尚未保存的新记忆"; list.SelectedItem = null; }
        list.SelectionChanged += (_, _) => { if (list.SelectedItem is MemoryListItem item) Load(item.Memory); };
        searchButton.Click += async (_, _) => await RefreshAsync(); create.Click += (_, _) => NewMemory();
        save.Click += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(summary.Text)) { await ShowNoticeAsync("无法保存", "记忆内容不能为空。"); return; }
            if (editingId is long id)
                await _api.UpdateMemoryAsync(id, _character.Id, new UpdateMemoryRequest(kind.SelectedItem?.ToString() ?? "other", summary.Text.Trim(), tags.Text?.Trim() ?? "", (double)(importance.Value ?? .65m)));
            else
                editingId = (await _api.CreateMemoryAsync(new CreateMemoryRequest(_character.Id, kind.SelectedItem?.ToString() ?? "other", summary.Text.Trim(), tags.Text?.Trim() ?? "", (double)(importance.Value ?? .65m)))).Id;
            await RefreshAsync(editingId);
        };
        remove.Click += async (_, _) => { if (editingId is not long id || !await ConfirmAsync(window, "删除长期记忆", "确定永久删除这条记忆吗？")) return; await _api.DeleteMemoryAsync(id, _character.Id); NewMemory(); await RefreshAsync(); };
        var leftHeader = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { search, searchButton } }; Grid.SetColumn(searchButton, 1);
        var listCard = CreateCompactDialogTextCard(list); listCard.Padding = new Thickness(8); listCard.Margin = new Thickness(0, 10);
        var left = new Grid { Margin = new Thickness(18), RowDefinitions = new RowDefinitions("Auto,*,Auto"), Children = { leftHeader, listCard, create } }; Grid.SetRow(listCard, 1); Grid.SetRow(create, 2);
        var form = new StackPanel { Margin = new Thickness(10), Spacing = 9 };
        void Field(string label, Control control) { form.Children.Add(new TextBlock { Text = label, Foreground = Brush.Parse("#B8CCE0") }); form.Children.Add(control); }
        form.Children.Add(new TextBlock { Text = $"{_character.Name}的长期记忆", FontSize = 22, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 0, 0, 8) }); Field("类型", kind); Field("内容", summary); Field("标签", tags); Field("重要度", importance); form.Children.Add(metadata); form.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Children = { remove, save } });
        var divider = new Border { BorderBrush = Brush.Parse("#263746"), BorderThickness = new Thickness(0, 0, 1, 0), Child = left };
        var formCard = CreateCompactDialogTextCard(new ScrollViewer { Content = form }); formCard.Margin = new Thickness(12, 18, 18, 18); formCard.Padding = new Thickness(12);
        var layout = new Grid { ColumnDefinitions = new ColumnDefinitions("350,*"), Children = { divider, formCard } }; Grid.SetColumn(formCard, 1); window.Content = CreateThemedDialogSurface(layout);
        NewMemory(); await RefreshAsync(); await window.ShowDialog(owner ?? this);
    }

    private async void Settings_Click(object? sender, RoutedEventArgs e)
    {
        var settingsOriginalTheme = _preferences.Theme;
        var detectedDevices = await _api.GetComputeDevicesAsync();
        var computeChoices = new List<ComputeDeviceChoice>
        {
            new("auto-discrete", detectedDevices.Count == 0 ? "自动（优先独显；当前未检测到 GPU 后端）" : "自动（优先独立显卡）"),
            new("cpu", "仅使用 CPU")
        };
        computeChoices.AddRange(detectedDevices.Select(x => new ComputeDeviceChoice(x.Id, $"{x.Name}（{x.IsIntegrated switch { true => "核显", false => "独显/加速卡" }}）")));
        var settingsUi = new SettingsUiFactory();
        TextBox Box(string value, int height = 34) => settingsUi.CreateTextBox(value, height);
        NumericUpDown Number(decimal value, decimal min, decimal max, decimal step = 1) => settingsUi.CreateNumber(value, min, max, step);
        var turn = EffectiveTurnSettings();
        var themePreferences = _preferences.Theme ?? new ThemePreferences();
        var allModels = _builtInProfiles.Concat(_preferences.CustomModels).ToArray();
        var modelSelection = new ModelSelectionEditor(allModels,
            _builtInSpeechProfiles.Concat(_preferences.CustomSpeechModels).ToArray(),
            GetActiveModelId(), GetVisionModelId(), GetSpeechModelId(), settingsUi);
        var initialTunings = _preferences.ModelTunings.ToDictionary(x => x.Key, x => x.Value);
        var activeTuningId = GetActiveModelId();
        if (!initialTunings.ContainsKey(activeTuningId) && GetModelTuning(activeTuningId) is { } legacyTuning)
            initialTunings[activeTuningId] = legacyTuning;
        var tuningEditor = new ModelTuningSettingsEditor(allModels, computeChoices, activeTuningId,
            initialTunings, settingsUi);
        var conversationEditor = new ConversationSettingsEditor(turn, settingsUi);
        var shortcutEditor = new ShortcutSettingsEditor(_preferences.Shortcuts, settingsUi);
        var desktopPetEditor = new DesktopPetSettingsEditor(_preferences.DesktopPet, settingsUi);
        var settingCards = settingsUi.Cards;
        var modelPanel = new StackPanel { Margin = new Thickness(28), Spacing = 14 }; var conversationPanel = conversationEditor.Panel; var shortcutPanel = shortcutEditor.Panel; var desktopPanel = desktopPetEditor.Panel; var themePanel = new StackPanel { Margin = new Thickness(28), Spacing = 14 };
        void Header(Panel target, string value) => settingsUi.AddHeader(target, value);
        void Field(Panel target, string label, Control control) => settingsUi.AddField(target, label, control);
        Border Card(string title, params Control[] controls) => settingsUi.CreateCard(title, controls);
        Header(modelPanel, "模型与语音");
        modelPanel.Children.Add(Card("对话模型分工", modelSelection.TextFields));
        modelPanel.Children.Add(Card("每模型独立参数", tuningEditor.Panel));
        modelPanel.Children.Add(Card("语音识别", modelSelection.SpeechFields));
        var tips = LoadTips();
        var tipText = tips.Count == 0 ? "暂无使用提示。" : tips[Random.Shared.Next(tips.Count)];
        using var themeImages = new ThemeImageResources();
        var backgroundDraft = new ThemeImageDraft(themePreferences.BackgroundImagePath, themePreferences.BackgroundHistory, themePreferences.BackgroundDisplays);
        var avatarDraft = new ThemeImageDraft(themePreferences.UserAvatarPath, themePreferences.UserAvatarHistory, themePreferences.UserAvatarDisplays);
        var backgroundGallery = new WrapPanel { Orientation = Orientation.Horizontal }; var userAvatarGallery = new WrapPanel { Orientation = Orientation.Horizontal };
        var backgroundPreview = new Image { Width = 500, Height = 280, Stretch = Stretch.UniformToFill };
        var userAvatarPreview = new Image { Width = 120, Height = 120, Stretch = Stretch.UniformToFill };
        var browseBackground = new Button { Content = "导入背景…" }; var clearBackground = new Button { Content = "不使用背景" }; var deleteBackground = new Button { Content = "删除所选" };
        var browseUserAvatar = new Button { Content = "导入用户头像…" }; var clearUserAvatar = new Button { Content = "使用默认头像" }; var deleteUserAvatar = new Button { Content = "删除所选" };
        var themeMode = new ComboBox { ItemsSource = new[] { "跟随系统", "深色", "浅色" }, SelectedIndex = themePreferences.ThemeMode.ToLowerInvariant() switch { "dark" => 1, "light" => 2, _ => 0 } };
        var accentColor = Box(string.IsNullOrWhiteSpace(themePreferences.AccentColor) ? "#B148C6" : themePreferences.AccentColor);
        var accentPalette = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var color in new[] { "#B148C6", "#7C5CFC", "#3B82F6", "#06A6A6", "#16A36A", "#84A322", "#E29A24", "#D95D39", "#E05278", "#6B7A90" })
        {
            var swatch = new Button { Width = 34, Height = 34, Padding = new Thickness(0), Margin = new Thickness(0, 0, 7, 7), Background = Brush.Parse(color), BorderBrush = Brushes.White, BorderThickness = new Thickness(1) };
            ToolTip.SetTip(swatch, color); swatch.Click += (_, _) => accentColor.Text = color; accentPalette.Children.Add(swatch);
        }
        var glassEffects = new CheckBox { Content = "侧边栏、顶部标题栏和底部输入区透出软件背景", IsChecked = themePreferences.UseGlassEffects };
        var liveSidebarResize = new CheckBox { Content = "拖动时实时调整侧边栏宽度", IsChecked = themePreferences.LiveSidebarResize };
        var backgroundDim = Number((decimal)themePreferences.BackgroundDimOpacity, 0, .85m, .05m);
        var backgroundOpacity = Number((decimal)themePreferences.BackgroundImageOpacity, 0, 1, .05m);
        var backgroundBlur = Number((decimal)themePreferences.BackgroundBlurRadius, 0, 32, 1);
        var avatarSize = Number((decimal)themePreferences.AvatarSize, 28, 76, 1);
        var bubbleFontSize = Number((decimal)themePreferences.BubbleFontSize, 11, 24, .5m);
        var bubbleMaxWidth = Number((decimal)themePreferences.BubbleMaxWidth, 280, 900, 20);
        var bubbleSpacing = Number((decimal)themePreferences.BubbleSpacing, 2, 36, 1);
        var backgroundFocusX = Number(.5m, 0, 1, .05m); var backgroundFocusY = Number(.5m, 0, 1, .05m); var backgroundZoom = Number(1, 1, 3, .05m);
        var avatarFocusX = Number(.5m, 0, 1, .05m); var avatarFocusY = Number(.5m, 0, 1, .05m); var avatarZoom = Number(1, 1, 3, .05m);
        var loadingImageControls = false;
        ImageDisplayPreferences ReadDisplay(NumericUpDown x, NumericUpDown y, NumericUpDown zoom) => new() { FocusX = (double)(x.Value ?? .5m), FocusY = (double)(y.Value ?? .5m), Zoom = (double)(zoom.Value ?? 1) };
        void LoadDisplay(string? path, IReadOnlyDictionary<string, ImageDisplayPreferences> values, NumericUpDown x, NumericUpDown y, NumericUpDown zoom, Image preview)
        {
            loadingImageControls = true; var display = path is not null && values.TryGetValue(path, out var saved) ? saved : new ImageDisplayPreferences();
            x.Value = (decimal)display.FocusX; y.Value = (decimal)display.FocusY; zoom.Value = (decimal)display.Zoom;
            if (themeImages.Load(preview, path)) ApplyImageDisplay(preview, display);
            loadingImageControls = false;
        }
        void RefreshImageGallery(WrapPanel gallery, IReadOnlyList<string> paths, string? selected, Action<string> select, bool square)
            => ThemeImageGallery.Refresh(gallery, paths, selected, select, square, ThemeAccentBrush(), themeImages);
        Action previewTheme = () => { };
        void RefreshBackgroundGallery() => RefreshImageGallery(backgroundGallery, backgroundDraft.History, backgroundDraft.SelectedPath, path => { backgroundDraft.Select(path, ReadDisplay(backgroundFocusX, backgroundFocusY, backgroundZoom)); LoadDisplay(path, backgroundDraft.Displays, backgroundFocusX, backgroundFocusY, backgroundZoom, backgroundPreview); RefreshBackgroundGallery(); previewTheme(); }, false);
        void RefreshUserAvatarGallery() => RefreshImageGallery(userAvatarGallery, avatarDraft.History, avatarDraft.SelectedPath, path => { avatarDraft.Select(path, ReadDisplay(avatarFocusX, avatarFocusY, avatarZoom)); LoadDisplay(path, avatarDraft.Displays, avatarFocusX, avatarFocusY, avatarZoom, userAvatarPreview); RefreshUserAvatarGallery(); previewTheme(); }, true);
        RefreshBackgroundGallery(); RefreshUserAvatarGallery(); LoadDisplay(backgroundDraft.SelectedPath, backgroundDraft.Displays, backgroundFocusX, backgroundFocusY, backgroundZoom, backgroundPreview); LoadDisplay(avatarDraft.SelectedPath, avatarDraft.Displays, avatarFocusX, avatarFocusY, avatarZoom, userAvatarPreview);
        Header(themePanel, "外观与主题");
        var appearanceFields = new StackPanel { Spacing = 7 }; Field(appearanceFields, "主题模式", themeMode); Field(appearanceFields, "强调色调色板", accentPalette); Field(appearanceFields, "自定义十六进制颜色", accentColor);
        themePanel.Children.Add(Card("颜色主题", appearanceFields, new TextBlock { Text = "可跟随系统切换深浅色，也可以指定软件强调色。", TextWrapping = TextWrapping.Wrap, Foreground = Brush.Parse("#7F91A4") }));
        var backgroundCropEditor = CreateVisualCropEditor(backgroundPreview, backgroundFocusX, backgroundFocusY, backgroundZoom, 500, 280, false);
        var openBackgroundSettings = new Button { Content = "聊天背景、透明度与模糊…", HorizontalAlignment = HorizontalAlignment.Left };
        var openAvatarSettings = new Button { Content = "用户头像与取景…", HorizontalAlignment = HorizontalAlignment.Left };
        themePanel.Children.Add(Card("图片素材", new TextBlock { Text = "背景与头像取景使用独立页面，避免大预览区占满外观设置并影响滚动。", TextWrapping = TextWrapping.Wrap, Foreground = Brush.Parse("#7F91A4") }, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { openBackgroundSettings, openAvatarSettings } }));
        themePanel.Children.Add(Card("界面材质", glassEffects));
        var avatarCropEditor = CreateVisualCropEditor(userAvatarPreview, avatarFocusX, avatarFocusY, avatarZoom, 180, 180, true);
        var backgroundPage = new StackPanel { Margin = new Thickness(28), Spacing = 14 };
        var backgroundBack = new Button { Content = "← 返回外观与主题", HorizontalAlignment = HorizontalAlignment.Left };
        Header(backgroundPage, "聊天背景"); backgroundPage.Children.Add(backgroundBack);
        backgroundPage.Children.Add(Card("背景图片与取景", new TextBlock { Text = "在预览中拖动取景，滚轮缩放。透明度会透出桌面；模糊程度同时作用于背景图，并在系统支持时启用桌面背景模糊。", TextWrapping = TextWrapping.Wrap, Foreground = Brush.Parse("#B8CCE0") }, backgroundCropEditor, backgroundGallery, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { browseBackground, clearBackground, deleteBackground } }, new TextBlock { Text = "背景透明度（透出桌面）", Foreground = Brush.Parse("#B8CCE0") }, backgroundOpacity, new TextBlock { Text = "背景模糊程度", Foreground = Brush.Parse("#B8CCE0") }, backgroundBlur, new TextBlock { Text = "背景遮罩浓度", Foreground = Brush.Parse("#B8CCE0") }, backgroundDim));
        var avatarPage = new StackPanel { Margin = new Thickness(28), Spacing = 14 };
        var avatarBack = new Button { Content = "← 返回外观与主题", HorizontalAlignment = HorizontalAlignment.Left };
        Header(avatarPage, "用户头像"); avatarPage.Children.Add(avatarBack);
        avatarPage.Children.Add(Card("头像图片与取景", new TextBlock { Text = "头像与背景采用相同的可视化操作：拖动取景、滚轮缩放；始终从原始素材直接解码，不压缩、不生成缩略副本。", TextWrapping = TextWrapping.Wrap, Foreground = Brush.Parse("#B8CCE0") }, avatarCropEditor, userAvatarGallery, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { browseUserAvatar, clearUserAvatar, deleteUserAvatar } }));
        var uiSizeFields = new StackPanel { Spacing = 7 }; Field(uiSizeFields, "头像尺寸", avatarSize); Field(uiSizeFields, "聊天文字大小", bubbleFontSize); Field(uiSizeFields, "气泡最大宽度", bubbleMaxWidth); Field(uiSizeFields, "气泡纵向间距", bubbleSpacing);
        themePanel.Children.Add(Card("界面尺寸（实时预览）", liveSidebarResize, new TextBlock { Text = "关闭时拖动只显示位置指示线，松手后调整宽度；开启时侧边栏会跟随指针实时变化。", TextWrapping = TextWrapping.Wrap, Foreground = Brush.Parse("#7F91A4") }, uiSizeFields, new TextBlock { Text = "调整后主聊天窗口会立即呈现效果；关闭设置而不保存会恢复原值。", TextWrapping = TextWrapping.Wrap, Foreground = Brush.Parse("#7F91A4") }));
        var characterManagerButton = new Button { Content = "打开角色卡管理…", HorizontalAlignment = HorizontalAlignment.Left };
        var stickerManagerButton = new Button { Content = "打开表情包管理…", HorizontalAlignment = HorizontalAlignment.Left };
        var memoryManagerButton = new Button { Content = "打开长期记忆管理…", HorizontalAlignment = HorizontalAlignment.Left };
        var characterManagerPanel = settingsUi.CreateManagerPanel("角色卡", $"当前角色：{_character?.Name ?? "未加载"}。可编辑、切换或新建角色卡。", characterManagerButton);
        var stickerManagerPanel = settingsUi.CreateManagerPanel("表情包", "管理预装与用户导入的表情，设置情绪标签和互动作用。", stickerManagerButton);
        var memoryManagerPanel = settingsUi.CreateManagerPanel("长期记忆", "查看、检索和编辑当前角色的长期记忆库。", memoryManagerButton);
        var categories = new ListBox { Padding = new Thickness(10, 18), Background = Brushes.Transparent, ItemsSource = new[] { "模型与语音", "对话与节奏", "快捷键", "桌宠与托盘", "外观与主题", "角色卡", "表情包", "长期记忆" }, SelectedIndex = 0 };
        var tipValue = new TextBlock { Text = tipText, TextWrapping = TextWrapping.Wrap, Foreground = ThemeAccentBrush(), FontSize = 12 };
        var tipPanel = new StackPanel { Spacing = 6, Children = { new TextBlock { Text = "Tips", FontWeight = FontWeight.SemiBold }, tipValue } };
        var tipCard = new Border { Margin = new Thickness(12), Padding = new Thickness(12), Background = Brush.Parse("#202B36"), BorderBrush = Brush.Parse("#34495E"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Child = tipPanel };
        var sidebarBody = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), Background = Brushes.Transparent, Children = { categories, tipCard } }; Grid.SetRow(tipCard, 1);
        var sidebar = new Border { MinWidth = 150, MaxWidth = 340, Background = Brush.Parse("#17212B"), BorderBrush = Brush.Parse("#263746"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(14), ClipToBounds = true, Child = sidebarBody };
        var contentHost = new ScrollViewer { Content = modelPanel }; contentHost.Classes.Add("pry-settings-scroll");
        void ShowSettingsPage(Control page) { contentHost.Content = page; contentHost.Offset = new Vector(0, 0); }
        categories.SelectionChanged += (_, _) => ShowSettingsPage(categories.SelectedIndex switch { 1 => conversationPanel, 2 => shortcutPanel, 3 => desktopPanel, 4 => themePanel, 5 => characterManagerPanel, 6 => stickerManagerPanel, 7 => memoryManagerPanel, _ => modelPanel });
        openBackgroundSettings.Click += (_, _) => ShowSettingsPage(backgroundPage);
        openAvatarSettings.Click += (_, _) => ShowSettingsPage(avatarPage);
        backgroundBack.Click += (_, _) => ShowSettingsPage(themePanel);
        avatarBack.Click += (_, _) => ShowSettingsPage(themePanel);
        var save = new Button { Content = "保存并应用", Classes = { "primary" }, HorizontalAlignment = HorizontalAlignment.Right, Padding = new Thickness(24, 10) };
        var topBarContent = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { new TextBlock { Text = "设置", FontSize = 22, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center }, save } }; Grid.SetColumn(save, 1);
        var topBar = new Border { Background = Brush.Parse("#17212B"), BorderBrush = Brush.Parse("#263746"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(14), Padding = new Thickness(24, 14, 18, 10), Child = topBarContent };
        var splitter = new Border { Width = 12, Margin = new Thickness(0, 0, -6, 0), HorizontalAlignment = HorizontalAlignment.Right, Background = Brushes.Transparent, Cursor = new Cursor(StandardCursorType.SizeWestEast), ZIndex = 20 };
        var layout = new Grid { Margin = new Thickness(10), ColumnDefinitions = new ColumnDefinitions("210,*"), RowDefinitions = new RowDefinitions("Auto,*"), ColumnSpacing = 10, RowSpacing = 10, Background = Brushes.Transparent, Children = { sidebar, splitter, topBar, contentHost } };
        Grid.SetRowSpan(sidebar, 2); Grid.SetColumn(splitter, 0); Grid.SetRowSpan(splitter, 2); Grid.SetColumn(topBar, 1); Grid.SetColumn(contentHost, 1); Grid.SetRow(contentHost, 1);
        var settingsResizeGuide = new Border { Width = 2, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Stretch, Background = ThemeAccentBrush(), IsHitTestVisible = false, IsVisible = false, ZIndex = 30 };
        Grid.SetRowSpan(settingsResizeGuide, 2); Grid.SetColumnSpan(settingsResizeGuide, 2); layout.Children.Add(settingsResizeGuide);
        var resizingSidebar = false; var resizeStartX = 0d; var resizeStartWidth = 210d; var pendingWidth = 210d;
        splitter.PointerPressed += (_, args) =>
        {
            resizingSidebar = true; resizeStartX = args.GetPosition(layout).X; resizeStartWidth = layout.ColumnDefinitions[0].ActualWidth; pendingWidth = resizeStartWidth;
            settingsResizeGuide.RenderTransform = new TranslateTransform(pendingWidth, 0); settingsResizeGuide.IsVisible = liveSidebarResize.IsChecked != true;
            args.Pointer.Capture(splitter); args.Handled = true;
        };
        splitter.PointerMoved += (_, args) =>
        {
            if (!resizingSidebar) return;
            pendingWidth = Math.Clamp(resizeStartWidth + args.GetPosition(layout).X - resizeStartX, 150, 340);
            if (liveSidebarResize.IsChecked == true) layout.ColumnDefinitions[0].Width = new GridLength(pendingWidth);
            else settingsResizeGuide.RenderTransform = new TranslateTransform(pendingWidth, 0);
            args.Handled = true;
        };
        splitter.PointerReleased += (_, args) =>
        {
            if (!resizingSidebar) return;
            resizingSidebar = false; settingsResizeGuide.IsVisible = false;
            if (liveSidebarResize.IsChecked != true) layout.ColumnDefinitions[0].Width = new GridLength(pendingWidth);
            args.Pointer.Capture(null); args.Handled = true;
        };
        Image? settingsBackgroundImage = null; Border? settingsBackgroundDim = null;
        var settingsSurface = CreateThemedDialogSurface(layout, transparentContent: true, (image, dim) => { settingsBackgroundImage = image; settingsBackgroundDim = dim; });
        var window = new Window { Title = "设置", Width = 900, Height = 740, Background = Brushes.Transparent, WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = settingsSurface };
        void StoreCurrentImageDisplays()
        {
            if (loadingImageControls) return;
            if (backgroundDraft.SelectedPath is not null) { var display = ReadDisplay(backgroundFocusX, backgroundFocusY, backgroundZoom); backgroundDraft.StoreDisplay(display); ApplyImageDisplay(backgroundPreview, display); }
            if (avatarDraft.SelectedPath is not null) { var display = ReadDisplay(avatarFocusX, avatarFocusY, avatarZoom); avatarDraft.StoreDisplay(display); ApplyImageDisplay(userAvatarPreview, display); }
        }
        ThemePreferences BuildThemeDraft()
        {
            StoreCurrentImageDisplays();
            return SettingsDraftService.BuildTheme(themePreferences, new ThemeSettingsDraft
            {
                BackgroundImagePath = backgroundDraft.SelectedPath, BackgroundHistory = backgroundDraft.History, BackgroundDisplays = backgroundDraft.Displays,
                UserAvatarPath = avatarDraft.SelectedPath, UserAvatarHistory = avatarDraft.History, UserAvatarDisplays = avatarDraft.Displays,
                ThemeModeIndex = themeMode.SelectedIndex, AccentColor = accentColor.Text?.Trim() ?? "#B148C6",
                UseGlassEffects = glassEffects.IsChecked == true, LiveSidebarResize = liveSidebarResize.IsChecked == true,
                BackgroundImageOpacity = (double)(backgroundOpacity.Value ?? 1m), BackgroundDimOpacity = (double)(backgroundDim.Value ?? .34m),
                BackgroundBlurRadius = (double)(backgroundBlur.Value ?? 0),
                AvatarSize = (double)(avatarSize.Value ?? 48), BubbleFontSize = (double)(bubbleFontSize.Value ?? 14), BubbleMaxWidth = (double)(bubbleMaxWidth.Value ?? 620), BubbleSpacing = (double)(bubbleSpacing.Value ?? 10)
            });
        }
        void UpdateSettingsSurface(ThemePreferences draft)
        {
            var light = draft.ThemeMode == "light" || (draft.ThemeMode == "system" && Application.Current?.ActualThemeVariant == ThemeVariant.Light);
            var hasThemeBackground = !string.IsNullOrWhiteSpace(draft.BackgroundImagePath) && File.Exists(draft.BackgroundImagePath);
            var canvas = Brush.Parse(light ? "#EEF3F7" : "#0E1621");
            var panel = Brush.Parse(hasThemeBackground ? (light ? "#B8E3EBF1" : "#A817212B") : (light ? "#E3EBF1" : "#17212B"));
            var card = Brush.Parse(hasThemeBackground ? (light ? "#B8F8FAFC" : "#B0182533") : (light ? "#F8FAFC" : "#182533"));
            var border = Brush.Parse(light ? "#BDCBD6" : "#263746");
            window.Background = Brushes.Transparent;
            window.TransparencyLevelHint = draft.BackgroundBlurRadius > .5
                ? [WindowTransparencyLevel.Blur, WindowTransparencyLevel.Transparent]
                : [WindowTransparencyLevel.Transparent];
            if (settingsSurface is Grid settingsRoot) settingsRoot.Background = hasThemeBackground ? Brushes.Transparent : canvas;
            if (settingsBackgroundImage is not null)
            {
                settingsBackgroundImage.IsVisible = hasThemeBackground;
                settingsBackgroundImage.Opacity = Math.Clamp(draft.BackgroundImageOpacity, 0, 1);
                if (hasThemeBackground)
                {
                    try
                    {
                        settingsBackgroundImage.Source = _backgroundImages.Get(draft.BackgroundImagePath!, Math.Clamp(draft.BackgroundBlurRadius, 0, 32));
                        var display = draft.BackgroundDisplays.TryGetValue(draft.BackgroundImagePath!, out var savedDisplay) ? savedDisplay : new ImageDisplayPreferences();
                        ApplyImageDisplay(settingsBackgroundImage, display);
                    }
                    catch { settingsBackgroundImage.IsVisible = false; }
                }
            }
            if (settingsBackgroundDim is not null)
            {
                settingsBackgroundDim.IsVisible = hasThemeBackground;
                settingsBackgroundDim.Background = canvas;
                settingsBackgroundDim.Opacity = Math.Clamp(draft.BackgroundDimOpacity, 0, .85);
            }
            layout.Background = Brushes.Transparent; sidebar.Background = panel; sidebar.BorderBrush = border; categories.Background = Brushes.Transparent; topBar.Background = panel; topBar.BorderBrush = border; tipCard.Background = card; tipCard.BorderBrush = border; splitter.Background = Brushes.Transparent;
            foreach (var item in settingCards)
            {
                item.Background = card; item.BorderBrush = border;
            }
            var textBrush = Brush.Parse(light ? "#17212B" : "#F2F6FA"); var mutedBrush = Brush.Parse(light ? "#526678" : "#8EA2B5");
            IEnumerable<Control> Walk(Control root)
            {
                yield return root;
                if (root is Panel panelRoot) foreach (var child in panelRoot.Children) foreach (var nested in Walk(child)) yield return nested;
                else if (root is Decorator decorator && decorator.Child is Control decoratedChild) foreach (var nested in Walk(decoratedChild)) yield return nested;
                else if (root is ContentControl content && content.Content is Control contentChild) foreach (var nested in Walk(contentChild)) yield return nested;
            }
            foreach (var text in Walk(layout).OfType<TextBlock>()) text.Foreground = text.FontSize >= 15 || text.FontWeight == FontWeight.SemiBold ? textBrush : mutedBrush;
            tipValue.Foreground = ThemeAccentBrush();
        }
        using var themePreview = new ThemePreviewController(
            () => Color.TryParse(accentColor.Text, out _) ? BuildThemeDraft() : null,
            draft => { _preferences = _preferences with { Theme = draft }; ApplyTheme(); UpdateSettingsSurface(draft); },
            () => { _preferences = _preferences with { Theme = settingsOriginalTheme }; ApplyTheme(); });
        void PreviewTheme() => themePreview.Request();
        previewTheme = PreviewTheme;
        void PreviewValueChanged(object? _, NumericUpDownValueChangedEventArgs __) { if (!loadingImageControls) PreviewTheme(); }
        themeMode.SelectionChanged += (_, _) => PreviewTheme(); accentColor.TextChanged += (_, _) => PreviewTheme(); glassEffects.IsCheckedChanged += (_, _) => PreviewTheme();
        liveSidebarResize.IsCheckedChanged += (_, _) => PreviewTheme();
        foreach (var number in new[] { backgroundOpacity, backgroundBlur, backgroundDim, avatarSize, bubbleFontSize, bubbleMaxWidth, bubbleSpacing, backgroundFocusX, backgroundFocusY, backgroundZoom, avatarFocusX, avatarFocusY, avatarZoom }) number.ValueChanged += PreviewValueChanged;
        window.Closed += (_, _) => themePreview.Dispose();
        UpdateSettingsSurface(themePreferences);
        async Task<string?> PickThemeImageAsync(string title, string category)
        {
            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = title, AllowMultiple = false, FileTypeFilter = [new FilePickerFileType("图片") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp"] }] });
            var path = files.FirstOrDefault()?.TryGetLocalPath(); return string.IsNullOrWhiteSpace(path) ? null : path;
        }
        browseBackground.Click += async (_, _) => { var path = await PickThemeImageAsync("导入聊天背景", "backgrounds"); if (path is null) return; backgroundDraft.Select(path, ReadDisplay(backgroundFocusX, backgroundFocusY, backgroundZoom)); LoadDisplay(path, backgroundDraft.Displays, backgroundFocusX, backgroundFocusY, backgroundZoom, backgroundPreview); RefreshBackgroundGallery(); PreviewTheme(); };
        clearBackground.Click += (_, _) => { backgroundDraft.ClearSelection(ReadDisplay(backgroundFocusX, backgroundFocusY, backgroundZoom)); themeImages.Clear(backgroundPreview); RefreshBackgroundGallery(); PreviewTheme(); };
        deleteBackground.Click += async (_, _) =>
        {
            if (backgroundDraft.SelectedPath is null || !await ConfirmAsync(window, "删除背景", "确定从当前背景列表移除此图片吗？原始文件不会被删除，保存后生效。")) return;
            backgroundDraft.RemoveSelected();
            themeImages.Clear(backgroundPreview); RefreshBackgroundGallery(); PreviewTheme();
        };
        browseUserAvatar.Click += async (_, _) => { var path = await PickThemeImageAsync("导入用户头像", "user-avatars"); if (path is null) return; avatarDraft.Select(path, ReadDisplay(avatarFocusX, avatarFocusY, avatarZoom)); LoadDisplay(path, avatarDraft.Displays, avatarFocusX, avatarFocusY, avatarZoom, userAvatarPreview); RefreshUserAvatarGallery(); PreviewTheme(); };
        clearUserAvatar.Click += (_, _) => { avatarDraft.ClearSelection(ReadDisplay(avatarFocusX, avatarFocusY, avatarZoom)); themeImages.Clear(userAvatarPreview); RefreshUserAvatarGallery(); PreviewTheme(); };
        deleteUserAvatar.Click += async (_, _) =>
        {
            if (avatarDraft.SelectedPath is null || !await ConfirmAsync(window, "删除用户头像", "确定从当前头像列表移除此图片吗？原始文件不会被删除，保存后生效。")) return;
            avatarDraft.RemoveSelected();
            themeImages.Clear(userAvatarPreview); RefreshUserAvatarGallery(); PreviewTheme();
        };
        characterManagerButton.Click += async (_, _) => await OpenCharacterEditorAsync(window);
        stickerManagerButton.Click += async (_, _) => await OpenStickerManagerAsync(window);
        memoryManagerButton.Click += async (_, _) => await OpenMemoryManagerAsync(window);
        modelSelection.ManageModels.Click += async (_, _) =>
        {
            tuningEditor.SaveCurrent();
            var selectedParameterId = tuningEditor.CurrentModelId;
            await ManageCustomModelsAsync(window);
            allModels = _builtInProfiles.Concat(_preferences.CustomModels).ToArray();
            modelSelection.RefreshModels(allModels);
            tuningEditor.UpdateProfiles(allModels, selectedParameterId);
        };
        modelSelection.ManageSpeech.Click += async (_, _) =>
        {
            await ManageSpeechModelsAsync(window);
            modelSelection.RefreshSpeech(_builtInSpeechProfiles.Concat(_preferences.CustomSpeechModels).ToArray(), GetSpeechModelId());
        };
        save.Click += async (_, _) =>
        {
            var turnOverride = conversationEditor.BuildDraft();
            var shortcutDraft = shortcutEditor.BuildDraft();
            var validationError = SettingsDraftService.Validate(turnOverride.MinReplyMessages,
                turnOverride.MaxReplyMessages, turnOverride.TypingStyle.MinDelayMs,
                turnOverride.TypingStyle.MaxDelayMs, accentColor.Text)
                ?? SettingsDraftService.ValidateShortcuts(shortcutDraft);
            if (validationError is not null) { await ShowNoticeAsync(validationError.Title, validationError.Message); return; }
            var tuningDrafts = tuningEditor.BuildDrafts(); var selectedModelId = modelSelection.TextModelId ?? GetActiveModelId(); var selectedVisionId = modelSelection.VisionModelId; var selectedSpeechId = modelSelection.SpeechModelId; var modelTuning = tuningDrafts.TryGetValue(selectedModelId, out var selectedTuning) ? selectedTuning with { ActiveModelId = selectedModelId } : new ModelTuningPreferences { ActiveModelId = selectedModelId };
            var candidate = _preferences with { ActiveModelId = selectedModelId, ActiveVisionModelId = selectedVisionId, ActiveSpeechModelId = selectedSpeechId, ModelTunings = new Dictionary<string, ModelTuningPreferences>(tuningDrafts), EnableThinking = modelTuning.EnableThinking == true, ModelTuning = modelTuning, TurnTakingOverride = turnOverride, SelectedCharacterId = _character?.Id, DesktopPet = desktopPetEditor.BuildDraft(), Theme = BuildThemeDraft(), Shortcuts = shortcutDraft };
            save.IsEnabled = false; _turnManager?.CancelAgentReply();
            try
            {
                RuntimeStatus.Text = "正在由后端切换模型…";
                await _api.UpdatePreferencesAsync(new UpdateClientPreferencesRequest(
                    candidate.SelectedCharacterId, _conversationId, candidate.UserProfile, candidate.DesktopPet,
                    candidate.Shortcuts, turnOverride, new ClientThemePreferences(candidate.Theme.ThemeMode,
                        candidate.Theme.AccentColor, candidate.Theme.UseGlassEffects, candidate.Theme.LiveSidebarResize,
                        candidate.Theme.BackgroundDimOpacity, candidate.Theme.BackgroundImageOpacity,
                        candidate.Theme.BackgroundBlurMode, candidate.Theme.BackgroundBlurRadius,
                        candidate.Theme.AvatarSize, candidate.Theme.BubbleFontSize, candidate.Theme.BubbleMaxWidth,
                        candidate.Theme.BubbleSpacing)));
                async Task<string?> UploadAppearanceAsync(string? path)
                {
                    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
                    await using var content = File.OpenRead(path);
                    var uploaded = await _api.UploadAsync(content, Path.GetFileName(path), ImageContentType(path));
                    foreach (var warning in uploaded.Warnings) RuntimeStatus.Text = warning;
                    return uploaded.Id;
                }
                var backgroundMediaId = await UploadAppearanceAsync(backgroundDraft.SelectedPath);
                var userAvatarMediaId = await UploadAppearanceAsync(avatarDraft.SelectedPath);
                await _api.UpdateAppearanceAsync(new UpdateAppearanceMediaRequest(backgroundMediaId,
                    backgroundDraft.SelectedPath is null, userAvatarMediaId, avatarDraft.SelectedPath is null,
                    backgroundDraft.SelectedPath is null ? null : ReadDisplay(backgroundFocusX, backgroundFocusY, backgroundZoom),
                    avatarDraft.SelectedPath is null ? null : ReadDisplay(avatarFocusX, avatarFocusY, avatarZoom)));
                var backendModels = await _api.UpdateModelSelectionAsync(new UpdateModelSelectionRequest(
                    selectedModelId, selectedVisionId, selectedSpeechId, candidate.ModelTunings));
                _preferences = candidate;
                _profiles = _builtInProfiles.Concat(candidate.CustomModels).ToArray();
                ApplyTheme(); await ReloadActiveConversationAsync();
                RuntimeStatus.Text = $"后端模型配置已保存 · {backendModels.First(x => x.SelectedForText).DisplayName}";
                themePreview.Commit(); window.Close();
            }
            catch (Exception ex)
            {
                save.IsEnabled = true; RuntimeStatus.Text = "后端拒绝了模型配置"; await ShowNoticeAsync("模型切换失败", ex.Message);
            }
        };
        await window.ShowDialog(this);
    }

    private async Task ManageSpeechModelsAsync(Window owner)
    {
        var models = _preferences.CustomSpeechModels.ToList();
        var window = new Window { Title = "自定义语音识别模型", Width = 760, Height = 560, Background = Brushes.Transparent, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var list = new ListBox { Background = Brush.Parse("#17212B"), Margin = new Thickness(0, 10, 0, 10) };
        void Refresh() => list.ItemsSource = models.Select(x => new ModelChoice(x.Id, $"{x.DisplayName} · {x.Provider}")).ToArray();
        Refresh();
        var add = new Button { Content = "添加" }; var edit = new Button { Content = "编辑" }; var remove = new Button { Content = "删除" }; var done = new Button { Content = "完成", Classes = { "primary" } };
        async Task<SpeechModelProfile?> EditAsync(SpeechModelProfile? source)
        {
            var name = new TextBox { Text = source?.DisplayName ?? "我的语音模型" };
            var provider = new ComboBox { ItemsSource = new[] { "sherpa-onnx", "openai-compatible" }, SelectedItem = source?.Provider ?? "sherpa-onnx" };
            var modelName = new TextBox { Text = source?.ModelName ?? "whisper-1" }; var path = new TextBox { Text = source?.ModelPath ?? "" };
            var baseUrl = new TextBox { Text = source?.BaseUrl ?? "" }; var language = new TextBox { Text = source?.Language ?? "zh" };
            var sampleRate = new NumericUpDown { Value = source?.SampleRate ?? 16000, Minimum = 8000, Maximum = 192000, Increment = 1000 };
            var save = new Button { Content = "保存", Classes = { "primary" }, HorizontalAlignment = HorizontalAlignment.Right };
            var panel = new StackPanel { Margin = new Thickness(26), Spacing = 8 };
            void Field(string label, Control control) { panel.Children.Add(new TextBlock { Text = label, Foreground = Brush.Parse("#B8CCE0") }); panel.Children.Add(control); }
            Field("显示名称", name); Field("类型", provider); Field("API model 名称", modelName); Field("本地模型目录", path); Field("转写 API 地址", baseUrl); Field("语言", language); Field("采样率", sampleRate);
            panel.Children.Add(new TextBlock { Text = "本地模型填写目录；API 模型填写 OpenAI-compatible 地址。密钥通过 PRY_SPEECH_API_KEY_<模型ID> 环境变量提供。", TextWrapping = TextWrapping.Wrap, Foreground = Brush.Parse("#7F91A4") }); panel.Children.Add(save);
            var editor = new Window { Title = source is null ? "添加语音模型" : "编辑语音模型", Width = 620, Height = 650, Background = Brushes.Transparent, WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = CreateThemedDialogSurface(new ScrollViewer { Content = panel }) };
            SpeechModelProfile? result = null;
            save.Click += (_, _) => { if (string.IsNullOrWhiteSpace(name.Text)) return; result = new SpeechModelProfile { Id = source?.Id ?? $"speech-{Guid.NewGuid():N}", DisplayName = name.Text.Trim(), Provider = provider.SelectedItem?.ToString() ?? "sherpa-onnx", ModelName = modelName.Text?.Trim() ?? "whisper-1", ModelPath = path.Text?.Trim() ?? "", BaseUrl = baseUrl.Text?.Trim() ?? "", Language = language.Text?.Trim() ?? "zh", SampleRate = (int)(sampleRate.Value ?? 16000) }; editor.Close(); };
            await editor.ShowDialog(owner); return result;
        }
        static SaveSpeechModelRequest SpeechRequest(SpeechModelProfile value) => new(value.DisplayName,
            value.Provider, value.ModelName, value.ModelPath, value.BaseUrl, value.Language, value.SampleRate);
        add.Click += async (_, _) =>
        {
            var value = await EditAsync(null); if (value is null) return;
            var saved = await _api.CreateSpeechModelAsync(SpeechRequest(value));
            models.Add(value with { Id = saved.Id }); Refresh();
        };
        edit.Click += async (_, _) =>
        {
            if (list.SelectedItem is not ModelChoice choice) return;
            var index = models.FindIndex(x => x.Id == choice.Id); if (index < 0) return;
            var value = await EditAsync(models[index]); if (value is null) return;
            await _api.UpdateSpeechModelAsync(choice.Id, SpeechRequest(value)); models[index] = value; Refresh();
        };
        remove.Click += async (_, _) => { if (list.SelectedItem is not ModelChoice choice || !await ConfirmAsync(window, "删除语音模型配置", $"确定删除“{choice.Name}”吗？本地模型文件不会被删除。")) return; await _api.DeleteSpeechModelAsync(choice.Id); models.RemoveAll(x => x.Id == choice.Id); Refresh(); };
        done.Click += (_, _) => { _preferences = _preferences with { CustomSpeechModels = models.ToArray() }; window.Close(); };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Children = { add, edit, remove, done } };
        var speechLayout = new Grid { Margin = new Thickness(22), RowDefinitions = new RowDefinitions("Auto,*,Auto"), Children = { new TextBlock { Text = "保留多个本地或 API 语音识别配置，在主设置页选择当前使用项。", TextWrapping = TextWrapping.Wrap }, list, buttons } }; Grid.SetRow(list, 1); Grid.SetRow(buttons, 2); window.Content = CreateThemedDialogSurface(speechLayout);
        await window.ShowDialog(owner);
    }

    private async void ManageStickers_Click(object? sender, RoutedEventArgs e) => await OpenStickerManagerAsync();

    private async Task OpenStickerManagerAsync(Window? owner = null)
    {
        var window = new Window { Title = "管理表情包", Width = 760, Height = 570, Background = Brushes.Transparent, WindowStartupLocation = WindowStartupLocation.CenterOwner }; var grid = new WrapPanel { Orientation = Orientation.Horizontal, ItemWidth = 112, ItemHeight = 132 }; StickerDefinition? selected = null;
        var import = new Button { Content = "导入" }; var edit = new Button { Content = "编辑" }; var remove = new Button { Content = "删除" }; var close = new Button { Content = "完成" };
        void Refresh()
        {
            grid.Children.Clear();
            foreach (var sticker in _stickers)
            {
                Control preview; try { preview = new Image { Source = new Bitmap(sticker.FilePath), Width = 88, Height = 88, Stretch = Stretch.Uniform }; } catch { preview = new TextBlock { Text = "无法预览", Width = 88, Height = 88 }; }
                var button = new Button { Width = 104, Height = 124, Padding = new Thickness(5), BorderBrush = sticker.Id == selected?.Id ? ThemeAccentBrush() : Brush.Parse("#344452"), BorderThickness = new Thickness(sticker.Id == selected?.Id ? 2 : 1), Content = new StackPanel { Spacing = 4, Children = { preview, new TextBlock { Text = sticker.Name, MaxWidth = 92, TextTrimming = TextTrimming.CharacterEllipsis, HorizontalAlignment = HorizontalAlignment.Center }, new TextBlock { Text = sticker.Source == StickerSource.BuiltIn ? "内置" : sticker.InteractionRole, FontSize = 10, Foreground = Brush.Parse("#7F91A4"), HorizontalAlignment = HorizontalAlignment.Center } } } };
                button.Click += (_, _) => { selected = sticker; Refresh(); }; grid.Children.Add(button);
            }
        }
        Refresh();
        import.Click += async (_, _) =>
        {
            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "导入表情包", AllowMultiple = true, FileTypeFilter = [new FilePickerFileType("表情图片") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.gif"] }] });
            foreach (var file in files)
            {
                var path = file.TryGetLocalPath(); if (path is null) continue;
                await using var content = File.OpenRead(path);
                var media = await _api.UploadAsync(content, Path.GetFileName(path), ImageContentType(path));
                await _api.ImportStickerAsync(media.Id, Path.GetFileNameWithoutExtension(path), []);
            }
            await ReloadStickerProjectionAsync(); Refresh();
        };
        remove.Click += async (_, _) => { if (selected is { Source: StickerSource.User } && await ConfirmAsync(window, "删除表情", $"确定删除“{selected.Name}”吗？这会同时删除应用数据目录里的副本。")) { await _api.DeleteStickerAsync(selected.Id); await ReloadStickerProjectionAsync(); selected = null; Refresh(); } };
        edit.Click += async (_, _) => { if (selected is { Source: StickerSource.User }) { await EditStickerAsync(window, selected); selected = _stickers.FirstOrDefault(x => x.Id == selected.Id); Refresh(); } }; close.Click += (_, _) => window.Close();
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 9, HorizontalAlignment = HorizontalAlignment.Right, Children = { import, edit, remove, close } };
        var scroll = new ScrollViewer { Content = grid, Background = Brushes.Transparent };
        var stickerCard = CreateCompactDialogTextCard(scroll); stickerCard.Padding = new Thickness(12);
        var root = new Grid { Margin = new Thickness(20), RowDefinitions = new RowDefinitions("Auto,*,Auto"), Background = Brushes.Transparent, RowSpacing = 12, Children = { new TextBlock { Text = "点击缩略图选中表情。你和角色都可以使用启用的表情，互动作用会参与打断判断。", TextWrapping = TextWrapping.Wrap }, stickerCard, buttons } };
        window.Content = CreateThemedDialogSurface(root); Grid.SetRow(stickerCard, 1); Grid.SetRow(buttons, 2); await window.ShowDialog(owner ?? this);
    }

    private async Task ManageCustomModelsAsync(Window owner)
    {
        var models = _preferences.CustomModels.ToList();
        var window = new Window { Title = "自定义模型", Width = 700, Height = 500, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var list = new ListBox(); void Refresh() => list.ItemsSource = models.Select(x => new ModelChoice(x.Id, $"{x.DisplayName} · {x.Provider} · {x.ModelName}")).ToArray(); Refresh();
        var add = new Button { Content = "添加" }; var edit = new Button { Content = "编辑" }; var remove = new Button { Content = "删除" }; var done = new Button { Content = "完成", Classes = { "primary" } };
        async Task<ModelProfile?> EditModelAsync(ModelProfile? source)
        {
            var name = new TextBox { Text = source?.DisplayName ?? "我的模型" }; var provider = new ComboBox { ItemsSource = new[] { "local-llama", "openai-compatible" }, SelectedItem = source?.Provider ?? "local-llama" };
            var modelName = new TextBox { Text = source?.ModelName ?? "model" }; var baseUrl = new TextBox { Text = source?.BaseUrl ?? "http://127.0.0.1:8080/v1" }; var modelPath = new TextBox { Text = source?.ModelPath ?? "" }; var mmproj = new TextBox { Text = source?.MmprojPath ?? "" }; var vision = new CheckBox { Content = "支持图片输入", IsChecked = source?.Capabilities.Vision == true };
            var save = new Button { Content = "保存", Classes = { "primary" }, HorizontalAlignment = HorizontalAlignment.Right }; var editor = new Window { Title = source is null ? "添加自定义模型" : "编辑自定义模型", Width = 580, Height = 610, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var panel = new StackPanel { Margin = new Thickness(24), Spacing = 8 }; void Field(string label, Control control) { panel.Children.Add(new TextBlock { Text = label }); panel.Children.Add(control); }
            Field("显示名称", name); Field("类型", provider); Field("模型名称 / API model", modelName); Field("OpenAI-compatible 地址", baseUrl); Field("GGUF 路径（在线模型可留空）", modelPath); Field("多模态 mmproj 路径（可留空）", mmproj); panel.Children.Add(vision); panel.Children.Add(new TextBlock { Text = "在线 API 密钥通过环境变量 PRY_API_KEY_<模型ID> 提供，避免把密钥明文写进角色与偏好文件。", TextWrapping = TextWrapping.Wrap, Foreground = Brush.Parse("#7F91A4") }); panel.Children.Add(save); editor.Content = CreateThemedDialogSurface(new ScrollViewer { Content = panel });
            ModelProfile? result = null; save.Click += (_, _) => { if (string.IsNullOrWhiteSpace(name.Text) || string.IsNullOrWhiteSpace(modelName.Text) || string.IsNullOrWhiteSpace(baseUrl.Text)) return; result = new ModelProfile { Id = source?.Id ?? $"custom-{Guid.NewGuid():N}", DisplayName = name.Text.Trim(), Provider = provider.SelectedItem?.ToString() ?? "local-llama", ModelName = modelName.Text.Trim(), BaseUrl = baseUrl.Text.Trim(), ModelPath = string.IsNullOrWhiteSpace(modelPath.Text) ? null : modelPath.Text.Trim(), MmprojPath = string.IsNullOrWhiteSpace(mmproj.Text) ? null : mmproj.Text.Trim(), Capabilities = new ModelCapabilities(Text: true, Vision: vision.IsChecked == true), ContextSize = source?.ContextSize ?? 4096, MaxOutputTokens = source?.MaxOutputTokens ?? 512, Temperature = source?.Temperature ?? .8, GpuLayers = source?.GpuLayers is > 0 ? source.GpuLayers : 999, ComputeDevice = source?.ComputeDevice ?? "auto-discrete", EnableThinking = source?.EnableThinking ?? true }; editor.Close(); };
            await editor.ShowDialog(owner); return result;
        }
        static SaveCustomModelRequest ModelRequest(ModelProfile value) => new(value.DisplayName, value.Provider,
            value.ModelName, value.BaseUrl, value.ModelPath, value.MmprojPath, value.Capabilities, value.ContextSize,
            value.MaxOutputTokens, value.Temperature, value.GpuLayers, value.ComputeDevice, value.EnableThinking);
        add.Click += async (_, _) =>
        {
            var result = await EditModelAsync(null); if (result is null) return;
            var saved = await _api.CreateCustomModelAsync(ModelRequest(result));
            models.Add(result with { Id = saved.Id }); Refresh();
        };
        edit.Click += async (_, _) =>
        {
            if (list.SelectedItem is not ModelChoice choice) return;
            var index = models.FindIndex(x => x.Id == choice.Id); if (index < 0) return;
            var result = await EditModelAsync(models[index]); if (result is null) return;
            await _api.UpdateCustomModelAsync(choice.Id, ModelRequest(result)); models[index] = result; Refresh();
        };
        remove.Click += async (_, _) => { if (list.SelectedItem is not ModelChoice choice || !await ConfirmAsync(window, "删除自定义模型", $"确定从配置中删除“{choice.Name}”吗？模型文件不会被删除。")) return; await _api.DeleteCustomModelAsync(choice.Id); models.RemoveAll(x => x.Id == choice.Id); Refresh(); };
        done.Click += (_, _) => { _preferences = _preferences with { CustomModels = models.ToArray() }; window.Close(); };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Children = { add, edit, remove, done } }; var modelLayout = new Grid { Margin = new Thickness(20), RowDefinitions = new RowDefinitions("Auto,*,Auto"), Children = { new TextBlock { Text = "内置模型不会出现在这里；自定义模型可长期保存多个。", TextWrapping = TextWrapping.Wrap }, list, buttons } }; window.Content = CreateThemedDialogSurface(modelLayout); Grid.SetRow(list, 1); Grid.SetRow(buttons, 2); await window.ShowDialog(owner);
    }

    private async Task EditStickerAsync(Window owner, StickerDefinition sticker)
    {
        var name = new TextBox { Text = sticker.Name }; var tags = new TextBox { Text = string.Join("，", sticker.Emotions) }; var role = new ComboBox { ItemsSource = new[] { "reaction", "backchannel", "topic" }, SelectedItem = sticker.InteractionRole }; var backchannel = new CheckBox { Content = "这个表情通常表示“我在听”", IsChecked = sticker.LikelyBackchannel }; var save = new Button { Content = "保存", HorizontalAlignment = HorizontalAlignment.Right };
        var editor = new Window { Title = "编辑表情", Width = 480, Height = 430, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        save.Click += async (_, _) => { var values = (tags.Text ?? "").Split(['，', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); await _api.UpdateStickerAsync(sticker.Id, new UpdateStickerRequest(name.Text ?? sticker.Name, values, role.SelectedItem?.ToString() ?? "reaction", backchannel.IsChecked == true)); await ReloadStickerProjectionAsync(); editor.Close(); };
        var form = new StackPanel { Spacing = 10, Children = { new TextBlock { Text = "名称" }, name, new TextBlock { Text = "情绪标签" }, tags, new TextBlock { Text = "互动作用" }, role, backchannel } };
        editor.Content = CreateThemedDialogSurface(CreateCompactDialogLayout(CreateCompactDialogTextCard(form), save)); await editor.ShowDialog(owner);
    }

    private Task ShowNoticeAsync(string title, string message) => _dialogs.ShowNoticeAsync(this, title, message);
    private Task<bool> ConfirmAsync(Window owner, string title, string message) => _dialogs.ConfirmAsync(owner, title, message);
    private sealed record StickerListItem(StickerDefinition Sticker) { public override string ToString() => $"{Sticker.Name} · {(Sticker.Source == StickerSource.BuiltIn ? "内置" : "用户")} · {Sticker.InteractionRole} · {string.Join(" / ", Sticker.Emotions)}"; }
    private sealed record MemoryListItem(MemoryRecord Memory)
    {
        public override string ToString() => $"{MemoryKindLabel(Memory.Kind)} · {Memory.Summary}";
        private static string MemoryKindLabel(string kind) => kind switch
        {
            "user_fact" => "事实", "preference" => "偏好", "promise" => "约定",
            "relationship" => "关系", "event" => "事件", _ => "其他"
        };
    }
    private IBrush ThemeAccentBrush()
    {
        var configured = _preferences.Theme?.AccentColor;
        return new SolidColorBrush(Color.TryParse(configured, out var color)
            ? color
            : Application.Current?.PlatformSettings?.GetColorValues().AccentColor1 ?? Color.Parse("#B148C6"));
    }

    private IBrush AssistantBubbleBrush()
    {
        var mode = _preferences.Theme?.ThemeMode ?? "system";
        var light = mode.Equals("light", StringComparison.OrdinalIgnoreCase)
                    || (mode.Equals("system", StringComparison.OrdinalIgnoreCase) && Application.Current?.ActualThemeVariant == ThemeVariant.Light);
        return Brush.Parse(light ? "#D9E5EE" : "#182533");
    }

    private IBrush ThemeTextBrush()
    {
        var mode = _preferences.Theme?.ThemeMode ?? "system";
        var light = mode.Equals("light", StringComparison.OrdinalIgnoreCase)
                    || (mode.Equals("system", StringComparison.OrdinalIgnoreCase) && Application.Current?.ActualThemeVariant == ThemeVariant.Light);
        return Brush.Parse(light ? "#17212B" : "#F2F6FA");
    }
    private sealed record ModelChoice(string Id, string Name) { public override string ToString() => Name; }
    private sealed record CharacterChoice(string Id, string Name) { public override string ToString() => Name; }
    private sealed record ConversationRoomChoice(ConversationRoom Room)
    {
        public override string ToString() => $"{Room.Title}\n{Room.UpdatedAt.LocalDateTime:MM-dd HH:mm} · {Room.MessageCount} 条消息";
    }
}
