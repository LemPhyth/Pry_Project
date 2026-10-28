# HDS-009 第一片·第三刀（续跑证据刀）报告：给波 34 剩下那 7 枚无红格逐枚取红，并用"往被扫产品文件注入样本"这条路结清两枚扫描格的第二换手（HDS-WAVE35-1028）

- 工单：`docs/dispatch-hds-wave35-1028.md`（唯一权威工单）
- 执行方：冷启动 lane（载体＝沿用第三十三波那棵，未新建 worktree）
- 性质：证据刀·续跑（零新增判据格、零产品字节终态、十轮零判据字节写入、零落地、零归位、零顶账）

> 状态词、结案词、在册基线判定、划账由守门人写，本 lane 不代写、不自封。四句话照旧不写＝新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链。

## 〇 开工前必读与入场

### 〇.1 开工前必读（八份，全部读完整页；本节是"读了什么"的登记，不是引用其数字）

1. `docs/plans/README.md`（现行工作规则权威副本 14 条，含第 9 条提交前命令、第 14 条以工程规范为准）。
2. `docs/plans/PROJECT_PLAN.md`：§4.2 的 HDS-009 行（第 82 行）＋§9 最新一条 entry（第 186 行起「收第三十四波」那条，含守门人替本刀复算的 §三.0 依据与"波 33 那 7 枚 M 系押第三十六波"的明令）。
3. `docs/agent-engineering-standards.md`（全文；交付形态按 §10 四项：问题与根因／实际改动／验证结果／剩余问题）。
4. `docs/dispatch-hds-wave34-1028.md`（§三 那张表＝本刀 A 类格式样板；§四 那张表＝本刀 A1–A8 方向出处）。
5. `docs/eval/reports/2026-10-28-hds009-wave34-red-proof-continuation.md`（§三 各轮十一项写法＋守门人 §九.0 仪器口径／§九.1 六件事／§九.2 划账／§九.3 断言级子账——A8 那条子账的出处就是这里）。
6. `docs/eval/reports/2026-10-28-hds009-state-and-learning-prototype.md` §三.0（波 33 的 M1–M7 事前论证表；**本刀不重取那七枚配方**，押第三十六波，见 §四）。
7. `src/Pry.Core/Narrative/StateAndLearning/` 五枚产品文件（逐行读：`StateLayering.cs` 102 行／`NarrativeAuthority.cs` 46 行／`DeliveryAttribution.cs` 94 行／`ExperienceMaterial.cs` 57 行／`LegacyInheritance.cs` 78 行）。
8. `tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs`（判据本体 445 行、26 枚格；**本刀对它零字节写入**）。

### 〇.2 入场读数（当场取，未引用工单数字）

- `date`：`Wed Oct 28 20:43:57     2026`
- `git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD`：`a955f227d31d816286d9469c178659ba18424d14`
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
  两枚 `M ` 暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）与全部 `??` ＝他人资产：本刀不读内容、不改、不提交、不 `stash`／`clean`／`rm`。` M docs/plans/PROJECT_PLAN.md` 是在册总计划的在途登记，不属本刀文件面。
- `git worktree list`（与本刀有关的两行；其余为他人 worktree，未动）：主树 `C:/Users/24782/Desktop/Pry_Project  a955f22 [codex/ui-main-window-redesign]`；载体 `C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave33-1028/carrier  51f149d (detached HEAD)`＝本刀沿用，**未新建 worktree**。
- `git log --oneline -3`（主树）：`a955f22`（第三十四波收口文档刀）→ `9a15e2a`（第三十三波收口文档刀）→ `e04e7e6`（第三十三波代码刀归位）。线尾被上一把收口刀从 `9a15e2a` 挪到 `a955f22`，故 §〇.3 右端全部当场现取。

### 〇.3 载体入场自证（右端一律当场 `git rev-parse HEAD` 取，不预写 SHA）

- `git -C <carrier> rev-parse HEAD` = `51f149d0b722e28a6566c93d19f48a94ca4c85df` ✓（等于工单指定线尾）
- `git -C <carrier> status --short` = **0 行** ✓
- `git -C <carrier> diff --stat fd93ebe $(git -C <carrier> rev-parse HEAD) -- src tests scripts Pry.slnx Directory.Build.props` = **只有波 33 那六枚新文件、+822/−0** ✓（`DeliveryAttribution.cs` 94／`ExperienceMaterial.cs` 57／`LegacyInheritance.cs` 78／`NarrativeAuthority.cs` 46／`StateLayering.cs` 102／`StateAndLearningPrototypeTests.cs` 445）
- `git -C <carrier> log --oneline fd93ebe..HEAD` = **2 行**（`1e02205` 落六枚新文件＋`51f149d` 一处判据整形），本刀结束时仍应是这 2 行（本刀零提交）。
- 本刀专属·主树侧 0 行式子：`git -C <main> diff --stat 51f149d $(git -C <main> rev-parse HEAD = a955f22…) -- src tests scripts Pry.slnx Directory.Build.props` = **0 行**（当场跑，输出为空，exit=0）✓ ⇒ 载体树与主线归位后的产品／判据字节逐字符相同，本刀在载体里取到的红就是主线那棵树的红。
- EOL 形状复核（决定"按字节还原"能否成立）：`.gitattributes` 有 `*.cs text eol=lf`；载体五枚产品文件 `grep -c '\r'` 全 = 0 ⇒ 工作副本字节＝blob 字节，还原写成 blob 原字节即等价于 checkout 形状。

### 〇.4 六枚文件入场 SHA-256 基准（当场 `sha256sum` 取，三侧同值；不从工单抄）

`git show 51f149d:<path> | sha256sum`（载体 blob）＝载体工作副本＝主树工作副本，三侧逐字符相同：

| 文件（相对路径） | SHA-256（三侧同值） |
|---|---|
| `src/Pry.Core/Narrative/StateAndLearning/StateLayering.cs` | `af727a9c04355d853f756a8cd6d22e374ac889042c847f99906784e0bb20ca3a` |
| `src/Pry.Core/Narrative/StateAndLearning/NarrativeAuthority.cs` | `000f2fc8bfd5d73ba7863029c8acafd7aa9df92ef11c910ccda3a28db3f13c97` |
| `src/Pry.Core/Narrative/StateAndLearning/DeliveryAttribution.cs` | `5858a75da3ed70e35f93782e1d760c87eea3feeecdafcdd645b27f69ea66f0ab` |
| `src/Pry.Core/Narrative/StateAndLearning/ExperienceMaterial.cs` | `289fc89be087fa35bace12b33805060562a210764a9774102b5a2875fcd55169` |
| `src/Pry.Core/Narrative/StateAndLearning/LegacyInheritance.cs` | `6a1f50999e7caf1d6d29cc07bbe752f89ea17851999013c00b167e590f3498ab` |
| 判据 `tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs` | `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc` |

判据文件全程零字节写入＝本刀十轮能签第二换手的唯一凭据（每轮 ⑩ 项复核对齐 `317d2171…`）。判据字节的 last-touch 在场：主树侧 `git log -1 -- <判据>`＝`e04e7e65676551f9a3b85f32f0b4053b614d984c`（守门人按路径落地提交）、载体侧 `51f149d…`（波 33 的判据修订）——**两者都不是本刀这只手**。

### 〇.5 在册基线起点（当场从上一把报告读到的原句，不做加法）

- 波 34 报告守门人 §九.1 第 3 项原文：「收尾全量**同值复采** 1406／executed 1404／failed 0／跳过 2，分项 108／785／280／233，两条同名跳过按 `NotExecuted` 点名同值」；同一把尺的上一处登记＝波 33 报告 §七.2（在册基线由波 33 那把从 1380 顶到 1406）。本刀 §〇.5 起点＝**1406／executed 1404／failed 0／跳过 2**（分项 Client 108／App 233／Api 280／Core 785、Core executed 783），另加盘上第三面自证：本刀仪器直读波 33 `out/trx-F` 那四份原始 TRX 求和＝total 1406／executed 1404／passed 1404／countersFailed 0／`NotExecuted` 开标签 2 条（见 §〇.6 那行的实测）。
- 本刀**新格数＝0**（工单 §二 明令），所以出口读数应当仍是 1406；收尾那轮若读到别的数，先怀疑树没还原干净，不改判据、不改基线，照实登记并作废那一轮。

### 〇.6 取数仪器与它的自证（口径⓪：先证明仪器读得对，再采信它的读数）

仪器＝本 lane 自写 `.local-notes/wave35-1028/w35-count.py`（**不复用** wave33 的 `w33-count.js`／`gk33-count.js`，也不复用 wave34 的 `w34-count.js`／`gk34-audit.js`；实现语言与切法都不同：Python＋逐 occurrence 引号感知找标签末）。三面＝① 逐份读 `<Counters>`（total／executed／passed／failed／notExecuted）；② 逐 occurrence 扫 `<UnitTestResult` 开标签、按该标签内 `outcome` 属性计红（自闭合 `…/>` 与成对写法走同一条切法，**全程不用开—闭配对正则**），红格名／消息首行／栈里 `:line N` 从同一切片取；③ 每个红格名回判据源码按"方法名＋`(`"真实存在再验一遍（先剥掉全限定名前缀与 `[Theory]` 的参数表）。红数与 `<Counters failed>` 不等即打印 `DISAGREE`。

