using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Pry.Api.Services;
using Pry.Contracts;
using Pry.Core.Memory;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>RT-002 只读诊断入口：错误语义与「读状态不创建运行时」两条不变量。</summary>
public sealed class ConversationRuntimeEndpointTests
{
    [Fact]
    public async Task Runtime_state_is_read_only_and_reports_persistence_when_no_runtime_is_live()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pry-runtime-diagnostics-{Guid.NewGuid():N}");
        var ct = TestContext.Current.CancellationToken;
        try
        {
            await using var app = await BackendApplication.BuildAsync(configure: builder =>
            {
                builder.Configuration["Pry:DataDirectory"] = directory;
                builder.Configuration["Pry:ModelsDirectory"] = Path.Combine(directory, "models");
                builder.Configuration["Pry:Url"] = "http://127.0.0.1:0";
            }, token: ct);
            await app.StartAsync(ct);
            using var http = app.CreateBackendHttpClient();
            var db = app.Services.GetRequiredService<MemoryDatabase>();
            await db.EnsureConversationAsync("room", "character", ct);
            var epoch = await db.BumpRuntimeEpochAsync("room", ct);

            // 读取本身不能把运行时建起来：连读两次之后仍然只有持久层水位。
            for (var attempt = 0; attempt < 2; attempt++)
            {
                using var response = await http.GetAsync("api/v1/conversations/room/runtime-state", ct);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var state = await response.Content.ReadFromJsonAsync<ConversationRuntimeStateResponse>(
                    new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web), ct);
                Assert.NotNull(state);
                Assert.False(state.Started);
                Assert.Equal("room", state.ConversationId);
                Assert.Equal("character", state.CharacterId);
                Assert.Equal(epoch, state.Epoch);
                Assert.Equal(epoch, state.Persisted!.Epoch);
                Assert.Null(state.Phase);
                Assert.Null(state.ActiveTurn);
                Assert.Empty(state.RecentTurns);
            }
            Assert.Empty(app.Services.GetRequiredService<ConversationSessionService>().ActiveConversationIds);

            // 未启动的运行时没有轨迹可读，显式报错而不是回空数组冒充「没有事件」。
            using var trace = await http.GetAsync("api/v1/conversations/room/runtime-trace?limit=5", ct);
            Assert.Equal(HttpStatusCode.NotFound, trace.StatusCode);
            Assert.Contains("runtime_not_started", await trace.Content.ReadAsStringAsync(ct));

            using var missing = await http.GetAsync("api/v1/conversations/ghost/runtime-state", ct);
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
            Assert.Contains("conversation", await missing.Content.ReadAsStringAsync(ct));
        }
        finally
        {
            SqliteTestPools.Clear(Path.Combine(directory, "memory.db"));
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
