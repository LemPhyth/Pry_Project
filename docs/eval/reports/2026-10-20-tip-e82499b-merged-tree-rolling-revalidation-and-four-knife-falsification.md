# 线尾 `e82499b` 合流树滚动复采 ＋ 并进来的四把刀逐条独立证伪（18 处突变，17 处成立 ＋ 1 处全绿）· 2026-10-20

- **Run ID**：`eval-QA-TIP1020-1020`
- **判定人**：QA / Evaluation（EVAL-001），同时以守门人身份登记台账
- **派工原话**：作者 2026-10-20（本轮第四次）「**按照守门人的派工进行工作**」。这一句在受跟踪基准页上只点名一格，逐字＝`docs/full-scope-sprint.md` §0「**⇒ 同一轮再补一格：QA 这一刀现在就能开工，不必等那两张新单——从线尾建干净副本复采＋逐条拆，验的正是已经并进来的四把（记忆管理、换后端自愈、插件第一期、安全/打包守卫）**」，载体更正逐字＝「**所以让人从最新的线尾取副本，而不是从那一刻之前的提交取。不改会怎样＝别人数回来的条数比现在少 8，那两把新写的守卫也没人替我们拆过一遍**」，同一页另有一句「**在册那 886 一字不动——QA 还没从合流树复采，改写权在他们手里**」——改写权在本角色手里，本轮就是执行这一句。
- **载体＝一个 SHA**：`e82499b2a81f98463d281c1910889e52289ec85d`（`docs(sprint): put the doc-shape round on the author's four-line page`）。从我自己的 `--detach` worktree `.local-notes/qa-tip-1020` 起跑，**没有复用任何 lane 的自跑树**（§5.0 判据④"读数不得互顶"）。建完 `git status --short` **0 行**，18 处突变每次还原后 **0 行**，出稿前重核仍 **0 行**、`git rev-parse HEAD` 仍是这一号。
- **一次复采覆盖的账**（`git log` 逐条对过，不是引用谁的转述）：`e82499b` ← `8e39c68` ← `0f2e7f1` ← `119c0d4` ← `ab984bc` ← `98dbdae` ← `2a5197a` ← `f0827c2`（第三条池键）← `ee194e6` ← `ede6a6d`（等窗口）← `51eb636` ← `c0b90c1`（插件第一期合并）← `2711c2f`（换后端自愈合并）← `2a76470`（安全/打包两把＝`30a7a5a` 本地档回环、`8f7dcb1` 发行形状门）← `e2b521a`（记忆管理）← 已复采过的 `88eff2a`／`863604a`／`c608220` 全链 ⇒ **本轮之前欠着"只有写它的人自证"的那五把（记忆管理、换后端自愈、插件第一期、安全/打包守卫、两把测试工装刀）在这一份读数里一次结清。**
- **零产品代码改动、零判据文本改动、零阈值改动**：只读、只跑、只在载体里做可还原的单点突变。**未 push、无 tag、未执行过一次打包**（那三件仍缺作者明令）。

---

## 0. 结论先写在最前面

