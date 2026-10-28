# 2026-10-29｜HDS-WAVE39-1029｜旧欠 75 格的第一片＝波 13 的 B 类 14 格换人复采（九枚单行突变·另取一双手）——冷启动 lane 交付面

- 工单＝`docs/dispatch-hds-wave39-1029.md`（122 行编号行，逐字读完；本 lane 只写 §〇–§七，§九 由守门人追加）
- 性质＝证据刀·换手复采（不是新原型、不是接线、不是新判据；净新增判据格 0、产品字节终态净写入 0）
- 执行方＝冷启动 lane（另取的一双手，非判据作者）
- 交付路径＝本报告（主树里本刀唯一新建文件，不提交、不 `git add`）

## 〇 开工前必读与入场

### 〇.1 八份必读（逐份当场读完，缺一条即停刀；本条齐）

| 份 | 盘上路径 | 行数 | 本刀取走的要点 |
|---|---|---|---|
| 1 | `docs/plans/README.md` | 50 | 登记处唯一＝`docs/plans/PROJECT_PLAN.md`；lane 不自开任务号 |
| 2 | `docs/plans/PROJECT_PLAN.md` | 913（只读 §4.2 的 HDS-003／HDS-010 行＋§9 最新一条 entry） | §4.2 HDS-003 行原文＝「本把开第一片＝`docs/dispatch-hds-wave39-1029.md` **在飞**」；§9 最新 entry（2026-10-29 01:2x 收第三十八波）末句＝「在飞＝1（`HDS-WAVE39-1029`）」 |
| 3 | `docs/agent-engineering-standards.md` | 214（全文） | §10 交付四项（做了什么／验证到哪／未验证与欠账／下一步）＋§7「未验证必须标成未验证」；本刀交付面按此形 |
| 4 | `docs/eval/reports/2026-10-29-hds009-wave38-assert-face-and-scan-bite.md` | 445 | 十一项写法模板＋守门人 §九.0「已知读数表每面必须带四元限定」＋§九.1 第 6 条基线读数（见 §〇.5）＋§九.5 押账（本刀出处） |
| 5 | `docs/eval/reports/2026-10-23-hds003-005-cell-evidence-audit.md` | §三.1／§四.1 | 65 格逐格凭证表；B 类＝盘上有单枚清洁红但缺字段且红出自判据作者的手；T1–T5 复采梯队，本刀＝T2 |
| 6 | `docs/eval/reports/2026-10-23-hds003-005-second-hand-recount-prototype.md` | §一–§三 | 上一把 24 枚换人复采的成对门与十一项写法；L158＝本刀出处「旧第二换手凭证对现网字节作废、须在现字节上以另一双手重取」 |
| 7 | `docs/eval/reports/2026-10-23-hds003-narrator-prototype.md` | §三.1–§三.15 | 九枚突变的原始配方（波 13 当年 m01–m15；老红集 1/3/1/1/1/1/1/4/1；当年 counters total=49） |
| 8a | 判据本体 `tests/Pry.Core.Tests/NarratorPrototypeTests.cs` | 989 | 54 枚方法／56 枚格；三计数 53／1／3；签名两族 24 枚 `public void`＋30 枚 `public async Task`（另两枚 `public async Task<NarratorOutcome> ProposeAsync` 在 `:130`／`:874` 属假 provider，不是格） |
| 8b | 三枚被改坏的产品文件 `src/Pry.Core/Narrative/NarrativeContextCompiler.cs`（470 行）／`NarratorContracts.cs`（124 行）／`NarrativeRoundRunner.cs`（341 行） | — | 逐枚核对 §三.0 的「改前整行」在盘上现文（见 §三.0R 件 1）；并读改动行邻域以定消费点 |
| 9 | `.local-notes/wave39-1029/mutation-prep/`（九枚）＋`gk39-prep.js`（103 行）＋`out/gk39-prep.txt`（92 行） | — | 守门人预合成文本与并排自证原文（lane 只读；本刀 §三.0R 五件另由 lane 亲手重算，不抄该推演） |

### 〇.2 入场读数（当场取，整段存档 `out/entry-readings.txt`；未引用工单里的任何数字）

```
date＝2026-10-29T01:57:57+08:00｜uname＝MINGW64_NT-10.0-26200 PhythLink 3.6.6-1cdd4371.x86_64 2026-01-15 22:20 UTC x86_64 Msys
git rev-parse HEAD（主树）＝0a9ce00c674b48c1631890fba9abe3806c013bb5
git status --short --branch（原样，8 行）：
## codex/ui-main-window-redesign
M  docs/dispatch-tip1020-02-memory-group-judgment.md
 M docs/plans/PROJECT_PLAN.md
M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs
?? .qoder/
?? docs/character-learning-safety.patch
?? docs/research/HDS_INTERLUDE.md
?? docs/research/HDS_QQ_RESEARCH_INDEX.md
?? docs/research/QQ_BRIDGE.md
git worktree list＝77 行（首行＝主工作树 0a9ce00 [codex/ui-main-window-redesign]；本刀载体行＝
  C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave33-1028/carrier  51f149d (detached HEAD)）
git log --oneline -3＝0a9ce00 / db064e6 / …（三条均为文档刀，非本刀）
工具链：dotnet --version＝10.0.200｜Python 3.14.5｜node v24.20.0
```

工单 §〇.7 列的他人资产＝那两枚 `M ` 暂存项＋全部 `??`；入场读数比该清单**多一行** ` M docs/plans/PROJECT_PLAN.md`（未暂存、他人在飞的登记行，波 38 当年也见到同形状）。本刀未读其内容、未改、未提交、未 `stash`／`clean`／`rm`，原样登记。

### 〇.3 载体入场自证五件（载体＝沿用第三十三波那棵，基点 `fd93ebe`，线端 `51f149d`；不新建 worktree）

| 件 | 命令 | 当场读数 | 判定 |
|---|---|---|---|
| ① | `git -C <carrier> rev-parse HEAD` | `51f149d0b722e28a6566c93d19f48a94ca4c85df` | 线端＝工单声明的 `51f149d` |
| ② | `git -C <carrier> status --short` | **0 行** | 干净 |
| ③ | `git -C <carrier> log --oneline fd93ebe..HEAD` | **2 行**（`51f149d` test: wave33 keep each mutation a single clean red…／`1e02205` test+feat: HDS-009 wave33 isolated prototype…） | 仍只有波 33 那两枚提交，本刀零提交 |
| ④ | `git -C <carrier> diff --stat fd93ebe HEAD -- src tests scripts Pry.slnx Directory.Build.props` | 末行＝**6 files changed, 822 insertions(+)**，删除列 0 | 与工单 §〇.3 ④ 同形 |
| ⑤ | `git -C <main> diff --stat 51f149d $(git -C <main> rev-parse HEAD) -- src tests scripts Pry.slnx Directory.Build.props`（右端当场取＝`0a9ce00c674b…`，不在工单里预写） | **0 行** | 成立 ⇒ 载体树与主线归位后的产品／判据字节逐字符相同 ⇒ 本刀在载体里取到的红就是主线那棵树的红 |

⑤ 成立后的三段判定（工单 §一.1 的解决方案级构建跳过）：
- 腿①＝§〇.3 ⑤ 的 0 行式子成立（上表当场读到 0 行）；
- 腿②＝载体 `src/Pry.Core/bin/Release/net10.0/Pry.Core.dll` 在盘＝1,745,920 字节，mtime 2026-10-29 00:46（波 38 收尾全量留下的那枚）；
- 腿③＝六枚 SHA 未变（见 §〇.4，三侧逐字符相同）。
⇒ 三段齐 ⇒ **解决方案级构建跳过**（预算 1／动用 0，全程未另起构建；九轮定向一律不带 `--no-build`，编译由 `dotnet test` 自己按需增量做）。

### 〇.4 六枚文件入场 SHA-256（lane 当场自测三侧各一次；`out/entry-sha-6files.txt`＋本文件 §三.P 后的 `six-sha` 复核）

| 文件 | 主树工作副本＝载体工作副本＝`git show 51f149d:<path>` blob | 三侧判定 |
|---|---|---|
| `src/Pry.Core/Narrative/NarrativeContextCompiler.cs` | `86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842` | THREE-WAY-MATCH |
| `src/Pry.Core/Narrative/NarratorContracts.cs` | `3a33a8151e61c3396963dac451dffcb46acb2f02807f140d18f694d6a52a9f8c` | THREE-WAY-MATCH |
| `src/Pry.Core/Narrative/NarrativeRoundRunner.cs` | `961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9` | THREE-WAY-MATCH |
| **判据** `tests/Pry.Core.Tests/NarratorPrototypeTests.cs` | `4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4` | THREE-WAY-MATCH |
| `tests/Pry.Core.Tests/NarrativeCommitFactoryTests.cs` | `fcee461164e39bae172c49149d39076e67aa36e844089afa4bcf4ff06d7cea0b` | THREE-WAY-MATCH |
| `tests/Pry.Core.Tests/NarrativeBudgetInequalityCellTests.cs` | `192bbaecb5d2a3f3bafd08d8cbe5fdacf65ee71a7ad7856798345eae6c3f7686` | THREE-WAY-MATCH |

判据那枚入场＝ `4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`（工单要求全程等于 `4f1b2f6e…`；九轮 18 次读数见各轮 ⑩ 与 §三.0R 件 5）。判据三计数入场面（主树／载体／blob 三侧同值）＝`[Fact]` 53／`[Theory]` 1／`[InlineData` 3／989 行。

### 〇.5 在册基线起点（当场从波 38 报告守门人 §九.1 第 6 条读到的那个数，原文照抄）

> 「收尾全量＝**1406 的第四次同值复采**：四份先数满再逐份 `<Counters>` 当场求和＝108＋785＋280＋233＝**1406／executed 1404／failed 0**；两条跳过按 `outcome="NotExecuted"` 开标签逐字符点名＝`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline` 与 `Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`，与波 28–37 同名同枚；四份的 `notExecuted` 计数器仍恒 0、缺口只以开标签点名且 `total−executed=2` 自洽＝同一形状第四次复现（仪器口径，不是缺陷）。」

本刀净新增格数＝0 ⇒ F 轮出口读数应当仍是 **1406／executed 1404／failed 0／跳过 2**；读到别的数先怀疑树没还原干净，不改判据、不改基线、照实登记并作废那一轮。本刀**不顶账**、不为顶账另起构建。

### 〇.6 本刀的坐标系（账读对再动手）

- 旧欠 **75 格**＝波 13 余 47＋波 20 余 28（现账出处＝波 24 报告 §四.2＋波 38 报告 §五/§六「照旧挂着」）。
- 波 13 那 47 枚在第二十二波 §三.2 分成 **B 类 19 枚**（#1–11、#13–15、#22、#24–27）＋ **D 类 28 枚**。本刀只碰 B 类里的 **14 枚**（＝九轮命中的 14 枚格），D 类一枚不碰；B 类剩 5 枚（波 13 的 m07–m10 那四枚门所守的格）押下一把，理由＝红集不可静态定死（那四枚门正是波 18 补格所钉的 `:203`／`:239`／`:246`／`:284`，三响以上必须事前点名每一枚格名）。
- 本刀性质＝「同一枚字节改坏、换一双手再响一次」；凭证＝**判据字节全程一个字节未动**（九轮 18 次读数逐字符 `4f1b2f6e…`），这是 14 枚能签第二换手资格的唯一凭据。
- 波 20 那 28 枚不混编；HDS-009 那两棵判据文件只作 §一.5 自证面（读，不写）。

### 〇.7 他人资产与 git 红线（本刀动用登记）

共享主树里 `git commit`／`add`／`checkout`／`reset`／`churn`／`cherry-pick`／`merge`／`tag`／`push`／`git apply`／`stash`／`clean`／`rm` ＝ **0 次动用**（全程只用 `git show`／`diff --stat`／`log`／`status`／`rev-parse`／`worktree list` 只读命令）。写入面＝载体内三枚产品文件（九轮临时覆盖，逐轮按字节还原）＋本报告＋`.local-notes/wave39-1029/`。`%LOCALAPPDATA%\PryCompanion` 零接触；未杀任何进程。

**本刀一处自曝（如实登记，不掩盖）**：`lane39-round.js` 的正面控件第一次执行（01:52:38）时，其还原步骤把 blob 名漏了 `src/` 前缀 ⇒ `git show 51f149d:Pry.Core/…` 报 `path … does not exist`，控件写的 M1 突变字节因此在载体里停留约 2 分钟。处置＝当场用 `node lane39-round.js revert src/Pry.Core/Narrative/NarrativeContextCompiler.cs`（`git show` 取字节再写文件，未用 `checkout`／`restore`）还原，SHA 回到 `86c3…9842`＝§〇.4 基准，载体 `status --short`＝0 行；该过程**零 `dotnet` 参与**、判据字节未被写（`4f1b2f6e…` 前后同值）。缺陷已修（blob 名补 `src/`），守卫实证随后重跑一遍干净的（`out/guard-proof.txt`，01:53:02）。教训面＝「仪器自身也要自证」这条不只对取数仪器成立，对写入仪器同样成立。

