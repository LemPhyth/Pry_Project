using Pry.Api.Contracts;
using Pry.Core.Models;

namespace Pry.Api.Services;

public sealed class StickerApplicationService(BackendRuntime runtime, ConversationSessionService sessions,
    MediaAssetStore media)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public IReadOnlyList<StickerResponse> List() => runtime.Stickers.All.Select(ToResponse).ToArray();

    public StickerDefinition Get(string id) => runtime.Stickers.All.FirstOrDefault(x => x.Id == id)
        ?? throw new ResourceNotFoundException("sticker", id);

    public async Task<StickerResponse> ImportAsync(ImportStickerRequest request, CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            var asset = await media.ResolveAsync(ContractValidation.Required(request.MediaId, "mediaId", 128), token);
            if (asset.Metadata.Kind != nameof(ChatAttachmentKind.Image)) throw new ApiValidationException("mediaId", "表情必须是图片资源");
            StickerDefinition? sticker = null;
            await sessions.ReconfigureAsync(async ct =>
            {
                sticker = await runtime.Stickers.ImportAsync(asset.Path, ContractValidation.Required(request.Name, "name", 100),
                    CleanTags(request.Emotions), ct);
                await runtime.RefreshContentAsync(ct);
            }, token);
            if (sticker is null) throw new InvalidOperationException("表情导入未完成。");
            return ToResponse(Get(sticker.Id));
        }
        finally { _gate.Release(); }
    }

    public async Task<StickerResponse> UpdateAsync(string id, UpdateStickerRequest request, CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            var current = Get(id);
            if (current.Source != StickerSource.User) throw new ApiValidationException("id", "内置表情不可修改");
            var role = request.InteractionRole?.Trim();
            if (role is not ("reaction" or "backchannel" or "topic")) throw new ApiValidationException("interactionRole", "只支持 reaction、backchannel 或 topic");
            await sessions.ReconfigureAsync(async ct =>
            {
                await runtime.Stickers.UpdateUserAsync(id, ContractValidation.Required(request.Name, "name", 100),
                    CleanTags(request.Emotions), role, request.LikelyBackchannel, ct);
                await runtime.RefreshContentAsync(ct);
            }, token);
            return ToResponse(Get(id));
        }
        finally { _gate.Release(); }
    }

    public async Task DeleteAsync(string id, CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            var current = Get(id);
            if (current.Source != StickerSource.User) throw new ApiValidationException("id", "内置表情不可删除");
            await sessions.ReconfigureAsync(async ct =>
            {
                await runtime.Stickers.RemoveUserAsync(id, ct); await runtime.RefreshContentAsync(ct);
            }, token);
        }
        finally { _gate.Release(); }
    }

    public static StickerResponse ToResponse(StickerDefinition value) => new(value.Id, value.Name, value.Source,
        value.Emotions, value.Situations, value.AvoidWhen, value.Intensity, value.Enabled, value.InteractionRole,
        value.LikelyBackchannel, $"/api/v1/stickers/{value.Id}/content");

    private static IReadOnlyList<string> CleanTags(IEnumerable<string>? values) => (values ?? []).Select(x => x.Trim())
        .Where(x => x.Length > 0).Select(x => x.Length <= 100 ? x : throw new ApiValidationException("emotions", "单个标签不能超过 100 字符"))
        .Distinct(StringComparer.OrdinalIgnoreCase).Take(50).ToArray();
}
