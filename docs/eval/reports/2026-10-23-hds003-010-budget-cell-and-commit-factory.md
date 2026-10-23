# HDS-WAVE25-1023｜片 A：统一预算不等式那枚自检的可达性问盘上；片 B：`BuildCommit` 提可见性＋绝对字面量哈希门

- 工单：`docs/dispatch-hds-wave25-20261023.md`（id＝`HDS-WAVE25-1023`，两片合一、共用一轮构建）
- 执行方：第二十五号冷启动 lane（本机唯一执行载体）
- 性质：片 A＝判据形状刀（是否动产品码由 A1 那轮的盘上答案决定）；片 B＝最小接线重构（`src/` 只动一枚文件的可见性，行为一字不变）
- 本报告由 lane 逐轮追加；lane 不提交、不 `git add`、不写状态词与结案词；归位权在守门人。

## 〇 入场读数（全部当场实测，不引用工单数字）

### 〇.1 时间与线尾（主树，创建载体之前）

```
$ date
Fri Oct 23 20:21:13     2026

$ git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD
a804a5371c3d726ea97ddb2dd8a4d735ad3f3122
```

### 〇.2 主树状态（他人资产按硬线只数行不读内容）

`git -C C:/Users/24782/Desktop/Pry_Project status --short --branch` 原样（共 8 行）：

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

- 两枚 `M ` 暂存项与全部 `??`＝他人名下资产：本刀未读其内容、未改、未提交、未 `stash`／`clean`／`rm`。
- 主树本刀唯一可写文件＝本报告。主树全程零 `git commit`／`cherry-pick`／`merge`／`tag`／`push`／FF `main`。

### 〇.3 落后核格与载体

```
$ git -C C:/Users/24782/Desktop/Pry_Project rev-list --count HEAD..a804a5371c3d726ea97ddb2dd8a4d735ad3f3122
0                      # 入场时主树即线尾，无需 merge --ff-only（主树本来也不许 merge）

$ git worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave25-1023/carrier a804a5371c3d726ea97ddb2dd8a4d735ad3f3122
Preparing worktree (detached HEAD a804a53)
HEAD is now at a804a53 docs(plan,sprint,eval,wave24): 收第二十四波纯复采刀并归位……

$ git -C .local-notes/qa-wave25-1023/carrier rev-parse HEAD
a804a5371c3d726ea97ddb2dd8a4d735ad3f3122        # 与入场线尾全号逐字相同 ⇒ 落后 0 格

$ git -C .local-notes/qa-wave25-1023/carrier status --short | wc -l
0
```

载体靶子文件的入场字节（突变后要与这两枚逐字节比对）：

```
86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842  src/Pry.Core/Narrative/NarrativeContextCompiler.cs
92fbce48c7948105479a6ca3fe75697dc24201c4f4d120b7ecc086729c353570  src/Pry.Core/Narrative/NarrativeRoundRunner.cs
```

`git log -1 --format=%H -- <两枚靶子文件>`＝`290a603fa59d811990cdf2f53fc8a9dd44205479`（HDS-003 归位那把，之后无人改过字节）。

所有 `dotnet`、所有突变、所有还原只发生在载体里。

### 〇.4 在册门禁基线（当场从上一把报告读，不引用工单）

- 现值＝**1333／executed 1331／failed 0／跳过 2**；分项 Client 108／App 233／Api 280／Core total 712（Core executed 710）。
- 取处＝第二十四波那份报告 `docs/eval/reports/2026-10-23-hds003-005-second-hand-recount-prototype.md` §〇.4（第 50 行）与守门人 §七 署名节（第 239 行，四份 TRX 逐份求和同值），并由 `docs/plans/PROJECT_PLAN.md` §9 那条 2026-10-23 20:3x entry 复述确认。
- 第二十四波已交回（`a804a53` 就是它的归位提交）⇒ 本刀起跑前置达成。
- 本刀**会新增判据格**，出口读数应当高于 1333、差值＝本刀新格数，逐格点名；起点对账等式＝`1333 ＋ 本刀新格数 ＝ 出口数`，由收尾那四份原始 TRX 当场求和写出。

### 〇.5 允许文件面（超出即作废）

1. `.local-notes/wave25-1023/`（自建，gitignored）＝仪器与全部 TRX；`.local-notes/qa-wave25-1023/carrier`＝执行载体。
2. 报告＝本文件（主树路径 `docs/eval/reports/2026-10-23-hds003-010-budget-cell-and-commit-factory.md`，lane 不提交）。
3. 片 A 判据文件（仅走乙路时新建）＝`tests/Pry.Core.Tests/NarrativeBudgetInequalityCellTests.cs`。
4. 片 B 产品文件＝只有 `src/Pry.Core/Narrative/NarrativeRoundRunner.cs`，且只允许「`BuildCommit` 可见性 `private`→`internal static`、`_writerId` 改走入参」这一种提取；哈希九项输入（`:267`–`:271`）与 `NarrativeCommit` 字段赋值（`:273`–`:281`）逐字节不动。
5. 其余 `src/`、`tests/`、`docs/`＝零字节；`NarrativeContextCompiler.cs` 只许被读、被临时突变、不许被改掉。

### 〇.6 硬线自证口径（本刀怎么证明没走捷径）

