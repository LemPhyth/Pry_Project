using Microsoft.Data.Sqlite;
using Pry.Core.Abstractions;
using Pry.Core.Expression;
using Pry.Core.Intelligence;
using System.Globalization;

namespace Pry.Core.Memory;

/// <summary>
/// 学习状态的落库原语（RT-003 缺口 ①，DEF-P3STATE-01）。判据本体在
/// `tests/Pry.Core.Tests/IntelligenceRepositoryConformanceTests.cs`，这里只是它的第二种实现；
/// 与 Runtime 那片一样，SQL 不出本层，`Pry.Core.Intelligence` 与 `Pry.Core.Expression` 里不出现 SQLite。
///
/// <para>
/// 时间一律以 UTC 的 "O" 形式落库，因为排序走的是文本序：`updated_at DESC` 与 `observed_at DESC`
/// 要等于时间序，前提是同一张表里不出现两种偏移写法。读回来只保证时刻相同、不保证原偏移——
/// 一致性用例的 <c>AssertInstant</c> 把这个口径写死在那里了。
/// </para>
/// </summary>
public sealed partial class MemoryDatabase
{
    private static async Task InitializeIntelligenceAsync(SqliteConnection connection, CancellationToken token,
        SqliteTransaction? transaction = null)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS character_state(
              character_id TEXT NOT NULL,
              user_id TEXT NOT NULL,
              schema_version INTEGER NOT NULL,
              familiarity REAL NOT NULL,
              trust REAL NOT NULL,
              closeness REAL NOT NULL,
              shared_context_strength REAL NOT NULL,
              exchange_count INTEGER NOT NULL,
              last_exchange_at TEXT NULL,
              repairs_accepted INTEGER NOT NULL,
              boundary_friction REAL NOT NULL,
              state_version INTEGER NOT NULL,
              valence REAL NOT NULL,
              arousal REAL NOT NULL,
              interest REAL NOT NULL,
              fatigue REAL NOT NULL,
              irritation REAL NOT NULL,
              mood_updated_at TEXT NOT NULL,
              baseline_valence REAL NOT NULL,
              baseline_arousal REAL NOT NULL,
              PRIMARY KEY(character_id,user_id)
            );
            CREATE TABLE IF NOT EXISTS behavior_pattern(
              behavior_pattern_id TEXT PRIMARY KEY,
              character_instance_id TEXT NOT NULL,
              scenario TEXT NOT NULL,
              strategy TEXT NOT NULL,
              confidence REAL NOT NULL,
              usage_count INTEGER NOT NULL,
              success_count INTEGER NOT NULL,
              failure_count INTEGER NOT NULL,
              outcome_average REAL NOT NULL,
              created_at TEXT NOT NULL,
              updated_at TEXT NOT NULL,
              last_used_at TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_behavior_pattern_instance
              ON behavior_pattern(character_instance_id,updated_at DESC,behavior_pattern_id);
            CREATE TABLE IF NOT EXISTS behavior_pattern_turn_key(
              behavior_pattern_id TEXT NOT NULL,
              ordinal INTEGER NOT NULL,
              turn_key TEXT NOT NULL,
              PRIMARY KEY(behavior_pattern_id,ordinal),
              FOREIGN KEY(behavior_pattern_id) REFERENCES behavior_pattern(behavior_pattern_id) ON DELETE CASCADE
            );
            CREATE TABLE IF NOT EXISTS expression_pattern(
              id TEXT PRIMARY KEY,
              character_instance_id TEXT NOT NULL,
              situation TEXT NOT NULL,
              style TEXT NOT NULL,
              tone TEXT NOT NULL,
              confidence REAL NOT NULL,
              usage_count INTEGER NOT NULL,
              evidence_count INTEGER NOT NULL,
              created_at TEXT NOT NULL,
              updated_at TEXT NOT NULL,
              last_used_at TEXT NULL,
              source TEXT NOT NULL,
              suppressed INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_expression_pattern_instance
              ON expression_pattern(character_instance_id,updated_at DESC,id);
            CREATE TABLE IF NOT EXISTS expression_pattern_evidence(
              expression_pattern_id TEXT NOT NULL,
              ordinal INTEGER NOT NULL,
              evidence_id TEXT NOT NULL,
              PRIMARY KEY(expression_pattern_id,ordinal),
              FOREIGN KEY(expression_pattern_id) REFERENCES expression_pattern(id) ON DELETE CASCADE
            );
            CREATE TABLE IF NOT EXISTS expression_pattern_turn_key(
              expression_pattern_id TEXT NOT NULL,
              ordinal INTEGER NOT NULL,
              turn_key TEXT NOT NULL,
              PRIMARY KEY(expression_pattern_id,ordinal),
              FOREIGN KEY(expression_pattern_id) REFERENCES expression_pattern(id) ON DELETE CASCADE
            );
            CREATE TABLE IF NOT EXISTS behavior_experience(
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              character_instance_id TEXT NOT NULL,
              conversation_id TEXT NOT NULL,
              turn_id TEXT NOT NULL,
              scenario TEXT NOT NULL,
              intent TEXT NOT NULL,
              outcome TEXT NOT NULL,
              observed_at TEXT NOT NULL,
              follow_up_evidence TEXT NULL,
              feedback_is_explicit INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_behavior_experience_instance
              ON behavior_experience(character_instance_id,observed_at DESC,id DESC);
            """;
        await command.ExecuteNonQueryAsync(token);
    }

    private static string Stamp(DateTimeOffset value) => value.ToUniversalTime().ToString("O");

    private static string? Stamp(DateTimeOffset? value) => value?.ToUniversalTime().ToString("O");

    private static DateTimeOffset ParseStamp(string value) => DateTimeOffset.Parse(value,
        CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    /// <summary>版本落后即拒绝；等于当前版本允许重写，那是重试幂等，不是放宽比较。</summary>
    public async Task<bool> SaveCharacterStateAsync(CharacterStateRecord record,
        CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO character_state(character_id,user_id,schema_version,familiarity,trust,closeness,
              shared_context_strength,exchange_count,last_exchange_at,repairs_accepted,boundary_friction,
              state_version,valence,arousal,interest,fatigue,irritation,mood_updated_at,baseline_valence,
              baseline_arousal)
            VALUES($character,$user,$schema,$familiarity,$trust,$closeness,$shared,$exchange,$lastExchange,
              $repairs,$friction,$stateVersion,$valence,$arousal,$interest,$fatigue,$irritation,$moodAt,
              $baselineValence,$baselineArousal)
            ON CONFLICT(character_id,user_id) DO UPDATE SET
              schema_version=excluded.schema_version,familiarity=excluded.familiarity,trust=excluded.trust,
              closeness=excluded.closeness,shared_context_strength=excluded.shared_context_strength,
              exchange_count=excluded.exchange_count,last_exchange_at=excluded.last_exchange_at,
              repairs_accepted=excluded.repairs_accepted,boundary_friction=excluded.boundary_friction,
              state_version=excluded.state_version,valence=excluded.valence,arousal=excluded.arousal,
              interest=excluded.interest,fatigue=excluded.fatigue,irritation=excluded.irritation,
              mood_updated_at=excluded.mood_updated_at,baseline_valence=excluded.baseline_valence,
              baseline_arousal=excluded.baseline_arousal
            WHERE character_state.state_version<=excluded.state_version;
            """;
        AddStateParameters(command, record);
        // 那条 WHERE 落空时这条语句改动 0 行，返回数就是 CAS 的判定，不需要第二轮读。
        return await command.ExecuteNonQueryAsync(token) == 1;
    }

    public async Task<CharacterStateRecord?> LoadCharacterStateAsync(string characterId, string userId,
        CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"SELECT {StateColumns} FROM character_state WHERE character_id=$character AND user_id=$user";
        command.Parameters.AddWithValue("$character", characterId);
        command.Parameters.AddWithValue("$user", userId);
        await using var reader = await command.ExecuteReaderAsync(token);
        return await reader.ReadAsync(token) ? ReadState(reader) : null;
    }

    /// <summary>删卡时清掉该角色名下所有用户的关系与 Mood；按字段精确匹配，不做前缀匹配。</summary>
    public async Task<int> PurgeCharacterStateAsync(string characterId, CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM character_state WHERE character_id=$character";
        command.Parameters.AddWithValue("$character", characterId);
        return await command.ExecuteNonQueryAsync(token);
    }

    // 读写两侧的列序必须逐格对齐，否则"忘记写一列"会变成"读回另一列"。列名只在这里各写一次。
    private const string StateColumns = """
        character_id,user_id,schema_version,familiarity,trust,closeness,shared_context_strength,
        exchange_count,last_exchange_at,repairs_accepted,boundary_friction,state_version,valence,arousal,
        interest,fatigue,irritation,mood_updated_at,baseline_valence,baseline_arousal
        """;

    private static void AddStateParameters(SqliteCommand command, CharacterStateRecord record)
    {
        command.Parameters.AddWithValue("$character", record.CharacterId);
        command.Parameters.AddWithValue("$user", record.UserId);
        command.Parameters.AddWithValue("$schema", record.SchemaVersion);
        command.Parameters.AddWithValue("$familiarity", record.Familiarity);
        command.Parameters.AddWithValue("$trust", record.Trust);
        command.Parameters.AddWithValue("$closeness", record.Closeness);
        command.Parameters.AddWithValue("$shared", record.SharedContextStrength);
        command.Parameters.AddWithValue("$exchange", record.ExchangeCount);
        command.Parameters.AddWithValue("$lastExchange", (object?)Stamp(record.LastExchangeAt) ?? DBNull.Value);
        command.Parameters.AddWithValue("$repairs", record.RepairsAccepted);
        command.Parameters.AddWithValue("$friction", record.BoundaryFriction);
        command.Parameters.AddWithValue("$stateVersion", record.StateVersion);
        command.Parameters.AddWithValue("$valence", record.Valence);
        command.Parameters.AddWithValue("$arousal", record.Arousal);
        command.Parameters.AddWithValue("$interest", record.Interest);
        command.Parameters.AddWithValue("$fatigue", record.Fatigue);
        command.Parameters.AddWithValue("$irritation", record.Irritation);
        command.Parameters.AddWithValue("$moodAt", Stamp(record.MoodUpdatedAt));
        command.Parameters.AddWithValue("$baselineValence", record.BaselineValence);
        command.Parameters.AddWithValue("$baselineArousal", record.BaselineArousal);
    }

    private static CharacterStateRecord ReadState(SqliteDataReader reader) => new()
    {
        CharacterId = reader.GetString(0),
        UserId = reader.GetString(1),
        SchemaVersion = reader.GetInt32(2),
        Familiarity = reader.GetDouble(3),
        Trust = reader.GetDouble(4),
        Closeness = reader.GetDouble(5),
        SharedContextStrength = reader.GetDouble(6),
        ExchangeCount = reader.GetInt64(7),
        LastExchangeAt = reader.IsDBNull(8) ? null : ParseStamp(reader.GetString(8)),
        RepairsAccepted = reader.GetInt32(9),
        BoundaryFriction = reader.GetDouble(10),
        StateVersion = reader.GetInt64(11),
        Valence = reader.GetDouble(12),
        Arousal = reader.GetDouble(13),
        Interest = reader.GetDouble(14),
        Fatigue = reader.GetDouble(15),
        Irritation = reader.GetDouble(16),
        MoodUpdatedAt = ParseStamp(reader.GetString(17)),
        BaselineValence = reader.GetDouble(18),
        BaselineArousal = reader.GetDouble(19)
    };

    public async Task SaveBehaviorPatternAsync(BehaviorPattern pattern, CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(token);
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO behavior_pattern(behavior_pattern_id,character_instance_id,scenario,strategy,
                  confidence,usage_count,success_count,failure_count,outcome_average,created_at,updated_at,
                  last_used_at)
                VALUES($id,$instance,$scenario,$strategy,$confidence,$usage,$success,$failure,$outcome,
                  $created,$updated,$lastUsed)
                ON CONFLICT(behavior_pattern_id) DO UPDATE SET
                  character_instance_id=excluded.character_instance_id,scenario=excluded.scenario,
                  strategy=excluded.strategy,confidence=excluded.confidence,usage_count=excluded.usage_count,
                  success_count=excluded.success_count,failure_count=excluded.failure_count,
                  outcome_average=excluded.outcome_average,created_at=excluded.created_at,
                  updated_at=excluded.updated_at,last_used_at=excluded.last_used_at;
                """;
            command.Parameters.AddWithValue("$id", pattern.BehaviorPatternId);
            command.Parameters.AddWithValue("$instance", pattern.CharacterInstanceId);
            command.Parameters.AddWithValue("$scenario", pattern.Scenario);
            command.Parameters.AddWithValue("$strategy", pattern.Strategy.ToString());
            command.Parameters.AddWithValue("$confidence", pattern.Confidence);
            command.Parameters.AddWithValue("$usage", pattern.UsageCount);
            command.Parameters.AddWithValue("$success", pattern.SuccessCount);
            command.Parameters.AddWithValue("$failure", pattern.FailureCount);
            command.Parameters.AddWithValue("$outcome", pattern.OutcomeAverage);
            command.Parameters.AddWithValue("$created", Stamp(pattern.CreatedAt));
            command.Parameters.AddWithValue("$updated", Stamp(pattern.UpdatedAt));
            command.Parameters.AddWithValue("$lastUsed",
                (object?)Stamp(pattern.LastUsedAt) ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(token);
        }

        // 回放守卫整串替换：留旧行再补差集会改变顺序，而顺序本身就是判据。
        await ReplaceGuardRowsAsync(connection, transaction, "behavior_pattern_turn_key",
            "behavior_pattern_id", "turn_key", pattern.BehaviorPatternId, pattern.EvaluatedTurnKeys, token);
        await transaction.CommitAsync(token);
    }

    public async Task<BehaviorPattern?> FindBehaviorPatternAsync(string behaviorPatternId,
        CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        BehaviorPattern? pattern = null;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                $"SELECT {PatternColumns} FROM behavior_pattern WHERE behavior_pattern_id=$id";
            command.Parameters.AddWithValue("$id", behaviorPatternId);
            await using var reader = await command.ExecuteReaderAsync(token);
            if (await reader.ReadAsync(token)) pattern = ReadPattern(reader);
        }
        if (pattern is null) return null;
        var keys = await ReadGuardValuesAsync(connection, "behavior_pattern_turn_key", "behavior_pattern_id",
            "turn_key", behaviorPatternId, token);
        return pattern with { EvaluatedTurnKeys = keys };
    }

    public async Task<IReadOnlyList<BehaviorPattern>> ListBehaviorPatternsAsync(string characterInstanceId,
        CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        var patterns = new List<BehaviorPattern>();
        var ids = new List<string>();
        await using (var command = connection.CreateCommand())
        {
            // 排序走文本序：updated_at 全是同一种 UTC 写法，主键比较用建表默认的 BINARY，等价于 ordinal。
            command.CommandText = $"SELECT {PatternColumns} FROM behavior_pattern " +
                "WHERE character_instance_id=$instance ORDER BY updated_at DESC,behavior_pattern_id";
            command.Parameters.AddWithValue("$instance", characterInstanceId);
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                patterns.Add(ReadPattern(reader));
                ids.Add(reader.GetString(0));
            }
        }
        if (ids.Count == 0) return [];

        var keys = await ReadGuardMapAsync(connection, "behavior_pattern_turn_key", "behavior_pattern_id",
            "turn_key", ids, token);
        return [.. patterns.Select(x => x with
        {
            EvaluatedTurnKeys = [.. keys.TryGetValue(x.BehaviorPatternId, out var found) ? found : []]
        })];
    }

    public async Task<int> PurgeBehaviorPatternsAsync(string characterInstanceId, CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(token);
        await using (var children = connection.CreateCommand())
        {
            children.Transaction = transaction;
            children.CommandText = """
                DELETE FROM behavior_pattern_turn_key WHERE behavior_pattern_id IN
                  (SELECT behavior_pattern_id FROM behavior_pattern WHERE character_instance_id=$instance);
                """;
            children.Parameters.AddWithValue("$instance", characterInstanceId);
            await children.ExecuteNonQueryAsync(token);
        }
        await using var parents = connection.CreateCommand();
        parents.Transaction = transaction;
        parents.CommandText = "DELETE FROM behavior_pattern WHERE character_instance_id=$instance";
        parents.Parameters.AddWithValue("$instance", characterInstanceId);
        // 先清子表再数父表：返回的必须是"清掉了几个模式"，不能是级联顺带带走的行数。
        var removed = await parents.ExecuteNonQueryAsync(token);
        await transaction.CommitAsync(token);
        return removed;
    }

    private const string PatternColumns = """
        behavior_pattern_id,character_instance_id,scenario,strategy,confidence,usage_count,success_count,
        failure_count,outcome_average,created_at,updated_at,last_used_at
        """;

    private static BehaviorPattern ReadPattern(SqliteDataReader reader) => new()
    {
        BehaviorPatternId = reader.GetString(0),
        CharacterInstanceId = reader.GetString(1),
        Scenario = reader.GetString(2),
        Strategy = Enum.Parse<ResponseIntent>(reader.GetString(3)),
        Confidence = reader.GetDouble(4),
        UsageCount = reader.GetInt64(5),
        SuccessCount = reader.GetInt64(6),
        FailureCount = reader.GetInt64(7),
        OutcomeAverage = reader.GetDouble(8),
        CreatedAt = ParseStamp(reader.GetString(9)),
        UpdatedAt = ParseStamp(reader.GetString(10)),
        LastUsedAt = reader.IsDBNull(11) ? null : ParseStamp(reader.GetString(11))
    };

    public async Task AppendBehaviorExperienceAsync(BehaviorExperience experience,
        CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(token);
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO behavior_experience(character_instance_id,conversation_id,turn_id,scenario,intent,
                  outcome,observed_at,follow_up_evidence,feedback_is_explicit)
                VALUES($instance,$conversation,$turn,$scenario,$intent,$outcome,$observed,$evidence,$explicit);
                """;
            command.Parameters.AddWithValue("$instance", experience.CharacterInstanceId);
            command.Parameters.AddWithValue("$conversation", experience.ConversationId);
            command.Parameters.AddWithValue("$turn", experience.TurnId);
            command.Parameters.AddWithValue("$scenario", experience.Scenario);
            command.Parameters.AddWithValue("$intent", experience.Intent.ToString());
            command.Parameters.AddWithValue("$outcome", experience.Outcome.ToString());
            command.Parameters.AddWithValue("$observed", Stamp(experience.ObservedAt));
            command.Parameters.AddWithValue("$evidence", (object?)experience.FollowUpEvidence ?? DBNull.Value);
            command.Parameters.AddWithValue("$explicit", experience.FeedbackIsExplicit ? 1 : 0);
            await command.ExecuteNonQueryAsync(token);
        }

        // 上限按角色实例计，所以一个聊得多的角色不能把别人的审计痕迹挤掉。保留数只有进程内实现一处定义。
        await using var evict = connection.CreateCommand();
        evict.Transaction = transaction;
        evict.CommandText = """
            DELETE FROM behavior_experience WHERE character_instance_id=$instance AND id NOT IN(
              SELECT id FROM behavior_experience WHERE character_instance_id=$instance
              ORDER BY observed_at DESC,id DESC LIMIT $retain);
            """;
        evict.Parameters.AddWithValue("$instance", experience.CharacterInstanceId);
        evict.Parameters.AddWithValue("$retain", InMemoryBehaviorExperienceRepository.RetainedPerInstance);
        await evict.ExecuteNonQueryAsync(token);
        await transaction.CommitAsync(token);
    }

    public async Task<IReadOnlyList<BehaviorExperience>> ListBehaviorExperiencesAsync(
        string characterInstanceId, int limit, CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT character_instance_id,conversation_id,turn_id,scenario,intent,outcome,observed_at,
              follow_up_evidence,feedback_is_explicit
            FROM behavior_experience WHERE character_instance_id=$instance
            ORDER BY observed_at DESC,id DESC LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$instance", characterInstanceId);
        command.Parameters.AddWithValue("$limit", Math.Max(1, limit));
        var result = new List<BehaviorExperience>();
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            result.Add(new BehaviorExperience(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), Enum.Parse<ResponseIntent>(reader.GetString(4)),
                Enum.Parse<InteractionOutcome>(reader.GetString(5)), ParseStamp(reader.GetString(6)),
                reader.IsDBNull(7) ? null : reader.GetString(7))
            { FeedbackIsExplicit = reader.GetInt64(8) != 0 });
        }
        return result;
    }

    public async Task<int> PurgeBehaviorExperiencesAsync(string characterInstanceId,
        CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM behavior_experience WHERE character_instance_id=$instance";
        command.Parameters.AddWithValue("$instance", characterInstanceId);
        return await command.ExecuteNonQueryAsync(token);
    }

    public async Task SaveExpressionPatternAsync(ExpressionPattern pattern, CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(token);
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO expression_pattern(id,character_instance_id,situation,style,tone,confidence,
                  usage_count,evidence_count,created_at,updated_at,last_used_at,source,suppressed)
                VALUES($id,$instance,$situation,$style,$tone,$confidence,$usage,$evidence,$created,$updated,
                  $lastUsed,$source,$suppressed)
                ON CONFLICT(id) DO UPDATE SET
                  character_instance_id=excluded.character_instance_id,situation=excluded.situation,
                  style=excluded.style,tone=excluded.tone,confidence=excluded.confidence,
                  usage_count=excluded.usage_count,evidence_count=excluded.evidence_count,
                  created_at=excluded.created_at,updated_at=excluded.updated_at,
                  last_used_at=excluded.last_used_at,source=excluded.source,suppressed=excluded.suppressed;
                """;
            command.Parameters.AddWithValue("$id", pattern.Id);
            command.Parameters.AddWithValue("$instance", pattern.CharacterInstanceId);
            command.Parameters.AddWithValue("$situation", pattern.Situation);
            command.Parameters.AddWithValue("$style", pattern.Style);
            command.Parameters.AddWithValue("$tone", pattern.Tone.ToString());
            command.Parameters.AddWithValue("$confidence", pattern.Confidence);
            command.Parameters.AddWithValue("$usage", pattern.UsageCount);
            command.Parameters.AddWithValue("$evidence", pattern.EvidenceCount);
            command.Parameters.AddWithValue("$created", Stamp(pattern.CreatedAt));
            command.Parameters.AddWithValue("$updated", Stamp(pattern.UpdatedAt));
            command.Parameters.AddWithValue("$lastUsed",
                (object?)Stamp(pattern.LastUsedAt) ?? DBNull.Value);
            command.Parameters.AddWithValue("$source", pattern.Source.ToString());
            command.Parameters.AddWithValue("$suppressed", pattern.Suppressed ? 1 : 0);
            await command.ExecuteNonQueryAsync(token);
        }

        await ReplaceGuardRowsAsync(connection, transaction, "expression_pattern_evidence",
            "expression_pattern_id", "evidence_id", pattern.Id, pattern.AcceptedEvidenceIds, token);
        await ReplaceGuardRowsAsync(connection, transaction, "expression_pattern_turn_key",
            "expression_pattern_id", "turn_key", pattern.Id, pattern.AcceptedTurnKeys, token);
        await transaction.CommitAsync(token);
    }

    public async Task<IReadOnlyList<ExpressionPattern>> ListExpressionPatternsAsync(
        string characterInstanceId, CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        var patterns = new List<ExpressionPattern>();
        var ids = new List<string>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = $"SELECT {ExpressionColumns} FROM expression_pattern " +
                "WHERE character_instance_id=$instance ORDER BY updated_at DESC,id";
            command.Parameters.AddWithValue("$instance", characterInstanceId);
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                patterns.Add(ReadExpression(reader));
                ids.Add(reader.GetString(0));
            }
        }
        if (ids.Count == 0) return [];

        var evidence = await ReadGuardMapAsync(connection, "expression_pattern_evidence",
            "expression_pattern_id", "evidence_id", ids, token);
        var turns = await ReadGuardMapAsync(connection, "expression_pattern_turn_key",
            "expression_pattern_id", "turn_key", ids, token);
        return [.. patterns.Select(x => x with
        {
            AcceptedEvidenceIds = [.. evidence.TryGetValue(x.Id, out var accepted) ? accepted : []],
            AcceptedTurnKeys = [.. turns.TryGetValue(x.Id, out var keys) ? keys : []]
        })];
    }

    public async Task<bool> DeleteExpressionPatternAsync(string expressionPatternId,
        CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(token);
        foreach (var table in new[] { "expression_pattern_evidence", "expression_pattern_turn_key" })
        {
            await using var children = connection.CreateCommand();
            children.Transaction = transaction;
            children.CommandText = $"DELETE FROM {table} WHERE expression_pattern_id=$id";
            children.Parameters.AddWithValue("$id", expressionPatternId);
            await children.ExecuteNonQueryAsync(token);
        }
        await using var parent = connection.CreateCommand();
        parent.Transaction = transaction;
        parent.CommandText = "DELETE FROM expression_pattern WHERE id=$id";
        parent.Parameters.AddWithValue("$id", expressionPatternId);
        var deleted = await parent.ExecuteNonQueryAsync(token) == 1;
        await transaction.CommitAsync(token);
        return deleted;
    }

    public async Task<int> PurgeExpressionPatternsAsync(string characterInstanceId,
        CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(token);
        foreach (var table in new[] { "expression_pattern_evidence", "expression_pattern_turn_key" })
        {
            await using var children = connection.CreateCommand();
            children.Transaction = transaction;
            children.CommandText = $"DELETE FROM {table} WHERE expression_pattern_id IN " +
                "(SELECT id FROM expression_pattern WHERE character_instance_id=$instance)";
            children.Parameters.AddWithValue("$instance", characterInstanceId);
            await children.ExecuteNonQueryAsync(token);
        }
        await using var parents = connection.CreateCommand();
        parents.Transaction = transaction;
        parents.CommandText = "DELETE FROM expression_pattern WHERE character_instance_id=$instance";
        parents.Parameters.AddWithValue("$instance", characterInstanceId);
        var removed = await parents.ExecuteNonQueryAsync(token);
        await transaction.CommitAsync(token);
        return removed;
    }

    private const string ExpressionColumns = """
        id,character_instance_id,situation,style,tone,confidence,usage_count,evidence_count,created_at,
        updated_at,last_used_at,source,suppressed
        """;

    private static ExpressionPattern ReadExpression(SqliteDataReader reader) => new()
    {
        Id = reader.GetString(0),
        CharacterInstanceId = reader.GetString(1),
        Situation = reader.GetString(2),
        Style = reader.GetString(3),
        Tone = Enum.Parse<ExpressionTone>(reader.GetString(4)),
        Confidence = reader.GetDouble(5),
        UsageCount = reader.GetInt64(6),
        EvidenceCount = reader.GetInt64(7),
        CreatedAt = ParseStamp(reader.GetString(8)),
        UpdatedAt = ParseStamp(reader.GetString(9)),
        LastUsedAt = reader.IsDBNull(10) ? null : ParseStamp(reader.GetString(10)),
        Source = Enum.Parse<ExpressionPatternSource>(reader.GetString(11)),
        Suppressed = reader.GetInt64(12) != 0
    };

    /// <summary>守卫引用整串替换：追加、去重或截断都会改变顺序，而顺序本身就是判据的一部分。</summary>
    private static async Task ReplaceGuardRowsAsync(SqliteConnection connection,
        SqliteTransaction transaction, string table, string parentColumn, string valueColumn, string parentId,
        IReadOnlyList<string> values, CancellationToken token)
    {
        await using (var clear = connection.CreateCommand())
        {
            clear.Transaction = transaction;
            clear.CommandText = $"DELETE FROM {table} WHERE {parentColumn}=$id";
            clear.Parameters.AddWithValue("$id", parentId);
            await clear.ExecuteNonQueryAsync(token);
        }
        for (var ordinal = 0; ordinal < values.Count; ordinal++)
        {
            await using var item = connection.CreateCommand();
            item.Transaction = transaction;
            item.CommandText = $"INSERT INTO {table}({parentColumn},ordinal,{valueColumn}) VALUES($id,$ordinal,$value)";
            item.Parameters.AddWithValue("$id", parentId);
            item.Parameters.AddWithValue("$ordinal", ordinal);
            item.Parameters.AddWithValue("$value", values[ordinal]);
            await item.ExecuteNonQueryAsync(token);
        }
    }

    private static async Task<IReadOnlyList<string>> ReadGuardValuesAsync(SqliteConnection connection,
        string table, string parentColumn, string valueColumn, string parentId, CancellationToken token)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"SELECT {valueColumn} FROM {table} WHERE {parentColumn}=$id ORDER BY ordinal";
        command.Parameters.AddWithValue("$id", parentId);
        var result = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token)) result.Add(reader.GetString(0));
        return result;
    }

    /// <summary>三张守卫子表共用：一次取回整批引用，按 (所属行, ordinal) 排好再分组，读侧因此不必逐行发查询。</summary>
    private static async Task<Dictionary<string, List<string>>> ReadGuardMapAsync(SqliteConnection connection,
        string table, string parentColumn, string valueColumn, IReadOnlyList<string> parentIds,
        CancellationToken token)
    {
        var map = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        await using var command = connection.CreateCommand();
        var placeholders = string.Join(',',
            Enumerable.Range(0, parentIds.Count).Select(x => $"$id{x}"));
        command.CommandText = $"SELECT {parentColumn},{valueColumn} FROM {table} " +
            $"WHERE {parentColumn} IN ({placeholders}) ORDER BY {parentColumn},ordinal";
        for (var index = 0; index < parentIds.Count; index++)
            command.Parameters.AddWithValue($"$id{index}", parentIds[index]);
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            var owner = reader.GetString(0);
            if (!map.TryGetValue(owner, out var values)) map[owner] = values = [];
            values.Add(reader.GetString(1));
        }
        return map;
    }
}
