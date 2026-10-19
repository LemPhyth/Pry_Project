# 派工单 `DEF-P3SEAM-04` · 占位符那一轮不许算成"她说了话"（Phase 3 收尾）

- **状态**：**已完成·已入库（2026-10-19 就地标注；实现＝Character Intelligence lane，可指认提交 `88eff2a`，完成标准九条齐、§2.6 的历史错账口径由作者 2026-10-18 落笔为甲；本单自此只作历史与判据出处保留，下面原文一字未改，唯一还欠的是 QA 从 `88eff2a` 复采）**。原状态词按史保留如下 ↓ 已派工·待认领（2026-10-16，守门人写单；作者原话「那到 Phase 4 还差什么东西你派单一下」）
- **Owner**：Character Intelligence & Learning（`src/Pry.Api/Runtime/CharacterIntelligenceGateway.cs`、`src/Pry.Core/TurnTaking/*`、`src/Pry.Core/Intelligence/*` 均为该 lane 名下）
- **判定**：QA 从可指认提交复采 + 独立突变反证；结案词与 §5.0 阶段指针不由 Agent 写
- **载体基线**：线尾 `c608220`（守门人提交前自跑 876／0／2 两次；**QA 尚未从该 SHA 复采**，在册的独立复采读数仍是 854 那份，同数不同项，引用必须写全四项）
- **前置**：无（`0-1` 的接缝提交 `6f47f34` 早已满足；本刀不依赖第 2 层 `2-C`）
- **严重度**：Medium。学不到错东西，但会把"没说出话的一轮"记成一次成功互动，关系与熟悉度照涨

---

## 0. 给作者的一段（只有这一段是写给你的）

有些轮次模型没兑现出正文，界面上你看到的是她发了两个点（`……`）。这一轮在账本上被当成**她说了话、而且说成功了**：你们的"聊过一次"计数 +1，关系分和熟悉度往上走一格，她本轮原本打算用的那个口头习惯也被记成"用过一次"。**她不会因此学会省略号**（省略有名无实，学说话那一侧拆出来是空的，那条路今天不通）。这一刀要做的就是把这一轮从账本上摘下来，同时保证她真说了话的那些轮一个字都不少记。**改完你看得见的唯一变化**：她不会在你没得到实质回答的那些轮之后，跟你更熟一点。

---

## 1. 事实表（全部 2026-10-16 当场 Read/grep 重取，行号会漂，不许沿用旧稿）

