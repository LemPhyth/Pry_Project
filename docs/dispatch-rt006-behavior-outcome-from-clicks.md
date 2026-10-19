# 工单 `RT-006-BEHAVIOR-OUTCOME-FROM-CLICKS`（第 3 层 · 行为学习的证据来源）

- **状态词**：已派工·待认领（守门人 2026-10-19 最晚按作者授权写出；Owner＝Character Intelligence，与认可入口 `DEF-P4LEARN-01` 同一条 lane；判定归 QA 从可指认提交复采，结案词归作者）
- **载体基线**：开工当日先 `git rev-parse` 与 `git status` 重取线尾；在册读数＝**886／0／2（Client 93／App 221／Api 185／Core 387+2skip，`eval-QA-P3CLOSE-1019`）**，本单不改写它，也不得挪用到别刀读数。
- **授权来源（逐字）**：作者 2026-10-19 原话「**我不想思考了，你就继续给我派工单，该提交的提交吧，早点做完之后再回来大修**」⇒ 守门人在本单纯文本推荐项 **A** 中代拍形状：行为证据**只**来自已有的两下明按钮。作者若事后读本句想改形状，一句话即可推翻——被推翻的是证据来源，不是判据链。

## 1. 事实表（全部为守门人 2026-10-19 当场读码，行号当日有效）

| # | 事实 | 位置 |
|---|---|---|
| F1 | 行为学习的闸门是 `learnsBehaviour = outcome.FeedbackIsExplicit && !string.IsNullOrWhiteSpace(outcome.FollowUpEvidence)` | `src/Pry.Core/Intelligence/CharacterIntelligenceService.cs:110` |
| F2 | 加权信号的门同形：`FeedbackIsExplicit && FollowUpEvidence 非空` 才取 `Signal()`，否则 null | `src/Pry.Core/Intelligence/BehaviorPattern.cs:133` |
| F3 | 信号值：`PositiveReaction` 1.0／`Corrected` −0.8／`Rejected` −1.0；`Unknown` 无信号 | `src/Pry.Core/Intelligence/BehaviorPattern.cs:71-75` |
| F4 | `ObserveFeedbackAsync` 有且只有一个生产调用者＝认可入口的点击路径，它传 `InteractionOutcome.Unknown` 且**刻意不带** `FollowUpEvidence`（docstring 明写"行为经验那一侧归第 3 层 RT-006，本刀不越过去"） | `src/Pry.Api/Runtime/CharacterIntelligenceGateway.cs:235-243`（`:214-215` 为那句原文） |
| F5 | 轮次终局路径同样传 `Unknown`，注释明写"用户怎么反应还没接进来" | `CharacterIntelligenceGateway.cs:272-277` |
| F6 | ⇒ 综合 F1–F5：点击路径有 `FeedbackIsExplicit=true` 但 `FollowUpEvidence` 恒空 ⇒ F1/F2 两道门**今天恒不成立**；两张 behavior pattern 表为空是设计使然，**行为经验从开业至今从未学到过一条** | 本单 §1 的推论，QA 可复跑 |
| F7 | 点击请求里已经握着本轮的完整现场：`receipt.Turn`（含 `Situation`／`State`／`Generation.Decision`）与 `receipt.Text`（已投递正文，且 `shapes.Count == 0` 已在前挡住空正文） | `CharacterIntelligenceGateway.cs:230-237` |
| F8 | `TurnOutcome` 的记录形状今天就有 `Outcome`（位参）与 `FollowUpEvidence`（可选 init）两个槽，**无需新字段、无需 schema** | `CharacterIntelligenceService.cs:13-21`、`BehaviorPattern.cs:49-51` |

## 2. 形状（A：只有那两下按钮兼作行为证据）