1. **复采成立**：`dotnet build Pry.slnx -c Release -t:Rebuild` **0 警告／0 错误**（`run6.build.log` 与 `run7.build.log` 各当场数到 **9 个程序集全部重新产出**）；解决方案级**连续七次 988 通过／0 失败／2 跳过（总 990）**，七次分项**逐位相同**＝Client total 101 executed 101 passed 101／App 233/233/233／Api 248/248/248／Core total 408 executed 406 passed 406。**在册基线自本起＝988／0／2（总 990），上一版的 886／0／2（总 888）自此过期**；更旧的 876／854／832／819 一并过期。
2. **跑位是刻意的**：**run1／run2／run3＝入场基线（在任何一次突变之前）**，**run4＝前 15 处突变全部还原之后**，**run5／run6＝第 16 处（打在记忆管理读口上）还原之后**，**run7＝全部 18 处（含那处打在界面分组上、以及那处打在 legacy 删除路径上的）还原之后**。七次与三次入场基线逐位相同 ⇒ 还原是真还原，不是"看起来干净"。
3. **四把并进来的刀，每一把的承重墙都被我亲手拆过并且当场报警**（逐项对账见 §3，共 18 处突变＝17 处成立 ＋ 1 处全绿）：记忆管理 5 处（M1／M2／M3／M4／M16）、换后端自愈 2 处（M8／M9）、插件第一期 2 处（M5／M6）、安全与打包 4 处（M7／M13／M14／M15）、测试工装两把 3 处（M10／M11／M12）、legacy 作用域限定 1 处（M18）、外加一处**故意让形状串留着、只掏空本体**的反面探针（M17）。
4. **`QA-TIP1019-05` 那一格就地结案**：那一刀（`f0827c2`）自己写过"有一道门第一次拆掉时没报警，查出来是漏了一条断言，补上后重拆才报警"。**本轮由 QA 独立重拆那道补上的门**＝M12（把"偷偷关掉连接池"的规避放回 fixture 里）⇒ **恰好 1 红**＝`TestFixtureTeardownLockTests.The_fixture_that_retired_the_pool_dodge_shuts_down_through_the_same_door`。上一轮的"自报"这一轮换成"别人拆过"。
5. **一处负面结果，是真缺口不是运气好**（新登记 `QA-TIP1020-02`，Low，**不卡任何结案**）：M17 把界面"分两组"的**本体掏空**（`details.Where(x => x.Status == "orphaned")` 后面接 `&& false`，孤儿那一组永远空）**而形状串原样留着** ⇒ **988 条全绿、零红**。那一面今天只有源码形状门（`MemoryManagementSurfaceTests` 的 `Assert.Contains("Status == \"orphaned\"", source)`），它自己的 docstring 就写明"注释里出现同样字样也算"。⇒ 这是 `QA-TIP1015-01` 那一族（掏空本体而扫描门仍绿）**第一次在记忆管理这条 lane 上复现**，不在上一轮的清单里。详见 §5。
6. **一处我起手时的判断被自己纠正**（自记不藏）：起手的笔记里我把 `e2b521a` 关于 N11 的那句写成"过度声称"。**把提交信息原文读全之后这句不成立**——它逐字写着「**the HTTP controller still binds by characterId because there are no instance producers yet — potential, not a live defect**」，已经把 HTTP 那一半与"潜在不是当下事故"披露在同一把里。本轮实测到的事实只有三条：`MemoryDatabase.cs:566／581／607` 三处仍硬编码 `scope_key='character:'||$c`；`src/` 内 `instance:` 生产者**零个**（独立 grep）；这条硬限定**不是无人守的**——M18 把它从 legacy 删除里摘掉 ⇒ **恰好 1 红**＝`ApplicationServiceTests.Character_and_client_preferences_are_owned_by_backend_without_path_leaks`。⇒ **不新开编号、不派工**，只作为事实登记（见 §6）。这是本册"动手前把原始文本读完整"那条纪律的第三次触发。
7. **门禁其余面**：`measure-fixed-eval.ps1` 对 run1…run7 **七个**独立结果目录各 **`exit=0`**，每份都打印 `Catalog valid: 76 permanent scenarios.` 与 `Passed=988; Failed=0; Other=2.`；`verify-repository.ps1` 在载体上通过 **485** 个受跟踪文件（本轮零增删文件）；`git diff --check` 干净；**红例清单：零条**；两条跳过恒为 `CoreTests.Configured_sense_voice_model_can_run_offline` 与 `LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`（按 `<UnitTestResult outcome="NotExecuted">` 逐条点名，**不看 `<Counters notExecuted>`——那个字段七次全都恒为 0**）。
8. **判定（QA 侧形式；状态词与结案词归作者与守门人）**：`RT-005-MANAGE`（记忆管理 `e2b521a`）、`0-6`（换后端自愈 `2711c2f`）、`RT-007` 第一期（插件 `c0b90c1`）、安全/打包两把（`2a76470`＝`30a7a5a`＋`8f7dcb1`）、测试工装两把（`ede6a6d`＋`f0827c2`）＝**已复采·承重墙已独立证伪·可签**；其中记忆管理那一把的界面分组那一面**带一条不卡结案的覆盖欠账**（`QA-TIP1020-02`）。**在册基线改写＝988／0／2（总 990），由本角色行使改写权。**
9. **§8.2 的"同一角色自证不签"本轮没有例外**：以上每一条红例名都出自**本角色**的载体、本角色的驱动脚本、本角色的 TRX 解析；上一轮那两处作废的突变没有被引用，本轮那 18 处**没有一处作废**（M17 的"全绿"不是作废，是它要问的那个问题的答案，按负面结果如实登记）。

---

## 1. 说人话（作者只要这一段）

