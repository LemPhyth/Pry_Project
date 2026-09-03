using Microsoft.AspNetCore.Mvc;
using Pry.Contracts;
using Pry.Api.Services;

namespace Pry.Api.Controllers;

[ApiController, Route("api/v1/media")]
public sealed class MediaController(MediaAssetStore store) : ControllerBase
{
    [HttpPost]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<ActionResult<MediaAssetResponse>> Upload(IFormFile file, CancellationToken token)
    {
        if (file is null) throw new ApiValidationException("file", "必须提供文件");
        await using var stream = file.OpenReadStream();
        var result = await store.SaveAsync(stream, file.FileName, file.Length, token);
        if (result.Warnings.Count > 0) Response.Headers.Append("X-Pry-Upload-Warning", "large-file");
        return Created(result.DownloadUrl, result);
    }

    [HttpGet("policy")]
    public MediaUploadPolicyResponse Policy() => new(MediaAssetStore.LargeFileWarningBytes, null, 6);

    [HttpGet("{id}/content")]
    public async Task<IActionResult> Download(string id, CancellationToken token)
    {
        var asset = await store.ResolveAsync(id, token);
        return File(new FileStream(asset.Path, FileMode.Open, FileAccess.Read, FileShare.Read), asset.Metadata.ContentType,
            asset.Metadata.OriginalName, enableRangeProcessing: true);
    }
}
