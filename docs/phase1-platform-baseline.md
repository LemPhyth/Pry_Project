# Pry Phase 1 平台基线与 Contract 冻结（RT-001 平台侧）

> 负责人：Core Platform & Extensibility。日期：2026-09-20。
> 本文冻结 Phase 1 的工程边界、稳定 ID、事件与协议契约目录，并登记盘点出的缺陷为编号任务。
> 它描述**当前实现的事实**与**必须保持的边界**，不表示 1.0 目标已经完成。
> 目标形态见 [协议版本化与 WebSocket 迁移](protocol-versioning-and-websocket-migration.md)。
>
> **2026-09-20 23:44 复验更新**：其他负责人已接管并推进本文依赖的多处改动（PLAT-002 由 Codex 在独立 worktree 完成、RT-002 的 `IRuntimeStore` 已实现、初测的 19 个编译错误已消除）。第 1、2、5、5.1、6、7、8 节按当前工作区实测事实修订，修订处以「初测/复验」区分时点；PLAT 编号任务的**缺陷判定**未被本文单方面改写。

## 1. 核对基线

| 项 | 值 |
|---|---|
| 分支 | `codex/ui-main-window-redesign`，HEAD `3613a2a`（本轮至复验期间无新提交） |
| 工作区 | 109 个已修改/未跟踪条目，属于其他协作者的进行中任务，本文一律未修改 |
| 已提交基线验证（初测） | 独立 worktree `.local-notes/wt-head`（HEAD `3613a2a`）：Release 构建 0 警告 0 错误，191 通过 / 1 跳过 |
| 工作区验证（2026-09-20 23:44 复验） | **通过**：Release 构建 0 错误、全量测试 366 通过 / 0 失败 / 1 跳过、仓库扫描 205 个受跟踪文件、`git diff --check` 与 `--cached --check` 干净；细节见第 6 节 |
| **统一提交基线（2026-09-22，守门人）** | Phase 1 代码基线 = `5361dab`（217 文件首次入库）+ `3f69ce3`（该提交自带的门禁修复）：代码自此全部受跟踪。从 `5361dab` 新建 `--detach` worktree 重跑：Release 构建 **0 警告 0 错误**、**628 通过 / 0 失败 / 2 跳过**、仓库扫描 **380 个受跟踪文件**通过、两项 `--check` 无输出；主工作区在 `3f69ce3` 上另跑一次，结论一致。细节见第 6 节末行。**此后为本文档自身的 `docs:` 提交只动文档、不动代码，代码基线仍以 `3f69ce3` 为准**。**本表早于本行的 HEAD 与工作区条目都是历史时点，不再代表当前** |

`Pry.slnx` 实际项目：`Pry.Core`、`Pry.Contracts`、`Pry.Client`、`Pry.Api`、`Pry.App` 加四个测试项目。

## 2. 分层事实与唯一允许的依赖方向

当前 `ProjectReference`：

```text
Pry.Core      → Pry.Contracts、Microsoft.Data.Sqlite、sherpa-onnx、SQLitePCLRaw   （无 HTTP 服务端、无 Avalonia）
Pry.Contracts → （零依赖）                                        ← 2026-09-21 起成立，见规则 4
Pry.Client    → Pry.Contracts
Pry.Api       → Pry.Core、Pry.Contracts
Pry.App       → Pry.Core、Pry.Contracts、Pry.Client、Pry.Api        （内嵌宿主）
```

冻结规则（新增代码必须满足，评审时按此拒绝）：

1. `Pry.Core` 不引用 Avalonia，不引用 `Microsoft.AspNetCore.*`，不承载 HTTP 服务端类型。Provider 出站 HTTP 只允许出现在 `Pry.Core/Inference`，且只能藏在模型抽象之后（现状满足）。
2. 只有 Repository / Store 层可以构造 `SqliteConnection`。Runtime 侧只允许通过 `Pry.Core.Runtime.IRuntimeStore` 读写持久状态。该端口的实现已存在（复验：`src/Pry.Api/Runtime/RuntimeAdapters.cs:15` 的 `DatabaseRuntimeStore` 委托 `MemoryDatabase.Runtime` 的 SQL），初测写的"实现归平台侧"已过时；当前形态是 **API 宿主适配层持有 SQL**，属 PLAT-004 的收敛对象而非缺陷本身。
3. `Pry.Api`（协议宿主）不是第二份角色状态真相源：不得在内存中持久化角色/会话事实。当前 `ConversationSessionService` 的 `_undo` 栈（`ConversationSessionService.cs:18,280-292`）属瞬时进程状态，可丢，不得成为恢复依据。
4. `Pry.Contracts` 必须能独立编译并版本化：目标是移除对 `Pry.Core` 的引用，把 `ChatMessage`、`RuntimeState`、`ModelCapabilities`、`MessageSources`、`TurnTakingSettings` 等领域类型换成显式传输 DTO（PLAT-005）。在此之前，任何 Core 内部重命名都会静默破坏线协议。
   **落地结果（2026-09-21，PLAT-005）**：引用已移除并反向为 `Pry.Core → Pry.Contracts`，`Pry.Contracts` 自此零依赖。原表述的"换成显式传输 DTO"未采纳，改为把这 18 个"同时进 HTTP/SSE 响应、被 Core 存储、被界面绑定"的类型声明整体移入 `Pry.Contracts/CommonContracts.cs`，属性名与默认值逐字不变。理由：两套同形 DTO 会在 Api 与约 20 个界面服务文件里长出映射层，而两份形状可以各自漂移——共享一份比"各留一份再加翻译"更不容易说谎。规则的目的（Core 内部改名不能再静默改线协议）由此达成，并由 `Pry.Api.Tests/ContractBoundaryTests` 三重钉住：契约程序集不得引用任何 Pry 程序集、契约公开签名不得出现 Core 类型、这些类型的 JSON 形状逐字固定。**这属于对冻结规则 4 的一次偏离，取舍交 QA 与作者复判。**
5. Client 不复制附件限制、模型策略、资源归属或权限规则；前端校验只用于即时提示（`docs/architecture.md` 已约定，继续有效）。

## 3. 稳定 ID 命名空间

| 标识 | 类型 | 生成方 | 约束与现状位置 |
|---|---|---|---|
| `conversationId` | `string` | SQLite 行 | 服务端解析归属，客户端不可伪造（`ConversationSessionService.cs:58,110`） |
| `messageId` | `long` | SQLite 自增 | 线协议按数值传输；会话内单调，用作已读水位与删除目标（`ApiContracts.cs:15,36`） |
| `characterId` | `string` | 角色卡 | `ContractValidation.OptionalId` 上限 128（`ApiContracts.cs:136`） |
| `mediaId` | `string` | 32 位十六进制 | 上传返回 `Id` + 相对 `DownloadUrl`（`MediaAssetStore.cs:24-43`） |
| `stickerId` / `folderId` | `string` | 后端 | ≤128 |
| `actionId`、`idempotencyKey` | `string` | 客户端提供、服务端校验 | 正则 `^[A-Za-z0-9][A-Za-z0-9_-]{0,127}$`（`ConversationSessionService.cs:134-137`） |
| `clientId`（活动上报） | `string` | 客户端 | 上限 64 个并发客户端（`CompanionActivityService.cs:33`） |
| `event.sequence` | `long` | `ConversationEventLog` | **每会话、仅内存、重启或重配置即清零**（`ConversationEventLog.cs:11,24`、`ConversationSessionService.cs:274,329`）→ PLAT-005 阻塞项 |
| `turnId` / `capabilityCallId` | 未定义 | — | Phase 2/7 落地前必须先在本文登记，不允许各自新建同名概念 |

