using Pry.Contracts;

namespace Pry.Core.Memory;

/// <summary>
/// 未知高版本结构保护（HDS-001 · v4 §9.3 / AUD-05）：数据库登记的 schema 版本比当前二进制
/// 已知的 <see cref="MemoryDatabase.SchemaVersion"/> 还高时，初始化在**任何写入之前**抛出本异常。
/// 这份 SQLite 是用户机器上的唯一一份，"读不懂但先写了再说"不允许发生——
/// 旧二进制写新版结构会按自己的理解改表，破坏的是新结构自己都还没验证过的数据。
/// <para>
/// 本异常同时实现 <see cref="IUserPresentableError"/>：拒绝必须能原样呈现给用户，
/// 且必须明说"重试不会改变结果"，否则界面会引导用户反复启动、反复加深伤害。
/// </para>
/// </summary>
public sealed class SchemaVersionTooNewException : Exception, IUserPresentableError
{
    public SchemaVersionTooNewException(int binarySchemaVersion, int databaseSchemaVersion, string? context = null)
        : base($"数据库结构版本 {databaseSchemaVersion} 高于当前程序可写入的版本 {binarySchemaVersion}"
               + (string.IsNullOrEmpty(context) ? "" : $"（{context}）")
               + "；已拒绝写入任何数据。")
    {
        BinarySchemaVersion = binarySchemaVersion;
        DatabaseSchemaVersion = databaseSchemaVersion;
    }

    /// <summary>当前二进制能理解并写入的最高结构版本。</summary>
    public int BinarySchemaVersion { get; }

    /// <summary>数据库里登记的版本——比上面那个数大才走到本异常。</summary>
    public int DatabaseSchemaVersion { get; }

    /// <summary>发生地点（初始化／恢复核对），只进技术细节，不影响拦截本身。</summary>
    public string? Context { get; init; }

    public string Headline => "本机数据库来自更新版本的 Pry";

    public string Body =>
        $"这份数据库的结构版本（{DatabaseSchemaVersion}）比当前程序认识的版本（{BinarySchemaVersion}）更高，" +
        "Pry 已在写入之前停止，一份数据都没有被改动。请使用与该数据库同版本或更新的 Pry 启动；" +
        "若必须回到当前版本，请先用备份恢复到另一个目录，不要就地重试。";

    public bool RetryAllowed => false;
}

/// <summary>
/// 迁移步骤失败（HDS-001 F 条）：登记表里某一步在事务边界内抛出，整个步骤已回滚、
/// 结构版本没有被推进。数据保留在上一次成功提交的状态，修好成因后重跑初始化即可续上——
/// 登记清单全部要求幂等，重跑不是"从中间接上"，而是"从头再安全地走一遍"。
/// </summary>
public sealed class MemoryMigrationException : Exception, IUserPresentableError
{
    public MemoryMigrationException(string stepId, string stepName, Exception inner)
        : base($"持久层迁移步骤“{stepName}”（登记号 {stepId}）失败，该步骤已在事务内整体回滚；" +
               "数据库版本未推进，此前已提交的步骤与数据不受影响。", inner)
    {
        StepId = stepId;
        StepName = stepName;
    }

    public string StepId { get; }

    public string StepName { get; }

    public string Headline => "Pry 没有完成数据库的启动整备";

    public string Body =>
        $"持久层在“{StepName}”这一步失败，Pry 已把这一步整体回滚，没有留下半套结构。" +
        "请展开技术详情查看原因（磁盘、权限或文件占用是常见成因）；解决后重新启动 Pry 即可从完好的状态续跑。";

    public bool RetryAllowed => true;
}
