using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Pry.Core.Models;

namespace Pry.App.Services;

public static class AttachmentPreviewFactory
{
    public static Control Create(ChatAttachment attachment, Action removeAttachment)
    {
        Control preview;
        if (attachment.IsImage)
        {
            try
            {
                preview = new Image
                {
                    Source = new Bitmap(attachment.Path), Width = 72, Height = 58,
                    Stretch = Stretch.Uniform
                };
            }
            catch
            {
                preview = new TextBlock { Text = "IMG", Width = 72, TextAlignment = TextAlignment.Center };
            }
        }
        else
        {
            preview = new Border
            {
                Width = 58, Height = 58, CornerRadius = new CornerRadius(8),
                Background = Brush.Parse("#263746"),
                Child = new TextBlock
                {
                    Text = Path.GetExtension(attachment.Name).TrimStart('.').ToUpperInvariant(),
                    FontSize = 10, FontWeight = FontWeight.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
        }

        var remove = new Button
        {
            Content = "×", Classes = { "composer-action" }, Width = 26, Height = 26,
            Padding = new Thickness(0), VerticalAlignment = VerticalAlignment.Top
        };
        AutomationProperties.SetName(remove, $"移除附件 {attachment.Name}");
        remove.Click += (_, _) => removeAttachment();
        var card = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto"),
            Margin = new Thickness(0, 0, 8, 0),
            Children = { preview, remove }
        };
        Grid.SetColumn(remove, 1);
        ToolTip.SetTip(card, attachment.Name);
        AutomationProperties.SetName(card, $"附件 {attachment.Name}");
        return card;
    }
}
