# 叙事与存储契约未决项对账备忘（U-1～U-12 × 已落地代码）

> 用途：给 HDS-004（叙事生命周期）与 HDS-005（原子提交与投递）的下一个动手的人一份"哪些格子已经被代码回答、哪些格子还空着"的决策备忘。
> 输入＝`docs/narrative-storage-contract.md` §12 的十二条未决项，对账对象＝主线上的 HDS-001（`8ef7d01`）与 HDS-002（`5145bde`＋`e9a8055`）落地代码，以及它们各自引用的现存生产代码。
> 本刀的取数方式＝只读代码与已入库文档，零构建、零测试进程；所有判断都能用「文件:行」复核（行号见 §15）。
> 本文不自封状态词，不挪动 §4.2 的任务指针，也不修改被对账的契约文档。

对账基线（当场取时点）：`git status --short --branch` 干净起点＝本 worktree 分支；被引用文件的 main 分支版本与工作树内容逐一致（本刀用 `md5` 对照了 14 个文件，见 §15 末）。

---

## 0. 一页读数

| # | 未决内容（缩述） | 判定 | 归属 |
| --- | --- | --- | --- |
| U-1 | `AliasUnknown`(109) 由谁抛 | 已被既有代码部分回答 | HDS-005 |
| U-2 | 目录锁真实形态＋跨重世代次落点 | 仍需实现波决定 | HDS-001 后续波 |
| U-3 | 目录代次与实例写权代次的联动 | 仍需实现波决定 | HDS-001＋HDS-005 |
| U-4 | 多段投递的原文切分算法 | 仍需实现波决定 | HDS-003＋HDS-005 |
| U-5 | 旧 reply plan 的清点与关单 | 已被既有代码部分回答 | HDS-005 |
| U-6 | 迁移前后可测证据 | 已被既有代码部分回答 | HDS-001＋HDS-005 |
| U-7 | `CommitId`／`ContentHash` 生成规范 | 仍需实现波决定 | HDS-005 |
| U-8 | 模型侧 JSON schema 与流式解析 | 仍需实现波决定 | HDS-003 |
| U-9 | 契约类型与旧链的接线时机与灰度 | 需作者拍（产品决策） | HDS-004 |
| U-10 | 一卡多实例时网关传参修正 | 已被既有代码部分回答 | HDS-005 |
| U-11 | 给迁移步骤加 `FenceRequirement`／`BackfillEvidence` | 仍需实现波决定 | HDS-001＋HDS-005 |
| U-12 | 目录锁与未知高版拒写门的先后 | 已被既有代码部分回答 | HDS-001 后续波 |

计数口径：已被既有代码部分回答 5 条（U-1/5/6/10/12）、仍需实现波决定 6 条（U-2/3/4/7/8/11）、需作者拍 1 条（U-9）。**没有任何一条是"代码已经全答"**，因为落地面（表、锁、真进程）一条都还没进主线。

---

## 1. U-1　`AliasUnknown`(109) 由谁抛

1. **现状：已被既有代码部分回答**。Core 那一侧已经选定"返回 null"，存储侧的抛点还不存在，所以只能算半答。凭据：`src/Pry.Core/Narrative/CharacterInstanceIdentity.cs:233-235`（`ResolveAlias` 未登记别名返回 null，注释写明"由调用方走迁移而不是猜"）；理由码只在枚举里挂着（`src/Pry.Core/Narrative/NarrativeContractReason.cs:26`），全仓 `git grep AliasUnknown` 只有这一处声明＋两个测试文件里也无引用，没有任何抛出点。
2. **既有代码说了什么**：登记处是纯内存字典，且提供快照重建入口（`CharacterInstanceIdentity.cs:135-157`），也就是说"映射账是内存重建"这条路已经有形状；而"映射账是 SQL 查询"那一侧现在只有**可借用的表**，没有别名账本身——库内已有 `memory_scopes(scope_key PRIMARY KEY, character_id, epoch, forget_before)`（`src/Pry.Core/Memory/SqliteMemoryRepository.cs:67-69`），`scope_key` 的现值仍是无用户维的 `character:<id>`（`src/Pry.Core/Memory/MemoryContracts.cs:8`），稳定 ID 摘要也把 `memory_scopes.scope_key` 当键采集（`src/Pry.Core/Backup/SqliteBackupDiagnostics.cs:56-60`）。
3. **还差什么**：一句话给 HDS-005——别名账落表之后，"查不到"有两种问法必须选一种：Core 内存版继续返回 null 并由存储层把 null 翻成 109，或存储层直接抛码；两者混用会出现"同一个失联有两条通道"。
4. **推荐**：接走方式＝`CharacterInstanceRegistry.ResolveAlias` 保持 null（内存形状不变），存储层在 SQL 查询 miss 时抛/返回 109，并把它映射成 HTTP 语义；归属＝HDS-005。这条不占作者裁决额度（属 Owner 职责范围内的技术判定，`docs/decision-request-protocol.md` §5）。

---

## 2. U-2　目录锁的真实形态与跨重启代次落点

