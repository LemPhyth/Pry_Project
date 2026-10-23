# HDS-WAVE26-1023｜第二十六波续跑刀：给第二十五波那 6 枚新格补红证（R3／R4）＋一轮构建与全量收尾（R5）

- 工单：`docs/dispatch-hds-wave26-20261023.md`（id＝`HDS-WAVE26-1023`，性质＝续跑刀第三轮／收尾）
- 执行方：冷启动 lane（第二十六号载体）
- 本报告由 lane 逐轮追加；lane 不提交、不 `git add`、不写状态词与结案词、不自封基线；归位权在守门人。

## 〇 入场读数（全部当场实测，不引用工单数字）

### 〇.1 时间与线尾（主树，创建载体之前）

```
$ date
Fri Oct 23 21:35:57     2026

$ git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD
62f8fd1c3ee9b06f0ed8084a6517be1a29bdc8c1

$ git -C C:/Users/24782/Desktop/Pry_Project log --oneline -3
62f8fd1 docs(plan,sprint,eval,wave25): 收第二十五波并裁决代码不归位——停刀纪律兑现（A2 预检 HOLD 后零 dotnet，runs 实数 2 目录无 full）……（守门人第二十五波收口＋出闸第二十六波工单）
a804a53 docs(plan,sprint,eval,wave24): 收第二十四波纯复采刀并归位……
0790d71 docs(plans,wave25): 预置第二十五波工单……
```

- 入场线尾＝`62f8fd1`（＝第二十五波报告＋守门人 §八 署名节＋第二十六波工单的归位提交）。本刀载体基点＝工单 §〇.3 指定的 `24d1b6f8b0a4a3d199dcab832e15facc84a16a01`（第二十五波在自己载体里写好的交付面，父＝`a804a53`）。

### 〇.2 主树状态（他人资产按硬线只数行不读内容）

`git -C C:/Users/24782/Desktop/Pry_Project status --short --branch` 原样（共 8 行）：

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

- 两枚 `M ` 暂存项与全部 `??`＝他人名下资产：本刀未读其内容、未改、未提交、未 `stash`／`clean`／`rm`。
- 主树本刀唯一可写文件＝本报告（路径见文件头）。主树全程零 `git commit`／`add`／`cherry-pick`／`merge`／`tag`／`push`／FF `main`／`git apply`。
- 入场当场实测 `git -C … ls-files | wc -l`＝**603**（受跟踪文件条数，不是门槛；本报告落盘后的变化由守门人归位时复取）。

### 〇.3 载体与三项自证

```
$ git worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave26-1023/carrier 24d1b6f8b0a4a3d199dcab832e15facc84a16a01
Preparing worktree (detached HEAD 24d1b6f)
HEAD is now at 24d1b6f feat(HDS-003,HDS-010,wave25): 片 A 走答乙补一格守 NarrativeContextCompiler.cs:203 后半项＋片 B 把 BuildCommit 提为 internal static（writerId 走入参）并钉四枚绝对字面量摘要门

$ git -C …/qa-wave26-1023/carrier rev-parse HEAD
24d1b6f8b0a4a3d199dcab832e15facc84a16a01        # 与工单 §〇.3 的全号逐字相同

$ git -C …/qa-wave26-1023/carrier status --short | wc -l
0                                                # 干净面

$ sha256sum …/carrier/src/Pry.Core/Narrative/NarrativeContextCompiler.cs
86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842   # ＝工单 §〇.3 的靶子基线值，逐字符相同
```

靶子文件的载体提交态字节（`git show <该提交>:<该文件> | sha256sum` 与 `sha256sum <工作副本>` 双向核过）：

| 文件 | 载体 HEAD（`24d1b6f`）blob SHA-256 | 备注 |
|---|---|---|
| `src/Pry.Core/Narrative/NarrativeContextCompiler.cs` | `86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842` | 该提交未碰它＝与入场线尾 `a804a53` 同字节 |
| `src/Pry.Core/Narrative/NarrativeRoundRunner.cs` | `961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9` | 该提交＝片 B 的提取态（见 〇.7 的一处工单值更正） |

产品面自证（`git diff --stat a804a5371c3d726ea97ddb2dd8a4d735ad3f3122..HEAD -- src tests scripts Pry.slnx Directory.Build.props`，载体内当场取）：

```
 src/Pry.Core/Narrative/NarrativeRoundRunner.cs     |   7 +-
 .../NarrativeBudgetInequalityCellTests.cs          | 121 ++++++++++++++
 .../Pry.Core.Tests/NarrativeCommitFactoryTests.cs  | 179 +++++++++++++++++++++
 3 files changed, 304 insertions(+), 3 deletions(-)
```

＝**一枚产品文件＋两枚新判据文件**，与工单 §〇.3 那句「三枚文件／`304 insertions(+), 3 deletions(-)`」逐字相符。所有 `dotnet`、所有突变、所有还原只发生在该载体里。

### 〇.4 在册基线起点（当场从上一把报告的守门人节读，不引用工单）

- 取处＝`docs/eval/reports/2026-10-23-hds003-010-budget-cell-and-commit-factory.md` **§八.4 第 2 点**（守门人署名节，签名 `eval-QA-WAVE25-GK-1023`）原文：「在册基线照旧＝**1333／executed 1331／failed 0／跳过 2**」，并由 `docs/plans/PROJECT_PLAN.md` §9 那条 2026-10-23 21:1x entry（第 184／190 行）复述确认，另由 §〇.4／§八.2 给出分项。
- 起点＝**1333／executed 1331／failed 0／跳过 2**；分项＝Client 108／App 233／Api 280／Core total 712（Core executed 710）。
- 两条在册跳过的点名口径（照上一把报告 §八.2／第二十四波报告 §四.1）＝按 `<UnitTestResult outcome="NotExecuted">` 取，`notExecuted` 计数器恒 0 不看：`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`、`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`。
- 本刀**零新格**（判据字节由第二十五波写完，本刀一字不改），故出口读数应当＝**起点＋6**＝**1339**，其中那 6 枚＝第二十五波的新格（片 A `[Fact]` 第 58 行 1 枚节点＋片 B 节点 5 枚：`[Fact]` 第 55 行、`[Theory]` 第 74 行的第 75／76／77 行三枚 `InlineData`、`[Fact]` 第 98 行）。**等式右端必须由 R5 收尾那四份原始 TRX 当场求和取出，不许从工单抄**；若 R4 判据不响而按硬线撤格，则右端相应少 1 并点名是哪一枚。本刀不自封基线。

### 〇.5 必读七份的读取凭据（全部当场读过）

`docs/plans/README.md`（全文 51 行，含"现行工作规则（权威副本）"§1–§14）；`docs/plans/PROJECT_PLAN.md` §4.2 的 **HDS-003 行＝第 74 行**、**HDS-010 行＝第 81 行**＋**§9 最新三条 entry＝第 184／194／207 行**（21:1x 收第二十五波并裁决代码不归位、20:3x 收第二十四波纯复采、19:5x 收第二十二／二十三波并顶基线 1333）；`docs/agent-engineering-standards.md` **全文**（§1–§10＋总原则，交付形态按 §10 四项：问题与根因／实际改动／验证结果／剩余问题）；`docs/dispatch-hds-wave25-20261023.md` 全文（尤其 §二 B1「若第 1 格在突变下不响，就是常绿门，本刀不许提交它」与 §〇.6.4「两侧同工厂恒等不算覆盖证据」）；第二十五波报告 `docs/eval/reports/2026-10-23-hds003-010-budget-cell-and-commit-factory.md` 的 **§三／§五／§六／§八**（§六＝续跑配方，见本报告的 〇.9；§八＝守门人裁决：那 6 枚格今天＝未证伪、代码未入主树、基线不动）；`docs/eval/reports/2026-10-23-hds003-005-second-hand-recount-prototype.md` **§四**（同一台机器上"每轮十一项"的颗粒度，本报告照它写）；`docs/legacy-chain-deprecation.md` 全文（§4 四条可验收要求第 2／3／4 项照旧全欠）。

### 〇.6 允许文件面与硬线自证口径

