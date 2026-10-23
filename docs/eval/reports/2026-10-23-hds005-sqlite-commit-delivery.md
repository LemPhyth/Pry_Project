# eval-HDS005-1023｜第十七波·HDS-005 落库面（真实 SQLite：叙事表＋原子提交＋崩溃／部分送达矩阵）

- 报告 id：`eval-HDS005-1023`
- 工单：`docs/dispatch-hds-wave17-20261023.md`（`HDS-WAVE17-1023`）
- 性质：动产品代码的存储面刀（`src/Pry.Core/Narrative/Storage/` 新目录＋`tests/Pry.Core.Tests/` 新文件）；不接 API／Client／App、不动旧链表、不迁旧数据。
- 执行方：冷启动 lane（第十七波）
- 开工时点（当场 `date`）：`Fri Oct 23 16:14:15 2026`

## 〇 入场判据（原样）

主树 `git status --short --branch`（他人名下项＝不读、不改、不提交、不 stash、不 clean）：

```text
## codex/ui-main-window-redesign
M  docs/dispatch-tip1020-02-memory-group-judgment.md
 M docs/plans/PROJECT_PLAN.md
M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs
?? .qoder/
?? docs/character-learning-safety.patch
?? docs/dispatch-hds-wave16-20261023.md
?? docs/dispatch-hds-wave17-20261023.md
?? docs/dispatch-hds-wave18-20261023.md
?? docs/research/HDS_INTERLUDE.md
?? docs/research/HDS_QQ_RESEARCH_INDEX.md
?? docs/research/QQ_BRIDGE.md
```

载体（`git worktree add --detach .local-notes/qa-wave17-1023/carrier def0faf`，2026-10-23 16:1x）：

```text
git status --short | wc -l ＝ 0
git rev-parse HEAD ＝ def0faf5456a5ddcb121f52040ee9e551b07a382
```

## 一 表名与列名的权威来源（原文行号，先取再建表）

| 表 | 权威名 | 取到的原文位置 |
|---|---|---|
| 实例映射账（含实例级水位列与条件写账） | `character_instance` | `docs/narrative-storage-contract.md:338`（§11.2.6：「`character_instance`（自然键 User × Character、当前 `instance_id`、provenance、mapping_contract_version、origin_alias）」）；自然键与五个分量逐字取自该行 |
| 别名账 | `character_instance_alias` | 同上 `docs/narrative-storage-contract.md:338`（「`character_instance_alias`（别名 → 当前实例）两张映射账，**每个别名只有一条当前有效记录**」） |
| 叙事条目 | `narrative_entries` | `docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md:238`（「拟定表/仓储面包括 `character_instances`、`narrative_entries`、`narrative_scene_projections`、`narrative_intents`、`narrative_input_receipts`，以及与现有 reply plan/planned message 合并后的唯一 outbox。具体表名和全局/组件版本由持久层任务分配，**本计划不预占 schema 11 或复用未核准版本**。」）；工单 §一 同文点名 `narrative_entries` |
| 意图 | `narrative_intents` | 同上 v4:238；列名逐字取 `src/Pry.Core/Narrative/NarrativeDocuments.cs:284-299`（`NarrativeIntent` 各字段）|
| 输入处理账（批次收据＋水位推进凭据） | `narrative_input_receipts` | 同上 v4:238；水位列语义＝`docs/narrative-storage-contract.md:332`（「叙事侧将来要落的每一步（实例身份映射、别名账、投递段表、水位列）必须登记成清单里的步骤」）与 `NarrativeDocuments.cs:46-67`（`ProcessedThroughSeq`） |
| outbox 与投递段 | `narrative_outbox` | v4:238 只点名「唯一 outbox」未给表名（「具体表名……由持久层任务分配」）；名取文档自有词：`docs/narrative-storage-contract.md:215/220/231`「叙事 outbox／`NarrativeOutbox`」（`OutboxOwnershipRegistry.cs:22` `SendableTruthSource.NarrativeOutbox`）；行＝投递段，唯一键 `(commit_id, action_id, segment_index)` 逐字取 `NarrativeDocuments.cs:388/421` 与 `docs/narrative-storage-contract.md:188`；其余列逐字取 `NarrativeDocuments.cs:391-413`（`state/text/sticker_id/ownership_epoch/cancel_reason/delivered_message_id`） |
| 会话删除世代账 | `narrative_conversation_deletion_epoch` | 文档未给该账命名（v4:238 把具体表名交给持久层任务）；名由文档既有词构成：`docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md:277`「删除会话须同时关闭生成/投递资格、提升 epoch」＋`NarrativeDocuments.cs:173-174`「删除 epoch」＋`NarrativeCommitWriter.cs:65-74`（按 `conversationId` 记账的既有形状） |

列归属说明（自决项，明写供守门人复核）：§11.2.6 的五个映射分量逐字入 `character_instance`；该行的条件写状态列（`current_writer_id/ownership_epoch/highest_ownership_epoch/engine_mode/story_version/processed_through_seq/applied_commit_count/last_commit_id/updated_at_utc`）是本波按工单 §二.2「条目＋输入处理水位＋意图＋outbox 段同事务」与 §二.6「失权世代／删除后不复活」**必须在真库上判定**而加在同一实例账行上的落地面——`OutboxOwnershipRegistry.cs:47` 与 `CharacterInstanceIdentity.cs:116-117` 原文写明「持久化……由 HDS-001/005 落地」，本波即那个 005；不新造文档已命名的任何同义名。`narrative_scene_projections`（v4:238）不在本波四张表面之内，未建，登记为未做。

