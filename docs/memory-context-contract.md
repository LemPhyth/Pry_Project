# Memory 与 Context 契约设计（MEM-005 评审稿）

状态：设计已交付；MEM-005 实现由 Codex 在 `codex/memory-context` 接手（未提交、待集成）。本文仍是契约与不变量的真相源，
Owner 裁定与实现对照见 §15。负责人：Qoder（Memory & Context）。

日期：2026-09-20。
承接：MEM-004（Embedding 混合检索）。前置于：RT-005（Phase 5 Long-term Memory）。
本文只定义边界、数据与契约，不代表任何功能已经实现。

## 0. 基线事实（评审前必读）

本文写作时核对了仓库现状，两条事实会影响结论，先摆在最前面：

1. **MEM-004 的代码不在版本控制里。** `memory_embeddings` 表、`MemoryEmbeddingService`、
   `MemoryEmbeddingController`、`OpenAiCompatibleEmbeddingProvider` 均为未跟踪或他人未提交改动；
   `git log --all -S "memory_embeddings"` 返回 0 条提交，`HEAD` 中的 `MemoryDatabase.cs` 不含该表。
   因此本文**不把向量检索当作可依赖的既成接口**，而是把语义通道标为"可选信号 + 待合流"（见 §6.4）。
2. **不存在 CharacterInstance 与持久化 Relationship。** 领域模型只有 `CharacterDefinition.Id`
   （`DomainModels.cs:7-19`）和随角色定义的 `RuntimeState InitialState`（同文件 `:27-34`，含 Trust/Familiarity，
   未持久化、未随会话推进）。作用域与关系相关性设计必须显式声明对 RT-001（稳定 ID）和 RT-003（关系状态）的依赖，
   不能假装它们已存在（见 §2.3、§6.3）。
   **更正（§15.5）**：本条对"Memory 无法表达实例作用域"的判断已被实现推翻——Core 层接受宿主提供的
   `instance:` 作用域键并有断言，不需要先造产品级实例实体。持久化 Relationship 的缺失仍然成立。

### 0.1 更新（2026-09-20 复核，MEM-005 实施已由 Codex 接手）

3. **MEM-005 的实现同样不在版本控制里。** `codex/memory-context` worktree（`.local-notes/memory-context`）
   有 13 个新文件 + 2 个修改文件未提交，该分支无自有提交（`git log` 与 `3613a2a` 相同）。
   连同 MEM-004 与主线 RT-002/003/004 的在途改动，**长期记忆的地基目前分散在多个未提交工作区**；
   主线工作区在途项已达 109。`MemoryDatabase.cs` 现在在两个 worktree 各自演化，合并冲突面显著。
4. **`MemoryQueryOutcome` 出现两份声明**：`Memory/MemoryContracts.cs:65`（实施分支）与
   `Intelligence/SituationSnapshot.cs:10`（主线）。后者已在注释中声明归属 Memory、待 `MemoryContext` 落地即塌缩，
   缺的只是归属裁定，见 §15.1 裁定 R1。

## 1. 目标与非目标

目标：让 Pry 记住**值得记、可追溯、能被用户更正和清除**的长期事实，并在上下文预算内**少而准**地取回。

非目标（本设计与 1.0 均不做）：

- 不把聊天记录全量向量化后统一 RAG。
- 不建知识图谱、entity/relation 抽取、多跳推理。
- 不把 BehaviorPattern / ExpressionPattern 塞进 `memories` 表（§2.2）。
- 不引入向量索引扩展（sqlite-vec 等）、云同步、多租户用户体系（计划 §2.4 已冻结）。
- 不实现 Runtime Scheduler、通用 Agent Harness、完整 Behavior Planner。
- 不让记忆模块写角色状态、发消息或决定何时回复。

## 2. 分层与归属

### 2.1 六个层不是同一张表

| 层 | 生命周期 | 归属 | 存储 | Memory 职责 |
|---|---|---|---|---|
| Working Context | 单次模型调用 | ContextBuilder | 无 | 组装与预算 |
| Conversation History | 会话内 | Conversation | `messages` | 提供短期证据源，不复制内容 |
| Long-term Memory | 跨会话持久 | **Memory** | `memories` 等 | 全部 |
| Relationship State | 跨会话持久 | RT-003 | 角色实例状态 | 仅检索相关性输入 |
| BehaviorPattern | 跨会话持久 | RT-003/006 | 独立存储 | 提供检索基础设施 |
| ExpressionPattern | 跨会话持久 | RT-004 | 独立存储 | 同上 |

判据：一条抽取结果若描述"**以后如何行动/措辞**"，属于 Behavior/Expression，不进 `memories`；
若描述"**世界/用户是什么样**"，才是 Memory。抽取阶段用 `kind` 分流，边界在 §5.3 的抽取契约里强制。

### 2.2 Memory 拥有的表

`memories`、`memory_evidence`、`memory_tombstones`、`memory_candidates`，以及 MEM-004 的
`memory_embeddings`（由 Embedding 侧写入，本文只读它的可用性）。Behavior/Expression 表由各自 Owner 定义。

### 2.3 作用域键

新增 `scope_key`（不透明字符串）。1.0 单用户本地场景下取值为 `character:{characterId}`。
当 RT-001 冻结稳定 ID、RT-003 引入持久化角色实例后，私密关系类记忆可改用 `instance:{characterInstanceId}`。

诚实边界（**已由实现部分推翻，见 §15.5**）：本段原判断"实例隔离测试今天无法通过"不成立。
Core 层 `MemoryScope("instance:bob-pry", "pry")` 已能隔离同角色下的两个实例并有测试断言（含跨实例不共享、
一会话不得绑两作用域、伪造跨作用域提交被拒）；仍然成立的部分是**产品链路未接线**——
HTTP/聊天适配只用 `character:{id}`，稳定实例 ID 由平台（RT-001）供应，Memory 不自行推导用户身份。
本设计不提前造用户体系这一点不变。

## 3. 数据模型（Memory 组件版本 `memory_schema=1`）

> 版本裁定（2026-09-20，见 §15.1 R2）：本节原写"schema v7 → v8"**作废**。全局 `schema_info` 已被
> RT-002 的运行时表占用为 8（`MemoryDatabase.cs:133`），多个 Owner 同时认领 v8 会造成迁移编号竞争。
> Memory 改用组件自版本表 `memory_schema(version)`，`schema_info` 的推进权归平台/RT-001 的迁移注册表。

