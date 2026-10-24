// HDS-006「记忆与删除全链」第一片·判据（工单 HDS-WAVE28-1024 §二）。
//
// 逐条对上 §一 的五条，每条至少一枚独立可判的格（下面的"枚"＝展开后的节点数，注释里刻意不写方括号属性名，
// 以免顶账时 grep 把注释行算成格）：
//   §一.1 三类身份不混      ＝ 1 枚 Fact ＋ 1 枚 Theory 展开 3 条区分性探针（real／fiction／cognitive 各一枚）
//   §一.2 证据来源可指认    ＝ 2 枚 Fact（缺失给明确原因、失效给明确原因，两者不共用沉默出口）
//   §一.3 统一检索一个入口  ＝ 3 枚 Fact（排序、截断逐条点名、以及"本原型没有 .5 地板"这条明写事实）
//   §一.4 派生物三处失效    ＝ 1 枚 Theory 展开 3 条探针 ＋ 2 枚 Fact（正文被红action＋原因点名、观察日志恰一条）
//   §一.5 撤销 CAS          ＝ 4 枚 Fact（状态版本拒、来源版本拒、恢复后旧条件写不进、失败不弹恢复记录）
//
// 断言只钉绝对字面量或"当场算出再与字面量比"的形状；没有"两个入口同工厂所以恒等"那种覆盖证据。
// 本文件不改任何既有判据、不改上游门、不用 Assert.Skip。
//
// 三句话（新链已接线／旧链可删／不进包已做到）在本文件与产品码里都不成立，也不被任何注释暗示。
using Pry.Core.Memory;
using Xunit;

namespace Pry.Core.Tests;

public sealed class Hds006EvidenceIdentityRetrievalPrototypeTests
{
    // §一.1 用同一句话挂三个身份；正文一律 ASCII，避免控制台编码把红证消息糊掉。
    private const string Sentence = "user-said-moving-to-suzhou";

    // ───────────────────────── §一.1 三类身份不混 ─────────────────────────

    [Fact]
    public void Same_sentence_under_the_three_identities_is_three_distinct_evidence_rows()
    {
        var (ledger, store) = SeededThreeIdentities();

        Assert.Equal(3, store.Rows.Count);
        Assert.Equal(Hds006EvidenceKind.Real, store.Find(11)!.Identity);
        Assert.Equal(Hds006EvidenceKind.Fiction, store.Find(12)!.Identity);
        Assert.Equal(Hds006EvidenceKind.Cognitive, store.Find(13)!.Identity);
        Assert.Equal(Sentence, store.Find(11)!.Content);
        Assert.Equal(Sentence, store.Find(12)!.Content);
        Assert.Equal(Sentence, store.Find(13)!.Content);
        Assert.Equal(3, store.Rows.Select(x => x.EvidenceId).Distinct().Count());
    }

    // 三条区分性探针：同一个正文、同一个来源，只有身份不同；每条探针钉自己的绝对期望标识，
    // 并另数"身份不匹配而被排除"的条数——不是"两个入口调同一个工厂所以恒等"的那种断言。
    [Theory]
    [InlineData(Hds006EvidenceKind.Real, 11)]
    [InlineData(Hds006EvidenceKind.Fiction, 12)]
    [InlineData(Hds006EvidenceKind.Cognitive, 13)]
    public void Retrieving_one_identity_never_carries_another(Hds006EvidenceKind asked, long expectedId)
    {
        var (ledger, store) = SeededThreeIdentities();

        var result = Retrieve(ledger, store, Sentence, asked, 5);

        var hit = Assert.Single(result.Hits);
        Assert.Equal(expectedId, hit.EvidenceId);
        Assert.Equal(asked, hit.Identity);
        Assert.Equal(1, hit.Rank);
        Assert.Equal(2, result.Exclusions.Count(x => x.Reason == Hds006ExclusionReason.IdentityMismatch));
        Assert.DoesNotContain(expectedId, result.Exclusions.Select(x => x.EvidenceId));
    }

    // ───────────────────────── §一.2 证据来源可指认 ─────────────────────────

