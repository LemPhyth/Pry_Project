namespace Pry.Core.Narrative;

/// <summary>
/// 实例的引擎模式。v4 §15 只承认这两个值：某实例由其中一方独占写入与投递权，
/// 选择由宿主持久化，不从请求参数临时决定，也没有第三种"都试一下"的模式。
/// </summary>
public enum EngineMode
{
    Legacy = 0,
    NarrativeV1 = 1,
}

/// <summary>
/// 可发送真相的来源。同一实例上它只能有一个值：旧 reply plan 与新 outbox 不能同时对外声称
/// "接下来要发的是我这一份"。
/// </summary>
public enum SendableTruthSource
{
    /// <summary>还没有任何可发送真相（新实例，或旧 pending 已清点关闭）。</summary>
    None = 0,
    LegacyReplyPlan = 1,
    NarrativeOutbox = 2,
}

/// <summary>实例写入权凭据。<see cref="OwnershipEpoch"/> 是宿主所有权代次，只能由登记处递增。</summary>
public sealed record WriteOwnershipTicket(
    CharacterInstanceId InstanceId,
    EngineMode Mode,
    string WriterId,
    long OwnershipEpoch,
    DateTimeOffset AcquiredAtUtc);

/// <summary>写入权申请的结果。拒绝时 <see cref="Ticket"/> 必为 null，不存在"给你半张票"的形状。</summary>
public sealed record OwnershipGrant(bool Accepted, WriteOwnershipTicket? Ticket, NarrativeContractReason Reason, string? Detail)
{
    internal static OwnershipGrant Ok(WriteOwnershipTicket ticket) => new(true, ticket, NarrativeContractReason.None, null);

    internal static OwnershipGrant No(NarrativeContractReason reason, string detail) => new(false, null, reason, detail);
}

/// <summary>
/// 实例写入权与单一 outbox 的归属登记处（HDS-002）。
/// 它守三条：
/// ① 同一实例同时只能有一名写者；第二名要么等待，要么走显式交接，不会被"后到的覆盖先到的"放行。
/// ② 引擎模式与可发送真相绑在这张票上；模式不匹配或另一份真相还在 ⇒ 拒绝。
/// ③ 每一次所有权更替都推进代次；代次落后的写者从此失去条件写资格。
/// 本类是纯内存的判定形状，持久化与真实进程锁由 HDS-001/005 落地；两者共用同一套理由码。
/// </summary>
public sealed class OutboxOwnershipRegistry
{
    private readonly Dictionary<CharacterInstanceId, WriteOwnershipTicket> _tickets = [];
    private readonly Dictionary<CharacterInstanceId, long> _highestEpoch = [];
    private readonly Dictionary<CharacterInstanceId, SendableTruthSource> _sendableTruth = [];

    public WriteOwnershipTicket? CurrentTicket(CharacterInstanceId instanceId) =>
        instanceId is not null && _tickets.TryGetValue(instanceId, out var ticket) ? ticket : null;

    /// <summary>该实例已经用过的最大代次。释放不会让它回退，这是 fencing 成立的前提。</summary>
    public long CurrentEpoch(CharacterInstanceId instanceId) =>
        instanceId is not null && _highestEpoch.TryGetValue(instanceId, out var epoch) ? epoch : 0;

    public SendableTruthSource CurrentSendableTruth(CharacterInstanceId instanceId) =>
        _sendableTruth.TryGetValue(instanceId, out var source) ? source : SendableTruthSource.None;

