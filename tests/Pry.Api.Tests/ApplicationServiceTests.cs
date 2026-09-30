using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Pry.Contracts;
using Pry.Api.Middleware;
using Pry.Api.Services;
using Pry.Core.Intelligence;
using Pry.Core.Memory;
using Pry.Core.Models;
using Xunit;

namespace Pry.Api.Tests;

public sealed class ApplicationServiceTests
{
    [Fact]
    public async Task Interaction_cooldown_maps_to_429_with_retry_metadata()
    {
        var retryAt = DateTimeOffset.UtcNow.AddSeconds(10);
        var middleware = new ApiExceptionMiddleware(_ => throw new InteractionRejectedException(
            "interaction_cooldown", "请稍后再试", retryAt), NullLogger<ApiExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext(); context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(429, context.Response.StatusCode);
        Assert.True(int.Parse(context.Response.Headers.RetryAfter.Single()!) > 0);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
        Assert.Contains("interaction_cooldown", body);
        Assert.Contains("retryAt", body);
    }

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
    public async Task Media_cleanup_removes_only_expired_unreferenced_assets()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pry-media-cleanup-{Guid.NewGuid():N}");
        try
        {
            var configuration = new ConfigurationManager(); configuration["Pry:DataDirectory"] = directory;
            var store = new MediaAssetStore(configuration);
            var png = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0, 0, 0, 0, 0 };
            var retained = await store.SaveAsync(new MemoryStream(png), "retained.png", png.Length,
                TestContext.Current.CancellationToken);
            var orphan = await store.SaveAsync(new MemoryStream(png), "orphan.png", png.Length,
                TestContext.Current.CancellationToken);
            var retainedAsset = await store.ResolveAsync(retained.Id, TestContext.Current.CancellationToken);
            var orphanAsset = await store.ResolveAsync(orphan.Id, TestContext.Current.CancellationToken);

            var result = await store.CleanupOrphansAsync([retainedAsset.Path], DateTimeOffset.UtcNow.AddMinutes(1),
                TestContext.Current.CancellationToken);

            Assert.Equal(1, result.RemovedCount);
            Assert.Equal(png.Length, result.RemovedBytes);
            Assert.True(File.Exists(retainedAsset.Path));
            Assert.False(File.Exists(orphanAsset.Path));
            await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
                store.ResolveAsync(orphan.Id, TestContext.Current.CancellationToken));
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
        await using var fixture = await DatabaseFixture.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Database.EnsureConversationAsync("room", "character", ct);
        var log = new ConversationEventLog(fixture.Database, "room", NullLogger.Instance);
        // 电平触发类不入库：它表达"现在处于哪"，重放会把界面倒回旧状态。
        log.Publish("turn.state", new { state = "ModelThinking" });
        var first = log.Publish("message.created", new { messageId = 42 });
        var second = log.Publish("message.created", new { messageId = 43 });
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await using var reader = log.ReadAsync(first.Sequence, timeout.Token).GetAsyncEnumerator(timeout.Token);
        Assert.True(await reader.MoveNextAsync());
        Assert.Equal(second.Sequence, reader.Current.Sequence);

