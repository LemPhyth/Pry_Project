namespace Pry.Core.Narrative;

/// <summary>一条契约拒绝：原因 + 给人看的说明。判定只认 <see cref="Reason"/>。</summary>
public sealed record ContractRejection(NarrativeContractReason Reason, string Detail);

/// <summary>
/// 叙事内容的公共视图。草稿与提交共享同一套语义判定，所以两者都先投影成它，
/// 避免"草稿放行、提交另有一套规则"这种二次真相。
/// </summary>
public sealed record NarrativeContent(
    CharacterInstanceId InstanceId,
    string ConversationId,
    string BatchId,
    string NarrativeProse,
    ProcessedThroughSeq ProcessedThrough,
    PerceivedInputRefs PerceivedInputRefs,
    IReadOnlyList<CommunicationAction> Actions,
    IReadOnlyList<NarrativeIntent> Intents,
    IReadOnlyList<KnowledgeCandidate> KnowledgeCandidates,
    long StoryVersionBase,
    long OwnershipEpoch,
    long DeletionEpoch)
{
    public static NarrativeContent From(NarrativeDraft draft) => new(draft.InstanceId, draft.ConversationId,
        draft.BatchId, draft.NarrativeProse, draft.ProcessedThrough, draft.PerceivedInputRefs, draft.Actions,
        draft.Intents, draft.KnowledgeCandidates, draft.StoryVersionBase, draft.OwnershipEpoch, draft.DeletionEpoch);

    public static NarrativeContent From(NarrativeCommit commit) => new(commit.InstanceId, commit.ConversationId,
        commit.BatchId, commit.NarrativeProse, commit.ProcessedThrough, commit.PerceivedInputRefs, commit.Actions,
        commit.Intents, commit.KnowledgeCandidates, commit.ExpectedStoryVersion, commit.OwnershipEpoch,
        commit.DeletionEpoch);
}

/// <summary>
/// 即时正文与叙事原文的绑定（v4 §7.1）。模型只给字面引用，位置由宿主算：
/// 零次出现即缺失，多次出现即歧义，两者都拒绝该动作，绝不代角色补一句话。
/// </summary>
public static class NarrativeProseBinder
{
    public static ProseBinding Bind(string? narrativeProse, string? quotedText)
    {
        if (string.IsNullOrEmpty(quotedText) || string.IsNullOrEmpty(narrativeProse))
        {
            return new ProseBinding
            {
                QuotedText = quotedText ?? string.Empty,
                Failure = NarrativeContractReason.ActionBindingMissing,
            };
        }

        var occurrences = 0;
        var first = -1;
        var start = 0;
        while (true)
        {
            var index = narrativeProse.IndexOf(quotedText, start, StringComparison.Ordinal);
            if (index < 0)
            {
                break;
            }

            occurrences++;
            if (first < 0)
            {
                first = index;
            }

            start = index + quotedText.Length;
        }

        if (occurrences == 0)
        {
            return new ProseBinding { QuotedText = quotedText, Failure = NarrativeContractReason.ActionBindingMissing };
        }

        if (occurrences > 1)
        {
            return new ProseBinding { QuotedText = quotedText, Failure = NarrativeContractReason.ActionBindingAmbiguous };
        }

        return new ProseBinding
        {
            QuotedText = quotedText,
            ResolvedOffset = first,
            Failure = NarrativeContractReason.None,
        };
    }

    /// <summary>把绑定结果写回动作；只有绑定成功的即时动作才允许进入投递。</summary>
    public static CommunicationAction Apply(CommunicationAction action, string? narrativeProse)
    {
        if (action.Mode != CommunicationMode.Immediate)
        {
            return action;
        }

        return action with { Binding = Bind(narrativeProse, action.ProseQuote) };
    }
}

/// <summary>
/// 草稿与提交的共同语义校验（HDS-002）。返回全部拒绝而不是第一个，是为了让宿主把"这一轮为什么
/// 不被认可"一次说完；小模型修复时不必逐条试。
/// </summary>
public static class NarrativeDraftValidator
{
    public static IReadOnlyList<ContractRejection> Validate(NarrativeContent content, NarrativeRequest request)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(request);
        var rejections = new List<ContractRejection>();

        if (!content.InstanceId.Equals(request.InstanceId))
        {
            rejections.Add(new ContractRejection(NarrativeContractReason.DraftIdentityMismatch,
                $"提案声明的实例 {content.InstanceId.Value} 不是宿主签发的 {request.InstanceId.Value}。"));
        }

        if (!string.Equals(content.ConversationId, request.ConversationId, StringComparison.Ordinal))
        {
            rejections.Add(new ContractRejection(NarrativeContractReason.ScopeEscalation,
                "提案把目标会话换成了请求之外的会话：作用域只能由宿主决定。"));
        }

