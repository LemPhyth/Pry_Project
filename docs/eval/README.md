# Pry 固定 Eval Suite

维护任务：EVAL-001（QA / Evaluation）；纵向反馈：EVAL-002。验收问题是：**同一个角色，是否在合适的时机，以符合她身份和共同经历的方式行动？** 内容正确、测试总数增加或构建成功都不能单独回答这个问题。

- [固定场景与约束](scenarios.json)：68 个永久 ID（suiteVersion 1.1.2），仍只 10 个 category；每项都有 Scenario、Input、Initial State、Expected Decision、Expected Output Constraints、Forbidden Outcome。
- [首轮实测报告](reports/2026-09-20-baseline.md)：冻结快照、缺陷、验证限制与发布阻塞。
- [主工作区复跑记录](reports/2026-09-20-rerun-main-worktree.md)：首轮三项 QA 缺陷回归通过，新增 QA-004/005；发行仍未评定。
- [Phase 1 验收判定（02:41）](reports/2026-09-21-phase1-acceptance.md)：**不通过**。§7 门禁红（456/3/2）、M0-d 零实现、无统一提交；三条失败逐条定责与源哈希。
- [Phase 1 验收判定·第二轮（14:58）](reports/2026-09-21-phase1-acceptance-rerun.md)：**仍不通过**。三条红门禁按建议方向归零（535/0/2）、M0-d 与 PLAT-001/006 已落地；采纳 `PLT-001`…`PLT-012` + `PER-005`；剩下统一提交与 `retryable` 契约断链（QA-P2-01）。
- [Phase 1 验收判定·第三轮（15:56）](reports/2026-09-21-phase1-acceptance-rerun3.md)：**仍不通过**，阻塞面收窄到两条——`PLT-002` 界面侧仍把 426/401 当网络故障重试（Fail）与无统一提交。`PLT-001` 转 Pass，契约字段已透传。546/0/2。**16:06 更正：本文件原记 QA-P2-04② "未动"有误，实为已修；文末更正记录说明错因（沿用上一轮结论、引用行号已漂移）与复核证据。判定不变。**
- [Phase 1 验收判定·第四轮（16:58）](reports/2026-09-21-phase1-acceptance-rerun4.md)：**仍不通过**，但阻塞面只剩一条结构性（统一提交）。`PLT-002` 界面分支已闭合转 **Pass**（双向用例 + 单一判据 `RetryForbidden`）；**同时把我第二、三轮误记 Pass 的 `PLT-003` 降级为 Blocked**（`runtime.json` 全树无读取者、界面层仍有三处 5078 字面量、实现方文档自己写明"此处不能结案"）。新登记 QA-P4-01（被测试守住的循环不在应用路径上）、QA-P4-02（禁令判据两处形状，UI-027 合流会撞同批文件）。551/0/2。
- [Phase 1 验收判定·第五轮（22:02）](reports/2026-09-21-phase1-acceptance-rerun5.md)：**仍不通过**，结构性阻塞仍只剩统一提交一条。`PLT-003`（作者裁定"1.0 需要独立进程客户端"→ `RuntimeDiscovery` 落地 + 真进程用例）与 `PLT-006`（统一 Runner 的客户端义务）转 **Pass**，`QA-P4-02`、`QA-P2-04①` 结案。新登记 `QA-P5-01`（活窗口接线零用例、5 例钉住不可达 `MainWindow` 路径）、`QA-P5-02`（**`PLT-*` 跨文档撞号**：迁移文档 §8 表用的还是采纳前编号，其 `PLT-006` ≠ 目录的 `PLT-006`）、`QA-P5-03`（`Card` 布局偏好不可达仍被回写）。613/0/2。
- [Phase 1 验收判定·第六轮（2026-09-22 01:25）](reports/2026-09-22-phase1-acceptance-rerun6.md)：**仍不通过，且阻塞面比第五轮宽一条**——门禁六轮里第一次转红：`HardwareProbeTimeoutTests.Killing_on_timeout_takes_the_whole_tree_not_just_the_shell` 在解决方案级并行连红 2/2（单跑与单程序集 5 次全绿），故 `REC-005` 由 Pass 降 **Fail**（`QA-P6-01`，Owner PLAT-001；全树终止属性今天只有这一例判，红即无判据）。上一轮剩下的 `QA-P5-01`（接线抽成 `MessengerEventStreamController` + 13 例，2 例跑真 Runner）、`QA-P5-02`（跨文档编号已按目录改键）、`QA-P2-04③`（整请求预算到期分支补用例）**全部结案**。新增 `PLT-013` 使目录升到 **1.1.2**；另登记 `QA-P6-02`（实施侧要求 OS 级证据 vs 目录 `scenario` 文本已满足——按文本对齐保 Pass，真要提口径得改文本并升版本）、`QA-P6-03`（启动顺序 + `MainWindow` 删除次序，合为作者裁定项）、`QA-P6-04`（"订阅起头有界"这条新判据无永久 ID 承接，我拒绝松挂）。627/1/2，测量脚本退出码 1。
- [最小闭环真机走查清单](minimal-loop-run-checklist.md)：**2026-09-22 作者裁定后的 Phase 1 结案标准**——四条必需步骤（启动到得到回复 / 重启后历史仍在 / 断一次后端有界收束 / 可指认提交 + 干净工作树复现），含证据格式与 `NotApplicable` 口径表。
- [Phase 1 第七轮 · 最小闭环走查判定](reports/2026-09-22-phase1-minimal-loop-walkthrough.md)：`eval-QA-P1WALK-0922`——**① Pass（36.9 s 真实往返、落库 1+1）／② Pass（重启后两条仍在、预览与描述符重建都对）／③ Blocked／④ 待裁定 ⇒ Phase 1 未结案、无 Fail**；新缺陷 `DEF-P1WALK-01`（模型内部草稿被当答复落库并渲染）与 F-1…F-4。
- [QA-P6-01 探测用例交接说明](reports/2026-09-22-qa-p6-01-probe-flake-handoff.md)：给 PLAT-001 Owner 的自包含信——同一工作区"实施侧 628 全绿 vs QA 627 带一红"的逐位对照、根因（前置条件挂在时间窗口上）、两种可接受修法与四种不可接受处置。
- [每轮验收记录模板](run-template.md)：工程证据、模型复核与 Memory 统计。
- [纵向用户观察模板](longitudinal/README.md)：真实反馈转回归，禁止把模板当成用户研究结果。
- [证据汇总脚本](../../scripts/measure-fixed-eval.ps1)：校验永久 ID 与测试映射；解析 TRX；不自动宣称角色验收通过。
- [SQLite 回归](../../tests/Pry.Core.Tests/EvaluationPersistenceTests.cs)：真实隔离数据库的投递、恢复、完整性验证；首轮记录的两项失败断言已由对应 Owner 修复。

