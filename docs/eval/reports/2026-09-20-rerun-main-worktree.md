# QA 运行记录 / EVAL-001 主工作区复跑

**Run ID**：`eval-QA-RERUN-0920` · 2026-09-20 · QA：Qoder（QA / Evaluation）
**候选版本**：HEAD `3613a2a` + 主工作区未提交内容。**不是发行候选**：多 worktree 并行写入，本会话期间已跟踪修改从 40 变为 36、未跟踪从 41 变为 73。
**Dirty snapshot**：36 个已跟踪修改 + 73 个未跟踪项，均属其他协作者，本轮未触碰、未暂存、未提交（`staged_files=0`）。
**构建源 SHA-256（前 16 位）**：

| 文件 | hash |
|---|---|
| `src/Pry.Core/Memory/MemoryDatabase.Runtime.cs` | `85f1fb2b74d3ca73` |
| `src/Pry.Core/Runtime/ConversationRuntime.cs` | `431c046fddbe0982` |
| `tests/Pry.Core.Tests/ConversationRuntimeTests.cs` | `3e07e53854b19c16` |
| `tests/Pry.Core.Tests/EvaluationPersistenceTests.cs` | `229a6bd9b96677b1` |
| `src/Pry.Core/Conversation/ConversationService.cs` | `6d68677d4c57b9c0` |

**环境**：Windows 10.0.26200 x64 · .NET SDK `10.0.200`（目标 `net10.0`）· 时区 China Standard Time (+08:00) · Release 配置。
**模型与角色**：未运行任何真实模型、未加载权重、未启动桌面应用。角色卡 hash / Provider / Prompt & Context 版本 / 采样参数一律 **NotRun**。
**对照上一 Run**：`docs/eval/reports/2026-09-20-baseline.md` 的冻结快照（301 文件）。本轮换成共享主工作区，源码更新，模型与角色设置未变（两者均未运行模型）。

## Coverage

本轮只执行自动化层。逐类结论按套件口径记录，**用例数不等于场景通过率**：

| 类别 | 层级 | 状态 | 实际证据 | 未覆盖约束 | 负责人 |
|---|---|---|---|---|---|
| Timing TIM-001～008 | Unit + SQLite | PartialPass | `ConversationRuntimeTests`/`RuntimeComponentTests` 通过，含 TIM-006 epoch fencing | 桌面真实事件、first bubble 延迟、客户端不显示 stale | RT-002 |
| Behavior BEH-001～007 | Unit | PartialPass | `CharacterIntelligenceTests` 决策与闸门通过 | 宿主生成链是否真的遵守；DoNothing 端到端 | RT-003 |
| Expression EXP-001～005 | Unit | PartialPass | `ExpressionSystemTests` Selector/冷却/全拒通过 | 真实模型输出、10 轮重复观察 | RT-004 |
| Memory MEM-001～008 | Unit | **Blocked** | 仅 CRUD 与向量基础通过 | 见 QA-004/QA-005：无可达自动抽取、降级不可区分、三项统计未跑 | MEM-005 / RT-005 |
| Relationship REL-001～004 | 未执行 | Blocked | 无 | `RuntimeState.Trust/Familiarity` 仍为常量，未持久化 | RT-003 / Persistence |
| Proactive ACT-001～006 | Unit + SQLite | PartialPass | Scheduler/Policy/Delivery 规则通过 | 全链 Trigger→…→Send 与内容审核 | RT-002 |
| Persistence PER-001～004 | 真实 SQLite | PartialPass | PER-001/PER-003 通过 | 历史 schema 迁移、损坏备份恢复 | PLAT-003 |
| Protocol PRO-001～004 | 真实 SQLite + SSE | PartialPass | PRO-003 本次通过（见下）、PRO-004 通过 | 客户端补历史、通知去重、WebSocket | PLAT-005 |
| Recovery REC-001～004 | 真实 SQLite | PartialPass | REC-002/REC-003 本次通过 | OS 强杀、半开连接、故障后下一回合 | RT-002 |
| Capability CAP-001～003 | Unit | PartialPass | 占位回调规则通过 | 真 Dispatcher/PluginContext、副作用取消 | RT-007 |

