# HDS-WAVE41-1029｜波 13 D 类第一片＝五枚"拆一行既有门即响"的第二双手（隔离原型，件 3′ 照旧为前置）

工单编号＝`HDS-WAVE41-1029`。坐标系＝`docs/plans/PROJECT_PLAN.md` §4.2 的 **HDS-003**（新生成中心隔离原型）。开工前必须完整读 `docs/plans/README.md`、`docs/plans/PROJECT_PLAN.md`、`docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md`、`docs/agent-engineering-standards.md`、根目录 `AGENTS.md`，再读本单全文与波 40 报告 `docs/eval/reports/2026-10-29-hds13-bclass-second-hand-recount-part2.md` 的 §九（守门人裁决节）。

## 〇 任务性质与硬线（先读这一节）

1. **性质＝证据刀·换人复采。** 零产品新字节（终态）、零判据字节、零新格、零新增数字门槛、零 UI 改动、零生产接线。本刀不产生任何用户可指认的行为变化。
2. **要结的账＝波 13 那本 47 枚里的 D 类 28 枚的第一片。** D 类＝盘上从未拿到过"单枚、单轮、清洁红"的那批格（与 B 类的区别：B 类有红但红出自写判据的那双手）。本刀从 `docs/eval/reports/2026-10-23-hds003-005-cell-evidence-audit.md` §四.1 的 **T3 梯队**（"拆一行既有门即响"）里取**五枚锚点已在本单 §三.0 当场按载体树重取过的格**开工，其余 T3／T4 押后续刀。
3. **签账资格（不可协商）**＝取红的那双手不许是写判据的那双手。判据文件字节全程一字不动：`tests/Pry.Core.Tests/NarratorPrototypeTests.cs` 在载体（基点 `51f149d`）侧的 SHA-256 恒等 `4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`。判据字节只要被写，本刀全部凭证作废。
4. **焦点预检是每一枚 `dotnet` 的前置。** 仪器＝`.local-notes/game-preflight6-1022.ps1`，调用形＝`powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1`。只有 `VERDICT=BUILD_OK` 才允许起跑；`HOLD` 就停下，不请示、不绕过、不跑、不改判据、不把 HOLD 当成"没有这回事"。**本刀预检编号从 P112 起**（守门人自跑的 P105b 与 lane 的 P106–P111 已归档在 `.local-notes/wave40-1029/out/`）。整段输出存档到本刀自己的 `out/preflight-<ID>.txt`。
5. **不许 `--no-build`。** 突变轮跑的是改过的字节，带 `--no-build` 会跑到上一次构建的旧 dll＝突变不生效＝假绿。
6. **共享主树禁一切写入与 git 动作**（`git commit`／`add`／`checkout`／`reset`／`cherry-pick`／`merge`／`tag`／`push`／`git apply`／`stash`／`clean`／`rm` 全部禁止）。所有突变只在载体 worktree `.local-notes/qa-wave33-1028/carrier`（基点 `51f149d`，detached）里做，还原＝从 `git show 51f149d:<path>` 取字节再写文件，不用 `checkout`／`restore`。
7. **他人资产按硬线只数行不读内容**：主树里两枚 `M ` 暂存项与五枚未跟踪资产、`PROJECT_PLAN.md` 工作树上他人的 18 行，一律一字不动、不提交、不 `stash`、不 `clean`。
8. **本刀唯一允许新建的跟踪文件＝本刀报告** `docs/eval/reports/2026-10-29-hds13-dclass-first-slice.md`（写完不提交、不 `git add`，由守门人按路径落地）；其余产物一律落 `.local-notes/wave41-1029/`（gitignored）。
9. **不许写状态词与结案词**（"已完成""已结案""已验证""已接线""可以删旧链"等），不许自宣基线移动，不许把"编译过了"当成"做到了"。交付标准＝`docs/agent-engineering-standards.md`：用户实际能完成的行为；本刀连这一层都不主张，只主张"这五枚格在改坏产品码之下各响了一次可归因的红"。
10. **四句话照旧不许写**＝新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链。

## 一 入场：先数盘，再读任何人的自述

逐条当场取，不许引用本单或波 40 报告的任何数字当"入场值"：

