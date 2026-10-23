# 2026-10-23｜HDS-010 第二十三波：回合穿线组合件＋端到端判据＋第二十波六格换人复采（HDS-WAVE23-1023）

- 工单：`docs/dispatch-hds-wave23-20261023.md`（`HDS-WAVE23-1023`），本刀按其逐字执行。
- 性质：动产品代码的整合刀。新建一枚组合件＋一枚新判据文件＋本报告；不切生产默认路径、不动旧链一字、不碰界面、不落生产库。
- 诚实前提：**本刀只在测试宿主里把链穿了一次**。"新链已可跑／旧链可删／不进包已做到"一律不是本刀结论；组合件在生产里零调用方（未验证清单见 §五）。

## 〇 入场读数（当场实量，非引用工单数字）

### 〇.1 主树入场（创建载体之前）

```
$ date
Fri Oct 23 18:42:32 2026

$ git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD
04a1f1bcf49133a76c3e012148afed9dba896ee2

$ git -C C:/Users/24782/Desktop/Pry_Project rev-parse codex/ui-main-window-redesign
04a1f1bcf49133a76c3e012148afed9dba896ee2

$ git -C C:/Users/24782/Desktop/Pry_Project status --short --branch
## codex/ui-main-window-redesign
M  docs/dispatch-tip1020-02-memory-group-judgment.md
M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs
?? .qoder/
?? docs/character-learning-safety.patch
?? docs/eval/reports/2026-10-23-hds003-005-cell-evidence-audit.md
?? docs/research/HDS_INTERLUDE.md
?? docs/research/HDS_QQ_RESEARCH_INDEX.md
?? docs/research/QQ_BRIDGE.md
```

- 两枚 `M ` 暂存项与全部 `??` ＝他人资产：未读内容、未改、未提交；主树 `.local-notes` 副本与那份 003-005 审计报告正被另一把在飞的审计刀使用，本刀一字未碰。
- 主树全程零 `commit/cherry-pick/merge/tag/push/FF main`；一切提交只发生在载体里。

### 〇.2 落后核格与载体

```
$ git -C C:/Users/24782/Desktop/Pry_Project rev-list --count HEAD..04a1f1bcf49133a76c3e012148afed9dba896ee2
0
```

- 落后 0 格，无需 `merge --ff-only`（当场线尾＝HEAD＝`04a1f1bc…ee2`）。

```
$ git -C C:/Users/24782/Desktop/Pry_Project worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave23-1023/carrier 04a1f1bcf49133a76c3e012148afed9dba896ee2
（创建成功）

$ git -C .../carrier rev-parse HEAD
04a1f1bcf49133a76c3e012148afed9dba896ee2

$ git -C .../carrier status --short | wc -l
0
```

### 〇.3 在册门禁基线（引用第二十波归位后的线尾副本，本刀不动基线、不自封）

- 1321／0／2（Client 108／App 233／Api 280／Core 700、executed 698）。出口对账＝四份 TRX 当场逐份求和，见 §四。

### 〇.4 必读五份与两份依据报告

已全文读：`docs/plans/README.md`、`docs/plans/PROJECT_PLAN.md`（§4.2 HDS-003/004/005/010 四行＋§9 最新三条 entry：18:4x wave23 出闸、18:3x wave20 收口＋基线 1321/0/2、18:1x wave21）、`docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md`、`docs/agent-engineering-standards.md`（交付按 §10 四项）、`docs/legacy-chain-deprecation.md`；正面依据两份报告（第二十波 `2026-10-23-hds010-gap-parity-prototype.md` 含守门人 §六、第十九波 `2026-10-23-hds010-legacy-chain-disposition.md` 表 C 片 4／表 D）亦已读。

### 〇.5 本刀新建文件（提交前后各测一次 SHA-256；提交＝载体内唯一一笔）

| 文件 | 行数 | SHA-256 |
|---|---|---|
| `src/Pry.Core/Narrative/NarrativeTurnPipeline.cs`（组合件，工单建议名，按建议采用） | 311 | `e3047b7cf6e4154e79c2e803e2d8c10cdc685ac4d348aa0988cdf1b2de8f1bb7` |
| `tests/Pry.Core.Tests/NarrativeEndToEndPipelinePrototypeTests.cs`（判据 12 格，全普通 [Fact]，零 [Theory]／[InlineData]／MemberData／抽象基类） | 669 | `39725f06238c2a3ea248c9708eab02a8bc91f52f95117e2ffe17dd0bbe387843` |

- 盘上扫描（本刀自测）：组合件源码零出现旧主链四标识；源码数字字面量仅枚举序号与注释段号，无任何门槛数值——批封顶／节奏／超时／水位一律必填入参。
- 第二十波五枚被守护文件的载体副本 SHA-256（与第二十波报告在册值逐字一致，作为换人复采的还原对照）：
  - `NarrativeInputBuffer.cs` `1e40dfb3f937d894b7bf19d1e20f66912da33df5a112b17baadfe18d97094067`（188 行）
  - `NarrativeInterruption.cs` `fca7209a7163846c3cf60cea6f87cd0ab15d9f7e40b5c6baff0bfae553ec7fdf`（94 行）
  - `NarrativeInterruptClassifier.cs` `9f1cfed2a62b3a9187ab48bcd9e9b1a032b988c8d2f67e11796574808961ce13`（62 行）
  - `NarrativeBehaviorExpressionMapping.cs` `2bbae3d9922b20db046746abc3edca4d9636fb7bdb1ffa2e8a0fbb9546cc2a61`（104 行）
  - `NarrativeReplyPacing.cs` `f0e826dfb35227903e68b835584a7589813250b25d70f3838a4e6029407e3fd9`（104 行）

### 〇.6 预算与跑法登记

