# eval-QA-WAVE10-1022｜第十波三件证据债：M-W3 第三人复采＋第七波那枚红的换手＋W2 的零构建可达性推演

- 工单：`docs/dispatch-hds-wave10-20261022.md`（出闸 2026-10-22 22:29，守门人派发）
- 执行方：冷启动 lane（本报告之外不假设任何上下文）
- 载体：`.local-notes/qa-wave10-1022/carrier`，detached HEAD＝`2002210`
- 本报告只写事实与读数；在册基线的改写权不在本报告。

---

## 〇 挪树输出＋三处锚点原文读回＋格账起点

### 0.1 主工作树入场只读测量（未写入主树任何字，唯一写盘＝本报告文件）

`git -C C:/Users/24782/Desktop/Pry_Project status --short --branch` 原样输出：

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

- 两枚他人在册暂存项（`M ` 状态，索引区）：`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`。本 lane **未读、未改、未提交、未 `stash`／`clean`／`checkout`**，全程只出现在上面这条只读 `status` 输出里。
- 五个未跟踪项（`??`）属别人／工具所有，本 lane 未触碰。

`git log --oneline -3`：

```
1bcc9ad docs(wave10): 载体从抽象"线尾号"改成钉死 2002210，并把"起点成立"的判据从文档口径改成代码口径（diff -- src tests 为空）
2002210 docs(plans,sprint,wave10): 出闸那把的 verify-repository 顶成落地后实测值（551→552，涨的一格就是这张工单本身）
a2d9f96 docs(plans,wave10): 第十波工单出闸（21 格之外剩下的三件"只能换一双手"）＋起点锚点按盘上原文自纠两处前案记录
```

`git rev-parse HEAD`（主树）＝`1bcc9ad0a7707bd13bf0437dcea2df2102f5aea3`。

### 0.2 独立载体与"起点成立"的代码判据

挪树命令（逐字照工单 §〇.2）：

```
git -C C:/Users/24782/Desktop/Pry_Project worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave10-1022/carrier 2002210
```

命令输出原样：

```
Preparing worktree (detached HEAD 2002210)
HEAD is now at 2002210 docs(plans,sprint,wave10): 出闸那把的 verify-repository 顶成落地后实测值（551→552，涨的一格就是这张工单本身）
```

载体内两行实测（原样）：

```
$ git rev-parse HEAD
200221093af555ed28114fd37decbb81f027efc9
$ git status --short
（0 行）
```

- 载体 HEAD＝`2002210`（短号）＝`200221093af555ed28114fd37decbb81f027efc9` ⇒ 与工单钉死的号一致。
- `git status --short`＝**0 行** ⇒ 载体入场干净。
- `git status --short --branch` 首行＝`## HEAD (no branch)` ⇒ 确为 detached，未动任何 `refs/heads/*`。

主树线尾在出闸后确实又落了一格纯文档登记刀（`2002210`→`1bcc9ad`），按工单 §〇.2 这不算起点缺陷，判据取代码：

```
$ git -C C:/Users/24782/Desktop/Pry_Project diff 2002210..1bcc9ad -- src tests
（空，0 行）
```

⇒ **起点成立**（照旧跑）。附两条产品码历史计数（本仓库口径：`git log -- <路径>` 的行数是提交数，不是 dotnet 轮数）：

```
$ git log --oneline ab9b3cf..2002210 -- src tests | wc -l   → 0
$ git log --oneline 6b6f549..ab9b3cf -- src tests | wc -l   → 1   （= 4039a50 test(core): DEF-GREENGATE-01 那条恒真断言换成会响的门）
```

另测 `git log --oneline -- tests/Pry.Api.Tests/ContextWindowLoadTests.cs` 只有一格＝`faf8ecf`（该文件自创建未再改）⇒ T1 的红点行号不存在"文件漂移"这一解释，见 §一.7。

`git ls-files | wc -l`：载体＝**552**，主树＝**552**（与工单 §四 的在册 `verify-repository`＝552 一致；本 lane 未新增受跟踪文件，报告文件在 `git status` 里是未跟踪项，不计入已提交线）。

### 0.3 三处锚点：盘上原文逐行读回（行号以载体钉死的 `2002210` 为准）

`src/Pry.Api/Services/ModelPerformancePolicy.cs`（awk 带行号读回 44–50）：

```
44:         var intended = contextSize == source.ContextSize
45:             ? source.MaxOutputTokens
46:             : Math.Min(source.MaxOutputTokens, Math.Max(512, contextSize / 2));
47:         if (!ContextBudget.TryNormalizeWindow(contextSize, intended, out var ctx, out var output, out _))
48:             return source with { ContextSize = contextSize, MaxOutputTokens = 1 };
49:         return source with { ContextSize = ctx, MaxOutputTokens = output };
```

`src/Pry.Core/Narrative/DeliveryLedger.cs`（76–84）：

```
76:                 + $"（带来的是 {segment.OwnershipEpoch}），无权追加投递段。");
77:         }
78:
79:         if (!_commits.TryGetValue(key.CommitId, out var commit))
80:         {
81:             return SegmentOutcome.No(NarrativeContractReason.SegmentCommitUnknown,
82:                 $"提交 {key.CommitId} 没有生效过，不能产生投递段。");
83:         }
```

`src/Pry.Core/Prompting/ContextBuilder.cs`（22–40，含 §〇.3① 要求顺带读回的 `:24`）：

```
23:     /// <summary>实际可用上下文扣除输出保留与安全余量后，可供装配的输入预算。</summary>
24:     public long InputBudget => (long)ContextTokens - OutputTokens - SafetyTokens;
...
33:         contextSize = contextTokens;
34:         var capped = Math.Min(Math.Max(requestedOutputTokens, 0), Math.Max(0, contextTokens / 2));
35:         var ceiling = contextTokens - DefaultSafetyTokens - MinimumInputTokens;
36:         if (ceiling < capped) capped = Math.Max(0, ceiling);
37:         outputTokens = Math.Max(1, capped);
38:         inputBudget = (long)contextTokens - outputTokens - DefaultSafetyTokens;
39:         return contextTokens > 0 && inputBudget >= MinimumInputTokens;
```

同文件常量（`:16`／`:17`）＝`DefaultSafetyTokens = 128`、`MinimumInputTokens = 512`；`ForModelWindow` 的抛出点在 `:45`（`throw new ContextBudgetException(...)` 在 `:46`）。

测试锚点：

- `tests/Pry.Api.Tests/ContextWindowLoadTests.cs`：`:50` 方法签名 `ForModelWindow_agrees_with_the_loader_on_the_default_profile`（W3）；`:52` `var budget = ContextBudget.ForModelWindow(32768, 32768);`；`:53` `var loaded = new ModelPerformancePolicy()`（该语句跨 `:53-54`）；`:56` `Assert.Equal(loaded.MaxOutputTokens, budget.OutputTokens);`；`:57` `Assert.Equal(loaded.ContextSize, budget.ContextTokens);`。W1＝`:19`，W2＝`:33`。
- `tests/Pry.Core.Tests/NarrativeStorageContractTests.cs`：`:327` `Assert.False(unregistered.Accepted);`（在 `:323-329` 那个 `foreach` 里，方法起 `:281`＝`Late_commit_from_a_lost_ownership_epoch_is_rejected_without_overwriting_anything`）；`:270`＝`Segments_cannot_be_appended_for_a_commit_that_never_applied` 的方法签名行，其断言在 `:274`／`:275`。

**逐条核对结论**：工单 §〇.3 表格四行与盘上原文**逐字相同**，无差异需更正。

### 0.4 两处前案记录的当场登记（照工单 §〇.3①②要求，两行原文都读回、不择一采信）

