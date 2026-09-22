using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Pry.Api.Services;
using Pry.Contracts;
using Pry.Core.Memory;
using Pry.Core.Models;
using Xunit;

namespace Pry.Api.Tests;

public sealed class ConversationReadEndpointTests
{
    [Fact]
    public async Task Read_endpoints_validate_publish_and_survive_backend_restart()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pry-read-http-{Guid.NewGuid():N}");
        var ct = TestContext.Current.CancellationToken;
        var jsonOptions = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
        jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        long first = 0;
        try
        {
            for (var restart = 0; restart < 2; restart++)
            {
                await using var app = await BackendApplication.BuildAsync(configure: builder =>
                {
                    builder.Configuration["Pry:DataDirectory"] = directory;
                    builder.Configuration["Pry:ModelsDirectory"] = Path.Combine(directory, "models");
                    builder.Configuration["Pry:Url"] = "http://127.0.0.1:0";
                }, token: ct);
                await app.StartAsync(ct);
                try
                {
                    // 令牌是每进程随机的，重启后的客户端必须重新从新实例取，不能复用上一轮的。
                    using var http = app.CreateBackendHttpClient();
                    var db = app.Services.GetRequiredService<MemoryDatabase>();
                    if (restart == 0)
                    {
                        first = await db.AddMessageAsync("room", ChatRole.Assistant, "主动", null, ct, source: MessageSources.Proactive);
                        await db.AddMessageAsync("room", ChatRole.Assistant, "下一条", null, ct);
                        await db.AddMessageAsync("other", ChatRole.Assistant, "其他", null, ct);
                        using var mark = await http.PutAsJsonAsync("api/v1/conversations/room/read-state", new MarkConversationReadRequest(first), ct);
                        Assert.Equal(HttpStatusCode.OK, mark.StatusCode);
                        Assert.Equal(1, (await mark.Content.ReadFromJsonAsync<ConversationReadStateResponse>(ct))!.UnreadCount);
                        using var retry = await http.PutAsJsonAsync("api/v1/conversations/room/read-state", new MarkConversationReadRequest(first), ct);
                        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
                        foreach (var invalid in new[] { new MarkConversationReadRequest(null), new(-1), new(long.MaxValue) })
                        {
                            using var bad = await http.PutAsJsonAsync("api/v1/conversations/room/read-state", invalid, ct);
                            Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
                            Assert.Contains("validation_error", await bad.Content.ReadAsStringAsync(ct));
                        }
                        using var missing = await http.PutAsJsonAsync("api/v1/conversations/missing/read-state", new MarkConversationReadRequest(0), ct);
                        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
                        using var eventToken = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        await using var events = app.Services.GetRequiredService<ConversationSessionService>()
                            .EventsAsync("room", 0, eventToken.Token).GetAsyncEnumerator(eventToken.Token);
                        Assert.True(await events.MoveNextAsync()); Assert.Equal("conversation.read", events.Current.Type);
                        Assert.Equal(first, System.Text.Json.JsonSerializer.SerializeToElement(events.Current.Data)
                            .GetProperty("lastReadMessageId").GetInt64());
                        eventToken.CancelAfter(TimeSpan.FromMilliseconds(100));
                        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => events.MoveNextAsync().AsTask());
                    }
                    var state = await http.GetFromJsonAsync<ConversationReadStateResponse>("api/v1/conversations/room/read-state", ct);
                    Assert.Equal(first, state!.LastReadMessageId); Assert.Equal(1, state.UnreadCount);
                    var summary = await http.GetFromJsonAsync<UnreadSummaryResponse>("api/v1/conversations/unread-summary", ct);
                    Assert.Equal(new UnreadSummaryResponse(2, 2), summary);
                    var rooms = await http.GetFromJsonAsync<ConversationRoom[]>("api/v1/conversations?limit=1", jsonOptions, ct);
                    Assert.Single(rooms!);
                    var room = await http.GetFromJsonAsync<ConversationRoom>("api/v1/conversations/room", jsonOptions, ct);
                    Assert.Equal(first, room!.LastReadMessageId); Assert.Equal(1, room.UnreadCount); Assert.NotNull(room.LastMessageId);
                    Assert.Equal("not_loaded", app.Services.GetRequiredService<BackendRuntime>().Status.ModelState);
                }
                finally { await app.StopAsync(ct); }
            }
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