1. **现状：仍需实现波决定**。Core 只给了判定形状，真实锁句柄一行都没有；代次持久化位置也没有。凭据：`src/Pry.Core/Narrative/DataDirectoryWriteGate.cs:52-57`（`_held`、`_fencingEpoch` 都是进程内字段，没有读写盘的代码路径）；全仓 `src/` 搜 `Mutex`／`FileShare` 只命中一处与本话题无关的临时文件写（`src/Pry.Api/Services/MediaAssetStore.cs:33`）；`DataDirectoryWriteGate` 这个名字在 `src/` 内除了自身文件零引用。
2. **既有代码说了什么**：
   - 判定侧已经能测：描述文件类凭据一律 `DescriptorIsNotALock`(601)，观察事实先判、凭据类型后判（`DataDirectoryWriteGate.cs:65-106`，条件写第一问 `CheckConditionalWriteAllowed` 只可能返回 None 或拒绝码，`:112-133`；接管 `FenceAndReacquire` 在别的进程仍持锁时拒绝自封新代次，`:137-150`；无凭据时 fail-closed 返回 603，`:152-168`）。对应测试用的是内存假路径字符串、不建目录（`tests/Pry.Core.Tests/NarrativeStorageContractTests.cs:452`、`:473`、`:491`、`:512`）。
   - 生产路径今天做的事恰恰是契约要禁的：`src/Pry.Api/Services/RuntimeDescriptorService.cs:71-90` 读到遗留描述文件只 `LogWarning`，随后 `:63` 覆盖写；`src/Pry.Api/Program.cs:143` 在应用构建完成后直接跑 `InitializeAsync`，第一个写盘动作是 `PRAGMA journal_mode=WAL`（`src/Pry.Core/Memory/MemoryDatabase.cs:30-34`）。
   - "代次持久化"并非无处可去——库里已经有两把可参照的先例：`conversation_runtime.epoch` 单行原子递增＋条件投递比对（`src/Pry.Core/Memory/MemoryDatabase.Runtime.cs:23`、`:58-66`、`:200-206`），以及 `memory_scopes.epoch` 由触发器随删除/手改递增（`src/Pry.Core/Memory/SqliteMemoryRepository.cs:117-128`、`:265`、`:299`）。缺的是"目录级"那一份。
3. **还差什么**：给下一个动手的人三个最小事实——① 取锁点在 `Program.cs:143` 之前而不是之后（见 U-12）；② 锁句柄形态自选（命名 `Mutex` 或 `FileShare.None` 锁文件都不改变 Core 判定，Core 侧 `DataDirectoryObservation` 已把"别的进程是否持锁"当输入事实，`DataDirectoryWriteGate.cs:14-19`）；③ 代次要落 SQL 单行，落点新表或复用治理表由持久层定，且必须让第二个进程读到它而不是本地自增（`:103-105` 是本地 `Math.Max`，进程重启即归零）。
4. **推荐**：机制选型本身写"接走方式＝HDS-001 后续波在 `Pry.Api` 启动路径实现真实锁＋一个单行代次账，Core 判定形状不动"。但**这一条里有一格是产品可见的**，需要作者拍，按 `docs/decision-request-protocol.md` 四段式：

【待你裁决】第二个 Pry 进程指向同一份数据目录时，用户该看到什么（附注：U-2 的可见行为格，Owner＝Architecture 代派，实现落点建议 HDS-001 后续波）

当前行为：今天第二个进程照样启动，它只写一条告警，然后把上一进程的 `runtime.json` 覆盖掉；两边的程序都认为自己在这份数据上工作。
用户可见后果：现在用户双开时看到的是"后开的那个能用、先开的那个端口和令牌突然失灵"，而且两份账都在往前走——真实风险是同一个角色的对话历史与待发文本被两边各写一半，事后没人能解释哪一份算数。加上独占锁之后，第二个进程会在写任何字节之前停下，用户立刻看到一条"这台机器上已有 Pry 正在用这份数据"的拒绝。
项目影响：不阻塞 HDS-004／HDS-005 的机制实现（它们的条件写判定已经有形状，可先把拒绝接上），但阻塞"能不能宣称数据不会双写"这条验收；改选另一种可见行为以后可逆（拒绝文案与是否允许接管都只是启动路径上的一格），不决定的后果是 §14 架构身份验收里"单一写者"只能停留在内存测试。

A. 第二个进程硬拒绝启动，用户看到可读的拒绝并被告知先去关掉另一个：立刻得到"一份数据只有一个写者"这件事的可信度，代价是双开的手滑会直接被挡（有人可能觉得不方便），以后想改成"可显式接管"仍然可改。
B. 第二个进程拒绝写数据但允许以只读方式打开界面：立刻得到更方便的双开体验，代价是要多定义一套只读语义和 UI 状态，可逆性差（只读面一旦暴露给用户就难收回）。
C. 第二个进程询问用户是否接管（接管即提升代次、把前一个进程的条件写全部作废）：立刻得到崩溃后的自助恢复路径，代价是把 fencing 的触发权交给用户，容易在误点时打断正在进行的投递。

推荐 A。理由落在用户利益与可逆性上：他真正怕的是"她的话被两份账撕开"，A 用一次不便换这条底线，且以后要加 C 不必推翻 A。
请裁决 A/B/C。

---

## 3. U-3　目录级 `FencingEpoch` 与实例级 `OwnershipEpoch` 的联动

1. **现状：仍需实现波决定**。两把代次今天住在不同的世界里，没有任何一行代码同时看过它们。凭据：`src/Pry.Core/Narrative/NarrativeCommitWriter.cs:47-54`（构造只注入 `OutboxOwnershipRegistry` 与 `CharacterInstanceRegistry`，没有写闸门）；`:80-101` 的条件写判定序列里第一关是实例登记、第二关是实例写权，全程不问目录锁；`src/Pry.Core/Narrative/DataDirectoryWriteGate.cs` 在 `Narrative/` 内部也没有被别的类型引用。
2. **既有代码说了什么**：
   - 实例侧代次是纯内存字典，`OutboxOwnershipRegistry.cs:51-53`＋`:231-236`（`_highestEpoch` 只在进程内单调），交接时 `TryHandover` 递增（`:99-124`），释放不让它回退（`:126-150`）。**并且它没有快照重建入口**——同目录的 `CharacterInstanceRegistry` 有（`CharacterInstanceIdentity.cs:135-157`），写权登记处没有，所以"重启后旧票必须失效"这件事目前没有可执行形状。
   - 目录侧代次同样进程内（`DataDirectoryWriteGate.cs:55`、`:103-105`），语义是"丢失后旧凭据全部得到 602"（`:112-133`）。
   - 真实落库的代次只有会话级与记忆作用域级两处（`MemoryDatabase.Runtime.cs:58-66`、`SqliteMemoryRepository.cs:117-128`），两者都以"会话"或"scope_key"为键，都不是实例、也不是目录。
