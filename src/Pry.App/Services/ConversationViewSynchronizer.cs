using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class ConversationViewSynchronizer(
    Func<string> currentConversationId,
    Func<CharacterDefinition?> currentCharacter,
    Func<string, Task<IReadOnlyList<ChatMessage>>> loadMessages,
    Action<IEnumerable<ChatMessage>> render,
    Action reset,
    Action<string, string> showGreeting,
    Action recreateTurnManager,
    Func<Task> refreshRooms)
{
    public async Task<bool> ReloadCurrentAsync()
    {
        var requestedConversationId = currentConversationId();
        var messages = await loadMessages(requestedConversationId);
        if (!string.Equals(requestedConversationId, currentConversationId(), StringComparison.Ordinal))
            return false;

        if (messages.Count == 0)
        {
            reset();
            if (currentCharacter() is { } character) showGreeting(character.Name, character.Greeting);
        }
        else render(messages);
        recreateTurnManager();
        await refreshRooms();
        return true;
    }
}
