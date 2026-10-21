# Pry 1.0 技术架构计划书（v3：研究与早期验证修订版）

> **2026-10-21 历史版本说明**：作者已明确要求以 HDS 启发的持续叙事更改 AI 回复架构。当前目标和实施路线见 [v4 架构重写计划](docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md)，任务与负责人见 [共享总计划](docs/plans/PROJECT_PLAN.md)；本文的旧回复生成中心与八阶段排期不再指挥新架构。已有成果、数据/隐私/许可约束及有效验收证据按 v4 逐项继承，不因改写目标而被宣告失效或完成。

> 状态：Draft  
> 文档版本：v3（2026-09-20）  
> 目标版本：Pry 1.0  
> 首发平台：Windows  
> 核心定位：Local-first Character Runtime  
> 面向对象：Codex / 开发者  
> 原则：角色存在感优先，Agent 能力后置

---

## 0. 文档目的

Pry 1.0 不是通用 Agent Framework，也不是“角色 Prompt + 聊天 UI”。

Pry 1.0 的正式定义是：

> **一个 Local-first 的长期 AI Character Runtime。**

它首先解决：

- 角色为什么现在说话；
- 角色是否应该说话；
- 用户是否还没说完；
- 用户插话后是否应该停止原计划；
- 同样的意图应该怎样以该角色自己的方式表达；
- 角色应该记住什么；
- 不同会话如何共享长期记忆但保持独立短期上下文；
- 角色怎样形成长期行为习惯和表达习惯。

工具、插件、Satori、Hosted Core、多 Core 同步、完整 Agent Harness 都属于次级或未来能力。

---

## 0.1 本次修订与基线

基线为用户提供的 `Pry_1.0_Rewrite_Architecture_Plan.md`（标题为“Pry 1.0 技术架构计划书（重写版）”）。该文件已采用此前 v2 讨论确定的 Local-first Character Runtime 结构。本次保留原主架构、章节编号、八个 Phase 和未来能力附录，通过子章节补充研究、接口与验证要求。

本次吸收用户提供的市场/源码研究结论，不代表重新审计了各项目的最新源码。上游实现细节应在正式研究记录中补齐仓库、commit、路径与证据；尚无证据的判断标为待核验，不写成已验证事实。

修订重点：

- Competitive/Reference Research 进入正式项目流程，但不新增独立开发 Phase。
- 明确 private-first、local-first、desktop-native 的独立角色产品定位。
- 将 Private Companion 启发转为主动消息审核链、正式模块 Contract，以及后续表达模式 backlog。
- 保持 1.0 少而完整，不复制生活模拟和信息服务功能集合。
- 以开发者长期 dogfood 与少数自愿用户的纵向内测补充固定 Eval。

本次不新增大型功能交付承诺；“研究到”“接口可扩展”“未来可做”均不等于进入 1.0 MUST。

---

# 1. 约束关键字

本文使用以下约束：

- **MUST**：Pry 1.0 必须实现。
- **SHOULD**：强烈建议；可以因明确技术原因或个人开发者的时间、维护成本约束延后，并记录理由。
- **MAY**：可选；不构成阶段或发布门槛。
- **FUTURE**：Post-1.0 能力；1.0 不得提前扩张实现。
- **MUST NOT**：1.0 明确禁止。

---

# 2. 产品优先级

开发优先级固定为：

```text
1. 角色连续性
2. 对话节奏
3. 行为一致性
4. 表达一致性
5. 长期记忆
6. 关系连续性
7. 简单本地能力
8. 插件扩展
9. 外部聊天平台
10. 完整 Agent Harness
```

任何明显增强第 8～10 项、却拖累第 1～6 项的设计 SHOULD 被拒绝。

---

## 2.1 产品定位与差异

Pry 是面向个人用户的独立角色产品。用户安装后接触的是普莱和长期相处体验，无需先理解 Bot Framework、Agent、插件宿主或 Memory Backend。

| 定位 | 对架构和产品的含义 | 1.0 边界 |
| --- | --- | --- |
| private-first | 以一个用户与一个 CharacterInstance 的长期关系为核心，优先解决私下相处、关系与记忆连续性 | 本地个人使用；不引入群聊人格或社交平台运营功能 |
| local-first | 角色状态与私人数据默认在本机；无账号、无官方服务器也能使用 | 本地模型支持离线；用户可选 API 时清楚说明发送数据的边界 |
| desktop-native | 桌面交互、typing、离开与回来等以 Pry 自有事件表达，进入同一 Runtime | 复用已有客户端事件；不因此新增屏幕监控、应用内容采集或复杂桌宠系统 |
| 独立角色产品 | Pry 自己拥有 Runtime、角色状态与交互生命周期，模型和简单能力是可替换组件 | 不作为 AstrBot 插件发布，不把通用 Agent Framework 当产品目标 |

角色连续性应由 Identity、Relationship、Memory、Behavior/Expression 和 Runtime 共同承载，不能等同于某个模型或某条 Prompt。1.0 重点验证重启、切换会话、换 Provider、备份恢复后是否仍能感到是同一个角色；跨平台与多设备实时连续性仍属 FUTURE。

研究形成的产品假设是：愿意使用 AI 陪伴的用户，可能需要更容易安装、本机可用且具有长期角色连续性的产品。参考项目说明相关方向值得研究，但不能据此宣称 Pry 已获得市场验证。该假设以少数真实用户的持续使用来检验。

## 2.2 Competitive/Reference Research：正式项目管理流程

研究服务于当前架构决策和体验问题，不以追齐竞争项目功能数为目标。由开发者负责维护；无需增加专职研究角色。

建议仓库目录：

```text
docs/research/
├─ README.md
├─ MaiBot.md
├─ AstrBot.md
├─ Satori.md
├─ Nekro-Agent.md
├─ Private-Companion.md
└─ COMPETITIVE_MATRIX.md
```

`README.md` 记录研究范围、证据标准、更新时间与决策入口。各项目文件围绕具体问题记录调用链，不只复述 README。`COMPETITIVE_MATRIX.md` 汇总安装门槛、角色连续性、私聊/桌面定位、数据归属、Runtime、学习/记忆边界、扩展方式与维护成本；未核验的格子明确写“待核验”。

| 研究对象 | 当前参考问题 | 对 Pry 的范围约束 |
| --- | --- | --- |
| MaiBot / MaiCore | 持续会话、节奏、行为与表达学习如何协同 | 参考 Runtime 思路，不照搬整个系统 |
| AstrBot | Provider、Capability、插件生命周期如何抽象 | 只取轻量工程参考，不引入完整 Agent 能力 |
| Satori | 协议与平台边界、资源传输如何分离 | 研究可进行，实现仍在 Post-1.0 |
| Nekro-Agent | 扩展机制与 Agent 能力的成本、边界是什么 | 待证据支持后再比较，不把其能力当 1.0 清单 |
| Private Companion | 陪伴行为、主动消息审核、表达适用条件与记忆桥接 | 吸收机制，不复制插件宿主耦合或功能堆叠 |

