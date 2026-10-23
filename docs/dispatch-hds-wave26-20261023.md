# 工单 HDS-WAVE26-1023｜把第二十五波留下的三格欠账补齐：两枚红证＋一轮构建与全量，并把那枚载体提交落进线尾

- 工单 id：`HDS-WAVE26-1023`
- 性质：**续跑刀**（第三轮＝收尾）。产品码与判据字节**已由第二十五波写完**，本刀不新增功能、不新增判据、不改一个字节，只做两件盘上欠着的事：① 给那 6 枚新格补"改坏产品码会不会响"的红证；② 补那一轮解决方案级构建与全量，让在册基线有得顶。
- 任务编号：`HDS-003`（装配器预算不等式那一格）＋`HDS-010` 的 5→6 提交构造；登记处＝`docs/plans/PROJECT_PLAN.md` §4.2 那两行＋§9
- 出闸：守门人 2026-10-23 21:2x。作者原令逐字＝「我不想思考了，你就继续给我派工单，该提交的提交吧」＋「你，还是自己起Agent吧」＋「下一次不要再三构建触发那个防火墙烦我打游戏」
- 执行方：冷启动 lane（第二十六号载体）
- **起跑前置**：守门人 21:0x 实测焦点窗＝`helldivers2 pid=53960 class=stingray_window covers=True`、`VERDICT=HOLD`、`free_memory_gb=4` ⇒ 本刀**归位轮零 `dotnet`**，代码面还没进主树。你按 §〇.7 那条有界重试规则自己判何时能起跑。

## 〇 开工前必读与入场（缺一条就就地停刀登记，不要自扩范围）

1. **先读七份**：`docs/plans/README.md`、`docs/plans/PROJECT_PLAN.md`（§4.2 的 HDS-003／HDS-010 两行＋§9 最新三条 entry）、`docs/agent-engineering-standards.md`（全文，交付形态按 §10 四项）、`docs/dispatch-hds-wave25-20261023.md`（上一刀的判据来源，尤其 §二 B1 那条"不响就不许提交"硬线与 §〇.6.4"两侧恒等不算覆盖证据"）、**第二十五波那份报告的 §三／§五／§六／§八**（`docs/eval/reports/2026-10-23-hds003-010-budget-cell-and-commit-factory.md`；§六 就是给你的续跑配方，§八 是守门人裁决：那 6 枚格今天＝未证伪、代码未入主树、基线不动）、`docs/eval/reports/2026-10-23-hds003-005-second-hand-recount-prototype.md` 的 §四（同一台机器上"每轮十一项"长什么样，照那个颗粒度写）、`docs/legacy-chain-deprecation.md`。
2. **入场读数当场贴进报告 §〇**（不许引用本工单的数字）：`date`、`git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD`、`git status --short --branch` 原样、`git worktree list`、`git log --oneline -3`。
3. **载体**：`git worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave26-1023/carrier 24d1b6f8b0a4a3d199dcab832e15facc84a16a01`。
   - 这一枚提交＝第二十五波在自己的载体里写好的交付面（父＝`a804a53`＝守门人第二十四波归位），**三枚文件＝1 产品（`src/Pry.Core/Narrative/NarrativeRoundRunner.cs`，只碰可见性＋入参＋调用点）＋2 新判据（`tests/Pry.Core.Tests/NarrativeBudgetInequalityCellTests.cs` 121 行、`NarrativeCommitFactoryTests.cs` 179 行）**，`304 insertions(+), 3 deletions(-)`。
   - 入场后先自证三件事并把读数贴进 §〇：`git rev-parse HEAD`＝上面那个全号；`git status --short`＝**0 行**；`sha256sum src/Pry.Core/Narrative/NarrativeContextCompiler.cs` 应＝`86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842`（靶子文件＝线尾字节，上一把已还原）。
   - 万一载体不见了（不该发生，字节另有备份）：`git apply C:/Users/24782/Desktop/Pry_Project/.local-notes/wave25-1023/out/wave25-code.patch` 到入场线尾 `a804a53` 的副本上，再逐文件核 `git diff --stat`＝上面那三枚。**不许把 patch 内容重新手打一遍**。
