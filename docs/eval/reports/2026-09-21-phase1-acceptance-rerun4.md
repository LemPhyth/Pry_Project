# Phase 1 验收判定 · 第四轮

Run ID: `eval-QA-P1ACC-0921D`　日期: 2026-09-21 16:58–17:10 本地
前序: `0921`（02:41，456/3/2）→ `0921B`（14:58，535/0/2）→ `0921C`（15:56，546/0/2；16:06 自查更正）→ 本轮
候选: 分支 `codex/ui-main-window-redesign`，HEAD **`f1c5118`**（`git log main..HEAD` 两格都是许可/文档提交，**仍无任何 Phase 1 代码提交**）
工作区: 仍 dirty——**50 已修改 + 123 未跟踪 = 173 条**（上轮 170）；`verify-repository.ps1` 通过 **214** 个受跟踪文件，口径不变
环境: Windows 10.0.26200 x64 / .NET SDK 10.0.200 / `net10.0` / Release；跑门禁前确认 `dotnet.exe` 只有 16:22 起的空闲 MSBuild 节点复用进程，无同级构建或测试在跑

**结论：Phase 1 仍不通过。** 第三轮点名的两条阻塞里，产品行为那条（`PLT-002`）**已闭合**；剩下的是一条结构性阻塞（无统一可审查提交），外加本轮我自己**把一项误判降级**（`PLT-003` Pass → Blocked）与两条新登记的 Medium。

> 本文件描述的是 **16:58 快照**。同日晚些的第五轮（`eval-QA-P1ACC-0921E`，22:02）已把本文件的两项 Blocked 复判为 Pass：`PLT-003`（作者裁定"需要独立进程客户端"后 `RuntimeDiscovery` + 真进程用例落地、界面层三处 5078 已清）与 `PLT-006`（统一 Runner 的客户端义务）。本文件的 551/0/2、173 条未提交与 Release Blockers 第 2、3、5 条**不要再当现状引用**；判定本身（不通过）与"PLT-003 那次降级是对的"依然成立。

## Regression Results

```
dotnet build Pry.slnx -c Release           → 已成功生成，0 个警告 0 个错误
dotnet test Pry.slnx -c Release --no-build → 通过，退出码 0（--blame-hang-timeout 120s）
  Pry.Core.Tests   242 通过 / 0 失败 / 2 跳过（244）
  Pry.Api.Tests    123 通过 / 0 失败 / 0 跳过（123）
  Pry.Client.Tests  19 通过 / 0 失败 / 0 跳过（19）
  Pry.App.Tests    167 通过 / 0 失败 / 0 跳过（167）
  合计             551 通过 / 0 失败 / 2 跳过（553）
verify-repository.ps1                      → 214 个受跟踪文件通过（仍不覆盖 173 条未提交条目）
git diff --check / --cached --check        → 无输出
measure-fixed-eval.ps1                     → Catalog valid: 67；Passed=551; Failed=0; Other=2；退出码 0
```

相对第三轮（548）：+5，全部来自 `tests/Pry.App.Tests/EventStreamReconnectBanTests.cs`（新文件）。2 项跳过仍是同样两条环境门控的真实二进制用例。TRX 与证据目录：`TestResults/phase1-acc-0921d/`。本轮我**独立重跑**得到的 551/0/2 与实现方自行回填进 `docs/phase1-platform-baseline.md` §6 的数字逐位一致（含 Api 123 / Core 242+2skip / Client 19 / App 167 与 214 个受跟踪文件），两侧互相印证。

## QA-P2-01 后半（`PLT-002` 的界面分支）→ 已闭合，且方向正确

16:20–16:32 一批改动把最后一环接上了，逐处读到代码：

