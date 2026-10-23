# 第十八波·判据刀报告：把第十三波 9 组"落门没有单枚反证"与"正向 → None 无人守"逐枚钉成会响的格

- 报告 id：`eval-QA-WAVE18-1023`
- 工单：`docs/dispatch-hds-wave18-20261023.md`（id `HDS-WAVE18-1023`，基点 `def0faf`）
- 性质：**纯判据刀**，`src/` 零字节改动（出口判据见 §〇.5）
- 执行方：冷启动 lane（一只独立手）
- 写法：按批次增量落盘。本报告在载体里逐轮补写，撞轮次上限就停在已有证据上。

---

## 〇 入场判据（当场实测，原样贴出）

### 〇.1 时点与主树现状

`date` → `Fri Oct 23 16:30:48 2026`

`git status --short --branch`（主树）：

```
## codex/ui-main-window-redesign
M  docs/dispatch-tip1020-02-memory-group-judgment.md
M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs
?? .qoder/
?? docs/character-learning-safety.patch
?? docs/eval/reports/2026-10-23-hds004-lifecycle-coordinator.md
?? docs/eval/reports/2026-10-23-hds005-sqlite-commit-delivery.md
?? docs/research/HDS_INTERLUDE.md
?? docs/research/HDS_QQ_RESEARCH_INDEX.md
?? docs/research/QQ_BRIDGE.md
```

他人名下项（两枚暂存项＋`docs/research/*`＋`.patch`＋`.qoder/`）本波一律未读、未改、未提交、未 stash、未 clean。主树本波唯一笔迹＝本报告文件（未 add、未 commit）。

### 〇.2 独立载体

`git worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave18-1023/carrier def0faf`

入场两行判据（载体内当场实测）：

```
git status --short | wc -l   → 0                                       （预期 0 ✓）
git rev-parse HEAD           → def0faf5456a5ddcb121f52040ee9e551b07a382 （预期 def0faf 全号 ✓）
```

### 〇.3 起点判据（`src` 面）

载体起点的 `git diff --stat def0faf..HEAD -- src` ＝ 空（起点即基点，HEAD 尚未移动）；本波出闸前再取一次，见 §〇.5。

### 〇.4 工单与盘面差异（就地登记，不涂改工单）

1. 工单 §〇.4 把 `ContextWindowLoadTests.cs` 列在 `tests/Pry.Core.Tests/` 下，盘上实际位置＝**`tests/Pry.Api.Tests/ContextWindowLoadTests.cs`**（`find` 当场实测；`tests/Pry.Core.Tests/` 下没有这枚文件）。W2 那格（工单 §一.3）就在这枚 Api 文件里，本波把它按"同性质判据文件"看待，但**只读不改**（W2 的结论是推演结论，见 §四），因此没有触发任何越界写盘。
2. 工单 §一.2 说"第十二波登记的 13 枚出口"，第十二波报告落盘口径是两处：§六.7「双向性未验证……20 枚突变全在'摘成员／删门'一侧」与待拆清单尾巴「放宽方向（往允许集合加成员）没有实测红，只有矩阵 `None` 行结构性把守」。两份原文都没写"13"这个数。本波按**可读盘的口径**执行：把 `KnowledgeCandidatePolicy`／`NarrativeIntentPolicy` 里"允许集合／区间边界"型出口逐枚枚举（§三.0），凡"收紧侧红过、放行侧无实测红"者补方向对称证据；对不上 13 这个数的差额如实登记在 §六。

### 〇.5 出闸判据（本波自证，最后一轮当场重取）

- `git diff --stat def0faf..HEAD -- src` ＝ 空 ⇒ 见 §七。
- 载体提交只含两枚既有测试文件的追加段（无新增文件、无 `src` 文件）⇒ 见 §七 `git show --stat`。

---

## 一 活单①：第十三波 §三.16 那 9 组落门的逐组判定与反证

### 1.0 本轮机制（先说清，免得读数被误读）

- 每轮＝**一枚单点突变**，改前改后都取 SHA-256，跑完按字节还原；突变轮零 `--no-build`；每轮独立 `--results-directory`；跑完先 `ls *.trx` 数份数再读 `<Counters>`。
- "单枚清洁红"口径＝该轮 `<Counters failed="1">` 且红格全名唯一，且红因指到本轮那一处锚点。
- 拿不到单枚的两种形状本波分开写：**（a）同落多枚**（红有，但不清洁）；**（b）够不着**（结构上放不出红，连布尔返回门与 fallback 一起算）。两者都不写成"已证"，也不写成"不需要拆"。
- 读码判定与实测红分开标注：凡只由读码得出的结论，句末标"（读码，未实测）"。

### 1.1 九组判定总表（结论先行，反证明细在 §二）