4. **在册基线起点＝当场从第二十五波报告 §八.4 读到的那个数**（守门人 10-23 顶到的是 **1333／executed 1331／failed 0／跳过 2**，分项 Client 108／App 233／Api 280／Core total 712、executed 710）。本刀**新格数＝6**（A 1 枚 `[Fact]`＋B 5 枚节点：1 枚绝对字面量 `[Fact]`、1 枚 `[Theory]` 的三行 `InlineData`、1 枚零行为漂移 `[Fact]`），所以出口读数应当＝**起点＋6**，并且这个等式的右端**必须由你收尾那四份原始 TRX 当场求和取出来**，不许从本工单抄。
5. **他人资产**：主树那两枚 `M ` 暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）与全部 `??`＝别人的，不读内容、不改、不提交、不 `stash`／`clean`／`rm`。**共享主树里禁止任何 `git commit`／`cherry-pick`／`merge`／`tag`／`push`／FF `main`／`git apply` 到主树**；交付物只落在你自己的载体＋那一份报告文件（代码归位由守门人做，你不做）。
6. **允许文件面（只有这些，超出即作废）**：
   - ① `.local-notes/wave26-1023/`（自建，gitignored）里的仪器与全部 TRX；
   - ② 报告 `docs/eval/reports/2026-10-23-hds003-010-red-proof-and-final-gate.md`（主树里唯一允许新建/写入的文件，不提交、不 `git add`）；
   - ③ **产品与判据字节＝零改动**。你只许临时改坏、跑一轮、`git checkout --` 逐字节还原；跑完每一轮的还原后 SHA 必须等于 §〇.3 那个基线值。
   - 其余 `src/`／`tests/`／`docs/`／`scripts/` 任何文件＝零字节持久化改动。
7. **焦点预检（这条会打断作者本人，违反＝本波作废）**：每一次 `dotnet`（含 build／test／每一枚突变轮）之前先跑

   ```
   powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1
   ```

   整段输出贴进报告并归档到 `.local-notes/wave26-1023/out/preflight-<ID>.txt`。只有 `VERDICT=BUILD_OK` 才起跑。读到 `HOLD`：**不许起跑、不请示、不改预检判据、不绕过、不拿"就这一次"当例外**；先把零 `dotnet` 的活做完（写 §〇、摆载体、写驱动脚本、读码、把上一刀的 §六 配方逐字抄进你报告的续跑节），然后**可以重跑预检，最多累计 8 次、相邻两次之间不短于 5 分钟**（这不算绕过：每一次都如实贴出、每一次之后才决定跑不跑 `dotnet`）。8 次之后仍 `HOLD` ⇒ **带着"本刀零 `dotnet` 起跑"的交付回来，那是合法交付**，报告里写清跑到第几步、下一手从哪一行接着跑。`free_memory_gb` 偏低或读数呈 SQLite `disk I/O`／`out of memory` 形态时那一轮读数作废（那是他机器的资源形状，不是断言失败），不动任何进程。
8. **预算（用满即停，不许自扩）**：解决方案级构建 **≤1**、解决方案级全量 **≤1**、**定向 ≤3**（R3 一枚、R4 一枚、留 1 枚给形状不符时的复取）。突变轮**一律不许 `--no-build`**（只有收尾那轮全量允许）。跑法固定写法＝`dotnet test <工程或解决方案> -c Release [--no-build 仅收尾全量] --results-directory "<绝对路径>" --logger trx`；**每轮独立结果目录、不许复用**；跑完先数 `*.trx` 再读 `<Counters>`；**dotnet 次数只认盘上产物**。
9. **报告先建、逐轮落盘**（150 轮硬上限；10-23 一夜已经三把栽在这格上）：动第一次 `dotnet` 前把 §〇 写满建文件，之后**每完成一轮立刻把该轮十一项字段追加进报告再跑下一轮**。十一项＝①文件:行 ②改前原文 ③改后原文 ④`git diff --stat` ⑤跑法整行 ⑥TRX 文件名 ⑦红格全名＋消息要点＋红点行号 ⑧还原命令 ⑨还原后 `git status --short` 行数 ⑩突变态与还原后 SHA-256 ⑪当轮预检整段。缺 ⑨⑩⑪ 的轮次按第二十二波口径退回、不补签。

## 一 R3｜片 A 那格的红证（一枚定向轮）

上一把 A1 的答案是**答乙**（删掉 `NarrativeContextCompiler.cs:203` 的 `used + cost > total` 半项，整个 `Pry.Core.Tests` 712 格 0 响），于是补了 `NarrativeBudgetInequalityCellTests`。但这枚新格**从没在突变下响过**——它今天可能是一枚常绿门。

