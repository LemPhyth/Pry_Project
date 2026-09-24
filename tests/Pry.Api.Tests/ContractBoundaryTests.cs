using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pry.Contracts;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// PLAT-005 的边界门：`Pry.Contracts` 不再引用 `Pry.Core`，方向反过来。
/// 引用解除只挡得住今天，挡不住以后有人把 Core 类型写进契约签名——那正是本次要消灭的
/// "Core 内部改名静默改变线协议"通道，所以这里既查装配引用，也查公开签名，并钉住共用值类型当前的线格式。
/// </summary>
public class ContractBoundaryTests
{
    private static readonly Assembly ContractsAssembly = typeof(CreateMessageRequest).Assembly;

    private static readonly Assembly CoreAssembly = typeof(Pry.Core.Models.CharacterDefinition).Assembly;

    private static readonly JsonSerializerOptions ApiJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly JsonSerializerOptions Readable = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// 真实字节里非 ASCII 会被转义成 \uXXXX，逐字符比较会假失败；重解析后改用宽松编码器输出，
    /// 属性顺序与取值不变，字面量可读。
    /// </summary>
    private static string Wire(object value)
    {
        using var parsed = JsonDocument.Parse(JsonSerializer.Serialize(value, ApiJson));
        return JsonSerializer.Serialize(parsed.RootElement, Readable);
    }

