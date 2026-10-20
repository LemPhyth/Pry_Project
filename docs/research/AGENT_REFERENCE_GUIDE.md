# Pry 七个 Agent 的研究资料阅读指南

本文件是参考阅读指南，不替代各 Agent 的岗位提示词、当前任务授权或仓库架构契约。来源范围见 [README](README.md)。

## 共同阅读与使用原则

七个 Agent 都先读 [README](README.md) 的证据边界、**照搬边界（`DOC-005`）**与 1.0 范围，再读 [COMPETITIVE_MATRIX](COMPETITIVE_MATRIX.md)。涉及某项接口时回到项目文档的 Caveat，不能只引用矩阵中的一句话。

**照搬白名单是硬边界，不是偏好。** 作者 2026-09-21 把 Pry 代码许可改为 **GPL-3.0-only**，2026-10-20 又改为 **AGPL-3.0-only**（逐字："将许可证变更为AGPL-3.0，我需要借鉴HDS项目的架构组织方式"）。据此允许把上游源码文本搬进仓库的是 **MaiBot / MaiCore（GPL-3.0，经 AGPL §13 并入）**、**Satori（MIT）**、**HDS-Interlude（AGPL-3.0，10-20 点名）** 三家；**AstrBot（AGPL-3.0）、Nekro-Agent（自定义协议 V1.1）、Private Companion（无 LICENSE）、qq-bridge（MIT 正文另加一节第三方声明，GitHub 判 `NOASSERTION`）只作机制参考**。注意 AstrBot 这一行的性质变了：10-20 的改约**已经解除它的法律阻塞**，它现在出不来只是因为**作者没有点名**，所以不要写"因为许可不兼容所以不能抄"那种旧理由，也不要以为再改一次约就自动放行。白名单内也不等于范围扩张——仍受各 Agent 的 1.0 取舍与契约冻结约束。依据、实测 SHA 与三处留痕义务见 `docs/upstream-license-compatibility.md` §1、§4。各 Agent 不得自行放宽，也不得用"换语言重写"绕过禁止名单：那同样触发 copyleft 义务。

将研究转为实现建议时，明确说明：参考哪个项目/章节；借鉴的是机制还是具体接口；属于 1.0 还是未来；影响哪个 Contract；需要何种验证。源码符号是定位线索，重新使用上游实现前须锁定 commit 并复核。

不要因文档提及 Satori、完整 Harness、Code Sandbox、生活模拟就自行扩展范围。Pry 仍以本地桌面 Character Runtime 为核心。

## 1. Architecture / Integration / Release Agent

**优先顺序**：矩阵 → [MaiCore](MaiBot_MaiCore.md) → [Private Companion](PrivateCompanion.md) → [Satori](Satori.md) → [AstrBot](AstrBot.md) → [Nekro](Nekro-Agent.md)，五份均需阅读。

重点：持久 Runtime 与 Task Harness 的责任区别；Private Companion 的 Hook/巨大组合类和版本化 Memory Bridge；Satori 必须留在边界；Provider、Capability、Memory 和持久化的契约；插件运行范围与发布依赖。

应形成的判断：哪些设计值得写入 ADR，哪些只是未来参考；谁拥有跨模块状态与协议；为何不把五个系统直接叠加。不要把历史 alpha API、上游数据结构或插件内部类型作为 Pry 的默认契约。

## 2. Character Runtime Agent

**优先顺序**：[MaiCore](MaiBot_MaiCore.md) → [Private Companion](PrivateCompanion.md)；补读 [AstrBot](AstrBot.md) 的 Follow-up / Abort 与 [Nekro](Nekro-Agent.md) 的任务防抖差异。

MaiCore 现在不只是"参考"：作者已裁决 Runtime 按 `DOC-005` 白名单**照抄 MaiCore 的模块划分与决策结构**（Phase 2 RT-002 的规范目标），因为自研一套 Runtime 极其拖慢进度。Private Companion 虽然产品机制最贴近 Pry，仍在禁止名单内——它的主动终审、表达约束与 Memory Contract 只能作为机制描述，源码文本不进仓库。

重点：Pending Queue、TurnScheduler、Reply Gate、Waiting、IdleBackoff、PlannerInterrupt；用户连续输入；用户返回取消主动 follow-up；生成后 Review 与发送前状态有效性。

将经验应用到 Pry 的 ConversationRuntime / ResponsePlan / DeliveryScheduler 时，明确 Turn / Generation 的有效性、取消范围和旧结果抑制。不要把请求阶段的防抖 Hook 当作完整持久 Runtime，也不要把函数存在当作竞态已解决。

建议回归场景：连续输入、生成中插话、第一气泡后插话、过期主动候选、空闲无输入、Runtime 淘汰/恢复。测试由 Pry 的真实契约决定。

## 3. Character Intelligence & Learning Agent

**优先顺序**：[MaiCore](MaiBot_MaiCore.md) → [Private Companion](PrivateCompanion.md)；再用矩阵校准与通用 Agent 的差别。

重点：Behavior 的场景—行为—后果经验，与 Expression 的情境—表达习惯分离；Selector 的候选召回和二次筛选；Relationship / Emotion / Intent 影响表达；Evidence、Avoid、persona conflict、适用渠道等约束；主动消息人格和事实终审。

不要把表达数据库学习说成模型微调，不把关系值直接等同于关系连续性；不照搬所有生活模拟模块。为每个纳入 1.0 的学习机制说明来源、使用门槛与可解释结果。

