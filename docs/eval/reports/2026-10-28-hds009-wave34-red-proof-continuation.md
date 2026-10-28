# HDS-009 第一片·第二刀（续跑证据刀）报告：给第三十三波 19 枚无红格逐枚配方向类突变（HDS-WAVE34-1028）

- 工单：`docs/dispatch-hds-wave34-1028.md`（唯一权威工单）
- 执行方：冷启动 lane（第三十四号载体＝沿用第三十三波那棵，不新建 worktree）
- 性质：证据刀·续跑（零新增产品文件、零产品字节持久改动、A 类轮零判据字节改动、零新增数字门槛、零落地、零归位、零顶账）

> 状态词、结案词、在册基线判定由守门人写，本 lane 不代写、不自封。三句话照旧不写（新链已接线／旧链可删／不进包已做到），上一把那句照旧不写（她的状态已经迁到新链）。

## 〇 入场读数与前置

### 〇.2 入场读数（当场取，未引用工单数字）

- `date`：`Wed Oct 28 20:05:59     2026`
- `git rev-parse HEAD`（主树）：`9a15e2aba37f93b3eb71ecf6f3e86365664e007a`
- `git status --short --branch`（主树原样）：
  ```
  ## codex/ui-main-window-redesign
  M  docs/dispatch-tip1020-02-memory-group-judgment.md
   M docs/plans/PROJECT_PLAN.md
  M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs
  ?? .qoder/
  ?? docs/character-learning-safety.patch
  ?? docs/research/HDS_INTERLUDE.md
  ?? docs/research/HDS_QQ_RESEARCH_INDEX.md
  ?? docs/research/QQ_BRIDGE.md
  ```
  他人两枚 `M ` 暂存项与全部 `??`＝别人的，本刀不读内容、不改、不提交。` M docs/plans/PROJECT_PLAN.md` 是在册总计划的在途登记，不属本刀文件面，本刀只在 §八 交回时提示守门人更新。
- `git worktree list`：主树在 `9a15e2a [codex/ui-main-window-redesign]`；`C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave33-1028/carrier 51f149d (detached HEAD)`＝本刀载体（沿用，未新建）。其余为他人 worktree，未动。
- `git log --oneline -3`（主树）：`9a15e2a`（第三十三波收口文档刀）→ `e04e7e6`（第三十三波代码刀归位）→ `7418615`（第三十三波派发）。线尾已被收口刀从 `7418615` 挪到 `9a15e2a`，故 §〇.3 右端全部当场现取。

### 〇.3 载体入场自证（右端一律当场取，不预写）

- `git -C <carrier> rev-parse HEAD` = `51f149d0b722e28a6566c93d19f48a94ca4c85df` ✓（等于工单指定线尾）
- `git -C <carrier> status --short` = **0 行** ✓
- `git -C <carrier> diff --stat fd93ebe $(git -C <carrier> rev-parse HEAD) -- src tests scripts Pry.slnx Directory.Build.props` = **只有第三十三波那六枚新文件、+822/−0** ✓（DeliveryAttribution 94／ExperienceMaterial 57／LegacyInheritance 78／NarrativeAuthority 46／StateLayering 102／StateAndLearningPrototypeTests 445）
- 本刀专属·主树侧 0 行式子：`git -C C:/Users/24782/Desktop/Pry_Project diff --stat 51f149d $(git rev-parse HEAD=9a15e2aba37f93b3eb71ecf6f3e86365664e007a) -- src tests scripts Pry.slnx Directory.Build.props` = **0 行** ✓ ⇒ 载体树与主线归位后的产品／判据字节逐字符相同，本刀在载体里取到的红就是主线那棵树的红。
- 五枚产品文件＋判据文件的 blob SHA-256（`git show 51f149d:<path> | sha256sum`，与载体工作副本、主树工作副本三侧同值，入场实测逐字符相等）：
  - `StateLayering.cs` = `af727a9c04355d853f756a8cd6d22e374ac889042c847f99906784e0bb20ca3a`
  - `NarrativeAuthority.cs` = `000f2fc8bfd5d73ba7863029c8acafd7aa9df92ef11c910ccda3a28db3f13c97`
  - `DeliveryAttribution.cs` = `5858a75da3ed70e35f93782e1d760c87eea3feeecdafcdd645b27f69ea66f0ab`
  - `ExperienceMaterial.cs` = `289fc89be087fa35bace12b33805060562a210764a9774102b5a2875fcd55169`
  - `LegacyInheritance.cs` = `6a1f50999e7caf1d6d29cc07bbe752f89ea17851999013c00b167e590f3498ab`
  - 判据 `StateAndLearningPrototypeTests.cs` = `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc`（A 类每一轮 ⑩ 项对账用的就是这个值）

### 〇.4 在册基线起点

- 从上一把报告 §七.2（守门人划账面）当场读到的数：**1406／executed 1404／failed 0／跳过 2**，分项 Client 108／App 233／Api 280／Core **785**、Core executed **783**；两条同名跳过＝`Configured_llama_server_requires_the_child_environment_key`／`Configured_sense_voice_model_can_run_offline`。
- 本刀**新格数＝0**（工单 §二明令不许新增判据），出口读数应当仍是 1406；收尾全量若读到别的数，先怀疑树没还原干净，照实登记并作废那一轮，不改判据不改基线。

### 〇.5 取数仪器自证（起跑第一轮之前，盘上四份已知读数）

仪器＝本 lane 自写 `.local-notes/wave34-1028/w34-count.js`（不复用 wave33 的 `w33-count.js`／`gk33-count.js` 实现）：只数 `<UnitTestResult … outcome="Failed">` 的**开标签** occurrence（逐 occurrence 扫描＋引号感知找标签末，自闭合 `/>` 单独识别），与同份 `<Counters failed>` 双向对账（不等打印 `DISAGREE`）；抽红格名／消息／行号用逐 occurrence 切片，不用开—闭配对正则。整段输出归档 `out/instrument-selfproof.txt`：

| 目录 | 期望 | 实测 countersFailed / openTagFailed | 对账 |
|---|---|---|---|
| `../wave29-1024/out/trx-M1` | 恰 1 | 1 / 1 | OK，红格名＋MSG「Expected RejectedForStaleEffect / Actual Advanced」＋`:322` 全非空 |
| `../wave29-1024/out/trx-M5` | 恰 3 | 3 / 3 | OK，三枚格名＋行号 271／287／309 全非空 |
| `../wave29-1024/out/trx-L6` | 恰 2 | 2 / 2 | OK，两枚格名＋行号 99／188 全非空 |
| `../wave33-1028/out/trx-F` | 4 份且 failed 全 0 | 0/0/0/0 | OK，total=108／785／280／233，SUM 1406／executed 1404／failed 0 |

**仪器四格全对 ⇒ 允许用它裁这一把。**

### 〇.6 焦点预检与预算声明

