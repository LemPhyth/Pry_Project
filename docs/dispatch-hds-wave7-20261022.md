# HDS 第七波派单（2026-10-22 深夜）｜第七波只做三件：一条结构上红不了的断言改判、E1 那两向真改坏、上一波没落盘的拆法补写

派单人：守门人（Architecture / Integration / Release）。执行方式＝守门人自起冷启动子 Agent，在隔离载体里跑，工单直接给执行方消费，不交作者转派。
坐标系＝`docs/plans/PROJECT_PLAN.md` §4.2。本波**零产品代码、零门槛数值改动**；判据条数**只允许持平或按改判说明变化**，不许顺手加用例凑数。

---

## ① 上一波的教训写死成本单的纪律（违反其中任一条＝这一波按未到付处理）

1. **报告是交付物，读数不是。** 载体选定之后、任何突变之前，先把报告文件建出来并写满第〇节（载体号、入场 `git status` 行数、起点读数）。
2. **每一个批次跑完立刻把该批次的表格追加进报告文件并落盘**，再开下一批。不许"全部跑完再一次性写"。
3. **感觉轮次或预算快到时，先收尾报告再停手**：把"做到哪、哪些没做、盘上证据在哪"写完，再回报。宁可交一份带缺口的报告，也不许交"活干了、痕迹没了"。
4. 每一条反证必须当场落：载体提交号、被改文件:行、before→after 原文、`git diff --stat` 数字、跑法整行、TRX 文件名、红格用例全名＋消息要点、还原方式与还原后 `git status --short` 行数。**缺任何一项就在报告里明写缺，不许用"已拆过"概括。**

## ② 载体与预算

- 载体复用 `.local-notes/qa-tip-1022`（detached `6b6f549`，`git status --short` 入场 0 行）。**开工前自己数一次 `git status`，不为零就换目录重开，不许在脏树上跑。**
- 预算＝**解决方案级构建 ≤1 次、全量 ≤1 次、定向跑 ≤3 次**。每一次 `dotnet`（含 `--no-build`）之前先跑 `.local-notes/game-preflight4-1022.ps1`，把整段输出（含 `VERDICT=`）**当场追加进报告文件**（这一条正是上一波查不出来的那格）；`VERDICT=HOLD` ⇒ 就地停刀回报，不等、不绕、不改预检判据。
- 本波若动到 `tests/Pry.Core.Tests`，收尾那一轮的"构建＋全量"就是唯一一次；三件里能靠 `--no-build` 的定向跑一律用 `--no-build`。

## ③ 三件活的原文事实（守门人 10-22 深夜零构建读码采到，逐行核过）

### 件 1｜一条**结构上红不了**的断言（新缺陷 `DEF-GREENGATE-01`，判据刀、零产品代码）

- 位置＝`tests/Pry.Core.Tests/NarrativeStorageContractTests.cs:313`，属 `Late_commit_from_a_lost_ownership_epoch_is_rejected_without_overwriting_anything()`（方法从 `:281` 起）。
- 原文＝`Assert.DoesNotContain(_writer.Ledger.Snapshot(), s => s.CommitId is "commit-late" or "commit-late-2");`
- **为什么它红不了**（三段读码，全部当场可复核）：
  1. `DeliveryLedger.Snapshot()` 返回的是**投递段**集合 `_segments.Values`（`src/Pry.Core/Narrative/DeliveryLedger.cs:45`），不是已登记提交集合 `_commits`（同文件 `:36`、`:61`）。
  2. `NarrativeCommitWriter.TryCommit(...)` 全程只写 `_commits`（`:196` 调 `RegisterAppliedCommit`），**从不追加投递段**；投递段只由 `DeliveryLedger.TryAppend(...)` 产生。
  3. 该方法 `:281`～`:313` 之间**一次 `TryAppend` 都没有**（同文件其它方法有：`:161`、`:178`、`:212`、`:229`、`:245`、`:260`、`:273`、`:443`）。⇒ `Snapshot()` 到 `:313` 时是**空集合**，对空集合判"不含某元素"恒真。
- **这件事真正由谁守着**（不许因为 `:313` 是空门就写成"迟到提交没人守"）：同一测试的 `:300`（`Assert.False(late.Accepted)`）、`:301` 与 `:306-307`（拒绝原因＝`OwnershipHeldElsewhere`／`OwnershipFenced`）、`:310-312`（`LastCommitId`／`StoryVersion`／`ProcessedThrough.Seq` 没被覆盖）；账本侧另有 `:273` 与 `:441-443` 两枚"未登记提交的幽灵投递段被拒"用例。⇒ **覆盖面在，缺的是这一行的证据价值。**
- **要做的改判**（二选一，选完在报告里写明理由，不许两个都做）：
  - A：把 `:313` 换成对**可观测事实**的断言——即证明迟到提交没进 `_commits`。若 `DeliveryLedger` 没有公开的可登记提交读取面，就用现有公开面构造判据（例如对迟到 `CommitId` 走一次 `TryAppend` 并断言被拒且拒绝原因指向"提交未登记"）；**不许为此给产品加公开接口**（那是产品代码，超出本单）。
  - B：删掉 `:313`，并在同一刀里把"迟到提交不入账"这一格**显式挂到 `:273`／`:441-443` 名下**（写进测试上方注释与本单报告），同时更正契约文档里引用那一行作为守卫的句子。