每条影响决策的研究记录至少包含：

```text
Question / ResearchDate
Repository / CommitOrTag / FilePath / Symbol
ObservedBehavior / Evidence
Interpretation / Uncertainty
PryDecision: Adopt / Adapt / Defer / Reject
AffectedContract / Phase / EvalCase
ScopeImpact / FollowUp
```

正式流程：

1. 当前 Phase 开始前，检查与本阶段有关的已有研究；无需每阶段重读全部项目。
2. 遇到真实架构疑问或自然度问题时，进行一次有边界的源码追踪，保存版本与路径。
3. 区分源码事实、解释和 Pry 的产品假设，更新矩阵。
4. 将结论落为接口约束、现有任务、Eval 用例或明确延后的 backlog；涉及边界变化时记录简短 ADR。
5. 阶段报告注明采纳/拒绝/延后理由；没有新研究时写“无变化”。

进入 1.0 的新建议必须说明它修复了哪个既有目标、增加多少工作、是否挤占角色核心体验。不能以“竞品有”作为范围扩张理由。未决研究可保留，不要求完整竞品审计后才继续开发。

---

# 3. Pry 1.0 范围

## 3.1 MUST

Pry 1.0 必须发布：

- Windows Pry Client
- Windows Pry Local-Core
- SQLite 本地持久化
- 内置轻量 2B 级本地模型
- OpenAI-compatible Provider
- 多会话
- 每个会话独立短期上下文
- 同一 User × Character 跨会话共享长期记忆
- Persistent Conversation Runtime
- Input Buffer
- Turn Scheduler
- Reply Gate
- Interruption Controller
- Response Plan
- Message Segmentation
- Delivery Scheduler
- Character Identity
- Relationship State
- Lightweight Mood
- Behavior Planner
- 基础 Behavior Pattern
- Expression Retrieval
- Expression Learning
- 异步 Memory Writeback
- Memory 查看 / 编辑 / 删除
- 简单 Capability 系统
- 简单 Plugin 系统
- 数据导出 / 备份 / 恢复
- Developer Mode
- 固定 Conversation Eval

## 3.2 MUST NOT

Pry 1.0 不实现：

- Android Local-Core
- iOS Local-Core
- macOS Local-Core
- Linux GUI Client
- Hosted Core
- Multi-Core Sync
- NAT 穿透
- P2P
- Satori Gateway Runtime
- QQ / Telegram / Discord 接入
- 完整 MCP Host
- 通用 Tool Loop
- Browser Agent
- Shell Agent
- Computer Use
- SubAgent
- Plugin Marketplace
- 插件独立进程沙箱
- CRDT
- 分布式身份系统

---

## 3.3 研究启发的范围归属

| 项目 | 1.0 的处理 | 后续范围 |
| --- | --- | --- |
| Competitive/Reference Research | 轻量文档与决策流程，嵌入现有 Phase | 按问题更新，不建设研究平台 |
| Memory / Character Intelligence Contract | 在已有模块之间明确输入、输出、错误与状态归属 | 不要求微服务、RPC 或第三方桥接 |
| Generate → Review → Send | 主动消息若进入本版实现，必须经过该链；优先复用 Reply Gate、Interruption、Delivery | 主动触发覆盖范围可延后，不新增完整 Proactive Engine 交付门槛 |
| ExpressionPattern 扩展字段 | 记录字段语义与 backlog，保留当前最小结构 | Channel 等条件不要求全部实现 |
| 小规模纵向内测 | 纳入已有 Eval 与发布复盘 | 不建设大规模统计或遥测系统 |

新闻、天气、衣柜、QQ 空间、阅读归档、复杂日程、做梦和完整生活模拟均不是 Pry 1.0 目标。简单 Plugin API 的存在不意味着必须交付这些插件。若自然度问题能用节奏、记忆或表达规则解决，应先修正现有核心链路。

---

# 4. 总体架构

```text
                         Pry.Client
                            │
                     Pry Protocol
                     HTTP + WebSocket
                            │
                            ▼
                       Pry.Core
                            │
              ┌─────────────┴─────────────┐
              │                           │
      Character Runtime               Persistence
              │
      ┌───────┼────────┐
      │       │        │
   Behavior Expression Memory
      │       │        │
      └───────┼────────┘
              │
           Planner
              │
       ┌──────┴──────┐
       │             │
     Reply       Capability
       │             │
 Model Provider    Plugin
```

Pry.Client、Pry.Core、未来外部平台适配层必须保持解耦。

---

# 5. 最重要的架构原则：消息不是一次任务

Pry MUST NOT 被实现成：

```text
Incoming Message
→ Build Prompt
→ LLM
→ Split Reply
→ Done
```

必须改为：

```text
Incoming Event
      ↓
ConversationRuntime
      ↓
Pending Input Queue
      ↓
Debounce / Wait
      ↓
Turn Scheduler
      ↓
Reply Gate
   ┌──┴──┐
   │     │
  No    Yes
         ↓
   Behavior Planner
         ↓
    Response Intent
         ↓
  Memory Retrieval
         ↓
Expression Selection
         ↓
       LLM
         ↓
   Response Plan
         ↓
 Delivery Scheduler
```

核心原则：

> **消息是对一个持续存在的 Conversation Runtime 的状态更新，而不是一次独立 Chat Completion。**

---

# 6. 主要运行时对象

```text
PryRuntimeHost
│
├─ CharacterInstanceRuntime
│  ├─ ConversationRuntime A
│  ├─ ConversationRuntime B
│  └─ ConversationRuntime C
│
├─ MemoryService
├─ RelationshipService
├─ BehaviorService
├─ ExpressionService
├─ ModelProviderRegistry
├─ CapabilityRegistry
└─ PluginRegistry
```

---

# 7. User × Character

长期角色关系的主实体定义为：

```text
User × Character = CharacterInstance
```

CharacterInstance 保存或引用：

- Long-term Memory
- Relationship State
- Learned Behavior Patterns
- Learned Expression Patterns
- Character State
- Conversations

即使 1.0 只有单本地用户，也 MUST 使用稳定 ID。

建议至少定义：

```text
PryUserId
CharacterId
CharacterInstanceId
ConversationId
MessageId
MemoryId
BehaviorPatternId
ExpressionPatternId
DeviceId
PluginId
CapabilityId
```

UUID 推荐。

---

# 8. Conversation

每个 Conversation 拥有独立短期上下文。

```text
CharacterInstance
│
├─ Conversation A
│   └─ Short-term Context A
│
├─ Conversation B
│   └─ Short-term Context B
│
└─ Shared Long-term Memory
```

Conversation A 和 B MUST NOT 自动共享最近聊天历史。

它们 MAY 检索同一套长期记忆。

建议数据结构：

```text
Conversation
{
    ConversationId
    CharacterInstanceId
    Title
    CreatedAt
    UpdatedAt
    Archived
}
```

---

# 9. Message

建议：

