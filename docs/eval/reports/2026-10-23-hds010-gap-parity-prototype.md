# HDS-010 第一片后续·5 处旧链缺口的等价格隔离原型（第二十波）

- 工单：`docs/dispatch-hds-wave20-20261023.md`（id `HDS-WAVE20-1023`；任务＝`HDS-010` 表 C 片 4 的"先建后删"那一半）
- 执行方：冷启动 lane（第二十波）
- 性质：动产品代码的**隔离原型刀**。新建类型＋新建判据，全部内存闭环，**不接生产、不落库、不碰界面**。
- 本文不写状态词／结案词；除当场贴读数的条目外一律标「未验证」。交付的是隔离原型，**生产仍走旧链**——本刀没有接线，就没有用户可见变化。

---

## 〇 开工前必读与入场读数（当场实测，原样贴）

### 〇.1 主树入场（共享脏树，他人资产一律不碰）

```
$ git -C C:/Users/24782/Desktop/Pry_Project status --short --branch
## codex/ui-main-window-redesign
M  docs/dispatch-tip1020-02-memory-group-judgment.md
M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs
?? .qoder/
?? docs/character-learning-safety.patch
?? docs/eval/reports/2026-10-23-qa-wave18-unheard-gates.md
?? docs/research/HDS_INTERLUDE.md
?? docs/research/HDS_QQ_RESEARCH_INDEX.md
?? docs/research/QQ_BRIDGE.md

$ git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD
bceb80655e2f452a8b0f0440aa38ce04ba93aa91

$ date
Fri Oct 23 17:50:15     2026
```

两枚 `M ` 暂存项与若干 `??` 全属他人名下：不读内容、不改、不提交、不 `stash`/`clean`/`rm`。本 lane 在共享主树里禁止任何 `git commit`/`cherry-pick`/`merge`/`tag`/`push`/打包。

### 〇.2 起刀点线尾就地更正（工单 §〇.3 的 `e8a842a` → 当下实测）

工单 §〇.3 写的起刀点线尾是 `e8a842a`；开工当场自取当下 `codex/ui-main-window-redesign` 线尾全号：

```
$ git -C C:/Users/24782/Desktop/Pry_Project rev-parse codex/ui-main-window-redesign
bceb80655e2f452a8b0f0440aa38ce04ba93aa91
```

实测当下线尾与工单所写 `e8a842a` 的差集（证"只差纯文档提交、`src`/`tests` 面一致"这两条）：

```
$ git -C … log --oneline e8a842a..bceb806
bceb806 docs(plans): 自我更正上一把的顶账加法——verify-repository 当场实测 583 而非预写的 578…
a04e1b4 docs(plans,sprint,eval,dispatch): 收第十六＋十七波归位登记…

$ git -C … merge-base --is-ancestor e8a842a bceb806   → 0（e8a842a 是 bceb806 的祖先）

$ git -C … diff --stat e8a842a..bceb806 -- src tests scripts Pry.slnx Directory.Build.props
（无输出＝空）
```

事实核对：`e8a842a..bceb806` 恰两枚提交，都是 `docs(...)` 前缀的文档刀；`src`/`tests`/`scripts`/`Pry.slnx`/`Directory.Build.props` 五面 `git diff --stat` 为空 ⇒ 与工单那句"只差纯文档提交、`src`/`tests` 面一致"当场自证吻合。**起刀点取当下线尾 `bceb806`**（不是工单预写的 `e8a842a`，也不是更旧的 `def0faf`），以免拆掉第十六／十七波刚落地的协调层与落库面。

### 〇.3 载体（detached 自当下线尾）

```
$ git -C C:/Users/24782/Desktop/Pry_Project worktree add --detach \
    C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave20-1023/carrier \
    bceb80655e2f452a8b0f0440aa38ce04ba93aa91
Preparing worktree (detached HEAD bceb806)
HEAD is now at bceb806 docs(plans): 自我更正上一把的顶账加法…

# 入场两行判据（载体内）：
$ git -C …/carrier status --short | wc -l   → 0
$ git -C …/carrier rev-parse HEAD           → bceb80655e2f452a8b0f0440aa38ce04ba93aa91
```

载体在 `src`/`tests`/`scripts`/`Pry.slnx`/`Directory.Build.props` 五面与主树工作副本一致（因 〇.2 已证两线尾间无这五面差异），且不含他人的两枚 `M `/`??`（detached 干净副本，`status` 行数 0）。

### 〇.4 允许文件面（本刀只有这些）

- `src/Pry.Core/Narrative/` 目录内的**新建**文件（既有十份契约／原型文件＋`NarrativeLifecycleCoordinator.cs`＋`Storage/` 两枚，一字不动）；
- `tests/Pry.Core.Tests/` 目录内的**新建**测试文件；
- 本报告。

需要既有类型开口子时**停下登记为"押接线刀"，不自己改**。

### 〇.5 交付的五个隔离类型（工单 §一 表 5 行 → 新建文件名）

| # | 缺口（大白话） | 新类型 | 新建文件 |
|---|---|---|---|
| 1 | 连着发几条，她攒成一轮再答 | `NarrativeInputBuffer` | `src/Pry.Core/Narrative/NarrativeInputBuffer.cs` |
| 2 | 正在打字时她先停 | `NarrativeInterruption` | `src/Pry.Core/Narrative/NarrativeInterruption.cs` |
| 3 | 判"接着说"还是"新话题" | `NarrativeInterruptClassifier` | `src/Pry.Core/Narrative/NarrativeInterruptClassifier.cs` |
| 4 | 表情／口头禅会不会被场景挡 | `NarrativeBehaviorExpressionMapping` | `src/Pry.Core/Narrative/NarrativeBehaviorExpressionMapping.cs` |
| 5 | 一句一句按阅读节奏冒出来 | `NarrativeReplyPacing` | `src/Pry.Core/Narrative/NarrativeReplyPacing.cs` |

