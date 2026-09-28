# Phase 2（Character Runtime / 触发层）验收复跑 · `eval-QA-P2TRIGGER-0923`

日期：2026-09-23（深夜）　验收人：QA / Evaluation（EVAL-001）　Owner 归属：Character Runtime Owner（RT-002）
被验对象：`docs/phase2-runtime-contract.md` §8.1 段②所要求的"从可指认提交的干净 worktree 复跑"
承载提交：**`b079321`**（tip，含 `8e039f9` 触发层 + `e6f0848` 状态行 + `8fe2be8` `DEF-P1WALK-02`）
作者口径：**本轮是验收复跑，不是 Phase 2 结案判定**——结案需要 §8.1 的 ②（本文件）与 ③（作者推进指针）两个动作，本文只签②。

> **2026-09-24 后续（本文判定保持不变，只标注现状）**：本文签的 `Fail` 由 `DEF-P2TRIGGER-02` 一条造成，Owner 已修并入库 `957002f`；同日 §7 第 1 步宿主端口的频率半边入库 `44e52fc`。段② 已在新载体上复跑为**全绿**，本文第 0 节表中判 `Blocked`（结构性）的第 ③ 条其"频率那一半"也已解除——**现行判定见 [`2026-09-24-phase2-closure-verification.md`](2026-09-24-phase2-closure-verification.md)**，本文保留作历史与归因证据。

## 0. 结论

**段②＝`Fail`（当前不可判 Pass），根因是一条测试竞态而不是产品行为。** Phase 2 出口四条里：① 成立、② 部分成立、③ **结构性不可判**（宿主端口未接，新链条在生产路径上进不去）、④ 因本条红而不成立。

一条新缺陷派工：**`DEF-P2TRIGGER-02`**（Owner：Character Runtime Owner / RT-002，严重度 High，判据类：测试竞态）。
一条文档级事实错误：**ACT-02 的锚点用例名拼错**（§6）。
许可三处留痕复查：**通过**（§7，供段④预检）。

| 判据（§8） | 判定 | 依据 |
|---|---|---|
| ① 上游一致性：§3 每节有判定、`Pending` 不默认消失 | **Pass** | §3.1–§3.15 共 15 处 `**判定…**`（实测 grep 计数 15），`Pending` 全部落 DEV-04…DEV-06 且状态为"开"，无删除行 |
| ② 合规：`DEF-P2TRIGGER-01` 有作者裁决 | **部分成立** | §5.1 已裁"暂不进发行默认"；逐值处置押后 ⇒ 契约自己写明"本条整体仍未成立"，本轮复核该表述与工作区一致 |
| ③ 行为：四条真机+真实模型证据 | **Blocked**（结构性） | `IsGroupChat` / `ReplyFrequency` 在 `src/` 内除 `Pry.Core/Runtime` 外**零生产者**（§4 实测），唯一宿主构造点不传参 ⇒ 新链条在发行默认下不可达 |
| ④ 集成：进入可指认提交且 QA 从干净 worktree 复跑门禁 | **Fail** | 提交面成立（`b079321` 干净 worktree），但门禁 1 红 ⇒ 该红未清之前不成立 |

## 1. 入场券四条（全部实测于本轮，非引用他轮）

载体 `.local-notes/p2-0923-verify`，`git worktree add --detach … b079321`，开工前 `git status --short` 为空。

| 项 | 命令 | 结果 |
|---|---|---|
| 构建 | `dotnet build Pry.slnx -c Release` | **0 警告 0 错误** |
| 测试 | `dotnet test Pry.slnx -c Release --no-build --logger "trx" --results-directory TestResults/gate-a` | Core `失败 1 / 通过 267 / 跳过 2 / 总计 270`；Client 92/0；App 182/0；Api 129/0 ⇒ **合计 673，1 红 2 跳** |
| 仓库校验 | `powershell -File scripts/verify-repository.ps1` | `Repository verification passed for 393 tracked files.`（exit 0） |
| 差异 | `git diff --check` | 干净 |

`scripts/measure-fixed-eval.ps1 -ResultsDirectory TestResults/gate-a`：`Passed=670; Failed=1; Other=2`，**exit=1**。

按 `PROJECT_PLAN.md` §7 末段纪律：**exit=1 的那一轮，其绿灯数字不得被引用为"门禁绿"**。上表 670 只是计数事实。
（覆盖统计 `53 PartialPass / 15 Unmapped` 是 catalog `1.1.2` 时点的产出，本文 §5 之后目录已升到 `1.2.0`，该组数字随之过期，不再引用。）

