# 五项目横向研究矩阵

来源与时间边界见 [README](README.md)。此表比较的是原源码研究确认的架构重心，**不是功能排名或当前版本能力普查**。“非核心”不代表没有功能，“未审查”不代表不存在。

## 核心架构比较

| 项目 | 核心运行对象 | LLM 角色 | 行动方式 | Memory | 平台层 | Character / Roleplay 深度 | 对 Pry 的主要价值 |
|---|---|---|---|---|---|---|---|
| [MaiBot / MaiCore](MaiBot_MaiCore.md) | 长寿命 Session / Social Runtime | Planner 形成互动行为与意图；Replyer 生成表达；后台学习 | Pending → Scheduler → Gate → Planner / Tools → Replyer | A_Memorix、人物事实异步写回；行为后果、表达经验 | PlatformIO Broker：路由、去重、出站跟踪、Driver | 架构中心；行为、表达、节奏与社交连续性投入深 | Pry Character Runtime 的主要参考 |
| [AstrBot](AstrBot.md) | Event / Pipeline Run、通用 Agent Task / Harness | 规划、Function Calling、结果汇总；由 Harness 管理上下文 | Event Pipeline + Tool Loop | Conversation、KB、Agent Context / Compression；本轮未完整审查原生角色记忆 | PlatformManager + event_queue + EventBus，多 Adapter | 有 Persona / Skills，可由插件增强；长期社交成长不是本报告确认的原生中心 | Provider / Tool 契约、Pipeline、上下文与异常处理 |
| [Satori](Satori.md) | Bot / Session / Login、Remote Bot Proxy | 不负责 LLM | HTTP Command + WebSocket Event；平台 Encoder | 不承担角色长期记忆；event buffer 是传输机制，不能等同 Memory | 协议、Element、Core、Server、Adapters；跨进程 Bot 抽象 | 不承担角色决策或成长 | 未来外部通信、能力发现、富消息与资源引用 |
| [Nekro-Agent](Nekro-Agent.md) | Code-execution Agent Job | 编写 Python 程序，结合执行结果再规划 | Docker Sandbox + Plugin RPC；以整段程序为执行步 | 异步 Consolidation、语义段落、Episode、Entity/Relation、多路召回 | BaseAdapter + 内部统一消息 / chat_key | 有 Persona / emotion 扩展，但架构中心在任务执行 | Memory Scheduler / Consolidator、执行记录、未来高级代码能力 |
| [Private Companion](PrivateCompanion.md) | AstrBot 生命周期中的陪伴状态 / 策略层 | 在陪伴上下文中生成、学习表达、参与主动消息评审等 | AstrBot Hooks + Debounce + Proactive Candidate / Review；借宿主执行 | UserMemory + 版本化 Memory Companion Bridge；外部记忆算法未完整审查 | 依赖 AstrBot 平台与请求类型 | 长期私聊、关系、表达学习、生活模拟投入深；不是 MaiCore Runtime | 主动终审、表达约束、私聊体验、Memory Contract 与超级插件警示 |

## 相似表象背后的差异

| 表面上相似的能力 | 必须保留的差异 |
|---|---|
| 都可以接收消息 | MaiCore 更新持久 Runtime；AstrBot 进入 Pipeline；Nekro 调度 Job；Private Companion 在宿主阶段控制请求；Satori 搬运并抽象事件 |
| 都出现防抖/排队 | MaiCore 的 Timing 是社交策略组成；Nekro 偏任务合并；Private Companion 借等待 LLM 的 Hook 延迟请求，不能视作同一种 Runtime |
| 都能让模型行动 | AstrBot 主要以结构化 Tool Call 交给 Harness；Nekro 让模型编写程序再执行；MaiCore 的行动服务于社交角色 |
| 都可用于“猫娘/角色” | Persona 支持、生活模拟、表达学习、持久社交调度是不同层次，不能用一项功能名称互相替代 |
| 都有 Adapter | AstrBot / Nekro 主要是内部平台抽象；Satori 的突出特征是远程 Bot 标准化；MaiCore 还有 IO Broker 的路由/去重/跟踪 |
| 都有状态或历史 | Conversation Context、长期人物事实、行为经验、Episode、协议 event buffer 语义不同，不能统称一套 Memory |
| Private Companion 很像 MaiBot | 产品能力有重合，但它深度 Hook AstrBot，不是已移植 MaiCore 的架构证据 |

## 照搬许可边界（作者 2026-09-21 裁决，`DOC-005`）