```text
Message
{
    MessageId
    ConversationId
    Role
    Content
    CreatedAt
    Source
    Status
    Metadata
}
```

Role 至少支持：

```text
User
Character
System
Capability
```

Source 1.0 至少支持：

```text
LocalClient
System
Plugin
```

未来平台来源必须通过 Adapter 映射，不能污染 Character Runtime。

---

# 10. ConversationRuntime

每个活跃会话都必须拥有 ConversationRuntime。

逻辑结构：

```text
ConversationRuntime
{
    ConversationId

    PendingInputQueue
    RuntimeState
    CurrentTurn
    PendingOutputs
    LastUserActivity
    LastCharacterActivity

    InputBuffer
    TurnScheduler
    ReplyGate
    InterruptionController
    DeliveryScheduler
}
```

Runtime 可以在长期不活跃后从内存卸载。

持久化状态必须足以在程序重启后重新构建。

---

# 11. Runtime State

SHOULD 支持类似：

```text
Idle
CollectingInput
WaitingForTurn
Planning
Generating
Delivering
Interrupted
Suspended
```

具体枚举可调整。

MUST NOT 假设某个 Task 永久存活。

进程可以死，角色状态不能跟着丢失。

---

# 12. Input Buffer

Input Buffer 用于判断用户是否还在说话。

例如：

```text
A
200ms 后 B
300ms 后 C
```

系统 SHOULD 能把它们视为同一输入阶段。

可考虑：

- 时间间隔
- 本地 typing 状态
- 标点
- 语义是否完整
- 当前是否正在发送 Pry 的消息
- 是否属于用户插话

1.0 可先使用时间规则 + 少量启发式，不要求完美语义模型。

---

# 13. Turn Scheduler

Turn Scheduler 决定：

> 当前积累的信息什么时候正式进入一次 Character Turn？

SHOULD 考虑：

- Pending Input
- Debounce
- 当前 Runtime State
- 是否已有 Plan
- 是否仍在 Delivery
- Interrupt
- 用户最近活动

Turn Scheduler 必须独立于 Reply Generator。

---

# 14. Reply Gate

Reply Gate 回答：

> Pry 这次要不要说话？

以下必须成为合法输出：

```text
DoNothing
```

Reply Gate SHOULD 考虑：

- 是否直接提问
- 是否明确叫角色
- 消息是否像未完成句
- 上一次是谁说话
- 当前关系状态
- 当前节奏 / cooldown
- 当前话题状态

第一版允许规则实现。

未来可换小模型或混合策略。

---

# 15. Interruption Controller

插话是一等运行时能力。

例如：

```text
Pry 计划：
A
B
C

已发送 A

User：
等等我刚才说错了
```

系统 MUST：

- 停止尚未发送的 B/C
- 标记旧 Plan stale
- 注入新消息
- 重新判断继续 / 修改 / 取消

不能机械把旧消息发完。

---

# 16. Response Plan

LLM 输出不等于单条消息。

建议：

```text
ResponsePlan
{
    Intent
    Messages[]
    CapabilityRequest?
    DeliveryHints
}
```

例如：

```text
Message 1:
草

Message 2:
你到底在整什么
```

而不是强行合并成一条完整助手回答。

---

# 17. Delivery Scheduler

负责：

- 首条延迟
- 多消息间延迟
- typing 模拟
- pending message
- cancel
- interrupt
- delivery state

延迟 SHOULD 受到：

- 文本长度
- 表达习惯
- 当前节奏
- 情绪 / 状态

影响。

MUST NOT 永远只是固定字符数公式。

---

## 17.1 Proactive Message：Generate → Review → Send

吸收 Private Companion 研究中“候选生成与发送分离”的启发。Pry 的主动消息设计采用：

```text
已有的本地事件 / 有限触发条件
↓
Reply Gate / Cooldown：是否值得生成候选
↓
Generate：生成 ProactiveCandidate，尚未发送
↓
Review：结合最新 Runtime 状态判定
├─ Drop：丢弃
├─ Defer：有限延后，过期丢弃
└─ Approve
   ↓
Send：交给唯一 Delivery Scheduler
   ↓
实际投递结果 / 取消原因
```

Review 是运行时审核步骤，不要求人工审批，也不要求再调用一次大模型。1.0 可采用规则与现有状态判断；允许 `DoNothing`，不因生成了候选而必须发送。

最小候选信息 SHOULD 包含 CandidateId、CharacterInstanceId、ConversationId、触发原因、生成时状态版本、CreatedAt、ExpiresAt、ResponsePlan。Review 检查：

- 用户是否正在输入、已经回来或发起新 Turn；候选上下文是否已过时。
- 是否与现有回复重复、与关系/情绪/意图不合，或没有足够的触发依据。
- 主动消息开关、静默设置、cooldown、发送预算是否允许。
- 候选是否过期，目标是否仍为当前允许的本地会话。

发送前 MUST 再核对取消状态和上下文版本；新输入使相关旧候选失效。异步模型结果不得绕过该检查直接投递。所有主动输出与普通回复共用 Interruption Controller 和 Delivery Scheduler，不能另起一条不受控的发送通道。

Review 失败、超时或信息不足时，保守丢弃或有限延后；重启后不得盲目重放过期候选。仅实际投递成功的消息记为已发送，取消/丢弃候选不得作为角色已经说过的话进入历史或学习。

本节规定实现边界，不新增天气、新闻、生活模拟等触发源。若主动消息整体延后，本阶段只记录接口决策和 backlog，无需实现空壳引擎。若发布启用，则上述审核、取消和过期行为必须通过验证，并提供关闭主动消息的入口。

---

# 18. Character State

Character State 分为：

```text
Identity
Relationship
Mood
```

MUST NOT 用一个超长 Persona Prompt 代替整个状态系统。

---

# 19. Identity

Identity 只保存稳定角色锚点。

SHOULD 包括：

- 名称
- 核心视觉身份
- 基础人格轮廓
- 核心行为倾向
- 不轻易变化的价值/边界

MUST NOT 把完整世界观强绑到基础 Identity。

目标是：

> 固定角色锚点 + 开放世界解释。

---

# 20. Relationship

建议内部状态：

```text
Familiarity
Trust
Closeness
SharedContextStrength
```

这些是内部信号。

MUST NOT 默认向用户暴露：

```text
亲密度 +1
信任值 78
```

---

# 21. Mood

轻量 Mood MAY 包含：

```text
Valence
Arousal
Interest
Fatigue
Irritation
```

Mood 可影响：

- Reply Gate
- 回复长度
- 节奏
- Expression

MUST NOT 发展成复杂心理模拟器。

---

# 22. Behavior System

Behavior 回答：

> 当前应该采用什么互动策略？

它不是语言风格。

例如：

```text
Scenario:
用户在开玩笑挑衅

Behavior:
不认真解释
回怼
保持玩笑状态
```

Behavior 与 Expression 必须分离。

---

# 23. Behavior Planner

输入：

- 当前会话状态
- 用户最近输入
- Relationship
- Mood
- Relevant Memory
- Relevant Behavior Patterns