| 组 | 锚点 | 判定 | 本波取到的红 |
|---|---|---|---|
| G1 | `NarrativeRoundRunner.cs:150` 即时支分岔 | 可取单枚 | 见 §二 m18-01 |
| G2 | `:167/:169/:170` 沉默支落点 | `:169` 的 Disposition 字面量＝同落多枚；`:170` 的 `Actions` 接线＝可取单枚 | 见 §二 m18-02 |
| G3 | `:238/:244/:250` 快照三项 | 三行各自可取单枚 | 见 §二 m18-03/04/05 |
| G4 | `:286-303` `SplitContiguous`（含 `:297` 余数） | `:288` 下界门可取单枚；`:297`/`:291-292` 余数分配＝同落多枚（捆绑格把整函数四件事钉在一枚 `[Fact]` 里） | 见 §二 m18-06 |
| G5 | `:313-321`／`:331-334`／`:327-330` | `:327-330` 可取单枚；`:331-334` 见实测；`:313-321` 的布尔门 `:315`＝改坏即挂死（不是红），`:320`＝与既有两格共用落点 | 见 §二 m18-07/08 |
| G6 | `NarratorContracts.cs:43/50/57` | 三行各自可取单枚（`[Theory]` 行是独立格） | 见 §二 m18-09/10/11 |
| G7 | `NarrativeContextCompiler.cs:178`＋`:443-444` | `:178`＝防御纵深，装配器自己走不到（算术链见 §一.2，**不写成已证**）；`:443-444` 的 `<=` 边界＝新格＋单枚红 | 见 §二 m18-12 |
| G8 | `NarrativeContextCompiler.cs:99/253/294` | `:99`／`:253`＝同落多枚（读码，未实测）；`:294`＝第十三波在册零代价出口，新格＋单枚红 | 见 §二 m18-13 |
| G9 | `NarrativeContextCompiler.cs:263-266` 四串理由 | `cross_scope` 单枚；`cross_scope_and_unit_budget`／`not_supplied`／`included == 0` 的 `budget` 各建新格后单枚；`unit_budget`＝两格共用（读码，未实测） | 见 §二 m18-14～17 |

### 1.2 G7 的算术链（为什么 `:178` 写成"够不着"而不是"已证"）

`NarrativeContextCompiler.cs`：`total = ContextTokens − OutputTokens − SafetyTokens`（`:61`，K2 门 `:62` 要求 `total > 0`）；必需前奏 `:87` 要求 `preludeCost ≤ total`；`AddSection :203` 与 `AddUnitRun :246` 都只在 `used + cost ≤ total` 时才累加 `used`；Advisory `:110` 装不下即整轮失败。于是任何走到 `:169` 构造装配体的路径都满足 `used ≤ total`，即 `InputEstimateTokens + OutputReservationTokens + SafetyTokens ≤ ContextTokens`＝`WithinUnifiedBudget`（`:443-444`）恒真 ⇒ `:178` 那道自检在现有构造下**放不出红**（改坏它＝零代价，不是红）。本轮不把这句话升格成"已证"，只登记：可达性＝否（读码），并把它本体 `:443` 的边界换成新格＋单枚实测红来还。

---

## 二 反证登记（十一项字段，逐轮）

> 字段口径（工单 §一.4）：载体全号／文件:行／改前→改后（当场 `git diff`）／`diff --stat`／完整跑法／TRX 文件名／红格全名＋消息＋锚点／还原方法／改前改后 SHA-256／还原后 `git status --short` 行数／结论。

### 2.0 构建与干净态（两轮读数的公共前提）

- 载体分支＝`codex/wave18-20261023`，提交全号＝**`147aa191c712aa7d627d74543b044df86249160e`**（基点 `def0faf5456a5ddcb121f52040ee9e551b07a382` 之上唯一一把）。
- `git show --stat`＝`2 files changed, 217 insertions(+)`，只含 `tests/Pry.Core.Tests/NarratorPrototypeTests.cs`（＋167）与 `tests/Pry.Core.Tests/NarrativeIdentityContractTests.cs`（＋50），**删除行数 0**，`src` 面 0 枚文件。
- 构建读数：第一次 `dotnet build Pry.slnx -c Release`＝**3 个错误**（全在我新落的格上：`CS1061` 把 `round.Runner.RunAsync` 写成 `round.RunAsync`；两处 `CS9176` 集合表达式无目标类型），**0 警告**。改形后第二次构建＝`已成功生成。`／**`0 个警告`／`0 个错误`（`out/build2.txt`）。⇒ 工单 §二.1 的"构建 1 次"实际用了 2 次，多出来的一次是本波新格自身的编译错误修复，不是判据读数；如实登记。新造的 46 类告警＝0 条（`xUnit2013`／`xUnit1051` 两类在第二次构建读数里都没出现）。
- 判据条数等式（载体 `grep -c` 当场实测）：