        if (!string.Equals(content.BatchId, request.BatchId, StringComparison.Ordinal))
        {
            rejections.Add(new ContractRejection(NarrativeContractReason.ScopeEscalation,
                "提案引用的批次与宿主签发的批次不一致。"));
        }

        if (string.IsNullOrWhiteSpace(content.NarrativeProse))
        {
            rejections.Add(new ContractRejection(NarrativeContractReason.NarrativeProseMissing,
                "没有叙事原文就没有可绑定的处境；本轮提案不予认可。"));
        }

        CheckConsumption(content, request, rejections);

        if (content.Actions.Count == 0)
        {
            rejections.Add(new ContractRejection(NarrativeContractReason.ActionBindingMissing,
                "一次主叙事必须共同给出通信决定；沉默也要显式表达成一条 Silence 动作。"));
        }

        foreach (var action in content.Actions)
        {
            CheckAction(action, content, request, rejections);
        }

        foreach (var intent in content.Intents)
        {
            rejections.AddRange(NarrativeIntentPolicy.Validate(intent, content));
        }

        foreach (var candidate in content.KnowledgeCandidates)
        {
            rejections.AddRange(KnowledgeCandidatePolicy.Validate(candidate, content));
        }

        return rejections;
    }

    private static void CheckConsumption(NarrativeContent content, NarrativeRequest request,
        List<ContractRejection> rejections)
    {
        var perceivedReason = content.PerceivedInputRefs.CheckSubsetOf(request.InputSeqs.ToArray());
        if (perceivedReason != NarrativeContractReason.None)
        {
            rejections.Add(new ContractRejection(perceivedReason,
                "感知引用指向了本批次之外的输入序号：角色不能自己造出没收到过的话。"));
        }

        if (!content.ProcessedThrough.WithinBatch(request.BatchHighestSeq))
        {
            rejections.Add(new ContractRejection(NarrativeContractReason.ProcessedBeyondBatch,
                $"消费水位 {content.ProcessedThrough.Seq} 越过批次上界 {request.BatchHighestSeq}。"));
        }

        if (content.ProcessedThrough.Seq < request.ProcessedThroughAtIssue.Seq)
        {
            rejections.Add(new ContractRejection(NarrativeContractReason.WatermarkNotAdvancing,
                "消费水位低于签发时的基线：这会撤销宿主已经确认处理过的输入。"));
        }
    }

    private static void CheckAction(CommunicationAction action, NarrativeContent content, NarrativeRequest request,
        List<ContractRejection> rejections)
    {
        NarrativeGuard.RequireText(action.ActionId, nameof(action.ActionId), NarrativeContractReason.ActionTargetMismatch);

        if (!string.Equals(action.TargetConversationId, request.ConversationId, StringComparison.Ordinal))
        {
            rejections.Add(new ContractRejection(NarrativeContractReason.ActionTargetMismatch,
                $"动作 {action.ActionId} 指向会话 {action.TargetConversationId}，与宿主签发的会话不同。"));
        }

        switch (action.Mode)
        {
            case CommunicationMode.Immediate:
                if (action.PlannedSegmentCount < 1 || action.PlannedSegmentCount > request.MaxOutboundSegments)
                {
                    rejections.Add(new ContractRejection(NarrativeContractReason.ActionSegmentCountInvalid,
                        $"即时动作 {action.ActionId} 的段数 {action.PlannedSegmentCount} 超出宿主上限 "
                        + $"{request.MaxOutboundSegments}（下限 1）。"));
                }

                var binding = action.Binding ?? NarrativeProseBinder.Bind(content.NarrativeProse, action.ProseQuote);
                if (!binding.Bound)
                {
                    rejections.Add(new ContractRejection(binding.Failure,
                        $"即时动作 {action.ActionId} 的正文没能在叙事原文中唯一定位；该动作被拒绝，不由宿主代写。"));
                }

                if (action.DeferredIntentId is not null)
                {
                    rejections.Add(new ContractRejection(NarrativeContractReason.IntentClaimedAsFact,
                        $"即时动作 {action.ActionId} 同时挂了未来意图：已发生与待发生必须分开提交。"));
                }

                break;

            case CommunicationMode.Deferred:
                if (action.PlannedSegmentCount != 0)
                {
                    rejections.Add(new ContractRejection(NarrativeContractReason.SegmentCountExceeded,
                        $"延后动作 {action.ActionId} 不能预留投递段：它还没发生。"));
                }

                if (action.ProseQuote is not null)
                {
                    rejections.Add(new ContractRejection(NarrativeContractReason.ActionBindingMissing,
                        $"延后动作 {action.ActionId} 不该带即时正文引用。"));
                }

                if (action.DeferredIntentId is null)
                {
                    rejections.Add(new ContractRejection(NarrativeContractReason.IntentClaimedAsFact,
                        $"延后动作 {action.ActionId} 没有指向任何意图，等于一份无法兑现的承诺。"));
                }
                else if (!content.Intents.Any(i => string.Equals(i.IntentId, action.DeferredIntentId, StringComparison.Ordinal)))
                {
                    rejections.Add(new ContractRejection(NarrativeContractReason.IntentClaimedAsFact,
                        $"延后动作 {action.ActionId} 指向的意图 {action.DeferredIntentId} 不在本次提案里。"));
                }

                break;

            default:
                if (action.PlannedSegmentCount != 0 || action.ProseQuote is not null)
                {
                    rejections.Add(new ContractRejection(NarrativeContractReason.SegmentCountExceeded,
                        $"沉默动作 {action.ActionId} 不能携带正文或预留段：沉默是合法无外发，不是待发文本。"));
                }

                break;
        }
    }
}

