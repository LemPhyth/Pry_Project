# 线尾 `88eff2a` 门禁复采 ＋ `DEF-P3SEAM-04` 十条新判据独立证伪 ＋ `0-2` 那六条接缝判据换人复判 · 2026-10-19

- **Run ID**：`eval-QA-P3CLOSE-1019`
- **判定人**：QA / Evaluation（EVAL-001，同时以守门人身份登记台账）
- **派工原话**：作者 2026-10-19「接下来」。承接两格已经写着的账——上一份报告 §6 第 2 条「`0-2` 那六条判据的非空转**换人复判**」，以及 `PROJECT_PLAN.md` §9 那一轮"下一步归属"第 ① 条「**QA**＝从 `88eff2a` 建干净载体复采并独立证伪那 10 条判据，出正式报告号」。工单：`docs/dispatch-p3seam04-placeholder-turn.md`（仍未跟踪，见 §7 第 1 条）。
- **载体＝一个 SHA**：`88eff2a77ac6`（`fix(intelligence): stop booking a turn she never said into three ledgers`，父 `863604a`；`git show --numstat`＝**6 文件／＋344／−6**，其中产品代码三处＝`CharacterIntelligenceGateway.cs` 6/2、`ReplyPlanner.cs` 2/4、`VisibleReply.cs` 29/0，另三面是新增测试）。从它新建我自己的 `--detach` worktree `.local-notes/qa-tip-1019`，**没有复用 lane 自跑时那棵树**（§5.0 判据④"读数不得互顶"）。建完 `git status --short` **0 行**，14 处突变每次还原后 **0 行**，出稿前重核仍 **0 行**。
- **零产品代码改动、零判据文本改动**：本轮只读、只跑、只在载体里做可还原的单点突变。**未 stage、未 commit、未 push，`main` 未动**（当下 `git rev-list --count main..HEAD` ＝ 1，`main` 仍停在 `863604a`；并线要有作者明令）。
- **一次复采覆盖的账**：`88eff2a` ← `863604a` ← `c608220` ← … ⇒ 把 `DEF-P3SEAM-04` 这一笔新账接上，同时再覆盖一遍上一轮已结的祖先账（不重复引用旧读数）。
- **临时产物全部落在载体之外**（`.local-notes/qa-tip-1019-results/`：四次全量的 TRX 与构建日志、四次 `measure` 的 `fixed-eval-coverage.json`、14 处突变的 `*.bak` 与定向 TRX）。上一轮那两条在册读法本轮继续生效：`.gitignore` 只忽略 `*.log` 与 `TestResults/`，所以 `.orig`／`.bak`／`.trx` 一旦落进载体就会长出 `??`，"干净"就得写脚注——写了脚注的读数不算成立。

---

## 0. 结论先写在最前面

