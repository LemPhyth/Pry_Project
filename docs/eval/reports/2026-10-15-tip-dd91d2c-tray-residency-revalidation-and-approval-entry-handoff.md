# 线尾 `dd91d2c` 门禁复采 ＋ 第 0 层 `0-5` 托盘常驻的独立证伪 ＋ 第 1 层认可入口那一刀的交接判定 · 2026-10-15

- **Run ID**：`eval-QA-TIP1015-1015`
- **判定人**：QA / Evaluation（EVAL-001，同时以守门人身份登记台账）
- **派工原话**：作者 2026-10-15「按你说的已经派工完成了，你看看」；工程侧承接＝`0-5` 那一刀 §9 登记「下一步归属」第 ① 条原话「QA 从 `dd91d2c` 建干净 `--detach` 载体复采——它是 `f6c9750` 的后代，一次可同场结掉上一轮登记的"正式复采未做"」。
- **载体＝一个 SHA**：`dd91d2c`（`test(app): make the tray-residency ruling decidable on three surfaces`，8 文件／767 插入，父 `f6c9750`）。我从它新建自己的 `--detach` worktree `.local-notes/qa-tip-1015`，**没有复用任何其他角色在同一 SHA 上的结果目录**。建完 `git status --short` 只剩 `?? .qa-run/`（我自己的 TRX、构建日志、突变备份与还原脚本，不属仓库内容，`.gitignore` 之外的一层临时面）；跑完（含四次突变与还原）**仍是同一行**。
- **一次复采覆盖的账**：`dd91d2c` 是 `f6c9750`／`30a9c8b`／`2ee3d5d`／`33f7935`／`cc1169f`／`b15ed9d`／`398101a`／`eb01acf` 的后代 ⇒ 上一轮"这几刀只有 Owner 自证、QA 未从可指认提交复采"那笔欠账**本轮同场结清**，不需要单独再采。

## 0. 结论先写在最前面

1. **复采成立**：`dotnet build Pry.slnx -c Release -t:Rebuild` **0 警告／0 错误**（日志 `.qa-run/rebuild.log`，当场数到 9 个程序集全部重新产出）；解决方案级**连续三次 854 通过／0 失败／2 跳过（总 856）**，三次分项**逐位相同**＝Client 93／App 214／Api 164／Core 383＋2skip；`measure-fixed-eval.ps1` 对三个独立结果目录各 **`exit=0`**，且每份都打印 `Catalog valid: 75 permanent scenarios.` 与 `Passed=854; Failed=0; Other=2.`；`verify-repository.ps1` 通过 **449** 个受跟踪文件；两条跳过恒为 `CoreTests.Configured_sense_voice_model_can_run_offline` 与 `LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`（从 Core 的 TRX 里按 `outcome="NotExecuted"` 逐条读出，不是转述）。**红例清单：零条。**
2. **门禁基线自此是 854／0／2**，上一轮在册的 832 与 `398101a` 的 819 全部过期。**但引用 854 必须带全四项**——见第 5 条。
3. **`0-5` 的承重墙由我自己证伪，没有采信写它的人自报的十处突变**（契约 §8.2「Owner 自证 ≠ QA 证据」）。四处独立反证、逐个点名转红、`sha256` 前 16 位与committed blob 双重核对还原（详见 §3）：删退出四步里的 `retireTrayIcon();` ⇒ **2 红／7 绿**；把"关窗只收窗"那条决策写成永远 false ⇒ **1 红／8 绿**；把托盘菜单标签从 `退出…` 改成 `Exit` ⇒ **1 红／12 绿**；第三条那种形状**在他们的十处清单里没有**。
4. **一处关于判据自身的事实（新登记 `QA-TIP1015-01`，Low，Owner 建议＝Windows Client 那条 lane，QA 只转达不代改）**：源码门 `TrayResidencyBoundaryTests` 的扫描面是 `App.axaml.cs` 与那一处 `Closing` 谓词，**不含 `WindowLifecycle.cs` 的决策本体**。我把决策本体掏空后单独跑那条门 ⇒ **13／13 全绿**（`.qa-run/M2-sourcegate-only.log`）。含义说准：**"关窗只收窗、退出才真退"这条作者 2026-10-12 裁的 A，今天只有一条真值表用例在守**（`Only_an_approved_exit_may_actually_close_the_window(exitApproved: False, expectedHide: True)`）——它守住了，本轮实测会红；但删掉它不会让任何一道门变红。这不是产品缺陷，也不动摇第 1 条的复采结论；它是"门的分工"里一条已知空隙。
5. **`DEF-P4LEARN-01`（认可入口）今天不可签**：那 16 个文件此刻仍在主工作树未提交、**没有 SHA**，因而不满足"从可指认提交复采"这条最低验收面。按 §8.2，那条 lane 自报的 **876／0／2** 与其 10 处突变**一律不引用为 QA 证据**。同时立一条防顶账规则：**854 这个总数在同一台机器上有两份互不相同的账**——本载体（`dd91d2c`，App 214／Api 164）与该 lane 在旧基座 `f6c9750` 上采到的 854（App 199／Api 179），**同数不同项**；她在 `dd91d2c`＋本刀 16 文件上是 876（App 221／Api 179）。这正是 §5.0 判据 ④"读数不得互顶"的具体形状，此后任何引用写全四项（Client／App／Api／Core）才算一个读数。
6. **真机那三条关闭路径：QA 没有独立重跑，因此不签**，也不引用 Owner 的 walk2 为证据。我独立核的是**仪器自己的约束面**（逐行读 `scripts/verify-window-close-paths.ps1`）：它**从不强杀**（`taskkill` 只出现在"这不是判据"的注释里）、`-DataDirectory` 缺失就中止、**拒绝 live 数据根本身及其任何子目录**（用 `[System.IO.Path]::GetFullPath` 而不是 5.1 上没有 `-Qualifier` 的 `Split-Path`）、会话令牌只留长度与 8 位 SHA-256 指纹、窗口没起来时把 A 整段判 **BLOCKED 而不是猜**、路径 C 判 **BLOCKED 而不是 FAIL**。⇒ **路径 C 今天零份可归因证据**，缺的那一步只能由人在托盘上点「退出…」并确认；按本行反向约束不许拿硬杀冒充点头、也不许为凑绿灯加一个脚本可触发的退出后门。**这是仪器够不着，不是产品坏。**
7. **`0-2` 那六条判据的"非空转"仍挂待独立复判**：本轮证伪面全在 `0-5`，那六条与它的突变出自同一角色（我当年以 Intelligence 身份写的），我没有重跑，也不签。
8. **我不写结案词**：§5.0 阶段指针不动，`0-5` 的状态词与"结案"两字归作者与守门人；`0-4`（`RT-008-PKG`）继续缺 tag／push／打包的明令，本轮**没有打 tag、没有 push、没有打包**。

