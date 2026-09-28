# RT-002 Character Runtime 实现与交接

日期：2026-09-20 首轮，2026-09-23 增补触发门控。首轮报告对应 `runtime-verification/Pry_Project` 隔离 worktree；不代表共享工作区或发行分支已经集成。

**当前集成状态（2026-09-23 深夜）**：首轮实现已随 `5361dab` 入库；触发层增量（四组件 + 两段式 `TurnScheduler` + 本文与 `docs/phase2-runtime-contract.md`）已成提交 `8e039f9`，状态词按契约 §8.1 记 **`已提交待验`**（QA 判定未做）。本文其余段落里"未提交"的字面描述的是它被写下那一刻的工作树，不是今天。

**2026-09-24 增补（契约 §7 第 1 步"宿主端口"）**：触发层的频率半边第一次接到真实产品路径上——`TurnTakingSettings.ReplyFrequency` 进线协议，`PATCH /api/v1/preferences` 可设，`ConversationSessionService.CreateAsync` 供值进 `ConversationRuntimeOptions`，`GET .../runtime-state` 回读该实例生效的 `replyFrequency`/`triggerMode`/`isGroupChat`。群聊半边**没有**接：1.0 没有多参与者概念，`IsGroupChat` 没有诚实的生产者（判定与归属见 `docs/phase2-runtime-contract.md` §7 第 1 步）。发行默认仍是惰性链，未配置即与移植前逐位等价。状态词：**已提交待验**（§8.1 段① 自证完成后由作者点头入库为 **`44e52fc`**；段② 的前置自此解除，QA 从该提交建干净 worktree 复跑）。

## Implemented

接续已有、尚未提交的 ConversationRuntime 实现，修复输入持久化与成轮的竞态、异步旧 Turn 清理污染新 Turn、退避无限续期、Capability 等待误报 Idle、投递异常悬挂、主动候选绕过审核及空闲心跳阻止驱逐等问题。

2026-09-23 增补：把 Runtime 的"要不要成轮"从一处手写判断换成 MaiCore 的确定性触发层。新增 `MessageIntervalStats`（消息到达节奏）、`ReplyNecessity`（四层必要性评分）、`TurnGates`（必要性门 + 频率阈值门）、`IdleBackoffController`（连续空转退避）四个纯组件，并把 `TurnScheduler` 改写为"状态闸门 → 触发门控"两段式；`ConversationRuntime` 只提供自己算出的事实快照，不新增宿主端口、不查库。详见 Scheduling/Gate Logic 与 Tests 的 2026-09-23 小节。

普通输入先经过 `IRuntimeStore.AppendUserInputAsync`；成功后才进入 InputBuffer。事件接收串行，模型调用与消息间延迟不占用输入接收锁。输入存储失败不形成幽灵 Turn；后续状态效果失败则挂起，等待宿主显式恢复。

ProactiveScheduler 在生成前捕获 Runtime 实例、epoch 和会话进度。候选经过 Runtime 审核与 DeliveryScheduler；宿主在最终提交时再次检查活动、策略与历史水位。消息、Runtime 计划状态、主动配额和投递账本在同一 SQLite 事务提交。删除旧的直接发布主动消息入口。创建 Runtime 不加载模型，真正 Compose 时才解析 Provider。

未实现 Memory、Expression、Satori、插件系统或 Agent Harness。未 push、未发布；提交面见开头"当前集成状态"一段。

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

### 触发门控（2026-09-23，移植自 MaiCore）

`TurnScheduler.Evaluate` 现在是两段式：先状态闸门（本轮在飞、输出未清、输入未说完），再触发门控（攒够没有、值不值得进、刚空转过）。顺序不可交换——评分一条还没说完的半句没有意义。门控链按上游 `schedule_message_turn` 的次序落地：

