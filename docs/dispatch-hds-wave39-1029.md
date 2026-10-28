# 工单 HDS-WAVE39-1029｜旧欠 75 格的第一片＝波 13 的 B 类 14 格换人复采（九枚单行突变·另取一双手，零落地、零归位、零顶账、判据字节全程不动）

- 工单 id：`HDS-WAVE39-1029`
- 性质：**证据刀·换手复采**（不是新原型、不是接线、不是新判据）。第三十八波把 HDS-009 那棵 26 枚的证据账收到 26／0／0；这一把离开 HDS-009，去收全仓最大的一本欠账＝**旧欠 75 格（波 13 余 47＋波 20 余 28）的换手账**的第一片。
- 任务编号：无新编号；登记处＝`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS-003／HDS-010 行与 §9 最新 entry；本刀出处＝`docs/eval/reports/2026-10-23-hds003-005-cell-evidence-audit.md`（第二十二波）§四.1 的 T2 梯队与 §四.2.1 那句"另一双手才谈得上第二换手"，＋`docs/eval/reports/2026-10-23-hds003-005-second-hand-recount-prototype.md`（第二十四波）L158 那条"旧第二换手凭证对现网字节作废、须在现字节上以另一双手重取"。
- 出闸：守门人 2026-10-29 01:1x。作者原令逐字＝「我不想思考了，你就继续给我派工单，该提交的提交吧」＋「你，还是自己起Agent吧」＋「继续工作，不要让我拍板啥的，告诉我现在是什么情况就行」＋「下一次不要再三构建触发那个防火墙烦我打游戏」＋「游戏基本都是全屏应用，看当前桌面焦点是不是全屏应用就行」
- 执行方：冷启动 lane（载体＝沿用第三十三波那棵，**不新建 worktree**）
- **这一刀对作者可见的东西＝零**：净新增判据格 0、产品字节终态净写入 0、界面与对话里什么都不会变。它做的是"同一枚字节改坏、换一双手再响一次"。

## 〇 开工前必读与入场（缺一条就就地停刀登记，不要自扩范围）

1. **先读八份**：`docs/plans/README.md`、`docs/plans/PROJECT_PLAN.md`（§4.2 的 HDS-003／HDS-010 行＋§9 最新一条 entry）、`docs/agent-engineering-standards.md`（全文，交付形态按 §10 四项）、`docs/eval/reports/2026-10-29-hds009-wave38-assert-face-and-scan-bite.md`（上一把十一项写法＋守门人 §九.0 的"已知读数表每面必须带四元限定"与 §九.5 的押账——**本刀的配方与仪器口径全部从那里派生**）、`docs/eval/reports/2026-10-23-hds003-005-cell-evidence-audit.md` §三.1 与 §四.1（65 格的逐格凭证表与 T1–T5 复采清单）、`docs/eval/reports/2026-10-23-hds003-005-second-hand-recount-prototype.md` §一–§三（上一把 24 枚换人复采的十一项写法与成对门）、`docs/eval/reports/2026-10-23-hds003-narrator-prototype.md` §三.1–§三.15（九枚突变的原始配方出处），以及判据本体 `tests/Pry.Core.Tests/NarratorPrototypeTests.cs`（**989 行、54 枚方法、56 枚格**）与三枚被改坏的产品文件 `src/Pry.Core/Narrative/NarrativeContextCompiler.cs`／`NarratorContracts.cs`／`NarrativeRoundRunner.cs`（只读改动行邻域，逐枚核对 §三.0 的"改前整行"）；`.local-notes/wave39-1029/mutation-prep/`（守门人预合成的**九枚**突变文本）与 `.local-notes/wave39-1029/gk39-prep.js`（并排自证：每枚先验 OLD 在文件里恰出现 1 次且不跨行，再打印改前／改后整行、行号、净括号偏移、全文件括号收支、行数变化＝0）与 `.local-notes/wave39-1029/out/gk39-prep.txt`（那次自证的整段原文）。
2. **入场读数当场贴进报告 §〇**（不许引用本工单的数字）：`date`、`git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD`、`git status --short --branch` 原样、`git worktree list`、`git log --oneline -3`。
3. **载体＝沿用第三十三波那棵**：`.local-notes/qa-wave33-1028/carrier`（基点 `fd93ebe`）。入场自证五件：① `rev-parse HEAD`；② `status --short`＝**0 行**；③ `log --oneline fd93ebe..HEAD`＝**2 行**；④ `diff --stat fd93ebe HEAD -- src tests scripts Pry.slnx Directory.Build.props`＝**6 files changed, 822 insertions(+), 0 deletions**；⑤ 主树侧 0 行式子 `git -C <main> diff --stat 51f149d $(git -C <main> rev-parse HEAD) -- src tests scripts Pry.slnx Directory.Build.props`＝**0 行**（右端当场取，**不许在本工单里预写 SHA**）。⑤成立⇒载体树与主线归位后的产品／判据字节逐字符相同，本刀在载体里取到的红就是主线那棵树的红。
4. **六枚文件入场 SHA-256 基准**：当场 `sha256sum` 三侧各取一次（主树工作副本＝载体工作副本＝`git show 51f149d:<path>` 的 blob），三侧逐字符相同才起跑。守门人出闸前实测（lane 必须自己重测，不得引用）：`NarrativeContextCompiler.cs`＝`86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842`；`NarratorContracts.cs`＝`3a33a8151e61c3396963dac451dffcb46acb2f02807f140d18f694d6a52a9f8c`；`NarrativeRoundRunner.cs`＝`961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9`；判据 `NarratorPrototypeTests.cs`＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`；`NarrativeCommitFactoryTests.cs`＝`fcee461164e39bae172c49149d39076e67aa36e844089afa4bcf4ff06d7cea0b`；`NarrativeBudgetInequalityCellTests.cs`＝`192bbaecb5d2a3f3bafd08d8cbe5fdacf65ee71a7ad7856798345eae6c3f7686`。**判据那枚全程必须等于 `4f1b2f6e…`**（本刀九轮一枚都不写判据，见 §一.7）。
5. **在册基线起点＝当场从波 38 报告守门人 §九.1.6 读到的那个数**（1406／executed 1404／failed 0／跳过 2，波 34／36／37／38 已四次同值复采）。本刀**净新增格数＝0**，F 轮出口读数应当**仍是 1406**：读到别的数先怀疑树没还原干净，不改判据、不改基线、照实登记并作废那一轮。
6. **本刀的坐标系（先把这本账读对再动手）**：旧欠 75 格＝**波 13 余 47＋波 20 余 28**（现账出处＝波 24 报告 §四.2 那句"现账＝75 格…旧值 99 自此过期"＋波 38 报告 §五/§六 的"照旧挂着"）。波 13 那 47 枚在第二十二波 §三.2 的档位里分成两半＝**B 类 19 枚**（盘上有单枚清洁红、缺一两项字段，且红全部出自判据作者那一双手）＋**D 类 28 枚**（盘上根本没有该格的突变红）。**本刀只碰 B 类里的 14 枚**，D 类一枚不碰（§四.1）。
7. **他人资产**：主树那两枚 `M ` 暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）与全部 `??`＝别人的，不读内容、不改、不提交、不 `stash`／`clean`／`rm`。**共享主树里禁止任何 `git commit`／`add`／`checkout`／`reset`／`cherry-pick`／`merge`／`tag`／`push`／`git apply`／`stash`／`clean`／`rm`**；写只许落在载体里（九轮临时覆盖、逐轮按字节还原）＋你自己那一份报告文件。**本刀归位是空转**：终态净写入 0 字节⇒无代码刀、`main` 不因本刀挪。
8. **焦点预检（这条会打断作者本人，违反＝本波作废）**：每一次 `dotnet`（含每一枚突变轮与收尾全量）之前先跑

   ```
   powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1
   ```

   整段输出贴进报告并归档 `.local-notes/wave39-1029/out/preflight-<ID>.txt`，**编号从 P95 起**（P1–P94 已被前十二把用掉）。只有 `VERDICT=BUILD_OK` 才起跑。读到 `HOLD`：**不起跑、不请示、不改判据、不绕过**；先把零 `dotnet` 的活做完（§〇 全部、§三.0R 的并排自证、仪器自证、守卫实证），然后**可以重跑预检**——预检本身不设上限，只对"HOLD 之后的重跑"设上限：**最多累计 16 次、相邻两次不短于 5 分钟**（额度按刀计）。16 次后仍 `HOLD` ⇒ **带着"本刀零 `dotnet` 起跑"或"只跑到第 N 轮"的交付回来，那是合法交付**，收尾全量不跑＝基线不移动，不是短处。`free_memory_gb` 偏低或读数呈 SQLite `disk I/O`／`out of memory` 形态时那一轮读数作废（他机器资源形状，非断言失败），**不动任何进程**。

