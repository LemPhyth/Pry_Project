using Pry.Core.Narrative;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// 第十四波判据刀：第十二波 §六 待拆清单登记的 16 枚"改坏了今天全仓一声不响"的拒绝出口，
/// 各钉一条会响的判据。零产品代码——只新增本文件，不碰 NarrativeIdentityContractTests.cs、不碰任何既有断言。
/// 断言形状：抛异常出口用 Assert.Throws&lt;NarrativeContractException&gt; ＋ Reason/Message 逐条点名等值；
/// 进 rejections 的出口用 Assert.Single ＋ Reason 点名；提交侧 :403 因与 :410 同 Reason 且互相 continue 短路，
/// 用 Assert.Collection 把两条拒绝逐条钉死（含提交侧那条的 Detail 原文）才对该门敏感。
/// </summary>
public sealed class NarrativeCommitAndActionGuardTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 8, 0, 0, TimeSpan.Zero);
    private static readonly CharacterInstanceId Alpha = CharacterInstanceId.Create("ci-instance-alpha");
    private const string Prose = "她把笔放回盒里，看见这条消息，决定拒绝：画具还没收完呢。照片？不给。";

    // ===== RequireText 抛异常型（R1–R5）=====

    [Fact]
    public void Blank_candidate_id_throws_rather_than_asserting_without_a_subject()
    {
        var (_, draft) = Fixture();
        var candidate = new KnowledgeCandidate
        {
            CandidateId = "",
            InstanceId = draft.InstanceId,
            Kind = CognitionKind.UserReportedFact,
            Origin = CognitionOrigin.UserMessageEvidence,
            Claim = "她说她要搬家",
            Confidence = .9,
            EvidenceRefs = ["msg-102"],
            ProposedBy = "narrator",
        };

        var error = Assert.Throws<NarrativeContractException>(
            () => KnowledgeCandidatePolicy.Validate(candidate, NarrativeContent.From(draft)));
        Assert.Equal(NarrativeContractReason.ConfirmationWithoutUserEvidence, error.Reason);
        Assert.Equal("CandidateId 不能为空。", error.Message);
    }

    [Fact]
    public void Blank_claim_throws_rather_than_asserting_an_empty_reality()
    {
        var (_, draft) = Fixture();
        var candidate = new KnowledgeCandidate
        {
            CandidateId = "kc-claim",
            InstanceId = draft.InstanceId,
            Kind = CognitionKind.UserReportedFact,
            Origin = CognitionOrigin.UserMessageEvidence,
            Claim = "",
            Confidence = .9,
            EvidenceRefs = ["msg-102"],
            ProposedBy = "narrator",
        };

        var error = Assert.Throws<NarrativeContractException>(
            () => KnowledgeCandidatePolicy.Validate(candidate, NarrativeContent.From(draft)));
        Assert.Equal(NarrativeContractReason.RealityContamination, error.Reason);
        Assert.Equal("Claim 不能为空。", error.Message);
    }

    [Fact]
    public void Blank_proposed_by_throws_rather_than_anonymous_confirmation()
    {
        var (_, draft) = Fixture();
        var candidate = new KnowledgeCandidate
        {
            CandidateId = "kc-proposer",
            InstanceId = draft.InstanceId,
            Kind = CognitionKind.UserReportedFact,
            Origin = CognitionOrigin.UserMessageEvidence,
            Claim = "她说她要搬家",
            Confidence = .9,
            EvidenceRefs = ["msg-102"],
            ProposedBy = "",
        };

        var error = Assert.Throws<NarrativeContractException>(
            () => KnowledgeCandidatePolicy.Validate(candidate, NarrativeContent.From(draft)));
        Assert.Equal(NarrativeContractReason.ConfirmationWithoutUserEvidence, error.Reason);
        Assert.Equal("ProposedBy 不能为空。", error.Message);
    }

    [Fact]
    public void Blank_intent_id_throws_rather_than_an_addressable_promise()
    {
        var (_, draft) = Fixture();
        var intent = draft.Intents[0] with { IntentId = "" };

        var error = Assert.Throws<NarrativeContractException>(
            () => NarrativeIntentPolicy.Validate(intent, NarrativeContent.From(draft)));
        Assert.Equal(NarrativeContractReason.IntentClaimedAsFact, error.Reason);
        Assert.Equal("IntentId 不能为空。", error.Message);
    }

    [Fact]
    public void Blank_intent_topic_throws_rather_than_an_intention_about_nothing()
    {
        var (_, draft) = Fixture();
        var intent = draft.Intents[0] with { Topic = "" };

        var error = Assert.Throws<NarrativeContractException>(
            () => NarrativeIntentPolicy.Validate(intent, NarrativeContent.From(draft)));
        Assert.Equal(NarrativeContractReason.IntentClaimedAsFact, error.Reason);
        Assert.Equal("Topic 不能为空。", error.Message);
    }

    // ===== 动作侧（A1–A6）=====

    [Fact]
    public void Blank_action_id_throws_before_any_scope_decision_is_made()
    {
        var (request, draft) = Fixture();
        var content = NarrativeContent.From(draft with
        {
            Actions = [draft.Actions[0] with { ActionId = "" }],
        });

        var error = Assert.Throws<NarrativeContractException>(
            () => NarrativeDraftValidator.Validate(content, request));
        Assert.Equal(NarrativeContractReason.ActionTargetMismatch, error.Reason);
        Assert.Equal("ActionId 不能为空。", error.Message);
    }

    [Fact]
    public void Immediate_action_segment_above_host_ceiling_is_refused()
    {
        var (request, draft) = Fixture();
        // 默认宿主上限 MaxOutboundSegments＝4；段数 5 越过上界一侧。
        var content = NarrativeContent.From(draft with
        {
            Actions = [draft.Actions[0] with { PlannedSegmentCount = 5 }],
        });

        var rejections = NarrativeDraftValidator.Validate(content, request);
        var rejection = Assert.Single(rejections);
        Assert.Equal(NarrativeContractReason.ActionSegmentCountInvalid, rejection.Reason);
    }

    [Fact]
    public void Immediate_action_that_also_hangs_a_future_intent_is_refused()
    {
        var (request, draft) = Fixture();
        var content = NarrativeContent.From(draft with
        {
            Actions = [draft.Actions[0] with { DeferredIntentId = "intent-1" }],
        });

        var rejections = NarrativeDraftValidator.Validate(content, request);
        var rejection = Assert.Single(rejections);
        Assert.Equal(NarrativeContractReason.IntentClaimedAsFact, rejection.Reason);
    }

    [Fact]
    public void Deferred_action_reserving_delivery_segments_is_refused()
    {
        var (request, draft) = Fixture();
        var content = NarrativeContent.From(draft with
        {
            Actions =
            [
                new CommunicationAction
                {
                    ActionId = "act-deferred-seg",
                    Mode = CommunicationMode.Deferred,
                    TargetConversationId = "room-1",
                    PlannedSegmentCount = 1,
                    DeferredIntentId = "intent-1",
                },
            ],
        });

        var rejections = NarrativeDraftValidator.Validate(content, request);
        var rejection = Assert.Single(rejections);
        Assert.Equal(NarrativeContractReason.SegmentCountExceeded, rejection.Reason);
    }

    [Fact]
    public void Deferred_action_carrying_immediate_prose_is_refused()
    {
        var (request, draft) = Fixture();
        var content = NarrativeContent.From(draft with
        {
            Actions =
            [
                new CommunicationAction
                {
                    ActionId = "act-deferred-prose",
                    Mode = CommunicationMode.Deferred,
                    TargetConversationId = "room-1",
                    ProseQuote = "画具还没收完呢。照片？不给。",
                    DeferredIntentId = "intent-1",
                },
            ],
        });

        var rejections = NarrativeDraftValidator.Validate(content, request);
        var rejection = Assert.Single(rejections);
        Assert.Equal(NarrativeContractReason.ActionBindingMissing, rejection.Reason);
    }

    [Fact]
    public void Silence_action_reserving_a_segment_is_refused()
    {
        var (request, draft) = Fixture();
        var content = NarrativeContent.From(draft with
        {
            Actions =
            [
                new CommunicationAction
                {
                    ActionId = "act-silence-seg",
                    Mode = CommunicationMode.Silence,
                    TargetConversationId = "room-1",
                    PlannedSegmentCount = 1,
                },
            ],
        });

        var rejections = NarrativeDraftValidator.Validate(content, request);
        var rejection = Assert.Single(rejections);
        Assert.Equal(NarrativeContractReason.SegmentCountExceeded, rejection.Reason);
    }

    // ===== 提交侧（C1–C5）=====

    [Fact]
    public void Blank_commit_id_throws_rather_than_a_hashless_commit_identity()
    {
        var (request, _) = Fixture();
        var commit = CommitFixture() with { CommitId = "" };

        var error = Assert.Throws<NarrativeContractException>(
            () => NarrativeCommitValidator.Validate(commit, request));
        Assert.Equal(NarrativeContractReason.ContentHashMissing, error.Reason);
        Assert.Equal("CommitId 不能为空。", error.Message);
    }

    [Fact]
    public void Commit_with_an_unsupported_contract_version_is_refused()
    {
        var (request, _) = Fixture();
        var commit = CommitFixture() with { ContractVersion = 2 };

        var rejections = NarrativeCommitValidator.Validate(commit, request);
        var rejection = Assert.Single(rejections);
        Assert.Equal(NarrativeContractReason.ContractVersionUnsupported, rejection.Reason);
    }

    [Fact]
    public void Commit_without_host_content_hash_is_refused()
    {
        var (request, _) = Fixture();
        var commit = CommitFixture() with { ContentHash = null };

        var rejections = NarrativeCommitValidator.Validate(commit, request);
        var rejection = Assert.Single(rejections);
        Assert.Equal(NarrativeContractReason.ContentHashMissing, rejection.Reason);
    }

    [Fact]
    public void Commit_intent_without_source_is_refused_by_both_layers()
    {
        var (request, _) = Fixture();
        var commit = CommitFixture() with
        {
            Intents = [CommitFixture().Intents[0] with { SourceCommitId = "" }],
        };

        var rejections = NarrativeCommitValidator.Validate(commit, request);
        // 草稿层 :275 与提交层 :403 对同一空白来源各记一条 IntentClaimedAsFact；
        // :403 命中后 continue 短路 :410，摘掉 :403 会让空白落到 :410 而只有 Detail 不同，
        // 故第二条必须把提交侧那条的 Detail 原文钉死，才对 :403 敏感。
        Assert.Collection(
            rejections,
            rejection => Assert.Equal(NarrativeContractReason.IntentClaimedAsFact, rejection.Reason),
            rejection =>
            {
                Assert.Equal(NarrativeContractReason.IntentClaimedAsFact, rejection.Reason);
                Assert.Equal("意图 intent-1 进入提交时仍没有来源凭据。", rejection.Detail);
            });
    }

    [Fact]
    public void Commit_intent_whose_source_is_neither_commit_nor_batch_is_refused()
    {
        var (request, _) = Fixture();
        // 非空白来源：草稿层 :275 不响，干净地只由提交层 :410 拒绝。
        var commit = CommitFixture() with
        {
            Intents = [CommitFixture().Intents[0] with { SourceCommitId = "foreign-source" }],
        };

        var rejections = NarrativeCommitValidator.Validate(commit, request);
        var rejection = Assert.Single(rejections);
        Assert.Equal(NarrativeContractReason.IntentClaimedAsFact, rejection.Reason);
    }

    // ===== 夹具 =====

    /// <summary>一条合法的即时叙事草稿（与 NarrativeIdentityContractTests 同形），用于逐枚改坏一侧。</summary>
    private static (NarrativeRequest Request, NarrativeDraft Draft) Fixture()
    {
        var request = new NarrativeRequest
        {
            InstanceId = Alpha,
            ConversationId = "room-1",
            BatchId = "batch-1",
            Inputs =
            [
                new InputRef(101, "msg-101", "room-1"),
                new InputRef(102, "msg-102", "room-1"),
            ],
            Trigger = NarrativeTriggerKind.UserInput,
            StoryVersion = 0,
            OwnershipEpoch = 1,
            IssuedByWriter = "host-a",
            IssuedAtUtc = Now,
        }.Validate();

        var draft = new NarrativeDraft
        {
            InstanceId = Alpha,
            ConversationId = "room-1",
            BatchId = "batch-1",
            NarrativeProse = Prose,
            ProcessedThrough = new ProcessedThroughSeq(102),
            PerceivedInputRefs = new PerceivedInputRefs
            {
                Refs = [new InputRef(102, "msg-102", "room-1")],
            },
            Actions =
            [
                new CommunicationAction
                {
                    ActionId = "act-1",
                    Mode = CommunicationMode.Immediate,
                    TargetConversationId = "room-1",
                    ProseQuote = "画具还没收完呢。照片？不给。",
                    PlannedSegmentCount = 1,
                },
            ],
            Intents =
            [
                new NarrativeIntent
                {
                    IntentId = "intent-1",
                    InstanceId = Alpha,
                    Topic = "把画具收完",
                    Status = NarrativeIntentStatus.Pending,
                    SourceCommitId = "batch-1",
                    EarliestDueAtUtc = Now.AddMinutes(20),
                },
            ],
            StoryVersionBase = 0,
            OwnershipEpoch = 1,
            ProposalId = "proposal-1",
            ProposedAtUtc = Now,
        };

        return (request, draft);
    }

    /// <summary>一条合法提交（草稿层与提交层全不响），用于逐枚改坏提交侧。</summary>
    private static NarrativeCommit CommitFixture() => new()
    {
        CommitId = "commit-1",
        InstanceId = Alpha,
        ConversationId = "room-1",
        BatchId = "batch-1",
        NarrativeProse = Prose,
        Actions =
        [
            new CommunicationAction
            {
                ActionId = "act-1",
                Mode = CommunicationMode.Immediate,
                TargetConversationId = "room-1",
                ProseQuote = "画具还没收完呢。照片？不给。",
                PlannedSegmentCount = 1,
                Binding = NarrativeProseBinder.Bind(Prose, "画具还没收完呢。照片？不给。"),
            },
        ],
        Intents =
        [
            new NarrativeIntent
            {
                IntentId = "intent-1",
                InstanceId = Alpha,
                Topic = "把画具收完",
                Status = NarrativeIntentStatus.Pending,
                SourceCommitId = "batch-1",
                EarliestDueAtUtc = Now.AddMinutes(20),
            },
        ],
        ProcessedThrough = new ProcessedThroughSeq(102),
        PerceivedInputRefs = new PerceivedInputRefs
        {
            Refs = [new InputRef(102, "msg-102", "room-1")],
        },
        ExpectedStoryVersion = 0,
        OwnershipEpoch = 1,
        ContractVersion = NarrativeContractVersions.NarrativeDocument,
        ContentHash = "sha256:base",
        CommittedByWriter = "host-a",
        CommittedAtUtc = Now,
        ProposalId = "proposal-1",
    };
}
