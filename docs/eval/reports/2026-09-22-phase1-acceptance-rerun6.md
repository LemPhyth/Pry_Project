# Phase 1 验收判定 · 第六轮

Run ID: `eval-QA-P1ACC-0922`　日期: 2026-09-22 00:40–01:25 本地
前序: `0921`（02:41，456/3/2）→ `0921B`（14:58，535/0/2）→ `0921C`（15:56，546/0/2；16:06 自查更正）→ `0921D`（16:58，551/0/2；含 PLT-003 降级更正）→ `0921E`（22:02，613/0/2）→ 本轮
候选: 分支 `codex/ui-main-window-redesign`，HEAD **`f1c5118`**（`git log main..HEAD` 仍是 2 格许可/素材提交，**Phase 1 代码一条都没进提交**）
工作区: 仍 dirty——`git status --porcelain` **183 条（51 已修改 + 132 未跟踪条目）**，上轮 181。`docs/eval/` 与 `scripts/measure-fixed-eval.ps1` **受跟踪数仍为 0**
环境: Windows 10.0.26200 x64 / .NET SDK 10.0.200 / `net10.0` / Release；跑门禁前 `Get-Process dotnet` 只有 8 个 `MSBuild.dll /nodemode:1` 复用节点（全是我自己上一轮留下的，无 `testhost`），即**无同级构建/测试在抢 CPU**——这一点本轮必须记，因为新缺陷的成因就是并发负载

**结论：Phase 1 仍不通过。** 与第五轮相比阻塞面**变宽而不是收窄**，但宽度可控：上一轮剩下的三条判定（`QA-P5-01`、`QA-P5-02`、`QA-P2-04③`）本轮**全部结案**，`PLT-002/003/005/006/007/011` 无回归；新出现的是一条**门禁转红**（`QA-P6-01`，High），它使 `REC-005` 由 Pass 降为 **Fail**——这是六轮里第一次有已判 Pass 的永久场景因复跑而转红。旧的唯一结构性阻塞（统一可审查提交）第五、六轮之间没有动作。

## Regression Results

§7 五条命令本轮全部由我本人重跑（不复用他人数字），并在 01:17 之后**第二次**跑满解决方案以测新缺陷的复现率。TRX：`TestResults/phase1-acc-0922a/`（首跑，4 份）与 `TestResults/phase1-acc-0922c/`（复跑）。

| 命令 | 结果 |
|---|---|
| `dotnet build Pry.slnx -c Release` | **0 警告 0 错误**（两轮一致；`TestResults/round6-rebuild.log`） |
| `dotnet test Pry.slnx -c Release --no-build --logger trx --blame-hang-timeout 120s` | 首跑 **627 通过 / 1 失败 / 2 跳过（总 630）**；复跑**同一例再红**。Client 80、Api 126、App 180、Core 241+1fail+2skip |
| `scripts/measure-fixed-eval.ps1 -ResultsDirectory TestResults/phase1-acc-0922a` | `Catalog valid: 68 permanent scenarios.` + `Passed=627; Failed=1; Other=2.`；**退出码 1**（有失败断言即非零，脚本按设计不放水） |
| `scripts/verify-repository.ps1` | 通过 **214** 个受跟踪文件（口径与第五轮一致，仍不覆盖 132 个未跟踪条目） |
| `git diff --check` | 无输出 |

跳过两条仍是 `LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key` 与 `CoreTests.Configured_sense_voice_model_can_run_offline`（显式环境门控，按约定不计 Pass）。相对第五轮的 +15 条来源逐一对得上：**App 167→180（+13 ＝ `MessengerEventStreamControllerTests` 13 例，`[Fact]` 计数实测 13）**、**Client 78→80（+2 ＝ 整请求预算到期与订阅起头有界两条真判据）**、Core 244 与 Api 126 不变。**没有任何一条断言被删除或放宽来换取绿**：`EventStreamReconnectBanTests` 原 5 例（1 `Fact` + 2 `Theory`）仍在且全绿，只是被重新定位（见 `QA-P5-01` 结案段）。

## 第五轮 Release Blockers 逐条复核

