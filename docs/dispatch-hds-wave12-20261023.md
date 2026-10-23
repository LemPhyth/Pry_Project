# 第十二波工单·HDS-002 契约面：把 17 处"改坏了今天零代价"的判定成员各钉一行会响的判据，再逐枚取单枚清洁红

- 工单 id：`HDS-WAVE12-1023`
- 作者：守门人（架构／集成／发行）
- 出闸时间：2026-10-23 01:5x
- 载体基点：**`6d6c66d`**（本工单落地前的线尾）
- 执行方：冷启动 lane（除本工单与 `AGENTS.md` 四份必读之外无上下文）
- 性质：**判据刀，零产品代码**。这一波不新增功能、不改她的任何行为、不动任何阈值，只把"改坏了没人报警"的那些判定变成"改坏了当场报警"。

---

## 〇 开工前必须当场做的三件事（做完才许动任何文件）

1. `git -C C:/Users/24782/Desktop/Pry_Project status --short --branch`——把已有未提交改动当作别人的资产：不读内容、不改、不提交、不 `stash`、不 `clean`。当前已知他人名下两枚暂存项＝`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`，**这两枚本波一律不碰**。
2. 建独立载体（**不动主工作树**）：
   `git -C C:/Users/24782/Desktop/Pry_Project worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave12-1023/carrier 6d6c66d`
   入场两行判据原样贴进报告：`git status --short | wc -l` 应为 **0**，`git rev-parse HEAD` 应为 `6d6c66d` 的全号。
3. **起点成立的代码面判据**：`git -C <主树> diff --name-only 6d6c66d..<本工单那把提交> -- src tests` 必须为**空**（本波出闸是纯文档刀）。若非空，停刀回报，不要自己判断。

### 〇.4 锚点读回（不许引用本工单的行号直接动手）

下面每一格我给的行号与条件原文，是我在 `6d6c66d` 上逐条读过的；**载体里必须再逐条 `sed -n '<a>,<b>p' | cat -n` 读回原文才许动手**。读不回原文的那一枚，登记"锚点不符"并停该枚，不许找一个"看起来等价"的位置改。

`src/Pry.Core/Narrative/NarrativeDraftValidator.cs`（`NarrativeIntentPolicy.Validate` 与 `KnowledgeCandidatePolicy.Validate` 两个政策体）：

| 编号 | 行 | 条件原文（我读到的） | 拒绝理由 |
|---|---|---|---|
| S | `:312` | `if (!candidate.InstanceId.Equals(content.InstanceId))` | `ScopeEscalation` |
| K1 | `:318` | `if (candidate.Confidence is < 0 or > 1)` | `RealityContamination` |
| K2 | `:325`～`:327` | `Kind == UserReportedFact && Origin is NarrativeProse or ModelSelfReport or Intent` | `RealityContamination` |
| K3 | `:334`～`:336` | `Kind == ConfirmedAgreement && Origin is not (UserMessageEvidence or HostObservation or ExplicitFeedback)` | `ConfirmationWithoutUserEvidence` |
| K4 | `:342` | `Kind == ConfirmedAgreement && EvidenceRefs.Count == 0` | `ConfirmationWithoutUserEvidence` |
| K5 | `:349`～`:351` | `Kind == Proposal && Origin is (UserMessageEvidence or HostObservation or ExplicitFeedback) && Confidence > .7` | `IntentClaimedAsFact` |
| K6 | `:358`～`:359` | `Kind == CharacterFiction && Origin is UserMessageEvidence or HostObservation` | `RealityContamination` |
| K7 | `:366`～`:367` | `Kind == NarrativeProse && Origin is not (NarrativeProse or ModelSelfReport)` | `RealityContamination` |
| I1 | `:275` | `if (string.IsNullOrWhiteSpace(intent.SourceCommitId))` | `IntentClaimedAsFact` |
| I2 | `:281` | `if (intent.EarliestDueAtUtc is { } earliest && intent.LatestDueAtUtc is { } latest && latest < earliest)` | `IntentClaimedAsFact` |
| I3 | `:287`～`:288` | `if (intent.Status is Fulfilled && content.Actions.All(a => a.Mode != Immediate || !string.Equals(a.DeferredIntentId, intent.IntentId, Ordinal)))` | `RealityContamination` |

