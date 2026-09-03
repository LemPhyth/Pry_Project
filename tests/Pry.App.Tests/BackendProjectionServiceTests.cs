using System.Net;
using System.Net.Http.Json;
using Pry.App.Services;
using Pry.Client;
using Pry.Contracts;
using Pry.Core.Expression;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class BackendProjectionServiceTests
{
    [Fact]
    public async Task Projection_maps_api_data_and_removes_cached_media_on_dispose()
    {
        using var http = new HttpClient(new ProjectionHandler())
        {
            BaseAddress = new Uri("http://127.0.0.1:5078/")
        };
        string avatarPath;
        string stickerPath;
        using (var service = new BackendProjectionService(new PryBackendClient(http)))
        {
            var projection = await service.LoadAsync(TestContext.Current.CancellationToken);

            Assert.Equal("character-1", projection.Preferences.SelectedCharacterId);
            Assert.Single(projection.BuiltInModels);
            Assert.Single(projection.Preferences.CustomModels);
            Assert.Single(projection.BuiltInSpeechModels);
            Assert.Single(projection.Characters);
            Assert.Single(projection.Stickers);
            avatarPath = Assert.IsType<string>(projection.Characters[0].AvatarPath);
            stickerPath = projection.Stickers[0].FilePath;
            Assert.True(File.Exists(avatarPath));
            Assert.True(File.Exists(stickerPath));
        }

        Assert.False(File.Exists(avatarPath));
        Assert.False(File.Exists(stickerPath));
    }

    private sealed class ProjectionHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var path = request.RequestUri!.AbsolutePath;
            object? value = path switch
            {
                "/api/v1/preferences" => new ClientPreferencesResponse("character-1", "room-1", "model-built-in",
                    null, "speech-built-in", new UserProfilePreferences(), new DesktopPetPreferences(),
                    new ShortcutSettings(), new TurnTakingSettings(), new ClientThemePreferences("dark", "#B148C6",
                        true, true, .3, 1, "none", 0, 42, 14, 560, 10), null, null),
                "/api/v1/models" => new[]
                {
                    new ModelProfileResponse("model-built-in", "Built in", "local-llama", "model", new ModelCapabilities(),
                        4096, 512, .8, 0, "cpu", false, true, false, false),
                    new ModelProfileResponse("model-custom", "Custom", "openai-compatible", "remote", new ModelCapabilities(),
                        4096, 512, .8, 0, "cpu", false, false, false, true)
                },
                "/api/v1/speech/models" => new[]
                {
                    new SpeechModelResponse("speech-built-in", "Speech", "sherpa-onnx", "speech", "zh", 16000,
                        true, true, false)
                },
                "/api/v1/characters" => new[]
                {
                    new CharacterSummaryResponse("character-1", "角色", "角色卡", "/api/v1/media/avatar/content", true)
                },
                "/api/v1/characters/character-1" => new CharacterResponse(1, "character-1", "角色", "角色卡", "用户",
                    "身份", "性格", "语气", [], [], new RuntimeState(), "你好", CharacterPromptMode.Structured, "",
                    "/api/v1/media/avatar/content", new ImageDisplayPreferences()),
                "/api/v1/stickers" => new[]
                {
                    new StickerResponse("sticker-1", "表情", StickerSource.User, ["happy"], [], [], .5, true,
                        "reaction", false, "/api/v1/media/sticker/content")
                },
                _ when path.StartsWith("/api/v1/media/", StringComparison.Ordinal) => null,
                _ => throw new InvalidOperationException($"Unexpected route: {path}")
            };
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = value is null
                ? new ByteArrayContent([137, 80, 78, 71])
                : JsonContent.Create(value, value.GetType());
            if (value is null) response.Content.Headers.ContentType = new("image/png");
            return Task.FromResult(response);
        }
    }
}
