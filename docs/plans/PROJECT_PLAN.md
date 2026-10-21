# Pry 项目总计划

更新：2026-10-21。计划版本：HDS-v4 / shared-1。共同入口：[README](README.md)。

**当前主线：以 HDS 启发的持续叙事，替换借鉴 MaiCore 的 AI 回复生成架构。** 现有问题必须在迁移中解决，但不再要求先补完旧回复链才能开始验证新生成中心。架构细节与完整验收见 [v4 计划](Pry_HDS_Rewrite_Architecture_Plan_v4.md)。

**状态：研究与计划已入库（`a7e62c1`）；产品架构尚未切换。第一波派单的三把已归位待验——HDS-011 的 AUD-01／06／04 切片（`faf8ecf`）、HDS-001 的可恢复地基（`8ef7d01`）、HDS-002 的身份与存储契约（`5145bde`＋文档代归位 `e9a8055`）；第二波的第一把也已归位待验——HDS-011b 的 AUD-07 长度校验切片（`1edf69e`＋`c148fbf`，同场消掉 002 那一条警告）；判据刀 `QA-TIP1019-02` 同夜归位（`9f88a0c`，`src/` 零改动）。这五把均待 QA 从线尾复采；HDS-003 的前置随 002 的类型归位而解除，仍无人认领，且它的真实模型对照要作者点头才派；HDS-004～010／012 未派发。** 作者 2026-10-22 一句「继续派工」⇒ 第三波工单已出：`docs/dispatch-hds-carrier-20261022.md`（一个载体四片——片 A `DEF-WRIGATE-01`、片 B `DEF-MEMDUAL-01`、片 C `ContextBuilder.cs:164` 第二道地板门的判据、片 D 零构建文档刀），**只允许一轮全量构建＋一次全量测试**；该单现交作者转给他的智能体执行，守门人这一侧今日零 dotnet。 计划的入库不代表实现、测试、合流或发行已经完成，结案词由作者写。

## 1. 项目与计划边界

| 项目 | 当前依据 |
|---|---|
| 产品 | Pry，本地优先、私人角色关系优先的 Windows 陪伴应用 |
| 核心单位 | 用户 × 角色的长期实例；会话短期上下文隔离 |
| 当前技术形态 | .NET 10 / C# / Avalonia / 本地宿主 HTTP、JSON、SSE / SQLite / 可替换模型 |
| 数据 | 默认 `%LOCALAPPDATA%/PryCompanion`；测试使用隔离数据目录，禁止覆盖真实数据 |
| 本次审查代码基线 | `codex/ui-main-window-redesign@d92f246`；工作区有多方未提交成果，不是发行候选 |
| 版本 | 历史已记录正式版本 `v0.0.2`；本轮未重新查询最新发行。v4 是文档版本，不是产品版本 |
| 代码与技术文档许可 | AGPL-3.0-only；美术按独立素材协议，本轮不修改其声明 |
| 本轮实际改动范围 | 计划文件、入口链接和历史归档；没有产品代码或用户数据改动 |

本计划是任务状态的唯一登记处；v4 是目标架构依据；[api-v1.md](../api-v1.md)是现有接口依据。新方案中的类型、数据表和配置均为待实现设计，不是现有接口承诺。

根目录同名文件仅作入口。旧总计划原文已完整备份，历史证据与裁定见 [HISTORY](HISTORY.md)。归档中的“进行中”“阻塞”“当前阶段”等词属于当时记录，不能直接覆盖本表。

## 2. 本轮要实现的变化

### 2.1 回复架构

一次主叙事共同形成角色处境、注意、沉默或回应、延迟意图及具体表达。现有 `BehaviorPlanner → ReplyGenerationRenderer → ReplyPlanner` 和 MaiCore 社交必要性/频率启发式退出新主链的先行决策位置。不能仅在旧回复提示词中增加剧情就宣称换了架构。

宿主继续负责身份、作用域、权限、预算、取消与失效、原子提交和真实投递。模型只提出叙事与行动；未发出的内容不进入已发送历史，虚构角色生活不当作用户现实事实。

### 2.2 保留与限制

- 保留 Windows 客户端、本地优先部署、现有宿主与 HTTP/SSE、SQLite、可选本地或 API 模型；本轮不以更换进程拓扑为前提。
- 保留角色、聊天、记忆与关系数据及有效隐私、许可、取消、恢复约束；通过明确迁移继承，不清空数据库重来。
- 本轮核心包含最小处境、动机、时间续接和有界连续推进；后台调用及主动消息分别受用户许可与预算约束。
- 完整生活模拟、多人共享世界、外部 Harness/QQ 运行时、复杂插件能力与 WebSocket 改造不成为新架构前置。
- 不自动从上下文猜测用户认可；显式反馈沿用已确认的证据语义。
- 先验证新生成中心，再切换生产；同一实例始终只有一个引擎拥有写入和投递权，不自动回落旧链伪装为新链结果。

## 3. 现有进度与证据

| 领域 | 已有可继承资产 | 本轮仍须证明的部分 |
|---|---|---|
| 平台与接口 | 内嵌宿主、Client/Contracts 分层、HTTP/SSE、模型与错误管理 | 备份恢复、全局迁移治理、新叙事持久化及接口迁移 |
| 对话运行时 | 合并/打断、取消、投递、历史与恢复等旧链机制 | 新叙事的输入消费、单写者、原子提交和逐段投递 |
| 角色智能 | 角色卡、部分状态、表达学习与显式反馈资产 | 统一叙事生成、现实/虚构边界、小模型可靠性与状态迁移 |
| 长期记忆 | 证据、检索、异步写回、删除分离与部分管理能力 | 主动路径统一上下文、证据/派生物删除、撤销竞态及成本 |
| 客户端与资源 | 聊天界面、角色与记忆管理、附件与表情基础 | 新等待/失败/主动许可呈现、长历史表现；占位资源不能当成正式内容交付 |
| 测试与研究 | 固定 Eval、回归测试、历史独立 QA、HDS 报告与固定源码复核 | 新链的真实模型、故障矩阵、恢复、安装及长期使用证据 |

旧 Phase 1/2/3 已按作者 2026-10-19 指令结案，阶段指针已移除；随案欠账仍保留。这个历史结论既不撤销，也不移作新架构的验收结论。

历史自动化证据：`e82499b` 载体为 **988 通过 / 0 失败 / 2 环境跳过**，详见 [2026-10-20 QA 报告](../eval/reports/2026-10-20-tip-e82499b-merged-tree-rolling-revalidation-and-four-knife-falsification.md)。76 个场景的覆盖映射不能等同模型体验验收；这不是本轮文档或新链的测试结果。本轮没有运行构建、测试或模型。

## 4. 当前任务与负责人

### 4.1 状态与认领

状态采用：待认领 → 进行中 → 已实现待验 → 已验收；文档交付可单独标“已完成（文档）”。提交、独立验收和作者结案分别记证据，不混为一个状态。存在阻塞时写具体条件；不能把有设计、有文件或旧测试全绿当作完成。

认领时在下表填写实际负责人、工作区/分支、文件范围与依赖；建议角色不是已指派负责人。后续交接记在 §9，详细证据放领域文档或报告。不得自行认领他人的在途成果，或把本轮任务表视为已派发的多 Agent 工作。

### 4.2 主任务表