```
NarratorPrototypeTests        ：[Fact] 46→53 ＋ [Theory] 1（[InlineData] 3 行不变）
                                格数＝46＋3 → 53＋3 ＝ 49 → 56（＋7）
NarrativeIdentityContractTests：[Fact] 21→22 ＋ [Theory] 4→5 ＋ [InlineData] 41→43
                                格数＝21＋41 → 22＋43 ＝ 62 → 65（＋3）
NarrativeCommitAndActionGuardTests：[Fact] 16 ＝ 16 格（本波零改动）
NarrativeStorageContractTests     ：[Fact] 30 ＝ 30 格（本波零改动）
```

- 干净态探针（`runs/run-clean`，1 份 TRX `24782_PHYTHLINK_2026-10-23_16_51_18.trx`）＝`失败: 0，通过: 121，总计: 121`（56＋65）⇒ 新增 10 枚格与既有 111 枚格在零突变盘上全绿，后面每轮的点名红才有意义。
- 预检逐轮贴在每轮读数里（全程 `VERDICT=BUILD_OK`；`free_physical_memory` 2.6–3.2 GB 带 WARN、未 HOLD，见 §六）。
- 每轮"完整跑法"都是同一条，只换 `--results-directory`，全部**零 `--no-build`**：

```
dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release ^
  --filter "FullyQualifiedName~NarratorPrototypeTests" ^
  --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave18-1023\runs\run-m18-NN" ^
  --logger trx
```

- 还原方法逐轮＝`git checkout -- <那一枚 src 文件>`（从不 `git checkout -- .`／不 reset／不 clean）；还原后 `git status --short` 行数逐轮实测＝**0**，且 `sha_restored == sha_pristine`。`NarrativeRoundRunner.cs` 的 pristine SHA-256 全程＝`92FBCE48C7948105479A6CA3FE75697DC24201C4F4D120B7ECC086729C353570`（与第十三波收口登记的同名哈希对平）。

### 2.1 m18-01｜G1 `NarrativeRoundRunner.cs:150` 即时支分岔

| 字段 | 读数 |
|---|---|
| 载体全号 | `147aa191c712aa7d627d74543b044df86249160e`，文件 `src/Pry.Core/Narrative/NarrativeRoundRunner.cs` |
| 文件:行 | `:150`（当场 `git diff` 头 `@@ -150 +150 @@`） |
| 改前→改后 | `if (immediate.Length > 0)` → `if (immediate.Length > 0 && deferred.Length == 0)` |
| `diff --stat` | `1 file changed, 1 insertion(+), 1 deletion(-)` |
| TRX | `runs/run-m18-01/24782_PHYTHLINK_2026-10-23_16_55_04.trx`，`ls *.trx`＝1 |
| Counters | `total=56 executed=56 passed=55 failed=1 notExecuted=0` ⇒ **单枚清洁红** |
| 红格全名 | `Pry.Core.Tests.NarratorPrototypeTests.Mixed_round_takes_the_immediate_branch_and_registers_no_intent_there`（本波新格） |
| 红消息＋锚点 | `Assert.Equal() Failure: Values differ Expected: Immediate Actual: Deferred`；`NarratorPrototypeTests.cs:838`（当场 TRX `StackTrace` 行号，`收口复采可用 readtrx18.ps1 直读 run-m18-01`） |
| 还原 | `git checkout -- src/Pry.Core/Narrative/NarrativeRoundRunner.cs`；改前 `92FBCE48…353570` → 改后 `5DE8FF66D16160D6C1C3D7CBDA68E566BA4D9B0184ACBDD99D0823E13C179227` → 还原 `92FBCE48…353570`（equal=True）；status＝0 行 |
| 结论 | `:150` 的"即时先于延迟"次序第一次拿到单枚反证。第十三波 §三.16 那句"拆它会把 D 组 4 格一起放红"的欠账，换成新格独钉＋单枚红还清。同轮其余 55 格全绿，含 D 组 5 格与 E 组——**没有**同落。 |

### 2.2 m18-02｜G2 `:167/:169/:170` 沉默支落点

