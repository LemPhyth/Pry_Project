# 第二十四波·HDS-WAVE24-1023｜把第十四波那 16 格的红换到另一双手上（纯复采刀）

- 工单：`docs/dispatch-hds-wave24-20261023.md`（`HDS-WAVE24-1023`，性质＝纯复采刀／证据刀，零产品代码、零判据字节改动）
- 执行方：冷启动 lane（第二十四号载体）
- 交付物：本报告一份（主树、未跟踪）＋`.local-notes/wave24-1023/` 与 `.local-notes/qa-wave24-1023/` 里的仪器副本、逐轮 TRX 与预检留档
- 本刀不新增一格、不删一格、不改一行 `src/` 或 `tests/` 的持久化字节，不自封基线、不写状态词与结案词

---

## 〇 入场读数（全部当场实测，不引用工单数字）

### 〇.1 时间与线尾

```
$ date
Fri Oct 23 19:48:38     2026

$ git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD
f26545d51b02002ae29b0b635a62ddbf187acf62
```

### 〇.2 主树状态（他人资产按硬线只数行不读内容）

`git -C C:/Users/24782/Desktop/Pry_Project status --short --branch` 原样：

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

两枚 `M ` 暂存项与全部 `??` ＝别人名下资产，本刀不读内容、不改、不提交、不 `stash`／`clean`／`rm`。

### 〇.3 落后核格与载体

- 线尾（`git rev-parse codex/ui-main-window-redesign`）＝`f26545d51b02002ae29b0b635a62ddbf187acf62`
- `git rev-list --count HEAD..f26545d`＝**0**（主树 HEAD 即线尾，无需 `merge --ff-only` 追平）
- 载体：`git worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave24-1023/carrier f26545d51b02002ae29b0b635a62ddbf187acf62`
  - 载体 `git rev-parse HEAD`＝`f26545d51b02002ae29b0b635a62ddbf187acf62`
  - 载体 `git status --short`＝**0 行**
  - 载体内 `src/Pry.Core/Narrative/NarrativeDraftValidator.cs` SHA-256＝`88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c`，与 `git show HEAD:…` 的 HEAD-blob SHA-256 逐字符相同（清洁态基线，本刀全程只在此值与其突变态之间往返、每轮跑完还原回此值）。

### 〇.4 在册门禁基线（读盘上登记处，不读工单）

在册基线现值＝**1333／executed 1331／failed 0／跳过 2**（分项 Client 108／App 233／Api 280／Core total 712、Core executed 710），取处＝`docs/eval/reports/2026-10-23-hds010-end-to-end-pipeline-prototype.md` §五.5 守门人归位轮的四份 TRX 逐份求和（`gate-wave23-1023/full/`：Client 108＋App 233＋Api 280＋Core 712＝1333、executed 1331、failed 0；两条 `outcome="NotExecuted"`＝`CoreTests.Configured_sense_voice_model_can_run_offline`／`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`），并由第二十四波归位提交 `f26545d` 的 message 复述同值确认在册。**本刀零新格，这一把不许动在册基线**；全量跑出来不等于 1333 即先怀疑仪器再怀疑读数。

### 〇.5 本刀允许文件面

1. `.local-notes/wave24-1023/`（自建，gitignored）＝仪器副本＋全部产物（runs／TRX／预检留档）；`.local-notes/qa-wave24-1023/carrier`＝本刀 detached 载体。
2. 报告 `docs/eval/reports/2026-10-23-hds003-005-second-hand-recount-prototype.md`（本文件）。
3. `src/`／`tests/`／`docs/` 其余任何文件＝零字节持久化改动；第十四波原件目录 `.local-notes/qa-wave14-1023/` 只 `cp` 不原地改。

### 〇.6 硬线自证口径（本刀读到 §〇 落盘为止）

- `dotnet` 调用＝**0**（入场至今）；焦点预检起跑 **1** 次（19:44，非 dotnet 命令、用于确认环境），读数 `VERDICT=BUILD_OK`（`FOREGROUND: QQ … covers=False`、`free_memory_gb=9.9`）。此后每一次 `dotnet`（build／test／每一枚定向突变轮）之前各跑一次 v6 预检并贴整段，只有 `BUILD_OK` 才起跑；读到 `HOLD` 就地停刀、不请示、不改预检判据、不绕过。
- 复采仪器复用＝`cp -r .local-notes/qa-wave14-1023/tools .local-notes/wave24-1023/tools`，只重指 `driver.sh` 顶部 `CARRIER`／`RUNS`／`TOOLS` 三行到本刀目录；**原件一字未动**。为满足本刀硬规则"每枚突变还原前后双 SHA-256"（工单 §一.3 点名要补的 ⑨⑩⑪ 里 ⑩ 需要突变态与还原态两侧），在 driver 的 mutate 之后加一行只读 `sha256sum`（打印突变态哈希），不改 mutate／测试／还原／比较任何逻辑与跑法；此一处加行在本节如实登记。
- 入场读数中已实测：主树对线尾 `git diff --stat f26545d..HEAD -- src tests scripts Pry.slnx Directory.Build.props`＝**空**（本刀身份面，见 §四.5）。

---

## 一 活单①｜第十四波那 16 格换人复采（driver 连跑，逐轮十一项）

跑法＝本刀自持 driver（`.local-notes/wave24-1023/tools/driver.sh`，CARRIER／RUNS／TOOLS 三行重指本刀目录，其余逻辑同第十四波原件；仅在 mutate 后加一行只读 `sha256sum` 以补 ⑩ 突变态侧）。每轮整行跑法＝`cd "<carrier>" && dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~NarrativeCommitAndActionGuardTests" --results-directory "<carrier>/.local-notes/wave24-1023/runs/run-<ID>" --logger trx`（无 `--no-build`）。所有轮：`total=16／executed=16／failed=1`、恰好点名单枚突变对应的那格红、其余 15 格绿；还原＝`git checkout -- src/Pry.Core/Narrative/NarrativeDraftValidator.cs`，还原后文件 SHA-256 逐轮＝`88ebf5c3…b00c`（与 HEAD-blob 同值＝无持久化字节改动），还原后 `git status --short`＝0 行。

