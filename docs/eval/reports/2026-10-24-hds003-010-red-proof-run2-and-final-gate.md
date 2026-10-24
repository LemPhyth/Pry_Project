# 第二十七波续跑刀出口证据｜R3／R4 两枚红证＋R5 一轮构建与全量（守门人亲手跑，10-24）

- 工单来源：`docs/dispatch-hds-wave26-20261023.md` §六（配方）＋第二十六波报告 §九.5（前置裁定）
- 执行方：守门人亲手（不再派冷启动 lane；理由见 §〇.6）
- 签名口径：本报告是**证据刀**，不是结案单。状态词与"已归位"由归位刀自己写，本文件只登记盘上读数。

## 〇 入场与前置

1. 时点与盘面（当场实测）：`date`＝`Sat Oct 24 14:44:57 2026`；`git rev-parse --short HEAD`＝`bb44fa6`；`git rev-parse --short main`＝`bb44fa6`；`git status --short --branch`＝`## codex/ui-main-window-redesign` ＋ 7 行他人资产（两枚 `M ` 暂存项＝`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`；五枚 `??`＝`.qoder/`、`docs/character-learning-safety.patch`、`docs/research/HDS_INTERLUDE.md`、`docs/research/HDS_QQ_RESEARCH_INDEX.md`、`docs/research/QQ_BRIDGE.md`）。全程未读其内容、未改、未 `stash`／`clean`／`rm`。
2. 焦点预检：14:44 那次（未归档，如实登记）＝`VERDICT=BUILD_OK`，前景窗＝`msedge pid=5424 class=Chrome_WidgetWin_1 covers=True borderless=False`、`free_memory_gb=8`。14:45 起跑前重取一次并归档＝`.local-notes/wave27-1024/out/preflight-P1-beforeR3.txt`，同值 `BUILD_OK`（前景仍 msedge，`free_memory_gb=7.7`）。
3. 载体（复用第二十六波那一枚，无新建）：`C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave26-1023/carrier`，入场自证＝`git rev-parse HEAD`＝`24d1b6f8b0a4a3d199dcab832e15facc84a16a01`；`git status --short`＝**0 行**；`find . -type d \( -name obj -o -name bin \) | wc -l`＝**0**；`find . -name '*.trx' | wc -l`＝**0** ⇒ 上一把确实从未落过 `dotnet`。两枚靶文件入场 SHA＝`src/Pry.Core/Narrative/NarrativeContextCompiler.cs`＝`86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842`、`src/Pry.Core/Narrative/NarrativeRoundRunner.cs`＝`961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9`（＝本载体 HEAD-blob，按第二十六波工单 §七.1 的更正品径，不是主树线尾的 `92fbce48…`）。
4. **配方先做零 `dotnet` 字节自证（本刀新增的一道前置，上一把没有）**：在把两枚突变写进载体之前，先在临时副本上按 perl 一行改法合成突变文本，当场 `sha256sum` 与上一把记录的"副本预合值"对号：
   - R3 突变态＝`72340f0597b1278299dac9c27453d845f56ca013ba727584d0846d3d27dc6fb9`（与第二十六波报告 §一.1 ⑩ 逐字符相同）⇒ 相符。
   - R4 突变态＝`1126b6016693a0c0a42f3f634a08f0f9aa1807af2491383be3d6ef0352736135`（与第二十六波报告 §二 ⑩ 逐字符相同）⇒ 相符。
   - 副本留在 `.local-notes/wave27-1024/probe/{r3.cs,r4.cs}`（gitignored）。这一步把"跑出来的那轮到底是不是工单点名的那一枚突变"从推断变成凭证。
5. 改法（逐字，两边同一，先副本自证后载体落笔）：
   - R3＝`perl -i -pe 'BEGIN{$n=0} $n++; s/ \|\| used \+ cost > total// if $n==203'`（第 203 行 `        if (cost > cap || used + cost > total)` → `        if (cost > cap)`）。
   - R4＝`perl -i -pe 'BEGIN{$n=0} $n++; $_="" if $n==272; s/,\r?$/])))).ToLowerInvariant();/ if $n==271'`（第 271–272 两行并成一行、九项哈希输入去掉第⑨项 `draft.Intents` 那个成员，`NarrativeCommit` 字段赋值一字不动）。
