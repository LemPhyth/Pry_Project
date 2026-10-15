# 工单 RT-004-DRAIN · 给她一条后台排空通道（口癖真的学得会）

> 状态：**当日已派工、已落地、已提交成可指认版本（2026-10-14 ⇒ 载体 `398101a`，4 文件／683 插入；作者原话"把那 4 个文件提交成可指认提交"，守门人逐文件对线后代跑提交动作并在同一台机器上重跑过门禁：`-t:Rebuild` 构建 0/0、解决方案级连续两次 819／0／2 分项逐位相同、`measure` 两目录各 exit=0（75 条）、`verify-repository` 431）**。排空半边成立、审批半边不成立 ⇒ 剩余那半边另登记为 `DEF-P4LEARN-01`。落地细节与本工单哪几条被哪条判据覆盖，写在 `docs/character-intelligence.md` §8 与 `PROJECT_PLAN.md` §9 当日记录；本文件保留原样作为派工时的依据，只在下面 §0 更正一句"做完后能做到"。**同刀的三份文档（`docs/character-intelligence.md`、`docs/phase2-runtime-contract.md`、`docs/character-learning-safety.md`）与 `CHANGELOG.md` 按作者那句"那 4 个文件"留在提交外，仍未入库；未 push、未打 tag、`main` 未动。**

## 0. 作者读这一段就够

> **落地后的更正（10-14，写完本工单当天实测）**：下面"做完后能做到"那一句**只对了一半**。排空通道确实接上了（新 `src/Pry.Api/Services/ExpressionDrainService.cs`，Core 零改动，队列满 64 静默丢弃这一半已解除），但**"你点头喜欢的说法"这个输入本身在今天的生产代码里没有任何来源**——`TurnOutcome.AcceptedExpressionStyles` 与 `ExpressionFeedbackId` 在 `src/` 内只有默认空值和读取处、没有赋值点，后端也没有反馈端点，而写进账本要求"被明确认可 ≥2 次"。所以真机今天仍然看不到她复用你的说法。那半边登记为 `DEF-P4LEARN-01`（同一 lane，与第 3 层 `RT-006` 同批），**本工单 §3 第 7 条明令不许顺手接，所以它是边界不是遗漏**。


- **做完后能做到**：她反复说过、而且你明确点头喜欢的说话方式，会真的写进她的"说话方式账本"；下次重启她还会用。今天不会——她每次说完一句都被记进一条待处理队列，那条队列**只有放进来的人，没有取走的人**，攒到 64 条就开始丢，而且丢的时候一点报错都没有。
- **仍然做不到（本刀不解决，别当成解决了）**：她"越聊越懂你"里靠**反馈**改进那一半（`ObserveFeedbackAsync` 仍无调用点）；行为模式那两张表照旧是空的，那是设计。
- **怎么算完成**：连聊一阵子之后，数据库里 `expression_pattern` 表出现标着"她自己学的"的行；重启后还在；而且聊天回复不会因为这条后台通道变慢。

## 1. 今天的事实（逐条可指认，2026-10-14 实读）

| 环节 | 位置 | 现状 |
|---|---|---|
| 入队 | `src/Pry.Core/Intelligence/CharacterIntelligenceService.cs:98` → `:122` | 每一轮投递成功都会 `TryEnqueue` 一条（`CompleteTurnAsync` 里以 `_ = ...` 丢弃返回值） |
| 排空 | `src/Pry.Core/Expression/ExpressionLearner.cs:103` `DrainAsync` | **`src/` 内零调用者**（只有测试调它） |
| 队列上限 | `ExpressionLearner.cs:51` `QueueCapacity = 64`、`:88` 满即 `Interlocked.Increment(_dropped)` 并返回 false | 64 轮之前无任何错误迹象；`PendingCount`／`DroppedCount` 是现成的可观测口 |
| 学习器归属 | `ExpressionService.cs:25` 内建 `Learner`（`public`），`ExpressionService` 由 `src/Pry.Api/Runtime/CharacterIntelligenceGateway.cs:83-85` **每卡一份**建在 `Scope` 里 | 排空必须**逐卡**做；gateway 今天没有"枚举活着的 Learner"的口子 |
| 写库这一半 | `Program.cs:68` 注册 `SqliteExpressionPatternRepository`（`Memory/IntelligenceRepositories.cs:60`），表 `expression_pattern` 在 `MemoryDatabase.Intelligence.cs:72` | 通的。**缺的只有"谁来排空"**，不是缺存储、不是缺参数标定 |
| 学到后能否被用上 | `ExpressionLearner.cs:255` 新建模式置信度 `0.35 + 0.03×(证据数-2)`；`ExpressionSelector.cs:25` `minimumConfidence = 0.35` | 算术成立：第一条学到的口癖正落在地板值上，**只要那一行真写得进库**，下一轮就有机会被她用上、不需要新标定参数。〔10-14 落地后补一句：那一行今天还写不进去，卡的是"认可"没有生产者，见 §0 的更正与 `DEF-P4LEARN-01`〕 |