| # | 事实 | 位置 | 为什么要紧 |
|---|---|---|---|
| 1 | 占位符是 Core 里的一个**私有常量**，本轮没有可落库的可见答复时投出去的就是它，注释自己写明"与'模型一个字没输出'同档" | `src/Pry.Core/TurnTaking/ReplyPlanner.cs:15`（定义）、`:98`（返回点） | 可见侧已经承认它等于"没说上话"，学习侧却不认 |
| 2 | 形状判定枚举是 **`internal`**：`Empty`／`OnlyInternalDraft` 两档就是"没有可见答复" | `src/Pry.Core/TurnTaking/ModelReplyFormat.cs:5`（`internal enum ModelReplyShape`）、`:56`、`:66-67` | Api 侧读不到它 ⇒ 完成标准①"同源"不是抄一行 if 就能满足的，这是本刀真正的设计点 |
| 3 | 网关那道闸只看**长度**： outcome ∈ {Completed, PartiallyDelivered} **且** 投递文本长度 > 0 才记账 | `src/Pry.Api/Runtime/CharacterIntelligenceGateway.cs:263-265` | `……` 长度是 2 ⇒ 放行 |
| 4 | 投递文本由投递账本逐条累加；同会话在飞不止一条时干脆不记 | 同文件 `:183`（`sole!.Delivered.Add(text)`）、`:181`（`inFlight != 1` 即返回） | 归属判定本来是对的，本刀不许碰它（那是 `DEF-P3SEAM-02` 已经花过一刀的地方） |
| 5 | Core 的记账入口只看 `DeliveredToUser` 一个布尔 | `src/Pry.Core/Intelligence/CharacterIntelligenceService.cs:82` | 一旦网关把占位符轮标成"已投递"，下面四步全部执行，Core 侧无从分辨 |
| 6 | **被污染的第一条账**：状态推进（关系、熟悉度、`exchange_count`） | 同文件 `:91`（`NoteExchangeAsync`，`RequestedRelationshipCorrection`／`Rejected` 分支在 `:85-89`） | 原登记的真机读数 n=1：`exchange_count` 1→2、`familiarity` 0.132→0.144 |
| 7 | **被污染的第二条账**：经验日志多一行 | 同文件 `:94-95` → `:133-143`（`AppendAsync` 在 `:140`），仅当本轮 `Intent != DoNothing` | `behavior_experience` 里那一行记的是"这个场景用这个意图，成功了" |
| 8 | **被污染的第三条账（原登记没点名，今天新读到）**：本轮打算用的口癖被记成用过一次，`UsageCount++`、`LastUsedAt` 写库 | `:97` → `src/Pry.Core/Expression/ExpressionService.cs:40-54`（`UsageCount++` 在 `:49`，落库 `:52`） | 口癖的轮换／冷却／排序都吃这个计数 ⇒ 她被"没说出话的一轮"推着把某个习惯算成已用 |
| 9 | **两条不成立的污染，别写进判据**：① `behavior_pattern` 不加权；② 口癖候选不产生 | `src/Pry.Core/Intelligence/BehaviorPattern.cs:133-134`（要有明确反馈＋非空后续证据才有信号）、`:139`（信号为 null 直接返回）；`src/Pry.Core/Expression/ExpressionLearner.cs:134-136`（`Observe` 抽取为空 ⇒ 循环零次） | 原登记 2026-10-14 那句"每次投递都被排空进证据账"**过强，本单一并更正**：排空确实跑了，但对 `……` 零抽取、不进证据账 |
| 10 | 现有判据抓不到这个形状：`An_undelivered_turn_teaches_nothing` 守的是"一笔都没投出去" | `tests/Pry.Api.Tests/CharacterIntelligenceSeamTests.cs`（原登记：三处接缝突变还原后仍 10/0） | 这是**判据覆盖空档**，不是假绿；本刀第 1 条判据今天必须红 |
| 11 | 跨层消息结构没有"这是占位"的字段，只有贴纸判定 | `src/Pry.Core/Runtime/RuntimeModels.cs:127-131`（`IsSticker`） | 想加字段就是改跨层线格式，见 §3 反向约束第 6 条 |
| 12 | 外发端口签名是冻结面 | `src/Pry.Core/Runtime/RuntimePorts.cs:112`、`:114`（`MessageDeliveredAsync`） | 契约 §7.1 结论 1；`DEF-P1WALK-03` 那条事件流判据牵动于此 |
| 13 | 占位符**为什么**会出现（首句只拿到 `……`）是另一条编号 | `DEF-P1WALK-07`，Owner＝Runtime／Provider 档位 | 明令不许与本条合并处置；本刀不许改任何可见文本 |

## 2. 完成标准（出判面）

