# HDS-010 第一片·旧主链逐文件处置表（零构建、只读、不删一字节代码）

- 工单：`docs/dispatch-hds-wave19-20261023.md`（id `HDS-WAVE19-1023`）
- 执行方：冷启动 lane（第十九波）
- 裁定登记处：`docs/legacy-chain-deprecation.md` §1、§2、§4
- 本文性质：零构建只读分析。全程未调用任何 `dotnet`；`src/`、`tests/`、`scripts/`、`Pry.slnx`、`Directory.Build.props`、任何 `.csproj` 一字未改（出口见 §六 硬读数）。本文不写状态词／结案词，除当场贴出读数的条目外一律标「未验证」。

---

## 〇 入场读数（原样命令＋输出）

载体建立（主树执行）：

```
git -C C:/Users/24782/Desktop/Pry_Project worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave19-1023/carrier d82cd2d
Preparing worktree (detached HEAD d82cd2d)
HEAD is now at d82cd2d docs(plans): 顶第二把文档刀那条 verify-repository 读数（568→571）…
```

入场两行判据（载体内）：

```
git -C …/carrier status --short | wc -l   → 0
git -C …/carrier rev-parse HEAD           → d82cd2d182e3c433939588ac79f82f04cbeda151
```

主树开工基线（他人名下未提交项，本波一律不碰、不读为结论）：

```
git -C C:/Users/24782/Desktop/Pry_Project status --short --branch
## codex/ui-main-window-redesign
M  docs/dispatch-tip1020-02-memory-group-judgment.md
M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs
?? .qoder/
?? docs/character-learning-safety.patch
?? docs/eval/reports/2026-10-23-hds004-lifecycle-coordinator.md
?? docs/eval/reports/2026-10-23-hds005-sqlite-commit-delivery.md
?? docs/research/HDS_INTERLUDE.md
?? docs/research/HDS_QQ_RESEARCH_INDEX.md
?? docs/research/QQ_BRIDGE.md
```

### 〇.1 决定「替代」这一步完成没有的那条读数

命令原文：

```
grep -rn "Pry.Core.Narrative" src/ --include=*.cs | grep -v '/obj/' | grep -v 'src/Pry.Core/Narrative/'
```

命中（全仓 src，除 obj／除 Narrative 目录自身）：

```
src/Pry.Api/Services/DataDirectoryWriteLease.cs:2:using Pry.Core.Narrative;
src/Pry.Api/Services/RuntimeDescriptorService.cs:140:    /// <see cref="Pry.Core.Narrative.DataDirectoryWriteGate"/> 的观测（契约 §10.2 第 1 条：
```

两行都**不是对话链调用**：第 1 行是 `using` 行，第 2 行是 `///` 注释行——按工单 §一 表 B 假阳性口径第 2 条，二者不计为生产调用方。**但**第 1 行所 import 的 `DataDirectoryWriteGate` 在 `DataDirectoryWriteLease.cs:19` 有一次真实构造：

```
grep -n "DataDirectoryWriteGate" src/Pry.Api/Services/DataDirectoryWriteLease.cs
10:/// … Core 的 <see cref="DataDirectoryWriteGate"/> 只做判定：      ← 注释行
19:    private readonly DataDirectoryWriteGate _gate = new();        ← 真实构造（生产调用）
```

即：`DataDirectoryWriteGate` 是**新链里唯一一枚有生产构造点的类型**（属目录写闸门，HDS-001/002/005 地基），它不等于「对话链已被新链替代」。`docs/legacy-chain-deprecation.md` §5 那句「grep … 在 Pry.Api 零命中」在本基点上应精确化为：**Pry.Api/Services 有命中，但命中的是对话链之外的写闸门（`using`＋`new DataDirectoryWriteGate()`）＋一条注释；对话链（NarrativeRoundRunner／NarrativeCommitWriter／NarrativeDraftValidator／NarrativeContextCompiler／CharacterInstanceRegistry／OutboxOwnershipRegistry／DeliveryLedger／NarratorContracts）在 `src/` 除自身目录外零生产调用方**（下条为证）。

按类型名逐枚复算「新链类型在 src/ 除 Narrative 目录外的调用方」：

```
grep -rln --include=*.cs "\b<类型>\b" src/ | grep -v '/obj/' | grep -v '/bin/' | grep -v 'src/Pry.Core/Narrative/'
NarrativeRoundRunner / NarrativeCommitWriter / NarrativeDraftValidator /
CharacterInstanceRegistry / OutboxOwnershipRegistry / DeliveryLedger /
NarrativeContextCompiler        → 0 命中（各自）
DataDirectoryWriteGate          → src/Pry.Api/Services/DataDirectoryWriteLease.cs（new 构造）、src/Pry.Api/Services/RuntimeDescriptorService.cs（仅注释行）
```

结论：本波判断「替代」完成度所读的**新链形状基点＝载体 d82cd2d**。`src/Pry.Core/Narrative/` 在该基点有 11 枚文件（`ls` 读数），与主树工作目录同 11 枚、逐名相同——第十六／十七／十八波在飞的 Narrative 新文件**尚未并进本基点**，故本文「新链等价物」列只对这 11 枚已落盘文件负责，未落盘的接线一律记「未验证」。

### 〇.2 判据格计数口径

- 表 A／表 C 每格「判据格数」口径＝`grep -c '\[Fact\]'` ＋ `grep -c '\[InlineData('`（真实 token；`[InlineData]` 无数值括号会得 0）。`[Theory]` 只作附注，不计入格数（一个 Theory 展开成多格由 InlineData 数决定）。
- 该口径是**静态扫描格数**，与 `dotnet test` 的 TRX 执行读数不是同一把尺；本波零构建，故只给静态口径，不声称等于在册 1179／0／2 那条门禁数（该数改写权在 QA）。
- 四项分项（当场 `grep` 逐文件求和，见 §三 表 C 基线）：Core 598、Api 280、App 233、Client 108；合计 598＋280＋233＋108＝1219（由分项相加得出）。

---

## 一 表 A：旧链候选逐文件三态处置（73 行）

证据口径（每行「生产调用方」列的取法，全表统一，逐行只列命中的文件尾段）：

- 命令原文：`grep -rln --include=*.cs "\b<TOKEN>\b" src/ | grep -v '/obj/' | grep -v '/bin/' | grep -v "^<定义文件>$"`
- `<TOKEN>`＝该文件对外的主类型名（多类型文件逐个取，见列内标注）；`\b…\b` 词边界过滤同名不同类（例如 `ConversationService` 不吞 `ConversationSessionService`／`ConversationApplicationService`／`ConversationFolderApplicationService`／`ConversationReadService`）。
- `using`／`GlobalContractUsings.cs`／`///` 注释行**不算调用方**但会被扫到，凡此类命中在列内注明。catalog／`docs/**` 提及**不算生产调用方**、单列见 §二。
- 「关联判据格」列口径＝`grep -c '\[Fact\]'`＋`grep -c '\[InlineData('`（见 §〇.2），括号内数字为该测试文件的静态格数；标「混」＝该测试文件还测本行之外的宿主面，第二片删格时要逐格拆。
- 「新链等价物」列读的是 §〇.1 声明的 d82cd2d 基点上那 11 枚 `src/Pry.Core/Narrative/` 文件；写「无」＝该 11 枚里 `grep` 不到承接类型。

### 表 A-1 `src/Pry.Core/Runtime/`（15）

