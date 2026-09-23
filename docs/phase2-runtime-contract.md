# Phase 2 运行时一致性契约（Pry ConversationRuntime ↔ MaiCore）

日期：2026-09-23　负责人：Character Runtime Owner（RT-002）
上游基线：`Mai-with-u/MaiBot@fb4a8bd917d6fe373df9e3c22289d229e7f1b344`（下称"上游"，路径均相对该 commit）

## 0. 本文是什么

Phase 2 的核心问题是"一个长期存在的角色此刻处于什么状态、什么时候该说话、怎么把一次回复作为真实聊天行为交付出去"。上游是同一问题上唯一被许可照搬的实现（`docs/upstream-license-compatibility.md` §4.4），所以本文逐机制记录四件事：

1. **上游事实**：带 `path:line`，来自锁 commit 的真实源码，不是转述研究文档；
2. **Pry 现状**：带 `path:line` 与契约符号；
3. **判定**：`Adopt`（同形状）/ `Adapt`（同目的、换形状）/ `Reject`（明确不搬）/ `Pending`（应搬未搬，附归属）；
4. **理由**：只写会导致今天或将来改变判定的理由，不写"更清晰""更优雅"这类无法复核的形容词。

本文同时是 RT-002 的**偏差登记表**（§6）、**逐值参数对照**（§5）、**跨 lane 归属条款**（§7.1）、**完成标记协议草案**（§8.1、§8.2）与**给 EVAL-001 的场景草案**（§9）。它不是完成声明：Phase 2 的出口判定权在作者与 QA（§8）。

三份文档分工：`docs/character-runtime.md` 是行为说明书（做了什么、怎么测）；本文是**与上游的对照契约**（为什么是这个形状、哪里故意不同）；`docs/research/MaiBot_MaiCore.md` 是进入实施前的研究记录，**其结论以本文为准**（研究文档写作时 Pry 尚无实现）。

## 1. 取证方法与可复核性

上游源码经 `https://cdn.jsdelivr.net/gh/Mai-with-u/MaiBot@<sha>/<path>` 逐文件取到本地临时目录 `.local-notes/maibot-fb4a8bd9…/`，该目录已 gitignore、不入仓（AGENTS.md 规则 6）。本轮实际读取的文件：

| 文件 | 行数 | 用于 |
|---|---|---|
| `src/maisaka/runtime.py` | 2142 | 消息注册、间隔统计、阈值折算、强制触发、wait、打断、驱逐前状态 |
| `src/maisaka/turn_scheduler.py` | 132 | 轮次入口闸门次序 |
| `src/maisaka/turn_gates.py` | 196 | 必要性门、频率阈值门、空窗补偿 |
| `src/maisaka/reply_necessity.py` | 277 | 四层评分、词表、曲线 |
| `src/maisaka/idle_backoff.py` | 95 | 连续空转退避 |
| `src/maisaka/mode_policy.py` | 28 | 触发模式与空闲轮次判定 |
| `src/maisaka/reasoning_engine.py` | 2149 | 内部循环消费者、触发合并、静默轮、wait 续接 |
| `src/maisaka/builtin_tool/wait.py` | 72 | wait 工具契约 |
| `src/chat/heart_flow/heartflow_manager.py` | 114 | 会话实例管理、LRU/容量/保留窗、频率调节入口 |
| `src/maisaka/focus/manager.py` | 442 | 焦点仲裁（§3.14 的上游事实来源） |
| `src/config/official_configs.py` | 6048 | 上游**发行默认值**（§5 右列的唯一来源） |

留痕三处按 §4.2 齐备：文件头来源行、`licenses/upstream/MaiBot_fb4a8bd9…/LICENSE`、`licenses/THIRD-PARTY-NOTICES.md` 照搬来源表（4 条 `ported` + `TurnScheduler.cs` 记 `mechanism`）。

**本轮自我更正的两处**（原话留在 `docs/character-runtime.md` Known Issues，不静默覆盖）：

- 四个移植组件的参数一度被描述为"Pry 自定、未取用上游群聊调参"。逐值复核后不成立，登记为 `DEF-P2TRIGGER-01`，逐值对照见 §5。
- 上一版本文所属轮次记"上游队列消费者所在文件未定位到"。现已定位：`src/maisaka/reasoning_engine.py:1006-1010`（`run_loop` 阻塞在 `_internal_turn_queue.get()`）。§3.9、§3.10 的成本结论据此重写。

## 2. 概念与状态映射

| 上游 | Pry | 说明 |
|---|---|---|
| `MaisakaHeartFlowChatting`（会话级 runtime） | `Pry.Core.Runtime.ConversationRuntime` | 一个会话一个实例 |
| `HeartflowManager` | `Pry.Api` 侧的 runtime 宿主字典 | 见 §3.1，容量策略故意不同 |
| `_agent_state ∈ {running, wait, stop}`（`runtime.py:142-144`） | `RuntimePhase {Idle, CollectingInput, WaitingForTurn, Planning, Delivering, Suspended}`（`RuntimeModels.cs:9-17`） | 不是 1:1。上游三态描述"planner 在不在跑"；Pry 六态描述"输入/规划/投递/挂起"四段可分别被诊断和恢复 |
| `message_cache` + `_last_processed_index`（`runtime.py:1136-1139`） | `InputBuffer`（未成轮）+ 平台层落库消息（已成轮） | Pry 的缓冲只放"尚未成为 Turn 的输入"，一旦成轮即由宿主落库 |
| `_internal_turn_queue`（`runtime.py:165`） | `ConversationRuntime` 的单事件入口 + `ITimer` 自醒（`ConversationRuntime.cs:64,1345-1373`） | 见 §3.9 |
| `wait` 工具（模型可见） | 无对应 | 见 §3.13，`Pending` |
| `focus_mode_manager` | 无对应 | 见 §3.14，`Pending` |
| monitor / `stage_status_board` | `RuntimeTrace`（`RuntimeTrace.cs`）+ 只读诊断端点 | 见 §3.15 |

## 3. 逐机制对照

### 3.1 会话实例管理：LRU、容量、保留窗、创建锁

**上游**（`chat/heart_flow/heartflow_manager.py`）：`OrderedDict`（`:22`）+ LRU 移动（`_touch_chat`，`:54-57`），硬容量 `HEARTFLOW_MAX_ACTIVE_CHATS = 100` 与活跃保留窗 24 h（`:14-15`），超限只淘汰"24 h 内无消息"的旧会话且保护刚创建的那一个（`_evict_over_limit_chats` / `_find_evictable_session_id`，`:59-76`）；每 session 一把 `asyncio.Lock` 防并发重复创建（`:23,33-42`）。

