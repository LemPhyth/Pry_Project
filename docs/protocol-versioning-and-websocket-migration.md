# Pry Protocol：版本化与 SSE → WebSocket 迁移方案（RT-001 交付）

> 负责人：Core Platform & Extensibility。日期：2026-09-20。
> 本文是**方案与验收标准**，Phase 1 内不实施传输改造（`PROJECT_PLAN.md` 第 2.3 节迁移约束 2：先冻结协议与版本策略，再改传输，不得双写或维护两份状态）。
> 现状事实与文件行号见 [Phase 1 平台基线与 Contract 冻结](phase1-platform-baseline.md)。

## 1. 现状与目标

| 维度 | 现状 | 目标 |
|---|---|---|
| 命令面 | HTTP/JSON，约 60 个端点，前缀 `api/v1`（`/health` 除外） | 不变，仍为 HTTP；见第 2 节不变量 |
| 事件面 | SSE，每会话独立 `GET /api/v1/conversations/{id}/events` | 单一共享连接上的 WebSocket，SSE 逐步退场 |
| 版本 | 无握手、无响应头、无 `/info`；`SchemaVersion` 只是配置文档版本 | 显式 `major.minor` 协商 + 不匹配拒绝 |
| 游标 | 每会话、仅内存、200 条环形、重启或 `ResetCoreAsync()` 归零 | 全局单调、持久化、可判定"游标已过期" |
| 保活 | 无心跳、无 `Last-Event-ID` | 双向心跳 + 统一重连策略 |
| 发现 | 内嵌：随机端口进程内传字符串；独立：硬编码 5078 | 运行时发现文件 + 会话 token |
| 鉴权 | 无（`docs/api-v1.md:5` 明示"无认证"），仅回环绑定守卫 | 本机共享密钥（local IPC 级），回环守卫不变 |

## 2. 迁移期间不得改变的不变量

1. 只监听回环。`Program.cs:21-23` 的 `IsLoopback` 抛错保留；非回环部署仍受 API-007 前置条件约束，本方案不解锁。
2. Core 不承载 HTTP 服务端类型，Client 不直接调用 Core 内部对象（架构 §48）。
3. 角色/会话状态的真相源只有 Core 的 Store 层。SSE 与 WebSocket 可以**同时投递同一份事件日志**（单一生产者），但不允许各自维护状态——那是"双写"。
4. 事件与响应不得携带密钥、本地绝对路径、模型文件路径。
5. 大媒体只以 `mediaId` / 相对 `downloadUrl` 传递。禁止任何路径把图片以 base64 内联进事件帧（今天 `ToBase64String` 只出现在出站 Provider 请求，`OpenAiCompatibleChatModel.cs:31-32`，迁移后仍不得进入事件面）。
6. 现有 8 个事件名与字段语义在第 4 节冻结之前不变；已发布客户端必须能在新服务端上继续工作。

## 3. 协议版本化（M0-a 已于 2026-09-21 落地主工作区，未提交）

版本常量单点定义于 `Pry.Contracts`（如 `PryProtocol.Version = "1.0"` / `MinSupported = "1.0"`），Client、Api、诊断界面读取同一常量。

- `GET /api/v1/info` → `{ protocolVersion, minSupportedClientVersion, coreVersion, schemaVersion, features[] }`。`features[]` 是能力开关名（如 `websocket-events`、`durable-event-cursor`），客户端据此降级而不是猜版本号。
- 每个 `api/v1` 响应带 `Pry-Protocol-Version` 头；客户端每个请求带 `Pry-Client-Protocol: 1.0`。
- 协商规则：`major` 必须相等；客户端 `minor` 不得高于服务端 `minor`。不满足时返回 `426 Upgrade Required`，`urn:pry:error:protocol_mismatch`，扩展字段含服务端区间。**这不是可静默降级的错误**：客户端必须停在明确的版本不匹配界面，不得当作网络故障无限重试。
- 兼容策略：事件类型与 DTO 字段只增不改不删（`minor`）；语义变更必须换 `major`。因此**客户端必须忽略未知事件类型与未知字段**，这条写进 `docs/api-v1.md` 并成为回归测试。
- 现有未版本化的 `/health` 保留为存活探针，但客户端不得再用它判断协议兼容（`docs/engineering-pitfalls.md` 第 2 条已记录该坑）。

## 4. 事件信封与游标

统一信封（SSE 的 `data:` 与 WS 的 `event` 帧共用同一 JSON，消除目前 `id:`/`event:` 与 `data` 内重复序列化但客户端只读 `data` 的分歧）：

```json
{ "protocolVersion": "1.0", "sequence": 4712, "type": "message.created",
  "occurredAt": "2026-09-20T12:00:00Z", "conversationId": "c1", "data": { } }
```

游标改为持久化：新增 `protocol_events(sequence INTEGER PRIMARY KEY AUTOINCREMENT, conversation_id, type, payload_json, created_at)`，事件发布与领域写入同事务提交，读取按 `conversation_id` 过滤。

- 重连语义唯一：`ConversationEventStreamRunner`（`codex/client-error-reconnect` worktree，未跟踪文件 `src/Pry.Client/ConversationEventStreamRunner.cs`）已经实现客户端侧的宿主：游标 `LastSequence` 跨重连单调（`:54,77`）、指数退避 500ms→15s、连续 8 次失败转 `Offline` 并抛 `Faulted`（`:18-19,96-108`）、重连后通过 `Resumed` 提示调用方补取历史（`:60-61,116`）。本方案**采用它为唯一标准**，不再另定退避参数；窗口内两套不一致的重连循环（`RemoteConversationTurnManager.cs:60-97` 固定 500ms、`PryMessengerWindow.axaml.cs:462-502` 固定 `after=-1` 因而无法补投）改为订阅它的 `EventReceived`/`StateChanged`/`Resumed`。PLAT-005 与之共同构成 M0，双方不要各自实现同一层。
- 该 Runner 目前只能"猜"是否有缺口：任何一次重新连上都会触发 `Resumed`。服务端游标持久化后应显式区分两种情况——连续无缺口时不下发额外帧，保留窗口被裁剪时下发 `stream.resumed { reason: "buffer_expired", firstRetained, latestSequence }`（判据与字段语义见 §4.1 第 3 条），客户端据此**必须先重取历史再接事件**，不得静默留洞。这是"reconnect"与"WebSocket disconnect"两条测试重点的共同判据。
- 心跳（新增，弥补该 Runner 的空白）：SSE 每 15s 注释帧 `:ping`；WS 每 15s ping 帧。当前 `await foreach` 在半开连接上会永远阻塞而不抛错，因此没有心跳就无法触发它的退避路径。仅有服务端心跳**不够**：`PryBackendClient.ReadEventsAsync`（`src/Pry.Client/PryBackendClient.cs:159` 起）只解析 `data:` 行并丢弃 SSE 注释行，心跳到了客户端仍是"零事件"，`ConversationEventStreamRunner` 依旧阻塞。M0 因此包含一个客户端传输侧改动——把注释帧或原始字节到达提升为存活信号（非事件回调或读空闲计时器），由该层判定"连续两个心跳周期无任何字节"即断开重连，而不是在 Runner 里加超时开关（UI-027 已明确不在其层加，接缝记在其文档 3.1）。
  **M0-b 进度（2026-09-21 落地主工作区，未提交）**：SSE 侧由 `Pry.Api/Services/SseEventStream.cs` 在空闲窗口内下发 `:ping`，周期与判定倍数单点定义于 `PryProtocol.HeartbeatIntervalMs` / `LivenessTimeoutMs` / `HeartbeatFrame`；客户端 `ReadEventsAsync` 改为逐行存活窗口判定——读到任意一行（含注释帧）即重新计时，超过 `LivenessTimeoutMs` 无任何行则抛 `TimeoutException` 结束本次订阅，由上层按既有退避重连，正常 EOF 仍是另一条退出路径。服务端周期可用 `Pry:EventHeartbeatMs` 调节，并被夹在 `[1000ms, LivenessTimeoutMs/2]` 内（语义见 `docs/api-v1.md`）。WS ping 帧属 M1：主工作区还没有 `/api/v1/ws`，本片不提前建传输。