### 〇.8 焦点预检规则（本刀口径）

每一次 `dotnet`（含每一枚突变轮与收尾全量）之前先跑
`powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1`（脚本在盘＝9,675 字节），整段输出贴进报告并归档 `.local-notes/wave39-1029/out/preflight-<ID>.txt`。守门人已跑并归档 **P95**（`out/preflight-P95.txt`，`VERDICT=BUILD_OK`），**本刀从 P96 起编号**。只有 `BUILD_OK` 才起跑；读到 `HOLD` ⇒ 不起跑、不请示、不改判据、不绕过，先把零 `dotnet` 的活做完（本刀已全部做完，见 §一／§二／§三.0R）再重跑预检；**HOLD 重跑最多累计 16 次、相邻两次不短于 5 分钟**；16 次后仍 HOLD ⇒ 带「零 dotnet 起跑」或「只跑到第 N 轮」回来，那是合法交付。`free_memory_gb` 偏低或读数呈 SQLite `disk I/O`／`out of memory` 形态 ⇒ 那一轮读数作废（他机器资源形状，非断言失败），产物改名 `trx-<轮>-voided-<预检号>` 留档，不清空、不重跑、不动进程、不改判据。

## 一 仪器（口径⓪：先自证再用；本刀三份全部 lane 自写，零引用前波实现）

### 一.1 取数仪器 `lane39-count.js`（228 行，本刀自写）

口径逐条对齐工单 §一.4：① 只数 `<UnitTestResult … outcome="Failed">` 的**开标签**（标签名判定＝`<UnitTestResult` 后必须跟空白／`>`／`/>`，避免把 `<UnitTest>` 定义节点误计）；② 反向锚定面＝抓 `outcome="Failed"` 回溯最近 `<` 判标签名，`<ResultSummary>` 不算用例，两路面必须同值；③ 红名先 `split('(')[0].split('.').pop()` 剥参数表与全限定前缀，再回判据验「方法名＋`(`」存在；④ 比对预期红名先剥前缀再比；⑤ 抽栈帧**不锚 `at `**，正则＝`([\w.\-]+\.cs):(?:line |：行 )(\d+)`；⑥ 属性**按名字取**（`attr(head,'testName')` 式，不按顺序拼正则——本树 TRX 的 `testName` 就排在 `outcome` 之前）。方法名抽取的正则**同时认两族签名**（`public void`／`public async Task`，并带 `Task<…>` 返回型容错），波 38 那枚只认 `void` 的仪器搬到这里会把 30 枚判成「判据里不存在」，本刀不用那枚。

一处本刀实测修正（盘上八份 TRX 全数复现，非缺陷、是写手形态）：`<Counters notExecuted="0">` 恒为 0，即使同份里有 2 枚 `outcome="NotExecuted"` 开标签 ⇒ 非执行枚数的裁决口径＝**开标签数＝total−executed 自洽**，属性值只原样登记（`RAWATTR` 行）。这条与波 38 §九.1 第 6 条那句「四份的 `notExecuted` 计数器仍恒 0、缺口只以开标签点名且 `total−executed=2` 自洽」同一形状，本刀独立复现。

### 一.2 仪器自证（`lane39-selfproof.js`；整段原文＝`out/instrument-selfproof.txt`）

十六面逐条判定，每面带**四元限定**（波次＋判据文件＋跑法＋该面结论字段值）；`notExec` 期望只由跑法决定，不由「这是突变轮」的身份决定。逐面结论（全文见归档，此处给判定表）：

| 面（四元限定） | 读到 | 判定 |
|---|---|---|
| 波 13 `run-m01`｜`NarratorPrototypeTests.cs`｜定向（旧 49 格树）｜failed=1 | 开标签红 1／Counters failed 1／total 49／红名 `Compiler_rejects_a_negative_section_share`（判据起始行 347，族 `public void`，帧 `:352`，消息 `Assert.False() Failure`） | AGREE（8 项子读数全 AGREE） |
| 波 13 `run-m02`｜同上｜定向｜failed=3 | 1 份／红 3／total 49／三枚名逐字符等于工单 §一.5 那三枚 | AGREE |
| 波 13 `run-m14`｜同上｜定向｜failed=4 | 红 4／四枚名逐字符等于工单 §一.5 那四枚 | AGREE |
| 波 13 `run-re-m02`｜同上｜守门人重取 定向 同一判据集｜failed=3 | 红 3，红名逐字符等于 `run-m02` 那三枚 | AGREE |
| 波 13 `run-re-m14`｜同上｜守门人重取 定向 同一判据集｜failed=4 | 红 4，红名逐字符等于 `run-m14` 那四枚 | AGREE |
| 波 13 `run-m03`／`m04`／`m05`／`m06`／`m11`／`m15`｜同上｜定向｜各恰 1 | 六面各读到红 1／total 49；红名分别为 `Compiler_refuses_a_scene_without_identity…`@247／`Compiler_refuses_a_current_event…`@256／`Compiler_refuses_the_round_when_the_mandatory_prelude…`@225／`Compiler_fails_loudly_when_the_reserved_directive…`@266／`Second_call_on_the_same_round_budget_is_refused`@408／`Deferred_round_registers_the_intent…`@503 | AGREE（六面各 8 项子读数） |
| 波 13 `run-full`｜四棵判据｜解决方案级全量｜工单该行写「failed 全 0」 | 4 份 TRX／开标签 1228＝Counters total 求和／Core 那份 total=607／executed=605／`NotExecuted` 开标签 2 枚点名＝`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`、`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`；**failed 求和＝2**＝`ResourceLimitLedgerCoverageTests` 的 `Every_limit_shaped_constant_has_a_row_in_the_ledger`（帧 `:50`）与 `The_guard_sees_a_row_being_dropped_and_a_row_drifting_off_its_line`（帧 `:110`），该份 `<Counters failed="2">` | **差额登记**（见下） |
| 波 38 `trx-M5c`／`trx-S1`／`trx-S2`｜`StateAndLearningPrototypeTests.cs`（另一棵判据，26 格）｜定向｜各恰 1 | 三面各读到红 1／total 26；红名 `A_candidate_touching_all_three_surfaces…`（帧 `:245`）／`State_and_learning_prototype_sources_carry_no_legacy_chain_names…`（`:320`）／`State_and_learning_prototype_declares_no_source_side_numeric_default`（`:344`）；三面红名**均不属于本刀 14 枚**、且在 `NarratorPrototypeTests.cs` 内不存在 | AGREE |
| 波 38 `trx-F`｜四棵判据｜解决方案级全量｜1406／1404／0／跳过 2 | 4 份 TRX／开标签求和 1406／executed 求和 1404／failed 求和 0／`NotExecuted` 开标签 2 枚（同名同枚，点名见归档） | AGREE |

**自证总结论＝`INSTRUMENT-OK`**：16 面逐条 AGREE，唯一非 AGREE 面是一处**工单预期面与盘上产物的差额**，已用「开标签＋`<Counters>`＋反向锚定」三路同值确认＝盘上事实（波 13 那次全量在 280 枚那一棵里确有 2 枚红，正是后来被结清的账），**不是仪器缺陷**。按工单 §一.5 尾句处置＝「任一不符＝仪器坏了，先修仪器，不改判据、不改盘上产物」：本刀未改盘上任何 TRX、未改工单 §一.5 那行、未把该面当自证面用（本刀裁决只用它与本刀同判据集／同跑法的九面）。仪器修过的两处（`notExecuted` 属性口径、写后 SHA 的取样次序）都在自证之前完成，自证面读数取修后那一版。

### 一.3 写入通道仪器 `lane39-round.js`（含 §一.8 判据目录硬拒守卫）

合法 ID→产品文件的映射逐字抄 §三.0（M1–M6→`NarrativeContextCompiler.cs`、M7→`NarratorContracts.cs`、M8／M9→`NarrativeRoundRunner.cs`）；`apply` 唯一取字来源＝`mutation-prep/<ID>_<basename>`（该目录文件名唯一性由仪器断言）⇒ **整文件字节复制**，无手写 replacement、无重排、无行尾改动；应用后立即 `sha256sum` 并与 §三.0 声明的突变 SHA＋字节数双面对账；还原一律 `git show 51f149d:<path>` 取 blob 字节再写文件（**未用 `checkout`／`restore`**），还原后 SHA 必须等于 §〇.4 基准。守卫裁决点＝所有写路径唯一经过的 `guard()`：`/tests/` 面硬拒、非载体硬拒、非 §二 ③ 那三枚硬拒、非 mutation-prep 来源（手写字节）硬拒。

### 一.4 守卫实证整段原文（`out/guard-proof.txt`，2026-10-29T01:53:02+08:00；两次 REFUSED＋一枚正面控件，全部当场真验）

```
--- (a) 合法 ID（W39-M1）的产品文本 → 判据文件（主树侧＋载体侧各一次）
动作＝apply:W39-M1→判据｜目标＝C:/Users/24782/Desktop/Pry_Project/tests/Pry.Core.Tests/NarratorPrototypeTests.cs｜来源＝…/mutation-prep/W39-M1_NarrativeContextCompiler.cs
  裁决＝REFUSED 拒写理由＝判据／测试面（本刀 0 写入，§二 ④）；不在载体内（写只许落在载体，§〇.7／§二 ③）
  目标 SHA 写前＝4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4
  目标 SHA 写后＝4f1b2f6ed68718f62522099d29933a285818f4｜前后同值＝YES
  结论＝REFUSED-AND-UNTOUCHED（硬拒真验出）
动作＝apply:W39-M1→载体判据｜目标＝…/carrier/tests/Pry.Core.Tests/NarratorPrototypeTests.cs｜来源＝同上
  裁决＝REFUSED 拒写理由＝判据／测试面（本刀 0 写入，§二 ④）；非 §二 ③ 那三枚产品文件：tests/Pry.Core.Tests/NarratorPrototypeTests.cs
  目标 SHA 写前＝4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4
  目标 SHA 写后＝4f1b2f6ed68718f62522099d29933a285818f4｜前后同值＝YES
  结论＝REFUSED-AND-UNTOUCHED（硬拒真验出）
--- (b) 手写字节（out/raw-attempt-line.txt，内容＝一枚自写的「if (total <= 0 || total < 7)」行）→ src/Pry.Core/Narrative/NarrativeContextCompiler.cs
动作＝raw｜目标＝…/carrier/src/Pry.Core/Narrative/NarrativeContextCompiler.cs｜来源＝.local-notes/wave39-1029/out/raw-attempt-line.txt
  裁决＝REFUSED 拒写理由＝手写字节／非 mutation-prep 来源（§一.8 禁手写 replacement）
  目标 SHA 写前＝86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842
  目标 SHA 写后＝86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842｜前后同值＝YES
  结论＝REFUSED-AND-UNTOUCHED（硬拒真验出）
--- 正面控件：合法 ID（W39-M1）＋合法文本＋合法产品文件（用后立即还原）
动作＝apply:W39-M1｜目标＝…/carrier/src/Pry.Core/Narrative/NarrativeContextCompiler.cs｜来源＝…/mutation-prep/W39-M1_NarrativeContextCompiler.cs
  裁决＝ALLOWED
  目标 SHA 写前＝86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842
  目标 SHA 写后＝a20eb74de3c38734c596e088c18dad67195283d7a2486f1462f9f924f051dee0｜前后同值＝NO（写入真的发生了）
  结论＝ALLOWED-AND-WRITTEN（正面控件：整文件字节复制真的落盘）
  控件落盘 SHA＝a20eb74de3c38734c596e088c18dad67195283d7a2486f1462f9f924f051dee0｜等于 §三.0 声明＝YES（整文件字节复制真的生效）
  控件用后立即还原：SHA＝86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842｜等于 §〇.4＝YES
--- 控件后载体 status 行数＝0
--- 控件后六枚入场 SHA 三侧复核＝六枚全部 THREE-WAY-MATCH＝§〇.4｜载体相对基线写入＝0 字节
--- 判据文件三侧 SHA 单列复核＝主树工作副本／载体工作副本／blob 均＝4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4
```

判定＝(a)(b) 两次**真验出 REFUSED** 且目标文件 SHA 前后同值；正面控件**真放行**（落盘 SHA 等于 §三.0 声明的突变 SHA）并当场还原。守卫不是声称。

### 一.5 复算仪器 `lane39-recompute.js`

`parallel`／`static`／`consumers`／`crossface` 四个面，见 §三.0R。

## 二 允许的文件面（本刀实际写入清单，逐处点名）

