# 第十二波执行报告·HDS-002 契约面：为 17 处"改坏零代价"判定成员各钉一行会响的判据

- 报告 id：`eval-QA-WAVE12-1023`
- 工单：`docs/dispatch-hds-wave12-20261023.md`（提交 `a4f89a6`）
- 载体基点：`6d6c66d`（全号 `6d6c66d5cc30f68803410ab063487c0f990c745e`）
- 执行方：冷启动 lane（wave12-lane）
- 性质：判据刀，零产品代码
- 状态：**执行中**（本文件按批次增量落盘；§〇／§一 先落）

---

## 〇 开工前当场做的三件事 + 锚点读回

### 〇.1 主工作树只读清点（开工前，原样贴出）

`git -C C:/Users/24782/Desktop/Pry_Project status --short --branch`：

```
## codex/ui-main-window-redesign
M  docs/dispatch-tip1020-02-memory-group-judgment.md
M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs
?? .qoder/
?? docs/character-learning-safety.patch
?? docs/research/HDS_INTERLUDE.md
?? docs/research/HDS_QQ_RESEARCH_INDEX.md
?? docs/research/QQ_BRIDGE.md
```

他人名下两枚暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）本波一律未读、未改、未提交、未 stash、未 clean。主树本波唯一笔迹＝本报告文件。

### 〇.2 独立载体（不动主工作树）

`git worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave12-1023/carrier 6d6c66d`：

```
Preparing worktree (detached HEAD 6d6c66d)
HEAD is now at 6d6c66d docs(qa,wave11): 第十一波收口入库——M-CB6 第一次拿到单枚清洁红……
```

入场两行判据（载体内当场实测）：

```
git status --short | wc -l   → 0        （预期 0 ✓）
git rev-parse HEAD           → 6d6c66d5cc30f68803410ab063487c0f990c745e   （预期 6d6c66d 全号 ✓）
```

### 〇.3 起点成立的代码面判据

`git -C 主树 diff --name-only 6d6c66d..a4f89a6 -- src tests`：

```
（空输出）
行数 = 0        （预期 0 ✓ —— 本波出闸是纯文档刀，起点成立）
```

### 〇.4 锚点读回（载体内逐条读回原文，全部对上）

载体 `src/Pry.Core/Narrative/NarrativeDraftValidator.cs`（`NarrativeIntentPolicy.Validate` 与 `KnowledgeCandidatePolicy.Validate`）：

| 编号 | 工单行 | 载体盘上原文 | 拒绝理由 | 对平 |
|---|---|---|---|---|
| S | `:312` | `if (!candidate.InstanceId.Equals(content.InstanceId))` | ScopeEscalation | ✓ |
| K1 | `:318` | `if (candidate.Confidence is < 0 or > 1)` | RealityContamination | ✓ |
| K2 | `:325`～`:327` | `Kind == UserReportedFact` ／ `&& Origin is NarrativeProse or ModelSelfReport` ／ `or Intent)` | RealityContamination | ✓ |
| K3 | `:334`～`:336` | `Kind == ConfirmedAgreement` ／ `&& Origin is not (UserMessageEvidence or HostObservation` ／ `or ExplicitFeedback))` | ConfirmationWithoutUserEvidence | ✓ |
| K4 | `:342` | `if (candidate.Kind == CognitionKind.ConfirmedAgreement && candidate.EvidenceRefs.Count == 0)` | ConfirmationWithoutUserEvidence | ✓ |
| K5 | `:349`～`:351` | `Kind == Proposal` ／ `&& Origin is (UserMessageEvidence or HostObservation` ／ `or ExplicitFeedback) && Confidence > .7)` | IntentClaimedAsFact | ✓ |
| K6 | `:358`～`:359` | `Kind == CharacterFiction` ／ `&& Origin is UserMessageEvidence or HostObservation)` | RealityContamination | ✓ |
| K7 | `:366`～`:367` | `Kind == NarrativeProse` ／ `&& Origin is not (NarrativeProse or ModelSelfReport))` | RealityContamination | ✓ |
| I1 | `:275` | `if (string.IsNullOrWhiteSpace(intent.SourceCommitId))` | IntentClaimedAsFact | ✓ |
| I2 | `:281` | `if (intent.EarliestDueAtUtc is { } earliest && intent.LatestDueAtUtc is { } latest && latest < earliest)` | IntentClaimedAsFact | ✓ |
| I3 | `:287`～`:288` | `if (intent.Status is NarrativeIntentStatus.Fulfilled && content.Actions.All(a =>` ／ `a.Mode != CommunicationMode.Immediate || !string.Equals(a.DeferredIntentId, intent.IntentId, StringComparison.Ordinal)))` | RealityContamination | ✓ |

附：`:269`（`if (!intent.InstanceId.Equals(content.InstanceId))`，ScopeEscalation）——工单指出它由既有 `[Fact] Intent_from_another_instance_scope_is_refused`（`:369`）以 `Assert.Contains` 形状把守，本波不动它。

枚举面 `src/Pry.Core/Narrative/NarrativeDocuments.cs`：`CognitionKind` 6 个（`UserReportedFact=0`／`CharacterFiction=1`／`CharacterBelief=2`／`Proposal=3`／`ConfirmedAgreement=4`／`NarrativeProse=5`，`:131`～`:145`）；`CognitionOrigin` 6 个（`UserMessageEvidence=0`／`HostObservation=1`／`ExplicitFeedback=2`／`NarrativeProse=3`／`ModelSelfReport=4`／`Intent=5`，`:148`～`:156`）。逐条对上。

判据侧现状 `tests/Pry.Core.Tests/NarrativeIdentityContractTests.cs`：载体上当场 `grep -c` 实测＝`[Fact] 17`／`[Theory] 3`／`[InlineData] 14`（与工单 §〇.4 一致）。三处与本波有关者已逐行读回原文（矩阵 `[Theory]` 5 行 InlineData 在 `:321`～`:325`；`Confirmed_agreement_without_any_evidence_reference_is_refused` 松形状 `Assert.Contains` 在 `:350`；`Intent_from_another_instance_scope_is_refused` 唯一 `[Fact]` 在 `:369`）。夹具原文（`:379`～`:440`）读回：`Confidence` 候选内联 `.9`、`EvidenceRefs = ["msg-102"]`、`InstanceId = draft.InstanceId`（Alpha）；意图夹具 `Status = Pending`、`SourceCommitId = "batch-1"`、`EarliestDueAtUtc = Now.AddMinutes(20)`、`LatestDueAtUtc` 未设；唯一动作 `Mode = Immediate`、`DeferredIntentId` 未设。

在册突变凭证 grep（`NarrativeDraftValidator.cs` 在 `docs/eval/reports/` 与 `docs/dispatch-*.md`）：仅命中本波工单本身，**0 处既有在册红**——与工单 §三"我出闸前实测＝0 处提及"一致。⇒ M-04／M-05／M-10／M-20 属该出口的第一次实测红。

---

## 一 T1｜出口表（纯读码，零构建）

> 说明：工单 §一 点名的四个政策体中，第三个 `NarrativeActionValidator` 在盘上不存在。动作侧的全部拒绝出口实际位于 **`NarrativeDraftValidator.CheckAction`（私有方法，`:186`～`:257`）**，`NarrativeGuard.RequireText(action.ActionId, …)` 也在其中（`:189`）。就地登记此差异，不涂改工单。以下表按盘上原文给出。

### 1.1 `KnowledgeCandidatePolicy.Validate`（`:303`～`:374`）