**Pry**：宿主按会话持有 runtime，无容量上限、无 LRU；驱逐判据是**状态洁净度**而非数量——`Idle` 且无 pending 输入/输出持续 `EvictAfterIdleMs`（默认 900 s，`RuntimePorts.cs:176`）才允许回收；创建并发由 `_lifecycleGate` + `Lazy`(ExecutionAndPublication) 收口。

**判定：Adapt。** Pry 不照搬"100 个 + 24 h"，那是面向多人群聊部署的容量参数，而 Pry 1.0 是单机单用户桌面：会话数量级在个位数到几十，按数量驱逐会驱逐掉一个"只是安静但仍有未投递输出"的会话，直接违反本任务的状态一致性要求。代价必须写清：**Pry 目前没有会话数上界保护**，若将来支持多角色/多会话并发常驻，须补容量与保留窗（§6 DEV-11）。

### 3.2 "用户说完了吗"：静默窗口

**上游**：`_message_debounce_seconds = 1.0` 是**硬编码**（`runtime.py:177`），且**只在 RUNNING 期间又来消息时**才置位（`:930-931`），即静默窗只服务于"打断后重新攒齐"（`reasoning_engine.py:846-860,900-901`）。没有 typing 参与判定。

**Pry**：`InputBuffer.Assess`（`InputBuffer.cs:121-148`）综合四路：`DebounceMs`(1200) 基线、typing 持锁（`TypingHoldMs` 4000，且**标志会自我过期**，`:111-115`）、半句启发（`IncompleteHoldMs` 900）、防饿死硬上限（`MaxBufferMs` 5000）；输出在投递时基线翻倍（backpressure）。

**判定：Adopt + 扩展。** 形状相同（"最后一次活动之后静默 X 则成轮"），Pry 多了 typing、半句与饿死保护三路输入，且把停止 typing 也计为一次活动（`:106-108`）——否则角色会在用户刚停手的一瞬抢话。Pry 的 `MaxBufferMs` 是上游没有的**上界**，它保证连续输入不会把 Turn 无限推后。

### 3.3 轮次入口的闸门次序

**上游**：`schedule_message_turn()`（`turn_scheduler.py:63-132`）顺序为 focus 准入 → wait 态处理 → 单槽防重（`_message_turn_scheduled`，`:81-82`）→ 有待处理 → 静默频率 → 强制触发 → 空闲退避 → 按模式选门。

**Pry**：`TurnScheduler.Decide`（`TurnScheduler.cs:83-138`）两段式，顺序**不可交换**：先状态闸门（无输入 / 本轮在飞 / 输出未清 / pending 轮次上限 / 缓冲未完成），再触发门控（静默频率 → @/提及 → 空闲退避 → 按模式选门）。单槽防重在 `ConversationRuntime` 侧由 `_active` 与 `MaxPendingTurns = 1` 承担。

**判定：Adopt。** 两段次序与上游一致，且 Pry 把它写成断言级注释，因为一旦把"攒够没有"排到"本轮在不在飞"之前，同一轮会被重复规划。差异：上游 focus 准入在链首（Pry 无 focus，见 §3.14）；Pry 的"输出未清 → `Backoff(DebounceMs)`"在上游对应的是打断后的静默窗，形状不同、目的相同。

### 3.4 频率折算阈值

**上游**：`_get_message_trigger_threshold()`（`runtime.py:1127-1134`）：`f<=0 → 0`；必要性模式 `ceil(1/f²)`；计数模式 `ceil(1/f)`；均 `max(1, ·)`。

**Pry**：`ReplyFrequency.Threshold(f, mode)`（`TurnGates.cs:50-57`）逐式一致。

**判定：Adopt（同式）。** 这是机制不是调参，按源保留。**必须显式记下一条语义陷阱**：同名为"阈值"的量在两个模式下含义相反——计数模式是硬门槛，必要性模式**只是压力分的分母**（`ReplyNecessity.cs:331-347` 用它做归一化）。f=0.3 时两模式给出 12 对 4，只按参数名理解必然误配。

### 3.5 空窗补偿与延迟自醒

**上游**：`FrequencyThresholdTurnGate._calculate_idle_compensation`（`turn_gates.py:157-196`）：`pending<1` 直接拒（`:170`，注释写明与封顶互为双保险）；折算量封顶 `threshold-1`（`:181-184`），"即便空窗无限长也不能让纯沉默跨过阈值"；不够则给 `delay = (threshold-pending)×avg - idle`（`:195`）并由 `_defer_message_turn_check` 设闹钟重查（`runtime.py:1243-1247,1264-1275`）。

**Pry**：`FrequencyThresholdTurnGate.Evaluate`（`TurnGates.cs:80-113`）同式同双保险，`Delay` 映射为 `GateDisposition.Backoff("idle_compensation", …)`（`TurnScheduler.cs:160-162`），由 `ArmWakeupLocked`（`ConversationRuntime.cs:1345-1360`）设 `_wakeTimer` 注入 `TurnDue` 事件。

**判定：Adopt。** 关键不变量已在两侧对齐：**延迟不是一个拒绝，而是"到点再问我一次"**；若把它实现成 `Wait`，低频会话里已到达的输入会因为没有新输入而永久挂起。Pry 的 `Delay ?? 1s` 兜底只在参数被配成 0 阈值时生效。

### 3.6 到达节奏统计

**上游**：`runtime.py:1141-1192` + 常量 `:83,88,91`。三点值得注意：连发抖动（<5 s）**只推进 `LastExternalAt`、不产生样本**（`:1176-1184`）；窗口 1800 s；均值为空但"见过外部消息"时**回退到 30 s 下限**而非 `None`（`:1160-1163`），注释明说这是为了不让空窗补偿与延迟自醒因缺样本而永久失效。

**Pry**：`MessageIntervalStats`（`MessageIntervalStats.cs:60-101`）三点全同（含回退托底），仅窗口收为 900 s。

**判定：Adopt。** 窗口收窄是 Pry 自己的取舍：单用户桌面下 30 min 前的节奏对当前判断几乎没有信息量，而长窗口会让"用户回来后的新节奏"被旧节奏拖住。

### 3.7 回复必要性评分

**上游**：`reply_necessity.py`：相关度阶梯 `@=100 > 提及=80 > 私聊=40 = focus=40 > 普通=0`（`:139-153`）；内容层命中问题 +15、请求 +20、征询 +20、长文本 +5、较长 +10、短反应批量 −25（`:239-276`）；压力层阈值内二次、超阈值对数、空窗达标 +15、上限 100（`:212-236`）；存在感层占比 0.25 免罚、0.60 罚满 25（`:196-209`）；四层相加后**整体乘 `0.5 + 0.5f`**（`:174-177`）。词表与占位前缀在 `:16-23`。

