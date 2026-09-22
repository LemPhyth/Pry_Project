# Phase 1 验收判定（QA / Evaluation）

Run ID: `eval-QA-P1ACC-0921`　日期: 2026-09-21（约 02:41–03:05 本地）　QA: Qoder QA / Evaluation
候选: 分支 `codex/ui-main-window-redesign`，HEAD `3613a2a`（`feat(app): use high-resolution brand artwork`）
工作区: **dirty snapshot，非统一提交**。`git status --porcelain` = 38 已修改 + 95 未跟踪（其中 `src/`、`tests/` 下 74 个）。Phase 1 的全部代码改动与本次三条失败涉及的测试文件都不在版本控制里。
环境: Windows 10.0.26200 x64 / .NET SDK 10.0.200 / `net10.0` / Release
对照上一 Run ID: `eval-QA-RERUN-0920`（366/0/1）。本轮不改模型、不改角色卡、不采集真实用户反馈——这是**工程门禁复跑 + 阶段判据核对**，不是产品验收。

**结论：Phase 1 不通过。** 建议保持"进行中（RT-001 + PLAT-001/002/005/006）"。阶段指针由作者/守门人按 §5.0 改，本节只给 QA 判定。

> 本文件是 02:41 快照的判定，已被同日 `eval-QA-P1ACC-0921B`（14:58）与 `eval-QA-P1ACC-0921C`（15:56）取代为现状：本文件的三条红门禁（456/3/2）现已全部按此处的方向归零，456/3/2 与"HEAD `3613a2a`"不要再当现状引用。判定"不通过"作为阶段结论仍在延续。

## Coverage

本轮不逐条执行 53 个永久场景（真实模型、真机断链、纵向观察均未执行，见 Deferred Eval）。覆盖的是 **Phase 1 判据的自动化证据面**：

| Phase 1 判据 | 状态 | 实际证据 | 未覆盖约束 | Owner |
|---|---|---|---|---|
| RT-001「验证 SQLite、配置、日志、Provider、消息持久化和重启」 | **Fail** | 构建 0 警告 0 错误；两个真实 SQLite 重启用例（`ConversationReadEndpointTests`、`ProtocolEventCursorTests`）跑通其前半 | F1 直接落在 SQLite schema 版本断言上；日志（PLAT-006）与 Provider 错误语义（PLAT-001）零实现；`ConversationEventLog` 的取消语义回退 | RT-001-INT + PLAT-005 |
| PLAT-001 有界超时 + 七类错误码 | **Blocked**（零实现） | 无 | 台账未列的第五处无界客户端；真实 Provider 401/429/超时矩阵 | Core Platform |
| PLAT-002 密钥经 env 块 | **部分** | 代码已并入主工作区（`LlamaServerManager.cs` env 块 + 清除继承项）；参数/环境回归通过 | 真实 `llama-server` 鉴权用例本轮 `Skip`（需 `PRY_TEST_LLAMA_SERVER`/`PRY_TEST_LLAMA_MODEL`） | Core Platform |
| PLAT-005 M0-a/M0-b/M0-c | **Fail** | 协议/游标/心跳自动化全绿 | F3 由 M0-c 重写引入；F2 使主动消息时序无证据 | PLAT-005 |
| PLAT-005 M0-d | **Blocked**（零实现） | 无 | `runtime.json` 发现、默认拒绝鉴权中间件、token 不进日志、停机清理 | PLAT-005 |
| PLAT-006 Core 日志 + 上传硬上限 | **Blocked**（零实现） | 无 | 见缺陷清单 | Core Platform |

用例数不等于场景通过率：本轮 459 个用例不产生任何永久场景的 `Pass`。

## New Scenarios

本轮不新增永久场景 ID（采纳动作欠着，见 Deferred Eval）。以下是我判定**必须新增**的草案，供下一次执行时写入 catalog；ID 治理权在 QA，我未自行落号：

