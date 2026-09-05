using Pry.Client;
using Pry.Contracts;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class SettingsSaveService(PryBackendClient api, Func<string, string> imageContentType)
{
    public static string? SelectedModelName(IReadOnlyList<ModelProfileResponse> models, string? modelId) =>
        models.FirstOrDefault(model => model.SelectedForText)?.DisplayName ??
        models.FirstOrDefault(model => string.Equals(model.Id, modelId, StringComparison.Ordinal))?.DisplayName;

    public async Task<SettingsSaveResult> SaveAsync(UserPreferences candidate,
        string conversationId, Action<string> warning, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(candidate.ActiveModelId);
        var theme = candidate.Theme;
        var preferences = new UpdateClientPreferencesRequest(
            candidate.SelectedCharacterId, conversationId, candidate.UserProfile, candidate.DesktopPet,
            candidate.Shortcuts, candidate.TurnTakingOverride,
            new ClientThemePreferences(theme.ThemeMode, theme.AccentColor, theme.UseGlassEffects,
                theme.LiveSidebarResize, theme.BackgroundDimOpacity, theme.BackgroundImageOpacity,
                theme.BackgroundBlurMode, theme.BackgroundBlurRadius, theme.AvatarSize,
                theme.BubbleFontSize, theme.BubbleMaxWidth, theme.BubbleSpacing));

        var backgroundId = await UploadAsync(theme.BackgroundImagePath, warning, token);
        var avatarId = await UploadAsync(theme.UserAvatarPath, warning, token);
        var appearance = new UpdateAppearanceMediaRequest(backgroundId,
            theme.BackgroundImagePath is null, avatarId, theme.UserAvatarPath is null,
            DisplayFor(theme.BackgroundImagePath, theme.BackgroundDisplays),
            DisplayFor(theme.UserAvatarPath, theme.UserAvatarDisplays));
        var models = new UpdateModelSelectionRequest(
            candidate.ActiveModelId, candidate.ActiveVisionModelId, candidate.ActiveSpeechModelId,
            candidate.ModelTunings);
        var response = await api.SaveSettingsAsync(new SaveSettingsRequest(preferences, appearance, models), token);
        return new SettingsSaveResult(ApplyServerProjection(candidate, response.Preferences), response.Models);
    }

    public static UserPreferences ApplyServerProjection(UserPreferences candidate, ClientPreferencesResponse response)
    {
        var serverTheme = response.Theme;
        return candidate with
        {
            SelectedCharacterId = response.SelectedCharacterId,
            ActiveConversationId = response.ActiveConversationId,
            ActiveModelId = response.ActiveModelId,
            ActiveVisionModelId = response.ActiveVisionModelId,
            ActiveSpeechModelId = response.ActiveSpeechModelId,
            UserProfile = response.UserProfile,
            DesktopPet = response.DesktopPet,
            Shortcuts = response.Shortcuts,
            TurnTakingOverride = response.TurnTaking,
            Theme = candidate.Theme with
            {
                ThemeMode = serverTheme.ThemeMode,
                AccentColor = serverTheme.AccentColor,
                UseGlassEffects = serverTheme.UseGlassEffects,
                LiveSidebarResize = serverTheme.LiveSidebarResize,
                BackgroundDimOpacity = serverTheme.BackgroundDimOpacity,
                BackgroundImageOpacity = serverTheme.BackgroundImageOpacity,
                BackgroundBlurMode = serverTheme.BackgroundBlurMode,
                BackgroundBlurRadius = serverTheme.BackgroundBlurRadius,
                AvatarSize = serverTheme.AvatarSize,
                BubbleFontSize = serverTheme.BubbleFontSize,
                BubbleMaxWidth = serverTheme.BubbleMaxWidth,
                BubbleSpacing = serverTheme.BubbleSpacing
            }
        };
    }

    private async Task<string?> UploadAsync(string? path, Action<string> warning, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
        await using var stream = File.OpenRead(path);
        var media = await api.UploadAsync(stream, Path.GetFileName(path), imageContentType(path), token);
        foreach (var message in media.Warnings) warning(message);
        return media.Id;
    }

    private static ImageDisplayPreferences? DisplayFor(string? path,
        IReadOnlyDictionary<string, ImageDisplayPreferences> displays) => path is null
        ? null
        : displays.GetValueOrDefault(path) ?? new ImageDisplayPreferences();
}

public sealed record SettingsSaveResult(UserPreferences Preferences, IReadOnlyList<ModelProfileResponse> Models);