- **能做到**：你说的那句"从最新的线尾取一份干净副本、自己复采、自己逐条拆"，今天做完了。副本是从 `e82499b` 建的，跑门禁之前先数到 **988 条全绿、零红**，一共跑了**七遍**，七遍的分项数字一位不差（客户端 101／界面 233／后端 248／核心 406 通过＋2 条环境跳过）。我不信任何写代码的人自己报的数，自己动手拆了 **18 处**：把"确认保留"不抬版本（她引用不回来）、把"同指纹已被占用"的前置检查架空（该回 409 的变成 500）、让删会话顺手抹掉**跨会话那条证据**、让界面删除按钮不再先问一句、把管理读口改回"只取在用的"（依据已删的那一组又看不见）、不通知界面重订事件流、把恢复演成成功、出厂白名单当成全放行、放过 `file` 协议、本地档允许填远程地址、把"脏树不许出包"那行守卫挪进"跳过验证"的分支里、把包里的来源提交号写死、缺运行库改成静默跳过、以及测试收尾那三把（漏清第三条连接池键、等窗口用尽就吞掉失败、偷偷关掉连接池）——**17 处各自把守着它的测试当场打红，红的全是点名那几条，没有一条不相干的**。所以"在册 886"这一格今天换成 **988**，改写权在我手里，这次改的是**事实**不是加法。
- **做不到**：**有一处拆出来是全绿的**（这轮唯一的坏消息，也是唯一的新账）：界面上"她正在用的／依据已被你删掉的"分两组那件事，**把判断条件掏空、只把那行字留在原地，988 条一条都不会响**。今天界面照常分两组（我跑过、也看着它绿），所以这不是坏了，是**没人守着"别把它掏空"**。已按老格式登记＝`QA-TIP1020-02`（Low，Owner 建议＝记忆那条 lane，**不卡任何结案**），修法＝加一条形状判据，**不许为此改产品行为**。另外三件照旧：**① 没有可安装的版本**（没打 tag、没推远端、没打包，那三件我们不会自己做）；**② 真机"你真的退过一次"那一条还是只能由你的手给**（托盘里点「退出…」并确认，脚本点不到托盘）；**③ 参数还没标定、备份与恢复还没有人认领执行**（`PLAT-003` 的前置早就达成，缺的是传话开工）。
- **挡路一件事**：**技术动作这一格现在不缺**——五把刀的复采与逐条拆都齐了，基线也换了。挡路的只剩**你手上的三个动作**：托盘那一次点击、tag／push／打包的明令、以及各阶段的结案词（那句我不写）。
- **要拍哪件**：三件，全是老账，**本轮没有新东西要你拍**。① **要不要真出一个可安装版本**（我实测过：只有你这台带着那份不进版本库的运行库的机器能出包）；② **托盘那一次人工点击**；③ 裁定③（那两个平台档位进不进本轮）。

---

## 2. 复采读数（同一 SHA、七跑、全部当场重采，证据留在载体之外）

| 跑法 | 位置（相对突变） | 分项（逐个 TRX 的 `<Counters>`，不是控制台转述） | `measure` |
|---|---|---|---|
| run1 | 入场基线，**突变之前** | Client 101/101/101／App 233/233/233／Api 248/248/248／Core total 408 executed 406 passed 406；failed 全 0；NotExecuted 标记 2 | `exit=0`，`Passed=988; Failed=0; Other=2`，catalog 76 |
| run2 | 入场基线第二次（配对用） | 四项逐位相同 | `exit=0`，同一行 |
| run3 | 入场基线第三次 | 四项逐位相同 | `exit=0`，同一行 |
| run4 | **前 15 处突变（M1…M15）全部还原之后** | 四项逐位相同 | `exit=0`，同一行 |
| run5 | **M16（打在 `SqliteMemoryRepository.ListAsync` 上）还原之后** | 四项逐位相同 | `exit=0`，同一行 |
| run6 | 同上，入场 `-t:Rebuild` 日志数到 **9 个程序集**、0/0 | 四项逐位相同 | `exit=0`，同一行 |
| run7 | **全部 18 处（含 M17 那处全绿的反面探针、M18 那处 legacy 删除）还原之后**，入场 `-t:Rebuild` 同样 9 个程序集、0/0 | 四项逐位相同 | `exit=0`，同一行 |

