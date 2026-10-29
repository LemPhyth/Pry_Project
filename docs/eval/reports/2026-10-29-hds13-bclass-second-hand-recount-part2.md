# 波 13 的 B 类剩 9 格·换手复采第二把（第四十波，lane 侧 §〇–§七）

- 工单：`docs/dispatch-hds-wave40-1029.md`（`HDS-WAVE40-1029`）
- 性质：证据刀·换手复采（第三十九波的续跑）。零产品新字节、零判据字节终态、零落地、零顶账。
- 载体：`.local-notes/qa-wave33-1028/carrier`（基点 `fd93ebe`，线尾 `51f149d`），不新建 worktree、不新建产品或测试文件、本刀零 git 写操作。
- 本报告是主树里本刀唯一新建的文件，未提交、未 `git add`。§九 归守门人追加，本刀不写状态词／结案词／在册基线。

## 〇 入场与先数盘（全部当场从盘上取，不引用工单数字）

- 时刻：`date` = `Thu Oct 29 02:37:20 2026`（随后每枚预检另记时刻）
- 主树：`git rev-parse HEAD` = `6ae0803d3016ec7b4237098d183030371f7f6c0c`
- 主树 `git status --short --branch` 原样（入场时 **9 行**＝1 行 branch＋两枚 `M ` 暂存项＋一枚 ` M`＋五枚 `??`；本节落盘后新增的第十行是本报告自己那枚 `??`，登记在 §五.4）：

  ```text
  ## codex/ui-main-window-redesign
  M  docs/dispatch-tip1020-02-memory-group-judgment.md
   M docs/plans/PROJECT_PLAN.md
  M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs
  ?? .qoder/
  ?? docs/character-learning-safety.patch
  ?? docs/research/HDS_INTERLUDE.md
  ?? docs/research/HDS_QQ_RESEARCH_INDEX.md
  ?? docs/research/QQ_BRIDGE.md
  ```

  （以上全部是他人资产：两枚 `M ` 暂存项与全部 `??` 本刀不读内容、不改、不提交、不 `stash`／`clean`／`rm`。本刀之后主树新增的只有本报告一枚未跟踪文件。）
- `git worktree list` 原样共 **76 行**（主树＋75 枚 worktree／carrier），完整存档＝`.local-notes/wave40-1029/out/entry-worktree-list.txt`；与本刀有关的两行：

  ```text
  C:/Users/24782/Desktop/Pry_Project                               6ae0803 [codex/ui-main-window-redesign]
  C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave33-1028/carrier  51f149d (detached HEAD)
  ```

- 主树 `git log --oneline -3` 的三枚 SHA 逐字符＝`6ae0803d3016ec7b4237098d183030371f7f6c0c`／`0a9ce00…`／`1d2c033…`；subject 原文各长达数千字（波 39／38 的守门人裁决正文），此处只取 `%h` 与逐字符前缀，全文可由 `git log -3` 当场取。（登记：这不是"读数"，是指令性文本，本刀不复制粘贴以免污染。）

### 〇.3 入场自证五件（载体）

| 件 | 命令 | 读数 | 判据 |
|---|---|---|---|
| ① | `git -C <carrier> rev-parse HEAD` | `51f149d0b722e28a6566c93d19f48a94ca4c85df` | ＝线尾 `51f149d` ✔ |
| ② | `git -C <carrier> status --short` | **0 行** | ✔ |
| ③ | `git -C <carrier> log --oneline fd93ebe..HEAD` | **2 行**＝`51f149d`＋`1e02205` | ✔ |
| ④ | `git -C <carrier> diff --stat fd93ebe HEAD -- src tests scripts Pry.slnx Directory.Build.props` | `6 files changed, 822 insertions(+)`（**0 deletions**，六份文件名逐字＝`DeliveryAttribution.cs`／`ExperienceMaterial.cs`／`LegacyInheritance.cs`／`NarrativeAuthority.cs`／`StateLayering.cs`／`StateAndLearningPrototypeTests.cs`） | ✔ |
| ⑤ | `git -C <main> diff --stat 51f149d 6ae0803 -- src tests scripts Pry.slnx Directory.Build.props`（右端当场 `rev-parse HEAD` 取） | **0 行** | ✔ ⇒ 载体树与主线归位后的产品／判据字节逐字符相同 |

### 〇.4 六枚文件入场 SHA-256（三侧各当场取一次，逐字符相同才起跑）

| 文件 | 主树工作副本 | 载体工作副本 | `git show 51f149d:` 的 blob | 三侧 |
|---|---|---|---|---|
| `src/Pry.Core/Narrative/NarrativeContextCompiler.cs` | `86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842` | 同值 | 同值 | MATCH |
| `src/Pry.Core/Narrative/NarratorContracts.cs` | `3a33a8151e61c3396963dac451dffcb46acb2f02807f140d18f694d6a52a9f8c` | 同值 | 同值 | MATCH |
| `src/Pry.Core/Narrative/NarrativeRoundRunner.cs` | `961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9` | 同值 | 同值 | MATCH |
| `tests/Pry.Core.Tests/NarratorPrototypeTests.cs`（判据） | `4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4` | 同值 | 同值 | MATCH |
| `tests/Pry.Core.Tests/NarrativeCommitFactoryTests.cs` | `fcee461164e39bae172c49149d39076e67aa36e844089afa4bcf4ff06d7cea0b` | 同值 | 同值 | MATCH |
| `tests/Pry.Core.Tests/NarrativeBudgetInequalityCellTests.cs` | `192bbaecb5d2a3f3bafd08d8cbe5fdacf65ee71a7ad7856798345eae6c3f7686` | 同值 | 同值 | MATCH |