    [Fact]
    public void Evidence_whose_source_was_never_registered_is_reported_as_source_missing_not_dropped()
    {
        var ledger = new Hds006SourceLedger();
        var store = new Hds006InMemoryEvidenceStore(ledger);
        store.Record(new Hds006EvidenceDraft(21, "prefers-black-tea", Hds006EvidenceKind.Real,
            "msg-ghost", .9, 0, 1));

        var result = Retrieve(ledger, store, "black-tea", Hds006EvidenceKind.Real, 5);

        Assert.Empty(result.Hits);
        var exclusion = Assert.Single(result.Exclusions);
        Assert.Equal(21, exclusion.EvidenceId);
        Assert.Equal(Hds006ExclusionReason.SourceMissing, exclusion.Reason);
        var readout = ledger.Readout("msg-ghost");
        Assert.Equal(Hds006SourceState.Missing, readout.State);
        Assert.Equal("source_never_registered", readout.Reason);
    }

    [Fact]
    public void Invalidated_source_is_named_in_the_readout_and_its_row_is_excluded_with_that_reason()
    {
        var ledger = new Hds006SourceLedger();
        ledger.Register("msg-77", "host-seeded");
        var store = new Hds006InMemoryEvidenceStore(ledger);
        store.Record(new Hds006EvidenceDraft(31, "wants-quiet-evenings", Hds006EvidenceKind.Real,
            "msg-77", .9, 0, 1));
        ledger.Invalidate("msg-77", "source_message_deleted");

        var result = Retrieve(ledger, store, "quiet-evenings", Hds006EvidenceKind.Real, 5);

        Assert.Empty(result.Hits);
        var exclusion = Assert.Single(result.Exclusions);
        Assert.Equal(31, exclusion.EvidenceId);
        Assert.Equal(Hds006ExclusionReason.SourceInvalidated, exclusion.Reason);
        var readout = ledger.Readout("msg-77");
        Assert.Equal(Hds006SourceState.Invalidated, readout.State);
        Assert.Equal("source_message_deleted", readout.Reason);
        Assert.Equal(2, readout.SourceVersion);
    }

    // ───────────────────────── §一.3 统一检索一个入口 ─────────────────────────

    [Fact]
    public void Retrieval_orders_eligible_rows_by_recorded_sequence_then_evidence_id()
    {
        var ledger = new Hds006SourceLedger();
        ledger.Register("msg-ord", "host-seeded");
        var store = new Hds006InMemoryEvidenceStore(ledger);
        store.Record(new Hds006EvidenceDraft(41, "shared-token", Hds006EvidenceKind.Real, "msg-ord", .9, 0, 7));
        store.Record(new Hds006EvidenceDraft(42, "shared-token", Hds006EvidenceKind.Real, "msg-ord", .9, 0, 9));
        store.Record(new Hds006EvidenceDraft(43, "shared-token", Hds006EvidenceKind.Real, "msg-ord", .9, 0, 9));
        store.Record(new Hds006EvidenceDraft(44, "shared-token", Hds006EvidenceKind.Real, "msg-ord", .9, 0, 8));

        var result = Retrieve(ledger, store, "shared-token", Hds006EvidenceKind.Real, 5);

        // 排序规则逐字可判：新→旧，同序号按标识小→大（42 与 43 就是那一次并列）。
        Assert.Equal("42,43,44,41", string.Join(",", result.Hits.Select(x => x.EvidenceId)));
        Assert.Equal("1,2,3,4", string.Join(",", result.Hits.Select(x => x.Rank)));
        Assert.Empty(result.Exclusions);
    }

