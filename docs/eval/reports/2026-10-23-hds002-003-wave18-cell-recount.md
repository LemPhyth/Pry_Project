# 第二十一波·换人复判报告：第十八波那 10 枚新格逐枚取"单枚、单轮、清洁红"＋它那 23 轮的归属分账＋补它欠的出口读数

- 报告 id：`eval-QA-WAVE21-1023`
- 工单：`docs/dispatch-hds-wave21-20261023.md`（id `HDS-WAVE21-1023`，起刀点 `e8a842a`，复判对象＝`147aa191c712aa7d627d74543b044df86249160e`）
- 性质：**判据复核刀，零产品代码净改动**（每轮单枚突变当场施加、当场 `git checkout --` 字节还原）；两份判据文件本刀一字未改
- 执行方：冷启动 lane（第十八波的**换人复核方**，不是原作者；本报告不构成归位，归位权在守门人）
- 写法：按批次增量落盘，动第一次 `dotnet` 前先把 §〇／§一 写满建成本文件。

---

## 〇 入场判据（当场实测，原样贴出）

### 〇.1 时点与主树现状（未读、未改、未提交他人名下项）

`date`（载体入场第一条）→ `Fri Oct 23 17:51:30 2026`

主树 `git status --short --branch`（开工前当场实测，原样）：

```
## codex/ui-main-window-redesign
M  docs/dispatch-tip1020-02-memory-group-judgment.md
M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs
?? .qoder/
?? docs/character-learning-safety.patch
?? docs/eval/reports/2026-10-23-qa-wave18-unheard-gates.md
?? docs/research/HDS_INTERLUDE.md
?? docs/research/HDS_QQ_RESEARCH_INDEX.md
?? docs/research/QQ_BRIDGE.md
```

主树 `git rev-parse HEAD` = `bceb80655e2f452a8b0f0440aa38ce04ba93aa91`（＝`e8a842a` 之上再叠两把文档刀 `a04e1b4`／`bceb806`）。两枚他人 `M ` 暂存项与全部 `??` 本刀一律**未读、未改、未提交、未 stash、未 clean、未 rm**；第十八波那份报告（`?? docs/eval/reports/2026-10-23-qa-wave18-unheard-gates.md`）只读、未动一个字节。

本刀在主树的唯一笔迹＝本报告文件自身（未 add、未 commit，与第十六／十七／十八波 lane 同形状）。

### 〇.2 载体（工单 §一 命令逐字照抄，零改写）

```
git -C C:/Users/24782/Desktop/Pry_Project worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave21-1023/carrier e8a842a
cd C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave21-1023/carrier
git cherry-pick 147aa191c712aa7d627d74543b044df86249160e
git status --short | wc -l
git diff --stat def0faf..HEAD -- tests
```

`worktree add` 当场输出＝`Preparing worktree (detached HEAD e8a842a)` / `HEAD is now at e8a842a feat(narrative,hds004+hds005): 归位第十六＋十七波两把隔离刀…`。

`cherry-pick` 当场输出（**零冲突**，与守门人 §9 17:5x 那句"`147aa19^` 与 `e8a842a` 在这两份测试文件上零漂移"相符）：

```
[detached HEAD 1998790] test(qa,wave18): 落第十八波方向对称格——9 组落门补 7 枚、正向 None/放行侧补 3 枚
 Date: Fri Oct 23 16:50:47 2026 +0800
 2 files changed, 217 insertions(+)
```

入场三行判据（载体内当场实测）：

```
date                    → Fri Oct 23 17:51:30 2026
git rev-parse HEAD      → 1998790d3edff99d698f39d76b08375329301251   （cherry-pick 后的全号）
git status --short | wc -l → 0                                       （预期 0 ✓）
```

`git log --oneline e8a842a..HEAD` = **1 行**（`1998790`）⇒ 载体干净不是"什么都没带"，而是那一把判据刀已在我树里落成一枚提交（守门人 §9 那条仪器教训照办：先看 `git log 基点..HEAD` 再看 `status`）。

### 〇.3 起点 src 面（零产品代码自证）

- `git diff --stat e8a842a..HEAD -- src` = **空**（cherry-pick 只动两份测试文件）。
- 载体里三枚将被突变的 `src` 文件基线 SHA-256（当场 `sha256sum`，与第十八波报告 §二 的 pristine 值**逐位相同** ⇒ 两波读的是同一份字节）：

```
NarrativeRoundRunner.cs    92fbce48c7948105479a6ca3fe75697dc24201c4f4d120b7ecc086729c353570
NarrativeContextCompiler.cs 86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842
NarrativeDraftValidator.cs 88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c
（未使用，仅列作对照）NarratorContracts.cs 3a33a8151e61c3396963dac451dffcb46acb2f02807f140d18f694d6a52a9f8c
```

### 〇.4 工单与盘面差异（就地登记，不涂改工单）

1. 工单 §一 末行注释说 `git diff --stat def0faf..HEAD -- tests` 只应见两份测试文件／217 insertions。盘上真值＝**4 files changed, 1653 insertions(+)**：

```
tests/Pry.Core.Tests/NarrativeIdentityContractTests.cs      |  50 ++
tests/Pry.Core.Tests/NarrativeLifecycleCoordinatorTests.cs  | 732 +++++++++++++
tests/Pry.Core.Tests/NarrativeSqliteCommitDeliveryTests.cs  | 704 ++++++++++++
tests/Pry.Core.Tests/NarratorPrototypeTests.cs              | 167 +++++
4 files changed, 1653 insertions(+)
```

   差额不是漂移而是**基点不同**：我的起刀点是 `e8a842a`（第十六＋十七波归位后的线尾），它本身就在 `def0faf` 之上落了那两枚新测试文件（732＋704＝1436 行）。第十八波那把的真实形状按 `git show --stat HEAD` 当场取＝**2 files changed, 217 insertions(+), 0 deletions**，与工单 §〇 那句"217 insertions／0 deletions"逐字相符。归因＝工单作者（守门人）把"复算式"写成 `def0faf..HEAD` 而载体基点是 `e8a842a`；按工单 §三"以盘上为准"处理，本刀改用 `e8a842a..HEAD`（src 面）与 `git show --stat HEAD`（本把内容）两条自证，不改工单原文。
2. 工单 §三 给的先验形状（`NarrativeIdentityContractTests` 62→65、`NarratorPrototypeTests` 49→56）**与盘上相符**，当场实测见 §一.1，无需更正。

---

## 一 活单①：10 枚格逐枚点名（全部当场从盘上取，不抄工单）

### 一.1 两份文件各补了多少（载体 `grep -c` 当场实测）

```
NarratorPrototypeTests.cs        [Fact] 53  [Theory] 1  [InlineData(] 3  →  格数 53＋3 ＝ 56（起点 49，＋7）  行数 989
NarrativeIdentityContractTests.cs [Fact] 22  [Theory] 5  [InlineData(] 43 →  格数 22＋43 ＝ 65（起点 62，＋3）  行数 590
合计 121 格（＝第十八波报告 §二.0 那枚 run-clean 探针的 total）
```

`git diff 147aa19^..147aa19` 逐枚点名新增的方法签名（`grep '^+'`，当场取）＝7 枚 `public (async Task|void)` ＋ 1 枚 `[Theory] + [InlineData(0d)] + [InlineData(1d)]`（＝2 格）＋ 1 枚 `[Fact]` ⇒ **10 格**，与守门人 §9 17:1x 那条"逐枚点名"（2 枚 async＋5 枚 void；`Candidate_at_…` 算 2 格）逐位相同。

### 一.2 十枚格的靶（每枚：守 `src/` 哪一行 → 单枚突变配方 → 预测红点行）

行号全部当场读码得出（`awk` 打 `src` 原文行号，不抄注释也不抄工单）。测试侧红点行＝该格断言所在行（载体行号实测）。

