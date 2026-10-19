using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Pry.Api.Services;
using Pry.Contracts;
using Pry.Core.Configuration;
using Pry.Core.Memory;
using Pry.Core.Models;
using Pry.Core.Runtime;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// <c>DEF-P4LEARN-01</c> §2.5 的判据面：新端点在真宿主上按仓库的规矩办事——DTO 在 <c>Pry.Contracts</c>、
/// 有验证、每种拒法有自己的状态码与错误码，而不是都塞 500（<c>AGENTS.md</c> 第 5 条）。
/// 这里跑真的 <c>BackendApplication</c> 宿主与真的 HTTP 往返，因为"错误语义"这件事只有穿过
/// <see cref="Pry.Api.Middleware.ApiExceptionMiddleware"/> 才算存在；网关侧的处理结果本身由
/// <see cref="ExplicitExpressionFeedbackTests"/> 判，两边读数不互顶。
/// </summary>
public sealed class ExpressionFeedbackEndpointTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static string Route(string conversationId, long messageId) =>
        $"api/v1/conversations/{conversationId}/messages/{messageId}/expression-feedback";

    private static async Task<(WebApplication app, string directory)> StartAsync(bool advancedSwitch)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pry-expression-feedback-endpoint-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        if (advancedSwitch)
            await JsonConfiguration.SaveAsync(Path.Combine(directory, "preferences.json"),
                new UserPreferences { ExplicitExpressionFeedback = true }, Token);
        var app = await BackendApplication.BuildAsync(configure: builder =>
        {
            builder.Configuration["Pry:DataDirectory"] = directory;
            builder.Configuration["Pry:ModelsDirectory"] = Path.Combine(directory, "models");
            builder.Configuration["Pry:Url"] = "http://127.0.0.1:0";
        }, token: Token);
        await app.StartAsync(Token);
        return (app, directory);
    }

    private static async Task CleanupAsync(WebApplication app, string directory)
    {
        await app.StopAsync(Token);
        // 只清自己这条库产生的池（DEF-P1WALK-04：进程级 ClearAllPools 会拆掉并行用例的句柄）。
        SqliteTestPools.Clear(Path.Combine(directory, "memory.db"));
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    private static async Task<string> BodyAsync(HttpResponseMessage response) =>
        await response.Content.ReadAsStringAsync(Token);

    [Fact]
    public async Task A_disabled_switch_answers_before_the_ledger_is_even_consulted()
    {
        var (app, directory) = await StartAsync(false);
        try
        {
            using var http = app.CreateBackendHttpClient();
            // 这条消息根本不存在：开关的检查排在归属解析之前，所以先说的是"入口今天不存在"，
            // 而不是让用户去猜"是不是我点的那条不对"。
            using var response = await http.PostAsJsonAsync(Route("room", 4242),
                new ExpressionFeedbackRequest(true), Token);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var body = await BodyAsync(response);
            Assert.Contains(ExpressionFeedbackErrorCodes.Disabled, body);
            Assert.Contains("urn:pry:error:expression_feedback_disabled", body);
        }
        finally
        {
            await CleanupAsync(app, directory);
        }
    }

    [Fact]
    public async Task Each_way_a_reaction_can_be_refused_has_its_own_status_and_code()
    {
        var (app, directory) = await StartAsync(true);
        try
        {
            using var http = app.CreateBackendHttpClient();
            var database = app.Services.GetRequiredService<MemoryDatabase>();
            await database.EnsureConversationAsync("room", "card-endpoint", Token);
            var deliveredBefore = await database.AddMessageAsync("room", ChatRole.Assistant,
                "我嘞个，居然活过来了", null, Token);

            // 一、这个会话里没有这条消息。
            using (var missing = await http.PostAsJsonAsync(Route("room", 987_654),
                new ExpressionFeedbackRequest(true), Token))
            {
                Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
                Assert.Contains(ExpressionFeedbackErrorCodes.MessageMissing, await BodyAsync(missing));
            }

            // 二、消息在，但本进程没有它的投递凭据（重启前的旧气泡）。明确拒，不猜归属。
            using (var unattributable = await http.PostAsJsonAsync(Route("room", deliveredBefore),
                new ExpressionFeedbackRequest(true), Token))
            {
                Assert.Equal(HttpStatusCode.Conflict, unattributable.StatusCode);
                Assert.Contains(ExpressionFeedbackErrorCodes.NotAttributable, await BodyAsync(unattributable));
            }

            // 三、连编号都不是编号：那是请求本身的问题，不是找不到。
            using (var invalid = await http.PostAsJsonAsync(Route("room", 0),
                new ExpressionFeedbackRequest(true), Token))
            {
                Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
                Assert.Contains("validation_error", await BodyAsync(invalid));
            }
        }
        finally
        {
            await CleanupAsync(app, directory);
        }
    }

    [Fact]
    public async Task An_approving_reaction_that_reaches_the_ledger_answers_with_its_outcome()
    {
        var (app, directory) = await StartAsync(true);
        try
        {
            using var http = app.CreateBackendHttpClient();
            var database = app.Services.GetRequiredService<MemoryDatabase>();
            await database.EnsureConversationAsync("room", "card-endpoint", Token);
            await database.AddMessageAsync("room", ChatRole.User, "聊阳台的植物", null, Token);
            var gateway = app.Services.GetRequiredService<Pry.Api.Runtime.CharacterIntelligenceGateway>();

            // 走真的投递装饰器拿到凭据，再让 HTTP 这一侧去点它——端点与网关用的是同一个单例。
            var sink = new RecordingSink();
            var decorated = new Pry.Api.Runtime.CharacterIntelligenceOutputSink(sink, gateway);
            var turn = new TurnIdentity(1, 1, 1);
            var reply = "我嘞个，居然活过来了";
            await gateway.CreateAdvisor("room", new CharacterDefinition
            {
                Id = "card-endpoint", Name = "小潮", CardName = "小潮", UserName = "你", Identity = "陪伴者",
                Personality = "沉稳", SpeechStyle = "简洁", BehavioralRules = [], Greeting = "你好。"
            }, new TurnTakingSettings()).AdviseAsync(new ReplyGateContext(turn, "聊阳台的植物", true, true, false,
                Pry.Core.Runtime.RuntimePhase.Planning, false, 1, 0, null, TimeSpan.Zero, 0, false), Token);
            var messageId = await database.AddMessageAsync("room", ChatRole.Assistant, reply, null, Token);
            await decorated.MessageDeliveredAsync("room", messageId,
                new Pry.Core.Runtime.RuntimePlannedMessage(0, reply, null), MessageSources.ModelReply, Token);
            await decorated.TurnTransitionAsync("room", turn, Pry.Core.Runtime.RuntimeTurnOutcome.Completed,
                "delivered", Token);

            using var response = await http.PostAsJsonAsync(Route("room", messageId),
                new ExpressionFeedbackRequest(true), Token);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await BodyAsync(response);
            Assert.Contains(ExpressionFeedbackOutcomes.Recorded, body);
            Assert.Contains(messageId.ToString(), body);
            // 转发给宿主 sink 的那一次投递没有被装饰器吞掉——表态是旁听，不是第二条外发路径。
            Assert.Equal(messageId, sink.DeliveredMessageId);
        }
        finally
        {
            await CleanupAsync(app, directory);
        }
    }

    private sealed class RecordingSink : Pry.Core.Runtime.IRuntimeOutputSink
    {
        public long? DeliveredMessageId { get; private set; }

        public Task MessageDeliveredAsync(string conversationId, long messageId,
            Pry.Core.Runtime.RuntimePlannedMessage message, string source, CancellationToken token)
        {
            DeliveredMessageId = messageId;
            return Task.CompletedTask;
        }

        public Task TypingIndicatorAsync(string conversationId, bool active, CancellationToken token) =>
            Task.CompletedTask;

        public Task PhaseChangedAsync(string conversationId, Pry.Core.Runtime.RuntimePhase phase,
            Pry.Core.Runtime.RuntimePhase previous, CancellationToken token) => Task.CompletedTask;

        public Task TurnTransitionAsync(string conversationId, TurnIdentity turn,
            Pry.Core.Runtime.RuntimeTurnOutcome outcome, string reason, CancellationToken token) =>
            Task.CompletedTask;

        public Task CapabilityRequestedAsync(string conversationId,
            Pry.Core.Abstractions.CapabilityRequest request, CancellationToken token) => Task.CompletedTask;

        public Task FailureAsync(string conversationId, TurnIdentity turn, string errorCode,
            CancellationToken token) => Task.CompletedTask;
    }
}
