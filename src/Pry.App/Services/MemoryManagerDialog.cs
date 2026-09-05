using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Pry.Client;
using Pry.Contracts;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class MemoryManagerDialog(
    PryBackendClient api,
    Func<Control, Control> createSurface,
    Func<Control, Border> createCard,
    Func<Window, string, string, Task<bool>> confirm,
    Func<string, string, Task> showNotice)
{
    public async Task ShowAsync(Window owner, string characterId, string characterName)
    {
        var window = new Window
        {
            Title = "长期记忆管理", Width = 940, Height = 680, Background = Brushes.Transparent,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var search = new TextBox { Watermark = "搜索内容、标签或类型…" };
        var searchButton = new Button { Content = "搜索" };
        var list = new ListBox { Background = Brushes.Transparent };
        var kind = new ComboBox
        {
            ItemsSource = new[] { "user_fact", "preference", "promise", "relationship", "event", "other" },
            SelectedItem = "user_fact"
        };
        var summary = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 150 };
        var tags = new TextBox { Watermark = "用逗号分隔" };
        var importance = new NumericUpDown { Value = .65m, Minimum = 0, Maximum = 1, Increment = .05m };
        var metadata = new TextBlock { Foreground = Brush.Parse("#7F91A4"), TextWrapping = TextWrapping.Wrap };
        var create = new Button { Content = "＋ 新建记忆" };
        var save = new Button { Content = "保存", Classes = { "primary" } };
        var remove = new Button { Content = "删除" };
        long? editingId = null;

        async Task RefreshAsync(long? selectId = null)
        {
            var memories = await api.GetMemoriesAsync(characterId, search.Text);
            var items = memories.Select(memory => new MemoryListItem(memory)).ToArray();
            list.ItemsSource = items;
            list.SelectedItem = items.FirstOrDefault(item => item.Memory.Id == selectId);
        }
        void Load(MemoryRecord memory)
        {
            editingId = memory.Id;
            kind.SelectedItem = memory.Kind;
            summary.Text = memory.Summary;
            tags.Text = memory.Tags;
            importance.Value = (decimal)memory.Importance;
            metadata.Text = FormatMetadata(memory);
        }
        void NewMemory()
        {
            editingId = null;
            kind.SelectedItem = "user_fact";
            summary.Text = "";
            tags.Text = "";
            importance.Value = .65m;
            metadata.Text = "尚未保存的新记忆";
            list.SelectedItem = null;
        }

        list.SelectionChanged += (_, _) =>
        {
            if (list.SelectedItem is MemoryListItem item) Load(item.Memory);
        };
        searchButton.Click += async (_, _) => await RefreshAsync();
        create.Click += (_, _) => NewMemory();
        save.Click += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(summary.Text))
            {
                await showNotice("无法保存", "记忆内容不能为空。");
                return;
            }
            var selectedKind = kind.SelectedItem?.ToString() ?? "other";
            var normalizedSummary = summary.Text.Trim();
            var normalizedTags = tags.Text?.Trim() ?? "";
            var normalizedImportance = (double)(importance.Value ?? .65m);
            if (editingId is long id)
                await api.UpdateMemoryAsync(id, characterId,
                    new UpdateMemoryRequest(selectedKind, normalizedSummary, normalizedTags, normalizedImportance));
            else
                editingId = (await api.CreateMemoryAsync(new CreateMemoryRequest(characterId, selectedKind,
                    normalizedSummary, normalizedTags, normalizedImportance))).Id;
            await RefreshAsync(editingId);
        };
        remove.Click += async (_, _) =>
        {
            if (editingId is not long id ||
                !await confirm(window, "删除长期记忆", "确定永久删除这条记忆吗？")) return;
            await api.DeleteMemoryAsync(id, characterId);
            NewMemory();
            await RefreshAsync();
        };

        var leftHeader = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { search, searchButton }
        };
        Grid.SetColumn(searchButton, 1);
        var listCard = createCard(list);
        listCard.Padding = new Thickness(8);
        listCard.Margin = new Thickness(0, 10);
        var left = new Grid
        {
            Margin = new Thickness(18), RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            Children = { leftHeader, listCard, create }
        };
        Grid.SetRow(listCard, 1);
        Grid.SetRow(create, 2);

        var form = new StackPanel { Margin = new Thickness(10), Spacing = 9 };
        void Field(string label, Control control)
        {
            form.Children.Add(new TextBlock { Text = label, Foreground = Brush.Parse("#B8CCE0") });
            form.Children.Add(control);
        }
        form.Children.Add(new TextBlock
        {
            Text = $"{characterName}的长期记忆", FontSize = 22,
            FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 0, 0, 8)
        });
        Field("类型", kind);
        Field("内容", summary);
        Field("标签", tags);
        Field("重要度", importance);
        form.Children.Add(metadata);
        form.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right, Children = { remove, save }
        });
        var divider = new Border
        {
            BorderBrush = Brush.Parse("#263746"), BorderThickness = new Thickness(0, 0, 1, 0), Child = left
        };
        var formCard = createCard(new ScrollViewer { Content = form });
        formCard.Margin = new Thickness(12, 18, 18, 18);
        formCard.Padding = new Thickness(12);
        var layout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("350,*"), Children = { divider, formCard }
        };
        Grid.SetColumn(formCard, 1);
        window.Content = createSurface(layout);
        NewMemory();
        await RefreshAsync();
        await window.ShowDialog(owner);
    }

    public static string FormatMetadata(MemoryRecord memory) =>
        $"创建：{memory.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm}　更新：{memory.UpdatedAt?.ToLocalTime():yyyy-MM-dd HH:mm}　最近调用：{memory.LastUsedAt?.ToLocalTime():yyyy-MM-dd HH:mm}";

    public static string KindLabel(string kind) => kind switch
    {
        "user_fact" => "事实", "preference" => "偏好", "promise" => "约定",
        "relationship" => "关系", "event" => "事件", _ => "其他"
    };

    private sealed record MemoryListItem(MemoryRecord Memory)
    {
        public override string ToString() => $"{KindLabel(Memory.Kind)} · {Memory.Summary}";
    }
}
