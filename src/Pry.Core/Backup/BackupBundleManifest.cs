using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pry.Core.Backup;

/// <summary>
/// 备份包清单（HDS-001 · v4 §9.3）。它是备份集合的"最后一笔"——所有数据文件写完才落清单，
/// 所以"清单缺失或不可解析"就等价于"这不是一次完成的备份"，恢复侧必须先核对再落地。
/// 清单本身只携带计数、散列与摘要，不携带任何用户内容原文。
/// </summary>
public sealed record BackupBundleManifest
{
    /// <summary>清单格式版本。与产品 schema 版本无关，改动它必须同步改读侧兼容。</summary>
    public const int CurrentManifestVersion = 1;

    [JsonPropertyName("manifestVersion")] public required int ManifestVersion { get; init; }

    [JsonPropertyName("bundleId")] public required string BundleId { get; init; }

    [JsonPropertyName("createdAtUtc")] public required string CreatedAtUtc { get; init; }

    /// <summary>制作备份的二进制登记的持久层结构版本（只读引用 <c>MemoryDatabase.SchemaVersion</c>，不冒领）。</summary>
    [JsonPropertyName("createdByBinarySchemaVersion")] public required int CreatedByBinarySchemaVersion { get; init; }

    /// <summary>备份包的固定文件名。发现与核对都以它为准。</summary>
    public const string ManifestFileName = "manifest.json";

    [JsonPropertyName("database")] public BackupDatabaseSection? Database { get; init; }

    [JsonPropertyName("files")] public required IReadOnlyList<BackupFileEntry> Files { get; init; }

    /// <summary>备份时被内容边界规则挡在门外的一级条目（如实披露"没备什么"，恢复后不靠猜）。</summary>
    [JsonPropertyName("excludedRootEntries")] public required IReadOnlyList<string> ExcludedRootEntries { get; init; }

    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static async Task<BackupBundleManifest> LoadAsync(string bundlePath, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(bundlePath, ManifestFileName);
        if (!File.Exists(path))
            throw new BackupIntegrityException(
                $"备份包 {Path.GetFileName(bundlePath)} 没有 {ManifestFileName}：备份集合不完整" +
                "（备份中断的残骸没有清单，按未完成处理，不尝试拼凑）。");
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<BackupBundleManifest>(stream, SerializerOptions, cancellationToken)
               ?? throw new BackupIntegrityException($"备份清单无法解析：{path}");
    }

    /// <summary>按 v4 §9.3：清单写在所有数据文件之后，用临时文件+改名落定，避免半截 JSON 被当成有效清单。</summary>
    public async Task WriteAsync(string bundlePath, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(bundlePath, ManifestFileName);
        var temporary = path + ".writing";
        await using (var stream = File.Create(temporary))
            await JsonSerializer.SerializeAsync(stream, this, SerializerOptions, cancellationToken);
        File.Move(temporary, path, overwrite: true);
    }

    public static string Sha256Of(Stream stream)
    {
        stream.Position = 0;
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}

/// <summary>备份集合里的一个文件（相对备份包的路径 + 逐文件散列）。恢复前必须先按散列核对。</summary>
public sealed record BackupFileEntry
{
    /// <summary>相对备份包根的路径，形如 <c>files/characters/x.json</c>。只允许正斜杠、不含 ".."。</summary>
    [JsonPropertyName("relativePath")] public required string RelativePath { get; init; }

    /// <summary>该文件在备份时的 SHA-256（十六进制小写）。</summary>
    [JsonPropertyName("sha256")] public required string Sha256 { get; init; }

    [JsonPropertyName("lengthBytes")] public required long LengthBytes { get; init; }

    /// <summary>恢复后数据目录里的相对落点；数据库快照落在根，其余 files/ 前缀剥掉。</summary>
    [JsonIgnore]
    public string TargetRelativePath => RelativePath.StartsWith("files/", StringComparison.Ordinal)
        ? RelativePath["files/".Length..]
        : RelativePath;
}

/// <summary>备份时记录的数据库快照与"备份时刻的数据库指纹"（计数、稳定 ID 摘要、删除水位等，均无内容原文）。</summary>
public sealed record BackupDatabaseSection
{
    [JsonPropertyName("relativePath")] public string RelativePath { get; init; } = "database/memory.db";

    [JsonPropertyName("integrityCheck")] public required string IntegrityCheck { get; init; }

    [JsonPropertyName("fingerprint")] public required DatabaseFingerprint Fingerprint { get; init; }
}

/// <summary>备份错误的大类：内容核对不符 / 目标位置不安全 / 写入中断。都带人话消息，供上层原样呈现。</summary>
public class BackupIntegrityException(string message) : Exception(message);

/// <summary>恢复目标选择越界（覆盖在用数据、嵌套在备份包内、路径重叠）时的拦截。</summary>
public sealed class RestoreTargetRejectedException(string message) : BackupIntegrityException(message);
