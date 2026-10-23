# eval-HDS004-1023｜HDS-004 叙事生命周期协调（隔离刀）执行报告

- 工单：`docs/dispatch-hds-wave16-20261023.md`（`HDS-WAVE16-1023`）
- 执行方：第十六波冷启动 lane
- 性质：动产品代码的主干刀——**隔离协调层**，不接 API/Client/App、不落生产库、不碰 `%LOCALAPPDATA%\PryCompanion`
- 本报告按批次增量落盘；读数全部当场从盘上取，不自封基线、不写状态词。

## 〇 入场与时点

- 当场 `date`：`Fri Oct 23 16:14:00 2026`（接令）；建载体时点 `16:21`。
- 主树 `git status --short --branch` 入场读数（他人名下项按工单 §〇.1 视为别人的资产，本波全程不读内容、不改、不提交、不 stash、不 clean）：

```
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

- 载体命令：`git -C C:/Users/24782/Desktop/Pry_Project worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave16-1023/carrier def0faf`
- 入场两行判据（当场实测原样）：
  - `git status --short | wc -l` ＝ **0**
  - `git rev-parse HEAD` ＝ **`def0faf5456a5ddcb121f52040ee9e551b07a382`**（＝工单基点 `def0faf` 全号；主树当时 `git rev-parse HEAD` 同为该值）

## 一 范围自证（允许／禁止文件面）

- 允许（工单 §〇.4）：`src/Pry.Core/Narrative/` 新文件、`tests/Pry.Core.Tests/` 新文件、本报告。
- 禁止（工单 §〇.5 七条反向约束）：不改 `src/Pry.Core/Narrative/` 既有十份文件的成员／条件／阈值／枚举取值；不接 API/Client/App／生产库／SQLite 仓储／`ContextBuilder.cs`；新协调器不引用旧链四件（`BehaviorPlanner`／`ReplyGenerationRenderer`／`ReplyPlanner`／`ReplyNecessity`）；数字门槛零改动且不新造；不固定返回值／不吞异常／不伪造成功；不碰许可与素材文件；零 tag／push／打包／merge／FF main。
- 交付面自证将在收尾以 `git diff --stat <基点>..HEAD -- src tests` 原文登记（见 §六）。

## 二 设计记录（判据形状先于代码）

载体新增两份文件（交付面全部为新建，零改既有文件）：

- `src/Pry.Core/Narrative/NarrativeLifecycleCoordinator.cs`（隔离协调层）
- `tests/Pry.Core.Tests/NarrativeLifecycleCoordinatorTests.cs`（判据组）

### 二.1 七项要求的落点

1. **作用域幂等**：作用域键＝`(InstanceId, ConversationId, BatchId)`（`NarrativeScopeKey`）。同键再次索要：在飞／排队相位取回同一枚任务句柄；已结算相位取回同一份结果；被取消／作废的键不重新入队，同键再索有一律经过既有 `NarratorCallBudget.Consume()` 强制点（第二次索要即抛），协调器把它映射成可点名的 `FailedByCallBudget`——额度不足是失败，不是第二次生成。
2. **实例单写者**：每实例一份 `InstanceSlot`，同一时刻至多一笔在飞；第二笔按 §二.3 规则排队。提交时先过既有 `OutboxOwnershipRegistry.IsAuthorizedWriter`：写入权不足以免提交时以**既有理由码**点名拒绝（落后代次 ⇒ `OwnershipFenced`、票在他手 ⇒ `OwnershipHeldElsewhere`、无票 ⇒ `OwnershipNotHeld`），拒绝发生在进队之前。
3. **公平调度（规则原文，逐字自源码 `PickFairLocked` 的注释与实现）**：
   > 公平调度步：按会话登记次序、从轮转游标起扫描非空队列；取到就推进游标到它的下一个会话，每个会话一次只取一枚，空会话跳过。任何已入队的会话在一轮完整轮转内必然轮到——不会被饿死。
   
   实现：`ConversationOrder` 记录会话首次入队次序；`Cursor` 指向下一个待扫描会话；`PickFairLocked` 从 `Cursor` 起环形扫描，命中非空队列即取**该队列队首**（同会话 FIFO）并把游标推进到 `(index + 1) % count`。无饿死判据：任一非空队列至多等待一轮完整轮转必被选中——由判据单元"连投三枚同会话也压不住另一会话"实测钉死（见 §五 T-fair，预期分派次序 `batch-0, batch-1, batch-4, batch-2, batch-3`）。
4. **取消传播**：同会话新批次顶掉在飞草稿（`CancelledByNewInput`）；调用方令牌取消走 `CancellationTokenSource.CreateLinkedTokenSource`＋`Token.Register`（`CancelledByHost`）。两条路径都把取消传进 `NarrativeRoundRunner.RunAsync` 的令牌；`RunAsync` 对宿主取消的既有语义是抛出 `OperationCanceledException` 且**绝不提交**，协调器沿该形状收场。
5. **无视取消的迟到结果永久无资格（只用既有出口）**：协调器终止在飞作用域时只做既有登记处的两次调用——`Release` 当前写票＋同写者同模式 `TryAcquire`（登记处自己递增代次），然后取消链路令牌。代次推进后，迟到草稿即便"正常完成"，其投递段在 `DeliveryLedger.TryAppend` 的**既有** `IsAuthorizedWriter` 代次门上吃 `OwnershipFenced`；会话删除走 `NarrativeCommitWriter.InvalidateConversation` 的既有删除代次（迟到提交 ⇒ `DeletionEpochAdvanced`，即 `NarrativeStorageContractTests.cs:403` 的协调层等价件）。协调器不自造任何失权判定；post-await 的 `Termination` 分支只把那一枚结果丢弃（`Round = null`），失权本身由上述既有门证明。
6. **删除／重配置**：`InvalidateConversation` 先落既有写者层删除代次，再作废该会话全部非已结算作用域；`ReconfigureInstance` 只作废该实例的在飞＋排队。两者都**不重新入队**——被作废的排队项 provider 调用计数保持 0。
7. **预算与三种可区分失败**：超时与预算一律从入参 `narratorTimeout`／宿主 `request` 取，源码零新数字门槛。`ProviderRejected` 按既有 `NarratorOutcomeKind` 三分：`TimedOut ⇒ FailedByTimeout`、`ProviderFailed ⇒ FailedByProvider`、`MalformedResponse ⇒ FailedByProvider`（outcome 类别在 `Round.Outcome.Kind` 上继续可点名）；超额度独立为 `FailedByCallBudget`。三者都不是产出：`Accepted` 只可能来自 `Completed`，失败路径永不映射 `CommunicationMode.Silence`。

### 二.2 实现骨架（判定次序）

单一 `object _gate` 锁守护全部状态迁移；`SubmitAsync`：`_scopes` 命中 ⇒ 回放；写权门（既有 `IsAuthorizedWriter`，用**签发代次**）⇒ 前置拒绝；同会话顶替 ⇒ 强制终止在飞；入队 ⇒ 槽空即分派。`DispatchNextLocked` 泵：槽忙即停；作废的结算跳过；分派门（既有 `IsAuthorizedWriter`，用**当前代次**）；分派时以 `request with { OwnershipEpoch = 当前代次 }` 重签（同写者链条的既有事实：代次由登记处推进，不由协调器私授）；`RunAndSettleAsync` 在调 runner 前 `Budget.Consume()`（既有唯一调用额度强制点），结算经 `TaskCompletionSource(RunContinuationsAsynchronously)`，迟到结果 `TrySetResult` 覆写不了已定局。分派以 `_ =` 弃接异步泵，锁内不等待。

### 二.3 判据组构成（13 元）

T-samekey（同键三相回放）／T-crosskey（异键不互吞，含跨实例）／T-busy（忙则排队＋单写者按实例）／T-stale（落后代次点名拒绝且零入队零调用）／T-fair（轮转次序＋无饿死）／T-supersede（新输入顶掉在飞：旧草稿作废、账上无它的段）／T-late（宿主取消＋无视取消的迟到段被既有代次门永久拒绝）／T-delete（删除作废在飞＋排队、不回炉、迟到提交过 `DeletionEpochAdvanced`）／T-reconfigure（重配置只波及本实例）／T-budget（取消后同键再索要＝`FailedByCallBudget` 且 provider 计数不动）／T-threefailures（超额度／provider 失败／坏响应／超时互不混淆）／T-silence-shape（真沉默只来自真草稿）／T-sourcescan（协调器源码对旧链四标识符零引用，扫描判据自带阳性探针）。

## 三 构建与运行读数（全部当场原样）

- 预检：每次 `dotnet` 调用前先跑 `powershell -NoProfile -ExecutionPolicy Bypass -File .local-notes/game-preflight6-1022.ps1`（主树路径执行），全程 22 次读数均为 `VERDICT=BUILD_OK`（另有 `free_physical_memory=2.9 GB` 级 WARN，非 Hold；无 SQLite disk-I/O／OOM 形态失败，无重采轮）。
- 构建收敛记录（首建 3 处病灶都在本刀新文件内，逐一定位修复）：
  1. `CS0165`（协调器 `SettleLocked` 变量作用域，协调器内部修）；
  2. 测试方法名内多打一个空格（`CS1519` 连锁 38 错）；
  3. `xUnit2029` 警告 1 条（`Assert.Empty(…Where…)` ⇒ `Assert.DoesNotContain`）。
- 最终构建读数：`dotnet build Pry.slnx -c Release` ＝ **0 个警告、0 个错误**（已用时间 00:00:01.69）。
- 载体提交：`daab642`（首交付）＋ `c4cf094`（清理轮：修复"取消续延链在锁内就地再分派未作废排队项"的真实判据缺陷＋测试夹具批次号两处）。主树未做任何 `git add`／`git commit`。
- 清理轮发现并修的实质缺陷（登记为判据之外的收获）：`ReconfigureInstance`/`InvalidateConversation` 原先按插入序终止作用域，`Link.Cancel()` 的同步续延链（锁重入）会在排队项作废之前把它分派出去——provider 被调用、判据 `CallsFor("batch-2")==0` 当场抓到（run-01 红格）。修复＝同批受害者先排队后在飞。修复后 run-02 目标跑 **13/13 全绿**（`13/0/0`，192 ms）。
- 目标跑账：`dotnet test tests/Pry.Core.Tests/…--filter FullyQualifiedName~NarrativeLifecycleCoordinatorTests` 共 **16 次**（≤20 上限内；变异轮全部零 `--no-build`），每轮独立 `trx/run-*` 目录；TRX 已整体移出载体归档于 `.local-notes/qa-wave16-1023/trx-archive/`（载体 `git status --short | wc -l` ＝ 0）。
- 全范围单采（本轮由写判据的同一只手单采）：`dotnet test Pry.slnx -c Release --logger trx`，四份 TRX `<Counters>`：
  - `Pry.Core.Tests.dll`：total 636／passed 634／failed 0／skipped 2（NotExecuted 点名：`Configured_sense_voice_model_can_run_offline`、`Configured_llama_server_requires_the_child_environment_key`——真实模型与 llama-server 密钥两格，非本刀新增，沿用既有跳过名）；
  - `Pry.Api.Tests.dll`：280／280／0／0；
  - `Pry.Client.Tests.dll`：108／108／0／0；
  - `Pry.App.Tests.dll`：233／233／0／0；
  - 合计 **1257 总／1255 过／0 败／2 跳**。与在册 1179 及同手重采 1244 的差异＝本刀新增 13 格（1244＋13＝1257，当场对账一致）。

## 四 反证表（11 字段逐轮登记）

共同字段：载体全号 `c4cf09426f7be94f4d2e7024391f193fc792a29b`（全部变异轮在其工作树上单行施加）；文件＝`src/Pry.Core/Narrative/NarrativeLifecycleCoordinator.cs`；完整跑法＝`dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~NarrativeLifecycleCoordinatorTests" --logger trx --results-directory "<载体内当轮 trx 目录>"`（预检先行、零 `--no-build`）；还原方法＝`git checkout -- src/Pry.Core/Narrative/NarrativeLifecycleCoordinator.cs`（单文件，从不 `-- .`）；改前 SHA-256（基线）＝`236bc43be479151159e131a53ba2397875e2776b35b8d02feab049f6965a6c1f`；每轮还原后复测 SHA-256 与基线逐字一致、`git diff HEAD --stat` 行数＝0（下表"还原后"列）。

