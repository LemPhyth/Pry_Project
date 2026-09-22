# RT-002 Character Runtime 实现与交接

日期：2026-09-20。此报告对应 `runtime-verification/Pry_Project` 隔离 worktree；不代表共享工作区或发行分支已经集成。

## Implemented

接续已有、尚未提交的 ConversationRuntime 实现，修复输入持久化与成轮的竞态、异步旧 Turn 清理污染新 Turn、退避无限续期、Capability 等待误报 Idle、投递异常悬挂、主动候选绕过审核及空闲心跳阻止驱逐等问题。

普通输入先经过 `IRuntimeStore.AppendUserInputAsync`；成功后才进入 InputBuffer。事件接收串行，模型调用与消息间延迟不占用输入接收锁。输入存储失败不形成幽灵 Turn；后续状态效果失败则挂起，等待宿主显式恢复。

ProactiveScheduler 在生成前捕获 Runtime 实例、epoch 和会话进度。候选经过 Runtime 审核与 DeliveryScheduler；宿主在最终提交时再次检查活动、策略与历史水位。消息、Runtime 计划状态、主动配额和投递账本在同一 SQLite 事务提交。删除旧的直接发布主动消息入口。创建 Runtime 不加载模型，真正 Compose 时才解析 Provider。

未实现 Memory、Expression、Satori、插件系统或 Agent Harness。未提交、推送或发布。

## Runtime State Changes

| 状态 | 进入条件 | 离开条件 |
|---|---|---|
| Idle | 没有活动 Turn、缓冲或排队输出 | 输入、主动投递、挂起 |
| CollectingInput | 收到输入或旧轮输入退回缓冲 | debounce 到期、typing 等待或形成 Turn |
| WaitingForTurn | debounce/typing/backoff 未满足，或 Capability 执行中 | 定时器、typing stop、匹配的 CapabilityResult、新输入 |
| Planning | 成轮、Gate、Compose 或 Capability 结果续接 | 沉默、暂缓、投递、打断或失败 |
| Delivering | 取得计划许可并排队 | 已发完、用户插话、typing、返回、过期或投递失败 |
| Suspended | 宿主挂起或状态存储不可用 | 仅显式 Resume |

当前 `Planning` 包含生成阶段；`Interrupted` 是 Turn 终局与轨迹事件，随后进入收集或空闲状态。Capability 等待通过 `_capabilityPending` 区分，仍保留 ActiveTurn，不能误报可驱逐。

心跳只推进空闲检查，不更新用户活动时间。实例重建首先递增权威 epoch，旧实例即使有不响应取消的调用也失去投递资格。恢复只清扫未完成计划，不恢复计时器或自动重发旧输出。

## Scheduling / Gate Logic

- InputBuffer 考虑 debounce、末尾标点/半句、typing 租约、最大等待、显式立即发送和投递背压。0/50/350ms 到达的 A/B/C 合为一轮。
- typing 报告续租；停止报告从停手时重新计时。失联后租约到期，避免永久等待。即使 buffer 硬上限到达，Reply Gate 仍检查 typing。
- 连续打断按上限退避；到达退避截止时间后放行，不再次无限延后。暂缓重试有最小间隔。
- Runtime 只判断新输入、typing、有效身份、待投递输出与 Capability 状态。`IReplyAdvisor` 决定社交行为；`ShouldReply=false` 或 `Intent=DoNothing` 都合法。
- 每张计划提供完整消息列表及延迟提示；Runtime 不按固定字符数切片。首条与条间延迟由提示或既有表达节奏参数决定。

## Interruption Semantics

`TurnIdentity(TurnId, GenerationId, FencedRevision)` 与取消令牌、当前 ActiveTurn 引用共同保护异步边界。当前重规划创建新 TurnId；GenerationId 保留为 1，Capability 续接沿用同一身份，不声称已实现同轮多代模型重试。

非 backchannel 新内容即使很短也触发重规划，避免把“上海”等重要改口吞成轻量回应。纯 backchannel 可沿用当前普通回复；主动消息遇到任何用户输入均让位。生成期间被打断且尚未发出内容时，原输入退回缓冲，与新输入一起规划。已经交付的气泡保留，剩余气泡取消；不撤回用户收到的内容。

显式 Cancel 清空待处理缓冲，不再为被取消的原输入自动生成下一轮。TypingStarted 和 UserReturned 使旧候选失效。异步旧结果只能结束自身，不能关闭、重排或改写较新的活动 Turn。

Delivery 对已出队、尚在提交的气泡也跟踪取消。最后检查与原子提交定义竞态边界：用户失效先持久化则旧提交拒绝；提交先完成则该气泡属于已交付历史，剩余输出取消。提交异常不自动重试，避免未知提交结果造成重复发送；后续历史刷新补齐已提交但未收到 SSE 的消息。

Capability 请求携带 Turn 的取消令牌。结果必须同时匹配 CapabilityId 和 TurnIdentity；旧结果不消耗新请求的等待状态。支持一次结果续接与沉默结果；再次请求 Capability 明确以 `capability_loop_not_supported` 结束，不实现无限 Tool Loop。

## Contracts Consumed

- `IRuntimeStore`：输入追加、状态快照、权威 epoch、计划申请、条件气泡提交、放弃与启动清扫。Runtime 无 SQL、SQLite、HTTP 或 Avalonia 依赖。
- `IReplyAdvisor`、`IInterruptClassifier`、`IReplyComposer`：分别提供行为建议、插话判断和结构化 ResponsePlan；不把可变 CharacterState 塞进 Runtime。
- `IRuntimeOutputSink`：已提交消息、状态投影、typing、Turn 终局与 Capability 请求。
- 既有 `ProactiveAttempt`、策略/配额账本与 `CompanionOperationCoordinator`：只由宿主适配器消费。

