using System.Runtime.CompilerServices;
using System.Text;
using Pry.Core.Inference;
using Pry.Core.Memory;
using Pry.Core.Models;
using Pry.Core.Prompting;
using Pry.Core.Expression;

namespace Pry.Core.Conversation;

public sealed class ConversationService(MemoryDatabase database, PromptBuilder promptBuilder, ModelRouter router,
    CharacterDefinition character, RuntimeState state, StickerCatalog stickerCatalog)
{
    public async IAsyncEnumerable<ConversationUpdate> SendAsync(string conversationId, string text, string? imagePath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var userId = await database.AddMessageAsync(conversationId, ChatRole.User, text, imagePath, cancellationToken);
        var recent = await database.GetRecentMessagesAsync(conversationId, 20, cancellationToken);
        var memories = await database.SearchMemoriesAsync(character.Id, text, 6, cancellationToken);
        var stickers = stickerCatalog.Enabled;
        var prompt = promptBuilder.Build(new PromptContext(character, state, memories, recent, stickers, text, imagePath));
        var imagePaths = imagePath is null ? Array.Empty<string>() : new[] { imagePath };
        var imageDescription = imagePath is null ? null : await router.DescribeImagesAsync(text, imagePaths, cancellationToken);
        if (!string.IsNullOrWhiteSpace(imageDescription))
            prompt += $"\n本轮图片理解模块的内部描述：\n{imageDescription}\n请结合用户原话自然回应，不要提及内部模型分工。";
        var model = router.Select(imagePath is not null);
        var response = new StringBuilder();
        var raw = new StringBuilder();
        var headerResolved = stickers.Count == 0;
        ExpressionIntent? expression = null;
        await foreach (var token in model.StreamAsync(prompt, recent, router.UsesVisionBridge ? null : imagePaths, null, cancellationToken))
        {
            if (headerResolved)
            {
                response.Append(token);
                yield return new ConversationUpdate(ConversationUpdateKind.Text, token);
                continue;
            }
            raw.Append(token);
            var rawText = raw.ToString();
            if (rawText.Contains(ExpressionProtocolParser.EndMarker, StringComparison.Ordinal) || raw.Length > 2048 ||
                (!ExpressionProtocolParser.StartMarker.StartsWith(rawText, StringComparison.Ordinal) && !rawText.StartsWith(ExpressionProtocolParser.StartMarker, StringComparison.Ordinal)))
            {
                (expression, var visible) = ExpressionProtocolParser.ParseHeader(rawText, stickers);
                headerResolved = true;
                if (expression is not null) yield return new ConversationUpdate(ConversationUpdateKind.Expression, Expression: expression);
                if (visible.Length > 0) { response.Append(visible); yield return new ConversationUpdate(ConversationUpdateKind.Text, visible); }
            }
        }
        if (!headerResolved && raw.Length > 0) { response.Append(raw); yield return new ConversationUpdate(ConversationUpdateKind.Text, raw.ToString()); }
        if (response.Length > 0 || expression?.StickerId is not null)
            await database.AddMessageAsync(conversationId, ChatRole.Assistant, response.ToString(), null, cancellationToken, expression?.StickerId);
        if (LooksMemorable(text)) await database.AddMemoryAsync(character.Id, "user_fact", text.Trim(), ExtractTags(text), 0.65, userId, cancellationToken);
    }

    private static bool LooksMemorable(string text) => text.Length is >= 8 and <= 300 &&
        new[] { "我喜欢", "我不喜欢", "我叫", "记住", "约定", "生日", "以后", "答应" }.Any(text.Contains);

    private static string ExtractTags(string text) => string.Join(',', text.Split([' ', '，', '。', ',', '.'], StringSplitOptions.RemoveEmptyEntries).Take(5));
}
