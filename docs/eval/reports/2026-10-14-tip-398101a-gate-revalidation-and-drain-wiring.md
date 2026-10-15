# 新提交 `398101a` 门禁复采 ＋ `RT-004-DRAIN` 排空半边 ＋ 裁定 A 在新载体复核 · 2026-10-14

- **Run ID**：`eval-QA-RT004-1014`
- **判定人**：QA / Evaluation（EVAL-001）
- **载体**：**一个 SHA**。我从 `398101a`（`feat(expression): give the learned-habit queue a background drainer`）新建自己的 `--detach` worktree `.local-notes/qa-tip-1014`，建完 `git status --short` **0 行**；本轮所有读数出自这棵树，跑完（含两次突变还原、一次判据临时拷入拷出）**仍是 0 行**。**没有复用任何其他角色在同一 SHA 上的结果目录。**
- **派工原话**（作者 2026-10-14）：「从新提交号建干净副本复采，出正式报告号」。
- **本载体上有两刀新内容**：`eb01acf`（长期记忆层 2-A/2-B 并线 ＋ 回复接缝三条修补 `DEF-P3SEAM-01/-02/-03` ＝ 执行基准的 `0-2`）与 `398101a`（`RT-004-DRAIN` 排空通道）。

## 0. 结论先写在最前面

1. **复采成立**：`-t:Rebuild` 构建 **0 警告／0 错误**；解决方案级**连续三次 819 通过／0 失败／2 跳过（总 821）**，三次分项逐位相同（Client 93／App 183／Api 164／Core 379+2skip）；`measure-fixed-eval.ps1` 对三个独立结果目录各 **`exit=0`**（目录仍 **75** 条永久场景）；`verify-repository.ps1` **433**；`git diff --check` 干净；一次全量的临时目录计数 **492→492 净零增**。⇒ **历轮"763／767"口径自本日起过期，现行门禁总数＝819 通过／2 跳过。**
2. **`RT-004-DRAIN` 的两处承重墙我都当场证伪过，不是空转**：M1 把 `AddHostedService<ExpressionDrainService>()` 注释掉 ⇒ **恰好 1 红**，红的正是那条守"生产接线"的源码门；M2 把真正的 `DrainAsync` 调用换成 `await Task.CompletedTask`（后台循环照转、预算照建）⇒ **8 红／2 绿**，红的全是行为判据。⇒ **我签这一刀的"排空半边"＝ Pass**：队列不再 64 满即丢这件事，现在既有生产者也有消费者，而且消费者被测试钉住。
3. **"她学到并会用"这一半仍然不成立，实测确认**：`src/` 内 `AcceptedExpressionStyles`／`ExpressionFeedbackId` 只有**定义与消费**（`CharacterIntelligenceService.cs:23-24`、`:111`、`:127-128`），**零填写点** ⇒ 提交一条 `Learned` 模式要两次显式认可，而认可能力今天没有入口。这正是 Owner 自己在提交信息里登记的 `DEF-P4LEARN-01`，我复现了他的说法而不是照抄。
4. **裁定 A 在新载体上仍然成立**：我那 4 条"数值不许出门"判据 ＋ 09-28 那 3 条提示词判据，在 `398101a` 上 **7/7 绿**（把判据临时拷进 QA 载体跑完即删，载体跑后仍 0 行）；新增的唯一提示词装配处 `src/Pry.Core/Prompting/ContextBuilder.cs` 里**零个**内部信号名 ⇒ `eb01acf` 的记忆层整合**没有**打开任何数值出口。
5. **一处严重度上升（不新开编号）**：`DEF-P3SEAM-04` 的形状在 tip 上**一字未变**（`CharacterIntelligenceGateway.cs:194` 仍是 `delivered.Length == 0) return;`），而今天它多了一个活消费者 ⇒ 从"往队列里记一行、大概率静默丢掉"变成"每次投递都被排空进证据账"。按代码路径把话说准：`ExtractShapes("……")` 的分隔符表里含 `…`，拆完为空 ⇒ **占位符不会变成口癖候选**，被污染的是**关系／互动计数**那一层。结论＝仍然按 `-04` 原编号转给 Intelligence，**不得**与 `-01/-02/-03` 合并处置（那三条已在 `eb01acf` 里闭合）。
6. **`0-2` 的判据非空转不由本报告签 Pass**：那一刀的 6 条新判据与它的突变反证出自**同一个角色**（我当时以 Character Intelligence 身份写的实现与判据），按 §8.2「Owner 自证 ≠ QA 证据」，本轮只确认这 6 条**在门禁里真的在跑且全绿**（Core `SingleInFlightTurnBoundaryTests` 3 例＋Api 3 例已进入 819 的构成），**非空转判定挂"待独立复判"**。M1/M2 是我今天能独立做的部分，覆盖的是 `RT-004-DRAIN`，不是 `0-2`。
7. **我不写结案词**。§5.0 指针不动，Phase 1／2 的"结案"两字仍归作者与守门人；`0-4`（`RT-008-PKG`）继续卡作者的 merge／tag／push 三句话。

