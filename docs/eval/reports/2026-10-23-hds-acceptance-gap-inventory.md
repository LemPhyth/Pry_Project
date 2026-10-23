# 全范围验收盘点·零构建：HDS-001～012／v4 §14 四类验收／24 项反例 逐条对照盘上现状

- 报告 id：`eval-GAP-1023`
- 工单：`HDS-WAVE15-1023`（`docs/dispatch-hds-wave15-20261023.md`）
- 执行方：第十五波冷启动 lane；性质＝纯读码／纯文档，**零 `dotnet`**（不起构建、不起测试、不跑 `scripts/measure-fixed-eval.ps1`）
- 读取基点：主工作树 `codex/ui-main-window-redesign` @ `ac58245`（当场 `git rev-parse HEAD` 实测）
- 判定档只有三档：**已证／部分证／未证**。"已证"的唯一标准＝盘上有可执行判据且有名有姓（文件＋行号，或 `docs/eval/reports/` 某份报告的章节＋TRX 读数）。只有实现代码没有格守 → 部分证或未证。
- 门禁基线 `1179／0／2`（Client 108／App 233／Api 280／Core 558，executed 556）＝守门人登记在册（`docs/plans/PROJECT_PLAN.md:189`），本报告只引用、不重跑、不自封新数。

---

## 〇 给作者的一页结论（先读这几行，编号都在附注里）

1. **她现在实际能做的，还是老那一套**：打开程序、发消息、角色回话、关窗口进托盘、托盘退出。备份和恢复的能力已经写进程序、也有测试一格一格守着，但界面上点不到——对真实用户等于还没有（HDS-001）。
2. **新架构今天只到"契约"这一层**：身份、草稿校验、提交与投递账本这些规矩已经写进主线，并且有 62 格判据守着、改坏了会响（HDS-002）。真正"一次叙事决定说不说、说什么"的那个大脑（Narrator），**主线里根本搜不到**——它在另一条分支 `codex/wave13-20261023` 上，还没并回主线（HDS-003）。
3. **换架构的三大件——生命周期协调、真实落库投递、连续生活与主动推进——主线一件都没开工**（HDS-004／005 落库面／006／007／008／009 全未接）。所以"她活在一个连续世界里"这种新体验，用户今天获得为零。
4. **要结束还差四件**：①把分支上的原型并回主线并由另一双手复采；②把契约落到数据库、把新链接进真实收发；③真机跑一次小模型对照，确认她说话自然、延迟能忍；④出一个能装、能恢复、托盘真退出的候选包。
5. **只有你能拍板的有 8 件**（详见表 C）：打 tag、推送、真打包、托盘那一次人工点击、点头允许跑真实模型、改素材许可页那句旧许可名（`ASSETS_LICENSE.md:11` 今天仍写 GPL-3.0，与代码的 AGPL-3.0-only 不符）、联网做依赖锁定、决定旧链路何时退役。
6. **一句话**：地基（可恢复＋身份契约）打了桩也守了格，房子还没盖；最快的下一步是先让原型回家，再让新链在你机器上真说一句话。

---

## 表 A｜HDS-001～012 十二项实施任务（来源＝`docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md:319-331` 交付与验收列＋前置列；状态列＝`docs/plans/PROJECT_PLAN.md:70-83`）

