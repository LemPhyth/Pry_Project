# 波 13 D 类第一片·第二双手（第四十一波，HDS-WAVE41-1029，lane 侧）

- 工单：`docs/dispatch-hds-wave41-1029.md`（`HDS-WAVE41-1029`）。坐标系＝`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS-003。
- 性质：证据刀·换人复采。零产品新字节（终态）、零判据字节、零新格、零新增数字门槛、零 UI、零生产接线。
- 载体：`.local-notes/qa-wave33-1028/carrier`（基点／detached HEAD＝`51f149d`）。本刀不新建 worktree、不新建产品或测试文件、主树零 git 写动作。
- 本报告是主树里本刀唯一新建文件，未提交、未 `git add`，归位由守门人按路径落地。守门人裁决节不由本刀写。
- 判据文件＝`tests/Pry.Core.Tests/NarratorPrototypeTests.cs`，本刀全程一枚不写；基准 SHA＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`。

## 〇 入场与先数盘（全部当场从盘取，不引用工单或波 40 的数字当入场值）

- 时刻：`date` = `Thu Oct 29 10:46:20 2026`（随后每枚预检另记时刻）。
- 主树 `git rev-parse HEAD` = `88a642b93848249467b6f014bb0acb0abfe070ca`（协作者已在此分支续提交，非波 40 报告里的 `6ae0803`；本刀不碰主树）。
- 主树 `git status --short --branch` 原样（入场时 **9 行**＝1 行 branch＋两枚 `M ` 暂存项＋一枚 ` M`＋五枚 `??`）：

  ```text
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

  以上全部他人资产：两枚 `M ` 暂存项、一枚 ` M`（`PROJECT_PLAN.md` 上他人 18 行在飞）、五枚 `??` —— 本刀不读内容、不改、不提交、不 `stash`／`clean`／`rm`。`mkdir .local-notes/wave41-1029/` 后主树 `git status` 仍为同样 9 行（`.local-notes` 已被 `git check-ignore` 证实忽略），报告落盘后将新增本报告一枚 `??`。
- 载体入场自证（当场取）：

  | 件 | 命令 | 读数 |
  |---|---|---|
  | ① | `git -C carrier rev-parse HEAD` | `51f149d0b722e28a6566c93d19f48a94ca4c85df` |
  | ② | `git -C carrier status --short` | **0 行**（起跑门，非 0 行即停手交回） |
  | ③ | `git -C carrier log --oneline fd93ebe..HEAD` | **2 行**＝`51f149d`＋`1e02205` |
  | ④ | `git rev-parse --show-toplevel`（主） | `C:/Users/24782/Desktop/Pry_Project` |
  | ⑤ | `git -C carrier rev-parse --show-toplevel` | `…/.local-notes/qa-wave33-1028/carrier`（每次写文件前先证此面） |
  | ⑥ | `git worktree list` 枚数 | **76 行**；本刀相关两行＝主树 `[codex/ui-main-window-redesign]` ＋ carrier `51f149d (detached HEAD)` |

- 判据文件格数当场重取（载体 51f149d 侧，`grep` 计数）：`[Fact]`＝**53**、`[Theory]`＝**1**、`[InlineData]`＝**3** ⇒ **53＋1×3＝56 枚格**（当前树＝56，不是波 13 当年的 49；本刀一切红集按 56 格盘推演，禁止旧树平移）。带属性方法＝**54** 枚（`public void` 24＋`public async Task` 30）；另有 2 枚 `public async Task<`＝夹具 `FakeNarrator.ProposeAsync`@130 与 `SelfCancellingNarrator.ProposeAsync`@874（不承载测试属性）。`Assert.Skip`＝**0** 处。判据文件 **989 行**。
- 判据 SHA 三面当场取（逐字符同值才起跑）：载体工作副本／主树工作副本／`git show 51f149d:` 的 blob 三面均＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`。
- 在册基线起点（从波 40 报告 §九.7 当场读，不从本单抄）：波 40 的 F 读数＝total **1406**／executed **1404**／failed **0**／跳过 **2**（跳过两枚＝`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`）。本刀净新增判据格 0；且本刀因 c 轮退回（见 §一.2）依预算第 4 行**不起 F**，故此起点仅作登记、不据以顶账。
- 取数仪器先自证再用（口径⓪；零 `dotnet`）：仪器＝`.local-notes/wave41-1029/w41-count.js`（照波 40 `w40-count.js` 同族）。对 5 面已知老读数当场重取（存档＝`out/instrument-selfproof.txt`），结论全 **AGREE（问题面 0 处）**：波 40 `trx-M8p`＝恰 5、`trx-m08`＝恰 4、`trx-F`＝求和 1406／1404／0／跳过开标签 2；波 13 旧 49 格树 `run-m08`＝恰 2（total 49）、`run-m07`＝恰 1。仪器对得上才敢裁新读数。

### 〇.1 写入通道（唯一允许写载体产品文件的通道）＝`.local-notes/wave41-1029/w41-apply.js`

六道守卫（照波 40 §〇.9 同族，本刀四枚锚点）：①目标落 `tests/**` 或树外 ⇒ REFUSED；②施加文本非本 ID 在 `mutation-prep/` 预合成那枚 ⇒ REFUSED；③施加前断言被替换行原文 === manifest OLD、且 OLD 整行出现恰 1 次 ⇒ 否则 ABORT 不写一字节；④施加前目标必须仍 === `blob(51f149d)`（上一轮没还原干净即 ABORT）；⑤写后 SHA 必须 === prep 声明值否则自动还原；⑥还原一律从 `git show 51f149d:<path>` 取字节再写文件（不用 checkout／restore）。

### 〇.2 件 1／件 2／件 3 成品（先档后跑，零 `dotnet`；原始输出＝`out/item1-2-3-prep.txt`）

四枚改前整行与载体 51f149d blob 逐字符并排（OLD 现值 === manifest OLD 各 `true`、OLD 整行出现次数各＝1、不跨行）；行数不变；净括号偏移守恒（不是括号数相等）＋全文件净圆括号／净大括号收支不变：

| 轮 | 文件:行 | OLD（载体现文，逐字符） | NEW（突变语义） | OLD→NEW 净括号 | 全文件净括号 | 全文件净大括号 | 行数 | 突变文本 SHA-256／字节 |
|---|---|---|---|---|---|---|---|---|
| a | `NarrativeDraftValidator.cs:139` | `        if (content.Actions.Count == 0)` | `        if (false)` | 0→0 | 0→0 | 0→0 | 421→421 | `de247f709afdca00788271cf35b8b2a4572156f3a6584539bb6c4eda20e7a48e`／19317 |
| b | 同上 `:287` | `        if (intent.Status is NarrativeIntentStatus.Fulfilled && content.Actions.All(a =>` | `        if (false && content.Actions.All(a =>` | 2→2 | 0→0 | 0→0 | 421→421 | `61615fb67bbe307f92aab7e2f0e13bbd327c231e8be4cfae6ff8e65957083f1c`／19295 |
| d | `NarratorContracts.cs:50` | `        if (!string.Equals(Assembly.ConversationId, Request.ConversationId, StringComparison.Ordinal))` | `        if (false)` | 0→0 | 0→0 | 0→0 | 125→125 | `9be9b71588df756e3a99d3046e5a337f93f50f45a19af91c8769d73fcec3fc8a`／5452 |
| e | `NarrativeRoundRunner.cs:244` | `        if (draft.OwnershipEpoch != request.OwnershipEpoch)` | `        if (false)` | 0→0 | 0→0 | 0→0 | 342→342 | `77d3231563ae818c8ecb9a81c28aa1cc506a97dfc6fb398296615514ca8cf300`／16727 |

每轮"施加后 SHA"必须逐字符等于本表那一行（工单 §三.0R 件 2）。

## 一 跑序、五轮配方与件 3′ 闭合点名表（**跑前落档；实测值不许回头改本节预期列**）

跑序固定＝a→b→c→d→e→全部还原并核 SHA→（F 的条件见预算第 4 行）。

### 一.1 c 枚＝经推演判"目标不可达⇒无法闭合"⇒ 退回定向额度、不起该轮、不跑、不作废（无 dotnet）

工单 §二.3／§三.0 给 c 的配方是：拆 `NarrativeDraftValidator.cs:235` 那道"延迟动作必须指着一枚意图"门（`if (action.DeferredIntentId is null)` ⇒ 恒假），目标格 #30 `Deferred_action_without_a_pointing_intent_is_refused`（载体行 @614）"必须响"。

**本刀从当前 56 格盘的可达性推演（这是推演，非实测）判该轮无法闭合**，逐枚依据：

1. 被改行 `:235` 是 `CheckAction` 里 Deferred 分支的 `if (action.DeferredIntentId is null)`，其**函数体**（`rejections.Add(… IntentClaimedAsFact "没有指向任何意图" …)` @236-238）只在"某条 Deferred 动作的 `DeferredIntentId` 为 null"时才执行。
2. 判据夹具 `DeferredAction(string intentId, …)`（@89-94）把 `DeferredIntentId = intentId` 设为**必填非空**实参；全 56 格里唯一走到 Deferred 分支的三枚——@503 `Deferred_round_registers_the_intent…`（`DeferredAction("intent-1")`＋intents 含 intent-1）、@599（`DeferredAction("intent-1")`＋Fulfilled intent）、@614（`DeferredAction("intent-1")`、intents 为空）——**没有一条 Deferred 动作的 DeferredIntentId 为 null**。⇒ `:235` 的函数体在本判据树下**从未被执行**（这是一道对当前格"常绿"的纵深防御分支，不是任何格的拒绝来源）。
3. 尤其 @614（c 的目标格）：其 `DeferredAction("intent-1")` 带**非空** DeferredIntentId，但本次提案 intents 为空 ⇒ 它实际拿到的拒绝来自 **`:240` 的 `else if (!content.Intents.Any(i => Equals(i.IntentId, "intent-1")))` 分支**（`IntentClaimedAsFact "指向的意图…不在本次提案里"` @242-243），而**非** `:235` 的 null 分支。格名"without a pointing intent"与它真正钉住的门（`:240`）不同名——这正是审计把 #30 归 D 类（"盘上从未拿到单枚单轮清洁红"）的成因。
4. 因此把 `:235` 改恒假后：`if(false){…null 分支…} else if(:240){…}` 里 `:240` 的 else-if 对 @614 仍成立 ⇒ 拒绝照旧 ⇒ @614 判定逐字段不变 ⇒ **目标格不响**；对其余格同样无改变（非空 intentId 者走 else-if、null intentId 者不存在）。⇒ 该突变的实到红集恒＝∅，无法写出"目标 @614 会红"的闭合表。