| 出口 | 行 | 条件原文 | 理由 | 被哪条用例命中／无命中（证据） |
|---|---|---|---|---|
| Guard-CandidateId | `:306` | `RequireText(candidate.CandidateId, …)` | ConfirmationWithoutUserEvidence（抛 `NarrativeContractException`，非进 rejections） | 无命中：夹具 CandidateId 恒非空（`kc-1`／`kc-2`），全仓无用例置空 |
| Guard-Claim | `:308` | `RequireText(candidate.Claim, …)` | RealityContamination（抛） | 无命中：Claim 恒非空 |
| Guard-ProposedBy | `:309` | `RequireText(candidate.ProposedBy, …)` | ConfirmationWithoutUserEvidence（抛） | 无命中：ProposedBy 恒 `narrator` |
| S（`:312`） | `:312` | `!candidate.InstanceId.Equals(content.InstanceId)` | ScopeEscalation | **无命中（本波补）**：矩阵与 `:350` 均用 `InstanceId = draft.InstanceId` 同实例；本波新增 `Candidate_from_another_instance_scope_is_refused` |
| K1（`:318`） | `:318` | `Confidence is < 0 or > 1` | RealityContamination | **无命中（本波补）**：夹具 Confidence 恒 `.9`；新增 `[Theory] Confidence_outside_the_unit_interval_is_refused` 两侧 |
| K2（`:325`～`:327`） | 三成员 | `UserReportedFact && Origin ∈ {NarrativeProse, ModelSelfReport, Intent}` | RealityContamination | NarrativeProse 有（`:321`）、ModelSelfReport 有（`:322`）、**Intent 无命中（本波补矩阵行）** |
| K3（`:334`～`:336`） | 三成员 | `ConfirmedAgreement && Origin ∉ {UserMessageEvidence, HostObservation, ExplicitFeedback}` | ConfirmationWithoutUserEvidence | 允许支三成员**全无命中（本波补矩阵行）**；拒绝支 `ModelSelfReport` 有（`:323`） |
| K4（`:342`） | `:342` | `ConfirmedAgreement && EvidenceRefs.Count == 0` | ConfirmationWithoutUserEvidence | 有格把守但形状松（`:350` 的 `Assert.Contains`）；**从未拿到实测红，本波 M-10 首次取红** |
| K5（`:349`～`:351`） | 三成员 | `Proposal && Origin ∈ {UserMessageEvidence, HostObservation, ExplicitFeedback} && Confidence > .7` | IntentClaimedAsFact | **Proposal 整门全仓无命中（本波补矩阵 6 行）**；阈值 `.7` 由矩阵 `Confidence=.9` 行命中（不改动它） |
| K6（`:358`～`:359`） | 两成员 | `CharacterFiction && Origin ∈ {UserMessageEvidence, HostObservation}` | RealityContamination | UserMessageEvidence 有（`:324`）、**HostObservation 无命中（本波补矩阵行）** |
| K7（`:366`～`:367`） | 两成员 | `NarrativeProse && Origin ∉ {NarrativeProse, ModelSelfReport}` | RealityContamination | **NarrativeProse Kind 整门全仓无命中（本波补矩阵 6 行）** |

### 1.2 `NarrativeIntentPolicy.Validate`（`:263`～`:295`）

| 出口 | 行 | 条件原文 | 理由 | 命中证据 |
|---|---|---|---|---|
| Guard-IntentId | `:266` | `RequireText(intent.IntentId, …)` | IntentClaimedAsFact（抛） | 无命中：夹具 IntentId 恒 `intent-1` |
| Guard-Topic | `:267` | `RequireText(intent.Topic, …)` | IntentClaimedAsFact（抛） | 无命中：Topic 恒 `把画具收完` |
| `:269` | `:269` | `!intent.InstanceId.Equals(content.InstanceId)` | ScopeEscalation | 有格（`:369` 的 `Assert.Contains`，非本波范围，不动） |
| I1（`:275`） | `:275` | `string.IsNullOrWhiteSpace(intent.SourceCommitId)` | IntentClaimedAsFact | **无命中（本波补）**：夹具 SourceCommitId 恒 `batch-1`；新增 `Intent_without_a_source_commit_is_refused` |
| I2（`:281`） | `:281` | `EarliestDueAtUtc is {} && LatestDueAtUtc is {} && latest < earliest` | IntentClaimedAsFact | **无命中（本波补）**：夹具从不设 LatestDueAtUtc；新增 `Intent_whose_deadline_precedes_its_earliest_is_refused` |
| I3（`:287`～`:288`） | `:287` | `Status is Fulfilled && Actions.All(a => a.Mode != Immediate || !Equals(a.DeferredIntentId, IntentId))` | RealityContamination | **无命中（本波补）**：夹具 Status 恒 Pending；新增 `Fulfilled_intent_without_a_matching_delivery_is_refused` |

### 1.3 动作侧 `NarrativeDraftValidator.CheckAction`（`:186`～`:257`，工单误名 `NarrativeActionValidator`）

| 出口 | 行 | 理由 | 命中／待拆 |
|---|---|---|---|
| Guard-ActionId `:189` | ActionTargetMismatch（抛） | 无命中 ⇒ **待拆清单** |
| `:191` 目标会话不符 | ActionTargetMismatch | 已由 `:204`／`:219` 等经 `NarrativeDraftValidator.Validate` 的 `[Fact]` 命中（本波不动） |
| `:200` Immediate 段数越界 | ActionSegmentCountInvalid | 无命中 ⇒ **待拆清单** |
| `:208` Immediate 正文绑定失败 | binding.Failure | 有（`:285`／`:271` Theory） |
| `:214` Immediate 挂未来意图 | IntentClaimedAsFact | 无命中 ⇒ **待拆清单** |
| `:223` Deferred 预留段 | SegmentCountExceeded | 无命中 ⇒ **待拆清单** |
| `:229` Deferred 带正文 | ActionBindingMissing | 无命中 ⇒ **待拆清单** |
| `:235`／`:240` Deferred 意图缺失／不在提案 | IntentClaimedAsFact | 有（`:299`） |
| `:248` default（沉默）带正文/段 | SegmentCountExceeded | 无命中 ⇒ **待拆清单** |

### 1.4 提交侧 `NarrativeCommitValidator.Validate`（`:383`～`:419`）

全部出口（`:387` Guard-CommitId、`:389` ContractVersionUnsupported、`:395` ContentHashMissing、`:403` 意图无来源、`:410` 意图来源既非提交也非批次）**均无命中**（`NarrativeIdentityContractTests` 无一枚走提交面）⇒ 全部列**待拆清单**，不进本波预算。

### 1.5 本波不拆只登记（工单 §〇.5 第二条）

- `CognitionKind.CharacterBelief`（`:138`）：在 `KnowledgeCandidatePolicy`／`NarrativeIntentPolicy` 两政策体中**无任何条款引用它** ⇒ 六个 Origin 全落 `None`。本波不补它的行（补了不构成任何出口把守）。归属下一波候选。
- `NarrativeGuard.RequireText` 五处（`:306`／`:308`／`:309`／`:266`／`:267`）＋ `CheckAction` 内 `:189`：均抛 `NarrativeContractException` 而非进 `rejections`，本波矩阵/新格夹具恒填非空文本 ⇒ 全列待拆清单。
- 反方向（往集合里加成员＝放宽→收紧）本波**明写为未验证**，仅由矩阵 `None` 行结构性把守，无实测红。

---

## 二 T2｜落判据（追加式）

载体提交基点 `6d6c66d5cc30f68803410ab063487c0f990c745e`。全部改动落在 `tests/Pry.Core.Tests/NarrativeIdentityContractTests.cs` 一份文件；`src/**` 未动。

### 2.1 矩阵 `[Theory] Knowledge_candidate_origin_decides_what_it_may_assert` 补成 30 格

方法体与现有 5 行 `[InlineData]`（`:321`～`:325`）一字未动；仅**追加** 25 行。追加行序照 Kind→Origin 枚举值，跳过 5 个已有格。期望值先按工单 §2.1 推演表填，与盘上政策原文逐格复核后**完全一致**（无一处需按"工单推演 vs 盘上不符"处理）：
- UserReportedFact：HostObservation=None、ExplicitFeedback=None、Intent=RealityContamination
- CharacterFiction：HostObservation=RealityContamination、ExplicitFeedback/NarrativeProse/ModelSelfReport/Intent=None
- Proposal：UserMessageEvidence/HostObservation/ExplicitFeedback=IntentClaimedAsFact（夹具 Confidence=.9>`.7`）、NarrativeProse/ModelSelfReport/Intent=None
- ConfirmedAgreement：UserMessageEvidence/HostObservation/ExplicitFeedback=None、NarrativeProse/Intent=ConfirmationWithoutUserEvidence
- NarrativeProse：UserMessageEvidence/HostObservation/ExplicitFeedback=RealityContamination、NarrativeProse/ModelSelfReport=None、Intent=RealityContamination

阈值 K5 的 `.7` 与 `Confidence` 默认 `.5` 均未改动，只用夹具 `.9` 去命中。

### 2.2 新建 `[Theory] Confidence_outside_the_unit_interval_is_refused(double)`

