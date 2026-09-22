using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Pry.Api.Services;
using Pry.Contracts;
using Pry.Core.Memory;
using Pry.Core.Models;
using Xunit;

namespace Pry.Api.Tests;

public sealed class ProactiveSchedulerTests
{
    [Fact]
    public async Task Runtime_return_fences_candidate_captured_before_generation_without_a_new_message()
    {
        await using var f = await Fixture.CreateAsync(); var ct = TestContext.Current.CancellationToken;
        await f.PrimeAsync(); f.Generator.Block = true;
        var run = f.Scheduler.RunOnceAsync(ct);
        await f.Generator.Started.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        await f.Sessions.NotifyActivityAsync("room", typing: false, returned: true, ct);
        f.Generator.Release.TrySetResult();
        await run.WaitAsync(TimeSpan.FromSeconds(5), ct);
        Assert.Empty(await f.Database.GetRecentMessagesAsync("room", 10, ct));
        var history = await f.Database.GetProactiveHistoryAsync(f.Stored.Policy, f.Clock.GetUtcNow(), ct);
        Assert.Equal("superseded", history.LastOutcome);
        Assert.Equal(0, history.SentToday);
    }

    [Fact]
    public async Task Scheduler_delivers_one_persisted_message_and_sse_then_waits_for_user()
    {
        await using var f = await Fixture.CreateAsync(); var ct = TestContext.Current.CancellationToken;
        await f.PrimeAsync();
        // turn.state 是电平类事件：不入持久日志、只扇出给订阅时已在线的读取方（迁移文档 §4.1 第 2/10 条）。
        // 所以"AgentSending 先于 message.created"这条时序只能靠先订阅后投递来证明；
        // 把电平类改回持久事件让断言变绿是禁止方向（QA-P1-03）。
        f.Sessions.PublishReadWatermark("room", 0);
        await using var events = f.Sessions.EventsAsync("room", 0, ct).GetAsyncEnumerator(ct);
        // 先取走这条持久 priming 事件：读到它才说明订阅真的建立了，之后的电平事件才算"当场收到"。
        Assert.True(await events.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2), ct));
        Assert.Equal("conversation.read", events.Current.Type);
        await Task.WhenAll(f.Scheduler.RunOnceAsync(ct), f.Scheduler.RunOnceAsync(ct));
        var message = Assert.Single(await f.Database.GetRecentMessagesAsync("room", 10, ct));
        Assert.Equal(MessageSources.Proactive, message.Source);
        Assert.Equal(1, f.Generator.Calls);
        var phases = new List<string>();
        do
        {
            Assert.True(await events.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2), ct));
            if (events.Current.Type == "turn.state") phases.Add(events.Current.Data.ToString() ?? "");
        } while (events.Current.Type != "message.created");
        Assert.Contains(phases, x => x.Contains("AgentSending", StringComparison.Ordinal));
        Assert.Equal("not_loaded", f.Runtime.Status.ModelState);
        Assert.Contains("proactive", events.Current.Data.ToString());
        Assert.Equal("awaiting_user_reply", (await f.Scheduler.GetStatusAsync(f.CharacterId, ct)).State);
        await f.Scheduler.RunOnceAsync(ct); Assert.Equal(1, f.Generator.Calls);
        var regeneration = await Assert.ThrowsAsync<InteractionRejectedException>(() => f.Sessions.RegenerateAsync("room", message.Id, ct));
        Assert.Equal("interaction_not_available", regeneration.Code);
        await f.Sessions.DeleteMessageAsync("room", message.Id, ct);
        await f.Sessions.UndoAsync("room", ct);
        Assert.Equal(MessageSources.Proactive, Assert.Single(await f.Database.GetRecentMessagesAsync("room", 10, ct)).Source);
    }

    [Theory]
    [InlineData("typing")]
    [InlineData("policy")]
    [InlineData("delete")]
    [InlineData("reconfigure")]
    [InlineData("input")]
    [InlineData("expired")]
    public async Task Activity_or_foreground_change_cancels_without_a_ghost_message(string change)
    {
        await using var f = await Fixture.CreateAsync(); var ct = TestContext.Current.CancellationToken;
        await f.PrimeAsync();
        f.Generator.Block = true;
        var run = f.Scheduler.RunOnceAsync(ct);
        await f.Generator.Started.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        switch (change)
        {
            case "typing": await f.ReportAsync(true); break;
            case "policy":
                await f.Policies.SaveAsync(f.CharacterId, new SaveProactivePolicyRequest(1, "room", false,
                    IdleSeconds: 60, MinimumIntervalSeconds: 60, TimeZoneId: "UTC", QuietHoursEnabled: false), ct); break;
            case "delete": await new ConversationApplicationService(f.Database, f.Operations).DeleteAsync("room", ct); break;
            case "reconfigure": await f.Sessions.ReconfigureAsync(_ => Task.CompletedTask, ct); break;
            case "input": await new ConversationApplicationService(f.Database, f.Operations).AddMessageAsync("room",
                new CreateMessageRequest(ChatRole.User, "回来了"), ct); break;
            case "expired": f.Clock.Advance(45); break;
        }
        await run.WaitAsync(TimeSpan.FromSeconds(5), ct);
        Assert.DoesNotContain(await f.Database.GetRecentMessagesAsync("room", 10, ct), x => x.Source == MessageSources.Proactive);
        var history = await f.Database.GetProactiveHistoryAsync(f.Stored.Policy, f.Clock.GetUtcNow(), ct);
        Assert.Equal("cancelled", history.LastOutcome); Assert.Equal(0, history.SentToday);
    }

    [Theory]
    [InlineData("")]
    [InlineData("failure")]
    [InlineData("long")]
    public async Task Failed_or_invalid_generation_backs_off_without_messages(string output)
    {
        await using var f = await Fixture.CreateAsync(); var ct = TestContext.Current.CancellationToken;
        await f.PrimeAsync(); f.Generator.Output = output;
        await f.Scheduler.RunOnceAsync(ct);
        Assert.Empty(await f.Database.GetRecentMessagesAsync("room", 10, ct));
        var history = await f.Database.GetProactiveHistoryAsync(f.Stored.Policy, f.Clock.GetUtcNow(), ct);
        Assert.Equal(output == "failure" ? "failed" : output == "long" ? "invalid_output" : "empty_output", history.LastOutcome);
        await f.Scheduler.RunOnceAsync(ct); Assert.Equal(1, f.Generator.Calls);
    }

    [Fact]
    public async Task Unloaded_model_and_busy_foreground_do_not_consume_an_attempt()
    {
        await using var f = await Fixture.CreateAsync(); var ct = TestContext.Current.CancellationToken;
        await f.PrimeAsync(); f.Generator.IsReady = false;
        Assert.Equal("model_not_loaded", (await f.Scheduler.GetStatusAsync(f.CharacterId, ct)).State);
        await f.Scheduler.RunOnceAsync(ct); Assert.Equal(0, f.Generator.Calls);
        f.Generator.IsReady = true;
        using (await f.Operations.EnterForegroundAsync(ct))
        {
            await f.Scheduler.RunOnceAsync(ct); Assert.Equal(0, f.Generator.Calls);
        }
        Assert.Null((await f.Database.GetProactiveHistoryAsync(f.Stored.Policy, f.Clock.GetUtcNow(), ct)).AttemptId);
    }

    [Fact]
    public async Task Idle_heartbeat_does_not_cancel_an_inflight_generation()
    {
        await using var f = await Fixture.CreateAsync(); var ct = TestContext.Current.CancellationToken;
        await f.PrimeAsync(); f.Generator.Block = true;
        var run = f.Scheduler.RunOnceAsync(ct);
        await f.Generator.Started.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        f.Clock.Advance(15); await f.ReportAsync();
        f.Generator.Release.TrySetResult();
        await run.WaitAsync(TimeSpan.FromSeconds(5), ct);
        Assert.Single(await f.Database.GetRecentMessagesAsync("room", 10, ct));
    }

    [Fact]
    public async Task Late_output_from_a_provider_ignoring_cancellation_is_discarded()
    {
        await using var f = await Fixture.CreateAsync(); var ct = TestContext.Current.CancellationToken;
        await f.PrimeAsync(); f.Generator.Block = true; f.Generator.IgnoreCancellation = true;
        var run = f.Scheduler.RunOnceAsync(ct);
        await f.Generator.Started.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        await f.ReportAsync(true);
        f.Generator.Release.TrySetResult();
        await run.WaitAsync(TimeSpan.FromSeconds(5), ct);
        Assert.Empty(await f.Database.GetRecentMessagesAsync("room", 10, ct));
    }

    [Fact]
    public async Task Hosted_scheduler_stops_and_records_cancellation_without_publishing()
    {
        await using var f = await Fixture.CreateAsync(); var ct = TestContext.Current.CancellationToken;
        await f.PrimeAsync(); f.Generator.Block = true;
        await f.Scheduler.StartAsync(ct);
        await f.Generator.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
        await f.Scheduler.StopAsync(ct).WaitAsync(TimeSpan.FromSeconds(5), ct);
        Assert.Empty(await f.Database.GetRecentMessagesAsync("room", 10, ct));
        Assert.Equal("cancelled", (await f.Database.GetProactiveHistoryAsync(f.Stored.Policy, f.Clock.GetUtcNow(), ct)).LastOutcome);
    }

    private sealed class Generator : IProactiveMessageGenerator
    {
        public bool IsReady { get; set; } = true;
        public int Calls { get; private set; }
        public bool Block { get; set; }
        public bool IgnoreCancellation { get; set; }
        public string Output { get; set; } = "要不要聊聊今天的趣事？";
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<string> GenerateAsync(ProactivePolicy policy, CancellationToken token)
        {
            Calls++; Started.TrySetResult();
            if (Block) await Release.Task.WaitAsync(IgnoreCancellation ? CancellationToken.None : token);
            if (Output == "failure") throw new InvalidOperationException("simulated provider error");
            return Output == "long" ? new string('字', 501) : Output;
        }
    }

    private sealed class TestClock : TimeProvider
    {
        private long _seconds;
        public override long TimestampFrequency => 1;
        public override long GetTimestamp() => Interlocked.Read(ref _seconds);
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse("2026-09-14T12:00:00Z").AddSeconds(GetTimestamp());
        public void Advance(int seconds) => Interlocked.Add(ref _seconds, seconds);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), $"pry-scheduler-{Guid.NewGuid():N}");
        private readonly string _clientId = Guid.NewGuid().ToString("N");
        private long _sequence;
        private readonly ModelProcessRegistry _registry = new(NullLogger<ModelProcessRegistry>.Instance);
        public MemoryDatabase Database { get; }
        public TestClock Clock { get; } = new();
        public CompanionOperationCoordinator Operations { get; }
        public CompanionActivityService Activity { get; }
        public BackendRuntime Runtime { get; }
        public ProactivePolicyService Policies { get; }
        public ConversationSessionService Sessions { get; }
        public Generator Generator { get; } = new();
        public ProactiveScheduler Scheduler { get; }
        public StoredProactivePolicy Stored { get; private set; } = null!;
        public string CharacterId => Runtime.Characters.First().Id;
        private Fixture()
        {
            var config = new ConfigurationManager(); config["Pry:DataDirectory"] = _directory;
            config["Pry:ModelsDirectory"] = Path.Combine(_directory, "models");
            Database = new(Path.Combine(_directory, "memory.db"));
            Operations = new(Clock); Activity = new(Database, Clock, Operations);
            Runtime = new(config, _registry, NullLogger<BackendRuntime>.Instance);
            Policies = new(Database, Runtime, Operations);
            Sessions = new(Database, Runtime, new MediaAssetStore(config), NullLogger<ConversationSessionService>.Instance,
                operations: Operations);
            Scheduler = new(Database, Runtime, Activity, Operations, Sessions, Generator, Clock, NullLogger<ProactiveScheduler>.Instance);
        }
        public static async Task<Fixture> CreateAsync()
        {
            var f = new Fixture(); var ct = TestContext.Current.CancellationToken;
            await f.Database.InitializeAsync(ct); await f.Runtime.StartAsync(ct);
            await f.Database.EnsureConversationAsync("room", f.CharacterId, ct);
            await f.Policies.SaveAsync(f.CharacterId, new SaveProactivePolicyRequest(0, "room", true,
                IdleSeconds: 60, MinimumIntervalSeconds: 60, TimeZoneId: "UTC", QuietHoursEnabled: false), ct);
            f.Stored = (await f.Database.GetProactivePolicyAsync(f.CharacterId, ct))!;
            return f;
        }
        public Task<CompanionActivityResponse> ReportAsync(bool typing = false) => Activity.ReportAsync(_clientId,
            new ReportCompanionActivityRequest(++_sequence, "room", 86400, IsTyping: typing), TestContext.Current.CancellationToken);
        public async Task PrimeAsync()
        {
            await ReportAsync();
            for (var i = 0; i < 4; i++) { Clock.Advance(15); await ReportAsync(); }
        }
        public async ValueTask DisposeAsync()
        {
            Scheduler.Dispose(); await Sessions.DisposeAsync(); await Runtime.StopAsync(CancellationToken.None);
            await _registry.DisposeAsync(); SqliteConnection.ClearAllPools(); Directory.Delete(_directory, true);
        }
    }
}