6. 为什么这一把我亲手跑而不是再放一把 lane：第二十六波的全部前置（载体、结果目录、两枚突变文本、读盘仪器）都已就位且只剩"跑"这一件事；`BUILD_OK` 窗口已经打开，再派冷启动 lane 会把额度花在重读七份文档与重取入场读数上。换手资格不受影响：这 6 枚新格的判据字节作者是第二十五波那把 lane，不是我，我没有改过任何判据字节（本刀对 `tests/**` 零写入）。
7. 在册基线起点（当场从第二十五波报告 §八.4 读，不引用工单）＝**1333／executed 1331／failed 0／跳过 2**，分项 Client 108／App 233／Api 280／Core 712（executed 710）。

## 一 R3｜片 A 那格的红证＝**已取到，恰 1 枚红**

| 项 | 当场值 |
|---|---|
| ① 文件:行 | `src/Pry.Core/Narrative/NarrativeContextCompiler.cs:203` |
| ② 改前原文 | `        if (cost > cap \|\| used + cost > total)` |
| ③ 改后原文 | `        if (cost > cap)` |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/NarrativeContextCompiler.cs \| 2 +- `＝`1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤ 跑法整行 | `cd "C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave26-1023/carrier" && dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave27-1024/runs/r3-compiler203-newcell-red" --logger trx`（无 `--no-build`） |
| ⑥ TRX 文件名 | `24782_PHYTHLINK_2026-10-24_14_46_42.trx`（该目录 `*.trx` 实数＝**1 枚**） |
| ⑦ 红格全名＋消息要点＋红点行号 | testName＝`Pry.Core.Tests.NarrativeBudgetInequalityCellTests.Compiler_drops_a_whole_section_that_fits_its_own_share_but_not_the_remaining_input_budget`；`outcome="Failed"`、duration＝`00:00:00.1183890`；消息＝`Assert.Equal() Failure: Values differ / Expected: Ok / Actual: BudgetInequalityViolated`；红点＝`tests/Pry.Core.Tests/NarrativeBudgetInequalityCellTests.cs:line 65`。本轮计数＝`total="718" executed="716" passed="715" failed="1" notExecuted="0"`；除这一枚红之外，两条在册环境跳过按 `[SKIP]` 打印＝`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`、`CoreTests.Configured_sense_voice_model_can_run_offline`。**这一格不是常绿门**：删掉 `used + cost > total` 半项后，装配器把该段从"按剩余输入预算降级"变成"整体丢弃且不报不等式"，第 65 行那条 `Assert.Equal(NarrativeCompileReason.Ok, assembly.ReasonCode)` 当场转红＝`BudgetInequalityViolated` |
| ⑧ 还原命令 | `git -C "C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave26-1023/carrier" checkout -- src/Pry.Core/Narrative/NarrativeContextCompiler.cs` |
| ⑨ 还原后 `git status --short` 行数 | **0 行** |
| ⑩ 突变态与还原后 SHA-256 | 突变态＝`72340f0597b1278299dac9c27453d845f56ca013ba727584d0846d3d27dc6fb9`（＝工单期望值，载体实取）；还原后＝`86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842`（＝§〇.3 基线值，逐字符相符） |
| ⑪ 当轮预检整段 | `=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===` ／ `FOREGROUND: msedge pid=5424 class=Chrome_WidgetWin_1 style=0x17CF0000 rect=-1928,96,1936x1048 screen=1920x1080 covers=True borderless=False title=…Microsoft? Ed` ／ `UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=15532, TextInputHost pid=21228, explorer pid=13364` ／ `gpu_util=12% used=1666MB` ／ `free_memory_gb=7.7` ／ `VERDICT=BUILD_OK` |

## 二 R4｜片 B 那五枚节点的红证＝**已取到，恰 5 枚红；第 55 行那枚 `[Fact]` 响了，撤格硬线未触发**

