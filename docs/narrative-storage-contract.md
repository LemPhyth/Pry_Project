# Pry 叙事与存储契约（HDS-002）

> 工单：HDS-002（稳定身份与存储契约）。范围只有两件事：**把契约定成文字**，以及**把契约写成可编译的纯领域类型＋三类反例测试**。
> 本波不落表、不预占 schema 版本号、不接线到任何现存回复链路。实际建表与迁移执行是 HDS-001/HDS-005 的落点。
>
> 每一条契约都按同一形状陈述：**现在是什么 → 本契约定成什么 → 违反时怎样被拒**。
> 定不下来的地方一律进 §12「未决项」，不写成假装已经 settled 的规则。

---

## 0. 权威依据与本文地位

| 依据 | 用到的部分 |
| --- | --- |
| `docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md` | §5 主叙事与四类资料、§7 通信动作与原文绑定、§8 进程级独占写锁、§9.2 事务 A/B、§12 outbox 与投递段、§15 引擎模式 |
| `docs/dispatch-hds-wave1-20261021.md` | HDS-002 的范围与「不许走的捷径」 |
| `docs/plans/PROJECT_PLAN.md` | §1 定位、§2 边界、§4.2/§4.3 存储与身份、§5 叙事、§6 投递、§7 迁移、§11 状态词约束 |
| `docs/plans/README.md` | 13 条作业规则（本文遵守第 3/5/9/11/13 条：不写他人名下文档、不以完成词自居、拒绝要有病因） |

本文**不覆盖**也不修改上面任何一份；出现分歧时以 v4 架构方案为准，并把分歧登记到 §12。

类型落点：`src/Pry.Core/Narrative/`（8 个新文件，全部为纯领域类型，无 HTTP／Avalonia／SQLite／进程／文件系统依赖）。
测试落点：`tests/Pry.Core.Tests/NarrativeIdentityContractTests.cs`、`tests/Pry.Core.Tests/NarrativeStorageContractTests.cs`（均为新建独立文件，未改动他人测试）。

---

## 1. 契约条目 → 类型 → 理由码 → 测试 对照

| # | 契约条目 | 主要类型 | 理由码（`NarrativeContractReason`） | 守它的测试 |
| --- | --- | --- | --- | --- |
| 2 | 稳定实例身份 | `CharacterInstanceId` / `CharacterInstanceKey` / `CharacterInstanceRegistry` | 100–110 | `Restart_proposing_a_different_identity_never_rebuilds_the_instance` 等 10 条 |
| 3 | 叙事文档族与归属边界 | `NarrativeRequest` / `NarrativeDraft` / `NarrativeCommit` / `NarrativeDraftValidator` | 200–216 | `Draft_that_picks_its_own_instance_or_conversation_is_not_a_proposal` 等 |
| 4 | 消费水位 vs 感知引用 | `ProcessedThroughSeq` / `PerceivedInputRefs` / `InputRef` | 202–204、215 | `Perceived_refs_must_come_from_the_issued_batch`、`Silence_advances_the_watermark_without_perceiving_anything`、`Commit_may_not_consume_input_the_host_never_received` |
| 5 | 通信动作与原文绑定 | `CommunicationAction` / `ProseBinding` / `NarrativeProseBinder` | 205–208 | `Immediate_text_must_locate_itself_in_the_narrative_once`(4 例)、`Immediate_action_with_unlocatable_text_is_refused_not_rewritten` |
| 6 | 投递段账本与唯一键 | `DeliverySegment` / `DeliverySegmentKey` / `DeliveryLedger` | 400–410 | `Same_commit_action_and_segment_index_cannot_be_written_twice` 等 8 条 |
| 7 | 意图与认知候选 | `NarrativeIntent` / `KnowledgeCandidate` / 两个 Policy | 209–211、210 | `Deferred_action_without_intent_is_a_promise_nothing_can_keep`、`Knowledge_candidate_origin_decides_what_it_may_assert`(5 例)、`Intent_from_another_instance_scope_is_refused` |
| 8 | 单一 outbox 与写入权 | `EngineMode` / `SendableTruthSource` / `WriteOwnershipTicket` / `OutboxOwnershipRegistry` | 300–306 | `Second_writer_on_the_same_instance_is_rejected_and_cannot_commit` 等 6 条 |
| 9 | 条件写 | `NarrativeCommitWriter` | 500–505、213、212 | `Late_commit_from_a_lost_ownership_epoch_is_rejected_without_overwriting_anything` 等 11 条 |
| 10 | 数据目录进程级独占写锁 | `DataDirectoryWriteGate` / `WriteLeaseCredential` | 600–603、601 | `Runtime_descriptor_is_discovery_not_ownership` 等 4 条 |

理由码是稳定标识：宿主可以把它映射成 HTTP 语义、诊断文案或指标，但**判定只认枚举，不认消息文本**。分组之间留了空位，后续任务往组里加成员不会改变已有编号。

---

## 2. 稳定身份：`CharacterInstanceId` = LocalUser × Character

### 2.1 现在是什么

- `src/Pry.Core/Memory/MemoryContracts.cs:8` —
  `public static MemoryScope ForCharacter(string id) => new($"character:{id}", id);`
  即：今天记忆作用域就是角色卡 ID 的字符串拼接，**没有用户维**。同一行注释已经写明「instance scope must never be derived from a character id alone」。
- `src/Pry.Api/Runtime/CharacterIntelligenceGateway.cs:336` —
  `CharacterInstanceId = character.Id`：今天所谓「实例身份」传下去的就是角色卡 ID 本身。
  同文件 26–31 行的注释自己登记了这个缺口（`docs/character-intelligence.md` §8 缺口 ③）：出现一卡多实例时，两处必须一起改，否则删卡的 `PurgeAsync` 会静默漏掉后建的实例。
- `CharacterIntelligenceGateway.LocalUserId = "local"`（同文件 38 行）：产品当前一台机器一个用户，但这个常量只活在 gateway 里，没有任何地方把它和角色 ID 一起持久化成一条实例记录。
- 结论：**当前不存在「实例」这一层**，只有「角色卡」这一层被当作实例用。每次启动不需要重建身份（因为身份就是卡 ID），代价是身份里天生缺用户维、也无法表达一卡多实例。

### 2.2 本契约定成什么