验证重点：严肃/低落情景误用口癖、无证据学习、场景不匹配、人格冲突、主动消息不应发送却发送。

## 4. Memory & Context Agent

**优先顺序**：[MaiCore](MaiBot_MaiCore.md) → [Nekro](Nekro-Agent.md) → [Private Companion](PrivateCompanion.md)；补读 [AstrBot](AstrBot.md) 的上下文和大结果处理。

重点：用户本人发言支持稳定事实；异步写回与附近上下文消歧；pending、时间、优先级驱动 Consolidation；语义段落/Episode/实体关系的层次；Memory Contract、Producer Context、版本兼容；Token Budget 与上下文裁剪。

1.0 先解决事实/偏好/长期事件与关系相关上下文，不为复刻 Nekro 一次引入完整图谱和 Qdrant。Private Companion 的 Bridge 只能证明契约存在，不能补写 Memory Companion 内部算法。

验证重点：他人猜测污染用户事实、重复沉淀、后台队列积压、重复写入、来源/用户范围错误、契约版本不匹配、召回无关或过期内容。

## 5. Core Platform & Extensibility Agent

**优先顺序**：[AstrBot](AstrBot.md) → [Satori](Satori.md) → [Nekro](Nekro-Agent.md)；补读 [MaiCore](MaiBot_MaiCore.md) 的 PlatformIO / Plugin Runner 和 [Private Companion](PrivateCompanion.md) 的 Bridge。

重点：Provider / FunctionTool / Schema 转换；能力声明；IO 路由与去重；插件生命周期；消息与资源分离；标准接口与特有能力出口。未来再考虑完整 Harness / MCP、Remote Bot、Sandbox RPC。

Satori 必须逐项保留 alpha、resume、auth/internal/proxy、QQ Official 的 caveat。Nekro 的 Docker 和 RPC Token 不能替代安全边界设计。1.0 小插件不应默认获得整个容器/服务集合或应用级无限扩展面。

这两个来源的**许可**情况在 2026-10-20 之后都通了：Satori（MIT）可照抄，AstrBot（AGPL-3.0）与 Pry 现行许可同族，法律上不再是障碍。但白名单是按项目点名而不是按许可筛的（`docs/upstream-license-compatibility.md` §4.4），作者此刻只点名了 MaiBot/MaiCore、Satori 与 HDS-Interlude，所以 AstrBot 仍然**只可搬机制**——Provider/Schema 转换、能力声明、生命周期分层这些由 Pry 自己写。别把"许可通了"写成"可以抄了"。真正的阻塞点在契约而非许可：协议改动仍先更新 `docs/api-v1.md` 并走 PLAT-005 的冻结节奏，白名单不豁免它。

验证重点：模型超时/fallback、能力不支持、插件失败、资源引用失效；未来接入时再做断线补发、路由认证、RPC 授权与上传链路测试。

## 6. Windows Client / Desktop Agent

**优先顺序**：[Private Companion](PrivateCompanion.md) → [MaiCore](MaiBot_MaiCore.md) 的节奏/输出 → [Satori](Satori.md) 的 Element / Resource；按需读 [Nekro](Nekro-Agent.md) 的 Trace 参考。

重点：用户连续输入、返回/空闲、待发送状态和主动打扰体验；消息与资源引用分离；Developer Mode 需要展示哪些可观察事件；角色在电脑上的存在感。

这些项目没有为 Pry 提供已经验证的完整 Avalonia UI 方案。`desktop_pet_bridge.py` 的存在也不是可直接移植桌宠的证明。Client 应消费 Pry Protocol 和领域结果，状态控制归 Core，不应借 UI 直接修改 Memory / Intelligence / Runtime 内部状态。

验证重点：连续气泡与取消的一致展示、typing/等待/恢复状态、资源失败的可理解反馈、主动消息与用户返回交错。具体 UI 实现仍以 Pry 产品约定为准。

## 7. QA / Eval Agent

**优先顺序**：[MaiCore](MaiBot_MaiCore.md) + [Private Companion](PrivateCompanion.md) → [AstrBot](AstrBot.md) + [Nekro](Nekro-Agent.md) → [Satori](Satori.md)。

重点：把观察转成可复现的 Timing / Behavior / Expression / Memory / Relationship / Capability 场景；区别测试文件存在与实测通过；区分不支持、未审查和缺陷。

| 验证领域 | 主要研究线索 | Pry 应验证的问题 |
|---|---|---|
| Timing / Interruption | MaiCore、Private Companion | 输入未结束是否抢答；旧结果是否发送；主动候选是否过期 |
| Behavior / Expression | MaiCore、Private Companion | 策略与表达是否分离；表达是否适用；学习是否有证据 |
| Memory / Relationship | MaiCore、Nekro、Private Companion | 事实来源、召回相关性、重复写回、用户范围、关系一致性 |
| Capability / Execution | AstrBot、Nekro | 超时、重复调用、错误恢复、补话/取消与副作用 |
| Protocol / Resource（未来） | Satori | resume 实效、重复事件、auth 路由、资源与大包各层失败 |
| Migration / Recovery | Private Companion、MaiCore | 契约兼容、重复回放、恢复后状态与发送行为 |

不要用五项目功能数量作为 Pry 的验收清单，也不要把未复核的历史 Satori 疑点写成对当前上游的已确认漏洞。报告应注明测试版本、输入、期望、结果与未覆盖边界。