现状 `memories` 列：`id, character_id, kind, summary, tags, importance, source_message_id, created_at,
updated_at, last_used_at`（`MemoryDatabase.cs:68-79, 125-126`）。以下为增量，全部走现有
`EnsureColumnAsync` 追加路径，不重建表、不搬数据。

```sql
-- 3.1 memories 扩展（每列独立 ADD，NULL 检查见 §3.5）
ALTER TABLE memories ADD COLUMN scope_key       TEXT    NOT NULL DEFAULT '';
ALTER TABLE memories ADD COLUMN status          TEXT    NOT NULL DEFAULT 'active';   -- active|superseded|archived|deleted
ALTER TABLE memories ADD COLUMN confidence      REAL    NOT NULL DEFAULT 0.5;        -- 0..1，来源见 §4.3
ALTER TABLE memories ADD COLUMN fingerprint     TEXT    NOT NULL DEFAULT '';          -- 归一化正文哈希，用于同文去重
ALTER TABLE memories ADD COLUMN topic_key       TEXT    NOT NULL DEFAULT '';          -- 主题槽，用于更正/替换匹配
ALTER TABLE memories ADD COLUMN origin          TEXT    NOT NULL DEFAULT 'manual';    -- manual|extraction|correction
ALTER TABLE memories ADD COLUMN supersedes_id   INTEGER NULL;                         -- -> memories(id) ON DELETE SET NULL
ALTER TABLE memories ADD COLUMN state_version   INTEGER NOT NULL DEFAULT 1;
ALTER TABLE memories ADD COLUMN deleted_at      TEXT    NULL;
```

```sql
-- 3.2 证据：一条记忆多条来源（说明书 §四）
CREATE TABLE IF NOT EXISTS memory_evidence(
  id                 INTEGER PRIMARY KEY AUTOINCREMENT,
  memory_id          INTEGER NOT NULL,
  message_id         INTEGER NULL,
  source_role        TEXT    NOT NULL,                 -- user|assistant|system（写入时快照）
  attribution        TEXT    NOT NULL DEFAULT 'first_person', -- first_person|confirmed|third_party|quoted_material
  excerpt            TEXT    NOT NULL,                 -- 有界原文摘要，上限 280 字符
  excerpt_created_at TEXT    NOT NULL,
  ordinal            INTEGER NOT NULL,
  UNIQUE(memory_id, ordinal),
  FOREIGN KEY(memory_id)  REFERENCES memories(id) ON DELETE CASCADE,
  FOREIGN KEY(message_id) REFERENCES messages(id) ON DELETE SET NULL
);
CREATE INDEX IF NOT EXISTS ix_memory_evidence_message ON memory_evidence(message_id);
CREATE INDEX IF NOT EXISTS ix_memory_evidence_memory  ON memory_evidence(memory_id);
```

```sql
-- 3.3 去重与删除一致性
CREATE UNIQUE INDEX IF NOT EXISTS ux_memories_active_fingerprint
  ON memories(scope_key, fingerprint) WHERE status='active' AND fingerprint<>'';
CREATE INDEX IF NOT EXISTS ix_memories_scope_status ON memories(scope_key, status, importance DESC);

CREATE TABLE IF NOT EXISTS memory_tombstones(
  memory_id  INTEGER PRIMARY KEY,
  scope_key  TEXT    NOT NULL,
  fingerprint TEXT   NOT NULL DEFAULT '',
  topic_key  TEXT    NOT NULL DEFAULT '',
  deleted_at TEXT    NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_tombstones_lookup ON memory_tombstones(scope_key, fingerprint);
```

墓碑**只存标识，不存正文**。理由：用户删除后正文不得保留（隐私），但抽取任务必须能认出"这条已被删过"从而不再写回（防复活）。
只靠 `status='deleted'` 无法同时满足两者；只靠硬删除则无法防复活。

```sql
-- 3.4 异步写回队列（说明书 §五）
CREATE TABLE IF NOT EXISTS memory_candidates(
  id                INTEGER PRIMARY KEY AUTOINCREMENT,
  turn_id           TEXT    NOT NULL,
  job_kind          TEXT    NOT NULL,                 -- consolidation|manual_rescan
  scope_key         TEXT    NOT NULL,
  conversation_id   TEXT    NOT NULL,
  character_id      TEXT    NOT NULL,
  input_fingerprint TEXT    NOT NULL,                 -- 本批输入消息 ID 集合的哈希
  payload_json      TEXT    NOT NULL,                 -- 有界输入快照（≤16 条消息，单条截断）
  state             TEXT    NOT NULL DEFAULT 'pending', -- pending|claimed|succeeded|failed|abandoned|superseded
  attempt           INTEGER NOT NULL DEFAULT 0,
  max_attempts      INTEGER NOT NULL DEFAULT 3,
  available_at      TEXT    NOT NULL,                 -- 退避窗口
  claimed_at        TEXT    NULL,
  lease_expires_at  TEXT    NULL,                     -- 崩溃回收
  last_error        TEXT    NULL,
  created_at        TEXT    NOT NULL,
  updated_at        TEXT    NOT NULL,
  UNIQUE(turn_id, job_kind, input_fingerprint)         -- 幂等：重复提交=重放
);
CREATE INDEX IF NOT EXISTS ix_candidates_dispatch ON memory_candidates(state, available_at, id);
```

SQLite 的 `UNIQUE` 允许多个 NULL，因此 `turn_id` 与 `input_fingerprint` 必须 NOT NULL，空值以 `''` 表示并由 Repository 拒绝。

### 3.5 迁移与回滚

- 可重复执行：全部 `IF NOT EXISTS` / `EnsureColumnAsync`；组件版本写 `memory_schema(version)`，**不动** `schema_info`。
  读到未知组件版本必须拒绝启动，不得半迁移运行。
- 迁移内一次性回填：`scope_key = 'character:' || character_id`；`fingerprint` 按 §4.1 归一化算法对存量 `summary||tags` 计算。
- 回填冲突处理：存量若已有同 `(scope_key, fingerprint)` 重复行，**保留最小 id 为 active，其余标 `archived`**，不删数据，写日志计数。
- NULL/索引/外键检查（计划 §6 要求）：迁移尾部执行 `PRAGMA foreign_key_check` 与 `integrity_check`，非空结果视为失败并中止。
- **回滚**：本段原判断（"旧版本代码忽略新列即可读写"）**已被 §15.1 R2 推翻**，以更严格者为准：
  升级后的库**不能**交给旧二进制——旧查询不识别 `superseded`，旧写入不维护 `scope_key`。
  安全回滚 = 停宿主 + 平台备份 + 恢复备份并切回旧二进制。