1. **实例身份是一次性的、宿主签发的、持久的字符串**：`CharacterInstanceId` 是不可变类型（私有构造 + `Create` 工厂），字符集限 `[a-z0-9_-]`、长度 8–128，允许 GUID/ULID/uuid7 形状。它由宿主在**首次绑定**时签发一次并落盘；之后所有叙事、记忆、意图、投递、知识候选都以它为作用域根。
2. **自然键是 `CharacterInstanceKey { LocalUserId, CharacterId }`**，两个分量都必填（`Validate()` 缺一即拒）。身份与自然键的关系是「一次绑定产生一行映射」，映射行同时记录 `InstanceProvenance`（HostBound / MigratedFromAlias / ReissuedWithEvidence）、映射契约版本与 `OriginAlias`，用于事后回答「这个 ID 是哪一次绑定产生的」。
3. **`Resolve` 必须幂等**：已登记的 User × Character 永远返回既有身份，这次调用带进来的任何新提案被忽略并如实上报 `IdentityProposalSuperseded`（不是异常、不是静默）。
4. **迁移与 alias 方案**（`MigrateIdentity` / `MigrateIdentities` / `BindLegacyScope` / `ResolveAlias`）：
   - 旧作用域键 `character:<id>` 通过 `BindLegacyScope` 显式绑到稳定实例；一条实例只允许一个旧作用域入口；一个别名只能属于一个实例。
   - 未登记的别名**不去猜**：`ResolveAlias` 返回 null，调用方必须走迁移而不是「看起来像」。
   - 换发新稳定 ID 的必要条件（三者同时成立才落）：新身份不是角色 ID 的另一种写法；原实例若持有证据引用则必须显式 `CarryEvidenceReferences`；`RetainLegacyIdentityAsAlias` 默认为 true——旧 ID 迁移后必须仍能解析到当前身份。
   - **批量换发 all-or-nothing**：任何一条被拒 ⇒ 整批不落，且每条结果的 `Reason` 统一改写成 `IdentityBatchRejected`，原始病因保留在 `Detail` 里。半途成功的批量换 ID 是「批量丢证据」的唯一现实路径，所以这里不给部分成功。
5. **证据引用随行**：`RecordEvidenceReference` / `EvidenceReferenceCount` / `OwnerOfEvidence` 是登记处对「这条记忆行/这段原文属于哪个实例」的唯一问法。迁移成功时证据集合整体搬到新身份名下，旧身份名下计数归零。

### 2.3 违反时怎样被拒

| 违例形状 | 拒绝点与理由码 | 测试 |
| --- | --- | --- |
| 每次启动提出一个新 ID 想重建实例 | `Resolve` 返回既有身份 + `IdentityProposalSuperseded`(104)，`Created=false`，绝不新增登记行 | `Restart_proposing_a_different_identity_never_rebuilds_the_instance` |
| 首次绑定没有宿主身份，想让 Core「随便造一个」 | 抛 `NarrativeContractException` / `IdentityValueBlank`(100)：构造期结构不合法是编程错误，不给兜底 | `First_binding_without_host_identity_is_refused_instead_of_inventing_one` |
| 身份是空白 / 超长 / 字符集不符 / 短于 8 | `IdentityValueBlank` 100 / `IdentityValueTooLong` 101 / `IdentityValueMalformed` 102（5 例 Theory） | `Identity_must_be_a_host_issued_stable_token` |
| 只给 `CharacterId` 换个写法（`pry-chan` → `pry_chan`）当作新实例 | 归一化（剥 `character:` 前缀、去 `-`/`_`、转小写）后相等即认定「同一个卡 ID 的另一种写法」⇒ `IdentityRenameIsNotAMigration`(105)，迁移不落，当前身份不变 | `Renaming_the_character_id_is_not_a_migration` |
| 有证据却不带 `CarryEvidenceReferences`，或带证据却不保留 alias | `IdentityReissueOrphansEvidence`(106)，且旧证据归属仍指向原身份 | `Reissuing_identity_without_carrying_evidence_is_refused` |
| 批量换发中某一条会把实例弄丢／撞名 | 整批 `IdentityBatchRejected`(107)，登记状态与调用前逐字节一致 | `Batch_reissue_that_would_orphan_any_instance_is_rejected_whole` |
| 别名重复绑到第二个实例 | `AliasConflict`(110)，先前解析结果不变 | `Legacy_character_scope_alias_resolves_to_the_stable_instance_once` |
| 缺 `LocalUserId` 或缺 `CharacterId` | `InstanceKeyIncomplete`(103) | `Instance_key_requires_both_user_and_character` |
| 提交指向没登记的实例 | 条件写第一关 `InstanceUnknown`(108)，账上什么都不写 | `Commit_for_an_unregistered_instance_has_nowhere_to_belong` |

`AliasUnknown`(109) 预留给存储层的映射查询使用（见 §12 未决项 U-1）。

---

## 3. 叙事文档族与归属边界：`NarrativeRequest` / `Draft` / `Commit`

### 3.1 现在是什么

- 今天的回复链路是 `BehaviorPlanner → ReplyGenerationRenderer → ReplyPlanner`（`src/Pry.Core/TurnTaking/ReplyPlanner.cs:12`），生成结果直接写进 `reply_plans` / `planned_messages`（`src/Pry.Core/Memory/MemoryDatabase.Runtime.cs:146`）。
- 这一条链上**没有「宿主签发的回合凭据」这个概念**：模型侧不需要携带实例身份、批次上界、故事版本、所有权代次，也不携带叙事原文；`ReplyPlan`（`src/Pry.Core/Models/DomainModels.cs:107`）就是待发文本本身。
- 因此今天无法回答「这条回复是哪一次叙事的哪个切片」，也无法阻止一个提案把作用域写成别的会话。

### 3.2 本契约定成什么

