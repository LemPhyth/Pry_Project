using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Pry.App.Services;

public static class VisualCropEditor
{
    public static Border Create(Image image, NumericUpDown focusX, NumericUpDown focusY, NumericUpDown zoom, double width, double height, bool circular)
    {
        image.Width = width; image.Height = height; image.Stretch = Stretch.UniformToFill;
        var viewport = new Border
        {
            Width = width, Height = height, CornerRadius = circular ? new CornerRadius(width / 2) : new CornerRadius(12),
            ClipToBounds = true, BorderBrush = Brush.Parse("#6B7F92"), BorderThickness = new Thickness(1),
            Background = Brush.Parse("#121C26"), Focusable = true, Child = image
        };
        viewport.AttachedToVisualTree += (_, _) => viewport.Cursor = new Cursor(StandardCursorType.SizeAll);
        AutomationProperties.SetName(viewport, "图片取景");
        AutomationProperties.SetHelpText(viewport, "方向键移动取景，加减键缩放；也可拖动或使用滚轮。");
        var hint = new Border
        {
            Background = Brush.Parse("#AA0E1621"), CornerRadius = new CornerRadius(8), Padding = new Thickness(9, 5),
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(8),
            IsHitTestVisible = false,
            Child = new TextBlock { Text = "拖动取景 · 滚轮缩放", Foreground = Brushes.White, FontSize = 11 }
        };
        var grid = new Grid { Width = width, Height = height, Children = { viewport, hint } };
        var dragging = false; Point dragOrigin = default; decimal originX = .5m; decimal originY = .5m;
        viewport.PointerPressed += (_, args) =>
        {
            if (!args.GetCurrentPoint(viewport).Properties.IsLeftButtonPressed) return;
            viewport.Focus();
            dragging = true; dragOrigin = args.GetPosition(viewport); originX = focusX.Value ?? .5m; originY = focusY.Value ?? .5m;
            args.Pointer.Capture(viewport); args.Handled = true;
        };
        viewport.PointerMoved += (_, args) =>
        {
            if (!dragging) return;
            var position = args.GetPosition(viewport); var currentZoom = Math.Max(1, (double)(zoom.Value ?? 1));
            focusX.Value = (decimal)Math.Clamp((double)originX - (position.X - dragOrigin.X) / Math.Max(1, viewport.Bounds.Width) / currentZoom, 0, 1);
            focusY.Value = (decimal)Math.Clamp((double)originY - (position.Y - dragOrigin.Y) / Math.Max(1, viewport.Bounds.Height) / currentZoom, 0, 1);
            args.Handled = true;
        };
        viewport.PointerReleased += (_, args) =>
        {
            if (!dragging) return; dragging = false; args.Pointer.Capture(null); args.Handled = true;
        };
        viewport.PointerCaptureLost += (_, _) => dragging = false;
        viewport.DetachedFromVisualTree += (_, _) => dragging = false;
        viewport.KeyDown += (_, args) =>
        {
            switch (args.Key)
            {
                case Key.Left: focusX.Value = Math.Clamp((focusX.Value ?? .5m) - .05m, 0, 1); break;
                case Key.Right: focusX.Value = Math.Clamp((focusX.Value ?? .5m) + .05m, 0, 1); break;
                case Key.Up: focusY.Value = Math.Clamp((focusY.Value ?? .5m) - .05m, 0, 1); break;
                case Key.Down: focusY.Value = Math.Clamp((focusY.Value ?? .5m) + .05m, 0, 1); break;
                case Key.Add:
                case Key.OemPlus: zoom.Value = Math.Clamp((zoom.Value ?? 1) + .08m, 1, 3); break;
                case Key.Subtract:
                case Key.OemMinus: zoom.Value = Math.Clamp((zoom.Value ?? 1) - .08m, 1, 3); break;
                default: return;
            }
            args.Handled = true;
        };
        viewport.PointerWheelChanged += (_, args) =>
        {
            zoom.Value = (decimal)Math.Clamp((double)(zoom.Value ?? 1) + args.Delta.Y * .08, 1, 3); args.Handled = true;
        };
        return new Border { HorizontalAlignment = HorizontalAlignment.Left, Child = grid };
    }
}
