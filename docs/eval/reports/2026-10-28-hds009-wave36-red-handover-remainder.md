# HDS-009 第一片·第三刀的剩余七轮（续跑证据刀）报告：给波 34 那 4 枚仍无红的格逐枚取红＋结清波 34 §九.3 那条断言级子账＋用"往被扫产品文件注入样本"这条路拿下两枚扫描格的红证与第二换手（HDS-WAVE36-1028）

- 工单：`docs/dispatch-hds-wave36-1028.md`（唯一权威工单）
- 执行方：冷启动 lane（载体＝沿用第三十三波那棵，未新建 worktree）
- 性质：证据刀·续跑（零新增判据格、零产品字节终态、七轮零判据字节写入、零落地、零归位、零顶账）
- 本刀不跑：A1／A2／A3（波 35 已由那只手拿到红证与换手）、波 33 的 M1–M7（押第三十七波）

> 状态词、结案词、在册基线判定、划账由守门人写，本 lane 不代写、不自封。四句话照旧不写＝新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链。

## 〇 开工前必读与入场

### 〇.1 开工前必读（全部读完整页；本节登记"读了什么"，不引用其数字作为本刀读数）

1. `docs/dispatch-hds-wave36-1028.md`（本刀唯一权威工单，逐字读完：载体、预算 `dotnet` ≤8／全量 ≤1／定向 ≤7／HOLD 重跑 ≤16 且间隔 ≥5 分钟、跑序 A4→A5→A6→A7→A8→C1→C2→F、七轮各自预期恰响格名与枚数、盘上预合成突变文本位置与 SHA、报告路径、§四 那本"不许重跑 A1–A3／不许碰 M1–M7"的押账）。
2. `docs/agent-engineering-standards.md`（全文；交付形态按 §10 四项：问题与根因／实际改动／验证结果／剩余问题）。
3. `docs/plans/README.md`（现行工作规则权威副本 14 条）与 `docs/plans/PROJECT_PLAN.md` 的 §4.2 HDS-009 行（第 82 行）＋§9 最新一条 entry（「收第三十四波」那条之后由守门人追加的「收第三十五波」登记，含 §九.3 的排期改判：本刀先跑波 35 未完的七轮，M1–M7 改押第三十七波）。
4. `docs/dispatch-hds-wave35-1028.md` §一（预算口径）与 §三.0（十轮事前论证表＝本刀七轮的**权威论证原文**，本刀不重做、不改写）。
5. `docs/eval/reports/2026-10-28-hds009-wave35-red-proof-handover.md`（§三.0 表＋§三.A 三轮十一项写法＝样板＋守门人 §九.0 仪器口径／§九.2 现账与签账资格／§九.3 排期改判与 A3 红点行号那条仪器已知形状／§九.5 押账）。
6. `src/Pry.Core/Narrative/StateAndLearning/` 五枚产品文件（逐行读：`StateLayering.cs` 102 行／`NarrativeAuthority.cs` 46 行／`DeliveryAttribution.cs` 94 行／`ExperienceMaterial.cs` 57 行／`LegacyInheritance.cs` 78 行）。
7. `tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs`（判据本体 445 行、26 枚格；**本刀对它零字节写入**）。
8. `.local-notes/wave35-1028/`（上一把留下的仪器 `w35-count.py`／`w35-mutate.py` 与 `mutation-prep/` 十枚预合成文本）——本刀**自写**仪器（`w36-count.py`／`w36-mutate.py`）并按 §一.4 先自证七面；预合成文本的 SHA 由本刀当场复算（不是抄盘上文件）。

### 〇.2 入场读数（当场取，未引用工单数字）

- `date`：`Wed Oct 28 22:18:13     2026`（第一次取）／`Wed Oct 28 22:22:42     2026`（写本节时复取）
- `git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD`：入场第一次＝`a955f227d31d816286d9469c178659ba18424d14`；**复取＝`fa6d24f85393abd7cc7d940537547951729a909e`**（在我读文档期间守门人把第三十五波收口文档刀落了盘＝`fa6d24f docs(plan,sprint,eval,wave35)`，其改动文件面＝`docs/dispatch-hds-wave36-1028.md`＋`docs/eval/reports/2026-10-28-hds009-wave35-red-proof-handover.md`＋`docs/full-scope-sprint.md`＋`docs/plans/PROJECT_PLAN.md` 四枚文档，`594 insertions(+), 4 deletions(-)`，`src`／`tests` 零字节）⇒ 本刀 §〇.3 那条 0 行式子的右端**按复取后的线尾现取**，不沿用 a955f22。
- `git status --short --branch`（主树原样，22:22 复取）：
  ```
  ## codex/ui-main-window-redesign
  M  docs/dispatch-tip1020-02-memory-group-judgment.md
   M docs/plans/PROJECT_PLAN.md
  M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs
  ?? .qoder/
  ?? docs/character-learning-safety.patch
  ?? docs/research/HDS_INTERLUDE.md
  ?? docs/research/HDS_QQ_RESEARCH_INDEX.md
  ?? docs/research/QQ_BRIDGE.md
  ```
  两枚 `M ` 暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）与全部 `??`＝他人资产：本刀不读内容、不改、不提交、不 `stash`／`clean`／`rm`。` M docs/plans/PROJECT_PLAN.md` 是在册总计划的在途登记，不属本刀文件面。（入场 22:18 那次读数与此仅差一行 `?? docs/dispatch-hds-wave36-1028.md` 与 `?? docs/eval/reports/2026-10-28-hds009-wave35-red-proof-handover.md`——那两枚在我读完工单后被守门人提交，故 22:22 起不再是未跟踪项。）
- `git worktree list`（与本刀有关的两行；余者为他人 worktree，未动）：主树 `C:/Users/24782/Desktop/Pry_Project  fa6d24f [codex/ui-main-window-redesign]`；载体 `C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave33-1028/carrier  51f149d (detached HEAD)`＝本刀沿用，**未新建 worktree**。
- `git log --oneline -3`（主树，22:22 复取）：`fa6d24f`（第三十五波收口文档刀）→ `a955f22`（第三十四波收口）→ `9a15e2a`（第三十三波收口）。

### 〇.3 载体入场自证（右端一律当场 `git rev-parse HEAD` 取，不预写 SHA）

- `git -C <carrier> rev-parse HEAD` = `51f149d0b722e28a6566c93d19f48a94ca4c85df` ✓（等于工单指定线尾）
- `git -C <carrier> status --short` = **0 行** ✓（当场 `| wc -l` ＝0，输出为空）
- `git -C <carrier> log --oneline fd93ebe..HEAD` = **2 行** ✓（`1e02205` 落六枚新文件＋`51f149d` 一处判据整形）；本刀零提交，出场仍应是这 2 行。
- `git -C <carrier> diff --stat fd93ebe HEAD -- src tests scripts Pry.slnx Directory.Build.props` = **6 files changed, 822 insertions(+), 0 deletions** ✓（逐枚行数 `DeliveryAttribution.cs` 94／`ExperienceMaterial.cs` 57／`LegacyInheritance.cs` 78／`NarrativeAuthority.cs` 46／`StateLayering.cs` 102／`StateAndLearningPrototypeTests.cs` 445）
- 主树侧那条 0 行式子（右端＝当场 `git rev-parse HEAD`＝`fa6d24f85393abd7cc7d940537547951729a909e`）：`git -C <main> diff --stat 51f149d fa6d24f -- src tests scripts Pry.slnx Directory.Build.props` = **0 行**（当场跑，输出为空，`| wc -l`＝0）✓ ⇒ 载体树与主线归位后的产品／判据字节逐字符相同，本刀在载体里取到的红就是主线那棵树的红。
- EOL 形状复核（决定"按字节还原"能否成立）：`.gitattributes:3` 有 `*.cs text eol=lf`；载体五枚产品文件 `grep -c $'\r'` 全＝**0** ⇒ 工作副本字节＝blob 字节，还原写成 blob 原字节即等价于 checkout 形状。

### 〇.4 六枚文件入场 SHA-256 基准（当场 `sha256sum` 三侧各取一次；不从工单抄）

`git show 51f149d:<path>` 的 blob＝载体工作副本＝主树工作副本，三侧逐字符相同（本刀七轮每轮 ⑩ 项复核对齐这张表，判据列即"全程零字节写入"的那枚）：

| 文件（相对路径） | 主树工作副本＝载体工作副本＝`51f149d` blob（三侧同值） |
|---|---|
| `src/Pry.Core/Narrative/StateAndLearning/StateLayering.cs` | `af727a9c04355d853f756a8cd6d22e374ac889042c847f99906784e0bb20ca3a` |
| `src/Pry.Core/Narrative/StateAndLearning/NarrativeAuthority.cs` | `000f2fc8bfd5d73ba7863029c8acafd7aa9df92ef11c910ccda3a28db3f13c97` |
| `src/Pry.Core/Narrative/StateAndLearning/DeliveryAttribution.cs` | `5858a75da3ed70e35f93782e1d760c87eea3feeecdafcdd645b27f69ea66f0ab` |
| `src/Pry.Core/Narrative/StateAndLearning/ExperienceMaterial.cs` | `289fc89be087fa35bace12b33805060562a210764a9774102b5a2875fcd55169` |
| `src/Pry.Core/Narrative/StateAndLearning/LegacyInheritance.cs` | `6a1f50999e7caf1d6d29cc07bbe752f89ea17851999013c00b167e590f3498ab` |
| 判据 `tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs` | `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc` |

判据文件全程零字节写入＝本刀七轮（含 C1/C2 两枚扫描格）能签第二换手的唯一凭据。判据字节的 last-touch 在场：主树侧 `git log -1 -- <判据>`＝`e04e7e6`（守门人按路径落地的波 33 代码刀）、载体侧 `51f149d`（波 33 的判据整形）——**两者都不是本刀这只手**。

### 〇.5 在册基线起点（当场从波 35 报告守门人 §九.2 末段读到的原句，不做加法）