1. `git status --short --branch`（主树）行数与内容分类（他人资产只数行）。
2. 载体：`git -C <carrier> rev-parse HEAD`、`git -C <carrier> status --short` 行数（**必须 0 行才允许起跑**）、`git -C <carrier> log --oneline fd93ebe..HEAD` 行数。
3. `git -C <carrier> worktree list` 与 `git rev-parse --show-toplevel` —— 每次写文件前先证明"我写的到底是哪棵树"（这条是波 26 那次错树的教训）。
4. 判据文件格数当场重取：`[Fact]`／`[Theory]`／`[InlineData]` 计数与带属性方法数（`public void` ＋ `public async Task`，注意夹具里 `public async Task<` 不承载测试属性）；当前树应当是一棵 **56 格**的判据，不是波 13 当年的 49 格。**任何按 49 格树做的红集平移一律无效。**
5. 在册基线起点当场从波 40 报告 §九.7 读（不从本单抄）。
6. 取数仪器**先自证再用**（口径⓪）：拿波 40 那五份已裁决落盘的 `.trx` 与波 13 的旧 `.trx` 当已知读数，仪器对不上就停手并报"仪器不符"，不许直接裁新读数。

## 二 本刀范围＝五枚格各拿到一次可归因的清洁红（五条可判范围）

每条都要"会响的格"作证据，不接受"代码写了／编译过了"：

1. **§三.0 表里 a 枚**：拆 `NarrativeDraftValidator.cs` 的那道"没有任何动作就不许当沉默"门 ⇒ 目标格 `Draft_without_any_action_is_rejected_rather_than_read_as_silence` 必须响。
2. **b 枚**：拆"已声称送达的延迟意图不许没有投递记录"那道门 ⇒ 目标格 `Deferred_intent_that_claims_to_be_fulfilled_without_delivery_is_refused` 必须响。
3. **c 枚**：拆"延迟动作必须指着一枚意图"那道门 ⇒ 目标格 `Deferred_action_without_a_pointing_intent_is_refused` 必须响。
4. **d 枚**：拆跨会话身份比对（`NarratorContracts.cs` 的 ConversationId 那一支）⇒ 目标格 `Call_refuses_an_assembly_that_belongs_to_another_scope` 的 `scope:"conversation"` 那一行必须响，另两行不许连坐。
5. **e 枚**：拆写者围栏代次比对（`NarrativeRoundRunner.cs` 的 `OwnershipEpoch` 那一支）⇒ 目标格 `Draft_from_a_fenced_writer_epoch_is_refused` 必须响。

**不许把本单当预期来源**：以上只点名"谁应当响"，一枚都没给枚数。每轮的**完整红集（含谁不连坐、为什么）必须由你在起跑前自己从当前盘推演并落档**（件 3′，见 §三.0R）。

## 三 跑序、预算与五轮配方

跑序固定＝a→b→c→d→e→全部还原并核八枚 SHA→F（F 的条件见预算第 4 行）。

### 三.0 五轮配方表（锚点与"改前整行"＝本单 10-29 当场从 `51f149d` 侧重取的原文；开工仍须逐字符复核）

| 轮 | 文件 | 载体树行号 | 改前整行（当场取，逐字符） | 改后（突变语义） | 目标格（波 22 审计编号） |
|---|---|---|---|---|---|
| a | `src/Pry.Core/Narrative/NarrativeDraftValidator.cs` | `:139` | `if (content.Actions.Count == 0)` | `if (false)`（保持形状与可达性，别删整块） | #23 `Draft_without_any_action_is_rejected_rather_than_read_as_silence` |
| b | 同上 | `:287` | `if (intent.Status is NarrativeIntentStatus.Fulfilled && content.Actions.All(a =>` | 只把 `intent.Status is NarrativeIntentStatus.Fulfilled` 换成恒假、**不改续行** | #29 `Deferred_intent_that_claims_to_be_fulfilled_without_delivery_is_refused` |
| c | 同上 | `:235` | `if (action.DeferredIntentId is null)` | 换成恒假 | #30 `Deferred_action_without_a_pointing_intent_is_refused` |
| d | `src/Pry.Core/Narrative/NarratorContracts.cs` | `:50` | `if (!string.Equals(Assembly.ConversationId, Request.ConversationId, StringComparison.Ordinal))` | 去掉取反（把门反过来＝同会话也拒），或换恒假——**两选一，写进你的闭合表并说明另一选择为什么不取** | #17 `Call_refuses_an_assembly_that_belongs_to_another_scope(scope: "conversation")` |
| e | `src/Pry.Core/Narrative/NarrativeRoundRunner.cs` | `:244` | `if (draft.OwnershipEpoch != request.OwnershipEpoch)` | 换恒假 | #43 `Draft_from_a_fenced_writer_epoch_is_refused` |

