# 波 13 D 类第二片·第二双手（第四十二波，HDS-WAVE42-1029，lane 侧）

- 工单：`docs/dispatch-hds-wave42-1029.md`（`HDS-WAVE42-1029`）。坐标系＝`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS-003；账本＝波 22 审计 `docs/eval/reports/2026-10-23-hds003-005-cell-evidence-audit.md` §四.1 的 T3 梯队。
- 性质：证据刀·换人复采。零产品新字节（终态）、零判据字节、零新格、零新增数字门槛、零 UI、零生产接线、零打包。
- 载体：`.local-notes/qa-wave33-1028/carrier`（基点／detached HEAD＝`51f149d`）。本刀不新建 worktree、不新建产品或测试文件、主树零 git 写动作（全程未用 `commit`／`add`／`checkout`／`reset`／`cherry-pick`／`merge`／`tag`／`push`／`apply`／`stash`／`clean`／`rm`）。
- 本报告是主树里本刀唯一新建文件，未提交、未 `git add`；守门人裁决节（§九）不由本刀写，本节号在本报告里保留为空白。
- 判据文件＝`tests/Pry.Core.Tests/NarratorPrototypeTests.cs`，本刀全程一枚字节不写。
- 归档目录＝`.local-notes/wave42-1029/`；仪器＝自写 `.local-notes/wave42-1029/w42-count.js`（未复用波 41 的 `w41-count.js`）；写入通道＝自写 `.local-notes/wave42-1029/w42-apply.js`（未复用波 41 的 `w41-apply.js`）。

## 〇 入场与先数盘（全部当场从盘取；守门人预审里给的那些数不作本刀入场值）

- 时刻：`date` = `Thu Oct 29 11:50:21 2026`（随后每枚预检另记时刻）。
- 主树 `git rev-parse HEAD` = `ae5d702463244f6649788207308bfd615bc6c982`（当场取；协作者已在 `codex/ui-main-window-redesign` 上续提交，本刀不碰主树、不以此面为工作面）。
- 主树 `git status --short --branch` 原样逐行（入场时 **9 行**＝1 行 branch＋两枚 `M ` 暂存项＋一枚 ` M`＋五枚 `??`）：

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

  以上全部他人资产：两枚 `M ` 暂存项、一枚 ` M`（`docs/plans/PROJECT_PLAN.md` 上他人的 18 行在飞）、五枚 `??`——本刀不读内容、不改、不提交、不 `stash`／`clean`／`rm`。`.local-notes/` 已被 `git check-ignore -v` 证实忽略，故本刀的产物不出现在这张表里；本报告落盘后主树 `git status --short --branch` 只多本刀这一枚 `?? docs/eval/reports/2026-10-29-hds13-dclass-second-slice.md`（＝10 行）。
- 载体入场自证（当场取，原始输出＝`.local-notes/wave42-1029/out/entry-readings.txt`）：

  | 件 | 命令 | 读数 |
  |---|---|---|
  | ① | `git -C carrier rev-parse HEAD` | `51f149d0b722e28a6566c93d19f48a94ca4c85df` |
  | ② | `git -C carrier status --short` | **0 行**（起跑门；非 0 行即停手交回） |
  | ③ | `git -C carrier log --oneline fd93ebe..HEAD` | **2 行**＝`51f149d test: wave33 keep each mutation a single clean red (authority cell drops null-window path)`＋`1e02205 test+feat: HDS-009 wave33 …` |
  | ④ | `git -C carrier rev-parse --show-toplevel` | `C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave33-1028/carrier`（每次写文件前先证此面） |
  | ⑤ | `git rev-parse --show-toplevel`（主） | `C:/Users/24782/Desktop/Pry_Project` |
  | ⑥ | `git worktree list` | **76 行**；本刀相关两行＝主树 `[codex/ui-main-window-redesign]`＋carrier `51f149d (detached HEAD)` |

### 〇.1 判据三面 SHA 与格数当场重取

- 三面逐字符（当场 `sha256sum`，原始输出＝`out/entry-readings.txt` 第 1 节＋`out/item1-2-3-prep.txt` 末段二次打印）：

  | 面 | SHA-256 |
  |---|---|
  | `git show 51f149d:tests/Pry.Core.Tests/NarratorPrototypeTests.cs` 的 blob | `4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4` |
  | 载体工作副本 `tests/Pry.Core.Tests/NarratorPrototypeTests.cs` | `4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4` |
  | 主树工作副本同路径 | `4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4` |

  三面同值＝YES（不同值面＝0）⇒ 起跑门之一成立。本面由本刀自己当场取，不取守门人预审里的那一枚 SHA 作为入场值。
- 格数当场重取（载体 51f149d 侧现文，`out/entry-readings.txt` 第 2／3 节）：`[Fact]`＝**53**、`[Theory]`＝**1**、`[InlineData`＝**3** ⇒ 等式 `total = [Fact] + [Theory]×[InlineData] = 53 + 1×3 =` **56 枚可跑单元**（当前树＝56；本刀一切红集按这枚当场值推演，不平移波 13 当年 49 格旧树）。逐行计数与出现次数同值（53/1/3），无"同行两枚"的偏差面。`Assert.Skip`＝**0** 处。判据文件＝**989 行**（`wc -l`；blob 末元素空＝文件以 `\n` 结尾）。
- 56 枚全枚举（格起始行 @＝方法声明行）见 `out/entry-readings.txt` 第 3 节，逐枚可核；分组等式在 §一.1。
- 定向跑的门槛：每轮 `total` 必须＝**56**（对账写在 §二各轮，不以本行代读数）。

### 〇.2 工单 §一.5 那批已知老读数：本刀自己回读原始产物并按算术自取值（不是引用）

八面全部由本刀自写仪器 `w42-count.js` 从盘上原始 `.trx`／`run-*.txt` 当场重算（原始输出＝`out/instrument-selfproof.txt`）。每面都带四元限定（哪一波／哪份产物／哪一个程序集／哪一种跑法）并把行号种类标开（**@＝判据文件里的格起始行＝方法声明行；:＝TRX 断言帧行**，两种口径不许混用不标）：

| 面（四元限定） | 盘上实算（当场） | 表值（工单 §一.5 的方向） | 行号种类 | 结论 |
|---|---|---|---|---|
| 波 41｜`out/trx-a/24782_PHYTHLINK_2026-10-29_10_55_33.trx`｜`NarratorPrototypeTests.cs`｜定向 56 格突变轮 | failed 开标签＝**1**、total＝56 | 恰 1 | 格起始 **@520**／断言帧 **:526**（分别标注，不混） | AGREE |
| 波 41｜同上 `trx-b/…10_56_01.trx`｜同上｜同上 | **1**、56 | 恰 1 | **@599**／**:606** | AGREE |
| 波 41｜同上 `trx-d/…10_56_24.trx`｜同上｜同上 | **1**、56 | 恰 1（`scope:"conversation"` 参数行） | **@437**／**:447**；同方法另两参数行（instance／batch）同场 Passed（盘上 `passed=55` 含另两枚） | AGREE |
| 波 41｜同上 `trx-e/…10_56_50.trx`｜同上｜同上 | **1**、56 | 恰 1 | **@790**／**:796** | AGREE |
| 波 40｜`wave40-1029/out/trx-M8p/…02_51_40.trx`｜同上｜定向 56 格 | **5**、56 | 恰 5 | 断言帧 **:540,555,569,582,865**／格起始 **@534,549,562,574,851** | AGREE |
| 波 40｜同上 `trx-m08/…02_54_37.trx`｜同上｜定向 56 格 | **4**、56 | 恰 4 | **:289,320,961,981**／**@281,313,952,975** | AGREE |
| 波 13｜`qa-wave13-1023/runs/run-full/`（四份 `.trx`）｜四程序集｜解决方案级全量 | failed 开标签求和＝**2**、total 求和＝**1228**、`NotExecuted` 开标签＝**2** | 2／1228／2 | —（全量面，非单判据格） | AGREE |
| 波 40｜`wave40-1029/out/trx-F/…03_00_02..05.trx`（四份）｜四程序集｜解决方案级全量 | total 求和＝**1406**、executed＝**1404**、failed 开标签＝**0**＝`<Counters failed>` 求和、`NotExecuted` 开标签＝**2** | 1406／1404／0／2 | — | AGREE |

⇒ 结论：**8/8 面 AGREE**（问题面 0 处）。这八面同时是本刀仪器的自证面（件 6）：仪器先把这批老读数复算到位、逐面 AGREE，才被允许裁本刀的新读数。

- 件 6 过程的 MISMATCH 登记（不许略）：**第一次自证 7/8**，唯一 MISMATCH＝波 41 `trx-d` 的"参数行限定"面（仪器判 `NO`）。本刀先证明坏的是仪器不是盘：当场回读该 `.trx` 的原始开标签，盘上字面＝`testName="Pry.Core.Tests.NarratorPrototypeTests.Call_refuses_an_assembly_that_belongs_to_another_scope(scope: &quot;conversation&quot;)"`——TRX 里的引号是 `&quot;` 转义态，而本刀仪器的属性提取器未解 XML 实体，故看不见 `scope: "conversation"`。修法＝只改仪器（提取属性后解码 XML 实体），判据字节与所有盘上产物一字未动；重跑第二次自证＝**8/8 AGREE**。两次的原始输出都留在 `out/instrument-selfproof.txt`（第一次的失败面写在文件头部注释区）。
- 预检归档归属登记：`.local-notes/wave42-1029/out/preflight-P116b.txt`（mtime 10-29 11:2x、`VERDICT=BUILD_OK`）**不是本刀的产物**，是守门人预审那一把的归档；本刀自己的预检从 **P116** 起连续编号（`out/preflight-P116.txt`、`P117`…），逐份 VERDICT 枚数＝`dotnet` 次数＝实跑轮数的三方对账写在 §五.3。波 40 的报告里登记过同一件事（P1xx 撞号面），本刀照此明写以免两把的预检台账混成一张。

### 〇.3 写入通道（唯一允许写载体产品文件的通道）＝`.local-notes/wave42-1029/w42-apply.js`

六道守卫（本刀自写，未读波 41 通道的实现来复用）：

1. 目标路径落 `tests/**` 或树外 ⇒ `REFUSED`（本刀两枚目标文件都在 `src/Pry.Core/Narrative/`）。
2. 施加文本必须逐字节等于本 ID 在 `mutation-prep/` 预合成的那枚（SHA-256＋字节数双面对上）⇒ 否则 `REFUSED`。
3. 施加前断言被替换行的现文 === manifest OLD 逐字符、且 OLD 整行出现恰 1 次 ⇒ 否则 `ABORT` 不写一字节。
4. 施加前目标文件的整体 SHA 必须仍 === `blob(51f149d)` ⇒ 上一轮没还原干净即 `ABORT`。
5. 写后 SHA 不等于声明值 ⇒ 自动还原并 `ABORT`。
6. 还原一律从 `git show 51f149d:<path>` 取字节再写文件（全程不用 `git checkout`／`git restore`）。

附加面（本刀自加）：每次写文件前当场打印 `git -C carrier rev-parse --show-toplevel` 与 `status --short` 行数；行形状守卫见件 3。

### 〇.4 件 1／件 2／件 3 成品（先档后跑，零 `dotnet`；原始输出＝`out/item1-2-3-prep.txt`）

五枚改前整行与载体 `blob(51f149d)` 现文**逐字符并排**打印（左＝载体现文、右＝manifest OLD），各面：现文长度＝OLD 长度、整行出现次数＝**1**（≠1 即 ABORT）、等值断言＝`true`；件 3 的形状自检口径＝**净括号偏移守恒**（不是绝对括号数相等）＋全文件净圆括号／净大括号收支不变＋行数不变；件 2＝五枚改后文本已预合成落 `mutation-prep/`，SHA-256 与字节数当场进本表：

| 轮 | 文件:行 | OLD（载体 `51f149d` 现文，逐字符） | NEW（突变语义） | 现文长＝OLD 长 | OLD 整行次数 | 行数 | 单行净圆括号 | 单行净大括号 | 全文件净圆括号 | 全文件净大括号 | 全文件圆括号开/闭 | 突变文本 SHA-256／字节 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| f | `NarrativeDraftValidator.cs:240` | `                else if (!content.Intents.Any(i => string.Equals(i.IntentId, action.DeferredIntentId, StringComparison.Ordinal)))` | `                else if (false)` | 129＝129 | 1 | 421→421 | 0→0 | 0→0 | 0→0 | 0→0 | 181/181→179/179 | `d652fb3a8acdde68e00fc3d61e57d9448e95379feee0aaf006af944f2bf0d1b0`／19240 |
| g | `NarrativeDraftValidator.cs:76` | `        if (occurrences > 1)` | `        if (false)` | 28＝28 | 1 | 421→421 | 0→0 | 0→0 | 0→0 | 0→0 | 181/181→181/181 | `8e3a6c3e355cb7a1b6bbe44b28c168501c01151a710a831c87f9e2f660fa613a`／19328 |
| h | `NarrativeDraftValidator.cs:167` | `        if (perceivedReason != NarrativeContractReason.None)` | `        if (false)` | 60＝60 | 1 | 421→421 | 0→0 | 0→0 | 0→0 | 0→0 | 181/181→181/181 | `9f0f216845d99896cfbc635db12fc42b4f0432ba0ef5c919d3a7bf872383e57d`／19296 |
| i | `NarrativeDraftValidator.cs:173` | `        if (!content.ProcessedThrough.WithinBatch(request.BatchHighestSeq))` | `        if (false)` | 75＝75 | 1 | 421→421 | 0→0 | 0→0 | 0→0 | 0→0 | 181/180（见下） | `4a5a7f4d9683a91ee5ab6a973f9c5d087e59176d8341f124607aad20d477f938`／19281 |
| j | `NarrativeRoundRunner.cs:250` | `        if (draft.ReceivedThroughSeqAtIssue != request.ProcessedThroughAtIssue.Seq)` | `        if (false)` | 83＝83 | 1 | 342→342 | 0→0 | 0→0 | 0→0 | 0→0 | 102/102→102/102 | `681d6bf2953621024e8d3b62ae263d0adc99c1653212174815f71cedbe55ffc4`／16703 |

- 两枚被改文件的 blob 面（当场）：`NarrativeDraftValidator.cs`＝421 行／SHA `88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c`／19338 字节；`NarrativeRoundRunner.cs`＝342 行／SHA `961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9`／16768 字节。
- 轮 i 的"全文件圆括号开/闭"面在 `out/item1-2-3-prep.txt` 里打印为 `181/181→180/180`（改后行少一枚开与一枚闭，净偏移仍 0→0，等式成立）；本表其余四枚的该面照原样打印。两种口径都以"净括号偏移守恒＋全文件净圆括号 0→0＋净大括号 0→0＋行数不变"四条为准，绝对括号数不作判据。
- 换行符面（本刀自加的排雷项）：两枚目标文件的 blob 与工作副本 CR 计数均＝**0**（＝LF），尽管仓库 `core.autcrlf=true` 且工作树属性里有 `eol=crlf` 形状；通道按字节整体替换、锚点按 `\n` 切行，故不会出现"锚点恒不命中"的形状。
- 每轮"施加后 SHA"必须逐字符等于本表那一行（工单 §三 件 2）。

## 一 跑序、五轮配方与件 3′ 闭合点名表（**跑前落档；实测值不许回头改本节预期列**）

跑序固定＝**f→g→h→i→j**，每轮：预检→施加→定向跑→仪器读数→还原→`status --short` 行数→本轮十一项落 §二。五轮全部有效才起收尾全量 F（§四）；任一轮作废或被件 3′ 退回即不起 F。

**关于 f 轮的独立重推**（工单 §二 那句"守门人的事前论证＝非结论"）：本刀没有把守门人对 #30 的静态推演当结论，而是自己按当前 56 格盘把 `:240` 的可达面与条件枚算重推了一遍，结果落在 §一.2 的闭合表里（结论同向＝恰 1，但凭据是本刀自己的 §一.2＋§一.6＋`out/closure-scan.txt`，不是工单那句话）。本刀同时独立复核了波 41 的 c 配方（`:235` 的 null 支）在当前树上仍"函数体从未被执行"：判据里 `DeferredAction(` 实调用点＝@505／@601／@616／@834 四处（`out/entry-readings.txt` 第 5 节），夹具签名 `DeferredAction(string intentId, …)`（@89）首参必填非空 ⇒ 无一条动作的 `DeferredIntentId` 为 null ⇒ `:235` 支不进，`:240` 的 else-if 才是 @614 的拒绝来源。这与波 41 §一.1 的方向一致，本刀不据此追认 c 轮、也不替 c 轮跑任何测试。

### 一.1 56 枚 universe 分组（非连坐判定的分母，逐枚可归组；当场枚数见 §〇.1）

- **A 组（只调 `NarrativeContextCompiler.Compile` 或直接构造装配体，不进 `RunAsync`）＝20 枚**：@183、@198、@210、@225、@237、@247、@256、@266、@281、@298、@313、@328、@347、@357、@367、@890、@913、@934、@952、@975。
- **B 组（调 `RunAsync`，走 `NarrativeRoundRunner` 全流程）＝30 枚**：@381、@394、@456、@471、@488、@503、@520、@534、@549、@562、@574、@587、@599、@614、@628、@644、@658、@671、@684、@698、@711、@722、@735、@746、@761、@776、@790、@802、@832、@851。
- **C 组（直接构造 `NarratorCall`／`NarratorCallBudget`）＝5 格**：@408（只 budget）、@420（rejected 装配体⇒`NarratorContracts.cs:36` 先抛）、@437 的三枚参数行（`scope="instance"`⇒`:43`／`"conversation"`⇒`:50`／`"batch"`⇒`:57`）。
- **D 组（静态工具）＝1 枚**：@813（只调 `NarrativeRoundRunner.SplitContiguous`）。
- 等式：**20＋30＋5＋1＝56**。分组凭据＝`out/entry-readings.txt` 第 5 节的当场 grep 面（`RunAsync(` 调用合计＝30 处＝B 组 30 枚各一处的对应面；`Compile(` 合计＝21 处＝A 组 20 枚＋@420 那枚装配体来源）。

### 一.2 轮 f 闭合点名表（`NarrativeDraftValidator.cs:240`，`else if (!content.Intents.Any(i => string.Equals(i.IntentId, action.DeferredIntentId, StringComparison.Ordinal)))` ⇒ `else if (false)`，条件换 `false`、保留 `else if` 语法形状）

机理（本刀自己按 `blob(51f149d)` 现文推演）：`:240` 在 `CheckAction`（@186）的 Deferred 分支里，是 `:235`（`if (action.DeferredIntentId is null)`）的 **else-if**；命中时经 `:242-243` 加一枚 `IntentClaimedAsFact`（"指向的意图…不在本次提案里"）。`CheckAction` 只由 `NarrativeDraftValidator.Validate:147` 的 foreach 进入，而 `Validate` 只由 `NarrativeRoundRunner.cs:136` 进入（另两处复用是 `NarrativeDraftValidator.cs:385` 的 `NarrativeCommitValidator.Validate`，其调用者只有 `NarrativeCommitWriter.cs:178` 与 `Storage/NarrativeSqliteStore.cs:492`——判据文件对这两个名字提及＝**0 处**，见 `out/closure-scan.txt` 末段与 `out/entry-readings.txt` 第 5 节末行，故该入口在本刀 56 格里不可达）。⇒ 判定会变的格＝"走到 Deferred 动作分支且 `:240` 条件为真"的格。

| 预期红（1 枚，逐枚点名） | 夹具如何走到 `:240`（逐步到行） | 翻掉的断言（判据行号） | 预期消息族／形状 |
|---|---|---|---|
| #30 `Deferred_action_without_a_pointing_intent_is_refused`（格起始 @614） | @616 `Round(Draft(actions: [DeferredAction("intent-1")]))`：`Draft()` 默认 `intents: []`（@102 夹具默认面）⇒ FakeMode.Produce 产草稿 ⇒ `R:95` 装配 accepted、`R:105` call 通过、`R:110` outcome＝DraftProduced ⇒ `R:121 CheckSnapshotFreshness` 三支（`:238` 故事版本默认 7＝7、`:244` 代次默认 1＝1、`:250` 基线默认 100＝100）都不命中 ⇒ `R:133 Apply`（Deferred 动作原样返回，`:92` 早返）⇒ `R:136 Validate` ⇒ `V:147 CheckAction` ⇒ 动作 Mode＝Deferred ⇒ `V:235` `DeferredIntentId is null`＝假（非空 `"intent-1"`）⇒ **`V:240` else-if＝真**（`content.Intents` 空 ⇒ `!Any(...)`＝真）⇒ 改前经 `V:242-243` 加 `IntentClaimedAsFact` ⇒ `R:137` rejections.Count>0 ⇒ `R:139` 返回 `ContractRejected`；改后 `else if (false)` ⇒ 不加、`V:150` foreach 空 ⇒ rejections＝0 ⇒ `R:147-153` immediate 空 ⇒ `R:155` deferred 非空 ⇒ `R:158` 返回 **Deferred** | **:620** `Assert.Equal(NarrativeRoundDisposition.ContractRejected, result.Disposition)` 即翻（其后 `:621 Assert.Single`／`:622 Assert.Equal(...IntentClaimedAsFact…)` 属同格第二、三枚断言，不计第二枚红） | `ASSERT-RED`：`Assert.Equal() Failure: Values differ`／`Expected: ContractRejected`／`Actual:   Deferred` |

其余 **55 枚逐枚**为什么不连坐（可达路径级理由，枚枚点名；表由 `out/closure-scan.txt` 的轮 f 段原样转录，@ 口径＝格起始行）：

```text
@183	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@198	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@210	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@225	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@237	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@247	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@256	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@266	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@281	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@298	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@313	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@328	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@347	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@357	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@367	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@890	不变	不跑 RunAsync（直接 new NarrativeContextAssembly 求 WithinUnifiedBudget，连 Compile 都不跑）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@913	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@934	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@952	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@975	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@408	不变	不跑 RunAsync（只 new NarratorCallBudget＋Consume()，无任何 Validate）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@420	不变	不跑 RunAsync（NarratorCall.Validate 对 rejected 装配体⇒Contracts:36 先抛）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@437[instance]	不变	不跑 RunAsync（NarratorCall.Validate⇒Contracts:43 抛，scope=instance）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@437[conversation]	不变	不跑 RunAsync（NarratorCall.Validate⇒Contracts:50 抛，scope=conversation）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@437[batch]	不变	不跑 RunAsync（NarratorCall.Validate⇒Contracts:57 抛，scope=batch）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@813	不变	不跑 RunAsync（只调 NarrativeRoundRunner.SplitContiguous 静态法）⇒CheckAction 的 Deferred 支（只经 NarrativeDraftValidator.Validate，而 Validate 只在 RunAsync:136 进） 不可达
@381	不变	到达该行，但条件枚算＝假⇒改前改后同值
@394	不变	RunAsync:95 assembly 未 accepted⇒ContextRejected 早返回⇒该行不被执行
@456	不变	到达该行，但条件枚算＝假⇒改前改后同值
@471	不变	到达该行，但条件枚算＝假⇒改前改后同值
@488	不变	到达该行，但条件枚算＝假⇒改前改后同值
@503	不变	到达该行，但条件枚算＝假⇒改前改后同值
@520	不变	到达该行，但条件枚算＝假⇒改前改后同值
@534	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
@549	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
@562	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
@574	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
@587	不变	InvokeOnceAsync:330 把 OperationCanceledException 往上抛，本格没有 result⇒该行不被执行
@599	不变	到达该行，但条件枚算＝假⇒改前改后同值
@614	★候选	Deferred action-later→intent 'intent-1' 不在 intents(空)⇒OLD 条件＝真
@628	不变	到达该行，但条件枚算＝假⇒改前改后同值
@644	不变	到达该行，但条件枚算＝假⇒改前改后同值
@658	不变	到达该行，但条件枚算＝假⇒改前改后同值
@671	不变	到达该行，但条件枚算＝假⇒改前改后同值
@684	不变	到达该行，但条件枚算＝假⇒改前改后同值
@698	不变	到达该行，但条件枚算＝假⇒改前改后同值
@711	不变	到达该行，但条件枚算＝假⇒改前改后同值
@722	不变	到达该行，但条件枚算＝假⇒改前改后同值
@735	不变	到达该行，但条件枚算＝假⇒改前改后同值
@746	不变	到达该行，但条件枚算＝假⇒改前改后同值
@761	不变	到达该行，但条件枚算＝假⇒改前改后同值
@776	不变	:238 故事版本命中⇒CheckSnapshotFreshness return，:244/:250/:133/:136 全部不及⇒该行不被执行
@790	不变	:244 所有权代次命中⇒return，:250/:133/:136 全部不及⇒该行不被执行
@802	不变	:250 签发基线命中⇒return，:133/:136 全部不及⇒该行不被执行
@832	不变	到达该行，但条件枚算＝假⇒改前改后同值
@851	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
```

判定门＝**恰 1**：实到 ≠ { @614 } ⇒ 整轮作废、#30 不签、不重跑第二轮、不追认（工单 §三 件 4）。

### 一.3 轮 g 闭合点名表（`NarrativeDraftValidator.cs:76`，`if (occurrences > 1)` ⇒ `if (false)`）

机理：`:76` 在 `NarrativeProseBinder.Bind`（@40）内，是"同一引用串在正文出现多次"的歧义门，命中即返回 `Failure = ActionBindingAmbiguous`（`:78`）。`Bind` 的两条进入路径：①`NarrativeRoundRunner.cs:133` 的 `NarrativeProseBinder.Apply`（只对 `Mode==Immediate` 动作生效，`:92` 对其余模式原样返回）；②`NarrativeDraftValidator.cs:207` 的 `action.Binding ?? Bind(...)`——而 `R:133` 已先行把 `Binding` 置好，非 Immediate 动作的 `Binding` 在 `Apply` 后仍为 null 但 `CheckAction:207` 只在 Deferred/Immediate 具体支里取，本刀逐枚按夹具算过（见 `out/closure-scan.txt`）。⇒ 判定会变的格＝"走到 Bind 且 occurrences>1"的格。

| 预期红（1 枚） | 夹具如何走到 `:76` 且改前命中 | 翻掉的断言 | 预期消息族 |
|---|---|---|---|
| #33 `Ambiguous_quote_is_rejected_instead_of_the_host_choosing_one`（@658） | @660-661：`prose = "She wrote maybe later, then said maybe later again."`、`ImmediateAction(quote: "maybe later")` ⇒ 该串在正文出现 **2** 次（`:54-69` 的 while 计次）⇒ `R:133 Apply`→`Bind` ⇒ 改前 `:76` 命中 ⇒ `Failure=ActionBindingAmbiguous`（`:78`）⇒ `Apply` 写回后 `V:207` 取到该绑定（`Bound`＝`Failure==None && ResolvedOffset.HasValue`＝假）⇒ `CheckAction` 加 `ActionBindingAmbiguous` 拒绝 ⇒ `R:137` ⇒ `ContractRejected`；改后 `if (false)` ⇒ 跳过 `:78`，直落 `:81-86` 的 `ResolvedOffset = first`／`Failure = None` ⇒ 绑定成功 ⇒ `Validate` 无该枚拒绝、其余 `V:121/:125/:131/:139` 各门对该夹具都为假（scope/batch/prose 非空/actions 1 枚）⇒ rejections＝0 ⇒ `R:150` immediate 非空 ⇒ `ImmediateBranch` | **:665** `Assert.Single(result.Rejections)` 即翻（集合为空）；其后 `:666`／`:667` 属同格不计第二枚 | `ASSERT-RED`：`Assert.Single() Failure: The collection was empty` |

其余 **55 枚逐枚**：

```text
@183	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@198	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@210	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@225	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@237	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@247	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@256	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@266	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@281	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@298	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@313	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@328	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@347	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@357	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@367	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@890	不变	不跑 RunAsync（直接 new NarrativeContextAssembly 求 WithinUnifiedBudget，连 Compile 都不跑）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@913	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@934	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@952	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@975	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@408	不变	不跑 RunAsync（只 new NarratorCallBudget＋Consume()，无任何 Validate）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@420	不变	不跑 RunAsync（NarratorCall.Validate 对 rejected 装配体⇒Contracts:36 先抛）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@437[instance]	不变	不跑 RunAsync（NarratorCall.Validate⇒Contracts:43 抛，scope=instance）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@437[conversation]	不变	不跑 RunAsync（NarratorCall.Validate⇒Contracts:50 抛，scope=conversation）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@437[batch]	不变	不跑 RunAsync（NarratorCall.Validate⇒Contracts:57 抛，scope=batch）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@813	不变	不跑 RunAsync（只调 NarrativeRoundRunner.SplitContiguous 静态法）⇒NarrativeProseBinder.Bind（RunAsync:133 Apply 只对 Immediate 动作；CheckAction:207 的 ?? 因 Binding 已置不复算） 不可达
@381	不变	到达该行，但条件枚算＝假⇒改前改后同值
@394	不变	RunAsync:95 assembly 未 accepted⇒ContextRejected 早返回⇒该行不被执行
@456	不变	到达该行，但条件枚算＝假⇒改前改后同值
@471	不变	到达该行，但条件枚算＝假⇒改前改后同值
@488	不变	到达该行，但条件枚算＝假⇒改前改后同值
@503	不变	到达该行，但条件枚算＝假⇒改前改后同值
@520	不变	到达该行，但条件枚算＝假⇒改前改后同值
@534	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
@549	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
@562	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
@574	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
@587	不变	InvokeOnceAsync:330 把 OperationCanceledException 往上抛，本格没有 result⇒该行不被执行
@599	不变	到达该行，但条件枚算＝假⇒改前改后同值
@614	不变	到达该行，但条件枚算＝假⇒改前改后同值
@628	不变	到达该行，但条件枚算＝假⇒改前改后同值
@644	不变	到达该行，但条件枚算＝假⇒改前改后同值
@658	★候选	quote 'maybe later' 在 prose 里出现 2 次⇒OLD 条件＝真
@671	不变	到达该行，但条件枚算＝假⇒改前改后同值
@684	不变	到达该行，但条件枚算＝假⇒改前改后同值
@698	不变	到达该行，但条件枚算＝假⇒改前改后同值
@711	不变	到达该行，但条件枚算＝假⇒改前改后同值
@722	不变	到达该行，但条件枚算＝假⇒改前改后同值
@735	不变	到达该行，但条件枚算＝假⇒改前改后同值
@746	不变	到达该行，但条件枚算＝假⇒改前改后同值
@761	不变	到达该行，但条件枚算＝假⇒改前改后同值
@776	不变	:238 故事版本命中⇒CheckSnapshotFreshness return，:244/:250/:133/:136 全部不及⇒该行不被执行
@790	不变	:244 所有权代次命中⇒return，:250/:133/:136 全部不及⇒该行不被执行
@802	不变	:250 签发基线命中⇒return，:133/:136 全部不及⇒该行不被执行
@832	不变	到达该行，但条件枚算＝假⇒改前改后同值
@851	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
```

判定门＝**恰 1**＝{ @658 }；实到 ≠ 本表 ⇒ 整轮作废、#33 不签、不重跑、不追认。

### 一.4 轮 h 闭合点名表（`NarrativeDraftValidator.cs:167`，`if (perceivedReason != NarrativeContractReason.None)` ⇒ `if (false)`）

机理：`:167` 是 `CheckConsumption`（@163）的第一道门；`perceivedReason` 由 `:166` 的 `content.PerceivedInputRefs.CheckSubsetOf(request.InputSeqs.ToArray())` 算出（`NarrativeDocuments.cs`：非批次内 seq ⇒ 返回 `PerceivedInputUnknown`，全在批次内 ⇒ `None`）。`CheckConsumption` 只由 `Validate:137` 进入（`NarrativeCommitValidator` 那条复用路径不可达，见 §一.2 同条凭据），而 `Validate` 只由 `R:136` 进入。⇒ 判定会变的格＝"进了 `Validate` 且 `perceivedReason != None`"的格。

| 预期红（1 枚） | 夹具如何走到 `:167` 且改前命中 | 翻掉的断言 | 预期消息族 |
|---|---|---|---|
| #38 `Perceived_reference_outside_the_batch_is_refused`（@722） | @724-725：`Draft(actions: [SilenceAction()], perceived: new PerceivedInputRefs { Refs = [new InputRef(999, "msg-999", Conversation)] })`，而 `Request()` 默认 `Inputs = [Input101(seq 101)]` ⇒ `999 ∉ {101}` ⇒ `:166` perceivedReason＝`PerceivedInputUnknown` ⇒ 改前 `:167` 命中 ⇒ `:169` 加 `PerceivedInputUnknown` 拒绝 ⇒ `ContractRejected`；改后 `if (false)` ⇒ 不加；`:173` 对本格＝`!WithinBatch`＝`!(101 ≤ 101)`＝假 ⇒ 也不加；其余门对该夹具都假 ⇒ rejections＝0 ⇒ immediate 空、deferred 空（唯一动作是 Silence）⇒ `R:167` 返回 **Silence** | **:730** `Assert.Single(result.Rejections)` 即翻（集合为空）；其后 `:731` 同格不计 | `ASSERT-RED`：`Assert.Single() Failure: The collection was empty` |

其余 **55 枚逐枚**：

```text
@183	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@198	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@210	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@225	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@237	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@247	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@256	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@266	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@281	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@298	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@313	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@328	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@347	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@357	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@367	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@890	不变	不跑 RunAsync（直接 new NarrativeContextAssembly 求 WithinUnifiedBudget，连 Compile 都不跑）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@913	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@934	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@952	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@975	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@408	不变	不跑 RunAsync（只 new NarratorCallBudget＋Consume()，无任何 Validate）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@420	不变	不跑 RunAsync（NarratorCall.Validate 对 rejected 装配体⇒Contracts:36 先抛）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@437[instance]	不变	不跑 RunAsync（NarratorCall.Validate⇒Contracts:43 抛，scope=instance）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@437[conversation]	不变	不跑 RunAsync（NarratorCall.Validate⇒Contracts:50 抛，scope=conversation）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@437[batch]	不变	不跑 RunAsync（NarratorCall.Validate⇒Contracts:57 抛，scope=batch）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@813	不变	不跑 RunAsync（只调 NarrativeRoundRunner.SplitContiguous 静态法）⇒CheckConsumption（只经 NarrativeDraftValidator.Validate⇒RunAsync:136） 不可达
@381	不变	到达该行，但条件枚算＝假⇒改前改后同值
@394	不变	RunAsync:95 assembly 未 accepted⇒ContextRejected 早返回⇒该行不被执行
@456	不变	到达该行，但条件枚算＝假⇒改前改后同值
@471	不变	到达该行，但条件枚算＝假⇒改前改后同值
@488	不变	到达该行，但条件枚算＝假⇒改前改后同值
@503	不变	到达该行，但条件枚算＝假⇒改前改后同值
@520	不变	到达该行，但条件枚算＝假⇒改前改后同值
@534	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
@549	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
@562	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
@574	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
@587	不变	InvokeOnceAsync:330 把 OperationCanceledException 往上抛，本格没有 result⇒该行不被执行
@599	不变	到达该行，但条件枚算＝假⇒改前改后同值
@614	不变	到达该行，但条件枚算＝假⇒改前改后同值
@628	不变	到达该行，但条件枚算＝假⇒改前改后同值
@644	不变	到达该行，但条件枚算＝假⇒改前改后同值
@658	不变	到达该行，但条件枚算＝假⇒改前改后同值
@671	不变	到达该行，但条件枚算＝假⇒改前改后同值
@684	不变	到达该行，但条件枚算＝假⇒改前改后同值
@698	不变	到达该行，但条件枚算＝假⇒改前改后同值
@711	不变	到达该行，但条件枚算＝假⇒改前改后同值
@722	★候选	perceived 999 ⊄ 批次 101⇒OLD 条件＝真
@735	不变	到达该行，但条件枚算＝假⇒改前改后同值
@746	不变	到达该行，但条件枚算＝假⇒改前改后同值
@761	不变	到达该行，但条件枚算＝假⇒改前改后同值
@776	不变	:238 故事版本命中⇒CheckSnapshotFreshness return，:244/:250/:133/:136 全部不及⇒该行不被执行
@790	不变	:244 所有权代次命中⇒return，:250/:133/:136 全部不及⇒该行不被执行
@802	不变	:250 签发基线命中⇒return，:133/:136 全部不及⇒该行不被执行
@832	不变	到达该行，但条件枚算＝假⇒改前改后同值
@851	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
```

判定门＝**恰 1**＝{ @722 }；实到 ≠ 本表 ⇒ 整轮作废、#38 不签、不重跑、不追认。

### 一.5 轮 i 闭合点名表（`NarrativeDraftValidator.cs:173`，`if (!content.ProcessedThrough.WithinBatch(request.BatchHighestSeq))` ⇒ `if (false)`）

机理：`:173` 与 §一.4 的 `:167` 同在 `CheckConsumption`，但是**平行的另一道门**（当场核现文：`:167` 支体只有 `:169-170` 一枚 `rejections.Add`、**没有 `return`**，故 `:167`／`:173`／`:179` 三支可叠红；本刀逐枚算过＝@722 只中 `:167`、@735 只中 `:173`，两枚各自那一道门，互不串）；`WithinBatch(seq) => seq <= batchHighestSeq`，`request.BatchHighestSeq = Inputs.Max(Seq)`（默认＝101）。进入路径与 §一.4 同（`Validate:137`⇒`R:136`）。⇒ 判定会变的格＝"进了 `Validate` 且 `:173` 条件为真"的格。

| 预期红（1 枚） | 夹具如何走到 `:173` 且改前命中 | 翻掉的断言 | 预期消息族 |
|---|---|---|---|
| #39 `Watermark_beyond_the_batch_upper_bound_is_refused`（@735） | @737：`Draft(actions: [SilenceAction()], processedThrough: 500)`（判据里 `processedThrough:` 覆盖点当场枚数＝**1**，见 `out/entry-readings.txt` 第 5 节 ⇒ 其余格都吃默认 101）⇒ `:166` perceived 默认＝`[Input101]` ⊂ 批次 ⇒ perceivedReason＝`None` ⇒ `:167` 不命中、`:169` 不加；`:173` 改前：`!(500 ≤ 101)`＝真 ⇒ `:175` 加 `ProcessedBeyondBatch` ⇒ `ContractRejected`；改后 `if (false)` ⇒ 不加；`:179` 是与 `:173` 平行的第三道门（非 else 支），本格 `500 < 100`＝假 ⇒ 改前改后都不加 ⇒ rejections＝0 ⇒ immediate／deferred 皆空 ⇒ `R:167` **Silence** | **:741** `Assert.Single(result.Rejections)` 即翻（集合为空）；其后 `:742` 同格不计 | `ASSERT-RED`：`Assert.Single() Failure: The collection was empty` |

其余 **55 枚逐枚**：

```text
@183	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（同上） 不可达
@198	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（同上） 不可达
@210	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（同上） 不可达
@225	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（同上） 不可达
@237	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（同上） 不可达
@247	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（同上） 不可达
@256	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（同上） 不可达
@266	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（同上） 不可达
@281	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（同上） 不可达
@298	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（同上） 不可达
@313	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（同上） 不可达
@328	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（同上） 不可达
@347	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（同上） 不可达
@357	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（同上） 不可达
@367	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（同上） 不可达
@890	不变	不跑 RunAsync（直接 new NarrativeContextAssembly 求 WithinUnifiedBudget，连 Compile 都不跑）⇒CheckConsumption（同上） 不可达
@913	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（同上） 不可达
@934	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（同上） 不可达
@952	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（同上） 不可达
@975	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckConsumption（同上） 不可达
@408	不变	不跑 RunAsync（只 new NarratorCallBudget＋Consume()，无任何 Validate）⇒CheckConsumption（同上） 不可达
@420	不变	不跑 RunAsync（NarratorCall.Validate 对 rejected 装配体⇒Contracts:36 先抛）⇒CheckConsumption（同上） 不可达
@437[instance]	不变	不跑 RunAsync（NarratorCall.Validate⇒Contracts:43 抛，scope=instance）⇒CheckConsumption（同上） 不可达
@437[conversation]	不变	不跑 RunAsync（NarratorCall.Validate⇒Contracts:50 抛，scope=conversation）⇒CheckConsumption（同上） 不可达
@437[batch]	不变	不跑 RunAsync（NarratorCall.Validate⇒Contracts:57 抛，scope=batch）⇒CheckConsumption（同上） 不可达
@813	不变	不跑 RunAsync（只调 NarrativeRoundRunner.SplitContiguous 静态法）⇒CheckConsumption（同上） 不可达
@381	不变	到达该行，但条件枚算＝假⇒改前改后同值
@394	不变	RunAsync:95 assembly 未 accepted⇒ContextRejected 早返回⇒该行不被执行
@456	不变	到达该行，但条件枚算＝假⇒改前改后同值
@471	不变	到达该行，但条件枚算＝假⇒改前改后同值
@488	不变	到达该行，但条件枚算＝假⇒改前改后同值
@503	不变	到达该行，但条件枚算＝假⇒改前改后同值
@520	不变	到达该行，但条件枚算＝假⇒改前改后同值
@534	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
@549	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
@562	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
@574	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
@587	不变	InvokeOnceAsync:330 把 OperationCanceledException 往上抛，本格没有 result⇒该行不被执行
@599	不变	到达该行，但条件枚算＝假⇒改前改后同值
@614	不变	到达该行，但条件枚算＝假⇒改前改后同值
@628	不变	到达该行，但条件枚算＝假⇒改前改后同值
@644	不变	到达该行，但条件枚算＝假⇒改前改后同值
@658	不变	到达该行，但条件枚算＝假⇒改前改后同值
@671	不变	到达该行，但条件枚算＝假⇒改前改后同值
@684	不变	到达该行，但条件枚算＝假⇒改前改后同值
@698	不变	到达该行，但条件枚算＝假⇒改前改后同值
@711	不变	到达该行，但条件枚算＝假⇒改前改后同值
@722	不变	到达该行，但条件枚算＝假⇒改前改后同值
@735	★候选	水位 500 > 批次上界 101⇒OLD 条件＝真
@746	不变	到达该行，但条件枚算＝假⇒改前改后同值
@761	不变	到达该行，但条件枚算＝假⇒改前改后同值
@776	不变	:238 故事版本命中⇒CheckSnapshotFreshness return，:244/:250/:133/:136 全部不及⇒该行不被执行
@790	不变	:244 所有权代次命中⇒return，:250/:133/:136 全部不及⇒该行不被执行
@802	不变	:250 签发基线命中⇒return，:133/:136 全部不及⇒该行不被执行
@832	不变	到达该行，但条件枚算＝假⇒改前改后同值
@851	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
```

判定门＝**恰 1**＝{ @735 }；实到 ≠ 本表 ⇒ 整轮作废、#39 不签、不重跑、不追认。

### 一.6 轮 j 闭合点名表（`NarrativeRoundRunner.cs:250`，`if (draft.ReceivedThroughSeqAtIssue != request.ProcessedThroughAtIssue.Seq)` ⇒ `if (false)`）

机理：`:250` 是 `CheckSnapshotFreshness`（@236，`private static`）三支里的**第三支**，三支按 `:238`（故事版本）→`:244`（所有权代次）→`:250`（签发基线）顺序排，任一命中即 `return` 一枚 `ContractRejection`（`:252`＝`LateCommitRejected`）。该方法只由 `RunAsync:121` 进入（当场枚数＝1，见 `out/entry-readings.txt` 第 6 节），且 `R:121` 在 `R:95`／`R:105`／`R:110` 三道早返回之后 ⇒ 只有"过了前三道门"的 B 组格能读到 `:250`。

| 预期红（1 枚） | 夹具如何走到 `:250` 且改前命中 | 翻掉的断言 | 预期消息族 |
|---|---|---|---|
| #44 `Draft_with_a_wrong_issue_baseline_is_refused_as_late`（@802） | @804：`Draft(actions: [ImmediateAction()], receivedBaseline: 90)`（判据里 `receivedBaseline:` 覆盖点当场枚数＝**1**），`Request()` 默认 `ProcessedThroughAtIssue.Seq`＝100 ⇒ `:238` 故事版本默认 7＝7 不命中、`:244` 代次默认 1＝1 不命中 ⇒ `:250` 改前 `90 != 100`＝真 ⇒ 返回 `LateCommitRejected` ⇒ `R:127` 组装 `ContractRejected`；改后 `if (false)` ⇒ 不进 `:251-253` ⇒ 函数落到 `:258` 返回 `null` ⇒ `R:133 Apply`（`ImmediateAction()` 默认 quote＝`"answered him"` 在默认 prose 里唯一 ⇒ 绑定成功）⇒ `R:136 Validate`（`receivedBaseline` 不参与 `Validate`，`V:121/:125/:131/:139/:167/:173` 各门对该夹具都假）⇒ rejections＝0 ⇒ `R:150` immediate 非空 ⇒ `ImmediateBranch`（对这段形状不抛：拒绝都走 `HistoryRejected` 且 `Rejections` 保持默认 `[]`） | **:808** `Assert.Single(result.Rejections)` 即翻（集合为空）；其后 `:809` 同格不计 | `ASSERT-RED`：`Assert.Single() Failure: The collection was empty` |

其余 **55 枚逐枚**：

```text
@183	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@198	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@210	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@225	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@237	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@247	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@256	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@266	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@281	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@298	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@313	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@328	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@347	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@357	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@367	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@890	不变	不跑 RunAsync（直接 new NarrativeContextAssembly 求 WithinUnifiedBudget，连 Compile 都不跑）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@913	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@934	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@952	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@975	不变	不跑 RunAsync（只 NarrativeContextCompiler.Compile）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@408	不变	不跑 RunAsync（只 new NarratorCallBudget＋Consume()，无任何 Validate）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@420	不变	不跑 RunAsync（NarratorCall.Validate 对 rejected 装配体⇒Contracts:36 先抛）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@437[instance]	不变	不跑 RunAsync（NarratorCall.Validate⇒Contracts:43 抛，scope=instance）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@437[conversation]	不变	不跑 RunAsync（NarratorCall.Validate⇒Contracts:50 抛，scope=conversation）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@437[batch]	不变	不跑 RunAsync（NarratorCall.Validate⇒Contracts:57 抛，scope=batch）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@813	不变	不跑 RunAsync（只调 NarrativeRoundRunner.SplitContiguous 静态法）⇒CheckSnapshotFreshness（private static，只经 RunAsync:121） 不可达
@381	不变	到达该行，但条件枚算＝假⇒改前改后同值
@394	不变	RunAsync:95 assembly 未 accepted⇒ContextRejected 早返回⇒该行不被执行
@456	不变	到达该行，但条件枚算＝假⇒改前改后同值
@471	不变	到达该行，但条件枚算＝假⇒改前改后同值
@488	不变	到达该行，但条件枚算＝假⇒改前改后同值
@503	不变	到达该行，但条件枚算＝假⇒改前改后同值
@520	不变	到达该行，但条件枚算＝假⇒改前改后同值
@534	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
@549	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
@562	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
@574	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
@587	不变	InvokeOnceAsync:330 把 OperationCanceledException 往上抛，本格没有 result⇒该行不被执行
@599	不变	到达该行，但条件枚算＝假⇒改前改后同值
@614	不变	到达该行，但条件枚算＝假⇒改前改后同值
@628	不变	到达该行，但条件枚算＝假⇒改前改后同值
@644	不变	到达该行，但条件枚算＝假⇒改前改后同值
@658	不变	到达该行，但条件枚算＝假⇒改前改后同值
@671	不变	到达该行，但条件枚算＝假⇒改前改后同值
@684	不变	到达该行，但条件枚算＝假⇒改前改后同值
@698	不变	到达该行，但条件枚算＝假⇒改前改后同值
@711	不变	到达该行，但条件枚算＝假⇒改前改后同值
@722	不变	到达该行，但条件枚算＝假⇒改前改后同值
@735	不变	到达该行，但条件枚算＝假⇒改前改后同值
@746	不变	到达该行，但条件枚算＝假⇒改前改后同值
@761	不变	到达该行，但条件枚算＝假⇒改前改后同值
@776	不变	:238 故事版本命中⇒CheckSnapshotFreshness return，:244/:250/:133/:136 全部不及⇒该行不被执行
@790	不变	:244 所有权代次命中⇒return，:250/:133/:136 全部不及⇒该行不被执行
@802	★候选	签发基线 90 ≠ 宿主 100⇒OLD 条件＝真
@832	不变	到达该行，但条件枚算＝假⇒改前改后同值
@851	不变	RunAsync:110 outcome 非 DraftProduced⇒ProviderRejected 早返回⇒该行不被执行
```

判定门＝**恰 1**＝{ @802 }；实到 ≠ 本表 ⇒ 整轮作废、#44 不签、不重跑、不追认。

### 一.7 五轮合计（跑前）与"闭合"自证声明

- 五张表各自的候选集合枚数（**逐轮逐枚点名，不用"各恰 1"合并写法**）：轮 f＝1 枚＝@614；轮 g＝1 枚＝@658；轮 h＝1 枚＝@722；轮 i＝1 枚＝@735；轮 j＝1 枚＝@802。五张表的"其余 55 枚"面各 56 行、枚枚有名有因，原始扫描＝`out/closure-scan.txt`（本节的五个代码块由该文件同段原样转录，非手写）。
- 转录自证（扫描模型 vs 判据 blob 当场 grep 枚数，12 面全 **AGREE**，原始输出＝`out/closure-scan.txt` 第 299-311 行）：`perceived` 覆盖点 1＝1、`processedThrough` 1＝1、`receivedBaseline` 1＝1、`storyVersionBase` 1＝1、`ownershipEpoch` 不符面 1＝1、`prose` 覆盖点 2＝2、非默认 `intents` 覆盖点 3＝3、`DeferredAction(` 实调用点 4＝4、`ImmediateAction(` 实调用点 14＝14、`actions: []` 面 1＝1、非 Produce 的 `RunAsync` 面 6＝6、`RunAsync(` 调用面 30＝30。⇒ 扫描模型对判据的转录没有漏枚或多数，五张表的分母可信。
- 可达性旁证（产品面 grep，`out/closure-scan.txt` 末段）：`NarrativeCommitWriter.cs` 里 `NarrativeDraftValidator.Validate`＝0 处、`NarrativeCommitValidator.Validate`＝1 处、`ProseBinder.Bind`＝1 处；`Storage/NarrativeSqliteStore.cs` 同形状（0／1／1）；`DeliveryLedger.cs` 三枚皆 0 处；判据文件里 `NarrativeCommitValidator`／`NarrativeCommitWriter`／`NarrativeSqliteStore` 三个名字提及＝**0 处** ⇒ `NarrativeDraftValidator.cs:385` 那条复用路径在本刀 56 格里不可达（这条同时纠正波 41 §一.4 括号里"经 `NarrativeCommitValidator` 复用"对本刀锚点的不适用面：本刀五枚锚点里有四枚落在 `DraftValidator`／`RoundRunner`，其复用者同样不可达）。
- 闭合声明：五轮都能写出闭合表（无轮次需按件 3′ 退回），故定向额度计划动用 5/5；F 的启用条件＝五轮全部有效（§四）。
- 本节全部为**跑前**推演与落档；§二的实测读数不回头改本节的预期列。

## 二 逐轮读数（每轮跑完立刻追加；预期列在 §一，本节不回头改）

口径：每轮记满十一项①被改文件:行／②施加前盘上态／③施加后盘上态／④`git diff --stat`／⑤`dotnet test` 末行原样／⑥`.trx` 文件名／⑦红格全名＋消息要义＋红点行号／⑧还原命令／⑨还原后 `status --short` 行数／⑩突变态与还原后 SHA＋本轮判据 SHA／⑪本轮预检段。红数按 §〇.2 的自证仪器只认 `<UnitTestResult … outcome="Failed">` 开标签，并与同份 `<Counters failed>` 双向对账；`<ResultSummary outcome="Failed">` 不算用例。

### 二.1 轮 f（`src/Pry.Core/Narrative/NarrativeDraftValidator.cs:240`，`else if (!content.Intents.Any(i => string.Equals(i.IntentId, action.DeferredIntentId, StringComparison.Ordinal)))` → `else if (false)`）

| 项 | 当场值 |
|---|---|
| ① 被改面 | `NarrativeDraftValidator.cs:240`（通道施加前当场逐字符断言 OLD===现文、整行出现恰 1 次后放行；改前行号面见 §〇.4） |
| ② 施加前盘上态 | `git -C carrier rev-parse HEAD`＝`51f149d0b722e28a6566c93d19f48a94ca4c85df`；`git -C carrier status --short`＝**0 行**；`rev-parse --show-toplevel`＝`…/.local-notes/qa-wave33-1028/carrier`；判据 SHA 三面本轮施加前的替代面＝0 行＋HEAD 未动（§〇.1 已当场逐字符打印过三面，本轮未单独再打；还原后在 ⑩ 补打） |
| ③ 施加后盘上态 | 通道打印＝`APPLIED W42-f 轮 f src/Pry.Core/Narrative/NarrativeDraftValidator.cs :240 sha=d652fb3a8acdde68e00fc3d61e57d9448e95379feee0aaf006af944f2bf0d1b0 bytes=19240`（逐字符等于 §〇.4 件 2 表那一行 ⇒ 件 2 满足）；`status --short`＝**1 行**＝` M src/Pry.Core/Narrative/NarrativeDraftValidator.cs`（`src/` 产品文件，全程未出现 `tests/**`）；`git diff --unified=0` 只那一行 `@@ -240 +240 @@` ＝ `-                else if (!content.Intents.Any(i => string.Equals(i.IntentId, action.DeferredIntentId, StringComparison.Ordinal)))` / `+                else if (false)`。归档说明：本轮的 `out/apply-f.txt` 只含 APPLIED 行，status／diff 两面当时打在终端并原样抄进本行（自轮 g 起整段录制并入 `out/apply-<轮>.txt`） |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/NarrativeDraftValidator.cs \| 2 +-` ⇒ `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤ `dotnet test` 末行原样 | `失败!  - 失败:     1，通过:    55，已跳过:     0，总计:    56，持续时间: 296 ms - Pry.Core.Tests.dll (net10.0)`；全量 stdout＝`out/run-f.txt`（11:54:21→11:54:29）。编译产物行（件 7 的正面凭据）＝`Pry.Core -> …\carrier\src\Pry.Core\bin\Release\net10.0\Pry.Core.dll`＋`Pry.Core.Tests -> …\Pry.Core.Tests.dll` 在场，且 `warning CS0162: 检测到无法访问的代码` 落 `NarrativeDraftValidator.cs(242,21)`＝改后 `else if (false)` 支体的行，与突变面自洽 |
| ⑥ `.trx` | **缺失**：本刀本轮命令行漏带 `--logger trx`，`out/trx-f/` 目录在盘上为**空**（当场 `ls` 与 `find carrier -name "*.trx"` 皆 0 枚）。旁证＝波 41／波 40 的 `out/run-*.txt` 第 18 行／第 24 行有 `结果文件: …\trx-a\….trx` 这类行，本刀 `out/run-f.txt` 里该类行 0 处 ⇒ 缺的是产物不是判定 |
| ⑦ 红集 | **TRX 口径无从取**（件 5 的读数只认 `<UnitTestResult … outcome="Failed">` 开标签）。stdout 面（**非 TRX 口径、不作签署凭据**）显示 1 枚：`Pry.Core.Tests.NarratorPrototypeTests.Deferred_action_without_a_pointing_intent_is_refused`，`Assert.Equal() Failure: Values differ / Expected: ContractRejected / Actual: Deferred`，帧行 `…NarratorPrototypeTests.cs:line 620`＝§一.2 表预测的那一行（@614 起始／:620 断言帧，两种口径本行分标） |
| ⑧ 还原命令 | `node .local-notes/wave42-1029/w42-apply.js revert --id=W42-f`（通道内部＝从 `git show 51f149d:src/Pry.Core/Narrative/NarrativeDraftValidator.cs` 取字节再写文件；未用 `checkout`／`restore`） |
| ⑨ 还原后 | `git -C carrier status --short`＝**0 行**（当场数） |
| ⑩ SHA 对账 | 通道打印 `REVERTED W42-f … sha=88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c sameAsBlob=true sameAsMainWorkcopy=true`；还原后当场 `sha256sum` 判据三面＝主树副本／carrier 副本／`git show 51f149d:` 的 blob 皆 `4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`（逐字符等于 §〇.1）；载体 `rev-parse HEAD` 仍＝`51f149d…` |
| ⑪ 预检段 | **P116**（`out/preflight-P116.txt`）：`VERDICT=BUILD_OK`；`FOREGROUND: QQ pid=27508 class=Chrome_WidgetWin_1 style=0x14C70000 rect=-1544,244,993x802 screen=1920x1080 covers=False borderless=False title=QQ`；另三枚 informational borderless＝NVIDIA Overlay pid=18308／TextInputHost pid=21788／explorer pid=7404；`gpu_util=22% used=1261MB`；`free_memory_gb=12.8`；时刻 11:54:05→11:54:06；HOLD 重跑 0 枚 |

本轮处置（件 4／件 5 口径下的登记，不代裁）：

- **本轮无效**：判定门是"实到恰 1 枚"，而实到只能按件 5 从 `.trx` 开标签计；本轮 `.trx` 一枚未生成 ⇒ 本刀**没有可裁的读数面**。stdout 那枚红与 §一.2 闭合表逐字段同向（同一格名、同一 `Assert.Equal()` 族、同一帧行 `:620`），但它是 stdout 面不是 TRX 面，本刀不据它签署、也不据它把轮 f 记为有效。
- 定向额度：轮 f 的那一次 `dotnet test` **已动用且不可回收**（本刀不自我扩额，不再为轮 f 补跑第二次——补跑＝定向第 6 次、`dotnet` 总数 6 且其中一枚是重复轮，超 §四.3 那条 ≤5）。故本刀定向实跑＝f（无 TRX）＋g＋h＋i＋j＝**5/5**，`dotnet` 总调用＝**5/6**。
- 后果：§四.2 的"五轮全部有效才起 F"条件不成立 ⇒ **不起收尾全量 F**（省一次防火墙弹窗），登记跳过理由＝轮 f 无 TRX 凭据。
- 交回守门人核的面：#30 `Deferred_action_without_a_pointing_intent_is_refused`（@614）在轮 f 的这次施加里**没有出现多响形状**（stdout 只 1 枚、total 恰＝§〇.1 的 56），但签署资格取决于守门人对"stdout 面能否作为该格的见证面"的口径裁量；本刀不自裁、不追认。成因逐字登记＝本刀命令形状缺 `--logger trx`，非载体、非判据、非通道。

### 二.2 轮 g（`src/Pry.Core/Narrative/NarrativeDraftValidator.cs:76`，`if (occurrences > 1)` → `if (false)`）

| 项 | 当场值 |
|---|---|
| ① 被改面 | `NarrativeDraftValidator.cs:76`（通道施加前 OLD===现文逐字符断言＋整行出现恰 1 次） |
| ② 施加前盘上态 | `rev-parse HEAD`＝`51f149d0b722e28a6566c93d19f48a94ca4c85df`；`status --short`＝**0 行**；toplevel＝`…/.local-notes/qa-wave33-1028/carrier`（通道在写文件前自证）；判据三面本轮施加前的替代面＝0 行＋HEAD 未动（还原后在 ⑩ 当场逐面打印） |
| ③ 施加后盘上态 | `APPLIED W42-g 轮 g src/Pry.Core/Narrative/NarrativeDraftValidator.cs :76 sha=8e3a6c3e355cb7a1b6bbe44b28c168501c01151a710a831c87f9e2f660fa613a bytes=19328`（逐字符等于 §〇.4 件 2 表轮 g 那一行）；`status --short`＝**1 行**＝` M src/Pry.Core/Narrative/NarrativeDraftValidator.cs`；`diff --unified=0`＝`@@ -76 +76 @@ public static class NarrativeProseBinder` 下 `-        if (occurrences > 1)` / `+        if (false)`（整段录制＝`out/apply-g.txt`） |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/NarrativeDraftValidator.cs \| 2 +-` ⇒ `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤ `dotnet test` 末行原样 | `失败!  - 失败:     1，通过:    55，已跳过:     0，总计:    56，持续时间: 220 ms - Pry.Core.Tests.dll (net10.0)`；stdout 归档＝`out/run-g.txt`（11:56:17→11:56:22）；构建产出行 `Pry.Core -> …dll`／`Pry.Core.Tests -> …dll` 在场（件 7 正面凭据；本轮命令行同样未带 `--no-build`，增量编译由 `dotnet test` 自带） |
| ⑥ `.trx` | `.local-notes/wave42-1029/out/trx-g/result-g.trx`（83976 字节，mtime 10-29 11:56；stdout 第 15 行的 `结果文件:` 与之逐字符同路径）；仪器读数归档＝`out/reading-g.txt` |
| ⑦ 红集（1 枚） | 全名＝`Pry.Core.Tests.NarratorPrototypeTests.Ambiguous_quote_is_rejected_instead_of_the_host_choosing_one`（`testName` 全限定、无参数表⇒非 Theory 参数行）；格起始 **@658**／TRX 断言帧 **:665**（两口径分标）；消息要义＝`Assert.Single() Failure: The collection was empty`；性质＝**断言级红**（`family=ASSERT-RED(断言级)`，非整格异常红）；全部帧＝`NarratorPrototypeTests.cs:665` 一枚 |
| ⑧ 还原命令 | `node .local-notes/wave42-1029/w42-apply.js revert --id=W42-g`（内部＝`git show 51f149d:<path>` 取字节再写文件） |
| ⑨ 还原后 | `git -C carrier status --short`＝**0 行**（当场数，`out/revert-g.txt`） |
| ⑩ SHA 对账 | 通道 `REVERTED W42-g … sha=88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c sameAsBlob=true sameAsMainWorkcopy=true`；还原后判据三面当场 `sha256sum`＝主树／carrier／blob 皆 `4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`（逐字符等于 §〇.1）；载体 HEAD 仍＝`51f149d…` |
| ⑪ 预检段 | **P117**（`out/preflight-P117.txt`）：`VERDICT=BUILD_OK`；`FOREGROUND: msedge pid=52656 class=Chrome_WidgetWin_1 style=0x16CF0000 rect=274,223,1356x896 screen=2560x1440 covers=False borderless=False`（前台窗为主屏内非全屏带边框窗）；informational borderless 三枚＝NVIDIA Overlay 18308／TextInputHost 21788／explorer 7404；`gpu_util=2% used=1535MB`；`free_memory_gb=11.3`；时刻 11:56:07；HOLD 重跑 0 枚 |

对账与裁决面（仪器 `w42-count.js` 原样，`out/reading-g.txt`）：

- 仪器行＝`nodes=56 total=56 executed=56 passed=55 failed(counters)=1 notExecuted(attr)=0 rawOutcomeFailedStrings=2 altWayNodes=1 openTagFailed=1`；`NotExecuted 开标签=（0 枚）`；三向同值 **openTagFailed 1 ＝ Counters failed 1 ＝ 反向走读 altWayNodes 1**；`rawOutcomeFailedStrings=2` 与开标签的差＝**1**，第 2 处字面量当场核为 `<ResultSummary outcome="Failed">`（件 5：不算用例）。
- `total`＝**56**＝§〇.1 当场格数（等式 53＋1×3）⇒ 门槛成立。
- 实到＝**恰 1**＝§一.3 闭合表的 { @658 }，且红名／断言帧 `:665`／消息族 `Assert.Single() Failure: The collection was empty` 三面逐字符等于该表 ⇒ **轮 g 有效**。
- 关键判不连坐面本轮实测支持（不推翻）：@644（quote 不在正文里＝occurrences 0，走 `:71` 那支）与 @628（唯一命中，走 `:81` 那支）本轮全绿 ⇒ "`:76` 只对 occurrences>1 的形状生效"这条推演与盘上一致。
- 累计：`dotnet` 2/6、定向 2/5（另见 §二.1：轮 f 的定向调用已动用但无 TRX）、构建额度 0/1（依 §五.2 三段判定合法跳过）、HOLD 0/16。

### 二.3 轮 h（`src/Pry.Core/Narrative/NarrativeDraftValidator.cs:167`，`if (perceivedReason != NarrativeContractReason.None)` → `if (false)`）

| 项 | 当场值 |
|---|---|
| ① 被改面 | `NarrativeDraftValidator.cs:167`（施加前 OLD===现文逐字符＋整行恰 1 次） |
| ② 施加前盘上态 | `HEAD`＝`51f149d0b722e28a6566c93d19f48a94ca4c85df`；`status --short`＝**0 行**；toplevel 由通道在写文件前自证；判据三面本轮施加前的替代面＝0 行＋HEAD 未动（还原后 ⑩ 逐面打印） |
| ③ 施加后盘上态 | `APPLIED W42-h 轮 h src/Pry.Core/Narrative/NarrativeDraftValidator.cs :167 sha=9f0f216845d99896cfbc635db12fc42b4f0432ba0ef5c919d3a7bf872383e57d bytes=19296`（逐字符等于 §〇.4 件 2 表轮 h 行）；`status --short`＝**1 行**＝` M src/Pry.Core/Narrative/NarrativeDraftValidator.cs`；`diff -U0`＝`@@ -167 +167 @@ public static class NarrativeDraftValidator` 下 `-        if (perceivedReason != NarrativeContractReason.None)` / `+        if (false)`（整段＝`out/apply-h.txt`） |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/NarrativeDraftValidator.cs \| 2 +-` ⇒ `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤ `dotnet test` 末行原样 | `失败!  - 失败:     1，通过:    55，已跳过:     0，总计:    56，持续时间: 225 ms - Pry.Core.Tests.dll (net10.0)`；stdout＝`out/run-h.txt`；构建产出行两枚 `-> …dll` 在场（件 7 正面凭据） |
| ⑥ `.trx` | `out/trx-h/result-h.trx`（stdout `结果文件:` 行同路径）；仪器读数＝`out/reading-h.txt` |
| ⑦ 红集（1 枚） | 全名＝`Pry.Core.Tests.NarratorPrototypeTests.Perceived_reference_outside_the_batch_is_refused`（无参数表）；格起始 **@722**／断言帧 **:730**；消息要义＝`Assert.Single() Failure: The collection was empty`；性质＝**断言级红**（`family=ASSERT-RED(断言级)`）；全部帧一枚＝`NarratorPrototypeTests.cs:730` |
| ⑧ 还原命令 | `node .local-notes/wave42-1029/w42-apply.js revert --id=W42-h`（从 `git show 51f149d:<path>` 取字节重写） |
| ⑨ 还原后 | `status --short`＝**0 行**（当场数，`out/revert-h.txt`） |
| ⑩ SHA 对账 | `REVERTED W42-h … sha=88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c sameAsBlob=true sameAsMainWorkcopy=true`；判据三面＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4` ×3；HEAD 仍 `51f149d…` |
| ⑪ 预检段 | **P118**（`out/preflight-P118.txt`）：`VERDICT=BUILD_OK`；`FOREGROUND: msedge pid=52656 … rect=274,223,1356x896 screen=2560x1440 covers=False borderless=False`；`gpu_util=0% used=1439MB`；`free_memory_gb=11.5`；时刻 11:56:53；HOLD 0 枚 |

对账与裁决面（`out/reading-h.txt` 原样）：

- 仪器行＝`nodes=56 total=56 executed=56 passed=55 failed(counters)=1 notExecuted(attr)=0 rawOutcomeFailedStrings=2 altWayNodes=1 openTagFailed=1`；`NotExecuted 开标签=（0 枚）`；三向同值 1＝1＝1；`rawOutcomeFailedStrings − openTagFailed ＝ 1` 的第 2 处字面量＝`<ResultSummary outcome="Failed">`（不算用例）。
- `total`＝**56**＝§〇.1 当场格数 ⇒ 门槛成立。
- 实到＝**恰 1**＝§一.4 闭合表 { @722 }，红名／`:730`／消息族三面逐字符等于表 ⇒ **轮 h 有效**。
- 判不连坐面本轮实测支持：@735（水位越界但 perceived 默认在批次内）本轮全绿，与 §一.4／§一.5 "`:167`／`:173` 是 `CheckConsumption` 里平行的两道门、@735 只中 `:173`" 的推演一致；@746（prose 空白，两枚拒绝）也全绿 ⇒ "`:167` 不是它的拒绝来源"成立。
- 累计：`dotnet` 3/6、定向 3/5、构建 0/1、HOLD 0/16。

### 二.4 轮 i（`src/Pry.Core/Narrative/NarrativeDraftValidator.cs:173`，`if (!content.ProcessedThrough.WithinBatch(request.BatchHighestSeq))` → `if (false)`）

| 项 | 当场值 |
|---|---|
| ① 被改面 | `NarrativeDraftValidator.cs:173`（施加前 OLD===现文逐字符＋整行恰 1 次） |
| ② 施加前盘上态 | `HEAD`＝`51f149d0b722e28a6566c93d19f48a94ca4c85df`；`status --short`＝**0 行**；toplevel 由通道自证；判据三面的替代面同 §二.2②（0 行＋HEAD 未动），还原后 ⑩ 逐面打印 |
| ③ 施加后盘上态 | `APPLIED W42-i 轮 i src/Pry.Core/Narrative/NarrativeDraftValidator.cs :173 sha=4a5a7f4d9683a91ee5ab6a973f9c5d087e59176d8341f124607aad20d477f938 bytes=19281`（逐字符等于 §〇.4 件 2 表轮 i 行）；`status --short`＝**1 行**（同一枚 `src/` 产品文件）；`diff -U0`＝`@@ -173 +173 @@ public static class NarrativeDraftValidator` 下 `-        if (!content.ProcessedThrough.WithinBatch(request.BatchHighestSeq))` / `+        if (false)`（整段＝`out/apply-i.txt`） |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/NarrativeDraftValidator.cs \| 2 +-` ⇒ `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤ `dotnet test` 末行原样 | `失败!  - 失败:     1，通过:    55，已跳过:     0，总计:    56，持续时间: 214 ms - Pry.Core.Tests.dll (net10.0)`；stdout＝`out/run-i.txt`；构建产出行两枚在场 |
| ⑥ `.trx` | `out/trx-i/result-i.trx`（stdout `结果文件:` 同路径）；仪器读数＝`out/reading-i.txt` |
| ⑦ 红集（1 枚） | 全名＝`Pry.Core.Tests.NarratorPrototypeTests.Watermark_beyond_the_batch_upper_bound_is_refused`；格起始 **@735**／断言帧 **:741**；消息要义＝`Assert.Single() Failure: The collection was empty`；性质＝**断言级红**；全部帧一枚＝`NarratorPrototypeTests.cs:741` |
| ⑧ 还原命令 | `… w42-apply.js revert --id=W42-i` |
| ⑨ 还原后 | `status --short`＝**0 行**（当场数，`out/revert-i.txt`） |
| ⑩ SHA 对账 | `REVERTED W42-i … sha=88ebf5c3… sameAsBlob=true sameAsMainWorkcopy=true`；判据三面＝`4f1b2f6e…` ×3；HEAD 仍 `51f149d…` |
| ⑪ 预检段 | **P119**（`out/preflight-P119.txt`）：`VERDICT=BUILD_OK`；`FOREGROUND: msedge pid=52656 … covers=False borderless=False`；`gpu_util=5% used=1439MB`；`free_memory_gb=11.5`；时刻 11:57:30；HOLD 0 枚 |

对账与裁决面（`out/reading-i.txt` 原样）：

- 仪器行＝`nodes=56 total=56 executed=56 passed=55 failed(counters)=1 notExecuted(attr)=0 rawOutcomeFailedStrings=2 altWayNodes=1 openTagFailed=1`；`NotExecuted 开标签=（0 枚）`；三向同值 1＝1＝1；差值面＝`rawOutcomeFailedStrings(2) − openTagFailed(1) ＝ 1`（第 2 处＝`<ResultSummary outcome="Failed">`）。
- `total`＝**56**＝当场格数门槛。
- 实到＝**恰 1**＝§一.5 闭合表 { @735 }，三面（红名／`:741`／消息族）逐字符等于表 ⇒ **轮 i 有效**。
- 判不连坐面本轮实测支持：@722（perceived 越界、水位默认 101 在批次内）本轮全绿 ⇒ "两轮锚点 `:167`／`:173` 各钉一道门、互不串"这条与 §二.3 的实测面同向；@802（签发基线 90，水位默认）也全绿 ⇒ `:173` 不是它的拒绝来源（它钉的是 `R:250`，见 §一.6）。
- 累计：`dotnet` 4/6、定向 4/5、构建 0/1、HOLD 0/16。

### 二.5 轮 j（`src/Pry.Core/Narrative/NarrativeRoundRunner.cs:250`，`if (draft.ReceivedThroughSeqAtIssue != request.ProcessedThroughAtIssue.Seq)` → `if (false)`）

| 项 | 当场值 |
|---|---|
| ① 被改面 | `NarrativeRoundRunner.cs:250`（本刀第二枚文件；施加前 OLD===现文逐字符＝`true`、现文长＝OLD 长＝83、整行出现恰 1 次） |
| ② 施加前盘上态 | `HEAD`＝`51f149d…`；`status --short`＝**0 行**（即 §〇.1 三面同值面的充分替代面：HEAD 未动＋0 行 ⇒ 全部跟踪文件逐字节等于 `blob(51f149d)`；本轮施加前未单独再打判据 SHA，还原后在 ⑩ 逐面当场打印） |
| ③ 施加后盘上态 | `APPLIED W42-j 轮 j src/Pry.Core/Narrative/NarrativeRoundRunner.cs :250 sha=681d6bf2953621024e8d3b62ae263d0adc99c1653212174815f71cedbe55ffc4 bytes=16703`（逐字符等于 §〇.4 件 2 表轮 j 行）；`status --short`＝**1 行**＝` M src/Pry.Core/Narrative/NarrativeRoundRunner.cs`；`diff -U0`＝`@@ -250 +250 @@ public sealed class NarrativeRoundRunner` 下 `-        if (draft.ReceivedThroughSeqAtIssue != request.ProcessedThroughAtIssue.Seq)` / `+        if (false)`（整段＝`out/apply-j.txt`）；本轮出现的是第二枚文件名，`tests/**` 全程 0 行 |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/NarrativeRoundRunner.cs \| 2 +-` ⇒ `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤ `dotnet test` 末行原样 | `失败!  - 失败:     1，通过:    55，已跳过:     0，总计:    56，持续时间: 218 ms - Pry.Core.Tests.dll (net10.0)`；stdout＝`out/run-j.txt`；构建产出行两枚 `-> …dll` 在场 |
| ⑥ `.trx` | `out/trx-j/result-j.trx`（stdout `结果文件:` 同路径）；仪器读数＝`out/reading-j.txt` |
| ⑦ 红集（1 枚） | 全名＝`Pry.Core.Tests.NarratorPrototypeTests.Draft_with_a_wrong_issue_baseline_is_refused_as_late`；格起始 **@802**／断言帧 **:808**；消息要义＝`Assert.Single() Failure: The collection was empty`；性质＝**断言级红**；全部帧一枚＝`NarratorPrototypeTests.cs:808` |
| ⑧ 还原命令 | `… w42-apply.js revert --id=W42-j` |
| ⑨ 还原后 | `status --short`＝**0 行**（当场数，`out/revert-j.txt`） |
| ⑩ SHA 对账 | `REVERTED W42-j … sha=961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9 sameAsBlob=true sameAsMainWorkcopy=true`；判据三面＝`4f1b2f6e…` ×3；HEAD 仍 `51f149d…` |
| ⑪ 预检段 | **P120**（`out/preflight-P120.txt`）：`VERDICT=BUILD_OK`；`FOREGROUND: msedge pid=52656 … rect=274,223,1356x896 screen=2560x1440 covers=False borderless=False`；`gpu_util=7% used=1441MB`；`free_memory_gb=11.3`；时刻 11:58:01；HOLD 0 枚 |

对账与裁决面（`out/reading-j.txt` 原样）：

- 仪器行＝`nodes=56 total=56 executed=56 passed=55 failed(counters)=1 notExecuted(attr)=0 rawOutcomeFailedStrings=2 altWayNodes=1 openTagFailed=1`；`NotExecuted 开标签=（0 枚）`；三向同值 1＝1＝1；`2 − 1 ＝ 1` 的第 2 处字面量＝`<ResultSummary outcome="Failed">`。
- `total`＝**56**＝当场格数门槛。
- 实到＝**恰 1**＝§一.6 闭合表 { @802 }，三面逐字符等于表 ⇒ **轮 j 有效**。
- 判不连坐面本轮实测支持（这是 `CheckSnapshotFreshness` 三支次序的关键面）：@776（故事版本 6≠7，走 `:238` 支）与 @790（代次不符，走 `:244` 支）本轮**全绿** ⇒ "三支按 `:238`→`:244`→`:250` 顺序短路，前两支命中的格读不到 `:250`"这条推演（§一.6 的 D 组分母依据）与盘上一致；同时说明轮 j 的突变没有把波 41 e 轮那枚锚点（`:244`）的判定面带下水。
- 累计：`dotnet` 5/6、定向 5/5、构建 0/1、HOLD 0/16、预检 5 份全 BUILD_OK。

## 三 还原与 SHA 核（五轮跑完、F 判定之前先核；原始输出＝`out/revert-j.txt`＋`out/final-state.txt`）

### 三.1 还原动作逐枚（还原命令一律＝从 `git show 51f149d:<path>` 取字节再写文件，全程未用 `checkout`／`restore`）

| 轮 | 还原命令 | 通道打印的还原后 SHA | sameAsBlob | 还原后 `status --short` 行数 | 归档 |
|---|---|---|---|---|---|
| f | `revert --id=W42-f` | `88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c` | true | **0 行** | `out/revert-f.txt`（终端记录；f 轮的 status／SHA 面当场取，归档文件只含 REVERTED 行） |
| g | `revert --id=W42-g` | `88ebf5c3…` | true | **0 行** | `out/revert-g.txt` |
| h | `revert --id=W42-h` | `88ebf5c3…` | true | **0 行** | `out/revert-h.txt` |
| i | `revert --id=W42-i` | `88ebf5c3…` | true | **0 行** | `out/revert-i.txt` |
| j | `revert --id=W42-j` | `961fb49b…` | true | **0 行** | `out/revert-j.txt` |

### 三.2 出场三面 SHA 核（`blob(51f149d)` vs 载体工作副本 vs 主树工作副本，全部当场 `sha256sum`）

| 文件 | blob(51f149d) | carrier 工作副本 | 主树工作副本 | 三面同值 |
|---|---|---|---|---|
| `src/Pry.Core/Narrative/NarrativeDraftValidator.cs`（轮 f/g/h/i） | `88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c` | 同 | 同 | YES |
| `src/Pry.Core/Narrative/NarrativeRoundRunner.cs`（轮 j） | `961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9` | 同 | 同 | YES |
| `tests/Pry.Core.Tests/NarratorPrototypeTests.cs`（判据，本刀一枚不写） | `4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4` | 同 | 同 | YES（且逐字符等于 §〇.1 入场基准） |

### 三.3 载体与主树终态

- 载体：`status --short`＝**0 行**；`log --oneline fd93ebe..HEAD`＝**2 行**；`rev-parse HEAD`＝`51f149d0b722e28a6566c93d19f48a94ca4c85df`；`diff --stat`＝**0 行**。
- 主树：`git status --short --branch`＝**10 行**＝入场那 9 行＋本刀新建的本报告一枚 `?? docs/eval/reports/2026-10-29-hds13-dclass-second-slice.md`；他人的两枚 `M `、一枚 ` M`、五枚 `??` 全部原样在场（本刀一字未动）；主树 `HEAD` 仍＝`ae5d702463244f6649788207308bfd615bc6c982`（本刀零 git 写动作）。
- 忽略产物面（诚实登记，不作判定凭据）：载体的 `bin/`／`obj/` 不在跟踪面里，最后一次增量编译（mtime 10-29 11:58，轮 j）编译自当时的突变态源码；还原后源码字节已回 blob，但本刀**不自起一次"清理构建"来把 dll 也对齐**——那会多花一次构建额度、且工单 §四.1 已按三段判定允许跳过。判据 dll 与本刀任何读数无关（读数只从 `.trx` 取），此面交守门人知悉。

## 四 收尾全量 F＝**未起**，登记理由

- 工单 §四.2 的启用条件是硬门：**五轮全部有效才起**；任一轮作废或不可裁 ⇒ 立即不起。本刀轮 f 无 `.trx` 凭据（见 §二.1⑥／§二.1 末段），不满足"有效"，故 **F 未起**，`dotnet test Pry.slnx -c Release` 动用 0/1，省下一次防火墙弹窗。
- 本刀不以任何方式补这一次 F：不为"看起来完整"多跑一轮 f（那会同时破定向 ≤5 与"每轮一次"两条），也不把轮 f 的 stdout 面升格成 TRX 面。
- 波 40 那枚解决方案级读数（total 1406／executed 1404／failed 0／`NotExecuted` 开标签 2）本刀**未复采**，故它仍是守门人手里的同一面；本刀 §〇.2 只是用自己的仪器把它复算了一遍作为仪器自证面，不构成第二次采样。

## 五 预算、预检与产物形状

### 五.1 四类预算的动用（上限／动用／说明）

| 预算 | 上限 | 动用 | 说明 |
|---|---|---|---|
| 解决方案级 `dotnet build` | ≤1 | **0** | 依 §五.2 三段判定合法跳过，未动额度 |
| 解决方案级全量 `dotnet test Pry.slnx -c Release` | ≤1 | **0** | §四：五轮未全部有效 ⇒ 不起 |
| 定向 `dotnet test --filter` | ≤5 | **5** | f（已跑，无 `.trx`）／g／h／i／j 各一次；本刀不自我扩额，未为 f 补跑 |
| `dotnet` 总调用 | ≤6 | **5** | 全部为定向；差额 1 枚未用（因 F 不起） |
| HOLD 重跑 | ≤16 | **0** | 五份预检全 `VERDICT=BUILD_OK`，无 HOLD、无等待 |

### 五.2 构建跳过三段判定（当场复取，原始输出＝`out/final-state.txt`；不引用工单或前一波的旧述）

1. **0 行式子成立**：`git diff --stat 51f149d ae5d702 -- src tests scripts Pry.slnx Directory.Build.props`＝**0 行**（`ae5d702`＝本刀入场时主树 `git rev-parse HEAD`，当场复取仍同值；另存 `out/seg1-0line.txt`）⇒ 载体基点树与当前主树在相关字节面逐字符相同，无需另起一次解决方案级构建来"对齐"。
2. **成品在盘**：`…/carrier/src/Pry.Core/bin/Release/net10.0/Pry.Core.dll`（1745408 字节，mtime 10-29 11:58）与 `…/carrier/tests/Pry.Core.Tests/bin/Release/net10.0/Pry.Core.Tests.dll`（1177088 字节，mtime 10-29 11:56）在场，均由本刀定向跑的自带增量编译刷新（忽略产物，见 §三.3 末段）。
3. **SHA 三段未变**：两枚被改产品文件＋判据文件的三面 SHA 逐字符等于 §〇.1／§三.2（当场 `sha256sum`）；载体 `git diff --stat`＝**0 行**、`status --short`＝**0 行**。

三段齐 ⇒ 跳过成立。**旁证面**（件 7 口径）：本刀五份 `run-*.txt` 都是 stdout，不含命令行，所以"没有 `--no-build`"在 `run-*.txt` 里本来就不会出现，不作为凭据；正面凭据是每份里 `Pry.Core -> …dll`／`Pry.Core.Tests -> …dll` 两行产出行在场＋`warning CS0162` 落在被改那一行的支体行号（f→`:242`、g→`:78`、h→`:169`、i→`:175`、j→`:252`），证明编译吃的就是当轮施加后的源码。

### 五.3 预检台账（编号／VERDICT／归档／前台窗／内存／放行哪一轮）

| 编号 | VERDICT | 归档 | 前台窗 pid／class／covers／borderless | `gpu_util`／`used` | `free_memory_gb` | 时刻 | 放行 |
|---|---|---|---|---|---|---|---|
| P116 | BUILD_OK | `out/preflight-P116.txt` | 27508／`Chrome_WidgetWin_1`／False／False（QQ，`rect=-1544,244`＝第二屏外） | 22%／1261MB | 12.8 | 11:54:05 | 轮 f |
| P117 | BUILD_OK | `out/preflight-P117.txt` | 52656／`Chrome_WidgetWin_1`／False／False（msedge，主屏内非全屏） | 2%／1535MB | 11.3 | 11:56:07 | 轮 g |
| P118 | BUILD_OK | `out/preflight-P118.txt` | 52656／同上 | 0%／1439MB | 11.5 | 11:56:53 | 轮 h |
| P119 | BUILD_OK | `out/preflight-P119.txt` | 52656／同上 | 5%／1439MB | 11.5 | 11:57:30 | 轮 i |
| P120 | BUILD_OK | `out/preflight-P120.txt` | 52656／同上 | 7%／1441MB | 11.3 | 11:58:01 | 轮 j |

- 三方对账（不许省）：**逐份 VERDICT 枚数 5 ＝ `dotnet` 次数 5 ＝ 实跑轮数 5**；HOLD 0 份；无"跳过预检起跑"的形状；F 未起 ⇒ 没有第 6 份预检（本刀不为本就不起的 F 预跑一枚预检来凑数）。
- 归档归属：`out/preflight-P116b.txt`（mtime 11:2x、`VERDICT=BUILD_OK`）**是守门人预审那把的产物**，本刀未用它放行任何一轮，也未改名、未删除；本刀自己的预检从 P116 起连续编号（工单 §四.6）。波 40 登记过同一枚撞号面，本刀照此写明。

### 五.4 产物枚数与形状（原始输出＝`out/final-state.txt`）

- 盘上本刀 `.trx`＝**4 枚**（`trx-g/result-g.trx`、`trx-h/result-h.trx`、`trx-i/result-i.trx`、`trx-j/result-j.trx`）；F 未起 ⇒ 无全量份。
- 结果目录＝**5 枚**（`trx-f`…`trx-j`），其中 **`trx-f/` 为 0 文件**（轮 f 命令行漏 `--logger trx`，见 §二.1⑥）——这是本刀产物形状里唯一一枚"目录在、凭据不在"的面，不隐藏、不删目录。
- `-voided-` 面：`find .local-notes/wave42-1029 -name "*voided*"`＝**0 枚**（本刀没有登记为作废的轮次目录；轮 f 是"无效但已跑"，不是"多响作废"）。
- 每轮 `total` 对账：g/h/i/j 四份 `.trx` 仪器行全部 `total=56`＝§〇.1 当场格数（53＋1×3）；轮 f 无 `.trx`，其 stdout 末行的"总计: 56"同值但不计入 TRX 对账。
- 每轮的 `apply-<轮>.txt`／`run-<轮>.txt`／`reading-<轮>.txt`／`revert-<轮>.txt` 四件套在场（f 的 apply／revert 只含通道单行，status／diff 面抄在 §二.1③⑨；g 起为整段录制）。
- 仪器与通道的原始脚本在场：`w42-entry.js`／`w42-count.js`／`w42-scan.js`／`w42-apply.js`／`w42-splice.js`；预合成突变文本在 `mutation-prep/`（五枚，件 2 的 SHA 面见 §〇.4）。

## 六 未验证面（本刀留下的，逐条点名）

1. **轮 f 的 TRX 面**：本刀没有对 `NarrativeDraftValidator.cs:240` 这一枚生成过任何 `.trx`，所以"实到恰 1 枚"在这一轮只由 stdout 面支持。该格（#30 @614）的 TRX 见证面＝**未验证**。
2. **解决方案级 F 面**：未起（§四），本刀对四程序集全量没有任何新读数；波 40 那枚 1406／1404／0／2 仍是唯一在册面，本刀未复采。
3. **`:235` 的 null 支**：本刀按 §一 前言独立复核了"当前 56 格里该支函数体从未被执行"（推演＋当场 `DeferredAction(` 四枚调用点），但**没有实测**（那是波 41 c 轮退回的原锚点，不在本刀五轮配方里）。
4. **`NarrativeCommitValidator.cs:385` 复用 `NarrativeDraftValidator.Validate` 这条路径**：本刀只验证了"判据文件里 `NarrativeCommitValidator`／`NarrativeCommitWriter`／`NarrativeSqliteStore` 三个名字提及 0 处"这一可达性面（`out/closure-scan.txt` 末段），没有对这三条入口本身做任何突变或读数；`NarrativeCommitWriter.cs:178`／`Storage/NarrativeSqliteStore.cs:492` 两处调用者的守护证据＝**未验证**（属波 22 审计 §七 T5 那类"拆门无人守"，不在本刀范围）。
5. **`ImmediateBranch`（`NarrativeRoundRunner.cs:179-230`）在轮 j 改后是否可能产出 `HistoryRejected`**：本刀按推演认定该形状不会（§一.6 表内），实测面只到"本轮红集恰 1、其余 55 枚全绿"，没有对历史支单独取数。
6. **零产品新字节（终态）的字节面**：本刀以"三面 SHA 逐字符相等＋载体 `status --short` 0 行＋`diff --stat` 0 行"为充分面（§三.2／§三.3），没有再对整棵树做逐文件字节比对。

## 七 交回守门人核的事（照工单 §五 那八件，逐件给当场读数，不代裁）

1. **五轮各自实到红枚数与红名／TRX 断言帧行／消息族 vs §一 落档表**：轮 g＝1 枚 { @658／:665／`Assert.Single() Failure: The collection was empty` }＝§一.3 表逐字符同；轮 h＝1 枚 { @722／:730／同族 }＝§一.4 表同；轮 i＝1 枚 { @735／:741／同族 }＝§一.5 表同；轮 j＝1 枚 { @802／:808／同族 }＝§一.6 表同。轮 f＝**无 TRX 实到面**（stdout 面为 1 枚 { @614／:620／`Assert.Equal() Expected: ContractRejected / Actual: Deferred` }，与 §一.2 表同向）⇒ 本刀判轮 f 无效、四轮（g/h/i/j）有效；哪一轮算签、哪一轮不签由守门人裁。
2. **判据 SHA 每轮打印面**：g／h／i／j 四轮在**还原后**各当场 `sha256sum` 三面（`out/revert-<轮>.txt`），f 轮同样在还原后打印三面（当场终端，抄在 §二.1⑩）；**施加前**那一面本刀没有逐轮单独打印，替代面＝载体 `status --short` 0 行＋`rev-parse HEAD` 未动（blob 同值的充分面），本刀已按工单 §五.2 要求在每轮 ② 项明写"这是替代面"。入场与出场各有三面逐字符同值面（§〇.1／§三.2）。
3. **载体终态三件＋被改文件三面 SHA**：`status --short`＝**0 行**／`fd93ebe..HEAD`＝**2 行**／`rev-parse HEAD`＝`51f149d0b722e28a6566c93d19f48a94ca4c85df`；两枚产品文件＋判据的三面 SHA 见 §三.2（`NarrativeDraftValidator.cs`＝`88ebf5c3…`、`NarrativeRoundRunner.cs`＝`961fb49b…`、判据＝`4f1b2f6e…`）。
4. **收尾全量**：**未起**（§四），故无求和式、无跳过两枚的逐字符点名；本刀不假装起过。
5. **盘上 `.trx` 枚数／结果目录枚数／有无 `-voided-`／每轮 `total` 对账**：4 枚／5 目录（`trx-f` 空）／0 枚／四轮 `total` 全＝56＝§〇.1 当场值（§五.4）。
6. **四类预算动用与上限**：构建 0/1（跳过成立）、全量 0/1、定向 5/5、`dotnet` 总 5/6、HOLD 0/16（§五.1）。
7. **每一枚红的性质**：轮 g/h/i/j 四枚全部＝**断言级红**（仪器 `family=ASSERT-RED(断言级)`，单帧落判据文件行 :665/:730/:741/:808，消息分别是 `Assert.Single()` 族的 "The collection was empty" ×4）；轮 f 的 stdout 那枚同属断言级（`Assert.Equal()` 族）但不是 TRX 面。整格异常红＝0 枚（无 `EXCEPTION-RED` 分类、无 `NotExecuted` 开标签）。
8. **未领账与判不清面逐枚登记**：
   - 未领账＝#30 `Deferred_action_without_a_pointing_intent_is_refused`（@614／`:240`）：本轮配方与施加点都对（`out/apply-f.txt` 的 SHA 与 §〇.4 件 2 表逐字符同、`diff -U0` 只那一行），缺的是本刀的运行命令行形状（`--logger trx`）⇒ 无 TRX 凭据。请守门人裁：该格能否凭 stdout 面记见证，或需要另出一把只补这一轮的刀。
   - 判不清面①＝轮 f 的闭合表里"其余 55 枚不连坐"这一支撑面：本刀用的是**逐枚扫描**口径（`out/closure-scan.txt` 轮 f 段 56 行、候选 1 枚），扫描器＝`w42-scan.js`，被扫集合＝§一.1 的 56 枚（A20＋B30＋C5＋D1，@437 三枚参数行各计一格）；转录自证 12 面 AGREE 在 `out/closure-scan.txt` 第 299-311 行。若守门人认为"其余格都不读这行"需要更强的反证（例如对每枚格单独取断言级证据），本刀登记为未验证面六.1／六.5。
   - 判不清面②＝工单 §二 里 f 轮那句守门人静态论证与波 41 §一.1 的 c 轮结论：本刀独立重推后**方向一致**（`:235` 的 null 支在本树恒不执行、@614 真钉 `:240`），但本刀没有实测 `:235`（六.3）。
   - 判不清面③＝`NarrativeDraftValidator.cs` 与 `NarrativeRoundRunner.cs` 在 `bin/obj` 里的忽略产物（§三.3 末段）：源码字节已回 blob，编译产物仍停在轮 j 的突变态；本刀不据它作判定，是否需要在出场面上处理由守门人定。
   - 本刀对波 41 报告 §一.4 括号里那条"经 `NarrativeCommitValidator` 复用"的说法做了范围修正（§一.7 末条）：那条对本刀五枚锚点的判据可达面不成立，凭据＝判据 blob 里三个名字 0 处提及。这是对本刀闭合表分母的支撑面，不是对上一把结论的改写请求。

## 九 守门人裁决（追加节；§〇–§七 一字未改，本节为纯追加）

### 九.0 我这把用的独立仪器、它的自证，以及我自己那处同族错

- 第十一件独立仪器＝`.local-notes/wave42-1029/gk42-verify.js`（node 从零写，lane 的 `w42-count.js`／`w42-apply.js` 一行没读）。口径＝件 5：只数 `<UnitTestResult … outcome="Failed">` 开标签，与同份 `<Counters failed>` 正反向双向对账，`<ResultSummary outcome="Failed">` 不算用例，属性按名字取不按顺序，栈帧按 `in <路径>:line N` 锚定，属性值里的 XML 实体先解码再比。
- 裁本刀之前先对八面带四元限定（波次＋产物＋判据文件／程序集＋跑法）的老读数复算＝**8/8 AGREE**：波 41 `trx-a` 恰 1／断言帧 :526、`trx-b` 恰 1／:606、`trx-d` 恰 1／:447（`scope:"conversation"` 参数行在解码 `&quot;` 后可见）、`trx-e` 恰 1／:796；波 40 `trx-M8p` 恰 5／帧 {540,555,569,582,865}、`trx-m08` 恰 4／{289,320,961,981}；波 13 `run-full` 四份 total 求和 607＋108＋280＋233＝**1228**／failed 0＋0＋2＋0＝**2**／`NotExecuted` 开标签 **2**（都在 607 那份）；波 40 `trx-F` 四份 108＋785＋280＋233＝**1406**／executed **1404**／failed **0**／`NotExecuted` **2**。
- 我自己的仪器第一次上场就犯本仓登记过的同族错（当场更正、不涂改原文）：对账谓词初稿写成读 `r.rawLiteral`，而返回对象里的字段名是 `rawFailedLiteral` ⇒ 八面全部打印 `MISMATCH`，而三面数字本身处处相等。处置按"仪器说 MISMATCH 先证明坏的是仪器还是表"这条老口径走＝先当场证明是仪器（原始 `grep` 该份 `<ResultSummary>` 字面确为 `outcome="Failed"`、`outcome="Failed"` 字面量枚数确为 2），再改我的仪器那一个字段名，八面读数与 lane 产物、判据字节一字未动，改后 **8/8 AGREE**。顺带登记一条正面事实：这个 bug 能在八面数学全对的情况下集体报警，说明 `rawLiteral − openTagFailed == (ResultSummary 是否 Failed ? 1 : 0)` 这条反向走法真的在咬，不是摆设。
- 载体终态我自己当场重取（不引用 §三）：`rev-parse HEAD`＝`51f149d0b722e28a6566c93d19f48a94ca4c85df`／`status --short`＝**0 行**／`log --oneline fd93ebe..HEAD`＝**2 行**／`diff --stat`＝**0 行**；`sha256sum` 三面（carrier 工作副本／主树工作副本／`git show 51f149d:` 的 blob）＝`NarrativeDraftValidator.cs` **88ebf5c3…**、`NarrativeRoundRunner.cs` **961fb49b…**、判据文件 **4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4**，三枚各自三面逐字符同值，且判据面与工单基准、§〇.1 入场值逐字符一致。
- 格数我也从 blob 重取＝`[Fact]` **53**／`[Theory]` **1**／`[InlineData` **3** ⇒ 53＋1×3＝**56**；`Assert.Skip` **0 处**；文件 **989 行**。四份 `.trx` 每份 `total=56` 与这个当场值对得上（本轮次不引用 §〇.1 那行代替读数）。
- 决定性那条 0 行式子我当场复取＝`git diff --stat 51f149d ae5d702 -- src tests scripts Pry.slnx Directory.Build.props`＝**0 行**（`ae5d702`＝本刀入场时主树 `HEAD`，也是上一把的线尾）。

### 九.1 五轮裁决

| 轮 | 锚点（载体 `51f149d` 现文） | 我的仪器实到 | vs §一 落档的闭合表 | 裁决 |
|---|---|---|---|---|
| f | `NarrativeDraftValidator.cs:240` | **无 TRX**：`out/trx-f/` 盘上 **0 文件**、`run-f.txt` 里 `结果文件` 行 **0 处**、全仓本刀产物内 `.trx` 枚数＝4 | stdout 面逐字段同向＝@614／帧 :620／`Assert.Equal() Expected: ContractRejected / Actual: Deferred`／末行"失败: 1，通过: 55，已跳过: 0，总计: 56" | **无效·不可裁**（件 5 的读数面不存在）⇒ #30 不签、账不减 |
| g | `NarrativeDraftValidator.cs:76` | 恰 1＝`Ambiguous_quote_is_rejected_instead_of_the_host_choosing_one`，@658／帧 :665／`Assert.Single() Failure: The collection was empty`，`nodes=56 passed=55 failed=1 NotExecuted=0` | 红名／帧行／消息族三面逐字符同 | **有效** |
| h | `NarrativeDraftValidator.cs:167` | 恰 1＝`Perceived_reference_outside_the_batch_is_refused`，@722／:730／同族 | 同 | **有效** |
| i | `NarrativeDraftValidator.cs:173` | 恰 1＝`Watermark_beyond_the_batch_upper_bound_is_refused`，@735／:741／同族 | 同 | **有效** |
| j | `NarrativeRoundRunner.cs:250` | 恰 1＝`Draft_with_a_wrong_issue_baseline_is_refused_as_late`，@802／:808／同族 | 同 | **有效** |

- 结清 4 枚＝**#33／#38／#39／#44**（按波 22 审计 §四.1 的 T3 点号逐枚对名）。签账资格第八次使用成动作＝判据字节全程未动（三面同值我独立复取，见 §九.0）＋取红的手不是写判据字节的手 ⇒ **这 4 枚的红证与第二换手同场结清**，不分两次还。
- 作废轮 **0** 枚：轮 f 的形状是"跑了但没有凭据"，不是"多响作废"，与 §五.4 那句 `-voided-` 枚数 0 自洽；两轨（作废／退回）不混用。
- 四枚红的性质全部＝**断言级红**（单帧落判据文件、`Assert.Single()` 族），整格异常红 0 枚、`NotExecuted` 0 枚。

### 九.2 为什么我不肯用 stdout 那枚红签 #30（写清裁量理由，不写成形式问题）

1. 件 5 那套口径的全部价值在"开标签枚数与同份 `<Counters failed>` 双向对账＋反向走法"三面；stdout 只给一枚汇总计数器，没有逐枚可枚举的节点面，也就没有**多响**的见证面——而 #30 恰恰是一枚重锚过的格，波 41 的 c 轮正是靠多响面才发现配方指错门。用一面看不见多响的读数去签一枚刚换过锚点的格，等于把这枚格最需要的那道保护关掉。
2. 事后放宽读数口径去追认已经跑完的一轮＝"差 5 秒也不改判据"的反面。正确动作是重跑那一轮，不是重定义判据。
3. 代价可承受且可算：#30 的账形已经三面证明正确（施加点＝`out/apply-f.txt` 的 SHA 逐字符等于 §〇.4 件 2 表、`diff --unified=0` 只那一行、还原后 blob 同值），缺的只是运行命令行的形状；补一次定向跑就能结清，不必重做推演。

### 九.3 新口径 件 8＝"跑"这一侧的命令形状自检（本刀买下来的东西）

- 五轮的"施加"与"还原"两侧各有 SHA 自检，唯独"跑"这一侧没有——那枚缺失的 `--logger trx` 正好落在这个空白面上。代价不是 1/5 而是**1 枚未领格＋1 次解决方案级读数**（§四 的 F 被它联动挡掉），这条"一枚命令形状换两样东西"的兑换比就是它该成为硬前置的理由。
- 从此定向跑的硬前置＝起跑前把完整命令行原样落档（`out/cmd-<轮>.txt`），逐字符核对含 `--logger trx` 与 `--results-directory`（或等价开关）；起跑后当场打印该目录的 `.trx` 枚数，枚数＝0 即刻登记为"本轮无凭据"，不许再往 stdout 找凭据、也不许补跑第二轮来救同一轮。
- 定位说明（防止把它读成判据）：件 8 是**仪器参数**，加的是自检，不放宽任何既有门，也不需要谁批准；改它不需要动产品字节。

### 九.4 其余交回面的核（工单 §五 那八件，逐件给裁）

- **SHA 打印面**：g／h／i／j 四轮还原后三面当场 `sha256sum` 在场；施加前那一面用替代面（载体 `status --short`＝0 行＋`rev-parse HEAD` 未动 ⇒ blob 同值的充分面）并在每轮 ② 项明写"这是替代面"——波 30 立的口径，本刀执行到位，f 轮的替代面同样成立（blob 同值我已独立复取）。
- **四类预算**：`dotnet` **5/6** 全部为定向；定向 **5/5**；解决方案级构建 **0/1**（三段判定我独立复取＝0 行式子＋`Pry.Core.dll` 1745408 字节在盘＋三枚 SHA 三面未变）；全量 **0/1**（依法不起，见 §九.5）；HOLD **0/16**。
- **预检台账**：`preflight-P116`–`P120` 五份逐份 `VERDICT=BUILD_OK`，枚数 5＝`dotnet` 5＝实跑轮 5，三方对账成立，无"跳过预检起跑"的形状；`preflight-P116b.txt`（mtime 11:29:52，比 lane 的 P116 早 24 分 46 秒）是**我预审那一把**的归档，lane 未用它放行任何一轮，撞号面按波 40 口径登记为"两把台账分开署名"，不作违规。
- **产物形状**：`.trx` **4** 枚／结果目录 **5** 枚（`trx-f` 为 0 文件）／`-voided-` **0** 枚——lane 没删那枚空目录、也没把空目录藏起来，这一条登记为诚实面处置正确。
- **未领账与判不清面（lane §七.8 三条）我逐条裁**：
  - (a) f 轮闭合表"其余 55 枚不连坐"＝逐枚扫描（`out/closure-scan.txt` 轮 f 段 56 行）＋12 面转录自证全 AGREE。本刀没有多响形状能反证它，登记为下一把可直接复用的口径面，不判缺陷；但它也**不能**替 #30 换来签账（该格欠的是凭据，不是推演）。
  - (b) `NarrativeDraftValidator.cs:235` 的 null 支恒不执行＝至今只有静态面（波 41 的 c 轮退回＋本刀 §一 前言独立重推）。实测它要走的是"新增夹具形状"那条路＝T4 类设计题，不属证据刀，押在该类刀里，本刀不为凑一次红造夹具。
  - (c) 载体 `bin/`／`obj/` 停在轮 j 的突变态构建产物：源码字节已回 blob、本刀读数只从 `.trx` 取 ⇒ 我裁为**非判定面**；但下一把的"构建跳过三段判定"不许把这枚 dll 当成"清洁二进制"的凭据，它只支撑"成品在盘"那一段，第一轮定向跑自带增量编译会把它洗回清洁态。这条登记成对下一把的具体约束，不是一句"注意"。
- **lane 对波 41 §一.4 那句"经 `NarrativeCommitValidator` 复用"的范围修正**：我当场从载体 blob 重取＝判据文件里 `NarrativeCommitValidator`／`NarrativeCommitWriter`／`NarrativeSqliteStore` 三枚名字提及 **0／0／0** 处 ⇒ 修正成立。裁为"对上一把括号说明的适用范围收紧"，不改写任何签账结论（波 41 那四轮锚点落 `DraftValidator`／`RoundRunner`，与本面无涉）。
- **主树面**：本刀全程零 git 写动作已由我复核＝主树 `HEAD` 仍 `ae5d702…`、`status --short --branch` 在场的是入场那 9 行＋本报告 1 行；他人的两枚 `M ` 暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）、一枚 ` M`（`PROJECT_PLAN.md` 上他人的 18 行在飞）、五枚 `??` 一字未动。

### 九.5 归位裁决＝零落地，以及 F 不起的合法性

- 同一条判据第八次执行：本刀对产品字节零写入、载体 `fd93ebe..HEAD` 恰 2 行且其中没有本刀的手 ⇒ 没有可归位的提交；通道从来不是 `merge --ff-only`（`51f149d` 不是 `main` 的祖先），本把唯一落地面＝文档。
- F 依法不起＝工单 §四.2 那条"五轮全部有效才起"的第一次由"轮次无效"（而非"轮次退回"）触发。这不是少做一件交付，这是那道门买下来的东西：本仓波 39 那枚 M8 就是用一次实测换来的一次误署名。**也不许为顶账另起一次构建或全量**（老口径，第二次写进裁决）。

### 九.6 账（当场从盘求和，不预写加法）

- 本刀结清 **4** 枚＝#33／#38／#39／#44。
- T3 余 **15** 枚，逐枚点名＝#16／#18／#21／#28／#30／#32／#34／#35／#36／#37／#40／#42／#45／#46／#48（其中 #30 欠一次带 TRX 的重跑，#45／#28 的历史行号已实测漂移、下刀前必须按载体现文重取，#34／#36 与波 14 成对同门）。T4 5 枚照旧＝#12／#19／#20／#41／#47（要新增形状或夹具改造，另议）。
- D 类＝T3 15＋T4 5＝**20**；波 20 余 **28**；**旧欠 52 → 48**（求和式＝20＋28＝48，逐项由这行列出实数）。断言级子账本刀不移动。
- 在册基线照旧 **total 1406／executed 1404／failed 0／`NotExecuted` 2**＝不移动（本刀未复采即不移动；§九.0 与 §〇.2 那两次"用仪器把老读数复算一遍"是**仪器自证面**，不构成第二次采样，lane §四 已自明这一点，我照此裁）。
- catalog **76** 未动；拆法原文照旧 **46／45／1＝W2**；格数面本刀净新增 **0** 格、改断言 **0**、新增数字门槛 **0**。

### 九.7 出闸第四十三波＝`docs/dispatch-hds-wave43-1029.md`

- 这一把的形状＝**1 枚补跑＋4 枚新锚**，不是纯补跑：只补 #30 一枚要用掉一整把的预检与构建窗口，而 T3 里恰有四枚的锚点与本轮五枚同族（同一文件、同一类"拆一行既有门即响"），同刀并跑能把件 8 这条新自检一次验证到位。
- 五轮＝**f′**（#30 `NarrativeDraftValidator.cs:240`，与波 42 同锚同预合成 SHA `d652fb3a…`，硬前置＝件 8）／**g′**（#42 `NarrativeRoundRunner.cs:238`，快照三支里 `:238`／`:244`／`:250` 的最后一支未响＝`:238`，@776）／**h′**（#32 `NarrativeDraftValidator.cs:71`，与波 42 g 轮的 `:76` 相邻支，@644）／**i′**（#37 `NarrativeDraftValidator.cs:191` `TargetConversationId` 不符，@711）／**j′**（#40 `NarrativeDraftValidator.cs:131` prose 空白，@746）。五枚锚点我全部按载体 `51f149d` 现文当场重取并逐行抄进工单 §二，含"整行出现恰 1 次"的当场核对。
- 两处连坐风险我在工单里事前点名，不许 lane 用"应该不影响"过关：**j′** 的 `:131` 在波 22 审计里被标为"也是 #23 走的路径之一"，而 #23（@520）已在波 41 签账——默认夹具 `prose = Prose` 非空白、盘上只 @746 传 `prose: "   "`，这两面支持"恰 1"，但闭合表必须由 lane 自己按当前 blob 重推；**i′** 的 `action.TargetConversationId` 在 `NarrativeDraftValidator.cs` 里出现 **2** 处（我当场 `grep -c` 取到 2），必须点名另一处落在哪个分支、改后会不会同场被读到。
- **h′ 的红形状我留一条如实提示**：`:71` 是一支 `return`，摘掉它之后 `occurrences == 0` 的用例会继续往下走，红可能是断言级、也可能是整格异常红，形状不同仍只算 1 枚，但 lane 必须事前把"改后走到哪一行、消息什么形状"写进闭合表，跑完不许回头改预期列。
- 硬前置照旧逐条抄进工单：件 1–件 8（件 8 为本刀新增）、闭合点名表逐枚点名禁止"各恰 N"合并、判据字节一枚不写、载体 `status` 全程只许出现 `src/`、归位与顶账由守门人按路径做；预算＝构建 ≤1（默认三段跳过）／全量 ≤1（**五轮全部起跑且全部有效才起**）／定向 ≤5／`dotnet` ≤6／HOLD ≤16 按刀计，预检编号从 **P121** 起。

### 九.8 押账照旧不减与不许写的句子

- 队列照旧（工单 §七）：波 20 那 28 枚、`used + cost > total` 那一半、`:367`、探针正数面的单独读数、HDS-007 的 `:195`／`:172`、波 28 那枚数值锁格（零门槛是作者定的，不为凑红造门槛）、`PrototypeFiles` 写死清单的覆盖盲区（仍欠"把扫描函数本身弄坏"那一半）、#49（T5 拆门无人守）、#31（那只手写过的判据字节会作废凭证）、两套并行命名债、HDS-010 删除片、HDS-003／007／009 的生产接线（属接线刀不属证据刀）。
- 未验证面一条不减，照本报告 §六.1–§六.6 逐条带走，另加本刀 §九.0 里那两条仍然欠着的实测面（`:235` 的 null 支、`NarrativeCommitWriter.cs:178`／`Storage/NarrativeSqliteStore.cs:492` 两处调用者的守护证据）。更大那张脸照旧全部零读数：真实模型对照、生产接线（这 56 枚格至今零真实调用方）、旧数据继承读取、UI 与对话、生产库与 `%LOCALAPPDATA%\PryCompanion`、待标定参数（是特性不是缺陷）、并发与断电、覆盖率、旧链删除片与"不进包"那道门未开始。
- **四句话照旧不许写**＝新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链。界面与对话今天零变化。
- 本把文件面＝本报告一枚（lane 写 §〇–§七 702 行＋守门人纯追加 §九，删除列恒 0，§〇–§七 一个字节未改）＋第四十三波工单一枚（新建）＋§4.2 的 HDS-003 行一处状态＋§9 一条 entry＋sprint §0 三条事实句与 §3 一条密行。顶账由登记刀落盘暂存后当场 `git ls-files` 实测再写进 §9 与提交信息，**不在本节预写加法**。