**处置（照工单 §三.0R 件 3′：闭合不了⇒不起那一轮、定向额度原样退回、判不清的面逐枚登记；禁止用"跑一把看响几枚"代替推演）**：c 轮**不跑**，定向额度退回 1（本刀定向实跑＝4 轮 a/b/d/e，非 5）。退回的判不清面登记如下，交守门人核：

- 判不清面①：`:235` 的 null 分支——要反证它，需要一个"Deferred 动作 DeferredIntentId 显式为 null"的新形状夹具，本判据树 56 格里没有；造这枚新格属"新增形状／夹具改造"（工单 §七 T4 类），且新增判据格不在本刀权限（§〇.2 零新格）。
- 判不清面②：c 的目标格 #30（@614）真正钉的是 `:240` 那道 else-if（"指向的意图不在本次提案里"），不是 `:235`；若要给 #30 拿单枚清洁红，配方应改指 `:240`（例如把 `:240` 的 `!content.Intents.Any(...)` 条件改恒假），那是**另一枚锚点**、不在工单 §三.0 的 c 配方里，须守门人另出配方。
- 本刀不替 c 追认、不重跑、不署名；c 的目标格 #30 记为"本刀未领账"（见 §未领账）。

### 一.2 因 c 退回 ⇒ 依预算第 4 行不起收尾全量 F（省一次弹窗）

工单 §四.4：若任一轮被作废或被件 3′ 退回 ⇒ 立即不起 F，把那次调用省下来。本刀 c 轮被件 3′ 退回 ⇒ **不起 F**，登记"跳过 F 的原因＝c 轮退回"。故本刀 `dotnet` 总调用＝定向实跑 4 轮＝4 枚（≤6）；解决方案级构建仍按三段判定跳过（≤1，动用 0）。

### 一.3 五枚格 universe 分组（非连坐判定的分母，逐枚可归组；当前 56 格）

- **A 组（仅调 `NarrativeContextCompiler.Compile`，不进 RunAsync／Contracts.Validate／DraftValidator）＝20 枚**：@183、@198、@210、@225、@237、@247、@256、@266、@281、@298、@313、@328、@347、@357、@367、@890、@913、@934、@952、@975。被改的四枚产品文件里，`NarrativeDraftValidator.cs`／`NarratorContracts.cs`／`NarrativeRoundRunner.cs` 一行都不执行（`NarrativeContextCompiler.cs` 本刀一枚未改）。
- **B 组（调 `RunAsync`，即经 NarrativeRoundRunner 全流程）＝30 枚**：@381、@394、@456、@471、@488、@503、@520、@534、@549、@562、@574、@587、@599、@614、@628、@644、@658、@671、@684、@698、@711、@722、@735、@746、@761、@776、@790、@802、@832、@851。
- **C 组（直接构造 `NarratorCall`/`NarratorCallBudget` 调 `Contracts.Validate` 或 budget）＝5 格**：@408（仅 budget，无 Validate）、@420（rejected 装配体→Contracts:36 抛）、@437 Theory 三行（instance/conversation/batch→Contracts:43/:50/:57）。
- **D 组（静态工具）＝1 枚**：@813 `Contiguous_split…`（只调 `NarrativeRoundRunner.SplitContiguous` 静态法，不跑 RunAsync）。

（20＋30＋5＋1＝56。以下每轮的"判不连坐"覆盖除目标外的全部 55 枚／53 枚分组。）

### 一.4 第 a 轮闭合点名表（`NarrativeDraftValidator.cs:139` `if(content.Actions.Count==0)` ⇒ `if(false)`）

机理（推演）：`:139` 在 `NarrativeDraftValidator.Validate`（@107）内，当草稿零动作时加一枚 `ActionBindingMissing` 拒绝。该方法经 `RunAsync:136` 进入（仅对 DraftProduced 且过了快照门的 B 组格），并经 `NarrativeCommitValidator.Validate:385` 复用（即时分支落账时对 commit content 复跑）。翻掉 `:139` ⇒ 只有"Validate 时 Actions.Count==0"的格判定才会变。

| 预期红（1 枚，逐枚点名） | 夹具如何走到 `:139` | 翻掉的断言（行号） | 预期消息族／形状 |
|---|---|---|---|
| `Draft_without_any_action_is_rejected_rather_than_read_as_silence`@520 | `Draft(actions: [])` ⇒ RunAsync 产草稿（FakeMode.Produce）→ 快照三项皆符（默认 epoch1/story7/base100）→ `Validate:136` 时 `content.Actions.Count==0` ⇒ 改前 `:139` 命中加 `ActionBindingMissing` ⇒ ContractRejected；改后 `if(false)` 不加、其余 foreach 空 ⇒ rejections=0 → immediate/deferred 皆空 → `:167` 落 **Silence** | `:526 Assert.Equal(ContractRejected, result.Disposition)` 即翻（其后 `:527/:528` 同格不计第二枚） | `Assert.Equal() … Expected: ContractRejected / Actual: Silence` |

判不连坐（其余 55 枚逐组，可达路径级）：
- A 组 20 枚：只 `Compile`，从不进 `NarrativeDraftValidator.Validate`（grep 判据对 `NarrativeDraftValidator` 的直接调用＝0，仅 D 组注释文本提及）。`:139` 不可达。
- D 组 @813：只调 `SplitContiguous` 静态法。`:139` 不可达。
- C 组 5 格：@408 只 budget；@420/@437 调的是 `NarratorCall.Validate`（Contracts），非 `NarrativeDraftValidator`。`:139` 不可达。
- B 组非目标 29 枚：其草稿 `Actions.Count ≥ 1`（夹具默认 `[SilenceAction()]` 或显式 ≥1 条动作）⇒ 改前 `content.Actions.Count==0` 恒假、改后 `if(false)` 恒假，`:139` 两支都不加拒绝，判定逐字段不变。其中 @534/@549/@562/@574/@587/@851 六枚＝非 DraftProduced（Malformed/超时/取消/故障/自取消），在 `RunAsync:110` 早返回，根本不进 `Validate`；@394 在 `:95` ContextRejected 早返回，同样不进 `Validate`；@746（prose 空白）Actions.Count==1（`[ImmediateAction()]`）⇒ `:139` 不命中，其两枚拒绝来自 `:131` NarrativeProseMissing 与 CheckAction 绑定，均不读 `:139`。
- 判定门＝**恰 1**：实到≠1 ⇒ 整轮作废、@520 不签、不重跑第二轮、不追认（件 4）。

### 一.5 第 b 轮闭合点名表（`NarrativeDraftValidator.cs:287` 把 `intent.Status is NarrativeIntentStatus.Fulfilled` 换恒假、不改续行）

机理（推演）：`:287` 在 `NarrativeIntentPolicy.Validate`（@263）内，仅当"意图 Status==Fulfilled 且无任一即时动作把它投递"时加 `RealityContamination` 拒绝。该方法经 `NarrativeDraftValidator.Validate:152` 对 `content.Intents` 逐条进入（B 组 DraftProduced 格），改后首操作数恒假 ⇒ `false && content.Actions.All(...)` 整条件恒假 ⇒ 该拒绝永不再加。翻红只可能发生在"原本会命中 Fulfilled 分支"的格。

| 预期红（1 枚） | 夹具如何走到 `:287` 且改前命中 | 翻掉的断言 | 预期消息族 |
|---|---|---|---|
| `Deferred_intent_that_claims_to_be_fulfilled_without_delivery_is_refused`@599 | `Draft(actions:[DeferredAction("intent-1")], intents:[PendingIntent(status: Fulfilled)])` → 快照符 → `Validate:136` → 动作走 Deferred 分支通过（DeferredIntentId 非空且 intent-1 在 intents 里）→ `:152` 进 intent 政策 → `:287` 改前：Status==Fulfilled 真 且 `All(a => a.Mode!=Immediate || …)` 真（唯一动作是 Deferred）⇒ 加 `RealityContamination` ⇒ ContractRejected；改后：首操作数恒假 ⇒ 不加 ⇒ rejections=0 → deferred 支 `:158` 落 **Deferred** | `:606 Assert.Equal(ContractRejected, result.Disposition)` 即翻（其后 `:607/:608` 同格不计） | `Assert.Equal() … Expected: ContractRejected / Actual: Deferred` |

判不连坐（其余 55 枚）：
- A 组 20 枚／D 组 @813／C 组 5 格：不进 `NarrativeDraftValidator.Validate` ⇒ `:287` 不可达（见上 §一.4 同条 grep 凭据）。
- B 组另 28 枚：
  - @503、@832 两枚带 intents，但 `PendingIntent()` 默认 `status: Pending` ⇒ 改前 `intent.Status is Fulfilled` 恒假（`:287` 不命中）、改后仍恒假 ⇒ 判定不变。
  - 其余 26 枚 B 组＝`Draft()` 默认 `intents: []` ⇒ `Validate:150` 的 `foreach (intent in content.Intents)` 空循环，`:287` 不可达。
  - 非 DraftProduced 六枚＋@394 早返回，同 §一.4。
