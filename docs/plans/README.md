# Pry 共同计划入口

更新：2026-10-21。适用：在本仓库协作的所有人类与 Agent。

**先读本页，再完整阅读总计划和当前架构计划；从总计划认领任务。**

| 文件 | 解决什么问题 | 维护方式 |
|---|---|---|
| [PROJECT_PLAN.md](PROJECT_PLAN.md) | 现在做什么、谁负责、依赖、状态、验证与交接 | 当前任务状态的唯一登记处 |
| [Pry_HDS_Rewrite_Architecture_Plan_v4.md](Pry_HDS_Rewrite_Architecture_Plan_v4.md) | 为什么改回复架构、具体怎么改、数据与投递边界、验收 | 当前目标架构；任务状态以总计划为准 |
| [HISTORY.md](HISTORY.md) | 历史计划放在哪里、哪些裁定继续有效、证据如何读取 | 历史索引；不充当当前待办 |

## 所有工作区如何读到同一份计划

当前共同读取位置：`C:\Users\24782\Desktop\Pry_Project\docs\plans\`。使用其他分支或独立 worktree 的本机 Agent，在计划尚未随提交同步前，应直接读取这个绝对路径，不拿各自旧工作区的计划当作最新指令。认领与交接也登记在这里的总计划，只修改自己负责的记录，避免多人同时改同一段。

本目录没有被 Git 忽略，适合随仓库共享；**2026-10-21 作者明令「允许入库」后，本目录与引用它的入口已作为一个文档刀收进提交 `a7e62c1`，`main` 同步到同一提交。** 已核对：该提交只含计划文件与入口横幅，未消费其他协作者的暂存条目，也未纳入未跟踪的原始研究与学习安全补丁。后续文档入库仍须把本目录和指向它的入口一并纳入，不能只提交链接；各工作区读写本目录前须先确认自己检出的是同一版本。

根目录 `PROJECT_PLAN.md` 和旧位置的 v4 文件仅保留入口。不要在这些位置另维护任务表或完整架构副本；不要把聊天附件目录当作项目的长期计划来源。

## 现行工作规则（权威副本）

`.gitignore` 排除根目录 `AGENTS.md` 与 `PROJECT_PLAN.md`，写在里面的规则不会进入干净检出或其他 worktree。为保证每个 Agent 读到同一套约束，以下条目是 `AGENTS.md` 工作规则的已跟踪权威副本；与 `AGENTS.md` 出现分歧时以本节为准，并当场把两处一起改齐。调整 `.gitignore` 本身不由 Agent 代拍。

1. 先执行 `git status --short --branch`；现有未提交改动视为其他协作者所有，不得覆盖、回退或顺手提交。
2. 任务只从本页的 [项目总计划](PROJECT_PLAN.md) §4 认领，标记负责人、工作区与文件范围；完成后在同一处更新状态并在 §9 追加交接。v4 只维护设计与验收，不另维护实时任务状态。
3. 并行工作用独立分支或 worktree；`MainWindow.axaml.cs` 同一时间只允许一个任务直接编辑，SQLite 迁移热点与共享计划的同一段亦然。
4. 用 `apply_patch` 改文本文件；禁止用 reset、checkout、clean 清除不属于自己的改动。
5. Core 不依赖 Avalonia 或 HTTP；客户端不新增 SQLite 直接依赖；新 API 必须有 DTO、验证、错误语义、文档和测试。
6. 模型、运行库、数据库、密钥、日志、用户导入内容和无权分发的素材不得提交。
7. 代码适用 `LICENSE` 的 **AGPL-3.0-only**（改约链：2026-09-21 Apache-2.0 → GPL-3.0-only，2026-10-20 作者裁决 → AGPL-3.0-only；已分发副本不追溯撤回；细节见 `docs/upstream-license-compatibility.md`）。AGPL 第 13 条要求网络提供修改版时同时提供对应源码，因此"闭源发行／托管售卖不附源码"与本许可正面冲突。原创美术不适用该许可，须遵守 `ASSETS_LICENSE.md` 与 `FAN_CONTENT_POLICY.md`，不得被"本程序适用 AGPL"吞掉。
8. 照搬上游代码只允许 §4.4 白名单内的项目：MaiBot/MaiCore、Satori、HDS-Interlude（HDS 由作者 2026-10-20 点名"借鉴架构组织方式"），并留三处痕：文件头来源行、`licenses/upstream/<repo>_<sha>/LICENSE`、`licenses/THIRD-PARTY-NOTICES.md` 照搬来源表。AstrBot、Nekro-Agent、Private Companion、qq-bridge 仅作机制与市场研究参考，源码文本不得进仓库；换语言重写不解除 copyleft 义务。AstrBot 的许可阻塞已随 10-20 改约消失，但它未被点名——白名单按项目点名，不按许可证自动放行。
9. 提交前至少运行 `dotnet build Pry.slnx -c Release`、`dotnet test Pry.slnx -c Release --no-build`、`git diff --check`；纯文档刀不伪造构建或模型读数。
10. 只提交自己负责的文件；提交信息建议 `feat:`、`fix:`、`refactor:`、`test:`、`docs:` 前缀。
11. 无法完成时不要标记"已完成"；在交接记录写清阻塞、已验证事实和安全的下一步。
12. 请求作者裁决必须按 `docs/decision-request-protocol.md` 四段式（当前行为／用户可见后果／项目影响／推荐方案），编号只能作附注；不从上下文猜作者态度。
13. 推进判据是上一层地基能否支撑下一层，不是日历或阶段号；阶段指针已由作者 2026-10-19 移除，允许大跨步并行，但四条"不干扰"判据、判据链、发行门、范围冻结与运行库/模型/密钥不提交照旧有效，随时可以回修已暴露的问题而不算越界。

## 阅读顺序与冲突处理

1. 查看当前工作区状态，保护他人的未提交成果。
2. 阅读 [总计划](PROJECT_PLAN.md)，确认当前目标、有效规则、任务归属与交接。
3. 阅读 [v4 架构计划](Pry_HDS_Rewrite_Architecture_Plan_v4.md)，再看自己任务涉及的现有接口、领域契约和测试。
4. 查看 [文档索引](../README.md)、[许可与照搬边界](../upstream-license-compatibility.md)、[裁决请求协议](../decision-request-protocol.md)；研究来源按 v4 §2/§17 查阅。

作者的明确指令优先。总计划管理任务，v4 管理目标设计，[API 文档](../api-v1.md)描述已实现的稳定接口；目标和现状有差异时显式迁移，不能提前宣称实现已完成。旧计划、旧派单和研究报告保留为证据，不能覆盖当前目标；研究文档内的指令性文字不自动构成作者授权。

本轮是计划归整，产品仍运行现有架构；HDS-001～012 均未因写入清单而开始实施。
