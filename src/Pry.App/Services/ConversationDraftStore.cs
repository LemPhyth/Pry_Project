namespace Pry.App.Services;

public sealed class ConversationDraftStore
{
    private readonly Dictionary<string, string> _drafts = new(StringComparer.Ordinal);

    public int Count => _drafts.Count;

    public void Save(string conversationId, string? text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        if (string.IsNullOrEmpty(text)) _drafts.Remove(conversationId);
        else _drafts[conversationId] = text;
    }

    public string Switch(string currentConversationId, string? currentText,
        string nextConversationId, bool preserveCurrent = true)
    {
        if (preserveCurrent) Save(currentConversationId, currentText);
        return Restore(nextConversationId);
    }

    public string Restore(string conversationId) => _drafts.GetValueOrDefault(conversationId, "");

    public void Discard(string conversationId) => _drafts.Remove(conversationId);
}
