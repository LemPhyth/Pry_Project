# 第十四波执行报告·契约面待拆清单：为 16 枚"改坏了今天全仓一声不响"的拒绝出口各造一条会响的判据

- 报告 id：`eval-QA-WAVE14-1023`
- 工单：`docs/dispatch-hds-wave14-20261023.md`
- 载体基点：`ac58245`（全号 `ac582459fcbb6c6cdcb3158dfba59b24f42f72af`，第十二波收口入库后的线尾）
- 执行方：冷启动 lane（wave14-lane）
- 性质：判据刀，零产品代码
- 状态：执行中（本文件按批次增量落盘；§〇／§一 先落）

---

## 〇 开工前当场做的事 + 锚点读回

### 〇.1 主工作树只读清点（开工前，原样贴出）

`git -C C:/Users/24782/Desktop/Pry_Project status --short --branch`：

```
## codex/ui-main-window-redesign
M  docs/dispatch-tip1020-02-memory-group-judgment.md
M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs
?? .qoder/
?? docs/character-learning-safety.patch
?? docs/dispatch-hds-wave13-20261023.md
?? docs/dispatch-hds-wave14-20261023.md
?? docs/dispatch-hds-wave15-20261023.md
?? docs/eval/reports/2026-10-23-hds003-narrator-prototype.md
?? docs/research/HDS_INTERLUDE.md
?? docs/research/HDS_QQ_RESEARCH_INDEX.md
?? docs/research/QQ_BRIDGE.md
```

他人名下两枚暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）本波一律不读、不改、不提交、不 stash、不 clean。主树本波唯一笔迹＝本报告文件。

### 〇.2 独立载体（不动主工作树）

`git worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave14-1023/carrier ac58245`：

```
Preparing worktree (detached HEAD ac58245)
HEAD is now at ac58245 docs(qa,wave12): 第十二波收口入库……
```

入场两行判据（载体内当场实测）：

```
git status --short | wc -l   → 0        （预期 0 ✓）
git rev-parse HEAD           → ac582459fcbb6c6cdcb3158dfba59b24f42f72af   （预期 ac58245 全号 ✓）
```

### 〇.3 起点成立的代码面判据

本波出闸是纯文档刀：`docs/dispatch-hds-wave14-20261023.md` 在主树是未跟踪文档（`??`），基点 `ac58245` 已是第十二波收口入库后的线尾，`git diff --name-only ac58245..ac58245 -- src tests` ＝ 空，行数＝0（预期 0 ✓，起点成立）。

### 〇.4 锚点读回（载体内逐条 `sed -n | cat -n` 读回原文，全部对上）

载体 `src/Pry.Core/Narrative/NarrativeDraftValidator.cs` 内本波 16 枚出口，逐枚读回原文与工单 §〇.5 一致：

**RequireText 抛异常型（5 枚，进 `KnowledgeCandidatePolicy`／`NarrativeIntentPolicy`）**

| # | 行 | 盘上原文 | reason（抛 `NarrativeContractException`） | 对上 |
|---|---|---|---|---|
| R1 | `:306`～`:307` | `NarrativeGuard.RequireText(candidate.CandidateId, nameof(candidate.CandidateId), NarrativeContractReason.ConfirmationWithoutUserEvidence);` | ConfirmationWithoutUserEvidence | ✓ |
| R2 | `:308` | `NarrativeGuard.RequireText(candidate.Claim, nameof(candidate.Claim), NarrativeContractReason.RealityContamination);` | RealityContamination | ✓ |
| R3 | `:309`～`:310` | `NarrativeGuard.RequireText(candidate.ProposedBy, nameof(candidate.ProposedBy), NarrativeContractReason.ConfirmationWithoutUserEvidence);` | ConfirmationWithoutUserEvidence | ✓ |
| R4 | `:266` | `NarrativeGuard.RequireText(intent.IntentId, nameof(intent.IntentId), NarrativeContractReason.IntentClaimedAsFact);` | IntentClaimedAsFact | ✓ |
| R5 | `:267` | `NarrativeGuard.RequireText(intent.Topic, nameof(intent.Topic), NarrativeContractReason.IntentClaimedAsFact);` | IntentClaimedAsFact | ✓ |

`NarrativeGuard.RequireText`（`NarrativeDocuments.cs:425`）原文：空白 ⇒ `throw new NarrativeContractException($"{field} 不能为空。", reason)`；超 128 字符 ⇒ `$"{field} 超过 128 个字符。"`。异常是 `ArgumentException` 子类，`.Reason` 即传入枚。消息点名 `nameof(field)`。

**动作侧（6 枚，`NarrativeDraftValidator.CheckAction`，工单 §〇.5 误名 `NarrativeActionValidator`；`NarrativeActionValidator` 在盘上不存在——沿用第十二波 §一 就地登记，本波不动工单）**