2 行 `[InlineData]`＝`1.5`／`-0.1`，夹具 Kind=UserReportedFact×Origin=UserMessageEvidence（其余门全不响）、Confidence 取参数。断言＝`Assert.Equal(1, rejections.Count)` ＋ `Assert.Equal(NarrativeContractReason.RealityContamination, rejections[0].Reason)`（两侧边界都点名确切计数与首条理由）。

### 2.3 新建四枚 `[Fact]`（全部确切计数＋点名理由，无 `Assert.Contains`／`> 0`／`NotEmpty`）

| 新格 | 守哪枚 | 夹具 | 确切期望 |
|---|---|---|---|
| `Candidate_from_another_instance_scope_is_refused` | `:312` | InstanceId=Beta，Kind/Origin=UserReportedFact×UserMessageEvidence，Confidence=.9，EvidenceRefs=["msg-102"] | `Count==1` ＋ `ScopeEscalation` |
| `Intent_without_a_source_commit_is_refused` | `:275` | `draft.Intents[0] with { SourceCommitId = "" }` | `Count==1` ＋ `IntentClaimedAsFact` |
| `Intent_whose_deadline_precedes_its_earliest_is_refused` | `:281` | `with { LatestDueAtUtc = Now.AddMinutes(10) }`（Earliest=+20） | `Count==1` ＋ `IntentClaimedAsFact` |
| `Fulfilled_intent_without_a_matching_delivery_is_refused` | `:287` | `with { Status = NarrativeIntentStatus.Fulfilled }`（夹具动作 Immediate、DeferredIntentId 空） | `Count==1` ＋ `RealityContamination` |

三枚意图新格均直调 `NarrativeIntentPolicy.Validate(intent, NarrativeContent.From(draft))`，不走 `NarrativeDraftValidator.Validate`，避免把动作侧拒绝卷进来。

### 2.4 判据条数等式（载体内 `grep -c` 实测，原样贴出）

```
落码前：[Fact] 17   [Theory] 3   [InlineData] 14
落码后：[Fact] 21   [Theory] 4   [InlineData] 41   （+4 / +1 / +27）
新增可跑用例 = 4（新 Fact） + 2（边界 Theory 行） + 25（矩阵新行） = 31 条 ✓
```

三条等式与工单预钉值一致（17→21／3→4／14→41）。

### 2.5 干净态正向探针（进 T3 的前置门）——已绿

预检 #1（本次 dotnet 前）整段原文：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: msedge pid=5424 class=Chrome_WidgetWin_1 style=0x17CF0000 rect=-1928,96,1936x1048 screen=1920x1080 covers=True borderless=False title=Lecture 12 Geometry 3_..._bilibili ... - 
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=14% used=1284MB
free_memory_gb=7.4
VERDICT=BUILD_OK
```

跑法（不带 `--no-build`，绝对 results-directory，带 `--logger trx`）：

```
dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release \
  --filter "FullyQualifiedName~NarrativeIdentityContractTests" \
  --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave12-1023\runs\run-clean" \
  --logger trx
```

TRX 份数：`ls *.trx`＝1（`24782_PHYTHLINK_2026-10-23_12_40_07.trx`）。`<Counters>` 原文：

```
<Counters total="62" executed="62" passed="62" failed="0" error="0" timeout="0" aborted="0" inconclusive="0" passedButRunAborted="0" notRunnable="0" notExecuted="0" disconnected="0" warning="0" completed="0" inProgress="0" pending="0" />
```

**failed=0 ✓**。控制台编译期有 5 处 `warning xUnit2013`（Do not use Assert.Equal() to check for collection size，行 `:422/:443/:453/:463/:473`）——这是 §2.3/§2.2 明确要求的确切计数形状（`Assert.Equal(1, rejections.Count)`）触发的分析器建议，非错误、非失败，未降低任何断言。

31 条新增可跑用例全部在通过列表按名点名（TRX `outcome="Passed"`）：

矩阵 30 格（全部 Passed）：
- `Knowledge_candidate_origin_...(kind: UserReportedFact, origin: UserMessageEvidence, expected: None)`
- `...(UserReportedFact, HostObservation, None)`
- `...(UserReportedFact, ExplicitFeedback, None)`
- `...(UserReportedFact, NarrativeProse, RealityContamination)`
- `...(UserReportedFact, ModelSelfReport, RealityContamination)`
- `...(UserReportedFact, Intent, RealityContamination)`
- `...(CharacterFiction, UserMessageEvidence, RealityContamination)`
- `...(CharacterFiction, HostObservation, RealityContamination)`
- `...(CharacterFiction, ExplicitFeedback, None)`
- `...(CharacterFiction, NarrativeProse, None)`
- `...(CharacterFiction, ModelSelfReport, None)`
- `...(CharacterFiction, Intent, None)`
- `...(Proposal, UserMessageEvidence, IntentClaimedAsFact)`
- `...(Proposal, HostObservation, IntentClaimedAsFact)`
- `...(Proposal, ExplicitFeedback, IntentClaimedAsFact)`
- `...(Proposal, NarrativeProse, None)`
- `...(Proposal, ModelSelfReport, None)`
- `...(Proposal, Intent, None)`
- `...(ConfirmedAgreement, UserMessageEvidence, None)`
- `...(ConfirmedAgreement, HostObservation, None)`
- `...(ConfirmedAgreement, ExplicitFeedback, None)`
- `...(ConfirmedAgreement, NarrativeProse, ConfirmationWithoutUserEvidence)`
- `...(ConfirmedAgreement, ModelSelfReport, ConfirmationWithoutUserEvidence)`
- `...(ConfirmedAgreement, Intent, ConfirmationWithoutUserEvidence)`
- `...(NarrativeProse, UserMessageEvidence, RealityContamination)`
- `...(NarrativeProse, HostObservation, RealityContamination)`
- `...(NarrativeProse, ExplicitFeedback, RealityContamination)`
- `...(NarrativeProse, NarrativeProse, None)`
- `...(NarrativeProse, ModelSelfReport, None)`
- `...(NarrativeProse, Intent, RealityContamination)`

边界 2 行（Passed）：`Confidence_outside_the_unit_interval_is_refused(confidence: 1.5)`、`Confidence_outside_the_unit_interval_is_refused(confidence: -0.1)`。

四枚新 `[Fact]`（Passed）：`Candidate_from_another_instance_scope_is_refused`、`Intent_without_a_source_commit_is_refused`、`Intent_whose_deadline_precedes_its_earliest_is_refused`、`Fulfilled_intent_without_a_matching_delivery_is_refused`。

⇒ 干净态 62／62 全绿，前置门打开，进入 T3。

---

## 三 T3｜逐枚单放突变，各取一枚单枚清洁红

统一约定（每轮共用，逐轮不再重复展开）：
- ① 载体基点 HEAD＝`6d6c66d5cc30f68803410ab063487c0f990c745e`；被改文件磁盘 blob 在还原后与 `git show HEAD:src/Pry.Core/Narrative/NarrativeDraftValidator.cs` 逐字节对平＝`88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c`（M-01 起该基线哈希在每一轮都稳定复现）。
- ⑤ 跑法（一轮一条，绝不带 `--no-build`）：`dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~NarrativeIdentityContractTests" --results-directory "<绝对路径>\run-mNN" --logger trx`
- ⑧ 还原方式：`git checkout -- src/Pry.Core/Narrative/NarrativeDraftValidator.cs`（绝不 `git checkout -- .`）。
- ⑩ 每轮还原后载体 `git status --short` 行数＝**1**（仅本波那一份测试文件）。
- 每轮 TRX 份数 `ls *.trx`＝1，目录不复用。红点行号照抄 TRX（断言调用处），另注断言文本所在源码行。

### 批次 1（M-01 … M-06）

**M-01｜`:312` 整门删**
- ② 被改：`NarrativeDraftValidator.cs:312`（S 门）
- ③ before→after：删除 `if (!candidate.InstanceId.Equals(content.InstanceId)) { … ScopeEscalation … }` 整块（312～316，共 5 行）
- ④ `--stat`：`1 file changed, 5 deletions(-)`
- ⑥ TRX：`run-m01/24782_PHYTHLINK_2026-10-23_12_44_18.trx`；Counters `total 62 executed 62 passed 61 failed 1 error 0`
- ⑦ 红格（唯一 1）：`Candidate_from_another_instance_scope_is_refused`；消息 `Assert.Equal() Failure: Expected: 1 / Actual: 0`；红点行号 `NarrativeIdentityContractTests.cs:line 443`（断言文本 `Assert.Equal(1, rejections.Count)` 在源 `:442`）
- ⑨ SHA-256：还原后磁盘 `88ebf5c3…b00c` ＝ HEAD `88ebf5c3…b00c`（对平 ✓）
- ⑪ 结论：证明 `:312` ScopeEscalation 门现在被本波新格把守——摘掉它，那条 `Assert.Equal(1,…)` 会当场变红。没证明：其它 Kind/Origin 组合、`NarrativeDraftValidator.Validate` 顶层作用域条款。单枚清洁，凭证仅属本波新格。