---

## 1. 说人话（作者只要这一段）

- **能做到**：今天最新那一刀（把"关窗只是收进托盘、只有托盘里点退出才是真退出"写成机器能判的规矩）我在一份干净副本上连跑三遍，**854 条全绿、零红**；而且我不信写它的人自己报的数，自己动手拆了四处——拆掉退出的第三步、把"关窗=收窗"改成"关窗=退出"、把托盘菜单上的中文换成英文，测试分别红了 2 条、1 条、1 条，说明这些规矩**真的有人在看**。上一版那笔"新版本没人复采"的欠账，这一次同时把前面几刀的账一起结掉了。
- **做不到**：**"她学你点头喜欢的说法"今天还不能算数**——那条 lane 当天真的做完了，我也读到了形状对不上的地方会被拒（409），但那 16 个文件**还在你机器上没提交**，没有提交号我就没法从干净副本复采，所以按规矩只能记"已落地·不可签"。另外**真机那三条关闭路径里，"用户真的退了一次"这一条至今零证据**，因为脚本点不到托盘图标。
- **挡路一件事**：只有一句——**你点头提交那 16 个文件**。提交之后我从新提交号建干净副本再采一次，一条命令的活，那半句"她能用你的点头改自己的说法"才第一次可写。
- **要拍哪件**：本轮不需要你拍判据。还欠的是那三样老账：**托盘上真点一次「退出…」**（这一下只有你的手能给）、**打 tag 与允许推送**（要不要钉一个版本号、要不要推远端，这两件我们不会自己做）、以及裁定③（`PLAT-004`/`007` 进不进本轮，第 5 层之前拍就行）。

---

## 2. 复采读数（同一 SHA、三跑、全部当场采集，证据留在载体里）

| 跑法 | 结果目录 | 分项（TRX `<Counters>`，逐个从文件里读，不是控制台转述） | `measure` |
|---|---|---|---|
| runA（解决方案级） | `.qa-run/runA/` | Client 93 passed／App 214／Api 164／Core total 385 executed 383 passed 383 | `exit=0`，`Passed=854; Failed=0; Other=2`，catalog 75 |
| runB（解决方案级） | `.qa-run/runB/` | 同上四项逐位相同 | `exit=0`，同一行 |
| runC（解决方案级） | `.qa-run/runC/` | 同上四项逐位相同 | `exit=0`，同一行 |