1. `ReplyFrequency <= 0` → `Cancel`（静默消费，见下）。
2. 显式立即发送 / 被 @ / 被提及 → 直接成轮。这组信号不与"攒够几条"讲条件。
3. `IdleBackoffController` 仍在退避窗口内 → `Backoff(remaining)`。
4. 阈值 = `ceil(1/f)`（计数模式）或 `ceil(1/f²)`（必要性模式）。计数模式把阈值当硬门槛并允许空窗补偿补齐；必要性模式把阈值当压力分母，改由四层评分裁决。
5. `Delay` 决策映射成 `Backoff`，Runtime 据此设闹钟自醒——**待处理消息不会因为"没有新输入"而永久挂起**，这是本轮唯一由门控保证的下界。

配套事实全部由 Runtime 自己的状态算出，不查库、不读角色内部对象：`MessageIntervalStats` 维护外部消息到达节奏（滑动窗口 + 连发抖动抑制 + 均值下限），存在感台账记录窗口内角色自己发了几条。用户输入的落库发生在事件入口、早于任何闸门，所以静默消费丢弃的是进程内临时缓冲，不是消息本身。

参数默认值刻意保持惰性：`TriggerMode` 默认计数模式、`ReplyFrequency` 默认 1.0、`IsGroupChat` 默认 false，三者合起来的判定与移植前的"有输入即成轮"完全等价，既有 244 条 Core 用例无一改动。**评分权重与阈值经复核多数与上游同值且一律未经 Eval 标定，不得直接作为发行默认值**（逐值对照见 `docs/phase2-runtime-contract.md` §5，处置待作者裁定，见 Known Issues）。

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

- `RuntimeSettings` 新增 `TriggerMode`、`Necessity`、`IdleBackoff`、`Intervals` 四组参数；`ConversationRuntimeOptions` 新增 `IsGroupChat`、`ReplyFrequency`。2026-09-23 那一版写的"全部为进程内配置，未触及 HTTP/DTO"当时成立、现在已被本轮改掉：**`ReplyFrequency` 于 2026-09-24 进了线协议**（`TurnTakingSettings.ReplyFrequency`，`PATCH/GET /api/v1/preferences` 与 `runtime-state` 各多一个字段），`IsGroupChat` 仍只在进程内。原句留在原地不静默覆盖，就是为了说明"未触及"是当时的边界，不是永久承诺。
- `ConversationRuntime` 新增三个只读投影 `ReplyFrequency`/`IsGroupChat`/`TriggerMode`（实例创建时定死，进程内不变），只被 `runtime-state` 诊断读取，不在写路径上，也不新增 `IRuntimeOutputSink` 之外的对外通道（契约 §7.1 第 1 条）。
- `SchedulingContext` 末位新增可选 `TriggerFacts`；`SchedulingOutcome` 新增可选 `TriggerDetail`（只进 Trace，不参与判定）。两者都带默认值，既有位置参数调用不破坏。
- `GateDisposition.Cancel` 从死分支变成有生产者、有消费方的裁决：静默频率下由 `MaybeFormTurnLocked` 排空缓冲并记 `consumed_silently`。
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

2026-09-20 轮 Release 构建通过，全量测试为 **382 通过、0 失败、1 跳过**（Core 168/0/1、API 45/0/0、Client 7/0/0、App 162/0/0）。新增运行时/存储/宿主用例 16 项。Core 测试重新编译有 44 个既有 Intelligence/Expression 警告，该轮无新增警告；最后增量构建 0 警告。仓库扫描 205 个受跟踪文件、相关未跟踪文件空白检查及 Git 差异检查通过。自动化使用替身 Provider、临时 SQLite 与本机 HTTP；未以此宣称真实模型措辞或桌面体验已验收。

### 触发门控用例（2026-09-23）

新增 `RuntimeTriggerTests` 22 项，直接对移植的四层评分与两道门做纯函数断言，不经过 Runtime 生命周期：

- 节奏：连发抖动只刷新时间戳不产生样本、空窗后计入样本、均值下限、滑动窗口裁剪、从未见过外部消息时返回空。
- 阈值：两种模式下随频率单调不增、必要性模式阈值不低于计数模式（`ceil(1/f²)` vs `ceil(1/f)`）、`f<=0` 归零。
- 评分：被 @ 压过内容分（"上海"这种改口不该靠字数裁决）、角色连续自说自话的存在感扣分、频率因子的精确缩放（1.0→70、0.5→53）、压力分在 1/50/100 三档的饱和与"极久没说话"和"绝对沉默"同分、引号壳剥除、他方 assistant 名字作为受话人时不加分、私聊相关性口径。
- 门控：纯沉默（`pending < 1`）永不成轮、空窗补偿只补到 `threshold-1` 且能算出确切 `Delay`、退避只在群聊生效、答过话即复位、指数增长触顶、积压达到 `BypassPendingCount` 时越过退避。