**M-02｜`:318` 去掉 `or > 1`**
- ③ before→after：`if (candidate.Confidence is < 0 or > 1)` → `if (candidate.Confidence is < 0 )`
- ④ `1 file changed, 1 insertion(+), 1 deletion(-)`；⑥ `run-m02/2026-10-23_12_45_20.trx`，Counters `passed 61 failed 1`
- ⑦ 红格（唯一 1）：`Confidence_outside_the_unit_interval_is_refused(confidence: 1.5)`；`Expected: 1 / Actual: 0`；红点 `:line 422`
- ⑨ 对平 ✓（`88ebf5c3…b00c`）；⑩ 1 行
- ⑪ 证明上界 `> 1` 一侧被新行把守。没证明下界（M-03 单列）。

**M-03｜`:318` 去掉 `< 0 or`**
- ③ `is < 0 or > 1` → `is > 1`；④ 1 insertion/1 deletion；⑥ `run-m03/…12_45_26.trx` `passed 61 failed 1`
- ⑦ 红格（唯一 1）：`Confidence_outside_the_unit_interval_is_refused(confidence: -0.10000000000000001)`；`Expected: 1 / Actual: 0`；红点 `:line 422`
- ⑨ 对平 ✓；⑩ 1 行
- ⑪ 证明下界 `< 0` 一侧被新行把守，与 M-02 分列两轮、红格不同，未合并。

**M-04｜`:326` 去掉 `CognitionOrigin.NarrativeProse`**（K2 成员）
- ③ `Origin is NarrativeProse or ModelSelfReport` → `Origin is ModelSelfReport`（连同 327 的 `or Intent` ⇒ 剩 `ModelSelfReport or Intent`）
- ④ 1/1；⑥ `run-m04/…12_45_31.trx` `passed 61 failed 1`
- ⑦ 红格（唯一 1）：矩阵 `Knowledge_candidate_origin_...(kind: UserReportedFact, origin: NarrativeProse, expected: RealityContamination)`；消息 `Expected: False / Actual: True`（`Assert.Equal(expected==None, count==0)` 两侧都落 True≠False）；红点 `:line 368`
- ⑨ 对平 ✓；⑩ 1 行
- ⑪ **该出口第一次实测红**（把守格＝工单 §〇.4 的 `:321` 现有行，grep 实测本文件此前 0 处在册红）。证明 K2 的 NarrativeProse 成员被现有行结构性把守且能响。没证明：反向"加成员"。

**M-05｜`:326` 去掉 `or CognitionOrigin.ModelSelfReport`**（K2 成员）
- ③ `Origin is NarrativeProse or ModelSelfReport` → `Origin is NarrativeProse`（+`or Intent` ⇒ `NarrativeProse or Intent`）
- ④ 1/1；⑥ `run-m05/…12_45_37.trx` `passed 61 failed 1`
- ⑦ 红格（唯一 1）：矩阵 `...(UserReportedFact, origin: ModelSelfReport, expected: RealityContamination)`；`Expected: False / Actual: True`；红点 `:line 368`
- ⑨ 对平 ✓；⑩ 1 行
- ⑪ 该成员第一次实测红（把守格 `:322`）。

**M-06｜`:327` 去掉 `or CognitionOrigin.Intent`**（K2 成员，工单零代价清单第 4 枚）
- ③ 327 `or CognitionOrigin.Intent)` → `)` ⇒ K2 剩 `NarrativeProse or ModelSelfReport`
- ④ 1/1；⑥ `run-m06/…12_45_42.trx` `passed 61 failed 1`
- ⑦ 红格（唯一 1）：矩阵 `...(UserReportedFact, origin: Intent, expected: RealityContamination)`；`Expected: False / Actual: True`；红点 `:line 368`
- ⑨ 对平 ✓；⑩ 1 行
- ⑪ 证明 `UserReportedFact × Intent` 这一格此前全仓无人经过（工单零代价第 4 枚），本波新行把它变成会响。

批次 1 小结：6 轮全部单枚、单轮、清洁，红集恰＝1 格，无红集多一格，无"改坏后无格能响"。

### 批次 2（M-07 … M-12）

**M-07｜`:335` 从允许集合去掉 `UserMessageEvidence`**（K3 允许支成员，工单零代价第 5 枚）
- ③ `not (UserMessageEvidence or HostObservation` → `not (HostObservation`（+336 `or ExplicitFeedback))` ⇒ 剩 `{HostObservation, ExplicitFeedback}`）
- ④ 1/1；⑥ `run-m07/…12_46_27.trx` `passed 61 failed 1`
- ⑦ 红格（唯一 1）：矩阵 `...(ConfirmedAgreement, origin: UserMessageEvidence, expected: None)`；`Expected: True / Actual: False`；红点 `:line 368`
- ⑨ 对平 ✓；⑩ 1 行
- ⑪ 证明 ConfirmedAgreement 允许支的 UME 成员此前无人经过。工单 §〇.5 附注已算到：摘掉后现有 `:350` 松格会因出现第二条同理由拒绝而**照绿**——实测复证：本轮红集里 `Confirmed_agreement_without_any_evidence_reference_is_refused` 未出现（它只 `Assert.Contains`），确只有新矩阵行响应。

**M-08｜`:335` 去掉 `HostObservation`**（K3 成员）
- ③ → `not (UserMessageEvidence`；⑥ `run-m08/…12_46_33.trx` `failed 1`
- ⑦ 红格（唯一 1）：矩阵 `...(ConfirmedAgreement, origin: HostObservation, expected: None)`；`Expected: True / Actual: False`；`:line 368`
- ⑨ 对平 ✓；⑩ 1 行

**M-09｜`:336` 去掉 `or CognitionOrigin.ExplicitFeedback`**（K3 成员，工单零代价第 7 枚）
- ③ 336 `or ExplicitFeedback))` → `))` ⇒ K3 `not (UserMessageEvidence or HostObservation)`
- ⑥ `run-m09/…12_46_39.trx` `failed 1`
- ⑦ 红格（唯一 1）：矩阵 `...(ConfirmedAgreement, origin: ExplicitFeedback, expected: None)`；`Expected: True / Actual: False`；`:line 368`
- ⑨ 对平 ✓；⑩ 1 行
- ⑪ K3 允许支三成员（M-07/08/09）现各有独立单枚红，全部为第一次实测红。

**M-10｜`:342` 整门删**（K4，工单零代价第 8 枚"有格把守但从未跑过红"）
- ③ 删除 `if (Kind == ConfirmedAgreement && EvidenceRefs.Count == 0) { … ConfirmationWithoutUserEvidence … }`（342～346）
- ④ `1 file changed, 5 deletions(-)`；⑥ `run-m10/…12_46_45.trx` `failed 1`
- ⑦ 红格（唯一 1）：`Confirmed_agreement_without_any_evidence_reference_is_refused`（现有 `[Fact]`，工单 §〇.4 的 `:350`）；消息 `Assert.Contains() Failure: Filter not matched in collection | Collection: []`；红点 `:line 391`
- ⑨ 对平 ✓；⑩ 1 行
- ⑪ **该出口第一次实测红**：把守格＝现有松形状 `[Fact]`（原名 `Confirmed_agreement_without_any_evidence_reference_is_refused`，提出波次＝HDS-002 契约刀 `5145bde`）。摘掉 `:342` 后该格 rejections 变空 ⇒ `Assert.Contains` 失败。证明 K4 确被这一格把守且能响。没证明：新矩阵 CA×UME（EvidenceRefs 非空）不受此门影响。