    [Fact]
    public void Truncation_reports_every_cut_row_with_the_rank_it_would_have_had()
    {
        var ledger = new Hds006SourceLedger();
        ledger.Register("msg-cut", "host-seeded");
        var store = new Hds006InMemoryEvidenceStore(ledger);
        store.Record(new Hds006EvidenceDraft(51, "cut-token", Hds006EvidenceKind.Real, "msg-cut", .9, 0, 1));
        store.Record(new Hds006EvidenceDraft(52, "cut-token", Hds006EvidenceKind.Real, "msg-cut", .9, 0, 2));
        store.Record(new Hds006EvidenceDraft(53, "cut-token", Hds006EvidenceKind.Real, "msg-cut", .9, 0, 3));
        store.Record(new Hds006EvidenceDraft(54, "cut-token", Hds006EvidenceKind.Real, "msg-cut", .9, 0, 4));

        var result = Retrieve(ledger, store, "cut-token", Hds006EvidenceKind.Real, 2);

        Assert.Equal("54,53", string.Join(",", result.Hits.Select(x => x.EvidenceId)));
        Assert.Equal(2, result.Exclusions.Count(x => x.Reason == Hds006ExclusionReason.BeyondLimit));
        Assert.Equal("52|3,51|4", string.Join(",", result.Exclusions
            .OrderBy(x => x.ObservedRank)
            .Select(x => x.EvidenceId + "|" + x.ObservedRank)));
    }

    // 明写事实：新链今天**没有**那把地板。既有 .5 只住在 src/Pry.Core/Memory/MemoryQueryService.cs:22，
    // 本原型不复制它、也不新增任何数字门槛；这条格把"没有地板"钉成事实——接线那一把要让它改绿
    // （地板守住），不许静默删掉它。
    [Fact]
    public void The_prototype_applies_no_retrieval_floor_the_existing_confidence_gate_would_have_applied()
    {
        var ledger = new Hds006SourceLedger();
        ledger.Register("msg-floor", "host-seeded");
        var store = new Hds006InMemoryEvidenceStore(ledger);
        store.Record(new Hds006EvidenceDraft(61, "low-confidence-token", Hds006EvidenceKind.Real,
            "msg-floor", .4, 0, 1));

        var result = Retrieve(ledger, store, "low-confidence-token", Hds006EvidenceKind.Real, 5);

        var hit = Assert.Single(result.Hits);
        Assert.Equal(61, hit.EvidenceId);
        Assert.Equal(.4, store.Find(61)!.Confidence);
        Assert.Empty(result.Exclusions);
    }

    // ───────────────────────── §一.4 派生物三处失效各自有格 ─────────────────────────

    [Theory]
    [InlineData(Hds006ArtifactKind.Summary)]
    [InlineData(Hds006ArtifactKind.Vector)]
    [InlineData(Hds006ArtifactKind.Cache)]
    public void Invalidating_one_derived_artifact_leaves_the_other_two_live_and_readable(
        Hds006ArtifactKind struck)
    {
        var ledger = new Hds006DerivedArtifactLedger();
        ledger.Publish(new Hds006DerivedArtifactKey(Hds006ArtifactKind.Summary, 71), "summary-text");
        ledger.Publish(new Hds006DerivedArtifactKey(Hds006ArtifactKind.Vector, 71), "vector-text");
        ledger.Publish(new Hds006DerivedArtifactKey(Hds006ArtifactKind.Cache, 71), "cache-text");

        Assert.True(ledger.TryInvalidate(new Hds006DerivedArtifactKey(struck, 71), "source_message_deleted"));

        foreach (var kind in new[] { Hds006ArtifactKind.Summary, Hds006ArtifactKind.Vector,
                    Hds006ArtifactKind.Cache })
        {
            var readout = ledger.Readout(new Hds006DerivedArtifactKey(kind, 71));
            if (kind == struck)
            {
                Assert.Equal(Hds006ArtifactState.Invalidated, readout.State);
                Assert.Equal("<redacted>", readout.Payload);
            }
            else
            {
                Assert.Equal(Hds006ArtifactState.Live, readout.State);
                Assert.Equal(SurvivorPayload(kind), readout.Payload);
            }
        }
    }

