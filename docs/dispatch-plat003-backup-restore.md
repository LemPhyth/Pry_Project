# 工单 `PLAT-003-BACKUP-RESTORE`（第 5 层 · 备份/恢复/完整性/迁移注册表）＋同层两件守门人修补

- **状态词**：主单＝**已派工·待认领**（守门人 2026-10-19 最晚按作者授权开出；主单 Owner＝Core Platform／Persistence——历轮登记明写"schema 编号由其分配"；判定归 QA；结案词归作者）；§2 那两件守门人自领会的修补＝**已成本刀·待归位**（10-19 更晚同场登记，逐刀读数见下面 §4）＝分支 `codex/gatekeeper-s1s2-1019` 上的 `30a7a5a`（S1）＋`8f7dcb1`（S2），二者已 rebase 到线尾 `332161b`，未 push、无 tag。
- **授权来源（逐字）**：作者 2026-10-19「**我不想思考了，你就继续给我派工单，该提交的提交吧，早点做完之后再回来大修**」⇒ 第 5 层不再等逐层请示；本单纯文本里守门人代拍的范围即 `PROJECT_PLAN.md` §4.2 `PLAT-003` 行本体，**没有夹带任何新功能**。同日下午再补一句「**按照守门人的派工进行工作**」⇒ §2 这两件按本单文字执行，未扩范围。
- **载体基线**：开工当日重取线尾；在册读数＝886／0／2（Client 93／App 221／Api 185／Core 387+2skip），引用必写全四项。**10-19 更晚本行补一格事实**：`2-C` 在 17:49 入库为 `e2b521a`（其自跑 904＝App 226／Api 188／Core 397），本单两件再叠 7 条判据，**守门人 rebase 后在 `332161b` 基座上的自跑读数＝911／0／2（Client 93／App 226／Api 195／Core 397＋2skip，两次分项逐位相同）**——这三个数都是载体自跑，**在册基线仍 886，改写权在 QA 复采**。

## 1. 主单 `PLAT-003`：事实表（守门人 2026-10-19 当场读码）

| # | 事实 | 位置 |
|---|---|---|
| P1 | schema 版本是硬编码常量 `SchemaVersion = 10` | `src/Pry.Core/Memory/MemoryDatabase.cs:158` |
| P2 | 启动路径只有"读旧值→建表→写新值"一条直线（`:27`、`:145`），**没有按版本号的迁移注册表**；现存迁移步骤里能看到两条语句串在一次执行里没有事务包裹的形状 | `MemoryDatabase.cs:27,145,959-961` |
| P3 | 完整性检查只在记忆表迁移路径里手写了两句（外键检查＋integrity 失败抛），**启动面与发行面没有 `PRAGMA integrity_check` 的固定档位** | `src/Pry.Core/Memory/SqliteMemoryRepository.cs:137-140` |
| P4 | 备份／恢复：**整个 `src/` 零个** `Backup`／`Restore` 端点、服务或脚本（`CharacterStateService.RestoreAsync` 与 `ConfigurationApplicationService` 里的 `interactionBackup` 是"删卡失败回滚临时文件"，不是用户备份） | 当场 `grep -rn "Backup\|Restore" src/` 复核 |
| P5 | 义务来源：§2.4 的 1.0 清单含"导出/备份/恢复"；`PROJECT_PLAN.md` §2.3 迁移约束第 5 条要求**数据迁移必须保留角色、会话、记忆与设置** | 台账文本 |

### 主单完成标准