规则：跨重启必须可比较的水位类 ID 用 `long`；对外暴露但可能改名的实体用 `string`；`PryUserId` 永不等价于任何外部平台的 user id。

## 4. 事件目录（当前 SSE 契约，冻结为唯一信封）

信封：`ConversationEvent(long Sequence, string Type, DateTimeOffset OccurredAt, object Data)`（`ApiContracts.cs:31`）。`Data` 在客户端解析为 `JsonElement`；客户端只读 `data:` 行，忽略 `id:`/`event:` 字段（`PryBackendClient.cs:159`）。

| 事件 | 载荷字段 | 发布点 | 消费者 |
|---|---|---|---|
| `session.ready` | `conversationId, characterId` | `ConversationSessionService.cs:270` | 未消费 |
| `turn.state` | `state` | `:257` | `RemoteConversationTurnManager.cs:69` |
| `message.created` | `messageId, role, type, content, stickerId, source` | `:40,122,176,260,338` | `RemoteConversationTurnManager.cs:74`、`PryMessengerWindow.axaml.cs:473` |
| `turn.cancelled` | `{}` | `:51` | 未消费 |
| `turn.failed` | `code, message`（`code` 恒为 `model_failure`） | `:268` | `RemoteConversationTurnManager.cs:88` |
| `conversation.changed` | `reason, messageId, scope` | `:67,87,103` | 历史刷新 |
| `conversation.read` | `conversationId, lastReadMessageId` | `:345` | 未读统计 |
| `interaction.performed` | `actionId, characterId, outcome, messageId, expressionIntent` | `:170` | 互动表现 |

已确认缺口（Phase 2/7 必须扩容本节，不得另建通道）：

- `turn.completed` 在 `PryMessengerWindow.axaml.cs:473` 有处理分支，但服务端**从未发布**，属死代码。
- 无 typing、runtime state、capability progress、developer trace 事件。
- 无心跳帧，无 `Last-Event-ID` 支持（grep 全仓为零），事件缓冲不落盘。

## 5. Runtime ↔ 平台接缝（与 RT-002 的分工线）

RT-002 已在 `Pry.Core/Runtime/RuntimePorts.cs` 定义 Runtime 的唯一输出通道；平台侧（本文所有者）是其**唯一宿主实现**，负责把方法调用映射成协议事件。Runtime 不再自行发布 SSE，也不再持有 `ConversationEventLog`。

| `IRuntimeOutputSink` 方法（`RuntimePorts.cs:107-124`） | 目标协议事件 | 传输 |
|---|---|---|
| `MessageDeliveredAsync` | `message.created`（携带 `source`） | HTTP 落库 + 事件推送 |
| `TypingIndicatorAsync` | `typing.changed` | 仅事件（瞬态，允许丢） |
| `PhaseChangedAsync` | `turn.state`（阶段迁移投影，复验时新增的一行） | 仅事件 + Developer Mode 诊断 |
| `TurnTransitionAsync` | `turn.completed` / `turn.cancelled` | 仅事件 + 诊断 |
| `CapabilityRequestedAsync` | `capability.progress` | 仅事件 |
| `FailureAsync` | `turn.failed`，`errorCode` 必须是稳定码 | 仅事件 |

对应持久化接缝（复验按当前签名更正，初测用的 `expectedWatermark` 措辞已被 RT-002 的 epoch/fence 模型取代）：`IRuntimeStore`（`RuntimePorts.cs:7-45`）以 `LoadStateAsync`/`SaveStateAsync` 存取状态，以 `ReadEpochAsync`/`BumpEpochAsync` 维护权威 fencing epoch，`AppendUserInputAsync` 返回单调水位，`RequestDeliveryAsync(conversationId, turn, fenceEpoch, …)` 在事务内比较 epoch，`CommitBubbleAsync(permission, sequence, …)` 条件写入，`AbandonPlanAsync` 与 `SweepStalePlansAsync` 收尾。`SweepStalePlansAsync` 是 Crash Recovery 的恢复入口：启动阶段清扫遗留 pending plan，且**不得重放**（架构 §55：进程状态可丢，角色状态不可丢）。SQL 目前集中在 `MemoryDatabase.Runtime.cs`，按第 2 节规则 2 归 PLAT-004 收敛。

### 5.1 Capability 契约命名归属

初测记录的重名问题已解除（复验：`RuntimeEvent` 上的 `Capability(...)` 现为接收 `CapabilityResult` 的工厂方法，`Runtime/RuntimeModels.cs:60`，不再是编译冲突）。**归属约定仍然有效且尚未满足**：`CapabilityRequest`/`CapabilityResult` 目前定义在 `Pry.Core.Runtime`（`Runtime/RuntimeModels.cs:137,140`），不在 `Pry.Core.Abstractions`。RT-001-INT 已把该项列为需 RT-002 与 Core Platform 联合收敛的待办，并明确不要求提前实现 RT-007。冻结结论保持不变：

- `ICapability`、`CapabilityDescriptor`、`CapabilityResult`、`CapabilityRequest`、`CapabilityPermission`（`Allow`/`Ask`/`Deny`）归 `Pry.Core.Abstractions`，与既有 `IChatModel` 同侧（`Abstractions/Capabilities.cs`）。
- `Pry.Core.Runtime` 只**引用**这些类型描述 Turn 里的 `UseCapability` 意图，不重新定义，也不实现分发器。
- Registry、Dispatcher、超时与取消执行层、审计追踪属宿主侧基础设施（实施归 RT-007），1.0 不做通用 Tool Loop。复验：`ICapability`/`CapabilityDescriptor`/`IPryPlugin` 在 `src/` 内仍为零命中，PLAT/RT-007 的实现面未被动过。
- 同理，`IRelevantMemorySource`/`RelevantMemoryContext`（MEM-005/RT-005 侧）与现存 `IMemoryRetriever` 的关系由 Memory Owner 收敛为单一适配器，平台侧不再新增第二个 Context 概念（`PROJECT_PLAN.md` 第 2.3 节约束 3）。

## 6. 验证证据（本文所有者实际执行）

命令与门禁来自 `PROJECT_PLAN.md` 第 7 节。按时点分列，不复用他人 worktree 的数字。