新增 `RuntimeTriggerIntegrationTests` 4 项，端到端穿过真实 Runtime：

1. `ReplyFrequency = 0` 时用户消息**照常落库**（`AppendedInputs == 1`）但零次 Compose、缓冲被排空、相位回到 Idle、Trace 记 `consumed_silently`——静默是"不成轮"，不是"丢消息"。
2. 必要性模式下的群聊闲聊在被 `@小予` 之前保持沉默（0 次 Compose + `trigger:Wait`），被 @ 后恰好一次。
3. 频率 0.25 时阈值为 4，而缓冲只有 1 条消息；压缩节奏后门控在 150ms 内**自己醒来**成轮，证明 `trigger:Backoff` 是带闹钟的等待而非丢弃。
4. 达到频率阈值的一批积压只形成**一个** Turn，不会逐条开轮。

本轮（含触发门控）Release 构建为 0 警告 0 错误，全量测试 **671 通过、0 失败、2 跳过**（Core 268/0/2、API 129/0/0、Client 92/0/0、App 182/0/0）。移植刻意保持行为惰性：`TriggerMode=MessageCount` + `ReplyFrequency=1.0` + `IsGroupChat=false` 三者合起来的判定与移植前等价。这条不是靠"总数没变红"声称的——用 `--filter FullyQualifiedName!~RuntimeTrigger` 单独跑既有 Core 用例得 **242 通过 / 0 失败 / 2 跳过（总 244）**，`~RuntimeTrigger` 单独跑得 **26 通过**，两者相加正好是全量的 268，即新链条没有改动任何一条旧断言。`git diff --check`、`git diff --cached --check` 与 `scripts/verify-repository.ps1`（383 个受跟踪文件）通过。测试仍使用替身 Provider 与虚拟时钟，未以分数断言宣称真实群聊语境下的措辞已被验收。

### 宿主端口用例（2026-09-24）

新增 `tests/Pry.Api.Tests/ReplyFrequencyHostPortTests.cs` 3 项，跑真 HTTP + 真 SQLite + 真 Runtime，全程**不配任何模型**——这是"零模型调用"最硬的兜法：想调用也没有可调：

1. `Reply_frequency_round_trips_and_rejects_values_outside_zero_to_one`：`PATCH turnTaking.replyFrequency=0.25` 存得下、`GET` 读得回；`1.5` 与 `-0.1` 各回 `400`（字段 `turnTaking.replyFrequency`），且被拒的那次不留半个配置。
2. `Silent_frequency_reaches_the_live_runtime_and_never_opens_a_turn`：频率 0 经偏好进运行时——`POST .../turns` 之后 `runtime-state` 报 `replyFrequency=0`、`triggerMode=MessageCount`、`isGroupChat=false`、`phase=Idle`、`pendingInputCount=0`、`recentTurns` 为空，轨迹有 `consumed_silently`，而库里那条用户消息**恰有一条**。`recentTurns` 空是这里最值钱的一条断言：没有模型可选，成轮必然留下失败轮。
3. `Changing_the_frequency_replaces_the_live_runtime_and_the_new_one_carries_the_new_value`：改偏好之后旧实例先被读到 `started=false`（内存实例已换），下一次提交才带上新值——证明频率挂在运行时实例上而不是每次读取重算配置。