- **判据收敛成一个读法**：`PryBackendException.RetryForbidden => Retryable == false`（`src/Pry.Client/PryBackendClient.cs:277`）。注释把三值语义钉死："缺席（`null`）与 `true` 都不是禁令……把旧服务端的沉默读成禁行，等于把一次网络抖动做成永久失败"。这正是第二轮我要求的形状，没有出现"缺席即假"的偷懒写法。
- **两处事件流循环都接**：`RemoteConversationTurnManager.cs:94-98` 与 `PryMessengerWindow.axaml.cs:488-493`，均为 `catch (PryBackendException ex) when (ex.RetryForbidden)`，**位置在通用 `catch (Exception)` 之前**（顺序错了就等于永不生效，这一点我按行序核过），命中后直接 `return`——不再退避、不再重启请求、不再安排 3 秒后的重连。
- **文案可执行**：`src/Pry.App/Services/BackendBanNotice.cs` 按 `Code` 分三档（协议不匹配→用同一发行版启动；令牌被拒→完全退出再启动；未登记禁行码→带出服务端原码），与 `docs/api-v1.md:27` 的要求一致；未登记码走兜底分支而不是回落成"正在重连"。
- **回归双向都有**：`EventStreamReconnectBanTests` 5 例——禁行方向 3（426/401/未登记的 502 `provider_auth`，各断言 `handler.EventRequests == 1` 且 1.5s 观察窗内只弹一条终止提示），放行方向 2（`retryable` 缺席与 `retryable:true` 必须**继续**重连、`failures == 0`）。**放行方向有例是本轮最有价值的一点**：只测禁行的守卫无法区分"禁行生效"与"什么都不许重连"，后者会把网络抖动做成永久失败。桩处理器还专门处理了"宿主在构造函数里就开始订阅"的竞态（第一次请求等 `Release()` 放行），说明"只发一次"这条断言不是靠时序侥幸。
- 实现方自报做过证伪跑（条件临时改 `when (false)` → 三条禁行例转红、两条放行例不受影响）。我没有复跑证伪，但本轮判据本身可复核：`src/Pry.App` 今天对 `RetryForbidden` 有 2 个消费点（上轮是 0）。

**→ `PLT-002` 由 Fail 转 Pass**（三条映射已登记：禁行 2 + 放行 1，Theory 各按参数展开为 10 条证据结果）。

## Coverage 变化

| 场景 | 第三轮 | 本轮 | 说明 |
|---|---|---|---|
| PLT-002 | Fail | **Pass** | 禁令抵达两处循环并停止退避；双向用例守住 |
| PLT-003 | Pass | **Blocked（本轮更正）** | 见下节"自我更正"。**这是降级，不是升级** |
| PLT-001/004/005/007/008/009/011 | Pass | Pass | 映射断言本轮全绿，无回归 |
| PLT-006 | Blocked | Blocked | 统一 Runner 仍未进主工作区；另见 QA-P4-02 |
| PLT-010 | Blocked | Blocked | 属 PLAT-007，§5.0 冻结中 |
| PLT-012 | NotRun | NotRun | 残留 `llama-server` 只在环境门控真实用例里，不以相邻绿灯代填 |
| REC-005 / PER-005 / REC-001 | Pass / Pass / PartialPass | 不变 | 探测有界、schema 单一来源、七类码无回归 |
| Timing/Behavior/Expression/Memory/Relationship/Capability | NotRun | NotRun | `Intelligence`/`Expression` 仍未接线、无真实模型运行；工程门禁不产生这些类的 Pass |

## 自我更正（第二处）：`PLT-003` Pass → Blocked

**错在哪**：第二、三轮我把 `PLT-003`（"独立进程仅凭发现文件连上"）记成 Pass，采信的是 `SessionTokenTests` 两条映射。但那两条证明的是**服务端**：写出的端口是真持有的端口、陈旧文件被覆盖且只留一条恢复记录。场景自己的 `input` 是"**客户端读取描述符后连接**"、`expectedOutputConstraints` 是"**客户端不需硬编码 5078**"——这两半今天都没有实现：