| 项 | 当场值 |
|---|---|
| ① 文件:行 | `src/Pry.Core/Narrative/NarrativeRoundRunner.cs:271`–`:272`（`BuildCommit` 里九项哈希输入 collection expression 的第⑨项） |
| ② 改前原文 | `                string.Join(",", draft.Actions.Select(a => a.ActionId + ":" + (int)a.Mode)),` ＋ 下一行 `                string.Join(",", draft.Intents.Select(i => i.IntentId))])))).ToLowerInvariant();` |
| ③ 改后原文 | 一行＝`                string.Join(",", draft.Actions.Select(a => a.ActionId + ":" + (int)a.Mode))])))).ToLowerInvariant();`（数组少掉第⑨个成员连同它前面那枚逗号；`:274`–`:283` 的 `NarrativeCommit` 字段赋值一字不动） |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/NarrativeRoundRunner.cs \| 3 +-- `＝`1 file changed, 1 insertion(+), 2 deletions(-)`，hunk＝`@@ -271,2 +271 @@` |
| ⑤ 跑法整行 | `cd "C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave26-1023/carrier" && dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave27-1024/runs/r4-hash-sequence-mutated" --logger trx`（无 `--no-build`） |
| ⑥ TRX 文件名 | `24782_PHYTHLINK_2026-10-24_14_48_22.trx`（该目录 `*.trx` 实数＝**1 枚**） |
| ⑦ 红格全名（逐格点名，直接从 `outcome="Failed"` 节点的 `testName` 抓取，共 5 枚）＋消息要点＋红点行号 | 1. `Pry.Core.Tests.NarrativeCommitFactoryTests.BuildCommit_hashes_the_nine_inputs_into_the_absolute_digest`＝工点点名的第 55 行那枚 `[Fact]`，红点 `NarrativeCommitFactoryTests.cs:line 60`，消息＝`Expected: "99a0cc4bed9061763112b52bdb545df3d5457082887f757ae4"… / Actual: "2377fab8fc34f1730797b1501c8bde58b77408f0ff83924037"…`（pos 0 起 differ）。2. `…BuildCommit_digest_moves_when_exactly_one_hash_input_moves(shape: "narrative-prose", expectedHash: "508fda3f3f4b3ac3ec6d17d3a8fab9fd992d130ca85f9da362"···)`，红点 `:line 92`，Actual＝`5e083ea551e7edbac639ab7a7b98fa0a76f7640ce3d03528fb…`（pos 1）。3. 同法 `(shape: "action-mode", expectedHash: "8667f316fc4b04115cb13ab0a1ff948e3b176605bc26b6b7f6"···)`，红点 `:line 92`，Actual＝`0f63463022e6dd3ebe1ed4040ed1c5e8631ea192e2e9360697…`（pos 0）。4. 同法 `(shape: "processed-through", expectedHash: "ce3c9175ce887194f67576031754bdec249cd9ed8811e73286"···)`，红点 `:line 92`，Actual＝`c637309c8af687973c9fc427f889eddfcde6dab888496cb211…`（pos 1）。5. `…BuildCommit_static_extraction_yields_the_same_commit_the_runner_registers`（第 98 行那枚零行为漂移 `[Fact]`），红点 `:line 117`，Expected／Actual 与第 1 枚同值（`99a0cc4b…` 对 `2377fab8…`）。本轮计数＝`total="718" executed="716" passed="711" failed="5" notExecuted="0"`，两条在册环境跳过同值仍在（`CoreTests.Configured_sense_voice_model_can_run_offline` 与 `LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`）。**R3 那枚新格本轮保持绿**（不在上面 5 枚里）⇒ 两轮的各自主张互不串味，没有"批次红署成单枚突变归属"的形状 |
| ⑧ 还原命令 | `git -C "C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave26-1023/carrier" checkout -- src/Pry.Core/Narrative/NarrativeRoundRunner.cs` |
| ⑨ 还原后 `git status --short` 行数 | **0 行** |
| ⑩ 突变态与还原后 SHA-256 | 突变态＝`1126b6016693a0c0a42f3f634a08f0f9aa1807af2491383be3d6ef0352736135`（＝工单期望值，载体实取）；还原后＝`961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9`（＝本载体 HEAD-blob 口径，见第二十六波工单 §七.1；**不是**主树线尾的 `92fbce48…`） |
| ⑪ 当轮预检整段 | `=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===` ／ `FOREGROUND: msedge pid=5424 class=Chrome_WidgetWin_1 style=0x17CF0000 rect=-1928,96,1936x1048 screen=1920x1080 covers=True borderless=False title=Lecture 12 Geometry 3_????_bilibili ??? 5 ??? -` ／ `UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=15532, TextInputHost pid=21228, explorer pid=13364` ／ `gpu_util=10% used=1598MB` ／ `free_memory_gb=7.7` ／ `VERDICT=BUILD_OK` |