| 文件 | 三态判定 | 生产调用方（命中文件尾段） | 守的产品语义（大白话） | 新链等价物（file:line） | 关联判据格 |
|---|---|---|---|---|---|
| ConversationRuntime.cs | 被新链替代（类型已存在但零生产接线） | Controllers/ConversationRuntimeController、Program、Runtime/CharacterIntelligenceGateway、Runtime/RuntimeAdapters、Services/ConversationSessionService、Contracts/ConversationRuntimeContracts、Core/Abstractions/Capabilities、同目录 MessageIntervalStats/RuntimeModels/TurnScheduler | 一轮对话的相位、修订号、事件账与投递权 | NarrativeRoundRunner（`NarrativeRoundRunner.cs:67`）＋OutboxOwnershipRegistry（`OutboxOwnershipRegistry.cs:49`）；两者 `src/` 除自身目录 0 调用方＝接线未达成 | ConversationRuntimeTests(23)、RuntimeTriggerIntegrationTests(4)；另混 RuntimeRaceTests(11)、CapabilitySeamWiringTests(5)、ConversationRuntimeEndpointTests(1) |
| TurnScheduler.cs | 退役（旧假设退出） | 同目录 ConversationRuntime.cs | 决定「这一轮到底开不开、要不要退避」的先行调度 | 无（v4 `:80` 明列 TurnScheduler.cs 退出新主链） | RuntimeComponentTests(23混)、RuntimeRaceTests(11混) |
| TurnGates.cs | 退役（频率/必要性门不再先行裁决） | 同目录 TurnScheduler.cs；其中 `ReplyFrequency` 另被 Api Services/ConfigurationApplicationService、Services/ConversationSessionService、Contracts/CommonContracts、Contracts/ConversationRuntimeContracts 引用（＝用户设置面引用，存废属第二片） | 「多久回一次、够不够必要才回」的门 | 无（`:80` 频率阈值退出；`:30` 社交必要性/频率启发式退出先行决策位置） | RuntimeTriggerTests(22)、ReplyFrequencyHostPortTests(3) |
| ReplyNecessity.cs | 退役（旧假设退出） | 同目录 TurnGates.cs | 给「值不值得回」打 0–100 分 | 无（`:30` 社交必要性退出；且新链把「沉默」设为合法产物由 Narrator 判定） | RuntimeTriggerTests(22混) |
| MessageIntervalStats.cs | 退役（旧假设退出） | 同目录 ConversationRuntime.cs | 消息间隔统计喂退避 | 无（`:80` 群聊空转退避退出） | RuntimeTriggerTests(22混) |
| IdleBackoffController.cs | 退役（旧假设退出） | 同目录 TurnScheduler.cs | 空转时越等越久的退避 | 无（`:80` 群聊空转退避退出） | RuntimeTriggerTests(22) |
| InputBuffer.cs | 产品语义继续，新链尚无等价格＝缺口 | 同目录 ConversationRuntime.cs | 把用户连着发的几条攒成「一个回合」再处理（她等我把话说完） | 无（`src/Pry.Core/Narrative/` 全文 `grep -i "buffer"` 0 命中） | RuntimeComponentTests(23混)、RuntimeRaceTests(11混) |
| InputProbe.cs | 退役（节奏启发随必要性/频率退出） | 同目录 ConversationRuntime/Delivery/InputBuffer/ReplyGate/ReplyNecessity | 「句子说完没/像不像在跟角色说话」的节奏启发 | 无（随先行节奏退出；`Normalize`/`FirstLine`/`VisualLength` 属可再借的纯函数，是否复用由第二片定） | RuntimeComponentTests(23混)、RuntimeFakes(0 格) |
| InterruptionController.cs | 产品语义继续，新链尚无等价格＝缺口 | 同目录 ConversationRuntime.cs | 她正在打字／发了新消息时打断当前回复（插话） | 无（Narrative 全文 `grep -i "interrupt"` 0 命中） | RuntimeComponentTests(23混) |
| ReplyGate.cs | 被新链替代（类型已存在但零生产接线） | Api/Runtime/CharacterIntelligenceGateway、同目录 ConversationRuntime/RuntimeDefaults/RuntimeModels/RuntimePorts | 「要不要回、能不能回」的当轮裁决口 | NarrativeDraftValidator（`NarrativeDraftValidator.cs:105`）＋INarrator（`NarratorContracts.cs:87`，沉默合法）；零生产接线 | RuntimeComponentTests(23混)、SingleInFlightTurnBoundaryTests(3)；另 CharacterIntelligenceSeamTests(13混)、ExpressionFeedbackEndpointTests(3混) 等 |
| RuntimeTrace.cs | 宿主继续保留 | Api/Controllers/ConversationRuntimeController、Api/Services/ConversationSessionService、Contracts/ConversationRuntimeContracts、同目录 ConversationRuntime/RuntimeModels | 运行事件的可观测流水 | 不适用（观测层，非对话语义） | 无专属 `[Fact]`（随宿主面共用） |
| Delivery.cs | 被新链替代（投递账）／气泡节奏子语义＝缺口 | `PendingOutputQueue`/`PendingBubble`：仅同文件内被 ConversationRuntime 引用，`grep -rln "\bPendingOutputQueue\b" src/`（排除 obj/bin）＝命中 ConversationRuntime.cs；`DeliveryReport` 被 Api/Services/ConversationSessionService、同目录 ConversationRuntime 引用 | 待发气泡队列＋逐条投递节奏＋投递回执 | DeliveryLedger（`DeliveryLedger.cs:33`）承接投递账；**气泡节奏 `ReplyPacing` 无等价**（Narrative 全文 `grep -i "pacing"` 0 命中）＝缺口 | RuntimeComponentTests(23混)、RuntimeRaceTests(11混) |
| RuntimeModels.cs | 宿主继续保留（多为跨面共享枚举/记录） | 大量引用（含 Api Services/Runtime、Contracts、Core Abstractions）；逐类型判 | 相位、事件种类、用户输入 DTO 等模型 | 部分由 NarrativeDocuments 的对应记录接管，未接线 | 无专属 `[Fact]`（共用） |
| RuntimePorts.cs | 被新链替代／退役（seams 换形） | 被 ConversationRuntime/ReplyGate/Api Runtime/RuntimeAdapters 等实现；`IReplyAdvisor` 定义在此、注入点 ConversationRuntime.cs:9 | 组合根与旧生成/投递端口的接缝契约 | NarratorContracts/`INarrator`、NarrativeCommitWriter 端口取代；零生产接线 | RuntimeFakes(0 格，测试替身) |
| RuntimeDefaults.cs | 退役（过渡/静默 advisor 属 legacy 模式） | `TransitionalReplyAdvisor`：Api/Runtime/CharacterIntelligenceGateway、Api/Services/ConversationSessionService；`SilentReplyAdvisor`：0 跨文件命中 | 「暂时照旧回」与「静默不裁决」两个过渡档 | 无（`:81` 由 Narrator+CommitValidator 取代） | CapabilitySeamWiringTests(5混)、CharacterIntelligenceSeamTests(13混) |

### 表 A-2 `src/Pry.Core/TurnTaking/`（6）

| 文件 | 三态判定 | 生产调用方 | 守的产品语义 | 新链等价物 | 关联判据格 |
|---|---|---|---|---|---|
| ConversationTurnManager.cs | 退役（旧 App 驱动回合机，随单引擎切换作废） | App/MainWindow.axaml.cs、App/Services/RemoteConversationTurnManager.cs | 桌面旧窗口里「一发一收一轮」的本地回合编排 | NarrativeRoundRunner 取代（未接线）；App 侧入口见表 A-8 | EventStreamReconnectBanTests(1混)、SessionRestoredSurfaceTests(6混)、CoreTests(18混) |
| InterruptClassifier.cs | 产品语义继续，新链尚无等价格＝缺口 | Api/Runtime/RuntimeAdapters、Api/Services/ConversationSessionService、Core/Runtime/{ConversationRuntime,InterruptionController,RuntimePorts}、Core/TurnTaking/ConversationTurnManager | 用户消息是「打断」还是「新话题」的分类（贴纸打断） | 无（Narrative 无 interrupt 分类类型） | CapabilitySeamWiringTests(5混)、CoreTests(18混)、RuntimeComponentTests(23混) |
| ModelReplyFormat.cs | 被新链替代（解析形状复用）／解析层未落地 | 同目录 ReplyPlanner.cs | 判模型吐的是可见正文/错误/工具调用等形状 | v4 `:81`「解析按职责复用」；Narrator 侧解析层属 HDS-003（`U-8` 未决），无落盘等价类型 | ModelReplyFormatTests(16)、PlaceholderDefinitionStaysInCoreTests(3)、UnansweredTurnShapeTests(1) |
| ReplyPlanner.cs | 退役（先行决策位置，`:30`/`:81` 点名退出） | Api/Runtime/RuntimeAdapters（ReplyPlannerComposer）、Api/Services/ConversationSessionService、同目录 ConversationTurnManager | 「先判断回不回、再拼多条回复」的旧生成器 | Narrator＋NarrativeDraftValidator/CommitValidator（`NarrativeDraftValidator.cs:105`/`:381`）取代，未接线 | CoreTests(18混)、ModelReplyFormatTests(16混)、UnansweredTurnShapeTests(1)、CharacterIntelligenceSeamTests(13混) |
| VisibleReply.cs | 被新链替代（可见文本清洗） | Api/Runtime/CharacterIntelligenceGateway、同目录 ReplyPlanner | 从模型原始输出里剥出「她真的会说出口」的可见文本 | NarrativeProseBinder（`NarrativeDraftValidator.cs:38`）承接引用绑定；语义边界不同，接线未验证 | PlaceholderDefinitionStaysInCoreTests(3混)、UnansweredTurnShapeTests(1混) |
| AttachmentTextExtractor.cs | 宿主继续保留 | Api/Runtime/RuntimeAdapters、同目录 ConversationTurnManager | 把图片/文本/CSV/DOCX 附件抽成正文＋解压限额守卫 | 新链仍需此能力，属共享工具（v4 `:81` 边界按职责复用；NR-21/22 保留） | AttachmentLimitTests(8)、CoreTests(18混) |

