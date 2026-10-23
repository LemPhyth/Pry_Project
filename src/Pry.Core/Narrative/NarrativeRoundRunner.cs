using System.Security.Cryptography;
using System.Text;
using Pry.Core.Memory;

namespace Pry.Core.Narrative;

/// <summary>
/// 一轮的结果形状。前四类都是"本轮没有产出可提交的叙事"，后三类是合法产出：
/// <see cref="Silence"/> 合法无外发、<see cref="Deferred"/> 只登记意图、<see cref="Immediate"/> 才有投递段。
/// </summary>
public enum NarrativeRoundDisposition
{
    ContextRejected = 0,
    ProviderRejected = 1,
    ContractRejected = 2,
    HistoryRejected = 3,
    Silence = 4,
    Deferred = 5,
    Immediate = 6,
}

/// <summary>一轮主叙事的完整结果。判定看 <see cref="Disposition"/> 与各项计数字段，不看消息文本。</summary>
public sealed record NarrativeRoundResult
{
    public required NarrativeRoundDisposition Disposition { get; init; }
    public NarrativeContextAssembly? Assembly { get; init; }
    public NarratorOutcome? Outcome { get; init; }
    public NarrativeDraft? Draft { get; init; }

    /// <summary>本轮模型调用次数。形状判据：一次叙事必须恰好等于 1；被预算挡下时为 0。</summary>
    public required int NarratorCallCount { get; init; }

    public IReadOnlyList<ContractRejection> Rejections { get; init; } = [];
    public IReadOnlyList<CommunicationAction> Actions { get; init; } = [];
    public IReadOnlyList<ContractRejection> HistoryRejections { get; init; } = [];

    /// <summary>延迟分支登记的意图。即时与沉默分支下必须为空。</summary>
    public IReadOnlyList<NarrativeIntent> RegisteredIntents { get; init; } = [];

    /// <summary>本轮真正进入投递账本（＝真实历史形状）的段。沉默与延迟必须为空。</summary>
    public IReadOnlyList<DeliverySegment> HistoryEntries { get; init; } = [];

    public string? Detail { get; init; }

    public int AssistantMessageCount => HistoryEntries.Count;

    /// <summary>本轮的外发决定数量：绑定成功的即时动作条数。</summary>
    public int OutboundActionCount => Actions.Count(a => a.Mode == CommunicationMode.Immediate && a.Binding?.Bound == true);

    public int SilentActionCount => Actions.Count(a => a.Mode == CommunicationMode.Silence);
    public int DeferredActionCount => Actions.Count(a => a.Mode == CommunicationMode.Deferred);
    public bool Accepted => Disposition is NarrativeRoundDisposition.Silence or NarrativeRoundDisposition.Deferred
        or NarrativeRoundDisposition.Immediate;
}

/// <summary>
/// HDS-003 的编排者：装配一次上下文 → 恰好一次 Narrator 调用 → 照用既有校验与绑定 → 按通信模式分岔。
///
/// 三条形状判据（工单 §二.2/§二.3/§二.4）：
/// ① 上下文装不下就不调模型，<see cref="NarrativeRoundResult.NarratorCallCount"/> 为 0；
/// ② 装得下就只调一次，产出的草稿直接进 <see cref="NarrativeDraftValidator"/>，没有第二次独立改写；
/// ③ 沉默不产生助手消息，延迟只登记 <see cref="NarrativeIntent"/>，真实历史一律经既有
///    <see cref="DeliveryLedger"/> 的判定才计入——本类不自己实现"正文是否来自原文"。
///
/// 本类不接生产库、不接 API/Client、不碰真实模型；投递账本与写者身份由调用方注入。
/// </summary>
public sealed class NarrativeRoundRunner
{
    private readonly INarrator _narrator;
    private readonly NarrativeContextCompiler _compiler;
    private readonly DeliveryLedger _history;
    private readonly string _writerId;