| # | 行 | 盘上原文（条件） | 类型 / 拒绝 | 对上 |
|---|---|---|---|---|
| A1 | `:189` | `NarrativeGuard.RequireText(action.ActionId, nameof(action.ActionId), NarrativeContractReason.ActionTargetMismatch);` | 抛异常 / ActionTargetMismatch | ✓ |
| A2 | `:200` | `if (action.PlannedSegmentCount < 1 \|\| action.PlannedSegmentCount > request.MaxOutboundSegments)` | rejections / ActionSegmentCountInvalid | ✓ |
| A3 | `:214` | `if (action.DeferredIntentId is not null)`（Immediate 段内） | rejections / IntentClaimedAsFact | ✓ |
| A4 | `:223` | `if (action.PlannedSegmentCount != 0)`（Deferred 段内） | rejections / SegmentCountExceeded | ✓ |
| A5 | `:229` | `if (action.ProseQuote is not null)`（Deferred 段内） | rejections / ActionBindingMissing | ✓ |
| A6 | `:248`～`:249` | `default:` ／ `if (action.PlannedSegmentCount != 0 \|\| action.ProseQuote is not null)`（沉默段） | rejections / SegmentCountExceeded | ✓ |

**提交侧（5 枚，`NarrativeCommitValidator.Validate`）**

| # | 行 | 盘上原文（条件） | 类型 / 拒绝 | 对上 |
|---|---|---|---|---|
| C1 | `:387` | `NarrativeGuard.RequireText(commit.CommitId, nameof(commit.CommitId), NarrativeContractReason.ContentHashMissing);` | 抛异常 / ContentHashMissing | ✓ |
| C2 | `:389` | `if (!NarrativeContractVersions.IsSupportedNarrativeVersion(commit.ContractVersion))` | rejections / ContractVersionUnsupported | ✓ |
| C3 | `:395` | `if (string.IsNullOrWhiteSpace(commit.ContentHash))` | rejections / ContentHashMissing | ✓ |
| C4 | `:403` | `if (string.IsNullOrWhiteSpace(intent.SourceCommitId))`（提交内循环，命中后 `continue`） | rejections / IntentClaimedAsFact | ✓ |
| C5 | `:410`～`:411` | `if (!Equals(intent.SourceCommitId, commit.CommitId) && !Equals(intent.SourceCommitId, commit.BatchId))` | rejections / IntentClaimedAsFact | ✓ |

计数口径：5＋6＋5＝**16 枚**（5 抛异常型 R1–R5 ＋ A1 ＋ C1 共 7 枚抛异常；9 枚进 rejections）。与工单 §〇.5 表逐枚对齐，无"锚点不符"。

### 〇.5 在册突变凭证 grep（动笔前）

`grep -r "NarrativeDraftValidator.cs" docs/eval/reports/ docs/dispatch-*.md` 命中＝第十二波工单/报告（`NarrativeDraftValidator.cs` 的 20 枚入账：`:312/:318/:326/:327/:335/:336/:342/:350/:351/:359/:367/:275/:281/:287-288`）。本波 16 枚（`:306/:308/:309/:266/:267/:189/:200/:214/:223/:229/:248/:387/:389/:395/:403/:410`）与那 20 枚**行号零重叠**（第十二波只拆两政策体的 rejection 门，未碰 RequireText／动作侧／提交侧）。⇒ 本波 16 枚均为"该出口第一次实测红"，不领"同格第二条因"。新测试文件名 `NarrativeCommitAndActionGuardTests.cs` 在 `docs/` grep＝0 命中，属新格。

---

## 一 T1｜出口表（纯读码，零构建）

命中判定证据均给到"测试文件名＋行号＋`Assert` 原文"。既有 62 格全在 `tests/Pry.Core.Tests/NarrativeIdentityContractTests.cs`（下称 `NarrativeIdentityContractTests`），逐枚核对本波 16 枚：全仓**无一枚**用例经过这些出口（既有格夹具恒填非空文本／合法段数／合法提交，且提交面 `NarrativeCommitValidator.Validate` 从无任何用例调用）。

