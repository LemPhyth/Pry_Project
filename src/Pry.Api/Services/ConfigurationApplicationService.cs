using System.Text.RegularExpressions;
using Pry.Contracts;
using Pry.Core.Configuration;
using Pry.Core.Memory;
using Pry.Core.Models;

namespace Pry.Api.Services;

public sealed partial class ConfigurationApplicationService(IConfiguration configuration, BackendRuntime runtime,
    ConversationSessionService sessions, MediaAssetStore media, MemoryDatabase database)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public IReadOnlyList<CharacterSummaryResponse> ListCharacters() => runtime.Characters.Select(ToSummary).ToArray();

    public CharacterResponse GetCharacter(string id) => ToResponse(FindCharacter(id));

    public async Task<CharacterResponse> CreateCharacterAsync(SaveCharacterRequest request, CancellationToken token)
    {
        var id = $"character-{Guid.NewGuid():N}";
        return await SaveCharacterAsync(id, request, creating: true, token);
    }

    public Task<CharacterResponse> UpdateCharacterAsync(string id, SaveCharacterRequest request, CancellationToken token)
    {
        ValidateId(id); _ = FindCharacter(id);
        return SaveCharacterAsync(id, request, creating: false, token);
    }

    public ClientPreferencesResponse GetPreferences() => ToResponse(runtime.Preferences);

    public IReadOnlyList<ModelProfileResponse> GetModels() => runtime.ModelProfiles.Select(profile =>
    {
        var tuning = runtime.Preferences.ModelTunings.TryGetValue(profile.Id, out var value) ? value : null;
        var effective = ApplyTuning(profile, tuning);
        return new ModelProfileResponse(profile.Id, profile.DisplayName, profile.Provider, profile.ModelName,
            profile.Capabilities, effective.ContextSize, effective.MaxOutputTokens, effective.Temperature,
            effective.GpuLayers, effective.ComputeDevice, effective.EnableThinking,
            profile.Id == runtime.ActiveTextModelId, profile.Id == runtime.ActiveVisionModelId,
            runtime.Preferences.CustomModels.Any(x => x.Id == profile.Id));
    }).ToArray();

    public Task<ModelProfileResponse> CreateCustomModelAsync(SaveCustomModelRequest request, CancellationToken token) =>
        SaveCustomModelAsync($"custom-{Guid.NewGuid():N}", request, true, token);

    public async Task<ModelProfileResponse> UpdateCustomModelAsync(string id, SaveCustomModelRequest request,
        CancellationToken token)
    {
        ValidateId(id);
        if (!runtime.Preferences.CustomModels.Any(x => x.Id == id)) throw new ResourceNotFoundException("custom_model", id);
        return await SaveCustomModelAsync(id, request, false, token);
    }

    public async Task DeleteCustomModelAsync(string id, CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            if (!runtime.Preferences.CustomModels.Any(x => x.Id == id)) throw new ResourceNotFoundException("custom_model", id);
            if (id == runtime.ActiveTextModelId || id == runtime.ActiveVisionModelId)
                throw new ApiValidationException("id", "不能删除当前选中的模型，请先切换模型");
            var updated = runtime.Preferences with { CustomModels = runtime.Preferences.CustomModels.Where(x => x.Id != id).ToArray() };
            await sessions.ReconfigureAsync(async ct =>
            {
                await JsonConfiguration.SaveAsync(PreferencesPath, updated, ct); await runtime.ReloadAsync(ct);
            }, token);
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<ModelProfileResponse>> UpdateModelSelectionAsync(UpdateModelSelectionRequest request,
        CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            var profiles = runtime.ModelProfiles.ToDictionary(x => x.Id, StringComparer.Ordinal);
            if (!profiles.TryGetValue(request.ActiveModelId, out var text) || !text.Capabilities.Text)
                throw new ApiValidationException("activeModelId", "文字模型不存在或不支持文字输入");
            if (request.ActiveVisionModelId is { } visionId && (!profiles.TryGetValue(visionId, out var vision) || !vision.Capabilities.Vision))
                throw new ApiValidationException("activeVisionModelId", "视觉模型不存在或不支持图片输入");
            if (request.ActiveSpeechModelId is { } speechId && !runtime.SpeechProfiles.Any(x => x.Id == speechId))
                throw new ApiValidationException("activeSpeechModelId", "语音模型不存在");
            var tunings = (request.ModelTunings ?? runtime.Preferences.ModelTunings).ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
            foreach (var item in tunings)
            {
                if (!profiles.ContainsKey(item.Key)) throw new ApiValidationException("modelTunings", $"模型 {item.Key} 不存在");
                ValidateTuning(item.Value);
            }
            var updated = runtime.Preferences with
            {
                ActiveModelId = request.ActiveModelId, ActiveVisionModelId = request.ActiveVisionModelId,
                ActiveSpeechModelId = request.ActiveSpeechModelId, ModelTunings = tunings
            };
            await sessions.ReconfigureAsync(async ct =>
            {
                await JsonConfiguration.SaveAsync(PreferencesPath, updated, ct);
                await runtime.ReloadAsync(ct);
            }, token);
            return GetModels();
        }
        finally { _gate.Release(); }
    }

    public async Task<ClientPreferencesResponse> UpdateAppearanceMediaAsync(UpdateAppearanceMediaRequest request,
        CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            var theme = runtime.Preferences.Theme;
            var backgroundPath = request.ClearBackground ? null : theme.BackgroundImagePath;
            var avatarPath = request.ClearUserAvatar ? null : theme.UserAvatarPath;
            if (request.BackgroundMediaId is not null) backgroundPath = await ResolveImagePathAsync(request.BackgroundMediaId, "backgroundMediaId", token);
            if (request.UserAvatarMediaId is not null) avatarPath = await ResolveImagePathAsync(request.UserAvatarMediaId, "userAvatarMediaId", token);
            var backgroundDisplays = theme.BackgroundDisplays.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
            var avatarDisplays = theme.UserAvatarDisplays.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
            if (backgroundPath is not null && request.BackgroundDisplay is not null) backgroundDisplays[backgroundPath] = ValidateDisplay(request.BackgroundDisplay);
            if (avatarPath is not null && request.UserAvatarDisplay is not null) avatarDisplays[avatarPath] = ValidateDisplay(request.UserAvatarDisplay);
            var updatedTheme = theme with
            {
                BackgroundImagePath = backgroundPath, UserAvatarPath = avatarPath,
                BackgroundHistory = theme.BackgroundHistory.Append(backgroundPath).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                UserAvatarHistory = theme.UserAvatarHistory.Append(avatarPath).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                BackgroundDisplays = backgroundDisplays, UserAvatarDisplays = avatarDisplays
            };
            await sessions.ReconfigureAsync(async ct =>
            {
                await JsonConfiguration.SaveAsync(PreferencesPath, runtime.Preferences with { Theme = updatedTheme }, ct);
                await runtime.RefreshContentAsync(ct);
            }, token);
            return ToResponse(runtime.Preferences);
        }
        finally { _gate.Release(); }
    }

    public string GetAppearancePath(string kind)
    {
        var path = kind switch { "background" => runtime.Preferences.Theme.BackgroundImagePath, "user-avatar" => runtime.Preferences.Theme.UserAvatarPath, _ => null };
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) throw new ResourceNotFoundException("appearance", kind);
        return path;
    }

    public async Task<ClientPreferencesResponse> UpdatePreferencesAsync(UpdateClientPreferencesRequest request,
        CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            var current = runtime.Preferences;
            if (request.SelectedCharacterId is not null) _ = FindCharacter(request.SelectedCharacterId);
            if (request.ActiveConversationId is not null && await database.GetConversationAsync(request.ActiveConversationId, token) is null)
                throw new ResourceNotFoundException("conversation", request.ActiveConversationId);
            var profile = request.UserProfile is null ? current.UserProfile : new UserProfilePreferences
            {
                DisplayName = ContractValidation.Required(request.UserProfile.DisplayName, "userProfile.displayName", 100),
                Signature = Limit(request.UserProfile.Signature, "userProfile.signature", 500)
            };
            var updated = current with
            {
                SelectedCharacterId = request.SelectedCharacterId ?? current.SelectedCharacterId,
                ActiveConversationId = request.ActiveConversationId ?? current.ActiveConversationId,
                UserProfile = profile,
                DesktopPet = request.DesktopPet ?? current.DesktopPet,
                Shortcuts = request.Shortcuts ?? current.Shortcuts,
                TurnTakingOverride = request.TurnTaking ?? current.TurnTakingOverride,
                Theme = request.Theme is null ? current.Theme : ApplyTheme(current.Theme, request.Theme)
            };
            ValidatePreferences(updated);
            await sessions.ReconfigureAsync(async ct =>
            {
                await JsonConfiguration.SaveAsync(PreferencesPath, updated, ct);
                await runtime.RefreshContentAsync(ct);
            }, token);
            return ToResponse(runtime.Preferences);
        }
        finally { _gate.Release(); }
    }

    public string GetAvatarPath(string id)
    {
        var character = FindCharacter(id);
        if (string.IsNullOrWhiteSpace(character.AvatarPath) || !File.Exists(character.AvatarPath))
            throw new ResourceNotFoundException("character_avatar", id);
        return character.AvatarPath;
    }

    private async Task<CharacterResponse> SaveCharacterAsync(string id, SaveCharacterRequest request, bool creating,
        CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            ValidateId(id);
            var existing = creating ? null : FindCharacter(id);
            string? avatarPath = request.ClearAvatar ? null : existing?.AvatarPath;
            if (request.AvatarMediaId is not null)
            {
                var asset = await media.ResolveAsync(request.AvatarMediaId, token);
                if (asset.Metadata.Kind != nameof(ChatAttachmentKind.Image)) throw new ApiValidationException("avatarMediaId", "头像必须是图片资源");
                avatarPath = asset.Path;
            }
            var character = new CharacterDefinition
            {
                Id = id,
                Name = ContractValidation.Required(request.Name, "name", 100),
                CardName = ContractValidation.Required(request.CardName, "cardName", 150),
                UserName = string.IsNullOrWhiteSpace(request.UserName) ? "你" : Limit(request.UserName, "userName", 100),
                Identity = Limit(request.Identity, "identity", 10_000), Personality = Limit(request.Personality, "personality", 10_000),
                SpeechStyle = Limit(request.SpeechStyle, "speechStyle", 5_000),
                BehavioralRules = CleanLines(request.BehavioralRules, "behavioralRules"),
                WorldFacts = CleanLines(request.WorldFacts, "worldFacts"), InitialState = request.InitialState ?? new RuntimeState(),
                Greeting = string.IsNullOrWhiteSpace(request.Greeting) ? "你好。" : Limit(request.Greeting, "greeting", 2_000),
                PromptMode = request.PromptMode, LegacySystemPrompt = Limit(request.LegacySystemPrompt, "legacySystemPrompt", 30_000),
                AvatarPath = avatarPath, AvatarDisplay = ValidateDisplay(request.AvatarDisplay ?? existing?.AvatarDisplay ?? new ImageDisplayPreferences())
            };
            try { JsonConfiguration.Validate(character); }
            catch (InvalidDataException ex) { throw new ApiValidationException("character", ex.Message); }
            await sessions.ReconfigureAsync(async ct =>
            {
                await JsonConfiguration.SaveAsync(Path.Combine(CharacterDirectory, id + ".json"), character, ct);
                await runtime.RefreshContentAsync(ct);
            }, token);
            return ToResponse(FindCharacter(id));
        }
        finally { _gate.Release(); }
    }

    private CharacterDefinition FindCharacter(string id)
    {
        ValidateId(id);
        return runtime.Characters.FirstOrDefault(x => x.Id == id) ?? throw new ResourceNotFoundException("character", id);
    }

    private CharacterSummaryResponse ToSummary(CharacterDefinition value) => new(value.Id, value.Name, value.CardName,
        string.IsNullOrWhiteSpace(value.AvatarPath) ? null : $"/api/v1/characters/{value.Id}/avatar",
        value.Id == runtime.Preferences.SelectedCharacterId);
    private static CharacterResponse ToResponse(CharacterDefinition value) => new(value.SchemaVersion, value.Id, value.Name,
        value.CardName, value.UserName, value.Identity, value.Personality, value.SpeechStyle, value.BehavioralRules,
        value.WorldFacts, value.InitialState, value.Greeting, value.PromptMode, value.LegacySystemPrompt,
        string.IsNullOrWhiteSpace(value.AvatarPath) ? null : $"/api/v1/characters/{value.Id}/avatar", value.AvatarDisplay);

    private static ClientPreferencesResponse ToResponse(UserPreferences value) => new(value.SelectedCharacterId,
        value.ActiveConversationId, value.ActiveModelId, value.ActiveVisionModelId, value.ActiveSpeechModelId,
        value.UserProfile, value.DesktopPet, value.Shortcuts, value.TurnTakingOverride,
        new ClientThemePreferences(value.Theme.ThemeMode, value.Theme.AccentColor, value.Theme.UseGlassEffects,
            value.Theme.LiveSidebarResize, value.Theme.BackgroundDimOpacity, value.Theme.BackgroundImageOpacity,
            value.Theme.BackgroundBlurMode, value.Theme.BackgroundBlurRadius, value.Theme.AvatarSize,
            value.Theme.BubbleFontSize, value.Theme.BubbleMaxWidth, value.Theme.BubbleSpacing),
        string.IsNullOrWhiteSpace(value.Theme.BackgroundImagePath) ? null : "/api/v1/appearance/background",
        string.IsNullOrWhiteSpace(value.Theme.UserAvatarPath) ? null : "/api/v1/appearance/user-avatar");

    private static ThemePreferences ApplyTheme(ThemePreferences current, ClientThemePreferences value) => current with
    {
        ThemeMode = value.ThemeMode, AccentColor = value.AccentColor, UseGlassEffects = value.UseGlassEffects,
        LiveSidebarResize = value.LiveSidebarResize, BackgroundDimOpacity = value.BackgroundDimOpacity,
        BackgroundImageOpacity = value.BackgroundImageOpacity, BackgroundBlurMode = value.BackgroundBlurMode,
        BackgroundBlurRadius = value.BackgroundBlurRadius, AvatarSize = value.AvatarSize,
        BubbleFontSize = value.BubbleFontSize, BubbleMaxWidth = value.BubbleMaxWidth, BubbleSpacing = value.BubbleSpacing
    };

    private static void ValidatePreferences(UserPreferences value)
    {
        var theme = value.Theme;
        if (theme.ThemeMode is not ("system" or "light" or "dark")) throw new ApiValidationException("theme.themeMode", "只支持 system、light 或 dark");
        if (!HexColor().IsMatch(theme.AccentColor)) throw new ApiValidationException("theme.accentColor", "必须是 #RRGGBB 颜色");
        Range(theme.BackgroundDimOpacity, 0, 1, "theme.backgroundDimOpacity"); Range(theme.BackgroundImageOpacity, 0, 1, "theme.backgroundImageOpacity");
        Range(theme.BackgroundBlurRadius, 0, 100, "theme.backgroundBlurRadius"); Range(theme.AvatarSize, 28, 76, "theme.avatarSize");
        Range(theme.BubbleFontSize, 11, 24, "theme.bubbleFontSize"); Range(theme.BubbleMaxWidth, 280, 900, "theme.bubbleMaxWidth");
        Range(theme.BubbleSpacing, 0, 40, "theme.bubbleSpacing");
        if (value.TurnTakingOverride is { } turn && (turn.DebounceMs is < 0 or > 30_000 || turn.MaxPendingMs is < 0 or > 60_000))
            throw new ApiValidationException("turnTaking", "回合等待参数超出范围");
    }

    private static ModelProfile ApplyTuning(ModelProfile profile, ModelTuningPreferences? value) => value is null ? profile : profile with
    {
        ContextSize = value.ContextSize ?? profile.ContextSize, MaxOutputTokens = value.MaxOutputTokens ?? profile.MaxOutputTokens,
        Temperature = value.Temperature ?? profile.Temperature, GpuLayers = value.GpuLayers ?? profile.GpuLayers,
        ComputeDevice = value.ComputeDevice ?? profile.ComputeDevice, EnableThinking = value.EnableThinking ?? profile.EnableThinking
    };

    private static void ValidateTuning(ModelTuningPreferences value)
    {
        if (value.ContextSize is < 256 or > 1_000_000) throw new ApiValidationException("modelTunings.contextSize", "必须在 256 到 1000000 之间");
        if (value.MaxOutputTokens is < 1 or > 100_000) throw new ApiValidationException("modelTunings.maxOutputTokens", "必须在 1 到 100000 之间");
        if (value.Temperature is { } temperature) Range(temperature, 0, 2, "modelTunings.temperature");
        if (value.GpuLayers is < 0 or > 10_000) throw new ApiValidationException("modelTunings.gpuLayers", "必须在 0 到 10000 之间");
        if (value.ComputeDevice is { Length: > 200 }) throw new ApiValidationException("modelTunings.computeDevice", "长度不能超过 200");
    }

    private static ImageDisplayPreferences ValidateDisplay(ImageDisplayPreferences value)
    {
        Range(value.FocusX, 0, 1, "avatarDisplay.focusX"); Range(value.FocusY, 0, 1, "avatarDisplay.focusY"); Range(value.Zoom, 1, 10, "avatarDisplay.zoom"); return value;
    }
    private static IReadOnlyList<string> CleanLines(IEnumerable<string>? values, string field) => (values ?? [])
        .Select(x => x.Trim()).Where(x => x.Length > 0).Select(x => Limit(x, field, 2_000)).Distinct().Take(100).ToArray();
    private static string Limit(string? value, string field, int max) { value ??= ""; if (value.Length > max) throw new ApiValidationException(field, $"长度不能超过 {max}"); return value.Trim(); }
    private static void Range(double value, double min, double max, string field) { if (double.IsNaN(value) || value < min || value > max) throw new ApiValidationException(field, $"必须在 {min} 到 {max} 之间"); }
    private static void ValidateId(string id) { if (!SafeId().IsMatch(id)) throw new ApiValidationException("id", "ID 格式不正确"); }

    private async Task<string> ResolveImagePathAsync(string mediaId, string field, CancellationToken token)
    {
        var asset = await media.ResolveAsync(ContractValidation.Required(mediaId, field, 128), token);
        if (asset.Metadata.Kind != nameof(ChatAttachmentKind.Image)) throw new ApiValidationException(field, "必须引用图片资源");
        return asset.Path;
    }

    private async Task<ModelProfileResponse> SaveCustomModelAsync(string id, SaveCustomModelRequest request,
        bool creating, CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            var provider = request.Provider?.Trim();
            if (provider is not ("local-llama" or "openai-compatible")) throw new ApiValidationException("provider", "只支持 local-llama 或 openai-compatible");
            var baseUrl = ValidateModelUrl(request.BaseUrl, provider);
            string? modelPath = null, mmprojPath = null;
            if (provider == "local-llama")
            {
                modelPath = ValidateGguf(request.LocalModelPath, "localModelPath", required: true);
                mmprojPath = ValidateGguf(request.LocalMmprojPath, "localMmprojPath", required: false);
            }
            var tuning = new ModelTuningPreferences { ContextSize = request.ContextSize, MaxOutputTokens = request.MaxOutputTokens,
                Temperature = request.Temperature, GpuLayers = request.GpuLayers, ComputeDevice = request.ComputeDevice,
                EnableThinking = request.EnableThinking };
            ValidateTuning(tuning);
            var profile = new ModelProfile
            {
                Id = id, DisplayName = ContractValidation.Required(request.DisplayName, "displayName", 150),
                Provider = provider, ModelName = ContractValidation.Required(request.ModelName, "modelName", 200),
                BaseUrl = baseUrl, ModelPath = modelPath, MmprojPath = mmprojPath, Capabilities = request.Capabilities,
                ContextSize = request.ContextSize, MaxOutputTokens = request.MaxOutputTokens, Temperature = request.Temperature,
                GpuLayers = request.GpuLayers, ComputeDevice = Limit(request.ComputeDevice, "computeDevice", 200),
                EnableThinking = request.EnableThinking
            };
            var models = runtime.Preferences.CustomModels.Where(x => x.Id != id).Append(profile).ToArray();
            var updated = runtime.Preferences with { CustomModels = models };
            await sessions.ReconfigureAsync(async ct =>
            {
                await JsonConfiguration.SaveAsync(PreferencesPath, updated, ct); await runtime.ReloadAsync(ct);
            }, token);
            return GetModels().Single(x => x.Id == id);
        }
        finally { _gate.Release(); }
    }

    private static string ValidateModelUrl(string value, string provider)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.UserInfo.Length > 0 || uri.Scheme is not ("http" or "https"))
            throw new ApiValidationException("baseUrl", "必须是不含凭据的 HTTP(S) 绝对地址");
        if (provider == "openai-compatible" && uri.Scheme != "https" && !uri.IsLoopback)
            throw new ApiValidationException("baseUrl", "远程模型必须使用 HTTPS，回环地址可使用 HTTP");
        return uri.ToString().TrimEnd('/');
    }

    private static string? ValidateGguf(string? value, string field, bool required)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required) throw new ApiValidationException(field, "必须选择 GGUF 文件");
            return null;
        }
        if (!Path.IsPathRooted(value) || Path.GetExtension(value) != ".gguf" || !File.Exists(value))
            throw new ApiValidationException(field, "必须是已存在的绝对 .gguf 文件路径");
        using var stream = File.OpenRead(value); var header = new byte[4];
        if (stream.Read(header) != 4 || System.Text.Encoding.ASCII.GetString(header) != "GGUF")
            throw new ApiValidationException(field, "文件头不是有效的 GGUF");
        return Path.GetFullPath(value);
    }

    private string DataDirectory => Path.GetFullPath(configuration["Pry:DataDirectory"] ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PryCompanion"));
    private string CharacterDirectory => Path.Combine(DataDirectory, "characters");
    private string PreferencesPath => Path.Combine(DataDirectory, "preferences.json");
    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9_-]{0,127}$")] private static partial Regex SafeId();
    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")] private static partial Regex HexColor();
}