### 表 A-3 `src/Pry.Core/Prompting/`（2）

| 文件 | 三态判定 | 生产调用方 | 守的产品语义 | 新链等价物 | 关联判据格 |
|---|---|---|---|---|---|
| ContextBuilder.cs | 宿主继续保留（新旧共用） | Core/Conversation/ConversationService、**Core/Narrative/NarrativeContextCompiler**、Core/TurnTaking/ReplyPlanner | 把身份/记忆/近期拼成喂模型的上下文预算 | NarrativeContextCompiler 已直接调用它＝新链复用而非替代 | ContextBudgetTests(7)、MemoryContextTests(17)、MemoryFloorRecheckTests(1)、MemoryRetrievalFloorTests(1)；另 NarratorPrototypeTests(46混) |
| PromptBuilder.cs | 宿主继续保留（随 ContextBuilder 存续） | Api/Services/ConversationSessionService、Api/Services/ProactiveMessageGenerator、Core/Conversation/ConversationService、Core/Prompting/ContextBuilder、Core/TurnTaking/ReplyPlanner | 具体提示词模板拼装 | 无独立等价；提示词模板属 HDS-003 未决（`U-8`），第二片须决定新链是否沿用 | CharacterIntelligenceSeamTests(13混)、CharacterIntelligenceTests(38混)、CoreTests(18混) |

### 表 A-4 `src/Pry.Core/Conversation/`（1）

| 文件 | 三态判定 | 生产调用方 | 守的产品语义 | 新链等价物 | 关联判据格 |
|---|---|---|---|---|---|
| ConversationService.cs | 退役（死入口，见 §二表 B 第 1 行复算） | 0 命中（`grep -rln --include=*.cs "\bConversationService\b" src/` 排除自身与 obj/bin ＝ 空） | 旧「边流式边落助手消息」的第二位生产者 | 无（本就无人调用） | 无（tests/ 亦 0 命中） |

### 表 A-5 `src/Pry.Core/Intelligence/`（12）

| 文件 | 三态判定 | 生产调用方 | 守的产品语义 | 新链等价物 | 关联判据格 |
|---|---|---|---|---|---|
| BehaviorPlanner.cs | 退役（先行决策位置，`:30` 点名） | 同目录 CharacterIntelligenceService.cs | 先出「意图/关系修正」再交给渲染 | Narrator（`NarratorContracts.cs:87`）取代，未接线；`:125` 的 0.55 单边抓手 §7 只登记不补 | CharacterIntelligenceTests(38) |
| ReplyGenerationRenderer.cs | 退役（先判断后表达的渲染段） | Api/Runtime/CharacterIntelligenceGateway（**ContextBuilder 的那处命中是注释行 `:19`，不算调用方**） | 把决策渲染成回复草案 | NarrativeDraft/NarrativeDraftValidator 取代 | CharacterIntelligenceTests(38) |
| BehaviorDecision.cs | 产品语义继续，新链尚无等价格＝缺口 | Core/Expression/ExpressionSelector、Core/Expression/ExpressionService、同目录 BehaviorPlanner/CharacterIntelligenceService | 表情选择器消费的「意图/语域/是否可用习惯语气」决策数据 | 无等价：新链用 NarrativeDraft/CommunicationAction（`NarrativeDocuments.cs:259`），形状不同且 Expression 侧未接 | ExpressionSystemTests(19混)、LearningSafetyTests(17混) |
| BehaviorPattern.cs（Retriever/Updater） | 宿主继续保留（数据/证据保留，不再先行裁决） | 同目录 CharacterIntelligenceService.cs（`BehaviorPatternRetriever`） | 行为经验的存取 | v4 `:82`「行为经验保留数据/证据/撤销约束，不再先行裁决」 | CharacterIntelligenceTests(38混) |
| CharacterIntelligenceService.cs | 退役／被新链替代（编排层） | Api/Runtime/CharacterIntelligenceGateway | 把 state+situation+planner+renderer 串成一次决策 | 由 Narrator 编排取代；网关改造属第二片 | CharacterIntelligenceTests(38)、LearningSafetyTests(17混)、ExpressionDrainChannelTests(10混) |
| CharacterStateService.cs | 宿主继续保留 | Api/Runtime/CharacterIntelligenceGateway、同目录 CharacterIntelligenceService | 身份/关系/心境的连续状态存取 | v4 `:82` 保留为「有来源的连续性材料」；`:139` 的 0.5 单边抓手照 §7 只登记 | CharacterIntelligenceTests(38)、LearningSafetyTests(17混)、ExpressionSystemTests(19混) |
| CharacterLearningCleanup.cs | 宿主继续保留 | Api/Program.cs、Api/Services/ConfigurationApplicationService | 删角色时连带清学习账 | 与对话链无关，保留 | CharacterLearningCleanupTests(3)、CharacterLearningDeletionTests(3混)、ApplicationServiceTests(23混)、ExplicitExpressionFeedbackTests(12混)、LocalModelLoopbackBoundaryTests(3混) |
| IdentitySnapshot.cs | 宿主继续保留（记录类型） | Api/Runtime/CharacterIntelligenceGateway、同目录 CharacterStateService/ReplyGenerationRenderer | 身份快照数据 | v4 `:82` 保留 | CharacterIntelligenceTests(38混)、LearningSafetyTests(17混)、ExpressionSystemTests(19混) |
| MoodState.cs | 宿主继续保留（记录类型） | 同目录 CharacterStateService、RelationshipState | 心境数据 | v4 `:82` 保留为软参考 | CharacterIntelligenceTests(38混)、ExpressionSystemTests(19混) |
| RelationshipState.cs | 宿主继续保留（记录类型） | Contracts/CommonContracts、同目录 CharacterStateService | 关系/熟悉度/表情闸数据 | v4 `:82` 保留 | CharacterIntelligenceTests(38混)、ExpressionSystemTests(19混) |
| SituationReader.cs | 退役（先行决策的情境读取） | Api/Runtime/CharacterIntelligenceGateway、同目录 BehaviorPlanner/CharacterIntelligenceService | 读记忆＋近期拼成「处境」供先行决策 | NarrativeRequest/PerceivedInputRefs（`NarrativeDocuments.cs:162`/`:74`）承载情境输入，未接线 | CharacterIntelligenceTests(38混)、CharacterIntelligenceSeamTests(13混) |
| SituationSnapshot.cs | 退役／宿主继续保留（视引用面） | 大量：Api/Runtime/CharacterIntelligenceGateway、Core/Abstractions/{Capabilities,CharacterIntelligence}、Core/Expression/{ExpressionSelector,ExpressionService}、Core/Intelligence/{BehaviorPlanner,CharacterIntelligenceService,SituationReader}、Core/Runtime/RuntimePorts | 「此刻她看到什么」的情境快照 DTO | 情境数据由 NarrativeRequest 取代；但 Expression 仍消费该形状＝第二片须逐格拆 | CharacterIntelligenceSeamTests(13混)、CharacterIntelligenceTests(38混)、ExpressionSystemTests(19混)、LearningSafetyTests(17混) |

### 表 A-6 `src/Pry.Api/Runtime/`（2）

