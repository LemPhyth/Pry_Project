# Phase 1 验收判定 · 第二轮（同一天新快照）

Run ID: `eval-QA-P1ACC-0921B`　日期: 2026-09-21 14:58 本地（上一轮 `eval-QA-P1ACC-0921` 是同日 02:41）
QA: Qoder QA / Evaluation　候选: 分支 `codex/ui-main-window-redesign`，HEAD `3613a2a`
工作区: **仍是 dirty snapshot**——52 已修改 + 121 未跟踪（Phase 1 全部代码仍未提交；`verify-repository.ps1` 只覆盖 205 个受跟踪文件）
对照: 上一轮判定"不通过"（456/3/2）。本轮先复核三条阻塞是否修复，再走 §8.1 第②段的场景采纳。

**结论：Phase 1 仍不通过，但性质变了——只剩一条结构性阻塞（统一提交）加一条契约断链（`retryable` 在客户端边界被丢弃，导致界面把版本不匹配当网络故障无限重连）。** 上一轮三条红门禁全部按我建议的方向归零，不是被删测试归零。

> 本文件是 14:58 快照（535/0/2、HEAD `3613a2a`、52 改 + 121 未跟踪），已被同日 `eval-QA-P1ACC-0921C`（15:56）部分取代：**QA-P2-03 已闭合（`PLT-001` Blocked → Pass）、QA-P2-01 的契约那一半已闭合、QA-P2-04② 已闭合**；`PLT-002` 因界面侧仍无分支继续记 Fail，统一提交仍开。引用本文件的四条 QA-P2 编号时以第三轮报告为准。

## Coverage

本轮实跑 §7 五条命令 + `measure-fixed-eval.ps1`（catalog 校验 + TRX 证据映射），并按迁移文档 §8 的九条协议验收逐条对代码取证。永久场景从 53 个扩到 **66 个**（`suiteVersion` 1.0.0 → 1.1.0，新增 `PLT-001`…`PLT-012` 与 `PER-005`；另为 `REC-001`/`PRO-001`/`PER-001` 追加证据映射，未改任何既有场景语义）。目录里的 `acceptance` 一律保持初始值 `NotRun`，判定写在本文件（约定见 `docs/eval/README.md`）。

`measure-fixed-eval.ps1` 结果：`Catalog valid: 66`；`Passed=535; Failed=0; Other=2`，退出码 0；自动化证据 **50 个场景 PartialPass / 16 个 Unmapped**，无 `NotRun`/`Incomplete`（即每条已登记映射的断言都在本轮 TRX 里找到且通过）。**PartialPass 只说明"映射断言全绿"，不是场景验收**；下表才是 QA 判定。