- ① `.local-notes/wave39-1029/`：新增 `lane39-count.js`、`lane39-selfproof.js`、`lane39-round.js`、`lane39-recompute.js` 与 `out/` 下 `entry-readings.txt`、`entry-sha-6files.txt`、`instrument-selfproof.txt`、`guard-proof.txt`、`raw-attempt-line.txt`、`recompute-item1-parallel.txt`、`recompute-item2-consumers.txt`、`recompute-item3-crossface.txt`、`recompute-item4-static.txt`、各轮预检与运行归档；`mutation-prep/` 九枚、`gk39-*.js` 与 `out/gk39-prep.txt`＝守门人产物，本刀**只读**（未改一字节）。
- ② 本报告 `docs/eval/reports/2026-10-29-hds13-bclass-second-hand-recount.md`（唯一可写文档，不提交、不 `git add`）。
- ③ 载体内三枚产品文件的九轮临时覆盖＋逐轮按字节还原（终态相对 `51f149d` 净写入 0 字节）。
- ④ 未写：任何 `tests/**`、`scripts/**`、`Pry.slnx`、`Directory.Build.props`、其他 `docs/**`、主树任何文件。§三.0 的预期列**未被本刀改成实测值**（§三.0R 件 2／3 的两处复算差额只登记在复算节，未回写工单或预期表）。

## 三 九轮（每轮十一项，逐轮追加；跑序固定 M1→…→M9→全部还原并核六枚 SHA→F）

### 三.0R 本刀亲手重做的独立复算（零 `dotnet`，先档后跑）

**件 1 逐枚并排自证**（`out/recompute-item1-parallel.txt`）：把 `mutation-prep/` 九枚与 `git show 51f149d:<path>` 的 blob 逐枚 diff ⇒ 九枚全部：改动面行数＝1、改动行号＝§三.0 声明行号（54／62／71／78／87／110／107／115／155）、blob 现文与工单「改前整行」逐字符 SAME、prep 改文与工单「改后整行」逐字符 SAME。⇒ **九枚齐，无一枚有多行改动或行号漂移面**。（波 37「明知语法坏还跑一轮」＋波 38「SHA 对账不替代并排自证」两道教训的合并执行：SHA 只证明复制到盘上的字节＝声明的字节，这里的并排 diff 才证明声明的东西与盘上一致。）

**件 2 消费点复算**（`out/recompute-item2-consumers.txt`）：判据 56 枚格按两族签名正则全数点名＝方法 56 枚（其中带 `[Fact]`／`[Theory]` 属性者 **54 枚**＝24 枚 `public void`＋30 枚 `public async Task`，不带属性的 2 枚＝`:130`／`:874` 的 `public async Task<NarratorOutcome> ProposeAsync` 假 provider）；API 落点分布＝直调 `.Compile(` 21 枚、`RunAsync(` 30 枚、`.Validate()` 2 枚、`.Consume()` 1 枚、只读装配体属性面（`WithinUnifiedBudget`）2 枚。逐枚判定各门消费者后与 §三.0 预期列对账：**七枚门 SAME-AS-TICKET（M2／M3／M4／M5／M6／M7／M9）**；两处差额：
- **M1（`:54` 负份额门）**：粗面（断言 `NarrativeCompileReason.InvalidBudget` 的格）命中 2 枚＝`Compiler_rejects_a_negative_section_share`@347＋`Compiler_rejects_a_window_of_zero_tokens`@357。**lane 细化判定＝1 枚**：`:52` 那一支的第一条件 `budget.ContextTokens < 1` 未被本刀改动，@357 的夹具是 `ContextTokens: 0` ⇒ 仍走 `InvalidBudget`，与该枚断言同值 ⇒ 不受 M1 影响；@347 的 `StateTokens: -1` 只经 `.Any(x => x < 0)` 那一支 ⇒ 突变后不再拒。与波 13 盘上 `run-m01` 恰 1 枚的形状同值。
- **M8（`:115` ProviderRejected）**：粗面（断言 `NarrativeRoundDisposition.ProviderRejected` 的格）命中 **5 枚**＝工单 §三.0 那四枚＋**`Provider_side_cancellation_with_a_live_host_token_is_named_timeout_not_failure`@851**。**lane 细化判定＝5 枚**：该格 provider 在自己的内部令牌上取消、宿主令牌与超时界都活着 ⇒ `InvokeOnceAsync` 产出 `Kind=TimedOut`（≠`DraftProduced`）⇒ 必进 `:110` 的 `if` ⇒ 从 `:113-117` 返回，`:115` 的 `Disposition` 正是本刀唯一改动的那个常量；该格 `:865` 断言 `Assert.Equal(NarrativeRoundDisposition.ProviderRejected, result.Disposition)` ⇒ 突变后必红（`:864` 的 `Kind` 断言不受影响）。
  **这条是预判不是结论**：§三.0 预期列原样不动（＝4），本刀仍按固定跑序起 M8，以载体里跑出的那份 TRX 为裁决。若实测＝5 枚 ⇒ **整轮作废、M8 那四枚红不署、不追第二轮**，并把多出的那名登记为「波 18 补格与波 13 老门的连坐面」发现（下一把 D 类复采要用的信息，不是本刀的账）；若实测＝4 枚 ⇒ 上述细化判定被盘上读数否证，同样如实登记、按 4 枚署。
  附带盘面事实：判据 `:823` 之后的波 18 追加段带属性的格＝**7 枚**（`Mixed_round…`@832、`Provider_side_cancellation…`@851、`Unified_budget_inequality…`@890、`Whole_section_segments…`@913、`Absent_sections…`@934、`Unit_run_losing_units…`@952、`Unit_run_that_fits_no_unit…`@975），49＋7＝56 ⇒ 工单 §三.0R 件 2／3 那句「波 18 新增的五枚」在盘上是七枚，前两枚（@832／@851）不在该清单里；本刀的交叉面因此按七枚逐枚复算，而不是按五枚。

**件 3 交叉面自扫**（`out/recompute-item3-crossface.txt`，独立复算，未抄守门人推演）：七枚补格逐枚判「本轮改的那一行读不读得到」——
- `Unified_budget_inequality…`@890：不调 `Compile`、不调 `RunAsync`（`直接构造装配体=true`）⇒ 九轮九条门对它全部不可达 ⇒ 不连坐。
- `Whole_section_segments…`@913／`Absent_sections…`@934／`Unit_run_losing_units…`@952／`Unit_run_that_fits_no_unit…`@975：四枚都直调 `Compile`，但断言常量面只有 `NarrativeSegmentDisposition.*`，无 `InvalidBudget`／`WindowTooSmall`／`MandatoryContentMissing`／`MandatoryExceedsBudget`／`ReservedDirectiveDoesNotFit`；夹具面＝Budget 覆盖仅 `BehaviorTokens: 10`／无／`RecentTokens: 20`／`RecentTokens: 5`（全部 ≥0，`:54` 那支前后同值），默认 `ContextTokens 4096−OutputTokens 512−SafetyTokens 128＝total 3456>0`（`:62` 门空转），`identity`／`currentEvent` 走默认非空（`:71`／`:78` 不拒），`advisory` 默认 `""` ⇒ `:106` 分支不进入 ⇒ `:87`／`:110` 的判定结果突变前后不变；`identity` 实参面为空。⇒ 四枚对 M1–M6 全不连坐。
- `Mixed_round_takes_the_immediate_branch…`@832：`RunAsync=true`、断言常量只有 `NarrativeRoundDisposition.Immediate`；`:150` 的即时支在 `:155` 延迟支之前返回 ⇒ M9（`deferred.Length > 1`）改不动它的返回路径；M7/M8 面：单次 `Consume` 与 `DraftProduced` 正常路径 ⇒ 不连坐。
- `Provider_side_cancellation…`@851：见件 2 的 M8 细化 ⇒ **唯一一枚预判会连坐的格**（对 M1–M7、M9 不连坐）。
⇒ 除 M8 那一枚外，九轮预期枚数＝波 13 老红集原样（1/3/1/1/1/1/1/4/1）。

**件 4 静态语法判读**（`out/recompute-item4-static.txt`）：九枚逐枚判改后行净括号偏移与全文件收支——行净偏移差（小／方／大括号）九枚全部＝`{0,0,0}`；全文件括号与大括号收支九枚「UNCHANGED」；行数变化九枚全部＝0（471／471、125／125、342／342 含末行换行符的 split 计数）；判读＝九枚 **CLOSED**，无一枚引入未闭合结构 ⇒ **九轮全部可起跑，第三选项（不起那一轮）未动用**。M1 那一行改前后同为 `小括号-1／大括号-1`（多行语句的续行），守恒；M4 改后 `|| false)` 同为 `-1`，守恒。

**件 5 判据字节三面自证**：起跑前＝主树工作副本／载体工作副本／`git show 51f149d:` blob 三侧同值 `4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`（§〇.4＋`out/guard-proof.txt` 尾）；此后每轮 apply 侧一次＋还原后一次，九轮 18 次读数逐条登记在各轮 ⑩。

## 三.1 轮 M1（W39-M1）——十一项

| 项 | 读数 |
|---|---|
| ①文件:行 | `src/Pry.Core/Narrative/NarrativeContextCompiler.cs:54`（载体内） |
| ②改前原文 | `                budget.ExpressionTokens, budget.CapabilityTokens, budget.AdvisoryTokens }.Any(x => x < 0))` |
| ③改后原文 | `                budget.ExpressionTokens, budget.CapabilityTokens, budget.AdvisoryTokens }.Any(x => x < 0 && x > 1000))`（＝`mutation-prep/W39-M1_NarrativeContextCompiler.cs` 整文件字节复制，非手写） |
| ④`git diff --stat` | `src/Pry.Core/Narrative/NarrativeContextCompiler.cs \| 2 +-` → `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤跑法整行 | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~NarratorPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave39-1029/out/trx-M1" --logger trx`（载体内，无 `--no-build`） |
| ⑥TRX 文件名 | `24782_PHYTHLINK_2026-10-29_02_01_22.trx`（枚数＝1，满额形状 1×1） |
| ⑦红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.NarratorPrototypeTests.Compiler_rejects_a_negative_section_share`（剥前缀＝`Compiler_rejects_a_negative_section_share`，在判据内＝true，族＝`public void`，判据起始行 347）；消息＝`Assert.False() Failure / Expected: False / Actual: True`；红点帧（不锚 `at `）＝`NarratorPrototypeTests.cs:352`；红形状＝**ASSERT-RED（该格承重断言失败）**；开标签红＝1／`<Counters failed="1">`＝1／反向锚定 `UnitTestResult`=1＋`ResultSummary`=1（后者不算用例）／total＝**56**／executed＝56／notExecuted 开标签＝0（total−executed＝0 自洽） |
| ⑧还原命令 | `node lane39-round.js revert src/Pry.Core/Narrative/NarrativeContextCompiler.cs`（＝`git show 51f149d:src/Pry.Core/Narrative/NarrativeContextCompiler.cs` 取字节再写文件，未用 `checkout`／`restore`） |
| ⑨还原后 `git status --short` 行数 | **0 行**（载体） |
| ⑩SHA | 突变态＝`a20eb74de3c38734c596e088c18dad67195283d7a2486f1462f9f924f051dee0`（＝§三.0 声明，24469 字节亦等）；还原后＝`86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842`（＝§〇.4）；**本轮判据 SHA** apply 侧＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`（主树侧＋载体侧双读同值）、还原后＝同值 ⇒ 判据字节 0 变动（换手凭据面成立） |
| ⑪当轮预检 | **P96**（`out/preflight-P96.txt`，02:00:52）整段：`FOREGROUND: ChatGPT pid=16348 class=Chrome_WidgetWin_1 style=0x14C70000 rect=-1748,263,1445x811 screen=1920x1080 covers=False borderless=False title=ChatGPT`／`UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404`／`gpu_util=9% used=2046MB`／`free_memory_gb=13.7`／**`VERDICT=BUILD_OK`** |

裁决＝**恰 1 枚清洁红，红名逐字符等于 §三.0 预期列**（`Compiler_rejects_a_negative_section_share`@347）⇒ 本轮有效；§三.0R 件 2 那条 M1 粗面 2 枚的细化（@357 仍走未改的 `ContextTokens < 1` 那一支）被盘上读数证实。整段原始输出＝`out/round-M1-run.txt`＋`out/round-M1-count.txt`＋`out/round-M1-revert.txt`。

## 三.2 轮 M2（W39-M2）——十一项