## Phase 1 结案标准（2026-09-22 作者裁定，覆盖前六轮的阻塞面）

Phase 1 的 bar 从"六轮 QA 阻塞项全部结案"改为**最小闭环能跑通**："Client 启动后能通过 Core 调用模型，模型生成回复后 Core 能够保存并且返回给 Client，其他的先不管。"因此：① 结案动作＝[走查清单](minimal-loop-run-checklist.md)四条，其中第 ④ 条（可指认提交 + 干净工作树复现）吸收了此前唯一那条结构性阻塞（统一可审查提交）；② 六类角色感受场景、P50/P95、OS 级证据、EVAL-002 转 `NotApplicable`（永久 ID 与历史判定保留、不删不改语义），`PLT-010`/`PLT-012` 仍留 Blocked / NotRun；③ `QA-P6-01` 按裁定不再属 Phase 1 阻塞面，且 Owner 已按 QA 给的方向 A（前置条件事件化）修好，QA 于 2026-09-22 03:55 独立复跑连续两次 **628 / 0 / 2** 全绿、前置断言仍在 ⇒ **`REC-005` 复判 Pass、脚本退出码回到 0**；通用口径不变——**门禁退出码为 1 时，任何一轮都不许引用那一次的绿灯数字**。本文件其余章节（发布门槛、Memory 三项统计、分层覆盖）描述的是**1.0 与后续阶段**的标准，不因本次裁定而放宽；本次裁定只作用于 Phase 1 结案，不构成先例。

