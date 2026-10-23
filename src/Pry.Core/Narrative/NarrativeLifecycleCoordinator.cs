namespace Pry.Core.Narrative;

/// <summary>
/// 叙事作用域键（HDS-004）＝实例 × 会话 × 批次。在飞与已完成的作用域都以它索引：
/// 同一键的第二次请求不再触发第二次生成，而是取回同一份结果（同一提案或同一拒绝理由）；
/// 不同键互不吞并。
/// </summary>
public readonly record struct NarrativeScopeKey(CharacterInstanceId InstanceId, string ConversationId, string BatchId);

/// <summary>
/// 协调层结局：这一枚作用域在生命周期里走到了哪一步。它与 <see cref="NarrativeRoundDisposition"/>
/// 不是一个轴——那个说明"这一轮产出了什么"，这一个说明"这一枚作用域怎么收场"。
/// 三种失败可区分且各自点名：<see cref="FailedByCallBudget"/>（超额度）、
/// <see cref="FailedByProvider"/>（provider 失败或坏响应）、<see cref="FailedByTimeout"/>（超时）。
/// 三者都不是产出：失败绝不映射成 <see cref="CommunicationMode.Silence"/> 这类合法沉默
/// （v4 §14.1 第 3 条——坏响应不能伪装合法沉默）。
/// </summary>
public enum NarrativeScopeDisposition
{
    /// <summary>作用域走完了一次生成；单轮产出形状（含沉默/延迟/即时与各种契约拒绝）在 <see cref="NarrativeScopeResult.Round"/> 里。</summary>
    Completed = 0,
    /// <summary>超额度：同一作用域键在已消耗唯一一次调用额度后再次索要。</summary>
    FailedByCallBudget = 1,
    /// <summary>provider 失败或坏响应。</summary>
    FailedByProvider = 2,
    /// <summary>超时。</summary>
    FailedByTimeout = 3,
    /// <summary>同会话新输入到达，旧草稿被取消。</summary>
    CancelledByNewInput = 4,
    /// <summary>调用方令牌取消。</summary>
    CancelledByHost = 5,
    /// <summary>会话删除：在飞与排队全部作废，不重新入队。</summary>
    InvalidatedByScopeDeletion = 6,
    /// <summary>实例重配置（代次变更）：该实例的在飞与排队全部作废，不重新入队。</summary>
    InvalidatedByReconfigure = 7,
    /// <summary>写入权不足以免提交或分派：拒绝理由复用 <see cref="NarrativeContractReason"/> 既有取值。</summary>
    RejectedByAuthority = 8,
}

/// <summary>
/// 一枚作用域的协调结果。判定看 <see cref="Disposition"/> 与 <see cref="Reason"/> 这两枚数据，
/// 消息文本只给人看。
/// </summary>
public sealed record NarrativeScopeResult(
    NarrativeScopeKey Scope,
    NarrativeScopeDisposition Disposition,
    NarrativeRoundResult? Round,
    NarrativeContractReason Reason,
    string? Detail)
{
    /// <summary>
    /// 合法结局＝作用域走完一次生成，且那一轮本身是合法产出（沉默/延迟/即时）。
    /// 超额度、provider 失败、超时、取消、失效、被拒都不是 accepted。
    /// </summary>
    public bool Accepted => Disposition == NarrativeScopeDisposition.Completed && Round?.Accepted == true;
}