| 字段 | 读数 |
|---|---|
| 文件:行 | `:169-170`（以 `Disposition = NarrativeRoundDisposition.Silence, …` 开头的两行联合锚，全文件命中点＝1） |
| 改前→改后 | `Draft = boundDraft, Actions = boundActions, NarratorCallCount = budget.CallCount,` → `Draft = boundDraft, Actions = [], NarratorCallCount = budget.CallCount,` |
| `diff --stat` | `1 file changed, 1 insertion(+), 1 deletion(-)` |
| TRX | `run-m18-02/…_16_56_16.trx`，份数＝1 |
| Counters | `56/56/55/1/0` ⇒ **单枚清洁红** |
| 红格全名 | `Silence_round_produces_no_assistant_message_and_no_intent`（既有格） |
| 红消息＋锚点 | `Assert.Equal() Failure: Values differ Expected: 1 Actual: 0`（`SilentActionCount`）；`NarratorPrototypeTests.cs:497` |
| 还原 | 双哈希对平（pristine `92FBCE48…353570`，equal=True），status 0 行 |
| 结论 | 沉默支的**接线**（把绑定后的动作带不带出去）拿到单枚红。`:169` 那个 `Disposition = NarrativeRoundDisposition.Silence` 字面量本身仍是**同落多枚**：`:387`（`Round_calls_the_provider_exactly_once_for_one_narration`）与 `:494` 两格钉的是同一 token，改它至少放 2 枚红（第十三波 m14 另证过 `ProviderRejected`→`Silence` 同落 4 枚）。本波不把这枚写成单枚已证。 |

### 2.3 m18-03／04／05｜G3 快照三项 `:238/:244/:250`

三轮同一形状：把"不等即拒"改成"更新才拒"（`!=` → `>`），即只拦"比宿主新"的草稿、放走"比宿主旧"的草稿——正是 v4 §7 第 6 步要拦的那一侧。跑法同 §2.0，还原同 §2.0（pristine `92FBCE48…353570`，三轮 equal=True，status 全 0 行）。

| 轮 | 锚点行 | 改前→改后 | TRX 与 Counters | 红格（红消息／栈行号） |
|---|---|---|---|---|
| m18-03 | `:238` | `if (draft.StoryVersionBase != request.StoryVersion)` → `… > …` | `…_16_56_20.trx` 份数 1；`56/56/55/1/0` | `Draft_on_a_stale_story_version_is_refused_before_any_history`（`Expected: ContractRejected Actual: Immediate`／`:782`） |
| m18-04 | `:244` | `if (draft.OwnershipEpoch != request.OwnershipEpoch)` → `… > …` | `…_16_56_25.trx`；`56/56/55/1/0` | `Draft_from_a_fenced_writer_epoch_is_refused`（`Assert.Single() Failure: The collection was empty`／`:796`） |
| m18-05 | `:250` | `if (draft.ReceivedThroughSeqAtIssue != request.ProcessedThroughAtIssue.Seq)` → `… > …` | `…_16_56_30.trx`；`56/56/55/1/0` | `Draft_with_a_wrong_issue_baseline_is_refused_as_late`（`Assert.Single() Failure: The collection was empty`／`:808`） |

结论：三项各一枚单点反证，一一对应 E 组三格，无同落。m18-04/05 的红消息是"rejections 直接空"，说明改坏后**没有任何别的门补位**拦住旧代次／迟到基线的草稿——这条读数对 HDS-005 的落库面有用：快照三项是当前唯一把守点。

### 2.4 m18-06｜G4 `SplitContiguous` 下界门 `:288`

- 文件:行 `:288`；改前→改后＝整行删除（`ArgumentOutOfRangeException.ThrowIfLessThan(parts, 1);` → 无）；`diff --stat`＝`1 file changed, 1 deletion(-)`；TRX `…_16_56_36.trx` 份数 1；Counters `56/56/55/1/0` ⇒ **单枚清洁红**。
- 红格＝`Contiguous_split_reproduces_the_bound_slice_verbatim`；消息＝`Assert.Throws() Failure: Exception type was not an exact match Expected: typeof(System.ArgumentOutOfRangeException) Actual: typeof(System.DivideByZeroException)`；锚点 `:820`；还原双哈希对平，status 0 行。
- 结论：`:288` 有人守，且失败模式是"异常类型换形（除零）"而不是"静默少给一段"——登记给下一刀。
- 同组其余判定：
  - `:297` 余数分配（连 `:291-292`）＝**同落多枚**，单枚不可得。原因：`Contiguous_split_reproduces_the_bound_slice_verbatim`（`:813-821`）一枚 `[Fact]` 捆了四件事（确切切法／逐字拼接／`parts == 1` 快路／`parts == 0` 抛门），而经 `SplitContiguous` 的另两枚投递格（`:638` 的 12 字 3 段、`:686` 的 3 字 4 段）共用同一函数。唯一理论上单枚的形状是把 `i < remainder` 反向（读码推演：`"abcd"/3` 越界抛红、`"him"/4` 仍逐字拼接），但那要先拆既有捆绑格的形状——工单 §〇.5.2 不许动既有格，故本波**只做读码判定，未实测**，欠账照实在册。
  - `:289`（`if (parts == 1) return [slice];`）＝**改坏零代价**：`parts == 1` 时 `baseLength = slice.Length`、`remainder = 0`，走循环与快路逐字等价 ⇒ 删掉快路今天没有格会倒（进 §五 复查清单，读码判定）。

