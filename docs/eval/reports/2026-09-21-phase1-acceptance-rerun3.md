# Phase 1 验收判定 · 第三轮

Run ID: `eval-QA-P1ACC-0921C`　日期: 2026-09-21 15:56–16:15 本地（含 16:06 自查更正）
前序: `eval-QA-P1ACC-0921`（02:41，456/3/2，不通过）→ `eval-QA-P1ACC-0921B`（14:58，535/0/2，不通过）→ 本轮
候选: 分支 `codex/ui-main-window-redesign`，HEAD **`f1c5118`**（`docs: relicense code under GPL-3.0-only…`；本轮期间从 `3613a2a` 前进过一格，内容是许可与研究文档，**不含任何 Phase 1 代码**）
工作区: 仍 dirty——49 已修改 + 121 未跟踪；`verify-repository.ps1` 现覆盖 **214** 个受跟踪文件（原 205，`docs/research/` 六份随 `f1c5118` 入库）

**结论：Phase 1 仍不通过。** 阻塞面已收窄到两条：一条产品行为（`PLT-002` = Fail，界面把不可重试错误当网络故障无限重连）与一条流程条件（Phase 1 代码批次仍无统一可审查提交）。上一轮我提的另外两条（`PLT-001` 无回归、契约字段丢失）本轮已闭合。

> **16:06 更正**：本文件原判 QA-P2-04② "本轮无改动"是错的——该缺陷在跑测时已经修好，我把第二轮的结论直接沿用了。修正后为"② 已闭合，①③ 仍开"，**判定结论与 Release Blockers 顺序不变**。详见文末"更正记录"。

## Regression Results

```
dotnet build Pry.slnx -c Release           → 已成功生成，0 个警告 0 个错误
dotnet test Pry.slnx -c Release --no-build → 通过，退出码 0（--blame-hang-timeout 120s）
  Pry.Core.Tests   242 通过 / 0 失败 / 2 跳过（244）
  Pry.Api.Tests    123 通过 / 0 失败 / 0 跳过（123）
  Pry.Client.Tests  19 通过 / 0 失败 / 0 跳过（19）
  Pry.App.Tests    162 通过 / 0 失败 / 0 跳过（162）
  合计             546 通过 / 0 失败 / 2 跳过（548）
verify-repository.ps1                      → 214 个受跟踪文件通过（仍不覆盖 170 个未提交文件）
git diff --check / --cached --check        → 无输出
measure-fixed-eval.ps1                     → Catalog valid: 66；Passed=546; Failed=0; Other=2；
                                             51 PartialPass / 15 Unmapped；missing mappings = 0
```

相对第二轮（537 用例）：+11（Api +5、Client +2、Core +4），失败仍为 0，跳过仍是同样两项环境门控的真实二进制用例。本轮 TRX 与证据目录：`TestResults/phase1-acc-0921c/`。目录里没有任何"映射指向不存在的测试"——`measure-fixed-eval.ps1` 会拒绝陈旧映射，这本身就是上一条整改的护栏。（16:1x 因目录追加 `REC-005` 在同一 TRX 上重跑脚本：Catalog valid **67**、52 PartialPass / 15 Unmapped、缺失映射 0、退出仍 0；测试计数不变。）

## 第二轮判定的闭环核对

**QA-P2-01 前半（契约字段丢失）→ 已修，且修法正确。** `ApiProblemResponse` 现在带 `retryable`，并且用 `bool?` 而不是 `bool`：字段缺席＝"服务端没宣告"，与"宣告了不可重试"是两回事（`src/Pry.Contracts/ApiContracts.cs:3` 起的注释）。`PryBackendClient.EnsureSuccessAsync` 原样透传，不替服务端猜（`:245-249`），`PryBackendException` 多一个 `Retryable`（`:257`）。用例新增两条把它钉住：`The_ban_reaches_the_stream_subscribers_that_do_the_reconnecting`（重连的那一层确实拿得到禁令）与 `An_absent_retryable_is_not_invented_as_a_ban`（缺席不当成禁）。**这一档我认可，不再追加要求。**