    [Fact]
    public void Derived_invalidation_redacts_the_payload_and_names_the_reason_in_the_readout()
    {
        var ledger = new Hds006DerivedArtifactLedger();
        ledger.Publish(new Hds006DerivedArtifactKey(Hds006ArtifactKind.Summary, 71), "summary-text");

        Assert.True(ledger.TryInvalidate(new Hds006DerivedArtifactKey(Hds006ArtifactKind.Summary, 71),
            "source_message_deleted"));

        var struck = ledger.Readout(new Hds006DerivedArtifactKey(Hds006ArtifactKind.Summary, 71));
        Assert.Equal(Hds006ArtifactState.Invalidated, struck.State);
        Assert.Equal("<redacted>", struck.Payload);
        Assert.Equal("source_message_deleted", struck.Reason);

        // 未发布是另一个读数，不是"同一件事的另一种说法"：正文为空、原因固定、版本为零。
        var never = ledger.Readout(new Hds006DerivedArtifactKey(Hds006ArtifactKind.Vector, 999));
        Assert.Equal(Hds006ArtifactState.Unpublished, never.State);
        Assert.Equal("", never.Payload);
        Assert.Equal("not_published", never.Reason);
        Assert.Equal(0, never.Version);
    }

    [Fact]
    public void Invalidation_appends_exactly_one_observable_entry_naming_the_artifact()
    {
        var ledger = new Hds006DerivedArtifactLedger();
        ledger.Publish(new Hds006DerivedArtifactKey(Hds006ArtifactKind.Summary, 71), "summary-text");
        ledger.Publish(new Hds006DerivedArtifactKey(Hds006ArtifactKind.Vector, 71), "vector-text");
        ledger.Publish(new Hds006DerivedArtifactKey(Hds006ArtifactKind.Cache, 71), "cache-text");

        Assert.True(ledger.TryInvalidate(new Hds006DerivedArtifactKey(Hds006ArtifactKind.Summary, 71),
            "vector_backend_reindexed"));

        // "内部布尔翻一下"不叫可观察：这一格数的是盘外可读的日志，且必须恰一条、点名到摘要那一处。
        var entry = Assert.Single(ledger.Observations, x => x.State == Hds006ArtifactState.Invalidated);
        Assert.Equal(Hds006ArtifactKind.Summary, entry.Key.Kind);
        Assert.Equal(71, entry.Key.EvidenceId);
        Assert.Equal("vector_backend_reindexed", entry.Reason);
    }

    // ───────────────────────── §一.5 撤销 CAS（v4:302 AUD-03 的那一课）─────────────────────────

    [Fact]
    public void Undo_against_a_row_that_moved_after_the_detach_is_refused_and_rewrites_nothing()
    {
        var (ledger, store) = SeededStore("msg-cas-1");
        store.Record(new Hds006EvidenceDraft(81, "quiet-evenings", Hds006EvidenceKind.Real, "msg-cas-1",
            .8, 0, 1));

        var first = store.TryDetach(81, 0, "message_deleted")!;
        var second = store.TryDetach(81, first.ExpectedStateVersion, "second_detach_in_same_undo")!;

        Assert.Equal(Hds006UndoOutcome.StateVersionMismatch, store.TryRestore(first));

        var row = store.Find(81)!;
        Assert.True(row.Detached, "被拒的撤销不得写回任何一个字段");
        Assert.Equal("second_detach_in_same_undo", row.DetachReason);
        Assert.Equal(second.ExpectedStateVersion, row.StateVersion);
        Assert.Equal("quiet-evenings", row.Content);
    }

    [Fact]
    public void Undo_whose_source_line_advanced_after_the_detach_is_refused_by_the_source_version()
    {
        var (ledger, store) = SeededStore("msg-shared");
        store.Record(new Hds006EvidenceDraft(91, "shared-source-line", Hds006EvidenceKind.Real, "msg-shared",
            .8, 0, 1));
        store.Record(new Hds006EvidenceDraft(92, "shared-source-line", Hds006EvidenceKind.Real, "msg-shared",
            .8, 0, 2));

        var first = store.TryDetach(91, 0, "message_deleted")!;
        var second = store.TryDetach(92, 0, "message_deleted")!;

        // 前提逐条明写：行自己的状态版本仍然对得上（所以响的确实是来源版本那一半），来源账已经走过一格。
        Assert.Equal(first.ExpectedStateVersion, store.Find(91)!.StateVersion);
        Assert.Equal(second.ExpectedSourceVersion, ledger.Readout("msg-shared").SourceVersion);

        Assert.Equal(Hds006UndoOutcome.SourceVersionMismatch, store.TryRestore(first));

        var row = store.Find(91)!;
        Assert.True(row.Detached, "来源版本不匹配时一个字段都不许写");
        Assert.Equal("message_deleted", row.DetachReason);
        Assert.Equal(first.PriorConfidence, row.Confidence);
    }

