# 消息删除与派生记忆的分离（MEM-006 设计）

来源：`docs/memory-context-contract.md` §7.3 与 §15.1 裁定 R3（本轮不做，转独立任务）。
Owner：本文定义语义与验收不变量（Memory & Context）。DDL 落地归 Persistence Owner，检索/写回改动归 MEM-005 实现 Owner，管理面归 Client/UI。
状态：设计完成，**未实现**。本文不主张任何已运行证据。

行号引用自**未提交**的 `codex/memory-context` worktree（`.local-notes/memory-context`）。主工作树当前**没有**这些文件（`src/Pry.Core/Memory/` 只有 4 个 `MemoryDatabase*.cs`）；本文的"现状"以该 worktree 快照为准。

---

## 0. 一句话

现在"删一条聊天消息"和"删一条记忆"共用同一个删除原语，因此**共用同一块永久墓碑**。这让清理聊天记录变成了**永久性地、不可恢复地撤回该事实，并且永远禁止角色再次学会它**。

---

## 1. 已核实的现状链路

| # | 事实 | 出处 |
|---|---|---|
| S1 | `BEFORE DELETE ON memories` 触发器对**任何**记忆行删除都记墓碑 `(old.id, old.scope_key, old.fingerprint)`，并把该作用域 `epoch+1`、`forget_before = MAX(messages.id)`、放弃全部 `pending/claimed` 候选任务 | `SqliteMemoryRepository.cs:117-122` |
| S2 | `BEFORE INSERT ON memories`：只要该 `id` 有墓碑就 `RAISE(IGNORE)`，静默丢弃插入 | `:123-124` |
| S3 | 重新抽取时按 `(scope_key, fingerprint)` 查墓碑，命中即跳过——**永久**，不看时间 | `:398-399` |
| S4 | 删消息前显式删记忆，条件只有 `source_message_id IN (被删消息)` | `MemoryDatabase.cs:552,558`（删会话 `:325`、撤销 `:586` 同型） |
| S5 | `memory_evidence.message_id` 是 `NOT NULL ... ON DELETE CASCADE`：证据行随消息一起销毁 | `:73-77` |
| S6 | `memories.source_message_id` 是 `NULL ... ON DELETE SET NULL`：**记忆行本身并不会被外键删掉**，是 S4 的显式 SQL 删掉的 | `MemoryDatabase.cs:60-62` |
| S7 | 撤销快照只携带旧版 10 列 `MemoryRecord`，恢复用 `INSERT OR REPLACE INTO memories(id,...10 列)` | `:601-608` |
| S8 | 置信度由证据条数推导：`MIN(.99, .9 + .03·(n−1))`，且**只在写回时重算** | `SqliteMemoryRepository.cs:430-433` |
| S9 | 检索与管理的可见性都过 `status='active'` 单一闸口：`MemoryQueryService.cs:22`、`MemoryDatabase.cs:385` | 见下 §4.3 |

### 1.1 由此得到的四个后果

**C1（Critical）删一条消息 = 该事实永久中毒。**
链路 S4 → S1 → S2/S3：删除消息触发记忆行删除，写入指纹墓碑；此后用户重新说出完全相同的事实，`CompleteAsync` 在 `:398` 跳过它。同时 `forget_before` 被推到当前最大消息 id（S1），把当时所有已存在消息一并水印掉。
用户可观察行为：**"我上次不是告诉你我喜欢红茶吗"——因为那条消息被删过，这个角色再也学不会这件事，且没有任何提示。**
按 `docs/eval/README.md:78`，"数据丢失 / 不可恢复"属 Critical。这里不是隐私泄漏，但是**不可恢复的行为性数据丢失**，归类偏保守方向可接受。