- 建议给 `ConversationStreamOptions` 增加抖动（jitter），避免 Core 重启后多窗口同时重连；属该负责人的改动，本方案只提出不代为实现。
- `turn.completed` 必须真正发布（当前有消费分支却从未发出），且 `turn.failed.code` 改为携带 PLAT-001 的稳定错误码，不再是恒定的 `model_failure`。
  **M0-b 前半已落地（2026-09-21）**：`ConversationRuntimeEventSink.TurnTransitionAsync` 对 `Completed` / `PartiallyDelivered` 下发 `turn.completed { turnId, generationId, outcome, reason }`，`PryMessengerWindow.axaml.cs:473` 那条死分支自此真正被触发。其余终态刻意不发——取消与失败各有 `turn.cancelled` / `turn.failed`，沉默、推迟、围栏淘汰的轮次没有交付任何东西，补发终态会把它们显示成"回复已完成"。后半（`turn.failed.code` 换成 PLAT-001 稳定错误码）随 PLAT-001 收敛，不在本片单方面改错误分类。

### 4.1 M0-c 契约细化（2026-09-21 冻结；**同日按作者"推进阶段一直到可以交给 QA 验收"的指令实施完毕，未提交**）

**落地事实（阶段①，工作区快照非统一提交）**：表与追加/读取/窗口/裁剪 API 在 `src/Pry.Core/Memory/MemoryDatabase.ProtocolEvents.cs`（`MemoryDatabase` 的 partial），schema 8→9 在 `MemoryDatabase.cs`，删除遮蔽与消息删除同事务；`Pry.Api` 侧 `ConversationEventLog` 已退化为唤醒信号 + 电平类 fan-out，`ConversationSessionService.EventsAsync` 负责 `stream.resumed` 宣告，`ProtocolEventCleanupService` 负责周期裁剪。判据：`tests/Pry.Core.Tests/ProtocolEventJournalTests.cs`（11 条）与 `tests/Pry.Api.Tests/ProtocolEventCursorTests.cs`（6 条，走真实 Kestrel）。两处与冻结契约的实现层级差异，均为**有意的偏离**而非放宽：① 配置区间与下界夹取（`[50,5000]` / `[500,200000]` / 年龄 1-365）落在传输层的 `ProtocolEventRetention.Read`，Core 只把无意义值兜到 1，避免同一条区间在两层各写一份而日后走偏；"配成 0 仍按 50 生效"因此由 `ProtocolEventCleanupService` 这一侧保证，判据仍在。② `ConversationEventLog.Publish` 保留同步签名（内部 sync-over-async 写一次本地 SQLite），以免动到他人未提交文件 `Runtime/RuntimeAdapters.cs` 的签名。

**研究来源与授权口径**（`PROJECT_PLAN.md` §6 红框三条）：借鉴的是 `docs/research/Satori.md` 第 5 节的**机制**——上游有 sequence / event buffer / resume 的设计，但原研究未找到"正常事件写入 buffer"的代码；这恰好是我们的缺陷形状（`ConversationEventLog.cs:24` 每会话 200 条内存环、序号每会话且重启归零）。本片不复制任何上游代码，故"锁定 commit/tag 复核"对本片不适用；反向的禁令有效：**不把 Satori 当 Guaranteed Delivery Bus**，M0-c 只做"至少一次补投 + 显式宣告丢失"，不建 Outbox / Ack / Retry / TTL（`ack` 帧留在第 5 节的 WS 面，属 M1）。同时按 `COMPETITIVE_MATRIX.md` 的判据"协议 event buffer 是传输机制，不能等同 Memory"划死读边界（第 6 条）。

