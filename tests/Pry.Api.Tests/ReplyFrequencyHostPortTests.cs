using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Pry.Api.Services;
using Pry.Contracts;
using Pry.Core.Memory;
using Pry.Core.Runtime;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// 契约 §7 第 1 步"宿主端口"的落地证明：用户可设的回复频率经 preferences 走到
/// <c>ConversationRuntimeOptions</c>，而不是只活在假时钟单测里。
/// 这里刻意不配置任何模型——频率 0 下永不成轮，composer 一次都不会被调用，
/// 所以"零模型调用"是由测试根本没有模型这一事实兜住的，不是靠自述的计数器。
/// </summary>
public sealed class ReplyFrequencyHostPortTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Reply_frequency_round_trips_and_rejects_values_outside_zero_to_one()
    {
        await using var backend = await Backend.CreateAsync();
        var ct = TestContext.Current.CancellationToken;

        var accepted = await backend.Http.PatchAsJsonAsync("api/v1/preferences",
            new { turnTaking = new { replyFrequency = 0.25 } }, Web, ct);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        var preferences = await accepted.Content.ReadFromJsonAsync<ClientPreferencesResponse>(Web, ct);
        Assert.NotNull(preferences);
        Assert.Equal(0.25, preferences.TurnTaking!.ReplyFrequency);

        foreach (var rejected in new[] { 1.5, -0.1 })
        {
            using var response = await backend.Http.PatchAsJsonAsync("api/v1/preferences",
                new { turnTaking = new { replyFrequency = rejected } }, Web, ct);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("turnTaking.replyFrequency", await response.Content.ReadAsStringAsync(ct));
        }

        // 被拒的那一次不得留下半个配置：0.25 仍然是当前值。
        using var unchanged = await backend.Http.GetAsync("api/v1/preferences", ct);
        var current = await unchanged.Content.ReadFromJsonAsync<ClientPreferencesResponse>(Web, ct);
        Assert.NotNull(current);
        Assert.Equal(0.25, current.TurnTaking!.ReplyFrequency);
    }

    [Fact]
    public async Task Silent_frequency_reaches_the_live_runtime_and_never_opens_a_turn()
    {
        await using var backend = await Backend.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        var configured = await backend.Http.PatchAsJsonAsync("api/v1/preferences",
            new { turnTaking = new { replyFrequency = 0d } }, Web, ct);
        Assert.Equal(HttpStatusCode.OK, configured.StatusCode);
        await backend.Database.EnsureConversationAsync("room", "character", ct);

        using var submitted = await backend.Http.PostAsJsonAsync("api/v1/conversations/room/turns",
            new SubmitTurnRequest("这条不该有回应", Immediate: true), Web, ct);
        // 该路由的成功语义就是 202 + events 游标（`ChatController.Submit`），落库在返回前已完成。
        Assert.Equal(HttpStatusCode.Accepted, submitted.StatusCode);

        var state = await backend.RuntimeStateAsync("room", ct);
        Assert.True(state.Started);
        Assert.Equal(0d, state.ReplyFrequency);
        Assert.Equal(nameof(ReplyTriggerMode.MessageCount), state.TriggerMode);
        Assert.False(state.IsGroupChat);
        Assert.Equal(nameof(RuntimePhase.Idle), state.Phase);
        Assert.Equal(0, state.PendingInputCount);
        // 一轮都不曾开过：不是"开了又取消"，是门没放行。没有模型可选，成轮必然留下失败轮。
        Assert.Empty(state.RecentTurns);

        using var trace = await backend.Http.GetAsync("api/v1/conversations/room/runtime-trace?limit=50", ct);
        Assert.Equal(HttpStatusCode.OK, trace.StatusCode);
        Assert.Contains("consumed_silently", await trace.Content.ReadAsStringAsync(ct));

        // 静默丢的是临时缓冲，不是用户的话。
        var message = Assert.Single(await backend.Database.GetRecentMessagesAsync("room", 10, ct));
        Assert.Equal(ChatRole.User, message.Role);
    }

    [Fact]
    public async Task Changing_the_frequency_replaces_the_live_runtime_and_the_new_one_carries_the_new_value()
    {
        await using var backend = await Backend.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        await backend.Database.EnsureConversationAsync("room", "character", ct);
        var silent = await backend.Http.PatchAsJsonAsync("api/v1/preferences",
            new { turnTaking = new { replyFrequency = 0d } }, Web, ct);
        Assert.Equal(HttpStatusCode.OK, silent.StatusCode);
        using (var first = await backend.Http.PostAsJsonAsync("api/v1/conversations/room/turns",
                   new SubmitTurnRequest("第一条"), Web, ct))
            Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.True((await backend.RuntimeStateAsync("room", ct)).Started);

        // 频率挂在运行时实例上，不在每次读取时重读配置，所以换配置必须换掉内存里那个实例。
        var changed = await backend.Http.PatchAsJsonAsync("api/v1/preferences",
            new { turnTaking = new { replyFrequency = 0.25, debounceMs = 30_000, maxPendingMs = 60_000 } }, Web, ct);
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.False((await backend.RuntimeStateAsync("room", ct)).Started);

        using var second = await backend.Http.PostAsJsonAsync("api/v1/conversations/room/turns",
            new SubmitTurnRequest("第二条"), Web, ct);
        Assert.Equal(HttpStatusCode.Accepted, second.StatusCode);
        var state = await backend.RuntimeStateAsync("room", ct);
        Assert.True(state.Started);
        Assert.Equal(0.25, state.ReplyFrequency);
    }

    /// <summary>一个后端实例一套临时数据目录；本仓门禁与 `measure-fixed-eval.ps1` 都不允许复用目录。</summary>
    private sealed class Backend(WebApplication app, HttpClient http, MemoryDatabase database, string directory)
        : IAsyncDisposable
    {
        public HttpClient Http { get; } = http;
        public MemoryDatabase Database { get; } = database;

        public static async Task<Backend> CreateAsync()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"pry-frequency-host-port-{Guid.NewGuid():N}");
            var ct = TestContext.Current.CancellationToken;
            var app = await BackendApplication.BuildAsync(configure: builder =>
            {
                builder.Configuration["Pry:DataDirectory"] = directory;
                builder.Configuration["Pry:ModelsDirectory"] = Path.Combine(directory, "models");
                builder.Configuration["Pry:Url"] = "http://127.0.0.1:0";
            }, token: ct);
            await app.StartAsync(ct);
            return new Backend(app, app.CreateBackendHttpClient(),
                app.Services.GetRequiredService<MemoryDatabase>(), directory);
        }

        public async Task<ConversationRuntimeStateResponse> RuntimeStateAsync(string conversationId,
            CancellationToken token)
        {
            using var response = await Http.GetAsync($"api/v1/conversations/{conversationId}/runtime-state", token);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var state = await response.Content.ReadFromJsonAsync<ConversationRuntimeStateResponse>(Web, token);
            Assert.NotNull(state);
            return state;
        }

        public async ValueTask DisposeAsync()
        {
            Http.Dispose();
            await app.DisposeAsync();
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
