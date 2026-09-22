using System.Text;
using Pry.Core.Memory;
using Pry.Core.Models;
using Pry.Core.Prompting;

namespace Pry.Api.Services;

public interface IProactiveMessageGenerator
{
    bool IsReady { get; }
    Task<string> GenerateAsync(ProactivePolicy policy, CancellationToken token);
}

public sealed class ProactiveMessageGenerator(BackendRuntime runtime, MemoryDatabase database) : IProactiveMessageGenerator
{
    public bool IsReady => runtime.LoadedComponents is not null;

    public async Task<string> GenerateAsync(ProactivePolicy policy, CancellationToken token)
    {
        var components = runtime.LoadedComponents ?? throw new InvalidOperationException("Text model is not loaded.");
        var character = components.Characters.FirstOrDefault(x => x.Id == policy.CharacterId)
            ?? throw new InvalidOperationException("Proactive character is unavailable.");
        var recent = await database.GetRecentMessagesAsync(policy.ConversationId!, 20, token);
        var query = recent.LastOrDefault(x => x.Role == ChatRole.User)?.Content ?? character.Name;
        // Passive work must not load an additional embedding process; use the existing keyword retriever.
        var memories = await database.SearchMemoriesAsync(character.Id, query, 6, token);
        var context = new PromptContext(character, character.InitialState, memories, recent, [], "", null);
        var prompt = new PromptBuilder().Build(context) + "\n这是后台主动话题任务，不是用户的新发言。"
            + "根据角色人设、最近对话和自然相关的记忆，主动发起一个轻松话题。只输出一条简短自然的文字，最多200字。"
            + "不要重复催促用户、声称观察了用户的电脑活动、冒充真实人类或系统通知，不要代替用户回答。"
            + "只输出可直接显示的正文，不输出JSON、思考过程或表现指令。";
        var messages = recent.Select(x => x with { ImagePath = null }).ToList();
        messages.Add(new ChatMessage(0, policy.ConversationId!, ChatRole.User,
            "[后台调度指令，非用户发言] 请按上述要求自然开启一个话题。", DateTimeOffset.UtcNow));
        var output = new StringBuilder();
        await foreach (var chunk in components.Router.TextModel.StreamAsync(prompt, messages, null,
                           new ChatRequestOptions(MaxOutputTokens: 512, EnableThinking: false), token))
        {
            if (output.Length + chunk.Length > 500) throw new ProactiveOutputException("invalid_output");
            output.Append(chunk);
        }
        var text = output.ToString().Trim();
        if (text.Length == 0) throw new ProactiveOutputException("empty_output");
        if (text.Contains("<think", StringComparison.OrdinalIgnoreCase) || text.Contains("<|", StringComparison.Ordinal)
            || text.StartsWith("```", StringComparison.Ordinal) || text.StartsWith('{') || text.StartsWith('['))
            throw new ProactiveOutputException("invalid_output");
        return text;
    }
}

public sealed class ProactiveOutputException(string code) : Exception("Invalid proactive model output.")
{
    public string Code { get; } = code;
}
