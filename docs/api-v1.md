# Pry Backend API v1

主动话题契约见 [主动话题调度](proactive-scheduling-api.md)：包含策略、活动上报、状态查询、取消、持久化去重和 SSE 投递；首版仅复用已加载的文字模型。

基础地址：`http://127.0.0.1:<随机端口>`。端口不固定：界面用 `http://127.0.0.1:0` 让 Kestrel 自选并占住该 socket，再从绑定结果读回端口，独立进程从数据目录下的 `runtime.json` 发现它（见下节）。请求和响应使用 UTF-8 JSON，字段名采用 camelCase，枚举使用字符串。时间为 ISO 8601 UTC/带偏移时间。本机单用户，但**不是无认证**：除 `GET /health` 与 `GET /api/v1/info` 之外，每个请求必须带本机会话令牌，缺令牌一律 `401`（契约见 [协议迁移方案](protocol-versioning-and-websocket-migration.md) 第 6 节）。

## 本机会话令牌与进程发现（PLAT-005 M0-d）

语义边界：这是**本机进程之间的接缝**，用来挡住同机上其他进程调用有副作用的接口；它不是账号体系，不是 API-007 的网络认证栈，客户端不得把它当登录态持久化，跨重启以 `runtime.json` 现读为准。

- 令牌是每个后端进程启动时随机生成的 32 字节秘密（Base64 放在请求头里），不来自配置、不来自用户、不落进数据库，进程退出即失效。
- 请求头：`X-Pry-Token: <base64>`。服务端做**等时比较**（`CryptographicOperations.FixedTimeEquals`），非法 Base64、长度不符、内容不符都得到同一个 `401`，响应体除 `traceId` 外一字不差——响应不得告诉调用方"你带了一个像样的令牌但它是错的"。
- 缺令牌或令牌无效：`401`，`urn:pry:error:unauthorized`，扩展字段 `code`、`traceId`、`retryable: false`。令牌门在协议协商中间件**之前**执行，所以未授权探测学不到服务端的协议版本与能力清单；`/api/v1/info` 因豁免仍可读，这正是版本不匹配的客户端需要的。
- 豁免面只有两条且必须整段相等（不是前缀匹配）：`GET /health`、`GET /api/v1/info`。默认拒绝的意思是"新端点忘记登记就被保护"，而不是"忘记登记就暴露"。
- 令牌明文不得出现在任何响应体、响应头、日志行与异常文本里；测试用可枚举的内存日志面证伪（`SessionTokenTests`）。
- 发现文件：`<DataDirectory>/runtime.json`，字段 `{ pid, port, token, protocolVersion, schemaVersion, startedAt }`，由 `RuntimeDescriptorService` 挂在 `ApplicationStarted`/`ApplicationStopping` 上写与删（宿主服务的启动顺序不在契约里，不依赖它）。先写临时名再原子替换；退出时只有内容仍属于本进程（`pid` + `port` 都对得上）才删除，避免抹掉后继实例的入口。
- 发现顺序：同进程内嵌时**直接读同一个服务对象**（`app.BoundUrl()` / `app.CreateBackendHttpClient()`），不去读自己刚写的文件；独立进程读 `runtime.json`，校验 `pid` 存活与端口一致后再带令牌发请求。
- 读侧实现是 `Pry.Client.RuntimeDiscovery`（`TryRead` / `TryConnectAsync`），判据是"**不确定就不接**"：文件缺席、半截 JSON、不是对象、端口越界（`<1` 或 `>65535`）、令牌为空、`pid` 已退出、以及 `pid` 就是本进程，全部返回 `null` 而不是猜一个入口——连错比连不上更难诊断，因为陈旧文件里的端口可能已被同机另一个进程占用。未知字段照常忽略（跨进程文件格式只增不改不删）。探测请求走豁免的 `GET /api/v1/info`：那个进程还活着但 HTTP 面已不再服务时也不接。版本是否兼容**不在读侧判定**，仍由协商中间件决定（避免两处事实源）。
- `Pry.App` 的启动顺序因此是"先发现、后内嵌"：`BackendLocator.TryConnectAsync(hostBuilder) ?? StartEmbeddedBackendAsync(hostBuilder)`，而 `hostBuilder` 由 `BackendApplication.CreateHostBuilder()` 建**一次**、两条路径共用同一个配置根。这意味着第二个界面实例会接上第一个实例的后端，而不是再起一个宿主去争同一份 `memory.db`。后果要说清：接上别人的后端时，退出本窗口不会关掉那个进程，退出提示按 `_backend` 是否为空分支。
- 发现文件所在的目录**读写两侧只有一个解析点**（`RuntimeDescriptorService.ResolveDataDirectory`）：界面不再自认默认值，未配置时两边同样落到 `PryProtocol.DefaultDataDirectory`。跨进程要让界面发现一个不在默认目录里的后端，用环境变量 `Pry__DataDirectory`——`--Pry:DataDirectory` 只抵达被启动的那一个进程，界面拿不到它，这是命令行不共享而不是缺陷。同一次环境设置会同时抵达两个进程，所以在隔离目录里跑"一个后端 + 一个界面"这一对不需要碰 `%LOCALAPPDATA%`（`Pry__ModelsDirectory` 一并给出，否则子进程会去扫真实用户的资源目录）。这三条边界由 `DiscoveryDataDirectoryBoundaryTests` 逐条扫源码钉住：漏传实参、界面另建发现入口、客户端复制目录推断，任一种形状都直接转红。
- 界面层不再自带任何备用端口，`5078` 只剩服务端 `Pry:Url` 默认值与测试夹具两处。
- 上一实例遗留（崩溃或未走停机路径）的描述文件被覆盖，并留一条崩溃恢复记录，只写 `pid` 与状态，不复述旧令牌。
- 已知限制：令牌保护的是 HTTP 面，`1.0` 尚无 WS；未来 WS 握手必须携带同一令牌（§6 第 3 条）。文件权限沿用数据目录现有的当前用户 ACL，未额外收紧。

## 协议版本协商（PLAT-005 M0）

版本常量的唯一来源是 `Pry.Contracts.PryProtocol`，服务端、客户端与诊断界面都读取它，不得复制字面量。当前 `major.minor` 为 `1.0`，服务端接受的最低客户端版本同为 `1.0`。传输层的保活周期与客户端存活判定（`HeartbeatIntervalMs`、`LivenessTimeoutMs`）也放在同一处，理由相同：这两个值只有服务端与客户端同时按同一数字实现才有意义。