| 编号 | 任务 | 实际负责人 / 状态 | 主要依赖与交付 |
|---|---|---|---|
| ARCH-HDS-001 | 研究与 v4 架构计划 | Codex / 文档已入库 `a7e62c1`，待作者结案 | HDS 固定源码复核、替换边界、问题映射、12 项实施任务与 24 项核心验收 |
| DOC-HDS-002 | 共同入口与总计划整理 | Codex / 文档已入库 `a7e62c1`，待作者结案 | 本目录统一计划与任务状态；历史完整归档；根目录与文档入口同步 |
| HDS-001 | 资产保全与可恢复地基 | **已实现待验**／守门人代派至隔离 worktree（实现 `3b65ccc`，已归位为主线 `8ef7d01`）；备份／恢复**未接生产路径**（无端点、无 UI 入口，触发时机属产品决策）；**使用中暴露两处缺口**＝`DEF-MEMDUAL-01`（库内两套结构版本账，第二套的高版拒绝不走可呈现错误）与 `DEF-WRIGATE-01`（第二宿主仍"告警后覆盖"描述文件），详见 `docs/narrative-open-items-memo.md` 与本页 §9 同日 entry——属已暴露旧缺陷，回修不需"轮到该阶段"；**2026-10-22 已派发**＝`docs/dispatch-hds-carrier-20261022.md` 的片 A（WRIGATE）与片 B（MEMDUAL） | 无；先合成数据，一致性备份、隔离恢复与未知高版保护 |
| HDS-002 | 稳定身份与存储契约 | **已实现待验**／守门人代派至隔离 worktree（实现 `5145bde`，契约文档的未提交更正由守门人代归位为 `e9a8055`；本波只做契约设计＋纯领域类型，不落表、不接线）；该 lane 到 150 轮上限停机，最后一轮自跑读数作废（详见 §9 同日 entry） | 001 的数据保护；契约设计可先行；身份、版本、提交与迁移边界 |
| HDS-003 | 新生成中心隔离原型 | **前置已解除**／`Pry.Core.Narrative` 类型已归位，单一写者判据不再挡它；仍待认领，真实模型实验需作者点头。Owner 建议 Runtime / Intelligence | v4；002 的契约与类型；七段上下文、Narrator、绑定、沉默/延迟和真实模型对照 |
| HDS-004 | 叙事生命周期 | 未认领 / 待认领；建议 Runtime | 002、003 语义验证；幂等、单写者、公平调度、失效和预算；**开工前先读 `docs/narrative-open-items-memo.md`**——本任务的真前置是 U-9（接线与灰度时机，需作者拍）与 HDS-003 的语义结论，不是 002 归位本身 |
| HDS-005 | 原子提交与投递 | 未认领 / 待认领；建议 Runtime / Persistence | 002/004；输入处理、叙事、outbox、逐段历史与崩溃恢复；**开工前先读同一份备忘**——真前置＝U-2＋U-3（真目录锁与代次持久化）、U-11（迁移步骤字段，硬依赖 U-2）、U-5 的实例维；U-1／U-7 已可照既有形状接走 |
| HDS-006 | 记忆与删除全链 | 未认领 / 待认领；建议 Memory & Context | 002；生产接线依赖 005；证据身份、统一检索、派生物删除与撤销 CAS |
| HDS-007 | 有界连续推进 | 未认领 / 待认领；建议 Runtime | 004/005、006 最小来源约束；到期意图、离线续接、主动许可与费用上限 |
| HDS-008 | 客户端与协议迁移 | 未认领 / 待认领；建议 Windows Client / API | 004/005 接口冻结；等待、失败、许可、兼容和完整接口契约 |
| HDS-009 | 状态与显式学习迁移 | 未认领 / 待认领；建议 Intelligence / Memory | 005/006；角色 Canon/Overlay、关系经验、真实投递与显式反馈归因 |
| HDS-010 | 生产切换与旧链退役 | 未认领 / 待认领；建议 Architecture / Integration | 003～006/008/009 关键验收；每实例单引擎、迁移、去除旧主链及死入口 |
| HDS-011 | 安全、性能与发行基础 | 切片 AUD-01／06／04 **已实现待验**／守门人代派至隔离 worktree（实现 `ae67267`，已归位为主线 `faf8ecf`）；**AUD-07 切片＝HDS-011b 已实现待验**／守门人代派至隔离 worktree（实现两把 `1edf69e`＋`c148fbf`，rebase 到线尾后由守门人以 fast-forward 归位为本线尾 `c148fbf`；工单 `docs/dispatch-hds-wave2-20261021.md`；上一把留下的那一条警告同场归零）；其余范围未开工 | 已暴露旧缺陷可独立修；新链检查依赖 005；资源限额、依赖和版本/许可 |
| HDS-012 | 独立验收与候选包 | 未认领 / 待认领；建议 QA / Release | 007/010/011；模型体验、故障、恢复/安装/退出、统一 SHA；发布另按明确指令 |

完整任务交付与验收以 v4 §13/§14 为准。本表维护状态，v4 不再另维护一套实时负责人。

### 4.3 现有在途材料与旧欠账承接

| 旧项目或材料 | 当前处理 |
|---|---|
| `RT-001/002/003` 及旧阶段结案 | 保留成果和原验收；新生成链重新给证据 |
| `RT-004` 表达、`RT-006` 行为学习 | 数据和显式反馈迁往 HDS-009；不先扩写旧网关/评分链 |
| `RT-005`、`MEM-005/006` | 事实语义、管理、遗忘和有效回归迁往 HDS-006；检索 `.5` 下限不因重构放松 |
| `PLAT-003`、仓储/版本治理 | 恢复地基由 HDS-001/002 承接；禁止仅靠回滚二进制承诺数据可恢复 |
| 旧标定、插件第二期、WebSocket | 不挡隔离原型；被替代部分在 HDS-010 明确退出，仍进入发行的参数必须重新有依据 |
| 托盘实际退出、真机安装、模型质量和长期体验 | 仍需针对实际候选验证，由 HDS-012 收证；不泛称整个项目尚未人工验收 |
| UI 分组与 Client 路由判据欠账 | 保留原 Owner 与独立验证要求；重构不能抹账 |
| 入口时已暂存的记忆分组工单及测试 | 属其他协作者；本轮不修改、不代提交，是否合流由其原交接处理 |
| 未跟踪的学习安全补丁及三份 HDS/QQ 研究资料 | 原样保留；未跟踪不等于已入库，不能只提交引用它们的链接 |

旧派单原文见 [全量收敛记录](../full-scope-sprint.md)、[冻结盘点](../freeze-20261020-line-inventory.md)及领域文档。保留义务不意味着沿旧生成架构补齐全部旧排期；每项按 v4 §15 的继承或替代关系处理。

## 5. 优先级与实施顺序

优先顺序是“数据可恢复与身份/提交契约 + 隔离生成原型 → 输入/提交/投递/记忆闭环 → 连续角色体验 → 单引擎切换和候选验收”。这是依赖图，不恢复阶段指针，也不要求每项串行等作者逐层开门。

| 里程碑 | 包含任务 | 出口证据 |
|---|---|---|
| M0 恢复与契约地基 | 001/002；003 可隔离并行 | 可恢复、稳定身份、版本及写入权边界清楚 |
| M1 新回复原型 | 003 | 真正统一 Narrator；本地小模型与 API 的质量、延迟和失败记录 |
| M2 新主链闭环 | 004/005/006 | 能接收、提交、发送、停止、删除、重启；数据/投递约束通过 |
| M3 连续角色体验 | 007/008/009 | 时间续接、主动许可、预算、状态与证据隔离有真实验证 |
| M4 切换与候选 | 010/011/012 | 单引擎拥有权、旧链退出、独立验收、可安装和可恢复 |

### 5.0 推进规则：四条不干扰判据

继承作者 2026-09-24、09-27 规则及 10-19 取消阶段指针的裁定：可以大跨步，也可以随时回修已暴露问题；不能以阶段已结案为由拒绝修复，也不能借回修扩大功能范围。

1. **载体隔离**：并行实现用独立分支/worktree，先检查状态；与他人未提交改动相交时协调或错开，不覆盖、回退或顺手提交。
2. **单一写者**：`MainWindow.axaml.cs` 及同族宿主文件同一时间只允许一个任务直接编辑；数据库热点和共享计划同一段也应避免双写。
3. **先登记再开工**：在 §4.2 认领编号、Owner、状态和范围；跨职责边界先核对归属与交接，不能在自己文档中单方面宣称没有冲突。
4. **读数不得互顶**：每项在自己的载体给可复现验证；不借其他提交、阶段或模型的绿灯，不以并行为由省略应做检查。

“过快”指地基无法支撑下一层，不按天数或阶段号判断。回修在受影响的验收面重新验证；红门禁、严重数据问题和未证实假设必须如实保留。

### 5.1 优先解决的审计问题

具体源码定位与复现边界见 v4 §12；以下是静态审查结论和待验证风险，不能冒称全部已动态复现。

| 优先级 | 问题与后果 | 承接 |
|---|---|---|
| P0：生产迁移/切换前 | 备份恢复与高版本保护缺口；会话删除未贯穿在途失效，迟到结果可能写回或恢复被删内容 | 001/002/004/005/006 |
| P1：相关路径落地前 | 默认上下文/输出预算冲突；建议块漏预算；撤销 CAS 与证据恢复竞态 | 003/006/011 |
| P1：处理不可信输入前 | DOCX 解压/XML 全量处理和附件取消/限额；表情附带文本绕过长度验证 | 008/011 |
| P1：闭环与候选前 | 检索/主动上下文分叉、证据读取成本、默认小模型效果与故障处理缺证据 | 003/006/007/012 |
| P2：退役和候选前 | 旧窗口死入口、实现形状测试薄弱、依赖与 SDK 不可复现、打包版本和许可声明漂移 | 010/011/012 |

编号对应关系和精确判断以 v4 `AUD-01～10` 为准。本表排序不自行改变旧工单的历史严重度，也不把原型限定成先修完所有旧问题。

## 6. 工程与资料规则