**表外的行号一律不许信**：审计那张表的行号是 2026-10-23 的 `8c2f3e4` 侧读数，本单只在上面这五行做过当场重取；同表里 `R:297`／`R:327` 在载体树已不是那一行（当场读到的是 `{` 与 `}`），这就是"引用历史行号前必须按当前树重取"的反例，登记给你看。

每轮记满十一项（照波 40 报告 §二.0 那份清单，一项不许省）：①被改文件:行 ②施加前盘上态 ③施加后盘上态 ④`git diff --stat` ⑤`dotnet test` 末行原样 ⑥`.trx` 文件名 ⑦红格全名＋消息要义＋红点行号 ⑧还原命令 ⑨还原后 `status --short` 行数 ⑩突变态与还原后的文件 SHA-256＋本轮判据文件 SHA（**判据 hash 要么当场打出来，要么写明"由 X 面推出"——不许把由相邻时刻推得的值写成当场值**，这条是波 40 §九.4 立的）⑪本轮预检段。

### 三.0R 五件前置（每件都要成品，不许只写"有做"）

- **件 1＝改前整行的并排自证**：把每一轮的"改前整行"与载体树现文逐字符并排打印（`out/item1-parallel.txt`），不符就 ABORT 不写一字节。**SHA 对账不能替代并排自证**（波 28 立的口径：预合成文本必须与所替换那行原文并排打印＋改前行内容等值断言）。
- **件 2＝突变文本预合成、零 `dotnet` 先核 SHA**：五枚改后文本先落 `.local-notes/wave41-1029/mutation-prep/`，当场打印 SHA-256 与字节数进报告 §一；跑起来后每一轮的"施加后 SHA"必须逐字符等于 §一 那行。
- **件 3＝语法形状自检**：单行突变的不变量＝**净括号偏移守恒**（不是括号数相等），外加全文件括号收支与行数不变；改后语法坏掉却不启动构建的那一轮不许跑（明知语法坏还跑＝波 37 立的禁令）。
- **件 3′＝闭合点名门（硬门，波 40 起）**：每一轮起跑前，报告里必须**已经落档**一张闭合的预期红集表＝逐枚点名谁会红（给夹具如何走到被改行、翻掉的断言行号、消息形状）＋逐枚写明其余每一枚格为什么不连坐（给可达路径级理由，**不许写"应该不影响"**）。闭合不了 ⇒ 不起那一轮、定向额度原样退回、把判不清的面逐枚登记为欠账。**枚数必须逐枚点名，禁止"各恰 N"这种合并写法**（波 40 §九.5 立的：我上一把就是把四行合并写成"各恰 1"才错了一行）。禁止用"跑一把看响几枚"代替推演。
- **件 4＝多响的处置**：实到红集不等于你落档的那张表 ⇒ **整轮作废**，目标格不签、不重跑第二轮、不追认；多响的那枚只有在起跑前被逐字符预判过时才构成作废依据，否则一律按"整轮作废、不做署名"处理（不许写成"目标都响了外加一点噪声"）。
- **件 5＝读数口径**：只数 `<UnitTestResult … outcome="Failed">` **开标签**，并与同份 `<Counters failed>` 双向对账；`<ResultSummary outcome="Failed">` 不算用例；属性按名字取不按顺序；栈帧锚点是 `<路径>:line N`（TRX 里没有 `at ` 可锚）；`testName` 是全限定＋参数表，回判方法名先剥括号；跳过格只认 `outcome="NotExecuted"` 开标签。

## 四 预算（超限即停，不自我扩额）