| # | 格全名（`Pry.Core.Tests.` 前缀见 §二） | 测试文件:行 | 守的 src 判定线（原文） | 单枚突变配方 | 预测红点 |
|---|---|---|---|---|---|
| C1 | `Mixed_round_takes_the_immediate_branch_and_registers_no_intent_there` | `NarratorPrototypeTests.cs:832` | `NarrativeRoundRunner.cs:150` `if (immediate.Length > 0)` | `→ if (immediate.Length > 0 && deferred.Length == 0)` | `:838`（`Expected: Immediate Actual: Deferred`） |
| C2 | `Provider_side_cancellation_with_a_live_host_token_is_named_timeout_not_failure` | `:851` | `NarrativeRoundRunner.cs:333` `return NarratorOutcome.Timeout($"provider 在 …` | `Timeout(` → `Failed(` | `:864`（`Expected: TimedOut Actual: ProviderFailed`） |
| C3 | `Unified_budget_inequality_is_pinned_at_the_edge_and_one_token_past_it` | `:890` | `NarrativeContextCompiler.cs:443-444` `… + SafetyTokens <= ContextTokens;` | `<=` → `<` | `:902`（`Assert.True() Expected: True Actual: False`） |
| C4 | `Whole_section_segments_carry_their_own_source_reference_even_when_dropped` | `:913` | `NarrativeContextCompiler.cs:294` `SourceRefs = [contentSourceRef], EmittedText = emittedText,`（`Simple` 唯一来源引用产地） | `SourceRefs = [contentSourceRef]` → `SourceRefs = []` | `:920`（`Collections differ Expected: ["scene:current"] Actual: []`） |
| C5 | `Absent_sections_are_named_not_supplied_rather_than_budget` | `:934` | `NarrativeContextCompiler.cs:285` `DispositionReason = "not_supplied",` | `"not_supplied"` → `"dropped"` | `:942` |
| C6 | `Unit_run_losing_units_to_both_reasons_names_both_together` | `:952` | `NarrativeContextCompiler.cs:265` `… : "cross_scope_and_unit_budget")` | `"cross_scope_and_unit_budget"` → `"cross_and_unit"` | `:961` |
| C7 | `Unit_run_that_fits_no_unit_at_all_is_dropped_and_named_budget` | `:975` | `NarrativeContextCompiler.cs:263` `var reason = included == 0 ? "budget"` | `? "budget"` → `? ""` | `:982` |
| C8 | `Candidate_at_a_closed_interval_confidence_edge_is_released(confidence: 1)` | `NarrativeIdentityContractTests.cs:553`（`[InlineData(1d)]` `:552`） | `NarrativeDraftValidator.cs:318` `if (candidate.Confidence is < 0 or > 1)` | `> 1` → `>= 1`（把闭区间上界误写成开区间） | `:569` |
| C9 | 同族格 `(confidence: 0)`（`[InlineData(0d)]` `:551`） | `:553` | 同上 `:318` | `< 0` → `<= 0`（下界误收紧） | `:569` |
| C10 | `Intent_whose_deadline_equals_its_earliest_is_released` | `:578` | `NarrativeDraftValidator.cs:281` `… is { } latest && latest < earliest)` | `<` → `<=` | `:588` |

### 一.3 跑前读码：为什么这十枚配方各自应当只放一枚红（同落风险先排，不是跑后再找借口）

- **C1**：全类只有 `:834` 一枚 Draft 同时含 `ImmediateAction()` 与 `DeferredAction()`（`grep -n` 当场实测：`:458/:630/:646/:661/:673/:686/:713/:748/:763/:778/:792/:804` 全是即时单枚；`:505/:601/:616` 全是延迟单枚）。延迟单枚那三格在突变前后都走 `:155` 分支，形状不变 ⇒ 结构上单枚。
- **C4**：`Simple(...)`（`:288-295`）只被 `AddSection` 的 `:206`／`:210` 两处调用（State／BehaviorReferences／ExpressionPatterns／CapabilityResult 四节）。全类 `SourceRefs` 断言共 8 处（`:294/:309/:920/:925/:966/:987` 加 `EmittedSourceRefs` 三处），其中只有 `:920/:925` 点的是 `Simple` 产出的段；`:372-375`（`Compiler_carries_source_references_into_every_emitted_section`）的 4 个 ref 是 `identity:canon`／`advisory:block`／`input:101`×2，那格的 `Payloads()` 不给 state／behavior ⇒ 不经 `Simple`（读码）。
- **C5／C6／C7**：三个字面量在全仓测试侧各只有一处点名（`grep -n`：`"not_supplied"`→`:942-943`；`cross_scope_and_unit_budget`→`:961`；C7 见下）。
- **C7 与"budget"字面量的撞名风险已排**：`"budget"` 在测试侧被点三处（`:218`／`:924`／`:982`），但 `:218` 与 `:924` 走的是 `AddSection` 的 `NarrativeContextCompiler.cs:206`（`Simple(..., Dropped, "budget", ...)`），与 `:263`（`AddUnitRun` 的 `included == 0` 支）是两个不同产地 ⇒ 改 `:263` 不碰它们。
- **C3**：`WithinUnifiedBudget` 在测试侧共 4 次读取（`:343` 与 `:902/:903/:904`）。`:343` 那格的装配体按 `:337-339` 的确切算式（`PreludeCost + advisory + state + event + 8 + memory + 8`）远小于 3456，等号位不在它身上（读码）；若实测出现 2 红，按消息形状与红点行号归因、并在报告里如实登记为"同落多枚"，不改为第二枚配方凑绿。
- **C2**：`:331-334` 的另一批经过者是 `:549`／`:562` 那两枚 40 ms 时序格（第十八波报告称它们同轮仍绿）。本刀先按同一条配方独立复采；若出现 2 红＝登记"同落多枚＋时序浮动"，不重试到绿。
- **C8／C9／C10**：`Confidence` 在 identity 全类只出现在 `:362`（`.9`）、`:416`（`[InlineData] 1.5 / -0.1`，断言**拒绝**）、`:437`（`.9`）、`:563`（本刀 0／1）⇒ `>= 1` 只伤 `1d` 行、`<= 0` 只伤 `0d` 行，`1.5`／`-0.1` 行改后仍被拒、照旧绿。`LatestDueAtUtc` 全类只两处：`:461`（`Now+10` 早于 `Now+20`，`<=` 后仍拒绝）与 `:584`（相等侧）；`:529` 只给 `EarliestDueAtUtc`（门要两枚都非空才走）⇒ `<=` 只伤 C10。`NarrativeCommitAndActionGuardTests` 的 `Confidence` 全是 `.9`，且该轮按类过滤（见 §一.4），不进本轮读数。

### 一.4 跑法与预算（固定写法，逐轮独立结果目录，不复用同名 LogFileName）

```
dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release ^
  --filter "FullyQualifiedName~<NarratorPrototypeTests | NarrativeIdentityContractTests>" ^
  --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave21-1023\runs\run-w21-NN" ^
  --logger trx
```

- 不用短参 `-r`（会拼进各工程 `bin` 目录，出现"零测试仍 exit=0"）；必给 `--logger trx`（不给就没有 TRX）；突变轮**一律不带 `--no-build`**（要编译那一枚突变），收尾那一轮全量才用 `--no-build`。
- 每轮跑完先 `ls *.trx` 数份数，再读 `<Counters>`；跳过不进 `notExecuted`（恒 0），只能按 `outcome="NotExecuted"` 点名。
- 预算：解决方案级构建 ≤1（本刀用在收尾，跑在还原后的字节上）、全量 ≤1、定向 ≤12（10 枚各一轮＋2 轮备用）。用满即停，剩余格按"未取到红"登记。
- 每一次 `dotnet` 之前先在主树跑 `.local-notes/game-preflight6-1022.ps1`，整段输出贴进当轮；`VERDICT=BUILD_OK` 才起跑，读到 `HOLD` 就地停刀。
- 十一项字段口径（逐轮齐）：①文件:行 ②改前原文 ③改后原文 ④`git diff --stat` ⑤跑法整行 ⑥TRX 文件名 ⑦红格全名＋消息要点＋红点行号 ⑧还原命令 ⑨还原后 `git status --short` 行数 ⑩突变态与还原后 SHA-256 ⑪当轮预检整段。