起跑第一轮之前，先拿盘上六份已知读数自证（整段输出归档 `out/instrument-selfproof.txt`）：

| 目录（盘上既有产物） | 期望 | 仪器实测（countersFailed／openTagFailed／红内容） | 对账 |
|---|---|---|---|
| `../wave33-1028/out/trx-M1` | 恰 1 | 1／1，红名 `…A_narrative_output_may_not_rewrite_the_stable_canon_layer`，消息首行 `Assert.Equal() Failure: Strings differ`，栈 `:47`，nameInCriteria=True | OK |
| `../wave33-1028/out/trx-M5` | 恰 1 | 1／1，红名 `…A_promise_send_proposes_a_commitment_but_proves_neither_consent_nor_completion`，`Assert.False() Failure`，栈 `:244` | OK |
| `../wave33-1028/out/trx-M7` | 恰 1 | 1／1，红名 `…Reading_experience_as_optional_material_without_a_validity_window_fails_loudly`，`System.InvalidOperationException : Nullable object must have a value.`，栈 `:46`（产品帧）＋`:256`（判据帧） | OK |
| `../wave33-1028/out/trx-F` | 4 份且 `failed` 全 0，total 108／785／280／233 | 4 份各 0／0；total 逐份 108／280／785（executed 783）／233；求和 total=1406／executed=1404／passed=1404／failed=0；`NotExecuted` 开标签 2 条＝`CoreTests.Configured_sense_voice_model_can_run_offline`／`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key` | OK |
| `../wave34-1028/out/trx-A1` | 恰 2 | 2／2，两枚红名 `…A_candidate_touching_all_three_surfaces_is_caught_by_the_first_gate`（`:167`）＋`…A_narrative_candidate_may_not_write_intimacy_state`（`:131`） | OK |
| `../wave34-1028/out/trx-A6` | 恰 1，红名逐字符含参数表 | 1／1，红名 `…A_delivered_body_that_was_suppressed_deduplicated_or_rejected_is_not_attribution_evidence(suppressed: True, deduplicated: False, rejected: False)`，栈 `:195` | OK |

**六格全对（9 份 TRX 全 `reconcile=OK`、`findings=0`、红名回判据全部 `nameInCriteria=True`）⇒ 允许用这台仪器裁这一把。** 未出现任一不符，故无需修仪器。

### 〇.7 焦点预检与预算声明

- 预检脚本＝`.local-notes/game-preflight6-1022.ps1`，每一次 `dotnet`（含每一枚突变轮与收尾全量）之前当场重取；编号从 **P60** 起；整段输出贴进报告并归档 `out/preflight-<ID>.txt`。只有 `VERDICT=BUILD_OK` 才起跑；读到 `HOLD`＝不起跑、不请示、不改判据、不绕过，HOLD 后重跑最多累计 8 次、相邻两次不短于 5 分钟（额度按刀计）。
- 预算：解决方案级构建 ≤1；解决方案级全量 ≤1（语义＝交付的读数只许来自 1 次全量）；定向 ≤8（A 类）＋≤2（C 类）＝10；`dotnet` 总调用上限 12。A 类与 C 类一律**不带 `--no-build`**（自行增量重建＝唯一能保证"跑的是当前源码"的写法）。
- **B1（解决方案级构建）跳过判定登记**：§〇.3 的主树侧 0 行式子成立 ✓，且载体 `src/Pry.Core/bin/Release/net10.0/Pry.Core.dll` 在盘（`-rwxr-xr-x 1 24782 197609 1745920 Oct 28 20:14`＝波 34 P59 收尾全量后产物），六枚 SHA 自入场起未变（§〇.4 三侧同值），载体 `status --short`＝0 行 ⇒ 按工单 §一.1 允许跳过 B1，构建预算用 0/1；每一轮定向与收尾全量自行增量重建，不存在"跑的不是当前源码"的形状。此判定与波 34 §〇.6 同类，但依据全部本刀当场重取。
- 作废轮（如有）的结果目录一律改名留档 `trx-<轮>-voided-<预检号>/`，不清空、不删除、不重跑同名目录。
- 动第一次 `dotnet` 之前已完成的零 `dotnet` 活：本节 §〇 全部＋§三.0 事前论证落盘＋十枚突变文本在临时副本上预合成并记 SHA-256（`out/mutation-prep-sha.txt`、`mutation-prep/`，见 §三.0 末段）。

### 〇.8 他人资产与 git 写边界声明

主树全程零 `git commit/add/checkout/reset/cherry-pick/merge/tag/push/apply/stash/clean/rm`；两枚 `M ` 暂存项与全部 `??` 未读不改。载体内只发生文件内容写入（临时突变）与逐轮按字节还原，本刀**零提交**（线尾仍 `51f149d`）。主树唯一新文件＝本报告，不 `git add`、不提交。

## 一 文件面自证（本刀实际写入清单）

- `.local-notes/wave35-1028/`（gitignored，本刀自建）：`w35-count.py`（仪器）、`w35-mutate.py`（突变＋还原＋核 SHA）、`mutation-prep/`（10 枚突变文本的临时副本）、`out/`（预检、manifest、各轮 TRX、计数存档）。
- 主树唯一新文件＝本报告 `docs/eval/reports/2026-10-28-hds009-wave35-red-proof-handover.md`。
- 载体内：仅 `src/Pry.Core/Narrative/StateAndLearning/*.cs` 的**临时**突变（十轮各自的宿主文件），逐轮按 `git show 51f149d:<path>` 的 blob 字节还原；终态相对 `51f149d` 净写入 0 字节。
- 判据文件 `tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs`：**零字节写入**（十轮每一轮 ⑩ 项都取 SHA-256 复核对齐 `317d2171…`）。
- 未新建任何产品文件、未新建任何测试文件、未碰 `scripts/**`／`Pry.slnx`／`Directory.Build.props`／其他 `docs/**`／主树任何文件；未往任何既有测试文件加行。

## 二 三计数自证（不许新增判据格）

判据文件（载体侧，入场时当场 `grep -c`）：`[Fact]` = **23**、`[Theory]` = **1**、`[InlineData` = **3** ⇒ 展开节点 23＋3＝26 枚格。与上一把报告 §二／守门人 §九.1 第 6 项的三计数（23／1／3）**逐位相同** ⇒ 本刀净新增格数＝0。本刀十轮只改产品文件（含两枚注释行的注入／摘除），不改任何断言、不改 `[InlineData]` 期望值、不 `Assert.Skip`、不注释既有断言、不新增数字门槛；终态侧将在 §六 再当场复数一次。

## 三 反证轮（事前论证先落盘；跑序固定 A1→A8→C1→C2→全部还原核 SHA→F）

### 三.0 事前论证（先落盘再起跑；下表是**假设**不是结论——实测枚数或格名与"预期枚数／格名"不符 ⇒ 整轮作废：红不署、不追第二轮、不许把本节改成实测值）

定向跑法（工单 §一.6 逐字，只换结果目录名，在载体内跑，不带 `--no-build`）：

```
dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave35-1028/out/trx-<轮>" --logger trx
```

本 lane 独立复推的依据（全部当场从盘上取，行号＝`51f149d` 侧）：判据里 `canon_revised_by_authority` 只 1 处（`:64`，所在方法声明 `:56`）；`experience_past_validity` 只 1 处（`:286`，方法 `:278`）；`inherited_source_missing` 2 处（`:365`＋`:367`）**但两处同在方法 `:356` 之内** ⇒ 红格数 1；覆盖层入册的来源身份只被 `:114-116`（`Assert.Single(layers.Overlays)`＋`overlay.SourceId == "src-1"`）读；继承行的来源身份只被 `:398-400` 读（`row.SourceId == "src-inherit"`，该串在判据共 2 处＝`:394` 入参加 `:399` 断言）；`a warm exchange` 在判据共 2 处＝`:267` 存入的正文＋`:273` 断言 `OptionalMaterial`（`:260`／`:287` 断的是 `Assert.Null`，走 `NotOffered` 支不读 `:52`）。第三面（交叉谓词）当场自证：十枚预合成文本按那两个扫描谓词各跑一遍＝A1–A8 全部 `legacyHits=[] defaultHits=[]`（不会连带打红另一枚扫描格）、C1 只命中 `ReplyGenerationRenderer`（不命中 `const `）、C2 只命中 `const `（不命中任何旧链名）——存档 `out/mutation-prep-sha.txt`＋`out/scan-predicate-crosscheck.txt`。

