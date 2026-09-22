using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Pry.Api.Services;
using Pry.Core.Memory;
using Pry.Core.Models;
using Xunit;

namespace Pry.Api.Tests;

public sealed class ProactiveGeneratorTests
{
    [Theory]
    [InlineData("聊聊你家猫咪怎么样？", null)]
    [InlineData("", "empty_output")]
    [InlineData("<think>秘密推理</think>", "invalid_output")]
    public async Task Real_http_adapter_uses_role_memory_context_and_bounded_non_thinking_request(string output, string? error)
    {
        var ct = TestContext.Current.CancellationToken;
        var directory = Path.Combine(Path.GetTempPath(), $"pry-generator-{Guid.NewGuid():N}");
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0"); builder.Logging.ClearProviders();
        await using var server = builder.Build();
        JsonElement payload = default;
        server.MapPost("/v1/chat/completions", async (HttpContext context) =>
        {
            using var json = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted);
            payload = json.RootElement.Clone();
            context.Response.ContentType = "text/event-stream";
            var chunk = JsonSerializer.Serialize(new { choices = new[] { new { delta = new { content = output } } } });
            await context.Response.WriteAsync($"data: {chunk}\n\ndata: [DONE]\n\n", context.RequestAborted);
        });
        await server.StartAsync(ct);
        try
        {
            var address = server.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var configuration = new ConfigurationManager(); configuration["Pry:DataDirectory"] = directory;
            configuration["Pry:ModelsDirectory"] = Path.Combine(directory, "models");
            await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
            var runtime = new BackendRuntime(configuration, registry, NullLogger<BackendRuntime>.Instance);
            await runtime.StartAsync(ct);
            var database = new MemoryDatabase(Path.Combine(directory, "memory.db"));
            await database.InitializeAsync(ct);
            var role = runtime.Characters.First();
            await database.EnsureConversationAsync("room", role.Id, ct);
            await database.AddMessageAsync("room", ChatRole.User, "猫咪", null, ct);
            await database.AddMemoryAsync(role.Id, "fact", "猫咪叫雪球", "猫咪", .8, null, ct);
            var generator = new ProactiveMessageGenerator(runtime, database);
            Assert.False(generator.IsReady);
            await runtime.ApplyPreferencesSnapshotAsync(runtime.Preferences with
            {
                ActiveModelId = "test", ActiveVisionModelId = "test",
                CustomModels = [new ModelProfile { Id = "test", DisplayName = "test", ModelName = "test",
                    Provider = "openai-compatible", BaseUrl = address + "/v1", MaxOutputTokens = 8192, EnableThinking = true }]
            }, ct);
            await runtime.GetComponentsAsync(ct);
            Assert.True(generator.IsReady);
            var policy = new ProactivePolicy(role.Id, "room", true);
            if (error is null) Assert.Equal(output, await generator.GenerateAsync(policy, ct));
            else Assert.Equal(error, (await Assert.ThrowsAsync<ProactiveOutputException>(() => generator.GenerateAsync(policy, ct))).Code);
            Assert.Equal(512, payload.GetProperty("max_tokens").GetInt32());
            Assert.False(payload.GetProperty("chat_template_kwargs").GetProperty("enable_thinking").GetBoolean());
            var prompt = payload.GetProperty("messages")[0].GetProperty("content").GetString()!;
            Assert.Contains(role.Name, prompt); Assert.Contains("猫咪叫雪球", prompt);
            Assert.Contains("后台主动话题任务", prompt);
            Assert.Single(await database.GetRecentMessagesAsync("room", 10, ct)); // generator never persists
            await runtime.StopAsync(ct);
        }
        finally
        {
            await server.StopAsync(ct);
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
