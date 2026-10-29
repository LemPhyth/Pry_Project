# HDS-003 D 类第三片证据刀（波 43／HDS-WAVE43-1029，T3 余五枚换人复采，含 #30 带凭据补跑）——lane 出口报告

- 工单＝`docs/dispatch-hds-wave43-1029.md`；坐标系＝`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS-003；账本＝波 22 审计报告 §四.1 梯队 T3。
- 性质＝证据刀·换人复采。本刀零产品新字节（终态）、零判据字节、零新格、零新增数字门槛、零 UI、零生产接线、零打包；本文件＝主树里本刀唯一新建文件，不提交、不 `git add`。
- 载体＝`.local-notes/qa-wave33-1028/carrier`（detached HEAD `51f149d`，基点 `fd93ebe`）；判据文件＝`tests/Pry.Core.Tests/NarratorPrototypeTests.cs`（一枚字节不写）。
- 产物目录＝`.local-notes/wave43-1029/`（gitignored）：`out/entry-readings.txt`、`out/item1-2-3-prep.txt`、`out/instrument-selfproof.txt`、`out/closure-scan.txt`、`mutation-prep/`、自写脚本 `w43-prep.js`／`w43-apply.js`／`w43-count.js`／`w43-scan.js`。
- 守门人裁决节＝§九，本节号在本报告里保留为空白，本刀不写。

## 〇 入场先数盘（全部当场从盘取，不引用工单或波 42 数字当入场值；原始面＝`out/entry-readings.txt`，取数时刻 2026-10-29 12:29:28）

### 〇.1 主树

- `git rev-parse HEAD`＝`68e6e2645f4c7e8710e4fedae85470c3003fb810`（入场时协作方已把 HEAD 从早前所见 `ae5d702` 推进到该值；本刀所有零行式子都对这枚 HEAD 重测，见 〇.6）。
- `git status --short --branch` 原样逐行（共 9 行）：

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

- 他人资产处置：两枚 `M ` 暂存项、一枚 ` M` 的 `PROJECT_PLAN.md`、五枚 `??` 未跟踪文件＝一字不读不改不提交（上面只是 `status` 文件名面，非内容读取）。
- `git worktree list` 行数＝76。

### 〇.2 载体四件

- `rev-parse HEAD`＝`51f149d0b722e28a6566c93d19f48a94ca4c85df`。
- `status --short` 行数＝0（入场即空，起跑合规）。
- `log --oneline fd93ebe..HEAD` 行数＝2（`51f149d`＋`1e02205`）。
- `rev-parse --show-toplevel`＝`C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave33-1028/carrier`。

### 〇.3 判据文件三面 SHA（起跑门，逐字符等值才准起跑）

`tests/Pry.Core.Tests/NarratorPrototypeTests.cs` 的 SHA-256 三面当场值全等于工单基准：

- `blob(51f149d)`＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`
- 载体工作副本＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`
- 主树工作副本＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`

三面逐字符等值＝起跑门过。

### 〇.4 判据格数当场重取（载体侧现文＝blob，逐字节等值已由 〇.3 证明）

- `[Fact]`＝53；`[Theory]`＝1；`[InlineData`＝3。
- 等式：`total = [Fact] + [Theory]×[InlineData] = 53 + 1×3 = 56`。**本刀定向跑每轮 total 必须等于这枚当场值 56。**
- `Assert.Skip` 计数＝0；判据文件行数（`\n` 计）＝989。

### 〇.5 取数仪器与读数口径（件5／件6）

- 读数口径（件5）＝只数 `<UnitTestResult … outcome="Failed">` 开标签并与同份 `<Counters failed>` 双向对账；`<ResultSummary outcome="Failed">` 不算用例；属性按名字取不按顺序、取回值解 XML 实体（`&quot;` 等）；栈帧锚点＝`in <路径>:line N`（不用 `at `，路径含盘符冒号故切分不用 `[^:]`）；跳过格只认 `outcome="NotExecuted"` 开标签；`testName` 全限定＋参数表，回判方法名先剥括号。仪器＝自写 `w43-count.js`（不沿用波 41／42 实现）。
- 件6 仪器自证＝对工单 §一.5 的 12 批历史面（波 41 trx-a/b/d/e、波 40 trx-M8p/m08/trx-F、波 42 trx-g/h/i/j、波 13 run-full）逐面复算：**12 批面全部 AGREE**（原始输出＝`out/instrument-selfproof.txt`；Failed 开标签与 Counters-failed 双向对账、帧行集合、消息族、total/executed/NotExecuted 求和逐面全等，含 trx-d 附加面"同方法另两参数行同场 Passed"）。12 枚历史定向面的 total 亦逐份读出 56，与 〇.4 当场格数同面。
- 自证过程中仪器错两枚（波 42 同族错，报警≠盘坏，如实登记）：首跑 2 面 MISMATCH，逐面查证后证明坏的是仪器不是盘——①名册互检表漏登 `@437` 键（theory 三参数行同起一行），②`prose:` 面断言式误写（把"仅两处"错拆成两个子条件）。盘面取证值首跑与复跑完全一致；修正仪器后复跑 ALL-OK（`out/closure-scan.txt`）。
- 行号种类全篇标注：**@＝判据文件里格起始行（方法声明行）；: ＝TRX 断言帧行或产品/判据文件里的行**，两种口径不混用不标即违规。
- 行数口径注：`NarrativeDraftValidator.cs`／`NarrativeRoundRunner.cs` 按 `awk NR`／`wc -l` 读作 420／341，按 `split('\n')`（尾随换行符产生末空段）读作 421／342；本刀所有 SHA 对账都按字节，两口径下 SHA 相同，与波 42 的 421／342 记录不构成盘面冲突。
- 锚点 OLD 整行"恰 1 次"取数面更正（如实登记）：入场一次性大单行里用 `grep -cFx` 读出 `V191=0 V240=0 R238=0`（`out/entry-readings.txt` §6），经 `od -c` 逐字节比对证伪＝blob 现文与模式字节全同，0 是该大单行的 shell 引号伪影。权威恰 1 次判定改由 `w43-prep.js` 对 blob 字节逐枚执行（件1，见 §一.1），五枚全部恰 1 次。

### 〇.6 构建跳过三段判定（预算 §四.1，默认合法跳过）

- 段一（0 行式子成立）：`git diff --stat 51f149d 68e6e26 -- src tests scripts Pry.slnx Directory.Build.props` 行数＝**0**（对入场新 HEAD 实测）。
- 段二（成品在盘）：`carrier/src/Pry.Core/bin/Release/net10.0/Pry.Core.dll`（1745408 字节，mtime 10-29 11:58）与 `carrier/tests/Pry.Core.Tests/bin/Release/net10.0/Pry.Core.Tests.dll`（1177088 字节，11:56）在盘。
- 段三（三枚 SHA 三面未变）：判据三面＝`4f1b2f6e…`（〇.3）；`NarrativeDraftValidator.cs` 三面＝`88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c`；`NarrativeRoundRunner.cs` 三面＝`961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9`。
- §一.7 清洁度限定照写：在盘 dll 停在波 42 轮 j 的突变态构建产物（源码字节已回 blob），**只支撑"成品在盘"这一段，不支撑"清洁二进制"**；第一轮定向跑的自带增量编译会按本刀突变态重建并覆盖该产物面。

## 一 前置件成品（件1／件2／件3／件3′；全部零 `dotnet` 当场从盘产出，原始面＝`out/item1-2-3-prep.txt`、`out/closure-scan.txt`）

### 一.0 件1＝改前整行并排自证（五枚，OLD＝工单 §二 现文 vs 载体 blob 现文）

五枚锚点全部按载体 `51f149d` blob 现文当场重取；两产品文件 EOL＝LF（cr-count=0）。逐枚：并排打印逐字符等值断言＝OK，整行（按 `\n` 切分后逐字符等值）出现次数＝恰 1。

| 轮 | 落点（载体 blob） | OLD 现文（逐字符） | 等值断言 | 整行恰 1 次 |
|---|---|---|---|---|
| f′ | `NarrativeDraftValidator.cs:240` | `                else if (!content.Intents.Any(i => string.Equals(i.IntentId, action.DeferredIntentId, StringComparison.Ordinal)))` | OK | 1 |
| g′ | `NarrativeRoundRunner.cs:238` | `        if (draft.StoryVersionBase != request.StoryVersion)` | OK | 1 |
| h′ | `NarrativeDraftValidator.cs:71` | `        if (occurrences == 0)` | OK | 1 |
| i′ | `NarrativeDraftValidator.cs:191` | `        if (!string.Equals(action.TargetConversationId, request.ConversationId, StringComparison.Ordinal))` | OK | 1 |
| j′ | `NarrativeDraftValidator.cs:131` | `        if (string.IsNullOrWhiteSpace(content.NarrativeProse))` | OK | 1 |

（SHA 对账不替代并排自证——上面是并排面；blob 面 SHA：V＝`88ebf5c3…/19338 字节`，R＝`961fb49b…/16768 字节`。）

### 一.1 件2＝突变文本预合成（五枚落 `mutation-prep/`，SHA-256＋字节数当场值；每轮"施加后 SHA"必须逐字符等于该行）

| 轮 | 预合成文件 | 改后行（逐字符） | SHA-256 | 字节数 |
|---|---|---|---|---|
| f′ | `NarrativeDraftValidator-f.cs` | `                else if (false)` | `d652fb3a8acdde68e00fc3d61e57d9448e95379feee0aaf006af944f2bf0d1b0` | 19240 |
| g′ | `NarrativeRoundRunner-g.cs` | `        if (false)` | `fcda7132a66b8d7c7ee844d604ec199d8e406c075976cbdfee0a0dbfd129a0b1` | 16727 |
| h′ | `NarrativeDraftValidator-h.cs` | `        if (false)` | `092e4594a383fbc404693368e9a8b28740aafd71742c9101b1f6629b391f4c63` | 19327 |
| i′ | `NarrativeDraftValidator-i.cs` | `        if (false)` | `27286e4dff4acc1f15c7aa5499ab25a4d7b21d95490d2f13c1ada7ee7497c551` | 19250 |
| j′ | `NarrativeDraftValidator-j.cs` | `        if (false)` | `88c00c05c7cb800bb67546f60985941d0fc87338d3d80067fa4f87ffcc423e87` | 19294 |

旁证（非依据）：本刀自推的 f′ 枚 SHA/字节恰与波 42 记录 `d652fb3a…/19240` 逐字符一致——两双手独立合成同锚同形状得同值，说明预合成方法可复现；g′／h′／i′／j′ 四枚波 42 无同锚记录，纯本刀自值。

### 一.2 件3＝形状自检（五枚全过）

逐枚：被改行净圆括号偏移 0→0、净大括号 0→0；全文件净圆括号收支 0→0（V，`split('\n')` 行数 421→421）、全文件净大括号收支不变；R 同（行数 342→342）。`if (false)`／`else if (false)` 保留原 `if`／`else if` 语法形状，一行对一行，行数不变。

### 一.3 件3′ 点名公约数（闭合表共读面；证据源＝载体 blob 本刀自推，未抄波 42 §一）

盘面事实（`out/closure-scan.txt` 逐条取证）：