**M-11｜`:350` 从来源集合去掉 `UserMessageEvidence`**（K5 成员，工单零代价第 9 枚）
- ③ `is (UserMessageEvidence or HostObservation` → `is (HostObservation`；⑥ `run-m11/…12_46_51.trx` `failed 1`
- ⑦ 红格（唯一 1）：矩阵 `...(Proposal, origin: UserMessageEvidence, expected: IntentClaimedAsFact)`；`Expected: False / Actual: True`；`:line 368`
- ⑨ 对平 ✓；⑩ 1 行
- ⑪ 证明 Proposal Kind 整门此前全仓无用例经过（工单 §〇.5 第 9～11 枚）；阈值 `.7` 由矩阵夹具 Confidence=.9 命中，未改动。

**M-12｜`:350` 去掉 `HostObservation`**（K5 成员）
- ③ → `is (UserMessageEvidence`；⑥ `run-m12/…12_46_57.trx` `failed 1`
- ⑦ 红格（唯一 1）：矩阵 `...(Proposal, origin: HostObservation, expected: IntentClaimedAsFact)`；`Expected: False / Actual: True`；`:line 368`
- ⑨ 对平 ✓；⑩ 1 行

批次 2 小结：6 轮全部单枚、单轮、清洁，红集恰＝1 格。M-07 复证了工单 §〇.5 关于"现有松格在允许支成员被摘时照绿"的推演（本轮 `:350` 未进红集）。

### 批次 3（M-13 … M-18）

**M-13｜`:351` 去掉 `or CognitionOrigin.ExplicitFeedback`**（K5 成员，工单零代价第 11 枚）
- ③ 351 `or ExplicitFeedback) && Confidence > .7)` → `) && Confidence > .7)` ⇒ K5 `is (UserMessageEvidence or HostObservation) && >.7`
- ④ 1/1；⑥ `run-m13/…12_47_31.trx` `passed 61 failed 1`
- ⑦ 红格（唯一 1）：矩阵 `...(Proposal, origin: ExplicitFeedback, expected: IntentClaimedAsFact)`；`Expected: False / Actual: True`；红点 `:line 368`
- ⑨ 对平 ✓；⑩ 1 行
- ⑪ Proposal 三来源成员（M-11/12/13）各有独立单枚红。

**M-14｜`:359` 去掉 `or CognitionOrigin.HostObservation`**（K6 成员，工单零代价第 12 枚）
- ③ `is UserMessageEvidence or HostObservation)` → `is UserMessageEvidence)`
- ⑥ `run-m14/…12_47_37.trx` `failed 1`；⑦ 红格（唯一 1）：矩阵 `...(CharacterFiction, origin: HostObservation, expected: RealityContamination)`；`Expected: False / Actual: True`；`:line 368`
- ⑨ 对平 ✓；⑩ 1 行
- ⑪ `CharacterFiction × HostObservation` 此前全仓无人经过，本波新行让它会响。

**M-15｜`:367` 从允许集合去掉 `NarrativeProse`**（K7 成员，工单零代价第 13 枚）
- ③ `not (NarrativeProse or ModelSelfReport))` → `not (ModelSelfReport))`
- ⑥ `run-m15/…12_47_43.trx` `failed 1`；⑦ 红格（唯一 1）：矩阵 `...(NarrativeProse, origin: NarrativeProse, expected: None)`；`Expected: True / Actual: False`；`:line 368`
- ⑨ 对平 ✓；⑩ 1 行
- ⑪ NarrativeProse Kind 整门此前全仓无用例经过（工单 §〇.5 第 13～14 枚）。

**M-16｜`:367` 去掉 `or CognitionOrigin.ModelSelfReport`**（K7 成员）
- ③ → `not (NarrativeProse))`；⑥ `run-m16/…12_47_49.trx` `failed 1`
- ⑦ 红格（唯一 1）：矩阵 `...(NarrativeProse, origin: ModelSelfReport, expected: None)`；`Expected: True / Actual: False`；`:line 368`
- ⑨ 对平 ✓；⑩ 1 行

**M-17｜`:275` 整门删**（I1，工单零代价第 15 枚）
- ③ 删除 `if (string.IsNullOrWhiteSpace(intent.SourceCommitId)) { … IntentClaimedAsFact … }`（275～279）
- ④ `1 file changed, 5 deletions(-)`；⑥ `run-m17/…12_47_56.trx` `failed 1`
- ⑦ 红格（唯一 1）：`Intent_without_a_source_commit_is_refused`（本波新 `[Fact]`）；`Expected: 1 / Actual: 0`；红点 `:line 453`（断言文本在源 `:452`）
- ⑨ 对平 ✓；⑩ 1 行
- ⑪ 证明意图"无来源"门此前夹具恒 `batch-1`、全仓无人经过。既有 `Intent_from_another_instance_scope`（守 `:269`）不在红集（它 InstanceId=Beta 且 SourceCommitId 仍 `batch-1`）。

**M-18｜`:281` 整门删**（I2，工单零代价第 16 枚；本轮属"从未拿到实测红"的 M-04/05/18 三枚之一？注：工单 M-18 落在新格，非旧格）
- ③ 删除 `if (EarliestDueAtUtc is {} && LatestDueAtUtc is {} && latest < earliest) { … IntentClaimedAsFact … }`（281～285）
- ④ `1 file changed, 5 deletions(-)`；⑥ `run-m18/…12_48_01.trx` `failed 1`
- ⑦ 红格（唯一 1）：`Intent_whose_deadline_precedes_its_earliest_is_refused`（本波新 `[Fact]`）；`Expected: 1 / Actual: 0`；红点 `:line 463`
- ⑨ 对平 ✓；⑩ 1 行

批次 3 小结：6 轮全部单枚、单轮、清洁，红集恰＝1 格。

### 批次 4（M-19 … M-20）

**M-19｜`:287`～`:288` 整门删**（I3，工单零代价第 17 枚）
- ③ 删除 `if (Status is Fulfilled && Actions.All(a => a.Mode != Immediate || !Equals(a.DeferredIntentId, IntentId))) { … RealityContamination … }`（287～292）
- ④ `1 file changed, 6 deletions(-)`；⑥ `run-m19/…12_48_30.trx` `passed 61 failed 1`
- ⑦ 红格（唯一 1）：`Fulfilled_intent_without_a_matching_delivery_is_refused`（本波新 `[Fact]`）；`Expected: 1 / Actual: 0`；红点 `:line 473`
- ⑨ 对平 ✓；⑩ 1 行

**M-20｜`:359` 去掉 `CognitionOrigin.UserMessageEvidence`（留下 `or … HostObservation`）**（K6 另一成员，落"已有行把守、第一次实测红"）
- ③ `is UserMessageEvidence or HostObservation)` → `is HostObservation)`（M-14 摘 HostObservation、M-20 摘 UserMessageEvidence，同枚门不同成员，两轮分开跑，红格不同，未合并）
- ④ 1/1；⑥ `run-m20/…12_48_37.trx` `passed 61 failed 1`
- ⑦ 红格（唯一 1）：矩阵 `...(CharacterFiction, origin: UserMessageEvidence, expected: RealityContamination)`（＝工单 §〇.4 现有行 `:324`）；`Expected: False / Actual: True`；红点 `:line 368`
- ⑨ 对平 ✓；⑩ 1 行
- ⑪ **该出口第一次实测红**：把守格＝现有 `[InlineData]` 行 `:324`（提出波次 HDS-002 契约刀 `5145bde`），grep 实测此前无在册红，故登记为新凭证。

### T3 总结（20 轮）

