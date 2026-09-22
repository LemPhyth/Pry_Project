using System.Text.RegularExpressions;
using Pry.Contracts;
using Pry.Core.Configuration;
using Pry.Core.Models;

namespace Pry.Api.Services;

public sealed partial class CharacterInteractionProfileService(IConfiguration configuration, BackendRuntime runtime)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    public async Task<InteractionProfileResponse> GetAsync(string characterId, CancellationToken token)
    {
        characterId = ValidateId(characterId, "characterId");
        EnsureCharacter(characterId);
        var (profile, source) = await LoadAsync(characterId, token);
        return ToResponse(profile, source);
    }

    public async Task<InteractionProfileResponse> SaveAsync(string characterId, SaveInteractionProfileRequest request,
        CancellationToken token)
    {
        characterId = ValidateId(characterId, "characterId");
        EnsureCharacter(characterId);
        var profile = Validate(characterId, request);
        await _gate.WaitAsync(token);
        try
        {
            await JsonConfiguration.SaveAsync(GetOverridePath(characterId), profile, token);
            return ToResponse(profile, "user-override");
        }
        finally { _gate.Release(); }
    }

    public async Task<CharacterInteractionProfile> GetDomainAsync(string characterId, CancellationToken token)
    {
        characterId = ValidateId(characterId, "characterId");
        EnsureCharacter(characterId);
        return (await LoadAsync(characterId, token)).Profile;
    }

    public string GetOverridePath(string characterId) => Path.Combine(DataDirectory, "interaction-profiles", characterId + ".json");

    private async Task<(CharacterInteractionProfile Profile, string Source)> LoadAsync(string characterId,
        CancellationToken token)
    {
        var overridePath = GetOverridePath(characterId);
        if (File.Exists(overridePath))
            return (ValidateStored(await JsonConfiguration.LoadAsync<CharacterInteractionProfile>(overridePath, token), characterId),
                "user-override");
        var builtInPath = Path.Combine(ResolveResourceDirectory(), "Interactions", characterId + ".json");
        if (File.Exists(builtInPath))
            return (ValidateStored(await JsonConfiguration.LoadAsync<CharacterInteractionProfile>(builtInPath, token), characterId),
                "built-in");
        return (new CharacterInteractionProfile { CharacterId = characterId }, "default");
    }

    private static CharacterInteractionProfile Validate(string characterId, SaveInteractionProfileRequest request)
    {
        if (request.SchemaVersion != 1) throw new ApiValidationException("schemaVersion", "只支持版本 1");
        if (request.CooldownSeconds is < 0 or > 86_400)
            throw new ApiValidationException("cooldownSeconds", "必须在 0 到 86400 之间");
        var pokeReplies = ValidateReplies(request.PokeReplies, "pokeReplies");
        var actions = request.Actions ?? [];
        if (actions.Count > 50) throw new ApiValidationException("actions", "动作数量不能超过 50 个");
        var actionIds = new HashSet<string>(StringComparer.Ordinal);
        var mapped = new List<InteractionActionDefinition>(actions.Count);
        foreach (var action in actions)
        {
            var id = ValidateId(action.Id, "actions.id");
            if (id == "poke") throw new ApiValidationException("actions.id", "poke 是保留动作，不应在动作列表重复声明");
            if (!actionIds.Add(id)) throw new ApiValidationException("actions.id", $"动作 ID {id} 重复");
            var mode = action.ResponseMode?.Trim().ToLowerInvariant();
            if (mode is not (InteractionResponseModes.Preset or InteractionResponseModes.Silent))
                throw new ApiValidationException("actions.responseMode", "只支持 preset 或 silent");
            var replies = ValidateReplies(action.Replies, "actions.replies");
            mapped.Add(new InteractionActionDefinition
            {
                Id = id,
                DisplayName = ContractValidation.Required(action.DisplayName, "actions.displayName", 100),
                Enabled = action.Enabled,
                ResponseMode = mode,
                ExpressionIntent = string.IsNullOrWhiteSpace(action.ExpressionIntent) ? null :
                    ContractValidation.Required(action.ExpressionIntent, "actions.expressionIntent", 64),
                Replies = replies
            });
        }
        return new CharacterInteractionProfile
        {
            SchemaVersion = 1, CharacterId = characterId, CooldownSeconds = request.CooldownSeconds,
            PokeReplies = pokeReplies, Actions = mapped
        };
    }

    private static CharacterInteractionProfile ValidateStored(CharacterInteractionProfile profile, string characterId)
    {
        if (profile.CharacterId != characterId || profile.SchemaVersion != 1)
            throw new InvalidDataException("角色互动配置版本或角色归属无效。");
        var request = new SaveInteractionProfileRequest(profile.SchemaVersion, profile.CooldownSeconds,
            profile.PokeReplies.Select(ToDto).ToArray(), profile.Actions.Select(ToDto).ToArray());
        return Validate(characterId, request);
    }

    private static IReadOnlyList<InteractionReplyPreset> ValidateReplies(
        IReadOnlyList<InteractionReplyPresetDto>? values, string field)
    {
        values ??= [];
        if (values.Count > 100) throw new ApiValidationException(field, "预设数量不能超过 100 条");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<InteractionReplyPreset>(values.Count);
        foreach (var item in values)
        {
            var id = ValidateId(item.Id, field + ".id");
            if (!ids.Add(id)) throw new ApiValidationException(field + ".id", $"预设 ID {id} 重复");
            if (!double.IsFinite(item.Weight) || item.Weight is < .01 or > 100)
                throw new ApiValidationException(field + ".weight", "必须在 0.01 到 100 之间");
            result.Add(new InteractionReplyPreset
            {
                Id = id, Text = ContractValidation.Required(item.Text, field + ".text", 500),
                Enabled = item.Enabled, Weight = item.Weight
            });
        }
        return result;
    }

    private void EnsureCharacter(string characterId)
    {
        if (!runtime.Characters.Any(item => item.Id == characterId))
            throw new ResourceNotFoundException("character", characterId);
    }

    private static InteractionProfileResponse ToResponse(CharacterInteractionProfile profile, string source) =>
        new(profile.SchemaVersion, profile.CharacterId, profile.CooldownSeconds,
            profile.PokeReplies.Select(ToDto).ToArray(), profile.Actions.Select(ToDto).ToArray(), source);

    private static InteractionReplyPresetDto ToDto(InteractionReplyPreset value) =>
        new(value.Id, value.Text, value.Enabled, value.Weight);

    private static InteractionActionDto ToDto(InteractionActionDefinition value) =>
        new(value.Id, value.DisplayName, value.Enabled, value.ResponseMode, value.ExpressionIntent,
            value.Replies.Select(ToDto).ToArray());

    private static string ValidateId(string? value, string field)
    {
        value = ContractValidation.Required(value, field, 128);
        if (!SafeId().IsMatch(value)) throw new ApiValidationException(field, "ID 格式不正确");
        return value;
    }

    private string DataDirectory => Path.GetFullPath(configuration["Pry:DataDirectory"] ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PryCompanion"));
    private static string ResolveResourceDirectory() => Path.Combine(AppContext.BaseDirectory, "Resources");
    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9_-]{0,127}$")] private static partial Regex SafeId();
}