1. 不改上游任何一道门制造可达性：`NarrativeContextCompiler.cs` 的 `:87`／`:110`／`:203`／`:246` 全程只被**临时突变**（A1 那枚，逐字节还原并核 SHA-256 见 §还原），最终字节必须与 〇.3 那枚 `86c30160…` 相同。
2. 不放宽／删除／`Assert.Skip`／注释任何既有断言，不改 `[InlineData]` 期望值；拿不到的红一律写「未取到红」＋原因。
3. 突变轮一律不带 `--no-build`；只有收尾全量允许 `--no-build`。
4. 片 B 的"两侧同工厂恒等"不作覆盖证据：绝对字面量格先故意让它红一次（〇.6 自查＝§B 那节的 R1 轮），不红就不提交。
5. 只引用 `.local-notes/wave25-1023/runs/` 与 `…/carrier` 自己目录里的 TRX 当本轮凭证；同轮多格红按消息形状＋红点行号逐格归因。
6. 不新增／不修改任何数字门槛；不接 `src/Pry.Api`／`App`／`Client`／端点／界面；不碰 `%LOCALAPPDATA%\PryCompanion`；`licenses/**`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md` 一字不碰。
7. 每一次 `dotnet` 之前先跑 `powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1`，整段输出逐轮贴在本报告；只有 `VERDICT=BUILD_OK` 才起跑；读到 `HOLD` 就地停刀并写清停在第几步。
8. 额度：解决方案级构建 ≤1、解决方案级全量 ≤1、定向 ≤8；实际用量在 §出口读数 逐次报出（只认盘上产物）。
9. 产品面自证＝`git diff --stat a804a53..HEAD -- src tests scripts Pry.slnx Directory.Build.props`（载体内）只许出现 `src/Pry.Core/Narrative/NarrativeRoundRunner.cs` ＋片 A／B 的新测试文件。

### 〇.7 必读八份的读取凭据（全部当场读过）

`docs/plans/README.md`、`docs/plans/PROJECT_PLAN.md`（§4.2 的 HDS-003 行＝第 74 行、HDS-010 行＝第 81 行；§9 最新三条 entry＝第 184／197／… 行，2026-10-23 20:3x 收第二十四波、19:5x 收第二十二／二十三波、以及其前一条）、`docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md` §6（第 158–183 行，统一预算不等式在 `:176` 那行 `输入估算 + 输出保留 + 安全余量 <= 实际可用上下文`）、`docs/agent-engineering-standards.md` 全文（§1–§10＋总原则）、`docs/legacy-chain-deprecation.md` 全文、`docs/eval/reports/2026-10-23-hds003-005-cell-evidence-audit.md` §四 T5 行（第 399 行＝#49）＋§七.3（第 446 行）、`docs/eval/reports/2026-10-23-hds010-end-to-end-pipeline-prototype.md` §三 衔接表 5→6 行（第 216 行）＋守门人 §五／§六、第二十四波报告 `2026-10-23-hds003-005-second-hand-recount-prototype.md`。

### 〇.8 守门人那张五行表的本刀当场复读（片 A 的靶面，逐行在盘上核到）

| `used` 的变更点 | 守住它的那一行（本 lane 当场读码确认） |
| --- | --- |
| `var used = preludeCost;`（`NarrativeContextCompiler.cs:103`） | `:87` `if (preludeCost > total)` ⇒ `MandatoryExceedsBudget` |
| Advisory 加入（`:126`） | `:110` `if (cost > budget.AdvisoryTokens \|\| used + cost > total)` ⇒ `ReservedDirectiveDoesNotFit` |
| `AddSection` 返回 `used + cost`（`:212`） | `:203` `if (cost > cap \|\| used + cost > total)` ⇒ 整段丢弃、`used` 不变 |
| `AddUnitRun` 循环内 `used += cost`（`:254`） | `:246` `if (wallet - cost < 0 \|\| used + cost > total)` ⇒ `break`、`used` 不变 |
| `total` 本身 | `:61` `total = ContextTokens − OutputTokens − SafetyTokens`，`:62` 非正即 `WindowTooSmall` |

`:178` `if (!assembly.WithinUnifiedBudget)` 与 `:443` `WithinUnifiedBudget =>`（`(long)InputEstimateTokens + OutputReservationTokens + SafetyTokens <= ContextTokens`）的字节位置也与工单所记一致。片 B 的两个位置＝`NarrativeRoundRunner.cs:260`（`private NarrativeCommit BuildCommit(...)`）与 `:183`（`var commit = BuildCommit(draft, request, now);`），当场核到。

## 一 片 A·A1 轮｜把"`:203` 的剩余预算这一项有没有人守"问盘上（一枚定向轮、一次真突变、逐轮十一项）

突变选择照工单 §一：在载体里把 `NarrativeContextCompiler.cs:203` 的 `used + cost > total` 这一项去掉，只去掉这一项，`cost > cap` 留着。跑法比工单那句"跑 `NarratorPrototypeTests` 那一类（49 格）"更宽：**跑整个 `Pry.Core.Tests` 工程（当场盘上 712 枚节点）**，因为要判的是"有没有任何既存格守这一项"，只看一枚类会把答案问窄；仍是同一轮、同一枚突变。

### 一.1 十一项字段

| 项 | 当场值 |
|---|---|
| ① 文件:行 | `src/Pry.Core/Narrative/NarrativeContextCompiler.cs:203`（`AddSection` 的整段留／整段丢门） |
| ② 改前原文 | `        if (cost > cap \|\| used + cost > total)` |
| ③ 改后原文 | `        if (cost > cap)` |
| ④ `git diff --stat` | `src/Pry.Core/Narrative/NarrativeContextCompiler.cs \| 2 +-` ⇒ `1 file changed, 1 insertion(+), 1 deletion(-)`；`git diff -U0` 只有 `@@ -203 +203 @@` 一处 |
| ⑤ 跑法整行 | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave25-1023/runs/a1-compiler203-remaining-term" --logger trx`（工作目录＝载体；**无 `--no-build`**，输出里可见 `Pry.Core.dll`／`Pry.Core.Tests.dll` 当场重编，突变确实进了二进制） |
| ⑥ TRX 文件名 | `24782_PHYTHLINK_2026-10-23_20_32_16.trx`（该目录 `ls *.trx \| wc -l`＝**1**） |
| ⑦ 红格名单 | **零枚红格**：`<Counters total="712" executed="710" passed="710" failed="0" … notExecuted="0" />`；`grep -c 'outcome="Failed"'`＝**0**。当场跳过的 2 枚按 `outcome="NotExecuted"` 点名＝`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`、`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`（与在册那两条同名环境跳过，非本刀造成，突变前后同值） |
| ⑧ 还原命令 | `git -C .local-notes/qa-wave25-1023/carrier checkout -- src/Pry.Core/Narrative/NarrativeContextCompiler.cs` |
| ⑨ 还原后 `git status --short` 行数 | **0** |
| ⑩ 两态 SHA-256 | 突变态＝`72340f0597b1278299dac9c27453d845f56ca013ba727584d0846d3d27dc6fb9`；还原后＝`86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842` ＝ 〇.3 的入场字节，逐字节相同 |
| ⑪ 当轮预检整段 | 见 §一.2 |

### 一.2 A1 轮预检（跑第一次 `dotnet` 之前，整段原样）

```
$ powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: leigod pid=60032 class=Chrome_WidgetWin_1 style=0x14C20000 rect=770,377,1020x637 screen=2560x1440 covers=False borderless=False title=?????
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=7% used=8967MB
free_memory_gb=8.7
VERDICT=BUILD_OK
```