- 判定门＝**恰 1**：实到≠1 ⇒ 整轮作废、@599 不签。

### 一.6 第 d 轮闭合点名表（`NarratorContracts.cs:50` `if(!Equals(Assembly.ConversationId, Request.ConversationId))` ⇒ `if(false)`）

配方选择（工单 §二.4"两选一"）：**取"换恒假 `if(false)`"，不取"去掉取反（同会话也拒）"**。理由（推演）：`NarratorCall.Validate` 经 `RunAsync:105` 对**每一枚**过 ContextRejected 门的 B 组格执行；编译出的装配体 `ConversationId` 恒等于 `request.ConversationId`（`NarrativeContextCompiler.cs:171` 当场读到 `ConversationId = request.ConversationId`）。若"去掉取反"＝把门反过来成 `if(Equals(...))`，则**所有 B 组格**在 `:105` 都会因"同会话"被抛 `CallScopeMismatch` ⇒ 连坐约二十余枚，红集不闭合、无法逐枚预判 ⇒ 违背件 3′。取"恒假"只摘掉这一道跨会话拒绝、不新增抛点 ⇒ B 组格（会话本就相等、该门改前也不抛）零改变，只有显式构造"会话不匹配"的 @437 conversation 行受影响。故取恒假。

| 预期红（1 格＝Theory 的 conversation 行） | 夹具如何走到 `:50` 且改前命中 | 翻掉的断言 | 预期消息族 |
|---|---|---|---|
| `Call_refuses_an_assembly_that_belongs_to_another_scope(scope: "conversation")`@437 | `assembly with { ConversationId = "room-2" }` 而 `Request().ConversationId="room-1"`；`new NarratorCall{…}.Validate()`：改前 `:50` `!Equals(room-2,room-1)`＝真 ⇒ 抛 `CallScopeMismatch` ⇒ `Assert.Throws` 通过；改后 `if(false)`：`:43` 实例相等（Alpha）不抛、`:50` 恒假不抛、`:57` 批次相等（Batch）不抛 ⇒ **Validate 正常返回不抛** ⇒ `Assert.Throws` 收不到异常 | `:447 Assert.Throws<NarrativeContextAssemblyRejectedException>(…)` 失败（"no exception thrown"族） | 断言级红：Assert.Throws 未捕获到预期异常（testName 参数表＝`scope: "conversation"`） |

判不连坐（其余，含另两行逐枚）：
- @437 `scope:"instance"` 行：`assembly with { InstanceId = beta }` ⇒ `:43` `!Equals(beta, alpha)` 真 ⇒ 改前/改后**都在 `:43` 先抛**（`:50` 在其后，永不及）⇒ 该格仍通过。不连坐＝抛点在 `:43`，早于被改的 `:50`。
- @437 `scope:"batch"` 行：`InstanceId`/`ConversationId` 都与 request 相等（room-1==room-1 ⇒ `:50` 改前也不抛）、仅 `BatchId=batch-2` ⇒ `:57` 抛 ⇒ 仍通过。不连坐＝该格从未依赖 `:50` 抛异常。
- @420 `Call_refuses_an_assembly_that_still_carries_a_rejection`：喂 rejected 装配体 ⇒ `:36 !Assembly.Accepted` 先抛（WindowTooSmall），到不了 `:50`。不连坐＝抛点在 `:36`。
- @408：只 `NarratorCallBudget.Consume()`，无 `NarratorCall.Validate`。`:50` 不可达。
- B 组 30 枚：经 `RunAsync:105` 调 `Validate`，但装配体 `ConversationId` 恒等 `request.ConversationId`（`:171`）⇒ `:50` 改前 `!Equals(同,同)` 恒假不抛、改后恒假不抛 ⇒ 逐格判定不变。
- A 组 20 枚／D 组 @813：不进 `Contracts.Validate`。
- 判定门＝**恰 1（conversation 行）**：实到≠1 或另两行任一连坐 ⇒ 整轮作废、不签。

### 一.7 第 e 轮闭合点名表（`NarrativeRoundRunner.cs:244` `if(draft.OwnershipEpoch != request.OwnershipEpoch)` ⇒ `if(false)`）

机理（推演）：`:244` 在 `CheckSnapshotFreshness`（@236，private static）内，经 `RunAsync:121` 对每枚 DraftProduced 的 B 组格调用；比对顺序＝`:238` 故事版本 → `:244` 所有权代次 → `:250` 签发基线，任一命中即 `return` 该拒绝。改恒假 ⇒ 只摘掉"代次不符"这一支拒绝。翻红只发生在"原本靠 `:244` 抛 OwnershipFenced"的格。

| 预期红（1 枚） | 夹具如何走到 `:244` 且改前命中 | 翻掉的断言 | 预期消息族 |
|---|---|---|---|
| `Draft_from_a_fenced_writer_epoch_is_refused`@790 | `Draft(…, ownershipEpoch: 1)` 对 `Request(ownershipEpoch: 2)`；故事版本 7==7 ⇒ `:238` 不命中 → `:244` 改前 `1 != 2` 真 ⇒ 返回 `OwnershipFenced` ⇒ ContractRejected（`Rejections=[snapshot]`）；改后 `if(false)` ⇒ `:244` 不命中、`:250` 基线 100==100 不命中 ⇒ snapshot=null → 走绑定+校验（ImmediateAction 合法）⇒ rejections=0 → 即时分支落 **Immediate** | `:796 Assert.Single(result.Rejections)` 即翻（空集合上 Single 失败；其后 `:797` 同格不计） | `Assert.Single() … Expected: 1 item / Actual: 0 items`（断言级红；红点行 Release 序列点或落多行调用起始行，归属看 testName＋消息） |

判不连坐（其余 55 枚）：
- @776 `Draft_on_a_stale_story_version`：`storyVersionBase=6` 对 request 7 ⇒ `:238` **先命中并 return**，`RunAsync:121` 拿到 snapshot 非空即早返回，`:244` 根本执行不到。不连坐＝短路点在 `:238`，早于被改的 `:244`。
- @802 `Draft_with_a_wrong_issue_baseline`：epoch 1==1、story 7==7 都不命中，仅 `receivedBaseline=90≠100` ⇒ `:250` 命中 LateCommitRejected。`:244` 改前也不命中（相等）⇒ 判定不变。
- B 组另 27 枚：均 `ownershipEpoch` 相等（Draft 默认 1 ＝ Request 默认 1；无第二处 `Request(ownershipEpoch:2)`，grep 仅 @794 一处）⇒ `:244` 改前 `1!=1` 假、改后假 ⇒ 逐字段不变。其中非 DraftProduced 六枚（@534/@549/@562/@574/@587/@851）在 `:110` 早返回、不进 `CheckSnapshotFreshness`；@394 在 `:95` 早返回。
- A 组 20 枚／C 组 5 格／D 组 @813：不进 `CheckSnapshotFreshness`（private static，只经 RunAsync）。
- 判定门＝**恰 1**：实到≠1 ⇒ 整轮作废、@790 不签。

### 一.8 五轮合计（跑前）与"闭合"自证声明

- 实跑四轮预期红集合＝{a:1}／{b:1}／{d:1}／{e:1}；每轮判不连坐均给到可达路径级理由，无一写"应该不影响"，无一处"各恰 N"合并写法。
- c 轮：件 3′ 判不闭合 ⇒ 退回定向额度、不跑（见 §一.1）。
- 要领账的波 13 D 类格（若各轮实到等表）：#23（@520，a）＋#29（@599，b）＋#17 的 conversation 行（@437，d）＋#43（@790，e）＝4 枚；#30（@614，c）**本刀不领**（目标不可达，退回）。
- 每轮 `total` 应当＝**56**（不是 49）。

## 二 逐轮读数（每轮跑完立刻追加；预期列在 §一，本节不回头改）

口径：每轮记满十一项①被改文件:行／②施加前盘上态／③施加后盘上态／④`git diff --stat`／⑤`dotnet test` 末行原样／⑥`.trx` 文件名／⑦红格全名＋消息要义＋红点行号／⑧还原命令／⑨还原后 `status --short` 行数／⑩突变态与还原后 SHA＋本轮判据 SHA／⑪本轮预检段。红数按 §〇 取数仪器只认 `<UnitTestResult … outcome="Failed">` 开标签，并与同份 `<Counters failed>` 双向对账；`<ResultSummary outcome="Failed">` 不算用例（每轮 `rawOutcomeFailedStrings − openTagFailed ＝ 1`）。

### 二.1 第 a 轮（`src/Pry.Core/Narrative/NarrativeDraftValidator.cs:139`，`if (content.Actions.Count == 0)` → `if (false)`）