| 项 | 读数 |
|---|---|
| ①文件:行 | `src/Pry.Core/Narrative/NarrativeContextCompiler.cs:62`（载体内） |
| ②改前原文 | `        if (total <= 0)` |
| ③改后原文 | `        if (total <= -1000000)`（整文件字节复制 `W39-M2_NarrativeContextCompiler.cs`） |
| ④`git diff --stat` | `1 file changed, 1 insertion(+), 1 deletion(-)`（同一枚文件） |
| ⑤跑法整行 | 同 §一.7 逐字，只换结果目录名＝`…/out/trx-M2`（载体内，无 `--no-build`） |
| ⑥TRX 文件名 | `24782_PHYTHLINK_2026-10-29_02_02_29.trx`（枚数＝1） |
| ⑦红格全名＋消息要点＋红点行号 | 三枚，消息族全部＝`Assert.Equal() Failure: Values differ / Expected: WindowTooSmall / Actual: MandatoryExceedsBudget`：<br>1) `Pry.Core.Tests.NarratorPrototypeTests.Compiler_refuses_a_window_that_leaves_no_positive_input_budget`（族 `public void`，判据起始行 237，帧 `:243`）<br>2) `…Round_does_not_call_the_provider_when_the_context_is_rejected`（族 `public async Task`，起始行 394，帧 `:404`）<br>3) `…Call_refuses_an_assembly_that_still_carries_a_rejection`（族 `public void`，起始行 420，帧 `:430`）<br>三枚均 **ASSERT-RED（承重断言失败）**，非「整格因异常而红」；开标签红＝3／`<Counters failed="3">`＝3／反向锚定 `UnitTestResult`=3＋`ResultSummary`=1／total＝**56**／executed＝56／notExecuted＝0 |
| ⑧还原命令 | `node lane39-round.js revert src/Pry.Core/Narrative/NarrativeContextCompiler.cs`（`git show` 取字节再写，未用 `checkout`／`restore`） |
| ⑨还原后 `git status --short` 行数 | **0 行** |
| ⑩SHA | 突变态＝`cf796feac5ac36d3407f3e7dfca887fbb6f2f9a12cefafad6f3c4de6d68228fb`（＝§三.0，24464 字节亦等）；还原后＝`86c3…9842`＝§〇.4；本轮判据 SHA apply 侧＋还原后＝`4f1b2f6e…`（第 3、4 次读数同值） |
| ⑪当轮预检 | **P97**（`out/preflight-P97.txt`）整段：`FOREGROUND: QQ pid=27508 class=Chrome_WidgetWin_1 style=0x14C70000 rect=-1825,234,993x802 screen=1920x1080 covers=False borderless=False title=QQ`／`UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404`／`gpu_util=15% used=2066MB`／`free_memory_gb=13.5`／**`VERDICT=BUILD_OK`** |

裁决＝**恰 3 枚清洁红，三枚红名逐字符等于 §三.0 预期列**⇒ 本轮有效（M2 的三枚＝§三.0R 件 2 复算 SAME-AS-TICKET 的第一枚实证）。整段原始输出＝`out/round-M2.txt`。

## 三.3 轮 M3（W39-M3）——十一项

| 项 | 读数 |
|---|---|
| ①文件:行 | `src/Pry.Core/Narrative/NarrativeContextCompiler.cs:71`（载体内） |
| ②改前原文 | `        if (string.IsNullOrWhiteSpace(payloads.Identity))` |
| ③改后原文 | `        if (payloads.Identity is null)`（整文件字节复制 `W39-M3_NarrativeContextCompiler.cs`） |
| ④`git diff --stat` | ` src/Pry.Core/Narrative/NarrativeContextCompiler.cs \| 2 +-` → `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤跑法整行 | 同 §一.7 逐字，只换结果目录名＝`…/out/trx-M3`（载体内，无 `--no-build`） |
| ⑥TRX 文件名 | `24782_PHYTHLINK_2026-10-29_02_03_25.trx`（枚数＝1） |
| ⑦红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.NarratorPrototypeTests.Compiler_refuses_a_scene_without_identity_instead_of_inventing_one`（剥前缀＝`Compiler_refuses_a_scene_without_identity_instead_of_inventing_one`，在判据内＝true，族＝`public void`，判据起始行 247）；消息＝`Assert.False() Failure / Expected: False / Actual: True`；红点帧＝`NarratorPrototypeTests.cs:251`；红形状＝**ASSERT-RED（该格承重断言失败）**；开标签红＝1／`<Counters failed="1">`＝1／反向锚定 `UnitTestResult`=1＋`ResultSummary`=1／total＝**56**／executed＝56／notExecuted 开标签＝0 |
| ⑧还原命令 | `node lane39-round.js revert src/Pry.Core/Narrative/NarrativeContextCompiler.cs`（`git show 51f149d:` 取字节再写，未用 `checkout`／`restore`） |
| ⑨还原后 `git status --short` 行数 | **0 行** |
| ⑩SHA | 突变态＝`7187b9bbd9fd03cfe8cd2db6b5c3e03d4b68d213a01f44be4005e94a36467cd3`（＝§三.0，24438 字节亦等）；还原后＝`86c3…9842`＝§〇.4；本轮判据 SHA apply 侧＋还原后＝`4f1b2f6e…`（第 5、6 次读数同值） |
| ⑪当轮预检 | **P98**（`out/preflight-P98.txt`）整段：`=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===`／`FOREGROUND: Godot pid=46964 class=Engine style=0x16CF0000 rect=1198,294,1116x799 screen=2560x1440 covers=False borderless=False title=???? ? ??????? (DEBUG)`／`UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404`／`gpu_util=12% used=2138MB`／`free_memory_gb=13`／**`VERDICT=BUILD_OK`** |

裁决＝**恰 1 枚清洁红，红名逐字符等于 §三.0 预期列**⇒ 本轮有效。整段原始输出＝`out/round-M3-M4.txt`。

## 三.4 轮 M4（W39-M4）——十一项

| 项 | 读数 |
|---|---|
| ①文件:行 | `src/Pry.Core/Narrative/NarrativeContextCompiler.cs:78`（载体内） |
| ②改前原文 | `            \|\| !string.Equals(payloads.CurrentEvent.ConversationId, request.ConversationId, StringComparison.Ordinal))` |
| ③改后原文 | `            \|\| false)`（整文件字节复制 `W39-M4_NarrativeContextCompiler.cs`） |
| ④`git diff --stat` | `1 file changed, 1 insertion(+), 1 deletion(-)`（同一枚文件） |
| ⑤跑法整行 | 同 §一.7 逐字，只换结果目录名＝`…/out/trx-M4`（载体内，无 `--no-build`） |
| ⑥TRX 文件名 | `24782_PHYTHLINK_2026-10-29_02_03_30.trx`（枚数＝1） |
| ⑦红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.NarratorPrototypeTests.Compiler_refuses_a_current_event_from_another_conversation`（剥前缀同名，在判据内＝true，族＝`public void`，判据起始行 256）；消息＝`Assert.False() Failure / Expected: False / Actual: True`；红点帧＝`NarratorPrototypeTests.cs:261`；红形状＝**ASSERT-RED**；开标签红＝1／`<Counters failed="1">`＝1／反向锚定 `UnitTestResult`=1＋`ResultSummary`=1／total＝**56**／executed＝56／notExecuted＝0 |
| ⑧还原命令 | `node lane39-round.js revert src/Pry.Core/Narrative/NarrativeContextCompiler.cs`（`git show` 取字节再写，未用 `checkout`／`restore`） |
| ⑨还原后 `git status --short` 行数 | **0 行** |
| ⑩SHA | 突变态＝`d7fff9107c5166be4c03a1fb99895a33924e4aa13e716615d045bc70a323e5c1`（＝§三.0，24360 字节亦等）；还原后＝`86c3…9842`＝§〇.4；本轮判据 SHA apply 侧＋还原后＝`4f1b2f6e…`（第 7、8 次读数同值） |
| ⑪当轮预检 | **P99**（`out/preflight-P99.txt`）整段：`=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===`／`FOREGROUND: Godot pid=46964 class=Engine style=0x16CF0000 rect=1198,294,1116x799 screen=2560x1440 covers=False borderless=False title=???? ? ??????? (DEBUG)`／`UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404`／`gpu_util=9% used=2011MB`／`free_memory_gb=12.8`／**`VERDICT=BUILD_OK`** |

裁决＝**恰 1 枚清洁红，红名逐字符等于 §三.0 预期列**⇒ 本轮有效。整段原始输出＝`out/round-M3-M4.txt`。

## 三.5 轮 M5（W39-M5）——十一项

| 项 | 读数 |
|---|---|
| ①文件:行 | `src/Pry.Core/Narrative/NarrativeContextCompiler.cs:87`（载体内） |
| ②改前原文 | `        if (preludeCost > total)` |
| ③改后原文 | `        if (preludeCost > total * 100)`（整文件字节复制 `W39-M5_NarrativeContextCompiler.cs`） |
| ④`git diff --stat` | `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤跑法整行 | 同 §一.7 逐字，只换结果目录名＝`…/out/trx-M5`（载体内，无 `--no-build`） |
| ⑥TRX 文件名 | `24782_PHYTHLINK_2026-10-29_02_03_48.trx`（枚数＝1） |
| ⑦红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.NarratorPrototypeTests.Compiler_refuses_the_round_when_the_mandatory_prelude_does_not_fit`（剥前缀同名，在判据内＝true，族＝`public void`，判据起始行 225）；消息＝`Assert.Equal() Failure: Values differ / Expected: MandatoryExceedsBudget / Actual: BudgetInequalityViolated`（＝拒绝原因从本门翻到上游 `:81` 那支的既有原因，属可解释翻转，不是异常）；红点帧＝`NarratorPrototypeTests.cs:232`；红形状＝**ASSERT-RED**；开标签红＝1／`<Counters failed="1">`＝1／反向锚定 `UnitTestResult`=1＋`ResultSummary`=1／total＝**56**／executed＝56／notExecuted＝0 |
| ⑧还原命令 | `node lane39-round.js revert src/Pry.Core/Narrative/NarrativeContextCompiler.cs` |
| ⑨还原后 `git status --short` 行数 | **0 行** |
| ⑩SHA | 突变态＝`6f8b4608172a95ac826c24b48dbbb6faac1102edeb26c70d4648424662d16b20`（＝§三.0，24463 字节亦等）；还原后＝`86c3…9842`＝§〇.4；本轮判据 SHA apply 侧＋还原后＝`4f1b2f6e…`（第 9、10 次读数同值） |
| ⑪当轮预检 | **P100**（`out/preflight-P100.txt`）整段：`=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===`／`FOREGROUND: Godot pid=46964 class=Engine style=0x16CF0000 rect=1198,294,1116x799 screen=2560x1440 covers=False borderless=False title=???? ? ??????? (DEBUG)`／`UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404`／`gpu_util=18% used=2007MB`／`free_memory_gb=12.8`／**`VERDICT=BUILD_OK`** |

裁决＝**恰 1 枚清洁红，红名逐字符等于 §三.0 预期列**⇒ 本轮有效。整段原始输出＝`out/round-M5-M6.txt`。

## 三.6 轮 M6（W39-M6）——十一项

| 项 | 读数 |
|---|---|
| ①文件:行 | `src/Pry.Core/Narrative/NarrativeContextCompiler.cs:110`（载体内） |
| ②改前原文 | `            if (cost > budget.AdvisoryTokens \|\| used + cost > total)` |
| ③改后原文 | `            if (used + cost > total)`（整文件字节复制 `W39-M6_NarrativeContextCompiler.cs`；去掉的是左半合取项，非手写） |
| ④`git diff --stat` | `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤跑法整行 | 同 §一.7 逐字，只换结果目录名＝`…/out/trx-M6`（载体内，无 `--no-build`） |
| ⑥TRX 文件名 | `24782_PHYTHLINK_2026-10-29_02_03_53.trx`（枚数＝1） |
| ⑦红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.NarratorPrototypeTests.Compiler_fails_loudly_when_the_reserved_directive_does_not_fit`（剥前缀同名，在判据内＝true，族＝`public void`，判据起始行 266）；消息＝`Assert.False() Failure / Expected: False / Actual: True`；红点帧＝`NarratorPrototypeTests.cs:271`；红形状＝**ASSERT-RED**；开标签红＝1／`<Counters failed="1">`＝1／反向锚定 `UnitTestResult`=1＋`ResultSummary`=1／total＝**56**／executed＝56／notExecuted＝0 |
| ⑧还原命令 | `node lane39-round.js revert src/Pry.Core/Narrative/NarrativeContextCompiler.cs` |
| ⑨还原后 `git status --short` 行数 | **0 行** |
| ⑩SHA | 突变态＝`c06190512e2780538fb480f95f56bf2b0c877084b7db89c09db9cda9ce69f866`（＝§三.0，24425 字节亦等）；还原后＝`86c3…9842`＝§〇.4；本轮判据 SHA apply 侧＋还原后＝`4f1b2f6e…`（第 11、12 次读数同值） |
| ⑪当轮预检 | **P101**（`out/preflight-P101.txt`）整段：`=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===`／`FOREGROUND: Godot pid=46964 class=Engine style=0x16CF0000 rect=1198,294,1116x799 screen=2560x1440 covers=False borderless=False title=???? ? ??????? (DEBUG)`／`UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404`／`gpu_util=8% used=2007MB`／`free_memory_gb=12.8`／**`VERDICT=BUILD_OK`** |

裁决＝**恰 1 枚清洁红，红名逐字符等于 §三.0 预期列**⇒ 本轮有效。整段原始输出＝`out/round-M5-M6.txt`。

## 三.7 轮 M7（W39-M7）——十一项

| 项 | 读数 |
|---|---|
| ①文件:行 | `src/Pry.Core/Narrative/NarratorContracts.cs:107`（载体内） |
| ②改前原文 | `        if (next != 1)` |
| ③改后原文 | `        if (next > 2)`（整文件字节复制 `W39-M7_NarratorContracts.cs`） |
| ④`git diff --stat` | ` src/Pry.Core/Narrative/NarratorContracts.cs \| 2 +-` → `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤跑法整行 | 同 §一.7 逐字，只换结果目录名＝`…/out/trx-M7`（载体内，无 `--no-build`） |
| ⑥TRX 文件名 | `24782_PHYTHLINK_2026-10-29_02_04_10.trx`（枚数＝1） |
| ⑦红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.NarratorPrototypeTests.Second_call_on_the_same_round_budget_is_refused`（剥前缀同名，在判据内＝true，族＝`public void`，判据起始行 408）；消息＝`Assert.Throws() Failure: No exception was thrown / Expected: typeof(Pry.Core.Narrative.NarrativeContextAssemblyRejectedException)`（`Assert.Throws` 族＝断言级失败，非「整格因异常而红」）；红点帧＝`NarratorPrototypeTests.cs:413`；红形状＝**ASSERT-RED**；开标签红＝1／`<Counters failed="1">`＝1／反向锚定 `UnitTestResult`=1＋`ResultSummary`=1／total＝**56**／executed＝56／notExecuted＝0 |
| ⑧还原命令 | `node lane39-round.js revert src/Pry.Core/Narrative/NarratorContracts.cs`（`git show 51f149d:src/Pry.Core/Narrative/NarratorContracts.cs` 取字节再写，未用 `checkout`／`restore`） |
| ⑨还原后 `git status --short` 行数 | **0 行** |
| ⑩SHA | 突变态＝`b69488df60da03777afc99e46e79cb1b41370f7021f23cf400c2d4860d0e4d7b`（＝§三.0，5535 字节亦等）；还原后＝`3a33a8151e61c3396963dac451dffcb46acb2f02807f140d18f694d6a52a9f8c`＝§〇.4；本轮判据 SHA apply 侧＋还原后＝`4f1b2f6e…`（第 13、14 次读数同值） |
| ⑪当轮预检 | **P102**（`out/preflight-P102.txt`）整段：`=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===`／`FOREGROUND: Godot pid=36540 class=Engine style=0x16CF0000 rect=802,385,956x599 screen=2560x1440 covers=False borderless=False title=`／`UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404`／`gpu_util=8% used=2029MB`／`free_memory_gb=12.6`／**`VERDICT=BUILD_OK`** |

裁决＝**恰 1 枚清洁红，红名逐字符等于 §三.0 预期列**⇒ 本轮有效。整段原始输出＝`out/round-M7-M8-M9.txt`。

## 三.8 轮 M8（W39-M8）——十一项（**实测与预期不符 ⇒ 整轮作废**）

| 项 | 读数 |
|---|---|
| ①文件:行 | `src/Pry.Core/Narrative/NarrativeRoundRunner.cs:115`（载体内） |
| ②改前原文 | `                Disposition = NarrativeRoundDisposition.ProviderRejected, Assembly = assembly, Outcome = outcome,` |
| ③改后原文 | 同行 `ProviderRejected` 改 `Silence`＝`                Disposition = NarrativeRoundDisposition.Silence, Assembly = assembly, Outcome = outcome,`（整文件字节复制 `W39-M8_NarrativeRoundRunner.cs`，非手写） |
| ④`git diff --stat` | ` src/Pry.Core/Narrative/NarrativeRoundRunner.cs \| 2 +-` → `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤跑法整行 | 同 §一.7 逐字，只换结果目录名＝`…/out/trx-M8`（载体内，无 `--no-build`） |
| ⑥TRX 文件名 | `24782_PHYTHLINK_2026-10-29_02_04_15.trx`（枚数＝1） |
| ⑦红格全名＋消息要点＋红点行号 | **实测红＝5 枚（预期列＝恰 4）⇒ 多出一枚**，五枚消息族全部＝`Assert.Equal() Failure: Values differ / Expected: ProviderRejected / Actual: Silence`，全部 **ASSERT-RED**：<br>1) `Pry.Core.Tests.NarratorPrototypeTests.Malformed_response_is_not_masquerading_as_a_chosen_silence`（族 `public async Task`，起始行 534，帧 `:540`）＝预期列内<br>2) `…A_provider_that_ignores_cancellation_still_exits_as_timeout`（族 `public async Task`，起始行 549，帧 `:555`）＝预期列内<br>3) `…A_provider_that_honours_the_timeout_token_exits_as_timeout_too`（族 `public async Task`，起始行 562，帧 `:569`）＝预期列内<br>4) `…Provider_failure_becomes_a_named_error_shape_and_writes_nothing`（族 `public async Task`，起始行 574，帧 `:582`）＝预期列内<br>5) **多出一枚**＝`Pry.Core.Tests.NarratorPrototypeTests.Provider_side_cancellation_with_a_live_host_token_is_named_timeout_not_failure`（在判据内＝true，族 `public async Task`，判据起始行 **851**，帧 `:865`）＝**不在 §三.0 预期列**<br>开标签红＝5／`<Counters failed="5">`＝5／反向锚定 `UnitTestResult`=5＋`ResultSummary`=1（后者不算用例）／total＝**56**／executed＝56／notExecuted＝0 |
| ⑧还原命令 | `node lane39-round.js revert src/Pry.Core/Narrative/NarrativeRoundRunner.cs`（`git show` 取字节再写，未用 `checkout`／`restore`） |
| ⑨还原后 `git status --short` 行数 | **0 行** |
| ⑩SHA | 突变态＝`0400a57714bbdd8e8d642e78daaa5e9ceaf79c927029c63a06d5d0daaf186c08`（＝§三.0，16759 字节亦等）；还原后＝`961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9`＝§〇.4；本轮判据 SHA apply 侧＋还原后＝`4f1b2f6e…`（第 15、16 次读数同值）＝**判据字节本轮仍未动，作废与判据无关** |
| ⑪当轮预检 | **P103**（`out/preflight-P103.txt`）整段：`=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===`／`FOREGROUND: Godot pid=36540 class=Engine style=0x16CF0000 rect=477,367,956x599 screen=2560x1440 covers=False borderless=False title=???? ? ???? (DEBUG)`／`UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404`／`gpu_util=14% used=2046MB`／`free_memory_gb=10.5`／**`VERDICT=BUILD_OK`** |

