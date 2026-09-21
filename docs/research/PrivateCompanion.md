# Private Companion：AstrBot-native 长期陪伴与生活模拟层

> **⚖️ 照搬边界（作者 2026-09-21 裁决，`DOC-005`）：本项目永久仅作机制与市场调研参考，源码文本不得进入 Pry 仓库或任何 worktree。**
> 原因：该仓库**根目录没有 LICENSE 文件**（GitHub license 接口无 license 字段、根目录 tree 确认无许可文件），未授予任何许可 = 默认保留所有权利；它运行在 AGPL 的 AstrBot 上也不会让它自己变成 AGPL。要复用只有两条路：向 `menglimi` 取得书面许可，或永久停留在行为层面的独立设计——作者选了后者。
> 因此本文件的用法限于：读 §4 的 Generate→Review→Send 终审、§5 的表达适用条件、§6 的版本化 Memory Bridge 契约**思想** → Pry 独立设计；不 clone、不 vendor、不逐行对照源码；角色设定、文案、素材一律不取用。要改变本条只能由作者裁决。细则见 `docs/upstream-license-compatibility.md` §4.4。

来源：R6（2026-09-17），见 [README](README.md)。项目为 `menglimi/astrbot_plugin_private_companion`。本文件只整理原源码报告，不复核最新仓库。许可实测日期 2026-09-21，HEAD `2313fd12e1bd9b72f6db9aca17cde4359102341e`。

## 1. 定位：长期陪伴层，但不是 MaiCore Runtime

Private Companion 在 AstrBot 的 Platform、Provider、Conversation、Agent、Tool、Plugin Runtime 和 LLM Pipeline 上建立 Character / Companion Application Layer。它自己管理陪伴状态、策略、主动行为与表达学习，但以 AstrBot 请求生命周期为宿主。

因此，产品能力上有 MaiBot-like 社交和表达机制，架构上却不是把 MaiCore 嵌入 AstrBot。核心差异为：

```text
MaiCore：持久 Social Runtime 决定何时调用 LLM
Private Companion：AstrBot Pipeline 周围挂 Companion Hooks / State / Policies
```

原研究报告确认了 Expression Learning，纠正了先前“只有关系值和固定表达”的低估。

## 2. 历史源码观察：组合与生命周期

`PrivateCompanionPlugin` 由 AstrBot `Star` 和四十多个 Mixin 组合。原报告列出 BusyReplyGate、Chronotype、MemoryCompanionAdapter、UserMemory、Proactive / ProactiveEngine、SceneContext、DailyState、AgendaRuntime、InteractionUtils、Wardrobe、GroupWakeup / GroupObservation、ReadingArchive、NewsExploration、SelfTimeline、AtRelay 等。

插件深度使用 `on_waiting_llm_request`、`on_llm_request`、`on_llm_response`，以高/低 priority 控制次序；历史例值包括 110000、109000、100000、-99950、-100000。它引用 AstrBot 内部的 ProviderRequest、MainAgentBuildConfig、build_main_agent、AssistantMessageSegment、MessageSession、PlatformStatus、star_handlers_registry、LLMResponse。

这些耦合点是工程事实与兼容风险线索，不是应复制到 Pry 的模块边界。

## 3. 私聊主调用链与防抖

原研究跟踪了 `handle_private_message()`：

```text
Incoming Private Message
→ 过滤 Bot 自己回流
→ 查找/建立 Profile、绑定 Unified Person、确认目标陪伴用户
→ 记录 Activity；用户回来时取消 pending follow-up
→ 检查其他插件是否已经回复
→ 图片请求 / 转发 / 引用 / 非文本适配
→ message debounce / pending message
→ AstrBot LLM 生命周期
   waiting 阶段：guard_pending_message_debounce
   响应后：settle_pending_message_debounce
```

报告还定位到 `test_message_debounce_pending.py`。这证明连续输入是其明确处理的问题，不等于本次执行了该测试或验证了所有并发交错。

其防抖是在 AstrBot 即将请求 LLM 时进行控制，仍不同于 Pry 由 ConversationRuntime / TurnScheduler 自己形成 Turn 的方向。

## 4. 主动行为：Candidate → Generate → Review → Send

有 Proactive Candidate Pool，候选含 accepted、deferred、queued、pending、sent 等状态，以及 per-user pending limit、repeat_count / repeat cap。原研究列出 MOOD_CHECKIN、MEMORY_ECHO、ABSENCE_MISS 等低频行为；8h / 8h / 36h 是当时观察到的最小静默参数，不是 Pry 默认值。

```text
状态变化 → Proactive Candidate → Candidate Pool
→ Eligibility / Cooldown / User State → 选择候选
→ Generate → _review_proactive_message_send_decision()
→ Send / Defer / Drop
```

原报告确认测试内容覆盖 send/defer/drop、persona consistency、fact grounding、history context、external share naturalness。这里只保留“存在这些测试覆盖内容”的研究观察，不宣称本次或原研究运行通过。