1. **复采成立**：`dotnet build Pry.slnx -c Release -t:Rebuild` **0 警告／0 错误**（`…-results/run4/run4.build.log` 当场数到 **9 个程序集全部重新产出**）；解决方案级按程序集分别跑、各自唯一 TRX 落新目录，**连续四次 886 通过／0 失败／2 跳过（总 888）**，四次分项**逐位相同**＝Client total 93 passed 93／App total 221 passed 221／Api total 185 passed 185／Core total 389 executed 387 passed 387。跑位：**run1／run2＝入场基线（在任何突变之前）**，**run3＝11 处突变全部还原之后**，**run4＝14 处突变全部还原之后**（含那处改测试文件自身的 Q5）。⇒ **在册基线自本起＝886／0／2；上一份的 876／0／2 过期，854／832／819 更旧。**
2. **`88eff2a` 新加的十条判据全部非空转**：我逐个把"它守的东西"移动掉，十条**每一条都至少被一次突变点名打红**（对账表见 §3.1）。其中两处正是工单完成标准⑦点名要求的方向（退回纯长度＝N1、摘掉 `NoteUsedAsync`＝Q6），本轮由 QA 独立复现而不是沿用 lane 自报。
3. **`0-2` 那六条接缝判据：换人复判成立**（§3.2）。三条各**只红一条**（Q1／Q2／Q3b＝Api 各 1 红／184 绿），三条闸序判据各红自己那条加 `RuntimeComponentTests` 的既有总闸（Q4a／Q4b 各 2 红、Q4c 1 红）。**判据出自 Intelligence lane、突变与读数出自本角色** ⇒ §8.2 那条"同一角色自证不签"在这一格上不再有理由挂着。
4. **一处过度修复方向也被压出来了**：把"这轮说没说上话"的判据**收得太紧**（要求正文长度 > 500 才算说了话）⇒ Api 7 红，红的正是"同轮附一条占位、但她真说了话"那条对照例与四条接缝判据；Core 侧同一突变**只红一条**＝`A_reply_she_actually_says_reads_as_visible_content`。这条对上一轮 `QA-TIP1015-01` 那一族（掏空本体而扫描门仍绿）是反向证据：**这个形状两侧都有抓手**。
5. **两处突变作废、不当反证**（按台账纪律第⑤条，如实登记）：**Q3**（把首轮建状态的逐出条件从"未成功完成就逐出"改成"被取消才逐出"）⇒ Api **185/185 全绿**，对判据不敏感，重做为 Q3b 才转红；**N2 第一版**（网关里内联第二套判据时漏了一个取反）⇒ 把闸门整个反过来，8 红里含不相干的接缝判据，属仪器错，重做为行为等价版本后 2 红。**N2 第一版的 TRX 被同名重跑覆盖，没有留档**，只在报告里按过程记录。
6. **新登记一条覆盖观察 `QA-TIP1019-01`（Low，Owner 建议＝Character Intelligence；QA 只转达不代改）**：Q3 全绿这件事本身是有内容的——**建状态域"抛异常"（Faulted）那半段今天没有任何判据守着**。现行条件 `"不是成功完成就逐出"` 两种失败形状都能逐出并重来，而既有判据只制造"被取消"那一种：把它改窄成"只有被取消才逐出"（Q3）⇒ 六条接缝判据一条都不红。修法只需要在既有那条判据旁边加一个"首轮建域抛异常"的对照，**不需要也不许动产品代码**。详见 §5。
7. **文档面本轮第一次拿到硬证据**：在 `88eff2a` 这个 SHA 上，`docs/api-v1.md` 里字符串 `expression-feedback` **出现次数＝0**，`docs/character-learning-safety.md` 里 `ExplicitExpressionFeedback`／"就是这样"**同样 0 次**（在载体上 grep 出来的，不是引用谁的转述）。那段完整端点文档此刻只存在于作者主工作树的**未提交面**（`docs/api-v1.md:312`）。⇒ "新 API 必须有文档"这条规矩在**任何干净检出**上目前不成立，卡的是分刀归位这个动作，不是代码。
8. **其余门禁**：`measure-fixed-eval.ps1` 对 run1／run2／run3／run4 **四个**独立结果目录各 **`exit=0`**，每份都打印 `Catalog valid: 75 permanent scenarios.` 与 `Passed=886; Failed=0; Other=2.`；`verify-repository.ps1` 在载体上通过 **458** 个受跟踪文件；`git diff --check` 干净；**红例清单：零条**；两条跳过恒为 `CoreTests.Configured_sense_voice_model_can_run_offline` 与 `LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`（按 `<UnitTestResult outcome="NotExecuted">` 逐条点名，**不看 `<Counters notExecuted>`——那个字段恒为 0**）。
9. **判定（QA 侧形式，结案词与状态词归作者与守门人）**：`DEF-P3SEAM-04`＝**可签·已复采·承重墙已独立证伪**（完成标准 1／2／3／4／5／7／9 判 Pass，第 6 条口径落笔权已由作者 2026-10-18 行使、本角色只登记，第 8 条未被牵动见 §4）；`0-2` 六条＝**非空转成立（已换人复判）**。**Phase 3 的验收面自此没有待办产品动作**，仍欠的两件都不是代码（§6），**§5.0 阶段指针不由我挪、结案词不由我写**。

---

## 1. 说人话（作者只要这一段）