- 预检脚本＝`.local-notes/game-preflight6-1022.ps1`，每一次 `dotnet` 之前当场重取；编号从 **P49** 起；`VERDICT=BUILD_OK` 才起跑，`HOLD` 则不起跑、不请示、不改判据、不绕过，HOLD 后重跑 ≤8 枚、相邻不短于 5 分钟。
- 预算：解决方案级构建 ≤1（判定见下）；解决方案级全量 ≤1（语义＝交付的读数只许来自 1 次全量）；定向 ≤8（A 类）＋≤2（B 类）。A／B 轮与收尾全量一律**不带 `--no-build`**。
- **B1（解决方案级构建）跳过判定登记**：§〇.3 主树侧 0 行式子成立 ✓，且载体 `src/Pry.Core/bin/Release/net10.0/Pry.Core.dll` 在盘（2026-10-28 19:33，第三十三波 P48 收尾全量后无源码改动，载体 `status --short`＝0 行）⇒ 按工单 §一.1 允许跳过 B1，解决方案级构建预算用 0/1；每一轮定向与收尾全量自行增量重建，不存在"跑的不是当前源码"的形状。
- 作废轮（如有）的结果目录一律改名留档 `trx-<轮>-voided-<预检号>/`，不清空不删除。

### 〇.7 他人资产与 git 写边界声明

主树全程零 `git commit/add/checkout/reset/cherry-pick/merge/tag/push/apply/stash/clean/rm`；两枚 `M ` 暂存项与全部 `??` 未读不改。载体内只发生文件内容写入（临时突变）与逐轮还原，本刀**零提交**（线尾仍 `51f149d`）。

## 一 文件面自证（本刀实际写入清单）

- `.local-notes/wave34-1028/`（gitignored）：`w34-count.js`（仪器）、`w34-mutate.js`（突变＋还原）、`mutation-prep/`（10 枚突变临时副本）、`out/`（预检、manifest、各轮 TRX、计数存档）。
- 主树唯一新文件＝本报告。
- 载体内：仅 `src/Pry.Core/Narrative/StateAndLearning/*.cs` 五枚与 `tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs` 的**临时**突变，逐轮还原；终态相对 `51f149d` 净写入 0 字节。
- 未新建任何产品文件、未新建任何测试文件、未碰 `scripts/**`／`Pry.slnx`／`Directory.Build.props`／其他 `docs/**`。

## 二 三计数自证（不许新增判据格）

判据文件 `tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs`（载体侧，入场时）当场 `grep -c`：
- `[Fact]` = **23**
- `[Theory` = **1**
- `[InlineData` = **3**

与上一把报告 §〇.4／§二 的三计数（23／1／3）**逐位相同** ⇒ 本刀净新增格数＝0。B1／B2 两枚反面探针只改扫描函数体内的样本串／谓词，不改任何 `Assert` 期望值、不增删格、不注释断言、不 `[InlineData]` 变动；B 类全程单列登记、不签第二换手。

## 三 反证轮（事前论证先落盘；跑序固定 A1→A8→B1→B2→全部还原核 SHA→F）

### 三.0 事前论证（抄自工单 §三 的表＋本 lane 逐行读码后的独立复推；下表是假设不是结论，实测形状不符⇒整轮作废、红不署、不追第二轮、不改本节原文）

定向跑法＝`dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "<wave34 out/trx-轮>" --logger trx`（在载体内跑，不带 `--no-build`）。突变文本已在动笔前于临时副本合成并记录 SHA-256（`out/mutation-prep-sha.txt`／`mutation-prep/`）。

| 轮 | 类别 | 文件:行（`51f149d` 侧） | 改法 | 消费点论证（本 lane 读码复推） | 预期恰响格（逐字符，前缀 `Pry.Core.Tests.StateAndLearningPrototypeTests.`） | 枚数 |
|---|---|---|---|---|---|---|
| A1 | 行为 | `NarrativeAuthority.cs:33-34` | 摘掉闸①（亲密）整段 `if` | `Assess` 顺序＝①→②→③。单独 `WritesIntimacyState=true` 的输入两处：`c-1`（真,假,假）摘后放行⇒`Assert.False(Allowed)` 红；`c-4`（真,真,真）被闸②接住⇒Allowed 仍 false 但拒因换成 `author_boundary_is_not_narrator_mutable`≠期望的 intimacy 拒因，红。`c-2` 不读被摘支。 | `A_narrative_candidate_may_not_write_intimacy_state`＋`A_candidate_touching_all_three_surfaces_is_caught_by_the_first_gate` | **恰 2** |
| A2 | 行为 | `NarrativeAuthority.cs:41-42` | 摘掉闸③（用户情绪）整段 `if` | 单独 `DeclaresUserEmotion=true` 只有 `c-3`；`c-1`/`c-2` 各被前两道闸拦下、`c-4` 被闸①先拦，拒因不变仍绿。 | `A_narrative_candidate_may_not_declare_the_users_emotion` | 恰 1 |
| A3 | 行为 | `StateLayering.cs:78-79` | 摘掉 `ReviseCanon` 作者侧来源闸 | 缺来源/空标识输入只在 canon-2 那格（null＋blank 两次调用同格）；`FromNarrativeOutput=true` 仍被 :74 拦（M1 格不受影响）；带来源的 canon-1 那格仍 Applied 且 :82 不读 `Source`，无 NRE。 | `An_author_side_canon_revision_without_a_source_is_refused_with_its_own_reason` | 恰 1 |
| A4 | 行为 | `DeliveryAttribution.cs:71` | 摘掉成功路径 `_registry.Add(...)`（只留读数不入册） | 全判据断 `Registry` 的格共 5 枚：4 枚断 `Assert.Empty`（摘后仍空⇒绿），只有 body-3 那格断 `Assert.Single(Registry)`⇒红。`readout.Admitted`/`Reason` 断言（:213-214 与 :216）不受影响、单点因在入册行。 | `An_experience_candidate_is_recorded_only_when_delivery_and_a_click_are_both_present` | 恰 1 |
| A5 | 行为 | `DeliveryAttribution.cs:68-69` | 摘掉闸③（缺点击不得入册） | 「投递但没点击」输入只在 body-4 那格；body-3 有点击不受影响；body-1 被闸②拦；theory 三行走闸①。摘后 body-4 走到入册⇒`Assert.False(Admitted)` 红。 | `A_delivered_body_without_an_explicit_click_is_not_recorded` | 恰 1 |
| A6 | 行为 | `DeliveryAttribution.cs:60` | 只摘合取第一支 `evidence.BodySuppressed ||`（留其余两支） | theory 三行各只置一枚排除位：`(suppressed: True, deduplicated: False, rejected: False)` 摘后漏过闸①、两证齐⇒入册红；`(False, True, False)`/`(False, False, True)` 仍被 `Deduplicated`/`Rejected` 拦下保持绿；其余格不读此行。 | `A_delivered_body_that_was_suppressed_deduplicated_or_rejected_is_not_attribution_evidence(suppressed: True, deduplicated: False, rejected: False)` | 恰 1（另两行必须保持绿，三行一起红＝摘错支，整轮作废） |
| A7 | 行为 | `LegacyInheritance.cs:50-53` | 摘掉 `Import` 的「来源缺失即失败」整道闸 | 缺来源输入两处：old-1/old-2 那格（:360 null Source、:361 版本缺失）摘后在 :57 `input.Source.SourceId` 抛 NRE⇒格红；三条输入那格第二条（StateKey b, null Source）同样抛出⇒格红。带来源的 old-3 那格结果不变。`AttemptWriteback` 那格不调 `Import`。 | `Inherited_state_without_a_pointable_source_fails_loudly_and_is_never_silently_dropped`＋`Every_inherited_input_leaves_a_named_readout_rather_than_silently_vanishing` | **恰 2** |
| A8 | 行为 | `ExperienceMaterial.cs:56` | `NotOffered` 工厂第 5 枚实参 `CarriesSpeakingAuthority` false→true（不动第 2 枚 `Available`） | 对**被拒读数**断 `CarriesSpeakingAuthority` 的只有权限格（:302 `Assert.False(notStored.CarriesSpeakingAuthority)`，走 `:47`→`NotOffered`）；:296 offered 走 :52 不受影响；缺有效期格/超期格只断 Available/Reason/OptionalMaterial；窗口内格断的是 :52 路径。 | `Experience_never_carries_authority_over_whether_to_speak` | 恰 1 |
| B1 | 反面探针 | `StateAndLearningPrototypeTests.cs:419` | `LegacyChainNames.Where(name => …Contains…)` → `Where(name => false)`（恒空，不改 `Assert.Equal` 探针句） | 五枚产品文件仍 `Assert.Empty` 通过；探针样本要求七枚名字按序全出⇒返回空串≠期望串，那格当场红。 | `State_and_learning_prototype_sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites` | 恰 1 |
| B2 | 反面探针 | `StateAndLearningPrototypeTests.cs:423-424` | 两条 `Contains` 样本串换成必然不存在的 token（不改 `Assert.Equal("const,static readonly", …)`） | 五枚产品文件仍 `Assert.Empty` 通过（盘上产品文件本就无 `const `/`static readonly`，波 33 已核）；探针样本含 `const `/`static readonly` 却扫不出⇒返回空集≠两枚 token，那格当场红。 | `State_and_learning_prototype_declares_no_source_side_numeric_default` | 恰 1 |

