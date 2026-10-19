# Character Intelligence：状态、行为与表达

负责人：Character Intelligence & Learning Engineer。认领任务：RT-003（Character State & Behavior）、RT-004（Expression）、RT-006 的一部分（Behavior Learning 的记录与召回，不含强化）。

本轮只新增 `Pry.Core` 内的领域代码与测试，**没有接线**：现有 `ReplyPlanner`、`ConversationSessionService`、`MemoryDatabase`、`Pry.App` 与 `Pry.Api` 均未修改，也不与 `Pry.Core/Runtime/`（RT-002 负责人正在写）产生编译依赖。

## 1. 边界

```text
Character State (Identity / Relationship / Mood)
  + Current Situation
  + Relevant Memory
  + Behavior Experience
        ↓ BehaviorPlanner
  Response Intent + Expression Tone
        ↓ Expression Selector
  ReplyGenerationContext  →  Runtime / Context Builder 决定说不说、何时说、怎么说
```

宿主对本模块只有四个调用点：说话前 `CharacterIntelligenceService.DecideAsync(situation, state)`，消息真正投递后 `CompleteTurnAsync(outcome)` 一次，之后收到用户评价时 `ObserveFeedbackAsync(outcome)`，以及 Context Builder 组装 Prompt 时调一次 `ReplyGenerationRenderer.Render`。没有第五个入口，也不会出现"每条消息同步学一次"的调用形状。

| 领域 | Owner | 本模块关系 |
| --- | --- | --- |
| 何时发送、取消、staleness、typing、投递 | RT-002 Runtime | 只消费我的 `BehaviorDecision`，我不触发任何发送 |
| 长期记忆抽取、证据、去重、写回、SQLite | RT-005 / MEM-005 | 只经 `IRelevantMemorySource` 拿只读视图，不查库。`MemoryQueryOutcome` 沿用 MEM-005 §6.4 的同一套取值（`Ok / DegradedKeywordOnly / Empty / Unavailable / Rejected`），`RelevantMemoryView` 只是 Intelligence 侧的临时投影：M1 的 `MemoryContext` 落地后应当删除本投影，换成一个 `MemoryHit → MemoryObservation` 适配器，而不是两套 Context 并存 |
| Capability 注册与调用 | RT-007 | 我只产出 `UseCapability` 意图与 `CapabilityHint`，不执行 |
| 表结构与迁移 | Persistence/DB | 我只定义 `I*Repository`，附带进程内实现供测试与开发模式 |

`Pry.Core.Intelligence` 与 `Pry.Core.Expression` 不依赖 HTTP、Avalonia、SQLite，也不引用 `Pry.Core.Runtime`。

## 2. Character State

三个类型分别承载三类事实，任何一条都不写进另外两条：

- `IdentitySnapshot`：稳定锚点（名称、核心身份、人格轮廓、说话风格种子、从行为规则里分出的"底线"、基线心情）。`WorldFacts` 属于开放世界解释，**不在** Identity 里。学习链路没有任何方法能写它。
- `RelationshipState`：`Familiarity / Trust / Closeness / SharedContextStrength` 四个信号按各自的速率变化（信任涨得慢、跌得快；长时间不联系只削弱 SharedContext 与 Closeness）。派生的是内部闸门：`AllowsTeasing`、`AllowsAffectionateAddress`、`ExpressionGate`、`ProactiveWillingness`。没有好感度等级，没有"到某个分数解锁某种关系"的逻辑。
- `MoodState`：`Valence / Arousal / Interest / Fatigue / Irritation`，按 45 分钟半衰期回落到基线，只保留轻量结论（`SupportsPlayfulRegister`、`PrefersDisengagement`、`LengthBudget`）。

**用户情绪 ≠ 角色情绪。** `MoodState` 没有任何字段可以存放用户状态（有测试断言属性名里不含 `User`）。外部判断只能以 `UserAffectEstimate` 进入 `CharacterStateService.ReactToUserAffectAsync`，该入口在置信度 < 0.5 时直接丢弃，命中时也只施加一次有界增量（|ΔValence| ≤ 0.08），并且从不改 Trust/Familiarity。

`CharacterStateService` 一个实例对应一个 User × Character，跨会话共享；会话内的消息列表不属于它。持久化形式是扁平的 `CharacterStateRecord`（带 `StateVersion`），仓储必须拒绝版本落后的写入。

## 3. Behavior

`BehaviorPlanner.Plan(situation, state, relevantPatterns)` 是纯函数、确定性、无模型调用。理由有两条：本地 2B 模型不可信地承担闸门判断；固定 Eval 与 Developer Mode 需要可复现的解释。

顺序固定为：读场景 → 选基线意图 → 让已有经验参与（只改置信度或在低置信时改策略）→ 闸门 → 语气 → 口癖许可 → 提问预算 → 长度预算。闸门永远在经验之后执行，所以学来的模式**不可能**越过关系或情绪限制。

意图集合就是计划书 §23 的那 12 个，包括 `DoNothing` 和 `UseCapability` 占位。

关键闸门（稳定标识写在 `BehaviorDecision.Gates` 里，供 Developer Mode 直接展示）：

| 闸门 | 触发条件 | 结果 |
| --- | --- | --- |
| `runtime:reply_required` | Runtime 声明必须回应 | 取消 `DoNothing`，改最短承接 |
| `relationship:tease_locked` | 未到 Familiar 或近期有边界摩擦 | `Tease` → `Acknowledge`（接住玩笑但不还手） |
| `mood:playful_unavailable` / `mood:low_energy` | 烦、累、低落 | 收起玩笑与展开性提问 |
| `behavior:question_streak` | 连续两轮以问句结尾 | `QuestionBudget = 0` |
| `behavior:ambiguity_blocking` | 短指代 + 无上文 + 无记忆 + 没在追问中 | 只澄清一次 |
| `capability:none_registered` | 请求动作但没有能力登记 | 退回 `ContinueTopic` |
| `memory:unavailable_do_not_claim_no_history` | 记忆查询降级 | 禁止"你没提过"式否认 |