- **必须先自证红一次**：改完之后，用**产品侧**的一次临时突变（`src/Pry.Core/Narrative/DeliveryLedger.cs:61` 的 `_commits[commit.CommitId] = ...` 那一句在载体内临时改成不写、或把 `:79`／`:230` 的 `_commits.TryGetValue` 前置检查放开）证明新断言真的会响。**如果这一向在今天的代码里够不着任何在册用例，就按第六波那条老规矩把"红不了"当场采成证据并登记，不许为了留一抹红去追一个不可能存在的红。**

### 件 2｜E1 那两向"只有测试内嵌样张、没有真改坏真相源"（`e554483` 的欠账，零产品代码）

- 判据＝`tests/Pry.Api.Tests/UpstreamProvenanceAndDependencyShapeTests.cs`。守门人已读码确认两向都**可达**，且都不需要还原图变化：
  - `No_package_reference_floats`（`:187`）经 `AllCsprojFiles()`（`:492-494`，`EnumerateFiles(RepositoryRoot(), "*.csproj", AllDirectories)`）读**真实 csproj 文本**。
  - `Only_the_authorised_upstreams_are_cited_and_the_two_lists_agree`（`:147`）在 `:174` 直接 `File.ReadAllText` 读 `docs/upstream-license-compatibility.md` 的白名单表。
- **跑法上的硬要求**（这是上一波没写清、差点踩坏依赖解析的那格）：csproj 突变之后**只允许**用
  `dotnet test tests/Pry.Api.Tests -c Release --no-build --no-restore --results-directory "<本轮专属绝对路径>" --logger trx`
  跑——突变必须只存在于测试**读取时**，绝不进入构建／还原图。还原之后同样用 `--no-build --no-restore` 复绿。**任何会触发还原的跑法（不带 `--no-restore`）在带突变的 csproj 上一律禁止**，宁可登记够不着。
