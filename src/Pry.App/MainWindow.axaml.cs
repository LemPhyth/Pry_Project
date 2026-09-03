using System.Text.Json;
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
using SkiaSharp;
using Pry.Client;
using Pry.Contracts;
using Pry.App.Services;

namespace Pry.App;

public sealed partial class MainWindow : Window
{
    private readonly PryBackendClient _api;
    private readonly string _appDirectory = AppContext.BaseDirectory;
    private readonly string _dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PryCompanion");
    private string _conversationId = Guid.NewGuid().ToString("N");
    private readonly List<ChatAttachment> _attachments = [];
    private readonly List<(Border Control, bool IsUser)> _messageAvatarControls = [];
    private readonly List<(Border Control, bool IsUser)> _messageBubbleControls = [];
    private readonly List<(TextBlock Control, bool IsUser)> _bubbleTextControls = [];
    private readonly Dictionary<string, Bitmap> _backgroundRenderCache = new(StringComparer.OrdinalIgnoreCase);
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
    private AppSettings? _settings;
    private UserPreferences _preferences = new();
    private ModelProfile[] _profiles = [];
    private StickerCatalog? _stickerCatalog;
    private RemoteConversationTurnManager? _turnManager;
    private WaveInEvent? _waveInput;
    private WaveFileWriter? _waveWriter;
    private TaskCompletionSource? _recordingStopped;
    private string? _recordingPath;
    private bool _speechBusy;
    private bool _allowClose;
    private string PreferencesPath => Path.Combine(_dataDirectory, "preferences.json");
    private string CharacterPath => Path.Combine(_dataDirectory, "character.json");
    private string CharacterDirectory => Path.Combine(_dataDirectory, "characters");
    private string ThemeMediaDirectory => Path.Combine(_dataDirectory, "theme-media");
    private string CharacterMediaDirectory => Path.Combine(_dataDirectory, "character-media");

