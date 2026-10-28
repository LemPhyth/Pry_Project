# 工单 HDS-WAVE40-1029｜旧欠的第一片续跑＝波 13 的 B 类剩 9 格（M8′ 四枚补签＋m07–m10 五枚带门复采，判据字节全程不动、零落地、零顶账）

- 工单 id：`HDS-WAVE40-1029`
- 性质：**证据刀·换手复采（第三十九波的续跑）**。不是新原型、不是接线、不是新判据。
- 任务编号：无新编号；登记处＝`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS-003／HDS-010 行与 §9 最新 entry；本刀出处＝`docs/eval/reports/2026-10-29-hds13-bclass-second-hand-recount.md`（第三十九波）§三.8X 与守门人 §九.4／§九.5。
- 出闸：守门人 2026-10-29 02:4x。作者原令逐字＝「我不想思考了，你就继续给我派工单，该提交的提交吧」＋「你，还是自己起Agent吧」＋「继续工作，不要让我拍板啥的，告诉我现在是什么情况就行」＋「下一次不要再三构建触发那个防火墙烦我打游戏」＋「游戏基本都是全屏应用，看当前桌面焦点是不是全屏应用就行」
- 执行方：冷启动 lane（载体＝沿用第三十三波那棵，**不新建 worktree**）
- **这一刀对作者可见的东西＝零**：净新增判据格 0、产品字节终态净写入 0、界面与对话里什么都不会变。
- 上一把的账（已由守门人从盘上裁完，本刀据此起跑）：九轮里八枚有效轮各拿到恰预期清洁红＝**10 枚格签下换手＋红证**（波 13 的 #1–6、#13–15、#22）；M8 一轮实测 5 枚红（预期 4，多出的那枚名与帧已由执行方在起跑前的独立复算逐字符预判＝`Provider_side_cancellation_with_a_live_host_token_is_named_timeout_not_failure`@851，帧 `:865`）⇒ 整轮作废、四枚（#24–27）未签。**本刀第一件事就是把那四枚补回来，第二件事是把 m07–m10 那五枚（#7–11）带门跑掉。**

## 〇 开工前必读与入场（缺一条就就地停刀登记，不要自扩范围）

1. **先读九份**：`docs/plans/README.md`、`docs/plans/PROJECT_PLAN.md`（§4.2 的 HDS-003／HDS-010 行＋§9 最新一条 entry）、`docs/agent-engineering-standards.md`（全文，交付形态按 §10 四项）、**`docs/eval/reports/2026-10-29-hds13-bclass-second-hand-recount.md` 全文（上一把十一项写法＋§三.0R 那五件独立复算的成品形状＋守门人 §九全部裁决——本刀的配方、仪器口径、连坐面清单全部从那里派生）**、`docs/eval/reports/2026-10-23-hds003-005-cell-evidence-audit.md` §三.1（逐格凭证表，本刀要跑的是 #7–11 与 #24–27 那九行）与 §四.1、`docs/eval/reports/2026-10-23-hds003-005-second-hand-recount-prototype.md` L158、`docs/eval/reports/2026-10-23-hds003-narrator-prototype.md` §三.7–§三.10 与 §三.14（m07–m10 与 m14 的原始配方出处）、判据本体 `tests/Pry.Core.Tests/NarratorPrototypeTests.cs`（**989 行、54 枚方法、56 枚格**）与两枚被改坏的产品文件 `src/Pry.Core/Narrative/NarrativeContextCompiler.cs`（m07–m10）／`NarrativeRoundRunner.cs`（M8′，只读改动行邻域，逐枚核对 §三.0 的"改前整行"）；`.local-notes/wave40-1029/mutation-prep/`（守门人预合成的**五枚**突变文本）与 `.local-notes/wave40-1029/gk40-prep.js`（并排自证原文：每枚先验 OLD 恰出现 1 次且不跨行，再打印改前／改后整行、行号、净括号偏移、全文件括号收支、行数变化＝0）与 `.local-notes/wave40-1029/out/gk40-prep.txt`。
2. **入场读数当场贴进报告 §〇**（不许引用本工单的数字）：`date`、`git rev-parse HEAD`、`git status --short --branch` 原样、`git worktree list`、`git log --oneline -3`。
3. **载体＝沿用第三十三波那棵** `.local-notes/qa-wave33-1028/carrier`（基点 `fd93ebe`）。入场自证五件：① `rev-parse HEAD`；② `status --short`＝**0 行**；③ `log --oneline fd93ebe..HEAD`＝**2 行**；④ `diff --stat fd93ebe HEAD -- src tests scripts Pry.slnx Directory.Build.props`＝**6 files changed, 822 insertions(+), 0 deletions**；⑤ 主树侧 0 行式子 `git -C <main> diff --stat 51f149d $(git -C <main> rev-parse HEAD) -- src tests scripts Pry.slnx Directory.Build.props`＝**0 行**（右端当场取，**不许在本工单里预写 SHA**）。⑤成立⇒载体树与主线归位后的产品／判据字节逐字符相同。
4. **六枚文件入场 SHA-256 基准**：当场 `sha256sum` 三侧各取一次（主树工作副本＝载体工作副本＝`git show 51f149d:<path>` 的 blob），三侧逐字符相同才起跑。守门人 10-29 02:3x 实测（lane 必须自己重测，不得引用）：`NarrativeContextCompiler.cs`＝`86c30160…1d9842`；`NarratorContracts.cs`＝`3a33a815…2a9f8c`；`NarrativeRoundRunner.cs`＝`961fb49b…ed37e9`；判据 `NarratorPrototypeTests.cs`＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`；`NarrativeCommitFactoryTests.cs`＝`fcee4611…7cea0b`；`NarrativeBudgetInequalityCellTests.cs`＝`192bbaec…3f7686`。**判据那枚全程必须等于 `4f1b2f6e…`**（本刀五轮一枚都不写判据，见 §一.7）。
5. **在册基线起点＝当场从波 39 报告 §四 读到的那个数**（1406／executed 1404／failed 0／跳过 2，波 34／36／37／38／39 已五次同值复采）。本刀**净新增格数＝0**，F 轮出口读数应当**仍是 1406**：读到别的数先怀疑树没还原干净，不改判据、不改基线、照实登记并作废那一轮。
6. **本刀的坐标系**：旧欠现值＝**65 格**＝波 13 余 37（47−10）＋波 20 余 28（出处＝波 39 报告守门人 §九.3 那句求和式，不许从本单抄）。本刀最多收 9 枚（M8′ 的四枚＋m07–m10 的五枚）；收不完就照实报，不许把没收的说成收了。
7. **他人资产与 git 红线**：主树那两枚 `M ` 暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）与全部 `??`＝别人的，不读内容、不改、不提交、不 `stash`／`clean`／`rm`。**共享主树里禁止任何 `git commit`／`add`／`checkout`／`reset`／`cherry-pick`／`merge`／`tag`／`push`／`git apply`／`stash`／`clean`／`rm`**；写只许落在载体里（五轮临时覆盖、逐轮按字节还原）＋你自己那一份报告文件。**本刀归位是空转**：终态净写入 0 字节⇒无代码刀、`main` 不因本刀挪。
8. **焦点预检**：每一次 `dotnet`（含每一枚突变轮与收尾全量）之前先跑

   ```
   powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1
   ```

   整段输出贴进报告并归档 `.local-notes/wave40-1029/out/preflight-<ID>.txt`，**编号从 P106 起**（P1–P105 已被前十三把用掉）。只有 `VERDICT=BUILD_OK` 才起跑。读到 `HOLD`：不起跑、不请示、不改判据、不绕过；先把零 `dotnet` 的活做完（§〇 全部、§三.0R 的并排自证与**逐轮点名表**、仪器自证、守卫实证），然后可重跑预检——**HOLD 之后重跑最多累计 16 次、相邻两次不短于 5 分钟**（额度按刀计）。16 次后仍 `HOLD` ⇒ 带着"零 `dotnet` 起跑"或"只跑到第 N 轮"回来，**那是合法交付**，收尾全量不跑＝基线不移动，不是短处。`free_memory_gb` 偏低或读数呈 SQLite `disk I/O`／`out of memory` 形态时那一轮读数作废（他机器资源形状，非断言失败），**不动任何进程**。