## 2. 唯一的红：`DEF-P2TRIGGER-02`

```
失败 Pry.Core.Tests.RuntimeTriggerIntegrationTests.A_backlog_below_the_threshold_wakes_itself_without_a_new_message
   Assert.Contains() Failure: Filter not matched in collection
   at ...RuntimeTriggerIntegrationTests.cs:line 88
```

该用例正是 §9 草案 `P2-Draft-TIM-02` 的主锚点，也是目录新立 **TIM-009** 的唯一载体——即：**这条红现在会体现在 Eval 覆盖里，不再只是"某个用例红了"。**

### 2.1 复跑率（不是确定性红，也不是纯偶发）

| 跑法 | 次数 | 红 |
|---|---|---|
| 解决方案级（本文 §1 那次 + 本轮先前 2 次） | 3 | **3** |
| 程序集级 `Pry.Core.Tests` 全量 | 3 | **3** |
| 单测隔离 `--filter …A_backlog_below…` | 10 | **6** |

第十轮在同一 tip 上曾有过 5 次 671/0/2 的全绿 ⇒ 该用例**能过**，因此这是一条**负载相关的竞态**，不是恒红。上面三行合起来给出的区间是"隔离约 60% 红、并行满载接近 100% 红"，我只报观测次数，不报拟合概率。

### 2.2 机制（已定位到边界算术，不是猜）

同一条用例的两个分支，轨迹（`Runtime.TraceEntries`，本轮实测抓取）分别是：

```
… Event:buffered … Gate:trigger:Backoff … Event:turn_due … Gate:trigger:Run … Turn:formed …   ← 通过
… Event:buffered … Gate:trigger:Run … Turn:formed …                                          ← 失败
```

红的那次**从没产生过 `trigger:Backoff`**：门控第一次评估就直接成轮了。原因是这条用例把自己放在了门控判据的边界上：

- `tests/Pry.Core.Tests/RuntimeTriggerIntegrationTests.cs:68-76`：`RuntimeSettingsFactory.Fast()` 的 `DebounceMs = 40`（`tests/Pry.Core.Tests/RuntimeFakes.cs:309`），`Intervals.MinimumAverage = 20 ms`、`BurstFloor = 1 ms`，`replyFrequency = 0.25` ⇒ 计数模式阈值 `ceil(1/0.25) = 4`；
- 空窗补偿（`src/Pry.Core/Runtime/TurnGates.cs:99-111`）：折算量封顶 `threshold-1 = 3`，`pending = 1` ⇒ 首次评估要触发 `Trigger` 需要 `idle ≥ 3 × 20 ms = 60 ms`，否则返回 `Delay`，由 `TurnScheduler.cs` 译成 `Backoff("idle_compensation")`；
- 首次评估发生在 debounce 窗关闭之后，即 `idle ≈ 40 ms + 定时器实到延迟`。**判定分支只取决于这 40→60 ms 之间约 20 ms 的墙钟余量**，而时钟是 `TimeProvider.System`（`tests/Pry.Core.Tests/RuntimeFakes.cs:349`），不是假时钟。满载并行时 20 ms 的定时器迟到是常态 ⇒ 红。

### 2.3 这条红的性质：判据没证到它声明要证的事

用例注释（`:66-67`）写明它要证的是"没有空窗补偿这条消息会永远挂在缓冲里"。而在红的分支里**根本没有发生自醒**——门控第一眼看够就成轮了，后面的 `composer.Calls == 1` 与 `["woken"]` 是因为另一条路径成立而顺带通过的。所以：

- **不是产品行为缺陷**：两个分支都符合契约（`§3.5` 的空窗补偿与双保险都在正常工作，纯沉默仍不成轮）；
- **是测试缺陷**：`trigger:Backoff` 这条断言依赖真实墙钟上的 20 ms 裕量，且当它失败时，该用例声称的"自醒"这件事今天**没有任何一条用例在稳定地守着**（TIM-009 的判据本身没问题，问题在载体）。

这与 `QA-P6-01`、`DEF-P1WALK-03/04` 是同族第三例：**把采样点/断言放在启动或定时器竞争的窗口里。**

### 2.4 反向约束（不许走的捷径）