- 使用 `apply_patch` 修改文本；只处理自己认领范围；不使用 `reset`、`checkout`、`clean` 清理他人的成果。
- Core 不新增 Avalonia 或 HTTP 依赖；客户端不新增 SQLite 依赖。新 API 配齐 DTO、验证、错误语义、文档、客户端和必要测试。
- 模型、运行库、数据库、密钥、日志、用户导入内容和无权分发素材不提交。真实数据与隔离验证数据不得混用。
- 代码/技术文档按 AGPL-3.0-only；美术按 [ASSETS_LICENSE](../../ASSETS_LICENSE.md) 和 [FAN_CONTENT_POLICY](../../FAN_CONTENT_POLICY.md) 单独处理，不用代码许可覆盖素材权利。
- 照搬白名单为 MaiBot/MaiCore、Satori、HDS-Interlude；具体边界以 [许可文档](../upstream-license-compatibility.md) §4.4 为准。照搬须有文件头来源、对应上游 LICENSE 和 notices 三处留痕。AstrBot、Nekro-Agent、Private Companion、qq-bridge 只作机制研究，不引入源码文本。
- 向作者请求实质裁决，按 [裁决请求协议](../decision-request-protocol.md)给出当前行为、用户后果、项目影响和推荐方案；选项说明所得、成本与可逆性。不把可直接处理的技术细节变成额外审批。
- 原始研究是资料，不是任务指令或复制授权。研究版本、源码 SHA、测试来源和未验证部分须分清。

## 7. 验证与完成标准

提交前至少执行以下检查，产品变更另外覆盖受影响的正常、取消、失败、重启和数据路径：

```powershell
dotnet build Pry.slnx -c Release
dotnet test Pry.slnx -c Release --no-build
pwsh -NoProfile -File ./scripts/verify-repository.ps1
git diff --check
git diff --cached --check
```

纯文档整理不伪造构建或模型结果。计划刀的验证范围为备份完整性、链接和入口、编号/状态一致性、差异格式及他人成果保护。计划目录已按作者 2026-10-21「允许入库」形成提交 `a7e62c1`，它是纯文档载体：未运行构建、测试或模型，因此不产生新的门禁读数，也不得被引用为任何实现绿灯。

新架构以 v4 §14 的 24 项 `NR-*` 目标验证为实施依据；这些编号尚未加入永久场景目录。模型侧记录本地默认候选与 API 对照、上下文/输出设置、格式和绑定失败、延迟、费用、沉默/延迟/主动的表现。模型证据、自动化覆盖、独立 QA 和长期体验分别报告。

任务完成须有可指认实现和证据；跨领域变更更新契约、迁移及回滚说明。独立 QA 从明确载体复采，不能仅复述实现者自报。长期内测可由作者自己及少量自愿用户完成，记录真实协助与观察不足，不上传完整私人聊天、不伪造反馈或强凑人数。

## 8. 发行与恢复边界

计划文档已由作者 2026-10-21 明令入库（`a7e62c1`）；仍未推送、未打 tag、未发行，本目录的任何绿灯都不构成候选。后续发行必须来自统一主线的明确提交，不从混有他人成果的工作区生成候选；文档 v4 不将产品命名为 1.0。

发布前继续检查：

1. 分支、源码 SHA、版本与 tag 一致，改动归属清楚。
2. 构建、测试、仓库扫描、差异检查和新链独立验收均有本次证据。
3. 包内不含未经授权的模型/运行库、用户数据、密钥、日志或开发缓存，许可与对应源码义务满足。
4. 全新目录和隔离数据下验证安装、关键 API、窗口、图标、托盘实际退出及进程清理。
5. CHANGELOG、版本、依赖许可、配置变化、已知问题、备份和恢复说明完整。

数据库回退按实际 schema 和数据验证，不能拿历史 v7 的“可忽略新增表”保证未来恢复。备份考虑 SQLite WAL 一致性；恢复先在隔离目录验证，不能复活备份之后已明确遗忘的内容，也不能只回滚程序而忽略数据兼容。

## 9. 最近交接

### 2026-10-22｜守门人｜第三波工单出闸（一个载体四片，只起一轮构建）＋工单直接交作者转派

- **作者的指令形状变了**：他今日逐字＝「继续派工」，随后补一句「如果有需要以前我创建的智能体干活的话，你把工单交给我」
  ⇒ 执行方式改为**守门人只出工单、不起子 Agent**，干活的人由作者点名。这一条改变的是"谁来跑"，不改变判据链与出口证据要求。
- **派出的单**＝`docs/dispatch-hds-carrier-20261022.md`，四片：片 A＝`DEF-WRIGATE-01`（宿主层取真实独占句柄→过 `DataDirectoryWriteGate`→拒绝时一个字节都不写并以人话拒绝启动，落点在 `src/Pry.Api/Services/RuntimeDescriptorService.cs:49/59/63/88-89`，契约口径＝`docs/narrative-storage-contract.md` §10.2 第 1／2 条）；
  片 B＝`DEF-MEMDUAL-01`（`src/Pry.Core/Memory/SqliteMemoryRepository.cs:100` 那句裸 `InvalidOperationException` 改走 `IUserPresentableError` 链，并把"两账并存"做成一条断言；**明令不许合并两账**，那是 HDS-005 的迁移决定）；
  片 C＝`ContextBuilder.cs:164` 第二道地板门的行为判据（零产品代码，上一单 §9 自记"未做双突变"那一格）；片 D＝零构建文档刀（`:219` 的加粗形状，`:290` 归片 A 顺带消）。
- **为什么合成一个载体**：作者 10-21 那句「不要再反复构建触发那个防火墙烦我打游戏」是硬约束。三片要动产品代码，分开派＝三次弹窗；
  合进一把＝一轮解决方案级构建＋一次全量测试，反证一律走单项目 `-t:Rebuild`＋`--filter` 定向跑法。工单里把"整个载体只允许一轮全量"写成可判红的第 3 条全局约束。
- **出口算术已钉死**（起点＝线尾 `0c38be0` 的代码面，读数即守门人今日逐份读 TRX 采的那一轮）：Core 524／Api 258／App 233／Client 106＝总 1121，失败 0，
  两条环境跳过点名。四个分项对不上＝读数作废。`verify-repository` 起点数到 **530**（含本工单入库那一格）。
- **本侧今日零构建**：这一把只有文档，没起 dotnet，没弹防火墙。
- **在飞的仍为 0**；`QA-TIP1020-02` 的实现（63/0）与客户端路由判据 `2954dbc` 仍等主工作区索引清空，不由本单承接。

### 2026-10-22｜守门人｜U-1～U-12 决策备忘归位（`docs/narrative-open-items-memo.md`）＋它查出的一处真缺陷

- 归位：新建 `docs/narrative-open-items-memo.md`（268 行）一枚文档提交 `df84c71`，无代码改动、**全程零 dotnet**（这一把派出去之前我把"一次都不许起构建"写进工单正文，lane 交付里逐字复述了这条）。主工作树只经由 `git merge --ff-only` 收下，他人索引照旧未动（1/1 与 63/0 归位前后各数一次）。
- 对账结果（备忘的主读数）：U-1～U-12 **没有一条被既有代码全答**——部分回答 5 条（U-1／U-5／U-6／U-10／U-12）、仍需实现波决定 6 条（U-2／U-3／U-4／U-7／U-8／U-11）、需作者拍 1 条（U-9 接线与灰度时机，另 U-2 里"第二宿主被拒时用户该看见什么"作附问按四段式提出）。⇒ HDS-005 的真前置＝U-2＋U-3（真目录锁与代次持久化）＋U-11（步骤字段，硬依赖 U-2）＋U-5 的实例维；HDS-004 的真前置＝U-9 与 HDS-003 的语义结论。
- **守门人独立复核的两条承重指控**（不靠备忘自述）：① 同一个库里确实有**两套结构版本账**——`schema_info`（`MemoryDatabase.cs:57-58`、读处 `:983`，由高版门 `EnsureWritableSchemaVersion`（`:195-199`）抛 `SchemaVersionTooNewException`，那个类型带 `IUserPresentableError`＝有标题／正文／`RetryAllowed=false`）与 `memory_schema`（`SqliteMemoryRepository.cs:65-66`、读处 `:99`，`:100` 遇 `<1 or >2` 只抛裸 `InvalidOperationException`，不经那条呈现链）。② 第二宿主持有同一数据目录时，生产路径今天仍是**记一条警告然后覆盖描述文件**（`src/Pry.Api/Services/RuntimeDescriptorService.cs:88-90` 原文「本次启动将覆盖它」），而 HDS-002 的 `DataDirectoryWriteGate` 契约禁止的正是这一形状。
- **我不给备忘加戏的地方**：`memory_schema` 那半的"拒写"并非零判据——`tests/Pry.Core.Tests/MemoryDeletionSeparationTests.cs:273-288` 那条 N12 已经把 version=3 与 version=0 都钉成抛异常。缺的是**呈现形状**（用户看见的那两句），不是"没人守着"。另外两账通常一起推进，只有迁移中途失败让它们分叉时，才会出现"`schema_info` 看着可写、`memory_schema` 是高版"这条绕过门的路径——所以这条属**已暴露旧缺陷**，不属"当前必然损坏数据"。
- 登记为 `DEF-MEMDUAL-01`（HDS-001 那把暴露的可见形状缺口）＋`DEF-WRIGATE-01`（第二宿主告警后覆盖）。两把都要动产品代码 ⇒ 按作者 10-21 那句硬约束**今晚不起新的构建轮**：修法可以共用同一轮全量，已排进 `docs/dispatch-hds-wave2-20261021.md` 的队尾，等他一句"修"或等 QA 复采那一轮带过去。
- 备忘自己的两处漂移我照实记（不改它名下文件，归位后由守门人登记）：U-12 的方向 v4 §`:220` 其实已经下过令（"任何初始化／迁移前取锁"），列在未决表里会让下一位重复讨论；§10.1／§2.1 的行号有 1 行漂移；空白 `CommitId` 的理由码写的是 213、实际在 `NarrativeDraftValidator.cs:387`。这些都属文字层，不影响上面的两条承重结论。
- 在册门禁未变（988／0／2 仍待 QA），本轮线尾＝`df84c71`、跟踪格数＝**529**、在飞＝0。这一把没跑构建、没跑测试、没跑模型。