| # | 判据格 | 文件:行 | 改前→改后（git diff 原样） | diff --stat | TRX 文件 | 红格全名＋消息＋锚点 | 还原后 status 行数 | 改后 SHA-256（还原前采样） | 结论 |
|---|---|---|---|---|---|---|---|---|---|
| M1 | §二.1 同键回放（在飞相位） | :260 | `if (entry.Stage is ScopeStage.Queued or ScopeStage.Running)` → `if (entry.Stage is ScopeStage.Queued)`（1 插 1 删） | 1 file, +1/−1 | run-03 `…_17_01_47.trx` | 恰 1 红：`Same_scope_key_replays_the_same_handle_and_never_generates_twice`，`System.NullReferenceException`（在飞项落入已结算读取路径） | 0 | `123503b97ab6ce5e…a0686125` | 单格干净红 ✓ |
| M2 | §二.1 异键不互吞 | :255 | `new(request.InstanceId, request.ConversationId, request.BatchId)` → 末位 `request.ConversationId`（1 插 1 删） | 1 file, +1/−1 | run-04 `…_17_02_07.trx` | 主红格：`Different_keys_keep_their_own_results_and_never_swallow_each_other`，`Expected: NarrativeScopeKey{…batch-1} Actual: …`；另 9 格连带红（13:10 红）——作用域键是全组身份前提，`NarrativeScopeKey` 计算行无更窄载体可分离，归因如实登记 | 0 | `bac1ec207af50cbc…f0a7412` | 归因复红（共享身份线） |
| M3 | §二.2 忙实例排队／在飞标记 | :402 | `slot.Running = entry` → `slot.Running = null`（1 插 1 删） | 1 file, +1/−1 | run-05 `…_17_16_43.trx` | 主红格：`Busy_instance_queues_the_second_scope_and_another_instance_still_runs`（`WaitRunning` 自旋耗尽消息"在飞标记没挂上"）；连带 7 红（13:8 红）——在飞标记同为排队/顶替/取消格的前提线，归因如实登记 | 0 | `886278a10f70b578…e6569d11` | 归因复红（共享单写者线） |
| M4 | §二.2 落后代次点名拒绝 | :146-147 | 提交门实参 `request.OwnershipEpoch` → `_ownership.CurrentEpoch(request.InstanceId)`（1 插 2 删） | 1 file, +2/−1 | run-06 `…_17_19_12.trx` | 恰 1 红：`Request_below_the_current_epoch_is_named_and_never_queued`，`Expected: RejectedByAuthority Actual: Completed` | 0 | `b00c267700c31955…31a39c5d` | 单格干净红 ✓ |
| M5 | §二.3 公平轮转（无饿死） | :425 | 删行 `slot.Cursor = (index + 1) % slot.ConversationOrder.Count;`（1 删） | 1 file, −1 | run-07 `…_17_19_41.trx` | 恰 1 红：`Fair_rotation_dispatches_by_conversation_round_robin_not_arrival_order`，`WaitRunning` 锚 `NarrativeScopeKey{…room-1, batch-2}` 1000 次自旋未上在飞位（游标停 0 ⇒ room-1 吃光队列、room-2 饿死） | 0 | `daca091a72c1bac7…6fedb7f8` | 单格干净红 ✓ |
| M6 | §二.4 新输入顶替 | :157 | `busy.Key.BatchId != key.BatchId` → `== key.BatchId`（1 插 1 删） | 1 file, +1/−1 | run-08 `…_17_20_29.trx` | 恰 1 红：`New_batch_in_the_same_conversation_supersedes_and_voids_the_running_draft`，`Expected: CancelledByNewInput Actual: Completed` | 0 | `a6997ce900384a68…da0f10aa` | 单格干净红 ✓ |
| M7 | §二.4 无视取消的迟到结果永久无资格 | :474 | `if (entry.Termination is { } lateIntent)` → `… lateIntent && false)`（1 插 1 删） | 1 file, +1/−1 | run-09 `…_17_20_53.trx` | 恰 1 红：`Late_result_from_a_provider_ignoring_cancellation_never_lands_in_history`，`Expected: CancelledByHost Actual: Completed` | 0 | `ad94149443e9a2f4…647d76fa` | 单格干净红 ✓ |
| M8 | §二.5 会话删除作废面 | :198 | `.Where(e => string.Equals(e.Key.ConversationId, conversationId…)` → 谓词取反 `!string.Equals(…)`（1 插 1 删） | 1 file, +1/−1 | run-10 `…_17_21_10.trx` | 恰 1 红：`Conversation_deletion_voids_inflight_and_queued_without_requeue`，`Expected: 0 Actual: 2`（`QueuedScopeCount` 锚） | 0 | `b260a0e9f4b23efe…f4c68cef` | 单格干净红 ✓ |
| M9 | §二.5 重配置只波及本实例 | :221 | `.Where(e => e.Key.InstanceId.Equals(instanceId)…)` → `!e.Key.InstanceId.Equals(…)`（1 插 1 删） | 1 file, +1/−1 | run-11 `…_17_21_25.trx` | 恰 1 红：`Reconfiguring_one_instance_voids_its_scopes_without_touching_the_other`，`Expected: InvalidatedByReconfigure Actual: Completed` | 0 | `437106bfabc07251…00a6bec4`（注：437106bfabc07251c7d8d810ad174b0515df033888a7464e24c1e9d1400a6bec） | 单格干净红 ✓ |
| M10 | §二.6 超额度经既有强制点 | :272 | `if (entry.Budget.CallCount == 0)` → `!= 0`（1 插 1 删） | 1 file, +1/−1 | run-12 `…_17_21_40.trx` | 恰 1 红：`Resubmitting_a_cancelled_scope_fails_at_the_existing_call_budget`，`Expected: FailedByCallBudget Actual: CancelledByHost` | 0 | `ba9e5a7487674b50…009ae484` | 单格干净红 ✓ |
| M11 | §二.6/7 provider 失败不映射超时 | :488 | `NarratorOutcomeKind.ProviderFailed => …FailedByProvider` → `=> …FailedByTimeout`（1 插 1 删） | 1 file, +1/−1 | run-13 `…_17_21_55.trx` | 恰 1 红：`Over_quota_provider_failure_malformed_and_timeout_are_distinguishable_and_never_silence`，`Expected: FailedByProvider Actual: FailedByTimeout` | 0 | `d440fb91b5f8288e…f3f83429` | 单格干净红 ✓ |
| M12 | §二.7 坏响应不是产出 | :489 | `NarratorOutcomeKind.MalformedResponse => …FailedByProvider` → `=> …Completed`（1 插 1 删） | 1 file, +1/−1 | run-14 `…_17_22_09.trx` | 2 红：`Silence_is_only_ever_produced_by_a_real_draft_never_by_a_failure` 与同判据族 `Over_quota_…_never_silence`，均 `Expected: FailedByProvider Actual: Completed`——两格共读同一坏响应分类线，归因如实登记 | 0 | `ddc6c223602e8d10…8e24e83` | 归因复红（共享分类线） |
| M13 | §二.8 零旧链引用门会咬 | :1 后插入 | 新增注释行 `// M13 反证变异样本行：旧链 ReplyPlanner 字面量…`（2 插，含空行） | 1 file, +2 | run-15 `…_17_22_25.trx` | 恰 1 红：`Coordinator_source_carries_no_legacy_chain_references_and_the_probe_profits_the_gate_bites`（全名 …and_the_probe_proves_the_gate_bites），`Assert.DoesNotContain() Failure: Sub-string found … Found: "ReplyPlanner"`（pos 49） | 0 | `1a263d424845060b…d8ed9ad1` | 单格干净红 ✓ |

