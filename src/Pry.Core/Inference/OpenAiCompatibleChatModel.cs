using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Pry.Core.Abstractions;
using Pry.Core.Models;

namespace Pry.Core.Inference;

public sealed class OpenAiCompatibleChatModel(HttpClient httpClient, ModelProfile profile) : IChatModel
{
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
            ["max_tokens"] = profile.MaxOutputTokens,
            ["chat_template_kwargs"] = new { enable_thinking = profile.EnableThinking }
        };
        if (options?.StructuredReplyPlan == true)
            payload["response_format"] = ReplyPlanResponseFormat.Value;
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null) yield break;
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;
            var json = line[6..];
            if (json == "[DONE]") yield break;
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("delta", out var delta) && delta.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
                yield return content.GetString()!;
        }
    }

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
