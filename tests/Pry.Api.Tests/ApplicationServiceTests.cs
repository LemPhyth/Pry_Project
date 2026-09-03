using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using Pry.Contracts;
using Pry.Api.Services;
using Pry.Core.Memory;
using Pry.Core.Models;
using Xunit;

namespace Pry.Api.Tests;

public sealed class ApplicationServiceTests
{
    [Fact]
    public async Task Media_store_sniffs_content_hides_paths_and_resolves_managed_attachment()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pry-media-test-{Guid.NewGuid():N}");
        try
        {
            var configuration = new ConfigurationManager(); configuration["Pry:DataDirectory"] = directory;
            var store = new MediaAssetStore(configuration);
            var png = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0, 0, 0, 0, 0 };
            var saved = await store.SaveAsync(new MemoryStream(png), "../private/avatar.png", png.Length, TestContext.Current.CancellationToken);
            Assert.Equal("avatar.png", saved.Name);
            Assert.Equal("Image", saved.Kind);
            Assert.DoesNotContain(directory, saved.DownloadUrl);
            var attachment = Assert.Single(await store.ResolveAttachmentsAsync([saved.Id], TestContext.Current.CancellationToken));
            Assert.True(attachment.IsImage);
            Assert.True(File.Exists(attachment.Path));
            var warning = store.ToResponse(new MediaMetadata("id", "large.txt", "stored.txt", "text/plain",
                MediaAssetStore.LargeFileWarningBytes, "Text", "hash", DateTimeOffset.UtcNow));
            Assert.NotEmpty(warning.Warnings);

            await Assert.ThrowsAsync<ApiValidationException>(() => store.SaveAsync(
                new MemoryStream("MZ executable"u8.ToArray()), "bad.png", 13, TestContext.Current.CancellationToken));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

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
    public async Task Backend_owns_message_branch_deletion_and_undo()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        var configuration = new ConfigurationManager(); configuration["Pry:DataDirectory"] = fixture.Directory;
        await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var runtime = new BackendRuntime(configuration, registry, NullLogger<BackendRuntime>.Instance);
        await using var sessions = new ConversationSessionService(fixture.Database, runtime,
            new MediaAssetStore(configuration), NullLogger<ConversationSessionService>.Instance);
        await fixture.Database.EnsureConversationAsync("room", "c", TestContext.Current.CancellationToken);
        var first = await fixture.Database.AddMessageAsync("room", ChatRole.User, "第一条", null, TestContext.Current.CancellationToken);
        await fixture.Database.AddMessageAsync("room", ChatRole.Assistant, "回复", null, TestContext.Current.CancellationToken);
        await fixture.Database.AddMessageAsync("room", ChatRole.User, "后续", null, TestContext.Current.CancellationToken);

        var mutation = await sessions.DeleteMessageAsync("room", first, TestContext.Current.CancellationToken);
        Assert.Equal("from_message", mutation.Scope);
        Assert.Equal(3, mutation.RemovedMessageCount);
        Assert.Empty(await fixture.Database.GetRecentMessagesAsync("room", 10, TestContext.Current.CancellationToken));