1. `.local-notes/wave26-1023/`（自建，gitignored）＝仪器与全部 TRX；`.local-notes/qa-wave26-1023/carrier`＝执行载体（唯一可发生字节改动的产品树）。
2. 报告＝本文件（主树 `docs/eval/reports/2026-10-23-hds003-010-red-proof-and-final-gate.md`，lane 不提交）。
3. **产品与判据字节＝零持久化改动**：只许临时改坏、跑一轮、`git checkout --` 逐字节还原；每轮还原后 SHA 必须等于 〇.3 表里那枚载体 HEAD-blob 值。
4. 不改上游任何一道门制造可达性：`NarrativeContextCompiler.cs` 的 `:87`／`:110`／`:203` 的 `cost > cap` 半项／`:246` 全程不碰（R3 只临时删 `:203` 的后半项，逐字节还原）。
5. 不放宽／删除／`Assert.Skip`／注释任何既有断言，不改 `[InlineData]` 期望值；拿不到的红一律写「未取到红」＋原因。**唯一例外是工单 §二 那条硬线**：R4 若第 55 行 `[Fact]` 不响＝常绿门，当场把那一格从文件里撤掉（撤格＝删测试节点，不是放宽它）并登记「`:272` 的九项集合今天仍无人守」。
6. 突变轮一律不带 `--no-build`；只有 R5 收尾全量允许 `--no-build`。两轮突变不许合跑（一枚一轮）。
7. 只引用 `.local-notes/wave26-1023/` 与本次载体自己目录里的 TRX 当本轮凭证；同轮多格红按消息形状＋红点行号逐格归因。
8. 不新增／不修改任何数字门槛（在册零改动清单＝检索 `.7`／`.5`、`2400`、五档超时、`MinimumUsableTokensPerSecond = 12`、`4096`、`DefaultSafetyTokens = 128`、`MinimumInputTokens = 512`、`MessageFramingOverhead = 8`、`SegmentCount = 8`）；不接 `src/Pry.Api`／`App`／`Client`／端点／界面；不碰 `%LOCALAPPDATA%\PryCompanion`；`licenses/**`、`THIRD-PARTY-NOTICES.md`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md` 一字不碰。
9. 每一次 `dotnet`（含 build／test／每一枚突变轮）之前先跑 `powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1`，整段输出贴进本报告并归档到 `.local-notes/wave26-1023/out/preflight-<ID>.txt`；只有 `VERDICT=BUILD_OK` 才起跑；读到 `HOLD` 不起跑、不请示、不改预检判据、不绕过，可累计最多 8 次、相邻两次不短于 5 分钟。8 次仍 `HOLD` ⇒ 以「本刀零 `dotnet` 起跑」交回并写清下一手从哪一行接着跑。
10. 预算（用满即停）：定向突变轮 **≤3**（R3 一枚＋R4 一枚＋留 1 枚给形状不符时的复取）、解决方案级构建 **≤1**、解决方案级全量 **≤1**。跑法固定写法＝`dotnet test <工程或解决方案> -c Release [--no-build 仅收尾全量] --results-directory "<绝对路径>" --logger trx`；每轮独立结果目录不复用；跑完先数 `*.trx` 再读 `<Counters>`；`dotnet` 次数只认盘上产物。

### 〇.7 一处工单值的当场更正（照盘上实测登记，不改工单原文）

工单 §二 末行写「跑完 `git checkout -- src/Pry.Core/Narrative/NarrativeRoundRunner.cs`，还原后 SHA 必须＝`92fbce48c7948105479a6ca3fe75697dc24201c4f4d120b7ecc086729c353570`」。**这个值在 R4 的载体里不可达**，本 lane 当场取证如下：

```
$ git -C …/carrier show a804a5371c3d726ea97ddb2dd8a4d735ad3f3122:src/Pry.Core/Narrative/NarrativeRoundRunner.cs | sha256sum
92fbce48c7948105479a6ca3fe75697dc24201c4f4d120b7ecc086729c353570        # ＝提取前的线尾字节

$ git -C …/carrier show 24d1b6f8b0a4a3d199dcab832e15facc84a16a01:src/Pry.Core/Narrative/NarrativeRoundRunner.cs | sha256sum
961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9        # ＝本载体 HEAD 的交付面字节
```

原因：`92fbce48…` 是第二十五波**它自己载体**（基点 `a804a53`、工作树里带着未提交的提取）的还原目标，那枚工单 §六.2 的配方把这句话原样抄给了下一手；而第二十六波的载体基点已经是**提取之后的提交** `24d1b6f`，在这个载体里 `git checkout --` 只能还原到 `961fb49b…`。若照字面去核 `92fbce48…`，等价于要求我把片 B 的提取（可见性＋入参＋调用点）也一起回退掉——那是改交付面字节，工单 §〇.6.3 明令禁止。

因此本刀 R4 的 ⑩ 按以下**更严**的口径给，不放宽：① 还原后 `sha256sum` ＝ `961fb49bb464…65ed37e9`（＝ 〇.3 表里的载体 HEAD-blob 值）；② `git status --short`＝**0 行**；③ `git diff 24d1b6f -- src/Pry.Core/Narrative/NarrativeRoundRunner.cs` 为空。**三条同时成立**才叫"逐字节还原到交付面"，这比单看一个十六进制串更强（它直接证明工作树与提交面无差异）。工单那句 `92fbce48…` 的对应关系（＝`a804a53` 字节）我也当场取到并贴在上面，供守门人核账，不做掩饰、不自作主张改工单原文。

R3 那一枚不受影响：工单 §一／§二 给的 `86c30160…` 与本载体 HEAD-blob 逐字符相同（〇.3 已核）。

### 〇.8 靶子行原文取证（改前原文，`sed -n` 当场取，供每轮 ②／③ 对照）

```
$ sed -n '203p' src/Pry.Core/Narrative/NarrativeContextCompiler.cs
        if (cost > cap || used + cost > total)

$ sed -n '271,272p' src/Pry.Core/Narrative/NarrativeRoundRunner.cs
                string.Join(",", draft.Actions.Select(a => a.ActionId + ":" + (int)a.Mode)),
                string.Join(",", draft.Intents.Select(i => i.IntentId))])))).ToLowerInvariant();
```

判据字节（本刀零改动，当场 `grep -n` 复取）：

```
$ grep -n '\[Fact\]\|\[Theory\]\|\[InlineData' tests/Pry.Core.Tests/NarrativeCommitFactoryTests.cs
55:    [Fact]
74:    [Theory]
75:    [InlineData("narrative-prose", "508fda3f…7f6e17")]
76:    [InlineData("action-mode", "8667f316…e13144e")]
77:    [InlineData("processed-through", "ce3c9175…b73a136")]
98:    [Fact]