| 轮 | 类别 | 文件:行（`51f149d` 侧） | 改法（预合成文本见 `mutation-prep/`） | 被改那行的消费点（为何恰这一枚） | 预期恰响格（逐字符，前缀 `Pry.Core.Tests.StateAndLearningPrototypeTests.`） | 枚数 |
|---|---|---|---|---|---|---|
| A1 | 文本面 | `StateLayering.cs:82` | 字面量 `"canon_revised_by_authority"` → `"canon_revised_by_authority_wave35_alias"`（身份＝文本面，不冒充行为突变） | 该字面量在判据只 `:64` 一处（`Assert.Equal(…, readout.Reason)`），所在方法＝本格；另两枚 `ReviseCanon` 格断的是 `"Rejected"` 与 `"canon_source_unattributable"`，不读这串；`Outcome=="Applied"` 与 `CanonContent` 两行不受影响 | `An_author_side_canon_revision_with_a_pointable_source_is_applied` | 恰 1 |
| A2 | 行为 | `StateLayering.cs:99` | 入册行的 `draft.Source.SourceId` → `draft.OverlayKey`（来源身份记错；该行唯一锚点，`:92` 的同名表达式不在锚点内） | 全判据只有 `:114-116` 那组读入册行的 `SourceId`（`Assert.Single(Overlays)`＋`== "src-1"`）；`SourceVersion`/`ChangeDomain`/`Sequence`/Canon 那三行不变⇒红点唯一落在 `:115`；缺来源那格走 `:91-94` 拒绝支不入册（`Assert.Empty(Overlays)` 那格不入册、不读 `:99`） | `An_overlay_with_source_and_version_is_recorded_and_leaves_the_canon_layer_untouched` | 恰 1 |
| A3 | 行为 | `DeliveryAttribution.cs:60` | 只摘合取第二支：`evidence.BodySuppressed \|\| evidence.BodyRejected`（留第一、三支） | theory 三行各只置一枚排除位；摘 `Deduplicated` 支后只有第二行 `(False, True, False)` 漏过闸①（`BodyDelivered`/`HasExplicitClick` 均真）⇒入册、`:199 Assert.False(Admitted)` 红；第一、三行仍被留下的两支拦下、拒因不变仍绿；其余格不读此行（上一把同位置摘第一支已实测恰 1＝同族形状已被证过一次） | `A_delivered_body_that_was_suppressed_deduplicated_or_rejected_is_not_attribution_evidence(suppressed: False, deduplicated: True, rejected: False)` | 恰 1 |
| A4 | 行为 | `DeliveryAttribution.cs:60` | 只摘合取第三支：`evidence.BodySuppressed \|\| evidence.BodyDeduplicated`（留第一、二支） | 同上，只有第三行 `(False, False, True)` 漏过闸①；其余两行仍被留下的两支拦下 | `A_delivered_body_that_was_suppressed_deduplicated_or_rejected_is_not_attribution_evidence(suppressed: False, deduplicated: False, rejected: True)` | 恰 1 |
| A5 | 行为 | `ExperienceMaterial.cs:52` | `match.Material` → `null`（存了却不把材料交出去） | 全判据只有 `:273` 断 `Assert.Equal("a warm exchange", readout.OptionalMaterial)`；`:260`／`:287` 本就断 `Assert.Null`（走 `NotOffered` 支不读 `:52`）；`:299`／`:301-302` 断的是 `Reason` 与 `CarriesSpeakingAuthority`，不受 `OptionalMaterial` 影响 | `Experience_within_the_input_validity_window_is_offered_as_optional_material` | 恰 1 |
| A6 | 文本面 | `ExperienceMaterial.cs:50` | 字面量 `"experience_past_validity"` → `"experience_past_validity_wave35_alias"`（身份＝文本面） | 该字面量在判据只 `:286` 一处；同格那行 `Assert.Null(readout.OptionalMaterial)` 不受影响⇒整格仍红、红点唯一落 `:286` | `Experience_past_the_input_validity_window_is_not_offered` | 恰 1 |
| A7 | 行为 | `LegacyInheritance.cs:57` | 继承行的 `input.Source.SourceId` → `input.StateKey`（带上错的来源身份；锚点＝整行，`:51` 的同名表达式不在锚点内） | 判据里入册行的 `SourceId` 只被 `:398-400` 那组断（`row.SourceId == "src-inherit"`）；`Every_inherited_input…` 只断 Outcome 串（`:384`）与 `Imported.Count`（`:385`）——`SourceId` 变了不改这两行；缺来源那格走 `:50-53` 拒绝支不读 `:57`；`SourceVersion` 断言仍绿⇒红点唯一落 `:399` | `A_successful_inheritance_carries_its_source_reference_and_version` | 恰 1 |
| A8 | 文本面 | `LegacyInheritance.cs:53` | 拒因字面量 `"inherited_source_missing"` → `"inherited_source_missing_wave35_alias"`（身份＝文本面；这条正是波 34 守门人 §九.3 立的那笔断言级子账的方向） | 该串在判据出现两次（`:365`、`:367`）**但两处都在同一枚方法 `:356` 内**⇒红格数＝1 而不是 2；同格 `Outcome=="Failed"` 那两行仍绿、红点落在这两处之一；`Every_inherited_input…` 只断 Outcome 串，不读这串 | `Inherited_state_without_a_pointable_source_fails_loudly_and_is_never_silently_dropped` | 恰 1 |
| C1 | 注入（红证＋换手） | `StateLayering.cs` 文末追加一行注释：`// MUTATION C1 (temporary, wave35): ReplyGenerationRenderer` | 扫描函数 `LegacyChainNamesFoundIn`（判据 `:418-419`）按 `Contains` **连注释一起匹配**，`PrototypeSource`（`:427-432`）读的就是这五枚产品文件；`PrototypeFiles`（`:34-38`）第一枚即 `StateLayering.cs` ⇒ `:310` 那句 `Assert.Empty` 当场红；`:320-323` 的阳性探针句读的是临时样本文件、不是产品文件，保持通过（交叉谓词实测：C1 文本不命中 `const ` ⇒ `:336` 那格不受牵连） | `State_and_learning_prototype_sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites` | 恰 1 |
| C2 | 注入（红证＋换手） | `StateLayering.cs` 文末追加一行注释：`// MUTATION C2 (temporary, wave35): const 7` | `SourceSideDefaultsFoundIn`（判据 `:421-425`）匹配的字面量是 `"const "`（含尾空格）⇒ `:336` 那句 `Assert.Empty` 当场红；`:344` 探针句读临时样本、保持绿（波 34 的 B2 证过反方向＝把样本串换成不存在的 token 只会红探针句；本刀走反方向＝把样本塞进被扫面，且**判据字节零写入**）。交叉谓词实测：C2 文本不含任何旧链类型名 ⇒ `:310` 那格不受牵连 | `State_and_learning_prototype_declares_no_source_side_numeric_default` | 恰 1 |

**署名规则（本刀与上一把最大的不同）**：C1/C2 各为那两枚扫描格第一次拿到"非判据作者之手逼出的红"＝红证与换手两样同场结清，凭据＝十轮每一轮判据文件 SHA-256 都等于 `51f149d` 的 blob `317d2171…`。代价如实登记：注入串也会进别的按 `src/**/*.cs` 全仓扫描的门（例如 Api 那把资源限额台账覆盖门），但本刀十轮的过滤器只圈 `StateAndLearningPrototypeTests`（§一.6 跑法），那些类根本不执行 ⇒ **形状判定限于本类之内**；F 轮跑前已逐字节还原，全仓门读到的仍是干净树。

**A6 的替代方向不用**：`ExperienceMaterial.cs:49` 的比较符 `>` 翻向 `<` 是三响方向（工单明令：要用就得事前点名三枚格名并写"恰 3"）；本刀只用 A6 那条文本面配方（恰 1）。

**跑序固定＝A1→A8 先跑完（这八轮判据字节必须全程等于 blob 值），再跑 C1→C2，C 类还原并核 SHA 之后才允许收尾全量。**

### 三.0.1 预合成突变文本的 SHA-256（动第一次 `dotnet` 之前完成，零 dotnet；全文存档 `out/mutation-prep-sha.txt`）

| 轮 | 宿主文件 | clean blob 前缀 | 突变态 SHA-256（预合成） | 字节数变化 |
|---|---|---|---|---|
| A1 | `StateLayering.cs` | `af727a9c0435` | `b3989662b28d7ef31e54265eee15c0de87a2d301c03d960eedb105fa889e46b2` | 5445→5527 |
| A2 | `StateLayering.cs` | `af727a9c0435` | `4bd2bc69da4bf1cc67aa5676693fa1402b2f8dfcc2e099c3a54181f5b5b0716d` | 5445→5519 |
| A3 | `DeliveryAttribution.cs` | `5858a75da3ed` | `bb363a6cafb122905a09fa8a8c4226545c68149ca66a1b86b39fc75f1e6ed782` | 4622→4654 |
| A4 | `DeliveryAttribution.cs` | `5858a75da3ed` | `964070d22df97da4a061e90ebeeca9e9da441b663ea2b6a0f552914393b1e80f` | 4622→4657 |
| A5 | `ExperienceMaterial.cs` | `289fc89be087` | `ae9235a2b950bab2020ff997bd3357691328189390652fe50a905881bf179b4a` | 3020→3078 |
| A6 | `ExperienceMaterial.cs` | `289fc89be087` | `1fcfcd828b9a0d1977c8d5b576d55f19689292feac469d68bc4d9873782bbf5c` | 3020→3108 |
| A7 | `LegacyInheritance.cs` | `6a1f50999e7c` | `51c9cb9c29e8745f0e79e4468fc73ab3344afd11c3fca2428e7705ebe3ba9252` | 3988→4066 |
| A8 | `LegacyInheritance.cs` | `6a1f50999e7c` | `2ff1fa21ed9e275401cffd7827954c0ea1aac48fd030d447d3b9e88dc2dd36a1` | 3988→4077 |
| C1 | `StateLayering.cs` | `af727a9c0435` | `2ab20e48fcb6b47a507eb5d20210685d59ce54df187c8fcea8ec12ea2c84bb7c` | 5445→5505 |
| C2 | `StateLayering.cs` | `af727a9c0435` | `d9940f120f9791e1bb635599e953125c0d15a849756416a0cae8662fb82baba4` | 5445→5489 |

