# 第十三波·HDS-003 新生成中心隔离原型（Narrator 形状）报告

- 报告 id：`eval-QA-WAVE13-1023`
- 工单：`docs/dispatch-hds-wave13-20261023.md`（`HDS-WAVE13-1023`，基点 **`ac58245`**）
- 执行方：冷启动 lane（除工单与四份必读外无先验上下文）
- 时点：当场 `date` ＝ `Fri Oct 23 14:29:18 2026`（下文各节时点另取）
- 性质：动产品代码的原型刀；交付物＝新文件，零既有契约语义改动

---

## 〇 开工三件事（原样读数）

### 〇.1 主树状态（未读他人内容，只数行）

`git -C C:/Users/24782/Desktop/Pry_Project status --short --branch` 当场输出：

```
## codex/ui-main-window-redesign
M  docs/dispatch-tip1020-02-memory-group-judgment.md
M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs
?? .qoder/
?? docs/character-learning-safety.patch
?? docs/dispatch-hds-wave13-20261023.md
?? docs/dispatch-hds-wave14-20261023.md
?? docs/dispatch-hds-wave15-20261023.md
?? docs/research/HDS_INTERLUDE.md
?? docs/research/HDS_QQ_RESEARCH_INDEX.md
?? docs/research/QQ_BRIDGE.md
```

主树 `git rev-parse HEAD` ＝ `ac582459fcbb6c6cdcb3158dfba59b24f42f72af`。他人名下两枚暂存项（工单 §〇 点名的那两行）全程未读、未改、未提交、未 `stash`、未 `clean`。

### 〇.2 载体入场两行判据

建载体命令（原样）：

```
git -C C:/Users/24782/Desktop/Pry_Project worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave13-1023/carrier ac58245
Preparing worktree (detached HEAD ac58245)
HEAD is now at ac58245 docs(qa,wave12): 第十二波收口入库——……基线顶到 1179/0/2，工单两处就地更正，作者大跨步现令逐字入册
```

在载体目录 `C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave13-1023/carrier` 当场读回：

```
=== git status --short | wc -l ===
0
=== git rev-parse HEAD ===
ac582459fcbb6c6cdcb3158dfba59b24f42f72af
=== stash list ===
0
```

判据成立：行数 **0**、HEAD ＝ `ac58245` 全号。

### 〇.3 边界遵守情况（自证口径）