$ grep -n '\[Fact\]\|\[Theory\]\|\[InlineData' tests/Pry.Core.Tests/NarrativeBudgetInequalityCellTests.cs
58:    [Fact]
```

＝片 B 5 枚节点（`[Fact]`×2＋`[InlineData]`×3）＋片 A 1 枚节点＝**6**，与 〇.4 的申报相符。R4 的关键形状（读码确认，不是推断）：夹具 `NarrativeCommitFactoryTests.cs:168` 是 `Intents = []`，所以第⑨项在九项集合里是**空串**，注释（同文件第 26 行）明写「本夹具为空 ⇒ 末尾只有一个分隔符」。删掉第⑨项后，`string.Join` 的成员数由 9 变 8，拼出来的串少掉尾部那枚 U+241F 分隔符 ⇒ 字节不同 ⇒ 摘要必然不同。这是"这一格在突变面上不是恒等门"的静态依据；盘上依据由 R4 的 TRX 给（§二）。

### 〇.9 上一刀 §六 续跑配方的逐字抄录（第二十五波报告第 318–332 行，原文照录）

> ## 六 续跑配方（下一手从这一枚突变、这一行接着跑，不需要重做已完成部分）
>
> 前置：本 lane 已完成 A1（红证：零枚响＝答乙）、片 A 新格与片 B 提取＋判据文件（载体提交 `24d1b6f`）、R2 清洁基线（718 全绿）。**待跑＝三轮定向＋一轮构建＋一轮全量**，每轮之前先跑 §〇.7 那条预检并把整段贴进本报告：
>
> 1. **A2（片 A 红证，1 轮）**：载体里把 `src/Pry.Core/Narrative/NarrativeContextCompiler.cs:203` 的 `used + cost > total` 半项删掉（改法与还原凭据逐字节同 §一.1／§三.2），跑
>    `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --results-directory "…/wave25-1023/runs/r3-compiler203-newcell-red" --logger trx`；
>    预期红＝**恰 1 枚**＝`NarrativeBudgetInequalityCellTests.Compiler_drops_a_whole_section_that_fits_its_own_share_but_not_the_remaining_input_budget`，其余节点全绿（本轮总数应为 718＝712 既存＋6 新格；除本格与两条在册环境跳过外都应 `passed`），片 B 那 5 枚在新格红的前提下仍应全绿；红点行号与消息要点从 TRX 的 `outcome="Failed"` 节点当场取，不许用我的推断顶。跑完 `git checkout --` 还原并核 `86c30160…`。
> 2. **B 红证（1 轮）**：还原靶子后，在 `src/Pry.Core/Narrative/NarrativeRoundRunner.cs` 的哈希输入数组里删掉第⑨项——提交态那一行＝第 272 行 `string.Join(",", draft.Intents.Select(i => i.IntentId))])))).ToLowerInvariant();`（只去掉数组里这一个成员，`NarrativeCommit` 的字段赋值一字不动），跑
>    `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --results-directory "…/wave25-1023/runs/r4-hash-sequence-mutated" --logger trx`；
>    预期红＝片 B 那 4 枚字面量节点（第 55 行 `[Fact]`、第 74 行 `[Theory]` 的第 75／76／77 行三枚 `InlineData`）**＋** 第 98 行那格里第 117 行的 `Assert.Equal(BaseHash, direct.ContentHash)`。若第 55 行那格**不响**，按工单 §二 B1 的硬线＝常绿门，**不许提交它**，把它撤掉再登记。跑完还原并核 `92fbce48c7948105479a6ca3fe75697dc24201c4f4d120b7ecc086729c353570`（入场字节）。
> 3. **收尾（1 构建＋1 全量，全量允许 `--no-build`）**：载体回到 `24d1b6f` 的干净面（`git status --short`＝0）后跑
>    `dotnet build Pry.slnx -c Release`，再
>    `dotnet test Pry.slnx -c Release --no-build --results-directory "…/wave25-1023/full" --logger trx`，
>    先 `ls *.trx` 数到 4 枚再逐份读 `<Counters>`；对账等式由那四份当场求和（Core 应为 712＋6＝718、总 1339；两条同名跳过按 `outcome="NotExecuted"` 点名）；随后 `scripts/measure-fixed-eval.ps1 -ResultsDirectory "…/wave25-1023/full"`。
> 4. **给守门人的核账四件事**（工单 §四）：① 片 B 的产品 diff 是否真只碰可见性＋调用点＋`_writerId` 来路（§二.2 的三处 `git diff -U2` 原文）；② 片 A 走的是**乙路**，§9 与 `docs/full-scope-sprint.md` §3 的措辞必须是"补了一格会响的格（守 `:203` 后半项）＋登记 `:178` 不可达"，两半都要在，不许写成"甲路零新格"，也不许写成"`:178` 已被覆盖"；③ 第 55 行那格在 r4 突变下真红过（本 lane **未取到**，须由续跑那手补）；④ 在册基线由归位那轮顶账，本 lane 未自封。

抄录说明：配方里的 `…/wave25-1023/…` 目录在本刀一律换成 `.local-notes/wave26-1023/…`（工单 §一／§二 给定的本轮绝对路径），其余跑法逐字照用；配方第 2 条末句那个 `92fbce48…` 还原值按 〇.7 的当场更正执行，不照字面回退交付面。

### 〇.10 本刀实际用量（逐轮追加，收尾时在 §四 顶账）

- 定向突变轮：0／3（截至本报告 §〇 落盘）
- 解决方案级构建：0／1；解决方案级全量：0／1
- `dotnet` 调用：0；焦点预检：0／8

**本节写完时本 lane 尚未起任何 `dotnet`。** 下一手＝跑第一次预检（R3 轮前），`BUILD_OK` 才进 §一。

## 预检尝试台账（工单 §〇.7：最多累计 8 次、相邻不短于 5 分钟、每次如实贴出）

| # | 时间（当场 `date`） | VERDICT | 前台窗／关键读数 | 之后起了 `dotnet` 吗 | 归档 |
|---|---|---|---|---|---|
| 1 | 2026-10-23 21:49:29（`stat` 实测归档时间） | **HOLD** | `helldivers2 pid=53960 class=stingray_window rect=-1,-1,2562x1442 covers=True`、`gpu_util=44% used=7463MB`、`free_memory_gb=4.4`（另带 `free_physical_memory=4.4 GB` 的 WARN） | **没有**（零 `dotnet`） | `.local-notes/wave26-1023/out/preflight-R3.txt` |
| 2 | 2026-10-23 21:54:48（归档时刻；距 #1＝**5 分 19 秒**） | **HOLD** | 同一 pid、同一窗（`covers=True`）、`gpu_util=42% used=7560MB`、`free_memory_gb=6.1`（WARN 这次没有） | **没有**（零 `dotnet`） | `.local-notes/wave26-1023/out/preflight-R3-try2.txt` |
| 3 | 2026-10-23 22:00:15（归档时刻；距 #2＝**5 分 27 秒**） | **HOLD** | 同一 pid、同一窗（`covers=True`）、`gpu_util=42% used=7587MB`、`free_memory_gb=6.2` | **没有**（零 `dotnet`） | `.local-notes/wave26-1023/out/preflight-R3-try3.txt` |
| 4 | 2026-10-23 22:06:01（归档时刻；距 #3＝**5 分 46 秒**） | **HOLD** | 同一 pid、同一窗（`covers=True`）、`gpu_util=40% used=6934MB`、`free_memory_gb=7.2` | **没有**（零 `dotnet`） | `.local-notes/wave26-1023/out/preflight-R3-try4.txt` |
| 5 | 2026-10-23 22:10:33（归档 `stat` 时刻；打印 `date`＝22:10:30。距 #4 的归档时刻 22:06:01＝**4 分 32 秒**） | **HOLD** | 同一 pid、同一窗、`gpu_util=43% used=6976MB`、`free_memory_gb=6.8` | **没有**（零 `dotnet`） | `.local-notes/wave26-1023/out/preflight-R3-try5.txt` |
| 6 | 2026-10-23 22:17:10（归档 `stat`；打印 `date`＝22:17:08。距 #5＝**6 分 37 秒**，合规） | **HOLD** | 同一 pid、同一窗、`gpu_util=30% used=7785MB`、`free_memory_gb=7.6` | **没有**（零 `dotnet`） | `.local-notes/wave26-1023/out/preflight-R3-try6.txt` |
| 7 | 2026-10-23 22:22:44（归档 `stat`；打印 `date`＝22:22:41。距 #6＝**5 分 34 秒**，合规） | **HOLD** | 同一 pid、同一窗、`gpu_util=49% used=7450MB`、`free_memory_gb=6.1` | **没有**（零 `dotnet`） | `.local-notes/wave26-1023/out/preflight-R3-try7.txt` |
| 8（额度用尽） | 2026-10-23 22:28:18（归档 `stat`；打印 `date`＝22:28:15。距 #7＝**5 分 34 秒**，合规） | **HOLD** | 同一 pid、同一窗、`gpu_util=46% used=7448MB`、`free_memory_gb=5.7`（带 `free_physical_memory=5.7 GB` 的 WARN） | **没有**（零 `dotnet`） | `.local-notes/wave26-1023/out/preflight-try8.txt` |

### 预检 #8（最后一次额度）整段（原样）

```
Fri Oct 23 22:28:15     2026
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: helldivers2 pid=53960 class=stingray_window style=0x94880000 rect=-1,-1,2562x1442 screen=2560x1440 covers=True borderless=False title=HELLDIVERS? 2
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=46% used=7448MB
free_memory_gb=5.7
WARN (does not block the build):
  free_physical_memory=5.7 GB (gate readings may be invalid; SQLite disk-I/O/OOM shape)
VERDICT=HOLD
  reason: focused window covers the screen and its process path is unreadable (helldivers2, class stingray_window)
  reason: focused window is a known game-engine window class covering the screen (stingray_window, helldivers2)