## 2. 完成标准（每条都要能先红一次）

1. **端到端可见（Api 层，真 SQLite＋真 gateway＋真投递路径，不许手调 handler）**：同一张卡在两个会话里，让带同一口癖的答复被明确认可 ≥2 次并跨过独立轮次 ⇒ `expression_pattern` 出现一行 `source='Learned'`，`expression_pattern_evidence` 按序；随后 `SelectForTurnAsync` 一侧能检索到它。这条今天必红，就是本刀的"先红一次"。
2. **不再丢**：连投 > `QueueCapacity` 轮 ⇒ `DroppedCount == 0`，且静置一个排空周期后 `PendingCount` 归零（或有界归零）。
3. **不在可见路径上**：任何"发一句→拿到答复"的既有判据不得新增等待；并加一条判据把这点钉住（形式自选，例如把排空人为拖慢到秒级、断言可见答复路径的等待里没有它）。
4. **失败可重试**：让仓储第一次 `SaveAsync` 抛 ⇒ 该口癖本轮不下库、**下一次排空自动补上**（宿主侧要复现 `A_failed_store_write_is_retried_without_another_turn` 的方向）。
5. **取消与退出安全**：排空被取消时队列里的活儿不丢（`A_cancelled_drain_preserves_queued_work` 的宿主版本）；后台循环内的异常只记日志，**不得终止进程、不得让宿主停摆**。
6. **显式纠正仍然压制**：走 `RejectedStyles`／纠正分支（`ExpressionLearner.cs:196-217`）写 `Suppressed=true` 之后，后续排空不得把它复活；这条要判据，不许只靠"看着像对的"。
7. **跨重启**：学到的行在重启（新建 gateway／新进程）后仍在，且不被学习器重建而重复加权（`Explicit_feedback_is_idempotent_across_learner_recreation` 的方向）。
8. **固定 Eval 对照**：本刀新增用户可见行为（她开始复用你的说法），完成时要重跑 `measure-fixed-eval.ps1` 并与 `eb01acf` 基线对照，目录仍是 75 条永久场景——**是判据变多，不是场景变多**。

## 3. 反向约束（随单入库，不得放宽）

1. **不为变绿放宽晋升滑窗与"按序往返"**：`ExpressionPattern.AcceptedEvidenceIds`／`AcceptedTurnKeys` 必须按原顺序往返，仓储不得排序、去重或截断（`docs/character-intelligence.md:116`）；窗口 64 条（`:75`）。要动这两条 = 另立工单。
2. **不做成"同步等待可见回复"**：抽取与写库只能在宿主排空时发生（`docs/character-intelligence.md:85`；`ExpressionService` 类注释原文"Nothing here sends or delays a message"，不得改这句）。
3. **不夺走 Selector"这次一个都不用"的许可**：`ExpressionRetriever`／`ExpressionSelector` 的选择语义一字不碰（`ChoseNothing`、`HabitToneAllowed.None`、`maxHints` 全部原样）。学到 ≠ 每轮必须用。
4. **既有学习安全判据一字不改**，点名这四条：`Model_repetition_alone_never_becomes_a_habit`、`A_cancelled_drain_preserves_queued_work`、`A_failed_store_write_is_retried_without_another_turn`、`Concurrent_enqueue_is_bounded_and_concurrent_drains_do_not_duplicate_evidence`（`tests/Pry.Core.Tests/LearningSafetyTests.cs`）。**断言方向与判别力都不许动**；若某条被本刀"合理地"变红了，说明形状选错了，不是改判据。
5. **不改 schema**：`expression_pattern` 与两张守卫子表已够用；真觉得非改不可，先登记再开工（`AGENTS.md` 第 2/3 条），不许顺手夹带迁移。
6. **Core 不引 HTTP/Avalonia**（`AGENTS.md` 第 5 条）。**理想形态是 Core 零改动**：`ExpressionService.Learner` 已是 `public`，让 `CharacterIntelligenceGateway` 在 `Scope` 里留住它建的那个 `ExpressionService`（现在是在参数表达式里就地构造的），就能枚举活着的 Learner——不需要在 Core 上开新公共 API。
7. **不许顺手把 `ObserveFeedbackAsync`（仍零调用者）接进来**：那是第 3 层 `RT-006` 的活，混进来会让本刀的用户可见变化无法归因。
8. **不许把本刀写成"她越聊越懂你已完成"**：`docs/phase2-runtime-contract.md:306` 那两句里，本刀只解除"没有排空者"这一句。

