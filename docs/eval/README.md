# Pry 固定 Eval Suite

维护任务：EVAL-001（QA / Evaluation）；纵向反馈：EVAL-002。验收问题是：**同一个角色，是否在合适的时机，以符合她身份和共同经历的方式行动？** 内容正确、测试总数增加或构建成功都不能单独回答这个问题。

- [固定场景与约束](scenarios.json)：75 个永久 ID（suiteVersion 1.3.1），仍只 10 个 category；每项都有 Scenario、Input、Initial State、Expected Decision、Expected Output Constraints、Forbidden Outcome。
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
- [Phase 1 第八轮 · 最小闭环走查复跑](reports/2026-09-23-phase1-minimal-loop-walkthrough-rerun.md)：`eval-QA-P1WALK-0923`——**① Pass（22.4 s，档位 1.7B，落库 1+1）／② Pass（含"陈旧描述符指向死 pid"的新真机证据）／③ 首次 Pass（③a 0.01 s、③b 47.44 s ≤ 55 s，A′ 注入）／④ 未成立 ⇒ Phase 1 仍未结案，且唯一未过项不再是产品行为**；`DEF-P1WALK-02` 的存活探测按作者"维持 ≤55 s、改产品达标"裁定验证达标，`DEF-P1WALK-01` 第四次复现（形状已变，暂不定责）。
- [Phase 2（触发层）验收复跑（09-23 深夜）](reports/2026-09-23-phase2-trigger-layer-verification.md)：`eval-QA-P2TRIGGER-0923`——**§8.1 段② 签 `Fail`**，载体 `b079321` 的干净 `--detach` worktree，门禁 673 中 1 红（`RuntimeTriggerIntegrationTests.cs:88`，隔离 10 次 6 红、并行 6 次全红 ⇒ 竞态而非恒红），新缺陷 **`DEF-P2TRIGGER-02`** 派 Character Runtime Owner；§8 四条判据逐条给词（① Pass／② 部分成立／③ **Blocked**——`IsGroupChat`/`ReplyFrequency` 在 `src/` 内除 Runtime 自身外零生产者，已符号面独立复核／④ Fail）；§9 草案 15 行全部落裁决（新立 `TIM-009/010/011/012`、`BEH-008/009`，`BEH-007` 第一次有载体，`ACT-04`/`REC-04`/`PER-01`/`PER-03` 暂不立），目录升 **1.2.0**；另登记契约 §9 的 `Group_chater_` 锚点拼写错误与许可三处留痕复查通过。
- [Phase 2（结案标准）验收 · 2026-09-24](reports/2026-09-24-phase2-closure-verification.md)：`eval-QA-P2CLOSE-0924`——按 §8 **四条出口判据**逐条给词（载体 `44e52fc` 干净 worktree）：**④ 集成 Pass**（build 0/0、**连续三次 675/0/2**、`verify-repository` 394、`diff --check` 干净、`measure` exit=0 ⇒ **`DEF-P2TRIGGER-02` 结案**）、**③ 行为 Pass**（连发可合并／可不回复／插话取消旧输出／切会话不串状态四条各有真机+真实模型证据，另立两处限制：沉默来自宿主频率端口而非社交判断、内容侧不可评）、**②③ 合规与上游一致性 Blocked**（动作在作者：`DEF-P2TRIGGER-01` 逐值、`DEV-03/04/05/06` 的 `Pending`、`IsGroupChat` 降级落笔）⇒ **Phase 2 停在"QA 判定"，`阶段结案` 尚缺段③指针推进**。新登记 `DEF-P2TRIGGER-03`（Low，门控轨迹把"退避再施加"写成"阈值不足"）、`QA-P2CLOSE-01`（**测量脚本的"删号即红"守卫可在绿灯里失效**：无 BOM+LF+非 ASCII 注释 ⇒ 守卫空转，已修并双向反证）、`DEF-P1WALK-01` 第 7 次复现（本轮 4 例）；目录升 **1.3.0**（新立 `PLT-014`）。
- [RT-002 终验 A–D（仅验证）· 2026-09-24](reports/2026-09-24-rt002-final-verification.md)：`eval-QA-RT002FINAL-0924`——作者派四条、载体 `44e52fc` 干净 worktree、**零产品代码/零产品测试改动**：**A `NotAfter` Pass**（6 个使用点全是"最早可投递时刻"、旧名零残留、`ExpiresAt` 才承担截止语义；限制：测试面零条直接断言，且 `ShiftPending` 链无调用者）、**B Pass（对 1.0 生产者）／对字面口径不成立**（真实 SQLite 上乱序与重复被前缀门 fail-closed 拒掉，**跳号 `[0,2]` 与负号 `[-1,0]` 各投递 2 条并 close 计划**；`FakeRuntimeStore` 反而严格判拒 ⇒ 只看 fake 的用例是假绿）、**C Pass 三项全给**（抛出型 sink 探针：DB 保留 1 行、重启清扫 0 条 open 计划、换库重建后不重发；两处限制＝本轮剩余气泡被一起取消、活动侧不自动补投）、**D Pass**（RT-002 范围 131/0/0，解决方案级再采 1 次 675/0/2）。按作者令只登记 backlog：**`DEF-P2PLAN-01`**（Low，不阻塞 RT-002）。
- [QA-P6-04 承接与目录 1.3.1 · 2026-09-24](reports/2026-09-24-qa-p6-04-closure.md)：`eval-QA-P604CLOSE-0924`——作者裁 ③B 的执行轮，**不铸新号（仍 75 条）、语义修订升 `suiteVersion` 1.3.0 → 1.3.1**：`PLT-007` 的初态扩成"已建立后既不发也不关 ／ 订阅连响应头都不作答"两档并明写两档各用自己的时钟（映射 5→6），`REC-001` 补"分档预算＋兜底必须严格宽于最宽一档、每档为正数"（映射 7→8，**此半条是 QA 参照同一口径自承接，不在作者裁面内**）；反证到位：改一个映射名成 `…_NOPE` ⇒ `Stale test mapping`／exit=1，还原 ⇒ exit=0 且哈希逐位回到 `1a0b46832f4c`；同轮附一条自纠（"新建含假方法名的文件"不算反证、以及 PS5.1 `Set-Content -Encoding UTF8` 会补 BOM）。
- [Phase 2 参数标定协议（**提案，未采纳**）](calibration-protocol.md)：作者裁"保留量级，押 Eval 标定"之后，§8 第 2 条缺的不再是一个裁定而是一套判据——契约写"三个参数箱过 Eval"却没定义"过"。**方法论主干沿用同仓先例 `docs/memory-recall-calibration.md`**（两阶段分离、一次只调一组、dev/holdout、双标 + Cohen's κ、样本量决定结论形态、标定可能要改代码），本文只替换对象与归因。核心是六条实读事实：`TriggerMode` 默认 `MessageCount` ⇒ **整个必要性箱在发行链上不可达**；宿主注入 `TransitionalReplyAdvisor`（`src/Pry.Api/Services/ConversationSessionService.cs:286`）⇒ "该沉默会沉默"在真实 advisor 接线前不可评；`FocusActive` 硬编码 false、退避只作用群聊、`MessageIntervalStats` 无对外投影、trace 不含四层分数。据此每条样本必须先归入 **M-mode／M-gate／M-port／M-content** 四类之一，不可达的组一律走"出口三选一"而不是造证据。`目标值` 故意留空待作者与 Owner 写死；**本轮不铸永久 ID、不改任何参数与判据**。
- [Phase 2（结案标准）复验 · 2026-09-26](reports/2026-09-26-phase2-closure-rerun.md)：`eval-QA-P2CLOSE2-0926`——**同一载体 `44e52fc` 的第二轮独立取证，全部读数当场重采、不引用上轮数字**：**④ Pass**（build 0/0、**解决方案级 11 次跑法全绿 675/0/2、零红例**、`verify-repository` 394、`diff --check` 干净、`measure` 三份 TRX 各 exit=0）、**③ Pass 带两处上限**（`ConversationSessionService.cs:286` 仍注入 `TransitionalReplyAdvisor` ⇒ 沉默来自频率端口；`DEF-P1WALK-01` 在 `src/` 内仍零命中）、**① Pass 仅凭作者裁定**（载体上 `DEV-03..06` 仍是 `Pending`；使裁定可审计的 §5.2/§6/§9 改写完整存在但**只在未提交面**）、**② 仍 Blocked**（Owner 自己写明判据文本 `提案 · 未采纳 · 未运行`）⇒ **Phase 2 仍停在 `QA 判定`，段③ 不推进**。另记三件：并发负载探针（同 worktree 双开 `dotnet test`，8 宿主仍零红例 ⇒ 只**收窄**不推翻 `-03`/`-04`，修完才算）、Owner 的 `-03` 代码刀已落地且形状自反证（混树 676/0/2，**不构成段②**）、新登记 **`QA-P2CLOSE2-01`**（Medium，QA 名下：干净 worktree 只见目录 `1.1.2`/68 条 ⇒ 历轮 `measure exit=0` 都没覆盖新立的 7 条 ID，**唯一修法是把 `docs/eval/**` 与脚本提交**）。
- [Phase 3 落库刀复核 · 2026-09-28](reports/2026-09-28-p3state-01-storage-verification.md)：`eval-QA-P3STATE-0928`——**不是验收判定轮，无状态词**（载体是**未提交的主工作树**，HEAD 仍 `afecf1d` ⇒ §8.1 段② 不能签）。当场重采：build 0/0、定向 **47/0**（22 in-memory + 25 SQLite）、混树全量一次 **739/0/2**（如实标注"不互顶"）。独立验到三件事：**22 条继承断言 `git diff` 为空**（派工的原话约束、本刀唯一能假绿的通道是关着的）、分层边界守住、**没有新增第 22 处进程级清池**（附一条仪器自纠：按子串数到 22 是我的筛法错）。**13 处突变反证未重跑，按 §8.2 不引用为 QA 证据**。未接面四项逐个给 grep：四个适配器在 `src/` 内零构造点、`PurgeAsync` 零调用者、真机仍走过渡实现、**删卡对学习状态既不清也不挡**（`MemoryDatabase.cs:408-415` 的 EXISTS 只查 `conversations`/`memories`）⇒ **用户可见症状一字未变**。新登记 `QA-P3STATE-01`（Low，CHANGELOG 标题句与 §9 自己的免责句相反）、`QA-P3STATE-02`（Low，README 链向未跟踪的裁决协议文件，与 `QA-P2CLOSE2-01` 同族）、`QA-P3STATE-03`（接线时 High，应并入 RT-003 缺口 ②：**第一次写入的接线必须与 purge＋删除判定同批**，否则不可逆的孤儿行进发行链；缓和＝卡 id 是随机 GUID，无"新卡继承旧卡记忆"的形状）。
- [QA-P6-01 探测用例交接说明](reports/2026-09-22-qa-p6-01-probe-flake-handoff.md)：给 PLAT-001 Owner 的自包含信——同一工作区"实施侧 628 全绿 vs QA 627 带一红"的逐位对照、根因（前置条件挂在时间窗口上）、两种可接受修法与四种不可接受处置。
- [每轮验收记录模板](run-template.md)：工程证据、模型复核与 Memory 统计。
- [纵向用户观察模板](longitudinal/README.md)：真实反馈转回归，禁止把模板当成用户研究结果。
- [证据汇总脚本](../../scripts/measure-fixed-eval.ps1)：校验永久 ID 与测试映射（永久 ID 表与目录双向对账，缺号、多号、表停在旧极大值都直接 throw）；解析 TRX；不自动宣称角色验收通过。
- [SQLite 回归](../../tests/Pry.Core.Tests/EvaluationPersistenceTests.cs)：真实隔离数据库的投递、恢复、完整性验证；首轮记录的两项失败断言已由对应 Owner 修复。