### 2026-10-22｜守门人｜`QA-TIP1019-02` 归位（检索地板第一道门有判据了）＋拒收那枚会删掉补正的文档提交

- 归位：新建 `tests/Pry.Core.Tests/MemoryRetrievalFloorTests.cs` 一枚提交落主线＝`9f88a0c`（线尾，`main` 已跟上，gap=0）；工单 `docs/dispatch-tip1019-02-retrieval-floor-judgment.md` 加 §9 交接记录，状态词改成**已实现待验**。`src/` 零改动，那个 `.5` 一个字节没动。
- **读数由守门人独立采，不靠实现方转述**：那条 lane 的结果目录留在临时盘上（`pry-tip1019-02-results-5b952707`），我当场逐份读四份 TRX 的 `<Counters>`＝Core 524（executed 522、passed 522、failed 0）／Api 258／App 233／Client 106，总 **1121**／失败 0／通过 1119；两条跳过按 `outcome="NotExecuted"` 点名（`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`）。顶账＝Core 523→524 恰好本刀那 1 条 `[Fact]`，其余三分项逐位不动；地板那条用例名在 Core 的 TRX 里可见。**我这一把没跑任何构建或测试**（一轮之限对我自己也生效，而它是判据刀、编译输入与我给它补的文档格同一）。
- 判据形状值得记一句：夹具先自检"两行都是 active"，所以状态闸不会替置信度背书；断言面钉在 `RetrieveAsync` 的 `Hits`，正是工单 §7 F6／§8 提醒的那个"端到端会拿假绿"的坑的反面。
- **本轮唯一的形状风险已挡掉**：该 lane 的第二枚提交（工单文字）起点是**没有 §8 补正二的旧版工单**——整枚收下会把 §8 那段当场删掉。处理方式＝只 cherry-pick 测试那枚 `4cb4b7a` 到线尾 `8776a5b`，工单文字由守门人重写；这类"实现方编辑了它没读到的共享文档版本"的形状，以后归位前先 `git diff --name-only` 数一遍它改了什么。
- 仍未覆盖的格子（别读过头）：第二道同值地板 `ContextBuilder.cs:164` 本轮**没有**判据，lane 如实登记"未做双突变、单点压不红"，属防御纵深；同族的两处单边抓手（`RelationshipState.cs:139` 的 `0.5`、`BehaviorPlanner.cs:125` 的 `0.55`）照 §7 只登记不补。在册基线仍 988／0／2，改写权在 QA——现在有五把刀（AUD-01/06/04、HDS-001、HDS-002、HDS-011b、本单）等它从线尾独立复采。
- 在飞＝0。排队下一位＝HDS-011c（资源限额总盘与依赖/许可复采，已随 HDS-011b 落地解锁）；HDS-003 仍等作者点头；`2954dbc` 仍等主工作区索引清空由守门人合流。跟踪格数＝**528**。

### 2026-10-22｜守门人｜门禁口径由"连续两次全绿"降为"一轮构建＋一轮全量"，并据此续派 `QA-TIP1019-02`

- 依据（作者指令逐字）：「你 下一次 不要 再 反复构建 触发那个防火墙 烦我打游戏 再这样 我要给你喂史了」。据此，一切工单里"解决方案级连续两次全绿且分项逐位相同"这项要求降为**一次**完整 `dotnet build Pry.slnx -c Release` ＋**一次**全量测试；改写权由守门人承担并在此登记，不改的部分＝四份 TRX 逐份读 `<Counters>`、长参绝对路径 `--results-directory`、`--logger trx`、跳过按 `outcome="NotExecuted"` 点名、引用总数必须写全四分项、出口那轮必须紧跟完整编译（严禁 `--no-build` 旧 dll）、反证只许定向跑不重跑全量。
- 代价写清楚（不是白拿的）：少了一次"同树重跑"的稳定性对照，历史上 `gate-expr-flake` 那一类表达式抖动因此可能漏检。补偿口径＝四分项必须逐位对上预期算术，对不上就当场停下并登记为读数作废，而不是"再跑一遍看看"。真需要第二次全量时（例如读数与顶账不符）由守门人自己起，并在那把的 §9 里写明为什么多花一次弹窗。
- 续派：`QA-TIP1019-02`（检索地板 `.5` 的行为判据，零产品代码，不许动那个数字）。工单 `docs/dispatch-tip1019-02-retrieval-floor-judgment.md` 新增 §8「开工前补正二」，内容＝锚点当场重读结果（F1 仍在 `MemoryQueryService.cs:22`；第二道地板行号从 `ContextBuilder.cs:88` 漂到 `:164`，`Take(6)` 在 `:172`、`stale_or_deleted` 在 `:176`）、门禁降档、基线＝`c148fbf`、预期读数顶账（Api 258／Core 523＋新增／App 233／Client 106）、落点收窄为新建 `tests/Pry.Core.Tests/MemoryRetrievalFloorTests.cs`（不追加进 `MemoryContextTests.cs`，避让主工作区索引里他人名下那 63/0）。
- 本 entry 零构建、零测试、零模型：只读码与跑加粗形状仪器（正向对照 DEFECT_LINES=2，改动四份文档 NEW_DEFECTS=0，`git diff --check` 干净）。

### 2026-10-22｜守门人｜HDS-011b 归位（AUD-07 长度校验＋那一行警告）＋守门人这一把为什么没复跑全量