（每轮 apply 后仪器实测的突变态 SHA 必须等于本表值，等于即"跑的就是预合成那份文本"；不等＝当场停手登记，不解释为红。）

## 三.P 焦点预检台账（逐枚当场登记，含 HOLD；每一次 `dotnet` 之前一枚）

| 编号 | 时刻（当场 `date`） | VERDICT | 归档 | 处置 |
|---|---|---|---|---|
| P60 | 20:50:57（`out/preflight-P60.txt` 落盘时刻） | **HOLD** | `out/preflight-P60.txt` | 不起跑、不请示、不改判据、不绕过；HOLD 后重跑计数 0/8（P61 起算第 1 次重跑，与上一次相隔 ≥5 分钟） |
| P61 | 20:56:55（第 1 次重跑，距 P60 5 分 58 秒 ≥5 分钟） | **HOLD** | `out/preflight-P61.txt` | 同 P60：零 `dotnet`；重跑计数 1/8 |
| P62 | 21:02:38（第 2 次重跑，距 P61 5 分 43 秒 ≥5 分钟） | **HOLD** | `out/preflight-P62.txt` | 同 P60：零 `dotnet`；重跑计数 2/8 |
| P63 | 21:07:57（第 3 次重跑，距 P62 5 分 19 秒 ≥5 分钟） | **BUILD_OK** | `out/preflight-P63.txt` | 允许 A1 起跑；A1 是 P63 之后第一次 `dotnet`（本刀 `dotnet` 计数 1） |
| P64 | 21:12:00 | **BUILD_OK** | `out/preflight-P64.txt`（整段原文见 A2 节⑪） | 放行 A2（`dotnet` 计数 2） |
| P65 | 21:13:32 | **BUILD_OK** | `out/preflight-P65.txt`（整段原文见 A3 节⑪） | 放行 A3（`dotnet` 计数 3） |
| P66 | 21:14:56 | **HOLD** | `out/preflight-P66.txt` | A4 不起跑、不请示、不改判据、不绕过；`dotnet` 计数仍 3；重跑累计从 4/8 起算（P67）；该轮带 `free_physical_memory=5.5 GB` WARN（读数形状不作断言依据，不动任何进程） |
| P67 | 21:20:26（累计第 4 次重跑，距 P66 5 分 30 秒） | **HOLD** | `out/preflight-P67.txt` | 零 `dotnet`；`dotnet` 计数仍 3；重跑累计 4/8 |
| P68 | 21:26:14（累计第 5 次重跑，距 P67 5 分 48 秒） | **HOLD** | `out/preflight-P68.txt` | 零 `dotnet`；`dotnet` 计数仍 3；重跑累计 5/8；HOLD 期间完成的零 `dotnet` 活＝A1–A3 十一项落盘＋§〇.3 载体边界当场复核（主树 `git status --short` 与入场读数一致、` M docs/plans/PROJECT_PLAN.md` 仍是在册总计划在途登记不属本刀文件面、载体 `status --short` 0 行、`fd93ebe..HEAD` 仍 2 行、判据三计数当场复核 23／1／3 与上一把逐位相同） |
| P69 | 21:32:04（累计第 6 次重跑，距 P68 5 分 50 秒） | **HOLD** | `out/preflight-P69.txt`（整段原文见上） | 零 `dotnet`；`dotnet` 计数仍 3；重跑累计 6/8；A4 仍不起跑、不请示、不改判据、不绕过 |
| P70 | 21:37:45（累计第 7 次重跑，距 P69 5 分 41 秒） | **HOLD** | `out/preflight-P70.txt`（整段原文见上，含 `free_physical_memory=5.9 GB` WARN） | 零 `dotnet`；`dotnet` 计数仍 3；重跑累计 7/8 |
| P71 | 21:43:13（累计第 8 次重跑，距 P70 5 分 28 秒＝额度最后一枚） | **HOLD** | `out/preflight-P71.txt`（整段原文见上，紧接 P70 段之后） | **重跑额度 8/8 用尽** ⇒ A4 与其余六轮、收尾全量 F 一律不起跑；不追加第 9 次重跑、不绕开、不改判据；本刀停手交回 |

P60 整段输出原文：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: helldivers2 pid=25864 class=stingray_window style=0x94880000 rect=-1,-1,2562x1442 screen=2560x1440 covers=True borderless=False title=HELLDIVERS? 2
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21788, NVIDIA Overlay pid=18308, explorer pid=7404
gpu_util=43% used=6389MB
free_memory_gb=8.8
VERDICT=HOLD
  reason: focused window covers the screen and its process path is unreadable (helldivers2, class stingray_window)
  reason: focused window is a known game-engine window class covering the screen (stingray_window, helldivers2)
```

P61 整段输出原文（第 1 次重跑）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: helldivers2 pid=25864 class=stingray_window style=0x94880000 rect=-1,-1,2562x1442 screen=2560x1440 covers=True borderless=False title=HELLDIVERS? 2
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21788, NVIDIA Overlay pid=18308, explorer pid=7404
gpu_util=39% used=6639MB
free_memory_gb=6.2
VERDICT=HOLD
  reason: focused window covers the screen and its process path is unreadable (helldivers2, class stingray_window)
  reason: focused window is a known game-engine window class covering the screen (stingray_window, helldivers2)
```

HOLD 期间完成的零 `dotnet` 活（不占重跑额度）：§〇 全部落盘、§二 入场三计数、§三.0 事前论证落盘、十枚突变文本预合成并记 SHA-256（§三.0.1＋`out/mutation-prep-sha.txt`）、十枚注入串对两枚扫描谓词的交叉面自证（`out/scan-predicate-crosscheck.txt`）、仪器六份已知读数自证（`out/instrument-selfproof.txt`）。

P62 整段输出原文（第 2 次重跑）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: helldivers2 pid=25864 class=stingray_window style=0x94880000 rect=-1,-1,2562x1442 screen=2560x1440 covers=True borderless=False title=HELLDIVERS? 2
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21788, NVIDIA Overlay pid=18308, explorer pid=7404
gpu_util=44% used=6656MB
free_memory_gb=8.3
VERDICT=HOLD
  reason: focused window covers the screen and its process path is unreadable (helldivers2, class stingray_window)
  reason: focused window is a known game-engine window class covering the screen (stingray_window, helldivers2)
```

P63 整段输出原文（第 3 次重跑，放行 A1）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: ChatGPT pid=16348 class=Chrome_WidgetWin_1 style=0x14C70000 rect=-1770,207,1506x811 screen=1920x1080 covers=False borderless=False title=ChatGPT
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=46% used=6513MB
free_memory_gb=8.7
VERDICT=BUILD_OK
```

另：工单 §三 末段那句代价（"注入串会同时进入别的按 `src/**/*.cs` 全仓扫描的门"）本刀当场读码核实＝`tests/Pry.Api.Tests/ResourceLimitLedgerCoverageTests.cs:165` 确实 `EnumerateFiles(source, "*.cs", AllDirectories)` 扫全仓 src，但它的常量谓词是 `:30` 那条行首锚定的正则（`^\s*(?:[a-z]+\s+)*(?:const|static\s+readonly)\s+…`），`//` 开头的注释行读不出声明形状 ⇒ 本刀 C2 那枚注入串在该门上**不构成匹配面**（这是盘上读码事实，不是推测）；且本刀十轮过滤器只圈 `StateAndLearningPrototypeTests`，该类根本不执行。F 轮跑前已逐字节还原。

P66 整段输出原文（A4 前的预检＝HOLD，该轮零 `dotnet`）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: helldivers2 pid=25864 class=stingray_window style=0x94880000 rect=-1,-1,2562x1442 screen=2560x1440 covers=True borderless=False title=HELLDIVERS? 2
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21788, NVIDIA Overlay pid=18308, explorer pid=7404
gpu_util=46% used=6607MB
free_memory_gb=5.5
WARN (does not block the build):
  free_physical_memory=5.5 GB (gate readings may be invalid; SQLite disk-I/O/OOM shape)
VERDICT=HOLD
  reason: focused window covers the screen and its process path is unreadable (helldivers2, class stingray_window)
  reason: focused window is a known game-engine window class covering the screen (stingray_window, helldivers2)
```

P67 整段输出原文（累计第 4 次重跑，距 P66 5 分 30 秒 ≥5 分钟）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: helldivers2 pid=25864 class=stingray_window style=0x94880000 rect=-1,-1,2562x1442 screen=2560x1440 covers=True borderless=False title=HELLDIVERS? 2
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21788, NVIDIA Overlay pid=18308, explorer pid=7404
gpu_util=43% used=6921MB
free_memory_gb=7.3
VERDICT=HOLD
  reason: focused window covers the screen and its process path is unreadable (helldivers2, class stingray_window)
  reason: focused window is a known game-engine window class covering the screen (stingray_window, helldivers2)
```

P68 整段输出原文（累计第 5 次重跑，距 P67 5 分 48 秒 ≥5 分钟）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: helldivers2 pid=25864 class=stingray_window style=0x94880000 rect=-1,-1,2562x1442 screen=2560x1440 covers=True borderless=False title=HELLDIVERS? 2
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21788, NVIDIA Overlay pid=18308, explorer pid=7404
gpu_util=45% used=6688MB
free_memory_gb=8.7
VERDICT=HOLD
  reason: focused window covers the screen and its process path is unreadable (helldivers2, class stingray_window)
  reason: focused window is a known game-engine window class covering the screen (stingray_window, helldivers2)