/// <summary>
/// HDS-004 叙事生命周期协调层（隔离实现）。它只管四件事：
/// ① 作用域幂等——同键不二次生成；
/// ② 实例单写者——同一实例同时刻至多一笔在飞，第二笔按规则排队，写入权不足时以既有理由码点名拒绝；
/// ③ 公平调度——一个实例下多会话排队时按"会话登记次序轮转、同会话按入队次序"取队首（规则原文见报告）；
/// ④ 取消与失效——新输入顶掉旧草稿、会话删除与实例重配置作废在飞与排队，
///    且失权判定全部走 <see cref="OutboxOwnershipRegistry"/>／<see cref="DeliveryLedger"/>／
///    <see cref="NarrativeCommitWriter"/> 的既有代次出口：取消一笔在飞时协调器只做"释放＋重新取得"
///    这两次既有登记处调用，代次推进后迟到结果在投递账本自己的代次门上被拒，协调器不另造一套失权判定。
///
/// 一轮＝一次 Narrator 调用额度：额度索要走既有 <see cref="NarratorCallBudget"/> 的强制点
/// （第二次索要即抛、协调器映射成可点名的超额度失败），超时与预算一律从入参取，本类不新造任何数字门槛。
/// 本类不接 API／Client／App、不落生产库、不引用旧主链的任何装配或评分件。
/// </summary>
public sealed class NarrativeLifecycleCoordinator
{
    private enum ScopeStage { Queued, Running, Settled }

    private sealed class ScopeEntry
    {
        public required NarrativeScopeKey Key { get; init; }
        public required NarrativeRequest Request { get; init; }
        public required NarrativeContextPayloads Payloads { get; init; }
        public required TimeSpan NarratorTimeout { get; init; }
        public required DateTimeOffset Now { get; init; }
        public required CancellationTokenSource Link { get; init; }

        /// <summary>这一枚作用域唯一一次调用额度的凭据；第二次索要走既有强制点。</summary>
        public NarratorCallBudget Budget { get; } = new();

        public TaskCompletionSource<NarrativeScopeResult> Handle { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ScopeStage Stage { get; set; }
        public NarrativeScopeDisposition? Termination { get; set; }
        public string? TerminationDetail { get; set; }
        public NarrativeScopeResult? Settled { get; set; }
        public CancellationTokenRegistration Registration { get; set; }
    }

    /// <summary>实例槽：单写者的排队与轮转都发生在这里；一个实例一份，跨实例互不相干。</summary>
    private sealed class InstanceSlot
    {
        public ScopeEntry? Running;
        public readonly List<string> ConversationOrder = [];
        public readonly Dictionary<string, List<ScopeEntry>> Queues = new(StringComparer.Ordinal);
        public int Cursor;
    }

    private readonly NarrativeRoundRunner _runner;
    private readonly OutboxOwnershipRegistry _ownership;
    private readonly NarrativeCommitWriter _commitWriter;
    private readonly string _writerId;
    private readonly object _gate = new();
    private readonly Dictionary<NarrativeScopeKey, ScopeEntry> _scopes = [];
    private readonly Dictionary<CharacterInstanceId, InstanceSlot> _slots = [];

    public NarrativeLifecycleCoordinator(NarrativeRoundRunner runner, OutboxOwnershipRegistry ownership,
        NarrativeCommitWriter commitWriter, string writerId)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(commitWriter);
        NarrativeGuard.RequireText(writerId, nameof(writerId), NarrativeContractReason.OwnershipNotHeld);
        _runner = runner;
        _ownership = ownership;
        _commitWriter = commitWriter;
        _writerId = writerId;
    }