### 2.5 m18-07／m18-08｜G5 取消与超时的三支

| 轮 | 锚点 | 改前→改后 | Counters（份数） | 红格（红消息／行号） | 判定 |
|---|---|---|---|---|---|
| m18-07 | `:327` | `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)` → `… IsCancellationRequested && false)` | `56/56/55/1/0`（1，`…_16_56_42.trx`） | `Host_cancellation_stays_cancellation_and_is_not_converted_into_a_round`（`Assert.ThrowsAny() Failure: No exception was thrown Expected: typeof(System.OperationCanceledException)`／`:593`） | **单枚清洁红**：宿主取消上抛支第一次有反证 |
| m18-08 | `:333` | `return NarratorOutcome.Timeout($"provider` → `return NarratorOutcome.Failed($"provider` | `56/56/55/1/0`（1，`…_16_56_47.trx`） | `Provider_side_cancellation_with_a_live_host_token_is_named_timeout_not_failure`（`Expected: TimedOut Actual: ProviderFailed`／`:864`，本波新格） | **单枚清洁红**：取消超时支第一次有独钉格；同轮 `:549`／`:562` 两枚 40 ms 格仍全绿，说明这一轮它们没落进 `:331-334` |

`:313-321`（竞速硬上限）本波判定＝**拿不到红，但不是"不需要拆"**，三条都算进去：

1. 布尔门 `:315 if (finished != callTask)` 的可信改坏形状是"恒假"——那样 `:325 await callTask` 会等一枚永不返回的 provider（`:141` 是 `Task.Delay(Timeout.InfiniteTimeSpan, CancellationToken.None)`），`dotnet test` 挂死而不是放红。本波不跑挂死轮（烧预算且污染 TRX），登记为"改坏形状＝挂死"。
2. 产出点 `:320 NarratorOutcome.Timeout(…)` 与 `:333` 是同一枚 `Timeout` 的两个产地；既有两格（`:549` 无视取消、`:562` 守规矩取消）落哪一支由 40 ms 时序决定（第十三波 m14 也证过它们与另两格同落 4 枚）。改 `:320` 的红是"1～2 枚、按轮浮动"，不构成单枚清洁凭证 ⇒ 记"同落多枚＋时序浮动"。
3. `:318-319`（观测后台任务异常的 `ContinueWith`）＝**改坏零代价**：现有五种 provider 形状里没有"已完成且 Faulted 但不被 await"的任务，删掉它今天没有任何格倒 ⇒ 进 §五 复查清单。

### 2.6 m18-09～m18-17（G6 三行作用域／G7 不等式边界／G8 段引用／G9 四串理由）

**九轮的公共字段**（逐轮同值，先列一次免得复读）：载体全号 `147aa191c712aa7d627d74543b044df86249160e`；完整跑法＝§二.0 那条模板（`--filter "FullyQualifiedName~NarratorPrototypeTests"`，只换 `--results-directory`，全程**零 `--no-build`**）；`diff --stat` 逐轮实测＝`1 file changed, 1 insertion(+), 1 deletion(-)`；TRX 份数逐轮 `ls *.trx`＝**1**；Counters 逐轮实测＝`total=56 executed=56 passed=55 failed=1 notExecuted=0` ⇒ **九轮全是单枚清洁红，零 NotExecuted**；还原方法＝`git checkout -- <该 src 文件>`，改前/还原后 SHA-256 逐轮对平 `equal=True`，`git status --short` 行数逐轮＝**0**；预检逐轮 `VERDICT=BUILD_OK`（`free_memory_gb` 7.5/7.5/7.3/7.3/7.3/7.3/7.4/7.3/7.3）。