`DoNothing` 的三条常见路径：刚回过话、用户连发低信息量碎片、以及"这个用户还不熟到值得为一句'草'开口"。

## 4. 经验与学习

`BehaviorExperience` 是逐条追加的原始记录；`BehaviorPattern` 是从中折叠出来的 `(场景, 策略) → 置信度`。二者都只经仓储接口流动。

刻意做小的部分：Outcome 缺失或含糊时记为 `InteractionOutcome.Unknown`，**不参与任何加权**；单次观察只把置信度移动 0.05，边界 `[0.05, 0.85]`；模式至少要 3 次观察才有资格影响 Planner（`MinimumObservationsToInfluence`），且影响方式是同分时选边和给出理由，不是放行被闸门挡住的行为。未投递（取消、被替换、stale）的输出在 `CompleteTurnAsync` 第一行就返回，不进经验、不计使用、不入学习队列。〔Character Intelligence 附注，2026-10-16（`DEF-P3SEAM-04`）：**"进了账本"与"她真的说了话"自此是同一个判定**——上一句只挡住"没投递"，而模型没兑现出正文时投出去的那一条是占位符 `……`，两个字符照样过得了宿主那道纯长度闸，于是一个她一个字没说上的轮次被记进三本账（一次交流＋关系与 Mood、一行 `behavior_experience`、本轮口癖记为"用过一次"）。修法不是再比一次字符串：新 `src/Pry.Core/TurnTaking/VisibleReply.cs` 是"这一轮有没有可见答复"的**唯一**定义处（公开的最小判定口 `HasVisibleContent(IEnumerable<string>)` ＋ `internal const string Unanswered`），`ReplyPlanner` 的回落引用同一个常量，`ModelReplyShape.Empty`／`OnlyInternalDraft` 兑现到界面上的也正是这一条 ⇒ 可见侧与学习侧同一个出口、同一档判定，与 `DEF-P1WALK-07` 共用判据而不分叉；网关只调用它，Core 之外不出现占位文本的字面量（这条有源码门守着，含"扫描器看得见自己声称禁止的泄漏"的正向探针）。**已知代价，写在这里而不是藏在代码注释里**：她若真的一字不差只回两个点，这一轮按沉默处理、一个字都不教——与认可入口那把刀对 `ShapesIn("……")` 判 `NothingToLearn` 是同一条读法。**这道闸不缩短"没投递"那半边**：`RecordDeliveredBubble`（`CharacterIntelligenceGateway.cs:170`）早就把 null 与空白挡在账本外，所以新判定为假的那一条必然覆盖旧纯长度为真的那一支。〕

只有**明确针对该行为**的反馈才会折叠：`FeedbackIsExplicit` 为真且 `FollowUpEvidence` 非空。`Continued / TopicShiftedAway / Silenced` 这类含糊参与没有信号值——继续聊天可能只是礼貌，换话题可能只是他本来就想换，把它们当负向或正向反馈都会让角色把自己的猜测当事实。它们仍然逐条进 `BehaviorExperience`，只是不动权重；`HasEvidence` 按 Success + Failure 计数而不是"用过几次"，所以一个从未被评价过的策略不会伪装成有证据。

投递与事后反馈是两次不同的调用：`CompleteTurnAsync` 只在消息真正送达时调用一次（计一次交流、更新关系与 Mood、登记表达使用），之后的真实用户评价走 `ObserveFeedbackAsync`，它不重复计数也不重复登记使用；两者都没有新证据可学时后者直接返回，不会往经验表里塞重复行。同一 `(场景, 策略)` 在一个 Turn 内只折叠一次（`EvaluatedTurnKeys` 回放保护），并发更新同一仓储时按仓储串行。

`EvaluatedTurnKeys` 与表达侧的 `AcceptedEvidenceIds / AcceptedTurnKeys` 都是**滑窗**（各 64 条）：它们只用于防回放，超过窗口的旧 Turn 不可能再被投递一次，而无限增长的列表会变成随聊天史膨胀的持久化负担。

## 5. Expression

`ExpressionPattern` 保持计划书 §27 的最小字段（`Situation / Style / Confidence / UsageCount / 时间戳`），另加学习安全性所需的少量字段：`EvidenceCount`（有多少条独立明确认可支持它）、`Tone`（这条说法听起来是什么语气）、`Suppressed`，以及两份认可证据引用。默认不存原文、不存 `SourceMessageIds`，避免把私人聊天复制进表达库。

`Channel / RelationshipStage / EmotionGate / Intent / Avoid / Evidence` 仍是 FUTURE backlog，本轮未实现。"严肃场景不许玩梗"这件事由 Behavior 侧的 `HabitToneAllowed` 与 Selector 共同完成，没有为此搭规则引擎。

召回上限 3 条，选择上限 1 条，且允许 0 条。Selector 的拒绝原因同样返回给调用方：`low_confidence`、`tone_blocked`、`mood_blocked`、`relationship_blocked`、`cooldown`（默认 12 分钟）、`already_recent`（她自己最近三条回复里刚用过）、`selector:budget_1`。

表达学习只跑在后台：`TryEnqueue` 只入队（队列满就丢弃并计数，绝不阻塞、绝不抛），抽取与合并发生在宿主调用 `DrainAsync()` 的时候；`DrainAsync` 自身串行，写库失败前不清 `Dirty`，因此下一次 drain 即使没有新消息也能重试，同一条认可不会因重试而重复加权。准入条件：