- **能做到**：那个"她没说出话的一轮，不再被记成一次成功聊天"的修复，我在一份干净副本上连跑四遍，**886 条全绿、零红**，四遍的分项数字一位不差；而且我不信写它的人自己报的数，自己动手拆了十三处（下面列十种改法，其中三处是把同一面换另一个程序集再压一遍）——把"她到底说没说上话"退回去只比长度、把这条判定抄第二份到后端里、把她其实说了话的那一轮也当成没说、把"这一轮该学哪条决策"改成事后重新算一遍、把"只有一条在飞的轮次才记账"改成按最近一条猜、把"首轮建状态失败要能重来"改成永不再来、把说话闸门挪到问模型之后、把占位文本真的当成一句回答往回收、把守卫的扫描面偷偷缩小、把她打算用的口癖干脆不记——**每一条守着它的测试都当场变红**。所以你上次点的那句"她不能把自己没说出口的话记成聊过"，现在是**真的有人在看**，不是注释里写着好看。
- **做不到**：**"越聊越懂你"今天还是不成立**，本轮一条新能力都没加，只是把上一刀钉牢。另外三件事今天仍然没人守：**① 发行那条路还是零机器判据**（上一轮登记的 `QA-TIP1016-01`，本轮没有重跑也没法重跑——它不在产品代码里，仍等点头派工）；**② 新接口的文档没进版本库**——这次我第一次拿到硬证据：那个提交号里查不到一行新接口文档，文档文字还在你机器上没提交；**③ 真机"用户真的退了一次"那一条还是零份可归因证据**，只有你的手能给（托盘里点「退出…」并确认）。
- **挡路一件事**：**Phase 3 现在不缺任何技术动作，缺的是你的一句话和一次点头**——那句结案词（我不写），加上把那些文档文字按刀分开发进去的点头（那 7～9 份还在未提交面里，其中一份是我名下）。
- **要拍哪件**：三件，全是老账——① **文档文字怎么分刀归位**（现在有你名下的和我名下的混在同一个工作树里，一次提完会抹掉归属，所以我不动）；② **要不要现在真出一个可安装版本**（我实测过：只有你这台带着那份不进版本库的运行库的机器能出包；tag／push／打包这三件我们不会自己做）；③ 裁定③（那两个平台档位进不进本轮）。**本轮我没有新东西要你拍。**

---

## 2. 复采读数（同一 SHA、四跑、全部当场重采，证据留在载体外的结果目录）

| 跑法 | 位置（相对突变） | 分项（逐个 TRX 的 `<Counters>`，不是控制台转述） | `measure` |
|---|---|---|---|
| run1 | 入场基线，**突变之前** | Client total 93 executed 93 passed 93／App 221/221/221／Api 185/185/185／Core total 389 executed 387 passed 387；failed 全 0 | `exit=0`，`Passed=886; Failed=0; Other=2`，catalog 75 |
| run2 | 同上（配对用的第二次） | 四项逐位相同 | `exit=0`，同一行 |
| run3 | **11 处产品码突变全部还原之后** | 四项逐位相同 | `exit=0`，同一行 |
| run4 | **14 处突变（含 Q5 那处改测试文件自身）全部还原之后**，入场 `-t:Rebuild` 0/0 | 四项逐位相同；构建日志数到 9 个程序集 | `exit=0`，同一行 |

run3／run4 的用处是刻意的：**它们跑在"这棵树被改过十四处、每处还原成入场那份"之后**，与 run1／run2 逐位相同 ⇒ 还原是真还原，不是"看起来干净"。

**同场列出全部跑法**（按台账纪律，含突变跑与仪器跑）＝**4 次全量（每次 4 个程序集）＋ 15 次定向突变跑（13 处成立 ＋ 2 处作废：Q3、N2 第一版）＝ 31 次测试调用**；正式读数取那四次全量。另有 **4 次全量 `dotnet build … -t:Rebuild`**、**15 次定向 `dotnet build`（测试工程）**、**4 次 `measure`**、**1 次 `verify-repository`**（458 个受跟踪文件通过）、**1 次 `git diff --check`**、**5 份源码/测试文件的 blob 比对**（见 §2.1）。

### 2.1 还原核对（两套，逐条落盘）

① 驱动脚本在 `finally` 里把载体外的 `*.bak` 原字节写回，并立刻断言"读回来的 SHA256 == 入场那份"——14 处全部打印 `restored=True`；② 出稿前再把五个被碰过的文件的 **git blob 哈希与 `88eff2a` 逐字节比**，五份全 `SAME`：`CharacterIntelligenceGateway.cs 7e8d4ab1…`、`ReplyGate.cs 5f3db039…`、`ReplyPlanner.cs 09ae1e76…`、`VisibleReply.cs d829ab9d…`、`PlaceholderDefinitionStaysInCoreTests.cs 0ded3f46…`（注意第五份**是测试文件**——Q5 打的就是判据自身的扫描面，见 §3.1 第 6 行）。此后 `git status --short` **0 行**、`git diff --check` 干净。