1. `PLT-004` 拆分：turn.state 时序（活订阅）——`events = EventsAsync(...)` 必须在 `RunOnceAsync` **之前**建立，断言 `AgentSending` 帧先于 `message.created`；禁止结果：把 `turn.state` 改回持久事件以满足旧断言。
2. 取消可观测：订阅中 `token.Cancel()` 后消费者必须能区分"主动取消"与"服务端 EOF"。Input: 已建立的事件枚举器 + 取消；Forbidden: 取消被静默转成正常结束。
3. schema 版本单一来源：写侧字面量与任何读侧断言不得并存两个硬编码值；前滚与重启后 `MAX(version)` 必须一致。
4. 端点默认拒绝遍历（§8 第 4 条 + §8.1 硬提醒）：枚举全部已注册端点，缺 token 必须 `401` 或在豁免白名单（只有 `/health`、`/api/v1/info`）。
5. 七类稳定错误码逐类映射（Auth/RateLimit/InvalidResponse/ModelUnavailable/ContextOverflow + 进程启动类 + 取消）。

## Regression Results

`PROJECT_PLAN.md` §7 五条命令，本轮实际执行：

```
dotnet build Pry.slnx -c Release                      → 通过，0 警告 0 错误
dotnet test Pry.slnx -c Release --no-build
  --blame-hang-timeout 120s                           → 失败，退出码 1
scripts/verify-repository.ps1                          → 通过：205 个受跟踪文件（不覆盖 74 个未跟踪 src/tests 文件）
git diff --check                                       → 无输出
git diff --cached --check                              → 无输出
```

| 程序集 | 通过 | 失败 | 跳过 | 总计 |
|---|---:|---:|---:|---:|
| Pry.Core.Tests | 206 | **1** | 2 | 209 |
| Pry.Api.Tests | 74 | **2** | 0 | 76 |
| Pry.Client.Tests | 14 | 0 | 0 | 14 |
| Pry.App.Tests | 162 | 0 | 0 | 162 |
| **合计** | **456** | **3** | **2** | **459** |

TRX/日志: `TestResults/phase1-gate/gate.trx`、`TestResults/phase1-gate/run.log`。三条失败**单跑同样失败**，不是并行竞态。跳过的 2 项均为环境门控的真实二进制用例（`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`、`CoreTests.Configured_sense_voice_model_can_run_offline`），符合项目基线，不以跳过冒充真机验收。

与集成负责人最近一次记录（`docs/phase1-platform-baseline.md` §6，2026-09-21 M0-b 后 **444/0/2**）对比：用例净增 15，且新增 3 条失败。**该 444/0/2 已不再描述当前工作区**，不能作为 Phase 1 结案证据引用。

## Runtime Findings

本轮唯一被执行到的 Runtime 面是 `ConversationRuntime` + 协议事件journal。可观察到的状态序列：主动消息回合从**重放游标**只看到 `message.created`，看不到任何 `turn.state`。原因不是 Runtime 未发布（`ConversationSessionService.cs:362` 确实在 `Delivering` 相位发 `AgentSending`），而是 M0-c 把 `turn.state` 定为电平类且不重放。这本身是 §4.1 第 10 条要求的行为（重连不得把三分钟前的 `ModelThinking` 推回界面），故**判为契约漂移而非产品回退**——但代价是这条时序目前无任何通过用例。

恢复/重连可见历史：持久事件（`message.created`、`conversation.read`）跨重启可见，已有通过用例覆盖；瞬态相位不可见（预期）。取消语义见 F3，属真实回退。

## Behavior Findings

NotRun。`Pry.Core/Intelligence`（BehaviorPlanner / RelationshipState / MoodState）代码已落地但**未接线**，无生产调用方可观察其决策；Phase 1 判据不含 Behavior。

## Expression Findings

NotRun。`Pry.Core/Expression` 同上未接线；无真实模型运行，规则测试不计入表达评测。

## Memory Findings

| 模型/模式/场景组 | 查询数 | U | F | M | U/(U+F) | U/(U+M) | 错误查询数/查询数 |
|---|---:|---:|---:|---:|---|---|---|
| 未评测（MEM-005/006 随 §5.0 冻结，Phase 5 前置） | NotRun | NotRun | NotRun | NotRun | N/A | N/A | N/A |

不填零。既有 QA-004（`ConversationService.LooksMemorable` 零调用方＝自动记忆抽取未接线）与 QA-005（`MemoryEmbeddingService.cs:87` 无 embedding 模型时静默降级为关键词）仍挂起；两者属 Phase 5 判据，**不阻塞 Phase 1**。F1 命中的是消息持久化的 schema 断言，不是召回质量。

## Critical / High / Medium / Low Issues

本轮无 Critical。已验证事实与推断分开标注。