| 文件 | 三态判定 | 生产调用方 | 守的产品语义 | 新链等价物 | 关联判据格 |
|---|---|---|---|---|---|
| CharacterIntelligenceGateway.cs | 被新链替代（宿主侧组合根待改接） | Api/Program.cs、Api/Services/ConversationSessionService、Api/Services/ExpressionDrainService、Api/Services/ExpressionFeedbackService | 把 Core 决策链接进宿主、签发 advisor/投递口 | 目标＝网关改接 Narrator（`U-9` 接线点 `CharacterIntelligenceGateway.cs:65`/`:315`）；未落 | CharacterIntelligenceLedgerCapBehaviorTests(2)、CharacterIntelligenceSeamTests(13)、ExplicitExpressionFeedbackTests(12混)、ExpressionDrainChannelTests(10混)、UnansweredTurnLedgerTests(3)、PlaceholderDefinitionStaysInCoreTests(3混)、ExpressionFeedbackEndpointTests(3混)、ExpressionFeedbackSurfaceTests(7混) |
| RuntimeAdapters.cs | 被新链替代／退役（旧链适配器束） | Api/Program.cs、Api/Services/ConversationSessionService；分类型：`DatabaseRuntimeStore`→Program/ConversationSessionService/Core/Memory/IntelligenceRepositories；`ReplyPlannerComposer`→ConversationSessionService；`StickerInterruptClassifier`→ConversationSessionService；`ConversationRuntimeEventSink`→CharacterIntelligenceGateway/ConversationSessionService | 把 ReplyPlanner/Interrupt/Store 适配成 Runtime 端口 | 端口语义由 NarrativeCommitWriter/OutboxOwnershipRegistry/DeliveryLedger 取代，未接线 | CapabilitySeamWiringTests(5)、CharacterIntelligenceSeamTests(13混)、EventStreamHeartbeatTests(8混) |

### 表 A-7 `src/Pry.Api/Services/`（32，逐枚全列）

说明：32 枚全部在 `Program.cs`（DI 注册）与/或 `Controllers/` 有生产调用方，**没有一枚是本波可删的死入口**；本列把「旧会话编排／投递」与「与对话链无关的宿主服务」分开：前者＝三态为「宿主继续保留、但其内部对旧 Runtime 的调用段第二片要改接新链」（记「改接」），后者＝「宿主继续保留（与链切换无关）」。

| 文件 | 三态判定 | 生产调用方（Program/DI 之外） | 守的产品语义 | 新链等价物 | 关联判据格 |
|---|---|---|---|---|---|
| BackendRuntime.cs | 宿主继续保留（改接：向旧链要生成/投递的那段要改） | Controllers/ChatController、CharacterInteractionProfileService、ConfigurationApplicationService、ConversationSessionService、ExpressionFeedbackService、MediaCleanupService、MemoryEmbeddingService、ProactiveMessageGenerator、ProactivePolicyService、ProactiveScheduler、SpeechApplicationService、StickerApplicationService、Core/Backup/BackupInclusionPolicy | 后端模型/运行时的装配与调用入口 | 无独立等价；改接点属第二片 | ApplicationServiceTests(23混)、ProactiveGeneratorTests(0+3=3混)、ProactivePolicyTests(4混)、ProactiveSchedulerTests(6混)、StickerContentLengthGuardTests(3混)、CharacterLearningDeletionTests(3混)、ExplicitExpressionFeedbackTests(12混)、LocalModelLoopbackBoundaryTests(3混)、ConversationReadEndpointTests(1混) |
| BackendSessionToken.cs | 宿主继续保留（与链无关） | EmbeddedBackend、Middleware/SessionTokenMiddleware、Program、Services/RuntimeDescriptorService | 会话令牌鉴权 | 不适用 | SessionTokenTests(7)、BackendLoggingTests(5混) |
| CharacterInteractionProfileService.cs | 宿主继续保留（与链无关） | Controllers/CharacterInteractionsController、Program、ConfigurationApplicationService、ConversationSessionService | 角色交互档案读写 | 不适用 | ApplicationServiceTests(23混) |
| CompanionActivityService.cs | 宿主继续保留（改接：主动消息活动门） | Controllers/ProactivePolicyController、Program、ProactiveScheduler | 陪伴活跃度（喂主动消息门） | 主动语义按 v4 `:84` 并入叙事入口，许可/静默/冷却保留；无独立落盘等价 | ProactiveSchedulerTests(6混)、ProactivePolicyTests(4混) |
| CompanionOperationCoordinator.cs | 宿主继续保留（与链无关，跨服务串行锁） | Program、CompanionActivityService、ConfigurationApplicationService、ConversationApplicationService、ConversationReadService、ConversationSessionService、MemoryApplicationService、ProactivePolicyService、ProactiveScheduler、SpeechApplicationService | 跨操作的互斥协调 | 不适用 | ProactiveSchedulerTests(6混) |
| ConfigurationApplicationService.cs | 宿主继续保留（改接：设置里的回复频率等旧参数存废随链切换） | Controllers/ConfigurationController、MemoryEmbeddingController、SpeechController、Program、Core/Backup/BackupInclusionPolicy | 配置读写 | 不适用（设置项本身保留，旧启发式阈值存废见 TurnGates 行） | ApplicationServiceTests(23)、CharacterLearningDeletionTests(3混)、ExplicitExpressionFeedbackTests(12混)、LocalModelLoopbackBoundaryTests(3混) |
| ConversationApplicationService.cs | 宿主继续保留（改接：会话增删改编排） | Controllers/ConversationsController、Program | 会话 CRUD | 无独立等价；第二片改接 | ApplicationServiceTests(23混)、ProactiveSchedulerTests(6混)、StickerContentLengthGuardTests(3混) |
| ConversationEventLog.cs | 宿主继续保留（改接：事件投递随引擎换） | Runtime/RuntimeAdapters、ConversationSessionService、SseEventStream | 对话事件的落账与 SSE 源 | DeliveryLedger/新投递账接管事件面（未接线） | ApplicationServiceTests(23混)、CapabilitySeamWiringTests(5混)、EventStreamHeartbeatTests(8混)、EventJournal.cs(0 替身) |
| ConversationFolderApplicationService.cs | 宿主继续保留（与链无关） | Controllers/FoldersController、Program | 会话分组/文件夹 | 不适用 | 无专属测试（0 格） |
| ConversationReadService.cs | 宿主继续保留（与链无关，只读） | Controllers/ConversationReadController、Program | 历史消息读取 | 不适用 | ConversationReadEndpointTests(1混) |
| ConversationSessionService.cs | 宿主继续保留（改接：旧会话编排/投递的中枢，串 ReplyPlannerComposer/Interrupt/EventLog/RuntimeTrace） | Controllers/{CharacterInteractionsController,ChatController,ConversationRuntimeController}、Program、CompanionActivityService、ConfigurationApplicationService、ConversationReadService、ExpressionDrainService、ProactiveScheduler、StickerApplicationService、Core/Intelligence/CharacterLearningCleanup、Core/Runtime/RuntimeDefaults | 一次收发的编排：取 advisor→规划→投递→落账 | 该编排整体由 NarrativeRoundRunner+NarrativeCommitWriter+DeliveryLedger 取代（未接线） | ApplicationServiceTests(23混)、CapabilitySeamWiringTests(5混)、CharacterIntelligenceSeamTests(13混)、CharacterLearningDeletionTests(3混)、ConversationReadEndpointTests(1混)、ConversationRuntimeEndpointTests(1)、ExplicitExpressionFeedbackTests(12混)、ExpressionDrainChannelTests(10混)、LocalModelLoopbackBoundaryTests(3混)、ProactiveSchedulerTests(6混)、ProtocolEventCursorTests(6混)、StickerContentLengthGuardTests(3混) |
| DataDirectoryWriteLease.cs | 宿主继续保留（已接新链写闸门） | Program、RuntimeDescriptorService | 数据目录进程级独占写锁 | 直接构造 `DataDirectoryWriteGate`（新链唯一生产接线的类型） | RuntimeDescriptorWriteGateTests(5) |
| EmbeddingProcessRegistry.cs | 宿主继续保留（与链无关） | Program、MemoryEmbeddingService | 嵌入子进程登记 | 不适用 | ApplicationServiceTests(23混) |
| ExpressionDrainService.cs | 宿主继续保留（改接：表情排空消费旧决策） | Program、ExpressionFeedbackService | 表情学习结果的排空通道 | BehaviorDecision→NarrativeDraft 换形后排空语义待接（未验证） | ExplicitExpressionFeedbackTests(12混)、ExpressionDrainChannelTests(10混)、ExpressionFeedbackSurfaceTests(7混) |
| ExpressionFeedbackService.cs | 宿主继续保留（改接） | Controllers/ChatController、Program、Runtime/CharacterIntelligenceGateway | 显式表情反馈 | 同上 | ExplicitExpressionFeedbackTests(12混)、ExpressionFeedbackSurfaceTests(7混) |
| LocalModelDiscoveryService.cs | 宿主继续保留（与链无关） | Program、BackendRuntime | 本地模型发现 | 不适用 | ApplicationServiceTests(23混) |
| MediaAssetStore.cs | 宿主继续保留（与链无关） | Controllers/MediaController、Program、ConfigurationApplicationService、ConversationSessionService、MediaCleanupService、SpeechApplicationService、StickerApplicationService | 媒体资产存取 | NR-21/22 保留 | MediaUploadLimitTests(5)、ApplicationServiceTests(23混)、CharacterLearningDeletionTests(3混)、ExplicitExpressionFeedbackTests(12混)、LocalModelLoopbackBoundaryTests(3混)、ProactiveSchedulerTests(6混)、StickerContentLengthGuardTests(3混) |
| MediaCleanupService.cs | 宿主继续保留（与链无关） | Program（仅此） | 媒体后台清理 | 不适用 | 无专属测试（0 格） |
| MediaUploadLimits.cs | 宿主继续保留（与链无关） | Controllers/MediaController、Program、MediaAssetStore | 上传限额 | NR-21/22 保留 | MediaUploadLimitTests(5) |
| MemoryApplicationService.cs | 宿主继续保留（与链无关） | Controllers/MemoriesController、Program | 记忆 CRUD | 不适用 | ApplicationServiceTests(23混) |
| MemoryConsolidationService.cs | 宿主继续保留（与链无关） | Program（仅此） | 记忆固化后台 | 不适用 | 无专属测试（0 格） |
| MemoryEmbeddingService.cs | 宿主继续保留（与链无关） | Controllers/MemoryEmbeddingController、Program、ConfigurationApplicationService、ConversationSessionService、MemoryApplicationService | 记忆向量化 | 不适用 | ApplicationServiceTests(23混) |
| ModelPerformancePolicy.cs | 宿主继续保留（与链无关） | Program、BackendRuntime、ModelProcessRegistry | 性能档/降档策略 | 不适用 | ModelPerformancePolicyBoundaryTests(3)、ApplicationServiceTests(23混)、ContextWindowLoadTests(5混) |
| ModelProcessRegistry.cs | 宿主继续保留（与链无关） | Program、BackendRuntime | 模型子进程登记 | 不适用 | ApplicationServiceTests(23混)、ModelPerformancePolicyBoundaryTests(3混)、CharacterLearningDeletionTests(3混)、ExplicitExpressionFeedbackTests(12混)、LocalModelLoopbackBoundaryTests(3混)、ProactiveGeneratorTests(3混)、ProactivePolicyTests(4混)、ProactiveSchedulerTests(6混)、StickerContentLengthGuardTests(3混) |
| ProactiveMessageGenerator.cs | 被新链替代（改接：主动消息并入叙事入口，v4 `:84`） | Program | 主动发消息的生成 | 由 Narrator 统一入口承接（未接线）；许可/静默/冷却保留 | ProactiveGeneratorTests(3) |
| ProactivePolicyService.cs | 宿主继续保留（改接：主动许可门随引擎） | Controllers/ProactivePolicyController、Program | 主动消息策略/安静时段 | v4 `:84` 约束保留、生成入口并入叙事 | ProactivePolicyTests(4混)、ProactiveSchedulerTests(6混) |
| ProactiveScheduler.cs | 宿主继续保留（改接） | Controllers/ProactivePolicyController、Program | 主动消息的排期 | 排期语义保留、生成走新入口（未接线） | ProactiveSchedulerTests(6) |
| ProtocolEventCleanupService.cs | 宿主继续保留（与链无关） | Program（仅此） | 协议事件清理 | 不适用 | 无专属测试（0 格） |
| RuntimeDescriptorService.cs | 宿主继续保留（已接新链写闸门） | Program、App/Services/BackendLocator | runtime.json 发现描述 | 写权判定已接 `DataDirectoryWriteGate` | BackendDiscoveryDirectoryTests(2混) |
| SpeechApplicationService.cs | 宿主继续保留（与链无关，语音） | Controllers/SpeechController、Program | 语音合成/识别编排 | 不适用（v4 `:375` 语音需回归，非本链替换对象） | ApplicationServiceTests(23混) |
| SseEventStream.cs | 宿主继续保留（改接：投递事件流随引擎） | Controllers/ChatController | SSE 推送流 | 新投递账接管事件源（未接线） | EventStreamHeartbeatTests(8混) |
| StickerApplicationService.cs | 宿主继续保留（与链无关，贴纸） | Controllers/StickersController、Program | 贴纸存取 | 不适用 | ApplicationServiceTests(23混)、StickerContentLengthGuardTests(3混) |

