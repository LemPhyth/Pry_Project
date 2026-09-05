using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class ModelSelectionEditor
{
    private readonly ComboBox _text = new();
    private readonly ComboBox _vision = new();
    private readonly ComboBox _speech = new();

    public ModelSelectionEditor(IReadOnlyList<ModelProfile> models, IReadOnlyList<SpeechModelProfile> speech,
        string? textId, string? visionId, string? speechId, SettingsUiFactory ui)
    {
        TextFields = new StackPanel { Spacing = 7 };
        ui.AddField(TextFields, "文字聊天模型", _text);
        ui.AddField(TextFields, "图片理解模型", _vision);
        TextFields.Children.Add(Description("原生多模态模型会同时出现在两个列表；选择不同模型时，图片先转为内部描述再交给文字模型。"));
        TextFields.Children.Add(ManageModels);
        SpeechFields = new StackPanel { Spacing = 7 };
        ui.AddField(SpeechFields, "语音识别模型", _speech);
        SpeechFields.Children.Add(ManageSpeech);
        SpeechFields.Children.Add(Description("点击聊天输入框旁的麦克风开始录音，再点一次结束；本地 SenseVoice 会直接转写到输入框。API 配置会按你的选择上传录音。"));
        SetChoices(_text, models.Where(x => x.Capabilities.Text).Select(x => new ModelSelectionOption(x.Id, x.DisplayName)), textId, false);
        SetChoices(_vision, models.Where(x => x.Capabilities.Vision).Select(x => new ModelSelectionOption(x.Id, x.DisplayName)), visionId, false);
        SetChoices(_speech, speech.Select(x => new ModelSelectionOption(x.Id, x.DisplayName)), speechId, false);
    }

    public StackPanel TextFields { get; }
    public StackPanel SpeechFields { get; }
    public Button ManageModels { get; } = new() { Content = "管理自定义对话模型…", HorizontalAlignment = HorizontalAlignment.Left };
    public Button ManageSpeech { get; } = new() { Content = "管理自定义语音模型…", HorizontalAlignment = HorizontalAlignment.Left };
    public string? TextModelId => (_text.SelectedItem as ModelSelectionOption)?.Id;
    public string? VisionModelId => (_vision.SelectedItem as ModelSelectionOption)?.Id;
    public string? SpeechModelId => (_speech.SelectedItem as ModelSelectionOption)?.Id;

    public void RefreshModels(IReadOnlyList<ModelProfile> models)
    {
        SetChoices(_text, models.Where(x => x.Capabilities.Text).Select(x => new ModelSelectionOption(x.Id, x.DisplayName)), TextModelId, true);
        SetChoices(_vision, models.Where(x => x.Capabilities.Vision).Select(x => new ModelSelectionOption(x.Id, x.DisplayName)), VisionModelId, true);
    }

    public void RefreshSpeech(IReadOnlyList<SpeechModelProfile> models, string? preferredId) =>
        SetChoices(_speech, models.Select(x => new ModelSelectionOption(x.Id, x.DisplayName)), preferredId, true);

    private static void SetChoices(ComboBox control, IEnumerable<ModelSelectionOption> options, string? selectedId, bool fallback)
    {
        var choices = options.ToArray();
        control.ItemsSource = choices;
        control.SelectedItem = choices.FirstOrDefault(x => x.Id == selectedId)
            ?? (fallback ? choices.FirstOrDefault() : null);
        control.IsEnabled = choices.Length > 0;
        control.PlaceholderText = choices.Length == 0 ? "暂无可用模型" : "请选择模型";
    }

    private static TextBlock Description(string text) => new()
    {
        Text = text, TextWrapping = TextWrapping.Wrap, Foreground = Brush.Parse("#7F91A4")
    };
}

public sealed record ModelSelectionOption(string Id, string Name)
{
    public override string ToString() => Name;
}