**裁决＝本轮作废**（§三 首部＋§七.4：实测与预期不符，含多出一枚红 ⇒ 整轮作废、如实登记、不追第二轮）。§三.0 预期列**原样未改**（仍＝恰 4＋那四枚名）。M8 那四枚格＝`Malformed_response_is_not_masquerading_as_a_chosen_silence`@534、`A_provider_that_ignores_cancellation_still_exits_as_timeout`@549、`A_provider_that_honours_the_timeout_token_exits_as_timeout_too`@562、`Provider_failure_becomes_a_named_error_shape_and_writes_nothing`@574——**红不署**（它们在波 13 那本账里的格号由守门人 §九 点对，本刀不代填编号）；多出的那一枚也不署（它不属于本刀 14 枚）。作废轮产物**未删、未清空、未重跑**，TRX 与归档原样留盘（改名处置＝`trx-<轮>-voided-<预检号>` 形态，见 §六.2）。

### 三.8X M8 连坐面登记（原样上报，不自行改判据或改预期）

盘上事实与 §三.0R 件 2／3 的独立复算**同向**：本刀复算时已把 M8 的粗面判为 **5 枚消费者**（不是工单预期的 4 枚），并把第五枚名逐字符预判为 `Provider_side_cancellation_with_a_live_host_token_is_named_timeout_not_failure`@851；盘上跑出的那枚多出的红正是这一名（帧 `:865`＝该格 `Assert.Equal(NarrativeRoundDisposition.ProviderRejected, result.Disposition)`，该格 `:864` 的 `Kind=TimedOut` 断言不受本轮改动影响）。机理：该格 provider 在**自己的内部令牌**上取消、宿主令牌与超时界都活着 ⇒ `InvokeOnceAsync` 产出 `Kind != DraftProduced` ⇒ 必进 `NarrativeRoundRunner.cs:110` 的 `if` ⇒ 从 `:113-117` 早返回，而 `:115` 的 `Disposition` 常量正是本轮唯一改动点，即时支（`:150`）与延迟支（`:155`）都在其后，不可达。

登记结论（三条，全部交给守门人）：
1. **本刀复算与盘上同值**＝「波 18 追加段与波 13 老门的连坐面」确实存在，且**恰好落在 M8 这一轮** ⇒ 按 §三 首部口径整轮作废，本刀不把它当成「4 枚清洁红」签。
2. **工单 §三.0R 件 2／3 的枚举面在盘上不全**＝波 18 追加段（判据 `:823` 注释起）带属性的格是 **7 枚**而非「五枚」：`Mixed_round_takes_the_immediate_branch_and_registers_no_intent_there`@832 与 `Provider_side_cancellation_with_a_live_host_token_is_named_timeout_not_failure`@851 不在工单点名的五枚里；49＋7＝56 与九轮每轮 `total=56` 一致。本刀按七枚逐枚复算才得到那条预判（若按五枚复算则会漏判 M8 连坐、把 5 枚红当批次红硬署）。
3. **下一把（B 类剩 5 枚／D 类 28 枚复采）要用的信息，不是本刀的账**：`:115` 这类「早返回路径上的共享常量」门，在 56 格树上的红集**不能用 49 格老红集平移**；本刀把它原样上报，未改判据、未改预期、未追第二轮、未另起构建。

## 三.9 轮 M9（W39-M9）——十一项

| 项 | 读数 |
|---|---|
| ①文件:行 | `src/Pry.Core/Narrative/NarrativeRoundRunner.cs:155`（载体内） |
| ②改前原文 | `        if (deferred.Length > 0)` |
| ③改后原文 | `        if (deferred.Length > 1)`（整文件字节复制 `W39-M9_NarrativeRoundRunner.cs`） |
| ④`git diff --stat` | `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤跑法整行 | 同 §一.7 逐字，只换结果目录名＝`…/out/trx-M9`（载体内，无 `--no-build`） |
| ⑥TRX 文件名 | `24782_PHYTHLINK_2026-10-29_02_04_21.trx`（枚数＝1） |
| ⑦红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.NarratorPrototypeTests.Deferred_round_registers_the_intent_without_touching_real_history`（剥前缀同名，在判据内＝true，族＝`public async Task`，判据起始行 503）；消息＝`Assert.Equal() Failure: Values differ / Expected: Deferred / Actual: Silence`；红点帧＝`NarratorPrototypeTests.cs:509`；红形状＝**ASSERT-RED**；开标签红＝1／`<Counters failed="1">`＝1／反向锚定 `UnitTestResult`=1＋`ResultSummary`=1／total＝**56**／executed＝56／notExecuted＝0 |
| ⑧还原命令 | `node lane39-round.js revert src/Pry.Core/Narrative/NarrativeRoundRunner.cs` |
| ⑨还原后 `git status --short` 行数 | **0 行** |
| ⑩SHA | 突变态＝`2e5533294126165ea841c7212033bdd9a52b5b1ffedace533e7c954145f0c194`（＝§三.0，16768 字节亦等）；还原后＝`961fb49b…37e9`＝§〇.4；本轮判据 SHA apply 侧＋还原后＝`4f1b2f6e…`（第 17、18 次读数同值） |
| ⑪当轮预检 | **P104**（`out/preflight-P104.txt`）整段：`=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===`／`FOREGROUND: Godot pid=36540 class=Engine style=0x16CF0000 rect=415,367,956x599 screen=2560x1440 covers=False borderless=False title=???? ? ???? (DEBUG)`／`UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404`／`gpu_util=12% used=2116MB`／`free_memory_gb=10.4`／**`VERDICT=BUILD_OK`** |

裁决＝**恰 1 枚清洁红，红名逐字符等于 §三.0 预期列**⇒ 本轮有效。整段原始输出＝`out/round-M7-M8-M9.txt`。

### 三.10 九轮小结（实测 vs 预期，逐轮）

| 轮 | 预期枚数／格名（§三.0 原样） | 实测枚数 | 红名逐字符对账 | 轮裁决 |
|---|---|---|---|---|
| M1 | 恰 1／`Compiler_rejects_a_negative_section_share` | 1 | SAME | 有效 |
| M2 | 恰 3／三枚名 | 3 | SAME | 有效 |
| M3 | 恰 1／`Compiler_refuses_a_scene_without_identity_instead_of_inventing_one` | 1 | SAME | 有效 |
| M4 | 恰 1／`Compiler_refuses_a_current_event_from_another_conversation` | 1 | SAME | 有效 |
| M5 | 恰 1／`Compiler_refuses_the_round_when_the_mandatory_prelude_does_not_fit` | 1 | SAME | 有效 |
| M6 | 恰 1／`Compiler_fails_loudly_when_the_reserved_directive_does_not_fit` | 1 | SAME | 有效 |
| M7 | 恰 1／`Second_call_on_the_same_round_budget_is_refused` | 1 | SAME | 有效 |
| M8 | 恰 4／那四枚名 | **5** | 四枚 SAME＋**多出一枚**（名见 §三.8） | **作废**（红不署、不追第二轮） |
| M9 | 恰 1／`Deferred_round_registers_the_intent_without_touching_real_history` | 1 | SAME | 有效 |