1. **不许改判据或改阈值让它变绿**（§8.2 明列；Phase 1 的 ≤55 s 档由产品去够是先例）。TIM-009 的 `expectedDecision` 我已按契约写成"到点自行醒来成轮"，不随载体松动。
2. **不许把断言放宽成 `trigger:Backoff` 或 `trigger:Run` 二选一**——那等于承认这条用例不再检查自醒。
3. **不许靠加大 `WaitUntilAsync` 超时或加 `sleep` 压竞态**（`QA-P6-01` 的第一版修法已被证伪过一次）。
4. 可选方向（**由 Owner 定，QA 不代做**）：(a) 该 harness 换成可注入时钟，让"首次评估时刻"与"补偿所需 idle"成为可控量而非竞争量；(b) 保留墙钟但把 `MinimumAverage` 提到使 `3 × avg` 远大于 `DebounceMs + 定时器迟到上界` 的量级（例如 200 ms 档 ⇒ 需 idle ≥ 600 ms），让分支选择退出 20 ms 灰区。

## 3. §8.1 四段链的当前位置

| 段 | 谁签 | 本轮判定 |
|---|---|---|
| ① 自证 | Owner | 已交（`8e039f9` + `e6f0848`，状态词 `已提交待验`） |
| ② 复跑 | QA | **本轮签 `Fail`**：门禁 1 红（§2），且该红正是 §9 交接的 TIM-02 锚点。清掉 `DEF-P2TRIGGER-02` 后可复跑；本文其余部分（§3–§7）本轮已给出可复用的判定，无需重做 |
| ③ 裁决 | 作者 | 未发生。待裁：`DEF-P2TRIGGER-01` 逐值处置、`DEV-02/03/04`、§7 第 1 步（宿主端口）是否排进下一刀 |
| ④ 发行 | Release | 未开始 |

**Phase 1 侧的连带影响（必须记）**：Phase 1 现在唯一未过的出口条件是"集成基线并行连续 ≥2 次全绿"，本轮这条红**同样落在该门禁里**，与 `DEF-P1WALK-03/04` 并列成为第三个竞争者。三者的 Owner 不同（PLAT / Memory+PLAT / Runtime），但**结案动作是同一个**：三条都清完才可能连绿。

## 4. "发行默认下整条链仍是惰性分支"——独立复核，非引用文档

`docs/character-runtime.md` 与契约 §7 第 1 步这么写，我不采信转述，按符号面复核：

- `grep -rn "IsGroupChat\|ReplyFrequency" src/` ⇒ 命中全部落在 `src/Pry.Core/Runtime/**`（`ConversationRuntime.cs:17,23,488,636-637`、`ReplyNecessity.cs:123,188`、`TurnGates.cs:42`、`TurnScheduler.cs:8,131,134,143`）与编译产物；**Api/App/Client 侧零命中**；
- `ConversationRuntimeOptions(` 在 `src/` 内只有一个真实构造点：`src/Pry.Api/Services/ConversationSessionService.cs:280`，其入参未传 `IsGroupChat` / `ReplyFrequency` ⇒ 取 record 默认（`false` / `1.0`）；
- `TurnScheduler.cs:131` 的 `focusActive: false` 与契约 DEV-06 一致。

⇒ **判定：契约的这条自我陈述成立**。因此 §9 里"今天可跑＝否"的五行（TIM-04、ACT-02、ACT-04、PER-01、PER-03）**不是缺陷**，登记为 `Blocked` + 前置 §7 第 1 步，符合契约 §9 提醒第 2 条。生产路径的既有行为未变这一点，也与"244 条 Core 用例无一改动"的自述不冲突（本轮总数 270，跳 2）。

## 5. §9 草案 15 行的采纳裁决（永久编号归 QA，本 Owner 不自封）

目录 `docs/eval/scenarios.json`：**`suiteVersion` 1.1.2 → 1.2.0**（新增永久 ID ⇒ 语义修订，按 `docs/eval/README.md` 维护约定升版本；68 → 74 条，**无删除、无复用、无改判据**）。升降级说明：本版只把触发层的既有自动化证据纳入永久目录，未改动任何历史场景的语义。