| 任务 | v4 要求逐字摘要 | 盘上做到了哪一层（文件＋行号／是否归位到线尾） | 判定 | 差的那一件事（大白话） |
|---|---|---|---|---|
| HDS-001 冻结资产与可恢复地基（v4:320，前置＝无） | "保全他人材料与分支成果；一致性备份、隔离恢复、未知高版保护；无真实数据覆盖" | 实现在线尾：`src/Pry.Core/Backup/` 七份（`DataBackupService.cs` 176 行、`DataRestoreService.cs` 164 行、`SqliteBackupDiagnostics.cs` 292 行、`BackupBundleManifest.cs` 103 行、`BackupInclusionPolicy.cs` 82 行、`RestoreVerificationReport.cs` 21 行、`IBackupFileCopier.cs` 35 行）＋写权门 `src/Pry.Core/Narrative/DataDirectoryWriteGate.cs`（169 行）＋`src/Pry.Api/Services/DataDirectoryWriteLease.cs`；判据在册＝`tests/Pry.Core.Tests/DataBackupServiceTests.cs`（10 枚 `[Fact]`，`:103`～`:300`）、`DataRestoreServiceTests.cs`；归位链＝`docs/plans/PROJECT_PLAN.md:72`（`8ef7d01`＋片 A `b18ae27`/`a0f7bb1`/`452cd4c`＋片 B `374ecd8`/`d2802ea`，代码面线尾 `6fc7dbb`）。盘上反证：`grep -rln backup src/Pry.Api src/Pry.Client src/Pry.App` 无用户入口（仅 `ConfigurationApplicationService.cs:55` 一处无关的同名文件改名）＝**没接生产路径**（同 `PROJECT_PLAN.md:72` 自述） | 部分证 | 她在界面上还按不到"备份／恢复"这个动作，这套本事只有测试能用——差的是接线与触发时机（那是产品决策，见 U-2／U-11，`docs/narrative-open-items-memo.md:50-52`、`:163`） |
| HDS-002 身份／存储契约（v4:321，前置＝001 数据保护） | "稳定实例映射、叙事/意图/输入处理/outbox 契约、版本迁移与回退；热区写入归属明确" | 类型与校验器在线尾：`src/Pry.Core/Narrative/` 八份——`NarrativeDocuments.cs`（437 行；`:162 NarrativeRequest`、`:259 CommunicationAction`、`:284 NarrativeIntent`、`:302 KnowledgeCandidate`、`:322 NarrativeDraft`、`:353 NarrativeCommit`、`:391 DeliverySegment`、`:421 DeliverySegmentKey`）、`NarrativeDraftValidator.cs`（420 行，`:105` 校验器、`:40` 原文绑定）、`CharacterInstanceIdentity.cs` 487 行、`DeliveryLedger.cs` 262 行、`OutboxOwnershipRegistry.cs` 237 行、`NarrativeCommitWriter.cs` 199 行、`NarrativeContractReason.cs` 143 行；判据在册＝`tests/Pry.Core.Tests/NarrativeIdentityContractTests.cs`（540 行，当场实测 `[Fact] 21`／`[Theory] 4`／`[InlineData] 41`＝**62 格**）与 `NarrativeStorageContractTests.cs`（`:56`～`:436` 三十余枚 Fact）；归位＝`c7f52ea`＋`a5d9b80`（在线尾 `ac58245` 之内），20 枚单枚清洁红登记在 `docs/plans/PROJECT_PLAN.md:187-188`，基线 1179／0／2 在 `:189`。盘上反证：`grep -rn "narrative_entries\|character_instances\|narrative_intents" src/` **零命中**＝没有表（`PROJECT_PLAN.md:73` 自述"不落表、不接线"） | 部分证 | 规矩写得很全也有人守格，但**没有任何一份叙事真的存进数据库**——她关掉再打开，今天靠的还是老那套数据，不是这套契约 |
| HDS-003 新生成中心隔离原型（v4:322，前置＝本文；与 001/002 可隔离） | "七段 Compiler、一次 Narrator、原文行动绑定、沉默/延迟、真实本地小模型与 API 对照；不接生产库" | 主线**零命中**：`grep -rn "Narrator" src/ --include=*.cs` 返回空。该把在另一条分支：`git log --oneline -1 codex/wave13-20261023` ＝ `98246cd feat(narrative): HDS-003 新生成中心隔离原型……49 格新判据＋15 枚单放反证随刀入库（不接生产库、不接线 API/Client、不碰真实模型）`——**未归位到线尾**；配套报告 `docs/eval/reports/2026-10-23-hds003-narrator-prototype.md` 在主树是未跟踪文件（他人名下在途物，本波按边界未读内容） | 未证（未归位，载体＝`codex/wave13-20261023`） | 房子的大样图在别人的工具箱里，还没搬进工地——差的是守门人把它并回主线并由另一双手复采，之后才轮到你判断"她说得自然不自然" |
| HDS-004 叙事生命周期（v4:323，前置＝002；003 语义验证） | "输入范围/幂等、实例单写者、公平调度、取消、删除/重配置失效、预算与失败语义" | 盘上没有协调器：`src/` 无对应实现，真实入口仍是旧链 `src/Pry.Core/Runtime/ConversationRuntime.cs`、`ReplyGate.cs`、`TurnScheduler`；契约层只有类型与校验格（上两行）。在册的真前置判断＝`docs/plans/PROJECT_PLAN.md:75`"真前置是 U-9（接线与灰度时机，需作者拍）与 HDS-003 的语义结论"，U-9 原文＝`docs/narrative-open-items-memo.md:24`、`:132-139` | 未证 | 还没有"同一时刻只跑一笔、你插话能把旧草稿顶掉"的真调度在跑——今天跑的还是老那套排队 |
| HDS-005 原子提交与投递（v4:324，前置＝002/004） | "叙事/输入处理/outbox 原子提交，逐段真实历史、SSE 补取、部分送达/崩溃矩阵" | 账本与写者的**形状有格守**（契约级）：`src/Pry.Core/Narrative/DeliveryLedger.cs`、`NarrativeCommitWriter.cs`；对应判据在 `tests/Pry.Core.Tests/NarrativeStorageContractTests.cs:156`（同键不可二次提交）／`:175`（段号不可跳）／`:192`（段正文必须逐字取自带绑定原文）／`:208`（已投递为终态、迟到结果不可翻转）／`:226`（前段未投不得标后段）／`:241`（须有真实历史证据才叫 delivered）／`:281`（失权世代的迟到提交被拒）／`:341`／`:352`（故事版本过时或超前）／`:361`（重放不二次消费）／`:403`（删除后迟到提交不能复活会话）／`:427`（无内容散列不能自证）。落库面为零：真实发送仍走旧 reply plan（`docs/narrative-open-items-memo.md:88` U-5）；崩溃/部分送达矩阵在真实库上没有任何一格 | 部分证 | 真正发出去的那句话今天不经过这套账本——差的是把账本接到真实数据库与真实收发上，并在真库上跑一遍崩溃/半发矩阵 |
| HDS-006 记忆与删除全链（v4:325，前置＝002；生产接线依赖 005） | "现实/虚构/认知身份、统一检索、证据来源、摘要/向量/缓存失效、撤销 CAS；保留旧 .5 门" | 契约成员有格：`NarrativeIdentityContractTests.cs:351`（五类认知 × 六枚来源矩阵 25 行）、`:376`（无证据引用的确认被拒）、`:395`（跨实例意图被拒）。**全链没有**：叙事侧无原文/摘要/向量/缓存的派生物，也就谈不上失效；撤销 CAS 的旧链判据在册（`docs/plans/PROJECT_PLAN.md:82` 之外另见 `tests/Pry.Core.Tests/MemoryDeletionSeparationTests.cs`），但那是旧链语义。已登记的契约缺口＝`CognitionKind.CharacterBelief` 零条款（`docs/plans/PROJECT_PLAN.md:199`） | 未证 | 删掉一段对话后新链的副本会不会漏，今天没有任何东西在管——因为新链本身还没接上，管它的前提不存在 |
| HDS-007 有界连续推进（v4:326，前置＝004/005；006 最小来源约束） | "用户事件、到期意图、后台场景同入口；冷却、静默、费用/调用预算、长离线恢复" | 主线零实现：`grep -rni "SceneAdvance\|IntentDue" src/` 零命中。现存的主动消息那套是**独立旧入口**：`src/Pry.Api/Services/ProactiveMessageGenerator.cs`、`ProactiveScheduler.cs`；其判据在册但守的是旧链（`tests/Pry.Api.Tests/ProactiveSchedulerTests.cs:15`／`:31`／`:72`／`:101`／`:113`）——这正撞 v4 §14.1 第 2 条"不存在第二个后台回复生成器"的反面 | 未证 | 她不会"自己过日子"，也不会到期想起一件事再开口——现在的主动消息还是老的那条单独管线在发 |
| HDS-008 客户端与协议迁移（v4:327，前置＝004/005 接口冻结） | "保持当前消息/SSE 语义，明确等待/失败/主动许可；不将原文/内部推理显示为聊天；契约齐全" | 客户端与 API 没有任何叙事消费面：`grep -rln -i narrative src/Pry.Api src/Pry.Client src/Pry.App` 只命中 `DataDirectoryWriteLease.cs` 与 `RuntimeDescriptorService.cs` 两处与叙事无关的写权面；`docs/api-v1.md` 仍按 v4 §1（`v4:36`）描述今天接口。旧链的客户端判据在册（`tests/Pry.Client.Tests/` 14 份：`DeadLoopbackReconnectTests.cs`、`StaleSessionRecoveryTests.cs`、`ProactiveClientTests.cs` 等） | 未证 | 界面上看不到任何"她在想/没发出去/已取消"的叙事状态——协议和窗口一个字没为叙事改过 |
| HDS-009 状态与显式学习迁移（v4:328，前置＝005/006） | "Canon 与 Overlay 边界、关系/心情/经验数据继承、投递后与点击证据归因；旧模式单向隔离" | Canon／Overlay 只在计划文字里（`v4:130`），`src/Pry.Core/Narrative/NarrativeDocuments.cs` 无对应类型；旧关系/心情/经验数据仍在旧服务（`CharacterStateService` 一族，`v4:82` 列为"保留数据"），没有迁移器，也没有"新链读旧数据"的任何一格 | 未证 | 现在这套关系、心情、说话习惯的数据还没有通往新链的桥——换过去会不会丢，今天没有任何东西证明过 |
| HDS-010 生产切换与旧链退役（v4:329，前置＝003~006/008/009 关键验收） | "每实例单引擎切换、清空/迁移旧 pending，移除旧评分/Planner 主链及无用窗口；保留版权与有效回归" | 旧主链四件**仍全数在线且被引用**：`src/Pry.Core/Intelligence/BehaviorPlanner.cs`（另被 `CharacterIntelligenceService.cs` 引用）、`src/Pry.Core/Intelligence/ReplyGenerationRenderer.cs`（被 `src/Pry.Api/Runtime/CharacterIntelligenceGateway.cs`、`src/Pry.Core/Prompting/ContextBuilder.cs` 引用）、`src/Pry.Core/TurnTaking/ReplyPlanner.cs`（被 `ConversationTurnManager.cs` 引用）、`src/Pry.Core/Runtime/ReplyNecessity.cs`（被 `Program.cs` 等引用）。新引擎模式开关零实现（唯一在册形状＝单写者契约格 `NarrativeStorageContractTests.cs:105`"一份写权不能同时挂两种引擎模式"） | 未证 | 旧的那套"该不该说话"的判断还在替她做决定——退役要先有你一句裁定（表 C 第 8 项） |
| HDS-011 安全/性能/发行基础（v4:330，前置＝可独立修旧缺陷；新链检查依赖 005） | "DOCX/文本限额、模型资源/网络边界、长历史和大记忆、依赖锁定、版本及许可证" | 已归位的切片（链与 SHA 全在 `docs/plans/PROJECT_PLAN.md:82`）：AUD-01/06/04＝`ae67267`→`faf8ecf`（负输入预算的换算格＝`tests/Pry.Core.Tests/ContextBudgetTests.cs:51`、`:58`、`:76`、`:88`；形状源 `src/Pry.Core/Prompting/ContextBuilder.cs:24`、`:39`）；AUD-07＝`1edf69e`＋`c148fbf`（`tests/Pry.Api.Tests/StickerContentLengthGuardTests.cs`）；HDS-011c＝`e554483`/`bca8b42`/`5593e1a`（依赖与许可机械面 `tests/Pry.Api.Tests/UpstreamProvenanceAndDependencyShapeTests.cs`；资源限额总盘 `docs/resource-limit-ledger.md` 116 行＋覆盖格 `tests/Pry.Api.Tests/ResourceLimitLedgerCoverageTests.cs`）；第五波＝`2eba867`/`e31e4da`/`dc5b010`（`dc5b010`＝写锁提前到第一次写盘前，判据 `tests/Pry.Api.Tests/RuntimeDescriptorWriteGateTests.cs`、`DesktopCapabilityGuardTests.cs`）。附件限额格＝`tests/Pry.Core.Tests/AttachmentLimitTests.cs`。**未做**：真·依赖锁定——盘上 `Directory.Packages.props` 与 `packages.lock.json` 均不存在（当场 `ls` 实测），等作者点头（`docs/plans/PROJECT_PLAN.md:82`、`:424`）；长历史与大记忆规模面、可复现构建面未开工 | 部分证 | 依赖版本今天仍然可以漂——同一份源码换台机器可能装出不一样的东西；做这件事要联网，得你点头 |
| HDS-012 独立验收与候选包（v4:331，前置＝007/010/011；发布另获明确指令） | "新链模型证据、故障回归、dogfood、恢复/退出/安装、已知限制与统一 SHA 包" | 无候选包：`scripts/package-release.ps1` 存在（`:4` 默认版本 `0.0.2`、`:34-35` 脏树门、`:76` 模型 SHA 校验）但从未在叙事链上产出过候选；旧线在册欠账原文＝`docs/eval/README.md:25`"（`0-4`（`RT-008-PKG`）未开始，前置＝作者给 merge/tag/push 顺序与三条 `CHANGELOG.md` 入库"；托盘侧现有判据只到形状级（`tests/Pry.App.Tests/TrayResidencyBoundaryTests.cs:38/54/65/78/88/96/109/126/134`、`WindowLifecycleTests.cs:17/24/28/43`），真实那一次人工点击未做（在册登记＝`docs/plans/PROJECT_PLAN.md:96`、`:252`） | 未证 | 还没有一个"能装、能恢复、点托盘真退干净"的交付包——而且这一步里有几件只能你亲手做（表 C） |