/// <summary>意图的政策：有来源、有期限、永远不是"已经发生"。</summary>
public static class NarrativeIntentPolicy
{
    public static IReadOnlyList<ContractRejection> Validate(NarrativeIntent intent, NarrativeContent content)
    {
        var rejections = new List<ContractRejection>();
        NarrativeGuard.RequireText(intent.IntentId, nameof(intent.IntentId), NarrativeContractReason.IntentClaimedAsFact);
        NarrativeGuard.RequireText(intent.Topic, nameof(intent.Topic), NarrativeContractReason.IntentClaimedAsFact);

        if (!intent.InstanceId.Equals(content.InstanceId))
        {
            rejections.Add(new ContractRejection(NarrativeContractReason.ScopeEscalation,
                $"意图 {intent.IntentId} 写到了别的实例作用域。"));
        }

        if (string.IsNullOrWhiteSpace(intent.SourceCommitId))
        {
            rejections.Add(new ContractRejection(NarrativeContractReason.IntentClaimedAsFact,
                $"意图 {intent.IntentId} 没有来源凭据，不能登记。"));
        }

        if (intent.EarliestDueAtUtc is { } earliest && intent.LatestDueAtUtc is { } latest && latest < earliest)
        {
            rejections.Add(new ContractRejection(NarrativeContractReason.IntentClaimedAsFact,
                $"意图 {intent.IntentId} 的最晚时间早于最早时间。"));
        }

        if (intent.Status is NarrativeIntentStatus.Fulfilled && content.Actions.All(a =>
                a.Mode != CommunicationMode.Immediate || !string.Equals(a.DeferredIntentId, intent.IntentId, StringComparison.Ordinal)))
        {
            rejections.Add(new ContractRejection(NarrativeContractReason.RealityContamination,
                $"意图 {intent.IntentId} 在没有任何对应投递动作的提交里被标为已兑现。"));
        }

        return rejections;
    }
}