```


P69 整段输出原文（累计第 6 次重跑，距 P68 5 分 50 秒 ≥5 分钟）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: helldivers2 pid=25864 class=stingray_window style=0x94880000 rect=-1,-1,2562x1442 screen=2560x1440 covers=True borderless=False title=HELLDIVERS? 2
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21788, NVIDIA Overlay pid=18308, explorer pid=7404
gpu_util=48% used=6730MB
free_memory_gb=6.6
VERDICT=HOLD
  reason: focused window covers the screen and its process path is unreadable (helldivers2, class stingray_window)
  reason: focused window is a known game-engine window class covering the screen (stingray_window, helldivers2)
```


P70 整段输出原文（累计第 7 次重跑，距 P69 5 分 41 秒 ≥5 分钟）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: helldivers2 pid=25864 class=stingray_window style=0x94880000 rect=-1,-1,2562x1442 screen=2560x1440 covers=True borderless=False title=HELLDIVERS? 2
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21788, NVIDIA Overlay pid=18308, explorer pid=7404
gpu_util=48% used=7442MB
free_memory_gb=5.9
WARN (does not block the build):
  free_physical_memory=5.9 GB (gate readings may be invalid; SQLite disk-I/O/OOM shape)
VERDICT=HOLD
  reason: focused window covers the screen and its process path is unreadable (helldivers2, class stingray_window)
  reason: focused window is a known game-engine window class covering the screen (stingray_window, helldivers2)
```


P71 整段输出原文（累计第 8 次＝最后一枚重跑额度，距 P70 5 分 28 秒 ≥5 分钟）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: helldivers2 pid=25864 class=stingray_window style=0x94880000 rect=-1,-1,2562x1442 screen=2560x1440 covers=True borderless=False title=HELLDIVERS? 2
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21788, NVIDIA Overlay pid=18308, explorer pid=7404
gpu_util=43% used=7457MB
free_memory_gb=8.6
VERDICT=HOLD
  reason: focused window covers the screen and its process path is unreadable (helldivers2, class stingray_window)
  reason: focused window is a known game-engine window class covering the screen (stingray_window, helldivers2)
```

## 三.A／三.C 定向轮逐轮十一项（一轮一节，跑完即追加）

### A1（文本面）——预检 P63＝BUILD_OK

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `src/Pry.Core/Narrative/StateAndLearning/StateLayering.cs:82` |
| ② 改前原文 | `        return new LayerReadout(request.CanonKey, "Applied", "canon_revised_by_authority", null);` |
| ③ 改后原文 | `        return new LayerReadout(request.CanonKey, "Applied", "canon_revised_by_authority_wave35_alias", null);  // MUTATION A1 (temporary, wave35): text face of the applied reason` |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/StateAndLearning/StateLayering.cs   | 2 +-`／` 1 file changed, 1 insertion(+), 1 deletion(-)`（存档 `out/a1-apply.txt`） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave35-1028/out/trx-A1" --logger trx` |
| ⑥ TRX 文件名 | `out/trx-A1/24782_PHYTHLINK_2026-10-28_21_08_22.trx` |
| ⑦ 红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.StateAndLearningPrototypeTests.An_author_side_canon_revision_with_a_pointable_source_is_applied`；消息＝`Assert.Equal() Failure: Strings differ`，Expected `"canon_revised_by_authority"`／Actual `"canon_revised_by_authority_wave35_alias"`（pos 26）；红点＝`StateAndLearningPrototypeTests.cs:line 64`。仪器读数：`total=26 executed=26 passed=25 countersFailed=1 openTagFailed=1 reconcile=OK notExecutedOpenTags=0 allRedNamesInCriteria=True` |
| ⑧ 还原命令 | `python w35-mutate.py revert A1`（内部＝把 `git show 51f149d:src/Pry.Core/Narrative/StateAndLearning/StateLayering.cs` 的字节原样写回该路径） |
| ⑨ 还原后 `git status --short` 行数 | `0` |
| ⑩ SHA-256 | 突变态实测 `b3989662b28d7ef31e54265eee15c0de87a2d301c03d960eedb105fa889e46b2` ＝§三.0.1 预合成值（match）；还原后 `af727a9c04355d853f756a8cd6d22e374ac889042c847f99906784e0bb20ca3a` ＝ blob（match）；**本轮判据字节未写入**：判据文件 `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc`（apply 时与 revert 时各测一次，两值均等于 `51f149d` blob） |
| ⑪ 当轮预检 | P63＝`VERDICT=BUILD_OK`（21:07:57，整段原文见 §三.P；归档 `out/preflight-P63.txt`） |

形状裁决＝实测恰 1 枚、格名与 §三.0 预论证逐字符相同 ⇒ 与事前论证一致，本轮有效（红是否署名由守门人裁，本 lane 不写状态词）。

### A2（行为）——预检 P64＝BUILD_OK

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `src/Pry.Core/Narrative/StateAndLearning/StateLayering.cs:99` |
| ② 改前原文 | `            draft.Source.SourceId, draft.Source.SourceVersion.Value, sequence));` |
| ③ 改后原文 | `            draft.OverlayKey, draft.Source.SourceVersion.Value, sequence));  // MUTATION A2 (temporary, wave35): overlay records the wrong source identity` |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/StateAndLearning/StateLayering.cs   | 2 +-`／` 1 file changed, 1 insertion(+), 1 deletion(-)`（存档 `out/a2-apply.txt`） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave35-1028/out/trx-A2" --logger trx` |
| ⑥ TRX 文件名 | `out/trx-A2/24782_PHYTHLINK_2026-10-28_21_12_17.trx` |
| ⑦ 红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.StateAndLearningPrototypeTests.An_overlay_with_source_and_version_is_recorded_and_leaves_the_canon_layer_untouched`；消息＝`Assert.Equal() Failure: Strings differ`，Expected `"src-1"`／Actual `"rel-1"`（pos 0，即入册行把覆盖层键当成了来源身份）；红点＝`StateAndLearningPrototypeTests.cs:line 115`。仪器读数：`total=26 executed=26 passed=25 countersFailed=1 openTagFailed=1 reconcile=OK notExecutedOpenTags=0 allRedNamesInCriteria=True` |
| ⑧ 还原命令 | `python w35-mutate.py revert A2`（内部＝把 `git show 51f149d:src/Pry.Core/Narrative/StateAndLearning/StateLayering.cs` 的字节原样写回该路径） |
| ⑨ 还原后 `git status --short` 行数 | `0` |
| ⑩ SHA-256 | 突变态实测 `4bd2bc69da4bf1cc67aa5676693fa1402b2f8dfcc2e099c3a54181f5b5b0716d` ＝§三.0.1 预合成值（match）；还原后 `af727a9c04355d853f756a8cd6d22e374ac889042c847f99906784e0bb20ca3a` ＝ blob（match）；**本轮判据字节未写入**：判据文件 `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc`（apply 时与 revert 时各测一次，两值均等于 `51f149d` blob） |
| ⑪ 当轮预检 | P64＝`VERDICT=BUILD_OK`（21:12:00，归档 `out/preflight-P64.txt`；整段原文见下） |

P64 整段输出原文：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: ChatGPT pid=16348 class=Chrome_WidgetWin_1 style=0x14C70000 rect=-1770,207,1506x811 screen=1920x1080 covers=False borderless=False title=ChatGPT
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=34% used=6129MB
free_memory_gb=9.3
VERDICT=BUILD_OK
```

形状裁决＝实测恰 1 枚、格名与 §三.0 预论证逐字符相同，红点落点＝预论证指明的 `:115` ⇒ 与事前论证一致，本轮有效。

### A3（行为·摘合取第二支）——预检 P65＝BUILD_OK

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `src/Pry.Core/Narrative/StateAndLearning/DeliveryAttribution.cs:60` |
| ② 改前原文 | `        if (evidence.BodySuppressed || evidence.BodyDeduplicated || evidence.BodyRejected)` |
| ③ 改后原文 | `        if (evidence.BodySuppressed || evidence.BodyRejected)  // MUTATION A3 (temporary, wave35): second conjunct removed` |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/StateAndLearning/DeliveryAttribution.cs | 2 +-`／` 1 file changed, 1 insertion(+), 1 deletion(-)`（存档 `out/a3-apply.txt`） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave35-1028/out/trx-A3" --logger trx` |
| ⑥ TRX 文件名 | `out/trx-A3/24782_PHYTHLINK_2026-10-28_21_13_55.trx` |
| ⑦ 红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.StateAndLearningPrototypeTests.A_delivered_body_that_was_suppressed_deduplicated_or_rejected_is_not_attribution_evidence(suppressed: False, deduplicated: True, rejected: False)`（theory 参数与预论证逐字符一致＝只有第二行漏过闸①）；消息＝`Assert.False() Failure`，Expected `False`／Actual `True`；红点＝TRX 内层测试帧 `StateAndLearningPrototypeTests.cs:line 195`（源码里该断言 `Assert.False(readout.Admitted)` 在 `:199`；Release 行号映射把整条断言语句记在它所属语句块的 `:195`——**这条映射差异未验证**，不在形状判定项内）。仪器读数：`total=26 executed=26 passed=25 countersFailed=1 openTagFailed=1 reconcile=OK notExecutedOpenTags=0 allRedNamesInCriteria=True` |
| ⑧ 还原命令 | `python w35-mutate.py revert A3`（内部＝把 `git show 51f149d:src/Pry.Core/Narrative/StateAndLearning/DeliveryAttribution.cs` 的字节原样写回该路径） |
| ⑨ 还原后 `git status --short` 行数 | `0` |
| ⑩ SHA-256 | 突变态实测 `bb363a6cafb122905a09fa8a8c4226545c68149ca66a1b86b39fc75f1e6ed782` ＝§三.0.1 预合成值（match）；还原后 `5858a75da3ed70e35f93782e1d760c87eea3feeecdafcdd645b27f69ea66f0ab` ＝ blob（match）；**本轮判据字节未写入**：判据文件 `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc`（apply 时与 revert 时各测一次，两值均等于 `51f149d` blob） |
| ⑪ 当轮预检 | P65＝`VERDICT=BUILD_OK`（21:13:32，归档 `out/preflight-P65.txt`；整段原文见下） |

P65 整段输出原文：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: ChatGPT pid=16348 class=Chrome_WidgetWin_1 style=0x14C70000 rect=-1770,207,1506x811 screen=1920x1080 covers=False borderless=False title=ChatGPT
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=43% used=6189MB
free_memory_gb=8.8
VERDICT=BUILD_OK
```