| 轮 | 突变 | 红集大小 | 红格 | 清洁单枚 |
|---|---|---|---|---|
| M-01 | :312 整门删 | 1 | Candidate_from_another_instance_scope | ✓ |
| M-02 | :318 去 or>1 | 1 | Confidence_outside(1.5) | ✓ |
| M-03 | :318 去 <0 or | 1 | Confidence_outside(-0.1) | ✓ |
| M-04 | :326 去 NarrativeProse | 1 | 矩阵 URF×NarrativeProse（现有 :321） | ✓ 第一次红 |
| M-05 | :326 去 ModelSelfReport | 1 | 矩阵 URF×ModelSelfReport（现有 :322） | ✓ 第一次红 |
| M-06 | :327 去 Intent | 1 | 矩阵 URF×Intent | ✓ |
| M-07 | :335 去 UserMessageEvidence | 1 | 矩阵 CA×UserMessageEvidence | ✓ |
| M-08 | :335 去 HostObservation | 1 | 矩阵 CA×HostObservation | ✓ |
| M-09 | :336 去 ExplicitFeedback | 1 | 矩阵 CA×ExplicitFeedback | ✓ |
| M-10 | :342 整门删 | 1 | Confirmed_agreement_without_any_evidence（现有 :350） | ✓ 第一次红 |
| M-11 | :350 去 UserMessageEvidence | 1 | 矩阵 Proposal×UserMessageEvidence | ✓ |
| M-12 | :350 去 HostObservation | 1 | 矩阵 Proposal×HostObservation | ✓ |
| M-13 | :351 去 ExplicitFeedback | 1 | 矩阵 Proposal×ExplicitFeedback | ✓ |
| M-14 | :359 去 HostObservation | 1 | 矩阵 CF×HostObservation | ✓ |
| M-15 | :367 去 NarrativeProse | 1 | 矩阵 NP×NarrativeProse | ✓ |
| M-16 | :367 去 ModelSelfReport | 1 | 矩阵 NP×ModelSelfReport | ✓ |
| M-17 | :275 整门删 | 1 | Intent_without_a_source_commit | ✓ |
| M-18 | :281 整门删 | 1 | Intent_whose_deadline_precedes_its_earliest | ✓ |
| M-19 | :287～:288 整门删 | 1 | Fulfilled_intent_without_a_matching_delivery | ✓ |
| M-20 | :359 去 UserMessageEvidence | 1 | 矩阵 CF×UserMessageEvidence（现有 :324） | ✓ 第一次红 |

- **全部 20 轮红集恰＝1 格，无红集多一格，无"取不到红"的轮次。** 17 处零代价成员各拿到第一次会响的凭证（M-01/02/03/06/07/08/09/11/12/13/14/15/16/17/18/19 落在本波新格／新行），加三枚旧格的第一次实测红（M-04 :321／M-05 :322／M-10 :350／M-20 :324——注：M-04/M-05/M-10/M-20 是"现有行/格把守、本波首次跑红"，共 4 轮落旧格，与工单"三枚 M-04/05/18 加 M-20"的点名口径一致，M-18 实际落新格，差异见 §六）。
- 每轮还原后 `NarrativeDraftValidator.cs` 磁盘 SHA-256 ＝ `git show HEAD:` SHA-256 ＝ `88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c`（20/20 逐字节对平）。
- 每轮载体 `git status --short` 行数＝1（仅本波测试文件），src 全程零残留。
- 待拆清单（本波未跑、不进预算）：动作侧 `:189`/`:200`/`:214`/`:223`/`:229`/`:248`、提交侧 `NarrativeCommitValidator` 全部出口、`NarrativeGuard.RequireText` 五处（抛异常型，非 rejections）、`CognitionKind.CharacterBelief` 无条款约束——详见 §一／§〇.5。

---

## 四 T4｜收尾那一轮（顺序照工单）

全部 20 枚突变逐字节还原、`NarrativeDraftValidator.cs` 磁盘哈希＝HEAD 哈希＝`88ebf5c3…b00c`（见 §三，每轮已对平）之后：

1. `git switch -c codex/wave12-20261023`（从 detached `6d6c66d`）
2. `git add tests/Pry.Core.Tests/NarrativeIdentityContractTests.cs`（仅此一份）
3. `git commit --only tests/Pry.Core.Tests/NarrativeIdentityContractTests.cs`（裸 commit 会消费主树他人索引项，故用 `--only`）
4. 解决方案级构建一次 → 全量一次（此时 `--no-build` 允许：刚构建过且载体干净）

### 4.1 交付形状

- 分支：`codex/wave12-20261023`
- 提交全号：**`b98549d9b89ff1db754ab6f18338747f76c64dda`**
- `git show --stat`：`.../NarrativeIdentityContractTests.cs | 99 ++++++++`，**1 file changed, 99 insertions(+)**（只含那一份测试文件 ✓）
- 载体收尾 `git status --short`＝**0 行**；`git stash list`＝**0 条** ✓
- `NarrativeDraftValidator.cs` 磁盘哈希＝`git show HEAD:` 哈希＝`88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c`（逐字节对平 ✓，交付物 `src/` 零改动）
- 主工作树：本波唯一笔迹＝本报告（`git status` 显示为 `?? docs/eval/reports/2026-10-23-qa-wave12-narrative-contract-unheard-exits.md`），他人名下两枚暂存项 `docs/dispatch-tip1020-02-memory-group-judgment.md`／`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs` 开工前后逐行一致、未读未改未提交未 stash 未 clean；主树零 `git add`／零 commit。

### 4.2 构建

预检（#22）`VERDICT=BUILD_OK` → `dotnet build Pry.slnx -c Release`：**0 个错误**、5 个警告（全部是本波新格的 `Assert.Equal(1, …)` 确切计数形状触发的 xUnit2013 建议，非失败、未降低任何断言）。

### 4.3 全量（`--no-build`）

预检（#23）`VERDICT=BUILD_OK`（含一条非阻塞 memory WARN `free_physical_memory=5.9 GB`）。`ls *.trx | wc -l`＝**4**，四份 `<Counters>` 逐份原样：

```
Core  (…_12_50_01.trx): total="558" executed="556" passed="556" failed="0" notExecuted="0"
Api   (…_12_50_02[1].trx): total="280" executed="280" passed="280" failed="0"
App   (…_12_50_03.trx): total="233" executed="233" passed="233" failed="0"
Client(…_12_50_02.trx): total="108" executed="108" passed="107" failed="1"
```

Core 两条按名点出的环境跳过（`outcome="NotExecuted"`，在册同名）＝`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`、`CoreTests.Configured_sense_voice_model_can_run_offline`（`total 558 − executed 556 = 2`）。

### 4.4 判据等式实测回收（与工单预钉值对照）

- 三条判据等式：**`[Fact] 17→21` ✓、`[Theory] 3→4` ✓、`[InlineData] 14→41` ✓**（新增可跑用例 31 条 ✓）
- Core：**`527 → 558`（＋31）✓**、executed 556、passed 556、Core 侧红 0。
- Api 280 ✓／App 233 ✓ 逐位不变。
- Client：预期 108 绿，实测 **108 总计 = 107 绿 + 1 红**。
- **总等式实测**：`558 + 280 + 233 + 108 = 1179`；**绿 = 556+280+233+107 = 1176**，红 = **1**，同名跳过 = 2 ⇒ `1176 + 1 + 2 = 1179`。

### 4.5 与工单预钉值的差异（照盘上实测登记，不动别人的格来凑）

工单 §四 预钉"总 1179 ＝ 1177 绿 ＋ 2 同名跳过 ＋ 0 红"。实测差在 **1 枚 Client 红**，使绿 1177→**1176**、红 0→**1**：

- 红格全名：`Pry.Client.Tests.DeadLoopbackReconnectTests.The_first_subscription_is_a_real_handshake_and_only_later_ones_are_skipped`
- 消息：`Assert.IsType() Failure: Value is null | Expected: typeof(System.Net.Sockets.SocketException) | Actual: null`
- 红点：`tests/Pry.Client.Tests/DeadLoopbackReconnectTests.cs:line 44`
- 判因（读消息形状）：该测试对"死回环端口首连必抛 `SocketException`"的断言取到 `null`（本轮环境里连接没有抛出预期的 `SocketException`）——属**真实网络/套接字环境依赖**用例，与本波判据刀无关（本波零产品代码，只加 `Pry.Core.Tests` 里的 `NarrativeIdentityContractTests.cs`，未碰 `Pry.Client`／`src`）。我**未**动这枚格、未放宽、未跳过、未重跑（保留盘上原样）。在册 §9 历史读数 Client 常为 108 全绿，此枚为环境相关波动；是否在无本波改动时也红＝**未验证**（本波未起对照轮，且预算/范围不允许动它）。

**在册基线 1146／0／2 不由本刀改写，本报告不自封 1179**——以上是载体收尾轮上读到的实测数；顶账由守门人收口时从盘上独立复采。

---

## 五 每次起跑之前的预检（整段登记）

每一次 `dotnet` 调用之前都跑了 `game-preflight6-1022.ps1` 并取 `VERDICT`，共 **23 次**（1 干净态 + 20 突变轮 + 1 构建 + 1 全量），**全部 `VERDICT=BUILD_OK`，0 次 `HOLD`，0 次停刀**。纯读码／grep／git 操作不涉及 `dotnet`，未跑预检（合规）。各轮预检整段原文已随轮贴出：§2.5 干净态 1 段、§三 每轮 1 段（driver 当场打印）、§4.2 构建 1 段、§4.3 全量 1 段（memory WARN 那条非阻塞）。前台抢占属已知代价，读数照贴。