下表区分两件常被混为一谈的事：**法律上能不能把上游源码文本搬进仓库**，与**范围上该不该搬**。下面这栏只有前者；"Pry 阶段取舍"里的"不应照搬"仍是后者，两栏都要满足。Pry 代码许可已由 Apache-2.0 改为 **GPL-3.0-only**，依据与实测 SHA 见 `docs/upstream-license-compatibility.md`。

| 项目 | 许可（2026-09-21 实测） | 源码文本进仓库 | 约束 |
|---|---|---|---|
| MaiBot / MaiCore | GPL-3.0 | ✅ 允许 | 同族兼容；Runtime 的规范目标。抄前先查目标子目录自己的 LICENSE（上游 EULA §2.1）。 |
| Satori | MIT | ✅ 允许 | MIT 可进 GPL；保留上游版权行。 |
| AstrBot | AGPL-3.0 | ❌ 禁止 | AGPL 代码不能并入 GPL-3.0-only 作品；作者判定不值得为其转 AGPL。只搬机制。 |
| Nekro-Agent | 自定义"Nekro Agent 开源协议 V1.1" | ❌ 永久禁止 | 把"设计、架构或概念"也算派生作品，另有强制 GUI 署名与用户规模授权门槛。 |
| Private Companion | 无 LICENSE 文件 | ❌ 永久禁止 | 无许可即保留所有权利。 |

白名单外三项"只搬机制"的可执行含义：不 clone、不 vendor、不把源码复制进仓库或任何 worktree，也不把上游源码文本当参考实现读完再自己写；**换语言重写不解除 copyleft 义务**，所以 Python/TS → C# 这条路对禁止名单同样不通。上游的美术资源、Prompt 文案与示例数据一律不取。照搬白名单项必须三处留痕：文件头 `Ported from <repo>@<sha>:<path>`、`licenses/upstream/<repo>_<sha>/` 的上游许可文本、`licenses/THIRD-PARTY-NOTICES.md` 照搬来源表一行。

## Pry 阶段取舍

| 项目 | 1.0 吸收方向 | 未来阶段 | 不应照搬 |
|---|---|---|---|
| MaiCore | Runtime、Timing/Gate、中断、Behavior/Expression 分离、事实证据与异步写回 | 更成熟学习机制、独立插件 Runner | 群聊参数、全套学习/子代理及上游内部类型 |
| AstrBot | Provider/Capability 契约、分层生命周期、上下文与错误处理思想 | 可选完整 Harness / MCP / Skills | 全能力 Agent 平台作为 1.0 主体 |
| Satori | 消息/资源分离和边界意识 | External Messenger / Gateway；接入前复核版本、auth、resume | 协议类型进入 Domain；把 sn/token 当可靠性/账号体系 |
| Nekro | 自适应异步记忆沉淀、可观察执行记录 | 可选 Code Agent / Sandbox 插件、复杂语义/图谱记忆 | Core 强依赖 Docker/Python、动态装包与整个插件开放面 |
| Private Companion | 私聊连续输入、主动发送前终审、表达适用约束、Memory 接口 | 精细 Expression Gate、选择性生活状态扩展 | 巨型 Mixin/Hook、全套生活模拟、功能数量竞赛 |

“吸收方向”不是要求 1.0 全部照实现。原会话已明确优先让本地桌面角色持续、自然地交流，简单 Capability 与小插件不能被研究资料自动扩展成通用 Agent 平台。

## Caveat 对照

| 项目 | 已知限制或待复核事项 |
|---|---|
| 全部 | 原研究无 commit SHA，本次未重审源码；不能宣称当前状态或生产保证。许可已另于 2026-09-21 实测（见上文"照搬许可边界"），照搬前仍需复核该 SHA 之后上游是否改约 |
| MaiCore | 主干研究；中断/淘汰机制不等于所有竞态和恢复路径已验证 |
| AstrBot | 插件隔离未完整审查；联合报告尾部读取截断；MCP 检查不是安全性证明 |
| Satori | 历史 alpha；resume buffer 写入未找到；internal/proxy 路由未见 checkAuth 调用；无多租户完整证明；官方 QQ 为 Open Platform，不是个人 QQ |
| Nekro | Sandbox / Token / RPC 机制已观察，安全性未全面审计；小模型代码可靠性未实测 |
| Private Companion | 宿主内部类型与 Hook 耦合；Memory Bridge 不代表已完整研究另一个 Memory 插件；测试存在不等于本次运行通过 |

五者能在不同层次提供参考，但不能据此得出“五选一即可构建 Pry”或“集成全部就等于 Pry”的结论。