    /// <summary>
    /// 取得写入权。已被他人持有 ⇒ <see cref="NarrativeContractReason.OwnershipHeldElsewhere"/>，
    /// 并且不返回任何凭据；同写者重复申请是幂等，不换代次。
    /// </summary>
    public OwnershipGrant TryAcquire(CharacterInstanceId instanceId, EngineMode mode, string writerId,
        DateTimeOffset acquiredAtUtc)
    {
        NarrativeGuard.RequireText(writerId, nameof(writerId), NarrativeContractReason.OwnershipNotHeld);
        ArgumentNullException.ThrowIfNull(instanceId);

        if (_tickets.TryGetValue(instanceId, out var held))
        {
            if (string.Equals(held.WriterId, writerId, StringComparison.Ordinal))
            {
                return held.Mode == mode
                    ? OwnershipGrant.Ok(held)
                    : OwnershipGrant.No(NarrativeContractReason.EngineModeMismatch,
                        $"同一写者 {writerId} 不能一边以 {held.Mode} 一边以 {mode} 持有同一实例；请先交接。");
            }

            return OwnershipGrant.No(NarrativeContractReason.OwnershipHeldElsewhere,
                $"实例 {instanceId.Value} 的写入权在 {held.WriterId}（代次 {held.OwnershipEpoch}）手上，"
                + "第二写者只能等待或走显式交接。");
        }

        var ticket = new WriteOwnershipTicket(instanceId, mode, writerId, NextEpoch(instanceId), acquiredAtUtc);
        _tickets[instanceId] = ticket;
        return OwnershipGrant.Ok(ticket);
    }

    /// <summary>
    /// 显式交接：交出方必须用自己的票签一次手，接管方拿到新的更高代次。
    /// 交出方不出现（或代次落后）⇒ 拒绝，抢占不存在。
    /// </summary>
    public OwnershipGrant TryHandover(CharacterInstanceId instanceId, string fromWriterId, long fromEpoch,
        EngineMode toMode, string toWriterId, DateTimeOffset handedOverAtUtc)
    {
        NarrativeGuard.RequireText(toWriterId, nameof(toWriterId), NarrativeContractReason.OwnershipNotHeld);
        if (!_tickets.TryGetValue(instanceId, out var held))
        {
            return OwnershipGrant.No(NarrativeContractReason.OwnershipNotHeld,
                $"实例 {instanceId.Value} 当前没有写者，交接无从发生。");
        }

        if (!string.Equals(held.WriterId, fromWriterId, StringComparison.Ordinal) || held.OwnershipEpoch != fromEpoch)
        {
            return OwnershipGrant.No(NarrativeContractReason.HandoverNotAuthorized,
                $"交出方凭据不匹配（登记的是 {held.WriterId}/代次 {held.OwnershipEpoch}）。");
        }

        if (string.Equals(fromWriterId, toWriterId, StringComparison.Ordinal))
        {
            return OwnershipGrant.No(NarrativeContractReason.HandoverNotAuthorized,
                "交接的两端是同一个写者：这只是换模式，走 Release + TryAcquire。");
        }

        var ticket = new WriteOwnershipTicket(instanceId, toMode, toWriterId, NextEpoch(instanceId), handedOverAtUtc);
        _tickets[instanceId] = ticket;
        return OwnershipGrant.Ok(ticket);
    }

    /// <summary>释放写入权。代次必须与票一致，落后的票释放不了别人的所有权。</summary>
    public OwnershipGrant Release(CharacterInstanceId instanceId, string writerId, long ownershipEpoch)
    {
        if (!_tickets.TryGetValue(instanceId, out var held))
        {
            return OwnershipGrant.No(NarrativeContractReason.OwnershipNotHeld,
                $"实例 {instanceId.Value} 没有登记中的写者。");
        }

        if (!string.Equals(held.WriterId, writerId, StringComparison.Ordinal))
        {
            return OwnershipGrant.No(NarrativeContractReason.OwnershipHeldElsewhere,
                $"持有者是 {held.WriterId}，{writerId} 无权释放它。");
        }

        if (held.OwnershipEpoch != ownershipEpoch)
        {
            return OwnershipGrant.No(NarrativeContractReason.OwnershipFenced,
                $"这张票的代次 {ownershipEpoch} 已落后于登记的 {held.OwnershipEpoch}。");
        }

        _tickets.Remove(instanceId);
        return new OwnershipGrant(true, null, NarrativeContractReason.None,
            "写入权已释放；可发送真相仍留在登记处，直到新写者清点或重新声明。");
    }

