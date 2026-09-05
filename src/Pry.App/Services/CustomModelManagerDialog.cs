using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Pry.Client;
using Pry.Contracts;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class CustomModelManagerDialog(
    PryBackendClient api,
    Func<Control, Control> createSurface,
    Func<Window, string, string, Task<bool>> confirm)
{
    public async Task<IReadOnlyList<ModelProfile>?> ShowAsync(Window owner, IEnumerable<ModelProfile> source)
    {
        var state = new CustomModelManagerState(source);
        IReadOnlyList<ModelProfile>? result = null;
        var window = new Window
        {
            Title = "自定义模型", Width = 700, Height = 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var list = new ListBox();
        void Refresh() => list.ItemsSource = state.Items
            .Select(model => new ModelChoice(model.Id, $"{model.DisplayName} · {model.Provider} · {model.ModelName}"))
            .ToArray();
        Refresh();

        var add = new Button { Content = "添加" };
        var edit = new Button { Content = "编辑" };
        var remove = new Button { Content = "删除" };
        var done = new Button { Content = "完成", Classes = { "primary" } };
        add.Click += async (_, _) =>
        {
            var draft = await EditAsync(window, null);
            if (draft is null) return;
            var saved = await api.CreateCustomModelAsync(ToRequest(draft));
            state.Add(draft, saved.Id);
            Refresh();
        };
        edit.Click += async (_, _) =>
        {
            if (list.SelectedItem is not ModelChoice choice || state.Find(choice.Id) is not { } current) return;
            var draft = await EditAsync(window, current);
            if (draft is null) return;
            await api.UpdateCustomModelAsync(choice.Id, ToRequest(draft));
            state.Update(choice.Id, draft);
            Refresh();
        };
        remove.Click += async (_, _) =>
        {
            if (list.SelectedItem is not ModelChoice choice ||
                !await confirm(window, "删除自定义模型", $"确定从配置中删除“{choice.Name}”吗？模型文件不会被删除。")) return;
            await api.DeleteCustomModelAsync(choice.Id);
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
            Orientation = Orientation.Horizontal, Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { add, edit, remove, done }
        };
        var layout = new Grid
        {
            Margin = new Thickness(20), RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            Children =
            {
                new TextBlock { Text = "内置模型不会出现在这里；自定义模型可长期保存多个。", TextWrapping = TextWrapping.Wrap },
                list, buttons
            }
        };
        Grid.SetRow(list, 1);
        Grid.SetRow(buttons, 2);
        window.Content = createSurface(layout);
        await window.ShowDialog(owner);
        return result;
    }

    public static SaveCustomModelRequest ToRequest(ModelProfile value) => new(
        value.DisplayName, value.Provider, value.ModelName, value.BaseUrl,
        string.IsNullOrWhiteSpace(value.ModelPath) ? null : value.ModelPath,
        string.IsNullOrWhiteSpace(value.MmprojPath) ? null : value.MmprojPath,
        value.Capabilities, value.ContextSize, value.MaxOutputTokens, value.Temperature,
        value.GpuLayers, value.ComputeDevice, value.EnableThinking);

    public static bool HasRequiredFields(string? displayName, string? provider, string? modelName, string? baseUrl) =>
        !string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(modelName) &&
        (provider != "openai-compatible" || !string.IsNullOrWhiteSpace(baseUrl));

    private async Task<ModelProfile?> EditAsync(Window owner, ModelProfile? source)
    {
        var name = new TextBox { Text = source?.DisplayName ?? "我的模型" };
        var provider = new ComboBox
        {
            ItemsSource = new[] { "local-llama", "openai-compatible" },
            SelectedItem = source?.Provider ?? "local-llama"
        };
        var modelName = new TextBox { Text = source?.ModelName ?? "model" };
        var baseUrl = new TextBox { Text = source?.BaseUrl ?? "http://127.0.0.1:8080/v1" };
        var modelPath = new TextBox { Text = source?.ModelPath ?? "" };
        var mmproj = new TextBox { Text = source?.MmprojPath ?? "" };
        var vision = new CheckBox { Content = "支持图片输入", IsChecked = source?.Capabilities.Vision == true };
        var save = new Button
        {
            Content = "保存", Classes = { "primary" },
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var panel = new StackPanel { Margin = new Thickness(24), Spacing = 8 };
        void Field(string label, Control control)
        {
            panel.Children.Add(new TextBlock { Text = label });
            panel.Children.Add(control);
        }
        Field("显示名称", name);
        Field("类型", provider);
        Field("模型名称 / API model", modelName);
        Field("OpenAI-compatible 地址", baseUrl);
        Field("GGUF 路径（在线模型可留空）", modelPath);
        Field("多模态 mmproj 路径（可留空）", mmproj);
        panel.Children.Add(vision);
        panel.Children.Add(new TextBlock
        {
            Text = "在线 API 密钥通过环境变量 PRY_API_KEY_<模型ID> 提供，避免把密钥明文写进角色与偏好文件。",
            TextWrapping = TextWrapping.Wrap, Foreground = Brush.Parse("#7F91A4")
        });
        panel.Children.Add(save);
        var editor = new Window
        {
            Title = source is null ? "添加自定义模型" : "编辑自定义模型",
            Width = 580, Height = 610, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = createSurface(new ScrollViewer { Content = panel })
        };
        ModelProfile? result = null;
        save.Click += (_, _) =>
        {
            var providerValue = provider.SelectedItem?.ToString() ?? "local-llama";
            if (!HasRequiredFields(name.Text, providerValue, modelName.Text, baseUrl.Text)) return;
            result = new ModelProfile
            {
                Id = source?.Id ?? $"custom-{Guid.NewGuid():N}",
                DisplayName = name.Text!.Trim(), Provider = providerValue, ModelName = modelName.Text!.Trim(),
                BaseUrl = baseUrl.Text?.Trim() ?? "",
                ModelPath = string.IsNullOrWhiteSpace(modelPath.Text) ? null : modelPath.Text.Trim(),
                MmprojPath = string.IsNullOrWhiteSpace(mmproj.Text) ? null : mmproj.Text.Trim(),
                Capabilities = new ModelCapabilities(Text: true, Vision: vision.IsChecked == true),
                ContextSize = source?.ContextSize ?? 4096, MaxOutputTokens = source?.MaxOutputTokens ?? 512,
                Temperature = source?.Temperature ?? .8, GpuLayers = source?.GpuLayers is > 0 ? source.GpuLayers : 999,
                ComputeDevice = source?.ComputeDevice ?? "auto-discrete", EnableThinking = source?.EnableThinking ?? true
            };
            editor.Close();
        };
        await editor.ShowDialog(owner);
        return result;
    }

    private sealed record ModelChoice(string Id, string Name)
    {
        public override string ToString() => Name;
    }
}

public sealed class CustomModelManagerState
{
    private readonly List<ModelProfile> _items;
    public CustomModelManagerState(IEnumerable<ModelProfile> source) => _items = source.ToList();
    public IReadOnlyList<ModelProfile> Items => _items;
    public ModelProfile? Find(string id) => _items.FirstOrDefault(item => item.Id == id);
    public void Add(ModelProfile value, string serverId) => _items.Add(value with { Id = serverId });
    public bool Update(string id, ModelProfile value)
    {
        var index = _items.FindIndex(item => item.Id == id);
        if (index < 0) return false;
        _items[index] = value;
        return true;
    }
    public bool Remove(string id) => _items.RemoveAll(item => item.Id == id) > 0;
}
