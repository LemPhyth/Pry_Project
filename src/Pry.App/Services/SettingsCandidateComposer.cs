using Pry.Core.Models;

namespace Pry.App.Services;

public sealed record SettingsCandidateDraft(
    string TextModelId,
    string? VisionModelId,
    string? SpeechModelId,
    IReadOnlyDictionary<string, ModelTuningPreferences> ModelTunings,
    TurnTakingSettings TurnTaking,
    DesktopPetPreferences DesktopPet,
    ThemePreferences Theme,
    ShortcutSettings Shortcuts,
    string? CharacterId);

public static class SettingsCandidateComposer
{
    public static UserPreferences Compose(UserPreferences source, SettingsCandidateDraft draft)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(draft.TextModelId);
        var tunings = new Dictionary<string, ModelTuningPreferences>(draft.ModelTunings);
        var activeTuning = tunings.TryGetValue(draft.TextModelId, out var selected)
            ? selected with { ActiveModelId = draft.TextModelId }
            : new ModelTuningPreferences { ActiveModelId = draft.TextModelId };
        tunings[draft.TextModelId] = activeTuning;
        return source with
        {
            ActiveModelId = draft.TextModelId,
            ActiveVisionModelId = draft.VisionModelId,
            ActiveSpeechModelId = draft.SpeechModelId,
            ModelTunings = tunings,
            EnableThinking = activeTuning.EnableThinking == true,
            ModelTuning = activeTuning,
            TurnTakingOverride = draft.TurnTaking,
            SelectedCharacterId = draft.CharacterId,
            DesktopPet = draft.DesktopPet,
            Theme = draft.Theme,
            Shortcuts = draft.Shortcuts
        };
    }
}