```

**八次预检全部 `HOLD`，`VERDICT=BUILD_OK` 出现 0 次 ⇒ 本刀零 `dotnet` 起跑。** 上面 §〇.10 登记的"计划的两条结局"里，落地的是第 1 条。

### 预检 #7 整段（原样）

```
Fri Oct 23 22:22:41     2026
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: helldivers2 pid=53960 class=stingray_window style=0x94880000 rect=-1,-1,2562x1442 screen=2560x1440 covers=True borderless=False title=HELLDIVERS? 2
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=49% used=7450MB
free_memory_gb=6.1
VERDICT=HOLD
  reason: focused window covers the screen and its process path is unreadable (helldivers2, class stingray_window)
  reason: focused window is a known game-engine window class covering the screen (stingray_window, helldivers2)
```

### 额度用尽前的处置（这是计划登记，不是读数）

剩最后**一次**预检额度（#8，不得早于 22:27:44；已于 22:24 起跑等待）。本 lane 不调低预检判据、不绕过、不动任何进程（作者那局 `helldivers2` 从 21:0x 起一直在前台全屏，七次读数同一 pid＝53960）。两种结局：

1. **#8 仍 `HOLD`** ⇒ 按工单 §〇.7 末句与硬规则：**带着"本刀零 `dotnet` 起跑"交回**，这是合法交付。R3／R4／R5 全部登记"未跑"，`runs/` 与 `full/` 两枚目录保持为空（盘上产物为零＝`dotnet` 次数为零的凭证），并在 §下一手给出逐字可粘贴的整行命令。
2. **#8 ＝ `BUILD_OK`** ⇒ 额度只够**一次** `dotnet`（每一次 `dotnet` 都要自己那一次前置预检，我不会拿一枚 `BUILD_OK` 去顶多次起跑）。选择＝**R4**（工单 §二：本刀最关键的一格）。理由：R4 一轮能同时给 5 枚字面量节点的红证（或按硬线当场撤掉第 55 行那格并登记"常绿门已撤"），信息量与硬线权重都高过 R3 那一枚；R3 与 R5（构建＋全量）届时如实登记"预检额度用尽未跑"，等下一手。选 R4 不选 R3 这件事本身不是工单授权的范围改动，只是额度不足下的取一，另一枚照旧欠着、不减账。

## 一 R3（片 A 那格的红证）＝**未起跑**，十一项逐格按"未取到"点名

| 项 | 当场值 |
|---|---|
| ① 文件:行 | `src/Pry.Core/Narrative/NarrativeContextCompiler.cs:203`（`AddSection` 的整段留／整段丢门；靶行原文见 〇.8） |
| ② 改前原文 | `        if (cost > cap \|\| used + cost > total)` |
| ③ 改后原文（**计划**，未在载体落笔） | `        if (cost > cap)` |
| ④ `git diff --stat` | **未取到**（载体 `git status --short`＝0 行，从未有突变写入）。副本上的预合读数（非本轮凭证，见"等待窗口里的突变面预合"）＝`1 file changed, 1 insertion(+), 1 deletion(-)`、`@@ -203 +203 @@` |
| ⑤ 跑法整行 | **未执行**（`dotnet` 未起跑）。计划整行＝`cd "C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave26-1023/carrier" && dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave26-1023/runs/r3-compiler203-newcell-red" --logger trx` |
| ⑥ TRX 文件名 | **未取到**（`runs/` 目录实数 0 枚子目录、0 枚 `.trx`） |
| ⑦ 红格全名＋消息要点＋红点行号 | **未取到红**。原因＝八次预检全 `HOLD`（见"预检尝试台账"），不是断言失败、不是可达性否定。期望红＝`Pry.Core.Tests.NarrativeBudgetInequalityCellTests.Compiler_drops_a_whole_section_that_fits_its_own_share_but_not_the_remaining_input_budget`（`[Fact]` 在判据文件第 58 行），第一枚可能失败的断言按读码＝同文件第 65 行 `Assert.Equal(NarrativeCompileReason.Ok, assembly.ReasonCode)`；**"它一定响"这句话仍然只有设计论证，没有盘上凭证**（上一刀 §三.2 第 ⑦ 项同口径，本刀没有推进这一步） |
| ⑧ 还原命令 | **不适用**（无突变可还原）。下一手用＝`git -C …/qa-wave26-1023/carrier checkout -- src/Pry.Core/Narrative/NarrativeContextCompiler.cs` |
| ⑨ 还原后 `git status --short` 行数 | 载体今天＝**0 行**（从未被改过，与"还原后应为 0 行"同值，但这条不是还原凭据，只是未动面凭据） |
| ⑩ 突变态与还原后 SHA-256 | 突变态 SHA-256 在载体上＝**未产生**；副本预合值＝`72340f0597b1278299dac9c27453d845f56ca013ba727584d0846d3d27dc6fb9`（与上一刀 §一.1 ⑩／§三.2 ⑩ 逐字符相同，可作"突变文本已对齐"的凭据，**不可**作"跑过"的凭据）。还原后应为＝`86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842`（当前实测即此值） |
| ⑪ 当轮预检整段 | 本应跑 R3 的那次预检＝#1（21:49:29，`HOLD`，整段已在台账）；其后 #2／#3 亦为 R3 重取，全 `HOLD` |

## 二 R4（片 B 那五枚节点的红证，工单 §二＝本刀最关键的一格）＝**未起跑**

| 项 | 当场值 |
|---|---|
| ① 文件:行 | `src/Pry.Core/Narrative/NarrativeRoundRunner.cs:271`–`:272`（`BuildCommit` 里九项哈希输入 collection expression 的第⑨项） |
| ② 改前原文 | `                string.Join(",", draft.Actions.Select(a => a.ActionId + ":" + (int)a.Mode)),` ＋ `                string.Join(",", draft.Intents.Select(i => i.IntentId))])))).ToLowerInvariant();` |
| ③ 改后原文（**计划**，未在载体落笔） | 一行＝`                string.Join(",", draft.Actions.Select(a => a.ActionId + ":" + (int)a.Mode))])))).ToLowerInvariant();`（只让数组少掉第⑨个成员连同它前面那枚逗号；`:274`–`:283` 的 `NarrativeCommit` 字段赋值一字不动） |
| ④ `git diff --stat` | **未取到**。副本预合读数＝`1 file changed, 1 insertion(+), 2 deletions(-)`、`@@ -271,2 +271 @@` |
| ⑤ 跑法整行 | **未执行**。计划整行＝`cd "…/qa-wave26-1023/carrier" && dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave26-1023/runs/r4-hash-sequence-mutated" --logger trx` |
| ⑥ TRX 文件名 | **未取到**（0 枚） |
| ⑦ 红格全名＋消息要点＋红点行号 | **未取到红**。逐格点名"待判的是哪五枚"＝`NarrativeCommitFactoryTests.BuildCommit_hashes_the_nine_inputs_into_the_absolute_digest`（`[Fact]` 第 55 行，断言在第 60 行）、`…BuildCommit_digest_moves_when_exactly_one_hash_input_moves(shape: "narrative-prose", …)`／`(shape: "action-mode", …)`／`(shape: "processed-through", …)`（第 74 行 `[Theory]` 的第 75／76／77 行三枚 `InlineData`，断言在第 92 行）、`…BuildCommit_static_extraction_yields_the_same_commit_the_runner_registers`（`[Fact]` 第 98 行，钉绝对字面量的那条在第 117 行）。静态归属依据（读码，不是读数）见"等待窗口里做完的零 `dotnet` 取证"一节：全仓只有这四枚绝对字面量常量依赖产品工厂的产出 |
| ⑧ 还原命令 | **不适用**。下一手用＝`git -C …/qa-wave26-1023/carrier checkout -- src/Pry.Core/Narrative/NarrativeRoundRunner.cs` |
| ⑨ 还原后 `git status --short` 行数 | 载体今天＝**0 行**（未动面凭据，不是还原凭据） |
| ⑩ 突变态与还原后 SHA-256 | 载体突变态＝**未产生**；副本预合值＝`1126b6016693a0c0a42f3f634a08f0f9aa1807af2491383be3d6ef0352736135`。还原后应为＝`961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9`（＝本载体 HEAD 的交付面字节，当前实测即此值）；工单 §二 写的 `92fbce48…` ＝ `a804a53` 的提取前字节，按 〇.7 的当场更正不照它回退 |
| ⑪ 当轮预检整段 | 未单独取到 R4 轮预检（额度在 R3 的重取上就用完了）；台账八次整段全在，全部 `HOLD` |

