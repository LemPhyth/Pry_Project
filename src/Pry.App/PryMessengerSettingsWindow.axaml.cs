using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Pry.Client;
using Pry.Contracts;
using Pry.Core.Models;
using Pry.App.Services;

namespace Pry.App;

public sealed partial class PryMessengerSettingsWindow : UserControl
{
    private readonly PryBackendClient _api;
    private ClientPreferencesResponse? _preferences;
    private IReadOnlyList<ModelProfileResponse> _models = [];
    private IReadOnlyList<SpeechModelResponse> _speechModels = [];
    private IReadOnlyList<ComputeDeviceResponse> _devices = [];
    private string? _userAvatarMediaId;
    private string? _backgroundMediaId;
    private bool _clearUserAvatar;
    private bool _clearBackground;
    private readonly Dictionary<string, ModelTuningPreferences> _tuningDrafts = new();
    private readonly Dictionary<string, ModelTuningPreferences> _initialTunings = new();
    private string? _editingModelId;
    private bool _refreshingChoices;

    public PryMessengerSettingsWindow() : this(PryBackendClient.Unattached()) { }

    public PryMessengerSettingsWindow(PryBackendClient api)
    {
        _api = api;
        InitializeComponent();
        ContextSizeBox.Spinned += (_, args) =>
        {
            args.Handled = true;
            ContextSizeBox.Value = ModelContextSteps.Move((int)(ContextSizeBox.Value ?? 4096),
                args.Direction == SpinDirection.Increase);
        };
        AttachedToVisualTree += async (_, _) => await LoadAsync();
    }

    public bool Saved { get; private set; }
    public bool LayoutChanged { get; private set; }
    public event Action? CloseRequested;

