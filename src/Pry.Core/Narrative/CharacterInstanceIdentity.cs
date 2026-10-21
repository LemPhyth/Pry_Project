namespace Pry.Core.Narrative;

/// <summary>
/// 稳定的角色实例身份：LocalUser × Character 的一次长期归属（HDS-002）。
/// 它不是 <c>CharacterId</c> 的别名，也不是每次启动重新算出来的临时值：
/// 由宿主签发一次、持久化、之后所有叙事/记忆/意图/投递都以它为作用域根。
/// </summary>
public sealed record CharacterInstanceId
{
    private CharacterInstanceId(string value) => Value = value;

    public string Value { get; }

    public static CharacterInstanceId Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new NarrativeContractException("CharacterInstanceId 不能为空：实例身份必须由宿主签发。",
                NarrativeContractReason.IdentityValueBlank);
        }

        var value = raw.Trim();
        if (value.Length > NarrativeBounds.MaxIdentityLength)
        {
            throw new NarrativeContractException(
                $"CharacterInstanceId 超过 {NarrativeBounds.MaxIdentityLength} 个字符。",
                NarrativeContractReason.IdentityValueTooLong);
        }

        if (value.Length < NarrativeBounds.MinIdentityLength || !NarrativeBounds.IsIdentityShape(value))
        {
            throw new NarrativeContractException(
                "CharacterInstanceId 只能由小写字母、数字、'-' 与 '_' 组成，且不得短于 "
                + $"{NarrativeBounds.MinIdentityLength} 个字符。",
                NarrativeContractReason.IdentityValueMalformed);
        }

        return new CharacterInstanceId(value);
    }

    public override string ToString() => Value;
}

/// <summary>
/// 实例的自然键：本机用户 × 角色卡。产品当前是本机单用户，但作用域必须从第一天就带上用户维，
/// 否则"角色实例"会退化成"角色卡"，与 <c>character:&lt;id&gt;</c> 旧作用域无法区分。
/// </summary>
public sealed record CharacterInstanceKey
{
    public required string LocalUserId { get; init; }
    public required string CharacterId { get; init; }

    public CharacterInstanceKey Validate()
    {
        if (string.IsNullOrWhiteSpace(LocalUserId) || string.IsNullOrWhiteSpace(CharacterId))
        {
            throw new NarrativeContractException("实例键必须同时提供 LocalUserId 与 CharacterId。",
                NarrativeContractReason.InstanceKeyIncomplete);
        }

        if (LocalUserId.Length > NarrativeBounds.MaxIdentifierLength
            || CharacterId.Length > NarrativeBounds.MaxIdentifierLength)
        {
            throw new NarrativeContractException("实例键的分量超过标识长度上限。",
                NarrativeContractReason.InstanceKeyIncomplete);
        }

        return this;
    }
}

/// <summary>实例身份的来源，写进登记行，便于迁移后仍能回答"这个 ID 是哪一次绑定产生的"。</summary>
public enum InstanceProvenance
{
    /// <summary>首次为此 User × Character 绑定，由宿主签发。</summary>
    HostBound = 0,
    /// <summary>由旧作用域（例如 <c>character:pry</c>）迁移而来。</summary>
    MigratedFromAlias = 1,
    /// <summary>由稳定 ID 换发为另一个稳定 ID，且证据引用已随行。</summary>
    ReissuedWithEvidence = 2,
}

/// <summary>一次登记的不可变视图。<see cref="InstanceId"/> 是当前有效 ID；历史 ID 只能从 alias 解析。</summary>
public sealed record CharacterInstanceRegistration(
    CharacterInstanceKey Key,
    CharacterInstanceId InstanceId,
    InstanceProvenance Provenance,
    long MappingContractVersion,
    string? OriginAlias);

/// <summary>迁移一条身份时携带的选项。默认是最保守的形状：不带证据、不换 ID。</summary>
public sealed record IdentityMigrationOptions
{
    /// <summary>把原实例已经拥有的证据引用一起搬到新 ID。缺这一项而原实例有证据 ⇒ 拒绝。</summary>
    public bool CarryEvidenceReferences { get; init; }
    /// <summary>迁移理由，进登记行；只作审计，不参与判定。</summary>
    public string Reason { get; init; } = "";
}

