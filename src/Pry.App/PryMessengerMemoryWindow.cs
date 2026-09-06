using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Pry.Client;
using Pry.Contracts;
using Pry.Core.Models;

namespace Pry.App;

internal sealed class PryMessengerMemoryWindow : Window
{
    private readonly PryBackendClient _api;
    private readonly CharacterSummaryResponse _character;
    private readonly ListBox _list = new();
    private readonly TextBox _search = new() { Watermark = "搜索记忆" };
    private readonly TextBox _kind = new() { Watermark = "类型，例如 fact" };
    private readonly TextBox _summary = new() { Watermark = "记忆内容", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 110 };
    private readonly TextBox _tags = new() { Watermark = "标签，用逗号分隔" };
    private readonly Slider _importance = new() { Minimum = 0, Maximum = 1, TickFrequency = 0.1, Value = 0.5 };
    private readonly TextBlock _status = new() { Foreground = Brush.Parse("#8492A8") };
    private MemoryRecord? _selected;

    public PryMessengerMemoryWindow(PryBackendClient api, CharacterSummaryResponse character)
    {
        _api = api; _character = character;
        Title = $"{character.Name} 的长期记忆"; Width = 820; Height = 600; MinWidth = 700; MinHeight = 500;
        WindowStartupLocation = WindowStartupLocation.CenterOwner; Background = Brush.Parse("#101827");
        var add = Button("新建", NewRecord); var save = Button("保存", SaveAsync, true); var remove = Button("删除", DeleteAsync);
        _search.TextChanged += async (_, _) => await ReloadAsync(_search.Text);
        _list.SelectionChanged += (_, _) => SelectRecord((_list.SelectedItem as ListBoxItem)?.Tag as MemoryRecord);
        var fields = new StackPanel { Spacing = 9, Margin = new Thickness(18), Children =
        {
            new TextBlock { Text = "记忆详情", FontSize = 20, FontWeight = FontWeight.SemiBold }, Label("类型"), _kind,
            Label("内容"), _summary, Label("标签"), _tags, Label("重要程度"), _importance, _status,
            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Children = { remove, save } }
        }};
        var left = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*"), Margin = new Thickness(18), RowSpacing = 10 };
        var heading = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { new TextBlock { Text = "长期记忆", FontSize = 20, FontWeight = FontWeight.SemiBold }, add } };
        Grid.SetColumn(add, 1); Grid.SetRow(_search, 1); Grid.SetRow(_list, 2); left.Children.Add(heading); left.Children.Add(_search); left.Children.Add(_list);
        var root = new Grid { ColumnDefinitions = new ColumnDefinitions("320,*"), Children = { left, new Border { BorderBrush = Brush.Parse("#263249"), BorderThickness = new Thickness(1,0,0,0), Child = fields } } };
        Grid.SetColumn(root.Children[1], 1); Content = root;
        Opened += async (_, _) => await ReloadAsync();
    }

    private static TextBlock Label(string text) => new() { Text = text, Foreground = Brush.Parse("#8D9AAF"), FontSize = 11 };
    private static Button Button(string text, Action action) { var b = new Button { Content = text }; b.Click += (_, _) => action(); return b; }
    private static Button Button(string text, Func<Task> action, bool primary = false) { var b = new Button { Content = text, Background = primary ? Brush.Parse("#6C63FF") : null }; b.Click += async (_, _) => await action(); return b; }

    private async Task ReloadAsync(string? query = null)
    {
        try
        {
            var records = await _api.GetMemoriesAsync(_character.Id, query);
            _list.ItemsSource = records.Select(item => new ListBoxItem { Tag = item, Padding = new Thickness(10), Content = new StackPanel { Spacing = 3, Children = { new TextBlock { Text = item.Summary, TextTrimming = TextTrimming.CharacterEllipsis }, new TextBlock { Text = $"{item.Kind} · 重要度 {item.Importance:0.0}", FontSize = 10, Foreground = Brush.Parse("#718198") } } } }).ToArray();
            _status.Text = records.Count == 0 ? "没有符合条件的记忆" : $"共 {records.Count} 条";
        }
        catch (Exception ex) { _status.Text = $"读取失败：{ex.Message}"; }
    }

    private void NewRecord() { _selected = null; _list.SelectedItem = null; _kind.Text = "fact"; _summary.Text = ""; _tags.Text = ""; _importance.Value = 0.5; _summary.Focus(); }
    private void SelectRecord(MemoryRecord? value) { if (value is null) return; _selected = value; _kind.Text = value.Kind; _summary.Text = value.Summary; _tags.Text = value.Tags; _importance.Value = value.Importance; }
    private async Task SaveAsync()
    {
        var kind = _kind.Text?.Trim(); var summary = _summary.Text?.Trim();
        if (string.IsNullOrWhiteSpace(kind) || string.IsNullOrWhiteSpace(summary)) { _status.Text = "类型和内容不能为空"; return; }
        try
        {
            if (_selected is null) await _api.CreateMemoryAsync(new CreateMemoryRequest(_character.Id, kind, summary, _tags.Text?.Trim() ?? "", _importance.Value));
            else await _api.UpdateMemoryAsync(_selected.Id, _character.Id, new UpdateMemoryRequest(kind, summary, _tags.Text?.Trim() ?? "", _importance.Value));
            NewRecord(); await ReloadAsync(_search.Text); _status.Text = "已保存";
        }
        catch (Exception ex) { _status.Text = $"保存失败：{ex.Message}"; }
    }

    private async Task DeleteAsync()
    {
        if (_selected is null) { _status.Text = "请先选择一条记忆"; return; }
        try { await _api.DeleteMemoryAsync(_selected.Id, _character.Id); NewRecord(); await ReloadAsync(_search.Text); _status.Text = "已删除"; }
        catch (Exception ex) { _status.Text = $"删除失败：{ex.Message}"; }
    }
}