## 一 预算与口径

1. **预算**：解决方案级构建 **≤1**（若 §〇.3 ⑤ 那条 0 行式子成立、且载体 `src/Pry.Core/bin/Release/net10.0/Pry.Core.dll` 在盘、六枚 SHA 未变，**允许跳过并登记判定**）；解决方案级全量 **≤1**；定向 **≤9**；`dotnet` 总调用上限＝**10**。九轮一律**不许带 `--no-build`**（改字节的突变轮带它就是跑旧 dll＝红假没）。
2. `全量 ≤1` 的语义＝"交付的读数只许来自 1 次解决方案级全量"。作废轮的重跑不占这一格，但每轮全量都要逐轮登记：第几次调用、结果目录名、TRX 枚数、`<Counters>` 求和。
3. **作废轮产物一律改名留档，不许清空重跑**：`trx-<轮>-voided-<预检号>`。**满额形状＝定向 9×1＋全量 1×4＝13 枚 `.trx`**；报告 §六 要有"盘上枚数＝登记轮数×每轮枚数"的自校，差额逐处点名去处。
4. **取数仪器（口径⓪：先自证再用；本刀的自证面全部换成"与本刀同一判据集／同一跑法"的盘上老读数，见下面那张四元限定表）**：① 只数 `<UnitTestResult … outcome="Failed">` 的**开标签**，与同份 `<Counters failed>` 双向对账；② 反向锚定面抓 `outcome="Failed"` 回溯最近 `<` 判标签名（`<ResultSummary>` 不算用例）；③ 红名先剥全限定前缀与参数表（`split('(')[0].split('.').pop()`）再回判据验"方法名＋`(`"存在——**本刀的判据里 54 枚方法有两族签名（24 枚 `public void` ＋ 30 枚 `public async Task`），方法名抽取的正则必须认 `async Task`，只按 `public void` 抽会把 30 枚判成"判据里不存在"**（波 38 的仪器就是只认 `void`，直接搬到这棵树会误报）；④ 比对预期红名时**先剥前缀再比**；⑤ 抽栈帧不许锚 `at `，形状＝`([\w.\-]+\.cs):(?:line |：行 )(\d+)`；⑥ 属性按**名字**取，不许按顺序拼正则（波 38 实测：有的 TRX 把 `testName` 排在 `outcome` 之前）。
5. **已知读数表（每一面带四元限定＝波次＋判据文件＋跑法＋该面结论字段值；不许只写目录名）**：

   | 面 | 盘上位置 | 跑法 | 应当读到 |
   |---|---|---|---|
   | 波 13 `run-m01` | `.local-notes/qa-wave13-1023/runs/run-m01/` | 定向 `NarratorPrototypeTests`（旧 49 格树） | 恰 **1** 枚红、`<Counters failed="1">`、total=49、红名＝`Compiler_rejects_a_negative_section_share` |
   | 波 13 `run-m02` | 同上 `run-m02/` | 同上 | 恰 **3**、failed="3"、三枚名＝`Compiler_refuses_a_window_that_leaves_no_positive_input_budget`／`Round_does_not_call_the_provider_when_the_context_is_rejected`／`Call_refuses_an_assembly_that_still_carries_a_rejection` |
   | 波 13 `run-m14` | 同上 `run-m14/` | 同上 | 恰 **4**、四枚名＝`Malformed_response_is_not_masquerading_as_a_chosen_silence`／`A_provider_that_ignores_cancellation_still_exits_as_timeout`／`A_provider_that_honours_the_timeout_token_exits_as_timeout_too`／`Provider_failure_becomes_a_named_error_shape_and_writes_nothing` |
   | 波 13 `run-re-m02`／`run-re-m14` | 同上 | 守门人重取（同一判据集） | 各恰 **3**／**4**，红名逐字符等于上面两行 |
   | 波 13 `run-m03`／`m04`／`m05`／`m06`／`m11`／`m15` | 同上各目录 | 定向同上 | 各恰 **1** |
   | 波 13 `run-full` | 同上 `run-full/` | 解决方案级全量 | **4** 份 TRX、failed 全 0、Core total=607／executed=605，两条 `outcome="NotExecuted"` 开标签点名 |
   | 波 38 `trx-M5c`／`trx-S1`／`trx-S2` | `.local-notes/wave38-1029/out/` | 定向 `StateAndLearningPrototypeTests`（另一棵判据，26 格） | 各恰 **1**，且红名不属于本工单任何一枚格 |
   | 波 38 `trx-F` | 同上 `trx-F/` | 解决方案级全量 | 4 份求和 **1406／executed 1404／failed 0**、跳过 2 枚同名点名 |

   任一不符＝仪器坏了，**先修仪器，不改判据、不改盘上产物**。