- 波 35 报告守门人 §九.2 原文：「在册基线照旧 **1406／executed 1404／failed 0／跳过 2**（本刀零全量调用，不顶账、不为顶账起构建）」；同一把尺的上一处登记＝波 34 守门人 §九.1 第 3 项的同值复采、更早＝波 33 把 1380 顶到 1406。本刀 §〇.5 起点＝**1406／executed 1404／failed 0／跳过 2**（分项 Client 108／App 233／Api 280／Core 785、Core executed 783）。第三面盘上自证＝本刀仪器直读波 33 `out/trx-F` 那四份原始 TRX 求和＝total 1406／executed 1404／passed 1404／countersFailed 0／`NotExecuted` 开标签 2 条（见 §〇.6 的 F4 面实测）。
- 本刀**新格数＝0**（工单 §二 明令），所以 F 轮出口读数应当**仍是 1406**：读到别的数先怀疑树没还原干净，不改判据、不改基线、照实登记并作废那一轮。

### 〇.6 取数仪器与它的自证（口径⓪：先证明仪器读得对，再采信它的读数）

仪器＝本 lane 自写 `.local-notes/wave36-1028/w36-count.py`（**不复用** wave35 的 `w35-count.py`／守门人的 `gk35-audit.js`，也不复用 wave33／34 的实现）。三面＝① 逐份读 `<Counters>`（total／executed／passed／failed／notExecuted／error）；② **两条互相独立的数红路**：A 面＝逐 occurrence 找 `<UnitTestResult` 开标签、按该标签内 `outcome` 属性计红（自闭合 `…/>` 与成对写法同一条切法，**全程不用开—闭配对正则**），红格名／消息首行／栈里 `:line N` 从同一切片取；B 面＝反向锚定——逐 occurrence 找 `outcome="Failed"` 再回溯最近的 `<` 判标签名（这一面专门用来抓 `<ResultSummary outcome="Failed">` 那种"不算用例"的结点），A≠B 即打印 `SELF-CHECK MISMATCH`；红数与同份 `<Counters failed>` 不等即打印 `DISAGREE`；③ 每个红格名先剥全限定前缀与 `[Theory]` 参数表，再回判据源码按"方法名＋`(`"验真实存在。

起跑第一轮之前先拿盘上**七组**已知读数自证（工单 §一.4 点名的七面，整段输出归档 `out/instrument-selfproof.txt`，退出码 0）：

| 面 | 期望（工单 §一.4） | 仪器实测 | 对账 |
|---|---|---|---|
| F1 波 33 `trx-M1` | 恰 1 | `countersFailed=1 faceA=1 faceB=1 selfCheck=OK reconcile=OK`，红名 `…A_narrative_output_may_not_rewrite_the_stable_canon_layer`，消息首行 `Assert.Equal() Failure: Strings differ`，栈 `:47`，`inCriteria=True` | AGREE |
| F2 波 33 `trx-M5` | 恰 1 | 1/1，红名 `…A_promise_send_proposes_a_commitment_but_proves_neither_consent_nor_completion`，`Assert.False() Failure`，栈 `:244` | AGREE |
| F3 波 33 `trx-M7` | 恰 1 | 1/1，红名 `…Reading_experience_as_optional_material_without_a_validity_window_fails_loudly`，`System.InvalidOperationException : Nullable object must have a value.`，栈 `:46,:256` | AGREE |
| F4 波 33 `trx-F` | 4 份且 `failed` 全 0；total 求和 1406／executed 1404／passed 1404；两条 `NotExecuted` 按开标签点名 | 4 份各 `0/0`；逐份 total 108／280／785（executed 783）／233；求和 total=1406／executed=1404／passed=1404／countersFailedSum=0／faceAFailedSum=0；`NotExecuted` 开标签 2 条＝`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`／`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key` | AGREE |
| F5 波 34 `trx-A1` | 恰 2 | 2/2，两枚红名 `…A_candidate_touching_all_three_surfaces_is_caught_by_the_first_gate`（`:167`）＋`…A_narrative_candidate_may_not_write_intimacy_state`（`:131`） | AGREE |
| F6 波 34 `trx-A6` | 恰 1，红名逐字符含参数表 | 1/1，红名 `…A_delivered_body_that_was_suppressed_deduplicated_or_rejected_is_not_attribution_evidence(suppressed: True, deduplicated: False, rejected: False)`，栈 `:195` | AGREE |
| F7 波 35 `trx-A1\|A2\|A3` | 各恰 1，红点 `:64`／`:115`／`:195` | 三份各 1/1；红名 `An_author_side_canon_revision_…`(`:64`)／`An_overlay_with_source_and_version_…`(`:115`)／theory 第二行 `(suppressed: False, deduplicated: True, rejected: False)`(`:195`) | AGREE |

末行打印 `SELFPROOF FACES=7 AGREED=7 DISAGREED=0 => ALL SEVEN FACES AGREE`。**过程如实登记**：第一版仪器有两处自己的缺陷（① F4 的期望匹配拿裸方法名去比 TRX 里带 `Pry.Core.Tests.` 全限定前缀的 `NotExecuted` 名字，判成 DISAGREE；② 消息抽取没解 `&#xD;` 这类数字实体，首行尾部挂着转义串），修的是**仪器**、判据与盘上 TRX 一个字节未动，修完七个面整体重跑；另有一处 `collect()` 收到字符串时逐字符迭代的崩溃。这是"仪器坏了先修仪器"的第一次现场执行，登记在此以免被读成"一次就跑对了"。

### 〇.7 焦点预检与预算声明

- 预检脚本＝`.local-notes/game-preflight6-1022.ps1`，每一次 `dotnet`（含每一枚突变轮与收尾全量）之前当场重取；编号从 **P72** 起；整段输出贴进报告并归档 `out/preflight-<ID>.txt`。只有 `VERDICT=BUILD_OK` 才起跑；读到 `HOLD`＝不起跑、不请示、不改判据、不绕过，HOLD 后重跑最多累计 **16** 次、相邻两次不短于 **5 分钟**（额度按刀计）。16 次后仍 HOLD ⇒ 带着"本刀零 `dotnet` 起跑"的交付回来。
- 预算：解决方案级构建 ≤1；解决方案级全量 ≤1（语义＝交付的读数只许来自 1 次全量）；定向 ≤7；`dotnet` 总调用上限 8。七轮一律**不带 `--no-build`**。
- **B1（解决方案级构建）跳过判定登记**：§〇.3 那条主树侧 0 行式子成立 ✓（右端 `fa6d24f` 当场现取）；载体 `src/Pry.Core/bin/Release/net10.0/Pry.Core.dll` 在盘（`-rwxr-xr-x … 1745920 Oct 28 21:13`＝波 35 P65 那轮 A3 定向的产物）；六枚 SHA 自入场起未变（§〇.4 三侧同值）；载体 `status --short`＝0 行 ⇒ 按工单 §一.1 允许跳过 B1，构建预算用 0/1。每一轮定向与收尾全量自行增量重建，不存在"跑的不是当前源码"的形状。此判定与波 34／波 35 同类，依据全部本刀当场重取。
- 作废轮（如有）的结果目录一律改名留档 `trx-<轮>-voided-<预检号>/`，不清空、不删除、不重跑同名目录。
- 盘上预合成突变文本的位置＝`.local-notes/wave35-1028/mutation-prep/`（十枚，波 35 落盘 20:48）。本刀**不采信盘上那份**，改为当场用自写的 `w36-mutate.py prep` 从 `51f149d` blob 独立复合成七枚（`wave36-1028/mutation-prep/`）并与工单 §三.0 的突变态 SHA 逐字符对账（见 §三.0.1）；两侧 SHA 相同⇒"盘上那份"与"本刀复算那份"是同一批字节，`apply` 用的为本刀复算结果。
- 动第一次 `dotnet` 之前已完成的零 `dotnet` 活：§〇 全部＋§二 三计数＋§三.0 七行论证复核＋七枚突变文本当场复合成并记 SHA-256（`out/mutation-prep-sha.txt`）＋七枚突变文本对两枚扫描谓词的交叉面自证（`out/scan-predicate-crosscheck.txt`）＋仪器七面自证（`out/instrument-selfproof.txt`）。

### 〇.8 他人资产与 git 写边界声明

主树全程零 `git commit/add/checkout/reset/cherry-pick/merge/tag/push/apply/stash/clean/rm`；两枚 `M ` 暂存项与全部 `??` 未读不改。载体内只发生文件内容写入（临时突变）与逐轮按 `git show 51f149d:<path>` 的 blob 字节还原，本刀**零提交**（线尾仍 `51f149d`）。主树唯一新文件＝本报告，不 `git add`、不提交。本刀归位是空转：终态净写入 0 字节。

## 一 文件面自证（本刀实际写入清单）

- `.local-notes/wave36-1028/`（gitignored，本刀自建）：`w36-count.py`（仪器）、`w36-mutate.py`（突变＋还原＋核 SHA）、`mutation-prep/`（本刀当场复合成的七枚突变文本）、`out/`（预检、各轮 TRX、apply／run／count 存档、仪器自证、交叉面自证）。
- 主树唯一新文件＝本报告 `docs/eval/reports/2026-10-28-hds009-wave36-red-handover-remainder.md`。
- 载体内：仅 `src/Pry.Core/Narrative/StateAndLearning/*.cs` 的**临时**突变（七轮各自的宿主文件），逐轮按 blob 字节还原；终态相对 `51f149d` 净写入 0 字节。
- 判据文件 `tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs`：**零字节写入**（七轮每一轮 ⑩ 项都取 SHA-256 复核对齐 `317d2171…`）。
- 未新建任何产品文件、未新建任何测试文件、未碰 `scripts/**`／`Pry.slnx`／`Directory.Build.props`／其他 `docs/**`／主树任何文件；未往任何既有测试文件加行；未跑 A1–A3、未碰 M1–M7。

## 二 三计数自证（不许新增判据格）

判据文件（载体侧与主树侧同一枚字节，入场当场 `grep -c`）：`[Fact]` = **23**、`[Theory]` = **1**、`[InlineData` = **3** ⇒ 展开节点 23＋3＝26 枚格；`public void` 方法数 24。与波 33／波 34／波 35 逐位相同 ⇒ 本刀净新增格数＝0。本刀七轮只改产品文件（含两枚注释行的注入／摘除），不改任何断言、不改 `[InlineData]` 期望值、不 `Assert.Skip`、不注释既有断言、不新增数字门槛；终态侧将在 §六 再当场复数一次。

## 三 反证轮（事前论证的权威原文＝波 35 报告 §三.0／波 35 工单 §三.0；本刀不重做、不改写；跑序固定 A4→A5→A6→A7→A8→C1→C2→全部还原核 SHA→F）