- `GET /api/v1/info`：200，返回 `{ "protocolVersion": "1.0", "minSupportedClientVersion": "1.0", "coreVersion": "0.0.2", "schemaVersion": 9, "features": ["protocol-version-negotiation", "http-command-plane", "sse-event-plane", "durable-event-cursor", "session-token", "runtime-discovery"] }`。`schemaVersion` 是数据库结构版本（`schema_info` 表），供诊断使用；v9 为协议事件日志 `protocol_events` 建表。`features` 只描述**当前构建真实具备**的能力，客户端据此降级，不得从版本号推测；尚不存在的能力（如 `websocket-events`）出现即视为契约违规。
- 每个 `api/**` 响应带请求头 `Pry-Protocol-Version: <服务端版本>`；客户端为每个请求带 `Pry-Client-Protocol: <客户端版本>`。
- 协商规则：`major` 必须相等，且客户端 `minor` 不得高于服务端 `minor`。不满足时返回 `426 Upgrade Required`，`urn:pry:error:protocol_mismatch`，扩展字段含 `clientProtocolVersion`、`serverProtocolVersion`、`minSupportedClientVersion` 与 `retryable: false`。**这不是可静默降级或重试的错误**：界面必须停在明确的版本不匹配提示，不能当作网络故障无限重连。`retryable: false` 已随 `PryBackendException.Retryable` 透传到客户端边界（QA-P2-01），禁行的一侧不再是服务端独白；两个事件流循环（`RemoteConversationTurnManager`、`PryMessengerWindow`）现在读它并对禁行停止退避、停在明确提示（`PryBackendException.RetryForbidden`），UI-027 的统一 Runner 合流后接管同一判据。
- 例外：`GET /api/v1/info` 永不因协商失败被拒，否则不匹配的客户端将无法读取服务端支持的版本区间。
- 未携带 `Pry-Client-Protocol` 的请求按“版本化上线前的已发布客户端”处理并正常服务，避免升级即断连；畸形取值（非 `major.minor`）按不支持处理。
- 兼容策略：事件类型与 DTO 字段**只增不改不删**属于 `minor`，因此客户端必须忽略未知字段与未知事件类型；任何语义变更必须换 `major`。
- `/health` 保留为未版本化的存活探针，返回 200 只证明 Web 主机可达，不得用于判断协议兼容。
- 现状差异（M1 收敛）：426 与其他错误体都沿用 `ApiExceptionMiddleware` 的 ProblemDetails 字段，但传输类型为 `application/json` 而非下节所述的 `application/problem+json`；客户端按字段解析，不依赖媒体类型。

## 持久化已读与未读（COMP-008）

本机单用户共享一份阅读状态，不按窗口或客户端分裂。所有 `role=assistant` 的现存消息都参与未读计数，
包括普通回复、主动消息、互动、倾听提示及 legacy；用户和系统消息不计数。阅读状态不等同于主动话题的“用户已回应”。

- `GET /api/v1/conversations/{id}/read-state`：200，返回 `conversationId`、`lastReadMessageId`、`unreadCount`。
- `PUT /api/v1/conversations/{id}/read-state`：请求 `{ "throughMessageId": 123 }`，200 返回相同投影。
  水位只能前进；重复或较旧水位是成功的无操作，不回退。不接受“把到达服务端时的全部消息设已读”的隐式操作。
  前进目标必须是该会话内仍存在的消息（用户消息也可作为顺序边界），不允许未来 ID 或其他会话 ID。
  小于0或缺少 throughMessageId：400 validation_error；会话不存在：404 resource_not_found；
  前进目标不存在、已删除或不属于会话：400 validation_error。0用于读取式无操作，不能清空已读水位。
  不超过当前水位的请求不再要求目标仍存在，从而允许已读目标删除后的幂等重试。
- `GET /api/v1/conversations/unread-summary`：200，返回 `totalUnreadCount`、`unreadConversationCount`，
  覆盖所有会话，不受列表 limit 影响。
- 会话列表及详情追加 `lastMessageId`（空会话为null）、`lastReadMessageId`、`unreadCount`；原字段含义不变。

未读数由同一数据库快照中的 `assistant AND id > lastReadMessageId` 计算，并有会话/角色/ID索引。
标记已读的校验、单调更新和响应统计在同一写事务中完成，不改变会话 updatedAt 或列表排序。
请求只确认 throughMessageId 及以前；其后新消息仍未读。读取历史、打开 SSE 或发送用户消息都不会自动标记已读。

水位推进后发布会话 SSE `conversation.read`，data 为 conversationId、lastReadMessageId；同值重试不重复发布。
多个窗口收到该事件应刷新未读投影并忽略水位倒退；消息新增/删除/撤销事件也应刷新。
事件在保留窗口内可跨重连、跨进程重启重放（见"聊天回合与事件"一节），但启动时仍以数据库投影为权威：
先 GET 会话投影/未读汇总再接事件，避免把"补投窗口已被裁掉"当成界面缺数据。
前端只能在消息实际呈现且符合阅读判定时提交所见最高消息 ID，不能在预取、后台通知或仅收到 SSE 时清零。
标记已读是用户活动，不应把它作为定时心跳重复调用；需要报告空闲时使用活动接口。

schema v7 为 conversations 增加 last_read_message_id（非空、默认0，无消息外键）。旧数据没有阅读记录，
默认所有现存助手消息未读；不推测历史阅读行为。删除消息不回退水位，未读数按现存消息减少；撤销恢复的消息按原ID
与当前水位重新计数。删除整个会话同时移除阅读状态。迁移可重复执行，回滚旧二进制可忽略新增列和索引，勿删除数据库。

## 通用错误格式

错误使用 `application/problem+json` 语义：

```json
{
  "type": "urn:pry:error:validation_error",
  "title": "请求参数不合法",
  "status": 400,
  "detail": "不能为空",
  "instance": "/api/v1/conversations",
  "code": "validation_error",
  "traceId": "0HN...",
  "field": "title"
}
```

- `retryable`（QA-P2-01 之后是线契约的一部分，不是调试附件）：服务端对"重试也不会变好"的错误明确写 `false`（`426 protocol_mismatch`、`401 unauthorized`、`provider_auth`、`context_overflow`）。该字段由 `Pry.Contracts.ApiProblemResponse` 解析、经 `PryBackendException.Retryable` 透传到调用方，**不得在客户端边界被丢弃**。缺席是 `null`，含义是"服务端没有宣告"（旧服务端、非 ProblemDetails 响应、坏 JSON），消费方**不得**把 `null` 当 `false` 用；只有 `false` 才是"停在明确提示、禁止退避重连"的禁令。禁令的消费点是 `PryBackendException.RetryForbidden`（`Retryable == false` 的唯一读法），而**唯一读它的地方是 `Pry.Client.ConversationEventStreamRunner`**：两处界面事件流循环（`RemoteConversationTurnManager`、`PryMessengerWindow`）都改成消费 Runner 的连接状态与终止出口，提示文案由 `PryErrorPresenter` 按 `Code` 给出（UI-027 / `PLT-006` 收敛，第四轮 Release Blockers 第 3 条与 QA-P4-02 的推荐路线）。界面层再出现自己的 `RetryForbidden` 判断会被 `SingleBanJudgeBoundaryTests` 挡下——两处各写一份正是"漏掉一侧无人报警"的通道。
- `400 validation_error`：字段或业务输入不合法。
- `404 resource_not_found`：会话、文件夹或记忆不存在。
- `413 payload_too_large`：上传内容超过允许大小（PLAT-006）。应用层拒绝时扩展字段为 `resource`（当前为 `file`）与 `maximumBytes`；直接撞到传输层天花板的请求由 Kestrel 截断，同样返回 `413 payload_too_large` 但**没有** `maximumBytes`，客户端以自己的 `policy.maximumBytes` 兜底提示。
- `503 cuda_out_of_memory`：GPU 显存不足，释放显存或降低 GPU 层数后可重试。
- `503 model_missing` / `runtime_missing` / `invalid_model`：本地模型或运行库不可用。
- `503 startup_timeout` / `process_exited` / `process_start_failed`：模型服务启动失败；响应中的 `retryable` 表示能否原配置重试。
- Provider 运行时失败（PLAT-001）——模型服务已经回答，但回答本身不行。Core 不认识 HTTP，状态码由 `ApiExceptionMiddleware` 按码映射，客户端**只按 `code` 分支**：
  - `502 provider_auth`：远端拒绝了凭据（含 `403`）。不可重试，提示去改密钥配置，与本机 `401 unauthorized`（会话令牌）是两件事。
  - `429 provider_rate_limited`：远端限流。可重试；Provider 给了等待建议时同时返回 `Retry-After` 头与扩展字段 `retryAfterSeconds`，没给就不猜。
  - `504 provider_timeout`：起头超时（远端 120 秒 / 本地 300 秒）或流式回复空档超过 120 秒。可重试。
  - `503 provider_unavailable`：连不上模型服务，或它回了 5xx。可重试。
  - `502 provider_invalid_response`：内容不是能用的 SSE/JSON。不可重试——同一请求重发只会得到同一份坏响应。
  - `400 context_overflow`：对话超出模型上下文长度。不可重试，用户能做的动作是缩短历史或调低上下文设置，因此不能与其他失败共用一句"稍后再试"。
  - `503 provider_cancelled`：模型调用被取消而调用方的 HTTP 请求还活着。调用方已断开时不写响应体（与 `OperationCanceledException` 同一处理）。
