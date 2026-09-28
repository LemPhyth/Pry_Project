# Phase 2 结案标准复验 · 2026-09-26

- **Run ID**：`eval-QA-P2CLOSE2-0926`
- **判定人**：QA / Evaluation（EVAL-001）
- **载体**：`44e52fc`（`feat(runtime): put the reply-frequency gate behind a real host port`）
- **取证 worktree**：`.local-notes/p2-0926-verify`，`git worktree add --detach 44e52fc` 新建，入场即 `## HEAD (no branch)` 且 `git status --short` 除构建产物外为空
- **与上一轮的关系**：`eval-QA-P2CLOSE-0924` 的**同一载体**。本轮不引用该轮任何数字，全部读数当场重采；本轮的增量在于（a）连绿样本加厚并做一次受控并发负载，（b）核对 09-24 之后未提交面的移动，（c）暴露一条 QA 自己测量资产上的缺陷。
- **本轮零产品代码、零产品测试改动**（QA 只在 worktree 里跑；主工作树的在途改动一字未动）。

---

## 1. 出口判据逐条给词（`docs/phase2-runtime-contract.md` §8）

| # | 判据 | 判定 | 依据（全部当场实测或当场实读） |
|---|---|---|---|
| ④ | 集成可指认提交 | **Pass** | 见 §2 读数表：build 0 警告 0 错误、**解决方案级 11 次跑法全绿、零红例**（每次 675/0/2）、`verify-repository.ps1` 394 个受跟踪文件通过、`git diff --check` 干净、`measure-fixed-eval.ps1` 三份 TRX 各 **exit=0**（Passed=675 / Failed=0 / Other=2） |
| ③ | 真实模型节奏下的行为证据 | **Pass，带两处明写上限** | 上限一：宿主仍注入过渡实现——`src/Pry.Api/Services/ConversationSessionService.cs:286` 的 `new TransitionalReplyAdvisor()`（本轮在载体上重新 grep 确认），故今天的"可不回复"成立的是**频率端口**，不是社交判断；上限二：内容侧不可评——`DEF-P1WALK-01`（思考信封被当答复）在 `src/` 内仍零命中（本轮按 `</thinking>` / `<RESPONSE>` / thinking 三种形态 grep，命中的只有思考模式开关 UI 与推理参数，无剥离逻辑） |
| ① | 上游一致性 | **Pass（仅凭作者裁定成立；使裁定可审计的文本不在载体上）** | 载体上的契约仍是旧态：§6 的 `DEV-03/04/05/06` 四行都还是 `Pending`，也没有 §5.2（`sed -n '/^## 5\.2/…'` 在 worktree 里无输出）。**改写已完整落在主工作树未提交面**，本轮逐行读过并认可（见 §3.1） |
| ② | 参数与许可合规 | **Blocked** | Owner 自己在契约新增 §5.2 里写明：判据文本 `docs/eval/calibration-protocol.md` 当前是 **`提案 · 未采纳 · 未运行`**，其执行顺序第 1 步（作者 + Owner 采纳、把 §6 的 `目标值` 写死）未完成 ⇒ "每个值有一种处置"不成立。**本轮该项没有任何移动** |

**⇒ Phase 2 状态词：`QA 判定`（未变）。`阶段结案` 仍不成立。** §8.1 段③ 按作者 09-24 的"不推：等门禁连绿"仍不推进，理由见 §4。

## 2. 读数（同场列出全部跑法，含负载设计）

构建：`dotnet build Pry.slnx -c Release` ⇒ **0 警告 / 0 错误**（22.45 s）。

| 跑法 | 方式 | 结果 | 退出码 |
|---|---|---|---|
| 1–3 | 解决方案级串行三次 | 每次 Client 92 / App 183 / Api 132 / Core 268+2skip ⇒ **675/0/2** | 未单独捕获（四行均为"已通过!"） |
| 4–6 | 同上，捕获真实退出码 | 675/0/2 | `dotnet_test_exit=0` ×3 |
| runA/B/C | 同上，各写独立 TRX 目录 | 675/0/2，`measure` 各 exit=0 | 0 ×3 |
| loadX + loadY | **同一 worktree 内两个 `dotnet test` 同时开跑**（造跨程序集并发负载，见 §2.1） | 两侧各 4 个程序集全绿 | `X_exit=0` / `Y_exit=0` |

合计 **11 次全量跑法、0 红例**。

### 2.1 并发负载探针为什么做、说明了什么、没说明什么

`DEF-P1WALK-03` 的登记描述是"四工程并行下 3/3 红、负载下确定"，`-04` 是"约 1/10 偶发"。本轮安静机器上 9 连绿之后，这个反差必须当场处理而不是留到下轮，所以做了一次受控探针：**同一 worktree 并行开两个解决方案级 `dotnet test`（8 个测试宿主同时在跑）**，两侧仍全绿。

