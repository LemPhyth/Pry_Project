using Microsoft.Data.Sqlite;
using Pry.Core.Memory;
using Pry.Core.Models;
using Pry.Core.Runtime;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>Fixed EVAL scenarios against real SQLite; no model or user data is involved.</summary>
[Trait("Suite", "FixedEval")]
public sealed class EvaluationPersistenceTests
{
    [Fact]
    [Trait("Scenario", "REC-003")]
    public async Task Epoch_only_state_can_be_loaded_after_interrupted_first_turn()
    {
        using var fixture = new Fixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.InitializeAsync(ct);
        var epoch = await fixture.Database.BumpRuntimeEpochAsync("room", ct);

        // A process may stop between epoch invalidation and saving its first full snapshot.
        var reopened = new MemoryDatabase(fixture.Path);
        await reopened.InitializeAsync(ct);
        var state = await reopened.LoadRuntimeStateAsync("room", ct);
        Assert.NotNull(state);
        Assert.Equal(epoch, state.Epoch);
        Assert.False(state.LastSpeakerWasCharacter);
        Assert.Equal(0, state.ConsecutiveCharacterTurns);
    }

    [Fact]
    [Trait("Scenario", "PRO-003")]
    public async Task Later_bubble_cannot_commit_before_the_first_bubble()
    {
        using var fixture = new Fixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.InitializeAsync(ct);
        var permission = await fixture.PlanAsync("room", ct);

        Assert.Null(await fixture.Database.CommitRuntimeBubbleAsync(permission, 1, Fixture.Second, ct));
        Assert.Empty(await fixture.Database.GetRecentMessagesAsync("room", 10, ct));
        Assert.NotNull(await fixture.Database.CommitRuntimeBubbleAsync(permission, 0, Fixture.First, ct));
        Assert.NotNull(await fixture.Database.CommitRuntimeBubbleAsync(permission, 1, Fixture.Second, ct));
        Assert.Equal(2, (await fixture.Database.GetRecentMessagesAsync("room", 10, ct)).Count);
    }

    [Fact]
    [Trait("Scenario", "PRO-004")]
    public async Task Duplicate_bubble_commit_does_not_duplicate_visible_history()
    {
        using var fixture = new Fixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.InitializeAsync(ct);
        var permission = await fixture.PlanAsync("room", ct);
        Assert.NotNull(await fixture.Database.CommitRuntimeBubbleAsync(permission, 0, Fixture.First, ct));
        Assert.Null(await fixture.Database.CommitRuntimeBubbleAsync(permission, 0, Fixture.First, ct));
        Assert.Single(await fixture.Database.GetRecentMessagesAsync("room", 10, ct));
    }

    [Fact]
    [Trait("Scenario", "TIM-006")]
    public async Task Epoch_change_blocks_old_output_without_cancelling_another_conversation()
    {
        using var fixture = new Fixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.InitializeAsync(ct);
        await fixture.Database.EnsureConversationAsync("other", "character", ct);
        var old = await fixture.PlanAsync("room", ct);
        var other = await fixture.PlanAsync("other", ct);
        await fixture.Database.BumpRuntimeEpochAsync("room", ct);

        Assert.Null(await fixture.Database.CommitRuntimeBubbleAsync(old, 0, Fixture.First, ct));
        Assert.NotNull(await fixture.Database.CommitRuntimeBubbleAsync(other, 0, Fixture.First, ct));
        Assert.Empty(await fixture.Database.GetRecentMessagesAsync("room", 10, ct));
        Assert.Single(await fixture.Database.GetRecentMessagesAsync("other", 10, ct));
    }

    [Fact]
    [Trait("Scenario", "REC-002")]
    public async Task Reopen_sweeps_pending_plan_preserves_sent_bubble_and_never_replays()
    {
        using var fixture = new Fixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.InitializeAsync(ct);
        var permission = await fixture.PlanAsync("room", ct);
        var sentId = await fixture.Database.CommitRuntimeBubbleAsync(permission, 0, Fixture.First, ct);
        Assert.NotNull(sentId);

        // Leave a real pending row without graceful Runtime disposal. This is a storage
        // recovery fixture, not evidence of an OS process-kill or power-loss test.
        var reopened = new MemoryDatabase(fixture.Path);
        await reopened.InitializeAsync(ct);
        Assert.Contains(permission.PlanId, await reopened.SweepStaleRuntimePlansAsync(ct));
        Assert.Empty(await reopened.SweepStaleRuntimePlansAsync(ct));
        Assert.Null(await reopened.CommitRuntimeBubbleAsync(permission, 1, Fixture.Second, ct));
        Assert.Equal(sentId.Value, Assert.Single(await reopened.GetRecentMessagesAsync("room", 10, ct)).Id);
        await fixture.CheckIntegrityAsync(ct);
    }