---

## 1. 先说人话（作者只要这一段就够了）

- **能做到**：新版代码（两刀并进来之后）我在一份干净副本上连跑三遍，**819 条测试全绿、零红**，跳过的还是原来那两条环境门；她的"学到的东西"第一次有了真正的搬运工——以前那个队列塞满 64 条就悄悄丢，现在有后台服务在取，而且我故意把搬运工摘掉、把取东西那一步换成空操作，测试分别红了 1 条和 8 条，说明这条线**真的在被看守**。
- **做不到**：她**还不会**因为聊天而改变说话方式。搬运工有了，但"你说得对、我下次这样讲"那个认可动作仍然没有任何入口，所以清单上那条"越聊越像你喜欢的方式"依然没通。
- **挡路一件事**：一句挡路话——**认可入口**（`DEF-P4LEARN-01`）。它不在本轮读数里，也不该被这 819 条绿误读成已完成。
- **要拍哪件**：本轮不用你拍判据。还欠的是那三样：更新记录里几条改动要不要一起入库、打包那刀的 merge／tag／push 顺序、裁定③什么时候落笔。另外我这 4 条"数值不许出门"的判据仍未跟踪（第 6 条结论说它们在哪儿、怎么跑）——你说一声才入库。

## 2. 复采读数（同一 SHA、三跑、全部当场采集）

| 跑法 | 结果 | TRX | `measure` |
|---|---|---|---|
| 全量 #1（`runA`） | 819／0／2 跳过（Client 93／App 183／Api 164／Core 379+2） | 4 个文件、各程序集独立命名无覆盖 | `exit=0`，`Passed=819; Failed=0; Other=2`，catalog 75 |
| 全量 #2（`runB`） | 同上，**分项逐位相同** | 同上 | `exit=0` |
| 全量 #3（`runC`，两次突变还原之后） | 同上 | 同上 | `exit=0` |

- 构建：`dotnet build Pry.slnx -c Release -t:Rebuild` 两次（入场一次、突变还原后一次）各 **0 警告／0 错误**。三次全量都带 `--no-build`，且都跑在**最近一次 `-t:Rebuild` 之后**——历轮"改过码仍用 `--no-build` 拿旧二进制"的坑本轮没有再犯（M1／M2 之后都重新 build 才取读数）。
- 红例清单（纪律要求同场列出全部跑法与红例名）：**三次全量零红例**；本轮所有红都发生在**定向突变跑**里，且都在还原之前：M1 红 1 条、M2 红 8 条。没有任何红被转成 skip，没有任何断言被放宽。
- 两条恒定跳过：`Configured_sense_voice_model_can_run_offline`、`Configured_llama_server_requires_the_child_environment_key`（与历轮同两条）。
- `verify-repository.ps1` = **433** 个受跟踪文件通过；`git status --short` 在载体上全程 **0 行**；`git diff --check` 干净。

## 3. `RT-004-DRAIN` 的独立证伪（两处承重墙各有抓手）

新判据在 `tests/Pry.Api.Tests/ExpressionDrainChannelTests.cs`，10 条。提交信息声称"没有一条手工调用 handler"，我按两条各自最省事的假修法验：

