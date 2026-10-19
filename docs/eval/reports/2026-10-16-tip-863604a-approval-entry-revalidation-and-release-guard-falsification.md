# 线尾 `863604a` 门禁复采 ＋ 第 1 层认可入口（`DEF-P4LEARN-01`）独立证伪 ＋ 发行脚本"脏树拒产"新守卫反证 · 2026-10-16

- **Run ID**：`eval-QA-TIP1016-1016`
- **判定人**：QA / Evaluation（EVAL-001，同时以守门人身份登记台账）
- **派工原话**：作者 2026-10-16「按照守门人说的开工」。承接的正是守门人那一页 §0 里唯一空着的一格——原文「876 那组绿灯是守门人自己跑的，**QA 还没从这个提交号正式复采**」，以及 §3 `0-4` 行里那句"来源提交号没有一道'工作区必须干净'的门守着"（本轮由实测结案为**已成立**）。
- **载体＝一个 SHA**：`863604abae3e`（`fix(release): make package-release.ps1 runnable on PS 5.1 and refuse a dirty tree`，**1 文件／20 插入／1 删除**，父 `c608220`）。我从它新建自己的 `--detach` worktree `.local-notes/qa-tip-1016`，**没有复用守门人自跑时用的那一棵树或它的结果目录**。建完 `git status --short` **0 行**，五处突变每次还原后 **0 行**，出稿前重核仍 **0 行**。
- **本轮修掉的仪器缺陷（上一轮自己的错）**：结果目录、突变备份（`*.orig`）与 TRX **不再放进载体里**。上一轮我放在载体内，`git status` 因此出现 `?? .qa-run/`，我当时把它当"临时面"解释了；事实是 `.gitignore` 只忽略 `*.log`（第 21 行）与 `TestResults/`（第 10 行），**`.orig`／`.trx` 一律不忽略**。本轮全部落到载体外 `.local-notes/qa-tip-1016-run/`，于是"干净"这一条第一次是真正的 0 行，不需要脚注。
- **一次复采覆盖的账**：`863604a` ← `c608220` ← `2af02c2` ← `dd91d2c` ← `f6c9750` ← … ⇒ 上一轮那笔"认可入口那 16 个文件只有 Owner 自证、QA 判不可签（无 SHA）"**本轮同场结清**，同时把 `2af02c2`／`dd91d2c`／`398101a`／`eb01acf`／`b15ed9d` 的复采账一并覆盖，不需要单独回访。

---

## 0. 结论先写在最前面

