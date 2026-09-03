using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Pry.Api.Contracts;
using Pry.Api.Services;
using Pry.Core.Memory;
using Pry.Core.Models;
using Xunit;

namespace Pry.Api.Tests;

public sealed class ApplicationServiceTests
{
    [Fact]
    public async Task Model_registry_reuses_identical_model_configuration()
    {
        await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var profile = new ModelProfile
        {
            Id = "online", DisplayName = "Online", Provider = "openai-compatible",
            BaseUrl = "http://127.0.0.1:19999/v1"
        };
        var first = await registry.GetAsync("unused", profile, TestContext.Current.CancellationToken);
        var second = await registry.GetAsync("unused", profile, TestContext.Current.CancellationToken);
        Assert.Same(first, second);
        Assert.Same(first.Model, second.Model);
    }

    [Fact]
    public async Task Event_log_replays_from_sequence_and_wakes_subscribers()
    {
        var log = new ConversationEventLog();
        var first = log.Publish("turn.state", new { state = "ModelThinking" });
        var second = log.Publish("message.created", new { messageId = 42 });
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await using var reader = log.ReadAsync(first.Sequence, timeout.Token).GetAsyncEnumerator(timeout.Token);
        Assert.True(await reader.MoveNextAsync());
        Assert.Equal(second.Sequence, reader.Current.Sequence);

        var next = reader.MoveNextAsync().AsTask();
        var third = log.Publish("turn.state", new { state = "Idle" });
        Assert.True(await next);
        Assert.Equal(third.Sequence, reader.Current.Sequence);
    }

    [Fact]
    public async Task Conversation_lifecycle_returns_persisted_resources()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        var service = new ConversationApplicationService(fixture.Database);
        var conversation = await service.CreateAsync(new CreateConversationRequest("character-a"), TestContext.Current.CancellationToken);
        Assert.Equal("character-a", conversation.CharacterId);

        var updated = await service.UpdateAsync(conversation.Id, new UpdateConversationRequest("测试对话", true, null), TestContext.Current.CancellationToken);
        Assert.Equal("测试对话", updated.Title);
        Assert.True(updated.IsPinned);

        var message = await service.AddMessageAsync(conversation.Id, new CreateMessageRequest(ChatRole.User, "你好"), TestContext.Current.CancellationToken);
        Assert.Equal(ChatRole.User, message.Role);
        Assert.Single(await service.MessagesAsync(conversation.Id, 10, TestContext.Current.CancellationToken));

        await service.DeleteAsync(conversation.Id, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.GetAsync(conversation.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Client_cannot_forge_assistant_message_or_submit_local_path()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        var service = new ConversationApplicationService(fixture.Database);
        var conversation = await service.CreateAsync(new CreateConversationRequest(null), TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<ApiValidationException>(() => service.AddMessageAsync(conversation.Id,
            new CreateMessageRequest(ChatRole.Assistant, "伪造回复"), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ApiValidationException>(() => service.AddMessageAsync(conversation.Id,
            new CreateMessageRequest(ChatRole.User, "图片", "C:\\secret.txt"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Memory_validates_importance_and_character_ownership()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        var service = new MemoryApplicationService(fixture.Database);
        await Assert.ThrowsAsync<ApiValidationException>(() => service.CreateAsync(
            new CreateMemoryRequest("c", "fact", "内容", "", 1.1), TestContext.Current.CancellationToken));
        var memory = await service.CreateAsync(new CreateMemoryRequest("c", "fact", "用户喜欢红茶", "饮品", .8), TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.UpdateAsync(memory.Id, "other",
            new UpdateMemoryRequest("fact", "修改", "", .5), TestContext.Current.CancellationToken));
    }

    private sealed class DatabaseFixture(string path, MemoryDatabase database) : IAsyncDisposable
    {
        public MemoryDatabase Database { get; } = database;
        public static async Task<DatabaseFixture> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"pry-api-test-{Guid.NewGuid():N}.db");
            var database = new MemoryDatabase(path);
            await database.InitializeAsync(TestContext.Current.CancellationToken);
            return new DatabaseFixture(path, database);
        }
        public ValueTask DisposeAsync()
        {
            SqliteConnection.ClearAllPools();
            foreach (var suffix in new[] { "", "-wal", "-shm" }) if (File.Exists(path + suffix)) File.Delete(path + suffix);
            return ValueTask.CompletedTask;
        }
    }
}