- 只新增文件；`NarrativeDraftValidator.cs`／`NarrativeDocuments.cs`／`NarrativeContractReason.cs` 三枚磁盘内容与 `ac58245` 的 blob 逐字节对平（收尾贴两个哈希）。
- 未接生产库、未接 API/Client、未动 `ContextBuilder.cs`、未动 SQLite 仓储。
- provider 全部走注入的假实现；真实模型那半登记为「未验证」。
- 数字门槛（`.7`／`.5`／`2400`／五档超时／`12`／`4096`／`128`／`512`）与 `docs/resource-limit-ledger.md`、`licenses/**`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md` 零改动；本波对既有数字的唯一动作是**引用**（见 §二.0 复用清单），不是复制一份新的门槛常量。
- 在册基线 1179／0／2 只作对照，不自封、不改写。

---

## 一 T1｜现状盘点（纯读码，零构建）

### 一.1 `src/Pry.Core/Narrative/` 现有类型清单（文件名＋公开类型＋职责一句话）

当场 `wc -l src/Pry.Core/Narrative/*.cs` ＝ 8 个文件／2354 行。逐枚公开类型（`grep -n "^public \|^internal "` 原文摘录）：

| 文件 | 公开类型（行号） | 职责一句话 |
|---|---|---|
| `NarrativeContractReason.cs` | `enum NarrativeContractReason`（:8）；`class NarrativeContractException`（:102）；`internal static class NarrativeBounds`（:108） | 契约拒绝的稳定分类码（分组留空位）＋结构非法的异常＋ID 长度/字符集护栏 |
| `NarrativeDocuments.cs` | `NarrativeContractVersions`（:5）；`InputRef`（:27）；`ProcessedThroughSeq`（:46）；`PerceivedInputRefs`（:74）；`enum NarrativeTriggerKind`（:93）；`enum CommunicationMode`（:101）；`enum DeliverySegmentState`（:112）；`enum NarrativeIntentStatus`（:121）；`enum CognitionKind`（:131）；`enum CognitionOrigin`（:148）；`NarrativeRequest`（:162）；`ProseBinding`（:243）；`CommunicationAction`（:259）；`NarrativeIntent`（:284）；`KnowledgeCandidate`（:302）；`NarrativeDraft`（:322）；`NarrativeCommit`（:353）；`DeliverySegment`（:391）；`DeliverySegmentKey`（:421）；`internal NarrativeGuard`（:423） | HDS-002 的纯领域类型与文档形状：宿主签发的请求、模型侧草稿、提交、投递段、意图与认知候选 |
| `NarrativeDraftValidator.cs` | `ContractRejection`（:4）；`NarrativeContent`（:10，含 `From(draft)`／`From(commit)`）；`NarrativeProseBinder`（:38，`Bind`＋`Apply`）；`NarrativeDraftValidator`（:105，`Validate`＋私有 `CheckConsumption`/`CheckAction`）；`NarrativeIntentPolicy`（:261）；`KnowledgeCandidatePolicy`（:301）；`NarrativeCommitValidator`（:381） | 草稿与提交的共同语义校验、原文↔正文绑定、意图与认知候选两条政策 |
| `CharacterInstanceIdentity.cs` | `CharacterInstanceId`（:8）；`CharacterInstanceKey`（:48）；`enum InstanceProvenance`（:73）；`CharacterInstanceRegistration`（:84）；`IdentityMigrationOptions`（:92）；`IdentityMigrationRequest`（:101）；`InstanceMigrationOutcome`（:107）；`CharacterInstanceRegistry`（:119）；`InstanceResolution`（:478） | User × Character → 稳定实例的映射、登记、迁移与证据引用归属 |
| `DataDirectoryWriteGate.cs` | `enum WriteLeaseCredentialKind`（:4）；`DataDirectoryObservation`（:14）；`WriteLeaseCredential`（:30）；`WriteLeaseGrant`（:33）；`DataDirectoryWriteGate`（:52） | 数据目录进程级独占写锁的判定形状（含 fencing 代次） |
| `OutboxOwnershipRegistry.cs` | `enum EngineMode`（:7）；`enum SendableTruthSource`（:17）；`WriteOwnershipTicket`（:26）；`OwnershipGrant`（:34）；`OutboxOwnershipRegistry`（:49） | 实例写入权、显式交接、代次推进与"可发送真相只能有一份" |
| `DeliveryLedger.cs` | `SegmentOutcome`（:5）；`internal RegisteredCommit`（:14）；`DeliveryLedger`（:33） | 投递段账本：唯一键、逐字来自绑定原文、终态不可翻转、前段未落不许后段落 |
| `NarrativeCommitWriter.cs` | `NarrativeInstanceState`（:4）；`ConditionalCommitOutcome`（:19）；`NarrativeCommitWriter`（:39） | 条件写入口：身份/代次/故事版本/水位四项同时成立才落，并登记到投递账本 |

关键形状（工单 §一.1 点名的几枚，逐条对着盘上原文读）：
`NarrativeRequest`（:162）＝宿主签发的资格凭据（实例、会话、批次、`Inputs`、`Trigger`、三个 epoch、`IssuedByWriter`、`ContractVersion`、`MaxOutboundSegments = 4`、`RequiresInput`、`ProcessedThroughAtIssue`）；`NarrativeDraft`（:322）＝模型的一次主叙事提案（含 `NarrativeProse`、`ProcessedThrough`、`PerceivedInputRefs`、`Actions`、`Intents`、`KnowledgeCandidates`、`ProposalId`）；`CommunicationAction`（:259）＝通信决定（`Mode`、`ProseQuote`、`PlannedSegmentCount`、`DeferredIntentId`、`Binding`）；`NarrativeIntent`（:284）＝有 `SourceCommitId` 的未来动机；`KnowledgeCandidate`（:302）＝带 `Kind`/`Origin`/`Confidence`/`EvidenceRefs` 的认知候选；`CommunicationMode`（:101）＝ `Silence`/`Immediate`/`Deferred` 三值；`ContractRejection`（:4）＝`(Reason, Detail)`；两个政策体＝`NarrativeIntentPolicy`（:261）与 `KnowledgeCandidatePolicy`（:301）。

**结论：HDS-002 的契约面齐备，但它只到"文档形状＋判定"为止——盘上没有任何一枚"生成侧"类型**（见一.4 差集）。

### 一.2 旧链装配器实际有几段（`ContextBuilder.cs` 全文逐段点名）

文件：`src/Pry.Core/Prompting/ContextBuilder.cs`（196 行）。当场 `grep -n "Section(" ` 原文：

```
54:public sealed record DroppedSection(string Section, string Reason);
120:        void Section(string name, string text, int cap)
144:        Section("当前状态", request.State, b.StateTokens);
179:                    Section("相关记忆", text, memoryBudget);
186:        Section("行为参考", request.BehaviorReferences, b.BehaviorTokens);
187:        Section("表达参考", request.ExpressionPatterns, b.ExpressionTokens);
188:        Section("能力结果（引用资料）", request.CapabilityResult, b.CapabilityTokens);
```

按"这段东西是否被独立计入成本、独立拒绝或独立丢弃"逐一点名，盘上是 **8 个装配单元**：

| 单元 | 盘上出处（同一文件行号） | 内容 | 预算字段 | 丢弃/拒绝形状 |
|---|---|---|---|---|
| U1 必需前奏 | :116 `var prompt = request.Identity + "\n" + request.OutputRules;` ＋ :117 `used = _tokens.Count(prompt) + 8 + MessageCost(request.CurrentEvent);` ＋ :118 `if (used > total) throw new ContextBudgetException();` ＋ :110 CurrentEvent 作用域/角色校验 | Identity ＋ OutputRules ＋ CurrentEvent **三个输入位共用一次核算与一道门** | **无**（不参与可裁预算） | 抛 `ContextBudgetFailure.IdentityAndCurrentEvent`，不丢弃 |
| U2 Advisory | :132–143 预留、:190 逐字追加 | 本轮决策块 | `AdvisoryTokens`（2400） | `dropped.Add(new("Advisory","budget"))` ＋ 抛 `AdvisoryDoesNotFit` |
| U3 当前状态 | :144 | State | `StateTokens` | 整段丢弃，记 `("当前状态","budget")` |
| U4 近期消息 | :145–155 自有循环 | RecentMessages | `RecentTokens` | 逐条从最旧方向停（:152–153 `break`）；作用域/角色不合记 `scope_or_role`（:148–149） |
| U5 相关记忆 | :157–185（含 :163/:166 两次 `RevisionAsync` 复核、:164 `Confidence >= .5` 资格、:172 `Take(6)`） | Memory | `MemoryTokens`（逐条递减钱包 :171–180） | `scope_or_unavailable`／`memory_changed_during_assembly`／`stale_or_deleted`／`repository_unavailable`／`("相关记忆","budget")` |
| U6 行为参考 | :186 | BehaviorReferences | `BehaviorTokens` | 整段丢弃 |
| U7 表达参考 | :187 | ExpressionPatterns | `ExpressionTokens` | 整段丢弃 |
| U8 能力结果 | :188 | CapabilityResult | `CapabilityTokens` | 整段丢弃 |

三种可复核的数法（都当场取证，不择一藏其他）：

- **装配单元 ＝ 8**（上表；这是"段"在盘上唯一同时具备名字、成本核算与丢弃形状的单位）。
- **带独立预算字段的段 ＝ 7**：`ContextBudget`（:6–8）里按段的字段是 `RecentTokens`、`MemoryTokens`、`StateTokens`、`BehaviorTokens`、`ExpressionTokens`、`CapabilityTokens`、`AdvisoryTokens` ＝ **恰好 7 个**；U1 必需前奏没有预算字段。**"七段"这个数字在盘上的出处就是这七个预算字段，不含必需前奏。**
- **`ContextRequest` 内容输入位 ＝ 10**（:50–53 去掉 `Scope`/`ConversationId`/`Budget` 这三项路由与配置后）：Identity、OutputRules、CurrentEvent、RecentMessages、Memory、State、BehaviorReferences、ExpressionPatterns、CapabilityResult、Advisory。

**登记与 v4 的不一致（以盘上为准）**：v4 §6（`docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md:158`–:171）给的七段是**语义分区**（`storyIdentity`、`relevantEstablishedEpisodes`、`currentSceneEvidence`、`ongoingThreads`、`availableNearFuture`、`incomingEvent`、`authoringWindow`），与旧链的 8 个装配单元**不是一套东西**：旧链有 `行为参考`／`表达参考`／`能力结果` 三段而 v4 分区里没有；v4 的 `ongoingThreads`／`availableNearFuture` 在旧链里没有对应段。本波按工单 §二.1「照 T1 从 `ContextBuilder.cs` 盘到的实际段，一一对应」实现 **8 段**，段名取盘上 `ContextRequest` 属性名，并把 v4 §6 的分区名作为**只读参考字段**挂在每段上（对不上的留空，不硬凑）。

### 一.3 现有生产入口到回复生成的跳数（点到，不改）

当场取证：

```
src/Pry.Core/Conversation/ConversationService.cs:27:  var context = await new ContextBuilder(database.LongTerm).BuildConversationAsync(
src/Pry.Core/TurnTaking/ReplyPlanner.cs:64:           var context = await new ContextBuilder(_database.LongTerm).BuildConversationAsync(
src/Pry.Api/Services/ConversationSessionService.cs:279/281: new ReplyPlanner(...) → new ReplyPlannerComposer(planner, runtime.TurnSettings).ComposeAsync(...)
src/Pry.Api/Runtime/RuntimeAdapters.cs:72:             public sealed class ReplyPlannerComposer(ReplyPlanner planner, TurnSettings settings) : IReplyComposer
src/Pry.Core/Intelligence/CharacterIntelligenceService.cs:35/50: private readonly BehaviorPlanner _planner; ... new BehaviorPlanner()
src/Pry.Core/Intelligence/BehaviorPlanner.cs:10:       public BehaviorDecision Plan(SituationSnapshot, CharacterStateSnapshot, ...)
src/Pry.Core/Intelligence/ReplyGenerationRenderer.cs:10/30: public static class ReplyGenerationRenderer ... public static string Render(...)
```

跳数（被动消息链）：`Controller → ConversationSessionService(:279) → ReplyPlannerComposer(RuntimeAdapters.cs:72) → ReplyPlanner(:64) → ContextBuilder.BuildConversationAsync(→ BuildAsync) → ModelRouter.Select/StreamAsync → ReplyPlanner.Parse → 落库助手消息`；主动/行为决策从 `CharacterIntelligenceService(:35) → BehaviorPlanner.Plan(:10) → ReplyGenerationRenderer.Render(:30)` 产出 `advisoryBlock`，再由 `ReplyPlanner.cs:13` 那句注释所述走 Advisory 通道进同一个装配器。

另一条链 `ConversationService.cs:27` 直接 `ContextBuilder → model.StreamAsync → database.AddMessageAsync(ChatRole.Assistant, ...)`（:55），即"流式边生成边落历史"，与 `ReplyPlanner` 的"整轮解析后落库"是**两个不同的助手消息生产者**。

### 一.4 差集：新链已有／新链缺（每项给"凭什么说它缺"的证据）

| 项 | 状态 | 证据（当场命令与命中） |
|---|---|---|
| 叙事文档类型（Request/Draft/Commit/Action/Intent/Candidate/Segment） | **已有** | 一.1 清单，`NarrativeDocuments.cs` 全部公开类型 |
| 草稿与提交校验、意图/候选两条政策 | **已有** | `NarrativeDraftValidator.cs:105/:261/:301/:381` |
| 原文↔正文绑定 | **已有** | `NarrativeProseBinder.Bind/Apply`（`NarrativeDraftValidator.cs:40/:90`）＋ `DeliveryLedger.TryAppend` 的逐字还原判定（`DeliveryLedger.cs:143–155`） |
| 投递段账本、条件写、写入权 | **已有** | `DeliveryLedger.cs:33`、`NarrativeCommitWriter.cs:39`、`OutboxOwnershipRegistry.cs:49` |
| **新链的上下文装配器（Compiler）** | **缺** | `grep -rln "NarrativeContextCompiler\|ContextCompiler\|IContextCompiler" src/ tests/` → **0 个文件**；`src/Pry.Core/Narrative/` 里没有任何装配/预算类型（一.1 全清单可见），唯一的装配器 `ContextBuilder` 在 `src/Pry.Core/Prompting/`，其入参是旧链 `ContextRequest`（含 `MemoryScope`、`ChatMessage`），不消费 `NarrativeRequest` |
| **一次主叙事的调用契约（INarrator）** | **缺** | `grep -rn "INarrator\|NarratorCall\|class Narrator" src/ tests/` → **0 命中**（见一.4 附注）；盘上模型侧只有 `Pry.Core/Inference` 的流式 token 端口与 `ReplyPlanner.Parse` 的文本解析，不存在"一次调用产出 `NarrativeDraft`"的类型 |
| **沉默/延迟的分岔与"不产生助手消息／不进真实历史"的裁决者** | **缺** | `CommunicationMode.Silence/Deferred` 只有判定（`NarrativeDraftValidator.cs:222/:248`），没有任何组件把模式映射成"本轮结果的形状"。当场原文（三条都是实测，不是推演）：`grep -rn "CommunicationMode.Silence" src/ --include=*.cs` ＝ **0 命中**（`rc=1`）；`grep -rn "Silence" src/ --include=*.cs` 只有 3 行＝枚举声明 `NarrativeDocuments.cs:104`、校验文案 `NarrativeDraftValidator.cs:142`、旧链无关的 `BehaviorPattern.cs:65 Silenced`；`grep -rn "CommunicationMode" src/ --include=*.cs` 只有 7 行且全在契约文件里 |
| **把装配结果喂给一次调用、再把 draft 交给既有校验的编排者** | **缺** | 新链 `src/Pry.Core/Narrative/` 无编排类型；生产链的编排者 `ConversationService`/`ReplyPlanner` 走的是旧装配器（一.3），且 `grep -rn "Pry.Core.Narrative" src/` 在 `Conversation/`、`TurnTaking/`、`Intelligence/` **0 命中** ⇒ 新契约今天完全没被生产链消费（这正是本波的隔离面） |
| **provider 注入点（可换假实现）** | **缺** | `INarrator` 不存在 ⇒ 无注入口；新链目录内没有任何 `interface`（一.1 清单里 `Narrative/` 无接口类型） |
| `ApplicationService`（工单 §一.3 点名的入口） | **盘上不存在** | `find src -name "ApplicationService*.cs"` → **0**；`grep -rn "ApplicationService" src/ --include=*.cs` → 无该类型。实际入口是 `ConversationSessionService` 与 `CharacterIntelligenceService`。**登记为工单锚点与盘上不符**（不影响本波范围，因为本波不接线） |
| `CharacterBelief` 契约条款 | **缺（不在本波）** | 第十二波 §9 已登记：`src/` 内只出现在枚举声明行。本波不补（那是功能刀，工单 §〇.4 第 1 条冻结契约面） |

### 一.5 本波只做的那几段（结论）

按一.4，本波要新落的缺项是四件：**装配器（8 段）→ 一次 Narrator 调用契约 → 沉默/延迟分岔与原文行动绑定的编排裁决 → provider 注入点（假实现）**。全部落在 `src/Pry.Core/Narrative/` 新文件，消费一.1 的既有类型，不新造平行的身份/草稿/提交契约。

（§二 起为落码与判据，增量落盘。）

---

## 二 T2｜落原型（四枚新文件，消费既有契约）

### 二.0 落码位置更正（自纠，先登记再往下写）

当场 `date` ＝ `Fri Oct 23 14:49:19 2026`（本节收尾另取）。

登记一项本波自己的走偏：**四枚新文件先落在了主工作树，而不是载体**。14:42–14:45 之间我按工单 §〇.4「允许在这些路径新增文件」的字面路径（`src/Pry.Core/Narrative/`、`tests/Pry.Core.Tests/`）写进了主工作树，漏了 §〇.2「建独立载体（**不动主工作树**）」这条前置。14:48 已按下面顺序纠正，当场取证：

```
cp（主树 → 载体）后逐枚 sha256sum 双份对平：
NarrativeContextCompiler.cs   a3d0c4fe066d59083bcc8084f0c8ae9fabe7505a5fd76d0234e13bbb05acfceb  两份相同
NarrativeRoundRunner.cs       99efc13bbd8c7ed5c27142ebf98dcd7cdb67e852dc456e73cd7ddc3ab31241c1  两份相同
NarratorContracts.cs          3a33a8151e61c3396963dac451dffcb46acb2f02807f140d18f694d6a52a9f8c  两份相同
NarratorPrototypeTests.cs     29a17d6684894077564bff36633f2fbde537d1a25d9978a6451a9f84c11922de  两份相同
```

随后 `rm` 主树那四份（只删我自己名下这四枚未跟踪项，未碰他人名下任何一项），主树 `git status --short` 回到开工时的形状：他人两枚暂存项＋既有未跟踪文档＋我这一份报告，**零源码项**。载体 `git status --short` ＝ 4 行，全是我名下那四枚新文件。更正之后的所有构建、突变、还原、提交都只在载体目录里发生。

### 二.1 交付物清单（全部为新增文件）

| 文件（载体相对路径） | 行数 | 公开类型 | 职责 |
|---|---|---|---|
| `src/Pry.Core/Narrative/NarratorContracts.cs` | 124 | `NarratorOutcomeKind`／`NarratorCall`／`NarratorOutcome`／`INarrator`／`NarratorCallBudget`／`NarrativeContextAssemblyRejectedException` | 一次主叙事调用的端口、凭据、结果四形状与"唯一一次"的强制点 |
| `src/Pry.Core/Narrative/NariveContextCompiler.cs`（正确名 `NarrativeContextCompiler.cs`） | 470 | `NarrativeContextCompiler`／`NarrativeSegmentTrim`／`NarrativeSegmentDisposition`／`NarrativeCompileReason`／`NarrativeContextUnit`／`NarrativeContextPayloads`／`NarrativeContextSegment`／`NarrativeContextAssembly`（＋ `internal NarrativeContextAssemblySegmentFactory`） | 8 段装配器：预算门、必需前奏门、逐字预留门、整段/整单元裁剪、统一预算不等式自检 |
| `src/Pry.Core/Narrative/NarrativeRoundRunner.cs` | 340 | `NarrativeRoundDisposition`／`NarrativeRoundResult`／`NarrativeRoundRunner` | 编排者：装配 → 恰好一次调用 → 快照三项 → 照用既有绑定与校验 → 沉默/延迟/即时三分岔 |
| `tests/Pry.Core.Tests/NarratorPrototypeTests.cs` | 821 | `NarratorPrototypeTests` | 46 枚 `[Fact]` ＋ 1 枚 `[Theory]`（3 个 `[InlineData]`）＝ **49 格** |

行数当场 `wc -l`（124 / 470 / 340 / 821）；上表第二行文件名那列的笔误按盘上正名 `NarrativeContextCompiler.cs` 读，载体 `git status` 与哈希都以正名为准。

### 二.2 复用了既有哪几件（引用，不复制判定）

**引用的既有类型与出口**（全部只在新增文件里被调用，三枚契约文件零改动）：
`NarrativeRequest.Validate()`（`NarrativeDocuments.cs:201`）、`NarrativeGuard.RequireText`（:423）、`NarrativeDraftValidator.Validate(NarrativeContent, NarrativeRequest)`（`NarrativeDraftValidator.cs:107`）、`NarrativeProseBinder.Apply`（:90）、`NarrativeContent.From(draft)`（:24）、`ContractRejection`（:4）、`CommunicationMode`／`NarrativeIntent`／`KnowledgeCandidate`／`DeliverySegment`、`DeliveryLedger.RegisterAppliedCommit`（`DeliveryLedger.cs:51`）与 `TryAppend`（:66）、`OutboxOwnershipRegistry.TryAcquire`（`OutboxOwnershipRegistry.cs:69`）、`EngineMode.NarrativeV1`、`NarrativeContractReason` 既有取值、`ITokenEstimator`／`ConservativeTokenEstimator`（`src/Pry.Core/Memory/MemoryContracts.cs:110`–:111）、`ContextBudget`（`src/Pry.Core/Prompting/ContextBuilder.cs:6`）。

**没有复制的判定**（本波刻意不带过来）：
- 记忆的 `.5` 置信地板与两次 `RevisionAsync` 复核（`ContextBuilder.cs:163`–:166）——新装配器只接"已经过检索/资格判定的完整单元"，资格判定仍留在旧链那一侧；本波不复制一份平行的 `.5`。
- `AdvisoryTokens = 2400` 的**数值**没有在新文件里重述：预算一律从注入的 `ContextBudget` 实例读（默认值 4096/512/128 也在 `ContextBudget` 自己身上，本文件不写第二份）。
- 五档超时、`12`、`.7` 等门槛：`NarrativeRoundRunner.RunAsync` 的 `narratorTimeout` 由调用方传入，本波不新增超时档位常量，也不选任何一档。
- `CheckAction`／`CheckConsumption` 两条校验出口照用（`NarrativeDraftValidator.cs`），没有平行实现。
- "投递文本必须逐字来自绑定原文"那条尺子在 `DeliveryLedger.TryAppend`（`DeliveryLedger.cs:143`–:155）里，`NarrativeRoundRunner` 只负责切片，放行与否由既有账本判。

**唯一一处照盘上取的数字**：`NarrativeContextCompiler.MessageFramingTokens = 8`（对齐 `ContextBuilder.cs:195` 的 `Count(content) + 8`）。`8` 不在工单 §〇.4 第 5 条的冻结清单里，且这里是**引用盘上同一把尺子**而不是新造门槛；登记于此，供收口时复核。

### 二.3 七段之争：按盘上落 8 段（工单 §二.1 的"以盘上为准"）

段序与段名照 §一.2 盘到的 U1–U8，每段带 `SourceRef`（指回 `ContextBuilder.cs` 的确切行区间）、`Trim` 策略、`BudgetTokens`、`Disposition`＋`DispositionReason`：

| # | 段名 | 盘上出处（写进 SourceRef 的锚） | Trim 策略 | 预算字段 | 装不下的形状 |
|---|---|---|---|---|---|
| U1 | `Identity` | `ContextBuilder.cs:116-118` | `Never`（Mandatory=true） | 无（不参与可裁预算） | 整轮不装配：`MandatoryContentMissing` / `MandatoryExceedsBudget` |
| U2 | `Advisory` | `:132-143` | `VerbatimOrReject` | `AdvisoryTokens` | `ReservedDirectiveDoesNotFit`＋段记 `reserved_directive_does_not_fit` |
| U3 | `State` | `:144` | `SectionOrDrop` | `StateTokens` | 整段丢，记 `budget` |
| U4 | `RecentMessages` | `:145-155` | `WholeUnitsOnly` | `RecentTokens` | 逐单元停，记 `budget`／`cross_scope`／`unit_budget`／`cross_scope_and_unit_budget` |
| U5 | `Memory` | `:157-185` | `WholeUnitsOnly` | `MemoryTokens` | 同上（钱包逐条递减） |
| U6 | `BehaviorReferences` | `:186` | `SectionOrDrop` | `BehaviorTokens` | 整段丢，记 `budget` |
| U7 | `ExpressionPatterns` | `:187` | `SectionOrDrop` | `ExpressionTokens` | 同上 |
| U8 | `CapabilityResult` | `:188` | `SectionOrDrop` | `CapabilityTokens` | 同上 |

v4 §6 的七个语义分区名挂在每段的只读字段 `V4Partition` 上（对不上的显式写"（v4 §6 无对应分区：……）"，不硬凑）。**§一.2 已登记的不一致在此落地**：装配体永远是 8 段（`SegmentCount = 8`），未供料的段以 `NotSupplied` 显式存在，不静默少给。

### 二.4 一次调用的形状（工单 §二.2）

`NarrativeRoundRunner.RunAsync` 的序列是唯一的：**装配一次 → `NarratorCallBudget.Consume()` 一次 → `INarrator.ProposeAsync` 一次 → 草稿直接进 `NarrativeContent.From(draft)` ＋ `NarrativeDraftValidator.Validate`**。
- `Consume()` 用 `Interlocked.Increment`，第二次索要即抛 `NarrativeContextAssemblyRejectedException(NarrativeCompileReason.SecondCallNotPermitted)`，额度不交出去——"第二次独立改写/第二个后台生成器"在形状上拿不到入口。
- `NarratorCall.Validate()` 三件事：装配体不得带拒绝理由；装配体的 `InstanceId`／`ConversationId`／`BatchId` 必须与宿主 `NarrativeRequest` 逐项相同（`NarrativeCompileReason.CallScopeMismatch`）。
- 上下文被拒时 `NarratorCallCount = 0`（`ContextRejected`），provider 一次都不调。
- 结果的四种形状互斥：`DraftProduced`／`MalformedResponse`／`TimedOut`／`ProviderFailed`；后三者在轮层一律落成 `ProviderRejected`，**不允许被读成"角色选择了沉默"**。
- 超时给两条界：① 到点取消 token（守规矩的 provider 自己停）② 硬上限竞速（无视取消的 provider 也只能以超时收场）；宿主真的取消（`cancellationToken` 已触发）时 `throw`，不伪装成任何一种轮结果。

### 二.5 沉默与延迟（工单 §二.3）

分岔用既有 `CommunicationMode`，不新造状态：
- **沉默**＝`Disposition = Silence`、`AssistantMessageCount = 0`（`HistoryEntries` 为空）、`RegisteredIntents` 为空、`OutboundActionCount = 0`；账本里一条不写。"没有任何动作"的草稿不是沉默——`NarrativeDraftValidator` 的 `RequiresInput`/空动作判定先把它拒成 `ContractRejected`（照用既有出口）。
- **延迟**＝`Disposition = Deferred`，只把 `draft.Intents` 交回作 `RegisteredIntents`，`HistoryEntries` 必须为空；到期后才由 HDS-004/007 重新叙事，本波不给"提前进真实历史"的任何路径。`DeferredIntentId` 指不到 Pending 意图、或意图自称 `Fulfilled` 却没有投递段，都由既有政策体（`NarrativeIntentPolicy`）先拒。
- **即时**＝把草稿升 `NarrativeCommit`（含内容哈希、`CommittedByWriter = writerId`）→ `RegisterAppliedCommit(commit, request.MaxOutboundSegments)` → 按绑定偏移切 `PlannedSegmentCount` 段 → 每段过既有 `DeliveryLedger.TryAppend`；任何一段被拒即整轮 `HistoryRejected`，已接受的段如实报数。

### 二.6 原文行动绑定（工单 §二.4）

动作正文一律来自当次叙事：`NarrativeProseBinder.Apply(action, draft.NarrativeProse)` 先把每条 `Immediate` 动作的 `ProseQuote` 绑成 `ProseBinding`（`Bound`/`ResolvedOffset`/`QuotedText`），再连同绑定后的动作交给 `NarrativeDraftValidator.Validate`——`CheckAction` 的四个出口（绑定缺失、歧义、段数非法、指向别的会话）照用不复制。切片拼接的逐字还原由 `DeliveryLedger.TryAppend` 判（`:143`–`:155`），所以"第二次改写"一定撞墙；每段的 `SourceRefs` 与 `NarrativeRequest.Inputs` 的 `InputRef` 同源，回指可查。

### 二.7 假 provider（工单 §二.5）

`NarratorPrototypeTests.FakeNarrator` 按注入的 `FakeMode` 产出五种形状（要求"至少两种"，实给四种可用）：`Produce`（合法 draft）／`Malformed`（截断的 JSON 片段，落 `MalformedResponse`）／`HangsPastTimeout`（`Task.Delay(InfiniteTimeSpan, CancellationToken.None)`，无视取消）／`CancelsOnToken`（`Task.Delay(InfiniteTimeSpan, cancellationToken)`，守取消）／`Throws`（`InvalidOperationException`，落 `ProviderFailed`）。真实模型与运行库不在位、也不接：那半 §六 登记为未验证。

### 二.8 条数与格子对照（当场数，不预测）

- 落码前：文件不存在 ⇒ `grep -c '\[Fact' tests/Pry.Core.Tests/NarratorPrototypeTests.cs` ＝ **文件不存在（0）**。
- 落码后同一命令（载体上，14:47）：**46**。
- `grep -c '\[Theory\|\[InlineData' tests/Pry.Core.Tests/NarratorPrototypeTests.cs`：**4**（1 枚 `[Theory]` ＋ 3 枚 `[InlineData]`）。
- 实际执行格子数 ＝ 46 ＋ 3 ＝ **49**（干净态 TRX 里 49 个 `testName`，不多不少）。

每格守哪一条 T2 行为（分组给；组名是测试文件里的分段注释原文）：

**A 组＝装配器（工单 §二.1；门在 `NarrativeContextCompiler.cs`）**

| 格 | 守的行为 | 落的判定门（文件:行） |
|---|---|---|
| `Compiler_emits_the_eight_assembly_units_of_the_disk_builder_in_its_order` | 8 段、顺序与盘上一致、每段带 `ContextBuilder.cs` 锚 | `:29` `SegmentNames` ＋ `:49`/`:93` |
| `Compiler_keeps_absent_sections_as_not_supplied_instead_of_dropping_them` | 未供料≠装不下（两回事） | `:195`–`:199`（`NotSupplied` 支） |
| `Compiler_drops_an_optional_section_that_exceeds_its_own_share_and_counts_it_exactly` | 整段留或整段丢＋计数 | `:203` `cost > cap \|\| used + cost > total` |
| `Compiler_refuses_the_round_when_the_mandatory_prelude_does_not_fit` | 不截身份凑窗口（v4 §6） | `:87` `preludeCost > total` |
| `Compiler_refuses_a_window_that_leaves_no_positive_input_budget` | 统一不等式的左半边 | `:62` `total <= 0` |
| `Compiler_refuses_a_scene_without_identity_instead_of_inventing_one` | 必需内容缺失即拒，不代造 | `:71` |
| `Compiler_refuses_a_current_event_from_another_conversation` | 事件必须落在宿主签发会话 | `:77`–`:79` |
| `Compiler_fails_loudly_when_the_reserved_directive_does_not_fit` | 必需指令不静默丢 | `:110` |
| `Compiler_trims_recent_messages_only_at_whole_unit_boundaries` | 只按完整单元取舍 | `:246` `wallet - cost < 0 \|\| used + cost > total` |
| `Compiler_removes_cross_conversation_units_and_names_the_count` | 跨会话单元整条剔除＋点名计数 | `:239`–`:243` |
| `Compiler_shrinks_the_memory_wallet_unit_by_unit` | 记忆钱包逐条递减（与上一行同一门） | `:246`＋`:254`–`:256` |
| `Compiler_honours_the_unified_budget_inequality_with_exact_numbers` | 不等式由构造保证并自检 | `:178` `!assembly.WithinUnifiedBudget` ＋ `:443`–`:444` |
| `Compiler_rejects_a_negative_section_share` | 预算字段自身非法 | `:52`–`:54` |
| `Compiler_rejects_a_window_of_zero_tokens` | 同上（同一门的另一形状） | `:52` `budget.ContextTokens < 1` |
| `Compiler_carries_source_references_into_every_emitted_section` | 每段能指回输入引用 | `:99`/`:253`/`:294` |

**B 组＝一次调用（工单 §二.2；门在 `NarratorContracts.cs` ＋ `NarrativeRoundRunner.cs`）**

| 格 | 守的行为 | 判定门 |
|---|---|---|
| `Round_calls_the_provider_exactly_once_for_one_narration` | 一轮恰好一次 | `Runner:107` `budget.Consume()` ＋ `Contracts:106` |
| `Round_does_not_call_the_provider_when_the_context_is_rejected` | 装不下不调模型（`NarratorCallCount = 0`） | `Runner:95`–`:102` |
| `Second_call_on_the_same_round_budget_is_refused` | 第二次索要被拒 | `Contracts:107` `next != 1` |
| `Call_refuses_an_assembly_that_still_carries_a_rejection` | 带拒绝理由的上下文不许喂进去 | `Contracts:36` `!Assembly.Accepted` |
| `Call_refuses_an_assembly_that_belongs_to_another_scope(scope:"instance")` | 实例不得自扩作用域 | `Contracts:43` |
| 同上（`scope:"conversation"`） | 跨会话串短期记忆 | `Contracts:50` |
| 同上（`scope:"batch"`） | 本轮不消费别批料 | `Contracts:57` |
| `Round_never_rewrites_the_narration_after_the_single_call` | 调用后不再第二次改写；改写即被既有校验/账本拒 | `Runner:136` ＋ `DeliveryLedger.cs:143`–:155 |
| `Round_feeds_the_provider_the_compiled_context_and_nothing_else` | provider 只拿到那一份装配体 | `Runner:105` ＋ `Contracts:30` |

**C 组＝沉默/延迟/错误形状（工单 §二.3）**

| 格 | 守的行为 | 判定门 |
|---|---|---|
| `Silence_round_produces_no_assistant_message_and_no_intent` | 沉默＝零外发、零助手消息 | `Runner:167`–`:171` |
| `Deferred_round_registers_the_intent_without_touching_real_history` | 延迟＝只登记意图、不进真实历史 | `Runner:155`–`:164` |
| `Draft_without_any_action_is_rejected_rather_than_read_as_silence` | 空动作≠沉默，先由既有校验拒 | `Runner:137` ＋ `NarrativeDraftValidator.cs:142` |
| `Malformed_response_is_not_masquerading_as_a_chosen_silence` | 坏 JSON 落成明确错误 | `Runner:110`–`:118` |
| `A_provider_that_ignores_cancellation_still_exits_as_timeout` | 无视取消也只能超时收场 | `Runner:313`–`:321`（竞速支） |
| `A_provider_that_honours_the_timeout_token_exits_as_timeout_too` | 守取消同样落超时 | `Runner:331`–`:334` |
| `Provider_failure_becomes_a_named_error_shape_and_writes_nothing` | provider 抛错＝`ProviderFailed`，零历史 | `Runner:335`–`:338` |
| `Host_cancellation_stays_cancellation_and_is_not_converted_into_a_round` | 宿主取消不许伪装成轮结果 | `Runner:327`–`:330` |
| `Deferred_intent_that_claims_to_be_fulfilled_without_delivery_is_refused` | 假 `Fulfilled` 意图由既有政策拒 | `Runner:137` ＋ `NarrativeDraftValidator.cs:263` |
| `Deferred_action_without_a_pointing_intent_is_refused` | 延迟必须指得到那条 Pending 意图 | 同上（同一出口） |

**D 组＝原文行动绑定（工单 §二.4，出口照用）**

| 格 | 守的行为 | 判定门（照用的既有出口） |
|---|---|---|
| `Immediate_action_binds_to_the_unique_slice_and_lands_three_segments` | 唯一切片→三段→逐字还原 | `Runner:184`–`:212` ＋ `DeliveryLedger.cs:66` |
| `Quote_absent_from_this_narration_rejects_the_action_without_host_invention` | 引用不在本次叙事即拒 | `NarrativeDraftValidator.cs:90`（`Apply`）→`CheckAction` |
| `Ambiguous_quote_is_rejected_instead_of_the_host_choosing_one` | 歧义不代选 | `NarrativeProseBinder.Bind`（:40） |
| `Immediate_action_over_the_host_segment_ceiling_is_refused` | 段数超宿主上限 | `CheckAction` ＋ `MaxOutboundSegments` |
| `An_over_split_slice_is_refused_by_the_existing_history_gate` | 空段被既有历史门拒 | `DeliveryLedger.cs:131`–:135（payload 判） |
| `Immediate_action_carrying_a_future_intent_is_refused` | 即时动作不许夹带意图 | `CheckAction` |
| `Action_targeting_another_conversation_is_refused` | 动作落在别个会话 | `CheckAction` |
| `Perceived_reference_outside_the_batch_is_refused` | 感知引用必须在批次内 | `CheckConsumption`（`NarrativeDraftValidator.cs:108` 起） |
| `Watermark_beyond_the_batch_upper_bound_is_refused` | 水位越界 | 同上 |
| `Round_refuses_a_narration_without_prose_instead_of_binding_to_nothing` | 无正文不绑定 | `Runner:136` ＋ `Bind` |

**E 组＝绑定回指与快照连续性**

| 格 | 守的行为 | 判定门 |
|---|---|---|
| `Bound_action_keeps_a_reference_back_to_the_input_it_answers` | 动作能指回输入引用 | `Runner:133`–`:135` ＋ 段 `SourceRefs` |
| `Draft_on_a_stale_story_version_is_refused_before_any_history` | 故事版本不符先拒且零历史 | `Runner:238` |
| `Draft_from_a_fenced_writer_epoch_is_refused` | 所有权代次已交接先拒 | `Runner:244` |
| `Draft_with_a_wrong_issue_baseline_is_refused_as_late` | 迟到快照先拒 | `Runner:250` |
| `Contiguous_split_reproduces_the_bound_slice_verbatim` | 切片拼接逐字等于绑定原文 | `Runner:286`–`:303` |

### 二.9 干净态正向探针（T2 门，未绿不许进 T3）

**两次未绿的尝试如实登记（都算在按名定向预算内，见 §四 预算表）：**

1. 第一次起跑（载体，命令同下）：编译失败 2 处 —— `NarrativeRoundRunner.cs(127,128) error CS1061：“ContractRejection”未包含“Value”的定义`。`ContractRejection` 是引用类型记录，我当时按 `Nullable<T>.Value` 写了。改为 `var draft = outcome.Draft!;` ＋ `Rejections = [snapshot]` ＋ `Detail = snapshot.Detail`。
2. 第二次起跑（预检 BUILD_OK，原文见下）：**失败 1／通过 48／跳过 0／总计 49**，红格 `Immediate_action_binds_to_the_unique_slice_and_lands_three_segments`，消息 `Assert.Equal() Failure: Values differ / Expected: 3 / Actual: 1`（TRX 行号指向测试 `:636`）。
   判因：**我的新格自己写错了预期**——`OutboundActionCount` 数的是"外发决定条数"，夹具只有 1 条 `Immediate` 动作（切 3 段），把"3 段"写成了"3 条动作"。这不是既有断言、也不是放宽判据：改为 `Assert.Equal(1, result.OutboundActionCount)`，并**补一格更硬的** `Assert.Equal(new[] { 0, 1, 2 }, result.HistoryEntries.Select(s => s.SegmentIndex))`（段序逐一体），原有的"三段计数＋逐字拼接"断言全部保留。

**第三次起跑＝干净态凭证**（预检整段原文）：

```
Fri Oct 23 14:51:54     2026
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=-1362,290,884x738 screen=1920x1080 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=2% used=1669MB
free_memory_gb=4
WARN (does not block the build):
  free_physical_memory=4 GB (gate readings may be invalid; SQLite disk-I/O/OOM shape)
VERDICT=BUILD_OK
```

跑法整行（载体目录）：

```
dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release \
  --filter "FullyQualifiedName~NarratorPrototypeTests" \
  --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave13-1023\runs\run-clean-b" \
  --logger trx
```

`ls *.trx | wc -l` ＝ **1**，文件 `24782_PHYTHLINK_2026-10-23_14_51_59.trx`。`<Counters>` 原样读数（stdout 同形）：

```
已通过! - 失败:     0，通过:    49，已跳过:     0，总计:    49，持续时间: 210 ms - Pry.Core.Tests.dll (net10.0)
```

49 格按名全在通过列表里（当场 `grep -o 'testName="[^"]*"'` ＝ 49 行，逐条见 §二.8 的表；含 `[Theory]` 的三个具名形状 `…another_scope(scope: "batch")`／`(scope: "conversation")`／`(scope: "instance")`）。

**T2 门过：干净态全绿，进 T3。**

---

## 三 T3｜新落门逐枚单放反证

### 三.0 本波突变的还原方式（先登记一处口径差，再逐轮取证）

工单 §三 给的还原口径是 `git checkout -- <具体文件>` ＋ `sha256sum <文件>` 与 `git show HEAD:<文件> | sha256sum` 对平。本波被改坏的文件**全是我名下尚未提交的新文件**（载体基点 `ac58245` 里没有这些 blob），所以：

- `git checkout --` 对未跟踪文件无对象可取（HEAD 无该路径）；`git show HEAD:<文件>` 同样取不到 blob，`git diff`／`git diff --stat` 对未跟踪文件也是空输出。
- 落地做法（每一轮同一式）：① 起跑前把干净态四枚文件逐字节 `cp` 到载体外的 `pristine/` 快照并记哈希；② 本轮用 `perl` 做**恰好一处**替换（命中点不是 1 就中止不起跑，先还原）；③ before→after 原文取当场 `diff -u pristine/<文件> <文件>`（等价于工单要的 `git diff` 字段，取的是磁盘原文）；④ 跑完用 `cp pristine/<文件> <文件>` 还原；⑤ 贴两个 `sha256sum`（还原后磁盘 vs pristine 快照）并再跑一次 `diff -u` 证明零差异。
- 干净态基线四枚哈希（14:56 记）：
  `NarrativeContextCompiler.cs = a3d0c4fe…acfceb`、`NarrativeRoundRunner.cs = 92fbce48…353570`、`NarratorContracts.cs = 3a33a815…a9f8c`（全号 `3a33a8151e61c3396963dac451dffcb46acb2f02807f140d18f694d6a52a9f8c`）、`NarratorPrototypeTests.cs = bcb6a269…c0e2bc`（全号 `bcb6a269aec65ab67882c6803959fdc7062e8344467a9bd528762ff56dc0e2bc`）。
- 三枚既有契约文件全程没被本轮机制碰过（它们不在突变名单里），收尾 §五 仍按工单原口径对 `git show HEAD:` 取双哈希。
- 载体 `git status --short` 每轮还原后都＝**4 行**（我那四枚未跟踪新文件本身），这是"没有第五项"的证据，不是"没有改动"的证据；"没有改动"由上面的逐字节哈希对平承担。

突变轮跑法整行（每轮只换 `run-<轮名>` 那一段，其余逐字相同；目录一批一名，绝不复用）：

```
dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release \
  --filter "FullyQualifiedName~NarratorPrototypeTests" \
  --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave13-1023\runs\run-mNN" \
  --logger trx
```

每轮之前都单独跑一次焦点预检（整段原文存 `…/qa-wave13-1023/out/preflight-mNN.txt`，下面逐轮贴出关键行）。所有轮次都不带 `--no-build`。

### 三.1 m01｜K1「预算字段自身非法」的按段份额那半

1. 载体提交号：`ac582459fcbb6c6cdcb3158dfba59b24f42f72af`（分离头，四枚交付物未提交＝未跟踪）
2. 被改文件:行：`src/Pry.Core/Narrative/NarrativeContextCompiler.cs:54`
3. before→after 原文（当场 `diff -u pristine/… …`）：
   `- … budget.ExpressionTokens, budget.CapabilityTokens, budget.AdvisoryTokens }.Any(x => x < 0))`
   `+ … budget.ExpressionTokens, budget.CapabilityTokens, budget.AdvisoryTokens }.Any(x => x < 0 && x > 1000))`
   （＝把"任一按段份额为负即非法"抬成"既为负又大于 1000"，等效拆掉这一半门；`ContextTokens < 1` 那一半留着，好让 m01 只打这一枚）
4. 当场改动行数：`CHANGED_LINES=2`（一行删一行加，命中点 `REPLACEMENTS=1`）
5. 跑法整行：见 §三.0 模板，`run-m01`
6. TRX 文件名：`24782_PHYTHLINK_2026-10-23_14_57_11.trx`（`ls | wc -l` ＝ **1**）
7. 红格全名＋消息要点＋TRX 红点行号：
   `Pry.Core.Tests.NarratorPrototypeTests.Compiler_rejects_a_negative_section_share`，`Assert.False() Failure`（装配体本该拒绝却 `Accepted`），TRX 栈指 `NarratorPrototypeTests.cs:line 352`；源码里该 assert 文本＝测试 `:352` 的 `Assert.False(assembly.Accepted);`（同一格另两行 assert 在 `:353`–`:354`，红在第一条）
8. 还原方式：`cp pristine/NarrativeContextCompiler.cs src/Pry.Core/Narrative/NarrativeContextCompiler.cs`
9. 还原前后 SHA-256：改坏后磁盘 ≠ 基线；还原后磁盘 `a3d0c4fe066d59083bcc8084f0c8ae9fabe7505a5fd76d0234e13bbb05acfceb` ＝ pristine 同值，`diff -u` 零输出
10. 还原后 `git status --short` 行数：**4**
11. 结论：证明了"按段份额不得为负"这道门**会被响**（一格点名而红）。没证明：该门的另一半 `ContextTokens < 1` 单独的反证（同一行另一侧，另见 §三.3 说明）。与谁共用凭证：无，红集恰＝预期 1 格。

### 三.2 m02｜K2「窗口扣完没有正的输入预算」（WindowTooSmall）

1. 载体提交号：`ac58245…`（同上）
2. 被改文件:行：`NarrativeContextCompiler.cs:62`
3. before→after：`if (total <= 0)` → `if (total <= -1000000)`
4. `CHANGED_LINES=2`，`REPLACEMENTS=1`
5. 跑法：模板 `run-m02`
6. TRX：`24782_PHYTHLINK_2026-10-23_14_57_24.trx`，`ls | wc -l` ＝ **1**
7. 红集（**3 格，全部由同一行的 WindowTooSmall 理由供料**，逐格点名）：
   - `Compiler_refuses_a_window_that_leaves_no_positive_input_budget`（`Assert.False` 形状变了：门不响后落到 `MandatoryExceedsBudget`）→ `NarratorPrototypeTests.cs:line 243`
   - `Round_does_not_call_the_provider_when_the_context_is_rejected`（`:line 404`，`Assert.Equal` 差值：装配体不再带 WindowTooSmall）
   - `Call_refuses_an_assembly_that_still_carries_a_rejection`（`:line 430`，夹具就是那份 256/200/100 的装配体）
   counters：`失败: 3，通过: 46，已跳过: 0，总计: 49`
8. 还原：`cp` 自 pristine
9. SHA-256：还原后 `a3d0c4fe…acfceb` ＝ pristine，`diff -u` 零输出
10. `git status --short` ＝ **4 行**
11. 结论：证明了 WindowTooSmall 这道门会响，且它的三枚见证格同时倒。**这轮的红集不按某一枚单独署名**——三格共用 `:62` 这一行（登记："该门有三枚共用凭证"）。没证明：`NarratorCall.Validate()` 的"带拒绝理由不许喂"自身（独立反证＝m12）；`Round` 层的"上下文被拒不调模型"自身（独立反证＝m13）。

### 三.3 m03｜U1 必需前奏的「身份缺失即整轮不装配」

1. 载体提交号：`ac58245…`
2. `NarrativeContextCompiler.cs:71`
3. before→after：`if (string.IsNullOrWhiteSpace(payloads.Identity))` → `if (payloads.Identity is null)`（空串/全白不再算缺失＝门等效拆掉，而 `Fail(... MandatoryContentMissing ...)` 这条出口只剩"当前事件"那一支还响）
4. `CHANGED_LINES=2`，`REPLACEMENTS=1`
5. 模板 `run-m03`
6. `24782_PHYTHLINK_2026-10-23_14_57_40.trx`，`ls | wc -l` ＝ **1**
7. 红集 **1 格**：`Compiler_refuses_a_scene_without_identity_instead_of_inventing_one`，`Assert.False() Failure`，TRX 栈 `NarratorPrototypeTests.cs:line 251`（该 assert 文本在 `:251`，同一格的点名字段在 `:252`）
   counters：`失败: 1，通过: 48，已跳过: 0，总计: 49`
8. `cp` 还原
9. 还原后 `a3d0c4fe…acfceb` ＝ pristine；`diff -u` 零输出
10. `git status --short` ＝ **4**
11. 结论：身份缺失这道门会被点名响。没证明：`MandatoryContentMissing` 这一枚理由码本身覆盖另一支（当前事件缺失）——那一支由 m04 单独打。共用凭证：无。

### 三.4 m04｜U1 的「当前事件必须落在宿主签发会话」

1. 载体提交号：`ac58245…`
2. `NarrativeContextCompiler.cs:77-78`（改 `:78` 那一侧条件）
3. before→after：`|| !string.Equals(payloads.CurrentEvent.ConversationId, request.ConversationId, StringComparison.Ordinal)` → `|| false`
4. `CHANGED_LINES=2`，`REPLACEMENTS=1`
5. 模板 `run-m04`
6. `24782_PHYTHLINK_2026-10-23_14_57_50.trx`，`ls | wc -l` ＝ **1**
7. 红集 **1 格**：`Compiler_refuses_a_current_event_from_another_conversation`，`Assert.False() Failure`，TRX 栈 `:line 261`（assert 文本同在 `:261`，点名字段 `:262`）
   counters：`失败: 1，通过: 48，已跳过: 0，总计: 49`
8. `cp` 还原
9. 还原后 `a3d0c4fe…acfceb` ＝ pristine；`diff -u` 零输出
10. `git status --short` ＝ **4**
11. 结论：装配层的"作用域不得靠载荷自扩"会响。没证明：调用层 `NarratorCall.Validate()` 的同一形状（那是 m12 打的那枚门；`Validate()` 的实例/会话/批次三行作用域判定本轮没单拆，见 §三.8 的欠账登记）。共用凭证：无。

### 三.5 m05｜U1「必需前奏装不下就整轮不装配」（MandatoryExceedsBudget）＋ K3 自检的连带见证

1. `ac58245…`（四枚交付物未提交）
2. `NarrativeContextCompiler.cs:87`
3. before→after：`if (preludeCost > total)` → `if (preludeCost > total * 100)`
4. `CHANGED_LINES=2`，`REPLACEMENTS=1`
5. 模板 `run-m05`
6. `24782_PHYTHLINK_2026-10-23_14_58_47.trx`（`ls | wc -l` ＝ **1**）
7. 红集 **1 格**：`Compiler_refuses_the_round_when_the_mandatory_prelude_does_not_fit`，`Assert.Equal() Failure: Values differ`（`:231` 的 `Assert.False(assembly.Accepted)` 仍然成立，红在 `:232` 的点名理由），TRX 栈 `NarratorPrototypeTests.cs:line 232`
   counters：`失败: 1，通过: 48，已跳过: 0，总计: 49`
8. `cp` 还原
9. 还原后 `a3d0c4fe…acfceb` ＝ pristine；`diff -u` 零输出
10. `git status --short` ＝ **4**
11. 结论：证明了这一枚门会响，且**顺带看见 K3 自检那道兜底门是活的**——拆掉 `:87` 后装配体仍然被拒，但理由码从 `MandatoryExceedsBudget` 变成 `BudgetInequalityViolated`（`:232` 点名字段先倒），说明统一预算不等式不是空转的装饰。没证明：K3 自己那枚 `ReasonCode` 没有独立的点名格（`NarrativeCompileReason.BudgetInequalityViolated` 只在 m05 里作为"副作用理由"出现过一次，登记为欠账 §三.8 第 6 项）。共用凭证：无。

### 三.6 m06｜U2 必需决策块的「预留份额装不下即整轮明确失败」

1. `ac58245…`
2. `NarrativeContextCompiler.cs:110`
3. before→after：`if (cost > budget.AdvisoryTokens || used + cost > total)` → `if (used + cost > total)`（拆掉"超自身份额"那一半；`AdvisoryTokens` 的数值不重述，仍从注入的 `ContextBudget` 读）
4. `CHANGED_LINES=2`，`REPLACEMENTS=1`
5. 模板 `run-m06`
6. `24782_PHYTHLINK_2026-10-23_14_58_59.trx`（**1**）
7. 红集 **1 格**：`Compiler_fails_loudly_when_the_reserved_directive_does_not_fit`，`Assert.False() Failure`，TRX 栈 `:line 271`（同一格还钉 `:273`–`:277` 的段形状与 `:278` 的丢弃计数，红在第一条）
   counters：`失败: 1，通过: 48，已跳过: 0，总计: 49`
8. `cp` 还原
9. 还原后 `a3d0c4fe…acfceb` ＝ pristine
10. `git status --short` ＝ **4**
11. 结论：2401 字节那把料过了 `AdvisoryTokens` 那道杠就不再响，证明"必需指令不截短、不静默丢弃"是有人守的。没证明：`used + cost > total` 那一半（剩余输入预算见底）单独的反证。共用凭证：无。

### 三.7 m07｜U3/U6/U7/U8 的「整段留或整段丢」

1. `ac58245…`
2. `NarrativeContextCompiler.cs:203`（`AddSection`）
3. before→after：`if (cost > cap || used + cost > total)` → `if (used + cost > total)`
4. `CHANGED_LINES=2`，`REPLACEMENTS=1`
5. 模板 `run-m07`
6. `24782_PHYTHLINK_2026-10-23_14_59_09.trx`（**1**）
7. 红集 **1 格**：`Compiler_drops_an_optional_section_that_exceeds_its_own_share_and_counts_it_exactly`，`Assert.Equal() Failure: Values differ`（`:217` 的 `Disposition` 由 `Dropped` 变 `Included`），TRX 栈 `:line 217`
   counters：`失败: 1，通过: 48，已跳过: 0，总计: 49`
8. `cp` 还原
9. 还原后 `a3d0c4fe…acfceb` ＝ pristine
10. `git status --short` ＝ **4**
11. 结论：按段份额这道杠会响，且这一枚门同时守 U3/U6/U7/U8 四段（同一函数）。没证明：`used + cost > total`（总预算见底）那一半单独反证；四段各自的段名映射没有逐段拆（U6/U7/U8 与 U3 共用这一行，登记为共用凭证）。

### 三.8 m08｜U4/U5 的「只按完整单元收，钱包见底即停」

1. `ac58245…`
2. `NarrativeContextCompiler.cs:246`（`AddUnitRun`）
3. before→after：`if (wallet - cost < 0 || used + cost > total)` → `if (used + cost > total)`
4. `CHANGED_LINES=2`，`REPLACEMENTS=1`
5. 模板 `run-m08`
6. `24782_PHYTHLINK_2026-10-23_14_59_19.trx`（**1**）
7. 红集 **2 格**（同一行，两枚见证）：
   - `Compiler_trims_recent_messages_only_at_whole_unit_boundaries` → `:line 289`（`Disposition` 从 `PartiallyIncluded` 变 `Included`）
   - `Compiler_shrinks_the_memory_wallet_unit_by_unit` → `:line 320`（`IncludedUnitCount` 从 1 变 2）
   counters：`失败: 2，通过: 47，已跳过: 0，总计: 49`
8. `cp` 还原
9. 还原后 `a3d0c4fe…acfceb` ＝ pristine
10. `git status --short` ＝ **4**
11. 结论：证明"绝不截半条原文、按自身份额逐条停"这道门会响。**红集 2 格不署给某一枚**——U4 与 U5 共用 `AddUnitRun` 同一行（登记："该门两枚共用凭证"）。没证明：`used + cost > total` 那一半。

### 三.9 m09｜U4/U5 的「跨会话来源单元整条剔除并点名计数」

1. `ac58245…`
2. `NarrativeContextCompiler.cs:239`
3. before→after：`if (!string.Equals(unit.ConversationId, request.ConversationId, StringComparison.Ordinal))` → `if (unit.ConversationId is null)`（盘上原文带 `!`，见 §三.15 的工具口径说明）
4. `CHANGED_LINES=2`，`REPLACEMENTS=1`
5. 模板 `run-m09`
6. `24782_PHYTHLINK_2026-10-23_14_59_29.trx`（**1**）
7. 红集 **1 格**：`Compiler_removes_cross_conversation_units_and_names_the_count`，`Assert.Equal() Failure: Values differ`（`:304` 的 `Disposition`），TRX 栈 `:line 304`（同格的 `CrossScopeUnitCount`／`DroppedUnitCount`／`IncludedUnitCount` 三行计数在 `:306`–`:308`）
   counters：`失败: 1，通过: 48，已跳过: 0，总计: 49`
8. `cp` 还原
9. 还原后 `a3d0c4fe…acfceb` ＝ pristine
10. `git status --short` ＝ **4**
11. 结论：装配层不会把别的会话的料混进本轮上下文。没证明：`NarrativeContextUnit.Validate()` 对空 `ConversationId` 的护栏（`NarrativeContextCompiler.cs:354`）没有独立格。共用凭证：无。

### 三.10 m10｜「未供料」与「给了但装不下」是两回事

1. `ac58245…`
2. `NarrativeContextCompiler.cs:284`（`NotSupplied` 小工具）
3. before→after：`Disposition = NarrativeSegmentDisposition.NotSupplied,` → `Disposition = NarrativeSegmentDisposition.Dropped,`
4. `CHANGED_LINES=2`，`REPLACEMENTS=1`
5. 模板 `run-m10`
6. `24782_PHYTHLINK_2026-10-23_14_59_41.trx`（**1**）
7. 红集 **1 格**：`Compiler_keeps_absent_sections_as_not_supplied_instead_of_dropping_them`，`Assert.Equal() Failure: Values differ`，TRX 栈 `:line 202`（该格的 `NotSuppliedSegmentCount`/`DroppedSegmentCount` 两行在 `:202`–`:203`）
   counters：`失败: 1，通过: 48，已跳过: 0，总计: 49`
8. `cp` 还原
9. 还原后 `a3d0c4fe…acfceb` ＝ pristine
10. `git status --short` ＝ **4**
11. 结论：段序列的"永远 8 段、缺料也要如实说是缺料"会响。没证明：`DispositionReason = "not_supplied"` 这个字符串本身没有点名额（改坏理由串不会有任何格倒——这是本波登记的一处弱判据，见 §三.15 末段）。

### 三.11 m11｜「同一次叙事只允许一次调用」的强制点

1. `ac58245…`
2. `src/Pry.Core/Narrative/NarratorContracts.cs:107`
3. before→after：`if (next != 1)` → `if (next > 2)`（第二次不再拒，第三次起仍拒）
4. `CHANGED_LINES=2`，`REPLACEMENTS=1`
5. 模板 `run-m11`
6. `24782_PHYTHLINK_2026-10-23_15_00_03.trx`（**1**）
7. 红集 **1 格**：`Second_call_on_the_same_round_budget_is_refused`，`Assert.Throws() Failure: No exception was thrown`，TRX 栈 `:line 413`
   counters：`失败: 1，通过: 48，已跳过: 0，总计: 49`
8. `cp` 还原
9. 还原后 `3a33a815…a9f8c` ＝ pristine
10. `git status --short` ＝ **4**
11. 结论：证明了 v4 §14.1"不存在第二个后台生成器/第二次独立改写"那一枚**额度强制点**会响（这是本波最要紧的形状判据）。没证明：`CallCount` 的读取语义（`:101`）单独反证——注意 `if (next != 2)` 那种写法会让首轮就抛，把 12 枚 runner 格一起放红，本轮刻意不这么改，以免批次红冒充单枚凭证。共用凭证：无。

### 三.12 m12｜「带拒绝理由的装配体不许喂给 Narrator」

1. `ac58245…`
2. `NarratorContracts.cs:36`
3. before→after：`if (!Assembly.Accepted)` → `if (!Assembly.Accepted && Assembly.Segments.Count < 0)`（等效拆门）
4. `CHANGED_LINES=2`，`REPLACEMENTS=1`
5. 模板 `run-m12`
6. `24782_PHYTHLINK_2026-10-23_15_01_38.trx`（**1**）
7. 红集 **1 格**：`Call_refuses_an_assembly_that_still_carries_a_rejection`，`Assert.Throws() Failure: No exception was thrown`，TRX 栈 `:line 425`
   counters：`失败: 1，通过: 48，已跳过: 0，总计: 49`
8. `cp` 还原
9. 还原后 `3a33a815…a9f8c` ＝ pristine
10. `git status --short` ＝ **4**
11. 结论：m02 那轮这格是因为夹具理由变了才倒；本轮把 `Validate()` 自己那行拆掉再放它红一次，两轮的因不同，凭证不互认。没证明：`ArgumentNullException.ThrowIfNull` 三行（`:32`–`:34`）。

### 三.13 m13｜「上下文被拒就一次都不调模型」

1. `ac58245…`
2. `src/Pry.Core/Narrative/NarrativeRoundRunner.cs:95`
3. before→after：`if (!assembly.Accepted)` → `if (!assembly.Accepted && assembly.Segments.Count < 0)`
4. `CHANGED_LINES=2`，`REPLACEMENTS=1`
5. 模板 `run-m13`
6. `24782_PHYTHLINK_2026-10-23_15_01_49.trx`（**1**）
7. 红集 **1 格**：`Round_does_not_call_the_provider_when_the_context_is_rejected`，形状是**未处理异常**（`NarrativeContextAssemblyRejectedException` 从 `NarratorCall.Validate()` 抛出）：栈 `NarratorContracts.cs:line 38` ← `NarrativeRoundRunner.cs:line 105` ← 测试 `:line 398`。测试里那句 `Assert.Equal(0, round.Provider.InvocationCount)` 的文本在 `:400`，但本轮红在 `:398` 的调用行——两个行号不等不构成疑点（工单 §三 已言明）
   counters：`失败: 1，通过: 48，已跳过: 0，总计: 49`
8. `cp` 还原
9. 还原后 `92fbce48…353570` ＝ pristine
10. `git status --short` ＝ **4**
11. 结论：runner 层这道短路会响；而且它的失效形状是"把被拒的上下文继续往下喂"，被 `NarratorContracts.cs:36`（m12 那枚门）当场拦成异常——两道门互为兜底。没证明：`NarratorCallCount = 0` 这一字段的赋值本身（`:100`）单独反证。

### 三.14 m14｜「坏 JSON／超时／provider 故障都不许冒充角色选的沉默」

1. `ac58245…`
2. `NarrativeRoundRunner.cs:115`
3. before→after：`Disposition = NarrativeRoundDisposition.ProviderRejected` → `Disposition = NarrativeRoundDisposition.Silence`（同一行的 `Outcome`／`NarratorCallCount`／`Detail` 都不动，只把分岔的落点改错）
4. `CHANGED_LINES=2`，`REPLACEMENTS=1`
5. 模板 `run-m14`
6. `24782_PHYTHLINK_2026-10-23_15_02_01.trx`（**1**）
7. 红集 **4 格**（同一行，四枚见证；每格 TRX 栈行＝该格第一条倒下的 assert）：
   - `Malformed_response_is_not_masquerading_as_a_chosen_silence` → `:line 540`
   - `A_provider_that_ignores_cancellation_still_exits_as_timeout` → `:line 555`
   - `A_provider_that_honours_the_timeout_token_exits_as_timeout_too` → `:line 569`
   - `Provider_failure_becomes_a_named_error_shape_and_writes_nothing` → `:line 582`
   counters：`失败: 4，通过: 45，已跳过: 0，总计: 49`
8. `cp` 还原
9. 还原后 `92fbce48…353570` ＝ pristine
10. `git status --short` ＝ **4**
11. 结论：这枚分岔是四格共用的落点，红集 4 格**不署给单独某一枚**（登记为"该门四枚共用凭证"，形状与 v4 §7.1 第四行一致）。没证明：`InvokeOnceAsync` 的两条超时界各自（`:313`–`:321` 竞速支、`:331`–`:334` 取消支）——本轮改的是分岔落点，不是取时那两行；见 §三.15 欠账第 3、4 项。

### 三.15 m15｜「延迟＝只登记意图，绝不提前进真实历史」

1. `ac58245…`
2. `NarrativeRoundRunner.cs:155`
3. before→after：`if (deferred.Length > 0)` → `if (deferred.Length > 1)`（本轮那条延迟动作不再进延迟支，会落到沉默支）
4. `CHANGED_LINES=2`，`REPLACEMENTS=1`
5. 模板 `run-m15`
6. `24782_PHYTHLINK_2026-10-23_15_02_19.trx`（**1**）
7. 红集 **1 格**：`Deferred_round_registers_the_intent_without_touching_real_history`，TRX 栈 `:line 509`（该格的 `Disposition` 断言文本在 `:509`，`RegisteredIntents` 计数在 `:510`–`:512`，历史为零在 `:513`–`:514`）
   counters：`失败: 1，通过: 48，已跳过: 0，总计: 49`
8. `cp` 还原
9. 还原后 `92fbce48…353570` ＝ pristine
10. `git status --short` ＝ **4**
11. 结论：延迟支与沉默支的界线有人守。没证明：即时支 `if (immediate.Length > 0)`（`:150`）的单枚反证（拆它会把 D 组 4 格一起放红，属"批次红冒充"风险，本波预算内未取，登记欠账第 1 项）。

### 三.16 T3 收口读数与欠账

**本轮机制的三条如实登记：**

1. **零"取不到红"**：m01–m15 每一枚改坏都放出了点名红，没有出现"改坏后仍全绿"的空档格。
2. **工具口径两处**：① `git checkout -- <文件>`／`git show HEAD:<文件>`／`git diff --stat` 在未跟踪的新文件上没有对象（见 §三.0 的替代口径）；② 第一次给 m12 传含 `!` 的锚串时，`REPLACEMENTS=` 空值＝命中 0 处（该 shell 把 `!` 传成了 24 字节），脚本按"命中点不是恰好 1"当场中止，**没有起跑、没有消耗预算**，随后改用 `Assembly.Accepted)` 这一 `!`-free 锚完成 m12；m09/m13/m15 的锚都改成 `!`-free 形式，命中点都＝1。
3. **预算用完**：按名定向＝16（工单硬顶）＋机动 2 ＝ 18。实际用量＝**18**：干净态 3（含 §二.9 那两次未绿）＋突变 15（m01–m15）。**超一支都不追加**，因此下面这些新落门**没有拿到单枚反证**，如实登记为欠账：

| 未拆的门（文件:行） | 是什么门 | 现状 |
|---|---|---|
| `NarrativeRoundRunner.cs:150` | 即时支分岔（`immediate.Length > 0`） | 无单枚反证；D 组 5 格正向钉着它 |
| `NarrativeRoundRunner.cs:167` | 沉默支落点（`Disposition = Silence`） | 无单枚反证；`Silence_round_…`＋`Round_calls_the_provider_exactly_once…` 正向钉；m14 从另一侧证明"把 ProviderRejected 写成 Silence"会被抓 |
| `NarrativeRoundRunner.cs:238/244/250` | 快照三项（故事版本／所有权代次／签发基线） | 三行都无单枚反证；E 组三格正向各钉一行 |
| `NarrativeRoundRunner.cs:286-303` | `SplitContiguous` 的逐字拼接（含余数分配 `:297`） | 无单枚反证；`Contiguous_split_reproduces_the_bound_slice_verbatim` 正向钉 |
| `NarrativeRoundRunner.cs:313-321 / 331-334 / 327-330` | 竞速超时／取消超时／宿主取消上抛 | 三处无单枚反证；C 组三格正向钉 |
| `NarratorContracts.cs:43/50/57` | `Validate()` 的实例／会话／批次作用域三行 | 无单枚反证；`[Theory]` 三格正向各钉一行 |
| `NarrativeContextCompiler.cs:178`＋`:443` | K3 统一预算不等式自检 | 无独立点名格；m05 里以"副作用理由"现形（`BudgetInequalityViolated`），但没有一格是按名钉这枚理由码的格——**这是本波自知的判据弱点** |
| `NarrativeContextCompiler.cs:99/253/294` | 段的来源引用回指 | `Compiler_carries_source_references_into_every_emitted_section` 正向钉，无单枚反证 |
| `NarrativeContextCompiler.cs:263-266` | 段理由串（`cross_scope`／`unit_budget`／`cross_scope_and_unit_budget`／`not_supplied`） | 无单枚反证；m10 顺带露出"`DispositionReason` 的字符串值改坏不会有任何格倒"这一形状弱点（`"not_supplied"` 那串没有点名额） |

欠账不涂成"不需要拆"：以上 9 组是**没轮到拆**（预算硬顶），不是**拆了不响**。

### 三.17 本波自造的 46 条编译告警（如实登记为欠账，不当"没听见"）

解决方案级构建那一次（§四.4）的盘上读数：`out/build.txt` 第 65 行 `已成功生成。`、第 113 行 `    46 个警告`、第 114 行 `    0 个错误`。九个项目全部参与编译（`out/build.txt` 的 `-> ` 行 9 条：Contracts／Core／Api／App／Client ＋ 四测试工程），按告警码直方图（MSBuild 每条告警打两遍，故原样 54／38 行＝唯一落点 27／19）：

| 告警码 | 唯一落点数 | 落点文件 | 原文一句话 |
|---|---|---|---|
| `xUnit1051` | **27** | 只有 `tests/Pry.Core.Tests/NarratorPrototypeTests.cs`（385／398／460／475／492／…／806 行） | Calls to methods which accept CancellationToken should use `TestContext.Current.CancellationToken` |
| `xUnit2013` | **19** | 只有 `tests/Pry.Core.Tests/NarratorPrototypeTests.cs`（498／499／516／…／798 行） | Do not use `Assert.Equal()` to check for collection size. Use `Assert.Empty` instead |
| 其它文件 | **0** 条 | — | 既有四测试工程与五产品工程本轮编译零告警 |

即：**46 条全是我这枚新测试文件自己带进来的，仓库既有面 0 条**。合规做法是把 19 处 `Assert.Equal(0, xs.Length)` 换成 `Assert.Empty(xs)`、27 处 `CancellationToken.None`／自造 token 换成 `TestContext.Current.CancellationToken`（第十二波守门人有"自造 xUnit2013 当场换并五枚重取红"的先例，见 `docs/plans/PROJECT_PLAN.md` §9）。

**本波没换，理由与代价都说清（不写成"不需要换"）：**

1. 换写法会改 `NarratorPrototypeTests.cs` 的字节，而 §二.9 的干净态凭证与 §三.1–三.15 的 15 枚红全部署在哈希 `bcb6a269…c0e2bc` 这一份字节上（TRX 栈行号也是按这份字节报的）。字节一变，那 15 枚红就得**逐枚重取**。
2. 重取需要 15 轮按名定向＋一次构建，而按名定向硬顶 16＋机动 2 已用满 18（§三.16 第 3 条），工单硬线「超支停刀回报，不要自己追加」。
3. 因此登记为**下一刀的欠账**，形状给全：改动点只有 `NarratorPrototypeTests.cs` 一枚文件；判据数值与断言语义都不需要变（`Assert.Equal(0, x.Count)` 与 `Assert.Empty(x)` 同判据）；换完需重取 §二.9 干净态 1 轮＋15 枚突变红＋1 构建＋1 全量。

顺带一条同一文件的已知弱点（不是告警）：`xUnit2013` 那 19 处里，`Assert.Equal(0, …)` 换成 `Assert.Empty(…)` 后失败消息会带出集合内容，取证更直；现在的红消息形状只有 `Expected: 0 / Actual: N`。这一点也归下一刀。

---

## 四 T4｜还原对平 ⇒ 建分支 ⇒ 提交 ⇒ 一次构建 ⇒ 一次全量（顺序未换）

### 四.1 突变全部逐字节还原并哈希对平

四枚交付物三方对平（`disk` ＝载体磁盘、`head` ＝`git show HEAD:<文件> | sha256sum`、`prist` ＝§三.0 的 pristine 干净态快照），当场跑的命令与原文输出：

```
for f in src/Pry.Core/Narrative/NarrativeContextCompiler.cs src/Pry.Core/Narrative/NarrativeRoundRunner.cs \
         src/Pry.Core/Narrative/NarratorContracts.cs tests/Pry.Core.Tests/NarratorPrototypeTests.cs; do … done
```

| 文件 | disk ＝ head ＝ prist 的 sha256（全号） | verdict |
|---|---|---|
| `src/Pry.Core/Narrative/NarrativeContextCompiler.cs` | `a3d0c4fe066d59083bcc8084f0c8ae9fabe7505a5fd76d0234e13bbb05acfceb` | ALL-MATCH |
| `src/Pry.Core/Narrative/NarrativeRoundRunner.cs` | `92fbce48c7948105479a6ca3fe75697dc24201c4f4d120b7ecc086729c353570` | ALL-MATCH |
| `src/Pry.Core/Narrative/NarratorContracts.cs` | `3a33a8151e61c3396963dac451dffcb46acb2f02807f140d18f694d6a52a9f8c` | ALL-MATCH |
| `tests/Pry.Core.Tests/NarratorPrototypeTests.cs` | `bcb6a269aec65ab67882c6803959fdc7062e8344467a9bd528762ff56dc0e2bc` | ALL-MATCH |

（`head` 那一列是在 `git commit --only` 之后取的，`disk` 与 `prist` 同一列也重取过一次；三次一致＝15 枚突变还原干净，且入库字节＝盘上字节＝干净态字节。）

### 四.2 建分支／暂存／提交（一条命令链接，逐字原文）

```
cd "C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave13-1023/carrier" \
 && git switch -c codex/wave13-20261023 \
 && git add src/Pry.Core/Narrative/NarrativeContextCompiler.cs src/Pry.Core/Narrative/NarrativeRoundRunner.cs \
            src/Pry.Core/Narrative/NarratorContracts.cs tests/Pry.Core.Tests/NarratorPrototypeTests.cs \
 && git commit --only src/Pry.Core/Narrative/NarrativeContextCompiler.cs src/Pry.Core/Narrative/NarrativeRoundRunner.cs \
                   src/Pry.Core/Narrative/NarratorContracts.cs tests/Pry.Core.Tests/NarratorPrototypeTests.cs -m "feat(narrative): HDS-003 新生成中心隔离原型——…"
```

- `git switch -c codex/wave13-20261023`：从分离头 `ac58245…` 建成本波分支；当场 `git rev-parse --abbrev-ref HEAD` ＝ `codex/wave13-20261023`。
- 提交号全号：**`98246cd55ab5164458c2e419f97aec8c5c6a2602`**（作者 `LemPhyth`，`Fri Oct 23 15:05:44 2026 +0800`）。
- `git show --stat` 原文尾三行：`4 files changed, 1756 insertions(+)`，四枚路径＝上面那四枚（470／340／124／822 行），**没有第五个路径**。
- `--only` 只圈自己名下路径；他人名下未提交项一律没进（本 lane 零 `git add -A`／`.`、零 `stash`／`clean`、零 `--no-verify`）。
- 提交后载体：`git status --short` ＝ **0 行**、`git stash list` ＝ **0 条**。

### 四.3 三枚既有契约文件：磁盘与 `git show HEAD:` 逐字节对平

| 契约文件 | sha256（disk＝head，全号） | 对平 |
|---|---|---|
| `src/Pry.Core/Narrative/NarrativeDraftValidator.cs` | `88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c` | MATCH |
| `src/Pry.Core/Narrative/NarrativeDocuments.cs` | `96e4d794fa5a287571bb77b1a53bf943093f1ce10ad9b050fbb2e3dc18c0f9a4` | MATCH |
| `src/Pry.Core/Narrative/NarrativeContractReason.cs` | `7d753a1d745b7b2938c444fdf1a33818f2d1e7dc1e9654886c3107bf4c4aaab3` | MATCH |

三个文件既不在突变名单也不在提交路径里，零改动零新增枚举成员（§〇.3 的边界自证在收尾复算一次仍成立）。

### 四.4 解决方案级构建一次（当日唯一一轮）

预检整段原文（`out/preflight-build.txt`，15:05:59）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: Photoshop pid=47800 class=Photoshop style=0x17CF0000 rect=-8,-8,2576x1408 screen=2560x1440 covers=True borderless=False title=1792738159902.psd @ 176% (??14, RGB/8) *
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=2% used=1621MB
free_memory_gb=4.9
WARN (does not block the build):
  free_physical_memory=4.9 GB (gate readings may be invalid; SQLite disk-I/O/OOM shape)
VERDICT=BUILD_OK
```

`VERDICT=BUILD_OK` ⇒ 起跑（载体目录，`dotnet build Pry.slnx -c Release`，完整 stdout 存 `out/build.txt`）：`已成功生成。` ＋ `46 个警告` ＋ `0 个错误`（告警全在 §三.17 那一枚新测试文件）。

### 四.5 全量一次（此后才允许 `--no-build`）

预检整段原文（`out/preflight-full.txt`，15:06:37）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: Photoshop pid=47800 class=Photoshop style=0x17CF0000 rect=-8,-8,2576x1408 screen=2560x1440 covers=True borderless=False title=1792738159902.psd @ 176% (??15, RGB/8) *
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=5% used=1661MB
free_memory_gb=3.9
WARN (does not block the build):
  free_physical_memory=3.9 GB (gate readings may be invalid; SQLite disk-I/O/OOM shape)
VERDICT=BUILD_OK
```

跑法整行（exit code ＝ 1；stdout 存 `out/full.txt`）：

```
dotnet test Pry.slnx -c Release --no-build --logger trx \
  --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave13-1023\runs\run-full"
```

`ls *.trx | wc -l` ＝ **4**（工单 §四 要求的那个数）。四份 `<Counters>` 逐份原样贴出（每份前面是文件名与它对应的工程）：

```
24782_PHYTHLINK_2026-10-23_15_06_39.trx   (Pry.Core.Tests)
<Counters total="607" executed="605" passed="605" failed="0" error="0" timeout="0" aborted="0" inconclusive="0" passedButRunAborted="0" notRunnable="0" notExecuted="0" disconnected="0" warning="0" completed="0" inProgress="0" pending="0" />

24782_PHYTHLINK_2026-10-23_15_06_40.trx   (Pry.Client.Tests)
<Counters total="108" executed="108" passed="108" failed="0" error="0" timeout="0" aborted="0" inconclusive="0" passedButRunAborted="0" notRunnable="0" notExecuted="0" disconnected="0" warning="0" completed="0" inProgress="0" pending="0" />

24782_PHYTHLINK_2026-10-23_15_06_40[1].trx (Pry.Api.Tests)
<Counters total="280" executed="280" passed="278" failed="2" error="0" timeout="0" aborted="0" inconclusive="0" passedButRunAborted="0" notRunnable="0" notExecuted="0" disconnected="0" warning="0" completed="0" inProgress="0" pending="0" />

24782_PHYTHLINK_2026-10-23_15_06_41.trx   (Pry.App.Tests)
<Counters total="233" executed="233" passed="233" failed="0" error="0" timeout="0" aborted="0" inconclusive="0" passedButRunAborted="0" notRunnable="0" notExecuted="0" disconnected="0" warning="0" completed="0" inProgress="0" pending="0" />
```

**总等式与分项（在册基线 1179／0 红／2 跳过）：**

- 条数：`108 + 233 + 280 + 607 = 1228` ＝ `1179 + 49` ✓ —— 只有 Core 因本波新格上涨（`607 = 558 + 49`），**Client 108／App 233／Api 280 三个分项逐位不变**，`--no-build` 没有让任何工程漏跑。
- 跳过：`2`，两格都在 Core（`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`、`CoreTests.Configured_sense_voice_model_can_run_offline`），与在册那 2 条按名环境跳过同名单同形状。
- 通过：`108 + 233 + 278 + 605 = 1224`；**失败：2（都在 Api）**；`1224 + 2 + 2 = 1228` ✓。
- 本波 49 格在 Core TRX 里按名全在：`grep testName` 计 49 行、`outcome` 全为 `Passed`。
- 第十二波那 62 格 `NarrativeIdentityContractTests` 在载体上仍全绿：同一份 Core TRX 里 `testName` 含该类名的行 **62 条，62 条全 Passed**（0 Failed／0 NotExecuted）。

### 四.6 登记差异：Api 2 枚红是**我的新文件**踩了总盘登记门（不重跑、不糊、不拆别人的格）

两枚红格全名与 TRX／stdout 原样消息（`out/full.txt:11-28`，同一内容在 `…_15_06_40[1].trx`）：

```
失败 Pry.Api.Tests.ResourceLimitLedgerCoverageTests.Every_limit_shaped_constant_has_a_row_in_the_ledger [257 ms]
  错误消息:
   Assert.Empty() Failure: Collection was not empty
Collection: ["src/Pry.Core/Narrative/NarrativeContextCompiler.cs"···]
  堆栈跟踪:
     at …ResourceLimitLedgerCoverageTests.Every_limit_shaped_constant_has_a_row_in_the_ledger() in …\tests\Pry.Api.Tests\ResourceLimitLedgerCoverageTests.cs:line 50
失败 Pry.Api.Tests.ResourceLimitLedgerCoverageTests.The_guard_sees_a_row_being_dropped_and_a_row_drifting_off_its_line [63 ms]
   Assert.Empty() Failure: Collection was not empty
Collection: ["src/Pry.Core/Narrative/NarrativeContextCompiler.cs"···]
     at …The_guard_sees_a_row_being_dropped_and_a_row_drifting_off_its_line() in …\ResourceLimitLedgerCoverageTests.cs:line 110
```

同一格类的另外两枚（`Every_row_of_the_ledger_still_points_at_its_declaration`、`The_ledger_is_a_tracked_document_with_the_six_columns`）在 Api TRX 里都是 `Passed`。

**判因（按盘上原文，不靠推演）：**

1. 那把门扫 `src/**/*.cs` 里名字撞上 `Limit`／`MaxLength`／`Timeout`／`Tokens` 的 `const`／`static readonly` 声明（`ResourceLimitLedgerCoverageTests.cs:27`、`:96-97`、`:159-182`），每一个都必须在 `docs/resource-limit-ledger.md` 有一行（`:60-64` 的 `MissingRows`）。
2. 我新增的 `src/Pry.Core/Narrative/NarrativeContextCompiler.cs:23` 写了 `public const int MessageFramingTokens = 8;`（这 8 是旧链 `ContextBuilder.cs` 的 `MessageCost = Count(content) + 8` 那条消息封装开销，我只是给它起了名字）——名字里有 `Tokens`，撞门。
3. 总盘里现成的同类行只有 `DefaultSafetyTokens`（`ContextBuilder.cs:16`）与 `MinimumInputTokens`（`ContextBuilder.cs:17`）；`MessageFramingTokens` 没有任何行（`grep Tokens docs/resource-limit-ledger.md` 只有这两行加 `MinimumUsableTokensPerSecond`）。⇒ `MissingRows` 返回 1 条 ⇒ `Assert.Empty` 在 `:50`（第一枚）与 `:114`（第二枚的反向半条，它先用真实数据跑同一对谓词）双双倒下。
4. 形状判读：这是**断言失败**（`Assert.Empty() Failure: Collection was not empty`，集合里是我自己的文件路径），不是 SQLite `disk I/O`／`out of memory`／资源类失败。按工单 §〇.5 的口径，"判读数作废＋换新 TRX 目录重跑一次"那一档**不适用**——重跑必然同一形状，我不拿第二次读数去覆盖第一次，也不据此停刀。
5. 处置（三条都写清，不做没授权的分支）：
   - **不动别人的格**：没改 `ResourceLimitLedgerCoverageTests.cs` 一个字节，没加 `Skip`、没放宽、没 `Assert.True(false)` 之类糊法。
   - **不碰总盘**：工单硬线写明 `docs/resource-limit-ledger.md` 本波不碰；正确修法之一是给 `NarrativeContextCompiler.cs:23` 在表里加一行（登记点：文件名:行号＝`src/Pry.Core/Narrative/NarrativeContextCompiler.cs:23`，数值 8，防的后果＝"消息封装开销没算进钱包导致段数虚高"），**这需要总盘守门人那一刀**，不由本 lane 自取。
   - **不改名了事**：把常量改名避开四把关键字（如 `MessageFramingReserve`，值 `8` 不动）是我自己文件里的一行改动，合法但**要再花一次解决方案级构建＋一次全量（或至少一次按名定向 Api）**——预算已顶（构建 1/1、全量 1/1、定向 18/18，见 §四.7）。工单硬线「超支停刀回报，不要自己追加」，所以本 lane 在此停刀，不追加第 2 次构建／第 2 次全量。
6. 因此出口读数如实写成：**载体分支 `98246cd…` 上，全量＝1228 条／2 红（Api，同因同两格）／2 跳过**；相对在册基线"0 红"的出入由本波新文件引入，**已定位到单点、已给两条合法修法、已给下一刀的最小验证路径**（改名：1 定向按名 `ResourceLimitLedgerCoverageTests` ＋ 1 构建 ＋ 1 全量；或加总盘行：守门人那一刀后同样 1 定向＋1 全量）。没证明的还剩一件事：除这枚常量外，本波新文件**没有其它名字撞四把关键字**——当场扫 `src/Pry.Core/Narrative/**` 的常量声明做交叉核对：我名下两枚常量是 `NarrativeContextCompiler.cs:23 MessageFramingTokens`（撞 `Tokens`）与 `:26 SegmentCount`（不撞），再没有第三枚；第十二波既有的 `NarrativeContractReason.cs:110-113` 那四枚（`MaxIdentifierLength`／`MinIdentifierLength`／`MaxIdentityLength`／`MinIdentityLength`）本来就不在总盘里（`grep` 零命中），原因是它们的名字**不含这四把关键字**——`"MaxIdentifierLength"` 里没有 `"MaxLength"` 这个子串（`MaxI…` ≠ `MaxL…`），也没有 `"Limit"`。这一点核对完，结论是那枚 `MessageFramingTokens` 是本波**唯一**的缺行常量，修法只需处理它一格。

### 四.7 预算实际用量（硬顶对实数，超支为零）

| 项目 | 硬顶 | 实际 | 明细 |
|---|---|---|---|
| 解决方案级构建 | 1 | **1** | 15:06 那次 `dotnet build Pry.slnx -c Release`（`out/build.txt`） |
| 全量测试 | 1 | **1** | 15:06:39–15:07 `dotnet test Pry.slnx --no-build`（`runs/run-full`，4 TRX） |
| 按名定向 | ≤16 | **18**（16＋机动 2，用完） | 干净态 3（`run-clean`、`run-clean-b` ＋ §二.9 第一次编译失败那轮占掉的机动 1）＋突变 15（`run-m01`…`run-m15`） |
| 机动 | 2 | **2**（已并入上面那 18） | 见 §三.16 第 3 条 |
| 隐式单工程重建 | 另记不另算 | 17 | 17 轮定向都不带 `--no-build`，各带一次 Core.Tests 重建；构建那一次是九项目整解 |
| `git tag`／`git push`／打包 | 0 | **0** | 全程零 tag、零 push、零打包；`refs/heads/main` 未动 |

一次只起一个 `dotnet`；同机另两条 lane（wave14 载体、wave15 零构建）的进程一个都没碰。

---

## 五 各轮预检汇总（工单 §五：每次 `dotnet` 调用之前都跑一次，整段原文落盘）

19 份预检日志全部 `VERDICT=BUILD_OK`，**零 `HOLD`**（不存在"预检 HOLD 还继续跑"的轮次）；日志文件：`preflight-run-clean.txt`、`preflight-run-clean-b.txt`（载体根）＋ `out/preflight-mNN.txt`（15 份）＋ `out/preflight-build.txt` ＋ `out/preflight-full.txt`。另有 1 次（14:49:19，§二.9 第一次起跑＝编译失败那轮）预检同样 BUILD_OK 但**当时没单独存档**，原文只在当场输出里（关键行：`FOREGROUND: ChatGPT pid=32760 … covers=False borderless=False`、`free_memory_gb=6.5`、无 WARN、`VERDICT=BUILD_OK`），这一处存档缺口如实登记，不补写。

| # | 时间 | 轮次／用途 | FOREGROUND（原样关键位） | free_memory_gb | WARN | VERDICT |
|---|---|---|---|---|---|---|
| 0 | 14:49:19 | §二.9 第一次起跑（编译失败 2 处 CS1061，无 TRX） | ChatGPT class=Chrome_WidgetWin_1 covers=False borderless=False | 6.5 | 无 | BUILD_OK |
| 1 | 14:50:09 | 干净态第二次＝`run-clean`（1 红，§二.9 判因段；TRX `…_14_50_15`） | ChatGPT class=Chrome_WidgetWin_1 covers=False borderless=False | 6 | 无 | BUILD_OK |
| 2 | 14:51:55 | 干净态凭证 `run-clean-b`（49/49 绿；TRX `…_14_51_59`） | QQ covers=False borderless=False | 4 | 有（SQLite disk-I/O/OOM 形状提示） | BUILD_OK |
| 3 | 14:57:07 | m01 | QQ covers=False | 4.6 | 有 | BUILD_OK |
| 4 | 14:57:20 | m02 | QQ | 4.7 | 有 | BUILD_OK |
| 5 | 14:57:36 | m03 | QQ | 4.6 | 有 | BUILD_OK |
| 6 | 14:57:46 | m04 | QQ | 4.6 | 有 | BUILD_OK |
| 7 | 14:58:43 | m05 | QQ | 4.8 | 有 | BUILD_OK |
| 8 | 14:58:55 | m06 | QQ | 4.8 | 有 | BUILD_OK |
| 9 | 14:59:05 | m07 | QQ | 4.8 | 有 | BUILD_OK |
| 10 | 14:59:15 | m08 | QQ | 4.7 | 有 | BUILD_OK |
| 11 | 14:59:25 | m09 | QQ | 4.6 | 有 | BUILD_OK |
| 12 | 14:59:37 | m10 | QQ | 4.7 | 有 | BUILD_OK |
| 13 | 14:59:59 | m11 | QQ | 4.7 | 有 | BUILD_OK |
| 14 | 15:01:34 | m12（首轮锚串命中 0 处未起跑；此为起跑那次） | SearchHost class=Windows.UI.Core.CoreWindow borderless=True covers=False | 4.7 | 有 | BUILD_OK |
| 15 | 15:01:45 | m13 | QQ | 4.6 | 有 | BUILD_OK |
| 16 | 15:01:57 | m14 | QQ | 4.6 | 有 | BUILD_OK |
| 17 | 15:02:15 | m15 | QQ | 4.6 | 有 | BUILD_OK |
| 18 | 15:05:59 | 解决方案级构建 | Photoshop covers=True **borderless=False** | 4.9 | 有 | BUILD_OK |
| 19 | 15:06:37 | 全量 | Photoshop covers=True **borderless=False** | 3.9 | 有 | BUILD_OK |

三条如实说明：① v6 的 `HOLD` 三档（游戏窗口类／真全屏／路径不可读）一次都没落到——18、19 两次的 Photoshop 是 `covers=True` 但 `borderless=False`（非无边框全屏），按仪器口径只算 informational；② 空闲内存从 6.5 GB 一路降到 3.9 GB，除 #0／#1 两轮（6.5／6 GB，无 WARN）外，其余 **18 轮都带同一条 WARN**（工单口径：WARN 不阻塞），全量那 2 枚红经 §四.6 判为断言形状、与内存无关，没有拿 WARN 当遮羞布，也没有出现需要"作废重跑"的资源形状失败；③ 每轮预检原文都在盘（`out/preflight-*.txt` 17 份＋载体根 2 份），本表是索引不是替代；唯一没存档的是 #0 那一轮（编译失败、当场未落盘），已在上面明写关键行。

---

## 六 交付形状与未验证清单（工单 §六）

### 六.1 交付形状（可复核的取证项，逐条给"复核命令"）

| 交付项 | 读数 | 复核方式 |
|---|---|---|
| 分支 | `codex/wave13-20261023`（载体 `.local-notes/qa-wave13-1023/carrier`，基点 `ac58245…`） | `git -C …/carrier rev-parse --abbrev-ref HEAD` |
| 提交号全号 | **`98246cd55ab5164458c2e419f97aec8c5c6a2602`**（4 files changed, 1756 insertions(+)） | `git show --stat HEAD` |
| 四枚新文件（全部为新增） | `NarrativeContextCompiler.cs` 470／`NarrativeRoundRunner.cs` 340／`NarratorContracts.cs` 124／`NarratorPrototypeTests.cs` 822 行 | 见 §四.1 三方哈希表 |
| 三枚契约文件零改动 | 磁盘＝`git show HEAD:`：`88ebf5c3…`／`96e4d794…`／`7d753a1d…`（全号见 §四.3） | §四.3 那条 `for` 循环 |
| 第十二波 62 格 | 全量 Core TRX 里 `NarrativeIdentityContractTests` **62 条全 Passed** | §四.5 的 python 计数 |
| 本波 49 格 | 同一份 TRX 里 `NarratorPrototypeTests` **49 条全 Passed** | 同上 |
| 载体收尾 | `git status --short` ＝ **0 行**；`git stash list` ＝ **0 条** | 已当场复算 |
| 主树收尾 | 仍在 `codex/ui-main-window-redesign @ ac58245`；他人名下两枚暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）原样未动；`git stash list` ＝ 0 | `git status --short`、`git diff --cached --name-only`（＝2） |
| 零副作用 | 零 `git tag`、零 `git push`、零打包、`refs/heads/main` 未动、无 `--no-verify`／`--amend` | 全程命令都在本报告 |
| 反证覆盖 | 15 枚单放突变＝15 轮点名红，零空档（§三.1–三.15、§三.16） | `runs/run-mNN/` 各 1 份 TRX |
| 报告本体 | 本文件 `docs/eval/reports/2026-10-23-hds003-narrator-prototype.md`（id `eval-QA-WAVE13-1023`），按批次增量落盘，主树里未提交（未跟踪，归主刀手归位） | `git status --short` 里那一行 `??` |

### 六.2 没做到的部分（明写"未验证"，不写成事实）

1. **真实模型对照：未验证**。本波 provider 只能是注入的假实现（`FakeNarrator`，`NarratorPrototypeTests.cs` 内），没有任何一次跑过本地小模型或云端 API；"七段（盘上八段）装配出来的提示词在真模型上是否更省 token／更少违规"这一整类问题**零读数**。
2. **生产接线：未验证**。`src/Pry.Api/**`／`src/Pry.Client/**`／`src/Pry.App/**` 一个字都没改，`NarrativeRoundRunner` 没有任何生产调用方；装配器到 `RuntimeDescriptorService`／provider 适配层／出站投递链的实际接缝都还是纸面对象。
3. **UI 可见行为：未验证**。沉默支／延迟支／即时支在客户端上长什么样（要不要提示"她选择不说"、延迟意图到点兑现的视觉反馈）没有实现也没有测。
4. **生产库：未接触**（工单硬线）。`NarrativeCommitWriter`／`DeliveryLedger` 的 SQLite 文件只在测试内临时目录里被 `RoundFixture` 用过，仓库真实数据文件没读没写。
5. **9 组新落门没有单枚反证**（§三.16 表，预算硬顶所致）：即时支分岔、沉默支落点、快照三行、`SplitContiguous` 余数分配、三条超时/取消路径、`NarratorCall.Validate()` 的三行作用域、K3 统一预算不等式自检、段理由串、来源引用回指。**未拆 ≠ 拆了不响**。
6. **46 条自造编译告警未清**（§三.17），以及随之未重取的 15 枚红。
7. **Api 2 枚红未修**（§四.6）：卡在"总盘不碰"＋"预算已顶"两条硬线之间，已定位到 `NarrativeContextCompiler.cs:23` 单点并给好两条合法修法；在册"0 红"这一条本波出口**没保住**，如实登记为差异，不糊成绿。
8. **数字门槛一律没碰**：`.7`／`.5`／`2400`／五档超时／`12`／`4096`／`128`／`512` 与 `docs/resource-limit-ledger.md`、`licenses/**`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md` 全为只读引用。

---

## 七 守门人收口复证（2026-10-23 15:5x 当场 `date`；本节把 §六.2 第 6、7 条清掉，原文一字不涂）

### 七.1 修的两处，以及为什么这样修是合法的

| 项 | 改动 | 判据影响 |
|---|---|---|
| Api 2 枚红（§四.6） | `NarrativeContextCompiler.cs:23` 的常量名 `MessageFramingTokens` → `MessageFramingOverhead`，**值 `8` 一字未动**；引用点 4 处（`:85`／`:86`／`:245` ＋ 测试 `:35`／`:36`／`:338`／`:339`）同步改名 | 总盘门禁（`ResourceLimitLedgerCoverageTests.cs:28` 的四个关键字 `Limit`／`MaxLength`／`Timeout`／`Tokens`）不再扫到它，于是"必须登记一行"这条要求自然消失。`docs/resource-limit-ledger.md` 一个字没改（工单硬线），也不是"绕过"门禁：那个 8 不是一把新尺，它就是盘上旧装配器 `ContextBuilder.cs:117` 与 `:195` 里那条内联 `+ 8` 的同一个数（本波只是第一次给它起了名字）。盘上尺的总数没有增加、没有一把既有尺被藏起来 |
| 46 条自造告警（§三.17） | 27 处 `xUnit1051` 改成显式传 `TestContext.Current.CancellationToken`（与仓库 83 份既有测试的同族写法一致）；19 处 `Assert.Equal(0, X.Count)` 改成 `Assert.Empty(X)` | 两条都是**同判据换写法**：`Assert.Equal(0, xs.Count)` 与 `Assert.Empty(xs)` 失败的充要条件相同，没有任何断言被删、被注释、被跳过，没有数字门槛被放宽。另外 6 处 `Assert.Equal(2/3/4, X.Count)`（`:372`／`:466`／`:635`／`:693`／`:694`／`:752`）**保留原样**——门禁不判它们，硬改成 `Assert.Single` 会改判据本身 |

改完的字节：`NarrativeContextCompiler.cs`＝`86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842`（原 `a3d0c4fe…cfceb`），`NarratorPrototypeTests.cs`＝`cb5e6c60dd695641f36f24dc03f06c56d4391f8e95683bdc364926688d63adf9`（原 `bcb6a269…c0e2bc`）。`NarrativeRoundRunner.cs`／`NarratorContracts.cs` 未改（`92fbce48…`／`3a33a815…` 照原）。行数一格没动（470／822），所以 §二／§三 报出的 TRX 栈行号仍然指得到同一处断言。

### 七.2 两枚旧红的当场复现归零（只认盘上 TRX）

| 跑次 | 命令要点 | 盘上读数 |
|---|---|---|
| `runs/run-fix-clean/…15_47_25.trx` | 定向 `--filter FullyQualifiedName~NarratorPrototypeTests`，带构建（不带 `--no-build`） | executed 49／passed 49／failed 0；同轮 `Pry.Core` 与 `Pry.Core.Tests` 两枚工程重编译，stdout 里 `warning` 行数 0 |
| `runs/run-fix-ledger/…15_47_39.trx` | 定向 `--filter FullyQualifiedName~ResourceLimitLedgerCoverageTests` | executed 4／passed 4／failed 0——§四.6 那两枚红（`Every_limit_shaped_constant_has_a_row_in_the_ledger`、`The_guard_sees_a_row_being_dropped_and_a_row_drifting_off_its_line`）同名转绿，且没少一格 |

### 七.3 字节变了 ⇒ 15 枚单放反证在新字节上逐枚重取

按 §三.17 第 3 条自己写下的形状执行：改动点只有那一枚测试文件＋那一枚装配器，所以 15 枚全部重取。载体＝`66906d0`（本波第二把，父 `98246cd`），每轮 `FROM→TO` 命中恰好 1 处（`REPLACEMENTS=1`）、`CHANGED_LINES=2`、跑前预检 `BUILD_OK`、跑完逐字节 `cp` 回 pristine 并双哈希对平。

| 轮 | 改坏的那一处（文件:行） | counters（失败/通过/总计） | 红格名（`NarratorPrototypeTests.` 前缀省略） | TRX 锚点 |
|---|---|---|---|---|
| re-m01 | `NarrativeContextCompiler.cs:51` 按段份额 `< 0` → `< 0 && x > 1000` | 1／48／49 | `Compiler_rejects_a_negative_section_share` | `:352` |
| re-m02 | `:59` `total <= 0` → `total <= -1000000` | 3／46／49 | `Compiler_refuses_a_window_that_leaves_no_positive_input_budget`、`Round_does_not_call_the_provider_when_the_context_is_rejected`、`Call_refuses_an_assembly_that_still_carries_a_rejection` | `:243`／`:404`／`:430` |
| re-m03 | `:68` 身份 `IsNullOrWhiteSpace` → `is null` | 1／48／49 | `Compiler_refuses_a_scene_without_identity_instead_of_inventing_one` | `:251` |
| re-m04 | `:75` 会话校验那半条 → `\|\| false)` | 1／48／49 | `Compiler_refuses_a_current_event_from_another_conversation` | `:261` |
| re-m05 | `:84` `preludeCost > total` → `> total * 100` | 1／48／49 | `Compiler_refuses_the_round_when_the_mandatory_prelude_does_not_fit` | `:232` |
| re-m06 | `:107` 丢掉 `cost > budget.AdvisoryTokens` 一半 | 1／48／49 | `Compiler_fails_loudly_when_the_reserved_directive_does_not_fit` | `:271` |
| re-m07 | `:200` 丢掉 `cost > cap` 一半 | 1／48／49 | `Compiler_drops_an_optional_section_that_exceeds_its_own_share_and_counts_it_exactly` | `:217` |
| re-m08 | `:243` 丢掉 `wallet - cost < 0` 一半 | 2／47／49 | `Compiler_trims_recent_messages_only_at_whole_unit_boundaries`、`Compiler_shrinks_the_memory_wallet_unit_by_unit` | `:289`／`:320` |
| re-m09 | `:236` 跨会话比对 → `ConversationId is null` | 1／48／49 | `Compiler_removes_cross_conversation_units_and_names_the_count` | `:304` |
| re-m10 | `:281` `NotSupplied` → `Dropped` | 1／48／49 | `Compiler_keeps_absent_sections_as_not_supplied_instead_of_dropping_them` | `:202` |
| re-m11 | `NarratorContracts.cs:104` `next != 1` → `next > 2` | 1／48／49 | `Second_call_on_the_same_round_budget_is_refused` | `:413` |
| re-m12 | `NarratorContracts.cs:36` 装配体拒绝门加 `&& Count < 0` | 1／48／49 | `Call_refuses_an_assembly_that_still_carries_a_rejection` | `:425` |
| re-m13 | `NarrativeRoundRunner.cs:92` 上下文被拒门加 `&& Count < 0` | 1／48／49 | `Round_does_not_call_the_provider_when_the_context_is_rejected`（抛在 `NarratorContracts.cs:38`→`NarrativeRoundRunner.cs:105`） | `:398` |
| re-m14 | `NarrativeRoundRunner.cs:112` `ProviderRejected` → `Silence` | 4／45／49 | `Malformed_response_is_not_masquerading_as_a_chosen_silence`、`A_provider_that_ignores_cancellation_still_exits_as_timeout`、`A_provider_that_honours_the_timeout_token_exits_as_timeout_too`、`Provider_failure_becomes_a_named_error_shape_and_writes_nothing` | `:540`／`:555`／`:569`／`:582` |
| re-m15 | `NarrativeRoundRunner.cs:155` `deferred.Length > 0` → `> 1` | 1／48／49 | `Deferred_round_registers_the_intent_without_touching_real_history` | `:509` |

15／15 轮都拿到点名红，零空档；22 枚红点与 §三 那张原表的 counters **逐行对平**（`m02=3`、`m08=2`、`m14=4`，其余各 1）。每轮跑完 `git status --short`＝2 行（只有守门人这两枚在修的文件），15 轮还原后仍＝2 行；载体收尾 `git status --short`＝**0 行**。

**就地更正（守门人自己的口算错，原文不涂）**：上面那句"22 枚红点"错，盘上真值＝**21 枚**。当场把 15 份 `runs/run-re-m01…m15` 的 TRX 逐份重读 `<Counters … failed="…">` 求和＝`m02`3＋`m08`2＋`m14`4＋其余 12 轮各 1＝**21**；lane 原表那 15 轮（`runs/run-m01…m15`）用同一把尺重读求和也是 **21**，所以"**与 §三 那张原表的 counters 逐行对平**"这半句照旧成立、逐行读数一格没变，错的只有我那句合计。根因＝那句合计是我写登记时心算出来的，没有按规矩先从 15 份 TRX 逐份读数再求和；真值以两次当场重读为准。

### 七.4 一次构建＋一次全量（载体 `66906d0`）

- 预检（`out/preflight-fix-build.txt`，15:52:35）：`VERDICT=BUILD_OK`，`free_memory_gb=8.3`，本轮无 WARN。
- `dotnet build Pry.slnx -c Release`（`out/build-fix.txt`）：`已成功生成。`／**`0 个警告`**／`0 个错误`（§三.17 那 46 条归零，全仓九枚工程仍 0 条）。
- 预检（`out/preflight-fix-full.txt`，15:52:52）：`BUILD_OK`，`free_memory_gb=7.4`。
- `dotnet test Pry.slnx -c Release --no-build --logger trx --results-directory …\runs\run-fix-full`：`ls *.trx`＝4；Client 108／App 233／Api 280 全 passed、Core **607**（executed 605／passed 605／failed 0）⇒ **总 1228／0 红／2 条按名跳过的同名环境格**（`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`、`CoreTests.Configured_sense_voice_model_can_run_offline`）。§四.6 那 2 枚红在这一轮是 0。
- 载体分支 `codex/wave13-20261023` 两把：`98246cd`（原型）＋`66906d0`（本次修复，2 files changed, 54 insertions(+), 54 deletions(-)）。

### 七.5 归位与合流复采（守门人另一手，另一枚载体）

1. 归位＝主树 `290a603`（4 枚新文件），逐字节对平 `66906d0` 的 blob 哈希（`86c30160…`／`92fbce48…`／`3a33a815…`／`cb5e6c60…` 四枚全等）；同场把第十四波 `f07f1d2` 的那枚新测试文件（`99cf0306…`）作 `d3fbac4` 分刀归位。他人名下两枚暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）用 `git commit --only` 保住，跑前跑后都还是 `M ` 两行。
2. 合流载体＝`git worktree add --detach .local-notes/qa-wave134-merge-1023/carrier d3fbac4`（干净副本，`git status --short`＝0 行）。预检 `BUILD_OK`（7.7 GB／6.8 GB）⇒ 一次构建 **0 警告／0 错误**；一次全量 `--no-build`＝4 份 TRX：Client 108／App 233／Api 280／Core 623（executed 621）⇒ **总 1244／0 红／2 条同名跳过**，与"1179＋49（十三波）＋16（十四波）＝1244"的对账等式逐位相符。
3. `scripts/verify-repository.ps1`＝**562 tracked files**（这一波落了 5 枚新跟踪文件；该数是当前跟踪文件条数，不是门槛）；`scripts/measure-fixed-eval.ps1 -CatalogOnly`＝**Catalog valid: 76 permanent scenarios**（未动场景）；`git diff --check` 干净。

### 七.6 本节没做的事（照旧未验证，不因收口升格为事实）

- §六.2 的 1～4 条（真实模型对照、生产接线、UI 可见行为、生产库）**一字未变**：本节只动了测试写法与一个常量名，没有任何一格里接上真模型或生产链。
- §三.16 那 **9 组没有单枚反证的落门**仍未拆（未拆 ≠ 拆了不响）。重取的 15 枚只覆盖 §三.1–三.15 已点名的那批门。
- 第十二波待拆清单里"放宽方向"（正向 `→ None` 那几行）无人守，照旧在册。
- 本轮**没有**第二只手复采：合流那一轮的判据是本守门人写的、构建与读数也是本守门人跑的，所以 1244 只是"守门人独立载体单采"，在册基线的改写权仍在 QA／下一次换手的复采。
- 仪器欠账一条，明写：改完的 `NarratorPrototypeTests.cs` 里那 27 行现在长 130–157 字符。仓库没有行长门禁（无 `.editorconfig`，既有测试里也有 >150 字符的行），所以保留单行写法**不换行**——换行会移动行号，让 §二／§三 已归档的 TRX 锚点指错地方。