| 轮 | 文件:行（当场 `git diff` 头） | 改前→改后 | SHA-256 改前→改后 | TRX 文件名 | 红格全名＋消息＋栈锚点 | 结论 |
|---|---|---|---|---|---|---|
| m18-09 | `NarratorContracts.cs:43`（`@@ -43 +43 @@`） | `if (!Assembly.InstanceId.Equals(Request.InstanceId))` → `…Equals(Assembly.InstanceId))` | `3A33A815…2A9F8C` → `42AAE0B3…24E0A6` | `24782_PHYTHLINK_2026-10-23_17_02_36.trx` | `Call_refuses_an_assembly_that_belongs_to_another_scope(scope: "instance")`／`Assert.Throws() Failure: No exception was thrown Expected: typeof(...NarrativeContextAssemblyRejectedException)`／`NarratorPrototypeTests.cs:447` | **单枚清洁红**（既有 `[Theory]` 行）。实例作用域门第一次有独钉反证 |
| m18-10 | `:50` | `…Equals(Assembly.ConversationId, Request.ConversationId, StringComparison.Ordinal)` → `… , Assembly.ConversationId, …)` | `3A33A815…2A9F8C` → `51612739…8D10AC` | `…_17_02_42.trx` | 同族格 `(scope: "conversation")`／同消息／`:447` | **单枚清洁红**：会话门独钉，与 `:43` 不同落（同轮只倒 1 枚） |
| m18-11 | `:57` | `…Equals(Assembly.BatchId, Request.BatchId, StringComparison.Ordinal)` → `… , Assembly.BatchId, …)` | `3A33A815…2A9F8C` → `A6852E7B…D621F90` | `…_17_02_48.trx` | 同族格 `(scope: "batch")`／同消息／`:447` | **单枚清洁红**：批次门独钉。G6 三行至此各有单枚反证，不必建新格 |
| m18-12 | `NarrativeContextCompiler.cs:444` | `…SafetyTokens <= ContextTokens;` → `… < ContextTokens;`（收紧 1 token） | `86C30160…1D9842` → `D2389C11…4E3AFE` | `…_17_02_54.trx` | `Unified_budget_inequality_is_pinned_at_the_edge_and_one_token_past_it`（本波新格）／`Assert.True() Failure Expected: True Actual: False`／`:902` | **单枚清洁红**：`<=`／`<` 这 1 token 的边界此前**零代价**（同轮既有 49 枚全绿＝实测，不是读码） |
| m18-13 | `:294` | `SourceRefs = [contentSourceRef], EmittedText = emittedText,` → `SourceRefs = [], …` | `86C30160…1D9842` → `D6C2DCB0…5784D2` | `…_17_03_00.trx` | `Whole_section_segments_carry_their_own_source_reference_even_when_dropped`（新格）／`Assert.Equal() Failure: Collections differ ↓(pos 0) Expected: ["scene:current"] Actual: []`／`:920` | **单枚清洁红**：第十三波在册的"整段来源引用被摘掉零代价"出口，本波把它变成会响的格 |
| m18-14 | `:264` | `"cross_scope"` → `"cross_conversation"`（理由串改名） | `86C30160…1D9842` → `167E960C…F151821` | `…_17_03_06.trx` | `Compiler_removes_cross_conversation_units_and_names_the_count`（既有格）／`Strings differ Expected: "cross_scope" Actual: "cross_conversation"`／`:305` | **单枚清洁红**：跨会话理由串早被既有格点名，G9 这一枚不是欠账 |
| m18-15 | `:265` | `"cross_scope_and_unit_budget"` → `"cross_and_unit"` | `86C30160…1D9842` → `B68938AE…B7ED94` | `…_17_03_11.trx` | `Unit_run_losing_units_to_both_reasons_names_both_together`（新格）／`Strings differ Expected: "cross_scope_and_unit_budget" Actual: "cross_and_unit"`／`:961` | **单枚清洁红**：第三种混合臂（`:265` 右值）此前从未被任何格走到，是第十三波"落门没有单枚反证"的那一类 |
| m18-16 | `:285` | `DispositionReason = "not_supplied",` → `= "dropped",` | `86C30160…1D9842` → `7E92D4D9…9DFCBF` | `…_17_03_17.trx` | `Absent_sections_are_named_not_supplied_rather_than_budget`（新格）／`Collections differ at index 0 Expected: "not_supplied" Actual: "dropped"`／`:942` | **单枚清洁红**：缺段与预算降级此前共用一枚 `Dropped` 理由，审计分不开；改坏此前零代价 |
| m18-17 | `:263` | `included == 0 ? "budget"` → `included == 0 ? ""`（空理由） | `86C30160…1D9842` → `82546639…E07CAC` | `…_17_03_23.trx` | `Unit_run_that_fits_no_unit_at_all_is_dropped_and_named_budget`（新格）／`Strings differ Expected: "budget" Actual: ""`／`:982` | **单枚清洁红**：整段因预算颗粒不够而全丢时的理由串此前无人守 |

两点如实限定：