---

## 二 活单①反证登记（十一项字段，逐轮）

> 公共字段（七轮同值，先列一次）：载体全号 `1998790d3edff99d698f39d76b08375329301251`（＝`e8a842a`＋cherry-pick `147aa19`）；跑法＝下面每轮的整行原文（全部**零 `--no-build`**、每轮独立 `--results-directory`、无同名 `LogFileName`）；还原命令一律 `git checkout -- <那一枚 src 文件>`（从不 `git checkout -- .`／不 reset／不 clean）；每轮跑完先 `ls *.trx` 数份数＝**1** 再读 `<Counters>`。
> 判据文件（两份测试）本刀一字未改：七轮 `git diff --stat` 每轮都是 **1 file changed**，且那 1 枚永远是 `src/` 下的产品码文件；`tests/**` 从未出现在任何一轮的 diff 里。

### 2.1 W21-01｜C1 `Mixed_round_takes_the_immediate_branch_and_registers_no_intent_there`

| 字段 | 读数 |
|---|---|
| ①文件:行 | `src/Pry.Core/Narrative/NarrativeRoundRunner.cs:150`（`git diff -U0` 头＝`@@ -150 +150 @@ public sealed class NarrativeRoundRunner`） |
| ②改前原文 | `        if (immediate.Length > 0)` |
| ③改后原文 | `        if (immediate.Length > 0 && deferred.Length == 0)` |
| ④`git diff --stat` | `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤跑法整行 | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~NarratorPrototypeTests" --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave21-1023\runs\run-w21-01" --logger trx` |
| ⑥TRX | `runs/run-w21-01/24782_PHYTHLINK_2026-10-23_17_56_20.trx`（份数 1） |
| ⑦红格 | `Pry.Core.Tests.NarratorPrototypeTests.Mixed_round_takes_the_immediate_branch_and_registers_no_intent_there`（**本波复核的落盘新格**）／`Assert.Equal() Failure: Values differ  Expected: Immediate  Actual: Deferred`／红点 `NarratorPrototypeTests.cs:line 838`。`<Counters total="56" executed="56" passed="55" failed="1" notExecuted="0">` ⇒ **单枚清洁红**，同轮其余 55 格全绿（含即时单枚那 12 格与延迟单枚 `:505/:601/:616` 三格，与 §一.3 的结构论证相符） |
| ⑧还原命令 | `git checkout -- src/Pry.Core/Narrative/NarrativeRoundRunner.cs` |
| ⑨还原后 status | `git status --short \| wc -l` ＝ **0** |
| ⑩SHA-256 | 基线/还原 `92fbce48…353570` → 突变 `5de8ff66d16160d6c1c3d7cbda68e566ba4d9b0184acbdd99d0823e13c179227` → 还原 `92fbce48c7948105479a6ca3fe75697dc24201c4f4d120b7ecc086729c353570`（＝基线，equal=True）。附注：该突变哈希与第十八波 m18-01 自报的 `5DE8FF66…179227` 逐位相同 ⇒ 两波读的是同一份字节、同一枚配方 |
| ⑪当轮预检 | `VERDICT=BUILD_OK`；`FOREGROUND: PickerHost pid=60196 class=Shell_SystemDialog … covers=False borderless=False`；`gpu_util=2% used=8556MB`；`free_memory_gb=12.3` |

### 2.2 W21-02｜C2 `Provider_side_cancellation_with_a_live_host_token_is_named_timeout_not_failure`

| 字段 | 读数 |
|---|---|
| ①文件:行 | `NarrativeRoundRunner.cs:333`（diff 头 `@@ -333 +333 @@`） |
| ②改前原文 | `            return NarratorOutcome.Timeout($"provider 在 {timeout.TotalMilliseconds} ms 的超时窗口内响应了取消。");` |
| ③改后原文 | `            return NarratorOutcome.Failed($"provider 在 {timeout.TotalMilliseconds} ms 的超时窗口内响应了取消。");` |
| ④`git diff --stat` | `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤跑法整行 | 同 2.1，`--results-directory` 换 `…\runs\run-w21-02` |
| ⑥TRX | `run-w21-02/…_17_56_55.trx`（份数 1） |
| ⑦红格 | `Pry.Core.Tests.NarratorPrototypeTests.Provider_side_cancellation_with_a_live_host_token_is_named_timeout_not_failure`／`Expected: TimedOut  Actual: ProviderFailed`／红点 `:line 864`。`56/56/55/1/notExecuted=0` ⇒ **单枚清洁红**；`:549`／`:562` 那两枚 40 ms 时序格同轮仍在全绿的 55 格里（本轮没有"同落多枚＋时序浮动"） |
| ⑧还原命令 | `git checkout -- src/Pry.Core/Narrative/NarrativeRoundRunner.cs` |
| ⑨还原后 status | 0 |
| ⑩SHA-256 | `92fbce48…` → 突变 `1ac438b961c652f84e9edc674b64e1d89d319b9d6cd2c2026677f07d9b26e19d` → 还原 `92fbce48…`（equal=True） |
| ⑪当轮预检 | `VERDICT=BUILD_OK`；同 Foreground 形状；`gpu_util=2% used=8558MB`；`free_memory_gb=11.8` |

### 2.3 W21-03｜C3 `Unified_budget_inequality_is_pinned_at_the_edge_and_one_token_past_it`

| 字段 | 读数 |
|---|---|
| ①文件:行 | `NarrativeContextCompiler.cs:444`（diff 头 `@@ -444 +444 @@ public sealed record NarrativeContextAssembly`） |
| ②改前原文 | `        (long)InputEstimateTokens + OutputReservationTokens + SafetyTokens <= ContextTokens;` |
| ③改后原文 | `        (long)InputEstimateTokens + OutputReservationTokens + SafetyTokens < ContextTokens;` |
| ④`git diff --stat` | `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤跑法整行 | 同 2.1，目录 `…\runs\run-w21-03` |
| ⑥TRX | `run-w21-03/…_17_57_26.trx`（份数 1） |
| ⑦红格 | `…NarratorPrototypeTests.Unified_budget_inequality_is_pinned_at_the_edge_and_one_token_past_it`／`Assert.True() Failure  Expected: True  Actual: False`／红点 `:line 902`。`56/56/55/1` ⇒ **单枚清洁红**；§一.3 担心的 `:343`（`Compiler_…exact_arithmetic` 那格 `Assert.True(WithinUnifiedBudget)`）同轮仍绿 ⇒ 装配器产出的体不落等号位，读码推断被实测证实（不是拿推演当红） |
| ⑧还原命令 | `git checkout -- src/Pry.Core/Narrative/NarrativeContextCompiler.cs` |
| ⑨还原后 status | 0 |
| ⑩SHA-256 | `86c30160…1d9842` → 突变 `d2389c119229961d166887f6242e03c2fd9f4e92361ca085c8f673fffb4e3afe`（＝第十八波 m18-12 自报值逐位相同）→ 还原 `86c30160…`（equal=True） |
| ⑪当轮预检 | `VERDICT=BUILD_OK`；`gpu_util=2% used=8558MB`；`free_memory_gb=11.9` |

### 2.4 W21-04｜C4 `Whole_section_segments_carry_their_own_source_reference_even_when_dropped`