| 出口 | 行 | 拒绝（类型） | 被哪条既有用例命中？（证据） | 本波处理 |
|---|---|---|---|---|
| R1 CandidateId | `:306` | ConfirmationWithoutUserEvidence（抛） | 无命中：`NarrativeIdentityContractTests` 矩阵夹具 `CandidateId="kc-1"`（`:357`）、`:379` 那枚 `"kc-2"`，恒非空 | 新格 |
| R2 Claim | `:308` | RealityContamination（抛） | 无命中：Claim 恒 "她说她要搬家"/"周末一起画画" | 新格 |
| R3 ProposedBy | `:309` | ConfirmationWithoutUserEvidence（抛） | 无命中：ProposedBy 恒 "narrator" | 新格 |
| R4 IntentId | `:266` | IntentClaimedAsFact（抛） | 无命中：意图夹具 IntentId 恒 "intent-1"（`:524`） | 新格 |
| R5 Topic | `:267` | IntentClaimedAsFact（抛） | 无命中：Topic 恒 "把画具收完"（`:526`） | 新格 |
| A1 ActionId | `:189` | ActionTargetMismatch（抛） | 无命中：动作夹具 ActionId 恒 "act-1"（`:513`），`Silence` 用例 "act-silence"（`:259`），`Deferred` "act-deferred"（`:308`），恒非空 | 新格 |
| A2 Immediate 段数越界 | `:200` | ActionSegmentCountInvalid | 无命中：Immediate 夹具 PlannedSegmentCount=1（`:517`），落 [1,MaxOutbound=4] 内 | 新格 |
| A3 Immediate 挂未来意图 | `:214` | IntentClaimedAsFact | 无命中：Immediate 夹具 DeferredIntentId 未设（null） | 新格 |
| A4 Deferred 预留段 | `:223` | SegmentCountExceeded | 无命中：`Deferred_action_without_intent`（`:298`）的 Deferred 动作 PlannedSegmentCount 未设＝0 | 新格 |
| A5 Deferred 带正文 | `:229` | ActionBindingMissing | 无命中：同上 Deferred 动作 ProseQuote 未设＝null | 新格 |
| A6 沉默带正文/段 | `:248` | SegmentCountExceeded | 无命中：`Silence_advances...`（`:246`）的沉默动作 PlannedSegmentCount=0、ProseQuote 未设（`:262`）⇒ 走通过支 | 新格 |
| C1 CommitId | `:387` | ContentHashMissing（抛） | 无命中：`NarrativeCommitValidator` 全仓零用例调用 | 新格 |
| C2 契约版本 | `:389` | ContractVersionUnsupported | 无命中：同上 | 新格 |
| C3 内容散列缺失 | `:395` | ContentHashMissing | 无命中：同上 | 新格 |
| C4 提交内意图无来源 | `:403` | IntentClaimedAsFact | 无命中：同上 | 新格 |
| C5 意图来源既非提交也非批次 | `:410` | IntentClaimedAsFact | 无命中：同上 | 新格 |

### 1.1 两处盘上事实（登记，不涂改工单）

1. **提交面全部零命中**：`NarrativeCommitValidator.Validate` 在 `tests/**` 无任何调用点（grep 确认），既有 62 格全走草稿/两政策体。故 C1–C5 全部需新格。
2. **C4 与 C5 的 reason 相同（IntentClaimedAsFact）且互相 `continue` 短路**：`SourceCommitId` 空白时 `:403` 命中并 `continue`（跳过 `:410`）；若摘掉 `:403`，空白来源会落到 `:410`（空白既不等于 CommitId 也不等于 BatchId），**rejection 枚数与 reason 都不变，只有 Detail 文本不同**。⇒ 单靠 `Reason` 无法让 `:403` 独立"会响"（这是该出口今天改坏后 reason 层面一声不响的真实原因）；本波对 C4 的判据须钉到**提交侧那条拒绝的 Detail 原文**（`$"意图 {IntentId} 进入提交时仍没有来源凭据。"`）才敏感，报告 §二／§六 明写这一层。C5 则可用非空白来源干净隔离（详见 §二）。

### 1.2 待拆（本波不做，盘上发现，不进预算）

- `CognitionKind.CharacterBelief`（`NarrativeDocuments.cs:138`）：两政策体零条款引用 ⇒ 六来源全 `None`。工单 §〇.5 点名"只登记不补判据"。补了不构成任何出口的把守，属产品决策，交回守门人。（承第十二波 §六 待拆清单，本波仍只登记。）
- `NarrativeRequest.Validate`（`NarrativeDocuments.cs:201`）与 `InputRef.Validate`／`ProcessedThroughSeq.Validate` 内多处 `RequireText`/抛门（`:36/:37/:203/:204/:205` 等）：均为构造期结构护栏、非本工单点名的 16 枚出口，且 `NarrativeIdentityContractTests` 与既有 Core 用例部分经过其正向路径 ⇒ 本波不动，列此备查。
- 反方向（往集合加成员＝放宽→收紧）本波同样不实测（工单 §六.9 口径），仅在报告中明写"放宽方向未验证"。

---

## 二 T2｜落判据（新增一枚文件，零改动既有断言／零产品代码）

新文件＝`tests/Pry.Core.Tests/NarrativeCommitAndActionGuardTests.cs`（载体基点 `ac58245`）。既有 `NarrativeIdentityContractTests.cs` 与 `src/**` 未动。断言形状：

- 7 枚抛异常出口（R1–R5、A1、C1）＝`Assert.Throws<NarrativeContractException>` ＋ `Assert.Equal(<点名 Reason>, error.Reason)` ＋ `Assert.Equal("<field> 不能为空。", error.Message)`（异常类型、Reason、消息字段三处逐条点名；`NarrativeContractException : ArgumentException`，`Message` 即 `{field} 不能为空。`）。
- 8 枚进 rejections 且天然只产生 1 条的出口（A2、A3、A4、A5、A6、C2、C3、C5）＝`var rejection = Assert.Single(rejections);` ＋ `Assert.Equal(<点名 Reason>, rejection.Reason)`。
- 1 枚（C4，`:403`）因与 `:410` 同 Reason 且互相 `continue` 短路，用 `Assert.Collection` 把草稿层与提交层两条逐条钉死（第二条含提交侧 Detail 原文），详见 §一.1.1 与 §三 C4 行。
- **零** `Assert.Contains`／`> 0`／`Assert.NotEmpty`／`Assert.ThrowsAny`／`Skip=`／`Assert.Equal(N, x.Count)`（当场 grep：Contains 0、NotEmpty 0、ThrowsAny 0、Skip= 0、`Assert.Equal(N,...Count)` 0）。⇒ 不招 xUnit2013。

