using Pry.Core.Models;
using Pry.Core.Prompting;

namespace Pry.App.Services;

public static class CharacterCardDraftService
{
    public static string Label(CharacterDefinition card) => string.IsNullOrWhiteSpace(card.CardName)
        ? $"{card.Name}-正式-v1"
        : card.CardName;

    public static CharacterDefinition? Build(CharacterDefinition original, CharacterCardDraft draft)
    {
        if (string.IsNullOrWhiteSpace(draft.CardName) || string.IsNullOrWhiteSpace(draft.Name) ||
            draft.Mode == CharacterPromptMode.Structured && string.IsNullOrWhiteSpace(draft.Identity) ||
            draft.Mode == CharacterPromptMode.Legacy && string.IsNullOrWhiteSpace(draft.LegacyPrompt))
            return null;

        return original with
        {
            Id = draft.Id ?? $"character-{Guid.NewGuid():N}",
            CardName = draft.CardName.Trim(),
            Name = draft.Name.Trim(),
            AvatarPath = draft.AvatarPath,
            AvatarDisplay = draft.AvatarDisplay,
            PromptMode = draft.Mode,
            LegacySystemPrompt = draft.LegacyPrompt?.Trim() ?? "",
            UserName = draft.UserName?.Trim() ?? "你",
            Identity = draft.Identity?.Trim() ?? "",
            Personality = draft.Personality?.Trim() ?? "",
            SpeechStyle = draft.SpeechStyle?.Trim() ?? "",
            BehavioralRules = Lines(draft.BehavioralRules),
            WorldFacts = Lines(draft.WorldFacts),
            Greeting = draft.Greeting?.Trim() ?? "你好。"
        };
    }

    public static string[] Lines(string? value) => (value ?? "")
        .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public sealed record CharacterCardDraft(
    string? Id,
    string? CardName,
    string? Name,
    string? AvatarPath,
    ImageDisplayPreferences AvatarDisplay,
    CharacterPromptMode Mode,
    string? LegacyPrompt,
    string? UserName,
    string? Identity,
    string? Personality,
    string? SpeechStyle,
    string? BehavioralRules,
    string? WorldFacts,
    string? Greeting);