/// <summary>
/// 认知候选的政策。核心一句话：引用存在不等于内容为真，而"角色想象的"永远不能当成"用户说过的"。
/// </summary>
public static class KnowledgeCandidatePolicy
{
    public static IReadOnlyList<ContractRejection> Validate(KnowledgeCandidate candidate, NarrativeContent content)
    {
        var rejections = new List<ContractRejection>();
        NarrativeGuard.RequireText(candidate.CandidateId, nameof(candidate.CandidateId),
            NarrativeContractReason.ConfirmationWithoutUserEvidence);
        NarrativeGuard.RequireText(candidate.Claim, nameof(candidate.Claim), NarrativeContractReason.RealityContamination);
        NarrativeGuard.RequireText(candidate.ProposedBy, nameof(candidate.ProposedBy),
            NarrativeContractReason.ConfirmationWithoutUserEvidence);

        if (!candidate.InstanceId.Equals(content.InstanceId))
        {
            rejections.Add(new ContractRejection(NarrativeContractReason.ScopeEscalation,
                $"认知候选 {candidate.CandidateId} 落到了别的实例作用域。"));
        }

        if (candidate.Confidence is < 0 or > 1)
        {
            rejections.Add(new ContractRejection(NarrativeContractReason.RealityContamination,
                $"置信度必须落在 [0,1]，当前是 {candidate.Confidence}。"));
        }

        // 现实污染：叙事原文与模型自评都不是用户现实来源。
        if (candidate.Kind == CognitionKind.UserReportedFact
            && candidate.Origin is CognitionOrigin.NarrativeProse or CognitionOrigin.ModelSelfReport
            or CognitionOrigin.Intent)
        {
            rejections.Add(new ContractRejection(NarrativeContractReason.RealityContamination,
                $"候选 {candidate.CandidateId} 把角色侧材料登记成用户现实事实。"));
        }

        // 已经确认：必须有真实用户证据、宿主观察或显式反馈。
        if (candidate.Kind == CognitionKind.ConfirmedAgreement
            && candidate.Origin is not (CognitionOrigin.UserMessageEvidence or CognitionOrigin.HostObservation
                or CognitionOrigin.ExplicitFeedback))
        {
            rejections.Add(new ContractRejection(NarrativeContractReason.ConfirmationWithoutUserEvidence,
                $"候选 {candidate.CandidateId} 宣称已确认，但来源给不出确认动作。"));
        }

        if (candidate.Kind == CognitionKind.ConfirmedAgreement && candidate.EvidenceRefs.Count == 0)
        {
            rejections.Add(new ContractRejection(NarrativeContractReason.ConfirmationWithoutUserEvidence,
                $"候选 {candidate.CandidateId} 没有任何证据引用却标为已确认。"));
        }

        // 提议与未来意图不能按已兑现的事实召回。
        if (candidate.Kind == CognitionKind.Proposal
            && candidate.Origin is (CognitionOrigin.UserMessageEvidence or CognitionOrigin.HostObservation
                or CognitionOrigin.ExplicitFeedback) && candidate.Confidence > .7)
        {
            rejections.Add(new ContractRejection(NarrativeContractReason.IntentClaimedAsFact,
                $"候选 {candidate.CandidateId} 是尚未发生的提议，不该带高置信的现实来源。"));
        }

        // 角色虚构经历只能留在叙事域。
        if (candidate.Kind == CognitionKind.CharacterFiction
            && candidate.Origin is CognitionOrigin.UserMessageEvidence or CognitionOrigin.HostObservation)
        {
            rejections.Add(new ContractRejection(NarrativeContractReason.RealityContamination,
                $"候选 {candidate.CandidateId} 用现实来源支撑虚构经历。"));
        }

        // 叙事原文本身永远是导航材料：它既不能支撑现实断言，也不能被当成用户认可。
        if (candidate.Kind == CognitionKind.NarrativeProse
            && candidate.Origin is not (CognitionOrigin.NarrativeProse or CognitionOrigin.ModelSelfReport))
        {
            rejections.Add(new ContractRejection(NarrativeContractReason.RealityContamination,
                $"候选 {candidate.CandidateId} 把叙事原文当作现实来源的断言材料。"));
        }

        return rejections;
    }
}

/// <summary>
/// 提交侧的附加校验：草稿允许的"引用批次作为来源"在进入提交时必须收敛到真实身份，
/// 同时补上契约版本、内容散列与提交标识这三项宿主责任。
/// </summary>
public static class NarrativeCommitValidator
{
    public static IReadOnlyList<ContractRejection> Validate(NarrativeCommit commit, NarrativeRequest request)
    {
        var rejections = new List<ContractRejection>(NarrativeDraftValidator.Validate(NarrativeContent.From(commit), request));

        NarrativeGuard.RequireText(commit.CommitId, nameof(commit.CommitId), NarrativeContractReason.ContentHashMissing);

        if (!NarrativeContractVersions.IsSupportedNarrativeVersion(commit.ContractVersion))
        {
            rejections.Add(new ContractRejection(NarrativeContractReason.ContractVersionUnsupported,
                $"提交携带不支持的叙事契约版本 {commit.ContractVersion}。"));
        }

        if (string.IsNullOrWhiteSpace(commit.ContentHash))
        {
            rejections.Add(new ContractRejection(NarrativeContractReason.ContentHashMissing,
                "没有宿主计算的内容散列，提交就无法在恢复时证明自己没被改写过。"));
        }

        foreach (var intent in commit.Intents)
        {
            if (string.IsNullOrWhiteSpace(intent.SourceCommitId))
            {
                rejections.Add(new ContractRejection(NarrativeContractReason.IntentClaimedAsFact,
                    $"意图 {intent.IntentId} 进入提交时仍没有来源凭据。"));
                continue;
            }

            if (!string.Equals(intent.SourceCommitId, commit.CommitId, StringComparison.Ordinal)
                && !string.Equals(intent.SourceCommitId, commit.BatchId, StringComparison.Ordinal))
            {
                rejections.Add(new ContractRejection(NarrativeContractReason.IntentClaimedAsFact,
                    $"意图 {intent.IntentId} 的来源 {intent.SourceCommitId} 既不是本次提交也不是本次批次。"));
            }
        }

        return rejections;
    }
}