**同场列出全部跑法**（按台账纪律，含突变跑与仪器跑）＝**7 次解决方案级全量 ＋ 15 次全量级突变跑（M1…M12、M16、M17、M18）＋ 3 次定向突变跑（M13／M14／M15＝`Pry.Api.Tests` 里 `ReleaseAuditabilityShapeTests` 那 4 条）＝ 25 次测试调用**；正式在册读数取那七次全量。另有 **5 次 `dotnet build … -t:Rebuild`**（两份留日志、两份只跑构建、一份入场）、**15 次驱动脚本内的定向 `dotnet build`**、**7 次 `measure`**、**4 次 `verify-repository`**（485）、**3 次 `git diff --check`**、**8 个被碰过的文件的 `git diff HEAD --quiet` 逐一核（全 `SAME`）**。

### 2.3 本刀（文档面）落地前的一次独立门禁（AGENTS 第 9 条，跑在主工作树）

零 `src/`、零 `tests/`、零 `scripts/` 的文档刀也照规矩跑一遍：`dotnet build Pry.slnx -c Release -t:Rebuild` ＝ **9 个程序集、0 警告 0 错误**；解决方案级 `--no-build` 带唯一结果目录 ⇒ **Client 101／App 233／Api 248／Core total 408·passed 406＋2 skip（总 990）、零红例**，与载体那七次**逐位相同**；`measure-fixed-eval.ps1` 对该结果目录 **`exit=0`**、`Catalog valid: 76 permanent scenarios.`、`Passed=988; Failed=0; Other=2.`；`verify-repository.ps1` **485** 个受跟踪文件通过；`git diff --check` 干净。并入同场计数＝**26 次测试调用**（正式在册读数仍只取载体那七次全量，本格是文档刀的自检不是第八次复采）。证据＝`.local-notes/qa-tip-1020-run/maintree-precommit/`（结果目录在主树跟踪面之外，`git status --short` 提交前＝**4 行**＝本刀名下三份文件（`docs/full-scope-sprint.md`、`docs/eval/README.md`、本报告）＋他人名下未跟踪草稿 `docs/character-learning-safety.patch`，**没碰**）。这一格的用处：**在册 988 不只由 QA 自己的 `--detach` 副本背书，文档刀落地前在同一份内容上又数到一次**。

### 2.1 还原核对（三层，逐条落盘）

① 驱动脚本（`.local-notes/qa-tip-1020-run/drive.py`／`drive_script.py`，两者都在载体之外）在 `finally` 里把入场那份**原字节**写回，并立刻断言"读回来 == 入场那份"——**18 处全部 `restored=True`**；② 出稿前对八个被碰过的文件逐个跑 `git diff HEAD --quiet -- <file>`：`src/Pry.Core/Memory/SqliteMemoryRepository.cs`、`src/Pry.Core/Memory/MemoryDatabase.cs`、`src/Pry.App/PryMessengerMemoryWindow.cs`、`src/Pry.Api/Capabilities/DesktopCapabilities.cs`、`src/Pry.Api/Services/ConfigurationApplicationService.cs`、`src/Pry.Client/PryBackendClient.cs`、`tests/Pry.Api.Tests/SqlitePoolCleanup.cs`、`scripts/package-release.ps1` ＝ **8 份全 SAME**；③ 此后 `git status --short` **0 行**、`git diff --check` 干净、`git rev-list --count main..HEAD` ＝ **0**。

### 2.2 锚点与仪器约束（写给下一个在这台机器上跑门禁的人；本轮两条新坑，都是我自己踩的）