/// <summary>一条身份迁移的请求与结果。</summary>
public sealed record IdentityMigrationRequest(CharacterInstanceKey Key, CharacterInstanceId TargetIdentity)
{
    /// <summary>旧 ID 换发新 ID 时，必须同时把旧 ID 作为 alias 指回同一个实例，否则历史证据失联。</summary>
    public bool RetainLegacyIdentityAsAlias { get; init; } = true;
}

public sealed record InstanceMigrationOutcome(
    NarrativeContractReason Reason,
    CharacterInstanceKey Key,
    string? Detail)
{
    public bool Accepted => Reason == NarrativeContractReason.None;
}

/// <summary>
/// 实例登记表：纯内存、确定性、可快照。持久化由 HDS-005/001 的存储层负责，
/// 本类型只保证"同一 User × Character 只有一个实例、身份不因重启而变、换 ID 必须带走证据"这三条形状。
/// </summary>
public sealed class CharacterInstanceRegistry
{
    private readonly Dictionary<CharacterInstanceKey, CharacterInstanceRegistration> _registrations = [];
    private readonly Dictionary<string, CharacterInstanceId> _scopeAliases =
        new(StringComparer.Ordinal);
    private readonly Dictionary<CharacterInstanceId, string> _instanceByAlias = new();
    private readonly Dictionary<CharacterInstanceId, HashSet<string>> _evidence = [];
    private readonly long _mappingContractVersion;