### 表 A 三档计数

- 已证：**0 行**（十二行里没有任何一行能声称"用户实际能完成的行为"已被盘上判据完整守住）
- 部分证：**4 行**（HDS-001、HDS-002、HDS-005、HDS-011）
- 未证：**8 行**（HDS-003〔未归位，在 `codex/wave13-20261023`〕、HDS-004、HDS-006、HDS-007、HDS-008、HDS-009、HDS-010、HDS-012）

---

## 表 B｜v4 §14 的四类验收

### B1 §14.1 架构身份验收（原文＝`Pry_HDS_Rewrite_Architecture_Plan_v4.md:347-354`）

先记一条**总前置未证**：§14.1 首句要求"必须提供从生产输入入口到 Narrator、Committer、Delivery 的一次可追踪记录"（`v4:349`）。主线上不存在这样一条记录（`grep -rn "Narrator" src/` 零命中，见表 A 的 HDS-003 行），所以下面四条里凡涉及"新路线真跑起来"的部分都只能在契约层给证据。

| # | §14.1 要求逐字摘要 | 盘上证据 | 判定 | 差的那一件事 |
|---|---|---|---|---|
| 1 | "禁用/替换旧 `BehaviorPlanner`、`ReplyGenerationRenderer` 和 MaiCore 必要性评分，不会破坏新路线生成；新路线没有偷偷回落旧链" | 旧三件**全部仍在线且被生产引用**：`src/Pry.Core/Intelligence/BehaviorPlanner.cs`（被同目录 `CharacterIntelligenceService.cs` 用）、`src/Pry.Core/Intelligence/ReplyGenerationRenderer.cs`（被 `src/Pry.Api/Runtime/CharacterIntelligenceGateway.cs` 与 `src/Pry.Core/Prompting/ContextBuilder.cs` 用）、`src/Pry.Core/Runtime/ReplyNecessity.cs`（被 `src/Pry.Api/Program.cs`、`src/Pry.Core/Runtime/RuntimePorts.cs` 用）；新路线本身在主线不存在 → 没有任何一格能"摘掉旧三件后仍跑通新路线" | 未证 | 得先把原型并回主线，再把旧三件真摘掉跑一遍，看她还能不能说话——今天连"新路线"这个主语都还不存在 |
| 2 | "普通输入、主动/到期事件都进入同一主叙事契约；不存在第二个后台回复生成器" | 反面证据在盘上：第二条独立生成器确实存在——`src/Pry.Api/Services/ProactiveMessageGenerator.cs` 与 `ProactiveScheduler.cs`，走的是旧回复计划通路；新契约只有类型形状（`src/Pry.Core/Narrative/NarrativeDocuments.cs:93` 的 `NarrativeTriggerKind` 枚举，没有任何入口消费它） | 未证 | 主动消息还没并进同一个入口，"到期意图/场景推进"这两个触发今天没有实现体 |
| 3 | "Narrator 输出沉默时无助手消息；它提出即时通信时正文由当前叙事动作取得，没有第二次独立改写" | 契约级判据有名有姓且拿到过实测红：`tests/Pry.Core.Tests/NarrativeIdentityContractTests.cs:246`（`Silence_advances_the_watermark_without_perceiving_anything`）、`:276`（`Immediate_text_must_locate_itself_in_the_narrative_once`，五类坏引用各钉一种拒绝理由）、`:285`（定位不到就拒绝而不是改写）；绑定实现面 `src/Pry.Core/Narrative/NarrativeDraftValidator.cs:40`（`Bind`）与 `:90`（`Apply`）；单枚清洁红登记＝`docs/plans/PROJECT_PLAN.md:187-188`（20 轮全 62／fail 1，红格与工单预期表 20/20 对平） | 部分证 | 契约层"沉默＝没有消息、正文必须来自同一段原文"已经会被响的门守住了；差的是真实模型跑出来的沉默与短句一条记录都还没有（那是表 B3 与表 C 第 5 项） |
| 4 | "移除叙事状态/原文来源或把正文换成另一个提案，绑定与连续性测试必须报警。仅扫类名或提示词字符串不算验收" | 会响的门逐枚在册：`tests/Pry.Core.Tests/NarrativeStorageContractTests.cs:192`（段正文必须逐字取自带绑定的叙事原文）、`:270`（从未 apply 的提交不得追加段，锚 `:270` 的正向探针在第十波由第二双手复采＝`docs/eval/reports/2026-10-22-qa-wave10-three-evidence-debts.md` §二 读数 `run2 total 30／failed 1` 且 `:270` 保持 Passed）、`:341`／`:352`（故事版本过时或超前）、`:373`（同一 commit id 换内容被拒）、`:383`（水位不可倒退）、`:427`（无内容散列不能在崩溃后自证）；这些格拿到过单枚实测红的登记＝`docs/plans/PROJECT_PLAN.md:187`（20 枚突变轮）与 `:259`（第九波 21 格全付） | 部分证 | 契约层的"换正文/删来源必须报警"已有实测红；但这条要求的主语是**生产链上的绑定与连续性**——生产链没接，所以整条只能算契约面已证、端到端未证 |

