# HDS-WAVE32-1028 · HDS-007 有界连续推进：最后 5 枚无红格的红证补取（证据刀，零落地）

- 工单编号：`HDS-WAVE32-1028`
- 出闸时点：2026-10-28（守门人；本单所有日期一律按盘上时间写，不沿用任何前一天的出闸日）
- 认领人：**守门人自起 lane**（作者 2026-10-22 明令「你，还是自己起Agent吧」，本单不交给作者转派）
- 状态：进行中（在飞 1）
- 载体：`.local-notes/qa-wave29-1024/carrier`（detached `663d2c9d78d7f607137f10cd2af09069b0a4f841`）——**复用第三十／三十一波那同一棵，不新建 worktree**
- 一句话范围：这一把仍然**不写新判据、不写新产品码、不落地、不顶账**，只做一件事——把第三十一波终账里剩下的 **5 枚**"没在改坏产品码之下响过"的格，按事前论证好的**独立方向**逐枚逼响；理想形状是五轮各响 1 枚、每枚都是它自己的清洁凭证。
- 上游权威文本：`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS-007 行与 §9；判据与本单冲突时以 PROJECT_PLAN 与 `docs/agent-engineering-standards.md` 为准，本单不得自行放宽任何一条。
- 前一把的原文：`docs/eval/reports/2026-10-24-hds007-bounded-advancement-prototype.md` §十.2（lane 的终账表）与 §十一.5（守门人的押账队列，本单五轮就是从那张表抄出来的）。

---

## 〇 起跑前置（每一件都要在盘上自证，自证不成立就停手并如实交回）

### 〇.1 载体形状

```
git -C C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave29-1024/carrier rev-parse HEAD
git -C C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave29-1024/carrier status --short
```

第一条必须读到 `663d2c9d78d7f607137f10cd2af09069b0a4f841`；第二条必须 **0 行**（第三十一波跑完已全部逐字节还原，并且那一把**零提交**）。非 0 行 ⇒ 本刀**不得起跑**，把那几行原样抄进报告 §十二.0 并交回，等守门人处置——不要"顺手还原"，那不是你的活。

### 〇.2 载体字节等于线尾（本刀允许跳过绿基线轮 G0 的唯一合法性来源）

**当场**跑（注意第二条实参是现取的线尾，不许照抄本单里任何一枚 SHA）：

```
cd C:/Users/24782/Desktop/Pry_Project
git diff --stat 663d2c9d78d7f607137f10cd2af09069b0a4f841 $(git rev-parse HEAD) -- src tests scripts Pry.slnx Directory.Build.props
```

必须 **0 行**。0 行 ⇒ 允许跳过 G0，理由是全部 23 枚格在 `663d2c9` 上已由第三十波取过全绿读数、此后 `src`／`tests` 五面字节未变。非 0 行 ⇒ **必须跑 G0**（占一枚定向额度），并把差异原文进报告。

> 这条"右侧现取"是本仓立的口径：第三十一波那单的 §〇.2 把线尾写死成 `3044c1c`，之后线尾又被守门人的收口刀挪过一格；照抄就是拿一枚过期 SHA 去做合法性证明。**报告里要写你实际用的那条命令与当时的 `git rev-parse HEAD` 全值。**

### 〇.3 起点在册基线（从盘上取，不许从本单抄）

去读 `docs/eval/reports/2026-10-24-hds007-bounded-advancement-prototype.md` §十一.3 第 2 条：现值＝**1380／executed 1378／failed 0／跳过 2**，分项 Client 108／App 233／Api 280／Core total 759（executed 757）。本刀**不移动这个数**（见 §二）。

### 〇.4 还原基准 SHA（按载体基点取，不要抄线尾）

覆盖前先自取、还原后再核一次，三处逐字符相同才算还原干净。取法＝`git -C <载体> cat-file blob HEAD:<路径>` 管道给 `sha256sum`：

| 文件 | 载体 `HEAD` blob 的 SHA-256 前缀 | 本刀是否临时改它 |
|---|---|---|
| `src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs` | `90947162fed797f8…` | 是（L1／L2／L4／L5 四轮） |
| `src/Pry.Core/Narrative/Advancement/DueIntentRegistry.cs` | `9bf4dc31bd3d1c72…` | 否（仅作形状对照） |
| `src/Pry.Core/Narrative/Advancement/AdvancementContracts.cs` | `259474dcccbfb1f0…` | 否（仅作形状对照） |
| `tests/Pry.Core.Tests/BoundedAdvancementPrototypeTests.cs` | `1fd944f94d399a8f…` | **否，一个字节都不许改**（本刀没有 §一.3 那种例外） |

本刀与第三十一波的一处形状差别＝**判据文件零写入**（那一把的 M3/M4 动过判据、所以单独归类；这一把五轮全在产品文件里，故每一轮的"取红手不是判据作者"这条资格天然成立，不用另证）。

### 〇.5 焦点预检（每一次 `dotnet` 之前一枚，无例外）

写法固定：

```
powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1
```

归档到 `.local-notes/wave29-1024/out/preflight-P<nn>.txt`，**编号从 P30 起**（P1–P29 是前两把的，不许覆盖）。读到 `BUILD_OK` 才起那一轮；读到 `HOLD` 就停下，不请示作者、不改判据、不绕过，等下一枚预检。`HOLD` 之后重跑的额度 ≤8 枚**按刀计**（不按日计）；用完还拿不到 `BUILD_OK` ⇒ 本刀以"零 `dotnet`"交回，这是合法交付，不是失败。

### 〇.6 仪器自证（开跑前置，零 `dotnet`）

1. **上一把留的那条 `round.py` 前置已作废。** 第三十波报告 §九.4 第 2 条登记它在 Python 3.14 下抛 `SyntaxError`；守门人本把按同一条口径重取过：`py -3.14` 的 `py_compile` 与 `ast.parse` 都通过、**未复现**（lane 在 §十.0 〇.6 也独立登记了未复现）。所以**本刀不要求先修它**，也不许把它当阻塞项交回。要用就自己验一次打印路径，验不过就自写小的。
2. **数红脚本必须先复现第三十一波在盘上的已知读数**，用同一棵树上的三份既有 TRX 当样本（守门人本把实测）：
   - `out/trx-M1` ⇒ 1 份 `.trx`，`total=759／executed=757／failed=1`，红格名 `A_late_advancement_carrying_a_superseded_effect_version_is_refused_and_writes_nothing`；
   - `out/trx-M5` ⇒ 同份形状，`failed=3`，三枚格名（`A_due_intent_that_has_not_reached_its_due_instant…`／`Replay_delivers_each_due_intent_exactly_once…`／`After_a_clock_forward_jump_due_intents_are_replayed_in_registration_order`）；
   - `out/trx-F31` ⇒ 4 份 `.trx`，逐份 `total` 应为 108／759／280／233，`failed` 全 0，两条同名跳过各出现一次。
   三份样本任一对不上 ⇒ 是仪器坏了，停下别起跑。参照实现＝`.local-notes/wave29-1024/out/gk31-count.js`（守门人那份，口径见下条），可以读它、不许改它。
3. 数红的口径（照第三十波 §九.4 第 1 条原文抄一份进来，免得下一只手再踩）：

> 只数 `<UnitTestResult … outcome="Failed" …>` 这**一个开标签**形状里的 `outcome` 属性；`<ResultSummary outcome="Failed">` 是文件级汇总、不是失败用例；`<Output>` 里的错误文本也不提供用例数。报"恰 N 枚红"之前，N 必须**同时**等于开标签计数与同份 `<Counters failed=…>` 的值；两侧不同值 ⇒ 是仪器坏了，不是读数变了。跳过只按 `<UnitTestResult … outcome="NotExecuted">` 点名，`notExecuted` 计数器恒 0，不看。

---

## 一 范围与不许走的捷径

### 一.1 本刀做的（只有这一件）

按 §三 那张表把五枚突变逐枚施加、逐轮取红、逐轮还原：**计划轮 L1–L5 ＋ 机动位 L6**（用法见 §五.2）。五轮的目标格互不相同，合起来正好覆盖第三十一波剩下那 5 枚。

### 一.2 不许走的捷径（反向约束，一条都不许）

1. **不许新增任何数字门槛。** 冷却时长、静默窗口、预算默认值在原型里一处数字都没有，那是作者定的特性不是缺陷；不许为了拿到红而"顺手补个常量"。L4 那枚改的是**分支条件的合取形状**，不是往预算上加数。
2. **不许把"这一格红不了"当结论而不试。** 登记"够不着"之前必须先穷举三类不碰门槛的方向：**方向翻转／删赋值／删登记**，并把穷举过程写进报告。第三十一波 §十一.4 与波 28 都有人在这条上被判据作者证伪过。
3. **不许改判据文件、不许改扫描门、不许碰注释里的 token**（本刀没有 §一.3 例外）。
4. **不许把批次红署给单枚突变。** 一枚突变连带响几格时，红之前就要事前点名那几格的格名并核对枚数；已领过清洁红的格在批次里再红一次＝"同一格的第二条因"，不重复领凭证、也不撤销原归属。本刀五轮都按"恰 1 枚"设计，一旦实测多响就是**事前论证错了**，照实登记作废并把错因写进报告——不许回头改预期。
5. **突变轮一律不带 `--no-build`**（带 `--no-build` 跑的是上一轮的 IL，那是本仓踩过一次的假门禁）。收尾全量 F 也不带 `--no-build`：它前面是 L5 或机动位的突变态还原，让它自行增量重建。
6. **不许在主工作树跑任何 `dotnet`**；载体之外零写入。
7. **不许放宽、删除、注释掉任何既有断言**来凑绿；`Assert.Skip` 不算跳过。
8. **不许改报告 §〇–§十一 的任何原文**；本刀只在文件末尾追加一节（见 §六）。
9. **不许自封在册基线、不许写状态词或结案词**。归位、顶账、换手签账三本账都由守门人做。
10. **不许碰** `licenses/**`、`licenses/THIRD-PARTY-NOTICES.md`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md`、`%LOCALAPPDATA%\PryCompanion`、任何模型／运行库／数据库／密钥／日志文件。
11. **构建必须 0 警告。** 上一把的 M1 差点写成自比较（CS1718）；本表所有改法都刻意避开"常量与自身比较""未用到变量""不可达语句"三种形状（尤其别写 `if (false && …)`，那会产出 CS0162）。

### 一.3 五轮的设计依据（本单替你数过的消费点，跑之前自己复述一遍）

这五枚格在第三十／三十一波之所以拿不到红，都是**因为预算闸与观察日志是三条格共用的**。所以选方向的第一判据＝"只有一格读它"。守门人 10-28 当场按盘上原文把每枚方向的消费点数全：

- 调用预算闸 `:85` 被三格共用（`An_exhausted_call_budget…`、`A_budget_that_is_zero_at_entry…`、`An_advancement_that_exactly_consumes…`）⇒ 单摘它必然批次红 ⇒ L4 改的是"入参即零被当成不设限"那半句合取，只有 `AdvancementBudget(0, 0, …)` 那格能走到（判据 `:204`／`:207` 是全文件仅有的两处 `TotalCalls = 0`）。
- 费用预算闸 `:94` 只有一格能走到（判据 `:225` 的 `remainingCost = 0.20m < 0.25m`；其余样本要么 `GenerousBudget`＝剩余 10m，要么先被调用闸拦下）⇒ 摘它是单响。
- 静默放行后的 `_deferrals.RemoveAll` `:120` 只被 `Assert.Empty(coordinator.PendingDeferrals)`（判据 `:170`）读；另一处 `PendingDeferrals` 断言是 `Assert.Single`（判据 `:143`，那一格之后没有成功推进）⇒ 删它是单响。
- 冷却表的写入 `:119` 只有"同一意图第二次进来"才可能被读到，而全文件里期望被冷却拦下的只有 `The_same_intent_resubmitted…` 一格；外沿那格（`:110`）与静默补推那格（`:152`）期望的都是放行 ⇒ 删写入是单响。
- 调用闸那格的**独有**读数只有 `OutcomeReason == "remaining_calls_below_required_calls"`（判据 `:189`，全文件唯一一处断言；`:209` 那格断的是 `StoppedByBudget` 与维度，不断这句）⇒ 改这句文本是单响，代价是它属"诊断文本面"而非"行为面"，报告里要照这个身份写，不许冒充行为突变。

---

## 二 落地、顶账与签账（本刀三本账都不动）

- **零落地**：本刀不产生任何要进主线的新字节，因此**没有归位动作**，也没有"扣住归位"这件事——没有字节就没有裁决（第三十一波立的这条与波 29 那条"字节写完但没响过 ⇒ 不归位"是两种不同的原因）。
- **零顶账**：在册 1380 不因本刀移动。收尾 F 的价值是两条：① 全部还原之后门禁重新全绿，证明这一把的手没留下任何漂移；② 让 1380 再多一份由**另一双手**在同一棵树上取到的同值复采读数（登记为"同值复采"，**不改写在册数**，改写权本来就在独立复采那把）。
- **换手签账**：由守门人做。本刀的资格比前两把更干净——判据字节零写入 ⇒ "取红那只手不是判据作者、且没改过判据一个字节"两半同时成立，lane 只要把每轮十一项字段与 §〇.4 的 SHA 对照交回即可，不要自己判。

---

## 三 事前论证（五枚计划轮＋一枚机动位，红格名逐字给出——跑之前先复述，跑完与 TRX 逐字符比对）

**格名一律从判据文件原文抄。** 预期与实测不符（多响、少响、消息形状不同）那一轮**判作废**，逐字节还原后把"预期 vs 实测"两栏都写进报告，再决定要不要用机动位复取。

| 轮 | 目标文件:行 | 改法（原文 → 突变） | 预期红（逐字点名） | 预期红点与消息形状 | 新增凭证枚数 |
|---|---|---|---|---|---|
| L1 | `AdvancementCoordinator.cs:88` | `"remaining_calls_below_required_calls"` → `"remaining_calls_below_required_amount"`（只改这一枚字面量；`:97` 费用那句**不动**） | `An_exhausted_call_budget_stops_advancement_and_names_consumed_remaining_and_the_dimension` | 恰 1 枚；红点判据 `:189`，Expected `"remaining_calls_below_required_calls"`／Actual `"remaining_calls_below_required_amount"`；`:188` 的 `StoppedByBudget` 与 `:195` 的观察日志保持绿（这一枚只弄脏诊断文本＝**文本面突变**，报告要照这个身份写） | 1 |
| L2 | `AdvancementCoordinator.cs:94`（连同 `:95`–`:101` 那一整个 `if` 块连花括号一起删） | 删掉费用闸（不许写 `if (false && …)`，那会产 CS0162） | `A_cost_budget_running_out_stops_advancement_while_calls_are_still_available` | 恰 1 枚；红点判据 `:227`，Expected `StoppedByBudget`／Actual `Advanced`（`remainingCost = 0.20m` 不再被拦，代次 0 相等 ⇒ 直接产记录）；`A_budget_that_is_zero_at_entry…` 先被调用闸拦下故**不经过这步、保持绿** | 1 |
| L3 | `AdvancementCoordinator.cs:120` | 删掉 `_deferrals.RemoveAll(x => x.IntentKey == request.IntentKey);` 整行 | `A_quiet_deferred_intent_advances_once_the_window_closes_and_leaves_the_pending_list` | 恰 1 枚；红点判据 `:170`，`Assert.Empty` 收到那一条 `DeferredByQuiet:intent-window` 的遗留登记；同一格的 `:172` 观察日志断言在这一枚下仍为真（所以红只能落在 `:170`，落在别处＝形状不符）；`A_due_intent_inside_the_quiet_window…` 断的是 `Assert.Single`（判据 `:143`）且其后没有成功推进 ⇒ **保持绿** | 1 |
| L4 | `AdvancementCoordinator.cs:85` | `if (remainingCalls < request.Budget.RequiredCallsPerAdvancement)` → `if (remainingCalls < request.Budget.RequiredCallsPerAdvancement && request.Budget.TotalCalls > 0)`（＝"入参即零"与"跑光"合流成不设限） | `A_budget_that_is_zero_at_entry_is_a_distinct_shape_from_one_that_ran_out` | 恰 1 枚；红点判据 `:210`，Expected `"calls"`／Actual `"cost"`（调用闸被绕过之后落到费用闸，那里 `remainingCost = 0m < 0.25m`）；`An_exhausted_call_budget…`（`TotalCalls = 3`）与 `An_advancement_that_exactly_consumes…`（`TotalCalls = 3`）都不成立那一半合取 ⇒ **保持绿** | 1 |
| L5 | `AdvancementCoordinator.cs:119` | 删掉 `_lastAdvancedByIntent[request.IntentKey] = request.Window.NowUtc;` 整行（＝冷却从未记住"上一次生效在何时"） | `The_same_intent_resubmitted_inside_the_cooldown_window_is_deferred_and_names_the_earliest_next_instant` | 恰 1 枚；红点判据 `:99`，Expected `DeferredByCooldown`／Actual `Advanced`（表里没有条目 ⇒ `:59` 那道闸永远不成立）；外沿那格 `An_intent_resubmitted_at_the_instant_the_cooldown_window_closes_advances_again` 期望的本来就是放行 ⇒ **保持绿**；`An_exhausted_call_budget…` 第二次进来先跳过冷却、仍在调用闸停住 ⇒ **保持绿** | 1 |
| L6（机动位） | `AdvancementCoordinator.cs:59` | 冷却闸方向翻转：`cooldownEnd > request.Window.NowUtc` → `cooldownEnd < request.Window.NowUtc` | **一次恰 2 枚（批次红，不许署给单枚）**：`The_same_intent_resubmitted_inside_the_cooldown_window_is_deferred_and_names_the_earliest_next_instant`＋`An_exhausted_call_budget_stops_advancement_and_names_consumed_remaining_and_the_dimension` | 这一枚的实测枚数**已由第三十一波 M8 在盘上量过一次**（`out/trx-M8`＝`failed=2`，那两枚格名逐字见 §〇.6 第 2 条同源目录），所以本行不是猜的：前者红点 `:99` Expected `DeferredByCooldown`／Actual `Advanced`，后者红点 `:188` Expected `StoppedByBudget`／Actual `DeferredByCooldown`；外沿那格因两侧相等不成立而**保持绿** | 0（两枚都已在 L1／L5 领过，这一枚只作为**行为面的第二因**登记） |

> 两处要 lane 自己动脑子核验、但**零 `dotnet`**：
> ① 上表五枚的预期红点行号是守门人按当前判据文件原文（494 行版）逐行数的；你施加前请自己 `grep -n` 复述一遍，格名与行号任一不符就先改正报告、再起跑。
> ② 五枚突变的**预合 SHA-256** 本单没有替你算——按第二十九波立的通用口径，在临时副本上零 `dotnet` 预先合出突变态字节并取 SHA，把五枚预合值补进报告 §十二.0；施加后 TRX 那一份文件的 SHA 必须逐字符等于预合值，不等＝那一轮的归属作废。

### 三.1 终账形状（跑之前先写死，跑完只许填实测）

若 L1–L5 全部形状相符：**新增红证 5 枚**（每枚各是一格的清洁凭证，无批次、无第二因），第三十一波剩下的那 5 枚无红格**结清 5 枚、剩余 0 枚**。机动位 L6 只有在五轮里出现作废轮时才允许用（复取那一轮），五轮全相符时用它跑一次批次并登记为"两枚已付格的第二条因"（不领新凭证）。

至于这把之后 HDS-007 第一片那 23 枚格的总覆盖度怎么算（含波 28 类账与旧欠 75 格），**由守门人算**；lane 只交自己这五轮的实测，不许自己写"全部结清"这类话。

**这个数字不许你事后改判**：形状不符的轮次作废了就是作废，照实登记"剩余 1 枚／2 枚／3 枚"，别为了凑 5 去追第二轮。

---

## 四 每轮十一项字段（照第三十波 §八.1 的模板，一项不缺）

① 轮名与目标文件 ② 预检文件名与 `VERDICT` ③ 改动原文（改前整行→改后整行）④ `git diff --stat`（几文件几入几出）⑤ 跑法整行命令（含工作目录与 `--results-directory` 绝对路径）⑥ TRX 文件名与枚数 ⑦ `<Counters>` 四值原文（total／executed／passed／failed）⑧ 红格 `testName` 逐字（含 `[Theory]` 实例参数；本刀五轮都应当是 `Fact` 形状）⑨ 红点行与消息原文 ⑩ SHA-256 三态（覆盖前／突变态／还原后，突变态要与 §三 预合值相符）⑪ 本轮结论（相符／作废＋原因）。

**落盘节奏**：每跑完一轮立刻把这一轮的十一项追加进报告，不许攒到最后一次写（冷启动 lane 有 150 轮上限，攒着写＝活干了账没了）。

---

## 五 预算与停止条件

1. `dotnet` 调用 ≤**8** 次＝构建 B1 一次（`dotnet build Pry.slnx -c Release`，工作目录＝载体；同时取"0 警告"读数）＋定向 ≤6 次（`dotnet test tests/Pry.Core.Tests -c Release --results-directory "<绝对路径>" --logger trx`，每轮独立目录 `out/trx-L<n>`，**不带** `--no-build`）＋收尾全量 F 一次（`dotnet test Pry.slnx -c Release --results-directory "…/out/trx-F32" --logger trx`，跑完先 `ls *.trx` 数到 4 份再逐份读 `<Counters>` 求和）。
2. 定向 ≤6 的用法：**计划 5 枚（L1–L5）＋机动 1 枚（L6）**。机动位二选一——五轮里有作废轮就用它复取那一轮（复取后仍不符就停手登记）；五轮全相符才允许用它跑 L6 那次批次。**不许**把机动位同时用于两处。
3. 焦点预检 ≤8 枚（每次 `dotnet` 前一枚，编号从 **P30** 起）＋ `HOLD` 重跑 ≤8 枚（按刀计）。额度用尽仍 `HOLD` ⇒ 以零 `dotnet` 交回。
4. `scripts/measure-fixed-eval.ps1` 只在 F 之后跑一次，且**必须带 `-ResultsDirectory` 指向 F 那一目录**（它会往该目录写并覆盖 `fixed-eval-coverage.json`，同目录复跑会抹掉上一轮覆盖读数；不带参数会 throw 而整段 exit 码照旧 0）。
5. 撞 150 轮上限不是完成；上限之前每一轮都已各自落盘才算交付。交回前做一次"数盘"：`ls out/trx-L*` 目录数、`.trx` 总枚数、`git -C <载体> status --short` 行数（**必须 0**）、§〇.4 四份 SHA 复核（含没改过的那三枚，逐枚核一遍才算"没漂移"）。

---

## 六 报告落点与追加规则

- 文件＝`docs/eval/reports/2026-10-24-hds007-bounded-advancement-prototype.md`（同一份报告继续追加，不新建）。
- 只允许在文件**末尾**追加一节，节名 `## 十二 第三十二波轮次记录（HDS-WAVE32-1028）`，其下自拟 `### 十二.0 起跑自证（含五枚预合 SHA 与 §〇.6 仪器自证）`／`### 十二.1 逐轮记录（L1…）`／`### 十二.2 终账（新增红证与剩余无红格逐格点名）`／`### 十二.3 未验证与押下一把`／`### 十二.4 本刀文件面`。
- §〇–§十一 是别人的原文与前两把的账，**一个字都不许改**（连文末空行也不许动）。追加完自证一次：`git diff --numstat -- <该文件>` 必须是 `N  0`（0 删除）。若你发现自己需要改前面某节才能说清一件事，**在 §十二 里写"对 §X.Y 的一处更正登记"**，不动原文。
- 报告里所有时间一律写盘上时间（`date` 与 TRX 文件名时间戳要对得上）。

---

## 七 本刀文件面（写死边界，越界即作废）

- 允许写入：载体上的 `src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs`（临时突变、当轮逐字节还原）、`.local-notes/wave29-1024/out/**`（预检、TRX、自写脚本、突变副本）、报告末尾那一节。
- **判据文件与另外两枚产品文件零写入**（这是本刀与第三十一波唯一的形状差）。
- **主工作树零写入**（除报告那一节之外，本刀的活全在载体与临时目录里）。**你不做任何提交**：git 提交动作全部归守门人。
- 主工作树上别人名下的两枚 `M ` 暂存项、五枚 `??` 未跟踪资产、以及 `docs/plans/PROJECT_PLAN.md` 那 18 行未提交的在飞研究登记，一律**不读内容、不改、不 `stash`／`clean`／`rm`**。

---

## 八 交回（守门人要看的六件事）

1. §〇.1／〇.2／〇.4／〇.6 四项自证的原文读数（尤其 〇.2 那条 `diff --stat` 的行数与右侧现取的线尾全值）。
2. 每轮十一项字段齐全，且**每轮的突变态 SHA 与 §十二.0 的预合值逐字符相符**。
3. 数红的两侧对账（开标签计数 == `<Counters failed>`）逐轮列出，不许只报"恰 N 枚红"。
4. 终账：新增红证逐枚点名（哪一格、红在哪一行、消息原文）、作废轮若有则连它当时那份绿一起登记、剩余无红格逐格点名并写清缺的是哪一类方向。
5. F 那四份 TRX 的逐份 `<Counters>` 与求和式（应当回到 1380／1378／0／跳过 2 同值；不同值就写不同值并说明差异来自哪一枚未还原的字节——**不许把差异圆成"环境噪声"**），以及 `measure-fixed-eval` 同场那份 `fixed-eval-coverage.json` 的四值。
6. 载体收尾 `status --short` 0 行＋§〇.4 四份 SHA 复核。

交回之后归位、顶账、换手三本账由守门人裁。**这一把没有新字节要落，最可能的正确结论还是"零落地、只结证据账"**——不要为了"看起来交付了东西"去写第二十八波那种数值锁格，也不要为凑红新增门槛。跑完这一把，HDS-007 第一片那 23 枚格除波 28 类账外就没有"从没在改坏码下响过"的欠账了；**但这仍然不等于接线、不等于可删旧链、不等于界面有任何变化**，那三句话照旧不许出现在任何位置。