### 三.0 七轮表（右端逐字符抄自波 35 报告 §三.0 与盘上现值；**"预期枚数／格名"是假设不是结论**：实测形状与之不符 ⇒ 整轮作废，红不署、不追第二轮、不许把本节改成实测值）

定向跑法（工单 §一.6 逐字，只换结果目录名，在载体内跑，不带 `--no-build`）：

```
dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave36-1028/out/trx-<轮>" --logger trx
```

本刀复核（零 `dotnet`，全部当场从盘上取；行号＝`51f149d` 侧；存档 `out/scan-predicate-crosscheck.txt`）：判据里 `experience_past_validity` 只 1 处（`:286`，所在方法声明 `:278`）；`inherited_source_missing` 2 处（`:365`＋`:367`）**但两处同在方法 `:356` 之内** ⇒ A8 红格数＝1；`experience_offered_as_material` 2 处（`:272`＋`:299`），A5 只把 `:52` 的材料位置空，`:299` 断的 `Reason` 不受牵连 ⇒ A5 红格数＝1；`src-inherit` 2 处（`:394` 入参加 `:399` 断言），A7 只红 `:399` 所在那格（`:389` 声明）；`body_excluded_from_attribution` 只 `:200` 一处（theory 体内）。交叉谓词面：五枚 clean blob 对两个扫描谓词全部 `legacyHits=[] defaultHits=[]`（⇒ 两枚扫描格当前为绿的形状成立）；A4–A8 五枚突变文本同样 `[]`／`[]`（不连带打红另一枚扫描格）；C1 只命中 `ReplyGenerationRenderer`（不命中 `const `）；C2 只命中 `const `（不命中任何旧链类型名）。

| 轮 | 类别 | 文件:行（`51f149d` 侧） | 盘上预合成文本（波 35 `mutation-prep/`） | 突变态 SHA-256（本刀当场复算值＝工单 §三.0 下表值才起跑） | 预期恰响格（逐字符，前缀 `Pry.Core.Tests.StateAndLearningPrototypeTests.`） | 枚数 |
|---|---|---|---|---|---|---|
| A4 | 行为 | `DeliveryAttribution.cs:60` 只摘合取第三支 `\|\| evidence.BodyRejected` | `A4_DeliveryAttribution.cs.cs` | `964070d22df97da4a061e90ebeeca9e9da441b663ea2b6a0f552914393b1e80f` ✓ | `A_delivered_body_that_was_suppressed_deduplicated_or_rejected_is_not_attribution_evidence(suppressed: False, deduplicated: False, rejected: True)` | 恰 1 |
| A5 | 行为 | `ExperienceMaterial.cs:52` 把 `match.Material` 换成 `null` | `A5_ExperienceMaterial.cs.cs` | `ae9235a2b950bab2020ff997bd3357691328189390652fe50a905881bf179b4a` ✓ | `Experience_within_the_input_validity_window_is_offered_as_optional_material` | 恰 1 |
| A6 | 文本面 | `ExperienceMaterial.cs:50` 把 `"experience_past_validity"` 改成另一串 | `A6_ExperienceMaterial.cs.cs` | `1fcfcd828b9a0d1977c8d5b576d55f19689292feac469d68bc4d9873782bbf5c` ✓ | `Experience_past_the_input_validity_window_is_not_offered` | 恰 1 |
| A7 | 行为 | `LegacyInheritance.cs:57` 把 `input.Source.SourceId` 换成 `input.StateKey` | `A7_LegacyInheritance.cs.cs` | `51c9cb9c29e8745f0e79e4468fc73ab3344afd11c3fca2428e7705ebe3ba9252` ✓ | `A_successful_inheritance_carries_its_source_reference_and_version` | 恰 1 |
| A8 | 文本面 | `LegacyInheritance.cs:53` 把 `"inherited_source_missing"` 改成另一串（＝波 34 §九.3 那条断言级子账的方向） | `A8_LegacyInheritance.cs.cs` | `2ff1fa21ed9e275401cffd7827954c0ea1aac48fd030d447d3b9e88dc2dd36a1` ✓ | `Inherited_state_without_a_pointable_source_fails_loudly_and_is_never_silently_dropped` | 恰 1 |
| C1 | 注入（红证＋换手） | `StateLayering.cs` 文末追加一行注释 `// MUTATION C1 (temporary, wave35): ReplyGenerationRenderer` | `C1_StateLayering.cs.cs` | `2ab20e48fcb6b47a507eb5d20210685d59ce54df187c8fcea8ec12ea2c84bb7c` ✓ | `State_and_learning_prototype_sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites` | 恰 1 |
| C2 | 注入（红证＋换手） | `StateLayering.cs` 文末追加一行注释 `// MUTATION C2 (temporary, wave35): const 7` | `C2_StateLayering.cs.cs` | `d9940f120f9791e1bb635599e953125c0d15a849756416a0cae8662fb82baba4` ✓ | `State_and_learning_prototype_declares_no_source_side_numeric_default` | 恰 1 |

**A8 的额外判据（工单 §三）**：只有当红点落在判据里那句拒因字面量断言（`:365`／`:367` 所在的断言序列）才算结清波 34 §九.3 那条断言级子账；若红点仍落在异常那一侧（波 34 那轮整格因产品侧 `NullReferenceException` 而红），照实登记"子账未结、形状不符"，**不许为了让它响而改 `LegacyInheritance.cs:52`**（那是另一枚突变，不在本刀额度内）。

**为什么 C1/C2 这一类能同时结"红证＋换手"**：本刀走的是"判据字节零写入＋把样本串注入被扫的产品文件"这条路（那两个扫描函数按 `Contains` 连注释一起匹配），红点落在前半句 `Assert.Empty`、阳性探针句保持绿；凭据只有 §一.5 的 ⑩ 项——七轮每一轮判据文件 SHA-256 都等于 `51f149d` 的 blob。

**跑序固定＝A4→A5→A6→A7→A8（这五轮判据字节必须全程等于 blob 值）→C1→C2→全部还原并核六枚 SHA→F（收尾全量）。** 未跑满 A 类不许起 C 类，未跑满七轮不许起 F。

### 三.0.1 本刀当场复合成的突变文本 SHA-256（动第一次 `dotnet` 之前完成，零 dotnet；存档 `out/mutation-prep-sha.txt`）

| 轮 | 宿主文件 | clean blob 前缀 | 突变态 SHA-256（本刀复算） | 字节数变化 | 与工单 §三.0 对账 |
|---|---|---|---|---|---|
| A4 | `DeliveryAttribution.cs` | `5858a75da3ed` | `964070d22df97da4a061e90ebeeca9e9da441b663ea2b6a0f552914393b1e80f` | 4622→4657 | ✓ 逐字符相同 |
| A5 | `ExperienceMaterial.cs` | `289fc89be087` | `ae9235a2b950bab2020ff997bd3357691328189390652fe50a905881bf179b4a` | 3020→3078 | ✓ |
| A6 | `ExperienceMaterial.cs` | `289fc89be087` | `1fcfcd828b9a0d1977c8d5b576d55f19689292feac469d68bc4d9873782bbf5c` | 3020→3108 | ✓ |
| A7 | `LegacyInheritance.cs` | `6a1f50999e7c` | `51c9cb9c29e8745f0e79e4468fc73ab3344afd11c3fca2428e7705ebe3ba9252` | 3988→4066 | ✓ |
| A8 | `LegacyInheritance.cs` | `6a1f50999e7c` | `2ff1fa21ed9e275401cffd7827954c0ea1aac48fd030d447d3b9e88dc2dd36a1` | 3988→4077 | ✓ |
| C1 | `StateLayering.cs` | `af727a9c0435` | `2ab20e48fcb6b47a507eb5d20210685d59ce54df187c8fcea8ec12ea2c84bb7c` | 5445→5505 | ✓ |
| C2 | `StateLayering.cs` | `af727a9c0435` | `d9940f120f9791e1bb635599e953125c0d15a849756416a0cae8662fb82baba4` | 5445→5489 | ✓ |

七枚全对 ⇒ 允许起跑。（每轮 `apply` 后仪器实测的突变态 SHA 必须等于本表值，等于即"跑的就是预合成那份文本"；不等＝当场停手登记，不解释为红。）

## 三.P 焦点预检台账（逐枚当场登记，含 HOLD；每一次 `dotnet` 之前一枚）

| 编号 | 时刻（当场 `date`） | VERDICT | 归档 | 处置 |
|---|---|---|---|---|
| P72 | 22:24:52 | **BUILD_OK** | `out/preflight-P72.txt`（整段原文见 A4 节⑪） | 放行 A4（本刀 `dotnet` 计数 1）；`free_memory_gb=11.8`、前台＝Godot pid=53448 class=Engine `covers=False` |
| P73 | 22:25:54 | **BUILD_OK** | `out/preflight-P73.txt`（整段原文见 A5 节⑪） | 放行 A5（`dotnet` 计数 2）；前台＝Godot pid=53448 `covers=False`、`free_memory_gb=10.8` |
| P74 | 22:26:08 | **BUILD_OK** | `out/preflight-P74.txt`（整段原文见 A6 节⑪） | 放行 A6（`dotnet` 计数 3）；前台＝Godot pid=40976 `covers=False`、`free_memory_gb=10.7` |
| P75 | 22:27:19 | **BUILD_OK** | `out/preflight-P75.txt`（整段原文见 A7 节⑪） | 放行 A7（`dotnet` 计数 4）；前台＝Godot pid=53448 `covers=False`、`free_memory_gb=11.5` |
| P76 | 22:27:37 | **BUILD_OK** | `out/preflight-P76.txt`（整段原文见 A8 节⑪） | 放行 A8（`dotnet` 计数 5）；前台＝Godot pid=53448 `covers=False`、`free_memory_gb=11.3` |
| P77 | 22:27:54 | **BUILD_OK** | `out/preflight-P77.txt`（整段原文见 C1 节⑪） | 放行 C1（`dotnet` 计数 6）；前台＝Godot pid=40976 `covers=False`、`free_memory_gb=11.4` |
| P78 | 22:28:20 | **BUILD_OK** | `out/preflight-P78.txt`（整段原文见 C2 节⑪） | 放行 C2（`dotnet` 计数 7）；前台＝Godot pid=53448 `covers=False`、`free_memory_gb=11.2` |
| P79 | 22:32:56 | **BUILD_OK** | `out/preflight-P79.txt`（整段原文见 §三.F） | 放行 F（`dotnet` 计数 8＝本刀最后一枚）；前台＝Godot pid=53448 `covers=False`、`free_memory_gb=11.7` |