| 场景 | 本轮判定 | 依据与边界 |
|---|---|---|
| PLT-001 只绑回环 | **Blocked** | 守卫在 `src/Pry.Api/Program.cs:21-22`（非回环即抛），但**无任何回归用例**遍历该分支。§8 第 1 条自称"现有行为回归"不成立，缺一条"配 `0.0.0.0` 启动必须失败"的进程级断言 |
| PLT-002 不匹配停在明确提示、不重试 | **Fail** | 见 QA-P2-01。服务端侧全对（`426` + `retryable:false` + 写入前拒绝 + `/api/v1/info` 仍可达），契约与界面侧断链 |
| PLT-003 仅凭发现文件连上 | **Pass** | `SessionTokenTests` 用真实 Kestrel + 真实描述符文件断言"发布实际持有的端口""两实例端口不同""陈旧文件被替换且只留一次恢复痕迹"。硬编码 5078 只剩 `MainWindow.axaml.cs:78` 的构造默认值，运行期被描述符覆盖 |
| PLT-004 默认拒绝 + token 不外泄 | **Pass** | 五用例：端点遍历、缺/错 token 响应不可区分、带 token 全通、拒绝发生在写入前（持久日志条数不变）、token 在响应与每一行日志里都不出现。这一条把 §8.1 我提的"遍历已注册端点"硬提醒落成了实现 |
| PLT-005 游标跨重启不丢不重 | **Pass** | 服务端/存储侧义务已证；`after=-1` 永不判过期、窗口内静默补投 |
| PLT-006 裁剪后客户端先重取历史 | **Blocked** | 服务端宣告恰一帧已证；客户端义务无载体——`src/Pry.Client/` 仍无 `ConversationEventStreamRunner`（在 worktree `../Pry_Project_client_error_reconnect`）。今天 `PryMessengerWindow` 以数据库投影重取，故**无数据丢失回归**，但判据本身不能结案，负责人 UI-027 |
| PLT-007 半开有界检出 | **Pass**（传输层） | 两个心跳周期 + 抖动（约 32s）实测断言；OS 级真断链与退避恢复属 PLT-006/Runner，未做 |
| PLT-008 取消与关流两条路径 | **Pass** | 上一轮 QA-P1-02 的修复方向被采纳：`ConversationEventLog.cs:113-115` 恢复原样传播并写明理由 |
| PLT-009 电平类只在活订阅内有序 | **Pass** | 上一轮 QA-P1-03 按建议改测试（先订阅后投递），并显式禁止"把电平类改回持久事件让断言变绿" |
| PLT-010 大媒体只带 mediaId | **Blocked** | `messages.image_path` 仍存路径、事件帧无 `mediaId`（PLAT-007 属 §5.0 冻结项，正确归属，不算 Phase 1 缺陷） |
| PLT-011 未知事件前向兼容 | **Pass** | 注释帧不干扰解析、未知 type 不断连 |
| PLT-012 退出无残留 | **NotRun** | 描述符生命周期两向已证；"无残留 `llama-server` 子进程"只在环境门控的真实二进制用例里，本轮 Skip，不以相邻绿灯代填 |
| PER-005 schema 单一来源与只前滚 | **Pass** | 上一轮 QA-P1-01 的根因被当作契约问题修：`MemoryDatabase.SchemaVersion` 常量（`:153`）为唯一来源，读侧断言改引用它，另有旧库前滚一次并保持、`/info` 报的版本与实际打开的库一致 |
| REC-001 Model timeout / malformed | **PartialPass** | 七类稳定码 + 单一归类入口 + 429 `Retry-After` + 取消仍是 OCE 全绿；转写预算到期路径无用例（PLAT-001 自记 Deferred ③） |
| Timing / Behavior / Expression / Memory / Relationship / Capability | **NotRun** | `Pry.Core/Intelligence`、`Expression` 仍未接线；无真实模型运行；MEM-005/006 属 §5.0 冻结。本轮是工程门禁复跑，不产生这些类的 Pass |

Memory 三项统计本轮继续 NotRun，未填零（表见上一轮报告，未变）。

## New Scenarios

采纳并落号 13 条（字段齐全，映射均为真实存在且本轮通过的用例；`measure-fixed-eval.ps1` 会拒绝陈旧映射）。对 §8.1 草案表的映射与改动：草案 `PLT-003`（发现+鉴权合并）→ 拆成 `PLT-003` + `PLT-004`；草案 `PLT-004`（游标）→ 拆成 `PLT-005`（服务端义务）+ `PLT-006`（客户端义务），因为两者的负责人不同；草案 `PLT-005`（断连）→ `PLT-007`，并另立 `PLT-008`（取消 vs 关流）与 `PLT-009`（电平类时序），这两条是我上一轮的两条根因，落成永久判据防回退；新增 `PLT-012`（停机残留）、`PER-005`（schema 单一来源）。**草案 `PLT-006`（大媒体）改号为 `PLT-010`**，保持"编号只增不改义"。severity 上调一条：草案把发现+鉴权记 High，我拆出的 `PLT-004` 记 **Critical**——token 明文外泄属角色说明书第十二章的隐私红线，违反即阻塞，不由平均分抵消。

## Regression Results

```
dotnet build Pry.slnx -c Release            → 已成功生成，0 个警告 0 个错误
dotnet test Pry.slnx -c Release --no-build  → 通过，退出码 0
  --blame-hang-timeout 120s
Pry.Core.Tests   238 通过 / 0 失败 / 2 跳过（240）
Pry.Api.Tests    118 通过 / 0 失败 / 0 跳过（118）
Pry.Client.Tests  17 通过 / 0 失败 / 0 跳过（17）
Pry.App.Tests    162 通过 / 0 失败 / 0 跳过（162）
合计             535 通过 / 0 失败 / 2 跳过（537）
verify-repository.ps1 → 205 个受跟踪文件通过
git diff --check / --cached --check → 无输出（仅 `LICENSE` 的 LF→CRLF 提示，非缺陷）
measure-fixed-eval.ps1 → 退出 0；66 场景有效；50 PartialPass / 16 Unmapped
```

