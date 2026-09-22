using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Pry.Core.Abstractions;
using Pry.Core.Models;

namespace Pry.Core.Inference;

/// <summary>
/// 空档窗口默认取 <see cref="ProviderTimeouts.ResponseStreamIdle"/>；之所以留成构造参数而不是常量，
/// 是因为"服务端不回也不关时要多久才报错"这条判据必须能在测试里被观察到。
/// </summary>
public sealed class OpenAiCompatibleChatModel(HttpClient httpClient, ModelProfile profile,
    TimeSpan? streamIdleWindow = null) : IChatModel
{
    private readonly TimeSpan idleWindow = streamIdleWindow ?? ProviderTimeouts.ResponseStreamIdle;

    public ModelProfile Profile => profile;

    public async IAsyncEnumerable<string> StreamAsync(string systemPrompt, IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<string>? imagePaths, ChatRequestOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{profile.BaseUrl.TrimEnd('/')}/chat/completions");
        if (!string.IsNullOrWhiteSpace(profile.ApiKey)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", profile.ApiKey);
        var payloadMessages = new List<object> { new { role = "system", content = systemPrompt } };
        foreach (var message in messages)
        {
            if (message.Role == ChatRole.System) continue;
            var role = message.Role == ChatRole.User ? "user" : "assistant";
            if (message == messages.LastOrDefault() && imagePaths is { Count: > 0 } && profile.Capabilities.Vision)
            {
                var content = new List<object> { new { type = "text", text = message.Content } };
                foreach (var imagePath in imagePaths)
                {
                    var mime = Path.GetExtension(imagePath).ToLowerInvariant() switch { ".png" => "image/png", ".webp" => "image/webp", ".gif" => "image/gif", _ => "image/jpeg" };
                    var data = Convert.ToBase64String(await File.ReadAllBytesAsync(imagePath, cancellationToken));
                    content.Add(new { type = "image_url", image_url = new { url = $"data:{mime};base64,{data}" } });
                }
                payloadMessages.Add(new { role, content });
            }
            else payloadMessages.Add(new { role, content = message.Content });
        }
        var payload = new Dictionary<string, object?>
        {
            ["model"] = profile.ModelName,
            ["messages"] = payloadMessages,
            ["stream"] = true,
            ["temperature"] = profile.Temperature,
            ["max_tokens"] = options?.MaxOutputTokens is { } maximum
                ? Math.Clamp(maximum, 1, Math.Max(1, profile.MaxOutputTokens)) : profile.MaxOutputTokens,
            ["chat_template_kwargs"] = new { enable_thinking = options?.EnableThinking ?? profile.EnableThinking }
        };
        if (profile.TopP is { } topP) payload["top_p"] = topP;
        if (profile.TopK is { } topK) payload["top_k"] = topK;
        if (profile.MinP is { } minP) payload["min_p"] = minP;
        if (profile.PresencePenalty is { } presencePenalty) payload["presence_penalty"] = presencePenalty;
        if (profile.RepetitionPenalty is { } repetitionPenalty) payload["repetition_penalty"] = repetitionPenalty;
        // llama.cpp's schema grammar can deadlock the final-content phase of reasoning models.
        // The planning prompt still requests JSON; keep server-side schema enforcement for instruct models.
        if (options?.StructuredReplyPlan == true && !(options.EnableThinking ?? profile.EnableThinking))
            payload["response_format"] = ReplyPlanResponseFormat.Value;
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (Exception ex)
        {
            throw Classify(ex, cancellationToken);
        }
        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw await FromErrorResponseAsync(response, cancellationToken);

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);
            while (true)
            {
                string? line;
                try
                {
                    line = await ReadLineWithIdleAsync(reader, cancellationToken);
                }
                catch (Exception ex)
                {
                    throw Classify(ex, cancellationToken);
                }
                if (line is null) yield break;
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;
                var json = line[6..];
                if (json == "[DONE]") yield break;
                string? chunk;
                try
                {
                    using var document = JsonDocument.Parse(json);
                    chunk = document.RootElement.TryGetProperty("choices", out var choices) &&
                            choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0 &&
                            choices[0].TryGetProperty("delta", out var delta) &&
                            delta.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String
                        ? content.GetString()
                        : null;
                }
                catch (JsonException ex)
                {
                    // 一个坏帧不能假装是"回复正常结束"：那会让界面看到半句话却以为说完了。
                    throw ModelProviderError.InvalidResponse($"data: {json}", profile.ModelName, ex);
                }
                if (chunk is not null) yield return chunk;
            }
        }
    }

    /// <summary>读取空档上限内的单行；空档超限归类为超时，调用方取消仍按取消传播。</summary>
    private async Task<string?> ReadLineWithIdleAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        using var idle = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        idle.CancelAfter(idleWindow);
        try
        {
            return await reader.ReadLineAsync(idle.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw ModelProviderError.StreamIdle(idleWindow, profile.ModelName);
        }
    }

    /// <summary>错误体只取有界片段并遮掉本 Profile 的密钥：Provider 会把请求头回显在错误里。</summary>
    private async Task<ModelProviderException> FromErrorResponseAsync(HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        string? body;
        try
        {
            body = await ReadBoundedAsync(response, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            body = null;
        }
        return ModelProviderError.FromStatus(response.StatusCode,
            body is null ? null : LlamaServerManager.RedactSecret(body, profile.ApiKey),
            response.Headers.RetryAfter?.Delta, profile.ModelName);
    }

    private static async Task<string?> ReadBoundedAsync(HttpResponseMessage response, CancellationToken token)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        using var reader = new StreamReader(stream);
        var buffer = new char[ModelProviderError.MaximumDiagnosticCharacters];
        var read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
        return read <= 0 ? string.Empty : new string(buffer, 0, read);
    }

    private Exception Classify(Exception exception, CancellationToken cancellationToken) => exception switch
    {
        // 调用方取消必须原样传播。运行态是按 OperationCanceledException 把这一轮判成"被打断"的，
        // 这里改成包装类型就会把用户主动停止记成模型故障（Failed + provider_cancelled）。
        OperationCanceledException when cancellationToken.IsCancellationRequested => exception,
        ModelProviderException provider => provider,
        _ => ModelProviderError.FromException(exception, cancellationToken.IsCancellationRequested, profile.ModelName)
    };

    private static class ReplyPlanResponseFormat
    {
        public static readonly object Value = new
        {
            type = "json_schema",
            json_schema = new
            {
                name = "reply_plan",
                strict = true,
                schema = new
                {
                    type = "object",
                    properties = new
                    {
                        messages = new
                        {
                            type = "array",
                            minItems = 1,
                            maxItems = 6,
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    type = new { type = "string", @enum = new[] { "text", "sticker" } },
                                    content = new { type = new[] { "string", "null" } },
                                    stickerId = new { type = new[] { "string", "null" } }
                                },
                                required = new[] { "type", "content", "stickerId" },
                                additionalProperties = false
                            }
                        }
                    },
                    required = new[] { "messages" },
                    additionalProperties = false
                }
            }
        };
    }
}
