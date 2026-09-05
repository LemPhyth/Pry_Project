using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Pry.Client;
using Pry.Contracts;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class SpeechModelManagerDialog(
    PryBackendClient api,
    Func<Control, Control> createSurface,
    Func<Window, string, string, Task<bool>> confirm)
{
    public async Task<IReadOnlyList<SpeechModelProfile>?> ShowAsync(
        Window owner,
        IEnumerable<SpeechModelProfile> source)
    {
        var state = new SpeechModelManagerState(source);
        IReadOnlyList<SpeechModelProfile>? result = null;
        var window = new Window
        {
            Title = "自定义语音识别模型",
            Width = 760,
            Height = 560,
            Background = Brushes.Transparent,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var list = new ListBox
        {
            Background = Brush.Parse("#17212B"),
            Margin = new Thickness(0, 10, 0, 10)
        };

        void Refresh() => list.ItemsSource = state.Items
            .Select(model => new SpeechModelChoice(model.Id, $"{model.DisplayName} · {model.Provider}"))
            .ToArray();

        Refresh();
        var add = new Button { Content = "添加" };
        var edit = new Button { Content = "编辑" };
        var remove = new Button { Content = "删除" };
        var done = new Button { Content = "完成", Classes = { "primary" } };

        add.Click += async (_, _) =>
        {
            var value = await EditAsync(window, null);
            if (value is null) return;
            var saved = await api.CreateSpeechModelAsync(ToRequest(value));
            state.Add(value, saved.Id);
            Refresh();
        };
        edit.Click += async (_, _) =>
        {
            if (list.SelectedItem is not SpeechModelChoice choice) return;
            var sourceModel = state.Find(choice.Id);
            if (sourceModel is null) return;
            var value = await EditAsync(window, sourceModel);
            if (value is null) return;
            await api.UpdateSpeechModelAsync(choice.Id, ToRequest(value));
            state.Update(choice.Id, value);
            Refresh();
        };
        remove.Click += async (_, _) =>
        {
            if (list.SelectedItem is not SpeechModelChoice choice ||
                !await confirm(window, "删除语音模型配置", $"确定删除“{choice.Name}”吗？本地模型文件不会被删除。")) return;
            await api.DeleteSpeechModelAsync(choice.Id);
            state.Remove(choice.Id);
            Refresh();
        };
        done.Click += (_, _) =>
        {
            result = state.Items.ToArray();
            window.Close();
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { add, edit, remove, done }
        };
        var layout = new Grid
        {
            Margin = new Thickness(22),
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            Children =
            {
                new TextBlock
                {
                    Text = "保留多个本地或 API 语音识别配置，在主设置页选择当前使用项。",
                    TextWrapping = TextWrapping.Wrap
                },
                list,
                buttons
            }
        };
        Grid.SetRow(list, 1);
        Grid.SetRow(buttons, 2);
        window.Content = createSurface(layout);
        await window.ShowDialog(owner);
        return result;
    }

    public static SaveSpeechModelRequest ToRequest(SpeechModelProfile value) => new(
        value.DisplayName, value.Provider, value.ModelName, value.ModelPath,
        value.BaseUrl, value.Language, value.SampleRate);

    private async Task<SpeechModelProfile?> EditAsync(Window owner, SpeechModelProfile? source)
    {
        var name = new TextBox { Text = source?.DisplayName ?? "我的语音模型" };
        var provider = new ComboBox
        {
            ItemsSource = new[] { "sherpa-onnx", "openai-compatible" },
            SelectedItem = source?.Provider ?? "sherpa-onnx"
        };
        var modelName = new TextBox { Text = source?.ModelName ?? "whisper-1" };
        var path = new TextBox { Text = source?.ModelPath ?? "" };
        var baseUrl = new TextBox { Text = source?.BaseUrl ?? "" };
        var language = new TextBox { Text = source?.Language ?? "zh" };
        var sampleRate = new NumericUpDown
        {
            Value = source?.SampleRate ?? 16000,
            Minimum = 8000,
            Maximum = 192000,
            Increment = 1000
        };
        var save = new Button
        {
            Content = "保存",
            Classes = { "primary" },
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var panel = new StackPanel { Margin = new Thickness(26), Spacing = 8 };

        void Field(string label, Control control)
        {
            panel.Children.Add(new TextBlock { Text = label, Foreground = Brush.Parse("#B8CCE0") });
            panel.Children.Add(control);
        }

        Field("显示名称", name);
        Field("类型", provider);
        Field("API model 名称", modelName);
        Field("本地模型目录", path);
        Field("转写 API 地址", baseUrl);
        Field("语言", language);
        Field("采样率", sampleRate);
        panel.Children.Add(new TextBlock
        {
            Text = "本地模型填写目录；API 模型填写 OpenAI-compatible 地址。密钥通过 PRY_SPEECH_API_KEY_<模型ID> 环境变量提供。",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush.Parse("#7F91A4")
        });
        panel.Children.Add(save);

        var editor = new Window
        {
            Title = source is null ? "添加语音模型" : "编辑语音模型",
            Width = 620,
            Height = 650,
            Background = Brushes.Transparent,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = createSurface(new ScrollViewer { Content = panel })
        };
        SpeechModelProfile? result = null;
        save.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(name.Text)) return;
            result = new SpeechModelProfile
            {
                Id = source?.Id ?? $"speech-{Guid.NewGuid():N}",
                DisplayName = name.Text.Trim(),
                Provider = provider.SelectedItem?.ToString() ?? "sherpa-onnx",
                ModelName = modelName.Text?.Trim() ?? "whisper-1",
                ModelPath = path.Text?.Trim() ?? "",
                BaseUrl = baseUrl.Text?.Trim() ?? "",
                Language = language.Text?.Trim() ?? "zh",
                SampleRate = (int)(sampleRate.Value ?? 16000)
            };
            editor.Close();
        };
        await editor.ShowDialog(owner);
        return result;
    }

    private sealed record SpeechModelChoice(string Id, string Name)
    {
        public override string ToString() => Name;
    }
}

public sealed class SpeechModelManagerState
{
    private readonly List<SpeechModelProfile> _items;

    public SpeechModelManagerState(IEnumerable<SpeechModelProfile> source) => _items = source.ToList();

    public IReadOnlyList<SpeechModelProfile> Items => _items;

    public SpeechModelProfile? Find(string id) => _items.FirstOrDefault(item => item.Id == id);

    public void Add(SpeechModelProfile value, string serverId) => _items.Add(value with { Id = serverId });

    public bool Update(string id, SpeechModelProfile value)
    {
        var index = _items.FindIndex(item => item.Id == id);
        if (index < 0) return false;
        _items[index] = value;
        return true;
    }

    public bool Remove(string id) => _items.RemoveAll(item => item.Id == id) > 0;
}
