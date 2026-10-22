# eval-QA-WAVE9-1022 ｜ HDS 第九波：拆法原文欠账逐格补证（载体树 = 线尾 d2dbdee）

报告 id：`eval-QA-WAVE9-1022`。工单：`docs/dispatch-hds-wave9-20261022.md`。执行 lane：冷启动子 Agent。
坐标系：`docs/plans/PROJECT_PLAN.md` §4.2。本波零产品代码提交、零新判据、零门槛改动。
本文件只写事实与读数；状态词（已结案／已验收／基线已改写）与在册基线改写权不归执行方。
载体＝`C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-tip-1022`（detached worktree）。全部改坏只存在于该工作树，收尾逐字节还原。
欠账分母 46 格：已付 24、未付 22（W2 一格不派跑，见 §③.3）；本波动手 21 格＝G1(1–10)＋G2(11–18)＋G3(19–21)。

## 〇 第 0 步挪树输出 ＋ 逐格计划（动任何突变之前先落盘）

### 〇.1 挪树命令与整段输出

命令：
`git -C "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-tip-1022" checkout --detach d2dbdee`

输出（原样）：
```
Previous HEAD position was 6b6f549 docs(plans): 第五波五片归位登记（F1 2eba867／F3 e31e4da／F5 dc5b010，出口 1146＝1144 绿＋2 条同名跳过）＋工单闭合＋本角色那次读错树的计数错账
HEAD is now at d2dbdee docs(qa,wave8): 第八波续跑收口入库——17/18 格清洁实测红＋W3 归属由守门人另起一轮净化＋W2 够不着的根因落账（欠账 43→26 格）
```

`git -C <载体> status --short` 行数：`0`（wc -l 实测＝0）。
`git -C <载体> rev-parse HEAD`：`d2dbdeedc1b8d0944122c115b79d816e81284b6c`（＝工单要求的 d2dbdee）。

开工前主工作树状态（他人名下项，本 lane 不读改不提交）：
```
## codex/ui-main-window-redesign
M  docs/dispatch-tip1020-02-memory-group-judgment.md
M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs
?? .qoder/
?? docs/character-learning-safety.patch
?? docs/research/HDS_INTERLUDE.md
?? docs/research/HDS_QQ_RESEARCH_INDEX.md
?? docs/research/QQ_BRIDGE.md
```
主工作树 HEAD＝`51bf724726eb49a52b134c0078d7588659bbfd87`（本 lane 不动它、不在主树里改文件或跑构建）。
线尾 ancestry 校验：`git merge-base --is-ancestor 4039a50 d2dbdee`＝真（cell 10 需要的那段 `4039a50` 新断言确在载体现行树上）。

### 〇.2 起点读数（引用 §② 四项分项，本波不为其先跑全量）

Core total 527／executed 525／passed 525／failed 0；Api 278；App 233；Client 108 ⇒ 总 1146＝passed 1144＋0 红＋2 条环境跳过。
两条 `outcome="NotExecuted"`（均在 `Pry.Core.Tests.dll`）：`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`。catalog 76；`verify-repository` 主工作树当前 549 条受跟踪文件（每落一份文档顶一格，非门槛）。本波不改任何条数。

### 〇.3 G1（Core，第 1–10 格）逐格计划：改哪一行 → 预期哪一格响

以下为动 G1 突变前定稿。每格附 §④.1 三问（谁调用／一生命周期几回／什么输入走到拒绝支）。

| # | 格 | 被改文件:行 | before → after（拟） | 预期响的格 | 三问要点 |
|---|---|---|---|---|---|
| 1 | `DataBackupServiceTests.Secrets_logs_runtime_and_user_imports_never_enter_or_leave_the_bundle` | `src/Pry.Core/Backup/BackupInclusionPolicy.cs:38`（`AlwaysDeniedExtensions` 里 `.key` 一项） | `{ ... ".key", ".pfx", ...}` → 去掉 `".key",` | 第 1 格 | 由 `IsDeniedFile`→打包器逐文件过；一次 CreateBackup 跑一回/文件；种子包 `media/server.key` 内含 FAKE 串，`.key` 不再被硬排除 ⇒ 该文件入包，`DoesNotContain(.key)` 与 `DoesNotContain("FAKE")` 双双响 |
| 2 | `DataRestoreServiceTests.A_bundle_from_a_future_schema_version_is_refused_before_anything_is_written` | `src/Pry.Core/Backup/DataRestoreService.cs:89` | `manifest.CreatedByBinarySchemaVersion > MemoryDatabase.SchemaVersion` → `... < MemoryDatabase.SchemaVersion` | 第 2 格 | 由 `GuardTargetAndLoadManifestAsync`→`RestoreAsync` 入口；每恢复一回；正常包版本＝SchemaVersion（`<` 下不误拦），+7 未来包 `< SchemaVersion` 假 ⇒ 不再抛 `SchemaVersionTooNewException` ⇒ `Assert.ThrowsAsync` 响 |
| 3 | `SchemaVersionGuardTests.Initialize_refuses_a_database_registered_above_the_binaries_schema_version` | `src/Pry.Core/Memory/MemoryDatabase.cs:197` | `if (databaseSchemaVersion <= SchemaVersion) return;` → `if (databaseSchemaVersion <= SchemaVersion + 1) return;` | 第 3 格 | 由 `InitializeAsync:30`→`EnsureWritableSchemaVersion`；每次初始化一回；种子登记 SchemaVersion+1=11，放宽到 `<=11` ⇒ 11 不再抛 ⇒ 该格 `Assert.ThrowsAsync` 响（+5/+7/42/99 仍在 `<=11` 之外，不误伤其它 schema 格） |
| 4 | `MemorySchemaMigrationRegistryTests.Registry_is_named_unique_ordered_and_never_pins_a_future_version` | `src/Pry.Core/Memory/MemoryDatabase.cs:50`（baseline 步 `Name`） | `"既有结构的自愈建表与列补齐"` → `""` | 第 4 格 | 由 `BuildMigrationSteps`→本测试直接断言（`Assert.All(... IsNullOrWhiteSpace(step.Name))`）；Name 不参与执行，InitializeAsync 各步照常跑；仅这一格读 Name ⇒ 只响它 |
| 5 | `NarrativeIdentityContractTests.Identity_must_be_a_host_issued_stable_token(candidate: "pry")` | `src/Pry.Core/Narrative/NarrativeContractReason.cs:113`（`MinIdentityLength`） | `MinIdentityLength = 8` → `MinIdentityLength = 3` | 第 5、6 格（同一枚突变） | 由 `CharacterInstanceId.Create:30`（`Length < Min || !IsIdentityShape`）；每次 Create 一回；"pry"(3)与"pry-"(4)在 min=3 下：长度过、全小写形状过 ⇒ 不再抛 ⇒ 这两行 `Assert.Throws` 响；"PRY"（大写）、"pry@"（@）、"ci:pry"（:）靠 `IsIdentityShape` 仍拒 ⇒ 那三行不动 |
| 6 | 同上，candidate "pry-" | 同上（同一枚） | 同上 | 与第 5 格同源一枚 | 见上；两行各自判，均响 |
| 7 | `NarrativeStorageContractTests.A_story_version_ahead_of_the_ledger_is_rejected_too` | `src/Pry.Core/Narrative/NarrativeCommitWriter.cs:141` | `commit.ExpectedStoryVersion < state.StoryVersion ? LateCommitRejected : StoryVersionConflict` → `> ... ? LateCommitRejected : StoryVersionConflict` | 第 7、10 格（同一枚，各响自己那条错因消息） | 由 `TryCommit:138`（版本门）；每次条件写一回；cell7 expectedStory=4>账上0 走成 `LateCommitRejected`（应为 StoryVersionConflict）⇒ 响；cell10 见 §〇.5 |
| 8 | `NarrativeStorageContractTests.Segment_text_must_be_taken_from_the_bound_narrative_verbatim` | `src/Pry.Core/Narrative/DeliveryLedger.cs:151` | `... && accumulated.Length != boundSlice.Length` → `... && accumulated.Length > boundSlice.Length` | 第 8 格 | 由 `TryAppend:151` 末段封口核验；每追加一回；cell8 步 3 用缺字"照片？"（比 boundSlice 短，`>` 假）⇒ 不再判 `SegmentTextNotFromNarrative`、被放行 ⇒ `Assert.False(shortClose.Accepted)` 响；步 1 仍由 `:145` 正确拦；对逐字相等/其它段无扰动 |
| 9 | `MemoryDeletionSeparationTests.N3_Deleting_one_of_three_supporting_messages_recalibrates_confidence_from_two` | `src/Pry.Core/Memory/SqliteMemoryRepository.cs:396` | `Math.Min(.99, .9 + .03 * (item.RemainingSupport - 1))` → `Math.Min(.99, .9 + .02 * (item.RemainingSupport - 1))` | 第 9 格 | 由 `DetachMemoriesForDeletedMessagesAsync:396`（删除侧重标定；与 :601 学习侧同式但本波只改删除侧）；每次删一条证据一回；N3 删后剩 2 支持⇒学习侧预删=.96（:601 未动，仍 .96），删除侧重标定为 .9+.02×1=.92 ≠ .93 ⇒ `Assert.Equal(.93,...)` 响；N4 剩 1 支持⇒ .9+.02×0=.9（仍 .9，不误伤） |
| 10 | `NarrativeStorageContractTests.Commit_built_on_a_stale_story_version_is_rejected_as_late` | 产品侧同第 7 格那枚 `NarrativeCommitWriter.cs:141`（迟到支） | 同上（一枚突变双响） | 第 10 格（错因消息） | cell10：CommitAndAssert 后账上 StoryVersion=1，迟到提交 expectedStory=0，`:141` 翻成 `>` 后 `0>1` 假 ⇒ 落 `StoryVersionConflict`（应为 `LateCommitRejected`）⇒ 响；§〇.5 单独核 |