### 表 A-8 `src/Pry.App/` 旧窗口与旧回合管理器（对照 v4 `:86`）

| 文件 | 三态判定 | 生产调用方 | 守的产品语义 | 新链等价物 | 关联判据格 |
|---|---|---|---|---|---|
| MainWindow.axaml.cs | 宿主继续保留（当前仍是在跑的主窗口；ui-redesign 分支正在做替代，非本波判定） | App/App.axaml.cs（`MainWindow` 类词界命中散见于多处 UI 文案与 `IPryMainWindow`，实构造点在 App/窗口层；`grep -rln "\bMainWindow\b" src/` 含 App.axaml、IPryMainWindow、PryMessenger*Window、若干 Settings/Theme 服务与 Contracts/DomainModels 的**文案/类型名 substring 命中**，已按词边界与语义过滤） | 桌面主窗口，收发入口 | 新窗口＝`PryMessengerWindow.axaml.cs`（实现 `IPryMainWindow`），替代进行中 | SessionRestoredSurfaceTests(6混)、EventStreamReconnectBanTests(1混)、TrayResidencyBoundaryTests(8混)、UserFacingNoticeBoundaryTests(3混) |
| Services/RemoteConversationTurnManager.cs | 退役（旧回合管理器，`:86` 点名退役） | MainWindow.axaml.cs | 旧窗口驱动的远程回合编排 | 由新链回合入口（经改接的 ConversationSessionService）取代，未接线 | EventStreamReconnectBanTests(1混)、SessionRestoredSurfaceTests(6混) |
| ConversationTurnManager 调用段（App 侧） | 退役（随 RemoteConversationTurnManager 一并） | 经 RemoteConversationTurnManager/MainWindow 调 Core/TurnTaking/ConversationTurnManager | 旧窗口的本地一轮收发 | 同上 | 同 Core/TurnTaking/ConversationTurnManager 行 |

缺口（产品语义继续、新链尚无等价格）汇总见 §三表 C；本波数到的缺口行＝**5** 行（InputBuffer、InterruptionController、InterruptClassifier、BehaviorDecision、Delivery 的 `ReplyPacing` 子语义）。

---

## 二 表 B：死入口清单（零调用方、本波已证可删）

死入口口径＝一个源文件对外的主类型在 `src/`（除自身、除 obj/bin）与 `tests/`＋`scripts/`（除 obj/bin）都拿不到任何引用行。

### 表 B-1：`ConversationService.cs`（`legacy-chain-deprecation.md` §5 已登记那枚，本波复算）

命令一（搜 src，排除 obj/bin 与定义文件）：

```
grep -rn --include=*.cs "\bConversationService\b" src/ | grep -v '/obj/' | grep -v '/bin/' | grep -v 'src/Pry.Core/Conversation/ConversationService.cs'
（无输出）→ 0 命中
```

命令二（搜 tests＋scripts）：

```
grep -rn --include=*.cs "\bConversationService\b" tests/ | grep -v '/obj/' | grep -v '/bin/'
grep -rn "\bConversationService\b" scripts/
（两条皆无输出）→ 0 命中
```

自身声明行（唯一命中，证明确实除自身外零引用）：

```
grep -n "\bConversationService\b" src/Pry.Core/Conversation/ConversationService.cs
11:public sealed class ConversationService(MemoryDatabase database, PromptBuilder promptBuilder, ModelRouter router, …
```

→ 判定：死入口成立。catalog 计数（见下条假阳性③）＝`ConversationService` 在 `scripts/measure-fixed-eval.ps1` **0 命中**、在 `docs/eval/scenarios.json` 亦未点名任何 `ConversationService.Method`，故删除它**不**打断覆盖映射。

### 表 B-2：三条假阳性口径的实证

