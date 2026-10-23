# 第十七波工单·HDS-005 落库面（真实 SQLite：叙事表＋输入处理＋outbox 的原子提交，以及真实库上的崩溃／部分送达矩阵），不接 API、不动旧链表

- 工单 id：`HDS-WAVE17-1023`
- 作者：守门人（架构／集成／发行）
- 出闸时间：2026-10-23 16:1x（**当场 `date` 取时点**）
- 载体基点：**`def0faf`**（当场 `git rev-parse HEAD` 复核）
- 执行方：冷启动 lane
- 性质：**动产品代码的主干刀**（存储面）。与第十六／十八波并行，文件面互不相干。
- 作者现令（逐字）：「你干脆能做的一次性全做了，直接大跨步开发疯狂挂子智能体就好了，我想快些结束这个项目」

---

## 〇 开工前必须当场做的三件事

1. `git -C C:/Users/24782/Desktop/Pry_Project status --short --branch`；他人名下未提交项（含两枚暂存项 `docs/dispatch-tip1020-02-memory-group-judgment.md`／`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`，以及未跟踪 `docs/research/*`、`docs/character-learning-safety.patch`、`.qoder/`）**不读内容、不碰、不提交**。
2. 建独立载体：`git -C C:/Users/24782/Desktop/Pry_Project worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave17-1023/carrier def0faf`；入场两行判据（`git status --short | wc -l`＝0、`git rev-parse HEAD` 全号）原样贴进报告。
3. 每一次 `dotnet` 起跑前先跑 `.local-notes/game-preflight6-1022.ps1`：`BUILD_OK` 才起，`HOLD` 就停下等待（不绕开、不请示、不换构建方式）。一次只起一个 `dotnet`。SQLite 资源形状失败（`disk I/O`／`out of memory`）＝该轮读数作废，换目录重跑一次；两次同形状就停刀回报，绝不动别人进程、绝不降判据。

### 〇.4 允许动什么

- `src/Pry.Core/Narrative/Storage/`（**新建子目录**）下的新文件：表定义、迁移登记、真实 SQLite 仓储实现；
- `tests/Pry.Core.Tests/` 新测试文件（建议 `NarrativeSqliteCommitDeliveryTests.cs`）；
- 报告一份＝主树 `docs/eval/reports/2026-10-23-hds005-sqlite-commit-delivery.md`（id＝`eval-HDS005-1023`），**按批次增量落盘**。

### 〇.5 绝不允许（反向约束）