**QA-P2-01 后半（界面无限重连）→ 未修，仍是 Fail。** `src/Pry.App` 全目录对 `Retryable`/`UpgradeRequired`/`protocol_mismatch` 仍是零命中；两个事件流消费者原样：`RemoteConversationTurnManager.cs:94-97` 用 `catch (Exception)` → 固定 500ms 重试并报"正在重连"，`PryMessengerWindow.axaml.cs:486-502` 用 `catch (Exception)` → 固定 3000ms 后重启流且每次以 `after=-1` 重连。可观察行为未变：老界面连新核心时用户看到的是永不放弃的"连接中断"，而 `docs/api-v1.md:27` 要求界面停在明确的版本不匹配提示。**信号现在已经在 `PryBackendException.Retryable` 上了，剩下的只是界面读它**——归属 UI-027（`PryErrorPresenter`/`ConversationEventStreamRunner`），且因缺的不是数据而是分支，我继续记 Fail 而不是 Blocked。

**QA-P2-03（只绑回环无回归）→ 已修。** 新增 `tests/Pry.Api.Tests/LoopbackBindingTests.cs`：`A_bind_address_outside_loopback_refuses_to_start`（`[InlineData]` 覆盖 `http://0.0.0.0:5078` 等）+ `A_loopback_bind_address_is_not_refused_by_that_guard`（正反两向，避免守卫写成永真）。`PLT-001` 由 Blocked 转 **Pass**，两条映射已写进 catalog。

**QA-P2-04（PLAT-001 自记三项）→ 两项实质变化，一项未动。** ①`PryErrorPresenter.Classify` 与七类错误码同步扩类：仍等 UI-027（同上一条同一个收敛点），`src/Pry.App/Services/` 下今日仍无 `PryErrorPresenter`/`ConversationEventStreamRunner`；②`LlamaHardwareDetector` 对子进程等待无界且持单槽 `SemaphoreSlim` —— **本轮已修，我原判"本轮无改动"有误，见文末更正记录**；③转写预算到期路径（4 分钟 linked CTS → `TimeoutException`）确仍无用例：`tests/Pry.Client.Tests/BackendRequestBudgetTests.cs` 只有两条静态预算形状断言与一条"取消仍是 `OperationCanceledException`"，到期分支本身没跑过。

## Coverage 变化（其余判定沿用第二轮）

| 场景 | 第二轮 | 本轮 | 说明 |
|---|---|---|---|
| PLT-001 | Blocked | **Pass** | 正反两向进程级断言落地 |
| PLT-002 | Fail | **Fail** | 契约侧已通，界面侧未读；缺的是分支不是数据 |
| PLT-003/004/005/007/008/009/011 | Pass | Pass | 映射断言本轮仍全绿，无回归。**`PLT-003` 的 Pass 已被第四轮降级为 Blocked**（客户端半片无实现无证据，见 `2026-09-21-phase1-acceptance-rerun4.md` 的"自我更正（第二处）"） |
| PLT-006 | Blocked | Blocked | 统一重连宿主仍未进主工作区（负责人 UI-027） |
| PLT-010 | Blocked | Blocked | 大媒体引用化属 PLAT-007，§5.0 冻结中，不属 Phase 1 缺陷 |
| PLT-012 | NotRun | NotRun | "无残留 `llama-server` 子进程"只在环境门控的真实用例里，本轮仍 Skip，不以相邻绿灯代填 |
| PER-005 / REC-001 | Pass / PartialPass | 不变 | 前滚与七类码无回归 |
| Timing/Behavior/Expression/Memory/Relationship/Capability | NotRun | NotRun | `Intelligence`/`Expression` 仍未接线、无真实模型运行，本轮仍是工程门禁，不产生这些类的 Pass |

Memory 三项统计继续 NotRun，未填零；QA-004/QA-005 状态未变（Phase 5 前置，不阻塞 Phase 1）。

## New Scenarios

本轮正文不新增 ID（仅把两处新落地断言登记为证据映射：`PLT-001` ← `LoopbackBindingTests` 两条；`PLT-002` ← 两条 retryable 客户端用例）。既有 66 条的 Scenario/约束/禁行结果一字未改，`suiteVersion` 保持 1.1.0（纯映射追加）。**文末更正记录里另有一条形成的追加：复查 ② 时发现探测挂起这类行为无场景承载，故新增 `REC-005`，目录 66 → 67、`suiteVersion` → 1.1.1。**