同轮补一处**由本刀自己引入的客户端回归**（`tests/Pry.App.Tests/ConversationSettingsEditorTests.cs`）：`ConversationSettingsEditor.BuildDraft()` 原先从零 `new TurnTakingSettings { ... }` 构造，在这个字段不存在时它恰好覆盖全部字段，加了字段之后它就变成**每保存一次"回复风格与轮次"面板就把频率抹回 1.0**——面板从不显示这个值，而 draft 会整份写进 `TurnTakingOverride` 并直接决定运行时开不开口。既有 `Editor_round_trips_existing_turn_settings` 用的是默认频率，所以它照旧绿：**这条"全绿"等于零信息**。修法是把面板不显示的字段原样带回——编辑器保存打开时的 `_source`，`BuildDraft()` 改为 `_source with { ... }`；新用例 `Saving_the_panel_keeps_a_frequency_the_panel_never_shows` 用 `ReplyFrequency = 0.25` 的源构造，改前必红。**没有**给面板加频率控件：把一个未标定的旋钮摆到用户面前是产品决定，不是这一步的权限（作者若要，见"Deferred Work"）。另需注意 `PATCH /preferences` 的 `turnTaking` 段是整段替换、不逐字段合并（对老字段一直如此），已写进 `docs/api-v1.md`。

本轮 Release 构建 0 警告 0 错误，全量测试连续两跑全绿：**675 通过 / 0 失败 / 2 跳过**（Core 268/0/2、API 132/0/0、Client 92/0/0、App 183/0/0；API 的 129→132 是上面三条宿主端口用例，App 的 182→183 是这条客户端回归用例）。`scripts/measure-fixed-eval.ps1` 两个目录各 `exit=0`（目录 74 条永久场景仍有效），`scripts/verify-repository.ps1` 393 个受跟踪文件通过，`git diff --check` 干净。契约 §5.1 的约束本轮照守：`src/Pry.Resources/appsettings.json` **没有**新增 `replyFrequency`，`TriggerMode` 与频率默认值一字未动，未配置时链条仍与移植前逐位等价。

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

触发门控（2026-09-23）对应的语义轨迹：

```text
ReplyFrequency=0
UserInput → 落库成功 → MaybeFormTurn → trigger:Cancel
       → consumed_silently → Idle        （composer_calls=0，appended_inputs=1）

群聊 / TriggerMode=ReplyNecessity / 未点名
UserInput("今天天气不错") → 攒够 → trigger:Wait   （detail=necessity_below_threshold）
UserInput("@小予 …")     → trigger:Run + 成轮原因 → 一次 Compose

ReplyFrequency=0.25 → 阈值=4 条，缓冲仅 1 条，此后无新输入
UserInput → trigger:Backoff(带自醒延时) → Runtime 设闹钟
Tick → 重新进入门控 → 空窗补偿补齐 → trigger:Run → 一次 Compose

外部消息连发 3 条（0s / 2s / 3s）
Record → 抖动抑制：样本数=0，但 LastExternalAt 推到 3s
       → IdleFor(6s)=3s      （刷屏刚停不会被误判成"已经空了很久"）
```

Trace 默认记录输入长度、原因码、身份和状态，不记录输入正文；TurnRecord 的输入摘要是进程内对话数据，不能直接当作可上传诊断。`trigger:` 的 detail 只带分数、门名和命中的配置词（词表来自 Pry 自己的 `ReplyNecessityOptions`，是有限常量集合），不回显用户原文。

## Known Issues

- 宿主仍使用 `TransitionalReplyAdvisor`；正式 ICharacterIntelligence、MemoryContext 与学习回报尚未接线。Runtime 的 DoNothing 能力已测试，不等于生产行为策略已完成。
- 桌面当前以周期活动报告提供 typing/idle。显式低延迟 TypingStarted/Stopped/UserReturned 的客户端协议与 UI 接线仍需前端协作；本轮未添加端点或改 UI。
- 状态恢复保留最后发言者/时间与连续角色回合数，不恢复未成轮输入缓冲；已接受的用户消息仍在历史中，Crash 后不会自动补回复。
- CapabilityRequest 附带的前置 Messages 尚无混合交付协议；当前以 Capability 等待为主。能力类型目前仍在 Runtime 命名空间，与 RT-001 的最终 Abstractions 归属约定待收敛。
- Provider 不响应取消时仍可能占用资源，Runtime 保证旧结果不能发；Provider 的统一超时和错误码归 PLAT-001。
- 测试快照含其他负责人的未提交基础文件。不能把整个 worktree 一次性暂存；共享工作区正在变化，本轮没有覆盖式合回。

