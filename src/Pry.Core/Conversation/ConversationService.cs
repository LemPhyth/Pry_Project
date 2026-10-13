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
        await database.EnsureConversationAsync(conversationId, character.Id, cancellationToken);
        var scope = MemoryScope.ForCharacter(character.Id);
        await database.LongTerm.BindConversationAsync(scope, conversationId, cancellationToken);
        var epoch = await database.LongTerm.RevisionAsync(scope, cancellationToken);
        var userId = await database.AddMessageAsync(conversationId, ChatRole.User, text, imagePath, cancellationToken);
        var recent = await database.GetRecentMessagesAsync(conversationId, 20, cancellationToken);
        var stickers = stickerCatalog.Enabled;
        var imagePaths = imagePath is null ? Array.Empty<string>() : new[] { imagePath };
        var imageDescription = imagePath is null ? null : await router.DescribeImagesAsync(text, imagePaths, cancellationToken);
        var model = router.Select(imagePath is not null);
        var context = await new ContextBuilder(database.LongTerm).BuildConversationAsync(
            new PromptContext(character, state, [], recent, stickers, text, imagePath), model.Profile,
            promptBuilder, true, imageDescription: imageDescription, token: cancellationToken);
        var response = new StringBuilder();
        var raw = new StringBuilder();
        var headerResolved = stickers.Count == 0;
        ExpressionIntent? expression = null;
        await foreach (var token in model.StreamAsync(context.SystemPrompt, context.Messages, router.UsesVisionBridge ? null : imagePaths, null, cancellationToken))
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
            await database.AddMessageAsync(conversationId, ChatRole.Assistant, response.ToString(), null, cancellationToken,
                expression?.StickerId, completedMemoryTurn: new(scope, conversationId, $"stream:{userId}", [userId], []),
                memoryEpoch: epoch);
    }

}