| 检查 | 时点与目标 | 结果 |
|---|---|---|
| `dotnet build Pry.slnx -c Release` | 初测，HEAD `3613a2a`（`--detach` worktree） | 通过，0 警告 0 错误 |
| `dotnet test Pry.slnx -c Release --no-build` | 初测，同上 | 通过 191、失败 0、跳过 1（`CoreTests.Configured_sense_voice_model_can_run_offline`，按条件显式跳过真实语音权重） |
| 分项目 | 初测，同上 | Core 16+1skip、Api 16、Client 3、App 156 |
| `dotnet build Pry.slnx -c Release` | 初测，当前工作区（含他人未提交改动） | 失败，19 错误，位于 `Abstractions/CharacterIntelligence.cs`、`Intelligence/MoodState.cs`、`Intelligence/RelationshipState.cs`、`Runtime/RuntimeModels.cs` |
| `dotnet build Pry.slnx -c Release` | 复验 2026-09-20 23:44，当前工作区 | **通过，0 错误**；初测的 19 个错误已由 RT-002/RT-003 负责人的后续改动消除，非本文修改引入 |
| `dotnet test Pry.slnx -c Release --no-build` | 复验，同上 | **366 通过 / 0 失败 / 1 跳过**：Core 153+1skip、Api 44、Client 7、App 162 |
| `dotnet test --filter ~EvaluationPersistenceTests` | 复验，同上 | 7 通过 / 0 失败：QA-001（`LoadRuntimeStateAsync` 读 NULL epoch）与 QA-002（真实 SQLite 允许 sequence 越序）两项 High 缺陷在主工作区**已不再复现**，其文件仍为未跟踪（`tests/Pry.Core.Tests/EvaluationPersistenceTests.cs`，22:42） |
| `scripts/verify-repository.ps1`（经 `powershell -NoProfile -ExecutionPolicy Bypass` 执行；`pwsh` 不在 PATH） | 复验，同上 | 通过 205 个受跟踪文件；不覆盖未跟踪源码 |
| `git diff --check` / `git diff --cached --check` | 复验，同上 | 均无输出 |
| `dotnet build` + `dotnet test --no-build` + `verify-repository.ps1` + 两项 `--check` | **2026-09-21，PLAT-005 M0-b 落地后**（仍含他人未提交改动，非统一提交） | 构建 0 警告 0 错误；**444 通过 / 0 失败 / 2 跳过**（Api 72、Core 196+2skip、Client 14、App 162）；仓库扫描 205 个受跟踪文件通过；两项 `--check` 无输出。明细见 `PROJECT_PLAN.md` 第 9 节 M0-b 记录 |
| `dotnet build Pry.slnx -c Release` + `dotnet test Pry.slnx -c Release --no-build` + `verify-repository.ps1` + `git diff --check` | **2026-09-21，PLAT-006 落地后**（仍含他人未提交改动，非统一提交；跑门禁前 `dotnet.exe` 只有空闲 MSBuild 节点复用进程，无同级构建/测试在跑） | 构建 **0 警告 0 错误**；**493 通过 / 0 失败 / 2 跳过**（Api 106、Core 211+2skip、Client 14、App 162）；仓库扫描 205 个受跟踪文件通过；`--check` 无空白错误（`git` 自带一条 `LICENSE` 的 LF→CRLF 提示，非本次引入）。本轮新增 10 个用例：Api `MediaUploadLimitTests` 5 + `BackendLoggingTests` 5（其中 1 条为既有文件的新增用例）、Core `InjectedLoggingTests` 3。明细见 `PROJECT_PLAN.md` 第 9 节 PLAT-006 记录 |
| `dotnet build Pry.slnx -c Release` + `dotnet test Pry.slnx -c Release --no-build` + `git diff --check` + `git diff --cached --check` + `scripts/verify-repository.ps1` | **2026-09-21，PLAT-001 落地后**（同一批未提交改动，非统一提交；跑门禁前确认 `dotnet.exe` 只有空闲 MSBuild 节点复用进程，无同级构建/测试在跑） | 构建 **0 警告 0 错误**；**535 通过 / 0 失败 / 2 跳过**（Api 118、Core 238+2skip、Client 17、App 162）。本轮新增 42 个用例：Core `ModelProviderErrorTests` 27 条（七类码、坏帧、空档超时、取消仍为取消、起头超时、向量两例、`turn.failed.code` 两例、超时预算与本地档位）、Api `ProviderErrorMappingTests` 11 + `BoundedWaitBoundaryTests` 1、Client `BackendRequestBudgetTests` 3。两项 `--check` 无输出（同前，`git` 的 `LICENSE` LF→CRLF 提示非本次引入）；仓库扫描 205 个受跟踪文件通过（仍不覆盖未跟踪源码）。明细见 `PROJECT_PLAN.md` 第 9 节 PLAT-001 记录 |
| `dotnet build Pry.slnx -c Release` + `dotnet test Pry.slnx -c Release --no-build` + `git diff --check` + `git diff --cached --check` + `scripts/verify-repository.ps1` | **2026-09-21，QA-P2 复核轮（PLAT-005 契约侧 + PLAT-001 探测侧）落地后**（同一批未提交改动，非统一提交；HEAD 已前移为 `f1c5118`，该提交只改许可声明、未取走 Phase 1 代码） | 构建 **0 警告 0 错误**；**546 通过 / 0 失败 / 2 跳过（总 548）**（Api 123、Core 242+2skip、Client 19、App 162）。本轮新增 11 个用例：Core `HardwareProbeTimeoutTests` 4（探测预算内切断、挂起不传染下一次探测、到期杀整棵子进程树、取消仍是 `OperationCanceledException`）、Api `LoopbackBindingTests` 5（四种非回环 `Pry:Url` 启动即失败且不创建数据目录 + `127.0.0.1:0` 正向对照）、Client `ProtocolHandshakeClientTests` 2 + 1 处强化（禁行宣告抵达 `ReadEventsAsync`、缺字段不得被发明成禁行）。两项 `--check` 无输出；`verify-repository.ps1` 通过 **214** 个受跟踪文件（口径随 `f1c5118` 变化；仍不覆盖 121 条未跟踪）。**树杀那条用例做过证伪跑**：临时把 `entireProcessTree` 改为 `false` 用例转红、改回即绿，确认断言非空转。明细见 `PROJECT_PLAN.md` 第 9 节该轮记录 |
| `dotnet build Pry.slnx -c Release` + `dotnet test Pry.slnx -c Release --no-build` + `git diff --check` + `git diff --cached --check` + `scripts/verify-repository.ps1` | **2026-09-21，第三轮阻塞①（`PLT-002` 界面分支）落地后**（同一批未提交改动，非统一提交；HEAD 仍为 `f1c5118`） | 构建 **0 警告 0 错误**；**551 通过 / 0 失败 / 2 跳过（总 553）**（Api 123、Core 242+2skip、Client 19、App 167）。本轮新增 **App 5 例**（`tests/Pry.App.Tests/EventStreamReconnectBanTests.cs`）：禁行方向 3（`426 protocol_mismatch`、`401 unauthorized`、未登记的 `502 provider_auth` 均只发一次 `/events` 且只弹一条"已停止重连"提示）、放行方向 2（`retryable` 缺席与 `retryable:true` 必须继续重连、不得升级为终止）。两项 `--check` 无输出；`verify-repository.ps1` 仍通过 **214** 个受跟踪文件（新增两文件均未跟踪，该脚本口径不变）。**禁行三条做过证伪跑**：条件临时改为 `when (false)` 后三条转红、两条放行例不受影响，随后还原。未提交条目增至 **173 条（50 已修改 + 123 未跟踪）**，其中 `.gitignore` 的 `Sample/` 一行属他人在途改动。明细见 `PROJECT_PLAN.md` 第 9 节该轮记录 |
| `dotnet build Pry.slnx -c Release` + `dotnet test Pry.slnx -c Release --no-build` + `git diff --check` + `git diff --cached --check` + `scripts/verify-repository.ps1` | **2026-09-21，第四轮阻塞②（`PLT-003` 读侧半片 + 真进程 Eval）落地后**（同一批未提交改动，非统一提交；HEAD 仍为 `f1c5118`；跑门禁前 `dotnet.exe` 只有 20:03 起的空闲 MSBuild 节点复用进程，无同级构建/测试在跑） | 构建 **0 警告 0 错误**；**565 通过 / 0 失败 / 2 跳过（总 567）**（Api 124、Core 242+2skip、Client 32、App 167）。本轮新增 **14 例**：Client `RuntimeDiscoveryTests` 13（缺席/半截 JSON/不是对象/空文件/端口 `0`·`70000`·`-1`/空令牌/已退出 `pid`/本进程 `pid` 各自拒接；"进程活着但无人服务该端口"仍拒接；未知字段可读；令牌不进 `ToString()`）+ Api `RuntimeDiscoveryProcessTests` 1（真起 `dotnet exec Pry.Api.dll` 子进程，只凭临时目录里那份 `runtime.json` 连上：`pid` 与端口对得上、端口 `!= 5078`、受门端点返回数据、匿名请求仍 `401`）。**该进程级用例做过证伪跑**：注释掉 `RuntimeDiscovery.TryConnectAsync` 附加令牌那一行后它在受门端点处转红（`401 urn:pry:error:unauthorized`），随后还原——判据确实是"令牌来自文件且真的过门"。两项 `--check` 无输出；`verify-repository.ps1` 仍通过 **214** 个受跟踪文件（口径不变，新文件均未跟踪）。未提交条目增至 **177 条（51 已修改 + 126 未跟踪）**，其中 `.gitignore` 的 `Sample/` 一行属他人在途改动。明细见 `PROJECT_PLAN.md` 第 9 节该轮记录 |
| `dotnet build Pry.slnx -c Release` + `dotnet test Pry.slnx -c Release --no-build` + `git diff --check` + `git diff --cached --check` + `scripts/verify-repository.ps1` | **2026-09-21，第五轮（UI-027 Runner 收敛，QA-P4-01/02 与 `PLT-006` 客户端义务）落地后**（同一批未提交改动，非统一提交；HEAD 仍为 `f1c5118`） | 构建 **0 警告 0 错误**；**613 通过 / 0 失败 / 2 跳过（总 615）**（Client 78、Api 126、App 167、Core 242+2skip）。本轮新增 **48 例**：Client `ConversationEventStreamRunnerTests` 15（游标首订 `-1`、按最后投递序号续订、退避 500ms→15s 封顶、8 次上限只报一次 `Faulted`、空流不空转、恢复只宣告一次且首连不算、取消无 `Faulted`、界面回调抛错按断线且不重投该条、**禁行三码 × 零次退避 + 只订一次**、`retryable` 缺席/为真两方向必须订满 8 轮）、`PryErrorPresentationTests` 31（PLAT-001 七类码逐档 + 禁行三档 + "同一条码在服务端沉默时仍可重试"）、Api `SingleBanJudgeBoundaryTests` 1（扫产品源码，`Pry.Client` 之外出现 `RetryForbidden` 即红）、`ContractBoundaryTests` 1（钉 `ProviderErrorCodes` 的程序集与七条取值）。界面侧 `EventStreamReconnectBanTests` 原 5 例保留为端到端形状，**同一批断言在收敛后仍全绿**，说明 Runner 接管判据没有改变对外行为。两次证伪跑：① Runner 禁行条件改为 `RetryForbidden: false` ⇒ Client 5 + App 5 同时转红后还原；② `src/Pry.App` 放一个含该判据的临时文件 ⇒ 新门禁转红后删除。两项 `--check` 无输出；`verify-repository.ps1` 仍通过 **214** 个受跟踪文件。未提交条目 **181 条（51 已修改 + 130 未跟踪）**：本轮净增 5 个新文件（Client 两片 + Client 测试两片 + Api 门禁一片）并删除一个本会话新建、从未跟踪入库的 `src/Pry.App/Services/BackendBanNotice.cs`。明细见 `PROJECT_PLAN.md` 第 9 节该轮记录 |