判据基准＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`，本刀五轮一枚都不写判据，全程必须等于它。原始打印＝`.local-notes/wave40-1029/out/w40-selfproof.txt`（件5 面）。

### 〇.5 在册基线起点（从波 39 报告 §四当场读，不从工单抄）

波 39 报告 §四 的 F 读数＝**total 1406／executed 1404／failed 0／跳过 2**（四份 TRX 求和 108＋280＋233＋785），跳过两枚＝`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`、`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`。本刀净新增判据格 0 ⇒ F 出口读数应当仍是 1406；读到别的数先怀疑树没还原干净，不改判据、不改基线、照实登记并作废那一轮。**本刀不顶账。**

### 〇.6 本刀坐标系（当场从盘上产物重取，不抄工单）

- 波 39 报告守门人 §九.3 的求和式：波 22 §三.2 的 65 格四类计数＝A 0／B 35／C 0／D 30；波 13 的 19 枚 B 类（#1–11、#13–15、#22、#24–27）由波 39 签 10 枚 ⇒ 波 13 余＝47−10＝37、波 20 余＝28，旧欠 75 → **65**（37＋28＝65；另一路 75−10＝65）。
- 波 13 的 B 类现值＝19−10＝**9 枚**＝#24–27（4 枚，押本刀 M8′）＋#7–11（5 枚，押本刀 m07–m10）。本刀最多收 9 枚；收不完照实报。
- 本刀五轮要签的格号（按波 13 审计报告 §三.1 的行，逐枚当场读）：#7＝`Compiler_drops_an_optional_section_that_exceeds_its_own_share_and_counts_it_exactly`、#8＝`Compiler_trims_recent_messages_only_at_whole_unit_boundaries`、#9＝`Compiler_shrinks_the_memory_wallet_unit_by_unit`、#10＝`Compiler_removes_cross_conversation_units_and_names_the_count`、#11＝`Compiler_keeps_absent_sections_as_not_supplied_instead_of_dropping_them`、#24＝`Malformed_response_is_not_masquerading_as_a_chosen_silence`、#25＝`A_provider_that_ignores_cancellation_still_exits_as_timeout`、#26＝`A_provider_that_honours_the_timeout_token_exits_as_timeout_too`、#27＝`Provider_failure_becomes_a_named_error_shape_and_writes_nothing`。

### 〇.7 取数仪器先自证再用（口径⓪；零 dotnet）

仪器＝`.local-notes/wave40-1029/w40-count.js`（lane 从零写）。四条口径照工单 §一.4：只数 `<UnitTestResult` 开标签的 `outcome="Failed"` 并与同份 `<Counters failed>` 双向对账；反向锚定（回溯最近 `<` 判标签名）作第二条走法互查；属性按名字取；栈帧用 `([\w.\-]+\.cs):(?:line |：行 )(\d+)` 不锚 `at `；红名先剥全限定前缀与参数表再回判据验"方法名＋`(`"，正则同时认 `public void` 与 `public async Task`；跳过格只认 `outcome="NotExecuted"` 开标签。

自证面（20 份盘上老读数当场重取，**不引用任何报告的转述**）＝`.local-notes/wave40-1029/out/instrument-selfproof-A.txt`＋`instrument-selfproof-B.txt`，两次运行结论均 **AGREE（问题面 0 处）**：

| 面（四元限定＝波次＋判据文件＋跑法＋该面字段值） | 盘上读数 | 与工单 §一.5 那张表 |
|---|---|---|
| 波 39 `trx-M1`／`M3`／`M4`／`M5`／`M6`／`M7`／`M9`，`NarratorPrototypeTests.cs`，定向 56 格树 | 各 `nodes=56 total=56 failed(开标签)=failed(counters)=1`、`notExec` 开标签 0、红名逐字符＝波 39 §三.10 | 相符 |
| 波 39 `trx-M2`，同上 | 恰 3＝3，三枚名相符 | 相符 |
| 波 39 `trx-M8-voided-P103`，同上 | 恰 **5**＝5：四枚目标＋`Provider_side_cancellation_with_a_live_host_token_is_named_timeout_not_failure`（帧 `:865`），五枚消息族同为 `Assert.Equal() … Expected: ProviderRejected / Actual: Silence` | 相符（本刀 M8′ 事前论证的来源面，**不是本刀凭证**） |
| 波 39 `trx-F`，解决方案级全量 | 4 份求和 `total=1406 executed=1404 passed=1404 failed=0`、`notExec` 开标签 2 枚同名、Core 那份 `785/783` | 相符 |
| 波 13 `run-m01`／`m02`／`m14`，旧 49 格树 | 恰 1／3／4 | 相符 |
| 波 13 `run-m07`／`m08`／`m09`／`m10`，旧 49 格树 | **1／2／1／1**（＝旧树读数；本刀在 56 格树上的预期不从这四面平移） | 相符（工单写"各恰 1"，`run-m08` 盘上是 **2**——与 §三.1 审计报告"红集 2 格"一致；差额登记在 §六，本刀把它当"旧树面"用而不当当前预期） |
| 波 13 `run-full`，解决方案级全量 | 4 份求和 `total=1228 executed=1226 failed=2`，两枚红＝`ResourceLimitLedgerCoverageTests.Every_limit_shaped_constant_has_a_row_in_the_ledger`（帧 `:50`）＋`…The_guard_sees_a_row_being_dropped_and_a_row_drifting_off_its_line`（帧 `:110`），Core 那份 `607/605`、`notExec` 开标签 2 枚同名 | 相符（工单 §一.5 已按波 39 守门人 §九.0 改正为 failed 求和＝2） |
| 波 38 `trx-M5c`／`S1`／`S2`，`StateAndLearningPrototypeTests.cs`，定向 26 格树 | 各恰 1＝1、三枚红名不属于本工单任何一枚格 | 相符 |

仪器修过的一处（如实登记）＝第一版把 `ResourceLimitLedgerCoverageTests` 这枚 Api 侧判据文件漏在回判表外，`run-full` 那两枚红的"回判据验方法名"一面读成 `UNKNOWN-FILE`（红数／消息／帧三面全对）⇒ 按"先修仪器不修读数"补上该文件后整面 AGREE；两处运行输出都留在盘上（修前那次的 PROBLEM 行在 `instrument-selfproof-B.txt` 的第一段，未删）。

### 〇.8 件 1／件 2／件 4／件 5 的独立复算成品（零 dotnet，先档后跑）

- **件 1 逐枚并排自证**（`.local-notes/wave40-1029/w40-selfproof.js` ⇒ `out/w40-selfproof.txt`）：五枚 `mutation-prep/` 文本对 `git show 51f149d:<path>` 的 blob 逐枚 diff ⇒ 实际差异行恰＝声明的那一行（`[115]`／`[203]`／`[246]`／`[239]`／`[284]`）；OLD 全文出现次数各＝1、不跨行；声明行原文===OLD 逐字符 `true`；行数变化 0；突变文本 SHA-256 与字节数与守门人 `out/gk40-prep.txt` 声明逐字符同值（`0400a577…`/16759、`24e90d81…`/24443、`a65399bb…`/24436、`dfc91733…`/24399、`24dbd7e5…`/24453）。
- **件 4 静态语法判读**：五枚逐枚净括号偏移守恒——M8′ 行 `(0,0)→(0,0)`、m07 `(1,1)→(1,1)`、m08 `(1,1)→(1,1)`、m09 行 `(2,2)→(1,1)` 净 0→0 且全文件圆括号 `97/97→96/96`（成对删除，净偏移守恒）、m10 `(0,0)→(0,0)`；全文件大括号收支未变（K＝94/94，R＝53/53，仪器按字符串态剥离后计数）。⇒ 无一枚不闭合。
- **件 2 枚举（按盘上算术自取，不转述"新增 N 枚"）**：`[Fact]`=53、`[Theory]`=1、`[InlineData]`=3 ⇒ **格数 53＋3＝56**；带属性方法＝54 枚（签名两族 `public void` 24＋`public async Task` 30）；另有 2 枚不带 `[Fact]/[Theory]` 的夹具方法＝`FakeNarrator.ProposeAsync`@130 与 `SelfCancellingNarrator.ProposeAsync`@874（它们与 30 枚测试方法同形，所以裸串 `public async Task` 全文命中 32 处，扣掉这 2 枚夹具才是 30——这一处差 2 就是"按串数"与"按带属性的方法数"两种算法的分野，本刀按后者）；判据文件行数＝989＋行尾 LF。逐枚格的可达路径（`Compile`／`RunAsync`／`Validate`／`Consume`／只读装配体属性面）全表已打印在 `out/w40-selfproof.txt` 末段。
- **件 3′ 候选连坐面（独立复算，不引用工单清单）**：只扫 `Assert.` 行、出现 `NarrativeSegmentDisposition.Dropped|NotSupplied|PartiallyIncluded|DroppedSegmentCount|NotSuppliedSegmentCount|DroppedUnitCount|CrossScopeUnitCount|BudgetDroppedUnitCount|IncludedUnitCount|DispositionReason|EmittedText` 任一枚者 ⇒ **恰 10 枚**＝@198、@210、@266、@281、@298、@313、@913、@934、@952、@975，与守门人 10-29 扫描所得逐枚同名同起始行（本次复算与工单清单一致，且是我自己从盘上取的）。另按 `Assert.` 行扫 `NarrativeRoundDisposition.ProviderRejected` ⇒ 恰 5 处＝断言@540→格@534、@555→@549、@569→@562、@582→@574、@865→@851；全仓再无第二枚格读这个枚举值（`grep -c`＝5）。
- **件 5 判据字节三面自证**：入场三侧同值（§〇.4）；起跑前与每轮还原后各 `sha256sum` 一次，五轮 10 次＋出场面必须逐字符＝`4f1b2f6e…`（读数见 §三）。

### 〇.9 写入通道的守卫实证（零 dotnet，真跑出报错，不是只声称）

`.local-notes/wave40-1029/w40-apply.js` ＝本刀唯一写载体产品文件的通道。三面试探全部当场跑（整段输出＝`out/guard-proof.txt`）：

| 面 | 尝试 | 结果 | 目标文件 SHA 前后 |
|---|---|---|---|
| (a) 硬拒 `tests/**` | 合法 ID `W40-M8p` 的产品文本，`--target=<carrier>/tests/Pry.Core.Tests/NarratorPrototypeTests.cs` | **REFUSED/ABORT [GUARD-TARGET]**，exit=1，未写一字节 | 判据 before＝after＝`4f1b2f6e…`（同值） |
| (b) 硬拒手写字节 | 合法 ID `W40-m07`，`--src=` 一枚手敲副本（同一行门、行尾多一句注释，字节数 24525≠24443） | **REFUSED/ABORT [GUARD-SRC-BYTES]**，exit=1，未写一字节 | `NarrativeContextCompiler.cs` before＝after＝`86c30160…`（同值） |
| (c) 正面控件 | 合法 ID `W40-m07` ＋ 合法预合成文本 ＋ 合法产品文件 | **APPLIED**（写后 SHA＝`24e90d81…` 恰等于声明值）⇒ 同场 `REVERTED`（SHA 回 `86c30160…`，`sameAsBlob=true`） | 载体 `status --short`＝**0 行**；五枚目标全部 `== blob(clean)` |

## 一 五轮表与件 3′ 闭合点名表（**跑前写；实测值不许回头改本节的任何预期列**）

跑序固定＝M8′→m07→m08→m09→m10→全部还原并核六枚 SHA→F。

### 一.0 五轮表（行号＝`51f149d` 侧现值，由 `w40-selfproof.js` 当场打印）

| 轮 | 文件:行 | 改前整行 → 改后整行 | 突变文本 SHA-256 | 本刀要领账的格（波 13 格号） | **本刀闭合点名的预期红集（事前，枚数）** |
|---|---|---|---|---|---|
| M8′ | `NarrativeRoundRunner.cs:115` | `                Disposition = NarrativeRoundDisposition.ProviderRejected, Assembly = assembly, Outcome = outcome,` → 同行 `ProviderRejected` 改 `Silence` | `0400a57714bbdd8e8d642e78daaa5e9ceaf79c927029c63a06d5d0daaf186c08` | #24 @534＋#25 @549＋#26 @562＋#27 @574 | **恰 5 枚**＝@534、@549、@562、@574、@851（守门人事前点名；本刀独立复算同值，见 §一.1） |
| m07 | `NarrativeContextCompiler.cs:203` | `        if (cost > cap \|\| used + cost > total)` → `        if (used + cost > total)` | `24e90d81e2d0c6be5a54786ab13c4d4f32373bfafd7455ed9e0bc631f5f10a43` | #7 @210 | **恰 2 枚**＝@210、@913（见 §一.2） |
| m08 | `NarrativeContextCompiler.cs:246` | `            if (wallet - cost < 0 \|\| used + cost > total)` → `            if (used + cost > total)` | `a65399bbbe78c9241d266c57898b01707f10b2a865b9ed8db8b25391da37f1cb` | #8 @281＋#9 @313 | **恰 4 枚**＝@281、@313、@952、@975（见 §一.3） |
| m09 | `NarrativeContextCompiler.cs:239` | `            if (!string.Equals(unit.ConversationId, request.ConversationId, StringComparison.Ordinal))` → `            if (unit.ConversationId is null)` | `dfc917334c7f48941ffc8e29e3a7735f283e60b78c3d9445cc25798020eb91e5` | #10 @298 | **恰 2 枚**＝@298、@952（见 §一.4） |
| m10 | `NarrativeContextCompiler.cs:284` | `            BudgetTokens = cap, UsedTokens = 0, Disposition = NarrativeSegmentDisposition.NotSupplied,` → 同行 `NotSupplied` 改 `Dropped` | `24dbd7e558ab4362ded03fd2de34e2833da90f0541286ca2dab62f1f0c4d14ce` | #11 @198 | **恰 2 枚**＝@198、@934（见 §一.5） |

算术底座（`ConservativeTokenEstimator.Count`＝`Encoding.UTF8.GetByteCount`，`src/Pry.Core/Memory/MemoryContracts.cs:114`；`ContextBudget` 默认值＝`ContextTokens 4096／OutputTokens 512／SafetyTokens 128／RecentTokens 1400／MemoryTokens 480／StateTokens 240／BehaviorTokens 200／ExpressionTokens 160／CapabilityTokens 240／AdvisoryTokens 2400`，`src/Pry.Core/Prompting/ContextBuilder.cs:6-8`）：

- 统一输入预算 `total = 4096 − 512 − 128 = 3456`。
- 夹具必需前奏成本 `PreludeCost = bytes("Pry canon\njson rules") + 8 + bytes("hello there") + 8 = (9+1+10) + 8 + 11 + 8 = 47`（`MessageFramingOverhead = 8`）。
- 各夹具串的 UTF-8 字节数：`"\nState：\n"`＝1+5+3+1＝10；一个 CJK 字＝3 字节；ASCII 一字＝1 字节；单元成本＝`bytes(content) + 8`。
  - m07 面：`State` 40 个 `x` ⇒ `cost = 10 + 40 = 50`；@913 的 `State "地点：画室"` ⇒ `25`（cap 240）；`BehaviorReferences "先应一声再展开"` ⇒ `bytes("\nBehaviorReferences：\n") + 21 = (1+17+3+1) + 21 = 43`（cap 10）。
  - m08 面：`"0123456789"`/`"abcdefghij"`/`"KLMNOPQRST"` 各 10 ASCII ⇒ 单元成本 `10+8 = 18`；`"aaa"/"bbb"/"ccc"` 各 3 ASCII ⇒ `3+8 = 11`。
  - m09 面：`room-2` 那枚单元的 `ConversationId` 逐字符不等于 `request.ConversationId = "room-1"`（夹具常量 `Conversation`）。
  - m10 面：`NotSupplied()` 的调用点＝`:138`（Advisory 缺席）／`:197`（整段缺席）／`:225`（单元序列为空）。

### 一.1 M8′ 闭合点名表（`NarrativeRoundRunner.cs:115`）

机理（从盘上行号读通）：`:110` 的 `if (outcome.Kind != NarratorOutcomeKind.DraftProduced || outcome.Draft is null)` 是**唯一**通往 `:115` 的入口，命中即在 `:113-117` 返回，即时支（`:150`）与延迟支（`:155`）在其后不可达；`:115` 那枚 `NarrativeRoundDisposition.ProviderRejected` 常量被改成 `Silence` ⇒ 凡是"provider 没产出草稿"这一族的见证者都会在同一枚断言上同时翻。

| 预期红（5 枚） | 夹具如何走到 `:110` | 翻掉的断言（行号） | 预期消息族／形状 |
|---|---|---|---|
| `Malformed_response_is_not_masquerading_as_a_chosen_silence`@534 | `FakeMode.Malformed` ⇒ `NarratorOutcome.MalformedResponse` | `:540 Assert.Equal(ProviderRejected, result.Disposition)` | `Assert.Equal() … Expected: ProviderRejected / Actual: Silence` |
| `A_provider_that_ignores_cancellation_still_exits_as_timeout`@549 | `FakeMode.HangsPastTimeout` ⇒ `InvokeOnceAsync` 竞速超时 ⇒ `Kind=TimedOut` | `:555`（同上） | 同上 |
| `A_provider_that_honours_the_timeout_token_exits_as_timeout_too`@562 | `FakeMode.CancelsOnToken` ⇒ `catch (OperationCanceledException)` ⇒ `Kind=TimedOut` | `:569`（同上；该格第一条断言就是 `:568` 的 `Outcome.Kind`，仍应为 `TimedOut`，不受被改常量影响） | 同上 |
| `Provider_failure_becomes_a_named_error_shape_and_writes_nothing`@574 | `FakeMode.Throws` ⇒ `catch (Exception)` ⇒ `Kind=ProviderFailed` | `:582`（同上） | 同上 |
| `Provider_side_cancellation_with_a_live_host_token_is_named_timeout_not_failure`@851（波 18 补格，**本刀不领它的账**，只登记同场响） | `SelfCancellingNarrator` 内部令牌取消、宿主令牌与 5 s 超时界都活着 ⇒ `:332-335` ⇒ `Kind=TimedOut` | `:865`（同上；`:862`–`:864` 三句先过） | 同上 |

判不连坐（逐枚点名，51 枚）：

- **同一族里"读 Outcome.Kind／NarratorCallCount／Ledger"的其余断言不新增红枚**：@541、@556、@557、@568、@580、@581、@862–`:864` 都在同一枚格内、被同一枚格的第一条失败断言终止，不另计格。
- 断言面**根本没出现 `NarrativeRoundDisposition.ProviderRejected`** 的格全不连坐——按件 3′ 的盘上扫描，该枚举值的 `Assert.` 行全文恰 5 处（`:540/:555/:569/:582/:865`），这是"为什么不连坐"的硬凭据：其余 51 枚格的断言面不读这枚常量。
- 走到 `:110` 之外分支的 RunAsync 格＝@381、@456、@471、@488、@503、@520、@599、@614、@628、@644、@658、@671、@684、@698、@711、@722、@735、@746、@761、@776、@790、@802、@832：夹具都是 `FakeMode.Produce`（或 @520／@599／@614 之类合法草稿但契约拒），`outcome.Kind == DraftProduced` ⇒ `:110` 为假 ⇒ 不经 `:115`，且它们断言的是 `Silence/Deferred/Immediate/ContractRejected/HistoryRejected` 分支值，未被改动。
- @394 `Round_does_not_call_the_provider_when_the_context_is_rejected`：在 `:95-101` 的 `ContextRejected` 早返回，`NarratorCallCount=0`，同样不经 `:115`。
- @587 `Host_cancellation_stays_cancellation_and_is_not_converted_into_a_round`：宿主令牌预先取消 ⇒ `:328-331` 往上 `throw`，整格由 `Assert.ThrowsAnyAsync` 收，`run` 根本不返回结果对象 ⇒ 不经 `:115`。
- @408／@420／@437 三枚（`Consume`／`Validate`／Theory×3）：不构造 `NarratorRoundResult`，不经 `:110`。
- A 组 15 枚（@183…@367）＋波 18 的 @890／@913／@934／@952／@975：只调 `Compile` 或只读装配体属性面，`NarrativeRoundRunner.cs` 一行都不执行。
- `result.Accepted` 这个派生属性（`:52-53` 会把 `Silence` 读成 accepted）——按 `grep -n "result\.Accepted"` 盘上 0 命中（四枚 `Accepted` 断言分别读 `grant.Accepted`@169/@855 与 `assembly.Accepted`@187/@340）⇒ 没有第二族见证者。
- 消息族形状与红点帧：波 39 的作废轮 `trx-M8-voided-P103` 已把这一族的 5 枚名／帧／消息逐字符落盘，本刀把同一枚文本（SHA 同值 `0400a577…`）再跑一次；**凭证只认本刀在载体里跑出来的那份 TRX**，波 39 那面只当事前论证来源。
- 判定门＝**恰 5**：实到不等于 5 ⇒ 整轮作废、四枚目标不签、不追第二轮（照波 39 守门人 §九.1 的处理）。

### 一.2 m07 闭合点名表（`NarrativeContextCompiler.cs:203`，`AddSection` 的"超自身份额"半边）

被改的是 `AddSection` 里的合取第一支 `cost > cap`。`AddSection` 只服务 4 个 `SectionOrDrop` 段＝`State`／`BehaviorReferences`／`ExpressionPatterns`／`CapabilityResult`（`:143-144`、`:159-167`）；`Advisory` 走 `:110`、`RecentMessages`／`Memory` 走 `AddUnitRun`，都不经这一行。`content` 为空白时在 `:195-199` 早返回（走 `NotSupplied()`），`:203` 不可达。⇒ 能观察这一行的格，夹具必须给这四段之一喂非空内容。盘上扫 `Payloads(... state:/behavior:/expression:/capability:` 的格恰 3 枚＝@210、@328、@913。

| 枚 | 夹具推演（改前／改后） | 判定 |
|---|---|---|
| @210 `Compiler_drops_an_optional_section_that_exceeds_its_own_share_and_counts_it_exactly` | `StateTokens:20`、`state=40 个 'x'` ⇒ `cost=50`。改前 `50>20` ⇒ `Dropped`＋`"budget"`；改后只看 `used+cost = 47+50 = 97 ≤ 3456` ⇒ **`Included`** | **红**：第一条断言 `:217 Assert.Equal(Dropped, section.Disposition)` 即翻（消息族 `Assert.Equal() … Expected: Dropped / Actual: Included`） |
| @328 `Compiler_honours_the_unified_budget_inequality_with_exact_numbers` | `state="地点：画室"` ⇒ `cost=25`，`StateTokens` 默认 240。改前 `25>240` 假、`47+25=72>3456` 假 ⇒ `Included`；改后同 `Included`；`behavior/expression/capability` 全缺席 ⇒ `:203` 根本不可达 | **不红**：被改行在这枚格的执行路径上是**同值合取**（另一半已决定结果），装配体 `used` 与断言的 `expected` 求和逐字节不变 |
| @913 `Whole_section_segments_carry_their_own_source_reference_even_when_dropped` | `BehaviorTokens:10`、`behavior="先应一声再展开"` ⇒ `cost=43`；进 `AddSection(BehaviorReferences)` 时 `used = 47 + 25(State 已含) = 72`。改前 `43>10` ⇒ `Dropped`＋`"budget"`；改后 `72+43=115 ≤ 3456` ⇒ **`Included`** | **红**：`:923 Assert.Equal(Dropped, behavior.Disposition)` 翻（同族消息）。同格 `:919/:920`（State 支）先过——State 那枚不超自身份额，不受被改半边影响 |

判不连坐（其余 53 枚，逐组点名）：

- A 组另 12 枚（@183、@198、@225、@237、@247、@256、@266、@281、@298、@313、@347、@357、@367 —— 13 枚，除 @210／@328／@913 外）：夹具 `state/behavior/expression/capability` 四段全为默认空串（`Payloads` 默认 `""`）⇒ `AddSection` 在 `:195` 早返回，`:203` 不可达。其中 @266 走 `Advisory` 的 `:110`（另一行门），@281／@298／@313 走 `AddUnitRun`，@225／@237／@347／@357／@247／@256 在 `:52-:91` 的失败门就 `return Fail`，装配根本没进行到 `AddSection`。
- B 组 7 枚＋C 组 10 枚＋D 组 10 枚＋E 组 5 枚（RunAsync 族）＋波 18 的 @832／@851／@890／@934／@952／@975：RunAsync 族的夹具全部 `Payloads()` 或 `Payloads(advisory:"decide")` ⇒ 四段 `SectionOrDrop` 内容皆空 ⇒ `:203` 不可达；@832／@851 同；@890 只 `new NarrativeContextAssembly{…}` 手装装配体不调 `Compile`；@934／@975 只给 `recent`，@952 只给 `recent`，@913 之外无 `state/behavior` 内容……逐枚判完，无遗漏。
- 本刀 m07 的凭证面＝**恰 2 枚**；@210 是要领账的 #7，@913 是波 18 补格的连坐（不领波 13 的账）。实到不等于 2 ⇒ 整轮作废、#7 不签。

### 一.3 m08 闭合点名表（`NarrativeContextCompiler.cs:246`，`AddUnitRun` 的"自身份额钱包"半边）

被改的是 `AddUnitRun` 里合取的第一支 `wallet - cost < 0`（钱包＝该节自身份额，逐条递减）。`AddUnitRun` 只服务 `RecentMessages`（cap=`RecentTokens`，`:147-149`）与 `Memory`（cap=`MemoryTokens`，`:153-155`）；`units.Count == 0` 时在 `:223-:227` 早返回走 `NotSupplied()`。⇒ 能观察这一行的格，夹具必须给 `recent` 或 `memory` 非空序列。盘上扫 `Payloads(… recent:/memory:` 的格恰 7 枚＝@281、@298、@313、@328、@367、@952、@975。

| 枚 | 夹具推演（改前／改后） | 判定 |
|---|---|---|
| @281 `Compiler_trims_recent_messages_only_at_whole_unit_boundaries` | `RecentTokens:30`，3 枚 10-ASCII 单元（各 `cost=18`），`used` 进节＝47。改前：`u1 30−18=12≥0` 收入（`used=65,wallet=12,inc=1`）；`u2 12−18=−6<0` ⇒ `break`，`budgetDropped=3−1−0=2` ⇒ `PartiallyIncluded`＋`"unit_budget"`＋inc 1／drop 2。改后只看 `used+cost>total`（65、83、101 全 ≤ 3456）⇒ 三枚全收 ⇒ `inc=3, dropped=0` ⇒ **`Included`** | **红**：`:289 Assert.Equal(PartiallyIncluded, …)` 翻 |
| @313 `Compiler_shrinks_the_memory_wallet_unit_by_unit` | `MemoryTokens:30`，2 枚 10-ASCII 单元。改前：`u1 30−18=12` 收；`u2 12−18<0` break ⇒ `budgetDropped=1`，inc 1／drop 1／`"unit_budget"`。改后两枚全收 ⇒ `inc=2, dropped=0, Included` | **红**：`:320 Assert.Equal(1, section.IncludedUnitCount)` 翻（Actual 2） |
| @952 `Unit_run_losing_units_to_both_reasons_names_both_together` | `RecentTokens:20`，`[input:104(18), other:1 room-2(18), input:103(18)]`。改前：`u1 20−18=2` 收；`u2` 跨会话 ⇒ `crossScope=1 continue`；`u3 2−18<0` break ⇒ `budgetDropped=3−1−1=1` ⇒ `PartiallyIncluded`＋`"cross_scope_and_unit_budget"`。改后：`u1` 收、`u2` 仍 `crossScope=1`、`u3` 只看总量 `65+18=83 ≤ 3456` ⇒ **也收** ⇒ `inc=2, crossScope=1, budgetDropped=0, dropped=1` ⇒ Disposition 仍 `PartiallyIncluded`，理由串变 **`"cross_scope"`** | **红**：`:960` 的 Disposition 恰好**仍过**（`inc≠0 且 dropped>0`），红在 `:961 Assert.Equal("cross_scope_and_unit_budget", reason)` ⇒ 消息族 `Expected: "cross_scope_and_unit_budget" / Actual: "cross_scope"` |
| @975 `Unit_run_that_fits_no_unit_at_all_is_dropped_and_named_budget` | `RecentTokens:5`，1 枚 10-ASCII（`cost=18`）。改前：`5−18=−13<0` 第一条即 break ⇒ `inc=0, budgetDropped=1` ⇒ `Dropped`＋`"budget"`。改后：`47+18=65 ≤ 3456` ⇒ **收入** ⇒ `inc=1, dropped=0` ⇒ `Included` | **红**：`:981 Assert.Equal(Dropped, …)` 翻 |
| @298 `Compiler_removes_cross_conversation_units_and_names_the_count` | `RecentTokens` 默认 **1400**，3 枚（`aaa`/`bbb` room-2/`ccc`，同会话两枚各 `cost=11`）。改前：`wallet 1400−11−11` 远不为负、`used` 69/80 ≤ 3456 ⇒ `inc=2, crossScope=1, budgetDropped=0` ⇒ `PartiallyIncluded`＋`"cross_scope"`；改后：钱包半边本来就没参与判决 ⇒ **逐字段同值** | **不红**（判不连坐的理由＝夹具份额足够大，`wallet - cost < 0` 这一支在改前也不成立，删掉它不改变任何一条断言的读值） |
| @328 `Compiler_honours_the_unified_budget_inequality_with_exact_numbers` | `recent=[input:101 "hello there"]`（`cost=19`，cap 1400）、`memory=[memory:9 "旧事实一条"]`（`cost=15+8=23`，cap 480）。改前两支都不成立 ⇒ 全收；改后全收 ⇒ `used` 求和逐字节同 | **不红**（同上：钱包这一支在该夹具下恒不成立） |
| @367 `Compiler_carries_source_references_into_every_emitted_section` | `recent=[input:101 …]` 单枚，cap 默认 ⇒ 钱包不 binding；`EmittedSourceRefs` 的 4 枚引用不随 Disposition 变 | **不红** |

其余 49 枚判不连坐＝RunAsync 族与 A 组其余格的 `recent`／`memory` 全为默认空序列 ⇒ `:223` 早返回，`:246` 不可达；@890 不调 `Compile`；@913 只喂 `state/behavior`（走 `AddSection` 不走 `AddUnitRun`）；@934／@851／@832 等只读别的字段。逐枚对照 §一.0 的可达表（`out/w40-selfproof.txt`）。

本刀 m08 的凭证面＝**恰 4 枚**＝@281（#8）＋@313（#9）＋@952、@975（波 18 连坐）。与波 13 旧树 `run-m08` 的"2 枚"差 2 枚，差的就是波 18 补的那两枚——这正是工单 §三 首部禁止"按旧树红集平移"的那件事。实到不等于 4 ⇒ 整轮作废。

### 一.4 m09 闭合点名表（`NarrativeContextCompiler.cs:239`，跨会话整条剔除的门）

改前＝`!string.Equals(unit.ConversationId, request.ConversationId, Ordinal)`（不等即 `crossScope++; continue`）；改后＝`unit.ConversationId is null`——`NarrativeContextUnit.ConversationId` 是非空 `string` 位置参数、`Validate()` 还要求非空白，五枚夹具单元全都非 null ⇒ **改后条件恒为假** ⇒ 跨会话单元不再被剔除，而是进入 `:245-:250` 的成本门并计入 `included`／`emitted`／`refs`。可达集合＝§一.3 那 7 枚给 `recent`／`memory` 的格。

| 枚 | 夹具推演 | 判定 |
|---|---|---|
| @298 `Compiler_removes_cross_conversation_units_and_names_the_count` | `recent=[input:103 "aaa", other:1 "bbb"(room-2), input:101 "ccc"]`，`RecentTokens` 默认 1400。改前 `inc=2, crossScope=1, dropped=1` ⇒ `PartiallyIncluded`＋`"cross_scope"`；改后三枚同路（各 `cost=11`，`used=47→58→69→80`，钱包与总量都不 binding）⇒ `inc=3, crossScope=0, dropped=0` ⇒ **`Included`** | **红**：`:304 Assert.Equal(PartiallyIncluded, …)` 翻（其后 `:305/:306` 的 `"cross_scope"`／`CrossScopeUnitCount=1` 也一并翻，同格不计第二枚） |
| @952 `Unit_run_losing_units_to_both_reasons_names_both_together` | `RecentTokens:20`，`[input:104 "0123456789"(18), other:1 "abcdefghij"(room-2,18), input:103 "KLMNOPQRST"(18)]`。改前＝§一.3 那套（`inc=1, crossScope=1, budgetDropped=1, "cross_scope_and_unit_budget"`）；改后：`u1` 收（`wallet 20−18=2, used=65, inc=1`）、`u2` **不再 continue** ⇒ `2−18<0` 触发预算支 ⇒ `break`＋`budgetDropped=3−1−0=2` ⇒ `inc=1, crossScope=0, budgetDropped=2, dropped=2` ⇒ Disposition 仍 `PartiallyIncluded`，理由串变 **`"unit_budget"`** | **红**：`:961 Assert.Equal("cross_scope_and_unit_budget", reason)` 翻（Expected 那串／Actual `"unit_budget"`）；`:963` 的 `CrossScopeUnitCount=1` 同格内也翻但不另计枚 |
| @281／@313／@975 | 三枚夹具的 `recent`／`memory` 单元**全部**落在 `Conversation="room-1"`＝`request.ConversationId`。改前 `!Equals` 恒假 ⇒ 不 continue；改后 `is null` 恒假 ⇒ 不 continue。两支逐单元走同一条路径，`wallet`／`total` 推演与 §一.3"改前"列完全一致 | **不红**（判不连坐的理由＝条件只对"跨会话单元"生效，这枚夹具里没有跨会话单元） |
| @328／@367 | `Unit(...)` 单参重载 ⇒ `conversation` 取夹具默认 `Conversation`；同上恒不走 continue 支 | **不红** |
| @256 `Compiler_refuses_a_current_event_from_another_conversation` | 它改的是 `currentEventConversation: "room-2"`，走 `:77-:82` 的必需前奏门（`payloads.CurrentEvent.ConversationId` 与 `request.ConversationId` 比对），**不是** `:239` 那行；且该格在 `:80` 就 `return Fail` | **不红**（同名不同门：这枚格钉的是前奏里那道跨会话门，被改的是单元循环里那道） |
| @437（Theory×3） | 它改的是**装配体**的 `ConversationId`（`assembly with { ConversationId = "room-2" }`），夹具里没有单元序列 ⇒ `:239` 不可达 | **不红** |
| 其余 48 枚 | `recent`／`memory` 空序列 ⇒ `:223` 早返回，`:239` 不可达；或只调 `Compile` 的其它门／只读装配体属性 | **不红** |

本刀 m09 的凭证面＝**恰 2 枚**＝@298（#10）＋@952（波 18 连坐）。实到不等于 2 ⇒ 整轮作废。

### 一.5 m10 闭合点名表（`NarrativeContextCompiler.cs:284`，`NotSupplied()` 工厂的 Disposition 常量）

`:284` 是 `private static NarrativeContextSegment NotSupplied(…)` 的表达式体里唯一一处 `Disposition`，改成 `Dropped` ⇒ **所有"宿主没给料"的段**（`:138` Advisory 缺席、`:197` 整段缺席、`:225` 单元序列为空）都从 `NotSupplied` 变 `Dropped`，并顺带把装配体的 `NotSuppliedSegmentCount`（`:434` 按 `Disposition == NotSupplied` 计）打到 0、`DroppedSegmentCount`（`:433`）按缺席段数上升。`DispositionReason` 仍是 `"not_supplied"`（`:285` 那行未被改），所以钉**字符串**的格不翻——这正是要分清的两侧。

| 枚 | 夹具推演 | 判定 |
|---|---|---|
| @198 `Compiler_keeps_absent_sections_as_not_supplied_instead_of_dropping_them` | `Payloads()` 全缺席 ⇒ 改前 `NotSuppliedSegmentCount=7, DroppedSegmentCount=0`；改后 `NotSuppliedSegmentCount=0, DroppedSegmentCount=7` | **红**：第一条断言 `:202 Assert.Equal(7, assembly.NotSuppliedSegmentCount)` 即翻（Expected 7／Actual 0） |
| @934 `Absent_sections_are_named_not_supplied_rather_than_budget`（波 18 补格） | 同一夹具；它 `Where(Disposition == NotSupplied)` 取理由串 ⇒ 改后该集合为空 | **红**：`:941 Assert.Equal(7, assembly.NotSuppliedSegmentCount)` 翻（同格 `:942` 的七枚串数组与 `:944` 的 `DroppedSegmentCount=0` 也随之翻，不另计枚）。注意：这枚格钉的 `"not_supplied"` **字符串**本身没被 m10 改掉，红在计数与集合半边——按件 3′ 如实登记这一面 |

判不连坐（其余 54 枚逐组）：

- @266 `Compiler_fails_loudly_when_the_reserved_directive_does_not_fit`：`advisory=2401 个 'a'` ⇒ 走 `:106-:124` 的"给了但装不下"支，在 `:121 return Fail` **之前**只加了 `Identity`＋一枚显式 `Dropped` 的 Advisory，`NotSupplied()` 从未被调用 ⇒ `Assert.Equal(1, assembly.DroppedSegmentCount)`@277 仍为 1。**不红**——这枚格是本刀唯一一处"钉 `DroppedSegmentCount` 但不读缺席段"的断言面，必须点名。
- @210（`state` 超份额⇒显式 `Dropped`）、@913（`behavior` 超份额⇒显式 `Dropped`）、@281／@298／@313／@952／@975（`AddUnitRun` 自己算 `disposition`，`:260-:262`，不经 `NotSupplied()`）：它们断言的**那一枚具名段**的 Disposition／理由／计数都来自被改行以外的生产者。其余缺席段虽然被 m10 改了标签，但这些格不读缺席段的 Disposition。**不红**。
- @183 `Compiler_emits_the_eight_assembly_units_of_the_disk_builder_in_its_order`：断言 `SegmentCount==8`、段名序列、每段 `SourceRef` 前缀——`NotSupplied()` 工厂改的只有 `Disposition` 一个字段，段数／段名／SourceRef 不变。**不红**。
- @328／@367：读 `InputEstimateTokens`／`InputBudgetTokens`／`WithinUnifiedBudget`／`EmittedSourceRefs`。`NotSupplied()` 的 `UsedTokens` 恒 0、`SourceRefs` 恒空，改 Disposition 不动这两面。**不红**。
- @420／@437：`Compile` 产装配体后只验 `Validate()` 抛与 `ReasonCode`；不读段级 Disposition。**不红**。
- RunAsync 族 24 枚（@381…@832）＋@851：夹具 `Payloads()`／`Payloads(advisory:"decide")` 确实会让多数段走 `NotSupplied()`，但**没有任何一枚格断言段级 Disposition 或那两个计数**；最贴近的是 @471（断言 `call.Assembly.SegmentCount==8`、`Trigger`、`BatchId`、`EmittedSourceRefs` 含 `"advisory:block"`、`Budget.CallCount==1`）与 @761（`EmittedSourceRefs` 含 `"input:101"`）——两者读的字段不受 `Disposition` 影响（缺席段的 `SourceRefs` 由 `NarrativeContextSegment` 默认 `[]` 给出，`:138/:197/:225` 三处调用根本不传 `SourceRefs`）。**不红**。
- @890：手工构造装配体、不调 `Compile`。**不红**。

本刀 m10 的凭证面＝**恰 2 枚**＝@198（#11）＋@934（波 18 连坐）。实到不等于 2 ⇒ 整轮作废。

### 一.6 五轮合计与"闭合"自证声明

- 五轮预期红集合＝{5}／{2}／{4}／{2}／{2}，每轮都**已在本节跑前落档**，且每轮的"判不连坐"都逐枚给了可达路径级理由（不是"应该不影响"）。
- 要领账的波 13 格：M8′ 4 枚（#24–27）＋m07 1 枚（#7）＋m08 2 枚（#8、#9）＋m09 1 枚（#10）＋m10 1 枚（#11）＝**9 枚**，与 §〇.6 从盘上取的"波 13 B 类现值 9 枚"逐枚对齐（不多不少）。
- 连坐但不领账的格＝@851（M8′）、@913（m07）、@952＋@975（m08）、@952（m09）、@934（m10）——它们属波 18 补格那一段，账不在本刀，本刀只登记"同场响过"。
- 每轮 `total` 应当＝**56**（不是 49）。

## 二 逐轮读数（每轮跑完立刻追加；预期列在 §一，本节不回头改它）

### 二.0 本节口径

- 每轮记满工单 §一.9 的十一项：①被改文件:行 ②施加前盘上态 ③施加后盘上态 ④`git diff --stat` ⑤`dotnet test` 末行原样 ⑥`.trx` 文件名 ⑦红格全名＋消息要义＋红点行号 ⑧还原命令 ⑨还原后 `status --short` 行数 ⑩突变态与还原后的文件 SHA-256＋本轮判据文件 SHA ⑪本轮预检段。
- 读数只认 `<UnitTestResult … outcome="Failed">` **开标签**，并与同份 `<Counters failed>` 对账；`<ResultSummary outcome="Failed">` 那一处不是格（本刀每轮的 `rawOutcomeFailedStrings` 与 `openTagFailed` 之差就是它，逐轮列出）。
- 五轮的顺序＝M8′ → m07 → m08 → m09 → m10（工单 §三.0 固定序），每轮各自先预检再起跑。

### 二.1 第 1 轮 M8′（`src/Pry.Core/Narrative/NarrativeRoundRunner.cs:115`）

| 项 | 当场值 |
|---|---|
| ① 被改面 | `src/Pry.Core/Narrative/NarrativeRoundRunner.cs` 第 **115** 行（`Disposition = NarrativeRoundDisposition.ProviderRejected, Assembly = assembly, Outcome = outcome,` → `Silence`），OLD 行由 `w40-apply.js` 施加前当场逐字符断言相等后放行 |
| ② 施加前盘上态 | 载体 `git status --short`＝**0 行**；`NarrativeRoundRunner.cs` carrier SHA＝`961fb49bb464188a…` ＝＝ `blob(clean)`；`NarrativeContextCompiler.cs`＝`86c301604dc178d7…` ＝＝ `blob(clean)`；判据文件＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4` |
| ③ 施加后盘上态 | `APPLIED W40-M8p src/Pry.Core/Narrative/NarrativeRoundRunner.cs :115 sha=0400a57714bbdd8e8d642e78daaa5e9ceaf79c927029c63a06d5d0daaf186c08 bytes=16759`；`status --short`＝**1 行**＝` M src/Pry.Core/Narrative/NarrativeRoundRunner.cs`（唯一被写过的对象是产品文件） |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/NarrativeRoundRunner.cs \| 2 +-` ⇒ `1 file changed, 1 insertion(+), 1 deletion(-)`（单行突变，符合"一行换一行"的形状） |
| ⑤ 运行命令与末行 | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~NarratorPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave40-1029/out/trx-M8p" --logger trx`；末行原样＝`失败!  - 失败:     5，通过:    51，已跳过:     0，总计:    56，持续时间: 234 ms - Pry.Core.Tests.dll (net10.0)`（`总计 56`＝§一.6 预期的格盘） |
| ⑥ `.trx` | `.local-notes/wave40-1029/out/trx-M8p/24782_PHYTHLINK_2026-10-29_02_51_40.trx`（89,846 字节）；运行日志原样＝`out/run-M8p.txt`；仪器读数存档＝`out/reading-M8p.txt` |
| ⑦ 红集（5 枚，全名／消息／红点行） | 见下表 |
| ⑧ 还原命令 | `node .local-notes/wave40-1029/w40-apply.js revert --id=W40-M8p`（通道内部＝从 `git show 51f149d:src/Pry.Core/Narrative/NarrativeRoundRunner.cs` 取字节再写文件，不用 `checkout`／`restore`） |
| ⑨ 还原后 | `git -C …/carrier status --short`＝**0 行** |
| ⑩ SHA 对账 | 还原后 `NarrativeRoundRunner.cs`＝`961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9`，通道打印 `sameAsBlob=true`；本轮结束时判据文件仍＝`4f1b2f6e…`（一字未动） |
| ⑪ 预检 | **P106**（`out/preflight-P106.txt`）：`VERDICT=BUILD_OK`；`FOREGROUND: msedge pid=29476 … covers=False borderless=False`；`UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404`；`gpu_util=9% used=2538MB`；`free_memory_gb=9.8`。HOLD 重跑 0 枚 |

红集逐枚（仪器 `w40-count.js` 原样，全部 `inJudge=YES(NarratorPrototypeTests.cs)`、`family=ASSERT-RED`）：

| 枚 | 格全名（`Pry.Core.Tests.NarratorPrototypeTests.` 前缀省略） | 红点行 | 消息要义 |
|---|---|---|---|
| 1 | `Malformed_response_is_not_masquerading_as_a_chosen_silence` | `:540` | `Assert.Equal() Failure: Values differ`／`Expected: ProviderRejected`／`Actual:   Silence` |
| 2 | `A_provider_that_ignores_cancellation_still_exits_as_timeout` | `:555` | 同上 |
| 3 | `A_provider_that_honours_the_timeout_token_exits_as_timeout_too` | `:569` | 同上 |
| 4 | `Provider_failure_becomes_a_named_error_shape_and_writes_nothing` | `:582` | 同上 |
| 5 | `Provider_side_cancellation_with_a_live_host_token_is_named_timeout_not_failure` | `:865` | 同上 |

对账与裁决面：

- 仪器行原样＝`nodes=56 total=56 executed=56 passed=51 failed(counters)=5 notExecuted(attr)=0 rawOutcomeFailedStrings=6 altWayNodes=5 openTagFailed=5`，`NotExecuted 开标签=（0 枚）`，结论＝**AGREE（问题面 0 处）**。`openTagFailed`（5）＝`failed(counters)`（5）＝第二走（`altWayNodes`，5）三向同值；`rawOutcomeFailedStrings` 那第 6 处字面量经当场定位＝`<ResultSummary outcome="Failed">`，按口径不是一枚格。
- 实到红集 vs §一.1 闭合点名表：逐枚对名相符、行号相符（540/555/569/582/865），枚数＝**恰 5**＝守门人事前点名值 ⇒ **本轮有效**，不需要触发"实到 ≠ 5 整轮作废、不署名、不追第二轮"。
- 机理面自证（不新增判据）：5 枚红的消息全是同一族 `Expected: ProviderRejected / Actual: Silence`，与被改行 `:115` 只把一个枚举值换成 `Silence` 的单行突变形状一致；`:110` 那道早退闸是 `:115` 的唯一来路，故 `:150`／`:155` 两支在本轮不可达——这一点在本刀是**推演**（§一.1）而非实测，实测只证明了"五个消费点同时翻"。
- 编译面（随 `dotnet test` 的增量构建发生，非解决方案级构建）：日志中 `error CS` 计数 0、`warning`／`警告` 计数 0；`错误消息` 计数 5 与红枚数同值（那是 MSTest/xUnit 输出正文里的关键字，不是构建错误）。还原的 `正在确定要还原的项目…所有项目均是最新的，无法还原。` 说明还原动作是无操作。
- 账面对本刀的意义（只登记事实，不写裁决）：本轮 5 枚里，#24–27 那四枚＝波 13 旧账剩下的 B 类格，第二次换手由这双手响完；@851 那枚＝工单 §四.3 明确"其换手不在本刀领账"的连坐格，本刀只记"同场响过"。
- 本轮 `dotnet` 计数＝1（累计 1/6）；定向额度累计 1/5；构建额度仍 0/1（未起解决方案级构建，跳过判定见 §五）。

### 二.2 第 2 轮 m07（`src/Pry.Core/Narrative/NarrativeContextCompiler.cs:203`）

| 项 | 当场值 |
|---|---|
| ① 被改面 | `NarrativeContextCompiler.cs` 第 **203** 行；当场实际换入换出的两行（`git diff --unified=0` 原样）＝`-        if (cost > cap \|\| used + cost > total)` / `+        if (used + cost > total)`，即摘掉"超自身份额"半边；OLD 行施加前逐字符断言相等后放行 |
| ② 施加前盘上态 | 载体 `status --short`＝**0 行**；compiler carrier SHA＝`86c301604dc178d7…` ＝＝ `blob(clean)`；runner＝`961fb49bb464188a…` ＝＝ `blob(clean)`（第 1 轮已还原干净）；判据文件＝`4f1b2f6e…` |
| ③ 施加后盘上态 | `APPLIED W40-m07 src/Pry.Core/Narrative/NarrativeContextCompiler.cs :203 sha=24e90d81e2d0c6be5a54786ab13c4d4f32373bfafd7455ed9e0bc631f5f10a43 bytes=24443`；`status --short`＝**1 行**＝` M src/Pry.Core/Narrative/NarrativeContextCompiler.cs` |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/NarrativeContextCompiler.cs \| 2 +-` ⇒ `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤ 运行命令与末行 | 同 §二.0 的命令形，`--results-directory` 换 `out/trx-m07`；末行原样＝`失败!  - 失败:     2，通过:    54，已跳过:     0，总计:    56，持续时间: 227 ms - Pry.Core.Tests.dll (net10.0)` |
| ⑥ `.trx` | `out/trx-m07/24782_PHYTHLINK_2026-10-29_02_53_37.trx`（86,254 字节）；运行日志＝`out/run-m07.txt`；仪器读数＝`out/reading-m07.txt` |
| ⑦ 红集（2 枚） | 见下表 |
| ⑧ 还原命令 | `node .local-notes/wave40-1029/w40-apply.js revert --id=W40-m07` |
| ⑨ 还原后 | `status --short`＝**0 行** |
| ⑩ SHA 对账 | 还原后 compiler＝`86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842`，`sameAsBlob=true`。判据 SHA 在本轮还原后的那一刻**没有单独打印**（逐轮打印面见 §七.3），本轮还原后的判据面凭据＝`status --short`＝0 行（判据是跟踪文件，0 行即与 `51f149d` 的 blob 逐字节同值） |
| ⑪ 预检 | **P107**（`out/preflight-P107.txt`）：`VERDICT=BUILD_OK`；`FOREGROUND: ChatGPT pid=16348 class=Chrome_WidgetWin_1 rect=-1748,263,1445x811 screen=1920x1080 covers=False borderless=False title=ChatGPT`（焦点窗在第二块屏外侧，不遮主屏）；`gpu_util=9% used=2501MB`；`free_memory_gb=9.3`。HOLD 重跑 0 枚 |

| 枚 | 格全名 | 红点行 | 消息要义 | vs §一.2 预期 |
|---|---|---|---|---|
| 1 | `Compiler_drops_an_optional_section_that_exceeds_its_own_share_and_counts_it_exactly` | `:217` | `Assert.Equal() Failure: Values differ`／`Expected: Dropped`／`Actual:   Included` | 名字、行号、消息族三项**逐字符相符** |
| 2 | `Whole_section_segments_carry_their_own_source_reference_even_when_dropped` | `:923` | 同上 | 相符 |

对账与裁决面：

- 仪器行原样＝`nodes=56 total=56 executed=56 passed=54 failed(counters)=2 notExecuted(attr)=0 rawOutcomeFailedStrings=3 altWayNodes=2 openTagFailed=2`，结论＝**AGREE（问题面 0 处）**；三向同值（2＝2＝2），第 3 处字面量＝`<ResultSummary outcome="Failed">`，不是格。
- 实到＝**恰 2**＝§一.2 闭合点名表的 { @210, @913 } ⇒ 本轮有效，#7（@210）可领波 13 的账；@913 是波 18 补格的连坐，本刀不领它的账（工单 §三.0R 件 3′ 的口径）。
- 判不连坐的两面被实测**支持**而非推翻（这里只报形状，不报"证明"）：@328（同读 `AddSection` 但被另一半边决定）与 @298／@313／@952／@975（走 `AddUnitRun` 不经 `:203`）本轮全绿，说明"被摘掉的半边在这些夹具下不 binding"这条推演与盘上一致。**注意**：这仍是同一枚突变的一次实测，不构成对 m07 方向的第二重证据；m07 的通过格证据只在"这 54 枚没被这枚突变影响"这个意义上成立。
- 编译面：`error CS` 计数 0、`warning`／`警告` 计数 0；`错误消息` 计数 2 与红枚数同值。
- 累计：`dotnet` 2/6、定向 2/5、解决方案级构建 0/1、HOLD 0/16。

### 二.3 第 3 轮 m08（`src/Pry.Core/Narrative/NarrativeContextCompiler.cs:246`）

| 项 | 当场值 |
|---|---|
| ① 被改面 | `NarrativeContextCompiler.cs` 第 **246** 行；当场两行＝`-            if (wallet - cost < 0 \|\| used + cost > total)` / `+            if (used + cost > total)`，即摘掉 `AddUnitRun` 的"自身份额钱包"半边；OLD 行施加前逐字符断言相等后放行 |
| ② 施加前盘上态 | 载体 `status --short`＝**0 行**；compiler＝`86c301604dc178d7…` ＝＝ `blob(clean)`；runner＝`961fb49bb464188a…` ＝＝ `blob(clean)`；判据文件＝`4f1b2f6e…` |
| ③ 施加后盘上态 | `APPLIED W40-m08 … :246 sha=a65399bbbe78c9241d266c57898b01707f10b2a865b9ed8db8b25391da37f1cb bytes=24436`；`status --short`＝**1 行**（同一枚产品文件） |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/NarrativeContextCompiler.cs \| 2 +-` ⇒ `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤ 末行原样 | `失败!  - 失败:     4，通过:    52，已跳过:     0，总计:    56，持续时间: 232 ms - Pry.Core.Tests.dll (net10.0)` |
| ⑥ `.trx` | `out/trx-m08/24782_PHYTHLINK_2026-10-29_02_54_37.trx`（89,701 字节）；运行日志＝`out/run-m08.txt`；仪器读数＝`out/reading-m08.txt` |
| ⑦ 红集（4 枚） | 见下表 |
| ⑧ 还原命令 | `node .local-notes/wave40-1029/w40-apply.js revert --id=W40-m08` |
| ⑨ 还原后 | `status --short`＝**0 行** |
| ⑩ SHA 对账 | 还原后 compiler＝`86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842`，`sameAsBlob=true`。本轮还原后的那一刻**未单独打印判据 SHA**（逐轮打印面登记在 §七.3）；该时刻的判据面凭据＝`status --short`＝**0 行**（判据是跟踪文件，0 行即与 blob 逐字节同值），最近一次打印值＝本轮 ② 那枚 `4f1b2f6e…` |
| ⑪ 预检 | **P108**（`out/preflight-P108.txt`）：`VERDICT=BUILD_OK`；`FOREGROUND: ChatGPT pid=16348 … covers=False borderless=False`；`gpu_util=10% used=2591MB`；`free_memory_gb=9.5`。HOLD 0 枚 |

| 枚 | 格全名 | 红点行 | 消息要义 | vs §一.3 预期 |
|---|---|---|---|---|
| 1 | `Compiler_trims_recent_messages_only_at_whole_unit_boundaries` | `:289` | `Values differ`／`Expected: PartiallyIncluded`／`Actual:   Included` | 相符（§一.3 第 1 行） |
| 2 | `Compiler_shrinks_the_memory_wallet_unit_by_unit` | `:320` | `Values differ`／`Expected: 1`／`Actual:   2` | 相符（§一.3 第 2 行点名的正是 `IncludedUnitCount` 那一条，不是 Disposition 那一条） |
| 3 | `Unit_run_losing_units_to_both_reasons_names_both_together` | `:961` | `Strings differ`／`↓ (pos 11)`／`Expected: "cross_scope_and_unit_budget"`／`Actual:   "cross_scope"` | 相符，而且**连"Disposition 那一条恰好仍过、红只在理由串"这一面都相符**（§一.3 第 3 行预言 `:960` 不翻、`:961` 翻） |
| 4 | `Unit_run_that_fits_no_unit_at_all_is_dropped_and_named_budget` | `:981` | `Values differ`／`Expected: Dropped`／`Actual:   Included` | 相符（§一.3 第 4 行） |

对账与裁决面：

- 仪器行原样＝`nodes=56 total=56 executed=56 passed=52 failed(counters)=4 notExecuted(attr)=0 rawOutcomeFailedStrings=5 altWayNodes=4 openTagFailed=4`，结论＝**AGREE**；三向同值（4＝4＝4），第 5 处字面量＝`<ResultSummary outcome="Failed">`。
- 实到＝**恰 4**＝{ @281, @313, @952, @975 } ⇒ 本轮有效。要领账的是 #8（@281）与 #9（@313）；@952／@975 是波 18 补格的连坐，不领波 13 的账。
- **与盘上旧读数的差是本轮的正面发现，不是矛盾**：工单 §一.5 已知读数表（`:43` 行）对波 13 的 `run-m07`／`m08`／`m09`／`m10` 写的是"各恰 **1**"，而波 13 那份原始报告 §三.8（`:518`、`:525` 两处明写"红集 **2 格**，U4 与 U5 共用 `AddUnitRun` 同一行"）与盘上 `.local-notes/qa-wave13-1023/runs/run-m08/24782_PHYTHLINK_2026-10-23_14_59_19.trx` 当场重取（本刀仪器跑它）＝**旧 49 格树上恰 2 枚**＝`Compiler_shrinks_the_memory_wallet_unit_by_unit` 帧 `:320`／`Compiler_trims_recent_messages_only_at_whole_unit_boundaries` 帧 `:289`，`<Counters failed="2">`、`total=49`。⇒ 表与盘面不符这一处已在 §〇.7 登记且**再次实证了工单 §三 首部那条禁令**（不许从旧树平移）。本刀当前树实测＝**恰 4**，比旧盘面的 2 枚多出 @952／@975——正是波 18 补进判据文件的那两枚，它们把 `:246` 这道的见证面从 2 枚扩到 4 枚；本轮的预期来自 §一.3 从**当前 56 格盘**的推演，故不需要回头解释任何历史值。
- 编译面：`error CS` 0、`warning`／`警告` 0、`错误消息` 计数 4 ＝红枚数。
- 累计：`dotnet` 3/6、定向 3/5、构建 0/1、HOLD 0/16。

### 二.4 第 4 轮 m09（`src/Pry.Core/Narrative/NarrativeContextCompiler.cs:239`）

| 项 | 当场值 |
|---|---|
| ① 被改面 | `NarrativeContextCompiler.cs` 第 **239** 行；当场两行＝`-            if (!string.Equals(unit.ConversationId, request.ConversationId, StringComparison.Ordinal))` / `+            if (unit.ConversationId is null)`（`ConversationId` 是非空 `string` 位置参数且 `Validate()` 要求非空白 ⇒ 改后条件恒假，即"跨会话整条剔除"这道门被摘走）；OLD 行施加前逐字符断言相等后放行 |
| ② 施加前盘上态 | 载体 `status --short`＝**0 行**（本轮施加前未跑 `w40-apply.js status` 的逐枚打印；0 行的含义更强＝载体 HEAD＝`51f149d`，0 行即**全部跟踪文件**——含两枚产品目标与判据——都与 `51f149d` 的 blob 逐字节同值）。紧随的 apply 通道自带 `DIRTY-BEFORE-APPLY` 闸，目标文件若不等即 ABORT、不写一字节；判据 SHA 的最近一次单独打印＝第 3 轮 ② 那枚 `4f1b2f6e…` |
| ③ 施加后盘上态 | `APPLIED W40-m09 … :239 sha=dfc917334c7f48941ffc8e29e3a7735f283e60b78c3d9445cc25798020eb91e5 bytes=24399`；`status --short`＝**1 行**＝` M src/Pry.Core/Narrative/NarrativeContextCompiler.cs` |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/NarrativeContextCompiler.cs \| 2 +-` ⇒ `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤ 末行原样 | `失败!  - 失败:     2，通过:    54，已跳过:     0，总计:    56，持续时间: 241 ms - Pry.Core.Tests.dll (net10.0)` |
| ⑥ `.trx` | `out/trx-m09/24782_PHYTHLINK_2026-10-29_02_56_58.trx`（86,388 字节）；运行日志＝`out/run-m09.txt`；仪器读数＝`out/reading-m09.txt` |
| ⑦ 红集（2 枚） | 见下表 |
| ⑧ 还原命令 | `node .local-notes/wave40-1029/w40-apply.js revert --id=W40-m09` |
| ⑨ 还原后 | `status --short`＝**0 行** |
| ⑩ SHA 对账 | 还原后 compiler＝`86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842`，`sameAsBlob=true`。本轮还原后的那一刻**未单独打印判据 SHA**（逐轮打印面登记在 §七.3）；该时刻的判据面凭据＝`status --short`＝**0 行**（判据是跟踪文件，0 行即与 `51f149d` 的 blob 逐字节同值），最近一次打印值＝第 3 轮 ② 那枚 `4f1b2f6e…`（02:53），两次之间载体只出现过产品文件那一行 |
| ⑪ 预检 | **P109**（`out/preflight-P109.txt`）：`VERDICT=BUILD_OK`；`FOREGROUND: explorer pid=7404 class=CabinetWClass rect=884,515,1082x910 screen=2560x1440 covers=False borderless=False`；`gpu_util=9% used=2565MB`；`free_memory_gb=9.7`。HOLD 0 枚 |

| 枚 | 格全名 | 红点行 | 消息要义 | vs §一.4 预期 |
|---|---|---|---|---|
| 1 | `Compiler_removes_cross_conversation_units_and_names_the_count` | `:304` | `Values differ`／`Expected: PartiallyIncluded`／`Actual:   Included` | 相符（§一.4 第 1 行点名的正是 `:304` 那条 Disposition 先倒，同格 `:305/:306` 的 `"cross_scope"`／`CrossScopeUnitCount=1` 随之翻但不另计枚） |
| 2 | `Unit_run_losing_units_to_both_reasons_names_both_together` | `:961` | `Strings differ`／`↓ (pos 0)`／`Expected: "cross_scope_and_unit_budget"`／`Actual:   "unit_budget"` | 相符，且**理由串剩哪一半也相符**：m09 摘跨会话门 ⇒ 余 `"unit_budget"`；m08 摘钱包门 ⇒ 余 `"cross_scope"`。同一枚格在两枚不同突变下各红一次、红值互反，这登记为"两条理由各自独立供料"的形状，不构成第二枚账 |

对账与裁决面：

- 仪器行原样＝`nodes=56 total=56 executed=56 passed=54 failed(counters)=2 notExecuted(attr)=0 rawOutcomeFailedStrings=3 altWayNodes=2 openTagFailed=2`，结论＝**AGREE**；三向同值（2＝2＝2），第 3 处字面量＝`<ResultSummary outcome="Failed">`。
- 实到＝**恰 2**＝{ @298, @952 } ⇒ 本轮有效；要领账的是 #10（@298），@952 是波 18 连坐、不领波 13 的账。
- 判不连坐的三处关键面本轮全绿，与 §一.4 的推演同向（只报形状不报"证明"）：@281／@313／@975（夹具单元全落 `room-1`，`!Equals` 改前也恒假）、@256（钉的是 `:77-:82` 那道前奏跨会话门，与被改的 `:239` 同名不同门）、@437（Theory×3 改装配体 `ConversationId`，无单元序列 ⇒ `:239` 不可达）。
- 编译面：`error CS` 0、`warning`／`警告` 0、`错误消息` 计数 2 ＝红枚数。
- 累计：`dotnet` 4/6、定向 4/5、构建 0/1、HOLD 0/16。

### 二.5 第 5 轮 m10（`src/Pry.Core/Narrative/NarrativeContextCompiler.cs:284`）

| 项 | 当场值 |
|---|---|
| ① 被改面 | `NarrativeContextCompiler.cs` 第 **284** 行；当场两行＝`-            BudgetTokens = cap, UsedTokens = 0, Disposition = NarrativeSegmentDisposition.NotSupplied,` / `+            BudgetTokens = cap, UsedTokens = 0, Disposition = NarrativeSegmentDisposition.Dropped,`（`NotSupplied()` 工厂的 Disposition 常量翻转，`:285` 的 `DispositionReason = "not_supplied"` 那行未被改）；OLD 行施加前逐字符断言相等后放行 |
| ② 施加前盘上态 | 载体 `status --short`＝**0 行**（本轮施加前同样没跑逐枚 SHA 打印；含义同上——含两枚产品目标与判据在内全部跟踪文件与 `51f149d` 的 blob 同值，并由 `DIRTY-BEFORE-APPLY` 闸当场对目标文件二次核验后才写）；判据 SHA 在本轮之前的最近一次单独打印＝第 3 轮 ②（02:53）那枚 `4f1b2f6e…`，而 §三.1 表里第 2／3／4 轮还原后各有一次 `status --short`＝0 行把它们连成一条不断链 |
| ③ 施加后盘上态 | `APPLIED W40-m10 … :284 sha=24dbd7e558ab4362ded03fd2de34e2833da90f0541286ca2dab62f1f0c4d14ce bytes=24453`；`status --short`＝**1 行**＝` M src/Pry.Core/Narrative/NarrativeContextCompiler.cs` |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/NarrativeContextCompiler.cs \| 2 +-` ⇒ `1 file changed, 1 insertion(+), 1 deletion(-)`（净括号偏移为一，符合单行突变的形状约束） |
| ⑤ 末行原样 | `失败!  - 失败:     2，通过:    54，已跳过:     0，总计:    56，持续时间: 215 ms - Pry.Core.Tests.dll (net10.0)` |
| ⑥ `.trx` | `out/trx-m10/24782_PHYTHLINK_2026-10-29_02_58_05.trx`（86,118 字节）；运行日志＝`out/run-m10.txt`；仪器读数＝`out/reading-m10.txt` |
| ⑦ 红集（2 枚） | 见下表 |
| ⑧ 还原命令 | `node .local-notes/wave40-1029/w40-apply.js revert --id=W40-m10` |
| ⑨ 还原后 | `status --short`＝**0 行**；五枚目标全部打印 `== blob(clean)` |
| ⑩ SHA 对账 | 还原后 compiler＝`86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842`，`sameAsBlob=true`；判据文件仍＝`4f1b2f6e…` |
| ⑪ 预检 | **P110**（`out/preflight-P110.txt`）：`VERDICT=BUILD_OK`，但本轮预检面有一处**需要交回核的形状**，原样登记：`FOREGROUND: explorer pid=7404 class=Progman style=0x96000000 rect=-1920,0,4480x1440 screen=2560x1440 covers=True borderless=True title=Program Manager` ＋ `DESKTOP-SHELL-FOCUS (informational only, does not hold): no app owns the foreground`；`gpu_util=8% used=2482MB`；`free_memory_gb=11`。**这台脚本自己把 Progman／Program Manager（桌面壳，跨双屏铺满）判为 informational only 并给出 BUILD_OK**，按工单口径"以 `VERDICT` 为准"起跑；但 `covers=True borderless=True` 这两个字段值若出现在游戏窗上就是 HOLD，故本刀不自行解释分类正确与否，原样进 §七 的"附加面乙"交回守门人核 |

| 枚 | 格全名 | 红点行 | 消息要义 | vs §一.5 预期 |
|---|---|---|---|---|
| 1 | `Compiler_keeps_absent_sections_as_not_supplied_instead_of_dropping_them` | `:202` | `Values differ`／`Expected: 7`／`Actual:   0` | 相符（§一.5 第 1 行点名的就是 `:202 Assert.Equal(7, assembly.NotSuppliedSegmentCount)`，改后计数被打到 0） |
| 2 | `Absent_sections_are_named_not_supplied_rather_than_budget` | `:941` | `Values differ`／`Expected: 7`／`Actual:   0` | 相符，且 §一.5 那行"这枚格钉的 `"not_supplied"` **字符串**本身没被 m10 改掉，红在计数与集合半边"也被实测支持：消息是计数 `7→0`，不是串值不符 |

对账与裁决面：

- 仪器行原样＝`nodes=56 total=56 executed=56 passed=54 failed(counters)=2 notExecuted(attr)=0 rawOutcomeFailedStrings=3 altWayNodes=2 openTagFailed=2`，结论＝**AGREE**；三向同值（2＝2＝2），第 3 处字面量＝`<ResultSummary outcome="Failed">`。
- 实到＝**恰 2**＝{ @198, @934 } ⇒ 本轮有效；要领账的是 #11（@198），@934 是波 18 连坐。
- §一.5 里那处必须点名的"判不连坐"最险一面本轮全绿：@266 `Compiler_fails_loudly_when_the_reserved_directive_does_not_fit`（走 `:121 return Fail` 的"给了但装不下"路，`NotSupplied()` 从未被调用，故它钉的 `DroppedSegmentCount=1` 不受 Disposition 翻转影响）。其余读 `DroppedSegmentCount` 的格（@210／@913 等）本轮也全绿，方向与推演同向。
- 编译面：`error CS` 0、`warning`／`警告` 0、`错误消息` 计数 2 ＝红枚数。
- 累计（五轮跑完）：`dotnet` 5/6、定向 5/5（额度用满，无退回）、解决方案级构建 0/1、HOLD 重跑 0/16、预检 P106–P110 共 5 枚全 `BUILD_OK`。

## 三 还原与六枚 SHA 核验（五轮跑完、F 之前先核）

### 三.1 还原动作逐枚（还原命令一律＝从 `git show 51f149d:<path>` 取字节再写文件）

| 轮 | 还原命令 | 通道打印 |
|---|---|---|
| M8′ | `node .local-notes/wave40-1029/w40-apply.js revert --id=W40-M8p` | `REVERTED W40-M8p src/Pry.Core/Narrative/NarrativeRoundRunner.cs sha=961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9 sameAsBlob=true` |
| m07 | `… revert --id=W40-m07` | `REVERTED W40-m07 … sha=86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842 sameAsBlob=true` |
| m08 | `… revert --id=W40-m08` | 同上 SHA，`sameAsBlob=true` |
| m09 | `… revert --id=W40-m09` | 同上 SHA，`sameAsBlob=true` |
| m10 | `… revert --id=W40-m10` | 同上 SHA，`sameAsBlob=true` |

- 五轮每轮还原后当场数载体 `git status --short`＝**0 行**（逐轮值见 §二.1–§二.5 的第⑨项），第五轮还原后另加一次 `git -C <carrier> diff --stat`＝**空输出**（0 文件、0 行）。
- 载体 `status --short` 全程最多 **1 行**且那一行永远是产品文件（`src/Pry.Core/Narrative/…`），从未出现 `tests/**`——这是"判据字节全程未被写入"的**过程面**证据，与下面那条 SHA **终态面**证据相互独立。

### 三.2 六枚 SHA 当面核（blob ＝ `git -C carrier show 51f149d:<path>` 的字节 SHA-256；两侧各取一次，逐枚打印 equal／unequal）

| 文件 | `51f149d` blob SHA-256 | 主树工作副本 | 载体工作副本 |
|---|---|---|---|
| `src/Pry.Core/Narrative/NarrativeContextCompiler.cs` | `86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842` | **EQUAL** | **EQUAL** |
| `src/Pry.Core/Narrative/NarratorContracts.cs` | `3a33a8151e61c3396963dac451dffcb46acb2f02807f140d18f694d6a52a9f8c` | **EQUAL** | **EQUAL** |
| `src/Pry.Core/Narrative/NarrativeRoundRunner.cs` | `961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9` | **EQUAL** | **EQUAL** |
| `tests/Pry.Core.Tests/NarratorPrototypeTests.cs`（判据） | `4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4` | **EQUAL** | **EQUAL** |
| `tests/Pry.Core.Tests/NarrativeCommitFactoryTests.cs` | `fcee461164e39bae172c49149d39076e67aa36e844089afa4bcf4ff06d7cea0b` | **EQUAL** | **EQUAL** |
| `tests/Pry.Core.Tests/NarrativeBudgetInequalityCellTests.cs` | `192bbaecb5d2a3f3bafd08d8cbe5fdacf65ee71a7ad7856798345eae6c3f7686` | **EQUAL** | **EQUAL** |

- **6 枚 × 2 侧＝12 次比对，全部 EQUAL，0 次 UNEQUAL**；原始打印＝`.local-notes/wave40-1029/out/final-six-sha.txt`（一次 `node` 运行现取，非转录）。
- 判据文件与 §〇.4 入场基准逐字符相同（`4f1b2f6e…18f4`），且与 `51f149d` 的 blob 同值 ⇒ 本刀对判据**净写入 0 字节**成立在终态面上。
- 与 §〇.4 对照：入场六值与本节六值逐字符相同（五轮的临时覆盖全部按字节还原，未留下任何"还原不彻底"的残差）。
- 主树 `git status --short --branch` 终态＝**10 行**（1 行 branch＋3 行 `M`／` M`＋6 行 `??`）：与入场那 9 行的差＝本报告自己那枚 `??`（`docs/eval/reports/2026-10-29-hds13-bclass-second-hand-recount-part2.md`），他人三枚暂存项与五枚未跟踪资产逐字未动（本刀对主树零写入，只追加这一枚未跟踪文件）。

### 三.3 五轮合计读数（对照 §一 的闭合点名表，预期列未回头改）

| 轮 | 被改面 | 预期红集（§一，跑前落档） | 实到 | 计数一致性 | 判定 |
|---|---|---|---|---|---|
| M8′ | `NarrativeRoundRunner.cs:115` | 恰 5＝@540／@555／@569／@582／@865 | **5**，名／帧／消息族逐字符相符 | `openTagFailed=5`＝`<Counters failed=5>`＝第二走 5 | 有效 |
| m07 | `NarrativeContextCompiler.cs:203` | 恰 2＝@210／@913 | **2**，相符 | 2＝2＝2 | 有效 |
| m08 | `:246` | 恰 4＝@281／@313／@952／@975 | **4**，相符 | 4＝4＝4 | 有效 |
| m09 | `:239` | 恰 2＝@298／@952 | **2**，相符 | 2＝2＝2 | 有效 |
| m10 | `:284` | 恰 2＝@198／@934 | **2**，相符 | 2＝2＝2 | 有效 |

- 五轮 `total` 全＝**56**（工单 §一.4 要求"每轮 total 应当＝56 而不是 49"），`<Counters total>` 与开标签计数逐份同值；跳过格每轮 0（`outcome="NotExecuted"` 开标签 0 枚，与 `<Counters skipped>` 同向）。
- 作废轮＝**0**；未闭合而退回的定向额度＝**0**；本轮次级别没有任何"实到≠预期"需要登记。
- 五轮合计的**格级红面**去重后＝11 枚不同格（@540/@555/@569/@582/@865/@210/@913/@281/@313/@298/@198/@934 去掉 @952 在两轮重复 ⇒ 12−1＝11）；其中领波 13 旧账的＝9 枚（#7–11、#24–27），其余 2 枚（@913、@934）＋@851＋@952＋@975 属连坐面，不领账（@952 两轮各响一次，仍算一枚格）。

## 四 收尾全量 F 的逐份 `<Counters>` 求和

### 四.1 F 的入场前置（先核再跑）

- 五轮全跑满且全有效（§三.3 作废轮 0）⇒ 工单 §三.0 `:71` 那句"未跑满能起跑的轮不许起 F"的前置成立。
- 起 F 前当场重取：载体 `status --short`＝**0 行**、`git diff --stat`＝空、六枚 SHA 全 EQUAL（§三.2）、判据＝`4f1b2f6e…`、`git -C carrier log --oneline fd93ebe..HEAD`＝**2 行**（`51f149d`＋`1e02205`，仍只有波 33 那两枚提交）、`rev-parse HEAD`＝`51f149d0b722e28a6566c93d19f48a94ca4c85df`。
- 预检＝**P111**（`out/preflight-P111.txt`，02:59 落盘）：`VERDICT=BUILD_OK`；`FOREGROUND: BaiduNetdiskUnite pid=17088 class=Chrome_WidgetWin_1 rect=633,209,1293x974 covers=False borderless=False`；`gpu_util=18% used=2771MB`；`free_memory_gb=9.7`。

### 四.2 跑法与逐份读数

跑法（载体内，解决方案级，无 `--no-build`）：

```
dotnet test Pry.slnx -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave40-1029/out/trx-F" --logger trx
```

末行原样四枚（`out/run-F.txt`）＋仪器逐份对账（`out/reading-F.txt`，文件↔项目按 `nodes` 计数一一对应，四值互不相同故无歧义）：

| `.trx` | 项目（由 `nodes` 数对应） | `<Counters>` | 开标签计数 | `NotExecuted` 开标签 |
|---|---|---|---|---|
| `…_03_00_02.trx`（163,625 B） | `Pry.Client.Tests` | total 108／executed 108／passed 108／failed 0 | `openTagFailed=0`＝`altWayNodes=0` | 0 枚 |
| `…_03_00_03.trx`（1,165,338 B） | `Pry.Core.Tests` | total 785／executed 783／passed 783／failed 0 | 0 | **2 枚**＝`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key` |
| `…_03_00_04.trx`（413,088 B） | `Pry.Api.Tests` | total 280／executed 280／passed 280／failed 0 | 0 | 0 枚 |
| `…_03_00_05.trx`（341,084 B） | `Pry.App.Tests` | total 233／executed 233／passed 233／failed 0 | 0 | 0 枚 |
| **求和（仪器自校行）** | 4 份 | **files=4 total=1406 executed=1404 passed=1404 failed=0 notExecutedAttr=0 notExecOpenTags=2** | 0 | 2 枚，逐枚点名如上 |

- 求和式（当场从四份产物加，不心算）：`108＋785＋280＋233＝1406`；`executed 108＋783＋280＋233＝1404`；`failed 0＋0＋0＋0＝0`；跳过＝2 枚且两枚同名点名与 §〇.5 从波 39 §四读到的那两枚逐字符相同。
- 仪器结论＝**AGREE（问题面 0 处）**；四份的 `openTagFailed` 与 `<Counters failed>` 逐份同值（0＝0）。`rawOutcomeFailedStrings` 四份全 0 ⇒ F 里没有红格，连 `<ResultSummary>` 的 `outcome="Failed"` 字样都没出现（这份文件的 `summary` 是 Passed），与"求和 failed＝0"三方同向。
- **本刀 F 与 §〇.5 起点的关系（只报读数关系，不作结论）**：本刀净新增判据格 0（§五.3 出场合格数复看），F 读到的 1406／1404／0／2 与波 39 §四 那一份逐字段同值。**本刀不顶账**，也不为顶账另起任何构建；这一面是否算"第 N 次同值复采"由守门人核（工单 §八 ④）。
- F 里本刀那 56 枚格的全绿面（当场从 `…_03_00_03.trx` 现取，零新构建）：`testName` 含 `NarratorPrototypeTests` 的 `<UnitTestResult>` 开标签＝**56 枚**，`outcome="Passed"` 56／`Failed` 0／其他 0 ⇒ 五轮还原后这棵判据在无突变态下整棵自洽，与本刀"零落地"的终态面同向。
- F 期间载体 `src/Pry.Core/bin/Release/net10.0/Pry.Core.dll` 由 `dotnet test` 自己增量重建（当场 stat＝1,745,920 字节、mtime `10-29 03:00:01`，落在 F 那一次调用之内；字节数与 §五.2 跳过判定入场时所见的同一枚值相同）——这是解决方案级**测试调用**的自带构建，不是本刀另起的解决方案级构建。

## 五 预算与预检台账

### 五.1 四类预算的动用（上限／动用／说明）

| 面 | 上限 | 动用 | 说明 |
|---|---|---|---|
| 解决方案级构建（`dotnet build`） | ≤1 | **0** | 合法跳过，三段判定见 §五.2 |
| 解决方案级全量 | ≤1 | **1**（F，`trx-F`，4 份 `.trx`） | 交付读数只来自这一次全量；无作废轮重跑 |
| 定向 | ≤5 | **5**（M8′／m07／m08／m09／m10，各 1 份） | 五轮的件 3′ 点名表全部在跑前闭合（§一.1–§一.5），故**无一轮因点名不闭合而退回额度** |
| `dotnet` 总调用 | ≤6 | **6** ＝ 定向 5＋全量 1 | 与预检枚数三方对账通过（§五.3） |
| HOLD 重跑 | ≤16（按整把刀计） | **0** | 六枚预检全 `BUILD_OK`，无需等待重试 |

### 五.2 构建跳过三段判定（当场复取，不引用工单或波 39 的旧述）

1. **0 行式子成立**：§〇.3 ⑤ 那条 `git diff --stat 51f149d 6ae0803 -- src tests scripts Pry.slnx Directory.Build.props`＝**0 行**（右端 `rev-parse HEAD` 入场当场取），本刀对主树产品／判据字节净写入 0 ⇒ 载体树与主线归位后的相关字节逐字符相同，无需另起一次解决方案级构建来"对齐"。
2. **载体侧成品在盘**：`.local-notes/qa-wave33-1028/carrier/src/Pry.Core/bin/Release/net10.0/Pry.Core.dll`＝1,745,920 字节，本刀入场时 mtime 为当日 02:10（波 39 那把留下的增量重建），F 之前已足够新（六枚 SHA 未变、无新源文件），F 时由 `dotnet test` 自行刷新到 03:00:01。
3. **六枚 SHA 未变**：入场（§〇.4）→每轮施加前（§二.1–§二.5 第②项）→五轮还原后（§三.2）三段全为 `== blob(clean)`，主树侧同值。

三段齐 ⇒ 跳过成立。**旁证面**：本刀报告与运行日志里 `dotnet test` 命令行 **0 处**含 `--no-build`（工单 §一.7 明令五轮不许带），五轮全部由 `dotnet test` 自带增量编译完成（每份 `run-*.txt` 前四行可见 `Pry.Contracts／Pry.Core／Pry.Core.Tests` 的现值产出行与"所有项目均是最新的，无法还原"）。

### 五.3 预检台账（编号／时刻／VERDICT／归档／前台窗／内存／放行哪一轮）

| 编号 | 落盘时刻 | VERDICT | 归档路径 | 前台窗 pid／class／covers／borderless | `free_memory_gb` | 放行 |
|---|---|---|---|---|---|---|
| P106 | 02:51:17 | BUILD_OK | `out/preflight-P106.txt` | 29476／`Chrome_WidgetWin_1`／False／False | 9.8 | 第 1 轮 M8′ |
| P107 | 02:53:16 | BUILD_OK | `out/preflight-P107.txt` | 16348／`Chrome_WidgetWin_1`／False／False | 9.3 | 第 2 轮 m07 |
| P108 | 02:54:21 | BUILD_OK | `out/preflight-P108.txt` | 16348／`Chrome_WidgetWin_1`／False／False | 9.5 | 第 3 轮 m08 |
| P109 | 02:56:45 | BUILD_OK | `out/preflight-P109.txt` | 7404／`CabinetWClass`／False／False | 9.7 | 第 4 轮 m09 |
| P110 | 02:57:47 | BUILD_OK | `out/preflight-P110.txt` | 7404／`Progman`／**True**／**True**（`title=Program Manager`，脚本判为桌面壳 informational only） | 11 | 第 5 轮 m10 |
| P111 | 02:59:52 | BUILD_OK | `out/preflight-P111.txt` | 17088／`Chrome_WidgetWin_1`／False／False | 9.7 | 收尾全量 F |

- 对账（工单 §〇.8 末段要求）：**逐份 grep `VERDICT` 的枚数 6＝`dotnet` 次数 6＝定向轮数 5＋全量 1**；`HOLD` 0 份；预检 mtime 与运行 mtime 一一配对（02:51:17→`run-M8p.txt` 02:51:40、02:53:16→`run-m07.txt` 02:53:38、02:54:21→`run-m08.txt` 02:54:37、02:56:45→`run-m09.txt` 02:56:59、02:57:47→`run-m10.txt` 02:58:05、02:59:52→`run-F.txt` 03:00:35），无"跳过预检起跑"的形状。
- `preflight-P105b.txt` 也在这个目录里（mtime 02:36:26），**不是本刀跑的、本刀也没覆盖它**：同目录里守门人自己的 `gk40-prep.txt` mtime 02:24:33，本刀第一件产物 `w40-selfproof.js` mtime 02:41，02:36 落在这两段之间且早于本刀任何写动作；本刀编号严格从 P106 起。这一点列在 §七.6 之外的附加面里交回核。
- P110 那枚 `covers=True borderless=True` 是本刀唯一一处"字段值若落在游戏窗上即为 HOLD"的预检面；脚本自身给出的 `VERDICT` 是 BUILD_OK，本刀按工单口径以 `VERDICT` 为准起跑，**不自行解释分类是否恰当**，原样交回（§七 附加面乙）。

### 五.4 产物枚数与合规格数复看

- 盘上 `.trx`＝**9 枚**＝定向 5×1＋全量 1×4（工单 §一.3 满额形状 9 枚，差额 **0**）；结果目录 **6 个**（`trx-M8p`／`trx-m07`／`trx-m08`／`trx-m09`／`trx-m10`／`trx-F`），无 `-voided-` 目录（作废轮 0）。清单当场值＝`find .local-notes/wave40-1029/out -name '*.trx'` 九行，逐一对应 §二各轮第⑥项与 §四.2 表格。
- 每轮定向 `total`＝**56**（五份全中），F 的 `total` 求和＝**1406**。
- 出场合格数复看（判据文件载体侧现取，与 §〇.8 入场值逐字段相同）：文件 **989 行**（三侧同：载体工作副本／主树工作副本／`git show 51f149d:` 的 blob 各 989）；`[Fact]` **53**／`[Theory]` **1**／`[InlineData]` **3** ⇒ 53＋1×3＝**56 枚格**；带属性的方法 **54** 枚＝`public void` 24＋`public async Task` 30；另有 2 枚 `public async Task<`（夹具 `ProposeAsync`，不承载测试属性，与 raw grep `public async Task`＝32 的差额由此解释）；`Assert.Skip` **0** 处。⇒ 本刀**净新增格 0、改断言 0、新增数字门槛 0**。
- 主树 `git status --short --branch`：入场 **9 行** → 出场 **10 行**，差额＝本报告那枚 `??`（工单 §一.6 允许的唯一新建文件，未提交、未 `git add`）；他人三枚暂存项与其余五枚未跟踪资产逐字未动。

## 六 未验证面（逐条点名，不用"已完成"混过去）

### 六.1 工单 §六 那十条照旧全挂（本刀一枚未动）

真实模型对照（零读数，需作者一句"可以跑"）；**生产接线**（HDS-003 这 56 枚至今**零生产调用方**，接线时机＝U-9 残句仍未拍）；旧数据真实继承（零读取）；UI 可见行为（零读数）；生产库与 `%LOCALAPPDATA%\PryCompanion`（零接触）；待标定参数（关系／心情各档阈值、经验有效期、点击归因窗口全部未定＝**特性不是缺陷**）；并发与断电（零读数）；旧链删除片与"构建时不把旧链打进包"那道门（**未开始**）；覆盖率（零读数）；Release 序列点把红点记到多行调用起始行的机理（已知形状，不影响署名）。

### 六.2 本刀自己新留下的未验证面（只登记，不补跑）

1. **`used + cost > total` 那另一半从没被单独摘过**：m07（`:203`）与 m08（`:246`）摘的都是合取的第一支，改后留下的正是总量这一支；⇒ 本刀两枚红证署的是"自身份额／钱包"那道门，**不署**"统一总量"那道门（波 13 §三.8 当年也照实写过"没证明：`used + cost > total` 那一半"，本刀同样没证明它）。
2. **`DispositionReason = "not_supplied"` 那行（`:285`）的反向面**：m10 改的是 `:284` 的 Disposition 常量，理由串未动 ⇒ "串与字段互相独立"这一面拿到一次实测（@934 红在计数而不是串值），但**反过来摘 `:285`** 本刀没跑，那枚突变不在工单 §三.0 的五轮配方里。
3. **`NarrativeRoundRunner.cs:150`／`:155` 两支的可达性**：§一.1 用推演说明"只有 `:110` 那道闸能走到 `:115`，故 `:115` 被改后这两支在本轮不可达"，实测只证明"五个消费点同时翻"；那两支各自的独立反证（`Immediate`／`Deferred` 两条 `Disposition` 生产者）**本刀没有单独逼红过**。
4. **跨会话门的另一半**：@256 那枚格钉的是 `:77-:82` 的前奏跨会话门，本刀 m09 摘的是 `:239` 的单元循环门，**前者本刀未反证**（它属波 13 老账 m04 那枚突变的面，不在本刀五轮里）。
5. **`NarrativeSegmentDisposition` 的 `PartiallyIncluded` 生产者穷举**：§一.3／§一.4 的多处"判不连坐"依赖"该格不读缺席段／不读跨会话单元"这一类**读字段面**的判断；本刀实测与推演同向（该些格全绿），但没有对"该字段还有谁读"做穷举（`out/w40-selfproof.txt` 的候选面扫描只覆盖 `^\s*Assert\.` 那 11 个 token 面）。
6. **连坐格的凭证归属**：@851（M8′）、@913（m07）、@952（m08＋m09）、@975（m08）、@934（m10）本刀各响过至少一次，但工单 §四.3 明确 @851 的换手不在本刀领，其余四枚属波 18 补格段、账不在本刀；⇒ 本刀只登记"同场响过"，**不署它们的账**。
7. **预检仪器（v6）对桌面壳窗的分级**：见 §五.3 的 P110 那条与 §七 附加面乙，本刀未验证 `Progman covers=True` 是否确属"不遮挡作者游戏"的分类。
8. **本刀不能署的面（结构性而非执行性）**：判据字节全程未动 ⇒ 本刀的红证是**行为面＋第二换手**两面，**不含**"判据写法本身"那一面；这条是工单 §〇 任务性质里"第一次由不是写它们的那双手响一遍"的定义本身。

## 七 交回守门人核的事（照工单 §五 那七件，逐件给当场读数，不代裁）

1. **M8′ 是否恰 5 且红名逐字符等于 §三.0 的事前论证**：本刀实到 5，名／帧／消息族见 §二.1；与工单 §三.0 那张表（`trx-M8-voided-P103` 那五枚）逐字符对得上；载体侧凭证＝`out/trx-M8p/24782_PHYTHLINK_2026-10-29_02_51_40.trx`（本刀在载体里跑出来的那份，**不是**波 39 那枚作废轮）。多一枚／少一枚的处置权在你。
2. **m07–m10 各轮的闭合点名表是否先落档、实测是否等表；表内是否有"判不清"被写成"判不连坐"**：四张表在 §一.2–§一.5，落盘时间早于任何 `dotnet`（§〇 全节先建文件，之后逐轮追加，见工单 §一.6 顺序）；实测与表逐字符等（§三.3）。**"判不清"＝0 枚**——五轮的每一处非红判定都给了可达路径级理由，没有一处写成"应该不影响"；但"理由是否足够硬"属你的裁量，特别是 §六.2 第 5 条那类"读字段面"的判断，我把它们全部列出来了，没有藏在"不红"里。
3. **判据文件 SHA 是否 10 次＋出场面全等于 `4f1b2f6e…`**：**本刀没有做到"每轮两次"**，这里如实列出实际打印面而不是凑数。判据 SHA 被**单独打印**并当场可见的时刻共 **7 个**：①入场 prep 的 `out/w40-selfproof.txt`（02:41，那一处一次就打了 `main／carrier／blob／base` 四枚同值面）＋②续跑起点重取（P106 之前那一次 powershell `Get-FileHash`，本刀续跑的第一件事）＝2 个；③M8′ 施加前、④M8′ 还原后、⑤m07 施加前、⑥m08 施加前、⑦m10 还原后（`w40-apply.js status` 的 `judge …` 行）＝5 个；值全部逐字符等于 `4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`。**十处轮边界面（五轮各"施加前＋还原后"）里实际打印的是 5 处，未单独打印的另 5 处**＝m07 还原后、m08 还原后、m09 施加前、m09 还原后、m10 施加前；我这五处原来的写法是"＝`4f1b2f6e…`"，属于**由相邻时刻推得的值写成了当场值**，现已在 §二.2–§二.5 的 ②／⑩ 项逐处改成当场实际凭据（`status --short` 行数与其内容）。支撑这些处的不是 hash 而是这条更硬的面：每轮还原后 `git -C carrier status --short`＝**0 行**且载体 HEAD＝`51f149d` ⇒ 判据（跟踪文件）与 blob 逐字节同值；每轮施加后那一行**永远是产品文件**、从不出现 `tests/**`。出场再补三次（`out/final-six-sha.txt` 里判据的 主树侧／载体侧／blob 侧三枚同值）＋F 之后再一次（03:04，`w40-apply.js status` 打印同值）。是否接受"0 行式子＋逐行内容"替代每轮两次 hash，是你的裁量；若不接受，缺 hash 的那五处对应的轮次请你按你的口径处置，本刀不自签。
4. **载体终态**：`status --short`＝**0 行**（F 之前与 F 之后各数一次，同值）、`fd93ebe..HEAD`＝**2 行**（`51f149d`、`1e02205`，即波 33 那两枚提交）、`rev-parse HEAD`＝`51f149d0b722…`、六枚 SHA 逐字符等于 §〇.4（§三.2 那 12 次比对全 EQUAL）。
5. **收尾全量**：4 份先数满再逐份求和＝**1406／executed 1404／failed 0／跳过 2**（两枚 `NotExecuted` 开标签逐枚点名，§四.2）；**本刀未顶账、未为顶账另起构建**。
6. **`.trx` 与格数对账**：9 枚＝5＋4（差额 0，§五.4）；每轮 `total`＝56（五份）；出场合格数复看＝`[Fact]` 53／`[Theory]` 1／`[InlineData]` 3＝56 枚格、方法 54 枚（24＋30）＋2 枚夹具 `ProposeAsync`、文件 989 行（三侧同值）⇒ 净新增 0。
7. **每一枚红的性质**：五轮 11 枚不同格（去重后）的红**全部**是 `family=ASSERT-RED`（消息族 `Assert.Equal() Failure`／`Strings differ`），**0 枚**是"整格因异常而红"（仪器未打印任何 `Exception thrown` 族；`out/reading-*.txt` 五份可逐枚复核）。所以按你的口径这 11 枚都有断言级凭证；本刀不替你决定其中哪些署"第二换手"、哪些署"红证"。

**附加面（七件之外，本刀自己留下的两处，请你单看）**

- 甲：工单 §一.5 已知读数表第 `:43` 行（波 13 `run-m07`／`m08`／`m09`／`m10`"各恰 1"）与盘上产物不符的是 **m08 那一面**（盘上旧 49 格树＝恰 2，`<Counters failed="2">`，两份原始报告 §三.8 自己也写"红集 2 格"）；本刀按你 §九.0 定的规矩**自己重取了一遍**（`out/old-tree-readings.txt`），m07／m09／m10 三面与表相符、m08 一面与表差 1。登记在此，不当作本刀的预期来源（本刀预期全部来自 §一 从当前 56 格盘的推演）。
- 乙：P110 那枚预检的 `Progman`／`Program Manager` 窗带 `covers=True borderless=True`，脚本按桌面壳分级给 BUILD_OK；另有一枚 `preflight-P105b.txt`（mtime 02:36）在本刀 out 目录里、非本刀所跑也未被本刀覆盖（§五.3 有 mtime 桥接证据）。两条都属"仪器／台账面"，超出本 lane 权限。

**本刀跑到哪里、还剩什么、为什么（照实收尾，不写状态词）**：五轮定向全部跑完并全部有效，收尾全量 F 跑完，六枚 SHA 当面核过，预算 6／6 用满、构建额度 0／1（按三段判定合法跳过）、HOLD 0／16、作废轮 0、点名不闭合而退回的额度 0。剩下的活不在本刀配方里：§六.1 那十条照旧全挂，§六.2 那八条是本刀新留下的未验证面（其中第 1、2、3、4 条各对应一枚从没被单独摘过的门，要跑就得先出新的闭合点名表与新配方），§四 那三本账（D 类 28 枚、波 13 的 #12／#16–21／#23／#28–49、波 20 那 28 枚）一枚未碰。

## 九 守门人独立裁决（第四十波收口；本节为纯追加，§〇–§七 一个字节未改）

### 九.0 取数工具与自证先行（口径⓪；零 `dotnet`）

- 仪器＝`.local-notes/wave40-1029/gk40-adjudicate.js`（Node，只读盘上产物，不发起任何构建），整段输出＝`out/gk40-adjudicate.txt`（135 行）。
- **自证先过再裁**：八面带四元限定（波次＋判据文件名＋跑法＋该面结论字段值）的老读数跑出 **8/8 AGREE**；四面带我先给的预期值（波 39 的 `trx-M1`＝恰 1／`trx-M2`＝恰 3／`trx-M8-voided`＝恰 5、波 13 的 `run-full`＝failed 2 且 `total=1228`／跳过 2），另四面是纯当场重取（波 13 的 `run-m07`／`m08`／`m09`／`m10`，旧 49 格树）。不相符我就没有裁决权。
- 数红口径在三份独立仪器上一致：只数 `<UnitTestResult … outcome="Failed">` **开标签**，与同份 `<Counters failed>` 对账，再走一遍反向定位（先找 `outcome="Failed"`、回溯最近一个 `<`）；`<ResultSummary outcome="Failed">` 每份恰 1 处，一律不计为格（lane 报告里 `rawOutcomeFailedStrings` 与 `openTagFailed` 的差额就是它，五轮差额全＝1）。
- 补测一面 lane 已报、我原先没独立复数的：`trx-F` 的 `Pry.Core.Tests` 那份里 `testName` 含 `NarratorPrototypeTests` 的开标签＝**56 枚**，`outcome="Passed"` 56／`Failed` 0／`NotExecuted` 0，唯一方法名 54 枚（＝54 个带属性方法＋`[Theory]` 的 2 枚额外行）⇒「五轮还原后这棵判据在无突变态下整棵自洽」由我这双手也取到一次。

### 九.1 件 3′ 闭合点名门上线第一把的读数（门被使用≠门被检验）

| 轮（被改面） | §一 跑前落档的闭合预期红集（帧行号） | 我在盘上读到的实到 | 判定 |
|---|---|---|---|
| M8′＝`NarrativeRoundRunner.cs:115` | 恰 5＝`:540`／`:555`／`:569`／`:582`／`:865` | 5，格名／帧／消息族逐字符相符 | 有效 |
| m07＝`NarrativeContextCompiler.cs:203` | 恰 2＝`:217`／`:923` | 2，相符 | 有效 |
| m08＝`:246` | 恰 4＝`:289`／`:320`／`:961`／`:981` | 4，相符 | 有效 |
| m09＝`:239` | 恰 2＝`:304`／`:961` | 2，相符 | 有效 |
| m10＝`:284` | 恰 2＝`:202`／`:941` | 2，相符 | 有效 |

- 作废轮 **0**；因点名不闭合而退回的定向额度 **0**；五轮 `total` 全＝**56**（不是波 13 的 49）；每轮跳过格 0（`outcome="NotExecuted"` 开标签 0 枚）。
- 如实两条：① 这道门在本把**没有被"违例"检验过**——lane 五张表全部闭合，被检验的是"能否在起跑前把每轮 51–54 枚非红判定逐枚给到可达路径级理由"，它给了且没有一处写成"应该不影响"；门真正发挥作用的证据在上一把（波 39 的 M8 因实到 5＞事前点名 4 而整轮作废）。② 本把把那条作废原因**变成了一条可用事实**：#24–27 那枚突变在旧 49 格树（`run-m14`）＝恰 4，在现 56 格树＝恰 5，多出的那枚正是波 18 补格 @851（第二十一波报告 §一.2 的 C2）。⇒ 从此**红集枚数只能按当前树的盘上枚举取**，"旧树几枚所以现树几枚"的平移一律无效（工单 §三 首部那条禁令至此拿到双向读数）。

### 九.2 九枚目标的签账＝红证与第二换手同场结清

- 签账资格按波 39 报告 §九.2 那把同一尺使用成动作：判据 `tests/Pry.Core.Tests/NarratorPrototypeTests.cs` 三侧同值＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`（主树工作副本／载体工作副本／`git show 51f149d:` 的 blob，我的仪器 B 面逐字符核过），last-touch 是波 33 那两枚提交而非本刀之手 ⇒ 取红的那双手不是写判据的那双手。
- 逐枚（凭证＝本刀在载体里跑出的那份 TRX，全部 `family=ASSERT-RED`）：

| 波 13 格号 | 格名（剥 `Pry.Core.Tests.NarratorPrototypeTests.` 前缀） | 轮 | 红点帧 | 消息族 | 凭证 `.trx` |
|---|---|---|---|---|---|
| #24 | `Malformed_response_is_not_masquerading_as_a_chosen_silence` | M8′ | `:540` | `Expected: ProviderRejected / Actual: Silence` | `trx-M8p/…_02_51_40.trx` |
| #25 | `A_provider_that_ignores_cancellation_still_exits_as_timeout` | M8′ | `:555` | 同上 | 同上 |
| #26 | `A_provider_that_honours_the_timeout_token_exits_as_timeout_too` | M8′ | `:569` | 同上 | 同上 |
| #27 | `Provider_failure_becomes_a_named_error_shape_and_writes_nothing` | M8′ | `:582` | 同上 | 同上 |
| #7 | `Compiler_drops_an_optional_section_that_exceeds_its_own_share_and_counts_it_exactly` | m07 | `:217` | `Expected: Dropped / Actual: Included` | `trx-m07/…_02_53_37.trx` |
| #8 | `Compiler_trims_recent_messages_only_at_whole_unit_boundaries` | m08 | `:289` | `Expected: PartiallyIncluded / Actual: Included` | `trx-m08/…_02_54_37.trx` |
| #9 | `Compiler_shrinks_the_memory_wallet_unit_by_unit` | m08 | `:320` | `Expected: 1 / Actual: 2` | 同上 |
| #10 | `Compiler_removes_cross_conversation_units_and_names_the_count` | m09 | `:304` | `Expected: PartiallyIncluded / Actual: Included` | `trx-m09/…_02_56_58.trx` |
| #11 | `Compiler_keeps_absent_sections_as_not_supplied_instead_of_dropping_them` | m10 | `:202` | `Expected: 7 / Actual: 0` | `trx-m10/…_02_58_05.trx` |

- 格号与格名的对应不是我推的：波 13 原始凭证表（`docs/eval/reports/2026-10-23-hds003-005-cell-evidence-audit.md` §三.1 第 `:248`–`:252`、`:265`–`:268` 行）逐枚已给 #7…#11、#24…#27 的格名与旧树红点行，本刀实测帧行号与之逐位相同。
- 9 枚全部是"单枚、单轮、清洁红"：每轮 `git diff --stat`＝`1 file changed, 1 insertion(+), 1 deletion(-)`，载体 `status --short` 全程最多 1 行且那一行永远是 `src/` 产品文件。

### 九.3 五枚连坐格的处置＝不签、不扩、不减少任何欠账

@851（M8′）、@913（m07）、@952（m08＋m09 两轮各一次）、@975（m08）、@934（m10）全是波 18 补格；它们的"第二只手＋单枚清洁红"已在**第二十一波**当场结清（那份报告 §一.2 的 C2／C4／C5／C6／C7 五枚靶＋§二.12 那句"10 枚＝10／10 全部拿到单枚、单轮、清洁红"）。本刀只新增一条事实：这五枚在**另一枚方向的突变**下也响过，且 @952 在 m08（摘钱包门）与 m09（摘跨会话门）下各红一次、理由串剩下的那半互反（`"cross_scope"` ↔ `"unit_budget"`）。裁决＝登记为"该格钉的两半各自独立供料"的形状证据，**不算**第二换手（已结清）、**不算**本刀可署的账，也不许据此写"该门已双向反证"——穷举面仍按报告 §六.2 第 5 条挂着。

### 九.4 lane 自己交回的两处，我的裁法

- **甲（§七.3＝十处轮边界面只单独打印了 5 处判据 hash）：接受替代面，不判任何一轮作废。** 理由＝判据是跟踪文件，载体 HEAD＝`51f149d` 下 `status --short`＝**0 行**是与该 blob 逐字节同值的**充分**证据，hash 只是它的另一种写法；且每轮施加后 `status` 那一行**从未**出现 `tests/**`，这条过程面与终态面相互独立。lane 已自行把 §二.2–§二.5 的 ②／⑩ 五处从"＝`4f1b2f6e…`"改成当场实际凭据，没有涂改读数。**立一条写法口径**：由相邻时刻推得的值不许写成当场值——要么打出来，要么写明"由 X 面推出"。
- **乙（P110 的 `Progman`／`Program Manager` 带 `covers=True borderless=True`）：脚本给 BUILD_OK 是设计行为，本刀不动仪器。** v4 作废的原因正是"把桌面壳误判成游戏"；桌面壳不是应用，`covers/borderless` 描述的是跨双屏铺满的 Program Manager 本身。同枚预检里 `DESKTOP-SHELL-FOCUS … no app owns the foreground` 与 `free_memory_gb=11` 说明当时前台没有可被遮挡的应用，读数与放行同向。六枚预检（P106–P111）全 `BUILD_OK`、与六次 `dotnet` 一一配对（间隔 15–43 秒），无"跳过预检起跑"的形状；HOLD 重跑 0/16 未动用。登记为仪器已知形状，不改分级。

### 九.5 我的第三张错表（同族错误的第三次）与一条新的禁止形状

波 40 工单 §一.5 已知读数表那行「波 13 `run-m07`／`m08`／`m09`／`m10` **各恰 1**」错在 **m08**（盘上＝恰 **2**）。三处独立证据同值：波 13 原始报告 §三.8 自写"红集 2 格（U4 与 U5 共用 `AddUnitRun` 同一行）"；波 13 凭证表第 `:249` 行明写 `run-m08/…_14_59_19.trx` 的 `f=**2**`；我的仪器 A 面当场重取那份 `.trx`＝开标签红 2／`<Counters failed="2">`／`total=49`，红名＝`Compiler_shrinks_the_memory_wallet_unit_by_unit`＋`Compiler_trims_recent_messages_only_at_whole_unit_boundaries`。
性质与前两次不同：波 39 那两次是"盘面读数取错"，这次是"把四行合并成一句写"。⇒ **同一张表里"各恰 N"这种合并写法从此禁止**，枚数必须逐枚点名——合并写法会把一次读数错误摊成四行，还会让下一把把错的值当预期用。后果面＝**零**：本把五轮的预期全部来自 §一 从当前 56 格盘的推演，没有一处取用我这张表（lane §七 附加面甲把它单独登记、不当预期来源）。工单 §一.5 原文不涂改，只在此更正。

### 九.6 归位＝零落地（第六次执行），两条腿不许混写

- 本刀对产品与判据字节**净写入 0**，因此没有"落地"这件事：载体 `status --short`＝**0 行**、`log --oneline fd93ebe..HEAD`＝**2 行**（`51f149d`＋`1e02205`，全属波 33）、`rev-parse HEAD`＝`51f149d0b722e28a6566c93d19f48a94ca4c85df`——本刀零提交。
- **决定性那条 0 行式子**＝`git diff --stat 51f149d 6ae0803 -- src tests scripts Pry.slnx Directory.Build.props`＝**0 行**（我的仪器 B 面当场取，右端＝当时的 `main`）。另一条腿 `fd93ebe → HEAD`＝**6 files changed, 822 insertions(+)**，那是波 33 既有提交的面，不是归位判据。
- `51f149d` **不是** `main` 的祖先（当场 `git merge-base --is-ancestor` 返回非零）⇒ 归位通道从来不是 `merge --ff-only`；本把唯一需要落盘的是这份报告加两本登记文档，由我按路径 `git add`＋`git commit --only` 提交。

### 九.7 F／预算／产物三面（本刀不顶账）

- F：四份先数满再逐份 `<Counters>` 当场求和＝`108＋785＋280＋233＝`**1406**／executed `108＋783＋280＋233＝`**1404**／failed **0**／跳过 **2**；两枚跳过按 `outcome="NotExecuted"` 开标签逐枚点名＝`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`，与波 28–39 同名同枚 ⇒ **第六次同值复采**，在册基线不移动，本刀不为顶账另起任何构建。
- `.trx` 盘上 **9 枚**＝定向 5×1＋全量 1×4，与工单满额形状差额 **0**；结果目录 6 个，无 `-voided-` 目录。
- 预算：`dotnet` **6/6**、定向 **5/5**（额度用满、无退回）、全量 **1/1**、解决方案级构建 **0/1**（三段跳过判定成立，第一段"0 行式子"由我在九.6 独立复证）、HOLD **0/16**。
- 格数面：判据文件 **989 行**（三侧同值），`[Fact]` 53＋`[Theory]` 1×`[InlineData]` 3＝**56 枚格**，带属性方法 54 枚（`public void` 24＋`public async Task` 30），`Assert.Skip` **0** 处 ⇒ 本刀**净新增格 0／改断言 0／新增数字门槛 0**。
- 五枚预合成突变文本的盘上 SHA 与工单 §三.0 声明值逐字符 **MATCH**（`0400a577…`／`24e90d81…`／`a65399bb…`／`dfc91733…`／`24dbd7e5…`，字节数一并相符），且 M8′ 与波 39 那枚作废轮是同一份字节（`0400a577…`）⇒ 波 39 扣住的那四枚这一次确实是在"改坏产品码"之下响的，不是换了一份文本。

### 九.8 旧欠求和（等式从盘上产物写，不心算）

- 波 13 那本账 47 枚在第二十二波 §三.2 分成 B 类 19 枚（#1–11、#13–15、#22、#24–27）＋ D 类 28 枚；波 39 签 10 枚（#1–6、#13–15、#22），本刀签 **9** 枚 ⇒ 波 13 余 `47 −（10＋9）＝28`＝**D 类那 28 枚，一枚不少**；B 类 19 枚自此**全结清**。
- 波 20 那本账余 **28**（本刀零触碰）。
- ⇒ **旧欠 65 → 56**。求和式：`(47 − 19) ＋ 28 ＝ 28 ＋ 28 ＝ 56`。
- 拆法原文照旧 **46／45／1＝W2**；catalog **76** 未动。

### 九.9 押下一把的活（按依赖排序，不写"最短路径"）

1. **D 类 28 枚**：这本账的活是**先出配方**——报告 §六.2 第 1、2、3、4 条已点名四枚"从没被单独摘过的门"（`used + cost > total` 那半边、`:285` 的 `DispositionReason`、`:150`／`:155` 两支、`:77-:82` 的前奏跨会话门），每一枚都要一张新的闭合点名表；九.5 那条教训在此生效＝**表必须逐枚点名，不许合并**。
2. **波 20 那 28 枚**：同一批刀里的另一本账，配方未起。
3. §六.2 第 5 条（字段消费者穷举）：现有仪器只扫 `^\s*Assert\.` 那 11 个 token 面；要补的是"该字段还有谁读"的穷举，属仪器刀不属证据刀。
4. **HDS-007 那 23 枚**（波 30 归位后）：波 30 §九.5 押的五条照旧在前，与本刀不冲突。
5. 真实模型对照仍零读数——**要作者一句"可以跑"**，我不替他拍。

### 九.10 未验证面与照旧不许写的句子

- 报告 §六.1 那十条照旧全挂（真实模型对照／生产接线＝这 56 枚至今**零生产调用方**／旧数据真实继承／UI 可见行为／生产库与 `%LOCALAPPDATA%\PryCompanion`／待标定参数＝**特性不是缺陷**／并发与断电／旧链删除片与"构建时不把它打进包"那道门＝**未开始**／覆盖率／Release 序列点把红点记到多行调用起始行的机理）。
- §六.2 那八条是本刀新留下的，我逐条读回并认可其登记口径；第 1、2、3、4 条各对应一枚没被单独摘过的门；第 8 条（结构性）我把它写成定义而非欠账：判据字节未动 ⇒ 本刀的红证**天然不含**"判据写法本身"那一面。
- **四句话照旧不许写**＝新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链。本节没有任何一句越过这四条。