- **`.gitattributes` 把 `scripts/**` 钉成 `eol=crlf`，而 `**/*.cs` 钉成 `eol=lf`**（载体上 `git ls-files --eol` 当场读到：`i/lf w/crlf attr/text eol=crlf → package-release.ps1`；`i/lf w/lf attr/text eol=lf → SqliteMemoryRepository.cs`）。⇒ **同一份突变驱动里，多行锚点对 `.cs` 命中、对 `.ps1` 恒命中 0**，因为脚本里的锚点用 `\n` 拼。M13 因此在两次跑里打印 `skip: anchor count=0`（`drive_script.py:43` 那句"命中次数≠1 就拒绝写入"救住了现场，一个字节都没写坏）。**教训的一般形式：`skip` 不是"这一处不需要拆"，而是"这一处今天没有非空转证据"**——把它当成跑过了，就等于把一堵没拆的墙写进"已证伪"清单。修法一条：**写多行锚点之前先读目标文件的 `git ls-files --eol`，或干脆把 `\n` 归一化成它的实际行尾**（本轮 `drive_script.py` 现在在匹配前做 `old.replace("\n","\r\n")`）。
- **`md5sum <worktree 文件>` 与 `git show HEAD:<file> | md5sum` 不相等 ≠ 文件被改坏**。本轮差点在这里判错：`package-release.ps1` 两份 md5 不同（`13268f36…` vs `f2ba8012…`），而 `git status --short` 是 0 行。真实原因就是上一条——工作树是 CRLF、blob 是 LF，git 比的是**归一化之后**的字节。**权威判据只有两个**：`git status --short` 行数与 `git diff HEAD --quiet -- <file>` 退出码。
- **锚点的缩进不许猜**：M8／M9 第一次两跑都 `anchor count=0`，我把 8 空格猜成 12 空格（`grep -n … | cat -A` 才看清）。第二次改成按行号取锚点后 12 处一次跑通。**这里没有走任何捷径**——没有为了让它匹配而放宽"命中恰好 1 次"那条断言。
- **资源与并发前置**（出稿前当场重取，脚本＝`.local-notes/qa-tip-1020-run/preflight-1020.ps1`）：空闲物理 **7,185 MB**／可见总量 32,124 MB；已用 commit **41,111／48,508 MB**；**没有任何进程私占超过 3 GB**（`processesOver3GB=0`）；`dotnet.exe` 在场 **8 个**、**按 `-notmatch 'nodemode:1'` 反向筛出的非驻留节点＝0 个** ⇒ 没有他人的 `dotnet test` 在同一棵树上并发；`python.exe` 在场 1 个（作者的 ComfyUI，**没有动它**——本册纪律：绝不动他人的进程）。这条前置的用处照旧：本机可用 commit 被压下去时 Core 会报 `disk I/O`／`out of memory`，那是**资源形状**，读数要作废重跑，而不是改阈值。
- **临时产物全部落在载体之外**（`.local-notes/qa-tip-1020-run/`＝七个全量 TRX 目录＋十五个突变 TRX 目录＋两份构建日志＋七份 `fixed-eval-coverage.json`＋驱动脚本＋`mutations.jsonl` 逐条日志），**一律不删**（`.gitignore` 只忽略 `*.log` 与 `TestResults/`，落进载体就会长出 `??`，"干净"就得写脚注——写了脚注的读数不算成立）。

---

## 3. 独立证伪（18 处：17 处成立 ＋ 1 处全绿；全部只落在 QA 自己的载体，逐处还原）

### 3.1 记忆管理 `e2b521a`（`RT-005-MANAGE`，第 2 层 2-C）

| # | 拆的是什么（承重墙） | 落点 | 结果 |
|---|---|---|---|
| M1 | "确认保留"不抬作用域 `epoch` ⇒ 缓存里的上下文永不失效，她抬回同一条却引用不到 | `SqliteMemoryRepository.ConfirmKeepAsync`（删掉那句 `UPDATE memory_scopes SET epoch=epoch+1`） | **1 红**＝`MemoryDeletionSeparationTests.ConfirmKeep_advances_the_epoch_so_cached_context_invalidates` |
| M2 | "同指纹已被活跃行占用"的前置检查被架空 ⇒ 该回 `409 memory_fingerprint_taken` 的变成部分唯一索引 2067 的 500 | 同文件（把前置检查的 `status='active'` 改成永不成立的 `'never-matches'`） | **1 红**＝`ConfirmKeep_reports_fingerprint_taken_when_an_active_twin_already_owns_the_slot` |
| M3 | 会话级联扩大到"还有别的会话作证"的记忆 ⇒ 跨会话证据被抹 | 同文件（`item.RemainingSupport == 0` 放宽成 `>= 0`） | **2 红**＝Api `Deleting_a_conversation_over_http_forgets_only_the_facts_it_exclusively_taught` ＋ Core `N8_Cascading_a_conversation_delete_forgets_only_the_facts_it_alone_taught`（同一条规矩两侧各有抓手，不是回声） |
| M4 | 界面删除不再先问一句（直接永久删） | `PryMessengerMemoryWindow.cs`（删掉 `if (result != ConfirmDeleteId) return;`） | **1 红**＝App `MemoryManagementSurfaceTests.The_delete_button_confirms_before_the_irreversible_call` |
| M16 | 管理读口重新只取 `status='active'` ⇒ "依据已被你删掉的"那一组**又看不见**（这正是工单缺的第 ① 件） | `SqliteMemoryRepository.ListAsync`（给读列表那条 SQL 补回 active 过滤） | **15 红／973 绿**，红的全部点名孤儿可见性与确认保留：含 `ConfirmKeep_and_the_management_read_path_reach_instance_scoped_orphans`（那条 N11 证据）、`N1_Deleting_the_only_evidence_message_orphans_the_memory_and_writes_no_tombstone`、`N2_Restating_an_orphaned_fact_is_learnable_again…`、`N7_…`、`N8_…`、Api `ConfirmKeep_promotes_an_orphan_over_http_with_its_own_error_semantics` ⇒ **这一面是本轮守得最厚的一面** |
| M17 | **反面探针**：分组本体掏空（`&& false`）而形状串留在原地 | `PryMessengerMemoryWindow.cs:98` | **0 红／988 绿** ⇒ 唯一没报警的一拆，见 §5 |

