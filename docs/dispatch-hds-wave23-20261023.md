# 工单 HDS-WAVE23-1023｜把新链在一条回合里真正穿起来（测试宿主内端到端），顺带给第二十波那 34 格补六枚换人复采

- 工单 id：`HDS-WAVE23-1023`
- 任务编号：`HDS-010` 第二片的**前置**（新链端到端可跑通）＋`HDS-003`／`HDS-004`／`HDS-005` 的**组合面**；登记处＝`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS-010 行与 §9
- 性质：**动产品代码的整合刀**。新建一枚组合件＋新判据；**不切生产默认路径、不动旧链一字、不碰界面、不落生产库**
- 执行方：冷启动 lane（第二十三号载体）
- 出闸：守门人 2026-10-23 18:4x，作者原令逐字＝「我不懂啊，你干脆能做的一次全做了，直接大跨步开发疯狂挂子智能体就好了，我想快些结束这个项目」＋「我不想思考了，你就继续给我派工单，该提交的提交吧」

## 〇 开工前必读与入场（缺一条就就地停刀登记，不要自扩范围）

1. **先读五份**：`docs/plans/README.md`、`docs/plans/PROJECT_PLAN.md`（§4.2 的 HDS-003／004／005／010 四行＋§9 最新三条 entry）、`docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md`、`docs/agent-engineering-standards.md`（全文，交付形态按 §10 四项）、`docs/legacy-chain-deprecation.md`。正面依据＝`docs/eval/reports/2026-10-23-hds010-legacy-chain-disposition.md` 表 C 片 4 与表 D，加 `docs/eval/reports/2026-10-23-hds010-gap-parity-prototype.md`（第二十波，含守门人 §六）。
2. **入场读数当场贴进报告 §〇**（不许引用本工单的数字）：`date`、`git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD`、`git status --short --branch`。
3. **载体**：`git worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave23-1023/carrier <当场线尾全号>`。**当场取线尾**（`git rev-parse codex/ui-main-window-redesign`），并先 `git rev-list --count HEAD..<线尾>` 核落后格数、非 0 就 `merge --ff-only` 追平再开工。**在册门禁基线现值＝1321／0／2**（分项 Client 108／App 233／Api 280／Core 700、executed 698），取于第二十波归位后的线尾副本——本刀**不动在册基线**，也不许自封。
4. **他人资产**：主树那两枚 `M ` 暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）与所有 `??`＝别人的，不读内容、不改、不提交、不 `stash`／`clean`／`rm`。**共享主树里禁止任何 `git commit`／`cherry-pick`／`merge`／`tag`／`push`／打包／FF `main`**，一切提交只发生在你自己的载体里，归位归守门人。
5. **允许文件面（只有这些）**：`src/Pry.Core/Narrative/` 目录内**新建**文件（建议名 `NarrativeTurnPipeline.cs`，可自决但报告里要点名）；`tests/Pry.Core.Tests/` 目录内**新建**测试文件；你的报告 `docs/eval/reports/2026-10-23-hds010-end-to-end-pipeline-prototype.md`。
6. **禁止（逐条硬线，违反一条即作废本轮）**：
   1. **不改任何既有文件**——包括第二十波那五枚原型、第十六波的 `NarrativeLifecycleCoordinator.cs`、第十七波的 `Storage/` 两枚与 `NarrativeCommitWriter.cs`／`DeliveryLedger.cs`／`OutboxOwnershipRegistry.cs`／`NarrativeDocuments.cs`／`NarrativeContextCompiler.cs`／`NarrativeDraftValidator.cs`／`NarrativeRoundRunner.cs` 的任何成员、条件、阈值或枚举取值。**若组合确实需要既有类型开口子，停下登记为"押接线刀"，不要自己改。**
   2. 不接 `src/Pry.Api`／`src/Pry.App`／`src/Pry.Client`／任何端点／任何界面；不新增 SQLite 依赖之外的东西；不碰 `%LOCALAPPDATA%\PryCompanion`；临时库／临时目录一律系统临时目录下自建并收尾删除。
   3. **不新造数字门槛，也不改既有门槛**。在册零改动清单＝检索 `.7`／`.5`、`2400`、五档超时、`MinimumUsableTokensPerSecond = 12`、`4096`、`DefaultSafetyTokens = 128`、`MinimumInputTokens = 512`。凡本刀需要的节奏／批量／时限参数**一律从入参传入**；仓库里找不到权威值的登记为"待标定（押到标定轮）"并让入参必填。
   4. 不引用旧链四件（`BehaviorPlanner`／`ReplyGenerationRenderer`／`ReplyPlanner`／`ReplyNecessity`）——读旧链代码取机制允许，新类型不得 `using`／构造／转发，并要有一枚**自带阳性探针的源码扫描格**（形状照第二十波 `NarrativeGapParityPrototypeTests.cs:322` 那格，探针只证明"扫描器看得见"，门在不在守要靠 §二 的单枚突变）。
   5. Core 不依赖 Avalonia 或 HTTP；不得提交模型／运行库／数据库／密钥／日志／用户导入内容／无权素材；`licenses/**`、`licenses/THIRD-PARTY-NOTICES.md`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md` 一字不碰。
   6. 不写状态词与结案词，不自封基线；拿不到的红一律写「未取到红」并写清原因（防御纵深／兜底支／住在禁改件里／结构上红不了），**结构上红不了的断言要指出承重断言实际落在哪一行**。
7. **焦点预检（这条会打断作者本人，违反＝本波作废）**：每一次 `dotnet`（含 build／test／任何工程）之前先在主树跑

   ```
   powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1
   ```

   整段输出贴进报告；只有 `VERDICT=BUILD_OK` 才起跑；读到 `HOLD` **就地停刀、不请示、不改预检判据、不绕过它**。`free_memory_gb` 偏低或读数呈 SQLite `disk I/O`／`out of memory` 形态时那一轮读数作废（那是他机器的资源形状，不是断言失败），不动任何进程。
8. **预算（用满即停，不许自扩）**：解决方案级构建 **≤1**、全量 **≤1**、定向 **≤16**（本刀组合判据的反证 ≤10 ＋ 活单② 的换人复采 ≤6）。跑法固定写法＝`dotnet test <工程或解决方案> -c Release [--no-build] --results-directory "<绝对路径>" --logger trx`（短参 `-r` 会把结果拼进各工程 `bin` 导致"零测试仍 exit=0"；不给 `--logger trx` 就没有 TRX；**每轮独立结果目录、不许复用同名 `LogFileName`**）。跑完先数 `*.trx` 再读 `<Counters>`。
9. **报告先建、逐批落盘**（150 轮硬上限）：动第一次 `dotnet` 前把 §〇 写满建文件，之后每完成一批追加一次。交付形态按规范 §10 四项。

## 一 活单①：把一条回合真正穿起来

今天的事实是：`src/Pry.Core/Narrative/` 里有十六枚文件、批次／打断／分类／换形／节奏五枚原型刚落地、协调器与落库面也已落地，但**没有任何一段代码按顺序把它们走完一次**。所以"新链能跑"这句话今天没有凭据。本刀的活＝新建一枚组合件（只做编排，不产新语义、不改任何被组合件的判定），并钉住"这条链真的按顺序接上了"。

要求逐段点名（报告里给一张"衔接表"：上一段输出的**哪个成员**喂给下一段入参的**哪个参数**，接不上的那一格如实写"接不上，押接线刀"）：

| 段 | 用的既有件 | 必须钉住的衔接语义 |
|---|---|---|
| 1 攒批 | `NarrativeInputBuffer` | 同一会话连续多条输入归入**一枚**批次；封顶条件全走入参 |
| 2 分类 | `NarrativeInterruptClassifier` | "接着说／新话题／不可判"三档不折叠，"不可判"独立成档 |
| 3 打断 | `NarrativeInterruption` ＋ `DeliveryLedger` | 只作废尚未送达的段；已送达段不可撤回（`DeliveryLedger` 一字不改） |
| 4 编译 | `NarrativeContextCompiler` | 上下文按契约装配；本刀不得改它的判定 |
| 5 叙事与校验 | `NarrativeRoundRunner`／`NarrativeDraftValidator` | 草稿→校验→终态；拒绝理由可点名 |
| 6 原子提交 | `NarrativeCommitWriter`／`Storage/NarrativeSqliteStore` | 落到**临时目录里的真 SQLite 库**，重启连接后从库里读回判定（不许用内存桩代替） |
| 7 表达换形 | `NarrativeBehaviorExpressionMapping` | 缺决策不默认放行；被场景挡时理由码透传 |
| 8 节奏 | `NarrativeReplyPacing` | 只决定下一段何时可发，不决定内容；可发时刻单调不减 |

判据要求：
- 至少**一枚端到端格**走完 1→8（真临时库、崩溃／半送达矩阵复用第十七波那面既有门，不重造）；
- 上表每一段至少一枚**衔接格**（证明这一段输出被下一段消费，而不是各自单测）；
- 新格若用 `[Theory]`／`[InlineData]`／`MemberData`／抽象基类，必须在报告里点名——本仓 Core 的静态计数与 TRX 计数不等价（差值前例：＋22 抽象基类派生重跑＋3 `MemberData` 行）。

## 二 活单②：给新落的每枚门各响一次，顺带补第二十波的换手

- **本刀新落门**：单枚·单轮·清洁红，十一项字段逐轮齐（①文件:行 ②改前原文 ③改后原文 ④`git diff --stat` ⑤跑法整行 ⑥TRX 文件名 ⑦红格全名＋消息要点＋红点行号 ⑧还原命令 ⑨还原后 `git status --short` 行数 ⑩突变态与还原后 SHA-256 ⑪当轮预检整段）。**改过被守护文件的字节＝它此前所有反证作废**；本刀不改既有文件（见 §〇.6.1），所以这条只约束你自己的新文件。
- **第二十波换人复采（≤6 轮，这是本刀的第二件正活）**：第二十波那 36 格里有 34 格的反证出自判据作者自己那一双手（台账 §9 18:3x 明写）。你要从 `NarrativeGapParityPrototypeTests.cs` 里**自选 6 格**做单枚突变复采，条件：① 五个类型各至少覆盖一枚；② 优先选"这枚突变如果假了，用户可见后果最重"的那格；③ 报告里逐格写清"为什么选它、它守的是 `src/` 哪一行、你放了哪枚单枚突变、红点落在哪一行、同轮其余几格是否全绿"。口径与 §一 相同：**恰好该格红、其余绿**才算换人到手的凭证；拿不到就写"未取到红＋原因"，**不许**为了让它响去改判据。
- 同一轮多格同红要按消息形状与红点行号归因；批次红永远只证明"这几枚里至少一枚能打红这一格"，不得署成某一枚的归属。

## 三 出口读数（缺哪格报哪格，不许留空）

1. 构建 0 警告 0 错误原样；2. 解决方案级全量一次，四份 TRX 的 `<Counters>` 逐份贴，两条跳过按 `<UnitTestResult outcome="NotExecuted">` 点名（`notExecuted` 计数器恒 0 不看）；3. 全量分项与在册 **1321／0／2（Client 108／App 233／Api 280／Core 700、executed 698）** 对账——**等式必须由那四份原始 TRX 当场求和写出来，不许心算、不许预写**；不等就逐格找差、点名差在哪一份文件；4. `scripts/measure-fixed-eval.ps1 -ResultsDirectory <本轮目录>`（带这个参数才跑，否则 throw 而退出码照旧 0）；5. `git diff --check` 干净；6. 收尾载体 `git status --short` ＝ 0 行、`git rev-parse HEAD` 仍是你登记的号；7. `dotnet` 调用次数只认盘上产物（`ls *.trx` 数跑次；`%TEMP%\Microsoft.NET.Workload_*.log` 不作证据）；8. 报告总行数与各节行数当场实测；9. `git diff --stat <线尾>..HEAD -- src tests scripts Pry.slnx Directory.Build.props` 原文（应只见新建文件）。

## 四 不许走的捷径（每条都有前例）

1. 不许以"接线"名义去改既有协调器／落库面／契约文件；2. 不许把节奏／攒批参数写成源码常量凑一个能跑的数；3. 不许把"不可判""缺决策"折叠进某一档来少写一格判据；4. 不许用固定返回值／吞异常／`Assert.Skip`／注释掉任何既有断言让红消失；5. 不许用内存桩冒充 §一 第 6 段的"真库落盘＋重开连接读回"；6. 不许把第二十波那 34 格里你没复采的部分写成"已换手"，也不许把你不复采的格写成"不需要复采"；7. 不许在本刀里顺手做归位／FF／tag／push；8. 不许写"新链已可跑／旧链可删／不进包已做到"——**本刀只在测试宿主里跑通，生产默认路径一字未动，旧链仍在生产与打包面里**。

## 五 仍未验证（报告里必须有一节，逐条点名）

真实模型对照（零读数）、生产接线与切换（组合件零生产调用方）、UI 可见行为（零读数）、生产库（零接触，只碰系统临时目录）、待标定参数的权威值来源、与 HDS-007 到期再叙事的关系。

## 六 归位通道（给守门人，不在本 lane 权限内）

主树共享脏树里不 `cherry-pick`：在隔离载体 `git worktree add --detach` 后提交，再把新文件按字节铺回主树并以 `git commit --only <显式路径>` 提交（新文件先 `git add`，`--only` 不认未跟踪路径），出口用 `git show --stat` 数文件数。归位需一轮构建＋一轮全量，与守门人的复采轮合并起跑；**在册基线的改写权在守门人那一手**。

—— 完 ——
