# 第十六波工单·HDS-004 叙事生命周期协调（隔离刀）：作用域幂等、实例单写者、公平调度、取消、删除失效、预算与失败语义，不落生产库、不接 API/Client

- 工单 id：`HDS-WAVE16-1023`
- 作者：守门人（架构／集成／发行）
- 出闸时间：2026-10-23 16:1x（**必须当场 `date` 取时点**，不许引用本行之外的钟）
- 载体基点：**`def0faf`**（第十三～十五波收口入库后的线尾；当场 `git rev-parse HEAD` 复核）
- 执行方：冷启动 lane（除本工单与 `AGENTS.md` 四份必读之外无上下文）
- 性质：**动产品代码的主干刀**。交付物允许新文件；禁止改动既有契约的既有语义。
- 作者现令（逐字）：「你干脆能做的一次性全做了，直接大跨步开发疯狂挂子智能体就好了，我想快些结束这个项目」⇒ 本波与第十七／十八波**并行**，同一台机器、互不相干的文件面。

---

## 〇 开工前必须当场做的三件事

1. `git -C C:/Users/24782/Desktop/Pry_Project status --short --branch`——他人名下未提交项一律当别人的资产：不读内容、不改、不提交、不 `stash`、不 `clean`。已知他人名下两枚暂存项＝`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`，另有未跟踪的 `docs/research/*`、`docs/character-learning-safety.patch`、`.qoder/`，**本波一律不碰、不提交**。
2. 建独立载体（**不动主工作树**）：
   `git -C C:/Users/24782/Desktop/Pry_Project worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave16-1023/carrier def0faf`
   入场两行判据原样贴进报告：`git status --short | wc -l` ＝ **0**，`git rev-parse HEAD` ＝ `def0faf` 全号。
3. **另两条 lane 在飞**（第十七波＝HDS-005 落库面，载体 `.local-notes/qa-wave17-1023/carrier`；第十八波＝判据刀零产品码）。你与它们共用一台机器：
   - **每一次 `dotnet` 起跑前先跑预检** `powershell -NoProfile -ExecutionPolicy Bypass -File .local-notes/game-preflight6-1022.ps1`；读数 `BUILD_OK` 才起跑，`HOLD` 就**停下等待**（不请示、不绕开、不改用别的构建方式），下一轮再预检。原因：作者机器上 `dotnet` 会弹防火墙并打断他的对局。
   - 一次只起一个 `dotnet` 调用；全量轮若出现 SQLite `disk I/O`／`out of memory`／资源形状失败（不是断言失败）＝**读数作废**，换新 TRX 目录重跑一次；两次同形状就停刀回报，**绝不动别人的进程、绝不降判据**。

### 〇.4 允许动什么

只允许在这些路径**新增**文件＋你自己的报告：

- `src/Pry.Core/Narrative/` 新文件（建议名：`NarrativeLifecycleCoordinator.cs`，可按需要再拆 `NarrativeScopeRegistry.cs`／`NarrativeSchedulingDecision.cs`；文件名由你定，但**不得**与既有文件同名）；
- `tests/Pry.Core.Tests/` 新测试文件（建议 `NarrativeLifecycleCoordinatorTests.cs`）；
- 报告一份＝主树 `docs/eval/reports/2026-10-23-hds004-lifecycle-coordinator.md`（报告 id＝`eval-HDS004-1023`），**按批次增量落盘**：每完成一节先写一段，撞 150 轮上限之前不许攒着一次写。

### 〇.5 绝不允许（反向约束，逐条都是走过人的坑）