**④ 第七轮走查结果（2026-09-22 18:47–19:15，`eval-QA-P1WALK-0922`）**：裁定的那句话**已在本机成立**——① 一句话得到答复（端到端 36.9 s，DB 恰 1 条 user + 1 条 assistant，状态机走完），② 杀掉重开后两条仍在、顺序不变、房间预览＝最后一条、描述符以新 `pid`/端口/令牌重建，且陈旧描述符的遗留告警文案逐字命中。③ 记 **Blocked**：A′ 在 QA 手上不可达，因为发现文件的**写侧认 `Pry:DataDirectory`、读侧不认**（`App.axaml.cs:47` 无参调用 `RuntimeDiscovery.TryConnectAsync()`），要跑 A′ 就得让实验去开作者本人的 live 库——不自行执行，解阻条件见清单 §3.5。④ 仍等作者的提交决定（HEAD 未动，Phase 1 代码零提交）。**新增一条 High 缺陷 `DEF-P1WALK-01`**：模型对系统提示词的自我分析连同未剥离的 `</thinking><RESPONSE>` 信封被当 `model_reply` 落库并逐字渲染（真正的答复在信封里）——它不在四条字面判据内，故 ① 仍按 Pass 记，但已给 ① 的禁行结果补上对应形状，**是否升级为 Phase 1 出口条件归作者裁定**。

## 维护约定

场景 ID 不删除、不复用。语义修订要增加 suiteVersion、说明原因并保留历史；功能暂未实现时保留场景并记录 Blocked 及负责人。自动化映射失效必须修复，不准删场景让数字变绿。新增功能先描述可观察变化，再补场景与失败方式。Golden 指稳定的输入、状态与约束，不是固定台词。

suiteVersion 1.1.0（2026-09-21）：新增 `PLT-001`…`PLT-012`（category 仍为 Protocol，前缀独立以承载平台/协议宿主验收）与 `PER-005`；对既有 53 条只做证据映射追加（`REC-001`/`PRO-001`/`PER-001`），未改任何一条的 Scenario、约束或禁行结果。`PLT-*` 的 acceptance 判定同样只写在运行记录里，不放目录。

第五轮（2026-09-21 22:02）只做证据映射追加（`PLT-002/003/004/006`、`REC-001`），语义一字未改，故 suiteVersion 保持 1.1.1——**映射追加不升版本，语义修订才升**。同轮立下一条跨文档对齐约定：`PLT-001`…`PLT-012` 的**唯一权威定义是本目录**；`docs/protocol-versioning-and-websocket-migration.md` §8 尾表沿用的是采纳前的草稿编号（其 `PLT-004`=游标跨重启、`PLT-006`=大媒体引用，与目录分别对应 `PLT-005`、`PLT-010`）。因此任何人（含 QA 自己）按 `PLT-*` 单号读现状时，**必须先用 `scenario` 文本对齐，不得用 ID 对齐**；本轮 QA 就是照 ID 读该表写着"Blocked"而差点把已落地的 `PLT-006` 再压一轮。该缺陷登记为 `QA-P5-02`，修法是给该表每行补目录 `scenario` 文本或改用目录 ID。

suiteVersion 1.1.2（2026-09-21，Phase 1 第六轮）：新增 `PLT-013`（Protocol / High，房间代次门控：旧流不得改新房间的界面）。承载的是第五轮末落地、当时无场景判据的一类可观察行为——Runner 的四个回调在切房后仍可各自触发，"漏一条门控"只表现为 B 房状态栏被 A 房文案盖掉，属用户体验级而非崩溃级。既有 67 条一字未改；本轮其余变更为纯映射追加（`PLT-002/006/007/011`、`REC-001`），不升版本。**同轮一条口径提醒**：`PLT-*` 现有 13 条（`PLT-001`…`PLT-013`），派生视图（如迁移文档 §8 尾表抬头写的 12 条 / 1.1.1）请按上面的对齐约定随手刷新，不要反过来以派生视图为准。

suiteVersion 1.1.1（2026-09-21，Phase 1 第三轮更正触发）：新增 `REC-005`（Recovery / High，外部探测进程挂起时启动路径有界且可恢复）。起因是复核发现 4 条 `HardwareProbeTimeoutTests` 断言的"挂起饿死后续探测 / 只杀外壳留孤儿 / 取消与超时混同"三类可观察行为在目录里无场景承载——测试只在 Owner 文件里，删掉不会有目录级信号。既有 66 条仍一字未改。第三轮对 `PLT-001`/`PLT-002` 的变更是纯映射追加，不改语义。