### 3.2 换后端自愈 `2711c2f`（第 0 层 `0-6`）

| # | 拆的是什么 | 落点 | 结果 |
|---|---|---|---|
| M8 | 换了入口之后不再通知界面重订事件流 | `PryBackendClient.cs`（删掉 `SessionRestored?.Invoke();`） | **5 红**＝Api 真进程 `BackendSwapRecoveryProcessTests.The_same_client_answers_again_after_the_backend_is_replaced_on_its_port` ＋ Client 四条（`The_event_plane_redials_before_it_gives_up`、`A_stale_token_re_reads_the_descriptor_and_re_issues_that_request`、`A_rejection_after_the_swap_stops_at_one_reissue`、`Requests_issued_before_the_swap_do_not_turn_into_a_false_ban`） |
| M9 | 令牌没变也当成换成功（真的拒绝被演成"恢复好了"） | 同文件（`if (fresh is null \|\| fresh.Token == session.Token) return false;` 削成只判 null） | **1 红**＝`An_unchanged_token_stays_a_ban_with_no_reissue` |

### 3.3 插件／能力第一期 `c0b90c1`（`RT-007`）

| # | 拆的是什么 | 落点 | 结果 |
|---|---|---|---|
| M5 | 出厂空白名单被当成"全放行" | `DesktopCapabilities.cs`（`if (!_allowed.Contains(full))` 前面加 `_allowed.Count > 0 &&`） | **1 红**＝`DesktopCapabilityGuardTests.An_empty_allow_list_refuses_even_a_program_that_really_exists` |
| M6 | `open_url` 放过 `file` 协议 | 同文件（`AllowedSchemes` 加 `"file"`） | **1 红**＝`Anything_that_is_not_a_web_address_never_reaches_the_launcher`（理论用例） |

### 3.4 安全与打包两把 `2a76470`（`30a7a5a` 本地档回环 ＋ `8f7dcb1` 发行形状门）

| # | 拆的是什么 | 落点 | 结果 |
|---|---|---|---|
| M7 | "本地模型"那一档不再要求回环 ⇒ 可以填远程 http、她的话明文送出机器 | `ConfigurationApplicationService.cs:753`（把档位名改成永不匹配的字面量） | **1 红**＝`LocalModelLoopbackBoundaryTests.A_local_llama_profile_refuses_a_remote_endpoint` |
| M13 | **把"工作区必须干净"那行守卫挪进 `-not $SkipValidation` 分支**（字样还在、等于没有）——`QA-TIP1016-01` 修法里点名的那一种 | `scripts/package-release.ps1:152` 起那一整块 | **2 红**＝`ReleaseAuditabilityShapeTests.The_working_tree_guard_runs_before_any_skip_switch` ＋ `The_scanner_sees_the_hollowing_it_claims_to_ban`（那道正向探针自己认得这个形状 ⇒ 不是假绿门） |
| M14 | manifest 里的来源提交号写死 | 同文件（`rev-parse --short=12 HEAD` → 固定 12 个 0） | **2 红**＝`The_release_manifest_still_takes_its_commit_from_git` ＋ 那条扫描器自证门 |
| M15 | 缺运行库从"先报错"改成"静默跳过" ⇒ 发出去一个跑不起来的包还报成功 | 同文件（`Copy-InferenceRuntime` 的 `throw` → `return`） | **2 红**＝`A_missing_inference_runtime_is_refused_before_anything_is_copied` ＋ 那条扫描器自证门 |

### 3.5 测试工装两把 `ede6a6d`（有界等句柄）＋ `f0827c2`（第三条池键）

