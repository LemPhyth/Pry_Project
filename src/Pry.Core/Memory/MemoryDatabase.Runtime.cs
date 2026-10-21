using Microsoft.Data.Sqlite;
using Pry.Core.Models;
using Pry.Core.Runtime;

namespace Pry.Core.Memory;

/// <summary>
/// RT-002 持久对话 Runtime 需要的存储原语。全部条件写入都在这里完成，Runtime 侧不出现任何 SQL。
///
/// fencing 依据是会话级 epoch：任何让旧回复失效的动作都会原子递增它，
/// 因此迟到的模型结果在写库这一步就会被拒绝，而不是只靠内存里的取消。
/// </summary>
public sealed partial class MemoryDatabase
{
    private static async Task InitializeRuntimeStateAsync(SqliteConnection connection, CancellationToken token,
        SqliteTransaction? transaction = null)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS conversation_runtime(
              conversation_id TEXT PRIMARY KEY,
              epoch INTEGER NOT NULL DEFAULT 0,
              last_speaker TEXT NULL,
              last_activity_at TEXT NULL,
              consecutive_character_turns INTEGER NOT NULL DEFAULT 0,
              CHECK(epoch >= 0)
            );
            """;
        await command.ExecuteNonQueryAsync(token);
        await EnsureColumnAsync(connection, "reply_plans", "turn_id", "INTEGER NULL", token, transaction);
        await EnsureColumnAsync(connection, "reply_plans", "generation_id", "INTEGER NULL", token, transaction);
        await EnsureColumnAsync(connection, "reply_plans", "fence_epoch", "INTEGER NULL", token, transaction);
        await EnsureColumnAsync(connection, "reply_plans", "status", "TEXT NOT NULL DEFAULT 'open'", token, transaction);
        await EnsureColumnAsync(connection, "reply_plans", "source", "TEXT NULL", token, transaction);
        await EnsureColumnAsync(connection, "reply_plans", "commit_token", "TEXT NULL", token, transaction);
        await EnsureColumnAsync(connection, "reply_plans", "planned_count", "INTEGER NULL", token, transaction);
        await using var index = connection.CreateCommand();
        index.Transaction = transaction;
        index.CommandText = """
            CREATE INDEX IF NOT EXISTS ix_reply_plans_status ON reply_plans(status,id);
            CREATE INDEX IF NOT EXISTS ix_reply_plans_conversation ON reply_plans(conversation_id,id DESC);
            """;
        await index.ExecuteNonQueryAsync(token);
    }

    /// <summary>会话的 fencing epoch。没有行时视为 0，首次使用不需要预建记录。</summary>
    public async Task<long> ReadRuntimeEpochAsync(string conversationId, CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT epoch FROM conversation_runtime WHERE conversation_id=$c";
        command.Parameters.AddWithValue("$c", conversationId);
        var value = await command.ExecuteScalarAsync(token);
        return value is null or DBNull ? 0L : Convert.ToInt64(value);
    }

    /// <summary>原子递增 epoch，使该会话下所有已申请但未投递完的计划在下一次写入时被拒绝。</summary>
    public async Task<long> BumpRuntimeEpochAsync(string conversationId, CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO conversation_runtime(conversation_id,epoch) VALUES($c,1)
              ON CONFLICT(conversation_id) DO UPDATE SET epoch=epoch+1
            RETURNING epoch;
            """;
        command.Parameters.AddWithValue("$c", conversationId);
        return Convert.ToInt64(await command.ExecuteScalarAsync(token) ?? 0L);
    }

    public async Task<RuntimePersistenceState?> LoadRuntimeStateAsync(string conversationId,
        CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT epoch,last_speaker,last_activity_at,consecutive_character_turns
            FROM conversation_runtime WHERE conversation_id=$c;
            """;
        command.Parameters.AddWithValue("$c", conversationId);
        await using var reader = await command.ExecuteReaderAsync(token);
        if (!await reader.ReadAsync(token)) return null;
        var activity = reader.IsDBNull(2) ? "" : reader.GetString(2);
        return new RuntimePersistenceState(reader.GetInt64(0), !reader.IsDBNull(1) && reader.GetString(1) == "assistant",
            string.IsNullOrEmpty(activity) ? DateTimeOffset.MinValue : DateTimeOffset.Parse(activity),
            reader.GetInt32(3));
    }

    public async Task SaveRuntimeStateAsync(string conversationId, RuntimePersistenceState state,
        CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        // epoch 只由 BumpRuntimeEpochAsync 前进；这里即便带上旧值也不得把已失效的会话拉回去。
        command.CommandText = """
            INSERT INTO conversation_runtime(conversation_id,epoch,last_speaker,last_activity_at,
              consecutive_character_turns)
            VALUES($c,$epoch,$speaker,$at,$turns)
            ON CONFLICT(conversation_id) DO UPDATE SET
              last_speaker=excluded.last_speaker,
              last_activity_at=excluded.last_activity_at,
              consecutive_character_turns=excluded.consecutive_character_turns
            WHERE conversation_runtime.epoch=excluded.epoch;
            """;
        command.Parameters.AddWithValue("$c", conversationId);
        command.Parameters.AddWithValue("$epoch", state.Epoch);
        command.Parameters.AddWithValue("$speaker", state.LastSpeakerWasCharacter ? "assistant" : "user");
        command.Parameters.AddWithValue("$at", state.LastActivityAt.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$turns", state.ConsecutiveCharacterTurns);
        await command.ExecuteNonQueryAsync(token);
    }

    /// <summary>
    /// 申请投递：只有在 fenceEpoch 仍等于会话当前 epoch 时才登记计划与其待投递行。
    /// 返回 Approved=false 时本轮一个气泡都不会发出。
    /// </summary>
    public async Task<DeliveryPermission> RequestRuntimeDeliveryAsync(string conversationId, TurnIdentity turn,
        long fenceEpoch, int plannedMessageCount, string source, string? commitToken,
        IReadOnlyList<RuntimePlannedMessage> messages, CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(token);
        await using (var check = connection.CreateCommand())
        {
            check.Transaction = transaction;
            check.CommandText = """
                SELECT epoch FROM conversation_runtime WHERE conversation_id=$c;
                """;
            check.Parameters.AddWithValue("$c", conversationId);
            var value = await check.ExecuteScalarAsync(token);
            var current = value is null or DBNull ? 0L : Convert.ToInt64(value);
            if (current != fenceEpoch)
            {
                await transaction.RollbackAsync(token);
                return new DeliveryPermission(false, turn, 0, conversationId, fenceEpoch, source, commitToken,
                    "epoch_mismatch");
            }
        }

        long planId;
        await using (var plan = connection.CreateCommand())
        {
            plan.Transaction = transaction;
            plan.CommandText = """
                INSERT INTO reply_plans(conversation_id,created_at,turn_id,generation_id,fence_epoch,status,
                  source,commit_token,planned_count)
                VALUES($c,$at,$turn,$generation,$epoch,'open',$source,$token,$count);
                SELECT last_insert_rowid();
                """;
            plan.Parameters.AddWithValue("$c", conversationId);
            plan.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O"));
            plan.Parameters.AddWithValue("$turn", turn.TurnId);
            plan.Parameters.AddWithValue("$generation", turn.GenerationId);
            plan.Parameters.AddWithValue("$epoch", fenceEpoch);
            plan.Parameters.AddWithValue("$source", source);
            plan.Parameters.AddWithValue("$token", (object?)commitToken ?? DBNull.Value);
            plan.Parameters.AddWithValue("$count", plannedMessageCount);
            planId = Convert.ToInt64(await plan.ExecuteScalarAsync(token) ?? 0L);
        }

        foreach (var message in messages)
        {
            await using var item = connection.CreateCommand();
            item.Transaction = transaction;
            item.CommandText = """
                INSERT INTO planned_messages(reply_plan_id,sequence,type,content,sticker_id,state)
                VALUES($p,$s,$t,$content,$sticker,'pending');
                """;
            item.Parameters.AddWithValue("$p", planId);
            item.Parameters.AddWithValue("$s", message.Sequence);
            item.Parameters.AddWithValue("$t", message.IsSticker ? "sticker" : "text");
            item.Parameters.AddWithValue("$content", (object?)message.Text ?? DBNull.Value);
            item.Parameters.AddWithValue("$sticker", (object?)message.StickerId ?? DBNull.Value);
            await item.ExecuteNonQueryAsync(token);
        }
        await transaction.CommitAsync(token);
        return new DeliveryPermission(true, turn, planId, conversationId, fenceEpoch, source, commitToken);
    }

    /// <summary>
    /// 条件投递单个气泡。计划已关闭、会话 epoch 已推进、序号不符或该条已投递时返回 null，
    /// 这就是"过时结果不得发送"和"重启不重发"的落库级保证。
    /// </summary>
    public async Task<long?> CommitRuntimeBubbleAsync(DeliveryPermission permission, int sequence,
        RuntimePlannedMessage message, CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(token);
        var deliveredAt = DateTimeOffset.UtcNow;
        await using (var guard = connection.CreateCommand())
        {
            guard.Transaction = transaction;
            guard.CommandText = """
                UPDATE planned_messages SET state='delivered', delivered_at=$at
                WHERE reply_plan_id=$p AND sequence=$s AND state='pending'
                  AND NOT EXISTS(SELECT 1 FROM planned_messages earlier
                                 WHERE earlier.reply_plan_id=$p AND earlier.sequence<$s AND earlier.state!='delivered')
                  AND EXISTS(SELECT 1 FROM reply_plans r JOIN conversation_runtime rtm ON rtm.conversation_id=r.conversation_id
                             WHERE r.id=$p AND r.status='open' AND rtm.epoch=$epoch AND r.fence_epoch=$epoch
                               AND r.conversation_id=$conversation AND r.turn_id=$turn AND r.generation_id=$generation);
                """;
            guard.Parameters.AddWithValue("$at", deliveredAt.ToString("O"));
            guard.Parameters.AddWithValue("$p", permission.PlanId);
            guard.Parameters.AddWithValue("$s", sequence);
            guard.Parameters.AddWithValue("$epoch", permission.FenceEpoch);
            guard.Parameters.AddWithValue("$conversation", permission.ConversationId);
            guard.Parameters.AddWithValue("$turn", permission.Identity.TurnId);
            guard.Parameters.AddWithValue("$generation", permission.Identity.GenerationId);
            if (await guard.ExecuteNonQueryAsync(token) != 1)
            {
                await transaction.RollbackAsync(token);
                return null;
            }
        }

        long messageId;
        await using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO messages(conversation_id,role,content,image_path,sticker_id,source,created_at)
                SELECT r.conversation_id,'assistant',$c,NULL,$sticker,$source,$at FROM reply_plans r WHERE r.id=$p;
                SELECT last_insert_rowid();
                """;
            insert.Parameters.AddWithValue("$c", message.Text ?? "");
            insert.Parameters.AddWithValue("$sticker", (object?)message.StickerId ?? DBNull.Value);
            insert.Parameters.AddWithValue("$source", permission.Source);
            insert.Parameters.AddWithValue("$at", deliveredAt.ToString("O"));
            insert.Parameters.AddWithValue("$p", permission.PlanId);
            messageId = Convert.ToInt64(await insert.ExecuteScalarAsync(token) ?? 0L);
        }

        await using (var finish = connection.CreateCommand())
        {
            finish.Transaction = transaction;
            finish.CommandText = """
                UPDATE reply_plans SET status='closed'
                WHERE id=$p AND (SELECT COUNT(*) FROM planned_messages WHERE reply_plan_id=$p AND state='pending')=0;
                UPDATE conversations SET updated_at=$at
                WHERE id=(SELECT conversation_id FROM reply_plans WHERE id=$p);
                """;
            finish.Parameters.AddWithValue("$p", permission.PlanId);
            finish.Parameters.AddWithValue("$at", deliveredAt.ToString("O"));
            await finish.ExecuteNonQueryAsync(token);
        }
        await transaction.CommitAsync(token);
        return messageId;
    }

    public async Task AbandonRuntimePlanAsync(DeliveryPermission permission, int fromSequence,
        CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE planned_messages SET state='cancelled' WHERE reply_plan_id=$p AND sequence>=$s AND state='pending';
            UPDATE reply_plans SET status='cancelled' WHERE id=$p AND status='open';
            """;
        command.Parameters.AddWithValue("$p", permission.PlanId);
        command.Parameters.AddWithValue("$s", fromSequence);
        await command.ExecuteNonQueryAsync(token);
    }

    /// <summary>
    /// 启动时清扫上个进程遗留的 open 计划：只取消，绝不重放。
    /// 返回清扫掉的计划数，供 Developer Mode 与恢复日志使用。
    /// </summary>
    public async Task<IReadOnlyList<long>> SweepStaleRuntimePlansAsync(CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var find = connection.CreateCommand();
        find.CommandText = "SELECT id FROM reply_plans WHERE status='open' ORDER BY id";
        var stale = new List<long>();
        await using (var reader = await find.ExecuteReaderAsync(token))
        {
            while (await reader.ReadAsync(token)) stale.Add(reader.GetInt64(0));
        }
        if (stale.Count == 0) return stale;
        await using var update = connection.CreateCommand();
        update.CommandText = """
            UPDATE planned_messages SET state='cancelled' WHERE state='pending'
              AND reply_plan_id IN (SELECT id FROM reply_plans WHERE status='open');
            UPDATE reply_plans SET status='cancelled' WHERE status='open';
            """;
        await update.ExecuteNonQueryAsync(token);
        return stale;
    }
}
