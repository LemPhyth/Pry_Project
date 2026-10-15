# 工单 `DEF-P4LEARN-01` · 给她一句"就是这样／别这样说"（认可有入口，口癖才谈得上学得会）

> 状态：**已派工·待 Owner 认领（守门人 2026-10-15 按作者裁定写成）**。作者 2026-10-15 原话：「在界面上一个明确的"就是这样／别这样说"，不从上下文猜用户的态度，但是，将这个界面作为高级调试选项，我希望是用户能够慢慢跟AI打磨说话习惯，而不是手动干预，但是也会有希望手动干预的用户，所以作为高级选项」。这一句解除了本条唯一的开工前置（"先拍一句那个入口长什么样"）。Owner＝Character Intelligence（与第 3 层 `RT-006` 反馈入口同一批）；判定归 QA；状态词与结案词归作者。
>
> 载体基线＝当日 tip（本工单写成时为 `b15ed9d`）。历轮 763／767／788／809／819 全部过期，**开工与出判都必须在自己的干净载体上当场重取读数，不许做加法**。

## 0. 作者读这一段就够

- **今天做不到**：你怎么反应她都不记。界面没有任何地方能让你说"这句就这样"或"别这样说话"；后端也没有收这种话的接口。数据库里那张"她学会的说法"表在真机上今天还是空的——排空的通道 10-14 已经接上了（`docs/dispatch-rt004-drain.md`），可队列里流过去的每一笔都写着"用户没认可过"，而**写进账本要求同一说法被明确认可两次、而且来自两个不同的轮次**。
- **做完后能做到**：你在界面上明确点过的"就是这样"会真的进她的说话方式账本；点过的"别这样说"会把那个说法压住，往后不再拿出来用。默认情况下界面上看不到这两个按钮——它按你的裁定放在高级选项里，要手动打开才出现。
- **仍然做不到（本刀不解决，别当成解决了）**：她"越聊越懂你"里靠**记忆**改进那一半（第 2 层 `2-C` 管理面）；语气/时机那些旋钮仍押在最后那轮标定；一个说法要**两次**分别点头才会被记住，这是她现有的学习门槛，本刀不降它。
- **怎么算完成（一句人话）**：打开高级开关，连着两次对她不同的回复点"就是这样"，数据库里出现标着"她自己学的"那一行；重启后还在，而且她说话不会因为点这一下而变慢。

## 1. 今天的事实（逐条可指认，2026-10-15 实读）

