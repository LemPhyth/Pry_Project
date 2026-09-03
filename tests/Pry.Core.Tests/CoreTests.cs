using Pry.Core.Memory;
using Pry.Core.Models;
using Pry.Core.Prompting;
using Xunit;
using Microsoft.Data.Sqlite;
using Pry.Core.Expression;
using Pry.Core.Abstractions;
using Pry.Core.Inference;
using Pry.Core.TurnTaking;

namespace Pry.Core.Tests;

public sealed class CoreTests
{
    [Fact]
    public void Prompt_contains_identity_state_and_memory()
    {
        var character = new CharacterDefinition { Id = "c", Name = "星", Identity = "陪伴者", Personality = "温柔", SpeechStyle = "简洁" };
        var memory = new MemoryRecord(1, "c", "fact", "用户喜欢雨天", "雨天", .8, null, DateTimeOffset.UtcNow);
        var prompt = new PromptBuilder().Build(new PromptContext(character, new RuntimeState { Mood = "开心" }, [memory], [], [], "你好", null));
        Assert.Contains("你是星", prompt); Assert.Contains("开心", prompt); Assert.Contains("用户喜欢雨天", prompt);
    }

    [Fact]
    public void Legacy_prompt_replaces_structured_character_fields()
    {
        var character = new CharacterDefinition
        {
            Id = "c", Name = "星", Identity = "不应出现的身份", Personality = "不应出现的人格", SpeechStyle = "不应出现的风格",
            PromptMode = CharacterPromptMode.Legacy, LegacySystemPrompt = "你是星。始终用古典而简短的方式说话。"
        };
        var prompt = new PromptBuilder().Build(new PromptContext(character, new RuntimeState(), [], [], [], "你好", null));
        Assert.StartsWith("你是星。始终用古典而简短的方式说话。", prompt);
        Assert.DoesNotContain("不应出现的身份", prompt);
    }