### 〇.4 G2（Api，11–18）／G3（Client，19–21）计划占位

G2/G3 的"改哪一行→预期哪一格响"＋三问，在各自组动突变前、于 §二／§三 顶部落盘（先于该组突变，满足"§〇 先于任何突变、每组突变前先有该组计划"）。工单 §③.2 给的读码起点：
- G2：`src/Pry.Api/Services/DataDirectoryWriteLease.cs`（11–14）、`RuntimeDescriptorService.cs`（12、13）、`docs/resource-limit-ledger.md`＋覆盖测试（15、16）、角色智能账本上限那把尺产品侧（17、18）。
- G3：`src/Pry.Client/PryErrorPresentation.cs`／`src/Pry.Contracts/UserPresentableErrorContract.cs`（19、20）、`src/Pry.Client/PryBackendClient.cs` 的 `api/v1/conversations/{id}?cascade=true` query 大小写（21）。

### 〇.5 第 10 格的单独核（对应工单 §③.4）

工单要求这一格回答两问，缺一不算付：
1. 线尾 `4039a50` 换进的那段新断言（`NarrativeStorageContractTests.cs` 里 `Late_commit_from_a_lost_ownership_epoch_is_rejected_without_overwriting_anything` 方法 :323–331 的 `foreach TryAppend`＋`Assert.Empty(Snapshot())`）若被临时改坏 ⇒ 响的是**那一格**（`Late_commit_from_a_lost_ownership…`，属第七波 §③.1 已付），**不是本格** `Commit_built_on_a_stale_story_version…`——二者是不同 [Fact]（:280 vs :341）。本 lane 依 §⑥"不许放宽/删除/注释掉任何既有断言"，**不去手改那段测试断言**；改以读码＋产品突变取证：第 7/10 格那枚 `:141` 翻向只让 cell7、cell10 两条 [Fact] 各响一条错因消息，而 `Late_commit_from_a_lost_ownership…` 走所有权门（:91）先拒、不触达 :141 ⇒ 保持绿 ⇒ 实测上把"本格"与该已付格分开了。此处置与工单"临时改坏那段"的字面有一处出入，记入 §七 偏差自报。
2. 本格自身在产品侧守的是 `NarrativeCommitWriter.cs:141–142` 的"迟到（落后故事版本）→ `LateCommitRejected`"这一支，与第七波守门人自证轮已付的 `DeliveryLedger.cs:79` 未登记提交门（`SegmentCommitUnknown`）是**两条不同产品行**，不重复领同一凭证。
结论取向：本格是**独立产品行为**，用 §〇.3 第 7/10 那枚 :141 突变付；若实测发现它未单独响（例如被 :138 之外的更早门吞掉），则据实改判"并入已付"并写清并入哪格、凭哪条读码——不静默算"已拆"。

### 〇.6 预算与预检纪律（本 lane 自约，照工单 §⑤）

- 预算硬顶：解决方案级构建 1／全量 1／定向 3＋机动 3。单工程构建是定向跑的隐式代价，另记不另算额度，但在 §五 如实列次数。
- 每一次 `dotnet`（含单工程构建、含 `--no-build` 的 test）之前先跑焦点预检 v5：
  `powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Users\24782\Desktop\Pry_Project\.local-notes\game-preflight5-1022.ps1"`
  整段输出（含 `VERDICT=` 行）原样贴 §五。`VERDICT=HOLD` ⇒ 就地停刀回报，不等、不绕、不改预检脚本。
- `dotnet test` 必带 `--logger trx` 与长参 `--results-directory "<绝对路径>"`；跑完先 `ls *.trx` 数到预期份数再读 `<Counters>`；跳过项逐条点 `outcome="NotExecuted"`（`notExecuted` 计数器恒 0）。
- `free_memory_gb` 低于 6 只 WARN 不阻塞；只有 SQLite `disk I/O` 或 `out of memory` 形状才作废。不自造更严作废判据。
- W2（`ContextWindowLoadTests.Every_downgrade_candidate_keeps_a_positive_input_budget`）按 §③.3 不派跑、不追红、不改判据、不删断言。

## 一 G1（Core，第 1–10 格）实跑账

公共字段（本格各格通用，逐格表内引用）：
- 载体提交号＝`d2dbdeedc1b8d0944122c115b79d816e81284b6c`（d2dbdee）。
- 跑法整行＝`dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\wave9-runs\g1" --logger trx`（本轮为该组唯一一次构建，隐式）。
- TRX 文件名＝`24782_PHYTHLINK_2026-10-22_19_39_37.trx`。
- 起跑前焦点预检＝`VERDICT=BUILD_OK`（整段原文见 §五）。
- 该轮 `<Counters>`＝`total=527 executed=525 passed=513 failed=12 notExecuted=0`（跳过不进 notExecuted，逐条点得 2 条 NotExecuted，与 §② 同名：`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`）。
- `git diff --stat`（G1 全部突变写盘后）＝7 文件 `8 insertions(+), 8 deletions(-)`：BackupInclusionPolicy 2、DataRestoreService 2、MemoryDatabase 4（两处独立单行）、SqliteMemoryRepository 2、DeliveryLedger 2、NarrativeCommitWriter 2、NarrativeContractReason 2。
- 还原方式＝`git checkout -- <改过的 7 个路径>`（只在载体内、只针对自己改的行）；还原后逐文件 `sha256sum 工作树` 对 `git show HEAD:<file> | sha256sum` 逐位比对，全部 MATCH（下表）；`git status --short`＝0 行、`rev-parse HEAD`＝d2dbdee。
- 依 §④.6，本组还原后**不再单独开定向绿跑**；复绿凭证＝§四的收尾解决方案级全量那一轮。

### 一.1 逐格（11 项字段）

