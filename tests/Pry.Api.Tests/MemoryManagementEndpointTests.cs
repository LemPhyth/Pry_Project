using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
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

    [Fact]
    public async Task ConfirmKeep_promotes_an_orphan_over_http_with_its_own_error_semantics()
    {
        var directory = Path.Combine(Path.GetTempPath(), "pry-memory-confirm-" + Guid.NewGuid().ToString("N"));
        var token = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(directory);
        var database = Path.Combine(directory, "memory.db");
        try
        {
            await using var app = await BackendApplication.BuildAsync(configure: builder => {
                builder.Configuration["Pry:DataDirectory"] = directory;
                builder.Configuration["Pry:Url"] = "http://127.0.0.1:0";
            }, token: token);
            await app.StartAsync(token);
            using var http = app.CreateBackendHttpClient();
            var db = app.Services.GetRequiredService<MemoryDatabase>();
            var orphaned = await db.AddMemoryAsync("pry", "preference", "用户喜欢后摇", "", .8, null, token);
            var active = await db.AddMemoryAsync("pry", "preference", "用户住在上海", "", .8, null, token);
            // The orphan-creating deletion path is exercised by the Core N-series; here we only stage the state
            // so the endpoint contract (transition, ceilings, per-case codes) is what the assertions pin down.
            await DemoteAsync(database, orphaned, token);

            var before = Assert.Single(await Details(http, "pry", token), x => x.Id == orphaned);
            Assert.Equal("orphaned", before.Status);
            Assert.Equal(HttpStatusCode.OK, (await http.PostAsync($"api/v1/memories/{orphaned}/confirm-keep?characterId=pry", null, token)).StatusCode);
            var kept = Assert.Single(await Details(http, "pry", token), x => x.Id == orphaned);
            Assert.Equal("active", kept.Status);
            Assert.Equal(.7, kept.Confidence, 5);
            Assert.Equal("manual", kept.Origin);
            Assert.Empty(kept.Evidence);            // confirming is a state transition, not a re-proof
            Assert.True(kept.Revision > before.Revision);  // epoch advanced, so her cached context drops the stale view

            // Same confirm a second time is now a non-orphan: rejected with its own code, not silently accepted.
            var again = await http.PostAsync($"api/v1/memories/{orphaned}/confirm-keep?characterId=pry", null, token);
            Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
            Assert.Equal("memory_not_orphaned", (await again.Content.ReadFromJsonAsync<ProblemJson>(token))!.Code);

            // A memory the caller does not own in this scope reads as not-found, exactly like the delete endpoint:
            // it must not leak that another character owns the id.
            Assert.Equal(HttpStatusCode.NotFound, (await http.PostAsync($"api/v1/memories/{active}/confirm-keep?characterId=other", null, token)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await http.PostAsync("api/v1/memories/999999/confirm-keep?characterId=pry", null, token)).StatusCode);
            await app.StopAsync(token);
        }
        finally
        {
            SqliteTestPools.Clear(database); Directory.Delete(directory, true);
        }
    }

    private static async Task<IReadOnlyList<MemoryDetailsResponse>> Details(HttpClient http, string characterId, CancellationToken token) =>
        await http.GetFromJsonAsync<MemoryDetailsResponse[]>($"api/v1/memories/details?characterId={characterId}", token)
        ?? [];

    // Stages a real learned fact with evidence through the consolidation pipeline, exactly like the Core N-series,
    // so the conversation-delete endpoint is exercised against genuine cross-conversation support rather than a stub.
    private static async Task LearnAsync(MemoryDatabase db, MemoryScope scope, string conversation, string text, CancellationToken token)
    {
        var now = DateTimeOffset.UtcNow;
        var policy = new ConsolidationPolicy(DelaySeconds: 0, LeaseSeconds: 10);
        var input = await db.AddMessageAsync(conversation, ChatRole.User, text, null, token);
        var output = await db.AddMessageAsync(conversation, ChatRole.Assistant, "收到。", null, token);
        var turn = new CompletedMemoryTurn(scope, conversation, "turn:" + input, [input], [output]);
        Assert.True((await db.LongTerm.EnqueueAsync(turn, policy, now, token)).Accepted);
        var job = Assert.IsType<MemoryJob>(await db.LongTerm.ClaimAsync(policy, now, token));
        Assert.True(await db.LongTerm.CompleteAsync(job, await new ExplicitFactExtractor().ExtractAsync(job, token), now, token));
    }

    [Fact]
    public async Task Deleting_a_conversation_over_http_forgets_only_the_facts_it_exclusively_taught()
    {
        var directory = Path.Combine(Path.GetTempPath(), "pry-convo-cascade-" + Guid.NewGuid().ToString("N"));
        var token = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(directory);
        var database = Path.Combine(directory, "memory.db");
        try
        {
            await using var app = await BackendApplication.BuildAsync(configure: builder => {
                builder.Configuration["Pry:DataDirectory"] = directory;
                builder.Configuration["Pry:Url"] = "http://127.0.0.1:0";
            }, token: token);
            await app.StartAsync(token);
            using var http = app.CreateBackendHttpClient();
            var db = app.Services.GetRequiredService<MemoryDatabase>();
            var scope = MemoryScope.ForCharacter("pry");
            await db.EnsureConversationAsync("a", "pry", token);
            await db.EnsureConversationAsync("b", "pry", token);
            await db.LongTerm.BindConversationAsync(scope, "a", token);
            await db.LongTerm.BindConversationAsync(scope, "b", token);
            await LearnAsync(db, scope, "a", "我喜欢后摇", token);   // only this conversation teaches it
            await LearnAsync(db, scope, "a", "我住在上海", token);
            await LearnAsync(db, scope, "b", "我住在上海", token);   // this fact also lives in b, so a cannot retract it

            var response = await http.DeleteAsync("api/v1/conversations/a?forgetExclusiveMemories=true", token);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var kept = await Details(http, "pry", token);
            Assert.DoesNotContain(kept, x => x.Content == "用户喜欢后摇");
            var shanghai = Assert.Single(kept, x => x.Content == "用户住在上海");
            Assert.Equal("active", shanghai.Status);
            await app.StopAsync(token);
        }
        finally
        {
            SqliteTestPools.Clear(database); Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task Deleting_a_conversation_without_the_cascade_flag_only_detaches_evidence()
    {
        var directory = Path.Combine(Path.GetTempPath(), "pry-convo-plain-" + Guid.NewGuid().ToString("N"));
        var token = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(directory);
        var database = Path.Combine(directory, "memory.db");
        try
        {
            await using var app = await BackendApplication.BuildAsync(configure: builder => {
                builder.Configuration["Pry:DataDirectory"] = directory;
                builder.Configuration["Pry:Url"] = "http://127.0.0.1:0";
            }, token: token);
            await app.StartAsync(token);
            using var http = app.CreateBackendHttpClient();
            var db = app.Services.GetRequiredService<MemoryDatabase>();
            var scope = MemoryScope.ForCharacter("pry");
            await db.EnsureConversationAsync("a", "pry", token);
            await db.LongTerm.BindConversationAsync(scope, "a", token);
            await LearnAsync(db, scope, "a", "我喜欢后摇", token);

            // Absent query parameter is the default (false): the same delete as before this work order.
            Assert.Equal(HttpStatusCode.NoContent, (await http.DeleteAsync("api/v1/conversations/a", token)).StatusCode);
            var orphaned = Assert.Single(await Details(http, "pry", token), x => x.Content == "用户喜欢后摇");
            Assert.Equal("orphaned", orphaned.Status);
            Assert.Equal(.4, orphaned.Confidence, 5);
            await app.StopAsync(token);
        }
        finally
        {
            SqliteTestPools.Clear(database); Directory.Delete(directory, true);
        }
    }

    private static async Task DemoteAsync(string database, long id, CancellationToken token)
    {
        // Pooling=false: this bare Data Source shape is a pool key the Api SqliteTestPools.Clear does not
        // reclaim, so a pooled handle here would survive cleanup and lock memory.db during Directory.Delete.
        await using var c = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = database, Pooling = false
        }.ToString());
        await c.OpenAsync(token);
        await using var cmd = c.CreateCommand();
        cmd.CommandText = "UPDATE memories SET status='orphaned',confidence=.4,source_message_id=NULL WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync(token);
    }

    private sealed record ProblemJson(string Code);
}