| 第 5 轮条目 | 本轮状态 | 判据 |
|---|---|---|
| 1 统一可审查提交 | **仍未做**（第六次登记） | HEAD `f1c5118` 未动；183 条未提交；`docs/eval/`、测量脚本受跟踪数为 0。`docs/phase1-platform-baseline.md` §6 已回填到 613 一行，回填的仍是**工作区快照**而非某个提交 |
| 2 `QA-P5-02` 编号对齐 | **已闭合** | 迁移文档 §8 尾表已按永久目录改键，并把对齐约定写进该表抬头（"`ID` 与 `scenario` 文本一律以目录为准，跨文档引用不得只看 ID"）。旧编号在该节不再出现 |
| 3 `QA-P5-01` 接线判据 | **已闭合** | 新增 `src/Pry.App/Services/MessengerEventStreamController.cs` + 13 例，其中 2 例跑真 `RunAsync`；`PryMessengerWindow.axaml.cs:454-461` 只剩构造 Runner、`Attach`、`RunAsync` 三行，五个界面动作以 `IMessengerStreamSurface` 显式实现、体内只有 `Dispatcher.UIThread` 投递。实施方另给出逐突变证伪表（删 `Faulted` 那行 ⇒ 3 红；删 `EventReceived` ⇒ 2 红；删 `Resumed`/`StateChanged` ⇒ 各 1 红），我实读了 `Attach` 与四个 handler 的门控位置，形状与判据相符 |
| 4 `QA-P2-04③` 预算到期分支 | **已闭合 → `QA-P2-04` 全项结清** | `PryBackendClient.cs:289` 那条 `TimeoutException("…没有完成这一请求")` 分支此前零用例；现由 `BackendRequestBudgetTests.An_exhausted_whole_request_budget_reports_a_timeout_instead_of_a_cancellation` 判到（注入 150ms `longRequestBudget`、`StallingHandler` 永不响应、断言异常类型与文案，且 `HttpClient.Timeout` 被同时抬到 `RequestBudgetCeilingMs` 以证明"被判到点的是逐请求那一档，不是兜底"） |
| 5 `PLT-010` / `PLT-012` | 保持 Blocked / NotRun | 不代为放宽；两者仍阻塞"PLAT-005 已完成"这个标记本身 |
| 6 真机端到端 | **仍未做** | 新增的控制器用例仍是"交给 surface 的动作"级，不进 Avalonia 生命周期 |

## Coverage 变化

| 场景 | 第五轮 | 本轮 | 说明 |
|---|---|---|---|
| REC-005 | Pass | **Fail** | `HardwareProbeTimeoutTests.Killing_on_timeout_takes_the_whole_tree_not_just_the_shell` 两轮解决方案级复跑连红 2/2（`QA-P6-01`）。见下节 |
| PLT-002/003/005/006/007/008/009/011 | Pass | Pass | 映射断言本轮逐条解析为 `Passed`，无回归 |
| PLT-013（**本轮新建**） | — | Pass（工程证据） | 房间代次门控 3 例；`automatedEvidence: PartialPass` |
| PLT-001/004 | Pass | Pass | 不变 |
| PLT-010 | Blocked | Blocked | 属 PLAT-007，§5.0 冻结中 |
| PLT-012 | NotRun | NotRun | 残留子进程只在环境门控真实用例里，不以相邻绿灯代填 |
| PER-005 / REC-001 / REC-002~004 | Pass / PartialPass | 不变 | 探测有界三条、schema 单一来源、七类码无回归 |
| Timing/Behavior/Expression/Memory/Relationship/Capability | NotRun | NotRun | 接线与真实模型仍未到位；工程门禁不产生这些类的 Pass |

## `QA-P6-01`：机制、复现率与不建议的修法

这是本轮唯一的实跑失败，也是六轮里第一次"已判 Pass 的场景被自己的复跑推翻"，所以逐条留证。

**观察到的事实**（同一台机器、同一份 `f1c5118` 工作区、Release）：