输出 Response Intent。

可用 Intent 示例：

```text
Acknowledge
ContinueTopic
Ask
Tease
PushBack
Comfort
Refuse
Share
Clarify
DoNothing
UseCapability
EndTopic
```

列表可以扩展。

---

# 24. BehaviorPattern

建议：

```text
BehaviorPattern
{
    BehaviorPatternId
    CharacterInstanceId
    Scenario
    Strategy
    Confidence
    UsageCount
    SuccessCount
    FailureCount
    CreatedAt
    UpdatedAt
}
```

1.0 可简化，但数据边界 SHOULD 保留。

---

# 25. Behavior Learning

不需要强化学习或模型微调。

基本目标：

```text
Situation
→ Pry Behavior
→ User Follow-up / Outcome
→ 更新 Behavior Pattern
```

它保存的是：

> 某类互动场景下，怎样的社交策略更符合这个长期关系。

---

# 26. Expression System

Expression 回答：

> 已经决定做什么之后，Pry 应该怎样说？

例如：

```text
Situation:
被发现说错了

Style:
先短句否认，再转移成吐槽
```

Behavior 决定“做什么”。

Expression 决定“怎么说”。

二者 MUST 分离。

---

# 27. ExpressionPattern

建议：

```text
ExpressionPattern
{
    ExpressionPatternId
    CharacterInstanceId
    Situation
    Style
    Confidence
    UsageCount
    CreatedAt
    UpdatedAt
}
```

以后可增加 `SourceMessageIds`、`Tags`、`Embedding`。结合 Private Companion 的研究启发，以下列入 FUTURE backlog，均不是 1.0 必填字段或交付要求：

| 字段 | 用途 | 实施边界 |
| --- | --- | --- |
| Channel | 限定表达适用的交互渠道或场景 | 使用 Pry 自有标识，不在领域模型塞入 QQ/Satori 类型 |
| RelationshipStage | 限定适用的关系阶段 | 不因此设计复杂关系等级系统 |
| EmotionGate | 判断当下情绪条件是否允许使用 | 复用轻量 Mood，不引入心理模拟 |
| Intent | 指定与哪些 Response Intent 相容 | 引用已有意图，保持 Behavior/Expression 分离 |
| Avoid | 明确不适用的场景或表达约束 | 用于排除误用，不成为另一份无限增长的 Prompt |
| Evidence | 保存学习依据、来源和可信度 | 可引用 SourceMessageIds，不默认复制私人原文 |

未来实现时，Selector 先检查适用性和排除条件，再对候选排序；不把所有条件直接交给模型自行理解。缺省字段保持与旧模式兼容，新增持久字段按实际需求 migration，不一次性搭建通用规则引擎。

1.0 继续使用当前最小 ExpressionPattern、少量检索与 Selector；优先解决口癖滥用、重复和场景不匹配。只有纵向反馈或 Eval 显示现有结构不足，才逐项启用上述扩展。

---

# 28. Expression Retrieval

回复前只检索少量相关 Expression Pattern。

可使用：

```text
Keyword
Embedding
Recency
UsageCount
Confidence
```

MUST NOT 把整个 Expression DB 塞进 Prompt。

---

# 29. Expression Selector

Selector 决定：

> 本次候选习惯哪些真正适用？

初版可：

- 排序
- 小模型调用
- 规则 + 排序

MUST NOT 强迫模型使用所有候选。

---

# 30. Expression Learning

Expression Learning SHOULD 异步运行：

```text
完成一次聊天 Turn
↓
后台分析
↓
抽取 Situation → Preferred Style
↓
去重 / 合并
↓
ExpressionPattern
```

MUST NOT 阻塞用户可见回复。

---

# 31. Memory 总体结构

Pry 的“记忆”必须拆开：

```text
Working Context
Conversation History
Long-term Memory
Relationship State
Behavior Patterns
Expression Patterns
```

不能把它们全部塞进一个 Memory 表然后统一 RAG。

---

# 32. Long-term Memory

建议：

```text
Memory
{
    MemoryId
    CharacterInstanceId
    Type
    Content
    Importance
    Confidence
    SourceMessageIds
    CreatedAt
    UpdatedAt
    Status
}
```

Type 可包含：

```text
Episodic
Semantic
Preference
Relationship
PersonalContext
```

Behavior 和 Expression SHOULD 使用独立存储。

---

# 33. Memory Retrieval

可综合：

```text
Semantic similarity
Keyword
Recency
Importance
Relationship relevance
Conversation relevance
```

MUST NOT 每次加载全部长期记忆。

---

# 34. Async Memory Writeback

长期记忆写入必须与主回复链路解耦。

```text
Turn 完成
↓
回复发送
↓
Background Memory Queue
↓
Candidate Extraction
↓
Evidence Validation
↓
Deduplicate / Merge
↓
Long-term Memory
```

Memory 学习变慢时，聊天本身仍应可正常工作。

---

# 35. Memory Evidence

自动记忆 SHOULD 尽量保留：

```text
SourceMessageIds[]
Confidence
ExtractionReason
```

优先保存：

> 用户自己明确说过的事实。

谨慎保存：

> 根据别人描述推测用户的事实。

---

# 36. Memory Management

用户 MUST 可以：

- 查看 Memory
- 编辑 Memory
- 删除 Memory
- 清空 CharacterInstance Memory

删除后 MUST NOT 因缓存而重新出现。

---

# 37. Context Builder

必须存在统一 Context Builder。

它负责组装：

```text
Identity
Relationship
Mood
Recent Conversation
Relevant Memory
Behavior Intent
Relevant Behavior Patterns
Relevant Expression Patterns
Capability Result
Current Input
```

各模块 MUST NOT 自己无限制往 Prompt 追加文本。

---

## 37.1 Memory / Character Intelligence 正式 Contract

Private Companion 的记忆桥接研究提示：记忆实现、角色智能与运行时之间的依赖，应成为正式接口，而非散落的内部调用或隐式 Hook 顺序。

Pry 使用同一进程内的轻量 Contract/Interface 解耦，不复制外部插件的 Bridge 实现，不因此增加服务部署层。以下名称为建议，应在现有共享 Contract 中统一定义，避免各模块重复发明 Context。

| 接口 / Contract | 输入与输出 | 状态与职责归属 |
| --- | --- | --- |
| IMemoryQuery | MemoryQuery → MemoryContext | Memory 返回有范围、预算和证据的相关记忆；不决定何时回复 |
| IMemoryWriteback | 已完成 Turn 的已投递消息与用户输入 → 入队结果 | Memory 异步抽取、校验证据、去重及持久化；不阻塞回复 |
| ICharacterIntelligence | TurnContext + CharacterStateSnapshot + MemoryContext → BehaviorDecision | 返回意图、表达候选和计划建议；不能直接发送消息 |
| IContextBuilder | 各模块有限快照 → 有预算的 ModelContext | 唯一 Prompt 组装入口；不接收无界文本追加 |
| Runtime / Delivery Contract | Decision / ResponsePlan → 调度、取消与投递结果 | Runtime 拥有 Turn 和输出生命周期，Delivery 负责实际发送 |