判据文件（波 14 载体那份字节，`d3fbac4` 至今未动）＝`tests/Pry.Core.Tests/NarrativeCommitAndActionGuardTests.cs`；被守护产品行＝`src/Pry.Core/Narrative/NarrativeDraftValidator.cs`（简称 `V:`）。本手＝另一双手（第二十四号载体）在独立载体逐枚重放，与第十四波 lane 的红集逐格同名。

### 批次 1（R1–R5、A1）——19:49:53～19:50:20，六轮全 `VERDICT=BUILD_OK`、0 HOLD

| ID | ①文件:行 | ②改前 / ③改后 | ④diff --stat | ⑤跑法 | ⑥TRX | ⑦红格全名＋消息要点＋红点行 | ⑧还原命令 | ⑨status行数 | ⑩突变态SHA / 还原后SHA | ⑪预检 |
|---|---|---|---|---|---|---|---|---|---|---|
| R1 | V:306–307 | 删 `NarrativeGuard.RequireText(candidate.CandidateId, …, ConfirmationWithoutUserEvidence);`（整两行）／无 | 1 file changed, 2 deletions(-) | driver 通用行 RID=run-R1 | `…_19_49_53.trx` (1份) | `…Blank_candidate_id_throws_rather_than_asserting_without_a_subject`；`Assert.Throws() Failure: No exception was thrown / Expected: NarrativeContractException`；`:37` | `git checkout -- …/NarrativeDraftValidator.cs` | 0 | `242a0ae8…712d` / `88ebf5c3…b00c` | 见下 R1 整段 |
| R2 | V:308 | 删 `NarrativeGuard.RequireText(candidate.Claim, …, RealityContamination);`／无 | 1 file changed, 1 deletion(-) | RID=run-R2 | `…_19_49_58.trx` (1) | `…Blank_claim_throws_rather_than_asserting_an_empty_reality`；同上 `No exception was thrown`；`:59` | 通用 | 0 | `c634654d…4295` / `88ebf5c3…b00c` | 19_49 BUILD_OK free=9.2 |
| R3 | V:309–310 | 删 `RequireText(candidate.ProposedBy, …, ConfirmationWithoutUserEvidence);`（两行）／无 | 1 file changed, 2 deletions(-) | RID=run-R3 | `…_19_50_04.trx` (1) | `…Blank_proposed_by_throws_rather_than_anonymous_confirmation`；`No exception was thrown`；`:81` | 通用 | 0 | `c137b53d…f833` / `88ebf5c3…b00c` | 通用 BUILD_OK free=9.3 |
| R4 | V:266 | 删 `RequireText(intent.IntentId, …, IntentClaimedAsFact);`／无 | 1 file changed, 1 deletion(-) | RID=run-R4 | `…_19_50_09.trx` (1) | `…Blank_intent_id_throws_rather_than_an_addressable_promise`；`No exception was thrown`；`:93` | 通用 | 0 | `4a08f946…d6d4` / `88ebf5c3…b00c` | 通用 BUILD_OK free=9.3 |
| R5 | V:267 | 删 `RequireText(intent.Topic, …, IntentClaimedAsFact);`／无 | 1 file changed, 1 deletion(-) | RID=run-R5 | `…_19_50_15.trx` (1) | `…Blank_intent_topic_throws_rather_than_an_intention_about_nothing`；`No exception was thrown`；`:105` | 通用 | 0 | `5165223c…96fd` / `88ebf5c3…b00c` | 通用 BUILD_OK free=9.3 |
| A1 | V:189 | 删 `RequireText(action.ActionId, …, ActionTargetMismatch);`／无 | 1 file changed, 1 deletion(-) | RID=run-A1 | `…_19_50_20.trx` (1) | `…Blank_action_id_throws_before_any_scope_decision_is_made`；`No exception was thrown`；`:122` | 通用 | 0 | `385de931…9adb` / `88ebf5c3…b00c` | 通用 BUILD_OK free=9.4 |

