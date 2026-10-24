// HDS-006 第一片·隔离原型：三类派生物的失效账（工单 §一.4）。
//
// v4 §10.2：原始叙事与真实收发是证据，压缩／SceneFrame／交接摘要均是**可重建的导航**，
// 删除或失效时必须同步更新它们。本文件把"摘要／向量／缓存"三处派生物做成各自独立的一格：
// 任一处失效时另外两处**既不受影响、也不需要读实现就能看出来**——失效后本处的正文被红action、
// 读数点名原因，另两处的正文仍原样读得到，同时观察日志留下一条命名到具体派生物的记录。
//
// 数字门槛零新增；全内存、参数全走入参、零生产调用方。
namespace Pry.Core.Memory;

/// <summary>三类派生物。摘要＝可重建导航，向量＝可重建索引，缓存＝可重建上下文副本。</summary>
public enum Hds006ArtifactKind
{
    Summary,
    Vector,
    Cache
}

/// <summary>派生物的三种可观察状态。未发布与已失效是两个读数，不共用一个"没有"。</summary>
public enum Hds006ArtifactState
{
    Unpublished,
    Live,
    Invalidated
}

/// <summary>派生物的定位键＝（哪一类，挂在哪条证据上）。两类之一的失效绝不能顺带动到另一类。</summary>
public sealed record Hds006DerivedArtifactKey(Hds006ArtifactKind Kind, long EvidenceId);

/// <summary>一次派生物读数：状态＋正文＋版本＋原因。失效后正文只剩固定哨兵文本，读得到"内容确实没了"。</summary>
public sealed record Hds006DerivedArtifactReadout(Hds006DerivedArtifactKey Key, Hds006ArtifactState State,
    string Payload, long Version, string Reason);

/// <summary>观察日志的一格：序号＋定位键＋落到的状态＋原因。§一.4 要"失效能被观察到"就落在这一格。</summary>
public sealed record Hds006DerivedArtifactObservation(long Sequence, Hds006DerivedArtifactKey Key,
    Hds006ArtifactState State, string Reason);

/// <summary>
/// 派生物账。三处派生物各占一格，失效动作按 (Kind, EvidenceId) 精确命中一处，
/// 命中后写观察日志；没命中（未发布或已失效）返回 false 且**不写日志**，避免"重复失效刷出第二条记录"。
/// </summary>
public sealed class Hds006DerivedArtifactLedger
{
    /// <summary>失效后被清掉的正文留下的固定哨兵文本（不是空串，空串与"从来没写过"就分不开了）。</summary>
    public const string RedactedPayload = "<redacted>";

    /// <summary>未发布派生物的固定读数原因。</summary>
    public const string NotPublishedReason = "not_published";

    private readonly List<ArtifactState> _entries = new();
    private readonly List<Hds006DerivedArtifactObservation> _observations = new();
    private long _nextSequence;

    /// <summary>观察日志的只读快照。</summary>
    public IReadOnlyList<Hds006DerivedArtifactObservation> Observations => _observations.ToArray();

    /// <summary>发布一处派生物；同键重复发布是覆盖导航正文（派生物本就可重建），并留下一条记录。</summary>
    public Hds006DerivedArtifactReadout Publish(Hds006DerivedArtifactKey key, string payload)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (string.IsNullOrEmpty(payload))
            throw new ArgumentException("A derived artifact must carry its payload text.", nameof(payload));
        var entry = FindExact(key);
        if (entry is null)
        {
            entry = new ArtifactState { Kind = key.Kind, EvidenceId = key.EvidenceId };
            _entries.Add(entry);
        }
        entry.Payload = payload;
        entry.State = Hds006ArtifactState.Live;
        entry.Reason = "published";
        entry.Version += 1;
        Append(key, Hds006ArtifactState.Live, "published");
        return entry.Readout(key);
    }

    /// <summary>
    /// 失效一处派生物：只有 (Kind, EvidenceId) 两处都命中且当前是 Live 才生效。
    /// 生效时清正文＋记原因＋写观察日志，**同一证据上的另外两类一个字节都不动**。
    /// </summary>
    public bool TryInvalidate(Hds006DerivedArtifactKey key, string reason)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Invalidating a derived artifact must name the reason.", nameof(reason));
        var matched = _entries.Where(x => x.EvidenceId == key.EvidenceId && x.Kind == key.Kind
                                        && x.State == Hds006ArtifactState.Live).ToList();
        if (matched.Count == 0) return false;
        foreach (var entry in matched)
        {
            entry.State = Hds006ArtifactState.Invalidated;
            entry.Payload = RedactedPayload;
            entry.Reason = reason;
            entry.Version += 1;
            Append(key with { Kind = entry.Kind }, Hds006ArtifactState.Invalidated, reason);
        }
        return true;
    }

    /// <summary>读数：未发布给 Unpublished＋固定原因，绝不返回 null 让调用方自己判断"是没写还是被删了"。</summary>
    public Hds006DerivedArtifactReadout Readout(Hds006DerivedArtifactKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        var entry = FindExact(key);
        return entry is null
            ? new Hds006DerivedArtifactReadout(key, Hds006ArtifactState.Unpublished, string.Empty, 0,
                NotPublishedReason)
            : entry.Readout(key);
    }

    private ArtifactState? FindExact(Hds006DerivedArtifactKey key) =>
        _entries.FirstOrDefault(x => x.EvidenceId == key.EvidenceId && x.Kind == key.Kind);

    private void Append(Hds006DerivedArtifactKey key, Hds006ArtifactState state, string reason)
    {
        _nextSequence += 1;
        _observations.Add(new Hds006DerivedArtifactObservation(_nextSequence, key, state, reason));
    }

    private sealed class ArtifactState
    {
        public Hds006ArtifactKind Kind;
        public long EvidenceId;
        public string Payload = string.Empty;
        public Hds006ArtifactState State = Hds006ArtifactState.Unpublished;
        public string Reason = NotPublishedReason;
        public long Version;

        public Hds006DerivedArtifactReadout Readout(Hds006DerivedArtifactKey key) =>
            new(key, State, Payload, Version, Reason);
    }
}