1. **`:24` vs `:34`**：盘上 `ContextBuilder.cs:24` ＝ `public long InputBudget => (long)ContextTokens - OutputTokens - SafetyTokens;`（`ContextBudget` record 的计算属性），`:34` ＝ `var capped = Math.Min(Math.Max(requestedOutputTokens, 0), Math.Max(0, contextTokens / 2));`（`TryNormalizeWindow` 里的半窗夹）。第八波 §〇 更正段把同一枚夹零写成 `:24`，第八波 §十二 的 M-CB1 写成 `:34`。**按第八波续跑报告 §五 字段 ③ 记的 before→after 原文核对**：M-CB1 的 before 文本＝`var capped = Math.Min(Math.Max(requestedOutputTokens, 0), Math.Max(0, contextTokens / 2));`、after＝`var capped = Math.Max(requestedOutputTokens, 0);`，该 before 文本在盘上唯一出现在 `:34`；`:24` 的文本里没有 `Math.Min`／半窗夹，改 `:24` 得不到第八波 §五 记录的那份 diff。⇒ **那次突变的目标行是 `:34`**；`:24` 是那次登记的笔误行号。本 lane 未据此停刀，仅按盘上读数登记。
2. **调用方文件名**：工单 §〇.3② 自报"一页纸与第八波把调用方写成 `ModelProcessPolicy.WithContext`"。载体盘上实测：`WithContext` 在 `src/Pry.Api/Services/ModelPerformancePolicy.cs:39`（私有方法），候选行 `:46`，归一化调用 `:47`，兜底行 `:48`。`ModelProcessPolicy` 这个类名在本仓另有其文件（`src/Pry.Core/Inference/ModelProcessPolicy.cs`，与本案无关）⇒ 本单写的真文件＝`src/Pry.Api/Services/ModelPerformancePolicy.cs`，与盘上一致，无需更正。
3. 第九波 §〇.5 关于第 10 格（`NarrativeCommitWriter.cs:141-142` vs `DeliveryLedger.cs:79` 两条不同产品行）：本波**不重做、不领第二次凭证**（照工单明令）。

### 0.5 格账起点（从盘上取，不做加减）

- 分母 46 格拆法原文：在册已付 **45**、欠 **1**＝W2（`ContextWindowLoadTests.Every_downgrade_candidate_keeps_a_positive_input_budget`），来源＝`docs/plans/PROJECT_PLAN.md` §9 第九波收口那条的原文"格账顶到格"段与第十波工单 §一 T3 背景句。本单三件活**不新增分母**：T1、T2 是给两枚"只由守门人自证"的红换一双手复采；T3 是 W2 的可达性推演。
- 在册读数（本 lane 未验证、仅作为对账目标引用）：总 **1146**／passed **1144**／红 **0**／**2** 条按名跳过的环境项；分项 Client **108**／App **233**／Api **278**／Core **527**（executed **525**＋2 跳过）。
- W3 那一格的既有凭证：`run8-gk-mw3-only`（守门人自证，Api total 278／failed 1）；第七波 `DeliveryLedger.cs:79` 那枚：守门人自证（载体 `6b6f549`，TRX `…_17_45_24.trx`）。本波目标是**由非写判据的手复采**。

---

## 一 T1｜W3 的第三人复采：只放 M-W3 一枚

### 1.1 本轮的定向方式（先说明，免得读账的人按别的口径核）

- 一轮只放 **M-W3 一枚**（`ModelPerformancePolicy.cs:47` 的第二个实参 `intended` → `intended / 4`），M-CB1／M-CB6／M-S1／M-S2 **一律不放**，`ContextBuilder.cs` 全程未改（还原后 sha 复证见 §1.2⑨ 之外的 1.4）。
- 跑法取"单项目整轮"（`Pry.Api.Tests`，278 格）而不是单方法过滤：工单 §一 T1 要求**同一轮里 W1／W2／S1～S3 必须全绿**，这五格分属 `ContextWindowLoadTests` 与 `StickerContentLengthGuardTests`，同轮观察才构成这一枚的正向探针，且与守门人轮（`run8-gk-mw3-only`＝total 278／failed 1）可逐位对账。本层占"按名定向"额度 **1 次**（≤3）。

### 1.2 十一项字段

| 项 | 实测值 |
|---|---|
| ① 载体提交号 | `200221093af555ed28114fd37decbb81f027efc9`（短号 `2002210`，detached，`## HEAD (no branch)`） |
| ② 被改文件:行 | `src/Pry.Api/Services/ModelPerformancePolicy.cs:47`（仅此一枚、仅此一行） |
| ③ 突变原文 before→after | before：`        if (!ContextBudget.TryNormalizeWindow(contextSize, intended, out var ctx, out var output, out _))`　after：`        if (!ContextBudget.TryNormalizeWindow(contextSize, intended / 4, out var ctx, out var output, out _))`（`git diff` 全文见 1.3，逐字取自本轮 diff；`:48` 兜底行与 `:49` 正常返回行一字未动） |
| ④ `git diff --stat` | ` src/Pry.Api/Services/ModelPerformancePolicy.cs \| 2 +-` ⇒ **1 file changed, 1 insertion(+), 1 deletion(-)**（`--numstat`＝`1  1`；`grep -c "intended / 4"`＝1 处改动点，还原后＝0） |
| ⑤ 跑法整行命令 | 构建：`dotnet build tests/Pry.Api.Tests/Pry.Api.Tests.csproj -c Release`（载体目录内，用时 11.77 秒，0 警告 0 错误）　跑测：`dotnet test tests/Pry.Api.Tests/Pry.Api.Tests.csproj -c Release --no-build --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave10-1022\run1-mw3-only" --logger trx` |
| ⑥ TRX 文件名 | `24782_PHYTHLINK_2026-10-22_22_38_42.trx`（跑完 `ls -1 *.trx` 数得 **1 份**，再读 `<Counters>`；控制台另报"失败 1，通过 277，总计 278"） |
| ⑦ 红格用例全名＋消息要点 | `Pry.Api.Tests.ContextWindowLoadTests.ForModelWindow_agrees_with_the_loader_on_the_default_profile`＝**Failed**；`<Counters total="278" executed="278" passed="277" failed="1" error="0" ...>`；消息（TRX `ErrorInfo` 原样）：`Assert.Equal() Failure: Values differ` / `Expected: 8192` / `Actual:   16384`；红点锚＝`ContextWindowLoadTests.cs:line 53`（栈首帧指向测试方法本身，其后是 `MethodBaseInvoker` 两帧）；本轮红点名 **1 个** |
| ⑧ 还原方式 | `git checkout -- src/Pry.Api/Services/ModelPerformancePolicy.cs`（字节级；未用 `Set-Content`、未新建文件、未注释该行） |
| ⑨ 还原前后 SHA-256 | 突变态（还原前）＝`08fa254f6b57bda4a40a4da61ddf0f4f3022e7d3d750294132da618fe071274c`；还原后文件＝`3f18ee19e64175fc2141f00b3ff3b2e787f15f6114469dbb2eea5c77337eaf52`；`git show HEAD:src/Pry.Api/Services/ModelPerformancePolicy.cs` \| `sha256sum`＝`3f18ee19e64175fc2141f00b3ff3b2e787f15f6114469dbb2eea5c77337eaf52` ⇒ **两侧逐字节对平**，且与工单 §一 T1 写的 HEAD 侧目标值逐字相同。入场基线（放突变之前的工作树文件实测）＝同一个 `3f18ee19…`，且与 `git show HEAD:` 侧同值（`DeliveryLedger.cs` 的入场基线＝`020c485c58de3f5d3def74e78fd90fab4e78fe38672cb747132664f34d1b09a7`，同样与 HEAD 侧对平，见 §二 ⑨）⇒ 突变态与干净态可复算 |
| ⑩ 还原后 `git status --short` 行数 | **0 行** |
| ⑪ 结论 | **证明了**：W3 那一格由 `ModelPerformancePolicy.cs:47` 的第二个实参（`intended`→`intended / 4`）**单独**打红，且只打红它——同轮 W1（`Loading_the_default_profile…`）、W2（`Every_downgrade_candidate…`）、S1（`Content_accompanying_a_sticker_is_rejected_over_the_shared_limit(entry: TurnSubmission)`）、S2（同法 `entry: DirectMessage`）、S3（`Both_message_entries_call_the_one_ruler_and_no_second_ceiling_is_declared`）在 TRX 里逐条 `outcome="Passed"`，非 Passed 的只有 W3 一格 ⇒ 归因唯一、不误报（正向探针通过）。**没证明**：W2 的可拆性（本轮 W2 实绿，正是 §三 推演的输入之一）；也不证明 `:48` 兜底行本身有独立判据面——本轮没有碰它。**共用凭证**：W3 这一格与第八波批2（`run5-api-red2`）的那条红共用过同一格，但批2 那条消息是 `ContextBudgetException`（抛出点 `ContextBuilder.cs:46`、测试点 `:52`），按逐值算术属 M-CB1∧M-CB6 的合力，故该格清洁凭证自守门人 `run8-gk-mw3-only` 起由"只放 M-W3 的一枚单轮"提供；本轮＝同一配方的**第三人复采**，不撤销原归属、不重复领第二次的凭证含义。 |

