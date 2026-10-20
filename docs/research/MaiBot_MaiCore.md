# MaiBot / MaiCore：长期社交与角色 Runtime

> **⚖️ 照搬白名单内（作者 2026-09-21 裁决，`DOC-005`）：本项目允许逐行照抄。**
> 原话："我觉得需要照抄 MaiBot，否则现在开始自研一套 Runtime 极其拖慢开发进度。" Pry 代码许可现为 **AGPL-3.0-only**（2026-10-20 由 GPL-3.0-only 变更），上游 **GPL-3.0** 代码经 AGPL 第 13 条允许并入 AGPL 作品，所以照抄仍然合法；该方向是单向的——GPL-3.0 进 AGPL 可以，反过来不行。MaiCore 的模块划分与决策结构是 Phase 2（RT-002 ConversationRuntime）的**规范目标**，与上游不同的地方必须是契约里显式写出的决定 + 理由，不是"我当时想的一套"。
> **照抄义务（缺任一不进集成基线）**：① 锁 commit 并读真源码（已核实 `Mai-with-u/MaiBot@fb4a8bd917d6fe373df9e3c22289d229e7f1b344`）；② 抄哪个文件先查该子目录自己的 LICENSE——上游 EULA §2.1 明示子目录/第三方组件可能另属他人许可；③ 文件头写 `Ported from Mai-with-u/MaiBot@<sha>:<path>` 并保留上游版权行；④ 上游许可文本进 `licenses/upstream/`，并在 `licenses/THIRD-PARTY-NOTICES.md` 的照搬来源表登记一行。
> **仍然禁止**：照搬群聊触发阈值/频率/约 100 个 Runtime 的容量等历史参数；把上游 Python 类型或内部状态模型当作 Pry 契约；取用上游美术、Prompt 文案与示例数据。细则见 `docs/upstream-license-compatibility.md` §4。

来源：原研究 R1（2026-09-15），详见 [证据边界与记录索引](README.md)。下文源码观察均指当时读取的默认分支；无 commit SHA。本包沿用原研究的 MaiBot / MaiCore 称谓，不据此推断所有历史版本结构相同。

## 1. 定位与核心运行对象

核心是长期存在的 Session / Chat Runtime。消息进入已存在的状态机，改变其待处理输入、等待状态和规划条件，而不是每条消息都直接启动独立的 Prompt → LLM → Reply 请求。

原研究把它归为 Persistent Social / Character Runtime：Scheduler / Gate 决定何时开口，Planner 决定采取什么互动行为，Replyer 负责如何表达，学习和记忆在后台持续更新。群聊社交取向不能直接等同于 Pry 的私聊桌面产品。

## 2. 历史源码观察：启动与主调用链

`src/main.py` 中 `MainSystem` 组织配置、插件运行时、A_Memorix、Prompt、Emoji、ChatManager、周期 Session 保存、Memory Automation、WebUI 等服务；主消息进入 `chat_bot.message_process`。退出时协调 Memory、插件运行时、MCP 和异步任务关闭。

```text
Platform Driver
→ PlatformIOManager / Inbound Dispatcher
→ Message Receive Layer
→ HeartFlow Runtime Manager（get/create session runtime）
→ MaisakaHeartFlowChatting
→ Pending Queue + MessageTurnScheduler + Current State
→ Turn Gate（允许等待或不回复）
→ MaisakaReasoningEngine / Planner
   ├─ Memory / 人物资料 / 中期记忆
   ├─ Behavior Pattern
   └─ Tools
→ Expression Intent / Expression Retrieval
→ Replyer
→ Message Output / PlatformIO Broker

后台：Conversation → Behavior / Expression / Jargon Learner、Memory Writeback
```

这是原报告的架构级链路，不是每次请求都严格执行所有步骤的逐行调用栈。

## 3. 关键模块与状态

| 源码定位线索 | 原研究确认的职责 |
|---|---|
| `heartflow_manager.py` | 每 session 创建/维护 `MaisakaHeartFlowChatting`；并发锁、缓存、淘汰、回复频率调整 |
| `src/maisaka/runtime.py` | Runtime 的长期状态与服务组合 |
| `turn_scheduler.py` / `MessageTurnScheduler` | 根据 pending、waiting、focus、频率、强制触发、idle backoff 等决定何时提交 Planner |
| `turn_gates.py` | `ReplyNecessityTurnGate` 与 `FrequencyThresholdTurnGate` |
| `reasoning_engine.py` | `MaisakaReasoningEngine`；逻辑 Turn、Planner Context、工具、记忆、人物资料、规划中断 |
| `chat_loop_service.py` | 区分 planner、expression_selector、behavior_scenario_analyzer、emotion 等请求类型 |
| `maisaka_expression_selector.py` | 表达候选召回、候选池、结合上下文二次筛选 |
| `expression_learner.py` | 后台抽取表达；独立模型、session/global 并发门、最少消息门槛 |
| `memory_flow_service.py` | 人物事实异步写回 |