1. **红来自既有格的只有 m18-09/10/11/14 四轮**——它们还的是第十三波那笔"这枚门到底有没有人守"的证据，不是本波新格；本波新格独钉的红是 m18-12/13/15/16/17 五轮（加 §二.1 的 m18-01、§二.5 的 m18-08，共 7 枚新格各取到一枚）。既有格那四轮的其余 55 格全绿，同样说明这些门的守法没有外溢。
2. `:263-266` 里剩下的 `"unit_budget"` 臂（`:265` 左值）本波**没取到单枚**，原因是它被既有两枚独钉在同一个字面量上：`Compiler_trims_recent_messages_only_at_whole_unit_boundaries`（`NarratorPrototypeTests.cs:290`）与 `Compiler_shrinks_the_memory_wallet_unit_by_unit`（`:322`）。改这个串或改选中它的那半个条件，最省的形状也至少同时放 2 枚红 ⇒ 判**同落多枚**，本波不再为它花一轮预算（跑前读码即可定，未实测）。

活单①至此累计 **17 枚单枚清洁红**（m18-01～m18-17），其余判定见 §一.1 表的"同落多枚／够不着／共用落点"三档。

## 三 活单②：正向（`→ None`／`→ 放行`）方向的对称反证

### 3.0 口径先钉死（跑前推演，读数在 §三.2）

第十二波在册的是"拒绝侧有实测红、放行侧只有矩阵 `None` 行结构性把守"。所以本波给每枚出口补的突变方向＝**把该组合错划进拒绝集**（收紧），预测红＝**那枚本该放行、此刻必须喊出来的格**。跑前先把预测红集写全，再跑，读数只贴不改预测。

本波三枚放行侧新格（`NarrativeIdentityContractTests` 追加段，`:541` 起）与矩阵 `None` 行的对应关系先摆出来：

| 出口（`NarrativeDraftValidator.cs` 行号＝当场 `git diff` 头） | 收紧突变 | 预测红集（只应有 1 枚） |
|---|---|---|
| K1 置信度区间 `:318`（上界） | `is < 0 or > 1` → `is < 0 or >= 1` | `Candidate_at_a_closed_interval_confidence_edge_is_released(confidence: 1)`（本波新格） |
| K1 置信度区间 `:318`（下界） | `is < 0 or > 1` → `is <= 0 or > 1` | 同族格 `(confidence: 0)`（本波新格） |
| I2 意图期限 `:281` | `latest < earliest` → `latest <= earliest` | `Intent_whose_deadline_equals_its_earliest_is_released`（本波新格） |
| K2 角色侧材料不得登记成 URF `:327` | 允许集合外再加 `ExplicitFeedback` | 矩阵行 `:327`（`UserReportedFact × ExplicitFeedback → None`） |
| K5 提议不得带高置信现实来源 `:351` | 拒绝集合再加 `NarrativeProse` | 矩阵行 `:337`（`Proposal × NarrativeProse → None`） |
| K7 叙事原文只留叙事域 `:367` | `not (NarrativeProse or ModelSelfReport)` → `not NarrativeProse` | 矩阵行 `:349`（`NarrativeProse × ModelSelfReport → None`） |

预测成立的读码依据（当场 `grep` 全类实测，非推断）：
- 全类只有三处 `Confidence` 赋值：`:362`（矩阵 `.9`）、`:416`（`1.5`／`-0.1`，断言的是**拒绝**）、`:437`（`.9`）、`:563`（本波新格的 `0`／`1`）。⇒ `>= 1` 只伤 `1d` 行，`<= 0` 只伤 `0d` 行，`1.5`／`-0.1` 行照旧被拒不会误红。
- `LatestDueAtUtc` 全类只出现两处：`:461`（`Now+10` 早于 `Now+20`，断言拒绝，`<=` 后仍拒绝）与 `:584`（本波新格的相等侧）。⇒ `<=` 只伤新格。
- 三处放行侧矩阵行（`:327`／`:337`／`:349`）各只有一行落在被扩大的那半个组合上：`ExplicitFeedback` 另两行是 `CharacterFiction × :330` 与 `ConfirmedAgreement × :342`，被 `Kind ==` 前置条件隔开；`NarrativeProse` 作 origin 的 `Proposal` 行只有 `:337`；`ModelSelfReport` 作 origin 的 `NarrativeProse` 行只有 `:349`。

跑法与 §二.0 同一条模板，只把 `--filter` 换成 `FullyQualifiedName~NarrativeIdentityContractTests`（65 格），仍**零 `--no-build`**、每轮一份独立 `--results-directory`、跑前预检、跑完逐字节还原双哈希对平。

定向额度对账：§二 用掉 17 轮（m18-01～m18-17），本批 6 轮（m18-18～m18-23）＝累计 23 轮，工单上限 24 轮 ⇒ 留 1 轮作补跑/复核额度，不自作主张用掉。
## 八 守门人归位与换人复采（2026-10-23 18:1x，原样读数）

