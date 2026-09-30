using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pry.Api.Services;
using Pry.Contracts;
using Pry.Core.Memory;
using Pry.Core.Models;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// PLAT-005 M0-c 的传输层判据：游标持久化、显式的 buffer_expired 宣告与保留区间。
/// 契约见 docs/protocol-versioning-and-websocket-migration.md §4.1。
/// </summary>
public sealed class ProtocolEventCursorTests
{
    [Fact]
    public async Task Stale_cursor_reports_buffer_expired_once_and_then_replays()
    {
        using var backend = await Backend.StartAsync(TestContext.Current.CancellationToken);
        var ct = TestContext.Current.CancellationToken;
        var sequences = await backend.PublishWatermarksAsync(3, ct);

        // 把窗口收紧到只剩最新一条，等价于客户端关掉一个周末之后的状态。
        await backend.Database.PruneProtocolEventsAsync(1, 365, 200_000, ct);

        var frames = await backend.ReadEventFramesAsync(after: sequences[0], count: 2, ct);

        // 丢了事件必须由服务端说出来：客户端只看到"没有新事件"时无法区分安静与丢帧。
        var (resumedId, resumedType, resumedPayload) = frames[0];
        Assert.Equal("stream.resumed", resumedType);
        Assert.Equal(sequences[^1], resumedId);
        Assert.Equal("buffer_expired", resumedPayload.GetProperty("reason").GetString());
        Assert.Equal(sequences[^1], resumedPayload.GetProperty("firstRetained").GetInt64());
        Assert.Equal(sequences[^1], resumedPayload.GetProperty("latestSequence").GetInt64());

        // 宣告之后照常补投窗口内存活的那条，不多不少。
        Assert.Equal(("conversation.read", sequences[^1]), (frames[1].Event, frames[1].Id));
    }

    [Fact]
    public async Task Cursor_inside_the_window_is_silent_and_replays_without_announcement()
    {
        using var backend = await Backend.StartAsync(TestContext.Current.CancellationToken);
        var ct = TestContext.Current.CancellationToken;
        var sequences = await backend.PublishWatermarksAsync(3, ct);

        var frames = await backend.ReadEventFramesAsync(after: sequences[0], count: 2, ct);
        Assert.All(frames, frame => Assert.NotEqual("stream.resumed", frame.Event));
        Assert.Equal(sequences.Skip(1).ToList(), frames.Select(x => x.Id).ToList());
    }

    [Fact]
    public async Task Live_only_subscription_is_never_reported_as_expired()
    {
        using var backend = await Backend.StartAsync(TestContext.Current.CancellationToken);
        var ct = TestContext.Current.CancellationToken;
        await backend.EnsureConversationAsync(ct);
        await backend.Database.PruneProtocolEventsAsync(1, 365, 200_000, ct);

        // after=-1 的语义是"从此刻起"，它不可能越界；判成 buffer_expired 会让每次重连都白取一次历史。
        using var response = await backend.GetStreamAsync(after: -1, ct);
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        // 订阅必须先建立，之后的写入才有资格只被直播一次；空窗期由心跳注释帧填充。
        backend.Service<ConversationSessionService>().PublishReadWatermark("room", 7);
        var frame = await ReadOneFrameAsync(reader, ct);
        Assert.NotEqual("stream.resumed", frame.Event);
        Assert.Equal("conversation.read", frame.Event);
        Assert.Equal(7, frame.Data.GetProperty("lastReadMessageId").GetInt64());
    }

    [Fact]
    public async Task Cursor_survives_a_process_restart_and_the_capability_is_declared()
    {
        var directory = Backend.NewDataDirectory();
        using var first = await Backend.StartAsync(directory, TestContext.Current.CancellationToken);
        var ct = TestContext.Current.CancellationToken;
        var sequences = await first.PublishWatermarksAsync(2, ct);
        first.Stop();

        using var second = await Backend.StartAsync(directory, ct);
        var info = await second.Http.GetFromJsonAsync<ProtocolInfoResponse>("api/v1/info", ct);
        Assert.NotNull(info);
        // 序号每会话重启归零是本片要解决的既有缺陷：旧游标会在新序号爬到该值之前静默收不到任何事件。
        Assert.Contains("durable-event-cursor", info.Features);
        // 读侧引用 Core 的写侧常量，不再另写一个数字：两处硬编码并存时契约漂移会伪装成产品缺陷。
        Assert.Equal(MemoryDatabase.SchemaVersion, info.SchemaVersion);

        var frames = await second.ReadEventFramesAsync(after: sequences[0], count: 1, ct);
        var only = Assert.Single(frames);
        Assert.Equal(("conversation.read", sequences[^1]), (only.Event, only.Id));
    }