合并计数（不可达红的布尔门与兜底）：`ResubmitLocked` 的"额度第二次索要仍放行"兜底抛出、`AdvanceOwnershipEpochLocked` 的释放／重取得失败抛出、映射 `switch` 的"ProviderRejected 却带不出 outcome 类别"抛出——三条都以既有强制点或原子性前提为条件（`Budget.Consume` 二次必抛、`Release+TryAcquire` 同写者必成、runner 必带 outcome），本轮无单行窄变异可独立触红，按工单口径计入布尔门兜底合计而非"结构上永远够不着"。M2/M3/M12 的多红是共享判定线（作用域键、在飞标记、坏响应分类）的如实归因，各登记主格＋连带数。

## 五 交付面自证（原文）

```
$ git diff --stat def0faf..HEAD -- src tests   #（在载体内）
 .../Narrative/NarrativeLifecycleCoordinator.cs     | 516 +++++++++++++++
 .../NarrativeLifecycleCoordinatorTests.cs          | 732 +++++++++++++++++++++
 2 files changed, 1248 insertions(+)
```

恰两份新建文件、零改既有文件——既有十份 Narrative 契约／原型文件的成员、条件、阈值、枚举取值一字未动（`def0faf..HEAD` 面不含任何既有文件即证）。数字门槛零新增：协调器全部时长来自入参 `narratorTimeout`，无新常量阈值；测试夹具里的等待自旋上限属测试观测卫生，不进入产品判定。

