# Phase 1 验收判定 · 第五轮

Run ID: `eval-QA-P1ACC-0921E`　日期: 2026-09-21 22:02–22:20 本地
前序: `0921`（02:41，456/3/2）→ `0921B`（14:58，535/0/2）→ `0921C`（15:56，546/0/2；16:06 自查更正）→ `0921D`（16:58，551/0/2；含 PLT-003 降级更正）→ 本轮
候选: 分支 `codex/ui-main-window-redesign`，HEAD **`f1c5118`**（`git log main..HEAD` 仍是许可/素材两格，**Phase 1 代码一条都没进提交**）
工作区: 仍 dirty——`git status --porcelain` **181 条（51 已修改 + 130 未跟踪条目；文件级 160）**，上轮 173。`docs/eval/`（10 个文件）与 `scripts/measure-fixed-eval.ps1` **受跟踪数仍为 0**
环境: Windows 10.0.26200 x64 / .NET SDK 10.0.200 / `net10.0` / Release；跑门禁前 `Get-Process dotnet` 无输出，即无同级构建/测试在跑

**结论：Phase 1 仍不通过。** 本轮把上一轮剩下的判定全部复核完：`PLT-003` 由 Blocked **转 Pass**、`PLT-006` 由 Blocked **转 Pass**、`QA-P4-02` 与 `QA-P2-04①` 结案，`QA-P4-01` 的判据面也收敛了。阻塞面与第四轮同形——**只剩"统一可审查提交"一条结构性阻塞**——但本轮新增两条我自己发现的缺陷（`QA-P5-02` 跨文档 ID 撞号、`QA-P5-03` 不可达布局偏好），并把 `QA-P4-01` 收窄为 `QA-P5-01`。

## Regression Results

§7 五条命令本轮全部由我本人重跑（不复用他人数字）。TRX：`TestResults/phase1-acc-0921e/`。

| 命令 | 结果 |
|---|---|
| `dotnet build Pry.slnx -c Release` | **0 警告 0 错误** |
| `dotnet test Pry.slnx -c Release --no-build --logger trx --blame-hang-timeout 120s` | **613 通过 / 0 失败 / 2 跳过（总 615）**；Client 78、Api 126、App 167、Core 242+2skip |
| `scripts/measure-fixed-eval.ps1 -ResultsDirectory TestResults/phase1-acc-0921e` | `Catalog valid: 67 permanent scenarios.` + `Passed=613; Failed=0; Other=2.`；证据 `fixed-eval-coverage.json` |
| `scripts/verify-repository.ps1` | 通过 **214** 个受跟踪文件（口径与第四轮一致，仍不覆盖 160 个未跟踪文件） |
| `git diff --check` / `--cached --check` | 均无输出 |

跳过两条仍是 `LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key` 与 `CoreTests.Configured_sense_voice_model_can_run_offline`（显式环境门控，按约定不计 Pass）。相对第四轮的 +62 条集中在 Client（19→78）与 Api（123→126），即本轮落地的 Runner / 呈现表 / 发现读侧 / 源码门禁；**没有任何一条断言被删除或放宽来换取绿**：`tests/Pry.App.Tests/EventStreamReconnectBanTests.cs` 原 5 例仍在（本轮实跑通过），第四轮判 `PLT-002` 用的三条禁行断言逐条对得上。

## 第四轮 Release Blockers 逐条复核

| 第 4 轮条目 | 本轮状态 | 判据 |
|---|---|---|
| 1 统一可审查提交 | **仍未做** | HEAD `f1c5118` 未动；181 条未提交；`docs/eval/` 与测量脚本受跟踪数为 0。基线 §6 已回填到 613（第六行"第五轮"），但回填的是**工作区快照**而非某个提交 |
| 2 `PLT-003` 裁定 + 补齐 | **已闭合** | 作者裁定"1.0 需要独立进程客户端"（`docs/protocol-versioning-and-websocket-migration.md:134`）；`src/Pry.Client/RuntimeDiscovery.cs` 落地；界面层三处 `5078` 字面量已清（`grep` 全仓 `src/Pry.App` 无命中，只剩 `src/Pry.Api/Program.cs:21` 服务端默认值与测试夹具） |
| 3 `QA-P4-01` | **部分闭合，收窄为 `QA-P5-01`** | 判据收敛为 Runner 一处；真正运行的那条循环的**接线**仍无用例（见下） |
| 4 `QA-P2-04①③` | ① **结案**；③ **仍开** | ① `src/Pry.Client/PryErrorPresentation.cs` 逐档覆盖 PLAT-001 七类码 + 禁行三档（31 例）；③ `BackendRequestBudgetTests` 仍是 2 条形状断言 + 1 条取消路径，`PryBackendClient.cs:236` 的**预算到期**分支仍零用例 |
| 5 `PLT-006`/`PLT-010`/`PLT-012` | `PLT-006` **转 Pass**；`PLT-010`/`PLT-012` 保持 | 见 Coverage 变化 |
| 6 真机端到端 | **仍未做** | 本轮新增的 `RuntimeDiscoveryProcessTests` 起的是真子进程，但不进 Avalonia 生命周期；界面实操仍无人走过 |