**C2 撤销路径上有一颗上了膛的枪。**
S7 的 `INSERT OR REPLACE` 只写 10 列，其余新列回落默认值（`:59-62`）：`scope_key=''` → 该行**脱离作用域**，`ListAsync`（`:176-177` 按 `scope_key` 过滤）与检索都看不见，但行还在表里；`fingerprint=''` → 逃掉唯一索引 `:115-116`（带 `WHERE fingerprint<>''`）→ 可产生重复；`origin='manual'`、`confidence=1` → **抽取来的记忆被洗成最高置信的人工记忆**。
今天它**不可达**（S1 已把这些 id 记墓碑，S2 把插入 `RAISE(IGNORE)` 掉），所以 `CoreTests.cs:174-179` 观察到"撤销后记忆仍为空"。一旦墓碑被清理、或撤销被实现为"先删墓碑再插入"，这颗枪就响。**必须在改 S4 的同时改 S7，不能只放开一边。**

**C3 多证据记忆被单条消息牵连，且反向漏删。**
S4 只按 `source_message_id` 选行，而一条记忆在 `memory_evidence` 里可以有多条证据（S8 的置信度就是按条数涨到 `.99`）：
- 删掉主证据消息 → 整条记忆连同**其余仍然存在的**佐证消息一起消失，并留下墓碑（C1）。
- 删掉佐证消息（非 `source_message_id`）→ 记忆行不动，但它的证据行被 S5 级联销毁，而 S8 只写回时才算数 → **置信度停在按旧条数算出的高值**。佐证越多的事实，删除后虚高越明显。
两个方向都错，且方向相反——说明"按 `source_message_id` 决定记忆生死"这个模型本身不成立。

**C4 删会话比删消息更狠。**
`:325` 对整会话执行 S4 → 该会话里学会的所有事实全部变成永久墓碑。用户为了省空间删掉旧会话，等于永久禁止角色再记住其中每一条。

---

## 2. 五种删除意图必须分开（本文的核心）

| 意图 | 触发者 | 现在的行为 | 应有效果 | 墓碑 |
|---|---|---|---|---|
| **D1 记忆删除** | 管理面"删除这条记忆"、`MemoryDatabase.cs:426`（`DeleteMemoryAsync`） | 删行 + 墓碑 | **不变**（说明书 §八：删后不得从 cache / index / 后台巩固复活） | 写 |
| **D2 作用域清空 / 遗忘** | "忘掉关于我的一切"、`SqliteMemoryRepository.cs:280`（`ClearAsync`） | 全删 + 墓碑 + 水位 | **不变** | 写 |
| **D3 消息删除** | 删一条 / 删该条及之后 | 记忆被删 + 墓碑（= D1 的效果） | 记忆**不删**；进入 §3 的孤立态；证据脱链并净化摘录 | **不写** |
| **D4 会话删除** | 删整个 Conversation | 同 D3 但覆盖全会话 | 同 D3；另提供显式"同时删除仅在本报会话学到的记忆"复选（用户主动要求时才落 D1） | 默认不写 |
| **D5 撤销（undo）** | `RestoreConversationMutationAsync`（`:580-617`） | 静默 no-op（C2） | 精确回到 D3 之前的状态 | 不读不写 |

D1/D2 与 D3/D4 的区别是**用户是否明确撤回了这个事实**。删聊天记录不等于撤回陈述；说明书 §八的"删除后不得复活"针对的是用户在记忆管理面删除的记忆，不是被清理的消息。

---

## 3. 目标设计：`orphaned`（证据孤立）中间态

### 3.1 状态机

```
                 ┌──────────────────────┐
                 │        active        │  ← 唯一可注入状态
                 └───┬──────────────┬───┘
     D1/D2 用户撤回   │              │  D3/D4 证据消息被删（且无剩余佐证）
      （删行+墓碑）    │              ▼
                 （终止）        ┌──────────────────┐
                                │     orphaned     │  不可注入、管理面可见
                                └───┬───────────┬──┘
                       D5 undo / 用户确认保留    │  用户在管理面删除
                                ▼               ▼
                            active         删行 + 墓碑（回到 D1 语义）

        既有：active ──被更晚断言取代──> superseded （不变，本文不动它）
```

