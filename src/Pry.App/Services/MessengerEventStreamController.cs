using System.Text.Json;
using Pry.Client;
using Pry.Contracts;

namespace Pry.App.Services;

/// <summary>
/// 控制器要做的那五件事与界面怎么做到它们之间的一层薄接口。刻意不叫"视图"：它不渲染任何东西，
/// 只是把"补历史、刷列表、改状态栏、改发送态、改轮次相位"这五个可数的动作交出去。
/// </summary>
public interface IMessengerStreamSurface
{
    Task RefreshHistoryAsync(string roomId, long revision);

    Task RefreshRoomsAsync();

    void SetSending(bool sending);

    void SetStatusText(string text);

    void ShowTurnState(string? state);
}

/// <summary>
/// 事件流宿主到界面的接线（UI-027）。抽成可测类是本仓库对"窗口代码后置里那些判不掉的逻辑"的既有做法
/// （<c>tests/Pry.App.Tests</c> 里的 *Controller* / *Factory* 系列），因为仓库没有 headless Avalonia 设施。
/// <para>
/// 为什么这一层需要自己的用例：<see cref="ConversationEventStreamRunner"/> 的判据在
/// <c>Pry.Client.Tests</c> 有十五例，但它只证明"Runner 什么时候该说什么"，证明不了"界面听没听见"。
/// 接线本身此前零判据——把 <c>Faulted</c> 那一行删掉，或者把重连后的补取接到已经离开的房间，
/// 全部门禁仍然全绿（QA-P5-01）。这里把四个回调的判定集中成一处，切房的 <c>revision</c> 门成为它的不变量。
/// </para>
/// </summary>
public sealed class MessengerEventStreamController(
    string roomId,
    long revision,
    Func<long> currentRevision,
    IMessengerStreamSurface surface)
{
    /// <summary>接线只有一个入口：四个回调在这里一次绑完，窗口侧不再各自 lambda。</summary>
    public void Attach(ConversationEventStreamRunner runner)
    {
        runner.EventReceived = HandleEventAsync;
        runner.Resumed = HandleResumedAsync;
        runner.StateChanged = HandleStateChanged;
        runner.Faulted = HandleFaulted;
    }

    /// <summary>切房之后旧流还会收到属于上一个房间的事件：游标已经换人，这里只能整条丢弃。</summary>
    public bool IsCurrentRoom => revision == currentRevision();

    public async Task HandleEventAsync(ConversationEvent item)
    {
        if (!IsCurrentRoom) return;
        switch (item.Type)
        {
            case "message.created":
                await surface.RefreshHistoryAsync(roomId, revision);
                await surface.RefreshRoomsAsync();
                break;
            case "conversation.read":
                await surface.RefreshRoomsAsync();
                break;
            case "turn.cancelled":
            case "turn.completed":
                surface.SetSending(false);
                break;
            case "turn.failed":
                surface.SetSending(false);
                surface.SetStatusText("模型回复失败，请重试");
                break;
            case "turn.state" when item.Data is JsonElement payload && payload.TryGetProperty("state", out var phase):
                surface.ShowTurnState(phase.GetString());
                break;
            // 未识别的 type 落到这里什么都不做，也不断连：这就是"只增 minor"的前向兼容判据（PLT-011）。
        }
    }

    /// <summary>断线期间落库的消息不会出现在续传游标之后，所以重连成功的第一件事是补取历史。</summary>
    public async Task HandleResumedAsync()
    {
        if (!IsCurrentRoom) return;
        await surface.RefreshHistoryAsync(roomId, revision);
        await surface.RefreshRoomsAsync();
    }

    public void HandleStateChanged(PryStreamState state)
    {
        // 连接状态同样受切房门约束：旧流的"正在重连"不该盖掉新房间的状态栏。
        if (state != PryStreamState.Reconnecting || !IsCurrentRoom) return;
        surface.SetStatusText("事件连接中断，正在重连…");
    }

    /// <summary>Runner 说"不再重连"才是终点：禁令与连续失败到上限共用这一个出口，文案由呈现表单点给出。</summary>
    public void HandleFaulted(Exception error)
    {
        if (!IsCurrentRoom) return;
        surface.SetStatusText(PryErrorPresenter.Describe(error).Body);
    }
}