**B1 小结**：4 条＝已证 0／部分证 2／未证 2。

### B2 §14.2 的 NR-01～NR-24 十二条反例（原文＝`v4:356-374`；本轮不新建场景，只回答"今天有没有可执行的等价判据"）

| 引用 | 必须验证（v4 逐字摘要） | 盘上有哪一格守它 | 判定 |
|---|---|---|---|
| NR-01/02 | 凭少量 Canon＋初始处境产出同源回应；连续两轮不重置场景 | **没有任何格**：Canon/初始场景在新链零实现（`src/Pry.Core/Narrative/NarrativeDocuments.cs` 无 Canon 类型），产出同源回应要靠 Narrator——只在分支 `codex/wave13-20261023`（`98246cd`），主线零命中 | 未证 |
| NR-03/04 | 明确沉默无消息无成功学习；延迟只建意图，不提前进真实历史 | 契约格：`NarrativeIdentityContractTests.cs:246`（沉默只推水位）、`:299`（`Deferred_action_without_intent_is_a_promise_nothing_can_keep`）、`:320`～`:351` 意图侧矩阵；旧链旁证 `tests/Pry.Core.Tests/ConversationRuntimeTests.cs:216`（`An_empty_plan_from_the_model_is_reported_as_silence_not_as_failure`，守的是旧语义）。**"沉默不产生可学证据"这一半没有任何格** | 部分证 |
| NR-05/06 | 请求只见 101 时不能消费 102；首泡前插话阻断旧提交、原输入可正确合并 | 契约格：`NarrativeIdentityContractTests.cs:220`（感知引用必须来自已发批次）、`:236`（水位不得越过批次）、`NarrativeStorageContractTests.cs:394`（不得消费宿主未收到的输入）、`:361`（重放不二次消费）；旧链行为格 `ConversationRuntimeTests.cs:150`（开始打字取消仍在排队的输出）与 `:98`（无视取消的迟到结果仍不投递）。**新链的"首泡前插话→旧提交被阻断"没有可跑路径**（无协调器，见表 A HDS-004） | 部分证 |
| NR-07/08 | 部分送达后插话只保留已发段；删除/清空使迟到结果永久无资格且不重新入队 | `NarrativeStorageContractTests.cs:208`（delivered 终态、迟到结果不可翻转）、`:226`（前段未投不得标后段）、`:403`（删除后迟到提交不能复活作用域）；旧链 `ConversationRuntimeTests.cs:124`（首泡后插话只取消第二泡） | 部分证 |
| NR-09/10 | 写入前崩溃/提交后崩溃/历史成功但 SSE 丢失均不重复生成；坏响应不能伪装合法沉默 | `NarrativeStorageContractTests.cs:427`（无内容散列不能在崩溃后自证）、`:156`（同键不可二次提交）、`:270`；SSE 断线补取的等价格在客户端旧协议面 `tests/Pry.Client.Tests/StaleSessionRecoveryTests.cs:44`、`:102`；坏响应侧＝validator 直接拒绝（`NarrativeIdentityContractTests.cs:285`）。**"部分送达后崩溃不重复生成"在真实库/真进程上零格**（HDS-005 落库面未开工） | 部分证 |
| NR-11/12 | 两用户/两角色所有原文、记忆、向量、摘要、后台任务均隔离；同实例跨会话不串短期上下文 | 新链契约格：`NarrativeIdentityContractTests.cs:204`（草稿自选实例不算提案）、`:395`（跨实例意图被拒）、`NarrativeStorageContractTests.cs:417`（未注册实例无处归属）、`:105`（一份写权不能同时挂两种引擎模式）；旧链等价格 `tests/Pry.Core.Tests/MemoryContextTests.cs:142`（`Same_character_instances_do_not_share_private_memories_or_short_history`）。**向量/摘要/缓存三类派生物今天不存在，隔离无从守** | 部分证 |
| NR-13/14 | "她想象用户同意"不能生成用户确认；角色虚构生活不能进入现实用户事实 | `NarrativeIdentityContractTests.cs:351`（五类认知 × 六枚来源矩阵 25 行，按来源决定可断言什么）、`:376`（无证据引用的确认被拒）；旧链 `MemoryContextTests.cs:71`、`:77`（归因与引用材料 fail-closed、伪造助手证据被拒）。已登记缺口＝`CognitionKind.CharacterBelief` 零条款（`docs/plans/PROJECT_PLAN.md:199`） | 部分证 |
| NR-15/16 | 删除来源后原文/摘要/向量/缓存不泄漏；手工编辑后撤销不覆盖新证据，显式遗忘不复活 | 旧链有成套格：`tests/Pry.Core.Tests/MemoryDeletionSeparationTests.cs:68/90/104/119/133/162`（含 N5 撤销恢复全部脱链字段、N6 撤销不能复活用户显式遗忘）＋`MemoryContextTests.cs:116`、`:131`；**叙事侧原文/摘要/向量/缓存的删除联动没有任何格**（派生物不存在） | 部分证 |
| NR-17/18 | 默认模型不降档时预算有效、全部指令计入；坏 JSON/Provider 超时与中断有界退出 | 预算侧成套装尺格：`tests/Pry.Core.Tests/ContextBudgetTests.cs:51`（原文等值负输入预算）／`:58`（`ForModelWindow` 把等值窗口转成正输入预算）／`:76`／`:88`，加上第十一波新钉的确切换算两格（归位 `0ba5b30`，登记 `docs/plans/PROJECT_PLAN.md:207-208`）；计入侧＝`src/Pry.Core/Prompting/ContextBuilder.cs:8` 把 Advisory 2400 计入构造参数（AUD-06 的旁路已收进预算）；有界退出侧＝`tests/Pry.Core.Tests/ModelProviderErrorTests.cs:21-33`（超时/限流/5xx 映射可重试与不可重试两类）。**注意 W2 那一格至今"够不着"**：`ContextWindowLoadTests.Every_downgrade_candidate_keeps_a_positive_input_budget` 没有实测红，欠账登记＝`docs/plans/PROJECT_PLAN.md:234`、`:211`（分母 46／已付 45／欠 1＝W2） | 部分证 |
| NR-19/20 | 后台推进不自动获得发消息许可；长离线/休眠恢复不补跑风暴、安静时段不打扰 | 旧链主动面成套格：`tests/Pry.Api.Tests/ProactivePolicyTests.cs:19/64/84/127`、`ProactiveSchedulerTests.cs:72/101/113`（活动或前台变化取消且不留幽灵消息、失败退避不发消息、未加载模型不消耗尝试）、`tests/Pry.Core.Tests/ProactiveDeliveryTests.cs:13/32/59/85`（权限与配额同事务、失败事务无幽灵消息、中断认领保冷却）。**"推进许可与发消息许可分开"这条在新链零格**（新链无 SceneAdvance） | 未证 |
| NR-21/22 | 高压缩比 DOCX/过大正文在分配前拒绝；只带贴纸、附件或空文本的合法性明确 | 成套且有名有姓：`tests/Pry.Core.Tests/AttachmentLimitTests.cs:55`（超限解压不落半份正文）／`:68`（高压缩比拒绝）／`:80`（超深嵌套在完整解析前拒绝）／`:93`（节点数超限）／`:105`（超长属性名）／`:115`（尊重取消不吞掉）／`:126`（缺部件报读取失败而非限额）；限额先在分配前＝`src/Pry.Core/TurnTaking/AttachmentTextExtractor.cs:14-17`（8 MiB／比 100／深度 200）与 `:66-67`、`:86`；贴纸与正文同一把尺＝`tests/Pry.Api.Tests/StickerContentLengthGuardTests.cs:56/69/80/98/109`；上限集中登记＝`docs/resource-limit-ledger.md`（116 行）＋覆盖格 `tests/Pry.Api.Tests/ResourceLimitLedgerCoverageTests.cs` | 已证 |
| NR-23/24 | 迁移中断可恢复、未知高版不写；隔离恢复的角色/设置/历史/记忆/媒体一致 | 成套且有名有姓：`tests/Pry.Core.Tests/MemorySchemaMigrationRegistryTests.cs:102`（半路炸掉的步骤不留痕、重跑补完）／`:151`（真实初始化中断整步回滚、下次启动自愈）／`:208`（写点前取消则库分毫未动）／`:81`（重复初始化幂等指纹一致）；高版保护＝`SchemaVersionGuardTests.cs:66`（高于二进制登记版本即拒）／`:83`（拒绝早于第一次写盘，连 WAL pragma 也不例外）／`:149`（未来版本连修复趟都不给）；两套账并存＝`MemoryDualSchemaLedgerTests.cs:40/70`；隔离恢复一致性＝`DataBackupServiceTests.cs`＋`DataRestoreServiceTests.cs`。缺口＝没接生产触发（表 A HDS-001 行） | 已证 |