相对上一轮（459 用例、3 失败）：用例 +78，失败归零。跳过的 2 项仍是环境门控的真实二进制用例（`PRY_TEST_LLAMA_SERVER`/`PRY_TEST_LLAMA_MODEL`、真实语音权重），符合基线。本轮受影响文件指纹：`ConversationEventLog.cs` `a1a1971dcd54`、`MemoryDatabase.cs` `fcc21820fda5`、`CoreTests.cs` `6ae8d4156707`、`ProactiveSchedulerTests.cs` `b5a87620ed3a`、`ConversationReadEndpointTests.cs` `8831c2816622`（sha256 前 12，仅本轮快照有效）。工作区在这 12 小时里从 38+95 变成 52+121，任何结论都别外推到别的时刻。

## Runtime Findings

事件流读写重写为"到达队列 + 持久锚点"模型（`ConversationEventLog.cs:87-139`）：先取唤醒信号再读历史，避免"两者之间落库的事件既不在这一批也不再触发等待"的丢事件窗口；电平帧与持久事实相邻的两种顺序（`turn.state(AgentSending) → message.created` 与 `message.created → turn.state(Idle)`）都按锚点补齐后才消费。这是可观察行为的正确修法，且由 PLT-009 钉住。取消路径见 PLT-008。

主动投递的 Trigger→Candidate→Generate→Review→Send 五结点仍由 `ProactiveSchedulerTests` 九个用例覆盖（含返回栅栏、迟到输出丢弃、宿主停止不发布），本轮全绿。

## Behavior / Expression Findings

NotRun，理由同上：未接线、无真实模型。不因代码已落地而改判。

## Memory Findings

NotRun。QA-004（生产链无可达的自动记忆抽取）与 QA-005（检索降级与空结果不可区分）状态未变，属 Phase 5 前置，不阻塞 Phase 1。

## Critical / High / Medium / Low Issues

本轮无 Critical。

**QA-P2-01　High　Product defect（契约字段丢失 + 界面把不可重试错误当网络故障）**
最短复现：以高于服务端冻结 `major` 的客户端版本启动界面 → 界面显示"事件连接中断"并以固定间隔无限重连。
- 服务端正确宣告：`ProtocolNegotiationMiddleware.cs:46`、`SessionTokenMiddleware.cs:47` 都写 `retryable:false`；`docs/api-v1.md:27` 明文"这不是可静默降级或重试的错误：界面必须停在明确的版本不匹配提示，不能当作网络故障无限重连"。
- 断链点一：`src/Pry.Contracts/ApiContracts.cs:3-4` 的 `ApiProblemResponse` 只有 `Type/Title/Status/Detail/Code/TraceId`，**没有 `Retryable` 字段**；`PryBackendClient.cs:245-248` 因此解不出，`PryBackendException`（`:256-261`）也没有该属性。服务端发出的可重试语义在客户端边界被丢弃。
- 断链点二：两个事件流消费者——`src/Pry.App/Services/RemoteConversationTurnManager.cs:94-97`（`catch (Exception)` → 固定 500ms）与 `src/Pry.App/PryMessengerWindow.axaml.cs:486-502`（`catch (Exception)` → 固定 3000ms 后重启流，且每次都以 `after=-1` 重连）——无任何 `protocol_mismatch`/`unauthorized` 分支，`src/Pry.App` 全目录对 `UpgradeRequired` 零命中。
影响：升级窗口内（老界面连新核心）用户看到的是一个永不放弃的"连接中断"，而不是"请用同一发行版本启动"；`401`（令牌缺失/陈旧）走同一条路径。这正是角色验收意义上的"体验越来越像普通 Chatbot 的坏连接"。
Owner 分界：**字段丢失属 PLAT-005**（`Pry.Contracts` 是它的对象；补 `Retryable` 并让 `PryBackendException` 透传）；**停在明确界面属 UI-027**（`PryErrorPresenter`/`ConversationEventStreamRunner` 合入主工作区，按 `Code` 停止重试）。修复后 PLT-002 转 Pass，永久回归即 `PLT-002` 本身。
说明：这不是"UI-027 没做所以 Blocked"——禁行的那一半今天就能证伪，所以记 Fail 而非 Blocked。