| # | 红格全名（去前缀 Pry.Core.Tests.） | 被改文件:行 | before→after | diff --stat（该文件） | TRX | 消息要点 | 还原 SHA-256（work==HEAD） |
|---|---|---|---|---|---|---|---|
| 1 | `DataBackupServiceTests.Secrets_logs_runtime_and_user_imports_never_enter_or_leave_the_bundle` | `Backup/BackupInclusionPolicy.cs:38` | `".key", ` 一项 → 删除 | 2（+1/−1） | g1.trx | `Assert.DoesNotContain() Failure: Filter matched`，集合含 `files/media/server.key`（即 `.key` 伪装件入包） | `85b6805b…febe0ef` |
| 2 | `DataRestoreServiceTests.A_bundle_from_a_future_schema_version_is_refused_before_anything_is_written` | `Backup/DataRestoreService.cs:89` | `CreatedByBinarySchemaVersion > SchemaVersion` → `< SchemaVersion` | 2 | g1.trx | `Assert.Throws() No exception was thrown / Expected: SchemaVersionTooNewException` | `dac7bc82…70418b` |
| 3 | `SchemaVersionGuardTests.Initialize_refuses_a_database_registered_above_the_binaries_schema_version` | `Memory/MemoryDatabase.cs:197` | `databaseSchemaVersion <= SchemaVersion` → `<= SchemaVersion + 1` | 计入 MemoryDatabase 4 | g1.trx | `Assert.Throws() No exception was thrown / Expected: SchemaVersionTooNewException`（登记 SchemaVersion+1=11 不再拒） | `39ffc79a…39c7d`（与第 4 格同行文件） |
| 4 | `MemorySchemaMigrationRegistryTests.Registry_is_named_unique_ordered_and_never_pins_a_future_version` | `Memory/MemoryDatabase.cs:50`（baseline 步 Name） | `"既有结构的自愈建表与列补齐"` → `""` | 计入 MemoryDatabase 4 | g1.trx | `Assert.All() 1 out of 9 … SchemaMigrationStep { Id = baseline.schema, Name = , Mode = SelfHealingBaseline }` | `39ffc79a…39c7d` |
| 5 | `NarrativeIdentityContractTests.Identity_must_be_a_host_issued_stable_token(candidate: "pry")` | `Narrative/NarrativeContractReason.cs:113` | `MinIdentityLength = 8` → `= 3` | 2 | g1.trx | `Assert.Throws() No exception was thrown / Expected: NarrativeContractException`（"pry" len3 被放行） | `7d753a1d…c4aaab3` |
| 6 | 同上 `(candidate: "pry-")` | 同一枚 `NarrativeContractReason.cs:113` | 同上 | 同一枚（计入 :113） | g1.trx | 同消息（"pry-" len4 被放行；同行突变，两行各自响、各自判，均响） | `7d753a1d…c4aaab3` |
| 7 | `NarrativeStorageContractTests.A_story_version_ahead_of_the_ledger_is_rejected_too` | `Narrative/NarrativeCommitWriter.cs:141` | `commit.ExpectedStoryVersion < state.StoryVersion`（迟到支三元）→ `>` | 2 | g1.trx | `Assert.Equal() Expected: StoryVersionConflict / Actual: LateCommitRejected`（4>0 落进迟到支） | `dee9ff23…a2cbf5c` |
| 8 | `NarrativeStorageContractTests.Segment_text_must_be_taken_from_the_bound_narrative_verbatim` | `Narrative/DeliveryLedger.cs:151` | `accumulated.Length != boundSlice.Length` → `> boundSlice.Length` | 2 | g1.trx | `Assert.False() Expected: False / Actual: True`（缺字末段"照片？"不再判二次改写被放行） | `020c485c…1b09a7` |
| 9 | `MemoryDeletionSeparationTests.N3_Deleting_one_of_three_supporting_messages_recalibrates_confidence_from_two` | `Memory/SqliteMemoryRepository.cs:396`（删除侧重标定，非 :100 版本门） | `Math.Min(.99, .9 + .03 * (item.RemainingSupport - 1))` → `.9 + .02 * …` | 2 | g1.trx | `Assert.Equal() Expected: 0.93 / Actual: 0.92`（剩 2 支持按 .02 重标定） | `44d1e4ff…cfcf66` |
| 10 | `NarrativeStorageContractTests.Commit_built_on_a_stale_story_version_is_rejected_as_late` | 同一枚 `Narrative/NarrativeCommitWriter.cs:141` | 同上（一枚突变双响） | 同一枚（计入 :141） | g1.trx | `Assert.Equal() Expected: LateCommitRejected / Actual: StoryVersionConflict`（0>1 假 ⇒ 落超前支；与第 7 格同因不同消息） | `dee9ff23…a2cbf5c` |

### 一.2 红集合与预期逐格对账

预期响 10 格＝第 1–10 格；实跑 failed＝12。逐格对上：
- 10/10 目标格全部按预期响，且各自消息形状与 §〇.3 预测一致（Do-throw 类＝"No exception was thrown"；错因类＝"Expected X / Actual Y"；常量类＝"Expected 0.93 / Actual 0.92"；集合类＝"Filter matched"；Assert.All＝"Name ="）。无一"预期红而实绿"（无空转）。
- 多出的 2 格＝`MemoryDualSchemaLedgerTests.The_two_schema_ledgers_coexist_and_each_high_version_refusal_names_its_own_gate` 与 `MemoryDualSchemaLedgerTests.Both_refusals_travel_the_presentable_chain_and_neither_invites_a_retry`。两格消息均为 `Assert.Throws() No exception was thrown / Expected: SchemaVersionTooNewException`——与第 3 格同一条产品门（`MemoryDatabase.cs:197 EnsureWritableSchemaVersion`）在同版本（`schema_info = SchemaVersion+1`）上的表现。读码（`MemoryDualSchemaLedgerTests.cs:55–60、:85–90`）证明这两格各自内含一条"平台账高版本→`SchemaVersionTooNewException`"的断言，正是第 3 格守的那一行；它们第七波按 §③.1 是以**组件账门**（`memory_schema=3 → MemoryComponentSchemaRejectedException`，:47–51、:73–74）为唯一因付掉的，该组件断言本轮未被我触碰、仍成立。故这 2 格＝已付格被第 3 格突变的"平台半"顺带打红（同因附带红），既非新增覆盖、也不撤销它们第七波的已付归属；判得出唯一因（:197 平台门），无需拆单枚轮。
- 第 3 格与上述附带红的关系：第 3 格是独立 [Fact]（`SchemaVersionGuardTests`），除拒写外还独有条"主文件 SHA 一字未变＋BinarySchemaVersion/DatabaseSchemaVersion 精确值"（:76–78），非并入已付；按 §③.1 口径以本轮 :197 证据付掉，并在此明写它与已付 M5 平台半共享同一真相源。

G1 十格据上述均为"付"。还原证明见本节公共字段与下表逐位 MATCH。

## 二 G2（Api，第 11–18 格）

### 二.0 逐格计划（动本组突变之前先落盘）

本组 8 格按 §④ 分成一次批（run #2）＋一次机动单枚轮（run #3，因 11/12 与 13 的判据形状彼此纠缠，批内不可分，见每格"批内不可分原因"）。每格附 §④.1 三问（谁调用／一生命周期几回／什么输入走到拒绝或越界支）。

