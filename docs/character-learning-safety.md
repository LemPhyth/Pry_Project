# RT-004 / RT-006：Character Intelligence & Learning 安全性增量

日期：2026-09-20。负责人：Codex Character Intelligence & Learning Engineer。

## Implemented

在 `codex/character-learning-safety` 独立 worktree 完成学习安全性修复与 18 项新增 Eval。位置为 `C:/Users/24782/.codex/worktrees/character-learning/Pry_Project`，提交基线为 `3613a2ab070a657ace22215ef1cae11f7c38605a`，并复制原工作区已有 Qoder 领域实现作为验证依赖。复制的实现仍属于原作者，本轮未提交它们，也未改原工作区的代码。

[增量补丁](character-learning-safety.patch) 仅包含本轮相对现有领域实现的修改，已在原工作区通过 `git apply --check docs/character-learning-safety.patch`。尚未应用补丁；因此原工作区的运行程序不会获得这些修复。后续集成者应先核对原作者的最新文件，再应用、全量测试和按所有权提交，不能将整个隔离 worktree 的未跟踪文件视为本轮新作。

改动文件：

- `src/Pry.Core/Intelligence/BehaviorPattern.cs`
- `src/Pry.Core/Intelligence/CharacterIntelligenceService.cs`
- `src/Pry.Core/Expression/ExpressionPattern.cs`
- `src/Pry.Core/Expression/ExpressionLearner.cs`
- 两个既有领域测试文件的反馈 fixture，以及新增 `tests/Pry.Core.Tests/LearningSafetyTests.cs`。

## Character State Changes

沿用 Identity / Relationship / Mood 分离，未改变身份锚点、关系闸门或 Mood 公式。新增后续反馈入口 `ObserveFeedbackAsync`，它不会重复增加交流次数、更新关系/Mood 或登记表达使用；真实投递仍只调用一次 `CompleteTurnAsync`。用户情绪估计与角色 Mood 的既有类型边界保持不变。

## Behavior Decisions

继续聊天、换话题、沉默与 Unknown 都属于模糊观察，不再产生成功/失败信号。即使已有模式也不增加它的计数、置信度或影响资格；原始 Experience 仍可记录。

只有明确针对该行为的反馈，且带非空 `FollowUpEvidence`，才允许 PositiveReaction / Corrected / Rejected 参与折叠。`HasEvidence` 要求至少三次已评价结果，普通使用次数不再解锁影响。每次变化仍最多 0.05，置信范围仍为 0.05～0.85。行为策略和表达形式仍分开，既有关系/情绪闸门继续在模式建议之后执行。

同一模式内按 Conversation + Turn 去重；相同仓储实例的并发 updater 串行折叠。同一回合第一次有效结果生效，后续冲突标签仅进入经验记录，不重复改权重；可信的结果更正/撤销需要后续单独契约。

## Expression Changes

模型自发重复只形成候选，不足以晋升为表达模式。候选需至少两个独立投递回合、两个独立明确认可反馈引用，并通过原有脏话、客服话术、事实、身份冲突和用户回声过滤。

明确拒绝的表达保存为 `Suppressed`。即使下一条回复已经没有这个口癖，也可通过 `RejectedStyles` 指明目标；规范化文字相同、Tone 不同的既有模式一并抑制。学习器重新创建后仍读取抑制状态，不会因新的正面样本自动恢复。尚未入库的拒绝项保存为零置信度抑制记录。

检索少量候选、Selector 可以全拒、严肃/低落场景和关系闸门、近期重复与冷却策略沿用已有实现。本轮没有加入完整 FUTURE 适用性规则引擎。

## Learning Logic

`TryEnqueue` 仅使用短本地锁和内存快照；队列容量 64，满时返回 false。抽取和仓储调用仍只发生在后台 `DrainAsync`，没有每条消息同步调用模型。并发 drain 串行；写入失败前不会清除 dirty 标记，下一次 drain 即使没有新消息也能重试。更新使用记录副本，避免失败的 Save 先修改内存仓储中已有对象。

表达置信度由去重后的认可证据计算，不按 drain 次数增加。Pattern 保存认可反馈 ID 与回合 key；重建学习器后重放相同反馈不会重复加权。行为 Experience 的 `FollowUpEvidence` 来自外部真实反馈，不再截取角色自己生成的回复冒充用户反馈。

## Contracts Consumed

消费既有 `ICharacterStateRepository`、`IBehaviorPatternRepository`、`IBehaviorExperienceRepository`、`IExpressionPatternRepository`、`SituationSnapshot` 与 `CharacterStateSnapshot`。未直接查询 Memory 数据库，也未改变 `ICharacterIntelligence.DecideAsync` 或现有 Memory 过渡投影。

本轮只沿用本地 `docs/research/MaiBot_MaiCore.md` 与 `PrivateCompanion.md` 的分层、证据和表达闸门设计启发，没有新增或声称完成上游版本核验。

## Contracts Changed

以下均为 Core 内部增量，没有 HTTP API、线协议或数据库迁移：

