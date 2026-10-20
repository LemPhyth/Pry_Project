# AstrBot：通用 Agent Task / Harness

> **⚖️ 照搬边界（作者 2026-09-21 裁决，`DOC-005`）：本项目只作机制参考，源码文本不进 Pry 仓库。**
> 原因（2026-09-21 当时的口径）：AstrBot 是 **AGPL-3.0**，而作者当时把 Pry 的代码许可定为 **GPL-3.0-only**（理由是"现在不一定需要照抄 AstrBot"，不值得为它承担 AGPL 的网络条款）；AGPL 代码不能放进 GPL-3.0-only 的作品，反方向才合法。
> **这条法律前提已在 2026-10-20 失效**：作者当日把 Pry 代码许可改为 **AGPL-3.0-only**，AGPL 对 AGPL 同族已不构成障碍。但白名单是按项目点名、不按许可筛（`docs/upstream-license-compatibility.md` §4.4），作者此刻只点名了 MaiBot/MaiCore、Satori 与 HDS-Interlude，所以本文件的 §3/§4（Provider 与工具契约分离、分层生命周期、超时与 fallback、渐进式工具披露、大结果外置）仍按**机制**吸收，由 Pry 自己写；**不复制、不逐行翻译其 Python 文本**。将来确实需要抄 AstrBot 代码时，缺的是作者点名，不是再改一次许可证。细则见 `docs/upstream-license-compatibility.md` §3、§4.4。

来源：R1（2026-09-15），见 [README](README.md)。这是原研究默认分支的历史观察，未锁定 commit；联合报告尾部截断，不据缺失内容补写结论。许可实测日期 2026-09-21，`AstrBotDevs/AstrBot`（默认分支 `master`）HEAD `6914bc3aa61e14ca9a9c2cb37a0f9ec1ff5d6334`。

## 1. 定位与核心运行对象

AstrBot 是通用 Agent 应用内核：Platform、Event Pipeline、Plugin、Provider、Conversation、Knowledge Base 与 Agent Runtime 共同组成系统。核心处理单位偏独立 Event / Pipeline Run 和本次 Agent Task；Tool Loop 是 Pipeline 内的一个 Processor。

它有 Persona、Conversation、Skills，也能承载长期陪伴插件；这不代表其原生架构中心与 MaiCore 的长期社交 Runtime 相同。Private Companion 需要单独阅读。

## 2. 历史源码观察：启动与主调用链

`core_lifecycle.py` 中 `AstrBotCoreLifecycle` 是 composition root，持有 Database、ConfigManager、PersonaManager、ProviderManager、PlatformManager、ConversationManager、KnowledgeBaseManager、CronManager、SubagentOrchestrator、Plugin Context/Manager、PipelineSchedulers、EventBus。

```text
Platform Adapter（异步 Task）
→ event_queue
→ EventBus / AstrMessageEvent
→ 根据 unified_msg_origin 选择配置和 PipelineScheduler
→ 独立异步 Pipeline Run
→ WakingCheck → WhitelistCheck → SessionStatusCheck → RateLimit
→ ContentSafety → PreProcess → Process → ResultDecorate → Respond

Process 内：Agent SubStage
→ build_main_agent()
→ ToolLoopAgentRunner / run_agent()
→ LLM → Tool Call → Tool Result → LLM … → 最终响应
```

`PipelineScheduler` 支持普通 async Stage 与 async generator Stage；后者能 yield 后执行下游，再恢复本阶段，形成洋葱式中间件。

## 3. Agent 构建与执行

`astr_main_agent.py` 的 `build_main_agent()` 为本次运行组装配置和工具世界。历史报告列出的配置包括 tool timeout、schema mode、streaming、context compression、KB、file extract、context limit、safety、computer-use runtime、sandbox、cron、fallback providers、subagent orchestrator、timezone。

组装能力包括 Handoff、MCP、Skills、Web Search、KB、Computer、Shell、Python、File、Browser、Cron、Group History、Send Message。清单表示原研究看到的工程能力，不表示每次运行全启用。

`ToolLoopAgentRunner` 在最大步数内循环模型与工具调用；到最后一步会移除工具并要求模型根据已有结果回答。研究还记录：