预算终账：

| 项 | 额度 | 已用 | 说明 |
|---|---|---|---|
| 解决方案级构建 | 1 | **1** | `dotnet build Pry.slnx -c Release` |
| 全量 | 1 | **1** | `dotnet test Pry.slnx -c Release --no-build`（紧接构建、载体干净） |
| 按名定向 | ≤22 | **21** | 干净态 1 + 单枚突变 20（预留 1 未动） |
| 机动 | 2 | 0 | 无补跑 |
| 预检次数 | — | 23 | 每次 dotnet 前各一次，全 BUILD_OK |
| dotnet 调用 | — | 23 | 定向轮的隐式单工程重建（Pry.Core＋Pry.Core.Tests）另记不另算 |

未超支、未撞轮次上限，**无"待拆清单"式未跑轮次**（M-01…M-20 全部完成）。

---

## 六 偏差自报（含工单内部不一致与仪器事项，均照盘上登记）

1. **工单 §三 引子与 §三 硬规矩点名清单不一致（就地登记，不涂改工单）**：§三 正文写"三枚已被现有行结构性把守但从未拿到实测红＝M-04／M-05／**M-18**"，但同一节硬规矩又把"第一次实测红"归给"M-04／M-05／M-10／**M-20**"。盘上实测：落在**现有行/现有格且首次跑红**的是 **M-04（:321）、M-05（:322）、M-10（:350）、M-20（:324）共 4 轮**；M-18 落的是本波**新 `[Fact]`**（`Intent_whose_deadline_precedes_its_earliest_is_refused`），并非旧格。故引子里的"M-18"应为"M-20"之误，我按 §三 硬规矩与表执行（20 轮一枚一格未变），并在此明写差异。
2. **Client 环境红 1 枚**（见 §4.5）：非本波改动所致，未处理、未放宽、未重跑；是否在无改动时也红＝未验证。
3. **xUnit2013 警告 5 条**：是本波按 §2.3/§2.2 明确要求的确切计数形状（`Assert.Equal(1, rejections.Count)`）触发，非错误；为守工单"不许 `Assert.Contains`/`>0`/`NotEmpty`"的硬约束而保留，未改成 `Assert.Single`（那会丢掉"确切值＝1"这层证明）。
4. **红线口径澄清（§一 盘上不符）**：工单 §一 点名的第 3 个政策体 `NarrativeActionValidator` 在盘上不存在，动作侧拒绝出口实为 `NarrativeDraftValidator.CheckAction`（私有方法 `:186`～`:257`），`RequireText(action.ActionId)` 在 `:189`；已在 §一 就地登记，未据此新增本波预算外的格。
5. **仪器事项**：首轮 M-01 用的 `trxsum.py` 正则把自闭合的 `<UnitTestResult …/>` 与后续闭合块错配，报 `FAILED_COUNT=0`（而控制台实为 failed 1）；**当场改用区分自闭合/带体两条分支的正则修复并只在已存在的 TRX 上重取，未为此多花一次 `dotnet`**。此后 M-02…M-20 及全量均用修复版解析，读数与控制台一致。
6. **还原纪律**：全程只用 `git checkout -- <具体文件>`，**从未** `git checkout -- .`／reset／clean；他人名下两枚暂存项未碰。
7. **双向性未验证（照 §六.9 明写）**：本波只做"摘成员／删门"方向的 20 枚突变；"往集合加成员（放宽→收紧的反方向）"仅由矩阵 `None` 行结构性把守，**无实测红**，不写成"两侧都已证"。
8. 我的写盘：本报告按批次增量落盘（§〇/§一 → §二 → §三 四批 → §四～§六），未发生覆盖他人文件；主树只写这一份自己的新文件。

### 待拆清单（本波只登记、未拆，交回守门人）

| 出口 | 位置 | 为何今天无格能响（本波未补格，属工单点名 17 枚之外） |
|---|---|---|
| RequireText CandidateId/Claim/ProposedBy | `:306/:308/:309` | 抛异常型、夹具恒非空，无置空用例 |
| RequireText IntentId/Topic | `:266/:267` | 同上 |
| 动作侧 ActionId/段数/即时挂意图/Deferred 段/Deferred 正文/沉默带正文 | `:189/:200/:214/:223/:229/:248` | 无命中 |
| 提交侧 NarrativeCommitValidator 全部出口 | `:387/:389/:395/:403/:410` | `NarrativeIdentityContractTests` 无一枚走提交面 |
| `CognitionKind.CharacterBelief` | `NarrativeDocuments.cs:138` | 两政策体零条款引用⇒六来源全 None，本波不补行（补了不构成把守） |
| Client 那条偶发红（守门人收口加） | `tests/Pry.Client.Tests/DeadLoopbackReconnectTests.cs:44` | lane 收尾轮 1 红（`Actual: null`）对守门人最终线 0 红；本波 `src` 零改动，一红一绿不构成配方，缺的是能稳定复现的输入形状 |

以上不进本波预算，登记后交回。报告完。

---

## 七 守门人收口复证（另一只手，2026-10-23 13:0x，载体 `.local-notes/qa-wave12-close-1023/carrier`）

跑法与读数全部由守门人当场重取，不引用上面 lane 的文字；lane 的报告原文一字未改。

### 7.1 对 lane 那 25 份 TRX 的独立重数（零构建）

- 载体分支 `codex/wave12-20261023` 尾＝`b98549d9b89ff1db754ab6f18338747f76c64dda`；`git show --stat` 只含那一份测试文件；`git diff 6d6c66d..b98549d -- src tests` 只点出 `tests/Pry.Core.Tests/NarrativeIdentityContractTests.cs` ⇒ 工单 §六.1 那句"零产品代码"自证成立。
- 判据条数当场 `grep -c` 复核＝`[Fact] 21`、`[Theory] 4`、`[InlineData] 41`，与工单预钉的 17→21／3→4／14→41 三条等式全中。
- 目录 `.local-notes/qa-wave12-1023/runs/` 实测 22 项（20 枚 `run-mXX` ＋ `run-clean` ＋ `run-full`，另含 lane 自己的 `driver.sh`／`trxsum.py` 两份仪器），`.trx` 共 **25** 份。逐份 `<Counters>` 重数结果：
  - `run-clean`＝total 62 exec 62 pass 62 fail 0；
  - 20 枚 `run-mXX` 全部 total=62 exec=62 pass=61 **fail=1**，红格逐一点名后与工单 §三 那张预期表 **20/20 对平**（下表照盘上 TRX 抄）；
  - `run-full` 四份＝Core 558/556/556/fail 0 ＋ 2 条按名跳过、Api 280、App 233、Client 108 但 **fail=1**（见 §7.3）。
- `src/Pry.Core/Narrative/NarrativeDraftValidator.cs` 三方哈希对平（lane 载体磁盘／lane 载体 HEAD blob／主树磁盘）＝`88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c`。

| 轮 | TRX 里点名的红格（去掉程序集前缀） |
|---|---|
| M-01 | `Candidate_from_another_instance_scope_is_refused` |
| M-02 | `Confidence_outside_the_unit_interval_is_refused(1.5)` |
| M-03 | `Confidence_outside_the_unit_interval_is_refused(-0.1)` |
| M-04 | 矩阵行 `UserReportedFact × NarrativeProse → RealityContamination` |
| M-05 | 矩阵行 `UserReportedFact × ModelSelfReport → RealityContamination` |
| M-06 | 矩阵行 `UserReportedFact × Intent → RealityContamination` |
| M-07 | 矩阵行 `ConfirmedAgreement × UserMessageEvidence → None` |
| M-08 | 矩阵行 `ConfirmedAgreement × HostObservation → None` |
| M-09 | 矩阵行 `ConfirmedAgreement × ExplicitFeedback → None` |
| M-10 | 已有格 `Confirmed_agreement_without_any_evidence_reference_is_refused` |
| M-11 | 矩阵行 `Proposal × UserMessageEvidence → IntentClaimedAsFact` |
| M-12 | 矩阵行 `Proposal × HostObservation → IntentClaimedAsFact` |
| M-13 | 矩阵行 `Proposal × ExplicitFeedback → IntentClaimedAsFact` |
| M-14 | 矩阵行 `CharacterFiction × HostObservation → RealityContamination` |
| M-15 | 矩阵行 `NarrativeProse × NarrativeProse → None` |
| M-16 | 矩阵行 `NarrativeProse × ModelSelfReport → None` |
| M-17 | `Intent_without_a_source_commit_is_refused` |
| M-18 | `Intent_whose_deadline_precedes_its_earliest_is_refused` |
| M-19 | `Fulfilled_intent_without_a_matching_delivery_is_refused` |
| M-20 | 矩阵行 `CharacterFiction × UserMessageEvidence → RealityContamination` |