### 2.1 判据条数等式（落码前后各数一次，原样贴出）

```
grep -c '\[Fact'          → 16（新文件；本波无既有 Fact 基数，62 格在另一文件未被触碰）
grep -c '\[Theory\|\[InlineData' → 0（本波 16 枚全部单值夹具，无参数化）
新增可跑用例 = 16 条（16 [Fact]，0 Theory 行）
```

16 格 ↔ 16 枚出口对照（一枚一格）：

| 新格（方法名） | 守的出口 | 枚类型 |
|---|---|---|
| Blank_candidate_id_throws_rather_than_asserting_without_a_subject | R1 `:306` | 抛 |
| Blank_claim_throws_rather_than_asserting_an_empty_reality | R2 `:308` | 抛 |
| Blank_proposed_by_throws_rather_than_anonymous_confirmation | R3 `:309` | 抛 |
| Blank_intent_id_throws_rather_than_an_addressable_promise | R4 `:266` | 抛 |
| Blank_intent_topic_throws_rather_than_an_intention_about_nothing | R5 `:267` | 抛 |
| Blank_action_id_throws_before_any_scope_decision_is_made | A1 `:189` | 抛 |
| Immediate_action_segment_above_host_ceiling_is_refused | A2 `:200`（上界侧） | rejections |
| Immediate_action_that_also_hangs_a_future_intent_is_refused | A3 `:214` | rejections |
| Deferred_action_reserving_delivery_segments_is_refused | A4 `:223` | rejections |
| Deferred_action_carrying_immediate_prose_is_refused | A5 `:229` | rejections |
| Silence_action_reserving_a_segment_is_refused | A6 `:248` | rejections |
| Blank_commit_id_throws_rather_than_a_hashless_commit_identity | C1 `:387` | 抛 |
| Commit_with_an_unsupported_contract_version_is_refused | C2 `:389` | rejections |
| Commit_without_host_content_hash_is_refused | C3 `:395` | rejections |
| Commit_intent_without_source_is_refused_by_both_layers | C4 `:403` | rejections |
| Commit_intent_whose_source_is_neither_commit_nor_batch_is_refused | C5 `:410` | rejections |

阈值零改动（`.7`／`.5`／`2400`／五档超时／`12`／`4096`／`128`／`512`／`resource-limit-ledger.md` 全不碰；本波只用到 `MaxOutboundSegments` 默认 4 造 A2 越界、`.9` 置信避开 Proposal 门等既有标定值，未改任何阈值）。

### 2.2 干净态正向探针（进 T3 前置门）——已绿

预检整段原文（clean 轮）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=1023,415,884x738 screen=2560x1440 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=8% used=1394MB
free_memory_gb=8.5
VERDICT=BUILD_OK
```

跑法（无 `--no-build`、绝对 results-directory 长参、`--logger trx`）：

```
dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release \
  --filter "FullyQualifiedName~NarrativeCommitAndActionGuardTests" \
  --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave14-1023\runs\run-clean" --logger trx
```

TRX 份数 `ls *.trx`＝1（`24782_PHYTHLINK_2026-10-23_15_28_21.trx`）。`<Counters>` 原文：

```
<Counters total="16" executed="16" passed="16" failed="0" error="0" timeout="0" aborted="0" inconclusive="0" passedButRunAborted="0" notRunnable="0" notExecuted="0" disconnected="0" warning="0" completed="0" inProgress="0" pending="0" />
```

**failed=0 ✓**。控制台构建零 xUnit2013 警告（本波无 `Assert.Equal(N,.Count)` 形状）。16 格全部按名点名通过：Blank_candidate_id…／Blank_claim…／Blank_proposed_by…／Blank_intent_id…／Blank_intent_topic…／Blank_action_id…／Immediate_action_segment_above_host_ceiling…／Immediate_action_that_also_hangs_a_future_intent…／Deferred_action_reserving_delivery_segments…／Deferred_action_carrying_immediate_prose…／Silence_action_reserving_a_segment…／Blank_commit_id…／Commit_with_an_unsupported_contract_version…／Commit_without_host_content_hash…／Commit_intent_without_source_is_refused_by_both_layers／Commit_intent_whose_source_is_neither_commit_nor_batch… ⇒ 前置门打开，进入 T3。

---

## 三 T3｜逐枚单放突变，各取一枚"单枚、单轮、清洁"的红（16 轮）

统一约定（每轮共用）：
- ① 载体基点 HEAD＝`ac582459fcbb6c6cdcb3158dfba59b24f42f72af`；被改文件 `src/Pry.Core/Narrative/NarrativeDraftValidator.cs` 基线 SHA-256＝`88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c`（与 HEAD blob 逐字节相等；16 轮每轮还原后均稳定复现此值）。
- ⑤ 跑法（一轮一 TRX 目录 `run-<id>`，绝不带 `--no-build`）：`dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~NarrativeCommitAndActionGuardTests" --results-directory "…\runs\run-<id>" --logger trx`
- ⑧ 还原＝`git checkout -- src/Pry.Core/Narrative/NarrativeDraftValidator.cs`（绝不 `git checkout -- .`）。
- ⑩ 每轮还原后载体 `git status --short` 行数＝**1**（仅本波那一份新测试文件）。
- ⑪ 每轮 `ls *.trx`＝1、目录不复用。突变经 `mutate.py` 按**行号**改写（`splitlines(keepends)`＋保留原行 EOL），CRLF/LF 容错；每轮脚本打印 `MUTATED … anchor_ok` 才起跑，任一 NO-HIT 即停（本波 16 轮全 anchor_ok，无 skip／无空转）。红格全名＋消息＋红点行号取自 TRX（断言调用处），另注断言文本源码行。
- 驱动脚本 `driver.sh`（3 份 spec，各 ≤6 轮：batch1＝R1–R5+A1 共 6；batch2＝A2–A6 共 5；batch3＝C1–C5 共 5）；证据照脚本当场打印原文抄。