（B 类署名规则照工单：动判据自身字节⇒只结红证、不签第二换手；A 类每一轮 ⑩ 项给出判据文件 SHA-256 并等于 `317d2171…`（`51f149d` blob）⇒红证＋第二换手双结清。）

### 三.A1 定向轮（P49 ⇒ A1）

预检 **P49**（`out/preflight-P49.txt`）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: QQ pid=27508 class=Chrome_WidgetWin_1 style=0x14C70000 rect=-1911,220,993x802 screen=1920x1080 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=16% used=2144MB
free_memory_gb=10.2
VERDICT=BUILD_OK
```

十一项：

1. 文件:行＝`NarrativeAuthority.cs:33-34`（载体内）
2. 改前原文＝`        if (attempt.WritesIntimacyState)\n            return new AuthorityReadout(attempt.CandidateId, false, "intimacy_state_is_not_narrator_writable");`
3. 改后原文＝`        // MUTATION A1 (temporary, wave34): gate 1 if-block removed`（闸①整段摘除）
4. `git diff --stat`＝`src/.../NarrativeAuthority.cs | 3 +--`，1 file changed, 1 insertion(+), 2 deletions(-)
5. 跑法整行＝`dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave34-1028/out/trx-A1" --logger trx`（于载体内，不带 `--no-build`）
6. TRX 文件名＝`trx-A1/24782_PHYTHLINK_2026-10-28_20_10_04.trx`（1 枚，先数后读）
7. 红格点名（2 枚，逐字符）：`Pry.Core.Tests.StateAndLearningPrototypeTests.A_narrative_candidate_may_not_write_intimacy_state`（MSG：Assert.False Expected False/Actual True；STACK 判据帧 `:131`）＋`Pry.Core.Tests.StateAndLearningPrototypeTests.A_candidate_touching_all_three_surfaces_is_caught_by_the_first_gate`（MSG：Expected "intimacy_state_is_not_narrator_writable"/Actual "author_boundary_is_not_narrator_mutable"；STACK `:167`）。仪器读数 total=26/executed=26/passed=24/countersFailed=2/openTagFailed=2/reconcile=OK（`out/a1-count.txt`）
8. 还原命令＝`node w34-mutate.js revert A1`（写回 `git show 51f149d:<file>` blob 逐字节）
9. 还原后载体 `git status --short`＝**0 行**
10. SHA-256：突变态 `NarrativeAuthority.cs`＝`593c260e03a98d95f7a77fd1194418c4801a7451005790c2f4f8f87b27ba9d3b`（＝预合成 manifest 值）；还原后六枚逐字符＝`000f2fc8…`／`af727a9c…`／`5858a75d…`／`6a1f5099…`／`289fc89b…`；**判据文件＝`317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc`＝`51f149d` blob ✓**（A 类换手凭据在场）
11. 结论＝**恰 2 枚清洁红，格名与 §三.0 事前论证逐字符相符**（A1 是事前论证的恰 2 形状）⇒ 通过；定向预算 1/10。

### 三.A2 定向轮（P50 ⇒ A2）

预检 **P50**（`out/preflight-P50.txt`）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: Godot pid=42864 class=Engine style=0x16CF0000 rect=309,365,956x599 screen=2560x1440 covers=False borderless=False title=???? ? ???? (DEBUG)
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=16% used=2079MB
free_memory_gb=10.6
VERDICT=BUILD_OK
```

十一项：

1. 文件:行＝`NarrativeAuthority.cs:41-42`
2. 改前原文＝`        if (attempt.DeclaresUserEmotion)\n            return new AuthorityReadout(attempt.CandidateId, false, "user_emotion_is_not_narrator_assertable");`
3. 改后原文＝`        // MUTATION A2 (temporary, wave34): gate 3 if-block removed`
4. `git diff --stat`＝1 file changed, 1 insertion(+), 2 deletions(-)
5. 跑法整行＝同 A1（`--results-directory .../out/trx-A2`，不带 `--no-build`）
6. TRX＝`trx-A2/24782_PHYTHLINK_2026-10-28_20_10_55.trx`（1 枚，先数后读）
7. 红格点名（1 枚）：`Pry.Core.Tests.StateAndLearningPrototypeTests.A_narrative_candidate_may_not_declare_the_users_emotion`（MSG：Assert.False Expected False/Actual True；STACK 判据帧 `:153`）。读数 total=26/executed=26/passed=25/countersFailed=1/openTagFailed=1/OK（`out/a2-count.txt`）
8. 还原＝`node w34-mutate.js revert A2`
9. 还原后载体 status＝**0 行**
10. SHA-256：突变态 `3fcd17d6f16a511ce80f3d0ad67d36609376820586fdcd710243067b5e7c873b`（＝manifest）；还原后六枚全 match=true；**判据文件 `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc`＝`51f149d` blob ✓**
11. 结论＝**恰 1 枚清洁红，格名＝事前论证** ⇒ 通过；定向预算 2/10。

### 三.A3 定向轮（P51 ⇒ A3）

预检 **P51**（`out/preflight-P51.txt`）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: Godot pid=42864 class=Engine style=0x16CF0000 rect=309,365,956x599 screen=2560x1440 covers=False borderless=False title=???? ? ???? (DEBUG)
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=14% used=2097MB
free_memory_gb=10.3
VERDICT=BUILD_OK
```

十一项：

1. 文件:行＝`StateLayering.cs:78-79`
2. 改前原文＝`        if (request.Source is null || string.IsNullOrWhiteSpace(request.Source.SourceId))\n            return new LayerReadout(request.CanonKey, "Rejected", "canon_source_unattributable", null);`
3. 改后原文＝`        // MUTATION A3 (temporary, wave34): author-side source gate removed`
4. `git diff --stat`＝1 file changed, 1 insertion(+), 2 deletions(-)
5. 跑法整行＝同前（`--results-directory .../out/trx-A3`，不带 `--no-build`）
6. TRX＝`trx-A3/24782_PHYTHLINK_2026-10-28_20_11_20.trx`（1 枚）
7. 红格点名（1 枚）：`Pry.Core.Tests.StateAndLearningPrototypeTests.An_author_side_canon_revision_without_a_source_is_refused_with_its_own_reason`（MSG：Expected "Rejected"/Actual "Applied"；STACK 判据帧 `:75`）。读数 26/26/25/failed=1/openTag=1/OK（`out/a3-count.txt`）；无 NRE（:82 不读 Source，与事前论证形状一致）
8. 还原＝`node w34-mutate.js revert A3`
9. status＝**0 行**
10. SHA-256：突变态 `6be0c89313e806d156d0ea7fbf67f392380312d28f38fcef9b1ad8ef51e6e53b`（＝manifest）；还原后六枚全 match=true；**判据 `317d2171…`＝blob ✓**
11. 结论＝**恰 1 枚清洁红** ⇒ 通过；定向预算 3/10。