形状裁决＝实测恰 1 枚、格名（含 theory 参数串）与 §三.0 预论证逐字符相同 ⇒ 与事前论证一致，本轮有效；红点行号的语句块映射差异如上如实登记，不作作废依据（作废条件只有"枚数／格名与事前论证不符"）。



### 三.F 收尾全量轮（未起跑）

收尾全量 F 的跑法（登记在此，未执行）：`dotnet test Pry.slnx -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave35-1028/out/trx-F" --logger trx`（不带 `--no-build`）。**本刀未跑**＝F 之前的定向轮未跑满十枚（A4–A8、C1、C2 六枚未起跑），按 §三 末段固定跑序"全部还原核 SHA 之后才允许收尾全量"虽已满足"载体 0 行"这一条，但工单要求的收尾全量是"十轮之后"那一枚，而十轮没有跑完 ⇒ 起跑它＝把工单的跑序改形；加之焦点预检 HOLD 重跑额度 8/8 已用尽（P71），本刀无任何再起跑的额度。四份 TRX 的四数求和（1406／executed 1404／failed 0／跳过 2）与两条同名跳过格名＝**本刀未验证**，本刀不产出任何全量读数，也不顶账、不为顶账起构建。

## 四 本刀跑到哪一行／下一手从哪一行接（如实登记，不含裁决）

- 已完成并逐轮还原的定向轮：**A1、A2、A3**（三轮均实测恰 1 枚红、格名与 §三.0 预论证逐字符一致、`git status --short` 还原后 0 行、判据文件 SHA-256 在 apply 与 revert 两侧都等于 `51f149d` 的 blob `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc`）。十一项字段见 §三.A／三.C 对应小节，⑪ 项预检整段另见 §三.P。
- **未起跑的轮**：A4、A5、A6、A7、A8、C1、C2（七枚定向）＋ F（收尾全量）。原因＝每一次 `dotnet` 之前必须当场一枚 `VERDICT=BUILD_OK` 的预检，而 21:14:56 起作者机器回到 HELLDIVERS 2 全屏（pid 25864，class `stingray_window`，covers=True），P66/P67/P68/P69/P70/P71 连续 6 枚 HOLD，HOLD 后重跑额度（最多累计 8 次，本刀已用满：P61、P62、P63、P67、P68、P69、P70、P71）在 P71 用尽 ⇒ 按工单 §一.7 停手交回，不起跑、不请示、不改判据、不绕过、不追加第 9 次重跑。
- 下一手的接手点（配方与凭据都已在盘上，不需要重做前置）：
  1. 先确认载体仍是 `51f149d0b722e28a6566c93d19f48a94ca4c85df`、`git -C <carrier> status --short` ＝ 0 行、六枚 SHA 仍等于本报告 §〇.4 的入场基准（本刀出场读数见 §六）。
  2. 当场取一枚新预检（本刀编号已用到 **P71**，下一把从 **P72** 起编；本刀的 HOLD 重跑额度 8/8 已用尽，下一把按工单自行重置额度），`BUILD_OK` 才起跑 A4。
  3. A4 配方＝§三.0 表 A4 行（`DeliveryAttribution.cs:60` 只摘合取第三支），预合成文本与突变态 SHA 见 `mutation-prep/A4_DeliveryAttribution.cs.cs`（`964070d22df97da4a061e90ebeeca9e9da441b663ea2b6a0f552914393b1e80f`）；apply／revert／核 SHA／数红由 `.local-notes/wave35-1028/w35-mutate.py`、`w35-count.py` 执行（两枚仪器为本刀自写，§〇.6 有六份已知读数的自证）。A5–A8、C1、C2 同法，跑序固定 A 类先跑满再 C 类，C 类还原并核 SHA 之后才允许 F。
  4. 事前论证已在 §三.0 落盘且未改动（本刀没有作废轮，没有任何一格被事后改写成实测值）；十轮预期形状仍照该表读。
- 本刀**不**碰的账（工单 §四，一律未跑、未读、未改）：波 33 的 M1–M7 换人重取（那是第三十六波另出一单的活）、断言级子账另两条（`BoundedAdvancementPrototypeTests.cs:195`／`:172`，属 007 那棵载体）、波 28 数值锁格、两套并行命名债、`PrototypeFiles` 写死五枚清单的扫描盲区、HDS-010 删除片、HDS-007 生产接线。

## 五 未验证（逐条点名，不用"已完成"混过去）

1. A4–A8 五枚行为／文本面突变：**未验证**（未起跑，HOLD 额度耗尽）。
2. C1、C2 两枚注入轮：**未验证**（未起跑）⇒ 两枚扫描格"另一双手逼出的红"与"红证＋换手"这两件事本刀没有拿到任何读数。
3. 收尾全量四数（1406／executed 1404／failed 0／跳过 2）与两条同名跳过格名：**未验证**（本刀零全量调用）。
4. 判据三计数 23／1／3：入场与出场各当场复核一次相同（§二、§六），但"净新增格数必须 0"是否成立由守门人按工单 §五.6 的口径裁，本 lane 不作状态词。
5. A3 那轮红点行号的映射差异（TRX 内层测试帧报 `:195`，源码断言 `Assert.False(readout.Admitted)` 在 `:199`）：**未验证**（Release 行号映射机理未查；本刀不改判据、不改跑法，只在 ⑦ 项如实登记两处读数）。
6. 真实模型对照：零读数，未验证（需作者一句"可以跑"）。
7. 生产接线（这 26 枚至今零生产调用方）：未验证、未做；要接的 005 落库面与 004 生命周期一律没接；接线时机＝U-9 残句仍未拍。
8. 旧数据真实继承：零读取，未验证。
9. UI 可见行为：零读数，未验证。
10. 生产库与 `%LOCALAPPDATA%\PryCompanion`：零接触。
11. 待标定参数权威值（关系／心情各档阈值、经验有效期、点击归因窗口）：全部未定＝特性不是缺陷，未验证。
12. 并发与断电：零读数，未验证。
13. 旧链删除片与"构建时不把旧链打进包"那道门：未开始，未验证。
14. 换手终账：不由本 lane 签；本刀只跑满 3/10 轮，故既没结清 §四.1 那 7 枚 M 系的账，也没拿到 C 类那两枚的任何红。旧欠 **75 格**（波 13 余 47＋波 20 余 28）本刀未碰、照旧不减；拆法原文照旧 46／45／1＝W2。

## 六 盘上产物与预算自校（出场读数）