3. **还差什么**：一句给 HDS-005 的最小事实——联动要成立，实例写权必须先能持久化重建（否则新进程无从知道上一代是几），建议的落点是 `OutboxOwnershipRegistry` 补一个与 `CharacterInstanceRegistry` 同形状的快照构造＋单行 epoch 账；"目录换主是否强制所有实例交接"这一格推荐定成**强制**，因为条件写第一关已是所有权（`NarrativeCommitWriter.cs:91-100`），把目录代次当交接触发器不需要新增判定层次。
4. **推荐**：接走方式＝HDS-005 落表时把实例写权登记成 SQL 行，目录代次由 HDS-001 后续波落在同一张治理表，读取顺序为"先读目录代次再重建实例票"；纸面规则不在本备忘定死（契约 §12 对这条的理由依然成立：没有真实重启路径）。归属＝HDS-001＋HDS-005。

---

## 4. U-4　多段投递的原文切分算法

1. **现状：仍需实现波决定**。不变式已经可执行，切分者还没选。凭据：`src/Pry.Core/Narrative/DeliveryLedger.cs:143-155`——账本用 `commit.NarrativeProse.Substring(ResolvedOffset, QuotedText.Length)` 还原绑定切片，要求"已有各段＋本段"仍是该切片前缀，最后一段封口必须正好还原完整长度，任何一字之差得到 407；序号必须连续且跳号即拒（`:110-128`）；段数上限取 `min(action.PlannedSegmentCount, MaxSegmentsPerAction)`（`:115-121`），上限源头是宿主签发的 `MaxOutboundSegments`（默认 4，`src/Pry.Core/Narrative/NarrativeDocuments.cs:186`，经 `NarrativeCommitWriter.cs:196` 注册进账本）。
2. **既有代码说了什么**：今天"分成几条"由两件事决定，且都在宿主侧可见——模型自己的输出格式（`src/Pry.Core/TurnTaking/ModelReplyFormat.cs:120` 按行切正文）与用户设置里的开关"将回复拆成多条气泡"`SplitReplies`（`src/Pry.Core/TurnTaking/ReplyPlanner.cs:86` 取或不取多条；默认 true，`src/Pry.Contracts/CommonContracts.cs:51`；UI 与设置持久化在 `src/Pry.App/Services/ConversationSettingsEditor.cs:39`、`:72`）。也就是说：现网从来没有"宿主按句号重切文本"这件事，文本边界一直来自模型格式＋用户许可。
3. **还差什么**：一句给 HDS-003/005 的最小事实——绑定切片由 `ProseBinding{QuotedText, ResolvedOffset}` 给出（`NarrativeDocuments.cs:243-254`），所以切分只能"沿切片内部既有边界下刀"，否则 407 会立刻判红；`PlannedSegmentCount` 是提案声明、不是宿主算出来的量。
4. **推荐**：接走方式＝HDS-003 让 Narrator 用**多个动作或带换行的单条引用**表达分段意图，HDS-005 只按声明段数逐字切片、并把 `SplitReplies` 当作宿主侧唯一的"要不要合一条"许可（沿用现网语义，不新增改写路径）。不占作者额度：用户可见行为与今天一致。归属＝HDS-003＋HDS-005。

---

## 5. U-5　旧 `reply_plans`／`planned_messages` 的清点与关单

1. **现状：已被既有代码部分回答**。取数通道和关单动作都已存在，缺的只有"按实例折叠"这一格。凭据见下。
2. **既有代码说了什么**：
   - 取数：`src/Pry.Core/Backup/SqliteBackupDiagnostics.cs:219-224` 已经在算 `SELECT COUNT(*) FROM planned_messages WHERE state='pending'`，键名 `planned_messages_pending`，进的是 `DatabaseFingerprint.OrphanCounters`（`:41-42`）。
   - 关单：`src/Pry.Core/Memory/MemoryDatabase.cs:727-735`（`CancelPendingPlanMessagesAsync`：把该 plan 的 pending 全部置 cancelled 并脱开记忆侧的在途账）与 `src/Pry.Core/Memory/MemoryDatabase.Proactive.cs:258-266`（`UPDATE reply_plans SET status='closed'`）；重启清扫走 `src/Pry.Core/Runtime/ConversationRuntime.cs:162` 的 `RecoverStalePlansAsync`，调用点在 `src/Pry.Api/Program.cs:145-147`，注释明写"只清账、绝不重放"。既有消费者：`src/Pry.Core/TurnTaking/ConversationTurnManager.cs:165`。
   - 缺口：契约侧的门槛参数是按**实例**问的（`src/Pry.Core/Narrative/OutboxOwnershipRegistry.cs:158-182`，`legacyPendingPlanCount > 0` ⇒ 305），而 `reply_plans` 只有 `conversation_id`（建表 `src/Pry.Core/Memory/MemoryDatabase.cs:126-130`，HDS-001 补的列也没有实例维：`MemoryDatabase.Runtime.cs:31-37`）。今天要从"会话"落到"实例"，可借的归属边是 `conversations.character_id`（`MemoryDatabase.cs:70-76`）与 `memory_scope_conversations`（`SqliteMemoryRepository.cs:70-72`）。