6. **报告先建、逐轮落盘**（150 轮硬上限）：动第一次 `dotnet` 前把 §〇 写满建文件（**报告路径＝`docs/eval/reports/2026-10-29-hds13-bclass-second-hand-recount.md`，主树里唯一允许新建的文件，不提交、不 `git add`**），之后每完成一轮立刻追加该轮十一项再跑下一轮。十一项＝①文件:行 ②改前原文 ③改后原文 ④`git diff --stat` ⑤跑法整行 ⑥TRX 文件名 ⑦红格全名（逐字符）＋消息要点＋红点行号 ⑧还原命令 ⑨还原后 `git status --short` 行数 ⑩突变态与还原后 SHA-256＋**本轮判据文件 SHA-256（apply 侧一次即可，本刀判据不该动）** ⑪当轮预检整段。缺 ⑨⑩⑪ 退回不补签。
7. **定向跑法固定**（逐字照抄只换结果目录名，载体内，无 `--no-build`）：`dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~NarratorPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave39-1029/out/trx-<轮>" --logger trx`。**每轮 `total` 应当＝56**（不是波 13 当年的 49——判据在波 18 由 49 涨到 56，这五枚新增格与本轮九枚门的交叉面由 §三.0R 逐枚排除）。
8. **突变写入通道**：九枚文本由守门人预合成在 `.local-notes/wave39-1029/mutation-prep/<ID>_<basename>`，lane **只许整文件字节复制**应用（不许重排、不许改行尾、不许手写 replacement）；应用后立即 `sha256sum`，**必须等于 §三.0 该轮 SHA**，不等即整轮作废并如实登记；还原本一律 `git show 51f149d:<path>` 取字节再写文件（不许 `checkout`/`restore`），还原后 SHA 必须等于 §〇.4 基准。**本刀的守卫形态＝对判据目录硬拒**：仪器必须拒绝对 `tests/**` 的任何写入（本刀合法写入目标只有 §二 ③ 那三枚产品文件），并拿两次尝试真验出报错——(a) 用一枚合法 ID 的产品文本试图写到 `tests/Pry.Core.Tests/NarratorPrototypeTests.cs`，(b) 用手写字节写到 `NarrativeContextCompiler.cs`——两次都必须 REFUSED 且目标文件 SHA 前后同值；再加一枚正面控件（合法 ID 配合法文本写到合法产品文件必须放行、随即还原），不许只声称。**九轮的判据字节全程等于 `4f1b2f6e…`＝本刀 14 枚全部具备签换手资格**（这是与波 38 那两枚反面探针的根本差别）。