| 跑法 | 次数 | 该例结果 |
|---|---|---|
| 解决方案级并行（4 个测试程序集同时跑） | 第五轮 1 次 | 绿（当时 Core 242 通过） |
| 解决方案级并行 | 本轮 2 次 | **红 2/2**，同为 `HardwareProbeTimeoutTests.cs:64`，耗时 9 s |
| 仅 `Pry.Core.Tests` 全量 | 2 次 | 绿 |
| 单跑该例 | 3 次 | 绿 |
| 跑前 `Get-Process dotnet` | — | 只有我自己的 MSBuild 复用节点，**无同级 testhost**，即红不是别人在同目录构建造成 |

**归因上要替实施侧说清一句**：他们在 23:4x 自跑的 **628 / 0 / 2（总 630：Client 80、Api 126、App 180、Core 242+2skip）** 与本轮 **627 / 1 / 2（同 630，Core 241+1fail+2skip）** 是**同一份工作区、同一组程序集计数**，逐位差集只有这一例。所以这不是"他们跑的是另一份代码"，也不是谁改坏了断言——同一份断言在冷机器上过、在四程序集并行时不过。合起来的复现率：**解决方案级 4 跑 2 红**（含实施侧那次绿与第五轮那次绿），隔离跑 5 次全绿。

**机制**（读 `tests/Pry.Core.Tests/HardwareProbeTimeoutTests.cs:52-147` 得，不是猜）：该例的前提是"假探测期间孙进程已经在往 `beat.log` 追加"。夹具用 `fake-llama-server.cmd` 以 `start "" /b` 拉起 `beat.cmd` 的 `for /l … echo beat>>` 循环，而探测预算 `ProbeBudget = 250ms`——孙进程的第一行要等"外层 cmd 冷启动 → 解析 → `start` 再冷启一个 cmd → 首次追加"完成才出现。四个测试程序集并行时这条链子的延迟超过 250 ms，产品侧在孙进程写下第一行之前就把整棵树杀掉了，`WaitForGrowthAsync`（80 × 100ms）拿不到任何增长 ⇒ `size == 0` ⇒ 第 64 行的**自诊断前置断言**转红，文案正是"本用例没有判据可言"。夹具注释自己写着"cmd 起 cmd 是毫秒级"——那是一次冷启动侥幸，不是保证。

**这条坑不是第一次出现**：夹具自己的注释（`HardwareProbeTimeoutTests.cs:127-129`）写着"孙进程不用 powershell：冷启动要几秒，会把'等到第一行'的窗口变成不稳定来源"——即实现方此前已因同一个不稳定源把 PowerShell 换成 cmd。换掉的是**量级**（几秒 → 毫秒），没换**结构**：前置条件依旧挂在"某个时间窗口内会不会出现第一行"上，而不是"第一行出现过"这一事件上。250 ms 预算下，毫秒级也够被并行负载吃掉。所以修法要落在结构上，而不是继续挑一个更小的进程启动器。

**为什么这不只是"测试不稳"**：全树终止（`Kill(entireProcessTree: true)`）这条属性在该目录里**只有这一例**判；其余三例判的是切断、不传染、取消仍是取消。所以门禁红的同时，`REC-005` 的 `forbiddenOutcome`（"只杀外壳留下孤儿孙进程"）在这一跑**没有任何判据**。判 Fail 而不是 Inconclusive 的理由：场景的 `automatedEvidence` 就是这四例，其中一条实跑失败；我不以"另外三例相邻绿"代填。

**建议方向（归 PLAT-001 Owner，QA 不代做）**：把前置条件改成事件驱动，而不是把时间赌大——例如让 `beat.cmd` 在追加之前先写一个 started 标记、用例在**探测仍在跑的窗口内**轮询到标记后再让预算到期；或单纯给这一例注入更宽的 `probeTimeout`（2–5 s 仍然证明"到期会切断"，而"树被收干净"的判据不再和进程创建延迟抢时间）。两条都保留判据本体。

**明确不建议**：删掉第 64 行那句前置断言、或把该例改成 skip/重试。前者会把"没有判据"变成**空过**（文件从未被写 ⇒ 停止增长 ⇒ 尾巴那条 `Assert.Equal(size, BeatSize(...))`  trivially 成立），后者把一次真实的环境耦合缺陷从门禁里抹掉。这个方向我会在后续每轮复跑里盯。

## Critical / High / Medium / Low Issues