### 1.3 突变态 `git diff` 全文（逐字，供复算）

```
diff --git a/src/Pry.Api/Services/ModelPerformancePolicy.cs b/src/Pry.Api/Services/ModelPerformancePolicy.cs
index b77ed79..56dfbaa 100644
--- a/src/Pry.Api/Services/ModelPerformancePolicy.cs
+++ b/src/Pry.Api/Services/ModelPerformancePolicy.cs
@@ -44,7 +44,7 @@ public sealed class ModelPerformancePolicy
         var intended = contextSize == source.ContextSize
             ? source.MaxOutputTokens
             : Math.Min(source.MaxOutputTokens, Math.Max(512, contextSize / 2));
-        if (!ContextBudget.TryNormalizeWindow(contextSize, intended, out var ctx, out var output, out _))
+        if (!ContextBudget.TryNormalizeWindow(contextSize, intended / 4, out var ctx, out var output, out _))
             return source with { ContextSize = contextSize, MaxOutputTokens = 1 };
         return source with { ContextSize = ctx, MaxOutputTokens = output };
     }
```

### 1.4 与守门人轮（`run8-gk-mw3-only`）的差异登记

| 对账项 | 守门人轮在册 | 本轮实测 | 差异 |
|---|---|---|---|
| 载体 | `ab9b3cf` 系载体（第八波续跑留下的二进制） | `2002210` | 载体号不同；`git diff 2002210..1bcc9ad -- src tests`＝空、`git log ab9b3cf..2002210 -- src tests`＝**0 行** ⇒ `src`／`tests` 两侧逐字相同，不构成产品行漂移 |
| Api `total`／`failed` | 278／1 | 278／1 | 无 |
| 红格名 | `…ForModelWindow_agrees_with_the_loader_on_the_default_profile` | 同 | 无 |
| 消息形状 | `Assert.Equal() Failure: Values differ`／Expected 8192／Actual 16384 | 同（TRX `ErrorInfo` 逐字） | 无 |
| 红点行号 | `ContextWindowLoadTests.cs:53` | `ContextWindowLoadTests.cs:line 53` | 无 |
| 同轮其他格 | W1／W2／S1～S3 全绿 | TRX 逐条 `Passed` | 无 |
| 突变态 sha | `08fa254f…71274c` | `08fa254f6b57bda4a40a4da61ddf0f4f3022e7d3d750294132da618fe071274c` | 无（同一字节状态可复算） |

⇒ **零差异，无需差异复跑**（机动额度未为 T1 动用）。附带如实登记一处仪器细节：`Assert.Equal` 的文本在源文件 `:56`，而 xUnit／PDB 把红点报在 `:53`（`var loaded = new ModelPerformancePolicy()` 那条跨 `:53-54` 语句的起始行）。工单写的锚点＝`:53`，与本轮 TRX 实报逐字相同，故按"锚点相符"登记，不作差异处理；行号语义以 TRX 报出的为准。另附计数供核对：`git log --oneline 6b6f549..ab9b3cf -- src tests`＝**1 行**（`4039a50`，改的是 `NarrativeStorageContractTests.cs` 的断言，不是 `ContextWindowLoadTests.cs`）。

**逐值算术核对（零构建，与实测互证）**：M-W3 下 W3 用例走 `contextSize == source.ContextSize`（32768＝32768）⇒ `intended = source.MaxOutputTokens = 32768` ⇒ 传入 `TryNormalizeWindow` 的第二个实参变 `32768 / 4 = 8192` ⇒ `:34` `capped = Math.Min(8192, Math.Max(0, 16384)) = 8192`、`:35` `ceiling = 32768-128-512 = 32128 ≥ 8192`（`:36` 不触发）、`:37` `output = 8192`、`:38` `inputBudget = 32768-8192-128 = 24448`、`:39` `24448 ≥ 512` ⇒ true ⇒ `:49` 返回 `output = 8192`；而 `ForModelWindow(32768, 32768)` 一侧未受影响 ⇒ `budget.OutputTokens = Math.Min(32768, 16384) = 16384` ⇒ `Assert.Equal(8192, 16384)`。实测 Expected 8192／Actual 16384 与此完全一致，且这一枚只改 `:47` 的调用侧实参、不改 `ContextBuilder` 的尺本身 ⇒ 红只可能出现在"两条路必须一致"的那一格（W3），W1（只看单调档位自身）与 W2（只看候选输入预算符号）不受影响，与实测绿相符。

---

## 二 T2｜第七波那枚"只由自证者拿过"的红的换手复采

### 2.1 定向方式

一轮**只放 M-DL 一枚**（`DeliveryLedger.cs:79` 的查找键 `key.CommitId` → `"commit-1"`），本轮载体里没有任何其他突变（`ModelPerformancePolicy.cs` 已于 §一.2⑧ 逐字节还原并 sha 对平；`ContextBuilder.cs` 全程未碰）。跑法＝按类名定向（`--filter "FullyQualifiedName~NarrativeStorageContractTests"`，该类 30 格），这样同轮既拿到 `:327` 的红，又拿到 `:270` 保持绿的正向探针，还能证明"这一枚没让同类里别的门跟着响"。占"按名定向"额度 **1 次**（累计 2／≤3）。

### 2.2 十一项字段