- 解决方案级构建 ≤1；全量 ≤1；定向 ≤16（新门反证 ≤10 含基线 1 轮＋换人复采 ≤6）。
- 固定写法 `dotnet test <工程或解决方案> -c Release [--no-build] --results-directory "<绝对路径>" --logger trx`；每轮独立结果目录；跑完先数 `*.trx` 再读 `<Counters>`；跳过只认 `<UnitTestResult outcome="NotExecuted">`。
- 每次 `dotnet` 前在主树跑 `.local-notes/game-preflight6-1022.ps1`，整段输出贴进本报告对应轮次；只认 `VERDICT=BUILD_OK`。

（本节以下按批次追加：①构建读数 ②全量读数与对账 ③衔接表＋端到端 ④新门反证逐轮 ⑤第二十波换人复采逐轮 ⑥出口读数 ⑦未验证清单。）

## 一 构建读数

### 一轮·解决方案级（预算 1/1）

预检（第一次 `dotnet` 前，主树运行，整段）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: msedge pid=5424 class=Chrome_WidgetWin_1 style=0x17CF0000 rect=-1928,96,1936x1048 screen=1920x1080 covers=True borderless=False title=?????????_????_bilibili ??? 3 ??? - ?? - Microso
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=5% used=8545MB
free_memory_gb=9.9
VERDICT=BUILD_OK
```

`dotnet build Pry.slnx -c Release`（载体内）结果：**1 个警告 0 个错误**——警告是我自己新建判据文件第 111 行的 `Assert.Equal(1, …Count)`（xUnit2013，建议 Assert.Single）。出口读数要求构建 0 警告，所以该格改用 `Assert.Single` 返回的元素继续断言（只动我自己的新文件，语义不变：条数恰为 1 且取该项 Accepted），随后**工程级**增量构建 `tests/Pry.Core.Tests` 一次刷新 DLL：**0 警告 0 错误**（工程级增量不计解决方案级构建，与历波突变轮同形）。这笔修正＝载体第二笔提交（改动 2 行 ±），发生在任何反证轮之前，不作废任何红证据。

- 修正后判据文件：669 行，SHA-256 `5cbb539c3dad13e42d19adb0ccdb2569ecc3fde29103b376b23d8ecd06760b81`（§〇.5 表中前值 `39725f06…7843` 为此前版本，登记于此不作覆盖）。
- 组合件源码未再改动：311 行，SHA-256 `e3047b7cf6e4154e79c2e803e2d8c10cdc685ac4d348aa0988cdf1b2de8f1bb7`。
- 载体线尾改为 `f8404acbce914146670bb724d63dc8e3764f22c4`（共两笔提交，均只含本刀新文件）。

预检（第二次 `dotnet`＝工程级增量构建之前，主树运行，整段）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: ChatGPT pid=10784 class=Chrome_WidgetWin_1 style=0x14C70000 rect=427,135,1827x1109 screen=2560x1440 covers=False borderless=False title=ChatGPT
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=9% used=8578MB
free_memory_gb=8.7
VERDICT=BUILD_OK
```

### 二轮·新判据基线（定向预算 1/16）

预检（第三次 `dotnet` 之前，主树运行，整段）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: ChatGPT pid=10784 class=Chrome_WidgetWin_1 style=0x14C70000 rect=427,135,1827x1109 screen=2560x1440 covers=False borderless=False title=ChatGPT
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=11% used=8549MB
free_memory_gb=9
VERDICT=BUILD_OK
```

跑法整行（载体内）：

```
cd "C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave23-1023/carrier" && dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --no-build --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave23-1023/results/w23-run01-baseline" --logger trx --filter "FullyQualifiedName~NarrativeEndToEndPipelinePrototypeTests"
```

跑完先数 `*.trx`：目录 `w23-run01-baseline` ＝ 1 份（`24782_PHYTHLINK_2026-10-23_19_03_50.trx`）。`<Counters>` 原文：

```
<Counters total="12" executed="12" passed="12" failed="0" error="0" timeout="0" aborted="0" inconclusive="0" passedButRunAborted="0" notRunnable="0" notExecuted="0" disconnected="0" warning="0" completed="0" inProgress="0" pending="0" />
```

判据文件里 12 枚格全部 `[Fact]`（无 `[Theory]`／`[InlineData]`／`MemberData`／无抽象基类派生），静态格数与 TRX `total` 相等＝12，本轮零红。

## 二 全量读数与对账

预检（第四次 `dotnet` 之前，主树运行，整段）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=978,211,884x738 screen=2560x1440 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=10% used=8572MB
free_memory_gb=9
VERDICT=BUILD_OK
```

跑法整行（载体内，解决方案级，唯一一笔全量）：

```
cd "C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave23-1023/carrier" && dotnet test Pry.slnx -c Release --no-build --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave23-1023/results/w23-run02-full" --logger trx
```

跑完先数 `*.trx`：目录 `w23-run02-full` ＝ 4 份。逐份按控制台归属行（`结果文件: … - Pry.X.Tests.dll (net10.0)`）对应到工程，`<Counters>` 原文逐份贴：