    public CharacterInstanceRegistry(long mappingContractVersion = NarrativeContractVersions.IdentityMapping)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(mappingContractVersion, 1L);
        _mappingContractVersion = mappingContractVersion;
    }

    /// <summary>从持久化快照重建。快照是登记行的原样回放，不做任何"重新推导"。</summary>
    public CharacterInstanceRegistry(IEnumerable<CharacterInstanceRegistration> snapshot,
        IEnumerable<KeyValuePair<CharacterInstanceId, IEnumerable<string>>> evidenceSnapshot,
        IEnumerable<KeyValuePair<string, CharacterInstanceId>> aliasSnapshot,
        long mappingContractVersion = NarrativeContractVersions.IdentityMapping)
        : this(mappingContractVersion)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        foreach (var registration in snapshot)
        {
            _registrations[registration.Key] = registration;
        }

        foreach (var (instanceId, refs) in evidenceSnapshot)
        {
            _evidence[instanceId] = new HashSet<string>(refs, StringComparer.Ordinal);
        }

        foreach (var (scopeKey, instanceId) in aliasSnapshot)
        {
            _scopeAliases[scopeKey] = instanceId;
            _instanceByAlias[instanceId] = scopeKey;
        }
    }

    public IReadOnlyCollection<CharacterInstanceRegistration> Registrations => _registrations.Values.ToArray();

    public long MappingContractVersion => _mappingContractVersion;

    /// <summary>
    /// 幂等解析：已登记 ⇒ 永远返回既有实例，宿主这次提出的新 ID 被忽略并如实上报
    /// <see cref="NarrativeContractReason.IdentityProposalSuperseded"/>。
    /// 这是"不得每次启动重建"的可执行形状。
    /// </summary>
    public InstanceResolution Resolve(CharacterInstanceKey key, CharacterInstanceId? proposedIdentityOnFirstBind = null)
    {
        key.Validate();
        if (_registrations.TryGetValue(key, out var existing))
        {
            var superseded = proposedIdentityOnFirstBind is not null
                && !string.Equals(proposedIdentityOnFirstBind.Value, existing.InstanceId.Value, StringComparison.Ordinal);
            return new InstanceResolution(existing.InstanceId, existing,
                superseded ? NarrativeContractReason.IdentityProposalSuperseded : NarrativeContractReason.None,
                Created: false);
        }

        if (proposedIdentityOnFirstBind is null)
        {
            throw new NarrativeContractException(
                "首次绑定必须由宿主签发实例身份；Core 不做任何「随便造一个」的兜底。",
                NarrativeContractReason.IdentityValueBlank);
        }

        if (IsRenameOfCharacterIdentifier(key.CharacterId, proposedIdentityOnFirstBind.Value))
        {
            throw new NarrativeContractException(
                "实例身份不能只是角色 ID 的另一种写法：它必须携带 LocalUser 维度，由宿主签发。",
                NarrativeContractReason.IdentityRenameIsNotAMigration);
        }

        var registration = new CharacterInstanceRegistration(key, proposedIdentityOnFirstBind,
            InstanceProvenance.HostBound, _mappingContractVersion, OriginAlias: null);
        _registrations[key] = registration;
        return new InstanceResolution(proposedIdentityOnFirstBind, registration, NarrativeContractReason.None, Created: true);
    }

    /// <summary>
    /// 迁移期把旧作用域键（现状是 <c>character:&lt;characterId&gt;</c>）绑到稳定实例上。
    /// 同一个别名指向两个实例 ⇒ 拒绝；同一个实例被要求换一个别名 ⇒ 拒绝（alias 是唯一入口）。
    /// </summary>
    public InstanceMigrationOutcome BindLegacyScope(string legacyScopeKey, CharacterInstanceKey key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(legacyScopeKey);
        key.Validate();
        if (!_registrations.TryGetValue(key, out var registration))
        {
            return Rejected(NarrativeContractReason.InstanceUnknown, key, "别名必须先有实例登记才能绑定。");
        }

        if (_scopeAliases.TryGetValue(legacyScopeKey, out var bound))
        {
            return string.Equals(bound.Value, registration.InstanceId.Value, StringComparison.Ordinal)
                ? new InstanceMigrationOutcome(NarrativeContractReason.None, key, "别名已绑定，重复绑定视为幂等。")
                : Rejected(NarrativeContractReason.AliasConflict, key,
                    $"别名 {legacyScopeKey} 已经属于实例 {bound.Value}，不能改指 {registration.InstanceId.Value}。");
        }

        if (_instanceByAlias.TryGetValue(registration.InstanceId, out var existingAlias)
            && !string.Equals(existingAlias, legacyScopeKey, StringComparison.Ordinal))
        {
            return Rejected(NarrativeContractReason.AliasConflict, key,
                $"实例 {registration.InstanceId.Value} 的别名是 {existingAlias}，一条实例只允许一个旧作用域入口。");
        }

        _scopeAliases[legacyScopeKey] = registration.InstanceId;
        _instanceByAlias[registration.InstanceId] = legacyScopeKey;
        return new InstanceMigrationOutcome(NarrativeContractReason.None, key, null);
    }

    /// <summary>按旧作用域键解析当前实例。未登记的别名返回 null，由调用方走迁移而不是猜。</summary>
    public CharacterInstanceId? ResolveAlias(string legacyScopeKey) =>
        !string.IsNullOrWhiteSpace(legacyScopeKey) && _scopeAliases.TryGetValue(legacyScopeKey, out var id) ? id : null;

    /// <summary>记录一条证据引用（消息 id、记忆行、原文片段等）属于哪个实例。</summary>
    public void RecordEvidenceReference(CharacterInstanceId instanceId, string evidenceReference)
    {
        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceReference);
        if (!_evidence.TryGetValue(instanceId, out var set))
        {
            set = [];
            _evidence[instanceId] = set;
        }

        set.Add(evidenceReference);
    }

    public int EvidenceReferenceCount(CharacterInstanceId instanceId) =>
        _evidence.TryGetValue(instanceId, out var set) ? set.Count : 0;

    /// <summary>证据归属的唯一问法：旧证据必须还能指回一个当前有效实例，否则就是丢证据。</summary>
    public CharacterInstanceId? OwnerOfEvidence(string evidenceReference)
    {
        foreach (var (instanceId, set) in _evidence)
        {
            if (set.Contains(evidenceReference))
            {
                return instanceId;
            }
        }

        return null;
    }

    /// <summary>
    /// 换发稳定 ID。三条禁令都在这里落地：
    /// ① 只把 CharacterId 换个写法当作新身份 ⇒ <see cref="NarrativeContractReason.IdentityRenameIsNotAMigration"/>；
    /// ② 原实例有证据却没声明携带 ⇒ <see cref="NarrativeContractReason.IdentityReissueOrphansEvidence"/>；
    /// ③ 换发后旧 ID 必须以 alias 继续可解析，否则历史引用失联。
    /// </summary>
    public InstanceMigrationOutcome MigrateIdentity(IdentityMigrationRequest request, IdentityMigrationOptions options)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(options);
        request.Key.Validate();
        if (!_registrations.TryGetValue(request.Key, out var current))
        {
            return Rejected(NarrativeContractReason.InstanceUnknown, request.Key, "没有登记的实例无法迁移。");
        }

        if (string.Equals(current.InstanceId.Value, request.TargetIdentity.Value, StringComparison.Ordinal))
        {
            return new InstanceMigrationOutcome(NarrativeContractReason.None, request.Key, "目标与当前一致，幂等。");
        }

        if (IsRenameOfCharacterIdentifier(request.Key.CharacterId, request.TargetIdentity.Value))
        {
            return Rejected(NarrativeContractReason.IdentityRenameIsNotAMigration, request.Key,
                "新身份仍是角色 ID 的另一种写法：换名不产生实例，只会让旧证据失去归属。");
        }

        var hasEvidence = EvidenceReferenceCount(current.InstanceId) > 0;
        if (hasEvidence && !options.CarryEvidenceReferences)
        {
            return Rejected(NarrativeContractReason.IdentityReissueOrphansEvidence, request.Key,
                $"实例 {current.InstanceId.Value} 有 {EvidenceReferenceCount(current.InstanceId)} 条证据引用，"
                + "换发 ID 必须显式携带它们。");
        }

        if (hasEvidence && !request.RetainLegacyIdentityAsAlias)
        {
            return Rejected(NarrativeContractReason.IdentityReissueOrphansEvidence, request.Key,
                "带证据换发 ID 时旧 ID 必须保留为 alias，否则按旧 ID 写下的引用查不到实例。");
        }

        // 目标身份如果已经是别的实例在用，两条实例会互相吞掉证据。
        if (_registrations.Values.Any(r => string.Equals(r.InstanceId.Value, request.TargetIdentity.Value, StringComparison.Ordinal)))
        {
            return Rejected(NarrativeContractReason.AliasConflict, request.Key,
                $"目标身份 {request.TargetIdentity.Value} 已被另一个实例占用。");
        }

        if (hasEvidence && _evidence.TryGetValue(request.TargetIdentity, out var targetEvidence) && targetEvidence.Count > 0)
        {
            return Rejected(NarrativeContractReason.AliasConflict, request.Key,
                "目标身份已经持有证据引用，迁移会造成两批证据混在一起。");
        }

        CommitMigration(current, request);
        return new InstanceMigrationOutcome(NarrativeContractReason.None, request.Key, options.Reason);
    }

    /// <summary>
    /// 批量换发。任何一条被拒 ⇒ 整批不落（<see cref="NarrativeContractReason.IdentityBatchRejected"/>）。
    /// 这条存在的理由很直接：半途成功的批量换 ID 就是"批量丢证据"的唯一现实路径。
    /// </summary>
    public IReadOnlyList<InstanceMigrationOutcome> MigrateIdentities(
        IReadOnlyList<IdentityMigrationRequest> requests, IdentityMigrationOptions options)
    {
        ArgumentNullException.ThrowIfNull(requests);
        var plan = new List<(CharacterInstanceRegistration Current, IdentityMigrationRequest Request)>();
        var outcomes = new List<InstanceMigrationOutcome>();
        var seen = new HashSet<CharacterInstanceId>();
        var rejected = false;

        foreach (var request in requests)
        {
            if (!_registrations.TryGetValue(request.Key, out var current))
            {
                outcomes.Add(Rejected(NarrativeContractReason.InstanceUnknown, request.Key, "没有登记的实例无法迁移。"));
                rejected = true;
                continue;
            }

            if (!seen.Add(current.InstanceId))
            {
                outcomes.Add(Rejected(NarrativeContractReason.AliasConflict, request.Key,
                    $"同一实例 {current.InstanceId.Value} 在批量迁移里出现两次。"));
                rejected = true;
                continue;
            }

            var single = EvaluateMigration(current, request, options);
            outcomes.Add(single);
            if (!single.Accepted)
            {
                rejected = true;
                continue;
            }

            plan.Add((current, request));
        }

        if (rejected)
        {
            // 整批拒绝：一条都没落，登记状态与调用前完全一致。每条都顶同一个批次理由，
            // 原始病因留在 Detail 里，避免"看着像成功了一半"。
            return outcomes.Select(o => o with
            {
                Reason = NarrativeContractReason.IdentityBatchRejected,
                Detail = $"本批整体未落（原因：{o.Detail}）。",
            }).ToArray();
        }

        foreach (var (current, request) in plan)
        {
            CommitMigration(current, request);
        }

        return outcomes;
    }

    private InstanceMigrationOutcome EvaluateMigration(CharacterInstanceRegistration current,
        IdentityMigrationRequest request, IdentityMigrationOptions options)
    {
        if (string.Equals(current.InstanceId.Value, request.TargetIdentity.Value, StringComparison.Ordinal))
        {
            return new InstanceMigrationOutcome(NarrativeContractReason.None, request.Key, "目标与当前一致，幂等。");
        }

        if (IsRenameOfCharacterIdentifier(request.Key.CharacterId, request.TargetIdentity.Value))
        {
            return Rejected(NarrativeContractReason.IdentityRenameIsNotAMigration, request.Key,
                "新身份仍是角色 ID 的另一种写法。");
        }

        var hasEvidence = EvidenceReferenceCount(current.InstanceId) > 0;
        if (hasEvidence && !options.CarryEvidenceReferences)
        {
            return Rejected(NarrativeContractReason.IdentityReissueOrphansEvidence, request.Key,
                "换发 ID 必须显式携带证据引用。");
        }

        if (hasEvidence && !request.RetainLegacyIdentityAsAlias)
        {
            return Rejected(NarrativeContractReason.IdentityReissueOrphansEvidence, request.Key,
                "带证据换发 ID 时旧 ID 必须保留为 alias。");
        }

        return new InstanceMigrationOutcome(NarrativeContractReason.None, request.Key, options.Reason);
    }

    private void CommitMigration(CharacterInstanceRegistration current, IdentityMigrationRequest request)
    {
        var migrated = current with
        {
            InstanceId = request.TargetIdentity,
            Provenance = InstanceProvenance.ReissuedWithEvidence,
        };
        _registrations[current.Key] = migrated;

        if (_evidence.Remove(current.InstanceId, out var refs) && refs.Count > 0)
        {
            // 落到这里必然已通过 CarryEvidenceReferences 判定：判定层不允许有证据的换发绕过这一条。
            if (!_evidence.TryGetValue(request.TargetIdentity, out var target))
            {
                target = [];
                _evidence[request.TargetIdentity] = target;
            }

            foreach (var item in refs)
            {
                target.Add(item);
            }
        }

        if (request.RetainLegacyIdentityAsAlias)
        {
            // 旧稳定 ID 从此只能作为 alias 解析到当前身份，不能变成第二个真相。
            _scopeAliases[current.InstanceId.Value] = request.TargetIdentity;
        }

        if (_instanceByAlias.TryGetValue(current.InstanceId, out var legacyAlias))
        {
            _scopeAliases[legacyAlias] = request.TargetIdentity;
            _instanceByAlias.Remove(current.InstanceId);
            _instanceByAlias[request.TargetIdentity] = legacyAlias;
        }
    }

    /// <summary>
    /// "只给 CharacterId 换个名字"的识别：把候选身份归一化（去掉 <c>character:</c> 前缀、分隔符并转小写），
    /// 只要结果等于角色 ID，就说明这个"实例身份"里没有 LocalUser 维度，它不是实例而是角色卡的另一种写法。
    /// 首次绑定与迁移换发都走这一条判定。
    /// </summary>
    private static bool IsRenameOfCharacterIdentifier(string characterId, string candidateIdentity) =>
        Normalize(candidateIdentity).Length > 0
        && string.Equals(Normalize(candidateIdentity), Normalize(characterId), StringComparison.Ordinal);

    private static string Normalize(string value)
    {
        var span = value.AsSpan().Trim();
        const string prefix = "character:";
        if (span.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            span = span[prefix.Length..];
        }

        return new string([.. span.ToString().ToLowerInvariant().Where(c => c is not ('-' or '_'))]);
    }

    private static InstanceMigrationOutcome Rejected(NarrativeContractReason reason, CharacterInstanceKey key, string detail) =>
        new(reason, key, detail);

    public sealed record InstanceResolution(
        CharacterInstanceId InstanceId,
        CharacterInstanceRegistration Registration,
        NarrativeContractReason Reason,
        bool Created)
    {
        /// <summary>宿主这次提出的身份被既有登记挡下：不是错误，但必须能被发现。</summary>
        public bool ProposalSuperseded => Reason == NarrativeContractReason.IdentityProposalSuperseded;
    }
}