## Coverage 变化

| 场景 | 第四轮 | 本轮 | 说明 |
|---|---|---|---|
| PLT-003 | Blocked | **Pass** | 两半都有判据，且真进程那条做过证伪跑 |
| PLT-006 | Blocked | **Pass** | 客户端义务落地：首订 `-1`、按最后投递序号续订、恢复只宣告一次并触发历史重取 |
| PLT-002 | Pass | Pass | 判据从"两处界面分支"收敛为"Runner 一处"，双向用例仍全绿 |
| PLT-001/004/005/007/008/009/011 | Pass | Pass | 映射断言本轮全绿，无回归 |
| PLT-010 | Blocked | Blocked | 属 PLAT-007，§5.0 冻结中（注意：迁移文档 §8 表把这一项写成 `PLT-006`，见 `QA-P5-02`） |
| PLT-012 | NotRun | NotRun | 残留 `llama-server` 只在环境门控真实用例里，不以相邻绿灯代填 |
| REC-005 / PER-005 / REC-001 | Pass / Pass / PartialPass | 不变 | 探测有界、schema 单一来源、七类码无回归 |
| Timing/Behavior/Expression/Memory/Relationship/Capability | NotRun | NotRun | 接线与真实模型仍未到位；工程门禁不产生这些类的 Pass |

**`PLT-003` 转 Pass 的依据**（不是采信台账文字，是读实现与用例）：`RuntimeDiscovery.TryRead` 对缺席、半截 JSON、非对象、空文件、端口 `<1 or >65535`、空令牌、`pid` 已退出、`pid == Environment.ProcessId` 一律返回 `null`——"不确定就不接"而不是猜一个入口；`TryConnectAsync` 先打免鉴权的 `/api/v1/info` 才交出 `HttpClient`，所以"进程还在但 HTTP 面已不再服务"也不接，且版本兼容**不在读侧判定**（避免与协商中间件形成两处事实源）。`tests/Pry.Api.Tests/RuntimeDiscoveryProcessTests` 起真 `dotnet exec Pry.Api.dll` 子进程，只凭临时目录里那份文件连上：`descriptor.Pid == child.Id`、端口由 Kestrel 自选且断言 `!= 5078`、受令牌门端点真返回数据、匿名请求仍 `401`。实现方记录的证伪跑（抽掉附加令牌那行 ⇒ 在受门端点处转红）方向正确——它判的确实是"令牌来自文件且真的过门"。

**`PLT-006` 转 Pass 的依据与一处如实保留**：`ConversationEventStreamRunner` 的 `LastSequence` 起于 `-1`、每次投递取 `Math.Max`，恢复时对 `Resumed` 只宣告一次且"首连不算"；`PryMessengerWindow.cs:481-487` 把它接成 `ReloadMessagesAsync + RefreshRoomsWithoutSwitchAsync`。一处与场景字面判据的偏差要记下：**Runner 并不读 `stream.resumed{reason:"buffer_expired"}` 这一帧的语义**，它在任何一次成功重订后都补取历史。这不构成缺陷（补取是超集，未识别 `type` 前向兼容由 `PLT-011` 守住），但"按宣告才重取"这一更强的形状没有实现，若将来某条流要求区分处理，`PLT-006` 的判据要重新复判。

**另一处更硬的保留**：本次 Pass 只覆盖**活路径**。`RemoteConversationTurnManager.cs:87` 明写"不挂 `Resumed`：本类的回复走追加渲染，补一次历史就是同一条画两遍"——即它的宿主那条循环**刻意不做历史重取**。今天这不违反任何判据（该宿主 `MainWindow` 无构造点，见 `QA-P5-01`），但一旦有人把 `MainWindow` 重新接线而不改这条注释，`PLT-006` 的"客户端收到宣告后以领域历史为准重取"在那条路径上就不成立。此点随 `QA-P5-01` 一并处置，不留给下一个人重新发现。

## 第四轮那条纪律的修正（不是自我更正，是加限定）