1. `24782_PHYTHLINK_2026-10-23_19_04_05.trx` ＝ Pry.Client.Tests.dll：
```
<Counters total="108" executed="108" passed="108" failed="0" error="0" timeout="0" aborted="0" inconclusive="0" passedButRunAborted="0" notRunnable="0" notExecuted="0" disconnected="0" warning="0" completed="0" inProgress="0" pending="0" />
```
2. `24782_PHYTHLINK_2026-10-23_19_04_07.trx` ＝ Pry.App.Tests.dll：
```
<Counters total="233" executed="233" passed="233" failed="0" error="0" timeout="0" aborted="0" inconclusive="0" passedButRunAborted="0" notRunnable="0" notExecuted="0" disconnected="0" warning="0" completed="0" inProgress="0" pending="0" />
```
3. `24782_PHYTHLINK_2026-10-23_19_04_05[1].trx` ＝ Pry.Api.Tests.dll：
```
<Counters total="280" executed="280" passed="280" failed="0" error="0" timeout="0" aborted="0" inconclusive="0" passedButRunAborted="0" notRunnable="0" notExecuted="0" disconnected="0" warning="0" completed="0" inProgress="0" pending="0" />
```
4. `24782_PHYTHLINK_2026-10-23_19_04_04.trx` ＝ Pry.Core.Tests.dll：
```
<Counters total="712" executed="710" passed="710" failed="0" error="0" timeout="0" aborted="0" inconclusive="0" passedButRunAborted="0" notRunnable="0" notExecuted="0" disconnected="0" warning="0" completed="0" inProgress="0" pending="0" />
```

两条跳过按 `<UnitTestResult outcome="NotExecuted">` 点名（都在 Pry.Core.Tests.dll 那份＝`19_04_04.trx`；`notExecuted` 计数器为 0，与工单口径一致，不看）：

- 第 576 行：`testName="Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline"` outcome="NotExecuted"（控制台 `[SKIP]`）
- 第 714 行：`testName="Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key"` outcome="NotExecuted"（控制台 `[SKIP]`）

当场求和（只由上面那四份原始 `<Counters>` 相加，不心算不预写）：

- total：108 ＋ 233 ＋ 280 ＋ 712 ＝ **1333**
- executed：108 ＋ 233 ＋ 280 ＋ 710 ＝ **1331**
- passed：108 ＋ 233 ＋ 280 ＋ 710 ＝ **1331**；failed：0 ＋ 0 ＋ 0 ＋ 0 ＝ **0**；跳过（total − executed）：0 ＋ 0 ＋ 0 ＋ 2 ＝ **2**

与在册 **1321／0／2（Client 108／App 233／Api 280／Core 700、executed 698）** 对账：

- 1333 − 1321 ＝ **＋12**；逐份找差：Client 108＝108（差 0）、App 233＝233（差 0）、Api 280＝280（差 0）、Core 712 vs 700（**差 12，全在 `19_04_04.trx` 这一份**）。
- 差的 12 格逐一点名＝本刀新建判据文件 `tests/Pry.Core.Tests/NarrativeEndToEndPipelinePrototypeTests.cs` 的 12 枚 `[Fact]`（第 63／172／187／203／229／254／283／305／341／367／391／429 行各一枚），全部 executed、passed，不折叠进任何既有格。
- executed：1331 − 1319 ＝ ＋12（同 12 格）；failed 两值同为 0；跳过 2＝2，且两份跳过格名与在册同格（`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`）。
- 在册基线的改写权在守门人那一手，本节只报等式与差值，本刀不自封。

## 三 衔接表（上一段输出的哪个成员 → 下一段入参的哪个参数）

组合件＝`src/Pry.Core/Narrative/NarrativeTurnPipeline.cs`（311 行，`NarrativeTurnSpec` 必填入参／`NarrativeTurnTrace` 逐段输出／`RunTurnAsync` 编排）。"停段"＝该段按既有拒绝出口返回后，后面各段一律不调用，`NarrativeTurnTrace` 对应成员保持默认空值。