    public NarrativeRoundRunner(INarrator narrator, DeliveryLedger history, string writerId,
        ITokenEstimator? estimator = null)
    {
        ArgumentNullException.ThrowIfNull(narrator);
        ArgumentNullException.ThrowIfNull(history);
        NarrativeGuard.RequireText(writerId, nameof(writerId), NarrativeContractReason.OwnershipNotHeld);
        _narrator = narrator;
        _history = history;
        _writerId = writerId;
        _compiler = new NarrativeContextCompiler(estimator);
    }

    /// <summary>
    /// 跑一轮。
    /// <param name="narratorTimeout">本轮调用的硬上限，由宿主传入（本波不新造超时档位常量）。</param>
    /// </summary>
    public async Task<NarrativeRoundResult> RunAsync(NarrativeRequest request, NarrativeContextPayloads payloads,
        TimeSpan narratorTimeout, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(narratorTimeout, TimeSpan.Zero);
        var assembly = _compiler.Compile(request, payloads);
        if (!assembly.Accepted)
        {
            return new NarrativeRoundResult
            {
                Disposition = NarrativeRoundDisposition.ContextRejected, Assembly = assembly,
                NarratorCallCount = 0, Detail = assembly.Detail,
            };
        }

        var budget = new NarratorCallBudget();
        var call = new NarratorCall { Request = request, Assembly = assembly, Budget = budget }.Validate();

        budget.Consume();
        var outcome = await InvokeOnceAsync(call, narratorTimeout, cancellationToken);

        if (outcome.Kind != NarratorOutcomeKind.DraftProduced || outcome.Draft is null)
        {
            // 坏 JSON / 超时 / provider 故障都不许被解释成"角色选择沉默"：本轮零登记、零历史。
            return new NarrativeRoundResult
            {
                Disposition = NarrativeRoundDisposition.ProviderRejected, Assembly = assembly, Outcome = outcome,
                NarratorCallCount = budget.CallCount, Detail = outcome.Detail ?? outcome.Kind.ToString(),
            };
        }

        var draft = outcome.Draft!;
        var snapshot = CheckSnapshotFreshness(draft, request);
        if (snapshot is not null)
        {
            return new NarrativeRoundResult
            {
                Disposition = NarrativeRoundDisposition.ContractRejected, Assembly = assembly, Outcome = outcome,
                Draft = draft, NarratorCallCount = budget.CallCount, Rejections = [snapshot],
                Detail = snapshot.Detail,
            };
        }

        // 原文行动绑定：照用既有 NarrativeProseBinder.Apply（Immediate 才绑，其余原样返回）。
        var boundActions = draft.Actions.Select(action => NarrativeProseBinder.Apply(action, draft.NarrativeProse))
            .ToArray();
        var boundDraft = draft with { Actions = boundActions };
        var rejections = NarrativeDraftValidator.Validate(NarrativeContent.From(boundDraft), request);
        if (rejections.Count > 0)
        {
            return new NarrativeRoundResult
            {
                Disposition = NarrativeRoundDisposition.ContractRejected, Assembly = assembly, Outcome = outcome,
                Draft = boundDraft, Actions = boundActions, NarratorCallCount = budget.CallCount,
                Rejections = rejections, Detail = string.Join("；", rejections.Select(r => r.Detail)),
            };
        }

        var immediate = boundActions.Where(a => a.Mode == CommunicationMode.Immediate).ToArray();
        var deferred = boundActions.Where(a => a.Mode == CommunicationMode.Deferred).ToArray();

        if (immediate.Length > 0)
        {
            return ImmediateBranch(boundDraft, immediate, request, assembly, budget, now);
        }

        if (deferred.Length > 0)
        {
            // 延迟＝只登记意图，不写真实历史：意图到期后才由 HDS-004/007 重新叙事。
            return new NarrativeRoundResult
            {
                Disposition = NarrativeRoundDisposition.Deferred, Assembly = assembly, Outcome = outcome,
                Draft = boundDraft, Actions = boundActions, NarratorCallCount = budget.CallCount,
                RegisteredIntents = boundDraft.Intents,
            };
        }

        // 沉默＝零外发、零助手消息；上下文与叙事原文照样成立（v4 §7.1 第三行）。
        return new NarrativeRoundResult
        {
            Disposition = NarrativeRoundDisposition.Silence, Assembly = assembly, Outcome = outcome,
            Draft = boundDraft, Actions = boundActions, NarratorCallCount = budget.CallCount,
        };
    }