        var next = reader.MoveNextAsync().AsTask();
        var third = log.Publish("message.created", new { messageId = 44 });
        Assert.True(await next);
        Assert.Equal(third.Sequence, reader.Current.Sequence);
    }

    [Fact]
    public async Task Sequences_are_global_monotonic_and_survive_a_new_log_instance()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Database.EnsureConversationAsync("room-a", "character", ct);
        await fixture.Database.EnsureConversationAsync("room-b", "character", ct);
        var roomA = new ConversationEventLog(fixture.Database, "room-a", NullLogger.Instance);
        var roomB = new ConversationEventLog(fixture.Database, "room-b", NullLogger.Instance);
        var a1 = roomA.Publish("message.created", new { messageId = 1 });
        var b1 = roomB.Publish("message.created", new { messageId = 2 });
        var a2 = roomA.Publish("message.created", new { messageId = 3 });

        // 序号由那张表唯一分配，因此跨会话单调；同一会话内留洞是正常状态，
        // 客户端不得据空洞自判丢失——丢失只由服务端的 stream.resumed 宣告。
        Assert.True(a1.Sequence < b1.Sequence && b1.Sequence < a2.Sequence);
        Assert.True(a2.Sequence - a1.Sequence > 1);

        // 换一个实例（等价于重配置或重启）后旧游标仍然有效：
        // 每会话序号从 1 重来时，带旧游标的客户端会在新序号爬到该值之前静默收不到任何事件。
        var revived = new ConversationEventLog(fixture.Database, "room-a", NullLogger.Instance);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var replayed = new List<ConversationEvent>();
        await foreach (var item in revived.ReadAsync(a1.Sequence, timeout.Token))
        {
            replayed.Add(item);
            break;
        }
        Assert.Equal(a2.Sequence, Assert.Single(replayed).Sequence);
        var window = await fixture.Database.GetProtocolEventWindowAsync("room-a", ct);
        Assert.Equal(a2.Sequence, window.Latest);
        // 会话 b 消耗过序号，所以 a 的流里确实有洞，但补投窗口只按本会话判定。
        Assert.Equal(a1.Sequence, window.FirstRetained);
    }

    [Fact]
    public async Task Level_events_are_never_stored_and_are_not_replayed_to_later_subscribers()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Database.EnsureConversationAsync("room", "character", ct);
        var log = new ConversationEventLog(fixture.Database, "room", NullLogger.Instance);
        var fact = log.Publish("message.created", new { messageId = 7 });
        log.Publish("turn.state", new { state = "ModelThinking" });
        log.Publish("session.ready", new { conversationId = "room" });

        Assert.Equal(1, await fixture.Database.CountProtocolEventsAsync("room", ct));
        Assert.Equal("message.created", (await fixture.Database.ReadProtocolEventsAsync("room", 0, 10, ct)).Single().Type);

        // 迟到的订阅者拿得到事实，拿不到事实之前那条"模型正在思考"——否则界面会被推回旧状态。
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var late = new List<ConversationEvent>();
        await foreach (var item in log.ReadAsync(0, timeout.Token))
        {
            late.Add(item);
            break;
        }
        Assert.Equal([fact.Type], late.Select(x => x.Type));
        Assert.Equal(fact.Sequence, Assert.Single(late).Sequence);
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
        var listProjection = Assert.Single(await service.ListAsync(100, TestContext.Current.CancellationToken));
        Assert.Equal("你好", listProjection.LastMessagePreview);
        Assert.Equal(ChatRole.User, listProjection.LastMessageRole);
        Assert.Equal("text", listProjection.LastMessageKind);
        Assert.Equal(message.CreatedAt, listProjection.LastMessageAt);

        await service.DeleteAsync(conversation.Id, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.GetAsync(conversation.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Conversation_projection_returns_media_kind_without_localized_placeholder()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        await fixture.Database.EnsureConversationAsync("room", "character-a", TestContext.Current.CancellationToken);
        await fixture.Database.AddMessageAsync("room", ChatRole.User, "", "media/asset.png",
            TestContext.Current.CancellationToken);

        var projection = Assert.Single(await fixture.Database.ListConversationsAsync(
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Null(projection.LastMessagePreview);
        Assert.Equal(ChatRole.User, projection.LastMessageRole);
        Assert.Equal("image", projection.LastMessageKind);
        Assert.NotNull(projection.LastMessageAt);
    }

    [Fact]
    public void Model_performance_policy_only_retries_capacity_and_performance_failures()
    {
        var policy = new ModelPerformancePolicy();
        var requested = new ModelProfile
        {
            Id = "test-model", DisplayName = "Test model", ContextSize = 262144, MaxOutputTokens = 32768
        };

        var largeDevice = new Pry.Core.Inference.LlamaComputeDevice(
            "CUDA0", "NVIDIA GeForce RTX 5090", false, 32768, 30000);
        Assert.Equal([262144, 131072, 65536, 32768, 16384, 8192, 4096],
            policy.CreateCandidates(requested, largeDevice).Select(profile => profile.ContextSize));
        Assert.True(policy.IsTooSlow(requested, new Pry.Core.Inference.LlamaServerStartupMetrics(4.5)));
        Assert.True(policy.MayTryLowerContext(new Pry.Core.Inference.ModelRuntimeException(
            "cuda_out_of_memory", "显存不足", true)));
        Assert.Equal("memory_allocation_failed", Pry.Core.Inference.ModelRuntimeException
            .FromProcessExit(1, "fatal: std::bad_alloc").Code);
        Assert.False(policy.MayTryLowerContext(new Pry.Core.Inference.ModelRuntimeException(
            "startup_timeout", "启动超时", true)));
        Assert.Equal(1, policy.Classify(new Pry.Core.Inference.LlamaComputeDevice(
            "CUDA0", "NVIDIA GeForce GTX 1050 Ti", false, 4096, 3900)).Level);
        Assert.Equal(3, policy.Classify(new Pry.Core.Inference.LlamaComputeDevice(
            "CUDA0", "NVIDIA GeForce RTX 4060", false, 8192, 8100)).Level);
        Assert.Equal(5, policy.Classify(largeDevice).Level);
    }

    [Fact]
    public void Settings_reload_decision_is_owned_by_backend_and_ignores_view_only_changes()
    {
        var before = new UserPreferences
        {
            ActiveModelId = "text", ActiveVisionModelId = "vision",
            Theme = new ThemePreferences { MainWindowLayoutMode = MainWindowLayoutModes.Card }
        };
        var viewOnly = before with
        {
            Theme = before.Theme with { MainWindowLayoutMode = MainWindowLayoutModes.Messenger },
            UserProfile = new UserProfilePreferences { DisplayName = "新名字" }
        };

        Assert.False(ConfigurationApplicationService.RequiresModelReload(before, viewOnly));
        Assert.True(ConfigurationApplicationService.RequiresModelReload(before,
            viewOnly with { ActiveModelId = "other" }));
        Assert.True(ConfigurationApplicationService.RequiresModelReload(before,
            viewOnly with
            {
                ModelTunings = new Dictionary<string, ModelTuningPreferences>
                {
                    ["text"] = new() { ContextSize = 8192 }
                }
            }));
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
    public async Task Character_interactions_use_overrides_idempotency_cooldown_and_persisted_events()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        var configuration = new ConfigurationManager(); configuration["Pry:DataDirectory"] = fixture.Directory;
        await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var runtime = new BackendRuntime(configuration, registry, NullLogger<BackendRuntime>.Instance);
        await runtime.StartAsync(TestContext.Current.CancellationToken);
        var profiles = new CharacterInteractionProfileService(configuration, runtime);
        var builtIn = await profiles.GetAsync("author-character", TestContext.Current.CancellationToken);
        Assert.Equal("built-in", builtIn.Source);
        Assert.NotEmpty(builtIn.PokeReplies);

        var saved = await profiles.SaveAsync("author-character", new SaveInteractionProfileRequest(1, 0,
            [new InteractionReplyPresetDto("poke-one", "你戳到我了。", true, 1)],
            [new InteractionActionDto("wave", "挥挥手", true, InteractionResponseModes.Silent, "happy", [])]),
            TestContext.Current.CancellationToken);
        Assert.Equal("user-override", saved.Source);
        await Assert.ThrowsAsync<ApiValidationException>(() => profiles.SaveAsync("author-character",
            new SaveInteractionProfileRequest(1, 0,
                [new InteractionReplyPresetDto("same", "一", true, 1),
                    new InteractionReplyPresetDto("same", "二", true, 1)], []),
            TestContext.Current.CancellationToken));
        Assert.Equal("poke-one", Assert.Single((await profiles.GetAsync("author-character",
            TestContext.Current.CancellationToken)).PokeReplies).Id);

        await fixture.Database.EnsureConversationAsync("room", "author-character",
            TestContext.Current.CancellationToken);
        var media = new MediaAssetStore(configuration);
        await using var sessions = new ConversationSessionService(fixture.Database, runtime, media,
            NullLogger<ConversationSessionService>.Instance, null, profiles);
        var first = await sessions.ExecuteInteractionAsync("room", new ExecuteInteractionRequest("poke", "gesture-1"),
            TestContext.Current.CancellationToken);
        var replay = await sessions.ExecuteInteractionAsync("room", new ExecuteInteractionRequest("poke", "gesture-1"),
            TestContext.Current.CancellationToken);
        Assert.False(first.Replayed); Assert.True(replay.Replayed); Assert.Equal(first.MessageId, replay.MessageId);
        var message = Assert.Single(await fixture.Database.GetRecentMessagesAsync("room", 10,
            TestContext.Current.CancellationToken));
        Assert.Equal(MessageSources.InteractionPreset, message.Source);
        var regeneration = await Assert.ThrowsAsync<InteractionRejectedException>(() => sessions.RegenerateAsync(
            "room", message.Id, TestContext.Current.CancellationToken));
        Assert.Equal("interaction_not_available", regeneration.Code);

        await using var events = sessions.EventsAsync("room", 0, TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        Assert.True(await events.MoveNextAsync()); Assert.Equal("interaction.performed", events.Current.Type);
        Assert.True(await events.MoveNextAsync()); Assert.Equal("message.created", events.Current.Type);
        await Assert.ThrowsAsync<InteractionRejectedException>(() => sessions.ExecuteInteractionAsync("room",
            new ExecuteInteractionRequest("wave", "gesture-1"), TestContext.Current.CancellationToken));

        var silent = await sessions.ExecuteInteractionAsync("room", new ExecuteInteractionRequest("wave", "gesture-2"),
            TestContext.Current.CancellationToken);
        Assert.Equal("presentation_only", silent.Outcome); Assert.Null(silent.MessageId);
        Assert.Single(await fixture.Database.GetRecentMessagesAsync("room", 10, TestContext.Current.CancellationToken));

        await profiles.SaveAsync("author-character", new SaveInteractionProfileRequest(1, 60,
            [new InteractionReplyPresetDto("poke-one", "你戳到我了。", true, 1)], []),
            TestContext.Current.CancellationToken);
        var cooldown = await Assert.ThrowsAsync<InteractionRejectedException>(() => sessions.ExecuteInteractionAsync(
            "room", new ExecuteInteractionRequest("poke", "gesture-3"), TestContext.Current.CancellationToken));
        Assert.Equal("interaction_cooldown", cooldown.Code); Assert.NotNull(cooldown.RetryAt);
    }

    [Fact]
    public async Task Interaction_is_rejected_while_a_user_turn_is_pending()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        var configuration = new ConfigurationManager(); configuration["Pry:DataDirectory"] = fixture.Directory;
        await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var runtime = new BackendRuntime(configuration, registry, NullLogger<BackendRuntime>.Instance);
        await runtime.StartAsync(TestContext.Current.CancellationToken);
        var profiles = new CharacterInteractionProfileService(configuration, runtime);
        await profiles.SaveAsync("author-character", new SaveInteractionProfileRequest(1, 0,
            [new InteractionReplyPresetDto("poke", "回应", true, 1)], []), TestContext.Current.CancellationToken);
        await fixture.Database.EnsureConversationAsync("room", "author-character", TestContext.Current.CancellationToken);
        var media = new MediaAssetStore(configuration);
        await using var sessions = new ConversationSessionService(fixture.Database, runtime, media,
            NullLogger<ConversationSessionService>.Instance, null, profiles);
        var configurationService = new ConfigurationApplicationService(configuration, runtime, sessions, media,
            fixture.Database, Learning(fixture.Database));
        var network = await configurationService.CreateNetworkModelAsync(new SaveNetworkModelRequest("测试文字模型",
            "test", "http://127.0.0.1:1/v1", new ModelCapabilities(), 2048, 64, .5, 0, "remote", false, null),
            TestContext.Current.CancellationToken);
        await configurationService.UpdateModelSelectionAsync(new UpdateModelSelectionRequest(network.Id, null,
            runtime.ActiveSpeechModelId, null), TestContext.Current.CancellationToken);

        await sessions.SubmitAsync("room", new SubmitTurnRequest("稍等，我还在输入"),
            TestContext.Current.CancellationToken);
        var busy = await Assert.ThrowsAsync<InteractionRejectedException>(() => sessions.ExecuteInteractionAsync(
            "room", new ExecuteInteractionRequest("poke", "gesture-busy"), TestContext.Current.CancellationToken));

        Assert.Equal("turn_busy", busy.Code);
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
        var service = new ConfigurationApplicationService(configuration, runtime, sessions, managedMedia, fixture.Database,
            Learning(fixture.Database));
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
        Assert.Equal(MainWindowLayoutModes.Messenger, preferences.Theme.MainWindowLayoutMode);
        Assert.True(File.Exists(Path.Combine(fixture.Directory, "preferences.json")));

        var cardTheme = preferences.Theme with { MainWindowLayoutMode = MainWindowLayoutModes.Card };
        preferences = await service.UpdatePreferencesAsync(new UpdateClientPreferencesRequest(null, null, null,
            null, null, null, cardTheme), TestContext.Current.CancellationToken);
        Assert.Equal(MainWindowLayoutModes.Card, preferences.Theme.MainWindowLayoutMode);
        Assert.Contains("\"mainWindowLayoutMode\": \"card\"",
            await File.ReadAllTextAsync(Path.Combine(fixture.Directory, "preferences.json"),
                TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ApiValidationException>(() => service.UpdatePreferencesAsync(
            new UpdateClientPreferencesRequest(null, null, null, null, null, null,
                cardTheme with { MainWindowLayoutMode = "unsupported" }), TestContext.Current.CancellationToken));
        Assert.Equal(MainWindowLayoutModes.Card, service.GetPreferences().Theme.MainWindowLayoutMode);
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
        var network = await service.CreateNetworkModelAsync(new SaveNetworkModelRequest("网络接口", "remote-model",
            "https://models.example/v1", new ModelCapabilities(), 8192, 512, .7, 0, "remote", false,
            ["自定义", "network"]), TestContext.Current.CancellationToken);
        Assert.Equal("network", network.Source);
        Assert.Equal(["自定义", "network"], network.Tags);
        await service.DeleteNetworkModelAsync(network.Id, TestContext.Current.CancellationToken);
        var networkEmbedding = await service.CreateNetworkModelAsync(new SaveNetworkModelRequest("网络向量", "embed-model",
            "https://models.example/v1", new ModelCapabilities(false, Embeddings: true), 2048, 1, 0, 0, "remote", false,
            ["embedding"]), TestContext.Current.CancellationToken);
        var embeddingStatus = await service.UpdateEmbeddingSelectionAsync(
            new UpdateEmbeddingSettingsRequest(true, networkEmbedding.Id), TestContext.Current.CancellationToken);
        Assert.True(embeddingStatus.Enabled);
        Assert.True(Assert.Single(service.GetModels(), x => x.Id == networkEmbedding.Id).SelectedForEmbedding);
        await Assert.ThrowsAsync<ResourceConflictException>(() =>
            service.DeleteNetworkModelAsync(networkEmbedding.Id, TestContext.Current.CancellationToken));
        await service.UpdateEmbeddingSelectionAsync(new UpdateEmbeddingSettingsRequest(false, null),
            TestContext.Current.CancellationToken);
        await service.DeleteNetworkModelAsync(networkEmbedding.Id, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<ApiValidationException>(() => service.CreateNetworkModelAsync(
            new SaveNetworkModelRequest("无能力模型", "invalid", "https://models.example/v1",
                new ModelCapabilities(false), 2048, 1, 0, 0, "remote", false, null),
            TestContext.Current.CancellationToken));
        var customSpeech = await service.CreateSpeechModelAsync(new SaveSpeechModelRequest("测试语音接口",
            "openai-compatible", "whisper-1", null, "http://127.0.0.1:19997/v1", "zh", 16000),
            TestContext.Current.CancellationToken);
        Assert.True(customSpeech.Custom);
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

        await Assert.ThrowsAsync<ApiValidationException>(() => service.UpdatePreferencesAsync(
            new UpdateClientPreferencesRequest(null, null, null, null, new ShortcutSettings
            {
                Send = "Enter", SendImmediately = "Enter"
            }, null, null), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ApiValidationException>(() => service.UpdatePreferencesAsync(
            new UpdateClientPreferencesRequest(null, null, null, null, null, new TurnTakingSettings
            {
                MinReplyMessages = 5, MaxReplyMessages = 2
            }, null), TestContext.Current.CancellationToken));

        await Assert.ThrowsAsync<ApiValidationException>(() => service.SaveSettingsAsync(new SaveSettingsRequest(
            new UpdateClientPreferencesRequest(null, null,
                new UserProfilePreferences { DisplayName = "不应保存", Signature = "" }, null, null, null, null),
            new UpdateAppearanceMediaRequest(null, false, null, false, null, null),
            new UpdateModelSelectionRequest("missing-model", null, null, null)),
            TestContext.Current.CancellationToken));
        Assert.Equal("测试用户", service.GetPreferences().UserProfile.DisplayName);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.SaveUserProfileAsync(
            new SaveUserProfileRequest(new UserProfilePreferences { DisplayName = "不应保存", Signature = "" },
                "00000000000000000000000000000000", false, new ImageDisplayPreferences()),
            TestContext.Current.CancellationToken));
        Assert.Equal("测试用户", service.GetPreferences().UserProfile.DisplayName);
        var savedProfile = await service.SaveUserProfileAsync(new SaveUserProfileRequest(
            new UserProfilePreferences { DisplayName = "原子用户", Signature = "原子保存" }, background.Id,
            false, new ImageDisplayPreferences { FocusX = .4, FocusY = .7, Zoom = 1.4 }),
            TestContext.Current.CancellationToken);
        Assert.Equal("原子用户", savedProfile.UserProfile.DisplayName);
        Assert.Equal("/api/v1/appearance/user-avatar", savedProfile.UserAvatarUrl);
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
        var profiles = new CharacterInteractionProfileService(configuration, runtime);
        await using var sessions = new ConversationSessionService(fixture.Database, runtime, media,
            NullLogger<ConversationSessionService>.Instance);
        var service = new ConfigurationApplicationService(configuration, runtime, sessions, media, fixture.Database,
            Learning(fixture.Database), null, profiles);
        var builtIn = runtime.Characters[0];
        await Assert.ThrowsAsync<ResourceConflictException>(() =>
            service.DeleteCharacterAsync(builtIn.Id, TestContext.Current.CancellationToken));

        var created = await service.CreateCharacterAsync(new SaveCharacterRequest(
            "待删除角色", "待删除角色-v1", "你", "测试身份", "", "", [], [], new RuntimeState(), "你好。",
            CharacterPromptMode.Structured, "", null, false, new ImageDisplayPreferences()),
            TestContext.Current.CancellationToken);
        await profiles.SaveAsync(created.Id, new SaveInteractionProfileRequest(1, 0,
            [new InteractionReplyPresetDto("poke", "回应", true, 1)], []), TestContext.Current.CancellationToken);
        var interactionPath = profiles.GetOverridePath(created.Id);
        Assert.True(File.Exists(interactionPath));
        await fixture.Database.EnsureConversationAsync("uses-character", created.Id, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<ResourceConflictException>(() =>
            service.DeleteCharacterAsync(created.Id, TestContext.Current.CancellationToken));

        await fixture.Database.DeleteConversationAsync("uses-character", TestContext.Current.CancellationToken);
        await service.DeleteCharacterAsync(created.Id, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(service.ListCharacters(), x => x.Id == created.Id);
        Assert.False(File.Exists(interactionPath));
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

    [Fact]
    public async Task Local_models_are_discovered_persisted_tagged_and_exposed_without_paths()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        var modelsDirectory = Path.Combine(fixture.Directory, "models");
        System.IO.Directory.CreateDirectory(modelsDirectory);
        await File.WriteAllBytesAsync(Path.Combine(modelsDirectory, "qwen3.5-9b-q4_k_m.gguf"), "GGUF-test"u8.ToArray(),
            TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(modelsDirectory, "mmproj-qwen3.5-9b-f16.gguf"), "GGUF-test"u8.ToArray(),
            TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(modelsDirectory, "bge-m3-embedding-q8_0.gguf"), "GGUF-test"u8.ToArray(),
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(modelsDirectory, "not-a-model.gguf"), "invalid",
            TestContext.Current.CancellationToken);

        var configuration = new ConfigurationManager();
        configuration["Pry:DataDirectory"] = fixture.Directory;
        configuration["Pry:ModelsDirectory"] = modelsDirectory;
        await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var runtime = new BackendRuntime(configuration, registry, NullLogger<BackendRuntime>.Instance);
        await runtime.StartAsync(TestContext.Current.CancellationToken);
        var media = new MediaAssetStore(configuration);
        await using var sessions = new ConversationSessionService(fixture.Database, runtime, media,
            NullLogger<ConversationSessionService>.Instance);
        var service = new ConfigurationApplicationService(configuration, runtime, sessions, media, fixture.Database,
            Learning(fixture.Database));

        var local = service.GetModels().Where(x => x.Source == "local").ToArray();
        Assert.Equal(2, local.Length);
        var chat = Assert.Single(local, x => x.Capabilities.Text);
        Assert.True(chat.Capabilities.Vision);
        Assert.Contains("qwen", chat.Tags);
        Assert.Contains("q4_k_m", chat.Tags);
        var embedding = Assert.Single(local, x => x.Capabilities.Embeddings);
        Assert.False(embedding.Capabilities.Text);
        Assert.Contains("embedding", embedding.Tags);
        Assert.DoesNotContain("reasoning", embedding.Tags);
        Assert.DoesNotContain(modelsDirectory, System.Text.Json.JsonSerializer.Serialize(local));
        var embeddingStatus = await service.UpdateEmbeddingSelectionAsync(
            new UpdateEmbeddingSettingsRequest(true, embedding.Id), TestContext.Current.CancellationToken);
        Assert.Equal(embedding.Id, embeddingStatus.ModelId);
        Assert.Equal(embedding.Id, service.GetPreferences().ActiveEmbeddingModelId);
        await service.UpdateEmbeddingSelectionAsync(new UpdateEmbeddingSettingsRequest(false, null),
            TestContext.Current.CancellationToken);

        var tagged = await service.UpdateModelTagsAsync(chat.Id, new UpdateModelTagsRequest(["常用", " RolePlay "]),
            TestContext.Current.CancellationToken);
        Assert.Equal(["常用", "roleplay"], tagged.Tags);
        var scan = await service.ScanModelsAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, scan.FoundCount);
        Assert.Equal(0, scan.AddedCount);
        Assert.Equal(["常用", "roleplay"], Assert.Single(scan.Models, x => x.Id == chat.Id).Tags);
        Assert.Contains("discoveredModels", await File.ReadAllTextAsync(Path.Combine(fixture.Directory, "preferences.json"),
            TestContext.Current.CancellationToken));

        await Assert.ThrowsAsync<ApiValidationException>(() => service.UpdateModelSelectionAsync(
            new UpdateModelSelectionRequest(embedding.Id, null, runtime.ActiveSpeechModelId, null),
            TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ApiValidationException>(() => service.UpdateModelTagsAsync(chat.Id,
            new UpdateModelTagsRequest(Enumerable.Range(0, 21).Select(x => x.ToString()).ToArray()),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Embedding_failure_degrades_to_keyword_memory_search()
    {
        await using var fixture = await DatabaseFixture.CreateAsync();
        var configuration = new ConfigurationManager(); configuration["Pry:DataDirectory"] = fixture.Directory;
        await using var chatRegistry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var runtime = new BackendRuntime(configuration, chatRegistry, NullLogger<BackendRuntime>.Instance);
        await runtime.StartAsync(TestContext.Current.CancellationToken);
        var media = new MediaAssetStore(configuration);
        await using var sessions = new ConversationSessionService(fixture.Database, runtime, media,
            NullLogger<ConversationSessionService>.Instance);
        var configurationService = new ConfigurationApplicationService(configuration, runtime, sessions, media,
            fixture.Database, Learning(fixture.Database));
        var profile = await configurationService.CreateNetworkModelAsync(new SaveNetworkModelRequest(
            "不可用向量服务", "embedding", "http://127.0.0.1:1/v1",
            new ModelCapabilities(false, Embeddings: true), 2048, 1, 0, 0, "remote", false, null),
            TestContext.Current.CancellationToken);
        await configurationService.UpdateEmbeddingSelectionAsync(new UpdateEmbeddingSettingsRequest(true, profile.Id),
            TestContext.Current.CancellationToken);
        await fixture.Database.AddMemoryAsync("c", "fact", "用户喜欢红茶", "红茶", .8, null,
            TestContext.Current.CancellationToken);
        await using var embeddingRegistry = new EmbeddingProcessRegistry(NullLogger<EmbeddingProcessRegistry>.Instance);
        var embeddings = new MemoryEmbeddingService(fixture.Database, runtime, embeddingRegistry,
            NullLogger<MemoryEmbeddingService>.Instance);

        var result = await embeddings.SearchMemoriesAsync("c", "红茶", 6, TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal("degraded", (await embeddings.GetStatusAsync(TestContext.Current.CancellationToken)).State);
    }

    [Fact]
    public void Discovery_recognizes_supported_multimodal_and_roleplay_file_names()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pry-model-tags-{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllBytes(Path.Combine(directory, "InternVL3-2B-Instruct-Q4_K_M.gguf"), "GGUF-test"u8.ToArray());
            File.WriteAllBytes(Path.Combine(directory, "mmproj-InternVL3-2B-Instruct-Q8_0.gguf"), "GGUF-test"u8.ToArray());
            File.WriteAllBytes(Path.Combine(directory, "Luminus-1.5B-Roleplay-Q5_K_M-imat.gguf"), "GGUF-test"u8.ToArray());
            var models = new LocalModelDiscoveryService().Discover(directory).Models;
            Assert.True(Assert.Single(models, x => x.ModelName.StartsWith("InternVL3")).Capabilities.Vision);
            Assert.Contains("roleplay", Assert.Single(models, x => x.ModelName.StartsWith("Luminus")).Tags);
        }
        finally { System.IO.Directory.Delete(directory, true); }
    }

    // 删角色卡时要清空学习状态（RT-003 缺口 ②）：服务现在必须拿到一份真的清理器，构造点由这里给。
    // 判据本体在 CharacterLearningDeletionTests.cs，这里只是让既有构造点保持可编译。
    private static CharacterLearningCleanup Learning(MemoryDatabase database) => new(
        new SqliteCharacterStateRepository(database), new SqliteBehaviorPatternRepository(database),
        new SqliteBehaviorExperienceRepository(database), new SqliteExpressionPatternRepository(database));

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
            SqliteTestPools.Clear(path);
            var root = Path.GetDirectoryName(path)!;
            if (System.IO.Directory.Exists(root)) System.IO.Directory.Delete(root, true);
            return ValueTask.CompletedTask;
        }
    }
}