### 2.2 锚点与仪器约束（写给下一个在这台机器上跑门禁的人）

- 突变驱动两个硬约束本轮都救到了场：**锚点在目标文件里命中次数≠1 就拒绝写入**（`ANCHOR-HITS=n REFUSED`）；**还原写在 `finally` 里**（上一轮那版在还原之前崩在 TRX 解析上，害得一份产品源文件留着突变内容）。占位文本的字面量不写进 ASCII 脚本，用 `[char]0x2026` 拼，避免 PS 5.1 的编码问题把"闸门"改成"永远放行"那种假绿。
- `dotnet test` 的 stderr 在 `$ErrorActionPreference='Stop'` 下会变成终止性 `NativeCommandError`，把脚本拦在还原之前 ⇒ 本轮全程用 `Continue` ＋ `finally`。
- **资源与并发前置**（出稿前当场重取，两份脚本落盘在载体外：`.local-notes/preflight-1019.ps1`／`preflight-1019b.ps1`）：空闲物理 **8.5 GB**／可见总量 31.4 GB，已用 commit **30.9／47.4 GB**，页面文件 `C:\pagefile.sys` 分配 16384 MB、当下实际占用 153 MB；**没有任何进程私占超过 3 GB**（那一族"ComfyUI 把可用提交吃掉导致 SQLite 报 `disk I/O`／`out of memory`"的形状本轮不存在）；`dotnet.exe` 在场 **8 个**（跑的过程中一度到 11 个），**命令行逐条读完全是 `…\sdk\10.0.200\MSBuild.dll /nodemode:1 /nodeReuse:true` 驻留节点，脚本按 `-notmatch 'nodemode:1'` 反向筛出的"非节点进程"＝0 个，即没有任何他人的 `dotnet test` 在同树并发**。**没有动作者的任何进程。** 这条前置的用处仍然是：本机可用 commit 被压到 ~3.9 GB 时 Core 会报那两个码，那是**资源形状**，判据要作废重跑而不是改阈值。

---

## 3. 独立证伪（十三处成立 ＋ 两处作废，全部只落在 QA 自己的载体）

### 3.1 `DEF-P3SEAM-04` 在 `88eff2a` 新增的十条判据（Api ＋6／Core ＋4）

| 判据（测试名逐条点名） | 钉住的那件事 | 让它转红的突变 | 读数 |
|---|---|---|---|
| `UnansweredTurnLedgerTests.A_turn_that_only_delivered_the_placeholder_teaches_nothing` | 只投出占位符的那一轮：互动／熟悉度／心情整格不动、`behavior_experience` 零新增 | **N1**（后端退回"只看有没有投出去"） | Api **3 红／182 绿** |
| `…The_habit_she_meant_to_use_is_not_charged_by_a_placeholder_turn` | 占位那轮她打算用的口癖不算"用过一次"；同测试自带的正向半段（`:145-149`）钉住"真说了话必须照记" | **N1**、**N4**、**Q6** | Q6 下 Api **1 红／184 绿**——红的就是它，正是工单完成标准⑦点名要的"把 `NoteUsedAsync` 摘掉"方向 |
| `…A_real_bubble_alongside_a_placeholder_is_still_one_successful_exchange` | 不许过度修复：同轮附一条占位但她真说了话 ⇒ 记账照旧 | **N4**（把可见性判据收得太紧：要求正文 >500 字符） | Api **7 红／178 绿**，本条在名单内 |
| `PlaceholderDefinitionStaysInCoreTests.The_placeholder_is_named_by_exactly_one_file_and_that_file_is_in_Core` | 占位文本在四个产品层里只许出现在 `src/Pry.Core/TurnTaking/VisibleReply.cs` | **N2**（后端内联第二套同判据）、**Q5**（把扫描面缩掉 `Pry.Core`） | N2 下 2 红／183 绿；Q5 下 2 红／183 绿 |
| `…The_host_asks_Core_whether_she_said_anything_instead_of_recognising_the_placeholder` | 网关真的调 Core 那个判定口、生产者真的引用那个常量，两侧都不许留第二套 | **N1**、**N2** | N1 三条红里含它；N2 两条红里含它 |
| `…The_placeholder_scanner_sees_the_leak_it_claims_to_ban` | **守卫的扫描面自身**要能看见那三个承重文件，且文件数 >100 | **Q5**（只删掉扫描层里的 `Pry.Core`） | Api **2 红／183 绿**，红的恰是这条与上一条 |
| `UnansweredTurnShapeTests.The_shape_that_says_she_said_nothing_exits_through_the_placeholder_and_reads_as_silent`（theory 三档：整段草稿／三个空格／空串） | 同一档形状同一个出口：兑现到界面上的那条与"一个字没说"同档、且判定为"没说上话" | **N3**（生产者回退成把模型原始输出直接投出去） | Core **7 红／380 绿**（total 389，executed 382，含 2 跳过）：三档 theory 全红 ＋ `ModelReplyFormatTests` 4 条同红 |
| `…A_reply_she_actually_says_reads_as_visible_content` | 她真说出口的那条必须判为"有可见内容"（反向：不许把闸门焊死成永不记账） | **N4b**（同一处收紧，Core 侧跑） | Core **1 红／386 绿**——只红这一条 |