- 载体与结局：`.qoder/worktrees/agent-general-purpose-464436d4`，分支 `codex/hds-011b-sticker-content-length`。起点落后线尾 86 格，先 `git merge --ff-only 7d8ab03` 追平才动手；工单前提（那两行的三元形状）当场复核仍成立。实现两把 `5c06938`（fix）＋`34783af`（test），守门人 rebase 到线尾 `5304c7c` 后得 `1edf69e`＋`c148fbf`，主工作树以 `git merge --ff-only` 归位，现线尾＝`c148fbf`。
- 落地内容：5 个文件、+210／−5＝`src/Pry.Api/Services/ConversationSessionService.cs`、`src/Pry.Api/Services/ConversationApplicationService.cs`、`src/Pry.Contracts/ApiContracts.cs`、新建 `tests/Pry.Api.Tests/StickerContentLengthGuardTests.cs`、`tests/Pry.Core.Tests/NarrativeStorageContractTests.cs` 的一行。`*.csproj`／`*.json`／NuGet 零变化＝没新增依赖，Core 仍不依赖 Avalonia／HTTP。
- 改法复核（读码得出，不靠构建）：新增唯一校验点 `ContractValidation.MessageContent(value, requireText)`，长度判断刻意放在分支**之前**，`requireText` 只决定"能不能一个字都不带"；上限 `MessageContentMaxLength = 20_000` 全仓只在 `ApiContracts.cs` 声明一处。错误面与旧路径逐字相同（`ApiValidationException` 的 field 仍是 `content`，文案仍是"不能为空"／"长度不能超过 20000"），所以纯文本那支的用户可见行为没变，变的只是带贴纸那支从此也被同一把尺量到，且拒绝发生在落库之前。
- 出口读数（lane 自报、守门人核对算术与属性计数，**没有复跑**）：`dotnet build Pry.slnx -c Release` 0 警告 0 错误，紧跟一轮全量测试得四份 TRX＝Api 258／Core 523（executed 521、passed 521）／App 233／Client 106，总 **1120**／失败 0／通过 1118；两条跳过按 `outcome="NotExecuted"` 点名＝`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`、`CoreTests.Configured_sense_voice_model_can_run_offline`。顶账：1120＝258+523+233+106＝上一把归位后的 1113＋本刀 7 条；7 条＝新文件 3 个 `[Fact]`＋2 个 `[Theory]`×2 个 `[InlineData]`，守门人逐文件 `grep -c` 复核过这三个数。在册基线仍是 988／0／2，改写权在 QA。
- 为什么这一把守门人不再跑全量（对着作者那句硬约束）：lane 出口那轮已经紧跟一次完整编译（工单全局约束第 5 条），而它缺的那一格 `5304c7c` 是纯文档提交——`git diff --name-only 7d8ab03..5304c7c` 只有两份 docs，编译输入与 lane 的建树同一；rebase 后守门人再数 `git diff --stat 34783af..HEAD -- src tests`＝空输出，代码树没动。换在昨天 HDS-002 那种"rebase 后带代码漂移"的形状上，这个读数是要作废的。
- 反证（lane 交的红例名与报错原文，守门人核对过对应断言行真实存在）：① 掏空 `MessageContent` 里的长度判断 ⇒ 4 红 3 绿，原文 `Assert.Throws() Failure: No exception was thrown / Expected: typeof(Pry.Contracts.ApiValidationException)`；② 把 `ConversationSessionService.SubmitAsync` 恢复成原三元形状 ⇒ 2 红 5 绿，红在 `Content_accompanying_a_sticker_is_rejected_over_the_shared_limit(entry: TurnSubmission)` 和源码门 `Both_message_entries_call_the_one_ruler_and_no_second_ceiling_is_declared`（`Assert.Equal() Failure: Values differ`）。两次都用单项目 `-t:Rebuild`＋`--filter`，没有为反证重跑全量。
- 这一把守不住的形状（诚实边界，别读过头）：那条源码门的"不许有第二把尺"只认字面量 `20_000`——谁用 `20000`、别的数字或别的常量名另起一套正文上限，它不会红；它被证到的是"只补一个入口"和"字面上的第二把尺"。同时它按整行字面量匹配调用点，重排格式会假红（与既有 seam 门同一手法，已知代价）。两条都不卡结案，记在这里供 QA 判覆盖面。
- 主工作树安全：他人索引全程未动，归位前后各数一次＝`docs/dispatch-tip1020-02-memory-group-judgment.md` 1/1、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs` 63/0；`git merge --ff-only` 不产生合并提交，因此不会消费他人已暂存的条目；本刀未使用 `reset`／`checkout --`／`clean`。真实数据目录 `%LOCALAPPDATA%/PryCompanion` 没当测试载体（新测试自建 `Path.GetTempPath()` 临时目录，并按自身库文件清连接池）。
- 仪器读数更正（顶账）：lane 报 `verify-repository`＝526 tracked files，那是它把新测试文件补进去之前跑的；归位后守门人当场 `git ls-files` 数到 **527**（526＋那一份新测试）。这个数每落一份文件就上一格，不是门槛变化。
- 状态词：§4.2 的 HDS-011 行与本页顶部状态改成"已实现待验"，`已完成`／`结案` 仍归作者与 QA。本刀归位后在飞＝0，但同场按工单「本波之后」的第 2 位继续派单（`QA-TIP1019-02`＝检索地板判据刀，零产品代码，落点在 Memory 检索语义，与他人的 `MemoryManagementSurfaceTests.cs` 不同文件），所以登记完之后在飞＝1；下一把 HDS-011c 仍排队，避免与它同轮抢构建弹窗。

### 2026-10-21｜守门人｜第二波派单（HDS-011b）＋把"不许反复构建"写成可判红的跑法

- 作者指令逐字：「你 下一次 不要 再 反复构建 触发那个防火墙 烦我打游戏 再这样 我要给你喂史了」。据此新建 `docs/dispatch-hds-wave2-20261021.md`，派 **HDS-011b＝AUD-07 切片**（带贴纸的消息整段跳过长度验证：`src/Pry.Api/Services/ConversationSessionService.cs:37-39` 与 `src/Pry.Api/Services/ConversationApplicationService.cs:57`，两处都写成 `request.StickerId is null ? ContractValidation.Required(request.Content, "content", 20_000) : request.Content?.Trim() ?? ""`——当场逐字读过才落进工单），并把上一把留下的那一行警告（`NarrativeStorageContractTests.cs:313` 的 xUnit2012）捆进同一把，理由就是这句约束：**能共用一轮构建的改动不拆成两轮**。
- 工单里新增的两条可判红跑法（本轮实测教训写成规矩）：① 一个载体只允许**一轮**全解决方案构建＋全量测试，反证改用定向跑法（单项目 `-t:Rebuild` ＋ `--filter`），不许为反证重跑全量；② **出口那轮必须紧跟一次完整 `dotnet build Pry.slnx -c Release`**——今天 HDS-002 的自报门禁就是栽在 `--no-build` 复用旧 dll（详见上一条 entry），这条不是风格问题，是读数真伪问题。
- 排队与不派的东西写进工单末节：HDS-003 前置已解除但真实模型对照占他机器／API 额度，**等他点头**；`QA-TIP1019-02` 判据刀排第二；`2954dbc`（客户端路由判据）等主工作区索引清空由守门人合流，不派给实现方；`QA-TIP1020-02` 的实现此刻正悬在他人索引里（63/0），不重复派；HDS-011c（资源限额总盘、依赖与许可复采）押在 HDS-011b 之后以免同文件撞位。
- 本刀**没跑任何构建、没跑测试、没跑模型**：只做文本与 `git`／`node` 级检查。加粗形状先跑正向对照（探针三行，两种缺陷形状各自转红、`正常**汉字**正常` 保持绿）再引用读数，新工单 0 处缺陷、本轮改动的两份跟踪文档对 HEAD 比出 `NEW_DEFECTS = 0`；`git diff --check` 干净。跟踪格数在派单落刀时从 525 上一格（`verify-repository` 那个数是格数，不是门槛）。
- 状态词：§4.2 的 HDS-011 行先写"AUD-07 切片已派＝HDS-011b"，lane 离闸后改成"进行中／守门人代派至隔离 worktree"；两版都不写"已完成"——派单与开工都不等于实现，归位时按出口证据再换词。


### 2026-10-21｜守门人｜HDS-002 契约刀归位＋它的自跑读数被证伪

- 载体与结局：`.local-notes/hds-002-contract`，分支 `codex/hds-002-identity-storage-contract`，起点 `1c2e83d`，实现提交 `5145bde`（父提交 `86cd08c`＝该分支 rebase 到当时线尾之后）。**该 lane 在 23:40 撞到 150 轮上限被系统停掉**，它留下的最后一句是"Core builds clean. Now the test files."——而 `git log` 显示它在 23:37:28 已经把实现＋两份测试＋契约文档都提交了。**教训按事实记下来：lane 的最后一句话不能当状态读，状态读 git。**
- 落地内容：11 个文件、+3754/**−0**（8 个 `src/Pry.Core/Narrative/` 纯领域文件、2 份新建测试文件、1 份 `docs/narrative-storage-contract.md`）；**没有一个现存文件被改动**，所以这一把对用户可见行为是零影响（契约是文字与形状，还没有人读这些类型）。
- 纯度复核（不靠构建）：8 个 Narrative 文件**连一行 `using` 都没有**；`git diff --stat` 对 `*.csproj`／`*.json`／`*.props`／`*.targets` 为空；全文只有 `DataDirectoryWriteGate` 里一个名为 `ExclusiveWriteLockHeldByAnotherProcess` 的布尔属性提到"进程"，那不是进程 API。工单"本波只做契约＋纯领域类型、不落表、不预占版本、不接线"照字面成立。
- **它的自跑门禁被逐份 TRX 证伪**（`.local-evidence/gate`，23:35:43）：Core total=491 里 `DataBackupServiceTests`／`DataRestoreServiceTests`／`SchemaVersionGuardTests`／`MemorySchemaMigrationRegistryTests` **四个类全部缺席**（32 例），Client total=101 少了 HDS-001 的 5 例——`491 = 462 − 32 + 61` 刚好对上，即 `dotnet test --no-build` 跑的是**它 rebase 之前编出来的旧二进制**。它的 `hds002-core/core-run1..3`（4 红／1 红／0 红）是开发迭代不是出口读数。**所以那一份自报门禁整批作废**，合流树由守门人重跑一次（见下）。
- 守门人门禁（**一次构建＋一次全量测试，只此一轮**；载体＝`5145bde` 的 detached worktree `.qoder/worktrees/gate-hds002`，用完已删）：`dotnet build Pry.slnx -c Release` ⇒ **1 个警告、0 个错误**——那条警告是它自己的 `tests/Pry.Core.Tests/NarrativeStorageContractTests.cs:313` 用 `Assert.False()` 判集合成员（xUnit2012）。**没有当场改**：改一行就要再来一轮构建，而作者 10-21 夜明确不许反复构建；登记为"下一把该构建时顺手带走的一行"，本轮不动。`dotnet test --no-build --results-directory C:/Users/24782/gate-hds002-trx --logger trx` ⇒ **Client 106／App 233／Api 251／Core 523（执行 521、通过 521）**，总 **1113／失败 0／跳过 2**，四条 `<Counters>` 从各自 TRX 当场读；两条 `outcome="NotExecuted"` 点名 `CoreTests.Configured_sense_voice_model_can_run_offline` 与 `LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`（与基线同两条）。`measure-fixed-eval.ps1` ⇒ Catalog valid: 76，Passed=1111; Failed=0; Other=2。`verify-repository` ⇒ **Repository verification passed for 525 tracked files**（514＋11，格数非门槛），`git diff --check` 干净。
- 新判据顶账（先逐文件数属性再对分项）：`NarrativeIdentityContractTests.cs` 17 `[Fact]`＋3 `[Theory]`/14 `[InlineData]`＝31 例、`NarrativeStorageContractTests.cs` 30 `[Fact]`＝30 例，合计 **+61**；Core 462→523 与总 1052→1113 两处独立对上（TRX 里按类计数就是 31 与 30，没有 `Skip=`）。**在册基线仍是 QA 复采的 988／0／2，本 entry 不自封基线。**
- 它的三条反证（先红后绿）复核到能核的程度：提交信息逐条写了拆掉哪道守卫与报错行号——第二写者（`Assert.False(second.Accepted)`，第 63 行）、投递段唯一键改成覆盖写（第 164 行）、代次落后的条件写（第 306／96／261 行三条同红）；守门人**当场核过这五个行号确实是那五条断言本身**，`red1/red2/red3` 三份 TRX 的失败用例名与之逐条相符（1／1／3 红）。诚实边界：TRX 只能证明"这些用例红过"，不能证明"红是因为守卫被拆而不是断言被改"——那一遍仍归 QA 独立做。
- 契约文档的未提交更正由守门人代归位＝`e9a8055`（与来源文件逐字节相同，md5 `eeb6ca7c9606823a221eaa6ed42acfaf`）：lane 停机时把这批修改留在工作树里，内容是把 §11.2 从"迁移落点是散的 if"改写成按**已落地的** `SchemaMigrationStep`／`VersionAlign`／`IdempotentRepair` 记账，并新增未决项 U-11（往该类型追加 `FenceRequirement`／`BackfillEvidence`）与 U-12（目录写锁与版本读取的先后）；另有五处源码引用行号更正，守门人逐条对现树核过（`CharacterIntelligenceGateway.cs:38`、`MemoryDatabase.Runtime.cs:146`、`:200`、`MemoryDatabase.Proactive.cs:217`、`TurnTaking/ReplyPlanner.cs:12`、Narrative 目录 8 个文件）。该文档里有 2 处加粗形状缺陷（`**` 后紧跟空格、`**` 后紧跟全角引号，第 219／290 行），`bold-flank-newonly` 对 HEAD 比出 **NEW_DEFECTS=0** ⇒ 不是这次带进来的，是它自己提交的行文；不是作者那两页，本轮不代改，登记为外观小账。
- 归位方式：`5145bde` 是线尾 `86cd08c` 的**严格后代**，所以这次用 `git merge --ff-only`（不是 blob 手术）——前提是先确认它不动他人名下文件：他人的两份**已暂存**文件（1/1 与 63/0）在合并前后逐字节不变，合并后 `git status` 只剩那两份，`git rev-list --count 5145bde..HEAD`＝0。main 随后跟到线尾。
- 这一把不给用户任何新东西，也**没有解除两条真实风险**：① "同一实例不许同时有两份可发送真相"目前只是契约文字＋内存形状判定，真正可执行要等 HDS-005 落 outbox 与表；② 实例身份（LocalUser×Character）今天仍不存在落库通道，`CharacterIntelligenceGateway` 传的"实例身份"还是角色卡 ID 本身。§12 的未决项从 10 条变成 12 条（U-1…U-12），HDS-003／005 开工时要逐条对着读。
- 状态词：§4.2 的 HDS-002 填"已实现待验"；HDS-003 的前置（单一写者判据）随本轮归位解除，仍待认领，真实模型实验需作者点头。结案词归作者。


### 2026-10-21｜守门人｜HDS-001 可恢复地基归位合流树

- 载体：隔离 worktree `.qoder/worktrees/agent-general-purpose-fde9331c`，实现提交 `3b65ccc`（父提交 `a839e5f`＝派单当夜的线尾，所以这把的基线只落后 4 个文档提交——与上一把落后 76 个提交相反，认领时数基线的动作照旧要做）。合流载体 `.qoder/worktrees/gate-hds001` ＝ `7307583` ＋ `3b65ccc` 无冲突合并，验证完已删除。落地内容 22 文件、+2428/−144，可指认提交＝主线 `8ef7d01`。
- 复核（逐文件读真实 diff，不只读自报）：
  - 数据库走 `VACUUM INTO` 在线一致性快照，**不复制主 .db**；`BackupInclusionPolicy.AlwaysDeniedExtensions` 主动拒绝 `.db`/`.db-wal`/`.db-shm`/`.db-journal`/`.sqlite`/`.sqlite3` 的原始拷贝。`Online_snapshot_carries_committed_rows_that_still_rest_in_the_write_ahead_log` 守"WAL 里已提交的行还在"，`Bundle_never_ships_wal_or_shm_sidecars_and_the_snapshot_is_a_single_self_contained_file` 守"侧文件不许上船"，`A_missing_wal_sidecar_still_yields_a_consistent_snapshot_of_what_the_main_file_holds`／`An_inconsistent_wal_sidefile_never_travels_into_the_bundle` 守两侧缺件。逐文件散列＋`PRAGMA integrity_check`，`Verification_fails_loudly_when_any_bundled_file_was_tampered_afterwards` 守篡改。
  - 方向是**白名单**（只点名收：`characters`／`interaction-profiles`／`media`／`stickers`／`character-media`／`theme-media`／`preferences.json`／`character.json`），其余默认排除并**在清单里写明原因**（`ExcludedRootEntries`，恢复后"少了什么"不靠猜）；`logs`／`attachments`（用户导入）／`temp`／`clipboard`／`runtime`／`runtime.json`／`.log`／`.key`／`.pfx`／`.pem`／`.secret`／`.token` 硬排除，而且白名单目录内部逐文件再过一遍——`Secrets_logs_runtime_and_user_imports_never_enter_or_leave_the_bundle` 把假密钥塞进 `media/` 也不许上船。
  - 恢复只落**隔离目录**，核对报告按 v4 §9.3 逐格点名（`DataRestoreService.cs:108-155`：散列／完整性／结构版本／行数／稳定 ID／索引集合／外键／墓碑数量与摘要／删除水位／孤立态／引用计数／偏好可解析），任一格不一致能指到具体那一格；目标非空拒、路径穿越与"落在包内"拒、future schema 在第一个字节写入前拒、中断不发布最终目录且可重试。
  - 未知高版拒写：`EnsureWritableSchemaVersion` 跑在任何写入之前（代码注释自己写了原因——`PRAGMA journal_mode=WAL` 本身就是写）；迁移改为登记清单九步、每步一个事务边界、失败整步回滚且版本不推进、全部幂等（重跑初始化＝完整恢复路径，NR-23）；`ValidateRegistry` 拒空清单、拒重复登记号、**拒 `TargetVersion > SchemaVersion`**——工单"不许顺手预占 schema 11"这条由代码里的闸守住，不只是嘴上；`UpgradeMemoryIndexAsync` 删除，`EnsureColumnAsync` 可挂进步骤事务。
  - 对旧形状的实际改动（当场 `git show 7307583:` 比过）：初始化原来是**一段无事务的大 SQL 批次**＋六个 `EnsureColumnAsync`＋`SetSchemaVersionAsync`，`versionBefore` 只进日志——也就是说版本比二进制高的库照样会被按旧理解改表、并把登记版本**写回较低的数**。现在先拒写、再按清单分步跑。用户可见后果＝这种库第一次会明确报"本机数据库来自更新版本的 Pry，已拒绝写入任何数据"（`RetryAllowed=false`，防止界面引导人反复启动加深伤害），而不是静默降级。
  - `PryErrorPresentation` 新增 `StorageGuarded` 一档，`IUserPresentableError` 分支放在 `Describe` 最前；当场核过全仓只有那两个新守卫实现该接口 ⇒ 不遮蔽既有异常归因。**无新增 NuGet 依赖**（`*.csproj`／`*.json` diff 为空），Core 未引入 Avalonia 或 HTTP；`IUserPresentableError` 落在 `Pry.Contracts`（Core 与 Client 早已共同依赖它）。
- 合流门禁（**一次性跑完，之后不再重复构建**）：`dotnet build Pry.slnx -c Release` ⇒ 0 警告 0 错误；`dotnet test --no-build --results-directory C:/Users/24782/gate-hds001-trx --logger trx` ⇒ **Client 106／App 233／Api 251／Core 462（执行 460、通过 460）**，总 **1052／失败 0／跳过 2**，四个 `<Counters>` 当场从各自 TRX 读出、程序集归属按 `Pry.<X>.Tests` 前缀；两条 `outcome="NotExecuted"` 点名为 `Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline` 与 `Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`（与基线同两条环境跳过）。`measure-fixed-eval.ps1 -ResultsDirectory` ⇒ Passed=1050; Failed=0; Other=2，Catalog valid: 76。
- 新判据顶账：+37 例＝Core +32（`SchemaVersionGuardTests` 4 `[Fact]`＋1 `[Theory]`/3 `[InlineData]`＝7、`MemorySchemaMigrationRegistryTests` 6、`DataBackupServiceTests` 10、`DataRestoreServiceTests` 9）、Client +5（`StorageGuardErrorPresentationTests` 5），App／Api 不动。逐文件先数过属性再对分项：1015→1052 与 Core 430→462、Client 101→106 两处一致。**在册基线仍是 QA 复采的 988／0／2，本 entry 不自封基线**；改写权在 QA，需要 QA 从明确载体（现线尾 `8ef7d01`）重采。
- 仓库扫描的一处真实修复（不是放宽判据）：`verify-repository` 第一次判红——测试夹具里的假密钥串 `sk-FAKE-…` 命中了仓库的密钥扫描。改法是换字面而不是改扫描：三处哨兵去掉 `sk-` 前缀，并加一行注释说明"排除由文件名／扩展名决定，不由内容形状决定"。反证照旧跑过：`-t:Rebuild` 后把白名单目录里那个伪装件改回普通名字，那条断言当场打红 ⇒ 门还在守；改回后与提交内 blob 逐字节相同（`tests/Pry.Core.Tests/DataBackupServiceTests.cs` md5 `6fed87544dbc06eea9be5acb52344067`，工作树与 `8ef7d01` 那份同值）。最终 `verify-repository` ⇒ **Repository verification passed for 514 tracked files**（499＋本刀新文件与文档；跟踪格数非门槛），`git diff --check` 干净。
- 工单出口证据对账（`docs/dispatch-hds-wave1-20261021.md` HDS-001 节）：合成数据上的可复现备份—破坏—恢复＝已落（上列用例）；未知高版拒写的行为测试＝已落；"一份真实数据未被写入的核对"这一格由守门人以只读方式当场补采——`%LOCALAPPDATA%/PryCompanion` 共 40 个文件、树内最新写入 `2026-10-19 13:27`（他自己跑程序留下的），本轮全部测试跑在临时目录 ⇒ 本轮对该目录零写入。**仍未接生产路径**：没有 HTTP 端点、没有 UI 入口，触发时机（手动／定期／退出前）属产品决策——所以这一格不能读成"你能点备份了"，只读成"数据地基有了、还没有门走得出去"。
- 归位方式：主工作区索引里仍有他人两份**已暂存**文件（`docs/dispatch-tip1020-02-memory-group-judgment.md` 1/1、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs` 63/0），故不 `git merge`、不裸 `git commit`——按 `.gitattributes`（`*.cs text eol=lf`）把合流树内容逐字节写入工作区，新文件先 `git add` 建立索引条目，再 `git commit --only -- <22 显式路径>`。提交后他人暂存仍 1/1 与 63/0 未变，`git diff --stat` 只剩我自己名下的 `docs/plans/PROJECT_PLAN.md`。main 已 fast-forward 到 `8ef7d01`，差距 0。
- 状态词：§4.2 的 HDS-001 填"已实现待验"，不是"已完成"——独立复采与结案词都还没发生，结案词归作者。下一把按派单是 HDS-011 剩余范围里的 AUD-07（表情附带文本绕过长度验证），以及等 HDS-002 的类型归位后开工的 HDS-003。