### 三.A4 定向轮（P52 ⇒ A4）

预检 **P52**（`out/preflight-P52.txt`）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: Godot pid=42864 class=Engine style=0x16CF0000 rect=445,324,956x599 screen=2560x1440 covers=False borderless=False title=???? ? ???? (DEBUG)
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=15% used=2082MB
free_memory_gb=10.1
VERDICT=BUILD_OK
```

十一项：

1. 文件:行＝`DeliveryAttribution.cs:71`
2. 改前原文＝`        _registry.Add(new AdmittedExperience(evidence.BodyId, "delivered_and_clicked"));`
3. 改后原文＝`        // MUTATION A4 (temporary, wave34): registry.Add removed (readout only)`（只留读数不入册）
4. `git diff --stat`＝1 file changed, 1 insertion(+), 1 deletion(-)
5. 跑法整行＝同前（`--results-directory .../out/trx-A4`，不带 `--no-build`）
6. TRX＝`trx-A4/24782_PHYTHLINK_2026-10-28_20_11_39.trx`（1 枚）
7. 红格点名（1 枚）：`Pry.Core.Tests.StateAndLearningPrototypeTests.An_experience_candidate_is_recorded_only_when_delivery_and_a_click_are_both_present`（MSG：Assert.Single() Failure: The collection was empty；STACK 判据帧 `:215`）。读数 26/26/25/failed=1/openTag=1/OK（`out/a4-count.txt`）；其余四枚断 `Assert.Empty(Registry)` 的格全绿＝与事前论证"摘后仍为空"一致
8. 还原＝`node w34-mutate.js revert A4`
9. status＝**0 行**
10. SHA-256：突变态 `19ae3d47b25ee4ab2331ef09dd2b21bed90c7b067322bed6be1e3d2e8a432336`（＝manifest）；还原后六枚全 match=true；**判据 `317d2171…`＝blob ✓**
11. 结论＝**恰 1 枚清洁红** ⇒ 通过；定向预算 4/10。

### 三.A5 定向轮（P53 ⇒ A5）

预检 **P53**（`out/preflight-P53.txt`）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: Godot pid=42864 class=Engine style=0x16CF0000 rect=445,324,956x599 screen=2560x1440 covers=False borderless=False title=???? ? ???? (DEBUG)
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=14% used=2090MB
free_memory_gb=10
VERDICT=BUILD_OK
```

十一项：

1. 文件:行＝`DeliveryAttribution.cs:68-69`
2. 改前原文＝`        if (!evidence.HasExplicitClickOrFeedback)\n            return NotAdmitted(evidence.BodyId, "no_explicit_click_or_feedback");`
3. 改后原文＝`        // MUTATION A5 (temporary, wave34): gate 3 (missing click) removed`
4. `git diff --stat`＝1 file changed, 1 insertion(+), 2 deletions(-)
5. 跑法整行＝同前（`--results-directory .../out/trx-A5`，不带 `--no-build`）
6. TRX＝`trx-A5/24782_PHYTHLINK_2026-10-28_20_12_32.trx`（1 枚）
7. 红格点名（1 枚）：`Pry.Core.Tests.StateAndLearningPrototypeTests.A_delivered_body_without_an_explicit_click_is_not_recorded`（MSG：Assert.False Expected False/Actual True；STACK 判据帧 `:224`）。读数 26/26/25/failed=1/openTag=1/OK（`out/a5-count.txt`）
8. 还原＝`node w34-mutate.js revert A5`
9. status＝**0 行**
10. SHA-256：突变态 `9fc1559b59bdffd9bf5583b6676c5582b59a813e63f7a60f79016b5b313c2478`（＝manifest）；还原后六枚全 match=true；**判据 `317d2171…`＝blob ✓**
11. 结论＝**恰 1 枚清洁红** ⇒ 通过；定向预算 5/10。

### 三.A6 定向轮（P54 ⇒ A6）

预检 **P54**（`out/preflight-P54.txt`）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: Godot pid=42864 class=Engine style=0x16CF0000 rect=445,324,956x599 screen=2560x1440 covers=False borderless=False title=???? ? ???? (DEBUG)
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=23% used=2118MB
free_memory_gb=9.7
VERDICT=BUILD_OK
```

十一项：

1. 文件:行＝`DeliveryAttribution.cs:60`
2. 改前原文＝`        if (evidence.BodySuppressed || evidence.BodyDeduplicated || evidence.BodyRejected)`
3. 改后原文＝`        if (evidence.BodyDeduplicated || evidence.BodyRejected) // MUTATION A6 (temporary, wave34): first conjunct removed`（只摘第一支，留其余两支）
4. `git diff --stat`＝1 file changed, 1 insertion(+), 1 deletion(-)
5. 跑法整行＝同前（`--results-directory .../out/trx-A6`，不带 `--no-build`）
6. TRX＝`trx-A6/24782_PHYTHLINK_2026-10-28_20_12_52.trx`（1 枚）
7. 红格点名（1 枚）：`Pry.Core.Tests.StateAndLearningPrototypeTests.A_delivered_body_that_was_suppressed_deduplicated_or_rejected_is_not_attribution_evidence(suppressed: True, deduplicated: False, rejected: False)`（MSG：Assert.False Expected False/Actual True；STACK 判据帧 `:195`）。**合格形状自证**：同方法另两行 `(suppressed: False, deduplicated: True, rejected: False)` 与 `(suppressed: False, deduplicated: False, rejected: True)` 当场点名＝Passed（三行只红第一行＝摘的正是第一支）。读数 26/26/25/failed=1/openTag=1/OK（`out/a6-count.txt`）
8. 还原＝`node w34-mutate.js revert A6`
9. status＝**0 行**
10. SHA-256：突变态 `6aa9cf982cd227267a931422ee2ccb85c23b1dec7d3432d3df763fd01adc461c`（＝manifest）；还原后六枚全 match=true；**判据 `317d2171…`＝blob ✓**
11. 结论＝**恰 1 枚清洁红＋另两行绿** ⇒ 通过；定向预算 6/10。

### 三.A7 定向轮（P55 ⇒ A7）