1. **复采成立**：`dotnet build Pry.slnx -c Release -t:Rebuild` **0 警告／0 错误**（`.local-notes/qa-tip-1016-run/rebuild.log`，当场数到 9 个程序集全部重新产出）；解决方案级**连续三次 876 通过／0 失败／2 跳过（总 878）**，三次分项**逐位相同**＝Client 93／App 221／Api 179／Core total 385·executed 383·passed 383；`measure-fixed-eval.ps1` 对 runA／runB／runC **三个**独立结果目录各 **`exit=0`**，每份都打印 `Catalog valid: 75 permanent scenarios.` 与 `Passed=876; Failed=0; Other=2.`（`.local-notes/qa-tip-1016-run/measure-summary.txt`）；`verify-repository.ps1` 在载体上通过 **454** 个受跟踪文件；`git diff --check` 干净。**红例清单：零条。** 两条跳过恒为 `CoreTests.Configured_sense_voice_model_can_run_offline` 与 `LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`。
2. **门禁基线自此是 876／0／2（总 878）**，上一轮在册的 854／总 856 过期。引用规则照旧且更硬：**必须写全四项**（Client 93／App 221／Api 179／Core 383＋2skip），因为 854 与 876 只差 22，而 22 恰好等于 App 那一刀加的条数——任何"总数差不多"的转述都会顶账。
3. **认可入口那把刀的承重墙由我自己证伪，没有采信写它的人自报的十处突变**（契约 §8.2）。五处独立反证、逐个点名转红、每处还原后 `cmp` 字节相同（详见 §3）：绕过高级开关 ⇒ App **1 红**；把"投递"当成"认可" ⇒ Api **2 红**；归属改成"按最近一轮" ⇒ App **1 红**＋Api **5 红**；证据门槛 2 改 1 ⇒ Core **2 红**＋Api **2 红**；在请求内排空学习队列 ⇒ App **1 红**＋Api **1 红**。
4. **一处作废并重做，如实记**：我第一版 M2（投递即盖章）写得**太弱**——用了固定 `ExpressionFeedbackId = "delivery-replay"`，结果那条真正该红的判据 `Deliveries_without_a_single_click_never_teach_a_habit` ** stayed 绿**，只红了一条不相干的。按我自己 10-12 那轮的规矩"全绿／错红点的突变必须作废重做，不能当反证"，该次读数不入账，改成每轮投递发一个新 GUID（`Guid.NewGuid().ToString("N")`，即真实回归的形状）重跑为 **M2b** ⇒ 那一面承重墙**确实红了**。
5. **新登记 `QA-TIP1016-01`（Medium，Owner 建议＝守门人／发行 lane，QA 只转达不代改）：整条发行路径机械判据为零。** 我在载体全文检索 `package-release`（`tests/` 与 `docs/eval/scenarios.json` 与全部受跟踪文件）⇒ **除脚本自身外零命中**（`guard-probe-summary.txt` 第 5 段 `references_outside_the_script=` 空）。含义说准：**把 `Assert-CleanWorkingTree` 那一行删掉，876 条测试一条都不会红**；`0-4` 的"包里的代码 == manifest 里那个 SHA"今天只由一次人工执行守着。修法必须是**给脚本自身加一道源码门**（像 `TrayResidencyBoundaryTests` 那样扫文本），不是改产品代码；这条与 `QA-TIP1015-01` 同族（守卫的扫描面本身没被守），但面不同——那次是产品源码门漏扫决策本体，这次是整条发行门**没有任何**判据。本条结论靠静态检索成立，不需要也没用突变去证明一个不存在的测试。
6. **新登记 `QA-TIP1016-02`（Low，Owner 建议＝Character Intelligence lane，只转达不代改）：一条形状门的文字强于它的断言。** `ExpressionFeedbackSurfaceTests` 用 `Assert.Single(explicitWriters)` 守 `FeedbackIsExplicit = true` 的填写点，而 `explicitWriters` 的元素是**文件**（`ProductionSources()` 逐文件 yield），docstring 却写"在整个 `src/` 只有一处"。M2 与 M2b 都在**同一个文件**里再加一处 ⇒ 该门 **7／7 全绿**。缓和（必须同时写）：**危害不是没接住**——同一形状在 Api 侧把两条行为判据打红了，所以这不是假绿，是"注释与断言量纲不一致"；修法是把 `Single` 换成按命中次数计，或把 docstring 改成"只有一个文件会写它"。
7. **一条反向事实（本轮的好消息，写给下一个想给 lane 加门的人）**：`QA-TIP1015-01` 那一族——"把本体掏空而源码扫描门仍然绿"——**在认可入口这把刀上没有复现**：M3（归属掏成"取最近一轮"）与 M5（请求内排空）各转红一条**形状门**（`Attribution_refuses_instead_of_guessing_and_adds_no_new_egress`、`A_reaction_never_drains_the_learning_queue_inside_the_request`），而且该类自带一条正向探针 `The_scanners_see_the_leaks_they_claim_to_see`。⇒ 上轮那条建议（把决策本体纳进扫描面）在这把刀上已被照着做出来了，不需要重复派。
8. **`0-4` 前置条件②（脏树拒产）由读代码升为实测，且实测比读代码更强**：干净树单跑守卫 `clean_guard_threw=False`；**脏树跑整只脚本 `exit=1`**，输出既提"refusing"也点名我埋的那个探针文件；**加 `-SkipValidation` 照样拒**（`skipvalidation_still_refuses=True`）；`artifacts/releases` 下的目录数**跑前＝跑后＝0**，指定 `-Version 0.0.9` 也没建出 `v0.0.9` ⇒ **拒绝发生在任何落盘之前**；移除探针后守卫再跑又放行 ⇒ **拒产不粘连**。序位我改成精确说法：**脚本主体第一条会执行的判断在 `scripts/package-release.ps1:152`，11–22 行只是路径变量赋值，第一条落盘语句在 154／155**（我最初用 AST 探"是否第一条语句"，读出来是 `Split-Path`——那是变量赋值不是判断，那次探针**不作数**，序位结论以行号实读＋上面那组落盘读数为准）。
9. **`0-4` 前置条件①同样由读代码升为实测**：把 `Copy-InferenceRuntime` 单独取出来在**干净载体**上调用（不跑 publish）⇒ **`THREW: Required inference runtime is missing: …\qa-tip-1016\runtime`**；`runtime/` 在载体不存在（被 `.gitignore` 排除）、在作者主树存在且 `runtime\llama-server.exe` 为真；探针**没有**创建 `artifacts/probe-target`，载体 `git status` 仍 0 行（`precondition1-summary.txt`）。⇒ **"出包只能在作者本机那棵带着 gitignored 运行库的树上做"今天成立且已被量过**；干净检出／CI 上 `0-4` 走不完，这是事实不是缺陷，但它决定了"谁来按下打包那个键"。
10. **我不签、也不动的清单**：`0-2` 那六条判据的"非空转"**仍待换人复判**（判据与突变同出一人）；真机路径 C"用户真退了一次"**仍零份可归因证据**，缺作者一次托盘点击；`DEF-P3SEAM-04` 在途——出稿前重看主树，`src/Pry.Api/Runtime/CharacterIntelligenceGateway.cs`、`src/Pry.Core/TurnTaking/ReplyPlanner.cs` 及三个新测试文件已是**别人的未提交改动**，**我一个都没碰**；§5.0 阶段指针与结案词不由 Agent 写；本轮**没有打 tag、没有 push、没有打包、没有新建可指认提交**（本报告文件与索引行仍在未提交面，等作者点头）。