- **`NarrativeRequest`（宿主签发，模型只能引用）**：`InstanceId`、`ConversationId`、`BatchId`、`Inputs`（宿主赋号的 `InputRef` 列表）、`Trigger`、`StoryVersion`、`DeletionEpoch`、`OwnershipEpoch`、`IssuedByWriter`、`ContractVersion`、`MaxOutboundSegments`（默认 4，宿主资源与体验上限，不是模型可自报的量）、`RequiresInput`、`IssuedAtUtc`、`ProcessedThroughAtIssue`。派生量 `InputSeqs` / `BatchHighestSeq` 为普通计算属性（不在初始化器求值，避免读到未赋值的 required 属性）。
- **`NarrativeDraft`（模型提案）**：只能引用请求里已签发的身份/会话/批次，加叙事原文、水位认领、感知引用、动作、意图、认知候选、`ProposalId`。**提案不是真相**：它不携带 `CommitId`，不能自己填 `Binding`（宿主解析结果），也不能自己填 `ContentHash`。
- **`NarrativeCommit`（提交＝已发生的一段叙事）**：在草稿语义之上补宿主责任四项——`CommitId`、`ExpectedStoryVersion`、`OwnershipEpoch`、`ContentHash`，再加 `ContractVersion`、`CommittedByWriter`、`ProposalId`（提交与提案一一对应）、`ConsumedInputSeqs`。
- **归属边界的唯一表述**：身份、作用域、批次、上限、时间窗由宿主签发；模型只能在划出的范围内提提案。任何自扩大（换实例、换会话、换批次、造序号）都是拒绝，不是纠正。
- 草稿与提交共用一套语义判定（`NarrativeContent.From(draft|commit)` → `NarrativeDraftValidator`），**不留「草稿放行、提交另有一套规则」的二次真相**；`NarrativeCommitValidator` 只加宿主责任项（`CommitId`、契约版本、内容散列、意图来源必须收敛到本次 `CommitId` 或本次 `BatchId`）。
- 校验返回**全部**拒绝而不是第一个，让宿主一次说完「这一轮为什么不被认可」，小模型修复不必逐条试。

### 3.3 违反时怎样被拒

| 违例 | 理由码 |
| --- | --- |
| 提案自带实例或会话（不是宿主签发的） | `DraftIdentityMismatch`(201)、`ScopeEscalation`(216) |
| 批次为空却声明需要输入 | `BatchEmpty`(200)（构造期抛出） |
| 契约版本不认识 | `ContractVersionUnsupported`(212)：`IsSupportedNarrativeVersion` 只认 1；加版本必须同时加迁移说明 |
| 没有内容散列 | `ContentHashMissing`(213)：恢复时无法自证没被改写 ⇒ 一律拒 |
| 叙事原文缺失 | `NarrativeProseMissing`(214)：没有原文就没有可绑定的处境 |
| 动作目标会话 ≠ 请求会话 | `ActionTargetMismatch`(207) |
| 意图来源既不是本次提交也不是本次批次 | `IntentClaimedAsFact`(209) |

测试：`Draft_that_picks_its_own_instance_or_conversation_is_not_a_proposal`、`Commit_with_an_unknown_contract_version_is_refused`、`Commit_without_content_hash_cannot_prove_itself_after_a_crash`。

---

## 4. `ProcessedThroughSeq` 与 `PerceivedInputRefs` 必须分开

### 4.1 现在是什么

今天这两件事根本没有被分别记录：`reply_plans` 只关心「接下来发什么」，历史 `messages` 只记录「谁在什么时候说了什么」。既没有一个「宿主消费到哪里」的水位字段，也没有「这一条叙事注意到哪几条输入」的引用。于是有两个现实后果：
- 重连/重启后无法判断某条输入是否已经被处理过（幂等只能靠 `turn_id`/`generation_id` 碰运气）；
- 「角色回应了哪句话」只能由文本内容反推，把「看到了」与「处理完了」混成同一件事。

### 4.2 本契约定成什么

- **`ProcessedThroughSeq(long Seq)`＝消费水位**：宿主已经成功处理到的批次前缀。它是**宿主的账**，只随有效提交单调前进（`AdvancesPast`：相等也算拒，因为那意味着重复消费同一前缀），上界是宿主签发的批次最高序号（`WithinBatch`），下界是签发时刻的基线 `request.ProcessedThroughAtIssue`。
- **`PerceivedInputRefs{ Refs }`＝感知引用**：这一条叙事感知到了哪些输入。它是**叙事的内容属性**，集合语义、可空（沉默/到期推进可以为 `None`），必须整体落在本批次签发的序号里（`CheckSubsetOf`）。
- 两者**故意做成两个不同类型、两个字段、两套判定**，就是为了让人无法把一个直接赋给另一个：
  - 水位不能「顺带确认」感知——处理过不等于注意到；
  - 感知不能抬高水位——看到了不等于处理完了。
- 一次提交同时携带两者：`ProcessedThrough`（进宿主的账）与 `PerceivedInputRefs`（进叙事的账），另有 `ConsumedInputSeqs` 表示本次真正消费掉的序号——它由「水位 + 批次」共同决定，**不由感知引用给出**。

### 4.3 违反时怎样被拒

| 违例 | 理由码 | 测试 |
| --- | --- | --- |
| 感知引用指向批次外的序号 | `PerceivedInputUnknown`(202) | `Perceived_refs_must_come_from_the_issued_batch` |
| 水位越过批次上界 | `ProcessedBeyondBatch`(203) | `Consumption_watermark_may_not_point_beyond_the_batch`、条件写同一关再挡一次 |
| 水位不前进或回退（换个 commitId 也没用） | `WatermarkNotAdvancing`(204) | `Watermark_can_never_regress_under_a_new_commit_id` |
| 声称消费了宿主没收到的输入 | `ProcessedInputUnreceived`(215) | `Commit_may_not_consume_input_the_host_never_received` |
| 沉默被当成失败 | 不是失败：Silence 动作合法、水位照前进、感知可以为空、投递段为零 | `Silence_advances_the_watermark_without_perceiving_anything`、`Silence_still_lands_a_commit_but_opens_no_delivery_at_all` |

---

## 5. `CommunicationAction` 与原文绑定

### 5.1 现在是什么

`planned_messages.state` 只有 pending/delivered/cancelled（`MemoryDatabase.Runtime.cs`），承载的是「即将发送的文本」，文本由渲染器直接产出，不存在「这段发送文本是叙事原文的哪个切片」的记录。

### 5.2 本契约定成什么

