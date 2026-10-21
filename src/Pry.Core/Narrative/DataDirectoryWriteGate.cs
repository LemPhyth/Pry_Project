namespace Pry.Core.Narrative;

/// <summary>申请数据目录写权时提交的凭据类型。</summary>
public enum WriteLeaseCredentialKind
{
    /// <summary>真正的进程级独占写入锁句柄。</summary>
    ExclusiveWriteLock = 0,

    /// <summary>只是读到了 <c>runtime.json</c>：它是发现入口，不是所有权证明。</summary>
    DiscoveryDescriptorOnly = 1,
}

/// <summary>宿主对数据目录外部观察到的事实，由 Infrastructure 层填好后交给判定。</summary>
public sealed record DataDirectoryObservation(
    string DataDirectoryPath,
    bool ExclusiveWriteLockHeldByAnotherProcess,
    bool RuntimeDescriptorPresent,
    bool DescriptorProcessAlive,
    long KnownFencingEpoch)
{
    public DataDirectoryObservation Validate()
    {
        NarrativeGuard.RequireText(DataDirectoryPath, nameof(DataDirectoryPath),
            NarrativeContractReason.WriteLockStateUnknown);
        return this;
    }
}

/// <summary>写权凭据。<see cref="FencingEpoch"/> 由锁持有处签发，本地不能自增。</summary>
public sealed record WriteLeaseCredential(WriteLeaseCredentialKind Kind, string HolderId, long FencingEpoch);

/// <summary>写权申请结果。</summary>
public sealed record WriteLeaseGrant(bool Accepted, WriteLeaseCredential? Lease, NarrativeContractReason Reason, string? Detail)
{
    internal static WriteLeaseGrant Ok(WriteLeaseCredential lease) => new(true, lease, NarrativeContractReason.None, null);

    internal static WriteLeaseGrant No(NarrativeContractReason reason, string detail) => new(false, null, reason, detail);
}

/// <summary>
/// 数据目录进程级独占写入锁的形状（v4 §8、HDS-002）。
/// 三条硬规矩：
/// ① <c>runtime.json</c> 只负责"去哪找活着的后端"，它不是锁：只凭描述文件申请写权一律
/// <see cref="NarrativeContractReason.DescriptorIsNotALock"/>。
/// ② 锁被别的进程拿着时，第二个宿主得到 <see cref="NarrativeContractReason.WriteLockHeldByOther"/> 之后
/// 没有"降级继续写"的入口：写权凭据为 null，条件写必须先取得凭据。
/// ③ 所有权一旦丢失（崩溃恢复、被接管、显式 fencing），旧凭据的条件写全部被
/// <see cref="NarrativeContractReason.WriteLeaseLost"/> 拒绝，恢复必须重新取得更高的代次。
/// 真实文件锁（例如命名 Mutex 或 <c>FileShare.None</c> 的锁文件）由宿主 Infrastructure 层实现：
/// Core 不碰进程与文件系统，这里只把"判定形状"定成可测的样子。
/// </summary>
public sealed class DataDirectoryWriteGate
{
    private WriteLeaseCredential? _held;
    private long _fencingEpoch;

    public WriteLeaseCredential? CurrentLease => _held;

    public long CurrentFencingEpoch => _fencingEpoch;

