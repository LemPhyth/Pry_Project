using System.Text.Json;
using Pry.Client;
using Pry.Contracts;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class RemoteConversationTurnManager : IAsyncDisposable
{
    private readonly PryBackendClient _api;
    private readonly string _conversationId;
    private readonly CancellationTokenSource _eventsCancellation = new();
    private readonly Task _eventsTask;
    private TurnState _state;

    public RemoteConversationTurnManager(PryBackendClient api, string conversationId)
    {
        _api = api; _conversationId = conversationId;
        _eventsTask = ReadEventsAsync(_eventsCancellation.Token);
    }

    public TurnState State => _state;
    public event Func<PlannedReplyMessage, Task>? AgentMessageDelivered;
    public event Action<TurnState>? StateChanged;
    public event Action<Exception>? Failed;
    public event Action<string>? Warning;

    public async Task<long> SubmitUserInputAsync(UserInputPart input, bool immediate = false,
        CancellationToken token = default)
    {
        var attachmentIds = new List<string>();
        foreach (var attachment in input.SafeAttachments)
        {
            await using var stream = File.OpenRead(attachment.Path);
            var uploaded = await _api.UploadAsync(stream, attachment.Name, ContentType(attachment.Path), token);
            attachmentIds.Add(uploaded.Id);
            foreach (var warning in uploaded.Warnings) Warning?.Invoke(warning);
        }
        var result = await _api.SubmitTurnAsync(_conversationId,
            new SubmitTurnRequest(input.Text, input.StickerId, immediate, attachmentIds), token);
        return result.MessageId;
    }

    public void NotifyInputActivity() { }

    public void CancelAgentReply() => _ = CancelSafelyAsync();

    public Task RegenerateAsync(long assistantMessageId, CancellationToken token = default) =>
        _api.RegenerateAsync(_conversationId, assistantMessageId, token);

    private async Task CancelSafelyAsync()
    {
        try { await _api.CancelTurnAsync(_conversationId, _eventsCancellation.Token); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Failed?.Invoke(ex); }
    }

    private async Task ReadEventsAsync(CancellationToken token)
    {
        var runner = new ConversationEventStreamRunner(
            (after, cancellation) => _api.ReadEventsAsync(_conversationId, after, cancellation));
        runner.EventReceived = async item =>
        {
            if (item.Data is not JsonElement data) return;
            if (item.Type == "turn.state" && data.TryGetProperty("state", out var stateValue)
                && Enum.TryParse<TurnState>(stateValue.GetString(), out var state))
            {
                _state = state; StateChanged?.Invoke(state);
            }
            else if (item.Type == "message.created" && data.TryGetProperty("role", out var role)
                     && role.GetString() == "Assistant")
            {
                var type = data.TryGetProperty("type", out var typeValue)
                           && Enum.TryParse<ReplyMessageType>(typeValue.GetString(), out var parsed) ? parsed : ReplyMessageType.Text;
                var message = new PlannedReplyMessage
                {
                    Id = data.GetProperty("messageId").GetInt64(), Type = type,
                    Content = data.TryGetProperty("content", out var content) ? content.GetString() : null,
                    StickerId = data.TryGetProperty("stickerId", out var sticker) && sticker.ValueKind != JsonValueKind.Null ? sticker.GetString() : null,
                    State = DeliveryState.Delivered
                };
                if (AgentMessageDelivered is not null) await AgentMessageDelivered(message);
            }
            else if (item.Type == "turn.failed") Failed?.Invoke(new InvalidOperationException(
                data.TryGetProperty("message", out var failure) ? failure.GetString() : "模型暂时无法完成回复"));
        };
        // 不挂 Resumed：本类的回复走追加渲染（MainWindow.AddAgentMessage），补一次历史就是同一条画两遍。
        // 断线窗口内落库的回复要等下一次重进房间才出现；收敛它需要在追加侧按消息 Id 去重，已记入待办。
        runner.StateChanged = state =>
        {
            if (state == PryStreamState.Reconnecting) Warning?.Invoke("与本地后端的事件连接中断，正在重连");
        };
        // Runner 说"不再重连"才是终点：禁令要给出可执行的修复说明，连续失败到上限也要说一句。
        runner.Faulted = error => Failed?.Invoke(new InvalidOperationException(
            PryErrorPresenter.Describe(error).Body, error));
        await runner.RunAsync(token);
    }

    public async ValueTask DisposeAsync()
    {
        _eventsCancellation.Cancel();
        try { await _eventsTask; } catch (OperationCanceledException) { }
        _eventsCancellation.Dispose();
    }

    private static string ContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png", ".jpg" or ".jpeg" => "image/jpeg", ".gif" => "image/gif", ".webp" => "image/webp",
        ".csv" => "text/csv", ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".wav" => "audio/wav", _ => "text/plain"
    };
}