- 只从**用户真正收到的角色输出**里抽取，因此用户偶然一句脏话没有直接入口；模型把脏话学舌出来的情况由 `IsCrude` 拦截。
- 只收短句形（≤10 字、无数字、无 URL、无单位），所以成句的事实断言与"客服清单式回答"不会变成习惯。
- **她自己反复说同一句式只形成候选，不构成晋升理由**：晋升需要至少 2 条互相独立的明确认可（不同反馈引用 + 不同 Turn，`RejectedUnconfirmed`）。置信度由认可条数决定，不由 drain 次数决定；认可数达到上限前每轮最多 +0.03，上限 0.8。
- 同一 Turn 里用户自己也说过这句，则要求再多一次独立出现（`RejectedUserEcho`）。
- 客服腔、身份声明、一次性梗、用户明确说"别这么说话"分别对应 `RejectedBoilerplate / RejectedPersonaConflict / RejectedInsufficientEvidence / RejectedExplicitCorrection`。
- 明确拒绝写成**持久抑制**：规范化写法相同但 `Tone` 不同的变体一并压制，尚未入库的拒绝项写成零置信度占位；换一个新学习器、或之后又攒到正面样本，都不会自动复活。
- 命中同一 `Tone|规范化写法` 的候选走合并而不是新增（`Merged`）；认可证据集合没有变化时返回 `Unchanged`，不写库。
- 学习器跟踪的候选形数量有上限（512，超出时清掉最久未出现且已提交过的），因为一个学习器和进程同寿命。

## 6. 交给 Context Builder 的产物

`ReplyGenerationContext` 是本轮的最终输出：意图、语气、口癖许可、提问预算、相对长度、可用/禁用表达、关系闸门、记忆降级标记，以及一组 `Avoidances`（不重复用户原话、这一轮不问、不用客服套话、不许声称对方从没提过）。

`ReplyGenerationRenderer.Render(context, identity, budgetCharacters)` 把这一份投影成**有预算的文本块**：必备行（意图、避免项）先落地，可选行（心情、关系描述、底线、表达参考）在超出预算时被丢弃。关系一律渲染成中文描述而不是数值——内部信号不进 Prompt，也不进 UI。〔2026-09-28 补，作者令"取消亲密值之类的数值系统"，同日追加"要的，把这条尾巴收掉"：这条不变量此前只守住了本域渲染那半边，**既存**的 `PromptBuilder` 把角色卡存档自带的 `RuntimeState.Trust`／`Familiarity`（0–100 整数，旧卡片格式）直接写成"信任：50/100；熟悉度：10/100"塞进每一条系统提示词，已删该行；**同轮把那两个字段本体也从 `RuntimeState` 删掉了**（`Pry.Contracts/CommonContracts.cs`），内置卡资产 `src/Pry.Resources/character.json` 里的 `"trust": 50, "familiarity": 10` 两个键一并清掉，线格式因此少两项——`ContractBoundaryTests` 的期望值已同步，删字段前写的旧卡片 JSON 仍读得进来（`JsonSerializerDefaults.Web` 忽略多余成员，这条兼容性有判据守着，不是假设）。三面各有一条判据：提示词里不得出现数值亲密（`The_shipped_card_prompt_never_hands_the_model_a_numeric_affinity`）、卡片类型上不得再有 int/double/float 属性（`The_card_format_carries_no_numeric_affinity_field_to_leak`）、删字段之前存的卡照样打得开（`A_card_saved_while_the_affinity_fields_existed_still_loads_and_still_shows_no_numbers`）。设置界面从来没有这类控件，本条不新增任何承诺，只把"内部信号不进 Prompt"的适用范围从本域渲染扩到卡片自带状态，并把源头也一起关掉。〕

它只是一个投影，不是第二个 Prompt 拼装入口。RT-002/RT-005 的 Context Builder 只需调用一次，把返回的文本并入现有 `PromptBuilder` 结果；本轮没有替它接线。

## 7. 固定 Eval 对应

`tests/Pry.Core.Tests/CharacterIntelligenceTests.cs`（Behavior/State/Learning）与 `ExpressionSystemTests.cs`（Expression 检索、Selector、学习准入）与 `LearningSafetyTests.cs`（自我强化、纠正后复活、后台重试、反馈归因）共 79 项，覆盖计划书 §7.1 的 Behavior/Expression 条目：不过度提问、不重复原话、不客服化、心情差但没求助时不过度安慰、关系浅时不过界、熟悉后允许自然玩笑、学到口癖不会每句都用、严肃场景抑制玩笑口癖、旧梗可召回但不强行、纠正关系认知后状态真的变化、单次异常互动不永久改变人格、空结果与查询失败可区分、取消的输出不进入学习、含糊反馈不加权、明确要求停止后不因正面样本复活。

两个测试文件都只断言决策与闸门，不断言措辞，因此换模型不会让它们失效。

## 8. 尚未完成（如实登记）

