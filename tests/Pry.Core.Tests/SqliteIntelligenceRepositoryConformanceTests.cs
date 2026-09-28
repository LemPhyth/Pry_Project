using Microsoft.Data.Sqlite;
using Pry.Core.Expression;
using Pry.Core.Intelligence;
using Pry.Core.Memory;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// 学习状态契约的第二种实现：真 SQLite 库（DEF-P3STATE-01，RT-003 落库侧缺口 ①）。
/// 这个类不新增任何一条对存储的要求，也不放宽任何一条继承下来的断言——它只把基类那 22 条判据
/// 从"in-memory 恰好如此"变成"落库之后确实如此"。<c>DEF-P2PLAN-01</c> 记的就是同一个教训：
/// fake 与真实存储判定不同时，只跑 fake 是假绿。
///
/// <para>
/// 下面三条是本实现自有的判据（表结构、前滚、换一个实例重开），因为基类没法对一种它不认识的存储
/// 提要求；它们只加不减。
/// </para>
/// </summary>
public sealed class SqliteIntelligenceRepositoryConformanceTests : IntelligenceRepositoryConformanceTests,
    IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(),
        $"pry-intelligence-{Guid.NewGuid():N}");
    private readonly string _databasePath;
    private readonly MemoryDatabase _database;

    public SqliteIntelligenceRepositoryConformanceTests()
    {
        _databasePath = Path.Combine(_directory, "memory.db");
        _database = new MemoryDatabase(_databasePath);
        // NewStores() 的签名由基类定死是同步的，建库这一步只能就地等它跑完；此后每次读写仍是真异步。
        _database.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    protected override Stores NewStores() => new()
    {
        States = new SqliteCharacterStateRepository(_database),
        Patterns = new SqliteBehaviorPatternRepository(_database),
        Experiences = new SqliteBehaviorExperienceRepository(_database),
        Expressions = new SqliteExpressionPatternRepository(_database)
    };

    /// <summary>
    /// 真库不需要"把内存里的东西压下去"这一步——每条写入都在事务里提交完了。这里要结算的是它下面
    /// 两层仍然活着的缓存：WAL 里尚未合并回主库文件的帧，以及连接池里那条还握着页缓存的连接。
    /// </summary>
    protected override async Task FlushAsync(CancellationToken cancellationToken)
    {
        await using (var connection = OpenConnection())
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        // 只清自己这一条连接串对应的池。ClearAllPools 会把同一进程里别的用例正在用的句柄一起拆掉
        // （DEF-P1WALK-04 的受害面按 21 个测试文件计，本刀不得成为第 22 处）。
        SqliteConnection.ClearPool(OpenConnection());
    }

    private SqliteConnection OpenConnection() => new(ConnectionString);

    /// <summary>
    /// 这三档参数必须与 <see cref="MemoryDatabase"/> 内部那一份逐字相同：连接池按连接串认人，
    /// 抄错一档，上面那道 ClearPool 就变成谁也没清的空操作。
    /// </summary>
    private string ConnectionString => new SqliteConnectionStringBuilder
    {
        DataSource = _databasePath,
        Mode = SqliteOpenMode.ReadWriteCreate,
        Cache = SqliteCacheMode.Shared
    }.ToString();

    public void Dispose()
    {
        SqliteConnection.ClearPool(OpenConnection());
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    /// <summary>换一个新实例读同一个文件，等价于进程重启之后再来一次。</summary>
    private Stores ReopenedStores()
    {
        var reopened = new MemoryDatabase(_databasePath);
        return new Stores
        {
            States = new SqliteCharacterStateRepository(reopened),
            Patterns = new SqliteBehaviorPatternRepository(reopened),
            Experiences = new SqliteBehaviorExperienceRepository(reopened),
            Expressions = new SqliteExpressionPatternRepository(reopened)
        };
    }

    [Fact]
    public async Task Schema_creates_the_intelligence_tables_with_their_indexes_and_cascade()
    {
        Assert.Equal(MemoryDatabase.SchemaVersion, await _database.GetSchemaVersionAsync(Token));
        await using var connection = OpenConnection();
        await connection.OpenAsync(Token);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT name FROM sqlite_master
                WHERE tbl_name IN('character_state','behavior_pattern','behavior_pattern_turn_key',
                  'expression_pattern','expression_pattern_evidence','expression_pattern_turn_key',
                  'behavior_experience')
                ORDER BY name
                """;
            var objects = new List<string>();
            await using var reader = await command.ExecuteReaderAsync(Token);
            while (await reader.ReadAsync(Token)) objects.Add(reader.GetString(0));
            foreach (var table in new[]
            {
                "character_state", "behavior_pattern", "behavior_pattern_turn_key", "expression_pattern",
                "expression_pattern_evidence", "expression_pattern_turn_key", "behavior_experience"
            }) Assert.Contains(table, objects);
            // 两张列表索引是读取顺序的落点，purge 的子表清理也依赖父表能按实例筛。
            Assert.Contains("ix_behavior_pattern_instance", objects);
            Assert.Contains("ix_expression_pattern_instance", objects);
            Assert.Contains("ix_behavior_experience_instance", objects);
        }

        foreach (var (child, parent) in new[]
        {
            ("behavior_pattern_turn_key", "behavior_pattern"),
            ("expression_pattern_evidence", "expression_pattern"),
            ("expression_pattern_turn_key", "expression_pattern")
        })
        {
            await using var pragma = connection.CreateCommand();
            pragma.CommandText = $"PRAGMA foreign_key_list({child})";
            var keys = new List<(string Table, string OnDelete)>();
            await using var reader = await pragma.ExecuteReaderAsync(Token);
            while (await reader.ReadAsync(Token))
                // 列序是 id, seq, table, from, to, on_update, on_delete, match；读错一格就把 on_update
                // 当成 on_delete，判据看着对其实什么都没验（同 ProtocolEventJournalTests）。
                keys.Add((reader.GetString(2), reader.GetString(6)));
            Assert.Contains((parent, "CASCADE"), keys);
        }
    }

    [Fact]
    public async Task An_older_database_rolls_forward_the_intelligence_tables_and_keeps_everything_else()
    {
        await _database.EnsureConversationAsync("room", "card-a", Token);
        var messageId = await _database.AddMessageAsync("room", ChatRole.User, "旧库里就有的话", null, Token);
        await _database.SaveCharacterStateAsync(NewState(stateVersion: 3), Token);
        await _database.SaveBehaviorPatternAsync(NewPattern(), Token);

        // 造出"上一个版本"的库：先删子表再删父表（外键开着，反向删会被拒），并把版本退回它之前。
        await using (var connection = OpenConnection())
        {
            await connection.OpenAsync(Token);
            await using var reset = connection.CreateCommand();
            reset.CommandText = """
                DROP TABLE behavior_pattern_turn_key;
                DROP TABLE expression_pattern_evidence;
                DROP TABLE expression_pattern_turn_key;
                DROP TABLE behavior_pattern;
                DROP TABLE expression_pattern;
                DROP TABLE behavior_experience;
                DROP TABLE character_state;
                UPDATE schema_info SET version=$previous;
                """;
            reset.Parameters.AddWithValue("$previous", MemoryDatabase.SchemaVersion - 1);
            await reset.ExecuteNonQueryAsync(Token);
        }

        await new MemoryDatabase(_databasePath).InitializeAsync(Token);
        Assert.Equal(MemoryDatabase.SchemaVersion, await _database.GetSchemaVersionAsync(Token));
        await using (var connection = OpenConnection())
        {
            await connection.OpenAsync(Token);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT name FROM sqlite_master WHERE type='table'
                  AND name IN('character_state','behavior_pattern','behavior_pattern_turn_key',
                    'expression_pattern','expression_pattern_evidence','expression_pattern_turn_key',
                    'behavior_experience')
                """;
            var rebuilt = new List<string>();
            await using var reader = await command.ExecuteReaderAsync(Token);
            while (await reader.ReadAsync(Token)) rebuilt.Add(reader.GetString(0));
            // 逐张点名：少建一张表（比如漏了守卫子表）在这一步就得变红，而不是等到某条写入撞 no such table。
            Assert.Equal(7, rebuilt.Count);
        }

        // 前滚不得动别的表：旧用户的历史是这一刀唯一不可再生的数据。
        var history = await _database.GetRecentMessagesAsync("room", 10, Token);
        Assert.Equal(messageId, Assert.Single(history).Id);

        // 前滚必须幂等：重复初始化既不能再推版本号，也不能把刚建好的表再拆一遍。
        await new MemoryDatabase(_databasePath).InitializeAsync(Token);
        Assert.Equal(MemoryDatabase.SchemaVersion, await _database.GetSchemaVersionAsync(Token));
        Assert.True(await _database.SaveCharacterStateAsync(NewState(stateVersion: 4), Token));
        Assert.Equal(4L, (await _database.LoadCharacterStateAsync("card-a", "user-1", Token))?.StateVersion);
        Assert.Equal(messageId, Assert.Single(await _database.GetRecentMessagesAsync("room", 10, Token)).Id);
    }

    [Fact]
    public async Task A_reopened_database_reads_back_every_learned_row()
    {
        var stores = NewStores();
        var state = NewState(stateVersion: 8);
        var pattern = NewPattern();
        var expression = NewExpression();
        var experience = NewExperience();

        Assert.True(await stores.States.SaveAsync(state, Token));
        await stores.Patterns.SaveAsync(pattern, Token);
        await stores.Expressions.SaveAsync(expression, Token);
        await stores.Experiences.AppendAsync(experience, Token);
        await FlushAsync(Token);

        // 这个新实例没有经过上一个对象的任何调用，读到同一行才说明那一行在文件里，不在句柄里。
        var reopened = ReopenedStores();
        Assert.Equal(state, await reopened.States.LoadAsync("card-a", "user-1", Token));

        var loadedPattern = await reopened.Patterns.FindAsync(pattern.BehaviorPatternId, Token);
        Assert.NotNull(loadedPattern);
        Assert.Equal(WithoutKeys(pattern), WithoutKeys(loadedPattern));
        Assert.Equal(pattern.EvaluatedTurnKeys, loadedPattern.EvaluatedTurnKeys, StringComparer.Ordinal);

        var loadedExpression = Assert.Single(await reopened.Expressions.ListForCharacterAsync("instance-a", Token));
        Assert.Equal(WithoutLists(expression), WithoutLists(loadedExpression));
        Assert.Equal(expression.AcceptedEvidenceIds, loadedExpression.AcceptedEvidenceIds, StringComparer.Ordinal);
        Assert.Equal(expression.AcceptedTurnKeys, loadedExpression.AcceptedTurnKeys, StringComparer.Ordinal);

        Assert.Equal(experience, Assert.Single(await reopened.Experiences
            .ListForCharacterAsync("instance-a", 10, Token)));
    }

    private static readonly DateTimeOffset Base = new(2026, 9, 20, 10, 0, 0, TimeSpan.FromHours(8));

    private static CharacterStateRecord NewState(long stateVersion) => new()
    {
        CharacterId = "card-a", UserId = "user-1", StateVersion = stateVersion,
        Familiarity = 0.417, Trust = 0.632, Closeness = 0.271, SharedContextStrength = 0.389,
        ExchangeCount = 91, LastExchangeAt = Base, RepairsAccepted = 3, BoundaryFriction = 0.154,
        Valence = 0.236, Arousal = 0.517, Interest = 0.628, Fatigue = 0.143, Irritation = 0.091,
        MoodUpdatedAt = Base.AddMinutes(7), BaselineValence = 0.111, BaselineArousal = 0.444
    };

    private static BehaviorPattern NewPattern() => new()
    {
        BehaviorPatternId = "bp:instance-a:topic:Comfort", CharacterInstanceId = "instance-a",
        Scenario = " 安慰 ", Strategy = ResponseIntent.Comfort, Confidence = 0.61, UsageCount = 12,
        SuccessCount = 9, FailureCount = 2, OutcomeAverage = 0.35, CreatedAt = Base,
        UpdatedAt = Base.AddHours(1), LastUsedAt = Base.AddMinutes(20),
        EvaluatedTurnKeys = ["conv-1turn-1", "conv-1turn-2", "conv-2turn-7"]
    };

    private static ExpressionPattern NewExpression() => new()
    {
        Id = "ep:instance-a:aaa111", CharacterInstanceId = "instance-a", Situation = "轻微吐槽",
        Style = "笑不活了", Tone = ExpressionTone.Playful, Source = ExpressionPatternSource.Learned,
        Confidence = 0.53, UsageCount = 6, EvidenceCount = 4, CreatedAt = Base,
        UpdatedAt = Base.AddHours(1), LastUsedAt = Base.AddMinutes(9),
        AcceptedEvidenceIds = ["fb-1", "fb-2", "fb-3"], AcceptedTurnKeys = ["conv-1turn-1", "conv-1turn-2"]
    };

    private static BehaviorExperience NewExperience() => new("instance-a", "conv-1", "t-9", "topic",
        ResponseIntent.Comfort, InteractionOutcome.PositiveReaction, Base, "fb-9")
    { FeedbackIsExplicit = true };

    private static BehaviorPattern WithoutKeys(BehaviorPattern value) =>
        value with { EvaluatedTurnKeys = [] };

    private static ExpressionPattern WithoutLists(ExpressionPattern value) =>
        value with { AcceptedEvidenceIds = [], AcceptedTurnKeys = [] };
}