    private async Task LoadAsync()
    {
        try
        {
            var preferencesTask = _api.GetPreferencesAsync();
            var modelsTask = _api.GetModelsAsync();
            var speechTask = _api.GetSpeechModelsAsync();
            var runtimeTask = _api.GetRuntimeAsync();
            var devicesTask = _api.GetComputeDevicesAsync();
            await Task.WhenAll(preferencesTask, modelsTask, speechTask, runtimeTask, devicesTask);
            _preferences = await preferencesTask;
            _models = await modelsTask;
            foreach (var model in _models) _tuningDrafts[model.Id] = new ModelTuningPreferences
            { ActiveModelId = model.Id, Temperature = model.Temperature, ContextSize = model.ContextSize, MaxOutputTokens = model.MaxOutputTokens,
                GpuLayers = model.GpuLayers, ComputeDevice = model.ComputeDevice, EnableThinking = model.EnableThinking };
            foreach (var (id, tuning) in _tuningDrafts) _initialTunings[id] = tuning;
            _speechModels = await speechTask;
            var runtime = await runtimeTask;
            _devices = await devicesTask;

            DisplayNameBox.Text = _preferences.UserProfile.DisplayName;
            SignatureBox.Text = _preferences.UserProfile.Signature;
            ThemeModeBox.ItemsSource = new[] {
                new ComboBoxItem { Tag = "system", Content = "跟随系统" },
                new ComboBoxItem { Tag = "dark", Content = "深色" },
                new ComboBoxItem { Tag = "light", Content = "浅色" } };
            ThemeModeBox.SelectedItem = ThemeModeBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => (string?)item.Tag == _preferences.Theme.ThemeMode);
            AccentColorBox.Text = _preferences.Theme.AccentColor;
            GlassEffectsBox.IsChecked = _preferences.Theme.UseGlassEffects;
            BackgroundOpacityBox.Value = _preferences.Theme.BackgroundImageOpacity;
            BackgroundDimBox.Value = _preferences.Theme.BackgroundDimOpacity;
            BackgroundBlurBox.Value = _preferences.Theme.BackgroundBlurRadius;
            var turn = _preferences.TurnTaking ?? new TurnTakingSettings();
            SplitRepliesBox.IsChecked = turn.SplitReplies;
            ListeningSignalsBox.IsChecked = turn.EnableListeningSignals;
            DebounceBox.Value = turn.DebounceMs;
            StyleInstructionBox.Text = turn.StyleInstruction;
            ExplicitFeedbackBox.IsChecked = _preferences.ExplicitExpressionFeedback;
            SendShortcutBox.Text = _preferences.Shortcuts.Send;
            SendImmediatelyShortcutBox.Text = _preferences.Shortcuts.SendImmediately;
            NewLineShortcutBox.Text = _preferences.Shortcuts.NewLine;
            CancelReplyShortcutBox.Text = _preferences.Shortcuts.CancelReply;
            NewConversationShortcutBox.Text = _preferences.Shortcuts.NewConversation;
            OpenStickersShortcutBox.Text = _preferences.Shortcuts.OpenStickers;
            OpenCharacterShortcutBox.Text = _preferences.Shortcuts.OpenCharacterEditor;

            var automaticDevice = PreferredAutomaticDevice(_devices);
            ComputeDeviceBox.ItemsSource = new[]
            {
                new DeviceChoice("auto-discrete", automaticDevice is null ? "自动选择独立显卡" :
                    $"自动 · {automaticDevice.Name}（推荐 {automaticDevice.RecommendedContextSize / 1024}K）",
                    automaticDevice?.RecommendedContextSize)
            }.Append(new DeviceChoice("remote", "网络服务（不使用本地 GPU）")).Concat(_devices.Select(item => new DeviceChoice(item.Id,
                $"{item.Name}（{item.PerformanceTierId} · 推荐 {item.RecommendedContextSize / 1024}K）",
                item.RecommendedContextSize))).ToArray();
            TextModelBox.ItemsSource = _models.Where(item => item.Capabilities.Text).Select(item => new ModelChoice(item.Id, item.DisplayName)).ToArray();
            VisionModelBox.ItemsSource = new[] { new ModelChoice("", "不单独指定") }.Concat(_models.Where(item => item.Capabilities.Vision).Select(item => new ModelChoice(item.Id, item.DisplayName))).ToArray();
            SpeechModelBox.ItemsSource = new[] { new ModelChoice("", "不启用语音识别") }.Concat(_speechModels.Where(item => item.Available).Select(item => new ModelChoice(item.Id, item.DisplayName))).ToArray();
            var textModelId = _preferences.ActiveModelId ?? _models.FirstOrDefault(model => model.SelectedForText)?.Id;
            var visionModelId = _preferences.ActiveVisionModelId ?? _models.FirstOrDefault(model => model.SelectedForVision)?.Id;
            Select(TextModelBox, textModelId);
            Select(VisionModelBox, visionModelId);
            Select(SpeechModelBox, _preferences.ActiveSpeechModelId);
            var library = new ModelLibraryPanel(_api, _models);
            library.ModelsChanged += models =>
            {
                CaptureTuningDraft();
                var textId = Choice(TextModelBox)?.Id;
                var visionId = Choice(VisionModelBox)?.Id;
                _models = models; _refreshingChoices = true;
                PopulateModelChoices(); Select(TextModelBox, textId); Select(VisionModelBox, visionId);
                _refreshingChoices = false;
            };
            ModelLibraryHost.Content = library;
            _refreshingChoices = true;
            PopulateModelChoices(); Select(TextModelBox, textModelId); Select(VisionModelBox, visionModelId);
            _refreshingChoices = false;
            RuntimeText.Text = runtime.State == "ready" ? "本地服务运行正常" : runtime.Error ?? runtime.State;
            DeviceText.Text = _devices.Count == 0 ? "未发现可用计算设备" : "计算设备：" + string.Join("、", _devices.Select(item => item.Name));
            await LoadPreviewAsync(UserAvatarPreview, _preferences.UserAvatarUrl);
            await LoadPreviewAsync(BackgroundPreview, _preferences.BackgroundUrl);
        }
        catch (Exception ex) { StatusText.Text = $"设置读取失败：{ex.Message}"; }
    }

    private static ModelChoice? Choice(ComboBox box) => box.SelectedItem as ModelChoice ?? (box.SelectedItem as ComboBoxItem)?.Content as ModelChoice;

    private static void Select(ComboBox box, string? id)
    {
        box.SelectedItem = box.ItemsSource?.Cast<object>().FirstOrDefault(item => (item as ModelChoice ?? (item as ComboBoxItem)?.Content as ModelChoice)?.Id == (id ?? ""));
    }

    private void TextModel_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_refreshingChoices) return;
        CaptureTuningDraft();
        if (Choice(TextModelBox) is not ModelChoice choice || _models.FirstOrDefault(item => item.Id == choice.Id) is not { } model) return;
        LoadTuning(model);
    }

    private void TuningModel_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_refreshingChoices) return;
        CaptureTuningDraft();
        if (Choice(TuningModelBox) is { } choice && _models.FirstOrDefault(x => x.Id == choice.Id) is { } model) LoadTuning(model);
    }

    private void LoadTuning(ModelProfileResponse model)
    {
        _editingModelId = model.Id;
        if (_tuningDrafts.TryGetValue(model.Id, out var draft))
            model = model with { Temperature = draft.Temperature ?? model.Temperature, MaxOutputTokens = draft.MaxOutputTokens ?? model.MaxOutputTokens,
                ContextSize = draft.ContextSize ?? model.ContextSize, GpuLayers = draft.GpuLayers ?? model.GpuLayers,
                ComputeDevice = draft.ComputeDevice ?? model.ComputeDevice, EnableThinking = draft.EnableThinking ?? model.EnableThinking };
        TemperatureBox.Value = (decimal)model.Temperature; MaxOutputBox.Value = model.MaxOutputTokens; ContextSizeBox.Value = model.ContextSize; GpuLayersBox.Value = model.GpuLayers; ThinkingBox.IsChecked = model.EnableThinking;
        var configuredDevice = string.IsNullOrWhiteSpace(model.ComputeDevice) ? "auto-discrete" : model.ComputeDevice;
        ComputeDeviceBox.SelectedItem = ComputeDeviceBox.ItemsSource?.Cast<DeviceChoice>().FirstOrDefault(item => item.Id == configuredDevice)
                                        ?? ComputeDeviceBox.ItemsSource?.Cast<DeviceChoice>().FirstOrDefault();
    }

    private static ComboBoxItem ModelItem(ModelProfileResponse model) => new()
    {
        Content = new ModelChoice(model.Id, $"{model.DisplayName} · {model.ModelName}" + (model.Available ? "" : "（文件不可用）")),
        IsEnabled = model.Available
    };

    private void PopulateModelChoices()
    {
        TuningModelBox.ItemsSource = _models.Where(x => x.Capabilities.Text || x.Capabilities.Vision).Select(ModelItem).ToArray();
        Select(TuningModelBox, _editingModelId);
        TextModelBox.ItemsSource = _models.Where(x => x.Capabilities.Text).Select(ModelItem).ToArray();
        VisionModelBox.ItemsSource = new[] { new ComboBoxItem { Content = new ModelChoice("", "不单独指定") } }.Concat(_models.Where(x => x.Capabilities.Vision).Select(ModelItem)).ToArray();
    }

    private void CaptureTuningDraft()
    {
        if (_editingModelId is not { } id) return;
        _tuningDrafts[id] = new ModelTuningPreferences { ActiveModelId = id, Temperature = (double?)TemperatureBox.Value,
            MaxOutputTokens = (int?)MaxOutputBox.Value, ContextSize = (int?)ContextSizeBox.Value, GpuLayers = (int?)GpuLayersBox.Value,
            ComputeDevice = (ComputeDeviceBox.SelectedItem as DeviceChoice)?.Id, EnableThinking = ThinkingBox.IsChecked == true };
    }

    private void AdvancedSettingsToggle_Changed(object? sender, RoutedEventArgs e)
    {
        var expanded = AdvancedSettingsToggle.IsChecked == true;
        AdvancedSettingsPanel.IsVisible = expanded;
        AdvancedSettingsChevron.Text = expanded ? "\uE70E" : "\uE70D";
    }

    private void ExplicitFeedbackToggle_Changed(object? sender, RoutedEventArgs e)
    {
        var expanded = ExplicitFeedbackToggle.IsChecked == true;
        ExplicitFeedbackPanel.IsVisible = expanded;
        ExplicitFeedbackChevron.Text = expanded ? "\uE70E" : "\uE70D";
    }

    private async void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (_preferences is null) return;
        var displayName = DisplayNameBox.Text?.Trim();
        var accent = AccentColorBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(displayName)) { StatusText.Text = "显示名称不能为空"; DisplayNameBox.Focus(); return; }
        if (!Regex.IsMatch(accent, "^#[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?$")) { StatusText.Text = "强调色必须使用 #RRGGBB 格式"; AccentColorBox.Focus(); return; }
        if (Choice(TextModelBox) is not ModelChoice textModel) { StatusText.Text = "请选择文字模型"; return; }
        if (_models.Any(x => !x.Available && (x.Id == textModel.Id || x.Id == Choice(VisionModelBox)?.Id)))
        { StatusText.Text = "所选模型文件不可用，请恢复文件并刷新或选择其他模型"; return; }
        var shortcuts = new ShortcutSettings
        {
            Send = Shortcut(SendShortcutBox, "Enter"),
            SendImmediately = Shortcut(SendImmediatelyShortcutBox, "Ctrl+Enter"),
            NewLine = Shortcut(NewLineShortcutBox, "Shift+Enter"),
            CancelReply = Shortcut(CancelReplyShortcutBox, "Escape"),
            NewConversation = Shortcut(NewConversationShortcutBox, "Ctrl+N"),
            OpenStickers = Shortcut(OpenStickersShortcutBox, "Ctrl+E"),
            OpenCharacterEditor = Shortcut(OpenCharacterShortcutBox, "Ctrl+Shift+C")
        };
        if (SettingsDraftService.ValidateShortcuts(shortcuts) is { } shortcutError)
        {
            StatusText.Text = shortcutError.Message; return;
        }

        try
        {
            IsEnabled = false;
            StatusText.Text = "正在保存…";
            var oldTurn = _preferences.TurnTaking ?? new TurnTakingSettings();
            var turn = oldTurn with
            {
                SplitReplies = SplitRepliesBox.IsChecked == true,
                EnableListeningSignals = ListeningSignalsBox.IsChecked == true,
                DebounceMs = (int)(DebounceBox.Value ?? oldTurn.DebounceMs),
                StyleInstruction = StyleInstructionBox.Text?.Trim() ?? ""
            };
            var selectedLayout = _preferences.Theme.MainWindowLayoutMode;
            LayoutChanged = false;
            var theme = _preferences.Theme with
            {
                ThemeMode = (ThemeModeBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "system",
                AccentColor = accent,
                UseGlassEffects = GlassEffectsBox.IsChecked == true,
                BackgroundImageOpacity = BackgroundOpacityBox.Value,
                BackgroundDimOpacity = BackgroundDimBox.Value,
                BackgroundBlurRadius = BackgroundBlurBox.Value,
                MainWindowLayoutMode = selectedLayout
            };
            var profile = _preferences.UserProfile with { DisplayName = displayName, Signature = SignatureBox.Text?.Trim() ?? "" };
            var visionId = Choice(VisionModelBox)?.Id;
            var speechId = Choice(SpeechModelBox)?.Id;
            CaptureTuningDraft();
            var saved = await _api.SaveSettingsAsync(new SaveSettingsRequest(
                new UpdateClientPreferencesRequest(_preferences.SelectedCharacterId, null, profile, _preferences.DesktopPet, shortcuts, turn, theme, ExplicitFeedbackBox.IsChecked == true),
                new UpdateAppearanceMediaRequest(_backgroundMediaId, _clearBackground, _userAvatarMediaId, _clearUserAvatar, null, null),
                new UpdateModelSelectionRequest(textModel.Id, string.IsNullOrEmpty(visionId) ? null : visionId,
                    string.IsNullOrEmpty(speechId) ? null : speechId, HasTuningChanges() ?
                        _tuningDrafts.Where(pair => _models.Any(model => model.Id == pair.Key)).ToDictionary() : null)));
            _preferences = saved.Preferences; _models = saved.Models;
            Saved = true;
            CloseRequested?.Invoke();
        }
        catch (Exception ex) { StatusText.Text = $"保存失败：{ex.Message}"; IsEnabled = true; }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => CloseRequested?.Invoke();
    private bool HasTuningChanges() => _tuningDrafts.Any(pair => !_initialTunings.TryGetValue(pair.Key, out var initial) || initial != pair.Value);
    private static string Shortcut(TextBox input, string fallback) => string.IsNullOrWhiteSpace(input.Text) ? fallback : input.Text.Trim();
    private sealed record ModelChoice(string Id, string Name) { public override string ToString() => Name; }
    private sealed record LayoutChoice(string Id, string Name) { public override string ToString() => Name; }
    internal static ComputeDeviceResponse? PreferredAutomaticDevice(IEnumerable<ComputeDeviceResponse> devices) =>
        devices.FirstOrDefault(device => !device.IsIntegrated) ?? devices.FirstOrDefault();

    private sealed record DeviceChoice(string Id, string Name, int? RecommendedContextSize = null) { public override string ToString() => Name; }

    private async void ChooseUserAvatar_Click(object? sender, RoutedEventArgs e) => await ChooseImageAsync(true);
    private async void ChooseBackground_Click(object? sender, RoutedEventArgs e) => await ChooseImageAsync(false);
    private void ClearUserAvatar_Click(object? sender, RoutedEventArgs e) { _clearUserAvatar = true; _userAvatarMediaId = null; UserAvatarPreview.Source = null; UserAvatarPreview.IsVisible = false; }
    private void ClearBackground_Click(object? sender, RoutedEventArgs e) { _clearBackground = true; _backgroundMediaId = null; BackgroundPreview.Source = null; BackgroundPreview.IsVisible = false; }
    private async Task ChooseImageAsync(bool avatar)
    {
        var storage = TopLevel.GetTopLevel(this)?.StorageProvider; if (storage is null) return;
        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions { Title = avatar ? "选择用户头像" : "选择聊天背景", AllowMultiple = false, FileTypeFilter = new[] { FilePickerFileTypes.ImageAll } });
        var file = files.FirstOrDefault(); var path = file?.TryGetLocalPath(); if (file is null || path is null) return;
        try
        {
            await using var stream = File.OpenRead(path); var media = await _api.UploadAsync(stream, file.Name, Mime(path));
            var preview = avatar ? UserAvatarPreview : BackgroundPreview; preview.Source = new Bitmap(path); preview.IsVisible = true;
            if (avatar) { _userAvatarMediaId = media.Id; _clearUserAvatar = false; } else { _backgroundMediaId = media.Id; _clearBackground = false; }
        }
        catch (Exception ex) { StatusText.Text = $"图片读取失败：{ex.Message}"; }
    }
    private async Task LoadPreviewAsync(Image image, string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try { var content = await _api.DownloadAsync(url); image.Source = new Bitmap(new MemoryStream(content.Bytes)); image.IsVisible = true; } catch { }
    }
    private static string Mime(string path) => Path.GetExtension(path).ToLowerInvariant() switch { ".png" => "image/png", ".webp" => "image/webp", _ => "image/jpeg" };
}