53 个永久场景的 `acceptance` 仍全部为 **NotRun**（自动化映射只是部分证据），本轮未改变该口径。

## New Scenarios

无新增永久场景。53 个 ID 全部保留，`suiteVersion` 未变。本轮新增的是两个缺陷登记（QA-004、QA-005），复用既有场景 ID 作为验收落点：

- **QA-004** → MEM-001（正确记住）/ MEM-002（不乱记）/ MEM-005（不重复）。可观察变化：服务端无任何自动记忆写入路径。Forbidden Outcome：不得因记忆 CRUD 测试通过而把这三个场景判 Pass。
- **QA-005** → MEM-003（不相关时沉默）/ MEM-006（降级）/ MEM-007（隔离）。可观察变化：检索降级对调用方不可见。Forbidden Outcome：空列表不得同时表示「没有相关」和「查不动」。

## Regression Results

- `dotnet build Pry.slnx -c Release`：**0 错误 / 0 警告**（首轮快照记录 44 个既有警告，本轮增量构建已无）。
- `dotnet test Pry.slnx -c Release --no-build --logger trx --results-directory TestResults/eval-QA-RERUN-0920`：返回码 0。

| 程序集 | 通过 | 失败 | 跳过 |
|---|---:|---:|---:|
| Pry.Core.Tests | 153 | 0 | 1 |
| Pry.Api.Tests | 44 | 0 | 0 |
| Pry.Client.Tests | 7 | 0 | 0 |
| Pry.App.Tests | 162 | 0 | 0 |
| 合计 | **366** | **0** | **1** |

- `scripts/measure-fixed-eval.ps1 -ResultsDirectory TestResults/eval-QA-RERUN-0920`：`Catalog valid: 53 permanent scenarios`、`Passed=366; Failed=0; Other=1`、退出码 0、`releaseAcceptance: NotEvaluated`。
- `scripts/verify-repository.ps1`：205 个受跟踪文件通过（不含未跟踪源码）。
- `git diff --check` 与 `git diff --cached --check` 通过；暂存区为空。
- 跳过项仍是既有 `Configured_sense_voice_model_can_run_offline` 真实权重用例，未计为通过。
- **故障重跑同一源快照**：对 `ConversationRuntimeTests` + `EvaluationPersistenceTests` 子集连续 3 次运行，每次 30/30 通过，与全量共 4 次绿，源码未变。基线报告与旧交接记录提到的 typing/恢复用例不稳定（一次重跑 1 通过/2 失败）在本窗口**未复现**；因两次观察不是同一源快照，不据此宣称该不稳定性已消除。
- TRX 与 `fixed-eval-coverage.json` 位于本机被忽略的 `TestResults/eval-QA-RERUN-0920/`，不入库。
- **取证局限（如实登记）**：本轮在共享主工作区直接构建并测试，**未先检查是否有其他协作者同时在同一目录构建**。因此严格说，`--no-build` 消费的二进制归属只由构建与测试期间未出现他人的构建产物来间接推断，没有独立证明。后续复跑应先查 `dotnet.exe` 进程列表或改用 detached worktree 快照，否则「366/0/1」的归属证明弱于首轮基线报告所用的 301 文件冻结快照。

## Runtime Findings

`Pry.Core/TurnTaking/ConversationTurnManager.cs` 与 `Pry.Core/Conversation/ConversationService.cs` 经调用方核查已无生产引用：客户端使用 `Pry.App/Services/RemoteConversationTurnManager.cs`，服务端使用 `ConversationRuntime` + `ReplyPlannerComposer`（`Pry.Api/Runtime/RuntimeAdapters.cs:55`、`ConversationSessionService.cs:259,266`）。旧 `TurnTaking` 路径上的行为观察不再代表产品。

