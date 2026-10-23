using Microsoft.Data.Sqlite;
using Pry.Core.Memory;

namespace Pry.Core.Narrative.Storage;

/// <summary>
/// 叙事落库面（HDS-005）的表定义与迁移登记。表名逐字取已登记在盘的缺口清单
/// （`docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md:238` 与
/// `docs/narrative-storage-contract.md:338`），列名逐字取 `NarrativeDocuments.cs` 的契约字段；
/// 权威行号已贴进报告 §一，这里不另造同义名。
/// 登记走既有链（工单 §〇.5.4）：`SchemaMigrationStep` 清单 + `MemorySchemaMigrator.RunAsync` +
/// `schema_info` 平台账；全部步骤的目标版本不超过当前二进制登记版本（`MemorySchemaMigrator.cs:120-123`
/// 的"不得冒领"校验照守），清单以 `VersionAlign` 收尾。本文件不修改任何既有登记行。
/// </summary>
public static class NarrativeStorageSchema
{
    /// <summary>
    /// 叙事侧对齐到的登记版本：直接跟随平台账的当前值，不预占任何新版本号
    /// （备忘 U-11 §3 同令：版本分配权在持久层任务，先占号就是替别人定基线）。
    /// </summary>
    public const int RegisteredVersion = MemoryDatabase.SchemaVersion;

    /// <summary>
    /// 基线建表 DDL（幂等，全部 <c>IF NOT EXISTS</c>）。半路失败时整步由迁移器的事务边界回滚，
    /// 库里不留半套结构；重跑初始化就是补完路径。
    /// </summary>
    public const string BaselineDdl = """
        CREATE TABLE IF NOT EXISTS schema_info(version INTEGER NOT NULL);
        INSERT INTO schema_info(version) SELECT 1 WHERE NOT EXISTS(SELECT 1 FROM schema_info);
        CREATE TABLE IF NOT EXISTS character_instance(
          local_user_id TEXT NOT NULL,
          character_id TEXT NOT NULL,
          instance_id TEXT NOT NULL,
          provenance INTEGER NOT NULL,
          mapping_contract_version INTEGER NOT NULL,
          origin_alias TEXT NULL,
          current_writer_id TEXT NULL,
          engine_mode INTEGER NULL,
          ownership_epoch INTEGER NOT NULL DEFAULT 0,
          highest_ownership_epoch INTEGER NOT NULL DEFAULT 0,
          story_version INTEGER NULL,
          processed_through_seq INTEGER NOT NULL DEFAULT 0,
          applied_commit_count INTEGER NOT NULL DEFAULT 0,
          last_commit_id TEXT NULL,
          updated_at_utc TEXT NULL,
          PRIMARY KEY(local_user_id, character_id)
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ix_character_instance_instance_id ON character_instance(instance_id);
        CREATE TABLE IF NOT EXISTS character_instance_alias(
          alias TEXT NOT NULL PRIMARY KEY,
          instance_id TEXT NOT NULL,
          FOREIGN KEY(instance_id) REFERENCES character_instance(instance_id)
        );
        CREATE TABLE IF NOT EXISTS narrative_entries(
          commit_id TEXT NOT NULL PRIMARY KEY,
          instance_id TEXT NOT NULL,
          conversation_id TEXT NOT NULL,
          batch_id TEXT NOT NULL,
          narrative_prose TEXT NOT NULL,
          actions_json TEXT NOT NULL,
          processed_through_seq INTEGER NOT NULL,
          perceived_input_seqs TEXT NOT NULL,
          consumed_input_seqs TEXT NOT NULL,
          expected_story_version INTEGER NOT NULL,
          deletion_epoch INTEGER NOT NULL,
          ownership_epoch INTEGER NOT NULL,
          contract_version INTEGER NOT NULL,
          content_hash TEXT NOT NULL,
          committed_by_writer TEXT NOT NULL,
          committed_at_utc TEXT NOT NULL,
          proposal_id TEXT NOT NULL,
          max_outbound_segments INTEGER NOT NULL,
          FOREIGN KEY(instance_id) REFERENCES character_instance(instance_id)
        );
        CREATE INDEX IF NOT EXISTS ix_narrative_entries_instance ON narrative_entries(instance_id, commit_id);
        CREATE TABLE IF NOT EXISTS narrative_input_receipts(
          receipt_id INTEGER PRIMARY KEY AUTOINCREMENT,
          instance_id TEXT NOT NULL,
          commit_id TEXT NOT NULL UNIQUE,
          batch_id TEXT NOT NULL,
          processed_through_seq INTEGER NOT NULL,
          consumed_input_seqs TEXT NOT NULL,
          received_at_utc TEXT NOT NULL,
          FOREIGN KEY(commit_id) REFERENCES narrative_entries(commit_id) ON DELETE CASCADE
        );
        CREATE TABLE IF NOT EXISTS narrative_intents(
          intent_id TEXT NOT NULL PRIMARY KEY,
          instance_id TEXT NOT NULL,
          topic TEXT NOT NULL,
          status INTEGER NOT NULL,
          source_commit_id TEXT NOT NULL,
          earliest_due_at_utc TEXT NULL,
          latest_due_at_utc TEXT NULL,
          proactive_send_permitted INTEGER NOT NULL DEFAULT 0,
          FOREIGN KEY(instance_id) REFERENCES character_instance(instance_id)
        );
        CREATE INDEX IF NOT EXISTS ix_narrative_intents_source ON narrative_intents(source_commit_id);
        CREATE TABLE IF NOT EXISTS narrative_outbox(
          commit_id TEXT NOT NULL,
          action_id TEXT NOT NULL,
          segment_index INTEGER NOT NULL,
          instance_id TEXT NOT NULL,
          conversation_id TEXT NOT NULL,
          state INTEGER NOT NULL,
          text TEXT NOT NULL,
          sticker_id TEXT NULL,
          ownership_epoch INTEGER NOT NULL,
          cancel_reason TEXT NULL,
          delivered_message_id TEXT NULL,
          PRIMARY KEY(commit_id, action_id, segment_index),
          CHECK (state <> 1 OR delivered_message_id IS NOT NULL),
          CHECK (length(text) > 0 OR sticker_id IS NOT NULL),
          FOREIGN KEY(commit_id) REFERENCES narrative_entries(commit_id)
        );
        CREATE TABLE IF NOT EXISTS narrative_conversation_deletion_epoch(
          conversation_id TEXT NOT NULL PRIMARY KEY,
          deletion_epoch INTEGER NOT NULL,
          invalidated_at_utc TEXT NOT NULL
        );
        """;