- Provider 的错误体片段只进本机日志且有界（2 KiB，密钥先遮掉）；响应里没有任何上游地址、路由或原始文本。
- `500 internal_error`：未预期服务端错误；客户端展示通用提示并记录 `traceId`，不应显示内部堆栈。

## 后端日志（PLAT-006）

内嵌宿主双击启动时没有控制台，console sink 等于"日志不存在"，崩溃后没有任何可读证据。后端因此自带一个滚动文件 sink，默认写在数据目录旁边：`<DataDirectory>/logs/pry-backend.log`，单档 5 MiB、保留 5 份（`pry-backend.log.1` … `.5`，最老的一份被挤掉）。逐条 flush：进程被强杀时最后一条记录仍在磁盘上，这正是这个 sink 存在的理由。

- 配置键（只走环境变量或测试注入，不进 `appsettings.json`）：`Pry:LogDirectory`、`Pry:LogFileMaximumBytes`（64 KiB～64 MiB）、`Pry:LogFileCount`（1～20）。越界值一律夹取，不因为一个写错的数字让后端起不来。
- sink 自己出错不拖垮后端，也不永久停用（文件被杀软占用是一次性的），原因只向 stderr 报一次。
- **不持有常驻文件句柄**：每条写完即释放。复用同一数据目录的重启测试、备份与卸载都必须能直接删掉这个目录。
- 行格式 `yyyy-MM-dd HH:mm:ss.fff zzz LVL Category: message`，异常原文以 `  | ` 前缀跟在后面。
- 遮蔽发生在渲染之后、落盘之前：本机会话令牌按注册值精确替换，`api_key`/`password`/`token` 之类的字段、`Bearer …`、`sk-…`、URL userinfo 按形态替换为 `[redacted]`；键名保留，否则"哪个字段泄了"这条线索一起没了。短于 8 字符的注册值不登记，免得正常词被遮成噪声。
- 日志不暴露 API 未暴露的东西：Core 只记结构事实（如 `schema 8 → 9`）与文件名，不记数据库绝对路径、用户内容或命令行；模型子进程的 stderr 在离开 `LlamaServerManager` 前就已遮过密钥。
- Core 侧只有注入口：`MemoryDatabase` 与 `LlamaServerManager` 接受可空 `ILogger`，默认 `NullLogger`，因此未接线的调用方行为不变。日志去哪里由宿主决定。

## 健康检查

`GET /health`：正常返回 `200 Healthy`。

`GET /api/v1/runtime`：返回后端状态和当前文字/视觉模型 ID。兼容字段 `state` 为 `starting`、`loading_models`、`ready` 或 `failed`；新增字段 `apiState`、`configurationState` 和 `modelState` 分别表示 API、配置和模型状态，其中 `modelState` 为 `not_loaded`、`loading`、`ready` 或 `failed`。模型按第一次聊天请求延迟加载，因此配置完成后的旧 `state=ready` 不等于模型已经加载。失败时返回安全的 `errorCode`、`error` 与 `retryable`，有界 stderr 只写入本机后端日志，不通过 API 暴露路径、密钥或命令行。模型加载后还返回 `requestedContextSize`、`effectiveContextSize`、`measuredTokensPerSecond` 和 `modelAdjustmentReason`；未降档时原因是 `null`，因容量或性能降档时为稳定错误码（例如 `cuda_out_of_memory` 或 `context_performance_low`）。客户端只展示该结果，不自行测试性能、选择上下文或重启模型。

`POST /api/v1/runtime/retry`：排空活跃会话、释放失败或旧的模型句柄并重新加载配置，使模型可以在释放显存或修正环境后重试，而无需重启整个应用。响应为新的运行状态；模型仍会在下一次聊天请求时按需加载。

`GET /api/v1/runtime/compute-devices`：由后端调用本地推理运行时和 NVIDIA 驱动探测可用设备，返回稳定的设备 ID、名称、是否为集成显卡、`totalMemoryMiB`、`freeMemoryMiB`、`performanceTier`（1–5）、`performanceTierId` 和 `recommendedContextSize`。驱动不能提供显存时两个显存字段为 `null`，后端按型号保守估计档位。桌面端不得直接启动 `llama-server`、调用 `nvidia-smi` 或自行决定模型档位。

## 会话

### 查询列表

`GET /api/v1/conversations?limit=100`

`limit` 会被限制到 1–200。结果按置顶优先、最近更新优先排序。

每个会话同时返回服务端生成的最后消息投影：`lastMessagePreview`（实际正文归一化为单行，最多 160 字符）、`lastMessageRole`、`lastMessageKind`（`text`、`image` 或 `sticker`）和 `lastMessageAt`。空会话的四个字段均为 `null`；图片或表情没有正文时，`lastMessagePreview` 为 `null`，其余元数据照常返回。客户端应依据 `lastMessageKind` 显示本地化占位文案，不得为了填充会话副标题逐个请求消息列表。

### 查询单个会话

`GET /api/v1/conversations/{id}`

### 创建会话

`POST /api/v1/conversations`

```json
{ "characterId": "pry" }
```

`characterId` 可为 `null`。返回 `201`、`Location` 和完整 `ConversationRoom`。会话 ID 由服务端生成，客户端不得自行指定。

### 更新会话

`PATCH /api/v1/conversations/{id}`

```json
{
  "title": "新的标题",
  "isPinned": true,
  "folderId": "folder-...",
  "clearFolder": false
}
```

所有字段可选。移动到根目录时传 `{"clearFolder":true}`，不要用空字符串表示。返回更新后的资源。

### 删除会话

`DELETE /api/v1/conversations/{id}` → `204`。当前为物理删除，连同消息、回复计划及由消息关联的记忆一起按现有事务规则处理；客户端应二次确认。

### 运行时诊断（RT-002，只读）

Developer Mode 与 Eval 用来回答“这一轮现在到底处在什么状态”。两个入口都**只读**：读取不会创建、唤醒或改变运行时，也不会推进任何水位。