| 环节 | 位置 | 现状 |
|---|---|---|
| 认可要什么才生效 | `src/Pry.Core/Expression/ExpressionLearner.cs:159-165`（记 `AcceptedIds`＋`AcceptedTurnKeys`）、`:52` `MinimumEvidence = 2`、`:229-230` `acceptedCount < 2 ⇒ RejectedUnconfirmed` | 必须是"该形状被明确列入 `AcceptedStyles`"**且**带一个非空 `FeedbackEvidenceId`；只被模型重复说过绝不晋升（`Model_repetition_alone_never_becomes_a_habit`） |
| 幂等形状 Core 已经留好了 | `ExpressionLearner.cs:150` 注释原文"Delivery replay can carry later feedback, but cannot manufacture another occurrence"、`:151` 按 `turnKey` 去重 | **迟到的认可**（用户过了几秒才点）在 Core 侧天生安全：重放同一轮只补证据，不虚增出现次数 |
| 输入字段 | `src/Pry.Core/Intelligence/CharacterIntelligenceService.cs:22-24`（`FeedbackIsExplicit`／`ExpressionFeedbackId`／`AcceptedExpressionStyles`）、`src/Pry.Core/Expression/ExpressionLearner.cs:21-22` | 只有默认空值与读取处；`src/` 内**零赋值点**（QA 于 `398101a` 独立复核过） |
| 唯一入口 | `CharacterIntelligenceService.cs:107` `ObserveFeedbackAsync` | **零生产调用者**。且 `:111-112` 表达侧只认"非空的 `AcceptedExpressionStyles` 或 `RejectedExpressionStyles`"；`:125` 把 `UserAskedToStopTalkingLikeThat` **写死 false** ⇒ 那半边的显式纠正今天同样没有入口 |
| 反馈端点 | `src/Pry.Api/Controllers/`（15 个控制器全查） | **没有任何**接受"用户对某条回复的反应"的端点；命中的 `Accepted(...)` 只是 HTTP 状态助手 |
| 客户端手里有什么 | `Pry.Contracts/CommonContracts.cs:122` `ChatMessage(long Id, …)`、`ApiContracts.cs:36`；`src/Pry.App/PryMessengerWindow.axaml.cs:409,418,435` 全部动作只用 `message.Id` | 客户端只拿得到**持久化消息行 id**。轮身份 `TurnIdentity(conversationId, TurnId, GenerationId)` 是后端私有的（`src/Pry.Api/Runtime/CharacterIntelligenceGateway.cs:121,220`），读端点 `ConversationRuntimeController.cs:11,15` 存在但 `PryBackendClient.cs` 从不调用 |
| 消息行 ↔ 轮 能不能对上 | `src/Pry.Core/Memory/MemoryDatabase.cs:33-42` `messages` 只有 `source TEXT DEFAULT 'legacy'`，**无轮次列**；`reply_plans` 有 `turn_id`／`generation_id`／`fence_epoch`（`MemoryDatabase.Runtime.cs:29`、`:143`），`planned_messages` 挂在 `reply_plan_id` 上（`MemoryDatabase.cs:105-116`） | 后端**有可能**把投递痕迹对到轮上，但 `messages` 与 `planned_messages` 之间今天没有外键；要拿到"用户点的那一条气泡 ⇒ 那一轮"必须补一处映射（见 §4，schema 相关改动要先登记） |
| 界面现有的每条消息动作 | `PryMessengerWindow.axaml.cs:365`（`stack.ContextMenu = CreateMessageMenu(...)`）、`:400-401`（"重新生成这条回复"／"删除消息"）；容器形状 `:331` `CreateMessageRow` → `:382` `Grid` → `:356-370` | 只有右键菜单，没有气泡悬浮按钮。同类先例：`Services/MessageContextMenuFactory.cs:8-22`、按钮排布 `Services/PryUi.cs:40 Actions(...)` |
| 高级开关放哪儿有先例 | `PryMessengerSettingsWindow.axaml:41-44`（`AdvancedSettingsToggle`／"高级设置"，在"模型"卡片里，`:46-54` 展开区）、持久化 `.axaml.cs:225-254` → `PryBackendClient.cs:126` `PUT api/v1/settings` → `ConfigurationController.cs:52` | 仓库里已经有一个"高级设置"折叠形状和一条完整的"设置项 → PUT → 后端"通路，照抄即可；五档 Tab＝个人资料／模型／聊天体验／快捷键／外观 |
| 学到之后会不会被用上 | `ExpressionLearner.cs:240,255` 新行置信度 `0.35 + 0.03×(认可次数-2)`；`ExpressionSelector.cs:25` `minimumConfidence = 0.35` | 算术成立：第一条学到的口癖正落在地板值上 ⇒ **不需要新标定参数**，本刀也不需要碰频率旋钮 |

## 2. 完成标准（每条都要能先红一次）

1. **端到端真机形状（今天必红＝本刀的"先红一次"）**：Api 层，真 SQLite＋真 gateway＋真投递路径，不许手调 handler。同一张卡、两个不同轮次各出现同一口癖，用户各点一次"就是这样" ⇒ `expression_pattern` 出现一行 `source='Learned'`，`expression_pattern_evidence`／认可 id 按序往返，重启（新建 gateway）后该行仍在。
2. **不点就永远不学**：连投同样的口癖若干轮而**没有任何点击** ⇒ 零 `Learned` 行、零认可证据。这条是"不许猜态度"的判据化身，必须存在且能红（反证＝把"投递即认可"写进去）。
3. **同一轮重复点不虚增**：同一轮连点三次"就是这样" ⇒ 认可证据按 `turnKey` 只算一次，仍 `<2` ⇒ 不晋升（照 `ExpressionLearner.cs:151`、`:229` 的形状）。
4. **"别这样说"真的压住**：显式拒绝某形状后走 `RejectedStyles`（`ExpressionLearner.cs:142-147`）⇒ `Suppressed=true`，此后该形状不再被她拿出来用，且后续排空/重放都不得复活它（`:196-216`）。
5. **新端点按仓库规矩办事**：DTO 落 `Pry.Contracts`、有验证、有明确错误语义（不存在的消息／不属于该会话／未开启高级反馈各成一种，不许都塞 500），文档与测试齐全（`AGENTS.md` 第 5 条）。
6. **高级开关是真的开关**：关 ⇒ 界面上两个动作不出现，且后端对该请求按 §2.5 的错误语义拒绝；开 ⇒ 立即可用，不需要重启。开关本身沿用既有设置通路（`PUT api/v1/settings`）。
7. **不碰可见回复路径的延迟**：点一下"就是这样"不得让下一条回复变慢，也不得让排空搬到轮次路径上（`A_store_that_stops_mid_drain_cannot_delay_a_visible_reply` 的方向）。
8. **高级面板不泄露任何内部数值**：面板与请求体只许出现"就是这样／别这样说／是否启用"，**不得**显示熟悉度、信任度、置信度、计数或档位；`AffinityNumbersStayInsideTests` 三面门必须仍绿（若在 `Pry.App`／`Pry.Client`／`Pry.Api` 源码里写出那些标识符，那道门会直接红——那是设计，不是需要绕开的障碍）。
9. **固定 Eval 对照**：完成时重跑 `scripts/measure-fixed-eval.ps1`，目录仍是 75 条永久场景 ⇒ **判据变多不是场景变多**。