机制取材（只读、不得 using/构造/转发旧链四件 `BehaviorPlanner`/`ReplyGenerationRenderer`/`ReplyPlanner`/`ReplyNecessity`）：
`InputProbe.cs`/`InputBuffer.cs`/`TurnScheduler.cs`（攒批）、`InterruptionController.cs`/`ConversationRuntime` 打断段（打断）、`InterruptClassifier.cs`/`TurnGates.cs`（分类形状）、`BehaviorPlanner.cs`+`BehaviorDecision.cs`+`ExpressionSelector`（行为→表达换形）、`Delivery.cs` 的 `ReplyPacing.DelayFor`/`MessageIntervalStats.cs`/`VisibleReply.cs`（节奏）。以上均在报告 §二 逐条落到新类型的入参与拒绝理由，封顶条件／时间／节奏／批量参数一律走**入参**，源码零数字门槛。

（§〇 到此，后续 §一～逐批落盘。）

## 一 构建读数（解决方案级，一轮，预算 1/1）

焦点预检（每次 dotnet 前跑；本波 build 前那一次整段原样）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: PickerHost pid=60196 class=Shell_SystemDialog style=0x94800000 rect=1056,501,447x390 screen=2560x1440 covers=False borderless=False title=Windows ????
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=2% used=8552MB
free_memory_gb=11.7
VERDICT=BUILD_OK
```

`free_memory_gb=11.7`（≥4）、前台窗是系统对话框（`covers=False borderless=False`＝非全屏应用，按 v6 放行）、无 SQLite `disk I/O`／`out of memory` 形态 ⇒ 允许起跑。

```
$ dotnet build Pry.slnx -c Release        # 载体内
已成功生成。
    0 个警告
    0 个错误