1. **序号单点、全局单调**：`sequence` 只由 `protocol_events` 的 AUTOINCREMENT 分配，跨会话全局递增。因此**同一会话内出现空洞是正常状态**（其他会话消耗了序号），"序号连续"不再是客户端可依赖的判据，是否丢事件只能由服务端判定（第 3 条）。现有"每会话独立序号、重启从 1 重来"必须废弃：带旧游标重连的客户端会在新序号爬到该值之前**静默收不到任何事件**，这比"无法补投"更糟，也是本片解决的既有问题。
2. **事件分两类，真相源只有一个**。事实/终态类入库并可重放（`message.created`、`conversation.changed`、`conversation.read`、`interaction.performed`、`turn.completed`、`turn.cancelled`、`turn.failed`、主动话题的投递终态）；电平触发类**不入库**（`session.ready`、`turn.state`，以及未来的 `typing`），因为重放一条三分钟前的 `ModelThinking` 会把界面倒回旧状态——它表达的是"现在处于哪"，不是"发生过什么"。兼容代价刻意选零：电平类帧的 `id:` 复用"当前最新持久序号"（因此不推进游标），帧格式与客户端实现完全不变，避免为分类引入新客户端义务。
3. **丢失判据 `stream.resumed`**：`firstRetained = SELECT MIN(sequence) FROM protocol_events WHERE conversation_id = $1`；当 `after >= 0` 且 `after + 1 < firstRetained` 时，在订阅建立后恰下发一次 `stream.resumed { reason: "buffer_expired", firstRetained, latestSequence }`，随后继续正常推送。会话从未有事件（`firstRetained` 为空）、或 `after` 大于等于最新序号，都不触发；`after=-1`（只收新事件）永不触发。判据刻意保守——极端情形会误报，代价是客户端多做一次历史 GET，远低于留洞。**客户端收到该帧必须先重取历史再接事件，不得静默留洞**——与上文"该 Runner 目前只能'猜'是否有缺口"那条的原有要求同口径，本片只是把它从"每次重连都猜"换成"只在确实裁掉过时宣告"，并补上 `firstRetained` 供客户端判断重取范围。
4. **保留与裁剪**：新增配置 `Pry:ProtocolEventMaxPerConversation`（默认 500，服务端夹在 `[50, 5000]`）、`Pry:ProtocolEventMaxAgeDays`（默认 7）、`Pry:ProtocolEventMaxTotalRows`（默认 20000，夹在 `[500, 200000]`），与 `Pry:EventHeartbeatMs` 同属"只走环境变量/测试注入、不进 `appsettings.json`"的一类。裁剪顺序：先按会话超额削最旧，再按年龄全局扫，最后受总行数约束。数字依据：一轮回复约 5～15 条入库事件，500 条约覆盖数十轮，够一个周末关闭客户端后的重连窗口；平均 payload 小于 1 KB 时 20000 行约 20 MB，与 `messages` 表本身同数量级。下界夹取是硬要求——配成 0 会让每次重连都 `buffer_expired`，把补投机制退化成轮询。
5. **隐私与删除遮蔽**（《项目计划》§7.1"诊断默认不收集完整私人聊天"的落点）：`payload_json` 会含对话明文（`ConversationSessionService.cs:43` 的 `message.created` 带 `content`）。同一 SQLite 文件、同样是明文，所以不新增机密级；新增的是**留存时长与冗余**——内存 200 环那条"意外的保留策略"一旦落盘就消失，因此第 4 条是必需项而非可选项。三条硬规则：① 事件 payload 只带引用（`messageId`、`mediaId`、相对 URL），不得内联大字段或 Base64（与第 8 节验收第 7 条同口径）；② 删除消息的同一领域事务必须清理 `subject_message_id` 命中的行，否则"删除"会在传输层留下明文副本；③ 删除会话由外键级联清理（第 7 条）。附带债务：`message.created` 的 `content` 最终应收敛为只带 ID、由客户端以数据库为权威重取（同 `docs/api-v1.md` 已读水位那节的"以数据库为权威"），但那会改帧语义，属 M2 统一信封，不在 M0-c 单方面做。
6. **读边界（journal ≠ Memory）**：`protocol_events` 的唯一读者是传输层。裁剪或清理 journal 不得触及 `messages`/`memories` 任何一行；反过来任何界面或 Runtime 都不得把 journal 当历史读取。这条要写成断言测试（裁剪前后领域历史逐字节不变），因为它是后续所有人往这张表加"顺便读一下"的入口。
7. **表结构与《项目计划》§6 要求的 NULL/索引/外键检查**：`protocol_events(sequence INTEGER PRIMARY KEY AUTOINCREMENT, conversation_id TEXT NOT NULL, type TEXT NOT NULL, payload_json TEXT NOT NULL, created_at TEXT NOT NULL, subject_message_id INTEGER NULL, FOREIGN KEY(conversation_id) REFERENCES conversations(id) ON DELETE CASCADE)`；索引 `ix_protocol_events_conversation(conversation_id, sequence)`（游标读取）与 `ix_protocol_events_created(created_at)`（年龄裁剪）。唯一可空列是 `subject_message_id`，语义为"该事件不指向具体消息"，客户端不得据此列缺失推断事件类型。外键写法沿用 `interaction_commands`（`MemoryDatabase.cs:59`），可用性和 `MemoryDatabase.cs:923` 一致：`OpenAsync` 为**每条**连接执行 `PRAGMA busy_timeout=5000; PRAGMA foreign_keys=ON`；注意这是连接级设置，任何绕过 `MemoryDatabase` 自开连接的代码都不享受它，所以级联不能被当作唯一清理手段（消息级遮蔽本就如第 5 条②走显式删除）。
8. **schema 版本与迁移归属**：现状是"一段幂等 `CREATE TABLE IF NOT EXISTS` 加末尾字面量"（`MemoryDatabase.cs:19-133`，`SetSchemaVersionAsync` 为 `UPDATE schema_info SET version=$version WHERE version<$version`）。M0-c 只做最小动作——补建表与索引语句、把版本推进 8→9，**不建迁移注册表、不加 down-migration**（迁移注册表与 `PRAGMA integrity_check` 属 PLAT-003，§5.0 已冻结，不得顺路实现）。回滚说明：旧二进制用 `SELECT MAX(version)` 读版本且不做相等断言，`WHERE version<$version` 的单调更新对降级无副作用，未知表被忽略，因此降级安全（与 §8"旧二进制可忽略新增结构"的 v7 论证同构）；真正的前滚恢复方案随 PLAT-003 出。
9. **分层落点**（2026-09-21 实施 M0-c 时更正，见本节末"更正记录"）：表与追加/读取/裁剪 API 落在 `Pry.Core`（新增窄接口 + Core 内的自有记录类型），因为追加必须能与领域写入同事务；`Pry.Api` 的 `ConversationEventLog` 退化为"唤醒信号 + 电平类 fan-out"的读取视图并继续产出既有 `ConversationEvent` DTO；Core 不新增对 HTTP/Avalonia 的依赖。`features[]` 里的 `durable-event-cursor` 只能在实现真正落地的那一次提交加入——在此之前出现即契约违规（`docs/api-v1.md` 协议节已写明）。
   **更正记录**：本条冻结时写的是"`Pry.Core` 不引用 `Pry.Contracts`（引用方向是 Contracts → Core，见 `Pry.Contracts.csproj:3`）"，那句话的前提是"Contracts 仍引用 Core"这一现状，而 PLAT-005 的完成标准恰恰要求解除该引用——两个前提不能同时成立。取舍是把箭头换成不变量：**Core 不承载线契约，Contracts 不承载持久化 API**。落地后 `Pry.Core → Pry.Contracts`，18 个"既进 HTTP/SSE 响应、又被 Core 存储与界面直接使用"的值类型（`CharacterPromptMode`、`ChatRole`、`StickerSource`、`MainWindowLayoutModes`、`RuntimeState`、`TypingStyle`、`TurnTakingSettings`、`ModelCapabilities`、`ShortcutSettings`、`UserProfilePreferences`、`ImageDisplayPreferences`、`DesktopPetPreferences`、`ModelTuningPreferences`、`MessageSources`、`ChatMessage`、`ConversationRoom`、`ConversationFolder`、`MemoryRecord`）的声明移入 `Pry.Contracts/CommonContracts.cs`，属性名、默认值与枚举拼写逐字保持，线格式不变；`protocol_events` 的表与 API 仍全在 Core，本条其余部分不变。副作用是正向的：`Pry.Client` 此前经 Contracts 传递依赖看到 Core 类型并把它们用作公开返回值，现在它的签名只剩契约类型。防止复发的门是 `Pry.Api.Tests/ContractBoundaryTests`（契约程序集不得引用任何 Pry 程序集、契约公开签名不得出现 Core 类型、上述类型的 JSON 形状逐字钉住）。

10. **验收补强**（细化第 8 节第 5 条）：跨重启原游标不丢不重；裁剪后 `stream.resumed` 恰一帧且客户端历史重取路径被触发；删会话后表内无残留、删消息后该 `subject_message_id` 的 payload 不再可读；两会话交错写入时序号全局单调且各自流内允许空洞、客户端不得据空洞自判丢失；重启后订阅不会出现早于订阅时刻的 `turn.state`/`session.ready`；保留配置配 0 仍按 50 生效；裁剪前后领域历史逐字节不变。

## 5. WebSocket 端点

`GET /api/v1/ws`，握手查询参数 `protocol=1.0`，鉴权见第 6 节。

- 帧：`{ kind: "hello" | "subscribe" | "event" | "ack" | "ping" | "pong", ... }`。`hello` 由服务端先于任何事件下发，携带 `protocolVersion`、`features[]`、`latestSequence`。
- 客户端→服务端只允许事件面与顺序敏感的本机信号：`subscribe`（会话订阅集）、`typing`、`input-activity`、`ack`（游标确认，用于服务端裁剪）。
- **CRUD 不搬进 WS**：会话、历史、记忆、设置、Provider/Plugin 配置、备份/恢复继续走 HTTP（架构 §49/§50 的分工）。
- 服务端事件生产者只有一个：`protocol_events` 的追加。`IRuntimeOutputSink` 的宿主实现写入该表，SSE 端点与 WS 端点都是它的读取视图（第 5 节接缝表给出映射）。
- 会话驱逐后无人订阅时事件仍落库（主动消息可能在无连接时产生），补投由游标负责。当前"订阅不预加载模型"的行为（`ConversationSessionService.cs:218-226` 只取事件日志、不建 session）必须保留。

## 6. 进程启动、端口发现与本机 token

一个机制同时解决发现、TOCTOU 与无鉴权三件事：