    [Fact]
    [Trait("Scenario", "PER-003")]
    public async Task Failed_message_insert_rolls_back_delivery_receipt_and_allows_one_retry()
    {
        using var fixture = new Fixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.InitializeAsync(ct);
        var permission = await fixture.PlanAsync("room", ct);
        await fixture.ExecuteAsync("""
            CREATE TRIGGER eval_fail_message BEFORE INSERT ON messages
            BEGIN SELECT RAISE(ABORT,'synthetic eval failure'); END;
            """, ct);
        await Assert.ThrowsAsync<SqliteException>(() =>
            fixture.Database.CommitRuntimeBubbleAsync(permission, 0, Fixture.First, ct));
        Assert.Empty(await fixture.Database.GetRecentMessagesAsync("room", 10, ct));
        await fixture.ExecuteAsync("DROP TRIGGER eval_fail_message;", ct);
        Assert.NotNull(await fixture.Database.CommitRuntimeBubbleAsync(permission, 0, Fixture.First, ct));
        Assert.Null(await fixture.Database.CommitRuntimeBubbleAsync(permission, 0, Fixture.First, ct));
        Assert.Single(await fixture.Database.GetRecentMessagesAsync("room", 10, ct));
        await fixture.CheckIntegrityAsync(ct);
    }

    [Fact]
    [Trait("Scenario", "PER-001")]
    public async Task Repeated_initialization_preserves_messages_memory_and_runtime_state()
    {
        using var fixture = new Fixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.InitializeAsync(ct);
        var messageId = await fixture.Database.AddMessageAsync("room", ChatRole.User, "合成偏好：喜欢红茶", null, ct);
        await fixture.Database.AddMemoryAsync("character", "preference", "合成偏好：喜欢红茶", "红茶", .8, messageId, ct);
        var snapshot = new RuntimePersistenceState(3, true, DateTimeOffset.Parse("2026-09-20T10:00:00Z"), 1);
        await fixture.Database.SaveRuntimeStateAsync("room", snapshot, ct);
        var reopened = new MemoryDatabase(fixture.Path);
        await reopened.InitializeAsync(ct);
        await reopened.InitializeAsync(ct);
        Assert.Equal(messageId, Assert.Single(await reopened.GetRecentMessagesAsync("room", 10, ct)).Id);
        Assert.Single(await reopened.SearchMemoriesAsync("character", "红茶", 10, ct));
        Assert.Equal(snapshot, await reopened.LoadRuntimeStateAsync("room", ct));
        await fixture.CheckIntegrityAsync(ct);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"pry-fixed-eval-{Guid.NewGuid():N}");
        public string Path => System.IO.Path.Combine(_directory, "memory.db");
        public MemoryDatabase Database { get; }
        public static RuntimePlannedMessage First => new(0, "合成第一条", null);
        public static RuntimePlannedMessage Second => new(1, "合成第二条", null);
        public Fixture() => Database = new MemoryDatabase(Path);

        public async Task InitializeAsync(CancellationToken ct)
        {
            await Database.InitializeAsync(ct);
            await Database.EnsureConversationAsync("room", "character", ct);
        }

        public async Task<DeliveryPermission> PlanAsync(string conversation, CancellationToken ct)
        {
            var epoch = await Database.ReadRuntimeEpochAsync(conversation, ct);
            await Database.SaveRuntimeStateAsync(conversation,
                new RuntimePersistenceState(epoch, false, DateTimeOffset.Parse("2026-09-20T10:00:00Z"), 0), ct);
            var permission = await Database.RequestRuntimeDeliveryAsync(conversation, new TurnIdentity(1, 1, epoch),
                epoch, 2, MessageSources.ModelReply, null, [First, Second], ct);
            Assert.True(permission.Approved);
            return permission;
        }

        public async Task ExecuteAsync(string sql, CancellationToken ct)
        {
            await using var connection = new SqliteConnection($"Data Source={Path};Pooling=False");
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(ct);
        }

        public async Task CheckIntegrityAsync(CancellationToken ct)
        {
            await using var connection = new SqliteConnection($"Data Source={Path};Pooling=False");
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";
            Assert.Equal("ok", await command.ExecuteScalarAsync(ct));
            command.CommandText = "PRAGMA foreign_key_check;";
            await using var reader = await command.ExecuteReaderAsync(ct);
            Assert.False(await reader.ReadAsync(ct));
        }

        public void Dispose()
        {
            // Clear only this fixture's pools; do not disturb concurrently running DB tests.
            // MemoryDatabase now opens the long-term repository's own connection string too, so the
            // three-key SqliteTestPools.Clear helper is required (a single ClearPool leaves memory.db locked).
            SqliteTestPools.Clear(Path);
            if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        }
    }
}