七轮定向＋一枚收尾全量＝8 次 `dotnet`，每次之前当场一枚预检，零跳过、零 `HOLD`、零重跑额度动用（`HOLD` 重跑额度 0/16）。台账"时刻"＝预检归档写入时刻（`date` 与文件 mtime 同源，差 ≤2 秒）。

（下一枚预检在本行之前追加）

## 三.A／三.C 定向轮逐轮十一项（一轮一节，跑完即追加）

### A4（行为·摘合取第三支）——预检 P72＝BUILD_OK

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `src/Pry.Core/Narrative/StateAndLearning/DeliveryAttribution.cs:60` |
| ② 改前原文 | `        if (evidence.BodySuppressed \|\| evidence.BodyDeduplicated \|\| evidence.BodyRejected)` |
| ③ 改后原文 | `        if (evidence.BodySuppressed \|\| evidence.BodyDeduplicated)  // MUTATION A4 (temporary, wave35): third conjunct removed` |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/StateAndLearning/DeliveryAttribution.cs | 2 +-`／` 1 file changed, 1 insertion(+), 1 deletion(-)`（存档 `out/a4-apply.txt`） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave36-1028/out/trx-A4" --logger trx` |
| ⑥ TRX 文件名 | `out/trx-A4/24782_PHYTHLINK_2026-10-28_22_25_12.trx` |
| ⑦ 红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.StateAndLearningPrototypeTests.A_delivered_body_that_was_suppressed_deduplicated_or_rejected_is_not_attribution_evidence(suppressed: False, deduplicated: False, rejected: True)`（theory 参数与预论证逐字符相同＝只有第三行漏过闸①）；消息＝`Assert.False() Failure`，Expected `False`／Actual `True`；红点＝TRX 内层测试帧 `StateAndLearningPrototypeTests.cs:line 195`（源码里该断言 `Assert.False(readout.Admitted)` 在 `:199`；Release 序列点把整条断言记在它所属多行调用语句的起始行——这条映射差异是波 35 守门人 §九.3 传给本刀的**仪器已知形状**，不影响归属：testName 里就带着 `(suppressed: False, deduplicated: False, rejected: True)` 参数表）。仪器读数：`total=26 executed=26 passed=25 countersFailed=1 faceA_openTagFailed=1 faceB_attrBackwardFailed=1 selfCheck=OK reconcile=OK notExecutedOpenTags=0 allRedNamesInCriteria=True` |
| ⑧ 还原命令 | `python w36-mutate.py revert A4`（内部＝把 `git show 51f149d:src/Pry.Core/Narrative/StateAndLearning/DeliveryAttribution.cs` 的字节原样写回该路径） |
| ⑨ 还原后 `git status --short` 行数 | `0` |
| ⑩ SHA-256 | 突变态实测 `964070d22df97da4a061e90ebeeca9e9da441b663ea2b6a0f552914393b1e80f` ＝§三.0.1 复算值＝工单 §三.0 值（match）；还原后 `5858a75da3ed70e35f93782e1d760c87eea3feeecdafcdd645b27f69ea66f0ab` ＝ blob（match）；**本轮判据字节未写入**：判据文件 `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc`（apply 时与 revert 时各测一次，两值均等于 `51f149d` blob） |
| ⑪ 当轮预检 | P72＝`VERDICT=BUILD_OK`（22:24:52，归档 `out/preflight-P72.txt`；整段原文见下） |

P72 整段输出原文：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: Godot pid=53448 class=Engine style=0x16CF0000 rect=248,411,956x599 screen=2560x1440 covers=False borderless=False title=???? ? ???? (DEBUG)
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=13% used=2073MB
free_memory_gb=11.8
VERDICT=BUILD_OK
```

形状裁决＝实测恰 1 枚、格名（含 theory 参数串）与 §三.0 预论证逐字符相同 ⇒ 与事前论证一致，本轮有效（红是否署名由守门人裁，本 lane 不写状态词）。

### A5（行为·存了不把材料交出去）——预检 P73＝BUILD_OK

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `src/Pry.Core/Narrative/StateAndLearning/ExperienceMaterial.cs:52` |
| ② 改前原文 | `        return new MaterialReadout(bodyId, true, "experience_offered_as_material", match.Material, false);` |
| ③ 改后原文 | `        return new MaterialReadout(bodyId, true, "experience_offered_as_material", null, false);  // MUTATION A5 (temporary, wave35): stored material not handed out` |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/StateAndLearning/ExperienceMaterial.cs | 2 +-`／` 1 file changed, 1 insertion(+), 1 deletion(-)`（存档 `out/a5-apply.txt`） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave36-1028/out/trx-A5" --logger trx` |
| ⑥ TRX 文件名 | `out/trx-A5/24782_PHYTHLINK_2026-10-28_22_25_59.trx` |
| ⑦ 红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.StateAndLearningPrototypeTests.Experience_within_the_input_validity_window_is_offered_as_optional_material`；消息＝`Assert.Equal() Failure: Strings differ`，Expected `"a warm exchange"`／Actual `null`；红点＝`StateAndLearningPrototypeTests.cs:line 273`（＝预论证指明的 `Assert.Equal("a warm exchange", readout.OptionalMaterial)` 那一行，无映射差异）。仪器读数：`total=26 executed=26 passed=25 countersFailed=1 faceA_openTagFailed=1 faceB_attrBackwardFailed=1 selfCheck=OK reconcile=OK notExecutedOpenTags=0 allRedNamesInCriteria=True`。同格 `:271`／`:272`／`:274` 三行（Available／Reason／CarriesSpeakingAuthority）仍绿＝"只材料为空、其余不动"的形状成立；`Experience_never_carries_authority_over_whether_to_speak` 那格断的是 `:299` 的 `Reason` 串，不受 `OptionalMaterial` 牵连＝未连带红（与 §三.0 的交叉面复核一致） |
| ⑧ 还原命令 | `python w36-mutate.py revert A5`（内部＝把 `git show 51f149d:src/Pry.Core/Narrative/StateAndLearning/ExperienceMaterial.cs` 的字节原样写回该路径） |
| ⑨ 还原后 `git status --short` 行数 | `0` |
| ⑩ SHA-256 | 突变态实测 `ae9235a2b950bab2020ff997bd3357691328189390652fe50a905881bf179b4a` ＝§三.0.1 复算值＝工单 §三.0 值（match）；还原后 `289fc89be087fa35bace12b33805060562a210764a9774102b5a2875fcd55169` ＝ blob（match）；**本轮判据字节未写入**：判据文件 `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc`（apply 时与 revert 时各测一次，两值均等于 `51f149d` blob） |
| ⑪ 当轮预检 | P73＝`VERDICT=BUILD_OK`（22:25:54，归档 `out/preflight-P73.txt`；整段原文见下） |

P73 整段输出原文：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: Godot pid=53448 class=Engine style=0x16CF0000 rect=248,411,956x599 screen=2560x1440 covers=False borderless=False title=???? ? ???? (DEBUG)
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=13% used=2020MB
free_memory_gb=10.8
VERDICT=BUILD_OK
```

形状裁决＝实测恰 1 枚、格名与 §三.0 预论证逐字符相同，红点落点＝预论证指明的 `:273` ⇒ 与事前论证一致，本轮有效。

### A6（文本面·超期拒因字面量）——预检 P74＝BUILD_OK

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `src/Pry.Core/Narrative/StateAndLearning/ExperienceMaterial.cs:50` |
| ② 改前原文 | `            return NotOffered(bodyId, "experience_past_validity");` |
| ③ 改后原文 | `            return NotOffered(bodyId, "experience_past_validity_wave35_alias");  // MUTATION A6 (temporary, wave35): text face of the past-validity reason` |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/StateAndLearning/ExperienceMaterial.cs | 2 +-`／` 1 file changed, 1 insertion(+), 1 deletion(-)`（存档 `out/a6-apply.txt`） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave36-1028/out/trx-A6" --logger trx` |
| ⑥ TRX 文件名 | `out/trx-A6/24782_PHYTHLINK_2026-10-28_22_26_14.trx` |
| ⑦ 红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.StateAndLearningPrototypeTests.Experience_past_the_input_validity_window_is_not_offered`；消息＝`Assert.Equal() Failure: Strings differ`（pos 24，Expected `"experience_past_validity"`／Actual 带 `_wave35_alias` 后缀）；红点＝`StateAndLearningPrototypeTests.cs:line 286`（＝预论证指明的唯一消费点，该串在判据只 `:286` 一处）。仪器读数：`total=26 executed=26 passed=25 countersFailed=1 faceA_openTagFailed=1 faceB_attrBackwardFailed=1 selfCheck=OK reconcile=OK notExecutedOpenTags=0 allRedNamesInCriteria=True`；同格 `Assert.Null(readout.OptionalMaterial)`（`:287`）不受影响＝整格仍红、红点唯一 |
| ⑧ 还原命令 | `python w36-mutate.py revert A6`（内部＝把 `git show 51f149d:src/Pry.Core/Narrative/StateAndLearning/ExperienceMaterial.cs` 的字节原样写回该路径） |
| ⑨ 还原后 `git status --short` 行数 | `0` |
| ⑩ SHA-256 | 突变态实测 `1fcfcd828b9a0d1977c8d5b576d55f19689292feac469d68bc4d9873782bbf5c` ＝§三.0.1 复算值＝工单 §三.0 值（match）；还原后 `289fc89be087fa35bace12b33805060562a210764a9774102b5a2875fcd55169` ＝ blob（match）；**本轮判据字节未写入**：判据文件 `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc`（apply 时与 revert 时各测一次，两值均等于 `51f149d` blob） |
| ⑪ 当轮预检 | P74＝`VERDICT=BUILD_OK`（22:26:08，归档 `out/preflight-P74.txt`；整段原文见下） |

P74 整段输出原文：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: Godot pid=40976 class=Engine style=0x16CF0000 rect=1279,308,1116x799 screen=2560x1440 covers=False borderless=False title=???? ? ??????? (DEBUG)
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=14% used=2026MB
free_memory_gb=10.7
VERDICT=BUILD_OK
```