**同场列出全部跑法**（按台账纪律，含突变跑与仪器跑）＝**3 次解决方案级 ＋ 3 次定向（App 程序集，突变期）＋ 1 次定向（掏空决策体后单跑源码门）＝ 7 次测试调用**；正式配对读数取前三次。另有 1 次 `-t:Rebuild` 全量构建（报告出稿前重取，确认未提交面之外的那棵树仍 0／0）。

**仪器事实两条，写给下一个在同一台机器上跑门禁的人**：
- 默认 TRX 命名在解决方案级会跨程序集撞名（同一时间戳的四个文件靠 `[1]`／`[2]`／`[3]` 后缀区分，runB 里出现同名不同秒）。**能用的判据只有 `<Counters>` 里的 `total/passed` 逐文件读**；看到"一个文件里只有 Core"就是撞名，不是产品少测了。本轮四个程序集各自有文件，才允许 `measure` 报 854。
- 跑前查机器：**可用 commit 6.6 GB、空闲物理 8.8 GB**；`dotnet.exe` 8 个进程命令行逐条读完，全部是 SDK 的 `MSBuild.dll /nodemode:1 /nodeReuse:true` 驻留节点，**没有他人的 `dotnet test` 在同树并发**；占 commit 的是作者自己开着的 ComfyUI（`python pid 49336`，私占约 9.8 GB）。**没有动作者的任何进程**（`.local-notes/preflight-1015c.ps1`／`preflight-1015c.txt` 留了当场读数）。这条前置来自 `0-5` 那一刀登记的同族事故：可用 commit 被压到 3.9 GB 时，Core 会报 `SQLite Error 10 disk I/O`／`Error 7 out of memory`，那种读数**作废而不是改判据**。

---

## 3. 独立证伪（四处，全部只落在 QA 自己的载体；还原用两套独立核对）

还原核对两套：① 突变脚本按 `sha256` 前 16 位断言"还原后必须等于入场基线"，不相等就中止（`App.axaml.cs` = `e931aa4e2259b223`、`WindowLifecycle.cs` = `d64f3516fe4930f2`）；② 事后另用 `git hash-object` 对两份备份与 `git rev-parse HEAD:<同路径>` 逐一对齐＝`f435ac9e0c1c…` 与 `672556d496b0…`，即**备份内容就是那把提交里的对象**，不是"我留着的一份改动"。跑完 `git status --short` 回到只剩 `?? .qa-run/`，且 `git diff --stat HEAD -- src tests` 无输出。突变脚本 `mutate-05.py`／`mutate-05b.py` 留在结果目录里，锚点命中次数不为 1 就拒绝写入。

| 编号 | 改哪里（一句话） | 红了谁 | 计数 | 证据 |
|---|---|---|---|---|
| **M1** | 把退出四步里的"收起托盘图标"那一步删掉（次序 coordinator 少一步） | `WindowLifecycleTests.The_exit_steps_run_in_the_locked_order`、`…Each_step_waits_for_the_previous_one_to_finish`（`Expected: "tray" / Actual: "shutdown"`） | **2 红／7 绿**（总 9） | `.qa-run/mut-M1-order.log` |
| **M2** | 把"是否该收窗"这条决策改成永远答"不收窗"（＝关窗变成退出，正是裁定 A 的反面） | `WindowLifecycleTests.Only_an_approved_exit_may_actually_close_the_window(exitApproved: False, expectedHide: True)` | **1 红／8 绿** | `.qa-run/mut-M2-hide-always-exit.log` |
| **M2b** | 同一处掏空，**只跑源码门**（不跑真值表） | 无人红：`TrayResidencyBoundaryTests` **13／13 绿** | 0 红／13 绿 | `.qa-run/M2-sourcegate-only.log` ⇒ 第 0 节第 4 条 `QA-TIP1015-01` |
| **M3** | 托盘右键菜单的 `退出…` 改成英文 `Exit`（前置条件改名，形状不塌但用户找不到出口） | `TrayResidencyBoundaryTests.Tray_residency_preconditions_stay_declared` | **1 红／12 绿**（总 13） | `.qa-run/mut-M3-menu-label.log` |

**如实登记的三点**：① M1 与写它的人清单里"删四步中任一步"是同一形状，**我这一处不构成新增覆盖**，只是独立复现；② M2 与他们的"`Closing` 换回 `if (_allowClose) return;`"打的是同一个用户可见后果，但**改的地点不同**（他们改调用点，我改决策本体），而 M2b 恰恰量出"改决策本体只有真值表那一条会红"；③ M3 在他们的十处清单里**没有对应项**，是本报告新增的那处覆盖。