契约至少明确：

- `CharacterInstanceId`、`ConversationId`、`TurnId` 的范围与状态版本；跨会话只检索共享长期记忆，不串入另一会话的短期消息。
- DTO/只读快照、取消、超时、最大条数或 token 预算，以及成功、空结果、失败的可区分结果。
- MemoryContext 中记忆 ID、来源证据、可信度与状态；Character Intelligence 不直接查询 Memory 数据库。
- Memory 暂不可用时允许有限上下文继续聊天，但不能把检索失败解释为“用户没有相关历史”，也不能编造记忆。
- 异步写回使用来源/任务标识避免重复提交；删除或更正后的记忆不得被旧缓存、旧任务重新写回。
- Relationship、Behavior、Expression 的更新各归所属服务；Memory 模块不能越权修改整个 Character State。

1.0 验证边界：用简单替身检查空结果、超时、取消、作用域隔离，以及学习写回不阻塞可见回复。接口以当前调用需求为限，不预建复杂插件协商、动态 Bridge 协议或分布式 Contract Registry。

---

# 38. Prompt 原则

MUST NOT 使用“一个巨大的角色 Prompt”承担全部行为。

Prompt SHOULD 模块化：

```text
Stable Identity
+
Current Relationship
+
Current Mood
+
Behavior Intent
+
Relevant Memory
+
Relevant Expression
+
Recent Conversation
+
Current Input
```

每部分 SHOULD 有大小预算。

---

# 39. Model Provider

最少定义：

```text
IChatModel
IEmbeddingModel
```

1.0 MUST：

- 内置轻量 Local Model
- 支持 OpenAI-compatible API

后续 Provider 可扩展，但不是 1.0 重点。

---

# 40. 内置 2B 模型的定位

内置模型只负责：

> 安装后立即可以说话。

它不负责代表 Pry 的最佳效果。

UI SHOULD 明确提示：

> 想获得更好的角色表现，可以配置更强的 API / Model Provider。

用户自接 API：

- 自行承担费用
- 自行承担第三方数据政策
- Pry 不承担用户外部 Provider 成本

---

# 41. Capability，而不是完整 Agent Tool

1.0 只实现简单 Capability。

Capability 表示：

> Pry 能执行的一项明确动作。

例如：

```text
OpenApplication
OpenUrl
PlayMusic
PauseMusic
AdjustVolume
PluginDefinedAction
```

---

# 42. Capability Interface

建议：

```text
ICapability
{
    CapabilityId
    Name
    Description
    ParameterSchema
    ExecuteAsync(...)
}
```

结果：

```text
CapabilityResult
{
    Success
    Output
    Error
    Metadata
}
```

---

# 43. Capability 调用链

```text
User
↓
Character Runtime
↓
Behavior Planner
↓
UseCapability Intent
↓
Capability Dispatcher
↓
Execute
↓
Result
↓
Character Runtime
↓
可选自然语言回应
```

1.0 MUST NOT 实现：

```text
LLM
→ Tool
→ LLM
→ Tool
→ LLM
→ ...
```

的通用循环。

---

# 44. Capability 权限

SHOULD 支持：

```text
Allow
Ask
Deny
```

例如：

```text
打开网易云 → Allow
危险本机操作 → Deny
```

高风险能力不属于 1.0。

---

# 45. Plugin

1.0 SHOULD 有极简 Plugin API：

```text
IPryPlugin
{
    Manifest
    InitializeAsync(...)
    ShutdownAsync(...)
}
```

插件可以注册：

- Capabilities
- Event listeners
- plugin-local storage
- settings metadata

---

# 46. PluginContext

建议只暴露：

```text
ILogger
ICapabilityRegistry
IEventBus
IPluginStorage
IReadOnlyCharacterContext
```

MUST NOT 默认暴露：

```text
Raw Database
整个 IServiceProvider
可修改的内部 Runtime
模型密钥
```

---

# 47. Plugin 非目标

1.0 不做：

- Marketplace
- 插件签名
- 独立 Runner
- RPC Sandbox
- 依赖求解
- 强保证 Hot Reload
- 原生代码隔离

---

# 48. Pry Client ↔ Core

Client 和 Core 必须通过正式协议通信。

推荐：

```text
HTTP
+
WebSocket
```

Client MUST NOT 直接调用 Core 内部对象。

协议从一开始 SHOULD 带版本号。

---

# 49. HTTP 职责

HTTP 负责：

- Conversation CRUD
- Message History
- Memory CRUD
- Character Settings
- Provider Settings
- Capability Settings
- Plugin Settings
- Backup / Export
- Diagnostics Snapshot

---

# 50. WebSocket 职责

WebSocket 负责：

- New Message
- Delivery State
- Typing
- Runtime State
- Response Plan State
- Capability State
- Interruption
- Proactive Message
- Model Status

---

# 51. Client 职责

Pry.Client 负责：

- UI
- Conversation List
- Input
- Typing Event
- Notification UI
- Settings
- Memory Management UI
- Developer Mode UI

Client 不负责：

- Memory 真相源
- Behavior Planning
- Expression Learning
- Model Routing
- Capability Business Logic
- 直接写数据库

---

# 52. Core 职责

Pry.Core 负责：

- Character Runtime
- Conversation Runtime
- Behavior
- Expression
- Memory
- Relationship
- Mood
- Context Builder
- Model Provider
- Capability
- Plugin Host
- Persistence
- Backup / Export
- Pry Protocol Server

---

# 53. Persistence

默认使用 SQLite。

业务模块通过 Repository 使用数据库。

建议：

```text
IConversationRepository
IMessageRepository
IMemoryRepository
IBehaviorPatternRepository
IExpressionPatternRepository
ICharacterStateRepository
```

MUST NOT 在 Runtime 到处散落原始 SQL。

---

# 54. Local-first

如果用户使用内置本地模型，Pry 1.0 MUST 在以下条件下工作：

```text
无账号
无官方服务器
无网络
```

首次使用不得强制注册。

---

# 55. Crash Recovery

原则：

> Process state 可丢，Character state 不可丢。

以下情况后应能恢复：

- Client 崩溃
- Core 崩溃
- Windows 重启
- 强制关闭

临时 pending event 可以丢。

Conversation、Memory、Relationship 等持久状态不能丢。

---

# 56. Developer Mode

SHOULD 显示：

```text
Runtime State
Pending Input
Turn Scheduler Decision
Reply Gate Decision
Behavior Intent
Retrieved Memories
Behavior Patterns
Expression Patterns
Model Request
Model Response
Response Plan
Capability Request
Capability Result
Latency
Token Usage
```

普通用户界面隐藏这些内容。

---

Developer Mode 对已实现的主动消息 SHOULD 展示触发原因、候选状态、Review 结果、丢弃/取消原因与投递结果。默认日志不记录完整私人对话；研究和内测材料不自动上传聊天原文。