### 2026-10-21｜守门人｜HDS-011 切片（AUD-01／06／04）归位合流树

- 载体：隔离 worktree `.qoder/worktrees/agent-general-purpose-97f9e73a`，分支起点实测是 `134eddc`（落后线尾 76 个提交），实现方当场 `git merge --ff-only a839e5f` 追平后才开工——**这条要记住：隔离 worktree 的起点不保证是线尾，认领前必须自己数 `git rev-list --count`**。实现提交 `ae67267`（7 文件，+605/−22）。
- 复核：逐文件读过真实 diff 而非只读自报。`ContextBudget` 新增 `InputBudget`／`TryNormalizeWindow`／`ForModelWindow`（输出保留≤窗口一半、`MinimumInputTokens=512`），配置保存 `ValidateTuning`、`ModelPerformancePolicy.WithContext`、每次 `BuildAsync` 三处共用同一把尺；`ContextBudgetException` 加 `Reason` 且无参构造保持原语义与原消息。AUD-06 确实删掉了 `prompt += "\n" + request.Advisory` 的旁路，改为预留份额并计入 `used`，决策文本仍逐字追加。AUD-04 改为流式：压缩比／解压字节（含伪造头后的实际字节复核）／节点数／深度／名字长度／正文字符／15 秒处理上限，`DtdProcessing.Prohibit`＋`XmlResolver = null`，取消向上传播而仅超时转限额错误。**无新增 NuGet 依赖**（`git diff --stat` 对 `*.csproj`／`*.json` 为空），Core 未引入 Avalonia 或 HTTP。
- 合流门禁（守门人自跑的载体＝`.qoder/worktrees/gate-hds011`，`38443f6`＋`ae67267` 无冲突合并成 `4cf1681`）：`dotnet build Pry.slnx -c Release` ⇒ 0 警告 0 错误；`dotnet test --no-build --results-directory C:/Users/24782/gate-hds011-trx --logger trx` ⇒ **Client 101／App 233／Api 251／Core 430（executed 428、passed 428）**，四份 TRX 的 `<Counters>` 非零、`failed="0"`；两条 `outcome="NotExecuted"` 点名为 `LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key` 与 `CoreTests.Configured_sense_voice_model_can_run_offline`（与基线同两条环境跳过）。总计 1015／执行 1013／失败 0／跳过 2。`measure-fixed-eval.ps1 -ResultsDirectory` ⇒ Catalog valid: 76，Passed=1013; Failed=0; Other=2。`verify-repository` ⇒ 499 个受跟踪文件（496＋3 份新测试文件，格数非门槛）。
- 新判据顶账：+25 例＝Core +22（`ContextBudgetTests` 7 `[Fact]`＋2 `[Theory]`/7 `[InlineData]`、`AttachmentLimitTests` 8 `[Fact]`）、Api +3（`ContextWindowLoadTests` 3 `[Fact]`），App／Client 不动。990→1015 与此一致。**在册基线仍是 QA 复采的 988／0／2，本 entry 不自封基线**；改写权在 QA，需要 QA 从明确载体（现线尾 `faf8ecf`）重采后才顶账。
- 归位方式：主工作区索引有他人的两份**已暂存**文件，`git merge`／裸 `git commit` 会把它们吞进合并提交，故按本仓库既有做法落内容——先把 `4cf1681` 的七个文件按 `.gitattributes`（`*.cs text eol=lf`）逐字节写入主工作区（`git hash-object` 与 gate 树逐文件相同），再 `git commit --only -- <七路径>`。提交后他人暂存仍为 1/1 与 63/0 未变，`git diff --stat 4cf1681 HEAD` 为空 ⇒ 主线树与合流树完全一致。main 已 fast-forward 到 `faf8ecf`，差距 0。
- 如实留着的两点（供 QA／作者复核，不因此判红）：① `AdvisoryTokens` 默认从"不占预算"改成 **2400 预留份额**，窗口极小的档位（如 256～几千 token）下原本能"静默超窗发出"的请求现在会显式失败（`AdvisoryDoesNotFit`）——这是 AUD-06 要的形状，但份额取值是否契合产品意图需要作者或 QA 定；② 实现方自报三条未采的反例：`ValidateTuning` 没有端到端 400 用例、15 秒超时→限额转换路径未测、伪造 zip 头未测。另 AUD-07（表情附带文本绕过长度验证，`ConversationSessionService.cs:37-39` 与 `ConversationApplicationService.cs:57`）本轮只定位未改，留给下一把刀。
- 状态词：§4.2 的 HDS-011 切片填"已实现待验"，不是"已完成"——独立复采与作者结案都还没发生。同夜 HDS-002（契约＋纯领域类型）已代派至新的隔离 worktree 登记为进行中；HDS-003 押后到 002 的 `Pry.Core.Narrative` 类型归位再开工，原因是单一写者判据——两支笔同时新建同一命名空间的类型必然有一支要做合并手术。
- 作者那页与仪器：`docs/full-scope-sprint.md` §0 的挡路栏改成"四把刀里还有三把没有 Owner＋第一把已归位待 QA"，欠话栏从三句加到四句（新增第④句＝Advisory 预留份额 2400 的取值要不要改）。加粗形状用 `.local-notes/bold-flank-range-1021.js` 当场跑：正向对照三条全过（两种缺陷形状转红、`正常**汉字**正常` 保持绿）后才引用读数；整页现存 8 处缺陷行全在我未触碰的旧长句里（L61／83／85／99／114），与 HEAD 做缺陷签名多重集对比 ⇒ **本轮新增 0 处**，`CHANGELOG.md` 同场 0 新增。`git diff --check` 干净，`verify-repository` ⇒ 499 个受跟踪文件（这一刀只改文本，格数不变）。

