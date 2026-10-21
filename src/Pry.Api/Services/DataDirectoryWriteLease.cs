using Pry.Contracts;
using Pry.Core.Narrative;

namespace Pry.Api.Services;

/// <summary>
/// DEF-WRIGATE-01 · 契约 §10.2：宿主（Infrastructure 层）对数据目录持有的真实进程级独占写句柄。
/// 数据目录内一把以 <see cref="FileShare.None"/> 打开的 <c>runtime-write.lock</c>：句柄随进程存活，
/// 进程退出（含崩溃）由系统释放——"另一个活着的进程持有同一目录"由此变成可观察的共享冲突，
/// 而不是靠 <c>runtime.json</c> 里的 pid 猜。Core 的 <see cref="DataDirectoryWriteGate"/> 只做判定：
/// 观察事实由本类填（<see cref="DataDirectoryObservation"/>），写权凭据以
/// <see cref="WriteLeaseCredentialKind.ExclusiveWriteLock"/> 提出；描述文件本身永远不构成写权
/// （<see cref="NarrativeContractReason.DescriptorIsNotALock"/>，契约 §10.2 第 1 条）。
/// </summary>
public sealed class DataDirectoryWriteLease : IDisposable
{
    public const string LockFileName = "runtime-write.lock";

    private readonly DataDirectoryWriteGate _gate = new();
    private readonly string _dataDirectory;
    private readonly string _holderId;
    private FileStream? _handle;
    private WriteLeaseCredential? _lease;

    public DataDirectoryWriteLease(string dataDirectory)
    {
        _dataDirectory = dataDirectory;
        _holderId = $"{Environment.ProcessId}:{Guid.NewGuid():N}";
    }

    public string LockPath => Path.Combine(_dataDirectory, LockFileName);

    public bool HoldsExclusiveHandle => _handle is not null;

    public WriteLeaseCredential? Lease => _lease;

    /// <summary>
    /// 取得独占句柄并过写权判定，返回当前代次的租约。被拒的路径上本类没有创建或覆盖过任何内容文件，
    /// 锁句柄也没有被我们拿到；数据目录本身只在"目录不存在 ⇒ 锁文件不可能存在 ⇒ 也没有别的持有者"时
    /// 才被创建，而那次创建属于成功取得写权的前置，不属于被拒路径。
    /// </summary>
    public WriteLeaseCredential Acquire(bool descriptorPresent, bool descriptorProcessAlive)
    {
        if (_lease is { } existing) return existing;

        FileStream handle;
        var freshDirectoryAttempted = false;
        while (true)
        {
            try
            {
                handle = new FileStream(LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                break;
            }
            catch (DirectoryNotFoundException) when (!freshDirectoryAttempted)
            {
                freshDirectoryAttempted = true;
                Directory.CreateDirectory(_dataDirectory);
            }
            catch (IOException failure)
            {
                // 要么锁被别的进程持有（共享冲突），要么我们根本无法独占这个目录——两者都只能读成
                // "未取得写权"。fail-closed：让 Core 的判定下结论，本路径不碰任何字节。
                var denial = _gate.TryAcquire(Credential(), Observe(descriptorPresent, descriptorProcessAlive, true));
                throw new DataDirectoryWriteLockedException(_dataDirectory, denial.Reason, denial.Detail, failure);
            }
        }

        _handle = handle;
        var grant = _gate.TryAcquire(Credential(), Observe(descriptorPresent, descriptorProcessAlive, false));
        if (!grant.Accepted || grant.Lease is null)
        {
            var failure = new DataDirectoryWriteLockedException(_dataDirectory, grant.Reason, grant.Detail, null);
            Dispose();
            throw failure;
        }

        _lease = grant.Lease;
        return grant.Lease;
    }

    /// <summary>条件写的第一问（契约 §10.2 第 3 条）：碰 <c>runtime.json</c> 之前必须再问一次判定。
    /// 返回拒绝时没有"记录一次告警然后继续"的出口——直接抛同一个可呈现拒绝。</summary>
    public void AssertWriteAllowed()
    {
        if (_lease is null)
        {
            throw new DataDirectoryWriteLockedException(_dataDirectory, NarrativeContractReason.WriteLockStateUnknown,
                "写权从未取得：条件写 fail-closed。", null);
        }

        var reason = _gate.CheckConditionalWriteAllowed(_lease);
        if (reason != NarrativeContractReason.None)
        {
            throw new DataDirectoryWriteLockedException(_dataDirectory, reason, "写权已被 fencing 或已释放。", null);
        }
    }

    private WriteLeaseCredential Credential() =>
        new(WriteLeaseCredentialKind.ExclusiveWriteLock, _holderId, FencingEpoch: 0);

    private DataDirectoryObservation Observe(bool descriptorPresent, bool descriptorProcessAlive, bool heldByOther) =>
        new DataDirectoryObservation(_dataDirectory, ExclusiveWriteLockHeldByAnotherProcess: heldByOther,
            RuntimeDescriptorPresent: descriptorPresent, DescriptorProcessAlive: descriptorProcessAlive,
            KnownFencingEpoch: _gate.CurrentFencingEpoch).Validate();

    /// <summary>释放租约并关闭句柄。句柄一关，后继实例的 <c>Acquire</c> 就能取得写权——
    /// 锁不跨进程存活，也就不需要谁去"删掉"它。</summary>
    public void Dispose()
    {
        if (_lease is { } lease) _gate.Release(_holderId, lease.FencingEpoch);
        _lease = null;
        _handle?.Dispose();
        _handle = null;
    }
}

/// <summary>
/// 第二个宿主在数据目录的进程级独占写锁前被拒（DEF-WRIGATE-01）。它走既有呈现链：
/// <see cref="IUserPresentableError"/> → 界面原样取 Headline/Body/RetryAllowed，
/// 并随宿主 <c>StartAsync</c> 的失败把"拒绝启动"落实成事实——接口上不存在"记录一次告警然后继续"的出口。
/// </summary>
public sealed class DataDirectoryWriteLockedException : Exception, IUserPresentableError
{
    public DataDirectoryWriteLockedException(string dataDirectory, NarrativeContractReason reason,
        string? gateDetail, Exception? inner)
        : base($"数据目录 {dataDirectory} 的进程级独占写锁无法取得（判定 {reason}" +
               (string.IsNullOrWhiteSpace(gateDetail) ? "" : $"：{gateDetail}") + "）；本次启动在写入任何字节之前停下。", inner)
    {
        DataDirectory = dataDirectory;
        Reason = reason;
    }

    /// <summary>被拒的数据目录。</summary>
    public string DataDirectory { get; }

    /// <summary>Core 判定给出的稳定理由码（通常为 <see cref="NarrativeContractReason.WriteLockHeldByOther"/>）。</summary>
    public NarrativeContractReason Reason { get; }

    public string Headline => "这个数据目录正被另一个 Pry 同时写";

    public string Body =>
        "另一个仍在运行的 Pry 持有这份数据目录的独占写锁，本次启动已经拒绝，没有写入或改动任何文件。" +
        "重复启动不会改变结果——只要那个实例还活着，锁就始终在它手上。" +
        "请先完全退出另一个 Pry（同一份数据目录同一时间只允许一个写入实例），" +
        "或为本实例换一个独立的数据目录，再重新启动。";

    public bool RetryAllowed => false;
}