| `dotnet build Pry.slnx -c Release` + `dotnet test Pry.slnx -c Release --no-build` + `git diff --check` + `git diff --cached --check` + `scripts/verify-repository.ps1` | **2026-09-21，第五轮返工（`QA-P5-01`/`QA-P5-02`/`QA-P2-04③`/`QA-P5-03` 登记）落地后**（同一批未提交改动，非统一提交；HEAD 仍为 `f1c5118`；跑门禁前 `dotnet.exe` 只有空闲 MSBuild 节点复用进程，无同级构建/测试在跑） | 构建 **0 警告 0 错误**；**628 通过 / 0 失败 / 2 跳过（总 630）**（Client 80、Api 126、App 180、Core 242+2skip）。本轮净增 **15 例**：App `MessengerEventStreamControllerTests` 13（接线 11 + 跑真 `RunAsync` 的 2 例）、Client `BackendRequestBudgetTests` +2（整请求预算到点报 `TimeoutException` 而非取消；事件流订阅拿不到响应头时按本地预算失败）。**证伪跑为逐个突变、每次跑满该文件 13 例**：删 `Attach` 里四个回调任一行 ⇒ 依次 3/2/1/1 例转红（含新加的 Runner 级两例，"删掉 `runner.Faulted` 那行 613 仍全绿"这一后果就此闭合）；去掉 `HandleEventAsync`/`HandleResumedAsync`/`HandleStateChanged` 任一处切房门 ⇒ 旧房间那例转红；不认 `conversation.read`、不渲染 `turn.state` ⇒ 对应两例转红。**产品侧一处真实缺陷随之修掉**：转写的 4 分钟预算此前被客户端级 `HttpClient.Timeout`（32 秒）顶掉、从未生效，现改为逐请求声明 + `RequestBudgetCeilingMs` 兜底，两个真装配点各钉一条排序断言。`git diff --check` 与 `--cached --check` 无输出；`verify-repository.ps1` 仍通过 **214** 个受跟踪文件（口径不变，两个新文件均未跟踪）。未提交条目 **183 条（51 已修改 + 132 未跟踪）**，净增 2 个新文件（`src/Pry.App/Services/MessengerEventStreamController.cs`、`tests/Pry.App.Tests/MessengerEventStreamControllerTests.cs`）；`.gitignore` 的 `Sample/` 一行仍属他人在途改动。`QA-P5-03`（不可达布局偏好）本轮**未动代码**，按实施侧意见随 `MainWindow` 那一刀处置。明细见 `PROJECT_PLAN.md` 第 9 节该轮记录 |

| `dotnet build Pry.slnx -c Release` + `dotnet test Pry.slnx -c Release --no-build`（连续两次）+ `git diff --check` + `git diff --cached --check` + `scripts/verify-repository.ps1` | **2026-09-22，`QA-P6-01` 红门禁（探测用例前置条件事件化）落地后**（同一批未提交改动，非统一提交；HEAD 仍为 `f1c5118`；两次解决方案级跑即 QA 结案判据所要求的"并行连续 ≥2 次全绿"） | 构建 **0 警告 0 错误**；**628 通过 / 0 失败 / 2 跳过（总 630）**（Client 80、Api 126、App 180、Core 242+2skip），**连续两次一致**。本轮不改产品代码，只改 `tests/Pry.Core.Tests/HardwareProbeTimeoutTests.cs` 一条用例的结构：全树终止那一条的前置条件原先与 250ms 探测预算共用同一条时间轴（孙进程要在预算内写下第一行），四程序集并行时该链子可超过预算 ⇒ 第六轮解决方案级连红 2/2、隔离跑全绿。现在事件与预算分轴（3 秒内轮询"开始写"这个事件 → 5 秒预算到期 → 判"杀掉后文件不再增长"），前置断言原文保留、等不到事件仍报红。**证伪在新结构上重做**：`entireProcessTree: false` ⇒ 该例在第 82 行增长断言转红（5538→5964 字节）后还原，确认变宽的只是前置条件而不是判据；隔离跑 4/4、7 秒。两项 `--check` 无输出；`verify-repository.ps1` 仍通过 **214** 个受跟踪文件。未提交条目 **183 条（51 已修改 + 132 未跟踪）**，本轮无新增文件。台账同步：`docs/engineering-pitfalls.md` 新增第 24 条；迁移文档 §8.1 的派生视图抬头按目录刷新为 `suiteVersion 1.1.2` / `PLT-*` 13 条，并对 `QA-P6-02`（本台账那条"不得因测试变绿记 Pass"的硬提醒超出目录文本）留更正记录。明细见 `PROJECT_PLAN.md` 第 9 节该轮记录 |