- `GET /api/v1/conversations/{id}/runtime-state` → `200`：`conversationId`、`characterId`、`started`、`phase`、`revision`、`epoch`、`conversationProgress`、`userTyping`、`pendingInputCount`、`pendingOutputCount`、`pendingOutputTurns[]`、`activeTurn`、`deliveringTurn`、`lastUserActivity`、`lastCharacterActivity`、`idleForMs`、`evictable`、`invalidatedTurns`、`schedulerBackoffMs`、`replyFrequency`、`triggerMode`、`isGroupChat`、`recentTurns[]`、`persisted{epoch,lastSpeakerWasCharacter,lastActivityAt,consecutiveCharacterTurns}`。
  `started=false` 表示运行时不在内存（从未启动或已被空闲驱逐），此时除 `epoch`/`persisted` 外全部为 `null`；连续性事实仍可读，这正是运行时可被驱逐而对话不丢的原因。`recentTurns[]` 每项为 `{turnId,generationId,fenceEpoch,openedAt,completedAt,outcome,outcomeReason,inputCount,plannedCount,deliveredCount}`。
  `replyFrequency`/`triggerMode`/`isGroupChat` 是**该实例创建时**生效的触发配置（`replyFrequency` 来自 `PATCH /api/v1/preferences` 的 `turnTaking.replyFrequency`，`triggerMode` 取值为 `MessageCount` 或 `ReplyNecessity`，发行默认恒为前者且 `isGroupChat` 恒为 `false`）。它们存在只为让"配置到底走到运行时没有"可以直接读到，不必靠翻轨迹或数消息反推；改偏好会重建内存中的运行时，因此改完之后的第一个会话轮即用新值。
- `GET /api/v1/conversations/{id}/runtime-trace?limit=80` → `200`：`{conversationId,revision,epoch,entries[]}`，`entries[]` 每项为 `{sequence,kind,action,event,turnId,generationId,fenceEpoch,reason,revisionBefore,revisionAfter,at}`。`limit` 缺省或非正数按 80，上限 300（超出按 300 截断，不报错）。
- 错误语义：会话不存在 → `404 resource_not_found`（`conversation '<id>' 不存在`）；会话存在但运行时未启动时读轨迹 → `404 resource_not_found`（`runtime_not_started '<id>' 不存在`），不回空数组冒充“没有发生任何事件”。
- **隐私边界**：诊断负载与轨迹都不含消息正文、提示词或用户输入原文；`TurnRecord.CombinedInput` 在投影时被丢弃，只留 `inputCount`。这些诊断面与所有非豁免端点一样受本机会话令牌门保护，其边界仍是"本机同用户进程"，公开网络部署前必须归其他任务补上真正的授权（同 `docs/phase1-platform-baseline.md` §6.3）。

## 消息

### 查询消息

`GET /api/v1/conversations/{id}/messages?limit=200`

`limit` 限制到 1–500，返回时间正序消息。

### 写入用户消息（迁移期接口）

`POST /api/v1/conversations/{id}/messages`

```json
{
  "role": "User",
  "content": "你好",
  "imagePath": null,
  "stickerId": null
}
```

只接受 `User`；`Assistant` 和 `System` 返回 400。当前不接受 `imagePath`，防止读取任意本机路径。文本最大 20,000 字符。返回 `201` 和持久化后的 `ChatMessage`。

此接口只保存消息，不触发模型回复。前端正式迁移时应使用待实现的聊天 SSE 端点，而不是先调用此端点再调用聊天端点，以免重复写入。

## 聊天回合与事件

前端应先建立事件连接，再提交回合，以便立即显示状态变化。SSE 断线重连时使用最后收到的序号：

`GET /api/v1/conversations/{id}/events?after=123`

`after=-1` 表示从订阅建立时的最新序号开始，只接收随后产生的新事件；桌面客户端切换会话时使用该模式，避免重放已渲染的气泡。

响应为 `text/event-stream`。事件由服务端的持久事件日志（`protocol_events` 表）供给，进程重启后仍可重放；`id` 是跨会话全局单调递增的序号，`event` 可能为：

- `session.ready`：后端会话与角色已就绪。
- `turn.state`：状态为 `UserPending`、`ModelThinking`、`AgentPending`、`AgentSending` 或 `Idle`。
- `message.created`：用户消息已持久化，或助手计划消息已送达。
- `turn.completed`：本轮正常结束，`data.outcome` 为 `Completed` 或 `PartiallyDelivered`，界面据此清除"发送中"。取消与失败仍分别由 `turn.cancelled` 与 `turn.failed` 表达，同一轮不会重复下发。
- `turn.cancelled`：客户端已请求取消。
- `turn.failed`：模型调用失败；只返回稳定错误码和安全提示。`data.code` 自 PLAT-001 起取 Provider 错误码（`provider_auth`、`provider_rate_limited`、`provider_timeout`、`provider_unavailable`、`provider_invalid_response`、`context_overflow`），无法归类的生成期故障仍保留旧兜底码 `reply_generation_failed`；界面按码决定是否提示"改配置"还是"稍后重试"，不得把所有取值合并成一句提示。取消与打断不走这里（`turn.cancelled`）。

保活与半开连接（PLAT-005 M0-b）：

- 事件流空闲满一个心跳周期时，服务端下发一行 SSE 注释 `:ping`。它是传输层帧，不是事件：不带序号、不带 `data`，客户端必须忽略其内容并在协议上保留忽略的权利（未来可能插入其他注释帧）。
- 心跳周期与客户端存活判定同源定义于 `Pry.Contracts.PryProtocol`（`HeartbeatIntervalMs = 15000`，`LivenessTimeoutMs`），两端不得各写一份字面量；调整周期必须同时确认判定倍数仍然覆盖至少两个完整心跳。
- 客户端把"读到任意一行（含注释帧）"视为存活信号，而不是把"收到任意事件"视为存活信号。`PryBackendClient.ReadEventsAsync` 在 `LivenessTimeoutMs`（两个心跳周期加抖动余量）内读不到任何行即判定连接半开，抛出 `TimeoutException` 结束本次订阅，由上层按既有退避重连；没有这条判定时，服务端不发也不关的半开连接会让读取永久阻塞，重连逻辑就没机会触发。检出时间因此有上界（约 32 秒），而不是无限。
- 服务端周期可用配置 `Pry:EventHeartbeatMs` 调节，并被强制夹在 `[1000, LivenessTimeoutMs / 2]` 毫秒内：更密会让注释帧变成忙等，更疏会让安静会话被客户端误判成断连，而客户端不会随单个服务端配置缩小自己的窗口。默认取 `PryProtocol.HeartbeatIntervalMs`。
- 服务端进程结束导致的正常 EOF 仍表现为流自然结束（客户端读到 `null` 行），与半开超时是两条不同的退出路径。
- 重连前的本机存活探测（DEF-P1WALK-02）：一次**被拒**的回环 `connect` 在 Windows 上不是瞬时的——RST 早就回来了，`connect` 完成却压在 TCP 层，本机实测每次约 2.05 秒，八轮重连的墙上时间几乎全是它；而"这个端口此刻有没有监听者"这件事，本机监听表 0–2 毫秒就能回答。于是 `PryBackendClient.OpenEventStreamAsync` 只在**上一次订阅因连接被拒而失败**、且目标是**字面量回环地址**时先问一次 `LoopbackEndpointLiveness.IsListening`：监听表说没人监听，就不再把这次必然被拒的握手打出去，而是抛出与真被拒同形的 `HttpRequestException`（内层是一个真的 `SocketException(ConnectionRefused)`），所以界面分类、失败计数与退避阶梯一概不变。四条边界：① 探测只在"上一次真被拒"之后生效，每个客户端的第一次订阅永远走真连接——否则连"版本不匹配"那句禁令也听不到了；② 凡是答不了的（主机名要先解析、非回环地址可能是远端、通配地址命中、查询本身失败）一律答"有人监听"，把"查不到"读成"没人监听"就是那条被禁止的快速失败；③ 它省的是握手不是轮数，`ConversationStreamOptions.Default` 的连续 8 次与 500 毫秒→15 秒阶梯一字未动（那是 `PLT-002`/`PLT-006` 的判据本体），后端重启在同一端口上时监听表立刻重新出现该端口、下一次照旧真连；④ 半开连接完全不受影响——那种端口一直待在监听表里，判定仍归响应头预算与存活看门狗。配套不变量：界面与发现路径的地址一律写 `127.0.0.1` 而不是 `localhost`（双栈解析会让一次被拒的连接付两份钱，本机实测 4.10 秒，而且主机名会让探测答不了）。判据 12 例见 `DeadLoopbackReconnectTests`，其中耗时那条按"本机第一次真握手的成本"现算而不写死秒数；证伪双向：抽掉探测 ⇒ 第二次尝试即转红（整类 6 秒→22 秒），把探测改成"记过就拒"式的快速失败 ⇒ "端口重新有人监听后回到真连接"那条转红。
- 附带效果：Kestrel 随第一次刷写才发出响应头，所以下发心跳也意味着订阅最迟在一个心跳周期内建立完成。这同时决定了本机客户端的超时不能任意缩小：`PryProtocol.BackendRequestTimeoutMs`（= `LivenessTimeoutMs`，32 秒）必须大于 `HeartbeatIntervalMs`，否则一次正常的空闲订阅会在"还没发响应头"的阶段被读成超时。**2026-09-21 第五轮返工更正（预算改为逐请求声明）**：上一条当时只写了"该值同时是所有本机请求的有界上限"，而 `PryBackendClient` 实际上把这两档预算都装在一个客户端级 `HttpClient.Timeout` 上——那意味着更宽的 4 分钟语音预算永远轮不到做主，32 秒会先跳。现在做主的是**逐请求声明**：每个调用在 `SendWithBudgetAsync` 里用自己的 linked CTS 声明预算（普通请求 `BackendRequestTimeoutMs`，转写 `TranscriptionRequestTimeoutMs`，事件流订阅只对"响应头到位"这一段用 `BackendRequestTimeoutMs`），`HttpClient.Timeout` 退居**兜底**并取 `RequestBudgetCeilingMs`（= 最宽一档 + 15 秒调度余量）。兜底必须严格宽于做主的那一档，否则它顶掉声明；这条关系由两个真装配点（`EmbeddedBackend`、`RuntimeDiscovery.TryConnectAsync`）各一条断言钉住（`SessionTokenTests`、`RuntimeDiscoveryProcessTests`），`PryBackendClient.Unattached` 是端口 0 的占位客户端、请求在连上之前就失败，因此不在其列。PLAT-001 的"任何出站请求都不得无界等待"不变：兜底与逐请求预算都是有界数，`Timeout.InfiniteTimeSpan` 仍由 `BoundedWaitBoundaryTests` 源码扫描禁止。`POST /api/v1/speech/transcriptions` 仍是唯一要等整个模型跑完才回包的调用，差别只是它那 4 分钟现在真的生效。