- `CommunicationAction{ ActionId, Mode(Silence|Immediate|Deferred), TargetConversationId, ProseQuote, PlannedSegmentCount, ExpiresAtUtc, DeferredIntentId, Binding }`。它是**角色决定怎么说**，不等于实际发言；只有投递段落进真实历史。
- `ProseBinding{ QuotedText, ResolvedOffset, Failure }`：模型只给**字面引用**，偏移与散列由宿主算（`NarrativeProseBinder.Bind`）。原文中出现 0 次＝缺失（`ActionBindingMissing` 205），出现多于 1 次＝歧义（`ActionBindingAmbiguous` 206）。
- 三种模式的形状约束：Immediate 段数 1..`MaxOutboundSegments` 且必须有绑定，且不得同时挂未来意图；Deferred 段数必须为 0、不得带正文引用、必须指向本次提案里存在的意图；Silence 段数必须为 0 且不得带正文——**沉默是合法无外发，不是待发文本**。
- `Binding` 是 `init` 属性且由宿主在条件写里回填（`TryCommit` 里对每个动作 `Bind` 一次），提案自己填了也不算数（判定看的是绑定结果而不是引用来路）。

### 5.3 违反时怎样被拒

`ActionSegmentCountInvalid`(208)、`ActionBindingMissing`(205)、`ActionBindingAmbiguous`(206)、`IntentClaimedAsFact`(209)、`SegmentCountExceeded`(404)。
测试：`Immediate_text_must_locate_itself_in_the_narrative_once`（4 例：唯一出现／缺失／两次出现／空引用）、`Immediate_action_with_unlocatable_text_is_refused_not_rewritten`（**宿主不代角色补一句「合理回复」**）、`Deferred_action_without_intent_is_a_promise_nothing_can_keep`。

---

## 6. `DeliverySegment` 与投递段唯一键

### 6.1 现在是什么

`planned_messages` 是 reply plan 下的行，靠自增 `id` 区分；没有 `(commitId, actionId, segmentIndex)` 这样的复合唯一键，也没有「同键二次写」的拒绝形状——今天的写路径一旦重入，行为取决于调用方是否记得先查。

### 6.2 本契约定成什么

- **唯一键 `(commitId, actionId, segmentIndex)`**（`readonly record struct DeliverySegmentKey`）。键相同即同一件事，第二次写必须撞墙：`SegmentKeyDuplicate`(400)。**不存在「后写覆盖前写」**，因为那会静默改掉已经发生的投递证据。
- 段的正文 `Text` 必须**逐字**来自该动作绑定所指的叙事切片：账本用 `commit.NarrativeProse.Substring(ResolvedOffset, QuotedText.Length)` 还原切片，并要求「已有各段 + 本段」仍是该切片的前缀；最后一段封口时必须正好还原完整切片长度。任何一字之差 ⇒ `SegmentTextNotFromNarrative`(407)。**投递不是第二次改写**。
- 序号必须连续（跳号 ⇒ `SegmentIndexNotContiguous` 403；负数同拒），段数上限取 `min(action.PlannedSegmentCount, commit.MaxSegmentsPerAction)`（越界 ⇒ `SegmentCountExceeded` 404）。写者不能靠往 `DeliverySegment` 上塞一个更大的数字来「扩容」——上限只来自已生效的提交。
- 只有**条件写成功的提交**才有资格产生投递段（`RegisterAppliedCommit` 只在 `TryCommit` 成功路径里被调用）；否则 `SegmentCommitUnknown`(401)。动作不存在 ⇒ 402；投递段落进请求作用域外的会话 ⇒ `ScopeEscalation`(216)。
- 状态机：`Pending → Delivered | Cancelled`，`Unknown` 留给恢复期。`Delivered`/`Cancelled` 是**终态**，迟到结果不得翻转（`SegmentTerminalImmutable` 406）；前一段没落地，后一段不许先落（`SegmentPredecessorPending` 405）；标 `Delivered` 必须有真实历史标识（`SegmentDeliveryEvidenceMissing` 410）；正文与贴纸标识都为空 ⇒ `SegmentStickerPayloadInvalid`(408)。
- 被拒的结果对象里带的 `Segment` 是**账本里的既有真相**（可能为 null），绝不是这次没写进去的那一份——这样调用方拿到的读数不会自相矛盾。

### 6.3 违反时怎样被拒（反例②的现场）

`Same_commit_action_and_segment_index_cannot_be_written_twice` 同时验证三件事：拒绝 ⇒ 原因码 400；账本内容不变（第一段仍是原措辞、状态仍 Pending）；`Snapshot()` 没有多出一条。
其余：`Segment_index_may_not_skip_ahead_and_action_may_not_grow_extra_segments`、`Segment_text_must_be_taken_from_the_bound_narrative_verbatim`、`Delivered_segment_is_terminal_and_a_late_result_cannot_flip_it`、`Later_segment_cannot_be_marked_delivered_before_its_predecessor`、`A_segment_needs_real_history_evidence_to_be_called_delivered`、`Fenced_writer_cannot_touch_the_delivery_ledger`、`Segments_cannot_be_appended_for_a_commit_that_never_applied`。

---

## 7. `NarrativeIntent` 与 `KnowledgeCandidate`

### 7.1 现在是什么

`conversation_runtime` 里有主动推进的时间戳，但「她打算稍后做某事」没有一等公民的记录，也没有「到期不等于已发生」的判定；记忆侧的候选今天直接进 `messages`/FTS，角色虚构与用户现实之间**没有类型区分**。

### 7.2 本契约定成什么

- `NarrativeIntent{ IntentId, InstanceId, Topic, Status, SourceCommitId, EarliestDueAtUtc, LatestDueAtUtc, ProactiveSendPermitted }`。意图是**有来源的待处理动机**，不是已发生的事实：来源缺失即不登记；`Fulfilled` 必须能对应到本次提交里一条 `DeferredIntentId` 指向它的 Immediate 动作；`ProactiveSendPermitted` 与「允许后台推进叙事」是两个独立许可（v4 §13 的许可分层）。
- `KnowledgeCandidate{ CandidateId, InstanceId, Kind, Origin, Claim, Confidence[0,1], EvidenceRefs, ProposedBy }`，`CognitionKind`：UserReportedFact / CharacterFiction / CharacterBelief / Proposal / ConfirmedAgreement / NarrativeProse；`CognitionOrigin`：UserMessageEvidence / HostObservation / ExplicitFeedback / NarrativeProse / ModelSelfReport / Intent。
- **来源决定它能支撑哪种断言**（这是防止现实污染的唯一抓手）：
  - `UserReportedFact` 不允许来源是叙事原文／模型自评／意图 ⇒ `RealityContamination`(210)；
  - `ConfirmedAgreement` 必须有现实来源**且**有证据引用 ⇒ `ConfirmationWithoutUserEvidence`(211)；
  - `Proposal` 带高置信（>0.7）的现实来源 ⇒ `IntentClaimedAsFact`(209)（尚未发生的事不该按事实召回）；
  - `CharacterFiction` 用现实来源支撑 ⇒ 210；
  - `NarrativeProse` 永远只是导航材料：来源只能是原文或自评。