预检 **P55**（`out/preflight-P55.txt`）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: Godot pid=42864 class=Engine style=0x16CF0000 rect=445,324,956x599 screen=2560x1440 covers=False borderless=False title=???? ? ???? (DEBUG)
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=14% used=2092MB
free_memory_gb=9.6
VERDICT=BUILD_OK
```

十一项：

1. 文件:行＝`LegacyInheritance.cs:50-53`
2. 改前原文＝`        if (input.Source is null\n            || string.IsNullOrWhiteSpace(input.Source.SourceId)\n            || input.Source.SourceVersion is null)\n            return new InheritanceReadout(input.StateKey, "Failed", "inherited_source_missing");`
3. 改后原文＝`        // MUTATION A7 (temporary, wave34): missing-source gate removed`（整道闸摘除）
4. `git diff --stat`＝`src/.../LegacyInheritance.cs | 5 +----`，1 file changed, 1 insertion(+), 4 deletions(-)
5. 跑法整行＝同前（`--results-directory .../out/trx-A7`，不带 `--no-build`）
6. TRX＝`trx-A7/24782_PHYTHLINK_2026-10-28_20_13_18.trx`（1 枚）
7. 红格点名（2 枚，逐字符）：`Pry.Core.Tests.StateAndLearningPrototypeTests.Inherited_state_without_a_pointable_source_fails_loudly_and_is_never_silently_dropped`＋`Pry.Core.Tests.StateAndLearningPrototypeTests.Every_inherited_input_leaves_a_named_readout_rather_than_silently_vanishing`（两枚 MSG 均 `System.NullReferenceException`，产品帧 `LegacyInheritance.cs:line 52`＝摘闸后 null Source 直落 `input.Source.SourceId` 当场炸，符合事前论证"缺来源即失败换成当场抛"的形状）。第三枚带来源格 `A_successful_inheritance_carries_its_source_reference_and_version` 保持绿。读数 26/26/24/failed=2/openTag=2/OK（`out/a7-count.txt`）
8. 还原＝`node w34-mutate.js revert A7`
9. status＝**0 行**
10. SHA-256：突变态 `bab94c49f5d2927736ca21291aea90806f2f397764f1c95e9a0f0573cebb3bc0`（＝manifest）；还原后六枚全 match=true；**判据 `317d2171…`＝blob ✓**
11. 结论＝**恰 2 枚清洁红（事前论证的恰 2 形状），格名逐字符相符** ⇒ 通过；定向预算 7/10。

### 三.A8 定向轮（P56 ⇒ A8）

预检 **P56**（`out/preflight-P56.txt`）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: Godot pid=42864 class=Engine style=0x16CF0000 rect=445,324,956x599 screen=2560x1440 covers=False borderless=False title=???? ? ???? (DEBUG)
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=14% used=2089MB
free_memory_gb=10.1
VERDICT=BUILD_OK
```

十一项：

1. 文件:行＝`ExperienceMaterial.cs:56`
2. 改前原文＝`        new(bodyId, false, reason, null, false);`（`NotOffered` 工厂）
3. 改后原文＝`        new(bodyId, false, reason, null, true); // MUTATION A8 (temporary, wave34): CarriesSpeakingAuthority flipped`（只翻第 5 枚实参，第 2 枚 `Available` 未动）
4. `git diff --stat`＝1 file changed, 1 insertion(+), 1 deletion(-)
5. 跑法整行＝同前（`--results-directory .../out/trx-A8`，不带 `--no-build`）
6. TRX＝`trx-A8/24782_PHYTHLINK_2026-10-28_20_13_41.trx`（1 枚）
7. 红格点名（1 枚）：`Pry.Core.Tests.StateAndLearningPrototypeTests.Experience_never_carries_authority_over_whether_to_speak`（MSG：Assert.False Expected False/Actual True；STACK 判据帧 `:302`＝正是对被拒读数 `notStored` 断 `CarriesSpeakingAuthority` 的那一行）。窗口内/缺有效期/超期三格不读被翻支保持绿（offered 走 `:52` 未动）。读数 26/26/25/failed=1/openTag=1/OK（`out/a8-count.txt`）
8. 还原＝`node w34-mutate.js revert A8`
9. status＝**0 行**
10. SHA-256：突变态 `54b8cad62e4d546bd25b288a839bdf54c35b225c36c88742b480eae3c3d74eaf`（＝manifest）；还原后六枚全 match=true；**判据 `317d2171…`＝blob ✓**
11. 结论＝**恰 1 枚清洁红** ⇒ 通过；定向预算 8/10。**A 类八轮至此全部命中事前论证，零作废轮。**

### 三.B1 定向轮（P57 ⇒ B1，反面探针·单列登记·不签第二换手）

预检 **P57**（`out/preflight-P57.txt`）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: ChatGPT pid=16348 class=Chrome_WidgetWin_1 style=0x14C70000 rect=413,296,1374x811 screen=2560x1440 covers=False borderless=False title=ChatGPT
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=13% used=2357MB
free_memory_gb=9.3
VERDICT=BUILD_OK
```

十一项：

1. 文件:行＝`StateAndLearningPrototypeTests.cs:419`（`LegacyChainNamesFoundIn` 扫描函数）
2. 改前原文＝`        LegacyChainNames.Where(name => source.Contains(name, StringComparison.Ordinal));`
3. 改后原文＝`        LegacyChainNames.Where(name => false); // MUTATION B1 (temporary, wave34): scan neutered`（恒空；探针那句 `Assert.Equal` 未改）
4. `git diff --stat`＝`tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs | 2 +-`，1 file changed, 1 insertion(+), 1 deletion(-)
5. 跑法整行＝同前（`--results-directory .../out/trx-B1`，不带 `--no-build`）
6. TRX＝`trx-B1/24782_PHYTHLINK_2026-10-28_20_14_04.trx`（1 枚）
7. 红格点名（1 枚）：`Pry.Core.Tests.StateAndLearningPrototypeTests.State_and_learning_prototype_sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites`（MSG：Expected "BehaviorPlanner,ReplyGenerationRenderer,CharacterS"···/Actual ""；STACK 判据帧 `:320`＝探针那句 `Assert.Equal` 当场红，前半五枚产品文件 `Assert.Empty` 保持通过——这正是反面探针要证的"门在咬"，不是常绿门）。读数 26/26/25/failed=1/openTag=1/OK（`out/b1-count.txt`）
8. 还原＝`node w34-mutate.js revert B1`（写回 `51f149d` blob）
9. status＝**0 行**
10. SHA-256：突变态判据文件＝`57b2cea8f378066b5b6e44dc2af3eaa68dec4f9cf02ab319be529063c9891336`（＝manifest）；还原后＝`317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc`＝blob ✓；其余五枚产品文件全程未动 match=true
11. 结论＝**恰 1 枚清洁红** ⇒ 该枚扫描格结清**红证**；因本轮动过判据自身字节，**不签第二换手**（波 31 立的口径，照工单 §三 单列登记）。定向预算 9/10。

### 三.B2 定向轮（P58 ⇒ B2，反面探针·单列登记·不签第二换手）