- 突变＝把 `src/Pry.Core/Narrative/NarrativeContextCompiler.cs:203` 的 `used + cost > total` 半项删掉（只删这一项，`cost > cap` 留着），改法与第二十五波 §一.1／§三.2 逐字节同一：`if (cost > cap || used + cost > total)` → `if (cost > cap)`。
- 跑法＝`dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave26-1023/runs/r3-compiler203-newcell-red" --logger trx`。
- 判据＝**恰 1 枚红**＝`Pry.Core.Tests.NarrativeBudgetInequalityCellTests.Compiler_drops_a_whole_section_that_fits_its_own_share_but_not_the_remaining_input_budget`（该文件第 58 行那格），其余节点全绿；本轮 `total` 应为 **718**（712 既存＋6 新格），除这枚红与两条在册环境跳过之外都应 `passed`。**红点行号与消息要点从 TRX 的 `outcome="Failed"` 节点当场取，不许用上一刀 §三.2 那句推断顶**（那是设计说明，不是读数）。
- 若这一格**不响**：如实写「未取到红」＋那一格实际落在哪一行＋**不许提交它**的结论，并在报告里点名"这一格是常绿门"。不许为了让它响去改别的门（`:87`／`:110`／`:203` 的 `cost > cap` 半项／`:246` 一律不许动）、不许改 `[InlineData]` 期望值、不许放宽或删掉任何既有断言。
- 跑完 `git checkout -- src/Pry.Core/Narrative/NarrativeContextCompiler.cs`，还原后 SHA 必须＝`86c301604dc1…d9842`，`git status --short` 里该文件消失（＝0 行对该文件而言）。

## 二 R4｜片 B 那五枚节点的红证（一枚定向轮，本刀最关键的一格）

工单 §二 B1 与 §〇.6.4 的硬线：**两侧同工厂恒等不算覆盖证据**；唯一能响的门＝钉死绝对摘要字面量那几枚。上一把只跑到"当场算出的摘要与测试里的字面量相同"（R2＝718 全绿），这证的是仪器没算错，**不证门在守**。

- 突变＝在 `src/Pry.Core/Narrative/NarrativeRoundRunner.cs` 的哈希输入数组里删掉第⑨项（＝第 272 行那个 `string.Join(",", draft.Intents.Select(i => i.IntentId))`，只去掉数组里这一个成员，`NarrativeCommit` 的字段赋值一字不动、九项之外的顺序一字不动）。
- 跑法＝`dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave26-1023/runs/r4-hash-sequence-mutated" --logger trx`。
- 判据（逐格点名，不许只报个数）＝片 B 那 4 枚字面量节点应当响：第 55 行 `[Fact]`（`BuildCommit_hashes_the_nine_inputs_into_the_absolute_digest`）＋第 74 行 `[Theory]` 的第 75／76／77 行三枚 `InlineData`；第 98 行那枚零行为漂移 `[Fact]` 里那条 `Assert.Equal(BaseHash, direct.ContentHash)`（约第 117 行）**也应当响**——它钉的是同一枚绝对字面量，所以这一格在"突变面＝哈希输入集合"上不是恒等门，这一句要在报告里说清它响的是哪一行、与 §〇.6.4 那条"两侧同工厂恒等"为什么不冲突。
- **硬线（不许打折）**：若第 55 行那枚 `[Fact]` 在突变下**不响**＝那枚门是常绿的 ⇒ **当场把那一格从文件里撤掉**（撤掉＝删测试节点，不是放宽它、不是 `Assert.Skip`、不是注释掉），然后在报告里登记"这一格设计失败、已撤、:272 的九项集合今天仍无人守"。**宁可交回 5 枚有红证的格，也不许交 6 枚里混一枚没证过的**。
- 跑完 `git checkout -- src/Pry.Core/Narrative/NarrativeRoundRunner.cs`，还原后 SHA 必须＝`92fbce48c7948105479a6ca3fe75697dc24201c4f4d120b7ecc086729c353570`。
- 若 R3 与 R4 都在**同一轮**里想合跑＝**禁止**（工单 §六.10：不许为省轮次把两枚突变放进同一轮然后各署一格）。一枚一轮。

## 三 R5｜收尾一轮（1 构建＋1 全量，落在"将来线尾"的那份字节上）