R1 轮预检整段（余五轮同形，仅 `free_memory_gb` 与 `used` 字节级浮动，均 `covers=False`→`VERDICT=BUILD_OK`）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWW_1 style=0x14C70000 rect=1281,295,884x734 screen=2560x1440 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=15% used=8639MB
free_memory_gb=9.9
VERDICT=BUILD_OK
```

（批次 1 减账点名＝波 14 判据格 #50 `Blank_candidate_id…`、#51 `Blank_claim…`、#52 `Blank_proposed_by…`、#53 `Blank_intent_id…`、#54 `Blank_intent_topic…`、#55 `Blank_action_id…` 共 6 格，十一项字段齐，本手为另一双手的红。）

### 批次 2（A2–A6）——19:51:35～19:51:57，五轮全 `VERDICT=BUILD_OK`、0 HOLD（A2／A3 轮前台窗＝electron `API` 但 `covers=False`→不 HOLD）

| ID | ①文件:行 | ②改前 / ③改后 | ④diff --stat | ⑤跑法 | ⑥TRX | ⑦红格全名＋消息要点＋红点行 | ⑧还原 | ⑨status | ⑩突变态SHA / 还原后SHA | ⑪预检 |
|---|---|---|---|---|---|---|---|---|---|---|
| A2 | V:200 | `if (action.PlannedSegmentCount < 1 \|\| action.PlannedSegmentCount > request.MaxOutboundSegments)` ／ `if (action.PlannedSegmentCount < 1)`（削上界项） | 1 file changed, 1 insertion(+), 1 deletion(-) | RID=run-A2 | `…_19_51_35.trx` (1) | `…Immediate_action_segment_above_host_ceiling_is_refused`；`Assert.Single() Failure: The collection was empty`；`:133` | 通用 | 0 | `6c5ec1e9…becf` / `88ebf5c3…b00c` | BUILD_OK free=9.4 |
| A3 | V:214–218 | 删整门 `if (action.DeferredIntentId is not null){…rejections.Add(…IntentClaimedAsFact…)}`／无 | 1 file changed, 5 deletions(-) | RID=run-A3 | `…_19_51_40.trx` (1) | `…Immediate_action_that_also_hangs_a_future_intent_is_refused`；`Assert.Single() Failure: The collection was empty`；`:147` | 通用 | 0 | `dbbd0436…3b2f` / `88ebf5c3…b00c` | BUILD_OK free=9.4 |
| A4 | V:223–227 | 删 `if (action.PlannedSegmentCount != 0){…SegmentCountExceeded…}`／无 | 1 file changed, 5 deletions(-) | RID=run-A4 | `…_19_51_46.trx` (1) | `…Deferred_action_reserving_delivery_segments_is_refused`；`Assert.Single() …empty`；`:161` | 通用 | 0 | `1e822793…90af` / `88ebf5c3…b00c` | BUILD_OK free=9.3 |
| A5 | V:229–233 | 删 `if (action.ProseQuote is not null){…ActionBindingMissing…}`／无 | 1 file changed, 5 deletions(-) | RID=run-A5 | `…_19_51_51.trx` (1) | `…Deferred_action_carrying_immediate_prose_is_refused`；`Assert.Single() …empty`；`:185` | 通用 | 0 | `ca866b5d…4053` / `88ebf5c3…b00c` | BUILD_OK free=9.2 |
| A6 | V:249 | `if (action.PlannedSegmentCount != 0 \|\| action.ProseQuote is not null)` ／ `if (action.ProseQuote is not null)`（去 `\|\|` 首成员） | 1 file changed, 1 insertion(+), 1 deletion(-) | RID=run-A6 | `…_19_51_57.trx` (1) | `…Silence_action_reserving_a_segment_is_refused`；`Assert.Single() …empty`；`:209` | 通用 | 0 | `0f9aa6d4…dbb6` / `88ebf5c3…b00c` | BUILD_OK free=9.2 |

（批次 2 减账点名＝波 14 格 #56 `Immediate_action_segment_above_host_ceiling…`、#57 `Immediate_action_that_also_hangs_a_future_intent…`、#58 `Deferred_action_reserving_delivery_segments…`、#59 `Deferred_action_carrying_immediate_prose…`、#60 `Silence_action_reserving_a_segment…` 共 5 格，十一项字段齐。）

### 批次 3（C1–C5）——19:52:35～19:52:58，五轮全 `VERDICT=BUILD_OK`、0 HOLD（前台窗＝QQ `Chrome_WidgetWin_1`，`covers=False`→不 HOLD）

| ID | ①文件:行 | ②改前 / ③改后 | ④diff --stat | ⑤跑法 | ⑥TRX | ⑦红格全名＋消息要点＋红点行 | ⑧还原 | ⑨status | ⑩突变态SHA / 还原后SHA | ⑪预检 |
|---|---|---|---|---|---|---|---|---|---|---|
| C1 | V:387 | 删 `NarrativeGuard.RequireText(commit.CommitId, nameof(commit.CommitId), NarrativeContractReason.ContentHashMissing);`／无 | 1 file changed, 1 deletion(-) | driver 通用行 RID=run-C1 | `…_19_52_35.trx` (1) | `…Blank_commit_id_throws_rather_than_a_hashless_commit_identity`；`Assert.Throws() Failure: No exception was thrown / Expected: NarrativeContractException`；`:236` | `git checkout -- …/NarrativeDraftValidator.cs` | 0 | `5f1ae5a8…cd188` / `88ebf5c3…b00c` | BUILD_OK free=9.3 covers=False |
| C2 | V:389–393 | 删整门 `if (!NarrativeContractVersions.IsSupportedNarrativeVersion(commit.ContractVersion)){…ContractVersionUnsupported…}`／无 | 1 file changed, 5 deletions(-) | RID=run-C2 | `…_19_52_40.trx` (1) | `…Commit_with_an_unsupported_contract_version_is_refused`；`Assert.Single() Failure: The collection was empty`；`:248` | 通用 | 0 | `9518758a…809d7` / `88ebf5c3…b00c` | BUILD_OK free=8.6 |
| C3 | V:395–399 | 删整门 `if (string.IsNullOrWhiteSpace(commit.ContentHash)){…ContentHashMissing…}`／无 | 1 file changed, 5 deletions(-) | RID=run-C3 | `…_19_52_46.trx` (1) | `…Commit_without_host_content_hash_is_refused`；`Assert.Single() …empty`；`:259` | 通用 | 0 | `36ae4696…af44` / `88ebf5c3…b00c` | BUILD_OK free=9.2 |
| C4 | V:403–408 | 删整门 `if (string.IsNullOrWhiteSpace(intent.SourceCommitId)){…IntentClaimedAsFact…continue;}`／无 | 1 file changed, 6 deletions(-) | RID=run-C4 | `…_19_52_52.trx` (1) | `…Commit_intent_without_source_is_refused_by_both_layers`；`Assert.Collection() Failure: Item comparison failure (pos 1)`；`:277` | 通用 | 0 | `802d1286…de79` / `88ebf5c3…b00c` | BUILD_OK free=9.3 |
| C5 | V:410–415 | 删整门 `if (!string.Equals(intent.SourceCommitId, commit.CommitId…Ordinal) && !string.Equals(…, commit.BatchId…)){…IntentClaimedAsFact…}`／无 | 1 file changed, 6 deletions(-) | RID=run-C5 | `…_19_52_58.trx` (1) | `…Commit_intent_whose_source_is_neither_commit_nor_batch_is_refused`；`Assert.Single() …empty`；`:297` | 通用 | 0 | `a5956c72…028a` / `88ebf5c3…b00c` | BUILD_OK free=9.3 |

（批次 3 减账点名＝波 14 格 #61 `Blank_commit_id_throws_rather_than_a_hashless_commit_identity`、#62 `Commit_with_an_unsupported_contract_version_is_refused`、#63 `Commit_without_host_content_hash_is_refused`、#64 `Commit_intent_without_source_is_refused_by_both_layers`、#65 `Commit_intent_whose_source_is_neither_commit_nor_batch_is_refused` 共 5 格，十一项字段齐。）

**活单① 收口**：批次 1（6）＋批次 2（5）＋批次 3（5）＝**16 轮全红、0 轮未取到红**，每轮 `total=16／executed=16／failed=1`、点名唯一对应格、其余 15 格绿，逐轮还原后 SHA＝`88ebf5c3…b00c`（== HEAD-blob）、`git status --short`＝0 行。波 14 判据格 #50–65 共 **16 格**换手成功（另一双手）。16 枚定向突变轮已用（预算 ≤24 上限中，剩 8）。

## 二 活单②｜第二十波 34 格里自选 6 格补可署名配方（逐轮十一项）

跑法＝`.local-notes/wave24-1023/tools/driver2.sh`（本刀自持，SRC 由 spec 每行给定，filter 固定 `NarrativeGapParityPrototypeTests`，RID=`w20-<ID>`），spec＝`.local-notes/wave24-1023/tools/w20.txt`。整行跑法＝`cd "<carrier>" && dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~NarrativeGapParityPrototypeTests" --results-directory "<carrier>/.local-notes/wave24-1023/runs/w20-<ID>" --logger trx`（无 `--no-build`）。每轮 `total=36／executed=36／failed=1`（filter 命中 36 格＝第二十波那份原型判据集），恰好点亮该枚单行突变对应的那一格、其余 35 格绿。6 枚各用**自己的源文件基线 SHA** 还原（`git checkout -- <该文件>`），逐轮还原后 SHA == `git show HEAD:<该文件>`，`git status --short`＝0 行。