**B2 小结**：12 行＝已证 2（NR-21/22、NR-23/24）／部分证 9／未证 1（NR-19/20）。凡判"部分证"的行，缺的都是同一件事：**守它的格全在契约层或旧链上，真实叙事链今天不经过这些格**。

### B3 §14.3 真实模型比较（原文＝`v4:377-385`）

**整节默认未证**——本轮零 `dotnet`、零真模型调用，盘上也不存在任何三份对照（旧路线／仅加场景提示／新 Narrator 主链）的样本记录；`docs/eval/reports/` 里没有一份带真实模型延迟或 token 读数的报告（v4 自己在 `:385` 写明"本轮未启动真实模型"）。

如果作者点头，**最小可执行配方**（按盘上现有形状给，不新造机制）：

1. **模型档位**：本地目标档＝"2B 级"（`v4:23`、`:383`"至少覆盖目标本地 2B 级档位"），可选 API 对照要写明确版本；本地这一档的运行库与权重今天**不在仓库里也不许提交**（`AGENTS.md` 规则 6／`PROJECT_PLAN.md:144`），拉取入口＝现成脚本 `scripts/download-models.ps1` 与 `scripts/install-local-runtime.ps1`。
2. **需要哪台机器**：作者这台 Windows 机（唯一的真机与真实数据目录；`%LOCALAPPDATA%\PryCompanion` 不得当测试载体是常驻禁令，登记在 `docs/plans/PROJECT_PLAN.md:252`）。所以对照要么用一次性隔离目录，要么押到作者自己 dogfood。
3. **要记录的量**（v4:381 逐字）：全部样本、失败、取消、首泡/整轮延迟 p50/p95、输入/输出 token、内存/显存、调用次数与后台预算；观察项＝短句自然度、主体性与拒绝、场景因果一致、纠错、事实污染、长间隔恢复。落盘形状可以复用既有仪器：独立 TRX 落盘（§14.4 已要求）＋`docs/eval/run-template.md` 的报告模板。
4. **现在缺哪一件**：①HDS-003 的原型**没归位到线尾**（表 A 行 3），没有可跑的生成中心；②模型与运行库不在位；③作者那句"可以跑"还没有说（`docs/plans/PROJECT_PLAN.md:82`、`:217` 明写"HDS-003 要跑真实模型，等作者一句『可以跑』"）。这三件按顺序补齐才有 §14.3 的第一份读数。

### B4 §14.4 工程与发行门禁（原文＝`v4:387-393`）

