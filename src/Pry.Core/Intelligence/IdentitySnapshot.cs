using Pry.Core.Models;

namespace Pry.Core.Intelligence;

/// <summary>
/// Stable character anchors only. World facts, mood and relationship never live here,
/// and nothing in the learning path is allowed to write back into it.
/// </summary>
public sealed record IdentitySnapshot
{
    public required string CharacterId { get; init; }
    public required string Name { get; init; }
    public required string CoreIdentity { get; init; }
    public required string PersonalityOutline { get; init; }
    public required string SpeechStyleSeed { get; init; }
    public required string UserName { get; init; }

    /// <summary>Values and limits the character does not trade away for a single interaction.</summary>
    public IReadOnlyList<string> Boundaries { get; init; } = [];

    public MoodBaseline Baseline { get; init; } = new();

    private static readonly string[] BoundaryMarkers =
        ["不要", "不能", "不会", "禁止", "绝不", "拒绝", "保持", "坚持", "必须", "never", "don't", "must not"];

    public static IdentitySnapshot From(CharacterDefinition character)
    {
        var rules = (character.BehavioralRules ?? []).Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
        var boundaries = rules.Where(x => BoundaryMarkers.Any(
            marker => x.Contains(marker, StringComparison.OrdinalIgnoreCase))).ToArray();
        return new IdentitySnapshot
        {
            CharacterId = character.Id,
            Name = character.Name,
            CoreIdentity = character.Identity,
            PersonalityOutline = character.Personality,
            SpeechStyleSeed = character.SpeechStyle,
            UserName = character.UserName,
            Boundaries = boundaries,
            Baseline = MoodBaseline.From(character.Personality)
        };
    }
}