## 4. Evidence First

### 4.1 指纹与归一化

`fingerprint = sha256(kind + "|" + normalize(summary))` 前 32 位。`normalize` = Trim + 折叠空白 + 转小写 +
去除末尾中英文句号。**不做语义归一化**（近义改写交给 §5.4 的合并步骤处理），因为归一化越强，误合并越难排查。

### 4.2 证据优先级（谁能成为"用户事实"）

| attribution | 可否写入用户偏好/个人事实 | 说明 |
|---|---|---|
| `first_person` | 可 | 用户第一人称明确陈述，最高权重 |
| `confirmed` | 可 | 用户对角色复述表示认可（"对，就是这样"） |
| `third_party` | **不可** | 他人说法，上下文只用于消歧 |
| `quoted_material` | **不可** | 附件、图片描述、引用文本内的主张 |

两条硬规则：

1. **assistant 消息永不作为用户事实证据。** 否则模型的自我发挥会被记成用户事实，并在后续轮次作为事实回灌，形成自我强化循环——这是本设计针对的最大误召回来源。
2. **引用材料不得写入记忆。** 附件正文与图片描述当前已被明确要求当作引用资料（`ReplyPlanner.cs` 附件指令）。若抽取器无差别读取，一段被转发的文本就能把持久指令注入长期记忆，构成长期化的提示注入。抽取输入必须按来源类型排除这两类。

### 4.3 confidence 的可解释构成

confidence 不是模型自评，而是规则加分，抽取模型给出的 `self_rating` 只能作为 ≤0.6 的弱信号参与：

- 第一人称明确陈述：0.9 起点
- 同一 `topic_key` 独立证据条数：每条 +0.03，上限 +0.09
- 用户纠正（`origin='correction'`）：1.0
- 仅由上下文推断、无第一人称证据：≤0.45，且 `status` 只能到 `archived` 之外的最低可注入档（§6.5）
- 被用户删除后再次抽取：直接拒绝（§7.2）

`importance` 保持现有用户可编辑语义（UI 与 API 已暴露，`MemoryApplicationService.cs:23`），
不被 confidence 取代：importance = "对用户多重要"，confidence = "我们多确定这是真的"。

### 4.4 `kind` 沿用现有取值

现网取值 `user_fact, preference, promise, relationship, event, other`（`MemoryManagerDialog.cs:30, 161-165`），
与说明书五类高度重合。**不做术语清洗**，避免让用户已录入的数据变成孤儿分类：

| 说明书类型 | 落库 kind |
|---|---|
| Semantic | `user_fact` |
| Preference | `preference` |
| Relationship | `relationship` |
| Episodic | `event` |
| PersonalContext | `personal_context`（新增，`user_fact` 不降级搬迁） |
| —（Pry 既有的约定语义） | `promise`, `other` 保留 |

## 5. 异步写回与巩固

### 5.1 链路

```
Turn 投递完成（只有成功投递的消息参与）
  → Repository 在写消息的同一事务内入队 memory_candidates（保证至少一次；入队与消息落库不可分离）
  → 后台 Worker 按 §5.2 触发条件认领（带租约）
  → 候选抽取（模型，单次调用处理一批消息，不是每条一次）
  → 证据校验（§4.2 规则 + §4.3 计分，纯代码，不再问模型）
  → 去重 / 合并 / 更正（§5.4）
  → 墓碑检查 → 落库（status=active）
  → Embedding 由 MEM-004 的索引循环补建（不在本链路内调用）
```

回复链路**不等待**上述任何一步。Memory 不可用时聊天照常工作（计划 §2.5、架构计划 §34）。

### 5.2 触发条件（Nekro 式调度，非每消息触发）

满足任一即入队巩固任务；同 `(turn_id, input_fingerprint)` 去重：

- 自上次巩固起新增未处理 user 消息 ≥ 8 条；或
- 距上次成功巩固 ≥ 20 分钟且有未处理消息；或
- 会话被切换走 / 关闭 / 进程空闲；或
- 命中高信号标记（第一人称偏好、纠正、承诺、关系陈述）→ 立即入队，不等批量。

反压：`pending + claimed > 256` 时只接收高信号任务，其余直接标 `abandoned` 并计数告警——
宁可暂时不学，不可让队列拖垮磁盘与模型时间。水位值实现时进配置，不硬编码在业务分支里。

### 5.3 抽取契约与分流

一次抽取的输入是**已完成 Turn 的真实输入 + 实际投递输出**（不是候选/被取消的草稿，与计划 §2.5"只有成功投递的内容进入学习"一致）。
输出为结构化候选，每条必须带：`kind`、`summary`、`topic_key`、`evidence_message_ids`、`self_rating`。
`kind ∈ {behavior, expression}` 的候选**转交对应 Owner 的入口，不落 `memories`**（§2.1 判据）。
没有 Behavior/Expression 存储时（RT-003/004 未完成）此类候选丢弃并计数，不塞进记忆表将就。

### 5.4 去重、合并、更正

- 同 `(scope_key, fingerprint)` 已 active → 不新建；改为向既有条目**追加证据**并重算 confidence。
- 同 `topic_key` 不同内容 → 视为更正候选，需满足 §5.5 才生效。
- 近似重复但未撞指纹（"喜欢后摇" / "爱听后摇"）：接受其存在，由后续人工管理或合并策略处理。
  **不引入自动语义合并**：阈值合并造成的静默错合比多一条记忆更难发现，属于本设计明确规避的复杂度。
- `event`（情景记忆）默认**不参与 `topic_key` 更正**——发生过的事不被"更正"掉，只可被删除。

### 5.5 更正与替换

新条目 `origin='correction'`、`supersedes_id = 旧 id`；旧条目同事务内置 `status='superseded'`、`state_version+1`、
写 `memory_tombstones`？—— **否**。supersede 不是删除：写墓碑会让"用户看到过又被更正"的条目永久屏蔽再抽取。
更正的墓碑规则见 §7.2，仅 `deleted` 进入墓碑。

同一事务完成，避免出现两条 active 的窗口；`ux_memories_active_fingerprint` 只挡同文，`topic_key` 层的双活由事务内规则保证。

### 5.6 幂等、重试、崩溃恢复、陈旧任务