## 二 允许的文件面（超出即作废）

- ① `.local-notes/wave39-1029/`（自建，gitignored）：报告草稿、仪器、manifest 与全部 TRX；**`mutation-prep/` 九枚、`gk39-prep.js` 与 `out/gk39-prep.txt` 是守门人产物，lane 只读**；
- ② 报告 `docs/eval/reports/2026-10-29-hds13-bclass-second-hand-recount.md`（唯一一份可写文档）；
- ③ **载体内**的 `src/Pry.Core/Narrative/NarrativeContextCompiler.cs`（M1–M6）、`NarratorContracts.cs`（M7）、`NarrativeRoundRunner.cs`（M8／M9）——只作临时覆盖、逐轮按字节还原，终态相对 `51f149d` 净写入 0 字节；
- ④ **禁止**：写任何 `tests/**` 文件（本刀判据字节一枚都不许动）、新建任何产品文件、新建任何测试文件、改 `scripts/**`／`Pry.slnx`／`Directory.Build.props`／其他 `docs/**`（除②）／主树任何文件；不许把 §三.0 的预期列改成实测值。

## 三 本刀要跑的九轮（预期面＝守门人从波 13 盘上老 TRX 取红集＋从当前 56 格树独立复算交叉面；**"预期枚数／格名／红形状"是假设不是结论**：实测与之不符⇒整轮作废、红不署、不追第二轮）

### 三.0 九轮表（行号＝`51f149d` 侧现值，由 `gk39-prep.js` 逐枚当场打印；"格@行"里的行＝该枚格在当前判据文件里的**方法起始行**，红点帧只作辅证，主证＝testName＋消息）