版本与登记链（工单 §〇.5.4）：新表登记走既有 `SchemaMigrationStep` 清单＋`MemorySchemaMigrator.RunAsync`＋`schema_info` 平台账（`docs/narrative-storage-contract.md:324` 原文口径），清单以 `VersionAlign` 收尾、全部步骤 `TargetVersion ≤ MemoryDatabase.SchemaVersion(10)`，**不冒领新版本号**（`MemorySchemaMigrator.cs:120-123` 那条校验照守，备忘 U-11 §197 行同令）；高版拒写复用既有 `SchemaVersionTooNewException`（`src/Pry.Core/Memory/SchemaVersionGuard.cs:15`），拒绝位置与 `MemoryDatabase.cs:27-30` 同形——在任何写入（含 WAL PRAGMA）之前。

## 二 落地面构成（本波新增的产品文件与判据文件）

载体路径前缀：`.local-notes/qa-wave17-1023/carrier/`（基点 `def0faf`，入场两行判据见 §〇）。

| 文件 | 角色 | 行数（落盘时） |
|---|---|---|
| `src/Pry.Core/Narrative/Storage/NarrativeStorageSchema.cs` | 基线建表 DDL（七张表，全部 `IF NOT EXISTS`）＋迁移登记清单（`SelfHealingBaseline`＋`VersionAlign` 两步，`RegisteredVersion = MemoryDatabase.SchemaVersion`，不冒领） | 148 |
| `src/Pry.Core/Narrative/Storage/NarrativeSqliteStore.cs` | 落库面本体：初始化（高版拒写先于任何写入，含 WAL PRAGMA）、实例登记／别名账、写入权（acquire/handover/release，代次只增）、`TryCommitAtomicAsync` 同事务条件写、投递段追加／终态转换、会话删除 epoch、重启连接后的只读视图与结构指纹 | 1190 |
| `tests/Pry.Core.Tests/NarrativeSqliteCommitDeliveryTests.cs` | 判据 18 格（A1-A4、B1-B2、C1-C5、D1-D3、E1、F1-F3），全部在临时目录自建库上跑，判定都在**重开连接之后**从库里的行读出；绝不碰 `%LOCALAPPDATA%\PryCompanion` | 704 |

闸门对齐（写判据的取数来源，全部为既有文件的既有行号，未改任何既有成员／条件／阈值）：

- 条件写闸门次序 ＝ `NarrativeCommitWriter.cs:85-183`：身份 108（:85）→ 所有权 300/301/302（:91，`OutboxOwnershipRegistry.cs:204-229`）→ 契约版本 212（:102）→ 内容散列 213（:109）→ 删除 epoch 504（:115）→ 重放 502/505（:123-131）→ 故事版本 501/500（:138-147）→ 水位 204（:149）→ 批次上界 203（:156）→ 消费凭据 215（:163-170）→ 绑定（`NarrativeProseBinder.Bind` :172-176）＋ `NarrativeCommitValidator.Validate` 首拒（:178-183）；生效＝故事+1、水位、lastCommitId、计数+1 与台账登记（:185-197）。落库版每一项都从 `character_instance`／`narrative_entries`／`narrative_conversation_deletion_epoch` 的行读出判定。
- 投递段追加次序 ＝ `DeliveryLedger.cs:66-166`：所有权 → 提交生效（401）→ 会话（216）→ 动作存在（402）→ 非即时（404）→ 唯一键（400，回既有行）→ 负号（403）→ `min(PlannedSegmentCount, MaxSegmentsPerAction)` 上限（404）→ 连号（403）→ 载荷（408）→ 绑定（205/`binding.Failure`）→ 逐字拼回（407）→ 末段封口全长还原（407）→ 强制 Pending、证据置空。
- 终态转换次序 ＝ `DeliveryLedger.cs:171-256`：PreTransition（401→所有权→409→406）→ 证据缺失 410 → 前段未落地 405 → Delivered；取消＝PreTransition → 空原因 406 → Cancelled；落库侧再以 `WHERE state=0` 的 CAS 复核「终态不可翻」。
- 写入权与代次 ＝ `OutboxOwnershipRegistry.cs:69-150/204-229`：空位授票代次＝历史最高+1、释放不回退历史最高（:58-60 原意）、同写者幂等含 303、被持 300、凭据不配 306、迟到代次 302。
- 半路迁移不留痕 ＝ `MemorySchemaMigrator.cs:64-105`（每步一个事务边界、失败整步回滚、版本不推进、`MemoryMigrationException`）与 `MemoryDatabase.cs:27-34`（高版拒写先于 WAL PRAGMA）——两文件均未改，落库面只是新增清单项的使用者。

如实自指：**写判据与跑这轮是同一只手**。判据（测试格）与落地面（store/schema）都出自本 lane 同一只手，红只证「这条闸门线确实拦得住一次定向突变」，不证「判据本身写得对」；交叉复核归守门人。

## 三 构建读数（一次，Release，全解）

载体 `def0faf` 基点上的构建（前置 `game-preflight6-1022.ps1`＝`VERDICT=BUILD_OK`，`free_physical_memory=2.9 GB` WARN 不阻断、已登记）：

```text
已成功生成。
    0 个警告
    0 个错误
```

（同轮迭代说明：首两次构建抓到 1 个本刀新文件内的 CS0136 与一批测试文件 CS8417/CS0136，均为本波新文件自造缺陷，改尽后得到上面的 0/0 读数；未改任何既有文件。）

## 四 反证轮台账（单枚突变·单轮·清洁红·十一字段全登）

通则：每轮=一枚只指向**本刀新产物文件**的定向突变→前置 preflight（`VERDICT=BUILD_OK` 才跑）→一次 dotnet（靶跑 18 格全类）→恰好该格红、其余 17 格绿→`git checkout --` 字节级还原→还原前后 SHA-256 双对→`git status --short` 归零。运行命令统一形如（⑤）：