1. `src/` 全树**没有任何 `runtime.json` 读取者**。`RuntimeDescriptorService` 只负责写；`RuntimeDescriptor` 记录只存在于 `Pry.Contracts/PryProtocol.cs:108`。也就是说"另一个进程只凭发现文件连上"这件事**没有消费者**，1.0 的界面始终走 `EmbeddedBackend` 同进程内嵌（`App.axaml.cs:47` 绑 `127.0.0.1:0`，`:49` 注释明确"同进程内嵌时读同一个服务对象，不读 runtime.json"）。
2. `Pry.App` 仍有三处 `http://127.0.0.1:5078/` 字面量构造点：`MainWindow.axaml.cs:78`、`PryMessengerSettingsWindow.axaml.cs:31`、`PryMessengerWindow.axaml.cs:49`。三者都是**无参构造器**，实跑路径注入 `_api` 所以不会真去连 5078（这也解释了为什么 551 全绿），但 `tests/Pry.App.Tests/MessengerModelDraftTests.cs:15,33` 确实会构造后者——"界面层硬编码端口"这个第一层症状仍然存在。
3. **决定性的一条**：实现方自己的文档早就写明这里不能结案。`docs/protocol-versioning-and-websocket-migration.md:132`——"**本项按 §8 尾注属'需要进程级测试'，此处不能结案**……'另一个真进程只读 `runtime.json` 就连上'要求 `Pry.App` 之外的调用方，属 QA 阶段②的 `PLT-003`"；§8.1 表 PLT-003 行同样写"仍待阶段②固定为可重放 Eval"。**我没读这两句就记了 Pass**，而我的 README 里正写着"automatedTests 仅为部分证据……SQLite reopen 不证明断电恢复"。同一个陷阱我又踩了一次，且这次是往松的方向。

**改判**：`PLT-003` = **Blocked**，负责人 PLAT-005（M0-d 客户端半片）。两条服务端映射保留为部分证据。

**同时抛给作者的裁定（不是我该代答的）**：1.0 是否真的需要一个"读 `runtime.json` 连上"的独立进程客户端？今天的发行形态是界面内嵌后端，发现文件写好但没人读。
- 若**需要** → PLAT-005 补客户端半片（含一条真·进程级 Eval），`PLT-003` 才可能转 Pass；
- 若**不需要** → 该场景按约定记 **NotApplicable（1.0 范围外）并保留 ID 不删**，同时三处 5078 字面量应从界面层清掉（它们现在是"没人用但不该在那"的代码）。
QA 不代为放宽，也不代为取消需求。

## Runtime Findings

事件流模型（到达队列 + 持久锚点）、心跳与半开检出、游标跨重启、电平类不重放——四项与第三轮一致全绿，本轮无相关改动。新增可观察行为：426/401/未登记禁行码在**两条订阅循环**上都变成"停止退避 + 一句能照做的提示"。未变的边界：默认路径仍是内嵌后端，独立进程连接仍不可达（见上）。

## Critical / High / Medium / Low Issues

本轮无 Critical，无新增 High。存量与新登记：