| 轮 | 配方出处 | 文件:行 | 改前整行 → 改后整行（守门人预合成，盘上实测 SHA） | 预期恰响格（前缀 `Pry.Core.Tests.NarratorPrototypeTests.`） | 枚数 | 突变文本 SHA-256 |
|---|---|---|---|---|---|---|
| M1 | 波 13 §三.1／`run-m01` | `NarrativeContextCompiler.cs:54` | `                budget.ExpressionTokens, budget.CapabilityTokens, budget.AdvisoryTokens }.Any(x => x < 0))` → 同项 `.Any(x => x < 0 && x > 1000))` | `Compiler_rejects_a_negative_section_share`@:347 | 恰 1 | `a20eb74de3c38734c596e088c18dad67195283d7a2486f1462f9f924f051dee0`（24469 字节） |
| M2 | 波 13 §三.2／`run-m02` | `:62` | `        if (total <= 0)` → `        if (total <= -1000000)` | `Compiler_refuses_a_window_that_leaves_no_positive_input_budget`@:237＋`Round_does_not_call_the_provider_when_the_context_is_rejected`@:394＋`Call_refuses_an_assembly_that_still_carries_a_rejection`@:420 | 恰 3 | `cf796feac5ac36d3407f3e7dfca887fbb6f2f9a12cefafad6f3c4de6d68228fb`（24464） |
| M3 | 波 13 §三.3 | `:71` | `        if (string.IsNullOrWhiteSpace(payloads.Identity))` → `        if (payloads.Identity is null)` | `Compiler_refuses_a_scene_without_identity_instead_of_inventing_one`@:247 | 恰 1 | `7187b9bbd9fd03cfe8cd2db6b5c3e03d4b68d213a01f44be4005e94a36467cd3`（24438） |
| M4 | 波 13 §三.4 | `:78` | `            \|\| !string.Equals(payloads.CurrentEvent.ConversationId, request.ConversationId, StringComparison.Ordinal))` → `            \|\| false)` | `Compiler_refuses_a_current_event_from_another_conversation`@:256 | 恰 1 | `d7fff9107c5166be4c03a1fb99895a33924e4aa13e716615d045bc70a323e5c1`（24360） |
| M5 | 波 13 §三.5 | `:87` | `        if (preludeCost > total)` → `        if (preludeCost > total * 100)` | `Compiler_refuses_the_round_when_the_mandatory_prelude_does_not_fit`@:225 | 恰 1 | `6f8b4608172a95ac826c24b48dbbb6faac1102edeb26c70d4648424662d16b20`（24463） |
| M6 | 波 13 §三.6 | `:110` | `            if (cost > budget.AdvisoryTokens \|\| used + cost > total)` → `            if (used + cost > total)` | `Compiler_fails_loudly_when_the_reserved_directive_does_not_fit`@:266 | 恰 1 | `c06190512e2780538fb480f95f56bf2b0c877084b7db89c09db9cda9ce69f866`（24425） |
| M7 | 波 13 §三.11 | `NarratorContracts.cs:107` | `        if (next != 1)` → `        if (next > 2)` | `Second_call_on_the_same_round_budget_is_refused`@:408 | 恰 1 | `b69488df60da03777afc99e46e79cb1b41370f7021f23cf400c2d4860d0e4d7b`（5535） |
| M8 | 波 13 §三.14 | `NarrativeRoundRunner.cs:115` | `                Disposition = NarrativeRoundDisposition.ProviderRejected, Assembly = assembly, Outcome = outcome,` → 同行 `ProviderRejected` 改 `Silence` | `Malformed_response_is_not_masquerading_as_a_chosen_silence`@:534＋`A_provider_that_ignores_cancellation_still_exits_as_timeout`@:549＋`A_provider_that_honours_the_timeout_token_exits_as_timeout_too`@:562＋`Provider_failure_becomes_a_named_error_shape_and_writes_nothing`@:574 | 恰 4 | `0400a57714bbdd8e8d642e78daaa5e9ceaf79c927029c63a06d5d0daaf186c08`（16759） |
| M9 | 波 13 §三.15 | `:155` | `        if (deferred.Length > 0)` → `        if (deferred.Length > 1)` | `Deferred_round_registers_the_intent_without_touching_real_history`@:503 | 恰 1 | `2e5533294126165ea841c7212033bdd9a52b5b1ffedace533e7c954145f0c194`（16768） |