## 一 预算与口径

1. **预算**：解决方案级构建 **≤1**（§〇.3 ⑤ 的 0 行式子成立＋载体 `src/Pry.Core/bin/Release/net10.0/Pry.Core.dll` 在盘＋六枚 SHA 未变 ⇒ 允许跳过并登记三段判定）；解决方案级全量 **≤1**；定向 **≤5**；`dotnet` 总调用上限＝**6**。五轮一律**不许带 `--no-build`**。
2. `全量 ≤1`＝交付读数只许来自 1 次解决方案级全量；作废轮的重跑不占这一格，但每轮全量都要逐轮登记（第几次调用、结果目录名、TRX 枚数、`<Counters>` 求和）。
3. **作废轮产物一律改名留档，不许清空重跑**：`trx-<轮>-voided-<预检号>`。**满额形状＝定向 5×1＋全量 1×4＝9 枚 `.trx`**；报告 §六要有"盘上枚数＝登记轮数×每轮枚数"的自校，差额逐处点名去处。
4. **取数仪器（口径⓪：先自证再用）**：沿上一把的四条——① 只数 `<UnitTestResult … outcome="Failed">` 的**开标签**并与同份 `<Counters failed>` 双向对账（`<ResultSummary>` 不算用例）；② 反向锚定抓 `outcome="Failed"` 回溯最近 `<` 判标签名；③ 红名先剥全限定前缀与参数表（`split('(')[0].split('.').pop()`）再回判据验"方法名＋`(`"存在，**正则必须同时认 `public void`（24 枚）与 `public async Task`（30 枚）**；④ 属性**按名字取**，不许按顺序拼正则；抽栈帧**不许锚 `at `**，形状＝`([\w.\-]+\.cs):(?:line |：行 )(\d+)`。
5. **已知读数表（每一面带四元限定＝波次＋判据文件＋跑法＋该面结论字段值）**：

   | 面 | 盘上位置 | 跑法 | 应当读到 |
   |---|---|---|---|
   | 波 39 `trx-M1`／`trx-M3`／`trx-M4`／`trx-M5`／`trx-M6`／`trx-M7`／`trx-M9` | `.local-notes/wave39-1029/out/` | 定向 `NarratorPrototypeTests`（56 格树） | 各恰 **1**、`total=56`、`notExec` 开标签 **0**、红名逐字符等于波 39 报告 §三.10 那行 |
   | 波 39 `trx-M2` | 同上 | 定向同上 | 恰 **3**、三枚名同上 |
   | 波 39 `trx-M8-voided-P103` | 同上 | 定向同上 | 恰 **5**（＝四枚目标＋@851 那枚），**这面是本刀 M8′ 的预期来源，不是"清洁单枚轮"凭证** |
   | 波 39 `trx-F` | 同上 `trx-F/` | 解决方案级全量 | 4 份求和 **1406／executed 1404／failed 0**、跳过 2 枚同名点名 |
   | 波 13 `run-m01`／`m02`／`m14` | `.local-notes/qa-wave13-1023/runs/` | 定向（旧 49 格树） | 恰 **1／3／4**，红名等于波 22 报告 §三.1 各行 |
   | 波 13 `run-m07`／`m08`／`m09`／`m10` | 同上 | 定向（旧 49 格树） | 各恰 **1**（＝**旧树**读数，本刀在 56 格树上的预期**不得**从这四面平移，见 §三 首部） |
   | 波 13 `run-full` | 同上 `run-full/` | 解决方案级全量 | 4 份 TRX、`total` 求和 1228／executed 1226、**failed 求和＝2**（＝`ResourceLimitLedgerCoverageTests` 的 `Every_limit_shaped_constant_has_a_row_in_the_ledger` 帧 `:50`＋`The_guard_sees_a_row_being_dropped_and_a_row_drifting_off_its_line` 帧 `:110`，那两枚后来已被结清）、Core 那份 total=607／executed=605、`NotExecuted` 开标签 2 枚点名 |
   | 波 38 `trx-M5c`／`trx-S1`／`trx-S2` | `.local-notes/wave38-1029/out/` | 定向 `StateAndLearningPrototypeTests`（另一棵判据，26 格） | 各恰 **1**，红名不属于本工单任何一枚格 |

   任一不符＝仪器坏了，**先修仪器，不改判据、不改盘上产物**。**上一把这张表里"波 13 `run-full` failed 全 0"那一行是守门人写错的**（盘上实有 2 枚红），本表已按盘上改正；把改正后的面当自证面用之前，lane 要自己重取一次读数。
