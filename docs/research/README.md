# Pry 开源项目源码研究资料包

整理日期：2026-09-20。用途：供 Pry 的 Architecture / Runtime / Intelligence / Memory / Platform / Client / QA 七个 Agent 阅读、讨论架构和制定验证任务。

## 资料性质与证据边界

本包整理自原对话《生成全栈工程师提示词》中 2026-09-15 至 2026-09-17 已完成的源码研究及补充分析。本次工作是研究成果归档，没有重新拉取或审计上游代码，也没有用新的网络资料补写源码结论。

原对话标识：`6aa4250e-1914-83e8-97bb-6526d00cdc2a`。

文档统一区分：

- **历史源码观察**：原研究明确报告在当时源码中看到的调用链、模块和字段。它们不是本次重新验证的当前版本事实。
- **Pry 设计建议**：基于研究对 Pry 的取舍，不意味着上游已经按此实现，也不是自动获准的开发任务。
- **Caveat / 待验证**：原研究的疑点、审查范围限制，以及缺失的版本证据。未验证不等于功能不存在。

原记录没有保存五个项目的 commit SHA、完整检出快照或可复现审计日志。符号和路径只能作为后续定位线索，不能保证最新版本仍相同。MaiBot/AstrBot 联合报告在本次读取时有 20,000 字符上限，文本末尾截断于 Pry 跨会话关系讨论；本包只整理已读取部分，不补写缺失结论。其他主要研究报告及 Satori 两份补充记录均已读到报告末尾。

“已确认”在本包中表示**原研究报告明确确认**，不表示本次完成了独立源码复核。当前维护状态、当前漏洞状态及生产可用性未在本次确认。**许可证例外**：`DOC-005` 已于 2026-09-21 逐个实测五个项目的 LICENSE 并记录 HEAD SHA，结论见下方"Pry 的照搬边界"与 `docs/upstream-license-compatibility.md` §1；除此之外本包仍不声称复核过上游现状。

## 文档索引

| 文档 | 核心定位 | 主要用途 |
|---|---|---|
| [MaiBot_MaiCore.md](MaiBot_MaiCore.md) | 长期社交 / Character Runtime | 会话调度、行为、表达、人物记忆 |
| [AstrBot.md](AstrBot.md) | 通用 Agent Task / Harness | Provider、Pipeline、Tool Loop、上下文与插件工程 |
| [Satori.md](Satori.md) | 跨平台 Bot Protocol / Remote Bot abstraction | 未来外部通信、富消息、资源与连接边界 |
| [Nekro-Agent.md](Nekro-Agent.md) | Code-as-Action + Sandbox/RPC | 异步记忆调度、执行记录、未来高级能力 |
| [PrivateCompanion.md](PrivateCompanion.md) | AstrBot-native 长期陪伴 / 生活模拟层 | 私聊、主动消息终审、表达约束、Memory Contract |
| [COMPETITIVE_MATRIX.md](COMPETITIVE_MATRIX.md) | 五项目横向对照 | 防止把不同层次的系统视作替代品 |
| [AGENT_REFERENCE_GUIDE.md](AGENT_REFERENCE_GUIDE.md) | 七个 Agent 阅读指南 | 阅读顺序、职责与验证重点 |

## Pry 的照搬边界（作者 2026-09-21 裁决设立，2026-10-20 追加一家，`DOC-005`）

研究资料的阅读不等于代码可以取用。作者 2026-09-21 把 Pry 的**代码与文档许可改为 GPL-3.0-only**（原 Apache-2.0），**2026-10-20 再改为 AGPL-3.0-only**（逐字："将许可证变更为AGPL-3.0，我需要借鉴HDS项目的架构组织方式"），并把研究对象按"法律上能不能把源码文本搬进仓库"分成两档。**这是硬边界，任何 Agent 不得自行放宽**；完整依据、许可实测 SHA 与执行细则见 `docs/upstream-license-compatibility.md`。

| 上游 | 许可（实测日期见括号） | 照搬 | 说明 |
|---|---|---|---|
| MaiBot / MaiCore | GPL-3.0（2026-09-21） | ✅ 允许 | Runtime 的规范目标。作者原话：不照抄就得自研一套 Runtime，极其拖慢进度。GPL-3.0 代码进 AGPL 作品走 §13，方向合法。 |
| Satori | MIT（2026-09-21） | ✅ 允许 | 法律无障碍；但 1.0 只吸收既定机制，不把上游类型变成 Pry 契约。 |
| HDS-Interlude | AGPL-3.0（2026-10-20，与 FSF 官方文本逐字节相同、无附加条款） | ✅ 允许（**作者 10-20 点名**） | 点名的范围是"架构组织方式"；它不在 1.0 冻结范围内新增功能，也不许把上游 Prompt 正文、人设或示例搬进来。 |
| AstrBot | AGPL-3.0（2026-09-21） | ❌ 禁止 | 10-20 的改约**已经解除它的法律阻塞**，但作者没有把它加进名单 ⇒ 照旧只搬机制。 |
| Nekro-Agent | 自定义"Nekro Agent 开源协议 V1.1"（2026-09-21） | ❌ 永久禁止 | 该协议把"设计、架构或概念"也算派生作品，并有强制署名与规模授权条款。改约救不回来。 |
| Private Companion | 无 LICENSE 文件（2026-09-21） | ❌ 永久禁止 | 无许可即保留所有权利，任何复制都无授权。 |
| qq-bridge | MIT 正文＋一节第三方声明，GitHub 判 `NOASSERTION`（2026-10-20） | ❌ 禁止 | 作者从未点名；且它不是"一眼干净的 MIT"，不得按 MIT 推论放行。 |

