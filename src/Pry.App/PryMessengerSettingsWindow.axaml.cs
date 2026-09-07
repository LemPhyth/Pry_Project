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

    public PryMessengerSettingsWindow() : this(new PryBackendClient(new HttpClient
    {
        BaseAddress = new Uri("http://127.0.0.1:5078/"), Timeout = Timeout.InfiniteTimeSpan
    })) { }

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
            _speechModels = await speechTask;
            var runtime = await runtimeTask;
            _devices = await devicesTask;

            DisplayNameBox.Text = _preferences.UserProfile.DisplayName;
            SignatureBox.Text = _preferences.UserProfile.Signature;
            ThemeModeBox.ItemsSource = new[] { "system", "dark", "light" };
            ThemeModeBox.SelectedItem = _preferences.Theme.ThemeMode;
            WindowStyleBox.ItemsSource = new[] { new LayoutChoice(MainWindowLayoutModes.Messenger, "新版 · Messenger"), new LayoutChoice(MainWindowLayoutModes.Card, "经典 · 卡片窗口") };
            WindowStyleBox.SelectedItem = WindowStyleBox.Items.Cast<LayoutChoice>().First(item => item.Id == _preferences.Theme.MainWindowLayoutMode);
            AccentColorBox.Text = _preferences.Theme.AccentColor;
            GlassEffectsBox.IsChecked = _preferences.Theme.UseGlassEffects;
            var turn = _preferences.TurnTaking ?? new TurnTakingSettings();
            SplitRepliesBox.IsChecked = turn.SplitReplies;
            ListeningSignalsBox.IsChecked = turn.EnableListeningSignals;
            DebounceBox.Value = turn.DebounceMs;
            StyleInstructionBox.Text = turn.StyleInstruction;
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
            }.Concat(_devices.Select(item => new DeviceChoice(item.Id,
                $"{item.Name}（{item.PerformanceTierId} · 推荐 {item.RecommendedContextSize / 1024}K）",
                item.RecommendedContextSize))).ToArray();
            TextModelBox.ItemsSource = _models.Where(item => item.Capabilities.Text).Select(item => new ModelChoice(item.Id, item.DisplayName)).ToArray();
            VisionModelBox.ItemsSource = new[] { new ModelChoice("", "不单独指定") }.Concat(_models.Where(item => item.Capabilities.Vision).Select(item => new ModelChoice(item.Id, item.DisplayName))).ToArray();
            SpeechModelBox.ItemsSource = new[] { new ModelChoice("", "不启用语音识别") }.Concat(_speechModels.Where(item => item.Available).Select(item => new ModelChoice(item.Id, item.DisplayName))).ToArray();
            Select(TextModelBox, _preferences.ActiveModelId);
            Select(VisionModelBox, _preferences.ActiveVisionModelId);
            Select(SpeechModelBox, _preferences.ActiveSpeechModelId);
            RuntimeText.Text = runtime.State == "ready" ? "本地服务运行正常" : runtime.Error ?? runtime.State;
            DeviceText.Text = _devices.Count == 0 ? "未发现可用计算设备" : "计算设备：" + string.Join("、", _devices.Select(item => item.Name));
            await LoadPreviewAsync(UserAvatarPreview, _preferences.UserAvatarUrl);
            await LoadPreviewAsync(BackgroundPreview, _preferences.BackgroundUrl);
        }
        catch (Exception ex) { StatusText.Text = $"设置读取失败：{ex.Message}"; }
    }

    private static void Select(ComboBox box, string? id)
    {
        box.SelectedItem = box.ItemsSource?.Cast<ModelChoice>().FirstOrDefault(item => item.Id == (id ?? ""))
                           ?? box.ItemsSource?.Cast<ModelChoice>().FirstOrDefault();
    }

    private void TextModel_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (TextModelBox.SelectedItem is not ModelChoice choice || _models.FirstOrDefault(item => item.Id == choice.Id) is not { } model) return;
        TemperatureBox.Value = (decimal)model.Temperature; MaxOutputBox.Value = model.MaxOutputTokens; ContextSizeBox.Value = model.ContextSize; GpuLayersBox.Value = model.GpuLayers; ThinkingBox.IsChecked = model.EnableThinking;
        var configuredDevice = string.IsNullOrWhiteSpace(model.ComputeDevice) ? "auto-discrete" : model.ComputeDevice;
        ComputeDeviceBox.SelectedItem = ComputeDeviceBox.ItemsSource?.Cast<DeviceChoice>().FirstOrDefault(item => item.Id == configuredDevice)
                                        ?? ComputeDeviceBox.ItemsSource?.Cast<DeviceChoice>().FirstOrDefault();
        if (ComputeDeviceBox.SelectedItem is DeviceChoice { RecommendedContextSize: int recommended } &&
            ContextSizeBox.Value > recommended) ContextSizeBox.Value = recommended;
    }

    private void AdvancedSettingsToggle_Changed(object? sender, RoutedEventArgs e)
    {
        var expanded = AdvancedSettingsToggle.IsChecked == true;
        AdvancedSettingsPanel.IsVisible = expanded;
        AdvancedSettingsChevron.Text = expanded ? "\uE70E" : "\uE70D";
    }

    private async void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (_preferences is null) return;
        var displayName = DisplayNameBox.Text?.Trim();
        var accent = AccentColorBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(displayName)) { StatusText.Text = "显示名称不能为空"; DisplayNameBox.Focus(); return; }
        if (!Regex.IsMatch(accent, "^#[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?$")) { StatusText.Text = "强调色必须使用 #RRGGBB 格式"; AccentColorBox.Focus(); return; }
        if (TextModelBox.SelectedItem is not ModelChoice textModel) { StatusText.Text = "请选择文字模型"; return; }
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
            var selectedLayout = (WindowStyleBox.SelectedItem as LayoutChoice)?.Id ?? MainWindowLayoutModes.Messenger;
            LayoutChanged = selectedLayout != _preferences.Theme.MainWindowLayoutMode;
            var theme = _preferences.Theme with
            {
                ThemeMode = ThemeModeBox.SelectedItem?.ToString() ?? "system",
                AccentColor = accent,
                UseGlassEffects = GlassEffectsBox.IsChecked == true,
                MainWindowLayoutMode = selectedLayout
            };
            var profile = _preferences.UserProfile with { DisplayName = displayName, Signature = SignatureBox.Text?.Trim() ?? "" };
            var visionId = (VisionModelBox.SelectedItem as ModelChoice)?.Id;
            var speechId = (SpeechModelBox.SelectedItem as ModelChoice)?.Id;
            var tuning = new ModelTuningPreferences { ActiveModelId = textModel.Id, Temperature = (double?)TemperatureBox.Value, MaxOutputTokens = (int?)MaxOutputBox.Value, ContextSize = (int?)ContextSizeBox.Value, GpuLayers = (int?)GpuLayersBox.Value, ComputeDevice = (ComputeDeviceBox.SelectedItem as DeviceChoice)?.Id, EnableThinking = ThinkingBox.IsChecked == true };
            var saved = await _api.SaveSettingsAsync(new SaveSettingsRequest(
                new UpdateClientPreferencesRequest(_preferences.SelectedCharacterId, null, profile, _preferences.DesktopPet, shortcuts, turn, theme),
                new UpdateAppearanceMediaRequest(_backgroundMediaId, _clearBackground, _userAvatarMediaId, _clearUserAvatar, null, null),
                new UpdateModelSelectionRequest(textModel.Id, string.IsNullOrEmpty(visionId) ? null : visionId,
                    string.IsNullOrEmpty(speechId) ? null : speechId, new Dictionary<string, ModelTuningPreferences> { [textModel.Id] = tuning })));
            _preferences = saved.Preferences; _models = saved.Models;
            Saved = true;
            CloseRequested?.Invoke();
        }
        catch (Exception ex) { StatusText.Text = $"保存失败：{ex.Message}"; IsEnabled = true; }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => CloseRequested?.Invoke();
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