### 7.2 我这一轮自己查出来的一枚回归，当场修掉并重新取红

- 归位（`c7f52ea`）之后，我在自己另建的 `--detach` 副本起那一次解决方案构建，读数＝**已成功生成，0 个错误，5 个警告**，位置全在 `NarrativeIdentityContractTests.cs(422,443,453,463,473)`，类型全是 `xUnit2013`（不许用 `Assert.Equal` 比集合大小）。根因是工单 §二.3 那句"断言必须是确切的：`rejections.Count` 的确切值"被落地成 `Assert.Equal(1, rejections.Count)`。**这笔账记在本波头上**：第十一波那轮构建读数是 0 个警告 0 个错误，本波把它变成了 5 条。
- 修法＝同一方法内替换，不新增也不删格：`var rejection = Assert.Single(rejections);` ＋ `Assert.Equal(<点名理由>, rejection.Reason);`。`Assert.Single` 对"0 条"和"≥2 条"都红，"恰好一条"这层确切性一点没丢，而且与全仓既有写法一致（`tests/Pry.Api.Tests/ApplicationServiceTests.cs` 等处一直用它）。
- 换了断言形状＝换了一枚门，必须重新响一次，否则新形状没有非空转证据。守门人自跑五轮（一轮一枚，五处改动各覆盖一枚；跑前各预检一次，全部 `VERDICT=BUILD_OK`；突变轮一律不加 `--no-build`）：

| 轮 | 被改文件:行（before→after 取自当场 `git diff`） | TRX（目录 `.local-notes/qa-wave12-close-1023/runs/<轮名>/`） | `<Counters>` | 红格与红点锚 | 还原后 sha256 对平 |
|---|---|---|---|---|---|
| 干净态 | 无突变（只有断言形状改动） | `00-clean-single-fix/…13_02_05.trx` | 62/62/62/fail 0 | 无红 | — |
| M-01 | `NarrativeDraftValidator.cs:312` 整门删（6 行） | `M-01/…13_03_42.trx` | 62/62/61/fail 1 | `Candidate_from_another_instance_scope_is_refused`，TRX 锚 `NarrativeIdentityContractTests.cs:442`，盘上 assert 文本在 `:443` | `88ebf5c3…b00c` ＝ HEAD，逐字节对平 |
| M-02 | `:318` `is < 0 or > 1` → `is < 0` | `M-02/…13_03_48.trx` | 62/62/61/fail 1 | `Confidence_outside_the_unit_interval_is_refused(1.5)`，TRX 锚 `:421`，盘上 `:422` | 同上 |
| M-17 | `:275` 整门删（6 行） | `M-17/…13_03_54.trx` | 62/62/61/fail 1 | `Intent_without_a_source_commit_is_refused`，TRX 锚 `:452`，盘上 `:453` | 同上 |
| M-18 | `:281` 整门删（6 行） | `M-18/…13_03_59.trx` | 62/62/61/fail 1 | `Intent_whose_deadline_precedes_its_earliest_is_refused`，TRX 锚 `:462`，盘上 `:463` | 同上 |
| M-19 | `:287`～`:288` 整门删（7 行） | `M-19/…13_04_06.trx` | 62/62/61/fail 1 | `Fulfilled_intent_without_a_matching_delivery_is_refused`，TRX 锚 `:472`，盘上 `:473` | 同上 |

- 五轮消息形状全部是 `Assert.Single() Failure: The collection was empty`——即"门拆掉之后这格确实没接到拒绝"，不是抛在别处。每轮 `git diff --name-only` 只含 `src` 那一枚 ＋ 我自己的测试文件；还原＝`git checkout -- src/Pry.Core/Narrative/NarrativeDraftValidator.cs`，还原后 `git status --short` 只剩 1 行（那份测试文件），载体里他人名下零笔迹。
- 仪器事项照盘登记：TRX 报的行号比盘上 `Assert.Single` 那一行小 1，五轮同向；照抄 TRX 原样，不改读、不动别人格。
- 归位刀＝`a5d9b80`，`1 file changed, 10 insertions(+), 10 deletions(-)`，只含那一份测试文件；`src` 零改动。修完之后构建读数还回 **0 个警告 0 个错误**。

### 7.3 lane 那一枚 Client 红：我这只手复现不出来

- 名＝`Pry.Client.Tests.DeadLoopbackReconnectTests.The_first_subscription_is_a_real_handshake_and_only_later_ones_are_skipped`；lane 那轮在 `DeadLoopbackReconnectTests.cs:44` 拿到 `Assert.IsType() Failure: Expected typeof(System.Net.Sockets.SocketException) / Actual: null`。
- 我在最终线（`c7f52ea` ＋ 本次形状修复）重跑全量：Client＝**108/108，fail 0**，该格为绿。
- 判定与本波的关系：`git diff 6d6c66d..a5d9b80 -- src` 为空，本波零产品代码，那一红不可能是本波的刀造成的；两次读数一红一绿之间只剩"对死回环端口起 socket"这类环境项可动。**登记为未定性的环境依赖读数，不开缺陷刀、也不写"已排除"**：要拆它得先有稳定复现的配方，1 红 1 绿不构成配方。已加进待拆清单。

### 7.4 顶账与基线（只报我读到的数）

- 我这只手在最终线上的全量（`runs/full-resample-close`，四份 `.trx`，`ls *.trx | wc -l` 实测＝4）＝Core total 558 exec 556 pass 556 fail 0 ＋ 两条按名点出的环境跳过（`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`、`CoreTests.Configured_sense_voice_model_can_run_offline`）、Api 280、App 233、Client 108 ⇒ **总 1179 ＝ 1177 绿 ＋ 2 条同名跳过 ＋ 0 红**，与工单 §四 预钉的等式逐位一致。
- 第十一波欠的那一格（"独立复采押到下一波本来就要起构建时，把收尾全量跑在归位后的线尾副本上"）由这一轮还上：写判据的是 lane，跑这一轮的是守门人，副本另建、构建自建。在册基线因此由 **1146／0／2** 顶到 **1179／0／2**，四项分项＝Client 108／App 233／Api 280／Core 558（exec 556、pass 556）。
- 差一层要说清：`1146／0／2` 那轮是两只手各跑一遍、四份 TRX 逐位相同；这一轮只有守门人一只手。第二只手**不单独起构建**，押在下一波本来要起构建时顺带复采。
- `verify-repository` 那个数是受跟踪文件条数，不是门槛；本波收口这一格由守门人在报告入库后当场实测，见 §9 交接。

### 7.5 就地更正两处（守门人自己的错，工单原文不涂）

- 工单 §三 那句"三枚今天已被现有行结构性把守、但从未拿到实测红的成员各一轮（M-04／M-05／M-18）"里，第三枚应是 **M-20**（`:359` 去掉 `CognitionOrigin.UserMessageEvidence`，把守的是已有行 `CharacterFiction × UserMessageEvidence`）。`M-18` 是 `:281` 那枚整门的新格，不属"已有行"。已在工单原文下方就地加注，原文照留。
- 工单 header 的"出闸时间：2026-10-23 01:5x"，以及 §9 与一页纸 §0 里那两处"02:0x"都写错了。当场 `git log --date=format:%H:%M` 实测＝工单 `a4f89a6` **12:30**、登记 `de4fbe2` **12:34**、lane 跑在 **12:35～12:50**、归位 `c7f52ea` **12:55**、形状修复 `a5d9b80` **13:06**。根因：我用了被压缩的会话摘要里的钟，没按自己的规矩先当场从盘上取时点。

### 7.6 本波收口时仍标为未验证的项

1. 放宽方向（往允许集合加成员）没有实测红，只有矩阵 `None` 行结构性把守。
2. §7.3 那枚 Client 红的复现性（一次红、一次绿，都不是配方）。
3. `1179／0／2` 尚无第二只手复采（只在守门人这一只手上成立）。

报告收口完。