因此本包各文档的使用方式随之改变：**"即使原封不动照搬项目也没关系"这句话现在只对 MaiBot、Satori 与 HDS-Interlude 成立**。白名单外的四份文档仍是必读的架构与 Caveat 来源，但它们的价值在**机制与结论**，不在代码文本——不 clone、不 vendor、不把源码文件复制进仓库或任何 worktree，也不把上游源码文本当作参考实现来"看一遍再自己写"（临时检出到仓库外的临时目录可以，读完要在交接记录里声明）。换语言重写**不**解除 copyleft 义务，所以禁止名单项目连这条路都不通。

每次照搬必须三处留痕，缺一不进集成基线：文件头 `Ported from <repo>@<sha>:<path>` 并保留上游版权行；上游许可文本进 `licenses/upstream/<repo>_<sha>/`；`licenses/THIRD-PARTY-NOTICES.md` 的照搬来源表加一行。抄 MaiBot 前额外要查目标子目录自己的 LICENSE——上游 EULA §2.1 明示子目录可能另属他人许可。

美术侧完全不受本次改约影响：`ASSETS_LICENSE.md` 与 `FAN_CONTENT_POLICY.md` 的条款不变，原创素材不适用代码许可，也不得被"本程序适用 GPL"吞掉；上游的美术资源、Prompt 文案与示例数据一律不取。

## Pry 的使用边界

Pry 当前方向是 **Local-first / Private-first / Desktop-native Character Runtime**。1.0 聚焦 Windows 本地 Client + Core、角色连续性、对话节奏、Behavior / Expression、长期记忆及简单本地 Capability、小插件。

研究不意味着把五个项目拼成一个底座：MaiCore 是核心行为与调度参考；AstrBot 是工程参考；Satori 是未来外部通信参考；Nekro 是记忆工程和未来高级执行参考；Private Companion 是陪伴产品机制及架构边界参考。

Satori Gateway、QQ / Telegram、Hosted Core、Multi-Core、完整 MCP Host、Tool Loop、Code Sandbox、SubAgent、完整生活模拟均不能仅因出现在资料中就进入 Pry 1.0。

原对话早期曾讨论多 Core / 网络作为较高优先级方向，后续已收敛到本地 1.0。本包采用后续范围约定；历史方案不视作当前实施要求。旧报告中的“Pry 内部 MaiCore”表示 MaiCore-like 的设计参考，不代表已决定嵌入上游 MaiCore。

## 安装与维护

ZIP 内含完整 `docs/research/` 层级，解压到 Pry 仓库根目录即可；也可将这八个 Markdown 文件直接复制到现有 `docs/research/`。

建议先读矩阵，再按 Agent 指南阅读。将研究用于 ADR 时注明对应研究章节，分清观察与建议。后续真正依赖上游接口前，补充仓库地址、commit SHA、文件/符号、复现结果及日期。发现版本变化时追加复核记录，不把历史疑点静默改成确定结论。

## 原研究记录索引

日期按原记录 UTC 日期列出，本包不将其解释为上游版本发布日期。

| 代号 | 日期 | 内容 | 原对话 turn ID |
|---|---|---|---|
| R1 | 2026-09-15 | MaiBot / MaiCore 与 AstrBot 联合源码报告 | `ef19c1ee-e4b9-43d8-8cc1-126e8b1aa81c` |
| R2 | 2026-09-15 | Satori 主研究 | `cce28001-384b-4641-9a80-aeb9bc0273bd` |
| R3 | 2026-09-16 | Satori 图片传输补充 | `e07a5e88-4c02-4a29-9bd3-098acbb0104e` |
| R4 | 2026-09-16 | Satori 大包排障分析 | `3e7a0a94-d088-4385-ad32-febadd02ef7b` |
| R5 | 2026-09-17 | Nekro-Agent 源码研究 | `06c887d4-7af0-43b5-85d1-cc107876cbb2` |
| R6 | 2026-09-17 | Private Companion 源码研究 | `7d0d47b9-41c0-47a8-ac31-1958ea9e4618` |

R4 包含基于链路的排障推断，不能当作已复现的大包故障。各文档的测试建议也不是上游已通过的测试结果。