`orphaned` 的语义是"角色仍然认为这是事实，但支撑它的原始消息已被用户删除，因此不再自动引用"。

### 3.2 不变量（实现与测试都必须守）

- **I1 注入资格**：只有 `status='active'` 可进上下文。不改检索代码，靠现有闸口保证（S9）。
- **I2 双重闸门**：进入 `orphaned` 时把 `confidence` 压到 `0.4`（**低于** `MemoryQueryService.cs:22` 的 `>= .5` 门）。这样即使有人误把 `status` 改回 `active`，置信门仍然挡住注入。孤立态必须是两个独立条件同时失效才能泄漏。
- **I3 墓碑只属于 D1/D2**：D3/D4 路径**不得**经过任何 `DELETE FROM memories`。这是 C1 的根修，也是本文最重要的一条。
- **I4 证据净化**：`orphaned` 记忆的 `memory_evidence.excerpt` 必须为空串（摘录是用户原话，删了消息却留着原话，等于把删除的内容换个表存起来）。
- **I5 无剩余佐证才孤立**：只要还有 ≥1 条证据消息未被删，记忆留在 `active`，但置信度按剩余条数重算（补齐 S8 的漏算，治 C3 的反向问题）。
- **I6 undo 精确且不外溢**：D5 只允许把**本次被该操作孤立**的记忆改回 `active`，不得复活任何 D1/D2 删除过的记忆（S2 触发器保持原样）。
- **I7 不动 `forget_before`**：D3/D4 只 `epoch+1` 并放弃输入含被删消息的 pending 任务，**不抬水位**。抬水位会连带水印掉所有历史消息，正是 C1 的一半。
- **I8 不新增第二套状态**：`superseded` / `archived`（`SqliteMemoryRepository.cs:112-114` 指纹去重用的归档态）语义不变；`orphaned` 只表达"证据缺失"。

### 3.3 为什么孤立态在四条既有链路上都是安全的

| 一致性面（契约 §7.1） | 孤立态下的表现 | 依据 |
|---|---|---|
| Repository | 行保留，`status` 变更 → 检索不可见 | S9 |
| 检索缓存 | `MemoryContext.Revision` 由 `epoch` 提供，`orphaned` 走 `epoch+1` → 旧缓存按 `stale_or_deleted` 丢弃 | 实现分支 `MemoryContextTests.cs:116-128` 已验证该机制 |
| 向量 / FTS 索引 | 语义打分只吃 `ListAsync` 过滤后的 `current` 集合，孤立行根本不进打分器 | `MemoryQueryService.cs:21-22,31` |
| Pending 队列 | 输入含被删消息的候选任务显式 `abandoned`；未放弃的会在 `ReadInputs` 少读一条后自然验证不过，fail-closed | `:388-395` |
| 后台 Worker | 无墓碑 → 未来重新陈述同一事实**可以**再次写入，指纹唯一索引去重 | `:115,416` |

关键：`orphaned` **不改检索代码**（S9 单点过滤）。改动全部集中在"哪些 SQL 允许删 `memories` 行"，即 S4 的三处。

---

## 4. 落地分两档，第一档不需要重建表

### 4.1 Stage 1（推荐；无 FK 重建）

`memory_evidence` 保持 `ON DELETE CASCADE`，把"证据集合"从数据库移到撤销快照里：

1. 新增 `DetachMemoriesForDeletedMessagesAsync(scope, messageIds)`，在**删除消息之前**的同一事务内：
   - 选出 `affected = 记忆行 WHERE source_message_id ∈ messageIds OR EXISTS(memory_evidence WHERE message_id ∈ messageIds)`（补 S4 只看 `source_message_id` 的漏，治 C3）；
   - 把每条 affected 记忆的**全部** `MemoryEntry` 列与全部证据三元组读进快照；
   - 按剩余佐证条数 `n'` 分类：`n' > 0` → 留 `active`，`confidence = MIN(.99, .9+.03·(n'−1))`；`n' == 0` → `status='orphaned'`、`confidence=.4`、`source_message_id=NULL`；
   - 显式 `DELETE FROM memory_evidence WHERE memory_id ∈ affected AND message_id ∈ messageIds`（抢在 CASCADE 之前，目的只是让我们知道删了什么；摘录净化由快照不外泄 + I4 保证）；
   - 执行一次 `Fence` 等价的 `epoch+1` + pending `abandoned`（复用 `SqliteMemoryRepository.cs:258-264`，**`reason='evidence_detached'`，且不写 `forget_before`**，见 I7）。