形状裁决＝实测恰 1 枚、格名与 §三.0 预论证逐字符相同（该轮身份＝文本面，照实登记不冒充行为突变），红点落点＝预论证指明的 `:286` ⇒ 与事前论证一致，本轮有效。

### A7（行为·继承行把来源身份写错）——预检 P75＝BUILD_OK

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `src/Pry.Core/Narrative/StateAndLearning/LegacyInheritance.cs:57` |
| ② 改前原文 | `            input.Source.SourceId, input.Source.SourceVersion.Value));` |
| ③ 改后原文 | `            input.StateKey, input.Source.SourceVersion.Value));  // MUTATION A7 (temporary, wave35): inherited row carries the wrong source identity` |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/StateAndLearning/LegacyInheritance.cs | 2 +-`／` 1 file changed, 1 insertion(+), 1 deletion(-)`（存档 `out/a7-apply.txt`；`git status --short`＝当轮 1 行 ` M` 该文件） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave36-1028/out/trx-A7" --logger trx` |
| ⑥ TRX 文件名 | `out/trx-A7/24782_PHYTHLINK_2026-10-28_22_27_23.trx` |
| ⑦ 红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.StateAndLearningPrototypeTests.A_successful_inheritance_carries_its_source_reference_and_version`；消息＝`Assert.Equal() Failure: Strings differ`（pos 0，Expected `"src-inherit"`／Actual `"old-3"`＝该格 `:394` 传入的 `StateKey`，与"把 `input.StateKey` 写进来源身份字段"这枚突变指向一致）；红点＝`StateAndLearningPrototypeTests.cs:line 399`（＝预论证点名的唯一消费点 `:398-400` 中的来源身份断言；同行 `:400` 断版本号、不受牵连）。仪器读数：`total=26 executed=26 passed=25 countersFailed=1 faceA_openTagFailed=1 faceB_attrBackwardFailed=1 selfCheck=OK reconcile=OK notExecutedOpenTags=0 allRedNamesInCriteria=True` |
| ⑧ 还原命令 | `python w36-mutate.py revert A7`（内部＝把 `git show 51f149d:src/Pry.Core/Narrative/StateAndLearning/LegacyInheritance.cs` 的字节原样写回该路径） |
| ⑨ 还原后 `git status --short` 行数 | `0` |
| ⑩ SHA-256 | 突变态实测 `51c9cb9c29e8745f0e79e4468fc73ab3344afd11c3fca2428e7705ebe3ba9252` ＝§三.0.1 复算值＝工单 §三.0 值（match）；还原后 `6a1f50999e7caf1d6d29cc07bbe752f89ea17851999013c00b167e590f3498ab` ＝ blob（match）；**本轮判据字节未写入**：判据文件 `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc`（apply 时与 revert 时各测一次，两值均等于 `51f149d` blob） |
| ⑪ 当轮预检 | P75＝`VERDICT=BUILD_OK`（22:27:19，归档 `out/preflight-P75.txt`；整段原文见下） |

P75 整段输出原文：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: Godot pid=53448 class=Engine style=0x16CF0000 rect=248,411,956x599 screen=2560x1440 covers=False borderless=False title=???? ? ???? (DEBUG)
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=14% used=2030MB
free_memory_gb=11.5
VERDICT=BUILD_OK
```

形状裁决＝实测恰 1 枚、格名与 §三.0 预论证逐字符相同，红点落点＝预论证指明的 `:399` ⇒ 与事前论证一致，本轮有效。

### A8（文本面·来源缺失拒因字面量；＝波 34 §九.3 那条断言级子账的方向）——预检 P76＝BUILD_OK

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `src/Pry.Core/Narrative/StateAndLearning/LegacyInheritance.cs:53` |
| ② 改前原文 | `            return new InheritanceReadout(input.StateKey, "Failed", "inherited_source_missing");` |
| ③ 改后原文 | `            return new InheritanceReadout(input.StateKey, "Failed", "inherited_source_missing_wave35_alias");  // MUTATION A8 (temporary, wave35): text face of the missing-source reason` |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/StateAndLearning/LegacyInheritance.cs | 2 +-`／` 1 file changed, 1 insertion(+), 1 deletion(-)`（存档 `out/a8-apply.txt`；当轮 `status --short`＝1 行 ` M` 该文件） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave36-1028/out/trx-A8" --logger trx` |
| ⑥ TRX 文件名 | `out/trx-A8/24782_PHYTHLINK_2026-10-28_22_27_41.trx` |
| ⑦ 红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.StateAndLearningPrototypeTests.Inherited_state_without_a_pointable_source_fails_loudly_and_is_never_silently_dropped`；消息＝`Assert.Equal() Failure: Strings differ`（pos 24，Expected `"inherited_source_missing"`／Actual `"inherited_source_missing_wave35_alias"`）；红点＝`StateAndLearningPrototypeTests.cs:line 365`＝`Assert.Equal("inherited_source_missing", nullSource.Reason)` 那句**拒因字面量断言**（工单 §三 为这枚轮子指定的落点面）。**本轮没有异常面**：堆栈只有这一条帧，`Assert.Equal("Failed", …)`（`:364`）在它之前已通过，说明红是字面量比对红的、不是 `NullReferenceException` 之类的异常把整格带红的（波 34 那轮的形状是异常侧，本轮不是）。仪器读数：`total=26 executed=26 passed=25 countersFailed=1 faceA_openTagFailed=1 faceB_attrBackwardFailed=1 selfCheck=OK reconcile=OK notExecutedOpenTags=0 allRedNamesInCriteria=True`；同格 `:366`／`:367`（第二组入参 `nullVersion` 的同名断言）与 `:368` 在同一条语句序列里、首断即抛，故当轮只点名 `:365` 一处（`inherited_source_missing` 在判据只 `:365`＋`:367` 两处、两处同在方法 `:356` 之内 ⇒ 红格数 1，与 §三.0 预论证的枚数推算相符）。 |
| ⑧ 还原命令 | `python w36-mutate.py revert A8`（写回 `51f149d` blob 原字节） |
| ⑨ 还原后 `git status --short` 行数 | `0` |
| ⑩ SHA-256 | 突变态实测 `2ff1fa21ed9e275401cffd7827954c0ea1aac48fd030d447d3b9e88dc2dd36a1` ＝§三.0.1 复算值＝工单 §三.0 值（match）；还原后 `6a1f50999e7caf1d6d29cc07bbe752f89ea17851999013c00b167e590f3498ab` ＝ blob（match）；**本轮判据字节未写入**：判据文件 `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc`（apply／revert 两侧各测一次，两值均等于 blob） |
| ⑪ 当轮预检 | P76＝`VERDICT=BUILD_OK`（22:27:37，归档 `out/preflight-P76.txt`；整段原文见下） |

P76 整段输出原文：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: Godot pid=53448 class=Engine style=0x16CF0000 rect=248,411,956x599 screen=2560x1440 covers=False borderless=False title=???? ? ???? (DEBUG)
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=14% used=2017MB
free_memory_gb=11.3
VERDICT=BUILD_OK
```

形状裁决＝实测恰 1 枚、格名与 §三.0 预论证逐字符相同，且红点落在工单 §三 指定的拒因字面量断言那一侧（`:365`），不是异常侧 ⇒ 本刀按工单要的形状取到了这一枚读数；**"`LegacyInheritance.cs:52` 是否该改"本刀没碰、也不主张**（那枚突变不在本刀额度内），照实把测到的形状交上去，划账归守门人。

### C1（注入·往被扫产品文件塞旧链名样本；红证＋换手同场）——预检 P77＝BUILD_OK

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `src/Pry.Core/Narrative/StateAndLearning/StateLayering.cs` 文末（blob 末行＝`:102` 的 `}`，该行之后追加一行；改的是被扫产品文件，不是判据） |
| ② 改前原文 | 文件末尾无此行（`…return new LayerReadout(draft.OverlayKey, "Applied", "overlay_recorded", sequence);\n    }\n}\n`，末字节 `\n`，5445 字节） |
| ③ 改后原文 | 追加一整行：`// MUTATION C1 (temporary, wave35): ReplyGenerationRenderer`（＋`\n`，5445→5505 字节） |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/StateAndLearning/StateLayering.cs | 1 +`／` 1 file changed, 1 insertion(+)`（存档 `out/c1-apply.txt`；当轮 `status --short`＝1 行 ` M` 该文件） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave36-1028/out/trx-C1" --logger trx` |
| ⑥ TRX 文件名 | `out/trx-C1/24782_PHYTHLINK_2026-10-28_22_27_58.trx` |
| ⑦ 红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.StateAndLearningPrototypeTests.State_and_learning_prototype_sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites`；消息＝`Assert.Empty() Failure: Collection was not empty`＋第二行 `Collection: ["ReplyGenerationRenderer"]`（＝注入的那一枚，旧链名集合其余六枚在产品文件里不存在 ⇒ 集合只 1 项，与交叉谓词实测一致）；红点＝TRX 帧 `StateAndLearningPrototypeTests.cs:line 308`＝`foreach (var file in PrototypeFiles)` 那一行，预论证点名的是循环体内的 `:310` `Assert.Empty(LegacyChainNamesFoundIn(PrototypeSource(file)))`。这一格差是 Release 序列点把循环体断言归到循环头行的已知仪器形状（波 35 A3 那轮 `:195`／`:199` 同形），**语句归属仍＝预论证指明的前半句 `Assert.Empty`**；阳性探针句 `:320-323` 读的是临时样本文件、本轮保持通过（红格只 1 枚即其证）。仪器读数：`total=26 executed=26 passed=25 countersFailed=1 faceA_openTagFailed=1 faceB_attrBackwardFailed=1 selfCheck=OK reconcile=OK notExecutedOpenTags=0 allRedNamesInCriteria=True` |
| ⑧ 还原命令 | `python w36-mutate.py revert C1`（写回 `51f149d` blob 原字节，追加行随之消失） |
| ⑨ 还原后 `git status --short` 行数 | `0` |
| ⑩ SHA-256 | 突变态实测 `2ab20e48fcb6b47a507eb5d20210685d59ce54df187c8fcea8ec12ea2c84bb7c` ＝§三.0.1 复算值＝工单 §三.0 值（match）；还原后 `af727a9c04355d853f756a8cd6d22e374ac889042c847f99906784e0bb20ca3a` ＝ blob（match）；**本轮判据字节未写入（＝本枚能签换手的唯一凭据）**：判据文件 `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc`（apply／revert 两侧各测一次，两值均等于 `51f149d` blob） |
| ⑪ 当轮预检 | P77＝`VERDICT=BUILD_OK`（22:27:54，归档 `out/preflight-P77.txt`；整段原文见下） |

P77 整段输出原文：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: Godot pid=40976 class=Engine style=0x16CF0000 rect=1279,308,1116x799 screen=2560x1440 covers=False borderless=False title=???? ? ??????? (DEBUG)
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=18% used=2028MB
free_memory_gb=11.4
VERDICT=BUILD_OK
```