**Pry**：`ReplyNecessityScorer`（`ReplyNecessity.cs:140-378`）逐层同形状。四处 Pry 改动：
- `TriggerScore` 80 → 70：Pry 的相关度阶梯里私聊只有 45，80 分门槛会让"无提问词的私聊陈述"几乎永远不成轮；
- 私聊/焦点相关度 40 → 45：同上，与 `TriggerScore` 是一对联动参数；
- 词表外置为可注入参数（`QuestionTerms`/`DirectRequestTerms`/`WeakRequestTerms`/`OpinionTerms`/`ShortReactions`），`OtherAssistantNames` 默认**空**（`:111`）——上游硬编码了 DeepSeek/ChatGPT 等名单（`:23`），那是它的场景，Pry 交给宿主；
- 角色名不再硬编码：上游征询判定要求文本含"麦麦"（`:126,131`），Pry 从输入快照取 `CharacterNames`（`:307-325`）；取整改 `AwayFromZero`（上游 Python `round` 是银行家舍入）。

**判定：Adopt（形状）+ Adapt（词表与角色名）。** **未解除的债务写在这里**：词表本身（"哈哈/草/笑死/666"等）是上游群聊语料，随 `ported` 类型合法入仓，但对 1.0 的私聊陪伴场景是**未审校的借来语料**；它不在 §4.3 的禁止项里（不是阈值/频率/容量参数），所以不构成许可缺陷，但必须随 §5 的参数箱一起过 Eval。

### 3.8 连续空转退避

**上游**：`idle_backoff.py`：指数 `base × 2^(count-start)` 封顶 `cap`（`:24-34`）；**只作用于群聊**（`:49-51,74-75`）；"说过话"即复位（`:44-46`，判据是 `cycle_end_reason ∈ IDLE_CYCLE_REASONS` 字符串集合，`mode_policy.py:5-10`）；焦点激活时复位并放行（`:70-72`）；积压达到 `bypass_pending_count` 无条件放行（`:85-91`）。

**Pry**：`IdleBackoffController`（`IdleBackoffController.cs:33-99`）同结构，两处 Adapt：用显式布尔 `idleCycle` 取代上游的字符串原因匹配（把"轮次为什么结束"的知识留在调用方，退避控制器不认识 planner 词汇）；时钟由调用方传入。`TurnScheduler.cs:131` 传 `focusActive: false`。

**判定：Adopt（结构）+ Pending（焦点豁免）。** 与 §3.14 同源。当前 Pry 的退避比上游**更保守**（少一条豁免放行路径）。

### 3.9 触发信号的生产与消费

**上游**（本轮才补齐的证据链）：三处生产 `_internal_turn_queue`——消息 `runtime.py:1237-1241`、wait 超时 `:1877`、主动任务 `:743`；单处消费 `reasoning_engine.py:1006-1010`。出队后先 `_drain_ready_turn_triggers`（`:1164-1190`）把队列里已就绪的同类/异类信号**合并成一次轮次**，再过 focus 二次准入（`:1011-1017`，注意：入队时过一次、出队时再过一次）。防重入靠 `_message_turn_scheduled` 单槽 + 静默窗。

**Pry**：单事件入口 `ApplyEvent`（所有事件在一把锁内串行），自醒由 `ITimer` 注入 `TurnDue`；"合并"发生在缓冲层而不是队列层——`InputBuffer` 天然把窗口内的多条输入合并成一个 `TurnRecord.CombinedInput`，`MaxPendingTurns = 1` 等价于上游单槽。

**判定：Adopt（同一目的）+ Adapt（把队列语义下沉到缓冲）。** 上游需要 drain 合并是因为它是**信号队列 + 索引切片**（一条消息一个信号）；Pry 是**状态机 + 缓冲批**（一个窗口一批输入）。这条差异的实际后果：Pry 不需要"过期触发忽略"分支（`reasoning_engine.py:940-942`），但必须有 §3.5 的自醒闹钟，否则没有第二个信号来叫醒它。

### 3.10 频率为 0 的静默语义

**上游**：`turn_scheduler.py:90-96` 在静默频率下**照常 `_enqueue_message_turn()`**；消费侧 `_handle_silent_turn`（`reasoning_engine.py:1131-1162`）清强制态、消费主动触发、跑 `_post_process_chat_history_after_cycle(enable_mid_term_memory=False)`、进 stop 态并打日志"静默接收并完成历史维护，不进入 Planner"。即：**静默不等于丢消息，而是零模型调用 + 保留历史维护**。

**Pry**：`TurnScheduler.cs:126` 返回 `GateDisposition.Cancel("frequency_silent")`，事件入口在闸门之前已完成落库（`AcceptAsync` → `_store.AppendUserInputAsync`），所以消息不丢，但不成轮、无任何后处理，轨迹记 `consumed_silently`。

**判定：Adopt（不落库丢失这条底线一致）+ Adapt（不做历史维护）。** 成本结论现在可以下了：**上游静默轮的实际代价不是模型调用，而是每轮一次历史裁切/后处理**；Pry 的历史与记忆不在 Runtime 名下（Phase 5 与 Persistence Owner），Runtime 无对象可维护，因此 Pry 侧照搬只会是空转。**必须登记的后果**：宿主把 `ReplyFrequency` 配成 0 时，用户消息照常落库但永不成轮，界面上不会有任何解释——今天只有 `consumed_silently` 轨迹可查。显式"已静默"投影或配置校验待协议负责人裁定（§6 DEV-08）。

### 3.11 强制触发：@ 与提及

**上游**：`_update_message_trigger_state`（`runtime.py:1277-1298`）检测 @/提及后置强制态，**但受两个配置开关约束**：`inevitable_at_reply` 默认 `True`，`mentioned_bot_reply` 默认 `False`（`official_configs.py:571-597`）——即上游**发行默认下只有 @ 必回，光提到名字不强制**；强制态会顺带 `_idle_backoff.reset()`（`:1311`），被点名不该背着退避包袱；消费是一次性（`_consume_forced_turn_reason`，`:1332-1345`），且把理由**以文案形式**注入下一轮（`reasoning_engine.py:1036-1037`）。

**Pry**：`TurnScheduler.cs:129`：`forced || HasAt || HasMention → Run`，三个入口合并、无开关；`Reset` 语义由退避控制器在 `Run` 之后处理。

