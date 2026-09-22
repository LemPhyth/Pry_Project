using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Pry.Core.Inference;
using Pry.Core.Models;
using Pry.Core.Runtime;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// PLAT-001 的两条判据：Provider 的每一次失败都落成七类稳定码之一，以及出站等待处处有界。
/// 这里刻意用真实的 <see cref="HttpClient"/> 走一遍（假 handler），因为"超时到底管不管正文"
/// 正是由 HttpClient 的完成选项决定的，换成直接调用内部方法就测不到那一层。
/// </summary>
public sealed class ModelProviderErrorTests
{
    private const string ApiKey = "sk-test-9f2c41d7";

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "", ProviderErrorCodes.Auth, false)]
    [InlineData(HttpStatusCode.Forbidden, "", ProviderErrorCodes.Auth, false)]
    [InlineData(HttpStatusCode.RequestTimeout, "", ProviderErrorCodes.Timeout, true)]
    [InlineData(HttpStatusCode.TooManyRequests, "", ProviderErrorCodes.RateLimited, true)]
    [InlineData(HttpStatusCode.InternalServerError, "", ProviderErrorCodes.ModelUnavailable, true)]
    [InlineData(HttpStatusCode.BadGateway, "", ProviderErrorCodes.ModelUnavailable, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, "", ProviderErrorCodes.ModelUnavailable, true)]
    // 上下文超限在 llama.cpp 与 OpenAI 两家用的是不同状态码，判据只能是措辞。
    [InlineData(HttpStatusCode.BadRequest, """{"error":{"code":"context_length_exceeded"}}""",
        ProviderErrorCodes.ContextOverflow, false)]
    [InlineData(HttpStatusCode.InternalServerError, "eval: input length (40960) exceeds context window (8192)",
        ProviderErrorCodes.ContextOverflow, false)]
    [InlineData(HttpStatusCode.BadRequest, "temperature must be <= 2", ProviderErrorCodes.InvalidResponse, false)]
    [InlineData(HttpStatusCode.NotFound, "no such route", ProviderErrorCodes.InvalidResponse, false)]
    public void Every_failure_lands_on_one_of_the_seven_codes(HttpStatusCode status, string body, string expectedCode,
        bool expectedRetryable)
    {
        var failure = ModelProviderError.FromStatus(status, body, null, "qwen");

        Assert.Equal(expectedCode, failure.Code);
        Assert.Equal(expectedRetryable, failure.Retryable);
    }

    [Fact]
    public async Task Rejected_credentials_never_carry_the_key_out_of_the_provider_response()
    {
        var model = CreateModel(new StatusHandler(HttpStatusCode.Unauthorized,
            $$"""{"error":{"message":"Invalid token in Authorization: Bearer {{ApiKey}}}"}"""));

        var failure = await Assert.ThrowsAsync<ModelProviderException>(() =>
            DrainAsync(model, TestContext.Current.CancellationToken));

        Assert.Equal(ProviderErrorCodes.Auth, failure.Code);
        Assert.DoesNotContain(ApiKey, failure.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(ApiKey, failure.DiagnosticDetails ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain(ApiKey, failure.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rate_limit_keeps_the_wait_the_provider_asked_for()
    {
        var model = CreateModel(new StatusHandler(HttpStatusCode.TooManyRequests, "{}", TimeSpan.FromSeconds(30)));

        var failure = await Assert.ThrowsAsync<ModelProviderException>(() =>
            DrainAsync(model, TestContext.Current.CancellationToken));

        Assert.Equal(ProviderErrorCodes.RateLimited, failure.Code);
        Assert.Equal(TimeSpan.FromSeconds(30), failure.RetryAfter);
    }

    [Fact]
    public async Task A_malformed_frame_is_a_failure_not_a_finished_reply()
    {
        // 坏帧若被当成"正常结束"，界面会看到半句话却以为说完了，也不会重试。
        var model = CreateModel(new StreamHandler(new SingleFrameStream(
            "data: {\"choices\":[{\"delta\":{\"content\":\"下半句", stallAfterFirstFrame: false)));

        var failure = await Assert.ThrowsAsync<ModelProviderException>(() =>
            DrainAsync(model, TestContext.Current.CancellationToken));

        Assert.Equal(ProviderErrorCodes.InvalidResponse, failure.Code);
        Assert.False(failure.Retryable);
    }

    [Fact]
    public async Task A_provider_that_goes_silent_becomes_a_timeout_instead_of_a_forever_wait()
    {
        var model = CreateModel(new StreamHandler(new SingleFrameStream(
            "data: {\"choices\":[{\"delta\":{\"content\":\"第一段\"}}]}\n\n", stallAfterFirstFrame: true)),
            streamIdleWindow: TimeSpan.FromMilliseconds(250));

        var failure = await Assert.ThrowsAsync<ModelProviderException>(() =>
            DrainAsync(model, TestContext.Current.CancellationToken));

        Assert.Equal(ProviderErrorCodes.Timeout, failure.Code);
        Assert.True(failure.Retryable);
    }

    [Fact]
    public async Task Stopping_a_turn_stays_a_cancellation_and_is_not_recorded_as_a_model_failure()
    {
        // 运行态是按 OperationCanceledException 把这一轮判成"被打断"的：归类器若把它包成
        // ModelProviderException，用户主动停止就会变成一次 Failed 轮次与一次错误的失败提示。
        var model = CreateModel(new StreamHandler(new SingleFrameStream(
            "data: {\"choices\":[{\"delta\":{\"content\":\"第一段\"}}]}\n\n", stallAfterFirstFrame: true)),
            streamIdleWindow: TimeSpan.FromSeconds(30));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            DrainAsync(model, cancellation.Token));
    }

    [Fact]
    public async Task Headers_that_never_arrive_are_reported_as_a_timeout_not_a_cancelled_request()
    {
        using var handler = new DelayedHeaderHandler(TimeSpan.FromSeconds(30));
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(200) };
        var model = new OpenAiCompatibleChatModel(client, Profile());

        var failure = await Assert.ThrowsAsync<ModelProviderException>(() =>
            DrainAsync(model, TestContext.Current.CancellationToken));

        Assert.Equal(ProviderErrorCodes.Timeout, failure.Code);
    }

    [Fact]
    public void Model_clients_are_bounded_and_the_local_tier_keeps_its_queueing_margin()
    {
        using var local = ProviderTimeouts.CreateModelClient(localLoopback: true);
        using var remote = ProviderTimeouts.CreateModelClient(localLoopback: false);

        // 曾经这两处都是 Timeout.InfiniteTimeSpan：Provider 挂起时前台请求永远等不到结果也等不到错误。
        Assert.NotEqual(Timeout.InfiniteTimeSpan, local.Timeout);
        Assert.NotEqual(Timeout.InfiniteTimeSpan, remote.Timeout);
        Assert.Equal(ProviderTimeouts.LocalResponseHeaders, local.Timeout);
        Assert.Equal(ProviderTimeouts.RemoteResponseHeaders, remote.Timeout);
        Assert.True(ProviderTimeouts.LocalResponseHeaders > ProviderTimeouts.RemoteResponseHeaders);
        Assert.True(ProviderTimeouts.ResponseStreamIdle < ProviderTimeouts.LocalResponseHeaders);
    }

    [Theory]
    [InlineData("local-llama", "https://remote.example/v1", true)]
    [InlineData("openai-compatible", "http://127.0.0.1:8080/v1", true)]
    [InlineData("openai-compatible", "http://localhost:11434/v1", true)]
    [InlineData("openai-compatible", "https://api.openai.com/v1", false)]
    public void The_local_tier_is_where_a_request_may_legitimately_queue(string provider, string baseUrl,
        bool expectedLocal)
    {
        Assert.Equal(expectedLocal, ProviderTimeouts.IsLocal(new ModelProfile
        {
            Id = "m", DisplayName = "M", Provider = provider, BaseUrl = baseUrl
        }));
    }

    [Fact]
    public void Classification_keeps_the_host_code_when_nothing_structured_is_known()
    {
        Assert.Equal("reply_generation_failed",
            ModelProviderError.CodeOf(new InvalidOperationException("composer threw"), "reply_generation_failed"));
        Assert.Equal(ProviderErrorCodes.Auth, ModelProviderError.CodeOf(
            ModelProviderError.FromStatus(HttpStatusCode.Unauthorized, null, null, null), "reply_generation_failed"));
        Assert.Equal(ProviderErrorCodes.Cancelled,
            ModelProviderError.CodeOf(new OperationCanceledException(), "reply_generation_failed"));
        Assert.True(ModelProviderError.IsRetryable(new HttpRequestException("connection refused")));
        Assert.False(ModelProviderError.IsRetryable(new InvalidOperationException("composer threw")));
    }

    [Fact]
    public async Task Embedding_timeouts_are_timeouts_and_not_an_unexplained_cancellation()
    {
        using var handler = new DelayedHeaderHandler(TimeSpan.FromSeconds(30));
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(200) };
        var provider = new OpenAiCompatibleEmbeddingProvider(client, Profile());

        var failure = await Assert.ThrowsAsync<ModelProviderException>(() =>
            provider.EncodeAsync("一句话", TestContext.Current.CancellationToken));

        Assert.Equal(ProviderErrorCodes.Timeout, failure.Code);
    }

    [Fact]
    public async Task Embedding_rejects_a_response_that_is_not_a_vector()
    {
        using var handler = new StatusHandler(HttpStatusCode.OK, """{"data":[{"embedding":"n/a"}]}""");
        using var client = new HttpClient(handler);
        var provider = new OpenAiCompatibleEmbeddingProvider(client, Profile());

        var failure = await Assert.ThrowsAsync<ModelProviderException>(() =>
            provider.EncodeAsync("一句话", TestContext.Current.CancellationToken));

        Assert.Equal(ProviderErrorCodes.InvalidResponse, failure.Code);
    }

    [Fact]
    public async Task A_provider_rejection_reaches_the_interface_as_its_own_turn_failure()
    {
        // turn.failed.code 是界面能否"让用户重试"的判据：密钥错与限流在界上是两种提示，
        // 所以不能再统一记成 reply_generation_failed。未结构化的故障保留原来的兜底码，不谎报原因。
        var harness = await RuntimeHarness.CreateAsync(new ControlledComposer((_, _, _) =>
            throw ModelProviderError.FromStatus(HttpStatusCode.Unauthorized, null, null, "qwen")));

        await harness.Runtime.SubmitUserInputAsync(new RuntimeUserInput("说一句"),
            cancellationToken: TestContext.Current.CancellationToken);
        await harness.SettleAsync(TestContext.Current.CancellationToken);

        Assert.Equal([ProviderErrorCodes.Auth], harness.Sink.Failures);
        await harness.Runtime.DisposeAsync();
    }

    [Fact]
    public async Task An_unstructured_generation_failure_keeps_the_code_the_host_already_used()
    {
        var harness = await RuntimeHarness.CreateAsync(new ControlledComposer((_, _, _) =>
            throw new InvalidOperationException("提示装配失败")));

        await harness.Runtime.SubmitUserInputAsync(new RuntimeUserInput("说一句"),
            cancellationToken: TestContext.Current.CancellationToken);
        await harness.SettleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["reply_generation_failed"], harness.Sink.Failures);
        await harness.Runtime.DisposeAsync();
    }

    private static ModelProfile Profile() => new()
    {
        Id = "qwen", DisplayName = "Qwen", ModelName = "qwen3-1.7b", BaseUrl = "http://127.0.0.1:8080/v1",
        ApiKey = ApiKey
    };

    private static OpenAiCompatibleChatModel CreateModel(HttpMessageHandler handler,
        TimeSpan? streamIdleWindow = null) =>
        new(new HttpClient(handler), Profile(), streamIdleWindow);

    private static async Task DrainAsync(OpenAiCompatibleChatModel model, CancellationToken token)
    {
        await foreach (var _ in model.StreamAsync("system",
            [new ChatMessage(1, "chat", ChatRole.User, "你好", DateTimeOffset.UtcNow)], null, null, token))
        {
        }
    }

    private sealed class StatusHandler(HttpStatusCode status, string body, TimeSpan? retryAfter = null)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            if (retryAfter is { } delay) response.Headers.RetryAfter = new RetryConditionHeaderValue(delay);
            return Task.FromResult(response);
        }
    }

    private sealed class StreamHandler(Stream stream) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(stream)
        });
    }

    private sealed class DelayedHeaderHandler(TimeSpan delay) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        }
    }

    /// <summary>吐完第一帧之后既不回也不关：半开连接在读取侧的形状，超时与取消两条路都从这里分岔。</summary>
    private sealed class SingleFrameStream(string firstFrame, bool stallAfterFirstFrame) : Stream
    {
        private readonly byte[] _pending = Encoding.UTF8.GetBytes(firstFrame);
        private int _offset;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _pending.LongLength;
        public override long Position
        {
            get => _offset;
            set => throw new NotSupportedException();
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) => ReadCore(buffer.AsSpan(offset, count));

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (_offset >= _pending.Length && stallAfterFirstFrame)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return 0;
            }
            return ReadCore(buffer.Span);
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private int ReadCore(Span<byte> buffer)
        {
            var count = Math.Min(buffer.Length, _pending.Length - _offset);
            if (count <= 0) return 0;
            _pending.AsSpan(_offset, count).CopyTo(buffer);
            _offset += count;
            return count;
        }
    }
}
