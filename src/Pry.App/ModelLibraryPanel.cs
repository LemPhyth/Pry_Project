using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Pry.Client;
using Pry.Contracts;
using Pry.Core.Models;
using Pry.App.Services;

namespace Pry.App;

internal sealed class ModelLibraryPanel : UserControl
{
    private readonly PryBackendClient _api;
    private IReadOnlyList<ModelProfileResponse> _models;
    private readonly StackPanel _rows = new() { Spacing = 12 }, _editor = new() { Spacing = 8, IsVisible = false };
    private readonly TextBox _search = new() { Watermark = "搜索模型名称、标识或标签" };
    private readonly ComboBox _source = new() { ItemsSource = new[] { "全部来源", "内置", "本地", "网络" }, SelectedIndex = 0 };
    private readonly ComboBox _purpose = new() { ItemsSource = new[] { "全部用途", "聊天", "视觉", "记忆检索" }, SelectedIndex = 0 };
    private readonly ComboBox _embedding = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly CheckBox _enabled = new() { Content = "启用语义记忆检索" };
    private readonly TextBlock _embeddingStatus = Text("正在读取记忆索引状态…"), _status = Text("");
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(5) };
    private bool _polling, _embeddingLoaded;
    public event Action<IReadOnlyList<ModelProfileResponse>>? ModelsChanged;

    public ModelLibraryPanel(PryBackendClient api, IReadOnlyList<ModelProfileResponse> models)
    {
        _api = api; _models = models;
        var memory = new StackPanel { Spacing = 16 };
        memory.Children.Add(PryUi.Heading("记忆检索"));
        memory.Children.Add(PryUi.Hint("用 Embedding 帮助角色找到相关记忆。它不负责生成聊天回复。"));
        memory.Children.Add(_enabled); memory.Children.Add(PryUi.Field("记忆检索模型", _embedding)); memory.Children.Add(_embeddingStatus);
        var apply = ActionButton("保存记忆检索设置", async () =>
        {
            var id = (_embedding.SelectedItem as ComboBoxItem)?.Tag as string;
            var state = await _api.UpdateEmbeddingSettingsAsync(new UpdateEmbeddingSettingsRequest(_enabled.IsChecked == true, _enabled.IsChecked == true ? id : null));
            ShowEmbeddingStatus(state); _status.Text = "记忆检索设置已独立保存";
        });
        var reindex = ActionButton("补建索引", async () => ShowEmbeddingStatus(await _api.ReindexMemoriesAsync()));
        memory.Children.Add(PryUi.Actions(apply, reindex));
        memory.Children.Add(PryUi.Hint("此处单独保存，立即生效。"));
        var form = new StackPanel { Spacing = 16 };
        form.Children.Add(PryUi.Heading("模型库"));
        form.Children.Add(PryUi.Hint("管理可用模型。网络配置与标签单独保存，不改变当前聊天选择。"));
        form.Children.Add(_search);
        var scan = ActionButton("刷新本地模型", async () =>
        {
            var result = await _api.ScanModelsAsync(); UpdateModels(result.Models);
            _status.Text = $"已扫描，新增 {result.AddedCount} 个模型";
        });
        var add = new Button { Content = "添加自定义模型" }; add.Click += (_, _) => EditNetwork(null);
        _source.MinWidth = 120; _purpose.MinWidth = 120;
        form.Children.Add(PryUi.Actions(_source, _purpose, scan, add));
        form.Children.Add(_status); form.Children.Add(_editor); form.Children.Add(_rows);
        Content = new StackPanel { Spacing = 24, Children = { PryUi.Section(memory), PryUi.Section(form) } };
        _search.TextChanged += (_, _) => Render(); _source.SelectionChanged += (_, _) => Render(); _purpose.SelectionChanged += (_, _) => Render();
        Render(); PopulateEmbedding();
        AttachedToVisualTree += async (_, _) => { await PollEmbeddingAsync(); _timer.Start(); };
        DetachedFromVisualTree += (_, _) => _timer.Stop();
        _timer.Tick += async (_, _) => await PollEmbeddingAsync();
    }
    private static TextBlock Text(string value, double size = 13) => new() { Text = value, FontSize = size,
        FontWeight = size >= 16 ? FontWeight.SemiBold : FontWeight.Normal,
        Foreground = PryVisualTheme.Get(size >= 16 ? "Text" : "Muted"), TextWrapping = TextWrapping.Wrap, LineHeight = size >= 16 ? 24 : 20 };
    private Button ActionButton(string label, Func<Task> action)
    {
        var button = new Button { Content = label, Margin = new Thickness(0, 0, 8, 4) };
        button.Click += async (_, _) =>
        {
            if (!IsEnabled) return;
            IsEnabled = false;
            try { await action(); }
            catch (Exception ex) { _status.Text = $"操作失败：{ex.Message}"; }
            finally { IsEnabled = true; }
        };
        return button;
    }
    private void UpdateModels(IReadOnlyList<ModelProfileResponse> models)
    { _models = models; Render(); PopulateEmbedding(); ModelsChanged?.Invoke(models); }
    private void PopulateEmbedding()
    {
        var selected = (_embedding.SelectedItem as ComboBoxItem)?.Tag as string;
        _embedding.ItemsSource = _models.Where(x => x.Capabilities.Embeddings).Select(x => new ComboBoxItem
        { Tag = x.Id, Content = $"{x.DisplayName} · {Source(x.Source)}{(x.Available ? "" : " · 文件不可用")}", IsEnabled = x.Available }).ToArray();
        _embedding.SelectedItem = _embedding.Items.Cast<ComboBoxItem>().FirstOrDefault(x => (string?)x.Tag == selected);
        _embedding.PlaceholderText = _embedding.ItemCount == 0 ? "尚未发现 Embedding 模型，请刷新模型库" : "请选择记忆检索模型";
    }
    private async Task PollEmbeddingAsync()
    {
        if (_polling) return; _polling = true;
        try
        {
            var state = await _api.GetEmbeddingStatusAsync(); ShowEmbeddingStatus(state);
            if (!_embeddingLoaded)
            {
                _enabled.IsChecked = state.Enabled;
                _embedding.SelectedItem = _embedding.Items.Cast<ComboBoxItem>().FirstOrDefault(x => (string?)x.Tag == state.ModelId);
                _embeddingLoaded = true;
            }
        }
        catch (Exception ex) { _embeddingStatus.Text = $"读取失败，将自动重试：{ex.Message}"; }
        finally { _polling = false; }
    }
    private void ShowEmbeddingStatus(EmbeddingStatusResponse state) => _embeddingStatus.Text =
        (state.State switch { "disabled" => "已停用", "idle" => "已选择，等待按需启动", "indexing" => "正在建立索引", "ready" => "语义记忆就绪",
            "degraded" => "语义检索暂不可用，仍可使用关键词记忆与普通聊天", _ => "等待状态更新" }) +
        $" · 已索引 {state.IndexedCount}，待处理 {state.PendingCount}";
    private static string Source(string source) => source switch { "built-in" => "内置", "network" => "网络", _ => "本地" };
    private void Render()
    {
        _rows.Children.Clear();
        foreach (var model in _models.Where(x =>
                     (_source.SelectedIndex == 0 || Source(x.Source) == _source.SelectedItem?.ToString()) &&
                     (_purpose.SelectedIndex switch { 1 => x.Capabilities.Text, 2 => x.Capabilities.Vision, 3 => x.Capabilities.Embeddings, _ => true }) &&
                     $"{x.DisplayName} {x.ModelName} {string.Join(' ', x.Tags)}".Contains(_search.Text ?? "", StringComparison.OrdinalIgnoreCase)))
        {
            var row = new StackPanel { Spacing = 8 };
            row.Children.Add(Text(model.DisplayName, 16)); row.Children.Add(Text($"{Source(model.Source)} · {model.ModelName} · {(model.Available ? "配置可用" : "文件不可用，请恢复后刷新")}"));
            row.Children.Add(Text(string.Join(" / ", new[] { model.Capabilities.Text ? "聊天" : null, model.Capabilities.Vision ? "视觉" : null, model.Capabilities.Embeddings ? "记忆检索" : null }.Where(x => x is not null))));
            var tags = new TextBox { Text = string.Join(", ", model.Tags), Watermark = "标签，用逗号分隔" };
            var tagSave = ActionButton("保存标签", async () =>
            {
                var updated = await _api.UpdateModelTagsAsync(model.Id, new UpdateModelTagsRequest(Tags(tags.Text)));
                UpdateModels(_models.Select(x => x.Id == updated.Id ? updated : x).ToArray()); _status.Text = "标签已保存";
            });
            row.Children.Add(new Expander { Header = model.Tags.Count == 0 ? "编辑标签" : string.Join(" · ", model.Tags.Take(3)),
                Content = new StackPanel { Spacing = 8, Children = { tags, tagSave } } });
            if (model.Source == "network")
            {
                var edit = new Button { Content = "编辑网络配置" }; edit.Click += (_, _) => EditNetwork(model);
                var remove = ActionButton("确认删除配置", async () => { await _api.DeleteNetworkModelAsync(model.Id); UpdateModels(_models.Where(x => x.Id != model.Id).ToArray()); });
                remove.Classes.Add("danger");
                row.Children.Add(PryUi.Actions(edit, new Expander { Header = "删除配置…", Content = remove }));
            }
            _rows.Children.Add(new Border { Padding = new Thickness(0, 16), BorderBrush = PryVisualTheme.Get("Border"), BorderThickness = new Thickness(0, 1, 0, 0), Child = row });
        }
        if (_rows.Children.Count == 0) _rows.Children.Add(Text("没有符合条件的模型。可调整筛选，刷新本地模型或添加网络模型。"));
    }
    private static string[] Tags(string? value) => (value ?? "").Split(new[] { ',', '，' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    private void EditNetwork(ModelProfileResponse? model)
    {
        _editor.Children.Clear(); _editor.IsVisible = true;
        _editor.Children.Add(Text(model is null ? "添加 OpenAI-compatible 网络模型" : "编辑网络模型", 18));
        _editor.Children.Add(Text("认证使用运行环境配置。本地 GGUF 请使用刷新本地模型。"));
        TextBox Field(string label, string? value)
        { var box = new TextBox { Text = value }; _editor.Children.Add(PryUi.Field(label, box)); return box; }
        var name = Field("显示名称", model?.DisplayName); var identifier = Field("模型标识", model?.ModelName);
        var url = Field("服务地址（远程 HTTPS；回环可 HTTP）", null);
        if (model is not null) _editor.Children.Add(Text("服务地址留空则保留原地址；输入新地址后才修改。"));
        var chat = new CheckBox { Content = "聊天", IsChecked = model?.Capabilities.Text ?? true };
        var vision = new CheckBox { Content = "视觉", IsChecked = model?.Capabilities.Vision ?? false };
        var embedding = new CheckBox { Content = "Embedding", IsChecked = model?.Capabilities.Embeddings ?? false };
        _editor.Children.Add(PryUi.Actions(chat, vision, embedding));
        var context = Field("上下文长度", model?.ContextSize.ToString()); var output = Field("最大输出 Token", model?.MaxOutputTokens.ToString());
        var temperature = Field("温度", model?.Temperature.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var thinking = new CheckBox { Content = "启用思考模式", IsChecked = model?.EnableThinking ?? false }; _editor.Children.Add(thinking);
        var tags = Field("标签", model is null ? "" : string.Join(", ", model.Tags));
        var save = ActionButton("保存网络配置", async () =>
        {
            if (string.IsNullOrWhiteSpace(name.Text)) { name.Focus(); throw new InvalidOperationException("请填写显示名称"); }
            if (string.IsNullOrWhiteSpace(identifier.Text)) { identifier.Focus(); throw new InvalidOperationException("请填写模型标识"); }
            if (!(model is not null && string.IsNullOrWhiteSpace(url.Text)) && !Uri.TryCreate(url.Text, UriKind.Absolute, out _)) { url.Focus(); throw new InvalidOperationException("请填写有效服务地址"); }
            if (!int.TryParse(context.Text, out var c) || !int.TryParse(output.Text, out var o) || !double.TryParse(temperature.Text, System.Globalization.CultureInfo.InvariantCulture, out var t))
                throw new InvalidOperationException("请填写上下文长度、输出上限和温度");
            var request = new SaveNetworkModelRequest(name.Text, identifier.Text, url.Text ?? "", new ModelCapabilities { Text = chat.IsChecked == true, Vision = vision.IsChecked == true, Embeddings = embedding.IsChecked == true }, c, o, t, 0, "remote", thinking.IsChecked == true, Tags(tags.Text));
            var saved = model is null ? await _api.CreateNetworkModelAsync(request) : await _api.UpdateNetworkModelAsync(model.Id, request);
            UpdateModels(_models.Where(x => x.Id != saved.Id).Append(saved).ToArray()); _editor.IsVisible = false; _status.Text = "网络配置已保存，当前聊天模型未改变";
        });
        var cancel = new Button { Content = "取消编辑" }; cancel.Click += (_, _) => _editor.IsVisible = false;
        _editor.Spacing = 16;
        _editor.Children.Add(PryUi.Actions(save, cancel));
    }
}