存档＝`.local-notes/wave25-1023/out/preflight-A1.txt`。

### 一.3 A1 的盘上答案＝**答乙**（没有任何既存格守这一项），以及为什么不是"问错了地方"

1. 712 枚节点里 **failed＝0**：把 `:203` 的"剩余预算"这一项整枚删掉，盘上一格都不响。按工单 §一 的分支表，这就是**答乙**＝`:203` 的"该段装得下自身份额、但 `used + cost` 超剩余 `total`"这一项今天没人守 ⇒ 片 A 走**乙路**：新建 `tests/Pry.Core.Tests/NarrativeBudgetInequalityCellTests.cs` 补**一格会响的格**，**零产品码、＋1 格**。
2. 突变的爆炸半径当场静态核过（不靠运气）：`grep -rln "NarrativeContextCompiler\|NarrativeContextAssembly" tests/ src/` 的源码命中只有 `src/Pry.Core/Narrative/{NarrativeContextCompiler,NarrativeLifecycleCoordinator,NarrativeRoundRunner,NarrativeTurnPipeline,NarratorContracts}.cs` ＋ `tests/Pry.Core.Tests/NarratorPrototypeTests.cs`；`tests/Pry.App.Tests`／`Pry.Api.Tests`／`Pry.Client.Tests` 三处**零命中**。间接吃它的（RoundRunner／Pipeline／Coordinator）全在 `Pry.Core.Tests` 里，而那 712 枚本刀全跑了 ⇒ "没有任何既存格响"这句话覆盖的是整个 Core 面，不是一枚类。这一格是静态口径，**我没有在另外三个工程上跑突变轮**（登记在 §未验证）。
3. 关于 `:178` 那句"按名钉 `BudgetInequalityViolated`"：**这条路在盘上没有可判红的实现路径**。理由两半——(a) 〇.8 那五行表：`used` 的每一个变更点都在加进去之前被一道门守住，所以 `used <= total` 由构造成立，而 `total = ContextTokens − OutputTokens − SafetyTokens`（`:61`）⇒ `InputEstimateTokens + OutputReservationTokens + SafetyTokens <= ContextTokens`（`:443` 的不等式）恒真 ⇒ `:178` 的 `if (!assembly.WithinUnifiedBudget)` 拿不到可达输入；(b) A1 盘上读数：即便**亲手拆掉** `:203` 那道门（唯一能让 `used` 越过 `total` 的位置），712 枚节点仍然零红——想借"删守卫"造出 `:178` 的红，盘上根本造不出来，硬写的"按名钉 `BudgetInequalityViolated`"就是一枚永远绿的常绿门。**诚实的交付是登记不可达，不是造一枚永远绿的断言去顶这个名**（工单 §一 末句，本 lane 照登）。删掉 `:178` 那个分支在盘上零可观测变化。
4. 因此片 A 的新格守的是 `:203`，**不守 `:178`**；`:178` 那一格仍是一笔补不上的账（同族先例＝`ContextBuilder.cs:39` 的 W2 兜底支、`NarrativeReplyPacing.cs:91` 的防御支）。也不许写成"已由 A1 响的那几格代替覆盖"——A1 响的那几格数量是**零**，`:203` 的其余半（`cost > cap`）今天由 `NarratorPrototypeTests` 里那几格守（例：`Compiler_drops_a_section_that_exceeds_its_own_share` 一类），本格守的是它们的另一半。

## 二 片 A 乙路新格落地＋片 B 可见性提取，R2＝清洁基线轮（零突变）

### 二.1 片 A 的新判据文件（乙路交付物，＋1 格）

新建 `tests/Pry.Core.Tests/NarrativeBudgetInequalityCellTests.cs`（121 行，1 枚 `[Fact]`），类名＝`NarrativeBudgetInequalityCellTests`，格名＝`Compiler_drops_a_whole_section_that_fits_its_own_share_but_not_the_remaining_input_budget`（`[Fact]` 在第 58 行，方法体第 59 行起；当场 `grep -n "\[Fact\]\|\[Theory\]\|\[InlineData"` 只有第 58 行这一条命中）。形状逐条对齐工单 §一 答乙：

- **① 载荷只让后半项响**：`Identity`="Pry canon"(9B)、`OutputRules`="json rules"(10B)、当前事件="hello there"(11B) ⇒ 必需前奏成本 `20+8+11+8 = 47`（`:85-86` 的同一算式，`MessageFramingOverhead = 8`）；预算 `ContextTokens=200, OutputTokens=100, SafetyTokens=50` ⇒ `total = 50`（`:61`）；State 段内容="studio" ⇒ `addition = "\nState：\nstudio"` 的 UTF-8 字节数 `1+5+3+1+6 = 16`（"："＝U+FF1A＝3 字节，估算器＝`ConservativeTokenEstimator`＝UTF-8 字节数，`Memory/MemoryContracts.cs:114`）。`16 <= cap(240)` ⇒ 前半项 `cost > cap` 不响；`47 + 16 = 63 > 50` ⇒ 只有后半项 `used + cost > total` 响。
- **② 断言面在 `:203` 的上游可观测处，且左边取确切数字**：`ReasonCode == Ok`（整轮不拒）、`Accepted`、该段 `Disposition == Dropped`、`DispositionReason == "budget"`、`EmittedText == ""`、`UsedTokens == 16`（并与 `Estimator.Count("\nState：\n"+content)` 同值＝估算口径一致）、`InputEstimateTokens == 47`（丢弃的 16 一分不进账）、`InputBudgetTokens == 50`、`InputEstimateTokens + OutputReservationTokens + SafetyTokens == 197`、`ContextTokens == 200`、`WithinUnifiedBudget` 为真、`SegmentCount == 8`。
- **③ 注释写明它守 `:203`、不守 `:178`**：类头与格前注释都点名 `NarrativeContextCompiler.cs:203` 的后半项，并写明 `:178` 在 `:203` 完整时拿不到可达输入（〇.8 五行表），"按名钉 `BudgetInequalityViolated`"没有可判红路径 ⇒ 仍登记为补不上的账。
- 片 A **零产品码**：`src/` 里片 A 相关的字节改动＝无。

### 二.2 片 B 的产品提取（`NarrativeRoundRunner.cs`，唯一的 `src/` 改动，三处）

`git diff -U2` 原文（当场取）：