- **未接线**：`Pry.Core/Runtime`（RT-002）目前自行引用了 `Pry.Core.Intelligence.ResponseIntent`。该目录与我的目录都是未跟踪状态，任一方单独提交都会让另一方编译失败——集成时必须同一提交链落地，或由 Runtime 负责人确认引用关系。
- **与 MEM-005 收敛**：`RelevantMemoryView` / `MemoryObservation` 是过渡投影，M1 之后必须删掉。归属已定（MEM-005 R1）：`MemoryQueryOutcome` 等跨模块记忆契约的唯一定居处是 `Pry.Core/Abstractions`，本文件与 `Intelligence/SituationSnapshot.cs` 里的声明是先行占位，由我在与 Memory Owner 同批的提交中删除并改 `using Pry.Core.Abstractions`——不提前搬，以免出现第二个定义点。
- **持久化**：`ICharacterStateRepository / IBehaviorPatternRepository / IExpressionPatternRepository / IBehaviorExperienceRepository` 只有进程内实现。SQLite 实现、索引与 schema 迁移属于 Persistence Owner（schema 编号的分配权归 PLAT-003，本角色不占号）。**契约面自 2026-09-28 起是判据而不是段落**：`tests/Pry.Core.Tests/IntelligenceRepositoryConformanceTests.cs` 把下面每一条写成 22 条用例，抽象基类 + 一个 in-memory 派生类；Persistence Owner 落库时只需新增一个派生类（`NewStores()` 开真库、`FlushAsync()` 关掉再重开模拟重启），**一条断言都不许为了变绿而放宽**。〔Core Platform 附注，2026-09-28（`DEF-P3STATE-01`，只登记状态、不改写本段判据）：这一句已经成立——真库派生类是 `tests/Pry.Core.Tests/SqliteIntelligenceRepositoryConformanceTests.cs`，22 条继承断言一字未改、一字未放宽；schema 由 v9 追加到 v10，新增 `character_state`、`behavior_pattern` + `behavior_pattern_turn_key`、`expression_pattern` + `expression_pattern_evidence` / `expression_pattern_turn_key`、`behavior_experience` 共五张表三条索引，四个仓储适配器与全部 SQL 都留在 `Pry.Core/Memory`（`IntelligenceRepositories.cs`、`MemoryDatabase.Intelligence.cs`），`Pry.Core.Intelligence` 与 `Pry.Core.Expression` 依旧不认识 SQLite。本附注只覆盖"存储与判据"这一半边；下一段的 purge **调用点**仍未接，归属不变。〕已由命名用例守住的四条：
  - `ExpressionPattern.Suppressed`（含 `Confidence = 0` 的占位记录）必须原样往返——丢了就等于用户明确的"别这么说话"只生效到下次重启（`Suppression_survives_a_reload`）。
  - `BehaviorPattern.EvaluatedTurnKeys` 与 `ExpressionPattern.AcceptedEvidenceIds / AcceptedTurnKeys` 必须**按原顺序**往返，仓储不得排序、去重或截断（滑窗裁剪归模型侧，`AcceptedEvidenceWindow = 64`）——乱序或去重会让同一 Turn 的反馈被重复折叠（`Replay_guards_survive_a_reload_in_order`、`Accepted_evidence_references_survive_a_reload_in_order`）。
  - `BehaviorExperience.FeedbackIsExplicit` 与 `FollowUpEvidence` 必须可分，含糊反馈一旦被读成明确反馈就会把礼貌的继续聊天当成加权证据；`FollowUpEvidence` 存**反馈引用的 ID**，不复制聊天正文（`Explicit_and_ambiguous_feedback_stay_distinguishable_after_a_reload`）。
  - 整字段往返：状态 20 个字段、行为模式与表达模式各自的每一个字段都逐个断言（`Every_state_field_survives_a_reload` 等三条），"忘记写一列"这种形状必须当场变红。