| 项 | 当场值 |
|---|---|
| ① 被改面 | `NarrativeDraftValidator.cs:139`，OLD 行由 `w41-apply.js` 施加前当场逐字符断言相等后放行 |
| ② 施加前盘上态 | `git -C carrier status --short`＝**0 行**；判据 SHA 本轮施加前当场打印＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`（§〇 三面同值面已当场取一次，本节 §二.1 ⑩ 再取一次）；toplevel 当场打印＝`…/.local-notes/qa-wave33-1028/carrier`（每次写文件前必打） |
| ③ 施加后盘上态 | `APPLIED W41-a src/Pry.Core/Narrative/NarrativeDraftValidator.cs :139 sha=de247f709afdca00788271cf35b8b2a4572156f3a6584539bb6c4eda20e7a48e bytes=19317`；`status --short`＝**1 行**＝` M src/Pry.Core/Narrative/NarrativeDraftValidator.cs`；`git diff --unified=0` 只显示那一行 `-        if (content.Actions.Count == 0)` / `+        if (false)` |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/NarrativeDraftValidator.cs \| 2 +-` ⇒ `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤ `dotnet test` 末行原样 | `失败!  - 失败:     1，通过:    55，已跳过:     0，总计:    56，持续时间: 300 ms - Pry.Core.Tests.dll (net10.0)` |
| ⑥ `.trx` | `.local-notes/wave41-1029/out/trx-a/24782_PHYTHLINK_2026-10-29_10_55_33.trx`；运行日志＝`out/run-a.txt`；仪器读数＝`out/reading-a.txt` |
| ⑦ 红集（1 枚） | `Pry.Core.Tests.NarratorPrototypeTests.Draft_without_any_action_is_rejected_rather_than_read_as_silence`；红点行＝`:526`；消息族＝`Assert.Equal() Failure: Values differ \| Expected: ContractRejected \| Actual:   Silence`；`family=ASSERT-RED`（仪器分类） |
| ⑧ 还原命令 | `node .local-notes/wave41-1029/w41-apply.js revert --id=W41-a`（通道内部＝从 `git show 51f149d:src/Pry.Core/Narrative/NarrativeDraftValidator.cs` 取字节再写文件） |
| ⑨ 还原后 | `git -C carrier status --short`＝**0 行**（当场数，未合并到 §三.2） |
| ⑩ SHA 对账 | 还原后 `NarrativeDraftValidator.cs` SHA＝`88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c`（＝`51f149d` 的 blob，通道打印 `sameAsBlob=true`）；本轮还原后当场 `sha256sum` 判据文件＝`4f1b2f6e…`（逐字符等于 §〇 三面基准） |
| ⑪ 预检段 | **P112**（`out/preflight-P112.txt`）：`VERDICT=BUILD_OK`；`FOREGROUND: ChatGPT pid=16076 class=Chrome_WidgetWin_1 rect=540,200,1445x811 screen=2560x1440 covers=False borderless=False title=ChatGPT`；`gpu_util=18% used=1273MB`；`free_memory_gb=14.2`；HOLD 重跑 0 |

对账与裁决面（仪器 `w41-count.js` 原样，全部 `inJudge=YES(NarratorPrototypeTests.cs)`、`family=ASSERT-RED`）：

- 仪器行＝`nodes=56 total=56 executed=56 passed=55 failed(counters)=1 notExecuted(attr)=0 rawOutcomeFailedStrings=2 altWayNodes=1 openTagFailed=1`；`NotExecuted 开标签=（0 枚）`；结论＝**AGREE（问题面 0 处）**；三向同值（1＝1＝1），第 2 处字面量＝`<ResultSummary outcome="Failed">`，不是格。
- 实到＝**恰 1**＝§一.4 闭合点名表的 { @520 } ⇒ 本轮有效；要领账的＝波 22 审计 §四.1 T3 表的 #23 `Draft_without_any_action_is_rejected_rather_than_read_as_silence`。
- 累计：`dotnet` 1/6、定向 1/5（额度动用，另见 §一.2 c 退回 ⇒ 实际定向使用 4/5、退回 1/5）、构建额度 0/1、HOLD 0/16。

### 二.2 第 b 轮（`src/Pry.Core/Narrative/NarrativeDraftValidator.cs:287`，把 `intent.Status is NarrativeIntentStatus.Fulfilled` 换恒假、不改续行）

| 项 | 当场值 |
|---|---|
| ① 被改面 | `NarrativeDraftValidator.cs:287`；OLD 行由通道施加前当场逐字符断言相等后放行 |
| ② 施加前盘上态 | 载体 `status --short`＝**0 行**（本轮施加前未再单独打印判据 SHA；`status=0`＋载体 HEAD=`51f149d` ⇒ 全部跟踪文件与 blob 逐字节同值＝§〇.4 已三面自证）；通道自带的 `DIRTY-BEFORE-APPLY` 闸对目标文件二次 SHA 核验通过后才写 |
| ③ 施加后盘上态 | `APPLIED W41-b src/Pry.Core/Narrative/NarrativeDraftValidator.cs :287 sha=61615fb67bbe307f92aab7e2f0e13bbd327c231e8be4cfae6ff8e65957083f1c bytes=19295`；`status --short`＝**1 行**（同一枚产品文件）；`git diff --unified=0` 只显示那一行 `-        if (intent.Status is NarrativeIntentStatus.Fulfilled && content.Actions.All(a =>` / `+        if (false && content.Actions.All(a =>` |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/NarrativeDraftValidator.cs \| 2 +-` ⇒ `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤ 末行原样 | `失败!  - 失败:     1，通过:    55，已跳过:     0，总计:    56，持续时间: 228 ms - Pry.Core.Tests.dll (net10.0)` |
| ⑥ `.trx` | `out/trx-b/24782_PHYTHLINK_2026-10-29_10_56_01.trx`；运行日志＝`out/run-b.txt`；仪器读数＝`out/reading-b.txt` |
| ⑦ 红集（1 枚） | `Deferred_intent_that_claims_to_be_fulfilled_without_delivery_is_refused`；红点行＝`:606`；消息族＝`Assert.Equal() Failure: Values differ \| Expected: ContractRejected \| Actual:   Deferred`；`family=ASSERT-RED` |
| ⑧ 还原命令 | `… revert --id=W41-b` |
| ⑨ 还原后 | `status --short`＝**0 行**（当场数） |
| ⑩ SHA 对账 | 还原后 `NarrativeDraftValidator.cs`＝`88ebf5c3…`，`sameAsBlob=true`；本轮还原后**单独打印**判据 SHA＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`（当场 sha256sum 输出） |
| ⑪ 预检段 | **P113**（`out/preflight-P113.txt`）：`VERDICT=BUILD_OK`；`FOREGROUND: ChatGPT pid=16076 … covers=False borderless=False`；`gpu_util=7% used=1365MB`；`free_memory_gb=14.1`；HOLD 0 |

- 仪器行＝`nodes=56 total=56 executed=56 passed=55 failed(counters)=1 … openTagFailed=1`，结论 AGREE；三向同值 1＝1＝1，第 2 处＝`<ResultSummary>`。
- 实到＝**恰 1**＝§一.5 表 { @599 } ⇒ 本轮有效；要领账的＝波 22 审计 §四.1 T3 表的 #29 `Deferred_intent_that_claims_to_be_fulfilled_without_delivery_is_refused`。
- 关键判不连坐面本轮实测支持（不是推翻）：@503（Pending intent）与 @832（Mixed round，Pending intent）全绿，说明"首操作数 `intent.Status is Fulfilled` 在这两枚夹具下改前本就假"这条推演与盘上一致；@614（Deferred 动作但 intents 为空）也全绿，说明"它钉的其实是 `:240` else-if，与 `:287` 无涉"（与 §一.1 对 c 轮的分析同向）。
- 累计：`dotnet` 2/6、定向 2/4（另 2 轮 d/e）、构建 0/1、HOLD 0/16。

### 二.3 第 c 轮＝**未跑**（件 3′ 闭合不了、定向额度原样退回，见 §一.1）

- 无 ①–⑪ 任何读数——本刀**没跑**这一轮，没有对应的 `.trx`、`run-*.txt`、`reading-*.txt` 或预检段。
- 定向额度：退回 1/5；c 轮的配方目标 #30 `Deferred_action_without_a_pointing_intent_is_refused`（@614）＝**本刀未领账**，见 §未领账。
- 依工单 §四.4 那条"任一轮被件 3′ 退回 ⇒ 立即不起 F"：本刀不起收尾全量 F，省一次弹窗（另见 §四）。

### 二.4 第 d 轮（`src/Pry.Core/Narrative/NarratorContracts.cs:50`，`if (!string.Equals(Assembly.ConversationId, Request.ConversationId, StringComparison.Ordinal))` → `if (false)`）

配方选择＝恒假（另一选择"去掉取反"理由与不取论证见 §一.6）。

| 项 | 当场值 |
|---|---|
| ① 被改面 | `NarratorContracts.cs:50`；OLD 行由通道施加前当场逐字符断言相等后放行 |
| ② 施加前盘上态 | 载体 `status --short`＝**0 行**；toplevel 当场打印＝`…/.local-notes/qa-wave33-1028/carrier`；判据 SHA 本轮**未单独再打**（当场凭据＝0 行＋`51f149d` HEAD 的 blob 同值面，最近一次单独打印＝§二.2 ⑩ 的还原后 sha256sum `4f1b2f6e…`；`DIRTY-BEFORE-APPLY` 闸对目标文件二次核验后才写） |
| ③ 施加后盘上态 | `APPLIED W41-d src/Pry.Core/Narrative/NarratorContracts.cs :50 sha=9be9b71588df756e3a99d3046e5a337f93f50f45a19af91c8769d73fcec3fc8a bytes=5452`；`status --short`＝**1 行**＝` M src/Pry.Core/Narrative/NarratorContracts.cs`（`git diff --unified=0` 只显示那一行） |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/NarratorContracts.cs \| 2 +-` ⇒ `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤ 末行原样 | `失败!  - 失败:     1，通过:    55，已跳过:     0，总计:    56，持续时间: 231 ms - Pry.Core.Tests.dll (net10.0)` |
| ⑥ `.trx` | `out/trx-d/24782_PHYTHLINK_2026-10-29_10_56_24.trx`；运行日志＝`out/run-d.txt`；仪器读数＝`out/reading-d.txt` |
| ⑦ 红集（1 枚，Theory 参数行） | `Call_refuses_an_assembly_that_belongs_to_another_scope(scope: "conversation")`；红点行＝`:447`；消息族＝`Assert.Throws() Failure: No exception was thrown \| Expected: typeof(Pry.Core.Narrative.NarrativeContextAssemblyRejectedException)`；`family=ASSERT-RED` |
| ⑧ 还原命令 | `… revert --id=W41-d` |
| ⑨ 还原后 | `status --short`＝**0 行**（当场数） |
| ⑩ SHA 对账 | 还原后 `NarratorContracts.cs`＝`3a33a8151e61c3396963dac451dffcb46acb2f02807f140d18f694d6a52a9f8c`（＝§〇 与 `51f149d` 侧 blob 同值），`sameAsBlob=true`；本轮还原后的判据 SHA **未单独再打**（当场凭据＝`status=0 行`＋通道 `REVERTED … sameAsBlob=true` 只写产品文件；最近一次单独打印＝§二.2 ⑩ 的 `4f1b2f6e…`；§三.2 出场面再打一次） |
| ⑪ 预检段 | **P114**（`out/preflight-P114.txt`）：`VERDICT=BUILD_OK`；`FOREGROUND: QQ pid=27508 class=Chrome_WidgetWin_1 rect=-1825,234,993x802 screen=1920x1080 covers=False borderless=False title=QQ`（前台窗在第二块屏外侧，不遮主屏）；`gpu_util=12% used=1293MB`；`free_memory_gb=13.9`；HOLD 0 |

- 仪器行＝`nodes=56 total=56 executed=56 passed=55 failed(counters)=1 … openTagFailed=1`，结论 AGREE；三向同值 1＝1＝1。
- 实到＝**恰 1（conversation 行）**＝§一.6 表 ⇒ 本轮有效；要领账的＝波 22 审计 §四.1 T3 表的 #17 中 `Call_refuses_an_assembly_that_belongs_to_another_scope(scope: "conversation")` 那一行。
- 另两行未连坐被本轮实测**支持**（同一次跑里 instance/batch 两行都在 55 枚绿中）：这与 §一.6 推演一致——instance 行的抛点在 `:43`、batch 行的抛点在 `:57`，都早于／不同于被改的 `:50`。
- 累计：`dotnet` 3/6、定向 3/4（含退回的 c 一轮不占 dotnet）、构建 0/1、HOLD 0/16。

### 二.5 第 e 轮（`src/Pry.Core/Narrative/NarrativeRoundRunner.cs:244`，`if (draft.OwnershipEpoch != request.OwnershipEpoch)` → `if (false)`）

| 项 | 当场值 |
|---|---|
| ① 被改面 | `NarrativeRoundRunner.cs:244`；OLD 行由通道施加前当场逐字符断言相等后放行 |
| ② 施加前盘上态 | 载体 `status --short`＝**0 行**；toplevel 当场打印＝`…/.local-notes/qa-wave33-1028/carrier`；判据 SHA 本轮**未单独再打**（当场凭据＝`status=0` 面，最近一次单独打印＝§二.2 ⑩ 的 `4f1b2f6e…`） |
| ③ 施加后盘上态 | `APPLIED W41-e src/Pry.Core/Narrative/NarrativeRoundRunner.cs :244 sha=77d3231563ae818c8ecb9a81c28aa1cc506a97dfc6fb398296615514ca8cf300 bytes=16727`；`status --short`＝**1 行**＝` M src/Pry.Core/Narrative/NarrativeRoundRunner.cs` |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/NarrativeRoundRunner.cs \| 2 +-` ⇒ `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤ 末行原样 | `失败!  - 失败:     1，通过:    55，已跳过:     0，总计:    56，持续时间: 221 ms - Pry.Core.Tests.dll (net10.0)` |
| ⑥ `.trx` | `out/trx-e/24782_PHYTHLINK_2026-10-29_10_56_50.trx`；运行日志＝`out/run-e.txt`；仪器读数＝`out/reading-e.txt` |
| ⑦ 红集（1 枚） | `Draft_from_a_fenced_writer_epoch_is_refused`；红点行＝`:796`；消息族＝`Assert.Single() Failure: The collection was empty`；`family=ASSERT-RED` |
| ⑧ 还原命令 | `… revert --id=W41-e` |
| ⑨ 还原后 | `status --short`＝**0 行**（当场数；§三.2 再复数一次） |
| ⑩ SHA 对账 | 还原后 `NarrativeRoundRunner.cs`＝`961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9`（＝`51f149d` blob），`sameAsBlob=true`；本轮还原后的判据 SHA 未单独再打（当场凭据＝`status=0`＋通道只写产品文件；§三.2 出场面再打一次） |
| ⑪ 预检段 | **P115**（`out/preflight-P115.txt`）：`VERDICT=BUILD_OK`；`FOREGROUND: Godot pid=42252 class=Engine rect=238,452,956x599 screen=2560x1440 covers=False borderless=False title=???? ? ???? (DEBUG)`（非全屏无边框，标题 UTF-8→GBK 显示乱码属预检脚本输出侧字符集形状，不改判据）；`gpu_util=8% used=1362MB`；`free_memory_gb=11.6`；HOLD 0 |

- 仪器行＝`nodes=56 total=56 executed=56 passed=55 failed(counters)=1 … openTagFailed=1`，结论 AGREE；三向同值 1＝1＝1。
- 实到＝**恰 1**＝§一.7 表 { @790 } ⇒ 本轮有效；要领账的＝波 22 审计 §四.1 T3 表的 #43 `Draft_from_a_fenced_writer_epoch_is_refused`。
- 关键判不连坐面：@776（story version 不符 ⇒ `:238` 先 return，`:244` 不可达）、@802（received baseline 不符 ⇒ `:250` 命中，`:244` 恒不命中）本轮全绿；其它 25 枚 DraftProduced 格 epoch 相等（1==1）⇒ 判定不变。
- 累计（四轮跑完）：`dotnet` 4/6、定向 4/4 实跑＋1 退回＝5/5 全额处置、解决方案级构建 0/1、HOLD 0/16、预检 P112–P115 共 4 枚全 BUILD_OK。

## 三 还原与 SHA 核（四轮跑完、F 之前先核；本轮不起 F，见 §四）

### 三.1 还原动作逐枚（还原命令一律＝从 `git show 51f149d:<path>` 取字节再写文件，不用 checkout／restore）

| 轮 | 通道打印 |
|---|---|
| a | `REVERTED W41-a src/Pry.Core/Narrative/NarrativeDraftValidator.cs sha=88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c sameAsBlob=true` |
| b | `REVERTED W41-b src/Pry.Core/Narrative/NarrativeDraftValidator.cs sha=88ebf5c3… sameAsBlob=true` |
| d | `REVERTED W41-d src/Pry.Core/Narrative/NarratorContracts.cs sha=3a33a815… sameAsBlob=true` |
| e | `REVERTED W41-e src/Pry.Core/Narrative/NarrativeRoundRunner.cs sha=961fb49b… sameAsBlob=true` |

- 每轮还原后当场数 `git -C carrier status --short`＝**0 行**（四轮各自值见 §二.1–§二.5 的第⑨项）。
- 载体 `status --short` 全程最多 1 行且那一行永远是 `src/` 产品文件；从未出现 `tests/**`（`tests/Pry.Core.Tests/NarratorPrototypeTests.cs` 一字未写＝过程面凭据）。

### 三.2 出场 SHA 三面核（`git show 51f149d:<path>` 的 blob vs 载体工作副本 vs 主树工作副本）

| 文件 | blob(51f149d) SHA-256 | 载体 | 主树 | 三面 |
|---|---|---|---|---|
| `src/Pry.Core/Narrative/NarrativeDraftValidator.cs` | `88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c` | 同值 | 同值 | MATCH |
| `src/Pry.Core/Narrative/NarratorContracts.cs` | `3a33a8151e61c3396963dac451dffcb46acb2f02807f140d18f694d6a52a9f8c` | 同值 | 同值 | MATCH |
| `src/Pry.Core/Narrative/NarrativeRoundRunner.cs` | `961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9` | 同值 | 同值 | MATCH |
| `tests/Pry.Core.Tests/NarratorPrototypeTests.cs`（判据） | `4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4` | 同值 | 同值 | MATCH |

原始打印＝`.local-notes/wave41-1029/out/final-sha.txt`（当场一次 sha256sum，非转录）。

### 三.3 载体终态与主树终态

- 载体 `git -C carrier status --short`＝**0 行**（当场数）。
- 载体 `git -C carrier log --oneline fd93ebe..HEAD`＝**2 行**（`51f149d`、`1e02205`＝波 33 那两枚既有提交，本刀零 git 写操作）。
- 载体 `git -C carrier rev-parse HEAD`＝`51f149d0b722e28a6566c93d19f48a94ca4c85df`（同入场值）。
- 主树 `git status --short --branch`＝**10 行**（1 行 branch＋两枚 `M ` 暂存项＋一枚 ` M`＋六枚 `??`）；与入场 9 行的差＝本报告那枚 `??`（工单 §〇.8 允许的唯一新建文件，未提交、未 `git add`）。他人三枚暂存项与其余五枚未跟踪资产逐字未动。

## 四 F（收尾全量）＝**未起**，登记跳过理由

- 依工单 §四.4：**若任一轮被作废或被件 3′ 退回 ⇒ 立即不起 F**。本刀 c 轮被件 3′ 退回（§一.1）⇒ **F 不起**，省一次 `dotnet` 调用。
- 因此本刀没有 F 的 `.trx`、`run-F.txt`、`reading-F.txt`、`preflight-P1xx` 段。
- F 未起 ⇒ 在册基线（波 40 §九.7 那份 total 1406／executed 1404／failed 0／跳过 2）本刀**不复采、不顶账、不移动**。

## 五 预算与预检台账

### 五.1 四类预算的动用（上限／动用／说明）

| 面 | 上限 | 动用 | 说明 |
|---|---|---|---|
| 解决方案级 `dotnet build` | ≤1 | **0** | 合法跳过（三段判定见 §五.2） |
| 解决方案级全量 `dotnet test Pry.slnx -c Release` | ≤1 | **0**（F 未起） | 依 §四.4"任一轮被件 3′ 退回 ⇒ 立即不起 F" |
| 定向 | ≤5 | **4 实跑＋1 退回** | a/b/d/e 各跑 1 枚；c 依件 3′ 判不闭合、未跑、定向额度原样退回（不重跑、不追认） |
| `dotnet` 总调用 | ≤6 | **4** | 定向 4；无解决方案级构建、无 F |
| HOLD 重跑 | ≤16 | **0** | P112–P115 四枚预检全 BUILD_OK，未动用等待重试 |

### 五.2 构建跳过三段判定（当场复取，不引用工单或波 40 的旧述）

1. **0 行式子成立**：`git diff --stat 51f149d 88a642b -- src tests scripts Pry.slnx Directory.Build.props`＝**0 行**（当场取；原始输出＝`out/seg1-0line.txt`；`88a642b`＝主树入场时 `git rev-parse HEAD`）。⇒ 载体树（基点 `51f149d`）与当前主树的相关字节逐字符相同，无需另起一次解决方案级构建来"对齐"。
2. **成品在盘**：`.local-notes/qa-wave33-1028/carrier/src/Pry.Core/bin/Release/net10.0/Pry.Core.dll` 存在，mtime 10-29 10:56（本刀四轮 `dotnet test` 自带增量构建的最新一次刷新）。
3. **SHA 未变**：四枚被改文件（三枚产品＋判据）在四轮还原后的三面 SHA 逐字符等于 §三.2 入场基准；载体 `git diff --stat`＝**0 行**（原始输出＝`out/seg3-carrier-diff.txt`）。

三段齐 ⇒ 跳过成立。**旁证面**：本刀运行日志里 `dotnet test` 命令行 **0 处**含 `--no-build`（工单 §一.7 明令突变轮不许带），四轮全部由 `dotnet test` 自带增量编译完成（每份 `run-*.txt` 前段可见 `Pry.Core → …dll` 与 `Pry.Core.Tests → …dll` 的产出行）。

### 五.3 预检台账（编号／VERDICT／归档／前台窗／内存／放行哪一轮）

| 编号 | VERDICT | 归档路径 | 前台窗 pid／class／covers／borderless | `free_memory_gb` | 放行 |
|---|---|---|---|---|---|
| P112 | BUILD_OK | `out/preflight-P112.txt` | 16076／`Chrome_WidgetWin_1`／False／False（ChatGPT，第二屏外） | 14.2 | 第 1 轮 a |
| P113 | BUILD_OK | `out/preflight-P113.txt` | 16076／`Chrome_WidgetWin_1`／False／False | 14.1 | 第 2 轮 b |
| P114 | BUILD_OK | `out/preflight-P114.txt` | 27508／`Chrome_WidgetWin_1`／False／False（QQ，第二屏外 rect=-1825,234） | 13.9 | 第 4 轮 d（c 未跑） |
| P115 | BUILD_OK | `out/preflight-P115.txt` | 42252／`Engine`／False／False（Godot 编辑窗，主屏 2560×1440，非全屏） | 11.6 | 第 5 轮 e |

- 对账：**逐份 VERDICT 枚数 4＝`dotnet` 次数 4＝定向实跑轮 4**（工单 §三.0R 件 5、§五.3 三方对账）；HOLD 0 份；无"跳过预检起跑"的形状；四轮按 a→b→d→e 顺序各跑前一枚预检，中间无重复无跨轮复用。
- P115 前台窗是 Godot 编辑器（非全屏非 borderless，`covers=False`），预检脚本 v6 的判据未将其 HOLD；本刀按工单 §〇.4 那条"以 `VERDICT` 为准"起跑，不自行解释分类。P112/P113/P114 三枚前台窗都在第二块屏外侧（`covers=False borderless=False`），与放行同向。

### 五.4 产物枚数

- 盘上本刀 `.trx`＝**4 枚**＝定向 4×1；F 未起；无 `-voided-` 目录（作废轮 0，见 §一.1 c 未跑＝非作废）。清单＝`out/trx-list.txt`。
- 每轮定向 `total`＝**56**（四份全中）。
- 出场合格数复看（判据文件载体侧现取，`out/…blob-NarratorPrototypeTests.cs`）：文件 **989 行**；`[Fact]` **53**／`[Theory]` **1**／`[InlineData]` **3** ⇒ 53＋1×3＝**56 枚格**；`Assert.Skip` **0** 处。⇒ 本刀**净新增格 0、改断言 0、新增数字门槛 0**。

## 六 未验证面（本刀留下的，逐条点名）

1. **c 枚 #30（@614）＝本刀未领账**：见 §一.1。判据树 56 格里 `DeferredAction` 一律非空 `DeferredIntentId` ⇒ `NarrativeDraftValidator.cs:235` 的 null 分支**函数体从未被执行**（常绿纵深），把 `:235` 改恒假对 @614 无影响；@614 的拒绝来源是 `:240` 那道 else-if，与 `:235` 无关。要拿这枚格的单枚清洁红，配方要改指 `:240`（或给 c 造"DeferredIntentId 显式 null"的新形状夹具＝新增格＝本刀权限外）。
2. **`used + cost > total` 那半边（工单 §六.2 波 40 第 1 条留的账）**：本刀四枚配方都不在 `AddSection`／`AddUnitRun` 的预算门里 ⇒ 那半边仍从未被单独摘过，押下一把（波 40 §六.2 第 1 条照挂）。
3. **`DispositionReason = "not_supplied"` 那行（`:285`）的反向面**：本刀 m10 系四枚未在此文件；波 40 §六.2 第 2 条照挂。
4. **`NarrativeRoundRunner.cs:150`／`:155` 两支**：波 40 §六.2 第 3 条照挂。本刀未反证。
5. **`NarrativeContextCompiler.cs:77-82` 前奏跨会话门**：波 40 §六.2 第 4 条照挂。
6. **字段消费者穷举**：波 40 §六.2 第 5 条照挂。本刀仪器只扫 `Assert.` 行的 token 面，未做"该字段还有谁读"的穷举。
7. **预检仪器 v6 对 Godot 前台窗（P115）的分级**：脚本给 BUILD_OK 是设计行为；本刀按工单 §〇.4"以 `VERDICT` 为准"，不自行解释分类是否恰当。
8. **本刀不能署的面（结构性）**：判据字节全程未动 ⇒ 本刀的红证是**行为面＋第二换手**两面，不含"判据写法本身"（工单 §〇.3 定义本身）。

## 七 交守门人核的事（照工单 §五 那七件，逐件给当场读数，不代裁）

1. **四轮各自实到红枚数与红名，是否逐字符等于 §一 落档的闭合表；哪一轮作废、作废依据**：a 恰 1＝`Draft_without_any_action…`@520（帧 `:526`）、b 恰 1＝`Deferred_intent_that_claims…`@599（帧 `:606`）、d 恰 1＝`Call_refuses_an_assembly_that_belongs_to_another_scope(scope:"conversation")`@437（帧 `:447`）、e 恰 1＝`Draft_from_a_fenced_writer_epoch…`@790（帧 `:796`）。四轮全有效、红名／帧／消息族逐字符等于 §一 表；**作废轮＝0**；c 未跑＝按 §一.1 件 3′ 退回定向额度（非作废、非未跑；无对应 dotnet／TRX／预检）。多响／少响＝0（每轮 `openTagFailed=1`＝`<Counters failed>=1`＝`altWayNodes=1` 三向同值）。
2. **判据文件 SHA 是否每轮两侧＋出场面全等于 `4f1b2f6e…`；没当场打印的轮次，替代面是什么**：**本刀未做到"每轮两次"**。判据 SHA 被单独打印（`sha256sum`）并当场可见的时刻共 **4 个**：①入场 §〇 三面（carrier/main/blob 三值同 `4f1b2f6e…`）；②a 轮还原后（`out/…` 打印于 §二.1 ⑩）；③b 轮还原后（同 §二.2 ⑩）；④§三.2 出场三面。未单独再打印的轮次＝d 施加前、d 还原后、e 施加前、e 还原后。**替代面**＝每轮还原后当场 `git -C carrier status --short`＝0 行＋载体 HEAD＝`51f149d` ⇒ 判据文件（跟踪文件）与 blob 逐字节同值＝充分；通道 `REVERTED … sameAsBlob=true` 只写产品文件面独立支撑。是否接受"0 行＋blob 同值"替代每轮两次 hash，由你裁；本刀不自签。
3. **载体终态**：`status --short`＝**0 行**、`fd93ebe..HEAD`＝**2 行**（`51f149d`＋`1e02205`）、`rev-parse HEAD`＝`51f149d0b722…`；三枚产品＋判据＝§三.2 三面 MATCH 表；`Directory.Build.props` 未触碰（工单 §五.3 八枚面里的第八枚本刀不需要核＝四枚配方不在这个文件）。
4. **收尾全量（若起了）**：见 §四＝**未起**（c 轮按件 3′ 退回 ⇒ 依工单 §四.4 立即不起 F）；无 `.trx`、无求和、无跳过点名。**本刀不顶账**、也不为顶账另起任何构建。
5. **`.trx` 枚数与每轮 `total` 格数（当前树＝56）对账**：盘上本刀 `.trx`＝**4 枚**＝定向 4（`out/trx-a/`、`out/trx-b/`、`out/trx-d/`、`out/trx-e/`），每轮 `total`＝**56**；结果目录 4 个，无 `-voided-`。工单 §五.5 的"满额形状"是"5＋4"＝9 枚，本刀因 c 未跑＋F 未起＝4 枚，差额＝5（＝c 未占 1＋F 未起 4），逐项对应"退回 1 定向额度＋不起 1 全量"两条处置，未越线。
6. **四类预算是否越线**：`dotnet` 4/6、定向 4 实跑＋1 退回（不越 5）、解决方案级构建 0/1（按 §五.2 三段判定合法跳过）、HOLD 0/16（未动）。四类均未越线；HOLD 未被写成放宽。
7. **每一枚红的性质**：四轮 4 枚不同格的红**全部**是 `family=ASSERT-RED`（消息族 `Assert.Equal() Failure`／`Assert.Throws() Failure`／`Assert.Single() Failure`），**0 枚**整格因异常而红（仪器未打印 `Exception thrown` 族；`out/reading-*.txt` 四份可逐枚复核）。4 枚都有断言级凭证。

**未领账的格（照 §六.1 与工单 §五.8）**：
- #30 `Deferred_action_without_a_pointing_intent_is_refused`（@614，c 的目标格）＝**本刀未领**：件 3′ 判不闭合（`:235` 常绿），退回定向额度；判不清面逐枚登记在 §一.1，处置由你决定（重出配方指 `:240`／造新形状夹具属"新增格"须另议）。

**四句话照旧不许写**：本刀通篇未写"新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链"。**状态词与结案词**：本刀通篇未写"已完成／已结案／已验证／绿灯"。基线不移动、不顶账、不 merge、不 push、不 tag。

## 九 守门人裁决（追加节；§〇–§七 一字未改，本节为纯追加）

### 九.1 裁决一行与入场数盘

- 裁决＝**四轮定向全部有效、作废轮 0、c 轮按件 3′ 退回定向额度（本刀形状＝4 实跑＋1 退回＋F 依法不起）、4 枚结清、零归位、基线不移动**。
- 我当场取的盘上面（不引用 lane 的表）：主树 `git status --short --branch`＝**10 行**（1 行 branch＋他人两枚 `M ` 暂存项＋一枚 ` M`＝`PROJECT_PLAN.md` 的 18/0＋六枚 `??`＝五枚他人资产＋本报告）；载体 `git -C carrier status --short`＝**0 行**、`rev-parse --short HEAD`＝`51f149d`、`log --oneline fd93ebe..HEAD`＝**2 行**（`51f149d`＋`1e02205`＝波 33 那两枚既有提交，本刀零提交）；`git worktree list`＝76 行；`git ls-files`＝648（暂存本报告与第四十二波工单之后实测，见 §九.9）。
- 决定性那条式子当场复取＝`git diff --stat 51f149d 88a642b -- src tests scripts Pry.slnx Directory.Build.props`＝**空输出＝0 行**（原始归档＝`out/seg1-0line.txt`，0 字节）。

### 九.2 第十份独立仪器与它自己的自证（`.local-notes/wave41-1029/gk41-adjudicate.js`，node 从零写；lane 的 `w41-count.js`／`w41-apply.js` 一行未读）

- 自证带七面，每面带四元限定（波次＋判据文件名＋跑法＋该面结论字段值），结果＝**7/7 AGREE**：波 40 `trx-M8p`（恰 5，帧行 540/555/569/582/865）、波 40 `trx-m08`（恰 4，帧行 289/320/961/981）、波 39 `trx-M2`（恰 3）、波 39 `trx-M8-voided-P103`（恰 5）、波 13 `run-m08`（旧 49 格树恰 2、total 49）、波 13 `run-full`（total 1228／failed 2／`NotExecuted` 开标签 2）、波 40 `trx-F`（1406／1404／0／2）。仪器输出＝`out/gk41-adjudicate.txt`。
- **一处我自己的表错（同族第四次，当场更正不涂改）**：自证表初稿把波 40 `trx-m08` 那组写成"帧行 281/313/952/975"，盘上是 **289/320/961/981**。坏的是我的引用面不是盘也不是本报告——`281/313/952/975` 是波 40 报告 §一.3 写的**格起始行**（@），`289/320/961/981` 才是同一表"红"列里明写的 **TRX 断言帧行**（:）。⇒ 新口径进第四十二波工单 §三 件 6＝**已知读数表里凡写行号必须标注种类，@ 与 : 两种口径不许在同一张表里混用不标**；本刀另把"报 `sha` 三面等值"的写法保留为充分面（§九.5）。仪器因此改了一次期望值，判据与盘上产物一个字节没动。

### 九.3 四轮逐枚（名／帧／消息三面由我从 `.trx` 独立取，不取 lane 的读数）

| 轮 | 被改行（载体现文改前） | 突变 | 落档预期（§一） | 实到（开标签＝`<Counters failed>`＝反向走法） | 红格全名／格起始行／TRX 断言帧行／消息 | 裁决 |
|---|---|---|---|---|---|---|
| a | `NarrativeDraftValidator.cs:139`＝`        if (content.Actions.Count == 0)` | → `if (false)` | 恰 1＝@520 | 1＝1＝1 | `Draft_without_any_action_is_rejected_rather_than_read_as_silence`／@520／:526／`Assert.Equal() … Expected: ContractRejected / Actual: Silence` | **有效** |
| b | 同上 `:287`＝`        if (intent.Status is NarrativeIntentStatus.Fulfilled && content.Actions.All(a =>` | 首操作数换 `false` | 恰 1＝@599 | 1＝1＝1 | `Deferred_intent_that_claims_to_be_fulfilled_without_delivery_is_refused`／@599／:606／`Assert.Equal() … Expected: ContractRejected / Actual: Deferred` | **有效** |
| d | `NarratorContracts.cs:50`＝`        if (!string.Equals(Assembly.ConversationId, Request.ConversationId, StringComparison.Ordinal))` | → `if (false)` | 恰 1＝@437 的 conversation 参数行 | 1＝1＝1 | `Call_refuses_an_assembly_that_belongs_to_another_scope(scope: "conversation")`／@437／:447／`Assert.Throws() Failure: No exception was thrown` | **有效** |
| e | `NarrativeRoundRunner.cs:244`＝`        if (draft.OwnershipEpoch != request.OwnershipEpoch)` | → `if (false)` | 恰 1＝@790 | 1＝1＝1 | `Draft_from_a_fenced_writer_epoch_is_refused`／@790／:796／`Assert.Single() Failure: The collection was empty` | **有效** |

- 每轮 `nodes=56／total=56／executed=56／passed=55／failed=1／NotExecuted 开标签=0`，与 §一.3 那张 A 20＋B 30＋C 5＋D 1＝56 的分母自洽；四份 `rawOutcomeFailedStrings` 各＝开标签＋1，多的那处逐字符复核＝`<ResultSummary outcome="Failed">`（文件级，不算用例）＝件 5 第三次现场生效。
- **件 4 一次都没触发**（四轮实到红集＝落档表，枚数与格名与帧行与消息族四项同时相符）。d 轮那条"另两参数行不连坐"另有盘上见证：同一次跑里 `scope:"instance"` 与 `scope:"batch"` 两枚节点都是 `Passed`（我从 `trx-d` 的节点面直接读出），不是靠推演。
- 四枚预合成文本我按 §三.0R 件 1 的口径并排自证（SHA 对账不证明文本正确）：对 `git cat-file -p 51f149d:<path>` 逐行比对，差异行数各＝恰 1 且就在声明那一行、行数不变、净括号偏移守恒（a 0→0、b 2→2、d 0→0、e 0→0）、盘上 SHA 与字节数逐字符等于报告 §〇.2 表与通道 `APPLIED` 行。c 没有预合成文本（`mutation-prep/` 只有四枚）＝与"该轮未跑"自洽。
- 一处仪器初稿的错要登记：我第一版把"全文件圆括号总数相等"写进件 3 的自检，d 轮被误报成缺陷——波 39 已立的口径是**净括号偏移守恒**（整行替换允许删掉一整枚平衡调用）。改的是我的仪器，不是判据也不是 lane 的产物。

### 九.4 c 轮退回的独立复核（我自己读的，不引用 §一.1 的结论）

- 读 `NarrativeDraftValidator.cs` 载体 blob `:229–:246`：`:235 if (action.DeferredIntentId is null)` → 体内 `:237` 加 `IntentClaimedAsFact "没有指向任何意图…"`；`:240 else if (!content.Intents.Any(i => string.Equals(i.IntentId, action.DeferredIntentId, StringComparison.Ordinal)))` → 体内 `:242` 加**同一枚枚举**、文案"指向的意图…不在本次提案里"。
- 读判据 blob：夹具 `DeferredAction(string intentId, …)`（`:89–:94`）把 `DeferredIntentId = intentId` 设成必填非空；盘上 `DeferredAction(` 的调用点＝**四处**（`:505`／`:601`／`:616`／`:834`），显式传 `null`＝**0 处** ⇒ `:235` 的函数体在这棵 56 格树上恒不执行＝一道对当前格"常绿"的纵深防御分支。@614（体在 `:616`，断言在 `:620/:621/:622`）的 `intents` 为空 ⇒ 它的拒绝来自 `:240`，枚举同名 ⇒ 把 `:235` 改恒假后该格三个字段逐字段不变。⇒ **该突变的实到红集恒＝∅，写不出"目标格会红"的闭合表 ⇒ 不起那一轮、定向额度原样退回＝成立**（件 3′ 上线以来第一次走"退回"这条路径，而不只是"作废"那条）。
- 一处 lane 的小账（不改判）：§一.1 第 2 点写"唯一走到 Deferred 分支的三枚"，盘上是四枚调用点，漏了 `@832 Mixed_round_takes_the_immediate_branch…`（夹具 `[ImmediateAction(), DeferredAction("intent-1")]` 同样进 `CheckAction`）。结论（全部非空 ⇒ 分支体不执行）不受影响；登记为"凡写枚数以盘上枚举为准"这一条同族。
- F 依法不起＝工单 §四.4 那条"任一轮被作废**或被件 3′ 退回** ⇒ 立即不起 F"的第一次实际执行。代价面＝本刀没有 `trx-F`、在册基线 1406/1404/0/2 未被复采也不被移动；收益面＝少弹一次防火墙。**这不是"少做了一件交付"，这是那道门买下来的东西**：用一次实测换一次误署名（波 39 的 M8 就是这么作废的）。
- 我给下一把的配方（**零 `dotnet` 静态推演，不是结论**）：#30 应重锚到 `:240`——把该 `else if` 的条件换 `false`，预期红集＝**恰 1 枚**＝@614 的 `:620`（`Expected: ContractRejected / Actual: Deferred`），不连坐面＝`:505`／`:601`／`:834` 三处调用的 `intent-1` 都在各自 `intents` 里故改前改后都不命中、`:235` 那支恒不执行故无第二路径。这条写进第四十二波工单 §二 的 f 轮，并明令 lane 自己按当前 56 格盘重推、落自己的闭合表后才准起跑。

### 九.5 签账资格与结清的账

- 判据字节全程未动：四枚文件（三枚产品＋判据）三面 SHA 我当场重测（`blob(51f149d)`／载体工作副本／主树工作副本）＝**12 次比对全 EQUAL、0 次 UNEQUAL**，判据值逐字符＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`；last-touch 主树＝`639a579`／`290a603`、载体＝`639a579`＋`290a603`（载体那 2 行 log 里根本没有本刀的手）；过程面那条独立硬证＝载体 `status --short` 全程出现的那一行永远是 `src/` 产品文件、从未出现 `tests/**`。⇒ 取红的那只手不是判据字节的作者，**这四枚的红证与第二换手同场结清**（波 24／27／28／39／40 那条口径第六次由我使用成动作）。
- lane 交回的第 2 件（判据 hash 只在 4 个时刻单独打印、d/e 两侧未打印）我判＝**接受**，依据是波 40 §九.5 立的那条：每轮还原后当场 `status --short`＝0 行＋载体 HEAD 未动 ⇒ 跟踪文件与 blob 逐字节同值是充分面；而且我这次的三面重测是在四轮跑完之后、由另一个时刻独立取的，与那 4 个打印点不同源。替代面在报告里明写了"这是替代面"，不是把缺漏写成读数。
- 结清逐枚点名（对波 22 审计 §四.1 T3 表的点号）：**#23**（`V:139`，@520）、**#29**（`V:287`，@599）、**#17**（`N:50` 的 `scope:"conversation"` 参数行，@437）、**#43**（`R:244`，@790）＝**4 枚**。#30（`V:235`）＝本刀未领、账不减，重锚配方见 §九.4 末。

### 九.6 求和（当场从盘上产物写算式，不做心算、不预写）

- 波 13 的 D 类 28（＝波 40 §九.6 那次求和 `47 −（10＋9）＝28`，本刀既没新增格也没减少格）−本刀结清 4＝**24**；24＋波 20 那 28＝**旧欠 56 → 52**。
- 断言级子账不移动（按判据文件分列的口径照旧：HDS-009 那棵已结 4／未结 2、HDS-007 那棵未结 2）；本刀**净新增格 0、改断言 0、新增数字门槛 0**＝判据 blob 我当场重数：989 行、`[Fact]` 53＋`[Theory]` 1×`[InlineData]` 3＝56 枚、`Assert.Skip` 0 处，与波 40 出场值逐项同值。

### 九.7 归位裁决＝零落地（同一条判据第七次执行）

- 三面等值＋0 行式子＋载体 0 行/2 行 log（本刀零提交）⇒ **树上无差可归**；`51f149d` 不是 `main` 的祖先，通道从来不是 `merge --ff-only`。本把唯一落地面＝文档（本报告＋第四十二波工单＋§4.2/§9/sprint 三处登记）。
- "字节写完不等于可归位"与"可归位"这两侧在本刀同时成立且方向不同：本刀没写任何产品字节 ⇒ 无归位对象；四枚格已经在"改坏产品码"之下响过并被我裁结清 ⇒ 归位这件事不欠它们。第四十波留的那句"同一条判据的两面"在这里第三次兑现。

### 九.8 预算、预检与产物形状

- `dotnet` 4/6；定向 4 实跑＋1 退回（不越 5，退回没有被写成"动用"，也没有为把它用满而另造一轮）；解决方案级构建 0/1（三段判定当场复取：0 行式子＋`carrier/src/Pry.Core/bin/Release/net10.0/Pry.Core.dll` 在盘＋四枚 SHA 三段未变）；全量 0/1（依 §四.4 合法不起）；HOLD 0/16。四类均未越线，HOLD 没被写成放宽。
- 盘上 `.trx`＝**4 枚**、结果目录 4 个（`trx-a/b/d/e`）、无 `-voided-` 目录（作废轮 0，c＝退回不是作废）；预检 P112–P115 逐份 `grep VERDICT` 全 `BUILD_OK`、枚数 4＝`dotnet` 次数 4＝实跑轮数 4（件 5 的三方对账成立）；预检 mtime 与 run mtime 一一配对（02:55:13＜02:55:33、02:55:49＜02:56:01、02:56:12＜02:56:24、02:56:40＜02:56:50）＝无"跳过预检起跑"的形状。
- **一处凭证口径的更正（这次是我的仪器第一次说出空话）**：`run-*.txt` 是 `dotnet test` 的 stdout，里面"dotnet"字样出现 0 次、根本不含命令行 ⇒ 报告 §五.2 那句"命令行 0 处 `--no-build`"在这份归档上**没有见证面**（我最初的检查就是照着它写的，得到一个恒真值）。真正的正面凭据我改读这一面：四份各 3 行 ` -> ….dll` 构建产出行在场（`--no-build` 时不会重编译）＋四轮末行原样 `失败: 1，通过: 55，已跳过: 0，总计: 56`。结论不变（没跑 `--no-build`），新口径进第四十二波工单 §三 件 7＝**负向断言必须指明它在哪个字段本来会出现**。
- 顺带一条形状登记（不是缺陷、也不需要新门槛）：a 轮与 e 轮构建日志各带 1 枚 `warning CS0162（检测到无法访问的代码）`，来自 `if (false)` 之后那段函数体；b/d 轮 0 枚。构建未失败 ⇒ 本仓 Release 未开 warnings-as-errors。突变轮的告警数是 1/0/0/1 这种不齐的形状，登记为已知副作用面。
- 件 1 的归档文件名与工单 §三.0R 写的 `out/item1-parallel.txt` 不同＝lane 实际落 `out/item1-2-3-prep.txt`（三件合一）。内容面齐（OLD 现文逐字符并排＋整行出现恰 1 次＋行数＋净括号收支），我按盘上文本独立复算四枚全对（§九.3 第三点）。命名偏离登记，substance 不缺，不因此判作废。

### 九.9 本把文件面、顶账与换手账

- 本刀报告只追加 §九（§〇–§七 一字未改，纯追加⇒删除列恒 0）；新增第四十二波工单 `docs/dispatch-hds-wave42-1029.md`；登记面＝`PROJECT_PLAN.md` 的 §4.2 HDS-003 行一处状态＋§9 一条 entry、`full-scope-sprint.md` 的 §0 三条事实句＋§3 一条密行。
- 顶账＝**648**（本报告与波 42 工单落盘暂存后当场 `git ls-files` 实测，不做加法预写），链 …→644→646→648。
- 换手账＝本刀 4 枚的红证＋第二换手同场结清（§九.5）；#30 依旧欠红证，而且本刀新给出一条更硬的事实＝它按 `V:235` 那枚配方**在数学上拿不到红**（分支体恒不执行），所以这笔账的形态是"配方指错门"而不是"没有非判据作者的手"。旧欠 52 枚的拆法因此写成 **T3 19／T4 5＋波 20 那 28**（19 含 #30 需重锚 `:240`；`46／45／1＝W2` 那句拆法原文照旧不动，它记的是另一本账）。
- `main` 照旧只挪 ref、**零 `tag`／零 push／零打包**；他人两枚 `M ` 暂存项（1/1 与 63/0）与五枚未跟踪资产一字未动；`PROJECT_PLAN.md` 上他人 18 行在飞，本把用两版 hunk 手术排除在提交外、提交后原样铺回并复测仍 18/0。

### 九.10 未验证面一条不减

- §六.1–§六.8 八条照挂（#30 未领、`used + cost > total` 那半边、`:285` 的 `not_supplied` 反向面、`NarrativeRoundRunner.cs:150`／`:155`、`NarrativeContextCompiler.cs:77-82` 前奏跨会话门、字段消费者穷举、预检 v6 对 Godot 前台窗的分级、本刀结构性不能署的"判据写法本身"）。
- 老账照挂：真实模型对照零读数、生产接线零真实调用方（这 56 枚至今只有测试在跑）、旧数据继承零读取、UI 零读数、生产库与 `%LOCALAPPDATA%\PryCompanion` 零接触、待标定参数未定＝**特性不是缺陷**、并发与断电零读数、覆盖率零读数、旧链删除片与"不进包"那道门未开始。
- 本刀新留两条：① @614 的拒绝来源这一面**只有静态推演**（`:240` 那道门从未被单枚突变实测过，`:235` 那一枚实测过的是"它不响"）；② "走到 Deferred 分支的格是几枚"这类枚数面，本报告写三、我按盘上取四——以后凡枚举面出表必须当场重取。

### 九.11 出闸第四十二波

`docs/dispatch-hds-wave42-1029.md`（HDS-WAVE42-1029）＝D 类第二片＝T3 五枚 `f/g/h/i/j`，其中 f 轮是我按 §九.4 给 #30 重锚 `NarrativeDraftValidator.cs:240` 的新配方（lane 必须自己重推并落闭合表）；五枚锚点全部按载体 `51f149d` 现文重取并逐行抄在工单 §二 表里；预算＝构建 ≤1（默认跳过）／全量 ≤1（**五轮全有效才起**）／定向 ≤5／`dotnet` ≤6／HOLD ≤16 按刀计、预检编号从 **P116** 起；新增两条口径为前置＝件 6（自证表的行号必须标种类）、件 7（负向断言要指明见证面）。自此在飞＝1（`HDS-WAVE42-1029`，我当场自起后台 lane）。**四句话照旧不许写**＝新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链。