⇒ **十条 / 十条有非空转证据**；其中 **6 条有行为突变打红**，另 4 条（三条源码门 ＋ 一条扫描器自证门）按源码门的本性由**移动它守的那块砖**来证（N1／N2／Q5），这与 `QA-TIP1015-01` 那一族的处理口径一致。

### 3.2 `0-2` 那六条接缝判据（本报告的"换人复判"部分）

| 编号 | 改哪里（一句话） | 红了谁 | 计数 |
|---|---|---|---|
| **Q1** | 网关的轮次账本从"只认第一个登记进来的决策"改成"后来者覆盖"（`Remember` first-write-wins 摘掉） | `CharacterIntelligenceSeamTests.She_learns_the_decision_the_model_read_not_the_one_the_gate_recheck_handed_back` | Api **1 红／184 绿** |
| **Q2** | "该会话恰好一轮在飞才记"改成"只要不是零轮在飞就记"（认不出唯一归属时按猜的记） | `…A_bubble_is_only_recorded_when_exactly_one_turn_of_that_conversation_is_in_flight` | Api **1 红／184 绿** |
| **Q3**（**作废**） | 首轮建状态域的失败逐出条件从"未成功完成就逐出"改成"被取消才逐出" | **无**——185/185 全绿 ⇒ 该突变对判据不敏感，不作数 | 重做为 Q3b；同时产出 §5 那条覆盖观察 |
| **Q3b** | 同一个洞，但改窄成"抛异常才逐出"（＝不再逐出被取消的那次建域） | `…A_failed_first_restore_does_not_switch_this_card_off_for_good` | Api **1 红／184 绿** |
| **Q4a** | 把"用户还在输入"那道闸挪到问完模型**之后**（挪成装饰） | `SingleInFlightTurnBoundaryTests.A_turn_is_never_advised_while_the_user_is_still_typing` ＋ 既有总闸 `RuntimeComponentTests.Reply_gate_defers_while_the_user_is_typing_or_output_is_queued` | Core **2 红／385 绿**（total 389，executed 387，含 2 跳过） |
| **Q4b** | 同样挪那道"还有排队输出"的闸 | `…A_turn_is_never_advised_while_the_previous_one_still_has_output_queued` ＋ 同一条总闸 | Core **2 红／385 绿** |
| **Q4c** | 队列清完后把"问一次"改成"问两次"（同一轮拿两份建议） | `…Once_the_queue_is_clear_the_same_turn_is_advised_exactly_once` | Core **1 红／386 绿** |

**如实登记的三点**：① Q1／Q2／Q3b **各只红点名那条**，所以那三条承重墙彼此不互顶——这正是 §5.0 判据④要的读法形状；② Q4a／Q4b 除新判据外还各自红了一条**既有**总闸（`Reply_gate_defers_while_…`），说明闸序不是只被新那两条守着；③ **本轮没有重复上一轮的错**：上一轮 M4 的第一版把闸移到 context 构造之后、`AdviseAsync` 之前，一条不红；本轮 Q4 系列直接改成"闸挪到问完模型之后"，第一次就红，无需作废重做（作废的是 Q3，另一处）。