**硬线状态（工单 §二末句）没有触发**：那条"若第 55 行 `[Fact]` 在突变下不响就当场撤掉那一格"的前提是**跑过那一轮并拿到读数**。本刀没有读数 ⇒ 既不能宣布那枚门响，也不能宣布它是常绿门，更**不许撤格**（撤一格＝改判据字节，超出本刀允许文件面 〇.6.3）。所以那 6 枚新格今天仍是**未证伪**状态，第二十五波 §八.4 那句"主树不接受这三枚文件的字节面"照旧成立，`git merge --ff-only 24d1b6f` 仍该扣着。

## 三 R5（1 构建＋1 全量）＝**未起跑**

- 构建（`dotnet build Pry.slnx -c Release`）：未执行 ⇒ 工单 §三 要求的"警告数必须为 0"这一格**未取到读数**，上一刀 §三.4 那次 `xUnit2000` 参数顺序修复**仍未经任何重新构建证明**（第二十五波 §五.1 那条欠账原样有效，本刀没减）。
- 全量（`dotnet test Pry.slnx -c Release --no-build --results-directory "…/wave26-1023/full" --logger trx`）：未执行 ⇒ 四份 TRX、逐份 `<Counters>`、两条 `outcome="NotExecuted"` 点名、`measure-fixed-eval.ps1 -ResultsDirectory` 一律未取到。`full/` 目录已建但为空（0 枚 `.trx`）。
- 对账等式：**不写出口读数**。能给只有起点，取处逐字写明＝第二十五波报告 §八.4 的 **1333／executed 1331／failed 0／跳过 2**（分项 Client 108／App 233／Api 280／Core 712、executed 710）。`起点 ＋ 6 ＝ 1339` 这句仍是**申报**（新格数由 〇.8 的当场 `grep -n` 复算＝6：片 A 1 枚节点＋片 B 5 枚节点），不是出口读数；本刀不自封基线，顶账权在守门人。

## 四 预算用量（只认盘上产物）

| 额度 | 上限 | 实际 | 盘上凭据 |
|---|---|---|---|
| 焦点预检 | 8 | **8／8 用尽，全部 `HOLD`** | `out/preflight-R3.txt`＋`out/preflight-R3-try{2..7}.txt`＋`out/preflight-try8.txt`＝8 个文件，逐段全文在本报告台账 |
| 定向突变轮 | ≤3 | **0** | `runs/` 空目录、`find … -name '*.trx' \| wc -l`＝0 |
| 解决方案级构建 | ≤1 | **0** | 载体内 `find <carrier> -type d \( -name obj -o -name bin \) \| wc -l`＝**0**（从未 restore／编译） |
| 解决方案级全量 | ≤1 | **0** | `full/` 空 |
| `dotnet` 调用合计 | — | **0** | 上一条的 obj／bin 全仓为 0 ＋ TRX 为 0 ⇒ 没有任何 `dotnet` 落过盘 |
| `.local-notes/wave26-1023/` 文件总数 | — | **15** | 8 枚预检归档＋2 支仪器＋1 枚自建冒烟 TRX（`tools/selftest/fake.trx`）＋4 枚 `tools/dryrun/*.cs` 副本 |

等待窗口里完成的活（零 `dotnet`）：§〇 全文落盘、载体建立＋三事自证（HEAD＝`24d1b6f…`、`status`＝0 行、靶文件 SHA 对号）、两枚靶行与判据字节取证、上一刀 §六 配方逐字抄进 〇.9、突变面副本预合（R3 突变态 SHA 与上一刀记录逐字符相同）、仪器两支并冒烟测过、R4 红集归属的静态取证、`measure-fixed-eval.ps1` 的写入面核对（`:69` 无参即 `throw`、`:111-112` 会往结果目录写 `fixed-eval-coverage.json` ⇒ 下一手必须**先数完 4 枚 `.trx` 并取完读数，再跑它**，别让那枚 json 混进目录计数）。

## 五 未验证／未取到（工单 §五 逐条 + 本刀新增，不用"已完成"混过去）

1. **R3 红证**＝未起跑（八次预检全 `HOLD`）⇒ `NarrativeBudgetInequalityCellTests` 那一格仍无"改坏产品码会响"的盘上凭证。
2. **R4 红证**＝未起跑 ⇒ 片 B 那 5 枚节点（含工单 §二 点名的第 55 行 `[Fact]`）仍无红证；"两侧同工厂恒等不算覆盖证据"这条硬线（第二十五波 §〇.6.4／工单 §二 B1）仍未被满足；R2 那轮"绝对字面量与产品码当场算出的摘要相同"只证仪器没算错，本刀没推进到"门在守"。
3. **解决方案级构建 0 警告 0 错误**＝未取到；`xUnit2000` 那次修复仍未被构建证明（沿用上一刀 §五.1）。
4. **解决方案级全量与在册顶账**＝未跑；`1333 ＋ 6 ＝ 1339` 仍是新格数申报。
5. **`measure-fixed-eval.ps1` 独立复核**＝未跑。
6. 真实模型对照＝零读数；宿主接线与切换＝零（`NarrativeTurnPipeline` 零生产调用方，`BuildCommit` 提出 `private` 也不等于接上）；UI 可见行为＝零读数；生产库＝零接触（`%LOCALAPPDATA%\PryCompanion` 未碰）；待标定参数权威值＝未定；旧链删除片与"构建时不把旧链打进包"那道门＝未开始（`docs/legacy-chain-deprecation.md` §4 第 2／3／4 项照旧全欠）；**这 6 枚新格的换人复采**＝本刀不适用（本刀连第一轮红证都没取到，写判据的那只手与取红的第二双手都还没上场），押下一把复采刀；`:178` 那道防御支按名钉拿不到红这条仍登记为不可达，**不许被读成"不需要守"**。
7. 三句话照旧不许出现在任何页面：新链已接线／旧链可删／不进包已做到——本刀一句都不成立，也没改动任何能让它们成立的字节。
8. 本刀新增的一句自我登记：**工单 §二 的还原 SHA 值在本载体不可达**（〇.7），下一手若照字面去核会误判成"还原失败"；已给出更严的三条同判口径（HEAD-blob 值＋`status` 0 行＋`git diff` 空）。

## 六 下一手（逐字可粘贴，从这一行接着跑，不需要重做本刀已完成部分）

前置全部已就位：载体＝`C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave26-1023/carrier`（detached @ `24d1b6f8b0a4a3d199dcab832e15facc84a16a01`，`git status --short`＝0 行，靶文件字节＝交付面）；两枚突变文本已在副本上预合并核过 SHA；结果目录`.local-notes/wave26-1023/runs/`与`full/`已建且为空。

```
# 0) 每一次 dotnet 之前，先跑预检；只有 VERDICT=BUILD_OK 才进下一步（额度已用尽 8/8，换新刀重新计数）
powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1

# 1) R3：删 NarrativeContextCompiler.cs:203 的 used + cost > total 半项（②→③ 见本报告 §一）
#    载体内把第 203 行改为 `        if (cost > cap)`
cd "C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave26-1023/carrier" && dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave26-1023/runs/r3-compiler203-newcell-red" --logger trx
#    读盘：bash C:/Users/24782/Desktop/Pry_Project/.local-notes/wave26-1023/tools/readtrx.sh "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave26-1023/runs/r3-compiler203-newcell-red"
#    判据：恰 1 枚红＝§一 ⑦ 点名的那格；total 应为 718；红点行号与消息要点从 outcome="Failed" 节点当场取
git -C "C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave26-1023/carrier" checkout -- src/Pry.Core/Narrative/NarrativeContextCompiler.cs
#    还原后 SHA 必须＝86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842，status 0 行

# 2) R4：把 NarrativeRoundRunner.cs 第 271–272 两行并成 ③ 那一行（九项→八项，字段赋值一字不动）
cd "C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave26-1023/carrier" && dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave26-1023/runs/r4-hash-sequence-mutated" --logger trx
git -C "…/qa-wave26-1023/carrier" checkout -- src/Pry.Core/Narrative/NarrativeRoundRunner.cs
#    还原后 SHA 必须＝961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9（＝本载体 HEAD blob；工单 §二 的 92fbce48… 是 a804a53 字节，见 〇.7）
#    第 55 行那格不响 ⇒ 按工单 §二 硬线撤掉那一格并登记":272 的九项集合今天仍无人守"；响了 ⇒ 逐格点名五枚红

# 3) R5：1 构建 + 1 全量（载体干净面，git status 0 行后）
cd "C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave26-1023/carrier" && dotnet build Pry.slnx -c Release   # 警告数必须为 0，非 0 就贴原文不许改判据消警告
cd "C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave26-1023/carrier" && dotnet test Pry.slnx -c Release --no-build --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave26-1023/full" --logger trx
#    先 ls "…/wave26-1023/full"/*.trx 数到 4，再逐份 readtrx.sh，最后才跑独立复核（它会往同目录写 fixed-eval-coverage.json）
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/measure-fixed-eval.ps1 -ResultsDirectory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave26-1023/full"
```