选格覆盖 5 个原型文件（攒批/打断/分类/换形/节奏）＋攒批第二枚＝6 格；避开 `NarrativeReplyPacing.cs:91`（`gap < TimeSpan.Zero` 双保险兜底分支，结构性不可发火）。判据文件＝`tests/Pry.Core.Tests/NarrativeGapParityPrototypeTests.cs`（简称 `T:`）。本手＝另一双手在独立载体逐枚点亮，配方十一项齐，可供署名。

| ID | ①文件:行 | ②改前 / ③改后 | ④diff --stat | ⑤跑法 | ⑥TRX | ⑦红格全名＋消息要点＋红点行 | ⑧还原 | ⑨status | ⑩基线SHA / 突变SHA / 还原后SHA | ⑪预检 |
|---|---|---|---|---|---|---|---|---|---|---|
| p1 攒批·唯一真相 | `src/…/NarrativeInputBuffer.cs:172` | `return _claimedSendable.Add(batchId);` ／ `return _claimedSendable.Add(batchId) \|\| _sealed.ContainsKey(batchId);` | 1 file changed, 1 insertion(+), 1 deletion(-) | driver2 整行 RID=w20-p1 | `…_20_00_44.trx` (1) | `NarrativeGapParityPrototypeTests.InputBuffer_one_batch_yields_exactly_one_sendable_truth`；`Assert.False() Failure: Expected False Actual True`（第二次领取被判成可领）；`T:97` | `git checkout -- …/NarrativeInputBuffer.cs` | 0 | `1e40dfb3…4067` / `86ebbb62…1956` / `1e40dfb3…4067` | 20:00 BUILD_OK free=9.8 |
| p6 攒批·hold 截止 | `src/…/NarrativeInputBuffer.cs:136` | `if (now - _openedAt < _ceiling.MaxBatchHold)` ／ `if (now - _openedAt <= _ceiling.MaxBatchHold)`（`<`→`<=`，恰好到期点被吞） | 1 file changed, 1 insertion(+), 1 deletion(-) | RID=w20-p6 | `…_20_00_50.trx` (1) | `…InputBuffer_hold_deadline_seals_the_batch_only_once_elapsed`；`Assert.True() Failure: Expected True Actual False`（到点未封批）；`T:57` | 通用 | 0 | `1e40dfb3…4067` / `5566fd11…919f` / `1e40dfb3…4067` | BUILD_OK free=9.7 |
| p2 打断·无名理由 | `src/…/NarrativeInterruption.cs:67` | `if (reason is not (UserInterruption or SupersededByNewInput))` ／ 尾部 `… ) && reason is not null`（把 null 从"不可点名即拒"里摘出） | 1 file changed, 1 insertion(+), 1 deletion(-) | RID=w20-p2 | `…_20_00_55.trx` (1) | `…Interruption_without_a_nameable_reason_is_rejected_not_guessed`；`Assert.Equal() Failure: Expected RejectedUnnamedReason Actual Applied`（null 被当成可落终态）；`T:147` | 通用 | 0 | `fca7209a…7fdf` / `e65b5e6d…2c8d` / `fca7209a…7fdf` | BUILD_OK free=9.6 |
| p3 分类·档位映射 | `src/…/NarrativeInterruptClassifier.cs:54` | `InterruptCue.ContinuationMarker => InterruptClass.Continuation,` ／ `… => InterruptClass.NewTopic,` | 1 file changed, 1 insertion(+), 1 deletion(-) | RID=w20-p3 | `…_20_01_01.trx` (1) | `…Classifier_every_cue_maps_to_its_declared_class(cue: ContinuationMarker, expected: Continuation)`；`Assert.Equal() Failure: Expected Continuation Actual NewTopic`；`T:179` | 通用 | 0 | `9f1cfed2…ce13` / `3562d634…5c59` / `9f1cfed2…ce13` | BUILD_OK free=9.6 |
| p4 换形·信号→档 | `src/…/NarrativeBehaviorExpressionMapping.cs:88` | `BehaviorRegisterSignal.Warm => NarrativeExpressionGear.Warm,` ／ `… => NarrativeExpressionGear.Restrained,` | 1 file changed, 1 insertion(+), 1 deletion(-) | RID=w20-p4 | `…_20_01_06.trx` (1) | `…Mapping_register_maps_to_declared_gear_when_scene_does_not_block(register: Warm, expected: Warm)`；`Assert.Equal() Failure: Expected Warm Actual Restrained`；`T:216` | 通用 | 0 | `2bbae3d9…2a61` / `68595540…b359` / `2bbae3d9…2a61` | BUILD_OK free=9.6 |
| p5 节奏·上界封顶 | `src/…/NarrativeReplyPacing.cs:103` | `value < min ? min : value > max ? max : value;` ／ `value < min ? min : value;`（去掉 `> max` 封顶支） | 1 file changed, 1 insertion(+), 1 deletion(-) | RID=w20-p5 | `…_20_01_12.trx` (1) | `…Pacing_min_gap_floors_and_max_gap_caps_the_interval`；`Assert.Equal() Failure: Expected 09:00:02 Actual 09:01:40`（max 不封顶，间隔漏成 100s）；`T:286` | 通用 | 0 | `f0e826df…3fd9` / `b2bce0bb…1d8a` / `f0e826df…3fd9` | BUILD_OK free=9.5 |