```text
dotnet test tests/Pry.Core.Tests -c Release --filter "FullyQualifiedName~NarrativeSqliteCommitDeliveryTests" --logger "trx;LogFileName=roundNN-格名.trx" --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave17-1023\trx\roundNN-格名"
```

（cwd＝载体根 `.local-notes/qa-wave17-1023/carrier/`；⑧还原命令＝`git checkout -- <①中的文件>`；文件行号＝载体树内行号。）

两个基线 SHA-256（各轮 ⑨⑩ 只列与此值的一致性）：
- `NarrativeStorageSchema.cs`＝`4a3bb7e9272176357f130e8f9b8cd61e65a2898d4e11ae6c391204dd823f9f7c`
- `NarrativeSqliteStore.cs`＝`6d1618f7c73501924a99591fb68b2810ae07307098b28ae978d98c37fcf2c099`

### 轮 00（清洁基线，无突变）

① 无。④ `git status --short`＝0 行。⑤⑥＝`round00-clean` TRX 目录、类内 18 格。⑦ 读数：`已通过! - 失败: 0，通过: 18，已跳过: 0，总计: 18`。⑨⑩ 不适用。⑪ `VERDICT=BUILD_OK`。

### 轮 R01 → A1（七表齐不齐）

| 字段 | 登记 |
|---|---|
| ① 文件:行 | `src/Pry.Core/Narrative/Storage/NarrativeStorageSchema.cs:49-53` |
| ② 改前 | `CREATE TABLE IF NOT EXISTS character_instance_alias( alias TEXT NOT NULL PRIMARY KEY, instance_id TEXT NOT NULL, FOREIGN KEY(instance_id) REFERENCES character_instance(instance_id) );`（整块） |
| ③ 改后 | 整块删除（模拟别名表漏建） |
| ④ diff stat | `1 file changed, 5 deletions(-)`（hunk `@@ -49,5 +48,0 @@`） |
| ⑤ 命令 | 通则形如，`round01-A1.trx` |
| ⑥ TRX | `trx/round01-A1/round01-A1.trx` |
| ⑦ 红格 | `A1_Initialize_creates_all_narrative_tables_on_a_real_database`；`Assert.Equal() Failure: Values differ / Expected: 1 / Actual: 0`；`NarrativeSqliteCommitDeliveryTests.cs:line 64`（别名表 COUNT 落 0）；读数 `失败: 1，通过: 17，总计: 18` |
| ⑧ 还原 | `git checkout -- src/Pry.Core/Narrative/Storage/NarrativeStorageSchema.cs` |
| ⑨⑩ SHA | 前＝后＝`4a3bb7e9…f9f7c`（双对一致） |
| ⑪ preflight | `free_physical_memory=3.1 GB (gate readings may be invalid; SQLite disk-I/O/OOM shape)` ＋ `VERDICT=BUILD_OK`（WARN 已登记，非 disk I/O／OOM 形态，读数有效） |

### 轮 R02 → A2（重复初始化幂等）

| 字段 | 登记 |
|---|---|
| ① 文件:行 | `NarrativeStorageSchema.cs:29` |
| ② 改前 | `INSERT INTO schema_info(version) SELECT 1 WHERE NOT EXISTS(SELECT 1 FROM schema_info);` |
| ③ 改后 | `INSERT INTO schema_info(version) VALUES(1);`（去掉「表空才播种」守卫） |
| ④ diff stat | `1 file changed, 1 insertion(+), 1 deletion(-)`（hunk `@@ -29 +29 @@`） |
| ⑤ 命令 | 通则形如，`round02-A2.trx` |
| ⑥ TRX | `trx/round02-A2/round02-A2.trx` |
| ⑦ 红格 | `A2_repeated_initialization_is_idempotent_on_the_same_file`；`Assert.Equal() Failure: Values differ / Expected: 1 / Actual: 2`（二次 Initialize 后 `schema_info` 行数）；`:line 81`；`失败: 1，通过: 17，总计: 18` |
| ⑧ 还原 | `git checkout -- src/Pry.Core/Narrative/Storage/NarrativeStorageSchema.cs` |
| ⑨⑩ SHA | 前＝后＝`4a3bb7e9…f9f7c` |
| ⑪ preflight | `VERDICT=BUILD_OK` |

### 轮 R03 → A4（高版库先拒写、不落任何结构）

| 字段 | 登记 |
|---|---|
| ① 文件:行 | `NarrativeSqliteStore.cs:46` |
| ② 改前 | `if (versionBefore > MemoryDatabase.SchemaVersion)` |
| ③ 改后 | `if (versionBefore > MemoryDatabase.SchemaVersion + 10)`（抬高拒写阈值，模拟冒领高版库放行） |
| ④ diff stat | `1 file changed, 1 insertion(+), 1 deletion(-)`（hunk `@@ -46 +46 @@`） |
| ⑤ 命令 | 通则形如，`round03-A4.trx` |
| ⑥ TRX | `trx/round03-A4/round03-A4.trx` |
| ⑦ 红格 | `A4_database_registered_above_the_binary_is_refused_before_any_write`；`Assert.Throws() Failure: No exception was thrown / Expected: typeof(Pry.Core.Memory.SchemaVersionTooNewException)`；`:line 157`；`失败: 1，通过: 17，总计: 18`。（消息里并了一条 `IOException: narrative.db …used by another process`——突变路径让测试在 :157 抛出后跳过了中途的显式 `Dispose`，夹具清理删临时库文件被锁；属突变伴生噪声，非 SQLite disk-I/O／OOM 形态，判定核（Throws 未响）完好。） |
| ⑧ 还原 | `git checkout -- src/Pry.Core/Narrative/Storage/NarrativeSqliteStore.cs` |
| ⑨⑩ SHA | 前＝后＝`6d1618f7…c099` |
| ⑪ preflight | `VERDICT=BUILD_OK` |