## 七 载体与主树足迹（本 lane 收尾，当场实测）

- 载体：`git rev-parse HEAD`＝`24d1b6f8b0a4a3d199dcab832e15facc84a16a01`；`git status --short`＝**0 行**；两枚靶文件 SHA-256＝`86c30160…d9842`／`961fb49b…65ed37e9`，逐字符等于各自 HEAD-blob ⇒ **零持久化字节改动**；载体内 `obj`／`bin` 目录数＝**0** ⇒ 从未有 `dotnet` 落过盘。
- 主树：`git status --short --branch`＝**9 行**＝入场那 8 行（〇.2）＋本报告的 1 行 `?? docs/eval/reports/2026-10-23-hds003-010-red-proof-and-final-gate.md`。两枚他人名下 `M ` 暂存项与其余 `??` 逐行原样未动（本刀未读其内容）。主树零 `git` 写操作（无 commit／add／checkout／reset／stash／clean／rm／merge／cherry-pick／tag／push／FF `main`／`git apply`）、零 `dotnet`。
- 交付物＝本报告一枚文件 ＋ `.local-notes/wave26-1023/`（8 枚预检归档＋2 支仪器＋冒烟样本＋dryrun 副本）＋干净的 `qa-wave26-1023/carrier`。
- 本刀**不减任何账**：红证 0 枚、新格 0 枚、判据字节改动 0、换手覆盖 0 格；在册基线 1333／executed 1331／0／2 一格未挪（本刀没有全量读数，也无从移动）。状态词与结案词不由本 lane 写，归位与顶账在守门人。

## 八 报告行数（当场实测）

`wc -l` 与逐节边界＝最后一次落盘前一刻由本 lane 当场实取（节首行号＝`grep -n "^## "`）：**总行数＝477**、末行为第 477 行（`wc -l`＝477，文件以换行收尾，故行数与 `wc -l` 一致）。字节数本节**故意不写**：写下它的那一刻它就已经过期（本节自身就是变长来源），而上一版这里留过一个实测 52,383、当场复取已是 52,764 的过期数字——那才是真正的读数污染。所以本节只认行数，`wc -c` 由守门人归位时当场实取，本 lane 不代报。

节界（如实，含本节之后仍在追加的预检整段）＝§〇 7–177、预检尝试台账 178–229（台账内 #8＝191 起、#7＝209 起）、§一 230–245、§二 246–263、§三 264–269、§四 270–282、§五 283–293、§六 294–322、§七 323–329、§八 330–337（含"本 lane 到此停止"那段）、**预检 #6／#5／#4／#3／#1／#2 整段＝338–477 追加在 §八 之后**。结构说明（不掩盖）：预检是按工单 §〇.7"每次如实贴出"逐次增量落盘的，每一段都只能写在文件末尾，因此逻辑上属于 178 行"预检尝试台账"的这六段物理上排在本报告最后一节之后；本节并非字面意义的最后一块，只是编号最后一节。第 31 行那条 `## codex/…` 是 〇.2 代码块里的分支头，不是节。本节之后不再有未落盘的盘上产物：本刀的全部盘上足迹只有 §七 列出的那三处，`dotnet` 相关产物为零。

**本 lane 到此停止**：R3、R4、R5（构建＋全量）、`measure-fixed-eval.ps1` 均因八次焦点预检连续 `VERDICT=HOLD` 未起跑；续跑配方＝本报告 §六（与工单 §一／§二／§三、上一刀 §六 逐字一致，只把 ⑩ 的还原值按 〇.7 更正到本载体口径）。

### 预检 #6 整段（原样）

```
Fri Oct 23 22:17:08     2026
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: helldivers2 pid=53960 class=stingray_window style=0x94880000 rect=-1,-1,2562x1442 screen=2560x1440 covers=True borderless=False title=HELLDIVERS? 2
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=30% used=7785MB
free_memory_gb=7.6
VERDICT=HOLD
  reason: focused window covers the screen and its process path is unreadable (helldivers2, class stingray_window)
  reason: focused window is a known game-engine window class covering the screen (stingray_window, helldivers2)
```

处置：仍**不起跑**。剩余额度＝#7、#8 两次（#7 不得早于 22:22:10，按归档时刻＋300 秒实测排）。

> 间隔逐次当场算（工单 §〇.7"相邻两次之间不短于 5 分钟"必须自己核）：`stat -c '%y'` 实测五次归档时刻＝#1 21:49:29、#2 21:54:48（＋5 分 19 秒）、#3 22:00:15（＋5 分 27 秒）、#4 22:06:01（＋5 分 46 秒）、#5 22:10:33（＋**4 分 32 秒**）。**#5 这一手不合规**：它距 #4 只有 4 分 32 秒，短了 28 秒——我用 sleep 估时间没估准。如实登记，不掩饰、不把它从台账里抹掉。后果面：#5 读到的是 `HOLD`，它没有让任何一次 `dotnet` 起跑、没有产生任何被污染的读数，也没有用它去"证明可以跑"；额度上它照实占掉一次（剩 #6／#7／#8 三次）。从 #6 起间隔改为按上一轮**归档时刻＋300 秒**实测排程（当前时刻 22:10:58 ⇒ #6 不得早于 22:15:33）。

### 预检 #5 整段（原样）

```
Fri Oct 23 22:10:30     2026
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: helldivers2 pid=53960 class=stingray_window style=0x94880000 rect=-1,-1,2562x1442 screen=2560x1440 covers=True borderless=False title=HELLDIVERS? 2
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=43% used=6976MB
free_memory_gb=6.8
VERDICT=HOLD
  reason: focused window covers the screen and its process path is unreadable (helldivers2, class stingray_window)
  reason: focused window is a known game-engine window class covering the screen (stingray_window, helldivers2)
```

### 预检 #4 整段（原样）

```
Fri Oct 23 22:05:59     2026
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: helldivers2 pid=53960 class=stingray_window style=0x94880000 rect=-1,-1,2562x1442 screen=2560x1440 covers=True borderless=False title=HELLDIVERS? 2
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=40% used=6934MB
free_memory_gb=7.2
VERDICT=HOLD
  reason: focused window covers the screen and its process path is unreadable (helldivers2, class stingray_window)
  reason: focused window is a known game-engine window class covering the screen (stingray_window, helldivers2)
```

处置同前三次：**不起跑**。作者那局游戏仍在前台全屏（同一 `pid=53960` 从入场至今未换），预检的两条 reason 每次都完整在场，`free_memory_gb` 只是从 4.4 涨到 7.2——它不是 HOLD 的判据，两条窗类／覆盖 reason 才是。

### 预检 #3 整段（原样）

```
$ date
Fri Oct 23 22:00:13     2026

$ powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: helldivers2 pid=53960 class=stingray_window style=0x94880000 rect=-1,-1,2562x1442 screen=2560x1440 covers=True borderless=False title=HELLDIVERS? 2
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=42% used=7587MB
free_memory_gb=6.2
VERDICT=HOLD
  reason: focused window covers the screen and its process path is unreadable (helldivers2, class stingray_window)
  reason: focused window is a known game-engine window class covering the screen (stingray_window, helldivers2)
```

### 预检 #1 整段（原样，含 `?` 那处＝控制台编码把游戏标题里的图形字符打成问号，非本报告改写）

