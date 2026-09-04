using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;

namespace Pry.App.Services;

public static class ThemeImageGallery
{
    public static void Refresh(WrapPanel gallery, IReadOnlyList<string> paths, string? selected,
        Action<string> select, bool square, IBrush accent, ThemeImageResources resources)
    {
        foreach (var button in gallery.Children.OfType<Button>())
            if (button.Content is Image image) resources.Clear(image);
        gallery.Children.Clear();
        foreach (var path in paths)
        {
            var preview = new Image
            {
                Width = square ? 68 : 112, Height = square ? 68 : 72, Stretch = Stretch.UniformToFill
            };
            if (!resources.Load(preview, path)) continue;
            var isSelected = string.Equals(path, selected, StringComparison.OrdinalIgnoreCase);
            var button = new Button
            {
                Width = square ? 82 : 126, Height = square ? 82 : 86,
                Margin = new Thickness(0, 0, 8, 8), Padding = new Thickness(5),
                BorderThickness = new Thickness(isSelected ? 3 : 1),
                BorderBrush = isSelected ? accent : Brush.Parse("#40505F"), Content = preview
            };
            ToolTip.SetTip(button, Path.GetFileName(path));
            AutomationProperties.SetName(button, $"{Path.GetFileName(path)}{(isSelected ? "，已选择" : "，选择图片")}");
            button.Click += (_, _) => select(path);
            gallery.Children.Add(button);
        }
        if (gallery.Children.Count == 0)
            gallery.Children.Add(new TextBlock
            {
                Text = paths.Count == 0 ? "尚未导入图片" : "图片无法加载，请重新导入。",
                Foreground = Brush.Parse("#7F91A4"), Margin = new Thickness(0, 8)
            });
    }
}
