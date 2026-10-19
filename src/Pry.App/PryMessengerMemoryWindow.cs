using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Pry.Client;
using Pry.Contracts;
using Pry.Core.Models;
using Pry.App.Services;

namespace Pry.App;

internal sealed class PryMessengerMemoryWindow : UserControl
{
    // 用户看得见的分组标题。写死在这里，源码门靠它们判"孤儿记忆有没有被单独摆出来"。
    private const string InUseLabel = "她正在用的";
    private const string OrphanedLabel = "依据已被你删掉的";
    private const string ConfirmDeleteId = "confirm_delete";

    private readonly PryBackendClient _api;
    private readonly CharacterSummaryResponse _character;
    // 列表数据源换成作用域感知的 details 读口，孤立的行才可能在管理面被看见（legacy 列表 SQL 带 status='active'）。
    private readonly ListBox _inUse = new() { Background = Brushes.Transparent, BorderThickness = new Thickness(0) };
    private readonly ListBox _orphaned = new() { Background = Brushes.Transparent, BorderThickness = new Thickness(0) };
    private readonly TextBox _search = new() { Watermark = "搜索记忆" };
    private readonly TextBox _kind = new() { Watermark = "类型，例如 fact" };
    private readonly TextBox _summary = new() { Watermark = "记忆内容", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 110 };
    private readonly TextBox _tags = new() { Watermark = "标签，用逗号分隔" };
    private readonly Slider _importance = new() { Minimum = 0, Maximum = 1, TickFrequency = 0.1, Value = 0.5 };
    private readonly TextBlock _status = new() { Foreground = Pry.App.Services.PryVisualTheme.Get("Muted") };
    private readonly Button _keep;
    private readonly Button _save;
    private MemoryRecord? _selected;
    private MemoryDetailsResponse? _selectedOrphan;