八枚有效轮的清洁红合起来＝**10 枚**红格（1＋3＋1＋1＋1＋1＋1＋1），覆盖工单 §〇.3 那 14 枚 B 类候选中的 **10 枚**；M8 作废使 **4 枚**（§三.8 点名的那四枚）本轮**无换手凭据**。此处置换本刀交付面＝**10 枚签资格成立、4 枚押下一把**（原样上报，由守门人 §九 裁决；本刀不写状态词、不划账、不替下一把轻判）。

## 三.P 焦点预检台账（逐枚当场登记，含 HOLD）

| 编号 | 时刻（归档 mtime） | VERDICT | 归档路径 | 前台窗 pid／class／covers／borderless | free_memory_gb | 放行哪一轮 |
|---|---|---|---|---|---|---|
| P96 | 2026-10-29 02:00:52 | BUILD_OK | `out/preflight-P96.txt` | ChatGPT pid=16348／`Chrome_WidgetWin_1`／covers=False／borderless=False | 13.7 | M1 |
| P97 | 2026-10-29 02:02（本轮 dotnet 前当场跑） | BUILD_OK | `out/preflight-P97.txt` | QQ pid=27508／`Chrome_WidgetWin_1`／covers=False／borderless=False | 13.5 | M2 |
| P98 | 2026-10-29 02:03 | BUILD_OK | `out/preflight-P98.txt` | Godot pid=46964／`Engine`／covers=False／borderless=False | 13 | M3 |
| P99 | 2026-10-29 02:03 | BUILD_OK | `out/preflight-P99.txt` | Godot pid=46964／`Engine`／covers=False／borderless=False | 12.8 | M4 |
| P100 | 2026-10-29 02:03 | BUILD_OK | `out/preflight-P100.txt` | Godot pid=46964／`Engine`／covers=False／borderless=False | 12.8 | M5 |
| P101 | 2026-10-29 02:03 | BUILD_OK | `out/preflight-P101.txt` | Godot pid=46964／`Engine`／covers=False／borderless=False | 12.8 | M6 |
| P102 | 2026-10-29 02:04 | BUILD_OK | `out/preflight-P102.txt` | Godot pid=36540／`Engine`／covers=False／borderless=False | 12.6 | M7 |
| P103 | 2026-10-29 02:04 | BUILD_OK | `out/preflight-P103.txt` | Godot pid=36540／`Engine`／covers=False／borderless=False | 10.5 | M8（该轮后来作废，预检本身仍 BUILD_OK；`free_memory_gb=10.5` 非低值面，无 SQLite／内存形状 ⇒ 不作废读数的第二理由） |
| P104 | 2026-10-29 02:04 | BUILD_OK | `out/preflight-P104.txt` | Godot pid=36540／`Engine`／covers=False／borderless=False | 10.4 | M9 |
| P105 | 2026-10-29 02:10（收尾全量之前当场跑） | BUILD_OK | `out/preflight-P105.txt` | Godot pid=36540／`Engine`／covers=False／borderless=False | 10.4 | F（收尾全量，本刀第 10 次也是最后一次 `dotnet`） |

（**HOLD 重跑额度动用＝0/16**：一次 HOLD 都没有，间隔这条从未被触发。逐份 `VERDICT` grep 对账＝`out/final-post-F-check.txt`：本刀放行的 10 份（P96–P105）全 `BUILD_OK`、`HOLD` 0 份；P95＝守门人入场存档、不占本刀放行面。`dotnet` 调用数＝10＝定向 9＋全量 1＝预检放行枚数，逐一对上。）

## 三.12 固定跑序的收尾两步（全部还原并核六枚 SHA → F）

跑序「M1→…→M9→**全部还原并核六枚 SHA**→F」的中间那步已当场做（零 `dotnet`，`out/final-six-sha.txt`）：六枚文件三侧（主树工作副本／载体工作副本／`git show 51f149d:<path>` blob）逐字符同值，且**全部等于工单 §〇.4 那六枚基准**；判据 `NarratorPrototypeTests.cs` 三侧仍＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`（第 19–21 次读数，九轮 18 次之外的出场面）。载体相对 `51f149d` 净写入＝**0 字节**。F 跑完后再复核一次（`out/final-post-F-check.txt`）＝六枚仍三侧同值、判据仍 `4f1b2f6e…`（第 22–24 次读数）、载体 `status --short` 仍 **0 行**、`fd93ebe..HEAD` 仍只有波 33 那两枚提交（`51f149d`＋`1e02205`）、`diff --stat fd93ebe HEAD -- src tests scripts Pry.slnx Directory.Build.props` 仍＝**6 files changed, 822 insertions(+), 0 deletions**、主树侧 0 行式子（右端当场取＝`0a9ce00c674b48c1631890fba9abe3806c013bb5`）仍＝**0 行** ⇒ 本刀在载体里取到的红＝主线那棵树的红，这条入场自证到出场没有失效。

**判据三计数与签名两族出场复看**（净新增 0 的证明）：`[Fact]`＝**53**／`[Theory]`＝**1**／`[InlineData]`＝**3**（53＋1×3＝56 枚格形状不变）、文件仍 989 行／46491 字节；签名抽取＝56 枚方法（`public void` **24**＋`public async Task` **30**＋`public Task` 0），其中带 `[Fact]`／`[Theory]` 属性格 **54**、不带属性的 2 枚＝`ProposeAsync@130`／`ProposeAsync@874`（假 provider，非格）⇒ **本刀九轮对判据净新增格 0、改断言 0、改 `[InlineData]` 期望值 0、`Assert.Skip` 0、注释既有断言 0、新增数字门槛 0**（归档＝`out/final-signature-recheck.txt`）。

## 四 收尾全量 F（第 10 次 `dotnet`，跑序最后一步；先数满四份再求和）

命令（载体内，无 `--no-build`，逐字沿用前几把的解决方案级全量形态）＝
`dotnet test Pry.slnx -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave39-1029/out/trx-F" --logger trx`

**先数枚数＝4 份**（满额形状＝全量 1×4）：`…_02_10_42.trx`／`…_02_10_43.trx`／`…_02_10_43[1].trx`／`…_02_10_44.trx`（第四枚与第三枚同名秒级碰撞，写手自动加 `[1]` 后缀，两份都是完整产物，未删未改名）。逐份 `<Counters>`（磁盘产物当场求和，非心算）：

| TRX | 归属项目 | `total` | `executed` | `passed` | `failed` | `notExecuted` 属性 | `UnitTestResult` 开标签 | 对账 |
|---|---|---|---|---|---|---|---|---|
| `02_10_42.trx` | `Pry.Client.Tests` | 108 | 108 | 108 | 0 | "0" | 108 | CONSISTENT |
| `02_10_43.trx` | `Pry.Api.Tests` | 280 | 280 | 280 | 0 | "0" | 280 | CONSISTENT |
| `02_10_43[1].trx` | `Pry.Core.Tests` | 785 | 783 | 783 | 0 | "0"（属性形态） | 785（含 `NotExecuted` 开标签 2） | CONSISTENT＋RAWATTR |
| `02_10_44.trx` | `Pry.App.Tests` | 233 | 233 | 233 | 0 | "0" | 233 | CONSISTENT |
| **求和** | 4 份 | **1406** | **1404** | **1404** | **0** | 属性求和 0（写手恒发 "0"） | 开标签求和 **1406** | 开标签＝`total` 求和 ✓ |

- 求和式（盘上逐份读数相加）：`108 + 280 + 233 + 785 = 1406`；`108 + 280 + 233 + 783 = 1404`（executed）；`0 + 0 + 0 + 0 = 0`（failed）。
- 非执行两枚按 `outcome="NotExecuted"` 开标签逐字符点名＝`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`、`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`（＝波 34／36／37／38 那两枚同名跳过，`total−executed＝785−783＝2`＝开标签枚数 2；`<Counters notExecuted="0">` 是写手属性形态，本刀仪器按波 38 §九.1 条 6 的口径原样登记为 RAWATTR，不拿属性值当读数）。
- **出口读数＝1406／executed 1404／failed 0／跳过 2＝与工单 §〇.5 起点同值**（第五次同值复采：波 34／36／37／38＋本刀）。**本刀不顶账**：M8 作废没有触发任何重跑、没有为对账另起构建（`全量 ≤1` 那一格只用了这一次）、没有动基线、没有改判据。读数与预期不同才需要的"怀疑树没还原干净"这一步没出现——而且出场六枚 SHA 已独立复核为 §〇.4。
- 控制台尾读与 TRX 同值（`已通过! - 失败: 0，通过: 783，已跳过: 2，总计: 785 … Pry.Core.Tests.dll` 等四行），整段原始输出＝`out/final-F-count.txt`（仪器裁决）＋本会话命令回显。

## 五 交回给守门人核的七件事（本 lane 只报数，不裁决）

1. **九轮实测枚数与红名逐字符对账**：M1＝1（SAME）、M2＝3（SAME）、M3＝1（SAME）、M4＝1（SAME）、M5＝1（SAME）、M6＝1（SAME）、M7＝1（SAME）、**M8＝实测 5 vs 预期 4 ⇒ 整轮作废**（多出的一枚逐字符名＝`Pry.Core.Tests.NarratorPrototypeTests.Provider_side_cancellation_with_a_live_host_token_is_named_timeout_not_failure`，判据起始行 851，红点帧 `NarratorPrototypeTests.cs:865`，消息族与其他四枚同＝`Expected: ProviderRejected / Actual: Silence`）、M9＝1（SAME）。红点帧全部落在对应格的方法体邻域（起始行＋3～＋8），无早于起始行的面。**批次红未署给单枚突变**：M8 那四枚本轮不署，本刀也没有拿"四枚SAME＋一枚多"当四枚署的口径。
2. **判据 SHA 读数**：九轮 apply 侧＋还原后＝**18 次**，逐次等值 `4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`；另有出场三面（六枚三侧复核的前后两次）＝同值。全 18 次无一次缺、无一次异值 ⇒ 换手凭据面成立（签与不签由 §九.2 逐轮点）。
3. **载体终态三面**：`git -C carrier status --short`＝**0 行**；`log --oneline fd93ebe..HEAD`＝**2 行**（`51f149d`、`1e02205`，未新增提交）；六枚 SHA 逐字符＝§〇.4（F 后再核一次仍同值）。归档＝`out/final-six-sha.txt`＋`out/final-post-F-check.txt`。
4. **F 四份求和**＝1406／1404／0／跳过 2，两枚跳过逐字符点名见 §四；本刀不顶账、未为顶账另起构建。
5. **`.trx` 对账**：盘上满额形状＝**13 枚**＝定向 9 轮各 1（含作废的 M8 那枚，改名留档为 `out/trx-M8-voided-P103/`）＋全量 1 次 4 枚 ⇒ `9×1 + 1×4 = 13`，与轮数自校**无差额**；每轮 `total`＝**56**（不是波 13 的 49），九轮九份逐份 CONSISTENT；三计数与签名两族出场复看＝53／1／3 与 24＋30＝54 枚属性格（净新增 0）。
6. **守卫实证**：§一.4 整段原文（(a) 合法 ID 产品文本→判据文件两路 REFUSED、(b) 手写字节→`NarrativeContextCompiler.cs` REFUSED、正面控件 ALLOWED 并当场还原，目标 SHA 前后同值逐条打出）。**有没有人偷偷改断言／改 `[InlineData]`／`Assert.Skip`／注释断言／加数字门槛／把 §三.0 预期列改成实测值**＝全部**没有**：判据字节 0 变动（18 次同值 SHA＋出场三计数复看），§三.0 预期列在报告里逐字仍是工单那四枚名＋恰 4。另有本刀自曝的一次仪器缺陷（§〇.7：`probe-control` 缺 `src/` 前缀致载体一枚文件短暂停在突变态，秒级还原、未经过任何 `dotnet`）。
7. **每枚红的凭证等级**：本轮跑出的红全部是 **ASSERT-RED（该格承重断言失败）**，没有一枚是 EXCEPTION-RED（消息族全部 `Assert.False`／`Assert.Equal`／`Assert.Throws`，无 `NarrativeContextAssemblyRejectedException thrown` 面）。逐枚＝M1 `…negative_section_share`（`Assert.False`，:352）；M2 三枚（`Assert.Equal` 原因翻转，:243／:404／:430）；M3（`Assert.False`，:251）；M4（`Assert.False`，:261）；M5（`Assert.Equal` 原因翻转，:232）；M6（`Assert.False`，:271）；M7（`Assert.Throws: No exception was thrown`，:413）；M9（`Assert.Equal` `Deferred`→`Silence`，:509）。M8 的 5 枚也是 ASSERT-RED（`Assert.Equal` `ProviderRejected`→`Silence`），但**整轮作废 ⇒ 不署任何断言级凭证**。