游标持久化与显式过期（PLAT-005 M0-c，当前构建已实现，`features[]` 已宣告 `durable-event-cursor`）：

- 序号由服务端在 `protocol_events` 表统一分配，**跨会话全局单调**，因此**同一会话内出现序号空洞是正常状态**（别的会话在写、或本会话的事件被保留策略裁掉），客户端不得据空洞自行判定丢事件——丢失只能由服务端通过一帧 `stream.resumed { reason: "buffer_expired", firstRetained, latestSequence }` 宣告，客户端收到必须先重取历史再接事件。该帧的 `id` 是宣告时的最新持久序号，因此跳过它不会让游标倒退。
- `after=-1` 表示"从订阅建立之刻起"，它永远不越界，因此永远不会被判成 `buffer_expired`；桌面端切换会话用该模式。
- 事件按"发生过什么"与"现在处于哪"分两类：前者入库可重放（`message.created`、`conversation.changed`、`conversation.read`、`interaction.performed`、`turn.completed`、`turn.cancelled`、`turn.failed`），后者（`session.ready`、`turn.state`）不入库、不被重放，避免重启后把旧状态推给界面。分箱按**黑名单**走：新增事件类型默认可重放。电平类的帧 `id:` 复用当前最新持久序号，因此它不推进游标，帧格式与上面完全一致。
- 进程重启、运行时重配置与空闲驱逐都不再作废客户端游标：序号与补投窗口在数据库里，内存缓冲只剩"唤醒信号 + 电平事件转发"。
- 保留是显式配置，且**不会永久保存对话明文**：`Pry:ProtocolEventMaxPerConversation`（默认 500）、`Pry:ProtocolEventMaxAgeDays`（默认 7）、`Pry:ProtocolEventMaxTotalRows`（默认 20000）；越界值按区间夹取，配成 0 不会退化成"每次都判过期"。裁剪顺序即优先级：每会话条数 → 年龄 → 总行数。
- 删除消息或删除会话必须让该消息的明文在传输层同样不可读：遮蔽与删除在同一个事务内完成，按负载里的 `messageId` 自动登记，发布者无法遗漏。`protocol_events` 不是历史存储，任何界面、Runtime 或统计都不得把它当会话记录读取（读边界有测试固定）。
- 旧客户端兼容：`after` 参数与帧格式都未变更；改动修掉的是"序号每会话从 1 重启"导致旧游标在爬升期间静默收不到事件的既有缺陷，因此**升级前保存的游标在升级后可能被合法判为已过期**——这正是 `stream.resumed` 存在的理由，客户端只要按上面的规则重取历史即可。细则与理由见 [协议迁移方案](protocol-versioning-and-websocket-migration.md) §4.1。

提交回合：`POST /api/v1/conversations/{id}/turns`

```json
{ "content": "你好", "stickerId": null, "immediate": false, "attachmentIds": [] }
```

返回 `202` 和 `{"messageId":123}`。该消息已经持久化；模型回复异步通过 SSE 到达。普通输入使用 `immediate:false` 以保留合并短输入的节奏，显式立即发送使用 `true`。同一会话的并发输入由后端现有回合状态机合并或打断。

RT-002：输入更新持续 ConversationRuntime。后端可等待更多输入或保持沉默；`202` 不保证一定出现助手消息。显式取消会终止当前回复并清空尚未成轮的输入缓冲，不自动重规划被取消的输入，已持久化历史保留。主动回复也经同一 DeliveryScheduler；开始投递前会出现 `session.ready`/`turn.state`，客户端不能假设下一条事件必为 `message.created`。现有活动报告向活跃 Runtime 转发 typing 与返回语义；无新增事件端点。实现与限制见 [Character Runtime](character-runtime.md)。

取消当前回复：`POST /api/v1/conversations/{id}/turns/cancel` → `202`。取消是幂等操作；如果没有活动回复也不会创建数据。

## 托管媒体与附件

客户端不得提交磁盘路径。先用 `multipart/form-data` 上传，再把服务端返回的资源 ID 放入聊天回合的 `attachmentIds`。

`POST /api/v1/media`，表单字段名为 `file`。成功返回 `201`：

```json
{
  "id": "83ec...",
  "name": "说明.txt",
  "contentType": "text/plain",
  "size": 128,
  "kind": "Text",
  "createdAt": "2026-09-03T11:00:00Z",
  "downloadUrl": "/api/v1/media/83ec.../content"
}
```

约束：