- Follow-up queue / injection：用户执行中补话可进入当前 Agent Context；另有 FollowUpTicket / Abort Signal。
- 相同工具与参数 streak 检测，重复越多警告越强。
- 大工具结果可落文件，向模型提供 preview 并按需读取。
- Context compression、fallback provider、trace 等工程处理。

这些机制不等同于 Character Runtime 的所有打断语义：注入新消息不能自动证明已发生的工具副作用被撤销。

## 4. Tool、Provider、MCP 与 Skills

`FunctionTool` 以 Name、Description、JSON Schema Parameters、Handler 定义工具。`ToolSet` 转换 OpenAI、Anthropic、Google GenAI 的工具 Schema，避免上层绑定单一模型供应商。

ToolSet 支持 Full、Light（name + description）、Parameter-only 等形式。Runner 的 `skills_like` 模式体现渐进式工具披露：先选工具，再提供所需参数，减少一开始的 Schema 开销。

`mcp_client.py` 的历史观察包含命令 allowlist / denylist、shell 元字符检查、Python `-c` / Node `-e`、Docker 特权与 host namespace 参数检查、env 类型、MCP Schema 验证和部分修复。原报告列出的命令清单是当时实现，不应复制为 Pry 的完整安全策略；这些检查也不构成已完成安全审计的证明。

`astrbot/core/skills/` 导出 `SkillInfo`、`SkillManager`、`build_skills_prompt`，并存在实际 `SKILL.md` 型 Skill。完整 MCP / Skills / Tool Loop 在 Pry 1.0 中不是能力目标。

## 5. 状态、Memory 与平台边界

`ConversationManager` 区分 Session 与 Conversation：一个平台 Session 下可创建、切换、删除多个 Conversation。`unified_msg_origin` 大致表达 `platform:message_type:session_id`。

本次研究涉及 Conversation、KB、Agent Context 和压缩，并未完整审查 AstrBot 原生长期人物记忆、关系学习或表达学习系统。应表述为“这些不是本报告确认的原生核心重点”，不能简单断言 AstrBot 没有长期角色扮演能力。

PlatformManager 动态加载平台 Adapter，各自投递同一事件队列；原报告列有 aiocqhttp、QQ Official、Telegram、Lark、DingTalk、WeCom、Discord、Slack、Satori 等。Satori 在此是一种平台接入，不是整个 AstrBot 的核心领域模型。

插件通过 Plugin Context / Manager 与生命周期进入应用。原研究明确未完整审查插件隔离实现，因此不能宣称它具有或不具有某种完整进程隔离保证。

## 6. 对 Pry 的建议：借鉴与不照搬

**1.0 可借鉴**：Provider 与工具契约分离；输入、业务处理、输出的生命周期分层；模型超时和 fallback；按需上下文构建；受控 Capability 的 Name / Schema / Handler 概念。

**不要照搬**：完整 Tool Loop、Computer Use、Shell、Browser、Cron、SubAgent、MCP 平台；以 Platform Session 作为 Pry 全局身份根；让每条事件必然成为独立聊天任务；巨大 Agent 构建器承载所有 Character 状态。

Pry 的简单“打开应用”需求不要求先建完整 Harness。Character Runtime 应保有何时回复、何时取消、如何交付的控制权。

**未来参考**：可选 Advanced Harness 插件、渐进式工具披露、大结果外置、工具重复检测、MCP 接入验证、Follow-up / Abort 与执行边界。独立决定扩展范围，不将这些基础设施提前塞入 1.0。

## 7. Caveat 与验证重点

- 原报告为历史主链路研究；没有当前版本、性能、安全性或插件隔离的完整证明。
- 主报告读取尾部截断；本文件使用已读取的 AstrBot 架构、执行和对照章节。
- Token 优化机制的存在不证明在 Pry 的小模型上有同等效果，需实测。
- 后续应验证工具超时、重复调用、超大结果、fallback、用户补话/中止及已发生副作用的处理。
- 跨平台统一身份、多 Core 同步、Character 关系连续性仍是 Pry 自己的领域设计。