**跑序固定＝M1→M2→…→M9→全部还原并核六枚 SHA（判据必须仍＝ `4f1b2f6e…`）→F（收尾全量）。** 未跑满九轮不许起 F。每轮枚数＝上表"枚数"列；**多出的红必须逐枚登记名并整轮作废**（尤其担心波 18 那五枚新增格连坐，见 §三.1 交叉面），**恰 0 枚红同样作废并登记**（0 枚＝这枚门在当前树上没人守＝真缺陷，必须原样报上来而不是改预期）。三枚产品文件的九轮都只改一行且**行数变化＝0**（`gk39-prep.js` 已逐枚打印），所以**不存在行号漂移面**；红点帧若落在该格方法起始行之前＝怀疑仪器或怀疑自己抄错，不怀疑漂移。

### 三.0R 本刀（lane）必须自己重做的独立复算（零 dotnet，先档后跑；不齐就不起那一轮）

1. **逐枚并排自证重做一遍**：把 `mutation-prep/` 九枚与 `git show 51f149d:<path>` 的 blob 逐枚 `diff`，改动面必须只有 §三.0 声明的那一行，且改前整行与 §三.0 逐字符相同（波 37 那次"明知语法坏还跑一轮"＋波 38 那条"SHA 对账不替代并排自证"两道教训的合并执行）。
2. **消费点复算**：九枚各自那一行在当前树上被哪些格读。判据 56 枚格按 §一.4 的正则全数点名（`public void` 24 枚＋`public async Task` 30 枚），逐枚判定它调的是 `NarrativeContextCompiler.Compile` 还是 `NarratorCall.Validate`／`NarratorCallBudget.Consume` 还是 `NarrativeRoundRunner.RunAsync`，与本刀九枚门的落点对上；**波 13 的 49 格红集不能直接当成 56 格树的预期**，波 18 新增的五枚（`Unified_budget_inequality_is_pinned_at_the_edge_and_one_token_past_it`@:890、`Whole_section_segments_carry_their_own_source_reference_even_when_dropped`@:913、`Absent_sections_are_named_not_supplied_rather_than_budget`@:934、`Unit_run_losing_units_to_both_reasons_names_both_together`@:952、`Unit_run_that_fits_no_unit_at_all_is_dropped_and_named_budget`@:975）必须逐枚判"本轮改的那一行它读不读得到"。
3. **交叉面自扫（守门人已推过一遍，lane 要独立复算而不是抄）**：五枚新增格里 `Unified_budget_inequality…` 不叫 `Compile`（直接构造装配体）⇒ 九轮全不碰它；其余四枚都叫 `Compile`，但读的门是 `:203`／`:246`／`:239`／`:284`／`:263`–`:265`／`:294` 与 `:443`——**都在本刀九枚之外**（本刀动 `:54`／`:62`／`:71`／`:78`／`:87`／`:110`，不碰 `:203`／`:239`／`:246`／`:284`）。这四枚的夹具 `ContextTokens` 默认 4096、`OutputTokens` 512、`SafetyTokens` 128 ⇒ `total=3456>0`，`:62` 那枚门对它们是空转；`:110` 只在 `payloads.Advisory` 非空时到达，四枚夹具不带 Advisory。**⇒ 九轮预期枚数＝波 13 老红集原样（1/3/1/1/1/1/1/4/1）**。这条推演是假设，实测多出一枚即作废并把多出的名登记为"波 18 新格与波 13 老门的连坐面"发现（那是下一把 D 类复采要用的信息，不是本刀的账）。
4. **静态语法判读（§七.3 第三选项）**：九枚逐枚判改后行的净括号偏移与全文件括号收支（守门人侧已打印：九枚全部"净偏移不变、行数变化 0、全文件大括号收支未变"）；lane 复算若发现任一枚不闭合 ⇒ **不起那一轮**，定向额度原样退回并登记，继续跑其余轮。
5. **判据字节三面自证**：起跑前与每轮还原后都 `sha256sum tests/Pry.Core.Tests/NarratorPrototypeTests.cs`，九轮 18 次读数必须全部逐字符＝ `4f1b2f6e…`（这是本刀 14 枚能签换手的全部凭据）。

### 三.P 焦点预检台账（逐枚当场登记，含 HOLD；每一次 `dotnet` 之前一枚，编号从 P95 起）

报告里要有：编号、时刻（归档文件 mtime）、VERDICT、归档路径、前台窗 pid／class／`covers`／`borderless`、`free_memory_gb`、放行哪一轮；出场时给分布统计（`BUILD_OK` N／`HOLD` M）与"HOLD 重跑额度动用 x/16"，并逐份 grep `VERDICT` 与 `dotnet` 次数对账（预检枚数＝`dotnet` 次数＝定向轮数＋全量 1）。

## 四 本刀**不**跑的账（押给下一把，不许顺手碰）