| 风险 | 机制 |
|---|---|
| 重复提交 | `UNIQUE(turn_id, job_kind, input_fingerprint)`，冲突即 `Replayed`，不报错 |
| 重复写入记忆 | 抽取阶段先查指纹与 topic，落库阶段撞唯一索引则转合并路径 |
| Worker 崩溃 | `lease_expires_at`；启动时把过期 `claimed` 重置为 `pending`（attempt 不变则重试会重复，见下） |
| 部分成功 | 落库与状态迁移在同一事务；崩溃后重放同一 job 由指纹去重兜底 |
| 重试次数 | `attempt >= max_attempts` → `failed`，不再自动调度，仅状态接口可见 |
| 陈旧任务 | job 记录提交时水位；执行前比对"该会话是否已有更新消息/记忆是否被删"，不满足则 `abandoned` |
| 取消 | 进程停止（`StopAsync`）与配置变更（Embedding/角色重配）取消在途 job |

每次尝试都 `attempt+1` 并延长 `available_at`（指数退避：30s、2m、10m）。
副作用重复由"先查指纹"与唯一索引消除，不依赖 job 恰好只执行一次。

## 6. 检索

### 6.1 信号组合

```
score = 0.30·semantic + 0.20·keyword + 0.20·recency + 0.15·importance
      + 0.10·conversationRelevance + 0.05·confidence
```

- semantic：现 MEM-004 余弦；归一到 [0,1]。
- keyword：现 `SearchMemoriesAsync` 的 LIKE/FTS 命中序；实现期把 LIKE 换为已有 FTS5（`memory_fts` 已建但当前检索未使用它——这是一个可直接修正的既有一致性问题）。
- recency：`exp(-Δt/halfLife(kind))`，`Δt` 取 `max(created_at, updated_at, last_used_at)`。
  半衰期分型：`event` 14 天、`relationship` 90 天、`preference/user_fact/personal_context/promise` 365 天、`other` 90 天。
  现有实现只用 `last_used_at` 打点、**不参与排序**（`MemoryEmbeddingService.cs:98-106`），本设计补上。
- importance：用户与抽取共同维护的先验。
- relationshipRelevance：权重先置 0，接口位保留，RT-003 持久化关系后启用（§0 事实 2）。
- confidence：低置信条目即使语义相近也进不了 Top-K。

### 6.2 少而准

- `count` 上限 6（沿用现状），且 `score < 0.35` 一律丢弃。
  **语义检索必须有绝对阈值**：向量 Top-1 永远存在，无阈值就是把"最不离谱的一条"当成"相关"，这是误召回污染主聊天的主要成因。
- 同 `topic_key` 只保留最高分一条，避免三条同主题记忆吃满预算。
- 每条注入文本长度上限 220 字符（超限截断并标注），防止单条长记忆吞掉分区预算。

### 6.3 跨会话与隔离

- 长期记忆按 `scope_key` 共享（同一角色实例跨会话一致）。
- 检索**永不**返回另一会话的 `messages`；短期历史只由 ConversationRuntime 提供（说明书 §十一 A/B 隔离）。
- 现有正确之处保持：`SearchMemoriesAsync` 已按 `character_id` 过滤（`MemoryDatabase.cs:671`），迁移后改为按 `scope_key`。

### 6.4 降级与失败语义（区分"没有"与"查不到"）

```csharp
public enum MemoryQueryOutcome { Ok, DegradedKeywordOnly, Empty, Unavailable, Rejected }
```

| Outcome | 含义 | ContextBuilder 行为 |
|---|---|---|
| `Ok` | 正常，含命中为空但流程完整 | 注入 |
| `DegradedKeywordOnly` | 向量通道不可用，仅关键词 | 注入 + trace 记降级 |
| `Empty` | 确实没有相关记忆 | 留空，**不得**表述为"用户没有历史" |
| `Unavailable` | 存储/检索失败 | 整段不注入，禁止编造 |
| `Rejected` | 预算不足未取用 | 不注入，记 trace |

当前实现把 Embedding 异常直接吞成关键词结果（`MemoryEmbeddingService.cs:110-115`），调用方无法区分——这是 §2.5 要修的点。

### 6.5 注入资格门

`confidence < 0.5` 且 `origin='extraction'` 的条目不进入模型上下文，只在管理 UI 可见（可被用户确认或删）。
未确认的推断值得被看见，不值得被当作事实说出口。

## 7. 删除与更正的一致性（说明书 §八）

### 7.1 一致性面清单与现状

| 面 | 现状 | 目标 |
|---|---|---|
| Repository | 硬 `DELETE`（`MemoryDatabase.cs:584-591`） | 事务内：删行 + 写墓碑 |
| 向量 | `memory_embeddings` FK CASCADE，已正确（`:88`） | 保持；重建索引时跳过墓碑指纹 |
| 全文 | `memories_ad` 触发器已正确清理 `memory_fts`（`:112-114`） | 保持 |
| 进程内缓存 | 当前无（每次查库） | 若引入必须随删除失效，且不得先于墓碑检查 |
| 待办队列 | 不存在 | 删除时把引用该记忆的 `pending` job 标 `superseded` |
| 在途任务 | 不存在 | job 携带 `notBeforeWatermark`；落库前重查墓碑 |

### 7.2 复活屏蔽（两条都要挡）

抽取落库前在同一事务内检查：

1. `memory_tombstones.memory_id = 目标 id` → 拒绝（防 `INSERT OR REPLACE` 式复活，现有恢复路径
   `RestoreConversationMutationAsync` 正是 `INSERT OR REPLACE`，见 `MemoryDatabase.cs:891`）。
2. `(scope_key, fingerprint)` 命中墓碑 → 拒绝同文再抽取。
3. `topic_key` 命中墓碑 → 只有 `origin='correction'` 或 `attribution='first_person'` 且 confidence ≥ 0.9 才允许重新建立。
   即：用户删掉"我喜欢后摇"后，模型再次自动抽取不能把它写回来；用户**自己明说**可以。

### 7.3 消息删除与记忆的分离（行为变更，需评审）

现状：删除消息/会话会**连带删除**以其为来源的记忆
（`MemoryDatabase.cs:473`、`:844`、`:872`）。这与"跨会话共享长期记忆"冲突——用户删掉一个旧会话，
不该同时抹掉角色对事实的记得。

建议改为：记忆保留，证据的 `message_id` 置 NULL，**同时清空该证据 `excerpt`**（隐私优先：用户删原文就意味不想留该内容），
并保留 `source_role`/时间戳供管理 UI 标注"来源已删除"。
影响面：撤销/重生成路径（`ConversationMutationSnapshot` 携带 `Memories` 做快照恢复）必须同步复核，
否则撤销一次删除会绕过 §7.2 复活被删记忆。**这是本设计中风险最高的一条改动，单独评审、单独提交。**

