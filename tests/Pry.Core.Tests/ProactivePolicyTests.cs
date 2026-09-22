using Microsoft.Data.Sqlite;
using Pry.Core.Memory;
using Pry.Core.Models;
using Xunit;

namespace Pry.Core.Tests;

public sealed class ProactivePolicyTests
{
    private static readonly ProactivePolicy Policy = new("character", "room", Enabled: true, TimeZoneId: "UTC");
    private static readonly DateTimeOffset Noon = DateTimeOffset.Parse("2026-09-14T12:00:00Z");

    [Theory]
    [InlineData(21, 59, "eligible")]
    [InlineData(22, 0, "quiet_hours")]
    [InlineData(0, 0, "quiet_hours")]
    [InlineData(7, 59, "quiet_hours")]
    [InlineData(8, 0, "eligible")]
    public void Quiet_hours_handle_midnight_and_exclusive_end(int hour, int minute, string reason)
    {
        var now = new DateTimeOffset(2026, 9, 14, hour, minute, 0, TimeSpan.Zero);
        Assert.Equal(reason, Evaluate(now: now).Reason);
    }

    [Fact]
    public void Rules_apply_activity_limits_and_wait_for_user_response()
    {
        Assert.Equal("disabled", Evaluate(Policy with { Enabled = false }).Reason);
        Assert.Equal("activity_stale", Evaluate(fresh: false).Reason);
        Assert.Equal("user_busy", Evaluate(busy: true).Reason);
        Assert.Equal("user_busy", Evaluate(turnBusy: true).Reason);
        Assert.Equal("target_unavailable", Evaluate(target: false).Reason);
        Assert.Equal("daily_limit", Evaluate(sent: 3).Reason);
        Assert.Equal("awaiting_user_reply", Evaluate(awaiting: true).Reason);
        Assert.Equal("minimum_interval", Evaluate(lastAttempt: Noon.AddSeconds(-7199)).Reason);
        Assert.True(Evaluate(lastAttempt: Noon.AddSeconds(-7200)).Eligible);
        Assert.Equal(Noon.AddSeconds(1), Evaluate(idle: 599).NextEligibleAt);
        Assert.True(Evaluate(idle: 600).Eligible);
        Assert.Equal("quiet_hours", Evaluate(Policy with { TimeZoneId = "Asia/Shanghai" },
            now: Noon.AddHours(2)).Reason);
        Assert.True(Evaluate(Policy with { QuietHoursEnabled = false }, now: Noon.AddHours(11)).Eligible);
        Assert.Equal("quiet_hours", Evaluate(Policy with { QuietStartMinute = 600, QuietEndMinute = 780 }).Reason);
    }

    [Fact]
    public async Task Policy_save_checks_revision_ownership_restart_and_target_deletion()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pry-policy-{Guid.NewGuid():N}", "memory.db");
        var token = TestContext.Current.CancellationToken;
        try
        {
            var database = new MemoryDatabase(path);
            await database.InitializeAsync(token);
            await database.InitializeAsync(token);
            await database.EnsureConversationAsync("room", "character", token);
            await database.EnsureConversationAsync("other", "different", token);
            var saved = await database.SaveProactivePolicyAsync(Policy, 0, token);
            Assert.Equal(1, saved!.Revision);
            Assert.Null(await database.SaveProactivePolicyAsync(Policy with { DailyLimit = 4 }, 0, token));
            var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
                Task.Run(() => database.SaveProactivePolicyAsync(Policy with { DailyLimit = 4 }, 1, token), token)));
            Assert.Single(results, x => x is not null);
            var restarted = new MemoryDatabase(path);
            await restarted.InitializeAsync(token);
            Assert.Equal(2, (await restarted.GetProactivePolicyAsync("character", token))!.Revision);
            await Assert.ThrowsAsync<ProactiveTargetMismatchException>(() => restarted.SaveProactivePolicyAsync(
                Policy with { ConversationId = "other" }, 2, token));
            Assert.Equal("room", (await restarted.GetProactivePolicyAsync("character", token))!.Policy.ConversationId);
            await using (var connection = new SqliteConnection($"Data Source={path}"))
            {
                await connection.OpenAsync(token);
                await using var failure = connection.CreateCommand();
                failure.CommandText = "CREATE TRIGGER reject_policy BEFORE UPDATE ON proactive_policies BEGIN SELECT RAISE(ABORT, 'test failure'); END;";
                await failure.ExecuteNonQueryAsync(token);
                await Assert.ThrowsAsync<SqliteException>(() => restarted.SaveProactivePolicyAsync(Policy, 2, token));
                Assert.Equal(2, (await restarted.GetProactivePolicyAsync("character", token))!.Revision);
            }
            await database.DeleteConversationAsync("room", token);
            Assert.Null(await restarted.GetProactivePolicyAsync("character", token));
        }
        finally { SqliteConnection.ClearAllPools(); Directory.Delete(Path.GetDirectoryName(path)!, true); }
    }

    private static ProactiveEligibility Evaluate(ProactivePolicy? policy = null, DateTimeOffset? now = null,
        bool fresh = true, bool busy = false, bool target = true, bool turnBusy = false,
        DateTimeOffset? lastAttempt = null, int sent = 0, bool awaiting = false, int idle = 600) =>
        ProactivePolicyRules.Evaluate(policy ?? Policy, now ?? Noon, fresh, busy, TimeSpan.FromSeconds(idle),
            target, turnBusy, lastAttempt, sent, awaiting);
}