    /// <summary>
    /// 即时分支：把草稿升成宿主提交，交给既有 <see cref="DeliveryLedger"/> 逐段判定。
    /// 段文本按绑定偏移从叙事原文连续切片，拼接必须逐字还原绑定原文——这条尺子是账本自己有的
    /// （<c>DeliveryLedger.cs:143-155</c>），本类只负责切，不负责放行，所以第二次改写一定被拒。
    /// </summary>
    private NarrativeRoundResult ImmediateBranch(NarrativeDraft draft,
        CommunicationAction[] immediate, NarrativeRequest request, NarrativeContextAssembly assembly,
        NarratorCallBudget budget, DateTimeOffset now)
    {
        var commit = BuildCommit(draft, request, now);
        _history.RegisterAppliedCommit(commit, request.MaxOutboundSegments);

        var appended = new List<DeliverySegment>();
        var refusals = new List<ContractRejection>();
        foreach (var action in immediate)
        {
            var binding = action.Binding!;
            var slice = commit.NarrativeProse.Substring(binding.ResolvedOffset!.Value, binding.QuotedText.Length);
            var parts = SplitContiguous(slice, action.PlannedSegmentCount);
            for (var i = 0; i < parts.Count; i++)
            {
                var segment = new DeliverySegment
                {
                    InstanceId = commit.InstanceId, ConversationId = commit.ConversationId,
                    CommitId = commit.CommitId, ActionId = action.ActionId, SegmentIndex = i,
                    State = DeliverySegmentState.Pending, Text = parts[i],
                    OwnershipEpoch = commit.OwnershipEpoch,
                };
                var outcome = _history.TryAppend(segment, _writerId);
                if (outcome.Accepted && outcome.Segment is not null)
                {
                    appended.Add(outcome.Segment);
                }
                else
                {
                    refusals.Add(new ContractRejection(outcome.Reason, outcome.Detail ?? outcome.Reason.ToString()));
                }
            }
        }

        if (refusals.Count > 0)
        {
            return new NarrativeRoundResult
            {
                Disposition = NarrativeRoundDisposition.HistoryRejected, Assembly = assembly, Draft = draft,
                Actions = [.. draft.Actions], NarratorCallCount = budget.CallCount, HistoryRejections = refusals,
                HistoryEntries = appended,
                Detail = string.Join("；", refusals.Select(r => r.Detail)),
            };
        }

        return new NarrativeRoundResult
        {
            Disposition = NarrativeRoundDisposition.Immediate, Assembly = assembly, Draft = draft,
            Actions = [.. draft.Actions], NarratorCallCount = budget.CallCount, HistoryEntries = appended,
        };
    }

    /// <summary>
    /// v4 §7 第 6 步"宿主校验：检查快照仍有效"。这里比三项：故事版本、所有权代次、签发基线。
    /// 任一项对不上都拒本轮，且拒绝路径上一条历史都不写。
    /// </summary>
    private static ContractRejection? CheckSnapshotFreshness(NarrativeDraft draft, NarrativeRequest request)
    {
        if (draft.StoryVersionBase != request.StoryVersion)
        {
            return new ContractRejection(NarrativeContractReason.StoryVersionConflict,
                $"提案建立在故事版本 {draft.StoryVersionBase} 上，宿主签发时已经是 {request.StoryVersion}。");
        }

        if (draft.OwnershipEpoch != request.OwnershipEpoch)
        {
            return new ContractRejection(NarrativeContractReason.OwnershipFenced,
                $"提案带的所有权代次 {draft.OwnershipEpoch} 与宿主请求的 {request.OwnershipEpoch} 不一致：写入权已交接。");
        }

        if (draft.ReceivedThroughSeqAtIssue != request.ProcessedThroughAtIssue.Seq)
        {
            return new ContractRejection(NarrativeContractReason.LateCommitRejected,
                $"提案记的签发基线水位 {draft.ReceivedThroughSeqAtIssue} 与宿主请求的 "
                + $"{request.ProcessedThroughAtIssue.Seq} 对不上：这是迟到快照。");
        }

        return null;
    }