形状裁决＝实测恰 1 枚、格名与 §三.0 预论证逐字符相同、消息集合内容＝注入的那一枚旧链名 ⇒ 与事前论证一致，本轮有效。红点行号按 TRX 原样登记 `:308`（预论证语句面＝`:310` 的 `Assert.Empty`），差异去处已在上表⑦写明，不改动 §三.0 任何一字。

### C2（注入·往被扫产品文件塞 `const 7` 样本；红证＋换手同场）——预检 P78＝BUILD_OK

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `src/Pry.Core/Narrative/StateAndLearning/StateLayering.cs` 文末（blob 末行＝`:102` 的 `}`，该行之后追加一行；与 C1 同一枚宿主文件，C1 已先还原并核过 SHA 才起本轮，无叠加） |
| ② 改前原文 | 文件末尾无此行（还原态 5445 字节，末字节 `\n`） |
| ③ 改后原文 | 追加一整行：`// MUTATION C2 (temporary, wave35): const 7`（＋`\n`，5445→5489 字节） |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/StateAndLearning/StateLayering.cs | 1 +`／` 1 file changed, 1 insertion(+)`（存档 `out/c2-apply.txt`；当轮 `status --short`＝1 行 ` M` 该文件） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave36-1028/out/trx-C2" --logger trx` |
| ⑥ TRX 文件名 | `out/trx-C2/24782_PHYTHLINK_2026-10-28_22_28_24.trx` |
| ⑦ 红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.StateAndLearningPrototypeTests.State_and_learning_prototype_declares_no_source_side_numeric_default`；消息＝`Assert.Empty() Failure: Collection was not empty`＋第二行 `Collection: ["const"]`（谓词字面量＝`"const "` 含尾空格，命中一枚 ⇒ 集合只 1 项；`static readonly` 未出现，与交叉谓词实测一致）；红点＝TRX 帧 `StateAndLearningPrototypeTests.cs:line 334`＝`foreach (var file in PrototypeFiles)` 那一行，预论证点名的是循环体内 `:336` `Assert.Empty(SourceSideDefaultsFoundIn(PrototypeSource(file)))`（同 C1 的 Release 序列点形状）。阳性探针句 `:344` 读临时样本、保持通过。仪器读数：`total=26 executed=26 passed=25 countersFailed=1 faceA_openTagFailed=1 faceB_attrBackwardFailed=1 selfCheck=OK reconcile=OK notExecutedOpenTags=0 allRedNamesInCriteria=True` |
| ⑧ 还原命令 | `python w36-mutate.py revert C2`（写回 `51f149d` blob 原字节） |
| ⑨ 还原后 `git status --short` 行数 | `0`（存档 `out/c2-revert.txt`；随后 `python w36-mutate.py verify` ＝六枚文件逐字符等于 blob、`verify_mismatches=0 status_lines=0`，存档 `out/final-verify.txt`） |
| ⑩ SHA-256 | 突变态实测 `d9940f120f9791e1bb635599e953125c0d15a849756416a0cae8662fb82baba4` ＝§三.0.1 复算值＝工单 §三.0 值（match）；还原后 `af727a9c04355d853f756a8cd6d22e374ac889042c847f99906784e0bb20ca3a` ＝ blob（match）；**本轮判据字节未写入（＝本枚能签换手的唯一凭据）**：判据文件 `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc`（apply／revert 两侧各测一次，两值均等于 `51f149d` blob） |
| ⑪ 当轮预检 | P78＝`VERDICT=BUILD_OK`（22:28:20，归档 `out/preflight-P78.txt`；整段原文见下） |

P78 整段输出原文：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: Godot pid=53448 class=Engine style=0x16CF0000 rect=248,411,956x599 screen=2560x1440 covers=False borderless=False title=???? ? ???? (DEBUG)
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=14% used=2044MB
free_memory_gb=11.2
VERDICT=BUILD_OK
```

形状裁决＝实测恰 1 枚、格名与 §三.0 预论证逐字符相同、消息集合内容＝注入的那一枚 `const` ⇒ 与事前论证一致，本轮有效。

**七枚定向至此跑完（A4→A5→A6→A7→A8→C1→C2），零轮作废、零枚需要走改名程序。** 全部还原后 `verify`：六枚文件工作副本 SHA-256 逐字符等于 `51f149d` blob（`af727a9c0435`／`000f2fc8bfd5`／`5858a75da3ed`／`289fc89be087`／`6a1f50999e7c`／判据 `317d2171264f`），载体 `status --short`＝**0 行**（`out/final-verify.txt`）。

（下一轮在此行之前追加）

### 三.F 收尾全量轮（P79 ⇒ F，不带 `--no-build`）

前置＝七枚定向全部跑完并逐枚还原，`python w36-mutate.py verify` 六枚 SHA 全等于 `51f149d` blob、载体 `status --short`＝0 行（上段末，跑前跑后各取一次：`out/final-verify.txt`／`out/final-verify-postF.txt`）。

预检 **P79**（22:32:56，`out/preflight-P79.txt`）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: Godot pid=53448 class=Engine style=0x16CF0000 rect=248,411,956x599 screen=2560x1440 covers=False borderless=False title=???? ? ???? (DEBUG)
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=13% used=2108MB
free_memory_gb=11.7
VERDICT=BUILD_OK
```

十一项（本轮无突变，①②③ 记还原态）：

1. 轮＝F（解决方案级全量；七轮还原＋核 SHA 之后跑，自行增量重建——读的是干净源码，不读突变态 IL，无 `--no-build`）
2. 前置＝P79 BUILD_OK；跑前六枚文件 `match=True`、载体 0 行（`out/final-verify.txt`）
3. 命令整行（在载体 `C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave33-1028/carrier` 内）＝`dotnet test Pry.slnx -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave36-1028/out/trx-F" --logger trx`
4. 控制台原文（四工程，`out/f-run.txt`）：Client `已通过! - 失败: 0，通过: 108，已跳过: 0，总计: 108`；App `已通过! - 失败: 0，通过: 233，已跳过: 0，总计: 233`；Api `已通过! - 失败: 0，通过: 280，已跳过: 0，总计: 280`；Core `已通过! - 失败: 0，通过: 783，已跳过: 2，总计: 785`；`dotnet` 退出码＝0
5. TRX 份数＝`ls trx-F/*.trx | wc -l`＝**4**（先数满再读 `<Counters>`；`22_33_24`／`22_33_25`／`22_33_26`／`22_33_29`）
6. 仪器读数（`out/f-count.txt`）：Client 108/108/108/0/0 OK；Core 785/783/783/0/0 OK（`notExecutedOpenTags=2`）；Api 280/280/280/0/0 OK；App 233/233/233/0/0 OK；**SUM＝files=4 total=1406 executed=1404 passed=1404 countersFailedSum=0 faceAFailedSum=0 faceBFailedSum=0 notExecutedOpenTags=2 findings=0**
7. 红格点名＝0 枚
8. 跳过点名（`outcome="NotExecuted"` 按开标签逐格点名，2 条）＝`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`／`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`（＝工单 §二.3 预点名的两条同名跳过格）
9. 出口对账＝收尾读数**仍＝1406／executed 1404／failed 0／跳过 2**，与 §〇.5 的在册起点同值；本刀新格数 0，等式成立；**本刀不顶账，1406 不因这一把移动**，也没为顶账另起任何构建。解决方案级全量预算用 1/1，`dotnet` 总调用 8/8
10. 跑后状态＝六枚文件仍逐字符等于 blob（`out/final-verify-postF.txt`，`verify_mismatches=0`）、载体 `status --short`＝**0 行**
11. `scripts/measure-fixed-eval.ps1` 本刀未跑（工单未要求，波 33／34 先例＝由守门人自决；本刀零新格，不贡献其覆盖面）

## 四 这一把刀跑到哪儿（只登记事实，不含状态词）

- 工单指定的七枚定向轮（A4／A5／A6／A7／A8／C1／C2）＋收尾全量 F **全部跑完**，八枚 `dotnet` 用满、八枚预检全 `BUILD_OK`、零 `HOLD`、零作废轮。
- 七枚定向各自实测＝**恰 1 枚红**，格名与波 35 报告 §三.0 的事前论证**逐字符相同**，红点行号除 C1/C2 落在 `foreach` 头一行（`:308`／`:334`，预论证语句面为循环体内 `:310`／`:336`）外全部落在预论证点名的断言行。
- A8 的红点是 `:365` 那句**拒因字面量断言**、不是异常侧，堆栈只一帧测试方法帧——这条是波 34 §九.3 那条断言级子账的判据面，本刀只交读数。
- C1/C2 两枚：判据字节在 apply 侧与 revert 侧四次测得 `317d2171…`（＝`51f149d` blob），突变对象是 `StateLayering.cs` 的一行注释；两枚扫描格各自第一次拿到"非判据作者之手逼出的红"。
- 未跑＝工单 §三.0 之外的任何方向；**M1–M7 一枚没碰**（押第三十七波）、A1–A3 未重跑（波 35 那把已取红）。

## 五 未验证／欠账（逐条，不并句）