> 结案（2026-09-20，裁定 R3）：**本轮不改**，维持"删消息连带删记忆"的现状。实施方在保留该行为的前提下加了墓碑，
> 使撤销聊天删除只恢复聊天、不再复活记忆——已覆盖本条最关键的安全风险（防复活）。
> 本建议（保留派生事实 + 清空 excerpt）转 MEM-006 独立评审，不与记忆地基捆绑落地。

> **更正（2026-09-21，MEM-006 设计复核）**：上面"已覆盖最关键安全风险"的判断**只对了一半**。墓碑确实防住了复活，
> 但它与用户删除记忆共用同一个 `BEFORE DELETE` 触发器，于是"删一条聊天消息"也会写下**永久指纹墓碑**，
> 使该事实在该作用域内**永远无法再次被学会**——用户后来重新说一遍也没用。设计稿当时未预见这条副作用。
> 完整链路、后果分级与分阶段方案见 `docs/memory-message-deletion-separation.md`（其 §1.1 C1/C2/C3/C4、§2 五种删除意图、§4.1 Stage 1）。
> 底线修复（把触发器拆成"仅用户意图墓碑"）独立于该文档其余部分成立，建议不等 MEM-006 全案评审。

## 8. ContextBuilder

### 8.1 唯一入口，收编现存三处拼装

当前每个调用方各自构造 `PromptContext` 并拼接：

- `ReplyPlanner.cs:40` 之后又用字符串加法把规划指令、图片描述、附件指令追加到 `basePrompt` 上；
- `ConversationService.cs:21` 独立构造；
- `ProactiveMessageGenerator.cs:27` 独立构造（sticker 传空、userMessage 传空串）。

说明书 §七禁止模块自行拼 System Prompt，这条**当前已被违反**。迁移要求：三处统一改为请求 `IContextBuilder`，
分区由请求声明，模型调用方不得对返回值再做字符串追加。

### 8.2 分区、优先级与预算

预算锚点：`ModelProfile.ContextSize` 已作为 `-c` 传给 llama-server（`LlamaServerManager.cs:34`），
输出预留 `ChatRequestOptions.MaxOutputTokens`，再留安全余量。

| 优先级 | 分区 | 预算（对可用输入 token 占比） | 不足时 |
|---|---|---|---|
| P0 | Identity + 输出格式约束 | 固定，约 12% | 不可裁 |
| P0 | 当前用户输入 / 本轮事件 | 剩余全部保留 | 不可裁 |
| P1 | Mood + Relationship 摘要 | ≤ 8% | 压缩为一行 |
| P2 | Recent messages | ≤ 45% | 从最旧丢弃，保底 4 条 |
| P3 | Relevant memories | ≤ 12% 且 ≤ 6 条 | 整段可弃 |
| P4 | Behavior references | RT-003 后启用，≤ 8% | 整段可弃 |
| P5 | Expression patterns | RT-004 后启用，≤ 10% | 整段可弃 |
| P6 | Capability result 摘要 | ≤ 10% | 整段可弃 |

不可进入模型上下文（无论预算多少）：记忆证据原文与 `excerpt`、内部 JSON/状态转储、被取消的候选、
引用材料的原始指令文本、其他 `scope_key` 的任何内容。

裁剪顺序 P5→P4→P3→P2（丢最旧）→P1；P0 永不裁。P0/P2 保底仍溢出时，返回明确的预算错误并由 Runtime 决定降档，
**不得静默截断 Identity 或用户当前输入**。

### 8.3 token 估算

不引入 tokenizer 依赖（会新增模型资产与大件依赖）。用保守启发式：CJK 每字 1.0、ASCII 每字符 0.30、
标点空白 0.5，整体再乘 1.10 余量系数。接口 `ITokenEstimator` 单点暴露，将来换真实 tokenizer 只改一处。
估算偏保守 → 少塞内容 → 与"少而准"同向。

### 8.4 记忆段的渲染契约

固定措辞，避免各模块自写提示：

```
与当前话题相关的长期记忆（可能已过时；仅在自然相关时使用；与用户本轮明说冲突时以本轮为准）：
- <summary>            （<kind>，置信 <confidence>）
```

"以本轮为准"这一句是防误召回的最后闸门：记忆是历史观测，用户当前输入是当下事实。

### 8.5 Retrieval Trace

不新建表。`MemoryContext` 自带 `Trace`（各候选的分项得分、Outcome、被弃原因），`ModelContext` 记录
分区实际用量与被弃分区。Developer Mode 与 Eval 直接消费返回值；只有当需要跨会话统计召回率时再讨论落库，
避免为不存在的需求先建索引。

## 9. 契约草案（`Pry.Core/Abstractions`，不依赖 Avalonia/HTTP）

```csharp
public interface IMemoryQuery
{
    Task<MemoryContext> QueryAsync(MemoryQueryRequest request, CancellationToken ct = default);
}

public sealed record MemoryQueryRequest(
    string ScopeKey, string ConversationId, string Query,
    int MaxMemories = 6, int BudgetTokens = 0,   // 0 = 由实现按默认分区预算决定
    DateTimeOffset? Now = null);

public sealed record MemoryContext(
    MemoryQueryOutcome Outcome,
    IReadOnlyList<MemoryHit> Hits,
    RetrievalTrace Trace);

public sealed record MemoryHit(MemoryRecord Memory, IReadOnlyList<MemoryEvidenceRecord> Evidence,
                               double Score, IReadOnlyList<string> AppliedSignals);

public interface IMemoryWriteback
{
    Task<WritebackReceipt> SubmitAsync(TurnCompleted turn, CancellationToken ct = default);
}

public sealed record TurnCompleted(
    string ScopeKey, string ConversationId, string TurnId,
    IReadOnlyList<ChatMessage> DeliveredMessages);   // 只含成功投递，不含取消草稿

public sealed record WritebackReceipt(bool Accepted, long? JobId, string Reason);

public interface IContextBuilder
{
    ValueTask<ModelContext> BuildAsync(ContextRequest request, CancellationToken ct = default);
}

public sealed record ContextRequest(
    CharacterDefinition Character, CharacterStateSnapshot State,
    IReadOnlyList<ChatMessage> RecentMessages, MemoryContext Memory,
    CurrentTurnEvent Event, BudgetProfile Budget);   // 只接收有限快照，不接收无界文本

public sealed record ModelContext(
    string SystemPrompt, IReadOnlyList<ChatMessage> Messages,
    BudgetUsage Usage, IReadOnlyList<DroppedSection> Dropped);
```