**判定：Adapt，且是一处放宽——Pry 把上游默认关闭的"提及必回"写成了无条件。** 理由：Pry 1.0 是单角色私聊为主，"叫到名字就回应"是产品承诺而非可配项；群聊侧 @/提及检测的真实来源尚未接线（§7 步骤 1 与 `docs/character-runtime.md` Known Issues），今天 `HasMention` 在生产路径上恒为 false，所以该放宽**未在生产上被激活过**。待 `IsGroupChat`/提及判定接线时，必须一并决定是否补回开关（§6 DEV-09）。上游"强制态复位退避"这一条 Pry 尚未照搬，登记为缺陷（§6 DEV-10）。

### 3.12 插话与打断预算

**上游**：`PlannerInterruptController`（`runtime.py:94-136`）四态返回 `request/duplicate/limit/idle`——**一次飞行请求最多打断一次**，且受 `planner_interrupt_max_consecutive_count` 约束；该配置**默认为 0**（`official_configs.py:616-629`），代入 `request()` 即 `0 >= 0 → "limit"`，**上游发行默认下从不因新消息打断 planner**，靠 §3.2 的打断后静默窗在下一内部轮合并新消息（`reasoning_engine.py:1082-1099,846-860`）。预算从配置实时读取（`runtime.py:236-239`）。

**Pry**：`InterruptionController` + `IInterruptClassifier`：插话是一等结果，裁决为继续投递 / 取消剩余气泡 / 取消整轮重规划；Capability 执行期间改口一律作废重规划；轻量背书式插话可被上一轮回复"顺带听见"并从缓冲剔除（`InputBuffer.RemoveAbsorbed`）。**没有连续打断预算，也没有"一次飞行只打断一次"的约束**——靠 `MaxPendingTurns = 1` 与打断退避（`TurnScheduler` 的 `_strikes`/`Backoff`，`:51-67`）压住反复作废。

**判定：Adopt（插话是一等语义）+ 一处 Pry 更强（打断退避按连续次数递增，上游无此项）+ 一处 Pending（打断预算）。** 上游默认 0 这个数字本身就是它的立场：**打断是昂贵操作，默认不做**。Pry 的角色说明书把"插话取消旧输出"列为必需行为，所以不能照抄默认值；但缺连续预算意味着用户狂发时角色可以无限次重规划。归 §6 DEV-04（High），修法不是抄 0，而是加"连续打断上限 + 到点转为让麦克风"。

### 3.13 wait 与逻辑轮续接

**上游**：`wait` 是模型可见工具（`builtin_tool/wait.py:31-72`，默认 30 s，`max(0, int)`，越界回退 30）；进入 wait 必须绑定非空 `tool_call_id` 与 `logical_turn_id`（`runtime.py:1841-1842`，否则抛错）；**等待期间新消息不打断**（工具回执原文 `:65`），且消费侧直接忽略消息触发继续等超时（`reasoning_engine.py:891-898`）；连续 wait 有上限（默认 3，`official_configs.py:631-643`），到限返回 `wait_rest` 并把轮次记为空闲轮（`mode_policy.py:5-10` → 喂给 §3.8 退避）；恢复时把"等待结束/超时"合成为一条 `ToolResultMessage` 注入历史并续接同一逻辑轮（`:1192-1208`）。

**Pry**：`RuntimePhase.WaitingForTurn` 只表示"在等下一轮形成"，**没有模型可见的 wait 原语，也没有逻辑轮 ID 续接**。最接近的两处是 capability 等待（`ReplyGateDisposition.DeferForCapability`）与 `ProactiveCandidate` 的到期投递。

**判定：Pending，且归属不在本 Owner。** 这是 Phase 2 与上游最大的一处形状缺口：上游用 wait 把"角色主动选择暂时不看"变成一次可恢复、可解释、有预算的运行时状态。Pry 要做到同样语义需要 (a) Intelligence 侧的一个意图（RT-003），(b) 工具/能力回路（RT-007，本任务明确不做完整 Agent Harness），(c) 逻辑轮续接的身份扩展（现有 `TurnIdentity` 三者都不表达"同一逻辑轮的第二次物理轮"）。登记为 §6 DEV-05，并在 §7 明确不排进本轮。

### 3.14 focus / 焦点仲裁

**上游**：`focus_mode_manager` 在入口链首（`turn_scheduler.py:65-70`）、出队后（`reasoning_engine.py:1011-1017`）、消息注册（`runtime.py:936-959`）三处把关；带白名单（`focus/manager.py:57`）、冷却窗（`:60`）、准入判定（`:224`）、槽位抢占与释放（`:234,:261,:279`）、@ 解除再入限制（`:301`）、焦点切换（`:315`），并向下游提供两条豁免/加权路径——空闲退避复位放行（`idle_backoff.py:70-72`）与必要性正相关输入（`turn_gates.py:76`）。

**Pry**：`FocusActive` 在 `TurnScheduler.cs:131,144` 两处固定 `false`。

**判定：Pending（有意不接）。** 焦点是角色内部对象，接它需要新增宿主端口并把 Character State 暴露给 Runtime——正是本任务边界禁止的两件事（"不得把整个 Character State 做成 Runtime 可随意修改的 Dictionary"）。实际方向偏保守：少一项正相关输入、少一条退避豁免路径 ⇒ **更少成轮**。归 §6 DEV-06，端口形状待与 RT-003 Owner 一起定。

### 3.15 诊断

**上游**：`monitor`（`runtime.py:889-913`、`monitor/event_store.py`）+ `stage_status_board`，面向"给人看的实时面板"。

**Pry**：`RuntimeTrace`（环形缓冲 `TraceCapacity = 300`）+ 只读端点 `GET /conversations/{id}/runtime-state`、`.../runtime-trace`；轨迹 `trigger:` 的 detail 只带分数、门名与命中的配置词，**不回显用户原文**。

**判定：Adopt + Pry 多一条隐私约束**（Developer Mode 可读决策依据不等于可以复读用户语料）。

## 4. Pry 独有、必须保留并显式声明的更强约束

这些在上游没有对应物，是 Pry 的领域要求，不因"对齐上游"而放宽：