Pry 可吸收发送前终审：生成完成不应自动意味着应该打扰用户；用户返回和状态变化可使候选失效。

## 5. Character、Relationship 与 Expression Learning

### 生活模拟

DailyState、AgendaRuntime、CalendarObserver、Chronotype、Dreaming、Wardrobe、PlaceCognitiveMap、SceneContext、SelfTimeline、ReadingArchive、NewsExploration、Body Monitor integration 等让生活状态进入上下文。它在“角色好像有自己的日常生活”上投入很重，不只是普通定时聊天。

原报告还看到 `desktop_pet_bridge.py`，因此不能声称它完全没有桌宠方向；但文件存在不证明已具有完整桌面产品体验。

### 表达学习

配置包括 `expression_private_learning_source_mode`、`expression_group_learning_source_mode`、`expression_learning_runtime`、`expression_voice_profile`。私聊按允许的来源更新表达 Profile，并持久化 voice profile。

群聊 Episode 归档 Prompt 抽取 `style_expressions` 和 `grammar_expressions`：前者偏具体场景短表达/口癖/模板，后者偏稳定句法结构。报告记录的结构字段有：

```text
situation / family_key / merge_into_id / style / instruction / tags
evidence_examples / channels / relationship_stages / emotion_gates
intent / avoid / persona_conflict / evidence_count
```

这说明表达不只是“场景 → 风格”，还带来源、证据和使用条件，避免学到口癖后在严肃/低落等不适合场景乱用。对比只限原报告观察，不据此宣称 MaiCore 在所有版本都没有任何使用约束。

`companion_interaction_expression.py` 中 `build_expression_decision()` 从受限结构化输入构造 deterministic expression decision。原研究列出的测试关注 affinity、relationship stage、emotion modulation、curiosity、sleep profile、proactive relationship target、expression dimensions。

关系、情绪和互动意图共同影响表达，不能简化为“亲密度数值追加到 Prompt”。

## 6. Memory Bridge 与模块化压力

插件内部仍有 UserMemory 等能力，同时通过 `MemoryCompanionAdapterMixin` 连接独立 `astrbot_plugin_memory_companion`。

Bridge 包含 contract fingerprint、contract revision、capability schema version、canonical schema version、payload schema version。Emotion 写入需要验证 Producer Context：bot_id、platform、scope=private、user_id、session_id。

这是正式版本化跨模块契约的参考，不能当作已完成对 Memory Companion 仓库全部检索、存储和算法的研究。本包没有把第六个项目的未知实现补进来。

原研究还看到 migration_backfill、migration_dual_write、migration_replay、migration_read_router、migration_stability、scoped_projection，以及 ARCHITECTURE_RESET / WORKING_PRINCIPLES 等线索。它说明超级插件已面临迁移和边界治理压力。

原报告对总体代码规模有夸张量级描述，但没有可复核统计，本包不把行数作为结论。保留具体的 Mixin、Hook、Bridge、迁移模块证据即可。

## 7. 对 Pry 的建议：借鉴与不照搬

**1.0 优先吸收**：私聊连续输入与回流处理；用户返回取消过期 follow-up；主动消息 Generate → Review → Send；Expression 的 Evidence / Avoid / 适用条件意识；Memory 与 Intelligence 的显式接口。

这些是设计原则。完整 Expression gate 字段、候选状态机和复杂主动生活行为应按实际阶段裁剪，不因资料完整就一次全部实现。

**不要照搬**：四十多个 Mixin 的组合方式；AstrBot 内部类型和极端 priority 作为 Pry 调度机制；跨模块直接改数据库；新闻、衣橱、梦境、身体状态等全套生活模拟；为匹配竞品功能数量扩大 1.0。

**未来参考**：更细表达适用条件、生活状态扩展、版本化 Memory Bridge、迁移兼容与投影。前提是现有领域边界和基本角色体验已稳定。

Pry 的差异方向是独立本地桌面产品，以 CharacterInstance / ConversationRuntime / Behavior / Expression / Memory 为根；外部通信作为扩展。围绕真实电脑使用状态形成存在感，是原报告提出的产品建议，不是已经由竞品源码证明的产品优越性。

## 8. Caveat 与验证入口

- 无 commit SHA；深度 Hook 的兼容性须针对实际 AstrBot 版本验证。
- 有表达学习、生活状态和关系逻辑不等于已证明长期体验效果或人格一致性。
- Memory Bridge 的存在不证明外部 Memory Companion 全部实现已审计。
- `enable_mai_style_integration` 名称不能证明集成了 MaiCore 源码。
- Pry 后续测试可覆盖快速连续输入、其他插件已回复、用户返回、候选过期、send/defer/drop、表达不适用场景、Bridge 版本不匹配及迁移重复写入。这些是验证建议，不是本次执行结果。