3. **还差什么**：一句给 HDS-005 的最小事实——per-instance 计数只能靠 JOIN 出来（`reply_plans`→`conversations`→角色），或者把声明粒度降到会话级；两条都行，但必须选一条写进表设计，不能让 `legacyPendingPlanCount` 变成"调用方随手填 0"的整数（它现在是纯参数，无来源校验）。
4. **推荐**：接走方式＝HDS-005 落表时新增一条按实例折叠的 pending 计数查询，关单复用上面两处已有 SQL，切换时"未发计划直接作废"沿用重启清扫的既有语义（不重放）。归属＝HDS-005。

---

## 6. U-6　迁移前后可测证据

1. **现状：已被既有代码部分回答**。可测通道是现成的，缺的是"每一步各测一次"。
2. **既有代码说了什么**：
   - 通道：`SqliteBackupDiagnostics.cs:106-240` 的 `CaptureFingerprintAsync` 产出逐表行数（`:124-133`）、稳定 ID 集合摘要（`:135-145`）、墓碑计数与摘要（`:156-169`）、作用域水位摘要（`:171-180`）、索引集合与外键违例（`:182-193`）、悬挂引用计数（`:195-225`）；恢复侧已经逐格对照（`src/Pry.Core/Backup/DataRestoreService.cs:125-128` 行数、`:129-131` 稳定 ID、`:137-142` 墓碑与水位）。
   - 已有用法：`tests/Pry.Core.Tests/MemorySchemaMigrationRegistryTests.cs:81-99` 用整本指纹做幂等判据（注释原话是"跑过不多东西"），并断言登记版本推进到 `MemoryDatabase.SchemaVersion`。
   - 缺格：迁移步骤本身没有前后计数——`src/Pry.Core/Memory/MemorySchemaMigrator.cs:45-54` 的 `SchemaMigrationStep` 只有 `TargetVersion/Id/Name/Mode/Apply` 五个分量，执行循环 `:66-102` 不采集任何计数，初始化只在结束时记一条"版本前后"的日志（`src/Pry.Core/Memory/MemoryDatabase.cs:38-40`）。现成就有一个"回填但没数"的样本：`SqliteMemoryRepository.cs:101-110` 给历史行补 `scope_key`/`fingerprint`，前后各有多少行无人问过。
3. **还差什么**：一句给 HDS-001/005 的最小事实——`CaptureFingerprintAsync` 走的是**独立只读连接**且 `Pooling=false`（`SqliteBackupDiagnostics.cs:262-279`），所以不能在迁移事务内直接复用来测同一步的前后差值；要测得要么在步骤内用同一连接跑 COUNT，要么把差值留给步骤自己的日志。
4. **推荐**：接走方式＝与 U-11 同一把刀做完：`BackfillEvidence` 作为可选委托挂在步骤上，由 `RunAsync` 在步骤前后各取一次差值并写日志；不新建第二套采集器。归属＝HDS-001＋HDS-005。

---

## 7. U-7　`CommitId`／`ContentHash` 的生成规范

1. **现状：仍需实现波决定**。Core 只要求"存在且稳定"，算法与形状一律没定；并且有一格实测事实要先说清。
2. **既有代码说了什么**：
   - 判定面：内容散列只判空白（`NarrativeCommitWriter.cs:109-113` ⇒ 213；`src/Pry.Core/Narrative/NarrativeDraftValidator.cs:395-399` 同），同 ID 重放靠**字符串等值**比对散列（`NarrativeCommitWriter.cs:123-131`，`StringComparison.Ordinal`）。`CommitId` 走 `NarrativeGuard.RequireText`（`NarrativeDraftValidator.cs:387`），因此实际有 128 字符上限且非空（`src/Pry.Core/Narrative/NarrativeDocuments.cs:423-436`，`NarrativeContractReason.cs:110`），但**空白 `CommitId` 报出来的理由码是 213 ContentHashMissing**——判的是 ID、报的是散列，宿主映射文案时会踩。`ContentHash` 本身没有长度与字符集校验。
   - 仓内现成先例：SHA-256 小写 hex 全长（`SqliteBackupDiagnostics.cs:290-291`、`src/Pry.Core/Backup/BackupBundleManifest.cs:67`、`src/Pry.Core/Memory/SqliteMemoryRepository.cs:45-46`）与截断形状（`src/Pry.Core/Expression/ExpressionLearner.cs:311` 取 12、`src/Pry.Api/Services/LocalModelDiscoveryService.cs:77` 取 16）；ID 形状方面 `CharacterInstanceId` 允许 GUID/ULID/uuid7 的小写形态（`NarrativeContractReason.cs:129-142`）。输入面另有 `ContractValidation.MessageContentMaxLength`＝20 000（`src/Pry.Contracts/ApiContracts.cs:194-216`，HDS-011b）。
3. **还差什么**：一句给 HDS-005 的最小事实——散列必须是**对稳定序列化结果**求值（同提交重放要字节一致），所以先定 canonical JSON 的字段次序与时间戳格式（`DateTimeOffset.ToString("O")` 是仓内既有做法，`BackupBundleManifest.cs:52`），再定截断长度；顺带把 213 与 `CommitId` 空白的错配、以及 `ContentHash` 无长度上限这两格补掉。
4. **推荐**：接走方式＝HDS-005 定 `ContentHash`＝SHA-256 全长小写 hex、`CommitId`＝uuid7/ULID 字符串，同时在 Core 侧补两个校验点（一行级小刀，可与 005 同把）。不占作者额度。

---

## 8. U-8　`NarrativeDraft` 的模型侧 JSON schema 与流式解析