- **QA-P2-02（High，流程门槛，仍是唯一结构性阻塞）** — Phase 1 代码批次无统一提交：50 改 + 123 未跟踪。`docs/eval/`（67 场景 catalog + 四份运行报告）与 `scripts/measure-fixed-eval.ps1` **至今全量未跟踪**，"永久场景不得删除"的约定在 git 层面仍未兑现；一次误 `git clean` 即不可恢复。另：`docs/phase1-platform-baseline.md` §6 本轮被回填了一条 551/0/2 记录，但那是**工作区快照**的回填，不是我要的"从统一提交重跑 §7 后回填"，两者不等价。
- **QA-P4-01（Medium，新）：被自动化守住的循环不在应用运行路径上** — `EventStreamReconnectBanTests` 5 例全部驱动 `RemoteConversationTurnManager`，而它唯一构造点是 `MainWindow.axaml.cs:225`；`MainWindow` 在 `src/` 内**无任何构造点**（`App.axaml.cs:67` 无条件 `new PryMessengerWindow(api)`，`:71` 的 `case MainWindow classic` 永不匹配）。真正运行的 `PryMessengerWindow.ObserveEventsAsync` 分支**零自动化覆盖**。后果不是今天的缺陷（我按行读过那段代码，形状与受测侧一致），而是**回归无人报警**：UI-027 的 Runner 合流改写这两个文件时，把 `when (ex.RetryForbidden)` 弄丢不会有任何测试变红。修法二选一：补一条同形用例驱动 `ObserveEventsAsync`，或按 `PLT-006` 把两处收敛成唯一 Runner（后者更好，一并解决 QA-P4-02）。归属：UI-027。**这条实现方自己在迁移文档 §8 里已如实收窄并声明"本项仍不在此结案"，不是抓出来的隐瞒。**
- **QA-P4-02（Medium，新）：禁令判据出现两处形状，且合流会撞同一批文件** — worktree `../Pry_Project_client_error_reconnect`（UI-027，基线 `134eddc`）里未提交的 `src/Pry.Client/ConversationEventStreamRunner.cs` + `PryErrorPresentation.cs` 同时改动了 `src/Pry.App/App.axaml.cs` 与 `PryMessengerWindow.axaml.cs`，而后者正是本轮 16:20–16:32 被改过的文件。今天禁令读法有两套：循环内 `when (ex.RetryForbidden)` + `BackendBanNotice`，与 Runner + `PryErrorPresenter`。迁移文档已立规矩（"本分支是其子集，不另立第二套错误状态源"），但规矩不等于不冲突。归属：UI-027 与 PLAT-005 联合收口，`PLT-006` 继续 Blocked。
- **QA-P2-04①③（Medium，存量）** — ①`PryErrorPresenter.Classify` 与七类错误码同步扩类仍等 UI-027；③转写预算**到期**分支仍无用例（`BackendRequestBudgetTests.cs` 仍是两条预算形状断言 + 一条取消路径）。② 已在上轮结案。

## User Feedback Converted to Tests

0；未收集。EVAL-002 仍只有模板与 0 样本，不以模板代替真实反馈。

## Release Blockers（Phase 1 结案前必需，顺序即建议顺序）

1. **统一可审查提交**（唯一结构性阻塞）：集成负责人把 Phase 1 代码批次连同 `docs/eval/`、`scripts/measure-fixed-eval.ps1` 一起形成提交，**从该提交**重跑 §7 五条命令并回填 `docs/phase1-platform-baseline.md` §6。本轮 551/0/2、上轮 546/0/2、再上轮 535/0/2 都只各自描述当分钟快照。
2. **`PLT-003` 的方向裁定 + 补齐**：作者先答"1.0 要不要独立进程客户端"，再由 PLAT-005 实施或按 NotApplicable 保留场景；顺带清掉界面层三处 5078 字面量。
3. **QA-P4-01**：给真正运行的那条循环补一条同形用例，或用 PLT-006 的 Runner 收敛（推荐后者）。
4. **PLAT-001/005 台账状态**按 QA-P2-04①③ 收敛，未做前不标"已完成"。
5. `PLT-006`、`PLT-010`、`PLT-012` 如实保持 Blocked / NotRun。它们不阻塞其余结案，但**阻塞"RT-001 / PLAT-005 已完成"这两个标记本身**；是否允许带着结案属作者/守门人的 §5.0 裁量，QA 不代为放宽。
6. 真机端到端（Client 启动 → 连 Core → 聊天 → 重启后历史仍在）本轮仍只到"内嵌真实后端的自动化"层，QA 未做界面实操验证；建议在统一提交之后由作者或 QA 走一遍并记录。

## Deferred Eval

沿用第三轮清单，无变化：PLT-012 残留子进程（待 `PRY_TEST_LLAMA_*`）、PLT-006（待 UI-027）、PLT-010（待 PLAT-007 解冻）、Timing P50/P95 与 Behavior/Expression/Memory 全类（待接线 + 真实模型）、纵向内测 Day 1/2/7（0 样本）。本轮另加 `PLT-003` 的进程级 Eval（待第 2 条裁定）。