        await sessions.UndoAsync("room", TestContext.Current.CancellationToken);
        Assert.Equal(3, (await fixture.Database.GetRecentMessagesAsync("room", 10, TestContext.Current.CancellationToken)).Count);
    }

    [Fact]
    public async Task Character_and_client_preferences_are_owned_by_backend_without_path_leaks()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        var configuration = new ConfigurationManager(); configuration["Pry:DataDirectory"] = fixture.Directory;
        await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var runtime = new BackendRuntime(configuration, registry, NullLogger<BackendRuntime>.Instance);
        await runtime.StartAsync(TestContext.Current.CancellationToken);
        await using var sessions = new ConversationSessionService(fixture.Database, runtime,
            new MediaAssetStore(configuration), NullLogger<ConversationSessionService>.Instance);
        var managedMedia = new MediaAssetStore(configuration);
        var service = new ConfigurationApplicationService(configuration, runtime, sessions, managedMedia, fixture.Database);
        var created = await service.CreateCharacterAsync(new SaveCharacterRequest(
            "新角色", "新角色-正式-v1", "你", "陪伴者", "沉稳", "简洁", ["尊重用户"], [],
            new RuntimeState(), "你好。", CharacterPromptMode.Structured, "", null, false, new ImageDisplayPreferences()),
            TestContext.Current.CancellationToken);
        Assert.StartsWith("character-", created.Id);
        Assert.Null(created.AvatarUrl);
        Assert.DoesNotContain(fixture.Directory, System.Text.Json.JsonSerializer.Serialize(created));

        var preferences = await service.UpdatePreferencesAsync(new UpdateClientPreferencesRequest(created.Id, null,
            new UserProfilePreferences { DisplayName = "测试用户", Signature = "签名" }, null, null, null, null),
            TestContext.Current.CancellationToken);
        Assert.Equal(created.Id, preferences.SelectedCharacterId);
        Assert.Equal("测试用户", preferences.UserProfile.DisplayName);
        Assert.True(File.Exists(Path.Combine(fixture.Directory, "preferences.json")));
        var models = service.GetModels();
        var text = Assert.Single(models, x => x.SelectedForText);
        var vision = models.First(x => x.Capabilities.Vision);
        var selected = await service.UpdateModelSelectionAsync(new UpdateModelSelectionRequest(text.Id, vision.Id,
            runtime.ActiveSpeechModelId, new Dictionary<string, ModelTuningPreferences>
            {
                [text.Id] = new() { Temperature = .4, MaxOutputTokens = 256 }
            }), TestContext.Current.CancellationToken);
        Assert.True(Assert.Single(selected, x => x.Id == vision.Id).SelectedForVision);
        Assert.Equal(.4, Assert.Single(selected, x => x.Id == text.Id).Temperature, 3);
        var custom = await service.CreateCustomModelAsync(new SaveCustomModelRequest("测试接口", "openai-compatible",
            "test-model", "http://127.0.0.1:19998/v1", null, null, new ModelCapabilities(), 4096, 256, .5, 0,
            "cpu", false), TestContext.Current.CancellationToken);
        Assert.True(custom.Custom);
        Assert.DoesNotContain("19998", System.Text.Json.JsonSerializer.Serialize(custom));
        await service.DeleteCustomModelAsync(custom.Id, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(service.GetModels(), x => x.Id == custom.Id);
        var customSpeech = await service.CreateSpeechModelAsync(new SaveSpeechModelRequest("测试语音接口",
            "openai-compatible", "whisper-1", null, "http://127.0.0.1:19997/v1", "zh", 16000),
            TestContext.Current.CancellationToken);
        Assert.DoesNotContain("19997", System.Text.Json.JsonSerializer.Serialize(customSpeech));
        await service.UpdateModelSelectionAsync(new UpdateModelSelectionRequest(text.Id, vision.Id,
            customSpeech.Id, null), TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<ResourceConflictException>(() =>
            service.DeleteSpeechModelAsync(customSpeech.Id, TestContext.Current.CancellationToken));
        await service.UpdateModelSelectionAsync(new UpdateModelSelectionRequest(text.Id, vision.Id,
            runtime.SpeechProfiles.First(x => x.Id != customSpeech.Id).Id, null), TestContext.Current.CancellationToken);
        await service.DeleteSpeechModelAsync(customSpeech.Id, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(runtime.SpeechProfiles, x => x.Id == customSpeech.Id);
        await Assert.ThrowsAsync<ApiValidationException>(() => service.CreateCustomModelAsync(new SaveCustomModelRequest(
            "不安全接口", "openai-compatible", "model", "http://example.com/v1", null, null,
            new ModelCapabilities(), 4096, 256, .5, 0, "cpu", false), TestContext.Current.CancellationToken));
        var png = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0, 0, 0, 0, 0 };
        var background = await managedMedia.SaveAsync(new MemoryStream(png), "background.png", png.Length, TestContext.Current.CancellationToken);
        var appearance = await service.UpdateAppearanceMediaAsync(new UpdateAppearanceMediaRequest(background.Id, false,
            null, true, new ImageDisplayPreferences { FocusX = .3, FocusY = .6, Zoom = 1.2 }, null), TestContext.Current.CancellationToken);
        Assert.Equal("/api/v1/appearance/background", appearance.BackgroundUrl);
        Assert.Null(appearance.UserAvatarUrl);
        Assert.True(File.Exists(service.GetAppearancePath("background")));
    }

    [Fact]
    public async Task Character_deletion_rejects_built_in_and_referenced_cards()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        var configuration = new ConfigurationManager(); configuration["Pry:DataDirectory"] = fixture.Directory;
        await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var runtime = new BackendRuntime(configuration, registry, NullLogger<BackendRuntime>.Instance);
        await runtime.StartAsync(TestContext.Current.CancellationToken);
        var media = new MediaAssetStore(configuration);
        await using var sessions = new ConversationSessionService(fixture.Database, runtime, media,
            NullLogger<ConversationSessionService>.Instance);
        var service = new ConfigurationApplicationService(configuration, runtime, sessions, media, fixture.Database);
        var builtIn = runtime.Characters[0];
        await Assert.ThrowsAsync<ResourceConflictException>(() =>
            service.DeleteCharacterAsync(builtIn.Id, TestContext.Current.CancellationToken));

        var created = await service.CreateCharacterAsync(new SaveCharacterRequest(
            "待删除角色", "待删除角色-v1", "你", "测试身份", "", "", [], [], new RuntimeState(), "你好。",
            CharacterPromptMode.Structured, "", null, false, new ImageDisplayPreferences()),
            TestContext.Current.CancellationToken);
        await fixture.Database.EnsureConversationAsync("uses-character", created.Id, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<ResourceConflictException>(() =>
            service.DeleteCharacterAsync(created.Id, TestContext.Current.CancellationToken));

        await fixture.Database.DeleteConversationAsync("uses-character", TestContext.Current.CancellationToken);
        await service.DeleteCharacterAsync(created.Id, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(service.ListCharacters(), x => x.Id == created.Id);
    }

    [Fact]
    public async Task Sticker_management_uses_managed_media_and_hides_storage_path()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        var configuration = new ConfigurationManager(); configuration["Pry:DataDirectory"] = fixture.Directory;
        var media = new MediaAssetStore(configuration);
        await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var runtime = new BackendRuntime(configuration, registry, NullLogger<BackendRuntime>.Instance);
        await runtime.StartAsync(TestContext.Current.CancellationToken);
        await using var sessions = new ConversationSessionService(fixture.Database, runtime, media,
            NullLogger<ConversationSessionService>.Instance);
        var service = new StickerApplicationService(runtime, sessions, media);
        var png = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0, 0, 0, 0, 0 };
        var asset = await media.SaveAsync(new MemoryStream(png), "reaction.png", png.Length, TestContext.Current.CancellationToken);

        var sticker = await service.ImportAsync(new ImportStickerRequest(asset.Id, "开心", ["开心"]), TestContext.Current.CancellationToken);
        Assert.Equal(StickerSource.User, sticker.Source);
        Assert.DoesNotContain(fixture.Directory, System.Text.Json.JsonSerializer.Serialize(sticker));
        var updated = await service.UpdateAsync(sticker.Id, new UpdateStickerRequest("很开心", ["喜悦"], "reaction", false), TestContext.Current.CancellationToken);
        Assert.Equal("很开心", updated.Name);
        await service.DeleteAsync(sticker.Id, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(service.List(), x => x.Id == sticker.Id);
    }

    [Fact]
    public async Task Speech_api_accepts_managed_wav_but_chat_rejects_audio_attachment()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        var configuration = new ConfigurationManager(); configuration["Pry:DataDirectory"] = fixture.Directory;
        var media = new MediaAssetStore(configuration);
        await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var runtime = new BackendRuntime(configuration, registry, NullLogger<BackendRuntime>.Instance);
        await runtime.StartAsync(TestContext.Current.CancellationToken);
        using var speech = new SpeechApplicationService(runtime, media);
        var wav = "RIFF0000WAVE"u8.ToArray();
        var asset = await media.SaveAsync(new MemoryStream(wav), "voice.wav", wav.Length, TestContext.Current.CancellationToken);
        Assert.Equal("Audio", asset.Kind);
        Assert.Contains(speech.ListModels(), x => x.Selected);
        await Assert.ThrowsAsync<ApiValidationException>(() => media.ResolveAttachmentsAsync([asset.Id], TestContext.Current.CancellationToken));
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
        public string Directory => Path.GetDirectoryName(path)!;
        public static async Task<DatabaseFixture> CreateAsync()
        {
            var root = Path.Combine(Path.GetTempPath(), $"pry-api-test-{Guid.NewGuid():N}");
            System.IO.Directory.CreateDirectory(root);
            var path = Path.Combine(root, "memory.db");
            var database = new MemoryDatabase(path);
            await database.InitializeAsync(TestContext.Current.CancellationToken);
            return new DatabaseFixture(path, database);
        }
        public ValueTask DisposeAsync()
        {
            SqliteConnection.ClearAllPools();
            var root = Path.GetDirectoryName(path)!;
            if (System.IO.Directory.Exists(root)) System.IO.Directory.Delete(root, true);
            return ValueTask.CompletedTask;
        }
    }
}
