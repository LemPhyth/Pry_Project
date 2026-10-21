# HDS 第三波派单（2026-10-22）｜一个载体三片：两处已暴露缺陷的回修 ＋ 检索地板第二道门的判据

派单人：守门人（Architecture / Integration / Release）。坐标系＝`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS 任务表、
`docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md`、契约文本＝`docs/narrative-storage-contract.md`。
作者 2026-10-22 的现行指令逐字：「继续派工」；上一轮仍生效的指令：「我不想思考了，你就继续给我派工单，该提交的提交吧」
＋「按照计划书进行哪里那么多废话」＋「你 下一次 不要 再 反复构建 触发那个防火墙 烦我打游戏 再这样 我要给你喂史了」。
**最后这句决定本波的形状**：三片合进同一个载体、只允许一轮全量构建＋一次全量测试——分开派就是三次弹窗。

基线起点＝线尾 **`0c38be0`**（本工单自己入库后的那一枚；代码面与 `9f88a0c` 相同，其后三枚提交只动文档——起笔晚于它的人要先数 `git rev-list --count 0c38be0..HEAD`）。

---

## 全局约束（本载体共用，逐条可判红）

1. 开工前 `git status --short --branch`，先读最新工单与本节；**主工作区索引里他人已暂存的文件不属于你**
   （本轮实测＝`docs/dispatch-tip1020-02-memory-group-judgment.md` 1/1、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs` 63/0），
   不 stage、不提交、不回退、不"顺手整理"。
2. 独立 worktree 开工；**先数基线**：`git rev-list --count 0c38be0..HEAD` 在起点上应为 0，若从别处起笔落后就 `git merge --ff-only 0c38be0` 追平再动手。
3. **整个载体只允许一轮解决方案级构建＋一次全量测试**（作者的 dotnet／testhost 会弹他机器的防火墙、打断他的对局）。
   迭代期只用 `dotnet build <单个 csproj>`（不碰整解决方案）与读码推理；把全量那一轮**留到最后一次**。
   出口之后的"先红后绿"一律走**定向**跑法：`dotnet test tests/<X>/<X>.csproj -c Release --filter "FullyQualifiedName~<用例名>"`
   配单项目 `-t:Rebuild`，一次拆一道守卫；**不许为了反证重跑全量**。