### 2026-10-21｜守门人｜计划入库＋HDS-WAVE-1 派单

- 作者指令逐字：「允许入库，同时根据计划书写的东西计划下一轮的派单」。据此把 `docs/plans/`（README、总计划、完整 v4、历史索引）、根目录 v4 兼容指针与六处已跟踪入口（`README.md`、`docs/README.md`、v3 头注、`docs/full-scope-sprint.md`、`docs/freeze-20261020-line-inventory.md`、`docs/research/README.md`）作为**一个文档刀**收进 `a7e62c1`；main 已 fast-forward 到该提交，差距 0。
- 他人成果核对：入库前后 `docs/dispatch-tip1020-02-memory-group-judgment.md`（1/1）与 `tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`（63/0）两份**已暂存**文件的暂存差异逐字节不变，未代提交；学习安全补丁与三份 HDS／QQ 原始研究材料仍未跟踪、原样保留。跟踪文件计数 490→495（新增 5 份计划载体），`verify-repository` 那个数每落一份文档就上一格，不是门槛变化。
- 复核过的计划内容：v4 §12 引用的五处源码证据当场全部对上——`src/Pry.Resources/appsettings.json:10` 与 `src/Pry.Api/Services/BackendRuntime.cs:288` 的 32768/32768 相等（⇒ `ContextBuilder.cs:55` 的 `total` 可为负）、`ContextBuilder.cs:115` 的 Advisory 绕过预算追加、`ConversationApplicationService.cs:38` 删会话不取消在途、`SqliteMemoryRepository.cs:433` 恢复状态的条件更新不查结果、`AttachmentTextExtractor.cs:37` 先 `XDocument.Load` 全量解析后截短。HDS-001～012 编号唯一，NR 十二行=24 项，`git diff --check` 与 `git diff --cached --check` 均干净。
- 对 v4 的两处更正接受并入库：`scripts/measure-fixed-eval.ps1:111-112` 会向结果目录写 `fixed-eval-coverage.json`，**不是完全只读**——我上一轮写进冻结登记的"只读"是错的，以本行为准。
- 状态词收口：Codex 自填的"已完成（研究、文档；未提交）"改为"文档已入库 `a7e62c1`，待作者结案"。状态词的定义与自授不应由同一支笔完成，结案词归作者。
- `AGENTS.md` 的传播性质未因本刀改变：`.gitignore` 排除 `/AGENTS.md` 与 `/PROJECT_PLAN.md`，所以 Codex 04:46 重写的那份规则文本仍只存在于本机主工作区。要把权威规则带到干净检出，只有两条路——作者允许调整忽略规则，或把规则正文抄进已跟踪载体。本刀取后者最小做法：已跟踪的 `docs/plans/README.md` 新增"现行工作规则（权威副本）"一节，`AGENTS.md` 保持其入口指针角色；调整 `.gitignore` 那一步不由 Agent 代拍。
- 派单：新建 `docs/dispatch-hds-wave1-20261021.md`，第一波四把刀（HDS-001 恢复地基／HDS-002 身份与存储契约，本波只做契约与纯领域类型／HDS-003 隔离原型／HDS-011 的 AUD-01·06·04 切片），每把带出口证据与"不许走的捷径"。派单把坐标系从旧 MaiCore 回复链切到 v4 任务表；「甲」当日停的是往四个旧界面派新功能刀，本刀不解除它，也不把旧标定、插件第二期、WebSocket 排回前面。HDS-003 的真实模型实验要占他的机器或 API 额度，动手前另需作者点头。
- 作者那页归位与当场读数：`docs/full-scope-sprint.md` §0 的挡路栏与欠话栏改到当前（入库栏记录 `a7e62c1`／`8b4d8bd`／`e24f104` 三个文档刀，挡路第一位换成"四把刀没有 Owner"），加粗标记成对写入并当场校验奇偶与 flank 形状通过；`git diff --check` 干净；`powershell -NoProfile -File ./scripts/verify-repository.ps1` ⇒ **Repository verification passed for 496 tracked files**（495＋这份派工单，是格数不是门槛）。本波**未运行构建、未跑测试、未跑模型**，因此不产生任何新门禁读数，在册仍 988／0／2，顶账权在 QA。
- 仍挂着的作者裁决：进程拓扑换不换（v4 §0.2 按"不换"设计）、HDS-007 后台推进是否留在本轮核心、`ASSETS_LICENSE.md:11` 那句旧许可名（只有他能改）、以及 tag／push／打包／托盘实际退出。