`IMemoryRetriever`（`Capabilities.cs:19-23`）保留为 `IMemoryQuery` 的内部实现细节，不对外作为记忆契约；
`PromptContext` 迁移为 `ContextRequest` 的字段来源而非拼装入口。Core 仍不依赖 SQLite 之外的基础设施。

## 10. 管理面（Client 经 API，不碰库）

现有：列表、创建、更新、删除（`MemoryApplicationService.cs:10-47`）。设计要求的缺口：

- `POST /api/v1/memory/clear`（按 `scope_key` 清空，必须带显式确认参数）——说明书 §八"Clear"。
- 读取响应补 `status`、`confidence`、`origin`、`evidence[]`、来源是否已删除。用户必须能看出"这条是你说的还是我猜的"。
- 删除幂等：重复删除返回同一结果，不产生第二次墓碑（与 §7.1 对齐）。
- 更正可见：`supersedes_id` 链在 UI 呈现为"由旧记忆更正"，不要求用户理解表结构。
- Embedding 只读状态沿用 MEM-004，前端不管理向量。

新 API 落地前须更新 `docs/api-v1.md`（计划 §6）。**该文件当前有他人未提交改动**，实施期需与其 owner 协调，不在本稿擅改。

## 11. 测试矩阵（对齐说明书 §十一）

| 要求 | 用例 | 本设计依据 |
|---|---|---|
| 用户明确事实能被记住 | 第一人称陈述 → 巩固 → 跨会话召回 | §4.2、§5.2 |
| 非用户陈述不写成用户事实 | 仅 assistant/第三方/附件证据 → 拒绝或 `archived` | §4.2 |
| 重复事实不无限创建 | 同指纹二次巩固 → 追加证据不新增行 | §5.4、`ux_memories_active_fingerprint` |
| 纠正后替换/降权 | 新 `correction` + 旧 `superseded`，单事务 | §5.5 |
| 删除后不再召回 | 删除 → 查询空 + 重建任务被墓碑拒 | §7.1、§7.2 |
| 不相关旧记忆不污染 | 语义相近但低于绝对阈值 → 不注入 | §6.2 |
| 重要旧事件能召回 | 高 importance + 语义命中，recency 权重不足以压掉 | §6.1 |
| 会话 A/B 短期隔离 | 检索结果不含另一会话消息 | §6.3 |
| 同实例长期记忆共享 | 同 `scope_key` 两会话均命中 | §2.3 |
| 不同实例不共享私密记忆 | `Same_character_instances_do_not_share_private_memories_or_short_history` | **Core 已通过**（原标"阻塞"有误，见 §15.5）；产品链路仍按 `character:{id}`，待 RT-001 提供稳定实例 ID |
| Worker 崩溃可恢复 | 租约过期 → 重启后回收，不重复写 | §5.6 |
| 巩固重试不重复写入 | 同 job 执行两次 → 行数不变 | §5.6 |
| 失败与空可区分 | 关库/断开 Embedding → `Unavailable` ≠ `Empty` | §6.4 |
| 预算不足可解释 | 强制小 ContextSize → 弃 P3 保 P0，`Dropped` 有记录 | §8.2 |
| 撤销不复活已删记忆 | 删消息 → 撤销 → 记忆仍不出现 | §7.3 |
| 引用材料不入库 | 附件正文含"用户喜欢 X" → 无该记忆 | §4.2 |

固定 Eval（EVAL-001）建议纳入：误召回阈值、删除后不复活、预算裁剪顺序。

## 12. 分期实施

| 期 | 内容 | 可独立验证 | 为什么这个顺序 |
|---|---|---|---|
| M1 | schema 迁移（组件版本）、墓碑、`memory_evidence`、去重索引、`Unavailable/Empty` 区分 | 是 | 没有证据与去重地基就开抽取，只会规模化产生脏记忆 |
| M2 | 候选队列 + 调度 + 抽取 + 更正/合并 + 崩溃恢复 | 是 | 产品价值最大的一段，风险被 M1 接住 |
| M3 | 统一检索打分（recency/阈值/多样性）+ 改用 FTS | 是 | 独立于抽取，可并行；直接降低误召回 |
| M4 | `IContextBuilder` 收编三处拼装 + 预算 + trace | 是 | 迁移面清晰，但会改回复链路，需与 Runtime/Expression Owner 同验 |
| M5 | 管理 UI/API 扩展（clear、证据可见、幂等删除） | 是 | 依赖 M1；前端需与 UI 协作者按已冻结接口对接 |

不依赖 MEM-004 合流：M1、M2、M4。M3 的语义信号需向量通道就位——若 MEM-004 未落库，
M3 只落关键词/recency/阈值部分，语义权重留接口位。

## 13. 决策点状态（2026-09-20 更新）

1. **§7.3 行为变更**：**已结案（本轮不改）+ 已转出** → MEM-006 设计已交付 `docs/memory-message-deletion-separation.md`，
   待项目负责人裁定其 §10 的 P1/P2/P3 三点。本文 §7.3 的结案说明已按该文档更正。
2. **§2.3 作用域**：**已定** → 采纳 `scope_key` 不透明键 + 宿主显式绑定实例；`character:{id}` 继续作为 HTTP/聊天的过渡取值。
   "1.0 不是完整多用户实例产品"这一表述保留，不得被读成实例隔离已完成。
3. **§5.2 水位数值**：**已定** → 先做内部 `ConsolidationPolicy` 配置，不开设置 UI。256 为硬上限，
   高信号**不得**绕过上限（实施方这一收紧优于我原稿，采纳）。
4. **§6.2 阈值**：**标定协议已交付，取值仍未闭合** → `docs/memory-recall-calibration.md` 定义了口径、语料、网格与选择规则；
   0.25/0.65/0.35 的具体数值要等 QA（EVAL-001）按该协议跑出结果。**在此之前不得宣称召回已验证**。
   该文档另纠正了两件本文当时的认知：① 语义通道在生产路径不可达（`ContextBuilder.cs:35` 未传 `semantic`），
   故 0.65 目前是死常量；② 0.35 总分地板与 recency 强耦合，实际是"随记忆年龄抬高的关键词门"，
   在搞清这点前不应网格搜索。
5. **§8.3 估算器**：**已定** → 采用 UTF-8 字节估算；真实 tokenizer 属平台适配，不阻塞 1.0。
6. **MEM-004 合流方式**：**仍未闭合** → 见 §15.4 第 ① 条。这是当前唯一能卡住整个 Phase 5 验收的外部动作。