（p1/p6 同指 `NarrativeInputBuffer.cs` 但分轮跑、各自还原，无相互污染；p3/p4 为 [Theory] 单例，突变只翻该一枚 InlineData，故 failed=1 而非整组。6 格十一项齐、0 格未取到红。）

**活单② 减账点名（6 格，第二十四波署名配方齐）**：第二十波 `NarrativeGapParityPrototypeTests` 内 ① 攒批唯一真相 `InputBuffer_one_batch_yields_exactly_one_sendable_truth`（V-File:NarrativeInputBuffer:172→T:97）、② 攒批 hold 截止 `InputBuffer_hold_deadline_seals_the_batch_only_once_elapsed`（:136→:57）、③ 打断无名理由 `Interruption_without_a_nameable_reason_is_rejected_not_guessed`（NarrativeInterruption:67→:147）、④ 分类档位 `Classifier_every_cue_maps_to_its_declared_class(ContinuationMarker)`（NarrativeInterruptClassifier:54→:179）、⑤ 换形档位 `Mapping_register_maps_to_declared_gear_when_scene_does_not_block(Warm)`（NarrativeBehaviorExpressionMapping:88→:216）、⑥ 节奏封顶 `Pacing_min_gap_floors_and_max_gap_caps_the_interval`（NarrativeReplyPacing:103→:286）。此 6 格此前（第二十波 §六 r1/r2/r3 及换形孤例）只有红无配方，本手补齐十一项＝第二换手凭证成立。6 枚定向突变轮已用（累计 22／上限 24，剩 2）。

## 三 活单③｜两枚成对门的一次双格凭证（先零成本验证 filter）

跑法＝`.local-notes/wave24-1023/tools/driver3.sh`（filter 固定 `FullyQualifiedName~NarrativeCommitAndActionGuardTests|FullyQualifiedName~NarratorPrototypeTests`，SRC 固定 `NarrativeDraftValidator.cs`，RID=`item3-<ID>`），spec＝`item3.txt`（两枚＝item① 的 A2/A3 同款突变，换成双判据 filter 再跑）。

**先做零成本 filter 验证（无突变，不计入定向突变轮）**：`dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~NarrativeCommitAndActionGuardTests|FullyQualifiedName~NarratorPrototypeTests" --results-directory "…/runs/validate-filter" --logger trx`（预检 `VERDICT=BUILD_OK` 后跑）。实测读数＝`总计:72 通过:72 失败:0`。

- **如实登记一处与工单不符**：工单 §三 预期 16+49=65 节点，盘上实测量＝**72**。差在 `NarratorPrototypeTests`：blame/log 显示它以 49 格经归位提交 `290a603f` 落主树，随后 `639a579`（波 18/21 归位）补 7 枚落门格 **49→56**；56＋16＝72。本刀一律以当场实测（72）为准，不引用工单的 65。filter 全绿＝基线在该组合下不红，成对门复采才有意义。

### 成对门复采（两枚，各点亮 2 格＝1 wave14＋1 wave13）