判据侧现状（同一把尺，我在 `6d6c66d` 上当场数过并读回原文的）：`tests/Pry.Core.Tests/NarrativeIdentityContractTests.cs` ＝ **441 行、17 个 `[Fact]`、3 个 `[Theory]`、14 行 `[InlineData]`**。与本波有关的三处：

- `Knowledge_candidate_origin_decides_what_it_may_assert`（`:320`～`:348`）＝ **5 行** `[InlineData]`（`:321`～`:325`），夹具 `Confidence = .9`、`EvidenceRefs = ["msg-102"]`、`InstanceId = draft.InstanceId`，直接调 `KnowledgeCandidatePolicy.Validate(candidate, NarrativeContent.From(draft))`；断言形状＝`Assert.Equal(expected == None, rejections.Count == 0)` ＋（`expected != None` 时）`Assert.Equal(expected, rejections[0].Reason)`。
- `Confirmed_agreement_without_any_evidence_reference_is_refused`（`:350`～`:367`）＝ K4 现有唯一把守，形状是**松的** `Assert.Contains(...)`。
- `Intent_from_another_instance_scope_is_refused`（`:369`～`:376`）＝ `NarrativeIntentPolicy` 今天只有这一枚 `[Fact]`，守的是 `:269` 那条 `ScopeEscalation`（也是 `Assert.Contains`）。夹具意图（`:421`～`:429`）＝`Status = Pending`、`SourceCommitId = "batch-1"`、`EarliestDueAtUtc = Now.AddMinutes(20)`、`LatestDueAtUtc` 未设。

枚举面（`src/Pry.Core/Narrative/NarrativeDocuments.cs`）：`CognitionKind` **6 个**（`UserReportedFact=0`／`CharacterFiction=1`／`CharacterBelief=2`／`Proposal=3`／`ConfirmedAgreement=4`／`NarrativeProse=5`，`:131`～`:146`），`CognitionOrigin` **6 个**（`UserMessageEvidence=0`／`HostObservation=1`／`ExplicitFeedback=2`／`NarrativeProse=3`／`ModelSelfReport=4`／`Intent=5`，`:148`～`:156`）。

### 〇.5 本波要补的空档（不是推演，是我按上表逐枚数出来的）

下面 17 处判定成员，今天**没有任何一条用例会在它们被改坏时变红**（改坏＝零代价）：

| # | 改坏的方式 | 为什么今天不响 |
|---|---|---|
| 1 | `:312` 整门删 | 没有任何用例把候选的 `InstanceId` 设成与 content 不同 |
| 2～3 | `:318` 去掉 `< 0`；去掉 `> 1` | 夹具 `Confidence` 恒 `.9`（未设时默认 `.5`），两侧边界全仓无人经过 |
| 4 | `:327` 去掉 `or CognitionOrigin.Intent` | `UserReportedFact × Intent` 无行 |
| 5～7 | `:335`～`:336` 从允许集合里去掉 `UserMessageEvidence`／`HostObservation`／`ExplicitFeedback` | `ConfirmedAgreement` 只有 `ModelSelfReport` 那一行（`:323`，走的是拒绝支），**允许支三枚成员全无人经过** |
| 8 | `:342` 整门删 | 有格把守（`:350`）但形状是 `Assert.Contains`；本波仍要第一次拿到它的实测红 |
| 9～11 | `:350`～`:351` 从来源集合里去掉 `UserMessageEvidence`／`HostObservation`／`ExplicitFeedback` | `CognitionKind.Proposal` 在全仓**没有任何一行用例经过**（整门也没人证过） |
| 12 | `:359` 去掉 `or CognitionOrigin.HostObservation` | `CharacterFiction × HostObservation` 无行（`UserMessageEvidence` 那行有＝`:324`） |
| 13～14 | `:367` 从允许集合里去掉 `NarrativeProse`／`ModelSelfReport` | `CognitionKind.NarrativeProse` 在全仓**没有任何一行用例经过** |
| 15 | `:275` 整门删 | 夹具 `SourceCommitId` 恒 `"batch-1"` |
| 16 | `:281` 整门删 | 夹具从不设 `LatestDueAtUtc` |
| 17 | `:287`～`:288` 整门删 | 夹具 `Status` 恒 `Pending`，`Fulfilled` 支无人经过 |