## 3. 反向约束（随单入库，不得放宽）

1. **不许从上下文猜用户态度**（作者原话"不从上下文猜用户的态度"）。禁止的形状包括：把"用户继续说话""用户复述了她的说法""没有反对"当成认可。`UserSaidIt`／`echoedByUser`（`ExpressionLearner.cs:148-149`）那套既有语义照旧，**不得**把它接成认可来源。认可只有一个来源：用户在界面上明确按过的那一下 ⇒ `FeedbackIsExplicit` 的语义自此定死。
2. **不降她的学习门槛**：`MinimumEvidence = 2`、`TurnKeysPerShape`、`AcceptedEvidenceIds`／`AcceptedTurnKeys` 的"按序往返、不排序不去重不截断"（`docs/character-intelligence.md:116`）一字不碰。"让她一次点头就学会"= 改判据本体，要动请作者另立裁定。
3. **不夺走 Selector"这次一个都不用"的许可**：`ExpressionRetriever`／`ExpressionSelector` 的选择语义（`ChoseNothing`、`HabitToneAllowed.None`、`maxHints`）原样。学到 ≠ 每轮必须用。
4. **既有学习安全判据一字不改**，点名四条：`Model_repetition_alone_never_becomes_a_habit`、`A_cancelled_drain_preserves_queued_work`、`A_failed_store_write_is_retried_without_another_turn`、`Concurrent_enqueue_is_bounded_and_concurrent_drains_do_not_duplicate_evidence`（`tests/Pry.Core.Tests/LearningSafetyTests.cs`）。若某条被本刀"合理地"变红 ⇒ 形状选错，改代码不改判据。
5. **不改端口形状**：`IRuntimeOutputSink` 六个方法与签名一字不动（契约 §7.1 结论 1 冻结）；不为反馈新增第七个外发方法；不在网关里新写第二套信封解析（`No_other_layer_recognises_the_reply_envelope` 那道源码门正是拦这件事的）。
6. **不与 `DEF-P3SEAM-04` 合并处置**：那条讲"占位符被记成一次已投递的可学互动"，本刀讲"谁给出认可"。两条各自有载体读数与反证，同刀会让 §5.0 判据 4「读数不得互顶」直接破。本刀只要求：对**占位符那一轮**点认可时要有明确口径（推荐＝判定"这一轮她没真的说话"就不重放），落笔不许写成"`-04` 已修"。
7. **不碰 `MainWindow` 家族**（执行基准 `0-5` 的反向约束）。同期 `PryMessengerWindow.axaml.cs`／`.Companion.cs`／`Services/NoticeContentFactory.cs` 属 UI-027 lane 的在途面 ⇒ **开工前必须在台账声明单一写者**（`AGENTS.md` 第 3 条）。
8. **schema 改动要先登记再开工**（`AGENTS.md` 第 2/3 条）：若选"给 `messages` 补可空 `reply_plan_id`"这类映射，必须走 `EnsureColumnAsync` 的既有形状并升 `memory_schema` 版本，不得夹带别的迁移，不得顺手删列。能在不新增列的前提下完成（例如反馈请求带 `conversationId` ＋后端自己的投递账本）就不新增列。
9. **不许把本刀写成"她越聊越懂你已完成"**：`docs/phase2-runtime-contract.md` 那两句里，本刀只解除"认可没有生产者"这一句；`docs/character-intelligence.md` §8 里"不许出现'她会学说话方式'的读法"要等 §2.1 那条真机判据真的绿了才撤销。