2. 把 `MemoryDatabase.cs:325,558,586` 三处 `DELETE FROM memories ...` 全部替换为第 1 步的调用。**这三处是 C1/C4 的全部来源。**
3. `ConversationMutationSnapshot` 扩型：携带完整列 + 证据三元组；`RestoreConversationMutationAsync` 改为**按列显式 UPDATE/INSERT**（不再用 `INSERT OR REPLACE`，见 §5），把本次孤立的记忆逐条回置 `active` 与原 `confidence`，并按快照重建 `memory_evidence`（含 `message_id`，此时消息已被同事务恢复，外键成立）。
4. 重启后 undo 不可用（快照在内存/调用方，**这是既有约束，对消息同样成立**，本文不扩大范围）：此时记忆停在 `orphaned`，是**可接受的降级**——数据没丢、用户可以看见并确认保留（§6）。

Stage 1 的代价：跨重启的 undo 不能重建证据行（摘录净化后只能靠"证据消息已删除"这一状态描述）。**不建议**在 1.0 为它付出重建表的代价。

### 4.2 Stage 2（明确延期，不是本轮）

把 `memory_evidence` 改成 `message_id INTEGER NULL REFERENCES messages(id) ON DELETE SET NULL`，加 `state TEXT NOT NULL DEFAULT 'attached'` 与 `detached_from INTEGER NULL`，使证据脱离**由外键自动完成、可持久、可审计**，从而支持跨重启 undo 与"该记忆的证据已被删除"的精确展示。
成本：SQLite 不能 `ALTER` 外键，必须建新表 + 拷贝 + drop + rename（`memory_evidence` 不被任何表引用，重建是安全的），并处理迁移事务内的 CASCADE 语义。
**触发条件**：只有当"跨重启撤销消息删除"或"按证据逐条展示删除历史"成为产品需求时才做。

---

## 5. 撤销路径的具体改法（C2）

`:604` 的 `INSERT OR REPLACE INTO memories(id, 10 列)` 必须废弃，理由见 C2。改为：

```
-- 记忆行不再被删除，所以 undo 绝大多数情况下只是回置状态
UPDATE memories SET status=$status_before, confidence=$conf_before, source_message_id=$src,
       updated_at=$upd, last_used_at=$used, state_version=state_version+1
 WHERE id=$id AND scope_key=$scope AND status='orphaned';
-- 证据整表重建前先清该记忆的证据，避免与残留行撞主键
DELETE FROM memory_evidence WHERE memory_id=$id;
INSERT INTO memory_evidence(memory_id,message_id,conversation_id,excerpt) VALUES(...);
```

要点：
- 必须带 `status='orphaned'` 条件（I6）：`id` 存在但状态不是本次造成的孤立态时**不覆盖**，静默跳过并记原因。
- 必须带 `scope_key` 条件：C2 说明 `scope_key` 是这行的可见性命脉，任何恢复写都要按作用域限定，与 `:270,280` 的写法一致。
- `state_version+1` 保持现有乐观并发语义（他人 worktree 的 `CharacterIntelligence` 状态仓储依赖"版本落后即拒写"这一类断言）。
- **不改 S2 触发器**。撤销不再需要绕过它，因为不再有墓碑产生。这是本文与"给 undo 开后门"方案的差别——后门会削弱 D1 的防复活保证。
- `messages` 的恢复维持 `INSERT OR REPLACE`（`:596`）：消息表没有墓碑触发器，行为不变。

---

## 6. 管理面与 API（`AGENTS.md` 规则 5：先改 `docs/api-v1.md`）