预检 **P58**（`out/preflight-P58.txt`）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: ChatGPT pid=16348 class=Chrome_WidgetWin_1 style=0x14C70000 rect=413,296,1374x811 screen=2560x1440 covers=False borderless=False title=ChatGPT
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=18% used=2266MB
free_memory_gb=9.9
VERDICT=BUILD_OK
```

十一项：

1. 文件:行＝`StateAndLearningPrototypeTests.cs:423-424`（`SourceSideDefaultsFoundIn` 两条 `if` 的条件）
2. 改前原文＝`        if (source.Contains("const ", StringComparison.Ordinal)) yield return "const";\n        if (source.Contains("static readonly", StringComparison.Ordinal)) yield return "static readonly";`
3. 改后原文＝两条 `Contains` 样本串各换成必然不存在的 token（`token-cannot-exist-const `／`token-cannot-exist-static readonly`），`Assert.Equal("const,static readonly", …)` 探针句未改
4. `git diff --stat`＝`tests/.../StateAndLearningPrototypeTests.cs | 4 ++--`，1 file changed, 2 insertions(+), 2 deletions(-)
5. 跑法整行＝同前（`--results-directory .../out/trx-B2`，不带 `--no-build`）
6. TRX＝`trx-B2/24782_PHYTHLINK_2026-10-28_20_14_32.trx`（1 枚）
7. 红格点名（1 枚）：`Pry.Core.Tests.StateAndLearningPrototypeTests.State_and_learning_prototype_declares_no_source_side_numeric_default`（MSG：Expected "const,static readonly"/Actual ""；STACK 判据帧 `:344`＝探针句当场红；前半五枚产品文件 `Assert.Empty` 保持通过）。读数 26/26/25/failed=1/openTag=1/OK（`out/b2-count.txt`）
8. 还原＝`node w34-mutate.js revert B2`
9. status＝**0 行**
10. SHA-256：突变态判据文件＝`f9ee7460ff7fa5f2d7846facc6dac85a67a041da7724bb4ba99b478624854ee7`（＝manifest）；还原后＝`317d2171…`＝blob ✓；五枚产品文件全程未动
11. 结论＝**恰 1 枚清洁红** ⇒ 该枚扫描格结清**红证**；动过判据自身字节⇒**不签第二换手**。定向预算 10/10 用满。

**十轮（A1→A8→B1→B2）至此全部跑完：每轮实测形状（枚数＋格名逐字符）与 §三.0 事前论证相符，零轮作废。** 之后 `revert` 全部突变并 `verify`：六枚文件工作副本 SHA-256 逐字符等于 `51f149d` blob（`af727a9c…`／`000f2fc8…`／`5858a75d…`／`289fc89b…`／`6a1f5099…`／判据 `317d2171…`），载体 `rev-parse HEAD`＝`51f149d…`（本刀零新提交）、`status --short`＝0 行。

### 三.F 收尾全量轮（P59 ⇒ F，不带 `--no-build`）

预检 **P59**（`out/preflight-P59.txt`）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: ChatGPT pid=16348 class=Chrome_WidgetWin_1 style=0x14C70000 rect=413,296,1374x811 screen=2560x1440 covers=False borderless=False title=ChatGPT
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=28% used=2263MB
free_memory_gb=9.6
VERDICT=BUILD_OK
```

十一项（对齐工单 §一.6 的字段含义，本轮无突变故 ①②③ 记还原态）：

1. 轮：F（解决方案级全量，全部还原＋核 SHA 之后跑，自行增量重建——源码还原当轮即重建，不读突变态 IL，工单 §一.4 口径）
2. 前置：P59 BUILD_OK；跑前六枚文件 match=true（见上段 verify 输出）
3. 命令整行：`dotnet test Pry.slnx -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave34-1028/out/trx-F" --logger trx`（**不带 `--no-build`**）
4. 控制台原文（四工程）：Client `已通过! - 失败: 0，通过: 108，总计: 108`；App `失败: 0，通过: 233，总计: 233`；Api `失败: 0，通过: 280，总计: 280`；Core `失败: 0，通过: 783，已跳过: 2，总计: 785`
5. TRX 份数：`ls *.trx | wc -l`＝**4**（先数满再读 `<Counters>`；`20_14_50`／`20_14_51`／`20_14_51[1]`／`20_14_52`）
6. 仪器读数（`out/f-count.txt`）：Client 108/108/108/0/0 OK；Api 280/280/280/0/0 OK；Core 785/783/783/0/0 OK；App 233/233/233/0/0 OK；**SUM＝files=4 total=1406 executed=1404 passed=1404 countersFailedSum=0 openTagFailedSum=0 reconcileAll=OK**
7. 红格点名：0 枚
8. 跳过点名（`outcome="NotExecuted"` 逐格点名，2 条）：`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`／`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`（与在册基线同名同枚）
9. 出口对账：收尾读数**仍＝1406／executed 1404／failed 0／跳过 2**，与起点（上一把 §七.2 的守门人核过值）同值——本刀新格数 0，等式照工单 §〇.4 成立；**本刀不顶账，1406 不因这一把移动**。这是交付读数唯一一次解决方案级全量（预算 1/1 用满）
10. 还原后 SHA：本轮跑前六枚＝blob 值（见上）；本轮跑后载体 `status --short` 仍 **0 行**
11. 结论＝全绿同值复采。`scripts/measure-fixed-eval.ps1` 本刀未跑（工单未要求，波 33 先例＝由守门人自决；本刀零新格不贡献其覆盖面）。

`dotnet` 总调用＝11 次（定向 10＋全量 1）≤12；解决方案级构建 0/1（§〇.6 判定合法跳过）；焦点预检 P49–P59 共 11 枚、全 `VERDICT=BUILD_OK`、`HOLD` 0 枚、重跑额度 0/8 未动。

## 四 结不到红的登记

本刀 19 枚无红格里：**A 类结清 10 枚**（A1 一枚突变结清 2 枚＝`A_narrative_candidate_may_not_write_intimacy_state`＋`A_candidate_touching_all_three_surfaces_is_caught_by_the_first_gate`；A2–A6、A8 各 1 枚；A7 一枚突变结清 2 枚＝`Inherited_state_without_a_pointable_source_fails_loudly_and_is_never_silently_dropped`＋`Every_inherited_input_leaves_a_named_readout_rather_than_silently_vanishing`），**B 类结清 2 枚的红证**（`State_and_learning_prototype_sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites`／`State_and_learning_prototype_declares_no_source_side_numeric_default`，反面探针类，单列登记不签换手），**仍余 7 枚本刀拿不到红**。这 7 枚不写"够不着"——守门人已在工单 §四 逐枚点名下一把的方向（含 `ExperienceMaterial.cs:49` 比较方向翻转＝**恰 3 枚**形状的事前枚数警告），本刀额度（定向 10/10）已排满、未顺手跑它们；换手账另见那表第 8 行（B 类两枚的第二换手需另一双手跑 G0 形状定向绿轮，本刀跑序 A→B 不签）。

7 枚逐格（格名逐字符，出处＝工单 §四 表，本刀实测确认未响过）：`An_author_side_canon_revision_with_a_pointable_source_is_applied`／`An_overlay_with_source_and_version_is_recorded_and_leaves_the_canon_layer_untouched`／`A_delivered_body_that_was_suppressed_deduplicated_or_rejected_is_not_attribution_evidence(suppressed: False, deduplicated: True, rejected: False)`／`…(suppressed: False, deduplicated: False, rejected: True)`／`Experience_within_the_input_validity_window_is_offered_as_optional_material`／`Experience_past_the_input_validity_window_is_not_offered`／`A_successful_inheritance_carries_its_source_reference_and_version`。

## 五 未验证面（逐条点名，不用"已完成"混过去）

真实模型对照（零读数，需作者一句"可以跑"）；**生产接线**（这 26 枚至今零生产调用方；要接的是 005 落库面与 004 生命周期，一律没接；接线时机＝U-9 残句「切换那一步她要不要做什么」仍未拍）；旧数据真实继承（零读取）；UI 可见行为（零读数）；生产库与 `%LOCALAPPDATA%\PryCompanion`（零接触）；待标定参数权威值（关系/心情各档阈值、经验有效期、点击归因窗口全部未定＝**特性不是缺陷**）；并发与断电（零读数）；旧链删除片与"构建时不把旧链打进包"那道门（**未开始**）；**换手终账不由本 lane 签**——本 lane 只交凭据：A 类八轮每一轮 ⑩ 项都给了判据文件 SHA-256 且逐轮等于 `51f149d` blob `317d2171…`（跑 A 类的手不是第三十三波判据作者、对判据零写入）；B 类两枚动过判据字节、只结红证。三句话照旧不写＝新链已接线／旧链可删／不进包已做到；上一把那句照旧不写＝她的状态已经迁到新链。