**第 5 枚红为什么算数、与工单 §〇.6.4 那条"两侧同工厂恒等不算覆盖证据"不冲突**：第 98 行那枚 `[Fact]` 里被否掉的那一行（`:117`）钉的是**绝对字面量** `BaseHash`（＝同一枚 `99a0cc4b…`），不是"工厂 A 对工厂 B"的恒等式。也就是说这枚格在"突变面＝哈希输入集合"上不是恒等门：突变让产品码当场算出的摘要漂走，它就把摘要按字面量拉回来对。所以 5 枚红里 4 枚是工单点名的字面量门（第 55 行 `[Fact]` ＋三枚 `InlineData`），第 5 枚是同一字面量在零行为漂移那枚格里的第二处守点；两处红点行号不同（`:60` 与 `:117`）、消息形状不同（前者比 `commit.ContentHash`、后者比 `direct.ContentHash`），可判唯一因。

**硬线状态（工单 §二末句）＝未触发**：第 55 行那枚 `[Fact]` 在突变下**响了**（红点 `:line 60`）。⇒ 不许撤格、那 6 枚新格全部拿到"改坏产品码会响"的盘上凭证、字节归位的前提满足。

## 三 R5｜一轮解决方案级构建＋一轮全量＝**均取到读数**

### 三.1 构建（一次，落在载体干净面）

- 跑前自证两行＝`git rev-parse HEAD`＝`24d1b6f8b0a4a3d199dcab832e15facc84a16a01`；`git status --short | wc -l`＝**0**。
- 跑法＝`cd "…/qa-wave26-1023/carrier" && dotnet build Pry.slnx -c Release`，整段原文归档＝`.local-notes/wave27-1024/out/build-R5.txt`；`exit=0`。
- 警告计数＝`grep -c -i "warning"` 对该整段文件实取＝**0**；末三行原文＝`已成功生成。`／`    0 个警告`／`    0 个错误`，`已用时间 00:00:10.54`。
- **这一轮就是第二十四波 §五.1 那条欠账的凭据**：第二十五波 §三.4 那次 `xUnit2000` 参数顺序修复，从落笔到本刀从未被任何构建证明过；本刀首次以"0 警告 0 错误"证明它。该读数不是"我判断没警告"，是盘上产物里 `warning` 命中数＝0 ＋ 官方计数行＝0。

### 三.2 全量（一次，`--no-build`）

- 跑前预检＝`preflight-P4-beforeR5full.txt`，`VERDICT=BUILD_OK`（`free_memory_gb=6.3`）。
- 跑法＝`cd "…/qa-wave26-1023/carrier" && dotnet test Pry.slnx -c Release --no-build --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave27-1024/full" --logger trx`；`exit=0`；整段原文归档＝`out/test-R5full.txt`。
- 先数文件＝`ls …/full/*.trx | wc -l`＝**4 枚**（在跑 `measure-fixed-eval.ps1` 之前数，按第二十六波工单 §七.4 的写入面次序）。
- 逐份读数（`<Counters>` 原样）＋程序集归属（按文件内 `testName` 的 `Pry.<X>.Tests` 前缀实数，不靠控制台印象）：

| TRX 文件名 | 归属 | total | executed | passed | failed | notExecuted 计数器 |
|---|---|---|---|---|---|---|
| `24782_PHYTHLINK_2026-10-24_14_50_09.trx` | Core（前缀计数＝Core:718） | 718 | 716 | 716 | 0 | 0 |
| `24782_PHYTHLINK_2026-10-24_14_50_10.trx` | Client（Client:108） | 108 | 108 | 108 | 0 | 0 |
| `24782_PHYTHLINK_2026-10-24_14_50_10[1].trx` | Api（Api:280） | 280 | 280 | 280 | 0 | 0 |
| `24782_PHYTHLINK_2026-10-24_14_50_12.trx` | App（App:233） | 233 | 233 | 233 | 0 | 0 |