1. Core 启动时绑定 `127.0.0.1:0` 并**持有该监听套接字**直到 Kestrel 接管（消除 `App.axaml.cs:211-215` 申请后立刻 `Dispose` 的端口竞态）。
2. 生成随机 32 字节 token，写运行时文件 `<DataDirectory>/runtime.json`：`{ pid, port, token, protocolVersion, startedAt, schemaVersion }`。目录沿用现有解析（`Program.cs:43-45`，`Pry__DataDirectory` 可隔离）。文件 ACL 限定当前用户。
3. 客户端发现顺序：同进程内嵌时直接读同一对象；独立进程时读 `runtime.json` 并校验 `pid` 存活与端口一致，再带 `X-Pry-Token` 发请求；WS 握手同样携带。缺失或不匹配 → `401 urn:pry:error:unauthorized`，不回退到硬编码 5078。
4. Core 退出时删除该文件；启动时若发现陈旧文件（`pid` 不存在或端口无响应）则覆盖，并记录一次崩溃恢复痕迹（配合架构 §55 与 `SweepStalePlansAsync`）。
5. 这是本机 IPC 级防护，**不是** API-007 的网络认证栈；回环限制与 token 同时生效，任一缺失都拒绝服务。
6. 优雅停机：`BackendRuntime.StopAsync`（`BackendRuntime.cs:37-41`）先停止接受新订阅、再冲刷 `protocol_events`，客户端在收到 `server.shutdown` 事件后按退避重连。退出语义目前一定会关闭模型服务（`docs/engineering-pitfalls.md` 第 12 条），本方案不改变它。
7. **鉴权必须是一个中间件，不是逐路由的 opt-in**（2026-09-21 补，来源 `docs/research/Satori.md` §5 的 auth caveat：原研究看到普通 REST 与 WS IDENTIFY 只带一个可选 Bearer，而 `/v1/internal/*` 与 `/v1/proxy/*` 未走前面的 `checkAuth()`）。落到 Pry：token 校验放在管线里，**默认拒绝**，豁免写成显式白名单且只有两项——未版本化的 `GET /health`（存活探针，第 3 节已规定它不得用于判断协议兼容，因此也不承载任何状态）与 `GET /api/v1/info`（不匹配的客户端必须能读到版本区间，见 §3 例外条）。`GET /api/v1/conversations/{id}/events` 是长连接但同样要带 token：它能读出会话历史走向。新增端点默认落在拒绝侧，"忘记加豁免"必须是安全失败而不是数据泄露。
8. token 只走请求头，不进查询串（会落进日志、URL 历史与进程命令行——PLAT-002 刚为模型密钥踩过同一类坑）；比较用定长时间；失败统一 `401 urn:pry:error:unauthorized`，响应与日志都不得回显 token 明文（第 8 节验收第 4 条）。它的语义边界要写清楚：这是**本机进程间接缝**（挡住同机其他进程调用有副作用的接口），不是账号体系，也不是 API-007 的网络认证栈；客户端不得把它当登录态持久化，跨重启以 `runtime.json` 现读为准。

### 6.1 M0-d 落地事实（2026-09-21，按作者"推进到可交 QA 验收"指令实施，未提交）

- 实现体：`src/Pry.Api/Services/BackendSessionToken.cs`（32 字节随机秘密、`FixedTimeEquals` 等时比较、`ToString()` 打码）、`src/Pry.Api/Services/RuntimeDescriptorService.cs`（写/删 `runtime.json`、陈旧文件的崩溃恢复记录、原子替换）、`src/Pry.Api/Middleware/SessionTokenMiddleware.cs`（默认拒绝，豁免两条按整段相等匹配）、`src/Pry.Api/EmbeddedBackend.cs`（`BoundUrl()` 与 `CreateBackendHttpClient()`）、`src/Pry.Contracts/PryProtocol.cs`（`SessionTokenHeader`、`UnauthorizedErrorCode`、`RuntimeDescriptorFileName`、`RuntimeDescriptor`；`features[]` 增 `session-token` 与 `runtime-discovery`）。接线：`Program.cs` 把令牌门放在 `ProtocolNegotiationMiddleware` **之前**，`App.axaml.cs` 改用随机端口与内嵌发现。`schemaVersion` 取自 `MemoryDatabase.SchemaVersion` 这一唯一写侧常量，不另写字面量。
- 与契约第 1 条的一处实现偏差（更强而非更弱）：不是"Core 先占住监听套接字再交给 Kestrel"，而是把 `Pry:Url` 直接设为 `http://127.0.0.1:0`，让 **Kestrel 自己绑定并持有**，端口从 `IServerAddressesFeature` 读回。"占住再转交"仍是两次绑定之间的一次交接，随机端口只绑一次才是无竞态形态；`App.axaml.cs` 里申请后立刻 `Dispose` 的 `ReserveLoopbackPort()` 已删除。
- 契约第 4 条的"端口无响应"探测未实现，陈旧判定只按 `pid` 存活：`pid` 已不存在＝崩溃恢复，`pid` 仍在＝同一数据目录多实例冲突。两种都在覆盖前留一条恢复记录，消息里区分状态，但都不复述旧令牌。主动探测端口要引入连接语义，留给 M1 的客户端重连路径一起收。
- 契约第 6 条的 `server.shutdown` 事件不在本片范围内（它需要 §4.1 的事件目录先加一条电平事件）；本层只保证 `ApplicationStopping` 删除描述文件，且只在文件内容仍属于本进程（`pid` + `port` 都匹配）时删除，避免抹掉后继实例的入口。
- 令牌门只覆盖 HTTP 面。WS 尚未存在，将来握手必须复用同一枚令牌与同一个中间件语义（第 3 条）。
- 测试：`tests/Pry.Api.Tests/SessionTokenTests.cs` 11 例，逐条对应 §8 第 3、4、9 条——端点遍历（无令牌一律 `401`、豁免面恰好两条且返回 200、带发现文件里的令牌一律过门）、缺/错/非法 Base64/长度不符的令牌响应除 `traceId` 外一字不差、被拒的写不落库、令牌不进响应也不进日志（日志面用可枚举的内存 provider，并用崩溃恢复记录证明它确实接上了）、描述文件的字段与真实绑定端口/进程/版本一致且优雅退出后消失、陈旧文件被覆盖且只留一条恢复记录、两个实例拿到不同端口。既有 5 个真实 HTTP 测试类改为经 `CreateBackendHttpClient()` 取令牌。

## 7. 迁移步骤与回滚

| 步骤 | 内容 | 传输 | 回滚 |
|---|---|---|---|
| M0-a（**已落地主工作区，2026-09-21，未提交**） | 版本常量 + `/api/v1/info` + 版本头 + 426 不匹配；`features[]` 只列真实能力 | SSE | 单提交 revert，无数据迁移 |
| M0-b（**心跳 + 存活信号 + `turn.completed` 已落地主工作区，2026-09-21，未提交**；WS ping 帧随 M1） | 心跳（SSE `:ping` / WS ping）+ 客户端存活信号：`PryBackendClient.ReadEventsAsync` 必须把注释帧或原始字节到达提升为非事件信号，否则 `ConversationEventStreamRunner` 在半开连接上依旧永久阻塞；补发 `turn.completed` | SSE | 单提交 revert |
| M0-c（**已落地主工作区，2026-09-21，未提交**；契约见 §4.1，含两处有意的层级偏离） | 持久事件日志与游标（`protocol_events`）、`stream.resumed{reason:"buffer_expired",firstRetained,latestSequence}` 显式过期信号、保留/裁剪与删除遮蔽 | SSE | 新增表，回滚需保留旧内存环形缓冲的读取路径直到确认无客户端依赖（过渡期内存环只作唤醒信号与电平类 fan-out，不再持事件真相源与序号，见 §4.1 第 2/9 条） |
| M0-d（**已落地主工作区，2026-09-21，见 §6.1；未提交**） | `runtime.json` 端口发现 + 32 字节会话令牌（一次改动同时关闭 TOCTOU 与零鉴权） | SSE | 单提交 revert；`runtime.json` 是新增文件，无数据迁移 |

M0 的客户端部分只接入既有 `ConversationEventStreamRunner`，不在本任务内重写它。
| M1（Phase 2 起，随 Runtime 落地） | 新增 `/api/v1/ws`，`IRuntimeOutputSink` 宿主写入单一事件日志，SSE 与 WS 双视图读同一日志 | SSE + WS 并存 | 删 WS 端点即可，SSE 未动 |
| M2 | `Pry.App` 全部窗口改订阅 `Pry.Client` 单一事件宿主；COMP-006 未读/通知、UI-021 连续工作区随迁 | WS 为主 | 恢复窗口内旧订阅路径（M0 后已是薄层） |
| M3 | 移除 SSE 端点与 `ConversationEventLog` 内存环形缓冲 | 仅 WS | 保留 SSE 直到不存在需要它的客户端；这一步不得与 M2 同一提交 |

禁止：任一步骤里同时保留两份事件状态源、给 WS 单独造一套事件对象、或让 UI 绕过 `Pry.Client` 直连新端点。

## 8. 本阶段验收清单（M0 完成即 Phase 1 协议验收）