1. **失效隔离（fencing）**：`TurnIdentity(TurnId, GenerationId, FencedRevision)` + SQLite 权威 epoch（`ConversationRuntime.cs:557-583`）。上游靠"旧结果回来时状态已不是 RUNNING"这种进程内判断；Pry 要求**即使取消不及时，每个气泡落库前仍按 epoch 复核**（`DeliveryScheduler` 逐条 `RequestDeliveryAsync`）。这是本任务"旧结果不得继续发送"的实现本体，属必须保留项。
2. **落库先于闸门**：消息在事件入口即持久化，`Cancel`/`Wait` 都不会丢输入（§3.10 的前提）。
3. **时钟全注入**：`TimeProvider` + 可注入 `ITimer`，整条触发链在假时钟下可复现——上游的 `time.time()` 与裸 `asyncio.create_task` 做不到确定性回归。
4. **单锁串行**：所有状态迁移在一把锁内同步完成，异步效果一律携带身份出锁。上游是多 `asyncio` 任务 + 多处标志位。
5. **DoNothing 是一等结果**：`RuntimeTurnOutcome.DoNothing`、`ReplyGateResult.DoNothing`、`SilentReplyAdvisor`。上游的"不说话"是 planner 没有调用 reply 工具的副产物。
6. **主动消息不绕道**：`ProactiveCandidate` 必须过同一套闸门、打断与投递通道，且带"会话进度水位"（`CapturedProgress`）——只比 epoch 不够，用户回来聊过一轮之后旧候选即使没被打断也已经过时。
7. **Core 不依赖 Avalonia/HTTP，Runtime 不查 SQLite**：评分输入全部由 Runtime 自己的内存状态算出（`TriggerFacts`，`TurnScheduler.cs:7-10`）。

**一处必须诚实的落差**：`RuntimeModels.cs` 的类型注释曾称"GenerationId 每次重新规划递增"，而生产代码 `NewTurnIdentityLocked()`（`ConversationRuntime.cs:557`）恒填 `1`，`Capability` 续规划复用同一身份（`:852-886`）。今天不构成正确性漏洞（同身份内只有一次续规划，且 epoch + 取消令牌覆盖），`docs/character-runtime.md` 对这一点的描述本来就是对的，错在类型注释；已就地改为陈述现状（§6 DEV-12）。

## 5. 逐值参数对照（`DEF-P2TRIGGER-01` 的证据表）

上游"发行默认值"两列来自 `src/config/official_configs.py`（Pry 移植的是**参数化对象**，其默认值列在中间）；标量若来自源码常量而非配置，出处写 `runtime.py` / `reply_necessity.py` 行号。**"关系"列是本文唯一的裁决事实来源**。

| Pry 参数（默认） | 上游值与出处 | 关系 | 处置 |
|---|---|---|---|
| `ReplyNecessityOptions.TriggerScore` = 70 | 80，`reply_necessity.py:8` | **Pry 改动** | 与下两行联动，须一起标定 |
| `RelevanceAt` = 100 | 100，`:139-141` | 同值 | 不进发行默认（作者已裁）；值押后 |
| `RelevanceMention` = 80 | 80，`:142-144` | 同值 | 不进发行默认（作者已裁）；值押后 |
| `RelevanceDirect` = 45 | 40，`:145-147` | **Pry 改动** | 待标定 |
| `RelevanceFocus` = 45 | 40，`:148-150` | **Pry 改动**；且 Pry 侧 `FocusActive` 恒 false，该值今天不可达 | 待标定 + 随 focus 端口复判 |
| `ContentQuestion/Request/Opinion` = 15/20/20 | 15/20/20，`:248,256,264` | 同值 | 不进发行默认（作者已裁）；值押后 |
| `ContentLong/VeryLong` = 5/10 @40/120 字 | 5/10 @40/120，`:269,272` | 同值；Pry 用 `InputProbe.VisualLength` 而非 `len()`（Adapt） | 不进发行默认（作者已裁）；值押后 |
| `ContentShortReactionPenalty` = 25 @≤8 字 | 25 @>8 即否，`:275`、`:82` | 同值 | 不进发行默认（作者已裁）；值押后 |
| `PressureAtThreshold/Max/FullRatio/IdleBonus` = 50/100/5.0/15 | 同值，`:9-12` | **全部同值** | 不进发行默认（作者已裁）；值押后 |
| `PresenceFreeRatio/FullRatio/PenaltyMax` = 0.25/0.60/25 | 同值，`:13-15` | **全部同值** | 不进发行默认（作者已裁）；值押后 |
| `PresenceWindow` = 5 min | 300 s，`turn_gates.py:101` | 同值 | 不进发行默认（作者已裁）；值押后 |
| 频率因子 `0.5 + 0.5f` | 同式，`:176` | 同式（机制） | 按源保留 |
| 词表（问题/请求/弱请求/征询/短反应） | `:16-20` 逐条同集合 | **沿用上游群聊语料** | 非 §4.3 禁止项，但属未审校借来语料，须过 Eval |
| `MessageIntervalOptions.Window` = 900 s | 1800 s，`runtime.py:83` | **Pry 改动** | 改动理由见 §3.6 |
| `BurstFloor` = 5 s / `MinimumAverage` = 30 s | 5 s / 30 s，`:88`、`:91` | 同值 | 不进发行默认（作者已裁）；值押后 |
| `IdleBackoffOptions.Base` = 10 s | 15 s，`official_configs.py:645-646` | **Pry 改动** | 待标定 |
| `Cap` = 180 s | 300 s，`:663-664` | **Pry 改动** | 待标定 |
| `StartCount` = 3 | 2，`:679-680` | **Pry 改动** | 待标定 |
| `BypassPendingCount` = 6 | 6，`:695-696` | **同值** | 不进发行默认（作者已裁）；值押后 |
| `RuntimeSettings.DebounceMs` = 1200 | 1000 ms 硬编码，`runtime.py:177` | Pry 可注入且更长 | 待标定 |
| `MaxBufferMs` = 5000 / `TypingHoldMs` = 4000 / `IncompleteHoldMs` = 900 | 无对应 | Pry 独有 | 待标定 |
| `ReplyFrequency` 默认 1.0 | `talk_value` / `private_talk_value` 默认 1，`:537-568` | 同值 | 与上游一致，无需改 |
| `TriggerMode` 默认 `MessageCount` | `reply_trigger_mode` 默认 `"frequency"`，`:599-614` | **同义**（计数模式） | 见下方"惰性默认的依据" |

**惰性默认不是 Pry 的怯懦，而是与上游同一条立场**：上游自己发行的是计数模式 + 频率 1.0 + "提及不强制回" + "打断预算 0"。也就是说，**上游今天在生产上跑的正是这条保守链**，Pry 把必要性模式与更激进的默认留在测试里，是同一判断而非额外打折。

**处置（`DEF-P2TRIGGER-01`，交作者裁定）**：`docs/upstream-license-compatibility.md` §4.3 第三条禁止照搬上游群聊调出来的阈值/频率/容量参数。上表标"同值"的行全部在禁止面内。两条出路——(a) 换一组 Pry 自定值；(b) 保留量级、由 Eval 标定覆盖后替换——**都不允许这些数字今天成为发行默认值**。(a) 的风险是"把巧合换成任意"，本文不建议在没有判据前换数。(b) 是建议路线，需要 EVAL-001 新增永久场景承接标定协议（新增 ID 归 QA）。

## 5.1 裁决记录（2026-09-23 深夜 · 作者）