① **同名不同类**——按 `\b` 精确 token 过滤。全仓实际存在的同名族：

```
grep -roE "Conversation[A-Za-z]*Service" src/ --include=*.cs | grep -v '/obj/' | grep -oE "Conversation[A-Za-z]*Service" | sort -u
ConversationApplicationService
ConversationFolderApplicationService
ConversationReadService
ConversationService
ConversationSessionService
```

`ConversationService` 是独立一枚；搜它的 `\bConversationService\b` 不会命中 `ConversationSessionService`（后者不含 "ConversationService" 连续子串）。上两条命令的 0 命中因此是真·零引用，不是被过滤掉。

② **`using`／`GlobalContractUsings.cs`／注释行不算调用方，但要标注**——两例：
- `grep -rn "Pry.Core.Narrative" src/` 命中 `DataDirectoryWriteLease.cs:2 using Pry.Core.Narrative;`（引用行）与 `RuntimeDescriptorService.cs:140 /// <see cref=…>`（注释行）——都不算对话链调用方；但 `DataDirectoryWriteLease.cs:19 private readonly DataDirectoryWriteGate _gate = new();` 是**真构造**，须单列为写闸门的生产接线（非对话链）。
- `grep -rln "\bReplyGenerationRenderer\b" src/` 命中 `Core/Prompting/ContextBuilder.cs`，逐条看 `grep -n "ReplyGenerationRenderer" ContextBuilder.cs → :19 // 默认 AdvisoryTokens（2400）对齐…（ReplyGenerationRenderer.DefaultBudgetCharacters` 是**注释行**，不算调用方；其真调用方只有 `Api/Runtime/CharacterIntelligenceGateway.cs`。

③ **catalog／文档提及单列**——`ConversationService` 在 7 枚 `docs/**` 出现（`docs/character-intelligence.md`、多份 eval 报告、`docs/memory-context-*.md`），全部**不算生产调用方**；`grep -c "ConversationService" scripts/measure-fixed-eval.ps1`＝0。真正会因删类而打断的是 **`docs/eval/scenarios.json` 里点名的旧链测试类**（`measure-fixed-eval.ps1:14` 扫 `tests/**/*Tests.cs`、`:26` 按 `Namespace.Class.Method` 比对），命中的旧链类有：`ConversationRuntimeTests`(7 场景)、`RuntimeComponentTests`(5)、`RuntimeTriggerTests`/`RuntimeTriggerIntegrationTests`(7)、`CharacterIntelligenceTests`(12)、`ReplyFrequencyHostPortTests`(3)、`EvaluationPersistenceTests`、`Proactive*Tests` 等——删这些**测试文件**才动覆盖映射，属第二片重基线账（见表 C）。

### 表 B-3：其余同类候选（全仓复扫结果）

对表 A 的 73 枚逐一复算主类型跨文件命中后，**除 `ConversationService.cs` 外，没有第二枚「整文件零外部调用方」的死入口**：Runtime 族经 `ConversationRuntime` 连到 Api，TurnTaking/Intelligence/Prompting/Api 族全部有 `Program.cs`＋`Controllers/` 或同目录活类型引用。文件内部有若干零外部引用的**子类型**（如 `SilentReplyAdvisor`、`PendingOutputQueue`、`DeferredReplyComposer`、`TurnGateResult`——`grep -rln` 除自身文件外均「无匹配」），但它们随宿主文件/退役片一并处理，本波不作为独立删除片。→ 表 B 结论：本波唯一可确认的死入口＝**1 枚（ConversationService.cs）**。

---

## 三 表 C：删除片排法、每片的构建凭证与判据账重基线

前置事实：新链对话类型在生产零构造（`grep -rnE "new (NarrativeRoundRunner|NarrativeCommitWriter|NarrativeDraftValidator|DeliveryLedger|…)" src/`＝无匹配）⇒ 按 v4 `:86`「替代＋依赖核对后退役」，**没有任何一片能在「新链未接线」时先删旧链**。故本片排法的顺序约束是先接线、后摘链、再删死格；每片都要一轮构建凭证（`dotnet build`＋全量 `dotnet test`），构建权在第十六～十八波代码刀落盘后同一场，本波零 `dotnet`。

判据格四项分项基线（本波当场 `grep` 逐文件求和，`Fact`＋`InlineData` 口径，见 §〇.2）：

| 分项 | Fact | InlineData | 分项格数(Fact＋InlineData) |
|---|---|---|---|
| Core | 481 | 117 | 598 |
| Api | 222 | 58 | 280 |
| App | 157 | 76 | 233 |
| Client | 70 | 38 | 108 |
| 合计（分项相加） | 930 | 289 | **1219** |

命令口径样例（Core 分项，其余同形）：

```
for f in tests/Pry.Core.Tests/*.cs; do echo "$(grep -c '\[Fact\]' $f)+$(grep -c '\[InlineData(' $f)"; done   # 相加＝598
```

删除片排法（只排顺序与凭证，不承诺行量）：

| 片 | 动的文件 | 连带消失判据格（分项；「消失」＝随旧假设退役，非改写到新链） | 需新写等价格 | 打包形状门（§4 第 2 项） | U-2/U-3/U-11 依赖 |
|---|---|---|---|---|---|
| 片 1（死入口） | `src/Pry.Core/Conversation/ConversationService.cs` | Core 0／Api 0／App 0／Client 0（无测试指向，表 B-1 已证） | 无需（本就无调用方） | 需：新增「publish 目录内不含 `ConversationService`」守卫，**先故意打红一次**取红凭证，删除后转绿取绿凭证 | 无 |
| 片 2（先行决策段退役） | `Intelligence/{BehaviorPlanner,ReplyGenerationRenderer}.cs`、`Runtime/{TurnScheduler,TurnGates,ReplyNecessity,MessageIntervalStats,IdleBackoffController,InputProbe,RuntimeDefaults}.cs`、`TurnTaking/ReplyPlanner.cs`＋`Api/Runtime/{CharacterIntelligenceGateway,RuntimeAdapters}.cs` 的旧决策段 | 全旧假设退役格（可整文件删）：Core `RuntimeTriggerTests`22＋`RuntimeTriggerIntegrationTests`4＝26、`UnansweredTurnShapeTests`1；Api `ReplyFrequencyHostPortTests`3、`PlaceholderDefinitionStaysInCoreTests`3；混格须逐格拆的 `CharacterIntelligenceTests`38、`ModelReplyFormatTests`16、`RuntimeComponentTests`23、`RuntimeRaceTests`11 里指向被删门的格——**本波不估删除数**，标「未验证（待片内逐格点名）」 | 由 Narrator 沉默/合法性判定接管；每格按 v4 `:375` 写「旧假设退役」还是「产品语义继续由新链接管」 | 打包门须断言产物不含 `BehaviorPlanner/ReplyGenerationRenderer/ReplyPlanner` 类型名（先红后绿） | 依赖 HDS-003 真实模型对照（`:86` 替代）＋`U-8` Narrator 解析层；不直接依赖 U-2/3/11 |
| 片 3（投递/回合机/事件改接） | `Runtime/{ConversationRuntime,Delivery,RuntimePorts,RuntimeModels}.cs`、`TurnTaking/{ConversationTurnManager,ModelReplyFormat,VisibleReply}.cs`、`Api/Services/{ConversationSessionService,ConversationEventLog,SseEventStream,ProactiveMessageGenerator}.cs` 的旧投递段 | 多为「产品语义继续→改写不删除」：Core `ConversationRuntimeTests`23、`EvaluationPersistenceTests`7、`Proactive*` 等由 DeliveryLedger/OutboxOwnershipRegistry 新格替换；旧格数与新格数须一一对账 | 需新写 DeliveryLedger/OutboxOwnershipRegistry 的对等运行格；`SplitReplies` 用户许可语义沿用（`U-4`） | 打包门不含 `ConversationRuntime` 投递类型（先红后绿） | 依赖 `U-5`（reply_plans 关单）、`U-7`（CommitId/ContentHash）；落库属 HDS-005 |
| 片 4（缺口回填＋数据迁移，删除前必须先建） | 新建：`Narrative/` 内承接 InputBuffer（攒回合）、Interruption（打断/倾听）、ReplyPacing（气泡节奏）、BehaviorDecision→Expression 换形的类型 | 0 删除（这片的格是**新增**，不是消失）；它补齐 §一 数的 5 处缺口 | 5 处缺口的等价格全在此片新写 | 打包门此时才可能全绿（旧链类型全消失后） | 硬依赖 `U-2`（目录写锁落点/代次持久化）、`U-3`（实例↔目录代次联动）、`U-11`（迁移步骤 FenceRequirement/BackfillEvidence）＋`PROJECT_PLAN:37` 数据迁移继承（messages/memories/runtime/proactive→新表，不清库） |

