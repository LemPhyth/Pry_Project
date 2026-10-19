using Microsoft.Data.Sqlite;
using Pry.Core.Memory;
using Pry.Core.Models;
using Xunit;

namespace Pry.Core.Tests;

// MEM-006 Stage 1: deleting a chat record removes evidence, it does not retract a learned fact.
// The numbered cases below are the N-series acceptance list in docs/memory-message-deletion-separation.md.
public sealed class MemoryDeletionSeparationTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "pry-memory-detach-" + Guid.NewGuid().ToString("N"));
    private MemoryDatabase _db = null!;
    private SqliteMemoryRepository Repo => _db.LongTerm;
    private readonly MemoryScope _scope = new("instance:alice-pry", "pry");
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;
    private CancellationToken Token => TestContext.Current.CancellationToken;
    private readonly ConsolidationPolicy _policy = new(DelaySeconds: 0, LeaseSeconds: 10);
    private string Database => Path.Combine(_directory, "memory.db");

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_directory);
        _db = new(Database); await _db.InitializeAsync(Token);
        await Room("a"); await Room("b");
    }
    public ValueTask DisposeAsync()
    {
        SqliteTestPools.Clear(Database); Directory.Delete(_directory, true); return ValueTask.CompletedTask;
    }

    private async Task Room(string id)
    {
        await _db.EnsureConversationAsync(id, _scope.CharacterId, Token); await Repo.BindConversationAsync(_scope, id, Token);
    }
    // Learns one explicit fact and returns the user message that evidenced it.
    private async Task<long> Learn(string text, string conversation = "a")
    {
        var input = await _db.AddMessageAsync(conversation, ChatRole.User, text, null, Token);
        var output = await _db.AddMessageAsync(conversation, ChatRole.Assistant, "收到。", null, Token);
        var turn = new CompletedMemoryTurn(_scope, conversation, "turn:" + input, [input], [output]);
        Assert.True((await Repo.EnqueueAsync(turn, _policy, _now, Token)).Accepted);
        var job = Assert.IsType<MemoryJob>(await Repo.ClaimAsync(_policy, _now, Token));
        Assert.True(await Repo.CompleteAsync(job, await new ExplicitFactExtractor().ExtractAsync(job, Token), _now, Token));
        return input;
    }
    private Task<IReadOnlyList<MemoryEntry>> Rows() => Repo.ListAsync(_scope, Token);
    private Task<MemoryContext> Query(string text, IMemorySemanticSignal? semantic = null) =>
        new MemoryQueryService(Repo, semantic).RetrieveAsync(new(_scope, "b", text, Now: _now), Token);
    private async Task<long> Number(string sql)
    {
        await using var c = new SqliteConnection($"Data Source={Database}");
        await c.OpenAsync(Token); await using var cmd = c.CreateCommand(); cmd.CommandText = sql;
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(Token) ?? 0L);
    }
    private async Task<string> Text(string sql)
    {
        await using var c = new SqliteConnection($"Data Source={Database}");
        await c.OpenAsync(Token); await using var cmd = c.CreateCommand(); cmd.CommandText = sql;
        return (string)(await cmd.ExecuteScalarAsync(Token) ?? "");
    }
    private Task<long> Tombstones() => Number("SELECT COUNT(*) FROM memory_tombstones");
    private Task<long> Watermark() => Number($"SELECT forget_before FROM memory_scopes WHERE scope_key='{_scope.Key}'");
    private static (long Message, string Conversation, string Excerpt) Triple(MemoryEvidence x) =>
        (x.MessageId, x.ConversationId, x.Excerpt);

    [Fact]
    public async Task N1_Deleting_the_only_evidence_message_orphans_the_memory_and_writes_no_tombstone()
    {
        var input = await Learn("我喜欢后摇");
        var revision = await Repo.RevisionAsync(_scope, Token);
        var snapshot = await _db.DeleteMessageAsync("a", input, Token);
        Assert.NotNull(snapshot);
        var row = Assert.Single(await Rows());
        Assert.Equal("orphaned", row.Status);
        Assert.Equal(.4, row.Confidence, 5);
        Assert.Null(row.Record.SourceMessageId);
        Assert.Empty(row.Evidence);
        // The excerpt only survives inside the in-memory undo snapshot, never in a table.
        Assert.Equal("我喜欢后摇", Assert.Single(Assert.Single(snapshot.DetachedMemories).Evidence).Excerpt);
        // I2: the orphan is invisible to retrieval even though the row still exists.
        Assert.Empty((await Query("后摇")).Hits);
        Assert.Single(await Rows());
        Assert.Equal(0, await Tombstones());
        Assert.Equal(0, await Watermark());
        Assert.Equal(revision + 1, await Repo.RevisionAsync(_scope, Token));
    }

    [Fact]
    public async Task N2_Restating_an_orphaned_fact_is_learnable_again_and_not_permanently_poisoned()
    {
        var input = await Learn("我喜欢后摇");
        await _db.DeleteMessageAsync("a", input, Token);
        await Learn("我喜欢后摇", "b");
        var rows = await Rows();
        Assert.Equal(2, rows.Count);
        Assert.Equal("用户喜欢后摇", Assert.Single(rows, x => x.Status == "active").Record.Summary);
        Assert.Single(rows, x => x.Status == "orphaned");
        Assert.Equal("用户喜欢后摇", Assert.Single((await Query("后摇")).Hits).Memory.Record.Summary);
        Assert.Equal(0, await Tombstones());
    }

    [Fact]
    public async Task N3_Deleting_one_of_three_supporting_messages_recalibrates_confidence_from_two()
    {
        await Learn("我喜欢后摇");
        await Learn("我喜欢后摇", "b");
        var third = await Learn("我喜欢后摇");
        Assert.Equal(.96, Assert.Single(await Rows()).Confidence, 5);
        await _db.DeleteMessageAsync("a", third, Token);
        var row = Assert.Single(await Rows());
        Assert.Equal("active", row.Status);
        Assert.Equal(.93, row.Confidence, 5);
        Assert.Equal(2, row.Evidence.Count);
        Assert.DoesNotContain(third, row.Evidence.Select(x => x.MessageId));
    }

    [Fact]
    public async Task N4_Deleting_the_primary_source_while_a_supporting_message_survives_keeps_the_memory_active()
    {
        var first = await Learn("我喜欢后摇");
        var second = await Learn("我喜欢后摇", "b");
        await _db.DeleteMessageAsync("a", first, Token);
        var row = Assert.Single(await Rows());
        Assert.Equal("active", row.Status);
        Assert.Equal(.9, row.Confidence, 5);
        Assert.Null(row.Record.SourceMessageId);
        Assert.Equal(second, Assert.Single(row.Evidence).MessageId);
        Assert.NotEmpty((await Query("后摇")).Hits);
    }

    [Fact]
    public async Task N5_Undo_restores_every_detached_field_and_the_exact_evidence_triples()
    {
        var first = await Learn("我喜欢后摇");
        var second = await Learn("我喜欢后摇", "b");
        var before = Assert.Single(await Rows());
        var snapshot = await _db.DeleteMessageAsync("a", first, Token);
        await _db.RestoreConversationMutationAsync(snapshot!, Token);
        var after = Assert.Single(await Rows());
        Assert.Equal(before.Status, after.Status);
        Assert.Equal(before.Confidence, after.Confidence, 5);
        Assert.Equal(first, after.Record.SourceMessageId);
        Assert.Equal(before.Record.Kind, after.Record.Kind);
        Assert.Equal(before.Record.Summary, after.Record.Summary);
        Assert.Equal(before.Record.Tags, after.Record.Tags);
        Assert.Equal(before.Record.Importance, after.Record.Importance, 5);
        Assert.Equal(before.Record.CreatedAt, after.Record.CreatedAt);
        Assert.Equal(before.Record.UpdatedAt, after.Record.UpdatedAt);
        Assert.Equal(before.Record.LastUsedAt, after.Record.LastUsedAt);
        // The row must be scope-visible: a restore that clears scope_key makes the memory unreachable.
        Assert.Equal(before.ScopeKey, after.ScopeKey);
        Assert.Equal(before.Evidence.Select(Triple).OrderBy(x => x.Message), after.Evidence.Select(Triple).OrderBy(x => x.Message));
        Assert.Equal(new[] { first, second }, after.Evidence.Select(x => x.MessageId).ToArray());
        // One version bump per detach and per restore keeps the optimistic concurrency contract honest.
        Assert.Equal(before.Revision + 2, after.Revision);
        Assert.Equal(0, await Tombstones());
        Assert.NotNull(Assert.Single(await _db.GetRecentMessagesAsync("a", 10, Token), x => x.Id == first));
    }

    [Fact]
    public async Task N6_Undo_cannot_resurrect_a_memory_the_user_deleted_explicitly()
    {
        var input = await Learn("我喜欢后摇");
        var id = Assert.Single(await Rows()).Record.Id;
        await Repo.DeleteAsync(_scope, id, Token);
        Assert.Empty(await Rows());
        var snapshot = await _db.DeleteMessageAsync("a", input, Token);
        Assert.Empty(snapshot!.DetachedMemories);
        await _db.RestoreConversationMutationAsync(snapshot, Token);
        Assert.Equal(0, await Number($"SELECT COUNT(*) FROM memories WHERE scope_key='{_scope.Key}'"));
        Assert.Equal(1, await Tombstones());
        Assert.Empty((await Query("后摇")).Hits);
    }

    [Fact]
    public async Task N7_Deleting_a_conversation_orphans_only_the_facts_it_taught()
    {
        await Learn("我喜欢后摇");
        await Learn("我住在上海", "b");
        await _db.DeleteConversationAsync("a", Token);
        var rows = await Rows();
        Assert.Equal("用户喜欢后摇", Assert.Single(rows, x => x.Status == "orphaned").Record.Summary);
        Assert.Equal("用户住在上海", Assert.Single(rows, x => x.Status == "active").Record.Summary);
        Assert.Equal(0, await Tombstones());
        Assert.Equal(0, await Watermark());
        Assert.Equal("用户住在上海", Assert.Single((await Query("上海")).Hits).Memory.Record.Summary);
    }

    [Fact]
    public async Task N9_Orphaned_memories_never_reach_the_semantic_scorer()
    {
        var input = await Learn("我喜欢后摇");
        var id = Assert.Single(await Rows()).Record.Id;
        var scorer = new RecordingSemantic();
        await Query("后摇", scorer);
        Assert.Contains(id, scorer.Seen);
        await _db.DeleteMessageAsync("a", input, Token);
        scorer.Seen.Clear();
        await Query("后摇", scorer);
        Assert.DoesNotContain(id, scorer.Seen);
        Assert.Empty(scorer.Seen);
    }

    [Fact]
    public async Task N10_Deleting_a_message_abandons_the_job_that_needed_it_without_forgetting_history()
    {
        var input = await _db.AddMessageAsync("a", ChatRole.User, "我喜欢后摇", null, Token);
        var output = await _db.AddMessageAsync("a", ChatRole.Assistant, "收到。", null, Token);
        var turn = new CompletedMemoryTurn(_scope, "a", "turn:queued", [input], [output]);
        Assert.True((await Repo.EnqueueAsync(turn, _policy, _now, Token)).Accepted);
        var revision = await Repo.RevisionAsync(_scope, Token);
        var snapshot = await _db.DeleteMessageAsync("a", input, Token);
        Assert.NotNull(snapshot);
        Assert.Empty(snapshot.DetachedMemories);
        Assert.Equal("abandoned/evidence_detached", await Text("SELECT state || '/' || reason FROM memory_candidates WHERE conversation_id='a'"));
        Assert.Equal("", await Text("SELECT payload FROM memory_candidates WHERE conversation_id='a'"));
        // Only rows that actually changed advance the epoch, so unrelated queued facts are not fenced away.
        Assert.Equal(revision, await Repo.RevisionAsync(_scope, Token));
        Assert.Equal(0, await Watermark());
        await _db.RestoreConversationMutationAsync(snapshot, Token);
        await Learn("我喜欢后摇", "b");
        Assert.Equal("用户喜欢后摇", Assert.Single((await Query("后摇")).Hits).Memory.Record.Summary);
    }

    [Fact]
    public async Task N6b_Replaying_an_undo_after_an_explicit_memory_deletion_neither_resurrects_nor_throws()
    {
        var input = await Learn("我喜欢后摇");
        var snapshot = await _db.DeleteMessageAsync("a", input, Token);
        await Repo.DeleteAsync(_scope, Assert.Single(await Rows()).Record.Id, Token);
        await _db.RestoreConversationMutationAsync(snapshot!, Token);
        await _db.RestoreConversationMutationAsync(snapshot!, Token);
        Assert.Equal(0, await Number($"SELECT COUNT(*) FROM memories WHERE scope_key='{_scope.Key}'"));
        Assert.Equal(0, await Number("SELECT COUNT(*) FROM memory_evidence"));
        Assert.Equal(1, await Tombstones());
    }

    [Fact]
    public async Task N5b_Undo_does_not_overwrite_a_memory_that_a_later_delete_re_orphaned()
    {
        var first = await Learn("我喜欢后摇");
        var other = await Learn("我喜欢后摇", "b");
        var snapshot = await _db.DeleteMessageAsync("a", first, Token);
        Assert.Equal("active", Assert.Single(await Rows()).Status);
        // A newer turn adds support after the snapshot was taken; the undo discards that message, so the
        // row is rewritten twice and the restore must leave the newer recalculation alone.
        var later = await Learn("我喜欢后摇");
        await _db.RestoreConversationMutationAsync(snapshot!, Token);
        var row = Assert.Single(await Rows());
        Assert.Equal("active", row.Status);
        Assert.Equal(.9, row.Confidence, 5);
        Assert.Equal(3, row.Revision);
        Assert.Equal(new[] { first, other }, row.Evidence.Select(x => x.MessageId).ToArray());
        Assert.DoesNotContain(row.Evidence, x => x.MessageId == later);
    }

    [Fact]
    public async Task N5c_Detaching_leaves_superseded_and_archived_states_untouched()
    {
        var original = await Learn("我住在上海");
        await Learn("更正，我住在杭州", "b");
        var rows = await Rows();
        var superseded = Assert.Single(rows, x => x.Status == "superseded");
        await _db.DeleteMessageAsync("a", original, Token);
        var after = Assert.Single(await Rows(), x => x.Record.Id == superseded.Record.Id);
        Assert.Equal("superseded", after.Status);
        Assert.Equal(superseded.Confidence, after.Confidence, 5);
        Assert.Equal(superseded.Revision, after.Revision);
        Assert.Equal("用户住在杭州", Assert.Single((await Query("杭州")).Hits).Memory.Record.Summary);
    }

    [Fact]
    public async Task N12_Component_version_two_is_recorded_and_an_unknown_version_refuses_startup()
    {
        Assert.Equal(2, await Number("SELECT MAX(version) FROM memory_schema"));
        await Run("UPDATE memory_schema SET version=3");
        await Assert.ThrowsAsync<InvalidOperationException>(() => new SqliteMemoryRepository(Database).InitializeAsync(Token));
        // A version-2 database may already hold orphaned rows, so it must start without migrating from 1.
        await Run("UPDATE memory_schema SET version=2");
        await new SqliteMemoryRepository(Database).InitializeAsync(Token);
        Assert.Equal(2, await Number("SELECT MAX(version) FROM memory_schema"));
        await Run("UPDATE memory_schema SET version=1");
        await new SqliteMemoryRepository(Database).InitializeAsync(Token);
        Assert.Equal(2, await Number("SELECT MAX(version) FROM memory_schema"));
        await Run("UPDATE memory_schema SET version=0");
        await Assert.ThrowsAsync<InvalidOperationException>(() => new SqliteMemoryRepository(Database).InitializeAsync(Token));
    }

    private async Task Run(string sql)
    {
        await using var c = new SqliteConnection($"Data Source={Database}");
        await c.OpenAsync(Token); await using var cmd = c.CreateCommand(); cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(Token);
    }

    // ---- 2-C confirm-keep: a user decision re-admits an orphaned fact, it does not re-prove it. ----

    [Fact]
    public async Task ConfirmKeep_promotes_orphan_to_recallable_manual_fact_without_rebuilding_evidence()
    {
        var input = await Learn("我喜欢后摇");
        await _db.DeleteMessageAsync("a", input, Token);
        var orphan = Assert.Single(await Rows());
        Assert.Equal("orphaned", orphan.Status);
        var evidenceBefore = await Number($"SELECT COUNT(*) FROM memory_evidence WHERE memory_id={orphan.Record.Id}");

        var result = await Repo.ConfirmKeepAsync(_scope, orphan.Record.Id, Token);
        Assert.Equal(ConfirmKeepOutcome.Confirmed, result.Outcome);
        var kept = result.Entry!;
        Assert.Equal("active", kept.Status);
        Assert.Equal(.7, kept.Confidence, 5);          // recall ceiling, above the .5 gate but under proven 1.0
        Assert.Equal("manual", kept.Origin);           // the user owns this now, not a freshly extracted proof
        Assert.Empty(kept.Evidence);                   // no evidence rows are fabricated back
        Assert.Equal(evidenceBefore, await Number($"SELECT COUNT(*) FROM memory_evidence WHERE memory_id={orphan.Record.Id}"));
        Assert.Equal(0, await Tombstones());           // confirming is not a delete
        Assert.Equal(0, await Watermark());            // and it must not raise the forget watermark
        Assert.NotEmpty((await Query("后摇")).Hits);    // immediately recallable, no restart
    }

    [Fact]
    public async Task ConfirmKeep_advances_the_epoch_so_cached_context_invalidates()
    {
        var input = await Learn("我喜欢后摇");
        await _db.DeleteMessageAsync("a", input, Token);
        var id = Assert.Single(await Rows()).Record.Id;
        var revision = await Repo.RevisionAsync(_scope, Token);
        var result = await Repo.ConfirmKeepAsync(_scope, id, Token);
        Assert.Equal(revision + 1, await Repo.RevisionAsync(_scope, Token));
        Assert.Equal(.7, result.Entry!.Confidence, 5);      // the ceiling is a constant, not a recalibrated value
    }

    [Fact]
    public async Task ConfirmKeep_rejects_a_row_that_is_not_orphaned()
    {
        await Learn("我住在上海", "b");
        var activeId = Assert.Single(await Rows(), x => x.Status == "active").Record.Id;
        Assert.Equal(ConfirmKeepOutcome.NotOrphaned, (await Repo.ConfirmKeepAsync(_scope, activeId, Token)).Outcome);
        // A rejected confirm writes nothing: the active row keeps its status and the epoch stays put.
        Assert.Equal("active", Assert.Single(await Rows(), x => x.Record.Id == activeId).Status);
    }

    [Fact]
    public async Task ConfirmKeep_reports_fingerprint_taken_when_an_active_twin_already_owns_the_slot()
    {
        var first = await Learn("我喜欢后摇");
        await _db.DeleteMessageAsync("a", first, Token);   // row A -> orphaned
        await Learn("我喜欢后摇", "b");                     // restated -> new active row B, same fingerprint
        var rows = await Rows();
        var orphan = Assert.Single(rows, x => x.Status == "orphaned");
        var activeTwin = Assert.Single(rows, x => x.Status == "active");
        Assert.Equal(ConfirmKeepOutcome.FingerprintTaken, (await Repo.ConfirmKeepAsync(_scope, orphan.Record.Id, Token)).Outcome);
        // The collision must not silently merge, and the orphan stays an orphan.
        Assert.Equal("orphaned", Assert.Single(await Rows(), x => x.Record.Id == orphan.Record.Id).Status);
        Assert.Equal("active", Assert.Single(await Rows(), x => x.Record.Id == activeTwin.Record.Id).Status);
    }

    [Fact]
    public async Task ConfirmKeep_cannot_resurrect_an_explicitly_deleted_memory()
    {
        var input = await Learn("我喜欢后摇");
        var id = Assert.Single(await Rows()).Record.Id;
        await _db.DeleteMessageAsync("a", input, Token);
        await Repo.DeleteAsync(_scope, id, Token);          // explicit D1 delete writes a tombstone
        Assert.Equal(ConfirmKeepOutcome.NotFound, (await Repo.ConfirmKeepAsync(_scope, id, Token)).Outcome);
        Assert.Equal(1, await Tombstones());
        Assert.Empty((await Query("后摇")).Hits);
    }

    [Fact]
    public async Task ConfirmKeep_and_the_management_read_path_reach_instance_scoped_orphans()
    {
        // This fixture's scope is instance:alice-pry, so reaching and confirming here is the N11 proof:
        // the legacy 'character:'||id SQL would never see it, but the repository-scoped path does.
        var input = await Learn("我喜欢后摇");
        await _db.DeleteMessageAsync("a", input, Token);
        var orphan = Assert.Single(await Repo.ListAsync(_scope, Token), x => x.Status == "orphaned");
        Assert.StartsWith("instance:", orphan.ScopeKey, StringComparison.Ordinal);
        Assert.Equal(ConfirmKeepOutcome.Confirmed, (await Repo.ConfirmKeepAsync(_scope, orphan.Record.Id, Token)).Outcome);
    }

    [Fact]
    public async Task N8_Cascading_a_conversation_delete_forgets_only_the_facts_it_alone_taught()
    {
        await Learn("我喜欢后摇", "a");        // taught exclusively in a
        await Learn("我住在上海", "a");
        await Learn("我住在上海", "b");        // the same fact also taught in b, so a alone cannot retract it
        await _db.DeleteConversationAsync("a", Token, forgetExclusiveMemories: true);
        var rows = await Rows();
        Assert.DoesNotContain(rows, x => x.Record.Summary == "用户喜欢后摇");
        var shanghai = Assert.Single(rows, x => x.Record.Summary == "用户住在上海");
        Assert.Equal("active", shanghai.Status);
        Assert.Equal("b", Assert.Single(shanghai.Evidence.Select(x => x.ConversationId).Distinct()));
        // Only the retracted fact writes a tombstone; the cross-session survivor is untouched.
        Assert.Equal(1, await Tombstones());
        Assert.Equal("用户住在上海", Assert.Single((await Query("上海")).Hits).Memory.Record.Summary);
    }

    [Fact]
    public async Task N8_A_cascaded_fact_cannot_be_learned_back_the_way_an_orphan_can()
    {
        await Learn("我喜欢后摇", "a");
        await _db.DeleteConversationAsync("a", Token, forgetExclusiveMemories: true);
        Assert.Empty(await Rows());
        // Contrast N2: restating an orphan re-learns it. A cascade-retracted fact is a D1 delete, so it stays gone.
        await Learn("我喜欢后摇", "b");
        Assert.DoesNotContain(await Rows(), x => x.Status == "active" && x.Record.Summary == "用户喜欢后摇");
        Assert.Empty((await Query("后摇")).Hits);
        Assert.Equal(1, await Tombstones());
    }

    [Fact]
    public async Task N8_Leaving_the_cascade_off_reproduces_the_N7_orphan_shape_exactly()
    {
        await Learn("我喜欢后摇", "a");
        await _db.DeleteConversationAsync("a", Token); // default: no cascade
        var row = Assert.Single(await Rows());
        Assert.Equal("orphaned", row.Status);
        Assert.Equal(.4, row.Confidence, 5);
        Assert.Null(row.Record.SourceMessageId);
        Assert.Empty(row.Evidence);
        Assert.Equal(0, await Tombstones());
        Assert.Equal(0, await Watermark());
        Assert.Empty((await Query("后摇")).Hits);
    }

    [Fact]
    public async Task N8_Deleting_a_message_never_cascades_even_though_a_conversation_can()
    {
        // The cascade is a whole-conversation ruling. Deleting one record must never write a tombstone,
        // no matter how the argument is shaped, so a fact supported only by that one message still orphans.
        var input = await Learn("我喜欢后摇");
        var snapshot = await _db.DeleteMessageAsync("a", input, Token);
        Assert.NotNull(snapshot);
        Assert.Single(await Rows(), x => x.Status == "orphaned");
        Assert.Equal(0, await Tombstones());
    }


    private sealed class RecordingSemantic : IMemorySemanticSignal
    {
        public List<long> Seen { get; } = [];
        public Task<IReadOnlyDictionary<long, double>> ScoreAsync(MemoryScope scope, string query,
            IReadOnlyList<MemoryEntry> current, CancellationToken token = default)
        {
            Seen.AddRange(current.Select(x => x.Record.Id));
            return Task.FromResult<IReadOnlyDictionary<long, double>>(new Dictionary<long, double>());
        }
    }
}