**本波不拆、只登记的两件事**（写进报告，不要自己扩大范围）：

- `CognitionKind.CharacterBelief`（角色的信念或推测，`NarrativeDocuments.cs:138`）在两个政策体里**没有任何一条条款引用它**⇒ 六个来源全落 `None`。本波**不加它的行**（加了也不构成任何出口的把守），只把"这个 Kind 今天没有任何政策约束"作为发现登记，归属下一波的候选。
- T1 出口表里若还有我上面没点名的无命中出口（`NarrativeGuard.RequireText` 那五处、`NarrativeActionValidator` 的 `switch (action.Mode)` 三段、`NarrativeCommitValidator` 全部、`:269`／`:312` 之外的作用域条款），一律列进**待拆清单**交回，**不进本波预算，不许顺手加格**。

## 一 T1｜先把出口表做全（读码，零构建）

在我点名的 11 枚之外，把 `KnowledgeCandidatePolicy.Validate`、`NarrativeIntentPolicy.Validate`、`NarrativeActionValidator`、`NarrativeCommitValidator` 四个政策体里的**每一条拒绝出口与每一枚 `NarrativeGuard.RequireText/Require…`** 列成表（行号＋条件原文＋理由＋"被哪一条用例的哪一行命中／无命中"）。

- 只列，不改，不跑。
- 我点名的 11 枚若在你的表里对不上，**以盘上原文为准**，并在报告 §一 明写"工单点名的第 N 枚与盘上不符，实际是……"——不涂改本工单，就地登记。
- 命中判定必须给证据（测试文件名＋行号＋那条 `Assert` 的原文），不许用"应该有覆盖"这类话。

## 二 T2｜落判据（追加，不改不删任何既有断言、不改既有方法体）

### 2.1 矩阵面：把 `Knowledge_candidate_origin_decides_what_it_may_assert` 补成"五类 × 六来源"的完整表

只**追加** `[InlineData]` 行，方法体与现有 5 行一个字不动。范围＝`UserReportedFact`／`CharacterFiction`／`Proposal`／`ConfirmedAgreement`／`NarrativeProse` 五类 Kind × 六枚 Origin ＝ 30 格，现有 5 格 ⇒ **新增 25 行**。我按下述夹具条件（`Confidence = .9`、`EvidenceRefs = ["msg-102"]`、同实例）推演出的期望表，**以盘上政策原文为准**：

| Kind ＼ Origin | UserMessageEvidence | HostObservation | ExplicitFeedback | NarrativeProse | ModelSelfReport | Intent |
|---|---|---|---|---|---|---|
| UserReportedFact | None ＠已有 | None | None | RealityContamination ＠已有 | RealityContamination ＠已有 | RealityContamination |
| CharacterFiction | RealityContamination ＠已有 | RealityContamination | None | None | None | None |
| Proposal | IntentClaimedAsFact | IntentClaimedAsFact | IntentClaimedAsFact | None | None | None |
| ConfirmedAgreement | None | None | None | ConfirmationWithoutUserEvidence | ConfirmationWithoutUserEvidence ＠已有 | ConfirmationWithoutUserEvidence |
| NarrativeProse | RealityContamination | RealityContamination | RealityContamination | None | None | RealityContamination |

落码要求与异常处理：

1. 每一格都要有行（已有 5 格不重复加）。行序照 Kind→Origin 的枚举值排，便于逐行对账。
2. **干净态若某新增行红**：先按盘上原文判定是"工单推演错"还是"政策行为与注释意图不符"。前者⇒把该行的期望改成盘上原文可推出的值，并在报告里逐字写出"工单推演＝X／盘上政策实际＝Y／依据行号"；后者⇒**停该行、不改期望**，登记为发现（附红格全名＋消息原文），继续其余行。两者都不许删行、不许放宽断言。
3. 现有两行断言的形状（`Assert.Equal(expected == None, rejections.Count == 0)` ＋ 点名 `rejections[0].Reason`）**不许动**；本波不靠它证明计数确切，它证明的是"这一格的拒绝有无与首条理由"。