表 C 汇总：**缺口＝5 处**（同 §一 尾，全部押在片 4 新写等价格），**整文件死入口删除格＝0**（片 1 的 `ConversationService` 无测试）。上表凡未贴当场读数的格数一律写「未验证」，删除片落地时按 v4 `:375` 逐格重取「红/绿」两凭证。

---

## 四 表 D：交给作者看的大白话后果（今天真把旧链删了会失去什么）

前提（当场已证一次，见 §〇.1＋表 C 前置）：**新链今天一行都没接上生产**（对话链类型在 `src/` 除自身目录零调用方、无任何 `new` 构造点）。所以下面「会不会失去」的判据是——只要第二片在没接线前就删旧链，旧链供的每一项都会立刻没。这一张表主语全用能力名，不用内部编号。

| 你在机器上能做的事 | 今天靠谁跑 | 若今天就删旧链、新链还没接上 |
|---|---|---|
| 发一句话、看她回你 | 旧链（对话运行时＋回复规划，经会话中枢） | **会失去**——发出去没人回（大脑被删、新大脑没接上） |
| 让她重新生成上一条 / 撤回 | 旧链 | **会失去** |
| 你正在打字时她先停、你连着发几条她攒成一轮再答（倾听/等说完） | 旧链（输入缓冲＋打断控制＋打断分类） | **会失去**，且这正是新链还没有对应的缺口之一 |
| 她说话一句一句按阅读节奏冒出来（气泡节奏） | 旧链（投递节奏） | **会失去**（气泡节奏也是新链暂无对应的缺口） |
| 她不定时主动给你发一条（主动消息） | 旧链排期＋旧生成 | **会失去生成**（排期/安静时段这些门是保留的，但生成入口被删） |
| 她用的表情/口头禅（会不会被场景挡） | 旧链决策产出的「行为决定」喂给表情选择 | **部分失去**（行为决定换形未接＝缺口之一；表情数据本身保留） |
| 语音说话 / 语音识别 | 独立语音服务（不经这条回复旧链） | 不受这次删除影响，仍可用 |
| 贴纸收发 | 独立贴纸服务 | 不受影响 |
| 记忆的增删改查 / 长期记忆库 | 独立记忆服务 | 记忆读写入口保留；但「说话时写进记忆」那一步随旧链删而断 |
| 角色档案、设置、文件夹、后端模型启停 | 各自独立服务 | 不受影响 |

一句话给作者：**这些能力现在全由旧链供，新链还没接线；所以「不进发布包」这件事要在第二片先把新链接上、再删旧链，否则她机器上的程序会当场失去全部对话行为，而不是平滑换代。**

---

## 五 反证（本波唯一的「红」：证明这张表能被复算、不是抄的）

正证已在各表贴命令。这里按工单 §二第 2 项**故意写错一行调用方清单**，验证按命令复算会暴露，再改对。

故意写错（错误版，填进表 A-2 的 `ReplyPlanner.cs` 调用方）：

> 「ReplyPlanner → 生产调用方：0 命中」

复算命令（与全表同一口径）：

```
grep -rln --include=*.cs "\bReplyPlanner\b" src/ | grep -v '/obj/' | grep -v '/bin/' | grep -v 'src/Pry.Core/TurnTaking/ReplyPlanner.cs'
```

当场输出（暴露那 3 行）：

```
src/Pry.Api/Runtime/RuntimeAdapters.cs
src/Pry.Api/Services/ConversationSessionService.cs
src/Pry.Core/TurnTaking/ConversationTurnManager.cs
```

→ 该错误行**当场被这条命令证伪**（实际 3 个生产调用方，不是 0）。改对后的正确行＝表 A-2 中所写：`Api/Runtime/RuntimeAdapters（ReplyPlannerComposer）、Api/Services/ConversationSessionService、Core/TurnTaking/ConversationTurnManager`。反证成立：任何一格调用方数若被抄错，守门人用同一条命令在 `d82cd2d` 上就能对出来。

---

## 六 出口读数（原样命令＋输出）

1. 主树 `git status --short`（收尾当场实测）与开工同一，且本波**只新增本报告那一行**；他人两枚暂存项 `M ` 一字未动：

```
git -C C:/Users/24782/Desktop/Pry_Project status --short
M  docs/dispatch-tip1020-02-memory-group-judgment.md
M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs
?? .qoder/
?? docs/character-learning-safety.patch
?? docs/eval/reports/2026-10-23-hds004-lifecycle-coordinator.md
?? docs/eval/reports/2026-10-23-hds005-sqlite-commit-delivery.md
?? docs/eval/reports/2026-10-23-hds010-legacy-chain-disposition.md   ← 本报告（本波唯一新增）
?? docs/eval/reports/2026-10-23-qa-wave18-unheard-gates.md
?? docs/research/HDS_INTERLUDE.md
?? docs/research/HDS_QQ_RESEARCH_INDEX.md
?? docs/research/QQ_BRIDGE.md
```

  如实登记一处偏差：收尾读数里除本报告外**还多出** `?? docs/eval/reports/2026-10-23-qa-wave18-unheard-gates.md`——那是**第十八波（在飞 lane）在本波运行期间自行落盘的报告**，不属本波差集，本波既未创建也未触碰它（对他人名下 `??` 只读、不改、不提交，照 §〇 边界）。所以本波对主树的净新增严格＝本报告一行；其余 `??`（含 wave18 那份、`.qoder/`、`docs/research/*`、`character-learning-safety.patch`、hds004/hds005 两份）全是他人名下。

2. 载体零落盘（报告不进载体）：`git -C …/carrier status --short | wc -l` ＝ **0**（当场实测）。
3. 硬出口（本波零产品码改动）：

```
git -C …/carrier diff --stat d82cd2d -- src tests scripts Pry.slnx Directory.Build.props
（无输出＝空）
git -C …/carrier rev-parse HEAD → d82cd2d182e3c433939588ac79f82f04cbeda151
```

4. 本波 `dotnet` 调用次数＝**0**（无 build／test／publish，也不需起 game-preflight，因本波不起 dotnet）。
5. 报告行数与四张表行数（写完当场用 gitignored 脚本 `count.js` 实数，非估）：

