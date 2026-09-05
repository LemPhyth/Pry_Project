using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Pry.App.Services;

public sealed class SettingsShell
{
    private readonly Border _sidebar;
    private readonly Border _topBar;
    private readonly Border _tipCard;
    private readonly Border _resizeGuide;
    private readonly TextBlock _tipValue;
    private readonly ScrollViewer _content;

    public SettingsShell(IReadOnlyList<SettingsPage> pages, string tip, IBrush accent, Func<bool> liveResize)
    {
        if (pages.Count == 0) throw new ArgumentException("设置窗口至少需要一个页面。", nameof(pages));
        var pageSnapshot = pages.ToArray();
        Categories = new ListBox
        {
            Padding = new Thickness(10, 18), Background = Brushes.Transparent,
            ItemsSource = pageSnapshot.Select(page => page.Title).ToArray(), SelectedIndex = 0
        };
        AutomationProperties.SetName(Categories, "设置分类");
        _tipValue = new TextBlock { Text = tip, TextWrapping = TextWrapping.Wrap, Foreground = accent, FontSize = 12 };
        var tips = new StackPanel { Spacing = 6, Children = { new TextBlock { Text = "Tips", FontWeight = FontWeight.SemiBold }, _tipValue } };
        _tipCard = new Border { Margin = new Thickness(12), Padding = new Thickness(12), CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(1), Child = tips };
        var sidebarBody = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), Children = { Categories, _tipCard } };
        Grid.SetRow(_tipCard, 1);
        _sidebar = new Border { MinWidth = 150, MaxWidth = 340, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(14), ClipToBounds = true, Child = sidebarBody };
        _content = new ScrollViewer { Content = pageSnapshot[0].Content };
        _content.Classes.Add("pry-settings-scroll");
        Categories.SelectionChanged += (_, _) =>
        {
            var index = Categories.SelectedIndex;
            ShowPage(pageSnapshot[index >= 0 && index < pageSnapshot.Length ? index : 0].Content);
        };
        SaveButton = new Button { Content = "保存并应用", Classes = { "primary" }, HorizontalAlignment = HorizontalAlignment.Right, Padding = new Thickness(24, 10) };
        var title = new TextBlock { Text = "设置", FontSize = 22, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center };
        var topContent = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { title, SaveButton } };
        Grid.SetColumn(SaveButton, 1);
        _topBar = new Border { BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(14), Padding = new Thickness(24, 14, 18, 10), Child = topContent };
        var splitter = new Border { Width = 12, Margin = new Thickness(0, 0, -6, 0), HorizontalAlignment = HorizontalAlignment.Right, Background = Brushes.Transparent, ZIndex = 20 };
        splitter.AttachedToVisualTree += (_, _) => splitter.Cursor ??= new Cursor(StandardCursorType.SizeWestEast);
        Layout = new Grid { Margin = new Thickness(10), ColumnDefinitions = new ColumnDefinitions("210,*"), RowDefinitions = new RowDefinitions("Auto,*"), ColumnSpacing = 10, RowSpacing = 10, Background = Brushes.Transparent, Children = { _sidebar, splitter, _topBar, _content } };
        Grid.SetRowSpan(_sidebar, 2);
        Grid.SetRowSpan(splitter, 2);
        Grid.SetColumn(_topBar, 1);
        Grid.SetColumn(_content, 1);
        Grid.SetRow(_content, 1);
        _resizeGuide = new Border { Width = 2, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Stretch, Background = accent, IsHitTestVisible = false, IsVisible = false, ZIndex = 30 };
        Grid.SetRowSpan(_resizeGuide, 2);
        Grid.SetColumnSpan(_resizeGuide, 2);
        Layout.Children.Add(_resizeGuide);
        BindResize(splitter, liveResize);
        ApplyColors(Brush.Parse("#17212B"), Brush.Parse("#202B36"), Brush.Parse("#263746"), accent);
    }

    public Grid Layout { get; }
    public ListBox Categories { get; }
    public Button SaveButton { get; }
    public Control? CurrentPage => _content.Content as Control;

    public void ShowPage(Control page)
    {
        _content.Content = page;
        _content.Offset = default;
    }

    public void ApplyColors(IBrush panel, IBrush card, IBrush border, IBrush accent)
    {
        _sidebar.Background = _topBar.Background = panel;
        _sidebar.BorderBrush = _topBar.BorderBrush = _tipCard.BorderBrush = border;
        _tipCard.Background = card;
        _tipValue.Foreground = _resizeGuide.Background = accent;
    }

    private void BindResize(Border splitter, Func<bool> liveResize)
    {
        var resizing = false;
        var startX = 0d;
        var startWidth = 210d;
        var pendingWidth = 210d;
        splitter.PointerPressed += (_, args) =>
        {
            if (!args.GetCurrentPoint(splitter).Properties.IsLeftButtonPressed) return;
            resizing = true;
            startX = args.GetPosition(Layout).X;
            startWidth = Layout.ColumnDefinitions[0].ActualWidth;
            pendingWidth = startWidth;
            _resizeGuide.RenderTransform = new TranslateTransform(pendingWidth, 0);
            _resizeGuide.IsVisible = !liveResize();
            args.Pointer.Capture(splitter);
            args.Handled = true;
        };
        splitter.PointerMoved += (_, args) =>
        {
            if (!resizing) return;
            pendingWidth = Math.Clamp(startWidth + args.GetPosition(Layout).X - startX, 150, 340);
            if (liveResize()) Layout.ColumnDefinitions[0].Width = new GridLength(pendingWidth);
            else _resizeGuide.RenderTransform = new TranslateTransform(pendingWidth, 0);
            args.Handled = true;
        };
        splitter.PointerReleased += (_, args) =>
        {
            if (!resizing) return;
            resizing = false;
            _resizeGuide.IsVisible = false;
            Layout.ColumnDefinitions[0].Width = new GridLength(pendingWidth);
            args.Pointer.Capture(null);
            args.Handled = true;
        };
        splitter.PointerCaptureLost += (_, _) => { resizing = false; _resizeGuide.IsVisible = false; };
    }
}

public sealed record SettingsPage(string Title, Control Content);