三条路线里作者选的是第三条：**只裁"暂不进发行默认"，逐值处置押后**。据此：

- 从今天起这条是**约束**而不再是本 Owner 的判断：上表任何数值（含 Pry 改动的 5 项与全部"同值"项）**不得写进发行说明、配置模板或对外的默认值表**；链条必须继续按 `TriggerMode=MessageCount` + `ReplyFrequency=1.0` + 私聊发布。切到 `ReplyNecessity` 需要**两个**前置：一轮 Eval 标定通过 **且** 逐值裁决落地，缺一即不得切。
- 明确押后、**不得由本 Owner 代拍**的：(a) 换一组 Pry 自定值，或 (b) 保留量级由标定覆盖。上表处置列写"值押后"就是这个意思，不要读成"已决定保留"。
- 直接后果：§8 出口判据第 2 条（"每个值有一种处置"）**仍不成立**，`DEV-02` 不得转结案；本文也不得因为"反正不进默认"就把这箱参数视为已解决——它只是被冻结在不可发行的位置上。

本文对那两条押后路线的技术意见不变（(a) 的风险是把"巧合"换成"任意"；(b) 需要 EVAL-001 新增永久场景承接标定协议，ID 归 QA），但那只是意见，不是裁决。

## 6. 偏差登记表

| ID | 偏差 | 判定 | 严重度 | 归属 | 状态 |
|---|---|---|---|---|---|
| DEV-01 | 触发链两段式、双门控、空窗补偿、必要性四层、退避、间隔统计已按源移植并留痕 | Adopt | — | Runtime | 已提交 `8e039f9`（按 §8.1 记 `已提交待验`） |
| DEV-02 | §5 全部"同值"行：沿用上游参数，§4.3 禁止面内 | 合规缺陷 | High | 作者裁定 | **部分裁决**（`DEF-P2TRIGGER-01`：作者已裁"暂不进发行默认"，逐值处置押后，见 §5.1）⇒ 仍未结案 |
| DEV-03 | 未接入真实模型的必要性标定 | Pending | High | Runtime + EVAL-001 | 开，是 DEV-02 的解法 |
| DEV-04 | 缺连续打断预算与"一次飞行只打断一次"约束（上游默认 0 即不打断） | Pending | High | Runtime | 开 |
| DEV-05 | 无模型可见 wait 原语与逻辑轮续接 | Pending | Medium | RT-003 / RT-007 | 开，不排进本轮 |
| DEV-06 | `FocusActive` 恒 false，缺焦点仲裁与退避豁免 | Pending | Medium | Runtime + RT-003 | 开 |
| DEV-07 | 上游类型未进领域层、只取其机制/评分形状 | 符合边界 | — | Runtime | 已满足（§4.3 第 4 条） |
| DEV-08 | `ReplyFrequency=0` 静默无用户可见解释 | Adapt 副作用 | Medium | PLAT（投影）/作者 | 开 |
| DEV-09 | "提及即回"无条件放行，上游默认关闭 | Adapt | Medium | Runtime | 开，随提及判定接线复判 |
| DEV-10 | 强制触发未复位空闲退避计数（上游 `runtime.py:1311`） | 未照搬 | Low | Runtime | 开 |
| DEV-11 | 会话实例无容量/保留窗 | Adapt | Low（单机） | Runtime | 开，多会话前复判 |
| DEV-12 | `GenerationId` 恒 1，注释与实现不符 | 文档缺陷 | Low | Runtime | 本轮改正注释 |
| DEV-13 | `MAX_INTERNAL_ROUNDS = 10` 与 `mode_policy` 字符串模式切换未搬 | Reject（有理由） | — | — | 结案：前者属 Agent Harness（本任务明确不做），后者是 §3.8 的布尔化替代 |

## 7. 下一轮顺序与端口归属

按依赖排序，不并行：

1. **宿主端口**：`IsGroupChat` 与用户可设 `ReplyFrequency` 进 `ConversationRuntimeOptions`（Api 侧，Runtime 不查库）。今天整条链在生产上仍是惰性分支，这一步之后新行为才真的跑过。
2. **标定**：`ReplyNecessityOptions`、`MessageIntervalOptions`、`IdleBackoffOptions` 三个参数箱过 Eval；标定通过才允许把默认 `TriggerMode` 切到 `ReplyNecessity`。同时裁 §3.7 的借来词表。
3. **打断预算**（DEV-04）与强制触发复位退避（DEV-10）。
4. **焦点端口**（DEV-06）与到达节奏跨重启持久化（归 Persistence Owner）。

### 7.1 跨 lane 归属条款

本节不是礼节性的"感谢名单"，它的作用是：上面每一步在动手之前先在这里对一次线，避免 Phase 2 的实现把别人的冻结结论改掉。分工线本体在 `docs/phase1-platform-baseline.md` §5、§5.1，此处只登记与**触发层**相撞的四条。

1. **对外通道只有一个，且不在本 Owner 手里**（§5）：Runtime 的全部外发经 `IRuntimeOutputSink`（`RuntimePorts.cs:107-124`，六个方法），宿主实现与"方法调用 → 协议事件"的映射归 Core Platform Owner。本契约里任何判为 `Adopt`/`Adapt` 的机制都**不得**新增 sink 之外的对外通道，也不得让 Runtime 自己发 SSE 或持有事件日志。§7 第 1 步"宿主端口"因此是改 `ConversationRuntimeOptions` 的入参形状 + Api 侧供值，不是加一条新的输出。
2. **失效权威在 SQLite，不在进程内**（§5）：epoch 由 `IRuntimeStore.ReadEpochAsync`/`BumpEpochAsync` 承载（`RuntimePorts.cs:10` 起），Runtime 只在锁内比较 `TurnIdentity.FencedRevision`。本契约 §3.6 提到的"节奏统计跨重启丢失"不能靠 Runtime 自己读库解决——那是 Persistence Owner 的表/列，且 SQL 收敛本身已挂在 PLAT-004 名下。§7 第 4 步按此归属，不由本 Owner 代做。
3. **Capability 类型的住处还没满足冻结结论**（§5.1）：`CapabilityRequest`/`CapabilityResult` 目前定义在 `Pry.Core.Runtime`（`RuntimeModels.cs:143,146`），而 §5.1 要求它们与 `ICapability`/`CapabilityDescriptor`/`CapabilityPermission` 同归 `Pry.Core.Abstractions`（那里今天只有 `IChatModel` 等同侧接口，`ICapability` 系在 `src/` 内仍零命中）。这与触发层有两处实打实的相撞：§3.12 的打断要按"是不是 capability 续轮"判、§3.13 的 wait 要消费 `CapabilityResult`。所以 DEV-04/DEV-05 的前置里包含这次搬迁，且搬迁**不要求**提前实现 RT-007 的通用 Tool Loop（§5.1 同一句冻结结论）。顺带更正坐标：§5.1 写的 `RuntimeModels.cs:137,140` 因本轮给 `TurnIdentity` 补注释而下移到 `:143,146`，不是类型被移动过。
4. **不引入第二个 Context 概念**（§5.1 末段）：必要性评分的输入全部是 Runtime 进程内已测得的事实（`TriggerFacts`），不接 Memory。`IRelevantMemorySource` 与 `IMemoryRetriever` 的收敛归 Memory Owner；本契约若哪天要让"记忆相关性"进评分，那是先改 §5.1 归属、再动代码，不是在 Runtime 里加一条检索旁路。