| # | 格 | 被改文件:行 | before → after（拟） | 轮次 | 预期响的格 | 三问要点 |
|---|---|---|---|---|---|---|
| 11 | `RuntimeDescriptorWriteGateTests.Second_host_on_the_same_directory_refuses_startup_without_touching_the_first_descriptor` | `src/Pry.Api/Services/DataDirectoryWriteLease.cs:65`（`Acquire` 的 IOException 分支抛出类型） | `throw new DataDirectoryWriteLockedException(_dataDirectory, denial.Reason, denial.Detail, failure);` → `throw new InvalidOperationException("probe", failure);` | 机动 run #3 | 第 11、12 格（同一枚，同因双响） | 调用者＝`Acquire:60` catch；每次进程取锁一回；输入＝第二个宿主 `BuildAsync`→`Acquire` 时 `runtime-write.lock` 被别的活句柄 `FileShare.None` 占住 → `FileStream` open 抛 IOException → 走到 :65。改抛出类型后 `Assert.ThrowsAsync<DataDirectoryWriteLockedException>`(:48) 响（"Wrong exception thrown"）。批内不可分原因：该枚同时点亮 11/12/13 三格（13 也断 `IsType<LockedException>`:182），与格 13 的独立"顺序门"证据不能混在一次批里，故单列为机动轮 |
| 12 | `RuntimeDescriptorWriteGateTests.Refusal_with_no_descriptor_yet_writes_none_and_a_successor_gets_the_lease_after_the_lock_is_released` | 同一枚 `DataDirectoryWriteLease.cs:65` | 同上（一枚突变双响） | 机动 run #3 | 第 12 格（错类型消息） | 同 :65；此处 `BuildAsync` 时描述文件不存在、持锁者是测试自建 foreign `FileShare.None` 句柄(:90)；open 仍抛 IOException → :65；改类型后 `Assert.ThrowsAsync<LockedException>`(:94) 响。本格的"writes-none / successor-recovers"两半与"拒发生在写之前"由 :65 同一拒绝门担保（描述文件 absent/present 走同一 IOException 支），无独立可分的产品行；工单 §③.2 明示的另一向"条件写复查失败后仍覆盖描述文件"＝已证够不着（wave5），本 lane 不追 |
| 13 | `RuntimeDescriptorWriteGateTests.A_refused_second_host_leaves_the_first_instances_open_plan_and_pending_message_untouched` | `src/Pry.Api/Program.cs:157`（`BuildAsync` 内取锁与建库/清扫的先后：`DEF-WRILEASE-ORDER-01`） | 把 `writeLease.Acquire(...)` 从 :157（InitializeAsync 与 RecoverStalePlansAsync 之前）移到清扫之后 | 批 run #2 | 第 13 格 | 调用者＝`BuildAsync`；每建一次宿主一回；输入＝第二个宿主跑到清扫（`RecoverStalePlansAsync` 取消第一个实例的 open 计划＋其 pending 消息）后才取锁 → 被拒之前已改库 → :173/:175 "两行逐字未变"响。移动后拒绝仍发生（Acquire 仍抛），只是晚于写 → 只点亮 13 的行不变断言，不动 11/12（它们只查描述文件字节/存在，不查这两行 DB 状态） |
| 14 | `RuntimeDescriptorWriteGateTests.Exit_cleanup_never_deletes_a_descriptor_owned_by_a_successor` | `src/Pry.Api/Services/RuntimeDescriptorService.cs:160`（`Clear()` 属主判定） | `if (current?.Pid != Environment.ProcessId \|\| current.Port != _writtenPort) return;` → `if (Environment.ProcessId == -1) return;`（恒不成立 ⇒ 不再跳过、照常删） | 批 run #2 | 第 14 格 | 调用者＝`ApplicationStopping`→`Clear`；每次优雅停一回；输入＝后继实例把描述文件覆盖成 Port=54321 的 `successor-entry` 后原宿主 `StopAsync` → 属主判定应跳过删除；改成恒不成立 → 直接 `File.Delete` → :241 "退出清理不得删掉后继描述文件" 响。用 `Environment.ProcessId == -1`（非编译期常量）避免 CS0162 不可达告警，语义＝把属主守卫拿掉 |
| 15 | `ResourceLimitLedgerCoverageTests.Every_limit_shaped_constant_has_a_row_in_the_ledger` | `docs/resource-limit-ledger.md`（删 `DocxProcessingTimeoutMs` 那一整行，数值一字不改） | 删除第三节… 实为第一节 `| DocxProcessingTimeoutMs | .../AttachmentTextExtractor.cs:21 | 15_000 | … |` 整行 | 批 run #2 | 第 15、16 格（同一枚，同因双响） | 调用者＝覆盖测试 `LedgerRows()`＋`MissingRows()`；每跑一回；输入＝被扫常量 `DocxProcessingTimeoutMs`（名字撞 Timeout、声明于 :21）失去表行 → 该常量无对应行 → 第 15 格 :50 `Assert.Empty(MissingRows)` 响（或 :47 "表被删薄了"）。§⑥"不改数值"＝不改任何 `数值` 列内容；删整行是工单 §③.2 第 15 格明示配方 |
| 16 | `ResourceLimitLedgerCoverageTests.The_guard_sees_a_row_being_dropped_and_a_row_drifting_off_its_line` | 同一枚（删 `DocxProcessingTimeoutMs` 行） | 同上 | 批 run #2 | 第 16 格（真表"行被删"半） | 本格真数据半 :114 `Assert.Empty(MissingRows(constants, rows))` 与第 15 格同一条 `MissingRows` 门；删行使其非空 → :114 响（消息指名 `DocxProcessingTimeoutMs` 缺行）。"行漂移"半（:115 `UntrustedRows` 与内部 :135 行号漂样张）是测试内拼的探针、不碰盘，删真行不影响它——本格唯一因＝真表少行（与第 15 格同因）；批内 :114 先于 :115 触发，实测消息即缺行。不另叠"漂移"突变，以免把 `Every_row…`（非本波 21 格）拖成额外红 |
| 17 | `CharacterIntelligenceLedgerCapBehaviorTests.A_receipt_pushed_out_of_the_window_is_refused_not_guessed` | `src/Pry.Api/Runtime/CharacterIntelligenceGateway.cs:200`（`RememberReceipt` 凭据窗淘汰取"最老"） | `var oldest = _receiptOrder[0];` → `var oldest = _receiptOrder[^1];`（改挤最新那格） | 批 run #2 | 第 17 格 | 调用者＝`RememberReceipt`←`RecordDeliveredBubble`；每条投递一回；输入＝cap+1(65) 条凭据，正常挤掉最老(第1条)⇒第1条 `NotAttributable`。改挤最新 → 第1条仍在册 → `ReportExplicitFeedback(messageIds[0])` 由 :141 期望 `NotAttributable` 变 `Recorded` → :140 响。只动 `_receiptOrder`，不碰 `_ledgerOrder`（格 18）；本格每轮即时 `TurnTransition` 收尾、pending 不越 8 → 与 :140 突变互不串 |
| 18 | `CharacterIntelligenceLedgerCapBehaviorTests.A_turn_past_the_pending_cap_drops_the_oldest_entry_and_keeps_the_newest` | `src/Pry.Core`→实为 `src/Pry.Api/Runtime/CharacterIntelligenceGateway.cs:140`（`Remember` 在飞账本淘汰取"最老"） | `var oldest = _ledgerOrder[0];` → `var oldest = _ledgerOrder[^1];`（改挤最新那格） | 批 run #2 | 第 18 格 | 调用者＝`Remember`←`Advisor`；每次建议一回；输入＝cap+1(9) 笔未收尾轮次，正常挤最老(turns[0]) ⇒ turns[0] 收尾不学。改挤最新 → turns[0] 仍在册 → `TurnTransition(turns[0])` 于 :94 触发一次学习 → :96 `Assert.Equal(0, ExchangeCount)` 响。只动 `_ledgerOrder`，与 :200 凭据窗互不串 |

批内不可分说明（§④.4）：格 11/12 与格 13 都断"第二个宿主抛 `DataDirectoryWriteLockedException`"，任何拿掉/换型该抛出的枚都同时点亮 11/12/13（13 的 `IsType`:182 也吃它）；而格 13 的产品行是 `Program.cs` 的取锁-清扫先后（顺序门），与 :65 抛出类型是两条不同判据。为给每格判到唯一充分因，把 :65 换型单列为机动轮（点亮 11/12，13 作同因附带），把顺序门留在批里（只点亮 13）。13 在机动轮里的附带红据 §③.1 归"已付格被同门顺带打红"，不重复领凭证。

### 二.1 公共字段

待实跑回填（载体提交号 d2dbdee、两次定向跑法、TRX、Counters、diff --stat、还原 SHA-256、还原后 status 行数）。

载体提交号＝`d2dbdeedc1b8d0944122c115b79d816e81284b6c`（d2dbdee）。本组分两轮（批＋机动单枚轮），每轮前各跑一次焦点预检 v5，均 `VERDICT=BUILD_OK`（整段原文见 §五）：

- 批（run #2）：突变 4 文件（B/C/E/G/H），`git diff --stat`＝`4 files changed, 6 insertions(+), 7 deletions(-)`（resource-limit-ledger −1；Program.cs 2；CharacterIntelligenceGateway 8；RuntimeDescriptorService 2）。跑法＝`dotnet test tests/Pry.Api.Tests -c Release --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\wave9-runs\g2batch" --logger trx`。TRX＝`24782_PHYTHLINK_2026-10-22_19_57_34.trx`。`<Counters total="278" executed="278" passed="272" failed="6" notExecuted="0">`。还原＝`git checkout --` 那 4 个路径，SHA-256 逐位 MATCH（下表）。
- 机动（run #3，格 11/12）：突变 1 文件（Y），`git diff --stat`＝`1 file changed, 1 insertion(+), 1 deletion(-)`（DataDirectoryWriteLease.cs:65）。跑法＝`dotnet test tests/Pry.Api.Tests -c Release --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\wave9-runs\g2maneuver" --logger trx`。TRX＝`24782_PHYTHLINK_2026-10-22_19_58_32.trx`。`<Counters total="278" executed="278" passed="275" failed="3" notExecuted="0">`。还原 SHA-256 MATCH。
- 依 §④.6，两轮还原后均不再单开定向绿跑；复绿凭证＝§四收尾全量那一轮。

### 二.2 逐格（11 项字段）