### 轮 R04 → B1（同事务把水位写对）

| 字段 | 登记 |
|---|---|
| ① 文件:行 | `NarrativeSqliteStore.cs:570` |
| ② 改前 | `advance.Parameters.AddWithValue("$wm", applied.ProcessedThrough.Seq);` |
| ③ 改后 | `advance.Parameters.AddWithValue("$wm", applied.StoryVersion);`（模拟水位串位） |
| ④ diff stat | `1 file changed, 1 insertion(+), 1 deletion(-)`（hunk `@@ -570 +570 @@`） |
| ⑤ 命令 | 通则形如，`round04-B1.trx` |
| ⑥ TRX | `trx/round04-B1/round04-B1.trx` |
| ⑦ 红格 | `B1_commit_lands_entry_receipt_watermark_intents_and_segments_atomically`；`Assert.Equal() Failure: Values differ / Expected: 102 / Actual: 1`（重开连接读 `ProcessedThrough.Seq`）；`:line 191`；`失败: 1，通过: 17，总计: 18` |
| ⑧ 还原 | `git checkout -- src/Pry.Core/Narrative/Storage/NarrativeSqliteStore.cs` |
| ⑨⑩ SHA | 前＝后＝`6d1618f7…c099` |
| ⑪ preflight | `VERDICT=BUILD_OK` |

### 轮 R05 → B2（半途约束撞库＝整笔回滚、异常原样上抛）

| 字段 | 登记 |
|---|---|
| ① 文件:行 | `NarrativeSqliteStore.cs:599`（意图表 foreach 内 `await command.ExecuteNonQueryAsync(cancellationToken);`） |
| ② 改前 | 该调用裸跑（SqliteException 沿事务自然上抛→整笔回滚） |
| ③ 改后 | 包进 `try { … } catch (SqliteException) { // 突变探针（仅 R05 本轮存在）：吞掉意图表约束异常 }`（模拟吞异常假成功） |
| ④ diff stat | `1 file changed, 8 insertions(+), 1 deletion(-)`（hunk 在意图 foreach 块 :598-600） |
| ⑤ 命令 | 通则形如，`round05-B2.trx` |
| ⑥ TRX | `trx/round05-B2/round05-B2.trx` |
| ⑦ 红格 | `B2_midwrite_constraint_failure_rolls_back_the_whole_commit`；`Assert.Throws() Failure: No exception was thrown / Expected: typeof(Microsoft.Data.Sqlite.SqliteException)`；`:line 211`；`失败: 1，通过: 17，总计: 18`。（并条 `IOException …narrative.db` 噪声同 R03 成因：:211 抛出后跳过 :216 的显式 `store.Dispose()`，夹具清理被锁；非资源形态失败，判定核完好。） |
| ⑧ 还原 | `git checkout -- src/Pry.Core/Narrative/Storage/NarrativeSqliteStore.cs` |
| ⑨⑩ SHA | 前＝后＝`6d1618f7…c099` |
| ⑪ preflight | `gpu_util=3% used=1783MB / free_memory_gb=14.4 / VERDICT=BUILD_OK` |

### 够不着单枚清洁红的格（如实登记，不硬凑）

- **A3（迁移半路失败整步回滚＋重跑补齐）**：回滚机制住在既有 `MemorySchemaMigrator.cs:64-105`（本刀禁改），在本刀新文件里没有任何单点突变能只弄红 A3——把两步清单改坏会连带 A1/B 系全红，不是「恰好该格红」。此格以 18/18 绿佐证，不出反证红。
- **D1（崩溃矩阵·未提交蒸发已提交存活）**：判定点是 SQLite 自身事务提交／WAL 检查点的性质，突变 store 无法在不撞红 B 系的前提下让 D1 单独红。此格只绿不红。
- **D3（部分送达矩阵·重启后仅余未送达段且已送达不可动）**：初判以为无隔离单点，R16 复核改判——「Pending 列举」过滤器（`ListPendingSegmentsAsync :813` 的 `state=0`）确是只由 D3 判定的单独出口，已取到清洁红（见 R16）。
- **C5（终态不可翻）**：初判以为可打，R10 实测松掉写面 CAS 后 18/18 全绿——读取面 `PreTransition :882` 已先拦（判定即 406），两闸冗余，单枚突变穿不过去。详见 R10 轮登记，此格归「够不着」而非未判。

（以下为 R06–R16 各轮。）

### 轮 R06 → C1（末段封口必须全长还原）

| 字段 | 登记 |
|---|---|
| ① 文件:行 | `NarrativeSqliteStore.cs:1025` |
| ② 改前 | `if (priorSegments.Count + 1 == action.PlannedSegmentCount && accumulated.Length != boundSlice.Length)` |
| ③ 改后 | 同上仅把 `!=` 改 `>`（缺字封口不再拒） |
| ④ diff stat | `1 file changed, 1 insertion(+), 1 deletion(-)`（hunk `@@ -1025 +1025 @@`） |
| ⑤ 命令 | 通则形如，`round06-C1.trx` |
| ⑥ TRX | `trx/round06-C1/round06-C1.trx` |
| ⑦ 红格 | `C1_segment_text_must_come_verbatim_from_the_bound_slice`；`Expected: SegmentTextNotFromNarrative / Actual: None`（缺字末段被放行）；`:line 247`；`失败: 1，通过: 17，总计: 18`（并条夹具噪声同 R03） |
| ⑧ 还原 | `git checkout -- src/Pry.Core/Narrative/Storage/NarrativeSqliteStore.cs` |
| ⑨⑩ SHA | 前＝后＝`6d1618f7…c099` |
| ⑪ preflight | `free_memory_gb=7.4 / VERDICT=BUILD_OK` |