## 六 盘上自校

- 盘上 `.trx` 枚数＝**14**＝登记轮数对账：定向 10 轮×1（trx-A1…trx-A8、trx-B1、trx-B2）＋收尾全量 1 轮×4（trx-F）＝14 ✓（`find out -name "*.trx"` 逐枚点名见上轮输出）。
- **作废轮＝0**：每轮实测枚数与格名都与 §三.0 事前论证逐字符相符（A1/A7 恰 2、其余恰 1、A6 另两行绿），没有任何轮需要走 `trx-<轮>-voided-<预检号>` 改名留档程序；`ls out | grep voided`＝无。
- 结果目录 11 个互不复用；预检台账 P49–P59 共 11 枚归档 `out/preflight-*.txt`；计数存档 `out/{a1..a8,b1,b2,f}-count.txt`＋`out/instrument-selfproof.txt`＋`out/mutation-manifest.txt`／`mutation-prep-sha.txt`；仪器与突变脚本＝`w34-count.js`／`w34-mutate.js`（本 lane 自写，不复用 wave33 实现）。
- `dotnet` 盘上凭证＝11 次（10 定向＋1 全量），预算 ≤12 ✓；解决方案级构建 0 次（§〇.6 跳过判定）；全量交付读数只来自 1 次（§一.2 语义）。

## 七 交回守门人核的六件事（凭据位置，判定归守门人）

1. **红格归属**：每轮红逐格点名到各自 TRX 的 `outcome="Failed"` 开标签节点（§三 各轮 ⑦ 项＋`out/*-count.txt`）；A1/A7＝事前论证的恰 2，其余恰 1；A6 形状含"另两行 Passed"的逐行点名。
2. **载体终态**：`rev-parse HEAD`＝`51f149d0b722e28a6566c93d19f48a94ca4c85df`、`status --short`＝**0 行**、`diff --stat fd93ebe 51f149d -- <产品路径>`＝六枚 +822/−0；六枚终态 SHA-256 逐字符等于入场基准（五枚产品 `af727a9c/000f2fc8/5858a75d/289fc89b/6a1f5099`＋判据 `317d2171`）——`w34-mutate.js verify` 终跑输出在案。
3. **收尾全量**：4 份 TRX 先数满再求和＝1406／executed 1404／failed 0／跳过 2（分项 108/785/280/233），两条同名跳过按 `NotExecuted` 点名；1406 未被本刀移动、未为顶账另起构建。
4. **作废轮登记**：0 枚作废；盘上 14 枚 `.trx` 与 11 轮（10 定向＋1 全量）一一对应（§六）。
5. **A 类判据字节**：八轮每一轮 ⑩ 项都记了判据文件 SHA-256 且等于 `51f149d` blob（`317d2171…`）；B 类两轮的突变态判据 SHA（`57b2cea8…`／`f9ee7460…`）也逐轮在册，供单列署名。
6. **零新增**：三计数 23／1／3 入场与终态两侧当场 `grep -c` 各数一次逐位相同（§二＋§六前的终态复测）；无新增格、无断言/期望值改动、无 `Assert.Skip`、无注释断言、产品字节终态净写入 0、数字门槛新增 0。

（本刀归位是空转：零新字节⇒无代码刀；主树唯一新文件＝本报告，未 `git add`；他人两枚 `M ` 暂存项与全部 `??` 未动——主树 `status --short --branch` 终态只多了本报告一枚 `??`。）

## 八 交接（本 lane 补记）

- `docs/plans/PROJECT_PLAN.md` 的状态登记（§4.2 HDS-009 行与 §9 收口 entry）不在本刀文件面内，交守门人处理；本报告只写 §〇–§七＋本节。
- 下一把素材＝工单 §四 那 7 枚方向＋B 类两枚扫描格的第二换手（G0 形状定向绿轮），本刀未顺手跑。

## 九 守门人划账（本节由守门人追加；§〇–§八 一字未改，追加前该文件 443 行）

### 九.0 复采仪器与它的自证（口径⓪：先证明仪器读得对，再采信它的读数）

守门人不复用 lane 的 `w34-count.js`，另写一份三面仪器＝`.local-notes/wave34-1028/gk34-audit.js`，输出存档＝`out/gk34-audit.txt`。三面＝① 逐份读 `<Counters>`；② 逐 occurrence 扫 `<UnitTestResult` 开标签并按 `outcome="Failed"` 计数（自闭合 `…/>` 单独判，不用开—闭配对正则）；③ **每个红格名回判据源码按方法名真实存在再验一遍**（`StateAndLearningPrototypeTests.cs` 里 `方法名(` 串命中），这一面是本轮新加的尺——它把"红名是 TRX 里的字符串"升级成"红名对应盘上一枚真实存在的判据"。

起跑前先拿盘上已知读数自证（四份，全部来自上一把的载体证据）：波 33 `trx-M1` 恰 1 枚（红点 `:47`）、`trx-M5` 恰 1 枚（`:244`）、`trx-M7` 恰 1 枚（`:46`）、波 33 `trx-F` 四份且 failed 全 0——四面与报告在册值逐字符相符 ⇒ 允许复采。复采结果＝**21 份 TRX 逐份 reconcile=OK、MISMATCH 0 处、nameInCriteria=false 0 处**；十五枚定向轮红格（含 A1/A7 各 2 枚）的红名、红点行号、消息首段与 §三 各轮 ⑦ 项逐字符相符。**结论＝lane 的凭据成立，本节的账以守门人自己的读数为署。**

### 九.1 六件事裁决

