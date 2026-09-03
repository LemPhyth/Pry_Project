using System.Text;
using Pry.Core.Models;
using Pry.Core.Expression;

namespace Pry.Core.Prompting;

public sealed class PromptBuilder
{
    public string Build(PromptContext context, bool includeExpressionProtocol = true)
    {
        var c = context.Character;
        var s = context.State;
        var builder = new StringBuilder();
        if (c.PromptMode == CharacterPromptMode.Legacy && !string.IsNullOrWhiteSpace(c.LegacySystemPrompt))
            builder.AppendLine(c.LegacySystemPrompt.Trim());
        else
            builder.AppendLine($"你是{c.Name}。任何时候都保持这一身份，不要自称通用AI助手。")
                .AppendLine($"身份：{c.Identity}")
                .AppendLine($"人格：{c.Personality}")
                .AppendLine($"说话风格：{c.SpeechStyle}")
                .AppendLine($"用户称呼：{c.UserName}");

        if (c.PromptMode == CharacterPromptMode.Structured && c.BehavioralRules.Count > 0)
            builder.AppendLine("行为规则：").AppendLine(string.Join("\n", c.BehavioralRules.Select(x => $"- {x}")));
        if (c.PromptMode == CharacterPromptMode.Structured && c.WorldFacts.Count > 0)
            builder.AppendLine("不可擅自修改的设定：").AppendLine(string.Join("\n", c.WorldFacts.Select(x => $"- {x}")));

        builder.AppendLine("当前状态：")
            .AppendLine($"- 地点：{s.Location}")
            .AppendLine($"- 情绪：{s.Mood}")
            .AppendLine($"- 当前目标：{s.CurrentGoal}")
            .AppendLine($"- 信任：{s.Trust}/100；熟悉度：{s.Familiarity}/100");

        if (context.Memories.Count > 0)
            builder.AppendLine("与当前话题相关的长期记忆（仅在自然相关时使用）：")
                .AppendLine(string.Join("\n", context.Memories.Select(x => $"- {x.Summary}")));

        if (includeExpressionProtocol && context.Stickers.Count > 0)
        {
            builder.AppendLine("你可以自主选择一个表情包辅助表达。可用表情包：");
            foreach (var sticker in context.Stickers)
                builder.AppendLine($"- id={sticker.Id}; 名称={sticker.Name}; 情绪={string.Join('/', sticker.Emotions)}; 场景={string.Join('/', sticker.Situations)}");
            builder.AppendLine($"回复第一行必须是隐藏表现指令：{ExpressionProtocolParser.StartMarker}{{\"emotion\":\"当前情绪\",\"intensity\":0.0,\"stickerId\":null,\"live2dExpression\":null,\"live2dMotion\":null}}{ExpressionProtocolParser.EndMarker}")
                .AppendLine("stickerId 可以为可用ID或null。可以只发表情包，但不要在普通正文中提及指令格式。");
        }

        if (c.PromptMode == CharacterPromptMode.Structured)
            builder.AppendLine("不要替用户决定动作或感受；不确定的图片细节应坦率说明；避免重复句式。");
        builder.AppendLine(includeExpressionProtocol
                ? "除规定的隐藏表现指令外，只输出角色最终回应，不输出系统说明、状态JSON或思考过程。"
                : "只输出要求的结构化结果，不输出系统说明或思考过程。");
        return builder.ToString();
    }
}