    /// <summary>
    /// 声明"这个实例的可发送真相是谁那一份"。
    /// 双真相在这里就进不来：已经声明过另一种 ⇒ <see cref="NarrativeContractReason.SendableTruthAlreadyDeclared"/>；
    /// 旧 reply plan 还有未清点的 pending 就要交给新 outbox ⇒
    /// <see cref="NarrativeContractReason.SendableTruthNotDrained"/>（先把旧的关干净，再来声明）。
    /// </summary>
    public OwnershipGrant TryDeclareSendableTruth(CharacterInstanceId instanceId, SendableTruthSource source,
        string writerId, long ownershipEpoch, int legacyPendingPlanCount)
    {
        if (!IsAuthorizedWriter(instanceId, writerId, ownershipEpoch, out var reason, out _))
        {
            return OwnershipGrant.No(reason, "声明可发送真相必须由当前持有写入权的票提出。");
        }

        var current = CurrentSendableTruth(instanceId);
        if (current == source)
        {
            return OwnershipGrant.Ok(_tickets[instanceId]);
        }

        if (current != SendableTruthSource.None)
        {
            return OwnershipGrant.No(NarrativeContractReason.SendableTruthAlreadyDeclared,
                $"实例 {instanceId.Value} 的可发送真相已经是 {current}；改判 {source} 必须先完成交接与清点。");
        }

        if (source == SendableTruthSource.NarrativeOutbox && legacyPendingPlanCount > 0)
        {
            return OwnershipGrant.No(NarrativeContractReason.SendableTruthNotDrained,
                $"还有 {legacyPendingPlanCount} 条旧 reply plan 待发条目没关闭，新 outbox 不能同时成为第二份真相。");
        }

        _sendableTruth[instanceId] = source;
        return OwnershipGrant.Ok(_tickets[instanceId]);
    }

    /// <summary>旧 pending 清点关闭后，把"真相已排空"记录成 None，允许下一次声明。</summary>
    public OwnershipGrant ClearSendableTruth(CharacterInstanceId instanceId, string writerId, long ownershipEpoch)
    {
        if (!IsAuthorizedWriter(instanceId, writerId, ownershipEpoch, out var reason, out _))
        {
            return OwnershipGrant.No(reason, "只有当前写者能清空真相声明。");
        }

        _sendableTruth[instanceId] = SendableTruthSource.None;
        return OwnershipGrant.Ok(_tickets[instanceId]);
    }

    /// <summary>
    /// 条件写前的唯一问法：写者是否仍持有这张票的当前代次。
    /// 任何一条不成立都返回具体原因，调用方必须据此拒绝写入——不允许"告警之后继续写"。
    /// </summary>
    public bool IsAuthorizedWriter(CharacterInstanceId instanceId, string writerId, long ownershipEpoch,
        out NarrativeContractReason reason, out WriteOwnershipTicket? ticket)
    {
        if (!_tickets.TryGetValue(instanceId, out ticket))
        {
            reason = NarrativeContractReason.OwnershipNotHeld;
            return false;
        }

        if (!string.Equals(ticket.WriterId, writerId, StringComparison.Ordinal))
        {
            reason = NarrativeContractReason.OwnershipHeldElsewhere;
            return false;
        }

        if (ticket.OwnershipEpoch != ownershipEpoch)
        {
            reason = ownershipEpoch < ticket.OwnershipEpoch
                ? NarrativeContractReason.OwnershipFenced
                : NarrativeContractReason.OwnershipNotHeld;
            return false;
        }

        reason = NarrativeContractReason.None;
        return true;
    }

    private long NextEpoch(CharacterInstanceId instanceId)
    {
        var next = CurrentEpoch(instanceId) + 1;
        _highestEpoch[instanceId] = next;
        return next;
    }
}