- **读取语义（本轮新增，两个实现必须一致）**：`ListForCharacterAsync` 排序为 `updated_at DESC`，同分再按主键**升序**（ordinal）；经验日志按 `observed_at DESC`，同刻按**后追加者在前**，等价于 `ORDER BY observed_at DESC, id DESC`。仓储交出来的对象必须是脱离副本——改一个已交出的对象不得改写库里的状态，两次读取也不得共享同一个证据列表实例（两条 `An_object_handed_out_by_*_read_cannot_rewrite_the_store`）。这一条过去只有 in-memory 实现会"碰巧"和真库一致，现在是断言；`DEF-P2PLAN-01` 记的就是同一个教训：fake 与真实存储判定不同时，只跑 fake 是假绿。
- **删除角色时的清空（契约、落库、调用点三面都已落地）**：四个仓储各有一条 `PurgeAsync`，返回清掉的条数；`ICharacterStateRepository.PurgeAsync(characterId)` 清该角色名下**所有用户**的关系与 Mood。目的只有一个：删掉角色卡之后学习状态不能留在库里 indefinitely（`Purging_*` 四条断言"只清这一份、别的不许动"）。两头曾经的状态：① SQLite 实现；② 调用点——`ConfigurationApplicationService.DeleteCharacterAsync` 过去只查 `conversations`/`memories` 引用，学习状态既不清也不挡。③ 键名要对上：状态仓储按 `characterId` 存，行为/表达/经验按宿主提供的 `characterInstanceId` 存，两者若被宿主写成不同字符串，purge 会漏——这是设计上的已知脆弱点，不是实现细节。〔Core Platform 附注，2026-09-28（`DEF-P3STATE-01`）：① 已落地——四个仓储各有真库 `PurgeAsync`，`Purging_*` 四条断言在 SQLite 派生类下全绿，返回值只数模式／表达／经验本身的行数（守卫子表随 `ON DELETE CASCADE` 一起走，不混进计数）；② 明确不在本刀范围，调用点仍归 Character Intelligence 与真实仓储接线同批落地；③ 键名脆弱点仍在，落库侧只能保证"同一个字符串落进同一个键"，替不了宿主选 id。〕〔Character Intelligence 附注，2026-09-28（缺口 ② 调用点，同批把 ③ 定成前提）：② 落地——`src/Pry.Core/Intelligence/CharacterLearningCleanup.cs` 是四张表作用域的唯一翻译处，`Program.cs` 首次注册四个适配器，`DeleteCharacterAsync` 调用它。三条判断写在这里而不是散在调用方：**学习状态不进那条 EXISTS**（它一旦存在就永远存在，加进引用判定等于把角色卡变成删不掉，处理方式是跟着卡片走、不是拦住删除）；**清空必须落在"卡片还能原样退回去"那一段**（四条删除各自是一个事务，挪过 `ReconfigureAsync` 那条线再失败时，回滚只把 JSON 放回磁盘、内存角色表已不含她，删除既报错又不可重试，剩下的行从此无人能清——`QA-P3STATE-03` 的终态就是这个）；**③ 自此是需要被维持的前提**（1.0 宿主拿卡片 id 当实例 id，见 `ConversationSessionService` 传 `character.Id`；一卡多实例的形状出现时，`PurgeAsync` 必须改成收该卡全部实例 id，否则调用点会静默漏掉后建的实例）。判据 6 例：`CharacterLearningCleanupTests` 3 条（清四边／只清这一份／空账可重复清）与 `CharacterLearningDeletionTests` 3 条跑真库真删卡链（清干净且别张卡原样、被拒的删除一个字不动库、失败把卡片还回来并可重试）；四处突变逐个反证后逐字节还原。仍未接且不属于本判据：往这四张表写东西的生产者（双向接缝归 RT-002/RT-003 联合评审），所以今天真机里它们是空的，本附注不主张任何"学到的东西活过重启"。〕
- **版本乐观并发**：`StateVersion` 落后即拒绝（写入等于当前版本允许，保证重试幂等），真实仓储必须保持同等语义（`A_write_behind_the_stored_state_version_is_rejected`、`Writing_the_same_state_version_again_is_allowed`）。RT-003 "Relationship 可持久化"的验收：存储面自 2026-09-28 起齐全（schema v10 五张表 + 四个适配器 + 删卡清空），**剩下的不是落库而是生产者**——今天没有任何一条生产代码往这四张表写状态，所以这一项仍只算部分完成，改口的依据是接线落地而不是表建好。〔Character Intelligence 附注，2026-09-30（生产者接缝落地，作者派的 RT-002 合刀）：宿主现在在说话前问一次、投递后报一次，两处都在 `src/Pry.Api/Runtime/CharacterIntelligenceGateway.cs` 这一个类里，由 `ConversationSessionService.CreateAsync` 注入（终局那一路是 `IRuntimeOutputSink` 的内侧装饰器，不是第二条外发通道；对线结果写在 `docs/phase2-runtime-contract.md` §7.2）。**上一段那句"今天没有任何一条生产代码往这四张表写状态"自此过期，但只过期一半**：`character_state`（每个完成轮 +1 次互动）与 `behavior_experience`（每个非沉默的完成轮一条，Outcome=Unknown ⇒ 照记不加权）从今天起有生产者；`behavior_pattern` 与 `expression_pattern` **仍然是空的，而且这是设计结果不是缺陷**——折叠需要明确反馈（`FeedbackIsExplicit`），而第三个调用点 `ObserveFeedbackAsync` 本刀没接。判据 10 例：`tests/Pry.Api.Tests/CharacterIntelligenceSeamTests.cs`（真 SQLite、真网关、真生成端口；四条"没投递就不许教"的形状、被点名/被提问不许沉默、渲染块走到模型跟前且一个数字都没有、两会话共用一本账且气泡不认错轮次、回落逐字等于接线前的形状、源码门守两处接线），11 处突变逐个证伪后逐字节还原。〕〔Character Intelligence 附注，2026-10-12（第 0 层 `0-2`：守门人 09-30 签结 §7.3 时登记的三条接缝缺陷 `-01`／`-02`／`-03` 同刀修掉，产品代码只动 `CharacterIntelligenceGateway.cs`）：**上一段那句"气泡不认错轮次"自此要按新形状读**——网关注册的是"该会话恰好一轮在飞才记，认不出唯一就一个字都不记"，而它原来那句理由（"围栏只认最新一轮"）经核对**不成立**（围栏比 epoch，成轮不抬 epoch），真实承重的是另外三道闸（`ReplyGate` 在 `UserTyping`／`PendingOutputCount > 0` 时先 `Defer`、`TryFormTurnLocked` 在 `_active` 非空时直接返回、主动候选同因被拒），这三道闸的顺序自此由新文件 `tests/Pry.Core.Tests/SingleInFlightTurnBoundaryTests.cs` 三条判据钉住（两条断言建议器**被问的次数**为 0，一条正向探针"队列清完后恰好被问一次"）。另外两条形状同时改掉：账本 **first-write-wins**（一轮过两到三次闸，她学的自此必须是模型 `:721` 真读过的那一份而非复校那一份；每轮咨询次数已在 `DecideGateAsync` 的 `ReplyGate` trace 里，不另记一本），以及**首轮建状态域失败不再永久占住 `_scopes`**（逐出已结算且非 `RanToCompletion` 的那一笔、不就地重建：本轮仍由 fail-open 把话说完，下一轮自然恢复；`RestoreAsync` 照旧认取消）。判据计数更正：该文件 **10 例 → 13 例**，加 Core 侧新文件 3 条 ＝ 本刀 6 条，门禁总数自 `6f47f34` 的 763 升到 **769**（连续两次分项逐位相同、`measure` 各 exit=0、`verify-repository` 418）；四次突变逐个反证、md5 逐字节还原，其中第一次的 `ReplyGate` 突变写法（把两道闸移到 context 构造之后、`AdviseAsync` 之前）对判据不敏感、全绿，已排除出反证样本后重做。**本附注不主张的范围**：`DEF-P3SEAM-04`（占位符被 `ReportTurnEndAsync` 当作"已投递、有正文"记进账本）作者已明令不得与 `-02`／`-03` 合并处置，`delivered.Length > 0` 那道判据一字未动；`:870` 的 Capability 续路（compose 用 `advice: null` 而账本记着一份决策）随 RT-007 才可达；缺口 ④ 照旧开着——她学到的口癖仍然没有生产者，这两层差别不因本刀改变。状态词：已落地主工作区、未提交，判定归 QA 从可指认提交复跑。〕〔Character Intelligence 更正，2026-10-16（`DEF-P3SEAM-04` 落地，按 `docs/dispatch-p3seam04-placeholder-turn.md` 开工）：本条里那句"**每个完成轮 +1 次互动**"自此要按新形状读——进账的条件不再是"这一轮投递成功了"，而是"这一轮她真的说了话"，判定只在 `VisibleReply.HasVisibleContent` 一处（形状与代价见 §4 那条 10-16 附注）。当年明令"不得与 `-02`／`-03` 合并处置、`delivered.Length > 0` 一字未动"的这条缺陷自此单独成刀修掉，`-01`／`-02`／`-03` 的既有判据一条未改、13 例仍全绿。历史错账（这一刀之前已经记进去的那几轮）不在本刀的处置面：口径三选一已由作者 2026-10-18 落笔为**甲——历史行一字不动，只保证今后不再记**，理由写在这里而不是只在提交信息里：乙（按轮次回退计数）需要"哪几轮当初显示的是占位符"这个信息，而那些轮次在库里**没有任何标记**，硬猜会误伤真实对话，且清理不可逆；丙（清空学习与关系状态）与"靠慢慢相处打磨"的裁定正面冲突。偏差的量级留给第 6 层标定轮去观测，不是被抹掉。⇒ 本文件与任何文档都不许把这几轮写进"她学到的东西"的因果里，也不许为它新增任何一次性迁移代码。〕
- **表达学习的排空者还不存在（缺口 ④，2026-09-30 新登记）**：`ExpressionLearner.DrainAsync` 在 `src/` 内零调用点，而接缝落地后 `CompleteTurnAsync` 每一轮都往观察队列 `TryEnqueue` 一条。队列上限是 `QueueCapacity = 64`，满了就开始丢——所以**学到的口癖今天依然进不了 `expression_pattern`**，且这个形状在 64 轮之前不会有任何报错迹象。这不是"参数没标定"，是"没人把队列排空"，修法是宿主侧一条后台通道（timer 或 channel），归 RT-004 后台 lane，需要作者派工；在此之前本文件不许出现"她会学说话方式"这类读法。〔作者附注，2026-09-30：已裁"派成下一张独立小工单" ⇒ 台账编号 `RT-004-DRAIN`（`PROJECT_PLAN.md` §4），**开工前置是接缝那一刀先形成可指认提交**，两刀叠在同一个未提交面上会让两轮读数互相顶掉（§5.0 判据 4）。工单自带四条反向约束：不为变绿放宽滑窗与按序往返、不做成同步等待可见回复、不夺走 Selector"这次一个都不用"的许可、两条既有学习安全判据一字不改。〕〔Character Intelligence 附注，2026-10-14（`RT-004-DRAIN` 落地，作者按 `docs/dispatch-rt004-drain.md` 派工）：**"没有排空者"这半边自此关闭**——新 `src/Pry.Api/Services/ExpressionDrainService.cs`（`BackgroundService` + `PeriodicTimer`，默认 15 秒、`Pry:ExpressionDrainMs` 夹在 25 毫秒到 600 秒、单次排空 5 秒预算、逐卡 try/catch，一张卡坏不掉整条通道也不带下宿主），`Program.cs` 自此注册它；枚举口开在网关的 `ActiveLearners()`（学习器一卡一份，只交已经建好的状态域、不等还在建的那一笔），**Core 一字未改**，工单 §3.6 的"理想形态"达成。到点取消与写库失败都不丢工作：脏标记留在原处，下一次 tick 不必用户再说一遍。**但上一段末句那条禁令不解除，理由换了**：工单 §1 的前提"缺的只有谁来排空"经核对不完整——`CommitAsync` 要新建一行需要 `acceptedCount ≥ 2`，而这个计数只可能来自 `DeliveredTurn.AcceptedStyles` 与非空的 `FeedbackEvidenceId`，二者在 `src/` 内**没有任何生产者**（`TurnOutcome.AcceptedExpressionStyles` 全仓只出现在 Core 的定义与 `EnqueueExpressionObservation` 的透传里，`ObserveFeedbackAsync` 仍是零调用点，Api／Client／App 没有反馈入口）。因此真机今天仍然不会长出 `source='Learned'` 行：本刀解除的是"满 64 静默丢弃"和"学到的东西没有落库通道"，**不是**"她能学到你点头的说法"。后者登记为新缺陷 `DEF-P4LEARN-01`（审批侧生产者，Owner＝Character Intelligence，解除条件＝RT-006 反馈入口接线；工单 §3.7 明令本刀不许顺手接它）。判据 10 例 `tests/Pry.Api.Tests/ExpressionDrainChannelTests.cs`：真 SQLite + 真网关 + 真投递路径、服务由宿主 `StartAsync` 起来（没有一条判据手调 handler），覆盖 §2 的八条完成标准——两条独立轮次的明确认可跨两个会话落成一行且被检索到、80 轮投递 `DroppedCount` 仍为 0、存储卡在排空里也不延迟可见回复、首次写失败下一 tick 自己补上、退出打断不丢队列、一张卡的存储异常不拦通道、明确纠正之后每个 tick 都不复活它、重启不重复加权；唯一的替身是"认可"本身（`Approve` 直接把带 `AcceptedStyles` 的 `DeliveredTurn` 推给活着的 Learner），这一点写在类注释里，不外推成真机行为。反证跑出一个**判据自身的缺陷**并当场改掉：源码门原先按 token 出现次数判"注册在不在"，一条被注释掉的 `// builder.Services.AddHostedService<ExpressionDrainService>()` 留着同样的字符串 ⇒ 假绿，现改为只数未注释的行（M2 当场验证：注释掉注册 ⇒ 只有该门转红）。四次突变逐个点名转红、md5 逐字节还原：M1 `ActiveLearners()` 返空 ⇒ 八条行为判据全红而两条源码门照绿；M2 注册注释掉 ⇒ 只有源码门红；M3 把排空搬进 `ReportTurnEndAsync` ⇒ "不延迟可见回复"那条与源码门同时红；M4 去掉逐卡 catch ⇒ 通道存活与失败重试两条红。门禁：Release 0 警告 0 错误，解决方案级 **819 / 0 / 2 连续两次**（Client 93／App 183／Api 154→**164**／Core 379+2skip，两次分项逐位相同），`measure-fixed-eval` 两目录各 exit=0、目录仍是 **75 条**永久场景（本刀加的是判据不是场景），`verify-repository` 431 个受跟踪文件通过，`git diff --check` 干净。状态词：已落地主工作区、未提交，判定归 QA 从可指认提交复跑，§5.0 指针不动。〕〔Character Intelligence 附注，2026-10-15（`DEF-P4LEARN-01` 认可入口落地，作者当日原话"按照QA反馈的开工"，入口形状按作者 10-15 裁定）：**上一段那句"真机今天仍然不会长出 `source='Learned'` 行"自此过期，缺口 ④ 的两半边合上了**，而合上它的不是任何自动推断，是**用户按下的那一下**。三件事同时落地：① **界面**——她某条回复的右键菜单里多出"就是这样／别这样说"两项，这两项**只在高级开关打开时存在**（`UserPreferences.ExplicitExpressionFeedback`，默认关：作者裁的是"靠慢慢相处打磨"，手动干预是给需要它的人的准备项），且只出现在**她这轮真的生成过**的气泡上——脚本预设与主动消息没有可归属的一次投递，把它们做成可点的按钮等于诱使用户去认可一次没发生过的说话；关掉开关不撤销已经学到的东西，只关掉入口。② **后端**——新 `src/Pry.Api/Services/ExpressionFeedbackService.cs` ＋ `POST /api/v1/conversations/{id}/messages/{messageId}/expression-feedback`（DTO 在 `Pry.Contracts`；五种拒法各有各的错误码与状态：`403 expression_feedback_disabled`／`404 expression_feedback_message_missing`／`409 expression_feedback_not_attributable`／`409 expression_feedback_wrong_conversation`／`503 expression_feedback_queue_full`，再加上 `messageId` 非正数那一种属于请求本身的 `400 validation_error`；**开关检查排在归属解析之前**，所以关掉时用户听到的是"入口今天不存在"而不是"猜你点错了"；回执只有一个布尔方向的结果文本，没有计数、档位与置信度）。③ **归属**——网关内新增进程内投递账本（`ReceiptLimit = 64`，`messageId == messages.rowid == ChatMessage.Id`，客户端手里唯一持有的就是这个 id），认不出这一条就 **409 明确拒**，绝不回退成"按最近一轮归属"——那正是 `DEF-P3SEAM-02` 花一整刀拆掉的形状。**`FeedbackIsExplicit` 与 `ExpressionFeedbackId` 的语义自此定死：唯一生产者是那次点击**；投递路径照旧带空 `FeedbackEvidenceId`，`ExpressionPattern.Confirmed` 那道过滤是第二重保险。学习门槛一字未动：`MinimumEvidence = 2`、`AcceptedEvidenceIds`／`AcceptedTurnKeys` 按序往返、同一条气泡连点三次仍只算一笔（`turnKey` 幂等）。**Core 本刀只加一个读口** `ExpressionLearner.ShapesIn`（回答"这句里有哪些形状"，不判断态度、不新增判断通道），`ExtractShapes` 本体、滑窗、Selector"这次可以一个都不用"的许可全部未动；新行置信度 `0.35` 恰等于 `ExpressionSelector.minimumConfidence` 地板，所以本刀**不需要、也不声称任何新标定**。表态请求自己只往队列放，取走仍只由 `ExpressionDrainService` 做；回放那一笔把 `RecentMessages` 清空、`InteractionOutcome` 记 `Unknown`，所以一次表态不重复加交流次数、不动关系与 Mood。**上一段那条"不许出现'她会学说话方式'的读法"的禁令，其解除条件（工单 §2.1 那条真机判据真的绿）已达成**：`Two_explicit_approvals_on_two_turns_become_a_learned_row_that_survives_a_restart` 跑真 SQLite ＋ 真网关 ＋ 真投递路径 ＋ 真后台排空，两次不同轮次的明确点头长成一行 `Learned`（`EvidenceCount = 2`、两个不同 `turnKey`、置信度 0.35、未压制），检索侧 `ExpressionRetriever` 取得到它；换一个新网关（新状态域、新学习器、投递账本清空）后那一行还在、证据列表逐字相同——活着的那一行只可能来自库。**允许的读法到此为止**："用户明确按两下能教成一句，教成的东西活过重启"。**仍然不许写成"她越聊越懂你"**：默认开关关着、没有任何自动推断、"会用"只到检索候选为止。判据 22 例：`tests/Pry.Api.Tests/ExplicitExpressionFeedbackTests.cs` 12（端到端：两下教成一句并活过重启／不点就永远学不到／连点三次只算一次／"别这样说"压下去之后点不回来／开关关了就把一次本来成立的分析也拒掉且当场打开就立即可用不需重启／对不上就拒不挂到最近一轮／跨会话不认／她没真说话的气泡教不出／非编号是请求错误不是找不到／慢仓储卡住时点击与下一条回复照旧走得完／开关走既有偏好往返且省略字段即保留原值）＋ `ExpressionFeedbackEndpointTests.cs` 3（端点状态码与错误码一族）＋ `tests/Pry.App.Tests/ExpressionFeedbackSurfaceTests.cs` 7（界面与线契约的源码门：两项只在开关后面／开关是设置页那一条既有往返／只有点击能标显式／归属拒而不猜且不新增出门口／路由是一个字符串且回执不带数字／表态不在请求里排空队列／扫描器看得见它自称看见的泄漏）。反证 10 处突变逐个点名转红、`cp`+`cmp` 字节级还原后复跑：M1b 生产者把每次投递标成认可 ⇒ §2.2 那条 1/1 红；M2 点击不再标显式、M5 菜单项漏出开关、M7 回执带出置信度、M8 设置保存丢字段、M9 后端把开关报成关 ⇒ 各红自己那条；M3 归属回退到最新凭据 ⇒ 3 红（含端点族与源码门）；M4 开关不再把关 ⇒ 2 红；M6 把排空搬进请求 ⇒ 行为判据与源码门同红。**两处如实登记，都不是产品结论**：其一，**M1（把 Core 的三条件认可门削成"提取到就算"）并没有打开"不点也学"那个洞**——空 `FeedbackEvidenceId` 被 `Confirmed` 挡住，它红的是证据配对那两条 Core 判据（`A_late_approval_still_counts_after_the_occurrence_tally_saturates`、`Multiple_feedback_events_for_one_turn_are_not_independent_turns`）加两条本刀端到端判据；这道双保险是实测出来的，不是推理出来的，也正因此"不点就永远学不到"那一洞的正确突变是 M1b。其二，**M6 第一次跑时判据不红而是把测试进程挂住**——问题在仪器不在产品：那条判据原先只给第三次点击设预算，而排空一旦搬进请求，第一次点击就永久阻塞在替身"第一次写库不返回"上。改的是仪器（三次点击共用同一个 2 秒预算；等行落库的预算从 8 秒放到 30 秒，因为同一台机器上别人会并行跑整解决方案门禁，8 秒会在负载下先红在仪器上），两条时限断言本体一字未放宽；改完 M6 恰好 2/2 红。门禁读数取自干净 `--detach` 载体＝线尾 `dd91d2c`（界面层 `0-5` 那一刀，父 `f6c9750`）＋ 本刀 16 个文件：`dotnet build -c Release -t:Rebuild` 0 警告 0 错误，解决方案级 **876／0／2 连续两次**（Client 93／App 221／Api 179／Core 383+2skip，两次分项逐位相同；本刀新增的是 App 7 ＋ Api 15 ＝ 22 例），`scripts/measure-fixed-eval.ps1` 两个结果目录各 `exit=0`、目录仍是 **75 条**永久场景（加的是判据不是场景），`verify-repository` 449 个受跟踪文件通过，`git diff --check` 干净。**为什么引用时必须带分项**：本刀早先在内容相同、基座为 `f6c9750` 的载体上采到 854／0／2（App 199／Api 179），与 `0-5` 那刀的 854（App 214／Api 164）**同数不同项**——两条 lane 各加 22 例撞出来的巧合同名，只写总数就会把别人的账顶掉（§5.0 判据 4），故本附注一律写全四项。**本刀不解除、也不顺带做的**：`2-D` 先接线后标定门、第 6 层标定轮、`DEF-P3SEAM-04`（占位符仍被记成一次"已投递、有正文"的可学互动；`ExtractShapes("……")` 拆完为空，污染的是互动／关系计数那一层，不是口癖候选）；`ObserveFeedbackAsync` 的**行为**半边仍然零来源——点击不提供 `FollowUpEvidence`，那是第 3 层 RT-006 的活；进程重启之前的旧气泡没有投递凭据 ⇒ 一律 409 `not_attributable`，这是设计结果不是缺陷；主动与脚本气泡界面上就没有这两项。**真机效果这一层仍未被任何读数覆盖**：以上全在测试宿主与真 SQLite 上证，模型档位与真人长聊的内测回访（第 6 层）不归本刀。状态词：已落地主工作区、未提交（未 stage、无 tag、未 push、`main` 仍停 `f6c9750`），判定归 QA 从可指认提交复采；§5.0 指针不动。〕
- **自动记忆抽取未接线（`memories` 0 行的定性）**：真机走查里"聊了很久但 `memories` 一条没有"不是解析缺陷，而是产品链路根本没有生产者。全仓只有三处写 `memories`：`Pry.Api/Services/MemoryApplicationService.cs:25`（手动 `POST /memories`，界面"记住这条"）、`Pry.Core/Conversation/ConversationService.cs:53`（该类在 `src/` 与 `tests/` 里除自身声明外零引用，是死类）、`Pry.Core/TurnTaking/ConversationTurnManager.cs:185`（只有 `tests/Pry.Core.Tests/CoreTests.cs:373` 构造过它）。运行时真实链路 `ConversationRuntime → ReplyGate/TurnScheduler → ReplyPlannerComposer → ReplyPlanner` 只写 `messages`，从不写 `memories` ⇒ 自动抽取属于 MEM/RT 的接线工作，不在本判据的修复面内。
- **Developer Mode UI**：`Rationale` 与 `Gates` 已经是可读字符串数组，但界面展示归 UI Owner。
- **表达抽取质量**：当前是启发式（插入语 + 句尾语气词 + 短独立片段）。要更接近"情境→表达"需要后台模型抽取，属于 RT-004 后续；本轮刻意没有为它预留接口。
- **关系信号初始化**：新角色实例从 `Familiarity 0.12 / Trust 0.35` 起步，这个先验没有用户数据支撑，需要靠 §7.2 的纵向内测回访校准。