## 4. 建议实施形状（推荐 A；B/C 不接受为主案）

- **A（推荐）＝"气泡 → 那一轮"的解析放在后端，客户端只报它真持有的东西**：
  1. 界面：在既有右键菜单（`PryMessengerWindow.axaml.cs:365`、`CreateMessageMenu`）里按开关加两条项——文案就是作者那两句"就是这样／别这样说"；不新建悬浮按钮体系，不动 `MainWindow`。
  2. 开关：沿用 `PryMessengerSettingsWindow.axaml:41-44` 那套"高级设置"折叠，落一条布尔设置，走既有 `PUT api/v1/settings` 通路；关掉 ⇒ 两项不出现且后端拒收。
  3. 端点：`POST api/v1/conversations/{conversationId}/messages/{messageId}/expression-feedback`（或等价形状），DTO 进 `Pry.Contracts`，错误语义按 §2.5。
  4. 归属解析：`CharacterIntelligenceGateway` 用后端自己的投递账本把 `(conversationId, messageId)` 对到 `TurnIdentity`；对不上就**明确拒**，不许退化成"按最近一轮归属"——那正是 `DEF-P3SEAM-02` 花了一刀才拆掉的承重墙写法。
  5. 教学：解析成功后走**该轮的重放**（带 `AcceptedStyles`＋每笔唯一的 `FeedbackEvidenceId`），依赖 `ExpressionLearner.cs:150-151` 已写好的幂等性；`ObserveFeedbackAsync` 自此第一次有生产调用者，排空侧不动（`ExpressionDrainService` 照旧逐卡取）。
- **B（不接受）**：客户端自己算"她刚才那一句"＝前端猜轮次。跨轮迟到、气泡与轮次不一一对应，且把后端私有不变量外抛。
- **C（不接受为主案）**：不做界面、改由模型侧输出一个"这条被认可"的标记。正面撞作者那句"不从上下文猜用户的态度"。

## 5. 门禁与读数（现有纪律一条不省）

- 从**可指认提交**起算的干净 `--detach` worktree；混树数字不得充当复采，也不得引用他人轮次数字。
- 每次读数一律 `dotnet build Pry.slnx -c Release -t:Rebuild`（改过码不重编＝跑在旧 DLL 上假绿，同族坑已记两次）。
- `dotnet test --logger trx` 且四个程序集 `LogFileName` 各自唯一，结果目录新建；随后 `scripts/measure-fixed-eval.ps1 -ResultsDirectory <新目录>`。
- 报"几连绿"必须同场列出**全部跑法次数与每一个红例名**；不许用 skip 或放宽断言换绿。
- 新判据先**故意红一次**，突变逐个点名转红、`cp`+`cmp` 字节级还原后复跑。
- 跑门禁前先查同级 worktree 有没有他人的 `dotnet` 进程（`docs/engineering-pitfalls.md`）。
- 提交只 stage 自己名下文件；不 `git add -A`、不 `--amend`、不 `--no-verify`、不 push。

## 6. 出判面与随刀登记

| 项 | 归属 |
|---|---|
| Owner 自证（判据＋突变反证＋干净载体读数） | Character Intelligence |
| 从可指认提交复跑、出正式报告号 | QA / Evaluation |
| "认可入口已落地"与 §8 那句读法撤销的落笔 | 作者（守门人不代写结案词） |

随刀文档（都在该 lane 名下，别混提交他人的在途面）：`docs/character-intelligence.md` §8、`docs/character-learning-safety.md`、`docs/phase2-runtime-contract.md` 相应那句、`CHANGELOG.md` 只写本刀那一条、`docs/full-scope-sprint.md` §3 第 1 层状态、`PROJECT_PLAN.md` §4.2 本行状态。

## 7. 本刀不解除的门（照旧有效）

- 第 2 层语义通道的**先接线后标定**门（`docs/full-scope-sprint.md` §3 的 `2-D`）不因本刀变。
- 第 6 层标定轮仍是 Release 前置；频率旋钮在标定轮结束前不进设置界面（本刀只加一个布尔开关，不加阈值）。
- `DEF-P3SEAM-04`（占位符教学）与本刀**并列**，不互为前置，各自派工。
- §5.0 阶段指针不因本刀推进。
