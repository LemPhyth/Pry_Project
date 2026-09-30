using Microsoft.Data.Sqlite;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Pry.Api.Services;
using Pry.Contracts;
using Pry.Core.Memory;
using Xunit;

namespace Pry.Api.Tests;

public sealed class ProactivePolicyTests
{
    [Fact]
    public async Task Activity_uses_monotonic_leases_idempotent_sequences_and_conservative_idle()
    {
        using var fixture = new DatabaseFixture();
        var token = TestContext.Current.CancellationToken;
        await fixture.Database.InitializeAsync(token);
        await fixture.Database.EnsureConversationAsync("room", "character", token);
        var clock = new TestClock();
        var service = new CompanionActivityService(fixture.Database, clock);
        var id = Guid.NewGuid().ToString("N");
        var request = new ReportCompanionActivityRequest(1, "room", 86400);
        Assert.False(service.Snapshot("room").Fresh);
        var first = await service.ReportAsync(id, request, token);
        Assert.Equal(TimeSpan.Zero, service.Snapshot("room").IdleFor);
        clock.Advance(15);
        Assert.Equal(first, await service.ReportAsync(id, request, token));
        await service.ReportAsync(id, request with { Sequence = 2 }, token);
        Assert.Equal(TimeSpan.FromSeconds(15), service.Snapshot("room").IdleFor);
        await Assert.ThrowsAsync<InteractionRejectedException>(() => service.ReportAsync(id, request, token));
        await Assert.ThrowsAsync<InteractionRejectedException>(() => service.ReportAsync(id,
            request with { Sequence = 2, IsTyping = true }, token));
        clock.Advance(45);
        Assert.False(service.Snapshot("room").Fresh);
        await service.ReportAsync(id, request with { Sequence = 3 }, token);
        Assert.Equal(TimeSpan.Zero, service.Snapshot("room").IdleFor);
        await service.ReportAsync(id, request with { Sequence = 4, IsRecording = true }, token);
        Assert.True(service.Snapshot("room").Busy);
        clock.Advance(15);
        await service.ReportAsync(id, request with { Sequence = 5 }, token);
        Assert.Equal(TimeSpan.Zero, service.Snapshot("room").IdleFor);
        var other = Guid.NewGuid().ToString("N");
        await service.ReportAsync(other, request with { ConversationId = null }, token);
        Assert.False(service.Snapshot("room").TargetMatches);
        await service.RemoveAsync(other, token);
        Assert.True(service.Snapshot("room").TargetMatches);
        clock.JumpWallClock(TimeSpan.FromDays(10));
        Assert.True(service.Snapshot("room").Fresh);
        await service.RemoveAsync(id, token); await service.RemoveAsync(id, token);
        Assert.False(service.Snapshot("room").Fresh);
        await Assert.ThrowsAsync<ApiValidationException>(() => service.ReportAsync("bad", request, token));
        await Assert.ThrowsAsync<ApiValidationException>(() => service.ReportAsync(id, request with { IdleSeconds = -1 }, token));
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.ReportAsync(id,
            request with { ConversationId = "missing" }, token));
    }

    [Fact]
    public async Task Activity_capacity_and_other_client_expiry_fail_closed()
    {
        using var fixture = new DatabaseFixture();
        await fixture.Database.InitializeAsync(TestContext.Current.CancellationToken);
        var clock = new TestClock();
        var service = new CompanionActivityService(fixture.Database, clock);
        var request = new ReportCompanionActivityRequest(1, null, 0);
        var ids = Enumerable.Range(0, 64).Select(_ => Guid.NewGuid().ToString("N")).ToArray();
        foreach (var id in ids) await service.ReportAsync(id, request, TestContext.Current.CancellationToken);
        var exception = await Assert.ThrowsAsync<InteractionRejectedException>(() => service.ReportAsync(
            Guid.NewGuid().ToString("N"), request, TestContext.Current.CancellationToken));
        Assert.Equal("activity_capacity", exception.Code);
        clock.Advance(45);
        await service.ReportAsync(ids[0], request with { Sequence = 2 }, TestContext.Current.CancellationToken);
        Assert.False(service.Snapshot("room").Fresh);
        foreach (var id in ids.Skip(1)) await service.RemoveAsync(id, TestContext.Current.CancellationToken);
        Assert.True(service.Snapshot("room").Fresh);
    }

    [Fact]
    public async Task Policy_validates_inputs_preserves_saved_state_and_enables_delivery_without_loading_models()
    {
        using var fixture = new DatabaseFixture();
        var token = TestContext.Current.CancellationToken;
        await fixture.Database.InitializeAsync(token);
        var config = new ConfigurationManager();
        config["Pry:DataDirectory"] = fixture.DirectoryPath;
        config["Pry:ModelsDirectory"] = Path.Combine(fixture.DirectoryPath, "models");
        await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var runtime = new BackendRuntime(config, registry, NullLogger<BackendRuntime>.Instance);
        await runtime.StartAsync(token);
        var service = new ProactivePolicyService(fixture.Database, runtime);
        var characterId = runtime.Characters.First().Id;
        await fixture.Database.EnsureConversationAsync("room", characterId, token);
        await fixture.Database.EnsureConversationAsync("other", "different", token);
        var defaults = await service.GetAsync(characterId, token);
        Assert.Equal(0, defaults.Revision); Assert.False(defaults.Enabled); Assert.True(defaults.DeliveryAvailable);
        var request = new SaveProactivePolicyRequest(0, "room");
        var saved = await service.SaveAsync(characterId, request, token);
        Assert.Equal(1, saved.Revision);
        Assert.Equal("revision_conflict", (await Assert.ThrowsAsync<InteractionRejectedException>(() =>
            service.SaveAsync(characterId, request, token))).Code);
        foreach (var invalid in new[] {
            request with { IdleSeconds = 59 }, request with { MinimumIntervalSeconds = 0 },
            request with { DailyLimit = 21 }, request with { TimeZoneId = "invalid-zone" },
            request with { QuietStartMinute = -1 }, request with { QuietEndMinute = 1440 },
            request with { QuietStartMinute = 480 }, request with { ExpectedRevision = -1 },
            request with { ConversationId = null } })
            await Assert.ThrowsAsync<ApiValidationException>(() => service.SaveAsync(characterId, invalid, token));
        Assert.Equal("target_mismatch", (await Assert.ThrowsAsync<InteractionRejectedException>(() =>
            service.SaveAsync(characterId, request with { ConversationId = "other" }, token))).Code);
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.SaveAsync(characterId,
            request with { ConversationId = "missing" }, token));
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.GetAsync("missing", token));
        Assert.Equal(saved, await service.GetAsync(characterId, token));
        var enabled = await service.SaveAsync(characterId, request with { ExpectedRevision = 1, Enabled = true }, token);
        Assert.True(enabled.Enabled); Assert.Equal(2, enabled.Revision);
        Assert.Equal("not_loaded", runtime.Status.ModelState);
        Assert.Empty(await fixture.Database.GetRecentMessagesAsync("room", 10, token));
        await runtime.StopAsync(token);
    }

    [Fact]
    public async Task Http_endpoints_round_trip_policies_and_map_validation_and_conflicts()
    {
        using var fixture = new DatabaseFixture();
        var token = TestContext.Current.CancellationToken;
        await using var app = await BackendApplication.BuildAsync(configure: builder =>
        {
            builder.Configuration["Pry:DataDirectory"] = fixture.DirectoryPath;
            builder.Configuration["Pry:ModelsDirectory"] = Path.Combine(fixture.DirectoryPath, "models");
            builder.Configuration["Pry:Url"] = "http://127.0.0.1:0";
        }, token: token);
        await app.StartAsync(token);
        try
        {
            // 令牌每进程随机，重启后必须向新实例重新取一次。
            using var http = app.CreateBackendHttpClient();
            var characterId = app.Services.GetRequiredService<BackendRuntime>().Characters.First().Id;
            await app.Services.GetRequiredService<MemoryDatabase>().EnsureConversationAsync("room", characterId, token);
            var path = $"/api/v1/characters/{characterId}/proactive-policy";
            using var initial = await http.GetAsync(path, token);
            Assert.Equal(HttpStatusCode.OK, initial.StatusCode);
            Assert.True((await initial.Content.ReadFromJsonAsync<ProactivePolicyResponse>(token))!.DeliveryAvailable);
            var request = new SaveProactivePolicyRequest(0, "room");
            using var saved = await http.PutAsJsonAsync(path, request, token);
            Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
            using var conflict = await http.PutAsJsonAsync(path, request, token);
            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
            Assert.Contains("revision_conflict", await conflict.Content.ReadAsStringAsync(token));
            using var unavailable = await http.PutAsJsonAsync(path, request with { ExpectedRevision = 1, Enabled = true }, token);
            Assert.Equal(HttpStatusCode.OK, unavailable.StatusCode);
            using var status = await http.GetAsync($"/api/v1/characters/{characterId}/proactive-status", token);
            Assert.Equal(HttpStatusCode.OK, status.StatusCode);
            Assert.Equal("activity_stale", (await status.Content.ReadFromJsonAsync<ProactiveStatusResponse>(token))!.State);
            using var invalid = await http.PutAsJsonAsync(path, request with { DailyLimit = -1 }, token);
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            using var absent = await http.GetAsync("/api/v1/characters/missing/proactive-policy", token);
            Assert.Equal(HttpStatusCode.NotFound, absent.StatusCode);
            var activityPath = $"/api/v1/companion/activity/{Guid.NewGuid():N}";
            using var activity = await http.PutAsJsonAsync(activityPath, new ReportCompanionActivityRequest(1, "room", 0), token);
            Assert.Equal(HttpStatusCode.OK, activity.StatusCode);
            using var removed = await http.DeleteAsync(activityPath, token);
            Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
            Assert.Equal("not_loaded", app.Services.GetRequiredService<BackendRuntime>().Status.ModelState);
        }
        finally { await app.StopAsync(token); }
    }

    private sealed class TestClock : TimeProvider
    {
        private long _seconds;
        private DateTimeOffset _origin = DateTimeOffset.Parse("2026-09-14T12:00:00Z");
        public override long TimestampFrequency => 1;
        public override long GetTimestamp() => _seconds;
        public override DateTimeOffset GetUtcNow() => _origin.AddSeconds(_seconds);
        public void Advance(long seconds) => _seconds += seconds;
        public void JumpWallClock(TimeSpan value) => _origin += value;
    }

    private sealed class DatabaseFixture : IDisposable
    {
        public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), $"pry-proactive-test-{Guid.NewGuid():N}");
        public MemoryDatabase Database { get; }
        public DatabaseFixture() => Database = new(Path.Combine(DirectoryPath, "memory.db"));
        public void Dispose()
        {
            SqliteTestPools.Clear(Path.Combine(DirectoryPath, "memory.db"));
            if (Directory.Exists(DirectoryPath)) Directory.Delete(DirectoryPath, true);
        }
    }
}