| 段 | 用的既有件 | 上一段输出成员 → 下一段入参（盘上真实数据流） | 停段 |
|---|---|---|---|
| 1 攒批 | `NarrativeInputBuffer.Append/TryCloseDueAt/ForceClose/FindSealed/TryTakeSendableTruth` | `NarratedInputBatch.ConversationId` → `NarrativeRequest.ConversationId`；`NarratedInputBatch.BatchId` → `NarrativeRequest.BatchId` 与 `TryTakeSendableTruth(batch.BatchId)`（同一批唯一可发送真相）；`NarratedInputBatch.OrderedSeqs` → `NarrativeRequest.Inputs`（按序号回取 `spec.Inputs` 里的 `InputRef`）。封顶条件（`MaxBatchItems`／`MaxBatchHold`）与到达时点、封批时点全部由入参传入，源码无常量 | 领不到封批 → `BatchNotSealed`；真相已领 → `SendableTruthAlreadyClaimed` |
| 1→4／1→5 | `NarrativeContextCompiler`／`NarrativeRoundRunner` | `trace.AssembledRequest`（＝`NarrativeRequest`）→ `Compile(request, spec.Payloads)` 的第 1 参；→ `RunAsync(request, payloads, narratorTimeout, now, ct)` 的第 1 参；→ `TryCommitAtomicAsync(request, commit, …)` 的第 1 参 | — |
| 2 分类 | `NarrativeInterruptClassifier.Classify` | `InterruptClass` → 编排条件 `klass == InterruptClass.NewTopic` 决定是否调用段 3；`Undeterminable`／`Continuation` 不折叠、不触发，`trace.Interruption` 保持 null | — |
| 2→4 | — | **接不上，押接线刀**：`NarrativeRequest` 与 `NarrativeContextPayloads` 都没有分类／理由成员入口（本刀禁改 `NarrativeDocuments.cs`／`NarrativeContextCompiler.cs`），分类结论对链的作用只体现在"是否触发打断"这一控制流，没有数据口 | — |
| 3 打断 | `NarrativeInterruption.Apply` ＋ `DeliveryLedger.TryMarkCancelled` | `InterruptionOutcome.Cancelled` 每项的 `CommitId`／`ActionId`／`SegmentIndex` → `new DeliverySegmentKey(…)` → `TryMarkCancelled(key, spec.InterruptionCancelReason, spec.OwnershipEpoch, _writerId)` 的第 1 参；返回的 `SegmentOutcome` 逐段留档在 `trace.LedgerCancellations`。已送达段由既有打断件按 `IsTerminal` 扣住（withheld），账本一字未改 | 打断件本身拒绝（无候选／理由不可点名）不中断回合：本件不代拒 |
| 3→4 | — | **接不上，押接线刀**：作废结果没有下一段的参数口（真实链上要靠宿主下一回合的实例快照／水位体现，不在本组合件面） | — |
| 4 编译 | `NarrativeContextCompiler.Compile` | `NarrativeContextAssembly.Accepted` → 停段门；`spec.Payloads`（`NarrativeContextPayloads` 同一实例）→ 段 5 `RunAsync` 的第 2 参 `payloads`。**装配产物（提示文本／水位／理由码）本身没有段 5 参数口**——既有 `RunAsync` 签名只吃 `request＋payloads＋时限＋时点`，它内部自行重装配并校新鲜度；这一格登记为**接不上，押接线刀** | 拒绝 → `ContextRejected`（一次模型调用都不发生） |
| 5 叙事与校验 | `NarrativeRoundRunner.RunAsync`（内含 `NarrativeDraftValidator`） | `NarrativeRoundResult.Accepted` → 停段门；`Draft`／`Actions`／`request` → `spec.CommitFactory(round, request)`（见下行）；`round.HistoryEntries` → `TryCommitAtomicAsync(…, pendingSegments, …)` 的第 3 参；拒绝理由经 `trace.Detail`（`round.Detail ?? round.Disposition`）可点名 | 拒绝 → `RoundRejected`，段 6／7／8 不调用 |
| 5→6 的口子 | — | **接不上，押接线刀（本刀最大的那一格）**：盘上唯一的"草稿→提交"构造＝`NarrativeRoundRunner.BuildCommit`，为 private，且工单 §〇.6.1 禁改既有文件。本刀不复制它的判定，改为宿主注入 `Func<NarrativeRoundResult, NarrativeRequest, NarrativeCommit> CommitFactory`；测试宿主里由 `HostCommitFactory` 给出，`ContentHash` 用字面量占位（真实哈希该由宿主计算，登记为待接线），组合件只把它产出的提交喂给落库面 | `Store` 在场而 `CommitFactory` 缺失 → 抛 `ArgumentNullException`（不静默跳过） |
| 6 原子提交 | `Storage/NarrativeSqliteStore.TryCommitAtomicAsync` ＋ `ListPendingSegmentsAsync`（临时目录里的真 SQLite 文件；`NarrativeCommitWriter` 的门与之一致，本刀用落库面本体，内存桩不作数） | `commit.CommitId` → `ListPendingSegmentsAsync(commitId, ct)`；`ConditionalCommitOutcome.Accepted` → 停段门；`Reason`／`Detail` → `trace.Detail` 可点名 | 拒收 → `CommitRejected`，段 7／8 不调用 |
| 6→7 | `NarrativeBehaviorExpressionMapping.Map` | **接不上，押接线刀**：`Map(BehaviorCue)` 只吃行为信号，没有提交／落库成员入参。本刀能钉的只有控制流：提交被拒则段 7 不执行（`trace.Expression` 保持 null） | — |
| 6→8 | `NarrativeReplyPacing.PlanSendTimes` | `ListPendingSegmentsAsync` 从库里读回的 `DeliverySegment` 行的 `Text.Length` → `counts` → `PlanSendTimes(startAt, counts, pacing)` 的第 2 参（`startAt`／`pacing` 均为入参）；这一段是真数据流：段 8 的字符数取自重开连接后的库中行 | — |
| 7 表达换形 | `NarrativeBehaviorExpressionMapping.Map` | `BehaviorExpressionOutcome.Result` → 编排门 `== BehaviorMappingResult.Mapped` 才排可发时刻；缺决策（`NoBehaviorDecision`）与场景挡（`BlockedByScene`）不默认放行，`SceneBlockCode` 理由码原样透传在 `trace.Expression` | 非 Mapped → 段 8 不排时刻（`trace.SendTimes` 保持 null） |
| 7→8 的数据口 | — | **接不上，押接线刀**：`Gear`／挡清理由码在既有节奏件 `ReplyPacingInput` 里没有入参位 | — |
| 8 节奏 | `NarrativeReplyPacing.PlanSendTimes` | `IReadOnlyList<DateTimeOffset> sendTimes` → `trace.SendTimes`（只给可发时刻，不给内容；单调性由既有件守） | — |
| 8→下游发送 | — | **接不上，押接线刀**：`sendTimes` 在盘上没有消费者——发送侧属旧链与宿主，本刀不接端点／界面／生产路径 | — |

新链不 `using`／不构造／不转发 `BehaviorPlanner`／`ReplyGenerationRenderer`／`ReplyPlanner`／`ReplyNecessity`：由第 12 枚源码扫描格（判据文件第 429 行）钉住，带阳性探针（临时写一枚含 `var legacy = new ReplyPlanner();` 的 `.cs` 让扫描器读到，探针只证明"扫描器看得见"，门带不带牙齿靠 §四 的 G6 单枚突变）。

端到端格（判据文件第 63 行 `Turn_one_to_eight_threads_the_sealed_batch_into_a_committed_real_sqlite_turn_with_paced_sends`）：1→8 一次走完，段 6 落真临时库并以新连接读回（`narrative_entries`＝1、`narrative_outbox`＝3、实例 `StoryVersion`＝1／`ProcessedThroughSeq`＝103／`AppliedCommitCount`＝1／`LastCommitId` 对得上、`ListPendingSegmentsAsync` 读回 3 段），段 8 的可发时刻逐段断言；每段另各一枚衔接格（第 172／187／203／229／254／283／305／341／367／391 行分别守 1→停、1→2、2→3、3、4→5、6→8、7→8、7、6→停）。

## 四 新落门反证（单枚·单轮·清洁红，逐轮十一项齐）