---

## 1. 说人话（作者只要这一段）

- **能做到**：你上周点头提交的那个"她能听你点头"的版本，我在一份干净副本上连跑三遍，**876 条全绿、零红**，三遍的分项数字一位不差；而且我不信写它的人自己报的数，自己动手拆了五处——把那个默认关着的开关当不存在、把"消息发出去了"当成"你认可了"、把"这条气泡属于哪一轮"改成"就按最近那一轮算"、把"要点头两次才算数"改成一次、在你点的那一下里顺手去清空学习队列——**每一处都有测试当场变红**，所以"必须开关打开才出现""不点就永远学不到""认不出就拒绝而不是猜""两次才算数""点一下不会卡住你聊天"这五条规矩**真的有人在看**。上一版那笔"新版本只有自己做的人验过"的欠账，这一次连前面几刀的账一起结掉了。
- **做不到**：**"越聊越懂你"今天还是不成立**——本轮证明的是"明确按两下能教成一句、教成的东西活过重启"这一格，而且那个开关默认关着、要你去设置里的"高级设置"打开。另外**发行那条路今天没有任何机器判据守着**：我全文搜过，876 条测试和那 75 条永久场景里**没有一条提到打包脚本**，也就是说谁把那行"工作区不干净就别出包"删掉，门禁一声不响。这条我登记了（`QA-TIP1016-01`），修法是给脚本自己加一道门，不动产品代码。
- **挡路一件事**：**真机"用户真的退了一次"那一条**——只有你的手能给（托盘里点「退出…」并确认），脚本点不到托盘图标。除此之外本轮不缺任何技术动作。
- **要拍哪件**：三件老账，本轮不新增判据要你拍——① **点头提交我这份报告＋那两份还没入库的工单**（`docs/dispatch-p3seam04-placeholder-turn.md`、`docs/dispatch-rt005-memory-management.md`）和那 8 份文档文本怎么分刀归位；② **要不要现在就把"打包"当成真事做**：我实测出"出包只能在你这台机器上做（运行库不进版本控制）"，所以如果你想要一个可安装版本，需要你亲口给 tag／push／打包的明令——**这三件我们不会自己做**；③ 裁定③（`PLAT-004`/`007` 进不进本轮），第 5 层之前拍就行。

---

## 2. 复采读数（同一 SHA、三跑、全部当场重采，证据留在载体外的结果目录）

| 跑法 | 结果目录 | 分项（逐个 TRX 的 `<Counters>`，不是控制台转述） | `measure` |
|---|---|---|---|
| runA（解决方案级） | `.local-notes/qa-tip-1016/TestResults/runA/` | Client total 93 passed 93／Api total 179 passed 179／Core total 385 executed 383 passed 383／App total 221 passed 221；failed 全 0 | `exit=0`，`Passed=876; Failed=0; Other=2`，catalog 75 |
| runB（解决方案级） | 同上 `runB/` | 同上四项逐位相同 | `exit=0`，同一行 |
| runC（解决方案级，**五处突变全部还原之后**） | 同上 `runC/` | 同上四项逐位相同 | `exit=0`，同一行 |