```
@@ -181,5 +181,5 @@ public sealed class NarrativeRoundRunner
         NarratorCallBudget budget, DateTimeOffset now)
     {
-        var commit = BuildCommit(draft, request, now);
+        var commit = BuildCommit(draft, request, now, _writerId);
         _history.RegisterAppliedCommit(commit, request.MaxOutboundSegments);
@@ -258,5 +258,6 @@ public sealed class NarrativeRoundRunner
     }
 
-    private NarrativeCommit BuildCommit(NarrativeDraft draft, NarrativeRequest request, DateTimeOffset now)
+    internal static NarrativeCommit BuildCommit(NarrativeDraft draft, NarrativeRequest request, DateTimeOffset now,
+        string writerId)
     {
         var commitId = "commit-" + draft.ProposalId;
@@ -278,5 +279,5 @@ public sealed class NarrativeRoundRunner
             ProcessedThrough = draft.ProcessedThrough, PerceivedInputRefs = draft.PerceivedInputRefs,
             ExpectedStoryVersion = draft.StoryVersionBase, DeletionEpoch = request.DeletionEpoch,
-            OwnershipEpoch = draft.OwnershipEpoch, ContentHash = hash, CommittedByWriter = _writerId,
+            OwnershipEpoch = draft.OwnershipEpoch, ContentHash = hash, CommittedByWriter = writerId,
             CommittedAtUtc = now, ProposalId = draft.ProposalId, ConsumedInputSeqs = consumed,
         };
```

`git diff --stat`＝`src/Pry.Core/Narrative/NarrativeRoundRunner.cs | 7 ++++---` ⇒ `1 file changed, 4 insertions(+), 3 deletions(-)`。九项哈希输入（原 `:267`–`:271`）与 `NarrativeCommit` 的字段赋值（原 `:273`–`:281`，签名换行后整体下移一行）**逐字节未动**；`Fail`／`Compile` 一字未碰；没有新增 `InternalsVisibleTo`（`src/Pry.Core/Properties/AssemblyInfo.cs:3` 早就在盘上）；可见性只到 `internal`（不是 `public`）。

新格数（片 B）＝`tests/Pry.Core.Tests/NarrativeCommitFactoryTests.cs`（179 行）：`[Fact]` 第 55 行（绝对字面量）、`[Theory]` 第 74 行＋三行 `[InlineData]`（第 75／76／77 行＝三条区分性探针，各钉自己的绝对字面量）、`[Fact]` 第 98 行（零行为漂移）。当场 `grep -n` 命中＝55／74／75／76／77／98 六行，注释里没有第二枚 `[Fact]` 字样（`grep -c '[Fact]'` 两枚文件＝1＋2＝3 枚属性，节点数＝1＋3＋1＝5）。

绝对字面量的取法＝**独立仪器**：`.local-notes/wave25-1023/out/compute-commit-hashes.ps1`（不读产品码，自己按九项＋U+241F 拼接后取 SHA-256 小写十六进制），输出存档＝`out/hash-literals-independent.txt`：

```
BASE        = 99a0cc4bed9061763112b52bdb545df3d5457082887f757ae45100582c12eda4
V1_PROSE    = 508fda3f3f4b3ac3ec6d17d3a8fab9fd992d130ca85f9da3623d28595c7f6e17
V2_ACTMODE  = 8667f316fc4b04115cb13ab0a1ff948e3b176605bc26b6b7f680c0aede13144e
V3_WATERMARK= ce3c9175ce887194f67576031754bdec249cd9ed8811e73286cba84b9b73a136
```

九项内容与顺序在测试类头注释里逐条写明（① commitId ② InstanceId.Value ③ ConversationId ④ BatchId ⑤ NarrativeProse ⑥ ProcessedThrough.ToString()（`NarrativeDocuments.cs:66` 把它重写成水位数字）⑦ 感知引用序号逗号连接 ⑧ 每条动作 `ActionId:(int)Mode` 逗号连接 ⑨ 每条意图 IntentId 逗号连接；本夹具的意图列表为空 ⇒ 第九项是空串）。

### 二.3 R2 轮（清洁基线，十一项）

| 项 | 当场值 |
|---|---|
| ① 文件:行 | **本轮零突变**（清洁基线轮：产品面＝二.2 那三处，判据面＝二.1＋二.2 两枚新文件） |
| ② 改前原文 | 不适用（无突变）；产品字节＝`a804a53` 面 ＋ 二.2 的三处提取 |
| ③ 改后原文 | 同二.2 的 `git diff -U2` 原文 |
| ④ `git diff --stat` | `src/Pry.Core/Narrative/NarrativeRoundRunner.cs \| 7 ++++---` ⇒ `1 file changed, 4 insertions(+), 3 deletions(-)`；`git status --short`＝3 行（1 枚 `M ` ＋ 2 枚 `??` 新测试文件） |
| ⑤ 跑法整行 | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave25-1023/runs/r2-clean-baseline-newcells" --logger trx`（无 `--no-build`） |
| ⑥ TRX 文件名 | `24782_PHYTHLINK_2026-10-23_20_48_37.trx`（目录内 `ls *.trx`＝1） |
| ⑦ 红格名单 | **零枚红**：`失败: 0，通过: 716，已跳过: 2，总计: 718` ⇒ 712 既存＋6 新格全绿。**这一格的意义＝独立仪器算出的四枚绝对字面量与产品码当场算出的摘要逐字相同**（若九项集合或顺序被我读错，基线轮就会红在这里）。两条跳过＝`Configured_llama_server_requires_the_child_environment_key`／`Configured_sense_voice_model_can_run_offline`（在册同名环境跳过） |
| ⑧ 还原命令 | 不适用（本轮无突变，无需还原） |
| ⑨ 还原后 `git status --short` 行数 | 3（＝本轮结束时载体状态：1 枚 `M `＋2 枚 `??`，与本轮开始时逐行相同） |
| ⑩ SHA-256 | `NarrativeContextCompiler.cs`＝`86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842`（＝入场字节，靶子本轮未被改）；`NarrativeRoundRunner.cs`＝`961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9`（提取态，突变前的基准） |
| ⑪ 当轮预检 | 见 §二.4 |

### 二.4 R2 轮预检整段

```
$ powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: Taskmgr pid=38736 class=TaskManagerWindow style=0x14CF0000 rect=359,289,1205x935 screen=2560x1440 covers=False borderless=False title=?????
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=6% used=9051MB
free_memory_gb=10.8
VERDICT=BUILD_OK
```