触发门控（2026-09-23）带来的已知问题：

- **【登记缺陷，待作者裁定】评分参数里大部分数值与上游同值，而 §4.3 禁止照搬群聊调出来的阈值。** 2026-09-23 复核上游真源码后确认：`ReplyNecessityOptions` 只有 `TriggerScore`（80→70）与私聊/焦点相关度（40→45）是 Pry 改的，`PressureAtThreshold/PressureMax/PressureFullRatio/PressureIdleBonus/PresenceFreeRatio/PresenceFullRatio/PresencePenaltyMax/PresenceWindow` 与内容层五项（15/20/20/5/10）以及短反应罚（25）全部与上游 `reply_necessity.py` 的模块级常量同值；`MessageIntervalOptions` 只有 `Window` 是 Pry 自己的（1800 s→900 s），`BurstFloor`(5 s) 与 `MinimumAverage`(30 s) 与上游同值。此前本文件、文件头与许可表都写成"Pry 重写的初值"，那是错的，已就地改正并把逐值对照放进 `docs/phase2-runtime-contract.md` §5。处置已**部分裁决**（作者 2026-09-23 深夜）：**今天起这些数字不得进任何发行默认路径**——这一条从"我保守"升级为"作者裁定"；而"换一组 Pry 自定值"与"保留量级并由 Eval 标定覆盖"两条路线**明确押后**，不由本 Owner 代拍（逐值对照与裁决记录见 `docs/phase2-runtime-contract.md` §5 与 §5.1）。链条因此继续按惰性默认发布（`TriggerMode=MessageCount` + 频率 1.0 + 私聊）。`IdleBackoffOptions` 四项不在此列的猜测已被证伪：同日取到上游发行默认值（`official_configs.py:645-709`）后逐值复核，Pry 改了基数（15 s→10 s）、封顶（300 s→180 s）与起算次数（2→3），`BypassPendingCount = 6` 与上游同值——同样是未标定值。另一项未登记过的沿用面：`ReplyNecessityOptions` 的四张词表（提问/请求/征询/短反应）逐条来自上游 `reply_necessity.py:16-20` 的群聊语料，随 `ported` 类型合法入仓，但对私聊陪伴场景是未审校的借来措辞，与参数箱一起过 Eval。
- **上游自己的发行默认值就是这条保守链**（2026-09-23 读 `official_configs.py:537-643` 得）：`reply_trigger_mode="frequency"`（等价 Pry 的 `MessageCount`）、群聊/私聊频率默认均为 1.0、`mentioned_bot_reply=False`（提到名字不强制回）、`planner_interrupt_max_consecutive_count=0`（新消息默认**不**打断 planner）。三处直接推翻了本文此前的表述：惰性默认不是 Pry 独有的保守，Pry 的"提及即回"相对上游是**放宽**，而 Pry 现在就有的"插话即裁决"在上游默认下根本不发生。逐机制对照与偏差登记见 `docs/phase2-runtime-contract.md`（§5 逐值表、§6 偏差表）。
- ~~`IsGroupChat` 与 `ReplyFrequency` 目前只由 `ConversationRuntimeOptions` 承载，Api 宿主适配器还没有真实来源。~~ **2026-09-24 拆开处理**：频率半边已接（`TurnTakingSettings.ReplyFrequency` → `PATCH /api/v1/preferences` → `CreateAsync` 供值），生产入口上"低频/静默角色"第一次真的可达；群聊半边**判定为没有来源而不是待接线**——`ConversationRoom` 只有标题/角色/文件夹/置顶，`src/` 内没有 channel、成员或 Satori 适配层，架构立场也关着这条路（`Pry_1.0_Rewrite_Architecture_Plan_v3.md` 的 private-first 条、`PROJECT_PLAN.md` 把外部 IM 列在 1.0 之外）。伪造一个恒 false 开关不算完成，故 `IsGroupChat` 继续取 record 默认，退避与"提及必回"两条群聊分支在生产上仍不可达；降级落笔权在作者（契约 §7 第 1 步）。上游对应入口 `heartflow_manager.py:103` 的 `adjust_talk_frequency`，Pry 侧的等价物今天就是偏好端点，不需要新端点。
- `FocusActive` 在 `TurnScheduler.cs` 两处固定传 `false`（退避判定与必要性快照）。聚焦/专注状态属于角色内部对象，接入需要新增宿主端口；本轮不为此把 Character State 暴露给 Runtime。后果是必要性评分少一项正相关输入、空闲退避少一条豁免路径（后者只在群聊路径上生效，见 Tests），实际偏保守（更少成轮）。
- 节奏统计与存在感台账都是**进程内**状态，实例驱逐或重建后从零开始：刚复活的 Runtime 会短暂表现为"没有历史节奏"——均值落到下限托底、连续空转计数复位。跨重启的到达节奏持久化归 Persistence Owner，本轮未新增表或列。
- `GateDisposition.Cancel` 第一次可达，也第一次带来"配置误用即静默"的形状：宿主把 `ReplyFrequency` 配成 0 时，用户消息照常落库、但永不成轮，界面上不会出现任何解释。2026-09-24 频率进偏好端点之后，这个形状从"宿主自己配错"变成"用户点一下就得到"——所以本轮补了边界校验（越界/NaN 一律 `400 turnTaking.replyFrequency`，坏值不被读成静默）与 `docs/api-v1.md` 的显式说明；**"已静默"的用户可见投影仍未做**，那一项归 PLAT（投影）/作者裁定（契约 §6 DEV-08），本 Owner 没有替它造一个界面文案。
- 必要性模式的阈值 `ceil(1/f²)` 在低频率下增长远快于计数模式（f=0.3 时为 12 对 4）。它此时是压力分的分母而不是硬门槛，语义与移植前相反；只看参数名容易误配，需在契约文档里显式说明。