### 2.2 新建一枚 `[Theory]` 守 `:318` 的两侧边界

`Confidence_outside_the_unit_interval_is_refused(double confidence)`，**2 行** `[InlineData]`：`1.5` 与 `-0.1`，两侧都期望 `RealityContamination`。夹具照 2.1 那一格的形状（Kind＝`UserReportedFact`、Origin＝`UserMessageEvidence`，让别的门全不响）。断言必须确切：`Assert.Equal(1, rejections.Count)` ＋ `Assert.Equal(NarrativeContractReason.RealityContamination, rejections[0].Reason)`。

### 2.3 新建四枚 `[Fact]`

命名照该文件既有的动宾风格，夹具照 2.1／夹具原文，**每枚都断言 `rejections.Count` 的确切值与逐条 `Reason` 的等值断言**（不许 `Assert.Contains`、不许 `> 0`／`Assert.NotEmpty`）：

| 新格 | 守哪一枚 | 夹具怎么造（其余门必须全不响） | 确切期望 |
|---|---|---|---|
| `Candidate_from_another_instance_scope_is_refused` | `:312` | 照 `:350` 那枚的形状，但 `InstanceId = Beta`、Kind/Origin 取 `UserReportedFact × UserMessageEvidence`（该行在 2.1 表里是 `None`） | 1 条，`ScopeEscalation` |
| `Intent_without_a_source_commit_is_refused` | `:275` | `draft.Intents[0] with { SourceCommitId = "" }` | 1 条，`IntentClaimedAsFact` |
| `Intent_whose_deadline_precedes_its_earliest_is_refused` | `:281` | `with { LatestDueAtUtc = Now.AddMinutes(10) }`（夹具 `Earliest` ＝ `Now.AddMinutes(20)`） | 1 条，`IntentClaimedAsFact` |
| `Fulfilled_intent_without_a_matching_delivery_is_refused` | `:287` | `with { Status = NarrativeIntentStatus.Fulfilled }`（夹具那枚动作是 `Immediate`、`DeferredIntentId` 为空） | 1 条，`RealityContamination` |

三枚都调 `NarrativeIntentPolicy.Validate(intent, NarrativeContent.From(draft))`（直调政策，别走 `NarrativeDraftValidator.Validate`，否则会把动作侧的拒绝一起卷进来）。

### 2.4 判据条数等式（落码前后各数一次，原样贴进报告；取不到就登记差异，不许改判据凑等式）

```
grep -c '\[Fact\]' tests/Pry.Core.Tests/NarrativeIdentityContractTests.cs   预期 17 → 21（+4）
grep -c '\[Theory\]' <同一文件>                                             预期 3 → 4（+1）
grep -c '\[InlineData' <同一文件>                                           预期 14 → 41（+25 矩阵行 +2 边界行）
```

⇒ 新增可跑用例 **31 条**（25＋2＋4）。阈值不许动：K5 的 `.7` 是政策标定值，只能加用例去命中它，**不许改它、不许在注释里提议改它**。

### 2.5 干净态必须全绿（正向探针，进 T3 的前置门）

预检之后跑一轮，`--filter` 只跑这一个类：

```
dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~NarrativeIdentityContractTests" --results-directory "<绝对路径>\run-clean" --logger trx
```

`<Counters>` 原样贴出，**`failed` 必须为 0**，并且 §2.1～§2.3 新增的每一格都要在通过列表里按名点名出现（TRX 里 theory 行会展开成 `方法(kind: origin, ...)` 之类的名字，按名贴出至少 31 条）。这一步没绿就不许进 T3。

## 三 T3｜逐枚单放突变，各取一枚"单枚、单轮、清洁"的红（共 20 轮）

每轮只改 `src/Pry.Core/Narrative/NarrativeDraftValidator.cs` 的**一枚**（其余一字不动），跑法与 2.5 同一条 `--filter`，TRX 目录一名一轮（`run-m01`…`run-m20`），跑完立刻逐字节还原。

