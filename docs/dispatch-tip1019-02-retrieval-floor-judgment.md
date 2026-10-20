# 工单 `QA-TIP1019-02`（第 2 层 · 检索地板 `.5` 的行为判据）

- **状态词**：已派工·待认领（守门人 2026-10-20 按作者授权落成白纸单；Owner＝Memory & Context，与 `RT-005-MANAGE` 同一条 lane；判定归 QA 从可指认提交复采，结案词归作者）
- **载体基线**：开工当日先 `git rev-parse` 与 `git status` 重取线尾。在册读数＝**988／0／2（总 990）**，分项＝Client 101／App 233／Api 248／Core executed 406 passed 406＋2 条环境跳过（`eval-QA-TIP1020-1020`，QA 从 `e82499b` 建干净 `--detach` 副本七次复采、七次分项逐位相同）。本单不改写它，也不把自跑数顶给它。
- **授权来源（逐字）**：作者 2026-10-19 原话「**我不想思考了，你就继续给我派工单，该提交的提交吧，早点做完之后再回来大修**」⇒ 守门人据此把这条 10-19 只登记在验收面的欠账落成可交接单。**本单不需要作者再拍任何东西**，全是执行活。
- **刀的体量**：**判据刀，零产品代码**。预计 1 个文件内 1 条用例（可加 fixture 装配），不动阈值、不动接口、不动界面。

## 1. 事实表（守门人 2026-10-20 当场重读码，行号当日有效）

| # | 事实 | 位置 |
|---|---|---|
| F1 | 检索地板与状态闸写在同一行：`.Where(x => x.Status == "active" && x.Confidence >= .5 && x.ScopeKey == query.Scope.Key)` | `src/Pry.Core/Memory/MemoryQueryService.cs:22` |
| F2 | **状态那半有抓手、置信度那半没有**：10-19 实测把 `>= .5` 改成 `>= .4` ⇒ 四个程序集**一条都不红**（当时 906 条，逐项与干净跑逐位相同）；改坏同一行的 `Status == "active"` 那半立刻红 `MemoryContextTests.Correction_supersedes_old_fact_and_out_of_order_jobs_cannot_restore_it` | `tests/Pry.Core.Tests/MemoryContextTests.cs:102` |
| F3 | 全仓没有第二条碰到这个地板的用例：受跟踪 `tests/**` 里 `.45` 只命中 `CharacterIntelligenceTests.cs:289`，那一条断的是**口癖**学到的置信度，与检索地板无关 | `tests/Pry.Core.Tests/CharacterIntelligenceTests.cs:289` |
| F4 | 同一行的评分公式（`.15 * Importance + .10 * conversation + .05 * relationship + .05 * Confidence`）是**第 6 层标定轮**的面对象，本单不许碰 | `src/Pry.Core/Memory/MemoryQueryService.cs:56` |
| F5 | 后果链条已证过一半：`orphaned`（依据已被删掉）的记忆 `confidence` 可以被留在 `.5` 以下，墓碑路径与 `forget_before` 由 `SqliteMemoryRepository` 守着；地板一旦下调，这些行重新进上下文而状态闸仍放行 | `docs/dispatch-rt005-memory-management.md` §8；`e2b521a` 那把的 Core 判据 |

## 2. 完成标准（出判面，逐条可判）

1. **一条对照用例**（挂在 F2 那条既有判据旁边同一族）：同一角色、同一作用域下备两行 `Status="active"` 的记忆，一行 `confidence` 落在地板下（`.45` 或 `.4`，写字面量不写魔法变量），一行落在 `.5`；断言**只有 `.5` 那行进检索结果**。
2. **先红一次（本单的承重墙）**：用例落好后当场把 F1 那行的 `>= .5` 改成 `>= .4`，新用例**必须**变红；改回 `.5` 后必须变绿。每一次突变之后按 SHA-256 或 `git diff` 逐字节还原源码，还原后对 `src/` 的 `git diff` 必须零行。
3. **门禁面**（口径见 §4）：`-t:Rebuild` 0 警告 0 错误；解决方案级连续两次全绿且**分项逐位相同**；`measure-fixed-eval.ps1` 各 `exit=0`、catalog 仍 **76** 条（完成标准＝不加场景也不删场景）；`verify-repository.ps1` 通过；`git diff --check` 干净。
4. **登记面**：`CHANGELOG.md` 不加用户可见条目（这条不改变任何行为）；`docs/eval/scenarios.json` 不铸新永久场景（这不是可指认的用户可见行为，铸进去会把"76"变成"77"去顶账）；§9 交接记录写清代号、突变方向名与红例名。

## 3. 反向约束（开工前逐条读）

1. **不许为了判据好写去动那个数字**——地板既不下调也不上调，也不"顺手改成常量"。原话口径＝`QA-TIP1019-02` 修法＝加对照用例，**不许为此放宽 `.5` 那个字**。
2. **不许用源码扫描形状门冒充行为判据**。写"`Assert.Contains("Confidence >= .5", source)`"这种文本门等于把本单要修的洞照抄一遍：`QA-TIP1020-02`（分组本体被掏空而 988 条全绿）与 `QA-TIP1015-01` 就是这一族，已经两次复现。**判据必须跑真行为**（把值喂进 `MemoryQueryService` 看返回哪些行）。
3. **不许碰 F1 那行的另外两个条件**去"顺带补覆盖"——状态闸已有抓手（F2），改它属范围扩大。
4. **不许顺手标定 F4 的权重**（第 6 层标定轮未开，`2-D` 先接线后标定的门一字未解除）。
5. 单一写者：只动 `tests/Pry.Core.Tests/` 内自己名下那一份（新建文件或挂在既有文件末尾皆可），`src/` 零改动 ⇒ 不与任何在途 lane 撞行；`AGENTS.md` 第 1 条——别人名下的未提交文本与未跟踪文件一律不碰。
6. 文档与代码**分刀提交**；未 push、未 tag、未打包；`main` 归位由守门人执行。