6. **报告先建、逐轮落盘**（150 轮硬上限）：动第一次 `dotnet` 前把 §〇 写满建文件（**报告路径＝`docs/eval/reports/2026-10-29-hds13-bclass-second-hand-recount-part2.md`，主树里唯一允许新建的文件，不提交、不 `git add`**），之后每完成一轮立刻追加该轮十一项再跑下一轮。十一项＝①文件:行 ②改前原文 ③改后原文 ④`git diff --stat` ⑤跑法整行 ⑥TRX 文件名 ⑦红格全名（逐字符）＋消息要点＋红点行号 ⑧还原命令 ⑨还原后 `git status --short` 行数 ⑩突变态与还原后 SHA-256＋本轮判据文件 SHA-256 ⑪当轮预检整段。缺 ⑨⑪ 退回不补签。
7. **定向跑法固定**（逐字照抄只换结果目录名，载体内，无 `--no-build`）：`dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~NarratorPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave40-1029/out/trx-<轮>" --logger trx`。**每轮 `total` 应当＝56**。
8. **突变写入通道**：五枚文本由守门人预合成在 `.local-notes/wave40-1029/mutation-prep/<ID>_<basename>`，lane **只许整文件字节复制**应用；应用后立即 `sha256sum`，**必须等于 §三.0 该轮 SHA**，不等即整轮作废并如实登记；还原一律 `git show 51f149d:<path>` 取字节再写文件（不许 `checkout`／`restore`），还原后 SHA 必须等于 §〇.4 基准。守卫形态＝对 `tests/**` 硬拒，两次尝试真验出报错（(a) 合法 ID 的产品文本写到 `NarratorPrototypeTests.cs`，(b) 手写字节写到 `NarrativeContextCompiler.cs`）＋一枚正面控件（合法 ID 配合法文本写到合法产品文件必须放行、随即还原），不许只声称。**五轮的判据字节全程等于 `4f1b2f6e…`＝本刀能签换手的全部凭据**。

