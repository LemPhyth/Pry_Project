# HDS-WAVE31-1028 · HDS-007 有界连续推进：16 枚无红格的红证补取（证据刀，零落地）

- 工单编号：`HDS-WAVE31-1028`
- 出闸时点：2026-10-28（守门人；本单所有日期一律按盘上时间写，不沿用任何前一天的出闸日）
- 认领人：**守门人自起 lane**（作者 2026-10-22 明令「你，还是自己起Agent吧」，本单不交给作者转派）
- 状态：进行中（在飞 1）
- 载体：`.local-notes/qa-wave29-1024/carrier`（detached `663d2c9d78d7f607137f10cd2af09069b0a4f841`）——**复用第三十波那同一棵，不新建 worktree**
- 一句话范围：这一把**不写新判据、不写新产品码、不落地、不顶账**，只做一件事——把第三十波终账里那 16 枚"没在改坏码之下响过"的格，按事前论证好的方向逐枚逼响，能响几枚算几枚，响不了的逐格登记缺的是哪一类方向。
- 上游权威文本：`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS-007 行与 §9；判据与本单冲突时以 PROJECT_PLAN 与 `docs/agent-engineering-standards.md` 为准，本单不得自行放宽任何一条。
- 前一把的原文：`docs/eval/reports/2026-10-24-hds007-bounded-advancement-prototype.md` §八.2（终账表）与 §九.5（本单就是从那张队列抄出来的）。

---

## 〇 起跑前置（每一件都要在盘上自证，自证不成立就停手并如实交回）

### 〇.1 载体形状

载体仍在 `663d2c9`，`git -C <载体> status --short` 必须是 **0 行**（第三十波跑完已全部逐字节还原）。非 0 行 ⇒ 本刀**不得起跑**，把非 0 的那几行原样抄进报告 §十.0 并交回，等守门人处置——不要"顺手还原"，那不是你的活。

### 〇.2 载体字节等于线尾（本刀可以不重跑绿基线的唯一合法性来源）

当场跑：

```
git -C C:/Users/24782/Desktop/Pry_Project diff --stat 663d2c9d78d7f607137f10cd2af09069b0a4f841 3044c1c -- src tests scripts Pry.slnx Directory.Build.props
```

**必须 0 行**（第三十波守门人已经用同一条式子证明过落地树＝产出 1380 那棵树；这一把只是复核它还没被后续提交挪动）。0 行 ⇒ 允许跳过绿基线轮（G0），理由是全部 23 枚格在 `663d2c9` 上已由第三十波 G0 取到全绿读数、其中 7 枚有清洁红，字节未变。非 0 行 ⇒ **必须跑 G0**（占一枚定向额度），并把差异原文进报告。

### 〇.3 起点在册基线（从盘上取，不许从本单抄）

去读 `docs/eval/reports/2026-10-24-hds007-bounded-advancement-prototype.md` §九.3 第 1 条：现值＝**1380／executed 1378／failed 0／跳过 2**，分项 Client 108／App 233／Api 280／Core total 759（executed 757）。本刀**不移动这个数**（见 §二）。

### 〇.4 还原基准 SHA（按载体基点取，不要抄线尾）

覆盖前先自取、还原后再核一次，三处逐字符相同才算还原干净：

| 文件 | 载体 `HEAD` blob 的 SHA-256 前缀 | 取法 |
|---|---|---|
| `src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs` | `90947162fed797f8…` | `git -C <载体> cat-file blob HEAD:src/.../AdvancementCoordinator.cs` 管道给 `sha256sum` |
| `src/Pry.Core/Narrative/Advancement/DueIntentRegistry.cs` | `9bf4dc31bd3d1c72…` | 同上 |
| `src/Pry.Core/Narrative/Advancement/AdvancementContracts.cs` | `259474dcccbfb1f0…` | 同上（本刀不动它，仅作形状对照） |
| `tests/Pry.Core.Tests/BoundedAdvancementPrototypeTests.cs` | `1fd944f94d399a8f…` | 同上（**本刀只有 M3/M4 两枚允许临时碰它**，见 §一.3） |

第三十波那把的教训照抄进本单：**续跑刀的"还原后应为"必须按载体基点重算**，线尾值与载体值在这五个面虽然相同，但你抄的是哪一处的值这件事本身要写进报告。

### 〇.5 焦点预检（每一次 `dotnet` 之前一枚，无例外）

写法固定：

```
powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1
```

归档到 `.local-notes/wave29-1024/out/preflight-P<nn>.txt`，**编号从 P20 起**（P10–P19 是上一把的 10 枚，不许覆盖）。读到 `BUILD_OK` 才起那一轮；读到 `HOLD` 就停下，不请示作者、不改判据、不绕过，等下一枚预检。`HOLD` 之后重跑的额度 ≤8 枚**按刀计**（不按日计）；用完还拿不到 `BUILD_OK` ⇒ 本刀以"零 `dotnet`"交回，这是合法交付，不是失败。

### 〇.6 仪器自证（本刀新增的一条开跑前置，零 `dotnet`）

上一把留的 `.local-notes/wave29-1024/round.py` 在 **Python 3.14 下打印失败消息那一段抛 `SyntaxError`**（守门人登记于第三十波报告 §九.4 第 2 条）。它的崩溃点落在"覆盖→跑→还原→核 SHA 与 status→数 TRX→读 `<Counters>`"全部完成之后，所以上一把七轮的凭据没受影响；但这一把我们**要读消息形状**（M5/M6 的期望消息是判读关键），所以：

- 要么先修自己那份（改完用盘上现成的第三十波 TRX 试跑打印路径，证明它取得到 `outcome="Failed"` 那枚的消息与红点行），要么自写一份；
- **不许为了省事直接沿用未自证的仪器**；取不到消息就报"红在某格"是假证据。

数红的口径照第三十波报告 §九.4 第 1 条，本单原文抄一份进来，免得下一只手再踩：

> 只数 `<UnitTestResult … outcome="Failed" …>` 这**一个开标签**形状里的 `outcome` 属性；`<ResultSummary outcome="Failed">` 是文件级汇总、不是失败用例；`<Output>` 里的错误文本也不提供用例数。报"恰 N 枚红"之前，N 必须**同时**等于开标签计数与同份 `<Counters failed=…>` 的值；两侧不同值 ⇒ 是仪器坏了，不是读数变了。跳过只按 `<UnitTestResult … outcome="NotExecuted">` 点名，`notExecuted` 计数器恒 0，不看。

---

## 一 范围与不许走的捷径

### 一.1 本刀做的（只有这一件）

按 §三 那张表把突变逐枚施加、逐轮取红、逐轮还原。**七枚计划轮 M1–M7 ＋ 一枚机动位**（机动位的用法见 §五.2）。

### 一.2 不许走的捷径（反向约束，一条都不许）

1. **不许新增任何数字门槛。** 冷却时长、静默窗口、预算默认值在原型里一处数字都没有，那是作者定的特性不是缺陷；不许为了拿到红而"顺手补个常量"。
2. **不许把"这一格红不了"当结论而不试。** 上一把之前有人这么写过两格，都被守门人亲手证伪（`ThenBy`→`ThenByDescending`、删 `Identity` 赋值）。登记"够不着"之前必须先穷举三类不碰门槛的方向：**方向翻转／删赋值／删登记**，并把穷举过程写进报告。
3. **不许用注释掉被扫 token 的方式做扫描门的反证。** 这类扫描器按口径把注释也算数，加 `//` 等于什么都没改；要改就把 token 本身改掉（§三 的 M3/M4 就是这么写的）。
4. **不许把批次红署给单枚突变。** 一枚突变连带响几格时，红之前就要事前点名那几格的格名（§三 已替你写好，跑之前逐字复述一遍并核对你打算报的红集与它是否相同）；已领过清洁红的格在批次里再红一次＝"同一格的第二条因"，不重复领凭证、也不撤销原归属。
5. **突变轮一律不带 `--no-build`**（带 `--no-build` 跑的是上一轮的 IL，那是本仓踩过一次的假门禁）。收尾全量 F 也不带 `--no-build`：它前面是 M7 或 M8 的突变态还原，让它自行增量重建。
6. **不许在主工作树跑任何 `dotnet`**；载体之外零写入。
7. **不许放宽、删除、注释掉任何既有断言**来凑绿；`Assert.Skip` 不算跳过。
8. **不许改报告 §〇–§九 的任何原文**；本刀只在文件末尾追加一节（见 §六）。
9. **不许自封在册基线、不许写状态词或结案词**。归位、顶账、换手签账三本账都由守门人做。
10. **不许碰** `licenses/**`、`licenses/THIRD-PARTY-NOTICES.md`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md`、`%LOCALAPPDATA%\PryCompanion`、任何模型／运行库／数据库／密钥／日志文件。

### 一.3 判据字节的临时突变（本刀唯一的例外，且单独归类）

M3 与 M4 这两枚的反证对象**本身就在判据文件里**（扫描器的实现函数）。这不违反"不改判据"硬线，前提三条全做到：

1. 跑完立刻 `git cat-file blob HEAD:<path>` 逐字节还原，还原后 SHA 逐字符等于 §〇.4 的 `1fd944f9…`；
2. 这两枚拿到的红**只署为"门在咬（反面探针结清）"**，不许写进"改坏产品码之下响过"那一类，也**不作为第二换手签账依据**——因为施加这一枚的那只手在这一轮里动过判据字节；
3. 报告里把这两枚与其余六枚分开列，不许并成一个数。

---

## 二 落地、顶账与签账（本刀三本账都不动）

- **零落地**：本刀不产生任何要进主线的新字节，因此**没有归位动作**，也没有"扣住归位"这件事——没有字节就没有裁决。
- **零顶账**：在册 1380 不因本刀移动。收尾 F 的价值是两条：① 全部还原之后门禁重新全绿，证明这一把的手没留下任何漂移；② 顺手让 1380 多一份由**另一双手**在同一棵树上取到的同值复采读数（登记为"同值复采"，**不改写在册数**，改写权本来就在独立复采那把）。
- **换手签账**：由守门人按第三十波立的资格判据做——取红那只手既不是判据字节的作者、也没改过一个判据字节（M3/M4 是显式例外，见 §一.3）。lane 只需把每轮十一项字段与 §〇.4 的 SHA 对照交回，不要自己判。

---

## 三 事前论证（八枚突变，红格名逐字给出——跑之前先复述，跑完与 TRX 逐字符比对）

**格名一律从判据文件原文抄，`[Theory]` 的实例参数照 TRX 的 `testName` 形状写全。** 预期与实测不符（多响、少响、消息形状不同）那一轮**判作废**，逐字节还原后把"预期 vs 实测"两栏都写进报告，再决定要不要用机动位复取。

| 轮 | 目标文件:行 | 改法（原文 → 突变） | 预期红（逐字点名） | 预期红点与消息形状 | 新增凭证枚数 |
|---|---|---|---|---|---|
| M1 | `AdvancementCoordinator.cs:105` | `if (request.ExpectedEffectVersion != _effectVersion)` → 同行加一道永不成立的合取 `if (request.ExpectedEffectVersion != _effectVersion && request.RequestId is null)`（**不许**写成 `!= request.ExpectedEffectVersion`，那是 CS1718 自比较告警，破坏"构建 0 警告"） | `A_late_advancement_carrying_a_superserved_effect_version…` ← **注意：格名以盘上原文为准，逐字符照抄判据文件 `:313` 那一行**，正确值是 `A_late_advancement_carrying_a_superseded_effect_version_is_refused_and_writes_nothing` | 恰 1 枚；红点 `:322`，Expected `RejectedForStaleEffect`／Actual `Advanced` | 1 |
| M2 | `AdvancementCoordinator.cs`（`Advance` 之后，同一行段内新增） | 新增一枚公开实例方法（非 special-name），例如 `public bool HasRecords() => _records.Count > 0;` | `The_advancement_coordinator_exposes_exactly_one_public_entry_method` | 恰 1 枚；红点 `:83`，Expected `"Advance"`／Actual `"Advance,HasRecords"` | 1 |
| M3 | 判据 `:468` | `LegacyChainNames.Where(name => source.Contains(name, StringComparison.Ordinal))` → 引用面翻转 `LegacyChainNames.Where(name => name.Contains(source, StringComparison.Ordinal))` | `Advancement_prototype_sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites` | 恰 1 枚；红点 `:417`，Expected 那串五个旧链类型名／Actual `""`；**前面那段 `Assert.Empty` 循环保持绿**（这正是"只有探针能发现门坏"的形状） | 1（反面探针类） |
| M4 | 判据 `:472` 与 `:473` | 把两个被扫 token 本身改掉：`"const "`→`"const_"`、`"static readonly"`→`"static_readonly"`（样本文本里都不出现＝扫描器被弄坏，不是被绕开） | `Advancement_prototype_declares_no_source_side_default_for_windows_or_budgets` | 恰 1 枚；红点 `:439`，Expected `"const,static readonly"`／Actual `""` | 1（反面探针类） |
| M5 | `DueIntentRegistry.cs:73` | 删掉 `entry.Replayed = true;`（盘上现成探针 `probe/unrun-replay-mark-removed.cs`，预合 SHA `cdd18e3f94896756d37ea4d2df5b7ce289dbee8ed45ca1e4346759bafb2feba0`） | **一次 3 枚（批次红，不许署给单枚）**：`Replay_delivers_each_due_intent_exactly_once_across_two_clock_jumps_and_loses_none`、`After_a_clock_forward_jump_due_intents_are_replayed_in_registration_order`、`A_due_intent_that_has_not_reached_its_due_instant_is_left_pending_and_still_reported` | 三枚的消息各自不同（第一次跳表／第二次跳表／`StillPending` 形状）；跑之前把这三枚格名照上表复述一遍，跑完逐枚对上 TRX 节点 | **1**（只有 `Replay_delivers…` 是新增；后两枚是已付格的第二条因） |
| M6 | `AdvancementCoordinator.cs:46` | `if (request.Source is null \|\| string.IsNullOrWhiteSpace(request.Source.SourceId))` → `if (request.Source is null ? false : string.IsNullOrWhiteSpace(request.Source.SourceId))`（空来源不再被拒，即摘掉来源闸） | **一次 4 枚（批次红）**：`Missing_source_is_refused_for_every_trigger_kind_with_the_same_named_failure` 的三个实例（`asked: UserEvent`／`DueIntent`／`BackgroundScene`，`requestId` 照 TRX 逐字）＋ `An_advancement_whose_source_cannot_be_pointed_to_fails_with_a_named_reason_and_stays_in_the_log` | **预期消息形状＝`System.NullReferenceException`**（摘闸之后 `:116` 直接解引用空来源）。这不是仪器故障，而是本单要采的一条正面事实：**来源闸摘掉后入口会崩，而不是明确拒绝**——原样进报告，不许写成"跑挂了" | **3**（三枚 Theory 实例）；第 4 枚是已付格的第二条因 |
| M7 | `AdvancementCoordinator.cs:116` | 成功路径的记录里把来源标识换成意图键：`request.Source.SourceId` → `request.IntentKey`（只改这一枚实参，`SourceVersion` 不动） | **一次 3 枚（批次红）**：`A_successful_advancement_carries_the_source_reference_into_the_record_it_produces`＋`Each_trigger_kind_enters_the_one_advancement_entry_and_names_its_own_reference` 的 `asked: UserEvent`／`asked: DueIntent` 两个实例 | 前两处红点＝判据 `:395` 与 `:67`，Expected `"src-record"`／`"src-1"`，Actual 为该格的 `intent` 串；`(asked: BackgroundScene, …)` 那一枚**也在同一轮红**＝已付格的第二条因，所以本枚预期**实为 4 枚红**，第 4 枚格名 `Each_trigger_kind…(asked: BackgroundScene, expectedReference: "bg-1")`，不领新凭证 | **3** |
| M8（机动位） | `AdvancementCoordinator.cs:59` | 冷却闸方向翻转：`cooldownEnd > request.Window.NowUtc` → `cooldownEnd < request.Window.NowUtc` | `The_same_intent_resubmitted_inside_the_cooldown_window_is_deferred_and_names_the_earliest_next_instant` | 恰 1 枚；红点 `:99`，Expected `DeferredByCooldown`／Actual `Advanced`；`An_intent_resubmitted_at_the_instant_the_cooldown_window_closes_advances_again` 在翻转后仍不拦（相等两侧都不成立）故**保持绿** | 1 |

> M1 那一格故意留了一处自我更正：上表第一版我把格名抄错过一次（`superserved`），当场按判据文件 `:313` 原文改正。**跑之前请你自己也逐字符核一遍八枚的预期格名与判据文件里的方法名是否相同**——格名错了这一轮就没有归属价值，而这一步零 `dotnet`。

### 三.1 终账形状（跑之前先写死，跑完只许填实测）

若 M1–M7 全部形状相符、机动位用作 M8：新增红证 **12 枚**（其中 M3/M4 两枚属"反面探针类"单列）＝M1 1＋M2 1＋M3 1＋M4 1＋M5 1＋M6 3＋M7 3＋M8 1；第三十波那 16 枚无红格**剩下 4 枚**仍无红，逐字点名＝`A_quiet_deferred_intent_advances_once_the_window_closes_and_leaves_the_pending_list`、`An_exhausted_call_budget_stops_advancement_and_names_consumed_remaining_and_the_dimension`、`A_budget_that_is_zero_at_entry_is_a_distinct_shape_from_one_that_ran_out`、`A_cost_budget_running_out_stops_advancement_while_calls_are_still_available`（这四枚的红方向各是独立摘一道闸或改一道闸的兜底支，本刀额度装不下，押下一把）。**这个数字不许你事后改判**：形状不符的轮次作废了就是作废，照实登记"剩余 5 枚／6 枚"，别为了凑 12 去追第二轮。

---

## 四 每轮十一项字段（照第三十波 §八.1 的模板，一项不缺）

① 轮名与目标文件 ② 预检文件名与 `VERDICT` ③ 改动原文（改前整行→改后整行）④ `git diff --stat`（几文件几入几出）⑤ 跑法整行命令（含工作目录与 `--results-directory` 绝对路径）⑥ TRX 文件名与枚数 ⑦ `<Counters>` 四值原文（total／executed／passed／failed）⑧ 红格 `testName` 逐字（含 `[Theory]` 实例参数）⑨ 红点行与消息原文 ⑩ SHA-256 三态（覆盖前／突变态／还原后，突变态要与 §三 预合值相符）⑪ 本轮结论（相符／作废＋原因）。

**落盘节奏**：每跑完一轮立刻把这一轮的十一项追加进报告，不许攒到最后一次写（冷启动 lane 有 150 轮上限，攒着写＝活干了账没了）。

---

## 五 预算与停止条件

1. `dotnet` 调用 ≤**10** 次＝构建 B1 一次（`dotnet build Pry.slnx -c Release`，工作目录＝载体）＋定向 ≤8 次（`dotnet test tests/Pry.Core.Tests -c Release --results-directory "<绝对路径>" --logger trx`，每轮独立目录 `out/trx-M<n>`，**不带** `--no-build`）＋收尾全量 F 一次（`dotnet test Pry.slnx -c Release --results-directory "…/out/trx-F31" --logger trx`，跑完先 `ls *.trx` 数到 4 份再逐份读 `<Counters>` 求和）。
2. 定向 ≤8 的用法：**计划 7 枚（M1–M7）＋机动 1 枚**。机动位二选一——若七轮里有作废轮就用它复取那一轮（复取后仍不符就停手登记）；七轮全相符才允许用它跑 M8。**不许**把机动位同时用于两处。
3. 焦点预检 ≤10 枚（每次 `dotnet` 前一枚，编号 P20 起）＋ `HOLD` 重跑 ≤8 枚（按刀计）。额度用尽仍 `HOLD` ⇒ 以零 `dotnet` 交回。
4. `scripts/measure-fixed-eval.ps1` 只在 F 之后跑一次，且**必须带 `-ResultsDirectory` 指向 F 那一目录**（它会往该目录写并覆盖 `fixed-eval-coverage.json`，同目录复跑会抹掉上一轮覆盖读数；不带参数会 throw 而整段 exit 码照旧 0）。
5. 撞 150 轮上限不是完成；上限之前每一轮都已各自落盘才算交付。交回前做一次"数盘"：`ls out/trx-M*` 目录数、`.trx` 总枚数、`git -C <载体> status --short` 行数（**必须 0**）、四份 §〇.4 SHA 复核。

---

## 六 报告落点与追加规则

- 文件＝`docs/eval/reports/2026-10-24-hds007-bounded-advancement-prototype.md`（同一份报告继续追加，不新建）。
- 只允许在文件**末尾**追加一节，节名 `## 十 第三十一波轮次记录（HDS-WAVE31-1028）`，其下自拟 `### 十.0 起跑自证`／`### 十.1 逐轮记录（M1…）`／`### 十.2 终账（新增红证与剩余无红格逐格点名）`／`### 十.3 未验证与押下一把`／`### 十.4 本刀文件面`。
- §〇–§九 是别人的原文与上一把的账，**一个字都不许改**（连文末空行也不许动）。追加完自证一次：`git diff --numstat -- <该文件>` 必须是 `N  0`（0 删除）。若你发现自己需要改前面某节才能说清一件事，**在 §十 里写"对 §X.Y 的一处更正登记"**，不动原文。
- 报告里所有时间一律写盘上时间（`date` 与 TRX 文件名时间戳要对得上）；上一把为这件事专门立过时点更正一节，本刀不要再犯。