| # | 拆的是什么 | 落点 | 结果 |
|---|---|---|---|
| M10 | 工装漏清第三条连接池键（回到 `QA-TIP1019-05` 登记过的那个旧形状） | `tests/Pry.Api.Tests/SqlitePoolCleanup.cs`（删掉裸 `Data Source=` 那一档清池） | **3 红**＝`SqlitePoolKeySymmetryTests.A_bare_data_source_connection_is_reclaimed_by_the_helper`、`…Both_test_assemblies_clear_the_same_set_of_pool_shapes`、`MemoryManagementEndpointTests.ConfirmKeep_promotes_an_orphan_over_http_with_its_own_error_semantics` |
| M11 | 等窗口用尽之后把删除失败**吞掉**（不报错，直接返回） | 同文件（`when (attempt < attempts && …)` 改成 `when (…)` ＋ `if (attempt >= attempts) return;`） | **1 红**＝`TestFixtureTeardownLockTests.A_handle_that_never_releases_still_fails_the_teardown_instead_of_hiding_it` |
| M12 | 把"偷偷关掉连接池"的规避放回 fixture（用例绕开工装＝工装缺一格） | `tests/Pry.Api.Tests/MemoryManagementEndpointTests.cs`（连接串补 `Pooling = false`） | **1 红**＝`The_fixture_that_retired_the_pool_dodge_shuts_down_through_the_same_door` ⇒ **`QA-TIP1019-05` 的"补上之后重拆才报警"这一格由 QA 独立重拆成立** |

### 3.6 legacy 作用域限定（不属于本轮四把，但工单里挂着 N11 那句，所以一并拆）

| # | 拆的是什么 | 落点 | 结果 |
|---|---|---|---|
| M18 | legacy 删除不再限定 `scope_key='character:'\|\|$c`（那条硬限定到底有没有人守） | `MemoryDatabase.cs:607` | **1 红**＝`ApplicationServiceTests.Character_and_client_preferences_are_owned_by_backend_without_path_leaks` ⇒ 不是无人守的常绿面 |

---

## 4. `QA-TIP1019-*`／`-1020-*` 在册账对账（本轮之后各格的时点）

- **`QA-TIP1019-05`**（那一刀自报"第一次拆掉没报警，补断言后重拆才报警"）⇒ **本轮换成 QA 独立重拆成立**（M12 恰 1 红）。账结清，无需再挂。
- **`QA-TIP1019-02`**（记忆检索地板 `0.5` 全仓零判据）⇒ **本轮没有触碰**（它不在产品行为里，也不在这四把的承重墙上），状态照原样挂着，Owner 仍是 Memory & Context。
- **`QA-TIP1019-03`**、**`-04`**、**`-06`** ⇒ 本轮没有新证据，原样不推。`-06`（测试工装总清）仍押在 R4 各刀落完之后。
- **`QA-TIP1020-01`**（那一页的 `**` 形状机械门，刻意未做）⇒ 原样挂着，本轮不新增证据；M13/M14/M15 用的那台"读脚本形状"的仪器是 C# 侧已有的门，不需要 markdown 依赖。
- **`QA-TIP1016-01`**（整条发行路径零判据）⇒ **本轮正式转为"已闭合"**：形状门四条（`ReleaseAuditabilityShapeTests`）不仅在册，而且 M13／M14／M15 三种掏空各红各的，含那道扫描器自证门。这条账从 10-16 挂到今天，结清。
- **`QA-TIP1016-02`**（形状门文字强于断言）⇒ 本轮没有新证据，原样挂着。

---

## 5. 新登记：`QA-TIP1020-02`（Low，Owner 建议＝Memory & Context 的界面半边；QA 只转达、不代改；**不卡任何结案**）

按 `docs/decision-request-protocol.md` 的四段写（这条不需要作者拍，但按同一口径写清楚，免得接刀的人再问一遍）：