**与预期不符的面（原样上报，本刀未自行改判据或改预期）**：① M8 实测 5 vs 预期 4（机理与格名见 §三.8X）；② 工单 §三.0R 件 2／3 说"波 18 新增五枚"，盘上判据 `:823` 之后带属性的追加格是 **7 枚**（49＋7＝56，与每轮 `total=56` 自洽），漏点名的两枚中恰有 M8 的连坐那枚；③ 仪器自证面里波 13 `run-full` 的"failed 全 0"与盘上产物不符（该份确有 2 枚红，帧 `:50`／`:110`，`<Counters failed="2">`），已按"工单预期 vs 盘上产物"差额登记，不影响本刀裁决面。

## 六 盘上面相与未验证清单

### 六.1 本刀归档清单（`.local-notes/wave39-1029/out/`，全留不删）

入场与零-`dotnet` 面：`entry-readings.txt`、`entry-sha-6files.txt`、`instrument-selfproof.txt`（16 面四元限定）、`guard-proof.txt`、`raw-attempt-line.txt`、`recompute-item1-parallel.txt`、`recompute-item2-consumers.txt`、`recompute-item3-crossface.txt`、`recompute-item4-static.txt`、`gk39-prep.txt`（守门人产物，只读）。运行面：`preflight-P96.txt`…`preflight-P105.txt`（10 份，本刀放行）、`round-M1-run.txt`／`round-M1-count.txt`／`round-M1-revert.txt`、`round-M2.txt`、`round-M3-M4.txt`、`round-M5-M6.txt`、`round-M7-M8-M9.txt`、`trx-M1`…`trx-M9`（其中 `trx-M8` 改名 `trx-M8-voided-P103`，产物 1 枚未动）。出场面：`final-six-sha.txt`、`final-terminal-check.txt`、`final-signature-recheck.txt`、`final-F-count.txt`、`final-post-F-check.txt`。

### 六.2 `.trx` 枚数自校

`9 轮定向 × 1 枚 ＋ 1 次全量 × 4 枚 ＝ 13 枚`；盘上 `find out -name '*.trx' | wc -l`＝**13**，分目录＝trx-M1…M7、trx-M8-voided-P103、trx-M9 各 1（共 9）＋trx-F 4 ⇒ **差额 0，无需点名去处**。作废轮（M8）产物**未删、未清空、未重跑**，按工单 §一.3 改名留档 `trx-<轮>-voided-<预检号>`。

### 六.3 预算对账

解决方案级构建＝**0 次**（三段判定成立后跳过并登记：① 主树侧 0 行式子＝0 行；② 载体 `src/Pry.Core/bin/Release/net10.0/Pry.Core.dll` 在盘（1,745,920 字节，mtime 2026-10-29 00:46，早于本刀第一次写入）；③ 六枚入场 SHA 三侧同值未变）。定向＝**9/9**。全量＝**1/1**。`dotnet` 总调用＝**10/10**（未越上限）；九轮一律**未带 `--no-build`**（每轮回显里有 `Pry.Core -> …dll` 的重编译行）。HOLD 重跑额度动用＝**0/16**。第三选项（静态判读不起那一轮）＝**未动用**（§三.0R 件 4 九枚全 CLOSED）。

### 六.4 未验证（照工单 §六逐条点名，不用"已完成"混过去）

真实模型对照（零读数，需作者一句"可以跑"）；**生产接线**（HDS-003 这 56 枚至今零生产调用方，005 落库面与 004 生命周期一律没接；接线时机＝U-9 残句仍未拍）；旧数据真实继承（零读取）；UI 可见行为（零读数）；生产库与 `%LOCALAPPDATA%\PryCompanion`（**零接触**，本刀未杀作者任何进程）；待标定参数（关系／心情各档阈值、经验有效期、点击归因窗口全部未定＝特性不是缺陷）；并发与断电（零读数）；旧链删除片与"构建时不把旧链打进包"那道门（未开始）；**D 类 28 枚一枚未碰**、波 20 那 28 枚未碰、B 类剩 5 枚未跑（本刀之后＝波 13 的 B 类 19 枚里 4 枚因 M8 作废仍无换手凭据、5 枚押下一把，本刀一枚没替它们说话）；**工单 §六 那句「旧欠 75 → 61」是按 14 枚全结清预登记的等式，本刀实动面只有 10 枚拿到本轮载体里的清洁红 ⇒ 该等式不在本刀成立，差额 4 枚＝§三.8 那四枚**（原样上报，划账与求和式归守门人 §九，本刀不心算、不替下一把轻判）；`:367` 那半句、探针正数面单独读数、HDS-009 那 26 枚的 A／C／M 类一律未重跑（那两棵判据只作 §一.5 自证面读，未写一字节）；波 28 那枚数值锁格照旧押；`PrototypeFiles` 覆盖盲区、两套并行命名债（属接线刀）不属本刀。**本刀不签"波 13 结案"、不写状态词、不自封在册基线。** 四句话照旧不许出现＝新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链。拆法原文照旧 46／45／1＝W2、catalog 76 未动、在册基线不移动（1406 未由本刀改动）。

## 七 交回与不许走的捷径对照

### 七.1 禁走捷径逐条对照（本刀实动面）

| 工单 §七 条 | 本刀 | 证据面 |
|---|---|---|
| 1 不新增格／不改断言／不改 `[InlineData]`／不 `Assert.Skip`／不注释断言／不改上游门／不新增数字门槛 | 未犯 | 判据 18＋6 次 SHA 同值；出场三计数 53／1／3 与 24＋30；九轮写入只落三枚产品文件 |
| 2 不手写 replacement／不合轮／不带 `--no-build` | 未犯 | `guard()` 对手写字节真 REFUSED（§一.4 (b)）；九轮九次单行单文件（④ 各轮 `1 file changed, 1 insertion(+), 1 deletion(-)`）；九轮回显均有重编译行 |
| 3 静态判读不闭合则不起那一轮 | 未触发 | §三.0R 件 4＝九枚 CLOSED，定向额度未退回（9/9 用满） |
| 4 不把预期列改成实测值；不符即作废登记 | 执行 | §三.0 预期列在报告内逐字仍＝工单原文；M8 实测 5 ⇒ 整轮作废登记（§三.8＋§三.8X） |
| 5 不拿波 13 老红署成本刀红 | 未犯 | 本刀 14 枚候选的凭证全部是本轮载体里跑出的 `trx-M1`…`trx-M9`；波 13／波 38 老 TRX 只出现在 §一.5 自证面 |
| 6 不碰 D 类／不拿 HDS-009 判据做自证以外的写 | 未犯 | §四.1 那 28 枚零操作；`NarrativeCommitFactoryTests.cs`／`NarrativeBudgetInequalityCellTests.cs` 只在 §〇.4／§一.5 被读 |
| 7 不删作废轮产物／不清空重跑／不替守门人写状态词结案词 | 未犯 | `trx-M8-voided-P103` 留档；结果目录无一次清空；报告写到 §七 止，§九 归守门人 |
| 8 不碰他人未提交改动／暂存项／未跟踪资产；共享主树禁 `commit` | 未犯 | 本刀零 `git commit`／`add`／`checkout`／`reset`／`stash`／`clean`／`rm`／`apply`／`merge`／`tag`／`cherry-pick`／`push`；主树 `git status --short` 出场原样仍含他人两枚 `M ` 与全部 `??`，本刀在主树只新建自己那一份报告（未提交、未 `git add`） |

### 七.2 交付形态（对齐 `docs/agent-engineering-standards.md` §10 四项）

① **结论**：14 枚 B 类换人复采跑完九轮，八轮清洁有效（10 枚红格有本轮换手凭据），M8 因波 18 追加段连坐实测 5≠预期 4 而整轮作废（其 4 枚无凭据、不署）；判据字节全程 `4f1b2f6e…`，载体终态 0 写入，在册基线 1406 第五次同值复采且本刀未改动它。② **证据**：§一–§四 的归档路径＋13 枚 `.trx`＋11 份预检＋18 次判据 SHA。③ **未验证**：§六.4 逐条。④ **下一步归属**：M8 那四枚与 B 类剩 5 枚、D 类 28 枚＝下一把（连坐面前置零-`dotnet` 推演）；本刀不排产、不催作者拍板。

**本刀在主树新建的唯一文件＝本报告**，未提交、未 `git add`；报告只写到 §七，§九 由守门人追加。



## 九 守门人裁决（2026-10-29 02:5x 追加；§〇–§七 一字未改，删除列恒 0）

### 九.0 先改我自己的表，再裁它的账

上一把工单 §一.5 那行「波 13 `run-full`｜解决方案级全量｜4 份 TRX、failed 全 0」是**我写错的**。我另起一台与 lane 无关的独立仪器 `.local-notes/wave39-1029/gk39-selfcheck.js`，对八面带四元限定的盘上老读数复算＝**7/8 AGREE**，唯一不 AGREE 的那面就是这一面：`.local-notes/qa-wave13-1023/runs/run-full/` 实测 4 份、`total` 求和 1228／`executed` 1226、`<Counters failed>` 求和 **2**，红名＝`Pry.Api.Tests.ResourceLimitLedgerCoverageTests.Every_limit_shaped_constant_has_a_row_in_the_ledger`（帧 `:50`）与 `…The_guard_sees_a_row_being_dropped_and_a_row_drifting_off_its_line`（帧 `:110`），那一份 `<ResultSummary outcome="Failed">` 同场在场，Core 那份仍是 total 607／executed 605、`NotExecuted` 开标签 2 枚同名。那两枚红后来由波 13 自己收尾的台账补行结清，与本报告无关，但**它当时在盘上就是红的**。

结论：坏的是出表人的读数，不是仪器（同一台仪器其余七面全对，含波 38 那三面 26 格定向与 1406 全量）。lane 的处置正确——它没有把这面当自证面用、没有改盘上产物、没有改工单那一行，只登记差额并原样上报。口径升级一条进第四十波：**已知读数表的每一面必须由出表人当场从盘上重取，不许凭前一份报告的转述或我自己的记忆填写**。这条与 §九.4 那条枚举面错误同族，两处都是"我在工单里写了没重取的数"。

### 九.1 九轮形状与红名归属（我的仪器读数，不复用 lane 的结论）

`.local-notes/wave39-1029/gk39-verify.js` 逐份只数 `<UnitTestResult>` 开标签并与同份 `<Counters>` 双向对账：

| 轮 | total | executed | failed（开标签＝Counters） | notExec 开标签 | 红名归属 | 裁决 |
|---|---|---|---|---|---|---|
| M1 | 56 | 56 | 1＝1 | 0 | `Compiler_rejects_a_negative_section_share` | 有效 |
| M2 | 56 | 56 | 3＝3 | 0 | `Compiler_refuses_a_window_that_leaves_no_positive_input_budget`＋`Round_does_not_call_the_provider_when_the_context_is_rejected`＋`Call_refuses_an_assembly_that_still_carries_a_rejection` | 有效 |
| M3 | 56 | 56 | 1＝1 | 0 | `Compiler_refuses_a_scene_without_identity_instead_of_inventing_one` | 有效 |
| M4 | 56 | 56 | 1＝1 | 0 | `Compiler_refuses_a_current_event_from_another_conversation` | 有效 |
| M5 | 56 | 56 | 1＝1 | 0 | `Compiler_refuses_the_round_when_the_mandatory_prelude_does_not_fit` | 有效 |
| M6 | 56 | 56 | 1＝1 | 0 | `Compiler_fails_loudly_when_the_reserved_directive_does_not_fit` | 有效 |
| M7 | 56 | 56 | 1＝1 | 0 | `Second_call_on_the_same_round_budget_is_refused` | 有效 |
| M8 | 56 | 56 | **5＝5** | 0 | 四枚目标＋`Provider_side_cancellation_with_a_live_host_token_is_named_timeout_not_failure` | **作废** |
| M9 | 56 | 56 | 1＝1 | 0 | `Deferred_round_registers_the_intent_without_touching_real_history` | 有效 |

八枚有效轮的清洁红合计 **10 枚**（1＋3＋1＋1＋1＋1＋1＋1），红名逐字符等于工单 §三.0 出闸前落档的事前论证，**先档后跑**（预期列原样未改，实测没有回头覆盖它）。每轮 `total` 都＝**56**（＝波 18 补格后的现树，不是波 13 当年的 49）。十枚消息族全部是 `Assert.*` 型（M1／M3／M4／M6＝`Assert.False()`；M2／M5／M9＝`Assert.Equal()` 值不等；M7＝`Assert.Throws() Failure: No exception was thrown`），**全部 ASSERT-RED、零枚 EXCEPTION-RED**——M7 那枚虽然消息写"No exception"，那是 `Assert.Throws` 自己的断言失败，不是整格被产品异常拖红，断言级凭证照签。红点帧全部落在判据文件内、与各格方法起始行相差 ≤8 行（`Compiler_rejects_a_negative_section_share` 起始 347／帧 352 等），属波 35–38 已登记的 Release 序列点家族，主证＝testName＋消息，行号只作辅证，不影响署名。