## 三 A2 轮＝**未起跑**（预检读到 `VERDICT=HOLD` ⇒ 就地停刀）；本 lane 的停止点与已落盘部分

### 三.1 停刀时的预检整段（这是本 lane 最后一次 `dotnet` 之前的预检，也是停刀依据）

```
$ powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: helldivers2 pid=53960 class=stingray_window style=0x94880000 rect=-1,-1,2562x1442 screen=2560x1440 covers=True borderless=False title=HELLDIVERS? 2
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=35% used=6303MB
free_memory_gb=3
WARN (does not block the build):
  free_physical_memory=3 GB (gate readings may be invalid; SQLite disk-I/O/OOM shape)
VERDICT=HOLD
  reason: focused window covers the screen and its process path is unreadable (helldivers2, class stingray_window)
  reason: focused window is a known game-engine window class covering the screen (stingray_window, helldivers2)
```

存档＝`.local-notes/wave25-1023/out/preflight-A2.txt`。按工单 §〇.7 与 `AGENTS.md`：读到 `HOLD` **就地停刀、不请示、不等、不改预检判据、不绕过**。此后本 lane **零 `dotnet` 调用**（没有跑 A2、没有跑片 B 的红证轮、没有跑解决方案级构建、没有跑收尾全量）。

### 三.2 停刀时那一枚突变的状态（十一项里能当场给的部分，红证那三项如实写"未取到"）

| 项 | 当场值 |
|---|---|
| ① 文件:行 | `src/Pry.Core/Narrative/NarrativeContextCompiler.cs:203` |
| ② 改前原文 | `        if (cost > cap \|\| used + cost > total)` |
| ③ 改后原文 | `        if (cost > cap)`（与 A1 轮逐字节同一枚突变，`git diff -U0` 只有 `@@ -203 +203 @@` 一处） |
| ④ `git diff --stat` | `src/Pry.Core/Narrative/NarrativeContextCompiler.cs \| 2 +-` ⇒ `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤ 跑法整行 | **未执行**（`dotnet` 未起跑）；计划中的整行＝`dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave25-1023/runs/r3-compiler203-newcell-red" --logger trx` |
| ⑥ TRX 文件名 | **未取到**（无该目录、无 `.trx`） |
| ⑦ 红格全名＋消息要点＋红点行号 | **未取到红**（原因＝预检 `HOLD`，见 §三.1）。计划中的期望红＝`NarrativeBudgetInequalityCellTests.Compiler_drops_a_whole_section_that_fits_its_own_share_but_not_the_remaining_input_budget`，第一枚失败的断言是 `Assert.Equal(NarrativeCompileReason.Ok, assembly.ReasonCode)`（文件第 65 行），要点＝`Expected: Ok / Actual: BudgetInequalityViolated`（因为删掉后半项后 State 被纳入 ⇒ `used=63 > total=50` ⇒ `:178` 才第一次拿到可达输入）。**这条推断没有盘上凭证，是设计说明不是读数**（工单 §〇.6.2 口径） |
| ⑧ 还原命令 | `git -C .local-notes/qa-wave25-1023/carrier checkout -- src/Pry.Core/Narrative/NarrativeContextCompiler.cs`（**已执行**，见 §三.3） |
| ⑨ 还原后 `git status --short` 行数 | 3（`M src/Pry.Core/Narrative/NarrativeRoundRunner.cs` ＋ 2 枚 `??` 新测试文件＝片 B 的在册交付面） |
| ⑩ 突变态与还原后 SHA-256 | 突变态＝`72340f0597b1278299dac9c27453d845f56ca013ba727584d0846d3d27dc6fb9`；还原后＝`86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842` ＝ 〇.3 入场字节 |
| ⑪ 当轮预检 | §三.1 整段＝`VERDICT=HOLD` ⇒ 本轮作废未起跑 |

### 三.3 载体收尾状态与归位面（本 lane 在自己载体内的唯一一笔提交）

- **停刀后处置**：先把 §三.2 那枚突变逐字节还原（靶子文件回到入场 SHA），再在载体内提交片 A／B 的交付面，使载体不留活动突变（第二十三波那把遗留孤儿突变的教训）。
- 载体提交＝`24d1b6f8b0a4a3d199dcab832e15facc84a16a01`（detached @ 入场号 `a804a5371c3d726ea97ddb2dd8a4d735ad3f3122` 之上，1 枚 `M` ＋ 2 枚新建，`3 files changed, 304 insertions(+), 3 deletions(-)`）。
- 提交后 `git -C …/carrier status --short \| wc -l`＝**0**。
- 工单 §三.6 那句"`git rev-parse HEAD` 仍是入场号"与 §〇.4"交付物只落在你自己的载体＋那一份报告文件"在本刀里无法同时成立：我按第二十三波同型的**载体提交通道**走（不提交就没有可归位的字节面，也不可能有 0 行 `git status`），并把两处 SHA 都当场贴出。是否改判由守门人定（§归位通道）。
- 产品面 `git diff --stat a804a5371c3d726ea97ddb2dd8a4d735ad3f3122..HEAD -- src tests scripts Pry.slnx Directory.Build.props` 原文：

  ```
   src/Pry.Core/Narrative/NarrativeRoundRunner.cs     |   7 +-
   .../NarrativeBudgetInequalityCellTests.cs          | 121 ++++++++++++++
   .../Pry.Core.Tests/NarrativeCommitFactoryTests.cs  | 179 +++++++++++++++++++++
   3 files changed, 304 insertions(+), 3 deletions(-)
  ```

  ＝**一枚产品文件＋两枚新测试文件**，零既有测试文件改动、零 `scripts`／`Pry.slnx`／`Directory.Build.props` 改动。工单 §三.5 写"只许出现两枚文件"，而 §二 B1 明写判据可放"同一片 A 那个新文件或**另一枚新文件**，由你定"：我选了按判据面分文件（装配器的预算门／提交工厂的摘要门），文件数是 3 而非 2，此处如实登记不合字面之处，不做掩饰。
