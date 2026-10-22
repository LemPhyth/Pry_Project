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