| 字段 | 读数 |
|---|---|
| ①文件:行 | `NarrativeContextCompiler.cs:294`（`Simple` 的 `SourceRefs` 唯一产地，diff 头 `@@ -294 +294 @@`） |
| ②改前原文 | `            SourceRefs = [contentSourceRef], EmittedText = emittedText,` |
| ③改后原文 | `            SourceRefs = [], EmittedText = emittedText,` |
| ④`git diff --stat` | `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤跑法整行 | 同 2.1，目录 `…\runs\run-w21-04` |
| ⑥TRX | `run-w21-04/…_17_57_48.trx`（份数 1） |
| ⑦红格 | `…NarratorPrototypeTests.Whole_section_segments_carry_their_own_source_reference_even_when_dropped`／`Assert.Equal() Failure: Collections differ ↓(pos 0) Expected: ["scene:current"] Actual: []`／红点 `:line 920`。`56/56/55/1` ⇒ **单枚清洁红**；`Compiler_carries_source_references_into_every_emitted_section`（`:372-375`）同轮仍绿，证实 §一.3 的"那格不经 `Simple`"读码 |
| ⑧还原命令 | `git checkout -- src/Pry.Core/Narrative/NarrativeContextCompiler.cs` |
| ⑨还原后 status | 0 |
| ⑩SHA-256 | `86c30160…` → 突变 `d6c2dcb0534bfd15c1cae157e84cdbd5467c6f350ab1fd1bc7035a225b5784d2`（＝第十八波 m18-13 值）→ 还原 `86c30160…`（equal=True） |
| ⑪当轮预检 | `VERDICT=BUILD_OK`；`gpu_util=2% used=8558MB`；`free_memory_gb=11.8` |

### 2.5 W21-05｜C5 `Absent_sections_are_named_not_supplied_rather_than_budget`

| 字段 | 读数 |
|---|---|
| ①文件:行 | `NarrativeContextCompiler.cs:285`（`NotSupplied` 工厂的理由串，diff 头 `@@ -285 +285 @@`） |
| ②改前原文 | `            DispositionReason = "not_supplied",` |
| ③改后原文 | `            DispositionReason = "dropped",` |
| ④`git diff --stat` | `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤跑法整行 | 同 2.1，目录 `…\runs\run-w21-05` |
| ⑥TRX | `run-w21-05/…_17_58_04.trx`（份数 1） |
| ⑦红格 | `…NarratorPrototypeTests.Absent_sections_are_named_not_supplied_rather_than_budget`／`Collections differ at index 0 ↓(pos 0) Expected: "not_supplied" Actual: "dropped" ↑(pos 0)`／红点 `:line 942`。`56/56/55/1` ⇒ **单枚清洁红**；`Compiler_keeps_absent_sections_as_not_supplied_instead_of_dropping_them`（`:198-207`，只点 `Disposition` 不点理由串）同轮仍绿 |
| ⑧还原命令 | `git checkout -- src/Pry.Core/Narrative/NarrativeContextCompiler.cs` |
| ⑨还原后 status | 0 |
| ⑩SHA-256 | `86c30160…` → 突变 `7e92d4d9f5b366cd8dc14b2cf3731f49894ac153ddeb8dfd10209a890d9dfcbf`（＝第十八波 m18-16 值）→ 还原 `86c30160…`（equal=True） |
| ⑪当轮预检 | `VERDICT=BUILD_OK`；`gpu_util=2% used=8558MB`；`free_memory_gb=11.8` |

### 2.6 W21-06｜C6 `Unit_run_losing_units_to_both_reasons_names_both_together`

| 字段 | 读数 |
|---|---|
| ①文件:行 | `NarrativeContextCompiler.cs:265`（混合臂理由串，diff 头 `@@ -265 +265 @@`） |
| ②改前原文 | `                : crossScope == 0 && budgetDropped > 0 ? "unit_budget" : "cross_scope_and_unit_budget")` |
| ③改后原文 | `                : crossScope == 0 && budgetDropped > 0 ? "unit_budget" : "cross_and_unit")` |
| ④`git diff --stat` | `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤跑法整行 | 同 2.1，目录 `…\runs\run-w21-06` |
| ⑥TRX | `run-w21-06/…_17_58_20.trx`（份数 1） |
| ⑦红格 | `…NarratorPrototypeTests.Unit_run_losing_units_to_both_reasons_names_both_together`／`Strings differ ↓(pos 6) Expected: "cross_scope_and_unit_budget" Actual: "cross_and_unit"`／红点 `:line 961`。`56/56/55/1` ⇒ **单枚清洁红**（同轮 `:264` 的 `cross_scope` 臂与 `:265` 左值 `unit_budget` 两族格全绿，说明这一枚只落在混合臂） |
| ⑧还原命令 | `git checkout -- src/Pry.Core/Narrative/NarrativeContextCompiler.cs` |
| ⑨还原后 status | 0 |
| ⑩SHA-256 | `86c30160…` → 突变 `b68938aed40cf5708caa949c86adb43946dd938377bd722dce55674734b7ed94`（＝第十八波 m18-15 值）→ 还原 `86c30160…`（equal=True） |
| ⑪当轮预检 | `VERDICT=BUILD_OK`；`gpu_util=2% used=8554MB`；`free_memory_gb=11.8` |

### 2.7 W21-07｜C7 `Unit_run_that_fits_no_unit_at_all_is_dropped_and_named_budget`

| 字段 | 读数 |
|---|---|
| ①文件:行 | `NarrativeContextCompiler.cs:263`（`included == 0` 支的理由串，diff 头 `@@ -263 +263 @@`） |
| ②改前原文 | `        var reason = included == 0 ? "budget"` |
| ③改后原文 | `        var reason = included == 0 ? ""` |
| ④`git diff --stat` | `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤跑法整行 | 同 2.1，目录 `…\runs\run-w21-07` |
| ⑥TRX | `run-w21-07/…_17_58_36.trx`（份数 1） |
| ⑦红格 | `…NarratorPrototypeTests.Unit_run_that_fits_no_unit_at_all_is_dropped_and_named_budget`／`Strings differ ↓(pos 0) Expected: "budget" Actual: ""`／红点 `:line 982`。`56/56/55/1` ⇒ **单枚清洁红**；另两处 `"budget"` 点名（`:218` 与 `:924`，走 `:206` 的 `AddSection` 产地）同轮仍绿，撞名风险排除（§一.3） |
| ⑧还原命令 | `git checkout -- src/Pry.Core/Narrative/NarrativeContextCompiler.cs` |
| ⑨还原后 status | 0 |
| ⑩SHA-256 | `86c30160…` → 突变 `82546639c92c9f385af7df2a3b6ee9c93092dece84e46a3b0e36ec9674e07cac`（＝第十八波 m18-17 值）→ 还原 `86c30160…`（equal=True） |
| ⑪当轮预检 | `VERDICT=BUILD_OK`；`gpu_util=2% used=8561MB`；`free_memory_gb=11.7` |

### 2.8 落门侧 7 枚小结（截至此批）

C1～C7 七枚**全部拿到单枚、单轮、清洁红**：七轮 Counters 一律 `total=56 executed=56 passed=55 failed=1 notExecuted=0`，红格逐轮点名唯一，红点行号 838／864／902／920／942／961／982 与 §一.2 预测逐位相同；七轮还原后 `git status --short` 行数全 0、SHA 全对平。定向预算已用 7／12，剩 5（3 枚放宽侧＋2 备用）。

### 2.9 W21-08｜C8 `Candidate_at_a_closed_interval_confidence_edge_is_released(confidence: 1)`

