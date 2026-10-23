# 工单 HDS-WAVE25-1023｜给"统一预算不等式"那枚没人按名钉的自检定形状，再把提交构造的字面占位换成真实哈希

- 工单 id：`HDS-WAVE25-1023`
- 性质：**两片合一、共用一轮构建**。片 A＝判据形状刀（是否动产品码由 A1 那一轮的盘上答案决定）；片 B＝最小接线重构（`src/` 只许动一枚文件的可见性，行为一字不变）
- 任务编号：`HDS-003`（上下文装配器的预算自检）＋`HDS-010` 的 5→6 数据口；登记处＝`docs/plans/PROJECT_PLAN.md` §4.2 那两行＋§9
- 出闸：守门人 2026-10-23 20:1x。作者原令逐字＝「我不想思考了，你就继续给我派工单，该提交的提交吧」＋「你，还是自己起Agent吧」＋「旧的链路丢到废案里面，但是构建时候不要把旧链路打包进去，保存一份旧链路最后可以构建的快照」
- 执行方：冷启动 lane（第二十五号载体）
- **开工前置**：本刀必须在**第二十四波（`HDS-WAVE24-1023`）交回之后**才起跑。那一把是纯复采刀、要占这台机器一整轮构建＋一整轮全量；同一时间两轮 `dotnet` ＝两次弹作者防火墙，违反他的现令。

## 〇 开工前必读与入场（缺一条就就地停刀登记，不要自扩范围）

1. **先读八份**：`docs/plans/README.md`、`docs/plans/PROJECT_PLAN.md`（§4.2 的 HDS-003／HDS-010 两行＋§9 最新三条 entry）、`docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md`（§6 统一预算不等式那节）、`docs/agent-engineering-standards.md`（全文，交付形态按 §10 四项）、`docs/legacy-chain-deprecation.md`、`docs/eval/reports/2026-10-23-hds003-005-cell-evidence-audit.md` 的 **§四 T5 那一行（#49）＋§七.3**（守门人批准另起一把的那格就是本片 A）、`docs/eval/reports/2026-10-23-hds010-end-to-end-pipeline-prototype.md` 的 **§三 衔接表 5→6 那一行＋守门人 §五／§六**（本片 B 的输入），以及**第二十四波那份报告**（若它已交回）。
2. **入场读数当场贴进报告 §〇**（不许引用本工单的数字）：`date`、`git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD`、`git status --short --branch`。
3. **载体**：`git worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave25-1023/carrier <当场线尾全号>`，先 `git rev-list --count HEAD..<线尾>` 核落后格数、非 0 就 `merge --ff-only` 追平。**在册基线＝按入场时线尾当场从上一把报告读到的那个数**（守门人 10-23 19:2x 顶到的是 1333／executed 1331／0／2，分项 Client 108／App 233／Api 280／Core 712、executed 710；若第二十四波之后在册数变了，以变了的那个为起点），本刀会**新增判据格**，所以出口读数应当**高于**起点数、且差值＝本刀新格数，逐格点名。
4. **他人资产**：主树那两枚 `M ` 暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）与全部 `??`＝别人的，不读内容、不改、不提交、不 `stash`／`clean`／`rm`。**共享主树里禁止任何 `git commit`／`cherry-pick`／`merge`／`tag`／`push`／FF `main`**；交付物只落在你自己的载体＋那一份报告文件（报告由守门人归位，你不提交）。
5. **允许文件面（只有这些，超出即作废）**：
   - ① `.local-notes/wave25-1023/`（自建，gitignored）里的仪器与全部 TRX；
   - ② 报告 `docs/eval/reports/2026-10-23-hds003-010-budget-cell-and-commit-factory.md`；
   - ③ 片 A 的判据文件：**新建** `tests/Pry.Core.Tests/NarrativeBudgetInequalityCellTests.cs`（不许开别人的既有文件塞格）；
   - ④ 片 B 的产品文件：**只有** `src/Pry.Core/Narrative/NarrativeRoundRunner.cs`，且**只允许"把 `BuildCommit` 的可见性从 `private` 提成 `internal static`、把 `_writerId` 改成入参"这一种提取**，字段集合、哈希输入序列、`Fail`／`Compile` 的任何判定一字不动。
   - **其余 `src/`、`tests/`、`docs/` 任何文件＝零字节**；特别是 `src/Pry.Core/Narrative/NarrativeContextCompiler.cs`（片 A 的靶子，只许被读、被临时突变，**不许被改掉**）、`NarrativeTurnPipeline.cs`、`docs/narrative-storage-contract.md`。
