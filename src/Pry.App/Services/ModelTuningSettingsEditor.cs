using Avalonia.Controls;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class ModelTuningSettingsEditor
{
    private readonly IReadOnlyList<ComputeDeviceChoice> _computeChoices;
    private readonly Dictionary<string, ModelTuningPreferences> _drafts;
    private IReadOnlyList<ModelProfile> _profiles;
    private readonly ComboBox _model = new();
    private readonly CheckBox _thinking = new() { Content = "启用思考模式" };
    private readonly NumericUpDown _temperature;
    private readonly NumericUpDown _outputTokens;
    private readonly NumericUpDown _contextSize;
    private readonly NumericUpDown _gpuLayers;
    private readonly ComboBox _computeDevice;
    private bool _loading;

    public ModelTuningSettingsEditor(IReadOnlyList<ModelProfile> profiles,
        IReadOnlyList<ComputeDeviceChoice> computeChoices, string initialModelId,
        IReadOnlyDictionary<string, ModelTuningPreferences> initialDrafts, SettingsUiFactory ui)
    {
        _profiles = profiles;
        _computeChoices = computeChoices;
        _drafts = initialDrafts.ToDictionary(item => item.Key, item => item.Value);
        _temperature = ui.CreateNumber(.8m, 0, 2, .05m);
        _outputTokens = ui.CreateNumber(512, 32, 8192, 32);
        _contextSize = ui.CreateNumber(4096, 512, 131072, 512);
        _gpuLayers = ui.CreateNumber(0, 0, 999);
        _computeDevice = new ComboBox { ItemsSource = computeChoices };
        Panel = new StackPanel { Spacing = 7 };
        ui.AddField(Panel, "参数配置对象", _model);
        Panel.Children.Add(_thinking);
        ui.AddField(Panel, "Temperature（随机性）", _temperature);
        ui.AddField(Panel, "最大输出 Token", _outputTokens);
        ui.AddField(Panel, "上下文长度", _contextSize);
        ui.AddField(Panel, "计算设备", _computeDevice);
        ui.AddField(Panel, "GPU 卸载层数（999 表示尽可能全部）", _gpuLayers);
        Panel.Children.Add(new TextBlock
        {
            Text = "自动模式优先选择独立显卡，不会优先使用 Intel 核显；每个文字或图片模型分别保存自己的选择。",
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Foreground = Avalonia.Media.Brush.Parse("#7F91A4")
        });
        _computeDevice.SelectionChanged += (_, _) => UpdateComputeState();
        _model.SelectionChanged += (_, _) => SwitchModel();
        UpdateProfiles(profiles, initialModelId);
    }

    public StackPanel Panel { get; }
    public string CurrentModelId { get; private set; } = "";

    public void SaveCurrent()
    {
        if (_loading || string.IsNullOrWhiteSpace(CurrentModelId)) return;
        _drafts[CurrentModelId] = new ModelTuningPreferences
        {
            EnableThinking = _thinking.IsChecked == true,
            Temperature = (double)(_temperature.Value ?? .8m),
            MaxOutputTokens = (int)(_outputTokens.Value ?? 512),
            ContextSize = (int)(_contextSize.Value ?? 4096),
            GpuLayers = (int)(_gpuLayers.Value ?? 999),
            ComputeDevice = (_computeDevice.SelectedItem as ComputeDeviceChoice)?.Id ?? "auto-discrete"
        };
    }

    public IReadOnlyDictionary<string, ModelTuningPreferences> BuildDrafts()
    {
        SaveCurrent();
        return new Dictionary<string, ModelTuningPreferences>(_drafts);
    }

    public void UpdateProfiles(IReadOnlyList<ModelProfile> profiles, string? preferredModelId = null)
    {
        SaveCurrent();
        _profiles = profiles;
        var choices = profiles.Select(profile => new ModelParameterChoice(profile.Id, profile.DisplayName)).ToArray();
        _loading = true;
        _model.ItemsSource = choices;
        _model.SelectedItem = choices.FirstOrDefault(choice => choice.Id == preferredModelId)
            ?? choices.FirstOrDefault(choice => choice.Id == CurrentModelId)
            ?? choices.FirstOrDefault();
        CurrentModelId = (_model.SelectedItem as ModelParameterChoice)?.Id ?? "";
        LoadCurrent();
        _loading = false;
    }

    private void SwitchModel()
    {
        if (_loading || _model.SelectedItem is not ModelParameterChoice choice || choice.Id == CurrentModelId) return;
        SaveCurrent();
        CurrentModelId = choice.Id;
        LoadCurrent();
    }

    private void LoadCurrent()
    {
        var profile = _profiles.FirstOrDefault(item => item.Id == CurrentModelId);
        if (profile is null) return;
        var tuning = _drafts.GetValueOrDefault(CurrentModelId);
        _loading = true;
        _thinking.IsChecked = tuning?.EnableThinking ?? profile.EnableThinking;
        _temperature.Value = (decimal)(tuning?.Temperature ?? profile.Temperature);
        _outputTokens.Value = tuning?.MaxOutputTokens ?? profile.MaxOutputTokens;
        _contextSize.Value = tuning?.ContextSize ?? profile.ContextSize;
        var requestedDevice = tuning?.ComputeDevice ?? profile.ComputeDevice;
        _computeDevice.SelectedItem = _computeChoices.FirstOrDefault(item => item.Id == requestedDevice)
            ?? _computeChoices.FirstOrDefault();
        _gpuLayers.Value = requestedDevice == "cpu" ? 0
            : tuning?.GpuLayers is > 0 ? tuning.GpuLayers
            : profile.GpuLayers > 0 ? profile.GpuLayers : 999;
        _gpuLayers.IsEnabled = requestedDevice != "cpu";
        _loading = false;
    }

    private void UpdateComputeState()
    {
        if (_loading) return;
        var cpu = (_computeDevice.SelectedItem as ComputeDeviceChoice)?.Id == "cpu";
        _gpuLayers.IsEnabled = !cpu;
        if (cpu) _gpuLayers.Value = 0;
        else if (_gpuLayers.Value <= 0) _gpuLayers.Value = 999;
    }
}

public sealed record ComputeDeviceChoice(string Id, string Name)
{
    public override string ToString() => Name;
}

public sealed record ModelParameterChoice(string Id, string Name)
{
    public override string ToString() => Name;
}