1. 修改 `src/Pry.Core/Narrative/` 下任何**既有**文件的成员、条件、阈值或枚举取值（`NarrativeDocuments.cs`／`NarrativeDraftValidator.cs`／`NarrativeContractReason.cs`／`DeliveryLedger.cs`／`OutboxOwnershipRegistry.cs`／`NarrativeCommitWriter.cs`／`CharacterInstanceIdentity.cs`／`NarrativeContextCompiler.cs`／`NarrativeRoundRunner.cs`／`NarratorContracts.cs`）——这些是第十二／十三波刚钉成会响的格，动它们＝拆别人的判据。**只允许新文件引用它们。**
2. 不接生产库、不接 API／Client／App、不动 `src/Pry.Api/**` 与 `src/Pry.Client/**`、不动 SQLite 仓储、不动旧链装配器 `ContextBuilder.cs`。
3. 新协调器**不得引用**旧链四件：`BehaviorPlanner`／`ReplyGenerationRenderer`／`ReplyPlanner`／`ReplyNecessity`（v4 §14.1 第 1 条要的就是"新路线没有偷偷回落旧链"，本波先把这条守成一格，见 §二.7）。
4. 数字门槛零改动：`.7`／`.5`／`2400`／五档超时／`MinimumUsableTokensPerSecond = 12`／`4096`／`DefaultSafetyTokens = 128`／`MinimumInputTokens = 512` 一个字不动；**本波不许新造数字门槛**——超时与预算一律从入参取（沿用 `NarrativeRoundRunner.RunAsync(..., TimeSpan narratorTimeout, DateTimeOffset now, CancellationToken)` 的形状），配额类常量若非要落地，必须写成"复用既有契约字段"而不是新尺。
5. 不许用固定返回值／吞异常／伪造成功／绕过既有安全检查让格子变绿；不许删、注释、跳过或放宽任何既有断言。
6. 模型、运行库、数据库、密钥、日志、用户导入内容、`licenses/**`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md` 一字不碰；`%LOCALAPPDATA%\PryCompanion` 不得当测试载体。
7. 零 `git tag`／零 `git push`／零打包；**不 merge 主工作树、不 FF `main`**（归位由守门人做）；不自封基线、不写状态词、不写"结案／已完成"。

## 一 先读（顺序不限，读完再动刀）

`AGENTS.md` → `docs/plans/README.md` → `docs/plans/PROJECT_PLAN.md`（§4.2 的 HDS-004 行＋§9 最近三格）→ `docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md` §13 表（`:319-331`）与 §14.1（`:347-354`）→ `docs/agent-engineering-standards.md`（交付标准与验证标准）→ 盘上现状：`src/Pry.Core/Narrative/` 全部文件、`tests/Pry.Core.Tests/NarrativeIdentityContractTests.cs`、`NarrativeStorageContractTests.cs`、`NarratorPrototypeTests.cs`（49 格）、`src/Pry.Core/Runtime/ConversationRuntime.cs` 与 `TurnScheduler`（**旧链的排队语义读它只为对照，不引用**）。

## 二 本波要把哪几件事变成"改坏了会响"的格（逐条一行，一条至少一枚判据）

1. **作用域与幂等**：在飞与已完成的作用域键＝(实例, 会话, 批次)。同一键第二次请求**不得**产生第二次生成，必须取回同一提案／同一拒绝理由；跨键不互相吞。
2. **实例单写者**：同一实例同时刻至多一笔在飞；第二笔按调度规则排队或被拒，被拒要给可点名的理由（复用 `NarrativeContractReason` 既有取值，不够用就**新增文件里定义新的协调层枚举**，不许改既有枚举）。不同实例可并行。
3. **公平调度**：一个实例下多会话排队时的次序必须有明确规则（建议：会话级轮转＋同会话内按登记次序），并有"某个会话不被饿死"的判据。规则由你定，但**必须在报告里逐字写出**，且判据钉的是规则不是你的心情。
4. **取消**：同会话新输入到达 → 旧草稿取消；取消必须传播到 `NarrativeRoundRunner`（它已接 `CancellationToken`），取消后**不得提交**；无视取消的迟到结果永久无资格（世代语义走 `OutboxOwnershipRegistry`／`NarrativeCommitWriter` 既有出口，不许另造一套失权判定）。
5. **删除与重配置失效**：会话删除、实例重配置或故事版本（epoch）变更 → 该作用域全部在飞与迟到结果作废，**且不重新入队**；对照 `NarrativeStorageContractTests.cs:403`（删除后迟到提交不能复活作用域）在协调层的等价格。
6. **预算与失败语义**：一轮＝一次 Narrator 调用额度（沿用 wave-13 的一次调用形状）；超额度、provider 失败、超时必须是**可区分的三种失败语义**，其中"provider 失败／坏响应"绝不允许映射成 `CommunicationMode.Silence`（这正是 v4 §14.1 第 3 条与 `NR-09/10` 的"坏响应不能伪装合法沉默"）。
7. **不回落旧链（源码扫描格）**：一格扫 `src/Pry.Core/Narrative/` 的新协调器文件，断言其对旧链四件的引用数为 0；**必须带正向探针**（在测试内用同一 scanner 扫一份含旧链引用的样本，证明 scanner 会响），否则是一枚常绿门。
8. 上面每一格都要拿到**单枚、单轮、清洁**的实测红：一轮只改一处、改坏 → 恰好该格红 → 逐字节还原 → 双 SHA 对平。十一项字段逐轮登记（载体全号／文件:行／改前→改后取自当场 `git diff`／`diff --stat`／完整跑法命令／TRX 文件名／红格全名＋消息＋锚点／还原方法／改前改后 SHA-256／还原后 `git status --short` 行数／结论）。

## 三 读数与交付（缺一即登记为"未验证"，不许虚构）

1. 载体一次构建：`dotnet build Pry.slnx -c Release` ⇒ 期望 **0 警告 0 错误**（警告不是错误，但本仓的构建读数口径是 0 警告；`xUnit2013`／`xUnit1051` 这两类自己造的告警必须在同载体清掉，写法＝同方法内换形状，不动判据条数）。
2. 定向一次：`dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter FullyQualifiedName~<你的测试类名> --results-directory <绝对路径> --logger trx` ⇒ `ls *.trx` 数到 1 之后读 `<Counters>`。
3. 全量一轮（收尾）：`dotnet test Pry.slnx -c Release --no-build --results-directory "<绝对路径>" --logger trx` ⇒ 数到 4 份 TRX 再报四分项＋总数。**只报读数，不自封基线**；并写明"本轮由写判据的同一只手单采"。
4. TRX 纪律：每轮独立目录；突变轮**零** `--no-build`；跳过项只能按 `<UnitTestResult outcome="NotExecuted">` 点名（`notExecuted` 计数恒 0，别把它读成"没跳过"）。
5. 报告必须有一节"仍未验证"：至少真实模型对照（零读数，要作者点头）、生产接线（API/Client/App 未动）、生产库未接触、UI 可见行为、以及本波任何够不着的反证（**够不着要连布尔返回门与 fallback 一起算**，不许写"结构上永远够不着"就了结）。
6. 报告末尾给守门人一段"归位提示"：`git show --stat` 全号、每枚新增文件的行数与 SHA-256、`git diff --stat <基点>..HEAD -- src tests` 原文。

## 四 预算与停刀线

- 构建 1 次、全量 1 次、定向 ≤ 20 次（含 §二.8 的逐枚反证轮，一轮一枚）。
- 撞 150 轮上限或撞预检 `HOLD` 连续两次 ⇒ **立即停刀**，把已完成的增量报告落盘并回报现状（已证到哪、还剩哪几条），不许攒着补写，也不许为赶交付降格。
- 判据条数与既有 49／62／16 格**不许动**；本波只新增格。