- 单文件硬上限默认 50 MiB（`Pry:MaximumUploadBytes`，可下调到 64 KiB～50 MiB 区间，夹取不报错）。超过上限：`413` + `urn:pry:error:payload_too_large`，扩展字段 `resource` 与 `maximumBytes`，且**不落盘**（在创建目录与写文件之前就被拒）。上限是 50 MiB 而非无限：本地单机应用一旦被 8 GiB 的表单打满，损失是整个后端而不是一个失败的上传。超过 10 MiB 但未超上限时仍返回成功，JSON `warnings` 给中文提示，响应头 `X-Pry-Upload-Warning` 返回稳定 ASCII 码 `large-file`。客户端应先读取 `GET /api/v1/media/policy`，在开始传输前根据 `warningThresholdBytes` 提醒用户。
- 每个聊天回合最多引用 `GET /api/v1/media/policy` 返回的 `maximumAttachmentsPerTurn` 个附件（当前为 6）。客户端不得硬编码不同的数量。
- 支持 PNG、JPEG、GIF、WebP、UTF-8 TXT/MD/CSV 和有效 DOCX。
- 后端同时检查扩展名和文件签名；不信任客户端声明的 Content-Type。
- 原始名称只用于显示与下载，磁盘文件名由服务端随机生成。
- 响应和聊天事件都不会包含服务端磁盘路径。
- 文件始终以流式方式落盘；UTF-8 校验同样分块执行，不会把大文本整体载入内存。用户仍需自行保证本机剩余磁盘空间。

下载：`GET /api/v1/media/{id}/content`，支持 Range 请求。不存在的资源返回 404。

上传策略：`GET /api/v1/media/policy`，返回 `{ "warningThresholdBytes": 10485760, "maximumBytes": 52428800, "maximumAttachmentsPerTurn": 6 }`。`maximumBytes` 的类型仍是 `long?`（`null` 保留给"无应用层上限"这一未实现语义），当前构建恒为服务端实际生效的那个数字；客户端不得把自己的常量当上限，一切以本接口为准。传输层与端点特性的天花板是同一个 50 MiB + 1 MiB multipart 余量，超过天花板的请求由 Kestrel 直接拒掉，错误码同样是 `413 payload_too_large`（少了 `maximumBytes` 扩展字段，见通用错误格式）。

### 孤立媒体保留与清理

后端在启动后及每 6 小时执行一次孤立媒体清理。默认保留期为 7 天，可通过配置键 `Pry:Media:OrphanRetentionDays`（环境变量形式为 `Pry__Media__OrphanRetentionDays`）调整；有效范围为 1～365 天。

清理前会汇总当前背景、用户头像、角色头像、贴纸和消息图片所引用的规范化磁盘路径。只有同时满足以下条件的托管媒体才会删除其内容文件和 JSON 元数据：

- 上传时间早于保留期截止时间；
- 当前不在上述引用集合内；
- 文件仍位于后端托管媒体目录内。

文本、文档和语音上传不会作为长期聊天附件路径保存，处理完成后按同一保留期回收。清理失败只记录不含用户内容的警告日志，不阻止后端启动；成功删除时记录数量、保留天数和释放字节数。前端不得自行扫描或删除后端媒体目录。

## 消息分支变更

- `DELETE /api/v1/conversations/{conversationId}/messages/{messageId}`：用户消息会删除该消息及之后的分支；助手消息只删除自身。返回删除范围及数量。
- `POST /api/v1/conversations/{conversationId}/messages/{messageId}/regenerate`：只接受助手消息，先等待当前回合完全取消，再从对应用户消息重新生成，返回 `202`。
- `POST /api/v1/conversations/{conversationId}/mutations/undo`：撤销最近一次删除或重新生成分支，返回 `204`。
- `POST /api/v1/conversations/{conversationId}/listening-signals`：由后端按会话状态和 30 秒节流规则写入倾听提示，返回创建的助手消息。客户端不得自行伪造或持久化助手消息。

后端会在变更前等待正在运行的回复真正退出，防止删除完成后迟到的助手消息重新写入数据库。每个活跃会话最多保留最近 20 个内存撤销快照；服务重启后撤销栈不保留。

## 会话文件夹

- `GET /api/v1/conversation-folders`
- `POST /api/v1/conversation-folders`，正文 `{"name":"收藏"}`，返回 `201`
- `PATCH /api/v1/conversation-folders/{id}`，正文 `{"name":"新名称"}`，返回 `204`
- `DELETE /api/v1/conversation-folders/{id}`，返回 `204`；其中的会话移到根目录，不删除会话

名称不能为空，最大 100 字符。

## 长期记忆

长期记忆正文仍保存在 SQLite；Embedding 是可丢弃、可重建的旁路索引，不替代正文数据。后端使用关键词与向量混合召回，Embedding 模型或索引不可用时自动退回关键词召回，不阻断对话。

- `GET /api/v1/memory/embedding`：返回独立 Embedding 运行与索引状态。
- `PUT /api/v1/memory/embedding`：以 `{ "enabled": true, "modelId": "模型ID" }` 启用或切换模型；以 `{ "enabled": false, "modelId": null }` 停用。模型必须存在、可用且声明 `embeddings=true`。
- `POST /api/v1/memory/embedding/reindex?clear=false`：幂等触发补建索引；`clear=true` 先删除当前模型的可重建向量，再异步重建，记忆正文不受影响。

状态结构为：

```json
{
  "enabled": true,
  "state": "idle|indexing|ready|degraded|disabled",
  "modelId": "bge-small-zh-v1.5",
  "indexedCount": 120,
  "pendingCount": 3,
  "dimensions": 512,
  "errorCode": null,
  "retryable": false
}
```

`idle` 表示已选择但尚未启动；本地模型在首次索引或检索时按需启动。前端只轮询并展示此状态，不直接启动 `llama-server`、生成向量、读写索引表或据此阻止普通对话。`degraded` 时可以提示“语义记忆暂不可用”，但关键词记忆仍然可用。

数据库 schema 版本升至 3；迁移只执行 `CREATE TABLE/INDEX IF NOT EXISTS`，可以重复运行。旧版本回滚后会忽略该旁路表，记忆正文和关键词索引继续可用。不要在回滚时删除记忆表。若只需释放向量空间，应停用 Embedding 后由后续维护任务清理旁路索引，而不是改动 `memories`。

### 查询

`GET /api/v1/memories?characterId=pry&query=红茶`

`characterId` 必填；`query` 可选。

### 创建

`POST /api/v1/memories`

```json
{
  "characterId": "pry",
  "kind": "preference",
  "summary": "用户喜欢红茶",
  "tags": "饮品,红茶",
  "importance": 0.8,
  "sourceMessageId": null
}
```

返回 `201`。`importance` 必须在 0–1；`kind` 最大 64 字符，`summary` 最大 4,000 字符。

### 全量更新与删除

- `PUT /api/v1/memories/{id}?characterId=pry`，正文包含 `kind`、`summary`、`tags`、`importance`
- `DELETE /api/v1/memories/{id}?characterId=pry` → `204`

`characterId` 同时作为资源归属约束，不能用一个角色 ID 修改另一个角色的记忆。

## 角色卡

- `GET /api/v1/characters`：角色摘要、当前选中状态和可选头像 URL。
- `GET /api/v1/characters/{id}`：完整角色设定；不包含磁盘路径。
- `POST /api/v1/characters`：由服务端生成角色 ID并保存角色卡。
- `PUT /api/v1/characters/{id}`：全量更新角色卡。
- `DELETE /api/v1/characters/{id}`：仅删除用户创建的角色卡。内置角色卡或仍被对话/长期记忆引用的角色返回 `409 resource_conflict`；客户端应提示用户先处理关联数据。
- `GET /api/v1/characters/{id}/avatar`：获取角色头像，支持 Range。

角色头像先通过媒体 API 上传，再在角色请求中传 `avatarMediaId`；传 `clearAvatar:true` 恢复默认头像。结构化角色必须提供 `identity`，Legacy 角色必须提供 `legacySystemPrompt`。配置写入使用临时文件替换；写入前会取消并排空活跃回合。