Runtime 维护 ChatLoopService、ChatHistory、MessageCache、Pending Turn Queue、Proactive Trigger、Debounce、消息时序统计、回复频率、Waiting、Current Tool State；并组合 `IdleBackoffController`、`PlannerInterruptController`、`ToolRegistry` 和各类 Learner。

原观察提到约 100 个活跃 Runtime 的保留上限及长期不用淘汰。这是历史实现参数，不是 Pry 应照搬的配置。

## 4. Timing、Gate 与 Behavior / Expression

`ReplyNecessityTurnGate` 综合 @、私聊/群聊、近期角色发言、回复频率、沉默时间、新输入和 focus；`FrequencyThresholdTurnGate` 将频率映射为消息阈值，并补偿长期沉默。原研究特别记录：相关 Gate 要求真实新输入，不应只因空闲增长而反复生成回复。这不等于项目没有独立主动触发机制。

Planner 与 Replyer 分开：前者回答“现在做什么、是否用工具、采用什么互动策略”，后者实现语言表达。不能把 Behavior 和 Expression 合并为一份 Persona Prompt。

`BehaviorPatternSelector` 以“场景 → 行为 → 后果”为经验单位，候选字段包括 `count`、`score`、`success_count`、`failure_count`、`activation_count`、`learning_type`、`scene_cluster_score`、`profile_tag_match`。这些经验提供互动参考，原报告没有证明它是模型参数训练或正式强化学习算法。

Expression 记录包含 `situation`、`style`、`count`、`session_id`；学习写入涉及 `(situation, style, source)`。选择过程按 session / expression group 查可用表达，可用 embedding / vector intent 召回，再经 Selector 按真实上下文筛选，向 Replyer 注入“表达习惯参考”。成长主要表现为行为/表达数据库的变化，不能推断为在线微调模型。

## 5. Memory 与证据

`A_memorix/core/` 下有 embedding、retrieval、runtime、storage、strategies、utils。人物事实写回链路为：

```text
Bot reply
→ PersonFactWritebackService Queue
→ 后台分析附近聊天
→ 提取稳定人物事实
→ 写入人物记忆
```

原研究确认的约束是：事实需由目标用户自己的发言支持，附近聊天用于消歧，不应把他人对该用户的猜测直接当作事实。这是 Pry 的 Evidence / 记忆污染防护的重要参考。

报告没有完整展开 A_Memorix 的全部检索评分、存储 Schema 或冲突解决流程，不能据目录名补全这些算法。

## 6. 平台和插件

`src/platform_io` 包含 drivers、registry、routing、route_key_factory、dedupe、outbound_tracker、manager、types。`PlatformIOManager` 组合 DriverRegistry、Send/ReceiveRouteTable、MessageDeduplicator、OutboundTracker、InboundDispatcher，负责路由、去重和出站状态；Driver 支持 register/start/stop/remove。

`plugin_runtime` 包含 host、runner、protocol、transport、capabilities 等。`PluginRunnerSupervisor` 管理独立 Runner 子进程、内部 RPC、健康检查、重载、重启与启动超时；报告列出 AuthorizationManager、CapabilityService、APIRegistry、ComponentRegistry、Event/HookDispatcher、MessageGateway、RPCServer、MsgPackCodec。

进程隔离的存在不等于原研究已经完成恶意插件隔离或安全边界审计。

## 7. 对 Pry 的建议：借鉴与不照搬

**1.0 优先借鉴**：会话 Runtime、输入缓冲、Scheduler / Reply Gate、显式等待和中断；Behavior / Expression 分离；异步事实写回和证据约束；平台 IO 与领域解耦。

**不要照搬**：群聊触发阈值、频率参数和容量；完整 Learner / Jargon / 子代理组合；上游 Python 类型和内部状态模型；复杂进程插件宿主。多个模型阶段也不代表本地小模型上的成本与延迟可以接受。

**未来参考**：更成熟的行为后果学习、表达分组召回、插件独立 Runner 与 RPC 生命周期。它们需独立需求和验证，不是 1.0 默认工作项。

Pry 的 ConversationRuntime、ResponsePlan、DeliveryScheduler、TurnId / GenerationId / stale-result fencing 来自 Pry 的设计约定；不能把这些名称反写成 MaiCore 已采用的完全相同契约。

## 8. Caveat 与后续验证

- 原研究是主干追踪，不是全仓逐行审计；版本未锁定。
- 原报告联合文本末尾截断，范围见 README；本文件所用 MaiBot 主报告部分可读取。
- 有 PlannerInterruptController 不等于所有模型、工具、发送阶段都已证明可取消或不会发出旧结果。
- Pry 应验证连续输入、生成中插话、发送中插话、空闲无新输入、Runtime 淘汰/恢复、事实来源混淆。以上是 Pry 测试建议，不是上游通过证明。
- 跨平台身份、多 Core 同步和 Pry 的用户—角色关系边界仍须由 Pry 自己定义。