1. 解决方案级构建 `dotnet build`：**≤1**，且默认不起——若"0 行式子"成立（载体树与 `main` 的相关字节逐字符相同）就按三段判定合法跳过，登记三段各自读数。
2. 收尾全量 `dotnet test Pry.slnx -c Release`：**≤1**，只作"还原后整棵树自洽＋读数没变"的复采；**本刀不顶账，绝不为顶账另起任何构建**。
3. 定向：**≤5**（＝本刀五轮，额度用满即停）。
4. `dotnet` 总调用：**≤6**；**若任一轮被作废或被件 3′ 退回 ⇒ 立即不起 F**，把那次调用省下来（作者的机器上每枚调用都会弹防火墙），登记"跳过 F 的原因＝哪一轮"。
5. HOLD 重跑：**≤16，按整把刀计**；HOLD 本身不算额度动用，只算重跑次数。连续 HOLD 到额度用尽 ⇒ 收尾全量不跑、基线不移动，直接交回，这是合法交付（波 29／35 已两次确立）。
6. 纯读数与文档动作**零 `dotnet`**；报告按批次增量落盘（冷启动有轮次上限，先写盘再补细节）。

## 五 交回守门人核的事（逐件给当场读数，不代裁）

1. 五轮各自：实到红枚数与红名，是否逐字符等于你跑前落档的那张闭合表；哪一轮作废、作废依据。
2. 判据文件 SHA 是否每轮两侧＋出场面全等于 `4f1b2f6e…`；没当场打印的轮次，替代面是什么（`status --short` 行数与那一行的内容）。
3. 载体终态：`status --short` 行数、`fd93ebe..HEAD` 行数、八枚文件（三枚产品＋四枚判据＋`Directory.Build.props`）三侧 SHA。
4. 收尾全量（若起了）：先数 `.trx` 份数，再逐份 `<Counters>` 当场求和，跳过格逐枚点名。
5. `.trx` 枚数与每轮 `total` 格数（当前树＝56）对账；结果目录枚数与命名。
6. 四类预算是否越线，HOLD 额度是否被写成放宽。
7. 每一枚红的性质：是断言级红（`ASSERT-RED`）还是整格因异常而红；红点帧与消息族逐枚列出。
8. 你没能领账的格与原因（额度、闭合不了、方向够不着），逐枚登记，不许合并成一句。

## 六 不许走的捷径（反向约束，每一条都有前例）

- 不许为了让某枚格响去**改判据、加断言、加 `[InlineData]`、放宽既有阈值、注释掉别人的断言、用 `Assert.Skip`**，也不许新增数字门槛凑红。
- 不许把"批次里恰好响了几枚"署给某一枚突变（件 4）。
- 不许用固定返回值／吞异常／伪造成功让门"看起来咬住了"。
- 不许只给阳性探针不给反面（扫描类判定尤其如此）。
- 不许把推演写成实测：机理段要标"这是推演"，实测只报盘上读数。
- 不许把历史报告的行号、枚数、SHA 当现值用（本单 §三.0 那条 `R:297`／`R:327` 的反例就是给这个用的）。
- 不许在主树留下任何笔迹（除那一枚未跟踪的报告）。
- 不许顶账、不许动 `main`、不许 `merge --ff-only`（载体基点 `51f149d` **不是** `main` 的祖先，通道从来由守门人按路径落地）。
- 不许 `tag`／`push`／实际打包；不许碰 `licenses/**`、`THIRD-PARTY-NOTICES.md`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md`；不许提交模型／运行库／数据库／密钥／日志／用户导入内容／未授权素材。

## 七 与后续刀的关系（登记，不在本刀做）

- T3 剩 18 枚与 T4 那 5 枚（含"需要新增形状／夹具改造"的设计题）押下一把；`#49`（T5，已随波 25 改判成可判红格）与 `#31`（特殊：它唯一的红是干净轮红、lane 当年自判预期写错）单独押，配方要另出。
- 波 20 那 28 枚照旧挂着，与本刀不同文件。
- 波 40 报告 §六.2 那八条未验证面照旧一条不减；其中第 1 条（`used + cost > total` 那半边从没被单独摘过）是下一把的候选配方，本刀不动它。
- 接线（HDS-003 这 56 枚至今零生产调用方）要作者先拍 U-9 那句"要不要拿真实模型跑对照"，本刀不许擅自开始。

**状态词与结案词不由本单写，也不由 lane 写。** 本单落地时 `main` 上已含波 40 全部裁决；在飞＝本刀一把；顶账由守门人在提交后当场 `git ls-files | wc -l` 实测。