    /// <summary>
    /// 叙事落库面的迁移登记清单：一条基线建表步骤 + 一条版本对齐收尾步骤。
    /// 交给 <see cref="MemorySchemaMigrator.RunAsync"/> 执行——每步一个事务边界，失败整步回滚、
    /// 版本不推进（"半路炸掉的迁移步骤不留痕、重跑补完"的机制来源）。
    /// </summary>
    public static IReadOnlyList<SchemaMigrationStep> BuildSteps() =>
    [
        new(1, "narrative.baseline.schema", "叙事落库面自愈建表", SchemaMigrationMode.SelfHealingBaseline,
            static async (context, token) =>
            {
                await using var command = context.Connection.CreateCommand();
                command.Transaction = context.Transaction;
                command.CommandText = BaselineDdl;
                await command.ExecuteNonQueryAsync(token);
            }),
        new(RegisteredVersion, "narrative.align-schema-version", "叙事落库面登记版本对齐",
            SchemaMigrationMode.VersionAlign,
            static async (context, token) =>
            {
                await using var command = context.Connection.CreateCommand();
                command.Transaction = context.Transaction;
                command.CommandText = "UPDATE schema_info SET version=$version WHERE version<$version";
                command.Parameters.AddWithValue("$version", RegisteredVersion);
                await command.ExecuteNonQueryAsync(token);
            }),
    ];
}