- 两条跳过按 `outcome="NotExecuted"` 的 `testName` 逐一点名（都在 Core 那份里）＝`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`、`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`。四份 `notExecuted` 计数器全＝0，不看。
- 四份当场求和（由这四枚 `<Counters>` 直接相加，无第三处来源）＝**total 1339／executed 1337／passed 1337／failed 0／跳过 2**。

### 三.3 独立复核

- 跑法＝`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/measure-fixed-eval.ps1 -ResultsDirectory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave27-1024/full"`（在载体目录内跑，跑序遵守工单 §七.4：先数完 4 枚、取完读数才跑它）。`exit=0`，整段原文＝

  ```
  Catalog valid: 76 permanent scenarios. Mappings are partial evidence only.
  Passed=1337; Failed=0; Other=2. Evidence: C:\Users\24782\Desktop\Pry_Project\.local-notes\wave27-1024\full\fixed-eval-coverage.json
  ```

- 与三.2 自洽＝`Passed 1337`＝四份 `passed` 求和；`Other 2`＝那两条 `NotExecuted`；`Failed=0`；catalog 仍 76（本刀未增删任何永久场景）。
- 副作用如实登记＝它已向同一目录写入／覆盖 `fixed-eval-coverage.json`，该目录的 `*.trx` 计数仍是 4 枚（json 不是 TRX），但**这一轮的覆盖读数不可与上一轮的覆盖读数并列引用**（同目录复跑会抹掉前一份）。

### 三.4 产品面自证（载体）

`git diff --stat a804a5371c3d726ea97ddb2dd8a4d735ad3f3122..HEAD -- src tests scripts Pry.slnx Directory.Build.props`＝恰好三枚文件、`3 files changed, 304 insertions(+), 3 deletions(-)`＝`src/Pry.Core/Narrative/NarrativeRoundRunner.cs`（7 行面）＋`tests/Pry.Core.Tests/NarrativeBudgetInequalityCellTests.cs`（121 行，新）＋`tests/Pry.Core.Tests/NarrativeCommitFactoryTests.cs`（179 行，新）；`git diff --check a804a53..HEAD` 干净（`exit=0`）；载体 `git status --short`＝0 行。

## 四 对账等式与归位裁决

1. **等式（右端由 §三.2 那四份当场求和取出）**：起点 1333（第二十五波报告 §八.4 的在册值）＋ 本刀新格 6（片 A 1 枚 `[Fact]` ＋ 片 B 5 枚节点）＝ **1339**；四份 TRX 实求和＝1339、executed 1337、failed 0、跳过 2。两侧对得上，Core 分项 712→718 是这 6 枚的唯一去处。
2. **6 枚新格全部拿到"改坏产品码会响"的盘上凭证**：R3 那一枚红点 `NarrativeBudgetInequalityCellTests.cs:line 65`；R4 那五枚红点 `NarrativeCommitFactoryTests.cs:line 60`（1 枚）、`:line 92`（三枚 `InlineData`，可按 shape 参数串逐格判唯一因）、`:line 117`（1 枚）。两轮各恰其数（1 枚／5 枚），互不串味。工单 §二末句那条"第 55 行 `[Fact]` 不响就当场撤格"的硬线**未触发**，没有撤掉任何一格。
3. **归位前提（第二十五波 §八.4 扣住 `git merge --ff-only 24d1b6f` 的那三句话）已满足**：那 6 枚不再是"未证伪"，`xUnit2000` 修复拿到 0 警告实测，出口读数取得到。⇒ 归位由守门人执行，落法见 §五.1。
4. **第二换手口径**：本刀取红的手＝守门人，既不是这 6 枚判据字节的作者（作者＝第二十五波那把 lane，`24d1b6f`），也没有改过任何判据字节（本刀对 `tests/**` 零写入，只临时改坏过两枚**产品**文件并逐字节还原）。按波 24 立的资格判据，这 6 格从今天起有第二换手在册凭据；**但欠账总数仍＝75 格**（波 13 余 47＋波 20 余 28 那两批与这 6 格无交集），本刀不减那 75，只把"这 6 格从未有第二双手"这一条从欠账里划掉。
5. **顶账权**：等式归本刀算，在册基线的改写归守门人归位刀。本刀不自封"新基线 1339"，只在 §四.1 给出四份 TRX 的当场求和与出处。