**20 轮怎么来的**：上面点名的 17 处零代价成员各一轮（17）＋三枚今天已被现有行结构性把守、但从未拿到实测红的成员各一轮（M-04／M-05／M-18）＝20。一轮＝一枚集合成员或一枚整门；整门删与逐成员删不重复跑（把一枚门的所有成员各摘一次已经覆盖"整门消失"的行为差集）。

| 轮 | 突变（单枚） | 预期红集＝恰好 1 格 |
|---|---|---|
| M-01 | `:312` 整门删 | `Candidate_from_another_instance_scope_is_refused` |
| M-02 | `:318` 去掉 `or > 1`（剩 `is < 0`） | 新行 `Confidence_outside…(1.5)` |
| M-03 | `:318` 去掉 `< 0 or`（剩 `is > 1`） | 新行 `Confidence_outside…(-0.1)` |
| M-04 | `:326` 去掉 `CognitionOrigin.NarrativeProse` | 已有行 `UserReportedFact × NarrativeProse`（`:321`） |
| M-05 | `:326` 去掉 `or CognitionOrigin.ModelSelfReport` | 已有行 `UserReportedFact × ModelSelfReport`（`:322`） |
| M-06 | `:327` 去掉 `or CognitionOrigin.Intent` | 新行 `UserReportedFact × Intent` |
| M-07 | `:335` 从允许集合去掉 `UserMessageEvidence` | 新行 `ConfirmedAgreement × UserMessageEvidence` |
| M-08 | `:335` 从允许集合去掉 `CognitionOrigin.HostObservation` | 新行 `ConfirmedAgreement × HostObservation` |
| M-09 | `:336` 去掉 `or CognitionOrigin.ExplicitFeedback` | 新行 `ConfirmedAgreement × ExplicitFeedback` |
| M-10 | `:342` 整门删 | 已有格 `Confirmed_agreement_without_any_evidence_reference_is_refused`（`:350`） |
| M-11 | `:350` 从来源集合去掉 `CognitionOrigin.UserMessageEvidence` | 新行 `Proposal × UserMessageEvidence` |
| M-12 | `:350` 去掉 `or CognitionOrigin.HostObservation` | 新行 `Proposal × HostObservation` |
| M-13 | `:351` 去掉 `or CognitionOrigin.ExplicitFeedback` | 新行 `Proposal × ExplicitFeedback` |
| M-14 | `:359` 去掉 `or CognitionOrigin.HostObservation` | 新行 `CharacterFiction × HostObservation` |
| M-15 | `:367` 从允许集合去掉 `CognitionOrigin.NarrativeProse` | 新行 `NarrativeProse × NarrativeProse` |
| M-16 | `:367` 去掉 `or CognitionOrigin.ModelSelfReport` | 新行 `NarrativeProse × ModelSelfReport` |
| M-17 | `:275` 整门删 | `Intent_without_a_source_commit_is_refused` |
| M-18 | `:281` 整门删 | `Intent_whose_deadline_precedes_its_earliest_is_refused` |
| M-19 | `:287`～`:288` 整门删 | `Fulfilled_intent_without_a_matching_delivery_is_refused` |
| M-20 | `:359` 去掉 `CognitionOrigin.UserMessageEvidence`（留下 `or … HostObservation`） | 已有行 `CharacterFiction × UserMessageEvidence`（`:324`） |

M-20 排在 M-14 之后跑，两轮预期红格不同（M-14＝新行 `CF × HostObservation`，M-20＝已有行 `:324`），**不许合并成一轮**。

硬规矩（违反则该轮凭证作废，别拿推测补）：