## Runtime Findings

事件流的"到达队列 + 持久锚点"模型在客户端边界之外没有再变动；本轮新增可观察行为是**错误语义贯通到订阅层**：426/401 的不可重试判定现在能到达"真正负责重连的那一层"，但那一层（界面）还没接。半开检出、心跳、游标跨重启、电平类不重放四项与第二轮一致全绿。

## Critical / High / Medium / Low Issues

本轮无新增缺陷，无 Critical。存量：

- **QA-P2-01（High，Product defect，部分闭合）** — 见上。修复后 `PLT-002` 转 Pass，永久回归即 `PLT-002`。
- **QA-P2-02（High，流程门槛）** — Phase 1 代码批次仍无统一提交：49 改 + 121 未跟踪。风险已部分缓解（研究文档与许可已随 `f1c5118` 入库，tracked 覆盖数 205→214），但**代码与 `docs/eval/` 全量仍未跟踪**：我的 catalog（67 场景）、三份运行报告、`scripts/measure-fixed-eval.ps1` 都不在版本控制里，任何一次误 clean 就不可恢复。§5.0 把可审查提交写成阶段门槛，QA 侧维持原判。
- **QA-P2-04（Medium，判据形态单一）** — 更正后的存量：② 已闭合（`RunProbeAsync` 统一 20s/10s 预算 + 到期 `Kill(entireProcessTree: true)`，`HardwareProbeTimeoutTests` 4 例覆盖"挂起不传染下一次探测"与"取消仍以 `OperationCanceledException` 浮出"）。保留的观察是判据设计层面的：`BoundedWaitBoundaryTests` 以源码扫描禁 `Timeout = Timeout.InfiniteTimeSpan`，方向正确但只覆盖 HTTP 一种形态（该文件正则即 `ExplicitlyInfinite`，无 `WaitForExitAsync` 分支），子进程等待这一类今天靠 4 条行为用例守着、不靠扫描。这条是建议不是缺陷，留给 PLAT-001 Owner；①③ 仍按上文记开。

## User Feedback Converted to Tests

0；未收集。EVAL-002 仍只有模板与 0 样本，不以模板代替真实反馈。

## Release Blockers（Phase 1 结案前必需，顺序即建议顺序）

1. **QA-P2-01 后半**：`src/Pry.App` 两个事件流循环读 `PryBackendException.Retryable`/`Code`，对不可重试错误停止退避并进入明确界面状态（可与 UI-027 的 Runner 合并做，也可先补分支）。→ `PLT-002` 转 Pass。
2. **统一可审查提交**：把 Phase 1 代码批次（含 `docs/eval/` 与 `scripts/measure-fixed-eval.ps1`）由集成负责人形成提交，从该提交重跑 §7 五条命令并回填 `docs/phase1-platform-baseline.md` §6。本轮 546/0/2 与第二轮 535/0/2 都只描述各自快照。
3. **PLAT-001/005 台账状态按 QA-P2-04 收敛**：①（UI-027 依赖）与③（转写预算到期无用例）未做前不标"已完成"；② 本轮可结案。
4. `PLT-006`（UI-027）与 `PLT-010`（PLAT-007）如实保持 Blocked。它们不阻塞 Phase 1 其余结案，但**阻塞"RT-001 / PLAT-005 已完成"这两个标记本身**；是否允许带两条 Blocked 结案属作者/守门人的 §5.0 裁量，QA 不代为放宽。
5. 真机端到端（Client 启动 → 连 Core → 聊天 → 重启后历史仍在）本轮仍只到"内嵌真实后端的自动化"层，QA 未做界面实操验证；建议在统一提交之后由作者或 QA 走一遍并记录。

## Deferred Eval

沿用第二轮清单，无变化：PLT-012 残留子进程（待 `PRY_TEST_LLAMA_*`）、PLT-006（待 UI-027）、PLT-010（待 PLAT-007 解冻）、Timing P50/P95 与 Behavior/Expression/Memory 全类（待接线 + 真实模型）、纵向内测 Day 1/2/7（0 样本）。

## 更正记录（2026-09-21 16:06，QA 自查）