1. **D 类 28 枚一枚不碰**（`Round_calls_the_provider_exactly_once_for_one_narration`、`Call_refuses_an_assembly_that_belongs_to_another_scope` 的三枚参数行、`Silence_round_produces_no_assistant_message_and_no_intent`、`Host_cancellation_stays_cancellation…`、`Draft_without_any_action…`、`Quote_absent…`、`Ambiguous_quote…`、`An_over_split_slice…`、`Perceived_reference…`、`Watermark_beyond…`、`Round_refuses_a_narration_without_prose…`、`Bound_action_keeps_a_reference…`、`Draft_on_a_stale_story_version…`、`Draft_from_a_fenced_writer_epoch…`、`Draft_with_a_wrong_issue_baseline…`、`Contiguous_split_reproduces…`、`Compiler_emits_the_eight_assembly_units…`、`Compiler_rejects_a_window_of_zero_tokens`、`Compiler_carries_source_references…`、`Immediate_action_binds_to_the_unique_slice…` 等）。它们**盘上没有任何突变红**，配方是"新增形状／拆既有历史门"级设计题（波 22 §四.1 的 T3／T4／T5），要逐枚先零 dotnet 推演红集再起跑，不与本刀的既有红集混编。**这条是本刀的边界不是它们的终点。**
2. **B 类剩 5 枚押下一把**（`Compiler_drops_an_optional_section…`#7、`Compiler_trims_recent_messages_only_at_whole_unit_boundaries`#8、`Compiler_shrinks_the_memory_wallet_unit_by_unit`#9、`Compiler_removes_cross_conversation_units…`#10、`Compiler_keeps_absent_sections_as_not_supplied…`#11）＝波 13 的 m07／m08／m09／m10 四枚。押它的理由要写清（不是"额度不够"而是"红集不可静态定死"）：这四枚门正是波 18 那四枚新格所钉的门（`:203`／`:239`／`:246`／`:284`），在当前 56 格树上重跑极可能一次点亮 2–4 枚，而"哪些格连坐"要先把四枚新格的夹具逐枚读通才能事前点名——**三响以上必须事前点名每一枚格名，批次红不许署给单枚突变**（波 30 立的口径）。下一把先把这枚交叉面写成表，再决定跑序。
3. 波 20 那 28 枚押在它自己那本账里（配方＝波 20 §二 M1–M7 与波 24 §二 p1–p6 那族单行翻转），与本刀的波 13 不同判据文件、不同载体焦点，**不许同一把混编**。
4. `:367` 那半句、探针正数面单独读数、HDS-009 那 26 枚的 A／C／M 类一律不重跑（**重跑＝把别人拿到的红署到自己名下**）；波 28 那枚数值锁格照旧押（**不许为凑红造门槛**）；两套并行命名债属接线刀不属证据刀；HDS-010 删除片、`PrototypeFiles` 覆盖盲区、生产接线（U-9 残句「切换那一步她要不要做什么」＝作者的话，lane 不拍也不催）、真实模型对照（要作者一句"可以跑"）——照旧挂着。

## 五 交回给守门人核的七件事（不在本 lane 权限内）

1. 九轮各自是否**恰 §三.0 那列枚数**红、红格全名是否逐字符等于 §三.0、红点帧是否落在对应格的方法体邻域；**多出的红逐枚登记名并整轮作废，批次红不许署给单枚突变**。
2. 九轮的**判据文件 SHA 是否 18 次全等于 `4f1b2f6e…`**（这是本刀 14 枚签换手的唯一凭据，缺一次就不签那一轮）。
3. 载体终态：`status --short`＝**0 行**、`fd93ebe..HEAD` 仍只有波 33 那两枚提交、六枚 SHA 逐字符等于 §〇.4。
4. 收尾全量四份 TRX 先数满再求和，应当**仍＝1406／executed 1404／failed 0／跳过 2**（两条同名跳过按 `outcome="NotExecuted"` 点名）。**本刀不顶账**，也不许为顶账另起构建。
5. `.trx` 枚数与轮数对账（满额 13 枚＝9＋4；差额逐处点名去处）；每轮 `total` 是否＝**56**（不是 49），以及三计数出场复看＝`[Fact]` 53／`[Theory]` 1／`[InlineData]` 3 与签名两族 24＋30＝54 枚方法（净新增 0）。
6. §一.8 那条硬拒守卫的证明：两次 REFUSED（判据路径／手写字节）与一次正面控件的整段原文，以及有没有人偷偷改断言、改 `[InlineData]` 期望值、`Assert.Skip`、注释既有断言、新增数字门槛、把 §三.0 的预期列改成实测值。
7. 每一枚红是"该格承重断言失败"还是"整格因异常而红"（消息族：`Assert.*` vs `NarrativeContextAssemblyRejectedException thrown`）——**后者不署断言级凭证**，要逐枚点名交回。