1. 只绑回环：非 `127.0.0.1` 的 `Pry:Url` 启动即失败。
   **校正（2026-09-21，QA-P2-03 / 场景 `PLT-001`）**：本条原记"现有行为回归"是**判据缺失**——守卫在 `Program.cs:21-23` 一直存在，但 `tests/` 里没有任何用例进入那条分支，"有回归"与"有人证明它有回归"不是一件事。现由 `tests/Pry.Api.Tests/LoopbackBindingTests` 补上禁行那一半：`0.0.0.0`、`10.0.0.5`、外部域名与**空串**四种 `Pry:Url` 必须在 `BuildAsync` 内抛错，且抛错时数据目录尚未创建（拒绝发生在写库/建日志之前）；另配一条 `127.0.0.1:0` 正向对照，证明守卫不是无脑拒绝一切。真正起监听的正向事实仍由 `SessionTokenTests`/`EmbeddedBackendEndpointTests` 的真实 Kestrel 用例提供。
2. 版本不匹配：客户端声明 `2.0` 时收到 `426` + `protocol_mismatch`，且不进入重试循环。
   **M0-a 进度（2026-09-21）**：服务端侧已断言——`426` + `urn:pry:error:protocol_mismatch` + `retryable:false` + 双方版本，且拒绝发生在业务写入之前（会话数量不变）；`GET /api/v1/info` 对不匹配客户端仍可达；客户端侧以带 `code`/`traceId` 的 `PryBackendException` 上抛，不在传输层重试。"停在明确不匹配界面、不进重试循环"仍要等 UI-027 的 `PryErrorPresenter`/`Offline` 路径进入主工作区才能整项结案。
   **QA-P2-01 更正（2026-09-21 第二轮）**：上一条里"客户端侧以带 `code`/`traceId` 的 `PryBackendException` 上抛"**不等于禁令到位**——`ApiProblemResponse` 当时没有 `Retryable` 字段，服务端写出的 `retryable:false` 在客户端边界被丢弃，界面因此把 `426`/`401` 当普通断链以固定间隔无限重连（QA 记 Fail 而非 Blocked 的理由正是"禁行的那一半今天就能证伪"）。现已修复：`ApiProblemResponse.Retryable`（`bool?`）+ `PryBackendException.Retryable` 逐层透传，`ReadEventsAsync` 的失败路径与 `EnsureSuccessAsync` 共用同一出口，所以**事件流订阅者也拿得到禁令**（`ProtocolHandshakeClientTests` 三条：426 为 `false`、缺席为 `null` 且不得被猜成禁令、流订阅路径同样带出）。**`null` 不是 `false`**：旧服务端与非 ProblemDetails 响应都落 `null`，消费方只能按既有策略处理。**QA-P2-01 收尾（2026-09-21 第三轮，界面侧已落地）**：上一条末尾"剩下的仍是界面侧按 `Code`/`Retryable` 停止重试"已按 QA 给的"也可先补分支"路线做完，不必等 UI-027 的 Runner 合流。判据收敛成一个消费点：`PryBackendException.RetryForbidden`（**只有** `retryable:false` 为真，`null` 与 `true` 都不是禁令），两个事件流循环（`RemoteConversationTurnManager`、`PryMessengerWindow`）各在通用 catch **之前**插入 `catch (PryBackendException ex) when (ex.RetryForbidden)`，直接终止循环、不再退避、也不再重启请求，文案由 `BackendBanNotice.For` 按 `Code` 给出可执行的修复说明（协议不匹配→同一发行版；令牌被拒→完全退出再启动；未登记禁行码→带出服务端原码）。回归由 `tests/Pry.App.Tests/EventStreamReconnectBanTests` 五个用例守住**两个方向**：426/401 与未登记的 502 `provider_auth` 都只发一次请求且只弹一次终止提示（观察窗 1.5s），而 `retryable` 缺席（旧后端/非 ProblemDetails）与 `retryable:true` 必须**继续**重连且不升级为终止——只测禁行那一半，守卫过严把网络抖动做成永久失败是测不出来的。证伪运行：把守卫条件临时改成 `when (false)` 后三条禁行用例全红，两条放行用例不受影响，随后还原。**一处如实收窄**：那 5 例驱动的是 `RemoteConversationTurnManager`，而它唯一的构造点在 `MainWindow.axaml.cs:225`，`MainWindow` 自身没有构造点（`App.axaml.cs:67` 打开 `PryMessengerWindow`）——**应用今天真正跑的 `PryMessengerWindow.ObserveEventsAsync` 那条分支没有自动化覆盖**，两边形状一致但后者是 Avalonia 代码后置。**本项仍不在此结案**：`PLT-002` 的界面判据转 Pass 由 QA 复判，且 UI-027 的统一 Runner 合流时会接管同一判据（本分支是其子集，不另立第二套错误状态源）。
   **UI-027 收敛（2026-09-21 第五轮，禁令只剩一处读法）**：上一条"两处循环各自拦禁令 + `BackendBanNotice` 各写一份文案"的临时形状已经撤掉。`src/Pry.Client/ConversationEventStreamRunner.cs` 与 `PryErrorPresentation.cs` 进主工作区，两处界面循环改为**只消费 Runner 的连接状态与终止出口**：退避档位、连续失败上限、游标续传、以及"`retryable:false` 就到此为止"全部由 Runner 的 `BackoffAsync` 判出（禁令分支排在计数与退避**之前**，顺序反了就会先排一轮延迟），文案由 `PryErrorPresenter` 单点给出，`BackendBanNotice.cs` 删除。附带修掉两条旧缺陷：`PryMessengerWindow` 那条循环原本固定 `after=-1`，重连时把整段事件日志重放一遍而不是续传，现在与另一条一样从最后投递序号续订，并在重连成功的第一条事件之前补取一次历史（`Resumed`）。**判据分两层**：Runner 侧 15 例（`tests/Pry.Client.Tests/ConversationEventStreamRunnerTests.cs`，含禁行三码 × 退避零次、`retryable` 缺席/为真两方向必须继续重连八次一轮不少）、呈现 31 例（`PryErrorPresentationTests.cs`，含 PLAT-001 七类码逐档 + 禁行三档 + "同一条码在服务端沉默时仍可重试"）、界面侧原有 5 例保留为端到端形状。防复发门：`SingleBanJudgeBoundaryTests` 扫产品源码，`Pry.Client` 之外再出现 `RetryForbidden` 即红（证伪跑：在 `Pry.App` 放一个含该词的文件即转红）。**QA-P4-01 的"改坏无人报警"就此闭合**——丢禁令不再可能发生在界面层，因为界面层已经没有这个判断。**仍未覆盖的两条如实登记**：`PryMessengerWindow` 里剩下的是 `Dispatcher.UIThread` 投递与切房丢弃，仓库无 headless Avalonia 设施（本仓库的既有做法是把逻辑抽成可测类，见 `tests/Pry.App.Tests/` 的 *Factory/Controller 系列）；`RemoteConversationTurnManager` 刻意**不挂** `Resumed`——它的回复走追加渲染，补取历史会把同一条画两遍，收敛需要在追加侧按消息 Id 去重。
   **QA-P5-01 结案（2026-09-21 第五轮返工；QA 给的两条路线取第一条）**：上一条末尾"接线零判据"那句已经不成立，因此**不采用**"接线属人工走查"那条结案路线。活窗口那条循环的接线抽成 `src/Pry.App/Services/MessengerEventStreamController.cs`（四个回调在 `Attach` 一处绑完，切房 `revision` 门是它的不变量，界面动作经 `IMessengerStreamSurface` 交出），`PryMessengerWindow.ObserveEventsAsync` 只剩 `Dispatcher.UIThread` 投递。判据 13 例在 `tests/Pry.App.Tests/MessengerEventStreamControllerTests.cs`；其中两例**跑真 `RunAsync`** 而不是手调 handler——handler 级用例只会说"字段为空"，Runner 级用例说的是"界面没被告知"，判据要落在被删的那一行上。证伪跑（每次一个突变、跑满 13 例）：删 `runner.Faulted` 那行 → 3 红；删 `runner.EventReceived` → 2 红；删 `Resumed`、删 `StateChanged` → 各 1 红；`HandleEventAsync`/`HandleResumedAsync`/`HandleStateChanged` 任一处去掉 `IsCurrentRoom` → 旧房间那例红；`conversation.read` 不认、`turn.state` 不渲染 → 对应两例红。**上一轮"删掉 `Faulted` 那行 613 仍全绿"这一后果具体化到此为止**。顺带收紧一处行为：`StateChanged` 此前不查 revision，现在查——旧流的"正在重连"不该盖掉新房间的状态栏。
   **`EventStreamReconnectBanTests` 5 例的处置（同一项的后半）**：保留，但按 QA 的反转接受其定位——它们驱动的 `RemoteConversationTurnManager` 只有 `MainWindow.axaml.cs:225` 一个构造点，而 `MainWindow` 在全仓（含测试）无任何构造点，所以这 5 例是"同一判据的第二形状"，**不是活路径证据**；文件头已按此标注。第四轮我提的"别顺手删掉唯一钉住禁行行为的用例"这条风险由 Runner 15 例 + 呈现 31 例 + 本处 13 例承担，不再落在那 5 例上。
   **`MainWindow`（1608 行）与 `ThemeAppearanceEditor` 的删除次序（实施侧意见；删与不删归作者）**：次序不能倒——先有替代判据，再动宿主。① 落 `MessengerEventStreamController` 与其 13 例（本轮已完成）；② 一次性删 `MainWindow` + `RemoteConversationTurnManager` + 那 5 例 + `ThemeAppearanceEditor`（全仓唯一能选"经典 · 卡片窗口"的控件，见 `QA-P5-03`），**同刀**处置 `PryMessengerSettingsWindow` 那个只会被写成 `false` 的 `LayoutChanged` 死标志与服务端仍接受 `Card` 的校验；③ 回填 §8.1 的 `PLT-002` 行与本节第 2 条。先删宿主再补判据，中间那次提交就是把禁行行为的活证据删掉；先删那 5 例，则会连带删掉"追加渲染不补历史"这条不变量的唯一记录处。`RemoteConversationTurnManager.cs:87` 那句"刻意不挂 `Resumed`"的 caveat 登记在此，不必留给下一个人重新发现：今天宿主不可达所以不违反任何判据，一旦有人重新接线而不改这条注释，`PLT-006` 的"客户端以领域历史为准重取"在那条路径上就不成立；收敛做法是在追加侧按消息 Id 去重，属②那一刀的一部分。