已用时间 00:00:09.98
```

新建五枚源码文件＋一枚测试文件均编译通过，0 警告 0 错误。

## 二 反证（单枚·单轮·清洁红，十一项字段逐轮齐）

预算内每轮一枚突变，突变轮去掉 `--no-build` 让被改工程增量重编（解决方案级 `dotnet build` 仍只 1 次，最终全量那轮用 `--no-build`）。还原一律 `git checkout -- <文件>`（载体已把六枚代码文件提交为 `0db4125`，`git checkout` 可靠还原；报告不入这枚提交）。

### 基线绿（定向 1/8，载体 `0db4125`）

跑法：`dotnet test Pry.slnx -c Release --no-build --results-directory "…/runs/run-baseline" --logger trx --filter "FullyQualifiedName~NarrativeGapParityPrototypeTests"`
→ `已通过! - 失败: 0，通过: 36，已跳过: 0，总计: 36`（Core.dll）＝静态 26 `[Fact]`＋10 `[InlineData(`＝36，本类无 `MemberData`／抽象基类 ⇒ 静态＝TRX＝36。

### M1｜缺口1 攒批"到达次序去重门"

- ① 文件:行 `src/Pry.Core/Narrative/NarrativeInputBuffer.cs:107`
- ② 改前 `if (_currentSeqs is not null && seq <= _lastSeq)`
- ③ 改后 `if (_currentSeqs is not null && seq < _lastSeq)`（去掉等号 ⇒ 重复序号不再被拦）
- ④ `git diff --stat` 突变态：`NarrativeInputBuffer.cs | 1 + 1 -`（1 file, 1 insertion, 1 deletion）
- ⑤ 跑法 `dotnet test Pry.slnx -c Release --results-directory "…/runs/run-m01" --logger trx --filter "FullyQualifiedName~NarrativeGapParityPrototypeTests"`
- ⑥ Core TRX `run-m01/24782_PHYTHLINK_2026-10-23_18_02_59[?].trx`（同目录四份，Core 那份含本失败；另三份 Client/Api/App 匹配 0 用例）
- ⑦ 红格全名 `Pry.Core.Tests.NarrativeGapParityPrototypeTests.InputBuffer_duplicate_and_out_of_order_seqs_are_rejected_by_name`；消息 `Assert.Equal() Failure — Expected: RejectedDuplicateSeq`；红点 `NarrativeGapParityPrototypeTests.cs:line 67`；本轮 失败 1／通过 35（**单枚清洁红**）
- ⑧ 还原 `git checkout -- src/Pry.Core/Narrative/NarrativeInputBuffer.cs`
- ⑨ 还原后 `git status --short | wc -l` ＝ 1（仅报告，他人不在载体）
- ⑩ 突变态 SHA-256 `23d2b2b1b804746cbb0aeada4adcc48f7a1e5f23fd1e1f5835fdbeb14fa9b5aa`；还原后 `1e40dfb3f937d894b7bf19d1e20f66912da33df5a112b17baadfe18d97094067` ＝ 基线（逐位相同）
- ⑪ 当轮预检整段见 §一（同一场 `free_memory_gb=11.0`、`VERDICT=BUILD_OK`，前台非全屏）

### M2｜缺口1 "同批次一条可发送真相"唯一性门

- ① 文件:行 `src/Pry.Core/Narrative/NarrativeInputBuffer.cs:172`（`TryTakeSendableTruth` 末行）
- ② 改前 `return _claimedSendable.Add(batchId);`
- ③ 改后 `_ = _claimedSendable.Add(batchId); return true;`（领取永不失败＝令牌可被重复领走）
- ④ `git diff --stat` 突变态：`NarrativeInputBuffer.cs | 2 + 1 -`（1 file）
- ⑤ 跑法 `dotnet test Pry.slnx -c Release --results-directory "…/runs/run-m02" --logger trx --filter "FullyQualifiedName~NarrativeGapParityPrototypeTests"`
- ⑥ Core TRX：`run-m02` 目录四份齐全（Client/Api/App 匹配 0 用例，Core 那份含本失败）
- ⑦ 红格全名 `…NarrativeGapParityPrototypeTests.InputBuffer_one_batch_yields_exactly_one_sendable_truth`；消息 `Assert.False() Failure — Expected: False`；红点 `NarrativeGapParityPrototypeTests.cs:line 97`；失败 1／通过 35（单枚清洁红，无新警告）
- ⑧ 还原 `git checkout -- src/Pry.Core/Narrative/NarrativeInputBuffer.cs`
- ⑨ 还原后 `git status --short | wc -l` ＝ 1
- ⑩ 突变态 SHA-256 `76259ed76cf04c2154ca12ddb14fce07baa2e0423f98ebee90ede298afcc2b07`；还原后 `1e40dfb3…4067` ＝ 基线
- ⑪ 当轮预检 `VERDICT=BUILD_OK`（free_memory 达标、前台非全屏）

### M3｜缺口2 "已送达段不可撤回"终态门

- ① 文件:行 `src/Pry.Core/Narrative/NarrativeInterruption.cs:38`
- ② 改前 `public bool IsCancellable => State == DeliverySegmentState.Pending;`
- ③ 改后 `… || State == DeliverySegmentState.Delivered;`（把已送达也判为可作废）
- ④ `git diff --stat` 突变态：`NarrativeInterruption.cs | 1 + 1 -`
- ⑤ 跑法 `dotnet test … --results-directory "…/runs/run-m03" --logger trx --filter "…NarrativeGapParityPrototypeTests"`
- ⑥ Core TRX：`run-m03` 四份，Core 那份含两失败
- ⑦ 红格（**同枚突变、按消息形状＋红点行号归因，两格同源于这条终态门**）：
  - `Interruption_never_withdraws_a_delivered_segment` — `Assert.Empty() Failure: Collection was not empty`，红点 `…Tests.cs:line 135`
  - `Interruption_cancels_only_pending_segments_and_withholds_delivered` — `Assert.Equal() Expected: 2 / Actual: 3`，红点 `…Tests.cs:line 126`
  - 本轮 失败 2／通过 34（一枚突变，计一处新证）
- ⑧ 还原 `git checkout -- src/Pry.Core/Narrative/NarrativeInterruption.cs`
- ⑨ 还原后 status 行数 ＝ 1
- ⑩ 突变态 `ed4c0b6879bddf8baf6685cd804702b3c1213982b66605fc2deec7686a7c973e`；还原后 `fca7209a7163846c3cf60cea6f87cd0ab15d9f7e40b5c6baff0bfae553ec7fdf` ＝ 基线
- ⑪ 当轮预检 `VERDICT=BUILD_OK`

### M4｜缺口3 "不可判独立成档、不折叠"门

- ① 文件:行 `src/Pry.Core/Narrative/NarrativeInterruptClassifier.cs:59`（`Classify` 默认档）
- ② 改前 `_ => InterruptClass.Undeterminable,`
- ③ 改后 `_ => InterruptClass.NewTopic,`（不可判被折叠进"新话题"）
- ④ `git diff --stat` 突变态：`NarrativeInterruptClassifier.cs | 1 + 1 -`
- ⑤ 跑法 `dotnet test … run-m04 … --filter`
- ⑥ Core TRX：`run-m04`，Core 那份含三失败
- ⑦ 红格（同枚折叠突变，三格同源于默认档不可判这一条）：
  - `Classifier_undeterminable_is_its_own_bucket_not_folded_into_a_real_class` — `Expected: Undeterminable / Actual: NewTopic`，红点 `:line 186`
  - `Classifier_every_cue_maps_to_its_declared_class(cue: NoSignal, expected: Undeterminable)`（`[Theory]` 的 NoSignal 行）— 同上，红点 `:line 179`
  - `Classifier_undefined_cue_value_falls_to_undeterminable_not_a_real_class`（越界值 `(InterruptCue)99`）— 同上，红点 `:line 195`
  - 失败 3／通过 33（一枚突变，一处折叠门；余四行 `Every_cue` 因走显式分支仍绿）
- ⑧ 还原 `git checkout -- …NarrativeInterruptClassifier.cs`
- ⑨ status ＝ 1
- ⑩ 突变态 `7bd201dbceaab7121e7d40a0a63a12edb1a1a0cfb81b99913f698ed02cc9e9fc`；还原后 `9f1cfed2a62b3a9187ab48bcd9e9b1a032b988c8d2f67e11796574808961ce13` ＝ 基线
- ⑪ 预检 `VERDICT=BUILD_OK`

### M5｜缺口4 "缺决策不得默认放行"门

- ① 文件:行 `src/Pry.Core/Narrative/NarrativeBehaviorExpressionMapping.cs:75-76`（null／Unspecified 分支返回值）
- ② 改前 `return new BehaviorExpressionOutcome(BehaviorMappingResult.NoBehaviorDecision, NarrativeExpressionGear.NotDetermined, null);`
- ③ 改后 `return new BehaviorExpressionOutcome(BehaviorMappingResult.Mapped, NarrativeExpressionGear.Neutral, null);`（缺决策当已放行）
- ④ `git diff --stat` 突变态：`NarrativeBehaviorExpressionMapping.cs | 2 + 2 -`
- ⑤ 跑法 `dotnet test … run-m05 … --filter`
- ⑥ Core TRX：`run-m05`，Core 那份含两失败
- ⑦ 红格（同源缺省放行门，两格）：
  - `Mapping_missing_behavior_decision_does_not_default_to_pass_through` — `Expected: NoBehaviorDecision / Actual: Mapped`，红点 `:line 223`
  - `Mapping_unspecified_register_is_treated_as_no_decision` — 同上，红点 `:line 233`
  - 失败 2／通过 34
- ⑧ 还原 `git checkout -- …NarrativeBehaviorExpressionMapping.cs`
- ⑨ status ＝ 1
- ⑩ 突变态 `60977d15a3f56e6987dfb3d3fafe90f5ed14f9d9f7b83fe8973329e4a3d51851`；还原后 `2bbae3d9922b20db046746abc3edca4d9636fb7bdb1ffa2e8a0fbb9546cc2a61` ＝ 基线
- ⑪ 预检 `VERDICT=BUILD_OK`

### M6｜缺口5 "下一段时刻由上一段长度决定"门

- ① 文件:行 `src/Pry.Core/Narrative/NarrativeReplyPacing.cs:89`
- ② 改前 `TimeSpan.FromSeconds(segmentCharacterCounts[i - 1] / input.ReadingCharactersPerSecond)`
- ③ 改后 `… segmentCharacterCounts[i] …`（用本段而非前一段长度推节奏）
- ④ `git diff --stat` 突变态：`NarrativeReplyPacing.cs | 1 + 1 -`
- ⑤ 跑法 `dotnet test … run-m06 … --filter`
- ⑥ Core TRX：`run-m06`，Core 那份含两失败
- ⑦ 红格（同源"前段驱动节奏"门，两格；`monotonic` 与 `rhythm_ignores_content` 仍绿，说明红点确实锁在"取哪一段长度"这一处）：
  - `Pacing_longer_previous_segment_sends_next_no_earlier` — `Assert.True() Expected: True / Actual: False`，红点 `:line 276`
  - `Pacing_min_gap_floors_and_max_gap_caps_the_interval` — `Expected: 09:00:02 / Actual: 09:00:00`（封顶支因取到零长度段而不被触发），红点 `:line 286`
  - 失败 2／通过 34
- ⑧ 还原 `git checkout -- …NarrativeReplyPacing.cs`
- ⑨ status ＝ 1
- ⑩ 突变态 `ae647737aca28d89db7e5408a2f87bb463f8ef36c8259ba2c3d51a9a3d2a661d`；还原后 `f0e826dfb35227903e68b835584a7589813250b25d70f3838a4e6029407e3fd9` ＝ 基线
- ⑪ 预检 `VERDICT=BUILD_OK`

### M7｜缺口4 "被场景挡给可点名理由"门 —— 未取到红（定向预算用尽）

定向预算（工单 §〇.7 ≤8）＝基线 1 ＋ M1～M7 共 8 轮，已用满。M7 拟单放禁用 `NarrativeBehaviorExpressionMapping.cs:79` 的
`if (cue.SceneBlockCodes is { Count: > 0 })` 分支（改成编译期可判恒假的模式以隔离"被挡"语义），实测该写法触发
`error CS8518`（模式恒不匹配）→ 编译失败、不是清洁红，该轮作废；预算此刻恰好用尽，未再起第二轮改法。

故 `Mapping_scene_block_yields_a_nameable_rejection_reason_and_no_pass_through` 一格登记为**未取到红**，原因＝
① 定向预算 8/8 用尽，② 首次尝试的突变表达式因编译器常量折叠而变编译错误、未成清洁红。该格在基线 36/36 绿与终局全量 Core 690 绿中在册通过，承重断言落在 `NarrativeBehaviorExpressionMapping.cs:82`（`BlockingSceneCode = cue.SceneBlockCodes[0]` 的可点名理由透传）与 `…Tests.cs:245` 的 `Assert.Equal("sensitive_scene", outcome.BlockingSceneCode)`。**未放宽任何断言、未固定返回值、未吞异常、未 Skip／注释任何一格。** 下一把定向额度（或归位后守门人复采轮）应补一枚"把 `is { Count: > 0 }` 改 `is null`"的单放清洁红。

> 结构上红不了的防御支（当场指出，非凑数）：`NarrativeReplyPacing.cs:91` 的 `if (gap < TimeSpan.Zero) gap = TimeSpan.Zero;` 是双保险——入参已 Validate 为正、字符数非负 ⇒ `rawGap` 恒非负，该分支在现有可达输入下不可触发，属防御纵深；其承重实际在 `:89` 的除法与 `Clamp`（已由 M6 打红）。登记为"结构上当前红不了"，不领红、不删断言。

## 三 出口读数（逐格贴，缺一格报哪格缺）

### 三.1 构建读数（解决方案级，1/1）

见 §一：载体内 `dotnet build Pry.slnx -c Release` → `已成功生成。0 个警告 0 个错误`（0 警告 0 错误原样）。0 警告这条对本刀尤其要紧——五枚新源码文件里若留未用字段／死支会顶破 0 警告（开工中确有一枚 `_hasLastSeq` 因 CS0414 风险被删）。

### 三.2 全量四份 TRX 的 `<Counters>`（逐份，`--no-build` 去掉那轮先重编清洁基线，见三.6 仪器注）

跑法：`dotnet test Pry.slnx -c Release --results-directory "…/runs/run-full" --logger trx`（四工程各落一份 TRX，先数到 4 份再逐份读）：

```
run-full/24782_PHYTHLINK_2026-10-23_18_07_47.trx  className=Pry.Client.Tests
  <Counters total="108" executed="108" passed="108" failed="0" error="0" notExecuted="0" …/>
run-full/24782_PHYTHLINK_2026-10-23_18_07_48.trx  className=Pry.Core.Tests
  <Counters total="690" executed="688" passed="688" failed="0" error="0" notExecuted="0" …/>
run-full/24782_PHYTHLINK_2026-10-23_18_07_49.trx  className=Pry.App.Tests
  <Counters total="233" executed="233" passed="233" failed="0" error="0" notExecuted="0" …/>
run-full/24782_PHYTHLINK_2026-10-23_18_07_49[1].trx  className=Pry.Api.Tests
  <Counters total="280" executed="280" passed="280" failed="0" error="0" notExecuted="0" …/>
```

合计 total 1311、executed 1309、failed 0。守门人入场基线 1275/0/2 加本刀 Core 新落 36 格＝1311/0/2，逐格吻合。

两枚跳过按 `outcome="NotExecuted"` 点名（`notExecuted` 计数器恒 0，不据它判）：

```
run-full/…_18_07_48.trx outcome="NotExecuted"：
  Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline
  Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key
```

二者是他机环境跳过（离线语音模型／子进程密钥缺失），非本刀引入、非断言失败；Core 690−2＝688 全绿。本刀 36 格无 Skip（见三.7 计数）。

### 三.3 定向轮次（8/8 用满）与每轮红名清单

`dotnet test … --filter "FullyQualifiedName~NarrativeGapParityPrototypeTests"`，各轮独立 `--results-directory run-…`：

| 轮 | 目录 | Core TRX 分项 | 红名（按消息形状＋红点行号归属） |
|---|---|---|---|
| 基线 | run-baseline | 36/0/36 | —（36 全绿） |
| M1 | run-m01 | 36/1/35 | `InputBuffer_duplicate_and_out_of_order_seqs_are_rejected_by_name` |
| M2 | run-m02 | 36/1/35 | `InputBuffer_one_batch_yields_exactly_one_sendable_truth` |
| M3 | run-m03 | 36/2/34 | `Interruption_never_withdraws_a_delivered_segment`、`Interruption_cancels_only_pending_segments_and_withholds_delivered`（同一枚终态门，两处红） |
| M4 | run-m04 | 36/3/33 | `Classifier_undeterminable_is_its_own_bucket_not_folded_into_a_real_class`、`Classifier_every_cue_maps_to_its_declared_class(cue: NoSignal, expected: Undeterminable)`、`Classifier_undefined_cue_value_falls_to_undeterminable_not_a_real_class`（同一枚折叠门，三处红） |
| M5 | run-m05 | 36/2/34 | `Mapping_missing_behavior_decision_does_not_default_to_pass_through`、`Mapping_unspecified_register_is_treated_as_no_decision` |
| M6 | run-m06 | 36/2/34 | `Pacing_longer_previous_segment_sends_next_no_earlier`、`Pacing_min_gap_floors_and_max_gap_caps_the_interval` |
| M7 | run-m07 | 0/0/0（TRX 仅 1806 字节，total=0） | 无红可读——`error CS8518` 使工程编译失败、测试未起跑，本轮作废 |

M7 记「未取到红」（详见 §二 M7 段）：① 定向预算 8/8 用尽，② 首次改法 `is { Count: > int.MaxValue }` 被编译器常量折叠成恒假模式 → CS8518 编译错误、非清洁红。故承重格 `Mapping_scene_block_yields_a_nameable_rejection_reason_and_no_pass_through` 未单放取红；该格在基线 36/36 与终局全量 Core 688 中均绿在册。

未取到红清单（其余）：`Gap_prototype_sources_…_probe_proves_the_gate_bites`（旧链四件源码扫描格，`:322`，阳性探针 `:349` 写临时文件含 `new ReplyPlanner()`、断言 `:351` Contains 自证咬合）与 §二末「结构上当前红不了」的 `NarrativeReplyPacing.cs:91` 防御支——两者预算内未各自单放；均**未放宽断言、未固定返回值、未吞异常、未 Skip／注释任何一格**。下一把定向额度应补：(a) 场景挡格把 `is { Count: > 0 }` 改 `is null` 的单放清洁红；(b) 扫描格把某枚 forbidden 标识符注入五源之一、证扫描器报警的单放红。

### 三.4 起刀点→载体 HEAD 的落地面（`git diff --stat` 原文，应只见新建文件）

```
$ git diff --stat bceb806..HEAD -- src tests scripts Pry.slnx Directory.Build.props
 .../NarrativeBehaviorExpressionMapping.cs          | 104 ++++++
 src/Pry.Core/Narrative/NarrativeInputBuffer.cs     | 188 +++++++++++
 .../Narrative/NarrativeInterruptClassifier.cs      |  62 ++++
 src/Pry.Core/Narrative/NarrativeInterruption.cs    |  94 ++++++
 src/Pry.Core/Narrative/NarrativeReplyPacing.cs     | 104 ++++++
 .../NarrativeGapParityPrototypeTests.cs            | 370 +++++++++++++++++++++
 6 files changed, 922 insertions(+)
```

`git diff --name-status` 六枚全为 `A`（新增），0 删除、0 修改；既有文件（含 `NarrativeLifecycleCoordinator.cs`、`Storage/` 两枚、`DeliveryLedger.cs`、`NarrativeContractReason.cs` 等）一字未动。922 插入＝五枚源码（552）＋一枚测试（370）。

### 三.5 载体收尾状态＋每轮还原后 SHA-256 与基线一致

- 定向／全量各轮突变态还原一律 `git checkout -- <文件>`；§二 每段 ⑨ 记当轮 `git status --short | wc -l`＝1（唯一未跟踪项＝本报告，他人资产不在载体），⑩ 记各枚还原后 SHA-256＝基线。
- 收尾本报告入库后，载体 `git status --short | wc -l`＝0（见文末落盘确认）。
- 基线 SHA-256（＝各突变轮 `git checkout` 还原后读数，逐位相同）当场复核：

```
1e40dfb3f937d894b7bf19d1e20f66912da33df5a112b17baadfe18d97094067  NarrativeInputBuffer.cs（§二 M1/M2 还原值）
fca7209a7163846c3cf60cea6f87cd0ab15d9f7e40b5c6baff0bfae553ec7fdf  NarrativeInterruption.cs（M3）
9f1cfed2a62b3a9187ab48bcd9e9b1a032b988c8d2f67e11796574808961ce13  NarrativeInterruptClassifier.cs（M4）
2bbae3d9922b20db046746abc3edca4d9636fb7bdb1ffa2e8a0fbb9546cc2a61  NarrativeBehaviorExpressionMapping.cs（M5）
f0e826dfb35227903e68b835584a7589813250b25d70f3838a4e6029407e3fd9  NarrativeReplyPacing.cs（M6）
```

各枚当场 `sha256sum` 与 §二 记录的还原后值逐位相同 ⇒ 未残留任何突变。

### 三.6 `dotnet` 调用总次数（只认盘上产物）

盘上证据：`dotnet build` 1 次（产物＝各工程 `bin/Release`，0 警告 0 错误）；`dotnet test` 9 次＝run-baseline＋run-m01…m07（8 次定向）＋run-full（1 次全量），每次落一份独立 `--results-directory`。合计 **10 次** `dotnet`，与预算（build≤1／全量≤1／定向≤8）相符，无越界。

仪器注（如实登记）：终局全量那轮**去掉** `--no-build`——因前序突变轮增量重编会让磁盘 Core.dll 停在 M6 突变态，去掉 `--no-build` 令测试前先重编回清洁基线，避免用脏 DLL 测。解决方案级 `dotnet build` 仍只 1 次（增量重编属 `dotnet test` 前置、非独立 build 调用）。

### 三.7 新增判据格数（静态 vs TRX）

逐文件计数（`tests/Pry.Core.Tests/NarrativeGapParityPrototypeTests.cs`）：

```
[Fact]        : 26
[Theory]      : 2   （每 Theory 由 [InlineData(] 展开）
[InlineData(  : 10  （两 Theory 各 5 行）
[MemberData   : 0
Skip / Assert.Skip: 0
静态格 = 26 + 10 = 36
```

与 TRX 分项的差：本类**静态＝TRX＝36**。工单 §三.7 提示本仓 Core 另有 1 个抽象基类与 1 个 `MemberData` Theory 会让静态计数≠TRX——本刀两样都没用（MemberData 计数 0、类非抽象、无 `Skip`），故不需为差例点名，逐格一一对应：定向各轮 Core TRX `total="36"`、全量那 36 格并到 Core `total=690`（654 原有＋36 新）／`executed=688`。

## 四 仍未验证（逐条点名，全为「未验证」）

1. **真实模型对照**：未验证——本刀不触任何模型端点、零真实读数，五型皆为纯内存判定。
2. **生产接线**：未验证——新建五型零生产调用方，`src/Pry.Api`／`Pry.App`／`Pry.Client` 无任何引用；「接进协调器／落库面」按工单 §一 明确要求登记为**未做**（那是另一把刀的活）。
3. **UI 可见行为**：未验证——零界面读数、未起 Avalonia、未点任何控件。
4. **生产库**：未验证——不新增 SQLite 依赖、不碰 `%LOCALAPPDATA%\PryCompanion`；原型全程内存闭环，未落任何库。
5. **与 HDS-007（到期再叙事）的关系**：未验证——本刀只做表 C 片 4 那 5 处缺口的等价格隔离型，未评估与 HDS-007 到期再叙事链的语义交叠或前置。
6. **待标定参数权威值来源**：未验证——`NarrativeInputBuffer` 的封顶（`MaxBatchItems`／`MaxBatchHold`）与 `NarrativeReplyPacing` 的 `ReadingCharactersPerSecond` 等，仓库内查无权威值，按工单 §〇.5.3 一律做成**必填入参**（源码零字面量门槛），真实标定值押到标定轮；本刀未标定、未落任何默认数。

## 五 新建文件清单（路径＋行数＋SHA-256，六枚全为新增）

```
src/Pry.Core/Narrative/NarrativeInputBuffer.cs                  188 行  1e40dfb3f937d894b7bf19d1e20f66912da33df5a112b17baadfe18d97094067
src/Pry.Core/Narrative/NarrativeInterruption.cs                  94 行  fca7209a7163846c3cf60cea6f87cd0ab15d9f7e40b5c6baff0bfae553ec7fdf
src/Pry.Core/Narrative/NarrativeInterruptClassifier.cs           62 行  9f1cfed2a62b3a9187ab48bcd9e9b1a032b988c8d2f67e11796574808961ce13
src/Pry.Core/Narrative/NarrativeBehaviorExpressionMapping.cs    104 行  2bbae3d9922b20db046746abc3edca4d9636fb7bdb1ffa2e8a0fbb9546cc2a61
src/Pry.Core/Narrative/NarrativeReplyPacing.cs                  104 行  f0e826dfb35227903e68b835584a7589813250b25d70f3838a4e6029407e3fd9
tests/Pry.Core.Tests/NarrativeGapParityPrototypeTests.cs        370 行  e7ee86e0950dc127cb6cb103ebfb6cf680b72e42d5b59747d9c7bb301505a07b
合计 5 枚源码 552 行 + 1 枚测试 370 行 = 922 行
```

五枚源码已提交进载体 HEAD `0db4125`；测试文件 SHA 为未变异态基线值（§二无对应突变轮，扫描格与正例皆在基线/全量绿中在册）。本报告随出口读数补齐后单独提交，令载体收尾 `git status --short`＝0。

交付形态（工单 §〇.8 / 规范 §10 四项）：
- 问题与根因：新链在用户可见能力上欠 5 件事（攒批一轮再答、打字时先停、接着说 vs 新话题、行为表达被场景挡、按阅读节奏逐句冒），删除片因此不可起跑。
- 实际改动：五处缺口各做成一个隔离类型＋一组判据（共 36 格），全内存闭环、不接线、不落库、不碰界面。
- 验证结果：解决方案级 build 0 警告 0 错误；全量 1311/0/2（+36＝1275→1311）；定向 8 轮 M1–M6 取到 6 处单枚清洁红、跨全部 5 缺口，M7 与扫描格／防御支登记未取到红（预算 8/8 用尽）。
- 剩余问题：见 §四（真实模型／生产接线／UI／生产库／HDS-007 关系／参数标定皆未验证）；场景挡格与旧链扫描格的单放清洁红待下一把定向额度补。

## 六 守门人收口（2026-10-23 18:3x）：归位字节对账＋线尾副本门禁复采＋补拿那两枚定向清洁红

归位通道（共享主树里不 `cherry-pick`）：载体代码 HEAD `0db412538cffed469d5fc4332471e58066220b18`（父＝起刀点 `bceb806`，lane 当场自取线尾而非工单预写的 `e8a842a`）→ 六枚文件按字节铺回主树 → 主树提交号 `59252aa`（6 files／922 insertions／0 deletions，六枚全为 `create mode`）。铺回前后各核一次 md5：`9bdd329d736a`／`361ab9d6cef8`／`1cbac47250ea`／`39fc923ed7ba`／`797f110aabf3`／`48aa8ec04508` 六枚两侧 SAME。等价性另两条：`git diff --stat 0db4125..HEAD -- src tests` 只多出第十八波那两份测试文件（50＋167＝217 insertions）、反向同值，说明本把除第十八波那 217 行之外代码面零漂移；主树两枚他人 `M ` 暂存项跑前跑后 blob 一字不变（`6a78b81c71636d07…`／`954584e31866abf1…`）。

门禁轮（跑在归位后的线尾 detached 副本 `.local-notes/gate-wave20-1023/carrier`，起刀点＝`59252aa10dd4ea31b9bb48c8d61cec15438e7fcb`，入场 `git status --short`＝0）：
- 焦点预检：每次 `dotnet` 之前各跑一次 `.local-notes/game-preflight6-1022.ps1`，五轮全 `VERDICT=BUILD_OK`（free_memory_gb 依次 10.3／9.0／9.7／9.6／9.5；前台窗口＝QQ，非全屏）。
- 构建一轮：`dotnet build Pry.slnx -c Release` ⇒ **0 个警告 0 个错误**，用时 00:00:10.73。
- 全量一轮：`dotnet test Pry.slnx -c Release --no-build --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/gate-wave20-1023/full" --logger trx` ⇒ 四份 TRX 各归各程序集，逐份 `<Counters>`：Client total 108／executed 108／failed 0；App 233／233／0；Api 280／280／0；**Core 700／698／0**（passed 698）。当场求和＝**1321／executed 1319／failed 0／跳过 2**。两条跳过按 `<UnitTestResult outcome="NotExecuted">` 点名＝`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`、`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`（`notExecuted` 计数器恒 0，不作证据）。
- 等式（由盘上求和、不心算）：108＋233＋280＋700＝**1321**；对上一版在册 1275 的差＝第十八波 10 格＋本波 36 格＝46 ⇒ 1275＋46＝1321 ✓。
- `scripts/measure-fixed-eval.ps1 -ResultsDirectory "…/full"` ⇒ `Catalog valid: 76 permanent scenarios.`、`Passed=1319; Failed=0; Other=2`、exit=0。

补拿那两枚（lane 定向 8/8 用尽后欠下的 §五 那句"待下一把定向额度补"，由守门人这一手取红＝**第二换手**）：
- **M8（缺口 4 场景挡格）**：①文件:行＝`src/Pry.Core/Narrative/NarrativeBehaviorExpressionMapping.cs:79`；②改前原文＝`        if (cue.SceneBlockCodes is { Count: > 0 })`；③改后原文＝同句 `{ Count: > 1 }`；④`git diff --stat`＝`1 file changed, 1 insertion(+), 1 deletion(-)`（此字段按同一配方补取于复绿轮之后，只取 diff、未再跑测试；红名与 `<Counters>` 全部来自 18:29:57 那一轮）；⑤跑法＝`dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/gate-wave20-1023/m8" --logger trx --filter "FullyQualifiedName~NarrativeGapParityPrototypeTests"`；⑥TRX＝`24782_PHYTHLINK_2026-10-23_18_29_57.trx`；⑦恰好一枚红＝`Pry.Core.Tests.NarrativeGapParityPrototypeTests.Mapping_scene_block_yields_a_nameable_rejection_reason_and_no_pass_through`，消息要点＝`Assert.Equal() Failure: Values differ / Expected: BlockedByScene / Actual: Mapped`，红点＝`tests/Pry.Core.Tests/NarrativeGapParityPrototypeTests.cs:line 242`，同轮其余 35 格全绿（total 36／executed 36／failed 1）；⑧还原命令＝`git checkout -- src/Pry.Core/Narrative/NarrativeBehaviorExpressionMapping.cs`；⑨还原后 `git status --short`＝0 行；⑩还原后 SHA-256＝`2bbae3d9922b20db046746abc3edca4d9636fb7bdb1ffa2e8a0fbb9546cc2a61`＝本文件 §五基线值；⑪当轮预检 BUILD_OK（free 9.7）。**这一枚把 §二 M7 那台作废仪器换成能响的配方**：M7 用 `> int.MaxValue` 被编译器常量折叠成 `error CS8518`（TRX total=0，没有红可读），而 `> 1` 只让"恰好一枚场景码"这一档漏放行，移动的正是该格断言的那件事。
- **M9（旧链引用扫描格）**：①文件:行＝`src/Pry.Core/Narrative/NarrativeReplyPacing.cs` 文件尾；②原文末行＝`}`；③追加一行＝`// mutation probe: legacy = new ReplyPlanner();`；④`git diff --stat`＝`1 file changed, 2 insertions(+)`；⑤跑法同 M8、结果目录 `…/m9`；⑥TRX＝`24782_PHYTHLINK_2026-10-23_18_30_19.trx`；⑦恰好一枚红＝`Gap_prototype_sources_carry_no_legacy_chain_references_and_the_probe_proves_the_gate_bites`，消息要点＝`Assert.DoesNotContain() Failure: Sub-string found … (pos 3473) Found: "ReplyPlanner"`，红点＝`tests/Pry.Core.Tests/NarrativeGapParityPrototypeTests.cs:line 339`，同轮 35 格绿（total 36／failed 1）；⑧还原命令＝`git checkout -- src/Pry.Core/Narrative/NarrativeReplyPacing.cs`；⑨0 行；⑩还原后 SHA-256＝`f0e826dfb35227903e68b835584a7589813250b25d70f3838a4e6029407e3fd9`＝§五基线值；⑪BUILD_OK（free 9.6）。这条门确实在守，而且**注释里的 token 也算**（`File.ReadAllText` ＋ `StringComparison.Ordinal` 子串匹配，与本仓 `ExpressionFeedbackSurfaceTests` 同口径）；lane 原有的阳性探针只证明"扫描器看得见"，本枚才证明"门在咬"——两者不是一件事，缺后者这道门就是常绿门。
- 还原后复测轮（两枚同轮复绿，带构建、非 `--no-build`）：`…/m89-restore/24782_PHYTHLINK_2026-10-23_18_30_41.trx` ⇒ total 36／executed 36／failed 0／passed 36。
- `dotnet` 调用只认盘上产物：本轮＝构建日志 1 份＋测试跑次 4 轮（全量 4 份 TRX／定向 M8、M9、复绿各 1 份）＝7 份 TRX 共 4 个跑次。

换手覆盖度如实分级（这一格不许含混）：本波 36 格里 **2 格**（场景挡格、旧链引用扫描格）由守门人这一手拿到"单枚·单轮·清洁红"＝第二换手；**其余 34 格的反证全部出自判据作者自己那一双手**（lane 的 M1–M6），与第十三／十四波那 65 格同类，属"有反证、无换人"。故全仓"反证缺第二换手"的现账＝第十三＋十四波 **65** ＋第二十波 **34** ＝ **99 格**，押在下一把本来就要起定向额度的刀上；第十八波 10 格与第十六＋十七波 31 格已是第二换手。

结构上红不了那一枚照登（不领红、也不许删断言）：`NarrativeReplyPacing.cs:91` 的负数量防御支——入参先 `Validate()`、数量非正在到达该支前就抛，当前形状下该支恒不触发。lane 写成"结构上红不了"，我复核其**承重断言实际落在** `tests/Pry.Core.Tests/NarrativeGapParityPrototypeTests.cs` 的 `Pacing_negative_character_count_is_rejected`（红点会是 `Assert.Throws` 那一行，而不是 `:91`），照此登记、不改该格一字。

在册基线由本把顶上＝**1321／0／2**（Client 108／App 233／Api 280／Core 700，executed 1319）。与既往不同之处要说清：这一格取自带第二换手的四份独立 TRX、跑在归位后的线尾副本上，属于"本来就要起的那一轮"，**不是为顶基线单独起的构建**；旧值 1275 自此过期，别再引用。lane 那一轮的 1311／0／2 只作载体自跑登记，不顶账（它不含第十八波那 10 格）。

生产状态照本宣科：五个类型零生产调用方，本把之后**用户可见行为仍零变化**——发不出消息的那件事没被这一把解决，旧链也仍在生产路径上、仍在打包面里；`构建时不要把旧链路打包进去` 这一条仍未满足，删除片仍不可起跑（缺口 1–5 只是"等价格就位"，接线没做）。