- 两向各一枚突变，各配一条预期红格名；逐字节还原并数 `git status --short` 归零。`licenses/**`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md` 仍然一字不许碰。

### 件 3｜把第六波那 46 格红的"拆法原文"补出来（纯文档，零 `dotnet`）

- 第六波那条 lane 撞 150 轮上限、报告从未写出，盘上只有红集合。本件＝**允许你在同一载体上重做其中最小的三格并留下完整原文**：`faf8ecf` 的预算负数一格、`374ecd8` 的双账拒绝一格、`2eba867` 的转义一格。
- 每件都要满足 §① 第 4 条的十一项字段。**只做这三格**，其余 43 格继续挂账，不许"顺手多拆几格"导致再次撞上限。
- 若 §② 预算不够：停在本件，把已完成格数与缺口写进报告。

## ④ 落点与禁止事项

- 报告唯一落点＝新文件 `docs/eval/reports/2026-10-22-qa-wave7-three-faces-with-incremental-report.md`（id 建议 `eval-QA-WAVE7-1022`）。写完即停，不许 `git add`、不许提交。
- 零提交；不许改限额数值、`.5` 检索地板、`2400`、五档超时、`MinimumUsableTokensPerSecond = 12`、`4096` 地板；不许放宽或删除任何既有断言（件 1 的改判属"把空断言换成可失败的断言"，动手前必须在本单 §③ 件 1 的两个选项里选一个并写明理由，不许直接删了了事）。
- 不碰作者的任何进程；不把 `%LOCALAPPDATA%/PryCompanion` 当测试载体；不 tag／不 push／不打包。
- 主工作树他人名下两枚暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）任何情况下不读改、不提交、不 `stash`、不 `clean`。
- **状态词与结案词不由执行方写**；在册基线仍＝总 1146／passed 1144／0 红／2 条同名环境跳过，本波若判据条数有变化必须在报告里写出等式与原因。

## ⑧ 执行结果（守门人登记，2026-10-22 深夜）

执行方＝冷启动子 Agent（`eval-QA-WAVE7-1022`，报告 454 行／十章，§〇 在动任何突变之前写满）；本节只写盘上核过的事实，改写权在守门人的登记刀。

- **§① 四条硬纪律全部兑现**：没有撞 150 轮上限，报告完整落盘；逐次焦点预检七次（PF-1～PF-7）整段原文在 §二／§四／§五／§六／§七，七次全 `VERDICT=BUILD_OK`；四批证据各用新目录名（`run1-api-red`／`run2-core-red`／`run3-client-red`／`run4-full`），守门人当场逐份读回 `<Counters>` 复证过，与报告自述逐字相同。预算终账＝解决方案级构建 1／全量 1／定向 3（用满）＋单工程构建 3（工单未设上限，lane 自列一册如实记）。
- **件 1**：守门人 §③ 的三段读码事实复核成立（唯一偏差＝工单写 `DeliveryLedger.cs:45`、载体实测 `:46`，不影响结论）；改判选 **A** 并落进载体工作树（`:313` 恒真的 `DoesNotContain` 换成"拿当前代次 host-b 的票对两个迟到 CommitId 各走一次 `TryAppend`、断言被拒且原因 `SegmentCommitUnknown`"＋`Assert.Empty(Snapshot())`，`+19/−1`）。**但写这条判据的人自己拿的红按仓库纪律仍算自证**，所以守门人在同一载体另拿一次：把 `src/Pry.Core/Narrative/DeliveryLedger.cs:79` 的未登记提交门临时换成常量键 ⇒ 立即红在 `NarrativeStorageContractTests.cs:327`（`Assert.False() Failure／Expected: False／Actual: True`），TRX＝`.local-notes/qa-wave7-1022/run5-gk-red`（527＝executed 520／failed 1／2 条同名跳过），跑后 `git checkout --` 还原、SHA-256 `020c485c…4d1b09a7` 与 `git show HEAD:` 逐字节一致、载体 `git status --short` 回到 1 行。**换人复判成立**：这一格既可响也不误报（未突变码上绿）。lane 另否证了工单给的第二个形状（`:61` 不写）：`_commits` 全空时新断言照样绿，证明不了这一向，故按读码改选 `:79`。
- **件 2**：两向都拿到"真改坏真相源"的红且**全程没让突变进还原／构建图**（跑法 `--no-build --no-restore`，M2 选的是不在 Api 求值图里的 `src/Pry.App/Pry.App.csproj:11`）——`No_package_reference_floats`（`:193`）与 `Only_the_authorised_upstreams_are_cited_and_the_two_lists_agree`（`:153`，`Expected 3／Actual 2`，先响的是"名单本身也要判"），另多响自探针格 `The_scanners_see_the_shapes_they_claim_to_ban`（`:315`，同因）；两枚逐字节还原（`9d94a791…`／`2e08e0ec…` 与 HEAD 一致），`licenses/**`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md` 一字未碰。**登记的偏差**：§③ 要求的"还原后同一条命令行再跑一次复绿"没做，因为定向 3 次硬顶已被三组突变用满；替代证据＝收尾全量里的 Api TRX（278／278／0 红，跑它时 csproj 已还原且带突变的 csproj 从未进过任何 restore／build）。守门人认这条替代证据成立，缺口按偏差留档，**不追认成字面合规**。
- **件 3**：点名的三格十一项字段齐全——M4 `ContextBuilder.cs:24` 夹零 ⇒ `ContextBudgetTests…negative_input_budget`（锚 `:54`，Expected −128／Actual 0）；M5 `SqliteMemoryRepository.cs:100` 上限抬到 3 ⇒ `MemoryDualSchemaLedgerTests…coexist…`（锚 `:48`，`No exception was thrown`，另同因多响 2 格按 §① 第 4 条另记）；M6 `PryBackendClient.cs:188` 摘掉 `Escape` ⇒ 转义那一格（锚 `:39`，`a%26b` vs `a&b`，红格名与第六波预告逐字相同）。**其余 43 格继续挂账**，lane 没"顺手多拆"。
- **出口读数与归位**：lane 收尾那一轮＝总 1146／passed 1144／0 红／2 条同名跳过（Client 108／Api 278／App 233／Core 527-executed 525-passed 525），与在册基线逐字相同，判据条数变化＝0（件 1 属同一方法内替换断言）。件 1 的判据由守门人取进主工作树＝`4039a50`（一次解决方案级构建 0 警告 0 错误＋一次全量）。**主树那一轮的读数是总 1149，比在册多 3 条**：当场核过＝全部在 `Pry.App.Tests`（236 而非 233），来源是别人名下那枚未提交的 `tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`（工作副本 `[Fact]`／`[Theory]` 计数 8 对 HEAD 的 5，多出的三个方法名＝`The_grouping_predicate_is_never_hollowed_out`、`The_grouped_orphans_actually_flow_into_the_second_list`、`The_grouping_gate_really_sees_the_m17_hollowing`），守门人未读其内容、未改、未提交；Core／Api／Client 三栏与在册逐位相同、全树 0 红。⇒ **在册基线照旧 1146／0／2，本波不改写。**
- **还欠的**：43 格拆法原文；`QA-TIP1020-02`（仍卡在他人暂存项）；`QA-TIP1019-06` teardown 总清（作者令"早点做完之后再回来大修"）；F2①"降级继续写"那一向按读码够不着；`ModelProcessRegistry.cs:63` 第二道门需要一个新缝。
