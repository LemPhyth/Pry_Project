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
