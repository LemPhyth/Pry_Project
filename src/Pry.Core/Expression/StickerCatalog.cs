using System.Text.Json;
using Pry.Core.Models;

namespace Pry.Core.Expression;

public sealed class StickerCatalog(string builtInManifestPath, string userDirectory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly List<StickerDefinition> _stickers = [];
    private string UserManifestPath => Path.Combine(userDirectory, "manifest.json");

    public IReadOnlyList<StickerDefinition> Enabled => _stickers.Where(x => x.Enabled && File.Exists(x.FilePath)).ToArray();
    public IReadOnlyList<StickerDefinition> All => _stickers.ToArray();

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _stickers.Clear();
        if (File.Exists(builtInManifestPath))
            _stickers.AddRange(await ReadManifestAsync(builtInManifestPath, StickerSource.BuiltIn, cancellationToken));
        Directory.CreateDirectory(userDirectory);
        if (File.Exists(UserManifestPath))
            _stickers.AddRange(await ReadManifestAsync(UserManifestPath, StickerSource.User, cancellationToken));
    }

    public StickerDefinition? Find(string? id) => id is null ? null : Enabled.FirstOrDefault(x => x.Id == id);

    public async Task<StickerDefinition> ImportAsync(string sourcePath, string name,
        IReadOnlyList<string> emotions, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (extension is not (".png" or ".jpg" or ".jpeg" or ".webp" or ".gif"))
            throw new InvalidDataException("仅支持 PNG、JPEG、WebP 和 GIF 表情包。");
        var id = $"user_{Guid.NewGuid():N}";
        var target = Path.Combine(userDirectory, id + extension);
        await using (var input = File.OpenRead(sourcePath))
        await using (var output = File.Create(target))
            await input.CopyToAsync(output, cancellationToken);
        var sticker = new StickerDefinition
        {
            Id = id, Name = string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(sourcePath) : name.Trim(),
            FilePath = target, Source = StickerSource.User,
            Emotions = emotions.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToArray()
        };
        _stickers.Add(sticker);
        await SaveUsersAsync(cancellationToken);
        return sticker;
    }

    public async Task RemoveUserAsync(string id, CancellationToken cancellationToken = default)
    {
        var sticker = _stickers.FirstOrDefault(x => x.Id == id && x.Source == StickerSource.User);
        if (sticker is null) return;
        _stickers.Remove(sticker);
        if (File.Exists(sticker.FilePath)) File.Delete(sticker.FilePath);
        await SaveUsersAsync(cancellationToken);
    }

    public async Task UpdateUserAsync(string id, string name, IReadOnlyList<string> emotions,
        string interactionRole = "reaction", bool likelyBackchannel = false,
        CancellationToken cancellationToken = default)
    {
        var index = _stickers.FindIndex(x => x.Id == id && x.Source == StickerSource.User);
        if (index < 0) return;
        _stickers[index] = _stickers[index] with
        {
            Name = string.IsNullOrWhiteSpace(name) ? _stickers[index].Name : name.Trim(),
            Emotions = emotions.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToArray(),
            InteractionRole = string.IsNullOrWhiteSpace(interactionRole) ? "reaction" : interactionRole,
            LikelyBackchannel = likelyBackchannel
        };
        await SaveUsersAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<StickerDefinition>> ReadManifestAsync(string path, StickerSource source,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var entries = await JsonSerializer.DeserializeAsync<List<StickerManifestEntry>>(stream, JsonOptions, cancellationToken) ?? [];
        var baseDirectory = Path.GetDirectoryName(path)!;
        return entries.Select(x => new StickerDefinition
        {
            Id = x.Id, Name = x.Name, FilePath = Path.GetFullPath(Path.Combine(baseDirectory, x.File)), Source = source,
            Emotions = x.Emotions, Situations = x.Situations, AvoidWhen = x.AvoidWhen,
            Intensity = Math.Clamp(x.Intensity, 0, 1), Enabled = x.Enabled,
            InteractionRole = x.InteractionRole, LikelyBackchannel = x.LikelyBackchannel
        }).ToArray();
    }

    private async Task SaveUsersAsync(CancellationToken cancellationToken)
    {
        var entries = _stickers.Where(x => x.Source == StickerSource.User).Select(x => new StickerManifestEntry
        {
            Id = x.Id, Name = x.Name, File = Path.GetFileName(x.FilePath), Emotions = x.Emotions,
            Situations = x.Situations, AvoidWhen = x.AvoidWhen, Intensity = x.Intensity, Enabled = x.Enabled,
            InteractionRole = x.InteractionRole, LikelyBackchannel = x.LikelyBackchannel
        }).ToArray();
        await using var stream = File.Create(UserManifestPath);
        await JsonSerializer.SerializeAsync(stream, entries, JsonOptions, cancellationToken);
    }

    private sealed record StickerManifestEntry
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required string File { get; init; }
        public IReadOnlyList<string> Emotions { get; init; } = [];
        public IReadOnlyList<string> Situations { get; init; } = [];
        public IReadOnlyList<string> AvoidWhen { get; init; } = [];
        public double Intensity { get; init; } = 0.5;
        public bool Enabled { get; init; } = true;
        public string InteractionRole { get; init; } = "reaction";
        public bool LikelyBackchannel { get; init; }
    }
}