```
wc -l（含尾随空行）＝394
表 A 数据行数＝73  （A-1 15 ＋ A-2 6 ＋ A-3 2 ＋ A-4 1 ＋ A-5 12 ＋ A-6 2 ＋ A-7 32 ＋ A-8 3 ＝ 73）
表 B ＝命令证据格式，`^| ` 管道行 0；已证死入口＝1 枚（ConversationService.cs），同族候选复扫后＝仅 1 枚
表 C `^| ` 数据行＝9  （判据分项基线表 5 行 Core/Api/App/Client/合计 ＋ 删除片表 4 行）
表 D `^| ` 数据行＝10 （十项能力）
缺口（产品语义继续、新链尚无等价格）＝5 处，全押片 4

---

## 七 守门人复检与就地更正（2026-10-23；登记人：守门人。§〇～§六 原文一字未动，逐字节备份＝`.local-notes/wave19-report-backup-before-audit.md`）

复检方式：**零 `dotnet`**。全部读数当场重取自两处盘上产物——① `d3fbac4` 干净 detached 副本那轮全量的四份 TRX（`.local-notes/qa-wave134-merge-1023/runs/run-full/`，`<Counters>` 逐份读）；② 本波干净载体 `.local-notes/qa-wave19-1023/carrier`（HEAD `d82cd2d182e3c433939588ac79f82f04cbeda151`、`git status --short | wc -l`＝0）。另有两处 lane 判定被我用不同仪器口径复核（见 §七.1 末条、§七.4），结论均维持。

### 七.1 采纳（复算一致）

- **5 处缺口逐一点名**：InputBuffer（攒回合）、Interruption（打断／倾听）、InterruptClassifier（打断分类）、BehaviorDecision→Expression（换形）、ReplyPacing（气泡节奏）。⇒ **「替代」未达成**这条判定成立，删除片在接线前不可起跑。
- **片 1 死入口**：`grep -rln --include=*.cs "\bConversationService\b" src/ | grep -v /obj/ | grep -v /bin/` 在干净载体与主树两侧都只命中自身定义文件 ⇒ 生产调用方 0，采纳。
- **新链零生产构造点**采纳，但补一条仪器纪律（结论对、口径不够）：唯一那条生产 `new` 是字段初始化器 `private readonly DataDirectoryWriteGate _gate = new();`（`src/Pry.Api/Services/DataDirectoryWriteLease.cs:19`），**只 grep `"new DataDirectoryWriteGate("` 会假报 0 命中**。今后「零构造点／唯一构造点」这类判定必须两条口径同跑：`new X(` 与 `= new(`／`new()`。守门人本轮就差点把这条读反，记下不归 lane。

### 七.2 就地更正①：「合计 1219」是静态扫描数，**不得**作删除片判据重基线的算基；算基＝TRX 1244

当场四程序集对账（静态＝`[Fact]`＋`[InlineData(` 口径，同 §〇.2；TRX＝`<Counters>`）：

| 分项 | 静态 | TRX `total` | 差 |
|---|---|---|---|
| Core | 598（481＋117） | **623**（executed 621） | **+25** |
| Api | 280（222＋58） | 280 | 0 |
| App | 233（157＋76） | 233 | 0 |
| Client | 108（70＋38） | 108 | 0 |
| 合计 | **1219** | **1244**（Passed 1242、NotExecuted 2、failed 0） | +25 |

Core 那 25 格差**当场全部对上账、无残留未知**，两条来源都经盘上实证：

- **+22**：`tests/Pry.Core.Tests/IntelligenceRepositoryConformanceTests.cs:25` 是 `public abstract class IntelligenceRepositoryConformanceTests`，内含 22 枚 `[Fact]`（当场 `grep -c '\[Fact\]'`＝22）。静态口径按文件只数一次，xUnit 按**派生类**各跑一遍：`:504` `InMemoryIntelligenceRepositoryConformanceTests` 与 `tests/Pry.Core.Tests/SqliteIntelligenceRepositoryConformanceTests.cs:20` `SqliteIntelligenceRepositoryConformanceTests`。TRX 实测两类分别 **22 格／25 格**（25＝22 继承＋3 自有），第二类的 22 格正是静态漏掉的那 22。
- **+3**：`tests/Pry.Core.Tests/UnansweredTurnShapeTests.cs:36` 的 `[Theory] + [MemberData(nameof(UnansweredShapes))]`，数据源在 `:28` 是 `TheoryData` 且只有三行（`:30`／`:31`／`:32`）。静态规则只认 `[InlineData(`，看不见 `MemberData` ⇒ 该文件静态＝1、TRX＝**4**。

覆盖面复核（证明这 25 是全部、不是头部样本）：全仓 `[MemberData` 只出现在 Core 的这一个文件，`^public abstract class` 的测试基类全仓只有这一个——Api／App／Client 三项均为 0，所以三者的静态＝TRX 是结构上必然，不是巧合。

**判据（写进后续每一片）**：1219 只作 §〇.2 扫描口径的存档引用；判据重基线一律从 TRX `<Counters>` 起，按四个程序集分列记账。当前可引用的干净副本读数＝**1244／failed 0／NotExecuted 2**，两枚跳过逐名＝`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`、`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`。凡改用静态数起算，必须先跑「`MemberData` 全仓扫＋抽象测试基类全仓扫」两条复查命令并把漏计项加回，才准进账。

### 七.3 就地更正②：表 C 三处格数与 TRX 不符、两处程序集归属要钉死（删除片按本节数走）

| 类 | 表 C 原写 | TRX 实测 | 程序集（TRX `className` 实证） | 差的归因 |
|---|---|---|---|---|
| `RuntimeComponentTests` | 23 | **33** | `Pry.Core.Tests` | 该格只取了 `[Fact]` 数，漏同文件 10 枚 `[InlineData(`（当场：Fact=23、Inline=10） |
| `RuntimeRaceTests` | 11 | **13** | `Pry.Core.Tests` | 同上，漏 2 枚 `[InlineData(`（Fact=11、Inline=2） |
| `UnansweredTurnShapeTests` | 1 | **4** | `Pry.Core.Tests` | `MemberData` 三行不可见（§七.2） |
| `CharacterIntelligenceTests` | 38 | 38 ✓ | **`Pry.Core.Tests`** | 数对；表 C 那句把它写在「Api …」分句之后未标程序集，实读易当成 Api，此处钉死 |
| `ModelReplyFormatTests` | 16 | 16 ✓ | **`Pry.Core.Tests`** | 同上 |
| `RuntimeTriggerTests` 22／`RuntimeTriggerIntegrationTests` 4／`ConversationRuntimeTests` 23／`EvaluationPersistenceTests` 7 | — | 逐格相同 | `Pry.Core.Tests` | 采纳 |
| `ReplyFrequencyHostPortTests` 3／`PlaceholderDefinitionStaysInCoreTests` 3 | — | 逐格相同 | `Pry.Api.Tests` | 采纳 |

归属不是文字问题：片 2／片 3 的「连带消失判据格」要按 Core／Api／App／Client 四本分账重开，把 38＋16 记在 Api 名下会让两本账同时错（Api 少 54、Core 多 54）。可复算命令口径（当场跑的这把）：

```
node .local-notes/trx-perclass-1023.js .local-notes/qa-wave134-merge-1023/runs/run-full \
  RuntimeComponentTests RuntimeRaceTests UnansweredTurnShapeTests CharacterIntelligenceTests …
```

（按 `<UnitTestResult testName="Pry.<X>.Tests.<Class>.` 前缀逐类计数，避开 TRX 不写源文件名的限制；这把脚本 gitignored，不进版本控制。）

### 七.4 一处仪器偏差登记（不归 lane，但必须写在册）

守门人第一次在**主工作树**跑同一套静态计数，得到 App＝**236**（对在册／TRX 的 233 多出 3）。当场逐文件定位：3 格全来自他人名下、已暂存的 `tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`（工作副本 8 个测试属性 vs HEAD 5 个）。lane 的 233 取在 `--detach` 干净副本，**正确**。由此定一条纪律并接线到派单模板：**判据格复算只在干净副本跑**；主树那一轮的 236 当场作废、不进任何账，也不写成回归。

### 七.5 报告自报数与守门人读数的一处口径差

§六.5 写「wc -l（含尾随空行）＝394」，守门人当场 `wc -l`＝393、按 `\n` 切分＝394（文件以换行结尾，两种仪器都对同一份字节各说一个数）。不涂改 §六 原文；本节后的行数以 `wc -l` 读数记录。

### 七.6 采纳后的下一步（只说顺序与前置，不扩范围）

1. **HDS-010 删除片仍不可起跑**。前置三件缺一不可：新链在生产路径上接线（当前零调用方）、片 4 那 5 处缺口各有等价格、`PROJECT_PLAN:37` 数据迁移方案拿到作者放行（U-2／U-3／U-11 未拍）。
2. **打包形状门不单建、不提前建**，押在删除片同一片内：先在旧链未删的树上把门建好并 `dotnet publish` 取**红凭证**（今天必红，因为旧链类型确实在产物里），同一片内删旧链后重跑取**绿凭证**。理由：门若提前进 `scripts/package-release.ps1`，会让所有出包动作在删除片落地前一直失败——那是把还没到期的账变成今天的故障。门的判据形状＝对 publish 目录里的程序集扫类型名（.NET 元数据里类型名以字符串存储，可直接 `grep`），红／绿两凭证各留一轮 publish 的原样命令与退出码。
3. **作者那半句「构建时不要把旧链路打包进去」今天仍未达成，并且不存在一条「不删代码就能满足」的现成机制**：`scripts/package-release.ps1` publish 的是 `src/Pry.App/Pry.App.csproj`，经 `ProjectReference`（`src/Pry.App/Pry.App.csproj:16-19`）传递把 Pry.Core／Contracts／Client／Api 整个程序集拉进产物；.NET 无法从被引用程序集里排除某个命名空间。所以这半句只能按「删完之后过门」实现，任何页面都不许写成已满足。
4. 快照引用 `archive/legacy-chain-last-buildable`→`d82cd2d`：若删除片落地前又有旧链面上的代码刀合入，按 `docs/legacy-chain-deprecation.md` §3 那条把引用前移到「删除那一刀的父提交」并重取字节相同论证，不许引用停在过期 SHA 上。