- 主树收尾（当场实测）：`git status --short --branch`＝**9 行**＝入场那 8 行（§〇.2）＋本报告新增的 1 行 `?? docs/eval/reports/2026-10-23-hds003-010-budget-cell-and-commit-factory.md`；两枚他人名下 `M ` 暂存项与其余 `??` 逐行原样未动。`git ls-files | wc -l`＝**601**（与入场登记值相同＝本 lane 在主树零跟踪文件写入，报告由守门人归位后才往上走）。主树零 `git` 写操作、零 `dotnet`。
- `git diff --check`（载体，提交前跑）＝无输出、退出码 0。

### 三.4 R2 轮的一条自我更正（不许漏）

R2 的构建输出里出现过**一条警告**：`warning xUnit2000: … WriterId should be passed as the 'expected' argument … NarrativeCommitFactoryTests.cs(122,9)`（我的参数顺序写反）。停刀前已把它改成 `Assert.Equal(WriterId, direct.CommittedByWriter)`（同参数顺序、单行替换、字节数不变），但**这次改动之后没有再跑过任何构建**（HOLD）⇒ 工单 §三.1 要求的"解决方案级构建 0 警告 0 错误"这一格本刀**未取到读数**，已改与未证之间不许混谈：登记在 §五 未验证第 1 条。

## 四 出口读数（能给的我给了，取不到的点名）

| 工单 §三 那十格 | 本刀状态 |
|---|---|
| 1 解决方案级构建 0 警告 0 错误 | **未取到**（HOLD 之后零 `dotnet`；唯一一次工程级构建是 R2 的 `dotnet test`，其控制台打印过 1 条 xUnit2000 警告，见 §三.4） |
| 2 全量一次＋四份 TRX `<Counters>` 逐份 | **未取到**（未跑收尾全量）。盘上本 lane 只有两枚 TRX：`runs/a1-compiler203-remaining-term/24782_PHYTHLINK_2026-10-23_20_32_16.trx`（712／710／710／0）与 `runs/r2-clean-baseline-newcells/24782_PHYTHLINK_2026-10-23_20_48_37.trx`（**718／716／716／0**，executed 716，两条 `outcome="NotExecuted"`＝`CoreTests.Configured_sense_voice_model_can_run_offline`／`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`，四条 `notExecuted` 计数器为 0 不看）。**它们是单工程定向轮，不是解决方案级全量，不顶在册账** |
| 3 对账等式由四份原始 TRX 求和 | **未取到**。能给的部分＝起点 1333（〇.4，取处逐字写明）＋本刀新格 **6**（A 1 枚节点＋B 5 枚节点，逐格行号见 §二.1／§二.2 的当场 `grep -n`）⇒ 期望出口 **1339**；等式右端须由下一手的收尾全量四份 TRX 当场求和确认，本 lane 不预写、不自封基线 |
| 4 `scripts/measure-fixed-eval.ps1 -ResultsDirectory <本轮目录>` | **未跑**（该脚本读的是解决方案级那四份 TRX，本刀没有） |
| 5 产品面 diff 只许两枚文件 | 见 §三.3：**3 枚**（1 产品＋2 新判据文件），`src/` 只碰 `NarrativeRoundRunner.cs`，其余 `src`／`tests` 既有文件零字节 |
| 6 载体收尾 `git status --short`＝0 且 HEAD 仍是入场号 | `status`＝**0 行**；HEAD＝`24d1b6f…`（≠入场号，载体提交通道，理由见 §三.3） |
| 7 `dotnet` 次数只认盘上产物 | `ls .local-notes/wave25-1023/runs/*/` 实数＝**2 个目录、各恰 1 枚 `.trx`** ⇒ 定向轮 **2／8**；解决方案级构建 **0／1**、全量 **0／1**（都因 HOLD 未用）。两次 `dotnet test` 均无 `--no-build`，突变确实进了二进制 |
| 8 报告总行数与各节行数 | 当场实测见 §六 末（本报告最后一节） |
| 9 `git diff --check` 干净 | 载体：提交前跑＝无输出、退出码 0；主树：本 lane 未动他人面，`git diff --check` 由守门人归位轮取 |
| 10 A1 突变的还原凭据单列一节 | §一.1 的 ⑧⑨⑩＋§三.2 的 ⑧⑨⑩（两枚同字节突变、各自还原并核 `86c30160…`）；靶子文件今天＝线尾字节，`git log -1 --format=%H -- src/Pry.Core/Narrative/NarrativeContextCompiler.cs`＝`290a603fa59d811990cdf2f53fc8a9dd44205479` 未变 |

## 五 未验证／未取到（逐条点名，不用"已完成"混过去）

1. **解决方案级构建 0 警告 0 错误**＝未取到（HOLD）；且 §三.4 那条警告的修复未经任何重新构建证明。
2. **解决方案级全量与在册顶账**＝未跑；`1333 ＋ 6 ＝ 1339` 只是本 lane 的新格数申报，不是出口读数。
3. **A2 轮（片 A 新格的红证）＝未取到红**。因此工单 §一 答乙要求的那件事实——"这一格确实会响"——目前只有**设计论证**（§三.2 第 ⑦ 项那条推断）而**没有盘上凭证**。第二十四波之前那片 A 的账按此性质登记：**不许签成已付**。
4. **片 B 的红证（工单 §二 B1 的"必须先故意让它红一次"）＝未取到**。R2 只证明了"绝对字面量与产品码当场算出的摘要相同（两侧同值）"，而 §〇.6.4 明写这句话**不算覆盖证据**；把九项哈希输入删一项／换位置的突变轮没有跑，四枚字面量摘要门在突变下是否会红**未验证**。⇒ 片 B 的新格今天仍是"未证伪"状态，不满足"取过红"的交付条件。
5. **换人复采**＝本 lane 不适用（写判据的这只手不能同时是取红的第二双手），押下一把复采刀。
6. **宿主调用点仍为零**：`NarrativeRoundRunner.BuildCommit` 现在可被宿主侧调用（`internal`＋`InternalsVisibleTo`），但全仓没有任何生产调用方；`NarrativeTurnPipeline` 依旧没有生产接线；`NarrativeTurnPipeline.cs` 那处 `CommitFactory`（`:82` 声明／`:276` 调用）本刀一字未动。不许把片 B 读成"新链已接线"。
7. `:178` 的"按名钉 `BudgetInequalityViolated`"＝盘上无可判红路径，永久无人按名钉（产品事实，本刀登记为不可达，见 §一.3）。
8. 真实模型对照＝零读数；UI 可见行为＝零读数；生产库＝零接触（`%LOCALAPPDATA%\PryCompanion` 未碰）；待标定参数权威值＝未定；旧链删除片与"构建时不把旧链打进包"那道门＝未开始（`docs/legacy-chain-deprecation.md` §4 第 2／3／4 项照旧全欠）；三句禁令（"旧链可删／不进包已做到／新链已接线"）本刀一句都不成立。
9. 另外三个测试工程（App／Api／Client）在 `:203` 突变下的响应＝未跑（只有 §一.3.2 那条静态引用面核证）。