我上一轮写下的教训是"记 Pass 前先读实现方对该项的自评状态与'不能结案'字样"。本轮它反过来咬了我一次：我按 `PLT-006` 去读 `docs/protocol-versioning-and-websocket-migration.md` §8 尾表，那里写着 **`PLT-006` = Blocked（收敛点在 PLAT-007）**，与我的工作区事实冲突——差点据此把一项已落地的能力再压一轮。查下去的结论是**编号撞号**（见 `QA-P5-02`），不是该项未开工。修正后的纪律：**读对方自评之前，先确认对方用的是哪套 `PLT-*` 编号**；跨文档引用永久 ID 时，以 `docs/eval/scenarios.json` 的 `scenario` 文本对齐，不以 ID 对齐。

## Runtime Findings

事件流模型、心跳与半开检出、游标跨重启、电平类不重放——四项与前几轮一致全绿。本轮新增可观察行为三条：① 本机已有另一实例在跑后端时，第二个界面会**接上**它而不是再起一个宿主（`App.axaml.cs:47`，代价已如实登记：退出提示按是否拥有后端分支）；② 断线重连成功后先补取历史再收事件，断线期间落库的消息不再需要手动刷新；③ 连续失败到 8 次上限与"服务端宣告重试无用"现在共用同一个终止出口，界面不再留一句永远不再更新的"正在重连"。

未变的边界：`App.InitializeDesktopAsync` 这条真实启动顺序仍无自动化覆盖（需要 Avalonia 生命周期），第四轮的"应用真正跑的那条循环无用例"由全仓库级收窄到**接线级**——逻辑本体（Runner）现在在 `Pry.Client.Tests` 有 15 例，包括禁行三码零次退避、两方向必须订满 8 轮。

## Critical / High / Medium / Low Issues

- **QA-P5-01（Medium，UI-027 / Core Platform）** — 活路径的接线无判据。`PryMessengerWindow.ObserveEventsAsync`（`:457-500`）里 Runner 的四个回调怎么落到界面——`Resumed` 补取、`Faulted` 用 `PryErrorPresenter.Describe(error).Body` 覆盖状态栏、`revision != _roomRevision` 时整条丢弃——今天零用例；而 `EventStreamReconnectBanTests` 那 5 例驱动的 `RemoteConversationTurnManager` **只有 `MainWindow.axaml.cs:225` 一个构造点**，`MainWindow` 在全仓（含测试）**没有任何构造点**。后果具体化：把 `runner.Faulted` 那行删掉、或把 `Resumed` 接到旧房间，613 仍全绿。建议方向（不越权代做）：给活窗口补一条与 App 层同形的用例，或明确以"Runner + 呈现表已判 + 接线属人工走查"结案并写进 §8.1；同时定 `MainWindow`（1608 行）与 `ThemeAppearanceEditor` 的**删除/覆盖次序**——第四轮提的"别顺手删掉唯一钉住禁行行为的用例"这一风险，现在应反过来说：那 5 例钉的是不可达路径。
- **QA-P5-02（Medium，文档 / PLAT-005 台账）** — `PLT-*` 跨文档撞号。`docs/protocol-versioning-and-websocket-migration.md` §8 尾表用的还是**采纳前**的编号：它的 `PLT-004` = 游标跨重启、`PLT-005` = 半开检出、`PLT-006` = 大媒体引用、`PLT-007` = 未识别事件；而永久目录（我第二轮采纳时拆开/重排过）里 `PLT-004` = 鉴权默认拒绝、`PLT-005` = 游标跨重启、`PLT-006` = 裁剪后先重取历史、`PLT-010` = 大媒体。同一个 `PLT-006` 在两份文档里指向两件事，且**两件的负责人不同**（客户端义务 vs PLAT-007）。这不是风格问题：本轮它已经让我差点做出一次错误降级，而下一次会表现为"某人按单号做了另一件事，验收数字变绿但场景没被满足"。修法：该表每行补一句永久目录的 `scenario` 文本，或改用目录 ID。QA 侧我已在 `docs/eval/README.md` 写下对齐约定（见本文件"第四轮那条纪律的修正"一节）。
- **QA-P5-03（Low，UI-027）** — 不可达但仍在生效的布局偏好。`MainWindowLayoutModes.Card` 仍被服务端校验接受（`ConfigurationApplicationService.cs:533`）、仍被活设置窗逐次原样回写（`PryMessengerSettingsWindow.axaml.cs:233,243`），但 `App.CreateMainWindowAsync` 已按 `:76` 注释对两种取值一律开新工作台，而唯一能选"经典 · 卡片窗口"的控件（`ThemeAppearanceEditor.cs:25,99`）只在不可达的 `MainWindow` 里被实例化。另有一个只会被写成 `false` 的活标志 `PryMessengerSettingsWindow.cs:45 LayoutChanged`。可观察变化：老用户库里存的 `Card` 值今天静默失效，没有一句话说明。属清理项，不阻塞结案，但别在台账里写成"经典布局仍可选"。
- **QA-P2-04③（Medium，存量）** — `PryBackendClient.cs:236` 的整请求预算到期分支仍零用例；现有 3 例判的是预算形状与"取消不得退化成超时"。
- 无新增 Critical / High。第三、四轮的 High 缺陷（`QA-P2-01`、`QA-P4-01/02`）在本轮按上述口径结案或收窄。

