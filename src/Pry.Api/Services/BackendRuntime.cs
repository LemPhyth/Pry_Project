using Pry.Contracts;
using Pry.Core.Configuration;
using Pry.Core.Expression;
using Pry.Core.Inference;
using Pry.Core.Models;

namespace Pry.Api.Services;

public sealed class BackendRuntime(IConfiguration configuration, ModelProcessRegistry registry,
    ILogger<BackendRuntime> logger) : IHostedService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AppSettings? _settings;
    private UserPreferences _preferences = new();
    private IReadOnlyList<CharacterDefinition> _characters = [];
    private RuntimeComponents? _components;
    private StickerCatalog? _stickers;
    private Exception? _error;
    private string _state = "starting";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await LoadConfigurationAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _error = ex; _state = "failed";
            logger.LogError(ex, "Backend runtime configuration failed to initialize");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public RuntimeStatusResponse Status => new(_state, ActiveTextModelId, ActiveVisionModelId,
        _error is null ? null : "运行时初始化失败，请查看本机服务日志");
    public string? ActiveTextModelId => _preferences.ActiveModelId ?? _preferences.ModelTuning.ActiveModelId ?? _settings?.ActiveModelId;
    public string? ActiveVisionModelId => _preferences.ActiveVisionModelId ?? _settings?.VisionModelId;
    public TurnTakingSettings TurnSettings => _preferences.TurnTakingOverride ?? _settings?.TurnTaking ?? new TurnTakingSettings();
    public UserPreferences Preferences => _preferences;
    public IReadOnlyList<CharacterDefinition> Characters => _characters;
    public IReadOnlyList<SpeechModelProfile> SpeechProfiles => (_settings?.SpeechModels ?? [])
        .Concat(_preferences.CustomSpeechModels).ToArray();
    public IReadOnlyList<ModelProfile> ModelProfiles => (_settings?.Models ?? []).Concat(_preferences.CustomModels).ToArray();
    public string? ActiveSpeechModelId => _preferences.ActiveSpeechModelId ?? _settings?.ActiveSpeechModelId;
    public StickerCatalog Stickers => _stickers ?? throw new InvalidOperationException("贴纸目录尚未就绪。");

    public async Task<IReadOnlyList<ComputeDeviceResponse>> ListComputeDevicesAsync(CancellationToken token)
    {
        var settings = _settings ?? throw new InvalidOperationException("后端配置尚未就绪。");
        var devices = await LlamaHardwareDetector.ListDevicesAsync(ResolveAssetPath(settings.LlamaServerPath), cancellationToken: token);
        return devices.Select(x => new ComputeDeviceResponse(x.Id, x.Name, x.IsIntegrated)).ToArray();
    }

    public async Task ReloadAsync(CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            _components = null; _settings = null; _characters = []; _preferences = new(); _error = null; _state = "starting";
            await registry.ResetAsync();
            await LoadConfigurationAsync(token);
        }
        finally { _gate.Release(); }
    }

    public async Task RefreshContentAsync(CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            _components = null; _settings = null; _characters = []; _preferences = new(); _stickers = null; _error = null; _state = "starting";
            await LoadConfigurationAsync(token);
        }
        finally { _gate.Release(); }
    }

    public async Task<RuntimeComponents> GetComponentsAsync(CancellationToken token)
    {
        if (_error is not null) throw new InvalidOperationException("后端运行时初始化失败。", _error);
        if (_components is not null) return _components;
        await _gate.WaitAsync(token);
        try
        {
            if (_components is not null) return _components;
            if (_settings is null || _characters.Count == 0) throw new InvalidOperationException("后端配置尚未就绪。");
            _state = "loading_models";
            var profiles = _settings.Models.Concat(_preferences.CustomModels).Select(ResolveProfile).ToDictionary(x => x.Id, StringComparer.Ordinal);
            var textId = ActiveTextModelId ?? throw new InvalidOperationException("未配置文字模型。");
            if (!profiles.TryGetValue(textId, out var textProfile)) throw new InvalidOperationException($"找不到文字模型配置 {textId}。");
            var serverPath = ResolveAssetPath(_settings.LlamaServerPath);
            var textHandle = await registry.GetAsync(serverPath, textProfile, token);
            ModelHandle? visionHandle = null;
            if (ActiveVisionModelId is { } visionId)
            {
                if (visionId == textId) visionHandle = textHandle;
                else if (profiles.TryGetValue(visionId, out var visionProfile)) visionHandle = await registry.GetAsync(serverPath, visionProfile, token);
            }
            var models = visionHandle is null || ReferenceEquals(visionHandle, textHandle)
                ? new[] { textHandle.Model }
                : new[] { textHandle.Model, visionHandle.Model };
            var router = new ModelRouter(models, textId, visionHandle?.Model.Profile.Id);
            _components = new RuntimeComponents(router, Stickers, _characters);
            _state = "ready"; return _components;
        }
        catch (Exception ex) { _error = ex; _state = "failed"; throw; }
        finally { _gate.Release(); }
    }

    private ModelProfile ResolveProfile(ModelProfile profile)
    {
        var tuning = _preferences.ModelTunings.TryGetValue(profile.Id, out var value) ? value : null;
        profile = tuning is null ? profile : profile with
        {
            Temperature = tuning.Temperature ?? profile.Temperature, MaxOutputTokens = tuning.MaxOutputTokens ?? profile.MaxOutputTokens,
            ContextSize = tuning.ContextSize ?? profile.ContextSize, GpuLayers = tuning.GpuLayers ?? profile.GpuLayers,
            ComputeDevice = tuning.ComputeDevice ?? profile.ComputeDevice, EnableThinking = tuning.EnableThinking ?? profile.EnableThinking
        };
        return profile with
        {
            ModelPath = string.IsNullOrWhiteSpace(profile.ModelPath) ? profile.ModelPath : ResolveAssetPath(profile.ModelPath),
            MmprojPath = string.IsNullOrWhiteSpace(profile.MmprojPath) ? profile.MmprojPath : ResolveAssetPath(profile.MmprojPath),
            ApiKey = Environment.GetEnvironmentVariable($"PRY_API_KEY_{profile.Id.Replace('-', '_').ToUpperInvariant()}")
        };
    }

    public SpeechModelProfile ResolveSpeechProfile(string? requestedId = null)
    {
        var id = requestedId ?? ActiveSpeechModelId ?? throw new InvalidOperationException("未选择语音识别模型。");
        var profile = SpeechProfiles.FirstOrDefault(x => x.Id == id) ?? throw new ResourceNotFoundException("speech_model", id);
        return profile with
        {
            ModelPath = string.IsNullOrWhiteSpace(profile.ModelPath) ? profile.ModelPath : ResolveAssetPath(profile.ModelPath),
            ApiKey = Environment.GetEnvironmentVariable($"PRY_SPEECH_API_KEY_{profile.Id.Replace('-', '_').ToUpperInvariant()}")
        };
    }

    private async Task<IReadOnlyList<CharacterDefinition>> LoadCharactersAsync(string resources, string data, CancellationToken token)
    {
        var paths = new List<string> { Path.Combine(resources, "character.json") };
        var legacy = Path.Combine(data, "character.json"); if (File.Exists(legacy)) paths.Add(legacy);
        var directory = Path.Combine(data, "characters");
        if (Directory.Exists(directory)) paths.AddRange(Directory.EnumerateFiles(directory, "*.json"));
        var result = new Dictionary<string, CharacterDefinition>(StringComparer.Ordinal);
        foreach (var path in paths)
        {
            var character = await JsonConfiguration.LoadAsync<CharacterDefinition>(path, token); JsonConfiguration.Validate(character); result[character.Id] = character;
        }
        return result.Values.ToArray();
    }

    private async Task LoadConfigurationAsync(CancellationToken token)
    {
        var resourceDirectory = ResolveResourceDirectory();
        _settings = await JsonConfiguration.LoadAsync<AppSettings>(Path.Combine(resourceDirectory, "appsettings.json"), token);
        var dataDirectory = ResolveDataDirectory();
        var preferencesPath = Path.Combine(dataDirectory, "preferences.json");
        if (File.Exists(preferencesPath)) _preferences = await JsonConfiguration.LoadAsync<UserPreferences>(preferencesPath, token);
        _characters = await LoadCharactersAsync(resourceDirectory, dataDirectory, token);
        _stickers = new StickerCatalog(Path.Combine(resourceDirectory, "Stickers", "manifest.json"), Path.Combine(dataDirectory, "stickers"));
        await _stickers.LoadAsync(token);
        _state = "ready";
    }

    private string ResolveDataDirectory() => Path.GetFullPath(configuration["Pry:DataDirectory"] ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PryCompanion"));
    private static string ResolveResourceDirectory() => Path.Combine(AppContext.BaseDirectory, "Resources");
    private static string ResolveAssetPath(string path)
    {
        if (Path.IsPathRooted(path)) return path;
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            var candidate = Path.Combine(current.FullName, path); if (File.Exists(candidate) || Directory.Exists(candidate)) return candidate;
        }
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }
}

public sealed record RuntimeComponents(ModelRouter Router, StickerCatalog Stickers, IReadOnlyList<CharacterDefinition> Characters)
{
    public CharacterDefinition ResolveCharacter(string? id, string? preferredId) =>
        Characters.FirstOrDefault(x => x.Id == id) ?? Characters.FirstOrDefault(x => x.Id == preferredId) ?? Characters[0];
}
