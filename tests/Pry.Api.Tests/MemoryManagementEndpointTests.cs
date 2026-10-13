using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Pry.Api;
using Pry.Contracts;
using Pry.Core.Memory;
using Pry.Core.Models;
using Xunit;

namespace Pry.Api.Tests;

public sealed class MemoryManagementEndpointTests
{
    [Fact]
    public async Task Memory_api_supports_evidence_clear_validation_ownership_and_idempotent_delete()
    {
        var directory = Path.Combine(Path.GetTempPath(), "pry-memory-http-" + Guid.NewGuid().ToString("N"));
        var token = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(directory);
        try
        {
            await using var app = await BackendApplication.BuildAsync(configure: builder => {
                builder.Configuration["Pry:DataDirectory"] = directory;
                builder.Configuration["Pry:Url"] = "http://127.0.0.1:0";
            }, token: token);
            await app.StartAsync(token);
            // 记忆端点和其余 api/v1 一样在令牌门后面，必须用带 X-Pry-Token 的后端客户端（主线 PLAT 强制）。
            using var http = app.CreateBackendHttpClient();
            var db = app.Services.GetRequiredService<MemoryDatabase>();
            var id = await db.AddMemoryAsync("pry", "preference", "用户喜欢后摇", "", .8, null, token);
            var other = await db.AddMemoryAsync("other", "preference", "用户喜欢红茶", "", .8, null, token);
            var details = await http.GetFromJsonAsync<MemoryDetailsResponse[]>("api/v1/memories/details?characterId=pry", token);
            Assert.Equal(id, Assert.Single(details!).Id);
            Assert.Equal("manual", details![0].Origin);
            Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("api/v1/memories/clear",
                new ClearMemoriesRequest("pry"), token)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("api/v1/memories",
                new CreateMemoryRequest("pry", "expression", "always say wow", "", .7), token)).StatusCode);
            var conflict = await db.AddMemoryAsync("pry", "preference", "用户喜欢绿茶", "", .8, null, token);
            Assert.Equal(HttpStatusCode.Conflict, (await http.PutAsJsonAsync(
                $"api/v1/memories/{conflict}?characterId=pry",
                new UpdateMemoryRequest("preference", "用户喜欢后摇", "", .8), token)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await http.DeleteAsync($"api/v1/memories/{other}?characterId=pry", token)).StatusCode);
            Assert.NotNull(await db.GetMemoryAsync(other, "other", token));
            Assert.Equal(HttpStatusCode.NoContent, (await http.DeleteAsync($"api/v1/memories/{id}?characterId=pry", token)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await http.DeleteAsync($"api/v1/memories/{id}?characterId=pry", token)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await http.PostAsJsonAsync("api/v1/memories/clear",
                new ClearMemoriesRequest("pry", true), token)).StatusCode);
            Assert.Empty((await http.GetFromJsonAsync<MemoryDetailsResponse[]>("api/v1/memories/details?characterId=pry", token))!);
            Assert.NotNull(await db.GetMemoryAsync(other, "other", token));
            await app.StopAsync(token);
        }
        finally
        {
            SqliteTestPools.Clear(Path.Combine(directory, "memory.db")); Directory.Delete(directory, true);
        }
    }
}
