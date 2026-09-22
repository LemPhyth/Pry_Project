using Microsoft.Data.Sqlite;
using Pry.Core.Memory;
using Pry.Core.Models;
using Xunit;

namespace Pry.Core.Tests;

public sealed class ProactiveDeliveryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Runtime_permission_and_proactive_quota_commit_in_one_transaction(bool stale)
    {
        using var f = new Fixture(); var ct = TestContext.Current.CancellationToken;
        var stored = await f.InitializeAsync(ct);
        var attempt = (await f.Database.BeginProactiveAttemptAsync(stored, f.Now, ct))!;
        var epoch = await f.Database.BumpRuntimeEpochAsync("room", ct);
        var messages = new[] { new Pry.Core.Runtime.RuntimePlannedMessage(0, "hello", null) };
        var permission = await f.Database.RequestRuntimeDeliveryAsync("room", new(1, 1, epoch), epoch, 1,
            MessageSources.Proactive, attempt.Id, messages, ct);
        if (stale) await f.Database.BumpRuntimeEpochAsync("room", ct);
        var id = await f.Database.CompleteProactiveAttemptAsync(attempt, "hello", f.Now, ct, permission);
        var history = await f.Database.GetProactiveHistoryAsync(stored.Policy, f.Now, ct);
        Assert.Equal(stale ? 0 : 1, history.SentToday);
        Assert.Equal(stale, id is null);
        Assert.Equal(stale ? 0 : 1, (await f.Database.GetRecentMessagesAsync("room", 10, ct)).Count);
        Assert.Null(await f.Database.CompleteProactiveAttemptAsync(attempt, "duplicate", f.Now, ct, permission));
    }

    [Fact]
    public async Task Delivery_is_atomic_deduplicated_and_quota_survives_message_deletion_and_restart()
    {
        using var f = new Fixture(); var ct = TestContext.Current.CancellationToken;
        var stored = await f.InitializeAsync(ct);
        var attempts = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
            f.Database.BeginProactiveAttemptAsync(stored, f.Now, ct), ct)));
        var attempt = Assert.Single(attempts, x => x is not null)!;
        var id = await f.Database.CompleteProactiveAttemptAsync(attempt, "想聊聊今天的趣事吗？", f.Now, ct);
        Assert.NotNull(id);
        Assert.Null(await f.Database.CompleteProactiveAttemptAsync(attempt, "重复", f.Now, ct));
        var message = Assert.Single(await f.Database.GetRecentMessagesAsync("room", 10, ct));
        Assert.Equal(MessageSources.Proactive, message.Source);
        Assert.Equal(ChatRole.Assistant, message.Role);
        Assert.Empty(await f.Database.SearchMemoriesAsync("character", "", 6, ct));
        await f.Database.DeleteMessageAsync("room", id.Value, ct);
        var restarted = new MemoryDatabase(f.Path);
        await restarted.InitializeAsync(ct); await restarted.RecoverProactiveAttemptsAsync(ct);
        var history = await restarted.GetProactiveHistoryAsync(stored.Policy, f.Now, ct);
        Assert.Equal(1, history.SentToday); Assert.True(history.AwaitingUserReply);
        Assert.Null(await restarted.BeginProactiveAttemptAsync(stored, f.Now.AddHours(3), ct));
        await restarted.AddMessageAsync("room", ChatRole.User, "可以啊", null, ct);
        Assert.False((await restarted.GetProactiveHistoryAsync(stored.Policy, f.Now, ct)).AwaitingUserReply);
        Assert.Null(await restarted.BeginProactiveAttemptAsync(stored, f.Now.AddHours(3), ct)); // daily limit=1
        Assert.NotNull(await restarted.BeginProactiveAttemptAsync(stored, f.Now.AddDays(1), ct));
    }

    [Fact]
    public async Task Failed_transaction_has_no_ghost_message_and_interrupted_claim_keeps_cooldown()
    {
        using var f = new Fixture(); var ct = TestContext.Current.CancellationToken;
        var stored = await f.InitializeAsync(ct);
        var attempt = (await f.Database.BeginProactiveAttemptAsync(stored, f.Now, ct))!;
        await using (var connection = new SqliteConnection($"Data Source={f.Path}"))
        {
            await connection.OpenAsync(ct);
            await using var trigger = connection.CreateCommand();
            trigger.CommandText = "CREATE TRIGGER fail_receipt BEFORE INSERT ON proactive_deliveries BEGIN SELECT RAISE(ABORT,'test failure'); END;";
            await trigger.ExecuteNonQueryAsync(ct);
            await Assert.ThrowsAsync<SqliteException>(() => f.Database.CompleteProactiveAttemptAsync(attempt, "你好", f.Now, ct));
        }
        Assert.Empty(await f.Database.GetRecentMessagesAsync("room", 10, ct));
        await f.Database.RecoverProactiveAttemptsAsync(ct);
        var history = await f.Database.GetProactiveHistoryAsync(stored.Policy, f.Now, ct);
        Assert.Equal("interrupted", history.LastOutcome); Assert.Equal(0, history.SentToday);
        Assert.Equal(f.Now.AddHours(2), history.NextAttemptAt);
        Assert.Null(await f.Database.BeginProactiveAttemptAsync(stored, f.Now.AddMinutes(1), ct));
        Assert.Null(await f.Database.CompleteProactiveAttemptAsync(attempt, "不能补发", f.Now, ct));
    }

    [Theory]
    [InlineData("policy")]
    [InlineData("message")]
    [InlineData("conversation")]
    public async Task Final_transaction_rejects_changed_policy_history_or_target(string change)
    {
        using var f = new Fixture(); var ct = TestContext.Current.CancellationToken;
        var stored = await f.InitializeAsync(ct);
        var attempt = (await f.Database.BeginProactiveAttemptAsync(stored, f.Now, ct))!;
        if (change == "policy") await f.Database.SaveProactivePolicyAsync(stored.Policy with { Enabled = false }, 1, ct);
        if (change == "message") await f.Database.AddMessageAsync("room", ChatRole.User, "我回来了", null, ct);
        if (change == "conversation") await f.Database.DeleteConversationAsync("room", ct);
        Assert.Null(await f.Database.CompleteProactiveAttemptAsync(attempt, "不应发送", f.Now, ct));
        await f.Database.EndProactiveAttemptAsync(attempt, "superseded", f.Now.AddMinutes(1), ct);
        var history = await f.Database.GetProactiveHistoryAsync(stored.Policy, f.Now, ct);
        Assert.Equal(0, history.SentToday);
        Assert.Equal(f.Now.AddHours(2).AddMinutes(1), history.NextAttemptAt);
    }

    private sealed class Fixture : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"pry-delivery-{Guid.NewGuid():N}", "memory.db");
        public MemoryDatabase Database { get; }
        public DateTimeOffset Now { get; } = DateTimeOffset.Parse("2026-09-14T12:00:00Z");
        public Fixture() => Database = new(Path);
        public async Task<StoredProactivePolicy> InitializeAsync(CancellationToken ct)
        {
            await Database.InitializeAsync(ct);
            await Database.EnsureConversationAsync("room", "character", ct);
            return (await Database.SaveProactivePolicyAsync(new ProactivePolicy("character", "room", true,
                DailyLimit: 1, TimeZoneId: "UTC", QuietHoursEnabled: false), 0, ct))!;
        }
        public void Dispose() { SqliteConnection.ClearAllPools(); Directory.Delete(System.IO.Path.GetDirectoryName(Path)!, true); }
    }
}