## 六 续跑配方（下一手从这一枚突变、这一行接着跑，不需要重做已完成部分）

前置：本 lane 已完成 A1（红证：零枚响＝答乙）、片 A 新格与片 B 提取＋判据文件（载体提交 `24d1b6f`）、R2 清洁基线（718 全绿）。**待跑＝三轮定向＋一轮构建＋一轮全量**，每轮之前先跑 §〇.7 那条预检并把整段贴进本报告：

1. **A2（片 A 红证，1 轮）**：载体里把 `src/Pry.Core/Narrative/NarrativeContextCompiler.cs:203` 的 `used + cost > total` 半项删掉（改法与还原凭据逐字节同 §一.1／§三.2），跑
   `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --results-directory "…/wave25-1023/runs/r3-compiler203-newcell-red" --logger trx`；
   预期红＝**恰 1 枚**＝`NarrativeBudgetInequalityCellTests.Compiler_drops_a_whole_section_that_fits_its_own_share_but_not_the_remaining_input_budget`，其余节点全绿（本轮总数应为 718＝712 既存＋6 新格；除本格与两条在册环境跳过外都应 `passed`），片 B 那 5 枚在新格红的前提下仍应全绿；红点行号与消息要点从 TRX 的 `outcome="Failed"` 节点当场取，不许用我的推断顶。跑完 `git checkout --` 还原并核 `86c30160…`。
2. **B 红证（1 轮）**：还原靶子后，在 `src/Pry.Core/Narrative/NarrativeRoundRunner.cs` 的哈希输入数组里删掉第⑨项——提交态那一行＝第 272 行 `string.Join(",", draft.Intents.Select(i => i.IntentId))])))).ToLowerInvariant();`（只去掉数组里这一个成员，`NarrativeCommit` 的字段赋值一字不动），跑
   `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --results-directory "…/wave25-1023/runs/r4-hash-sequence-mutated" --logger trx`；
   预期红＝片 B 那 4 枚字面量节点（第 55 行 `[Fact]`、第 74 行 `[Theory]` 的第 75／76／77 行三枚 `InlineData`）**＋** 第 98 行那格里第 117 行的 `Assert.Equal(BaseHash, direct.ContentHash)`。若第 55 行那格**不响**，按工单 §二 B1 的硬线＝常绿门，**不许提交它**，把它撤掉再登记。跑完还原并核 `92fbce48c7948105479a6ca3fe75697dc24201c4f4d120b7ecc086729c353570`（入场字节）。
3. **收尾（1 构建＋1 全量，全量允许 `--no-build`）**：载体回到 `24d1b6f` 的干净面（`git status --short`＝0）后跑
   `dotnet build Pry.slnx -c Release`，再
   `dotnet test Pry.slnx -c Release --no-build --results-directory "…/wave25-1023/full" --logger trx`，
   先 `ls *.trx` 数到 4 枚再逐份读 `<Counters>`；对账等式由那四份当场求和（Core 应为 712＋6＝718、总 1339；两条同名跳过按 `outcome="NotExecuted"` 点名）；随后 `scripts/measure-fixed-eval.ps1 -ResultsDirectory "…/wave25-1023/full"`。
4. **给守门人的核账四件事**（工单 §四）：① 片 B 的产品 diff 是否真只碰可见性＋调用点＋`_writerId` 来路（§二.2 的三处 `git diff -U2` 原文）；② 片 A 走的是**乙路**，§9 与 `docs/full-scope-sprint.md` §3 的措辞必须是"补了一格会响的格（守 `:203` 后半项）＋登记 `:178` 不可达"，两半都要在，不许写成"甲路零新格"，也不许写成"`:178` 已被覆盖"；③ 第 55 行那格在 r4 突变下真红过（本 lane **未取到**，须由续跑那手补）；④ 在册基线由归位那轮顶账，本 lane 未自封。

## 七 报告行数（当场实测）

`wc -l`／`wc -c` 与逐节边界＝最后一次落盘后由本 lane 当场实取（`grep -n "^## [一二三四五六七〇]"` 取节首行号）：**总行数＝338**（`wc -l`，行数在本节之后的自我编辑里保持稳定）；`wc -c` 在写入本句前一刻＝39,126 字节（字节数会随本句自身变长，故以行数为准，守门人归位时当场复取即可）；头部 1–7＝7 行、§〇 8–108＝101 行、§一 109–149＝41 行、§二 150–231＝82 行、§三 232–290＝59 行、§四 291–305＝15 行、§五 306–317＝12 行、§六 318–333＝16 行、§七 334–338＝5 行。本节是报告的最后一节，本节之后不再有未落盘的盘上产物（载体面见 §三.3；两轮 TRX 与四枚预检存档见 `.local-notes/wave25-1023/`）。

**本 lane 到此停止**：A2 与片 B 红证两轮、解决方案级构建与收尾全量、`measure-fixed-eval.ps1` 均因 §三.1 那次 `VERDICT=HOLD` 未起跑；续跑配方在 §六。

---

## 八 守门人出口核验与归位裁决（签名 `eval-QA-WAVE25-GK-1023`，10-23 21:1x）

上面 §〇–§七 是第二十五波 lane 的原文，我一字未改、未代写、未署它的名。这一节是我这一手的独立复数与裁决，只读盘上产物。

### 八.1 停刀依据成立（这一把没有硬闯）

- 我复读了它的三次预检存档（`.local-notes/wave25-1023/out/preflight-{A1,B1,A2}.txt`）＝A1 `BUILD_OK free=8.7`、B1／R2 `BUILD_OK free=10.8`、A2 `HOLD`。§三.1 那段 `HOLD` 我逐字核过：前台窗＝`helldivers2 pid=53960 class=stingray_window rect=-1,-1,2562x1442 covers=True`，两条 reason 都在，`free_memory_gb=3` 那条 WARN 也如实带着。
- 停刀之后它**确实零 `dotnet`**：`runs/` 实数 2 个目录、各恰 1 枚 `.trx`（`20_32_16`＝A1、`20_48_37`＝R2），没有第三个目录、没有 `full/`。这与它 §四.7 的自述一致，我按盘上产物采信。
- 21:0x 我自己又跑了一次预检＝**仍 `HOLD`**（同一 pid、同一窗、`free_memory_gb=4`）⇒ 归位这一手在我手里也必须零 `dotnet`，所以本刀的代码**不带构建进主树**，见八.4。