3. 发现：独立进程模式下，客户端仅凭 `runtime.json` 完成连接；陈旧文件（假 `pid`）被覆盖且不导致误连。
   **M0-d 进度（2026-09-21）**：文件写出/删除、字段与真实绑定端口一致、陈旧文件被覆盖且只留一条恢复记录、带文件里那枚令牌的请求确实过门，都由 `Pry.Api.Tests/SessionTokenTests` 断言。**本项按 §8 尾注属"需要进程级测试"，此处不能结案**：现有用例在同一测试进程里起两个后端、验证端口互不相同且各自可达，但"另一个真进程只读 `runtime.json` 就连上"要求 `Pry.App` 之外的调用方，属 QA 阶段②的 `PLT-003`。
   **`PLT-003` 读侧收尾（2026-09-21 第四轮 Release Blockers 第 2 条，作者裁定"1.0 需要独立进程客户端"后落地）**：本项的客户端半片补齐，`PLT-003` 的两半今天都有用例。新增 `Pry.Client.RuntimeDiscovery`（`TryRead` / `TryConnectAsync`），判据是**不确定就不接**：缺席、半截 JSON、不是对象、空文件、端口 `<1` 或 `>65535`、令牌为空、`pid` 已退出、`pid` 是本进程——全部返回 `null`；未知字段忽略（文件格式只增不改不删）；探测走豁免的 `GET /api/v1/info`，所以"进程还在但 HTTP 面已不再服务"也不接。**版本兼容不在读侧判定**，仍归协商中间件，避免两处事实源。用例分两层：`tests/Pry.Client.Tests/RuntimeDiscoveryTests`（13 例，含"存活但无人服务 ⇒ 拒连"与"令牌不进 `ToString()`"），`tests/Pry.Api.Tests/RuntimeDiscoveryProcessTests`（真起一个 `dotnet exec Pry.Api.dll` 子进程，只凭临时目录里那份文件连上：`descriptor.Pid == child.Id`、端口由 Kestrel 自选且 `!= 5078`、发现的 `HttpClient.BaseAddress.Port` 等于文件里的端口、握手版本与 `runtime-discovery` 能力对得上、受门端点真读到数据、匿名请求仍 `401`）。证伪运行：注释掉 `TryConnectAsync` 里附加令牌那一行后，进程级用例在"受令牌门保护的端点"处红（`401 urn:pry:error:unauthorized`），随后还原——这条用例判的确实是"令牌来自文件且真的过门"，不是"端口能连通"。产品侧接线：`Pry.App` 启动改为**先发现后内嵌**（`RuntimeDiscovery.TryConnectAsync() ?? StartEmbeddedBackendAsync()`），界面层三处硬编码 `http://127.0.0.1:5078/` 全部移除（无参构造器改用 `PryBackendClient.Unattached()` 占位，它指向端口 0，意外触网只会失败而不会连上"恰好在 5078 上的某个进程"），`5078` 只剩服务端 `Program.cs` 默认值与测试夹具。接受的代价（作者已知）：第二个界面实例会接上第一个实例的后端，因此退出提示按是否拥有后端分支，不再一律宣称"退出会关闭内嵌后端"。**本项仍不在此结案**的部分：`App.InitializeDesktopAsync` 这条真实启动顺序没有自动化覆盖（它需要 Avalonia 生命周期）。**（2026-09-21 第五轮返工更正）**：上一句原写"与第 2 条末尾'应用真正跑的那条循环无用例'同源，随 UI-027 的 Runner 收敛一并处理"——那个同源今天已经断开：那条循环的接线有 13 例（`MessengerEventStreamControllerTests`，见第 2 条末尾 `QA-P5-01` 的结案注），而启动顺序仍无判据，只能靠 Avalonia 生命周期设施或人工走查，QA 已把它单列在本轮"仍未覆盖"清单；`PLT-003` 的复判归 QA。
4. 鉴权：缺 token 的 HTTP 与 WS 握手均 `401`；日志与响应不含 token 明文。另按 §6 第 7 条断言**默认拒绝**：新增/未列入豁免白名单的路由在带不上 token 时不得返回业务数据（用一条"遍历已注册端点、缺 token 必须 401 或在白名单内"的测试守住，避免以后加端点时静默漏保护）。
   **M0-d 进度（2026-09-21）**：HTTP 侧整项已断言——端点遍历从 `EndpointDataSource` 取真实注册表（不手写清单），缺令牌一律 `401 urn:pry:error:unauthorized`、豁免面恰好 `GET /health` 与 `GET /api/v1/info` 且返回 200、遍历规模低于阈值时测试自曝为退化；缺/错/非法 Base64/长度不符四种输入除 `traceId` 外响应体完全一致；被拒的写不落 `protocol_events`；token 既不在任何响应文本里也不在捕获到的日志行里，且用崩溃恢复记录先证明日志面确实被接上。WS 握手侧 NotApplicable（1.0 尚无 WS，落地时必须复用同一中间件）。