1. **F 的四数只有本刀这一次读数**＝1406／executed 1404／failed 0／跳过 2；本刀不主张它是"新基线"，也不顶账。
2. C1/C2 当轮的**阳性探针句是在同一格里"未红"而非"被单独验过绿"**：本轮红格数＝1 说明探针句没红，但本刀没有把探针句单独跑出读数的额度（要单独验它得改判据或另起一轮，两者都不在工单内）。
3. 注入串的**跨类牵连面本刀没测**：工单 §三 末段的代价登记（注入串也进别的按 `src/**/*.cs` 扫描的门）在波 35 报告 §三 已用读码核实（`ResourceLimitLedgerCoverageTests.cs:165` 的谓词是行首锚定正则，注释行读不出声明形状），但那是**盘上读码事实、非本刀读数**；本刀七轮过滤器只圈 `StateAndLearningPrototypeTests`，那些类当轮根本不执行。形状判定范围仅限本类之内。
4. A8 的红**只点名 `:365`**：同格 `:367`（第二组入参的同一断言）因首断即抛而未执行 ⇒ "`:367` 也会响"这一点本刀没有读数。
5. A4 摘掉合取第三支后，`(suppressed: False, deduplicated: False, rejected: True)` 之外两枚实例仍绿——本刀只登记当轮"恰 1 枚＋格名带 `rejected: True`"，**没有**验证另两枚实例的独立形状（那要另起突变，不在额度内）。
6. 载体线尾之外（`fd93ebe..51f149d` 两枚提交）的产品／判据字节＝波 33 的形状，本刀未复核其内容。
7. 本刀**零产品字节净变化**（七轮全部还原）、**零落地**（无 `git add`／`commit`／`push`）、**零顶账**——主树线尾仍是入场所见的 `fa6d24f`。
8. C1/C2 两枚的"换手"凭据只有判据 SHA 与 last-touch 两条（§〇.4 末段）；判据字节的主树 last-touch＝`e04e7e6`、载体＝`51f149d`，**都不是本刀这只手**，但这一条属守门人划账口径，本刀不作结论。

## 六 盘上凭据与预算自证

- `.trx` 枚数＝**11**：定向 7 轮×1（`trx-A4`…`trx-C2` 各 1 枚）＋收尾全量 1 轮×4（`trx-F`）＝11 ✓（`find out -name "*.trx" | wc -l`＝11，逐目录计数见下）：`trx-A4`1／`trx-A5`1／`trx-A6`1／`trx-A7`1／`trx-A8`1／`trx-C1`1／`trx-C2`1／`trx-F`4。与工单满额形状（定向 7×1＋全量 4＝11）**枚数相符，无差额**。
- 作废轮＝**0 枚** ⇒ 盘上无 `trx-<轮>-voided-<预检号>` 目录（`ls -d out/trx-*voided*`＝0）；无任何清空／删除／重名覆盖。
- 焦点预检＝**8 枚**（P72–P79），`grep "^VERDICT"` 分布＝`BUILD_OK`×8、`HOLD`×0；HOLD 重跑额度 0/16 未动；每一次 `dotnet`（含 F）之前各当场一枚，零跳过。
- `dotnet` 实际调用＝**8**（定向 7＋全量 1）≤8 ✓；解决方案级全量＝1 ≤1 ✓；定向＝7 ≤7 ✓；解决方案级构建（B1 型）＝**0/1**（§〇.7 登记的三段跳过理由成立：主树侧 0 行式子＋载体 `Pry.Core.dll` 在盘＋六枚 SHA 未变）。
- 八轮（含 F）全**不带 `--no-build`**。
- 判据字节＝七轮 apply 侧与 revert 侧共 14 个读数全部等于 `51f149d` blob `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc`（逐枚见各轮⑩项，存档 `out/a*-apply.txt`／`out/*-revert.txt`）；三计数出场复测＝`[Fact]`23／`[Theory]`1／`[InlineData]`3（载体侧 `grep -c`，与 §二 入场值逐位相同）。
- **载体终态四行**（跑完 F 之后当场取，`git -C .local-notes/qa-wave33-1028/carrier`）：
  ```
  rev-parse HEAD                          = 51f149d0b722e28a6566c93d19f48a94ca4c85df
  status --short                          = 0 行
  log --oneline fd93ebe..HEAD             = 2 行（1e02205 落六枚新文件 + 51f149d 一处判据整形）
  diff --stat fd93ebe HEAD -- src tests scripts Pry.slnx Directory.Build.props
                                          = 6 files changed, 822 insertions(+), 0 deletions
  ```
  与 §〇.3 入场四行逐字符相同 ⇒ 本刀在载体里零提交、零净字节改动。