### 轮 R07 → C2（段序上限）

| 字段 | 登记 |
|---|---|
| ① 文件:行 | `NarrativeSqliteStore.cs:991` |
| ② 改前 | `if (candidate.SegmentIndex >= ceiling)` |
| ③ 改后 | `if (candidate.SegmentIndex > ceiling)`（idx==上限放行） |
| ④ diff stat | `1 file changed, 1 insertion(+), 1 deletion(-)`（hunk `@@ -991 +991 @@`） |
| ⑤ 命令 | 通则形如，`round07-C2.trx` |
| ⑥ TRX | `trx/round07-C2/round07-C2.trx` |
| ⑦ 红格 | `C2_segment_index_skip_and_ceiling_are_refused_on_the_database`；`Expected: SegmentCountExceeded / Actual: SegmentTextNotFromNarrative`（越限段穿过上限闸后被逐字闸以另一 reason 拦下——仍拒绝不写，但 reason 码错位即红）；`:line 278`；`失败: 1，通过: 17，总计: 18` |
| ⑧ 还原 | 同⑧形如 |
| ⑨⑩ SHA | 前＝后＝`6d1618f7…c099` |
| ⑪ preflight | `free_memory_gb=6.6 / VERDICT=BUILD_OK` |

### 轮 R08 → C3（重复键回带首行）

| 字段 | 登记 |
|---|---|
| ① 文件:行 | `NarrativeSqliteStore.cs:698` |
| ② 改前 | `var duplicate = existing.FirstOrDefault(s => s.SegmentIndex == key.SegmentIndex);` |
| ③ 改后 | `… == key.SegmentIndex + 1);`（查重错位） |
| ④ diff stat | `1 file changed, 1 insertion(+), 1 deletion(-)`（hunk `@@ -698 +698 @@`） |
| ⑤ 命令 | 通则形如，`round08-C3.trx` |
| ⑥ TRX | `trx/round08-C3/round08-C3.trx` |
| ⑦ 红格 | `C3_duplicate_segment_key_returns_the_first_row_and_keeps_its_text`；`Expected: SegmentKeyDuplicate / Actual: SegmentIndexNotContiguous`（重复段穿过查重后被连号闸以另一 reason 拦下）；`:line 296`；`失败: 1，通过: 17，总计: 18` |
| ⑧ 还原 | 同形如 |
| ⑨⑩ SHA | 前＝后＝`6d1618f7…c099` |
| ⑪ preflight | `free_memory_gb=7.1 / VERDICT=BUILD_OK` |

### 轮 R09 → C4（送达必须有聊天历史证据）

| 字段 | 登记 |
|---|---|
| ① 文件:行 | `NarrativeSqliteStore.cs:745` |
| ② 改前 | `if (string.IsNullOrWhiteSpace(deliveredMessageId))` |
| ③ 改后 | `if (string.IsNullOrWhiteSpace(deliveredMessageId) && false)`（证据闸失效） |
| ④ diff stat | `1 file changed, 1 insertion(+), 1 deletion(-)`（hunk `@@ -745 +745 @@`） |
| ⑤ 命令 | 通则形如，`round09-C4.trx` |
| ⑥ TRX | `trx/round09-C4/round09-C4.trx` |
| ⑦ 红格 | `C4_delivery_without_chat_history_evidence_stays_pending_after_reconnect`；`SQLite Error 19: 'CHECK constraint failed: state <> 1 OR delivered_message_id IS NOT NULL'`（应用闸被松后，库侧 CHECK 约束自己拦下空证据写——段保持 Pending 不落；判格按预期 410 拒绝落空即红）；`:line 316`；`失败: 1，通过: 17，总计: 18` |
| ⑧ 还原 | 同形如 |
| ⑨⑩ SHA | 前＝后＝`6d1618f7…c099` |
| ⑪ preflight | `free_memory_gb=7.1 / VERDICT=BUILD_OK` |

### 轮 R10 → C5 尝试（登记为够不着的实测证据，非清洁红）

① `NarrativeSqliteStore.cs:836`；② `WHERE commit_id=$c AND action_id=$a AND segment_index=$i AND state=0;`；③ `… AND state IN (0, 1, 2);`（CAS 不再排除终态）。④ `1 file changed, 1 insertion(+), 1 deletion(-)`。⑤⑥ `round10-C5.trx`。⑦ 读数：`已通过! - 失败: 0，通过: 18，总计: 18`——**未取到红**。原因：C5 的「迟到不得翻终态」由两道独立闸共同把守——`PreTransition :882`（判定即拒 406）在读取面先拦，CAS（:836）只是写面兜底；单枚松掉 CAS 时 :882 已把迟到转换全部拦下，够不着红。此轮本身即双防线存在的实测证据，如实登记为「够不着（冗余双闸）」，不算清洁红。⑧⑩ 还原前后 `6d1618f7…c099` 双对、`DIRTY=0`。⑪ `free_memory_gb=7 / VERDICT=BUILD_OK`。

### 轮 R11 → D2（重放识别看内容散列）

| 字段 | 登记 |
|---|---|
| ① 文件:行 | `NarrativeSqliteStore.cs:1145` |
| ② 改前 | `SELECT content_hash FROM narrative_entries WHERE commit_id=$c` |
| ③ 改后 | `SELECT committed_by_writer FROM narrative_entries WHERE commit_id=$c`（重放比对取错列） |
| ④ diff stat | `1 file changed, 1 insertion(+), 1 deletion(-)`（hunk `@@ -1145 +1145 @@`） |
| ⑤ 命令 | 通则形如，`round11-D2.trx` |
| ⑥ TRX | `trx/round11-D2/round11-D2.trx` |
| ⑦ 红格 | `D2_replaying_an_applied_commit_after_reconnect_is_refused_without_advancing`；`Expected: CommitAlreadyApplied / Actual: DuplicateCommitId`（同一内容重放被误判为 ID 复用）；`:line 398`；`失败: 1，通过: 17，总计: 18`。此轮消息无并条夹具噪声 |
| ⑧ 还原 | 同形如 |
| ⑨⑩ SHA | 前＝后＝`6d1618f7…c099` |
| ⑪ preflight | `free_memory_gb=6.7 / VERDICT=BUILD_OK` |