### 2026-10-21｜DOC-HDS-002｜共同计划整理（已完成：文档）

- Owner：Codex / Architecture & Planning。作者要求将重写计划放进所有 Agent 可共同读取的项目目录，并整理总计划。
- 交付：`docs/plans/` 包含统一入口、当前总计划、完整 v4 方案和历史索引；根目录两份旧位置文件保留兼容指针，`AGENTS.md`、项目/领域索引及旧计划入口已同步。原总计划 1,231,187 字节完整备份，SHA-256 与原文一致；当前任务不再夹在约 1.23 MB 的累计历史里。
- 验证：133 个本地文档链接、5 个章节锚点无失效；HDS-001～012 编号唯一且全部待认领；新计划文本无行尾空白或冲突标记；`git diff --check` 与 `git diff --cached --check` 通过；仓库扫描通过 490 个受跟踪文件（起草当场未跟踪的新文件不计入；`docs/plans/` 入库后由守门人重取为 495，派工单落地后 496——这个数是跟踪格数，不是门槛）。
- 保护核对：入口时两份他人暂存文件、学习安全补丁及三份原始研究文件，共 6 份文件内容摘要保持不变；暂存差异摘要保持不变。备份摘要再次核对相同。产品、测试和素材协议未修改；未运行构建、自动化测试或模型，未暂存、提交或推送。
- 传播与下一步：本机 Agent 现在共同读取主工作区绝对路径 `C:\Users\24782\Desktop\Pry_Project\docs\plans\`。目录未被忽略，但未提交前不自动进入其他 worktree/干净检出；后续文档入库应把共享目录和入口一起纳入。后续实现按本表认领，不以本次计划完成宣称新架构已落地。
- 后续（守门人补记，2026-10-21）：上面那句"未提交前不自动进入"已在本日关闭——作者明令「允许入库」，本目录与引用入口作为文档刀收进 `a7e62c1`，`main` 同步；本 entry 中的"未暂存、提交或推送"描述的是起草当场的工作区状态，不是当前状态。第一波派单见 §9 顶部与 `docs/dispatch-hds-wave1-20261021.md`。

### 2026-10-21｜ARCH-HDS-001｜研究与 v4 计划（已完成：文档）

- 重点阅读 HDS 报告，复核固定提交 `1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090` 的生成、上下文、提交、投递与证据机制；未安装/运行 HDS，未执行真实模型实验。
- 交付 v4 目标架构、保留/替换关系、12 张任务、24 项核心验收与 10 类审计问题映射；旧 v3/旧排期降为历史依据。
- 实现尚未开始。默认小模型的表现、故障矩阵、安装恢复和真实连续体验仍需后续任务验证；历史测试数字不作本轮通过证明。