| `dotnet build Pry.slnx -c Release` + `dotnet test Pry.slnx -c Release --no-build --logger trx`（连续两次，结果目录 `TestResults/qa-p8-01-a`、`-b`）+ `scripts/measure-fixed-eval.ps1` + `git diff --check` | **2026-09-22，QA-P6-01 同类形状排查轮（纯 `tests/` 改动，产品代码与 `src/` 一字未改）**（同一批未提交改动，非统一提交；HEAD 仍 `f1c5118`；跑门禁前确认无他人 `dotnet.exe`/`testhost.exe`） | 构建 **0 警告 / 0 错误**；解决方案级**连续两次 628 通过 / 0 失败 / 2 跳过（总 630）**（Client 80、App 180、Api 126、Core 242+2skip），两次跳过的仍是原来那两条环境门控，**未把任何红例转 skip**；`measure-fixed-eval.ps1` 两个目录退出码均 **0**（`Passed=628; Failed=0; Other=2`，catalog 68 条有效）；`--check` 无输出。本轮改动两条用例：`CoreTests.Closing_windows_process_job_terminates_owned_process` 补"关句柄时子进程仍活着"的前置（并把非 Windows 从静默 `return` 改为 `Assert.Skip`）与 `SessionTokenTests.ReadDescriptorAsync` 等待 2s→5s。**该前置两次证伪**：第一版（紧接着采样）把子进程换成 `cmd /c exit` 仍 27ms 通过 ⇒ 修法是错的；改成 500ms 落定后同一证伪转红于 `:88`（549ms）、还原后 546ms 绿。过程缺陷如实记：首次跑门禁漏了 `--logger trx`，两个结果目录为空，`measure-fixed-eval.ps1` 以"No TRX results found; absence is never a pass."抛错，那一次的 console 绿灯未被引用。明细见 `PROJECT_PLAN.md` 第 9 节该轮记录 |
| `dotnet build Pry.slnx -c Release` + `dotnet test Pry.slnx -c Release --no-build` + `git diff --check` + `git diff --cached --check` + `scripts/verify-repository.ps1` | **2026-09-22，Phase 1 基线提交 `5361dab`（217 文件首次入库）+ 集成门禁修复 `3f69ce3`**。两次独立跑法：① 从 `5361dab` 新建 `--detach` worktree，工作树内零条未提交改动；② 主工作区 HEAD `3f69ce3`，`git status` 除刻意排除的 `docs/character-learning-safety.patch` 外干净。跑门禁前确认无他人 `dotnet.exe`/`testhost.exe` | 两次结论一致：构建 **0 警告 / 0 错误**；**628 通过 / 0 失败 / 2 跳过（总 630）**（Client 80、App 180、Api 126、Core 242+2skip），跳过的仍是原来那两条环境门控，**未把任何红例转 skip**。**仓库扫描口径从 214 个受跟踪文件变为 380 个并通过**——覆盖面积扩大是因为未跟踪源码首次入库，"扫描面之外还有源码"的盲区就此关闭。两项 `--check` 无输出。**本节首次出现可指认提交**：此前每一行的"目标"都只能写"同一批未提交改动，非统一提交"，从本行起可写提交哈希，§8.1 段③ 要求的"统一提交哈希 + 从该提交重跑"第一次成立。**代价如实记**：`5361dab` 把 217 个从未被扫描的文件一次性纳入 `verify-repository.ps1`，随即扫出 `tests/Pry.Api.Tests/BackendLoggingTests.cs:76` 的一条字面量命中密钥形状。处置是改测试夹具（`"sk-" + "abcdefghijklmnopqrstuvwxyz"` 拼接，常量折叠后运行期值不变、遮蔽断言仍成立、该文件 5/5 绿）而**不是放宽扫描规则**——刻意不选另一侧：把 `tests/**` 排除出扫描面等于给真密钥留一条藏身处。见 `3f69ce3`。排除入库的两项：6 张走查截图（含二创美术，按 `ASSETS_LICENSE.md` 与 `AGENTS.md` 第 6/7 条不得提交，已写入 `.gitignore` 的 `docs/eval/walkthroughs/`）、`docs/character-learning-safety.patch`（41 KB，归属其作者处置，未擅自提交）。明细见 `PROJECT_PLAN.md` 第 9 节该轮记录 |

结论：**当前工作区已可编译并通过全量自动化门禁**，第 7 节的阻塞表述随之解除。2026-09-22 起 **Phase 1 代码基线 `5361dab` + `3f69ce3` 是可指认、可复现的提交**：上表最后一行已从该提交的 `--detach` worktree 重跑并得到同一结论（构建 0/0、628/0/2、扫描 380 个受跟踪文件通过），自动化层面的"绿"到此可归属到具体哈希；工作区也不再是 183 条未提交条目，`git status` 只剩上表末行说明的那份刻意排除的他人 patch。仍未结案的是自动化之外的两项：`PROJECT_PLAN.md` §5.0 走查口径的 **③ 仍为 Blocked**（`F-1`：配置了非默认数据目录时 `RuntimeDiscovery.TryConnectAsync` 发现不到已有后端，会让两个宿主写同一份 `memory.db`），以及 **① 的 `DEF-P1WALK-01` 按未修计**（工作区与 11 个 worktree 内均无该修复的载体文件）。第六轮另有一条与本节同源的教训：同一份工作区可以既跑出 613 全绿又跑出 627 带一红，差别只在并行负载——所以本表每一行都必须写清跑法，"绿"不是单个数字而是"该跑法下该时点的形状"。

Phase 1 平台侧其余验收项（Client 启动并连上 Core、聊天、历史持久化、重启后历史仍在、无官方服务器依赖）在 HEAD 基线上由初测的 191 个用例覆盖其 HTTP/持久化部分；带真实模型的端到端仍按项目基线显式跳过，不以自动化冒充真实模型验收。

## 7. 阻塞（已解除，改为集成门禁）

初测记录的阻塞——他人未提交的 `Pry.Core/Runtime`、`Pry.Core/Intelligence` 导致 19 个编译错误、因而无法在主工作区执行第 7 节门禁——复验时不再成立。RT-001 平台侧的"验证 SQLite、配置、日志、Provider、消息持久化和重启"在自动化层面已可执行且已通过。

RT-001 仍不标"已完成"，因为剩余门槛不是编译（2026-09-21 按当前工作区逐条复核，其中第 3 项的原表述已过期并就地更正）：