---

# 57. Internal Event Bus

SHOULD 有轻量事件系统，例如：

```text
UserMessageReceived
UserTypingStarted
UserTypingStopped
TurnStarted
TurnCancelled
ReplyPlanned
ReplyDelivered
CapabilityStarted
CapabilityCompleted
MemoryCandidateCreated
MemoryWritten
ExpressionLearned
BehaviorPatternUpdated
```

避免模块之间形成循环依赖。

---

# 58. Conversation Eval

必须建立固定测试集，避免再次陷入“凭感觉调 Prompt”。

Eval 分为：

```text
Timing Eval
Behavior Eval
Expression Eval
Memory Eval
Relationship Eval
Capability Eval
```

---

## 58.1 两条互补的验证路径

固定 Conversation Eval 用于发现可复现回归；真实用户纵向内测用于观察持续相处是否自然。二者不能互相替代：通过测试不等于有人愿意长期使用，少数用户喜欢也不能替代数据完整性与取消行为测试。

1.0 在已有测试集中增加少量相关用例，不引入大型评估平台：

| 维度 | 补充场景 | 检查重点 |
| --- | --- | --- |
| Timing / Proactive（启用时） | 候选生成后用户回来、开始输入、插话；Review 后状态变化；候选过期 | 过时内容不发送；取消后迟到模型结果不复活；审核拒绝不投递 |
| Expression | 同一口癖反复召回；用户难过时检索到玩笑表达 | 候选可不用，避免机械套用；未来字段启用时再测对应条件 |
| Memory Contract | 检索超时、错误、空结果；删除后旧写回完成 | 结果可区分；聊天可降级；不编造或复活已删记忆 |
| Relationship / Continuity | 重启、切会话、换 Provider、备份恢复，再谈已知事实和关系 | 状态持续且作用域正确；记录语言风格变化，不要求不同模型逐字一致 |

Eval 记录软件版本、模型/Provider、相关设置及失败示例，区分模型质量与 Runtime 回归。仅对实际启用能力设通过门槛；FUTURE 字段和未启用触发源不产生 1.0 测试义务。

## 58.2 个人开发者规模的早期验证

验证目标：少数本来愿意使用 Pry 的人，能否顺利开始，并愿意持续与同一个角色相处。无需统计学意义的市场验证，也不把大样本问卷作为前置条件。

```text
开发者自己长期 dogfood
↓
2～3 个明确愿意使用 Pry 的核心早期用户
↓
有余力时，可选扩展到总计 3～8 个真实测试者
↓
同一批人跨版本持续使用与反馈
```

这些人数是可行的招募节奏，不是必须凑齐的发布指标；开发者 dogfood 单独记录，扩展人数指外部测试者总数。不要求测试者代表大众市场，也不把愿意参与视为样本缺陷。其反馈用于修复早期产品问题，不能外推为市场规模结论。

首次让测试者尝试自行安装和开始聊天；遇到困难可以帮助，但记录哪一步需要协助，不以“没人能独立安装”为由继续只演示产品。

持续观察：

- 安装、首次启动、内置模型可用与可选 API 配置是否顺利，卡在哪里。
- 第二天是否主动再次打开；一周后是否继续使用，为什么留下或停用。
- 是否形成固定场景，例如睡前聊天、学习间隙或开机后打招呼。
- 哪次记忆、表达、关系或打断处理让人觉得“这是同一个角色”。
- 哪次遗忘、误召回、客服腔、口癖重复或不合时宜的主动消息破坏了这种感觉。

不要求每天签到，不以消息数、在线时长或亲密度作为成功本身。几条具体经历、继续/弃用原因和一个可复现问题，比汇总满意度分数更适合当前阶段。

## 58.3 纵向内测记录与反馈闭环

从软件可稳定使用时开始，随 Runtime、Expression、Memory 等现有阶段逐步观察；无需等全部功能做完，也不要求每阶段重新招募。建议在次日、一周左右以及关键版本后做简短交流，时间可按测试者意愿调整，持续数周作为方向而非统一硬期限。

可在 `docs/eval/longitudinal/` 保留匿名、经同意且脱敏的摘要：

```text
TesterAlias / Build / ObservationPeriod
ModelOrProvider / RelevantSettings
InstallFriction / HelpNeeded
ReturnedNextDay / StillUsingAfterWeek / Unknown
RecurringUseCase
ContinuityMoment / ContinuityBreak
ContinueOrStopReason
Issue / Decision / NextVersionFollowUp
```

未回复记“未知”，不自动算弃用。测试者可以停止参与，无需提交完整私人聊天；复现场景尽量用合成或主动提供的脱敏内容。

每次关键改动后比较同一个人的前后体验，并注明模型与设置是否同时变化。反馈先归因到安装、模型、节奏、记忆、表达或关系，再落为小修复或已有 Eval 用例，下一版本回访。新功能诉求先进入 backlog，不能自动提升为 1.0 MUST。

---

# 59. Timing Eval

场景：

- 用户快速连发三句
- 用户说一半停顿
- 用户在 Pry 多条消息之间插话
- 用户半小时后回来
- Pry 有 pending output

检查：

- 是否回复太早
- 是否继续发送过时消息
- Interrupt 是否真正取消
- Delay 是否过度机械

---

# 60. Behavior Eval

场景：

- 用户玩笑挑衅
- 用户直接提问
- 用户发了一句不需要回应的话
- 用户情绪差但没求安慰
- 用户说法与过去矛盾

检查：

- 是否过度解释
- 是否永远追问
- 是否变成客服
- 是否能输出 DoNothing
- 是否选择合适互动策略

---

# 61. Expression Eval

例如：

```text
User:
草
```

检查：

- 是否一定要回复
- 是否回复过长
- 是否解释“草”
- 是否机械问“怎么了”
- 是否符合已有 Expression Pattern
- 是否出现固定 AI 套话

---

# 62. Memory Eval

测试：

- 用户建立偏好
- 用户后来改变偏好
- 新 Conversation 引用该信息
- 用户手动删除记忆

检查：

- 是否正确召回
- 是否仍使用旧事实
- 是否跨会话共享正确
- 是否把另一个 Conversation 的短期上下文直接带过来
- 删除后是否复活

---

# 63. Capability Eval

例如：

```text
帮我打开网易云
```

检查：

- 是否识别为 Action Intent
- 是否调用正确 Capability
- 是否避免进入无限 Tool Loop
- CapabilityResult 是否正常返回
- 权限策略是否生效

---

# 64. Phase 1 — Local Skeleton

实现：

```text
Pry.Client
Pry.Core
Pry Protocol
SQLite
Config
Logging
Basic Model Provider
Single Conversation
Message Persistence
```

验收：

- Client/Core 启动
- Client 能连接 Core
- 能聊天
- 能保存历史
- 重启历史仍在
- 无官方服务器依赖

---

