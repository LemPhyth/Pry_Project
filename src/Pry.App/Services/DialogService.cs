using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Pry.App.Services;

public sealed class DialogService(Func<Control, Control> createSurface, Func<Control, Border> createTextCard)
{
    public async Task<string?> PromptTextAsync(Window owner, string title, string label, string initialValue)
    {
        var input = new TextBox { Text = initialValue };
        var save = new Button { Content = "确定", Classes = { "primary" } };
        var cancel = new Button { Content = "取消" };
        var dialog = new Window
        {
            Title = title, Width = 460, Height = 250,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        string? result = null;
        save.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(input.Text)) return;
            result = input.Text.Trim();
            dialog.Close();
        };
        cancel.Click += (_, _) => dialog.Close();
        dialog.KeyDown += (_, args) =>
        {
            if (args.Key == Key.Escape) dialog.Close();
            else if (args.Key == Key.Enter && !string.IsNullOrWhiteSpace(input.Text))
            {
                result = input.Text.Trim();
                dialog.Close();
            }
        };
        dialog.Opened += (_, _) => input.Focus();
        var form = new StackPanel
        {
            Spacing = 8,
            Children = { new TextBlock { Text = label }, input }
        };
        dialog.Content = createSurface(CreateLayout(createTextCard(form), CreateActions(cancel, save)));
        await dialog.ShowDialog(owner);
        return result;
    }

    public async Task ShowNoticeAsync(Window owner, string title, string message)
    {
        var dialog = new Window
        {
            Title = title, Width = 440, Height = 240,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var close = new Button { Content = "知道了" };
        close.Click += (_, _) => dialog.Close();
        dialog.KeyDown += (_, args) =>
        {
            if (args.Key is Key.Escape or Key.Enter)
            {
                args.Handled = true;
                dialog.Close();
            }
        };
        dialog.Opened += (_, _) => close.Focus();
        var messageCard = createTextCard(new TextBlock
        {
            Text = message, TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        });
        dialog.Content = createSurface(CreateLayout(messageCard, CreateActions(close)));
        await dialog.ShowDialog(owner);
    }

    public async Task<bool> ConfirmAsync(Window owner, string title, string message)
    {
        var dialog = new Window
        {
            Title = title, Width = 460, Height = 250,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var result = false;
        var cancel = new Button { Content = "取消（Esc）" };
        var confirm = new Button { Content = "确定（Enter）", Classes = { "primary" } };
        cancel.Click += (_, _) => dialog.Close();
        confirm.Click += (_, _) => { result = true; dialog.Close(); };
        dialog.KeyDown += (_, args) =>
        {
            if (args.Key == Key.Escape) { args.Handled = true; dialog.Close(); }
            else if (args.Key == Key.Enter) { args.Handled = true; result = true; dialog.Close(); }
        };
        dialog.Opened += (_, _) => confirm.Focus();
        var messageCard = createTextCard(new TextBlock
        {
            Text = message, TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        });
        dialog.Content = createSurface(CreateLayout(messageCard, CreateActions(cancel, confirm)));
        await dialog.ShowDialog(owner);
        return result;
    }

    private static Grid CreateLayout(Control contentCard, Control actions)
    {
        var grid = new Grid
        {
            Margin = new Thickness(20),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 14,
            Children = { contentCard, actions }
        };
        Grid.SetRow(actions, 1);
        return grid;
    }

    private static StackPanel CreateActions(params Control[] controls)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };
        foreach (var control in controls) panel.Children.Add(control);
        return panel;
    }
}