    public PryMessengerMemoryWindow(PryBackendClient api, CharacterSummaryResponse character)
    {
        _api = api; _character = character; Background = Pry.App.Services.PryVisualTheme.Get("Canvas");
        var add = Button("新建", NewRecord); _save = Button("保存", SaveAsync, true); var remove = Button("删除", DeleteAsync);
        _keep = Button("确认保留", KeepAsync, true);
        _search.TextChanged += async (_, _) => await ReloadAsync(_search.Text);
        _inUse.SelectionChanged += (_, _) => SelectInUse(_inUse.SelectedItem as ListBoxItem);
        _orphaned.SelectionChanged += (_, _) => SelectOrphan(_orphaned.SelectedItem as ListBoxItem);
        var fields = new StackPanel { Spacing = 16, Margin = new Thickness(24), MaxWidth = 800, Children =
        {
            PryUi.Heading("记忆详情"), PryUi.Field("类型", _kind),
            PryUi.Field("内容", _summary), PryUi.Field("标签", _tags), PryUi.Field("重要程度", _importance)
        }};
        remove.Classes.Add("danger");
        _status.TextWrapping = TextWrapping.Wrap;
        var actions = PryUi.Actions(_keep, remove, _save); actions.HorizontalAlignment = HorizontalAlignment.Right;
        var footer = PryUi.Footer(new StackPanel { Spacing = 8, Children = { _status, actions } });
        var detail = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), Children = { new ScrollViewer { Content = fields }, footer } };
        Grid.SetRow(footer, 1);

        var column = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,2*,Auto,*"), Margin = new Thickness(24), RowSpacing = 8
        };
        var heading = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { PryUi.Heading("长期记忆"), add } };
        Grid.SetColumn(add, 1);
        column.Children.Add(heading); column.Children.Add(_search);
        column.Children.Add(SectionLabel(InUseLabel)); column.Children.Add(_inUse);
        column.Children.Add(SectionLabel(OrphanedLabel)); column.Children.Add(_orphaned);
        for (var i = 0; i < column.Children.Count; i++) Grid.SetRow(column.Children[i], i);

        var root = new Grid { ColumnDefinitions = new ColumnDefinitions("280,*"), Children =
        {
            column, new Border { BorderBrush = Pry.App.Services.PryVisualTheme.Get("Border"), BorderThickness = new Thickness(1,0,0,0), Child = detail }
        } };
        Grid.SetColumn(root.Children[1], 1); Content = root;
        SetEditable(false);
        AttachedToVisualTree += async (_, _) => await ReloadAsync();
    }

    private static Button Button(string text, Action action) { var b = new Button { Content = text }; b.Click += (_, _) => action(); return b; }
    private static Button Button(string text, Func<Task> action, bool primary = false) { var b = new Button { Content = text }; if (primary) b.Classes.Add("primary"); b.Click += async (_, _) => await action(); return b; }
    private static TextBlock SectionLabel(string text) =>
        new() { Text = text, FontWeight = FontWeight.SemiBold, FontSize = 13, Foreground = Pry.App.Services.PryVisualTheme.Get("Muted") };

    private static ListBoxItem Item(MemoryRecord record) => Row(record.Summary, $"{record.Kind} · 重要度 {record.Importance:0.0}", record);
    private static ListBoxItem OrphanItem(MemoryDetailsResponse detail) => Row(detail.Content, $"{detail.Kind} · {OrphanedLabel}", detail);
    private static ListBoxItem Row(string title, string subtitle, object tag) => new()
    {
        Tag = tag, Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 4),
        Content = new StackPanel { Spacing = 3, Children =
        {
            new TextBlock { Text = title, TextTrimming = TextTrimming.CharacterEllipsis },
            new TextBlock { Text = subtitle, FontSize = 13, Foreground = Pry.App.Services.PryVisualTheme.Get("Muted") }
        } }
    };

    private async Task ReloadAsync(string? query = null)
    {
        try
        {
            var records = await _api.GetMemoriesAsync(_character.Id, query);
            var details = await _api.GetMemoryDetailsAsync(_character.Id);
            var orphans = details.Where(x => x.Status == "orphaned")
                .Where(x => string.IsNullOrWhiteSpace(query) || x.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            _inUse.ItemsSource = records.Select(Item).ToArray();
            _orphaned.ItemsSource = orphans.Select(OrphanItem).ToArray();
            _status.Text = records.Count + orphans.Length == 0
                ? "没有符合条件的记忆"
                : $"{InUseLabel} {records.Count} 条 · {OrphanedLabel} {orphans.Length} 条";
        }
        catch (Exception ex) { _status.Text = $"读取失败：{ex.Message}"; }
    }

    private void NewRecord()
    {
        _selected = null; _selectedOrphan = null;
        _inUse.SelectedItem = null; _orphaned.SelectedItem = null;
        _kind.Text = "fact"; _summary.Text = ""; _tags.Text = ""; _importance.Value = 0.5;
        SetEditable(true); _keep.IsEnabled = false; _save.IsEnabled = true; _summary.Focus();
    }

    private void SelectInUse(ListBoxItem? item)
    {
        if (item?.Tag is not MemoryRecord record) return;
        _orphaned.SelectedItem = null;
        _selected = record; _selectedOrphan = null;
        _kind.Text = record.Kind; _summary.Text = record.Summary; _tags.Text = record.Tags; _importance.Value = record.Importance;
        SetEditable(true); _keep.IsEnabled = false; _save.IsEnabled = true;
    }

    private void SelectOrphan(ListBoxItem? item)
    {
        if (item?.Tag is not MemoryDetailsResponse detail) return;
        _inUse.SelectedItem = null;
        _selected = null; _selectedOrphan = detail;
        // 孤立的记忆没有可编辑的正文来源，只承认它或删掉它；把编辑框锁住，避免"改内容"被当成第二条恢复通道。
        _kind.Text = detail.Kind; _summary.Text = detail.Content; _tags.Text = ""; _importance.Value = detail.Importance;
        SetEditable(false); _keep.IsEnabled = true; _save.IsEnabled = false;
        _status.Text = "这条事实的依据已被删掉，确认保留会重新启用它，但不重建依据。";
    }

    private void SetEditable(bool value) { foreach (var c in new Control[] { _kind, _summary, _tags, _importance }) c.IsEnabled = value; }

    private async Task SaveAsync()
    {
        if (_selectedOrphan is not null) return; // 孤立的记忆走"确认保留"，不走编辑保存。
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

    // 确认保留＝一次状态迁移（orphaned→active），不是"反删除"；不重建证据，撞同指纹会被服务端拒。
    private async Task KeepAsync()
    {
        if (_selectedOrphan is not { } orphan) { _status.Text = "请先选择一条被删掉依据的记忆"; return; }
        try
        {
            await _api.ConfirmKeepMemoryAsync(orphan.Id, _character.Id);
            _selectedOrphan = null; await ReloadAsync(_search.Text); _status.Text = "已确认保留，她会重新把这件事当回事";
        }
        catch (Exception ex) { _status.Text = $"确认保留失败：{ex.Message}"; }
    }

    private async Task DeleteAsync()
    {
        var id = _selected?.Id ?? _selectedOrphan?.Id;
        if (id is not long target) { _status.Text = "请先选择一条记忆"; return; }
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is null) { _status.Text = "无法弹出确认，删除已取消"; return; }
        // 删除是永久动作（D1 写墓碑，此后自动学不回来），先问一句，文案说清"不会再重新学到"这一层。
        var actions = new[]
        {
            new MessengerDialogAction("cancel", "取消"),
            new MessengerDialogAction(ConfirmDeleteId, "永久删除", Destructive: true)
        };
        var result = await PryMessengerConfirmDialog.ShowAsync(owner, "删除长期记忆", "确定永久删除这条记忆吗？",
            "删掉之后她不会再重新学到这句话，除非你亲手再录入一次。此操作不可撤销。", actions, "#6C63FF");
        if (result != ConfirmDeleteId) return;
        try { await _api.DeleteMemoryAsync(target, _character.Id); NewRecord(); await ReloadAsync(_search.Text); _status.Text = "已删除"; }
        catch (Exception ex) { _status.Text = $"删除失败：{ex.Message}"; }
    }
}
