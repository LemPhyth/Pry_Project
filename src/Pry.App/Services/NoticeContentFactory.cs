using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Pry.Client;

namespace Pry.App.Services;

/// <summary>
/// 面向用户的文案位：界面只负责说"哪件事没做成"，**说什么、还能不能再试一律取 <see cref="PryErrorPresenter"/>**。
/// 原始异常串（"The response ended prematurely."、HTTP 状态、code、traceId）只进默认收起的技术详情，
/// 且必须可复制——反馈时用户要能把它带走。
/// </summary>
public static class NoticeContentFactory
{
    public sealed record NoticeView(StackPanel Content, TextBlock Detail, Button Toggle, SelectableTextBlock Technical);

    /// <summary>状态栏那种没有展开区的位点，只能有一句用户语言。</summary>
    public static string StatusText(Exception error) => PryErrorPresenter.Describe(error).Body;

    /// <summary>启动失败时异常常常落在未登记的档位，那一句要保住"启动失败"这个具体处境。</summary>
    public static string StartupHeadline(PryUserError error) =>
        error.Kind == PryErrorKind.Unknown ? "Pry 无法启动本地后端" : error.Headline;

    public static NoticeView Create(Exception error) => Build(PryErrorPresenter.Describe(error));

    private static NoticeView Build(PryUserError described)
    {
        var detail = new TextBlock { Text = described.Body, TextWrapping = TextWrapping.Wrap };
        var technical = new SelectableTextBlock
        {
            Text = described.TechnicalDetail,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Foreground = PryVisualTheme.Get("Muted"),
            Margin = new Thickness(0, 8, 0, 0),
            IsVisible = false,
        };
        var toggle = new Button
        {
            Content = "查看技术详情",
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        toggle.Click += (_, _) =>
        {
            technical.IsVisible = !technical.IsVisible;
            toggle.Content = technical.IsVisible ? "收起技术详情" : "查看技术详情";
        };
        return new NoticeView(new StackPanel { Spacing = 4, Children = { detail, toggle, technical } },
            detail, toggle, technical);
    }
}
