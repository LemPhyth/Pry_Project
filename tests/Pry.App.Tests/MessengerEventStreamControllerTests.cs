using System.IO;
using System.Net;
using System.Text.Json;
using Pry.App.Services;
using Pry.Client;
using Pry.Contracts;
using Xunit;

namespace Pry.App.Tests;

/// <summary>
/// QA-P5-01：Runner 自己的判据在 <c>Pry.Client.Tests</c> 有十五例，但那十五例证明的是"Runner 什么时候该说什么"，
/// 证明不了"今天真正跑着的那条循环听没听见"。接线此前零判据——把 <c>Faulted</c> 那一行删掉，或者把重连后的
/// 补取接到已经离开的房间，全部门禁仍然全绿。
/// <para>
/// 仓库没有 headless Avalonia 设施，所以这里断言的是控制器交给 surface 的动作，不碰线程投递与控件本身。
/// 末两例特意用真 <see cref="ConversationEventStreamRunner"/> 跑一遍 <c>RunAsync</c>，而不是手工调 handler：
/// 前者只说"字段为空"，后者说"界面没被告知"——接线判据要落在行为上才算落在被删的那一行上。
/// </para>
/// </summary>
public sealed class MessengerEventStreamControllerTests
{
    [Fact]
    public async Task A_new_message_refetches_this_rooms_history_then_the_room_list()
    {
        var surface = new Surface();
        var controller = Create(surface, new Room());

        await controller.HandleEventAsync(Event("message.created"));

        Assert.Equal(["history:room-7:7", "rooms"], surface.Calls);
    }

    [Fact]
    public async Task A_read_receipt_refreshes_the_room_list_without_re_reading_history()
    {
        var surface = new Surface();
        var controller = Create(surface, new Room());

        await controller.HandleEventAsync(Event("conversation.read"));

        Assert.Equal(["rooms"], surface.Calls);
    }

    [Fact]
    public async Task The_terminal_phases_clear_the_sending_flag()
    {
        foreach (var type in new[] { "turn.cancelled", "turn.completed" })
        {
            var surface = new Surface();
            await Create(surface, new Room()).HandleEventAsync(Event(type));
            Assert.Equal(["sending:False"], surface.Calls);
        }
    }

    [Fact]
    public async Task A_failed_turn_says_what_to_do_next()
    {
        var surface = new Surface();
        await Create(surface, new Room()).HandleEventAsync(Event("turn.failed"));

        Assert.Equal(["sending:False", "status:模型回复失败，请重试"], surface.Calls);
    }

    [Fact]
    public async Task A_turn_phase_frame_reaches_the_phase_display_untouched()
    {
        var surface = new Surface();
        var payload = JsonSerializer.Deserialize<JsonElement>("""{"state":"Thinking"}""");

        await Create(surface, new Room()).HandleEventAsync(Event("turn.state", payload));

        Assert.Equal(["phase:Thinking"], surface.Calls);
    }

    [Fact]
    public async Task An_unrecognized_event_changes_nothing_and_breaks_nothing()
    {
        var surface = new Surface();

        await Create(surface, new Room()).HandleEventAsync(Event("provider.future.event"));

        // 前向兼容（PLT-011）：没听过的 type 既不渲染也不报错，更不能被当成断连。
        Assert.Empty(surface.Calls);
    }

    [Fact]
    public async Task Recovery_refetches_history_before_the_next_event()
    {
        var surface = new Surface();
        await Create(surface, new Room()).HandleResumedAsync();

        Assert.Equal(["history:room-7:7", "rooms"], surface.Calls);
    }

    [Fact]
    public void Reconnecting_is_the_only_state_worth_saying_so()
    {
        var reconnecting = new Surface();
        Create(reconnecting, new Room()).HandleStateChanged(PryStreamState.Reconnecting);
        Assert.Equal(["status:事件连接中断，正在重连…"], reconnecting.Calls);

        foreach (var state in new[] { PryStreamState.Connected, PryStreamState.Offline })
        {
            var silent = new Surface();
            Create(silent, new Room()).HandleStateChanged(state);
            Assert.Empty(silent.Calls);
        }
    }

    [Fact]
    public void A_terminated_stream_says_why_instead_of_hanging_on_reconnecting()
    {
        var surface = new Surface();
        var banned = new PryBackendException((HttpStatusCode)426, PryProtocol.MismatchErrorCode,
            "protocol mismatch", null, false);

        Create(surface, new Room()).HandleFaulted(banned);

        var written = Assert.Single(surface.Calls);
        Assert.Equal($"status:{PryErrorPresenter.Describe(banned).Body}", written);
        Assert.DoesNotContain("正在重连", written);
    }