    [Fact]
    public void After_a_successful_undo_a_stale_write_cannot_overwrite_the_restored_content()
    {
        var (ledger, store) = SeededStore("msg-cas-3");
        store.Record(new Hds006EvidenceDraft(101, "original-text", Hds006EvidenceKind.Real, "msg-cas-3",
            .8, 0, 1));

        var record = store.TryDetach(101, 0, "message_deleted")!;
        Assert.Equal(Hds006UndoOutcome.Restored, store.TryRestore(record));
        Assert.Equal("original-text", store.Find(101)!.Content);
        Assert.False(store.Find(101)!.Detached);

        // AUD-03 的形状：拿着脱链后那一版的旧条件回来的后续写，必须写不进去，而不是"更新了但结果没人核"。
        Assert.False(store.TryRewrite(101, "late-evidence-overwrite", record.ExpectedStateVersion));
        Assert.Equal("original-text", store.Find(101)!.Content);
        Assert.Equal(record.ExpectedStateVersion + 1, store.Find(101)!.StateVersion);
    }

    [Fact]
    public void A_refused_undo_keeps_the_recovery_record_available()
    {
        var (ledger, store) = SeededStore("msg-cas-4");
        store.Record(new Hds006EvidenceDraft(111, "pre-deletion-text", Hds006EvidenceKind.Real, "msg-cas-4",
            .8, 0, 1));

        var record = store.TryDetach(111, 0, "message_deleted")!;
        Assert.True(store.TryRewrite(111, "user_manual_rewrite", record.ExpectedStateVersion));

        Assert.Equal(Hds006UndoOutcome.StateVersionMismatch, store.TryRestore(record));

        // v4:281 后半句：撤销失败不得先把唯一的恢复记录弹掉。
        var kept = Assert.Single(store.RecoveryRecords);
        Assert.Equal(111, kept.EvidenceId);
        Assert.Equal(record.ExpectedSourceVersion, kept.ExpectedSourceVersion);
        Assert.Equal("user_manual_rewrite", store.Find(111)!.Content);
    }

    // ───────────────────────── 夹具与局部函数 ─────────────────────────

    private static (Hds006SourceLedger Ledger, Hds006InMemoryEvidenceStore Store) SeededStore(string sourceId)
    {
        var ledger = new Hds006SourceLedger();
        ledger.Register(sourceId, "host-seeded");
        return (ledger, new Hds006InMemoryEvidenceStore(ledger));
    }

    private static (Hds006SourceLedger Ledger, Hds006InMemoryEvidenceStore Store) SeededThreeIdentities()
    {
        var (ledger, store) = SeededStore("msg-identity");
        store.Record(new Hds006EvidenceDraft(11, Sentence, Hds006EvidenceKind.Real, "msg-identity", .9, 0, 1));
        store.Record(new Hds006EvidenceDraft(12, Sentence, Hds006EvidenceKind.Fiction, "msg-identity", .9, 0, 2));
        store.Record(new Hds006EvidenceDraft(13, Sentence, Hds006EvidenceKind.Cognitive, "msg-identity", .9, 0, 3));
        return (ledger, store);
    }

    private static Hds006RetrievalResult Retrieve(Hds006SourceLedger ledger,
        Hds006InMemoryEvidenceStore store, string query, Hds006EvidenceKind identity, int limit) =>
        new Hds006UnifiedMemoryRetrieval(store, ledger)
            .Retrieve(new Hds006RetrievalRequest(query, identity, limit));

    // 三条探针各自的存活方正文：绝对字面量表，不是"从同一次发布里反推"。
    private static string SurvivorPayload(Hds006ArtifactKind kind) => kind switch
    {
        Hds006ArtifactKind.Summary => "summary-text",
        Hds006ArtifactKind.Vector => "vector-text",
        Hds006ArtifactKind.Cache => "cache-text",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
