using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Pry.Contracts;
using Pry.Core.Models;

namespace Pry.Api.Services;

public sealed class MediaAssetStore(IConfiguration configuration)
{
    public const long LargeFileWarningBytes = 10 * 1024 * 1024;
    private readonly string _directory = Path.Combine(Path.GetFullPath(configuration["Pry:DataDirectory"] ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PryCompanion")), "media");

    public async Task<MediaAssetResponse> SaveAsync(Stream source, string submittedName, long length,
        CancellationToken token)
    {
        if (length <= 0) throw new ApiValidationException("file", "文件不能为空");
        var name = submittedName.Replace('\\', '/').Split('/')[^1].Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 255) throw new ApiValidationException("file", "文件名不合法");
        Directory.CreateDirectory(_directory);
        var temporaryPath = Path.Combine(_directory, $"upload-{Guid.NewGuid():N}.tmp");
        string? storedPath = null; string? metadataPath = null;
        try
        {
            await using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                             81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await source.CopyToAsync(output, token);
            }
            var detected = await DetectAsync(temporaryPath, name, token);
            var id = Guid.NewGuid().ToString("N");
            var storedName = id + detected.Extension;
            storedPath = Path.Combine(_directory, storedName);
            File.Move(temporaryPath, storedPath);
            var info = new FileInfo(storedPath);
            var metadata = new MediaMetadata(id, name, storedName, detected.ContentType, info.Length, detected.Kind,
                await Sha256Async(storedPath, token), DateTimeOffset.UtcNow);
            metadataPath = MetadataPath(id);
            await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata, JsonOptions), token);
            return ToResponse(metadata);
        }
        catch
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            if (storedPath is not null && File.Exists(storedPath)) File.Delete(storedPath);
            if (metadataPath is not null && File.Exists(metadataPath)) File.Delete(metadataPath);
            throw;
        }
    }

    public async Task<ResolvedMediaAsset> ResolveAsync(string id, CancellationToken token)
    {
        if (id.Length != 32 || id.Any(c => !Uri.IsHexDigit(c))) throw new ResourceNotFoundException("media", id);
        var metadataPath = MetadataPath(id);
        if (!File.Exists(metadataPath)) throw new ResourceNotFoundException("media", id);
        var metadata = JsonSerializer.Deserialize<MediaMetadata>(await File.ReadAllTextAsync(metadataPath, token), JsonOptions)
                       ?? throw new InvalidDataException("媒体元数据损坏。");
        var path = Path.Combine(_directory, metadata.StoredName);
        if (!File.Exists(path) || !Path.GetFullPath(path).StartsWith(Path.GetFullPath(_directory) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase)) throw new ResourceNotFoundException("media", id);
        return new ResolvedMediaAsset(metadata, path);
    }

    public async Task<IReadOnlyList<ChatAttachment>> ResolveAttachmentsAsync(IEnumerable<string>? ids, CancellationToken token)
    {
        var supplied = (ids ?? []).ToArray();
        if (supplied.Length > 6) throw new ApiValidationException("attachmentIds", "每次最多上传 6 个附件");
        var unique = supplied.Distinct(StringComparer.Ordinal).ToArray();
        var result = new List<ChatAttachment>();
        foreach (var id in unique)
        {
            var asset = await ResolveAsync(id, token);
            if (!Enum.TryParse<ChatAttachmentKind>(asset.Metadata.Kind, out var kind))
                throw new ApiValidationException("attachmentIds", $"资源 {id} 不能作为聊天附件");
            result.Add(new ChatAttachment(asset.Path, kind, asset.Metadata.OriginalName));
        }
        return result;
    }

    public MediaAssetResponse ToResponse(MediaMetadata metadata) => new(metadata.Id, metadata.OriginalName,
        metadata.ContentType, metadata.Size, metadata.Kind, metadata.CreatedAt, $"/api/v1/media/{metadata.Id}/content",
        metadata.Size >= LargeFileWarningBytes ? ["文件较大，上传和处理可能耗时，并会占用较多本地磁盘空间"] : []);

    private string MetadataPath(string id) => Path.Combine(_directory, id + ".json");

    private static async Task<DetectedMedia> DetectAsync(string path, string name, CancellationToken token)
    {
        var extension = Path.GetExtension(name).ToLowerInvariant();
        var header = new byte[16];
        await using (var stream = File.OpenRead(path)) _ = await stream.ReadAsync(header, token);
        if (header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) && extension == ".png") return new(".png", "image/png", nameof(ChatAttachmentKind.Image));
        if (header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff && extension is ".jpg" or ".jpeg") return new(".jpg", "image/jpeg", nameof(ChatAttachmentKind.Image));
        if (Encoding.ASCII.GetString(header, 0, 6) is "GIF87a" or "GIF89a" && extension == ".gif") return new(".gif", "image/gif", nameof(ChatAttachmentKind.Image));
        if (Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && Encoding.ASCII.GetString(header, 8, 4) == "WEBP" && extension == ".webp") return new(".webp", "image/webp", nameof(ChatAttachmentKind.Image));
        if (Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && Encoding.ASCII.GetString(header, 8, 4) == "WAVE" && extension == ".wav") return new(".wav", "audio/wav", "Audio");
        if (extension == ".docx" && await IsDocxAsync(path, token)) return new(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", nameof(ChatAttachmentKind.Docx));
        if (extension is ".txt" or ".md" or ".csv" && await IsUtf8TextAsync(path, token))
            return new(extension, extension == ".csv" ? "text/csv" : "text/plain", extension == ".csv" ? nameof(ChatAttachmentKind.Csv) : nameof(ChatAttachmentKind.Text));
        throw new ApiValidationException("file", "仅支持签名有效的 PNG、JPEG、GIF、WebP、WAV、TXT、MD、CSV 或 DOCX 文件");
    }

    private static async Task<bool> IsUtf8TextAsync(string path, CancellationToken token)
    {
        var decoder = new UTF8Encoding(false, true).GetDecoder();
        var bytes = new byte[81920]; var chars = new char[81920];
        try
        {
            await using var stream = File.OpenRead(path);
            while (true)
            {
                var read = await stream.ReadAsync(bytes, token); if (read == 0) break;
                if (bytes.AsSpan(0, read).Contains((byte)0)) return false;
                decoder.Convert(bytes, 0, read, chars, 0, chars.Length, false, out _, out _, out _);
            }
            decoder.Convert([], 0, 0, chars, 0, chars.Length, true, out _, out _, out _);
            return true;
        }
        catch (DecoderFallbackException) { return false; }
    }

    private static Task<bool> IsDocxAsync(string path, CancellationToken token) => Task.Run(() =>
    {
        try
        {
            using var archive = ZipFile.OpenRead(path);
            return archive.GetEntry("[Content_Types].xml") is not null && archive.GetEntry("word/document.xml") is not null;
        }
        catch (InvalidDataException) { return false; }
    }, token);

    private static async Task<string> Sha256Async(string path, CancellationToken token)
    {
        await using var stream = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream, token)).ToLowerInvariant();
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private sealed record DetectedMedia(string Extension, string ContentType, string Kind);
}

public sealed record MediaMetadata(string Id, string OriginalName, string StoredName, string ContentType, long Size,
    string Kind, string Sha256, DateTimeOffset CreatedAt);
public sealed record ResolvedMediaAsset(MediaMetadata Metadata, string Path);