1. **现状：仍需实现波决定**（且归属本就不在本波）。
2. **既有代码说了什么**：`NarrativeDraft` 的字段面齐了（`src/Pry.Core/Narrative/NarrativeDocuments.cs:322-347`：提案只有 `ProposalId`、不带 `CommitId`/`Binding`/`ContentHash` 的宿主责任项），但没有任何生产代码构造或解析它——`git grep "Pry.Core.Narrative" src/` 只命中该目录自身，`tests/` 命中两个新测试文件（`tests/Pry.Core.Tests/NarrativeIdentityContractTests.cs:1`、`NarrativeStorageContractTests.cs:1`）。现存的"模型输出形状"知识集中在一处：`ModelReplyFormat`（形状判定 `src/Pry.Core/TurnTaking/ModelReplyFormat.cs:5-24`，信封标签按字符码拼装以免下游工具误判）＋ `ReplyPlanner.Parse`（`:79-95`，解析不出可见内容时回落占位而不是整段原始输出）。JSON→DTO 的先例另有 `src/Pry.Core/Expression/ExpressionProtocolParser.cs:20`。设计侧硬约束是 v4 §8 那句"第一版采用完整结构响应通过校验后再投递"（`docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md:232`）。
3. **还差什么**：一句给 HDS-003 的最小事实——Narrator 的 schema 必须能把 `Actions[].ProseQuote` 表达成**字面引用**（宿主算偏移，`NarrativeDraftValidator.cs:207`／`NarrativeProseBinder.Bind` `:38-88`），不要让模型报 UTF-16 偏移，否则 205/206 会把小模型全判红。
4. **推荐**：接走方式＝HDS-003 新建 narrator 解析层，复用 `ModelReplyFormat` 的"形状知识只写一处"判据，第一版不做流式提前提交（v4 已定）。本刀不派生作者额度；但**HDS-003 的真实模型对照要占作者的机器与 API 额度，需他点头**（`docs/plans/PROJECT_PLAN.md:74`、`:191`、`:217` 已在册）。

---

## 9. U-9　契约类型与 `IReplyAdvisor`／`ReplyPlanner` 的接线时机与灰度形状

1. **现状：需作者拍（产品决策）**。接线点在代码里已经存在（这一格其实被回答了），**没答的是"什么时候让她走新链"**——这是产品可见的取舍，不是技术选型。
2. **既有代码说了什么**：可接缝的形状已就位——`IReplyAdvisor` 定义在 `src/Pry.Core/Runtime/RuntimePorts.cs:80`，组合根把它注入 `src/Pry.Core/Runtime/ConversationRuntime.cs:9`，宿主实现在 `src/Pry.Api/Runtime/CharacterIntelligenceGateway.cs:65`（`CreateAdvisor`）与 `:315`，旧生成链由 `src/Pry.Api/Runtime/RuntimeAdapters.cs:72` 的 `ReplyPlannerComposer` 适配、在 `src/Pry.Api/Services/ConversationSessionService.cs:279-281` 装配。"宿主持久化一个开关再切"的现成先例是 `TurnTakingSettings.SplitReplies` 走设置落盘（`src/Pry.Contracts/CommonContracts.cs:51`＋`src/Pry.App/PryMessengerSettingsWindow.axaml.cs:81`、`:236`）。设计侧规则在 v4 §15（`docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md:397`）：`legacy`／`narrative-v1` 单引擎独占、选择由宿主持久化、切换前排空与提升 epoch。契约 §12 对本条的立场是"本波不接线"，且 §13 已核对未接线（`docs/narrative-storage-contract.md:371-372`）。
3. **还差什么**：一句给 HDS-004 的最小事实——灰度需要一个新的持久位（每实例的 engineMode＋叙事契约版本），今天没有任何地方存它（全仓 `git grep EngineMode` 只命中 `src/Pry.Core/Narrative/`）；而"按实例切"还依赖 U-10 的映射账。
4. **推荐**：按 `docs/decision-request-protocol.md` 四段式提请作者：

【待你裁决】她的回复什么时候从旧链路换到新叙事链路，以及换的时候要不要她做什么（附注：U-9，归属＝HDS-004，接线点见上面 file:line）

当前行为：她每一轮的回复都由旧链路产出——`ReplyPlanner` 生成、落 `reply_plans`/`planned_messages`，宿主按计划发送；新契约的叙事类型今天在生产代码里一次都没被调用。
用户可见后果：维持现状她感觉不到任何变化，但架构切换的收益（沉默是合法产物、她说过的话能对上"回应了哪一句"、崩溃后不重复发旧消息）也一天都不会落到她身上；一旦切换，她会先看到行为差异：某些轮她说的话可能不被回（合法的沉默），一条回复可能被分成几条。
项目影响：新链的落表（HDS-005）与生命周期（HDS-004）都可以先建好再切，**不被本条阻塞**；被阻塞的是"什么时候有真实用户可见的差异"。灰度粒度一旦选了按实例，就必须先把实例映射账落表（U-10），否则只能按全局切。可逆性：切换本身可逆（保留旧链一个版本周期即可回退），但"新链已经写进库的叙事与投递账"不可逆——回退后那些行没人读。

A. 先建不改：HDS-004/005 落表与接线代码全部默认 `legacy`，她自己没有任何变化，等 HDS-003 的真实模型对照出数后再决定给谁开：立刻得到零风险，代价是新架构的收益暂时看不见，可逆。
B. 按实例灰度，她可以在设置里为某个角色打开：立刻得到可控的真实反馈，代价是要多做一格 UI 与每实例持久位，且必须先有实例映射账（U-10），可逆但工序更多。
C. 全局直接切到新链：立刻得到统一的行为与新数据，代价是一旦小模型叙事质量不达标就是全线可见回退事件，且库里已写的新账无法回收，可逆性最差。

推荐 A。她此刻得不到任何好处，而 A 保住唯一不可逆的那一格（数据）；等 HDS-003 拿真实模型对照说话，再按同一格式提 B。
请裁决 A/B/C。

---