    /// <summary>
    /// 提交一枚作用域。返回值是这一枚作用域的最终结果任务：
    /// 同一作用域键的第二次请求取回同一任务（不触发第二次生成）；取消与失效也经由它点名收场。
    /// </summary>
    public Task<NarrativeScopeResult> SubmitAsync(NarrativeRequest request, NarrativeContextPayloads payloads,
        TimeSpan narratorTimeout, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(payloads);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(narratorTimeout, TimeSpan.Zero);
        request.Validate();

        lock (_gate)
        {
            var key = KeyOf(request);
            if (_scopes.TryGetValue(key, out var known))
                return ResubmitLocked(known, now);

            if (!_ownership.IsAuthorizedWriter(request.InstanceId, _writerId, request.OwnershipEpoch,
                    out var authorityReason, out _))
            {
                return Task.FromResult(new NarrativeScopeResult(key, NarrativeScopeDisposition.RejectedByAuthority,
                    null, authorityReason,
                    $"宿主签发的请求在进队之前就被拒：写者 {_writerId} 不持有实例 {request.InstanceId.Value} "
                    + $"按代次 {request.OwnershipEpoch} 的当前写入权（理由码 {authorityReason}）。"));
            }

            var slot = SlotLocked(key.InstanceId);
            if (slot.Running is { } busy && busy.Key.ConversationId == key.ConversationId
                && busy.Key.BatchId != key.BatchId)
            {
                ForceTerminateLocked(busy, NarrativeScopeDisposition.CancelledByNewInput, now,
                    "同会话新输入到达：在飞草稿取消、所有权代次推进，无视取消的迟到结果永久无资格。");
            }

            var entry = new ScopeEntry
            {
                Key = key, Request = request, Payloads = payloads, NarratorTimeout = narratorTimeout, Now = now,
                Link = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken),
            };
            entry.Registration = entry.Link.Token.Register(() =>
            {
                lock (_gate)
                {
                    if (entry is { Stage: ScopeStage.Queued or ScopeStage.Running, Termination: null })
                    {
                        ForceTerminateLocked(entry, NarrativeScopeDisposition.CancelledByHost, entry.Now,
                            "调用方令牌取消：草稿作废、所有权代次推进，迟到结果永久无资格。");
                    }
                }
            });
            _scopes[key] = entry;
            EnqueueLocked(slot, entry);

            if (slot.Running is null)
                DispatchNextLocked(slot);
            return entry.Handle.Task;
        }
    }

    /// <summary>会话删除（或清空）：既有删除 epoch 出口先记代次，再作废该会话的在飞与排队，不重新入队。</summary>
    public void InvalidateConversation(string conversationId, long deletionEpoch, DateTimeOffset now)
    {
        NarrativeGuard.RequireText(conversationId, nameof(conversationId),
            NarrativeContractReason.ConversationInvalidated);
        ArgumentOutOfRangeException.ThrowIfNegative(deletionEpoch);
        lock (_gate)
        {
            _commitWriter.InvalidateConversation(conversationId, deletionEpoch);
            var victims = _scopes.Values
                .Where(e => string.Equals(e.Key.ConversationId, conversationId, StringComparison.Ordinal)
                            && e.Stage != ScopeStage.Settled)
                .ToArray();
            // 先作废排队项再取消在飞项：取消的续延链可能就地推进结算与再分派，
            // 此刻队列里不能留下任何本应作废的作用域。
            foreach (var entry in victims.Where(e => e.Stage == ScopeStage.Queued))
                ForceTerminateLocked(entry, NarrativeScopeDisposition.InvalidatedByScopeDeletion, now,
                    $"会话 {conversationId} 被删除（删除 epoch 升到 {deletionEpoch}）：该作用域的在飞与排队全部作废，"
                    + "迟到结果不得复活作用域，也不重新入队。");
            foreach (var entry in victims.Where(e => e.Stage == ScopeStage.Running))
                ForceTerminateLocked(entry, NarrativeScopeDisposition.InvalidatedByScopeDeletion, now,
                    $"会话 {conversationId} 被删除（删除 epoch 升到 {deletionEpoch}）：该作用域的在飞与排队全部作废，"
                    + "迟到结果不得复活作用域，也不重新入队。");
        }
    }

    /// <summary>实例重配置：该实例的在飞与排队全部作废，不重新入队；别的实例不受波及。</summary>
    public void ReconfigureInstance(CharacterInstanceId instanceId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(instanceId);
        lock (_gate)
        {
            var victims = _scopes.Values
                .Where(e => e.Key.InstanceId.Equals(instanceId) && e.Stage != ScopeStage.Settled)
                .ToArray();
            // 同上：先清排队，再取消在飞，取消续延不会在作废完成前把谁分派出去。
            foreach (var entry in victims.Where(e => e.Stage == ScopeStage.Queued))
                ForceTerminateLocked(entry, NarrativeScopeDisposition.InvalidatedByReconfigure, now,
                    $"实例 {instanceId.Value} 重配置：所有权代次推进，该实例的在飞与排队全部作废，不重新入队。");
            foreach (var entry in victims.Where(e => e.Stage == ScopeStage.Running))
                ForceTerminateLocked(entry, NarrativeScopeDisposition.InvalidatedByReconfigure, now,
                    $"实例 {instanceId.Value} 重配置：所有权代次推进，该实例的在飞与排队全部作废，不重新入队。");
        }
    }

    /// <summary>当前排队中的作用域数（公平调度判据用；不含在飞的那一笔）。</summary>
    public int QueuedScopeCount(CharacterInstanceId instanceId)
    {
        lock (_gate)
            return _slots.TryGetValue(instanceId, out var slot) ? slot.Queues.Values.Sum(q => q.Count) : 0;
    }

    /// <summary>该实例此刻在飞的作用域键；空闲为 null。同一实例至多一笔，这正是单写者的形状。</summary>
    public NarrativeScopeKey? RunningScope(CharacterInstanceId instanceId)
    {
        lock (_gate)
            return _slots.TryGetValue(instanceId, out var slot) ? slot.Running?.Key : null;
    }

    /// <summary>已见过的作用域的当前结果；未知键或尚未有结果为 null。</summary>
    public NarrativeScopeResult? ResultOf(NarrativeScopeKey scope)
    {
        lock (_gate)
            return _scopes.TryGetValue(scope, out var entry) ? entry.Settled : null;
    }

    private static NarrativeScopeKey KeyOf(NarrativeRequest request) =>
        new(request.InstanceId, request.ConversationId, request.BatchId);

    private Task<NarrativeScopeResult> ResubmitLocked(ScopeEntry entry, DateTimeOffset now)
    {
        // 在飞与排队中的同键请求：取回同一枚句柄，绝不产生第二次生成。
        if (entry.Stage is ScopeStage.Queued or ScopeStage.Running)
            return entry.Handle.Task;

        var disposition = entry.Settled!.Disposition;
        var cancelledOrInvalidated = disposition is NarrativeScopeDisposition.CancelledByNewInput
            or NarrativeScopeDisposition.CancelledByHost
            or NarrativeScopeDisposition.InvalidatedByScopeDeletion
            or NarrativeScopeDisposition.InvalidatedByReconfigure;
        if (!cancelledOrInvalidated)
            return Task.FromResult(entry.Settled);

        // 被取消／作废的作用域永不重新入队；同键再索要一律要过既有唯一调用额度的强制点。
        if (entry.Budget.CallCount == 0)
            return Task.FromResult(entry.Settled);
        try
        {
            entry.Budget.Consume();
        }
        catch (NarrativeContextAssemblyRejectedException overQuota)
        {
            return Task.FromResult(new NarrativeScopeResult(entry.Key,
                NarrativeScopeDisposition.FailedByCallBudget, null, NarrativeContractReason.None, overQuota.Message));
        }

        throw new NarrativeContractException(
            "唯一调用额度已被使用而第二次索要仍然放行：既有额度强制点失效，这里不兜底。",
            NarrativeContractReason.OwnershipFenced);
    }

    private InstanceSlot SlotLocked(CharacterInstanceId instanceId)
    {
        if (!_slots.TryGetValue(instanceId, out var slot))
        {
            slot = new InstanceSlot();
            _slots[instanceId] = slot;
        }

        return slot;
    }

    private static void EnqueueLocked(InstanceSlot slot, ScopeEntry entry)
    {
        var conversation = entry.Key.ConversationId;
        if (!slot.Queues.TryGetValue(conversation, out var queue))
        {
            queue = [];
            slot.Queues[conversation] = queue;
            slot.ConversationOrder.Add(conversation);
        }

        queue.Add(entry);
    }

    private void DequeueLocked(ScopeEntry entry)
    {
        if (entry.Stage != ScopeStage.Queued)
            return;
        if (_slots.TryGetValue(entry.Key.InstanceId, out var slot)
            && slot.Queues.TryGetValue(entry.Key.ConversationId, out var queue))
            queue.RemoveAll(e => ReferenceEquals(e, entry));
    }

    /// <summary>
    /// 终止一枚作用域。排队中的直接作废；在飞的先推进所有权代次（既有登记出口），
    /// 再传播取消——代次落后之后，无视取消的迟到结果会在投递账本自己的代次门上被拒，
    /// 协调器不参与任何失权判定。
    /// </summary>
    private void ForceTerminateLocked(ScopeEntry entry, NarrativeScopeDisposition intent, DateTimeOffset now,
        string detail)
    {
        if (entry.Termination is not null || entry.Stage == ScopeStage.Settled)
            return;
        entry.Termination = intent;
        entry.TerminationDetail = detail;
        if (entry.Stage == ScopeStage.Running)
        {
            AdvanceOwnershipEpochLocked(entry.Key.InstanceId, now);
            entry.Link.Cancel();
            return;
        }

        DequeueLocked(entry);
        SettleLocked(entry, new NarrativeScopeResult(entry.Key, intent, null, NarrativeContractReason.None, detail));
    }

    /// <summary>代次推进＝既有出口的两次调用：释放当前票，再以同写者同模式重新取得（登记处据此递增代次）。</summary>
    private void AdvanceOwnershipEpochLocked(CharacterInstanceId instanceId, DateTimeOffset now)
    {
        var ticket = _ownership.CurrentTicket(instanceId);
        if (ticket is null || !string.Equals(ticket.WriterId, _writerId, StringComparison.Ordinal))
        {
            throw new NarrativeContractException(
                $"推进代次必须仍持有实例 {instanceId.Value} 的当前写票；当前票不在 {_writerId} 手上。",
                NarrativeContractReason.OwnershipNotHeld);
        }

        var released = _ownership.Release(instanceId, _writerId, ticket.OwnershipEpoch);
        if (!released.Accepted)
        {
            throw new NarrativeContractException(released.Detail ?? "代次推进：释放当前写票被拒。", released.Reason);
        }

        var reacquired = _ownership.TryAcquire(instanceId, ticket.Mode, _writerId, now);
        if (!reacquired.Accepted)
        {
            throw new NarrativeContractException(reacquired.Detail ?? "代次推进：同写者重新取得写票被拒。",
                reacquired.Reason);
        }
    }

    /// <summary>
    /// 单写者的取队步：槽内已有在飞就停下；否则按会话轮转取一枚（规则原文写在类注释与报告里），
    /// 作废的跳过、写入权过期的以既有理由码拒绝并继续泵。
    /// </summary>
    private void DispatchNextLocked(InstanceSlot slot)
    {
        while (true)
        {
            if (slot.Running is not null)
                return;
            var entry = PickFairLocked(slot);
            if (entry is null)
                return;
            if (entry.Termination is { } intent)
            {
                SettleLocked(entry, new NarrativeScopeResult(entry.Key, intent, null,
                    NarrativeContractReason.None, entry.TerminationDetail));
                continue;
            }

            var instanceId = entry.Key.InstanceId;
            if (!_ownership.IsAuthorizedWriter(instanceId, _writerId, _ownership.CurrentEpoch(instanceId),
                    out var reason, out _))
            {
                SettleLocked(entry, new NarrativeScopeResult(entry.Key,
                    NarrativeScopeDisposition.RejectedByAuthority, null, reason,
                    $"分派时协调器已不持有实例 {instanceId.Value} 的当前写票（理由码 {reason}）："
                    + "作废的作用域不重新入队，宿主必须按新代次重新签发。"));
                continue;
            }

            entry.Stage = ScopeStage.Running;
            slot.Running = entry;
            var signed = entry.Request with { OwnershipEpoch = _ownership.CurrentEpoch(instanceId) };
            _ = RunAndSettleAsync(entry, signed);
            return;
        }
    }

    /// <summary>
    /// 公平调度步：按会话登记次序、从轮转游标起扫描非空队列；取到就推进游标到它的下一个会话，
    /// 每个会话一次只取一枚，空会话跳过。任何已入队的会话在一轮完整轮转内必然轮到——不会被饿死。
    /// </summary>
    private static ScopeEntry? PickFairLocked(InstanceSlot slot)
    {
        if (slot.ConversationOrder.Count == 0)
            return null;
        for (var turn = 0; turn < slot.ConversationOrder.Count; turn++)
        {
            var index = (slot.Cursor + turn) % slot.ConversationOrder.Count;
            var queue = slot.Queues[slot.ConversationOrder[index]];
            if (queue.Count == 0)
                continue;
            var entry = queue[0];
            queue.RemoveAt(0);
            slot.Cursor = (index + 1) % slot.ConversationOrder.Count;
            return entry;
        }

        return null;
    }

    private async Task RunAndSettleAsync(ScopeEntry entry, NarrativeRequest signed)
    {
        NarrativeRoundResult round;
        try
        {
            entry.Budget.Consume();
            round = await _runner.RunAsync(signed, entry.Payloads, entry.NarratorTimeout, entry.Now,
                entry.Link.Token);
        }
        catch (OperationCanceledException)
        {
            lock (_gate)
            {
                if (entry.Termination is null && entry.Stage == ScopeStage.Running)
                {
                    AdvanceOwnershipEpochLocked(entry.Key.InstanceId, entry.Now);
                    entry.Termination = NarrativeScopeDisposition.CancelledByHost;
                    entry.TerminationDetail = "调用方令牌取消：草稿作废、所有权代次推进。";
                }

                var intent = entry.Termination ?? NarrativeScopeDisposition.CancelledByHost;
                SettleLocked(entry, new NarrativeScopeResult(entry.Key, intent, null,
                    NarrativeContractReason.None, entry.TerminationDetail));
            }

            return;
        }
        catch (NarrativeContextAssemblyRejectedException overQuota)
        {
            // 同键被分派第二次（形状上不该发生）：既有额度强制点就是它的门。
            lock (_gate)
            {
                SettleLocked(entry, new NarrativeScopeResult(entry.Key,
                    NarrativeScopeDisposition.FailedByCallBudget, null, NarrativeContractReason.None,
                    overQuota.Message));
            }

            return;
        }

        lock (_gate)
        {
            if (entry.Termination is { } lateIntent)
            {
                // 无视取消而"正常完成"的迟到结果：永久无资格。作用域按取消／作废收场，
                // 它的提交尝试已被投递账本自己的代次门拒掉（见 AdvanceOwnershipEpochLocked），协调器不另判。
                SettleLocked(entry, new NarrativeScopeResult(entry.Key, lateIntent, null,
                    NarrativeContractReason.None, entry.TerminationDetail));
                return;
            }

            var disposition = round.Disposition switch
            {
                NarrativeRoundDisposition.ProviderRejected => round.Outcome?.Kind switch
                {
                    NarratorOutcomeKind.TimedOut => NarrativeScopeDisposition.FailedByTimeout,
                    NarratorOutcomeKind.ProviderFailed => NarrativeScopeDisposition.FailedByProvider,
                    NarratorOutcomeKind.MalformedResponse => NarrativeScopeDisposition.FailedByProvider,
                    _ => throw new InvalidOperationException(
                        "ProviderRejected 却带不出 provider 结果类别：这个形状不存在，这里不兜底。"),
                },
                _ => NarrativeScopeDisposition.Completed,
            };

            SettleLocked(entry, new NarrativeScopeResult(entry.Key, disposition, round,
                NarrativeContractReason.None,
                disposition == NarrativeScopeDisposition.Completed ? null : round.Detail));
        }
    }

    private void SettleLocked(ScopeEntry entry, NarrativeScopeResult result)
    {
        if (entry.Stage == ScopeStage.Settled)
            return;
        entry.Stage = ScopeStage.Settled;
        entry.Settled = result;
        _slots.TryGetValue(entry.Key.InstanceId, out var slot);
        if (slot is not null && ReferenceEquals(slot.Running, entry))
            slot.Running = null;
        entry.Registration.Dispose();
        entry.Handle.TrySetResult(result);
        if (slot is not null && slot.Running is null)
            DispatchNextLocked(slot);
    }
}