---

## 4. `DEF-P3SEAM-04` 完成标准逐条对账（工单 §2 原文拆句）

| 完成标准 | QA 判定 | 依据 |
|---|---|---|
| ① 占位轮 `behavior_experience` 零新增、`exchange_count`／`familiarity`／`mood` 一字不动 | **Pass（判据面）** | 判据在册且**整格比对**（`Assert.Equal(before, after)`，record 值相等，不是逐列挑）；**N1／N4 两种方向都打红** |
| ② 同一轮她原本打算用的口癖：`UsageCount` 不 ++、`LastUsedAt` 不动 | **Pass（判据面）** | **N1／N4／Q6** 三处打红；该测试自带正向半段（真说了话的那轮必须照记）⇒ 上一条不是"没人选上口癖"的常绿 |
| ③ 同源：判定只有一处定义且在 Core，网关只调用；源码门带扫描器自证正向探针 | **Pass（判据面）** | `VisibleReply.cs` 是占位文本在四个产品层里的唯一命中；**N2**（内联第二套）红 2 条、**Q5**（缩扫描面）红 2 条 ⇒ 本体与守卫两面都有抓手 |
| ④ 同一档形状同一个出口（与 `DEF-P1WALK-07` 共用、不分叉） | **Pass（判据面）** | Core 三档 theory 与 `ModelReplyFormatTests` 在 **N3** 下**同场 7 红** ⇒ 想让可见侧与账本侧分叉，两侧的门会一起响 |
| ⑤ 不许过度修复：既有那道闸照旧有效、三条既有接缝判据一字不改仍绿、另补对照例 | **Pass（判据面）** | 对照例在册并由 **N4** 打红；`An_undelivered_turn_teaches_nothing` 在 14 次突变跑的红名单里**从未出现**（＝没有被"顺手关掉教学"这种改法顶掉） |
| ⑥ 历史错账口径落笔 | **由作者行使，QA 不复述内容** | 作者 2026-10-18 落笔为甲（旧行不动、只保证今后不再记）。实测印证一条形状：`88eff2a` 的 6 个文件里**没有任何迁移／schema／SQL 文件**（`git show --name-only` 手查），即口径取"不回滚"就没有一次性回滚代码，符合工单推荐的第三项 |
| ⑦ 每条新判据逐个突变反证并字节级还原，报告点名"哪处突变让哪条转红"；至少含"退回纯长度"与"摘掉 `NoteUsedAsync`" | **Pass** | §3.1 十条逐条点名；那两个方向分别由 **N1**、**Q6** 复现 |
| ⑧ `docs/phase2-runtime-contract.md` 若被牵动则文档先行 | **未被牵动（判定＝不适用）** | `git show --numstat 88eff2a` 六文件里没有契约文档；`IRuntimeOutputSink`／`MessageDeliveredAsync` 端口签名一字未动（反向约束③） |
| ⑨ 门禁九项（Rebuild 0/0、分程序集唯一 TRX、连续 ≥2 全绿、同场列出全部跑法与红例名、总数写全四项、measure 75 且 exit=0、verify 全过、diff --check 干净） | **Pass** | §0 第 1／8 条＋§2 全表；本轮**四次**全量而非两次，且正式跑法 31 次逐类列清，红例零条 |

**反向约束十条**逐条独立核对为"未被违反"：新判定住在 Core（`VisibleReply.HasVisibleContent` ＋ `internal const Unanswered`），网关只调用；`ModelReplyFormat` 没有整类转 public（`ModelReplyShape` 仍是 internal，这也是那条测试只能用字符串形状名而不是枚举当参数类型的理由）；`RuntimePlannedMessage` 没加"是不是占位"的字段；schema 一字未动；本刀没有顺手接 `ObserveFeedbackAsync`；没有改任何关系／熟悉度数值。**且本刀没有与 `DEF-P1WALK-07`／`-01`／`-02`／`-03` 合并处置**（各判据各载体各读数）。

⇒ **QA 判定：`DEF-P3SEAM-04` 可签（已复采·十条新判据承重墙已独立证伪）。** 上限照旧：本轮**没有新增任何用户可见能力**，只把"她不会把自己没说出口的话记成聊过"钉住；"按两下能教成一句"仍停在上一轮的判定，"越聊越懂你"仍不可写。