## User Feedback Converted to Tests

0；未收集。EVAL-002 仍只有模板与 0 样本，不以模板代替真实反馈。

## Release Blockers（Phase 1 结案前必需，顺序即建议顺序）

1. **统一可审查提交**（唯一结构性阻塞，第五次登记）：把 Phase 1 代码批次连同 `docs/eval/`、`scripts/measure-fixed-eval.ps1` 形成提交，**从该提交**重跑 §7 五条命令并回填 `docs/phase1-platform-baseline.md` §6。本轮 613/0/2、上轮 551/0/2、再上轮 546/0/2 各自只描述当分钟快照。
2. **`QA-P5-02` 编号对齐**：改动只在文档，成本极低，但它是后续每一轮验收的前置安全条件——单号不对齐时，"按单号做事"本身就是错的事。
3. **`QA-P5-01` 或明确结案**：给活窗口的接线补一条用例，或在 §8.1 写死"接线属人工走查"并连带定 `MainWindow` 的删除次序。
4. **`QA-P2-04③`**：转写/整请求预算到期分支补用例后，`PLAT-001` 台账才可标"已完成"。
5. `PLT-010`、`PLT-012` 如实保持 Blocked / NotRun。它们不阻塞其余结案，但**阻塞"PLAT-005 已完成"这个标记本身**；是否带着结案属作者/守门人的 §5.0 裁量，QA 不代为放宽。
6. **真机端到端**（Client 启动 → 发现或内嵌 → 聊天 → 重启后历史仍在）仍只到"内嵌真实后端的自动化 + 一个真子进程握手"层，本轮新增的"第二实例接上第一实例后端"这条行为尤其需要人走一遍。

## 本轮登记的证据映射（目录仍 67 条、suiteVersion 仍 1.1.1）

只追加 `automatedTests` 证据，未改任何一条的 Scenario / Input / Initial State / Expected Decision / Constraints / Forbidden Outcome，因此不升 suiteVersion。校验：`Catalog valid: 67 permanent scenarios`，五条受影响项 `missingTests` 均为空、全部映射逐条解析为 `Passed`。

| 场景 | 映射数 | 本轮新增 |
|---|---|---|
| PLT-002 | 8 → 13 | Runner 的禁行/放行双向 2 条（`A_banned_failure_stops_before_the_first_backoff`、`An_undeclared_or_retryable_failure_keeps_reconnecting`）、呈现表 2 条、`SingleBanJudgeBoundaryTests` 源码门禁 1 条 |
| PLT-003 | 2 → 7 | 发现读侧 4 条 + 真进程连接 1 条（`RuntimeDiscoveryProcessTests.Another_process_connects_with_only_the_discovery_file`） |
| PLT-004 | 5 → 6 | `RuntimeDiscoveryTests.The_token_never_rides_along_in_a_string`（令牌不进字符串表示） |
| PLT-006 | 3 → 7 | Runner 的游标首订、按序号续订、恢复只宣告一次、首连不算恢复 4 条 |
| REC-001 | 4 → 6 | 呈现表的"七类码各给一条能做的事"与"timeout 读成慢不是错" |

一处刻意**没有**做的映射：`A_descriptor_without_a_token_is_refused_because_nothing_could_pass_the_gate` 我一度挂到 `PLT-004`（鉴权），随即撤掉——它判的是"缺令牌的文件我不连"，属发现侧（`PLT-003`）而不是默认拒绝门。按 README 的约定，映射是证据不是计数，松挂一条等于给未来的"绿"掺水。

## Deferred Eval

沿用第四轮：`PLT-012` 残留子进程（待 `PRY_TEST_LLAMA_*`）、`PLT-010`（待 §5.0 解冻 PLAT-007）、Timing P50/P95 与 Behavior/Expression/Memory 全类（待接线 + 真实模型）、纵向内测 Day 1/2/7（0 样本）。本轮移除 `PLT-003`（已判 Pass）与 `PLT-006`（客户端义务已判 Pass，服务端裁剪侧随 `PLT-010` 继续挂起）。新增一项：**`App.InitializeDesktopAsync` 的"先发现后内嵌"顺序**，需要 Avalonia 生命周期或人工走查，与 `QA-P5-01` 同源。
