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

刻意做小的部分：Outcome 缺失或含糊时记为 `InteractionOutcome.Unknown`，**不参与任何加权**；单次观察只把置信度移动 0.05，边界 `[0.05, 0.85]`；模式至少要 3 次观察才有资格影响 Planner（`MinimumObservationsToInfluence`），且影响方式是同分时选边和给出理由，不是放行被闸门挡住的行为。未投递（取消、被替换、stale）的输出在 `CompleteTurnAsync` 第一行就返回，不进经验、不计使用、不入学习队列。

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

`ReplyGenerationRenderer.Render(context, identity, budgetCharacters)` 把这一份投影成**有预算的文本块**：必备行（意图、避免项）先落地，可选行（心情、关系描述、底线、表达参考）在超出预算时被丢弃。关系一律渲染成中文描述而不是数值——内部信号不进 Prompt，也不进 UI。

它只是一个投影，不是第二个 Prompt 拼装入口。RT-002/RT-005 的 Context Builder 只需调用一次，把返回的文本并入现有 `PromptBuilder` 结果；本轮没有替它接线。

## 7. 固定 Eval 对应

`tests/Pry.Core.Tests/CharacterIntelligenceTests.cs`（Behavior/State/Learning）与 `ExpressionSystemTests.cs`（Expression 检索、Selector、学习准入）与 `LearningSafetyTests.cs`（自我强化、纠正后复活、后台重试、反馈归因）共 79 项，覆盖计划书 §7.1 的 Behavior/Expression 条目：不过度提问、不重复原话、不客服化、心情差但没求助时不过度安慰、关系浅时不过界、熟悉后允许自然玩笑、学到口癖不会每句都用、严肃场景抑制玩笑口癖、旧梗可召回但不强行、纠正关系认知后状态真的变化、单次异常互动不永久改变人格、空结果与查询失败可区分、取消的输出不进入学习、含糊反馈不加权、明确要求停止后不因正面样本复活。

两个测试文件都只断言决策与闸门，不断言措辞，因此换模型不会让它们失效。

## 8. 尚未完成（如实登记）

- **未接线**：`Pry.Core/Runtime`（RT-002）目前自行引用了 `Pry.Core.Intelligence.ResponseIntent`。该目录与我的目录都是未跟踪状态，任一方单独提交都会让另一方编译失败——集成时必须同一提交链落地，或由 Runtime 负责人确认引用关系。
- **与 MEM-005 收敛**：`RelevantMemoryView` / `MemoryObservation` 是过渡投影，M1 之后必须删掉。归属已定（MEM-005 R1）：`MemoryQueryOutcome` 等跨模块记忆契约的唯一定居处是 `Pry.Core/Abstractions`，本文件与 `Intelligence/SituationSnapshot.cs` 里的声明是先行占位，由我在与 Memory Owner 同批的提交中删除并改 `using Pry.Core.Abstractions`——不提前搬，以免出现第二个定义点。
- **持久化**：`ICharacterStateRepository / IBehaviorPatternRepository / IExpressionPatternRepository / IBehaviorExperienceRepository` 只有进程内实现。SQLite 实现、索引与 schema v8 迁移属于 Persistence Owner；版本乐观并发（`StateVersion` 落后即拒绝）和"删除角色实例时级联清库"必须在真实仓储里保持同等语义。RT-003 "Relationship 可持久化"的验收在真实仓储落地前只能算部分完成。落库时有三处必须原样往返，否则学习安全性会在重启后失效：`ExpressionPattern.Suppressed`（含零置信度占位记录）——丢了就等于用户明确的"别这么说话"只生效到下次重启；`BehaviorPattern.EvaluatedTurnKeys` 与 `ExpressionPattern.AcceptedEvidenceIds / AcceptedTurnKeys`——丢了同一 Turn 的反馈会被重复折叠，两者各自只需保留最近 64 条（滑窗语义，超出的旧值不可回放）；`BehaviorExperience.FeedbackIsExplicit` 与 `FollowUpEvidence`——含糊/明确必须可分，否则审计学习来源时无法判断某次权重变化是谁授权的。`FollowUpEvidence` 存**反馈引用的 ID**，不要复制聊天正文。
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
