namespace Pry.Core.Narrative;

/// <summary>
/// 契约拒绝的稳定分类（HDS-002）。宿主把它映射成 HTTP 语义、诊断文案或指标；
/// 判定永远认这个枚举，不认消息文本——消息只给人看。
/// 分组之间留出空位，是为了让后续任务往组里加成员时不改变已有编号。
/// </summary>
public enum NarrativeContractReason
{
    None = 0,

    // —— 稳定身份与迁移 ——
    IdentityValueBlank = 100,
    IdentityValueTooLong = 101,
    IdentityValueMalformed = 102,
    InstanceKeyIncomplete = 103,
    /// <summary>同一个 User × Character 又提出一个新 ID：既有实例保留，提案被忽略并如实上报。</summary>
    IdentityProposalSuperseded = 104,
    /// <summary>把 CharacterId 换个写法当作新实例：这不是迁移。</summary>
    IdentityRenameIsNotAMigration = 105,
    /// <summary>换 ID 但没有带走原证据引用：会让旧记忆/原文变成孤儿。</summary>
    IdentityReissueOrphansEvidence = 106,
    /// <summary>批量迁移里任何一条被拒，整批不落。</summary>
    IdentityBatchRejected = 107,
    InstanceUnknown = 108,
    AliasUnknown = 109,
    AliasConflict = 110,

    // —— 叙事文档与归属边界 ——
    BatchEmpty = 200,
    /// <summary>草稿试图自带实例或接收者：身份只能由宿主签发。</summary>
    DraftIdentityMismatch = 201,
    /// <summary>感知引用指向本批次之外的序号。</summary>
    PerceivedInputUnknown = 202,
    /// <summary>消费水位越过宿主签发的批次上界。</summary>
    ProcessedBeyondBatch = 203,
    /// <summary>消费水位不前进或回退。</summary>
    WatermarkNotAdvancing = 204,
    ActionBindingMissing = 205,
    ActionBindingAmbiguous = 206,
    ActionTargetMismatch = 207,
    ActionSegmentCountInvalid = 208,
    /// <summary>未来意图被当成已经发生的事实召回。</summary>
    IntentClaimedAsFact = 209,
    /// <summary>角色虚构材料被登记成用户现实事实。</summary>
    RealityContamination = 210,
    /// <summary>没有真实用户来源或显式反馈却宣称已确认。</summary>
    ConfirmationWithoutUserEvidence = 211,
    ContractVersionUnsupported = 212,
    ContentHashMissing = 213,
    NarrativeProseMissing = 214,
    /// <summary>提交或提案把作用域扩大到了宿主签发之外（换实例、换会话、换批次）。</summary>
    ScopeEscalation = 216,
    /// <summary>提交声称处理过批次里没有的输入。</summary>
    ProcessedInputUnreceived = 215,

    // —— 写入权与单一 outbox ——
    OwnershipHeldElsewhere = 300,
    OwnershipNotHeld = 301,
    OwnershipFenced = 302,
    EngineModeMismatch = 303,
    SendableTruthAlreadyDeclared = 304,
    /// <summary>旧 pending 还没清点关闭，不允许把可发送真相交给新 outbox。</summary>
    SendableTruthNotDrained = 305,
    HandoverNotAuthorized = 306,

    // —— 投递段账本 ——
    SegmentKeyDuplicate = 400,
    SegmentCommitUnknown = 401,
    SegmentActionUnknown = 402,
    SegmentIndexNotContiguous = 403,
    SegmentCountExceeded = 404,
    SegmentPredecessorPending = 405,
    /// <summary>delivered / cancelled 是终态，迟到结果不得翻转。</summary>
    SegmentTerminalImmutable = 406,
    SegmentTextNotFromNarrative = 407,
    SegmentStickerPayloadInvalid = 408,
    SegmentNotFound = 409,
    SegmentDeliveryEvidenceMissing = 410,

    // —— 条件提交 ——
    StoryVersionConflict = 500,
    LateCommitRejected = 501,
    CommitAlreadyApplied = 502,
    ConversationInvalidated = 503,
    DeletionEpochAdvanced = 504,
    DuplicateCommitId = 505,

    // —— 数据目录进程级独占写锁 ——
    WriteLockHeldByOther = 600,
    /// <summary>只凭 runtime.json 的发现结果申请写权：描述文件不是所有权锁。</summary>
    DescriptorIsNotALock = 601,
    WriteLeaseLost = 602,
    WriteLockStateUnknown = 603,
}

/// <summary>
/// 结构不合法（空白、越界、字符集不符）在构造期就抛；这是编程错误，不是运行时可协商的拒绝。
/// 契约级的"这次不许写"一律以 <see cref="NarrativeContractReason"/> 作为数据返回，
/// 好让 Core 保持不依赖 HTTP 与错误码语义。
/// </summary>
public sealed class NarrativeContractException(string message, NarrativeContractReason reason) : ArgumentException(message)
{
    public NarrativeContractReason Reason { get; } = reason;
}

/// <summary>长度与字符集的公共护栏。ID 要进主键、索引和日志，所以边界必须是数字而不是"看着还行"。</summary>
internal static class NarrativeBounds
{
    public const int MaxIdentifierLength = 128;
    public const int MinIdentifierLength = 1;
    public const int MaxIdentityLength = 128;
    public const int MinIdentityLength = 8;

    public static void RequireIdentifier(string? value, string field, NarrativeContractReason blank,
        NarrativeContractReason tooLong)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new NarrativeContractException($"{field} 不能为空。", blank);
        }

        if (value.Length > MaxIdentifierLength)
        {
            throw new NarrativeContractException($"{field} 超过 {MaxIdentifierLength} 个字符。", tooLong);
        }
    }

    /// <summary>宿主签发的实例 ID 允许 GUID、ULID、uuid7 之类的形状：小写字母、数字与连字符。</summary>
    public static bool IsIdentityShape(string value)
    {
        foreach (var c in value)
        {
            var ok = c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_';
            if (!ok)
            {
                return false;
            }
        }

        return true;
    }
}