## 10. U-10　一卡多实例时 `CharacterIntelligenceGateway` 的传参修正

1. **现状：已被既有代码部分回答**。用户维已经在代码里露头，但传下去的仍然是角色卡 ID。
2. **既有代码说了什么**：`src/Pry.Api/Runtime/CharacterIntelligenceGateway.cs:36-38` 有 `public const string LocalUserId = "local"`（注释明写"多用户落地时改这一处"），`:91` 把它传给 `CharacterStateService`；但 `:336` 构造 `SituationSnapshot` 时填的是 `CharacterInstanceId = character.Id`，`:27-31` 的作用域前提注释自己登记了后果：一卡多实例时这里与 `ConversationRuntimeOptions.CharacterInstanceId` 必须一起改，否则删卡的 `PurgeAsync` 会静默漏掉后建实例。记忆侧同样无用户维：`MemoryScope.ForCharacter` 拼 `character:<id>`（`src/Pry.Core/Memory/MemoryContracts.cs:8`，同一行注释已写"instance scope must never be derived from a character id alone"）。
3. **还差什么**：一句给 HDS-005 的最小事实——改传参之前必须先把实例登记行落库并让 `Resolve` 幂等（`CharacterInstanceIdentity.cs:168-198`），否则每次启动签一个新 ID 就正好踩契约禁的那条反例（104，`:173-177`）。
4. **推荐**：接走方式＝HDS-005 落 `character_instance`/`character_instance_alias` 后，把 `:336` 与 `ConversationRuntimeOptions.CharacterInstanceId` 两处一起换成解析出的实例 ID，并把 `PurgeAsync` 改走 alias 解析；本波不动他人运行时文件。不占作者额度。

---

## 11. U-11　往 `SchemaMigrationStep` 追加 `FenceRequirement`／`BackfillEvidence`

1. **现状：仍需实现波决定**（形状要求已由契约写清，代码里两个字段都不存在）。
2. **既有代码说了什么**：`MemorySchemaMigrator.cs:45-54` 的五参数记录定义里没有这两项，`UsesAmbientTransaction`（`:53`）只区分"迁移器是否包事务"；清单不变量校验 `:105-126` 查空表、重号、目标版本非法、冒领未来版本、必须且只能以 `VersionAlign` 收尾，**不查持锁**；执行循环 `:72-101` 每步一个事务边界、失败整步回滚并以 `MemoryMigrationException` 停下。另有一格必须提醒动手的人：`IdempotentRepairOwnsConnection` 档的步骤自带连接（`:20-22` 注释写明外层包了会自锁），现网正是这么用的（`MemoryDatabase.cs:173-175` 的 `repair.long-term-memory` 调 `LongTerm.InitializeAsync`，内部自开连接与事务，`SqliteMemoryRepository.cs:50-52`）——给这类步骤加"必须在外层事务里持锁"的判定会自相矛盾。
3. **还差什么**：一句给 HDS-001/005 的最小事实——`FenceRequirement` 的硬前置是 U-2：今天没有任何地方取目录锁（见上面 §2），如果直接把"结构变更必须持锁"写进 `ValidateRegistry`，`Program.cs:143` 这条真实初始化路径会当场全红；因此该字段必须先以默认 false 引入、由新步骤显式置 true。
4. **推荐**：接走方式＝与 U-6 同一把刀：`FenceRequirement`（默认 false，保持既有步骤行为）＋`BackfillEvidence`（可选委托）一起加，并在 HDS-001 后续波取到真实锁之后把结构变更类步骤翻成 true。归属＝HDS-001＋HDS-005；文件归属在他人名下，动前先按 `docs/plans/README.md` 第 3 条排撞位。

---

## 12. U-12　目录写锁与 `SchemaVersionGuard` 的先后

1. **现状：已被既有代码部分回答**——**顺序其实已经被定死了**，只剩落点没实现，这一格不该再当"未决"消费。
2. **既有代码说了什么**：现行初始化顺序是"开连接 → 读版本 → 拒写门 → WAL → 迁移清单"（`MemoryDatabase.cs:26-37`），门在任何写之前（`:30-31` 注释逐字写明第一批语句里的 `PRAGMA journal_mode=WAL` 本身就是写；判定体 `:195-199`；测试专门守这一格：`tests/Pry.Core.Tests/SchemaVersionGuardTests.cs:83`「拒写门坐落在那个写之前」）。**没有任何一步取锁**——`Program.cs:143` 直接跑初始化，`RuntimeDescriptorService` 要等到 `ApplicationStarted` 才写描述文件（`RuntimeDescriptorService.cs:34-41`＋`Program.cs:118`），所以真实顺序是"先迁移完、后发布发现入口"。方向侧权威文本已经给答：v4 §8 明写"数据目录在任何初始化/迁移前必须取得可检测的进程级独占写入锁；`runtime.json` 只负责发现"（`docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md:220`）。
3. **还差什么**：一句给 HDS-001 后续波的最小事实——取锁点只能坐在 `MemoryDatabase.cs:26` 开连接之前（更准确地说坐在 `Program.cs:143` 之前，否则 Core 会依赖进程类型，破 AGENTS 第 5 条的"Core 不依赖 HTTP/进程"边界）；`SchemaVersionGuard` 的判定位置不动，它已经在锁该在的位置之后紧邻的第一步。
4. **推荐**：接走方式＝HDS-001 后续波在宿主侧加"取锁→再初始化"的启动序，Core 判定与门位置都不改；并把契约 §12 这条从"未决"降级为"待实现"（**这条更正要由守门人或本 lane 的下一把在契约文档里做，本刀不碰他人名下文档**）。

---

## 13. HDS-004／HDS-005 现在就能开工的前置清单

**已不构成阻塞**（可以直接按既有形状写代码）：