1. **今天必红**：真 SQLite + 真网关 + 真投递路径下，"投出去的内容只有占位符"的那一轮 ⇒ `behavior_experience` 零新增、`character_state` 的 `exchange_count`／`familiarity`／`mood` 一字不动。
2. **今天必红**：同一轮里她原本打算用的那个口癖，`UsageCount` 不 ++、`LastUsedAt` 不动。
3. **同源**："这一轮有没有可见答复"的判定只有一处定义，且在 Core；网关调它，不在 `src/Pry.Api` 里出现 `"……"` 字面量。用一条源码门钉住（扫描面＝`src/`，测试面按惯例豁免），并带**扫描器自证的正向探针**——先例是 `QA-TIP1015-01` 那个教训：守卫的扫描面本身要被钉住。
4. **同一档形状同一个出口**：`ModelReplyShape.Empty`／`OnlyInternalDraft` 与占位符走同一个判定；不许写出第二套"什么算空"（这条与 `DEF-P1WALK-07` 共用，两处不得分叉）。
5. **不许过度修复**：`delivered.Length == 0` 那道闸照旧有效，`An_undelivered_turn_teaches_nothing` 与另两条既有接缝判据一字不改仍要绿；另补一条对照例——本轮有真正文（哪怕同轮还附了一条占位）时，记账照旧。
6. **历史错账要有口径**（原登记完成标准④）：三选一落笔——回滚／标记忽略／"历史行不动、只保证今后不再记"。**落笔权在作者与 Owner**；推荐第三项（数据量小、且回滚要碰的表与 `memory_deletion_fence` 那套永久语义离得太近）。
7. 每条新判据逐个**突变反证**并字节级还原，报告里点名"哪一处突变让哪一条转红"；至少含：把同源判定退回纯长度 ⇒ 第 1、2 条红；把 `NoteUsedAsync` 整段摘掉 ⇒ 第 5 条红。
8. `docs/phase2-runtime-contract.md` 若被牵动（端口住处／线格式），**文档先行**，且改动归 Owner 原文，不由守门人代写。
9. 门禁：`-t:Rebuild` 0 警告 0 错误；解决方案级按程序集分别跑、各自唯一 TRX 落新目录，连续 ≥2 次全绿，**同场列出全部跑法与每一个红例名**；总数写全四项分项；`measure-fixed-eval.ps1` 仍 75 条永久场景且 `exit=0`；`verify-repository.ps1` 全过；`git diff --check` 干净。

## 3. 反向约束（不许走的捷径，开工前逐条读）

1. 不得为了让这条绿而在网关新写第三套信封解析——`No_other_layer_recognises_the_reply_envelope` 那条源码门正是禁这件事的（判据本体归 `DEF-P1WALK-01`）。
2. 不得反过来把 `delivered.Length > 0` 整条删掉：那会连真答复的教学一起关掉。
3. 不得改 `IRuntimeOutputSink`／`MessageDeliveredAsync` 的端口签名（契约 §7.1 结论 1 冻结，且牵动 `DEF-P1WALK-03`）。
4. 不得给 `RuntimePlannedMessage` 加"是不是占位"的字段——那是跨层线格式，比本刀欠账更大；判定住在 Core，网关只调用。
5. 不得把 `ModelReplyFormat` 整个类转 `public` 来省事；要暴露就暴露**一个**最小判定口（先例＝上一刀 Core 只加读口 `ShapesIn`）。
6. 不得与 `DEF-P1WALK-07` 合并处置；不得顺手把 `DEF-P3SEAM-01`／`-02`／`-03` "捎带修一遍"——各条载体读数与反证各归各的，同刀会破 §5.0 判据④"读数不得互顶"。
7. 不得顺手接 `ObserveFeedbackAsync`（那是 `RT-006`／Phase 6 的活，本刀零相关）。
8. 不得改任何关系／熟悉度的数值或步长：那些未标定阈值属第 6 层标定轮，`2-D` 的"先接线后标定"门不因本刀解除。
9. 分层照旧：Core 不依赖 Avalonia 或 HTTP，客户端不新增 SQLite 直接依赖；schema 一字不动。
10. 单一写者登记：`CharacterIntelligenceGateway.cs`、`ReplyPlanner.cs`、`CharacterIntelligenceService.cs`、`ExpressionService.cs` 同期只允许本 lane 写；`MainWindow.axaml.cs` 家族不碰。文档文本与代码**分刀提交**。

## 4. 推荐形状