```
$ powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: helldivers2 pid=53960 class=stingray_window style=0x94880000 rect=-1,-1,2562x1442 screen=2560x1440 covers=True borderless=False title=HELLDIVERS? 2
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=44% used=7463MB
free_memory_gb=4.4
WARN (does not block the build):
  free_physical_memory=4.4 GB (gate readings may be invalid; SQLite disk-I/O/OOM shape)
VERDICT=HOLD
  reason: focused window covers the screen and its process path is unreadable (helldivers2, class stingray_window)
  reason: focused window is a known game-engine window class covering the screen (stingray_window, helldivers2)
```

处置：按工单 §〇.7 与本报告 〇.6.9——**不起跑、不请示、不改预检判据、不绕过**；把零 `dotnet` 的活先做完（本报告 §〇 已落盘、载体已摆好并三事自证通过、突变靶行与判据字节已当场取证、上一刀 §六 配方已逐字抄进 〇.9），再重跑预检。

### 预检 #2 整段（原样）

```
$ date
Fri Oct 23 21:54:45     2026

$ powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: helldivers2 pid=53960 class=stingray_window style=0x94880000 rect=-1,-1,2562x1442 screen=2560x1440 covers=True borderless=False title=HELLDIVERS? 2
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=42% used=7560MB
free_memory_gb=6.1
VERDICT=HOLD
  reason: focused window covers the screen and its process path is unreadable (helldivers2, class stingray_window)
  reason: focused window is a known game-engine window class covering the screen (stingray_window, helldivers2)
```

处置同 #1：**不起跑**。等待窗口里做完的零 `dotnet` 核对（不改任何东西，只为收尾那两轮的取证口径）：

- 仪器面：`.local-notes/wave26-1023/tools/targeted.sh`（定向轮整行跑法＋`git diff -U0`/`--stat`＋TRX 计数＋两侧 SHA-256）、`tools/readtrx.sh`（`<Counters>` ＋按 `outcome="Failed"`／`"NotExecuted"` 逐节点取 testName＋消息要点＋`Stacktrace` 里的 `.cs:line N` 红点）。`readtrx.sh` 已用**自建合成 TRX**（`tools/selftest/fake.trx`，非任何历史轮产物）做过冒烟测试：`<Counters total="718" …>` 取到、`[Failed] … 红点 NarrativeCommitFactoryTests.cs:60` 取到、`[NotExecuted]` 点名取到。仪器不调用预检也不不自作还原，跑法与还原各由一条独立命令出，便于逐轮贴原文。
- 环境面：本机 SDK＝`10.0.200`（`ls "/c/Program Files/dotnet/sdk"`）；`~/.nuget/packages` 已有 73 个包（carrier 的 `obj/` 为空＝首轮要 restore，包在本地缓存里）；无 `global.json`／`nuget.config`／`Directory.Packages.props`。
- 收尾自证面：carrier 的 `.gitignore` 第 1／2／10／32 行＝`bin/`、`obj/`、`TestResults/`、`/.local-notes/` ⇒ 构建与测试产物不会污染 `git status --short`，工单 §三 那句"跑前跑后 0 行"才可核。

### 等待窗口里的突变面预合（零 `dotnet`、零载体写入：在 `tools/dryrun/` 的临时副本上做，载体字节一字未动）

把两枚计划中的突变先在**副本**上落一遍并取 SHA，用来证明"这一轮的 ③ 与上一把逐字节同一"以及"R4 只动数组那一个成员"：

```
$ git diff --no-index -U0 tools/dryrun/r3_before.cs tools/dryrun/r3_after.cs
@@ -203 +203 @@ public sealed class NarrativeContextCompiler(ITokenEstimator? estimator = null)
-        if (cost > cap || used + cost > total)
+        if (cost > cap)
→ 1 file changed, 1 insertion(+), 1 deletion(-)

$ git diff --no-index -U0 tools/dryrun/r4_before.cs tools/dryrun/r4_after.cs
@@ -271,2 +271 @@ public sealed class NarrativeRoundRunner
-                string.Join(",", draft.Actions.Select(a => a.ActionId + ":" + (int)a.Mode)),
-                string.Join(",", draft.Intents.Select(i => i.IntentId))])))).ToLowerInvariant();
+                string.Join(",", draft.Actions.Select(a => a.ActionId + ":" + (int)a.Mode))])))).ToLowerInvariant();
→ 1 file changed, 1 insertion(+), 2 deletions(-)

$ sha256sum tools/dryrun/r3_before.cs tools/dryrun/r3_after.cs tools/dryrun/r4_before.cs tools/dryrun/r4_after.cs
86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842   r3_before（＝载体 HEAD-blob，与 〇.3 同值）
72340f0597b1278299dac9c27453d845f56ca013ba727584d0846d3d27dc6fb9   r3_after
961fb49bb464188a93eebb4c7059e8c1129d2287e9f626976cae754c65ed37e9   r4_before（＝载体 HEAD-blob，与 〇.3 同值）
1126b6016693a0c0a42f3f634a08f0f9aa1807af2491383be3d6ef0352736135   r4_after
```

- **R3 的突变态 SHA＝`72340f05…6fb9` 与第二十五波报告 §一.1 ⑩／§三.2 ⑩ 记录的那一枚逐字符相同** ⇒ 本刀 R3 与上一把 A1／§三.2 是同一枚字节改坏（工单 §一 要求"改法与第二十五波 §一.1／§三.2 逐字节同一"这一条就此核到，不用等跑完再回头看）。
- R4 的突变＝collection expression 里少了第⑨个成员（`draft.Intents.Select(...)` 那一项连同它前面那枚逗号一起消失），`NarrativeCommit` 的字段赋值（`:276`–`:282`）与九项之外的顺序一字未动；`1126b601…` 是本刀新取的值（上一把从未跑过这轮）。
- 两枚靶文件在工作树里是 **LF**（`CR count=0`、`LF count=470／341`），我的文本替换按原文行尾写回，不会把整文件换行改掉（上面 diff 只有一处／两处命中即为证）。

### 等待窗口里做完的零 `dotnet` 取证（R4 红集归属的静态依据，防"批次红署错单枚突变"）

`grep -rn "BuildCommit" tests/ src/` 与 `grep -rn "[0-9a-f]\{64\}" tests/` 当场结果：全仓**只有** `tests/Pry.Core.Tests/NarrativeCommitFactoryTests.cs` 的四枚绝对十六进制字面量（第 47／75／76／77 行）依赖 `NarrativeRoundRunner.BuildCommit` 的产出；其余带 `ContentHash` 的判据（`NarrativeCommitAndActionGuardTests.cs:407`、`NarrativeEndToEndPipelinePrototypeTests.cs:514/594`、`NarrativeLifecycleCoordinatorTests.cs:726`、`NarrativeStorageContractTests.cs:592`）用的是测试自己写的占位串（`"sha256:base"`／`"sha256:wave23-host-hash"` 一类）或该类私有的同名 helper（`NarrativeSqliteCommitDeliveryTests.cs:596`），不经产品工厂 ⇒ R4 的可归因红集应＝该文件的第 60／92／117 三处断言所在的 5 枚节点。这一段是**静态读码**，不是读数；盘上凭证由 R4 那轮的 TRX 逐格点名给。

另：`grep -rn "ContentHash" tests/` 里没有任何一格断言摘要长度或成员数，所以"九项变八项"若只影响摘要值，红点必然落在那三处 `Assert.Equal(绝对字面量, …)`；若 R4 盘上还出现别的红格，我会按消息形状＋红点行号如实登记并停下让守门人判，不自作主张。

---

## 九 守门人出口核验与归位裁决（签名 `eval-QA-WAVE26-GK-1023`，10-23 22:4x）

上面 §〇–§八 是第二十六波 lane 的原文，我一字未改、未代写、未署它的名。这一节是我这一手的独立复核，只读盘上产物；结论先写在最前面：**这把交回的是"合法的空交付"，我不但它、也不罚它，账一格不减、基线一格不动、`git merge --ff-only 24d1b6f` 照旧扣住。**

### 九.1 "零 `dotnet`"我用三条互相独立的盘上面复核，不采信它的自述