## 4. 读数口径（这一台机器上已经踩过两次，照抄别再踩）

- `dotnet test` **只认长参** `--results-directory <绝对路径>`。短参 `-r` 会把路径拼到各程序集 `bin\Release\net10.0\` 之后，**四个程序集一条测试都没跑仍然 `exit=0`**，输出过滤器还会吞掉报错。唯一可信证据＝TRX 文件存在＋里面 `<Counters>` 非零。
- 每程序集 `--logger trx;LogFileName=<各自唯一名>`，否则互相覆盖成一个文件。
- **跳过不进 `<Counters notExecuted>`（该字段恒 0）**，只能按 `<UnitTestResult outcome="NotExecuted">` 逐条点名；今天这两条恒为 `CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`。
- 引用任何总数**必须写全四项分项**；不做"在册数＋本刀新判据"的加法。
- 起跑前先数 `dotnet.exe`：若全是本会话的 MSBuild node-reuse worker 才可跑；**绝不动他人进程**，他人结果目录不复用不引用。

## 5. 归位

落地后按老流程：守门人在合流树重跑门禁并登记（`PROJECT_PLAN.md` §4.2／§9 ＋ `docs/full-scope-sprint.md` §0），`main` 按作者 10-15「可以并线」跟线尾（只挪 ref）；**QA 从当时的线尾建干净 `--detach` 副本复采并亲手拆这一条**——本单结案词不由本角色与本 lane 写。

## 7. 开工前补正（守门人另一条 lane 2026-10-20 更晚，线尾 `1f31416`，逐锚点当场重跑；本单未认领，所以补正在任何人起刀前落地；零 `src/`、零 `tests/`）

- **§1 的四处代码锚点当场逐行读过，全部复现**：`MemoryQueryService.cs:22`（那一行结尾是 `.ToArray();`，事实表引的是前半）、`MemoryQueryService.cs:56`、`MemoryContextTests.cs:102`、`CharacterIntelligenceTests.cs:289`。§4 点名的两条环境跳过也都在盘上（`CoreTests.cs`、`LlamaServerSecurityTests.cs`）。
- **F3 那句扫描口径要补一行**：受跟踪 `tests/**` 里 `.45` 当场命中 **2** 处，除 F3 写的 `CharacterIntelligenceTests.cs:289` 之外还有 `tests/Pry.App.Tests/VisualCropEditorTests.cs:50` 的 `Assert.Equal(.45m, y.Value)`——那是裁剪坐标的 `decimal` 字面量，与检索地板无关。**结论不变**（没有任何用例碰到这道 `.5`），但认领人复跑扫描会多看见这一行，别把它读成「已经有抓手」。
- **新增 F6：同一棵链上有第二道同值地板，本单的断言面必须钉在检索面、不能钉成端到端面**（静态可读证据，本补正没跑突变）：`src/Pry.Core/Prompting/ContextBuilder.cs:88` 把 `x.Status == "active" && x.Confidence >= .5` 又过滤一遍做成 `fresh` 字典，`:96-100` 只把 `request.Memory.Hits.Take(6)` 里**存在于 `fresh`** 的行写进提示词，其余记 `dropped(…, "stale_or_deleted")`。两个后果：
  1. **只松 F1 的 `.5`** ⇒ `Hits` 多出一行 `.45`，而提示词那一面看不见（第二道门照样丢它）。若新用例断的是 `SystemPrompt` 含不含那句话，§2.2 的「先红一次」会拿到**假绿**，然后被误读成「我的仪器坏了」。**这一格是本单最容易踩的坑**：本仓库 10-19 已经为同形踩过一次（`QA-TIP1020-02`＝掏空本体而形状门仍绿）。
  2. **只松 `ContextBuilder.cs:88`** ⇒ 那行根本到不了第二道门（F1 先拦），**任何单点突变都压不红它**＝防御纵深，不是可达判据。先例口径见 `e2b521a` 那把登记的「2 处需双错（防御纵深）」。
  ⇒ 写法照既有 F2 那条用例的口径（`Assert.Empty((await Query("…")).Hits)` 这一族，直接拿 `MemoryQueryService` 的返回；`MemoryDatabase.SearchMemoriesAsync` 是同一入口的另一头）；§2.2 的突变只松 F1 那半，必须红。
  ⇒ 若还想给第二道门留证据，唯一诚实的形状是**双突变**：两处 `.5` 同时松到 `.4` ⇒ `.45` 的 active 行必须出现在提示词里 ⇒ 必须红；§9 要如实写「两处各自单点为绿、双点为红」，**不许把单点绿当覆盖**。
- **同族、明确不属于本单的两处数字**（只登记，认领人不许顺手补，守门人下一轮派单时再定级别与 Owner）：`src/Pry.Core/Intelligence/RelationshipState.cs:139` `Reliable => Confidence >= 0.5`（唯一消费者 `CharacterStateService.cs:181`；抓手＝`CharacterIntelligenceTests.cs:251`／`:254` 的 0.2 拒、0.9 放行——**两侧都不贴边**，所以地板下调无人知晓、上调会被发现）；`src/Pry.Core/Intelligence/BehaviorPlanner.cs:125` `x.Confidence >= 0.55`（抓手＝`CharacterIntelligenceTests.cs:345` 那条喂 `Confidence = 0.7` 的用例，同样是只钉住上界）。这两条比本单轻：它们不是零抓手，是**单边抓手**，本单的 `.5` 是零抓手。