1. ~~**无统一提交**~~ **已闭合（2026-09-22，守门人）**：原表述是"HEAD 已前移到 `f1c5118`，`git log main..HEAD` 仍无任何 Phase 1 代码提交，全部 1.0 新代码未提交"。该批改动现已形成基线提交 **`5361dab`**（217 文件首次入库，含 Runtime、Intelligence、Memory、UI-027 客户端层与本文件的 PLAT-002/PLAT-005 M0-a…M0-d/PLAT-001/PLAT-006），并由 **`3f69ce3`** 修掉该提交自带的门禁冲突。第 6 节末行记录了从 `5361dab` 的 `--detach` worktree 重跑门禁的结果，§5.0 的"形成可审查提交"这一条结案硬条件自此成立。**原表述里的判断仍然有效且未被推翻**：那批改动确实跨 Owner 耦合、任何单点提交都会让另一方编译失败——正因如此做的是**单条全量提交**而不是拆细，"任何单点提交都会失败"这句现在读作该形状的理由，不是障碍。刻意排除的两项与其归属见第 6 节末行。
2. **契约归属未收敛**：`Pry.Contracts → Pry.Core` 引用（原 `Pry.Contracts.csproj:3`）已于 2026-09-21 随 PLAT-005 解除并反向——见第 2 节规则 4 的落地结果，其中"未采纳显式传输 DTO、改为共享声明"是一处待 QA/作者复判的偏离。第 5.1 节的 Capability 类型归属仍未收敛，归 RT-002 与 Core Platform 联合裁定。
3. **平台侧地基实施状态（更正）**：原文写"PLAT-001/003/004/005/006/007 在 `src/` 内仍为零实现命中、PLAT-002 未进入主工作区（`LlamaServerManager.cs:41` 仍把密钥放进 argv）"——两点已不成立：**PLAT-002 已逐 hunk 并入**（`LlamaServerManager.cs:94-98` 改经子进程环境块传 `LLAMA_API_KEY` 并先 `Remove` 继承值），**PLAT-005 的 M0-a/M0-b 已落地**。2026-09-21 重新核对后仍属零实现命中的是：PLAT-003（`integrity_check`/`BackupDatabase`/`VACUUM INTO` 全库无命中，版本仍是 `MemoryDatabase.cs:153` 的字面量，随 M0-c 由 8 升为 9）、PLAT-004（`IConversationRepository` 无命中）、PLAT-007（`messages.image_path` 仍存绝对路径）。同日 **PLAT-001 与 PLAT-006 已落地主工作区（仍未提交），PLAT-005 的 M0-c/M0-d 亦已落地（仍未提交）**：PLAT-001 的六处无界 `HttpClient`（`ModelProcessRegistry.cs:57,88`、`EmbeddedBackend.cs:38`、三个界面构造点）全部改为有界预算，Provider 七类稳定码 + HTTP 映射 + `turn.failed.code` 分类落地，坏帧不再伪装成正常结束，取消保持可观测且仍是 `OperationCanceledException`；PLAT-006 侧 `long.MaxValue` 的两处声明点已收敛为 `MediaUploadLimits` 单一硬上限并回 `413 payload_too_large`，Core 有了可注入 `ILogger`、Api 有滚动文件日志与 `LogRedactor`。`runtime.json` 发现与本机会话令牌见 `BackendSessionToken` / `RuntimeDescriptorService` / `SessionTokenMiddleware`。
4. **真实模型端到端与真机验收**：按项目基线属显式待办，不以当前 444 通过冒充。

安全的下一步（按顺序）：

0. 验收链路已成形，见 `docs/protocol-versioning-and-websocket-migration.md` §8.1：实施（本文所有者）→ 固定 Eval（EVAL-001，协议类判据需 QA 采纳为 `PLT-*` 永久场景）→ 集成基线复验（RT-001-INT，在统一提交上重跑第 6 节）→ 发行输入（RT-008）。阶段指针仍只由作者或守门人按 §5.0 推进。
1. ~~由集成负责人把该批改动形成统一可审查提交，从该提交重跑第 6 节命令并把结果补进本节。~~ **已完成（2026-09-22）**：`5361dab` + `3f69ce3`，重跑结果已补进第 6 节末行。下一步不再是"做提交"，而是走查口径剩下的 **③（`F-1`）** 与 **①（`DEF-P1WALK-01`）**——前者是把描述符路径收敛到单一 resolver 的契约修复（Owner：PLAT-005），后者归 Character Intelligence，都不是提交层面的工作。阶段指针仍只由作者或守门人按 §5.0 推进；**提交成立不等于 Phase 1 结案**。
2. 实施 PLAT-005 剩余的 M0-c/M0-d。**"与 `Pry.Core/Runtime` 无文件重叠"这句原表述要收窄**：M0-c 的事件追加要接进 `ConversationSessionService`（已被他人改动）与 Core 写入路径，M0-b 的 `turn.completed` 更是已经落在他人未跟踪的 `src/Pry.Api/Runtime/RuntimeAdapters.cs` 里；因此起手前必须先做第 9 节 M0-c 记录 Known Issue ③ 的封存核对，不能再按"零重叠另开 worktree"设想。实施前仍须先改 `docs/api-v1.md`（本片已把契约写清并标注"当前构建尚未实现"）。
3. PLAT-004 的 Repository 收敛以已落地的 `DatabaseRuntimeStore` + `MemoryDatabase.*` 为起点，不新增第二份 Store。（PLAT-003/004/007 现属 §5.0 冻结项，轮到前不动手。）

## 8. 缺陷台账（登记进 `PROJECT_PLAN.md` 第 4.2 节）

> **状态口径（2026-09-22 守门人加注）**：本节多行写着"已落地主工作区**未提交**（2026-09-21）"。该状语自 `5361dab` + `3f69ce3` 起过期——PLAT-001/002/005（M0-a…M0-d）/006 与 UI-027 客户端层现已全部受跟踪，逐条技术表述与判据不因此改变。仍按原样保留"未提交"字样的行不再重抄，读时以第 6 节末行与本注为准。**未提交的只剩他人那份 `docs/character-learning-safety.patch`**（归属其作者，见第 6 节末行）。