| 字段 | 读数 |
|---|---|
| ①文件:行 | `src/Pry.Core/Narrative/NarrativeDraftValidator.cs:318`（`KnowledgeCandidatePolicy`，diff 头 `@@ -318 +318 @@`） |
| ②改前原文 | `        if (candidate.Confidence is < 0 or > 1)` |
| ③改后原文 | `        if (candidate.Confidence is < 0 or >= 1)`（把闭区间上界误写成开区间＝收紧侧突变） |
| ④`git diff --stat` | `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤跑法整行 | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~NarrativeIdentityContractTests" --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave21-1023\runs\run-w21-08" --logger trx` |
| ⑥TRX | `run-w21-08/24782_PHYTHLINK_2026-10-23_17_59_52.trx`（份数 1） |
| ⑦红格 | `Pry.Core.Tests.NarrativeIdentityContractTests.Candidate_at_a_closed_interval_confidence_edge_is_released(confidence: 1)`／`Assert.Empty() Failure: Collection was not empty  Collection: [ContractRejection { Reason = RealityContamination, Detail = 置信度必须落在 [0,1]，当前是 1。 }]`／红点 `NarrativeIdentityContractTests.cs:line 568`。`<Counters total="65" executed="65" passed="64" failed="1" notExecuted="0">` ⇒ **单枚清洁红**；同族行 `(confidence: 0)` 与 `Confidence_outside_the_unit_interval_is_refused(1.5／-0.1)` 两行同轮全绿（`>= 1` 后 `1.5` 仍被拒，故拒绝侧不红，见 §一.3） |
| ⑧还原命令 | `git checkout -- src/Pry.Core/Narrative/NarrativeDraftValidator.cs` |
| ⑨还原后 status | 0 |
| ⑩SHA-256 | 基线/还原 `88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c` → 突变 `02a7c062a6e29e951ee7cdc1fb18e948e8f49b77d42b316885972baf3ba3de81` → 还原 `88ebf5c3…`（equal=True） |
| ⑪当轮预检 | `VERDICT=BUILD_OK`；`gpu_util=2% used=8554MB`；`free_memory_gb=11.9` |

> 更正我自己 §一.2 的预测：那三枚放宽侧格的红点实测在 `:568`／`:587`（`…Policy.Validate(...)` 那一行），不是我预测的 `Assert.Empty` 行 `:569`／`:588`（xUnit 的 source-linked 行号取的是该断言所属语句行）。读数只贴不改预测，这条差额如实登记。

### 2.10 W21-09｜C9 `Candidate_at_a_closed_interval_confidence_edge_is_released(confidence: 0)`

| 字段 | 读数 |
|---|---|
| ①文件:行 | `NarrativeDraftValidator.cs:318`（同一行，**另一枚配方**：只动下界） |
| ②改前原文 | `        if (candidate.Confidence is < 0 or > 1)` |
| ③改后原文 | `        if (candidate.Confidence is <= 0 or > 1)` |
| ④`git diff --stat` | `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤跑法整行 | 同 2.9，目录 `…\runs\run-w21-09` |
| ⑥TRX | `run-w21-09/…_18_00_11.trx`（份数 1） |
| ⑦红格 | `…Candidate_at_a_closed_interval_confidence_edge_is_released(confidence: 0)`／`Collection: [ContractRejection { Reason = RealityContamination, Detail = 置信度必须落在 [0,1]，当前是 0。 }]`／红点 `:line 568`。`65/65/64/1/notExecuted=0` ⇒ **单枚清洁红**。同族行 `(confidence: 1)` 本轮保持绿 ⇒ 两枚 InlineData 行在"单枚"意义上是可分的，C8／C9 各算一枚凭证，不算共享落点 |
| ⑧还原命令 | `git checkout -- src/Pry.Core/Narrative/NarrativeDraftValidator.cs` |
| ⑨还原后 status | 0 |
| ⑩SHA-256 | `88ebf5c3…` → 突变 `388e5d7ec10a65a74ff1a62c7fd4fa4ea762db9d7c5f8094b671382d41d88184` → 还原 `88ebf5c3…`（equal=True） |
| ⑪当轮预检 | `VERDICT=BUILD_OK`；`gpu_util=2% used=8553MB`；`free_memory_gb=11.8` |

### 2.11 W21-10｜C10 `Intent_whose_deadline_equals_its_earliest_is_released`

| 字段 | 读数 |
|---|---|
| ①文件:行 | `NarrativeDraftValidator.cs:281`（`NarrativeIntentPolicy`，diff 头 `@@ -281 +281 @@`） |
| ②改前原文 | `        if (intent.EarliestDueAtUtc is { } earliest && intent.LatestDueAtUtc is { } latest && latest < earliest)` |
| ③改后原文 | 同行末段 `latest < earliest` → `latest <= earliest` |
| ④`git diff --stat` | `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤跑法整行 | 同 2.9，目录 `…\runs\run-w21-10` |
| ⑥TRX | `run-w21-10/…_18_00_28.trx`（份数 1） |
| ⑦红格 | `Pry.Core.Tests.NarrativeIdentityContractTests.Intent_whose_deadline_equals_its_earliest_is_released`／`Collection: [ContractRejection { Reason = IntentClaimedAsFact, Detail = 意图 intent-1 的最晚时间早于最早时间。 }]`／红点 `:line 587`。`65/65/64/1` ⇒ **单枚清洁红**；`Intent_whose_deadline_precedes_its_earliest_is_refused`（`:458`，取"早于"侧）同轮仍绿，证实 §一.3 读码 |
| ⑧还原命令 | `git checkout -- src/Pry.Core/Narrative/NarrativeDraftValidator.cs` |
| ⑨还原后 status | 0 |
| ⑩SHA-256 | `88ebf5c3…` → 突变 `d154c15703136701e53086e12dd7d26a7717ab3b372d7d92fb1cfa4146f4ab0b` → 还原 `88ebf5c3…`（equal=True） |
| ⑪当轮预检 | `VERDICT=BUILD_OK`；`gpu_util=2% used=8554MB`；`free_memory_gb=11.8` |

### 2.12 活单①总账

**10 枚格＝10／10 全部拿到"单枚、单轮、清洁红"**，零"未取到红"、零同落多枚、零挂死轮：

| 枚 | 轮 | 红点 | Counters | 同轮其余格 |
|---|---|---|---|---|
| C1 | W21-01 | `NarratorPrototypeTests.cs:838` | 56/56/55/1/NE0 | 55 全绿 |
| C2 | W21-02 | `:864` | 56/56/55/1/NE0 | 55 全绿 |
| C3 | W21-03 | `:902` | 56/56/55/1/NE0 | 55 全绿 |
| C4 | W21-04 | `:920` | 56/56/55/1/NE0 | 55 全绿 |
| C5 | W21-05 | `:942` | 56/56/55/1/NE0 | 55 全绿 |
| C6 | W21-06 | `:961` | 56/56/55/1/NE0 | 55 全绿 |
| C7 | W21-07 | `:982` | 56/56/55/1/NE0 | 55 全绿 |
| C8 | W21-08 | `NarrativeIdentityContractTests.cs:568` | 65/65/64/1/NE0 | 64 全绿 |
| C9 | W21-09 | `:568`（同族另一行） | 65/65/64/1/NE0 | 64 全绿（含 C8 那行） |
| C10 | W21-10 | `:587` | 65/65/64/1/NE0 | 64 全绿 |

定向预算 10／12 用掉，**剩余 2 轮备用未用**（没有格需要补跑，不为了用满而加轮）。

---

## 三 活单②：第十八波那 23 轮的归属分账（临时格／已落盘判据）

### 3.0 取证方法（不引用第十八波的推演当实测）

三路盘上证据，全部当场从 `.local-notes/qa-wave18-1023/` 只读取得（未改一字节、未跑任何 `dotnet` 在它上面）：