| 门禁项 | 盘上实际 | 判定 |
|---|---|---|
| Release 构建 | 在册读数＝`docs/plans/PROJECT_PLAN.md:188-189`（守门人在归位后另建副本实测 0 警告 0 错误）；本轮零构建，只引用不重跑 | 已证（在册引用，非本轮重跑） |
| 解决方案测试 | 在册基线 **1179／0 红／2 条按名点出的环境跳过**（Client 108／App 233／Api 280／Core 558，executed 556），登记＝`PROJECT_PLAN.md:189`；两条跳过恒为 SenseVoice 与 llama-server 的环境键（同名点名见 `docs/eval/reports/2026-10-22-qa-wave10-three-evidence-debts.md` 与 `PROJECT_PLAN.md:231`）。**差的那一层也说清了**：`:189` 原文＝这一轮只有守门人一只手，第二只手押在下一波顺带复采 | 已证（带单手续证限制） |
| `scripts/verify-repository.ps1` | 脚本存在（`ls scripts/`），跟踪文件数最近一次在册读数＝556（`PROJECT_PLAN.md:189`、`:204`：`Repository verification passed for 556 tracked files.`）；本报告落库后会再顶一格，属条数不是门槛 | 已证 |
| TRX 独立保存 | 逐轮独立证据目录在册（`.local-notes/qa-wave12-1023/runs/` 25 份、`qa-wave12-close-1023/runs/full-resample-close` 4 份，见 `PROJECT_PLAN.md:187`、`:189`）；`v4:389` 要求"每次保存独立构建日志与非空 TRX，明确提交、引擎、模型和跳过原因"——按名点出跳过原因这项在报告里有原文 | 已证 |
| 候选包统一 SHA | `scripts/package-release.ps1` 存在且带脏树无条件门（`:34-35`）与模型文件 SHA 校验（`:76`），但**从未在叙事链上产出候选**；旧线在册欠账＝`docs/eval/README.md:25`"（`0-4`（`RT-008-PKG`）未开始，前置＝作者给 merge/tag/push 顺序与三条 `CHANGELOG.md` 入库" | 未证 |
| 许可材料 | 三处留痕机制有机械面判据（`tests/Pry.Api.Tests/UpstreamProvenanceAndDependencyShapeTests.cs`，HDS-011c 片 E1；归位 `e554483`/`bca8b42`/`5593e1a`）；但 `ASSETS_LICENSE.md:11` 仍把代码许可写成"GPL-3.0"，与根 `LICENSE` 的 AGPL-3.0-only 不符（改约链见 `AGENTS.md` 规则 7）——该文件本波一字不碰，列为表 C 第 6 项 | 部分证 |
| 隔离启动 | 在册事实只到"用现成产物把程序起回来并当场读数"那一层（`PROJECT_PLAN.md:250-252`：pid 38524、`127.0.0.1:4783` LISTENING、`runtime.json`/`runtime-write.lock` 新写），且同场明写两条未验证（第二个实例的拒绝、界面能否聊起来） | 部分证 |
| 备份恢复 | 判据成套装（表 A HDS-001、B2 的 NR-23/24 行），但**没有用户可达入口**（无端点、无 UI），也没有真库演练 | 部分证 |
| 托盘真实退出 | 形状级判据在册：`tests/Pry.App.Tests/TrayResidencyBoundaryTests.cs:65`（每个托盘退出站点保持锁定的步骤次序）／`:78`（次序打乱 scanner 会看见）／`:88`（退出绝不伸手去 kill 进程）、`WindowLifecycleTests.cs:28/43`（锁定次序与逐步等待）＋`scripts/verify-window-close-paths.ps1`；**"真那一次人工点击"只有作者能做**（`PROJECT_PLAN.md:96`、表 C 第 4 项） | 部分证 |
| 无所属模型残留 | 未见任何一格守它（模型进程面＝`ModelProcessRegistry.cs:63` 仍是在册待拆新缝，`PROJECT_PLAN.md:204`） | 未证 |
| 固定 Eval 只算部分证据 | `docs/eval/scenarios.json` 76 个永久 ID（`docs/eval/README.md:5`，suiteVersion 1.4.0）；`scripts/measure-fixed-eval.ps1` 会写覆盖 JSON、不能称只读（`v4:391`）——本轮未跑 | 未验证（本轮无条件跑；口径本身在 v4 与报告里有一致文字） |

**B4 小结**：11 行＝已证 3／部分证 5／未证 2／未验证 1。

---

## 表 C｜必须作者亲自点头才动的事（盘点过程中撞到的全清单）

| # | 事项 | 盘上出处（为什么 Agent 不能自己做） | 点头之后的第一步 |
|---|---|---|---|
| 1 | `git tag` | `AGENTS.md` 规则与 `docs/plans/PROJECT_PLAN.md:193`（"本次放权不解除的硬线照旧：`git tag`／`git push`／实际打包需他点名"）；`docs/plans/README.md:38` 同向 | 按你给的顺序给线尾（当场实测 `git rev-parse HEAD`＝`ac58245`，实现面线尾要看归位后的号）打附注 tag，tag 名与版本一致 |
| 2 | `git push` | 同上（`PROJECT_PLAN.md:193`）；`docs/plans/README.md:34` 记录 `main` 目前靠本地 ref-only 同步 | 先确认要推的分支清单与顺序（`codex/ui-main-window-redesign` → `main`？），再一次性推，推完当场读回远端 SHA |
| 3 | 实际打包（旧线编号 `0-4`／`RT-008-PKG`） | `docs/eval/README.md:25`："`0-4`（`RT-008-PKG`）未开始，前置＝作者给 merge/tag/push 顺序与三条 `CHANGELOG.md` 入库"；`scripts/package-release.ps1:4` 默认版本仍是 `0.0.2` | 你给 merge/tag/push 顺序并让三条 CHANGELOG 入库 → 用显式 `-Version` 跑 `scripts/package-release.ps1`，产物按统一 SHA 登记 |
| 4 | 托盘「退出…」那一次人工点击 | `docs/plans/PROJECT_PLAN.md:96`（"仍需针对实际候选验证，由 HDS-012 收证"）、`:252`（守门人明写没替他点任何控件，按规范不算已证明） | 你在真机（现成产物或候选包）点一次「退出…」，我们读回：进程清零、模型子进程无残留、托盘图标消失、数据目录 WAL 正常关闭 |
| 5 | 真实模型对照（一句"可以跑"） | `docs/plans/PROJECT_PLAN.md:82`、`:217`（"HDS-003 要跑真实模型，等作者一句『可以跑』"）、`docs/narrative-open-items-memo.md:128`（"要占作者的机器与 API 额度，需他点头"） | 先把 `codex/wave13-20261023`（`98246cd`）归位并由另一双手复采 → 用 `scripts/download-models.ps1`＋`scripts/install-local-runtime.ps1` 拉 2B 档 → 按 B3 第 3 项那份量全记录跑三形状对照（真实数据目录不当载体） |
| 6 | `ASSETS_LICENSE.md:11` 那句旧许可名 | 该行现文＝"上述内容适用根目录 `LICENSE` 所载的 **GPL-3.0**"，与 `AGENTS.md` 规则 7 的改约链（2026-10-20 起 **AGPL-3.0-only**）不符；本波按 `docs/dispatch-hds-wave15-20261023.md` §〇.5 一字不碰 | 你裁决措辞后由一把纯文档刀改这一行（并同场核对 `FAN_CONTENT_POLICY.md` 与 `docs/upstream-license-compatibility.md` 是否有同族旧名） |
| 7 | 真·依赖锁定（中央包管理／lock 文件，要联网 restore） | `docs/plans/PROJECT_PLAN.md:82`（"不在该单范围，等作者点头：要联网 restore、牵动 `scripts/package-release.ps1`"）、`:424`（"不点头就押到下一次打包前"）；盘上实测 `Directory.Packages.props` 与 `packages.lock.json` **均不存在**（只有 `Directory.Build.props`） | 你点头 → 引入 `Directory.Packages.props`＋`--use-lock-file`，第一次联网 restore 后把 `UpstreamProvenanceAndDependencyShapeTests.cs` 的版本范围那几格从"形状"升级成"锁定" |
| 8 | HDS-010 的旧主链退役与阶段裁定（含 U-9 接线时机） | `docs/plans/PROJECT_PLAN.md:75`（HDS-004 真前置＝U-9"需作者拍"）、`docs/narrative-open-items-memo.md:24`、`:132-139`（"什么时候让她走新链……是产品可见的取舍"）；旧四件仍在生产（表 A HDS-010 行列的 file:line） | 按 `docs/decision-request-protocol.md` 四段式给你一份"换链时机＋切换时她要做什么＋能不能回退"的选择题；选定后第一件事是把 `engineMode` 持久位与排空旧 pending 写进 HDS-005/010 的派单 |
| 9 | 备份／恢复的触发时机与入口形态（用户数据保护的产品面） | `docs/plans/PROJECT_PLAN.md:72`（"备份／恢复**未接生产路径**（无端点、无 UI 入口，触发时机属产品决策）"） | 你定"迁移前自动备份＋设置页一个手动按钮"还是只做自动；定了就派一把接线刀（端点＋DTO＋UI＋判据） |
| 10 | 第二个 Pry 进程指向同一数据目录时用户该看到什么 | `docs/narrative-open-items-memo.md:50-52`（U-2 里那格产品可见行为，已按四段式列为【待你裁决】） | 你选文案与行为（拒绝启动／只读打开／提示带恢复指引），选定后 HDS-001 后续波把真实锁与单行代次账接进启动路径 |
| 11 | 第一次让真实用户库吃新 schema（破坏性数据迁移） | `docs/agent-engineering-standards.md` §5"需要用户批准：破坏性数据迁移、删除重要数据或不可逆操作"；`v4:252-255` 要求迁移前先做已验证备份 | 先确认第 9 项已落地（备份可用），再批准一次隔离副本上的迁移演练并核对行数/稳定 ID/引用/索引/外键/墓碑 |
| 12 | `CognitionKind.CharacterBelief` 要不要补契约条款 | `docs/plans/PROJECT_PLAN.md:199`（实测该枚举在 `src/` 只出现在声明行、`tests/` 零命中；"不许 lane 顺手给它补一条规则（那是功能刀，本波冻结）"） | 你放行功能刀 → 由契约刀补一类来源门并把 `NarrativeIdentityContractTests.cs:351` 那张矩阵扩一行；不放行就继续挂账 |