- U-1：内存语义已定（返回 null），HDS-005 只需在 SQL miss 处抛 109。
- U-4：投递不变式已可执行（逐字前缀＋封口还原，`DeliveryLedger.cs:143-155`），切分策略不影响 HDS-005 的账本写入判定。
- U-5：取数与关单都有现成 SQL 可复用（`SqliteBackupDiagnostics.cs:219-224`、`MemoryDatabase.cs:727-735`、`MemoryDatabase.Proactive.cs:258-266`）。
- U-6：采集通道现成（`SqliteBackupDiagnostics.cs:106-240`），只缺"每步前后各一次"。
- U-7：算法选型是实现侧自决，Core 判定面已就位（空白即拒、散列等值即幂等）。
- U-8：属 HDS-003；HDS-004/005 不依赖它的解析层，只依赖 `NarrativeDraft` 字段面（已齐）。
- U-10：改传参本身清楚，唯一依赖是映射账落表（HDS-005 自己的活）。
- U-12：顺序已由 v4 §8 定死，实现照做即可。

**HDS-005 的真前置**（动手前要先解决）：

1. U-2/U-3：目录锁与代次持久化落点——它的"崩溃恢复／接管后旧票必须失效"两组判据没有真锁就没有可信测试，目前只能停在内存形状。
2. U-11：叙事侧新步骤要登记进 `SchemaMigrationStep` 清单，且必须避开 `ValidateRegistry` 的"不得冒领未来版本"（`MemorySchemaMigrator.cs:117-120`）——版本号分配权在持久层，本备忘不自封任何 `TargetVersion`。
3. U-5 的实例维：`reply_plans` 无实例列，per-instance pending 计数要么 JOIN 要么降粒度。

**HDS-004 的真前置**：

1. U-9：灰度与持久位（需作者拍，且新持久位依赖 HDS-005 的实例映射账）。
2. HDS-003 的语义验证结论（`docs/plans/PROJECT_PLAN.md:75` 把 004 的语义来源记为 002＋003，003 仍无人认领）。
3. U-3 的联动规则（调度侧的"所有权丢失即停"要问的就是这两把代次）。

---

## 14. 本刀读到的、契约文档与已落地代码不一致之处

1. **同一份库里存在两套结构版本账**。契约 §11.2 第 1 点写"版本来源仍是 `schema_info` 这一张登记账……一个库只能有一个『当前结构版本』的问法"，并称 HDS-001 已按此实现；但 `src/Pry.Core/Memory/SqliteMemoryRepository.cs:65-66` 另建了 `memory_schema(version)` 表并插 1，`:99` 读它、`:134` 把它从 1 推到 2，`tests/Pry.Core.Tests/MemoryDeletionSeparationTests.cs:276-286` 直接断言并改写它的值。契约 §11.1 的"现在"一节没有提这第二张表。
2. **两类 fail-closed 并不一致**（契约 §11.2 第 8 点要求一致）：`schema_info` 侧的高版未知走 `SchemaVersionTooNewException`，实现 `IUserPresentableError` 且 `RetryAllowed=false`（`src/Pry.Core/Memory/SchemaVersionGuard.cs:15-43`，判定体在 `MemoryDatabase.cs:195-199`）；`memory_schema` 侧的未知版本只抛裸 `InvalidOperationException`（`SqliteMemoryRepository.cs:100`），用户面拿不到病因。
3. **未知高版拒写门没覆盖第二张账**：门只比 `schema_info` 读数与 `MemoryDatabase.SchemaVersion`（`MemoryDatabase.cs:27-30`），`memory_schema.version` 高于当前二进制时不会被它拦下。
4. **§12 把 U-12 当未决，v4 §8 已经定了它**（`docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md:220`）；而代码里真实顺序还多了一格文档没写：描述文件在 `ApplicationStarted` 才写，晚于整条迁移路径（`RuntimeDescriptorService.cs:34-41`＋`Program.cs:143`）。
5. **文件名与内容对不上**：契约 §11.1 说"另有 `src/Pry.Core/Memory/SchemaVersionGuard.cs`"承担拒写门，实际该文件只有两个异常类型（`:15`、`:50`），门的判定在 `MemoryDatabase.cs:195-199`。动手的人若照文档去改"闸门"会改错文件。
6. **行号漂移（无害但会误导）**：契约 §10.1 记 `ReportLeftover()` 为 71–89 行，实际 71–90（`RuntimeDescriptorService.cs:71-90`）；§2.1 记网关缺口注释为 26–31 行，实际 27–31。
7. **一条代码内部错配（顺带登记）**：空白 `CommitId` 报出的理由码是 213 `ContentHashMissing`（`NarrativeDraftValidator.cs:387`），与契约 §9.2 判定表里 213 对应"没有内容散列"的语义不符；HDS-005 若要把它映射成 HTTP 语义会先撞上这个错配。

契约 §13 的三条红线核对为**属实**：`src/Pry.Core/Narrative/` 在 `src/` 内零生产引用、未新增 NuGet 包、`git grep -I user_version -- '*.cs'` 无命中（命中的只有 `src/**/bin/` 下的原生 sqlite 构建产物，不受跟踪）。

---

## 15. 证据自检（本刀实际打开过的文件与行号范围）

以下每条都能用「文件:行」独立复核；行号取自我在主工作树只读打开的内容，且已用 `md5` 逐一确认这些文件的工作树版本与 main 分支提交版本相同（14 个文件全部 SAME）。