### 7.3 违反时怎样被拒

理由码 209/210/211 加上跨作用域的 216。测试：`Knowledge_candidate_origin_decides_what_it_may_assert`（5 例）、`Confirmed_agreement_without_any_evidence_reference_is_refused`、`Intent_from_another_instance_scope_is_refused`。

---

## 8. 单一 outbox：谁在什么条件下持有写入权与投递权

### 8.1 现在是什么

`reply_plans`（配 `planned_messages`）就是当前的「可发送真相」：写路径是 `ReplyPlanner` 生成 → 落 `reply_plans` → 宿主按计划发送。表上已经有 `fence_epoch`，条件更新用 `WHERE r.id=$p AND r.status='open' AND rtm.epoch=$epoch AND r.fence_epoch=$epoch`（`MemoryDatabase.Runtime.cs:200`、`MemoryDatabase.Proactive.cs:217`）——**fencing 的形状已经存在**，但它只保护「同一条 plan 不被过期epoch 改写」，没有回答「这一轮到底谁有权产生待发文本」。
所以：叙事 outbox 一旦上线，同一次输入可能既产出 reply plan 又产出 outbox 条目，两份都能被发送。这就是派工单点名的「不许走的捷径」之一，本契约正面禁止。

### 8.2 本契约定成什么

- **每个实例同一时刻只有一名写者**（`OutboxOwnershipRegistry`）：`WriteOwnershipTicket{ InstanceId, Mode, WriterId, OwnershipEpoch, AcquiredAtUtc }`。
  - `TryAcquire`：已被他人持有 ⇒ `OwnershipHeldElsewhere`(300) **且不返回任何凭据**（没有「半张票」）；同写者重复申请是幂等、不换代次；同写者换模式 ⇒ `EngineModeMismatch`(303)（先交接，不要在持有期间改引擎）。
  - `TryHandover`：交出方必须用自己的票（writer + epoch 双匹配）签一次手，否则 `HandoverNotAuthorized`(306)；两端同名也拒（那只是换模式）；成功则 `OwnershipEpoch` **只增不减**（`_highestEpoch` 单调，`Release` 不让它回退）。
  - `Release`：非持有者释放不了（300）；代次落后的票释放不了别人的所有权（302）。
- **引擎模式与可发送真相绑在票上**：`EngineMode{Legacy|NarrativeV1}` 只有两个值，选择由宿主持久化、不从请求参数临时决定，也没有「都试一下」的第三种模式。`SendableTruthSource{None|LegacyReplyPlan|NarrativeOutbox}` 在同一实例上只能有一个非 None 值。
- **谁在什么条件下持有写入／投递权**（本波定的规则）：
  1. `Legacy` 模式：可发送真相必须是 `LegacyReplyPlan`；此时叙事 outbox 不得声明为真相。
  2. `NarrativeV1` 模式：真相必须是 `NarrativeOutbox`，**且声明时必须 `legacyPendingPlanCount == 0`**——旧 reply plan 还有未清点的 pending 条目就交给新 outbox ⇒ `SendableTruthNotDrained`(305)。这一条就是「先关干净、再换真相」，避免出现两份都能发。
  3. 已经声明过一种真相之后想改判另一种 ⇒ `SendableTruthAlreadyDeclared`(304)，且当前值不变。改判必须先完成显式交接与清点（`ClearSendableTruth` 把状态记回 None）。
  4. 声明与清空都必须由**当前持有写入权的票**提出（否则 300/302），别的进程不能替这个实例改判真相。
  5. 投递权是写入权的下游：`DeliveryLedger` 的每一次 `TryAppend`/`TryMarkDelivered`/`TryMarkCancelled` 都先过 `IsAuthorizedWriter`；所有权丢失后（`OwnershipFenced` 302）连账本状态都改不动。
- 条件写在**判定顺序**上把这件事排在内容校验之前：所有权先于契约版本、内容、水位、动作、意图。顺序是有意的——「谁先拒」决定运维看到的病因；一个已经没有写入权的写者不该先收到「你的措辞有问题」。

### 8.3 违反时怎样被拒（反例①的现场）

`Second_writer_on_the_same_instance_is_rejected_and_cannot_commit` 把「双写者」按三条路径都试一遍：
1. 第二写者直接申请 ⇒ 300，`Ticket` 为 null；
2. 第二写者**绕过登记处**直接条件写 ⇒ 同一个理由码 300，`StateOf(instance)` 仍为 null（账上什么都没落）；
3. 持有者自己来写 ⇒ 成立，`AppliedCommitCount == 1`。
第三条是必需的：拒绝必须只针对「第二个」，不能把这条路径整个堵死，否则测试就成了「什么都失败」的假绿。

其余：`Explicit_handover_raises_the_epoch_and_retires_the_old_ticket`（下台的旧写者 ⇒ 300，新写者拿旧代次 ⇒ 302，之后状态仍是交接那一次）、`One_writer_cannot_hold_two_engine_modes_on_the_same_instance`、`Release_by_someone_else_does_not_free_the_instance`、`New_outbox_cannot_be_declared_while_legacy_pending_remains`、`Only_the_current_writer_may_declare_the_sendable_truth`。

---

## 9. 条件写：`NarrativeCommitWriter.TryCommit`

### 9.1 现在是什么

今天的写入靠「乐观 UPDATE + fence_epoch」保护单条 plan（§8.1），但一次回合要同时落叙事、状态引用、候选意图、待发文本与消费范围，这些**不是一个条件覆盖的整体**：中途失败会留下「叙事已经写了、水位没写」这类半件。

### 9.2 本契约定成什么

一句话：**写者带来的身份、所有权代次、故事版本与消费水位必须同时等于账上的那一份；少一项或多一项都拒，且拒绝路径上一条字节都不落。**（对应 v4 §9.2 事务 A：叙事原文、状态引用、候选意图、准确输入范围与待发动作在同一次判定里成立或整体不成立。）