本轮共四条缺陷加一条同步提示：**Critical 0、High 1（`QA-P6-01`，见上节）、Medium 2、Low 1**。第五轮的三条（`QA-P5-01/02/03`）与存量的 `QA-P2-04` 在本轮按上述口径结案或并入。

- **QA-P6-02（Medium，口径分歧 → 交作者裁定，不改判）**：实施侧在 `docs/protocol-versioning-and-websocket-migration.md` §8.1 尾加了一条"QA 侧硬提醒"，据 `docs/eval/README.md` 的"SSE 解析不证明断线重连"要求 `PLT-005`（游标跨重启）与 `PLT-007`（半开检出）**不得因测试变绿记 Pass，必须落到真实进程重启与真机断链**。我按自己上一轮定下的对齐约定逐条读了永久目录的 `scenario` 文本再判：`PLT-007` 的 `initialState` 写的就是"服务端建立事件流后既不发也不关"，`Silent_stream_is_declared_half_open_instead_of_blocking_forever` 恰是这一形状的**直接**观测（零字节、无 FIN/RST），不是代理；`PLT-005` 的 `input` 写"Core 停止后重启"，`Cursor_survives_a_process_restart_…` 确实 `first.Stop()` 后以同一数据目录起第二个真 Kestrel 宿主并用原 `after` 重放。两条**按目录文本已满足**，故本轮保持 Pass。他们说得对的部分是另一件事：**没有任何一条用例把"真 OS 进程边界 + 真客户端 + 真界面"一次跑完**——那属第五轮就挂着、至今未做的 Release Blocker 第 6 条（真机端到端），不属这两条场景。若作者要把 OS 级证据定为判据，那是**语义修订**：得改 `initialState`/`input` 文本并升 `suiteVersion`，我按修订后的文本重判，而不是在现文本下按更严的口径翻案（那会让目录文本失去权威，正是 `QA-P5-02` 的成因）。
- **QA-P6-03（Medium，UI-027 / Core Platform）**：`QA-P5-01` 结案后剩下的一半，加上第五轮的 `QA-P5-03`，本轮合为一条，**都需要作者裁定而非补码**：① `App.InitializeDesktopAsync` 的"先发现后内嵌"顺序（`App.axaml.cs:34-47`）仍无自动化判据——实施方自己在 §8 第 3 条更正了上一版"与循环接线同源"的说法，那个同源今天确实已断；② `StartEventStream` 的换房取消（`:446-452` 先 `Cancel()` 再 `Dispose()` 旧的 CTS）与 surface 的 `InvokeAsync`/`Post` 投递差（前两个动作 `await InvokeAsync`、后三个 `Post` 不等）也仍无判据，仓库没有 headless Avalonia 设施；③ `MainWindow`（1608 行）+ `RemoteConversationTurnManager` + 那 5 例 + `ThemeAppearanceEditor` + 只会被写成 `false` 的 `LayoutChanged` 死标志，实施侧已给出**次序不能倒**的删除方案（先判据后宿主，§8 第 2 条末尾），并明写"删与不删归作者"。我的意见与前几轮一致：**同刀**处置，且 `RemoteConversationTurnManager.cs:87` 那句"刻意不挂 `Resumed`"的 caveat 必须在删除时一起消失，否则 `PLT-006` 的补取不变量会在某次重新接线后静默失效。
- **QA-P6-04（Low，Eval 目录自身）**：新增的"订阅起头有界"判据（`PryBackendClient.cs:226` 那条 `TimeoutException("…没有接受事件流订阅")` 分支，由 `An_event_subscription_that_never_gets_headers_fails_on_the_local_budget` 守）今天**没有任何永久 ID 承接**——它不属于 `PLT-007`（那条讲的是建立之后的静默），也不属于 `PLT-008`（取消/关流两条路径）。同理未映射的还有 `Transcription_gets_its_own_budget_without_loosening_the_shared_one`（"兜底必须宽于做主那一档"这条不变量）。我没有松挂：按 README 约定映射是证据不是计数。下一轮要么把 `PLT-007` 的 `initialState` 扩含"起头无响应头"（语义修订 ⇒ 升 `suiteVersion`），要么给这条判据开一条新永久 ID，二者都归作者/我裁，不靠"反正相邻绿"过关。
- **文档同步提示（Low，不阻塞）**：迁移文档 §8.1 抬头写的是 `suiteVersion 1.1.1`、"`PLT-*` 共 12 条"。本轮目录已到 **1.1.2 / `PLT-*` 13 条**，那两行按对齐约定属派生视图，请顺手刷新；我不改别人的文档。