### QA-P1-01　High　Product defect（测试与实现互斥，二者必改其一）
`Pry.Core.Tests.CoreTests.Sqlite_stores_and_retrieves_messages_and_memories`
最短复现：`dotnet test tests/Pry.Core.Tests -c Release --no-build --filter FullyQualifiedName~CoreTests.Sqlite_stores_and_retrieves`
Expected `8` / Actual `9`。写侧 `src/Pry.Core/Memory/MemoryDatabase.cs:134`（`SetSchemaVersionAsync(connection, 9, …)`），读侧断言 `tests/Pry.Core.Tests/CoreTests.cs:132`。
影响：同一工作区内对同一 DB 的版本断言自相矛盾；v8→v9 迁移路径没有任何用例证明可前滚、可重启后一致。**推断**（非本轮验证）：老用户 DB 停在 8 时的实际前滚结果未测，故未定 Critical。
Owner：PLAT-005/RT-001/MEM 三方对 §PLAT-003 的"编号归持久化负责人统一裁定"未裁定——PLAT-003 现属 §5.0 冻结项，因此这条不是"顺手改断言"能结案的。修复后固定回归 ID：新场景 3（schema 单一来源）。

### QA-P1-02　High　Product defect（未提交改动引入的语义回退）
`Pry.Api.Tests.ConversationReadEndpointTests.Read_endpoints_validate_publish_and_survive_backend_restart`
`ConversationEventLog.cs:108`（本轮新增）把订阅者取消转成 `yield break`；HEAD 版同位置是 `await wait.WaitAsync(token)`，向消费者传播 `OperationCanceledException`。测试在 `:65` 期望取消可观测。
影响：上层无法区分"我取消了"与"服务端正常关流"。直接抵触 PLAT-001 完成标准"取消可观测"与迁移文档 §8.1 `PLT-005` 的"正常 EOF 与超时是两条不同退出路径"。挂起的 Provider/后端会因此更难定位。
Owner：PLAT-005（该文件受跟踪、由 M0-c 修改）。要求二选一并写进迁移文档：恢复传播，或显式改契约并同步该未跟踪测试。**不得**以删测试结案。

### QA-P1-03　Medium　Test defect + Coverage gap
`Pry.Api.Tests.ProactiveSchedulerTests.Scheduler_delivers_one_persisted_message_and_sse_then_waits_for_user`
`:39` 在投递完成后才订阅，`:46` 断言 `phases` 含 `AgentSending`，实际得空集。根因：`MemoryDatabase.ProtocolEvents.cs:17` 将 `turn.state` 列为电平类，`ConversationEventLog.cs:75` 在订阅时把 volatile 刻度取在当前位置，电平类按设计永不重放。
影响分两层：断言在新契约下不可能满足（测试缺陷）；同时 §7.3 阶段 DoD「已启用主动消息的审核/取消/过期用例通过」因此**缺一条有时序证据的用例**（覆盖缺口）。
Owner：`ProactiveSchedulerTests` 的作者（未跟踪文件）。修复方向：改为先订阅再 `RunOnceAsync`。

### QA-P1-04　High　阶段判据缺口（非新缺陷）
PLAT-005 台账原文：「M0 四片全部落地并过第 8 节判据才算本任务完成」。**已验证事实**：M0-d 的 `runtime.json` 与本机会话令牌在 `src/` 内零命中，因此 §8 第 3、4、9 条（独立进程发现、默认拒绝鉴权、停机清理）没有实现体可测。PLAT-001（五处无界 HTTP 客户端、七类错误码只有进程启动类）、PLAT-006（Core 零日志、`MultipartBodyLengthLimit = long.MaxValue` 出现在 `Program.cs:75` 与 `MediaController.cs:12` 两处）同为 Phase 1 台账项且零实现。`Pry.Contracts.csproj:3` 对 `Pry.Core` 的引用仍在。
补充台账缺口（建议 Owner 更新 PLAT-001/006 行）：PLAT-001 的无界客户端清单漏了 `PryMessengerWindow.axaml.cs:49`；PLAT-006 的 `long.MaxValue` 是两处不是一处。

### QA-P1-05　High　流程门槛未满足
`git log main..HEAD` 无 Phase 1 代码提交，HEAD 是素材提交 `3613a2a`。§5.0 把"形成可审查提交"写成阶段门槛；`docs/phase1-platform-baseline.md` §7 已自行把该条从"整洁性问题"升级为**结案硬条件**。**已验证事实**：本轮 3 条失败里有 2 条（QA-P1-01、QA-P1-02）正是"未统一提交 → 两方断言与契约各自漂移"的直接产物；受影响文件源哈希见文末。

