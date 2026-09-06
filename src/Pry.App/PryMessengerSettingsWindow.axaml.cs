using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Pry.Client;
using Pry.Contracts;
using Pry.Core.Models;

namespace Pry.App;

public sealed partial class PryMessengerSettingsWindow : Window
{
    private readonly PryBackendClient _api;
    private ClientPreferencesResponse? _preferences;
    private IReadOnlyList<ModelProfileResponse> _models = [];
    private IReadOnlyList<SpeechModelResponse> _speechModels = [];

    public PryMessengerSettingsWindow() : this(new PryBackendClient(new HttpClient
    {
        BaseAddress = new Uri("http://127.0.0.1:5078/"), Timeout = Timeout.InfiniteTimeSpan
    })) { }

    public PryMessengerSettingsWindow(PryBackendClient api)
    {
        _api = api;
        InitializeComponent();
        Opened += async (_, _) => await LoadAsync();
    }

    public bool Saved { get; private set; }

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
            var devices = await devicesTask;

            DisplayNameBox.Text = _preferences.UserProfile.DisplayName;
            SignatureBox.Text = _preferences.UserProfile.Signature;
            ThemeModeBox.ItemsSource = new[] { "system", "dark", "light" };
            ThemeModeBox.SelectedItem = _preferences.Theme.ThemeMode;
            AccentColorBox.Text = _preferences.Theme.AccentColor;
            GlassEffectsBox.IsChecked = _preferences.Theme.UseGlassEffects;
            var turn = _preferences.TurnTaking ?? new TurnTakingSettings();
            SplitRepliesBox.IsChecked = turn.SplitReplies;
            ListeningSignalsBox.IsChecked = turn.EnableListeningSignals;
            DebounceBox.Value = turn.DebounceMs;
            StyleInstructionBox.Text = turn.StyleInstruction;

            TextModelBox.ItemsSource = _models.Where(item => item.Capabilities.Text).Select(item => new ModelChoice(item.Id, item.DisplayName)).ToArray();
            VisionModelBox.ItemsSource = new[] { new ModelChoice("", "不单独指定") }.Concat(_models.Where(item => item.Capabilities.Vision).Select(item => new ModelChoice(item.Id, item.DisplayName))).ToArray();
            SpeechModelBox.ItemsSource = new[] { new ModelChoice("", "不启用语音识别") }.Concat(_speechModels.Where(item => item.Available).Select(item => new ModelChoice(item.Id, item.DisplayName))).ToArray();
            Select(TextModelBox, _preferences.ActiveModelId);
            Select(VisionModelBox, _preferences.ActiveVisionModelId);
            Select(SpeechModelBox, _preferences.ActiveSpeechModelId);
            RuntimeText.Text = runtime.State == "ready" ? "本地服务运行正常" : runtime.Error ?? runtime.State;
            DeviceText.Text = devices.Count == 0 ? "未发现可用计算设备" : "计算设备：" + string.Join("、", devices.Select(item => item.Name));
        }
        catch (Exception ex) { StatusText.Text = $"设置读取失败：{ex.Message}"; }
    }

    private static void Select(ComboBox box, string? id)
    {
        box.SelectedItem = box.ItemsSource?.Cast<ModelChoice>().FirstOrDefault(item => item.Id == (id ?? ""))
                           ?? box.ItemsSource?.Cast<ModelChoice>().FirstOrDefault();
    }

    private async void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (_preferences is null) return;
        var displayName = DisplayNameBox.Text?.Trim();
        var accent = AccentColorBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(displayName)) { StatusText.Text = "显示名称不能为空"; DisplayNameBox.Focus(); return; }
        if (!Regex.IsMatch(accent, "^#[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?$")) { StatusText.Text = "强调色必须使用 #RRGGBB 格式"; AccentColorBox.Focus(); return; }
        if (TextModelBox.SelectedItem is not ModelChoice textModel) { StatusText.Text = "请选择文字模型"; return; }

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
            var theme = _preferences.Theme with
            {
                ThemeMode = ThemeModeBox.SelectedItem?.ToString() ?? "system",
                AccentColor = accent,
                UseGlassEffects = GlassEffectsBox.IsChecked == true
            };
            var profile = _preferences.UserProfile with { DisplayName = displayName, Signature = SignatureBox.Text?.Trim() ?? "" };
            _preferences = await _api.UpdatePreferencesAsync(new UpdateClientPreferencesRequest(
                _preferences.SelectedCharacterId, null, profile, _preferences.DesktopPet, _preferences.Shortcuts, turn, theme));

            var visionId = (VisionModelBox.SelectedItem as ModelChoice)?.Id;
            var speechId = (SpeechModelBox.SelectedItem as ModelChoice)?.Id;
            _models = await _api.UpdateModelSelectionAsync(new UpdateModelSelectionRequest(
                textModel.Id, string.IsNullOrEmpty(visionId) ? null : visionId,
                string.IsNullOrEmpty(speechId) ? null : speechId, null));
            Saved = true;
            Close();
        }
        catch (Exception ex) { StatusText.Text = $"保存失败：{ex.Message}"; IsEnabled = true; }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();
    private sealed record ModelChoice(string Id, string Name) { public override string ToString() => Name; }
}