## Runtime Findings

本轮工作区里的可观察行为变化（相对第五轮，全部读代码 + 用例核实）：① 活窗口的断线终止文案现在**一定**出自呈现表——`StateChanged` 也进了房间代次门控，旧流那句"正在重连…"不再可能盖到新房间状态栏；② Runner 的四个回调（补历史、刷列表、改发送态、改状态栏、改轮次相位共五类动作）在 `Attach` 一处绑完，`Attaching_wires_every_callback_the_runner_can_call` 使"漏绑一条"从"没人会注意"变成用例红；③ `PryProtocol.RequestBudgetCeilingMs = TranscriptionRequestTimeoutMs + 15_000` 把 4 分钟语音预算与 HttpClient 级兜底的次序钉死，第五轮"宽预算被 32 秒顶掉"那一类缺陷有了源码级防线；④ 未知事件类型 `provider.future.event` 在界面侧整条丢弃且不报错，`PLT-011` 的前向兼容判据从"解析层"延伸到"呈现层"。

未变的边界：真机端到端、`App.InitializeDesktopAsync` 启动顺序、`PLT-010` 大媒体引用、`PLT-012` 残留子进程。

## User Feedback Converted to Tests

0；未收集。EVAL-002 仍只有模板与 0 样本，不以模板代替真实反馈。

## Release Blockers（Phase 1 结案前必需，顺序即建议顺序）

1. **`QA-P6-01` 红门禁**（新增，最高优先）：`REC-005` 今天不绿，`measure-fixed-eval.ps1` 退出码 1。修法见上节，方向为"前置条件事件化或该例预算放宽"，**不得以删判据/转 skip 结案**。Owner：PLAT-001。结案判据我按"解决方案级并行连续 ≥2 次全绿 + 该例仍保留 `size > 0` 前置断言"复跑认定。
2. **统一可审查提交**（唯一结构性阻塞，第六次登记）：把 Phase 1 代码批次连同 `docs/eval/`、`scripts/measure-fixed-eval.ps1` 形成提交，**从该提交**重跑 §7 五条命令并回填 `docs/phase1-platform-baseline.md` §6。本轮 627/1/2、上轮 613/0/2 各自只描述当分钟快照——尤其本轮证明：同一工作区内"613 全绿"与"627 带一红"可以并存，只有固定提交能让数字可归属。
3. **`QA-P6-03` 三项一次裁定**：启动顺序与投递面的判据归属（补设施 / 明确人工走查结案）、`MainWindow` 一族的删除次序（实施侧方案已在工作区，"同刀"）。
4. **`QA-P6-02` 口径确认**：`PLT-005`/`PLT-007` 的判据是否要提到 OS 级证据。要 ⇒ 改 `scenario` 文本 + 升 `suiteVersion`；不要 ⇒ 把真机部分明确归入第 6 条端到端项，两文档口径统一。
5. `PLT-010`、`PLT-012` 如实保持 Blocked / NotRun。它们不阻塞其余结案，但**阻塞"PLAT-005 已完成"这个标记本身**。
6. **真机端到端**（Client 启动 → 发现或内嵌 → 聊天 → 断线重连 → 重启后历史仍在）仍只到"内嵌真实后端的自动化 + 一个真子进程握手"层。

## 本轮登记的证据映射（目录 67 → **68 条**、`suiteVersion` **1.1.1 → 1.1.2**）

版本升级**只因新增一条永久场景**（`PLT-013`），既有各条的 Scenario / Input / Initial State / Expected Decision / Constraints / Forbidden Outcome 一字未改；其余变更全为 `automatedTests` 追加，按 README 约定不升版本。校验：`Catalog valid: 68 permanent scenarios`，本轮受影响项 `missingTests` 全为空、全部映射逐条解析为 `Passed`（`REC-005` 除外，其 `automatedEvidence: Failed`）。