    [Fact]
    public async Task Sqlite_stores_and_retrieves_messages_and_memories()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pry-test-{Guid.NewGuid():N}.db");
        var cancellationToken = TestContext.Current.CancellationToken;
        try
        {
            var db = new MemoryDatabase(path); await db.InitializeAsync(cancellationToken);
            var id = await db.AddMessageAsync("chat", ChatRole.User, "我喜欢下雨天", null, cancellationToken);
            await db.AddMemoryAsync("c", "user_fact", "用户喜欢下雨天", "下雨天", .8, id, cancellationToken);
            Assert.Single(await db.GetRecentMessagesAsync("chat", 10, cancellationToken));
            Assert.Single(await db.SearchMemoriesAsync("c", "下雨天", 6, cancellationToken));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            foreach (var suffix in new[] { "", "-wal", "-shm" }) if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }
    }

    [Fact]
    public async Task Conversation_rooms_are_created_titled_and_listed_by_activity()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pry-test-{Guid.NewGuid():N}.db");
        var cancellationToken = TestContext.Current.CancellationToken;
        try
        {
            var db = new MemoryDatabase(path); await db.InitializeAsync(cancellationToken);
            await db.EnsureConversationAsync("room-a", "character-a", cancellationToken);
            await db.AddMessageAsync("room-a", ChatRole.User, "这是用于生成房间标题的一条较长消息内容", null, cancellationToken);
            await db.EnsureConversationAsync("room-b", "character-b", cancellationToken);

            var rooms = await db.ListConversationsAsync(10, cancellationToken);
            Assert.Equal(2, rooms.Count);
            var room = Assert.Single(rooms, x => x.Id == "room-a");
            Assert.Equal("character-a", room.CharacterId);
            Assert.StartsWith("这是用于生成房间标题", room.Title);
            Assert.Equal(1, room.MessageCount);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            foreach (var suffix in new[] { "", "-wal", "-shm" }) if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }
    }

    [Fact]
    public async Task Conversation_mutations_remove_linked_memory_and_can_be_undone()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pry-test-{Guid.NewGuid():N}.db");
        var cancellationToken = TestContext.Current.CancellationToken;
        try
        {
            var db = new MemoryDatabase(path); await db.InitializeAsync(cancellationToken); await db.EnsureConversationAsync("room", "c", cancellationToken);
            var userId = await db.AddMessageAsync("room", ChatRole.User, "记住我喜欢红茶", null, cancellationToken);
            await db.AddMemoryAsync("c", "user_fact", "用户喜欢红茶", "红茶", .8, userId, cancellationToken);
            var assistantId = await db.AddMessageAsync("room", ChatRole.Assistant, "记住啦", null, cancellationToken);
            await db.AddMessageAsync("room", ChatRole.User, "下一句话", null, cancellationToken);

            var single = await db.DeleteMessageAsync("room", assistantId, cancellationToken);
            Assert.NotNull(single); Assert.Equal(2, (await db.GetRecentMessagesAsync("room", 10, cancellationToken)).Count);
            await db.RestoreConversationMutationAsync(single!, cancellationToken);
            Assert.Equal(3, (await db.GetRecentMessagesAsync("room", 10, cancellationToken)).Count);

            var branch = await db.DeleteMessageAndFollowingAsync("room", userId, cancellationToken);
            Assert.NotNull(branch); Assert.Empty(await db.GetRecentMessagesAsync("room", 10, cancellationToken)); Assert.Empty(await db.ListMemoriesAsync("c", null, cancellationToken));
            await db.RestoreConversationMutationAsync(branch!, cancellationToken);
            Assert.Equal(3, (await db.GetRecentMessagesAsync("room", 10, cancellationToken)).Count); Assert.Single(await db.ListMemoriesAsync("c", null, cancellationToken));

            var folderId = await db.CreateConversationFolderAsync("测试文件夹", cancellationToken);
            await db.MoveConversationToFolderAsync("room", folderId, cancellationToken); await db.SetConversationPinnedAsync("room", true, cancellationToken); await db.RenameConversationAsync("room", "重命名房间", cancellationToken);
            var room = Assert.Single(await db.ListConversationsAsync(10, cancellationToken)); Assert.Equal(folderId, room.FolderId); Assert.True(room.IsPinned); Assert.Equal("重命名房间", room.Title);
            await db.RenameConversationFolderAsync(folderId, "已重命名", cancellationToken);
            Assert.Equal("已重命名", Assert.Single(await db.ListConversationFoldersAsync(cancellationToken)).Name);
            await db.DeleteConversationFolderAsync(folderId, cancellationToken);
            Assert.Empty(await db.ListConversationFoldersAsync(cancellationToken)); Assert.Null(Assert.Single(await db.ListConversationsAsync(10, cancellationToken)).FolderId);
        }
        finally
        {
            SqliteConnection.ClearAllPools(); foreach (var suffix in new[] { "", "-wal", "-shm" }) if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }
    }

    [Fact]
    public async Task Memory_library_supports_create_update_search_and_delete()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pry-test-{Guid.NewGuid():N}.db");
        var cancellationToken = TestContext.Current.CancellationToken;
        try
        {
            var db = new MemoryDatabase(path); await db.InitializeAsync(cancellationToken);
            var id = await db.AddMemoryAsync("c", "preference", "用户偏爱热茶", "饮品", .7, null, cancellationToken);
            Assert.True(id > 0);
            Assert.Equal(id, Assert.Single(await db.ListMemoriesAsync("c", "热茶", cancellationToken)).Id);

            await db.UpdateMemoryAsync(id, "c", "preference", "用户偏爱红茶", "饮品,红茶", .9, cancellationToken);
            var updated = Assert.Single(await db.ListMemoriesAsync("c", "红茶", cancellationToken));
            Assert.Equal(.9, updated.Importance, 3);
            Assert.NotNull(updated.UpdatedAt);

            await db.DeleteMemoryAsync(id, "c", cancellationToken);
            Assert.Empty(await db.ListMemoriesAsync("c", null, cancellationToken));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            foreach (var suffix in new[] { "", "-wal", "-shm" }) if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }
    }

    [Fact]
    public async Task Router_bridges_vision_description_to_a_separate_text_model()
    {
        var text = new FakeChatModel(new ModelProfile { Id = "text", DisplayName = "Text", Capabilities = new ModelCapabilities(Text: true) }, "text reply");
        var vision = new FakeChatModel(new ModelProfile { Id = "vision", DisplayName = "Vision", Capabilities = new ModelCapabilities(Text: true, Vision: true) }, "一只白猫");
        var router = new ModelRouter([text, vision], "text", "vision");

        Assert.True(router.UsesVisionBridge);
        Assert.Same(text, router.Select(needsVision: true));
        Assert.Equal("一只白猫", await router.DescribeImagesAsync("看看它", ["cat.png"], TestContext.Current.CancellationToken));
        Assert.Equal("cat.png", vision.LastImagePath);
        Assert.Contains("不扮演角色", vision.LastSystemPrompt);
    }

    [Fact]
    public async Task Text_attachment_is_bounded_and_added_to_the_prompt()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pry-attachment-{Guid.NewGuid():N}.txt");
        try
        {
            await File.WriteAllTextAsync(path, new string('甲', 17_000), TestContext.Current.CancellationToken);
            var result = await AttachmentTextExtractor.ExtractForPromptAsync(
                new ChatAttachment(path, ChatAttachmentKind.Text, "说明.txt"), TestContext.Current.CancellationToken);
            Assert.StartsWith("[附件：说明.txt]", result);
            Assert.Contains("[中间内容因上下文长度限制已省略", result);
            Assert.True(result.Length < 17_000);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task Reply_planner_delivers_text_attachment_content_to_the_chat_model()
    {
        var root = Path.Combine(Path.GetTempPath(), $"pry-planner-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(root, "memory.db");
        Directory.CreateDirectory(root);
        try
        {
            var database = new MemoryDatabase(databasePath); await database.InitializeAsync(TestContext.Current.CancellationToken);
            await database.AddMessageAsync("chat", ChatRole.User, "这个文件写了什么？", null, TestContext.Current.CancellationToken);
            var model = new FakeChatModel(new ModelProfile { Id = "text", DisplayName = "Text" }, "{\"messages\":[{\"type\":\"text\",\"content\":\"看到了\",\"stickerId\":null}]}");
            var catalog = new StickerCatalog(Path.Combine(root, "missing.json"), Path.Combine(root, "stickers"));
            await catalog.LoadAsync(TestContext.Current.CancellationToken);
            var planner = new ReplyPlanner(database, new PromptBuilder(), new ModelRouter([model], "text"),
                new CharacterDefinition { Id = "c", Name = "星", Identity = "陪伴者", Personality = "温柔", SpeechStyle = "简洁" },
                new RuntimeState(), catalog);

            await planner.PlanAsync("chat", "这个文件写了什么？\n[附件：说明.txt]\n附件里的关键正文", [], new TurnTakingSettings(), TestContext.Current.CancellationToken);

            Assert.Contains("附件里的关键正文", Assert.Single(model.LastMessages, x => x.Role == ChatRole.User).Content);
            Assert.Contains("必须先阅读再回答", model.LastSystemPrompt);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task Bundled_sense_voice_model_can_run_offline()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !Directory.Exists(Path.Combine(root.FullName, "models"))) root = root.Parent;
        Assert.NotNull(root);
        var modelDirectory = Path.Combine(root!.FullName, "models", "sensevoice-small-int8");
        Assert.True(File.Exists(Path.Combine(modelDirectory, "model.int8.onnx")));
        var path = Path.Combine(Path.GetTempPath(), $"pry-silence-{Guid.NewGuid():N}.wav");
        try
        {
            WriteSilenceWave(path, 16000, 8000);
            var recognizer = new SenseVoiceSpeechRecognizer(new SpeechModelProfile
            {
                Id = "sensevoice", DisplayName = "SenseVoice", ModelPath = modelDirectory, Language = "zh"
            });
            Assert.True(recognizer.IsAvailable);
            Assert.NotNull(await recognizer.RecognizeAsync(path, TestContext.Current.CancellationToken));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void Expression_protocol_only_accepts_known_stickers()
    {
        var sticker = new StickerDefinition { Id = "happy", Name = "开心", FilePath = "unused.png", Enabled = true };
        var content = "[PRY_EXPRESSION]{\"emotion\":\"开心\",\"intensity\":0.8,\"stickerId\":\"happy\",\"live2dExpression\":\"happy\",\"live2dMotion\":null}[/PRY_EXPRESSION]\n欢迎回来。";
        var (intent, text) = ExpressionProtocolParser.ParseHeader(content, [sticker]);
        Assert.Equal("happy", intent?.StickerId);
        Assert.Equal("欢迎回来。", text);

        var (invalid, _) = ExpressionProtocolParser.ParseHeader(content.Replace("happy\",\"live2d", "missing\",\"live2d"), [sticker]);
        Assert.Null(invalid?.StickerId);
    }

    private sealed class FakeChatModel(ModelProfile profile, string response) : IChatModel
    {
        public ModelProfile Profile { get; } = profile;
        public string? LastImagePath { get; private set; }
        public string LastSystemPrompt { get; private set; } = "";
        public IReadOnlyList<ChatMessage> LastMessages { get; private set; } = [];

        public async IAsyncEnumerable<string> StreamAsync(string systemPrompt, IReadOnlyList<ChatMessage> messages,
            IReadOnlyList<string>? imagePaths, ChatRequestOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            LastSystemPrompt = systemPrompt;
            LastMessages = messages;
            LastImagePath = imagePaths?.FirstOrDefault();
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return response;
        }
    }

    private static void WriteSilenceWave(string path, int sampleRate, int sampleCount)
    {
        using var stream = File.Create(path); using var writer = new BinaryWriter(stream);
        var byteCount = sampleCount * 2;
        writer.Write("RIFF"u8.ToArray()); writer.Write(36 + byteCount); writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray()); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
        writer.Write(sampleRate); writer.Write(sampleRate * 2); writer.Write((short)2); writer.Write((short)16);
        writer.Write("data"u8.ToArray()); writer.Write(byteCount); writer.Write(new byte[byteCount]);
    }
}