Timing P50/P95/max、stale attempted/committed/displayed 计数均为 **NotRun**（无真实桌面事件与真实 Provider）。「stale 永不发送」是系统不变量，本轮 SQLite fencing 用例只提供部分证据。恢复用例走的是重新打开连接，不是 OS 强杀。

## Behavior Findings

沉默、提问预算、低落未求助不安慰、客服语气规避等决策规则有通过证据（`CharacterIntelligenceTests`）。宿主生成链尚未调用 `CharacterIntelligenceService.DecideAsync`（`TransitionalReplyAdvisor` 仍在位），因此「模型是否真的照做」本轮未测。鹦鹉学舌、自然结束话题、每句都解释仍需多轮真实输出对照。

## Expression Findings

未运行真实模型。Selector 闸门（可全拒、严肃场景阻断口癖、冷却、未投递不学习）有规则测试证据；身份锚点、关系阶段、Mood 对照与 10 轮重复观察均 NotRun。

## Memory Findings

| 模型/模式/场景组 | 查询数 | U | F | M | U/(U+F) | U/(U+M) | 错误查询/查询数 |
|---|---:|---:|---:|---:|---|---|---|
| 未运行真实模型 | NotRun | NotRun | NotRun | NotRun | N/A | N/A | N/A |

三项统计全部 **NotRun，分母未知**，不是 0/0/0，更不是 100% 正确。本轮只确认代码事实：混合检索在 embedding 可用时有语义重排（`MemoryEmbeddingService.cs:94-106`），不可用时静默回落到关键词序（`:87`）。

## Critical / High / Medium / Low Issues

**QA-001 / High / Product defect / REC-003 — 已修复，回归通过。** `MemoryDatabase.Runtime.cs:82-83` 现以 `IsDBNull` 处理 `last_speaker`/`last_activity_at` 空值。4 次连续绿。**关闭口径限于「本快照回归通过」，统一提交上仍需复跑。**

**QA-002 / High / Product defect / PRO-003 — 已修复，回归通过。** `MemoryDatabase.Runtime.cs:195-197` 的 guard 增加 `NOT EXISTS(... earlier.sequence<$s AND earlier.state='pending')`，即事务内校验允许序号。同上口径。

**QA-003 / Medium / Test defect / REC-002 — 已修复。** 由 RT-002 Owner 在 `ConversationRuntimeTests.cs` 侧修正断言，本轮未修改该文件。

**QA-004 / Medium / Product gap / MEM-001·002·005 — 未修复。** 事实：`ConversationService.cs:53` 的自动抽取随该文件一起失去调用方；生产链唯一写入点是 `MemoryApplicationService.cs:25` 手工录入。遗留实现本身为 8 个硬编码关键词 + 长度 8~300（`:56-57`）、固定 importance `.65`、整句原文入库、`ExtractTags` 取前 5 个切片（`:59`），`AddMemoryAsync`（`MemoryDatabase.cs:517-529`）无 UNIQUE 无去重。影响：角色不会自动形成共同经历，§七 无验收对象。Owner：MEM-005/RT-005。修复后固定回归：MEM-001/002/005 端到端。

**QA-005 / Medium / Product gap / MEM-003·006·007 — 未修复。** 事实：`MemoryEmbeddingService.cs:87`（无 embedding 模型）与 `:110`（异常）静默回落到 `MemoryDatabase.cs:665-674` 的 `LIKE` OR 拼接 + `importance DESC, id DESC`；自动记忆 importance 恒 `.65`，故退化为「最近优先」且 OR 无最低命中门槛。`SituationSnapshot.cs:10` 已有 `MemoryQueryOutcome.DegradedKeywordOnly` 但生产检索器不产出。影响：误召回风险最高且调用方无从知晓，违背 §7.1「错误/空结果可区分」。Owner：MEM-005/MEM-006。修复后固定回归：MEM-003/006/007 与 outcome 断言。