runC 的位置是刻意的：**它跑在"这棵树曾经被改过五次、每次都还原"之后**，与 runA／runB 逐位相同 ⇒ 还原是真还原，不是"看起来干净"。runC 之前还重跑了一次全量 `dotnet build`（`0 警告／0 错误`）确认二进制确实是还原后的那份。

**同场列出全部跑法**（按台账纪律，含突变跑与仪器跑）＝**3 次解决方案级 ＋ 9 次定向（M1 2／M2 2／M3 2／M4 2／M5 2，其中 M2 那次作废；M2b 2）＝ 12 次测试调用**；正式配对读数取那三次解决方案级。另有 **2 次全量构建**（入场 `-t:Rebuild` ＋ runC 前的还原后重建）、**3 次 `measure`**、**1 次 `verify-repository`**、**2 次发行脚本探针**（守卫探针＋`Copy-InferenceRuntime` 单函数探针，都不产包）。

**仪器事实四条，写给下一个在这台机器上跑门禁的人**：
- **TRX 的 `<Counters>` 里 `notExecuted` 恒等于 0，跳过不会进那个字段**。本轮 Core 那份是 `total="385" executed="383" passed="383" notExecuted="0"` ⇒ 两条跳过只能从 `<UnitTestResult outcome="NotExecuted">` **逐条点名**（本轮点名结果见 §0 第 1 条）或从控制台的"已跳过: 2"读。按 `notExecuted` 数跳过会永远得到 0——上一轮我写的"按 `outcome=\"NotExecuted\"` 逐条读出"是对的，但这一轮把"为什么不能用 Counters"补上。
- 默认 TRX 命名在解决方案级仍会跨程序集撞名（本轮 runA 四个文件里三个共用同一秒时间戳，靠 `[1]`／`[2]` 区分）。**程序集归属靠文件内的 `Pry.<X>.Tests` 前缀证实**，不靠文件名。
- 出稿前重取的资源读数（`.local-notes/preflight-1016-final.txt`）：**空闲物理 4.4 GB，已用 commit 48.3／总 50.4 GB**；`dotnet.exe` 9 个进程，命令行逐条读完**全部是 SDK 的 `MSBuild.dll /nodemode:1 /nodeReuse:true` 驻留节点，没有他人的 `dotnet test` 在同树并发**；占 commit 的大头仍是作者自己开着的 ComfyUI（`python pid 37416`，私占 11.1 GB）。**没有动作者的任何进程。** 这条前置的用处：本机可用 commit 被压到 ~3.9 GB 时 Core 会报 `SQLite Error 10 disk I/O`／`Error 7 out of memory`，那是**资源形状**、判据要作废重跑而不是改阈值。另外要如实记一句：**17:45 那次入场探测只输出到控制台、没落盘**，所以入场时刻的精确数字我不复述，只有出稿前这份是有文件的。
- 突变驱动的两个硬约束（上一轮踩过，本轮生效）：**还原写在 `finally` 里**——上一版脚本在还原之前崩在 TRX 解析上，害得一份产品源文件留着突变内容；锚点在目标文件里命中次数≠1 就**拒绝写入**并记 `ANCHOR NOT UNIQUE`。每处突变的原始字节先落到载体外的 `*.orig`。

---

## 3. 独立证伪（五处成立 ＋ 一处作废重做，全部只落在 QA 自己的载体）

还原核对两套：① 驱动脚本在 `finally` 里把备份写回并立刻断言"读回来的字节 == 原始字节"，五处全部打印 `restored byte-identical = True`；② 事后 `git status --short` 回 0 行（本轮载体内零 `??`，见开头那条仪器修正），再用 runC 与 runA 分项逐位相同收口。