**改了什么**：把 QA-P2-04②（`LlamaHardwareDetector` 子进程等待无界 + 单槽 `SemaphoreSlim`）从"本轮无改动"更正为**已闭合**，并据此调整 Release Blockers 第 3 条（②③ → ①③）与 Medium 段落。**判定（Phase 1 仍不通过）、覆盖率表、`PLT-002` 的 Fail 与其余各条均不变。**

**错在哪**：第二轮（14:58）我读到的 `LlamaHardwareDetector.cs:15,34,84` 确实是"单槽 `Gate` + 无界 `WaitForExitAsync`"。本轮我沿用了那条结论去写"② 未动"，**没有在该轮重新打开文件**——而引用的行号 `:15,34,84` 在本轮工作区里已经指向别的东西（`:15` 是 `Gate` 声明、`:22/:25` 是 20s/10s 预算常量、`:44/:99` 都走 `RunProbeAsync`）。这是"把上一轮快照的结论当本轮证据"的失误，恰好是我自己在报告里反复警告别人的那种失误。

**证据（本轮跑测里本来就有）**：Core 用例数 238 → **242**，多出的 4 例正是 `tests/Pry.Core.Tests/HardwareProbeTimeoutTests.cs`（预算内切断、**挂起不传染下一次探测**、到期杀整棵树、取消仍是 `OperationCanceledException` 且随后探测照常可用），本轮全绿；调用方 `BackendRuntime.cs:92`、`ModelProcessRegistry.cs:39`、`LlamaServerManager.cs:37` 三条真机启动路径现在拿到的是有界探测。也就是说：**我执行的那次门禁跑测已经包含了这项修复的判据，而我在同一份报告里写了它不存在。**

**同时复核了两项未被质疑的**：① `PryErrorPresenter`/`ConversationEventStreamRunner` 在 `src/Pry.App/Services/` 下仍不存在（UI-027 未合流，成立）；③ `BackendRequestBudgetTests.cs` 只有两条预算形状断言加一条取消路径用例，转写预算**到期**分支确仍无用例（成立）。

**给后续复核的约束**：跨轮沿用任何一条结论前，必须在本轮工作区重新确认被引用行号仍指向所描述的代码；行号漂移即视为该结论失效，需重读。本轮报告由实现方 §9 记录（`PROJECT_PLAN.md` 同日的"QA-P2 复核轮"）触发复查。

### 更正触发的目录追加（16:1x，`suiteVersion` 1.1.0 → **1.1.1**，永久 ID 66 → **67**）

复查 ② 时发现一个我两轮都没登记的覆盖缺口：那 4 条 `HardwareProbeTimeoutTests` 断言的"外部进程挂起会饿死后续探测 / 只杀外壳留孤儿 / 把取消与超时混成一种"三类可观察行为，**在永久场景集里没有任何场景承载**——此前 `REC-001` 只管 Provider 请求的有界失败，`PLT-*` 只管协议地基。修它的判据因此只属于 Owner 的测试文件，删掉不会有任何目录级信号。这正是"永久场景不得删除"要防的形态，故补一条：

- **`REC-005`（Recovery / High）"外部探测进程挂起时启动路径有界且可恢复"** — 输入为"挂起期间再触发下一次探测"与"调用方主动取消"两条；禁行结果＝挂起永久占住单槽闸门、只杀外壳留孤儿孙进程、把取消静默降级成超时或反之。四条测试全部登记为证据映射（`automatedTests` 是部分证据，不等于场景验收）。

既有 66 条的 Scenario/约束/禁行结果仍一字未改；`REC-005` 的 `acceptance` 与其它一样留 `NotRun`。重跑 `measure-fixed-eval.ps1 -ResultsDirectory TestResults/phase1-acc-0921c`（同一轮的既有 TRX，未混跑）：**Catalog valid: 67；Passed=546; Failed=0; Other=2；`REC-005` = PartialPass（缺失映射 0）**，52 PartialPass / 15 Unmapped，退出码 0。

> 提醒一个读法陷阱：脚本对 `PLT-002` 也给 `PartialPass`，因为它只看映射到的断言是否全绿。`PLT-002` 的**场景判定仍是 Fail**——不可重试错误抵达订阅层这一半有测试，"界面停在明确提示"这一半没有测试也没实现，脚本看不到这一点。