## Phase 1 结案标准（2026-09-22 作者裁定，覆盖前六轮的阻塞面）

Phase 1 的 bar 从"六轮 QA 阻塞项全部结案"改为**最小闭环能跑通**："Client 启动后能通过 Core 调用模型，模型生成回复后 Core 能够保存并且返回给 Client，其他的先不管。"因此：① 结案动作＝[走查清单](minimal-loop-run-checklist.md)四条，其中第 ④ 条（可指认提交 + 干净工作树复现）吸收了此前唯一那条结构性阻塞（统一可审查提交）；② 六类角色感受场景、P50/P95、OS 级证据、EVAL-002 转 `NotApplicable`（永久 ID 与历史判定保留、不删不改语义），`PLT-010`/`PLT-012` 仍留 Blocked / NotRun；③ `QA-P6-01` 按裁定不再属 Phase 1 阻塞面，且 Owner 已按 QA 给的方向 A（前置条件事件化）修好，QA 于 2026-09-22 03:55 独立复跑连续两次 **628 / 0 / 2** 全绿、前置断言仍在 ⇒ **`REC-005` 复判 Pass、脚本退出码回到 0**；通用口径不变——**门禁退出码为 1 时，任何一轮都不许引用那一次的绿灯数字**。本文件其余章节（发布门槛、Memory 三项统计、分层覆盖）描述的是**1.0 与后续阶段**的标准，不因本次裁定而放宽；本次裁定只作用于 Phase 1 结案，不构成先例。