| 突变 | 假修法的样子 | 读数 |
|---|---|---|
| **M1** | 排空服务写好了但**没接到宿主上**（`src/Pry.Api/Program.cs:106` 那行前面加 `//`） | **1 红／9 绿** ⇒ 红的正是 `The_host_registers_the_channel_and_nothing_on_a_turn_path_drains_the_queue`；该门数的是"没被注释掉的那一行"，所以它守的确实是生产接线，不是测试自己 new 出来的对象 |
| **M2** | 接上了宿主、循环照转，但**取东西那一步是空的**（`ExpressionDrainService.cs:65` 的 `await learner.Learner.DrainAsync(budget.Token)` 换成 `await Task.CompletedTask`） | **8 红／2 绿** ⇒ `Approved_habits_reach_the_pattern_ledger_through_the_background_channel`、`The_channel_keeps_up_so_no_delivered_turn_is_dropped`、`A_store_that_stops_mid_drain_cannot_delay_a_visible_reply`、`A_write_that_fails_is_picked_up_by_the_next_tick_without_the_user_repeating_it`、`A_drain_cut_short_by_shutdown_loses_nothing`、`One_card_whose_store_throws_cannot_stop_the_channel_or_the_next_card`、`An_explicit_correction_survives_every_tick_that_follows`、`A_learned_habit_survives_a_restart_without_being_counted_twice`。留绿的两条是 M1 的源码门（不涉及这行）与 `The_expression_domain_still_promises_it_never_delays_a_message`（守的是 Core 侧"永不延迟消息"那条独立承诺）⇒ **8 条各自有抓手，不是同一条断言的八次回声** |
| 还原 | `cp` 回写 + `cmp` 字节同一（两处各自），重新 `-t:Rebuild` 后复跑 | 10/10 绿；随后第三次全量 819/0/2 |

**这刀不覆盖的部分（如实划界）**：M2 的红只证明"排空会被真的执行"，不证明"排出来的东西能变成她会用的口癖"——后者需要 `AcceptedExpressionStyles` 有人填（§0 第 3 条实测＝零填写点）。所以本报告签的是**排空半边**，不是"缺口 ④ 已闭合"。

## 4. 逐条独立核对（grep 事实，不看提交信息）

| 声称 | 我在 `398101a` 干净载体的实测 | 结论 |
|---|---|---|
| "`DrainAsync` 从生产零调用者变成有" | `ExpressionDrainService.cs:65` 一处真调用 ＋ `Program.cs:106` 一处 `AddHostedService` | 成立 |
| 另有两处 `DrainAsync`（`ConversationSessionService.cs:60`／`:331`） | 存在，是 **Runtime 消息排空**（`ConversationRuntime.cs:1207`，5 秒预算那个形状），与表达学习队列不是同一件事 | 别把这两条读成学习通道已接 |
| "没有 fixture 用被禁的进程级清池"（`DEF-P1WALK-04`） | `ClearAllPools()` **调用式 0 处**；全仓唯一命中在 `ProcessWidePoolResetBoundaryTests.cs:7` 的**文档注释**里（那条判据自己的说明） | 成立，且形状未被"顺手放宽" |
| "`-01` first-write-wins" | `CharacterIntelligenceGateway.cs:123` `if (_pending.ContainsKey(key)) return;` | 在 tip 上 |
| "`-03` 逐出已结算的非成功 scope 任务" | `CharacterIntelligenceGateway.cs:70-72` `_scopes.GetOrAdd(...)` 后紧跟条件 `TryRemove` | 在 tip 上 |
| "`-04` 未处置"（我登记的原状） | `CharacterIntelligenceGateway.cs:194` 仍 `delivered.Length == 0) return;` | 未修，按原编号继续挂 Intelligence；见 §0 第 5 条的**严重度上升** |
| "两张 pattern 表今天仍长不出 `Learned` 行" | `AcceptedExpressionStyles`／`ExpressionFeedbackId` 在 `src/` 内只有定义与消费、无填写点 | 成立 ⇒ `DEF-P4LEARN-01` 保留 |
| "10 条判据跑真库、真网关、真投递路径" | 定向跑 10/10 用时 4 s（还原态）／66 s（M2 带预算等待），与"真库＋真后台 tick"一致；M2 的红只可能来自真路径 | 与声称一致 |
| **提交信息里的一处过期数字** | `398101a` 正文写 "repository verification 431 tracked files"；我在该 SHA 干净载体实测 **433**（`git ls-files` 同为 433）。431 是上一刀 `eb01acf` 的数，差的 2 恰是本刀新增的 `ExpressionDrainService.cs` 与 `ExpressionDrainChannelTests.cs` | **文本过期，不是产品缺陷**；同族先例＝`QA-P1W0304-02`。建议该 lane 后续提交信息取当场读数 |