### 轮 R12 → E1（前段未送达不得跳发）

| 字段 | 登记 |
|---|---|
| ① 文件:行 | `NarrativeSqliteStore.cs:753` |
| ② 改前 | `if (predecessor is not null && predecessor.State != DeliverySegmentState.Delivered)` |
| ③ 改后 | `… predecessor.State == DeliverySegmentState.Cancelled)`（前段 Pending 放行；选此形而非改查表偏移，正是为隔离 D3——查表 `-1`→`+1` 会连带红 D3） |
| ④ diff stat | `1 file changed, 1 insertion(+), 1 deletion(-)`（hunk `@@ -753 +753 @@`） |
| ⑤ 命令 | 通则形如，`round12-E1.trx` |
| ⑥ TRX | `trx/round12-E1/round12-E1.trx` |
| ⑦ 红格 | `E1_later_segment_cannot_land_before_its_predecessor`；`Expected: SegmentPredecessorPending / Actual: None`（后段抢在前段送达之前落了库）；`:line 448`；`失败: 1，通过: 17，总计: 18` |
| ⑧ 还原 | 同形如 |
| ⑨⑩ SHA | 前＝后＝`6d1618f7…c099` |
| ⑪ preflight | `free_memory_gb=6.7 / VERDICT=BUILD_OK` |

### 轮 R13 → F1（失权代次判错方向）

| 字段 | 登记 |
|---|---|
| ① 文件:行 | `NarrativeSqliteStore.cs:907` |
| ② 改前 | `return (false, ownershipEpoch < row.OwnershipEpoch` |
| ③ 改后 | `return (false, ownershipEpoch > row.OwnershipEpoch`（fencing 方向反转：迟到代次被误报成 300 而非 302） |
| ④ diff stat | `1 file changed, 1 insertion(+), 1 deletion(-)`（hunk `@@ -907 +907 @@`） |
| ⑤ 命令 | 通则形如，`round13-F1.trx` |
| ⑥ TRX | `trx/round13-F1/round13-F1.trx` |
| ⑦ 红格 | `F1_fenced_epoch_cannot_commit_or_touch_the_ledger_while_new_ticket_can`；`Expected: OwnershipFenced / Actual: OwnershipNotHeld`；`:line 482`；`失败: 1，通过: 17，总计: 18`。此轮消息无并条夹具噪声 |
| ⑧ 还原 | 同形如 |
| ⑨⑩ SHA | 前＝后＝`6d1618f7…c099` |
| ⑪ preflight | `free_memory_gb=6.9 / VERDICT=BUILD_OK` |

### 轮 R14 → F2（故事版本闸松成单侧）

| 字段 | 登记 |
|---|---|
| ① 文件:行 | `NarrativeSqliteStore.cs:451` |
| ② 改前 | `if (commit.ExpectedStoryVersion != state.StoryVersion)` |
| ③ 改后 | `if (commit.ExpectedStoryVersion > state.StoryVersion)`（迟到侧放行） |
| ④ diff stat | `1 file changed, 1 insertion(+), 1 deletion(-)`（hunk `@@ -451 +451 @@`） |
| ⑤ 命令 | 通则形如，`round14-F2.trx` |
| ⑥ TRX | `trx/round14-F2/round14-F2.trx` |
| ⑦ 红格 | `F2_stale_and_ahead_story_versions_are_refused_on_the_persisted_state`；`Expected: LateCommitRejected / Actual: ScopeEscalation`（故事闸松开后迟到提交被下游另一道拒绝闸以另一 reason 拦下——仍拒绝不写、无半写，但 reason 错位即红）；`:line 514`；`失败: 1，通过: 17，总计: 18`。此轮消息无并条夹具噪声 |
| ⑧ 还原 | 同形如 |
| ⑨⑩ SHA | 前＝后＝`6d1618f7…c099` |
| ⑪ preflight | `free_memory_gb=6.8 / VERDICT=BUILD_OK` |

### 轮 R15 → F3（删除 epoch 复活闸松一档）

| 字段 | 登记 |
|---|---|
| ① 文件:行 | `NarrativeSqliteStore.cs:431` |
| ② 改前 | `if (storedDeletionEpoch > commit.DeletionEpoch)` |
| ③ 改后 | `if (storedDeletionEpoch > commit.DeletionEpoch + 1)`（差一档的复活提交放行） |
| ④ diff stat | `1 file changed, 1 insertion(+), 1 deletion(-)`（hunk `@@ -431 +431 @@`） |
| ⑤ 命令 | 通则形如，`round15-F3.trx` |
| ⑥ TRX | `trx/round15-F3/round15-F3.trx` |
| ⑦ 红格 | `F3_deleted_conversation_stays_deleted_across_reconnect`；`Expected: DeletionEpochAdvanced / Actual: None`（停留在旧删除 epoch 的复活提交竟然被接受——会话删除不可复活这条线确实由此闸单独承担）；`:line 539`；`失败: 1，通过: 17，总计: 18`。此轮消息无并条夹具噪声 |
| ⑧ 还原 | 同形如 |
| ⑨⑩ SHA | 前＝后＝`6d1618f7…c099` |
| ⑪ preflight | `free_memory_gb=6.9 / VERDICT=BUILD_OK` |

### 轮 R16 → D3（重启后补发只认未送达段）