**④ 第七轮走查结果（2026-09-22 18:47–19:15，`eval-QA-P1WALK-0922`）**：裁定的那句话**已在本机成立**——① 一句话得到答复（端到端 36.9 s，DB 恰 1 条 user + 1 条 assistant，状态机走完），② 杀掉重开后两条仍在、顺序不变、房间预览＝最后一条、描述符以新 `pid`/端口/令牌重建，且陈旧描述符的遗留告警文案逐字命中。③ 记 **Blocked**：A′ 在 QA 手上不可达，因为发现文件的**写侧认 `Pry:DataDirectory`、读侧不认**（`App.axaml.cs:47` 无参调用 `RuntimeDiscovery.TryConnectAsync()`），要跑 A′ 就得让实验去开作者本人的 live 库——不自行执行，解阻条件见清单 §3.5。④ 仍等作者的提交决定（HEAD 未动，Phase 1 代码零提交）。**新增一条 High 缺陷 `DEF-P1WALK-01`**：模型对系统提示词的自我分析连同未剥离的 `</thinking><RESPONSE>` 信封被当 `model_reply` 落库并逐字渲染（真正的答复在信封里）——它不在四条字面判据内，故 ① 仍按 Pass 记，但已给 ① 的禁行结果补上对应形状，**是否升级为 Phase 1 出口条件归作者裁定**。