Phase 1 补充：建立研究目录与矩阵的最小记录，明确哪些内容待核验；约定当前用到的共享 Contract 和状态归属。开发者开始日常 dogfood，先记录安装、启动和首轮对话问题。无需等全部竞品研究完成才进入 Runtime 开发。

---

# 65. Phase 2 — Persistent Conversation Runtime

实现：

```text
ConversationRuntime
PendingInputQueue
InputBuffer
TurnScheduler
ReplyGate
InterruptionController
DeliveryScheduler
ResponsePlan
Multi-message Output
```

验收：

- 连发消息可合并
- Reply Gate 可不回复
- 插话后旧 pending output 被取消
- 多条回复有独立 delivery
- 切换 Conversation 后 Runtime 正常

这是 Pry 1.0 最重要的阶段。

---

Phase 2 补充：记录主动候选如何复用现有取消、过期与投递机制。若本阶段启用主动消息，验证 Generate → Review → Send 和“审核后用户插话”的竞争场景；若延后，明确记录，不新增独立 Proactive Phase。

---

# 66. Phase 3 — Character State & Behavior

实现：

```text
Identity
Relationship
Mood
Behavior Planner
Response Intent
Basic BehaviorPattern Retrieval
```

验收：

- Behavior 与 wording 分离
- Relationship 可持久化
- Developer Mode 可看到行为决策
- 能输出 DoNothing
- 能输出 UseCapability 占位 Intent

---

Phase 3 补充：以正式 Contract 接入 MemoryContext 与 CharacterStateSnapshot；Intelligence 只产生决策，不能绕过 Runtime 发送消息。达到可日常使用状态后，可邀请 2～3 个核心用户开始纵向内测。

---

# 67. Phase 4 — Expression

实现：

```text
ExpressionPattern
Expression Retrieval
Expression Selector
Async Expression Learning
```

验收：

- Expression 独立存储
- 可按场景召回
- 不强制全部候选
- 学习不阻塞回复
- 长期使用开始出现稳定表达习惯

---

Phase 4 补充：将 Channel / RelationshipStage / EmotionGate / Intent / Avoid / Evidence 记入 FUTURE backlog，不要求本阶段实现。用现有 Selector 和内测实例检查口癖重复与场景误用。

---

# 68. Phase 5 — Long-term Memory

实现：

```text
Memory Store
Memory Retrieval
Embedding
Async Memory Writeback
Evidence
Dedup / Merge
Memory UI
Cross-conversation Retrieval
```

验收：

- A 会话产生长期记忆
- B 会话可检索
- B 不继承 A 的短期消息
- 用户能删除
- Writeback 异步

---

Phase 5 补充：验证 Memory Contract 的作用域、空结果/失败区分、异步写回和删除后不复活；在同一批测试者中观察跨会话记忆是否增强或破坏角色连续性。

---

# 69. Phase 6 — Behavior Learning

实现：

```text
BehaviorPattern Store
Scenario Retrieval
Outcome Capture
Pattern Update
```

验收：

- 可记录结构化互动经验
- 可影响后续 Planner
- Developer Mode 可检查
- 不依赖模型微调

---

# 70. Phase 7 — Capability & Plugin

实现：

```text
ICapability
CapabilityRegistry
CapabilityDispatcher
IPryPlugin
PluginContext
PluginStorage
```

提供少量示例：

```text
OpenApplication
OpenUrl
```

可选加入：

```text
NeteaseMusic Plugin
```

验收：

- Planner 可请求 Capability
- 权限生效
- Plugin 可注册 Capability
- Plugin 不直接依赖数据库实现

---

# 71. Phase 8 — Release Hardening

实现：

```text
Conversation Eval
Developer Diagnostics
Backup
Restore
Export
Crash Recovery
Installer
Migration
Performance Check
```

验收：

- 干净安装可运行
- 更新不损坏数据
- Backup/Restore 可验证
- Eval 可重复执行
- Release build 可复现

---

## 71.1 发布阶段的纵向复盘

Phase 8 汇总开发者 dogfood、核心用户持续使用记录和固定 Eval。重点处理阻碍安装、造成数据丢失、频繁打断用户或显著破坏角色连续性的已知问题。

发布复盘至少说明：观察了哪些版本、实际参与情况、安装阻碍、次日/一周使用情况（允许未知）、固定场景、连续性正反例、继续/弃用原因、已修复问题与保留限制。用匿名个案叙述即可，不设置留存率或统计显著性门槛，不要求大样本问卷。

外部反馈暂缺时应如实记录，不能宣称产品假设已验证；也不因招募未达某个人数阻塞所有工程推进。是否发布由核心质量、已知风险与可用反馈共同决定。

---

# 72. Codex 实施规则

Codex MUST：

1. 先阅读本计划。
2. 一次只实施当前 Phase。
3. 保持 Client/Core 分离。
4. 保持 Behavior/Expression 分离。
5. Memory Writeback 必须异步。
6. Context Builder 必须集中管理。
7. Plugin 只能通过公开接口访问 Core。
8. 通过 Repository 访问持久化。
9. 为 Runtime 核心行为写测试。
10. Schema 变化必须有 migration。
11. 每阶段结束运行 Build/Test。
12. 架构冲突先报告，不偷改边界。
13. 不用临时 Hack 绕过模块边界。
14. 不把复杂行为重新塞回一个 System Prompt。
15. 不给 1.0 偷加网络依赖。
16. 当前 Phase 涉及参考研究时，保存证据与采纳/延后决策；不把对话总结当作最新源码证明。
17. Memory / Character Intelligence 通过正式 Contract 交互，Runtime 保有调度与发送权。
18. 已启用的主动消息必须经过 Generate → Review → Send，复用取消与投递机制。
19. FUTURE 表达字段只记 backlog，未获范围调整不得全部提前实现。
20. 结合固定 Eval 与少数用户纵向反馈修复现有体验，不以竞品功能数量驱动开发。

Codex MUST NOT：

- 提前实现 Satori Runtime
- 提前实现 Multi-Core Sync
- 因为“以后可能用”就加入 MCP
- 做通用 Tool Loop
- 做 SubAgent
- 强制 Cloud Account
- 让领域模型直接依赖第三方协议类型
- 把 Plugin 做成 Marketplace
- 为抽象美感重写已经工作的 Phase
- 复制新闻、天气、衣柜、QQ 空间等功能集合进入 1.0
- 把外部测试者人数、问卷规模或统计学市场验证变成开发前置门槛

---

# 73. 每阶段结束报告

必须输出：

```text
Implemented
Architecture
Data Changes
Protocol Changes
Tests
Validation
Known Issues
Research Decisions / No Change
Dogfood or Longitudinal Feedback / Not Yet Available
Scope Impact
Deferred Work
Next Phase Preconditions
```

然后停止。

未经要求不得自动继续下一 Phase。

---

# 74. Definition of Done

一个阶段只有满足以下条件才算完成：

```text
Build 通过
+
核心测试通过
+
手动运行验证通过
+
Migration 明确
+
模块边界未破坏
+
Developer Mode 能解释核心决策
+
无已知严重数据丢失 Bug
```