    public MainWindow() : this(new PryBackendClient(new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5078/"), Timeout = Timeout.InfiniteTimeSpan })) { }

    public MainWindow(PryBackendClient api)
    {
        _api = api;
        InitializeComponent();
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
        foreach (var bitmap in _backgroundRenderCache.Values) bitmap.Dispose();
        _backgroundRenderCache.Clear();
    }

    private async Task InitializeRuntimeAsync()
    {
        try
        {
            Directory.CreateDirectory(_dataDirectory);
            var resources = Path.Combine(_appDirectory, "Resources");
            _settings = await JsonConfiguration.LoadAsync<AppSettings>(Path.Combine(resources, "appsettings.json"));
            if (File.Exists(PreferencesPath)) _preferences = await JsonConfiguration.LoadAsync<UserPreferences>(PreferencesPath);
            if (!string.IsNullOrWhiteSpace(_preferences.ActiveConversationId)) _conversationId = _preferences.ActiveConversationId;
            await LoadCharactersAsync(Path.Combine(resources, "character.json"));
            await MigrateLegacyMediaAsync();
            ApplyTheme();
            if (!await _api.IsHealthyAsync()) throw new InvalidOperationException("本地后端未就绪。");
            try { _ = await _api.GetConversationAsync(_conversationId); }
            catch (PryBackendException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var created = await _api.CreateConversationAsync(_character?.Id); _conversationId = created.Id;
                _preferences = _preferences with { ActiveConversationId = _conversationId }; await JsonConfiguration.SaveAsync(PreferencesPath, _preferences);
            }
            _stickerCatalog = new StickerCatalog(Path.Combine(resources, "Stickers", "manifest.json"), Path.Combine(_dataDirectory, "stickers")); await _stickerCatalog.LoadAsync();
            var profiles = BuildProfiles(_preferences); _profiles = profiles;
            var activeId = GetActiveModelId();
            var visionId = GetVisionModelId();
            CreateTurnManager(); RefreshCharacterSelector(); CharacterName.Text = _character!.Name; await RefreshConversationRoomsAsync();
            var runtime = await _api.GetRuntimeAsync(); RuntimeStatus.Text = runtime.State == "ready" ? "本地后端已就绪" : $"后端状态：{runtime.State}";
            var existingMessages = await _api.GetMessagesAsync(_conversationId, 200);
            if (existingMessages.Count == 0) AddTextBubble(_character.Name, _character.Greeting, false); else RenderConversationMessages(existingMessages);
        }
        catch (Exception ex) { RuntimeStatus.Text = $"初始化失败：{ex.Message}"; AddTextBubble("系统", ex.Message, false); }
    }

    private async Task LoadCharactersAsync(string bundledPath)
    {
        Directory.CreateDirectory(CharacterDirectory); _characters.Clear();
        async Task AddOrReplaceAsync(string path)
        {
            var loaded = await JsonConfiguration.LoadAsync<CharacterDefinition>(path); JsonConfiguration.Validate(loaded);
            var card = NormalizeCharacterNaming(loaded);
            if (card != loaded && !Path.GetFullPath(path).Equals(Path.GetFullPath(bundledPath), StringComparison.OrdinalIgnoreCase)) await JsonConfiguration.SaveAsync(path, card);
            _characters.RemoveAll(x => x.Id == card.Id); _characters.Add(card);
        }
        await AddOrReplaceAsync(bundledPath);
        if (File.Exists(CharacterPath)) await AddOrReplaceAsync(CharacterPath);
        foreach (var path in Directory.EnumerateFiles(CharacterDirectory, "*.json")) await AddOrReplaceAsync(path);
        _character = _characters.FirstOrDefault(x => x.Id == _preferences.SelectedCharacterId) ?? _characters[0];
    }

    private static CharacterDefinition NormalizeCharacterNaming(CharacterDefinition card)
    {
        if (!string.IsNullOrWhiteSpace(card.CardName)) return card;
        var separator = card.Name.IndexOf('-');
        return separator > 0
            ? card with { CardName = card.Name, Name = card.Name[..separator].Trim() }
            : card with { CardName = $"{card.Name}-正式-v1" };
    }

    private async Task MigrateLegacyMediaAsync()
    {
        var theme = _preferences.Theme ?? new ThemePreferences();
        var migratedPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? Migrate(string? source, string category)
        {
            if (string.IsNullOrWhiteSpace(source) || !File.Exists(source)) return null;
            if (migratedPaths.TryGetValue($"{category}|{source}", out var existing)) return existing;
            var targetDirectory = Path.Combine(category == "characters" ? CharacterMediaDirectory : ThemeMediaDirectory, category);
            var result = IsPathInside(source, targetDirectory) ? source : CacheManagedImage(source, category);
            migratedPaths[$"{category}|{source}"] = result; return result;
        }
        List<string> MigrateMany(IEnumerable<string?> sources, string category) => sources
            .Select(x => Migrate(x, category)).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var backgrounds = MigrateMany(theme.BackgroundHistory.Append(theme.BackgroundImagePath), "backgrounds");
        var userAvatars = MigrateMany(theme.UserAvatarHistory.Append(theme.UserAvatarPath), "user-avatars");
        var selectedBackground = Migrate(theme.BackgroundImagePath, "backgrounds");
        var selectedUserAvatar = Migrate(theme.UserAvatarPath, "user-avatars");
        var legacyCharacterAvatar = Migrate(theme.CharacterAvatarPath, "characters");
        if (_character is not null && string.IsNullOrWhiteSpace(_character.AvatarPath) && legacyCharacterAvatar is not null)
        {
            _character = _character with { AvatarPath = legacyCharacterAvatar };
            _characters.RemoveAll(x => x.Id == _character.Id); _characters.Add(_character);
            await JsonConfiguration.SaveAsync(Path.Combine(CharacterDirectory, _character.Id + ".json"), _character);
        }
        for (var index = 0; index < _characters.Count; index++)
        {
            var card = _characters[index]; var migrated = Migrate(card.AvatarPath, "characters");
            if (migrated is null || migrated == card.AvatarPath) continue;
            card = card with { AvatarPath = migrated }; _characters[index] = card;
            if (_character?.Id == card.Id) _character = card;
            await JsonConfiguration.SaveAsync(Path.Combine(CharacterDirectory, card.Id + ".json"), card);
        }
        _preferences = _preferences with { Theme = theme with { BackgroundImagePath = selectedBackground, BackgroundHistory = backgrounds, UserAvatarPath = selectedUserAvatar, UserAvatarHistory = userAvatars, CharacterAvatarPath = null } };
        await JsonConfiguration.SaveAsync(PreferencesPath, _preferences);
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
        _preferences = _preferences with { SelectedCharacterId = selected.Id, ActiveConversationId = _conversationId }; await JsonConfiguration.SaveAsync(PreferencesPath, _preferences); await RefreshConversationRoomsAsync();
    }

    private void CreateTurnManager()
    {
        if (_character is null || _settings is null || _stickerCatalog is null) return;
        if (_turnManager is not null) _ = _turnManager.DisposeAsync();
        _turnManager = new RemoteConversationTurnManager(_api, _conversationId);
        _turnManager.AgentMessageDelivered += message => Dispatcher.UIThread.InvokeAsync(() => AddAgentMessage(message)).GetTask();
        _turnManager.StateChanged += state => Dispatcher.UIThread.Post(() => TurnStatus.Text = state switch { TurnState.UserPending => "等你把话说完…", TurnState.ModelThinking => "正在想…", TurnState.AgentPending => "正在输入…", TurnState.AgentSending => "正在发送…（Esc 可打断）", _ => "准备就绪" });
        _turnManager.Failed += ex => Dispatcher.UIThread.Post(() => { RuntimeStatus.Text = "模型连接失败"; AddTextBubble("系统", $"暂时无法回应：{ex.Message}", false); });
        _turnManager.Warning += warning => Dispatcher.UIThread.Post(() => RuntimeStatus.Text = warning);
    }

    private ModelProfile ResolveProfile(ModelProfile profile)
    {
        string? Resolve(string? value) => string.IsNullOrWhiteSpace(value) || Path.IsPathRooted(value) ? value : ResolveBundledPath(value) ?? Path.GetFullPath(Path.Combine(_appDirectory, value));
        return profile with { ModelPath = Resolve(profile.ModelPath), MmprojPath = Resolve(profile.MmprojPath), ApiKey = Environment.GetEnvironmentVariable($"PRY_API_KEY_{profile.Id.Replace('-', '_').ToUpperInvariant()}") };
    }

    private string GetActiveModelId(UserPreferences? preferences = null)
    {
        var value = preferences ?? _preferences;
        return value.ActiveModelId ?? value.ModelTuning.ActiveModelId ?? _settings?.ActiveModelId ?? "qwen3-1.7b-local";
    }

    private string? GetVisionModelId(UserPreferences? preferences = null) => (preferences ?? _preferences).ActiveVisionModelId ?? _settings?.VisionModelId;
    private string? GetSpeechModelId(UserPreferences? preferences = null) => (preferences ?? _preferences).ActiveSpeechModelId ?? _settings?.ActiveSpeechModelId;

    private SpeechModelProfile? GetSpeechProfile()
    {
        var id = GetSpeechModelId();
        var profile = _settings?.SpeechModels.Concat(_preferences.CustomSpeechModels).FirstOrDefault(x => x.Id == id);
        if (profile is null) return null;
        var path = string.IsNullOrWhiteSpace(profile.ModelPath) || Path.IsPathRooted(profile.ModelPath)
            ? profile.ModelPath : ResolveBundledPath(profile.ModelPath) ?? Path.GetFullPath(Path.Combine(_appDirectory, profile.ModelPath));
        return profile with { ModelPath = path, ApiKey = Environment.GetEnvironmentVariable($"PRY_SPEECH_API_KEY_{profile.Id.Replace('-', '_').ToUpperInvariant()}") };
    }

    private ModelTuningPreferences? GetModelTuning(string id, UserPreferences? preferences = null)
    {
        var value = preferences ?? _preferences;
        if (value.ModelTunings.TryGetValue(id, out var tuning)) return tuning;
        return id == GetActiveModelId(value) ? value.ModelTuning with { EnableThinking = value.ModelTuning.EnableThinking ?? value.EnableThinking } : null;
    }

    private static ModelProfile ApplyTuning(ModelProfile profile, ModelTuningPreferences? tuning) => tuning is null ? profile : profile with
    {
        EnableThinking = tuning.EnableThinking ?? profile.EnableThinking,
        Temperature = tuning.Temperature ?? profile.Temperature,
        MaxOutputTokens = tuning.MaxOutputTokens ?? profile.MaxOutputTokens,
        ContextSize = tuning.ContextSize ?? profile.ContextSize,
        GpuLayers = tuning.GpuLayers ?? profile.GpuLayers,
        ComputeDevice = tuning.ComputeDevice ?? profile.ComputeDevice
    };

    private ModelProfile[] BuildProfiles(UserPreferences preferences) => _settings!.Models
        .Concat(preferences.CustomModels)
        .Select(ResolveProfile)
        .Select(x => ApplyTuning(x, GetModelTuning(x.Id, preferences)))
        .ToArray();

    private TurnTakingSettings EffectiveTurnSettings() => _preferences.TurnTakingOverride ?? _settings!.TurnTaking;

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
        var items = new List<ListBoxItem>();
        void AddGroup(string? folderId, string name)
        {
            var grouped = rooms.Where(x => x.FolderId == folderId).ToArray(); if (grouped.Length == 0 && folderId is null) return;
            var key = folderId ?? "__unfiled"; var collapsed = _collapsedConversationFolders.Contains(key);
            var header = new ListBoxItem { Tag = new ConversationFolderChoice(key, name), Content = $"{(collapsed ? "▸" : "▾")}  📁 {name}", FontWeight = FontWeight.SemiBold, Padding = new Thickness(10, 8) };
            header.PointerPressed += async (_, args) =>
            {
                if (!args.GetCurrentPoint(header).Properties.IsLeftButtonPressed) return;
                if (!_collapsedConversationFolders.Add(key)) _collapsedConversationFolders.Remove(key);
                args.Handled = true;
                await RefreshConversationRoomsAsync();
            };
            if (folderId is not null)
            {
                var folder = folders.First(x => x.Id == folderId);
                header.ContextMenu = CreateFolderContextMenu(folder);
            }
            else
            {
                var createFolder = new MenuItem { Header = "新建文件夹…" };
                createFolder.Click += async (_, _) => { if (await CreateConversationFolderAsync() is not null) await RefreshConversationRoomsAsync(); };
                header.ContextMenu = new ContextMenu { ItemsSource = new[] { createFolder } };
            }
            items.Add(header); if (collapsed) return;
            foreach (var room in grouped)
            {
                var title = new TextBlock { Text = $"{(room.IsPinned ? "📌 " : "")}{room.Title}", TextTrimming = TextTrimming.CharacterEllipsis, FontWeight = FontWeight.SemiBold };
                var detail = new TextBlock { Text = $"{room.UpdatedAt.LocalDateTime:MM-dd HH:mm} · {room.MessageCount} 条消息", Foreground = Brush.Parse("#8EA2B5"), FontSize = 11 };
                var item = new ListBoxItem { Tag = room, Padding = new Thickness(14, 9), Content = new StackPanel { Spacing = 2, Children = { title, detail } } };
                item.ContextMenu = CreateConversationContextMenu(room, folders); items.Add(item);
            }
        }
        AddGroup(null, "未分类"); foreach (var folder in folders) AddGroup(folder.Id, folder.Name);
        ConversationList.ItemsSource = items;
        ConversationList.SelectedItem = items.FirstOrDefault(x => x.Tag is ConversationRoom room && room.Id == _conversationId);
        var activeRoom = rooms.FirstOrDefault(x => x.Id == _conversationId); if (activeRoom is not null) CharacterName.Text = activeRoom.Title;
        _changingConversation = false;
    }

    private async void ConversationList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_changingConversation || ConversationList.SelectedItem is not ListBoxItem item) return;
        if (item.Tag is ConversationFolderChoice) return;
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
        await JsonConfiguration.SaveAsync(PreferencesPath, _preferences); await RefreshConversationRoomsAsync();
    }

    private ContextMenu CreateConversationContextMenu(ConversationRoom room, IReadOnlyList<ConversationFolder> folders)
    {
        var pin = new MenuItem { Header = room.IsPinned ? "取消顶置" : "顶置" };
        pin.Click += async (_, _) => { await _api.UpdateConversationAsync(room.Id, new UpdateConversationRequest(null, !room.IsPinned, null)); await RefreshConversationRoomsAsync(); };
        var rename = new MenuItem { Header = "重命名" };
        rename.Click += async (_, _) =>
        {
            var value = await PromptTextAsync("重命名对话", "对话房间标题", room.Title);
            if (string.IsNullOrWhiteSpace(value)) return; await _api.UpdateConversationAsync(room.Id, new UpdateConversationRequest(value, null, null)); await RefreshConversationRoomsAsync();
        };
        var move = new MenuItem { Header = "移动到文件夹" }; var moveItems = new List<MenuItem>();
        void AddMoveItem(string label, string? folderId)
        {
            var target = new MenuItem { Header = label }; target.Click += async (_, _) => { await _api.UpdateConversationAsync(room.Id, new UpdateConversationRequest(null, null, folderId, folderId is null)); await RefreshConversationRoomsAsync(); }; moveItems.Add(target);
        }
        AddMoveItem("未分类", null); foreach (var folder in folders) AddMoveItem(folder.Name, folder.Id);
        var createFolder = new MenuItem { Header = "新建文件夹…" };
        createFolder.Click += async (_, _) => { var folderId = await CreateConversationFolderAsync(); if (folderId is not null) { await _api.UpdateConversationAsync(room.Id, new UpdateConversationRequest(null, null, folderId)); await RefreshConversationRoomsAsync(); } };
        moveItems.Add(createFolder); move.ItemsSource = moveItems;
        var remove = new MenuItem { Header = "删除" }; remove.Click += async (_, _) => await DeleteConversationAsync(room);
        return new ContextMenu { ItemsSource = new MenuItem[] { pin, rename, move, remove } };
    }

    private ContextMenu CreateFolderContextMenu(ConversationFolder folder)
    {
        var rename = new MenuItem { Header = "重命名文件夹" };
        rename.Click += async (_, _) =>
        {
            var value = await PromptTextAsync("重命名文件夹", "文件夹名称", folder.Name);
            if (string.IsNullOrWhiteSpace(value)) return;
            await _api.RenameFolderAsync(folder.Id, value); await RefreshConversationRoomsAsync();
        };
        var remove = new MenuItem { Header = "删除文件夹" };
        remove.Click += async (_, _) =>
        {
            if (!await ConfirmAsync(this, "删除文件夹", $"确定删除“{folder.Name}”吗？其中的对话会移回未分类，不会被删除。")) return;
            await _api.DeleteFolderAsync(folder.Id); _collapsedConversationFolders.Remove(folder.Id); await RefreshConversationRoomsAsync();
        };
        return new ContextMenu { ItemsSource = new[] { rename, remove } };
    }

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

    private async Task<string?> PromptTextAsync(string title, string label, string initialValue)
    {
        var input = new TextBox { Text = initialValue }; var cancel = new Button { Content = "取消" }; var ok = new Button { Content = "确定", Classes = { "primary" } };
        var window = new Window { Title = title, Width = 460, Height = 270, Background = Brushes.Transparent, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        string? result = null; cancel.Click += (_, _) => window.Close(); ok.Click += (_, _) => { result = input.Text?.Trim(); window.Close(); };
        window.KeyDown += (_, args) => { if (args.Key == Key.Escape) { args.Handled = true; window.Close(); } else if (args.Key == Key.Enter) { args.Handled = true; result = input.Text?.Trim(); window.Close(); } };
        window.Opened += (_, _) => input.Focus();
        var content = CreateCompactDialogTextCard(new StackPanel { Spacing = 10, Children = { new TextBlock { Text = label, FontWeight = FontWeight.SemiBold }, input } });
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Children = { cancel, ok } };
        window.Content = CreateThemedDialogSurface(CreateCompactDialogLayout(content, actions));
        await window.ShowDialog(this); return result;
    }

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

    private string? ResolveBundledPath(string relativePath)
    {
        var directory = new DirectoryInfo(_appDirectory);
        for (var i = 0; i < 8 && directory is not null; i++, directory = directory.Parent) { var candidate = Path.GetFullPath(Path.Combine(directory.FullName, relativePath)); if (File.Exists(candidate) || Directory.Exists(candidate)) return candidate; }
        return null;
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
                var renderKey = BackgroundRenderKey(theme.BackgroundImagePath!, blurRadius);
                if (!string.Equals(_currentBackgroundRenderKey, renderKey, StringComparison.Ordinal))
                {
                    ChatBackgroundImage.Source = GetBackgroundBitmap(theme.BackgroundImagePath!, blurRadius);
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
                dialogBackground.Source = GetBackgroundBitmap(backgroundPath, Math.Clamp(theme.BackgroundBlurRadius, 0, 32));
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

    private string BackgroundRenderKey(string path, double radius) => $"{Path.GetFullPath(path)}|{File.GetLastWriteTimeUtc(path).Ticks}|{Math.Round(radius, 1):0.0}";

    private Bitmap GetBackgroundBitmap(string path, double radius)
    {
        var key = BackgroundRenderKey(path, radius);
        if (_backgroundRenderCache.TryGetValue(key, out var cached)) return cached;
        var bitmap = CreateBackgroundBitmap(path, radius);
        _backgroundRenderCache[key] = bitmap;
        if (_backgroundRenderCache.Count > 12)
        {
            var stale = _backgroundRenderCache.Keys.FirstOrDefault(x => !string.Equals(x, key, StringComparison.Ordinal));
            if (stale is not null && _backgroundRenderCache.Remove(stale, out var removed)) removed.Dispose();
        }
        return bitmap;
    }

    private static Bitmap CreateBackgroundBitmap(string path, double radius)
    {
        if (radius < .5) return new Bitmap(path);
        using var decoded = SKBitmap.Decode(path) ?? throw new InvalidDataException("无法读取背景图片。");
        const int maxSide = 1800;
        var scale = Math.Min(1d, maxSide / (double)Math.Max(decoded.Width, decoded.Height));
        var width = Math.Max(1, (int)Math.Round(decoded.Width * scale)); var height = Math.Max(1, (int)Math.Round(decoded.Height * scale));
        using var resized = scale < 1 ? decoded.Resize(new SKImageInfo(width, height), SKFilterQuality.High) : decoded.Copy();
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        using var paint = new SKPaint { ImageFilter = SKImageFilter.CreateBlur((float)Math.Clamp(radius, 0, 40), (float)Math.Clamp(radius, 0, 40)), IsAntialias = true };
        surface.Canvas.Clear(SKColors.Transparent); surface.Canvas.DrawBitmap(resized, 0, 0, paint); surface.Canvas.Flush();
        using var image = surface.Snapshot(); using var data = image.Encode(SKEncodedImageFormat.Png, 92); using var stream = new MemoryStream(data.ToArray());
        return new Bitmap(stream);
    }

    private void ApplyUiSizing(ThemePreferences theme)
    {
        var avatar = Math.Clamp(theme.AvatarSize, 28, 76);
        var header = avatar * 1.25; HeaderAvatarBorder.Width = header; HeaderAvatarBorder.Height = header; HeaderAvatarBorder.CornerRadius = new CornerRadius(header / 2);
        var sidebar = avatar * 1.15; SidebarAvatar.Width = sidebar; SidebarAvatar.Height = sidebar; SidebarAvatar.CornerRadius = new CornerRadius(sidebar / 2);
        foreach (var item in _messageAvatarControls)
        {
            item.Control.Width = avatar; item.Control.Height = avatar; item.Control.CornerRadius = new CornerRadius(avatar / 2);
            item.Control.Background = item.IsUser ? Brush.Parse(lightTheme() ? "#91A4B5" : "#536C82") : ThemeAccentBrush();
        }
        foreach (var item in _messageBubbleControls) item.Control.Background = item.IsUser ? ThemeAccentBrush() : AssistantBubbleBrush();
        foreach (var item in _bubbleTextControls) { item.Control.FontSize = Math.Clamp(theme.BubbleFontSize, 11, 24); item.Control.MaxWidth = Math.Clamp(theme.BubbleMaxWidth, 280, 900); item.Control.Foreground = item.IsUser ? AccentTextBrush() : Brush.Parse(lightTheme() ? "#17212B" : "#F2F6FA"); }
        MessagesPanel.Spacing = Math.Clamp(theme.BubbleSpacing, 2, 36);
        bool lightTheme() => theme.ThemeMode.Equals("light", StringComparison.OrdinalIgnoreCase) || (theme.ThemeMode.Equals("system", StringComparison.OrdinalIgnoreCase) && Application.Current?.ActualThemeVariant == ThemeVariant.Light);
    }

    private string CacheManagedImage(string sourcePath, string category)
    {
        var targetDirectory = Path.Combine(category == "characters" ? CharacterMediaDirectory : ThemeMediaDirectory, category);
        Directory.CreateDirectory(targetDirectory);
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        var target = Path.Combine(targetDirectory, $"{Guid.NewGuid():N}{extension}");
        File.Copy(sourcePath, target, false);
        return target;
    }

    private static bool IsPathInside(string path, string directory)
    {
        var fullPath = Path.GetFullPath(path);
        var fullDirectory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
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
        if (Matches(e, _preferences.Shortcuts.NewLine)) return;
        if (Matches(e, _preferences.Shortcuts.SendImmediately)) { e.Handled = true; await SendAsync(true); }
        else if (Matches(e, _preferences.Shortcuts.Send)) { e.Handled = true; await SendAsync(false); }
    }
    private async void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled) return;
        if (e.Key == Key.Z && e.KeyModifiers == KeyModifiers.Control) { e.Handled = true; await UndoConversationMutationAsync(); }
        else if (Matches(e, _preferences.Shortcuts.CancelReply)) { _turnManager?.CancelAgentReply(); TurnStatus.Text = "已打断"; e.Handled = true; }
        else if (Matches(e, _preferences.Shortcuts.NewConversation)) { NewConversation_Click(sender, e); e.Handled = true; }
        else if (Matches(e, _preferences.Shortcuts.OpenStickers)) { ToggleStickerPopup(); e.Handled = true; }
        else if (Matches(e, _preferences.Shortcuts.OpenCharacterEditor)) { await OpenCharacterEditorAsync(); e.Handled = true; }
    }
    private static bool Matches(KeyEventArgs e, string gesture)
    {
        var parts = gesture.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); if (parts.Length == 0) return false;
        var modifiers = KeyModifiers.None;
        foreach (var part in parts[..^1]) modifiers |= part.ToLowerInvariant() switch { "ctrl" or "control" => KeyModifiers.Control, "shift" => KeyModifiers.Shift, "alt" => KeyModifiers.Alt, "meta" or "win" => KeyModifiers.Meta, _ => KeyModifiers.None };
        var keyName = parts[^1];
        var keyMatches = keyName.Equals("Enter", StringComparison.OrdinalIgnoreCase)
            ? e.Key is Key.Enter or Key.Return
            : Enum.TryParse<Key>(keyName, true, out var key) && e.Key == key;
        return keyMatches && e.KeyModifiers == modifiers;
    }
    private void InputBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressInputActivity) return;
        _turnManager?.NotifyInputActivity();
        if (string.IsNullOrWhiteSpace(InputBox.Text)) EndUserComposition(); else BeginUserComposition(false);
    }

    private async Task SendAsync(bool immediate, string? stickerId = null)
    {
        var text = InputBox.Text?.Trim() ?? ""; if (_turnManager is null || (text.Length == 0 && stickerId is null && _attachments.Count == 0)) return;
        EndUserComposition();
        var sentAttachments = _attachments.ToArray();
        var input = new UserInputPart(text, stickerId, sentAttachments); _suppressInputActivity = true; InputBox.Text = ""; _suppressInputActivity = false; ClearAttachments();
        _conversationDrafts.Remove(_conversationId);
        var messageId = await _turnManager.SubmitUserInputAsync(input, immediate);
        if (text.Length > 0) AddTextBubble("你", text, true, messageId: messageId); if (stickerId is not null) AddStickerBubble("你", stickerId, true, messageId: messageId);
        foreach (var attachment in sentAttachments) AddAttachmentBubble("你", attachment, true, messageId);
        await RefreshConversationRoomsAsync();
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
        var text = new TextBlock { Text = content, TextWrapping = TextWrapping.Wrap, MaxWidth = Math.Clamp(theme.BubbleMaxWidth, 280, 900), FontSize = Math.Clamp(theme.BubbleFontSize, 11, 24), Foreground = isUser ? AccentTextBrush() : ThemeTextBrush() };
        _bubbleTextControls.Add((text, isUser));
        AddBubble(author, new Border { Background = isUser ? ThemeAccentBrush() : AssistantBubbleBrush(), CornerRadius = new CornerRadius(12), Padding = new Thickness(13, 9), Child = text }, isUser, timestamp, messageId, content);
    }
    private void AddStickerBubble(string author, string stickerId, bool isUser, DateTimeOffset? timestamp = null, long? messageId = null)
    {
        var sticker = _stickerCatalog?.Find(stickerId); if (sticker is null) { AddTextBubble(author, "[表情]", isUser); return; }
        try { AddBubble(author, new Border { CornerRadius = new CornerRadius(14), Child = new Image { Source = new Bitmap(sticker.FilePath), MaxWidth = 220, MaxHeight = 220, Stretch = Stretch.Uniform } }, isUser, timestamp, messageId, ""); }
        catch { AddTextBubble(author, $"[表情：{sticker.Name}]", isUser); }
    }
    private void AddImageBubble(string author, string imagePath, bool isUser, DateTimeOffset? timestamp = null, long? messageId = null)
    {
        try
        {
            var image = new Image { Source = new Bitmap(imagePath), MaxWidth = 360, MaxHeight = 280, Stretch = Stretch.Uniform };
            AddBubble(author, new Border { Background = isUser ? ThemeAccentBrush() : AssistantBubbleBrush(), CornerRadius = new CornerRadius(12), Padding = new Thickness(5), Child = image }, isUser, timestamp, messageId, "");
        }
        catch { AddTextBubble(author, $"[图片无法显示：{Path.GetFileName(imagePath)}]", isUser); }
    }
    private void AddAttachmentBubble(string author, ChatAttachment attachment, bool isUser, long? messageId = null)
    {
        if (attachment.IsImage) { AddImageBubble(author, attachment.Path, isUser); return; }
        var extension = Path.GetExtension(attachment.Name).TrimStart('.').ToUpperInvariant();
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        panel.Children.Add(new Border { Width = 42, Height = 42, CornerRadius = new CornerRadius(8), Background = Brush.Parse("#263746"), Child = new TextBlock { Text = extension, FontSize = 10, FontWeight = FontWeight.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
        panel.Children.Add(new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { new TextBlock { Text = attachment.Name, MaxWidth = 300, TextTrimming = TextTrimming.CharacterEllipsis }, new TextBlock { Text = "文档附件", FontSize = 10, Foreground = Brush.Parse("#8EA2B5") } } });
        AddBubble(author, new Border { Background = isUser ? ThemeAccentBrush() : AssistantBubbleBrush(), CornerRadius = new CornerRadius(12), Padding = new Thickness(10), Child = panel }, isUser, messageId: messageId, messageText: "");
    }
    private Control CreateMessageAvatar(bool isUser)
    {
        var theme = _preferences.Theme ?? new ThemePreferences();
        var path = isUser ? theme.UserAvatarPath : _character?.AvatarPath ?? theme.CharacterAvatarPath;
        Control child;
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            try
            {
                var image = new Image { Source = new Bitmap(path), Stretch = Stretch.UniformToFill };
                var display = isUser && theme.UserAvatarDisplays.TryGetValue(path, out var userDisplay) ? userDisplay : _character?.AvatarDisplay ?? new ImageDisplayPreferences();
                ApplyImageDisplay(image, display); child = image;
            }
            catch { child = new TextBlock { Text = isUser ? "你" : (_character?.Name.FirstOrDefault().ToString() ?? "P"), FontWeight = FontWeight.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }; }
        }
        else child = new TextBlock { Text = isUser ? "你" : (_character?.Name.FirstOrDefault().ToString() ?? "P"), FontWeight = FontWeight.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var size = Math.Clamp(theme.AvatarSize, 28, 76);
        var result = new Border { Width = size, Height = size, CornerRadius = new CornerRadius(size / 2), Background = isUser ? Brush.Parse("#536C82") : ThemeAccentBrush(), ClipToBounds = true, Child = child, VerticalAlignment = VerticalAlignment.Bottom };
        _messageAvatarControls.Add((result, isUser)); return result;
    }

    private void AddBubble(string author, Control content, bool isUser, DateTimeOffset? timestamp = null, long? messageId = null, string messageText = "")
    {
        var panel = new StackPanel { Spacing = 3, HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left };
        var meta = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 7, HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left };
        meta.Children.Add(new TextBlock { Text = author, FontSize = 11, Foreground = Brush.Parse("#7F91A4") }); meta.Children.Add(new TextBlock { Text = (timestamp ?? DateTimeOffset.Now).LocalDateTime.ToString("HH:mm"), FontSize = 10, Foreground = Brush.Parse("#586B7D") });
        if (messageId is long storedId) panel.ContextMenu = CreateMessageContextMenu(storedId, isUser, messageText);
        panel.Children.Add(meta); panel.Children.Add(content);
        var row = new Grid { ColumnDefinitions = isUser ? new ColumnDefinitions("*,Auto") : new ColumnDefinitions("Auto,*"), ColumnSpacing = 8 };
        var avatar = CreateMessageAvatar(isUser);
        if (content is Border bubble && bubble.Background is not null) _messageBubbleControls.Add((bubble, isUser));
        if (isUser) { Grid.SetColumn(panel, 0); Grid.SetColumn(avatar, 1); } else { Grid.SetColumn(avatar, 0); Grid.SetColumn(panel, 1); }
        row.Children.Add(panel); row.Children.Add(avatar); if (panel.ContextMenu is not null) row.ContextMenu = panel.ContextMenu;
        MessagesPanel.Children.Insert(Math.Max(0, MessagesPanel.Children.Count - 1), row); ScrollChatToEnd();
    }

    private ContextMenu CreateMessageContextMenu(long messageId, bool isUser, string messageText)
    {
        var items = new List<MenuItem>();
        if (isUser)
        {
            var edit = new MenuItem { Header = "从此条消息开始编辑" };
            edit.Click += async (_, _) => await EditFromUserMessageAsync(messageId, messageText);
            items.Add(edit);
        }
        else
        {
            var regenerate = new MenuItem { Header = "从此条开始重新回复" };
            regenerate.Click += async (_, _) => await RegenerateFromAssistantMessageAsync(messageId);
            items.Add(regenerate);
        }
        var remove = new MenuItem { Header = "删除" };
        remove.Click += async (_, _) => await DeleteMessageFromMenuAsync(messageId, isUser);
        items.Add(remove);
        return new ContextMenu { ItemsSource = items };
    }

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
        MessagesPanel.Children.Clear(); _messageAvatarControls.Clear(); _messageBubbleControls.Clear(); _bubbleTextControls.Clear();
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
        var rejected = new List<string>();
        foreach (var sourcePath in paths.Where(File.Exists))
        {
            if (_attachments.Count >= 12) { rejected.Add("最多同时添加 12 个附件"); break; }
            var kind = GetAttachmentKind(sourcePath);
            if (kind is null) { rejected.Add($"{Path.GetFileName(sourcePath)}：格式不支持"); continue; }
            if (new FileInfo(sourcePath).Length > 20 * 1024 * 1024) { rejected.Add($"{Path.GetFileName(sourcePath)}：超过 20 MB"); continue; }
            try
            {
                var directory = Path.Combine(_dataDirectory, "attachments"); Directory.CreateDirectory(directory);
                var storedPath = Path.Combine(directory, $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{Path.GetExtension(sourcePath).ToLowerInvariant()}");
                File.Copy(sourcePath, storedPath);
                _attachments.Add(new ChatAttachment(storedPath, kind.Value, Path.GetFileName(sourcePath)));
            }
            catch { rejected.Add($"{Path.GetFileName(sourcePath)}：无法读取"); }
        }
        RefreshAttachmentTray();
        if (_attachments.Count > 0) _turnManager?.NotifyInputActivity();
        if (rejected.Count > 0) await ShowNoticeAsync("部分附件未添加", string.Join(Environment.NewLine, rejected.Distinct()));
    }

    private static ChatAttachmentKind? GetAttachmentKind(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif" => ChatAttachmentKind.Image,
        ".txt" => ChatAttachmentKind.Text,
        ".csv" or ".scv" => ChatAttachmentKind.Csv,
        ".docx" => ChatAttachmentKind.Docx,
        _ => null
    };

    private void RefreshAttachmentTray()
    {
        AttachmentPreviewPanel.Children.Clear();
        foreach (var attachment in _attachments.ToArray())
        {
            Control preview;
            if (attachment.IsImage)
            {
                try { preview = new Image { Source = new Bitmap(attachment.Path), Width = 72, Height = 58, Stretch = Stretch.Uniform }; }
                catch { preview = new TextBlock { Text = "IMG", Width = 72, TextAlignment = TextAlignment.Center }; }
            }
            else preview = new Border { Width = 58, Height = 58, CornerRadius = new CornerRadius(8), Background = Brush.Parse("#263746"), Child = new TextBlock { Text = Path.GetExtension(attachment.Name).TrimStart('.').ToUpperInvariant(), FontSize = 10, FontWeight = FontWeight.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
            var remove = new Button { Content = "×", Classes = { "composer-action" }, Width = 26, Height = 26, Padding = new Thickness(0), VerticalAlignment = VerticalAlignment.Top };
            remove.Click += (_, _) => { _attachments.Remove(attachment); RefreshAttachmentTray(); };
            var card = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto"), Margin = new Thickness(0, 0, 8, 0), Children = { preview, remove } };
            Grid.SetColumn(remove, 1);
            ToolTip.SetTip(card, attachment.Name);
            AttachmentPreviewPanel.Children.Add(card);
        }
        AttachmentTray.IsVisible = _attachments.Count > 0;
        ScrollChatToEnd();
    }

    private void ClearAttachments()
    {
        _attachments.Clear();
        RefreshAttachmentTray();
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
        if (_stickerCatalog is null) return;
        if (StickerPopup.IsOpen) { StickerPopup.IsOpen = false; return; }
        StickerGrid.Children.Clear();
        foreach (var sticker in _stickerCatalog.Enabled)
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
        await JsonConfiguration.SaveAsync(PreferencesPath, _preferences); await RefreshConversationRoomsAsync();
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
            avatarPath = CacheManagedImage(source, "user-avatars"); focusX.Value = .5m; focusY.Value = .5m; zoom.Value = 1; RefreshPreview();
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
            await JsonConfiguration.SaveAsync(PreferencesPath, _preferences); ApplyTheme(); window.Close();
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
        var save = new Button { Content = "保存", Classes = { "primary" } }; var saveAndSwitch = new Button { Content = "保存并切换到此角色" }; form.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Margin = new Thickness(0, 10, 0, 0), Children = { saveAndSwitch, save } });
        var left = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), Background = Brushes.Transparent, Children = { new TextBlock { Text = "角色卡", FontSize = 21, FontWeight = FontWeight.SemiBold, Margin = new Thickness(16, 18, 12, 4) }, cardList, newCard } }; Grid.SetRow(cardList, 1); Grid.SetRow(newCard, 2);
        var editorScroll = new ScrollViewer { Content = form, Background = Brushes.Transparent };
        var divider = new Border { Width = 1, Background = Brush.Parse("#263746"), HorizontalAlignment = HorizontalAlignment.Right };
        var layout = new Grid { ColumnDefinitions = new ColumnDefinitions("210,*"), Background = Brushes.Transparent, Children = { left, divider, editorScroll } }; Grid.SetColumn(editorScroll, 1);
        var window = new Window { Title = "角色卡管理", Width = 920, Height = 740, Background = Brushes.Transparent, WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = CreateThemedDialogSurface(layout) };
        chooseAvatar.Click += async (_, _) =>
        {
            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "选择角色头像", AllowMultiple = false, FileTypeFilter = [new FilePickerFileType("图片") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp"] }] });
            var source = files.FirstOrDefault()?.TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(source)) { avatarPath = CacheManagedImage(source, "characters"); RefreshAvatarPreview(); }
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
            await JsonConfiguration.SaveAsync(Path.Combine(CharacterDirectory, edited.Id + ".json"), edited); _characters.RemoveAll(x => x.Id == edited.Id); _characters.Add(edited); editingId = edited.Id;
            if (_character?.Id == edited.Id || switchToCard) { _character = edited; CharacterName.Text = edited.Name; ApplyTheme(); CreateTurnManager(); }
            if (switchToCard) { _conversationId = Guid.NewGuid().ToString("N"); ResetMessagesPanel(); AddTextBubble(edited.Name, edited.Greeting, false); _preferences = _preferences with { SelectedCharacterId = edited.Id }; await JsonConfiguration.SaveAsync(PreferencesPath, _preferences); }
            RefreshCharacterSelector(); RefreshList(edited.Id); currentHint.Text = edited.Id == _character?.Id ? "这是当前聊天角色" : "已保存；当前聊天角色没有改变"; RuntimeStatus.Text = "角色卡已保存";
        }
        save.Click += async (_, _) => await SaveAsync(false); saveAndSwitch.Click += async (_, _) => await SaveAsync(true);
        RefreshList(_character.Id); LoadCard(_character); await window.ShowDialog(owner ?? this);
    }

    private static string CardLabel(CharacterDefinition card) => string.IsNullOrWhiteSpace(card.CardName) ? $"{card.Name}-正式-v1" : card.CardName;

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
        if (_settings is null) return;
        var settingsOriginalPreferences = _preferences; var themeCommitted = false;
        var llamaPath = ResolveBundledPath(_settings.LlamaServerPath) ?? Path.GetFullPath(Path.Combine(_appDirectory, _settings.LlamaServerPath));
        var detectedDevices = await LlamaHardwareDetector.ListDevicesAsync(llamaPath);
        var computeChoices = new List<ComputeChoice>
        {
            new("auto-discrete", detectedDevices.Count == 0 ? "自动（优先独显；当前未检测到 GPU 后端）" : "自动（优先独立显卡）"),
            new("cpu", "仅使用 CPU")
        };
        computeChoices.AddRange(detectedDevices.Select(x => new ComputeChoice(x.Id, $"{x.Name}（{x.IsIntegrated switch { true => "核显", false => "独显/加速卡" }}）")));
        TextBox Box(string value, int height = 34) => new() { Text = value, MinHeight = height, AcceptsReturn = height > 60, TextWrapping = TextWrapping.Wrap };
        NumericUpDown Number(decimal value, decimal min, decimal max, decimal step = 1) => new() { Value = value, Minimum = min, Maximum = max, Increment = step };
        var turn = EffectiveTurnSettings();
        var themePreferences = _preferences.Theme ?? new ThemePreferences();
        var allModels = _settings.Models.Concat(_preferences.CustomModels).ToArray();
        var textChoices = allModels.Where(x => x.Capabilities.Text).Select(x => new ModelChoice(x.Id, x.DisplayName)).ToArray();
        var visionChoices = allModels.Where(x => x.Capabilities.Vision).Select(x => new ModelChoice(x.Id, x.DisplayName)).ToArray();
        var parameterChoices = allModels.Select(x => new ModelChoice(x.Id, x.DisplayName)).ToArray();
        var activeModel = new ComboBox { ItemsSource = textChoices, SelectedItem = textChoices.FirstOrDefault(x => x.Id == GetActiveModelId()) };
        var visionModel = new ComboBox { ItemsSource = visionChoices, SelectedItem = visionChoices.FirstOrDefault(x => x.Id == GetVisionModelId()) };
        var parameterModel = new ComboBox { ItemsSource = parameterChoices };
        var speechChoices = _settings.SpeechModels.Concat(_preferences.CustomSpeechModels).Select(x => new ModelChoice(x.Id, x.DisplayName)).ToArray();
        var speechModel = new ComboBox { ItemsSource = speechChoices, SelectedItem = speechChoices.FirstOrDefault(x => x.Id == GetSpeechModelId()) };
        var thinking = new CheckBox { Content = "启用思考模式" };
        var temperature = Number(.8m, 0, 2, .05m); var outputTokens = Number(512, 32, 8192, 32); var contextSize = Number(4096, 512, 131072, 512); var gpuLayers = Number(0, 0, 999, 1);
        var computeDevice = new ComboBox { ItemsSource = computeChoices };
        var tuningDrafts = _preferences.ModelTunings.ToDictionary(x => x.Key, x => x.Value); string currentModelId = GetActiveModelId(); bool loadingModel = false;
        ModelProfile? ConfiguredModel(string id) => _settings.Models.Concat(_preferences.CustomModels).FirstOrDefault(x => x.Id == id);
        void StoreModelFields()
        {
            if (loadingModel || string.IsNullOrWhiteSpace(currentModelId)) return;
            tuningDrafts[currentModelId] = new ModelTuningPreferences { EnableThinking = thinking.IsChecked == true, Temperature = (double)(temperature.Value ?? .8m), MaxOutputTokens = (int)(outputTokens.Value ?? 512), ContextSize = (int)(contextSize.Value ?? 4096), GpuLayers = (int)(gpuLayers.Value ?? 999), ComputeDevice = (computeDevice.SelectedItem as ComputeChoice)?.Id ?? "auto-discrete" };
        }
        void LoadModelFields(string id)
        {
            var profile = ConfiguredModel(id); if (profile is null) return; var tuning = tuningDrafts.TryGetValue(id, out var saved) ? saved : GetModelTuning(id);
            loadingModel = true; thinking.IsChecked = tuning?.EnableThinking ?? profile.EnableThinking; temperature.Value = (decimal)(tuning?.Temperature ?? profile.Temperature); outputTokens.Value = tuning?.MaxOutputTokens ?? profile.MaxOutputTokens; contextSize.Value = tuning?.ContextSize ?? profile.ContextSize;
            var requestedDevice = tuning?.ComputeDevice ?? profile.ComputeDevice; computeDevice.SelectedItem = computeChoices.FirstOrDefault(x => x.Id == requestedDevice) ?? computeChoices[0];
            gpuLayers.Value = requestedDevice == "cpu" ? 0 : (tuning?.GpuLayers is > 0 ? tuning.GpuLayers : profile.GpuLayers > 0 ? profile.GpuLayers : 999); gpuLayers.IsEnabled = requestedDevice != "cpu"; loadingModel = false;
        }
        computeDevice.SelectionChanged += (_, _) => { if (loadingModel) return; var cpu = (computeDevice.SelectedItem as ComputeChoice)?.Id == "cpu"; gpuLayers.IsEnabled = !cpu; if (cpu) gpuLayers.Value = 0; else if (gpuLayers.Value <= 0) gpuLayers.Value = 999; };
        parameterModel.SelectionChanged += (_, _) => { if (parameterModel.SelectedItem is not ModelChoice choice || choice.Id == currentModelId) return; StoreModelFields(); currentModelId = choice.Id; LoadModelFields(currentModelId); };
        parameterModel.SelectedItem = parameterChoices.FirstOrDefault(x => x.Id == currentModelId) ?? parameterChoices.FirstOrDefault(); if (parameterModel.SelectedItem is ModelChoice initialChoice) { currentModelId = initialChoice.Id; LoadModelFields(currentModelId); }
        var debounce = Number(turn.DebounceMs, 0, 10000, 100); var maxPending = Number(turn.MaxPendingMs, 200, 30000, 100);
        var minReplies = Number(turn.MinReplyMessages, 1, 6); var maxReplies = Number(turn.MaxReplyMessages, 1, 6); var maxChars = Number(turn.MaxMessageCharacters, 20, 500, 10);
        var speed = Number((decimal)turn.TypingStyle.Speed, .25m, 4, .05m); var burst = Number((decimal)turn.TypingStyle.Burstiness, 0, 2, .05m);
        var minDelay = Number(turn.TypingStyle.MinDelayMs, 0, 10000, 50); var maxDelay = Number(turn.TypingStyle.MaxDelayMs, 0, 30000, 100);
        var split = new CheckBox { Content = "将回复拆成多条气泡", IsChecked = turn.SplitReplies }; var interrupts = new CheckBox { Content = "自动识别附和与打断", IsChecked = turn.AutoClassifyInterrupts };
        var listeningSignals = new CheckBox { Content = "输入或语音持续较久时发送“我在听”信号", IsChecked = turn.EnableListeningSignals };
        var listeningDelay = Number(turn.ListeningSignalDelayMs, 1500, 15000, 250);
        var style = Box(turn.StyleInstruction, 110);
        var send = Box(_preferences.Shortcuts.Send); var immediate = Box(_preferences.Shortcuts.SendImmediately); var newline = Box(_preferences.Shortcuts.NewLine); var cancel = Box(_preferences.Shortcuts.CancelReply); var newChat = Box(_preferences.Shortcuts.NewConversation); var stickers = Box(_preferences.Shortcuts.OpenStickers); var character = Box(_preferences.Shortcuts.OpenCharacterEditor);
        var settingCards = new List<Border>();
        var modelPanel = new StackPanel { Margin = new Thickness(28), Spacing = 14 }; var conversationPanel = new StackPanel { Margin = new Thickness(28), Spacing = 14 }; var shortcutPanel = new StackPanel { Margin = new Thickness(28), Spacing = 14 }; var desktopPanel = new StackPanel { Margin = new Thickness(28), Spacing = 14 }; var themePanel = new StackPanel { Margin = new Thickness(28), Spacing = 14 };
        void Header(Panel target, string value) => target.Children.Add(new TextBlock { Text = value, FontSize = 21, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 4, 0, 8) });
        void Field(Panel target, string label, Control control) { target.Children.Add(new TextBlock { Text = label, Foreground = Brush.Parse("#B8CCE0") }); target.Children.Add(control); }
        Border Card(string title, params Control[] controls)
        {
            var panel = new StackPanel { Spacing = 9 };
            panel.Children.Add(new TextBlock { Text = title, FontSize = 15, FontWeight = FontWeight.SemiBold });
            foreach (var control in controls) panel.Children.Add(control);
            var card = new Border { Background = Brush.Parse("#B017212B"), BorderBrush = Brush.Parse("#263746"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(18), Child = panel };
            settingCards.Add(card);
            return card;
        }
        Header(modelPanel, "模型与语音");
        var textFields = new StackPanel { Spacing = 7 }; Field(textFields, "文字聊天模型", activeModel); Field(textFields, "图片理解模型", visionModel); textFields.Children.Add(new TextBlock { Text = "原生多模态模型会同时出现在两个列表；选择不同模型时，图片先转为内部描述再交给文字模型。", TextWrapping = TextWrapping.Wrap, Foreground = Brush.Parse("#7F91A4") });
        var manageModels = new Button { Content = "管理自定义对话模型…", HorizontalAlignment = HorizontalAlignment.Left }; textFields.Children.Add(manageModels); modelPanel.Children.Add(Card("对话模型分工", textFields));
        var tuningFields = new StackPanel { Spacing = 7 }; Field(tuningFields, "参数配置对象", parameterModel); tuningFields.Children.Add(thinking); Field(tuningFields, "Temperature（随机性）", temperature); Field(tuningFields, "最大输出 Token", outputTokens); Field(tuningFields, "上下文长度", contextSize); Field(tuningFields, "计算设备", computeDevice); Field(tuningFields, "GPU 卸载层数（999 表示尽可能全部）", gpuLayers); tuningFields.Children.Add(new TextBlock { Text = "自动模式优先选择独立显卡，不会优先使用 Intel 核显；每个文字或图片模型分别保存自己的选择。", TextWrapping = TextWrapping.Wrap, Foreground = Brush.Parse("#7F91A4") }); modelPanel.Children.Add(Card("每模型独立参数", tuningFields));
        var speechFields = new StackPanel { Spacing = 7 }; Field(speechFields, "语音识别模型", speechModel); var manageSpeech = new Button { Content = "管理自定义语音模型…", HorizontalAlignment = HorizontalAlignment.Left }; speechFields.Children.Add(manageSpeech); speechFields.Children.Add(new TextBlock { Text = "点击聊天输入框旁的麦克风开始录音，再点一次结束；本地 SenseVoice 会直接转写到输入框。API 配置会按你的选择上传录音。", TextWrapping = TextWrapping.Wrap, Foreground = Brush.Parse("#7F91A4") }); modelPanel.Children.Add(Card("语音识别", speechFields));
        var tips = LoadTips();
        var tipText = tips.Count == 0 ? "暂无使用提示。" : tips[Random.Shared.Next(tips.Count)];
        Header(conversationPanel, "回复风格与轮次"); Field(conversationPanel, "额外对话风格指令", style); Field(conversationPanel, "每轮最少消息数", minReplies); Field(conversationPanel, "每轮最多消息数", maxReplies); Field(conversationPanel, "单条建议最大字符数", maxChars); conversationPanel.Children.Add(split); conversationPanel.Children.Add(interrupts); conversationPanel.Children.Add(listeningSignals); Field(conversationPanel, "“我在听”信号等待时间（毫秒）", listeningDelay); Field(conversationPanel, "等待用户补话（毫秒）", debounce); Field(conversationPanel, "最长等待（毫秒）", maxPending); Field(conversationPanel, "打字速度倍率（越大越快）", speed); Field(conversationPanel, "节奏随机度", burst); Field(conversationPanel, "单条最短延迟（毫秒）", minDelay); Field(conversationPanel, "单条最长延迟（毫秒）", maxDelay);
        Header(shortcutPanel, "快捷键"); shortcutPanel.Children.Add(new TextBlock { Text = "格式示例：Enter、Ctrl+Enter、Ctrl+Shift+C", Foreground = Brush.Parse("#7F91A4") }); Field(shortcutPanel, "发送", send); Field(shortcutPanel, "立即回复", immediate); Field(shortcutPanel, "换行", newline); Field(shortcutPanel, "打断回复", cancel); Field(shortcutPanel, "新对话", newChat); Field(shortcutPanel, "打开表情", stickers); Field(shortcutPanel, "角色卡", character);
        var petEnabled = new CheckBox { Content = "启用桌宠模式（等待美术素材）", IsChecked = _preferences.DesktopPet.Enabled }; var petTop = new CheckBox { Content = "桌宠始终置顶", IsChecked = _preferences.DesktopPet.AlwaysOnTop }; var petScale = Number((decimal)_preferences.DesktopPet.Scale, .5m, 3, .1m);
        Header(desktopPanel, "桌宠与托盘"); desktopPanel.Children.Add(petEnabled); desktopPanel.Children.Add(petTop); Field(desktopPanel, "桌宠缩放", petScale); desktopPanel.Children.Add(new TextBlock { Text = "托盘菜单会始终可用；桌宠渲染将在你提供美术素材后接入现有占位接口。", TextWrapping = TextWrapping.Wrap, Foreground = Brush.Parse("#7F91A4") });
        string? selectedBackground = themePreferences.BackgroundImagePath;
        string? selectedUserAvatar = themePreferences.UserAvatarPath;
        var backgroundDisplays = themePreferences.BackgroundDisplays.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        var userAvatarDisplays = themePreferences.UserAvatarDisplays.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        var backgroundHistory = themePreferences.BackgroundHistory.Append(themePreferences.BackgroundImagePath).Where(x => !string.IsNullOrWhiteSpace(x) && File.Exists(x)).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var userAvatarHistory = themePreferences.UserAvatarHistory.Append(themePreferences.UserAvatarPath).Where(x => !string.IsNullOrWhiteSpace(x) && File.Exists(x)).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToList();
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
        void LoadDisplay(string? path, Dictionary<string, ImageDisplayPreferences> values, NumericUpDown x, NumericUpDown y, NumericUpDown zoom, Image preview)
        {
            loadingImageControls = true; var display = path is not null && values.TryGetValue(path, out var saved) ? saved : new ImageDisplayPreferences();
            x.Value = (decimal)display.FocusX; y.Value = (decimal)display.FocusY; zoom.Value = (decimal)display.Zoom; preview.Source = null;
            if (path is not null && File.Exists(path)) try { preview.Source = new Bitmap(path); ApplyImageDisplay(preview, display); } catch { }
            loadingImageControls = false;
        }
        void RefreshImageGallery(WrapPanel gallery, IReadOnlyList<string> paths, string? selected, Action<string> select, bool square)
        {
            gallery.Children.Clear();
            foreach (var path in paths)
            {
                try
                {
                    var preview = new Image { Source = new Bitmap(path), Width = square ? 68 : 112, Height = square ? 68 : 72, Stretch = Stretch.UniformToFill };
                    var button = new Button { Width = square ? 82 : 126, Height = square ? 82 : 86, Margin = new Thickness(0, 0, 8, 8), Padding = new Thickness(5), BorderThickness = new Thickness(string.Equals(path, selected, StringComparison.OrdinalIgnoreCase) ? 3 : 1), BorderBrush = string.Equals(path, selected, StringComparison.OrdinalIgnoreCase) ? ThemeAccentBrush() : Brush.Parse("#40505F"), Content = preview };
                    ToolTip.SetTip(button, Path.GetFileName(path));
                    button.Click += (_, _) => select(path); gallery.Children.Add(button);
                }
                catch { }
            }
            if (gallery.Children.Count == 0) gallery.Children.Add(new TextBlock { Text = "尚未导入图片", Foreground = Brush.Parse("#7F91A4"), Margin = new Thickness(0, 8) });
        }
        Action previewTheme = () => { };
        void RefreshBackgroundGallery() => RefreshImageGallery(backgroundGallery, backgroundHistory, selectedBackground, path => { selectedBackground = path; LoadDisplay(path, backgroundDisplays, backgroundFocusX, backgroundFocusY, backgroundZoom, backgroundPreview); RefreshBackgroundGallery(); previewTheme(); }, false);
        void RefreshUserAvatarGallery() => RefreshImageGallery(userAvatarGallery, userAvatarHistory, selectedUserAvatar, path => { selectedUserAvatar = path; LoadDisplay(path, userAvatarDisplays, avatarFocusX, avatarFocusY, avatarZoom, userAvatarPreview); RefreshUserAvatarGallery(); previewTheme(); }, true);
        RefreshBackgroundGallery(); RefreshUserAvatarGallery(); LoadDisplay(selectedBackground, backgroundDisplays, backgroundFocusX, backgroundFocusY, backgroundZoom, backgroundPreview); LoadDisplay(selectedUserAvatar, userAvatarDisplays, avatarFocusX, avatarFocusY, avatarZoom, userAvatarPreview);
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
        StackPanel ManagerPanel(string title, string description, Button action)
        {
            var panel = new StackPanel { Margin = new Thickness(28), Spacing = 14 }; Header(panel, title);
            panel.Children.Add(Card(title, new TextBlock { Text = description, TextWrapping = TextWrapping.Wrap, Foreground = Brush.Parse("#B8CCE0") }, action)); return panel;
        }
        var characterManagerButton = new Button { Content = "打开角色卡管理…", HorizontalAlignment = HorizontalAlignment.Left };
        var stickerManagerButton = new Button { Content = "打开表情包管理…", HorizontalAlignment = HorizontalAlignment.Left };
        var memoryManagerButton = new Button { Content = "打开长期记忆管理…", HorizontalAlignment = HorizontalAlignment.Left };
        var characterManagerPanel = ManagerPanel("角色卡", $"当前角色：{_character?.Name ?? "未加载"}。可编辑、切换或新建角色卡。", characterManagerButton);
        var stickerManagerPanel = ManagerPanel("表情包", "管理预装与用户导入的表情，设置情绪标签和互动作用。", stickerManagerButton);
        var memoryManagerPanel = ManagerPanel("长期记忆", "查看、检索和编辑当前角色的长期记忆库。", memoryManagerButton);
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
            if (selectedBackground is not null) { var display = ReadDisplay(backgroundFocusX, backgroundFocusY, backgroundZoom); backgroundDisplays[selectedBackground] = display; ApplyImageDisplay(backgroundPreview, display); }
            if (selectedUserAvatar is not null) { var display = ReadDisplay(avatarFocusX, avatarFocusY, avatarZoom); userAvatarDisplays[selectedUserAvatar] = display; ApplyImageDisplay(userAvatarPreview, display); }
        }
        ThemePreferences BuildThemeDraft()
        {
            StoreCurrentImageDisplays();
            return new ThemePreferences
            {
                BackgroundImagePath = selectedBackground, BackgroundHistory = backgroundHistory.ToArray(), BackgroundDisplays = new Dictionary<string, ImageDisplayPreferences>(backgroundDisplays),
                UserAvatarPath = selectedUserAvatar, UserAvatarHistory = userAvatarHistory.ToArray(), UserAvatarDisplays = new Dictionary<string, ImageDisplayPreferences>(userAvatarDisplays), CharacterAvatarPath = themePreferences.CharacterAvatarPath,
                ThemeMode = themeMode.SelectedIndex switch { 1 => "dark", 2 => "light", _ => "system" }, AccentColor = accentColor.Text?.Trim() ?? "#B148C6",
                UseGlassEffects = glassEffects.IsChecked == true, LiveSidebarResize = liveSidebarResize.IsChecked == true,
                BackgroundImageOpacity = (double)(backgroundOpacity.Value ?? 1m), BackgroundDimOpacity = (double)(backgroundDim.Value ?? .34m),
                BackgroundBlurMode = (backgroundBlur.Value ?? 0) > 0 ? "blur" : "none", BackgroundBlurRadius = (double)(backgroundBlur.Value ?? 0),
                AvatarSize = (double)(avatarSize.Value ?? 48), BubbleFontSize = (double)(bubbleFontSize.Value ?? 14), BubbleMaxWidth = (double)(bubbleMaxWidth.Value ?? 620), BubbleSpacing = (double)(bubbleSpacing.Value ?? 10)
            };
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
                        settingsBackgroundImage.Source = GetBackgroundBitmap(draft.BackgroundImagePath!, Math.Clamp(draft.BackgroundBlurRadius, 0, 32));
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
        var previewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(55) };
        previewTimer.Tick += (_, _) =>
        {
            previewTimer.Stop();
            if (!Color.TryParse(accentColor.Text, out _)) return;
            var draft = BuildThemeDraft(); _preferences = _preferences with { Theme = draft }; ApplyTheme(); UpdateSettingsSurface(draft);
        };
        void PreviewTheme() { previewTimer.Stop(); previewTimer.Start(); }
        previewTheme = PreviewTheme;
        void PreviewValueChanged(object? _, NumericUpDownValueChangedEventArgs __) { if (!loadingImageControls) PreviewTheme(); }
        themeMode.SelectionChanged += (_, _) => PreviewTheme(); accentColor.TextChanged += (_, _) => PreviewTheme(); glassEffects.IsCheckedChanged += (_, _) => PreviewTheme();
        liveSidebarResize.IsCheckedChanged += (_, _) => PreviewTheme();
        foreach (var number in new[] { backgroundOpacity, backgroundBlur, backgroundDim, avatarSize, bubbleFontSize, bubbleMaxWidth, bubbleSpacing, backgroundFocusX, backgroundFocusY, backgroundZoom, avatarFocusX, avatarFocusY, avatarZoom }) number.ValueChanged += PreviewValueChanged;
        window.Closed += (_, _) => { previewTimer.Stop(); if (themeCommitted) return; _preferences = settingsOriginalPreferences; ApplyTheme(); };
        UpdateSettingsSurface(themePreferences);
        async Task<string?> PickThemeImageAsync(string title, string category)
        {
            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = title, AllowMultiple = false, FileTypeFilter = [new FilePickerFileType("图片") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp"] }] });
            var path = files.FirstOrDefault()?.TryGetLocalPath(); return string.IsNullOrWhiteSpace(path) ? null : CacheManagedImage(path, category);
        }
        browseBackground.Click += async (_, _) => { var path = await PickThemeImageAsync("导入聊天背景", "backgrounds"); if (path is null) return; backgroundHistory.Add(path); selectedBackground = path; LoadDisplay(path, backgroundDisplays, backgroundFocusX, backgroundFocusY, backgroundZoom, backgroundPreview); RefreshBackgroundGallery(); PreviewTheme(); };
        clearBackground.Click += (_, _) => { selectedBackground = null; backgroundPreview.Source = null; RefreshBackgroundGallery(); PreviewTheme(); };
        deleteBackground.Click += async (_, _) =>
        {
            if (selectedBackground is null || !await ConfirmAsync(window, "删除背景", "确定从 Pry 素材库永久删除当前背景吗？")) return;
            var path = selectedBackground; backgroundHistory.RemoveAll(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase)); selectedBackground = null;
            if (IsPathInside(path, Path.Combine(ThemeMediaDirectory, "backgrounds"))) try { File.Delete(path); } catch { }
            backgroundPreview.Source = null; RefreshBackgroundGallery(); PreviewTheme();
        };
        browseUserAvatar.Click += async (_, _) => { var path = await PickThemeImageAsync("导入用户头像", "user-avatars"); if (path is null) return; userAvatarHistory.Add(path); selectedUserAvatar = path; LoadDisplay(path, userAvatarDisplays, avatarFocusX, avatarFocusY, avatarZoom, userAvatarPreview); RefreshUserAvatarGallery(); PreviewTheme(); };
        clearUserAvatar.Click += (_, _) => { selectedUserAvatar = null; userAvatarPreview.Source = null; RefreshUserAvatarGallery(); PreviewTheme(); };
        deleteUserAvatar.Click += async (_, _) =>
        {
            if (selectedUserAvatar is null || !await ConfirmAsync(window, "删除用户头像", "确定从 Pry 素材库永久删除当前头像吗？")) return;
            var path = selectedUserAvatar; userAvatarHistory.RemoveAll(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase)); selectedUserAvatar = null;
            if (IsPathInside(path, Path.Combine(ThemeMediaDirectory, "user-avatars"))) try { File.Delete(path); } catch { }
            userAvatarPreview.Source = null; RefreshUserAvatarGallery(); PreviewTheme();
        };
        characterManagerButton.Click += async (_, _) => await OpenCharacterEditorAsync(window);
        stickerManagerButton.Click += async (_, _) => await OpenStickerManagerAsync(window);
        memoryManagerButton.Click += async (_, _) => await OpenMemoryManagerAsync(window);
        manageModels.Click += async (_, _) =>
        {
            StoreModelFields();
            var selectedTextId = (activeModel.SelectedItem as ModelChoice)?.Id ?? GetActiveModelId();
            var selectedVisionId = (visionModel.SelectedItem as ModelChoice)?.Id ?? GetVisionModelId();
            var selectedParameterId = (parameterModel.SelectedItem as ModelChoice)?.Id ?? currentModelId;
            await ManageCustomModelsAsync(window);
            allModels = _settings.Models.Concat(_preferences.CustomModels).ToArray();
            textChoices = allModels.Where(x => x.Capabilities.Text).Select(x => new ModelChoice(x.Id, x.DisplayName)).ToArray();
            visionChoices = allModels.Where(x => x.Capabilities.Vision).Select(x => new ModelChoice(x.Id, x.DisplayName)).ToArray();
            parameterChoices = allModels.Select(x => new ModelChoice(x.Id, x.DisplayName)).ToArray();
            activeModel.ItemsSource = textChoices;
            activeModel.SelectedItem = textChoices.FirstOrDefault(x => x.Id == selectedTextId) ?? textChoices.FirstOrDefault();
            visionModel.ItemsSource = visionChoices;
            visionModel.SelectedItem = visionChoices.FirstOrDefault(x => x.Id == selectedVisionId) ?? visionChoices.FirstOrDefault();
            parameterModel.ItemsSource = parameterChoices;
            parameterModel.SelectedItem = parameterChoices.FirstOrDefault(x => x.Id == selectedParameterId) ?? parameterChoices.FirstOrDefault();
        };
        manageSpeech.Click += async (_, _) => { await ManageSpeechModelsAsync(window); speechChoices = _settings.SpeechModels.Concat(_preferences.CustomSpeechModels).Select(x => new ModelChoice(x.Id, x.DisplayName)).ToArray(); speechModel.ItemsSource = speechChoices; speechModel.SelectedItem = speechChoices.FirstOrDefault(x => x.Id == GetSpeechModelId()) ?? speechChoices.FirstOrDefault(); };
        save.Click += async (_, _) =>
        {
            var minCount = (int)(minReplies.Value ?? 1); var maxCount = (int)(maxReplies.Value ?? 4);
            if (maxCount < minCount || (minDelay.Value ?? 0) > (maxDelay.Value ?? 0)) { await ShowNoticeAsync("参数无效", "最多消息数不能小于最少消息数，最长延迟也不能小于最短延迟。"); return; }
            var turnOverride = new TurnTakingSettings { DebounceMs = (int)(debounce.Value ?? 1200), MaxPendingMs = (int)(maxPending.Value ?? 5000), SplitReplies = split.IsChecked == true, AutoClassifyInterrupts = interrupts.IsChecked == true, EnableListeningSignals = listeningSignals.IsChecked == true, ListeningSignalDelayMs = (int)(listeningDelay.Value ?? 4500), MinReplyMessages = minCount, MaxReplyMessages = maxCount, MaxMessageCharacters = (int)(maxChars.Value ?? 90), StyleInstruction = style.Text?.Trim() ?? "", TypingStyle = new TypingStyle { Speed = (double)(speed.Value ?? 1), Burstiness = (double)(burst.Value ?? .75m), MinDelayMs = (int)(minDelay.Value ?? 450), MaxDelayMs = (int)(maxDelay.Value ?? 3200) } };
            StoreModelFields(); var selectedModelId = (activeModel.SelectedItem as ModelChoice)?.Id ?? GetActiveModelId(); var selectedVisionId = (visionModel.SelectedItem as ModelChoice)?.Id; var selectedSpeechId = (speechModel.SelectedItem as ModelChoice)?.Id; var modelTuning = tuningDrafts.TryGetValue(selectedModelId, out var selectedTuning) ? selectedTuning with { ActiveModelId = selectedModelId } : new ModelTuningPreferences { ActiveModelId = selectedModelId };
            if (!Color.TryParse(accentColor.Text, out _)) { await ShowNoticeAsync("强调色无效", "请填写 #RRGGBB 格式的颜色，例如 #B148C6。"); return; }
            var candidate = _preferences with { ActiveModelId = selectedModelId, ActiveVisionModelId = selectedVisionId, ActiveSpeechModelId = selectedSpeechId, ModelTunings = new Dictionary<string, ModelTuningPreferences>(tuningDrafts), EnableThinking = modelTuning.EnableThinking == true, ModelTuning = modelTuning, TurnTakingOverride = turnOverride, SelectedCharacterId = _character?.Id, DesktopPet = new DesktopPetPreferences { Enabled = petEnabled.IsChecked == true, AlwaysOnTop = petTop.IsChecked == true, Scale = (double)(petScale.Value ?? 1) }, Theme = BuildThemeDraft(), Shortcuts = new ShortcutSettings { Send = send.Text ?? "Enter", SendImmediately = immediate.Text ?? "Ctrl+Enter", NewLine = newline.Text ?? "Shift+Enter", CancelReply = cancel.Text ?? "Escape", NewConversation = newChat.Text ?? "Ctrl+N", OpenStickers = stickers.Text ?? "Ctrl+E", OpenCharacterEditor = character.Text ?? "Ctrl+Shift+C" } };
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
                var backendModels = await _api.UpdateModelSelectionAsync(new UpdateModelSelectionRequest(
                    selectedModelId, selectedVisionId, selectedSpeechId, candidate.ModelTunings));
                _preferences = candidate;
                _profiles = BuildProfiles(candidate);
                await JsonConfiguration.SaveAsync(PreferencesPath, _preferences);
                ApplyTheme(); await ReloadActiveConversationAsync();
                RuntimeStatus.Text = $"后端模型配置已保存 · {backendModels.First(x => x.SelectedForText).DisplayName}";
                themeCommitted = true; window.Close();
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
        add.Click += async (_, _) => { var value = await EditAsync(null); if (value is not null) { models.Add(value); Refresh(); } };
        edit.Click += async (_, _) => { if (list.SelectedItem is not ModelChoice choice) return; var index = models.FindIndex(x => x.Id == choice.Id); if (index < 0) return; var value = await EditAsync(models[index]); if (value is not null) { models[index] = value; Refresh(); } };
        remove.Click += async (_, _) => { if (list.SelectedItem is not ModelChoice choice || !await ConfirmAsync(window, "删除语音模型配置", $"确定删除“{choice.Name}”吗？本地模型文件不会被删除。")) return; models.RemoveAll(x => x.Id == choice.Id); Refresh(); };
        done.Click += async (_, _) => { _preferences = _preferences with { CustomSpeechModels = models.ToArray() }; await JsonConfiguration.SaveAsync(PreferencesPath, _preferences); window.Close(); };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Children = { add, edit, remove, done } };
        var speechLayout = new Grid { Margin = new Thickness(22), RowDefinitions = new RowDefinitions("Auto,*,Auto"), Children = { new TextBlock { Text = "保留多个本地或 API 语音识别配置，在主设置页选择当前使用项。", TextWrapping = TextWrapping.Wrap }, list, buttons } }; Grid.SetRow(list, 1); Grid.SetRow(buttons, 2); window.Content = CreateThemedDialogSurface(speechLayout);
        await window.ShowDialog(owner);
    }

    private async void ManageStickers_Click(object? sender, RoutedEventArgs e) => await OpenStickerManagerAsync();

    private async Task OpenStickerManagerAsync(Window? owner = null)
    {
        if (_stickerCatalog is null) return;
        var window = new Window { Title = "管理表情包", Width = 760, Height = 570, Background = Brushes.Transparent, WindowStartupLocation = WindowStartupLocation.CenterOwner }; var grid = new WrapPanel { Orientation = Orientation.Horizontal, ItemWidth = 112, ItemHeight = 132 }; StickerDefinition? selected = null;
        var import = new Button { Content = "导入" }; var edit = new Button { Content = "编辑" }; var remove = new Button { Content = "删除" }; var close = new Button { Content = "完成" };
        void Refresh()
        {
            grid.Children.Clear();
            foreach (var sticker in _stickerCatalog.All)
            {
                Control preview; try { preview = new Image { Source = new Bitmap(sticker.FilePath), Width = 88, Height = 88, Stretch = Stretch.Uniform }; } catch { preview = new TextBlock { Text = "无法预览", Width = 88, Height = 88 }; }
                var button = new Button { Width = 104, Height = 124, Padding = new Thickness(5), BorderBrush = sticker.Id == selected?.Id ? ThemeAccentBrush() : Brush.Parse("#344452"), BorderThickness = new Thickness(sticker.Id == selected?.Id ? 2 : 1), Content = new StackPanel { Spacing = 4, Children = { preview, new TextBlock { Text = sticker.Name, MaxWidth = 92, TextTrimming = TextTrimming.CharacterEllipsis, HorizontalAlignment = HorizontalAlignment.Center }, new TextBlock { Text = sticker.Source == StickerSource.BuiltIn ? "内置" : sticker.InteractionRole, FontSize = 10, Foreground = Brush.Parse("#7F91A4"), HorizontalAlignment = HorizontalAlignment.Center } } } };
                button.Click += (_, _) => { selected = sticker; Refresh(); }; grid.Children.Add(button);
            }
        }
        Refresh();
        import.Click += async (_, _) => { var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "导入表情包", AllowMultiple = true, FileTypeFilter = [new FilePickerFileType("表情图片") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.gif"] }] }); foreach (var file in files) { var path = file.TryGetLocalPath(); if (path is not null) await _stickerCatalog.ImportAsync(path, Path.GetFileNameWithoutExtension(path), []); } Refresh(); };
        remove.Click += async (_, _) => { if (selected is { Source: StickerSource.User } && await ConfirmAsync(window, "删除表情", $"确定删除“{selected.Name}”吗？这会同时删除应用数据目录里的副本。")) { await _stickerCatalog.RemoveUserAsync(selected.Id); selected = null; Refresh(); } };
        edit.Click += async (_, _) => { if (selected is { Source: StickerSource.User }) { await EditStickerAsync(window, selected); selected = _stickerCatalog.Find(selected.Id); Refresh(); } }; close.Click += (_, _) => window.Close();
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
        add.Click += async (_, _) => { var result = await EditModelAsync(null); if (result is not null) { models.Add(result); Refresh(); } };
        edit.Click += async (_, _) => { if (list.SelectedItem is not ModelChoice choice) return; var index = models.FindIndex(x => x.Id == choice.Id); if (index < 0) return; var result = await EditModelAsync(models[index]); if (result is not null) { models[index] = result; Refresh(); } };
        remove.Click += async (_, _) => { if (list.SelectedItem is not ModelChoice choice || !await ConfirmAsync(window, "删除自定义模型", $"确定从配置中删除“{choice.Name}”吗？模型文件不会被删除。")) return; models.RemoveAll(x => x.Id == choice.Id); Refresh(); };
        done.Click += async (_, _) => { _preferences = _preferences with { CustomModels = models.ToArray() }; await JsonConfiguration.SaveAsync(PreferencesPath, _preferences); window.Close(); };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Children = { add, edit, remove, done } }; var modelLayout = new Grid { Margin = new Thickness(20), RowDefinitions = new RowDefinitions("Auto,*,Auto"), Children = { new TextBlock { Text = "内置模型不会出现在这里；自定义模型可长期保存多个。", TextWrapping = TextWrapping.Wrap }, list, buttons } }; window.Content = CreateThemedDialogSurface(modelLayout); Grid.SetRow(list, 1); Grid.SetRow(buttons, 2); await window.ShowDialog(owner);
    }

    private async Task EditStickerAsync(Window owner, StickerDefinition sticker)
    {
        var name = new TextBox { Text = sticker.Name }; var tags = new TextBox { Text = string.Join("，", sticker.Emotions) }; var role = new ComboBox { ItemsSource = new[] { "reaction", "backchannel", "topic" }, SelectedItem = sticker.InteractionRole }; var backchannel = new CheckBox { Content = "这个表情通常表示“我在听”", IsChecked = sticker.LikelyBackchannel }; var save = new Button { Content = "保存", HorizontalAlignment = HorizontalAlignment.Right };
        var editor = new Window { Title = "编辑表情", Width = 480, Height = 430, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        save.Click += async (_, _) => { var values = (tags.Text ?? "").Split(['，', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); await _stickerCatalog!.UpdateUserAsync(sticker.Id, name.Text ?? sticker.Name, values, role.SelectedItem?.ToString() ?? "reaction", backchannel.IsChecked == true); editor.Close(); };
        var form = new StackPanel { Spacing = 10, Children = { new TextBlock { Text = "名称" }, name, new TextBlock { Text = "情绪标签" }, tags, new TextBlock { Text = "互动作用" }, role, backchannel } };
        editor.Content = CreateThemedDialogSurface(CreateCompactDialogLayout(CreateCompactDialogTextCard(form), save)); await editor.ShowDialog(owner);
    }

    private async Task ShowNoticeAsync(string title, string message)
    {
        var window = new Window { Title = title, Width = 440, Height = 240, WindowStartupLocation = WindowStartupLocation.CenterOwner }; var close = new Button { Content = "知道了", HorizontalAlignment = HorizontalAlignment.Right }; close.Click += (_, _) => window.Close();
        var textCard = CreateCompactDialogTextCard(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center });
        window.Content = CreateThemedDialogSurface(CreateCompactDialogLayout(textCard, close)); await window.ShowDialog(this);
    }
    private async Task<bool> ConfirmAsync(Window owner, string title, string message)
    {
        var window = new Window { Title = title, Width = 460, Height = 250, WindowStartupLocation = WindowStartupLocation.CenterOwner }; var result = false;
        var cancel = new Button { Content = "取消（Esc）" }; var confirm = new Button { Content = "确定（Enter）", Classes = { "primary" } }; cancel.Click += (_, _) => window.Close(); confirm.Click += (_, _) => { result = true; window.Close(); };
        window.KeyDown += (_, args) => { if (args.Key == Key.Escape) { args.Handled = true; window.Close(); } else if (args.Key == Key.Enter) { args.Handled = true; result = true; window.Close(); } };
        window.Opened += (_, _) => confirm.Focus();
        var textCard = CreateCompactDialogTextCard(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center });
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Children = { cancel, confirm } };
        window.Content = CreateThemedDialogSurface(CreateCompactDialogLayout(textCard, actions)); await window.ShowDialog(owner); return result;
    }
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
    private sealed record ComputeChoice(string Id, string Name) { public override string ToString() => Name; }
    private sealed record CharacterChoice(string Id, string Name) { public override string ToString() => Name; }
    private sealed record ConversationRoomChoice(ConversationRoom Room)
    {
        public override string ToString() => $"{Room.Title}\n{Room.UpdatedAt.LocalDateTime:MM-dd HH:mm} · {Room.MessageCount} 条消息";
    }
    private sealed record ConversationFolderChoice(string Id, string Name);
}