用户必须能区分"我删了记忆"和"我删了消息所以这条不再被引用"。需要：

| 能力 | 语义 | 需要的契约变更 |
|---|---|---|
| 列出孤立记忆 | 管理面新增筛选/分组，标注"证据消息已删除" | `MemoryRecord` 需携带 `status`/`confidence`/`origin`——**现有 DTO 没有 `status`**，必须扩（同时影响 `ListMemoriesAsync` 返回形状） |
| 确认保留 | 用户主动把 `orphaned → active`，`confidence` 上限 `.7`、`origin='manual'`、不重建证据 | 新端点；错误语义：非孤立态返回 `409`，不静默成功 |
| 删除 | 走 D1，写墓碑（此后同指纹不可再学，需在 UI 明确二次确认） | 复用现有删除端点，语义不变 |
| 会话删除选项 | "同时删除仅在本报会话学到的记忆"复选框（默认不勾） | 会话删除端点加一个可选布尔参数 + DTO 验证 |

一条必须同时修的既有缺陷（不在本文范围内但会被本文暴露）：`DeleteMemoryAsync`（`MemoryDatabase.cs:426`）与 `ListMemoriesAsync`（`:385`）都硬限定 `scope_key='character:'||$c`，**只能触达角色作用域**。RT-001 提供稳定实例 id 后，实例作用域（`instance:*`，见 `MemoryContextTests.cs:142-155`）里的孤立记忆将无法在管理面被看到或确认——`orphaned` 会让这个"看不见"从隐私特性变成可用性缺陷。要求：管理面统一改走 `IMemoryRepository`（作用域感知），或至少让 legacy 端点接受 `scopeKey` 参数。归属 MEM-005 实现 Owner + UI。

---

## 7. 迁移与回滚

- 组件版本 `memory_schema` 从 `1` 升到 `2`（`SqliteMemoryRepository.cs:65-66,99-100` 的未知版本拒绝启动逻辑照现有模式扩展，**不要**动全局 `schema_info` v8——契约 §15.1 R2）。
- 迁移内容：Stage 1 只做 `status` 取值扩展（`TEXT`，无约束改动）、新增一条按 `(scope_key,status)` 已存在的索引（`:97`）即可，**不新增列**。这是选 Stage 1 的主要理由。
- 回滚：沿用 R2 已推翻乐观结论的那套现实——升级后的库不能交给旧二进制（旧代码不认识 `orphaned`，会把它当 `active` 注入）。所以**回滚 = 停宿主 + 平台备份 + 恢复备份 + 切旧二进制**，不是"旧代码忽略新值"。备份/恢复归 PLAT。
- 迁移期一次性动作：无历史 `orphaned` 行需要合成（本变更只影响今后的删除行为）。**不要**回填或"顺手清理"既有墓碑——那会改变 D1 的历史保证。

---

## 8. 验收：新增用例 + 一个必须加强的既有断言

新增（放 `tests/Pry.Core.Tests/`，沿用确定性替身与虚拟时钟）：

| # | 用例 | 关键断言 |
|---|---|---|
| N1 | 删除唯一证据消息 | 记忆行仍在；`status='orphaned'`；`confidence<.5`；**`memory_tombstones` 为空** |
| N2 | 孤立后重新陈述同一事实 | 再次写入成功（证伪 C1，锁死 I3）；`ListAsync` 只有一条 active |
| N3 | 三条证据删一条 | 留 `active`；`confidence` 按剩余 2 条重算为 `.93`（锁死 I5 / 治 C3） |
| N4 | 删主证据但另一佐证存活 | 记忆**不**孤立（当前会整条消失且留墓碑） |
| N5 | undo 后 | `status/confidence/证据三元组`与删除前逐字段相等；`INSERT OR REPLACE` 已不再使用（锁死 C2） |
| N6 | undo 不得复活 D1 删除的记忆 | 用户先删记忆、再删消息、再 undo → 仍为空（S2 保证未被削弱） |
| N7 | 删整个会话（未勾选级联） | 该会话学到的记忆全部 `orphaned`，零墓碑（治 C4） |
| N8 | 勾选级联删除 | 只有"证据全部来自本报会话"的记忆被 D1 删除；跨会话佐证的不得消失 |
| N9 | `orphaned` 不进语义打分器 | 用计数替身 `IMemorySemanticSignal` 断言收到的集合里没有孤立行（S9 单点过滤的护栏） |
| N10 | pending 任务含被删输入 | 任务 `abandoned`、`forget_before` **未变**（锁死 I7）；undo 后新陈述仍可写入 |
| N11 | 实例作用域可见性 | 实例作用域的孤立记忆能被管理面查询路径取到（暴露 §6 末的 scope 缺陷） |