### 八.2 盘上读数复数（不采信自述）

| 轮 | 我读到的 `<Counters>` | 采信什么 |
|---|---|---|
| A1（删 `NarrativeContextCompiler.cs:203` 的 `used + cost > total` 半项） | `total=712 executed=710 passed=710 failed=0` | **零枚既存格响** ⇒ 工单 §一 的答乙成立；这一项今天无人守 |
| R2（清洁基线，载有 6 枚新格） | `total=718 executed=716 passed=716 failed=0` | 新格数＝718−712＝**6**，与它申报的 A 1 枚＋B 5 枚相符；两条 `outcome="NotExecuted"` 点名＝`CoreTests.Configured_sense_voice_model_can_run_offline`／`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`（与在册那两条同源） |

这两轮都是**单工程定向轮，不是解决方案级全量**，所以都不顶在册账。

### 八.3 交付面的字节我逐行读过

- 主树：`src/Pry.Core/Narrative/NarrativeContextCompiler.cs` SHA-256＝`86c301604dc1…d9842`、`NarrativeRoundRunner.cs`＝`92fbce48c794…353570`，两者都等于各自行 `git show HEAD:<该文件>` 的 HEAD-blob 值 ⇒ **lane 说"靶子文件今天＝线尾字节"这句我核到为真**，主树没有任何未登记的代码改动（`git status --short` 上代码面只有他人名下那枚 `M tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）。
- 载体提交 `24d1b6f8b0a4a3d199dcab832e15facc84a16a01` 的父＝`a804a53`＝入场线尾，**不落后、不夹带他人提交**；`git diff --stat a804a53..24d1b6f` ＝三枚文件、`304 insertions(+), 3 deletions(-)`；提交后载体 `git status --short`＝0 行。
- `src/` 那一枚的 `7 +-` 我逐 hunk 读过（三处）：`:183` 调用点传 `_writerId`、`:260` 签名 `private`→`internal static` 并加 `string writerId` 入参、`:281` `CommittedByWriter = writerId`。九项哈希输入序列（`:267`–`:271`）与其余字段赋值一字未动，未新增 `InternalsVisibleTo`、未变 `public` ⇒ 工单 §二 B1"只许这一种提取"这条我核过成立。
- 两枚新判据文件我读了实质面：A 那格（`[Fact]` 第 58 行）断言不等式左边取确切数字、注释写明守 `:203` 不守 `:178`；B 那五枚节点＝第 55 行绝对字面量 `[Fact]`、第 74 行 `[Theory]` 带三行 `InlineData`（三条区分性探针各自钉自己的摘要串）、第 98 行零行为漂移 `[Fact]`（并在注释里自证"这不是覆盖证据"）。§三.4 那句 `xUnit2000` 参数顺序改动我核过＝`WriterId` 是第 40 行的 `const`，改后仍是同一表达式，但**它未经任何重新构建证明**，这一格照 lane 的口径挂在未验证。
- 工作固化：我已把整枚提交归档成 `.local-notes/wave25-1023/out/wave25-code.patch`（18,579 字节）与 `0001-feat-HDS-003-HDS-010-wave25-….patch`，载体即便被回收也不丢字节面；这两份都在 gitignored 目录，不入库、不删。

### 八.4 归位裁决：代码这一把不入主树，押第二十六波

工单 §二 B1 写死一条硬线＝"若第 1 格在突变下不响，就是常绿门，本刀不许提交它"；lane 自己在 §五.3／§五.4 也按同一口径写着 A2 与 B 红证**未取到**、"不许签成已付"。所以：

1. **片 A 那一格与片 B 那五枚节点今天都是未证伪状态**（一次都没在改坏产品码的情况下响过），主树**不接受**这三枚文件的字节面；`git merge --ff-only 24d1b6f` 我扣住不执行。
2. **`1333 ＋ 6 ＝ 1339` 只是新格数申报，不是出口读数**；在册基线照旧＝**1333／executed 1331／failed 0／跳过 2**，本刀一格不动、一字节不动。
3. 归位通道改由第二十六波走＝`docs/dispatch-hds-wave26-20261023.md`：三轮补齐（A2 红证 1 轮、B 红证 1 轮、1 构建＋1 全量收尾）＋把这枚载体提交落进线尾，**红证不响就当场撤掉那枚格、不许提交常绿门**。
4. lane 自报的两处"不合工单字面"我裁定如下并登记：① 载体提交通道（§三.3）＝**认可**，与第二十三波同型，不提交就没有可归位的字节面；② 产品面 3 枚文件而非 §三.5 字面的 2 枚＝**认可**，工单 §二 B1 明文授权"另一枚新文件由你定"，按判据面分文件不扩范围。

### 八.5 这一把新增的产品事实（与它的边界）

- 新增的一件事实：装配器里"`used + cost > total`（该段装得下自身份额、但装不下剩余输入预算）这一项今天没有任何一道判据守着"——不是推演，是 A1 那轮 `712 格 0 响` 的盘上答案。它给第二十二波那本"30 格从没报过警"的账添了一枚具体新格，性质与 `ContextBuilder.cs:39`（W2）同族：**守卫在、判据不在**。
- `:178` 那道 `if (!assembly.WithinUnifiedBudget)` 按 lane 的五行门表＋A1 读数登记为**无可达输入的防御支**，"按名钉 `BudgetInequalityViolated`"这条路在盘上拿不到红；这一格诚实的交付是登记不可达，不是造一枚永远绿的断言去顶那个名（工单 §一 那句原话照录）。**这条不是"不需要守"**，它是一条已知且未闭合的覆盖面欠账。
- 边界：本刀没有跑任何解决方案级构建与全量，片 B 的"提交构造提出来"这一步**只存在于载体提交里**，主树与生产调用点今天都没变；`NarrativeTurnPipeline` 仍零生产调用方，"新链已接线／旧链可删／不进包已做到"三句照旧全部不成立。真实模型、UI、生产库、待标定参数、换人复采照 lane §五 全欠。