| ID | ①文件:行 | ②改前 / ③改后 | ④diff --stat | ⑤跑法 | ⑥TRX | ⑦红格（两枚）全名＋消息要点＋红点行 | ⑧还原 | ⑨status | ⑩基线SHA / 突变SHA / 还原后SHA | ⑪预检 |
|---|---|---|---|---|---|---|---|---|---|---|
| pg1 | V:200 | `if (action.PlannedSegmentCount < 1 \|\| action.PlannedSegmentCount > request.MaxOutboundSegments)` ／ `if (action.PlannedSegmentCount < 1)`（削上界项，同 item① A2） | 1 file changed, 1 insertion(+), 1 deletion(-) | driver3 整行 filter=65→72 RID=item3-pg1 | `…_20_03_13.trx` (1) `total=72 executed=72 failed=2` | **wave14 #56** `NarrativeCommitAndActionGuardTests.Immediate_action_segment_above_host_ceiling_is_refused`；`Assert.Single() Failure: The collection was empty`；`:133`。＋ **wave13** `NarratorPrototypeTests.Immediate_action_over_the_host_segment_ceiling_is_refused`；`Assert.Equal() Failure: Expected ContractRejected Actual HistoryRejected`（超上限段数不再被契约门上界挡下、漏到历史门被 HistoryRejected 接走）；`:677` | `git checkout -- …/NarrativeDraftValidator.cs` | 0 | `88ebf5c3…b00c` / `6c5ec1e9…becf` / `88ebf5c3…b00c` | BUILD_OK free=10.1 |
| pg2 | V:214–218 | 删整门 `if (action.DeferredIntentId is not null){…IntentClaimedAsFact…}`（同 item① A3）／无 | 1 file changed, 5 deletions(-) | RID=item3-pg2 | `…_20_03_19.trx` (1) `total=72 executed=72 failed=2` | **wave14 #57** `NarrativeCommitAndActionGuardTests.Immediate_action_that_also_hangs_a_future_intent_is_refused`；`Assert.Single() …empty`；`:147`。＋ **wave13** `NarratorPrototypeTests.Immediate_action_carrying_a_future_intent_is_refused`；`Assert.Single() …empty`（拒绝集被清空）；`:705` | 通用 | 0 | `88ebf5c3…b00c` / `dbbd0436…3b2f` / `88ebf5c3…b00c` | BUILD_OK free=10.1 |

（pg1 突变态 SHA `6c5ec1e9…becf`、pg2 突变态 SHA `dbbd0436…3b2f` 与 item① A2/A3 逐一相同＝同一枚字节改坏，在只含 wave14 的 filter 下点亮 1 格、在含 wave13 的 filter 下同点亮 2 格，成对门坐实：`NarrativeDraftValidator.cs:200` 同时托 wave14#56 与 wave13 该格，`:214`–`:218` 同时托 wave14#57 与 wave13 该格。）

**wave13 历史凭证第二句（如实）**：这两枚 wave13 格（`:671`/`:697`，断言行 `:677`/`:705`、`RunAsync` 调用行 `:675`/`:703`）的当前字节 blame 到归位提交 `290a603f`（波 13 HDS-003「49 格判据」落盘，含 `改传 TestContext.Current.CancellationToken`），文件头计数经 `639a579` 涨到 56；而波 13 载体尖端 `66906d0`（「46 告警与 2 枚 Api 总盘红归零、50/50 改写本文件」）**不是 HEAD 的祖先**——那轮 50/50 改写被并入 `290a603f` 的归位落盘。故：任何在 `66906d0` 载体字节上取过的 wave13 旧红，其字节与 HEAD 现字节不同（且被守门人改写），**旧第二换手凭证对现网字节作废**；本刀在 `f26545d` 现字节上、以另一双手重取这 2 格红＝对 wave13 成对格的现势复采，被守护产品行 `:200`/`:214` blame `5145bdea`（2026-10-21）自始未动。

**活单③ 减账点名（4 格，两 wave14＋两 wave13）**：`NarrativeCommitAndActionGuardTests.Immediate_action_segment_above_host_ceiling_is_refused`（=item① #56，此处成对再取）、`…Immediate_action_that_also_hangs_a_future_intent_is_refused`（=#57）、`NarratorPrototypeTests.Immediate_action_over_the_host_segment_ceiling_is_refused`（:677）、`NarratorPrototypeTests.Immediate_action_carrying_a_future_intent_is_refused`（:705）。2 枚定向突变轮已用（累计 24／上限 24，已用满）。

## 四 出口读数

### 四.1 全量一把（收尾，允许 `--no-build`）

收尾先 `dotnet build Pry.slnx -c Release`（本刀唯一一次解级编译，预检 `BUILD_OK` 后跑，读数 `已成功生成 0 个警告 0 个错误`），再 `dotnet test Pry.slnx -c Release --no-build --results-directory "…/.local-notes/wave24-1023/full" --logger trx`（预检 `BUILD_OK` 后跑）。结果目录新建、四份 TRX 各 1 枚。

计数 TRX 文件＝**4 枚**（`20_05_53`＝Core／`20_05_54`＝Client／`20_05_54[1]`＝Api／`20_05_56`＝App），逐份 `<Counters>`：

| TRX | 项目 | total | executed | passed | failed |
|---|---|---|---|---|---|
| …_20_05_54 | Client | 108 | 108 | 108 | 0 |
| …_20_05_56 | App | 233 | 233 | 233 | 0 |
| …_20_05_54[1] | Api | 280 | 280 | 280 | 0 |
| …_20_05_53 | Core | 712 | 710 | 710 | 0 |
| **逐份求和** | | **1333** | **1331** | **1331** | **0** |

- 读数＝**1333／executed 1331／failed 0**，与在册门禁基线逐位相同（§〇.4）＝**本刀没挪动基线**（零新格、零判据字节改动，符合纯复采刀性质）。
- 两条跳过＝`<Counters>` 的 `notExecuted` 恒为 0（不读它），改按 `<UnitTestResult outcome="NotExecuted">` 计＝**2 枚**，全名点名：`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`、`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`——与在册基线那两条同源，未增未减。
- `scripts/measure-fixed-eval.ps1 -ResultsDirectory "…/wave24-1023/full"`（预检后跑，退出码 0）独立复核：`Catalog valid: 76 permanent scenarios` ＋ `Passed=1331; Failed=0; Other=2`（Other＝那 2 枚 NotExecuted）。

