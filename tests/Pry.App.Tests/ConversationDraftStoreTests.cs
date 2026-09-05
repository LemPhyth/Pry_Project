using Pry.App.Services;
using Xunit;

namespace Pry.App.Tests;

public sealed class ConversationDraftStoreTests
{
    [Fact]
    public void Switching_preserves_exact_text_and_restores_target()
    {
        var drafts = new ConversationDraftStore();
        drafts.Save("second", "target draft");
        var restored = drafts.Switch("first", "  source draft\n", "second");
        Assert.Equal("target draft", restored);
        Assert.Equal("  source draft\n", drafts.Restore("first"));
        Assert.Equal(2, drafts.Count);
    }

    [Fact]
    public void Empty_drafts_are_not_retained()
    {
        var drafts = new ConversationDraftStore();
        drafts.Save("room", "text");
        drafts.Save("room", "");
        Assert.Equal(0, drafts.Count);
        Assert.Equal("", drafts.Restore("room"));
    }

    [Fact]
    public void Deleted_current_room_is_not_recreated_during_switch()
    {
        var drafts = new ConversationDraftStore();
        drafts.Save("deleted", "must disappear");
        drafts.Discard("deleted");
        var restored = drafts.Switch("deleted", "stale input", "remaining", preserveCurrent: false);
        Assert.Equal("", restored);
        Assert.Equal("", drafts.Restore("deleted"));
        Assert.Equal(0, drafts.Count);
    }

    [Fact]
    public void Invalid_conversation_id_is_rejected() =>
        Assert.Throws<ArgumentException>(() => new ConversationDraftStore().Save(" ", "text"));
}