1. **备份/恢复有正式入口**：DTO＋验证＋错误语义＋`docs/api-v1.md` 文档位＋测试（AGENTS 第 5 条例子，一步不许缺）；备份物覆盖角色卡、设置、会话库、记忆库，恢复到隔离数据目录可用。
2. **迁移注册表**：`SchemaVersion` 从"改常量＋祈祷建表 IF NOT EXISTS 兜住"改为**按版本号的有序迁移表**，每条迁移自带事务与失败语义；从 `versionBefore` 到当前值的任意升级路径可测。
3. **完整性档位**：启动跑一次轻量 `PRAGMA integrity_check`（失败→明确错误面＋诊断导出路径，不许静默），备份与恢复各自完成后必查一次。
4. **"迁移保留四类数据"有判据**：真库升级前后角色／会话／记忆／设置四类计数与关键行逐字段对照的用例（§2.3 第 5 条从台账文字变成会红的测试）。
5. 零新阈值（`2-D` 门照旧）；不许夹带安装器/升级策略等第 5 层其余格——那是后续刀。

### 主单反向约束

- **不许**把备份文件写进 `runtime/`、`models/` 或任何被 `.gitignore` 保护以免泄密的目录语义里打擦边球（AGENTS 第 6 条：密钥/DB/用户导入内容不得提交——备份产物默认落用户数据目录，路径可配）。
- **不许**用"删库重建"冒充迁移；迁移失败必须可回滚或明示不可回滚（P2 那格欠的就是这句）。
- 恢复入口**不许**绕过会话令牌中间件；备份导出**不许**带出 `runtime.json` 或密钥。
- 单一写者：本刀写者＝Core Platform；`MemoryDatabase.cs` 与 `SqliteMemoryRepository.cs` 在 `2-C` 认领归位前不得同文件双写——**开工前先确认 `2-C` 那半边已提交或已归位**，否则本刀与它撞行，等归位完成再动。**⇒ 10-19 更晚当场复核：这道硬前置已经达成**——`2-C` 那半边＝`e2b521a`（17:49 入库，主工作树此刻除他人一份未跟踪草稿外零改动），它在 `MemoryDatabase.cs` 只落 6/4 行且全是 N8 级联的参数透传（`DeleteConversationAsync(…, bool forgetExclusiveMemories)` 与三处调用点），**主单要动的那三格一格没被它动**：重读线尾 `332161b` 得 `SchemaVersion = 10` 仍在 `MemoryDatabase.cs:158`、启动路径仍是"读旧值→建表→写新值"一条直线（`:27`、`:145`）、`P3` 那两句手写检查仍在 `SqliteMemoryRepository.cs`（本单原文写 `:137-140`，线尾重读因 `2-C` 那 45/1 行的改动漂到 **`:135` 与 `:138`**＝`PRAGMA foreign_key_check` 与 `PRAGMA integrity_check` 各一处，除行号外形状未变，全仓仍无第二处 `integrity_check`）。⇒ **本主单自此没有同文件双写风险，可以认领**；`P4`（备份/恢复零入口）同场重读仍成立——`grep -rn "Backup\|Restore" src/` 的 17 处命中全是"角色状态恢复／草稿恢复／撤销回滚"那一族（`CharacterStateService.RestoreAsync`、`ConversationDraftStore.Restore`、`RestoreConversationMutationAsync`、`RestoreDetachedMemoriesAsync`、`RestoreTurnAsync`、`interactionBackup` 临时文件回滚），**没有一个用户备份端点、服务或脚本**。

## 2. 同层两件·守门人 lane 自认领（不占上单，各成一小刀）

- **S1（安全修补，10-19 只读审计的 Low 项）**：`ConfigurationApplicationService.cs:747-754` 的 `ValidateModelUrl` 只对 `openai-compatible` 强制"HTTPS 且非回环"，`local-llama` 档位可以填**远程 http**——本地后端名不副实，聊天内容会明文出境。修法＝`local-llama` 强制回环主机（`127.0.0.1`/`localhost`/`::1`），越界＝400＋错误语义＋文档一句话＋两条判据（填远程必红、回环保留必过）。零产品行为变更（正常用户本来就填本地地址）。
- **S2（`QA-TIP1016-01`，Medium，发行 lane）**：整条打包路径今天机械判据为零——删掉 `scripts/package-release.ps1:152` 的 `Assert-CleanWorkingTree` 调用，在册 886 条一条不红。修法＝给脚本自身加一道"形状门"（扫守卫调用存在、`sourceCommit` 仍来自 `git rev-parse HEAD`、缺运行库仍抛）＋为"发行可审计性"铸一条永久场景；**不需要也不许动产品代码**（原登记口径照旧）。