1. **红格归属＝通过。** 十轮十份定向 TRX 每份 `total=26 / executed=26 / failed` 与开标签同值：A1＝2 枚（`A_candidate_touching_all_three_surfaces_is_caught_by_the_first_gate` `:167`、消息是拒因串不符 Expected `intimacy_state_is_not_narrator_writable`；`A_narrative_candidate_may_not_write_intimacy_state` `:131` `Assert.False`）、A2/A3/A4/A5/A6/A8/B1/B2 各 1 枚、A7＝2 枚（两格都 `:52`）——A1 与 A7 命中事前论证的"恰 2"，其余命中"恰 1"，A6 只响 `(suppressed: True, deduplicated: False, rejected: False)` 那一枚实例。零轮需要走作废改名程序。
2. **载体终态＝通过。** `rev-parse HEAD`＝`51f149d0b722e28a6566c93d19f48a94ca4c85df`（本刀零新提交）、`status --short`＝**0 行**、`fd93ebe..HEAD` 恰两枚提交且 `diff --stat`＝六枚 +822/−0；六枚工作副本 SHA-256 前缀逐字符等于入场基准（`af727a9c0435`／`000f2fc8bfd5`／`5858a75da3ed`／`289fc89be087`／`6a1f50999e7c`／判据 `317d2171264f`）。主树侧 `git diff --stat 51f149d HEAD -- src tests scripts Pry.slnx Directory.Build.props`＝**0 行**，故这十四枚红格跑的那棵树与在册 1406 那棵树逐字节相同。
3. **收尾全量＝同值复采。** 先数满 4 份再求和＝**1406／executed 1404／passed 1404／failed 0／跳过 2**（Client 108／Api 280／Core 785、executed 783／App 233），两条 `NotExecuted` 与波 28–33 同名同枚。**1406 不因这一把移动**：本刀净新增格数 0、零落地、零归位、零顶账。
4. **作废轮＝0，盘上对得上。** 盘上 `.trx` 14 枚＝定向 10×1＋全量 1×4，与 11 轮一一对应；`ls out | grep voided`＝0。焦点预检 P49–P59 共 11 枚全 `VERDICT=BUILD_OK`、HOLD 0 枚、按刀计的 8 枚重跑额度 0/8 未动用；`dotnet` 11 次（定向 10＋全量 1）≤12、解决方案级构建 0/1（§〇.6 的跳过判定成立：载体 `Pry.Core.dll` 时间戳 19:33 早于本刀入场且六枚 SHA 未变、`status` 0 行）。
5. **判据字节＝A 类八轮全部有资格，B 类两枚单列。** `out/mutation-manifest.txt` 逐轮登记 A1–A8 每轮⑩项判据文件 SHA-256 都等于 `51f149d` blob `317d2171…`（＝零写入），且判据在主树的 last-touch＝`e04e7e6`（守门人的按路径落地提交）＋载体侧 `51f149d`（波 33 的判据修订）——**取红这只手既不是判据字节的作者、也没在这一把写过判据一个字节**，签账资格成立。B1/B2 两轮的突变对象就是判据文件自身（扫描函数与它的探针），当轮判据 SHA 变成 `57b2cea8…`／`f9ee7460…`，因此只结"门在咬"这一半，**第二换手不签**（波 31 立的口径，工单 §三 已预告）。
6. **零新增＝成立。** 三面各自当场数：`[Fact]` 23／`[Theory]` 1／`[InlineData` 3＝26 枚格，入场与终态两侧同值；无新增格、无断言或期望值改动、无 `Assert.Skip`、无注释掉的断言、**新增数字门槛 0**、产品字节净写入 0。工单 §一的六条硬约束逐条满足。

### 九.2 划账（红证账与换手账分开结）

- **A 类结清的 10 枚＝红证＋换手双结。** 这一把的手不是判据作者、对判据零写入，十枚格各自在"改坏产品码"下响过并且红名回判据源码对得上——两样同时结清。
- **B 类 2 枚＝只结红证。** `…_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites`／`State_and_learning_prototype_declares_no_source_side_numeric_default` 的反面探针证明确实"门在咬"，但那只手当轮动过判据字节 ⇒ 换手仍欠，解法＝另一双手跑一次 G0 形状的定向绿轮（不改判据一个字节）。
- **7 枚＝仍欠红证**（工单 §四 已逐枚点名方向，本刀额度排满未顺手跑）。
- **波 33 那 7 枚 M 系格＝欠的是"另一双手取红"，不是欠换手形状。** 这一点必须写清：M1–M7 的红当年由**判据作者自己那只手**取到（波 33），`51f149d` 那次判据修订又让 M7 那枚连那次都不算；本刀的 11 轮只给了这 7 枚"另一双手跑绿"，**从没被另一双手逼红过一次**。按波 24/27/28 那条资格判据（取红那只手不是判据作者），它们的换手仍未结。
- **合计：完整换手仍欠 16 枚＝7（M 系欠另一双手的红）＋2（B 类扫描格）＋7（无红）。** 旧欠 75 格（波 13 余 47＋波 20 余 28）与本刀无交集、照旧不减；拆法原文照旧 46／45／1＝W2。
- **就地更正一句（原文不涂）：** 工单 `docs/dispatch-hds-wave34-1028.md` §六 那句「其余 14 枚仍欠」写得有歧义——它把"红证欠 7"与"换手欠"混在同一格里数。按上面的四面拆法终账＝**16 枚欠完整换手**，账目以本节为准；工单原文一字不改，这条歧义记在守门人自己的账上（波 34 的派发者＝守门人）。

### 九.3 A7 那两枚红的机制披露（不销账，加一条子账）

A7 的突变让两枚格同时红，但守门人读了消息首段：两格都是 `System.NullReferenceException`、红点都落在 `LegacyInheritance.cs:52`（产品侧解引用），**不是**判据里那句拒因字面量（`inherited_source_missing`）被打断。⇒ 这两枚格的"整格响过"凭证照记，但**其中"来源缺失要留下可指认的拒因文本"这一断言面从没被单独逼响**，登记为**断言级子账**。方向（交下一把，身份＝文本面突变、不冒充行为突变）：把产品侧那枚 `"inherited_source_missing"` 字面量改成另一串 ⇒ 预期**恰 1 枚**红（`Inherited_state_without_a_pointable_source_fails_loudly_and_is_never_silently_dropped`），跑法与本轮同（定向、不带 `--no-build`）。同族先例＝波 32 立的 `BoundedAdvancementPrototypeTests.cs:195`／`:172` 两条断言级子账，现累计三条。

### 九.4 未验证面（§五 那十条一条不减，逐条照旧）

真实模型对照（零读数，需作者一句「可以跑」）；**生产接线**（这 26 枚至今零生产调用方，要接的是 005 落库面与 004 生命周期，一律没接，接线时机押在 U-9 残句「切换那一步她要不要做什么」＝作者的话，我不拍也不催）；旧数据真实继承（零读取）；UI 可见行为（零读数）；生产库与 `%LOCALAPPDATA%\PryCompanion`（零接触）；待标定参数（关系/心情各档阈值、经验有效期、点击归因窗口全部未定＝**特性不是缺陷**）；并发与断电（零读数）；旧链删除片与"构建时不把旧链打进包"那道门（**未开始**）；`PrototypeFiles` 写死清单的扫描盲区（新增第六枚产品文件不会自动进扫描面，押接线刀）。**四句话照旧不许写**＝新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链。本刀全部凭据只证一件事：这些格在改坏产品码之下会响。

### 九.5 出闸＝第三十五波

`docs/dispatch-hds-wave35-1028.md`：同一棵载体（基点 `fd93ebe`、线尾 `51f149d`，不新建 worktree、不新建产品文件、不改产品与判据字节、净新增格数 0、零落地零顶账）。跑序与额度＝**A 类 7 轮**（工单 §四 那 7 枚无红方向逐枚单放，含 `ExperienceMaterial.cs:49` 那枚比较方向翻转＝**恰 3 响**，必须事前点名三枚格名）＋**B 类 7 轮**（波 33 的 M1–M7 配方由非判据作者的手重取＝那 7 枚换手第一次结）＋**C 类 1 轮定向绿**（G0 形状、零字节写入＝B 类两枚扫描格的第二换手）＋**F 1 轮解决方案级全量**；预检编号从 **P60** 起、`dotnet` ≤16、HOLD 重跑 ≤8 枚按刀计、全量交付读数只来自那 1 次（波 33 的教训照抄：突变轮与收尾轮一律**不带 `--no-build`**）；仪器自证仍按九.0 的四面（已知读数取波 33 的 `trx-M1/M5/M7/F`，另加波 34 的 `trx-A1` 恰 2／`trx-A6` 恰 1／`trx-B2` 恰 1），任一不符先修仪器不起跑。报告＝`docs/eval/reports/2026-10-28-hds009-wave35-red-proof-handover.md`。
