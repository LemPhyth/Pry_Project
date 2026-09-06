using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Pry.Client;
using Pry.Contracts;

namespace Pry.App;

internal sealed class PryMessengerStickerWindow : Window
{
    private readonly PryBackendClient _api;
    private readonly ListBox _list = new();
    private readonly TextBox _name = new() { Watermark = "表情名称", MaxLength = 80 };
    private readonly TextBox _emotions = new() { Watermark = "情绪标签，用逗号分隔" };
    private readonly ComboBox _role = new() { ItemsSource = new[] { "reaction", "message", "backchannel" } };
    private readonly CheckBox _backchannel = new() { Content = "允许作为倾听反馈" };
    private readonly TextBlock _status = new() { Foreground = Brush.Parse("#8492A8"), TextWrapping = TextWrapping.Wrap };
    private StickerResponse? _selected;

    public PryMessengerStickerWindow(PryBackendClient api)
    {
        _api = api; Title = "表情管理"; Width = 760; Height = 540; MinWidth = 650; MinHeight = 460;
        Background = Brush.Parse("#101827"); WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var import = MakeButton("导入图片", ImportAsync, true); var save = MakeButton("保存", SaveAsync, true); var remove = MakeButton("删除", DeleteAsync); var close = MakeButton("完成", () => Close());
        _list.SelectionChanged += (_, _) => Select((_list.SelectedItem as ListBoxItem)?.Tag as StickerResponse);
        var left = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), RowSpacing = 10, Margin = new Thickness(18), Children = { import, _list } }; Grid.SetRow(_list, 1);
        var form = new StackPanel { Margin = new Thickness(20), Spacing = 11, Children =
        {
            new TextBlock { Text = "表情详情", FontSize = 21, FontWeight = FontWeight.SemiBold }, _name, _emotions, _role, _backchannel, _status,
            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Children = { remove, save, close } }
        }};
        var root = new Grid { ColumnDefinitions = new ColumnDefinitions("300,*"), Children = { left, new Border { BorderBrush = Brush.Parse("#263249"), BorderThickness = new Thickness(1,0,0,0), Child = form } } }; Grid.SetColumn(root.Children[1], 1); Content = root;
        Opened += async (_, _) => await ReloadAsync();
    }

    public bool Changed { get; private set; }
    private static Button MakeButton(string text, Action action) { var button = new Button { Content = text }; button.Click += (_, _) => action(); return button; }
    private static Button MakeButton(string text, Func<Task> action, bool primary = false) { var button = new Button { Content = text, Background = primary ? Brush.Parse("#6C63FF") : null }; button.Click += async (_, _) => await action(); return button; }
    private async Task ReloadAsync()
    {
        try
        {
            var items = await _api.GetStickersAsync();
            _list.ItemsSource = items.Select(item => new ListBoxItem { Tag = item, Padding = new Thickness(10), Content = $"{item.Name}\n{string.Join(" · ", item.Emotions)}" }).ToArray();
            _status.Text = items.Count == 0 ? "还没有用户表情" : $"共 {items.Count} 个表情";
        }
        catch (Exception ex) { _status.Text = $"读取失败：{ex.Message}"; }
    }
    private void Select(StickerResponse? item) { if (item is null) return; _selected = item; _name.Text = item.Name; _emotions.Text = string.Join(", ", item.Emotions); _role.SelectedItem = item.InteractionRole; _backchannel.IsChecked = item.LikelyBackchannel; }
    private async Task ImportAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "导入表情图片", AllowMultiple = false, FileTypeFilter = new[] { FilePickerFileTypes.ImageAll } });
        var file = files.FirstOrDefault(); var path = file?.TryGetLocalPath(); if (file is null || path is null) return;
        try
        {
            await using var stream = File.OpenRead(path); var media = await _api.UploadAsync(stream, file.Name, Mime(path));
            await _api.ImportStickerAsync(media.Id, Path.GetFileNameWithoutExtension(file.Name), []); Changed = true; await ReloadAsync(); _status.Text = "表情已导入";
        }
        catch (Exception ex) { _status.Text = $"导入失败：{ex.Message}"; }
    }
    private async Task SaveAsync()
    {
        if (_selected is null || string.IsNullOrWhiteSpace(_name.Text)) { _status.Text = "请选择表情并填写名称"; return; }
        try { await _api.UpdateStickerAsync(_selected.Id, new UpdateStickerRequest(_name.Text.Trim(), Tags(_emotions.Text), _role.SelectedItem?.ToString() ?? "reaction", _backchannel.IsChecked == true)); Changed = true; await ReloadAsync(); _status.Text = "表情已保存"; }
        catch (Exception ex) { _status.Text = $"保存失败：{ex.Message}"; }
    }
    private async Task DeleteAsync()
    {
        if (_selected is null) return;
        try { await _api.DeleteStickerAsync(_selected.Id); _selected = null; Changed = true; await ReloadAsync(); _status.Text = "表情已删除"; }
        catch (Exception ex) { _status.Text = $"无法删除：{ex.Message}"; }
    }
    private static IReadOnlyList<string> Tags(string? value) => (value ?? "").Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    private static string Mime(string path) => Path.GetExtension(path).ToLowerInvariant() switch { ".png" => "image/png", ".webp" => "image/webp", ".gif" => "image/gif", _ => "image/jpeg" };
}