- 盘上 `.trx` 枚数＝**3**：`out/trx-A1/24782_PHYTHLINK_2026-10-28_21_08_22.trx`、`out/trx-A2/..._21_12_17.trx`、`out/trx-A3/..._21_13_55.trx`；登记定向轮数＝3，每轮 1 份 ⇒ 3×1＝3 对账相符 ✓。作废轮＝**0 枚**，故盘上无 `trx-<轮>-voided-<预检号>` 目录（不清空、不删除、不重跑同名目录）。收尾全量＝0 份目录（`out/trx-F` 未创建，因未起跑）。工单满额形状（定向 10×1＋全量 1×4＝14 枚）本刀只到 3 枚，差额 11 枚＝未跑的 A4–A8（5）、C1–C2（2×1＝2）、F（4）⇒ 5＋2＋4＝11 ✓。
- 焦点预检台账＝**12 枚**（P60–P71，全部归档 `out/preflight-P<ID>.txt` 且整段贴进 §三.P／各轮 ⑪ 项）：`BUILD_OK` 3 枚（P63、P64、P65）／`HOLD` 9 枚（P60、P61、P62、P66、P67、P68、P69、P70、P71）；HOLD 后重跑＝**8/8 用尽**（相邻两次间隔 5 分 58／43／19〔P63 距 P62〕／30〔P67 距 P66〕／48／50／41／28 秒，全部 ≥5 分钟）。
- `dotnet` 实际调用＝**3 次**（A1、A2、A3 各一枚定向，全部不带 `--no-build`）≤ 上限 12 ✓；解决方案级构建 B1＝0 次（跳过判定登记见 §〇.7）；解决方案级全量＝0 次。
- 载体出场终态（当场取）：`git -C <carrier> rev-parse HEAD`＝`51f149d0b722e28a6566c93d19f48a94ca4c85df`；`git status --short`＝**0 行**；`git log --oneline fd93ebe..HEAD`＝**2 行**（`1e02205`、`51f149d`，本刀零提交）；六枚文件工作副本 SHA-256 与 `51f149d` 侧 blob **逐字符相同、mismatch＝0**：`StateLayering.cs` `af727a9c0435…`／`NarrativeAuthority.cs` `000f2fc8bfd5…`／`DeliveryAttribution.cs` `5858a75da3ed…`／`ExperienceMaterial.cs` `289fc89be087…`／`LegacyInheritance.cs` `6a1f50999e7c…`／判据 `317d2171264f…`（全文见 §〇.4）⇒ 产品字节终态＝零净写入，判据字节＝零写入。
- 判据三计数出场复核＝`[Fact]` 23／`[Theory]` 1／`[InlineData]` 3（与 §二 入场读数、上一把在册读数逐位相同）；`public void` 方法数 24（23 枚 Fact 方法＋1 枚 Theory 方法），测试节点 26＝23＋3，与三轮仪器读数 `total=26` 一致。
- 主树出场 `git status --short`＝入场读数（§〇.2）逐行相同，只多一行本报告：`?? docs/eval/reports/2026-10-28-hds009-wave35-red-proof-handover.md`（不 `git add`、不提交）；主树 `HEAD` 仍 `a955f227d31d816286d9469c178659ba18424d14`；两枚 `M ` 暂存项与全部 `??` 未跟踪资产未读内容、未改、未提交、未 `stash`／`clean`／`rm`。
- `out/` 目录另有 **10 份**非本刀产物（时间戳 20:33:57–20:47:22，全部早于本刀第一枚预检 P60＝20:50:57）：`plan-theirs-mine-backup.md`、`plan-head.md`、`plan-commit.md`、`plan-final.md`、`commit-msg.txt`、`mem-add.txt`、`mem-add2.txt`、`mem-append.js`、`mem-index.js`、`mem-index-fix.js`。为判归属只取了 `stat` 时间戳与个别首行片段，未改、未删、未移动、未提交（不在本刀文件面）。本刀自产物清单见 §一：仪器与突变脚本（`w35-count.py`、`w35-mutate.py`）、`mutation-prep/`（10 枚预合成副本）、`out/instrument-selfproof.txt`、`out/mutation-prep-sha.txt`、`out/scan-predicate-crosscheck.txt`、`out/preflight-P60..P71.txt`（12 枚）、`out/a1|a2|a3-apply.txt`、`out/a1|a2|a3-run.txt`、`out/trx-A1|A2|A3/`（各 1 份 `.trx`）。

## 七 交守门人核的六件事·凭据位置（本 lane 只登记凭据，不作裁决、不写状态词）

1. 每轮红格归属：A1／A2／A3 三节的 ⑦ 项给出红格全名、消息要点与 TRX 内层帧行号；⑥ 项给出 TRX 文件名；数红口径＝`<UnitTestResult … outcome="Failed">` 开标签枚数与同份 `<Counters failed>` 双向对账（仪器三轮均报 `openTagFailed=1 countersFailed=1 reconcile=OK`、`allRedNamesInCriteria=True`、`notExecutedOpenTags=0`），抽红名／消息／行号未用开闭标签配对正则（§〇.6 记仪器口径）。批次红：三轮均为定向单枚突变、单份 TRX，无合轮。
2. 载体终态：见 §六 出场读数四行（HEAD／0 行／2 行／六枚 SHA）。
3. 收尾全量四数：**本刀未验证**（§三.F），无读数可核。
4. 作废轮登记：本刀 0 枚作废轮；`.trx` 枚数与轮数对账见 §六 首条（含与工单满额 14 枚的差额去向）。
5. 十轮判据文件 SHA-256：本刀只跑了 3 轮，3 轮 ⑩ 项各给 apply 侧与 revert 侧两个读数，四值全部等于 `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc`＝`51f149d` 的 blob；**其余 7 轮（A4–A8、C1、C2）没有 SHA 读数可交**，故本刀不主张任何"十轮全对齐"的形状，C 类两枚的红证与换手本刀未取到、请押给下一把。
6. 净新增格数／改断言／改数字门槛：本刀文件面见 §一（判据与产品均为零终态字节），三计数 23／1／3 入场出场各一次相同；除 §三.0 预论证表所指的临时突变外，本刀未新增任何测试、未注释任何断言、未动 `[InlineData]` 期望值、未动 `scripts/**`／`Pry.slnx`／`Directory.Build.props`、未动任何上游门、未 `Assert.Skip`。

## 九 守门人划账（追加；§〇–§七 一字未改，numstat 只增不删）

### 九.0 我这一把用的仪器（不复用 lane 的实现，也不复用上一把的实现）

- 仪器＝守门人自写 `.local-notes/wave35-1028/gk35-audit.js`（JavaScript＋引号感知切标签，与 lane 的 `w35-count.py`（Python＋逐 occurrence 找标签末）实现语言与切法都不同）。三面照旧：① 逐份 `<Counters>`（total／executed／passed／failed／notExecuted）；② 只数 `<UnitTestResult … outcome="Failed">` **开标签**并与同份 `<Counters failed>` 双向对账（全程不用开—闭配对正则）；③ 每个红格名先剥全限定前缀与 `[Theory]` 参数表，再回判据源码按"方法名＋`(`"验真实存在。
- **口径⓪：用之前先拿盘上已知读数自证这台仪器**，七面全对才允许它裁这一把（守门人当场跑，输出留档 lane 的报告之外，实测值逐条＝）：波 33 `trx-M1`＝恰 1 且红名含 `A_narrative_output_may_not_rewrite_the_stable_canon_layer`／`trx-M5`＝恰 1 且含 `A_promise_send_proposes_a_commitment_but_proves_neither_consent_nor_completion`／`trx-M7`＝恰 1 且含 `Reading_experience_as_optional_material_without_a_validity_window_fails_loudly`／波 33 `trx-F`＝**4 份、total 1406／executed 1404／passed 1404、开标签红＝0、`NotExecuted` 开标签 2 条**（点名＝`CoreTests.Configured_sense_voice_model_can_run_offline`／`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`，与波 28–34 同名同枚）／波 34 `trx-A1`＝恰 2／波 34 `trx-A6`＝恰 1 且红名逐字符含参数表 `(suppressed: True, deduplicated: False, rejected: False)`。⇒ `SELF-CHECK RESULT: ALL FACES AGREE`，无一面不符，故不需要先修仪器。

### 九.1 工单 §五 那六件事逐件判定

1. **每轮红格归属**：三轮我用自己那台仪器独立复采，读数与 lane 的 ⑦ 项逐字符相符＝`trx-A1` `total=26／executed=26／passed=25／countersFailed=1／openTagFailed=1／reconcile=OK／notExecutedOpenTags=0／nameInCriteria=true`，红名 `An_author_side_canon_revision_with_a_pointable_source_is_applied`、消息 `Assert.Equal() Failure: Strings differ`、栈 `:64`；`trx-A2` 同形状、红名 `An_overlay_with_source_and_version_is_recorded_and_leaves_the_canon_layer_untouched`、红点 `:115`；`trx-A3` 同形状、红名含 theory 参数串 `(suppressed: False, deduplicated: True, rejected: False)`、消息 `Assert.False() Failure`、栈 `:195`。**三枚格名与我自己在工单 §三.0 的事前论证逐字符相同**（先档后跑，跑序未被事后改写：§三.0 表与本刀实测之间无任何一处把预期值改成实测值），且三轮互不重叠、无批次红署给单枚的形状。**裁决＝三轮有效，且归属无歧义。**
2. **载体终态**（我当场重取，不采信自述）：`rev-parse HEAD`＝`51f149d0b722e28a6566c93d19f48a94ca4c85df`；`status --short`＝**0 行**；`log --oneline fd93ebe..HEAD`＝**2 行**（本刀零提交）；`diff --stat fd93ebe HEAD -- src tests scripts Pry.slnx Directory.Build.props`＝**6 files changed, 822 insertions(+), 0 deletions**（仍只有波 33 那六枚）。六枚文件**三侧 SHA-256 逐字符同值**（载体工作副本＝`git show 51f149d:<path>` 的 blob＝主树工作副本）＝`af727a9c0435`／`000f2fc8bfd5`／`5858a75da3ed`／`289fc89be087`／`6a1f50999e7c`／判据 `317d2171264f`，mismatch＝0 ⇒ 产品字节终态净写入 0、判据字节净写入 0。
3. **收尾全量**：本刀**未跑** ⇒ `1406／executed 1404／failed 0／跳过 2` 这四数**本刀未复采**，在册基线一字不动、也不因这一把移动。这一格的裁决是：**没跑 F 是本刀正确的动作，不是偷懒**（工单 §三 末段把跑序固定成"A 类跑满→C 类→还原核 SHA→才允许收尾全量"，十轮只跑了 3 轮就起 F＝把工单跑序改形）。代价如实登记＝本刀不产出任何全量读数，交付读数仍只能引波 33／波 34 那两次同值复采。
4. **作废轮与盘上对账**：作废轮＝**0 枚**，盘上无 `trx-*-voided-*`；`find out -name "*.trx"`＝**3 枚**＝登记定向轮数 3×每轮 1 份 ✓；`out/trx-F` 未创建（与"未起跑"自洽）。与工单满额形状 14 枚的差额 11 枚有明确去处＝未跑的 A4–A8（5）＋C1–C2（2）＋F（4）＝11 ✓，**没有"少记一轮"那种形状**（波 33 那把盘上 12 枚而真实 16 枚的教训没有重演）。
5. **签账资格凭据（本刀的胜负手）**：三轮 ⑩ 项各给了 apply 侧与 revert 侧两个判据文件 SHA-256，四值全等于 `51f149d` 的 blob `317d2171…`；我自己另取盘上现值复核＝主树工作副本与载体 blob 同值；判据字节的 last-touch 在主树侧＝`e04e7e6`（守门人按路径落地的波 33 代码刀）、载体侧＝`51f149d`（波 33 的判据整形），**两者都不是这一把那只手** ⇒ 取红的手既不是判据作者、这一轮也没写过判据一个字节。按口径（波 24／27／28／30／34 用过同一把尺）**这 3 枚的红证与第二换手同场结清**。反面：其余 7 轮（A4–A8、C1、C2）没有 SHA 读数可交，本刀**不主张**任何"十轮全对齐"的形状，lane 自己也这么登记了（§七.5），这句诚实要记进账。
6. **净新增格数／门槛／断言**：判据三计数我当场复核＝`[Fact]` 23／`[Theory]` 1／`[InlineData` 3 ⇒ 展开 26 枚节点，与波 33／波 34 逐位相同 ⇒ **净新增格数 0**、新增数字门槛 0、注释或放宽既有断言 0、`Assert.Skip` 0。`dotnet` 实际调用＝**3 次** ≤12；解决方案级构建 B1＝0/1（跳过判定＝主树侧 `51f149d..HEAD` 那 0 行式子成立＋载体 `bin/Release/net10.0/Pry.Core.dll` 在盘 20:14 那一份＋六枚 SHA 自入场未变，我当场重取那条 0 行式子＝0 行 ✓）；全量 0 次。