## 14. 与其他 Agent 的边界（重申）

- Runtime：只向 `IContextBuilder`/`IMemoryQuery` 请求，不碰 `memories`。
- Intelligence（RT-003）：拿 `MemoryContext`，不直查库、不发送消息。
- Platform：SQLite 基础设施与迁移执行归它，Repository 语义（墓碑、状态机、幂等键）归 Memory。
- Client/UI：经 API 查看/编辑/删除/清空，不访问库、不解释 `kind` 之外的业务规则。
- Memory 不修改关系状态、不写 Behavior/Expression、不决定回复时机。

## 15. Owner 裁定与实现现状对照（2026-09-20）

实施交接见 `codex/memory-context` worktree 的 `.local-notes/memory-context/docs/memory-context-implementation.md`。
本节只记录**归属裁定**与**设计偏差的处置**，不重复其实现细节。

### 15.1 三项裁定

**R1 跨模块契约归属。** `MemoryQueryOutcome`、`MemoryContext`、`MemoryHit`、`RetrievalTrace` 等被 Memory 之外
模块消费的类型，**唯一定居 `Pry.Core/Abstractions`**（沿用 `Abstractions/CharacterIntelligence.cs` 已有先例，
并与平台基线把 `CapabilityRequest/Result` 移入 Abstractions 的要求同向）。
`Memory/MemoryContracts.cs` 只保留 Memory 内部类型（候选、租约、fencing、repository 语义）。
行动项：① 实施分支把上述类型移出 `Pry.Core.Memory` 命名空间；② Intelligence 删除
`Intelligence/SituationSnapshot.cs:10` 的重复声明，改为 `using Pry.Core.Abstractions`；
③ `RelevantMemoryView/MemoryObservation` 过渡投影在 `MemoryContext` 接线后连同适配器一并删除。
判据是计划 §2.3 第 3 条"不新造多个含义相同的 Context"：**同一语义只允许一处声明**，临时双份必须有删除条件，
现在条件已明确，不再延长。

**R2 schema 编号。** 采纳实施方的组件版本方案（`memory_schema=1`，未知组件版本拒绝启动），本设计原 §3 的
全局 v8 声明作废（已在 §3 顶部标注）。全局 `schema_info` 推进与迁移注册表归平台/RT-001。
回滚策略采纳实施方写法：停宿主 → 平台备份 → 恢复备份 + 切旧二进制；**不得**靠删表删列"回滚"，
也不得把升级后的库交给旧二进制（旧代码不认识 `superseded`，且不会维护 `scope_key`）。
此条较本设计原 §3.5"旧二进制忽略新列即可读写"更严格，**以更严格者为准**——原文的乐观判断未经验证。

**R3 §7.3 结案。** 见 §7.3 内联标注：本轮维持消息删除连带删记忆，防复活由墓碑承担，本建议转 MEM-006。

### 15.2 设计偏差处置

| 节 | 本设计 | 实施现状 | 处置 |
|---|---|---|---|
| §6.1 权重 | 0.30 语义 + 0.20 关键词 + 0.20 recency + … | 先过相关性门（关键词重合 ≥0.25 或余弦 ≥0.65），再 0.35·max(k,s) + 0.20k + 0.10r + 0.15i + 0.10c + 0.05rel + 0.05conf | **采纳实施**。前置相关性门严于原设计，更符合"宁少勿滥"；且 max() 避免同一条被关键词与语义双重计分，是我的公式没想到的 |
| §6.2 阈值/去冗 | 总分 ≥0.35、同 topic 一条、≤6 条 | 一致，另加 confidence ≥0.5 | **一致** |
| §6.5 注入资格 | 低置信不进上下文 | confidence ≥0.5 为检索硬门 | **一致**（实现位置更靠前，更好） |
| §6.1 半衰期 | 按 kind 五档 | 三档（event 14 / preference 365 / 其余 90） | **采纳实施**，五档在真实语料标定前无意义 |
| §8.2 预算 | 按 ContextSize 百分比 | 绝对 token 上限（P1 240 / P2 1400 / P3 480≤6 条 / P4 200 / P5 160 / P6 240），可用输入 = context − 输出预留 − 128 | **采纳实施**。绝对上限可测可复现，百分比在小 context 下会算出无意义分区 |
| §8.2 保底 | 最近消息丢到保底 4 条 | 无保底，预算不足可全丢 | **保留观察**。全丢会让角色"忘记本轮刚说过的话"，与身份连续性冲突；列为集成期 Eval 项，不单方面改回 |
| §8.3 估算 | CJK 启发式 ×1.10 | UTF-8 字节数 + 每消息 8 单位 | **采纳实施**，更保守且方向正确（少塞）；真实 tokenizer 仍是平台适配项 |
| §5.3 抽取 | 模型抽取，`self_rating` 作弱信号 | **规则式**，只整句单一第一人称，仅产 preference/personal_context | **接受为 M2 首版**。窄而可验证优于宽而不可验证；`self_rating` 通道随通用 LLM 抽取一起再启用 |
| §6.4 状态 | 五态 | 一致，且明确适配器 2 秒超时→`DegradedKeywordOnly`、库失败→`Unavailable`、真无→`Empty` | **一致**，这是本设计 §0 事实 6 要修的那条，已修 |
| §4.2 证据 | 四类 attribution | role 必须 user、禁图、≤100 字、引用/附件/条件句/第三方/多主语句 fail-closed | **采纳实施**，比我列的表更严；§4.2 表格按此重述 |
| §4.1 指纹 | kind + 归一化正文 | kind + 归一化正文 SHA-256 | **一致** |
| §7.1 墓碑 | 应用层落库前检查 | **DB 触发器**：删则记墓碑、插撞墓碑 id 则 `RAISE(IGNORE)` | **优于设计**，采纳。存储级屏蔽覆盖了我没考虑到的"非应用路径写入" |
| §3 新表 | 4 张 | 另加 `memory_scopes`、`memory_scope_conversations`、`memory_turn_sources`、`memory_turn_deliveries` | **采纳**。作用域注册与 Turn 投递证明是 §5.1"只有成功投递参与学习"的必要支撑，我原设计缺这张证明表 |
| §10 API | clear + 证据可见 + 幂等删除 | `GET /api/v1/memories/details`、`POST /api/v1/memories/clear`、DELETE 幂等、编辑撞同文 409 | **一致** |

### 15.3 尚未闭合的接缝（Owner 视角）