M8 作废的执行判定我复核过：多出的那枚红名与帧（`:865`）与该格 `Assert.Equal(NarrativeRoundDisposition.ProviderRejected, result.Disposition)` 的落点相符，五枚消息族同为 `Expected: ProviderRejected / Actual: Silence` ⇒ 是"同一枚常量被改坏导致五枚见证者同响"，不是异常扩散。按 §三 首部与 §七.4 整轮作废＝**批次红不许署给单枚突变**，四枚目标一枚都不签，多出的那枚也不签。作废轮产物留盘形态＝`out/trx-M8-voided-P103/`（未删、未清空、未重跑同名目录）。

### 九.2 换手账：本刀 14 枚里 **10 枚结清／4 枚未签**

签账资格那把尺第四次按同一形态使用成动作：**判据字节全程未动**。凭据三条——① lane 九轮 apply 侧＋还原侧 18 次读数＋出场三侧 6 次＝24 次逐字符等于 `4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`；② 我当场三面重测（主树工作副本＝载体工作副本＝`git show 51f149d:` 的 blob）同值，其余五枚文件亦三侧同值且等于 §〇.4；③ 写入守卫对 `tests/**` 硬拒**真验出三次 REFUSED**（合法 ID 的产品文本写判据＝主树侧与载体侧各一次、手写字节写产品文件一次，目标 SHA 前后同值）＋一枚正面控件真落盘并当场还原。判据文件的 last-touch 我当场重取＝主树侧 `639a579`（第十八波那把判据刀归位）、载体侧同一枚再加 `290a603`（第十三波原型归位），**都不是本刀这只手**。

结清的 10 枚＝波 13 那本账里的 **#1、#2、#3、#4、#5、#6、#13、#14、#15、#22**（我按格名逐枚对波 22 报告 §三.1 那张表点的号，不是按上一把工单的字面枚数）：M1→#1、M2→#2＋#13＋#15、M3→#3、M4→#4、M5→#5、M6→#6、M7→#14、M9→#22。这十枚的红证与第二换手**同场结清**，不重复领。

未签的 4 枚＝**#24、#25、#26、#27**（M8 那四枚，共用 `NarrativeRoundRunner.cs:115` 那一行落点）。它们今天的状态是"这一把没拿到能署的凭证"，不是"验过了"；代价如实写＝第四十波 M8′ 再跑一轮，那轮的预期枚数由我按盘上事实事前点名为**恰 5 枚**（四枚目标＋@851 那枚，第五枚的名与帧已经从波 39 的作废轮落盘取到）。**我不写"这四枚至少看过一次红"**——看过但不签，等于没结。

### 九.3 旧欠求和（当场从盘上产物写，不心算）

上一把出闸时预登记的等式是「75 → 61」（减 14 枚）。**本刀不成立**，因为 M8 作废掉 4 枚。按实际签账重求和：波 22 §三.2 的 65 格四类计数＝A 0／B 35／C 0／D 30，其中波 13 那 19 枚 B 类（#1–11、#13–15、#22、#24–27）本刀签 10 枚 ⇒ 波 13 余＝47−10＝37、波 20 余＝28，**旧欠 75 → 65**（求和式：37＋28＝65；另一路校验：75−10＝65 同值）。波 13 的 B 类现值＝19−10＝**9 枚**＝4 枚（#24–27，押 M8′）＋5 枚（#7–11，押 m07–m10，条件是 §三.0R 件 3′ 那道闭合点名门先过）。D 类 28 枚一枚未动、波 20 那 28 枚一枚未动。

### 九.4 一处我自己的枚举错误＋一条被第二次救场的口径

**错**：上一把工单 §三.0R 件 2／件 3 我写「波 18 新增的五枚」，并只点了 @890／@913／@934／@952／@975 五枚名。盘上是**七枚**：`NarrativeContextCompiler.cs` 侧的波 18 追加段（判据 `:823` 注释起）带属性格＝@832、@851、@890、@913、@934、@952、@975，49＋7＝56 与九轮每轮 `total=56` 同值。更硬的一点是——**这个 7 写在三处而我一处都没读回**：盘上算术（49＋7＝56）、同一份波 22 报告的 §〇.5（原话"第十八波那 7 枚已随 `639a579` 归位进来"）、以及那枚归位提交自己的信息（`test(narrative,wave18)` 原文＝"NarratorPrototypeTests 49->56 补 7 枚落门格"）。我没读回自己就填了 5。后果不是纸面的：漏点名的两枚里恰含 M8 的连坐那枚 @851，若按我那张五枚表推演就会把 5 枚批次红当 4 枚清洁红硬署。

**救场的那一手是 lane 的**：它按 §三.0R 件 2／件 3 自己重做枚举（按七枚、不按我列的五枚），在**起跑前**把 M8 的粗面判成 5 枚消费者并把第五枚名逐字符预判，还明确写了"这条是预判不是结论，预期列原样不动"。于是盘上多出的那枚不是"意外"而是"已登记的预测被证实"，作废判定才有依据。**口径"三响以上必须事前点名每一枚格名、批次红不许署给单枚突变"第二次救场**（第一次＝波 30 那枚一次响 3 枚的探针）。

两条升级落进第四十波：① §七.7——枚举面必须按盘上算术自取，不许转述任何报告里的"新增 N 枚"，也不许用我自己的记忆；② §三.0R 件 3′——把"事前点名"变成**硬门**：每轮起跑前报告里必须已有一张闭合的预期表（含"判不连坐的格为什么不连坐"），闭合不了就不起那一轮、定向额度原样退回、把判不清的面逐枚登记，**禁止用"跑一把看响几枚"代替推演**。候选连坐面我这次当场从盘上扫描给出 10 枚清单（断言面出现 `Disposition.Dropped`／`NotSupplied`／`PartiallyIncluded`／`DroppedSegmentCount`／`NotSuppliedSegmentCount`／`DroppedUnitCount`／`CrossScopeUnitCount`／`BudgetDroppedUnitCount`／`IncludedUnitCount`／`DispositionReason`／`EmittedText` 的格），并要求 lane 连这张清单本身也独立复算。

### 九.5 预算、仪器与形状对账（我重测的）

盘上 `.trx`＝**13 枚**＝定向 9×1＋全量 1×4，与满额形状 13 差额 0；结果目录 10 个（九个 trx 名＋`trx-M8-voided-P103`）；`dotnet` 调用＝**10 次**＝定向 9＋全量 1（预算 10，用满不越）；解决方案级构建＝**0/1**，跳过判定三段当场复核＝⑤那条 0 行式子成立＋载体 `src/Pry.Core/bin/Release/net10.0/Pry.Core.dll` 在盘（1,745,920 字节，mtime 02:10:41 落在 F 那一轮的增量重建里）＋六枚 SHA 自入场未变。报告里 `dotnet test` 命令行 **0 处**含 `--no-build`（我 grep 全文 13 次提及全是"无／不许"的口径陈述）。预检＝**P96–P105 共 10 份**（P95＝我入场时那份，不占本刀放行面），逐份 grep `VERDICT` 全 `BUILD_OK`、`HOLD` 0 份 ⇒ **HOLD 重跑额度动用 0/16**；预检枚数＝`dotnet` 次数＝定向 9＋全量 1 三方对账通过；mtime 一一配对（02:00:52→M1 … 02:04:21→M9、02:10:42→F）＝无"跳过预检起跑"的形状。第三选项（静态判读命中语法级缺陷⇒不起那一轮）**未动用**，如实登记为未动用。三计数出场复看＝`[Fact]` 53／`[Theory]` 1／`[InlineData]` 3＝56 枚格、方法 54 枚（`public void` 24＋`public async Task` 30）＋2 枚 `ProposeAsync` 夹具，文件仍 989 行 ⇒ **净新增格 0、新增数字门槛 0、改断言 0、`Assert.Skip` 0**。

### 九.6 收尾全量＝1406 的第五次同值复采（本刀不顶账）

`trx-F/` 先数满 **4 份**再逐份 `<Counters>` 相加：`108＋280＋233＋785＝1406`；executed `108＋280＋233＋783＝1404`；failed `0＋0＋0＋0＝0`；`NotExecuted` 开标签 **2 枚**逐字符点名＝`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`、`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`（与波 28–38 同名同枚，未增未减）；`total−executed＝785−783＝2`＝开标签枚数，四份 `<Counters notExecuted>` 属性仍恒 "0"（写手形态，登记为 RAWATTR，不拿属性值当读数）。M8 作废**没有**换来任何重跑、没有为对账另起构建、没有动基线。**在册基线不移动**＝1406／executed 1404／failed 0／跳过 2（旧值 1380／1357／1339 等全过期）。

### 九.7 归位裁决＝零落地（同一条判据的第五次执行）

载体终态我当场复测＝`status --short` **0 行**、`log --oneline fd93ebe..HEAD` **恰 2 行**（`51f149d`＋`1e02205`，本刀零提交）、`diff --stat fd93ebe HEAD -- src tests scripts Pry.slnx Directory.Build.props`＝6 files／822 insertions／**0 deletions**、六枚文件三侧同值等于 §〇.4 ⇒ **终态净写入 0 字节**；主树那条 0 行式子右端当场取 `0a9ce00`＝**0 行** ⇒ 在树上做归位是空转，本把唯一落地面＝文档。**没有代码刀、`main` 不因本刀挪**，"字节写完不等于可归位"与"归位空转"是同一条判据的两面，这里两面都成立。

### 九.8 出闸第四十波＝`docs/dispatch-hds-wave40-1029.md`

`HDS-WAVE40-1029`（旧欠第一片续跑＝M8′ 补那四枚＋m07–m10 那五枚带门复采）：跑序固定 M8′→m07→m08→m09→m10→全部还原并核六枚 SHA→F；预算＝构建 ≤1（三段落合法跳过）／全量 ≤1／定向 ≤5／`dotnet` ≤6／HOLD 重跑 ≤16 按刀计、预检编号从 **P106** 起；载体复用波 33 那棵、不新建 worktree、不新建产品或测试文件、**判据字节全程不动**；五枚突变文本由我预合成并逐枚并排自证（`gk40-prep.js`，异常 0）＝M8′ `0400a57714bbdd8e…`（16759 字节，与波 39 的 M8 文本同字节）、m07 `24e90d81e2d0c6be…`、m08 `a65399bbbe78c924…`、m09 `dfc917334c7f4894…`、m10 `24dbd7e558ab4362…`；m09 那枚全文件圆括号 97→96 是成对删除、净偏移守恒（§三.0R 件 4 的不变量按"净偏移"而非"括号数相等"，这条是波 39 立的）。工单 §一.5 那张表里"波 13 `run-full`"那一面已按 §九.0 改正为 failed 求和＝2 并附两枚红名。

### 九.9 押账（一条不减）

D 类 28 枚（配方是"新增形状／拆既有历史门"级设计题）；波 20 那 28 枚（不同判据文件、不同载体焦点，不许混编）；#24–27 四枚押第四十波 M8′；#7–11 五枚押 m07–m10 且**以闭合点名表为前置**；`:367` 那半句、探针正数面单独读数、HDS-007 那两棵的 `:195`／`:172`、波 28 那枚数值锁格（零门槛是作者定的，不许为凑红造门槛）、`PrototypeFiles` 写死清单的扫描盲区（登记给接线刀）、两套并行命名债（`Hds006*` 产品类型名＋契约 §7.2 的 `CognitionKind`／`CognitionOrigin`）、HDS-010 删除片、HDS-007／HDS-009／HDS-003 的生产接线（属接线刀不属证据刀）、真实模型对照（要作者一句"可以跑"）。HDS-009 那 26 枚与本刀无关，本刀不重跑、不顶替。

### 九.10 未验证面（一条不减，不用"已完成"混过去）

真实模型对照＝零读数；生产接线＝零真实调用方（HDS-003 这 56 枚至今只有测试在跑，005 落库面与 004 生命周期一律没接）；旧数据真实继承＝零读取；UI 可见行为＝零读数；生产库与 `%LOCALAPPDATA%\PryCompanion`＝零接触；待标定参数（关系／心情各档阈值、经验有效期、点击归因窗口）全部未定＝**特性不是缺陷**；并发与断电＝零读数；覆盖率＝零读数；旧链删除片与"构建时不把旧链打进包"那道门＝**未开始**；Release 序列点把红点记到多行调用起始行的机理＝已知形状未证（本刀 10 枚红点全部落在事前登记的格邻域，没有一次靠该机理救回来，这不构成对该机理的证成）；断言级子账未结的 `:367`／探针正数面／`:195`／`:172` 四条照旧。**四句话照旧不许出现在任何页面**＝新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链。拆法原文照旧 **46／45／1＝W2**、catalog **76** 未动、在册基线 **1406** 不移动。