4. 出口读数的唯一写法：
   `dotnet test Pry.slnx -c Release --no-build --results-directory "<绝对路径>" --logger trx`，
   跑完先数到**四份** `.trx` 再逐份读 `<Counters>`；没有 TRX 就没有证据（控制台"已通过!"不算）。
   短参 `-r` 会把结果写进各程序集 `bin\`、四程序集零执行仍 exit 0。
5. **不许拿 `--no-build` 的旧二进制当出口证据**：最后一轮必须紧跟一次完整 `dotnet build Pry.slnx -c Release`（且构建读数 0 警告 0 错误）。
6. 新判据先红后绿：每条新断言都要当场拆一次它守的那道守卫，红例名与报错原文记进交付说明；只跑绿＝没有证据。
7. Core 不依赖 Avalonia／HTTP；不新增 NuGet 包；模型、运行库、数据库、密钥、日志、用户导入内容不得提交；
   `%LOCALAPPDATA%/PryCompanion` 是禁区，**连只读都不许当测试载体**（真实锁与路径测试一律用临时目录）。
8. 状态词与结案词不由实现方写：证据齐了也只填"已实现待验"。
9. 不许 `git tag`、不许 push、不许打包、不许改美术许可文件、不许动 `ASSETS_LICENSE.md`。
10. 三片的文件面**互不相交**（见下），任何人都不许越到自己片外改别人的文件；确需触碰他人名下文件时，
    只允许"语义等价的那一行"级别，且必须在交付说明里逐行列出改了什么。

---

## 片 A｜DEF-WRIGATE-01：第二宿主必须"停下"，不是"告警之后覆盖"

**Owner**：Platform / API（写入者唯一，先登记）。前置：HDS-002 已归位（`DataDirectoryWriteGate` 在生产可引用）。

### 现在是什么（当场读码，非转述）

- `src/Pry.Api/Services/RuntimeDescriptorService.cs`：`Write()`（`:49`）在 `:59` 调 `ReportLeftover()`，随后 `:63` 照样 `WriteAtomically(descriptor)`；
  `ReportLeftover()`（`:71`）在 `:88-89` 打的日志原文是「发现上一实例遗留的 {File}（{Observed}），本次启动将覆盖它」。
- 也就是说 `IsProcessAlive(pid)` 为真（**两个后端同时活着**）时，现在的处理仍是"记一条警告然后覆盖"。
- 这正是契约点名禁止的形状：`docs/narrative-storage-contract.md` §10.1（`:288-289`）把它写成"当前没有任何进程级独占写锁"，
  §10.2 第 2 条（`:299`）定成必拒项——`WriteLockHeldByOther`，且「接口上不存在『记录一次告警然后继续』的合法出口」。
- 可用的判定已经在生产代码里：`src/Pry.Core/Narrative/DataDirectoryWriteGate.cs` 的 `TryAcquire(credential, observation)`、
  `CheckConditionalWriteAllowed`、`FenceAndReacquire`。契约 §10.2 同时写明**真实锁句柄由宿主 Infrastructure 层实现，Core 不碰进程与文件系统**。
  ⇒ 本片的落点就在 Api（宿主层），不是 Core。

### 要交付什么

1. 启动写描述文件之前先取得写权：宿主层持有一个**真实的进程级独占句柄**（例如数据目录下一把 `FileShare.None` 的锁文件，
   随进程释放），用它填 `DataDirectoryObservation`，再以 `WriteLeaseCredentialKind.ExclusiveWriteLock` 调 `TryAcquire`。
2. `TryAcquire` 拒绝时：**一个字节都不写**（不 `CreateDirectory`、不 `WriteAtomically`），并让宿主以一句人话呈现"这个数据目录正被另一个 Pry 同时写"后**拒绝启动**
   （呈现走既有链：`Pry.Contracts` 的 `IUserPresentableError` → `src/Pry.Client/PryErrorPresentation.cs:52`，`RetryAllowed` 由你判断但必须明说重试会怎样）。
3. 判据必须钉在**行为**上，不是文本上：
   - 同数据目录两个宿主 ⇒ 第二个不覆盖第一个的 `runtime.json`（先写后争，读回内容仍是第一个的 `Pid`/`Port`/`Token`）；
   - 第二个宿主拿不到写权时，`runtime.json` 不存在或仍是前者的，且没有任何"告警后继续"的分支被走到；
   - 释放/退出路径不把后继实例的入口删掉（既有 `Clear()` 的"只删自己写的那份"语义不许被削弱）；
   - 单宿主正常路径行为不变：描述文件照样写、字段格式不变、优雅退出照样清。
4. 顺手把契约 §10.1 那段"现在是什么"改写成本片落地后的事实（`:288-289` 两句 + 同段"当前没有任何进程级独占写锁"那句），
   并**在同一次改写里消掉 `docs/narrative-storage-contract.md:290` 那行的加粗形状缺陷**（`**「` 开不了口；只删/挪标记，语义不动）。

### 不许走的捷径

- 不许用"读到 `runtime.json` 里那个 pid 不在了"自证写权——契约 §10.2 第 1 条把这条路定为 `DescriptorIsNotALock`；pid 存活也不是锁。
- 不许改 `src/Pry.Core/Narrative/` 里已归位的判定语义（那是 HDS-002 名下契约文件；要改先向守门人登记写入者）。
- 不许把拒绝做成"静默不写"而不告诉用户；也不许只加日志。
- 不许改 `runtime.json` 的字段形状或 `MemoryDatabase.SchemaVersion` 那个数字。
- 不许引入新包来实现锁（`FileShare.None` 足够）。

---

## 片 B｜DEF-MEMDUAL-01：第二套版本账的拒写必须走可呈现错误

**Owner**：Persistence / Core。前置：无（属已暴露旧缺陷，回修不需要"轮到该阶段"）。

### 现在是什么（当场读码，非转述）

- 同一个 SQLite 文件里有**两套结构版本账**：
  - `schema_info`（`src/Pry.Core/Memory/MemoryDatabase.cs:57` 建、`:208` 的 `SchemaVersion = 10` 是写侧唯一编号）——
    高版保护在 `EnsureWritableSchemaVersion`（`:195-199`），抛 `SchemaVersionTooNewException`；该类型
    （`src/Pry.Core/Memory/SchemaVersionGuard.cs:15`）实现 `IUserPresentableError`，带 `Headline`/`Body`/`RetryAllowed=false`。
  - `memory_schema`（`src/Pry.Core/Memory/SqliteMemoryRepository.cs:65-66` 建、`:99` 读、`:134` 由 1 推进到 2）——
    `:100` 对 `version is < 1 or > 2` 只抛**裸 `InvalidOperationException`**，不进那条呈现链。
- 拒写本身**已有测试钉住**：`tests/Pry.Core.Tests/MemoryDeletionSeparationTests.cs:273-288`（把 `memory_schema` 改成 3／1／0 都断言拒与自愈回 2）。
  ⇒ 缺的不是"拒不拒"，是**用户看到什么**：绕过头道门时界面按普通失败处理，会引导他反复启动。
- 契约 §11.2 第 1 条（`docs/narrative-storage-contract.md`:「版本来源仍是 `schema_info` 这一张登记账……一个库只能有一个『当前结构版本』的问法」）
  已把"一个库一个问法"定成口径。

### 要交付什么

1. 把 `SqliteMemoryRepository.cs:100` 那处拒绝改成**可呈现**形状：新建或复用一个实现 `IUserPresentableError` 的类型
   （倾向放在 `SchemaVersionGuard.cs` 同侧，与既有两个类型并列），`Headline`/`Body` 用中文写清"这份数据库的记忆组件结构来自更新版本的 Pry、
   已拒绝写入、重试不会改变结果"，`RetryAllowed=false`。异常消息里保留技术细节（版本号、组件名）。
2. 加一条断言把**两账并存这件事本身**钉住：一个测试能区分"高版库是被哪一道门拦下"（`schema_info` 门 vs `memory_schema` 门），
   并且**两道的拒绝都走可呈现链**——即断言面是 `IUserPresentableError` 的 `Headline`/`Body`/`RetryAllowed`，不是 `ex.Message` 的字符串匹配。
3. 既有 N12 那几条断言的**语义必须活下来**（拒写、版本单调、自愈到 2）：可以改写承载它的文件，**不许整段删除**；
   若你把它迁到新测试文件，交付说明里必须逐条写出"原来钉的东西由谁接手"。

### 不许走的捷径

- **不许把两账合成一账**（删表、停写 `memory_schema`、给它分配新版本号都是迁移决定，归 HDS-005；本片只补可见性）。
- 不许改 `MemoryDatabase.SchemaVersion = 10` 或给 `memory_schema` 引入 3；版本编号分配权归 PLAT-003（§5.0 冻结中）。
- 不许用文本匹配异常消息来过门；不许 `catch` 掉那句裸异常改成静默。
- 不许加"自动降级旧库/重建记忆表"的任何路径——那是拿用户数据换一个绿灯。

---

## 片 C｜QA-TIP1019-02 的第二道门：`ContextBuilder.cs:164` 的地板复核

**Owner**：QA / Core 测试。**零产品代码**：不许动 `>= .5` 这个数（作者口径：地板数值是待标定项，不是本片范围）。

### 现在是什么

- 第一道门＝`src/Pry.Core/Memory/MemoryQueryService.cs:22` 的 `x.Confidence >= .5`，判据已由 `9f88a0c`
  （`tests/Pry.Core.Tests/MemoryRetrievalFloorTests.cs`，1 条 `[Fact]`）钉住，突变 `.5→.4` 已证红。
- 第二道门＝`src/Pry.Core/Prompting/ContextBuilder.cs:164`：提示词组装前对候选记忆做**重读资格核对**
  （`x.Status == "active" && x.Confidence >= .5`）。上一单只做了第一道，
  `docs/dispatch-tip1019-02-retrieval-floor-judgment.md` §9 明记「未做双突变 ⇒ 第二道门本轮仍无判据」。
- 风险形状：查询结果（缓存 hit）里带着一条地板上的记忆，而在组装前该记录被改到地板下 ⇒ 第二道门是唯一的拦截处；
  把 `:164` 的 `.5` 掏空（改成 `>= 0`）现在没有任何测试会红。

### 要交付什么

1. 一条行为判据（新建独立文件，命名与被拦那道门对应，例如 `tests/Pry.Core.Tests/MemoryFloorRecheckTests.cs`）：
   构造 `request.Memory.Hits` 里含一条"查询时合格、组装前已被降到 `.45`"的记忆，另有两条对照（`.5` 贴地板合格、`.45` 地板下），
   断言面落在 `ContextBuilder` 产出的提示词与 `dropped` 原因上（`stale_or_deleted` 那类形状），
   **不要**断 `MemoryQueryService`（那是第一道门已有的判据，会互相顶包）。
2. 夹具先自检（照 `9f88a0c` 的写法）：状态那半不许替本判据背书。
3. 反证（定向跑）：拆 `:164` 的 `.5 → 0`（或整段去掉 `Confidence` 条件）看新判据转红，红例名＋报错原文进交付说明；改回后转绿。
   **不许顺手拆 `:22`**——那一道的红已由上一单采过，拆它只是多起一轮构建。
4. 不许改成源码文本门（grep 型断言）；上一单已把那条口径写死：断言面必须是行为。

---

## 片 D｜零构建的文档刀（可单独交给一个人，不必进全量轮）

- `docs/narrative-storage-contract.md:219`：`**「可发送真相」**` 这类 `**` 内侧紧跟全角引号的形状在作者那两页开不了口；
  只删标记或把引号挪到标记外，**语义与其余文本逐字节不动**。
- 同一文件的 `:290` 由片 A 在改写 §10.1 时一并消掉，**本片不重复做**（避免两片撞同一行）。
- 校验工具与口径：`node .local-notes/bold-flank-newonly-1021.js '<本刀改动的跟踪文件 JSON>'`，
  正向对照＝`.local-notes/bold-probe-1021.md` 1-3 行必须数出 `DEFECT_LINES=2`；对照不成立就是仪器坏了，先修仪器再判红。
- 这一片**不起 dotnet**：纯文档改动，出口证据只需 `git diff --check` ＋ 上述检测器输出 ＋ 改动前后 `git diff --stat`。

---

## 出口证据（一次跑齐，缺一项就不算完）

1. 合流前本载体树上**四份 `.trx`** 的 `<Counters>` 全量读数：总／四分项／失败／点名跳过（跳过只按 `<UnitTestResult outcome="NotExecuted">` 点名，`notExecuted` 计数恒 0）。
2. **顶账表**（起点＝`0c38be0` 的代码面，读数由守门人 2026-10-22 当场逐份读 TRX 采）：
   Core **524**／Api **258**／App **233**／Client **106**＝总 **1121**，失败 0，通过 1119，两条环境跳过
   （`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`）。
   出口必须写成"某分项 524 → 524＋N，N＝本片新增 X 条（逐文件 `grep -c '[Fact]'`＋`[Theory]`/`[InlineData]` 计数）"；
   **四个分项对不上＝读数作废**，回来找守门人，不要自圆其说。
3. 完整 `dotnet build Pry.slnx -c Release` 那一轮的 **0 警告 0 错误**读数。
4. 本刀新判据的**红例名＋报错原文**：片 A 至少两条（拆"锁被占仍写"、拆"拒绝后不呈现"），片 B 至少两条（拆可呈现链、拆第二道账的识别），片 C 一条（拆 `:164` 的地板）。
5. `scripts/verify-repository.ps1` 与 `scripts/measure-fixed-eval.ps1 -ResultsDirectory <本单专属目录>` 的当场输出（catalog 仍 **76**；
   `verify-repository` 那个数＝当前跟踪文件条数，起点 **530**（`git ls-files` 在 `0c38be0` 上数到 530，含本工单自己），每落一份文档就往上走，别把它读成门槛变化）。
6. `git diff --check` 干净；`CHANGELOG.md` 若要写就随片 A/B 各一行，不写不判红。
7. 交付说明里逐条列出：动过的文件、每个文件为什么动、有没有触碰他人名下文件、构建与全量测试各跑了几次（**期望＝1 和 1**）。

## 交付形态

- 代码/测试与文档**分刀提交**，前缀按 `feat:`／`fix:`／`test:`／`docs:`；提交信息里点名本片编号（DEF-WRIGATE-01／DEF-MEMDUAL-01／QA-TIP1019-02 第二道门）。
- 落点建议：片 A＝1 枚 src＋1 枚 tests＋1 枚 docs（契约 §10.1 那两句）；片 B＝1 枚 src＋1 枚 tests；片 C＝1 枚 tests；片 D＝1 枚 docs。
- **不要自己合回主线**：留在独立 worktree/分支上，把分支名与提交 SHA 交给守门人归位。
  归位纪律（守门人这边，2026-10-22 已踩过一次）：收前先 `git diff --name-only <base>..<lane>` 数一遍它改了什么，
  含工单/台账文档的提交逐份核对基线版本，**绝不整枚收下会删掉他人补正的文档提交**。

## 排队（本波不开工，顺序不是许可）

1. **HDS-011c**（资源限额总盘＋依赖/许可复采）——AUD-07 已落地，它的前置解除，但它要自己起一轮测量；**等本载体归位后再派**，否则两个载体同时弹窗。
2. **HDS-003** 新生成中心隔离原型——真实模型对照要占作者这台机器的算力或 API 额度，**动手前需作者点头**。
3. HDS-004／005 的表与接线——开工前逐条读 `docs/narrative-open-items-memo.md`；硬前置＝U-2／U-3／U-11＋U-5 实例维（005）、U-9 与 HDS-003 的语义结论（004）。
4. `QA-TIP1020-02` 的实现仍悬在他人索引（63/0），等它归位，不重复派。
5. 客户端路由判据 `2954dbc`（`QA-TIP1019-03`）——等主工作区索引清空由守门人合流，不派给实现方。