1. **第三处 ContextBuilder 未收编**：`ProactiveMessageGenerator.cs:27` 仍自行拼装。§8.1 要求三处收敛，目前 2/3。
2. **MEM-004 未合流**：实施分支不含向量表，语义通道只有接口与测试替身 → §6.1 的语义权重未经真实模型验证。
3. **阈值 0.25/0.65/0.35 未经真实语料标定**，中文双字片段会偶然重合，误召回风险仍在。
   标定协议见 `docs/memory-recall-calibration.md`（含"0.65 目前是死常量"与"0.35 地板与年龄耦合"两条纠正）。
4. **实例隔离只在 Core 层成立**，HTTP/聊天适配仍按 `character:{id}`；§2.3 的诚实边界不变。
5. **抽取面窄**：英文、复杂偏好、确认式证据（`confirmed`）、多数情景事件当前会漏记——这是刻意的，
   但必须在管理 UI 上让用户能手工补，否则"记不住"会被理解为"忘了"。
6. 证据与清空前端 UI 未做（MEM-005 M5）。
7. **（2026-09-21 新增，优先级最高）删消息会永久毒化该事实**：用户意图墓碑与非用户意图的行删除共用
   `BEFORE DELETE ON memories` 一个触发器，因此清理聊天记录即写下永久指纹墓碑，使同一事实此后无法再被学会。
   这不是"记忆没记住"，是**不可恢复的行为性数据丢失**。底线修复（拆分触发器为"仅用户意图墓碑"）
   可独立于 MEM-006 全案先行落地，见 `docs/memory-message-deletion-separation.md` §1.1 C1 与 §10 末段。

### 15.4 集成前置条件（提请负责人）

合并顺序按实施报告 §集成顺序，我确认其正确性；另加两条门禁：
① **先让 MEM-004 与 MEM-005 形成提交**，否则任何后续 worktree 都在猜基线；
② 每次合并后必须重跑"删除后索引/缓存/队列不复活"与"撤销不复活"两类用例——它们是本设计的核心不变量，
回归一次即失去信任基础。`MemoryDatabase.cs` 双 worktree 演化，禁止整文件覆盖。

### 15.5 测试与实现审计（2026-09-20 23:49，Owner 抽查）

**方法声明**：本轮为**静态审计**——逐条读实施分支的用例正文与被测代码，核到文件行号；
**未重跑构建与测试**（当时本机有 8 个 `dotnet.exe` 并发，避免污染他人门禁与产出误导性耗时）。
因此本节不独立复现实施方报告的"218 通过"，只回答另一个问题：**这些用例是否真的在断言本设计的不变量**。
结论：**是，且多数比我原稿更严**。18 项 Core 用例（`MemoryContextTests.cs`）+ 1 项改造的既有用例
（`CoreTests.cs:174-179`）覆盖 §11 矩阵全部 16 行，无一为空断言。

抽查到的强于设计之处：

| 位置 | 断言内容 | 对比本设计 |
|---|---|---|
| `MemoryContextTests.cs:116-128` | 删除后：在途 job `CompleteAsync` 返回 false、同文再抽取被指纹墓碑挡住、**删除前已捕获的 `MemoryContext` 再组装时不得出现在 SystemPrompt**（`Dropped=stale_or_deleted`） | 我 §7.1 只要求"旧任务不得复活"，未要求挡住"上游已缓存的 MemoryContext 迟到组装"这条更隐蔽的泄漏路径 |
| `:142-155` | 跨实例不共享；一会话绑第二个作用域抛 `ArgumentException`；伪造跨作用域 turn 提交返回 `scope_mismatch`；另一会话的 `PRIVATE SHORT HISTORY` 不进 prompt | 我 §11 把这条判为"阻塞"，**判断有误**，已在 §0/§2.3/§11 更正 |
| `:213-224` | 预算紧时保 `IDENTITY` 与 `CURRENT`、`InputTokens ≤ InputBudget`、记忆整段弃且 `Dropped` 非空；P0 放不下抛 `ContextBudgetException` | 与 §8.2 一致，且把"明确失败而非静默截断"写成了断言 |
| `:227-234` | `InitializeAsync()` 连跑两次可重复；手工编辑后 `Evidence` 清空且 `origin=manual` | §3.5 幂等 + §4.2"手工记忆不假装有聊天证据" |
| `:237-251` | 只投递半条回复时 `ClaimAsync` 返回 null；补完第二条才可认领，且 job 带 2 个已投递消息 ID | §5.1"只有成功投递的完整 Turn 参与学习"，实现成资格判定而非约定 |
| `CoreTests.cs:176-179` | 撤销删除后消息恢复为 3 条，但 `ListMemoriesAsync` 仍为空 | 正是 §7.2 我点名担心的 `RestoreConversationMutationAsync` 用 `INSERT OR REPLACE` 绕过墓碑——已被触发器挡住并有断言 |

审计后仍**未闭合**的四条（都不属于"测试没写好"，而是能力本身尚未成立）：

1. **阈值全部未经标定**。`0.25`（关键词重合）/`0.65`（余弦）/`0.35`（总分）在测试里由**确定性语义替身**驱动
   （实施报告 §Persistence 亦承认"不是真实模型验收"）。这些用例证明的是**机制正确**，不是**取值正确**；
   中文双字片段偶然重合造成的误召回风险**没有因为测试通过而下降**。这是 Phase 5 验收前的第一优先 Eval 项。
2. **"最近消息保底"既无实现也无断言**。`:216` 的预算用例只放 1 条历史，无法证明下限存在与否。
   §15.2 那行"保留观察"仍是开放问题，不能记为已验证。
3. **情景事件（`event`）的自动召回未闭环**。自动抽取只产 `preference` / `personal_context`（实施报告 §Memory Schema），
   所以"重要旧事件能被召回"这条用例依赖手工录入数据。"角色自己记住发生过什么"目前不成立。
4. **审计对象是 `3613a2a` 基线上的隔离分支**。该分支 `src/Pry.Core/Memory/` 内**没有** `MemoryDatabase.Runtime.cs`
   （RT-002 的运行时表拆分只存在于主线工作区），即两个分支的 `MemoryDatabase.cs` 已实质分叉。
   §15.4 的两条合并门禁（不复活、撤销不复活）**必须在合并后重跑**，本审计不能替代。

**Owner 判断**：实现质量支持"按接缝合并"，不支持"整包合入即完成"。MEM-005 保持进行中；
上述第 1、3 条属真实缺口，合并后仍不能把 RT-005 标已完成。