---

## 5. 新登记：`QA-TIP1019-01`（Low，Owner 建议＝Character Intelligence & Learning；QA 只转达、不代改）

- **事实**：`CharacterIntelligenceGateway` 里"缓存的首轮建状态域任务，只要不是成功完成就逐出重来"那半句（`:81`），今天**只被"被取消"那一种失败形状守着**。把条件改窄成"只有被取消才逐出"（Q3）⇒ 解决方案里 **Api 185/185 全绿**、六条接缝判据一条都不红（该突变因此作废、不算反证）；改窄成"只有抛异常才逐出"（Q3b）⇒ 立刻红那条对照例。两个方向都是收窄，区别只在收窄掉哪一半——**被取消那一半有判据，抛异常那一页没有**。
- **用户可见后果**：如果哪天某张卡的首轮建域是**抛异常**而不是被取消，现行代码会正确重来；但**没有任何测试会在有人把这段改窄时报警**——那才是本条要说的事。危害本身不在产品代码里，在判据的覆盖面里。
- **项目影响**：**不卡任何结案**（`0-2` 那三条的完成标准原文只要求"建状态域失败后第二轮还能重来"，"被取消"那一形状已由 Q3b 打红证明真被守着）。属"守卫覆盖面"一族的欠账，同族先例＝`QA-TIP1015-01`、`QA-TIP1016-02`。
- **推荐方案**：在既有那条判据旁边**加一个对照**（首轮建域改为"抛异常"形状），立刻得到＝两种失败形状各有判据；付出＝测试工程一条新用例，**可逆且零产品代码改动**。**明确不许**：不许为此动产品代码那半句的语义（它今天已经是对的）、不许把新对照写成与既有那条共用夹具的"取消形状重述"（那等于没有对照）、不许改那六条既有判据的文本。

---

## 6. Phase 3 验收面：本轮之后还欠什么（都不是代码）

1. **作者一句结案词**（我不写）。§5.0 阶段指针仍＝ **Phase 3**，不由 Agent 挪。
2. **文档文字分刀归位**（作者点头才能动）。本轮把这条从"叙事"升为"实测"：在 `88eff2a` 上，`docs/api-v1.md` 的新端点那几行**不存在**（`expression-feedback` 命中 0 次）、`docs/character-learning-safety.md` 里那套入口的文字**不存在**；未提交面里那些文件同时含别人名下的文本与我名下的文本，**一次提完会抹掉归属**，所以我不碰。
3. **仍未解除的门（照旧有效，本轮一条没动）**：`2-D` 先接线后标定；第 6 层标定轮；`0-6`／`DEF-P1WALK-05`（换后端后恢复）；真机路径 C 欠作者一次托盘点击；`QA-TIP1015-01`；`QA-TIP1016-01`／`-02`；§6 裁定③。

---

## 7. 下一步归属

1. **作者**：① 点头提交本轮产物（本报告 ＋ `docs/eval/README.md` 一行 ＋ 台账 §4.2／§9 与那一页 §0 的五条事实），并给那批未提交的文档文字分刀归位——**顺带包括那张仍未跟踪的工单 `docs/dispatch-p3seam04-placeholder-turn.md`**（它被三处引用却从未提交过，本角色 2026-10-19 一度误把它删掉、已按字节还原）；② 托盘上真点一次「退出…」并确认；③ tag／push／打包仍缺明令。
2. **QA（本轮之后仍欠的）**：`DEF-P4LEARN-01` 若再有第二刀（例如 `FollowUpEvidence` 起来源）从新 SHA 复采；`0-6`／`DEF-P1WALK-05` 与真机路径 C 不在 QA 能自证的范围内，等作者的动作。
3. **Character Intelligence lane**：`QA-TIP1019-01`（Low，按 §5 的补法）＋上一轮仍挂着的 `QA-TIP1016-02`（Low，形状门文字强于断言）。**两条都不许以修判据为名改用户可见行为。**
4. **守门人／发行 lane**：`QA-TIP1016-01`（Medium）仍待作者点头才派工；`main` 是否跟到 `88eff2a` 等明令（**本轮未动 ref、未打 tag、未 push、未新建提交、未打包**）。§5.0 指针与结案词不由 Agent 写。
