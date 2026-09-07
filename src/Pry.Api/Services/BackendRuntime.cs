using Pry.Contracts;
using Pry.Core.Configuration;
using Pry.Core.Expression;
using Pry.Core.Inference;
using Pry.Core.Models;

namespace Pry.Api.Services;

public sealed class BackendRuntime(IConfiguration configuration, ModelProcessRegistry registry,
    ILogger<BackendRuntime> logger, ModelPerformancePolicy? performancePolicy = null) : IHostedService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private AppSettings? _settings;
    private UserPreferences _preferences = new();
    private IReadOnlyList<CharacterDefinition> _characters = [];
    private RuntimeComponents? _components;
    private ModelHandle? _activeTextHandle;
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

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _lifetime.Cancel();
        return Task.CompletedTask;
    }

    public RuntimeStatusResponse Status
    {
        get
        {
            var modelError = FindModelError(_error);
            return new RuntimeStatusResponse(_state, ActiveTextModelId, ActiveVisionModelId,
                modelError?.SafeMessage ?? (_error is null ? null : "运行时初始化失败，请查看本机服务日志"))
            {
                ConfigurationState = _settings is null ? (_error is null ? "loading" : "failed") : "ready",
                ModelState = _components is not null ? "ready" : _state == "loading_models" ? "loading" :
                    modelError is not null ? "failed" : "not_loaded",
                ErrorCode = modelError?.Code ?? (_error is null ? null : "runtime_initialization_failed"),
                Retryable = modelError?.Retryable ?? false,
                RequestedContextSize = _activeTextHandle?.RequestedContextSize,
                EffectiveContextSize = _activeTextHandle?.EffectiveContextSize,
                MeasuredTokensPerSecond = _activeTextHandle?.MeasuredTokensPerSecond,
                ModelAdjustmentReason = _activeTextHandle?.AdjustmentReason
            };
        }
    }
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
        var policy = performancePolicy ?? new ModelPerformancePolicy();
        return devices.Select(x =>
        {
            var tier = policy.Classify(x);
            return new ComputeDeviceResponse(x.Id, x.Name, x.IsIntegrated, x.TotalMemoryMiB, x.FreeMemoryMiB,
                tier.Level, tier.Id, tier.MaximumInitialContextSize);
        }).ToArray();
    }

    public async Task ReloadAsync(CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            _components = null; _activeTextHandle = null; _settings = null; _characters = []; _preferences = new(); _error = null; _state = "starting";
            await registry.ResetAsync();
            await LoadConfigurationAsync(token);
        }
        finally { _gate.Release(); }
    }

    public async Task ApplyPreferencesSnapshotAsync(UserPreferences preferences, CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            _preferences = preferences;
            _error = null;
            _state = _components is null ? "ready" : _state;
        }
        finally { _gate.Release(); }
    }

    public async Task RefreshContentAsync(CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            _components = null; _activeTextHandle = null; _settings = null; _characters = []; _preferences = new(); _stickers = null; _error = null; _state = "starting";
            await LoadConfigurationAsync(token);
        }
        finally { _gate.Release(); }
    }

    public async Task<RuntimeComponents> GetComponentsAsync(CancellationToken token)
    {
        if (_error is not null && _settings is null)
            throw new InvalidOperationException("后端运行时初始化失败。", _error);
        if (_components is not null) return _components;
        await _gate.WaitAsync(token);
        try
        {
            if (_components is not null) return _components;
            if (_settings is null || _characters.Count == 0) throw new InvalidOperationException("后端配置尚未就绪。");
            _error = null;
            _state = "loading_models";
            var profiles = _settings.Models.Concat(_preferences.CustomModels).Select(ResolveProfile).ToDictionary(x => x.Id, StringComparer.Ordinal);
            var textId = ActiveTextModelId ?? throw new InvalidOperationException("未配置文字模型。");
            if (!profiles.TryGetValue(textId, out var textProfile)) throw new InvalidOperationException($"找不到文字模型配置 {textId}。");
            var serverPath = ResolveAssetPath(_settings.LlamaServerPath);
            var textHandle = await registry.GetAsync(serverPath, textProfile, _lifetime.Token);
            _activeTextHandle = textHandle;
            ModelHandle? visionHandle = null;
            if (ActiveVisionModelId is { } visionId)
            {
                if (visionId == textId) visionHandle = textHandle;
                else if (profiles.TryGetValue(visionId, out var visionProfile)) visionHandle = await registry.GetAsync(serverPath, visionProfile, _lifetime.Token);
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

    private static ModelRuntimeException? FindModelError(Exception? error)
    {
        while (error is not null)
        {
            if (error is ModelRuntimeException modelError) return modelError;
            error = error.InnerException;
        }
        return null;
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
        if (File.Exists(preferencesPath))
        {
            _preferences = await JsonConfiguration.LoadAsync<UserPreferences>(preferencesPath, token);
            _preferences = MigrateLegacyBundledModelDefaults(_preferences);
        }
        _characters = await LoadCharactersAsync(resourceDirectory, dataDirectory, token);
        _stickers = new StickerCatalog(Path.Combine(resourceDirectory, "Stickers", "manifest.json"), Path.Combine(dataDirectory, "stickers"));
        await _stickers.LoadAsync(token);
        _state = "ready";
    }

    private string ResolveDataDirectory() => Path.GetFullPath(configuration["Pry:DataDirectory"] ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PryCompanion"));

    private static UserPreferences MigrateLegacyBundledModelDefaults(UserPreferences preferences)
    {
        var tunings = preferences.ModelTunings.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        Upgrade("qwen3-1.7b-local", 4096, 512, 32768, 32768, .6);
        Upgrade("qwen3.5-9b-local", 4096, 512, 262144, 32768, 1.0);
        Upgrade("qwen2.5-vl-3b-local", 4096, 384, 32768, 8192, null);
        return preferences with { ModelTunings = tunings };

        void Upgrade(string id, int oldContext, int oldOutput, int newContext, int newOutput, double? temperature)
        {
            if (!tunings.TryGetValue(id, out var tuning) ||
                tuning.ContextSize != oldContext || tuning.MaxOutputTokens != oldOutput) return;
            tunings[id] = tuning with
            {
                ContextSize = newContext,
                MaxOutputTokens = newOutput,
                Temperature = temperature ?? tuning.Temperature
            };
        }
    }

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