固定判定顺序（每步都返回稳定理由码，被拒时结果里带的是**未改动的既有状态**）：

| 顺序 | 判定 | 被拒理由码 |
| --- | --- | --- |
| 1 | 实例已登记 | `InstanceUnknown` 108 |
| 2 | 写者持有当前代次的票 | 300 / 301 / 302 |
| 3 | 契约版本受支持 | 212 |
| 4 | 内容散列存在 | 213 |
| 5 | 会话删除 epoch 未推进 | `DeletionEpochAdvanced` 504 |
| 6 | `CommitId` 未生效过；同 ID 同散列＝幂等重放 | `CommitAlreadyApplied` 502 / `DuplicateCommitId` 505 |
| 7 | `ExpectedStoryVersion == 账上故事版本` | 落后 `LateCommitRejected` 501 / 超前 `StoryVersionConflict` 500 |
| 8 | 水位严格前进 | 204 |
| 9 | 水位不越批次上界 | 203 |
| 10 | 消费序号都收到过 | 215 |
| 11 | 动作绑定可解析 + 提交语义校验全通过 | 201–211、214、216 |
| 12 | 生效：故事版本 +1、水位前移、记 `LastCommitId`、`AppliedCommitCount` +1、注册进投递账本 | — |

### 9.3 违反时怎样被拒（反例③的现场）

`Late_commit_from_a_lost_ownership_epoch_is_rejected_without_overwriting_anything` 走完整个接管剧本：提交一次 → 显式交接到代次 2 → 旧写者带着代次 1 回来（被拒 300）→ 新写者用自己接管前生成的落后代次 1（被拒 302）→ 两条被拒之后**账上仍是第一次的读数**（`LastCommitId` 相同、故事版本 1、水位 102、账本里没有迟到 commit 的段）→ 当代凭据提交才成立（水位 103、版本 2）。
迟到提交「不静默覆盖」在这条测试里是以**状态读数**断言的，不是以「抛异常」断言的：Core 不发明错误语义，只把拒绝作为数据交回。

其余：`Commit_built_on_a_stale_story_version_is_rejected_as_late`、`A_story_version_ahead_of_the_ledger_is_rejected_too`、`Replaying_the_same_commit_does_not_consume_input_twice`、`Reusing_a_commit_id_for_different_content_is_rejected`、`Late_commit_after_conversation_deletion_cannot_revive_the_scope`（删除 epoch 之后的迟到提交永久无资格，`AppliedCommitCount` 不变）。

---

## 10. 数据目录进程级独占写锁的形状

### 10.1 现在是什么

- `src/Pry.Api/Services/RuntimeDescriptorService.cs` 写 `runtime.json`，内容是 `{Pid, Port, Token, PryProtocol.Version, MemoryDatabase.SchemaVersion, StartedAt}`；每次写入前都要过 `DataDirectoryWriteLease.AssertWriteAllowed()`（条件写的第一问，§10.2 第 2 条），拿不到写权就没有下一步。
- DEF-WRIGATE-01 落地后，`StartAsync` 先取得进程级独占写锁、再挂载描述文件的写入与清理回调：`DataDirectoryWriteLease`（同目录 `src/Pry.Api/Services/DataDirectoryWriteLease.cs`）以 `FileShare.None` 打开数据目录内的 `runtime-write.lock`，把观察事实填进 `DataDirectoryObservation`，并以 `ExclusiveWriteLock` 凭据向 `DataDirectoryWriteGate.TryAcquire` 申请写权。锁在别的进程手上 ⇒ 共享冲突 ⇒ 判定 `WriteLockHeldByOther`(600) ⇒ 宿主抛 `DataDirectoryWriteLockedException`：Headline 是「这个数据目录正被另一个 Pry 同时写」，`RetryAllowed == false`，经 App 既有呈现链（`IUserPresentableError`）向人呈现这一句话并拒绝启动。
- 被拒的路径一个字节都不写：不建目录、不碰 `runtime.json`，唯一失败的操作是 OS 拒绝打开锁文件。锁句柄随宿主实例生命周期释放（`StopAsync` 与容器拆除的 `Dispose` 都归还，幂等），进程崩溃时由系统收回——后继实例因此能重新取得写权。
- `ReportLeftover()`（`RuntimeDescriptorService.cs:83` 起）只剩崩溃恢复一种语义：能走到它说明本进程已持有独占锁，遗留文件的 pid 早已不在，覆盖它安全；「pid 还活着也告警后继续写」这条路已被锁在入口截断。
- SQLite 侧的 `PRAGMA busy_timeout=5000`（`MemoryDatabase.cs:974`）仍是行级让路；目录级「谁有权写」不再靠 `runtime.json` 里的 pid 猜测，改由本节锁形状回答（§10.2 第 1 条）。

### 10.2 本契约定成什么

三条硬规矩（`DataDirectoryWriteGate`，纯判定形状；真实锁句柄由宿主 Infrastructure 层实现）：

1. **`runtime.json` 只负责发现，不是所有权锁**。凭据分两类：`ExclusiveWriteLock`（真实独占锁句柄）与 `DiscoveryDescriptorOnly`（只读到了描述文件）。后者申请写权一律 `DescriptorIsNotALock`(601)——**即使描述文件说「那个 PID 已经死了」也不构成写权**（判定顺序是先看观察事实、再看凭据类型，顺序反了就会放行「读文件自证没人用」这条路）。
2. **第二个宿主不得「告警之后继续写」**。观察事实 `ExclusiveWriteLockHeldByAnotherProcess == true` ⇒ `WriteLockHeldByOther`(600)，且 `WriteLeaseGrant.Lease` 为 null；本进程之内已有别的 holder ⇒ 同样 600。条件写的第一问 `CheckConditionalWriteAllowed` 只可能返回 `None`（当前 holder 的当前代次）或一个拒绝码；**接口上不存在「记录一次告警然后继续」的合法出口**。
3. **所有权丢失（崩溃恢复、被接管、租约到期）后条件提交必须被拒**。`FenceAndReacquire` 提升 `FencingEpoch` 并让旧凭据全部失效（`CheckConditionalWriteAllowed(旧凭据) == WriteLeaseLost` 602）；`FencingEpoch` 由锁持有处签发、本地不能自增，落后代次续期同样 602。**锁还在别的进程手上时，fencing 不能凭空发新凭据**（否则「告警后继续写」从后门回来）：此时返回 600。没有凭据时（未持有或已释放）问写权 ⇒ `WriteLockStateUnknown`(603)，即 fail-closed。
4. 实例级 `OwnershipEpoch`（§8）与目录级 `FencingEpoch`（本节）是两个层次：目录代次丢失必然使实例写权失效，实例写权交接不必然更换目录锁持有者。两者的联动落点见 §12 未决项 U-3。