- `docs/narrative-storage-contract.md` 全文（1-375 行，重点 §11.2 328-341、§12 350-366、§13 369-374）
- `docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md` 184-256（§7、§8、§9）、395-425（§15、§16）
- `docs/plans/PROJECT_PLAN.md` 7、70-94、190-191、206-217、222-234、237-252、262-275
- `docs/decision-request-protocol.md` 1-71（四段式与"不走本协议的情形"）
- `AGENTS.md`（仓库根，工作规则 1、2、5、12 条）、`docs/plans/README.md` 10、32、43、48
- `src/Pry.Core/Narrative/DataDirectoryWriteGate.cs` 1-169（全文）
- `src/Pry.Core/Narrative/CharacterInstanceIdentity.cs` 1-487（全文，重点 134-157、168-198、204-235、274-384、416-473）
- `src/Pry.Core/Narrative/OutboxOwnershipRegistry.cs` 1-237（全文，重点 51-63、69-124、126-150、158-198、204-236）
- `src/Pry.Core/Narrative/NarrativeCommitWriter.cs` 1-199（全文，重点 41-57、80-131、133-197）
- `src/Pry.Core/Narrative/DeliveryLedger.cs` 1-262（全文，重点 51-63、66-167、171-223、225-256）
- `src/Pry.Core/Narrative/NarrativeDocuments.cs` 1-437（全文，重点 5-21、46-90、162-237、243-281、322-385、391-421、423-436）
- `src/Pry.Core/Narrative/NarrativeDraftValidator.cs` 1-420（全文，重点 38-99、107-183、186-257、263-296、303-375、383-419）
- `src/Pry.Core/Narrative/NarrativeContractReason.cs` 1-143（全文，重点 26、89-95、108-142）
- `src/Pry.Core/Memory/MemorySchemaMigrator.cs` 1-127（全文）
- `src/Pry.Core/Memory/SchemaVersionGuard.cs` 1-71（全文）
- `src/Pry.Core/Memory/MemoryDatabase.cs` 1-140、160-215、960-1000、725-735（含 22-41、48-58、126-142、173-188、195-199、208、980-986、1014-1020）
- `src/Pry.Core/Memory/MemoryDatabase.Runtime.cs` 10、23、31-42、47-52、58-66、78-107、115-137、146-155、167、178-206
- `src/Pry.Core/Memory/MemoryDatabase.Proactive.cs` 48、100、212-223、258-266
- `src/Pry.Core/Memory/MemoryContracts.cs` 1-20（重点 8）
- `src/Pry.Core/Memory/SqliteMemoryRepository.cs` 45-52、60-90、92-140、147-149、170、265、299、413、500、533、645
- `src/Pry.Core/Backup/SqliteBackupDiagnostics.cs` 1-292（全文，重点 13-43、56-60、63-79、82-103、106-240、243-252、262-291）
- `src/Pry.Core/Backup/DataRestoreService.cs` 1-164（全文）
- `src/Pry.Core/Backup/DataBackupService.cs` 1-60
- `src/Pry.Core/Backup/BackupBundleManifest.cs` 50-67
- `src/Pry.Api/Services/RuntimeDescriptorService.cs` 1-143（全文）
- `src/Pry.Api/Program.cs` 56、118、120-150（重点 143-147）
- `src/Pry.Api/Runtime/CharacterIntelligenceGateway.cs` 20-45、65、91、315、325-345（重点 336、357）
- `src/Pry.Api/Runtime/RuntimeAdapters.cs` 36、45-47、69-109
- `src/Pry.Api/Services/ConversationSessionService.cs` 279-281
- `src/Pry.Api/Services/MediaAssetStore.cs` 33、180
- `src/Pry.Api/Services/LocalModelDiscoveryService.cs` 77
- `src/Pry.Core/TurnTaking/ReplyPlanner.cs` 8-16、75-95；`ModelReplyFormat.cs` 1-40、120；`ConversationTurnManager.cs` 165
- `src/Pry.Core/Runtime/RuntimePorts.cs` 30-33、66、80、188、207；`ConversationRuntime.cs` 9、162；`RuntimeModels.cs` 189-194；`ReplyGate.cs` 12-15；`RuntimeDefaults.cs` 13、34
- `src/Pry.Core/Configuration/JsonConfiguration.cs` 17；`src/Pry.Core/Expression/ExpressionProtocolParser.cs` 20；`ExpressionLearner.cs` 311；`StickerCatalog.cs` 78；`src/Pry.Core/Prompting/ContextBuilder.cs` 19
- `src/Pry.Contracts/ApiContracts.cs` 194-216；`UserPresentableErrorContract.cs`（HDS-001 带入，经 `SchemaVersionGuard.cs:1` 引用核对）；`ConversationRuntimeContracts.cs` 42、54；`CommonContracts.cs` 51
- `src/Pry.App/PryMessengerSettingsWindow.axaml.cs` 81、236；`Services/ConversationSettingsEditor.cs` 39、72
- `tests/Pry.Core.Tests/NarrativeStorageContractTests.cs` 1、440-555（写锁四条测试正文）；`NarrativeIdentityContractTests.cs` 1（仅取引用面）
- `tests/Pry.Core.Tests/MemorySchemaMigrationRegistryTests.cs` 54-55、80-101、101-102、150-151、189-190、207-208
- `tests/Pry.Core.Tests/SchemaVersionGuardTests.cs` 65-66、82-83、115-116、133-137、148-149
- `tests/Pry.Core.Tests/MemoryDeletionSeparationTests.cs` 276-286
- 检索类操作（非构建）：`git status --short --branch`、`git log`、`git show --stat 8ef7d01`、`git grep`（`AliasUnknown`、`Pry.Core.Narrative`、`user_version`、`memory_schema`、`fence_epoch`、`EngineMode`、`CancelPendingPlanMessagesAsync`、`RecoverStalePlansAsync`）、`grep`（SHA256、reply_plans、epoch、InitializeAsync 调用点）、`md5sum` 对照

本刀零 `dotnet` 调用：没有 build、没有 test、没有 run，一次都没有。