## 3. 认领与出判

上单认领＝本文件首行签名＋`PROJECT_PLAN.md` §4.2 `PLAT-003` 行状态词；S1/S2 由守门人在各自小刀入库时同场登记。出场三件套同 `RT-006-BEHAVIOR-OUTCOME-FROM-CLICKS` 工单 §5（§9 记录＋带分项的自跑读数＋QA 从 SHA 复采证伪）。

## 4. S1／S2 出场记录（守门人，2026-10-19 更晚，同场登记）

载体＝`.local-notes/gatekeeper-s1s2-1019`（分支 `codex/gatekeeper-s1s2-1019`）。两把先落在 `385d306` 上（`67c1475`／`6f37fb5`），随后 rebase 到线尾 `332161b` 成为 **`30a7a5a`（S1）＋`8f7dcb1`（S2）**——起因是 `2-C` 在 17:49 入库 `e2b521a` 时把 `RT-005-MANAGE` 那条 `CHANGELOG.md` 条目插在同一段（`### Added` 顶部），与本刀 S2 那条撞位；`docs/api-v1.md` 两侧改的是不同区段（记忆端点在文件头部、本刀那句在 `:566`），rebase 后两处并存。**rebase 只重写本角色自己那两把未推送的提交，`main` 与线分支一字未动。**

### S1（安全修补，`local-llama` 强制回环）

- 落点＝`src/Pry.Api/Services/ConfigurationApplicationService.cs` 的 `ValidateModelUrl` 新增两行判据（`provider == "local-llama" && !uri.IsLoopback` ⇒ `ApiValidationException("baseUrl", …)` → 400）；`openai-compatible` 那条与语音档（只 `sherpa-onnx`／`openai-compatible`）未动。
- 判据＝新 `tests/Pry.Api.Tests/LocalModelLoopbackBoundaryTests.cs` 3 例（真 SQLite＋真配置存储＋真服务；`local-llama` 保存要先过 `ValidateGguf`，夹具写带 `GGUF` 魔数的临时模型；URL 校验排在其前；响应不回显 `baseUrl`，回环那例从落盘的 `preferences.json` 读回判）。工单要求的"两条判据"（填远程必红／回环保留必过）都在，第三条是本刀自带的防过度修复对照。
- 反证三向，逐条点名且各自只打红一条：摘掉新增两行 ⇒ `A_local_llama_profile_refuses_a_remote_endpoint` 红（`Assert.Throws() Failure: No exception was thrown`）；去掉档位条件让回环要求套到所有档 ⇒ 红的是 `The_network_lane_keeps_its_own_rules_untouched`；要求回环也上 HTTPS ⇒ 红 `Every_loopback_form_of_a_local_llama_profile_still_saves`。源文件按 SHA-256 逐字节还原。
- 用户可见影响当场核为零：这道门只守 `SaveCustomModelAsync`；作者 live `preferences.json` 实读 `customModels`／`customSpeechModels` **各 0 条**，9 条 `discoveredModels` 全是 `http://127.0.0.1`（运行库发现写的，不经此校验）⇒ 无现有配置被拒。文档位＝`docs/api-v1.md:566` 那句已补三种回环写法＋400＋字段名＋"HTTPS 也不例外"。

### S2（`QA-TIP1016-01`，发行脚本形状门）