- 前提：载体回到 `24d1b6f` 的干净面（`git status --short`＝0 行）；此时载体字节与"守门人把这枚提交落进线尾之后"的主树字节**等价**，所以这一轮的四份 TRX 就是归位轮的顶账凭据。跑之前用 `git rev-parse HEAD`＋`git status --short | wc -l` 两行自证。
- 构建＝`dotnet build Pry.slnx -c Release`。**警告数必须为 0**：上一把 §三.4 那条 `xUnit2000` 参数顺序改过之后从没重新构建证明过，这一轮就是它的证明；若还打印任何警告，把警告原文贴进报告、**不许改判据去消警告**，也不许把它读成"0 警告"。
- 全量＝`dotnet test Pry.slnx -c Release --no-build --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave26-1023/full" --logger trx`（结果目录只认长参绝对路径）。跑完**先 `ls *.trx` 数到 4 枚**再逐份读 `<Counters>`，四份文件名与各自 `total/executed/passed/failed` 逐份贴出；两条跳过按 `<UnitTestResult outcome="NotExecuted">` 点名（`notExecuted` 计数器恒 0，不看）。
- 独立复核＝`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/measure-fixed-eval.ps1 -ResultsDirectory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave26-1023/full"`（不带这个参数它会 `throw` 而退出码照旧 0）。
- 对账等式必须由那四份当场求和写出来：`起点 ＋ 6 ＝ 出口`；若 R4 那格被撤掉，则右端相应少 1、并在等式旁边点名是哪一枚。**本刀不许自封基线**，等式归你算，改写权在守门人。
- 产品面自证＝`git diff --stat a804a5371c3d726ea97ddb2dd8a4d735ad3f3122..HEAD -- src tests scripts Pry.slnx Directory.Build.props` 只许出现那**三枚**文件（1 产品＋2 判据）；`git diff --check` 干净；`git status --short`＝0 行。

## 四 归位通道（给守门人，不在本 lane 权限内）

守门人核四件事：① R3／R4 的红格归属是不是**逐格点名**到 TRX 的 `outcome="Failed"` 节点（批次红不许署成单枚突变的归属）；② 片 B 那枚绝对字面量格**真红过**没有——没红过就不许进主树；③ R5 的四份 TRX 求和与 `measure-fixed-eval` 的读数是否自洽，在册基线由这一把顶上去；④ 那份 `xUnit2000` 修复是否在这一轮拿到"0 警告"的实测。代码进主树由守门人做（`git merge --ff-only 24d1b6f` 或其后续号），**你不碰主树的 git 写操作**。

## 五 仍未验证（报告里必须有一节，逐条点名）

真实模型对照（零读数）；宿主接线与切换（`NarrativeTurnPipeline` 零生产调用方，`BuildCommit` 提出 `private` 也不等于接上）；UI 可见行为（零读数）；生产库（零接触）；待标定参数权威值；旧链删除片与"构建时不把旧链打进包"那道门；**这 6 枚新格的换人复采**（写判据的那只手不能同时是第二双手，押下一把复采刀）；`:178` 那道防御支按名钉拿不到红这条已登记为不可达，不许被读成"不需要守"。三句话照旧不许出现在任何页面：新链已接线／旧链可删／不进包已做到。

## 六 不许走的捷径（每条都有前例）

1. 不许改上游门（`NarrativeContextCompiler.cs` 的 `:87`／`:110`／`:203` 的 `cost > cap` 半项／`:246`）去制造可达性；2. 突变轮不许 `--no-build`；3. 不许固定返回值／吞异常／`Assert.Skip`／注释掉既有断言／改 `[InlineData]` 期望值；4. 不许拿"两侧恒等"当覆盖证据；5. 不许把两轮突变合进一轮；6. 不许引用 `.local-notes` 之外的历史 TRX 当本轮凭证；7. 不许在本刀里顺手做归位／FF／tag／push／改 `main`；8. 不许动主树那两枚他人暂存项，也不 `stash`／`clean`／`rm`；9. 不许把片 B 说成"新链已接线"；10. 不许为凑绿灯把没响过的格留在文件里；11. 不新增数字门槛，也不改既有门槛（在册零改动清单＝检索 `.7`／`.5`、`2400`、五档超时、`MinimumUsableTokensPerSecond = 12`、`4096`、`DefaultSafetyTokens = 128`、`MinimumInputTokens = 512`、`MessageFramingOverhead = 8`、`SegmentCount = 8`）；不接 `src/Pry.Api`／`src/Pry.App`／`src/Pry.Client`／任何端点／任何界面；不碰 `%LOCALAPPDATA%\PryCompanion`；`licenses/**`、`licenses/THIRD-PARTY-NOTICES.md`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md` 一字不碰。

—— 完 ——