---

## 4. `0-5` 完成标准逐条对账（对照执行基准 §3 那一行的出判面）

| 完成标准（原文拆句） | QA 判定 | 依据 |
|---|---|---|
| 托盘图标常驻可见、右键菜单给出"退出" | **Pass（判据面）** | `Tray_residency_preconditions_stay_declared` 钉住 `IsVisible = true`／`Menu = menu`／`Icon = CreateApplicationIcon()`／`NativeMenuItem("退出…")` 四项，M3 证明它不是常绿 |
| 关窗只收窗、进程与内嵌后端宿主继续存活 | **Pass（判据面）＋ 真机部分只有 Owner 自证** | 源码面：`ShouldHideToTray` 真值表＋"收窗路径不得注销事件流／不得出现 exit-only token"；真机面：walk2 的 A 四条／B 属 Owner 自证，**本报告不引用为 QA 证据** |
| "退出"菜单项才是真退出，且退出时连内嵌后端一起退、不留孤儿 | **判据面 Pass（次序被钉住）／真机面 Blocked** | 四步次序 coordinator ＋ M1；路径 C 三条判据**零份可归因证据**，缺一次人工点击 |
| 三条关闭路径可脚本化判定"进程／宿主／`runtime.json`" | **部分成立**：A、B 可全自动，**C 只能"人工触发＋脚本判定"**，脚本对此的处理是判 BLOCKED 而非 FAIL——**这个处理本身是对的**，把它改成 FAIL 或加后门才是破口 | 读脚本 §0 第 6 条 |
| 收窗路径必须判成"仍在运行"而不是"已退出" | **Pass（判据面）**：可见性不得成为心跳／事件流的开关（`VisibilityTokens` 那条门）；实际投递需已配置模型与房间，仍归第 5 层走查与 Eval | Owner 登记第 ③ 条自留待办，QA 复述为边界不是遗漏 |
| 反向约束：不许碰 `MainWindow` 家族 | **Pass** | `dd91d2c` 的 8 个文件里没有 `MainWindow.axaml*`；载体上 `PryMessengerWindow.axaml.cs` 的该刀只有 1＋/1−（`git show dd91d2c:` 内 `feedback` 命中数 **0**，即没有把并行 lane 的在途 37 行顺手提走） |

## 5. 第 1 层那一刀（`DEF-P4LEARN-01`）的可归因面

**本轮 QA 对它只有一条可签结论：不可签。**能如实写的只有三件：① 主工作树里那 16 个文件的存在与归属（12 改＋1 新服务＋3 份新判据文件）我当场用 `git status --short` 对过名，全部落在 `DEF-P4LEARN-01` 那条 lane 名下，**我没有碰它们中的任何一个**；② 那条 lane 的 §3 段落此刻与我的 §0/§3 更新混在同一个未提交的 `docs/full-scope-sprint.md` 里 ⇒ 守门人本轮**只提交自己那两处 hunk**，他们的段落留在未提交面；③ 一旦作者点头提交，新 SHA 是 `dd91d2c` 的后代，**下一次复采可与本轮共用一张对账表**（不必再单独回到 `f6c9750` 或 `398101a`），但读数必须按第 0 节第 5 条写全四项，否则会和 `0-5` 的 854 顶账。

## 6. 下一步归属

1. **作者**：点头提交 `DEF-P4LEARN-01` 那 16 个文件（＝"让它进一个可指认版本"这一句话）；在托盘上真点一次「退出…」并确认（之后把 `-ExitBudgetSeconds` 加长再跑一轮，路径 C 三条一次采齐）；tag／push／打包仍缺明令。
2. **QA（本轮之后仍欠的）**：从认可入口的新 SHA 建干净载体复采并出正式报告号；`0-2` 那六条判据的非空转**换人复判**。
3. **Windows Client lane**：`QA-TIP1015-01`（Low）——把 `WindowLifecycle.cs` 的决策本体纳进源码扫描面，或给 `ShouldHideToTray`／`IsConfirmed` 各补一条"两档都验且红在点名那条"的门；**不许**为了这条改动任何用户可见行为。
4. **Intelligence lane**：`DEF-P3SEAM-04` 按原编号处置（占位符 `……` 进互动计数那一层）；`FollowUpEvidence` 仍零来源＝`RT-006` 没被顺带做掉。
5. **守门人**：`docs/full-scope-sprint.md` §0 与 §3 `0-5` 行的事实部分本轮已更新；`main` 按「可以并线」这句明令继续用 ref-only fast-forward 跟线尾；**不打 tag、不 push、不打包**。§5.0 指针与结案词不由 Agent 写。