被守护文件＝本刀新文件 `src/Pry.Core/Narrative/NarrativeTurnPipeline.cs`（清洁态 SHA-256 全程 `e3047b7cf6e4154e79c2e803e2d8c10cdc685ac4d348aa0988cdf1b2de8f1bb7`，每轮还原后实测一致）。跑法整行除结果目录外六轮逐字相同：

```
cd "C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave23-1023/carrier" && dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave23-1023/results/w23-run0N-gN" --logger trx --filter "FullyQualifiedName~NarrativeEndToEndPipelinePrototypeTests"
```

还原命令六轮相同＝`git checkout -- src/Pry.Core/Narrative/NarrativeTurnPipeline.cs`（载体内），还原后 `git status --short` 每轮实测 0 行。定向预算已用 7／16（基线 1 ＋ 本轮次 6），新门反证已用 6／10。

### G1｜段 2→3 的折叠门（把"不可判"折进"新话题"就打断）

- ① `src/Pry.Core/Narrative/NarrativeTurnPipeline.cs:229`
- ② 改前：`        if (klass == InterruptClass.NewTopic && spec.InterruptionReasonWhenNewTopic is { } reason)`
- ③ 改后：`        if (klass is InterruptClass.NewTopic or InterruptClass.Undeterminable && spec.InterruptionReasonWhenNewTopic is { } reason)`
- ④ `git diff --stat`：`src/Pry.Core/Narrative/NarrativeTurnPipeline.cs | 2 +-`，`1 file changed, 1 insertion(+), 1 deletion(-)`
- ⑤ 跑法整行：上面通用行，目录 `w23-run03-g1`
- ⑥ TRX：`24782_PHYTHLINK_2026-10-23_19_11_44.trx`（数 `*.trx`＝1）；`<Counters total="12" executed="12" passed="11" failed="1" … notExecuted="0" …/>`
- ⑦ 红格＝`Pry.Core.Tests.NarrativeEndToEndPipelinePrototypeTests.Turn_undeterminable_cue_stays_its_own_class_and_triggers_no_interruption`；消息要点：`Assert.Null() Failure: Value is not null / Expected: null / Actual: InterruptionOutcome { Result = Applied, Reason = SupersededByNewInput, … }`；红点行号＝判据文件 `NarrativeEndToEndPipelinePrototypeTests.cs:222`
- ⑧ `git checkout -- src/Pry.Core/Narrative/NarrativeTurnPipeline.cs`
- ⑨ 还原后 `git status --short`＝0 行
- ⑩ 突变态 SHA `636acaf324046c861b01c1520795c38d1b29fc35dd3b11c0452aeca41f362cc0`；还原后 SHA `e3047b7c…1bb7`
- ⑪ 当轮预检整段：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=1281,295,884x738 screen=2560x1440 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=9% used=8636MB
free_memory_gb=8.5
VERDICT=BUILD_OK
```

门有牙齿：不可判档独立成档这条在组合件里也被守住了（同轮其余 11 格全绿）。

### G2｜段 1→2 的唯一可发送真相门

- ① `src/Pry.Core/Narrative/NarrativeTurnPipeline.cs:196`
- ② 改前：`        if (!spec.Buffer.TryTakeSendableTruth(batch.BatchId))`
- ③ 改后：`        if (!spec.Buffer.TryTakeSendableTruth(batch.BatchId) && batch.BatchId.Length < 0)`（领取动作照做、停段门永不落下）
- ④ `git diff --stat`：`1 file changed, 1 insertion(+), 1 deletion(-)`
- ⑤ 目录 `w23-run04-g2`
- ⑥ TRX：`24782_PHYTHLINK_2026-10-23_19_12_22.trx`（1 份）；`<Counters total="12" executed="12" passed="11" failed="1" …/>`
- ⑦ 红格＝`…Turn_second_run_on_the_same_batch_is_stopped_at_the_single_sendable_truth`；要点：`Assert.Equal() Failure: Values differ / Expected: SendableTruthAlreadyClaimed / Actual: RoundRejected`；红点＝判据文件第 196 行
- ⑧⑨⑩ 同通用行；突变态 SHA `c3a4f4e610facd0df55ec851f8cc1d1033cdde457566e48e016e02f809e03eed`；还原后 `e3047b7c…1bb7`；status 0 行
- ⑪ 预检整段：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=1281,295,884x738 screen=2560x1440 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=4% used=8673MB
free_memory_gb=8.8
VERDICT=BUILD_OK
```

归因说明（诚实）：同轮其余 11 格全绿；第二回合没被停段门拦住后，是既有编排者按水位／历史重复把这一轮判成 `RoundRejected`，所以消息形状落在停段档位差（`SendableTruthAlreadyClaimed`→`RoundRejected`）而不是模型调用次数上——"同一批次不二次叙事"这句仍然被这一格的红点证伪了（真相领用门失效后，同批第二条叙事请求确实被放进来了）。

### G3｜段 4→5 的编译拒绝停段门