## 二 允许的文件面（超出即作废）

- ① `.local-notes/wave40-1029/`（自建，gitignored）：报告草稿、仪器、manifest 与全部 TRX；**`mutation-prep/` 五枚、`gk40-prep.js` 与 `out/gk40-prep.txt` 是守门人产物，lane 只读**；
- ② 报告 `docs/eval/reports/2026-10-29-hds13-bclass-second-hand-recount-part2.md`（唯一一份可写文档）；
- ③ **载体内**的 `src/Pry.Core/Narrative/NarrativeContextCompiler.cs`（m07–m10）／`NarrativeRoundRunner.cs`（M8′）——只作临时覆盖、逐轮按字节还原，终态相对 `51f149d` 净写入 0 字节；
- ④ **禁止**：写任何 `tests/**` 文件、新建任何产品或测试文件、改 `scripts/**`／`Pry.slnx`／`Directory.Build.props`／其他 `docs/**`（除②）／主树任何文件；不许把 §三.0 的预期列改成实测值。

## 三 本刀要跑的五轮

### 三.0 五轮表（行号＝`51f149d` 侧现值，由 `gk40-prep.js` 当场打印；"格@行"＝该枚格在当前判据文件里的方法起始行，红点帧只作辅证，主证＝testName＋消息）

| 轮 | 配方出处 | 文件:行 | 改前整行 → 改后整行（盘上实测 SHA） | 本刀要领账的格 | 守门人事前点名 | 枚数 | 突变文本 SHA-256 |
|---|---|---|---|---|---|---|---|
| M8′ | 波 13 §三.14／波 39 §三.8 | `NarrativeRoundRunner.cs:115` | `                Disposition = NarrativeRoundDisposition.ProviderRejected, Assembly = assembly, Outcome = outcome,` → 同行 `ProviderRejected` 改 `Silence` | `Malformed_response_is_not_masquerading_as_a_chosen_silence`@534＋`A_provider_that_ignores_cancellation_still_exits_as_timeout`@549＋`A_provider_that_honours_the_timeout_token_exits_as_timeout_too`@562＋`Provider_failure_becomes_a_named_error_shape_and_writes_nothing`@574 | **恰这 5 枚＝四枚目标＋`Provider_side_cancellation_with_a_live_host_token_is_named_timeout_not_failure`@851（帧 `:865`）**，机理＝`NarrativeRoundRunner.cs:110` 的早返回路径上 `:115` 那枚共享 `Disposition` 常量，即时支（`:150`）／延迟支（`:155`）都在其后不可达；@851 那枚要领账的账不在本刀（它是波 18 补格，本刀只登记它同场响过） | 恰 **5** | `0400a57714bbdd8e8d642e78daaa5e9ceaf79c927029c63a06d5d0daaf186c08`（16759 字节，与波 39 的 M8 文本同字节） |
| m07 | 波 13 §三.7／`run-m07` | `NarrativeContextCompiler.cs:203` | `        if (cost > cap \|\| used + cost > total)` → `        if (used + cost > total)` | `Compiler_drops_an_optional_section_that_exceeds_its_own_share_and_counts_it_exactly`@210 | **不预名**（见 §三.0R 件 3′） | 待点名 | `24e90d81e2d0c6be5a54786ab13c4d4f32373bfafd7455ed9e0bc631f5f10a43`（24443） |
| m08 | 波 13 §三.8／`run-m08` | `:246` | `            if (wallet - cost < 0 \|\| used + cost > total)` → `            if (used + cost > total)` | `Compiler_trims_recent_messages_only_at_whole_unit_boundaries`@281＋`Compiler_shrinks_the_memory_wallet_unit_by_unit`@313 | **不预名** | 待点名 | `a65399bbbe78c9241d266c57898b01707f10b2a865b9ed8db8b25391da37f1cb`（24436） |
| m09 | 波 13 §三.9／`run-m09` | `:239` | `            if (!string.Equals(unit.ConversationId, request.ConversationId, StringComparison.Ordinal))` → `            if (unit.ConversationId is null)` | `Compiler_removes_cross_conversation_units_and_names_the_count`@298 | **不预名** | 待点名 | `dfc917334c7f48941ffc8e29e3a7735f283e60b78c3d9445cc25798020eb91e5`（24399） |
| m10 | 波 13 §三.10／`run-m10` | `:284` | `            BudgetTokens = cap, UsedTokens = 0, Disposition = NarrativeSegmentDisposition.NotSupplied,` → 同行 `NotSupplied` 改 `Dropped` | `Compiler_keeps_absent_sections_as_not_supplied_instead_of_dropping_them`@198 | **不预名** | 待点名 | `24dbd7e558ab4362ded03fd2de34e2833da90f0541286ca2dab62f1f0c4d14ce`（24453） |