| # | 红格全名（去 Pry.Api.Tests.） | 被改文件:行 | before→after | diff | 轮次/TRX | 红格消息要点 | 还原 SHA-256（==HEAD） |
|---|---|---|---|---|---|---|---|
| 11 | `RuntimeDescriptorWriteGateTests.Second_host_on_the_same_directory_refuses_startup_without_touching_the_first_descriptor` | `Services/DataDirectoryWriteLease.cs:65`（IOException 分支抛出类型） | `throw new DataDirectoryWriteLockedException(_dataDirectory, denial.Reason, denial.Detail, failure)` → `throw new InvalidOperationException("probe: refusal bypassed", failure)` | 1（+1/−1） | 机动 g2maneuver.trx | `Assert.Throws() Exception type was not an exact match / Expected DataDirectoryWriteLockedException / Actual InvalidOperationException`（内层 IOException＝锁被占，:48 处冒到测试；与格 12 同因） | `df1a5c52…74d20` |
| 12 | `RuntimeDescriptorWriteGateTests.Refusal_with_no_descriptor_yet_writes_none_and_a_successor_gets_the_lease_after_the_lock_is_released` | 同一枚 `Services/DataDirectoryWriteLease.cs:65` | 同上（一枚突变双响） | 同一枚 | 机动 g2maneuver.trx | 同消息（:94 `Assert.ThrowsAsync<LockedException>` 吃错类型；foreign `FileShare.None` 句柄占锁仍走到 :65）。本格"writes-none/successor-recovers"两半由同一 :65 拒绝门担保，无独立可分产品行；"条件写复查失败后仍覆盖描述文件"一向＝已证够不着（wave5），不追 | `df1a5c52…74d20` |
| 13 | `RuntimeDescriptorWriteGateTests.A_refused_second_host_leaves_the_first_instances_open_plan_and_pending_message_untouched` | `Program.cs:157`（取锁 vs InitializeAsync/RecoverStalePlansAsync 的先后） | 把 `writeLease.Acquire(...)` 由清扫之前移到清扫之后 | 计入 Program.cs 2 | 批 g2batch.trx | `Assert.Equal() Strings differ / Expected: "1\|open" / Actual: "1\|cancelled"`（:173；被拒之前第二个宿主先跑了清扫，把第一个实例的 open 计划取消）。机动轮里本格 :182 亦错类型附带红＝同 :65 门，凭证归格 11/12，不重复领 | `b6913ec7…259e1` |
| 14 | `RuntimeDescriptorWriteGateTests.Exit_cleanup_never_deletes_a_descriptor_owned_by_a_successor` | `Services/RuntimeDescriptorService.cs:160`（`Clear()` 属主判定） | `if (current?.Pid != Environment.ProcessId \|\| current.Port != _writtenPort) return;` → `if (Environment.ProcessId == -1) return;`（恒假，不再跳过删除） | 2（+1/−1） | 批 g2batch.trx | `Assert.True() 退出清理不得删掉后继实例写的描述文件`（:241；后继写成 Port=54321/`successor-entry` 后原宿主 Clear 误删） | `b69d358a…aad185d` |
| 15 | `ResourceLimitLedgerCoverageTests.Every_limit_shaped_constant_has_a_row_in_the_ledger` | `docs/resource-limit-ledger.md` 第一节（删 `DocxProcessingTimeoutMs` 整行，数值未动） | 删该行 | −1 | 批 g2batch.trx | `Assert.Empty() Collection was not empty / Collection:["src/Pry.Core/TurnTaking/AttachmentTextExtractor.cs…"]`（:50 缺行；被扫常量 `DocxProcessingTimeoutMs`（声明 :21）失表行）。§⑥"不改数值"未触：只删整行 | `924e11dc…a62468b` |
| 16 | `ResourceLimitLedgerCoverageTests.The_guard_sees_a_row_being_dropped_and_a_row_drifting_off_its_line` | 同一枚（删 `DocxProcessingTimeoutMs` 行） | 同上（一枚突变双响） | 同一枚 | 批 g2batch.trx | 同消息（:114 `Assert.Empty(MissingRows)` 真数据半＝"守卫看见一行被删"，与格 15 同一条 MissingRows 门；内部行漂移样张 :135 为测试内拼、不碰盘，删真行不触它）。本格唯一因＝真表少行，与格 15 同因（§③.1 口径），非独立新增 | `924e11dc…a62468b` |
| 17 | `CharacterIntelligenceLedgerCapBehaviorTests.A_receipt_pushed_out_of_the_window_is_refused_not_guessed` | `Runtime/CharacterIntelligenceGateway.cs:200–201`（`RememberReceipt` 凭据窗淘汰取最老） | `var oldest = _receiptOrder[0]; _receiptOrder.RemoveAt(0)` → `_receiptOrder[^1]` / `RemoveAt(Count-1)`（改挤最新） | 计入 8（4 行） | 批 g2batch.trx | `Assert.Equal() Values differ / Expected: NotAttributable / Actual: Recorded`（:140；65 条凭据挤掉的是最新而非第 1 条，第 1 条仍在册被认出） | `6c5d1783…41eaebb` |
| 18 | `CharacterIntelligenceLedgerCapBehaviorTests.A_turn_past_the_pending_cap_drops_the_oldest_entry_and_keeps_the_newest` | `Runtime/CharacterIntelligenceGateway.cs:140–141`（`Remember` 在飞账本淘汰取最老） | `var oldest = _ledgerOrder[0]; _ledgerOrder.RemoveAt(0)` → `_ledgerOrder[^1]` / `RemoveAt(Count-1)`（改挤最新） | 计入 8（4 行） | 批 g2batch.trx | `Assert.Equal() Values differ / Expected: 0 / Actual: 1`（:96；9 笔在飞挤掉最新而非 turns[0]，turns[0] 收尾时仍在册被学一次） | `6c5d1783…41eaebb` |

### 二.3 红集合与预期逐格对账

- 批（run #2）预期 {13,14,15,16,17,18}＝6 红；实跑 `failed=6`，逐格对上，消息形状与 §二.0 预测一致（顺序门＝"open→cancelled"；属主门＝"不得删掉后继"；缺行＝MissingRows 集合；两把尺＝"Expected 0/Actual 1" 与 "Expected NotAttributable/Actual Recorded"）。无"预期红而实绿"（无空转），无额外红（`Every_row…`、`The_ledger_is_a_tracked_document` 均未响：删整行不损其余行的声明点，`DocxProcessingTimeoutMs` 名字仍在 §8 正文里被 :150 引用）。格 11/12 在此轮按设计保持绿（本批未含 :65 抛出类型突变）。
- 机动（run #3）预期 {11,12,13}＝3 红；实跑 `failed=3`。格 11、12 由 :65 抛出类型改坏而各响自己那条 `Assert.Throws`（同因双响，与 §③.2 给 11/12 标注的"FileShare 参数、Acquire 的 IOException 分支"同一产品行）；格 13 在此轮 :182 错类型附带红，其唯一充分因已由批轮 `Program.cs` 顺序门付掉（:173），故机动轮里的格 13＝已付格被同门顺带打红，不重复计。
- 为何把 11/12 与 13 拆成两轮：三格都断"第二个宿主抛 `DataDirectoryWriteLockedException`"，任何对该抛出的改坏（换型/吞掉）都同时点亮 11/12/13，而 13 的独立判据是 `Program.cs` 的取锁-清扫顺序（顺序门）——批内不可分（§④.4），故 13 用顺序门在批里单独响、11/12 用 :65 抛出类型在机动轮响。
- 机动额度占用：3 中用 1（run #3）。单工程构建：两轮各为定向跑的隐式代价，另记 2 次于 §五、不另算额度。

G2 八格据上述均为"付"；两枚"一枚突变双响"（11/12、15/16）按 §③.1 同因口径登记，格 13 在机动轮的附带红归已付格顺带。全部还原后载体 `git status --short`＝0 行、`rev-parse HEAD`＝d2dbdee，五文件 SHA-256 逐位 MATCH。

## 三 G3（Client，第 19–21 格）

### 三.0 逐格计划（动本组突变之前先落盘）

三格同一批（run，定向 Pry.Client.Tests）。每格附 §④.1 三问。19/20 同处 `PryErrorPresentation.cs` 的 `Describe` switch：19 判"守卫档逐字透传守卫自己的 Headline"、20 判"守卫档排在超时档之前"，两处改坏以红格消息（:21 Headline 相等 vs :43 Kind==StorageGuarded）分得开；21 在 `PryBackendClient.cs`。

| # | 格 | 被改文件:行 | before → after（拟） | 预期响的格 | 三问要点 |
|---|---|---|---|---|---|
| 19 | `StorageGuardErrorPresentationTests.A_guarded_refusal_is_presented_verbatim_as_storage_error` | `src/Pry.Client/PryErrorPresentation.cs:52`（守卫档 Headline 槽） | `guarded => new PryUserError(PryErrorKind.StorageGuarded, guarded.Headline, guarded.Body, …, guarded.RetryAllowed)` 中 Headline 实参 `guarded.Headline` → 字面量 `"Pry 遇到了未预期的问题"` | 第 19 格 | 调用者＝`Describe` switch；每次呈现一回；输入＝`FakeGuardError`（Exception＋IUserPresentableError）。守卫头不再逐字取自契约 → :21 `Assert.Equal("本机数据库来自更新版本的 Pry", Headline)` 响。Body/Kind/CanRetry 不动 → `An_allowed_retry_guard…`(:57 只查 Body 含"回滚")、`Guard_technical_detail…`(:31 查 TechnicalDetail) 不响 |
| 20 | `StorageGuardErrorPresentationTests.The_guard_arm_precedes_every_generic_bucket` | `src/Pry.Client/PryErrorPresentation.cs:52–53`（守卫档位置） | 把 `IUserPresentableError guarded => …` 整档移到 `TimeoutException or TaskCanceledException => …` 那档之后 | 第 20 格 | 调用者＝`Describe` switch；每次一回；输入＝`TimeoutShapedGuard`（继承 `TimeoutException` 且实现契约）。守卫档下移到超时档之后 → 该异常先命中超时档（Kind=Timeout、CanRetry=true、Body 含"再试一次"）→ :43 `Assert.Equal(StorageGuarded, Kind)` 响。`FakeGuardError`/`FakeRepairGuardError` 非 Timeout、仍只匹配守卫档（在 `_` 之前）→ 19/其它格不因此响；`Plain_exceptions_still_fall_back`（InvalidOperationException）不经守卫档仍落 `_`→绿 |
| 21 | `PryBackendClientTests.Delete_conversation_asks_for_the_cascade_in_lowercase_on_the_query` | `src/Pry.Client/PryBackendClient.cs:112`（DELETE 会话 query 布尔值大小写） | `?forgetExclusiveMemories={forgetExclusiveMemories.ToString().ToLowerInvariant()}` → 去掉 `.ToLowerInvariant()`（`ToString()` 出 `True`/`False`） | 第 21 格 | 调用者＝`DeleteConversationAsync`←`SendNoContentAsync`；每次删会话一回；输入＝`forgetExclusiveMemories:true`→query 值成 `True` → :124 `Assert.Equal("…=true", sent)` 响（转义 `a%26b` 与开关名不变，只大小写漂）。仅本测试调用该方法，无外溢 |