**表 C 小结**：12 件，其中工单 §一 已点名的 8 件全数在列（第 1～8 项），另 4 件（第 9～12 项）＝本次盘点新撞到、同样只能你点头。

---

## 附｜我读了哪些文件（逐份路径）

**完整读过**：
- `docs/dispatch-hds-wave15-20261023.md`（本波工单）
- `docs/plans/README.md`
- `docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md`（全文 441 行）
- `docs/agent-engineering-standards.md`（全文 214 行）

**按段落读过（未通读全文，引用行号已逐行核对）**：
- `docs/plans/PROJECT_PLAN.md`（`:1-57` 边界与进度、`:58-177` §4～§8、`:182-297` §9 第十一／十二／十／九／八波 entry、`:424`）
- `docs/narrative-open-items-memo.md`（一页读数表 `:12-29`＋U-2／U-5／U-8／U-9／U-11 各节要点 `:50-52`、`:88`、`:128`、`:132-139`、`:163-173`）
- `ASSETS_LICENSE.md`（`:1-20`，只为核对 `:11` 那句旧许可名）
- `docs/eval/README.md`（`:5`、`:22`、`:25`、`:26` 四处引用段）

**读过的实现与判据面（均为只读）**：
- `src/Pry.Core/Narrative/`：`NarrativeDocuments.cs`、`NarrativeDraftValidator.cs`、`CharacterInstanceIdentity.cs`、`DeliveryLedger.cs`、`NarrativeCommitWriter.cs`、`OutboxOwnershipRegistry.cs`、`DataDirectoryWriteGate.cs`、`NarrativeContractReason.cs`（结构行号与行数，逐份 `wc -l`＋符号行读取）
- `src/Pry.Core/Backup/` 七份、`src/Pry.Core/Prompting/ContextBuilder.cs`、`src/Pry.Core/TurnTaking/AttachmentTextExtractor.cs`、`src/Pry.Core/Runtime/ReplyGate.cs` 与 `ReplyNecessity.cs`、`src/Pry.Core/Intelligence/BehaviorPlanner.cs` 与 `ReplyGenerationRenderer.cs`、`src/Pry.Core/TurnTaking/ReplyPlanner.cs`、`src/Pry.Api/Services/ProactiveMessageGenerator.cs` 与 `ProactiveScheduler.cs` 与 `DataDirectoryWriteLease.cs`、`src/Pry.Api/Runtime/CharacterIntelligenceGateway.cs`
- 判据文件：`tests/Pry.Core.Tests/NarrativeIdentityContractTests.cs`（当场实测 `[Fact] 21`／`[Theory] 4`／`[InlineData] 41`＝62 格，540 行）、`NarrativeStorageContractTests.cs`、`ContextBudgetTests.cs`、`ContextWindowLoadTests.cs`、`AttachmentLimitTests.cs`、`MemoryDeletionSeparationTests.cs`、`MemoryContextTests.cs`、`MemorySchemaMigrationRegistryTests.cs`、`SchemaVersionGuardTests.cs`、`MemoryDualSchemaLedgerTests.cs`、`ProactiveDeliveryTests.cs`、`ModelProviderErrorTests.cs`、`DataBackupServiceTests.cs`、`DataRestoreServiceTests.cs`；`tests/Pry.Api.Tests/`：`StickerContentLengthGuardTests.cs`、`ProactivePolicyTests.cs`、`ProactiveSchedulerTests.cs`、`ResourceLimitLedgerCoverageTests.cs`、`UpstreamProvenanceAndDependencyShapeTests.cs`、`RuntimeDescriptorWriteGateTests.cs`、`DesktopCapabilityGuardTests.cs`；`tests/Pry.App.Tests/`：`TrayResidencyBoundaryTests.cs`、`WindowLifecycleTests.cs`；`tests/Pry.Client.Tests/`：`StaleSessionRecoveryTests.cs` 等 14 份文件名
- `scripts/`：`package-release.ps1`、`verify-repository.ps1`、`verify-window-close-paths.ps1`、`measure-fixed-eval.ps1`、`download-models.ps1`、`install-local-runtime.ps1`（存在性与被引用的行；未执行）
- `docs/resource-limit-ledger.md`（存在＋116 行）
- Git 只读命令：`git status --short --branch`、`git log`、`git rev-parse HEAD`、`git branch --list`、`git log -1 codex/wave13-20261023`、`git diff --stat HEAD -- tests/Pry.Core.Tests/NarrativeIdentityContractTests.cs`（空＝盘上与线尾一致）

## 附二｜我没读但被引用的材料（一律登记为未证）