## 五 归位后的补记（守门人亲手追加，签名 `eval-QA-WAVE27-GK-1024`）

1. 落法：载体提交 `24d1b6f` 的父是 `a804a53`，而主树线尾已走到 `bb44fa6`（其间 `62f8fd1`、`bb44fa6` 两把纯文档刀），⇒ `git merge --ff-only 24d1b6f` **不是** fast-forward，照字面跑必然失败。本次改用**按路径落地**：`git checkout 24d1b6f -- <那三枚文件>`（只写这三枚他人未碰的路径）→ `git commit --only -- <同三枚路径>`（不把他人两枚 `M ` 暂存项卷进提交）→ 提交后逐字节复核他人暂存项的索引 blob 未变。落完再单独提交文档刀（本报告＋`PROJECT_PLAN.md` §9＋`docs/full-scope-sprint.md` §0/§61＋本工单的续跑前置划账），最后按既有口径 ref-only 快进 `main`。
2. 落地前后各数一次：主树 `git status --short` 行数与他人资产签名（两枚 `M ` 暂存项的索引 blob、五枚 `??`）逐行原样；`git diff --check` 干净。

## 六 仍未验证／未取到（不用"已完成"混过去）

真实模型对照＝零读数；宿主接线与切换＝零（`NarrativeTurnPipeline` 仍零生产调用方，`BuildCommit` 提出 `private` 不等于接上）；UI 可见行为＝零读数；生产库＝零接触（`%LOCALAPPDATA%\PryCompanion` 未碰）；待标定参数权威值＝未定；旧链删除片与"构建时不把旧链打进包"那道门＝未开始；`:178` 那道防御支按名钉拿不到红这条仍登记为不可达，**不许被读成"不需要守"**；其余 75 格的换人复采照旧欠着。三句话照旧不许出现在任何页面：新链已接线／旧链可删／不进包已做到——本刀没有让任何一句成立。

## 七 盘上产物清单（本刀全部足迹）

- 主树：本报告一枚（`docs/eval/reports/2026-10-24-hds003-010-red-proof-run2-and-final-gate.md`，未跟踪→由归位文档刀提交）＋ §五.1 那两把提交。
- `.local-notes/wave27-1024/`：`out/preflight-P1-beforeR3.txt`、`out/preflight-P2-beforeR4.txt`、`out/preflight-P3-beforeR5build.txt`、`out/preflight-P4-beforeR5full.txt`（4 枚，全 `BUILD_OK`）、`out/build-R5.txt`、`out/test-R5full.txt`、`out/measure-fixed-eval.txt`、`probe/{r3.cs,r4.cs}`（§〇.4 的零 `dotnet` 字节自证副本）、`runs/r3-compiler203-newcell-red/`（1 枚 TRX）、`runs/r4-hash-sequence-mutated/`（1 枚 TRX）、`full/`（4 枚 TRX＋`fixed-eval-coverage.json`）。
- 载体 `.local-notes/qa-wave26-1023/carrier`：本刀之后 `git status --short`＝0 行、`HEAD`＝`24d1b6f…`、两枚靶文件 SHA 回到 §〇.3 的交付面值；`bin`／`obj` 由本刀的 4 次 `dotnet` 产生（gitignored，不删）。
- `dotnet` 调用合计＝**4**（R3 定向、R4 定向、R5 构建、R5 全量），盘上凭据＝上面那 6 枚 TRX ＋ 一次构建整段日志；突变轮 2 枚各用独立结果目录、无 `--no-build`、无复用。预算对照工单 §〇.8：定向 ≤3 用了 2、构建 ≤1 用了 1、全量 ≤1 用了 1，未自扩。