- **先更正我自己那一格**：17:1x 我曾把"这一把载体 `git status --short`＝0 行"登记成"零落盘"，那是读错了仪器——`status` 只看未提交面，看不见 detached 载体里已经提交的那一枚。`147aa19` 是真实的 217 行判据刀，已在 `a704c7c` 就地更正；本节补的是它当时的另一半——**报告断在 §三、没有出口读数**，所以缺的是凭证与归属分账，不是这把刀本身是空的。
- **归位通道（共享脏树里不 `cherry-pick`）**：第二十一波在隔离载体把 `147aa19` 换人复采之后（载体＝`.local-notes/qa-wave21-1023/carrier`，cherry-pick 后 HEAD＝`1998790d3edff99d698f39d76b08375329301251`，`git status --short`＝0、零冲突零择边），守门人把两枚判据文件按字节铺进主树并以显式路径提交＝**`639a579`**；出口 `git show --stat`＝2 files／217 insertions／0 deletions＝声明路径数。
- **字节对账**：`tests/Pry.Core.Tests/NarrativeIdentityContractTests.cs`＝`4436c95859f1643b3cdf486aba4f7f4d35bb02b79f4a193240396e657777981b`、`tests/Pry.Core.Tests/NarratorPrototypeTests.cs`＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`（载体与主树两侧同值）；`git diff --stat 1998790..639a579 -- src tests scripts Pry.slnx Directory.Build.props` **为空**＝归位后的代码树与跑出读数的载体逐位相同。他人名下两枚暂存项的索引 blob 前后同值（`6a78b81c…2d0a83d7`／`954584e3…dfa319b0`），本刀零消费。
- **换人复采（活单①）**：10 枚新格**全部拿到"单枚、单轮、清洁红"**，未取到红＝0 枚。七枚落门侧红点＝`NarratorPrototypeTests.cs:838/864/902/920/942/961/982`（同轮其余 55 格全绿），三枚放宽侧＝`NarrativeIdentityContractTests.cs:568`（置信度闭区间上下界两枚各用同一行的另一枚配方，分别只打红 `(confidence: 1)` 与 `(confidence: 0)` 那一行）与 `:587`（同轮其余 64 格全绿）。判据文件本刀一字未改，十轮每轮的 `1 file changed` 永远是 `src/`。
- **23 轮归属分账（活单②）**：临时格＝**0 轮**（判据本体在 `147aa19` 里已落盘，四路盘上证据：提交时刻 16:50:47 早于全部 24 份 TRX、reflog 无 amend、两枚判据文件与 blob 哈希在第十八波载体与第二十一波载体逐位相同、10 枚新格名在 `147aa19^` 零命中）。其中**10 轮**（m18-01、08、12、13、15、16、17、18、19、20）打在这 10 枚已落盘新格上＝可归名的凭证；**13 轮**（m18-02～07、09～11、14、21～23）打在 `147aa19` 之前的既有判据上＝那条判定线自己的证据，不记到这 10 枚名下，也不写成"白跑"。23 轮全部打在 `src/` 产品码行、零轮打在判据自身。**如实登记一处缺陷**：m18-18～23 那 6 轮在本报告原文里十一项字段一项都没给（盘上 TRX 齐、缺的是笔迹与逐轮还原哈希），由第二十一波 §3.2 逐轮代记，**不升格为"已登记"**。
- **出口读数补齐（活单③，第二十一波在等价树上取，守门人当场逐份复算）**：解决方案级构建 1 次＝0 警告 0 错误；全量 4 份 TRX 求和＝**总 1285／executed 1283／红 0**，分项 Core `total=664 executed=662 passed=662 failed=0`、Client 108、App 233、Api 280；两条跳过按 `outcome="NotExecuted"` 点名（`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`、`CoreTests.Configured_sense_voice_model_can_run_offline`）；`measure-fixed-eval.ps1 -ResultsDirectory` 跑通、catalog **76**；`git diff --check` 干净；预检 12 次全 `VERDICT=BUILD_OK`、零 `HOLD`、零作废轮（`free_memory_gb` 12.3→10.4）；`dotnet` 只认盘上产物＝12 次（定向 10＋构建 1＋全量 1），TRX 共 14 份。
- **在册基线这一格不归我动**：1285 是**另一双手在等价树上**取的读数，在册仍是守门人在 `91b2e90` 那一轮的 **1275／0／2**。按已定的两条常驻纪律——绝不为"顶基线"单独起一轮构建打断作者、顶账只押在本来就要起的那轮——这一格押到第二十波（隔离原型刀）归位时，把收尾那轮全量跑在归位后的线尾副本上顺带复采；**不做加法**。
- 仍未验证照旧：真实模型对照、生产接线（本刀零产品码）、UI 可见行为、生产库；以及第十三＋十四波那 65 格的独立换人复采（与本波那 10 格不同层，仍押在同一批刀里）。