- **A（推荐）**：在 Core 提一个单一来源的可见性判定（占位常量 + 一个 `IsUnanswered` 之类的公开最小方法，`ReplyPlanner` 那侧改为引用它），网关在 `:263-265` 那道闸旁边**追加**一个条件：本轮存在非占位的可见内容才允许记账。立刻得到＝三条账一起干净、判定只有一处、可见文本一字不改；付出＝Core 多一个公开成员；**可逆**（摘掉那个条件即回到今天）。
- **B**：网关直接比字符串常量。能工作，但把"什么算空"写成两处，完成标准③④必红，不推荐。
- **C（拒绝）**：把占位符换成空文本或不投。改的是用户可见行为，还连带破 `An_undelivered_turn_teaches_nothing` 的语义，不在本刀范围。

## 5. 门禁纪律（这一栏是踩过的坑，不是模板）

干净 `--detach` 载体上测，不在混工作树取数；`--no-build` 只跑刚构建过的那棵树；每个程序集 `--logger "trx;LogFileName=<唯一名>.trx"` 落**新目录**，只认逐个文件的 `<Counters>`；两条环境跳过按 `outcome="NotExecuted"` 点名；PowerShell 只用 `powershell.exe`（5.1），路径不要写在会被变量插值吞掉的引号里（10-16 那次 `measure` exit=1 就是这么来的，属仪器前置不成立，**不许动阈值或断言来凑绿**）；隔离 `Pry__DataDirectory`，绝不打开作者 live 数据目录；不 `git add -A`、不 amend、不 `--no-verify`、不 push、不打 tag。

## 6. 签收与随刀文档

| 角色 | 动作 | 产物 |
|---|---|---|
| Owner（Intelligence） | 实现 + 自跑 + 逐个突变反证 | 主工作区，形成可指认提交需作者点头 |
| QA | 从该 SHA 建干净载体复采、独立突变、出报告号 | `docs/eval/reports/eval-QA-*.md` |
| 作者 | 状态词与第 6 条的历史账口径落笔 | `PROJECT_PLAN.md` §4.2 / §5.0 |

随刀文档：`docs/character-intelligence.md`（学习入口边界一节）、`PROJECT_PLAN.md` §4.2 该条状态词；`CHANGELOG.md` 各 lane 分刀归位，不混提。

## 7. 本刀不解除的门（照旧有效）

`2-D` 先接线后标定门；第 6 层标定轮（Release 前置）；`0-6`／`DEF-P1WALK-05`（换后端后恢复）；`QA-TIP1015-01`（源码门不扫决策本体）；`0-2` 那 6 条判据的**换人**非空转复判；§6 裁定③（`PLAT-004`／`007` 进不进本轮）；那 7 份文档文本的分刀归位；QA 从 `c608220` 的复采。

### 附：Phase 3 → Phase 4 之间的差额（本单写成时实测）

只有第 1 项是产品代码，其余三项是验收动作：

1. 本刀 `DEF-P3SEAM-04`（待认领）。
2. `0-2` 那 6 条判据的非空转复判要**换人**——判据与突变出自同一角色，按 §8.2 不签。
3. QA 从线尾 `c608220` 建干净载体复采一次（该 SHA 是 `eb01acf`／`398101a`／`b15ed9d`／`f6c9750`／`2af02c2`／`dd91d2c` 的后代，采一次同时结掉那一整串）。
4. 你一句结案词（§5.0 状态词与阶段指针不由 Agent 写）。

**两处台账过期事实（本单就地更正，不改 Owner 原文）**：① `PROJECT_PLAN.md` §4.2 `RT-003` 行 2026-09-28 那句"落库侧仍缺，且只有两处"（SQLite 实现、`PurgeAsync` 调用点）已随 `b66efc8` 入库，四个适配器注册在 `src/Pry.Api/Program.cs:63-70`，清空调用点在 `src/Pry.Api/Services/ConfigurationApplicationService.cs:64`；② `DEF-P3SEAM-04` 原登记 10-14 那句"每次投递都被排空进证据账"过强，见 §1 第 9 行。Phase 4 的活本身（`RT-004-DRAIN` + `DEF-P4LEARN-01`）两把锁都已入库 ⇒ **到 Phase 4 缺的不是功能，是验收面**。