1. 「就是这样」的一次点击，除现有口癖证据外，同笔带上 `Outcome = InteractionOutcome.PositiveReaction`、`FollowUpEvidence = receipt.Text`（已投递正文原样，长度上限沿用 Core 既有截断语义，不新造）。
2. 「别这样说」＝ `Corrected`（−0.8）。`Rejected`（−1.0）**保持零生产者**并落文档写明为什么——一个按钮不该同时宣判"整体拒绝"。
3. **没按＝永远不学**：无点击轮次仍走 `Unknown`、信号 null、零加权，F5 那条终局路径本单一行不改。
4. 同一轮重复点击的**行为侧去重**沿用 Core 既有的按 `turnKey` 去重（`ExpressionLearner.cs:150` 同源形状），不另造第二本账；连点三次仍只算一次证据、一次加权。
5. 证据归属仍走既有进程内投递账本：认不出＝409 拒，**不许**回退"按最近一轮"（那是 `DEF-P3SEAM-02` 花一刀拆掉的写法）。

## 3. 完成标准（出判面，逐条可判）

1. **先红一次**：下面第 2、3 条用例在实现前当场为红。
2. 已投递的一轮被「就是这样」→ 该角色实例的行为经验账里出现一条带 `PositiveReaction` 与非空 `FollowUpEvidence` 的记录；连续两轮不同轮次的同向点击后，对应行为模式的置信度**动了**（权重路径 F2 被真实走通——这是本单存在的意义）。
3. 「别这样说」→ 同形状落成 `Corrected`，负向；且本轮口癖证据照旧只教"别这样说"的那句，不与行为证据互吞。
4. 没有任何点击 → `Unknown`、null 信号、零加权，与今日逐字段等值。
5. 同一轮连点 3 次 → 行为加权事件只发生一次（按 `turnKey` 去重）。
6. `scripted` 气泡与"她没说上话"的占位轮**仍无入口、仍不进行为账**（`VisibleReply.HasVisibleContent` 判定引用，不重抄长度比较——`DEF-P3SEAM-04` 的形状）。
7. **解除条件落文档**：`docs/character-intelligence.md` 写死"两张 pattern 表自本刀起可加权，且加权只来自显式点击；任何自动推断的接入都必须先有新的作者裁定"——不许靠运行时惊喜。
8. 零 schema、零迁移、零 SQL 文件；`src/` 内不出现任何从文本猜态度的代码。

## 4. 反向约束（不许走的捷径，逐条点名过）

- **不许**用用户下一句话的情绪／措辞／是否继续聊来填 `Outcome`——那是作者 10-15 逐字禁掉的"从上下文猜用户的态度"，也是本单推荐 A 淘汰 C 的全部理由。
- **不许**动已签形状：`MinimumEvidence = 2`、按序往返、默认关的高级开关、409 归属拒法、请求内不排空。
- **不许**为覆盖率改任何产品代码以外的守卫语义；不许把 `Rejected` 拿来当"别这样说"的别名。
- **不许**把本刀写成或读成"越聊越懂你"——懂的那一半仍然是"你按过两下"；§8 的解除位置从"口癖"扩到"行为模式"，但**主语仍是明确点击**。
- **不许**带进任何未标定阈值（`2-D` 门一字不解除）；本单只接线，参数标定在第 6 层。
- 文档文本与代码**分刀提交**；载体＝从开工当日线尾新建的独立 worktree；主工作树里他人名下文件（含 `CHANGELOG.md`、`docs/api-v1.md` 在途面）一律不碰。
- 与本 lane 的 `QA-TIP1016-02`／`QA-TIP1019-01` 两条覆盖面欠账**可以**同刀处理（纯测试文件），但完成标准里必须各点名一条红。

## 5. 认领与出判

认领＝在本文件首行状态词处签名＋进 `PROJECT_PLAN.md` §4.2 该行列"进行中"；出场＝§9 同轮记录＋门禁自跑读数（带全四项分项）；**判定归 QA 从可指认提交复采＋独立证伪**（承重墙＝第 2／4／5 条，QA 至少突变三处：把点击的证据退回 `Unknown`⇒第 2 条红；把无点击轮次判成正向⇒第 4 条红；去掉 `turnKey` 去重⇒第 5 条红——方向由 QA 自定，此处只列可判形状）。