- ① `src/Pry.Core/Narrative/NarrativeTurnPipeline.cs:248`
- ② 改前：`        if (!assembly.Accepted)`
- ③ 改后：`        if (!assembly.Accepted && assembly.ReasonCode == NarrativeCompileReason.Ok)`（拒绝门只在不拒绝时才落下＝实际永不拦）
- ④ `1 file changed, 1 insertion(+), 1 deletion(-)`
- ⑤ 目录 `w23-run05-g3`
- ⑥ TRX：`24782_PHYTHLINK_2026-10-23_19_12_56.trx`（1 份）；`<Counters total="12" executed="12" passed="11" failed="1" …/>`
- ⑦ 红格＝`…Turn_context_rejection_short_circuits_the_compile_to_narrate_edge_with_zero_model_calls`；要点：`Assert.Equal() Failure: Values differ / Expected: ContextRejected / Actual: RoundRejected`；红点＝判据文件第 297 行（该格前三条断言 `NotNull`／`False(Accepted)`／`ReasonCode == WindowTooSmall` 仍绿，说明拒绝理由本身没被撼动，塌的是"拒绝之后不再往下走"这一句）
- ⑧⑨⑩ 通用行；突变态 SHA `28a6525e097a3b04e2c78acb3909ee1ce09c46733a2dcd8d7012551b913788ed`；还原后 `e3047b7c…1bb7`；status 0 行
- ⑪ 预检整段（本轮 dotnet 调用前那一次）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=1281,295,884x738 screen=2560x1440 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=3% used=8649MB
free_memory_gb=8.8
VERDICT=BUILD_OK
```

（本轮次我在改文件之前还跑过一次预检，读数为 `FOREGROUND: QQ pid=28676 … rect=318,74,1317x1221 … title=????? / gpu_util=6% used=8658MB / free_memory_gb=8.8 / VERDICT=BUILD_OK`；预检只在 `dotnet` 前施令，非 dotnet 命令不消耗它。）

### G4｜段 5→6 的待发段转发（不落库的成员＝整条链没有可发段）

- ① `src/Pry.Core/Narrative/NarrativeTurnPipeline.cs:279`
- ② 改前：`            commitOutcome = await store.TryCommitAtomicAsync(request, commit, round.HistoryEntries,`
- ③ 改后：`            commitOutcome = await store.TryCommitAtomicAsync(request, commit, null,`（该参数在落库面签名里可空，突变不产生任何构建告警）
- ④ `1 file changed, 1 insertion(+), 1 deletion(-)`
- ⑤ 目录 `w23-run06-g4`
- ⑥ TRX：`24782_PHYTHLINK_2026-10-23_19_13_47.trx`（1 份）；`<Counters total="12" executed="12" passed="10" failed="2" …/>`
- ⑦ 两枚红格（单枚突变带来的两处红，按消息形状与红点行号逐格归因，不署成两枚突变的功劳）：
  - `…Turn_one_to_eight_threads_the_sealed_batch_into_a_committed_real_sqlite_turn_with_paced_sends`：`Assert.Equal() Failure: Values differ / Expected: 3 / Actual: 0`（重开连接后 `narrative_outbox` 读回零行）；红点＝判据文件第 135 行
  - `…Turn_pacing_counts_come_from_the_committed_outbox_rows_read_back_from_the_database`：`System.ArgumentOutOfRangeException : Index was out of range. Must be non-negative and less than the size of the collection. (Parameter 'index')`（节奏拿到的读回序列为空，取第 1 段可发时刻越界）；红点＝判据文件第 335 行
  - 同轮 AggregateException 的内层还有 `DisposeAsync` 的 `System.IO.IOException : The process cannot access the file 'narrative.db' because it is being used by another process.`（判据文件第 54 行）：断言中途抛出后测试没走到本轮的库句柄释放，临时目录删不掉。这是**清理形状**、不是断言失败，绿轮（基线 12／12）里没有它；遗留的两个临时目录 `pry-wave23-e2e-91b290bf…`、`pry-wave23-e2e-abac3809…` 当场已删（系统临时目录、本刀自建）。改判据文件让这句不再出现会移动上面全部红点行号，故不改，登记于此。
- ⑧⑨⑩ 通用行；突变态 SHA `9ae0fa76183f4cc291c4725cc9353500120c6c25e2dd0379cd621521d831eed2`；还原后 `e3047b7c…1bb7`；status 0 行
- ⑪ 预检整段：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=1281,295,884x738 screen=2560x1440 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=4% used=8644MB
free_memory_gb=8.7
VERDICT=BUILD_OK
```

### G5｜段 7→8 的换形放行门（被场景挡也排可发时刻）

- ① `src/Pry.Core/Narrative/NarrativeTurnPipeline.cs:302`
- ② 改前：`        if (expression.Result == BehaviorMappingResult.Mapped && spec.Pacing is { } pacing`
- ③ 改后：`        if (expression.Result is not BehaviorMappingResult.NoBehaviorDecision && spec.Pacing is { } pacing`（只拦"缺决策"，把"被场景挡"当放行）
- ④ `1 file changed, 1 insertion(+), 1 deletion(-)`
- ⑤ 目录 `w23-run07-g5`
- ⑥ TRX：`24782_PHYTHLINK_2026-10-23_19_14_32.trx`（1 份）；`<Counters total="12" executed="12" passed="11" failed="1" …/>`
- ⑦ 红格＝`…Turn_scene_blocked_expression_skips_pacing_and_carries_the_reason_code_through`；要点：`Assert.Null() Failure: Value is not null / Actual: [09:00:00.0, 09:00:00.5, 09:00:01.0]`（被场景挡的回合照样排出了三条可发时刻）；红点＝判据文件第 363 行
- ⑧⑨⑩ 通用行；突变态 SHA `226b511ca8f2e8724afe3d553caa67ab85370c0bda195e95b38e6adc3a1eb612`；还原后 `e3047b7c…1bb7`；status 0 行
- ⑪ 预检整段：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=1281,295,884x738 screen=2560x1440 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=3% used=8621MB
free_memory_gb=8.9
VERDICT=BUILD_OK
```

（同轮的 `Turn_missing_behavior_decision_is_not_passed_through_and_paces_nothing` 仍绿，正好说明这枚突变只松开了"场景挡"这一档，缺决策档另有其门。）

### G6｜旧主链四件的源码扫描门（带阳性探针）

- ① `src/Pry.Core/Narrative/NarrativeTurnPipeline.cs:313`（文件末尾追加一行注释；清洁态 311 行）
- ② 改前：文件末尾为 `        return trace with { SendTimes = sendTimes };` ＋ `    }` ＋ `}`
- ③ 改后：同三行之后追加 `// mutation probe: legacy = new ReplyPlanner();`
- ④ `git diff --stat`：`src/Pry.Core/Narrative/NarrativeTurnPipeline.cs | 2 ++`，`1 file changed, 2 insertions(+)`
- ⑤ 目录 `w23-run08-g6`
- ⑥ TRX：`24782_PHYTHLINK_2026-10-23_19_14_58.trx`（1 份）；`<Counters total="12" executed="12" passed="11" failed="1" …/>`
- ⑦ 红格＝`…Pipeline_source_carries_no_legacy_chain_references_and_the_probe_proves_the_gate_bites`；要点：`Assert.DoesNotContain() Failure: Sub-string found ↓ (pos 11959) String: …"// mutation probe: legacy = new ReplyPlanner();" Found: "ReplyPlanner"`；红点＝判据文件第 435 行
- ⑧⑨⑩ 通用行；突变态 SHA `f640bb5b70471b69f21211411a99ae7a7a72ed98d0ed14cb1480ce8d33230c75`；还原后 `e3047b7c…1bb7`；status 0 行
- ⑪ 预检整段：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=1281,295,884x738 screen=2560x1440 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=4% used=8618MB
free_memory_gb=8.8
VERDICT=BUILD_OK
```

门有牙齿：扫描器确实读到被守护文件的字节；探针那半（自造含 `ReplyPlanner` 的临时 `.cs`）证明的是"看得见"，"拦得住"由这一轮的红点证明。

### 四·小结（新门的取红情况，无一枚靠改判据换来）

| 轮 | 守的门（组合件行号） | 红格数 | 红点（判据行） | 结论 |
|---|---|---|---|---|
| G1 | 段 2→3 折叠门 :229 | 1 | 222 | 门有牙齿 |
| G2 | 段 1→2 唯一真相门 :196 | 1 | 196 | 门有牙齿（归因见上） |
| G3 | 段 4→5 停段门 :248 | 1 | 297 | 门有牙齿 |
| G4 | 段 5→6 成员转发 :279 | 2 | 135／335 | 门有牙齿（单枚突变两处红，逐处归因见上） |
| G5 | 段 7→8 放行门 :302 | 1 | 363 | 门有牙齿 |
| G6 | 旧链四件扫描门 :313 | 1 | 435 | 门有牙齿 |

未取到红的本轮没有：六枚单枚突变各换来一次红，且同轮其余格绿（G4 是同轮两格红、十格绿）。

---

## 五 出口读数（守门人归位轮，10-23 19:2x–19:3x）

lane 在 §四 小结后撞上 150 轮硬上限，工单 §三 那九格出口读数与 §五／§六 两节未写。以下几格由守门人当场取，署名是守门人，不是 lane 的自述。

1. **归位通道**：lane 载体提交＝`7609af6`＋`f8404ac`（起点 `04a1f1b`）；主树落盘＝按字节铺两份新建文件后 `git add` ＋ `git commit --only <显式路径>`，落盘提交为 **`49d2454`**。他人名下两枚暂存项的索引 blob 全程未变（`6a78b81c71636d0757474084301cd35c2d0a83d7`／`954584e31866abf143468f4b6ebf530ddfa319b0`）；主树未跟踪面里 `docs/character-learning-safety.patch` 等七项照旧未动。
2. **`git diff --stat 8c2f3e4..HEAD -- src tests scripts Pry.slnx Directory.Build.props` 原文**：
   ```
    src/Pry.Core/Narrative/NarrativeTurnPipeline.cs    | 311 ++++++++++
    .../NarrativeEndToEndPipelinePrototypeTests.cs     | 669 +++++++++++++++++++++
    2 files changed, 980 insertions(+)
   ```
   ＝只见新建文件，既有文件零字节改动（工单 §〇.6.1 那条硬线由这一格自证）。
3. **判据条数从盘上数**（不采信任何自述数）：`grep -nE '\[Fact\]|\[Theory\]|\[InlineData|\[MemberData' tests/Pry.Core.Tests/NarrativeEndToEndPipelinePrototypeTests.cs` ＝ **12 枚 `[Fact]`**，行号 62／171／186／202／228／253／282／304／340／366／390／428（第 17 行那条是文件头注释里的说明文字，不是用例），`[Theory]`／`[InlineData]`／`MemberData`／抽象基类＝**0**。这与 §〇.5 那句"静态计数＝TRX 应执行数＝12"、与定向基线轮 `w23-run01-baseline` 的 `total=12`、与全量 Core 700→712 的差值三处对得上。
4. **守门人 `dotnet` 调用＝1 构建＋1 全量**，载体＝`.local-notes/gate-wave23-1023/carrier`（detached @ `49d2454`）。两次预检整段已在 `gate-round.log`：19:22:44 `free_memory_gb=9.4 VERDICT=BUILD_OK`（构建前）、19:22:54 `free_memory_gb=7.9 VERDICT=BUILD_OK`（全量前）；构建段末三行＝`已成功生成。／0 个警告／0 个错误`。
5. **解决方案级全量一次，四份 TRX 的 `<Counters>` 逐份**（目录 `.local-notes/gate-wave23-1023/full/`）：
   | TRX 文件名 | 程序集 | total | executed | passed | failed |
   |---|---|---|---|---|---|
   | `24782_PHYTHLINK_2026-10-23_19_22_58.trx` | Pry.Client.Tests | 108 | 108 | 108 | 0 |
   | `24782_PHYTHLINK_2026-10-23_19_22_59.trx` | Pry.App.Tests | 233 | 233 | 233 | 0 |
   | `24782_PHYTHLINK_2026-10-23_19_22_58[1].trx` | Pry.Api.Tests | 280 | 280 | 280 | 0 |
   | `24782_PHYTHLINK_2026-10-23_19_22_59[1].trx` | Pry.Core.Tests | 712 | 710 | 710 | 0 |
   等式由上表四行当场求和写出：108＋233＋280＋712＝**1333**，executed **1331**，failed **0**；两条跳过按 `outcome="NotExecuted"` 节点点名＝`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`（都在 Core 那一份；四份的 `notExecuted` 计数器均为 0，不看该计数器）。对账：在册 1321＝108＋233＋280＋700，本刀净增 Core 12 格 ⇒ **1321＋12＝1333**，无第三处来源。**在册基线由守门人这一手改写＝1333／0／2（总 1333、executed 1331；分项 Client 108／App 233／Api 280／Core 712、executed 710），1321 自此过期。**
6. `scripts/measure-fixed-eval.ps1 -ResultsDirectory <本轮目录>`＝`Catalog valid: 76 permanent scenarios. Mappings are partial evidence only. / Passed=1331; Failed=0; Other=2`，退出码 0。
7. `git diff --check` 干净；守门人载体 `git status --short`＝0 行；lane 载体 `git status --short`＝0 行（见 §六.3）；`git ls-files \| wc -l`＝**596**（落盘后实测，上一登记值 594 的增量＝本报告＋审计那一份，代码刀那两枚文件在 `49d2454` 之前已计入 594 之后的两格——此处只报实测，不做加法）。

## 六 第二十波换人复采（工单 §二 第二件正活）：盘上有红、配方无档 ⇒ 本刀不签第二换手

lane 用定向额度跑了三轮换人复采，**盘上产物在、报告里一节没有**（报告最后一次落盘 19:18，三轮 TRX 分别是 19:18:55／19:19:20／19:19:42）。逐轮从 TRX 直读：

| 轮 | TRX（`.local-notes/qa-wave23-1023/results/`） | 计数 | 唯一红格 | 红点 |
|---|---|---|---|---|
| r1 | `w23-run09-r1/…_19_18_55.trx` | total 36／executed 36／passed 35／failed 1 | `NarrativeGapParityPrototypeTests.InputBuffer_one_batch_yields_exactly_one_sendable_truth` | `:97`（`Assert.False` Expected False／Actual True） |
| r2 | `w23-run10-r2/…_19_19_20.trx` | 36／36／35／1 | `…Interruption_without_a_nameable_reason_is_rejected_not_guessed` | `:147`（Expected `RejectedUnnamedReason`／Actual `Applied`） |
| r3 | `w23-run11-r3/…_19_19_42.trx` | 36／36／35／1 | `…Classifier_every_cue_maps_to_its_declared_class(cue: ContinuationMarker, expected: Continuation)` | `:179`（Expected `Continuation`／Actual `NewTopic`） |

- **形状是对的，凭证是不全的**：每轮"恰好一枚红、其余绿"符合工单 §二 的判据形状，但十一项字段里 ①改前原文 ②改后原文 ③`git diff --stat` ⑧还原命令 ⑨还原后 status 行数 ⑩两态 SHA-256 ⑪当轮预检**全部无档**——即"红的是哪一格"可考，"改坏了 `src/` 哪一行"不可考。按常驻口径，缺 ①②③ 的单枚突变不能署成第二换手凭证。**所以这三格登记为"盘上有红、配方无档"，第二十波那 34 格的在册第二换手覆盖度本刀贡献＝0 格，34 格照旧全欠**（不许写成"已付 6"或"已付 3"，也不许写成"不需要复采"）。
- **r4 未起跑**：载体在 19:19:49 遗留一枚未提交突变（`src/Pry.Core/Narrative/NarrativeBehaviorExpressionMapping.cs:88`，`Warm => NarrativeExpressionGear.Warm` 被改成 `Restrained`），mtime 晚于 r3 的 TRX（19:19:42）⇒ 属一次没跑完的 r4，**不是 r3 的证据**。守门人处置＝先把突变态字节归档（`.local-notes/qa-wave23-1023/out/orphan-r4-mutated-NarrativeBehaviorExpressionMapping.cs.txt`），再 `git checkout --` 还原该文件（还原后 SHA `2bbae3d9…`，与 `49d2454` 面一致），载体 `git status --short`＝0 行。该文件此后未进本轮构建／全量（守门人轮起点是干净树）。
- **剩余定向额度**：lane 用 10／16（1 定向基线＋6 枚 G＋3 轮 r），复采那 6 轮里实跑 3 轮，**还有 3 轮的额度没花**——第二十四波工单把这笔额度接走，并要求逐轮十一项齐落盘。

## 七 未验证（工单 §五 那节 lane 未写，此处按逐项登记）

真实模型对照＝零读数；生产接线与切换＝组合件 `NarrativeTurnPipeline` **零生产调用方**（全仓只有那份新测试引用它）；UI 可见行为＝零读数；生产库＝零接触（只碰系统临时目录，临时库收尾已删）；待标定参数权威值＝未定（本刀全部走入参，源码零新门槛）；与 HDS-007 到期再叙事的关系＝未评估。另有工单 §一 那八段里的**六处"接不上，押接线刀"**（lane §三 衔接表点名，其中 5→6 段是 `NarrativeRoundRunner.BuildCommit` 为 private、由宿主注入 `CommitFactory` 代偿，且 `ContentHash` 是字面占位「待接线」）——这六格不是本刀的未验证，是本刀**明确交付给下一把接线刀的输入**。

## 八 口径（这三句不许从本报告读出来）

「新链已可跑」「旧链可删」「不进包已做到」三条**均不成立**：本刀只在测试宿主里把这条链按顺序穿过一次，生产默认路径一字未动，旧链仍在生产与打包面里；构建不把旧链打包那一条照旧未达成。在册第二换手覆盖度不因本刀的 G1–G6 而前进（那六枚是**新门的自证**，属第一只手）。

