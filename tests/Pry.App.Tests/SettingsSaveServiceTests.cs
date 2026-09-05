using System.Net;
using System.Text;
using System.Text.Json;
using Pry.App.Services;
using Pry.Client;
using Pry.Contracts;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class SettingsSaveServiceTests
{
    [Fact]
    public void Selected_model_name_tolerates_incomplete_backend_projection()
    {
        static ModelProfileResponse Model(string id, string name, bool selected = false) =>
            new(id, name, "local", id, new(), 2048, 256, .7, 0, "cpu", false, selected, false, false);
        Assert.Equal("Selected", SettingsSaveService.SelectedModelName(
            [Model("fallback", "Fallback"), Model("selected", "Selected", true)], "fallback"));
        Assert.Equal("Fallback", SettingsSaveService.SelectedModelName([Model("fallback", "Fallback")], "fallback"));
        Assert.Null(SettingsSaveService.SelectedModelName([], "missing"));
    }

    [Fact]
    public async Task Save_preserves_request_order_and_explicit_appearance_clear()
    {
        var handler = new RecordingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new SettingsSaveService(new PryBackendClient(http), _ => "image/png");
        var result = await service.SaveAsync(new UserPreferences { ActiveModelId = "text" }, "room", _ => { }, TestContext.Current.CancellationToken);
        Assert.Empty(result);
        Assert.Equal(new[] { "/api/v1/preferences", "/api/v1/appearance/media", "/api/v1/models/selection" }, handler.Paths);
        using var appearance = JsonDocument.Parse(handler.Bodies[1]);
        Assert.True(appearance.RootElement.GetProperty("clearBackground").GetBoolean());
        Assert.True(appearance.RootElement.GetProperty("clearUserAvatar").GetBoolean());
    }

    [Fact]
    public async Task Failed_stage_prevents_subsequent_requests()
    {
        var handler = new RecordingHandler { FailPath = "/api/v1/appearance/media" };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var service = new SettingsSaveService(new PryBackendClient(http), _ => "image/png");
        await Assert.ThrowsAsync<PryBackendException>(() => service.SaveAsync(
            new UserPreferences { ActiveModelId = "text" }, "room", _ => { }, TestContext.Current.CancellationToken));
        Assert.Equal(2, handler.Paths.Count);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? FailPath { get; init; }
        public List<string> Paths { get; } = [];
        public List<string> Bodies { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var path = request.RequestUri!.AbsolutePath;
            Paths.Add(path);
            Bodies.Add(await request.Content!.ReadAsStringAsync(token));
            var failed = path == FailPath;
            return new HttpResponseMessage(failed ? HttpStatusCode.BadRequest : HttpStatusCode.OK)
            {
                Content = new StringContent(failed ? "{\"title\":\"rejected\"}" : path.EndsWith("selection") ? "[]" : "{}", Encoding.UTF8, "application/json")
            };
        }
    }
}