- 56 格名册＝判据文件方法声明行（@口径）逐枚解析，与本刀人工点名表互检全对（§〇.5 仪器错已证为仪器面非盘面）。
- 产品调用图（本刀按 blob 现文通读自推）：`NarrativeRoundRunner.RunAsync`（R:90）的门序＝R:95 `assembly.Accepted`（不过⇒ContextRejected 返回）→R:105 `call.Validate()`（NarratorContracts，不过即抛）→R:108 `InvokeOnceAsync`→R:110 非 `DraftProduced`⇒ProviderRejected 返回→R:121 `CheckSnapshotFreshness`（唯一调用点，盘面对证 `:121,236`；三支按序短路 `:238` 故事版本→`:244` 所有权代次→`:250` 签发基线）→R:133 `Apply`（非 Immediate 原样返回）→R:136 `NarrativeDraftValidator.Validate`→R:137 拒绝>0⇒ContractRejected 返回→R:150 即时⇒`ImmediateBranch`（R:183 `BuildCommit`、R:184 `RegisterAppliedCommit`、**R:191 `Substring(binding.ResolvedOffset!.Value, …)`**、R:202 `TryAppend`）→R:155 延迟⇒登记意图返回→R:167 沉默⇒返回。
- `Validate`（V:107）的拒绝序＝:113/:119/:125 身份与会话/批次→**V:131 prose 空白⇒NarrativeProseMissing**→V:137 `CheckConsumption`（:166 感知越界、:173 水位越界、:179 水位倒退）→V:139 空动作⇒ActionBindingMissing→V:145/:147 逐动作 `CheckAction`（V:189 RequireText；**V:191 会话指向不符⇒ActionTargetMismatch，支体内 :194 Detail 插值＝`action.TargetConversationId` 在 V 里的第二处（grep 面 `:191,:194` 恰两处）**；:200 段数越界；:207 `binding = action.Binding ?? Bind(…)`，:208 未绑定⇒其 Failure；:214 即时挂意图⇒IntentClaimedAsFact；Deferred 支 :223/:229/:235（意图 id 为空⇒IntentClaimedAsFact）/**:240 `else if` 意图不在提案⇒:242 IntentClaimedAsFact**；:248 沉默支）→V:150/:152 逐意图 `NarrativeIntentPolicy.Validate`→V:155/:157 知识候选。
- `NarrativeProseBinder.Bind`（V:40）＝:42 空 quote/空 prose⇒ActionBindingMissing 返回；:51-:69 计 `occurrences`、`first` 初值 -1；**V:71 `occurrences == 0`⇒ActionBindingMissing 返回**；V:76 `>1`⇒Ambiguous 返回；V:81-:86 成功返回 `ResolvedOffset = first`、`Failure = None`。`ProseBinding.Bound`（NarrativeDocuments.cs:253）＝`Failure == None && ResolvedOffset.HasValue`。
- 夹具默认面（判据 :40/:82/:89/:102 现文）：`Request()`＝storyVersion 7／ownershipEpoch 1／processedBaseline 100／maxSegments 4／Conversation="room-1"；`Draft()`＝prose=Prose（:22 非空白）、intents 默认空（:111）、storyVersionBase 7、ownershipEpoch 1、receivedBaseline 100、processedThrough 101、perceived=[Input101]、actions=[SilenceAction()]；`ImmediateAction()`＝quote=BoundQuote（"answered him"，在 Prose 中恰现 1 次）、segments 3、target=Conversation；`DeferredAction(intentId)`＝target 默认 Conversation、ProseQuote 为空（record 默认 null）、DeferredIntentId=实参。
- 判据盘面专点：`DeferredAction(` 调用点 4 处（:505/:601/:616/:834）一律传非空 `"intent-1"` ⇒ V:235 恒不执行；`intents:` 非空三处（:505/:602/:834）都含 `PendingIntent()` 缺省 id="intent-1" ⇒ :240 的 `Any` 命中；唯一 `intents` 为空的 Deferred 格＝@614（:616）。`prose:` 显式传参仅 2 处（:661 非空白、:748 `"   "`）。`target:` 非默认仅 1 处（:713 `"room-2"`，@711 格）。`storyVersionBase:` 仅 1 处（:778 `6`，@776 格）。`CheckSnapshotFreshness` 判据零提及；`NarrativeCommitValidator/CommitWriter/SqliteStore` 判据零提及 ⇒ V/R 的调用图进不了别类。

以下 §一.4〜§一.8＝逐轮闭合表：每表 56 行逐枚点名（枚数逐枚点名，不用"各恰N"合并）；"预期"列＝该枚在该轮突变态下的红/不红判定；目标格红格一律注明【断言级红】或【整格异常红】性质与将翻的判据断言行（:口径）。**表落档先于任何 `dotnet`；跑完不回头改预期列。**

### 一.4 f′ 闭合表（V:240 `else if (!content.Intents.Any(…))`→`else if (false)`；见证格＝#30 `Deferred_action_without_a_pointing_intent_is_refused` @614）

预期红集＝**恰 1：@614**。断言帧 :620（`Assert.Equal(NarrativeRoundDisposition.ContractRejected, result.Disposition)`），消息族 `Assert.Equal() Failure` Expected=`ContractRejected`／Actual=`Deferred`，性质＝断言级红。推导：@616 夹具 `DeferredAction("intent-1")` 且 intents 默认空（:111）；R:136 进 Validate→:147 CheckAction→:222 Deferred 支→:223 段 0 不触发、:229 quote 空不触发、:235 id 非空不进⇒**:240 改前 `!Any`=true→:242 加 IntentClaimedAsFact**；改后 `else if (false)` ⇒ 0 拒绝⇒R:137 不拦→R:155 延迟支⇒Disposition=Deferred、Rejections 空。@621/:622 同格后随断言不再执行（xUnit 首失败即止），首翻点 :620。

| # | 格 | 方法名 | 预期 | 逐枚理由（可达路径级） |
|---|---|---|---|---|
| 1 | @183 | Compiler_emits_the_eight_assembly_units_of_the_disk_builder_in_its_order | 不红 | 体内仅 `NarrativeContextCompiler.Compile`＋装配面断言，无 `RunAsync(`/无 validator 引用⇒V:240 不在调用图（:385 起才有 RunAsync，判据盘面 §一.3）。 |
| 2 | @198 | Compiler_keeps_absent_sections_as_not_supplied_instead_of_dropping_them | 不红 | 同 @183 类：Compile-only，不入 CheckAction。 |
| 3 | @210 | Compiler_drops_an_optional_section_that_exceeds_its_own_share_and_counts_it_exactly | 不红 | Compile-only，不触及 V 文件。 |
| 4 | @225 | Compiler_refuses_the_round_when_the_mandatory_prelude_does_not_fit | 不红 | Compile-only，断言装配拒绝码 MandatoryExceedsBudget，与 :240 无通路。 |
| 5 | @237 | Compiler_refuses_a_window_that_leaves_no_positive_input_budget | 不红 | Compile-only（WindowTooSmall 面）。 |
| 6 | @247 | Compiler_refuses_a_scene_without_identity_instead_of_inventing_one | 不红 | Compile-only（MandatoryContentMissing 面）。 |
| 7 | @256 | Compiler_refuses_a_current_event_from_another_conversation | 不红 | Compile-only；其 "room-2" 在 Payloads 当前事件面（:259），非动作 target，不经 CheckAction。 |
| 8 | @266 | Compiler_fails_loudly_when_the_reserved_directive_does_not_fit | 不红 | Compile-only（ReservedDirectiveDoesNotFit 面）。 |
| 9 | @281 | Compiler_trims_recent_messages_only_at_whole_unit_boundaries | 不红 | Compile-only（unit_budget 段面）。 |
| 10 | @298 | Compiler_removes_cross_conversation_units_and_names_the_count | 不红 | Compile-only（cross_scope 段面；其 "room-2" 在 recent 单元面 :300）。 |
| 11 | @313 | Compiler_shrinks_the_memory_wallet_unit_by_unit | 不红 | Compile-only（Memory 钱包面）。 |
| 12 | @328 | Compiler_honours_the_unified_budget_inequality_with_exact_numbers | 不红 | Compile-only（预算不等式面）。 |
| 13 | @347 | Compiler_rejects_a_negative_section_share | 不红 | Compile-only（InvalidBudget 面）。 |
| 14 | @357 | Compiler_rejects_a_window_of_zero_tokens | 不红 | Compile-only（InvalidBudget 面）。 |
| 15 | @367 | Compiler_carries_source_references_into_every_emitted_section | 不红 | Compile-only（EmittedSourceRefs 面）。 |
| 16 | @381 | Round_calls_the_provider_exactly_once_for_one_narration | 不红 | 达 R:136 Validate，但动作＝Draft() 默认 `[SilenceAction()]`（:110）⇒:197 switch 走 :248 沉默支，:222 Deferred 支不进⇒:240 不求值；:387-:390 断言面不变。 |
| 17 | @394 | Round_does_not_call_the_provider_when_the_context_is_rejected | 不红 | R:95 `assembly.Accepted=false` 即返 ContextRejected，R:108 之后根本不执行⇒R:136/:240 不发生。 |
| 18 | @408 | Second_call_on_the_same_round_budget_is_refused | 不红 | 体内仅 `NarratorCallBudget` 两次 Consume＋异常形状（:410-:416），无 RunAsync/无 validator⇒不到 :240。 |
| 19 | @420 | Call_refuses_an_assembly_that_still_carries_a_rejection | 不红 | 直调 `NarratorCall.Validate()`（NarratorContracts.cs），不经 NarrativeDraftValidator⇒:240 不求值。 |
| 20 | @437[instance] | Call_refuses_an_assembly_that_belongs_to_another_scope | 不红 | 同上：`NarratorCall.Validate()` 作用域面（InstanceId 参改），无 RunAsync。 |
| 21 | @437[conversation] | Call_refuses_an_assembly_that_belongs_to_another_scope | 不红 | 同上（ConversationId="room-2" 参改在装配体面 :443，不经 CheckAction 的 action 目标判定）。 |
| 22 | @437[batch] | Call_refuses_an_assembly_that_belongs_to_another_scope | 不红 | 同上（BatchId 参改，CallScopeMismatch 面）。 |
| 23 | @456 | Round_never_rewrites_the_narration_after_the_single_call | 不红 | 动作＝`[ImmediateAction()]`（:458）⇒Immediate 支（:199-:220），:240 属 Deferred 支不求值；Immediate 走通投递，:462-:467 不变。 |
| 24 | @471 | Round_feeds_the_provider_the_compiled_context_and_nothing_else | 不红 | Draft() 默认沉默动作（:473）⇒:248 支；断言读 LastCall 装配面，与 :240 无涉。 |
| 25 | @488 | Silence_round_produces_no_assistant_message_and_no_intent | 不红 | `[SilenceAction()]`（:490）⇒:248 支，:240 不求值。 |
| 26 | @503 | Deferred_round_registers_the_intent_without_touching_real_history | 不红 | 达 :240（:505 DeferredAction("intent-1")＋intents=[PendingIntent()] 命中 "intent-1"）⇒**改前 `!Any`=false、改后 false，两态值同**；0 拒绝⇒R:155 延迟支不变，:509-:516 不翻。 |
| 27 | @520 | Draft_without_any_action_is_rejected_rather_than_read_as_silence | 不红 | 达 R:136 但 `actions=[]`（:522）⇒V:145 循环空转，:240 不求值；唯一拒绝仍来自 :139，:526-:529 不变。 |
| 28 | @534 | Malformed_response_is_not_masquerading_as_a_chosen_silence | 不红 | R:110 非 DraftProduced（Malformed）即返 ProviderRejected，R:136 不发生。 |
| 29 | @549 | A_provider_that_ignores_cancellation_still_exits_as_timeout | 不红 | 同 @534 类：R:110 短路（TimedOut）。 |
| 30 | @562 | A_provider_that_honours_the_timeout_token_exits_as_timeout_too | 不红 | 同 @534 类：R:110 短路（TimedOut）。 |
| 31 | @574 | Provider_failure_becomes_a_named_error_shape_and_writes_nothing | 不红 | R:110 短路（ProviderFailed）。 |
| 32 | @587 | Host_cancellation_stays_cancellation_and_is_not_converted_into_a_round | 不红 | R:108 `InvokeOnceAsync` 即抛 OperationCanceledException（:593 格界捕获），R:121/:136 之后不发生。 |
| 33 | @599 | Deferred_intent_that_claims_to_be_fulfilled_without_delivery_is_refused | 不红 | 达 :240（:601 DeferredAction＋:602 intents 含 id="intent-1"）⇒改前/后均 false；其唯一拒绝出自 V:152 `NarrativeIntentPolicy`（RealityContamination），非 :240/:242，:606-:610 不变。 |
| 34 | @614 | Deferred_action_without_a_pointing_intent_is_refused | **红（恰此一枚·断言级）** | 表头推导：:616 夹具 intents 空⇒:240 改前 true（IntentClaimedAsFact）→改后 false⇒Disposition ContractRejected→Deferred；首翻 :620，帧 :620，消息 Expected ContractRejected/Actual Deferred。 |
| 35 | @628 | Immediate_action_binds_to_the_unique_slice_and_lands_three_segments | 不红 | 动作 Immediate（:630）⇒不进 Deferred 支，:240 不求值；Immediate 全链（:200/:207/:214 均不触发）不变。 |
| 36 | @644 | Quote_absent_from_this_narration_rejects_the_action_without_host_invention | 不红 | Immediate（:646）⇒:207 `Bind` 失败 ActionBindingMissing 照由 :208 拒绝（本轮没改 :71），Deferred 支不进⇒:240 不求值。 |
| 37 | @658 | Ambiguous_quote_is_rejected_instead_of_the_host_choosing_one | 不红 | Immediate（:661）⇒:207 Bind 二现⇒Ambiguous 拒绝（:208）不变；:240 不求值。 |
| 38 | @671 | Immediate_action_over_the_host_segment_ceiling_is_refused | 不红 | Immediate（:673 segments:6）⇒:200 段数越界拒绝不变；Deferred 支不进。 |
| 39 | @684 | An_over_split_slice_is_refused_by_the_existing_history_gate | 不红 | Immediate（:686）validator 面 0 拒绝⇒HistoryRejected 出自账本 TryAppend（R:202/:209），:240 不求值。 |
| 40 | @698 | Immediate_action_carrying_a_future_intent_is_refused | 不红 | Immediate 挂 DeferredIntentId="intent-9"（:700）⇒拒绝出自 :214-:217（即时挂意图），本轮未改；:240 属 Deferred 支不求值。 |
| 41 | @711 | Action_targeting_another_conversation_is_refused | 不红 | Immediate＋target "room-2"（:713）⇒拒绝出自 :191-:195（ActionTargetMismatch），本轮未改；Deferred 支不进。 |
| 42 | @722 | Perceived_reference_outside_the_batch_is_refused | 不红 | 动作 Silence（:724）；拒绝出自 V:166-:170（PerceivedInputUnknown），:240 不求值。 |
| 43 | @735 | Watermark_beyond_the_batch_upper_bound_is_refused | 不红 | 动作 Silence（:737）；拒绝出自 V:173-:176（ProcessedBeyondBatch）。 |
| 44 | @746 | Round_refuses_a_narration_without_prose_instead_of_binding_to_nothing | 不红 | 动作 Immediate（:748）⇒拒绝出自 :131＋:207（prose "   " Bind 零现⇒ActionBindingMissing），两条本轮均未改；Deferred 支不进⇒:240 不求值，:752-:754 不变。 |
| 45 | @761 | Bound_action_keeps_a_reference_back_to_the_input_it_answers | 不红 | Immediate（:763）⇒:240 不求值；绑定偏移断言 :770 读 Binder 成功面（:81-:86），未改。 |
| 46 | @776 | Draft_on_a_stale_story_version_is_refused_before_any_history | 不红 | R:121 之 :238 命中（base 6≠7）即返，**R:133/:136 根本不发生**⇒:240 不求值；:782-:786 断言的 StoryVersionConflict 出自 R 文件未改支。 |
| 47 | @790 | Draft_from_a_fenced_writer_epoch_is_refused | 不红 | R:121 之 :244 命中即返（:238 处 base 7==7 本就不进），不到 R:136⇒:240 不求值。 |
| 48 | @802 | Draft_with_a_wrong_issue_baseline_is_refused_as_late | 不红 | R:121 之 :250 命中即返，不到 R:136⇒:240 不求值。 |
| 49 | @813 | Contiguous_split_reproduces_the_bound_slice_verbatim | 不红 | 仅调静态 `NarrativeRoundRunner.SplitContiguous`（:815-:820），不进 RunAsync/CheckAction⇒:240 不求值。 |
| 50 | @832 | Mixed_round_takes_the_immediate_branch_and_registers_no_intent_there | 不红 | 达 :240（:834 DeferredAction("intent-1")＋intents=[PendingIntent()]）⇒改前/后均 false；混合轮走 Immediate 支的结果不变，:838-:843 不翻。 |
| 51 | @851 | Provider_side_cancellation_with_a_live_host_token_is_named_timeout_not_failure | 不红 | SelfCancellingNarrator（:857）⇒R:110 短路（TimedOut），无草稿进 :136。 |
| 52 | @890 | Unified_budget_inequality_is_pinned_at_the_edge_and_one_token_past_it | 不红 | 纯记录面（手工构 `NarrativeContextAssembly` 断言 WithinUnifiedBudget），无 RunAsync/无 validator。 |
| 53 | @913 | Whole_section_segments_carry_their_own_source_reference_even_when_dropped | 不红 | Compile-only（段来源面）。 |
| 54 | @934 | Absent_sections_are_named_not_supplied_rather_than_budget | 不红 | Compile-only（not_supplied 理由串面）。 |
| 55 | @952 | Unit_run_losing_units_to_both_reasons_names_both_together | 不红 | Compile-only（cross_scope_and_unit_budget 面）；其 "room-2" 在 recent 单元（:954），不经 CheckAction。 |
| 56 | @975 | Unit_run_that_fits_no_unit_at_all_is_dropped_and_named_budget | 不红 | Compile-only（budget 理由串面）。 |

f′ 表合计：红 1（@614）＋不红 55＝56；与名册逐枚一致（`out/closure-scan.txt` §D 转写自证）。

### 一.5 g′ 闭合表（R:238 `if (draft.StoryVersionBase != request.StoryVersion)`→`if (false)`；见证格＝#42 `Draft_on_a_stale_story_version_is_refused_before_any_history` @776）

预期红集＝**恰 1：@776**。断言帧 :782（`Assert.Equal(ContractRejected, result.Disposition)`），消息族 `Assert.Equal() Failure` Expected=`ContractRejected`／Actual=`Immediate`，性质＝断言级红。推导：@778 `storyVersionBase: 6` vs `Request(storyVersion: 7)`（:780，默认也是 7）⇒改前 :238 true→返 StoryVersionConflict 单拒绝；改后第一支摘除⇒`:244`（draft epoch 1==1 false）、`:250`（receivedBaseline 100==request 基线 100 false）⇒freshness 返 null→R:133 Apply（ImmediateAction 绑定成功）→R:136 Validate 0 拒绝（:131 prose=Prose 非空白、:191 target 默认、:200 段 3≤4、:207 绑定 Bound、:214 无意图）→R:150 ImmediateBranch：R:191 `Substring(ResolvedOffset=23（Prose.IndexOf(BoundQuote)，HasValue 且≥0）, 12)` 合法、SplitContiguous 3 段、TryAppend 以本道新签 writer 放行（同 @628 干净通过面）⇒Disposition=Immediate、Rejections 空。首翻点 :782（xUnit 首败即止，:783-:786 不再执行）。

| # | 格 | 方法名 | 预期 | 逐枚理由 |
|---|---|---|---|---|
| 1 | @183 | Compiler_emits_the_eight_assembly_units_of_the_disk_builder_in_its_order | 不红 | Compile-only，体内无 `RunAsync(`（:238 在 RunAsync→CheckSnapshotFreshness 实例路径内）⇒不可达。 |
| 2 | @198 | Compiler_keeps_absent_sections_as_not_supplied_instead_of_dropping_them | 不红 | 同上：不入 RunAsync。 |
| 3 | @210 | Compiler_drops_an_optional_section… | 不红 | 同上。 |
| 4 | @225 | Compiler_refuses_the_round_when_the_mandatory_prelude_does_not_fit | 不红 | 同上。 |
| 5 | @237 | Compiler_refuses_a_window_that_leaves_no_positive_input_budget | 不红 | 同上。 |
| 6 | @247 | Compiler_refuses_a_scene_without_identity_instead_of_inventing_one | 不红 | 同上。 |
| 7 | @256 | Compiler_refuses_a_current_event_from_another_conversation | 不红 | 同上。 |
| 8 | @266 | Compiler_fails_loudly_when_the_reserved_directive_does_not_fit | 不红 | 同上。 |
| 9 | @281 | Compiler_trims_recent_messages_only_at_whole_unit_boundaries | 不红 | 同上。 |
| 10 | @298 | Compiler_removes_cross_conversation_units_and_names_the_count | 不红 | 同上。 |
| 11 | @313 | Compiler_shrinks_the_memory_wallet_unit_by_unit | 不红 | 同上。 |
| 12 | @328 | Compiler_honours_the_unified_budget_inequality_with_exact_numbers | 不红 | 同上。 |
| 13 | @347 | Compiler_rejects_a_negative_section_share | 不红 | 同上。 |
| 14 | @357 | Compiler_rejects_a_window_of_zero_tokens | 不红 | 同上。 |
| 15 | @367 | Compiler_carries_source_references_into_every_emitted_section | 不红 | 同上。 |
| 16 | @381 | Round_calls_the_provider_exactly_once_for_one_narration | 不红 | 达 R:121 :238 处：draft base 默认 7（:103）==Request 7（:40）⇒改前 false、改后 false，值同短路续 :244/:250 亦等值（1==1、100==100）⇒返 null 照常，Silence 结果不变。 |
| 17 | @394 | Round_does_not_call_the_provider_when_the_context_is_rejected | 不红 | R:95 早于 R:121 短路返回⇒:238 不求值。 |
| 18 | @408 | Second_call_on_the_same_round_budget_is_refused | 不红 | 无 RunAsync 调用面，不可达 :238。 |
| 19 | @420 | Call_refuses_an_assembly_that_still_carries_a_rejection | 不红 | 同上（NarratorCall.Validate 面）。 |
| 20 | @437[instance] | Call_refuses_an_assembly_that_belongs_to_another_scope | 不红 | 同上。 |
| 21 | @437[conversation] | Call_refuses_an_assembly_that_belongs_to_another_scope | 不红 | 同上。 |
| 22 | @437[batch] | Call_refuses_an_assembly_that_belongs_to_another_scope | 不红 | 同上。 |
| 23 | @456 | Round_never_rewrites_the_narration_after_the_single_call | 不红 | 达 :238：7==7⇒false/false，后续 Validate/Immediate 支全链不变（:462-:467 同改前面）。 |
| 24 | @471 | Round_feeds_the_provider_the_compiled_context_and_nothing_else | 不红 | :238 false/false，断言读 LastCall 装配面，不变。 |
| 25 | @488 | Silence_round_produces_no_assistant_message_and_no_intent | 不红 | :238 false/false，沉默支不变。 |
| 26 | @503 | Deferred_round_registers_the_intent_without_touching_real_history | 不红 | :238 false/false，延迟支登记不变。 |
| 27 | @520 | Draft_without_any_action_is_rejected_rather_than_read_as_silence | 不红 | :238 false/false（freshness 过）⇒拒绝仍出自 V:139，:526-:529 不翻。 |
| 28 | @534 | Malformed_response_is_not_masquerading_as_a_chosen_silence | 不红 | R:110 早于 R:121 短路⇒:238 不求值。 |
| 29 | @549 | A_provider_that_ignores_cancellation_still_exits_as_timeout | 不红 | 同上（R:110）。 |
| 30 | @562 | A_provider_that_honours_the_timeout_token_exits_as_timeout_too | 不红 | 同上（R:110）。 |
| 31 | @574 | Provider_failure_becomes_a_named_error_shape_and_writes_nothing | 不红 | 同上（R:110）。 |
| 32 | @587 | Host_cancellation_stays_cancellation_and_is_not_converted_into_a_round | 不红 | R:108 抛取消，R:121 不发生⇒:238 不求值。 |
| 33 | @599 | Deferred_intent_that_claims_to_be_fulfilled_without_delivery_is_refused | 不红 | :238 false/false⇒进 :136，RealityContamination 拒绝面在 V（本轮没改 V），:606-:610 不变。 |
| 34 | @614 | Deferred_action_without_a_pointing_intent_is_refused | 不红 | :238 false/false⇒进 :136，IntentClaimedAsFact 拒绝出自 V:240（本轮没改 V），:620-:622 不变。 |
| 35 | @628 | Immediate_action_binds_to_the_unique_slice_and_lands_three_segments | 不红 | :238 false/false⇒Immediate 全链与改前逐字节同路径。 |
| 36 | @644 | Quote_absent… | 不红 | :238 false/false⇒V:207 绑定失败拒绝不变。 |
| 37 | @658 | Ambiguous_quote… | 不红 | :238 false/false⇒Ambiguous 拒绝不变。 |
| 38 | @671 | Immediate_action_over_the_host_segment_ceiling_is_refused | 不红 | :238 false/false⇒V:200 段数拒绝不变。 |
| 39 | @684 | An_over_split_slice_is_refused_by_the_existing_history_gate | 不红 | :238 false/false⇒账本 HistoryRejected 面不变。 |
| 40 | @698 | Immediate_action_carrying_a_future_intent_is_refused | 不红 | :238 false/false⇒V:214 拒绝不变。 |
| 41 | @711 | Action_targeting_another_conversation_is_refused | 不红 | :238 false/false⇒V:191 拒绝不变。 |
| 42 | @722 | Perceived_reference_outside_the_batch_is_refused | 不红 | :238 false/false⇒V:166 拒绝不变。 |
| 43 | @735 | Watermark_beyond_the_batch_upper_bound_is_refused | 不红 | :238 false/false⇒V:173 拒绝不变。 |
| 44 | @746 | Round_refuses_a_narration_without_prose… | 不红 | :238 false/false⇒V:131/V:207 双拒绝（计数 2）不变，:752-:754 不翻。 |
| 45 | @761 | Bound_action_keeps_a_reference_back… | 不红 | :238 false/false⇒Immediate 绑定回指面不变。 |
| 46 | @776 | Draft_on_a_stale_story_version_is_refused_before_any_history | **红（恰此一枚·断言级）** | 表头推导：:238 改前 true（6≠7）→改后 false⇒三支全落空→全链走到 Immediate 投递⇒首翻 :782，帧 :782，Expected ContractRejected/Actual Immediate。 |
| 47 | @790 | Draft_from_a_fenced_writer_epoch_is_refused | 不红 | 达 :238：base 默认 7==7⇒改前改后均 false，都落到 :244（1≠2）⇒OwnershipFenced 单拒绝两态同值，:796/:798/:799 不翻。 |
| 48 | @802 | Draft_with_a_wrong_issue_baseline_is_refused_as_late | 不红 | 达 :238：7==7 false/false⇒:244（1==1 false）⇒:250（90≠100 true）⇒LateCommitRejected 单拒绝两态同值，:808/:809 不翻。 |
| 49 | @813 | Contiguous_split_reproduces_the_bound_slice_verbatim | 不红 | 静态 SplitContiguous 独立方法，不经 RunAsync/CheckSnapshotFreshness⇒:238 不求值。 |
| 50 | @832 | Mixed_round_takes_the_immediate_branch… | 不红 | :238 false/false（两动作均默认版本面）⇒混合 Immediate 支结果不变。 |
| 51 | @851 | Provider_side_cancellation_with_a_live_host_token… | 不红 | R:110 短路（provider 自取消⇒TimedOut），无草稿⇒R:121 不发生。 |
| 52 | @890 | Unified_budget_inequality… | 不红 | 记录面，无 RunAsync。 |
| 53 | @913 | Whole_section_segments… | 不红 | Compile-only。 |
| 54 | @934 | Absent_sections… | 不红 | Compile-only。 |
| 55 | @952 | Unit_run_losing_units… | 不红 | Compile-only。 |
| 56 | @975 | Unit_run_that_fits_no_unit… | 不红 | Compile-only。 |

g′ 表合计：红 1（@776）＋不红 55＝56。

### 一.6 h′＝件3′ 退回轮（V:71 `if (occurrences == 0)`→`if (false)`）——闭合不了，不起轮、定向额度原样退回，本表＝判不清面逐枚登记（非起跑闭合表）

本刀按载体 blob 自推的结果是**同场两枚受害**，与判定门"恰 1"不可调和，故按工单 §三 件3′／波 41 c 轮先例退回。推导链（逐字节面）：

- **@644＝整格异常红**：夹具 `ImmediateAction(quote: "he replied about the studio visit")`（:646）在 Prose 中 0 现；R:133 Apply→V:40 Bind：:42 非空跳过、循环计得 occurrences=0、first=-1 ⇒ 改后 :71 摘除不落 return、:76（0>1 false）不落 ⇒ 直落 **:81-:86 成功返回**：`ResolvedOffset = first = -1`（`int?` 的 HasValue=true）、`Failure = None` ⇒ NarrativeDocuments.cs:253 `Bound=true`⇒V:208 不拒绝⇒该草稿 0 拒绝（:131/:191/:200/:214 均不触发，:139 有动作）⇒R:150 ImmediateBranch⇒**R:191 `commit.NarrativeProse.Substring(-1, 35)` 抛 ArgumentOutOfRangeException**，异常穿出 RunAsync，@644 整格以异常红收场（测试帧 :648）。
- **@746＝断言级红**：夹具 prose `"   "`（:748）＋默认 quote ⇒ :131 仍 true（本轮未改 :131）给 NarrativeProseMissing；:207 Bind 同走 :81-:86 成功面（Bound=true）⇒第二条 ActionBindingMissing 消失⇒`Rejections.Count` 2→1⇒**:752 `Assert.Equal(2, result.Rejections.Count)` 当场翻红**（消息族 `Assert.Equal() Expected: 2 / Actual: 1`）。
- 其余 Immediate 绑定格不受害的原因＝其 quote 在 prose 中现 1 次或 2 次（first≥0 或 :76 先落），见下表逐枚。

| # | 格 | 方法名 | 判定 | 逐枚理由 |
|---|---|---|---|---|
| 1 | @183 | Compiler_emits_the_eight_assembly_units_of_the_disk_builder_in_its_order | 不红 | Compile-only，无 RunAsync/Binder 通路。 |
| 2 | @198 | Compiler_keeps_absent_sections… | 不红 | 同上。 |
| 3 | @210 | Compiler_drops_an_optional_section… | 不红 | 同上。 |
| 4 | @225 | Compiler_refuses_the_round… | 不红 | 同上。 |
| 5 | @237 | Compiler_refuses_a_window… | 不红 | 同上。 |
| 6 | @247 | Compiler_refuses_a_scene… | 不红 | 同上。 |
| 7 | @256 | Compiler_refuses_a_current_event… | 不红 | 同上。 |
| 8 | @266 | Compiler_fails_loudly… | 不红 | 同上。 |
| 9 | @281 | Compiler_trims_recent… | 不红 | 同上。 |
| 10 | @298 | Compiler_removes_cross… | 不红 | 同上。 |
| 11 | @313 | Compiler_shrinks… | 不红 | 同上。 |
| 12 | @328 | Compiler_honours… | 不红 | 同上。 |
| 13 | @347 | Compiler_rejects_a_negative… | 不红 | 同上。 |
| 14 | @357 | Compiler_rejects_a_window_of_zero… | 不红 | 同上。 |
| 15 | @367 | Compiler_carries_source… | 不红 | 同上。 |
| 16 | @381 | Round_calls_the_provider_exactly_once… | 不红 | 动作 Silence⇒Apply 在 V:92 原样返回、Validate 不进 :207（无 Immediate 动作）⇒:71 不求值。 |
| 17 | @394 | Round_does_not_call_the_provider… | 不红 | R:95 短路，R:133 不发生。 |
| 18 | @408 | Second_call_on_the_same_round_budget… | 不红 | 无 runner/Binder 通路。 |
| 19 | @420 | Call_refuses_an_assembly… | 不红 | 同上。 |
| 20 | @437[instance] | Call_refuses_an_assembly_that_belongs_to_another_scope | 不红 | 同上。 |
| 21 | @437[conversation] | Call_refuses_an_assembly_that_belongs_to_another_scope | 不红 | 同上。 |
| 22 | @437[batch] | Call_refuses_an_assembly_that_belongs_to_another_scope | 不红 | 同上。 |
| 23 | @456 | Round_never_rewrites_the_narration… | 不红 | quote=BoundQuote 在 Prose 现 1 次⇒occurrences=1，:71 两态均 false（改后走 :81 面与改前成功面同值同偏移）⇒断言不翻。 |
| 24 | @471 | Round_feeds_the_provider… | 不红 | 动作 Silence⇒:71 不求值。 |
| 25 | @488 | Silence_round… | 不红 | 同上。 |
| 26 | @503 | Deferred_round… | 不红 | 动作 Deferred⇒V:92/V:207 都不跑 Bind⇒:71 不求值。 |
| 27 | @520 | Draft_without_any_action… | 不红 | 无动作⇒不跑 Bind；拒绝仍出 :139。 |
| 28 | @534 | Malformed_response… | 不红 | R:110 短路，R:133 不发生。 |
| 29 | @549 | A_provider_that_ignores_cancellation… | 不红 | 同上。 |
| 30 | @562 | A_provider_that_honours… | 不红 | 同上。 |
| 31 | @574 | Provider_failure… | 不红 | 同上。 |
| 32 | @587 | Host_cancellation… | 不红 | R:108 抛出，R:133 不发生。 |
| 33 | @599 | Deferred_intent_that_claims… | 不红 | Deferred 动作⇒不跑 Bind；:152 拒绝面不变。 |
| 34 | @614 | Deferred_action_without_a_pointing_intent_is_refused | 不红 | Deferred⇒不跑 Bind；:240 拒绝（本轮未改）照给⇒ContractRejected/Single/IntentClaimedAsFact :620-:622 不翻。**此格与 @644/@746 不同：它的红依赖 :240，不是 :71。** |
| 35 | @628 | Immediate_action_binds_to_the_unique_slice… | 不红 | occurrences=1⇒两态同走成功面（first≥0），投递结果不变。 |
| 36 | @644 | Quote_absent_from_this_narration… | **红（之一·整格异常红）** | 上链：Bind 漏到 :81 面（ResolvedOffset=-1、Bound=true）→0 拒绝→R:191 `Substring(-1,35)` 抛 ArgumentOutOfRangeException，测试帧 :648。 |
| 37 | @658 | Ambiguous_quote… | 不红 | occurrences=2⇒:71 两态 false（2≠0）、:76 true 照返 Ambiguous⇒:665/:666/:667 不翻。 |
| 38 | @671 | Immediate_action_over_the_host_segment_ceiling… | 不红 | quote 默认 1 现⇒Bind 不变；:200 段数拒绝照给（Validate 对 :207 的求值在改后返 Bound=true，但 :200 先已入拒绝；**见下行注**）。 |
| 39 | @684 | An_over_split_slice… | 不红 | quote="him" 在 Prose 现 1 次（"answered him" 尾）⇒occurrences=1⇒Bind 两态同值；validator 0 拒绝照进 Immediate 支，R:191 偏移 30≥0 合法，账本 SegmentStickerPayloadInvalid 面不变。 |
| 40 | @698 | Immediate_action_carrying_a_future_intent… | 不红 | quote 默认 1 现⇒Bind 不变；:214 拒绝照给。 |
| 41 | @711 | Action_targeting_another_conversation_is_refused | 不红 | quote 默认 1 现⇒Bind 不变；:191 拒绝照给（本轮未改 :191）。 |
| 42 | @722 | Perceived_reference_outside_the_batch… | 不红 | Silence⇒不跑 Bind；:166 拒绝不变。 |
| 43 | @735 | Watermark_beyond… | 不红 | Silence⇒不跑 Bind；:173 拒绝不变。 |
| 44 | @746 | Round_refuses_a_narration_without_prose… | **红（之二·断言级）** | 上链：:131 拒绝留、:207 绑定拒绝失（Bind 返 Bound=true）⇒Rejections 2→1⇒:752 翻红，帧 :752。 |
| 45 | @761 | Bound_action_keeps_a_reference_back… | 不红 | quote 默认 1 现⇒Bind 同值；:770 断言 ResolvedOffset=IndexOf=23 两态同。 |
| 46 | @776 | Draft_on_a_stale_story_version… | 不红 | R:121 即返（本轮未改 R），R:133 不发生⇒:71 不求值。 |
| 47 | @790 | Draft_from_a_fenced_writer_epoch… | 不红 | 同上（:244 命中即返）。 |
| 48 | @802 | Draft_with_a_wrong_issue_baseline… | 不红 | 同上（:250 命中即返）。 |
| 49 | @813 | Contiguous_split… | 不红 | 静态方法不经 Binder。 |
| 50 | @832 | Mixed_round… | 不红 | Immediate 动作 quote 默认 1 现⇒Bind 同值；Deferred 动作不跑 Bind；混合结果不变。 |
| 51 | @851 | Provider_side_cancellation… | 不红 | R:110 短路，无草稿。 |
| 52 | @890 | Unified_budget_inequality… | 不红 | 记录面，无 Binder 通路。 |
| 53 | @913 | Whole_section_segments… | 不红 | Compile-only。 |
| 54 | @934 | Absent_sections… | 不红 | Compile-only。 |
| 55 | @952 | Unit_run_losing_units… | 不红 | Compile-only。 |
| 56 | @975 | Unit_run_that_fits_no_unit… | 不红 | Compile-only。 |

h′ 判定：预期同场红＝**@644（整格异常红）＋@746（断言级红）＝2 枚 ≠ 恰 1**⇒该轮按件3′**退回不起**，定向额度原样交回，上表即判不清面逐枚登记。注：@671 行需一句拆清——改后其拒绝序列仍恰 1 条（:200 段数越界先入，:207 绑定支改后返 Bound 不再入第二条；改前 :207 绑定成功也不入），两态 `Rejections` 均＝[ActionSegmentCountInvalid]⇒Single/理由断言 :677-:679 不翻。注：@684 的 quote "him" 于 Prose `IndexOf=30` 恰 1 现（`answered him` 尾段），本刀已按 :56 循环语义自推复核。

### 一.7 i′ 闭合表（V:191 `if (!string.Equals(action.TargetConversationId, request.ConversationId, …))`→`if (false)`；见证格＝#37 `Action_targeting_another_conversation_is_refused` @711）

预期红集＝**恰 1：@711**。断言帧 :717（`Assert.Single(result.Rejections)`），消息族 `Assert.Single() Failure: The collection was empty`（与波 40/41/42 同族，按 `out/instrument-selfproof.txt` 面校准），性质＝断言级红。推导：:713 `ImmediateAction(target: "room-2")`≠Request 会话 "room-1"⇒改前 :191 true→ActionTargetMismatch 单拒绝；改后条件恒 false⇒:192-:195 支体（含 **第二处 `action.TargetConversationId` 的 Detail 插值行 :194**）运行时永不执行——工单点名的"另一处"就在这一支体内，**改后同场不被读到**（静态可达＝同一 if 体；动态可达＝恒 false 不再进入；编译面＝`if (false)` 支体在 C# 里仅产 CS0162 警告级，载体 `Directory.Build.props` 盘上现文 :1-9 无 TreatWarningsAsErrors，警告不致构建失败）。零拒绝后全链：R:133 绑定成功（默认 quote 1 现）→R:137 不拦→R:150 ImmediateBranch 干净投递（同 @628 面）⇒Disposition=Immediate、Rejections 空⇒:717 首翻（:718 不再执行）。

| # | 格 | 方法名 | 预期 | 逐枚理由 |
|---|---|---|---|---|
| 1 | @183 | Compiler_emits_the_eight_assembly_units… | 不红 | Compile-only，无 RunAsync/Validate 通路⇒:191 不求值。 |
| 2 | @198 | Compiler_keeps_absent_sections… | 不红 | 同上。 |
| 3 | @210 | Compiler_drops_an_optional_section… | 不红 | 同上。 |
| 4 | @225 | Compiler_refuses_the_round… | 不红 | 同上。 |
| 5 | @237 | Compiler_refuses_a_window… | 不红 | 同上。 |
| 6 | @247 | Compiler_refuses_a_scene… | 不红 | 同上。 |
| 7 | @256 | Compiler_refuses_a_current_event… | 不红 | 同上；其 "room-2"（:259）在装配 Payloads 面，不经 CheckAction 的 :191。 |
| 8 | @266 | Compiler_fails_loudly… | 不红 | Compile-only。 |
| 9 | @281 | Compiler_trims_recent… | 不红 | 同上。 |
| 10 | @298 | Compiler_removes_cross… | 不红 | 同上；"room-2"（:300）在 recent 单元面。 |
| 11 | @313 | Compiler_shrinks… | 不红 | 同上。 |
| 12 | @328 | Compiler_honours… | 不红 | 同上。 |
| 13 | @347 | Compiler_rejects_a_negative… | 不红 | 同上。 |
| 14 | @357 | Compiler_rejects_a_window_of_zero… | 不红 | 同上。 |
| 15 | @367 | Compiler_carries_source… | 不红 | 同上。 |
| 16 | @381 | Round_calls_the_provider_exactly_once… | 不红 | 达 :191（Silence 动作也进 CheckAction 循环），`SilenceAction` 的 target＝:78 默认 Conversation="room-1"==request⇒改前/后条件恒 false，:194 支体两态均不执行⇒结果不变。 |
| 17 | @394 | Round_does_not_call_the_provider… | 不红 | R:95 短路⇒R:136 不发生⇒:191 不求值。 |
| 18 | @408 | Second_call_on_the_same_round_budget… | 不红 | 不经 validator。 |
| 19 | @420 | Call_refuses_an_assembly… | 不红 | 同上。 |
| 20 | @437[instance] | Call_refuses_an_assembly_that_belongs_to_another_scope | 不红 | 同上（装配作用域面不经 CheckAction）。 |
| 21 | @437[conversation] | Call_refuses_an_assembly_that_belongs_to_another_scope | 不红 | 同上；"room-2"（:443）在装配体作用域面。 |
| 22 | @437[batch] | Call_refuses_an_assembly_that_belongs_to_another_scope | 不红 | 同上。 |
| 23 | @456 | Round_never_rewrites_the_narration… | 不红 | Immediate 默认 target="room-1"⇒:191 两态 false⇒绑定/投递链不变。 |
| 24 | @471 | Round_feeds_the_provider… | 不红 | 默认沉默 target 同 @381 推理⇒不变。 |
| 25 | @488 | Silence_round… | 不红 | 同上。 |
| 26 | @503 | Deferred_round… | 不红 | DeferredAction target＝:90 默认 Conversation⇒:191 两态 false；延迟登记不变。 |
| 27 | @520 | Draft_without_any_action… | 不红 | actions 空⇒V:145 循环不跑⇒:191 不求值；:139 拒绝不变。 |
| 28 | @534 | Malformed_response… | 不红 | R:110 短路⇒R:136 不发生。 |
| 29 | @549 | A_provider_that_ignores_cancellation… | 不红 | 同上。 |
| 30 | @562 | A_provider_that_honours… | 不红 | 同上。 |
| 31 | @574 | Provider_failure… | 不红 | 同上。 |
| 32 | @587 | Host_cancellation… | 不红 | R:108 抛出⇒R:136 不发生。 |
| 33 | @599 | Deferred_intent_that_claims… | 不红 | target 默认⇒:191 两态 false；RealityContamination 出自 :152 政策面，不变。 |
| 34 | @614 | Deferred_action_without_a_pointing_intent_is_refused | 不红 | target 默认⇒:191 两态 false；其拒绝出自 V:240-:243（本轮未改），Single/理由 :620-:622 不翻。 |
| 35 | @628 | Immediate_action_binds_to_the_unique_slice… | 不红 | target 默认⇒:191 两态 false；0 拒绝投递面不变。 |
| 36 | @644 | Quote_absent… | 不红 | target 默认⇒:191 两态 false；ActionBindingMissing 出自 :207-:211（未改），:650-:654 不翻。 |
| 37 | @658 | Ambiguous_quote… | 不红 | 同上；理由出自 :207-:211。 |
| 38 | @671 | Immediate_action_over_the_host_segment_ceiling… | 不红 | target 默认⇒:191 两态 false；:200 段数拒绝不变。 |
| 39 | @684 | An_over_split_slice… | 不红 | target 默认⇒:191 两态 false；账本面 SegmentStickerPayloadInvalid 不变。 |
| 40 | @698 | Immediate_action_carrying_a_future_intent… | 不红 | target 默认⇒:191 两态 false；:214 拒绝不变。 |
| 41 | @711 | Action_targeting_another_conversation_is_refused | **红（恰此一枚·断言级）** | 表头推导：target "room-2"（:713）⇒:191 改前 true→改后 false⇒拒绝消失→Immediate 支干净投递⇒:717 Assert.Single 空集合翻红，帧 :717。 |
| 42 | @722 | Perceived_reference_outside_the_batch… | 不红 | Silence target 默认⇒:191 两态 false；:166 拒绝不变。 |
| 43 | @735 | Watermark_beyond… | 不红 | 同上；:173 拒绝不变。 |
| 44 | @746 | Round_refuses_a_narration_without_prose… | 不红 | Immediate target 默认⇒:191 两态 false；其双拒绝出自 :131＋:207（均未改），:752-:754 不翻。 |
| 45 | @761 | Bound_action_keeps_a_reference_back… | 不红 | target 默认⇒两态 false；回指断言面不变。 |
| 46 | @776 | Draft_on_a_stale_story_version… | 不红 | R:121 即返⇒R:136 不发生⇒:191 不求值。 |
| 47 | @790 | Draft_from_a_fenced_writer_epoch… | 不红 | 同上。 |
| 48 | @802 | Draft_with_a_wrong_issue_baseline… | 不红 | 同上。 |
| 49 | @813 | Contiguous_split… | 不红 | 静态方法不经 CheckAction。 |
| 50 | @832 | Mixed_round… | 不红 | 两动作 target 均默认 Conversation⇒:191 两态 false；混合 Immediate 结果不变。 |
| 51 | @851 | Provider_side_cancellation… | 不红 | R:110 短路⇒无 Validate。 |
| 52 | @890 | Unified_budget_inequality… | 不红 | 记录面无 validator 通路。 |
| 53 | @913 | Whole_section_segments… | 不红 | Compile-only。 |
| 54 | @934 | Absent_sections… | 不红 | Compile-only。 |
| 55 | @952 | Unit_run_losing_units… | 不红 | Compile-only；"room-2"（:955）在 recent 单元面。 |
| 56 | @975 | Unit_run_that_fits_no_unit… | 不红 | Compile-only。 |

i′ 表合计：红 1（@711）＋不红 55＝56；第二处 `TargetConversationId` 已点名（:194，:191 支体内，改后恒不被执行）。

### 一.8 j′ 闭合表（V:131 `if (string.IsNullOrWhiteSpace(content.NarrativeProse))`→`if (false)`；见证格＝#40 `Round_refuses_a_narration_without_prose_instead_of_binding_to_nothing` @746）

预期红集＝**恰 1：@746**。断言帧 :752（`Assert.Equal(2, result.Rejections.Count)`），消息族 `Assert.Equal() Failure` Expected=`2`／Actual=`1`，性质＝断言级红。推导：:748 `prose: "   "`⇒改前 :131 true→NarrativeProseMissing[0]＋:207 Bind("   ","answered him") 0 现→ActionBindingMissing[1]＝2 拒绝；改后 :131 恒 false⇒NarrativeProseMissing 消失，:131 之后的 :137/:139/:145/:150/:155 照跑（consumption 面：默认 perceived/watermark 都过；:139 有动作），:207 绑定失败支给唯一拒绝⇒Count=1⇒:752 首翻（:753/:754 不再执行）。**工单点名的连坐风险格 @520（#23）不连坐的路径面（本刀自推）**：@520 夹具 :522 `Draft(actions: [])` 不传 prose⇒Draft 默认 `prose = Prose`（:105 默认参，:22 值非空白）⇒@520 到 :131 是**求值可达、值恒 false**（改前改后都不产 NarrativeProseMissing），其唯一拒绝源在 :139-:142（空动作⇒ActionBindingMissing），本轮未改⇒:526-:529 逐断言不翻。"也是 #23 走的路径之一"指的是求值经过，不是拒绝来源——路径级理由落此，非"应该不影响"。

| # | 格 | 方法名 | 预期 | 逐枚理由 |
|---|---|---|---|---|
| 1 | @183 | Compiler_emits_the_eight_assembly_units… | 不红 | Compile-only，无 Validate 通路⇒:131 不求值。 |
| 2 | @198 | Compiler_keeps_absent_sections… | 不红 | 同上。 |
| 3 | @210 | Compiler_drops_an_optional_section… | 不红 | 同上。 |
| 4 | @225 | Compiler_refuses_the_round… | 不红 | 同上。 |
| 5 | @237 | Compiler_refuses_a_window… | 不红 | 同上。 |
| 6 | @247 | Compiler_refuses_a_scene… | 不红 | 同上（其 identity:"" 在装配面 :249，不经草稿 :131）。 |
| 7 | @256 | Compiler_refuses_a_current_event… | 不红 | Compile-only。 |
| 8 | @266 | Compiler_fails_loudly… | 不红 | 同上。 |
| 9 | @281 | Compiler_trims_recent… | 不红 | 同上。 |
| 10 | @298 | Compiler_removes_cross… | 不红 | 同上。 |
| 11 | @313 | Compiler_shrinks… | 不红 | 同上。 |
| 12 | @328 | Compiler_honours… | 不红 | 同上。 |
| 13 | @347 | Compiler_rejects_a_negative… | 不红 | 同上。 |
| 14 | @357 | Compiler_rejects_a_window_of_zero… | 不红 | 同上。 |
| 15 | @367 | Compiler_carries_source… | 不红 | 同上。 |
| 16 | @381 | Round_calls_the_provider_exactly_once… | 不红 | 达 :131：prose=Prose 默认非空白⇒两态 false；Silence 结果不变。 |
| 17 | @394 | Round_does_not_call_the_provider… | 不红 | R:95 短路⇒R:136 不发生。 |
| 18 | @408 | Second_call_on_the_same_round_budget… | 不红 | 不经 validator。 |
| 19 | @420 | Call_refuses_an_assembly… | 不红 | 同上。 |
| 20 | @437[instance] | Call_refuses_an_assembly_that_belongs_to_another_scope | 不红 | 同上。 |
| 21 | @437[conversation] | Call_refuses_an_assembly_that_belongs_to_another_scope | 不红 | 同上。 |
| 22 | @437[batch] | Call_refuses_an_assembly_that_belongs_to_another_scope | 不红 | 同上。 |
| 23 | @456 | Round_never_rewrites_the_narration… | 不红 | :131 求值两态 false（prose=Prose）；:465 断言的正是 Prose 本体，不变。 |
| 24 | @471 | Round_feeds_the_provider… | 不红 | :131 false/false，装配喂料面不变。 |
| 25 | @488 | Silence_round… | 不红 | :131 false/false，沉默面不变。 |
| 26 | @503 | Deferred_round… | 不红 | :131 false/false，延迟登记面不变。 |
| 27 | @520 | Draft_without_any_action_is_rejected_rather_than_read_as_silence | 不红 | **工单点名连坐风险格＝#23**：表头路径面——求值可达、值恒 false、拒绝源在 :139 未改⇒:526-:529 不翻。 |
| 28 | @534 | Malformed_response… | 不红 | R:110 短路⇒无 :136。 |
| 29 | @549 | A_provider_that_ignores_cancellation… | 不红 | 同上。 |
| 30 | @562 | A_provider_that_honours… | 不红 | 同上。 |
| 31 | @574 | Provider_failure… | 不红 | 同上。 |
| 32 | @587 | Host_cancellation… | 不红 | R:108 抛出⇒无 :136。 |
| 33 | @599 | Deferred_intent_that_claims… | 不红 | :131 false/false；RealityContamination 在 :152，不变。 |
| 34 | @614 | Deferred_action_without_a_pointing_intent_is_refused | 不红 | :131 false/false；IntentClaimedAsFact 在 :240，不变。 |
| 35 | @628 | Immediate_action_binds_to_the_unique_slice… | 不红 | :131 false/false；绑定投递面不变。 |
| 36 | @644 | Quote_absent… | 不红 | :131 false/false（prose=Prose）；ActionBindingMissing 出自 :207-:211（本轮未改），:650-:654 不翻。 |
| 37 | @658 | Ambiguous_quote… | 不红 | :131 求值两态 false（其自定义 prose :660 "She wrote maybe later…" 非空白）；Ambiguous 在 :207-:211，不变。 |
| 38 | @671 | Immediate_action_over_the_host_segment_ceiling… | 不红 | :131 false/false；:200 拒绝不变。 |
| 39 | @684 | An_over_split_slice… | 不红 | :131 false/false；账本面不变。 |
| 40 | @698 | Immediate_action_carrying_a_future_intent… | 不红 | :131 false/false；:214 拒绝不变。 |
| 41 | @711 | Action_targeting_another_conversation_is_refused | 不红 | :131 false/false；:191 拒绝（本轮未改）不变。 |
| 42 | @722 | Perceived_reference_outside_the_batch… | 不红 | :131 false/false；:166 拒绝不变。 |
| 43 | @735 | Watermark_beyond… | 不红 | :131 false/false；:173 拒绝不变。 |
| 44 | @746 | Round_refuses_a_narration_without_prose_instead_of_binding_to_nothing | **红（恰此一枚·断言级）** | 表头推导：prose "   "（:748）⇒:131 改前 true→改后 false⇒Rejections 2→1⇒:752 翻红，帧 :752，Expected 2/Actual 1。 |
| 45 | @761 | Bound_action_keeps_a_reference_back… | 不红 | :131 false/false；回指面不变。 |
| 46 | @776 | Draft_on_a_stale_story_version… | 不红 | R:121 即返⇒无 :136⇒:131 不求值。 |
| 47 | @790 | Draft_from_a_fenced_writer_epoch… | 不红 | 同上。 |
| 48 | @802 | Draft_with_a_wrong_issue_baseline… | 不红 | 同上。 |
| 49 | @813 | Contiguous_split… | 不红 | 静态方法不经 Validate。 |
| 50 | @832 | Mixed_round… | 不红 | :131 false/false（prose 默认）；混合面不变。 |
| 51 | @851 | Provider_side_cancellation… | 不红 | R:110 短路⇒无 :136。 |
| 52 | @890 | Unified_budget_inequality… | 不红 | 记录面无 validator 通路。 |
| 53 | @913 | Whole_section_segments… | 不红 | Compile-only。 |
| 54 | @934 | Absent_sections… | 不红 | Compile-only。 |
| 55 | @952 | Unit_run_losing_units… | 不红 | Compile-only。 |
| 56 | @975 | Unit_run_that_fits_no_unit… | 不红 | Compile-only。 |

j′ 表合计：红 1（@746）＋不红 55＝56。

### 一.9 起跑计划（依 §一.6 退回后的形状）

- 起跑轮＝f′→g′→i′→j′ 四轮定向（每轮前预检一枚，编号 P121 起）；h′ 不起、额度退回。
- 收尾全量 F＝**不起**：§四.2 硬条件"五轮全部起跑且全部有效"因 h′ 退回而不满足（波 42 先例同理），不许为凑一次 F 多弹防火墙（§六.3）。
- 预算动用计划：build 0/1（三段判定合法跳过，见 〇.6）、定向 ≤4/5、F 0/1、dotnet 总 ≤4/6、预检＝每次 dotnet 一枚。

## 二 逐轮实跑十一项读数（每轮跑完即落；签署只认 TRX 面，stdout 面只抄录不签署——§六.6）

### 二.1 f′ 轮（V:240，见证格 #30 @614）——有效，实到＝落档表逐字符一致

1. 被改文件:行＝`src/Pry.Core/Narrative/NarrativeDraftValidator.cs:240`（@＝判据面另注，此处为产品文件行 :口径）。
2. 施加前盘上态＝`88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c`/19338（G4 面＝blob 同值，OK）。
3. 施加后盘上态＝`d652fb3a8acdde68e00fc3d61e57d9448e95379feee0aaf006af944f2bf0d1b0`/19240（G5 面＝逐字符等于 §一.1 预合成声明值；六道守卫全过，`out/apply-f.txt`）。
4. `git diff --name-only`＝仅 `src/Pry.Core/Narrative/NarrativeDraftValidator.cs` 一行；`git status --short` 施加态＝` M src/Pry.Core/Narrative/NarrativeDraftValidator.cs`（恰 1 行，纯 `src/`，无 `tests/**`）。
5. `run-f2.txt` 末行原样（仅抄录，不签署）＝`失败!  - 失败:     1，通过:    55，已跳过:     0，总计:    56，持续时间: 292 ms - Pry.Core.Tests.dll (net10.0)`。
6. **件8 两面**＝跑前 `out/cmd-f2.txt` 落档原样命令行，逐字符核对含 `--logger "trx;LogFileName=result-f2.trx"` 与 `--results-directory …trx-f2`（node 面 True/True/True）；跑完立刻数盘＝`out/trx-f2/` 内 `.trx` 枚数 **1**（非无凭据轮）。
7. TRX 读数（件5 仪器，`out/read-f2.txt`）：Counters total=56 executed=56 passed=55 failed=1；`outcome="Failed"` 开标签＝1，双向对账 AGREE；`NotExecuted` 开标签＝0；红格全名＝`Pry.Core.Tests.NarratorPrototypeTests.Deferred_action_without_a_pointing_intent_is_refused`（testName 剥括号后同左，无参数表）；断言帧＝`in …NarratorPrototypeTests.cs:line 620`（:＝TRX 断言帧行）；消息族＝`Assert.Equal() Failure: Values differ Expected: ContractRejected Actual: Deferred`（XML 实体已解）；性质＝断言级红。**逐字符比对 §一.4 落档表＝一致（格 @614、帧 :620、消息族、性质四项全等）；其余 55 枚 Passed，零连坐。**
8. 还原命令＝`node w43-apply.js revert f`（G6：字节源＝`git show 51f149d:src/Pry.Core/Narrative/NarrativeDraftValidator.cs`，未用 checkout/restore）。
9. 还原后 `git status --short` 行数＝0。
10. SHA 对账：判据三面（载体副本/主树副本/blob 面）＝`4f1b2f6e…` 本轮两侧＋出场当场打印全等；V 出场面＝`88ebf5c3…`/19338＝blob 原值；R 未触及面＝`961fb49b…`。
11. 预检段＝P121（`out/preflight-P121.txt`，VERDICT=BUILD_OK，本轮唯一一枚，dotnet 次数＝1＝预检枚数＝实跑轮数对账成立）。

### 二.2 g′ 轮（R:238，见证格 #42 @776）——有效，实到＝落档表逐字符一致

1. 被改文件:行＝`src/Pry.Core/Narrative/NarrativeRoundRunner.cs:238`。
2. 施加前盘上态＝`961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9`/16768（G4＝blob 同值）。
3. 施加后盘上态＝`fcda7132a66b8d7c7ee844d604ec199d8e406c075976cbdfee0a0dbfd129a0b1`/16727（G5＝逐字符等于 §一.1 声明值；六道守卫全过，`out/apply-g.txt`）。
4. `git diff --name-only`＝仅 R 文件；`status --short` 施加态＝` M src/Pry.Core/Narrative/NarrativeRoundRunner.cs`（恰 1 行纯 `src/`）。
5. `run-g2.txt` 末行原样（抄录不签署）＝`失败!  - 失败:     1，通过:    55，已跳过:     0，总计:    56，持续时间: 235 ms - Pry.Core.Tests.dll (net10.0)`。
6. 件8 两面＝跑前 `out/cmd-g2.txt` 原样落档＋逐字符核对（logger-trx true／results-directory true／LogFileName=result-g2.trx true）；跑完立刻数盘＝`out/trx-g2/` 内 `.trx` 枚数 **1**。
7. TRX 读数（`out/read-g2.txt`）：total=56 executed=56 passed=55 failed=1；Failed 开标签=1 与 Counters failed=1 双向 AGREE；NotExecuted=0；红格全名＝`…Draft_on_a_stale_story_version_is_refused_before_any_history`；断言帧＝`in …NarratorPrototypeTests.cs:line 782`（:口径）；消息族＝`Assert.Equal() Failure: Values differ Expected: ContractRejected Actual: Immediate`；性质＝断言级红。**与 §一.5 落档表四项全等（格 @776、帧 :782、消息族、性质）；其余 55 枚 Passed——含预检点名的 @790（:244 支）与 @802（:250 支）两枚下游格，实际未连坐，与推演一致。**
8. 还原命令＝`node w43-apply.js revert g`（G6 字节源＝`git show 51f149d:…NarrativeRoundRunner.cs`）。
9. 还原后 `git status --short` 行数＝0。
10. SHA 对账：判据三面＝`4f1b2f6e…`（本轮两侧打印）；R 出场面＝`961fb49b…`/16768＝blob 原值；V 未触及（G4/G6 面全程 `88ebf5c3…`）。
11. 预检段＝P122（`out/preflight-P122.txt`，BUILD_OK；本轮 dotnet＝1＝预检枚数）。

### 二.3 i′ 轮（V:191，见证格 #37 @711）——有效，实到＝落档表逐字符一致

1. 被改文件:行＝`src/Pry.Core/Narrative/NarrativeDraftValidator.cs:191`。
2. 施加前盘上态＝`88ebf5c3…`/19338（G4＝blob 同值）。
3. 施加后盘上态＝`27286e4dff4acc1f15c7aa5499ab25a4d7b21d95490d2f13c1ada7ee7497c551`/19250（G5＝等于 §一.1 声明值；六道守卫全过，`out/apply-i.txt`）。
4. `git diff --name-only`＝仅 V 文件；`status --short` 施加态＝` M src/Pry.Core/Narrative/NarrativeDraftValidator.cs`（恰 1 行纯 `src/`）。
5. `run-i2.txt` 末行原样（抄录不签署）＝`失败!  - 失败:     1，通过:    55，已跳过:     0，总计:    56，持续时间: 234 ms - Pry.Core.Tests.dll (net10.0)`。
6. 件8 两面＝跑前 `out/cmd-i2.txt` 原样落档＋逐字符核对（true/true/true）；跑完立刻数盘＝`out/trx-i2/` 内 `.trx` 枚数 **1**。
7. TRX 读数（`out/read-i2.txt`）：total=56 executed=56 passed=55 failed=1；Failed 开标签=1 双向 AGREE；NotExecuted=0；红格全名＝`…Action_targeting_another_conversation_is_refused`；断言帧＝`in …NarratorPrototypeTests.cs:line 717`；消息族＝`Assert.Single() Failure: The collection was empty`；性质＝断言级红。**与 §一.7 落档表四项全等；其余 55 枚 Passed。**
8. 还原命令＝`node w43-apply.js revert i`（G6 字节源＝git show）。
9. 还原后 `git status --short` 行数＝0。
10. SHA 对账：判据三面＝`4f1b2f6e…`；V 出场面＝`88ebf5c3…`/19338＝blob 原值；R 全程未触及。
11. 预检段＝P123（BUILD_OK；dotnet 1＝预检 1）。
12. 补充点名面（工单 §二 i′ 要求）：另一处 `action.TargetConversationId`＝V:194，位于 :191 支体内；本轮 TRX 全场恰 1 枚红＝该支体（含 :194）改后未被任何其余格在同场读到，与闭合表推演一致。

### 二.4 j′ 轮（V:131，见证格 #40 @746）——有效，实到＝落档表逐字符一致

1. 被改文件:行＝`src/Pry.Core/Narrative/NarrativeDraftValidator.cs:131`。
2. 施加前盘上态＝`88ebf5c3…`/19338（G4＝blob 同值）。
3. 施加后盘上态＝`88c00c05c7cb800bb67546f60985941d0fc87338d3d80067fa4f87ffcc423e87`/19294（G5＝等于 §一.1 声明值；`out/apply-j.txt`）。
4. `git diff --name-only`＝仅 V 文件；`status --short` 施加态＝` M src/Pry.Core/Narrative/NarrativeDraftValidator.cs`（恰 1 行纯 `src/`）。
5. `run-j2.txt` 末行原样（抄录不签署）＝`失败!  - 失败:     1，通过:    55，已跳过:     0，总计:    56，持续时间: 227 ms - Pry.Core.Tests.dll (net10.0)`。
6. 件8 两面＝跑前 `out/cmd-j2.txt` 原样落档＋逐字符核对（true/true/true）；跑完立刻数盘＝`out/trx-j2/` 内 `.trx` 枚数 **1**。
7. TRX 读数（`out/read-j2.txt`）：total=56 executed=56 passed=55 failed=1；Failed 开标签=1 双向 AGREE；NotExecuted=0；红格全名＝`…Round_refuses_a_narration_without_prose_instead_of_binding_to_nothing`；断言帧＝`in …NarratorPrototypeTests.cs:line 752`；消息族＝`Assert.Equal() Failure: Values differ Expected: 2 Actual: 1`；性质＝断言级红。**与 §一.8 落档表四项全等；其余 55 枚 Passed——工单点名的连坐风险格 #23/@520 实测在场 Passed，与 §一.8 行 27 的路径级推演一致。**
8. 还原命令＝`node w43-apply.js revert j`（G6 字节源＝git show）。
9. 还原后 `git status --short` 行数＝0。
10. SHA 对账：判据三面＝`4f1b2f6e…`；V 出场面＝`88ebf5c3…`/19338＝blob 原值；R 本轮未触及（出场面 `961fb49b…` 仍＝blob）。
11. 预检段＝P124（BUILD_OK；dotnet 1＝预检 1）。

### 二.5 四轮判定门总账

| 轮 | 实到红枚数（TRX 面） | 红格@ | 帧: | 消息族 | 性质 | ＝落档表？ | 判定 |
|---|---|---|---|---|---|---|---|
| f′ | 1 | @614 | :620 | `Assert.Equal() … Expected: ContractRejected Actual: Deferred` | 断言级红 | 逐字符一致 | 有效·#30 领（本轮带 `--logger trx` 凭据，补上波 42 f 轮的缺口） |
| g′ | 1 | @776 | :782 | `Assert.Equal() … Expected: ContractRejected Actual: Immediate` | 断言级红 | 逐字符一致 | 有效·#42 领 |
| h′ | 0（未起跑） | — | — | — | — | §一.6 退回表 | **件3′ 退回：预期同场 2 枚（@644 整格异常红＋@746 断言级红）≠恰 1，不起轮、定向额度原样交回** |
| i′ | 1 | @711 | :717 | `Assert.Single() Failure: The collection was empty` | 断言级红 | 逐字符一致 | 有效·#37 领 |
| j′ | 1 | @746 | :752 | `Assert.Equal() … Expected: 2 Actual: 1` | 断言级红 | 逐字符一致 | 有效·#40 领 |

- 每轮 total＝56＝§〇.4 当场格数（4 轮 TRX Counters total 均为 56，无一轮偏移）。
- 无任何轮作废（件4 未触发）；无 `-voided-` 归档名（find 面＝0）；无无凭据轮（4 轮结果目录各恰 1 枚 `.trx`）。
- 红格签署全部走 TRX 面；stdout 末行只抄录存档，未据其签署任何一格（§六.6）。

## 三 还原与终态对账（全部当场从盘打印）

- 四次还原命令均为 `node w43-apply.js revert <轮>`，字节源一律 `git show 51f149d:<path>`（G6 面，未用 `checkout`/`restore`）；逐轮 REVERTED 行见 `out/revert-f.txt`／`revert-g.txt`／`revert-i.txt`／`revert-j.txt`。
- 载体终态三件（出场当场）：`git status --short` 行数＝**0**；`git log --oneline fd93ebe..HEAD` 行数＝**2**；`rev-parse HEAD`＝`51f149d0b722e28a6566c93d19f48a94ca4c85df`。
- 被改文件三面 SHA 终态：
  - `NarrativeDraftValidator.cs`（f′/i′/j′ 三轮被改）：blob＝载体工作副本＝主树工作副本＝`88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c`（19338 字节）。
  - `NarrativeRoundRunner.cs`（仅 g′ 被改）：blob＝载体工作副本＝主树工作副本＝`961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9`（16768 字节）。
- 判据文件终态：载体副本＝主树副本＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`＝工单基准＝入场 blob 面；全程一枚字节未写（§二每轮第 10 项两侧打印齐）。
- 主树终态：`status --short --branch` ＝入场 9 行原样＋本刀唯一新建报告一行 `?? docs/eval/reports/2026-10-29-hds13-dclass-third-slice.md`（不提交、不 `git add`）；他人资产（两 `M `、一 ` M`、五 `??`）逐枚原位未动；`git diff --stat 51f149d HEAD -- src tests scripts Pry.slnx Directory.Build.props` 出场复测＝**0 行**（零产品新字节终态成立）。
- 主树零 git 写动作自证：本刀在主树内只运行过只读命令（`rev-parse`／`status`／`log`／`diff --stat`／`worktree list`），无 commit/add/checkout/reset/cherry-pick/merge/tag/push/apply/stash/clean/rm 任何一枚。

## 四 收尾全量 F＝不起（登记跳过依据）

- §四.2 硬条件＝"五轮全部起跑且全部有效才起"；h′ 按件3′ 退回未起跑 ⇒ 条件不满足 ⇒ **F 不起**（波 42 联动先例同理）。
- 不为凑一次 F 多弹防火墙（§六.3）；在册 F 基线（波 40 `trx-F` total 1406／failed 0／NotExecuted 2）本刀未复采即不移动。

## 五 预算、预检台账与产物形状

### 五.1 四类预算动用／上限

| 预算 | 上限 | 动用 | 说明 |
|---|---|---|---|
| 解决方案级 `dotnet build` | ≤1 | **0** | 三段判定合法跳过（§〇.6；含 §一.7 dll 清洁度限定照写） |
| 解决方案级全量 `dotnet test Pry.slnx -c Release` | ≤1 | **0** | §四.2 条件不满足（h′ 退回），不起（§四） |
| 定向 `dotnet test --filter` | ≤5 | **4** | f′/g′/i′/j′ 各 1；h′ 额度原样退回 |
| `dotnet` 总调用 | ≤6 | **4** | 上三行之和 |
| HOLD 重跑 | ≤16 | **0** | 无 HOLD |

### 五.2 预检台账（编号 P121 起；三方对账＝逐份 VERDICT 枚数＝dotnet 次数＝实跑轮数）

- `out/preflight-P121.txt`＝BUILD_OK→f′；P122＝BUILD_OK→g′；P123＝BUILD_OK→i′；P124＝BUILD_OK→j′。逐份 BUILD_OK 枚数＝4＝定向 dotnet 次数＝4＝实跑轮数＝**4＝4＝4 对账成立**。
- `out/preflight-P120b.txt`＝守门人入场前自留一枚（非本刀台账内，不归本刀计数，只登记归属防混账）。
- HOLD 枚数＝0，无连续 HOLD 交局面。

### 五.3 产物形状清点（当场 find／ls）

- 盘上 `.trx` 枚数＝**4**（`out/trx-f2/result-f2.trx`、`trx-g2/result-g2.trx`、`trx-i2/result-i2.trx`、`trx-j2/result-j2.trx`）；结果目录枚数＝4；`-voided-` 名＝**0**。
- 每轮 `total`（TRX Counters）＝56＝§〇.4 当场格数 56，四轮齐；每轮 `executed`=56、`NotExecuted` 开标签=0、Passed=55。
- 命令形状件8 双面（`cmd-*.raw` 三 true 自检＋跑完即数＝1）＝4/4 轮齐。
- 预检 5 份（含 P120b 归属枚）；run stdout 4 份；read 4 份；apply/revert 各 4 份；件1/2/3 面 1 份；仪器自证 1 份；闭合扫描 1 份；入场读数 1 份；脚本 5 枚（`w43-prep.js`／`w43-apply.js`／`w43-count.js`／`w43-scan.js`／`w43-fix-report.js`，均本刀自写）；`mutation-prep/` 五枚（含 h′ 那枚——预合成完成但该轮退回，字节从未施加到载体，仅作 §一.1 声明面留存）。
- 报告组装修复登记（落盘后期，如实自曝）：本刀在增量落盘 §二.4〜§七 时一次 Edit 用错锚点，致 §二.2 标题＋项 1 丢失、§二.2 项 2–11 与 §二.3 悬挂到 §七 之后、尾部残留哨兵一行。修复＝`node w43-fix-report.js`（唯一写面＝本报告文件；纯已落块搬运＋按 §一.1 声明面重建 §二.2 标题与项 1 两行；十一项读数内容一字未改，§一 各落档表预期列零触碰），事后校验＝§二.1→二.5 标题各恰 1、项数 11/11/12/11、哨兵除净；修复后章节序＝〇→一→二(1–5)→三→四→五→六→七，§九 仍不存。本登记只述组装面事故，不触及任何测量面。

## 六 未验证面（逐枚登记，能数盘的已数盘，数不了盘的明写未验证）

1. h′ 轮（#32@644，V:71 门）＝**未实跑**：§一.6 推演给出同场两枚受害（@644 整格异常红＋@746 断言级红）与判定门"恰 1"不可调和，按件3′ 退回；该推演是盘上字节级论证，不是运行凭据——#32 本刀**未领**，保持欠账。
2. F 收尾全量＝未起（§四）；`trx-F` 在册基线未动。
3. 解决方案级 build＝未起（§〇.6 三段判定跳过）；四轮定向跑的自带增量编译只覆盖 `Pry.Core`＋`Pry.Core.Tests` 两工程面，其余工程本轮零编译凭据。
4. 在盘 dll 清洁度＝未验证（§一.7 限定）：终态 dll 是 j′ 突变态构建再经还原后未重编的产物，源码字节虽已回 blob，本刀不写任何"清洁二进制"自证。
5. CS0162 面＝未实测计数：i′ 轮增量编译通过（BUILD 面在 run-i2.txt 的 ` -> ….dll` 产出行在场，件7 正面凭据），但本刀归档零枚编译告警清单，`:194` 支体是否触发 CS0162 属"编译器版本行为"面＝未逐枚读数，登记为未验证；此面不影响 i′ 判定门（红枚数与红名以 TRX 为准）。
6. `NarrativeCommitValidator.cs:385` 复用 DraftValidator.Validate 的生产侧调用链（CommitWriter.cs:178／SqliteStore.cs:492）＝判据零提及、本刀零凭据，未验证面照旧。
7. 真实模型对照（FakeNarrator 之外）＝未验证（判据头注 :13 原样立场，本刀未触碰）。
8. 波 42 f 轮（#30 首次尝试）＝无凭据轮历史读数不变；本刀 f′ 只是把 #30 以带凭据轮重领一次，不消解波 42 报告本身的历史登记面。

## 七 交回守门人核的事（工单 §五 九枚，逐件当场读数）

1. **五轮各自实到红枚数与红名／帧行／消息族 vs §一 落档表**：f′＝1（@614/:620/`ContractRejected→Deferred`）；g′＝1（@776/:782/`ContractRejected→Immediate`）；h′＝未跑（件3′ 退回，依据＝§一.6 两枚同场推演）；i′＝1（@711/:717/`Assert.Single collection empty`）；j′＝1（@746/:752/`Expected: 2 Actual: 1`）。四轮逐字符等于落档表；无作废轮。
2. **每轮 cmd 与跑后 .trx 枚数（件8 两面）**：`cmd-f2/g2/i2/j2` 四面齐（各含 `--logger "trx;…"`＋`--results-directory` 三 true 自检），跑完即数各＝1。
3. **判据 SHA 每轮两侧＋出场面**：§二.1〜二.4 第 10 项逐轮打印，全部当场＝`4f1b2f6e…`；无缺打印轮次，无需替代面。
4. **载体终态三件＋被改文件三面 SHA**：status 行数＝0、`fd93ebe..HEAD` 行数＝2、HEAD＝`51f149d0…`；V 三面＝`88ebf5c3…`、R 三面＝`961fb49b…`（§三逐字符值）。
5. **收尾全量**：未起（§四），无求和式；在册 1406/0/2 基线未动。
6. **盘上 .trx／结果目录／-voided-／total 对账**：4／4／0／四轮 total 均 56＝§〇.4 当场值（§五.3）。
7. **四类预算动用与上限**：build 0/1、F 0/1、定向 4/5、总 4/6、HOLD 0/16；预检三方对账 4=4=4（§五.1／五.2）。
8. **每一枚红的性质**：f′/g′/i′/j′ 四枚均断言级红；h′ 未跑，其推演面 §一.6 已逐枚点名形状（@644＝整格异常红 ArgumentOutOfRangeException@R:191、@746＝断言级红）并如实登记为退回依据而非红凭据。
9. **未领账与判不清面**：#32（h′）未领＝本刀退回面，两枚同场推演已逐枚落 §一.6；i′"另一处 TargetConversationId"点名＝V:194（:191 支体内，改后恒不被执行，实测全场恰 1 佐证）；j′ #23/@520 不连坐路径面＝求值可达、值恒 false、拒绝源 :139 未改（§一.8 行 27，实测 @520 Passed 佐证）；其余判不清面＝§六 八枚逐登。

（§九 节号保留空白，由守门人裁决面使用；本刀不写归位／顶账／基线移动。）

## 九 守门人裁决（追加面，本刀产物；所有数字当场从盘取，不引用 lane 的 §〇 与上一把工单当入场值）

### 九.0 入场与出场盘面复看（我这一侧独立重取）

- 主树：`rev-parse HEAD`＝`68e6e2645f4c7e8710e4fedae85470c3003fb810`；`status --short --branch`＝9 行（两枚 `M ` 暂存项＝`docs/dispatch-tip1020-02-memory-group-judgment.md` 1/1 与 `tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs` 63/0、一枚 ` M` 的 `docs/plans/PROJECT_PLAN.md` 当场 `git diff --numstat HEAD` 复测＝**18 0**、`??` 六枚＝五枚既有资产＋本刀唯一新建报告）；他人资产逐枚原位未动，本刀在主树零 `git` 写动作。
- 载体四件：`rev-parse HEAD`＝`51f149d0b722e28a6566c93d19f48a94ca4c85df`、`status --short` 行数＝**0**、`log --oneline fd93ebe..HEAD` 行数＝**2**、`rev-parse --show-toplevel`＝`.local-notes/qa-wave33-1028/carrier`。
- `git worktree list` 行数＝**76**（与 catalog 76 同面）。
- 判据文件三面 SHA 逐字符＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`（blob＝载体工作副本＝主树工作副本）；两枚被改产品文件终态三面＝`NarrativeDraftValidator.cs` `88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c`/19338、`NarrativeRoundRunner.cs` `961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9`/16768。
- 决定性那枚 0 行式子当场成立＝`git diff --stat 51f149d 68e6e26 -- src tests scripts Pry.slnx Directory.Build.props`＝**0 行**；且 `51f149d` 不是 `main` 的祖先 ⇒ 通道从来不是 `merge --ff-only`。
- 我另取一枚 lane §〇.6 之后变化的面（终态而非入场态）：`carrier/src/Pry.Core/bin/Release/net10.0/Pry.Core.dll` 现值＝**1,745,920 字节、mtime 10-29 12:54**，而 lane 入场读数是 1,745,408 字节/11:58。两枚都真：入场那枚是波 42 末轮（R:250 突变态）的产物，终态这枚是本刀 j′（V:131 突变态）自带增量编译的产物，源码字节已回 blob。**这就是 §一.7 那条限定最硬的一次现场证据——同一枚路径在一把刀之内换了两副字节**，它永远只支撑"成品在盘"，不支撑"清洁二进制"。

### 九.1 第十一枚独立仪器与件 6 自证

- 仪器＝`.local-notes/wave43-1029/gk43-verify.js`（node 从零写；lane 的 `w43-count.js`／`w43-apply.js`／`w43-scan.js`／`w43-fix-report.js` 一行没读、没拷），输出留档 `out/gk43-verify.stdout`。
- 九面带四元限定（波次＋产物／判据文件＋跑法＋该面字段值）的老读数复算＝**9/9 AGREE**＝波 42 `trx-g/h/i/j` 各恰 1（帧 :665/:730/:741/:808，消息族同值）、波 40 `trx-M8p` 恰 5、波 40 `trx-m08` 恰 4（帧含 961）、波 39 `trx-M2` 恰 3、波 39 `trx-M8-voided-P103` 恰 5、波 40 `trx-F` 四份求和 1406／passed 1404／failed 0／`NotExecuted` 开标签 2。
- **我自己的两处仪器错（如实登记，两处坏的都是仪器不是盘）**：① 首版按 `<Counter outcome="X" count="N">` 取计数器，盘上真实形状是单枚 `<Counters total="…" executed="…" passed="…" failed="…" notExecuted="…" />`（属性名小写、单元素）⇒ 四面全 `null` 而集体报警；我把那份 TRX 的 `<Counters>` 段逐字节打出来证明盘没坏，改的是我的取数层。② 首版 `dec()` 只解命名实体（`&quot;` 等），消息面留着 `&#xD;` 这类数字实体原样，于是"消息族逐字符等于落档表"那枚判定当时**并不成立**——修法不是直接信任新实现，而是把四面历史消息族（波 42 g/h/i/j 全＝`Assert.Single() Failure: The collection was empty`）加为已知答案做等值检查，改后仍 9/9 AGREE 才用它裁新读数。
- 口径升级（进件 5）＝**XML 实体解码表必须含数字实体，且解码这条通道本身要被一枚已知答案当面值验一次；"报了 AGREE"不等于"那个字段真被读过"**。

### 九.2 四轮实跑的裁决（红证只认 TRX 面，stdout 面只抄录）

| 轮 | 门（产品文件:行，载体 `51f149d` 现文） | 波 22 审计 §四.1 点号 | 实到红（我的仪器） | 帧（:＝TRX 断言帧行） | 消息族 | 性质 | 判定 |
|---|---|---|---|---|---|---|---|
| f′ | `NarrativeDraftValidator.cs:240`（`else if (!content.Intents.Any(…))`→`else if (false)`） | **#30** | 恰 1＝`Deferred_action_without_a_pointing_intent_is_refused`（格 @614） | :620 | `Assert.Equal() Failure: Values differ / Expected: ContractRejected / Actual: Deferred` | 断言级红 | **有效·结清**（补上波 42 f 轮漏 `--logger trx` 的那枚运行凭据） |
| g′ | `NarrativeRoundRunner.cs:238`（`if (draft.StoryVersionBase != request.StoryVersion)`→`if (false)`） | **#42** | 恰 1＝`Draft_on_a_stale_story_version_is_refused_before_any_history`（格 @776） | :782 | `…Expected: ContractRejected / Actual: Immediate` | 断言级红 | **有效·结清** |
| i′ | `NarrativeDraftValidator.cs:191`（`if (!string.Equals(action.TargetConversationId, request.ConversationId, …))`→`if (false)`） | **#37** | 恰 1＝`Action_targeting_another_conversation_is_refused`（格 @711） | :717 | `Assert.Single() Failure: The collection was empty` | 断言级红 | **有效·结清** |
| j′ | `NarrativeDraftValidator.cs:131`（`if (string.IsNullOrWhiteSpace(content.NarrativeProse))`→`if (false)`） | **#40** | 恰 1＝`Round_refuses_a_narration_without_prose_instead_of_binding_to_nothing`（格 @746） | :752 | `Assert.Equal() Failure: Values differ / Expected: 2 / Actual: 1` | 断言级红 | **有效·结清** |

- 四轮每轮 `nodes`／`total`＝**56**＝§〇.4 当场格数（`[Fact]`53＋`[Theory]`1×`[InlineData]`3），`executed` 56、`passed` 55、`failed` 1、`NotExecuted` 开标签 0；`outcome="Failed"` 开标签与 `<Counters failed>` 双向等值，且 `outcome="Failed"` 全文字面量−开标签恒＝1、那 1 枚属 `<ResultSummary outcome="Failed">`（不算用例）——波 30 立的"只数 `<UnitTestResult>` 开标签"第五次现场生效。
- 件 8 双面四轮齐（`out/cmd-{f2,g2,i2,j2}.txt`/`.raw` 含 `--logger "trx;…"` 与 `--results-directory`，跑完立刻数盘各 1 枚 `.trx`）⇒ **波 42 那枚"漏 logger 的无凭据轮"这一把没有再发生；件 8 第一次实际使用＝买对了**（它是上一把我因亏 1 枚未领＋1 次 F 读数而新增的硬门，登记为仪器参数不是判据）。
- 签账资格判据按同一把尺使用成动作（这类 ordinal 我自本刀起不再自报，理由见 §九.8）＝判据字节全程未写（lane §二 每轮第 10 项两侧打印＋我三侧当场重测＝`4f1b2f6e…`；last-touch 主树 `639a579`／载体 `290a603`，`fd93ebe..HEAD` 那 2 行里没有本刀这只手）＋过程面硬证（四轮施加态 `git status --short` 各恰 1 行且永远是 `src/` 产品文件、从不出现 `tests/**`）⇒ **这 4 枚的红证与第二换手同场结清**，按格名逐枚对波 22 审计 §四.1 的 T3 表点号。
- 工单点名的两枚另面我自己回读了盘：① i′ 的"另一处 `action.TargetConversationId`"＝`NarrativeDraftValidator.cs:194`（我按 `git show 51f149d:…| grep -n` 取到 `:191`／`:194` 恰两处），:194 在 :191 支体内、改后恒不被执行，与全场恰 1 枚红自洽；② j′ 的 #23/@520 不连坐＝@520 传默认非空白 prose ⇒ `:131` 求值得到 false、拒绝源在 `:139`，本轮实测 @520 在场 Passed。
- 还原四面（`out/revert-{f,g,i,j}.txt`）逐枚打印改回后的 SHA 全等 blob 原值（V 三轮 `88ebf5c3…`、R 一轮 `961fb49b…`），字节源一律 `git show 51f149d:<path>`，未用 `checkout`/`restore`；四轮还原后载体 `status --short` 行数各＝0。

### 九.3 h′ 退回的裁决，以及我自己那张表错在哪

- lane 的推演面＝§一.6 那张 56 行逐枚点名的退回表（不是闭合表）：V:71 摘除后 `NarrativeProseBinder.Bind` 直落 `:81-:86` 成功返回 `ResolvedOffset = first = -1`（`int?` 的 HasValue 仍为 true），于是 @644 走成**整格异常红**（R:191 `commit.NarrativeProse.Substring(-1, 35)` 抛 ArgumentOutOfRangeException），而 @746 因 `:207` 不再产生绑定拒绝使 `Rejections.Count` 2→1 翻成**断言级红** ⇒ 同场 2 枚 ≠ 判定门"恰 1" ⇒ 按件 3′ 不起轮、定向额度原样退回（退回不等于作废，盘上 `-voided-` 名＝0 已核）。
- 我核这一刀的三条独立面：① **先档后跑**＝mtime 序列 12:29:29 `entry-readings` → 12:35:23 `item1-2-3-prep` → 12:37:28 `instrument-selfproof` → 12:44:50 `closure-scan` → 12:50:47 首枚 `preflight-P121` → 12:51:17 `run-f2`，全部闭合表落档在任何 `dotnet` 之前；② h′ 的预合成字节 `092e4594a383fbc404693368e9a8b28740aafd71742c9101b1f6629b391f4c63`/19327 确实落 `mutation-prep/` 而从未施加（四份 `apply-*` 里没有 h，载体 `status` 全程只出现被跑那两枚文件）；③ §一.6 里 @671／@684 各有一句把"为什么不连坐"拆到行（不是"应该不影响"）。
- **我的错（第五张同族错表，当场更正、不涂改原文）**＝波 43 工单 §二 h′ 那行我把形状写成"红可能是断言级、也可能是整格异常红——形状不同仍只算 1 枚"。那句话只处理了 @644 自己那一格的形状，**没有逐枚枚举 @746 会因为 `:207` 绑定改判成功而连坐**。我出闸时把"单行突变＝1 枚"当默认，而 V:71 是一支早退 `return`：摘掉它等于把失败路径换成成功路径，下游每一个消费者都必须先列一遍。口径升级（进波 44 与新前置件清单）＝**凡被改的那行是一支早退 `return`／`throw`，事前论证必须先把它下游的每一个消费者逐枚枚举**，"仍只算 1 枚"这句从此不许再写。
- #32 裁决＝**未领、账不减**，状态＝"需要重新配一刀"，既不是"再找一双手"也不是"结构上够不着"：按盘上事实 `:71` 与 `:76` 同属 `Bind` 的相邻两支（波 42 g 轮已在 `:76` 拿到 @658 的凭证）、`:51-:69` 是计数循环；要结 #32 得先构造一条"只被 @644 走"的改坏（候选方向＝计数循环本身、或让 `first` 初值面单独坏掉），且必须先按当前 blob 证明 @746／@456／@628／@684／@761／@832 这些同走 `Bind` 的格不连坐。这把刀押在波 44 之后，不在波 44 默认范围。

### 九.4 F 依法不起／基线不移动／构建合法跳过

- §四.2 硬条件"五轮全部起跑且全部有效才起"因 h′ 退回而不满足 ⇒ **F 不起**（与波 42 那次联动是同一条判据的第二次执行）。本刀没有 `trx-F`，在册 1406／executed 1404／failed 0／跳过 2 未被复采也**不移动**；**不为顶账另起构建**——少弹一次防火墙是那道门买下来的，不是"少做了一件交付"。
- 解决方案级构建 0/1＝三段判定当场复取成立：0 行式子（对 `68e6e26` 实测）＋成品在盘（入场面 1,745,408 字节/11:58，见 §九.0 的终态变化）＋三枚 SHA 三面未变。四轮定向跑的自带增量编译只覆盖 `Pry.Core`＋`Pry.Core.Tests` 两工程面，其余工程本轮零编译凭据。

### 九.5 报告组装事故的裁决（lane §五.3 自曝，`w43-fix-report.js` 13:01:21）

- 三面复看：① 结构＝`### 二.1`〜`### 二.5` 与 `## 三`〜`## 七` 各恰 1 枚、章节序〇→一→二→三→四→五→六→七、哨兵字样除登记那句之外 0 命中、§九 此前不存在；② 张数＝四张闭合表（§一.4/一.5/一.7/一.8）＋那张退回表（§一.6）各**恰 56 行**（我按行首 `| 数字 |` 逐张数）；③ 判定面＝§一 各预期列与我独立仪器从盘读出的红名／帧行／消息族逐字符相符，四轮 11 项读数里没有一项是"从实测回写预期"。
- 裁决＝事故只在组装面，不移动任何判定；lane 把它写进 §五.3 而不是悄悄搬平，并且明确"唯一写面＝本报告文件、十一项读数一字未改、预期列零触碰"——处置正确。登记为口径＝**报告组装用错锚点属可自愈的组装缺陷，但必须自曝并给出复看面；不自曝就会被读成"预期列是抄实测的"**。

### 九.6 预算与形状对账（当场）

- 盘上 `.trx`＝**4** 枚＝定向 4 轮 × 1；结果目录＝4 个；`-voided-` 名＝0；未施加的 h′ 预合成字节 1 枚留存；`mutation-prep/` 五枚。
- `dotnet` 总调用 **4/6**＝定向 **4/5**（退回那轮没被写成动用，也没为用满额度另造一轮）＋build **0/1**＋全量 **0/1**；HOLD 重跑 **0/16** 未动用。
- 预检台账＝P121〜P124 逐份 grep `VERDICT` 全 `BUILD_OK`＝4 枚＝`dotnet` 次数＝实跑轮数（三方对账成立）；mtime 一一配对＝12:50:47＜12:51:17、12:52:03＜12:52:22、12:52:52＜12:53:16、12:53:44＜12:54:02 ⇒ 无"跳过预检起跑"的形状。
- `preflight-P120b.txt`（我出闸时自留一枚，BUILD_OK）不归 lane 台账，归属面照波 40 那条先例登记防混账；lane 编号严格从 P121 起成立。

### 九.7 格面复看

判据文件 `51f149d` 的 blob 三枚计数当场重取＝`[Fact]` 53／`[Theory]` 1／`[InlineData` 3 ⇒ **56 枚格**、`Assert.Skip` 0 处、989 行，与波 40／41／42 的出场值逐项同值 ⇒ 本刀**净新增格 0、改断言 0、新增数字门槛 0**。

### 九.8 归位裁决＝零落地（同一条判据照旧执行）

载体线尾仍 `51f149d`、`status` 0 行、`fd93ebe..HEAD` 恰 2 行（本刀零提交）；决定性那枚 0 行式子两条腿当场成立 ⇒ 在树上做归位是空转，本把落地面只有文档（lane 报告 §〇–§七＋我的 §九 追加＋波 44 工单＋四处登记）。**一处我自己的抄录错（当场更正，不涂改 lane 任何读数）**＝本把入场时我把 §九.2 与 §九.8  ordinal 直接抄成"第八次"，那是波 42 那份报告的计数面；从盘上既有报告与登记重取＝波 41 写第七次、波 42 的两份产物互相对不上（报告 §九＝"签账资格第八次"、sprint §3 那条密行＝"第七次"）。⇒ 新口径＝**"第 N 次执行"这类 ordinal 不再自报，只写可核的参照（波次＋提交＋判据文件名）**；ordinal 是我上一把刚登记过的"抄上一把的读数当当场读数"那一族的最后一次，本刀起四处登记与波 44 工单均不出现此类计数。

### 九.9 旧欠求和（当场从盘上产物写，不按上一把的"余 15"做减法）

- T3 全梯队我从波 22 审计 §四.1 逐行重数＝**23 枚**＝#16/#17/#18/#21/#23/#28/#29/#30/#32/#33/#34/#35/#36/#37/#38/#39/#40/#42/#43/#44/#45/#46/#48。
- 已签＝波 41 四枚（#23/#29/#17/#43）＋波 42 四枚（#33/#38/#39/#44）＋本刀四枚（#30/#42/#37/#40）＝**12 枚** ⇒ T3 余 **11**＝#16/#18/#21/#28/#32/#34/#35/#36/#45/#46/#48（逐枚点名）。
- T4 5 枚（#12/#19/#20/#41/#47）与波 20 那 28 枚照旧不减 ⇒ **旧欠 48 → 44**＝(11＋5)＋28。
- #49（T5 拆门无人守）与 #31（那只手写过的判据字节会作废凭证）照旧单列，不进这本账的加减。

### 九.10 未验证面（lane §六 那八条照抄一条不减＋我补三条）

lane 的八条＝h′ 未实跑（#32 保持欠账）／F 未起／解决方案级 build 未起／在盘 dll 清洁度未验证／CS0162 面未逐枚读数／`NarrativeCommitValidator.cs:385` 复用链判据零提及零凭据／真实模型对照未验证／波 42 f 轮的历史登记面不消解。我补三条＝⑨ 件 8 只证"命令行含 logger 与结果目录"，不证所跑二进制的清洁度（两面是两件事）；⑩ 我的 `<Counters>` 与 `dec()` 两处已修，但防法只剩"已知答案当面值"这一条，波 44 的自证表必须至少含一枚消息族面值；⑪ h′ 那两枚受害是静态推演、零运行验证，@644 的异常形状与 @746 的计数面都属"如果跑会怎样"，登记为推演面不是红凭据。更大的十条老面（真实模型对照／生产接线＝这 56 枚至今零真实调用方／旧数据继承／UI 读数／生产库与 `%LOCALAPPDATA%\PryCompanion`／待标定参数＝特性不是缺陷／并发与断电／覆盖率／旧链删除片与不进包那道门）照旧一条不减。

### 九.11 出闸与押账

波 44＝D 类第四片＝T3 余 11 里的五枚，先追红方向已被事前点名的那几枚，锚点全部按载体 `51f149d` 现文由 lane 自己重取（#45/#28 的历史行号已实测漂移、#34/#36 与波 14 成对同门＝一次全量两类同跑会两枚同响，属全量面不属定向面，本刀不起全量即不碰它）；件 3′／件 6／件 7／件 8 四条照旧为前置，另加 §九.3 那条新口径（早退 `return` 的下游消费者逐枚枚举）与 §九.1 那条（解码通道要被已知答案验一次）。押账照旧不减＝#32 的新配方、T4 那 5 枚、波 20 那 28、#49、#31、`:367`、探针正数面单独读数（成本裁决＝改被守护判据会作废既有凭证，只与本来就要改那棵判据的接线刀合并）、HDS-007／009 那两棵的 `:195`／`:172`、波 28 那枚数值锁格（零门槛是作者定的，不为凑红造门槛）、`PrototypeFiles` 写死清单的覆盖盲区、两套并行命名债、HDS-010 删除片、HDS-003／007／009 的生产接线（属接线刀不属证据刀）；拆法原文照旧 **46／45／1＝W2**、catalog **76**、基线 **1406 不移动**。

### 九.12 一句与本刀判定无关、要报给作者的面

lane 在本仓库之外发现一枚可疑文件 `C:\Users\24782\Downloads\nul` 并登记为安全告警；本刀与 lane 都**没有读取、没有执行**它的任何内容，我也不会去碰它。它不在仓库里、不在任何一条证据链上，处置权在作者。

### 九.13 三句话照旧不许写

新链已接线／旧链可删／不进包已做到，以及"她的状态已经迁到新链"——本刀 4 枚结的是证据身份（红证＋第二换手），一格都没有接进产品运行路径；界面与对话今天零变化。
