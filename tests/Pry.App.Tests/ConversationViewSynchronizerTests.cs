using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class ConversationViewSynchronizerTests
{
    [Fact]
    public async Task Late_response_for_previous_conversation_is_ignored()
    {
        var current = "first";
        var pending = new TaskCompletionSource<IReadOnlyList<ChatMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var sideEffects = 0;
        var synchronizer = new ConversationViewSynchronizer(() => current, () => Character(),
            _ => pending.Task, _ => sideEffects++, () => sideEffects++, (_, _) => sideEffects++,
            () => sideEffects++, () => { sideEffects++; return Task.CompletedTask; });
        var reload = synchronizer.ReloadCurrentAsync();
        current = "second";
        pending.SetResult([Message(1)]);
        Assert.False(await reload);
        Assert.Equal(0, sideEffects);
    }

    [Fact]
    public async Task Empty_history_resets_and_shows_current_character_greeting()
    {
        var reset = 0;
        (string Author, string Text)? greeting = null;
        var recreated = 0;
        var refreshed = 0;
        var synchronizer = new ConversationViewSynchronizer(() => "room", () => Character(),
            _ => Task.FromResult<IReadOnlyList<ChatMessage>>([]), _ => throw new Xunit.Sdk.XunitException("rendered"),
            () => reset++, (author, text) => greeting = (author, text), () => recreated++,
            () => { refreshed++; return Task.CompletedTask; });
        Assert.True(await synchronizer.ReloadCurrentAsync());
        Assert.Equal(1, reset);
        Assert.Equal(("Pry", "你好"), greeting);
        Assert.Equal(1, recreated);
        Assert.Equal(1, refreshed);
    }

    [Fact]
    public async Task Existing_history_renders_without_greeting()
    {
        IReadOnlyList<ChatMessage>? rendered = null;
        var messages = new[] { Message(7) };
        var synchronizer = new ConversationViewSynchronizer(() => "room", () => Character(),
            _ => Task.FromResult<IReadOnlyList<ChatMessage>>(messages), value => rendered = value.ToArray(),
            () => throw new Xunit.Sdk.XunitException("reset"), (_, _) => throw new Xunit.Sdk.XunitException("greeting"),
            () => { }, () => Task.CompletedTask);
        Assert.True(await synchronizer.ReloadCurrentAsync());
        Assert.Same(messages[0], Assert.Single(rendered!));
    }

    private static ChatMessage Message(long id) =>
        new(id, "room", ChatRole.User, "hello", DateTimeOffset.Now);

    private static CharacterDefinition Character() => new()
    {
        Id = "pry", Name = "Pry", Identity = "角色", Personality = "温和",
        SpeechStyle = "自然", Greeting = "你好"
    };
}
