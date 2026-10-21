using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Pry.Api.Services;
using Pry.Contracts;
using Pry.Core.Memory;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// AUD-07（HDS-011b）：贴纸／表情附带的那段文本必须和纯文本走同一条长度上限。
/// 原缺陷形状是两个入口都写 <c>StickerId is null ? Required(content, 20_000) : content?.Trim() ?? ""</c>，
/// 也就是带贴纸那一支整段跳过长度校验，超长文本可以直接进会话。
/// 这里守三件事：上限真的在拒（不是只测"能过"）、两个入口用的是同一把尺（不许只补一个）、
/// 上限数字只有一处声明（不许有第二套语义）；顺手守住"补齐之后不许把贴纸附带文本丢掉"。
/// </summary>
public sealed class StickerContentLengthGuardTests : IAsyncLifetime
{
    private const int ContentLimit = 20_000; // 与 ContractValidation.MessageContentMaxLength 同值，但故意写死字面量
    private const string ConversationId = "room";
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"pry-sticker-length-{Guid.NewGuid():N}");
    private string _databasePath = null!;
    private MemoryDatabase _database = null!;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        _databasePath = Path.Combine(_root, "memory.db");
        _database = new MemoryDatabase(_databasePath);
        await _database.InitializeAsync(Token);
        await _database.EnsureConversationAsync(ConversationId, "character-length", Token);
    }

    public ValueTask DisposeAsync()
    {
        // 只清自己这条库产生的池（DEF-P1WALK-04：进程级 ClearAllPools 会拆掉并行用例的句柄）。
        SqliteTestPools.ClearAndDeleteRoot(_databasePath, _root);
        return ValueTask.CompletedTask;
    }

    /// <summary>会话正文的两个写入入口。两个都得被同一把尺量到，所以行为断言一律按这两个入口各跑一遍。</summary>
    public enum MessageEntry
    {
        /// <summary><c>ConversationSessionService.SubmitAsync</c>（<c>POST conversations/{id}/turns</c>）。</summary>
        TurnSubmission,

        /// <summary><c>ConversationApplicationService.AddMessageAsync</c>（<c>POST conversations/{id}/messages</c>）。</summary>
        DirectMessage,
    }

    [Theory]
    [InlineData(MessageEntry.TurnSubmission)]
    [InlineData(MessageEntry.DirectMessage)]
    public async Task Content_accompanying_a_sticker_is_rejected_over_the_shared_limit(MessageEntry entry)
    {
        var rejection = await RejectAsync(entry, new string('字', ContentLimit + 1), stickerId: "happy");

        Assert.Equal("content", rejection.Field);
        Assert.Contains(ContentLimit.ToString(), rejection.Message, StringComparison.Ordinal);
        // 拒绝必须发生在落库之前：超长的那一条不许留在会话里。
        Assert.Empty(await _database.GetRecentMessagesAsync(ConversationId, 10, Token));
    }

    [Theory]
    [InlineData(MessageEntry.TurnSubmission)]
    [InlineData(MessageEntry.DirectMessage)]
    public async Task Plain_text_message_keeps_the_same_limit_it_always_had(MessageEntry entry)
    {
        // 补贴纸那一支不许把纯文本那一支的尺带松：这里用与贴纸用例同一个长度、同一个字段名。
        var rejection = await RejectAsync(entry, new string('字', ContentLimit + 1), stickerId: null);

        Assert.Equal("content", rejection.Field);
        Assert.Contains(ContentLimit.ToString(), rejection.Message, StringComparison.Ordinal);
        Assert.Empty(await _database.GetRecentMessagesAsync(ConversationId, 10, Token));
    }

    [Fact]
    public void The_shared_ruler_sits_at_the_original_ceiling_and_tolerates_both_message_shapes()
    {
        // 上限不许为凑绿灯被调大：这个数字在这里被钉死，改动即红。
        Assert.Equal(ContentLimit, ContractValidation.MessageContentMaxLength);
        // 带贴纸的分支：整好达到上限必须放行，而且文本一个都不许少（不许用"丢掉贴纸文本"绕过校验）。
        var atLimit = new string('字', ContentLimit);
        Assert.Equal(atLimit, ContractValidation.MessageContent(atLimit, requireText: false));
        Assert.Equal(atLimit, ContractValidation.MessageContent(atLimit, requireText: true));
        Assert.Equal(ContentLimit, ContractValidation.MessageContent(atLimit, requireText: false).Length);
        // 只发表情不带文本照旧允许；带文本就先去空白再判长度。
        Assert.Equal("", ContractValidation.MessageContent(null, requireText: false));
        Assert.Equal("晚安", ContractValidation.MessageContent("  晚安 \n", requireText: false));
        // 纯文本消息仍然不许空。
        var empty = Assert.Throws<ApiValidationException>(() => ContractValidation.MessageContent("   ", requireText: true));
        Assert.Equal("content", empty.Field);
    }

    [Fact]
    public async Task Sticker_message_under_the_limit_is_stored_with_both_the_text_and_the_sticker()
    {
        var service = new ConversationApplicationService(_database);
        var message = await service.AddMessageAsync(ConversationId,
            new CreateMessageRequest(ChatRole.User, "  晚安，今天也辛苦了  ", StickerId: "happy"), Token);

        Assert.Equal("happy", message.StickerId);
        Assert.Equal("晚安，今天也辛苦了", message.Content);
    }

    [Fact]
    public void Both_message_entries_call_the_one_ruler_and_no_second_ceiling_is_declared()
    {
        // 源码门（与 CharacterIntelligenceSeamTests 同一手法）：两个入口的校验点必须在同一处，
        // 只补一个入口、或者再另起一套上限语义，都会在这里红；行为断言守不住"第二把尺藏在别处"。
        var root = RepositoryRoot();
        foreach (var relative in new[]
                 {
                     Path.Combine("src", "Pry.Api", "Services", "ConversationSessionService.cs"),
                     Path.Combine("src", "Pry.Api", "Services", "ConversationApplicationService.cs"),
                 })
        {
            var source = File.ReadAllText(Path.Combine(root, relative));
            Assert.Equal(1, source.Split("ContractValidation.MessageContent(request.Content, requireText: request.StickerId is null)",
                StringSplitOptions.None).Length - 1);
            Assert.DoesNotContain("Required(request.Content", source, StringComparison.Ordinal);
            Assert.DoesNotContain("request.Content?.Trim() ?? \"\"", source, StringComparison.Ordinal);
        }

        var declared = new List<string>();
        foreach (var directory in new[] { Path.Combine(root, "src", "Pry.Api"), Path.Combine(root, "src", "Pry.Contracts") })
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                var hits = File.ReadAllText(file).Split("20_000", StringSplitOptions.None).Length - 1;
                for (var index = 0; index < hits; index++) declared.Add(file);
            }
        }

        // 消息正文的上限只允许有一个声明点：就是契约里那条 MessageContentMaxLength。
        var sole = Assert.Single(declared);
        Assert.Equal("src/Pry.Contracts/ApiContracts.cs",
            Path.GetRelativePath(root, sole).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    private async Task<ApiValidationException> RejectAsync(MessageEntry entry, string content, string? stickerId) =>
        entry switch
        {
            MessageEntry.TurnSubmission => await RejectFromSessionAsync(content, stickerId),
            MessageEntry.DirectMessage => await RejectFromApplicationAsync(content, stickerId),
            _ => throw new ArgumentOutOfRangeException(nameof(entry), entry, "未知的消息写入入口"),
        };

    private async Task<ApiValidationException> RejectFromSessionAsync(string content, string? stickerId)
    {
        var configuration = NewConfiguration();
        await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var runtime = new BackendRuntime(configuration, registry, NullLogger<BackendRuntime>.Instance);
        await runtime.StartAsync(Token);
        await using var sessions = new ConversationSessionService(_database, runtime, new MediaAssetStore(configuration),
            NullLogger<ConversationSessionService>.Instance);
        return await Assert.ThrowsAsync<ApiValidationException>(() =>
            sessions.SubmitAsync(ConversationId, new SubmitTurnRequest(content, stickerId), Token));
    }

    private async Task<ApiValidationException> RejectFromApplicationAsync(string content, string? stickerId)
    {
        var service = new ConversationApplicationService(_database);
        return await Assert.ThrowsAsync<ApiValidationException>(() => service.AddMessageAsync(ConversationId,
            new CreateMessageRequest(ChatRole.User, content, StickerId: stickerId), Token));
    }

    private IConfiguration NewConfiguration()
    {
        var configuration = new ConfigurationManager();
        configuration["Pry:DataDirectory"] = _root;
        configuration["Pry:ModelsDirectory"] = Path.Combine(_root, "models");
        return configuration;
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null;
             directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Pry.slnx"))) return directory.FullName;
        throw new InvalidOperationException("从测试程序集出发找不到仓库根，无法核对源码约束。");
    }
}