## 六 仍未验证（一律未验证，不写状态词）

- 真实本地小模型对照：0 读数（沿用既有两格 NotExecuted 跳过名，见 §三）。
- 生产接线：协调器无任何 API/Client/App 调用点——`SubmitAsync` 在生产路径零被调，未验证。
- UI 可见行为：0 读数，未验证。
- 生产库／`%LOCALAPPDATA%\PryCompanion`：本波未打开任何 SQLite 路径、未触碰该目录；对其"未受影响"的正面证明＝零接触（未做运行时对账）。
- 延迟意图到期再叙事（HDS-007 轴）：本刀只把 `Deferred` 轮作为 `Completed` 结果携带，到期触发未实现、未验证。
- §四所列三条兜底抛出线：反证未单独触红（合并计数登记）。
- 宿主重签发（`request with { OwnershipEpoch = 当前代次 }`）对下游契约的长期影响：只在本判据组内闭环，跨波对照未验证。

## 七 归位提示（给门禁手）

- 载体：`.local-notes/qa-wave16-1023/carrier`，detached 基点 `def0faf5456a5ddcb121f52040ee9e551b07a382`，HEAD `c4cf09426f7be94f4d2e7024391f193fc792a29b`，其上两提交：`daab642`（feat 首交付）、`c4cf094`（fix 清理轮＋夹具）。工作树 `git status --short` ＝ 0 行。
- 新文件与指纹：
  - `src/Pry.Core/Narrative/NarrativeLifecycleCoordinator.cs`，516 行，SHA-256 `236bc43be479151159e131a53ba2397875e2776b35b8d02feab049f6965a6c1f`
  - `tests/Pry.Core.Tests/NarrativeLifecycleCoordinatorTests.cs`，732 行，SHA-256 `e84109c9b35831979bd19e123f9741c985ba64540ae810f67dcfeb5ae38b3f18`