| 草案 ID | 处置 | 落点 / 理由 |
|---|---|---|
| `P2-Draft-TIM-01` | 追加证据 | 现有 **TIM-001** 已有主锚；`Inputs_at_zero_fifty_and_three_hundred_fifty_ms_form_one_turn` 补为第二证据（纯映射追加） |
| `P2-Draft-TIM-02` | **新立两条** | **TIM-009**（攒不够阈值自己醒来，载体即 §2 的红）＋ **TIM-010**（纯沉默多久都不成轮）。草案把一正一反两个判据捆在一行，目录侧必须拆开，否则载体红了会连坐正确的那一半 |
| `P2-Draft-TIM-03` | 部分采纳 | `Repeated_typing_reports_renew_the_lease…` 追加进现有 **TIM-002**；半句/封顶已由 TIM-003 覆盖；`Immediate_flag_short_circuits_the_buffer` 语义是"即时标志短路缓冲"，与现有任何场景都不吻合 ⇒ **不硬塞**，留待下轮随 `RuntimeComponentTests` 一起评 |
| `P2-Draft-TIM-04` | **新立** | **TIM-011**（群聊空转指数退避：封顶 / 积压越窗 / 答话清零，三条绿载体）。"今天可跑＝否"指的是**产品路径**，单元证据是真的 ⇒ 目录 `acceptance` 仍记 `NotRun` |
| `P2-Draft-ACT-01` | **新立** | **BEH-008**（频率为 0：照常落库、零调用）。类别选 Behavior 不选 Proactive——目录里 `ACT` 前缀是 Proactive，这条是被动回合的门控 |
| `P2-Draft-ACT-02` | **新立** | **BEH-009**（群聊必要性模式未被点名不开口）。载体名以真实拼写 `Group_chatter_…` 入目录，见 §6 |
| `P2-Draft-ACT-03` | 追加证据 | 现有 **BEH-007**（自然结束与简单确认）此前 `automatedTests: []`，本轮第一次有载体：`Reply_gate_is_allowed_to_stay_silent` + `Reply_gate_returns_do_nothing_as_a_first_class_result`（DoNothing 一等结果正是它的 `expectedDecision`） |
| `P2-Draft-ACT-04` | **暂不立** | 契约自评 `前置＝标定（DEV-03）`，而 §5.1 作者已把逐值处置**押后并禁止本 Owner 代拍**。此时立永久号＝把一组未标定数值冻结成判据，等 §5.1 落地后再立 |
| `P2-Draft-REC-01` | 追加证据 | 现有 **TIM-004** 已覆盖；`Repeated_interruptions_stack_up_without_delivering_any_stale_reply` 补为第二证据 |
| `P2-Draft-REC-02` | 已覆盖 | 现有 **TIM-006** 的锚点就是它，无需动作 |
| `P2-Draft-REC-03` | 已覆盖 | 现有 **REC-002** 的锚点就是它，无需动作 |
| `P2-Draft-REC-04` | **暂不立** | 契约自己写"无判据"。无判据不占号（ID 永不删除，占号会永久挂一条无法评的场景）；待 DEV-11 + Persistence 给出可观察判据后再立 |
| `P2-Draft-PER-01` | **暂不立** | 无自动化、无真实模型档位、且需 §7 第 1 步。判据"静默轮为 0"目前只在假时钟下可指认 |
| `P2-Draft-PER-02` | **新立（半条）** | **TIM-012** 只收"间隔来自阅读时长、不得退化成固定字符切分"这一半（有绿载体）；P50/P95 那一半按契约提醒第 1 条**必须照填模型档位**，留到真实模型走查那次再立，不与本条混在一个 ID 里 |
| `P2-Draft-PER-03` | **暂不立** | 需标定 + 群聊端口，且"显著低于 100"还不是可判据数字 |

**给 Owner 的前缀冲突提醒（必须记，否则会二次踩）**：草案的四组前缀 `TIM/ACT/REC/PER` 与永久目录的同名前缀**语义不同**——目录里 `ACT` = Proactive、`PER` = Persistence、`REC` = Recovery。采纳时一律按语义重排（本轮 `ACT-01/02 → BEH`、`PER-02 → TIM`），**不得照抄 `P2-Draft-*` 的名字铸号**。

## 6. 契约文档级事实错误（Owner 修，文档不改代码）