### 九.2 划账（波 33 那 26 枚的现账）

- **本刀结清 3 枚（红证＋第二换手双结）**：`An_author_side_canon_revision_with_a_pointable_source_is_applied`、`An_overlay_with_source_and_version_is_recorded_and_leaves_the_canon_layer_untouched`、`A_delivered_body_that_was_suppressed_deduplicated_or_rejected_is_not_attribution_evidence(suppressed: False, deduplicated: True, rejected: False)`。这 3 枚正是波 34 §九.2 那本"仍欠红证 7 枚"里的三枚（我按波 34 报告 §四 那行逐格名单对过，不是按轮次数加的）。
- **现账拆分（26 枚，按格名去重后）**＝**13 枚红证＋换手双结**（波 34 A 类 10＋本刀 3）／**2 枚只有红证、欠换手**（波 34 B 类那两枚扫描格，本刀 C1/C2 那条路未跑成）／**7 枚红证在手仍欠另一双手**（波 33 的 M1–M7）／**4 枚仍完全没有红**（theory 第三行 `(suppressed: False, deduplicated: False, rejected: True)`、`Experience_within_the_input_validity_window_is_offered_as_optional_material`、`Experience_past_the_input_validity_window_is_not_offered`、`A_successful_inheritance_carries_its_source_reference_and_version`）。13＋2＋7＋4＝26 ✓。**完整换手欠账：16 枚 → 13 枚**（结清 3 枚＝7 枚欠红证那批里的 3 枚同时把换手拿到手）。**红证欠账：7 枚 → 4 枚。**
- **断言级子账照旧未结**＝`Inherited_state_without_a_pointable_source_fails_loudly_and_is_never_silently_dropped` 那枚方法体内的拒因字面量断言（`:365`／`:367`）至今没有单独响过（波 34 那轮整格因产品侧 `NullReferenceException` 而红）；方向＝本刀 A8，配方与突变态 SHA 已在盘上（`mutation-prep/A8_LegacyInheritance.cs.cs`＝`2ff1fa21ed9e…`），押第三十六波。
- **旧账与别的账一律不因这一把变动**：波 13 余 47＋波 20 余 28＝旧欠 **75 格**照旧不减（与本刀无交集，出账前按格名对过）；拆法原文照旧 **46／45／1＝W2**；在册基线照旧 **1406／executed 1404／failed 0／跳过 2**（本刀零全量调用，不顶账、不为顶账起构建）。

### 九.3 两处如实登记（一处是我自己那一把的排期歧义，一处是仪器的形状）

- **排期歧义记在我头上**：工单 §四.1 写过"波 33 那 7 枚 M1–M7 押第三十六波"，而本刀只跑到 3/10 轮、剩 7 轮未完。第三十六波若照原句去跑 M1–M7，就会把本刀这 7 轮未完的账推成"没人认领"。**改判＝第三十六波先把本刀剩下的 A4–A8＋C1–C2＋F 跑完（配方与凭据全在盘上，不需重做前置），M1–M7 就地改押第三十七波。** 这条改动是派发者调自己的排期，不是执行方扩范围。
- **HOLD 额度的实测形状（仪器口径调整，不是放宽判据）**：本刀 12 枚预检我当场用文件 mtime 逐枚复算时刻与相邻间隔＝P60 20:50:57、P61 20:56:54（5′57″）、P62 21:02:38（5′44″）、P63 21:07:57（5′19″，**BUILD_OK**）、P64 21:12:02、P65 21:13:34（这两枚也 BUILD_OK）、P66 21:14:59 起连续 HOLD、P67 21:20:28（5′29″）、P68 21:26:17（5′49″）、P69 21:32:07（5′50″）、P70 21:37:48（5′41″）、P71 21:43:15（5′27″）⇒ 8 枚重跑全部 ≥5 分钟、第 9 次没有跑、`dotnet` 只在 BUILD_OK 之后起（三轮 TRX 时间戳 21:08:22／21:12:17／21:13:55 各自紧跟一枚 BUILD_OK 预检，无一处"预检 HOLD 仍起跑"的形状）。**但形状暴露出来的是**：窗口是短促的（21:07–21:13 那 6 分钟连着三枚 OK，21:14 游戏回到前台），而额度按刀计 ≤8 枚＝大约 45 分钟等待就吃光，于是一把最多榨到 3 轮。预检脚本本身是只读的、不打扰作者；真正打扰他的是 `dotnet` 那一下弹窗。⇒ **第三十六波把上限改为 ≤16 枚重跑、间隔仍 ≥5 分钟**（HOLD 的语义一字未改：读到 HOLD 一律不起跑、不请示、不绕过、不改判据），并明写"窗口一开就把能跑的轮次连着跑完，每一轮各自一枚预检"。这条登记为**仪器参数**，不记为判据放宽。
- **A3 那条红点行号**（lane 列为未验证的第 5 条）我读码判一半、留一半：判据方法体 `:190-201` 内 `Assert.False` **只有 `:199` 一处**（我把全文 12 处 `Assert.False` 的行号都列出来核过），消息又明写 `Assert.False() Failure`，参数串 `(suppressed: False, deduplicated: True, rejected: False)` 就在 testName 里 ⇒ **归属无歧义，红点行号只作辅证**，本刀的署名不依赖它。留下的：Release 序列点为何把 `:199` 记在它上方那条多行调用 `:195`（`Admit(...)` 的语句起始行），机理**仍未验证**；同一位置波 34 那轮的 `trx-A6` 也报 `:195`（两处读数一致＝不是本刀的形状）。这条作为"仪器已知形状"传给下一把，不作为缺陷。

### 九.4 归位判定

零落地。主树侧 `git diff --stat 51f149d HEAD -- src tests scripts Pry.slnx Directory.Build.props`＝**0 行**（我当场重取），载体 `status`＝0 行、六枚 SHA 三侧同值 ⇒ 本刀**没有代码刀可归位、不做按路径落地、`main` 不因本刀挪**（只有收口文档刀自己挪）。文件面＝本刀新增跟踪文件一枚（本报告）＋第三十六波工单一枚；他人两枚 `M ` 暂存项与五枚 `??` 未跟踪资产一字未动；`PROJECT_PLAN.md` 用 hunk 手术把他人那 18 行排除在本把提交外、提交后原样还回并复测仍 18/0。

### 九.5 押下一把（第三十六波＝`docs/dispatch-hds-wave36-1028.md`）

七轮定向（A4–A8、C1、C2）＋一轮收尾全量，跑序固定 A→C→还原核 SHA→F；预检编号从 **P72** 起；`dotnet` ≤8、HOLD 重跑额度 ≤16、按刀计；十轮一律不许写判据字节（C1/C2 能签换手的唯一凭据就是每轮 ⑩ 项等于 `317d2171…`）；零新增格／零产品字节终态／零落地／零顶账。三枚扫描格与两枚注入格的形状判定范围仍只圈 `StateAndLearningPrototypeTests`。M1–M7 改押第三十七波（配方逐字仍见波 33 报告 §三.0，含"M5 只翻 `DeliveryAttribution.cs:88`、不许连 `:89` 一起翻"那条）。**三句话照旧不许写**＝新链已接线／旧链可删／不进包已做到，另加波 33 起那句照旧不许写＝她的状态已经迁到新链。未验证面（§五 那 14 条）照旧一条不减，其中第 3 条（收尾全量四数）由下一把的 F 轮补。