必须加强、但**结论方向不变**的现有断言：`CoreTests.cs:174-179` 现在写 `Assert.Empty(await db.ListMemoriesAsync(...))`。`ListMemoriesAsync` 带 `status='active'` 过滤（S9），所以在 `orphaned` 设计下它**依然通过**——好消息是本设计不翻转任何既有断言。但它现在会同时被"记忆被删了"这个 bug 满足，因此必须追加一条 `orphaned` 行仍存在的断言，否则这条防复活用例已经失去判别力。`MemoryContextTests.cs:116-128`（D1 防复活）应保持原样通过，作为 N6 的对照。

若任一 N 用例失败，本文视为未实现；不接受"先合并后补测试"。

---

## 9. 分期与非目标

| 阶段 | 内容 | 依赖 |
|---|---|---|
| E1 | `DetachMemoriesForDeletedMessagesAsync` + 替换三处 `DELETE FROM memories`（`:325,558,586`） | 本文 I1–I8 |
| E2 | 快照扩型 + 撤销改法（§5） | E1 |
| E3 | 管理面/DTO/API + `docs/api-v1.md` 先行 | E1；§6 末的作用域缺陷同批 |
| E4 | N1–N11 与 `CoreTests` 断言加强 | E1–E3 |
| E5（延期） | Stage 2 证据表 FK 重建 | 仅当出现跨重启 undo 需求 |

**非目标**：不做逐条证据的保留期策略、不做审计日志表、不做"从备份恢复后墓碑失效"（归 PLAT 的备份语义）、不给 `orphaned` 加自动过期回收（那是 MEM-005 §5 巩固的范围，且会削弱 I6 的可确认性）。

---

## 10. 需要项目负责人裁定的三点（我不代答）

| # | 决策 | 我的建议 | 不裁定的后果 |
|---|---|---|---|
| **P1** | D3 是否真的**不**撤回事实？（即本文全部前提） | 不撤回：删聊天记录 ≠ 撤回陈述 | 若裁定"撤回"，则 C1 是预期行为，本文作废，但必须把 §2 的墓碑语义在 UI 上写清楚并加二次确认 |
| **P2** | 孤立记忆的摘录是否必须净化（I4） | 必须：删掉的消息原文不得以摘录形式留存 | 保留摘录 = 删除语义不完整，属隐私缺口 |
| **P3** | 会话删除是否默认级联删记忆（D4） | 默认不级联，提供显式复选 | 默认级联 = 一次清理动作造成永久不可学 |

另外一条提请注意（与本文同源，但属实现方）：D1/D2 与 D3/D4 现在共用 `memory_deletion_fence` 一个触发器承担四种语义。若不采纳本文的 `orphaned`，**至少**要把触发器拆成"仅用户意图墓碑"，否则 C1 与 C4 在任何版本下都存在。这条是底线修复，独立于本文其余部分。

## 11. 关联
契约 §7.1 / §7.2 / §7.3、§15.1 R3、§15.3 未闭合接缝；`docs/memory-recall-calibration.md` §3 集 B 的 B6（已删/已孤立/跨作用域不得进 `Hits`）；`docs/eval/README.md:72,78`（复活与不可恢复的 Critical 归类）；MEM-005 台账。