    [Fact]
    public async Task A_stream_from_the_room_the_user_left_touches_nothing()
    {
        var surface = new Surface();
        var stale = Create(surface, new Room { Revision = 8 });

        await stale.HandleEventAsync(Event("message.created"));
        await stale.HandleResumedAsync();
        stale.HandleStateChanged(PryStreamState.Reconnecting);
        stale.HandleFaulted(new TimeoutException("预算到期"));

        // 切房后旧流的游标已经换人：补历史、刷列表、改状态栏一律整条丢弃，不然旧房间会把新房间写脏。
        Assert.Empty(surface.Calls);
    }

    [Fact]
    public void Attaching_wires_every_callback_the_runner_can_call()
    {
        var runner = new ConversationEventStreamRunner((_, _) => Nothing());

        new MessengerEventStreamController("room-7", 7, () => 7, new Surface()).Attach(runner);

        // 少接一条不会有任何编译期信号：Runner 那边的回调字段全是可空的。
        Assert.NotNull(runner.EventReceived);
        Assert.NotNull(runner.Resumed);
        Assert.NotNull(runner.StateChanged);
        Assert.NotNull(runner.Faulted);
    }

    [Fact]
    public async Task A_ban_reaches_the_surface_through_the_real_runner_without_one_extra_retry()
    {
        var surface = new Surface();
        var banned = new PryBackendException((HttpStatusCode)426, PryProtocol.MismatchErrorCode,
            "protocol mismatch", null, false);
        var attempts = 0;
        var runner = new ConversationEventStreamRunner(
            (_, _) => { attempts++; return Failing(banned); },
            delay: (_, _) => { attempts++; return Task.CompletedTask; });
        new MessengerEventStreamController("room-7", 7, () => 7, surface).Attach(runner);

        await runner.RunAsync(TestContext.Current.CancellationToken);

        // 这条用真 Runner 而不手工调 HandleFaulted：删掉 Attach 里 Faulted 那一行时，
        // 上一例只报"字段为空"，这里报的是"界面没被告知"——接线判据落在行为上。
        Assert.Equal($"status:{PryErrorPresenter.Describe(banned).Body}", Assert.Single(surface.Calls));
        Assert.Equal(PryStreamState.Offline, runner.State);
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task The_real_runner_delivers_events_to_the_surface_in_order_then_reports_the_break()
    {
        var surface = new Surface();
        var options = new ConversationStreamOptions(TimeSpan.Zero, TimeSpan.Zero, 1);
        var runner = new ConversationEventStreamRunner(
            (_, _) => EventThenBreak(Event("message.created")), options,
            delay: (_, _) => Task.CompletedTask);
        new MessengerEventStreamController("room-7", 7, () => 7, surface).Attach(runner);

        await runner.RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(PryStreamState.Offline, runner.State);
        Assert.Equal(["history:room-7:7", "rooms"], surface.Calls[..2]);
        Assert.StartsWith("status:", surface.Calls[^1]);
    }

    private static async IAsyncEnumerable<ConversationEvent> Nothing()
    {
        await Task.Yield();
        yield break;
    }

    private static IAsyncEnumerable<ConversationEvent> Failing(Exception error) => throw error;

    private static async IAsyncEnumerable<ConversationEvent> EventThenBreak(ConversationEvent item)
    {
        await Task.Yield();
        yield return item;
        throw new IOException("事件流在收到任何后续事件前结束。");
    }

    private static MessengerEventStreamController Create(Surface surface, Room room)
        => new("room-7", 7, () => room.Revision, surface);

    private static ConversationEvent Event(string type, object? data = null)
        => new(3, type, DateTimeOffset.UnixEpoch, data ?? new object());

    private sealed class Room
    {
        public long Revision { get; set; } = 7;
    }

    private sealed class Surface : IMessengerStreamSurface
    {
        public List<string> Calls { get; } = [];

        public Task RefreshHistoryAsync(string roomId, long revision)
        {
            Calls.Add($"history:{roomId}:{revision}");
            return Task.CompletedTask;
        }

        public Task RefreshRoomsAsync()
        {
            Calls.Add("rooms");
            return Task.CompletedTask;
        }

        public void SetSending(bool sending) => Calls.Add($"sending:{sending}");

        public void SetStatusText(string text) => Calls.Add($"status:{text}");

        public void ShowTurnState(string? state) => Calls.Add($"phase:{state}");
    }
}