| 编号 | 优先级 | 缺陷与完成标准 | 受影响 Contract |
|---|---|---|---|
| PLAT-001 | P1 | ~~出站 HTTP 全部无界超时~~ ~~Provider 错误语义残缺~~ **已落地主工作区未提交（2026-09-21，Core Platform）**：六处 `Timeout.InfiniteTimeSpan` 声明点全部改为有界预算——模型起头超时远端 120 秒 / 本地 300 秒（本地那一档覆盖 llama-server 单槽排队的合法等待，常量单点 `ProviderTimeouts`）、流式回复空档 120 秒（`OpenAiCompatibleChatModel.ReadLineWithIdleAsync`，可注入窗口以便测试）、本机请求预算 32 秒且与心跳同源（`PryProtocol.BackendRequestTimeoutMs`，**第五轮返工后由 `PryBackendClient` 逐请求声明**，共享 `HttpClient.Timeout` 退为更宽的 `RequestBudgetCeilingMs` 兜底；两个真装配点各一条排序断言）、语音识别整请求 4 分钟（`TranscribeAsync` 自带预算，唯一要等整个模型跑完才回包的调用；**此前这一档从未生效过**，被装配点的 32 秒客户端级 Timeout 顶掉，见 `docs/engineering-pitfalls.md` 第 22 条）。七类稳定码 `provider_auth`/`provider_rate_limited`/`provider_timeout`/`provider_unavailable`/`provider_invalid_response`/`context_overflow`/`provider_cancelled` 由 `ModelProviderError` 单点归类，Core 不带 HTTP 事实，状态映射只在 `ApiExceptionMiddleware`（502/429/504/503/400 + `retryable`/`retryAfterSeconds`）；`turn.failed.code` 与记忆向量降级状态改用同一批码，未结构化故障保留原兜底码不谎报。`EnsureSuccessStatusCode()` 已从聊天路径移除（它把 401/429 压成 500），坏帧不再伪装成"回复正常结束"，取消原样传播 `OperationCanceledException`（运行态据此判"被打断"，包成新类型会把用户停止记成模型故障），Provider 错误体截到 2 KiB、先遮密钥、只进本机日志。判据：Core `ModelProviderErrorTests` 27 + Api `ProviderErrorMappingTests` 11 + Client `BackendRequestBudgetTests` 3，防复发门 `BoundedWaitBoundaryTests` 扫产品源码禁止该写法。原缺陷记录保留如下，供复判：出站 HTTP 全部无界超时（`ModelProcessRegistry.cs:57,88`、`App.axaml.cs:51` 时代的 `MainWindow.axaml.cs:78`、`PryMessengerSettingsWindow.axaml.cs:31`，两者还把回环地址与 5078 端口硬编码在界面层，属 PLAT-005 发现机制缺失的下游症状），`ModelRuntimeException.cs` 只有进程启动类失败，取消在 `OpenAiCompatibleChatModel.cs:62` 静默 `yield break`。**仍未做（下一片或另行认领）**：① ~~`PryErrorPresenter.Classify`（UI-027）不在此工作区，只存在于 worktree `../Pry_Project_client_error_reconnect`，合入时必须把这批码并进它的分类表，否则界面侧仍是两套语义~~ **第五轮已闭合（2026-09-21）**：`src/Pry.Client/PryErrorPresentation.cs` 按当前语义重做进主工作区，七类码逐档进分类表并前置禁行档（QA-P2-04① 的那句"两套语义"自此不存在——同一批常量在 `Pry.Contracts.ProviderErrorCodes`，Core 归类、Api 状态映射、Client 呈现表三处共用）；判据 `tests/Pry.Client.Tests/PryErrorPresentationTests.cs` 31 例，含"同一条码在服务端沉默时仍可重试"这一防误读档；② ~~`LlamaHardwareDetector.ListDevicesAsync` 与 `QueryNvidiaMemoryAsync` 的子进程等待仍无界，且它持有单槽 `Gate`，一次挂起会毒化后续所有设备探测~~ **2026-09-21 第二轮已落地**：统一的 `RunProbeAsync` 给设备探测 20 秒、显存查询 10 秒两档预算，到点 `Kill(entireProcessTree: true)` 只杀自己拉起的那棵树（不扫同名进程，不进入 Job Object 域），三条等待改并发以避开管道互锁，`Gate` 在到期与取消两条出口都释放；判据 `tests/Pry.Core.Tests/HardwareProbeTimeoutTests.cs` 4 条；③ ~~语音识别预算到点那条路只有取消侧有用例（4 分钟预算本身无法在测试里等满）~~ **2026-09-21 第五轮返工已闭合**：预算改为可注入后，150 毫秒即可判到点（`BackendRequestBudgetTests` 两例：到点报 `TimeoutException`、订阅拿不到响应头按本地预算失败），并顺带查出真正缺陷——那一档 4 分钟此前被客户端级 Timeout 顶掉、从未生效。 | `urn:pry:error:*`、`turn.failed.code`、`IChatModel`、`PryProtocol.BackendRequestTimeoutMs` |
| PLAT-002 | P1 | **状态更新（复验）**：Codex Core Platform 已在 `.local-notes/plat-002-model-secret` / `codex/plat-002-model-secret` 完成并通过独立验证（env 块传密钥、清除继承项、6 项参数/环境回归 + 真实模型 401/200 鉴权），**尚未进入主工作区**——`src/Pry.Core/Inference/LlamaServerManager.cs:41` 仍是 `args.Add("--api-key"); args.Add(profile.ApiKey)`。原缺陷：本地模型密钥以 `--api-key` 明文进入进程命令行（密钥生成在 `ModelProcessRegistry.cs:37`），本机任意进程可通过进程列表/WMI 读取。集成约束（该分支自带）：须保留 MEM-004 在同一文件的 `embeddingsOnly` 分支与测速行为，把鉴权配置应用到共用启动路径，禁止整文件覆盖。 | 无外部契约变化 |
| PLAT-003 | P1 | 备份/恢复/integrity_check **完全不存在**（复验：`integrity_check`、`BackupDatabase`、`VACUUM INTO`、迁移注册表在 `src/` 内仍零命中）；schema 版本是硬编码字面量，复验时该值已由 `7` 升为 `8`（`MemoryDatabase.cs:133`，随 RT-002 的 Runtime 表），迁移无事务包裹、无回滚说明。完成标准：迁移注册表 + 版本断言、`integrity_check` 启动自检、按角色的备份与恢复端点及回归测试。编号归持久化负责人统一裁定（MEM-005 在其分支改用 `memory_schema=1` 组件版本而非抢全局 v8；主工作区暂无该列）。 | 新增 `POST /api/v1/backup`、`/restore` |
| PLAT-004 | P2 | 无 Repository 层：`MemoryDatabase` 作为具体单例注入 12 个 Api 服务与 3 个 Core 类（`Program.cs:46` 起），83 处原始 SQL 全在它的三个 partial 里；`ReplyPlanner`/`ConversationTurnManager` 直接持有它。复验补充：`IRuntimeStore` 已有实现 `DatabaseRuntimeStore`（`Pry.Api/Runtime/RuntimeAdapters.cs:15`），但它委托的是 `MemoryDatabase.Runtime` 且位于 API 宿主层，因此本任务的收敛对象包含它；MEM-005 分支另有自己的 Memory Repository，需在其合入后统一而非另造。`busy_timeout=5000` 之外无 SQLITE_BUSY 重试、无统一事务隔离级别、消息/记忆/会话无 revision 列（乐观并发只有主动策略与已读水位有）。完成标准：`IConversationRepository`/`IMessageRepository`/`IMemoryRepository` + Store 层实现落地，SQL 不出 Store 层，写所有权与重试策略成文。前置：RT-002 的 Core 改动进入可编译基线——**复验已满足**。 | 无 |
| PLAT-005 | P1 | ~~协议无版本协商~~ **M0-a 已落地主工作区未提交（2026-09-21）**：`/api/v1/info` + 双向版本头 + `426 protocol_mismatch`（`Pry.Contracts/PryProtocol.cs` 单点）。事件游标每会话且仅内存、重启即归零（**M0-c 契约已于 2026-09-21 冻结**：全局单调序号 + 保留/裁剪 + 删除遮蔽 + journal≠Memory 读边界，见迁移文档 §4.1；实现待 §5.0 顺序裁决），~~SSE 无心跳~~ **M0-b 已落地未提交**：空闲 `:ping` + 客户端逐行存活窗口（`SseEventStream.cs`、`PryBackendClient.ReadLineWithLivenessAsync`），`turn.completed` 自此真正下发；无 `Last-Event-ID` 仍待 M0-c。两套客户端重连语义不一致（~~`RemoteConversationTurnManager.cs:60-97` 保留游标固定 500ms；`PryMessengerWindow.axaml.cs:462-502` 固定 `after=-1` 因而无法补投，3000ms~~ **2026-09-21 第五轮已由统一 Runner 收敛**，见本行末尾）。复验：UI-027 的统一重连宿主 `ConversationEventStreamRunner` 与 `PryErrorPresenter` 只存在于 worktree `../Pry_Project_client_error_reconnect`（基线 `main@134eddc`），主工作区 `src/Pry.Client/` 尚无这两个文件，故上述两套循环仍是当前事实；该 Runner 合入后即为客户端标准层，本任务不重写（M0-b 的存活判定刻意落在更下面的传输层，两条旧循环无需改动即获得半开检出）。回环端口"申请后即释放"存在竞态（`App.axaml.cs:211-215`），独立进程模式无发现机制、硬编码 5078（`Program.cs:20`，**M0-d 契约已补全 2026-09-21**：默认拒绝的单一鉴权中间件 + 豁免白名单只有 `/health` 与 `/api/v1/info` + token 只走请求头，见迁移文档第 6 节第 7-8 条；实现待 §5.0 顺序裁决）。承接 API-007 废弃后移交的协议版本协商地基要求。完成标准：见迁移文档第 3、4 节（含 §4.1 的 M0-c 细化与验收补强）与第 6 节第 7-8 条（M0-d 鉴权中间件）的验收清单；M0 四片全部落地并过第 8 节判据才算本任务完成。**2026-09-21 第二轮（QA-P2）本任务侧两项已落地**：`ApiProblemResponse` 补 `Retryable`、`PryBackendException` 透传该属性（三值：`false`＝禁行、`null`＝服务端未宣告，客户端不猜），线契约写进 `docs/api-v1.md`，判据钉在 `ReadEventsAsync` 这条真正重连的路径上（原 `426` 用例名带 "non-retryable" 却没有对应断言，即 QA-P2-01 的断链）；"只绑回环"从"代码里有守卫"变成"有判据"——`LoopbackBindingTests` 断言 `0.0.0.0`/私网/公网域名/空值四种 `Pry:Url` 启动即失败**且不创建数据目录**，配 `127.0.0.1:0` 正向对照。**第三轮（`eval-QA-P1ACC-0921C` 阻塞①）补上"停在明确界面"那一半**：`PryBackendException.RetryForbidden` 为唯一禁行判据（只有 `retryable:false` 算禁令，`null`/`true` 都不是），上面列的两条旧循环各在通用兜底前拦下禁行、终止退避并给出 `BackendBanNotice` 的可执行文案，判据 `tests/Pry.App.Tests/EventStreamReconnectBanTests.cs` 5 例同时钉"禁行必须停"与"未宣告必须继续重连"两个方向。UI-027 的统一 Runner 合流时接管同一判据，这两处分支是其子集，不另立第二套错误状态源；`PryMessengerWindow` 那条分支暂无自动化覆盖（见第 9 节该轮 Known Issues ①）。**`PLT-002` 的界面判据已具备转 Pass 条件，复判归 QA（`eval-QA-P1ACC-0921D` 已记 Pass）**。**第四轮（`eval-QA-P1ACC-0921D`）阻塞②补上发现的读侧半片**：作者裁定"1.0 需要独立进程客户端"后新增 `Pry.Client.RuntimeDiscovery`（`TryRead`/`TryConnectAsync`，判据"不确定就不接"：缺席、半截 JSON、不是对象、端口越界、空令牌、`pid` 已退出或就是本进程一律不接；未知字段忽略；探测走豁免的 `/api/v1/info`，版本兼容仍归协商中间件不在读侧复制一份判定），`Pry.App` 启动改为先发现后内嵌，界面层三处 `http://127.0.0.1:5078/` 构造点改为 `PryBackendClient.Unattached()`（端口 0，意外触网只会失败），`5078` 只剩 `Program.cs` 服务端默认值与测试夹具；判据为 Client 13 例 + Api 一条真进程连接（证伪跑：抽掉令牌头即红）。代价如实登记：接上他人后端时退出本窗口不关掉那个进程，退出提示按 `_backend` 是否为空分支；`App.InitializeDesktopAsync` 的启动顺序仍无自动化覆盖，与 QA-P4-01 同源，随 UI-027 收敛。`PLT-003` 复判归 QA。**第五轮（2026-09-21）该"随 UI-027 收敛"已兑现**：`src/Pry.Client/ConversationEventStreamRunner.cs` 与 `PryErrorPresentation.cs` 按当前语义重做进主工作区，两处事件流循环改为消费 Runner 的状态与终止出口，第三轮临时补的 `when (ex.RetryForbidden)` 与 `BackendBanNotice` 撤除（禁令判据自此只在 `Pry.Client` 一处，`SingleBanJudgeBoundaryTests` 扫源码守这条线），`PryMessengerWindow` 那条循环改为按序号续订 + 重连后补取历史，上面划掉的"两套重连语义"自此收敛为一套；`ProviderErrorCodes` 移入 `Pry.Contracts`。QA-P4-01/02 与场景 `PLT-006` 的客户端义务闭合，服务端裁剪侧仍归 PLAT-007。 | 全部 `Pry.Contracts` DTO（`Pry.Contracts → Pry.Core` 引用**已于 2026-09-21 随 PLAT-005 收尾解除并反向**，见第 2 节规则 4 的落地结果） |
| PLAT-006 | P2 | ~~`Pry.Core` 零日志基础设施~~ ~~`Pry.Api` 只有 console logger~~ ~~上传无硬上限~~ **已落地主工作区未提交（2026-09-21）**：Core 的 `MemoryDatabase` 与 `LlamaServerManager` 接受可空 `ILogger`（默认 `NullLogger`，未接线调用方行为不变；`Pry.Core.csproj` 为此新增零实现依赖 `Microsoft.Extensions.Logging.Abstractions`），Api 侧 `RollingFileLoggerProvider` 写 `<DataDirectory>/logs/pry-backend.log`（单档 5 MiB / 5 份、逐条 flush、`Pry:Log*` 三键夹取、sink 自身故障只停用本条写入并一次性报 stderr、**不留常驻句柄**），`LogRedactor` 在渲染后落盘前遮本机会话令牌与 `api_key`/`Bearer`/`sk-`/URL userinfo（键名保留，短于 8 字符的注册值不登记）；上传默认 50 MiB 硬上限收敛到 `MediaUploadLimits` 单一来源，`Program.cs` 的全局 `FormOptions` 与 `MediaController` 的两处特性同值，超限在落盘前拒绝并回 `413 urn:pry:error:payload_too_large`（`resource` + `maximumBytes`），`policy.maximumBytes` 发布真实数字。判据：`BackendLoggingTests` 5 + `MediaUploadLimitTests` 5 + `InjectedLoggingTests` 3。原缺陷记录保留如下，供复判：`Pry.Core` 零日志基础设施（复验：Core 内 `ILogger` 曾零命中、`Pry.Core.csproj` 无日志包），`Pry.Api` 只有 console logger，无日志文件、无集中遮蔽；密钥不进日志此前依赖 `LlamaServerManager` 单点字符串替换。另外 `MultipartBodyLengthLimit = long.MaxValue`（`Program.cs:74`）只有 10 MiB 警告头。仍未做（超出本片完成标准，见 `PROJECT_PLAN.md` 第 9 节 Deferred）：日志级别可按配置收窄（默认跟随宿主 `Information`）、`Console.Error` 之外的 sink 内诊断通道、列级迁移证据（属冻结的 PLAT-003）。 | `X-Pry-Upload-Warning`、`413 payload_too_large`、`GET /api/v1/media/policy` 的 `maximumBytes` |
| PLAT-007 | P2 | 媒体以**绝对文件路径**存进 `messages.image_path`（建表 `MemoryDatabase.cs:29`，写入 `:144`、`:225`），而非 ResourceId/URI；清理只扫该列（`MediaCleanupService.cs:22-26`），删除会话后媒体滞留到宽限期（`MemoryDatabase.cs:468-482`）。完成标准：消息只引用 `mediaId`，资源解析集中在 Resource Store，未来平台编码器可替换而不改 Runtime。 | `ChatMessage`、`/api/v1/media/{id}/content` |

## 9. 研究结论

本轮无变化（按 `PROJECT_PLAN.md` 第 5.1 节要求显式记录）。`docs/research/` 与 `COMPETITIVE_MATRIX.md` 仍未建立，属 RES-001，本文不代为创建。AstrBot/Satori/Private Companion/Nekro 的对比结论不在本轮采纳，本文所有决定均来自本仓库代码证据。