### 三.1 公共字段

待实跑回填（载体提交号、跑法、TRX、Counters、diff --stat、还原 SHA-256、status 行数）。

载体提交号＝`d2dbdeedc1b8d0944122c115b79d816e81284b6c`（d2dbdee）。三格同一批一轮，起跑前焦点预检 `VERDICT=BUILD_OK`（整段原文见 §五）。

- 跑法＝`dotnet test tests/Pry.Client.Tests -c Release --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\wave9-runs\g3" --logger trx`。TRX＝`24782_PHYTHLINK_2026-10-22_20_01_56.trx`。`<Counters total="108" executed="108" passed="105" failed="3" notExecuted="0">`。
- `git diff --stat`（写盘后）＝`2 files changed, 3 insertions(+), 3 deletions(-)`：PryBackendClient.cs 2、PryErrorPresentation.cs 4（守卫档移动 2 行＋Headline 字面量化 1 行，同文件合计 +3/−3 中的 PryErrorPresentation 部分）。
- 还原＝`git checkout --` 那 2 个路径，SHA-256 逐位 MATCH（下表）；`git status --short`＝0 行、`rev-parse HEAD`＝d2dbdee。
- 依 §④.6 本组还原后不再单开定向绿跑，复绿凭证＝§四收尾全量那一轮。

### 三.2 逐格（11 项字段）

| # | 红格全名（去 Pry.Client.Tests.） | 被改文件:行 | before→after | diff | TRX | 红格消息要点 | 还原 SHA-256（==HEAD） |
|---|---|---|---|---|---|---|---|
| 19 | `StorageGuardErrorPresentationTests.A_guarded_refusal_is_presented_verbatim_as_storage_error` | `src/Pry.Client/PryErrorPresentation.cs:52`（守卫档 Headline 实参） | Headline 槽 `guarded.Headline` → 字面量 `"Pry 遇到了未预期的问题"` | 计入 PryErrorPresentation.cs 4 | g3.trx | `Assert.Equal() Strings differ / Expected: "本机数据库来自更新版本的 Pry" / Actual: "Pry 遇到了未预期的问题"`（:21，契约自述头不再逐字透传） | `5ef925c5…97fe37f2` |
| 20 | `StorageGuardErrorPresentationTests.The_guard_arm_precedes_every_generic_bucket` | `src/Pry.Client/PryErrorPresentation.cs:52–53`（守卫档位置） | 守卫档整档从超时档之前移到之后 | 计入 4 | g3.trx | `Assert.Equal() Values differ / Expected: StorageGuarded / Actual: Timeout`（:43，`TimeoutShapedGuard` 先命中超时档） | `5ef925c5…97fe37f2` |
| 21 | `PryBackendClientTests.Delete_conversation_asks_for_the_cascade_in_lowercase_on_the_query` | `src/Pry.Client/PryBackendClient.cs:112`（query 布尔值大小写） | `…={forgetExclusiveMemories.ToString().ToLowerInvariant()}` → `…={forgetExclusiveMemories.ToString()}` | 2（+1/−1） | g3.trx | `Assert.Equal() Strings differ（pos 59）/ Expected: "…forgetExclusiveMemories=true" / Actual: "…=True"`（:124，去掉小写化后 `bool.ToString()` 出首字母大写） | `c448fff2…0fca72b6` |

### 三.3 红集合与预期逐格对账

- 预期 {19,20,21}＝3 红；实跑 `failed=3`，逐格对上，三条消息形状互不相同（Headline 相等 / Kind 相等 / query 大小写）→ 各判到唯一充分因，无空转、无额外红。
- 同文件的 19 与 20 不互相吞：`FakeGuardError`（非 Timeout）即便守卫档下移仍只匹配守卫档、命中被字面量化的 Headline → :21 响；`TimeoutShapedGuard` 命中上移到前面的超时档 → :43 响，两点都到不了对方的判据。
- 未响的同文件邻格据读码应保持绿并已实测绿：`An_allowed_retry_guard_keeps_its_own_headline_and_retry_flag`（:55/56/57 查 Kind/CanRetry/Body 含"回滚"，Headline 与位置改动都不触 Body 与 RetryAllowed 透传）、`Guard_technical_detail…`（查 TechnicalDetail）、`Plain_exceptions_still_fall_back_after_the_guard_arm_was_added`（InvalidOperationException 不经守卫档，落 `_`）——本轮实跑均通过。105 通过数与基线 108 减 3 红吻合。

G3 三格据上述均为"付"。

## 四 收尾那一轮：四个分项与顶账（§④ 末 / §②）

全部突变还原后，在载体（detached d2dbdee、`git status --short`＝0 行）上做解决方案级构建 1 次＋全量 1 次，充当 §④.6 要求的"每组复绿"凭证（覆盖面比逐组绿跑大）。

- 构建＝`dotnet build Pry.slnx -c Release`：`已成功生成，0 个警告，0 个错误`，9 个项目全部出 dll（含 4 个测试项目）。构建前预检 `VERDICT=BUILD_OK`。
- 全量＝`dotnet test Pry.slnx -c Release --no-build --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\wave9-runs\final" --logger trx`。先 `ls *.trx` 数到 **4 份**再读 `<Counters>`。全量前预检 `VERDICT=BUILD_OK`。

四个分项（逐 TRX 读 `<Counters>`）：

| 程序集 | TRX | total | executed | passed | failed | notExecuted 计数 |
|---|---|---|---|---|---|---|
| Pry.Core.Tests | `..._20_02_59[2].trx` | 527 | 525 | 525 | 0 | 0 |
| Pry.Api.Tests | `..._20_02_59[1].trx` | 278 | 278 | 278 | 0 | 0 |
| Pry.App.Tests | `..._20_03_02.trx` | 233 | 233 | 233 | 0 | 0 |
| Pry.Client.Tests | `..._20_02_59.trx` | 108 | 108 | 108 | 0 | 0 |
| 合计 | 4 份 | **1146** | **1144** | **1144** | **0** | 0 |

顶账（对 §②）：总 **1146＝passed 1144＋0 红＋2 条环境跳过**，四分项逐位对上基线（Core 527/525/525/0、Api 278、App 233、Client 108），差 0 条。
两条跳过按 `outcome="NotExecuted"` 点名（`notExecuted` 计数器恒 0，跳过不进那一栏，只能逐条点结果结点——控制台亦打印 2 条 `[SKIP]`）：

- `Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`
- `Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`

与 §② 起点读数**同名同条数**（2 条，均在 `Pry.Core.Tests.dll`）。本轮未改任何判据条数。收尾轮里四个测试项目均无红 ⇒ 三组（G1/G2/G3）还原干净（若某组没还原净，此处会残留该组的红）。

## 五 预算终账 + 逐次焦点预检整段原文

预算上限＝解决方案级构建 1／全量 1／定向 3＋机动 3。

| 用途 | 次数 | 是否计额度 | 结果 |
|---|---|---|---|
| 解决方案级构建（收尾） | 1 | 计（用满 1/1） | 0 警告 0 错误 |
| 全量（收尾 --no-build） | 1 | 计（用满 1/1） | 1146＝1144＋0＋2 |
| 定向跑（三组各一） | 3 | 计（用满 3/3）：G1 Core、G2 批 Api、G3 Client | 见各节 |
| 机动（单枚归属轮） | 1 | 计（用 1/3）：G2 run #3（:65 换型，点亮 11/12） | 剩 2 未动 |
| 单工程构建（定向跑隐式代价） | 5 | 另记不另算 | G1×1、G2批×1、G2机动×1、G3×1、+ 收尾 solution build 前无单工程 |
| 收尾全量 --no-build | 0 构建 | 复用上面 solution build 产物 | — |

> 注：定向跑与机动的单工程构建是各自那一轮 `dotnet test` 的隐式代价（`Pry.<X>.Tests` 及其依赖被增量编译），如实列 5 次，不占定向/机动额度。W2（cell 22）按 §③.3 未派跑。