`scenarios.json` 是场景规范；其中 `acceptance: NotRun` 是初始值，不是最新运行结果。实际结果放在独立运行记录中。`automatedTests` 仅为**部分证据映射**：例如 CRUD 测试不证明有用召回，SSE 解析不证明断线重连，SQLite reopen 不证明断电恢复，Pattern Selector 不证明模型真的遵守它。完整验收还须按场景运行真实链路并复核所有约束。

结果状态：Pass / Fail / Blocked / NotRun / Inconclusive / NotApplicable。最后一种只用于明确不在候选版本范围内的能力，必须写原因；1.0 必需但未实现的能力记 Blocked。跳过与无样本永不计入 Pass。未执行场景不能因映射测试通过而完成。

## 分层覆盖与证据

| 类别 | 固定范围 | 自动化起点 | 仍需的验收证据 |
|---|---|---|---|
| Timing TIM-001～008 | 聚合、typing、上限、打断、pending、stale、返回 | RuntimeComponentTests / ConversationRuntimeTests；SQLite fencing | 实际桌面事件到投递；真实延迟分布；客户端不显示 stale |
| Behavior BEH-001～007 | 沉默、简单确认、不盘问、不附和、不客服化 | CharacterIntelligenceTests 的决策/闸门 | 多轮真实输出是否遵守；结束话题与拒绝是否自然 |
| Expression EXP-001～005 | 旧梗、场景适用、口癖、Mood、抑制 | ExpressionSystemTests；Identity/Mood 规则 | 同角色不同模型/关系阶段对照；10 回合重复与风格观察 |
| Memory MEM-001～008 | 正确/错误/漏召回、更正、删除、隔离、降级 | 现有 CRUD；Empty/Unavailable 规则 | 真检索→Context→生成；墓碑/异步幂等；实例隔离；三项统计 |
| Relationship REL-001～004 | 初识、共同语境、慢变化、边界 | CharacterIntelligenceTests；进程内状态仓储 | SQLite/进程重启/换 Provider 后关系和边界连续 |
| Proactive ACT-001～006 | 动机、返回、Review 后变化、降速、时区、内容审核 | Scheduler/Policy/Delivery 的规则与真实 SQLite | Trigger→Candidate→Generate→Review→Send 全链；重复/误记忆/人格审核 |
| Capability CAP-001～003 | 改意图、故障/超时、权限 | Runtime capability 占位回调 | RT-007 真 Dispatcher/PluginContext、副作用取消边界 |
| Persistence PER-001～004 | 初始化、历史迁移、事务、备份恢复 | 新 SQLite 测试；已有主动消息事务测试 | 每个支持的历史 schema、迁移故障、损坏备份与恢复连续性 |
| Protocol PRO-001～004 | 重连、乱序/重复、顺序、幂等 | SSE 解析；新 SQLite 用例 | 客户端补历史与通知去重；目标 WebSocket 与版本协商 |
| Recovery REC-001～004 | Provider、crash、部分状态、后台任务 | HTTP 替身/Runtime/SQLite 清扫 | 真隔离进程强杀；半开连接；旧写回释放；故障后下一回合 |

## 可重复运行

先 `git status --short --branch` 并完整阅读 PROJECT_PLAN。并行开发时用独立 worktree。记录 commit、dirty diff 与**实际被构建的源文件 SHA-256**；仅写 HEAD 不能标识未提交代码。本轮工作区仍在变化时不可把某个快照的绿灯推广到当前版本。

在待验证的 worktree 根目录运行；每次使用全新结果目录：

```powershell
dotnet build Pry.slnx -c Release
# 只有上一步成功才执行 --no-build，禁止测陈旧程序集冒充当前构建。
dotnet test Pry.slnx -c Release --no-build --logger trx --results-directory TestResults/eval-RUN-ID --blame-hang-timeout 60s
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/measure-fixed-eval.ps1 -ResultsDirectory TestResults/eval-RUN-ID
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-repository.ps1
git diff --check
git diff --cached --check
```

`ExecutionPolicy Bypass` 仅作用于该 PowerShell 子进程，不修改机器策略。有 pwsh 的环境可直接用 pwsh。Catalog 校验可用 `-CatalogOnly`。汇总脚本退出码 1 表示测试失败或规范错误，2 表示已映射证据缺失/跳过；退出 0 仅说明已映射自动化证据可用，**不代表发布通过**。无映射的场景仍显示 Unmapped；完整验收保持 NotRun，交由运行报告决策。

