using Pry.Core.Narrative;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// HDS-002 三类反例（派工单出口证据）：
/// ① 同实例双写者被拒；② 投递段唯一键 (commitId, actionId, segmentIndex) 冲突被拒；
/// ③ 迟到提交在条件写处被拒（所有权代次/故事版本落后即拒绝，不静默覆盖）。
/// 另附数据目录进程级写锁的形状测试。
/// </summary>
public sealed class NarrativeStorageContractTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 9, 0, 0, TimeSpan.Zero);
    private const string Prose = "她把笔放回盒里，看见这条消息，决定拒绝：画具还没收完呢。照片？不给。";
    private const string BoundQuote = "画具还没收完呢。照片？不给。";

    private static readonly CharacterInstanceKey PryKey = new()
    {
        LocalUserId = "local-user-1",
        CharacterId = "pry",
    };

    private static readonly CharacterInstanceId Alpha = CharacterInstanceId.Create("ci-instance-alpha");

    private static readonly InputRef Input101 = new(101, "msg-101", "room-1");
    private static readonly InputRef Input102 = new(102, "msg-102", "room-1");
    private static readonly InputRef Input103 = new(103, "msg-103", "room-1");

    private readonly CharacterInstanceRegistry _instances = new();
    private readonly OutboxOwnershipRegistry _ownership = new();
    private readonly NarrativeCommitWriter _writer;
    private readonly NarrativeRequest _request;

    public NarrativeStorageContractTests()
    {
        _instances.Resolve(PryKey, Alpha);
        _writer = new NarrativeCommitWriter(_ownership, _instances);
        _request = new NarrativeRequest
        {
            InstanceId = Alpha,
            ConversationId = "room-1",
            BatchId = "batch-1",
            Inputs = [Input101, Input102],
            Trigger = NarrativeTriggerKind.UserInput,
            StoryVersion = 0,
            OwnershipEpoch = 1,
            IssuedByWriter = "host-a",
            IssuedAtUtc = Now,
        }.Validate();
    }

    // ———— 反例①：同实例双写者 ————

    [Fact]
    public void Second_writer_on_the_same_instance_is_rejected_and_cannot_commit()
    {
        var first = Acquire("host-a");
        Assert.True(first.Accepted);
        Assert.Equal(1, first.Ticket!.OwnershipEpoch);

        var second = _ownership.TryAcquire(Alpha, EngineMode.NarrativeV1, "host-b", Now);
        Assert.False(second.Accepted);
        Assert.Null(second.Ticket);
        Assert.Equal(NarrativeContractReason.OwnershipHeldElsewhere, second.Reason);

        // 第二写者绕过登记处直接条件写：同一件事在写入口再挡一次，并且账上什么都没有。
        var rejected = _writer.TryCommit(_request, Commit(writer: "host-b"));
        Assert.False(rejected.Accepted);
        Assert.Equal(NarrativeContractReason.OwnershipHeldElsewhere, rejected.Reason);
        Assert.Null(_writer.StateOf(Alpha));

        // 持有者来写就成立：拒绝只针对"第二个"，不是把这条路径整个堵死。
        Assert.True(_writer.TryCommit(_request, Commit()).Accepted);
        Assert.Equal(1, _writer.StateOf(Alpha)!.AppliedCommitCount);
    }

    [Fact]
    public void Explicit_handover_raises_the_epoch_and_retires_the_old_ticket()
    {
        Acquire("host-a");
        Assert.True(_writer.TryCommit(_request, Commit()).Accepted);

        var handover = _ownership.TryHandover(Alpha, "host-a", 1, EngineMode.NarrativeV1, "host-b", Now);
        Assert.True(handover.Accepted);
        Assert.Equal(2, handover.Ticket!.OwnershipEpoch);

        // 被交接下台的旧写者：它连票都不是自己的了。
        var ousted = _writer.TryCommit(_request, Commit(writer: "host-a", epoch: 1));
        Assert.False(ousted.Accepted);
        Assert.Equal(NarrativeContractReason.OwnershipHeldElsewhere, ousted.Reason);

        // 新写者拿着自己接管前生成的落后代次提交：这正是 fencing 要挡的那种。
        var stale = _writer.TryCommit(_request, Commit(writer: "host-b", epoch: 1));
        Assert.False(stale.Accepted);
        Assert.Equal(NarrativeContractReason.OwnershipFenced, stale.Reason);

        // 两次拒绝之后账上仍是交接前那一次：旧票只能留下被拒的读数，改不了状态。
        Assert.Equal(1, _writer.StateOf(Alpha)!.StoryVersion);
        Assert.Equal(1, _writer.StateOf(Alpha)!.AppliedCommitCount);
        Assert.Equal("commit-1", _writer.StateOf(Alpha)!.LastCommitId);
    }

    [Fact]
    public void One_writer_cannot_hold_two_engine_modes_on_the_same_instance()
    {
        Assert.True(_ownership.TryAcquire(Alpha, EngineMode.Legacy, "host-a", Now).Accepted);
        var mixed = _ownership.TryAcquire(Alpha, EngineMode.NarrativeV1, "host-a", Now);
        Assert.False(mixed.Accepted);
        Assert.Equal(NarrativeContractReason.EngineModeMismatch, mixed.Reason);
    }

    [Fact]
    public void Release_by_someone_else_does_not_free_the_instance()
    {
        Acquire("host-a");
        var wrong = _ownership.Release(Alpha, "host-b", 1);
        Assert.False(wrong.Accepted);
        Assert.Equal(NarrativeContractReason.OwnershipHeldElsewhere, wrong.Reason);
        Assert.NotNull(_ownership.CurrentTicket(Alpha));
    }

    // ———— 反例①之二：不允许两份可发送真相 ————

    [Fact]
    public void New_outbox_cannot_be_declared_while_legacy_pending_remains()
    {
        Acquire("host-a");
        var early = _ownership.TryDeclareSendableTruth(Alpha, SendableTruthSource.NarrativeOutbox, "host-a", 1, 2);
        Assert.False(early.Accepted);
        Assert.Equal(NarrativeContractReason.SendableTruthNotDrained, early.Reason);
        Assert.Equal(SendableTruthSource.None, _ownership.CurrentSendableTruth(Alpha));

        Assert.True(_ownership.TryDeclareSendableTruth(Alpha, SendableTruthSource.NarrativeOutbox, "host-a", 1, 0).Accepted);
        Assert.Equal(SendableTruthSource.NarrativeOutbox, _ownership.CurrentSendableTruth(Alpha));

        // 已经是新 outbox，再想把旧 reply plan 也立成真相 ⇒ 拒绝。
        var second = _ownership.TryDeclareSendableTruth(Alpha, SendableTruthSource.LegacyReplyPlan, "host-a", 1, 0);
        Assert.False(second.Accepted);
        Assert.Equal(NarrativeContractReason.SendableTruthAlreadyDeclared, second.Reason);
        Assert.Equal(SendableTruthSource.NarrativeOutbox, _ownership.CurrentSendableTruth(Alpha));
    }

    [Fact]
    public void Only_the_current_writer_may_declare_the_sendable_truth()
    {
        Acquire("host-a");
        var intruder = _ownership.TryDeclareSendableTruth(Alpha, SendableTruthSource.NarrativeOutbox, "host-b", 1, 0);
        Assert.False(intruder.Accepted);
        Assert.Equal(NarrativeContractReason.OwnershipHeldElsewhere, intruder.Reason);
    }

    // ———— 反例②：投递段唯一键 ————

    [Fact]
    public void Same_commit_action_and_segment_index_cannot_be_written_twice()
    {
        CommitAndAssert();
        var key = new DeliverySegmentKey("commit-1", "act-1", 0);

        Assert.True(_writer.Ledger.TryAppend(Segment(0, BoundQuote), "host-a").Accepted);

        var duplicate = _writer.Ledger.TryAppend(Segment(0, "另一条后来改写的措辞"), "host-a");
        Assert.False(duplicate.Accepted);
        Assert.Equal(NarrativeContractReason.SegmentKeyDuplicate, duplicate.Reason);

        // 被拒的那一次没有改动账本：第一段还是它自己，条数也没多。
        var stored = _writer.Ledger.Find(key);
        Assert.Equal(BoundQuote, stored!.Text);
        Assert.Equal(DeliverySegmentState.Pending, stored.State);
        Assert.Single(_writer.Ledger.Snapshot());
    }

    [Fact]
    public void Segment_index_may_not_skip_ahead_and_action_may_not_grow_extra_segments()
    {
        CommitAndAssert(plannedSegments: 2);
        Assert.True(_writer.Ledger.TryAppend(Segment(0, "画具还没收完呢。"), "host-a").Accepted);

        var skipped = _writer.Ledger.TryAppend(Segment(2, "照片？不给。"), "host-a");
        Assert.False(skipped.Accepted);
        Assert.Equal(NarrativeContractReason.SegmentCountExceeded, skipped.Reason);

        var beyondPlan = _writer.Ledger.TryAppend(Segment(1, "照片？不给。"), "host-a");
        Assert.True(beyondPlan.Accepted);
        var third = _writer.Ledger.TryAppend(Segment(2, "再多一句"), "host-a");
        Assert.False(third.Accepted);
        Assert.Equal(NarrativeContractReason.SegmentCountExceeded, third.Reason);
    }

    [Fact]
    public void Segment_text_must_be_taken_from_the_bound_narrative_verbatim()
    {
        CommitAndAssert(plannedSegments: 2);
        var rewritten = _writer.Ledger.TryAppend(Segment(0, "好的，我拍一张给你。"), "host-a");
        Assert.False(rewritten.Accepted);
        Assert.Equal(NarrativeContractReason.SegmentTextNotFromNarrative, rewritten.Reason);
        Assert.Empty(_writer.Ledger.Snapshot());

        // 最后一段封口时必须正好还原完整绑定原文，少一个字也是第二次改写。
        Assert.True(_writer.Ledger.TryAppend(Segment(0, "画具还没收完呢。"), "host-a").Accepted);
        var shortClose = _writer.Ledger.TryAppend(Segment(1, "照片？"), "host-a");
        Assert.False(shortClose.Accepted);
        Assert.Equal(NarrativeContractReason.SegmentTextNotFromNarrative, shortClose.Reason);
    }

    [Fact]
    public void Delivered_segment_is_terminal_and_a_late_result_cannot_flip_it()
    {
        CommitAndAssert();
        var key = new DeliverySegmentKey("commit-1", "act-1", 0);
        Assert.True(_writer.Ledger.TryAppend(Segment(0, BoundQuote), "host-a").Accepted);
        Assert.True(_writer.Ledger.TryMarkDelivered(key, "history-7", 1, "host-a").Accepted);

        var cancel = _writer.Ledger.TryMarkCancelled(key, "迟到的取消", 1, "host-a");
        Assert.False(cancel.Accepted);
        Assert.Equal(NarrativeContractReason.SegmentTerminalImmutable, cancel.Reason);

        var redeliver = _writer.Ledger.TryMarkDelivered(key, "history-8", 1, "host-a");
        Assert.False(redeliver.Accepted);
        Assert.Equal(NarrativeContractReason.SegmentTerminalImmutable, redeliver.Reason);
        Assert.Equal("history-7", _writer.Ledger.Find(key)!.DeliveredMessageId);
    }

    [Fact]
    public void Later_segment_cannot_be_marked_delivered_before_its_predecessor()
    {
        CommitAndAssert(plannedSegments: 2);
        Assert.True(_writer.Ledger.TryAppend(Segment(0, "画具还没收完呢。"), "host-a").Accepted);
        Assert.True(_writer.Ledger.TryAppend(Segment(1, "照片？不给。"), "host-a").Accepted);

        var outOfOrder = _writer.Ledger.TryMarkDelivered(new DeliverySegmentKey("commit-1", "act-1", 1),
            "history-2", 1, "host-a");
        Assert.False(outOfOrder.Accepted);
        Assert.Equal(NarrativeContractReason.SegmentPredecessorPending, outOfOrder.Reason);
        Assert.Equal(DeliverySegmentState.Pending,
            _writer.Ledger.Find(new DeliverySegmentKey("commit-1", "act-1", 1))!.State);
    }

    [Fact]
    public void A_segment_needs_real_history_evidence_to_be_called_delivered()
    {
        CommitAndAssert();
        var key = new DeliverySegmentKey("commit-1", "act-1", 0);
        Assert.True(_writer.Ledger.TryAppend(Segment(0, BoundQuote), "host-a").Accepted);
        var hollow = _writer.Ledger.TryMarkDelivered(key, null, 1, "host-a");
        Assert.False(hollow.Accepted);
        Assert.Equal(NarrativeContractReason.SegmentDeliveryEvidenceMissing, hollow.Reason);
    }

    [Fact]
    public void Fenced_writer_cannot_touch_the_delivery_ledger()
    {
        CommitAndAssert();
        Assert.True(_ownership.Release(Alpha, "host-a", 1).Accepted);
        var takeover = _ownership.TryAcquire(Alpha, EngineMode.NarrativeV1, "host-b", Now);
        Assert.Equal(2, takeover.Ticket!.OwnershipEpoch);

        // host-b 手上这张早于接管生成的段仍带着代次 1：新写者也不能用落后的凭据写账本。
        var stale = _writer.Ledger.TryAppend(Segment(0, BoundQuote) with { OwnershipEpoch = 1 }, "host-b");
        Assert.False(stale.Accepted);
        Assert.Equal(NarrativeContractReason.OwnershipFenced, stale.Reason);

        // 同一写者换上自己的当代凭据，同一件事就成立：拒绝针对的是落后代次，不是把这条路整个堵死。
        var fresh = _writer.Ledger.TryAppend(Segment(0, BoundQuote) with { OwnershipEpoch = 2 }, "host-b");
        Assert.True(fresh.Accepted, fresh.Detail);
    }

    [Fact]
    public void Segments_cannot_be_appended_for_a_commit_that_never_applied()
    {
        Acquire("host-a");
        var ghost = _writer.Ledger.TryAppend(Segment(0, BoundQuote) with { CommitId = "commit-never" }, "host-a");
        Assert.False(ghost.Accepted);
        Assert.Equal(NarrativeContractReason.SegmentCommitUnknown, ghost.Reason);
    }

    // ———— 反例③：迟到提交在条件写处被拒 ————

    [Fact]
    public void Late_commit_from_a_lost_ownership_epoch_is_rejected_without_overwriting_anything()
    {
        Acquire("host-a");
        var applied = CommitAndAssert();
        var stateAfterFirst = _writer.StateOf(Alpha)!;
        Assert.Equal(102, applied.State!.ProcessedThrough.Seq);

        // 进程接管：显式交接后新写者拿到代次 2。
        Assert.True(_ownership.TryHandover(Alpha, "host-a", 1, EngineMode.NarrativeV1, "host-b", Now).Accepted);
        var secondBatch = _request with
        {
            BatchId = "batch-2",
            Inputs = [Input103],
            StoryVersion = 1,
            ProcessedThroughAtIssue = new ProcessedThroughSeq(102),
        };

        var late = _writer.TryCommit(secondBatch, Commit(commitId: "commit-late", writer: "host-a", epoch: 1,
            expectedStory: 1, watermark: 103, consumed: [103], batchId: "batch-2", perceived: [Input103]));
        Assert.False(late.Accepted);
        Assert.Equal(NarrativeContractReason.OwnershipHeldElsewhere, late.Reason);

        // 同一条迟到提交如果换上新写者的身份、却仍带着旧代次，就是 fencing 落后 ⇒ 同样被条件写挡下。
        var fencedLate = _writer.TryCommit(secondBatch, Commit(commitId: "commit-late-2", writer: "host-b", epoch: 1,
            expectedStory: 1, watermark: 103, consumed: [103], batchId: "batch-2", perceived: [Input103]));
        Assert.False(fencedLate.Accepted);
        Assert.Equal(NarrativeContractReason.OwnershipFenced, fencedLate.Reason);

        // 关键：拒绝之后账上还是第一次提交的那一份，水位与故事版本都没被迟到结果覆盖。
        Assert.Equal(stateAfterFirst.LastCommitId, late.State!.LastCommitId);
        Assert.Equal(1, late.State.StoryVersion);
        Assert.Equal(102, late.State.ProcessedThrough.Seq);
        Assert.DoesNotContain(_writer.Ledger.Snapshot(), s => s.CommitId is "commit-late" or "commit-late-2");

        var fresh = _writer.TryCommit(secondBatch, Commit(commitId: "commit-2", writer: "host-b", epoch: 2,
            expectedStory: 1, watermark: 103, consumed: [103], batchId: "batch-2", perceived: [Input103]));
        Assert.True(fresh.Accepted, fresh.Detail);
        Assert.Equal(103, fresh.State!.ProcessedThrough.Seq);
        Assert.Equal(2, fresh.State.StoryVersion);
    }

    [Fact]
    public void Commit_built_on_a_stale_story_version_is_rejected_as_late()
    {
        Acquire("host-a");
        CommitAndAssert();
        var stale = _writer.TryCommit(_request, Commit(commitId: "commit-stale", expectedStory: 0, watermark: 102));
        Assert.False(stale.Accepted);
        Assert.Equal(NarrativeContractReason.LateCommitRejected, stale.Reason);
        Assert.Equal(1, _writer.StateOf(Alpha)!.StoryVersion);
    }

    [Fact]
    public void A_story_version_ahead_of_the_ledger_is_rejected_too()
    {
        Acquire("host-a");
        var ahead = _writer.TryCommit(_request, Commit(expectedStory: 4, watermark: 102));
        Assert.False(ahead.Accepted);
        Assert.Equal(NarrativeContractReason.StoryVersionConflict, ahead.Reason);
    }

    [Fact]
    public void Replaying_the_same_commit_does_not_consume_input_twice()
    {
        Acquire("host-a");
        CommitAndAssert();
        var replay = _writer.TryCommit(_request, Commit());
        Assert.False(replay.Accepted);
        Assert.Equal(NarrativeContractReason.CommitAlreadyApplied, replay.Reason);
        Assert.Equal(102, _writer.StateOf(Alpha)!.ProcessedThrough.Seq);
        Assert.Equal(1, _writer.StateOf(Alpha)!.AppliedCommitCount);
    }

    [Fact]
    public void Reusing_a_commit_id_for_different_content_is_rejected()
    {
        Acquire("host-a");
        CommitAndAssert();
        var hijack = _writer.TryCommit(_request, Commit() with { NarrativeProse = "完全不同的一段叙事。", ContentHash = "sha256:other" });
        Assert.False(hijack.Accepted);
        Assert.Equal(NarrativeContractReason.DuplicateCommitId, hijack.Reason);
    }

    [Fact]
    public void Watermark_can_never_regress_under_a_new_commit_id()
    {
        Acquire("host-a");
        CommitAndAssert();
        var regress = _writer.TryCommit(_request, Commit(commitId: "commit-2", expectedStory: 1, watermark: 101));
        Assert.False(regress.Accepted);
        Assert.Equal(NarrativeContractReason.WatermarkNotAdvancing, regress.Reason);
        Assert.Equal(102, _writer.StateOf(Alpha)!.ProcessedThrough.Seq);
    }

    [Fact]
    public void Commit_may_not_consume_input_the_host_never_received()
    {
        Acquire("host-a");
        var phantom = _writer.TryCommit(_request, Commit(watermark: 102, consumed: [101, 102, 404]));
        Assert.False(phantom.Accepted);
        Assert.Equal(NarrativeContractReason.ProcessedInputUnreceived, phantom.Reason);
    }

    [Fact]
    public void Late_commit_after_conversation_deletion_cannot_revive_the_scope()
    {
        Acquire("host-a");
        CommitAndAssert();
        _writer.InvalidateConversation("room-1", deletionEpoch: 5);

        var late = _writer.TryCommit(_request, Commit(commitId: "commit-2", expectedStory: 1, watermark: 102,
            deletionEpoch: 3));
        Assert.False(late.Accepted);
        Assert.Equal(NarrativeContractReason.DeletionEpochAdvanced, late.Reason);
        Assert.Equal(1, _writer.StateOf(Alpha)!.AppliedCommitCount);
    }

    [Fact]
    public void Commit_for_an_unregistered_instance_has_nowhere_to_belong()
    {
        var stranger = CharacterInstanceId.Create("ci-instance-stranger");
        Acquire("host-a");
        var orphan = _writer.TryCommit(_request, Commit(instanceId: stranger));
        Assert.False(orphan.Accepted);
        Assert.Equal(NarrativeContractReason.InstanceUnknown, orphan.Reason);
    }

    [Fact]
    public void Commit_without_content_hash_cannot_prove_itself_after_a_crash()
    {
        Acquire("host-a");
        var faceless = _writer.TryCommit(_request, Commit() with { ContentHash = null });
        Assert.False(faceless.Accepted);
        Assert.Equal(NarrativeContractReason.ContentHashMissing, faceless.Reason);
    }

    [Fact]
    public void Commit_with_an_unknown_contract_version_is_refused()
    {
        Acquire("host-a");
        var future = _writer.TryCommit(_request, Commit() with { ContractVersion = 99 });
        Assert.False(future.Accepted);
        Assert.Equal(NarrativeContractReason.ContractVersionUnsupported, future.Reason);
    }

    [Fact]
    public void Silence_still_lands_a_commit_but_opens_no_delivery_at_all()
    {
        Acquire("host-a");
        var silent = _writer.TryCommit(_request, Commit(actions:
        [
            new CommunicationAction
            {
                ActionId = "act-silence",
                Mode = CommunicationMode.Silence,
                TargetConversationId = "room-1",
            },
        ]));
        Assert.True(silent.Accepted);
        Assert.Equal(102, silent.State!.ProcessedThrough.Seq);
        Assert.Empty(_writer.Ledger.Snapshot());

        var ghostSegment = _writer.Ledger.TryAppend(
            Segment(0, "宿主替角色补的一句话") with { CommitId = "commit-1", ActionId = "act-silence" }, "host-a");
        Assert.False(ghostSegment.Accepted);
        Assert.Equal(NarrativeContractReason.SegmentCountExceeded, ghostSegment.Reason);
    }

    // ———— 数据目录进程级独占写锁的形状 ————

    [Fact]
    public void Runtime_descriptor_is_discovery_not_ownership()
    {
        var gate = new DataDirectoryWriteGate();
        var observation = new DataDirectoryObservation(
            DataDirectoryPath: "C:/temp/pry-hds002-isolated",
            ExclusiveWriteLockHeldByAnotherProcess: false,
            RuntimeDescriptorPresent: true,
            DescriptorProcessAlive: false,
            KnownFencingEpoch: 0);

        var byDescriptor = gate.TryAcquire(
            new WriteLeaseCredential(WriteLeaseCredentialKind.DiscoveryDescriptorOnly, "host-b", 0), observation);

        Assert.False(byDescriptor.Accepted);
        Assert.Null(byDescriptor.Lease);
        Assert.Equal(NarrativeContractReason.DescriptorIsNotALock, byDescriptor.Reason);
        Assert.Equal(NarrativeContractReason.DescriptorIsNotALock, gate.CheckConditionalWriteAllowed(
            new WriteLeaseCredential(WriteLeaseCredentialKind.DiscoveryDescriptorOnly, "host-b", 42)));
    }

    [Fact]
    public void Second_process_stops_before_writing_instead_of_warning_and_continuing()
    {
        var gate = new DataDirectoryWriteGate();
        var free = new DataDirectoryObservation("C:/temp/pry-hds002-isolated", false, false, false, 0);
        Assert.True(gate.TryAcquire(new WriteLeaseCredential(WriteLeaseCredentialKind.ExclusiveWriteLock, "host-a", 0), free)
            .Accepted);

        var held = gate.TryAcquire(new WriteLeaseCredential(WriteLeaseCredentialKind.ExclusiveWriteLock, "host-b", 1),
            free with { ExclusiveWriteLockHeldByAnotherProcess = true });
        Assert.False(held.Accepted);
        Assert.Equal(NarrativeContractReason.WriteLockHeldByOther, held.Reason);

        // 第二个宿主即便想跳过登记处直接条件写，也拿不到"允许"的读数。
        Assert.Equal(NarrativeContractReason.WriteLockHeldByOther,
            gate.CheckConditionalWriteAllowed(new WriteLeaseCredential(WriteLeaseCredentialKind.ExclusiveWriteLock, "host-b", 1)));
    }

    [Fact]
    public void Lost_ownership_fences_every_write_from_the_old_credential()
    {
        var gate = new DataDirectoryWriteGate();
        var free = new DataDirectoryObservation("C:/temp/pry-hds002-isolated", false, false, false, 0);
        var acquired = gate.TryAcquire(new WriteLeaseCredential(WriteLeaseCredentialKind.ExclusiveWriteLock, "host-a", 0), free);
        Assert.True(acquired.Accepted);
        var oldLease = gate.CurrentLease!;

        var fenced = gate.FenceAndReacquire("host-a", free);
        Assert.True(fenced.Accepted);
        Assert.Equal(2, fenced.Lease!.FencingEpoch);

        Assert.Equal(NarrativeContractReason.WriteLeaseLost, gate.CheckConditionalWriteAllowed(oldLease));
        Assert.Equal(NarrativeContractReason.None, gate.CheckConditionalWriteAllowed(gate.CurrentLease!));

        // 锁还在别人手上时，fencing 不能凭空发新凭据。
        Assert.Equal(NarrativeContractReason.WriteLockHeldByOther,
            gate.FenceAndReacquire("host-a", free with { ExclusiveWriteLockHeldByAnotherProcess = true }).Reason);
    }

    [Fact]
    public void Release_requires_the_current_holder_and_epoch()
    {
        var gate = new DataDirectoryWriteGate();
        var free = new DataDirectoryObservation("C:/temp/pry-hds002-isolated", false, false, false, 0);
        gate.TryAcquire(new WriteLeaseCredential(WriteLeaseCredentialKind.ExclusiveWriteLock, "host-a", 0), free);

        Assert.False(gate.Release("host-b", 1).Accepted);
        var released = gate.Release("host-a", gate.CurrentFencingEpoch);
        Assert.True(released.Accepted);
        Assert.Null(gate.CurrentLease);
        Assert.Equal(NarrativeContractReason.WriteLockStateUnknown, gate.CheckConditionalWriteAllowed(
            new WriteLeaseCredential(WriteLeaseCredentialKind.ExclusiveWriteLock, "host-a", 1)));
    }

    // ———— 夹具 ————

    private OwnershipGrant Acquire(string writerId) => _ownership.TryAcquire(Alpha, EngineMode.NarrativeV1, writerId, Now);

    private ConditionalCommitOutcome CommitAndAssert(int plannedSegments = 1)
    {
        // TryAcquire 对同写者是幂等的：这里只是保证投递类用例也有一张当前代次的票。
        Assert.True(Acquire("host-a").Accepted);
        var outcome = _writer.TryCommit(_request, Commit(plannedSegments: plannedSegments));
        Assert.True(outcome.Accepted, outcome.Detail);
        return outcome;
    }

    private static NarrativeCommit Commit(
        string commitId = "commit-1",
        string writer = "host-a",
        long epoch = 1,
        long expectedStory = 0,
        long watermark = 102,
        IReadOnlyList<long>? consumed = null,
        long deletionEpoch = 0,
        int plannedSegments = 1,
        CharacterInstanceId? instanceId = null,
        CommunicationAction[]? actions = null,
        InputRef[]? perceived = null,
        string? batchId = null) => new()
    {
        CommitId = commitId,
        InstanceId = instanceId ?? Alpha,
        ConversationId = "room-1",
        BatchId = batchId ?? "batch-1",
        NarrativeProse = Prose,
        Actions = actions ??
        [
            new CommunicationAction
            {
                ActionId = "act-1",
                Mode = CommunicationMode.Immediate,
                TargetConversationId = "room-1",
                ProseQuote = BoundQuote,
                PlannedSegmentCount = plannedSegments,
            },
        ],
        ProcessedThrough = new ProcessedThroughSeq(watermark),
        PerceivedInputRefs = new PerceivedInputRefs { Refs = perceived ?? [Input102] },
        ExpectedStoryVersion = expectedStory,
        DeletionEpoch = deletionEpoch,
        OwnershipEpoch = epoch,
        ContentHash = "sha256:commit-one",
        CommittedByWriter = writer,
        CommittedAtUtc = Now,
        ProposalId = "proposal-1",
        ConsumedInputSeqs = consumed ?? [101, 102],
    };

    private static DeliverySegment Segment(int index, string text) => new()
    {
        InstanceId = Alpha,
        ConversationId = "room-1",
        CommitId = "commit-1",
        ActionId = "act-1",
        SegmentIndex = index,
        State = DeliverySegmentState.Pending,
        Text = text,
        OwnershipEpoch = 1,
    };
}