逐次焦点预检 v5 整段原文（`-File .local-notes\game-preflight5-1022.ps1`，每一次 `dotnet` 之前各跑一次；共 6 次，其中 G2 批/机动、G3、收尾 build、收尾全量 5 次的原文在下；G1 那次见下注）：

G2 批（run #2，`dotnet test tests/Pry.Api.Tests`，TRX 19_57_34）之前：
```
=== GAME PREFLIGHT v5 (foreground rule, desktop-shell focus excluded) ===
  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=1439,242,1069x871 screen=2560x1440 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=4% used=8632MB
free_memory_gb=6.8
VERDICT=BUILD_OK
```
G2 机动（run #3，TRX 19_58_32）之前：
```
=== GAME PREFLIGHT v5 (foreground rule, desktop-shell focus excluded) ===
  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=1439,242,1069x871 screen=2560x1440 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=3% used=8690MB
free_memory_gb=7.3
VERDICT=BUILD_OK
```
G3（TRX 20_01_56）之前：
```
=== GAME PREFLIGHT v5 (foreground rule, desktop-shell focus excluded) ===
  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=1439,242,1069x871 screen=2560x1440 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=29% used=8828MB
free_memory_gb=6.9
VERDICT=BUILD_OK
```
收尾 solution build 之前：
```
=== GAME PREFLIGHT v5 (foreground rule, desktop-shell focus excluded) ===
  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=1439,242,1069x871 screen=2560x1440 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=3% used=8803MB
free_memory_gb=6.9
VERDICT=BUILD_OK
```
收尾全量（--no-build）之前：
```
=== GAME PREFLIGHT v5 (foreground rule, desktop-shell focus excluded) ===
  FOREGROUND: Xshell pid=47420 class=Xshell8::MainFrame_0 style=0x14CFC000 rect=295,162,1147x805 screen=2560x1440 covers=False borderless=False title=LinkPry - root@linkpry: ~ - Xshell 8 (Free for H
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=11% used=8852MB
free_memory_gb=6.1
VERDICT=BUILD_OK
```
全部 6 次 `VERDICT=BUILD_OK`，无一次 `HOLD`。`free_memory_gb` 最低 6.1（>6，未触发 WARN，亦未出现 SQLite disk I/O / OOM 形状）。

> G1 那次预检（`dotnet test tests/Pry.Core.Tests`，TRX 19_39_37）在本 lane 的上下文边界之前执行，`VERDICT=BUILD_OK` 已记于 §一；其整段多行输出未随上下文压缩保留，故未在此逐字重贴（见 §七.4 偏差自报）。该次同样是每一次 dotnet 之前各跑一次纪律内的一次。

## 六 够不着清单（每格附实测过的配方名＋TRX）

本波 21 格**无一登记为够不着**——全部按 §③.1 口径付掉。下列两处"够不着"是工单已明登、本波据规则**不再追**的对象（不是"没拆"、不是"不需要拆"）：

1. **W2 / cell 22 `ContextWindowLoadTests.Every_downgrade_candidate_keeps_a_positive_input_budget`**：本波 §③.3 不派跑。实测配方与凭证在第八波：单枚 `M-CB1`（`ContextBuilder.cs:34` 去掉半窗夹）→ `Passed`；`M-CB1∧M-CB6`（`:35` 再去掉 `- MinimumInputTokens`）→ `Passed`（第八波清洁轮 `run8-gk-mw3-only` 一族）。根因＝`ContextBuilder.cs:39` 返回门 `inputBudget >= MinimumInputTokens(512)`，两夹同拿后归一化返 false → `ModelPerformancePolicy.WithContext:48` 落兜底 `MaxOutputTokens=1` → 反给正输入预算 ⇒ 设计性冗余（防御纵深），现有产品形状下结构上不会红。本轮未提出"能同时让 :39 返 true 又让降级候选预算非正"的可达配方 ⇒ 据 §③.3 照实登记"仍够不着，与第八波同因，本轮未再跑"。
2. **cell 12 的一向"条件写复查失败后仍覆盖描述文件"**：§③.2 明记"已证够不着的那一向不要再追"，凭证在 `docs/dispatch-hds-wave5-20261022.md`「出闸前预检」（够不着实测清单）。本格另一向（:65 拒绝抛出类型）已由机动轮 TRX `..._19_58_32.trx` 付掉（见 §二.2 格 12），故本格整体登记为"付（拒绝门那半）＋一向已证够不着不追"。

## 七 偏差自报

1. **§③.4 第 10 格的处置与工单字面有一处出入**：工单要"把线尾 `4039a50` 那段新断言临时改坏"以看谁响。本 lane 依 §⑥"不许放宽/删除/注释掉任何既有断言"，未手改 `NarrativeStorageContractTests.cs` 的测试断言，改以读码＋产品侧 `NarrativeCommitWriter.cs:141` 突变（cell 7/10 一枚双响、各响自己错因消息）回答 §③.4 两问，`Late_commit_from_a_lost_ownership…`（已付格）经所有权门 :91 先拒、不触 :141 保持绿，实测上把本格与该已付格分开。详见 §〇.5。
2. **G1 批内 12 红 vs 预期 10**：多出 2 红为已付格 `MemoryDualSchemaLedgerTests.The_two_schema_ledgers_coexist…`／`Both_refusals_travel…` 被第 3 格突变（`MemoryDatabase.cs:197` 平台门）顺带打红（同因、消息"没有抛出异常/期望 SchemaVersionTooNewException"）。按 §④.4 判到唯一因（:197）后归"已付格附带红"，不撤销其第七波归属、不新增覆盖；未为此单拆机动轮。
3. **机动额度未用满**：3 中用 1（G2 的 11/12 归属）。G3 三格批内即可分（消息形状各异），未动用机动。剩余额度如实留白，未"多跑一轮求确认"。
4. **§五 G1 预检整段输出未逐字重贴**：该次预检在上下文压缩边界之前、`VERDICT=BUILD_OK` 已存 §一，多行原文未随压缩保留。这是本 lane 唯一未能原样呈现的记录项；其余 5 次预检均在边界内、原文完整。纪律本身（每一次 dotnet 前各跑一次、遇 HOLD 停刀）未违反：全程无 HOLD。
5. **收尾时机器上 dotnet/testhost 进程数＝8**：为本 lane 自身 `dotnet build`／`dotnet test` 触发的 Roslyn build-server 节点复用与 VSTest testhost 残留，非作者进程、非载体树内改动（载体 `git status --short`＝0）。依 §⑥"不碰作者的任何进程"未强杀；不影响交付。
6. **零提交**：本波在 `src/`／`tests/`／`docs/` 无任何 commit；§① 第 0 步的 `checkout --detach d2dbdee` 是挪树不是提交。全部改坏仅存在于载体工作树并在各组跑后逐字节还原（§一/二/三 各节 SHA-256 MATCH）。主工作树两枚他人名下暂存项未读改、未 `stash`/`clean`。

## 八 守门人复证（2026-10-22 深夜；本节由守门人写，上文 §〇～§七 一字未改）

方法＝当场从盘上重数，不采信自述。两把仪器都在 gitignored 的 `.local-notes/`、零构建：`wave9-readtrx-1022.js`（逐 TRX 读 `<Counters>`，并按 `<UnitTestResult outcome="Failed">` 与 `outcome="NotExecuted"` 结点去重点名红格与跳过格）、`wave9-msgs-1022.js`（读结果块内 `<Message>` 与 `<StackTrace>` 锚点）。后者本轮先修过自身口径：TRX 里行号写作 `.cs:line 71`、且 Windows 路径本身含 `:`，先前那个字符类抓取恒空，差点把"抓不到锚点"误读成"证据缺失"。

### 八.1 盘上产物与计数器（逐份实读）

| 目录 | TRX | 程序集 | total／executed／passed／failed | 守门人数到的红格名 |
|---|---|---|---|---|
| g1 | `..._19_39_37.trx` | Pry.Core.Tests | 527／525／513／12 | 12 |
| g2batch | `..._19_57_34.trx` | Pry.Api.Tests | 278／278／272／6 | 6 |
| g2maneuver | `..._19_58_32.trx` | Pry.Api.Tests | 278／278／275／3 | 3 |
| g3 | `..._20_01_56.trx` | Pry.Client.Tests | 108／108／105／3 | 3 |
| final | 4 份（`20_02_59`×3＋`20_03_02`） | Client／Api／Core／App | 108／0、278／0、527／525／525／0、233／0 | 0（Core 另有 2 条 `NotExecuted` 结点） |

收尾四份合计＝总 1146／passed 1144／failed 0／2 条环境跳过，与在册基线逐位相同；两条跳过按结点点名＝`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`，与 §② 同名同数。