**⑤ 第八轮复跑结果（2026-09-23，`eval-QA-P1WALK-0923`）**：作者第三次裁定为**维持 ③b ≤55 s 的档位不动、改由产品达标**，Core 据此落地 `DEF-P1WALK-02`（订阅前先查一次本机监听表，只对"上一次连接被拒的同一个回环端点"生效，八档退避阶梯与 `PLT-002`/`PLT-006` 的判据实质均未放宽）。真机按清单 §3.5 的 A′ 复现命令重跑：**① Pass**（22.38 s 落定，档位 `qwen3-1.7b-q4_k_m.gguf`，DB 恰 1+1）、**② Pass**（硬杀重开后历史仍在，并新增一条此前只有代码推断的证据：指向死 `pid` 的陈旧描述符被拒→回退内嵌并重写描述符）、**③ 首次 Pass**（③a 0.01 s；③b **47.44 s** ≤ 55 s，与预测的 47.55 s 差 0.11 s；终止态文案逐字为"Pry 的本地服务没有在本机监听…"，稳定至 150.58 s）。门禁 671 / 0 / 2 连续两次、探测新增 12 例全绿、测量脚本退出码 0。**⇒ 四条必需步骤里只剩 ④ 未成立**，而 ④ 的动作是"把 `DEF-P1WALK-02` 那批形成一个可指认提交、再从干净 worktree 复跑 ①②③"——该批与 Character Runtime 在飞的 Phase 2 改动混在同一工作树里，QA 不代为分拣，**这一步是作者的决策，不是任何 Agent 的编码**。另：`DEF-P1WALK-01` 本轮第四次复现，形状已从"信封套答复"变为"裸 JSON 信封"，因 Phase 2 代码正在改动而**暂不定责**；`QA-P6-03`、`QA-P6-04` 不因 ③ 转 Pass 而自动关闭。

## 维护约定

场景 ID 不删除、不复用。语义修订要增加 suiteVersion、说明原因并保留历史；功能暂未实现时保留场景并记录 Blocked 及负责人。自动化映射失效必须修复，不准删场景让数字变绿。新增功能先描述可观察变化，再补场景与失败方式。Golden 指稳定的输入、状态与约束，不是固定台词。

suiteVersion 1.1.0（2026-09-21）：新增 `PLT-001`…`PLT-012`（category 仍为 Protocol，前缀独立以承载平台/协议宿主验收）与 `PER-005`；对既有 53 条只做证据映射追加（`REC-001`/`PRO-001`/`PER-001`），未改任何一条的 Scenario、约束或禁行结果。`PLT-*` 的 acceptance 判定同样只写在运行记录里，不放目录。