| 编号 | 改哪里（一句话） | 红了谁（逐条点名） | 计数 | 证据 |
|---|---|---|---|---|
| **M1** | 把那条界面开关的判断 `if (!scripted && _preferences?.ExplicitExpressionFeedback == true)` 改成 `if (!scripted)`——即"高级开关不存在，人人默认见到这两项" | `ExpressionFeedbackSurfaceTests.The_two_reaction_items_only_exist_behind_the_advanced_switch` | App **1 红／6 绿**；Api 15／15 绿（正确：这条只管界面） | `mutate-summary.txt` |
| **M2**（**作废**） | 在投递回执上顺手盖"这是显式认可"的章，但 `ExpressionFeedbackId` 用固定串 | 只有 `Three_clicks_on_one_bubble_are_one_approval_and_promote_nothing` 红；**该红的 `Deliveries_without_a_single_click_never_teach_a_habit` 没红** | 不作数 ⇒ 重做为 M2b | 同上，作废理由见 §0 第 4 条 |
| **M2b** | 同一个洞，但每轮投递发**新 GUID**（＝真实回归的形状：每次投递都被当成一次新认可） | `ExplicitExpressionFeedbackTests.Deliveries_without_a_single_click_never_teach_a_habit`、`…Two_explicit_approvals_on_two_turns_become_a_learned_row_that_survives_a_restart` | Api **2 红／13 绿**；App 7／7 绿 | `mutate-b-summary.txt` |
| **M3** | 归属不看投递账本，改成"取最近一条回执"（`_receipts.TryGetValue(messageId, …)` → `_receiptOrder` 尾元素） | `ExpressionFeedbackSurfaceTests.Attribution_refuses_instead_of_guessing_and_adds_no_new_egress` ＋ Api 的 `…refuses_instead_of_hanging_on_the_latest_turn`、`…not_owed_to_this_one`、`A_click_never_waits_for_the_pattern_store`、`An_explicit_rejection_silences_the_shape_and_no_later_click_revives_it`、`Two_explicit_approvals…` | App **1 红**；Api **5 红／10 绿** | `mutate-summary.txt` |
| **M4** | `MinimumEvidence = 2` 改成 1（一次点头就晋升） | `ExpressionSystemTests.A_single_use_of_a_phrase_is_not_a_personality`、`…Something_the_user_keeps_saying_stays_theirs` ＋ Api 的 `Three_clicks_on_one_bubble…`、`Two_explicit_approvals…` | Core **2 红／32 绿**；Api **2 红／13 绿** | 同上 |
| **M5** | 在你点那一下的请求里就地 `await DrainAsync`（把后台排空搬进用户等待路径） | `ExpressionFeedbackSurfaceTests.A_reaction_never_drains_the_learning_queue_inside_the_request` ＋ Api `A_click_never_waits_for_the_pattern_store` | App **1 红**；Api **1 红／14 绿** | 同上 |

**如实登记的三点**：① M1／M3／M5 打的都是**行为判据**，与写它的人自报清单形状有重叠，我这一处不构成"新增覆盖"，只是独立复现；② **只有 M2b 是本报告新增的那处覆盖**——它把"投递即认可"这一面墙压到必须由那条不点就学不到的判据来守，而这条正是他们十处清单里我读不出来的形状（也正因为如此，我的第一版 M2 才写得那么弱）；③ **M4 红了 Core 两条**，说明"门槛 2"这个值今天是被断言守着的，不是被注释守着的——这条对上作者 2026-10-12 那句"我想要的回复效果是长期记忆磨合出来的，不是数值调出来的"。

---

## 4. 第 1 层那一刀（`DEF-P4LEARN-01`）完成标准逐条对账

