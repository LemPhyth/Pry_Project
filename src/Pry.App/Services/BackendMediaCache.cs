using System.Security.Cryptography;
using System.Text;
using Pry.Client;

namespace Pry.App.Services;

public sealed class BackendMediaCache(PryBackendClient api) : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"pry-client-media-{Guid.NewGuid():N}");
    private readonly Dictionary<string, string> _paths = new(StringComparer.Ordinal);

    public async Task<string?> GetPathAsync(string? relativeUrl, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl)) return null;
        if (_paths.TryGetValue(relativeUrl, out var cached) && File.Exists(cached)) return cached;
        var content = await api.DownloadAsync(relativeUrl, token);
        Directory.CreateDirectory(_directory);
        var name = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(relativeUrl))).ToLowerInvariant();
        var path = Path.Combine(_directory, name + Extension(content.ContentType));
        await File.WriteAllBytesAsync(path, content.Bytes, token);
        _paths[relativeUrl] = path;
        return path;
    }

    public void Invalidate(string relativeUrl)
    {
        if (!_paths.Remove(relativeUrl, out var path)) return;
        try { File.Delete(path); } catch { }
    }

    public void Dispose()
    {
        foreach (var path in _paths.Values) try { File.Delete(path); } catch { }
        try { if (Directory.Exists(_directory)) Directory.Delete(_directory, false); } catch { }
        _paths.Clear();
    }

    private static string Extension(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/png" => ".png", "image/webp" => ".webp", "image/gif" => ".gif", "image/bmp" => ".bmp",
        "image/jpeg" => ".jpg", _ => ".bin"
    };
}