### 10.3 违反时怎样被拒

| 违例 | 理由码 | 测试 |
| --- | --- | --- |
| 拿 `runtime.json` 的发现结果当写权 | 601 | `Runtime_descriptor_is_discovery_not_ownership` |
| 第二个宿主在锁被占用时继续写 | 600 | `Second_process_stops_before_writing_instead_of_warning_and_continuing` |
| 所有权丢失后用旧凭据条件写／旧凭据续期 | 602 | `Lost_ownership_fences_every_write_from_the_old_credential` |
| 别人持锁时想自封一个新代次 | 600 | 同上（第三条断言） |
| 非当前 holder／非当前代次释放；无凭据时问写权 | 600/602/603，释放后 `CurrentLease == null` ⇒ 603 | `Release_requires_the_current_holder_and_epoch` |

测试全部使用**内存里的假路径字符串**（`C:/temp/pry-hds002-isolated` 只作为 `DataDirectoryObservation` 的字段值参与判定），没有创建任何真实目录，更没有触碰真实用户数据目录 `C:/Users/24782/AppData/Local/PryCompanion`。

---

## 11. 迁移登记：本波只定设计要求

### 11.1 现在是什么（迁移落点的事实）

本波开工时（起点 `1c2e83d`）迁移落点是散的形状；工作期间 **HDS-001 已落地并把这一节改写成了下面这样**（`8ef7d01`，本分支已 rebase 到 `86cd08c` 之后复测）。所以这里同时记「开工时」与「现在」：

- **开工时**：结构版本落在 `schema_info` 表——`CREATE TABLE IF NOT EXISTS schema_info(version INTEGER NOT NULL); INSERT INTO schema_info(version) SELECT 1 WHERE NOT EXISTS(SELECT 1 FROM schema_info);`，读数统一是 `SELECT COALESCE(MAX(version), 1) FROM schema_info`，推进是 `UPDATE schema_info SET version=$version WHERE version<$version`（单调、只升不降）；加列走 `EnsureColumnAsync`（`PRAGMA table_info` 判存后 `ALTER TABLE ADD COLUMN`）；唯一的一次版本升级是硬编码步长 `UpgradeMemoryIndexAsync`（`if (current >= 2) return;` 后 `rebuild` FTS 并 `UPDATE … version=2`）。当时的行号（`MemoryDatabase.cs:31/933/956/969/977/938–962`）现已随 HDS-001 重排，不另作更正。**全仓没有 `PRAGMA user_version`**（grep 无命中），今天依然没有。
- **现在（HDS-001 落地后）**：迁移变成**可注册的具名步骤清单**——`src/Pry.Core/Memory/MemorySchemaMigrator.cs` 的 `SchemaMigrationStep(int TargetVersion, string Id, string Name, SchemaMigrationMode Mode, Func<SchemaMigrationContext, CancellationToken, Task> Apply)`，模式五种（`SelfHealingBaseline` / `VersionGatedUpgrade` / `IdempotentRepair` / `IdempotentRepairOwnsConnection` / `VersionAlign`），清单由 `MemoryDatabase.BuildMigrationSteps()`（`MemoryDatabase.cs:48`）提供、`InitializeAsync` 第 37 行整体交给 `MemorySchemaMigrator.RunAsync(connection, versionBefore, steps, token)` 执行。登记要求写在代码注释里且已被执行：`Id` 稳定唯一点分命名、`TargetVersion` **不得超过当前二进制的 `MemoryDatabase.SchemaVersion`**（不冒领新版本号）、每步幂等、每步一个事务边界、任何一步失败即回滚该步并以 `MemoryMigrationException` 停下——**不允许「跳过坏步骤继续往后写」**。
- 另有 `src/Pry.Core/Memory/SchemaVersionGuard.cs`：库里登记的版本高于当前二进制已知版本时，在**任何写入之前**抛 `SchemaVersionTooNewException`（`RetryAllowed=false`，且实现 `IUserPresentableError` 把拒绝原样呈现给用户）。这就是本文要的 fail-closed 形状，只不过它守的是结构版本，不守叙事契约版本。
- 结构版本仍被 `RuntimeDescriptorService` 读出来写进 `runtime.json`（协议握手与诊断用）——它是**发现**信息，§10 关于「描述文件不是锁」的判断不受影响。

### 11.2 本契约定成什么（设计要求，**不落表、不占版本号**）

1. **版本来源仍是 `schema_info` 这一张登记账**，**不得**改用 `PRAGMA user_version` 造成两套版本真相；一个库只能有一个「当前结构版本」的问法。HDS-001 已按这一条实现（读 `MAX(version)`、`VersionAlign` 步骤在事务里跟随成功步骤推进），本契约认可并保持。
2. **迁移走已落地的 `SchemaMigrationStep` 清单**，不再新增第二套「散装 if」。叙事侧将来要落的每一步（实例身份映射、别名账、投递段表、水位列）**必须登记成清单里的步骤**，而不是在 `InitializeAsync` 里就地补一段 SQL。本波不新增任何步骤、不分配任何 `TargetVersion`：号段归 HDS-001/005，先占号就是替别人定基线。
3. **叙事侧对迁移清单追加两项要求**（现有 `SchemaMigrationStep` 形状里没有，需在 HDS-005 落表时补，属未决项 U-6/U-11）：
   - `FenceRequirement`：该步骤是否必须持有 §10 的目录写锁才允许跑（结构变更属于写入，不能靠「我只是迁移」绕过独占锁）；
   - `BackfillEvidence`：需要回填历史数据的步骤要能回答「回填前后各有多少行」，即派工单要求的**迁移前后可测证据**。