TRX/日志/数据库只存忽略的 TestResults 或 `.local-notes`，不提交。故障注入只用生成的合成数据；数据库测试每次创建唯一临时目录。模型评测用隔离 `Pry__DataDirectory`，不要对日常数据库强杀、迁移或恢复。只终止本轮明确创建的进程。禁止将真实用户聊天、模型权重、密钥写入仓库。

## 工程与模型分开归因

确定性规则使用虚拟时钟或受控 Task 完成点，测 deadline、状态版本、事件次数和持久状态；不要只用 Sleep 猜状态。实际 Runtime/UI 另测端到端：输入、typing、first bubble、取消、candidate review、commit、event 与 client display 的时间戳。使用同一关联的 Conversation/Instance/Turn/Generation/Epoch，但普通 UI 不显示这些细节。

Timing 至少记录 input→decision、input→first bubble、typing-stop→release、cancel→pending-empty 的 P50/P95/max，以及 stale attempted / committed / displayed。禁止 stale 的门槛是后两者均为 0；“从未”是系统不变量，有限测试只能提供证据。等待上限依 RuntimeSettings + 已配置 Provider deadline + DeliveryHints 核验，不杜撰统一秒数。毫秒缩放的单元测试不能作为真实性能测量。

模型对照固定角色卡/版本/hash、初始关系、Mood、合成记忆、对话序列、时钟/时区、Prompt/Context 版本、Provider/模型/hash、temperature/seed/预算。默认本地轻量模型必须评；可选 API 分组记录。建议每个行为场景至少保留 3 次独立运行（有 seed 则固定种子），同时记录一致失败与偶发现象，不挑最好的一次。无模型时记 NotRun，替身输出只证明流程。

复核者看决策、输出、状态变化和违禁结果。关键词可预筛，不能凭“没有问号”判定不追问；相同句子在不同时间可能合理或不合理。LLM judge 若使用只作辅助，记录 judge 版本、rubric 与人工复核，不让模型为自己的结果直接发绿灯。拒绝逐字 Golden String；合成测试标记文本仅用来追踪消息身份。

## Memory 三项统计

逐条查询事先标注 `expectedFactIds`（此时应该用的有效事实），事后按证据人工标注 `usefulFactIds`、`falseClaims` 与原因；不能由模型自行声称命中。Useful 是正确、相关、作用域有效且真正用于输出的独立事实；False 包括虚构、错误、过期、已删、跨作用域或不相关却硬插入的记忆断言；Missing 是应该用但未用的事实。仅检索到、没有实际使用的不算 Useful。

每次查询分别计数，再汇总：U=Useful；F=False；M=expectedFactIds 中未正确使用的数量。一个回复可以同时有 U、F、M；同一回复重复同一事实不增加 U，重复问题另由 Behavior 统计。精确率 U/(U+F)、覆盖率 U/(U+M)、含错误召回查询占比同时报告分母；分母为 0 记 N/A，未跑记 NotRun，不能写成 100%。按本地/API、关系、正常/降级、相关/无关组分别统计。

诊断损失可用 `5*F+M`（暂定工程权重，非用户验证的统计结论），避免漏召回下降掩盖误召回上升。任何跨用户/实例泄漏或删除复活直接 Critical 阻塞，不以平均分抵消。三项计数和原始脱敏证据保留，不能只发综合分数。

## 发布门槛

| 优先级 | 例子 | 决策 |
|---|---|---|
| Critical | 数据丢失、隐私泄漏、跨实例记忆、不可恢复 crash、stale reply 错发 | 任一可复现即阻塞；相关必需场景未跑也不能验收该能力 |
| High | 严重失忆、持续重复、主动轰炸、关系错乱、输入打断失效 | 修复并回归后再验收；关键路径不以“模型偶然”免责 |
| Medium | 偶发风格不自然、表达不稳定；测试 oracle 错误另标 Test defect | 记录频率、触发条件、负责人、后续场景；测试错误不能用来推断产品错误 |
| Low | UI/措辞细节 | 登记；不扩大为泛化 UI 阻塞 |

发布记录同时检查：统一明确提交、构建/测试/仓库门禁、隔离新装/升级、默认本地无账号聊天、必需 Migration/Backup/Recovery/隔离、真实模型角色连续性及已获得的纵向反馈。未知不是通过，已知缺口写 owner/下一步。人数、等待天数、问卷分数不是硬门槛。QA 提出缺陷和小回归，不自行改造业务架构或接管其他 Owner 的文件。