### 四.2 定向轮与预算（读盘上产物计数，不自报）

`.local-notes/wave24-1023/runs/` 下**25 个结果目录、每目录恰 1 枚 TRX**：

- **24 枚定向突变轮**（＝上限用满）：item① `run-R1…R5`(5)＋`run-A1…A6`(6)＋`run-C1…C5`(5)＝16；item② `w20-p1…p6`＝6；item③ `item3-pg1`、`item3-pg2`＝2。
- 1 个 `validate-filter`＝活单③ 的零成本 filter 验证（无突变、不计入定向突变轮、不减账）。
- 解级编译 1 次＋解级全量 1 次（见四.1）＝各 1，符合"≤1 build／≤1 全量"。
- dotnet 调用合计＝24＋1(验证)＋1(build)＋1(全量)＝**27**；每一次之前均跑过 v6 预检且全部 `VERDICT=BUILD_OK`，全程 **0 次 HOLD**。

### 四.3 三笔减账合计（16＋6＋4＝26 格）

- 活单①＝波 14 `NarrativeCommitAndActionGuardTests` **#50–65 共 16 格**，另双手全红、十一项齐（§一）。
- 活单②＝波 20 `NarrativeGapParityPrototypeTests` 自选 **6 格**，补可署名配方（§二）。
- 活单③＝成对门 **4 格**（2 枚 `NarrativeDraftValidator` 突变同时点亮 wave14 #56/#57 ＋ wave13 两枚 `NarratorPrototypeTests` 即时动作格）（§三）。
- **26 格全取到红，0 格标"未取到红"**；本手为另一双手，未造常绿门、未松/删/`Assert.Skip`/注释任何既有断言、未改数字门槛。

### 四.4 载体足迹（字节面＝零）

- 载体 `git rev-parse HEAD`＝`f26545d`（全程未动），`git status --short`＝**0 行**，`git diff --check`＝干净（退出 0）。
- `NarrativeDraftValidator.cs` 终态 SHA-256＝`88ebf5c3…b00c` == HEAD-blob；5 枚原型源文件（InputBuffer/Interruption/Classifier/Mapping/ReplyPacing）终态 SHA 逐一 == 各自 HEAD-blob（`1e40dfb3…4067`／`fca7209a…7fdf`／`9f1cfed2…ce13`／`2bbae3d9…2a61`／`f0e826df…3fd9`）。突变只发生在载体工作树、每轮 `git checkout --` 当场还原，无持久化字节改动。

### 四.5 身份面（本刀允许文件差为空）

入场记录线尾＝`f26545d`。核 `git diff --stat f26545d..HEAD -- src tests scripts Pry.slnx Directory.Build.props`＝**空**（0 行输出）。

- 如实登记：入场至今，主树分支 `codex/ui-main-window-redesign` 已由**他人**推进 1 枚提交 `0790d71`（`docs(plans,wave25)`，仅新增 `docs/dispatch-hds-wave25-20261023.md`，88 行纯文档），**不碰 src/tests/scripts/Pry.slnx/Directory.Build.props**——故上述允许文件差仍为空。本刀载体仍钉在我方线尾 `f26545d`，未随该 foreign 提交移动，足迹为零。
- `f26545d..0790d71` 的 `git diff --check`＝干净（退出 0）。

## 五 归位通道（守门人核，不在本 lane 权限内）

见工单 §五；本刀无代码落盘面，交付物只有本报告＋`.local-notes` 里的仪器与 TRX 证据。

## 六 仍未验证（逐条点名）

见工单 §六：真实模型对照（零读数）、生产接线与切换（`NarrativeTurnPipeline` 零生产调用方）、UI 可见行为（零读数）、生产库（零接触）、待标定参数权威值、与 HDS-007 到期再叙事的关系；另加本刀特有的一句——**本刀只证明"这些门会被单枚改坏弄响"，不证明"产品今天行为正确"**；判据覆盖度≠功能正确性，也不许被读成旧链可删／不进包已做到（那两条仍不成立）。

---

## 七 出口核验与归位（守门人，签名 `eval-QA-WAVE24-GK-1023`，10-23 20:2x）

上面 §〇–§六 是第二十四波 lane 的原文，我一字未改、未补写、未代署。这一节是我这一手的独立复数：只读盘上原始产物，不引用它的自述当证据。

### 七.1 逐轮 `<Counters>` 重读（仪器＝`grep -ho 'total=… executed=… passed=… failed=…'`，目录 `.local-notes/wave24-1023/`）

盘上 `runs/` 实数 **25 个结果目录、每目录恰 1 枚 `.trx`**，与 §四.2 的自述逐位相符；`full/` 实数 4 枚 TRX。

| 轮集 | 我读到的 `<Counters>` | 与报告声称 |
|---|---|---|
| `run-R1…R5`＋`run-A1…A6`＋`run-C1…C5`（16 轮） | 每轮 `total=16 executed=16 passed=15 failed=1` | §一 相符 |
| `w20-p1…p6`（6 轮） | 每轮 `total=36 executed=36 passed=35 failed=1` | §二 相符 |
| `item3-pg1`、`item3-pg2`（2 轮） | 每轮 `total=72 executed=72 passed=70 failed=2` | §三 相符 |
| `validate-filter`（无突变） | `total=72 executed=72 passed=72 failed=0` | §三 相符 |