### QA-P1-06　Medium　QA 自有缺口（不用于互相免责）
迁移文档 §8.1 第②段（把 `PLT-001`…`PLT-008` 采纳/改写进 `docs/eval/scenarios.json`）我尚未执行。场景 ID 治理权在 QA，因此这一段是**我欠着**，不是实现欠着。完成它不会改变本次判定。

## User Feedback Converted to Tests

0；未收集。本轮无纵向内测观察输入，不把内部 QA 当作 early adopter，也不伪造反馈。

## Release Blockers

当前结论：**阻塞**，不可进入下一验收阶段。Phase 1 结案前必需：

1. QA-P1-01/02/03 三条失败归零（Owner 各自裁定方向，不由 QA 改业务代码）。
2. 由集成负责人形成**统一可审查提交**，从该提交重跑 §7 五条命令并回填 `docs/phase1-platform-baseline.md` §6；本轮 456/3/2 与既有 444/0/2 都只描述可变快照。
3. M0-d 落地并过迁移文档 §8 第 3、4、9 条（含"缺 token 遍历已注册端点必须 401 或在白名单内"与"token 不进日志"）。
4. PLAT-001、PLAT-006 至少到台账定义（有界超时 + 七类稳定错误码 + Core 可注入日志 + 上传硬上限），并移除 `Pry.Contracts → Pry.Core` 引用。
5. 真实模型端到端与真机断链按项目基线单列，不以 459 用例绿冒充。

需要交接的 Owner：RT-001-INT（Architecture/Integration/Release）、PLAT-005、`ProactiveSchedulerTests` 与 `ConversationReadEndpointTests` 作者、UI-027（`PryErrorPresenter` 进主工作区后 §8 第 2 条才可结案）、MEM-005/006（QA-004/005，不阻塞 Phase 1）。

传播边界提醒：`PROJECT_PLAN.md` 被 gitignore、`docs/eval/` 整体未跟踪，本文件到其他 Owner 之间只能经作者转达。

## Deferred Eval

| ID / 项 | 原因 | 依赖任务 | 下次触发条件 |
|---|---|---|---|
| PLT-001…PLT-008 采纳进 catalog | §8.1 第②段动作未做（QA 自有） | PLAT-005 | 统一提交后立即做，不必等 M0-d |
| PLT-003 / PLT-008 真实进程发现与停机 | M0-d 零实现 | PLAT-005 M0-d | M0-d 落地 |
| PLT-005 OS 级真断链与半开检出 | 本轮只有传输层与回环 HTTP 断言 | M0-b | 可进程级测试时 |
| Memory U/F/M 三项统计 | MEM-005/006 属 Phase 5 前置，§5.0 冻结 | MEM-005 | §5.0 解锁 Phase 5 |
| Behavior / Expression / Relationship 全类 | 代码未接线 + 无真实模型运行 | RT-003/004/006 接线 | 接线并启用真实模型后 |
| 真实模型鉴权与语音（本轮 2 Skip） | 环境变量未设置 | PLAT-002 / 语音权重 | 具备 `PRY_TEST_LLAMA_*` 与权重时 |
| 纵向内测（Day 1 / Day 2 / Day 7） | 未收集 | §7.2 | 有 2–3 名明确早期用户后 |

## 受影响源文件指纹（sha256 前 12 位）

```
e056cb2a90c2  src/Pry.Api/Services/ConversationEventLog.cs            (M)
55d1376c7ef7  src/Pry.Core/Memory/MemoryDatabase.ProtocolEvents.cs    (??)
5d1ed31ef304  src/Pry.Core/Memory/MemoryDatabase.cs                   (M)
f09431f7a218  tests/Pry.Core.Tests/CoreTests.cs                       (M)
dabae1ccda43  tests/Pry.Api.Tests/ProactiveSchedulerTests.cs          (??)
77c4f4ae9b8f  tests/Pry.Api.Tests/ConversationReadEndpointTests.cs    (??)
```

三条失败的两条根因都在这一批未提交改动里。指纹只在本轮分钟级有效（见多 worktree 记忆约定），统一提交后应重新核对。