一句可执行的判据：**把任何 `Pending` 转成实现的轮次，开工前先在本节列出它碰上面哪条冻结结论；碰了就去改对方的文档并让对方确认，而不是在自己文档里写"应该没问题"。**

## 8. Phase 2 出口怎么判，以及"完成"这个词由谁写下（草案，判定权不在本 Owner）

Phase 2 的完成判据不是"本文写完了"，也不是"测试数涨了"，而是下列四条同时成立且能被第三方复跑：

1. 上游一致性：§3 每节有判定，`Pending` 项要么关闭、要么由作者显式降级到后续阶段（不接受"默认消失"）；
2. 合规：`DEF-P2TRIGGER-01` 有作者裁决（**2026-09-23 部分成立**：已裁"暂不进发行默认"，见 §5.1；逐值处置仍押后 ⇒ **本条整体仍未成立**），§5 的"同值"行要么被标定替换、要么被显式保留并记录裁决；
3. 行为：真实模型节奏下"连发可合并、可不回复、插话取消旧输出、切会话不串状态"四条各有真机证据（不是假时钟下的单测）。**这条有现成的承接面，不必新造判据**：Phase 1 走查清单 §5 把 Timing / Behavior / Expression / Memory / Relationship / Capability 六类 Eval 与"真实模型对照、P50/P95"整体记为 `NotApplicable（Phase 2）` 且保留永久 ID（`docs/eval/minimal-loop-run-checklist.md` §5），Phase 2 的动作是把属于运行时的那几行从"移出"变成"判过"。
4. 集成：RT-002 的全部改动进入**可指认提交**，QA 从该提交的干净 worktree 复跑 §5.0 门禁与本表判据。

第 3、4 条与 Phase 1 走查同源，受 `PROJECT_PLAN.md` §5.0 阶段串行门禁约束：Phase 1 未结案前，本阶段可继续开发（作者 2026-09-22 裁定实施入口开放），但**阶段指针不由本 Owner 推进**。

### 8.1 四段验收链与状态词汇

一句判据：**"完成"不是一个由写代码的人单独填的格子。**本阶段每一步只在下面四段各留一次可指认的痕迹，状态词汇照此推进，跳段即视为该段未发生。

| 段 | 谁签 | 必须产出的东西 | 台账允许写的状态 |
|---|---|---|---|
| ① 自证 | Character Runtime Owner（本 Owner） | 代码 + 用例 + 本文对应节的判定与 DEV 状态 + 门禁四条实测数字（构建/测试/`verify-repository.ps1`/`git diff --check`）+ §9 交接记录一条 | `已落地主工作区未提交` |
| ② 复跑 | QA / Evaluation（独立于①的一次会话） | 从**可指认提交**的干净 worktree 重跑门禁；真机走查按 `docs/eval/minimal-loop-run-checklist.md` 的证据格式出报告，落 `docs/eval/reports/`，并在 `docs/eval/README.md` 加索引行；逐场景判定只用 `docs/eval/run-template.md` 那套词汇：`Pass`/`Fail`/`Blocked`/`NotRun`/`Inconclusive`/`NotApplicable` | `已提交待验` → QA 给出的判定 |
| ③ 裁决 | 作者（或其显式指派的守门人） | `DEF-*` 的处置、任何判据或场景语义的改动（须按 `docs/eval/README.md` 维护约定升 `suiteVersion`）、以及 §5.0 阶段指针的推进 | 遗留项标"已裁决"，指针才动 |
| ④ 发行 | Release 侧 | `CHANGELOG.md`、回滚说明（`PROJECT_PLAN.md` §8）、许可三处留痕复查（源文件头 / `licenses/upstream/**/LICENSE` / `THIRD-PARTY-NOTICES.md` 表）；整轮结论按 `run-template.md` 末段四词之一：未评定 / 阻塞 / 可进入下一验收阶段 / 接受 | `阶段结案` |

**双签的含义就这么具体**：`阶段结案` 需要 ② 的 QA 判定与 ③ 的指针推进两个动作同时存在，缺一只能停在 `已提交待验`；①与②不得是同一次会话自评自证——本 Owner 交付时把"我认为还差哪一段"如实写进 §9，就是给②的输入，而不是替②下结论。

### 8.2 不许用来充当"完成"的证据

- **用例总数**。数字变了只说明有人加了文件；本契约要求的判据是 §3 逐机制的判定与 §6 逐条 DEV 的状态。这条不是我个人洁癖，QA 侧早已立着同一句（`docs/eval/run-template.md`："不要把用例数当场景通过率"）。
- **0 警告 / 门禁全绿**。那是入场券（`PROJECT_PLAN.md` §7），不是行为达标；`docs/character-runtime.md` 的假时钟单测再绿也不构成第 3 条。
- **自评 `Pass`**。本 Owner 可以写 `Pending`、`Adapt`、"未标定"，不可以写"Phase 2 已通过验收"。
- **改判据让它变绿**。≤ 55 s 那一档在 Phase 1 被作者原样保住、由产品去够（`DEF-P1WALK-02`），那是先例：阈值归 QA 与作者，归实施侧的只有"把产品做到"。
- **引用上一轮的数字当本轮证据**（`PROJECT_PLAN.md` §7 末段同条纪律）。本文任何"实测"都必须与它所标注的那次运行同时成立。
- **删掉 `Pending` 行**。§6 的 DEV 只允许改状态并留原因，删除等于制造 §3 判据里第 1 条禁止的"默认消失"。

## 9. 给 EVAL-001 的 Phase 2 场景草案（永久 ID 由 QA mint，本 Owner 不自封）

§8 第 3 条判据要求真机与真实模型证据，而 `docs/eval/minimal-loop-run-checklist.md` §5 已把六类 Eval 与"真实模型对照、P50/P95"整体记为 `NotApplicable（Phase 2）` 并保留永久 ID。本节就是那几行的 Phase 2 对应物：把运行时侧"该被量到什么"摆出来，**ID 用 `P2-Draft-*` 前缀，只作提案**；永久编号、`suiteVersion` 与是否采纳全部归 QA（`docs/eval/README.md` 维护约定：ID 不删除、不复用）。