## Contracts Changed

- `DeliveryScheduler.Enqueue` 新增可选候选过期时间；每条气泡提交前复核 TTL。
- `CompleteProactiveAttemptAsync` 新增可选 Runtime 投递许可；提供许可时，计划与主动配额在一个事务检查并提交。旧仓储调用保持兼容。
- `ConversationSession` 保存对应的 DatabaseRuntimeStore；新增捕获候选、统一投递及活动转发的内部宿主方法。
- 无新增 HTTP 路径或 DTO。沿用 HTTP/JSON/SSE，未实现 WebSocket。`message.created` 同时投影既有 `text` 与 `content`，保持消费者兼容。主动会话现在也出现 `session.ready` 与 `turn.state`。
- 既有活动报告在有效新序号下，把 typing/返回转交活跃 Runtime；无活跃 Runtime 时不为心跳加载模型或创建会话。
- 未改 schema 版本或新建表；修复 Runtime 快照 NULL 读取、快照不得前进 epoch、气泡顺序及计划归属校验。单调 epoch 仍由 `BumpEpoch` 拥有。

## Tests

新增 `RuntimeRaceTests`：精确连发时间、typing 续租、显式取消不重规划、旧模型返回不污染新 Turn、错误 Capability 结果、30 分钟返回与驱逐、慢/失败输入追加、Commit 异常收尾、主动消息 Return/沉默/TTL 闸门、退避截止时间。

补充真实 SQLite 测试：Runtime epoch 与主动配额事务绑定、过期许可不产生消息、不增加配额、重复完成不重复发送。API 回归验证主动发送经过 AgentSending 状态且没有加载模型。

既有用例继续覆盖生成期间插话、第一条 bubble 后插话、第二条取消、忽略取消的迟到模型、Capability 改口、跨会话隔离、驱逐重建、Crash 遗留计划清扫、策略/历史/配置变化、SSE 与删除恢复。

最终 Release 构建通过，全量测试为 **382 通过、0 失败、1 跳过**（Core 168/0/1、API 45/0/0、Client 7/0/0、App 162/0/0）。新增运行时/存储/宿主用例 16 项。Core 测试重新编译有 44 个既有 Intelligence/Expression 警告，本轮无新增警告；最后增量构建 0 警告。仓库扫描 205 个受跟踪文件、相关未跟踪文件空白检查及 Git 差异检查通过。自动化使用替身 Provider、临时 SQLite 与本机 HTTP；未以此宣称真实模型措辞或桌面体验已验收。

## Runtime Trace Examples

以下为测试对应的语义轨迹，不包含用户原文：

```text
0ms UserInput(A) → WaitingForTurn
50ms UserInput(B) → WaitingForTurn
350ms UserInput(C) → WaitingForTurn
1550ms TurnDue → Planning (input_count=3) → 一次 Compose

Turn 41 / epoch 7 → Planning
UserInput → close:Interrupted → epoch 8 → Planning(Turn 42)
Turn 41 late result → dropped_late_model_result → 无投递

Delivering → bubble #0 committed
TypingStarted → cancel remaining #1 → epoch advanced → Idle

Candidate captured(epoch 9) → Review Approved
UserReturned → epoch 10
Delivery → RejectedStale → 无消息/配额变更
```

Trace 默认记录输入长度、原因码、身份和状态，不记录输入正文；TurnRecord 的输入摘要是进程内对话数据，不能直接当作可上传诊断。

## Known Issues

- 宿主仍使用 `TransitionalReplyAdvisor`；正式 ICharacterIntelligence、MemoryContext 与学习回报尚未接线。Runtime 的 DoNothing 能力已测试，不等于生产行为策略已完成。
- 桌面当前以周期活动报告提供 typing/idle。显式低延迟 TypingStarted/Stopped/UserReturned 的客户端协议与 UI 接线仍需前端协作；本轮未添加端点或改 UI。
- 状态恢复保留最后发言者/时间与连续角色回合数，不恢复未成轮输入缓冲；已接受的用户消息仍在历史中，Crash 后不会自动补回复。
- CapabilityRequest 附带的前置 Messages 尚无混合交付协议；当前以 Capability 等待为主。能力类型目前仍在 Runtime 命名空间，与 RT-001 的最终 Abstractions 归属约定待收敛。
- Provider 不响应取消时仍可能占用资源，Runtime 保证旧结果不能发；Provider 的统一超时和错误码归 PLAT-001。
- 测试快照含其他负责人的未提交基础文件。不能把整个 worktree 一次性暂存；共享工作区正在变化，本轮没有覆盖式合回。

## Deferred Work

正式 Intelligence/Memory 契约整合、Capability 类型归属、低延迟桌面事件、typing/Turn 终局协议投影、真实模型 dogfood、持久事件游标与备份恢复仍交由相应负责人。未进入 RT-003～008 的实现范围。

研究决定：沿用计划中的 persistent runtime、debounce、idle backoff、候选复核原则；本轮未重新获取外部参考项目源码，未作新的源码对标结论。

回滚：只撤销本轮所列 Runtime、适配器、测试及文档的增量；不删除用户数据库、消息、配额或协作者文件。新增旁路状态是既有在途 Runtime 基础的一部分，发行前仍需 Persistence Owner 审核迁移与回滚基线。