第五轮（2026-09-21 22:02）只做证据映射追加（`PLT-002/003/004/006`、`REC-001`），语义一字未改，故 suiteVersion 保持 1.1.1——**映射追加不升版本，语义修订才升**。同轮立下一条跨文档对齐约定：`PLT-001`…`PLT-012` 的**唯一权威定义是本目录**；`docs/protocol-versioning-and-websocket-migration.md` §8 尾表沿用的是采纳前的草稿编号（其 `PLT-004`=游标跨重启、`PLT-006`=大媒体引用，与目录分别对应 `PLT-005`、`PLT-010`）。因此任何人（含 QA 自己）按 `PLT-*` 单号读现状时，**必须先用 `scenario` 文本对齐，不得用 ID 对齐**；本轮 QA 就是照 ID 读该表写着"Blocked"而差点把已落地的 `PLT-006` 再压一轮。该缺陷登记为 `QA-P5-02`，修法是给该表每行补目录 `scenario` 文本或改用目录 ID。

suiteVersion 1.1.2（2026-09-21，Phase 1 第六轮）：新增 `PLT-013`（Protocol / High，房间代次门控：旧流不得改新房间的界面）。承载的是第五轮末落地、当时无场景判据的一类可观察行为——Runner 的四个回调在切房后仍可各自触发，"漏一条门控"只表现为 B 房状态栏被 A 房文案盖掉，属用户体验级而非崩溃级。既有 67 条一字未改；本轮其余变更为纯映射追加（`PLT-002/006/007/011`、`REC-001`），不升版本。**同轮一条口径提醒**：`PLT-*` 现有 13 条（`PLT-001`…`PLT-013`），派生视图（如迁移文档 §8 尾表抬头写的 12 条 / 1.1.1）请按上面的对齐约定随手刷新，不要反过来以派生视图为准。

suiteVersion 1.2.0（2026-09-23 深夜，Phase 2 触发层验收 `eval-QA-P2TRIGGER-0923`）：新增 `TIM-009`（攒不够阈值的消息自己醒来）／`TIM-010`（纯沉默多久都不成轮）／`TIM-011`（群聊空转指数退避）／`TIM-012`（多气泡间隔来自阅读时长）／`BEH-008`（回复频率调到零：只收不回）／`BEH-009`（群聊里没被点名就不开口），68 → 74 条；同轮对既有 5 条做纯映射追加（`TIM-001`、`TIM-002`、`TIM-004` 各补一条第二证据；`BEH-007` 从 `automatedTests: []` 第一次拿到载体）。**既有 68 条的 Scenario、约束与禁行结果一字未改**，升版本只因新增永久 ID。两点口径：(1) 草案的 `ACT/PER/REC` 前缀与目录同前缀**语义不同**（目录里 `ACT`=Proactive、`PER`=Persistence、`REC`=Recovery），采纳时按语义重排，不得照抄 `P2-Draft-*` 的名字铸号；(2) `ACT-04`、`REC-04`、`PER-01`、`PER-03` 四行**暂不立号**——前三行的判据依赖 §5.1 已被作者押后的逐值裁决或尚无判据，占号等于永久挂一条无法评的场景。

suiteVersion 1.3.0（2026-09-24，Phase 2 结案标准验收 `eval-QA-P2CLOSE-0924`）：新增 `PLT-014`（Protocol / High，回复频率经宿主配置走到活运行时：可校验、可回读、改值换实例）。承载的是 `44e52fc`（契约 §7 第 1 步"宿主端口"）落地的三类可观察变化——越界值 `400` 且错误字段为 `turnTaking.replyFrequency`、被拒那一次不留半个配置、配置一变必须换掉内存里那个运行时实例（外加一条 App 用例守"面板保存不得抹掉它从未展示过的字段"）。同轮 `BEH-008` 第二次拿载体（Api 侧 `Silent_frequency_reaches_the_live_runtime_and_never_opens_a_turn`，走的是真宿主路径而不是假时钟）。既有 74 条语义一字未改；`BEH-008` 的文本只在本轮**定稿前**按实跑载体校准过（初稿误写 `ReplyNecessity + 0.5`，真实载体是默认 `MessageCount` + `replyFrequency: 0.0`），而 1.2.0 从未提交，故不计一次语义修订。