**Proactive / 非缺陷 / 产品定夺。** `MemoryDatabase.Proactive.cs:178,208` 在 `AwaitingUserReply` 时直接判不合格并返回 null，即用户不回应则永久静默而非降速。正面：主动轰炸结构上不可能。风险：反向造成「她再也不理我」。登记于 ACT 场景，是否改为退避由 RT-002/产品决定。

**Critical：本轮未复现。** 未运行真实数据迁移、损坏备份、跨实例与 OS 强杀，因此不排除，不视为通过。

## User Feedback Converted to Tests

真实用户反馈 **0 条**，转化 **0 条**，未发邀请、未新增遥测。本轮 2 个新登记项来自 QA 源码核查与调用方分析，不得冒充 early adopter 反馈。

## Release Blockers

1. QA-001/002/003 仅在多 worktree 未提交快照上回归通过，需在 RT-008 统一可构建提交上整批重跑后才可按发行意义关闭。
2. QA-004/QA-005 使 MEM-001～008 停留在 Blocked，1.0 必需能力未验收。
3. REL/EXP 依赖 Intelligence 接线与关系状态持久化；CAP 依赖 RT-007；PER/REC/PRO 依赖 PLAT-003/005。
4. 真实模型角色连续性、桌面输入与打断、Memory 三项统计本轮零证据。

**当前结论：未评定（NotEvaluated），不接受发行。**

**传播边界（QA 结论如何到达 Owner）**：`PROJECT_PLAN.md` 被 `.gitignore` 排除，且各协作者工作在**同级独立 worktree**（`git worktree list` 当前 10 个），因此本文件与 `docs/eval/` 整体仍是未跟踪状态，**不会通过 Git 传到 MEM-005 / RT-002 / Persistence 负责人的工作区**。QA-004、QA-005 与"本轮快照分钟级有效"的限定目前只经由作者（产品负责人）转达。若需要跨协作者可靠传递，须由文件 Owner 将 `docs/eval/` 与本报告转为受跟踪文件；QA 不自行提交他人分支上的内容，也不以台账更新替代送达确认。

**§5.0 串行门禁下的范围**：作者 2026-09-21 指令后，唯一开放阶段是 Phase 1（RT-001 + PLAT-001/002/005/006），`MEM-005`、RT-002/003/004/005/007 冻结。EVAL 属豁免线但只服务当前阶段的真实问题，故 QA 本阶段验收面收敛为 **PER / PRO / REC + Provider 有界超时与错误语义（PLAT-001）+ Core 日志与上传上限（PLAT-006）**；QA-004/QA-005 转为冻结阶段登记项，不催促 Memory Owner 动工。本轮 366/0/1 含现已冻结的 Runtime/Intelligence/Expression 源码，原负责人按 §5.0 做现状封存一旦改变源码组合，本文件数字即失效，需在新基线重跑。

## Deferred Eval

真实模型多次运行与人工语用复核（依赖：可构建统一候选）；默认本地离线首次使用（依赖 RT-001/MODEL）；Timing P50/P95/max（依赖桌面端到端探针）；PER-002 历史 schema 迁移与损坏备份（PLAT-003）；REC-002 完整 OS 强杀（RT-002）；PRO-001/002 WebSocket 与目标协议版本协商（PLAT-005，SSE 测试不可替代）；CAP-002/003 真插件超时与权限（RT-007）；MEM-001～008 三项统计、墓碑与实例隔离（MEM-005/RT-005）；EVAL-002 真实 Day 1/2/7（建议推迟到 QA-004 修复后，否则「失忆」会被误归因）。以上 1.0 必需能力均记 Blocked，不因延期变成 NotApplicable。本轮未执行真实模型、未强杀操作系统进程、未操作真实用户数据库、未做纵向观察。
