using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using Pry.App.Services;
using Xunit;

namespace Pry.App.Tests;

public sealed class SettingsShellTests
{
    [Fact]
    public void Category_selection_and_subpages_share_content_host()
    {
        var first = new StackPanel();
        var second = new StackPanel();
        var child = new StackPanel();
        var shell = new SettingsShell([new("模型", first), new("主题", second)], "提示", Brushes.Red, () => false);
        Assert.Same(first, shell.CurrentPage);
        Assert.Equal("设置分类", AutomationProperties.GetName(shell.Categories));
        shell.Categories.SelectedIndex = 1;
        Assert.Same(second, shell.CurrentPage);
        shell.ShowPage(child);
        Assert.Same(child, shell.CurrentPage);
        shell.Categories.SelectedIndex = 0;
        Assert.Same(first, shell.CurrentPage);
        Assert.Equal("保存并应用", shell.SaveButton.Content);
    }

    [Fact]
    public void Empty_navigation_is_rejected() =>
        Assert.Throws<ArgumentException>(() => new SettingsShell([], "", Brushes.Red, () => false));
}