**跑序固定＝M8′→m07→m08→m09→m10→全部还原并核六枚 SHA（判据必须仍＝`4f1b2f6e…`）→F。** 未跑满"能起跑的轮"（＝§三.0R 件 3′ 已闭合点名的那些轮）不许起 F；一格都没闭合就一格不跑，那仍是合法交付。

**为什么 M8′ 由守门人预名而 m07–m10 不预名**：M8′ 的第五枚名与帧在波 39 已经落盘（`trx-M8-voided-P103`，红名逐字符＋帧 `:865`＋消息族 `Expected: ProviderRejected / Actual: Silence`），机理已由那只手在起跑前独立复算过一次并与盘上同向 ⇒ 本刀把它写成**事前论证**而不是事后追认。m07–m10 四枚门是波 18 那几枚补格所钉的门（`:203`／`:239`／`:246`／`:284`），在当前 56 格树上极可能一次点亮 2–4 枚，而"哪些格连坐"必须逐枚读通夹具才能点名——**上一把已经证明：按旧树红集平移会在 M8 那种轮上漏判连坐**（工单原写"波 18 新增五枚"，盘上是七枚，49＋7＝56）。所以这四轮的枚数**不在本工单预填**，由 §三.0R 件 3′ 那道门当场闭合。

### 三.0R 本刀（lane）必须自己重做的独立复算（零 `dotnet`，先档后跑；不齐就不起那一轮）