## 角色互动

### 互动配置

- `GET /api/v1/characters/{characterId}/interaction-profile`
- `PUT /api/v1/characters/{characterId}/interaction-profile`

`PUT` 是一次全量、原子替换。内置角色的发行配置保持只读；首次修改会在用户数据目录保存完整覆盖，响应 `source=user-override`。未修改的内置配置返回 `source=built-in`，没有内置配置的自定义角色返回可编辑的空默认配置。请求和响应都不包含本机路径。

```json
{
  "schemaVersion": 1,
  "characterId": "author-character",
  "cooldownSeconds": 3,
  "pokeReplies": [
    { "id": "poke-hello", "text": "你是在叫我吗？", "enabled": true, "weight": 1 }
  ],
  "actions": [
    {
      "id": "pat_head",
      "displayName": "摸摸头",
      "enabled": true,
      "responseMode": "preset",
      "expressionIntent": "happy",
      "replies": [
        { "id": "pat-head-soft", "text": "嗯……再摸一下也可以。", "enabled": true, "weight": 1 }
      ]
    }
  ],
  "source": "user-override"
}
```

稳定回应模式首版仅为 `preset` 和 `silent`：前者必须有至少一条启用预设才能执行，后者只产生 `interaction.performed` 表现事件、不创建消息。`poke` 是保留动作 ID，固定使用 `pokeReplies`，不得在 `actions` 中重复声明。限制：冷却 0～86400 秒；戳一戳预设最多 100 条；动作最多 50 个；每个动作预设最多 100 条；ID 匹配 `[A-Za-z0-9][A-Za-z0-9_-]{0,127}` 且集合内不重复；显示名最多 100 字符；回应正文 1～500 字符；权重必须为有限的 0.01～100。任何验证失败返回 `400 validation_error`，原配置保持不变。

### 执行互动

`POST /api/v1/conversations/{conversationId}/interactions`

```json
{ "actionId": "poke", "idempotencyKey": "由客户端为一次手势生成的稳定ID" }
```

`characterId` 不由客户端提交，后端只使用会话绑定的角色。成功返回 `202`：

```json
{
  "actionId": "poke",
  "characterId": "author-character",
  "messageId": 321,
  "outcome": "message_created",
  "replayed": false,
  "acceptedAt": "2026-09-13T12:00:00Z",
  "expressionIntent": "happy"
}
```

同一会话内的 `idempotencyKey` 唯一并持久化。使用相同键和相同动作重试返回相同 `messageId`、`acceptedAt` 且 `replayed=true`，不再次写消息或发布事件；相同键改为其他动作返回 `409 idempotency_conflict`。服务端在同一会话内串行化互动与用户回合：存在待合并输入、模型生成或消息发送时返回 `409 turn_busy`。动作不存在、停用、预设为空或会话没有绑定角色返回 `409 interaction_not_available`。冷却返回 `429 interaction_cooldown`，Problem Details 包含 `retryAt`，响应头包含向上取整秒数的 `Retry-After`。

互动消息后来被用户删除时，幂等回执仍保留原 `messageId` 作为历史执行结果，不会因重试重新创建消息；客户端应以当前消息历史判断该消息是否仍存在。删除会话会级联删除该会话的幂等记录。

预设回应和幂等命令在同一 SQLite 事务中写入。助手消息的 `source=interaction_preset`，随后依次发布：

- `interaction.performed`：包含 `actionId`、`characterId`、`outcome`、`messageId` 和可选 `expressionIntent`，供头像或桌宠表现使用；不能替代消息持久化。
- `message.created`：沿用普通助手消息事件并增加稳定 `source`。

消息历史的 `source` 稳定值为 `user_input`、`model_reply`、`interaction_preset`、`interaction_generated`、`proactive`、`listening_signal` 或旧数据使用的 `legacy`。首版不允许重新生成 `interaction_preset` 消息；请求返回 `409 interaction_not_available`。数据库 schema 版本升至 4；回滚旧二进制会忽略互动命令表和消息来源列，原消息内容仍可读取。

## 客户端偏好与外观

- `GET /api/v1/preferences`：返回用户资料、快捷键、回合节奏、桌宠偏好和不含路径的主题设置。
- `PATCH /api/v1/preferences`：局部更新上述偏好。
- `PUT /api/v1/appearance/media`：用托管媒体 ID 设置或清除背景及用户头像。
- `PUT /api/v1/settings`：原子应用偏好、外观媒体引用和模型选择，供“保存全部设置”使用。
- `PUT /api/v1/user-profile`：原子保存用户资料与用户头像，供独立的用户资料窗口使用。
- `GET /api/v1/appearance/background`
- `GET /api/v1/appearance/user-avatar`

背景和头像 URL 仅在资源存在时返回。客户端不能通过这些接口设置任意磁盘路径。

回合节奏 `turnTaking` 含一个影响"要不要开口"的字段 `replyFrequency`（0 到 1 的有限数）。它是运行时触发门的输入：值越低，一条回复之前需要攒够的消息越多；0 表示关闭自动回复——用户消息照常落库、一条不丢，但永不成轮，界面上不会出现"正在输入"。超出范围、NaN、Infinity 一律 `400 validation_error`（字段 `turnTaking.replyFrequency`），因为把一个坏值读成"这条会话我不要回复了"是不可逆的用户可见后果。未显式配置时沿用运行时内部默认，发行链条仍按 `docs/phase2-runtime-contract.md` §5.1 的惰性默认走，本文件不把它当作推荐值表。偏好写入成功后后端会排空并重建内存中的会话运行时，所以新值对之后的第一个轮次即刻生效（见 `runtime-state` 的 `replyFrequency`）。另需注意 `turnTaking` 段是**整段替换、不是逐字段合并**：请求里出现该段时，段内没写的字段按类型默认取回（`replyFrequency` 会被带回 `1.0`），因此只想改一个字段也要把该段现有值一并送全；完全省略该段才会原样保留。

主题偏好包含 `mainWindowLayoutMode`，稳定值为：

- `messenger`：经典聊天三栏布局，也是旧偏好文件缺少该字段时的默认值。
- `card`：原有卡片式布局。

客户端通过 `PATCH /api/v1/preferences` 的 `theme.mainWindowLayoutMode` 保存选择，并以响应投影为准。其他值返回 `400 validation_error`，前端不得另建本地持久化配置覆盖服务端结果。

2026-09-12 桌面 UI 重设计后，Pry.App 对这两个历史值均显示统一的陪伴空间，设置页不再提供旧窗口切换。字段及验证规则保持兼容，已有值不会因打开新 UI 被改写；它不再决定当前桌面窗口的视觉布局。

`PUT /api/v1/settings` 请求由三个对象组成：

```json
{
  "preferences": { "selectedCharacterId": "pry", "activeConversationId": "room-id" },
  "appearance": {
    "backgroundMediaId": null,
    "clearBackground": false,
    "userAvatarMediaId": null,
    "clearUserAvatar": false
  },
  "models": {
    "activeModelId": "qwen3-1.7b-local",
    "activeVisionModelId": null,
    "activeSpeechModelId": "sensevoice-local",
    "modelTunings": {}
  }
}
```

后端会先验证三个部分并解析媒体引用，然后只写入一次偏好；任何验证失败都不会应用其中任一配置。成功返回最终 `preferences`、`models` 和 `runtimeAction`。`runtimeAction=settings_applied` 表示只更新运行时偏好快照，现有模型进程保持不变；`models_reloaded` 表示文字/视觉模型选择或模型调参发生变化，后端已经排空会话并重载模型运行时。前端不得通过比较字段自行判断是否需要重载。上传媒体是独立操作，未被设置引用的上传资源不会自动绑定到配置。