| 场景 | 映射数 | 本轮新增 |
|---|---|---|
| **PLT-013（新建，High/Protocol）** | 0 → 3 | `A_stream_from_the_room_the_user_left_touches_nothing`、`Attaching_wires_every_callback_the_runner_can_call`、`A_new_message_refetches_this_rooms_history_then_the_room_list` |
| PLT-002 | 13 → 15 | 控制器跑真 Runner 的两例：`A_ban_reaches_the_surface_through_the_real_runner_without_one_extra_retry`、`A_terminated_stream_says_why_instead_of_hanging_on_reconnecting` |
| PLT-006 | 7 → 8 | `Recovery_refetches_history_before_the_next_event`（客户端"收到宣告先补领域历史"的界面侧义务） |
| PLT-007 | 4 → 5 | `Idle_subscription_still_fits_inside_the_request_budget`（起头预算宽于心跳周期 ⇒ 安静会话不被误判断连） |
| PLT-011 | 1 → 2 | `An_unrecognized_event_changes_nothing_and_breaks_nothing`（未知 type 在呈现层整条丢弃） |
| REC-001 | 6 → 7 | `An_exhausted_whole_request_budget_reports_a_timeout_instead_of_a_cancellation` |

一处刻意**没有**做的映射见 `QA-P6-04`：`An_event_subscription_that_never_gets_headers_fails_on_the_local_budget` 我一开始想挂 `PLT-007`，按目录文本对齐后撤掉——那条场景写的是"建立事件流之后既不发也不关"，而它判的是"连响应头都没有"，机制与初态都不同。挂上去会让下一轮误以为 `PLT-007` 已覆盖起头路径。

## Deferred Eval

沿用第五轮：`PLT-012` 残留子进程（待 `PRY_TEST_LLAMA_*` 环境门控）、`PLT-010`（待 §5.0 解冻 PLAT-007）、Timing P50/P95 与 Behavior/Expression/Memory 全类（待接线 + 真实模型）、纵向内测 Day 1/2/7（0 样本）。本轮**新增**：`App.InitializeDesktopAsync` 的"先发现后内嵌"顺序与 `StartEventStream` 的换房取消/线程投递（`QA-P6-03`），以及 `QA-P6-04` 指出的"订阅起头有界"无永久 ID 承接项。**本轮移除**：`QA-P5-01`（接线已判）、`QA-P5-02`（编号已对齐）、`QA-P2-04③`（预算到期已判）。

## 更正与降级（2026-09-22 03:5x，作者裁定 Phase 1 结案标准；同日追加，不改写上文）

**上文"Release Blockers"一节的六条，从本段起不再是 Phase 1 的结案标准。** 作者（产品负责人）在本轮报告交付后裁定：

> Phase 1 的结案标准应该是"使用后能得到答复"：Client 启动后能通过 Core 调用模型，模型生成回复后 Core 能够保存并且返回给 Client，其他的先不管。主要是验证这个软件能跑，最小程度的能跑。

据此，本报告的判定与阻塞面重述如下。**判定本身（第六轮 = 不通过）不撤回**——那一分钟的工作区确实红了一条门禁、确实零提交；被撤回的是"这两件事构成 Phase 1 结案阻塞"。