- 红格归属我按 `outcome="Failed"` 的 `testName` 逐轮抓取，不是看结论行：16 轮 `run-*` 的失败格名逐字等于 §一 三张表点名的那 16 格（`NarrativeCommitAndActionGuardTests.Blank_*` 五枚／`…action_*` 与 `Immediate_*`／`Deferred_*`／`Silence_*`／`Commit_*` 十一枚）；6 轮 `w20-*` 等于 §二 点名的 6 格（含两枚 `[Theory]` 单例，参数串 `cue: ContinuationMarker, expected: Continuation` 与 `register: Warm, expected: Warm` 在 TRX 里就是这个名字）；2 轮 `item3-*` 各含两格，恰为 wave14 的 `Immediate_action_segment_above_host_ceiling_is_refused`／`…that_also_hangs_a_future_intent_is_refused` 加 wave13 的 `NarratorPrototypeTests.Immediate_action_over_the_host_segment_ceiling_is_refused`／`…carrying_a_future_intent_is_refused`。没有一轮多红、没有一轮空转。

### 七.2 全量一把与基线（四份 TRX 当场求和）

`full/` 四份：Core `total=712 executed=710 passed=710 failed=0`；Client 108／108／108／0；Api 280／280／280／0；App 233／233／233／0 ⇒ **1333／executed 1331／failed 0**。四条 `notExecuted` 计数器全为 0（不读它），按 `outcome="NotExecuted"` 点名实得 **2 枚**：`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`、`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`。与在册基线 1333／1331／0／2 逐位相同 ⇒ 本刀一格新判据也没加、基线不挪，这一格由我确认。

### 七.3 字节足迹（主树＋载体两侧都数）

- 载体 `.local-notes/qa-wave24-1023/carrier`：`git rev-parse HEAD` = `f26545d`，`git status --short` = 0 行。
- 我在主树对六枚被改坏的源文件逐一取 SHA-256，并与 `git show HEAD:<该文件>` 的哈希比：`NarrativeDraftValidator.cs` 88ebf5c3e3c6、`NarrativeInputBuffer.cs` 1e40dfb3f937、`NarrativeInterruption.cs` fca7209a7163、`NarrativeInterruptClassifier.cs` 9f1cfed2a62b、`NarrativeBehaviorExpressionMapping.cs` 2bbae3d9922b、`NarrativeReplyPacing.cs` f0e826dfb352 — 六枚工作副本与 HEAD blob 全部相同 ⇒ 无持久化字节改动，与 §四.4 相符。
- 主树 `git status --short` 上他人名下那两枚暂存项与七枚未跟踪项跑前跑后一字未动；本刀在我这一手只新增本报告一份文件。

### 七.4 第二换手资格（这一节决定它能不能签账）

判据字节与产品字节的 last-touch 我逐个 `git log -1` 取，不靠推演：

| 文件 | last-touch | 归属 |
|---|---|---|
| `tests/Pry.Core.Tests/NarrativeCommitAndActionGuardTests.cs` | `d3fbac4` | 第十四波归位，本刀未改 |
| `tests/Pry.Core.Tests/NarrativeGapParityPrototypeTests.cs` | `59252aa` | 第二十波归位，本刀未改 |
| `tests/Pry.Core.Tests/NarratorPrototypeTests.cs` | `639a579` | 第十八／二十一波归位 |
| `src/Pry.Core/Narrative/NarrativeDraftValidator.cs` | `5145bde` | 2026-10-21 起未动 |
| 五枚原型 `src/…`（InputBuffer／Interruption／Classifier／Mapping／ReplyPacing） | `59252aa` | 第二十波归位 |

⇒ 取红的这只手既不是写这些判据的手，也不是改过判据字节的手（`66906d0` 那一改不在 HEAD 祖先里，§三 那句 blame 我也按 `290a603f`／`639a579` 核过），且十一项字段逐轮齐（①②③④⑤⑥⑦⑧⑨⑩⑪）。**这两笔合起来才让"换人"从声称变成凭证**，所以这一格我签。

### 七.5 减账口径更正：26 格是轮次口径，在册减账＝24 格 distinct

- lane 在 §四.3 写「三笔减账合计 16＋6＋4＝26 格」，那是**逐笔点名**的格子数；其中 `Immediate_action_segment_above_host_ceiling_is_refused`（#56）与 `…that_also_hangs_a_future_intent_is_refused`（#57）在活单① 与活单③ 各被点过一次，**同一格不能签两次**。
- 我按 distinct 格名重算＝**24 格**：波 14 那 16 格（#50–65，全清）＋波 20 那 34 格里的 6 格＋波 13 那 49 格里的 2 格。
- 因此全仓"反证缺第二换手"的现账＝**75 格**（波 13 余 47＋波 20 余 28），旧值 99 自此过期。波 14 那 16 格自此**全部有第二只手**；波 13 那 47 格与波 20 那 28 格照旧欠，**不许被这句读成"不需要复采"**。

### 七.6 归位通道与实际落盘

本刀性质＝纯复采／证据刀，交付物只有本报告加 `.local-notes` 里的仪器与 TRX（gitignored），所以归位＝**一把零 `dotnet` 的文档刀**：本报告入库＋`PROJECT_PLAN.md` §9 交接 entry＋一页纸 §0 与 §3 的大白话与顶账。`src`／`tests`／`scripts`／`Pry.slnx`／`Directory.Build.props` 一字未动，主树全程未起构建，不打扰作者机器。顶账（`git add` 本报告后当场实测）＝**跟踪文件条数 601**，链＝598（两份撞线报告）→599（第二十四波工单）→600（第二十五波工单 `0790d71`）→601（本刀这份报告）；条数不是门槛。`main` 按作者那句「可以并线」只挪 ref 跟到线尾，仍零 tag、零 push、零打包。

### 七.7 我这一手也没做的事（照登）

真实模型对照、生产接线、UI 可见行为、生产库、待标定参数权威值——本刀零读数，§六 那六条原样有效。另加一句边界：这一把把 24 格的"有人独立压响过"补上了，它**不证明产品今天的行为正确**，也不减少 §六 里任何一条未验证；判据覆盖度和功能正确性是两本账。