**QA-P2-02　High　流程门槛未满足（唯一剩下的结构性阻塞）**
HEAD 仍是 `3613a2a`（一个素材提交），`git log main..HEAD` 无 Phase 1 代码提交；52 已修改 + 121 未跟踪。§5.0 把"形成可审查提交"写成阶段门槛。风险量级已变化：173 个文件、五个 Owner 的在途工作，任何单点提交或一次误 clean 都可能让另一方编译失败或成果不可恢复（`docs/eval/` 整体也未跟踪，我的 catalog 与本报告同样在风险内）。本轮三条根因全部源于同一机制：两方在未交汇的快照里各自漂移。

**QA-P2-03　Medium　Coverage gap（判据自称有回归，实际没有）**
`docs/protocol-versioning-and-websocket-migration.md` §8 第 1 条把"只绑回环"标为"现有行为回归"。实际：`src/Pry.Api/Program.cs:21-22` 有守卫，`tests/` 内无任何用例进入该分支（遍历 `127.0.0.1` 拒绝路径为零）。PLT-001 记 Blocked。修法是一行进程级断言，属 PLAT-005。

**QA-P2-04　Medium　已知缺口（Owner 自记，QA 复述以防被当作已完）**
PLAT-001 台账自列三项未做：①`PryErrorPresenter.Classify` 未与七类错误码同步扩类（等 UI-027）；②`LlamaHardwareDetector` 对子进程 `WaitForExitAsync` 无界且持单槽 `SemaphoreSlim`（`src/Pry.Core/Inference/LlamaHardwareDetector.cs:15,34,84`，一次挂起会毒化后续全部探测）；③转写预算到期路径无用例。②与 PLAT-001 完成标准"超时有界"同类，且调用方在真机启动路径上（`BackendRuntime.cs:92`、`ModelProcessRegistry.cs:39`、`LlamaServerManager.cs:37`）。据实结论：**PLAT-001 不能标"已完成"**，PLAT-005 也不能标"已完成"（§8 第 1/2/6/7/9 条各有一处未闭合）。

## User Feedback Converted to Tests

0；未收集。EVAL-002 仍只有模板，无样本。不以此代替质量验收。

## Release Blockers

Phase 1 结案前必需（顺序即建议顺序）：

1. **由集成负责人把 173 个文件的 Phase 1 批次形成统一可审查提交**，从该提交重跑 §7 五条命令并回填 `docs/phase1-platform-baseline.md` §6。这是唯一的结构性阻塞。
2. **QA-P2-01**：`ApiProblemResponse`/`PryBackendException` 透传 `retryable`，界面按 `protocol_mismatch`/`unauthorized` 停止重试并进明确提示（后半属 UI-027）。PLT-002 由 Fail 转 Pass。
3. **QA-P2-03**：补"非回环地址启动必须失败"的进程级断言（PLT-001）。
4. PLAT-001/005 的台账状态按 QA-P2-04 与上述两项收敛，不以"大体完成"入账。
5. §8 第 6/7 条（PLT-006/PLT-010）依 §5.0 与负责人分工分别记 Blocked（UI-027 / PLAT-007），不阻塞 Phase 1 的其余结案，但**阻塞"RT-001/PLAT-005 已完成"这两个标记本身**——是否允许带着两条 Blocked 结案，属作者/守门人的 §5.0 裁量，QA 不代为放宽。

## Deferred Eval

| 项 | 原因 | 依赖 | 触发条件 |
|---|---|---|---|
| PLT-012 残留子进程 | 只有环境门控的真实 `llama-server` 用例覆盖 | PLAT-002 | 设置 `PRY_TEST_LLAMA_*` 后复跑 |
| PLT-006 客户端重取、Runner 退避 | 统一重连宿主仍在另一 worktree | UI-027 | `PryErrorPresenter`/`ConversationEventStreamRunner` 进主工作区 |
| PLT-010 大媒体引用化 | §5.0 冻结项 | PLAT-007 | 解锁 Phase 7/8 |
| Timing P50/P95、Behavior/Expression/Memory 全类、Memory U/F/M | 未接线 + 无真实模型运行 | RT-003/004/006 接线、MEM-005/006 | §5.0 解锁对应阶段 |
| 真机端到端（Client 启动→连 Core→聊天→重启后历史仍在） | 本轮只到自动化与内嵌真实后端层，QA 未做界面实操验证 | RT-001 | 统一提交后由作者或 QA 在真机走一遍并记录 |
| 纵向内测 Day 1/2/7 | 无样本 | EVAL-002 | 可日常使用后邀请 2–3 位早期用户 |