“代码写完”不算完成。

---

## 74.1 本次修订的完成边界

- 涉及研究的决策可追溯到记录；证据不足处明确标记，不要求研究矩阵全部填满。
- 本阶段使用的 Memory / Intelligence Contract 已明确；相关隔离、降级与取消行为有验证。
- 主动消息若启用，其 Review、发送前状态检查、取消及过期用例通过；未启用则记录延后，不做虚假验收。
- 能获得的 dogfood/纵向反馈已归档，严重问题有处置；没有反馈则标明观察不足。
- FUTURE 条目未悄然变为发布依赖，八个 Phase 不因本次修订增加大型子系统。

阶段完成不要求当阶段就完成一周观察；持续内测跨阶段进行，发布时汇总。工程完成与产品假设得到多少支持分别报告。

---

# 75. Pry 1.0 发布标准

用户必须能够：

1. 在 Windows 安装 Pry。
2. 使用内置模型直接聊天。
3. 可选配置更强 OpenAI-compatible API。
4. 创建多个 Conversation。
5. 每个 Conversation 有独立短期上下文。
6. 所有 Conversation 共享同一 CharacterInstance 的长期记忆。
7. 自然插话并打断旧回复。
8. 看到多消息聊天节奏。
9. 长期形成可识别 Expression 习惯。
10. 维持 Relationship 连续性。
11. 使用少量简单本地 Capability。
12. 管理长期 Memory。
13. 备份并恢复角色状态。
14. 完全不依赖官方服务器。

---

## 75.1 发布评估补充

在上述技术与体验标准外，MUST 完成一次诚实的纵向复盘：开发者长期使用情况、实际可获得的早期用户反馈、哪些行为维持/破坏同一角色感，以及仍未验证的假设。2～3 个核心早期用户是建议起点，扩展到总计 3～8 个测试者完全可选；两者均不是凑人数式发布门槛。

发布文案区分本地模型的开箱可用性与不同 Provider 的角色表现，不能用更强模型的演示代替默认体验验收。优先处理首次使用、消息节奏、记忆正确性和持续使用中的真实阻碍，功能数量不作为发布成绩。

---

# Appendix A — Future External Messaging

以下均为 FUTURE。

未来外部通信必须经过：

```text
IExternalMessenger
IExternalEndpoint
IExternalIdentityResolver
```

Character Runtime 不认识：

```text
Satori
QQ
Telegram
Discord
```

正确边界：

```text
External Platform
↓
Protocol Adapter
↓
Pry-owned ExternalEvent
↓
Character Runtime
```

---

# Appendix B — Future Satori

Satori 是未来外部 IM 接入的首选方向，但它只能存在于边界层。

```text
Pry.Core
    │
IExternalMessenger
    │
Pry.SatoriAdapter
    │
Satori
    │
Gateway
    │
QQ / Telegram / etc.
```

Pry Domain MUST NOT 直接依赖：

```text
Satori Session
Satori Login
Satori Element
```

应该映射为 Pry 自己拥有的类型。

未来值得借鉴的 Satori 设计：

- Capability Discovery
- Rich Message Element Tree
- Remote Bot Proxy
- 标准 API + Platform-specific Escape Hatch
- HTTP Command + WebSocket Event 分离

---

# Appendix C — Future ExternalEndpoint

未来可定义：

```text
ExternalEndpoint
{
    EndpointId
    Platform
    AccountId
    Features[]
    Status
}
```

业务判断使用：

```text
endpoint.Supports("reaction.create")
```

而不是大量：

```text
if platform == ...
```

---

# Appendix D — Future Hosted Core

Hosted Core 不属于 1.0。

未来可增加：

```text
Account
Authentication
Multi-user Isolation
Quota
Remote Access
Optional Sync
```

Hosted 与 Local SHOULD 尽量共享同一 Character Runtime。

---

# Appendix E — Future Multi-Core

未来允许：

```text
Local-Core A
↕
Local-Core B
↕
Hosted-Core
```

Sync 必须 Opt-in。

不强制唯一 Authoritative Core。

1.0 不实现同步。

---

# Appendix F — Future Sync

真正开发同步时再评估：

```text
Event Log
Version Vector
Hybrid Logical Clock
CRDT
Conflict Record
```

MUST NOT 在 1.0 提前实现分布式 Event Sourcing。

也不得简单使用：

```text
latest timestamp wins
```

作为长期最终方案。

---

# Appendix G — Future Identity Federation

未来可能：

```text
PryUserId
├─ Local Identity
├─ QQ Identity
├─ Telegram Identity
└─ Web Identity
```

绑定必须显式验证。

MUST NOT 按昵称自动合并身份。

---

# Appendix H — Future Agent Harness

完整 Harness 不属于 Pry 1.0。

但 Capability / Plugin 结构应该允许未来第三方自行开发：

```text
AgentHarnessPlugin
├─ MCP
├─ Tool Loop
├─ Browser
├─ Shell
├─ Skills
└─ SubAgents
```

对 Pry.Core 来说，它仍只是可选扩展。

Character Runtime 不依赖它。

---

# Appendix I — Future Plugin Isolation

如果未来插件生态真的变大，再考虑：

```text
Core Process
↕ IPC/RPC
Plugin Runner
↕
Plugin
```

只有出现真实问题时再做：

- Plugin Crash
- 依赖冲突
- 不可信插件
- Hot Reload
- 权限隔离

---

# Appendix J — Future Network Layer

1.0 不选择网络拓扑。

未来可研究：

```text
HTTP
WebSocket
gRPC
QUIC
Relay
Reverse Tunnel
P2P
Overlay Network
```

Character Runtime 必须独立于网络实现。

---

# Appendix K — 许可边界

代码与角色资产必须分离。

建议仓库结构显式分开：

```text
src/
plugins/
assets/
character/
```

直接复用第三方代码必须遵守上游许可证。

不能认为“换语言重写”自动解除许可证义务。

原创角色、美术、Logo、角色设定使用独立资产/IP许可。

---

# Appendix L — 最终判断标准

添加任何大型功能前先问：

> **它是否增强了“普莱作为一个长期存在角色”的可信度？**

如果主要收益只是：

- Tool 更多
- Plugin 更多
- Agent 名词更多
- 网络拓扑更复杂
- Framework 更漂亮

但没有明显改善：

- 连续性
- 聊天节奏
- 人格稳定性
- 表达自然度
- 长期记忆
- 关系连续性

则延后。

---

# Final Architecture Statement

Pry 1.0 不是通用 AI 平台。

它是：

> **一个拥有持续对话状态、行为规划、表达学习、长期记忆和轻量扩展能力的 Local-first Character Runtime。**

未来的 Satori、Hosted Core、Multi-Core Sync、完整 Agent Harness 都只能建立在这个核心之外。

最重要的开发目标始终是：

> **让用户觉得“普莱一直存在”，而不是每次发消息都临时启动了一次聊天机器人。**