| 项 | 实测值 |
|---|---|
| ① 载体提交号 | `200221093af555ed28114fd37decbb81f027efc9`（短号 `2002210`） |
| ② 被改文件:行 | `src/Pry.Core/Narrative/DeliveryLedger.cs:79`（`TryAppend` 里"提交未登记"那道前置门；同文本的第二处出现 `:230`（`PreTransition` 里的同名门）**未改**，见 2.3） |
| ③ 突变原文 before→after | before：`        if (!_commits.TryGetValue(key.CommitId, out var commit))`　after：`        if (!_commits.TryGetValue("commit-1", out var commit))` —— 只换查找键这一个表达式；`:81` 的拒绝码 `SegmentCommitUnknown` 与 `:82` 的文案 `$"提交 {key.CommitId} 没有生效过，不能产生投递段。"` 一字未动（`git diff` 全文见 2.3） |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/DeliveryLedger.cs \| 2 +-` ⇒ **1 file changed, 1 insertion(+), 1 deletion(-)**（`--numstat`＝`1  1`；改动点计数 `grep -c 'TryGetValue("commit-1"'`＝1，还原后＝0） |
| ⑤ 跑法整行命令 | 构建：`dotnet build tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release`（3.07 秒，0 警告 0 错误，`Pry.Core` 由突变态源重编译）　跑测：`dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~NarrativeStorageContractTests" --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave10-1022\run2-m-delivery-single" --logger trx` |
| ⑥ TRX 文件名 | `24782_PHYTHLINK_2026-10-22_22_41_57.trx`（`ls -1 *.trx`＝**1 份**；`<Counters total="30" executed="30" passed="29" failed="1" error="0" ... notExecuted="0" .../>`） |
| ⑦ 红格用例全名＋消息要点 | `Pry.Core.Tests.NarrativeStorageContractTests.Late_commit_from_a_lost_ownership_epoch_is_rejected_without_overwriting_anything`＝Failed；消息＝`Assert.False() Failure` / `Expected: False` / `Actual:   True`；红点锚＝`NarrativeStorageContractTests.cs:line 327`（该行原文当场读回＝`            Assert.False(unregistered.Accepted);`）⇒ 响的正是件 1 新写进去的那一条，方法内既有的承重断言 `:300`／`:301`／`:304-307`／`:310-312` 与 `:331` `Assert.Empty(...)` 都先过了。**红点没有落在 `:270`，也没有落在别的行。** |
| ⑧ 还原方式 | `git checkout -- src/Pry.Core/Narrative/DeliveryLedger.cs`（字节级） |
| ⑨ 还原前后 SHA-256 | 突变态＝`5728089506b813fc819321002cdda85114ba3f47af97bacf2c59c8bb651b303f`；还原后文件＝`020c485c58de3f5d3def74e78fd90fab4e78fe38672cb747132664f34d1b09a7`；`git show HEAD:src/Pry.Core/Narrative/DeliveryLedger.cs` \| `sha256sum`＝`020c485c58de3f5d3def74e78fd90fab4e78fe38672cb747132664f34d1b09a7` ⇒ **两侧逐字节对平**，且与第七波 §6 登记的还原目标、与本 lane 入场实测值三者同值 |
| ⑩ 还原后 `git status --short` 行数 | **0 行** |
| ⑪ 结论 | **证明了**：`DEF-GREENGATE-01` 改判后新写进去的那一条（`:323-329` 的 `TryAppend`＋`Assert.False`）在"未登记提交门"被放开后**确实会响**，且响在唯一的目标行；同轮 `Segments_cannot_be_appended_for_a_commit_that_never_applied`（`:270`）**保持绿**，另一条共用该门的 `Fenced_writer_cannot_touch_the_delivery_ledger`（`:252`）与 `A_segment_needs_real_history_evidence_to_be_called_delivered`（`:241`）也绿 ⇒ 这一枚的归属是单因的，不是批次合力。**没证明**：`:230`（`PreTransition` 里形状相同的那道门）是否可拆——本格目标行是 `:79`，`PreTransition` 由状态转换路径守，属另一格账；本 lane 未领、也未顺手改它（工单 §三"多分支修法按分支逐个撤销"在此的适用方式见 2.3）。**共用凭证**：这一枚与第七波 §4.1 的那次红同格（`Late_commit_from_a_lost_ownership_epoch_…` 的 `:327`）；差别见 2.4。 |

### 2.3 突变态 `git diff` 全文（逐字）

```
diff --git a/src/Pry.Core/Narrative/DeliveryLedger.cs b/src/Pry.Core/Narrative/DeliveryLedger.cs
index c816173..37a45a9 100644
--- a/src/Pry.Core/Narrative/DeliveryLedger.cs
+++ b/src/Pry.Core/Narrative/DeliveryLedger.cs
@@ -76,7 +76,7 @@ public sealed class DeliveryLedger
                 + $"（带来的是 {segment.OwnershipEpoch}），无权追加投递段。");
         }
 