| 完成标准（工单原文拆句） | QA 判定 | 依据 |
|---|---|---|
| 两项反馈（"就是这样／别这样说"）只在高级开关打开时出现，默认不打扰 | **Pass（判据面）** | 876 在册的 `ExpressionFeedbackSurfaceTests` 7 条 + **M1 实测会红** ⇒ 不是常绿门 |
| 只对她真的生成过的气泡给这两项（`scripted` 气泡不给） | **Pass（判据面）** | 同一条件里的 `!scripted` 半句，M1 只动后半句时其余判据仍绿 ⇒ 两半各有抓手 |
| 归属＝后端进程内投递账本；认不出就拒（409），不回退"按最近一轮" | **Pass（判据面）** | `ReceiptLimit = 64`、schema 一字未动（`git show --stat c608220` 无迁移文件）；**M3 同时红界面门与 4 条 Api 行为判据** ⇒ "不回退最近一轮"是真被守着的 |
| 两次明确认可才晋升；一条气泡连点三次只算一次 | **Pass（判据面）** | **M4** 红 Core 2＋Api 2；`Three_clicks_on_one_bubble…` 在 M2b 与 M4 两种突变下都红 ⇒ 去重与门槛不是同一条断言的回声 |
| 点一下不等待学习队列、不在请求内排空 | **Pass（判据面）** | **M5** 两侧各红一条（App 形状门 + Api 行为门） |
| 教成的东西活过重启；检索侧拿得到 | **Pass（判据面）／无长聊读数** | `Two_explicit_approvals…survives_a_restart` 在册；本轮证伪里它两次被点名转红 ⇒ 真值。但"越聊越懂你"要的是长聊读数，本轮**没有**这种读数 |
| 零新增出门口（数值不出后端） | **Pass（判据面）** | `…adds_no_new_egress` 那条钉 `IRuntimeOutputSink` 恰好 6 个成员，M3 期间仍绿；`ScalesVocabulary` 与正向探针 `The_scanners_see_the_leaks_they_claim_to_see` 在册 |
| 反向约束：`MinimumEvidence`、按序往返、schema 不得顺手改 | **Pass** | 三处独立核对：`ExpressionLearner.cs:52` 在 `c608220` 上仍是 `= 2`；无迁移文件；`ReceiptLimit = 64` |

⇒ **第 1 层 `DEF-P4LEARN-01`：QA 判定 可签（已复采、承重墙已独立证伪）。** 上限照旧写死在 §8 那一格：**"明确按两下能教成一句、教成的东西活过重启"可写；"越聊越懂你"不可写**（默认关、零自动推断、无长聊读数）。`FollowUpEvidence` 仍零来源＝`RT-006` 没被顺带做掉。

---

## 5. `0-4`（`RT-008-PKG`）本次能签的与不能签的

**能签（判据／实测面）**：§0 第 8、9 条——脏树拒产门**存在、位置正确、无条件执行、拒绝在任何落盘之前、不粘连**；前置①（运行库只能在作者本机）**已被量过**；PS 5.1 可解析（UTF-8 BOM，`parse-check-1016.ps1` 当场解析通过）。
**不能签**：`0-4` 整段。缺的不是判据是**动作与明令**——没打过 tag、没 push 过、没真出过一个包，`artifacts/releases` 里至今只有旧线的 `v0.0.2`。而且 §0 第 5 条说明**这条路一旦真跑，没有任何测试会在它被改坏时报警**。
**建议派工（不自动派，等作者点头）**：`QA-TIP1016-01` 的修法＝给 `scripts/package-release.ps1` 加一条"脚本自身形状"的源码门（扫三件事：`Assert-CleanWorkingTree` 存在、`sourceCommit` 仍来自 `rev-parse HEAD`、`Copy-InferenceRuntime` 仍在缺文件时抛），并在 `docs/eval/scenarios.json` 里为"发行可审计性"铸一条永久场景；这是守门人 lane 的活，**不需要动任何产品代码**。

---

## 6. 下一步归属

1. **作者**：① 点头提交本轮产物（本报告 ＋ `docs/eval/README.md` 一行 ＋ §3/§0 更新），并给那两份仍未跟踪的工单与那 8 份 lane 文档文本分刀归位；② 托盘上真点一次「退出…」并确认（之后把 `-ExitBudgetSeconds` 加长再跑一轮，路径 C 三条一次采齐）；③ tag／push／打包仍缺明令——**且现在多了一条已知事实：打包只能在你这台机器上做**。
2. **QA（本轮之后仍欠的）**：`0-2` 那六条判据的非空转**换人复判**；认可入口若再有第二刀（例如 `FollowUpEvidence` 起来源），从新 SHA 复采。
3. **Character Intelligence lane**：`QA-TIP1016-02`（Low）——把 `Assert.Single(explicitWriters)` 的量纲与 docstring 对齐（按命中次数计，或改文字）；**不许**为此改任何用户可见行为。`DEF-P3SEAM-04` 按已派工单处置（我看到主树已有在途改动，归属不变）。
4. **守门人／发行 lane**：`QA-TIP1016-01`（Medium）按 §5 的修法承接；`main` 继续按「可以并线」用 ref-only fast-forward 跟线尾；**不打 tag、不 push、不打包**。§5.0 指针与结案词不由 Agent 写。