- **当前行为**：界面"记忆"那一栏分两组摆出「她正在用的」与「依据已被你删掉的」。分组靠的是 `PryMessengerMemoryWindow.cs:98` 那句 `details.Where(x => x.Status == "orphaned")`。守它的只有 `MemoryManagementSurfaceTests` 的形状门——它断言这行字样**存在**（`Assert.Contains("Status == \"orphaned\"", source)`），并不断言这个条件**真的在起作用**。
- **用户可见后果**：**今天没有后果**——我跑过、也看着它绿，界面照常分两组。风险在下一刀：谁把判断条件掏空（加一个恒假的合取、把孤儿那一组改回从 legacy 列表取、或把这行字原样搬进注释），**988 条测试一条都不会响**，你会看到一个"两组都在、第二组永远是空"的界面，而且没有任何自动门告诉你。
- **项目影响**：这是 `QA-TIP1015-01` 那一族（**掏空本体而扫描门仍绿**）第一次出现在记忆管理这条 lane；该族的成因在 10-15 与 10-16 两份报告里已经写过——形状门能证明"字样在"，不能证明"字样有用"。`MemoryManagementSurfaceTests` 自己带了一道正向探针（`The_scanners_see_the_shapes_they_claim_to_see`），**但那只证明扫描器认得这个形状，不证明形状与行为等价**——这一点必须写清楚，否则下一个人会以为探针已经补上了这个洞。
- **推荐方案（含不许走的捷径）**：推荐＝**在既有那条形状门旁边补一条"有用性"判据**，两种形状任选：① 断言那个谓词的字面量里**不含恒假合取**，并把 `_orphaned.ItemsSource` 的赋值来源钉成 `orphans`（即"分组结果必须流到那个控件"）；② 或者直接给界面加一条最小的渲染断言（`Status="orphaned"` 的行必须落进第二组、`active` 的行必须落进第一组）。**不许走的三条捷径**：① **不许为了让它绿去改产品行为**（今天分两组是对的，这条是判据欠账不是缺陷）；② **不许把"注释里出现字样也算"当覆盖证据**（该门 docstring 已明写这一限制，引用它当"已守"是错的）；③ **不许用"再加一条 `Assert.Contains`"当修法**——同形第二次出现就是这条欠账本身的形状。

---

## 6. 事实登记（不是缺陷、不新开编号）：`instance:` 作用域那三处硬编码

三条当场可复核的事实，按上一轮"把原始文本读全"的纪律写成，不带判断：

1. `src/Pry.Core/Memory/MemoryDatabase.cs:566`（`ListMemoriesAsync`）、`:581`（`GetMemoryAsync`）、`:607`（`DeleteMemoryAsync`）三处仍写 `scope_key='character:'||$c`。
2. `src/` 内 `instance:` 生产者**零个**（`grep -rn "ForInstance\|instance:" src/` 命中 0 行；只有三个测试夹具用 `instance:alice-pry`）。⇒ **潜在面，不是当下事故**，这一点 `e2b521a` 的提交信息自己已经写了（逐字：`potential, not a live defect`）。
3. 新接口 `GET /api/v1/memories/details`（`MemoryApplicationService.cs:13` → `database.LongTerm.ListAsync`）**走的是作用域感知的仓储路径**，所以"看得见"与"点删除"今天走的是两条不同路径：前者 `details`、后者 `database.DeleteMemoryAsync`（`MemoryApplicationService.cs:91`，界面按钮 `PryMessengerMemoryWindow.cs:181`）。**M18 证明这条 legacy 限定有判据守着（恰 1 红）**；**M16 反过来证明仓储那条读口有 15 条判据守着**。⇒ 等真的有 `instance:` 生产者那天，这一对不对称会自己变红（M16 那 15 条里没有一条会替你看着删除侧，但 M18 那条会），到那天再按缺陷登记，本轮不提前。

---

## 7. 之后还欠什么（都不是代码）

1. **作者手上的三件**：真机托盘那一次「退出…」并确认（真机路径 C 至今**零份可归因证据**，脚本点不到托盘，也不许为此加一个能被脚本触发的退出后门）；tag／push／打包的明令；各阶段结案词与状态词（永不由 Agent 写）。
2. **别人名下的执行**：`RT-006`（行为经验从点击取证）、`PLAT-003`（备份与恢复，前置早在 `e2b521a` 达成，缺的是有人认领）、`QA-TIP1019-02`、`-03`、`-06`、`QA-TIP1020-01`、本轮新加的 `QA-TIP1020-02`。
3. **本轮刻意没做的**：不 tag／不 push／不打包；不动 §5.0 意义上的任何状态词与结案词；不改任何产品代码、判据文本或阈值；`docs/character-learning-safety.patch`（他人未跟踪草稿）没碰；`qa-tip-1020-run/` 里所有脚本、TRX、日志与七份 coverage json **一律不删**。

## 8. 下一步归属

- **QA（本角色）**：下一格等 R4 两张单（`RT-006`／`PLAT-003`）落地后从当时线尾再滚一次；`QA-TIP1019-06` 启动前不动测试工装。
- **守门人（本角色）**：台账 §4.2 与 §9 回填、作者那一页四行更新、`main` 跟线尾（本轮线尾未动，`main..HEAD`＝0）；不 tag／不 push。
- **Memory & Context**：`QA-TIP1020-02`（不卡结案）；`QA-TIP1019-02` 照旧。
- **作者**：§7 第 1 条那三件。