    /// <summary>
    /// 取得数据目录写权。观察事实先判，凭据类型后判：
    /// 顺序是有意的——描述文件说"没人活着"也不构成写权。
    /// </summary>
    public WriteLeaseGrant TryAcquire(WriteLeaseCredential credential, DataDirectoryObservation observation)
    {
        ArgumentNullException.ThrowIfNull(credential);
        observation.Validate();

        if (credential.Kind == WriteLeaseCredentialKind.DiscoveryDescriptorOnly)
        {
            return WriteLeaseGrant.No(NarrativeContractReason.DescriptorIsNotALock,
                "runtime.json 只负责发现：读到它不等于拥有数据目录，第二个宿主不能凭它继续写。");
        }

        if (string.IsNullOrWhiteSpace(credential.HolderId))
        {
            return WriteLeaseGrant.No(NarrativeContractReason.WriteLockStateUnknown,
                "写权凭据必须有可核对的持有者标识。");
        }

        if (observation.ExclusiveWriteLockHeldByAnotherProcess)
        {
            return WriteLeaseGrant.No(NarrativeContractReason.WriteLockHeldByOther,
                $"数据目录 {observation.DataDirectoryPath} 已被另一个进程独占；本次启动必须在写任何字节之前停下。");
        }

        if (_held is { } current)
        {
            if (string.Equals(current.HolderId, credential.HolderId, StringComparison.Ordinal))
            {
                // 同持有者重新取得必须带上不低于当前代的凭据，否则等于承认"我可以拿旧凭据续期"。
                return credential.FencingEpoch >= current.FencingEpoch
                    ? WriteLeaseGrant.Ok(current)
                    : WriteLeaseGrant.No(NarrativeContractReason.WriteLeaseLost,
                        $"同一持有者用落后的代次 {credential.FencingEpoch} 续期：所有权已被 fencing，续期无效。");
            }

            return WriteLeaseGrant.No(NarrativeContractReason.WriteLockHeldByOther,
                $"本进程之外的写者 {current.HolderId} 已持有该数据目录的写权（代次 {current.FencingEpoch}）。");
        }

        _fencingEpoch = Math.Max(_fencingEpoch + 1, credential.FencingEpoch);
        _held = credential with { FencingEpoch = _fencingEpoch };
        return WriteLeaseGrant.Ok(_held);
    }

    /// <summary>
    /// 条件写前的第一问：这张凭据现在还能不能写。
    /// 返回 false 时调用方没有任何"记录一次告警然后继续"的合法路径。
    /// </summary>
    public NarrativeContractReason CheckConditionalWriteAllowed(WriteLeaseCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);
        if (credential.Kind == WriteLeaseCredentialKind.DiscoveryDescriptorOnly)
        {
            return NarrativeContractReason.DescriptorIsNotALock;
        }

        if (_held is null)
        {
            return NarrativeContractReason.WriteLockStateUnknown;
        }

        if (!string.Equals(_held.HolderId, credential.HolderId, StringComparison.Ordinal))
        {
            return NarrativeContractReason.WriteLockHeldByOther;
        }

        return credential.FencingEpoch < _fencingEpoch
            ? NarrativeContractReason.WriteLeaseLost
            : NarrativeContractReason.None;
    }

    /// <summary>所有权丢失（崩溃恢复、被接管、租约到期）：代次提升，旧凭据全部失效。
    /// 别的进程仍持有着锁时，fencing 不能凭空发新凭据——那等于把"告警后继续写"从后门放回来。</summary>
    public WriteLeaseGrant FenceAndReacquire(string holderId, DataDirectoryObservation observation)
    {
        NarrativeGuard.RequireText(holderId, nameof(holderId), NarrativeContractReason.WriteLockStateUnknown);
        observation.Validate();
        if (observation.ExclusiveWriteLockHeldByAnotherProcess)
        {
            return WriteLeaseGrant.No(NarrativeContractReason.WriteLockHeldByOther,
                "锁仍在别的进程手上：本进程只能放弃写入，不能自封一个新代次。");
        }

        _fencingEpoch++;
        _held = new WriteLeaseCredential(WriteLeaseCredentialKind.ExclusiveWriteLock, holderId, _fencingEpoch);
        return WriteLeaseGrant.Ok(_held);
    }

    /// <summary>释放写权。代次不回退，避免下一个宿主拿着旧号继续。</summary>
    public WriteLeaseGrant Release(string holderId, long fencingEpoch)
    {
        if (_held is null)
        {
            return WriteLeaseGrant.No(NarrativeContractReason.WriteLockStateUnknown, "当前没有写权可释放。");
        }

        var reason = CheckConditionalWriteAllowed(_held with { HolderId = holderId, FencingEpoch = fencingEpoch });
        if (reason != NarrativeContractReason.None)
        {
            return WriteLeaseGrant.No(reason, "释放必须由当前持有者以当前代次提出。");
        }

        _held = null;
        return new WriteLeaseGrant(true, null, NarrativeContractReason.None, "写权已释放。");
    }
}
