using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Pry.Core.Models;

namespace Pry.Api.Services;

public sealed class LocalModelDiscoveryService
{
    private static readonly string[] EmbeddingMarkers = ["embed", "bge-", "e5-", "gte-", "nomic-embed"];
    private static readonly string[] VisionMarkers = ["llava", "vision", "minicpm-v", "qwen2-vl", "qwen2.5-vl", "qwen3-vl", "qwen3.5", "internvl", "smolvlm", "gemma-3"];
    private static readonly string[] ReasoningMarkers = ["deepseek-r1", "qwq", "qwen3", "reasoning"];
    private static readonly string[] Families = ["qwen", "llama", "mistral", "gemma", "deepseek", "phi", "bge", "nomic", "gte", "e5", "luminus", "internvl", "smolvlm"];

    public ModelDiscoveryResult Discover(string modelsDirectory)
    {
        var root = Path.GetFullPath(modelsDirectory);
        if (!Directory.Exists(root)) return new ModelDiscoveryResult(root, []);

        var files = EnumerateGgufFiles(root).Where(IsGguf).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        var projectors = files.Where(IsProjector).ToArray();
        var profiles = files.Where(x => !IsProjector(x)).Select(path => CreateProfile(root, path, projectors)).ToArray();
        return new ModelDiscoveryResult(root, profiles);
    }

    private static IEnumerable<string> EnumerateGgufFiles(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            IEnumerable<string> files;
            IEnumerable<string> directories;
            try
            {
                files = Directory.EnumerateFiles(directory, "*.gguf", SearchOption.TopDirectoryOnly).ToArray();
                directories = Directory.EnumerateDirectories(directory).ToArray();
            }
            catch (UnauthorizedAccessException) { continue; }
            catch (IOException) { continue; }

            foreach (var file in files) yield return Path.GetFullPath(file);
            foreach (var child in directories)
            {
                try
                {
                    if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0) pending.Push(child);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }

    private static bool IsGguf(string path)
    {
        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            Span<byte> header = stackalloc byte[4];
            return stream.Read(header) == 4 && header.SequenceEqual("GGUF"u8);
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    private static ModelProfile CreateProfile(string root, string path, IReadOnlyList<string> projectors)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        var normalized = fileName.ToLowerInvariant();
        var embedding = EmbeddingMarkers.Any(normalized.Contains);
        var visionCandidate = !embedding && VisionMarkers.Any(normalized.Contains);
        var projector = visionCandidate ? FindProjector(fileName, projectors) : null;
        var tags = BuildTags(normalized, embedding, visionCandidate, projector is not null);
        var relative = Path.GetRelativePath(root, path).Replace('\\', '/').ToLowerInvariant();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(relative)))[..16].ToLowerInvariant();
        return new ModelProfile
        {
            Id = $"discovered-{hash}", DisplayName = fileName, Provider = "local-llama", ModelName = fileName,
            ModelPath = path, MmprojPath = projector, Capabilities = new ModelCapabilities(Text: !embedding,
                Vision: projector is not null, Embeddings: embedding), ContextSize = 4096,
            MaxOutputTokens = embedding ? 1 : 512, Temperature = embedding ? 0 : .8,
            ComputeDevice = "auto-discrete", EnableThinking = !embedding && ReasoningMarkers.Any(normalized.Contains), Tags = tags
        };
    }

    private static IReadOnlyList<string> BuildTags(string name, bool embedding, bool visionCandidate, bool vision)
    {
        var tags = new List<string> { "local", "gguf", "auto-discovered" };
        tags.Add(embedding ? "embedding" : "chat");
        if (vision) tags.Add("vision");
        else if (visionCandidate) tags.Add("vision-candidate");
        if (!embedding && ReasoningMarkers.Any(name.Contains)) tags.Add("reasoning");
        if (name.Contains("roleplay")) tags.Add("roleplay");
        if (name.Contains("creativewriter") || name.Contains("creative-writer")) tags.Add("creative-writing");
        if (name.Contains("chinese") || name.Contains("-zh-")) tags.Add("chinese");
        if (name.Contains("multilingual")) tags.Add("multilingual");
        tags.Add(Families.FirstOrDefault(name.Contains) ?? "unknown-family");
        var parameters = Regex.Match(name, @"(?<![a-z0-9])(\d+(?:[._]\d+)?)b(?![a-z0-9])", RegexOptions.IgnoreCase);
        if (parameters.Success) tags.Add(parameters.Value.Replace('_', '.').ToLowerInvariant());
        var quantization = Regex.Match(name, @"(?<![a-z0-9])(iq\d(?:_[a-z0-9]+)*|q\d(?:_[a-z0-9]+)+)(?![a-z0-9])", RegexOptions.IgnoreCase);
        if (quantization.Success) tags.Add(quantization.Value.ToLowerInvariant());
        return tags.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static string? FindProjector(string modelName, IReadOnlyList<string> projectors)
    {
        var modelTokens = SignificantTokens(modelName);
        var ranked = projectors.Select(path => new
        {
            Path = path,
            Score = SignificantTokens(Path.GetFileNameWithoutExtension(path)).Intersect(modelTokens, StringComparer.OrdinalIgnoreCase).Count()
        }).OrderByDescending(x => x.Score).ThenBy(x => x.Path, StringComparer.OrdinalIgnoreCase).ToArray();
        return ranked.Length > 0 && ranked[0].Score >= 2 && (ranked.Length == 1 || ranked[0].Score > ranked[1].Score)
            ? ranked[0].Path : null;
    }

    private static string[] SignificantTokens(string value) => Regex.Split(value.ToLowerInvariant(), "[^a-z0-9.]+")
        .Where(x => x.Length >= 2 && !Regex.IsMatch(x, @"^(gguf|mmproj|f\d+|q\d+|k|m|s|xl)$"))
        .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    private static bool IsProjector(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        return name.Contains("mmproj", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("projector", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record ModelDiscoveryResult(string Directory, IReadOnlyList<ModelProfile> Models);
