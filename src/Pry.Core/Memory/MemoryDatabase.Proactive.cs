using System.Text.Json;
using Microsoft.Data.Sqlite;
using Pry.Core.Models;

namespace Pry.Core.Memory;

public sealed partial class MemoryDatabase
{
    private static async Task InitializeProactivePoliciesAsync(SqliteConnection connection, CancellationToken token)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS proactive_policies(
              character_id TEXT PRIMARY KEY,
              conversation_id TEXT NULL,
              revision INTEGER NOT NULL CHECK(revision > 0),
              policy_json TEXT NOT NULL,
              FOREIGN KEY(conversation_id) REFERENCES conversations(id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ix_proactive_policies_conversation ON proactive_policies(conversation_id);
            CREATE INDEX IF NOT EXISTS ix_conversations_character ON conversations(character_id,id);
            CREATE TABLE IF NOT EXISTS proactive_state(
              character_id TEXT PRIMARY KEY, attempt_id TEXT NOT NULL UNIQUE,
              last_outcome TEXT NOT NULL, last_attempt_at INTEGER NOT NULL,
              next_attempt_at INTEGER NOT NULL, last_message_id INTEGER NULL
            );
            CREATE TABLE IF NOT EXISTS proactive_deliveries(
              attempt_id TEXT PRIMARY KEY, character_id TEXT NOT NULL,
              conversation_id TEXT NOT NULL, message_id INTEGER NOT NULL UNIQUE, sent_at INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_proactive_deliveries_character_time
              ON proactive_deliveries(character_id,sent_at);
            """;
        await command.ExecuteNonQueryAsync(token);
    }

    public async Task<StoredProactivePolicy?> GetProactivePolicyAsync(string characterId,
        CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT policy_json, revision FROM proactive_policies WHERE character_id=$id";
        command.Parameters.AddWithValue("$id", characterId);
        await using var reader = await command.ExecuteReaderAsync(token);
        return await reader.ReadAsync(token)
            ? new(JsonSerializer.Deserialize<ProactivePolicy>(reader.GetString(0))
                ?? throw new InvalidDataException("Invalid proactive policy."), reader.GetInt64(1)) : null;
    }

    // Target ownership and optimistic concurrency are checked under the same write transaction.
    public async Task<StoredProactivePolicy?> SaveProactivePolicyAsync(ProactivePolicy policy, long expectedRevision,
        CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var transaction = connection.BeginTransaction(deferred: false);
        if (policy.ConversationId is { } conversationId)
        {
            await using var target = connection.CreateCommand();
            target.Transaction = transaction;
            target.CommandText = "SELECT character_id FROM conversations WHERE id=$id";
            target.Parameters.AddWithValue("$id", conversationId);
            if ((await target.ExecuteScalarAsync(token)) as string != policy.CharacterId)
                throw new ProactiveTargetMismatchException();
        }
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO proactive_policies(character_id,conversation_id,revision,policy_json)
            SELECT $id,$conversation,1,$json WHERE $expected=0
            ON CONFLICT(character_id) DO NOTHING;
            """;
        command.Parameters.AddWithValue("$id", policy.CharacterId);
        command.Parameters.AddWithValue("$conversation", (object?)policy.ConversationId ?? DBNull.Value);
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(policy));
        command.Parameters.AddWithValue("$expected", expectedRevision);
        var changed = await command.ExecuteNonQueryAsync(token);
        if (expectedRevision > 0)
        {
            command.CommandText = """
                UPDATE proactive_policies SET conversation_id=$conversation,policy_json=$json,revision=revision+1
                WHERE character_id=$id AND revision=$expected
                """;
            changed = await command.ExecuteNonQueryAsync(token);
        }
        if (changed != 1) return null;
        await transaction.CommitAsync(token);
        return new(policy, expectedRevision + 1);
    }

    public async Task<IReadOnlyList<StoredProactivePolicy>> ListProactivePoliciesAsync(CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT policy_json,revision FROM proactive_policies ORDER BY character_id";
        await using var reader = await command.ExecuteReaderAsync(token);
        var result = new List<StoredProactivePolicy>();
        while (await reader.ReadAsync(token))
            result.Add(new(JsonSerializer.Deserialize<ProactivePolicy>(reader.GetString(0))
                ?? throw new InvalidDataException("Invalid proactive policy."), reader.GetInt64(1)));
        return result;
    }

    public async Task RecoverProactiveAttemptsAsync(CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE proactive_state SET last_outcome='interrupted' WHERE last_outcome='generating'";
        await command.ExecuteNonQueryAsync(token);
    }

    public async Task<ProactiveHistory> GetProactiveHistoryAsync(ProactivePolicy policy, DateTimeOffset now,
        CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        return await ReadProactiveHistoryAsync(connection, null, policy, now, token);
    }

    private static async Task<ProactiveHistory> ReadProactiveHistoryAsync(SqliteConnection connection,
        SqliteTransaction? transaction, ProactivePolicy policy, DateTimeOffset now, CancellationToken token)
    {
        string? attempt = null, outcome = null;
        DateTimeOffset? last = null, next = null;
        long? lastMessage = null;
        await using (var state = connection.CreateCommand())
        {
            state.Transaction = transaction;
            state.CommandText = "SELECT attempt_id,last_outcome,last_attempt_at,next_attempt_at,last_message_id FROM proactive_state WHERE character_id=$id";
            state.Parameters.AddWithValue("$id", policy.CharacterId);
            await using var reader = await state.ExecuteReaderAsync(token);
            if (await reader.ReadAsync(token))
            {
                attempt = reader.GetString(0); outcome = reader.GetString(1);
                last = DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(2));
                next = DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(3));
                lastMessage = reader.IsDBNull(4) ? null : reader.GetInt64(4);
            }
        }
        var zone = TimeZoneInfo.FindSystemTimeZoneById(policy.TimeZoneId);
        var day = TimeZoneInfo.ConvertTime(now, zone).Date;
        static DateTimeOffset Boundary(DateTime local, TimeZoneInfo timeZone)
        {
            // Some zones advance clocks at midnight. Use the first valid minute of that day.
            while (timeZone.IsInvalidTime(local)) local = local.AddMinutes(1);
            if (timeZone.IsAmbiguousTime(local)) return new DateTimeOffset(local, timeZone.GetAmbiguousTimeOffsets(local).Max());
            return TimeZoneInfo.ConvertTimeToUtc(local, timeZone);
        }
        await using var count = connection.CreateCommand();
        count.Transaction = transaction;
        count.CommandText = "SELECT COUNT(*) FROM proactive_deliveries WHERE character_id=$id AND sent_at >= $start AND sent_at < $end";
        count.Parameters.AddWithValue("$id", policy.CharacterId);
        count.Parameters.AddWithValue("$start", Boundary(day, zone).ToUnixTimeMilliseconds());
        count.Parameters.AddWithValue("$end", Boundary(day.AddDays(1), zone).ToUnixTimeMilliseconds());
        var sent = Convert.ToInt32(await count.ExecuteScalarAsync(token));
        var awaiting = false;
        if (lastMessage is not null)
        {
            await using var response = connection.CreateCommand();
            response.Transaction = transaction;
            response.CommandText = """
                SELECT EXISTS(SELECT 1 FROM conversations c JOIN messages m ON m.conversation_id=c.id
                  WHERE c.character_id=$id AND m.role='user' AND m.id>$last)
                """;
            response.Parameters.AddWithValue("$id", policy.CharacterId);
            response.Parameters.AddWithValue("$last", lastMessage.Value);
            awaiting = Convert.ToInt32(await response.ExecuteScalarAsync(token)) == 0;
        }
        return new(attempt, outcome, last, next, lastMessage, sent, awaiting);
    }

    public async Task<ProactiveAttempt?> BeginProactiveAttemptAsync(StoredProactivePolicy stored, DateTimeOffset now,
        CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var transaction = connection.BeginTransaction(deferred: false);
        if (!stored.Policy.Enabled || !await ProactiveTargetCurrentAsync(connection, transaction, stored, token)) return null;
        var history = await ReadProactiveHistoryAsync(connection, transaction, stored.Policy, now, token);
        if (history.LastOutcome == "generating" || history.NextAttemptAt > now ||
            history.SentToday >= stored.Policy.DailyLimit || history.AwaitingUserReply) return null;
        var id = Guid.NewGuid().ToString("N");
        var watermark = await ProactiveWatermarkAsync(connection, transaction, stored.Policy.ConversationId!, token);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO proactive_state(character_id,attempt_id,last_outcome,last_attempt_at,next_attempt_at)
            VALUES($id,$attempt,'generating',$now,$next)
            ON CONFLICT(character_id) DO UPDATE SET attempt_id=$attempt,last_outcome='generating',
              last_attempt_at=$now,next_attempt_at=$next
            """;
        command.Parameters.AddWithValue("$id", stored.Policy.CharacterId);
        command.Parameters.AddWithValue("$attempt", id);
        command.Parameters.AddWithValue("$now", now.ToUnixTimeMilliseconds());
        command.Parameters.AddWithValue("$next", now.AddSeconds(stored.Policy.MinimumIntervalSeconds).ToUnixTimeMilliseconds());
        await command.ExecuteNonQueryAsync(token);
        await transaction.CommitAsync(token);
        return new(id, stored, watermark);
    }

    public async Task<long?> CompleteProactiveAttemptAsync(ProactiveAttempt attempt, string content, DateTimeOffset now,
        CancellationToken token = default, Pry.Core.Runtime.DeliveryPermission? runtimePermission = null)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > 500) throw new ArgumentException("Invalid proactive content.", nameof(content));
        var policy = attempt.Policy.Policy;
        await using var connection = await OpenAsync(token);
        await using var transaction = connection.BeginTransaction(deferred: false);
        if (runtimePermission is { } permission)
        {
            await using var guard = connection.CreateCommand();
            guard.Transaction = transaction;
            guard.CommandText = """
                UPDATE planned_messages SET state='delivered',delivered_at=$at
                WHERE reply_plan_id=$plan AND sequence=0 AND state='pending'
                  AND EXISTS(SELECT 1 FROM reply_plans r JOIN conversation_runtime rt ON rt.conversation_id=r.conversation_id
                    WHERE r.id=$plan AND r.status='open' AND r.planned_count=1 AND r.source='proactive'
                      AND r.commit_token=$attempt AND r.conversation_id=$conversation
                      AND r.fence_epoch=$epoch AND rt.epoch=$epoch);
                """;
            guard.Parameters.AddWithValue("$at", now.ToUniversalTime().ToString("O"));
            guard.Parameters.AddWithValue("$plan", permission.PlanId);
            guard.Parameters.AddWithValue("$attempt", attempt.Id);
            guard.Parameters.AddWithValue("$conversation", policy.ConversationId!);
            guard.Parameters.AddWithValue("$epoch", permission.FenceEpoch);
            if (await guard.ExecuteNonQueryAsync(token) != 1) return null;
        }
        if (!await ProactiveTargetCurrentAsync(connection, transaction, attempt.Policy, token)) return null;
        var history = await ReadProactiveHistoryAsync(connection, transaction, policy, now, token);
        if (history.AttemptId != attempt.Id || history.LastOutcome != "generating" ||
            history.SentToday >= policy.DailyLimit || history.AwaitingUserReply ||
            await ProactiveWatermarkAsync(connection, transaction, policy.ConversationId!, token) != attempt.LastHistoryMessageId)
            return null;
        await using var message = connection.CreateCommand();
        message.Transaction = transaction;
        message.CommandText = """
            INSERT INTO messages(conversation_id,role,content,source,created_at) VALUES($conversation,'assistant',$content,'proactive',$at);
            SELECT last_insert_rowid();
            """;
        message.Parameters.AddWithValue("$conversation", policy.ConversationId!);
        message.Parameters.AddWithValue("$content", content);
        message.Parameters.AddWithValue("$at", now.ToUniversalTime().ToString("O"));
        var messageId = Convert.ToInt64(await message.ExecuteScalarAsync(token));
        await using var receipt = connection.CreateCommand();
        receipt.Transaction = transaction;
        receipt.CommandText = """
            INSERT INTO proactive_deliveries(attempt_id,character_id,conversation_id,message_id,sent_at)
              VALUES($attempt,$id,$conversation,$message,$at);
            UPDATE proactive_state SET last_outcome='sent',last_message_id=$message,
              next_attempt_at=MAX(next_attempt_at,$next) WHERE attempt_id=$attempt;
            UPDATE conversations SET updated_at=$updated WHERE id=$conversation;
            """;
        receipt.Parameters.AddWithValue("$attempt", attempt.Id);
        receipt.Parameters.AddWithValue("$id", policy.CharacterId);
        receipt.Parameters.AddWithValue("$conversation", policy.ConversationId!);
        receipt.Parameters.AddWithValue("$message", messageId);
        receipt.Parameters.AddWithValue("$at", now.ToUnixTimeMilliseconds());
        receipt.Parameters.AddWithValue("$next", now.AddSeconds(policy.MinimumIntervalSeconds).ToUnixTimeMilliseconds());
        receipt.Parameters.AddWithValue("$updated", now.ToUniversalTime().ToString("O"));
        await receipt.ExecuteNonQueryAsync(token);
        if (runtimePermission is { } committed)
        {
            await using var close = connection.CreateCommand();
            close.Transaction = transaction;
            close.CommandText = "UPDATE reply_plans SET status='closed' WHERE id=$plan";
            close.Parameters.AddWithValue("$plan", committed.PlanId);
            await close.ExecuteNonQueryAsync(token);
        }
        await transaction.CommitAsync(token);
        return messageId;
    }

    public async Task EndProactiveAttemptAsync(ProactiveAttempt attempt, string outcome, DateTimeOffset now,
        CancellationToken token = default)
    {
        if (outcome is not ("cancelled" or "failed" or "empty_output" or "invalid_output" or "superseded" or "timed_out"))
            throw new ArgumentException("Invalid attempt outcome.", nameof(outcome));
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE proactive_state SET last_outcome=$outcome,next_attempt_at=MAX(next_attempt_at,$next) WHERE attempt_id=$id AND last_outcome='generating'";
        command.Parameters.AddWithValue("$outcome", outcome); command.Parameters.AddWithValue("$id", attempt.Id);
        command.Parameters.AddWithValue("$next", now.AddSeconds(attempt.Policy.Policy.MinimumIntervalSeconds).ToUnixTimeMilliseconds());
        await command.ExecuteNonQueryAsync(token);
    }

    private static async Task<bool> ProactiveTargetCurrentAsync(SqliteConnection connection, SqliteTransaction transaction,
        StoredProactivePolicy stored, CancellationToken token)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT EXISTS(SELECT 1 FROM proactive_policies p JOIN conversations c ON c.id=p.conversation_id
              WHERE p.character_id=$id AND p.revision=$revision AND p.policy_json=$json AND c.character_id=$id)
            """;
        command.Parameters.AddWithValue("$id", stored.Policy.CharacterId);
        command.Parameters.AddWithValue("$revision", stored.Revision);
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(stored.Policy));
        return Convert.ToInt32(await command.ExecuteScalarAsync(token)) == 1;
    }

    private static async Task<long> ProactiveWatermarkAsync(SqliteConnection connection, SqliteTransaction transaction,
        string conversationId, CancellationToken token)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COALESCE(MAX(id),0) FROM messages WHERE conversation_id=$id";
        command.Parameters.AddWithValue("$id", conversationId);
        return Convert.ToInt64(await command.ExecuteScalarAsync(token));
    }
}