    [Fact]
    public async Task Http_message_delete_takes_the_plaintext_out_of_the_replayable_journal()
    {
        using var backend = await Backend.StartAsync(TestContext.Current.CancellationToken);
        var ct = TestContext.Current.CancellationToken;
        await backend.EnsureConversationAsync(ct);
        const string secret = "明文：周三去医院复诊";
        var messageId = await backend.Database.AddMessageAsync("room", ChatRole.User, secret, null, ct);
        // 按真实发布者（ConversationSessionService.SubmitAsync）的负载形状写入，而不是自造一份精简格式。
        await backend.Database.AppendProtocolEventAsync("room", "message.created",
            $$"""{"messageId":{{messageId}},"role":"User","content":"{{secret}}"}""", ct);

        var response = await backend.Http.DeleteAsync($"api/v1/conversations/room/messages/{messageId}", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // 删完还剩删除本身产生的那条 conversation.changed；关键是那条消息的正文一条都不许留下。
        var surviving = await backend.Database.ReadProtocolEventsAsync("room", 0, 100, ct);
        Assert.Equal(new[] { "conversation.changed" }, surviving.Select(x => x.Type).ToList());
        Assert.All(surviving, row => Assert.DoesNotContain(secret, Decode(row.PayloadJson), StringComparison.Ordinal));

        // 遮蔽会在窗口里留下空洞，所以 after=0 的重连方先收到 buffer_expired、再收到变更事件；
        // 关键是它任何一帧都拿不到被删正文——删除必须在游标流里同样成立。
        var frames = await backend.ReadEventFramesAsync(after: 0, count: 2, ct);
        Assert.Equal("conversation.changed", frames[^1].Event);
        Assert.All(frames, frame => Assert.DoesNotContain(secret, Decode(frame.Data.GetRawText()),
            StringComparison.Ordinal));
        Assert.DoesNotContain("message.created", string.Join(',', frames.Select(x => x.Event)),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Retention_band_clamps_a_broken_configuration_to_usable_values()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Pry:ProtocolEventMaxPerConversation"] = "0",
            ["Pry:ProtocolEventMaxAgeDays"] = "0",
            ["Pry:ProtocolEventMaxTotalRows"] = "0"
        }).Build();

        // 配成 0 若原样生效，每次重连都会判 buffer_expired，补投机制退化成轮询。
        Assert.Equal((ProtocolEventRetentionPolicy.MinPerConversation, 1, ProtocolEventRetentionPolicy.MinTotalRows),
            ProtocolEventRetention.Read(configuration));

        var oversized = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Pry:ProtocolEventMaxPerConversation"] = "999999",
            ["Pry:ProtocolEventMaxAgeDays"] = "999999",
            ["Pry:ProtocolEventMaxTotalRows"] = "999999999"
        }).Build();
        Assert.Equal((ProtocolEventRetentionPolicy.MaxPerConversationLimit, 365,
            ProtocolEventRetentionPolicy.MaxTotalRowsLimit), ProtocolEventRetention.Read(oversized));

        Assert.Equal((ProtocolEventRetentionPolicy.DefaultPerConversation,
            ProtocolEventRetentionPolicy.DefaultMaxAgeDays, ProtocolEventRetentionPolicy.DefaultTotalRows),
            ProtocolEventRetention.Read(new ConfigurationBuilder().Build()));
    }

    /// <summary>
    /// 取出 JSON 中全部字符串值的解码结果。默认编码器会把中文写成 \uXXXX，
    /// 直接拿原文做包含判断会永远"没找到"，等于什么都没验。
    /// </summary>
    private static string Decode(string json)
    {
        using var document = JsonDocument.Parse(json);
        var text = new StringBuilder();
        AppendStrings(document.RootElement, text);
        return text.ToString();
    }

    private static void AppendStrings(JsonElement element, StringBuilder text)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    AppendStrings(property.Value, text);
                    text.Append(' ');
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    AppendStrings(item, text);
                    text.Append(' ');
                }
                break;
            case JsonValueKind.String:
                text.Append(element.GetString());
                break;
        }
    }

    private static async Task<(long Id, string Event, JsonElement Data)> ReadOneFrameAsync(StreamReader reader,
        CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(10));
        long id = 0;
        string? type = null;
        string? data = null;
        while (true)
        {
            var line = await reader.ReadLineAsync(deadline.Token);
            if (line is null) throw new InvalidOperationException("事件流在帧完整之前关闭。");
            if (line.Length == 0)
            {
                // 心跳注释帧以 ':' 开头，落到这里时 data 仍为 null，直接继续等下一帧。
                if (data is null) { id = 0; type = null; continue; }
                // data 行是整条 ConversationEvent 的 JSON，业务负载在它的 data 字段里。
                var envelope = JsonDocument.Parse(data).RootElement;
                Assert.Equal(type, envelope.GetProperty("type").GetString());
                Assert.Equal(id, envelope.GetProperty("sequence").GetInt64());
                return (id, type ?? "message", envelope.GetProperty("data").Clone());
            }
            if (line.StartsWith("id: ", StringComparison.Ordinal)) id = long.Parse(line[4..]);
            else if (line.StartsWith("event: ", StringComparison.Ordinal)) type = line[7..];
            else if (line.StartsWith("data: ", StringComparison.Ordinal)) data = line[6..];
        }
    }

    /// <summary>起了真实回环后端的测试夹具；游标语义只有穿过 Kestrel 才算被证明。</summary>
    private sealed class Backend : IDisposable
    {
        private readonly WebApplication app;
        private readonly string dataDirectory;

        private Backend(string dataDirectory, WebApplication app, HttpClient http, MemoryDatabase database)
        {
            this.dataDirectory = dataDirectory;
            this.app = app;
            Http = http;
            Database = database;
        }

        public HttpClient Http { get; }
        public MemoryDatabase Database { get; }

        public static string NewDataDirectory()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"pry-cursor-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            return directory;
        }

        public static Task<Backend> StartAsync(CancellationToken token) => StartAsync(NewDataDirectory(), token);

        public static async Task<Backend> StartAsync(string directory, CancellationToken token)
        {
            var app = await BackendApplication.BuildAsync(configure: builder =>
            {
                builder.Configuration["Pry:DataDirectory"] = directory;
                builder.Configuration["Pry:ModelsDirectory"] = Path.Combine(directory, "models");
                builder.Configuration["Pry:Url"] = "http://127.0.0.1:0";
                // 没有事件时响应头要等第一个心跳注释帧才发出；心跳放快到下限，
                // 用例才能证明"订阅已建立"而不是把每个用例都拖成 15 秒。
                builder.Configuration["Pry:EventHeartbeatMs"] = "1000";
            }, token: token);
            await app.StartAsync(token);
            return new Backend(directory, app, app.CreateBackendHttpClient(),
                app.Services.GetRequiredService<MemoryDatabase>());
        }

        public T Service<T>() where T : notnull => app.Services.GetRequiredService<T>();

        public Task EnsureConversationAsync(CancellationToken token) =>
            Database.EnsureConversationAsync("room", "character", token);

        /// <summary>用后端已有的公开写入路径产生可重放事件，不去伪造一份日志实现。</summary>
        public async Task<long[]> PublishWatermarksAsync(int count, CancellationToken token)
        {
            await EnsureConversationAsync(token);
            for (var index = 0; index < count; index++)
                Service<ConversationSessionService>().PublishReadWatermark("room", index + 1);
            // 序号是服务端分配的，测试不猜它的值，只从日志本身读回来。
            return (await Database.ReadProtocolEventsAsync("room", 0, 100, token)).Select(x => x.Sequence).ToArray();
        }

        public void Stop() => app.StopAsync(CancellationToken.None).GetAwaiter().GetResult();

        public async Task<List<(long Id, string Event, JsonElement Data)>> ReadEventFramesAsync(long after,
            int count, CancellationToken token)
        {
            using var response = await GetStreamAsync(after, token);
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            using var reader = new StreamReader(stream);
            var frames = new List<(long, string, JsonElement)>();
            for (var index = 0; index < count; index++) frames.Add(await ReadOneFrameAsync(reader, token));
            return frames;
        }

        public async Task<HttpResponseMessage> GetStreamAsync(long after, CancellationToken token)
        {
            var response = await Http.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, $"api/v1/conversations/room/events?after={after}"),
                HttpCompletionOption.ResponseHeadersRead, token);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return response;
        }

        public void Dispose()
        {
            app.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
            app.DisposeAsync().AsTask().GetAwaiter().GetResult();
            Http.Dispose();
            SqliteTestPools.Clear(Path.Combine(dataDirectory, "memory.db"));
            if (Directory.Exists(dataDirectory)) Directory.Delete(dataDirectory, recursive: true);
        }
    }
}