1. **24 份 TRX 逐份读 `<Counters>` 与 `outcome="Failed"` 的 `testName`**（`run-clean`＋`run-m18-01`～`run-m18-23`，份数＝24，逐目录 1 份）；
2. **第十八波载体的提交时刻 vs 每轮 TRX 的时刻**：`git -C carrier log -1 --format=… 147aa19` = author `2026-10-23 16:50:47 +0800`／committer `2026-10-23 16:50:47 +0800`，`git reflog --date=iso` 只有两条：`checkout def0faf→codex/wave18-20261023`（16:15:35）、`commit 147aa19`（16:50:47），**无 amend、无第二把**；24 份 TRX 的时间戳＝`16:51:18`（run-clean）～`17:09:16`（m18-23），**全部晚于那枚提交**。⇒ 每一轮跑的时候，那 10 枚格已经在那枚提交里落盘；
3. **判据字节对平**：第十八波载体 `git status --short`＝0 行且 HEAD＝`147aa19`，其两份测试 blob＝`NarratorPrototypeTests.cs:de196e28a4e8eb6c99cfc857a56d6db8c20bc6d3`／`NarrativeIdentityContractTests.cs:3e27e34cadd7ed3933c469ac93717fe631bac717`；我这份载体（`e8a842a`＋cherry-pick）`HEAD:` 同一位置的 blob **逐位相同** ⇒ 两波压红压在同一份判据字节上，我复采到的红点行号（838／864／902／920／942／961／982）与它报告 §二 写的行号逐位相同不是巧合而是同一字节。
4. 反向核验（当场 `git grep -c`）：那 10 枚新格名在 `147aa19^`＝**0 命中**、在 `147aa19`＝1 命中（＝这 10 枚确由该提交引入）；其余 13 轮的红格名（`Silence_round_…`／`Draft_on_a_stale_…`／`Draft_from_a_fenced_…`／`Draft_with_a_wrong_…`／`Contiguous_split_…`／`Host_cancellation_…`／`Call_refuses_an_assembly_…`／`Compiler_removes_cross_conversation_…`／`Knowledge_candidate_origin_…` 共 9 个方法名，展开成 13 轮含 `[Theory]` 行）**在 `147aa19^` 就已存在**（＝已落盘的既有判据）。

### 3.1 分账结论（先行）

| 类别 | 轮数 | 明细 |
|---|---|---|
| 红在**本波那 10 枚已落盘新格**上（＝这 10 枚格的归位凭证候选） | **10** | m18-01（C1）、m18-08（C2）、m18-12（C3）、m18-13（C4）、m18-15（C6）、m18-16（C5）、m18-17（C7）、m18-18（C8）、m18-19（C9）、m18-20（C10） |
| 红在**更早已落盘的既有判据**上（＝那条判定线的证据，不记到这 10 枚格名下） | **13** | m18-02～07、m18-09～11、m18-14、m18-21～23 |
| **临时格**（当轮先写入判据→跑红→又删掉，判据本体不在任何提交里） | **0** | 无。提交时刻（16:50:47）早于全部 24 份 TRX（16:51:18～17:09:16），且 reflog 无 amend ⇒ 不存在"跑完才落盘"或"跑完又删掉"的形状 |
| **归属不清** | **0**（读码归因）／**6**（报告笔迹） | 分两笔写清：① 23 轮的红格归属全部能从盘上 TRX 点名 ⇒ 归属本身 0 格不清；② 但 m18-18～m18-23 这 **6 轮在第十八波报告里没有任何读数**（报告写到 §三.0 的预测表就断，§三 没有十一项字段、没有 TRX 文件名、没有还原哈希、没有 `<Counters>`）⇒ 那 6 轮"报告笔迹缺失"是事实，盘上 TRX 补齐了它们，缺失部分由本刀 §三.2 逐轮代记 |

两条必须并排放的口径（工单 §四）：**临时格＝0 枚 ⇒ 这 10 枚格不存在"只是临时态"的嫌疑**；同时 m18-02～07／09～11／14／21～23 那 13 轮**不是白跑**——它们是 `:169-170`／`:238/:244/:250`／`:288`／`:327`／`NarratorContracts.cs:43/50/57`／`:264`／`NarrativeDraftValidator.cs:327/:351/:367` 这些判定线各自的单枚反证，只是**记账对象是既有格，不是这 10 枚新格**。

一处编号说明（本刀自己的表序 vs 第十八波的轮序，不是谁的错）：本报告 §一.2 按"src 锚点行号"给十枚格编号，于是 C5＝`:285 not_supplied`、C6＝`:265 cross_scope_and_unit_budget`；第十八波按跑轮先后记 m18-15＝`:265`、m18-16＝`:285`。两套编号在盘上逐轮对得上（m18-15 的红格全名＝`Unit_run_losing_units_to_both_reasons_names_both_together`＝C6；m18-16 的红格全名＝`Absent_sections_are_named_not_supplied_rather_than_budget`＝C5），**没有命名冲突需要更正**，只是引用时要认格名不认编号。

### 3.2 m18-18～m18-23 六轮的代记读数（第十八波报告缺的部分）

| 轮 | TRX（份数 1） | Counters | 盘上点名的红格 | 判据本体 | 红点行 |
|---|---|---|---|---|---|
| m18-18 | `…_17_08_49.trx` | 65/65/64/1/NE0 | `Candidate_at_a_closed_interval_confidence_edge_is_released(confidence: 1)` | `147aa19` 内（新格 C8） | 报告未记；本刀同配方复采实测 `:568` |
| m18-19 | `…_17_08_54.trx` | 65/65/64/1/NE0 | 同族 `(confidence: 0)` | 新格 C9 | 同上 `:568` |
| m18-20 | `…_17_09_00.trx` | 65/65/64/1/NE0 | `Intent_whose_deadline_equals_its_earliest_is_released` | 新格 C10 | 本刀复采 `:587` |
| m18-21 | `…_17_09_05.trx` | 65/65/64/1/NE0 | `Knowledge_candidate_origin_decides_what_it_may_assert(kind: UserReportedFact, origin: ExplicitFeedback, expected: None)` | **既有矩阵行**（`147aa19^` 已存在） | 报告 §三.0 预测的那枚矩阵行 |
| m18-22 | `…_17_09_10.trx` | 65/65/64/1/NE0 | `…(kind: Proposal, origin: NarrativeProse, expected: None)` | 既有矩阵行 | 同上 |
| m18-23 | `…_17_09_16.trx` | 65/65/64/1/NE0 | `…(kind: NarrativeProse, origin: ModelSelfReport, expected: None)` | 既有矩阵行 | 同上 |

三枚矩阵行（m18-21～23）的红格是既有 `[Theory]` 的三个 `expected: None` 行，命中工单 §四问 2 的另一半：它们的突变对象仍是 `NarrativeDraftValidator.cs` 的产品码行（`:327`／`:351`／`:367`，报告 §三.0 表），不是判据文件自身；但它们**不给这 10 枚格记功**，给的是"放行侧矩阵行有没有人守"那条线。

### 3.3 十一项字段的齐缺（对第十八波那 23 轮，如实登记）

- m18-01～m18-17（报告 §二）：①～⑪ 逐轮齐（⑥TRX 名、⑦红格全名＋消息＋行号、⑨status 行数＝0、⑩双哈希对平逐轮给了；⑪预检 §2.0 给了"全程 `VERDICT=BUILD_OK`"与 m18-09～17 那九轮的 `free_memory_gb` 逐轮值，m18-01～08 只给了"全程 BUILD_OK"这一句、**没有逐轮贴整段输出**）⇒ 这两批的字段完整度不等同，本刀 §二 按"逐轮整段贴"补齐了自己的 10 轮。
- m18-18～m18-23（报告 §三）：**十一项字段一项都没有落进报告**（只有跑前的预测表 §三.0 与一句额度对账）⇒ 缺凭证的严重性不等价于"这 6 轮没跑"：盘上 6 份 TRX 各 `failed="1"`、红格逐一点名（§3.2），但报告笔迹与哈希对平证据缺失，第十八波自身的 §二.0"每轮预检整段贴"这一条在这 6 轮没有兑现。

### 3.4 逐轮三问表（m18-01～m18-23，每轮一行）

问 1＝那一轮压红的格，判据本体在 `147aa19` 里（已落盘新格）／在 `147aa19^` 就有（已落盘既有格）／只在当轮工作树里写过又删（临时格）；问 2＝突变对象是 `src/` 产品码行还是判据文件自身；问 3＝那一轮之后报告有没有给"载体 `git status --short` 归 0"与还原 SHA（本刀另在盘上核到第十八波载体此刻三枚 `src` 文件仍是 pristine 哈希，见 §3.0 第 2 条末）。