**同轮一条测量脚本自身的更正（`QA-P2CLOSE-01`，QA 自有）**：`measure-fixed-eval.ps1` 的"删号即红"最小值表停在 v1（`TIM=8/BEH=7/PER=4`、无 `PLT`），1.1.x 与 1.2.0 新立的 **22 个号**在表里没有任何保护；现已把表补到当前极大值并加一条**双向对账**（目录出现表里没有的前缀、或表与目录极大值不等 ⇒ 直接 throw）。加这条的实测理由不是"怕忘了改表"，而是**这条守卫可以在绿灯里消失**：给表补上 `PLT=14` 后，我把 `PLT-014` 从目录删掉重跑，脚本仍报 `Catalog valid: 74` 且 **exit=0**。逐条件反证后定位到触发组合是**无 BOM + LF 行尾 + 非 ASCII 注释**——该组合下表所在行不生效、整个守卫空转；把注释改成 ASCII、或把行尾换成 CRLF，同一次删号立刻 `Permanent scenario removed: PLT-014` / exit=1（字节级机制未逐字节确认，结论只按实测组合给）。LF 的来路值得单记：`.gitattributes:13` 明写 `*.ps1 text eol=crlf`，而我这份工作副本当时是 `w/lf`——**原地编辑的工具不做行尾归一**，所以这个危害只咬"改过的工作副本"、不咬新克隆，因而能在多轮全绿里藏住。⇒ 本仓口径补充：**`.ps1` 的注释要么纯 ASCII、要么文件带 BOM（并按 `.gitattributes` 保持 CRLF）；新增守卫必须配一次"删掉判据看它是否真的转红"的反证，green 不等于在跑。**

suiteVersion 1.3.1（2026-09-24，作者按 `docs/decision-request-protocol.md` 选项式裁 ③B 的执行轮，`eval-QA-P604CLOSE-0924`）：**永久 ID 数不变（仍 75 条），本轮不铸新号**——作者的裁定原文是"不新增永久编号，改为扩写相邻条目"。两条语义修订：(1) **`PLT-007`** 的 `initialState` 扩为两档初态（已建立后既不发也不关 ／ **订阅连响应头都不作答**），`expectedDecision` 明写两档**各用自己的时钟**（心跳窗口管已建立流、逐请求预算管起头），`expectedOutputConstraints` 补"起头预算不得跟着 `HttpClient` 兜底一起放宽"，`forbiddenOutcome` 补"把兜底那一档当起头上限，一次无人应答的订阅把界面钉在四分多钟"；映射追加 `BackendRequestBudgetTests.An_event_subscription_that_never_gets_headers_fails_on_the_local_budget`（5 → 6 条）。(2) **`REC-001`** 的 `initialState` 补"客户端按请求分档配预算、兜底在最外一层"，`expectedDecision` 补"宽档请求不被共享兜底顶掉"，`expectedOutputConstraints` 补"兜底必须严格宽于最宽的逐请求预算，且每档都是正数——负数在 .NET 里是‘永不超时’而不是‘超时很长’"；映射追加 `BackendRequestBudgetTests.Transcription_gets_its_own_budget_without_loosening_the_shared_one`（7 → 8 条）。**归属如实分开**：第 (1) 条是作者已裁（③B）；第 (2) 条**不在提问面里**，是 QA 按 `QA-P6-04` 自家约定参照同一口径自承接，不得写成"作者裁了两项"。**为什么升版本而不只是追加映射**：本轮改的是两条的 `initialState`/`expectedDecision`/`expectedOutputConstraints`/`forbiddenOutcome` 文本，按本 README 约定"映射追加不升版本，语义修订才升"。**反证（按上一条口径必做）**：把新加的 `PLT-007` 映射名改成不存在的 `…_NOPE` ⇒ `measure-fixed-eval.ps1 -CatalogOnly` 报 `Stale test mapping` 且 **exit=1**；从字节级备份还原后 **exit=0**、SHA-256 回到 `1a0b46832f4c…`，另跑基线一次同为 0——证明映射校验确实在读 `tests/**/*Tests.cs`，不是空转。**本轮零代码、零测试改动**，两条被挂判据的用例在 RT-002 终验轮（`eval-QA-RT002FINAL-0924`）已随全量门禁绿过，故未重跑 `dotnet test`。全文：`reports/2026-09-24-qa-p6-04-closure.md`。

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
