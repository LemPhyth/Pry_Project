using System.Net;
using System.Net.Sockets;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Pry.App.Services;
using Pry.Client;
using Pry.Contracts;
using Xunit;

namespace Pry.App.Tests;

/// <summary>
/// UI-027 后半的判据：<see cref="Pry.Client.Tests.PryErrorPresentationTests"/> 判的是"这张表该说什么"，
/// 这里判的是"窗口有没有真的从这张表取话"。QA-P5-01 已经否过一次"只有表、接线无判据"的假绿，
/// 所以每一条都从界面实际渲染出来的那一行文本判起。
/// </summary>
public sealed class NoticeContentFactoryTests
{
    private const string ServerRawText = "message not found: 0198ccd3";

    private static PryBackendException NotFound() => new(
        HttpStatusCode.NotFound, "resource_not_found", ServerRawText, null);

    [Fact]
    public void The_body_is_the_presenters_sentence_and_the_server_text_only_reaches_the_collapsed_area()
    {
        var view = NoticeContentFactory.Create(NotFound());

        Assert.Equal(PryErrorPresenter.Describe(NotFound()).Body, view.Detail.Text);
        Assert.Contains("它可能已经被删除", view.Detail.Text);
        Assert.DoesNotContain(ServerRawText, view.Detail.Text);
        Assert.False(view.Technical.IsVisible);
        Assert.Contains(ServerRawText, view.Technical.Text);
    }

    [Fact]
    public void The_technical_area_opens_and_closes_from_its_own_label()
    {
        var view = NoticeContentFactory.Create(new PryBackendException(
            HttpStatusCode.InternalServerError, "internal_error", ServerRawText, "trace-1"));

        Assert.Equal("查看技术详情", view.Toggle.Content);
        view.Toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.True(view.Technical.IsVisible);
        Assert.Equal("收起技术详情", view.Toggle.Content);
        Assert.Contains("traceId=trace-1", view.Technical.Text);

        view.Toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.False(view.Technical.IsVisible);
        Assert.Equal("查看技术详情", view.Toggle.Content);
    }

    [Fact]
    public void Status_bar_sites_get_the_same_sentence_as_the_dialog_and_no_technical_text()
    {
        var error = new PryBackendException(HttpStatusCode.Conflict, "resource_conflict", ServerRawText, null);

        var status = NoticeContentFactory.StatusText(error);

        Assert.Equal(PryErrorPresenter.Describe(error).Body, status);
        Assert.DoesNotContain(ServerRawText, status);
        Assert.DoesNotContain("\n", status);
    }

    [Fact]
    public void The_startup_dialog_keeps_its_own_context_only_when_the_table_has_no_category()
    {
        var classified = PryErrorPresenter.Describe(new PryBackendException(
            HttpStatusCode.ServiceUnavailable, "runtime_missing", ServerRawText, null));
        var unknown = PryErrorPresenter.Describe(new InvalidOperationException(ServerRawText));

        Assert.Equal("本地推理组件没有准备好", NoticeContentFactory.StartupHeadline(classified));
        Assert.Equal(PryErrorKind.Unknown, unknown.Kind);
        Assert.Equal("Pry 无法启动本地后端", NoticeContentFactory.StartupHeadline(unknown));
    }

    /// <summary>禁令过界面这一层不能被软化：包装成"稍后重试"就是把永久失败说成一次抖动。</summary>
    [Fact]
    public void A_forbidden_retry_is_still_a_dead_end_after_the_window_renders_it()
    {
        var view = NoticeContentFactory.Create(new PryBackendException(
            HttpStatusCode.Unauthorized, PryProtocol.UnauthorizedErrorCode, "session token rejected", null,
            retryable: false));

        Assert.Contains("重新握手", view.Detail.Text);
        Assert.DoesNotContain("稍后", view.Detail.Text);
    }

    [Fact]
    public void A_refused_loopback_handshake_reaches_the_startup_dialog_as_a_core_that_never_came_up()
    {
        var refused = new HttpRequestException(
            "No connection could be made because the target machine 127.0.0.1:5078",
            new SocketException((int)SocketError.ConnectionRefused));

        Assert.Equal("本地核心没有启动成功", NoticeContentFactory.StartupHeadline(PryErrorPresenter.Describe(refused)));
        Assert.Contains("没有在本机监听", NoticeContentFactory.Create(refused).Detail.Text);
    }
}