| 轮 | 红格（盘上 TRX `testName` 点名） | 问 1 | 问 2（突变对象，`src/` 行） | 问 3 |
|---|---|---|---|---|
| m18-01 | `Mixed_round_takes_the_immediate_branch_and_registers_no_intent_there`（C1） | **已落盘新格**（`147aa19` 内） | `NarrativeRoundRunner.cs:150` 产品码 | 有（status 0＋双哈希） |
| m18-02 | `Silence_round_produces_no_assistant_message_and_no_intent` | 已落盘既有格 | `NarrativeRoundRunner.cs:169-170` 产品码 | 有 |
| m18-03 | `Draft_on_a_stale_story_version_is_refused_before_any_history` | 已落盘既有格 | `:238` 产品码 | 有 |
| m18-04 | `Draft_from_a_fenced_writer_epoch_is_refused` | 已落盘既有格 | `:244` 产品码 | 有 |
| m18-05 | `Draft_with_a_wrong_issue_baseline_is_refused_as_late` | 已落盘既有格 | `:250` 产品码 | 有 |
| m18-06 | `Contiguous_split_reproduces_the_bound_slice_verbatim` | 已落盘既有格 | `:288` 产品码（整行删除） | 有 |
| m18-07 | `Host_cancellation_stays_cancellation_and_is_not_converted_into_a_round` | 已落盘既有格 | `:327` 产品码 | 有 |
| m18-08 | `Provider_side_cancellation_with_a_live_host_token_is_named_timeout_not_failure`（C2） | **已落盘新格** | `:333` 产品码 | 有 |
| m18-09 | `Call_refuses_an_assembly_that_belongs_to_another_scope(scope: "instance")` | 已落盘既有格（`[Theory]` 行） | `NarratorContracts.cs:43` 产品码 | 有 |
| m18-10 | 同族 `(scope: "conversation")` | 已落盘既有格 | `NarratorContracts.cs:50` 产品码 | 有 |
| m18-11 | 同族 `(scope: "batch")` | 已落盘既有格 | `NarratorContracts.cs:57` 产品码 | 有 |
| m18-12 | `Unified_budget_inequality_is_pinned_at_the_edge_and_one_token_past_it`（C3） | **已落盘新格** | `NarrativeContextCompiler.cs:444` 产品码 | 有 |
| m18-13 | `Whole_section_segments_carry_their_own_source_reference_even_when_dropped`（C4） | **已落盘新格** | `NarrativeContextCompiler.cs:294` 产品码 | 有 |
| m18-14 | `Compiler_removes_cross_conversation_units_and_names_the_count` | 已落盘既有格 | `NarrativeContextCompiler.cs:264` 产品码 | 有 |
| m18-15 | `Unit_run_losing_units_to_both_reasons_names_both_together`（C6） | **已落盘新格** | `NarrativeContextCompiler.cs:265` 产品码 | 有 |
| m18-16 | `Absent_sections_are_named_not_supplied_rather_than_budget`（C5） | **已落盘新格** | `NarrativeContextCompiler.cs:285` 产品码 | 有 |
| m18-17 | `Unit_run_that_fits_no_unit_at_all_is_dropped_and_named_budget`（C7） | **已落盘新格** | `NarrativeContextCompiler.cs:263` 产品码 | 有 |
| m18-18 | `Candidate_at_a_closed_interval_confidence_edge_is_released(confidence: 1)`（C8） | **已落盘新格** | `NarrativeDraftValidator.cs:318` 产品码（上界） | **无**（报告 §三 一项都没给） |
| m18-19 | 同族 `(confidence: 0)`（C9） | **已落盘新格** | `NarrativeDraftValidator.cs:318` 产品码（下界，另一枚配方） | **无** |
| m18-20 | `Intent_whose_deadline_equals_its_earliest_is_released`（C10） | **已落盘新格** | `NarrativeDraftValidator.cs:281` 产品码 | **无** |
| m18-21 | `Knowledge_candidate_origin_decides_what_it_may_assert(kind: UserReportedFact, origin: ExplicitFeedback, expected: None)` | 已落盘既有格（矩阵行） | `NarrativeDraftValidator.cs:327` 产品码 | **无** |
| m18-22 | `…(kind: Proposal, origin: NarrativeProse, expected: None)` | 已落盘既有格（矩阵行） | `NarrativeDraftValidator.cs:351` 产品码 | **无** |
| m18-23 | `…(kind: NarrativeProse, origin: ModelSelfReport, expected: None)` | 已落盘既有格（矩阵行） | `NarrativeDraftValidator.cs:367` 产品码 | **无** |

三问的合计读数：**问 1＝临时格 0 轮**（10 轮打在 `147aa19` 的十枚新格上、13 轮打在 `147aa19^` 已存在的既有格上）；**问 2＝23 轮全部打在 `src/` 产品码行上，零轮打在判据文件自身**（这十枚格之所以能"单枚清洁红"，正因为压的是判定线本体而不是测试自己的字节）；**问 3＝17 轮给了、6 轮没给**，缺的是 m18-18～23 那六轮的 status 归 0 与还原哈希（工单 §二.1 要求逐轮登记，这六轮在报告笔迹上没兑现；盘上载体此刻三枚 `src` 仍是 pristine 哈希，说明最终态还原了，但不能倒推每一轮都还原了）。


---

## 四 活单③：出口读数（补齐第十八波欠的那一节）

### 四.1 构建（解决方案级，1 次，跑在全部突变还原之后的字节上）

预检（该轮整段，`free_memory_gb=10.8`）通过后跑：

```
dotnet build Pry.slnx -c Release
→ 已成功生成。
    0 个警告
    0 个错误
已用时间 00:00:06.13
```

原样存盘＝`.local-notes/qa-wave21-1023/out-build/build1.txt`。构建前的 `git status --short | wc -l`＝**0**（第十轮已还原），所以这 0/0 读的是 `e8a842a`＋`147aa19` 的干净线，不是任何突变态。

### 四.2 全量（解决方案级，1 次，`--no-build`）

```
dotnet test Pry.slnx -c Release --no-build --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave21-1023\runs\run-final-full" --logger trx
```

跑完先数 `*.trx`＝**4 份**（自动名带 `[1]` 后缀分开，第十七波那枚"同名 `final-full.trx` 互相覆盖"的坑本刀没有踩；未指定 `LogFileName`）。四份 `<Counters>` 逐份原样：

| TRX | `<Counters>`（关键四项＋notExecuted） |
|---|---|
| `24782_PHYTHLINK_2026-10-23_18_03_36.trx`（Client） | `total="108" executed="108" passed="108" failed="0" … notExecuted="0"` |
| `…_18_03_37.trx`（App） | `total="233" executed="233" passed="233" failed="0" … notExecuted="0"` |
| `…_18_03_37[1].trx`（Api） | `total="280" executed="280" passed="280" failed="0" … notExecuted="0"` |
| `…_18_03_36[1].trx`（Core） | `total="664" executed="662" passed="662" failed="0" … notExecuted="0"` |

两条跳过按 `outcome="NotExecuted"` 点名（`notExecuted` 计数器恒 0，不看）：

1. `Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`
2. `Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`

（与守门人 §9 17:5x 在册那两条同名，形状未变。）

### 四.3 对账（当场从盘上取，不做"差不多"）

- 本刀全量＝**总 1285／红 0／跳过 2（executed 1283）**；分项 Client 108／App 233／Api 280／Core **664**（executed 662）。
- 线尾在册＝1275＝Client 108＋App 233＋Api 280＋Core 654（executed 652）。
- 等式：1275 ＋ 10（`147aa19` 那十枚格：Core 654→**664**）＝ **1285**，与盘上四份 TRX 相加逐位相符（108＋233＋280＋664＝1285）；executed 652＋10＝662 相符；差额**没有残留在 Client／App／Api 任何一份**（三份逐一与在册同值：108／233＋0／280＋0）。⇒ 工单 §五.3 预测的"Core 应为 664、总 1285"成立，零对不上项。
- 交叉核：`NarratorPrototypeTests` 56 格＋`NarrativeIdentityContractTests` 65 格＝121，与两枚定向过滤器读到的 `total=56`／`total=65` 一致；§二 十轮的 `total` 与 §五 全量 Core 的 `total` 差 543 格属其他 Core 测试类，无重叠计数问题。