| 字段 | 登记 |
|---|---|
| ① 文件:行 | `NarrativeSqliteStore.cs:813` |
| ② 改前 | `Command(SegmentSelect + " WHERE commit_id=$c AND state=0 ORDER BY action_id, segment_index", null)` |
| ③ 改后 | `… AND state IN (0, 1) …`（已送达段重新混进待补发清单） |
| ④ diff stat | `1 file changed, 1 insertion(+), 1 deletion(-)`（hunk `@@ -813 +813 @@`） |
| ⑤ 命令 | 通则形如，`round16-D3.trx` |
| ⑥ TRX | `trx/round16-D3/round16-D3.trx` |
| ⑦ 红格 | `D3_delivery_lost_midrun_refills_only_the_gap_after_reconnect`；`Assert.Single() Failure: The collection contained 2 items`（重开连接后 Pending 清单把 msg-5 已送达的第 0 段也列了回来——「绝不重发已发的」清单来源正是这道闸）；`:line 423`；`失败: 1，通过: 17，总计: 18`。此轮消息无并条夹具噪声 |
| ⑧ 还原 | 同形如 |
| ⑨⑩ SHA | 前＝后＝`6d1618f7…c099` |
| ⑪ preflight | `free_memory_gb=13.7 / VERDICT=BUILD_OK` |

### 小结（反证账面）

18 格中 **15 枚单枚清洁红**（A1、A2、A4、B1、B2、C1、C2、C3、C4、D2、D3、E1、F1、F2、F3）：每轮一枚突变、恰好该格红、其余 17 格绿、字节级还原双 SHA 对、`git status` 归零。**3 枚够不着**：A3（回滚机理住在禁改的既有迁移器）、D1（提交持久性归 SQLite 本体）、C5（R10 实测：读面＋写面冗余双闸，单枚穿不过）——三格都以 18/18 绿在册，只欠「改坏必响」这一维。夹具噪声登记：R03/R05/R06/R07/R08/R09/R12 的失败消息里并有一条 `IOException …narrative.db used by another process`——突变路径让判格在显式 `Dispose` 之前抛出、清理临时库时文件仍被自己连接占用；属突变伴生噪声，非 SQLite disk-I/O／OOM 资源形态，判定核（reason 码/计数）完好，读数有效。



## 五 终局全量读数（一次，Release，Pry.slnx 四工程）

前置 preflight：`free_memory_gb=13 / VERDICT=BUILD_OK`。命令（cwd＝载体根）：

```text
dotnet test Pry.slnx -c Release --logger "trx;LogFileName=final-full.trx" --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave17-1023\trx\final-full"
```

四工程控制台读数（原样）＋ 残留 TRX `<Counters>`：

| 工程 | 读数 |
|---|---|
| `Pry.Client.Tests.dll` | `已通过! - 失败: 0，通过: 108，已跳过: 0，总计: 108，持续时间: 6 s` |
| `Pry.App.Tests.dll` | `已通过! - 失败: 0，通过: 233，已跳过: 0，总计: 233，持续时间: 7 s` |
| `Pry.Api.Tests.dll` | `已通过! - 失败: 0，通过: 280，已跳过: 0，总计: 280，持续时间: 22 s` |
| `Pry.Core.Tests.dll` | `已通过! - 失败: 0，通过: 639，已跳过: 2，总计: 641，持续时间: 31 s`；TRX：`<Counters total="641" executed="639" passed="639" failed="0" …>` |

对账：`def0faf` 在册合流复采基线 **1244/0/2** ＋ 本波新增 18 格 ＝ **1262/0/2**（108+233+280+641＝1262）——零回归、零自造跳过。两个跳过为 `def0faf` 既有环境门控 `Assert.Skip`（`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`，基线里本就跳）。如实登记：四工程共用同名 `final-full.trx` 互相覆盖（控制台有「正在覆盖结果文件」警告），目录里只剩最后写入的 Core 一份 TRX；逐工程读数以控制台原样为凭，一建一全量是票面预算、本轮不重跑顶数，归守门人归位时可复采。

再声明一次（§二已写）：**写判据与跑这轮是同一只手**。以上读数只证本刀产物在自身判据与既有 1244 格面前自洽，交叉复核归守门人。

## 六 仍未验证（如实清单）

1. **未接生产路径／新表零生产调用方**：`NarrativeSqliteStore` 今天只被本波测试类调用；API/Client/App 均未接线（票面禁改），真实运行链仍走内存契约，落库面是「备好的第二双眼睛」而非现役权威。
2. **真实模型**：全部判据用构造的 commit/请求文本，没有经真实 llama／推理产出的叙事内容入账；绑定与逐字闸对真实模型畸形产出尚未验证。
3. **UI 可见行为**：零验证——本波不落任何界面变化。
4. **真实数据目录未演练**：所有跑库都在临时目录（`pry-narrative-sqlite-{guid}`）；`%LOCALAPPDATA%\PryCompanion` 现网库从未被本刀触碰或演练迁移（票面禁写）。
5. **备份包对新表覆盖＝HDS-001 inclusion-policy 缺口**：七张新表是否被既有备份/恢复包含，取决于 HDS-001 的表清单策略，本波未证也不得改；若策略按旧表白名单走，新表将静默漏出备份——登记为已知缺口。
6. **反证维度缺口**：A3（迁移器回滚住在禁改的既有件）、D1（提交持久性归 SQLite 本体）、C5（读面＋写面冗余双闸，R10 实测单枚穿不过）三枚「改坏必响」够不着，只以绿在册（机理见 §四）。
7. **`narrative_scene_projections` 未建**：契约文档提及的场景投影表未入本波七表范围，属后续波次。

## 七 归位提示（给守门人）