## 9. 模型输出到落库之间的可见性判据（`DEF-P1WALK-01`）

思考型模型把内部草稿连同答复信封一起吐进 `delta.content`（`OpenAiCompatibleChatModel` 只读 `delta.content`，且 thinking 关闭时才挂 `reply_plan` 的 `response_format`，所以信封是 chat template 原样输出的）。草稿一旦落库就进界面历史、并被后续轮次当上文复读，撤不回来。

判据因此只有一条：**认不出可见答复就这一轮不说话（`……`，与"模型一个字没输出"同档），绝不回落成整段原始输出。** 形状识别全部集中在 `Pry.Core/TurnTaking/ModelReplyFormat.cs` 一处（四档：`Empty` / `PlainText` / `StructuredPlan` / `OnlyInternalDraft`），`ReplyPlanner.Parse` 只决定它落成什么，`Pry.Client`、界面与持久化层一律不认形状——这条由 `ModelReplyFormatTests.No_other_layer_recognises_the_reply_envelope` 扫产品源码守住（判据 ③）。

抽取用的全是结构信号，不做语义判定、不按字数放宽：

- 配对的答复信封优先，取**最后一个**配对体（草稿段里逐字复读的示例信封不能顶掉真答复）；
- 闭合的推理标签之前一律算草稿（走查 W1），未闭合的起始标签裁到文本末尾；
- 停在半截的标签名也算标签——只有整段文本以已知标签名的前缀收尾时才成立，且名字后面紧跟字母/数字/`:`/`-`/`_` 时判为"这不是完整标签名"（W3 的 `…</thinkin` 正是靠这条不被 `think` 的前缀命中蒙过去）；
- 没有信封但明显"想当计划"的残体（以 `{`/`[`/代码围栏开头，或留着 `"messages"` 键）判 `OnlyInternalDraft`，不按散文放行（第九、十一轮的裸 JSON 形状）；
- 结构化条目的正文同样剥推理段，剥不干净（16 趟预算用尽仍留标签）就整条丢弃；
- 散文答复只取第一条完整行，首行超出预算判空而不是截断——半截话与草稿长得太像。

判据载体：`tests/Pry.Core.Tests/ModelReplyFormatTests.cs` 16 例——13 条形状与拒绝档用例、2 条反向用例（正文里的合法尖括号、正文里引用的小段配置 JSON 都不许被这条判据改成沉默）、1 条扫产品源码的归属门禁。六条守卫各自被反证过一次——去掉回落拒绝、去掉信封优先、去掉推理剥离、去掉计划残体判据、去掉散文预算、在 `Pry.Api` 放一个含标签的临时文件，分别有点名的用例转红，跑完即还原。