产物总数＝8 份 TRX，目录只有 `final／g1／g2batch／g2maneuver／g3` 五个 ⇒ 没有第五个批次，也没有"多跑一轮求确认"；三组还原后没开定向绿跑＝工单 §④.6 的取消令，属合规而非缺口。

### 八.2 21 格全付、2 格附带红、无一空转

四批红并集去重＝23 格，与 §③.2 前 21 行逐格点名对上：21/21 都拿到当场红，无"预期红而实绿"，多出的 2 格正是 §七.2 自报的 `MemoryDualSchemaLedgerTests` 两格。W2 没被重跑＝四批红集合里没有任何 `ContextWindowLoadTests` 格（§③.3 合规）。

### 八.3 被指的 21 行产品码，当场逐行验在（载体＝d2dbdee，`git status --short`＝0 行）

`BackupInclusionPolicy.cs:38` ＝ `".key", ".pfx", ".pem", ".secret", ".token"`；`DataRestoreService.cs:89` ＝ `manifest.CreatedByBinarySchemaVersion > MemoryDatabase.SchemaVersion`；`MemoryDatabase.cs:197` ＝ `if (databaseSchemaVersion <= SchemaVersion) return;`；`MemoryDatabase.cs:50` ＝ baseline 步的 Name 字面量；`NarrativeContractReason.cs:113` ＝ `public const int MinIdentityLength = 8;`；`NarrativeCommitWriter.cs:141` ＝ `commit.ExpectedStoryVersion < state.StoryVersion`（同式在 :145 还出现一次，本轮改的是 :141 那处三元）；`DeliveryLedger.cs:151` ＝ `accumulated.Length != boundSlice.Length`；`SqliteMemoryRepository.cs:396` ＝ `Math.Min(.99, .9 + .03 * (item.RemainingSupport - 1))`；`Program.cs:157` ＝ `writeLease.Acquire(descriptorPresent, descriptorProcessAlive)`；`RuntimeDescriptorService.cs:160` ＝ `if (current?.Pid != Environment.ProcessId || current.Port != _writtenPort) return;`；`DataDirectoryWriteLease.cs:65` ＝ `throw new DataDirectoryWriteLockedException(...)`；`CharacterIntelligenceGateway.cs:140` ＝ `var oldest = _ledgerOrder[0];`、`:200` ＝ `var oldest = _receiptOrder[0];`；`docs/resource-limit-ledger.md` 的 `DocxProcessingTimeoutMs` 行在位且数值未动；`PryErrorPresentation.cs:52` ＝ `IUserPresentableError guarded => new PryUserError(PryErrorKind.StorageGuarded, guarded.Headline, ...)`；`PryBackendClient.cs:112` ＝ `...ToLowerInvariant()`。

⇒ 每格指向的文件:行号与该行文本都落得住，报告里的行号不是转述。

### 八.4 还原凭证抽验

SHA-256 抽 6 份，首 8／末 8 与报告自述逐字相同：`PryErrorPresentation.cs` ＝ `5ef925c5…97fe37f2`；`CharacterIntelligenceGateway.cs` ＝ `6c5d1783…541eaebb`；`NarrativeCommitWriter.cs` ＝ `dee9ff23…ea2cbf5c`；`docs/resource-limit-ledger.md` ＝ `924e11dc…ea62468b`；`BackupInclusionPolicy.cs` ＝ `85b6805b…1febe0ef`；`Program.cs` ＝ `b6913ec7…1e0259e1`。加载体 `git status --short`＝0 行、`rev-parse HEAD`＝`d2dbdeedc1b8d0944122c115b79d816e81284b6c` ⇒ 全部改坏已不在树上。

### 八.5 归属质量（守门人独立抽的四条，不靠消息转述）

1. 第 1 格：完整消息里集合含 `files/media/server.key`（Sha256 `282227a0…`、35 字节），指针落在 `↓ (pos 6)` ⇒ "去掉 `.key` 这一项后伪装密钥件入包"这条因果在盘上直接可见，不是从断言名推的。
2. 第 8 格：TRX 锚＝`NarrativeStorageContractTests:203`，读码确认 :202 就是那枚缺字样张 `TryAppend(Segment(1, "照片？"), "host-a")`、:203 是 `Assert.False(shortClose.Accepted)` ⇒ 与 §〇.3 预测的触发点逐行一致。
3. 附带红两格：锚＝`:56` 与 `:86`，两处都是 `Assert.ThrowsAsync<SchemaVersionTooNewException>`（平台账那一半），而组件账那一半在 `:48` 与 `:74`——第七波付掉的正是后两者 ⇒ "同一方法里的另一半被第 3 格顺带打红、既不重复领凭证也不撤销第七波归属"这句判语站得住。
4. 机动轮三格：`<StackTrace>` 里字面含 `DataDirectoryWriteLease.cs:65`（并带 `Program.cs:157`／`:171`）⇒ 那枚"抛出类型换掉"由栈直接证明，不必靠消息形状猜；格 13 在此轮是 `:182` 的同门附带红、凭证归 11/12，与报告一致。

### 八.6 预算与调用次数：一条方法更正（连带纠正守门人自己上一轮的取证方式）

盘上能证的调用数＝测试跑 4 次（四份红 TRX）＋收尾全量 1 次（四份 TRX，`--no-build`）＋解决方案级构建 1 次（收尾前，§四记 0 警告 0 错误）＝工单 §⑤ 的 构建 1／全量 1／定向 3＋机动 1（3 中用 1），未超支。

**但第六波 §⑧ 用来数调用次数的那条证据这次不成立**：`%TEMP%\Microsoft.NET.Workload_*.log` 在 19:33～20:27 窗口有 76 份，而凌晨 00:0x、03:5x、22:3x 这些无人跑测试的时段同样每十分钟有 10～30 份 ⇒ 它是 SDK 工作负荷解析的噪声，不能当 dotnet 调用次数用。第六波那次"十份日志与九批次一一对上"的推断按今日实测降级为不可复证；此后只认 TRX 份数与产物时间戳。报告自述的"单工程构建 5 次"属定向跑隐式代价、不占额度，盘上既证不实也伪不了，如实登记。

### 八.7 两处按最严口径仍挂的账

1. 预检留档差一次：§五 有 5 次整段原文（全 `VERDICT=BUILD_OK`，`free_memory_gb` 最低 6.1、未触发 WARN，全程无 SQLite `disk I/O`／OOM 形状），G1 那次只有结论没有原文 ⇒ 与第六波同一条口径登为欠账，不追认字面合规。
2. §③.4 的冲突归守门人：本单 §③.4 要执行方"临时改坏 `4039a50` 换进的那段新断言"，而 §⑥ 禁止放宽／删除／注释任何既有断言——两条正面冲突，属工单作者的编写错误，不是执行方违规；lane 选 §⑥ 并自报偏差，处置正确。效力边界＝第 10 格十一项字段齐全、唯一因（`NarrativeCommitWriter.cs:141`）判得出 ⇒ 按 §③.1 口径算付；仍未取得盘上反证的那一问是"第七波那段新断言（`NarrativeStorageContractTests.cs:323–331`）对本格是否承重"，本波只以读码＋实测分离回答（:281 与 :341 是两个不同 [Fact]、各守不同产品行，:141 突变下 :281 那格保持绿）。附一句：第七波那一格的红由守门人本人补拿，若要按"非实现方之外还要第三次换人"的最严口径处理，那是另立单的事，本波既不并入也不撤销。

### 八.8 进程残留（守门人当场实测）

`dotnet` 进程 8 个，全部起始于 20:02:40（＝收尾解决方案级构建那一刻），合计工作集 31 MB；无 `testhost`、无作者名下 Pry／llama 进程。⇒ §七.5 的形状成立，只是这 8 个实测都属 build-server 一类。守门人未强杀：它们不是作者的进程，但也没有任何读数需要靠杀进程换取。

### 八.9 顶账与本波之后的欠账

在册基线照旧＝总 1146／passed 1144／0 红／2 条同名环境跳过，本波判据条数变化 0；catalog 76；`verify-repository` 受跟踪文件条数随本刀 550→551（是条数，不是门槛）。拆法原文的分母 46＝已付 24＋本波 21＝45，仍欠 1 格＝W2：要还必须先造出"能让 `ContextBuilder.cs:39` 返 true、同时降级候选的输入预算非正"的可达配方，按 §③.3 不再派跑。其余在册欠账不变＝`QA-TIP1020-02`（卡他人暂存项）、`QA-TIP1019-06` teardown 总清、`ModelProcessRegistry.cs:63` 第二道门的新缝、F2①"降级继续写"一向，另加本波新登记的 G1 预检原文 1 次。零 `src/`、零 `tests/` 改动；本波不 tag、不 push、不打包。