| 材料 | 为什么没读 | 影响哪几行判定 |
|---|---|---|
| `docs/eval/reports/2026-10-23-hds003-narrator-prototype.md`（主树未跟踪） | 他人名下在途物，工单 §〇.3 禁读禁碰 | 表 A 的 HDS-003 行、B1 第 1/2 条——只能按主线零命中判未证 |
| `docs/eval/reports/2026-10-23-qa-wave14-narrative-commit-action-unheard.md`（主树未跟踪） | 同上 | 凡涉及"第十四波判据刀"的证据：线尾 `ac58245` 的 `git log` 里**没有** wave14 的内容提交（当场实测最新测试提交＝第十二波 `c7f52ea`/`a5d9b80`，`docs/dispatch-hds-wave14-20261023.md` 亦为未跟踪）⇒ 按未归位处理 |
| `codex/wave13-20261023` 分支上的实现（`98246cd`）与 `codex/wave14-20261023` 分支尖 | 不在主工作树，读它要把别人的在途面当现状；工单 §〇.3 | 表 A HDS-003、B1 全部 |
| `docs/full-scope-sprint.md`、`docs/freeze-20261020-line-inventory.md` | 工单 §〇.4 只禁改；本波未读（旧线欠账细节以 `docs/eval/README.md:25` 的在册摘要为准） | B4 的"候选包"行只到未证，不再细分 |
| `docs/api-v1.md`、`docs/decision-request-protocol.md`、`docs/upstream-license-compatibility.md`、`licenses/THIRD-PARTY-NOTICES.md`、`LICENSE` | 未通读（`AGENTS.md` 已给出许可结论） | 表 C 第 6/8 项的第一步描述以规范文字为准，未逐行核对 |
| `docs/eval/scenarios.json`（76 个永久 ID） | 只引用 `docs/eval/README.md:5` 的在册描述，未逐条读 | B4 最后一行判"未验证" |
| `.local-notes/**` 下那 25＋4 份 TRX | 不在仓库面、且属守门人证据目录；本波只引用其在册登记 | B1/B2 各行的"实测红在册"字样全部指向 `PROJECT_PLAN.md:187-189` 的文字，不指向原始 TRX |
| 他人名下：两枚已暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）、`docs/research/` 三份、`docs/character-learning-safety.patch` | 工单 §〇.3 明令不读不碰 | App 读数 233 与 236 之差按 `PROJECT_PLAN.md:208`、`:277` 的在册解释引用，未自行核对 |

## 附三｜本轮的交付标准自检

- 零 `dotnet`：本波一次构建、一次测试、一次 `measure-fixed-eval.ps1` 都没起（工单 §〇.1）。
- 零 git 写入：主树无 `add`／`commit`／`stash`／`clean`／`checkout`／`tag`／`push`，唯一笔迹＝本报告这一份新文件。
- 未改任何既有报告、工单、`docs/plans/**`、`docs/full-scope-sprint.md`、`licenses/**`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md`。
- 按规范 §7／§10：以上每一条"已证"都只指盘上判据的存在与在册实测红，**不指用户今天能完成的行为**；全盘点里唯一能称"用户实际能完成"的，仍是旧那套对话链路，新的还不存在。

---

## 守门人收口与时点更正（2026-10-23 15:5x；原文一字不涂，本节只追补）

### 一、本报告的读取基点在跑的过程中被线尾追过了

lane 当场实测的基点＝`ac58245`（`:6`），它按这个基点判的三行**在当时都是对的**；守门人在它读写之间把两把代码刀归了位，所以下面三行现在是**过期读数**，按盘上最新事实更正：

| 本报告的行 | 报告原判定（基点 `ac58245`） | 盘上最新事实（线尾 `d3fbac4`） | 更正后的判定 |
|---|---|---|---|
| 表 A 的 HDS-003 行（`:29`） | 未证（未归位，载体 `codex/wave13-20261023`） | 归位＝`290a603`（4 枚新文件逐字节对平载体 `66906d0`）；`grep -rn "Narrator" src/ --include=*.cs` 从**零命中**变成命中 `src/Pry.Core/Narrative/NarratorContracts.cs`／`NarrativeRoundRunner.cs`；判据 **49 格**在 `tests/Pry.Core.Tests/NarratorPrototypeTests.cs`，15 枚单放反证在新字节上逐枚重取（`docs/eval/reports/2026-10-23-hds003-narrator-prototype.md` §七.3） | **部分证**：隔离原型已在主线、有会响的门守它；仍未证的还是那四件——真实模型对照、生产接线、UI 可见行为、生产库。**不是"已证"**，因为没有任何用户可达行为 |
| 表 B1 第 1、2 条（`:56`、`:57`） | 未证，理由含"新路线在主线不存在" | 新路线的**主语已经在主线存在**了（上一行）；但这四条要判的是"摘掉旧三件后新链还能跑"与"主动事件进同一入口"——旧 `BehaviorPlanner`／`ReplyGenerationRenderer`／`ReplyNecessity` 一件没摘、`ProactiveMessageGenerator` 那条第二生成器一条没并，`NarrativeRoundRunner` 在生产里**零调用方** | **未证照旧**（原因换了一条：不是"大脑不存在"，而是"大脑存在但没接上、旧脑还在替她做决定"） |
| 附二表里那两行"未读"（`:160`、`:161`） | 第十三／十四波报告属未跟踪在途物，按未归位处理 | 两份报告所描述的刀都已归位（`290a603`／`d3fbac4`），本报告落库时同场把这二份报告一起入库 | 时点差说明，非 lane 读错 |

同一场被追过的还有两处数字：`:99` 的 `verify-repository`＝556（归位后当场实测＝**562**）、`:98` 的解决方案测试在册 1179／0／2（**在册基线仍＝1179／0／2**，归位后那一轮独立副本单采读数＝1244／0／2，登记在 `docs/plans/PROJECT_PLAN.md` §9 同日 entry 与两份收口报告 §七；按"绝不单独为顶基线起构建"的规矩，换手复采仍欠着）。

### 二、〇 节那六句里，有一句现在能划掉一半

`:17` 那句"要结束还差四件"的第 ① 件（把分支上的原型并回主线并由另一双手复采）——**"并回主线"已做完**；"另一双手复采"这一半仍欠，因为归位后那一轮全量是**写判据的同一只手**在独立副本上单采。第 ②③④ 件（落库＋接真实收发、真机小模型对照、能装能恢复能真退的候选包）一字未变。

`:19` 那句"最快的下一步是先让原型回家"——原型今晚回家了；下一步按依赖顺序是**把它接上真实收发（HDS-004／005 落库面）**，而那一件的入口仍卡在表 C 第 8 项（接线时机 U-9）要你先拍。

### 三、本报告里守门人认可、不改判的三处

1. **表 A 已证＝0 行**这一条是全份报告最要紧的一格，它没有因为两把刀归位而改变：HDS-002／003 都只能算"契约与原型有门守着"，用户今天能完成的仍然是旧那套对话链路（`:174` 那句自检照旧成立）。
2. 表 C 那 12 件"只能你点头"的，守门人一件都没替你做：零 tag、零 push、零打包、没点托盘、没碰 `ASSETS_LICENSE.md:11`、没联网做依赖锁定、没放行 `CharacterBelief` 功能刀、没启动真实模型。第 9～12 项（备份触发时机、双开可见行为、首次真实库迁移、`CharacterBelief`）是本次盘点**新撞到**的，与工单点名的 8 件同场在册。
3. 表 B2 把 NR-21/22 与 NR-23/24 判成"已证"是本份报告唯二的两行"已证"，证据形状站得住（有名有姓的成套格＋在册实测红）。守门人不升格也不降级这两行，只补一条边界：NR-23/24 那两行同场自报了"没接生产触发"，所以它们的"已证"是**判据层已证**，不等于用户今天能做一次隔离恢复演练。