- 落点＝**零产品代码**（工单反向约束照做）：新 `tests/Pry.Api.Tests/ReleaseAuditabilityShapeTests.cs` 4 例，扫 `scripts/package-release.ps1` 文本判决三根承重点**连同次序**——守卫恰一处调用且落在 `if (-not $SkipValidation)` 之前；守卫体仍读 `status --porcelain`＋拒绝语＋`rev-parse HEAD`＋40 位校验；`sourceCommit = $commit` 且同函数有 `git … rev-parse … HEAD`；缺运行库的 `throw` 先于任何 `Copy-Item`。
- 铸新号＝`docs/eval/scenarios.json` 新增 **`PLT-015`**（Protocol／High，映射上述四例）、`suiteVersion` **1.3.1 → 1.4.0**、`docs/eval/README.md` 索引行 75 → 76 与同日的升版记录；`scripts/measure-fixed-eval.ps1` 的永久 ID 表 `PLT=14` → `15`（字节级改，CRLF 与无 BOM 保住）。
- 反证五向，逐条点名：删调用 ⇒ `Assert.Single() Failure: The collection was empty`（＋探针反向半条）；**唯一调用挪进 `-SkipValidation`** ⇒ 同两条红且错误消息为"不再是无条件的那一处调用"（这一向证明门判的是次序不是存在性，也正是本单要防的那种假绿）；写死 `sourceCommit` ⇒ manifest 那条红；缺运行库改静默 `return` ⇒ 运行库那条红；守卫体掏成 `git diff --check` ⇒ `Assert.Contains() Failure: Sub-string not found`。脚本按 SHA-256 还原后 `git diff` 对该文件零行、四例复绿。
- **本刀自己的仪器缺陷（如实登记）**：第一版用 `line.Trim() == "}"` 定界 PowerShell 函数体，而本仓函数都写在第 0 列、体内 `if` 的收尾是缩进的 ⇒ 三条判据在**未改动的真实脚本**上就红；修成只认顶格 `}`，坑写进 `FunctionBody` 的注释。另记：门第一版带 5 条 xUnit 分析器警告，构建门是 0 警告，所以改的是判据结构（谓词助手持正则、用例断言谓词、计数用 `Assert.Single`），**没有压制警告、没有放宽判据**。
- 没做也不做：**没有执行过一次打包**——`Copy-InferenceRuntime` 要 gitignored 的 `runtime/llama-server.exe`，干净载体必抛（那正是第四条判据钉的形状），所以 `0-4` 仍只能在作者那台机器上做；`PLT-015` 的 `scenarioAcceptance` 仍 `NotRun`、`automatedEvidence` 只到 `PartialPass`。`QA-TIP1016-01` 的**结案词不由本角色写**。

### 同场读数（rebase 后，基座 `332161b`）

`dotnet build Pry.slnx -c Release -t:Rebuild`＝**0 警告 0 错误**；`dotnet test Pry.slnx -c Release --no-build` **连续两次全绿、两次分项逐位相同＝911／0／2**（Client 93／App 226／Api 195／Core 399 内 397 通过＋2 条按 `outcome="NotExecuted"` 点名：`Configured_sense_voice_model_can_run_offline`、`Configured_llama_server_requires_the_child_environment_key`）；比 `2-C` 自跑的 904 多的 7 条正是本两把（Api 188→195），当场测得不是加法；rebase 前那把基座 `385d306` 上另有四次全量（同一分项结构，893／0／2）与五处脚本突变，一并留档。`measure-fixed-eval.ps1` **两个结果目录各 `exit=0`**、目录 **76 条**永久场景、`PLT-015` 四条证据同一 TRX 且 `missingTests=[]`；`verify-repository` **467** 个受跟踪文件通过；`git diff --check` 干净；跑门禁前当场核过 8 个 `dotnet.exe` 全是空闲的 MSBuild node-reuse worker（同一时刻创建、命令行 `MSBuild.dll /nodemode:1 /nodeReuse:true`），零 `testhost`、无他人主机。**在册基线仍＝886**；911 是本角色自跑，**复采与独立证伪归 QA 从 `8f7dcb1` 建干净 `--detach` 载体**。归位：与 `codex/backend-swap-recovery`（W1）、`codex/rt007-capability-plugin`（W4）同押尾段合流，本角色不在他人工作树里 `switch`／`checkout`／`stash`。无 tag、未 push。