1. 改任何**既有**文件的成员／条件／阈值／枚举取值；尤其 `src/Pry.Core/Narrative/` 那十份（第十二／十三波刚钉过格）与 `src/Pry.Core/Memory/**` 的既有仓储／迁移登记表语义。**新增登记项允许，改既有行不允许。**
2. **不动旧链表的形状、不迁移旧数据**（`messages`／记忆表／关系心情经验数据的搬迁＝HDS-009，本波不碰）。
3. 不接 API 端点、不改 `src/Pry.Api/**` 运行时路径、不碰 Client/App；**不接生产数据目录**：所有测试用一次性隔离库文件（temp 目录），`%LOCALAPPDATA%\PryCompanion` 一律禁止。
4. 未知高版保护照旧：新表登记必须走既有 schema 登记／版本门机制，**不得**绕开"高于二进制登记版本即拒"那条链（`SchemaVersionGuardTests.cs:66/83/149` 守的就是它）。
5. 数字门槛零改动；不新造门槛。不许固定返回值／吞异常／伪造成功／绕过安全检查；不许删、注释、跳过或放宽既有断言。
6. 模型、运行库、密钥、日志、用户导入内容、`licenses/**`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md` 一字不碰。
7. 零 tag／零 push／零打包；不 merge 主树、不 FF `main`；不自封基线、不写状态词。

## 一 先读

`AGENTS.md` → `docs/plans/PROJECT_PLAN.md`（§4.2 的 HDS-005 行＋§9 最近三格）→ `docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md` §13 表 HDS-005 行与 §14.2 的 `NR-07`～`NR-10` → `docs/agent-engineering-standards.md` → 盘上现状：`src/Pry.Core/Narrative/DeliveryLedger.cs`、`NarrativeCommitWriter.cs`、`OutboxOwnershipRegistry.cs`、`NarrativeDocuments.cs`（`NarrativeCommit`／`DeliverySegment`／`DeliverySegmentKey`／`NarrativeIntent`）、既有 SQLite 设施（`src/Pry.Core/Memory/**` 的仓储／schema 登记／版本门）、`tests/Pry.Core.Tests/NarrativeStorageContractTests.cs`（**契约层已有 30 余枚，本波做的是它们在真实库上的等价格**）、`MemorySchemaMigrationRegistryTests.cs`、`SchemaVersionGuardTests.cs`、`DataBackupServiceTests.cs`。

**表名与列名不许自造同义名**：先从 `docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md` 与 `docs/narrative-*.md` 契约文档里取权威名（`narrative_entries`／`character_instances`／`narrative_intents` 等已登记在盘的缺口清单里点名过），把取到的原文行号贴进报告，再建表。

## 二 本波要把哪几件事变成"改坏了会响"的格

1. **建表与登记**：叙事条目、实例映射、意图、outbox／投递段的表在一次性隔离库里能建起来；重复初始化幂等（指纹一致）；**半路炸掉的迁移步骤不留痕、重跑补完**；**高于二进制登记版本的库不写**（照旧拒）。
2. **原子提交**：一笔 `NarrativeCommit` 的"条目＋输入处理水位＋意图＋outbox 段"必须在**同一事务**；中途任一步抛异常 ⇒ 库分毫未动（逐字节或逐表行数对平），**不许**出现半笔。
3. **真实历史逐段**：段正文必须逐字取自带绑定原文（契约层 `NarrativeStorageContractTests.cs:192` 的真实库等价格）；段号不可跳；已投递为终态、迟到结果不可翻转；**标 delivered 必须有真实历史证据**。
4. **崩溃矩阵**（每条一格，且必须在真库上重启连接后判定，不是内存里假装）：① 写入前崩溃 ⇒ 不产生半笔、可重新提交；② 提交后崩溃 ⇒ 重放不二次消费、不重复生成；③ 历史成功但投递丢失 ⇒ 补取不重复、不遗漏。
5. **部分送达矩阵**：前段未投不得标后段；部分送达后的迟到结果只允许补齐未投段，不得改写已投段。
6. **失权与世代**：失权世代的迟到提交在真实库上被拒；故事版本过时或超前被拒；删除作用域后迟到提交不能复活它。
7. 上述每一格都要**单枚、单轮、清洁**实测红（一轮一枚，十一项字段逐轮登记；口径同第十二～十四波）。**"改坏了零代价"的那类出口如果发现，就地登记，不要顺手扩到别人的文件。**

## 三 读数与交付

1. 载体一次构建 ⇒ 期望 **0 警告 0 错误**（自造的 `xUnit2013`／`xUnit1051` 类告警在同载体用"同方法内换形状"清掉，不动判据条数）。
2. 定向一轮＋全量一轮（收尾）：`dotnet test Pry.slnx -c Release --no-build --results-directory "<绝对>" --logger trx` ⇒ `ls *.trx` 数到 4 再读 `<Counters>`；报四分项＋总数，**不自封基线**，并明写"写判据与跑这轮是同一只手"。
3. TRX 纪律：每轮独立目录；突变轮零 `--no-build`；跳过按 `outcome="NotExecuted"` 点名。
4. 报告必须有一节"仍未验证"：至少**未接生产路径**（API／Client／App 一字未动，新表今天没有任何生产调用方）、真实模型、UI 可见行为、真实数据目录未演练、备份包是否涵盖新表（那是 HDS-001 的包含策略，本波不许顺手改，登记为缺口）。
5. 报告末尾给守门人"归位提示"：`git show --stat` 全号、新增文件行数与 SHA-256、`git diff --stat def0faf..HEAD -- src tests` 原文。

## 四 预算与停刀线

构建 1 次、全量 1 次、定向 ≤ 22 次；撞 150 轮上限或预检连续两次 `HOLD` ⇒ 停刀并把增量报告落盘回报。既有 49／62／16 格不许动，本波只新增格。