### 批次 1（R1 … A1）

| 轮 | 被改（单枚） | before→after | --stat | TRX | `<Counters>` | 唯一红格（消息 / 红点行） | 还原 sha 对平 | 结论 |
|---|---|---|---|---|---|---|---|---|
| R1 | `:306`～`:307` RequireText(CandidateId) 整行删 | 删 2 行 | 2 deletions | run-R1/…15_32_00.trx | 16/16/15/**fail 1** | Blank_candidate_id… `Assert.Throws() Failure: No exception was thrown` / TRX `:37`（断言在源 `:36`） | 88ebf5c3 ✓ | 证明 `:306` 现被本波新格把守；摘掉即不抛→红。单枚清洁。 |
| R2 | `:308` RequireText(Claim) 删 | 删 1 行 | 1 deletion | run-R2/…15_32_05.trx | 16/16/15/fail 1 | Blank_claim… 同上 / `:59` | ✓ | Claim 抛门会响。 |
| R3 | `:309`～`:310` RequireText(ProposedBy) 删 | 删 2 行 | 2 deletions | run-R3/…15_32_11.trx | 16/16/15/fail 1 | Blank_proposed_by… 同上 / `:81` | ✓ | ProposedBy 抛门会响。 |
| R4 | `:266` RequireText(IntentId) 删 | 删 1 行 | 1 deletion | run-R4/…15_32_16.trx | 16/16/15/fail 1 | Blank_intent_id… 同上 / `:93` | ✓ | 意图 IntentId 抛门会响。 |
| R5 | `:267` RequireText(Topic) 删 | 删 1 行 | 1 deletion | run-R5/…15_32_21.trx | 16/16/15/fail 1 | Blank_intent_topic… 同上 / `:105` | ✓ | 意图 Topic 抛门会响。 |
| A1 | `:189` RequireText(ActionId) 删 | 删 1 行 | 1 deletion | run-A1/…15_32_27.trx | 16/16/15/fail 1 | Blank_action_id… 同上 / `:122` | ✓ | 动作 ActionId 抛门（经 `NarrativeDraftValidator.Validate`）会响。 |

批次 1 小结：6 轮全部单枚、单轮、清洁，红集恰＝1 格，无多格、无"改坏后无格能响"。

### 批次 2（A2 … A6）

| 轮 | 被改（单枚） | before→after | --stat | TRX | `<Counters>` | 唯一红格（消息 / 红点行） | 还原 sha | 结论 |
|---|---|---|---|---|---|---|---|---|
| A2 | `:200` 去 `\|\| count > Max`（上界侧成员） | `…< 1 \|\| …> Max` → `…< 1` | 1 ins/1 del | run-A2/…15_32_47.trx | 16/16/15/fail 1 | Immediate_action_segment_above_host_ceiling… `Assert.Single() Failure: The collection was empty` / `:133` | ✓ | 段数上界一侧（count=5）会响。下界 `< 1` 未摘（属反方向，见 §六）。 |
| A3 | `:214`～`:218` 整门删（Immediate 挂未来意图） | 删 5 行 | 5 deletions | run-A3/…15_32_53.trx | 16/16/15/fail 1 | Immediate_action_that_also_hangs_a_future_intent… Empty / `:147` | ✓ | 会响。 |
| A4 | `:223`～`:227` 整门删（Deferred 预留段） | 删 5 行 | 5 deletions | run-A4/…15_32_59.trx | 16/16/15/fail 1 | Deferred_action_reserving_delivery_segments… Empty / `:161` | ✓ | 会响。 |
| A5 | `:229`～`:233` 整门删（Deferred 带正文） | 删 5 行 | 5 deletions | run-A5/…15_33_04.trx | 16/16/15/fail 1 | Deferred_action_carrying_immediate_prose… Empty / `:185` | ✓ | 会响。 |
| A6 | `:249` 去 `count != 0 \|\|`（沉默段首成员） | `count!=0 \|\| quote!=null` → `quote!=null` | 1 ins/1 del | run-A6/…15_33_10.trx | 16/16/15/fail 1 | Silence_action_reserving_a_segment… Empty / `:209` | ✓ | 沉默带段一侧（count=1）会响。既有 `Silence_advances…`（count=0）不受此摘动，未进红集。 |

批次 2 小结：5 轮全部单枚、单轮、清洁，红集恰＝1 格。A2/A6 为"从 `\|\|` 集合摘一枚成员"（照工单 §三"或从 or 集合摘成员"），其余三枚为整门删。

### 批次 3（C1 … C5）

| 轮 | 被改（单枚） | before→after | --stat | TRX | `<Counters>` | 唯一红格（消息 / 红点行） | 还原 sha | 结论 |
|---|---|---|---|---|---|---|---|---|
| C1 | `:387` RequireText(CommitId) 删 | 删 1 行 | 1 deletion | run-C1/…15_33_30.trx | 16/16/15/fail 1 | Blank_commit_id… `No exception was thrown` / `:236` | ✓ | 提交 CommitId 抛门会响。 |
| C2 | `:389`～`:393` 整门删（契约版本） | 删 5 行 | 5 deletions | run-C2/…15_33_36.trx | 16/16/15/fail 1 | Commit_with_an_unsupported_contract_version… Empty / `:248` | ✓ | 会响。 |
| C3 | `:395`～`:399` 整门删（内容散列缺失） | 删 5 行 | 5 deletions | run-C3/…15_33_41.trx | 16/16/15/fail 1 | Commit_without_host_content_hash… Empty / `:259` | ✓ | 会响。 |
| C4 | `:403`～`:408` 整门删（提交内意图无来源，含 `continue`） | 删 6 行 | 6 deletions | run-C4/…15_33_46.trx | 16/16/15/fail 1 | Commit_intent_without_source_is_refused_by_both_layers `Assert.Collection() Failure: Item comparison failure (pos 1)` / `:277` | ✓ | **会响**：摘掉 `:403` 后空白来源落到 `:410`（reason 仍是 IntentClaimedAsFact、条数仍 2），但提交侧那条 Detail 由"进入提交时仍没有来源凭据。"变成"…既不是本次提交也不是本次批次。"⇒ 本格 `Assert.Collection` 第二条的 Detail 等值断言当场红。见 §一.1.1：`:403` 与 `:410` 同 Reason、`continue` 互斥，单靠 Reason 该门改坏今天全仓一声不响，只有钉 Detail 才敏感。 |
| C5 | `:410`～`:415` 整门删（来源既非提交也非批次） | 删 6 行 | 6 deletions | run-C5/…15_33_52.trx | 16/16/15/fail 1 | Commit_intent_whose_source_is_neither_commit_nor_batch… Empty / `:297` | ✓ | 会响（非空白来源 `foreign-source` 干净隔离，仅 `:410` 响应；C4 用空白来源走 `:403+continue`，不受 C5 摘动，未进红集）。 |

批次 3 小结：5 轮全部单枚、单轮、清洁，红集恰＝1 格。

### T3 总结（16 轮）

- **全部 16 轮红集恰＝1 格，无红集多一格，无"取不到红／改坏后无格能响"的轮次。** 16 枚出口各拿到第一次会响的单枚清洁红（本波 16 行与第十二波 20 行零重叠 ⇒ 16 枚均第一次实测红，无"同格第二条因"）。
- 每轮还原后 `NarrativeDraftValidator.cs` 磁盘 SHA-256 ＝ `git show HEAD:` SHA-256 ＝ `88ebf5c3…b00c`（16/16 逐字节对平）。
- 每轮载体 `git status --short` 行数＝1（仅本波新测试文件），src 全程零残留。
- 全部 16 轮预检 `VERDICT=BUILD_OK`，0 次 HOLD，0 次停刀。

---

## 四 T4｜一次全量构建 + 全量跑 + 交付形状自证

### 4.1 分刀提交（载体，`git commit --only <明确路径>`）

- 载体分支：`codex/wave14-20261023`
- 提交号：**`f07f1d2`**（全号 `f07f1d29e08bfa139746dc10ba640e7bccc11100`），基点 `ac58245`
- `git show --stat f07f1d2`：仅 `tests/Pry.Core.Tests/NarrativeCommitAndActionGuardTests.cs | 412 +++++`，`1 file changed, 412 insertions(+)`——零 `src/**`、零既有测试文件、零工单/日志。
- 命令用 `git commit --only tests/Pry.Core.Tests/NarrativeCommitAndActionGuardTests.cs`（明确路径，不吃他人 index）。

### 4.2 全量四份 TRX 读数（`run-full`，`--no-build`，`ls *.trx`＝4，逐份 `<Counters>` 原文）

| 份（TRX 文件） | 项目 | `<Counters>` 原文 |
|---|---|---|
| `24782_PHYTHLINK_2026-10-23_15_36_01.trx` | Pry.Core.Tests | `total="574" executed="572" passed="572" failed="0" … notExecuted="0" …`（2 格 NotExecuted，见下） |
| `24782_PHYTHLINK_2026-10-23_15_36_02.trx` | Pry.Client.Tests | `total="108" executed="108" passed="108" failed="0" … notExecuted="0" …` |
| `24782_PHYTHLINK_2026-10-23_15_36_02[1].trx` | Pry.Api.Tests | `total="280" executed="280" passed="280" failed="0" … notExecuted="0" …` |
| `24782_PHYTHLINK_2026-10-23_15_36_03.trx` | Pry.App.Tests | `total="233" executed="233" passed="233" failed="0" … notExecuted="0" …` |

Core 那 2 格未跑（`total 574` 与 `executed 572` 之差，outcome=NotExecuted 计 2 条）：
`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`、`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`——均为环境门（SenseVoice 模型离线件 / llama 子进程环境密钥），本波既有、非本波引入、非红。

### 4.3 总等式（照盘上数一次）

```
四份 total：      574 + 108 + 280 + 233 = 1195
四份 passed：     572 + 108 + 280 + 233 = 1193
NotExecuted(skip)：2（均在 Core）
failed（红）：    0
⇒ 1195 = 1193 passed + 2 skip + 0 红   ✓
```

Core 对平：`574 = 558（第十二波收口入库后的基线）+ 16（本波新格）`；Api 280 / Client 108 / App 233 与基线**逐位不动**。

### 4.4 构建警告数

一次 Release 全量构建：**已成功生成，0 个警告，0 个错误**。本波无 `Assert.Equal(N, x.Count)` 形状 ⇒ 不招 xUnit2013（与第十二波守门人 §七 那 5 条同类误报零复发）。

### 4.5 交付形状自证（dispatch §六.1，全部当场 grep/sha/diff 复现）

```
载体 git status --short 行数                → 0     （提交后干净 ✓）
git diff HEAD --name-only -- src tests      → 0     （提交已捕获全部笔迹 ✓）
git stash list                              → 0     ✓
NarrativeDraftValidator.cs 磁盘 sha256       → 88ebf5c3…b00c
  与 git show HEAD: 同文件 sha256            → 88ebf5c3…b00c   （逐字节对平 ✓）
NarrativeDocuments.cs      磁盘==HEAD blob   → 96e4d794…0f9a4  （✓）
NarrativeContractReason.cs 磁盘==HEAD blob   → 7d753a1d…4aaab3 （✓）
提交 f07f1d2 --name-only                    → 仅 tests/Pry.Core.Tests/NarrativeCommitAndActionGuardTests.cs
```

三枚契约文件（本波唯一被逐枚临时改坏再还原的 `NarrativeDraftValidator.cs` ＋ 两枚只读的 `NarrativeDocuments.cs`/`NarrativeContractReason.cs`）磁盘与 HEAD blob 逐字节相等 ⇒ 还原干净、`src/**` 零残留。主工作树他人名下两枚暂存项本波未碰。

---

## 五 预算与门禁账（preflight / dotnet 逐笔）

- 每一次 `dotnet` 调用前均跑 `powershell -NoProfile -ExecutionPolicy Bypass -File .local-notes/game-preflight6-1022.ps1`，整段原文（clean 轮样例见 §2.2）已贴；`VERDICT=BUILD_OK` 才起跑。
- 预检次数＝1（clean 前置门）＋16（16 轮突变）＋1（全量构建）＋1（全量跑）＝**19 次**；**19 次全 BUILD_OK，0 次 HOLD，0 次变通**。
- 预算对账（工单给的额度）：
  - 全量构建 1/1（T4 用尽，未超）
  - 全量跑 1/1（T4 用尽）
  - targeted ≤22：clean 1 + 16 轮突变 = **17/22**（5 枚机动额度未动用）
  - 机动 0 动用
- 突变轮 `dotnet test` 全程**未加** `--no-build`（每轮重建被临时改坏的 src 才能取到真红；仅 T4 全量跑一次按工单允许带 `--no-build`）。

---

## 六 偏差自报 + 待拆 + 未验证

### 6.1 偏差（相对工单）

- **动作侧类名**：工单 §〇.5 记为 `NarrativeActionValidator`，盘上不存在该类；出口实际在 `NarrativeDraftValidator.CheckAction`（`:189/:200/:214/:223/:229/:248`）。沿用第十二波 §一 就地登记口径，本波不改工单。计数与行号锚点全部对上。
- **C4（`:403`）判据形状偏离"单枚单判据"**：`:403` 与 `:410` 同 `IntentClaimedAsFact` 且 `:403` 命中后 `continue` 短路 `:410`；仅摘 `:403` 时 rejection 的**条数与 Reason 都不变**，只有提交侧那条 Detail 由「进入提交时仍没有来源凭据。」换成「…既不是本次提交也不是本次批次。」。故 C4 格用 `Assert.Collection` 把草稿层＋提交层两条逐条钉死（第二条含提交侧 Detail 原文）才对该门敏感——这正是「该出口今天改坏后在 Reason 层面全仓一声不响」的根因，红证形态为 `Assert.Collection() Failure: Item comparison failure (pos 1)`（非 `Assert.Single() … empty`）。枚数仍 1 格、红集仍恰 1 格，不违反"单枚单轮清洁"。

### 6.2 待拆（本波不做，盘上发现，不进预算；承第十二波 §六）

- `CognitionKind.CharacterBelief`（`NarrativeDocuments.cs:138`）：两政策体零条款引用，补了不守任何出口，属产品决策，交回守门人——本波仍只登记。
- `NarrativeGuard.RequireText` 在**其它文件**（`NarrativeRequest`/`InputRef`/`ProcessedThroughSeq` 等构造期护栏，`NarrativeDocuments.cs:36/:37/:201/:203/:204/:205`）的调用点：非本工单点名的 16 枚出口，本波不动，列此备查。
- `NarrativeCommitValidator` 提交面除 C1–C5 外的其余条款（批次/去重/绑定等）：本波只拆工单点名 5 枚，其余未穷举，待后续波次。

### 6.3 未验证（明写，不谎称已证）

- **放宽方向（反方向）未实测**：本波只朝"收紧侧"取红（删门/摘 `\|\|` 成员使越界输入被放行→红）。A2 只摘上界侧、下界 `< 1` 未摘；若"往 or 集合加成员＝放宽到更宽"这类改动，本波不造、不测（工单 §六.9 口径）。
- **第十二波登记的 Client `DeadLoopbackReconnectTests` flake**：本波全量 Client 108/108 全绿、未见该 flake 复发（照盘登记为"本轮未复现"，非已根治结论）。
- **2 格 Core NotExecuted 的门禁内真实行为**：环境依赖测试（离线模型 / llama 子进程密钥）在非当前环境被门挡下，本波未在其内部逻辑上取红，仅确认非本波引入、非红。

### 6.4 状态

工单 `HDS-WAVE14-1023` 全量完成：T1 出口表 ✓、T2 16 格判据 ✓、T3 16/16 各一枚单枚清洁红 ✓、T4 一次全量构建（0 警告）+ 全量跑（1193/2/0）+ 分刀提交 `f07f1d2` + 交付自证 ✓。零产品代码残留，主树唯一笔迹＝本报告。

---

## 七 守门人收口复证（2026-10-23 15:5x，主树当场 `date`）

### 7.1 载体自证的盘上复核

- 载体 `codex/wave14-20261023 @ f07f1d29e08bfa139746dc10ba640e7bccc11100`，`git show --stat HEAD` 末行＝`…/NarrativeCommitAndActionGuardTests.cs | 412 +++++…`、`1 file changed, 412 insertions(+)`——**唯一一枚新文件，零产品代码、零既有断言改动**，与工单 §二 的范围一致。
- 枚数与判据条数从盘上重取：`grep -c "\[Fact\]"`＝**16**、`wc -l`＝**412**；`git show HEAD:<文件> | sha256sum` 与磁盘＝`99cfb03067633b30a6fdd91730a3890af2b8c8e81a0157a0e4e03ab5499fbdbc` 两路一致。载体 `git status --short`＝**0 行**。
- §4.2 的全量读数按 lane 自跑的 TRX 引用，本波不再重数：归位之后守门人在**另一枚干净副本**上重跑了一次全量（见 7.3），那一轮才是收口读数。

### 7.2 归位（分刀，`git commit --only`）

主树 `d3fbac4b86ee1ead7cb998072c5cc2b3ca406bbd`，1 file changed, 412 insertions(+)。落盘前逐字节对平：`git show f07f1d2:<文件>` 与主树磁盘文件哈希同为 `99cfb030…`。同一场把第十三波的四枚新文件作 `290a603` 归位；他人名下两枚暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）跑前跑后都还是索引里的 `M ` 两行，未被消费、未被改动。

### 7.3 合流复采（干净 detached 副本 `d3fbac4`，一次构建＋一次全量）

预检 `VERDICT=BUILD_OK`（`free_memory_gb=7.7`／`6.8`）⇒ `dotnet build Pry.slnx -c Release`＝**已成功生成／0 个警告／0 个错误**；`dotnet test Pry.slnx -c Release --no-build --logger trx`＝4 份 TRX：

| 程序集 | executed | passed | failed | total |
|---|---|---|---|---|
| Client | 108 | 108 | 0 | 108 |
| App | 233 | 233 | 0 | 233 |
| Api | 280 | 280 | 0 | 280 |
| Core | 621 | 621 | 0 | 623 |

**总 1244＝108＋233＋280＋623，0 红，2 条按名点出的环境跳过**（`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`）。对账等式：在册 1179 ＋ 本波 16 ＋ 第十三波 49 ＝ 1244，逐位相符；`scripts/verify-repository.ps1`＝562 tracked files，`measure-fixed-eval.ps1 -CatalogOnly`＝Catalog valid 76（场景未动），`git diff --check` 干净。

### 7.4 三条登记口径（不外推）

1. §6.4 那句"全量完成"是 **lane 自述**，不是状态登记；任务表状态与基线改写权在守门人／QA，本节只写"已归位待复采"。
2. §6.3 的**放宽方向未实测**照旧在册，本波没有还它；第十二波待拆清单里那几行正向 `→ None` 仍无人守。
3. 1244 这一轮由**写判据的同一只手**（守门人）在独立副本上单采，没有第二人手复采——所以它是"归位后读数"，不自动等于"在册基线"；下一次本来就要起构建的波次把收尾那轮跑在别的载体上时才算换手复采。**绝不为顶基线单独起构建**。