## 六 未验证（照旧逐条点名，不用"已完成"混过去）

真实模型对照（零读数，需作者一句"可以跑"）；**生产接线**（HDS-003 这 56 枚至今零生产调用方，005 落库面与 004 生命周期一律没接；接线时机＝U-9 残句仍未拍）；旧数据真实继承（零读取）；UI 可见行为（零读数）；生产库与 `%LOCALAPPDATA%\PryCompanion`（零接触）；待标定参数（关系／心情各档阈值、经验有效期、点击归因窗口全部未定＝**特性不是缺陷**）；并发与断电（零读数）；旧链删除片与"构建时不把旧链打进包"那道门（**未开始**）；D 类 28 枚＋波 20 那 28 枚＋B 类剩 5 枚（本刀之后旧欠 75 → **61**，本刀一枚没替它们说话）；Release 序列点把红点记到多行调用起始行的机理（波 35–38 已知形状，不影响署名）；**本刀不签"波 13 结案"、不写状态词**。**四句话照旧不许出现在任何页面**＝新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链。拆法原文照旧 46／45／1＝W2、catalog 76 未动、在册基线不移动。

## 七 不许走的捷径（逐条对照前十二把踩过的坑）

1. 不许新增判据格、不许改任何断言、不许改 `[InlineData]` 期望值、不许 `Assert.Skip`、不许注释既有断言、不许改上游门、不许新增数字门槛。
2. 不许手写 replacement；不许"顺手把 M2 与 M8 合轮"（一次两枚突变＝红无法归给单枚门）；不许带 `--no-build`。
3. **静态判读命中"语法不完整"级缺陷时不起那一轮**（波 37→38 立的第三选项）：把缺陷面（盘上行号＋现文）逐字符登记进报告，定向额度原样退回，继续跑其余轮。**不要重复"明知坏还跑一轮再作废"**：那一枚 `dotnet` 占用的是作者的窗口。
4. 不许把 §三.0 的预期列改成实测值；实测与预期不符（含 0 枚红、含多出一枚红）⇒ 那一轮作废并如实登记。
5. 不许把波 13 老 TRX 的红署成本刀的红（**本刀的凭证＝本轮在载体里跑出来的那份 TRX**）；不许拿"波 22 已在盘上读到过红"当成交付。
6. 不许把 D 类那 28 枚里任何一枚顺手跑一轮"看看响不响"（§四.1）；不许拿 HDS-009 那两棵判据文件做本刀的自证面以外的任何写。
7. 不许删作废轮产物、不许清空结果目录重跑、不许在报告里替守门人写状态词／结案词／自封在册基线。
8. 不许碰他人未提交改动、暂存项、未跟踪资产；共享主树禁裸 `git commit`。预合成文本的 SHA 对账**不能替代并排自证**（波 37／38 教训：SHA 只证明复制到盘上的字节＝声明的字节，不证明声明的东西是对的）。

## 八 交回与划账（本 lane 写 §〇–§七，守门人追加 §九）

（守门人在此预登记划账面：① 九轮形状与红名归属逐格点名，M2＝恰 3、M8＝恰 4，其余各恰 1，任何连坐都整轮作废并登记为"波 18 新格连坐面"发现；② **14 枚的换手账**——判据字节九轮全程未动 ⇒ 本刀具备签资格，签与不签由 §九.2 逐轮点名（预期＝14 枚结清：波 13 的 #1–6、#13–15、#22、#24–27）；③ 旧欠 75 → 61 的等式要当场从盘上产物写出求和式，不许心算（14 枚减账的每一枚都要有一轮清洁红作凭）；④ 1406 是否第五次同值复采（本刀不顶账）；⑤ `.trx` 13 枚对账与构建跳过三段；⑥ 定向 9 轮／`dotnet` 10 次是否越预算，HOLD 额度是否被用成放宽；⑦ 第三选项没用上就照实写"未动用"；⑧ 出闸后的账面目：波 13 的 B 类由 19 → 5、D 类 28 未动、波 20 的 28 未动，**HDS-009 那 26 枚与本刀无关**，本刀不新增格子。报告只追加 §九，§〇–§七 一字不改，删除列恒 0。）
