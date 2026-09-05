using Pry.Contracts;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class AttachmentDraftService
{
    private readonly List<ChatAttachment> _items = [];
    private int? _maximumCount;
    private long? _warningBytes;

    public AttachmentDraftService() { }

    public AttachmentDraftService(int maximumCount, long warningBytes = long.MaxValue)
    {
        _maximumCount = Math.Max(1, maximumCount);
        _warningBytes = Math.Max(1, warningBytes);
    }

    public IReadOnlyList<ChatAttachment> Items => _items;

    public void ApplyPolicy(MediaUploadPolicyResponse policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        _maximumCount = Math.Max(1, policy.MaximumAttachmentsPerTurn);
        _warningBytes = Math.Max(1, policy.WarningThresholdBytes);
    }

    public AttachmentDraftResult AddPaths(IEnumerable<string> paths)
    {
        var rejected = new List<string>();
        var warnings = new List<string>();
        if (_maximumCount is null || _warningBytes is null)
            return new AttachmentDraftResult(["附件策略尚未加载，请稍后重试"], warnings);
        foreach (var sourcePath in paths.Where(File.Exists))
        {
            if (_items.Count >= _maximumCount)
            {
                rejected.Add($"最多同时添加 {_maximumCount} 个附件");
                break;
            }

            var kind = GetKind(sourcePath);
            if (kind is null)
            {
                rejected.Add($"{Path.GetFileName(sourcePath)}：格式不支持");
                continue;
            }

            try
            {
                var file = new FileInfo(sourcePath);
                using (file.OpenRead()) { }
                if (file.Length > _warningBytes.Value)
                    warnings.Add($"{file.Name} 较大（{file.Length / 1024d / 1024d:F1} MiB），上传和处理可能需要更长时间");
                _items.Add(new ChatAttachment(file.FullName, kind.Value, file.Name));
            }
            catch
            {
                rejected.Add($"{Path.GetFileName(sourcePath)}：无法读取");
            }
        }
        return new AttachmentDraftResult(rejected.Distinct().ToArray(), warnings);
    }

    public bool Remove(ChatAttachment attachment) => _items.Remove(attachment);

    public void RemoveRange(IEnumerable<ChatAttachment> attachments)
    {
        foreach (var attachment in attachments) _items.Remove(attachment);
    }

    private static ChatAttachmentKind? GetKind(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif" => ChatAttachmentKind.Image,
        ".txt" => ChatAttachmentKind.Text,
        ".csv" or ".scv" => ChatAttachmentKind.Csv,
        ".docx" => ChatAttachmentKind.Docx,
        _ => null
    };
}

public sealed record AttachmentDraftResult(IReadOnlyList<string> Rejected, IReadOnlyList<string> Warnings);