---

## 七 本刀文件面（写死边界，越界即作废）

- 允许写入：载体上的那两份产品文件（临时突变、当轮还原）、判据文件（仅 M3/M4，当轮还原）、`.local-notes/wave29-1024/out/**`（预检、TRX、自写脚本、探针副本）、报告末尾那一节。
- **主工作树零写入**（除报告那一节之外，本刀的活全在载体与临时目录里）。**你不做任何提交**：git 提交动作全部归守门人。
- 主工作树上别人名下的两枚 `M ` 暂存项与五枚 `??` 未跟踪资产、以及 `docs/plans/PROJECT_PLAN.md` 那 18 行未提交的在飞研究登记，一律**不读内容、不改、不 `stash`／`clean`／`rm`**。

---

## 八 交回（守门人要看的六件事）

1. §〇.1／〇.2／〇.4／〇.6 四项自证的原文读数（尤其 〇.2 那条 `diff --stat` 的行数）。
2. 每轮十一项字段齐全，且**每轮的突变态 SHA 与 §三 预合值逐字符相符**（M2/M6/M7 这三枚本单没给预合值——你要在施加前于临时副本上零 `dotnet` 预证并把预合值补进 §十.0，这是上一把立的通用口径）。
3. 数红的两侧对账（开标签计数 == `<Counters failed>`）逐轮列出，不许只报"恰 N 枚红"。
4. 终账：新增红证逐枚点名（反面探针类单列）、剩余无红格逐格点名并写清缺的是哪一类方向、作废轮若有则含它当时那份绿一起登记。
5. F 那四份 TRX 的逐份 `<Counters>` 与求和式（应当回到 1380／1378／0／跳过 2 同值；不同值就写不同值并说明差异来自哪一枚未还原的字节——**不许把差异圆成"环境噪声"**）。
6. 载体收尾 `status --short` 0 行＋四份 SHA 复核。

交回之后归位、顶账、换手三本账由守门人裁。**这一把没有新字节要落，所以最可能的正确结论是"零落地、只结证据账"**——不要为了"看起来交付了东西"去写第二十八波那种数值锁格，也不要为凑红新增门槛。