- **`QA-P6-01`（红门禁 / `REC-005` Fail）已修复并由 QA 复判回 Pass，同时按作者裁定它本就不该阻塞 Phase 1 结案**。两件事分开写：
  - **修复与复判（03:51–03:55）**：Owner 采纳我给的**方向 A**（前置条件事件化）落在工作区——`tests/Pry.Core.Tests/HardwareProbeTimeoutTests.cs` 现在是三条时间轴分离的写法：`TreeSweepBudget = 5s`（探测预算）、`FirstBeatWindow = 3s`（等"孙进程开始写文件"这个**事件**，由新的 `StartedWritingAsync` 轮询 `BeatSize > 0`，取代原来跑在杀掉之后的 `WaitForGrowthAsync`），判据仍是杀掉后重新采样文件长度并确认不再增长（`:82`）。我点名的四种不可接受处置一条都没做：**前置断言原文仍在 `:77`**，只是把提示语里的"探测期间"改成"3 秒内"，等不到事件仍报"本用例没有判据可言"。我自己的独立取证（03:47 起新构建、无同级 `dotnet/testhost`）：Release 构建 **0 警告 / 0 错误**；解决方案级 `dotnet test --no-build` **连续两次 628 通过 / 0 失败 / 2 跳过（总 630：Client 80、App 180、Api 126、Core 242+2skip）**；两次跳过仍是第六轮那两条环境门控（`Configured_llama_server_requires_the_child_environment_key`、`Configured_sense_voice_model_can_run_offline`），**不是把红的那条改成 skip**；TRX 逐条核对 `HardwareProbeTimeoutTests` 四条在两次运行中全部 `Passed`，含 `Killing_on_timeout_takes_the_whole_tree_not_just_the_shell`。`measure-fixed-eval.ps1` 两个结果目录**退出码均为 0**（`Passed=628; Failed=0; Other=2`）。**结案判据（并行连续 ≥2 次全绿 + 前置断言仍在）两条都满足 ⇒ `REC-005` 由 Fail 复判为 Pass，`QA-P6-01` 结案**。
  - **口径上它已不再是阻塞项**：即便仍红，按作者本次裁定它也属"测试自身不稳"而非"产品跑不通"，不挡 Phase 1。**但这条降级不适用于红门禁一般情形**——门禁退出码为 1 时"全绿"这句话不成立，任何一轮都不许引用那一次的绿灯数字；这是第六轮第一次出现的实际情况，我按事实登记而不是按情绪。
  - **一处我自己的过时之处**：给 Owner 的信（`2026-09-22-qa-p6-01-probe-flake-handoff.md`）成文于 03:45，其 §2 引用的 `:64` 与 `WaitForGrowthAsync` 描述的是我 01:05 读到的快照，**在信写出的时候该文件已经被 Owner 改成上述结构**。信已在顶部加状态段更正，正文保留原行号作为"当时看到什么才那么判"的痕迹——这与第三轮那条"引用行号漂移即结论失效"的教训同源，只是这次漂移发生在写信的同一小时里。
- **`QA-P6-02` 结案**（按"不采用"结案）：OS 级证据不作为 Phase 1 口径，`PLT-005`/`PLT-007` 维持按目录 `scenario` 文本判 **Pass**，不升 `suiteVersion`；其真机部分归入下面的端到端走查。
- **上文第 2 条（统一可审查提交）与第 6 条（真机端到端）合并为新的唯一结案动作**，落成清单：[最小闭环真机走查](../minimal-loop-run-checklist.md)。四条全部必需——① 启动→发一句话→得到回复，② 关掉再开历史仍在，③ 断一次后端能在有界时间内给出可指认结论，④ 可指认的提交 + 从干净工作树复现第 1–3 条。**第 ④ 条同时兑现了"统一提交"这一条**：走查证据必须挂在一次 `git commit` 之后，因此本节第 2 条不是被取消而是被吸收。
- **转 `NotApplicable`（永久 ID 与历史判定全部保留，不删不改语义）**：Behavior / Expression / Memory / Relationship / Proactive / Capability 六类角色感受场景、Timing 的 P50/P95 分布、OS 级重启与断网证据、EVAL-002 纵向反馈。理由统一写"不在 Phase 1 最小闭环范围内"，并逐项写明恢复门槛；`PLT-010`（大媒体）与 `PLT-012`（残留子进程）仍如实留 Blocked / NotRun，不折进 NotApplicable。
- **我保留的一条不同意见**（写在清单里也写在这里）：作者举的对照是 v0.0.2——"给朋友用根本不好用，但在我机器上能跑起来"。以"我机器上能跑"为 bar，Phase 1 可以结案；以"别人能跑"为 bar，本次走查的第 ①② 条在同一台机器上重复 3 次仍属于最小证据，不能算多机验证。这份差异不阻塞结案，但会在后续阶段以缺陷形式回来。

**下一轮的形态**：走查跑完后以 `eval-QA-P1WALK-<MMDD>` 出第七轮报告，按清单四条逐项给证据（截图 + `sqlite3` 查询结果 + 提交 SHA），并据此重述 Phase 1 判定。在走查之前，本报告的"不通过"与上面这段"降级"同时成立——前者描述快照，后者描述口径。