4. **只允许前进，禁止就地降级**；被拒的迁移不留半件（回填类步骤必须在单事务内完成或整体回滚，沿用 `VersionGatedUpgrade` 的事务边界语义）。
5. **幂等性必须被验证而不是被假设**：`IdempotentRepair`/`SelfHealingBaseline` 两档每次初始化都跑，其幂等由 HDS-001 的测试守；`VersionGatedUpgrade` 档「跳过」是闸门语义，不得被读成「漏跑」。
6. **身份映射的存储要求**（§2 的落地面）：`character_instance`（自然键 User × Character、当前 `instance_id`、provenance、mapping_contract_version、origin_alias）与 `character_instance_alias`（别名 → 当前实例）两张映射账，**每个别名只有一条当前有效记录**；迁移写入必须整批 all-or-nothing（对应 `MigrateIdentities` 的 107）。本波不建表、不写 DDL、不占号。
7. **契约版本 ≠ 结构版本**：`NarrativeContractVersions` 四个常量（IdentityMapping / NarrativeDocument / DeliveryContract / OutboxOwnership）都是「文档形状」的版本，明确不与任何表名或 `schema_info` 数值绑定。加叙事契约版本必须同时加迁移说明（`IsSupportedNarrativeVersion` 是唯一入口）。
8. **两类 fail-closed 要一致**：`SchemaVersionGuard` 已经在结构版本上做到「读不懂就先别写」；本契约要求在**叙事文档形状**上有同样的处理——`ContractVersionUnsupported`(212) 在条件写第 3 关拒绝，语义与它一致：宁可停下，不猜形状。

9. **迁移的可观测性**：每一步执行前后要能回答「库里现在有 0 个还是 1 个实例账」「这一批换发落了 3 条还是 0 条」——派工单把「迁移前后实例账数量的可测证据」列为要求，本波因不落表而无法提供，登记为未决项 U-6，交由 HDS-001/005 在真实迁移上采集。

### 11.3 违反时怎样被拒

- 本波没有任何表变更，因此「抢跑建表」这类违例在 diff 里就应当可见：本次改动集只有 `src/Pry.Core/Narrative/*`（8 个新文件）与两个新测试文件，未修改任何现存 `.cs`、任何 `.sql`、`MemoryDatabase*.cs`、`MemorySchemaMigrator.cs`、`docs/plans/*`。
- 契约版本不认识 ⇒ 条件写第 3 关 `ContractVersionUnsupported`(212)（测试 `Commit_with_an_unknown_contract_version_is_refused`）：将来某条提交带着本进程不认的版本进来，是拒，不是「按最接近的形状猜一次」。

---

## 12. 未决项（明确没定下来的东西）

| # | 未决内容 | 为什么本波不定 | 归属 |
| --- | --- | --- | --- |
| U-1 | `AliasUnknown`(109) 由谁抛：Core 返回 null（现状）还是存储层抛码 | 取决于映射账是内存重建还是 SQL 查询，本波不选 | HDS-005 |
| U-2 | 目录锁的真实实现形态：命名 `Mutex` / `lockf` / `FileShare.None` 锁文件，跨重启的 `FencingEpoch` 持久化位置 | Core 不得依赖进程/文件系统类型，只能定判定形状 | HDS-001 |
| U-3 | 目录级 `FencingEpoch` 与实例级 `OwnershipEpoch` 的联动规则（目录换主是否强制实例交接） | 需要真实重启路径才能验证，纸面定死会造出假规则 | HDS-001 + HDS-005 |
| U-4 | 多段投递的原文切分算法：段边界由叙事给出还是由宿主按行/句号切 | 本波只锁「逐字来自绑定切片 + 封口还原完整切片」这一不变式；切分策略会随渲染实现变 | HDS-003/HDS-005 |
| U-5 | 旧 `reply_plans`/`planned_messages` 的清点与关闭流程（`legacyPendingPlanCount` 的真实取数与关单动作） | 涉及别人的表与写路径；本波只定「不为 0 就不许声明新 outbox」 | HDS-005 |
| U-6 | 迁移前后可测证据（实例账数量、映射行数变化） | 不建表就采不到；见 §11.2 第 3、9 点 | HDS-001/005 |
| U-7 | `CommitId`／`ContentHash` 的生成规范（宿主算：内容散列算法、ID 形状、长度上限） | Core 只要求「存在且稳定」，算法选型属实现 | HDS-005 |
| U-8 | `NarrativeDraft` 的模型侧 JSON schema 与流式解析（v4 §7 的解析层） | 属 HDS-003  narrator 侧 | HDS-003 |
| U-9 | 契约类型与 `IReplyAdvisor`/`ReplyPlanner` 的接线时机与灰度形状 | 派工单明令本波不得接线；接线即双真相风险 | HDS-004 |
| U-10 | 一卡多实例时 `CharacterIntelligenceGateway` 的传参修正（现状传 `character.Id`，见 §2.1） | 改动他人运行时文件，且要等映射账落表 | HDS-005 |
| U-11 | 往 HDS-001 已落地的 `SchemaMigrationStep` 上追加 `FenceRequirement`／`BackfillEvidence` 两项（§11.2 第 3 点） | 该类型属 HDS-001 名下文件，本波只登记设计要求，不改他人形状 | HDS-001 + HDS-005 |
| U-12 | 目录写锁与 `SchemaVersionGuard` 的先后关系（先取锁再读版本，还是读版本时不取锁） | 属初始化时序，需真实宿主进程才能验证；本波只定「迁移也是写，必须持锁」这一条 | HDS-001 |

---

## 13. 与现有链路的接线约束（本波红线）

- 未改动任何现存文件：`IReplyAdvisor`、`ReplyPlanner`、`CharacterIntelligenceGateway`、`MemoryDatabase*`、`RuntimeDescriptorService`、`src/Pry.App/*`、`docs/plans/*`、`CHANGELOG.md` 全部原样。
- 新增类型**没有**被任何生产代码引用（`src/Pry.Core/Narrative/` 只被这两个新测试文件引用）。因此本契约当前是「已定形、待接线」状态，接线波次之前不会出现两份真相。
- `Pry.Core` 未新增任何 NuGet 包，未新增 Avalonia/HTTP 依赖；`System.IO.Compression` 之外的进程/传输类型一律未用（本波连 `System.IO` 都没用）。
- 状态词：本文与代码注释只描述契约形状与拒绝行为，不写「已完成／已结案」——结论由 QA 独立复采后落账。