- **预检八份全在、全 `HOLD`**：`out/preflight-R3.txt`＋`preflight-R3-try{2..7}.txt`＋`preflight-try8.txt`＝实数 8 个文件，逐份 `grep -o "VERDICT=[A-Z_]*"` 取回全是 `HOLD`；前台窗八次同一枚（`helldivers2 pid=53960 class=stingray_window rect=-1,-1,2562x1442 covers=True`），`free_memory_gb` 逐次＝4.4／6.1／6.2／7.2／6.8／7.6／6.1／5.7。
- **没有任何编译落过盘**：`find <carrier> -maxdepth 3 -type d \( -name bin -o -name obj \) | wc -l`＝**0**（我当场独立跑的那条，与它 §四 的数同值）；`runs/` 与 `full/` 两目录存在但 `*.trx` 计数＝**0**。⇒ 这条比"它说没跑"硬：真跑过一次 `dotnet test` 就不可能没有 `bin`／`obj`。
- **载体与主树的字节面**：载体 `rev-parse HEAD`＝`24d1b6f8b0a4a3d199dcab832e15facc84a16a01`、`git status --short`＝**0 行**；两枚靶文件 `sha256sum`＝`86c301604dc1…d9842`／`961fb49bb464…65ed37e9`，逐字符等于各自在该载体的 HEAD-blob。主树 `git status --short --branch`＝9 行＝它入场那 8 行＋它那枚未跟踪报告；他人名下两枚暂存项的索引 blob 跑前跑后一字未变（`6a78b81c71636d0757474084301cd35c2d0a83d7`／`954584e31866abf143468f4b6ebf530ddfa319b0`），主树零 `git` 写、零 `dotnet`。
- 22:43 我自己再跑一次预检＝**仍 `HOLD`**（同一 pid、同一窗、`free=5.8`、gpu_util 51%）⇒ 归位这一手在我手里同样只能零 `dotnet`，这与上一刀 §八.1 是同一形状。

### 九.2 它顶回来的一条工单缺陷，我核为真，并且这缺陷是我写的

工单 §二 末行与本报告 §二 ⑩ 要求 R4 还原后 SHA＝`92fbce48c794…353570`。我当场三取：`git show a804a53:src/Pry.Core/Narrative/NarrativeRoundRunner.cs | sha256sum`＝ **`92fbce48…`**；`git show 24d1b6f:<同一路径>`＝ **`961fb49b…`**；载体工作副本实测＝`961fb49b…`。⇒ **工单那个值＝提取前的线尾字节，在钉在 `24d1b6f` 的载体里根本不可达**；照字面核会把一次正确的还原读成"还原失败"，进而误判整轮作废。这条错出在我写工单时把上一刀 §六 的还原值原样抄了过来，没按"载体基点＝交付面"重算——**登记作废：凡突变对象文件被载体提交自身改过，还原判据必须取该载体的 HEAD-blob，不是主树线尾值**。lane 采用的更严口径（HEAD-blob 值＋`status` 0 行＋`git diff` 空三件同判）我照单收下，并写进工单更正段。同一格另两处按"提取前"写就的值我也顺带复核过：工单 §〇.3 的靶子 `NarrativeContextCompiler.cs`＝`86c30160…` 在 `24d1b6f` 与线尾**同字节**（那枚提交不碰它），所以只有 `:272` 那一处错，不是系统性错。

### 九.3 两枚突变文本我独立复算过：这一把与上一把改的是同一字节

`.local-notes/wave26-1023/tools/dryrun/` 四份副本我当场 `sha256sum`：`r3_before.cs`＝`86c30160…d9842`（＝载体交付面）、`r3_after.cs`＝ **`72340f0597b1…27dc6fb9`**、`r4_before.cs`＝`961fb49b…65ed37e9`、`r4_after.cs`＝ **`1126b6016693…52736135`**（本刀首次落档）。`72340f05…` 在第二十五波报告里出现 2 次（§一.1 ⑩ 与 §三.2 ⑩）且逐字符相同 ⇒ **两把刀要钉的是同一枚改坏，没漂字节**；`1126b601…` 是本刀首次落档（上一刀没算过 R4 的突变态）。这些只证"突变文本已对齐"，**一律不充当"跑过"的凭据**——lane 自己在 ⑩ 里把这句写明了，我照录。判据节点行号我也从 `git show 24d1b6f:` 独立复取：`NarrativeCommitFactoryTests.cs` 的 `[Fact]` 55／断言 60、`[Theory]` 74、三枚 `[InlineData]` 75／76／77、`[Fact]` 98、那条绝对字面量断言 117；`NarrativeBudgetInequalityCellTests.cs` 的 `[Fact]` 58／首枚可能失败断言 65——与它 §一 ⑦／§二 ⑦ 逐字相同。

### 九.4 一处自我登记的不合规，我裁定为"如实、无损害"，但仍留下

八次预检的 mtime＝21:49:29／21:54:48／22:00:15／22:06:01／**22:10:33**／22:17:10／22:22:44／22:28:18；相邻间隔＝5m19／5m27／5m46／**4m32**／6m37／5m34／5m34。工单 §〇.7 写的是"不短于 5 分钟"，#5 差 28 秒——**这是它自己贴出来并点名的**（§预检台账）。我的裁定：那一次结果仍是 `HOLD`、那一次之后没有起跑任何 `dotnet`，所以没有造成任何作者侧损害，也不算绕过（绕过＝读到 HOLD 还起跑，或改预检判据）。**收下不但**，但登记一条给下一把：**额度用尽就是额度用尽，不能因为"差 28 秒"把它算成合规**；这一把的 8 次是 8 次，续跑必须换新刀重新计数，不许在旧刀里补第 9 次。

### 九.5 裁决与下一手（含"这一把我为什么不立刻再派一把"）

1. **账一格不减**：红证 0 枚、新格 0 枚、判据字节改动 0、换手覆盖 0 格。那 6 枚新格今天仍是**未证伪**；`1333 ＋ 6 ＝ 1339` 仍是申报；在册基线照旧 **1333／executed 1331／failed 0／跳过 2**；`NarrativeDraftValidator`／`ContextBuilder` 那两本旧账照 §八.4 与本节口径不动。
2. **撤格硬线没有触发**（lane 在 §二 末句的推理我核为正确）：那条"第 55 行 `[Fact]` 不响就当场撤掉"的前提是**拿得到读数**；没有读数既不能宣布门响、也不能宣布它是常绿门，而撤一格＝改判据字节，超出本刀允许文件面。**它没有为了让账好看去撤格，也没有为了交差去造读数——这两条都是对的方向。**
3. **归位通道**：`git merge --ff-only 24d1b6f` 继续扣住；字节另有一份 `.local-notes/wave25-1023/out/wave25-code.patch`（18,579 B）兜着，载体被回收也不丢。**下一把的前置不是"新工单"，而是一次 `VERDICT=BUILD_OK` 的窗口**：配方已就位（本报告 §六 那段逐字可粘贴，只把 R4 的还原值按九.2 更正为 `961fb49b…`），载体已建好、结果目录已建好、两枚突变已预合并核过 SHA、读盘仪器 `tools/readtrx.sh` 已冒烟测过。我在 22:43 实测仍 `HOLD` ⇒ **本刀不立刻再放一把 lane 出去干等**：那会把 150 轮额度花在反复跑同一支预检上，而它能做的零 `dotnet` 活这一把已经全部做完了。等到一次 `BUILD_OK` 再出闸，一轮就能把 R3／R4／R5 跑穿。
4. 一句它替下一手抓出来的顺序约束我确认过、并写进工单更正段：`scripts/measure-fixed-eval.ps1` 会向 `-ResultsDirectory` 那个目录**写** `fixed-eval-coverage.json`（`:111-112`），所以收尾那轮必须**先 `ls *.trx` 数到 4 枚、逐份取完 `<Counters>`，再跑它**——反过来就会把 json 混进目录计数，把"四份 TRX"这条判据数歪。
5. 三句照旧不许说，本刀一句也没说成：新链已接线／旧链可删／不进包已做到。边界也照旧：本刀只补"门会不会响"的证据链，而且今天连一环都没接上；它没有证明她今天的行为是对的，更没有证明那 6 枚格该进主线。

**本节之后本报告不再有未落盘产物**：全部盘上足迹＝九.1 列的三处（八份预检归档、`runs/`＋`full/` 两个空目录、`tools/` 六枚文件）与那枚干净的 `qa-wave26-1023/carrier`。行数与字节由我在 §九 落盘后当场复取（见 `PROJECT_PLAN.md` §9 那条与本文件的 git diff 统计）。