## 4. 建议实施形状（推荐 A；B/C 不接受为主案）

- **A（推荐）**：`Pry.Api` 一个 `BackgroundService` + `PeriodicTimer`，照 `src/Pry.Api/Services/MemoryConsolidationService.cs:11` 与 `MediaCleanupService` 的既有先例。每 tick 向 `CharacterIntelligenceGateway` 要"当前活着的 Learner 列表"，逐个 `DrainAsync(ct)`，**带一个有界超时**（照 `ConversationSessionService.cs:60` 那个 5 秒形状），逐个 try/catch，异常写日志不逃逸。空闲成本已知很低：没有脏形状时 `DrainCoreAsync` 直接返回 `[]`（`ExpressionLearner.cs:117`），一次 tick 就是一趟内存遍历。
- **B（不接受）**：在 `CompleteTurnAsync` 里就地排空——把后台活搬上轮次路径，正面撞反向约束 2。
- **C（可作补充，不可作主案）**：只在 `CancelAsync`／会话卸载时排空——那等于"她学到什么取决于你什么时候点停止"。
- 间隔与阈值由 Owner 定，但工单要求你能**解释**：正常使用下 64 的容量为什么不会被填满（或：填满时 `DroppedCount` 会被判据抓住）。

## 5. 门禁与读数（现有纪律一条不省）

- 从**可指认提交**起算的干净 `--detach` worktree；混树数字不得充当复采，也不得引用他人轮次的数字充数。
- 每次读数一律 `-t:Rebuild`（旧 mtime 会让 MSBuild 跳过重编 ⇒ 突变跑在旧 DLL 上整轮假绿）。
- `dotnet test --logger trx`，四个程序集的 `LogFileName` 各自唯一（共用一个名会互相覆盖成一份，同族坑已记两次）；`measure-fixed-eval.ps1 -ResultsDirectory <新目录>`。
- **总数基线**：`eb01acf` = 809 通过／0 失败／2 跳过（Client 93／App 183／Api 154／Core 379+2skip）。本刀新增几条就报几条，报"几连绿"必须同场列出**全部跑法次数与每一个红例名**。
- 新判据必须先**故意红一次**（突变逐个点名转红、`cp`+`cmp` 或 md5 字节级还原）；反证脚本改源码不许用能在截断之后抛的形式（`docs/engineering-pitfalls.md`）。
- 跑门禁前先查同级 worktree 有没有他人的 `dotnet` 进程；`git status --short` 确认只动自己名下文件，`MainWindow.axaml.cs` 不在本刀范围。
- 不 `git add -A`、不 `--amend`、不 `--no-verify`、不 push。

## 6. 出判面与随刀登记

| 项 | 归属 |
|---|---|
| Owner 自证（判据 + 突变反证 + 干净载体读数） | Character Intelligence |
| 从可指认提交复跑、出正式报告号 | QA / Evaluation |
| 状态词与"缺口 ④ 解除"落笔 | 作者（守门人不代写结案词） |

随刀文档（都在该 lane 名下，别混提交别人的在途文档）：`docs/character-intelligence.md` §8 缺口 ④ 解除（那句"本文件不许出现'她会学说话方式'的读法"自此撤销）、`docs/phase2-runtime-contract.md:306`、`docs/character-learning-safety.md`、`CHANGELOG.md` 一条（只写本刀那条）、`docs/full-scope-sprint.md` §3 第 1 层状态、`PROJECT_PLAN.md` §4 本行状态。

## 7. 本刀不解除的门（照旧有效）

- 第 2 层语义通道的**先接线后标定**门（`docs/full-scope-sprint.md` §3 的 2-D；`docs/memory-recall-calibration.md` §7.1、`docs/memory-context-contract.md` §15.3）不因本刀变。
- 第 6 层标定轮仍是 Release 前置；频率旋钮在标定轮结束前不进设置界面。