- 载体：`.local-notes/qa-wave17-1023/carrier/`，detached 基点 `def0faf5456a5ddcb121f52040ee9e551b07a382`，唯一提交 **`94accd89cb87f41ef30f91df377ec387fc419b8d`**（载体内提交＝归位通道；主树 `git status` 未因本刀添任何 src/tests 脏项）。
- `git show --stat 94accd89cb87f41ef30f91df377ec387fc419b8d` 原样：

```text
 .../Narrative/Storage/NarrativeSqliteStore.cs      | 1190 ++++++++++++++++++++
 .../Narrative/Storage/NarrativeStorageSchema.cs    |  148 +++
 .../NarrativeSqliteCommitDeliveryTests.cs          |  704 ++++++++++++
 3 files changed, 2042 insertions(+)
```

- `git diff --stat def0faf..HEAD -- src tests` 原样：与上同（仅三新文件，2042 插入、0 删除，未触任何既有文件）。
- 新文件行数与 SHA-256（盘上与 HEAD 字节一致，载体 `git status --short`＝0）：

| 文件 | 行数 | SHA-256 |
|---|---|---|
| `src/Pry.Core/Narrative/Storage/NarrativeStorageSchema.cs` | 148 | `4a3bb7e9272176357f130e8f9b8cd61e65a2898d4e11ae6c391204dd823f9f7c` |
| `src/Pry.Core/Narrative/Storage/NarrativeSqliteStore.cs` | 1190 | `6d1618f7c73501924a99591fb68b2810ae07307098b28ae978d98c37fcf2c099` |
| `tests/Pry.Core.Tests/NarrativeSqliteCommitDeliveryTests.cs` | 704 | `7eb7cd76fd75b6e5c524c2f93a8beeaebe10d08a833cbfaf183e1f1134222148` |

- 判据重放锚：TRX 全在 `.local-notes/qa-wave17-1023/trx/`（round00-clean、round01-A1 … round16-D3、final-full）；每轮还原后载体零脏态，任何一轮可凭上表 SHA 复核字节基线。

## 八 守门人归位与换手复采（2026-10-23 17:4x，原样读数）

- **归位通道**：与第十六波同一把合流——隔离载体 `.local-notes/qa-wave1617-merge-1023/carrier`（detached 自 `333b7d6`）上 `cherry-pick 94accd8` 得到映射提交 `91b2e90`，本刀三份文件的字节与 §七 自报值逐位相同（守门人当场 `sha256sum`）：schema `4a3bb7e9…f9f7c`、store `6d1618f7…c099`、判据 `7eb7cd76…2148`。主树以显式路径提交＝`e8a842a`（`git show --stat`＝5 枚文件＝本刀 3 枚＋第十六波 2 枚；他人两枚 `M ` 暂存项一字未动）。合流树与归位后主树在 `src`／`tests`／`scripts`／`Pry.slnx`／`Directory.Build.props` 五面 `git diff --stat` 为空。
- **换手全量（守门人跑，干净 detached 合流副本）**：构建 0 警告 0 错误；解决方案级一次全量＝**总 1275／passed 1273／0 红／2 条跳过**，四份 TRX 各读 `<Counters>`＝Client `108/108/108/0`、App `233/233/233/0`、Api `280/280/280/0`、Core `total=654 executed=652 passed=652 failed=0`；两条跳过按 `outcome="NotExecuted"` 点名（`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`）。
- **§五 那一格坑由本轮补上**：本刀终局全量把四个工程写进同名 `final-full.trx` 互相覆盖，目录里只剩 Core 一份，另三个分项只有控制台原样。守门人这一轮用缺省 TRX 名（每工程一份、带时间戳，见 `run1-full/` 四份文件）拿到四份独立计数，逐工程＝上列四项，与本刀 §五 自报的控制台读数逐位相同 ⇒ **绿侧分项自此有盘上凭证**；但那只补"全绿"这一维，§四 的 15 枚单枚清洁红仍只出自本 lane 的 round TRX（每份 18 格、红名逐格点名，守门人按 §四 表逐轮对过目录名与红格计数，未重跑）。
- **对账**：`def0faf` 合流复采 1244 ＋ 第十六波 13 ＋ 本波 18 ＝ **1275**；Core 623 → 641（本波单采）→ 654（合流，含十六波 13 格）三者相容。`measure-fixed-eval.ps1 -ResultsDirectory <本轮目录>` `exit=0`、catalog **76**、`Passed=1273; Failed=0; Other=2.`
- **预算与预检**：两波合并共用 `dotnet` 2 次（构建 1＋全量 1）；每次起前跑 `game-preflight6-1022.ps1`，三次全 `VERDICT=BUILD_OK`，无 SQLite disk-I/O／OOM 形态、无重采轮。
- **§四 小结的三枚"够不着"照原样挂账**：A3（回滚机理住在禁改的既有迁移器）、D1（提交持久性归 SQLite 本体）、C5（读面＋写面冗余双闸，R10 实测单枚穿不过）——本轮不重跑、不改判据、不为凑红放宽任何断言；R03/R05/R06/R07/R08/R09/R12 的夹具 `IOException` 噪声按本 lane 的定性接受（突变伴生、判定核完好），不当作资源形态作废。
- **§六 七项照原样为未验证**，其中第 5 项（备份包是否涵盖七张新表＝HDS-001 的 inclusion-policy 缺口）继续作为在册缺口，不随本刀结案；第 1 项"新表零生产调用方"与第十九波处置表的"新链未接收发"同一事实，是删除片不能起跑的同一个原因。
- **不做的三件**：未 `tag`、未 `push`、未执行过一次打包；许可与素材三文件一字未碰；数字门槛零改动（本轮 diff 面只有三份新文件）。状态词按 §4.2 口径只写"落库面已实现待验／归位"，结案词归作者与 QA。