## Deferred Work

正式 Intelligence/Memory 契约整合、Capability 类型归属、低延迟桌面事件、typing/Turn 终局协议投影、真实模型 dogfood、持久事件游标与备份恢复仍交由相应负责人。未进入 RT-003～008 的实现范围。

触发层留给下一轮的三件事，按依赖排序：~~宿主侧把真实的 `IsGroupChat` / `ReplyFrequency` 接进 `ConversationRuntimeOptions`~~（2026-09-24：频率已接，群聊判为 1.0 无来源，见 Known Issues）；Eval 标定 `ReplyNecessityOptions` 与两个节奏/退避参数箱，标定通过才允许把默认 `TriggerMode` 切到 `ReplyNecessity`；专注端口与到达节奏的跨重启持久化。**另留一项待作者裁定，不在本 Owner 权限内**：要不要在"回复风格与轮次"面板上摆一个频率控件——本轮只保证它不被面板抹掉（`_source with`），把未标定旋钮暴露给用户是产品决定。上游 `runtime.py` 里还有 `MAX_INTERNAL_ROUNDS` 一类的轮内护栏与 `mode_policy` 的模式切换未搬，前者属 Agent Harness 范围（本任务明确不做），后者与 `FocusActive` 同源，一并等宿主端口。

研究决定：沿用计划中的 persistent runtime、debounce、idle backoff、候选复核原则。2026-09-23 一轮按 `docs/upstream-license-compatibility.md` 的留痕流程把参考锁到 MaiCore `fb4a8bd917d6fe373df9e3c22289d229e7f1b344`，读了真实源码（`reply_necessity.py`、`turn_gates.py`、`turn_scheduler.py`、`idle_backoff.py`、`mode_policy.py`、`runtime.py` 的间隔与阈值段），据源移植而非凭印象重建；四文件在 `licenses/THIRD-PARTY-NOTICES.md` 照搬来源表逐行登记，`TurnScheduler.cs` 记为 mechanism。逐条偏差登记在一致性矩阵任务里跟踪，本文件不重复。

回滚：只撤销本轮所列 Runtime、适配器、测试及文档的增量；不删除用户数据库、消息、配额或协作者文件。新增旁路状态是既有在途 Runtime 基础的一部分，发行前仍需 Persistence Owner 审核迁移与回滚基线。