- **说明的**：在纯 `44e52fc`、无他人活动测试进程的机器上，两条红例**不出现**——它们不是"四工程并行"这一形状本身的必然后果。
- **没说明的**：这两条红例当初是在**主工作树**（混着多个 lane 的未提交代码、并且有别的 Agent 在同时跑构建/测试）里出现的。本轮**没有**复现那个条件，也**不能**用"这次没红"去推翻缺陷登记或满足 §5.0 的连绿判据——判据要的是**修完**，不是**没撞上**。
- 环境自证：探针与读数期间机器上只有 8 个 `dotnet.exe`，全部起于同一秒（22:00:58）、各约 4 s CPU、约 100 MB，是本轮 `dotnet build` 留下的 MSBuild 复用节点，不是他人在跑的测试。这一点必须写出来，否则"零红例"会被高估。

## 3. 09-24 之后未提交面的移动（本轮逐行实读，全部标 `已落地主工作区未提交`）

### 3.1 RT-002 Owner 按作者裁定改写了契约，且改写是忠实的

`docs/phase2-runtime-contract.md`（M，未提交）新增 **§5.2「逐值处置已裁、判据文本尚未采纳」**，并把 §6 与 §9 改成裁决的形状：

- `DEV-04` / `DEV-05` / `DEV-06` ⇒ **"1.0 不实现，转入后续阶段（作者 2026-09-24 显式降级；保留编号）"**；`DEV-06` 还配了可复核事实（`FocusActive: false` 是 `TurnScheduler.cs:144` 硬编码 ⇒ **今天不可标定**，不是"待标定"）。
- `DEV-03` ⇒ **"1.0 内不标定，由 §5.2 的标定轮承接"**，并明写"不得读成 §7 第 2 步已在文档层关掉"。**这正是 QA 上一轮提请以复核的口径**（那两条裁定会在文档层撞同一个动作）——Owner 采纳了，且没有借它提前关掉 §7 第 2 步。
- §9 的群聊两行（`P2-Draft-TIM-04`、`P2-Draft-ACT-02`）⇒ 按 §5.2 **出口 ③** 记 `NotApplicable`、编号保留，不是待办。
- §5.2 引用 QA 标定协议 §1 的 **F1 / F3 / F4** 三条并声称"逐条回源确认"。本轮核对：**编号与内容都对得上**（F1 必要性箱发行默认下不可达、F3 `FocusActive` 硬编码、F4 退避只作用在无生产者的群聊分支）。
- §5.2 还替作者留了一条必须先回答的耦合（引 QA 协议 §6 末）：**若标定轮最终出口是"1.0 不提供频率旋钮"，则频率侧标定的 1.0 净收益为零**，采纳协议时必须同时说明，否则这轮工作会朝错误目标优化。QA 认可这条，并要求它在采纳时被写进裁决而不是停在提醒。

**QA 立场**：Owner 侧对 §8① 的义务**实质已完成，形式上还差一次提交**。本轮不因此把 ① 从"凭裁定成立"升格为"可审计地成立"——载体上没有它。

### 3.2 `DEF-P2TRIGGER-03` 的代码刀已落地且是真实守卫

`src/Pry.Core/Runtime/TurnScheduler.cs`（+14 行）新增 `BackoffDetail(...)`，在两处把决策改写为打断退避的分支上**同时改写 `detail`**，并把内层事实带在括号里供对照；`tests/Pry.Core.Tests/RuntimeComponentTests.cs`（+23 行）新增 `An_interrupt_backoff_rewrite_names_itself_in_the_detail_it_hands_to_the_trace`。

- 断言面：`StartsWith("interrupt_backoff")` + `Contains("pending=2")` + 第二个分支（500 ms 后仍在退避窗内）同判据 ⇒ **修复前两行必红**（旧值是"pending=2 达到阈值=1"，不可能以 `interrupt_backoff` 开头）。这是**形状层面的自反证**，因此 QA 未做变异反证——不改他人未提交代码（`AGENTS.md` 第 1、4 条）。
- 混树一次全量（含这批在途改动）：build 0/0，**676/0/2**（Core 268→269，多的正是这一条）。**该读数只说明"这一刀绿到可以提交"，不构成 §8.1 段② 的判定**，因为它不在任何可指认提交上。
- 结论：`DEF-P2TRIGGER-03` 状态 = `已落地主工作区未提交`，结案动作 = 提交后由 QA 从新载体复跑。

### 3.3 我上一轮登记的一条提请仍未处理

契约 §7 开头仍是"**按依赖排序，不并行**"。它现在与 §5.0（作者 2026-09-24 改规：满足四条"不干扰"判据即可并行推进整个项目）直接冲突。**该文件是 Owner 名下未提交文档，QA 不代改**；本轮再次登记，提请 Owner 在下次改写时按现行 §5.0 处理。