5. 游标跨重启：Core 停止/启动后，客户端以原 `after` 重连不丢不重；窗口被裁剪时收到 `stream.resumed` 并触发历史重取。**判据细化为 §4.1 第 10 条**（含"会话内序号空洞合法、客户端不得据空洞自判丢失"、"电平类事件不被重放"、"删消息后传输层不留明文副本"、"裁剪前后领域历史逐字节不变"）。
   **M0-c 进度（2026-09-21，服务端侧已断言）**：`Pry.Api.Tests/ProtocolEventCursorTests` 用真实 Kestrel 断言"越界游标恰一帧 `buffer_expired` 后照常补投 / 窗口内静默补投 / `after=-1` 永不判过期 / 跨进程重启原游标可重放且 `features[]` 已宣告 / HTTP 删消息后明文在游标流里不可读 / 保留配置越界被夹回可用区间"，`Pry.Core.Tests/ProtocolEventJournalTests` 断言序号全局单调、分箱、三条裁剪轴、遮蔽与回滚、级联、`journal ≠ Memory` 读边界与 schema v9。**整项不能由这些测试结案**：第 10 条的"客户端不得据空洞自判丢失"与"收到宣告后重取历史"属客户端义务，而统一 Runner 仍在 `codex/client-error-reconnect` worktree（见第 6 条），主工作区的现有客户端把未知 `type` 按前向兼容忽略、并在启动时以数据库投影为权威重取，因此今天不存在数据丢失回归，但这条判据要等 UI-027 收敛后由 QA 复判。
   **（2026-09-21 第五轮更正）**：上一条"统一 Runner 仍在 worktree"已过期。`ConversationEventStreamRunner` 与 `PryErrorPresentation` 已进主工作区，本条的客户端义务——按最后投递序号续订、重连后先补取历史——已有用例，见第 2 条末尾的收敛注与 §8.1 的 `PLT-005`/`PLT-006` 两行；复判归 QA，本项仍不在此结案。
6. 断连：杀掉事件流后由 `ConversationEventStreamRunner` 按退避重连并补齐中断期间的事件；`typing`/心跳类瞬态帧允许丢；游标被裁剪时客户端收到 `stream.resumed` 并触发历史重取；半开连接（服务端不发也不关）在一个心跳周期内被客户端检出。
   **M0-b 进度（2026-09-21）**："一个心跳周期"按实现事实修正为**两个心跳周期 + 抖动余量**（`LivenessTimeoutMs = 2 × 15s + 2s`）：判定阈值若等于单个周期，服务端一次刷写延迟就会把安静会话误判断连，检出上界因此是约 32s 而不是 15s——已用 `Pry.Client.Tests/EventStreamLivenessTests` 断言"零字节被判死 / 密于心率的心跳续命 / 稀于窗口的心跳仍死"三条，以及 `Pry.Api.Tests/EventStreamHeartbeatTests` 用真实回环 HTTP 断言 `:ping` 确实穿过刷写到达客户端。仍未结案的部分：主工作区还没有 `ConversationEventStreamRunner`（它在 `codex/client-error-reconnect` worktree，见本节第 4 条），本片的存活信号落在传输层 `PryBackendClient`，两套旧重连循环（`RemoteConversationTurnManager`、`PryMessengerWindow`）尚未收敛到统一 Runner，"补齐中断期间的事件"要等 M0-c 的持久游标。
   **（2026-09-21 第五轮更正）**：上一条整体过期，按事实重记：Runner 已在主工作区且两处旧循环都已收敛为它的消费方（上一句的"见本节第 4 条"也是错指——第 4 条是鉴权，Runner 的事在第 2、6 条），持久游标随 M0-c 落地，因此"补齐中断期间的事件"今天成立。仍开的是 OS 级真断链，见 §8.1 的 `PLT-007` 行；上一句里的"活窗口接线"已按 `QA-P5-01` 在第 2 条末尾结案（接线抽成 `MessengerEventStreamController`，13 例逐个突变逐个红）。
7. 大媒体：一张 >2MB 图片走完整回合后，事件帧内无 base64、无绝对路径，只有 `mediaId` 与相对 URL。
8. 未知事件兼容：客户端遇到未识别 `type` 时忽略且不断连（前向兼容回归）。
9. 停机：正常退出后 `runtime.json` 消失、无残留 `llama-server` 进程。
   **M0-d 进度（2026-09-21）**：前半已断言——`StopAsync` 后文件不存在，且只有内容仍匹配本进程 `pid`+`port` 时才删，后继实例的入口不会被抹掉。后半（无残留 `llama-server`）不属本片：它是 `ModelProcessRegistry`/`EmbeddingProcessRegistry` 的停机冲刷问题，挂在 PLAT-001。

以上 1、2、5、6、7、8 用 `Pry.Api.Tests` 真实回环 HTTP 断言（沿用现有内嵌后端测试方式），3、4、9 需要进程级测试，4 另加"token 不进日志"的断言。

## 8.1 验收与移交（本节判据如何走完 Phase 1）

上一节是**测试设计**，不是验收。验收链路分四段，权限与交付物各自固定；本方案的所有者（Core Platform）只走第一段。

| 段 | 谁 | 交给他的东西 | 判定 |
|---|---|---|---|
| ① 实施 | Core Platform（PLAT-005 负责人） | 每条判据对应的测试文件与用例名、`PROJECT_PLAN.md` 第 6 节门禁的**时点化**运行数字（含"工作区快照，非统一提交"标注）、未覆盖边界清单；文档与本节判据同步 | 我把 PLAT-005 台账行改"已完成"的前提是：代码已在**统一提交**上、②的场景不为 `Blocked`（缺哪条就写缺哪条，不做"大体完成"） |
| ② 固定 Eval | EVAL-001（QA / Evaluation） | 下表 13 条永久场景（ID 与 `scenario` 文本以 `docs/eval/scenarios.json` 为准，`suiteVersion 1.1.2`；本表是它的派生视图，见下）+ 其自动化映射 | **场景 ID 由 QA 治理**，我不自行写入 catalog；QA 可改判 ID、类别与 `severity`。按该 catalog 的约定：`Pass` 不由"映射测试通过"自动得出，未执行即 `NotRun`，未实现即 `Blocked` + 负责人 + 恢复前提，跳过与无样本不计入 `Pass` |
| ③ 集成基线复验 | RT-001-INT（Architecture / Integration / Release） | 统一可审查提交的哈希 + 从该提交重跑的第 6 节门禁结果，回填 `docs/phase1-platform-baseline.md` 第 6 节 | 该 Owner 的记录已自限"仅复验完成，不据此宣告 Phase 1 完成"，本节沿用，不扩大其权限 |
| ④ 发行输入 | RT-008（Phase 8） | 协议冻结清单（版本、`features[]`、事件目录、错误码）、配置项清单（`Pry:EventHeartbeatMs`、§4.1 第 4 条三个保留键及其夹取区间与"只走环境变量/测试注入"的口径）、`runtime.json` 与本机 token 的安全边界、已知限制 | PLAT-005 自身**不是**发布门槛，但它是"统一基线"的前置：本节判据未过则 Phase 1 不结案，RT-008 不得声称有可发基线 |

**阶段指针不在我手里**：按 §5.0"解锁方式"，Phase 1 → Phase 2 的推进只能由作者或守门人在本计划改指针并追加交接记录；我在台账与交接记录里的"已完成"只代表本任务判据成立，不构成阶段结案。

永久场景（**不再是草案**）：`docs/eval/scenarios.json` 已在 2026-09-21 第二轮采纳本节的草案，并按不同的负责人拆开与重排；本表自此只是该目录的**派生视图**。对齐约定（QA 同步记在 `docs/eval/README.md`）：**ID 与 scenario 文本一律以目录为准**（`suiteVersion 1.1.2`，`PLT-*` 共 13 条，2026-09-22 第六轮），跨文档引用不得只看 ID。本节此前用的正是采纳前的编号，同一个 `PLT-006` 在两份文档里指向两件负责人都不同的事（客户端重取历史 vs 大媒体引用），`eval-QA-P1ACC-0921E` 据此记为 `QA-P5-02`。判定（`Pass`/`Blocked`/`NotRun`）归 EVAL-001/QA，最后一列只记实施侧在当时那版工作区里看到的事实。