### 四.4 `scripts/measure-fixed-eval.ps1`（带 `-ResultsDirectory` 才会跑）

```
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/measure-fixed-eval.ps1 -ResultsDirectory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave21-1023\runs\run-final-full"
→ Catalog valid: 76 permanent scenarios. Mappings are partial evidence only.
  Passed=1283; Failed=0; Other=2. Evidence: …\run-final-full\fixed-eval-coverage.json
EXITCODE=0
```

catalog＝**76**（与在册同值，本刀没动场景）；`Passed=1283; Failed=0; Other=2` 与 §四.2 四份 TRX 相加一致。该脚本会往该目录写 `fixed-eval-coverage.json`（只写在我自己这一轮目录里）。

### 四.5 其余出口项

| 项 | 读数 |
|---|---|
| `git diff --check` | 载体内**无输出**＝干净 |
| 收尾 `git status --short \| wc -l` | **0** |
| 收尾 `git rev-parse HEAD` | `1998790d3edff99d698f39d76b08375329301251`（＝起刀后那一号，未再动） |
| `git diff --stat e8a842a..HEAD -- src` | **空**（零产品代码净改动） |
| `git diff HEAD --stat -- src tests` | 空 ⇒ 工作树字节＝HEAD 字节，两份判据文件本刀一字未改 |
| 判据文件字节 SHA-256 | `NarratorPrototypeTests.cs 4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`／`NarrativeIdentityContractTests.cs 4436c95859f1643b3cdf486aba4f7f4d35bb02b79f4a193240396e657777981b`；与第十八波载体磁盘上同两份文件**逐位相同**（blob 亦同：`de196e28…`／`3e27e34c…`） |
| `dotnet` 调用次数（只认盘上产物） | **12**＝定向 10（`runs/run-w21-01`～`run-w21-10`，各 1 份 TRX）＋解决方案级构建 1（`out-build/build1.txt`）＋全量 1（`runs/run-final-full` 4 份 TRX）。盘上 TRX 总数＝10＋4＝**14** 份，与调用次数逐位对得上（10 轮各 1 份、全量 1 次 4 份）。预检脚本调用＝**12 次**（每一次 `dotnet` 前一次），全部 `VERDICT=BUILD_OK`，`free_memory_gb` 逐轮 12.3／11.8／11.9／11.8／11.8／11.8／11.7／11.9／11.8／11.8／10.8／10.4，**全程零 HOLD、零 SQLite `disk I/O`／`out of memory` 形态、零作废轮** |
| 预算 | 构建 1／1、全量 1／1、定向 10／12（备用 2 轮未用，没有格需要补跑） |
| 本报告行数 | 见 §五.3 末尾（落盘后当场 `wc -l`） |

### 四.6 归位侧（不归本 lane，照实写）

载体里唯一的提交＝`1998790`（cherry-pick 自 `147aa19`，两份测试文件 217 insertions／0 deletions，`src` 面空）。本刀**未做任何归位／FF／tag／push／merge／打包**；主树 `git status --short --branch` 的他人两枚 `M ` 与全部 `??` 本刀未读未改未提交，主树里除本报告文件（未 add、未 commit）外本刀零笔迹；主树 `dotnet` 调用＝**0**（全部跑在载体里，主树只跑了预检脚本与只读 `git`／`sha256sum`／`grep`）。第十八波那把的状态照工单 §六.6：它交付了 217 行判据与 23 轮记录（盘上 24 份 TRX 齐、每轮各 1 枚点名红），**缺的是出口读数与归属分账**，本刀 §二／§三／§四 就是把这两格补上；不写"失败"、不写"作废"、不写状态词与结案词。

---

## 五 仍未验证与自查

### 五.1 未验证（照实列，不升格）

1. 真实模型对照：零读数（要作者点头才派）。
2. 生产接线：`src/Pry.Api/**`／`Client`／`App` 一个字节未改；`NarrativeRoundRunner`／`NarrativeContextCompiler`／`NarrativeDraftValidator` 仍无任何生产调用方（本刀只在载体里读过它们）。
3. UI 可见行为：未验证。
4. 生产库对账：未接触 `%LOCALAPPDATA%\PryCompanion`；零新增 SQLite 依赖；不接 API／Client／App。
5. 这 10 枚格在**接线后**的用户可见后果：没接线就没有 ⇒ 一律未验证。
6. 第十三＋十四波那 65 格的独立换人复采：本刀没有做（押在同一批刀里，见守门人 §9 17:5x 覆盖度那条），本刀只换了第十八波这 10 枚格的手。
7. 第十八波 m18-02～07／09～11／14／21～23 那 13 轮的**换人复采**：本刀没有重跑（预算 12 轮定向只够这 10 枚格）；我给它们的只是"盘上 TRX 与字节对平的归属分账"，不是第二只手。

### 五.2 六条禁止捷径自查（工单 §六）

1. 未改判据、未改阈值、未加 `Skip`、未注释别格：`git diff HEAD --stat -- tests`＝空，十轮 diff 每轮 `1 file changed` 且那枚文件永远是 `src/`；在册数字门槛（`.7`／`.5`／`2400`／五档超时／`MinimumUsableTokensPerSecond=12`／`4096`／`DefaultSafetyTokens=128`／`MinimumInputTokens=512`）一字未动。
2. 没把"全绿"当"没跑"的反证：每轮先 `ls *.trx` 数份数＝1（全量＝4）再读 `<Counters>`，红格按 `testName` 点名。
3. 零重试到绿、零固定返回值、零吞异常：十轮一次成型，无第二轮同名目录复用。
4. 未引用第十八波报告的推演当实测红：§二 十轮全是本刀自己跑出来的读数；§三 对它的 23 轮只引用盘上 24 份 TRX 的 `<Counters>`／`testName`／提交与 reflog 时刻。
5. 未做归位／FF／tag／push／merge／打包（见 §四.6）。
6. 没把第十八波写成"失败／作废"（见 §四.6 与 §三.1）。

### 五.3 报告自计数（落盘后当场实测）

```
wc -l 本报告                                   → 535 行（工作树 CRLF `grep -c $'\r'`＝535，与仓库内同族报告一致，core.autocrlf=true 入库归一为 LF）
§二 小节数（grep -c '^### 2\.'）                → 12 ＝ 10 张十一项字段轮表（2.1～2.7、2.9～2.11）＋ 2 个小结（2.8、2.12）
§一.2 靶表数据行（^| C，行 112～121）             → 10（＝十枚格，逐枚给 src 锚点与配方）
§二.12 总账表数据行（^| C，行 327～336）           → 10（同一批十枚格的出口对照）
  注：`grep -c '^| C'` 全文＝20 ＝ 上述两张表各 10 行，不是靶表多写了格
§三.1 分账表数据行（行 357～360）                 → 4（10 枚新格轮／既有格轮／临时格＝0／归属不清）
§三.2 代记表数据行（行 370～375）                 → 6（m18-18～m18-23）
§三.4 逐轮三问表数据行（行 390～412 中 ^| m18-）    → 23（m18-01～m18-23 每轮一行，零缺行）
  注：`grep -c '^| m18-'` 全文＝29 ＝ §三.4 的 23 行 ＋ §三.2 的 6 行
§四.5 出口表数据行（行 479～487）                 → 9（更正本刀初稿误记的 11：按盘上 `^|` 逐行数，表头 477＋分隔行 478 之后的数据行是 9 条）
尾随空白行数（grep -c ' $'）                     → 0
```

—— 完 ——