1. **件 1 逐枚并排自证**：把 `mutation-prep/` 五枚与 `git show 51f149d:<path>` 的 blob 逐枚 `diff`，改动面必须只有 §三.0 声明的那一行，且改前整行与 §三.0 逐字符相同。
2. **件 2 消费点复算**：判据 56 枚格按 §一.4 的两族签名正则全数点名（`public void` 24＋`public async Task` 30＝54 枚带属性格，另有 2 枚 `ProposeAsync` 夹具），逐枚判定它调 `Compile`／`RunAsync`／`Validate`／`Consume` 还是只读装配体属性面。**枚举面必须自己按盘上算术取，不许抄任何报告里的"新增 N 枚"转述**（上一把就是栽在这里）。
3. **件 3′ 逐轮点名闭合门（本刀新增，是上一把 §三.0R 件 3 的强化版）**：m07–m10 每轮起跑前，报告里必须已经有一张**闭合的**预期表＝"本轮改的那一行 ⇒ 哪些格的断言会翻、每枚的夹具面与可达路径逐枚写清（含判定为不连坐的格为什么不连坐）"。候选面从盘上取：当前判据里断言面出现 `NarrativeSegmentDisposition.Dropped`／`NotSupplied`／`PartiallyIncluded`／`DroppedSegmentCount`／`NotSuppliedSegmentCount`／`DroppedUnitCount`／`CrossScopeUnitCount`／`BudgetDroppedUnitCount`／`IncludedUnitCount`／`DispositionReason`／`EmittedText` 的格共 **10 枚**＝`Compiler_keeps_absent_sections_as_not_supplied_instead_of_dropping_them`@198、`Compiler_drops_an_optional_section_that_exceeds_its_own_share_and_counts_it_exactly`@210、`Compiler_fails_loudly_when_the_reserved_directive_does_not_fit`@266、`Compiler_trims_recent_messages_only_at_whole_unit_boundaries`@281、`Compiler_removes_cross_conversation_units_and_names_the_count`@298、`Compiler_shrinks_the_memory_wallet_unit_by_unit`@313、`Whole_section_segments_carry_their_own_source_reference_even_when_dropped`@913、`Absent_sections_are_named_not_supplied_rather_than_budget`@934、`Unit_run_losing_units_to_both_reasons_names_both_together`@952、`Unit_run_that_fits_no_unit_at_all_is_dropped_and_named_budget`@975（守门人 10-29 02:3x 当场扫描所得，lane 要独立复算这张清单本身）。**闭合不了（有任一枚判不清）⇒ 不起那一轮**，定向额度原样退回，把判不清的那枚与判不清的原因逐枚登记——那是发现，不是失败。
4. **件 4 静态语法判读**：五枚逐枚判净括号偏移与全文件收支；发现任一枚不闭合 ⇒ **不起那一轮**。（守门人侧已打印：五枚全部"行数变化 0、净括号偏移守恒、全文件大括号收支未变"；m09 的圆括号总数 97→96 是成对删除，净偏移仍守恒。）
5. **件 5 判据字节三面自证**：起跑前与每轮还原后都 `sha256sum` 判据文件，五轮 10 次读数＋出场面必须全部逐字符＝`4f1b2f6e…`。

### 三.P 焦点预检台账（逐枚当场登记，含 HOLD；每一次 `dotnet` 之前一枚，编号从 P106 起）

报告里要有：编号、时刻、VERDICT、归档路径、前台窗 pid／class／covers／borderless、`free_memory_gb`、放行哪一轮；出场给分布统计与"HOLD 重跑额度动用 x/16"，并逐份 grep `VERDICT` 与 `dotnet` 次数对账（预检枚数＝`dotnet` 次数＝定向轮数＋全量 1）。

## 四 本刀**不**跑的账（押给下一把，不许顺手碰）

1. **D 类 28 枚一枚不碰**（波 22 §三.1 里标 D 的那些，配方＝"新增形状／拆既有历史门"级设计题，要逐枚先零 dotnet 推演红集再起跑）。
2. **波 13 的 #12、#16–21、#23、#28–49 那批**＝D 类，同上。波 20 那 28 枚押在它自己那本账里，**不许与本刀混编**。
3. `Provider_side_cancellation…`@851 那枚格的换手／红证归属**不在本刀领**（它不是波 13 那 65 格里的成员，本刀只在 M8′ 的预期表里点名它）；`:367` 那半句、探针正数面、HDS-009 那 26 枚的 A／C／M 类一律不重跑；波 28 那枚数值锁格照旧押（**不许为凑红造门槛**）；两套并行命名债属接线刀不属证据刀；HDS-010 删除片、`PrototypeFiles` 覆盖盲区、生产接线（U-9 残句仍未拍）、真实模型对照（要作者一句"可以跑"）——照旧挂着。
4. 不许把 m07–m10 里任何一轮的"点名不闭合"处理成"跑一把看看响几枚"（§三.0R 件 3′ 是硬门）。

## 五 交回给守门人核的七件事（不在本 lane 权限内）