| 类型 | 新字段或语义 |
|---|---|
| `BehaviorExperience` | `FeedbackIsExplicit` 默认 false；`FollowUpEvidence` 必须属于实际用户后续反馈，建议使用引用而非聊天正文 |
| `BehaviorPattern` | `EvaluatedTurnKeys` 持久保存已折叠回合；`HasEvidence` 按 Success + Failure 判断；本轮 updater 的 UsageCount 只随有效折叠增加 |
| `DeliveredTurn` | `FeedbackEvidenceId`、`AcceptedStyles`、`RejectedStyles`；列表默认空；认可必须明确针对具体表达 |
| `ExpressionPattern` | `AcceptedEvidenceIds`、`AcceptedTurnKeys`；Suppressed 作为拒绝记录，禁止自动恢复 |
| `LearningOutcome` | 追加 `RejectedUnconfirmed`、`Unchanged`；保留原有枚举值顺序 |
| `TurnOutcome` | 明确行为反馈和表达反馈字段；普通 Corrected 不再被自动解释为拒绝所有使用过的表达 |
| `CharacterIntelligenceService` | `ObserveFeedbackAsync` 用于投递后的真实后续反馈，不重复交流计数或表达使用登记；返回 bool，未投递或队列满时 false |

宿主应遵守：

1. 投递成功后调用一次 `CompleteTurnAsync`，未知结果保持 Unknown、反馈字段为空；取消输出保持 `DeliveredToUser=false`。
2. 后续用户明确评价时，对原投递回合调用 `ObserveFeedbackAsync`；提供稳定、在 CharacterInstance 内唯一的真实反馈引用。不能拿当前用户输入、角色自己的输出或一般积极情绪冒充反馈。
3. 原 `UserInput` 和 `DeliveredAssistantText` 保留原投递回合内容；后续反馈放独立字段，不替换原始样本。
4. 每个 CharacterInstance 只有一个 ExpressionLearner 所有者，后台调 drain；失败由宿主有界重试。接口异常与取消向调用者传播，不伪装成已学习。
5. 仓储实现必须保留去重字段和 Suppressed；字段缺失的旧 Learned Pattern 不会自动获得新增的可信反馈证据。

## Eval Cases

新增 18 项自动化案例：四种模糊 Outcome、没有明确证据的标签、并发反馈重放、三次有效观察、模型重复输出、同一反馈引用重复利用、单回合多次认可、学习器重建后的幂等、后续纠正及重建后抑制、已观察回合的迟到纠正、失败后无新消息重试、取消保留队列、并发容量与 drain、反馈不重复改变角色状态、不同 Tone 的人工表达抑制。

既有 58 项 Character / Behavior / Expression Eval 继续通过，包括不过度提问、不复述、不客服化、低落未求助时不过度安慰、关系边界、熟悉后玩笑、严肃场景抑制口癖、旧梗可选、关系更正与单次异常不改人格。

最终验证均在上述隔离 worktree 执行：

- `dotnet build Pry.slnx -c Release` 成功：0 错误，44 个复用领域测试既有警告（本轮修改前同为 44）。
- `dotnet test Pry.slnx -c Release --no-build`：267 通过、0 失败、1 跳过。Core 92 通过/1 跳过，Client 3，Api 16，App 156。跳过的是既有真实 SenseVoice 权重用例。
- `scripts/verify-repository.ps1` 通过，扫描 205 个受跟踪文件；新增领域文件另行检查。`git diff --check`、`git diff --cached --check` 通过。
- 真实模型自然度、实际桌面运行、长期用户反馈未在本轮验证。隔离基线通过不代表原工作区所有其他在途变更通过。

## Known Failure Modes

- 可信反馈生产者尚未接线。当前采取保守默认；没有显式证据就不自动学权重。不能通过把全部样本设为 accepted 来绕过门槛。
- 抽取仍是词表/短句启发式，无法可靠识别所有事实、反讽或方言。认可引用的真实性、角色实例归属由生产者负责；本轮没有实现 Outcome 模型评估器。
- 队列满时可能丢掉反馈，包括纠正；宿主必须处理 `ObserveFeedbackAsync` 的 false、保留并重试已投递回合的重要纠正。主链 `CompleteTurnAsync` 只提交普通投递观察，队列满时允许丢弃；后续重要反馈必须走有返回值的新入口。自动接线尚未完成。
- 去重集合和候选证据随使用增长；尚无持久证据索引、保留上限与删除失效协议。删除 Pattern 也不是持久墓碑，本轮的 Suppressed 只解决明确拒绝后的恢复问题，不宣称已解决删除后旧任务复活。
- 同仓储对象的行为锁是进程内保证；多个 Repository 对象或多进程不能依赖它替代事务。表达一次多个变体的抑制尚无原子事务；保存失败后必须重试。
- `CompleteTurnAsync` 本身并非事务性 exactly-once，仍需 Runtime 保证投递通知一次；后续反馈必须走新入口。旧模式已有的可疑计数不会被本轮静默重写，需后续迁移审计。

## Deferred Work

RT-003/004/006 保持进行中。后续由对应负责人完成正式 MemoryContext 收敛、可信反馈归因、宿主学习消费与故障策略、仓储去重/墓碑/事务和状态持久化，以及模型与纵向体验 Eval。本轮未实现 Runtime Scheduler、SQLite 基础设施、Satori、UI、强化学习或微调。

回滚仅撤销本增量；没有数据库变化。合并前保留现有作者改动，补丁只作为增量审阅与接入材料，不应直接提交副本中全部未跟踪文件。
