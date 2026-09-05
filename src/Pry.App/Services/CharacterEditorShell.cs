using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Pry.App.Services;

public sealed class CharacterEditorShell
{
    public CharacterEditorShell(Func<Control, Control> createSurface, IBrush accentBrush,
        CharacterAvatarEditor avatarEditor, CharacterPromptEditor promptEditor)
    {
        CardName = Box();
        Name = Box();
        Greeting = Box(70);
        CardList = new ListBox { Margin = new Thickness(12), Background = Brushes.Transparent };
        CurrentHint = new TextBlock { Foreground = accentBrush, TextWrapping = TextWrapping.Wrap };
        NewCard = new Button { Content = "＋ 新建角色卡", Margin = new Thickness(12, 0, 12, 12) };
        RemoveCard = new Button { Content = "删除角色卡" };
        Save = new Button { Content = "保存", Classes = { "primary" } };
        SaveAndSwitch = new Button { Content = "保存并切换到此角色" };

        var form = new StackPanel { Margin = new Thickness(24), Spacing = 10 };
        AddField(form, "角色卡名称（显示名-用途-版本）", CardName);
        AddField(form, "聊天显示名", Name);
        form.Children.Add(new TextBlock { Text = "角色头像", FontWeight = FontWeight.SemiBold });
        form.Children.Add(new TextBlock
        {
            Text = "从原始图片直接解码；拖动调整取景，滚轮缩放，不会压缩或改写原图。",
            Foreground = Brush.Parse("#8EA2B5"), TextWrapping = TextWrapping.Wrap
        });
        form.Children.Add(avatarEditor.View);
        form.Children.Add(CurrentHint);
        form.Children.Add(promptEditor.View);
        AddField(form, "开场白", Greeting);
        form.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8, Margin = new Thickness(0, 10, 0, 0),
            Children = { RemoveCard, SaveAndSwitch, Save }
        });

        var left = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"), Background = Brushes.Transparent,
            Children =
            {
                new TextBlock
                {
                    Text = "角色卡", FontSize = 21, FontWeight = FontWeight.SemiBold,
                    Margin = new Thickness(16, 18, 12, 4)
                },
                CardList, NewCard
            }
        };
        Grid.SetRow(CardList, 1);
        Grid.SetRow(NewCard, 2);
        var editorScroll = new ScrollViewer { Content = form, Background = Brushes.Transparent };
        var divider = new Border
        {
            Width = 1, Background = Brush.Parse("#263746"), HorizontalAlignment = HorizontalAlignment.Right
        };
        var layout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("210,*"), Background = Brushes.Transparent,
            Children = { left, divider, editorScroll }
        };
        Grid.SetColumn(editorScroll, 1);
        Window = new Window
        {
            Title = "角色卡管理", Width = 920, Height = 740, Background = Brushes.Transparent,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = createSurface(layout)
        };
    }

    public Window Window { get; }
    public ListBox CardList { get; }
    public TextBox CardName { get; }
    public TextBox Name { get; }
    public TextBox Greeting { get; }
    public TextBlock CurrentHint { get; }
    public Button NewCard { get; }
    public Button RemoveCard { get; }
    public Button Save { get; }
    public Button SaveAndSwitch { get; }

    private static TextBox Box(int height = 34) => new()
    {
        MinHeight = height, AcceptsReturn = height > 60, TextWrapping = TextWrapping.Wrap
    };

    private static void AddField(Panel target, string label, Control control)
    {
        target.Children.Add(new TextBlock { Text = label, FontWeight = FontWeight.SemiBold });
        target.Children.Add(control);
    }
}
