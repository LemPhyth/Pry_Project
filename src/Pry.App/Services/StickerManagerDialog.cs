using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Pry.Client;
using Pry.Contracts;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class StickerManagerDialog(
    PryBackendClient api,
    Func<IReadOnlyList<StickerDefinition>> getStickers,
    Func<Task> reloadStickers,
    Func<Control, Control> createSurface,
    Func<Control, Border> createCard,
    Func<Control, Control, Grid> createLayout,
    Func<Window, string, string, Task<bool>> confirm,
    Func<IBrush> accentBrush,
    Func<string, string> imageContentType)
{
    public async Task ShowAsync(Window owner)
    {
        var window = new Window
        {
            Title = "管理表情包", Width = 760, Height = 570, Background = Brushes.Transparent,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var grid = new WrapPanel
        {
            Orientation = Orientation.Horizontal, ItemWidth = 112, ItemHeight = 132
        };
        StickerDefinition? selected = null;
        var import = new Button { Content = "导入" };
        var edit = new Button { Content = "编辑" };
        var remove = new Button { Content = "删除" };
        var close = new Button { Content = "完成" };

        void Refresh()
        {
            grid.Children.Clear();
            foreach (var sticker in getStickers())
            {
                Control preview;
                try
                {
                    preview = new Image
                    {
                        Source = new Bitmap(sticker.FilePath), Width = 88, Height = 88,
                        Stretch = Stretch.Uniform
                    };
                }
                catch
                {
                    preview = new TextBlock { Text = "无法预览", Width = 88, Height = 88 };
                }
                var button = new Button
                {
                    Width = 104, Height = 124, Padding = new Thickness(5),
                    BorderBrush = sticker.Id == selected?.Id ? accentBrush() : Brush.Parse("#344452"),
                    BorderThickness = new Thickness(sticker.Id == selected?.Id ? 2 : 1),
                    Content = new StackPanel
                    {
                        Spacing = 4,
                        Children =
                        {
                            preview,
                            new TextBlock
                            {
                                Text = sticker.Name, MaxWidth = 92,
                                TextTrimming = TextTrimming.CharacterEllipsis,
                                HorizontalAlignment = HorizontalAlignment.Center
                            },
                            new TextBlock
                            {
                                Text = sticker.Source == StickerSource.BuiltIn ? "内置" : sticker.InteractionRole,
                                FontSize = 10, Foreground = Brush.Parse("#7F91A4"),
                                HorizontalAlignment = HorizontalAlignment.Center
                            }
                        }
                    }
                };
                button.Click += (_, _) => { selected = sticker; Refresh(); };
                grid.Children.Add(button);
            }
        }

        Refresh();
        import.Click += async (_, _) =>
        {
            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "导入表情包", AllowMultiple = true,
                FileTypeFilter = [new FilePickerFileType("表情图片") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.gif"] }]
            });
            foreach (var file in files)
            {
                var path = file.TryGetLocalPath();
                if (path is null) continue;
                await using var content = File.OpenRead(path);
                var media = await api.UploadAsync(content, Path.GetFileName(path), imageContentType(path));
                await api.ImportStickerAsync(media.Id, Path.GetFileNameWithoutExtension(path), []);
            }
            await reloadStickers();
            Refresh();
        };
        remove.Click += async (_, _) =>
        {
            if (!CanModify(selected) ||
                !await confirm(window, "删除表情", $"确定删除“{selected!.Name}”吗？这会同时删除应用数据目录里的副本。")) return;
            await api.DeleteStickerAsync(selected.Id);
            await reloadStickers();
            selected = null;
            Refresh();
        };
        edit.Click += async (_, _) =>
        {
            if (!CanModify(selected)) return;
            var id = selected!.Id;
            await EditAsync(window, selected);
            selected = getStickers().FirstOrDefault(sticker => sticker.Id == id);
            Refresh();
        };
        close.Click += (_, _) => window.Close();

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 9,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { import, edit, remove, close }
        };
        var stickerCard = createCard(new ScrollViewer { Content = grid, Background = Brushes.Transparent });
        stickerCard.Padding = new Thickness(12);
        var root = new Grid
        {
            Margin = new Thickness(20), RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            Background = Brushes.Transparent, RowSpacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = "点击缩略图选中表情。你和角色都可以使用启用的表情，互动作用会参与打断判断。",
                    TextWrapping = TextWrapping.Wrap
                },
                stickerCard, buttons
            }
        };
        Grid.SetRow(stickerCard, 1);
        Grid.SetRow(buttons, 2);
        window.Content = createSurface(root);
        await window.ShowDialog(owner);
    }

    public static bool CanModify(StickerDefinition? sticker) => sticker?.Source == StickerSource.User;

    public static string[] ParseTags(string? value) => (value ?? "")
        .Split(['，', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private async Task EditAsync(Window owner, StickerDefinition sticker)
    {
        var name = new TextBox { Text = sticker.Name };
        var tags = new TextBox { Text = string.Join("，", sticker.Emotions) };
        var role = new ComboBox
        {
            ItemsSource = new[] { "reaction", "backchannel", "topic" },
            SelectedItem = sticker.InteractionRole
        };
        var backchannel = new CheckBox
        {
            Content = "这个表情通常表示“我在听”", IsChecked = sticker.LikelyBackchannel
        };
        var save = new Button { Content = "保存", HorizontalAlignment = HorizontalAlignment.Right };
        var editor = new Window
        {
            Title = "编辑表情", Width = 480, Height = 430,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        save.Click += async (_, _) =>
        {
            await api.UpdateStickerAsync(sticker.Id, new UpdateStickerRequest(
                name.Text ?? sticker.Name, ParseTags(tags.Text),
                role.SelectedItem?.ToString() ?? "reaction", backchannel.IsChecked == true));
            await reloadStickers();
            editor.Close();
        };
        var form = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = "名称" }, name,
                new TextBlock { Text = "情绪标签" }, tags,
                new TextBlock { Text = "互动作用" }, role, backchannel
            }
        };
        editor.Content = createSurface(createLayout(createCard(form), save));
        await editor.ShowDialog(owner);
    }
}