    [Fact]
    public void Contract_assembly_references_no_other_pry_assembly()
    {
        var selfName = ContractsAssembly.GetName().Name;
        var referenced = ContractsAssembly.GetReferencedAssemblies()
            .Select(name => name.Name ?? "")
            .Where(name => name.StartsWith("Pry.", StringComparison.Ordinal) && name != selfName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.Empty(referenced);
    }

    [Fact]
    public void Public_contract_surface_exposes_no_core_types()
    {
        var leaks = new List<string>();
        foreach (var type in ContractsAssembly.GetExportedTypes())
        {
            foreach (var (member, offending) in CoreTypesIn(type))
            {
                leaks.Add($"{type.Name}.{member} -> {offending.FullName}");
            }
        }

        Assert.Empty(leaks);
    }

    private static IEnumerable<(string Member, Type Type)> CoreTypesIn(Type type)
    {
        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (baseType.Assembly == CoreAssembly)
            {
                yield return ("base type", baseType);
            }
        }

        foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
                     | BindingFlags.DeclaredOnly))
        {
            IEnumerable<Type> signature = member switch
            {
                PropertyInfo property => new[] { property.PropertyType },
                FieldInfo field => new[] { field.FieldType },
                MethodInfo method => new[] { method.ReturnType }.Concat(method.GetParameters().Select(p => p.ParameterType)),
                ConstructorInfo constructor => constructor.GetParameters().Select(p => p.ParameterType),
                _ => [],
            };

            foreach (var used in signature.SelectMany(Unwrap).Where(used => used.Assembly == CoreAssembly))
            {
                yield return (member.Name, used);
            }
        }
    }

    private static IEnumerable<Type> Unwrap(Type type)
    {
        yield return type;
        foreach (var argument in type.GetGenericArguments())
        {
            foreach (var nested in Unwrap(argument))
            {
                yield return nested;
            }
        }
    }

    [Fact]
    public void Shared_value_types_serialize_with_their_current_wire_shape()
    {
        Assert.Equal("\"Structured\"", Wire(CharacterPromptMode.Structured));
        Assert.Equal("\"User\"", Wire(ChatRole.User));
        Assert.Equal("\"BuiltIn\"", Wire(StickerSource.BuiltIn));
        Assert.Equal("""{"location":"日常空间","mood":"平静","currentGoal":"陪伴用户","trust":50,"familiarity":10}""", Wire(new RuntimeState()));
        Assert.Equal("""{"speed":1,"burstiness":0.75,"minDelayMs":450,"maxDelayMs":3200}""", Wire(new TypingStyle()));
        Assert.Equal("""{"text":true,"vision":false,"embeddings":false,"imageGeneration":false,"toolCalling":false}""", Wire(new ModelCapabilities()));
        Assert.Equal("""{"focusX":0.5,"focusY":0.5,"zoom":1}""", Wire(new ImageDisplayPreferences()));
        Assert.Equal("""{"displayName":"你","signature":"愿今天也有好心情"}""", Wire(new UserProfilePreferences()));
        Assert.Equal("""{"enabled":false,"alwaysOnTop":true,"scale":1}""", Wire(new DesktopPetPreferences()));
        Assert.Equal("""{"activeModelId":null,"temperature":null,"maxOutputTokens":null,"contextSize":null,"gpuLayers":null,"computeDevice":null,"enableThinking":null}""",
            Wire(new ModelTuningPreferences()));
        Assert.Equal("""{"send":"Enter","sendImmediately":"Ctrl+Enter","newLine":"Shift+Enter","cancelReply":"Escape","newConversation":"Ctrl+N","openStickers":"Ctrl+E","openCharacterEditor":"Ctrl+Shift+C"}""",
            Wire(new ShortcutSettings()));
        Assert.Equal("""{"debounceMs":1200,"maxPendingMs":5000,"replyFrequency":1,"splitReplies":true,"autoClassifyInterrupts":true,"typingStyle":{"speed":1,"burstiness":0.75,"minDelayMs":450,"maxDelayMs":3200},"minReplyMessages":1,"maxReplyMessages":4,"maxMessageCharacters":90,"enableListeningSignals":true,"listeningSignalDelayMs":4500,"styleInstruction":"像即时通讯聊天一样自然、简短，不要一次倾倒过多信息。"}""",
            Wire(new TurnTakingSettings()));
    }

    [Fact]
    public void Shared_resource_types_serialize_with_their_current_wire_shape()
    {
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        Assert.Equal("""{"id":7,"conversationId":"c","role":"User","content":"hi","createdAt":"2026-01-01T00:00:00+00:00","imagePath":null,"stickerId":null,"deliveredAt":null,"source":"user_input"}""",
            Wire(new ChatMessage(7, "c", ChatRole.User, "hi", createdAt, Source: MessageSources.UserInput)));
        Assert.Equal("""{"id":9,"characterId":"a","kind":"fact","summary":"s","tags":"t","importance":1,"sourceMessageId":null,"createdAt":"2026-01-01T00:00:00+00:00","updatedAt":null,"lastUsedAt":null}""",
            Wire(new MemoryRecord(9, "a", "fact", "s", "t", 1, null, createdAt)));
        Assert.Equal("""{"id":"f","name":"默认","createdAt":"2026-01-01T00:00:00+00:00"}""",
            Wire(new ConversationFolder("f", "默认", createdAt)));
        Assert.Equal("""{"id":"c","title":"t","characterId":null,"createdAt":"2026-01-01T00:00:00+00:00","updatedAt":"2026-01-01T00:00:00+00:00","messageCount":0,"folderId":null,"isPinned":false,"lastMessagePreview":null,"lastMessageRole":null,"lastMessageKind":null,"lastMessageAt":null,"lastMessageId":null,"lastReadMessageId":0,"unreadCount":0}""",
            Wire(new ConversationRoom("c", "t", null, createdAt, createdAt, 0)));
    }

    [Fact]
    public void Shared_vocabularies_are_pinned()
    {
        Assert.Equal(
            new[] { "interaction_generated", "interaction_preset", "legacy", "listening_signal", "model_reply", "proactive", "user_input" },
            typeof(MessageSources).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(field => (string)field.GetRawConstantValue()!)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());

        Assert.Equal(new[] { "messenger", "card" },
            new[] { MainWindowLayoutModes.Messenger, MainWindowLayoutModes.Card });
    }

    /// <summary>
    /// 七类 Provider 码是线契约，改名等于改协议；它们在 2026-09-21 从 `Pry.Core` 移到 `Pry.Contracts`，
    /// 因为界面的错误表（`Pry.Client`）与 HTTP 状态映射（`Pry.Api`）要按同一批码判读，而 Client 不得依赖 Core。
    /// 钉住归属与取值：漂回 Core 会让 Client 要么断不掉依赖、要么抄一份字面量，两种都是 QA-P4-02 说的那类"两套读法"。
    /// </summary>
    [Fact]
    public void Provider_error_codes_are_pinned_in_the_contract_assembly()
    {
        Assert.Equal(ContractsAssembly, typeof(ProviderErrorCodes).Assembly);

        Assert.Equal(
            new[]
            {
                "context_overflow", "provider_auth", "provider_cancelled", "provider_invalid_response",
                "provider_rate_limited", "provider_timeout", "provider_unavailable",
            },
            typeof(ProviderErrorCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(field => (string)field.GetRawConstantValue()!)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
    }
}
