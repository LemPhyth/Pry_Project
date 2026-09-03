using Pry.Core.Abstractions;
using Pry.Core.Models;

namespace Pry.Core.Inference;

public sealed class ModelRouter(IEnumerable<IChatModel> models, string activeModelId, string? visionModelId = null)
{
    private readonly Dictionary<string, IChatModel> _models = models.ToDictionary(x => x.Profile.Id);

    public IChatModel TextModel => _models.TryGetValue(activeModelId, out var model)
        ? model
        : throw new InvalidOperationException("未找到当前文字聊天模型配置。");

    public IChatModel? VisionModel => visionModelId is not null && _models.TryGetValue(visionModelId, out var model) && model.Profile.Capabilities.Vision
        ? model
        : null;

    public bool UsesVisionBridge => VisionModel is not null && VisionModel.Profile.Id != TextModel.Profile.Id;

    public IChatModel Select(bool needsVision)
    {
        if (!needsVision || UsesVisionBridge) return TextModel;
        if (TextModel.Profile.Capabilities.Vision) return TextModel;
        throw new InvalidOperationException("没有可用的图片理解模型。请在模型设置中选择视觉模型。");
    }

    public async Task<string?> DescribeImagesAsync(string userText, IReadOnlyList<string> imagePaths, CancellationToken cancellationToken)
    {
        if (!UsesVisionBridge || imagePaths.Count == 0) return null;
        var prompt = "你是图片理解模块。只客观描述图片中与用户消息相关的信息，不扮演角色，不回答用户，不编造不可见细节。输出简洁的中文描述，供另一个聊天模型继续对话。";
        var messages = new[] { new ChatMessage(0, "vision-bridge", ChatRole.User, string.IsNullOrWhiteSpace(userText) ? "请按顺序描述这些图片。" : userText, DateTimeOffset.UtcNow, imagePaths[0]) };
        var result = new System.Text.StringBuilder();
        await foreach (var token in VisionModel!.StreamAsync(prompt, messages, imagePaths, null, cancellationToken)) result.Append(token);
        return result.ToString().Trim();
    }
}