§9 表 `P2-Draft-ACT-02` 行的锚点写作 `Group_chater_stays_quiet_in_necessity_mode_until_the_character_is_called`，实际用例名是 `Group_chatter_…`（`RuntimeTriggerIntegrationTests.cs:40`）。该行表头自称"现有自动化锚点（本轮逐条回源核过名）"——我把 22 个锚点全部回源核过，**21 真 1 假**，假的就是这一处。影响面：照抄该锚点入目录会让 `measure-fixed-eval.ps1` 抛 `Stale test mapping`（脚本按方法名在源文件里实搜）。我在目录侧已用真实拼写落账，契约侧那行请 Owner 就地改正并保留一处"曾写错"的痕迹，不要静默覆盖。

## 7. 许可三处留痕复查（段④预检，本轮顺手做完）

`docs/upstream-license-compatibility.md` §4.2/§4.4 要求的三处齐备，实测：

1. `licenses/THIRD-PARTY-NOTICES.md:71-75` 共 5 行——`MessageIntervalStats.cs` / `ReplyNecessity.cs` / `TurnGates.cs` / `IdleBackoffController.cs` 记 `ported`，`TurnScheduler.cs` 记 `mechanism`，全部指到 `Mai-with-u/MaiBot@fb4a8bd9…` 的具体文件与函数；
2. `licenses/upstream/MaiBot_fb4a8bd917d6fe373df9e3c22289d229e7f1b344/LICENSE` 在仓；
3. 上述 5 个源文件头部均有来源行（`grep -l "MaiCore\|fb4a8bd9"` 命中，另有 `RuntimePorts.cs`）。

另核 §4.3 第 4 条：上游类型未进 `Pry.Core.Abstractions`/领域层，与 DEV-07 的"已满足"一致。

## 8. 本轮我**没有**做的事（边界声明）

- 未修改任何产品代码或测试代码——`DEF-P2TRIGGER-02` 只派工不代修；§2.4 列的修法是我能想到的方向，不是建议 Owner 照做的方案。
- 未推进 §5.0 阶段指针（Phase 1 仍"未结案 + Phase 2 推进中"），未替作者裁 §5.1 押后的两条路线。
- 探针只在一次性 scratch worktree 里做过，用完 `git restore` 该单文件复原；主工作树只有我自己名下的 `docs/eval/**` 有改动。
- **证据载体暂时保留**：`.local-notes/p2-0923-verify`（`git status` 干净）连同其 `TestResults/gate-a`（本轮 4 个 TRX + `fixed-eval-coverage.json`）先不删，供 Owner 复算 §2 的分支边界；`DEF-P2TRIGGER-02` 结案后我用 `git worktree remove` 清掉，不做静默留存。

## 9. 待作者裁定（本轮新增/仍在）

1. **`DEF-P2TRIGGER-02`** 派工确认（Owner：Character Runtime Owner），以及它与 `DEF-P1WALK-03/04` 三条并列后 Phase 1"≥2 次全绿"的结案次序。
2. `DEF-P2TRIGGER-01` 逐值处置（§5.1 押后项）与 §7 第 1 步"宿主端口"是否排进下一刀——**它决定 §8 第 3 条能否从 `Blocked` 变成可判**。
3. 旧项仍在：`DEF-P1WALK-01`（第六次复现，至今无归属）、`QA-P6-03`、`QA-P6-04`。

## 10. 复现命令

```powershell
# 载体：tip 的干净 --detach worktree
git worktree add --detach .local-notes/p2-0923-verify b079321
cd .local-notes/p2-0923-verify
dotnet build Pry.slnx -c Release
dotnet test Pry.slnx -c Release --no-build --logger "trx" --results-directory TestResults/gate-a
powershell -File scripts/measure-fixed-eval.ps1 -ResultsDirectory TestResults/gate-a   # 当前 exit=1
powershell -File scripts/verify-repository.ps1
git diff --check

# 竞态复现率（本用例，隔离 10 次）
dotnet build tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release
1..10 | ForEach-Object {
  dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --no-build `
    --filter "FullyQualifiedName~A_backlog_below_the_threshold_wakes_itself" 2>&1 |
    Select-String -Pattern '失败!|已通过!'
}
```

**QA 侧本轮踩到的一条自查**（记进台账，防止下轮自我误判）：上面第二条命令必须带 `--no-build` **且中途不改源码**。我第一遍在探针版与原始版之间改过文件后仍用 `--no-build` 跑了 5 次，得到"5 次全红且轨迹里明明有 `trigger:Backoff`"的矛盾读数——那是旧二进制。重建成原始版本后测得的是 10 次 6 红。**矛盾读数优先当作自己的仪器故障来查，再当作被测对象的性质。**
