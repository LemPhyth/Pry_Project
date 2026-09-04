using Pry.Client;
using Pry.Contracts;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class SettingsSaveService(PryBackendClient api, Func<string, string> imageContentType)
{
    public async Task<IReadOnlyList<ModelProfileResponse>> SaveAsync(UserPreferences candidate,
        string conversationId, Action<string> warning, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(candidate.ActiveModelId);
        var theme = candidate.Theme;
        await api.UpdatePreferencesAsync(new UpdateClientPreferencesRequest(
            candidate.SelectedCharacterId, conversationId, candidate.UserProfile, candidate.DesktopPet,
            candidate.Shortcuts, candidate.TurnTakingOverride,
            new ClientThemePreferences(theme.ThemeMode, theme.AccentColor, theme.UseGlassEffects,
                theme.LiveSidebarResize, theme.BackgroundDimOpacity, theme.BackgroundImageOpacity,
                theme.BackgroundBlurMode, theme.BackgroundBlurRadius, theme.AvatarSize,
                theme.BubbleFontSize, theme.BubbleMaxWidth, theme.BubbleSpacing)), token);

        var backgroundId = await UploadAsync(theme.BackgroundImagePath, warning, token);
        var avatarId = await UploadAsync(theme.UserAvatarPath, warning, token);
        await api.UpdateAppearanceAsync(new UpdateAppearanceMediaRequest(backgroundId,
            theme.BackgroundImagePath is null, avatarId, theme.UserAvatarPath is null,
            DisplayFor(theme.BackgroundImagePath, theme.BackgroundDisplays),
            DisplayFor(theme.UserAvatarPath, theme.UserAvatarDisplays)), token);
        return await api.UpdateModelSelectionAsync(new UpdateModelSelectionRequest(
            candidate.ActiveModelId, candidate.ActiveVisionModelId, candidate.ActiveSpeechModelId,
            candidate.ModelTunings), token);
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