- **一轮只放一枚**。批次红只能证明"这几枚里至少一枚能打红这一格"，不许署成某一枚的凭证。红集若比预期多一格，照盘上登记并读消息形状判因（`Assert.Equal` 还是抛错、红点落在哪一行），**不改断言、不放宽、不加第二枚去凑**。
- 取不到的那一格（某枚改坏后该类仍全绿）＝**这就是本波要登记的空档**，写"该出口改坏后无格能响"并附 `<Counters>` 原文，不许删格、不许写成"不需要拆"、也不许去改别人的格来制造红。
- **既有格不重复领凭证**：动笔之前先在 `docs/eval/reports/` 与 `docs/dispatch-*.md` 里 `grep` 该文件名（`NarrativeDraftValidator.cs`）确认是否已有在册突变凭证。我出闸前实测＝**0 处提及**（这一整个文件今天没有任何一枚在册红），所以 M-04／M-05／M-10／M-20 那四轮落在"已有行／已有格"上，属**该出口的第一次实测红**（把守的格在前、红没跑过），照新凭证登记并注明把守格的原名与提出波次；若你 grep 到我没找到的在册凭证，就地更正并写明出处。
- 还原＝`git checkout -- src/Pry.Core/Narrative/NarrativeDraftValidator.cs`（**绝不 `git checkout -- .`**）。每轮还原后 `sha256sum` 该文件与 `git show HEAD:src/Pry.Core/Narrative/NarrativeDraftValidator.cs | sha256sum` 必须**逐字节对平**，两个哈希都贴出来；`git status --short` 每轮只应剩你自己那一份测试文件的改动。
- **允许用一个驱动脚本连跑多轮**（省轮次上限），但脚本必须逐轮打印：当场 `git diff -U0` 原文、该轮完整跑法命令行、TRX 文件名、`<Counters>` 原文、红格全名＋消息＋红点行号、还原命令、还原前后两个 SHA-256。凭证只从脚本打印的原文抄，**不许事后凭记忆补写**；脚本任一轮异常或预检 `HOLD` 就停在该轮并报告已完成轮次，不许跳过继续。单脚本连跑 **≤6 轮**。
- 每一轮的**十一项字段**齐全（载体提交号／被改文件:行／before→after 原文／当场 `git diff --stat`／跑法整行命令／TRX 文件名／红格全名＋消息要点＋红点行号／还原方式／还原前后 SHA-256／还原后 `git status --short` 行数／结论"证明了什么·没证明什么·与谁共用凭证"）。
- TRX 规则照旧：`--results-directory "<绝对路径>"` **长参** ＋ `--logger trx`，一批一名不复用目录；跑完先数 `ls *.trx` 再读 `<Counters>`。**突变轮绝不加 `--no-build`**（前案：加了就跑旧二进制，绿灯是假的）。
- 报红点锚一律照抄 TRX 那一行（那是断言调用处），另起一栏写"源码里 assert 文本在哪行"，两者不等不构成疑点。

## 四 T4｜收尾那一轮（顺序不许换）

全部突变逐字节还原并哈希对平 ⇒ 在载体上 `git switch -c codex/wave12-20261023` ⇒ **只** `git add tests/Pry.Core.Tests/NarrativeIdentityContractTests.cs`（若你确实需要动第二个文件，先停刀回报，不要自己决定）⇒ `git commit --only <同一份路径>`（主树里他人名下有暂存项，裸 `git commit` 会消费别人的索引项）⇒ 解决方案级构建一次 ⇒ 全量一次（`--no-build` 此时才允许，因为紧接着构建且载体干净）。

全量读数要求：`ls *.trx | wc -l` ＝ **4**，四份 `<Counters>` 逐份原样贴出，并写出总等式。**预期（取不到就登记差异，不许动别人的格来凑）**：Core `total 527 → 558`（＋31 条新用例；`executed 556`、`passed 556`、红 0、按名点出的 `NotExecuted` 仍＝那两条环境跳过），Api 280／Client 108／App 233 逐位不变 ⇒ 总 **1179 ＝ 1177 绿 ＋ 2 条同名跳过 ＋ 0 红**。

**在册基线 1146／0／2 不由本刀改写，也不由你自封 1179**——你只报你在载体上读到的数，顶账由守门人收口时做（他会另建副本独立复采一轮）。

预算硬顶（超支就停刀回报，不要自己追加）：解决方案级构建 **1**／全量 **1**／按名定向 **≤22**（2.5 干净态 1 ＋ T3 二十轮突变 20 ＋ 预留 1）／机动 **2**。定向轮的隐式单工程重建另记不另算。**若额度或轮次上限用尽：把未跑的轮次列成"待拆清单"（轮号＋突变＋预期格）原样交回，绝不为了"跑完"用批次突变冒充单枚、也不压缩字段。**