- **主树终态**：`git status --short --branch` 非未跟踪行＝入场那三条（`M  docs/dispatch-tip1020-02-memory-group-judgment.md`、` M docs/plans/PROJECT_PLAN.md`、`M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）逐字符不变；`??` 由入场 5 条变 6 条，唯一新增＝本刀报告 `docs/eval/reports/2026-10-28-hds009-wave36-red-handover-remainder.md`（**未 `add`、未提交**）；`git rev-parse HEAD`＝`fa6d24f85393abd7cc7d940537547951729a909e`（与入场复取值相同，本刀零提交）。
- 本刀写入的文件面＝唯一那枚报告（§一 清单）；其余全部产物落在 `.local-notes/wave36-1028/`（仪器两枚 `w36-count.py`／`w36-mutate.py`＋`out/` 下 8 份预检、7×4 轮次档案、11 份 `.trx`、自证与 verify 档案），属工单 §一.6/§二 要求的凭据目录，不属仓库文件面。
- 禁用的 git 写操作（`commit`／`add`／`checkout`／`reset`／`stash`／`clean`／`rm`／`tag`／`push`）本刀**一次都没调用**；还原一律走 `git show 51f149d:<path>` 读字节再写文件。

## 七 交给守门人的六件事（凭据位置）

1. **七枚定向轮的形状**＝各轮 §三.A／§三.C 十一项表格（`docs/eval/reports/2026-10-28-hds009-wave36-red-handover-remainder.md` §三，A4／A5／A6／A7／A8／C1／C2 七节），逐轮附仪器整行读数、TRX 绝对路径、apply／revert 两侧判据 SHA、当轮预检整段原文。原始凭据＝`.local-notes/wave36-1028/out/`（`*-apply.txt`／`*-run.txt`／`*-count.txt`／`*-revert.txt`／`trx-<轮>/*.trx`／`preflight-P72..P79.txt`）。
2. **两枚扫描格的红证＋换手形状**＝C1/C2 两节：判据字节四轮读数全等 blob（`317d2171…`）、突变对象是 `StateLayering.cs` 的一行注释、红格消息含注入的那一枚（`["ReplyGenerationRenderer"]`／`["const"]`）、探针句未红（红格数 1）。本刀只交形状，不写它该记成什么账。
3. **波 34 §九.3 那条断言级子账**＝A8 节：红点 `:365`＝`Assert.Equal("inherited_source_missing", nullSource.Reason)`，消息为字符串比对不符（pos 24），堆栈无异常帧；`LegacyInheritance.cs:52` 本刀未改。凭据＝`out/a8-run.txt`＋`out/trx-A8/24782_PHYTHLINK_2026-10-28_22_27_41.trx`＋`out/a8-count.txt`。
4. **收尾全量读数**＝§三.F（1406／executed 1404／failed 0／跳过 2＋两条同名跳过格名逐字点名）；四份 TRX 在 `out/trx-F/`，仪器求和行在 `out/f-count.txt`，控制台原文在 `out/f-run.txt`。本刀未顶账。
5. **仪器与其自证**＝`w36-count.py`／`w36-mutate.py`（本刀自写）；七面已知读数自证在 §〇.6，原文存档 `out/instrument-selfproof.txt`（`FACES=7 AGREED=7 DISAGREED=0`，退出码 0）；自证过程中修过仪器两处（`collect()` 的字符串路径展开、F4 面全限定前缀剥离），判据／TRX 字节未动，过程登记在 §〇.6。
6. **本刀没做的事**＝M1–M7 一枚未碰（改押第三十七波，配方仍在波 33 报告 §三.0）、A1–A3 未重跑、`scripts/measure-fixed-eval.ps1` 未跑、零新增格／零 `Assert.Skip`／零注释断言／零新增数值门槛／零合并轮、HOLD 额度未动用。

## 九 守门人划账（追加节；§〇–§七 一字未改，本节零 `dotnet`、不产生新读数）

### 九.0 我这只手用的仪器与它的自证

- 仪器＝`.local-notes/wave36-1028/gk36-audit.js`（我自写，不复用 lane 的 `w36-count.py`，两把尺各走一遍），三面对账＝同份 `<Counters>`／`<UnitTestResult … outcome="Failed">` **开标签**／红格名剥掉全限定前缀与参数表后回判据源码验"方法名＋`(`"真实存在"；整段输出存档 `out/gk36-audit.txt`。
- 起跑前拿盘上九面**别人产出的**已知读数自证＝波 33 `trx-M1`／`trx-M5`／`trx-M7` 各恰 1、波 34 `trx-A1`＝恰 2、`trx-A6`＝恰 1、波 35 `trx-A1|A2|A3` 各恰 1、波 33 `trx-F` 四份求和 1406／1404／1404／0＋两条 `NotExecuted` 同名 ⇒ 打印 `ALL FACES AGREE`。
- **一处我自己的仪器错（第三次踩同一族）**：首版把"红名逐字符等于事前论证"写成 `names[0] === want[d]`，而 TRX 的 `testName` 是全限定名 ⇒ 七轮**全部误报** `EXACT_NAME_MATCH=false`。就地改成 `endsWith` 后七枚全 `true`，重跑前没有据此报过任何红账。可复用形式照旧＝**仪器说"没对上"先怀疑仪器再怀疑读数**，坏的是读数通道，判据与 TRX 字节一个没动。

### 九.1 §八 预登记那七件事的判定

1. **七轮定向形状**＝每轮 `total=26／executed=26／passed=25／countersFailed=1／开标签红=1／reconcile=OK／nameInCriteria=true／EXACT_NAME_MATCH=true`，红格全名逐字符等于波 35 报告 §三.0 的事前论证（**先档后跑**：论证表没被实测值改写，工单 §三.0 那七行与我入场读到的字节一致）。红点行号依次 `:195`／`:273`／`:286`／`:399`／`:365`／`:308`／`:334`。其中 A4 落在多行调用起始行 `:195`（断言文本在 `:199`）、C1/C2 落在 `foreach` 头一行（语句面 `:310`／`:336`）＝波 35 §九.3 那条 Release 序列点已知形状的**第二次与第三次实例**；归属由 testName＋消息定，行号只作辅证，两处都不构成疑点。
2. **载体终态四行**当场重取＝`rev-parse HEAD`＝`51f149d…`、`status --short`＝0 行、`log --oneline fd93ebe..HEAD`＝2 行、`diff --stat fd93ebe HEAD -- src tests scripts Pry.slnx Directory.Build.props`＝6 files changed, 822 insertions(+), 0 deletions，与 §〇.3 入场值逐字符相同。六枚 SHA 我另在载体工作副本 `sha256sum` 取了一遍（`5858a75d…`／`289fc89b…`／`6a1f5099…`／`000f2fc8…`／`af727a9c…`＋判据 `317d2171…`），与七轮 revert 侧的 `restored_sha256` 一一对上 ⇒ 本刀在载体里零提交、零净字节。
3. **收尾全量**＝F 四份逐份 `<Counters>` 与开标签同时求和＝108/108/0、785/783/0、280/280/0、233/233/0 ⇒ total 1406／executed 1404／failed 0／两份 `NotExecuted` 点名与波 28–35 同名同枚，仪器判定 `SAME_AS_BASELINE_1406=true`。**这是 1406 的第二次同值复采**（第一次＝波 34），本刀不顶账、不为顶账另起构建。
4. **作废轮与 `.trx` 枚数对账**＝盘上 11 枚＝定向 7×1＋全量 1×4（逐目录 `trx-A4`…`trx-C2` 各 1、`trx-F` 4）；`trx-*voided*`＝0 枚；八轮（含 F）全不带 `--no-build`。
5. **签账资格（本刀的胜负手）**＝七轮 apply 侧与 revert 侧共 **14 枚**判据文件 SHA-256 读数全部等于 `51f149d` 的 blob `317d2171…`；判据字节 last-touch 主树侧＝`e04e7e6`、载体侧＝`51f149d`，**两个都不是这一把那只手**；三计数 23/1/3 ⇒ 展开 26 节点、**净新增格 0**、新增数字门槛 0、注释或放宽既有断言 0、`Assert.Skip` 0。按同一把尺（波 24／27／28／30／34／35）**这七轮全有签账资格**——波 35 §七.5 那句"其余 7 轮没有 SHA 读数可交"的诚实账，这一把被补齐了。
6. **押账未被顺手碰**＝M1–M7 一枚没跑（盘上无 `trx-M*`），A1–A3 未重跑，`scripts/measure-fixed-eval.ps1` 未跑。
7. **预算与 HOLD 额度的实际形状**＝`dotnet` 实际 **8** 次＝定向 7＋全量 1 ≤8 ✓；解决方案级构建 **0/1**（跳过判定的三段理由我逐一核过：主树侧那条 0 行式子成立、载体 `bin/Release/net10.0/Pry.Core.dll` 在盘、六枚 SHA 自入场未变）；预检 **8** 枚（P72–P79）`grep "^VERDICT"` 分布＝`BUILD_OK`×8、`HOLD`×0，每一次 `dotnet` 之前各当场一枚、零跳过。**HOLD 重跑额度 0/16 未动用 ⇒ 波 35 §九.3 那条"上限从 8 提到 16"的仪器调参，这一把没有被消费一次，因此没有被用成放宽**；它的实际收益是"窗口一开就把七轮连着跑完"（本刀正是八分钟内跑满），反面也成立：**下一次如果把额度用满仍榨不到轮次，那要记为窗口形状而不是额度不足**。

### 九.2 划账（波 33 那 26 枚的终账）

- **本刀结清 6 枚**，两本账分开说，不许并成"6 枚新红"：
  - **红证＋换手双结 4 枚**＝`A_delivered_body_that_was_suppressed_deduplicated_or_rejected_is_not_attribution_evidence(suppressed: False, deduplicated: False, rejected: True)`（A4）、`Experience_within_the_input_validity_window_is_offered_as_optional_material`（A5）、`Experience_past_the_input_validity_window_is_not_offered`（A6）、`A_successful_inheritance_carries_its_source_reference_and_version`（A7）。这 4 枚正是波 35 §九.2 那本"完全没有红"的账，现两样同时到手。
  - **只补换手 2 枚**＝`State_and_learning_prototype_sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites`（C1）、`State_and_learning_prototype_declares_no_source_side_numeric_default`（C2）。它们的红证早在波 34 B 类就已结（当轮动的是判据自身字节＝反面探针类，只结门在咬不签换手）；本刀走注入路＝**判据字节零写入＋样本串进被扫产品文件**，换手第一次到手，红证计数不重复领。
- **A8 那一格不加格子账**＝`Inherited_state_without_a_pointable_source_fails_loudly_and_is_never_silently_dropped` 本就在波 34 那 10 枚双结里；它结的是**断言级子账**（见 §九.3.3）。
- **现账（26 枚按格名去重）＝19 枚红证＋换手双结／7 枚红证在手仍欠另一双手／0 枚完全没有红**。19＋7＋0＝26 ✓（19＝波 34 的 10＋波 35 的 3＋本刀的 6；7＝波 33 的 M1–M7）。**完整换手欠账 13 → 7；红证欠账 4 → 0。**
- 出账前按格名对过交集：本刀 6 枚与波 34 A 类那 10 枚、波 35 那 3 枚、波 33 M 系那 7 枚两两不重复；**别把 19 读成"19 只手"**，同手的定义是"取红那只手不是判据作者"，红证的定义是"在改坏产品码之下响过"，两把尺各量一件事。
- **旧账与别的账一律不因这一把变动**：波 13 余 47＋波 20 余 28＝旧欠 **75 格**照旧不减（与本刀无交集）；拆法原文照旧 **46／45／1＝W2**；在册基线照旧 **1406／executed 1404／failed 0／跳过 2**（同值复采不等于顶账，顶账要由守门人在归位刀当场实测后写）。

### 九.3 三处形状判定（两处比 lane 的登记硬一点，一处替它收窄）

1. **C1/C2 的双结我判得比 §五.2 的措辞更硬**：lane 只交"红格数 1＋探针句未红"，我另从两份 TRX 的消息面把"门咬在哪"钉住＝C1 的集合字面量是 `Collection: ["ReplyGenerationRenderer"]`、C2 是 `Collection: ["const"]`，逐字符等于当轮注入 `StateLayering.cs` 文末的那一枚 token ⇒ 红确实从**被扫的产品面**来（配合 14 枚判据 SHA 未动＝不是判据自身字节），不是我担心的"注入了没用的串、门靠别的东西响"。
2. **"探针句在突变轮只是未红"这条欠账，我用别的轮次把它收窄但不销**：同一枚阳性探针在 A4/A5/A6/A7/A8 五轮里随类通过（每轮 `passed=25`，红的都是别的格）、在 F 轮里全绿 ⇒ "扫描函数看得见被扫文件、且不因合法内容误报"这两半各有六轮执行证据。**仍欠的是"把探针句单独跑出一次读数"这一格**——那需要新增判据或另起轮次，两者都不在证据刀的范围里，照旧押着，不许因为"六轮都绿"就写成"探针已单独验过"。
3. **A8 结清的措辞＝"拒因字面量断言第一次单独响过"，不是"`:365`／`:367` 两处都响过"**：消息形状＝`Expected: "inherited_source_missing"`／`Actual: "inherited_source_missing_wave35_alias"`（pos 24），堆栈只有测试方法一帧、无异常帧 ⇒ 红的确实是 `:365` 那句断言而非产品侧 `NullReferenceException` 那一侧；同格 `:367`（第二组入参的同一断言）因首断即抛**未执行**，lane §五.4 那句登记我照单收下。波 34 §九.3 那条子账自此结清**一半**（`:365` 已响），另一半（`:367`）与新形状一起押第三十七波。

### 九.4 归位判定

零落地。主树侧 `git diff --stat 51f149d HEAD -- src tests scripts Pry.slnx Directory.Build.props`＝**0 行**（我当场重取），载体终态四行不变 ⇒ 本刀**没有代码刀可归位、不做按路径落地、`main` 不因这一把挪**（只有收口文档刀自己挪）。文件面＝lane 那一份报告（写时未跟踪，由收口文档刀落地）＋第三十七波工单一枚；他人两枚 `M ` 暂存项与五枚 `??` 未跟踪资产一字未动；`PROJECT_PLAN.md` 用 hunk 手术把他人那 18 行排除在本把提交外、提交后原样还回并复测仍 18/0。

### 九.5 押下一把（第三十七波＝`docs/dispatch-hds-wave37-1028.md`）

七轮定向（M1–M7，**取红的手必须是第二双手**）＋一轮收尾全量 F，跑序固定 M1→…→M7→逐字节还原核 SHA→F；配方＝波 33 报告 §三.0 那七行（M5 只翻 `DeliveryAttribution.cs:88` 的 `UserConsentProven`，**不许连 `:89` 的 `TaskCompletedProven` 一起翻**），事前论证表由我从**当前干净树**逐枚复算"被改那一行有几个消费点"后落进工单，先档后跑、跑前不许把预期改成实测值。另押三条小的＝`:367` 的独立读数、阳性探针的单独读数、两套并行命名债与 HDS-007/HDS-009 生产接线（**属接线刀不属证据刀**）。**照旧不许写的四句话**＝新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链；未验证面（波 33 报告 §五 那十条）**照旧一条不减**，本刀新登记的收窄只发生在 §九.3 那两处措辞上。

**一处出闸后的自我更正（本节原文不涂改，更正追加在此）**：上面那句"七轮定向（M1–M7）"在我把工单写下去时扩成了**十轮**＝M1–M7 之外另加 M2b／M5b／M7b 三枚**断言面配方**，出处与理由见 `docs/dispatch-hds-wave37-1028.md` §三.2（波 33 的 M2／M7 那两枚红在异常侧＝产品码先抛，M5 只证了 `:244` 而 `:245` 从没单独响过；整格因异常而红不等于承重那句断言响过）。代价照实登记＝定向 7→10、`dotnet` 上限 8→11、构建与全量额度不变、HOLD 语义与"十轮不许写判据字节"一字未动；这三枚是**派发者自己新加的方向**，不记为 lane 扩范围，也不引用为本刀已拿到的读数。
