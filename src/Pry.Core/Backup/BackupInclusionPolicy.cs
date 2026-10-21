namespace Pry.Core.Backup;

/// <summary>
/// 备份集合的内容边界（HDS-001 C 条 / v4 §9.3）。方向是<strong>白名单</strong>：
/// 只有点名的角色卡、设置、数据库与受管媒体依赖进包；其余一切（日志、<c>runtime.json</c>、
/// 用户导入内容、临时件、模型与运行库）默认排除，且无论落在哪个目录，硬排除名单在每一层都再过一遍——
/// token／API 密钥类文件即使被人塞进受管目录也进不了备份包。
/// </summary>
public sealed class BackupInclusionPolicy
{
    /// <summary>数据目录名（宿主布局的单一事实来自 BackendRuntime/ConfigurationApplicationService；这里是备份侧的镜像，只点名、不改写）。</summary>
    public const string DatabaseFileName = "memory.db";

    /// <summary>一级单文件白名单：设置与当前角色指针。</summary>
    public static readonly IReadOnlySet<string> IncludedRootFiles =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "preferences.json", "character.json" };

    /// <summary>一级目录白名单：角色卡、互动档位覆写、受管媒体（主库/角色头像/主题素材）与贴纸目录。</summary>
    public static readonly IReadOnlySet<string> IncludedRootDirectories =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "characters", "interaction-profiles", "media", "stickers", "character-media", "theme-media"
        };

    /// <summary>硬排除的根目录（日志、用户导入内容、临时与缓存）。白名单本就不含它们；列出是为了在披露清单里说原因。</summary>
    public static readonly IReadOnlySet<string> ExcludedRootDirectories =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "logs", "attachments", "temp", "clipboard", "runtime" };

    /// <summary>任何层级都拒绝入包的文件名／后缀：会话令牌载体、SQLite 侧文件、日志、数据库原件与密钥类扩展名。
    /// 数据库原件走在线快照通道（VACUUM INTO），文件拷贝一律不收 <c>*.db</c>——只复制主 <c>.db</c> 恰恰是 v4 §9.3 点名禁止的做法。</summary>
    public static readonly IReadOnlySet<string> AlwaysDeniedFileNames =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "runtime.json", "manifest.writing" };

    public static readonly IReadOnlySet<string> AlwaysDeniedExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".log", ".db", ".db-wal", ".db-shm", ".db-journal", ".sqlite", ".sqlite3",
            ".key", ".pfx", ".pem", ".secret", ".token"
        };

    /// <summary>判断数据目录根下的一个一级条目是否整棵进入备份集合。</summary>
    public bool IsRootEntryIncluded(string rootEntryName, bool isDirectory) =>
        isDirectory
            ? IncludedRootDirectories.Contains(rootEntryName)
            : IncludedRootFiles.Contains(rootEntryName) && !IsDeniedFile(rootEntryName);

    /// <summary>白名单目录内部仍要逐文件过一遍的硬排除（纵深防御：假密钥文件不因为位置正确就入包）。</summary>
    public static bool IsDeniedFile(string fileName) =>
        AlwaysDeniedFileNames.Contains(fileName) || AlwaysDeniedExtensions.Contains(Path.GetExtension(fileName));

    /// <summary>
    /// 被排除的一级条目标注原因，供清单如实披露。未知条目按"不在白名单"处理——
    /// 备份包宁可少备并说明，不可多备泄密；用户可见后果是恢复后该目录内容缺席，需要另行告知。
    /// </summary>
    public string ExclusionReason(string rootEntryName)
    {
        if (ExcludedRootDirectories.Contains(rootEntryName))
            return rootEntryName.Equals("logs", StringComparison.OrdinalIgnoreCase)
                ? "日志不属于备份集合（体积大且可能含敏感上下文）"
                : rootEntryName.Equals("attachments", StringComparison.OrdinalIgnoreCase)
                    ? "用户导入内容不在备份集合边界内（v4 §9.3 点名排除）"
                    : "临时／缓存目录不属于备份集合";
        if (rootEntryName.Equals(Pry.Contracts.PryProtocol.RuntimeDescriptorFileName, StringComparison.OrdinalIgnoreCase))
            return "runtime.json 是本机进程发现文件，内含会话令牌，绝不入包";
        if (rootEntryName.Equals(DatabaseFileName, StringComparison.OrdinalIgnoreCase) ||
            rootEntryName.StartsWith(DatabaseFileName + "-", StringComparison.OrdinalIgnoreCase))
            return "数据库走在线一致性快照单独入包；主 .db 与 -wal/-shm 的原始文件拷贝不是备份";
        return "不在备份白名单内（默认排除）";
    }

    /// <summary>把包内相对路径（正斜杠）解析成安全的目标相对路径；含穿越段或绝对化形式时拒绝。</summary>
    public static string NormalizeBundleRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new BackupIntegrityException($"备份清单里的路径不合法：{relativePath}");
        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(s => s is "." or "..") ||
            segments.Any(s => s.Contains(':') || s.Contains('\\')))
            throw new BackupIntegrityException($"备份清单里的路径试图离开恢复目标：{relativePath}");
        return Path.Combine(segments);
    }
}