快捷键格式、快捷键冲突、回复数量关系、打字延迟关系、桌宠缩放及其他数值范围均由后端最终校验。客户端可以重复这些检查以即时提示，但不得将客户端校验视为安全或一致性边界。

独立用户资料窗口应调用 `PUT /api/v1/user-profile`：

```json
{
  "profile": { "displayName": "你", "signature": "愿今天也有好心情" },
  "avatarMediaId": "已上传的媒体ID或null",
  "clearAvatar": false,
  "avatarDisplay": { "focusX": 0.5, "focusY": 0.5, "zoom": 1.0 }
}
```

后端先校验资料、媒体资源类型和头像显示参数，再单次保存并刷新内容。任一字段或媒体引用无效时，资料和头像均保持原值。清除头像时传 `clearAvatar:true`；不得先调用偏好接口再调用外观接口模拟该操作。

## 模型配置

- `GET /api/v1/models`：返回可选模型、能力、有效调参值、文字/视觉/Embedding 选中状态（`selectedForEmbedding`）、`source`、`available` 和 `tags`；不返回模型路径或密钥。
- `POST /api/v1/models/scan`：立即重新扫描受管 `models` 目录并为新的有效 GGUF 生成本机配置；响应包含 `foundCount`、`addedCount` 和刷新后的 `models`。后端启动时也会自动执行同一扫描。
- `PUT /api/v1/models/{id}/tags`：用 `{ "tags": ["常用", "roleplay"] }` 替换该模型的用户标签。适用于内置、本地自动发现和网络模型；最多 20 个，去除首尾空白并忽略大小写去重，单项最长 32 字符。
- `PUT /api/v1/models/selection`：全量设置文字、视觉、语音模型选择以及逐模型 `modelTunings`。
- `POST /api/v1/models/network`：新增 OpenAI-compatible 网络模型配置。
- `PUT /api/v1/models/network/{id}`：更新网络模型配置。
- `DELETE /api/v1/models/network/{id}`：删除未被选中的网络模型配置。
- `POST|PUT|DELETE /api/v1/models/custom...`：旧版兼容入口；新前端不要再用它添加本地模型，本地 GGUF 统一由扫描发现。
- `GET /api/v1/runtime`：读取当前运行状态。

`source` 的稳定值为 `built-in`、`local`、`network`，历史手工本地配置可能返回 `custom-local`。只有 `source=network` 的项显示网络配置编辑/删除入口；`local` 项的文件定位、GGUF 校验、视觉投影配对及默认能力判定均由后端负责。`available=false` 表示本地配置对应文件当前不存在，前端应禁用选择并提示用户检查文件，不得要求或显示真实路径。

当前被文字、视觉或 Embedding 任一用途选中的自定义/网络模型不可删除，返回 `409 resource_conflict`。能力声明至少包含一种能力；`vision=true` 时必须同时为 `text=true`，本地视觉模型还必须有有效 mmproj。Embedding 独立选择不触发对话模型重载。

自动标签根据可信的文件名元数据生成，包括 `local`、`gguf`、`auto-discovered`、`chat`/`embedding`、模型家族、参数规模、量化类型，以及能够唯一匹配视觉投影文件时的 `vision`。已由内置配置引用的 GGUF 不会再生成重复条目。文件名只用于保守推断：无法确认的视觉模型标为 `vision-candidate`，Embedding 模型不会作为文字模型接受选择。用户编辑标签保存为覆盖值，重新扫描不会覆盖用户标签；传空数组表示用户明确清空。

网络模型请求不包含任何本地路径字段：

```json
{
  "displayName": "我的网络模型",
  "modelName": "provider/model-name",
  "baseUrl": "https://example.com/v1",
  "capabilities": { "text": true, "vision": false, "embeddings": false },
  "contextSize": 32768,
  "maxOutputTokens": 2048,
  "temperature": 0.8,
  "gpuLayers": 0,
  "computeDevice": "remote",
  "enableThinking": false,
  "tags": ["network", "roleplay"]
}
```

模型选择或运行参数改变时，后端先排空会话、原子保存偏好，再释放旧模型进程。在线 API Key 始终来自环境变量，不进入请求或响应。

Windows 下由 Pry 启动的 `llama-server` 会加入带 `kill-on-close` 的 Job Object；正常释放、宿主崩溃或被强制结束时，操作系统都会结束所属模型进程。注册表仍只在当前后端生命周期内按模型参数去重，不会按进程名结束其他软件的 `llama-server`。

远程 OpenAI-compatible 地址必须使用 HTTPS，回环地址允许 HTTP，URL 不得包含用户名或密码。默认扫描目录为发行目录向上解析到的 `models`，开发和测试可用 `Pry__ModelsDirectory` 指定受管目录；扫描跳过符号链接目录及无效 GGUF 文件。模型路径仅保存在本机偏好文件，不会通过 API 返回。

## 贴纸

- `GET /api/v1/stickers`
- `POST /api/v1/stickers`：正文包含已上传图片的 `mediaId`、名称和情绪标签。
- `PUT /api/v1/stickers/{id}`：更新用户贴纸名称、情绪标签和互动作用。
- `DELETE /api/v1/stickers/{id}`：删除用户贴纸；内置贴纸不可修改或删除。
- `GET /api/v1/stickers/{id}/content`

贴纸响应只包含内容 URL，不包含磁盘路径。互动作用只接受 `reaction`、`backchannel` 或 `topic`。

## 语音识别

- `GET /api/v1/speech/models`：语音模型列表、选中和可用状态；`custom` 区分内置配置与用户配置，客户端据此决定是否显示编辑/删除入口。
- `POST /api/v1/speech/models/custom`：创建自定义语音模型。
- `PUT /api/v1/speech/models/custom/{id}`：更新自定义语音模型。
- `DELETE /api/v1/speech/models/custom/{id}`：删除未被选中的自定义语音模型；当前选中模型返回 `409 resource_conflict`。
- `POST /api/v1/speech/transcriptions`：正文 `{"mediaId":"...","modelId":null}`。

录音由客户端完成，然后以 WAV 上传到媒体 API。识别由后端串行执行，返回 `text` 和实际 `modelId`；音频资源不能作为聊天附件。首版保持现有能力，只支持 16-bit PCM WAV，不引入隐式转码。
本地 `sherpa-onnx` 配置要求绝对模型目录，并验证 `model.int8.onnx` 与 `tokens.txt`；在线转写地址必须使用 HTTPS，回环开发地址可以使用 HTTP。响应不返回目录、服务地址或密钥。
客户端必须使用语音模型响应的 `available` 字段决定能否开始录音，不得尝试读取后端模型目录。

## 尚未冻结的接口

后端 v1 所需能力已经冻结。桌面客户端切换完成前，仍不得与 API 同时修改同一数据目录。

契约已冻结、实现尚未落地的项：无。M0-d 的 `runtime.json` 端口发现与本机会话令牌（见开头基础地址段与"本机会话令牌与进程发现"一节）、持久事件游标与 `stream.resumed`（M0-c，随 schema v9）均已落地；发现的写侧与读侧都在 2026-09-21 各有一条用例守着（`SessionTokenTests` 判写，`RuntimeDiscoveryTests`/`RuntimeDiscoveryProcessTests` 判读）。尚未落地的是 WebSocket 传输本身（M1），届时握手必须携带同一令牌。