| 目录 ID | 目录 scenario 文本 | 判据来源 | severity | 实施侧现状（2026-09-21 第五轮工作区快照，非判定） |
|---|---|---|---|---|
| PLT-001 | 只允许回环监听 | §8 第 1 条 | High | 正反两向都有用例：`LoopbackBindingTests` 让四种非回环 `Pry:Url` 在 `BuildAsync` 内失败且数据目录尚未创建，`127.0.0.1:0` 作正向对照；真正起监听的事实由 `SessionTokenTests`/`EmbeddedBackendEndpointTests` 的真实 Kestrel 用例提供 |
| PLT-002 | 协议主版本不匹配时停在明确提示 | §8 第 2 条 | High | 服务端宣告 → 契约 `retryable` → 客户端禁令 → 停止重连整链到位；第五轮起禁令只在 `ConversationEventStreamRunner` 一处读，`SingleBanJudgeBoundaryTests` 防界面层再加一份。**第五轮返工补上最后一环**：活窗口那条循环的接线由 `MessengerEventStreamControllerTests` 13 例钉住，删掉四个回调里的任何一行都有对应用例转红；`EventStreamReconnectBanTests` 那 5 例改标为"驱动不可达宿主（`MainWindow` 无构造点）的第二形状"，不再是活路径证据 |
| PLT-003 | 独立进程仅凭发现文件连上 | §8 第 3 条 | High | 写侧 `SessionTokenTests` 11 例；读侧 `RuntimeDiscoveryTests` 13 例 + `RuntimeDiscoveryProcessTests` 真起子进程、只凭那份文件连上（含"匿名请求仍 `401`"与"令牌不进 `ToString()`"）；界面层三处 `5078` 字面量已清。未覆盖：`App.InitializeDesktopAsync` 的启动顺序 |
| PLT-004 | 鉴权默认拒绝且 token 不外泄 | §8 第 4 条 + §6 第 7、8 条 | **Critical** | 端点遍历取真实 `EndpointDataSource` 而非手写清单，缺令牌一律 `401 urn:pry:error:unauthorized`，豁免面恰为 `GET /health` 与 `GET /api/v1/info`；缺/错/非法 Base64/长度不符四种输入除 `traceId` 外响应体完全一致；被拒的写不落 `protocol_events`；日志与响应均无明文。severity 系第二轮按第十二章隐私红线从 High 上调，**本表旧值偏低，别照旧表读** |
| PLT-005 | 事件游标跨重启不丢不重 | §8 第 5 条 | High | 服务端侧 `ProtocolEventCursorTests`（真实 Kestrel）+ `ProtocolEventJournalTests` 已断言；客户端自第五轮起按最后投递序号续订，不再以 `after=-1` 重放整段日志。真进程重启的复判仍归 QA |
| PLT-006 | 保留窗口被裁剪后客户端先重取历史 | §8 第 5、6 条 + §4.1 第 10 条 | High | 客户端义务第五轮进主工作区：重连成功后的第一条事件之前先补取一次历史。**与目录字面判据的一处偏差不隐瞒**：Runner 不解析 `stream.resumed{reason:"buffer_expired"}` 的语义，任何一次成功重订都补取——这是超集而非漏，"按宣告才重取"这个更强的形状没有实现。另 `RemoteConversationTurnManager` 刻意**不挂** `Resumed`（它的回复走追加渲染，补历史会画两遍），其宿主不可达；`QA-P5-01` 已按"补用例"那条路线结案，该 caveat 与 `MainWindow`/`ThemeAppearanceEditor` 的删除次序一并记在 §8 第 2 条末尾，不留给下一个人重新发现 |
| PLT-007 | 半开连接有界检出 | §8 第 6 条 | Medium | 阈值是实现事实 `2 × 心跳周期 + 抖动余量`（约 32s，不是 15s）：`EventStreamLivenessTests` 三条 + `EventStreamHeartbeatTests` 真实回环证明 `:ping` 确实穿过刷写。OS 级真断链未做 |
| PLT-008 | 取消订阅与关流是两条可区分路径 | §8 第 6 条 + §4.1 第 10 条 | High | 两条各有一例钉住：`Cancellation_ends_the_loop_without_a_fault`（调用方取消⇒静默退出、不报故障）与 `A_stream_that_closes_without_events_retries_instead_of_spinning`（收到零事件即结束⇒按失败退避而非空转）；Provider 侧主动取消仍以 `OperationCanceledException` 浮出（`BackendRequestBudgetTests`） |
| PLT-009 | 电平类相位帧只在活订阅内有序 | §4.1 第 10 条 | Medium | 服务端不入库、不重放已断言；客户端**没有**"序号连续即未丢"的推断通道——Runner 只把游标取 `Math.Max`，丢失判定完全依赖服务端宣告，因此该条的禁止结果无法被客户端违反 |
| PLT-010 | 大媒体只以引用穿过事件帧 | §8 第 7 条 | Medium | `Blocked`：收敛点在 PLAT-007，§5.0 冻结中。**`QA-P5-02` 的撞号来源就是本表旧编号把这条写成 `PLT-006`** |
| PLT-011 | 未识别事件类型保持前向兼容 | §8 第 8 条 | High | 传输层把未知 `type` 原样上抛而不断连（`Comment_frames_do_not_disturb_event_parsing_or_break_on_unknown_types`），消费侧按类型不匹配即忽略——第五轮返工后这一句在接线层也有用例（`An_unrecognized_event_changes_nothing_and_breaks_nothing`：不渲染、不报错、更不被当成断连）；发现文件格式同理由"未知字段忽略"守住，新旧构建的升级顺序不被读侧锁死 |
| PLT-012 | 退出后不留发现文件与残留子进程 | §8 第 9 条 | Medium | 前半 M0-d 已断言：`StopAsync` 后文件消失，且只在内容仍匹配本进程 `pid`+`port` 时才删（后继实例的入口不会被抹掉），陈旧文件被覆盖并留恰一条恢复记录；"无残留 `llama-server`"属 PLAT-001 的停机冲刷，仍 `Blocked` |

**一条 QA 侧的硬提醒**（来自 `docs/eval/README.md` 自己的措辞）：该 catalog 明确写着"SSE 解析不证明断线重连"。因此 **`PLT-005`（游标跨重启）与 `PLT-007`（半开检出）不得因 `Pry.Api.Tests`/`Pry.Client.Tests` 变绿而记 `Pass`**——必须落到真实进程重启与真机断链，其余边界按 `NotRun` 或 `Inconclusive` 如实记录。这两条在旧表里写作 `PLT-004`、`PLT-005`，即 `QA-P5-02` 的另一处撞号；改完后旧 ID 不再出现在本节。

**2026-09-22 更正（`eval-QA-P1ACC-0922` / `QA-P6-02`，作者裁定口径不改判）**：上面那句"不得记 `Pass`"**超出了目录文本给这两条场景定的初态与输入**，因此按现文本作废：`PLT-007` 的 `initialState` 写的就是"服务端建立事件流后既不发也不关"，零字节、无 FIN/RST 的静默流是它的**直接**观测对象而非代理；`PLT-005` 的 `input` 写"Core 停止后重启"，而那条用例确实 `first.Stop()` 后用同一数据目录起第二个真 Kestrel 宿主并以原 `after` 重放。两条按目录文本已满足，QA 保持 `Pass`。本提醒说对的部分是另一件事：**没有任何一条用例把"真 OS 进程边界 + 真客户端 + 真界面"一次跑完**，那属本节阶段表之外的"真机端到端"一项（QA 第六轮 Release Blocker 第 6 条），不属这两条场景的语义。若作者日后要把 OS 级证据定为判据，那是**语义修订**——要改 `scenario` 文本并升 `suiteVersion`，不能像本条这样在现文本之上加更严口径；那种做法恰好是 `QA-P5-02` 的成因。派生视图与目录不一致时以目录为准，本条即为一次实证。

## 9. 明确不做

- 不实现 Satori Gateway、外部 IM、`IExternalMessenger`/`IExternalEndpoint`/`IExternalIdentityResolver` 的任何实现体；Resource 与身份边界只在第 2 节以不变量形式预留。
- 不做 Hosted Core、多 Core 同步、TLS/证书、限流与来源策略（API-007 条件任务）。
- 不在 Phase 1 引入插件进程隔离、RPC Sandbox 或插件市场。
- 不因为"要上 WebSocket"而提前改动 HTTP 命令面的 DTO 语义。
