using Pry.Core.Narrative;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// HDS-002 反例①族：稳定身份的三条禁令。
/// 每条判据都写成"这条路径必须被挡下"，而不是"这个方法返回了什么"。
/// </summary>
public sealed class NarrativeIdentityContractTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 8, 0, 0, TimeSpan.Zero);

    private static readonly CharacterInstanceKey PryKey = new()
    {
        LocalUserId = "local-user-1",
        CharacterId = "pry",
    };

    private static readonly CharacterInstanceId Alpha = CharacterInstanceId.Create("ci-instance-alpha");
    private static readonly CharacterInstanceId Beta = CharacterInstanceId.Create("ci-instance-beta");

    [Fact]
    public void Restart_proposing_a_different_identity_never_rebuilds_the_instance()
    {
        var firstDay = new CharacterInstanceRegistry();
        var bound = firstDay.Resolve(PryKey, Alpha);
        Assert.True(bound.Created);
        Assert.Equal(Alpha.Value, bound.InstanceId.Value);

        // 快照回放 = 进程重启后从登记行恢复；再次解析时哪怕换了提案身份，也必须拿回同一个实例。
        var nextDay = new CharacterInstanceRegistry(firstDay.Registrations, [], [], firstDay.MappingContractVersion);
        var again = nextDay.Resolve(PryKey, Beta);

        Assert.False(again.Created);
        Assert.Equal(Alpha.Value, again.InstanceId.Value);
        Assert.True(again.ProposalSuperseded);
        Assert.Equal(NarrativeContractReason.IdentityProposalSuperseded, again.Reason);
        Assert.Single(nextDay.Registrations);
    }

    [Fact]
    public void First_binding_without_host_identity_is_refused_instead_of_inventing_one()
    {
        var registry = new CharacterInstanceRegistry();
        var error = Assert.Throws<NarrativeContractException>(() => registry.Resolve(PryKey));
        Assert.Equal(NarrativeContractReason.IdentityValueBlank, error.Reason);
        Assert.Empty(registry.Registrations);
    }

    [Theory]
    [InlineData("pry")]
    [InlineData("pry-")]
    [InlineData("PRY")]
    [InlineData("pry@")]
    [InlineData("ci:pry")]
    public void Identity_must_be_a_host_issued_stable_token(string candidate)
    {
        var error = Assert.Throws<NarrativeContractException>(() => CharacterInstanceId.Create(candidate));
        Assert.True(
            error.Reason is NarrativeContractReason.IdentityValueBlank
                or NarrativeContractReason.IdentityValueMalformed
                or NarrativeContractReason.IdentityValueTooLong,
            $"意外的拒绝原因：{error.Reason}");
    }

    [Fact]
    public void Renaming_the_character_id_is_not_a_migration()
    {
        var registry = new CharacterInstanceRegistry();
        var key = new CharacterInstanceKey { LocalUserId = "local-user-1", CharacterId = "pry-chan" };
        registry.Resolve(key, CharacterInstanceId.Create("ci-instance-alpha"));

        // "pry_chan" 只是 pry-chan 的另一种写法：没有 LocalUser 维度，换它等于没有迁移。
        var outcome = registry.MigrateIdentity(
            new IdentityMigrationRequest(key, CharacterInstanceId.Create("pry_chan")),
            new IdentityMigrationOptions { CarryEvidenceReferences = true, Reason = "改名" });

        Assert.False(outcome.Accepted);
        Assert.Equal(NarrativeContractReason.IdentityRenameIsNotAMigration, outcome.Reason);
        Assert.Equal("ci-instance-alpha", registry.Resolve(key).InstanceId.Value);
    }

    [Fact]
    public void Reissuing_identity_without_carrying_evidence_is_refused()
    {
        var registry = new CharacterInstanceRegistry();
        var resolution = registry.Resolve(PryKey, Alpha);
        registry.RecordEvidenceReference(resolution.InstanceId, "memory:1024");
        registry.RecordEvidenceReference(resolution.InstanceId, "narrative:commit-7");

        var rejected = registry.MigrateIdentity(
            new IdentityMigrationRequest(PryKey, Beta),
            new IdentityMigrationOptions { CarryEvidenceReferences = false });

        Assert.False(rejected.Accepted);
        Assert.Equal(NarrativeContractReason.IdentityReissueOrphansEvidence, rejected.Reason);
        Assert.Equal(Alpha.Value, registry.OwnerOfEvidence("memory:1024")!.Value);
        Assert.Equal(2, registry.EvidenceReferenceCount(Alpha));

        var accepted = registry.MigrateIdentity(
            new IdentityMigrationRequest(PryKey, Beta) { RetainLegacyIdentityAsAlias = true },
            new IdentityMigrationOptions { CarryEvidenceReferences = true, Reason = "实例作用域收敛" });

        Assert.True(accepted.Accepted);
        Assert.Equal(0, registry.EvidenceReferenceCount(Alpha));
        Assert.Equal(2, registry.EvidenceReferenceCount(Beta));
        // 旧 ID 仍能解析回当前实例：按旧 ID 写下的历史引用不会失联。
        Assert.Equal(Beta.Value, registry.ResolveAlias(Alpha.Value)!.Value);
        Assert.Equal(Beta.Value, registry.OwnerOfEvidence("narrative:commit-7")!.Value);
    }

    [Fact]
    public void Legacy_character_scope_alias_resolves_to_the_stable_instance_once()
    {
        var registry = new CharacterInstanceRegistry();
        registry.Resolve(PryKey, Alpha);

        // 绑定之前不猜：别名解析不出来就必须走显式迁移，不能凭空指回某个实例。
        Assert.Null(registry.ResolveAlias("character:pry"));
        Assert.True(registry.BindLegacyScope("character:pry", PryKey).Accepted);
        Assert.Equal(Alpha.Value, registry.ResolveAlias("character:pry")!.Value);
        // 重复绑定幂等，不产生第二个入口。
        Assert.True(registry.BindLegacyScope("character:pry", PryKey).Accepted);

        // 同一条旧作用域不能同时属于两个实例。
        var otherKey = new CharacterInstanceKey { LocalUserId = "local-user-2", CharacterId = "pry" };
        registry.Resolve(otherKey, Beta);
        var conflict = registry.BindLegacyScope("character:pry", otherKey);

        Assert.False(conflict.Accepted);
        Assert.Equal(NarrativeContractReason.AliasConflict, conflict.Reason);
        Assert.Equal(Alpha.Value, registry.ResolveAlias("character:pry")!.Value);
    }

    [Fact]
    public void Unregistered_alias_is_not_guessed()
    {
        var registry = new CharacterInstanceRegistry();
        Assert.Null(registry.ResolveAlias("character:never-seen"));
    }

    [Fact]
    public void Batch_reissue_that_would_orphan_any_instance_is_rejected_whole()
    {
        var registry = new CharacterInstanceRegistry();
        var alphaKey = new CharacterInstanceKey { LocalUserId = "local-user-1", CharacterId = "pry" };
        var betaKey = new CharacterInstanceKey { LocalUserId = "local-user-1", CharacterId = "rin" };
        var alphaId = registry.Resolve(alphaKey, CharacterInstanceId.Create("ci-old-pry")).InstanceId;
        var betaId = registry.Resolve(betaKey, CharacterInstanceId.Create("ci-old-rin")).InstanceId;
        registry.RecordEvidenceReference(alphaId, "memory:1");
        // 第二条没有证据，但同批里有一条会被拒 ⇒ 整批不落。
        registry.RecordEvidenceReference(betaId, "memory:2");

        var outcomes = registry.MigrateIdentities(
        [
            new IdentityMigrationRequest(alphaKey, CharacterInstanceId.Create("ci-new-pry")),
            new IdentityMigrationRequest(betaKey, CharacterInstanceId.Create("ci-new-rin")),
        ],
        new IdentityMigrationOptions { CarryEvidenceReferences = false });

        Assert.All(outcomes, outcome => Assert.Equal(NarrativeContractReason.IdentityBatchRejected, outcome.Reason));
        Assert.Equal(2, registry.Registrations.Count);
        Assert.Equal(alphaId.Value, registry.Resolve(alphaKey).InstanceId.Value);
        Assert.Equal(betaId.Value, registry.Resolve(betaKey).InstanceId.Value);
        Assert.Equal(1, registry.EvidenceReferenceCount(alphaId));
        Assert.Equal(1, registry.EvidenceReferenceCount(betaId));
    }

    [Fact]
    public void Batch_reissue_with_evidence_carried_applies_every_member()
    {
        var registry = new CharacterInstanceRegistry();
        var alphaKey = new CharacterInstanceKey { LocalUserId = "local-user-1", CharacterId = "pry" };
        var alphaId = registry.Resolve(alphaKey, CharacterInstanceId.Create("ci-old-pry")).InstanceId;
        registry.RecordEvidenceReference(alphaId, "memory:1");

        var outcomes = registry.MigrateIdentities(
            [new IdentityMigrationRequest(alphaKey, CharacterInstanceId.Create("ci-new-pry"))],
            new IdentityMigrationOptions { CarryEvidenceReferences = true, Reason = "作用域收敛" });

        Assert.Single(outcomes);
        Assert.True(outcomes[0].Accepted);
        Assert.Equal("ci-new-pry", registry.OwnerOfEvidence("memory:1")!.Value);
    }

    [Fact]
    public void Instance_key_requires_both_user_and_character()
    {
        Assert.Throws<NarrativeContractException>(() => new CharacterInstanceKey
        {
            LocalUserId = "",
            CharacterId = "pry",
        }.Validate());
        var error = Assert.Throws<NarrativeContractException>(() => new CharacterInstanceKey
        {
            LocalUserId = "local-user-1",
            CharacterId = " ",
        }.Validate());
        Assert.Equal(NarrativeContractReason.InstanceKeyIncomplete, error.Reason);
    }

    [Fact]
    public void Draft_that_picks_its_own_instance_or_conversation_is_not_a_proposal()
    {
        var (request, draft) = Fixture();
        var content = NarrativeContent.From(draft with
        {
            InstanceId = Beta,
            ConversationId = "room-of-someone-else",
        });

        var rejections = NarrativeDraftValidator.Validate(content, request);

        Assert.Contains(rejections, r => r.Reason == NarrativeContractReason.DraftIdentityMismatch);
        Assert.Contains(rejections, r => r.Reason == NarrativeContractReason.ScopeEscalation);
    }

    [Fact]
    public void Perceived_refs_must_come_from_the_issued_batch()
    {
        var (request, draft) = Fixture();
        var content = NarrativeContent.From(draft with
        {
            PerceivedInputRefs = new PerceivedInputRefs
            {
                Refs = [new InputRef(999, "msg-999", "room-1")],
            },
        });

        var rejections = NarrativeDraftValidator.Validate(content, request);
        Assert.Contains(rejections, r => r.Reason == NarrativeContractReason.PerceivedInputUnknown);
    }

    [Fact]
    public void Consumption_watermark_may_not_point_beyond_the_batch()
    {
        var (request, draft) = Fixture();
        var content = NarrativeContent.From(draft with { ProcessedThrough = new ProcessedThroughSeq(103) });

        var rejections = NarrativeDraftValidator.Validate(content, request);
        Assert.Contains(rejections, r => r.Reason == NarrativeContractReason.ProcessedBeyondBatch);
    }

    [Fact]
    public void Silence_advances_the_watermark_without_perceiving_anything()
    {
        // 这两个数就是不能混同的证明：水位前进了、感知为空，仍然是合法提交。
        var (request, draft) = Fixture();
        var content = NarrativeContent.From(draft with
        {
            ProcessedThrough = new ProcessedThroughSeq(102),
            PerceivedInputRefs = PerceivedInputRefs.None,
            Actions =
            [
                new CommunicationAction
                {
                    ActionId = "act-silence",
                    Mode = CommunicationMode.Silence,
                    TargetConversationId = "room-1",
                    PlannedSegmentCount = 0,
                },
            ],
        });

        Assert.Empty(NarrativeDraftValidator.Validate(content, request));
        Assert.Equal(102, content.ProcessedThrough.Seq);
        Assert.Empty(content.PerceivedInputRefs.Refs);
    }

    [Theory]
    [InlineData("画具还没收完呢。照片？不给。", NarrativeContractReason.None)]
    [InlineData("照片？", NarrativeContractReason.None)]
    [InlineData("她", NarrativeContractReason.ActionBindingAmbiguous)]
    [InlineData("根本没出现的一句", NarrativeContractReason.ActionBindingMissing)]
    public void Immediate_text_must_locate_itself_in_the_narrative_once(string quote, NarrativeContractReason expected)
    {
        const string Prose = "她把笔放回盒里，看见这条消息，决定拒绝：画具还没收完呢。照片？不给。她";
        var binding = NarrativeProseBinder.Bind(Prose, quote);
        Assert.Equal(expected, binding.Failure);
        Assert.Equal(expected == NarrativeContractReason.None, binding.Bound);
    }

    [Fact]
    public void Immediate_action_with_unlocatable_text_is_refused_not_rewritten()
    {
        var (request, draft) = Fixture();
        var action = draft.Actions[0] with { ProseQuote = "另一段根本不在原文里的话" };
        var content = NarrativeContent.From(draft with
        {
            Actions = [action with { Binding = NarrativeProseBinder.Bind(draft.NarrativeProse, action.ProseQuote) }],
        });

        var rejections = NarrativeDraftValidator.Validate(content, request);
        Assert.Contains(rejections, r => r.Reason == NarrativeContractReason.ActionBindingMissing);
    }

    [Fact]
    public void Deferred_action_without_intent_is_a_promise_nothing_can_keep()
    {
        var (request, draft) = Fixture();
        var content = NarrativeContent.From(draft with
        {
            Actions =
            [
                new CommunicationAction
                {
                    ActionId = "act-deferred",
                    Mode = CommunicationMode.Deferred,
                    TargetConversationId = "room-1",
                    DeferredIntentId = "intent-missing",
                },
            ],
        });

        var rejections = NarrativeDraftValidator.Validate(content, request);
        Assert.Contains(rejections, r => r.Reason == NarrativeContractReason.IntentClaimedAsFact);
    }

    [Theory]
    [InlineData(CognitionKind.UserReportedFact, CognitionOrigin.NarrativeProse, NarrativeContractReason.RealityContamination)]
    [InlineData(CognitionKind.UserReportedFact, CognitionOrigin.ModelSelfReport, NarrativeContractReason.RealityContamination)]
    [InlineData(CognitionKind.ConfirmedAgreement, CognitionOrigin.ModelSelfReport, NarrativeContractReason.ConfirmationWithoutUserEvidence)]
    [InlineData(CognitionKind.CharacterFiction, CognitionOrigin.UserMessageEvidence, NarrativeContractReason.RealityContamination)]
    [InlineData(CognitionKind.UserReportedFact, CognitionOrigin.UserMessageEvidence, NarrativeContractReason.None)]
    [InlineData(CognitionKind.UserReportedFact, CognitionOrigin.HostObservation, NarrativeContractReason.None)]
    [InlineData(CognitionKind.UserReportedFact, CognitionOrigin.ExplicitFeedback, NarrativeContractReason.None)]
    [InlineData(CognitionKind.UserReportedFact, CognitionOrigin.Intent, NarrativeContractReason.RealityContamination)]
    [InlineData(CognitionKind.CharacterFiction, CognitionOrigin.HostObservation, NarrativeContractReason.RealityContamination)]
    [InlineData(CognitionKind.CharacterFiction, CognitionOrigin.ExplicitFeedback, NarrativeContractReason.None)]
    [InlineData(CognitionKind.CharacterFiction, CognitionOrigin.NarrativeProse, NarrativeContractReason.None)]
    [InlineData(CognitionKind.CharacterFiction, CognitionOrigin.ModelSelfReport, NarrativeContractReason.None)]
    [InlineData(CognitionKind.CharacterFiction, CognitionOrigin.Intent, NarrativeContractReason.None)]
    [InlineData(CognitionKind.Proposal, CognitionOrigin.UserMessageEvidence, NarrativeContractReason.IntentClaimedAsFact)]
    [InlineData(CognitionKind.Proposal, CognitionOrigin.HostObservation, NarrativeContractReason.IntentClaimedAsFact)]
    [InlineData(CognitionKind.Proposal, CognitionOrigin.ExplicitFeedback, NarrativeContractReason.IntentClaimedAsFact)]
    [InlineData(CognitionKind.Proposal, CognitionOrigin.NarrativeProse, NarrativeContractReason.None)]
    [InlineData(CognitionKind.Proposal, CognitionOrigin.ModelSelfReport, NarrativeContractReason.None)]
    [InlineData(CognitionKind.Proposal, CognitionOrigin.Intent, NarrativeContractReason.None)]
    [InlineData(CognitionKind.ConfirmedAgreement, CognitionOrigin.UserMessageEvidence, NarrativeContractReason.None)]
    [InlineData(CognitionKind.ConfirmedAgreement, CognitionOrigin.HostObservation, NarrativeContractReason.None)]
    [InlineData(CognitionKind.ConfirmedAgreement, CognitionOrigin.ExplicitFeedback, NarrativeContractReason.None)]
    [InlineData(CognitionKind.ConfirmedAgreement, CognitionOrigin.NarrativeProse, NarrativeContractReason.ConfirmationWithoutUserEvidence)]
    [InlineData(CognitionKind.ConfirmedAgreement, CognitionOrigin.Intent, NarrativeContractReason.ConfirmationWithoutUserEvidence)]
    [InlineData(CognitionKind.NarrativeProse, CognitionOrigin.UserMessageEvidence, NarrativeContractReason.RealityContamination)]
    [InlineData(CognitionKind.NarrativeProse, CognitionOrigin.HostObservation, NarrativeContractReason.RealityContamination)]
    [InlineData(CognitionKind.NarrativeProse, CognitionOrigin.ExplicitFeedback, NarrativeContractReason.RealityContamination)]
    [InlineData(CognitionKind.NarrativeProse, CognitionOrigin.NarrativeProse, NarrativeContractReason.None)]
    [InlineData(CognitionKind.NarrativeProse, CognitionOrigin.ModelSelfReport, NarrativeContractReason.None)]
    [InlineData(CognitionKind.NarrativeProse, CognitionOrigin.Intent, NarrativeContractReason.RealityContamination)]
    public void Knowledge_candidate_origin_decides_what_it_may_assert(CognitionKind kind, CognitionOrigin origin,
        NarrativeContractReason expected)
    {
        var (_, draft) = Fixture();
        var candidate = new KnowledgeCandidate
        {
            CandidateId = "kc-1",
            InstanceId = draft.InstanceId,
            Kind = kind,
            Origin = origin,
            Claim = "她说她要搬家",
            Confidence = .9,
            EvidenceRefs = ["msg-102"],
            ProposedBy = "narrator",
        };

        var rejections = KnowledgeCandidatePolicy.Validate(candidate, NarrativeContent.From(draft));
        Assert.Equal(expected == NarrativeContractReason.None, rejections.Count == 0);
        if (expected != NarrativeContractReason.None)
        {
            Assert.Equal(expected, rejections[0].Reason);
        }
    }

    [Fact]
    public void Confirmed_agreement_without_any_evidence_reference_is_refused()
    {
        var (_, draft) = Fixture();
        var candidate = new KnowledgeCandidate
        {
            CandidateId = "kc-2",
            InstanceId = draft.InstanceId,
            Kind = CognitionKind.ConfirmedAgreement,
            Origin = CognitionOrigin.UserMessageEvidence,
            Claim = "周末一起画画",
            EvidenceRefs = [],
            ProposedBy = "narrator",
        };

        var rejections = KnowledgeCandidatePolicy.Validate(candidate, NarrativeContent.From(draft));
        Assert.Contains(rejections, r => r.Reason == NarrativeContractReason.ConfirmationWithoutUserEvidence);
    }

    [Fact]
    public void Intent_from_another_instance_scope_is_refused()
    {
        var (_, draft) = Fixture();
        var intent = draft.Intents[0] with { InstanceId = Beta };
        var rejections = NarrativeIntentPolicy.Validate(intent, NarrativeContent.From(draft));
        Assert.Contains(rejections, r => r.Reason == NarrativeContractReason.ScopeEscalation);
    }

    [Theory]
    [InlineData(1.5)]
    [InlineData(-0.1)]
    public void Confidence_outside_the_unit_interval_is_refused(double confidence)
    {
        var (_, draft) = Fixture();
        var candidate = new KnowledgeCandidate
        {
            CandidateId = "kc-conf",
            InstanceId = draft.InstanceId,
            Kind = CognitionKind.UserReportedFact,
            Origin = CognitionOrigin.UserMessageEvidence,
            Claim = "她说她要搬家",
            Confidence = confidence,
            EvidenceRefs = ["msg-102"],
            ProposedBy = "narrator",
        };

        var rejections = KnowledgeCandidatePolicy.Validate(candidate, NarrativeContent.From(draft));
        var rejection = Assert.Single(rejections);
        Assert.Equal(NarrativeContractReason.RealityContamination, rejection.Reason);
    }

    [Fact]
    public void Candidate_from_another_instance_scope_is_refused()
    {
        var (_, draft) = Fixture();
        var candidate = new KnowledgeCandidate
        {
            CandidateId = "kc-scope",
            InstanceId = Beta,
            Kind = CognitionKind.UserReportedFact,
            Origin = CognitionOrigin.UserMessageEvidence,
            Claim = "她说她要搬家",
            Confidence = .9,
            EvidenceRefs = ["msg-102"],
            ProposedBy = "narrator",
        };

        var rejections = KnowledgeCandidatePolicy.Validate(candidate, NarrativeContent.From(draft));
        var rejection = Assert.Single(rejections);
        Assert.Equal(NarrativeContractReason.ScopeEscalation, rejection.Reason);
    }

    [Fact]
    public void Intent_without_a_source_commit_is_refused()
    {
        var (_, draft) = Fixture();
        var intent = draft.Intents[0] with { SourceCommitId = "" };
        var rejections = NarrativeIntentPolicy.Validate(intent, NarrativeContent.From(draft));
        var rejection = Assert.Single(rejections);
        Assert.Equal(NarrativeContractReason.IntentClaimedAsFact, rejection.Reason);
    }

    [Fact]
    public void Intent_whose_deadline_precedes_its_earliest_is_refused()
    {
        var (_, draft) = Fixture();
        var intent = draft.Intents[0] with { LatestDueAtUtc = Now.AddMinutes(10) };
        var rejections = NarrativeIntentPolicy.Validate(intent, NarrativeContent.From(draft));
        var rejection = Assert.Single(rejections);
        Assert.Equal(NarrativeContractReason.IntentClaimedAsFact, rejection.Reason);
    }

    [Fact]
    public void Fulfilled_intent_without_a_matching_delivery_is_refused()
    {
        var (_, draft) = Fixture();
        var intent = draft.Intents[0] with { Status = NarrativeIntentStatus.Fulfilled };
        var rejections = NarrativeIntentPolicy.Validate(intent, NarrativeContent.From(draft));
        var rejection = Assert.Single(rejections);
        Assert.Equal(NarrativeContractReason.RealityContamination, rejection.Reason);
    }

    /// <summary>一条合法的即时叙事：处境与话语同源于一段原文，感知只到 102，水位报到批次上界。</summary>
    private static (NarrativeRequest Request, NarrativeDraft Draft) Fixture()
    {
        const string Prose = "她把笔放回盒里，看见这条消息，决定拒绝：画具还没收完呢。照片？不给。";
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
}