## 4. §8.1 四段链与 §5.0 指针

- 段①（Owner 自证）：契约 §5/§5.1/§6/§9 与 `DEF-P2TRIGGER-03` 的代码刀都在主工作树，混树读数全绿。
- 段②（QA 从可指认提交复跑）：**本文件**。载体仍是 `44e52fc`（`git log --oneline -8` 确认自 09-24 起无新提交）。
- 段③（作者/守门人推指针）：**不推进**。作者口径是"不推：等门禁连绿"，其含义是 `DEF-P1WALK-03`/`04` **修完**之后连绿；两条本轮仍在主工作树侧未修（`git log` 无相关提交、`-03` 落点 `SseEventStream.FramesAsync` 与 `-04` 的 `ClearAllPools()` 模式均未变动）。**本轮 11 连绿不折算成判据成立**，理由见 §2.1。
- 段④（Release）：未开始，且被 §8② 与 ①B 的"二选一落笔"前置卡着。

## 5. 新登记缺陷

**`QA-P2CLOSE2-01`（Medium，Owner = QA 自己，不派给别人）**：干净 worktree 里 `docs/eval/scenarios.json` 只有 **`suiteVersion 1.1.2` / 68 条**，因为 QA 在 09-23、09-24 把它扩到 **`1.3.1` / 75 条**的全部改动**至今未提交**。后果分两层：

1. 本轮 `measure-fixed-eval.ps1` 的 **exit=0 是对着旧目录算的**——它没有覆盖新立的 `TIM-009`…`TIM-012`、`BEH-008`/`BEH-009`、`PLT-014` 这 7 条。09-24 那轮同样如此（该轮只记了 exit=0，未记目录版本落差，本轮补齐）。
2. 按 §8.1 段② 的取证方式，"从可指认提交建干净 worktree"意味着**测量资产和判据文本一样必须受跟踪**，否则每次复验都在评一份旧目录，而"新场景已被映射"这个结论在载体上不可复现。

处置：这不是判据问题，是 QA 自己的证据链问题。**修法只有一条**——把 `docs/eval/**`（目录 + 报告 + 索引 + 本文件）与 `scripts/measure-fixed-eval.ps1` 提交，之后复验才会看到 1.3.1/75。提交动作等作者点头（`AGENTS.md` 第 10 条：只提交自己负责的文件）。在此之前，任何"目录已生效"的表述都必须带上"仅主工作树可见"的限定。

## 6. 下一步（按依赖与完整性排序，不按墙钟时间）

| 动作 | Owner | 出口条件 |
|---|---|---|
| 作者点头 ⇒ QA 提交 `docs/eval/**` + 测量脚本（结案 `QA-P2CLOSE2-01`） | QA / 作者 | 新提交的干净 worktree 里 `measure` 报 75 条 |
| 作者点头 ⇒ Owner 的 §5.2/§6/§9 改写与 `-03` 代码刀形成一次提交 ⇒ QA 从新载体复跑 | 作者 + RT-002 Owner + QA | §8① 从"凭裁定"升为"可审计"；`DEF-P2TRIGGER-03` 结案 |
| `DEF-P1WALK-03` / `-04` 修完（两条测试隔离竞态） | Core Platform @ `Pry.Api` / Core·Api 测试层 | §5.0"并行连续 ≥2 次全绿"才有结案资格；**本轮同时要求两位 Owner 重述触发条件**（安静机器 + 双跑并发下不出现，见 §2.1） |
| 采纳或修订 `docs/eval/calibration-protocol.md`，把 §6 `目标值` 写死，并同时回答"频率旋钮是否进界面"的耦合 | 作者 + RT-002 Owner | §8② 的唯一出口；未采纳前 ② 永远 `Blocked` |
| 契约 §7 开头"不并行"随现行 §5.0 改写 | RT-002 Owner | 文档层不再互相打脸 |

## 7. 未提交面清单（本轮 `git status --short --branch` 末态）

`M docs/README.md`、`M docs/character-runtime.md`、`M docs/phase2-runtime-contract.md`（Owner/他人名下）；`M docs/eval/README.md`、`M docs/eval/minimal-loop-run-checklist.md`、`M docs/eval/scenarios.json`、`M scripts/measure-fixed-eval.ps1`（QA 名下）；`M src/Pry.Core/Runtime/TurnScheduler.cs`、`M tests/Pry.Core.Tests/RuntimeComponentTests.cs`（Owner 名下，本轮首次出现在门禁面）；未跟踪：`docs/character-learning-safety.patch`、`docs/decision-request-protocol.md`、`docs/eval/calibration-protocol.md`、5 份 eval 报告 + 本文件。QA 未 stage、未 commit、未 push，未对他人文件做任何写入。