    private NarrativeCommit BuildCommit(NarrativeDraft draft, NarrativeRequest request, DateTimeOffset now)
    {
        var commitId = "commit-" + draft.ProposalId;
        var consumed = request.Inputs
            .Where(i => i.Seq > request.ProcessedThroughAtIssue.Seq && i.Seq <= draft.ProcessedThrough.Seq)
            .Select(i => i.Seq)
            .ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\u241f',
            [commitId, draft.InstanceId.Value, draft.ConversationId, draft.BatchId, draft.NarrativeProse,
                draft.ProcessedThrough.ToString(), string.Join(",", draft.PerceivedInputRefs.Refs.Select(r => r.Seq)),
                string.Join(",", draft.Actions.Select(a => a.ActionId + ":" + (int)a.Mode)),
                string.Join(",", draft.Intents.Select(i => i.IntentId))])))).ToLowerInvariant();

        return new NarrativeCommit
        {
            CommitId = commitId, InstanceId = draft.InstanceId, ConversationId = draft.ConversationId,
            BatchId = draft.BatchId, NarrativeProse = draft.NarrativeProse, Actions = draft.Actions,
            Intents = draft.Intents, KnowledgeCandidates = draft.KnowledgeCandidates,
            ProcessedThrough = draft.ProcessedThrough, PerceivedInputRefs = draft.PerceivedInputRefs,
            ExpectedStoryVersion = draft.StoryVersionBase, DeletionEpoch = request.DeletionEpoch,
            OwnershipEpoch = draft.OwnershipEpoch, ContentHash = hash, CommittedByWriter = _writerId,
            CommittedAtUtc = now, ProposalId = draft.ProposalId, ConsumedInputSeqs = consumed,
        };
    }

    /// <summary>把绑定原文连续切成 N 段（前若干段各多拿一个字符），拼接后逐字等于原切片。</summary>
    internal static IReadOnlyList<string> SplitContiguous(string slice, int parts)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(parts, 1);
        if (parts == 1) return [slice];

        var baseLength = slice.Length / parts;
        var remainder = slice.Length % parts;
        var result = new List<string>(parts);
        var cursor = 0;
        for (var i = 0; i < parts; i++)
        {
            var take = baseLength + (i < remainder ? 1 : 0);
            result.Add(slice.Substring(cursor, take));
            cursor += take;
        }

        return result;
    }

    private async Task<NarratorOutcome> InvokeOnceAsync(NarratorCall call, TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        // 两条界一起给：① 到点取消 token，让守规矩的 provider 自己停下来；② 硬上限竞速，
        // 让无视取消的 provider 也只能以超时收场。两条界都只产出"超时"这一种明确形状。
        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
        var callTask = _narrator.ProposeAsync(call, linked.Token);
        var finished = await Task.WhenAny(callTask, Task.Delay(timeout, CancellationToken.None));

        if (finished != callTask)
        {
            // 观测后台任务的异常，避免"未观测任务异常"最终把进程拖崩；这不是把失败改成成功。
            _ = callTask.ContinueWith(t => _ = t.Exception, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            return NarratorOutcome.Timeout($"一次主叙事调用超过 {timeout.TotalMilliseconds} ms 未返回，本轮按超时退出。");
        }

        try
        {
            return await callTask;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // 宿主真的取消了（用户停止、进程退出）必须往上抛，不许伪装成任何一轮结果形状。
        }
        catch (OperationCanceledException)
        {
            return NarratorOutcome.Timeout($"provider 在 {timeout.TotalMilliseconds} ms 的超时窗口内响应了取消。");
        }
        catch (Exception error)
        {
            return NarratorOutcome.Failed($"provider 抛出 {error.GetType().Name}：{error.Message}");
        }
    }
}