1. M8′ 是否**恰 5 枚**且红名逐字符等于 §三.0 那张事前论证；**多一枚或少一枚都整轮作废**（四枚目标就不签，照上一把处理，不许追第二轮）。
2. m07–m10 各轮：起跑前报告里是否**已有闭合点名表**；实测红集是否逐字符等于该表；表内是否有"判不清"被写成"判不连坐"。
3. 五轮的**判据文件 SHA 是否 10 次＋出场面全等于 `4f1b2f6e…`**（缺一次就不签那一轮）。
4. 载体终态：`status --short`＝**0 行**、`fd93ebe..HEAD` 仍只有波 33 那两枚提交、六枚 SHA 逐字符等于 §〇.4。
5. 收尾全量四份 TRX 先数满再求和，应当**仍＝1406／executed 1404／failed 0／跳过 2**。**本刀不顶账**，也不许为顶账另起构建。
6. `.trx` 枚数与轮数对账（满额 9 枚＝5＋4；差额逐处点名去处）；每轮 `total` 是否＝**56**；三计数出场复看＝`[Fact]` 53／`[Theory]` 1／`[InlineData]` 3 与签名两族 24＋30（净新增 0）。
7. 每一枚红是"该格承重断言失败"还是"整格因异常而红"（消息族 `Assert.*` vs `…Exception thrown`）——后者不署断言级凭证，逐枚点名交回。

## 六 未验证（照旧逐条点名，不用"已完成"混过去）

真实模型对照（零读数，需作者一句"可以跑"）；**生产接线**（HDS-003 这 56 枚至今零生产调用方；接线时机＝U-9 残句仍未拍）；旧数据真实继承（零读取）；UI 可见行为（零读数）；生产库与 `%LOCALAPPDATA%\PryCompanion`（零接触）；待标定参数（关系／心情各档阈值、经验有效期、点击归因窗口全部未定＝**特性不是缺陷**）；并发与断电（零读数）；旧链删除片与"构建时不把旧链打进包"那道门（**未开始**）；覆盖率（零读数）；Release 序列点把红点记到多行调用起始行的机理（已知形状，不影响署名）。**本刀不签"波 13 结案"、不写状态词**。**四句话照旧不许写**＝新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链。拆法原文照旧 46／45／1＝W2、catalog 76 未动、在册基线不移动。

## 七 不许走的捷径（逐条对照前十三把踩过的坑）

1. 不许新增判据格、不许改任何断言、不许改 `[InlineData]` 期望值、不许 `Assert.Skip`、不许注释既有断言、不许改上游门、不许新增数字门槛。
2. 不许手写 replacement；不许把两轮合并成一轮；不许带 `--no-build`。
3. **点名不闭合就不起那一轮**（§三.0R 件 3′）：把判不清的面逐枚登记进报告，定向额度原样退回。**不许用"跑一把看响几枚"代替事前点名**——上一把 M8 那一轮就是这么判废的，那四枚今天还得再跑一次。
4. 不许把 §三.0 的预期列改成实测值；实测与事前表不符（含 0 枚红、含多一枚、含少一枚）⇒ 那一轮作废并如实登记。
5. 不许把波 13 老 TRX 或波 39 `trx-M8-voided-P103` 的红署成本刀的红（**本刀凭证＝本刀在载体里跑出来的那份 TRX**；波 39 那枚作废轮只用作"事前论证的来源"，不作凭证）。
6. 不许碰 D 类、不许拿 HDS-009 那两棵判据文件做 §一.5 自证面以外的任何写。
7. 不许删作废轮产物、不许清空结果目录重跑、不许在报告里替守门人写状态词／结案词／自封在册基线。**SHA 对账不能替代并排自证**；**枚举面必须按盘上算术自取，不能转述任何报告里的"新增 N 枚"**。

## 八 交回与划账（本 lane 写 §〇–§七，守门人追加 §九）

（守门人预登记划账面：① M8′ 逐枚点名与"恰 5"的形状，多一枚即整轮作废、#24–27 不签；② m07–m10 每轮的闭合点名表是否先落档、实测是否等表；③ 判据字节全程未动＝本刀能签换手的全部凭据；④ 顶账＝本刀不顶，1406 的第六次同值复采（若 F 起了）；⑤ `.trx` 9 枚对账与构建跳过三段；⑥ 定向 ≤5／`dotnet` ≤6 是否越预算，HOLD 额度是否被用成放宽；⑦ 旧欠 65 → 几格：减账的每一枚都要有一轮清洁红作凭，求和式当场从盘上产物写，不许心算。报告只追加 §九，§〇–§七 一字不改，删除列恒 0。）