- 证据归档：`.local-notes/qa-wave16-1023/trx-archive/run-00…run-16`（目标跑 16 次＋全范围单采 1 次；`run-00` 为无 TRX 的首次调试轮，空目录留名）。
- 主树只新增了本报告文件（`docs/eval/reports/2026-10-23-hds004-lifecycle-coordinator.md`），未 add／未 commit；他人名下项一字未碰。

## 八 守门人归位与换手复采（2026-10-23 17:4x，原样读数）

- **归位通道**：主树共享脏树里不 `cherry-pick`（索引上有他人两枚暂存项，`cherry-pick` 会把它们消费进提交）。改走隔离载体合流＝`git worktree add --detach .local-notes/qa-wave1617-merge-1023/carrier 333b7d6` 上依次 `cherry-pick daab642 c4cf094`（本刀两枚）＋`94accd8`（第十七波一枚），合流线尾＝`91b2e90`（本刀映射为 `6cc1501`＋`61001a4`）；`git diff --stat 333b7d6..91b2e90` ＝ **5 files changed, 3290 insertions(+), 0 deletions**、零改既有文件。主树只把这两份文件按字节铺进去后以显式路径提交＝`e8a842a`（`git show --stat`＝5 枚文件，与声明路径数一致；他人两枚 `M ` 暂存项一字未动、索引 blob 前后同值）。
- **字节复核**（当场 `sha256sum`，主树铺完后）＝§七 自报值逐位相同：协调器 `236bc43be479151159e131a53ba2397875e2776b35b8d02feab049f6965a6c1f`、判据组 `e84109c9b35831979bd19e123f9741c985ba64540ae810f67dcfeb5ae38b3f18`。合流树与归位后主树在 `src`／`tests`／`scripts`／`Pry.slnx`／`Directory.Build.props` 五面 `git diff --stat` 为空。
- **换手全量（守门人跑，干净 detached 合流副本 `91b2e90`）**：`dotnet build Pry.slnx -c Release` ＝ **0 警告 0 错误**（00:00:09.95）；`dotnet test Pry.slnx -c Release --no-build --results-directory "…\qa-wave1617-merge-1023\run1-full" --logger trx` ＝ **总 1275／passed 1273／0 红／2 条跳过**。四份 TRX 逐份 `<Counters>`（各文件独立、无同名覆盖）：Client `total=108 executed=108 passed=108 failed=0`、App `233/233/233/0`、Api `280/280/280/0`、Core `total=654 executed=652 passed=652 failed=0`；两条跳过按 `outcome="NotExecuted"` 点名＝`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`（`notExecuted` 计数器仍为 0，不看）。
- **对账**：`def0faf` 的合流复采 **1244** ＋ 本刀 **13** ＋ 第十七波 **18** ＝ **1275**；Core 一条线 `623 → 636（本刀单采）→ 641（十七波单采）→ 654（合流）` 三者相容。`scripts/measure-fixed-eval.ps1 -ResultsDirectory <本轮目录>` `exit=0`、catalog **76**、打印 `Passed=1273; Failed=0; Other=2.`；`git diff --check` 干净。
- **预算与预检**：本轮 `dotnet` 调用 **2** 次（构建 1＋全量 1，合并两波共用一轮）；每次起 `dotnet` 前跑 `.local-notes/game-preflight6-1022.ps1`，三次读数全 `VERDICT=BUILD_OK`（其中一次前台窗是 `PickerHost`／类名 `Shell_SystemDialog`、`covers=False borderless=False`，不是全屏应用，按 v6 判据放行；`free_memory_gb` 12.7／11.6，无 SQLite disk-I/O／OOM 形态失败、无重采轮）。
- **换手覆盖度如实分级**：本刀 13 格的判据由 lane 写、守门人一字未改，故本轮构成这 13 格的**第二换手绿证**；§四 的 M2／M3／M12 三枚"共享判定线归因复红"按 lane 的登记接受，不要求重做；§四 末尾三条兜底抛出线（额度二次索要仍放行的兜底、释放／重取得失败抛出、`ProviderRejected` 无 outcome 类别的抛出）**仍无单枚实测红**＝继续挂账，不写成"结构上够不着"；§六 六项（真实模型对照、生产接线、UI、生产库对账、到期再叙事、跨波长程影响）照原样为**未验证**。
- **不做的三件**：未 `tag`、未 `push`、未执行过一次打包；`licenses/**`／`ASSETS_LICENSE.md`／`FAN_CONTENT_POLICY.md` 一字未碰；数字门槛零改动（本轮 diff 面不含任何既有文件即证）。结案词与状态词按 §4.2 口径只写"已实现待验／归位"。
