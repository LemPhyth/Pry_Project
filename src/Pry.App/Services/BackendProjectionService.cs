using Pry.Client;
using Pry.Contracts;
using Pry.Core.Configuration;
using Pry.Core.Expression;
using Pry.Core.Inference;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class BackendProjectionService(PryBackendClient api) : IDisposable
{
    private readonly BackendMediaCache _mediaCache = new(api);

    public async Task<DesktopProjection> LoadAsync(CancellationToken token = default)
    {
        var preferences = await api.GetPreferencesAsync(token);
        var modelResponses = await api.GetModelsAsync(token);
        var speechResponses = await api.GetSpeechModelsAsync(token);
        var characters = new List<CharacterDefinition>();
        foreach (var summary in await api.GetCharactersAsync(token))
        {
            var value = await api.GetCharacterAsync(summary.Id, token);
            characters.Add(new CharacterDefinition
            {
                SchemaVersion = value.SchemaVersion, Id = value.Id, Name = value.Name, CardName = value.CardName,
                UserName = value.UserName, Identity = value.Identity, Personality = value.Personality,
                SpeechStyle = value.SpeechStyle, BehavioralRules = value.BehavioralRules, WorldFacts = value.WorldFacts,
                InitialState = value.InitialState, Greeting = value.Greeting, PromptMode = value.PromptMode,
                LegacySystemPrompt = value.LegacySystemPrompt,
                AvatarPath = await _mediaCache.GetPathAsync(value.AvatarUrl, token), AvatarDisplay = value.AvatarDisplay
            });
        }

        var models = modelResponses.Select(ToModel).ToArray();
        var speechModels = speechResponses.Select(ToSpeech).ToArray();
        var backgroundPath = await _mediaCache.GetPathAsync(preferences.BackgroundUrl, token);
        var userAvatarPath = await _mediaCache.GetPathAsync(preferences.UserAvatarUrl, token);
        var theme = new ThemePreferences
        {
            BackgroundImagePath = backgroundPath, BackgroundHistory = backgroundPath is null ? [] : [backgroundPath],
            UserAvatarPath = userAvatarPath, UserAvatarHistory = userAvatarPath is null ? [] : [userAvatarPath],
            ThemeMode = preferences.Theme.ThemeMode, AccentColor = preferences.Theme.AccentColor,
            UseGlassEffects = preferences.Theme.UseGlassEffects, LiveSidebarResize = preferences.Theme.LiveSidebarResize,
            BackgroundDimOpacity = preferences.Theme.BackgroundDimOpacity,
            BackgroundImageOpacity = preferences.Theme.BackgroundImageOpacity,
            BackgroundBlurMode = preferences.Theme.BackgroundBlurMode,
            BackgroundBlurRadius = preferences.Theme.BackgroundBlurRadius, AvatarSize = preferences.Theme.AvatarSize,
            BubbleFontSize = preferences.Theme.BubbleFontSize, BubbleMaxWidth = preferences.Theme.BubbleMaxWidth,
            BubbleSpacing = preferences.Theme.BubbleSpacing
        };
        var customModels = modelResponses.Where(x => x.Custom).Select(ToModel).ToArray();
        var customSpeechModels = speechResponses.Where(x => x.Custom).Select(ToSpeech).ToArray();
        var clientPreferences = new UserPreferences
        {
            SelectedCharacterId = preferences.SelectedCharacterId, ActiveConversationId = preferences.ActiveConversationId,
            ActiveModelId = preferences.ActiveModelId, ActiveVisionModelId = preferences.ActiveVisionModelId,
            ActiveSpeechModelId = preferences.ActiveSpeechModelId, UserProfile = preferences.UserProfile,
            DesktopPet = preferences.DesktopPet, Shortcuts = preferences.Shortcuts,
            TurnTakingOverride = preferences.TurnTaking, Theme = theme, CustomModels = customModels,
            CustomSpeechModels = customSpeechModels
        };
        return new DesktopProjection(clientPreferences, models,
            modelResponses.Where(x => !x.Custom).Select(ToModel).ToArray(), speechModels,
            speechResponses.Where(x => !x.Custom).Select(ToSpeech).ToArray(), characters,
            await LoadStickersAsync(token));
    }

    public async Task<IReadOnlyList<StickerDefinition>> LoadStickersAsync(CancellationToken token = default)
    {
        var stickers = new List<StickerDefinition>();
        foreach (var value in await api.GetStickersAsync(token))
        {
            var path = await _mediaCache.GetPathAsync(value.ContentUrl, token);
            if (path is null) continue;
            stickers.Add(new StickerDefinition
            {
                Id = value.Id, Name = value.Name, FilePath = path, Source = value.Source,
                Emotions = value.Emotions, Situations = value.Situations, AvoidWhen = value.AvoidWhen,
                Intensity = value.Intensity, Enabled = value.Enabled, InteractionRole = value.InteractionRole,
                LikelyBackchannel = value.LikelyBackchannel
            });
        }
        return stickers;
    }

    public void Dispose() => _mediaCache.Dispose();

    private static ModelProfile ToModel(ModelProfileResponse value) => new()
    {
        Id = value.Id, DisplayName = value.DisplayName, Provider = value.Provider, ModelName = value.ModelName,
        BaseUrl = "", Capabilities = value.Capabilities, ContextSize = value.ContextSize,
        MaxOutputTokens = value.MaxOutputTokens, Temperature = value.Temperature, GpuLayers = value.GpuLayers,
        ComputeDevice = value.ComputeDevice, EnableThinking = value.EnableThinking
    };

    private static SpeechModelProfile ToSpeech(SpeechModelResponse value) => new()
    {
        Id = value.Id, DisplayName = value.DisplayName, Provider = value.Provider, ModelName = value.ModelName,
        Language = value.Language, SampleRate = value.SampleRate
    };
}

public sealed record DesktopProjection(UserPreferences Preferences, ModelProfile[] Models,
    ModelProfile[] BuiltInModels, SpeechModelProfile[] SpeechModels, SpeechModelProfile[] BuiltInSpeechModels,
    IReadOnlyList<CharacterDefinition> Characters, IReadOnlyList<StickerDefinition> Stickers);