## 五 每次起跑之前（这条不打折）

**每一次 `dotnet` 调用之前**，先跑焦点预检并把整段原文贴进报告：

```
powershell -NoProfile -ExecutionPolicy Bypass -File .local-notes/game-preflight6-1022.ps1
```

- `VERDICT=BUILD_OK` 才许起跑；**`HOLD` 就停下**，不请示、不变通、不换目录、不"先跑一个小的"，直接在该轮报告里写"HOLD，停刀"并继续完成不需要构建的部分（读码、落判据）。
- 预检的输出会抢一次前台（约一秒），这是已知代价；不要在报告里替作者解释她机器上的现象，只贴读数。
- 纯读码与纯文档不需要预检。

## 六 不许越界的线（本波的边界）

1. **零产品代码**。`src/**` 只允许被临时改坏再逐字节还原；交付物里 `src/` 必须零改动（当场用 `git diff --stat HEAD -- src` 自证为空）。
2. 不许放宽、删除、注释、跳过任何既有断言；不许给既有 `[Theory]` 的现有行改期望值、不许改既有方法体；不许加 `Skip=`。
3. 数字门槛零改动：`.7`（政策置信阈值）、`.5`（`Confidence` 默认值与检索地板）、`2400`、五档超时、`MinimumUsableTokensPerSecond = 12`、`4096`、`DefaultSafetyTokens = 128`、`MinimumInputTokens = 512`、`docs/resource-limit-ledger.md` 全部不碰。
4. **W2 那一格照旧欠着**（`ContextWindowLoadTests.Every_downgrade_candidate_keeps_a_positive_input_budget`）——本波与它无关，不许动它、不许把它算进任何账。拆法原文那本账现值＝分母 46／已付 45／欠 1＝W2；本波新登记的"改坏无格能响"出口写进你自己的新表，**不要拿去改那本账的分母**（那本账只记第六波盘上红去重）。
5. 只在载体里写。主工作树唯一允许出现的本波笔迹＝你的报告（由守门人入库），你**不在主树做任何 `git add`／`commit`**。
6. 零 `git tag`／零 `git push`／零打包／零对 `refs/heads/main` 的改动；`%LOCALAPPDATA%/PryCompanion` 不是测试载体；`licenses/**`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md` 一字不碰。
7. 报告按批次**增量落盘**（撞轮次上限之前每完成一批就先写一段：§〇／§一 先落，§二 干净态之后落，T3 每两张脚本连跑之后补一段，§四～§六 收尾补齐）。交付物＝`docs/eval/reports/2026-10-23-qa-wave12-narrative-contract-unheard-exits.md`，报告 id ＝ `eval-QA-WAVE12-1023`。写主树那份报告时只写你自己的文件，不动他人名下任何未跟踪文件。
8. 无法完成的部分**明写"未验证"**，不许把推演写成事实，也不许用大量绿例掩盖某枚没取到红。
9. **双向性明写为未验证**：本波只做"摘掉成员／删门"这一方向的突变；"往集合里加成员"（放宽→收紧的反方向）由 2.1 矩阵的 `None` 行结构性把守，但**没有实测红**，报告里要照这句写，不许写成"两侧都已证"。

## 七 交付形状（守门人收口时按这些取证）

- 分支 `codex/wave12-20261023`，提交号全号；`git show --stat` 只应含那一份测试文件。
- 载体收尾 `git status --short` ＝ 0 行、`git stash list` ＝ 0 条。
- `NarrativeDraftValidator.cs` 磁盘哈希 ＝ `git show HEAD:` 哈希（逐字节对平）。
- T3 二十轮（M-01…M-20）一名一份 TRX，目录不留两轮；四份全量 TRX 的 `<Counters>` 与报告里的总等式一致。
- 三条等式照原文回收：`[Fact] 17→21`、`[Theory] 3→4`、`[InlineData] 14→41`；Core `527→558`；总 `1179`。
- 顶账：本工单这把（纯文档）使 `verify-repository` 由 **555 → 556**（受跟踪文件条数，不是门槛）；报告那一格留给守门人收口时实测，报告里只写你在载体上读到的数。