四组分别是 **TIM**（节奏与门控）、**ACT**（说不说）、**REC**（打断与恢复）、**PER**（真实模型经济性）。"今天可跑"这一列不是修辞——它记录的是宿主端口未接导致生产路径根本进不去的分支，QA 若照单全收会得到一批 `Blocked`。

| 草案 ID | 层 | 可观察变化与判据 | 现有自动化锚点（本轮逐条回源核过名） | 今天可跑 | 前置 |
|---|---|---|---|---|---|
| `P2-Draft-TIM-01` | Integration | 缓冲窗口内的连发只开一轮、且一条不丢：模型调用数＝1，落库 user 条数＝实际发言数 | `Rapid_consecutive_inputs_are_merged_into_one_turn_and_one_model_call`、`Inputs_at_zero_fifty_and_three_hundred_fifty_ms_form_one_turn` | 是 | — |
| `P2-Draft-TIM-02` | Runtime | 攒不够阈值时门控**自己按节奏醒来**成轮，不需要新消息推动；纯沉默（`pending=0`）多久都不成轮 | `A_backlog_below_the_threshold_wakes_itself_without_a_new_message`、`Silence_alone_never_opens_a_turn_how_long_it_lasts` | 部分（假时钟可跑，真机需端口） | §7 第 1 步 |
| `P2-Draft-TIM-03` | Unit | 未 settle 的半句不进门控：静默期/不完整保持/打字中租约/封顶四条各有独立判据 | `Idle_runtime_without_output_just_buffers_the_input`、`Immediate_flag_short_circuits_the_buffer`、`Repeated_typing_reports_renew_the_lease_but_disconnect_still_expires` | 是 | — |
| `P2-Draft-TIM-04` | Runtime | 群聊连续空转按指数退避到封顶，积压达门槛立即越过退避；答过话后计数立刻清零 | `Backoff_grows_exponentially_and_stops_at_the_cap`、`Waiting_messages_outrank_a_backoff_window`、`An_answered_turn_clears_the_backoff_immediately` | **否**（`IsGroupChat` 无宿主来源） | §7 第 1 步 + DEV-11 |
| `P2-Draft-ACT-01` | Runtime | `ReplyFrequency=0` 时：消息**照常落库**、一条不丢，但模型调用数为 0，界面不出现"正在输入" | `Silent_frequency_consumes_the_message_without_opening_a_turn` | 是 | DEV-08（用户可见解释） |
| `P2-Draft-ACT-02` | Runtime | 必要性模式群聊里没点名 ⇒ 0 次调用；同一会话被 `@角色名` 后 ⇒ 恰好 1 次 | `Group_chater_stays_quiet_in_necessity_mode_until_the_character_is_called` | **否**（默认是计数模式） | §7 第 1、2 步 |
| `P2-Draft-ACT-03` | Model | DoNothing 是一等结果：门放行但判定不必回时，终局记 `DoNothing` 且历史里不留半条气泡 | `Reply_gate_is_allowed_to_stay_silent`（`ConversationRuntimeTests.cs:198`）、`RuntimeComponentTests.cs:247` | 部分（假建议器可跑，真实模型未跑） | 真实模型档位 |
| `P2-Draft-ACT-04` | Model | 频率下降时"该沉默"的判断变严，而不是变成"攒更多条再一次性回一大段" | `Necessity_mode_asks_for_a_much_bigger_backlog_as_frequency_falls` | **否** | 标定（DEV-03） |
| `P2-Draft-REC-01` | Integration | 飞行中插入新消息 ⇒ 旧结果回来后**不得再发**，界面不出现两条互相覆盖的答复 | `Late_model_result_that_ignores_cancellation_is_still_not_delivered`、`Repeated_interruptions_stack_up_without_delivering_any_stale_reply` | 是 | — |
| `P2-Draft-REC-02` | Desktop | 切会话不串状态：A 会话的待投递气泡不会落到 B 会话，也不牵连 B 的进行轮 | `Epoch_change_blocks_old_output_without_cancelling_another_conversation` | 部分（内存态可跑，真机需界面配合） | — |
| `P2-Draft-REC-03` | Runtime | 崩溃/重启后遗留 pending plan 被清扫且**不重放**（进程状态可丢，角色状态不可丢） | `Reopen_sweeps_pending_plan_preserves_sent_bubble_and_never_replays` | 部分 | — |
| `P2-Draft-REC-04` | Runtime | 实例驱逐后重建：节奏统计与退避归零，可观察后果是低频角色重启后短期偏"话多" | 无判据（相邻事实只有 `Eviction_and_recreation_keep_conversation_continuity_and_fence_epoch` 判连续性） | **否** | DEV-11 + Persistence |
| `P2-Draft-PER-01` | Model | 真实模型下一轮最多付几次调用要有上界，且静默轮为 0（对照上游：静默轮仍付一次历史后处理，见 §3.10） | 无 | **否** | §7 第 1 步 |
| `P2-Draft-PER-02` | Model | 多气泡投递的间隔来自阅读时长与 `ReplyPacing.DelayFor`，**不得退化成固定字符数切分**；P50/P95 需与走查同格式记录 | `Pacing_scales_with_reading_time_and_never_uses_a_fixed_character_split`、`Later_bubble_cannot_commit_before_the_first_bubble` | 部分（形状有判据，P50/P95 没有） | 真实模型档位 |
| `P2-Draft-PER-03` | Model | 低频角色的经济性：设定 `f=0.05`、灌 100 条群消息，实际模型调用数应显著低于 100 | 无 | **否** | 标定 + 群聊端口 |

三条提给 QA 的口径提醒（都是本轮实测踩过的）：

1. **走查记录必须照填模型档位**，否则 ①/PER 类的耗时跨轮不可比（Phase 1 第八轮 22.38 s @1.7B vs 第七轮 36.9 s @4B 就是同一件事）。
2. **"今天可跑＝否"的行不是缺陷**。它们被 §7 的宿主端口挡住，登记为 `Blocked` 并写前置，不要为了让数字变绿而松判据或删行（`docs/eval/README.md` 维护约定同条）。
3. 标定为 `Pass` 的必要条件里包含"**参数已由 Eval 覆盖**"这一项：在 `DEF-P2TRIGGER-01` 有裁决、§5 的"同值"行被标定替换或显式保留之前，任何用当前默认值得出的 TIM/ACT 结论都只对 Pry 成立，**不得写成"上游行为如此"**。