-        if (!_commits.TryGetValue(key.CommitId, out var commit))
+        if (!_commits.TryGetValue("commit-1", out var commit))
         {
             return SegmentOutcome.No(NarrativeContractReason.SegmentCommitUnknown,
                 $"提交 {key.CommitId} 没有生效过，不能产生投递段。");
```

如实登记一处**盘上与工单表述不冲突但值得留痕**的形状：`if (!_commits.TryGetValue(key.CommitId, out var commit))` 这一行文本在该文件出现 **2 次**（`:79` 属 `TryAppend`、`:230` 属 `PreTransition`）。按工单 §一 T2 的"只换 `_commits.TryGetValue(key.CommitId, …)` 的查找键"＋锚点 `:79`，本 lane 只改 `:79`（Edit 时用 `:79-82` 的上下文把两处区分开，`grep -n` 复证 `:230` 保持原文）。⇒ 本轮是单改动点、单因轮，不存在"多分支一起放开后再逐支撤销"的情形。

### 2.4 与第七波在册那一轮的差异登记

| 对账项 | 第七波 §4.1 在册 | 本轮 | 差异处理 |
|---|---|---|---|
| 载体 | `6b6f5498ea8d32153d4d61de5bca04a6e1f32715` | `2002210` | `git log 6b6f549..ab9b3cf -- src tests`＝1 行（`4039a50`＝本格判据刀本身），`ab9b3cf..2002210`＝0 行 ⇒ 被测产品行 `:79` 与目标测试行 `:327` 在两树逐字相同，红点可跨载体对账 |
| 跑法 | 四枚合放的一轮（§4.1 字段 ⑤ 自述"同一轮里还带着 M4／M5"；TRX `…_17_45_24.trx`，`Counters total=527 failed=5`） | **单枚单轮**（class 定向，total 30／failed 1） | 本轮把这一枚净化成"清洁凭证"：批次红只能证明"这几枚里至少一枚能打红这一格"，本轮的红只此一枚。不撤销第七波那条归属，只是补一条归属干净的 |
| 红点行号 | `NarrativeStorageContractTests.cs:327` | `:line 327` | 无 |
| 消息形状 | `Assert.False() Failure`／Expected False／Actual True | 同 | 无 |
| `:270` 探针 | 该批实测绿（第七波 §4.1 末段给了机理：那条用例从不提交、`_commits` 空字典，换键同样查不到） | 本轮 `Passed`（TRX 逐条点名） | 无 |

---

## 三 T3｜W2（`Every_downgrade_candidate_keeps_a_positive_input_budget`）：零构建读码列出口，未起定向跑

### 3.1 被断言的那件事与它读的算式（先钉死判据面）

测试原文（`tests/Pry.Api.Tests/ContextWindowLoadTests.cs:33-47`）：fixture 为 `ModelProfile{ ContextSize = 262144, MaxOutputTokens = 262144 }` ＋ `new LlamaComputeDevice("CUDA0", "NVIDIA GeForce RTX 5090", false, 32768, 30000)`；`LlamaComputeDevice` 的形参序（`src/Pry.Core/Inference/LlamaHardwareDetector.cs:6-7`）＝`(Id, Name, IsIntegrated, TotalMemoryMiB, FreeMemoryMiB)` ⇒ `FreeMemoryMiB = 30000`，`ModelPerformancePolicy.Classify` 取 `memory = Free ?? Total = 30000 ≥ 19500` ⇒ **Extreme**（`MaximumInitialContextSize = 262144`，`:85`）。

于是 `CreateCandidates` 产出的候选集**可枚举**（`firstContext = Math.Min(262144, 262144) = 262144`，`:14`；循环表 `:16` 取严格小于它的项）＝

```
262144, 131072, 65536, 32768, 16384, 8192, 4096   （共 7 档，下界 4096）
```

断言（`:43-45`）逐档两件事：`input = candidate.ContextSize - candidate.MaxOutputTokens - ContextBudget.DefaultSafetyTokens > 0`，以及 `candidate.MaxOutputTokens >= 1`。**测试自己用的 `input` 算式与产品行 `ContextBuilder.cs:38` 的 `inputBudget` 是同一个表达式**（`ctx`＝`contextTokens` 时），这一条是本格所有推演的支点。

### 3.2 出口表：这条链上每一个 return 条件（行号原文均在载体 `2002210` 当场读回）

| 出口 | 文件:行 | 原文（判据指向的表达式） | 该出口成立时 W2 看到什么 | W2 可否为红 |
|---|---|---|---|---|
| O1 | `ContextBuilder.cs:33` | `contextSize = contextTokens;`（out 参数唯一写点） | 候选 `ContextSize` 恒＝窗口 `W` ⇒ 测试 `input` ≡ `:38` 的 `inputBudget` | 不可（除非改 `:33` 本身，那是归一化的返回值语义、不属本格） |
| O2 | `ContextBuilder.cs:34` | `var capped = Math.Min(Math.Max(requestedOutputTokens, 0), Math.Max(0, contextTokens / 2));` | 半窗夹：`output ≤ W/2` ⇒ `input ≥ W/2 − 128` | 不可（`W ≥ 4096` ⇒ `input ≥ 1920 > 0`） |
| O3 | `ContextBuilder.cs:35` | `var ceiling = contextTokens - DefaultSafetyTokens - MinimumInputTokens;` | 与 O4 合用给出 `output ≤ W − 640` ⇒ `input ≥ 512` | 不可 |
| O4 | `ContextBuilder.cs:36` | `if (ceiling < capped) capped = Math.Max(0, ceiling);` | 第三道夹子：把 `output` 钉在 `W−640` 以下 | 不可 |
| O5 | `ContextBuilder.cs:37` | `outputTokens = Math.Max(1, capped);` | `MaxOutputTokens ≥ 1` ⇒ 测试第二条断言恒过 | 不可 |
| O6 | `ContextBuilder.cs:38` | `inputBudget = (long)contextTokens - outputTokens - DefaultSafetyTokens;` | 与测试 `input` 同式（`long` vs `int`，见 E9） | 不可 |
| **O7** | **`ContextBuilder.cs:39`** | `return contextTokens > 0 && inputBudget >= MinimumInputTokens;` | **返 true ⇒ `input ≥ 512 > 0`** ⇒ 走 O9 ⇒ W2 绿 | **不可（这恰是判据问题问的那一支）** |
| O8 | `ModelPerformancePolicy.cs:47→:48` | `:47 if (!ContextBudget.TryNormalizeWindow(...))` ／ `:48 return source with { ContextSize = contextSize, MaxOutputTokens = 1 };` | 返 false ⇒ 兜底 `output = 1` ⇒ `input = W − 1 − 128 = W − 129` | 不可（候选下界 4096 ⇒ `input ≥ 3967 > 0`） |
| O9 | `ModelPerformancePolicy.cs:49` | `return source with { ContextSize = ctx, MaxOutputTokens = output };` | 正常返回，`(ctx, output)` 即 O7 判过 true 的那一对 | 不可（见 O7） |
| O10 | `ModelPerformancePolicy.cs:44-46` | `intended` 两支：`source.MaxOutputTokens` ／ `Math.Min(source.MaxOutputTokens, Math.Max(512, contextSize / 2))` | 任何 `intended` 值都被 O2／O4 吸收、由 O7 裁决 ⇒ 落 O7 或 O8 两支之一 | 不可 |
| O11 | `ContextBuilder.cs:45-46`（`ForModelWindow` 抛 `ContextBudgetException`） | `if (!TryNormalizeWindow(...)) throw new ContextBudgetException(...)` | **不在 W2 的通路上**（W2 只走 `CreateCandidates`）；由 C6／W3 那一侧与 `ContextBuilder.cs:97` 的装配路消费 | 与本格无关 |
| O12 | `ConfigurationApplicationService.cs:625-628` | 配置保存侧 `!TryNormalizeWindow(ctx, maxOut, ...)` ⇒ `ApiValidationException` | 同样不在 W2 通路上（那是 A／C 组的面） | 与本格无关 |

### 3.3 对判据问题的直接回答（只读算式，不猜数字）

问：**是否存在一组输入，使 `:39` 返 true（调用方不走兜底）同时候选输入预算非正？**

答：**不存在。** 证明是两行恒等式：

1. `:33` 把 out 参数 `contextSize` 写成 `contextTokens`，函数体内没有第二处写点（O1）⇒ 非兜底返回时 `candidate.ContextSize = W`、`candidate.MaxOutputTokens = output`；
2. 测试算式 `input = candidate.ContextSize − candidate.MaxOutputTokens − 128 = W − output − 128`，与 `:38` 的 `inputBudget = (long)W − output − 128` 是同式（溢出条件见 E9）；而 `:39` 返 true 的定义就是 `inputBudget ≥ MinimumInputTokens = 512` ⇒ `input ≥ 512 > 0`。⇒ 与"输入预算非正"矛盾。∎

补一条同向事实：`MinimumInputTokens`（`:17`＝512）与 `DefaultSafetyTokens`（`:16`＝128）在 `:35`／`:39`／测试 `:43` 三处共用，所以"门"和"断言"读的是同一把尺——这正是这一格拆不动的结构原因，不是运气。

### 3.4 逐枚配方的排除（含第八波两种实测配方的逐值复算，与实测互证）

`R = intended`，`W ∈ {262144, 131072, 65536, 32768, 16384, 8192, 4096}`；干净代码下 `R = 262144`（仅 `W = 262144` 一支）或 `R = W/2`（其余支）。

| 配方 | 逐值算术（取最不利档 `W = 262144` 与最小档 `W = 4096`） | `:39` | W2 | 实测对照 |
|---|---|---|---|---|
| 干净（无突变） | `W=262144`：`capped=min(262144,131072)=131072`、`ceiling=261504`、`output=131072`、`input=130944`；`W=4096`：`output=2048`、`input=1920` | true（全 7 档） | 绿 | 收尾那一轮全量（§四）再核 |
| M-CB1（拆 `:34` 半窗夹） | `W=262144`：`capped=262144`、`:36` 夹到 `261504`、`input=262144−261504−128=512 ≥ 512` ⇒ true；`W=4096`：`R=2048` ⇒ `capped=2048`、`ceiling=3456` 不触发、`input=1920` | true | 绿 | 第八波 §五 run4（`run4-api-red`）W2＝`Passed` |
| M-CB6（拆 `:35` 的 `- MinimumInputTokens`） | `ceiling=W−128`；`:34` 仍给 `output ≤ W/2` ⇒ `input ≥ W/2−128`（`W=4096` ⇒ 1920） | true | 绿 | 第八波只在批2 与之合拆，见下 |
| M-CB1 ∧ M-CB6 | `W=262144`：`capped=262144`、`ceiling=262016` ⇒ `output=262016`、`input=0` ⇒ **false** ⇒ 兜底 `output=1` ⇒ `input=262015 > 0`；`W=4096`：`R=2048 ≤ 3968` ⇒ `output=2048`、`input=1920` ⇒ true ⇒ 正 | 混合（首档 false 走兜底） | 绿 | 第八波 §六 run5（`run5-api-red2`）W2＝`Passed` |
| 拆 `:36`（第三道夹子）单放 | `:34` 仍给 `output ≤ W/2` ⇒ `input ≥ 1920` | true | 绿 | 未跑（推演排除） |
| 拆 `:36` ∧ `:34` ∧ `:35`（三夹全拆） | `W=262144`：`output=262144`、`input=−128` ⇒ **false** ⇒ 兜底 ⇒ `input=262015 > 0`；其余档 `R=W/2` ⇒ `input=W/2−128 > 0` | 混合 | 绿 | 未跑（推演排除；要红必须连 O8 一起拆） |
| 拆调用方 `:46`（候选公式，例如换成 `source.MaxOutputTokens`） | `W=4096`、`R=262144` ⇒ `:34` 夹到 `2048` ⇒ `input=1920`；任何 `R` 都被 O2／O4 吸收 | true | 绿 | 未跑（推演排除） |
| 拆 `:39`（把门放宽成 `return contextTokens > 0`）单放 | 干净夹子下 `output ≤ ceiling = W−640` ⇒ `input ≥ 512` 仍成立 ⇒ 无差异；要让它响必须同时拆 `:36` ⇒ **≥2 枚合放**，且被破坏的是"小窗口必须被拒"那一格（`ContextBudgetTests.cs:76-81`）与 C 组 | — | 绿（单放） | 未跑：本格目标行不含 `:39`，且多枚合放拿不到单因凭证 |
| 拆 `:48`（去掉兜底）单放 | 干净代码下该 fixture 的 7 档 `:39` **全为 true**（见第一行）⇒ `:48` 对本格**不可达** ⇒ 等价突变，零可观察差异 | true | 绿 | 未跑 |

⇒ **没找到可达配方。** 被排除的出口是：**E1＝O7／`:39` 返 true 支**（true ⇒ 输入预算必 ≥ 512，恒等式矛盾）；**E2＝O8／`:48` 兜底支**（false ⇒ `input = W − 129 ≥ 3967`，因候选下界 4096）；**E3＝O2／O4 两道夹子的任一单独拆除**（各自独立给出 `input ≥ 1920` 或 `≥ 512`）；**E4＝三夹同拆**（把 `:39` 打成 false，落回 E2）；**E5＝调用方 `:46` 的任何取值**（被夹子吸收）；**E6＝O1（`ctx ≠ W`）**（需改 `:33`，不属本格）；**E7＝int／long 溢出使测试式与 `:38` 分家**（需 `|值| ≥ 2^31`，候选表与 tier 上界把 `W`、`output` 封在 262144 内）；**E8＝把候选窗口压到 ≤ 128 让兜底也非正**（`ModelPerformancePolicy.cs:16` 的表下界 4096 与 `GpuPerformanceTier` 的 4096 地板，二者在工单 §六 不许动的数值清单内，本 lane 未碰）；**E9＝`ForModelWindow`／配置保存侧（O11／O12）**（不在 W2 通路上）；**E10＝改 `MinimumInputTokens`／`DefaultSafetyTokens` 常量**（尺的常量被 C 组、W1、W3、`ConfigurationApplicationService:626` 共用，拆它打红的是别格，且属"动数值"）；**E11＝改测试 fixture**（那是换判据不是拆这一格，禁）；**E12＝单拆 `:39`**（干净夹子下无可观察差异，要响必须合放 ≥2 枚 ⇒ 批次红，给不出这一格的清洁凭证）。

### 3.5 十一项字段在本格的缺失申明（照第七波纪律：缺哪一项明写缺）

① 载体提交号＝`2002210`（有）；②～⑩ **全部不适用**——本格**未放任何突变、未起任何 `dotnet`**，故无被改行、无 `git diff --stat`、无跑法、无 TRX、无红格用例、无还原与哈希对平（这两项只在实际放过突变时才存在）、`git status --short` 仍为 **0 行**（见 §四 入场状态）；⑪ 结论见 3.6。缺的原因＝工单 §一 T3 的顺序硬规矩：先零构建列出口，**只有推出具体可达配方才起定向跑**；3.4 的推演没有推出配方 ⇒ 机动额度 **0／1 未动用**。

### 3.6 结论取向（够不着 ≠ 不需要拆）

- 这一格的状态＝**够不着（没找到可达配方）**，不是"这一格不需要拆"：W2 的判据面（每个降档候选输入预算 > 0）被三层各自独立充分的守卫罩着——`:34` 半窗夹、`:36` ceiling 夹、`:39` 返回门＋调用方 `:48` 兜底；任取一层拆掉，另一层仍把 `input` 压在正数区间，两层同拆时 `:39` 直接把调用方送进兜底、兜底给出的 `input = W − 129` 依然为正（候选下界 4096）。
- 与既有登记的关系：第八波两种实测配方（单 M-CB1、M-CB1∧M-CB6）都报 W2 `Passed`，本轮的逐值复算与这两次实测**逐位对上**（见 3.4 第 2、4 行），并在其上把"其余出口"穷举掉；本轮**没有**重跑任何配方、没有为凑红追跑、没有放宽任何断言。
- 仍未被证明的：这一格是否真的"无独立可观察差异"只在 `W2 fixture`（`262144／Extreme`）这一条输入形状上成立。若要给本格领红，可动的不是拆夹子，而是**换判据面**（例如为"兜底路径本身"补一条能被断言的格，那属新增判据的立项，须由守门人／作者定，不属本单，本 lane 不擅自提出为已完成工作）。

---

## 四 收尾那一轮：四分项与顶账（`run3-full`）

跑法整行（载体根目录）：

```
dotnet build Pry.slnx -c Release          （解决方案级构建，本单唯一一次；用时 6.84 秒，0 警告 0 错误；起跑前载体 git status --short＝0 行）
dotnet test Pry.slnx -c Release --no-build --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave10-1022\run3-full" --logger trx
```

`ls -1 *.trx` 数得 **4 份**后才读 `<Counters>`（工单 §三：没有 TRX 就等于没跑过）：

| 分项 | TRX 文件名 | `<Counters>` 原样读数 | 与在册对账 |
|---|---|---|---|
| Client | `24782_PHYTHLINK_2026-10-22_22_46_20.trx` | total 108／executed 108／passed 108／failed 0／error 0／notExecuted 0 | ＝108 ✓ |
| App | `24782_PHYTHLINK_2026-10-22_22_46_21.trx` | total 233／executed 233／passed 233／failed 0／error 0／notExecuted 0 | ＝233 ✓ |
| Api | `24782_PHYTHLINK_2026-10-22_22_46_19.trx` | total 278／executed 278／passed 278／failed 0／error 0／notExecuted 0 | ＝278 ✓ |
| Core | `24782_PHYTHLINK_2026-10-22_22_46_19[1].trx` | total 527／executed 525／passed 525／failed 0／error 0／notExecuted 0（**另有 2 条 `outcome="NotExecuted"` 的结果条目**，见下） | ＝527（executed 525＋2 按名跳过）✓ |

- 两条跳过按名点名（与在册同名，不算红）：`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`、`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`。如实登记仪器形状：Core 那份 TRX 的 `Counters` 里 `notExecuted="0"`，而结果条目里有 2 条 `outcome="NotExecuted"`（控制台报"已跳过: 2"）——本单照盘抄两个读数，不做取舍。
- 总账等式：**1146**（总）＝ 108＋233＋278＋527；**1144**（绿）＝ 108＋233＋278＋525；红 **0**；跳过 **2**。与工单 §四／§〇.5 引用的在册（1146／1144／0／2）逐位相同 ⇒ 本单收尾轮**不产生任何需要改写在册基线的差异**（改写权在守门人，本节不写状态词）。
- 收尾轮的复绿作用：本单两枚突变（`ModelPerformancePolicy.cs:47`、`DeliveryLedger.cs:79`）在本轮对应的格全部 `Passed`——`ContextWindowLoadTests.Loading_the_default_profile…`／`…Every_downgrade_candidate…`／`…ForModelWindow_agrees_with_the_loader_on_the_default_profile` 三条逐条 `Passed`；`StickerContentLengthGuardTests` 的 7 条（含 S1／S2／S3 与 `The_shared_ruler_*`／`Plain_text_*`×2／`Sticker_message_under_the_limit_*`）逐条 `Passed`；`NarrativeStorageContractTests.Late_commit_from_a_lost_ownership_epoch_is_rejected_without_overwriting_anything`（`:327` 那一格）与 `Segments_cannot_be_appended_for_a_commit_that_never_applied`（`:270`）逐条 `Passed`；`ContextBudgetTests.The_raw_equal_output_and_context_window_yields_a_negative_input_budget` 亦 `Passed` ⇒ 两枚突变没有以任何形式残留在二进制或源码里（源码侧另有 §一⑨／§二⑨ 的逐字节对平）。
- 顶账：`verify-repository` 读数＝**552**（收尾时在载体与主树各实测一次 `git ls-files | wc -l`＝552／552，与工单 §四 写的在册 552 同值）。本单未新增受跟踪文件（报告文件在主树里是 `??` 未跟踪项，见 §六.7），故 552 不因本 lane 变；守门人在本单运行期间又落了 1 格纯文档刀（`1bcc9ad`→`2403bea`），`git diff 2002210..2403bea -- src tests`＝**0 行**、跟踪文件条数仍 552 ⇒ 起点判据到收尾仍成立（§〇.2 的"代码判据"口径）。
- 判据条数：本单零 `tests/` 改动（收尾载体 `git status --short`＝**0 行**，两枚还原 sha 与 HEAD 侧逐字节对平）⇒ `[Fact]`／`[Theory]` 条数无变化，等式无需书写。盘上未改动状态下的读数（供守门人对账，非门槛）：`grep -rho "[Fact]" tests --include=*.cs`＝**862**、`[Theory]`＝**62**。

---

## 五 预算终账与逐次焦点预检原文

### 5.1 预算终账（硬顶 vs 实耗）

| 额度 | 硬顶 | 实耗 | 明细 |
|---|---|---|---|
| 解决方案级构建 | 1 | **1** | `dotnet build Pry.slnx -c Release`（22:46 前，6.84 秒） |
| 全量 | 1 | **1** | `dotnet test Pry.slnx -c Release --no-build …`（4 份 TRX，1146／1144／0／2） |
| 按名定向 | ≤3 | **2** | ① T1：`dotnet test tests/Pry.Api.Tests/… --no-build`（total 278／failed 1）；② T2：`dotnet test tests/Pry.Core.Tests/… --filter "FullyQualifiedName~NarrativeStorageContractTests"`（total 30／failed 1） |
| 机动 | 1 | **0** | T3 推演未得出可达配方 ⇒ 不起跑（工单 §一 T3 顺序硬规矩）；T1 与守门人轮零差异 ⇒ 无需复跑 |
| 单工程构建（另记不另算额度） | — | **2** | ① `dotnet build tests/Pry.Api.Tests/Pry.Api.Tests.csproj -c Release`（11.77 秒，含首次还原 5 个工程）；② `dotnet build tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release`（3.07 秒） |
| `dotnet` 调用总数 | — | 6 | 上述 1＋1＋2＋2＝6 次，**每一次之前各跑一次焦点预检**（见 5.2） |
| 焦点预检 | 每次 dotnet 前 | **7** | 6 次对应 6 次 dotnet；pf1 是放第一枚突变之前多跑的一次（只增不减，无绕过） |

**未超支。** T1 用 1／定向、T2 用 1／定向；构建 1、全量 1 各用满；机动与定向各余 1 与 1。

### 5.2 逐次预检整段原文（v6；7 次，全部 `VERDICT=BUILD_OK`，无一次 HOLD，故无停刀）

说明一条读数局限：脚本经我的控制台捕获时，窗口标题里的非 ASCII 字符渲染为 `?`（代码页转换所致，非本 lane 改写）；`===` 行、`style=`／`rect=`／`covers=`／`borderless=`、`gpu_util=`、`free_memory_gb=`、`VERDICT=` 等承重字段为原样。原始捕获同时留在 `.local-notes/qa-wave10-1022/pf1.txt … pf7.txt`。

**pf1（第一枚突变之前，未紧跟 dotnet）**

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: msedge pid=37192 class=Chrome_WidgetWin_1 style=0x17CF0000 rect=-1928,96,1936x1048 screen=1920x1080 covers=True borderless=False title=?????????... - ??   RuanCang.Net ??? 3 ??? - ?? 
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=6% used=2361MB
free_memory_gb=9.9
VERDICT=BUILD_OK
```

**pf2 → 之前跑 `dotnet build tests/Pry.Api.Tests`（T1 的单工程构建）**

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: msedge pid=37192 class=Chrome_WidgetWin_1 style=0x17CF0000 rect=-1928,96,1936x1048 screen=1920x1080 covers=True borderless=False title=Pr 2026_?????? ????-????? ??? 3 ??? - ?? - Micro
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=12% used=2359MB
free_memory_gb=9.5
VERDICT=BUILD_OK
```

**pf3 → 之前跑 T1 那轮 `dotnet test`（`run1-mw3-only`）**

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: BaiduNetdiskUnite pid=53172 class=Chrome_WidgetWin_1 style=0x14C70000 rect=633,209,1293x974 screen=2560x1440 covers=False borderless=False title=???? ? ????
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=0% used=2616MB
free_memory_gb=7.5
VERDICT=BUILD_OK
```

**pf4 → 之前跑 `dotnet build tests/Pry.Core.Tests`（T2 的单工程构建）**

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=294,196,1069x871 screen=2560x1440 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=8% used=2535MB
free_memory_gb=8.9
VERDICT=BUILD_OK
```

**pf5 → 之前跑 T2 那轮 `dotnet test`（`run2-m-delivery-single`）**

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=294,196,1069x871 screen=2560x1440 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=9% used=2526MB
free_memory_gb=8.5
VERDICT=BUILD_OK
```

**pf6 → 之前跑解决方案级构建**

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: YoudaoDict pid=56944 class=YodaoMainWndClass style=0x960FC000 rect=1261,358,1050x904 screen=2560x1440 covers=False borderless=False title=??????
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=10% used=2548MB
free_memory_gb=8.4
VERDICT=BUILD_OK
```

**pf7 → 之前跑全量那一轮（`run3-full`）**

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=294,196,1069x871 screen=2560x1440 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=8% used=2542MB
free_memory_gb=7.3
VERDICT=BUILD_OK
```

`free_memory_gb` 七次为 9.9／9.5／7.5／8.9／8.5／8.4／7.3，**全部 ≥ 6** ⇒ 本轮无 WARN；无 SQLite `disk I/O`／`out of memory` 形状 ⇒ 读数不作废。

---

## 六 偏差自报（哪条纪律差点没守住、哪里与本单有出入）

1. **追加 §二 时误覆盖 §一.4 末段（本 lane 自己的写盘事故，已恢复）**：用 Edit 在 §一.4 表格后接 §二 时，`old_string` 取的是该节末段那一句，导致"零差异＋仪器细节＋log 计数"那段被替换掉；当场从文件里核对发现缺段，随即补回并顺手加了 T1 的逐值算术段。⇒ 最终文本完整（本报告 §一.4 有该段），但这属"增量落盘"纪律下的一次自伤，如实登记，不掩饰。
2. **预检多跑了一次**：pf1 在放第一枚突变之前跑，并非紧跟 `dotnet`。口径是"每次 dotnet 前必跑"，多跑不减损判据，登记以免被误读为计数错。
3. **T1 的"定向"口径**：本 lane 按单项目整轮（`Pry.Api.Tests`，278 格）取证据，不是 `--filter` 单方法。理由与逐位对账需要写在 §1.1／§1.2⑤。若守门人的账本把"按名定向"严格定义为 `--filter` 轮，则本波定向计数为 **1（T2）＋1 待归类（T1）**，请由守门人定归类，本 lane 不自改在册、也不为此重跑一轮（机动额度还在，但重跑不会增加任何判据）。
4. **T2 的定向口径**：用 `--filter "FullyQualifiedName~NarrativeStorageContractTests"`（类级 30 格），不是单方法。这样同轮同时拿到 `:327` 红与 `:270` 绿，并证明同类其余 28 格不受该枚影响。属"按名定向"的一种，登记备核。
5. **与前案记录的三处出入（均按盘上读数登记，未涂改本单、未停刀）**：① 第八波 §〇 更正段把夹零写成 `ContextBuilder.cs:24`，盘上 `:24` 是 `InputBudget` 计算属性、`:34` 才是 M-CB1 的 before 文本所在（§〇.4.1）；② 一页纸与第八波把调用方写成 `ModelProcessPolicy.WithContext`，真文件＝`src/Pry.Api/Services/ModelPerformancePolicy.cs`（§〇.4.2）；③ 工单 §一 T2 称第七波那枚为"守门人自证"，而第七波报告 §4.1 字段 ⑤ 自述那一轮"同一轮里还带着 M4／M5"（四枚合放的批次轮，TRX `…_17_45_24.trx`，`total 527／failed 5`）⇒ 本 lane 按工单"这一枚必须单独一轮"的要求做成单枚单轮，并在 §2.4 登记这一形状差，不撤销第七波那条归属。
6. **红线自检（逐条给结论）**：主工作树唯一写盘＝本报告（见 6.7）；零 `git add`／`commit`／`tag`／`push`／打包；未动 `refs/heads/*` 与主树分支指针（主树 HEAD 今夜由守门人自己推进：`1bcc9ad`→`2403bea`，与本 lane 无关）；他人两枚暂存项未读未改未提交、未 `stash`／`clean`／`checkout`（`checkout` 只在**载体**里用于还原我自己的两枚突变）；未碰作者任何进程；未把 `%LOCALAPPDATA%/PryCompanion` 当测试载体（连只读探查都没做）；未动 `.5` 检索地板、`2400`、五档超时、`MinimumUsableTokensPerSecond = 12`、`4096` 地板、`docs/resource-limit-ledger.md` 数值、`MinimumInputTokens = 512`／`DefaultSafetyTokens = 128`、以及 1146／0／2 这类门槛读数；未放宽／删除／注释任何既有断言；`licenses/**`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md` 一字未碰；预检脚本未改。
7. **交付物状态**：`git -C C:/Users/24782/Desktop/Pry_Project status --short --branch` 在写完本报告后新增且仅新增一行 `?? docs/eval/reports/2026-10-22-qa-wave10-three-evidence-debts.md`（其余 7 行入场即有，归属别人）。归位（跟踪、提交、状态登记）由守门人做，本 lane 写完即停。
8. **留在盘上的证据（未回收，归权属守门人）**：载体 `.local-notes/qa-wave10-1022/carrier`（detached `2002210`，收尾 `git status --short`＝0 行，`git worktree list` 仍列它）；`run1-mw3-only/`（1 份 TRX）、`run2-m-delivery-single/`（1 份）、`run3-full/`（4 份）＝**TRX 共 6 份**；`pf1.txt … pf7.txt`。本 lane 未执行 `git worktree remove`／`prune`。
9. **未验证面（照实申明，不虚构）**：本单全部证据限于自动化测试与零构建读码；未起过模型进程、未在真机跑推理、未验证任何 UI 行为；T3 的结论是**数学排除＋与两次既有实测互证**，不是"跑过并且绿"，因此它给的是"够不着"，不是"不需要拆"，也不构成 W2 那一格的付账。


---

## 七 守门人复证节（2026-10-22 22:5x（守门人 `date` 读到 22:54 时收口），lane 原文一字未改，本节独立追加）

追加人＝守门人。本节只写**我自己从盘上重数出来的东西**，不复述上面任何自述；口径＝不采信自述。

### 7.1 重数方式

只读脚本 `.local-notes/wave10-trx-check-1022.js`（不进任何目录写盘）逐份解析 `<UnitTestResult outcome>` 与 `<Counters>`，跑在守门人进程里，输入＝lane 留在 `.local-notes/qa-wave10-1022/` 下的 6 份原始 TRX。产物清点：`run1-mw3-only`＝1 份、`run2-m-delivery-single`＝1 份、`run3-full`＝4 份，与报告 §四 声称的份数一致；`pf1.txt`～`pf7.txt`＝7 份。

### 7.2 逐格复证（与报告对上＝✓，对不上一律登记）

| 复证项 | 我从盘上读到的 | 报告声称 | 结论 |
|---|---|---|---|
| run1 计数器 | total 278／passed 277／failed **1**／notExecuted 0 | 278／1 | ✓ |
| run1 唯一 Failed | `Pry.Api.Tests.ContextWindowLoadTests.ForModelWindow_agrees_with_the_loader_on_the_default_profile`，栈 `ContextWindowLoadTests.cs:line 53`，`ErrorInfo` 内 `Expected: 8192`／`16384` 各出现 2 次 | 同 | ✓ |
| run1 正向探针 | 同轮 `Every_downgrade_candidate_keeps_a_positive_input_budget`＝**Passed**、`Loading_the_default_profile_at_full_context_yields_a_positive_input_budget`＝**Passed** | W1／W2 全绿 | ✓（这一枚没有把 W2 带响） |
| run2 计数器 | total 30／passed 29／failed **1** | 30／1 | ✓ |
| run2 唯一 Failed | `…Late_commit_from_a_lost_ownership_epoch_is_rejected_without_overwriting_anything`，栈 `NarrativeStorageContractTests.cs:line 327` | `:327` | ✓（不是 `:270`） |
| run2 正向探针 | `Segments_cannot_be_appended_for_a_commit_that_never_applied`＝**Passed**；`Late_commit_after_conversation_deletion_cannot_revive_the_scope`＝Passed | `:270` 保持绿 | ✓ 工单 §T2 第 3 条在第二双手上同样成立 |
| run3-full 四分项 | 108／233／278／527（executed 525）＝总 **1146**、绿 **1144**、红 **0**；两条 NotExecuted 按名点名＝`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`、`CoreTests.Configured_sense_voice_model_can_run_offline` | 同 | ✓ **与在册基线逐位相同 ⇒ 基线不改写** |
| 载体起点 | `git -C …/carrier rev-parse --short HEAD`＝`2002210`、`git status --short`＝**0 行** | 同 | ✓ |
| 代码漂移 | `git diff 2002210..2403bea -- src tests`＝**0 行**（守门人期间落的四把全是文档刀） | 同 | ✓ 起点判据按"代码不漂"成立 |
| 三枚还原指纹 | 载体内 HEAD 侧 vs 磁盘侧 `sha256sum`：`ModelPerformancePolicy.cs` `3f18ee19e641…`＝`3f18ee19e641…`；`DeliveryLedger.cs` `020c485c58de…`＝`020c485c58de…`；`ContextBuilder.cs` `c5da1e3e3277…`＝`c5da1e3e3277…`（本波只动前两枚，第三枚是工单 §〇.3 要求的读回锚点，逐字节未动） | 报告 §1／§2 的两枚对平 | ✓ 逐字节还原成立 |
| 焦点预检 | 7 份 `pf*.txt` 全部 `VERDICT=BUILD_OK`；前台依次是 `msedge`×2、`BaiduNetdiskUnite`、`QQ`×3、`YoudaoDict` | 7 次全放行 | ✓ 无 HOLD、无只留结论的一次 |

### 7.3 两条本轮顺手得到的仪器事实（进 §9 与长期记忆）

- **v6 的"绿半"在他真机上又自然复证了 7 次**：Edge／百度网盘／QQ／有道词典这类**普通应用**持焦点时 v6 一律 `BUILD_OK`，没有一次误停。这与我 22:19 用合成窗口测的那一半方向一致，但这一批是真实应用，价值更高。残留风险不变（提权／跨用户的最大化窗口可能因"路径读不出"误 HOLD）。
- **xUnit 的行号是断言调用点，不是断言文本所在行**：`Assert.Equal` 的字面在源文件 `:56`，TRX 报 `:line 53`。将来引用红点锚必须写 TRX 的那一行，不能拿源码里那一句的行号当凭证。

### 7.4 我对本单的更正（错在守门人的工单，不在执行方）

1. **T2 的凭证形状我写错了**：工单 §一 T2 说这一枚是"守门人自证"的**单枚**清洁轮。盘上原文＝第七波报告 §4.1：字段④ 写「本批四枚合起来＝4 files changed, 4 insertions(+), 4 deletions(+)」，字段⑤ 写「同一轮里还带着 M4／M5」⇒ 那一枚的红来自**批次轮**，而批次轮只能证明"这几枚里至少一枚能打红这一格"。执行方按"单枚单轮"重做并拿到同一条红，这一格自此才升级为清洁凭证；"自证"这个定性仍然成立（第七波报告 执行方＝独立冷启动 lane，写判据与拿红是同一双手），但**归属我按盘上原文更正为"那条 lane 的批次轮自证"，不是守门人**。
2. **派单模板要加一条**：引用前案凭证时必须把那一格的**字段⑤（跑法）读回**，不能只看红点行号——把批次轮写成单枚轮，会让下一刀以为清洁度已经具备。已写进用户长期记忆。

### 7.5 我未复算的部分（如实申明，不替 lane 背书的三样）

- **T3 那张 12 出口表我只做两点抽查**：① `ContextBuilder.cs:16`＝`DefaultSafetyTokens = 128`、`:17`＝`MinimumInputTokens = 512` 当场读回；② 兜底分支恒等式独立复算＝`input = W − 1 − 128 = W − 129`，降档候选下界 4096 ⇒ `input ≥ 3967 > 0`，与报告一致；③ W2 测试原文 `ContextWindowLoadTests.cs:33-47` 的 fixture（`262144／262144`）与两条断言形状当场读回。**我没有逐档重算 7 档 × 8 配方的全表**。
- **因此我不签"结构上永远够不着"**：这一格的证据是"在这一条 fixture（`262144／Extreme`、RTX 5090 32768 显存档）的输入形状下不存在可达配方"。lane 自己在 §3.6 也把这局限写清了，我照抄登记，不放大。
- **W2 在拆法原文账上仍记"欠"**：分母 46／已付 45／欠 1 不变，欠的那一格从"没配方"改成"**已有 12 出口表＋恒等式证明，仍无实测红**"。够不着是交付，不是结案——这条口径不由我或执行方改写。

### 7.6 归位与残留

本报告以未跟踪文件入库（`git add` 由守门人做，只提本报告一份）；lane 名下零 `git add`／`commit`／`tag`／`push`／打包，主工作树除本报告外无其笔迹。载体 `.local-notes/qa-wave10-1022/carrier` 与 6 份 TRX、7 份预检原文**留在盘上未回收**（可复算性优先；`.local-notes/` 不受跟踪，不进仓库）。顶账：本刀新增一份受跟踪文件 ⇒ `verify-repository` **552 → 553**；`[Fact]` 条数变化 **0**；**在册基线照旧 1146／0／2**。