6. **禁止（逐条硬线，违反一条即作废本轮）**：
   1. 不许为了让 `NarrativeContextCompiler.cs:178` 那枚自检"变得可达"而**改动上游任何一道门**（`:87`／`:110`／`:203`／`:246` 都不许动，包括把比较符放宽、把项删掉、把 `total` 的定义改掉）。删守卫来制造可判红＝改产品行为，本刀禁。
   2. 不许为拿红去放宽／删除／`Assert.Skip`／注释掉任何既有断言，不许改 `[InlineData]` 期望值。拿不到的红一律写「未取到红」＋原因，并写出承重断言实际落在哪一行。
   3. **突变轮一律不许 `--no-build`**（旧 dll＝突变根本没进二进制＝假绿，这正是第二十二波审出来的工单矛盾）。
   4. 片 B 的"等价性"不许当覆盖证据：**两侧都走同一个工厂 ⇒ 工厂怎么改两边一起变 ⇒ 恒等**。要钉就钉**绝对字面量**（落地时当场算出的那枚十六进制哈希，写进测试并在注释里写明它由哪九个字段组成）＋**区分性探针**（只改 `NarrativeProse`、只改一条 `Action.Mode`、只改 `ProcessedThrough` 三个输入各应得到不同哈希）。这一格是本项目已经付过学费的地方（"断言面要在孪生闸的上游"），别重犯。
   5. 不许引用 `.local-notes` 之外的历史 TRX 当本轮凭证；同轮多格红必须按消息形状＋红点行号逐格归因，不许把批次红署成某一枚突变的归属。
   6. 不新增数字门槛，也不改既有门槛（在册零改动清单＝检索 `.7`／`.5`、`2400`、五档超时、`MinimumUsableTokensPerSecond = 12`、`4096`、`DefaultSafetyTokens = 128`、`MinimumInputTokens = 512`；本片另加一条＝`MessageFramingOverhead = 8` 与 `SegmentCount = 8`）；不接 `src/Pry.Api`／`src/Pry.App`／`src/Pry.Client`／任何端点／任何界面；不碰 `%LOCALAPPDATA%\PryCompanion`；`licenses/**`、`licenses/THIRD-PARTY-NOTICES.md`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md` 一字不碰。
   7. 不写状态词与结案词，不自封基线；不许写"旧链可删／不进包已做到／新链已接线"三句中任何一句——本刀交付的那一格只把 5→6 的**构造**接上，宿主调用点仍是零。
7. **焦点预检（这条会打断作者本人，违反＝本波作废）**：每一次 `dotnet`（含 build／test／每一枚定向突变轮）之前先跑

   ```
   powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1
   ```

   整段输出贴进报告；只有 `VERDICT=BUILD_OK` 才起跑；读到 `HOLD` **就地停刀、不请示、不改预检判据、不绕过它**，并在报告写清停在第几步（续跑归下一手）。`free_memory_gb` 偏低或读数呈 SQLite `disk I/O`／`out of memory` 形态时那一轮读数作废（那是他机器的资源形状，不是断言失败），不动任何进程。
8. **预算（用满即停，不许自扩）**：解决方案级构建 **≤1**、解决方案级全量 **≤1**、**定向 ≤8**（A1 一枚＝1 轮；A2 若需要新格的红证＝≤2 轮；B 的绝对字面量与三条区分性探针＝≤3 轮；留 2 轮给形状不符时的复取）。每一枚定向轮自带一次工程级构建，那算定向跑次、不算构建额度。跑法固定写法＝`dotnet test <工程或解决方案> -c Release [--no-build 仅限收尾全量] --results-directory "<绝对路径>" --logger trx`；**每轮独立结果目录、不许复用同名 `LogFileName`**；跑完先数 `*.trx` 再读 `<Counters>`；**dotnet 次数只认盘上产物**。
9. **报告先建、逐批落盘**（150 轮硬上限；10-23 一夜已经有两把栽在这格上）：动第一次 `dotnet` 前把 §〇 写满建文件，之后**每完成一批追加一次并把复采凭证逐轮写全**。**十一项字段是每轮的最低落盘单位**（①文件:行 ②改前原文 ③改后原文 ④`git diff --stat` ⑤跑法整行 ⑥TRX 文件名 ⑦红格全名＋消息要点＋红点行号 ⑧还原命令 ⑨还原后 `git status --short` 行数 ⑩突变态与还原后 SHA-256 ⑪当轮预检整段）。缺 ⑨⑩⑪ 的轮次按第二十二波口径退回、不补签。

## 一 片 A 活单 A1｜先把"这格到底红不了还是没人守"问盘上，不要问工单

第二十二波审计的 T5 那一行（#49）写着：`Compiler_honours_the_unified_budget_inequality_with_exact_numbers` 拆不动——`NarrativeContextCompiler.cs:178` 与 `:443` 两行都拆不出红；第十三波自己登记过「**无独立点名格**；m05 里以副作用理由现形（`BudgetInequalityViolated`），但没有一格是按名钉这枚理由码的格——这是本波自知的判据弱点」。守门人 §七.3 批准"另起一把判据刀补一格按名钉"。**但守门人 10-23 20:0x 读码得出一个必须先验的事实**：`used` 在这枚装配器里只有五个变更点，每一个都在加进去之前被一道门守住——

| 变更点 | 守住它的那一行（守门人当场读过） |
| --- | --- |
| `var used = preludeCost;`（`:103`） | `:87` `if (preludeCost > total)` ⇒ `MandatoryExceedsBudget` |
| Advisory 加入（`:126`） | `:110` `if (cost > budget.AdvisoryTokens \|\| used + cost > total)` ⇒ `ReservedDirectiveDoesNotFit` |
| `AddSection` 返回的 `used + cost`（`:212`） | `:203` `if (cost > cap \|\| used + cost > total)` ⇒ 整段丢弃、`used` 不变 |
| `AddUnitRun` 循环内 `used += cost`（`:254`） | `:246` `if (wallet - cost < 0 \|\| used + cost > total)` ⇒ `break`、`used` 不变 |
| `total` 本身 | `:61` `total = ContextTokens − OutputTokens − SafetyTokens`，`:62` 非正即 `WindowTooSmall` |

⇒ 若 A1 的盘上答案支持，`:178` 那道 `if (!assembly.WithinUnifiedBudget)` 就是一枚**结构上没有可达输入的防御支**（同族先例＝`ContextBuilder.cs:39` 的 W2 兜底支、`NarrativeReplyPacing.cs:91` 的防御支），"补一格按名钉它"这条路**根本拿不到红**，硬写只会造出一枚常绿门。

- **A1＝一枚定向轮，做一次真突变**：在载体里把 `NarrativeContextCompiler.cs:203` 的 `used + cost > total` 这一项去掉（只去掉这一项，`cost > cap` 留着），跑 `NarratorPrototypeTests` 那一类（49 格），**逐格点名**：哪些格当场变红、红在哪一行、消息形状是什么。跑完逐字节还原并核 SHA-256。
- **A1 的两种答案分别对应两种交付，不许混**：
  - **答甲（有既存格响）**：上游那道门今天已经有人守 ⇒ 那 #49 缺的**只是"按名钉这枚理由码"**。此时片 A 的交付改判为：**把"红不了"当场采成证据**——报告里用五张行号（上表）＋A1 的盘上红格名单，写明 `:178` 无可达输入、删掉它零可观测变化，并**明确登记这是一格补不上的账**（不许写"已由 A1 响的那几格代替覆盖"，那几格守的是 `:203`，不是 `:178`）。此路**零产品码、零新格**。
  - **答乙（无任何格响）**：`:203` 的"剩余预算"这一项今天没人守 ⇒ 片 A 补**一格会响的格**（新建文件 `tests/Pry.Core.Tests/NarrativeBudgetInequalityCellTests.cs`），形状必须同时满足：① 载荷构造要让"该段自身份额 `cap` 装得下、但 `used + cost` 超剩余 `total`"（否则响的是 `cost > cap` 那半，不是这一项）；② 断言面**在 `:203` 的上游可观测处**——该段 `Disposition == Dropped`、`DispositionReason == "budget"`、`UsedTokens` 与估算口径一致、整轮 `ReasonCode` 仍是 `Ok`、`WithinUnifiedBudget` 为真且不等式左边取**确切数字**（不是 `Assert.True` 一句带过）；③ 注释里写明这一格守的是 `NarrativeContextCompiler.cs:203`，**并写明它不守 `:178`**（`:178` 仍按答甲登记为不可达）。此路**零产品码、＋1 格**。
- 无论甲乙，报告都要写清一句：**"按名钉 `BudgetInequalityViolated`"这句话在盘上没有可判红的实现路径时，诚实的交付是登记不可达，不是造一枚永远绿的断言去顶这个名**。

## 二 片 B 活单 B1｜5→6 那个口子：把提交构造从 private 提出来，哈希不许再是字面占位

第二十三波报告 §三 衔接表 5→6 那一行（本刀最大的那一格）登记＝盘上唯一的"草稿→提交"构造 `NarrativeRoundRunner.BuildCommit` 是 `private`（`src/Pry.Core/Narrative/NarrativeRoundRunner.cs:260`，调用点在 `:183`），工单禁改既有文件，于是组合件改吃宿主注入的 `Func<NarrativeRoundResult, NarrativeRequest, NarrativeCommit> CommitFactory`（`NarrativeTurnPipeline.cs:82` 声明、`:276` 调用），测试宿主用 `HostCommitFactory(...) with { ContentHash = ... }` 把哈希写成字面量占位（`"sha256:wave23-host-hash"` 一类）。

- **B1 的允许改动＝只此一种**：把 `private NarrativeCommit BuildCommit(NarrativeDraft draft, NarrativeRequest request, DateTimeOffset now)` 提成 `internal static NarrativeCommit BuildCommit(NarrativeDraft draft, NarrativeRequest request, DateTimeOffset now, string writerId)`，调用点 `:183` 改成传 `_writerId`。可见性基础设施已在盘上（`src/Pry.Core/Properties/AssemblyInfo.cs:3` ＝ `InternalsVisibleTo("Pry.Core.Tests")`），**不许新加 `InternalsVisibleTo`、不许新增公共 API、不许把方法变 public**。哈希那九项输入（`:267`–`:271`）与 `NarrativeCommit` 的字段赋值（`:273`–`:281`）逐字节不动。
- **B1 的判据（新格，同一片 A 那个新文件或另一枚新文件，由你定，但要逐格点名）**：
  1. **绝对字面量格**：对一条固定的 draft／request／now／writerId，`BuildCommit` 产出的 `ContentHash` 必须等于**你这一手当场算出并抄进测试的那枚十六进制串**（小写、无分隔），注释写明它由哪九项、用什么分隔符（`\u241f`）、按哪个字段顺序。这一格是**唯一能响的门**——它守的是"字段集合与顺序不得被人偷偷改"。
  2. **三条区分性探针**（可以是一枚 Theory 的三行 `InlineData`，也可以三枚 Fact，逐格点名）：只改 `NarrativeProse`／只把一条 `Action.Mode` 换值／只推进 `ProcessedThrough`，三者各自必须得到**不同**哈希。
  3. **零行为漂移格**：提取前后，`NarrativeRoundRunner` 自己那条正常路径产出的提交（经同一 draft）与工厂直调产出的提交**逐字段同一**——这一格只证明"提取没改语义"，**不许被写成覆盖证据**（两侧同工厂恒等，见 §〇.6.4）。
- **B1 的红证（必须先故意让它红一次）**：把哈希输入序列里任意一项换位置或删掉（只在载体里、只一轮），确认第 1 格当场红、第 2 格那三枚按形状各自红或至少红其一，逐字节还原并核 SHA-256。**若第 1 格在突变下不响，就是常绿门，本刀不许提交它**。
- **本刀明确不交付**（写进报告 §未验证，不许省）：宿主调用点仍为零（`NarrativeTurnPipeline` 依旧没有任何生产调用方）；`Fail`／`Compile`／落库面／发送侧一行不碰；「旧链可删」「构建时不把旧链打进包」两条前置照旧未达成；片 B 新格的**换人复采**押下一把复采刀（写判据的你不能同时是给它取红的第二双手）。

## 三 出口读数（缺哪格报哪格，不许留空）

1. 解决方案级构建 0 警告 0 错误原样（**警告数必须为 0**——第二十三波那把留下的账不许变两回）；2. 全量一次，四份 TRX 的 `<Counters>` 逐份贴文件名，两条跳过按 `<UnitTestResult … outcome="NotExecuted">` 点名（`notExecuted` 计数器恒 0 不看）；3. 对账等式**必须由那四份原始 TRX 当场求和写出来**：`起点数 ＋ 本刀新格数 ＝ 出口数`，新格逐格给文件名＋`[Fact]`／`[Theory]` 所在行号（**先 `grep -c` 再顶账，注释行里的 `[Fact]` 字样不算格**）；4. `scripts/measure-fixed-eval.ps1 -ResultsDirectory <本轮目录>`（不带这个参数它会 throw 而退出码照旧 0）；5. **产品面 `git diff --stat <入场线尾>..HEAD -- src tests scripts Pry.slnx Directory.Build.props` 只许出现两枚文件**：`src/Pry.Core/Narrative/NarrativeRoundRunner.cs` 与片 A／B 的新测试文件；出现第三枚＝作废；6. 载体收尾 `git status --short`＝0 行且 `git rev-parse HEAD` 仍是入场号；7. `dotnet` 次数只认盘上产物（`ls **/*.trx` 数跑次）；8. 报告总行数与各节行数当场实测；9. `git diff --check` 干净；10. **A1 那枚突变的还原凭据必须单列一节**（它动的是靶子文件，读者要能一眼看出靶子今天＝线尾字节）。

## 四 归位通道（给守门人，不在本 lane 权限内）

守门人核四件事：① 片 B 的产品 diff 是否真只碰可见性与调用点（逐行读 `NarrativeRoundRunner.cs` 的那几处）；② 片 A 走了甲还是乙，登记进 §9 与 `docs/full-scope-sprint.md` §3 的措辞必须与 A1 的盘上答案一致——**甲路不许写成"补了一格"，乙路不许写成"不可达"**；③ 第 1 格（绝对字面量）在突变下真红过；④ 在册基线由归位那轮顶账（本刀是新格，改写权在守门人，不在本 lane）。

## 五 仍未验证（报告里必须有一节，逐条点名）

真实模型对照（零读数）；宿主接线与切换（新链组合件零生产调用方）；UI 可见行为（零读数）；生产库（零接触）；待标定参数权威值；旧链删除片与"不进包"那道门；片 B 新格的换人复采；`:178` 防御支若走甲路则永久无人按名钉（这是产品事实，不是本刀偷懒）。

## 六 不许走的捷径（每条都有前例）

1. 不许改上游门（`:87`／`:110`／`:203`／`:246`）来制造可达性；2. 不许 `--no-build` 跑突变；3. 不许用固定返回值／吞异常／`Assert.Skip`／注释掉既有断言；4. **不许拿"两侧恒等"当覆盖证据**（§〇.6.4 那格就是为此而写）；5. 不许把 A1 响的那几格写成"`:178` 已被覆盖"；6. 不许引用他人目录的产物当本轮凭证；7. 不许在本刀里顺手做归位／FF／tag／push；8. 不许动主树那两枚他人暂存项，也不 `stash`／`clean`；9. 不许把片 B 说成"新链已接线"——它只把提交构造从 private 里拿出来，调用点仍是零；10. 不许为省轮次把两枚突变放进同一轮然后各署一格。

—— 完 ——