## 5. 裁定 A 在新载体的复核（4＋3＝7 绿）

作者 10-12 裁的 A 方案落地了 4 条判据，但它们**不在 tip 上**（未跟踪）。本轮把它们临时拷进 QA 载体，与 09-28 那 3 条提示词判据同场跑：**7 通过／0 失败**，跑完删除，载体 `git status --short` 回到 0 行。

- 为什么这一步值得做：`eb01acf` 新写了 `ContextBuilder`（"唯一回复上下文装配处"）并改了 `PromptBuilder`，还加了 `MemoryManagementContracts`／`MemoriesController`／`PryBackendClient` 的对外面——恰好全在我那三条门的扫描面上。**整合确实没有把数值出口打开**：`ContextBuilder.cs` 内 `Familiarity`／`Trust`／`Closeness`／`Valence`／`Arousal`／`Irritation` **零命中**。
- **口径提醒**：819 **不含**这 4 条。若随后入库，现行总数变 **823／0／2（总 825）**；届时历轮"763／767"两档读数都要按新载体重取，不能做加法充数。

## 6. 临时目录观察（不登记为缺陷）

`%TEMP%` 的 `pry-*` 条目本轮 492，高于 10-12 记录的 264；但**一次完整全量跑前后 492→492 净零增** ⇒ 增长不由本 tip 的套件产生。抽样看到的多为其他工具与非套件进程留下的目录（`pry-dsh-tests-20261013-*`、`pry-research-dsh-*`），另有今日的 `pry-upload-limit-*`。**结论＝记一条不可归因的观察，不派工**；若下一轮在同一载体再做两次全量仍净零增，就确认套件自身不漏。

## 7. 反向约束自查

| 约束 | 本轮 |
|---|---|
| QA 可以指出任何模块的问题，但不自行大规模修改业务架构 | ✅ 产品代码零净改动：两处突变只在 QA 载体、各自 `cp`+`cmp` 字节还原并 `-t:Rebuild` 复跑；判据文件的拷入拷出也已撤销 |
| 不引用 Owner 自证当 QA 证据 | ✅ §3 两处突变是我今天做的；§0 第 6 条明确把 `0-2` 的判据非空转挂"待独立复判" |
| 不以 skip／放宽断言换绿 | ✅ 跳过恒为原两条环境门；未改任何断言 |
| 全量必须 `--logger trx` 独立结果目录、不复用文件名 | ✅ 三个目录各 4 份 TRX，无覆盖（上一轮同族坑本轮没再踩） |
| 中间改过码必须重新 build | ✅ M1、M2 之后各重新 `-t:Rebuild` 再取读数 |
| 不动他人名下文件、不 stage／commit／push | ✅ 台账与本报告的写入不外溢到产品代码；主工作树他人的在途文件一字未碰 |
| schema 编号 | ✅ 零 SQL 接触 |

## 8. 欠账与下一步（都不由 QA 单方推进）

1. **待作者点头入库**：我的 4 条判据文件（现在只存在于 `.local-notes/qa-affinity-outlet-1012`）、`docs/eval/**` 的历轮报告与 README 索引行、`CHANGELOG.md` 上那几条各 lane 写了没入库的更新记录。
2. **`0-2` 的 QA 独立复判**：需要一个没写过那 6 条判据的角色来做突变；我今天能做的独立部分已做（§5 的 7 绿只是回归，不是非空转证明）。
3. **`DEF-P3SEAM-04`**：按 §0 第 5 条转给 Intelligence，载体已换成 `398101a`，反向约束一字不动（不得新写第三套信封解析、不得整条删 `delivered.Length > 0`、不得与已闭合的 `-01/-02/-03` 合并处置）。
4. **`DEF-P4LEARN-01`**：认可入口是第 1 层剩下的唯一实质缺口，也是作者真正在意的那件事（"越聊越懂你"）的门槛。它排在执行基准第 1 层之后、第 2 层记忆管理面（`2-C`）之前怎么排，归作者与守门人。
5. **`0-4`（`RT-008-PKG`）**：仍卡作者的 merge／tag／push 三句话；两条 precondition（干净载体与 `runtime/` 打包输入互相顶、`sourceCommit` 无人看守）仍未落笔。
6. §5.0 指针不动；本报告不构成阶段推进。
