# HDS-003 D 类第四片证据刀（波 44／HDS-WAVE44-1029，T3 余 11 枚里的五枚：三枚单行条件＋一枚合取半边＋一枚值面清空）——lane 出口报告

- 工单＝`docs/dispatch-hds-wave44-1029.md`；坐标系＝`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS-003；账本＝波 22 审计报告 `docs/eval/reports/2026-10-23-hds003-005-cell-evidence-audit.md` §四.1 梯队 T3。
- 性质＝证据刀·换人复采。本刀零产品新字节（终态）、零判据字节、零新格、零新增数字门槛、零 UI、零生产接线、零打包；本文件＝主树里本刀唯一新建文件，不提交、不 `git add`。
- 载体＝`.local-notes/qa-wave33-1028/carrier`（detached HEAD `51f149d`，基点 `fd93ebe`）；判据文件＝`tests/Pry.Core.Tests/NarratorPrototypeTests.cs`（一枚字节不写）。
- 产物目录＝`.local-notes/wave44-1029/`：`out/w44-count.txt`、`out/w44-cells.txt`、`out/item1-2-3-prep.txt`、`out/closure-scan.txt`、`out/instrument-selfproof.txt`、`out/instrument-selfproof-run1-mismatch.txt`、`mutation-prep/`、自写脚本 `w44-count.js`／`w44-cells.js`／`w44-prep.js`／`w44-scan.js`／`w44-trx2.js`／`w44-apply.js`。
- 守门人裁决节＝§九，本节号在本报告里保留为空白，本刀不写。本报告不写状态／收口类用语，不写顶账，不动基线，不自宣基线。
- 名册口径注：本报告里的"名册序号"＝本刀自判据 blob 逐枚解析的展开序号（56 枚），与工单引用的波 22 审计 §四.1 点号（#16／#18／#34／#36／#48）是两套编号，逐处并列写出，不互相顶替。

## 〇 入场先数盘（全部当场从盘取，不引用工单或波 43 的数字当入场值；原始面＝`out/w44-count.txt`＋`out/w44-cells.txt`）

### 〇.1 主树

- `git rev-parse HEAD`＝`7687457587a2cc33d0f54c92be56f2c70a645bfd`。
- `git status --short --branch` 原样逐行（共 9 行，含分支行）：

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
- 入场门读数：`git diff --numstat HEAD -- docs/plans/PROJECT_PLAN.md`＝`18	0	docs/plans/PROJECT_PLAN.md`＝工单 §〇.1 要求的 18/0 ⇒ 入场合规，未停手。
- `git worktree list` 行数＝76。

### 〇.2 载体四件

- `rev-parse HEAD`＝`51f149d0b722e28a6566c93d19f48a94ca4c85df`。
- `status --short` 行数＝0（入场即空，起跑合规）。
- `log --oneline fd93ebe..HEAD` 行数＝2（`51f149d`＋`1e02205`，逐行原文见 `out/w44-count.txt` §2）。
- `rev-parse --show-toplevel`＝`C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave33-1028/carrier`。

### 〇.3 判据文件三面 SHA（起跑门，逐字符等值才准起跑）

`tests/Pry.Core.Tests/NarratorPrototypeTests.cs` SHA-256 三面当场值：

- `blob(51f149d)`＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`
- 载体工作副本＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`
- 主树工作副本＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`

三面逐字符等值且等于工单 §判据基准 ⇒ 起跑门过。三枚被改产品文件的三面 SHA 也当场取齐（每枚 blob＝载体工作副本＝主树工作副本）：`NarratorContracts.cs`＝`3a33a8151e61c3396963dac451dffcb46acb2f02807f140d18f694d6a52a9f8c`；`NarrativeDraftValidator.cs`＝`88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c`；`NarrativeContextCompiler.cs`＝`86c301604dc178d72e726a4c177b737c21a20eba504420b12dace4d1771d9842`。

### 〇.4 判据格数当场重取（载体侧现文＝blob，逐字节等值已由 〇.3 证明；原始面＝`out/w44-cells.txt`）

- `[Fact]`＝53（`[Fact]` 独占行形状与 `[Fact(` 变体都数，见 `w44-cells.js`）；`[Theory]`＝1（起点 :433）；`[InlineData`＝3（:434/:435/:436，逐枚在盘）。
- 工单等式：`total = [Fact] + [Theory]×[InlineData] = 53 + 1×3 = 56`。
- 本刀另按"每枚 Theory 各自的 InlineData 枚数"精确展开复核：53 枚 `[Fact]` ＋ 唯一 `[Theory]`（:433）的 3 枚参数行＝53＋3＝56，两式同值（该文件只有一枚 Theory，故乘式与展开式必然相等）。
- **本刀定向跑每轮 `total` 必须等于这枚当场值 56**；名册逐枚（@口径＋方法名＋体内特征）见 `out/closure-scan.txt` §A，逐枚展开 56 枚，与上式互检全对。
- `Assert.Skip` 计数＝0；判据文件行数（`split('\n')`，尾随换行产生末空段）＝990；字节数＝46491；CR 枚数＝0（LF 文件）。

### 〇.5 取数仪器、读数口径与件6／件10 自证

- 读数口径（件5）＝只数 `<UnitTestResult … outcome="Failed">` 开标签，并与同份 `<Counters failed>` 双向对账；`<ResultSummary outcome="Failed">` 不算用例（每份 TRX 实测该枚＝1，未计入红枚）；计数器形状＝单枚 `<Counters total="…" executed="…" passed="…" failed="…" notExecuted="…" />`、属性名小写、按名字取不按顺序；取回的属性值与 `<Message>`／`<StackTrace>` 文本一律解 XML 实体，含数字实体 `&#xD;`／`&#xA;`；栈帧锚点＝`in <路径>:line N`（TRX 里没有 `at ` 可锚，路径含盘符冒号，切分不用 `[^:]`）；跳过格只认 `outcome="NotExecuted"` 开标签；`testName` 是全限定＋参数表，回判方法名先剥括号。仪器＝自写 `w44-trx2.js`（不沿用波 41／42／43 实现，也不沿用守门人 `gk43-*.js`）。
- 件10 解码通道自证（`out/instrument-selfproof.txt` 首节）：探针串 `A&#xD;<LF>B &quot;q&quot; &#10; &#x41;Z &amp;amp &#xA;尾部` 解码后 CR 出现＝true、LF 出现＝true、`"` 出现＝true、`&#x41;` 解为 `A`＝true、字面 `&amp;` 未被误食品＝true。
- 件6 仪器自证：对工单 §一.5 的 16 枚历史定向面（波 41 trx-a/b/d/e、波 42 trx-g/h/i/j、波 43 trx-f2/g2/i2/j2、波 40 trx-M8p/m08、波 39 trx-M2／trx-M8-voided-P103）＋2 枚全量面（波 13 run-full、波 40 trx-F）逐面复算：**18 面全部 AGREE**（含 Failed 开标签↔Counters 双向对账、帧行集合、解码后消息逐字符等值、total/executed/passed 求和、NotExecuted 逐字符点名）。其中件 10 要求的"具体报错消息文字"面值取自波 41 `trx-d`＝`Assert.Throws() Failure: No exception was thrown&#xD;\nExpected: typeof(Pry.Core.Narrative.NarrativeContextAssemblyRejectedException)`，逐字符等值才算 AGREE；trx-d 附加面"同方法另两参数行同场 Passed"亦按 Passed 名册逐枚读到。
- **本刀仪器自己的第一版错面（如实登记，原始面＝`out/instrument-selfproof-run1-mismatch.txt`）**：首跑 16 枚定向面全报 MISMATCH，逐面查证后证明坏的是仪器不是盘——①我把工单 §一.5 的压缩记法 `Expected: ContractRejected / Actual: Deferred` 直接当逐字符面值比对，而盘上真实解码消息是 `Assert.Equal() Failure: Values differ\r\nExpected: ContractRejected\r\nActual:   Deferred`（`Actual:` 后两枚空格、换行是 CR+LF）；②我把 trx-d 的 `testName` 写成编码形态 `…(scope: &quot;conversation&quot;)`，而仪器已按件5 解码成 `…(scope: "conversation")`。盘面取证值两遍完全一致（同帧行、同计数、同消息），只有我这两处字面量写法变过；读数逻辑一行未动。修正＝另存 `w44-trx2.js`（只改面册字面量），坏的那版留档 `w44-trx-v1-facespec-bug.js`（语法已被我自己的补丁写坏，留证不再使用）。⇒ 结论：新口径"工单表里的 `/` 记法不是面值，进自证表前必须先解一遍盘上原始字节再落字面量"。
- 行号种类全篇标注：**@＝判据文件里格起始行（方法声明行）；: ＝TRX 断言帧行或产品／判据文件里的行**，两种口径不混用不标即违规。

### 〇.6 构建跳过三段判定（预算 §四.1，默认合法跳过）

- 段一（0 行式子成立）：`git -C 载体 diff --stat 51f149d..HEAD -- src tests Pry.slnx` 行数＝0，且载体 `status --short` 行数＝0（〇.2）⇒ 载体工作副本与 `51f149d` 在这四枚面（产品／判据／解／props）上零行差；主树与本刀无关的那两枚 `M ` 项不在我的落点内，未读未改。
- 段二（成品在盘）：`carrier/src/Pry.Core/bin/Release/net10.0/Pry.Core.dll`＝1,745,920 字节、mtime 2026-10-29T04:54:01Z（本地 12:54）；`carrier/tests/Pry.Core.Tests/bin/Release/net10.0/Pry.Core.Tests.dll`＝1,177,088 字节、mtime 04:52:20Z（12:52）在盘。
- 段三（三枚 SHA 三面未变）：判据三面 `4f1b2f6e…`（〇.3）＋三枚被改文件三面（〇.3 逐枚列出，三面各自等值）。
- §一.7 清洁度限定照写：在盘 `Pry.Core.dll` 体积正是工单 §一.7 记的波 43 出场态（1,745,920/12:54＝V:131 突变态产物），**只支撑"文件存在＋体积合理"，不支撑"该二进制与基线源码逐字节对应"**；第一轮定向跑的自带增量编译会按本刀突变态再换一副字节。本刀因此不跑解决方案级 `dotnet build`（预算 ≤1，动用 0）。

### 〇.7 锚点现文核对（§一.6 换锚门，原始面＝`out/w44-count.txt` §5）

五枚落点按载体 `51f149d` 现文逐行重取，全部读到预期文本（非 `{`／`}`／注释行之类漂移面）：`N:43`＝`if (!Assembly.InstanceId.Equals(Request.InstanceId))`、`N:57`＝`if (!string.Equals(Assembly.BatchId, …))`、`V:200`＝段数合取行、`V:214`＝`if (action.DeferredIntentId is not null)`、`K:99`＝`SourceRefs = [payloads.IdentitySourceRef, payloads.CurrentEvent.SourceRef],`。
漂移实测并登记：工单 §二 n′ 那格引用的"`:93` 的 `DeferredAction(...)`"在 blob 里读作 `        {`（判据文件 :93 是 `DeferredAction` 夹具体的一行，不是 `NarrativeDraftValidator.cs:93`）；本刀按盘面重锚＝夹具 `DeferredAction` 定义＝判据 :89-:94（`PlannedSegmentCount = 0, DeferredIntentId = intentId` 在判据 :93），产品侧 Deferred 支入口＝`NarrativeDraftValidator.cs:222`，两者都取到字节面，不照抄历史行号下刀。另 §一.6 点名的 `R:297`／`R:327`／`D:130` 本刀未使用为落点，不受影响。

## 一 前置件成品（件1／件2／件3／件3′／件8／件9／件10；全部零 `dotnet` 当场从盘产出，原始面＝`out/item1-2-3-prep.txt`、`out/closure-scan.txt`、`out/instrument-selfproof.txt`）

### 一.0 件1＝改前整行并排自证（五枚，OLD＝工单 §二 现文 vs 载体 blob 现文）

五枚锚点全部按载体 `51f149d` blob 现文当场重取；三枚产品文件 EOL＝LF（cr-count=0）。逐枚并排打印＋改前行内容逐字符等值断言＋整行（按 `\n` 切分后逐字符等值）出现次数＝恰 1，全部 OK。SHA 对账不替代并排自证（并排面在下表，SHA 面在 〇.3）。

| 轮 | 落点（载体 blob） | OLD 现文（逐字符，工单 §二 vs blob 现文两列已在 `out/item1-2-3-prep.txt` 并排打印） | 等值断言 | 整行恰 1 次 |
|---|---|---|---|---|
| k′ | `NarratorContracts.cs:43` | `        if (!Assembly.InstanceId.Equals(Request.InstanceId))` | OK | 1 |
| l′ | `NarratorContracts.cs:57` | `        if (!string.Equals(Assembly.BatchId, Request.BatchId, StringComparison.Ordinal))` | OK | 1 |
| m′ | `NarrativeDraftValidator.cs:200` | `                if (action.PlannedSegmentCount < 1 || action.PlannedSegmentCount > request.MaxOutboundSegments)` | OK | 1 |
| n′ | `NarrativeDraftValidator.cs:214` | `                if (action.DeferredIntentId is not null)` | OK | 1 |
| o′ | `NarrativeContextCompiler.cs:99` | `            SourceRefs = [payloads.IdentitySourceRef, payloads.CurrentEvent.SourceRef],` | OK | 1 |

### 一.1 件2＝突变文本预合成（五枚落 `mutation-prep/`，SHA-256＋字节数当场值；每轮"施加后 SHA"必须逐字符等于该行）

| 轮 | 预合成文件 | 改后行（逐字符） | SHA-256 | 字节数 |
|---|---|---|---|---|
| k′ | `NarratorContracts-k.cs` | `        if (false)` | `4b5ea7a0734558ba7bc9ed4d9c9f2fbca5ca1624ab8da5894d2ba5ff39132c0a` | 5494 |
| l′ | `NarratorContracts-l.cs` | `        if (false)` | `3ed287620ec893abfa3070245ffba39730a967231902e39d9045bc42f170bd94` | 5466 |
| m′ | `NarrativeDraftValidator-m.cs` | `                if (action.PlannedSegmentCount < 1)` | `6c5ec1e9618ecc47c65b955447e31d4a982d3e7a147c5b882c0f902b2cbbbecf` | 19278 |
| n′ | `NarrativeDraftValidator-n.cs` | `                if (false)` | `c2657962758c35a77befe2f7fc5c33b7c4f7c1245df0e98d9e4d185d37e7633b` | 19308 |
| o′ | `NarrativeContextCompiler-o.cs` | `            SourceRefs = [],` | `cf3a0a2a4a37978e7ac4e2ad86c52e90821282864bd85a97236850409e341678` | 24398 |

另有字节回环自检（不改动任何行时重建必须逐字节等于 blob）五枚全 OK＝证明预合成只动那一行、没有编码／换行副作用。o′ 那枚已预合成但按件 3′ 不起轮（见 §一.6），文件留档不动。

### 一.2 件3＝形状自检（五枚全过；净偏移守恒，不是绝对数相等）

逐枚实测（`out/item1-2-3-prep.txt`）：被改行圆括号净 OLD→NEW、中方括号净、大括号净均守恒；全文件净圆括号／净大括号／中方括号收支不变；行数不变。

| 轮 | 被改行净偏移（圆／方／大）OLD→NEW | 全文件净收支 OLD→NEW（圆／方／大） | 行数 |
|---|---|---|---|
| k′ | 0/0/0 → 0/0/0 | 0/0/0 → 0/0/0 | 125→125 |
| l′ | 0/0/0 → 0/0/0 | 0/0/0 → 0/0/0 | 125→125 |
| m′ | 0/0/0 → 0/0/0 | 0/0/0 → 0/0/0 | 421→421 |
| n′ | 0/0/0 → 0/0/0 | 0/0/0 → 0/0/0 | 421→421 |
| o′ | 0/0/0 → 0/0/0（中方括号 `[`+`]` 成对消去，净仍 0） | 0/0/0 → 0/0/0 | 471→471 |

### 一.3 件3′ 点名公约数＋件9 下游消费者枚举表（闭合表共读面；证据源＝载体 blob 本刀自推，未抄波 43 §一文本；原始面＝`out/closure-scan.txt`）

盘上事实（逐条取证，行号种类照 §〇.5 标注）：

- **名册**＝判据 blob 逐枚解析方法声明行（@口径）：54 枚方法体，其中 :433 那枚 `[Theory]` 带 :434/:435/:436 三枚 `[InlineData]`，按参数行展开＝56 枚（`out/closure-scan.txt` §A 逐枚点名，含体内特征 `run=`／`compile=`／`validate=`／`imm=`／`def=`／`sil=`／`seg=[…]`／`deferIntent=`／`srcRefs=`）。
- **N:43／N:50／N:57 的短路序**（`NarratorContracts.cs` 现文，本刀通读自推）：`NarratorCall.Validate()`＝:32-:34 三枚 `ThrowIfNull` → :36 `!Assembly.Accepted` ⇒ :38 抛（ReasonCode=Assembly 自带拒绝码）→ **:43 实例不等 ⇒ :45 抛 CallScopeMismatch** → :50 会话不等 ⇒ :52 抛 → **:57 批次不等 ⇒ :59 抛** → :64 `return this`。三支是同一方法内的顺序 `throw`，不是值面；任一支摘掉后控制流会继续求值它后面的支，故闭合表必须逐枚说明后面的支对该参数行是否仍为 false。
- **R:105 是唯一产品侧消费者**：`git grep` 面（`out/closure-scan.txt` §B11）＝`NarrativeRoundRunner.cs:105 var call = new NarratorCall { Request = request, Assembly = assembly, Budget = budget }.Validate();` 全仓 `src/` 仅此一枚命中；`NarrativeTurnPipeline.cs:219` 的 `.Validate()` 是 `new NarrativeRequest{…}.Validate()`（:207-:219 现文），**不是** `NarratorCall.Validate`，不在 N:43／N:57 射程内。判据侧直接入口＝:425-:428（格 @420）与 :447-:450（格 @437 三枚参数行），逐处见 §B5。
- **装配体的三面 id 恒等于请求**（k′／l′ 的"不连坐"字节依据）：`NarrativeContextCompiler.cs:171`＝`InstanceId = request.InstanceId, ConversationId = request.ConversationId, BatchId = request.BatchId,`（成功装配面）与 :300（`Fail(…)` 面）同形；`NarratorCall.Validate` 的三面比较（N:43/:50/:57）在 `R:105` 这条路上改前改后都恒为 false ⇒ 30 枚 `RunAsync` 格（§B12 枚点逐枚 30 处）一条都不连坐。
- **V:200 右半边的消费面**：`request.MaxOutboundSegments` 在判据里只出现在 `Request()` 形参默认值（:41 `int maxSegments = 4`）与 :52 赋值两处，**零枚格传过 `maxSegments:`**（§B2 逐处）；`segments:` 显式传参恰两处＝:673 的 `6` 与 :686 的 `4`（§B1），`ImmediateAction()` 默认 3（:82-:87）。故 `> request.MaxOutboundSegments` 改前只对 :673 那枚为 true。
- **V:214 的消费面**：`DeferredIntentId` 在判据里恰两处（§B3）＝:93（`DeferredAction` 夹具，Mode=Deferred）与 :700（`ImmediateAction() with { DeferredIntentId = "intent-9" }`）。V:214 在 `switch (action.Mode)` 的 `case CommunicationMode.Immediate:`（:199）体内，Deferred 支（:222）里的对应门是 :235／:240，**不求值 V:214**；`DeferredAction(` 四处调用点（§B7＝:505/:601/:616/:834）全属 Deferred 支。
- **K:99 的值面消费者枚举（件9 射程，本刀自取）**：产品侧＝`NarrativeContextCompiler.cs:440 EmittedSourceRefs => [.. Segments.SelectMany(s => s.SourceRefs)]` 一枚；`git grep SourceRefs 51f149d -- src` 全部 9 枚命中都在该文件（§B9：:99/:119/:133/:275/:294 是构造点，:409/:461 是 record 默认 `[]`，:440 是唯一读取点，:468 是 `…SegmentFactory.ToSegment()` 的复制点，Identity 段走 :93-:101 的直接构造不经过它）⇒ 编译器不校验它非空，清空不会让 `Compile` 抛。判据侧读取面 12 处（§B10）：:294/:309/:920/:925/:966/:987 读的是**别的段**（RecentMessages／State／BehaviorReferences 的 `section.SourceRefs`，出处在 :275/:294，不经 :99）；只有 :372/:373/:374/:375（同格 @367）、:481（格 @471）、:772（格 @761）读 `EmittedSourceRefs`。
- **V:200／V:214 的下游控制流**（件9：这两行不是早退 `return`／`throw`，是 `rejections.Add` 的门）：`NarrativeDraftValidator.Validate`（:107）在 :137 `CheckConsumption`、:145-:148 逐动作 `CheckAction`（:186）之后 :160 一次返回全部拒绝；`NarrativeRoundRunner` :136 取回、:137 `rejections.Count > 0` ⇒ ContractRejected 返回（:141-:144）。摘掉一条拒绝只改变 :137 的计数，不改 R 里其它门。
- **m′ 摘门后的下游（必须逐枚枚举，不许"形状不同仍只算 1 枚"）**：`R:137` 放行 ⇒ `R:150-:152` 进 `ImmediateBranch`：`R:183 BuildCommit`（:259）→ `R:184 _history.RegisterAppliedCommit(commit, request.MaxOutboundSegments)` → `R:191 slice = commit.NarrativeProse.Substring(ResolvedOffset, QuotedText.Length)`＝`"answered him"`（12 字符）→ `R:192 SplitContiguous(slice, 6)`（:287-:304 现文：`baseLength=12/6=2, remainder=0` ⇒ 恰 6 段各 2 字符，逐段非空）→ `R:202 TryAppend` 逐段：`DeliveryLedger.cs:115 var ceiling = Math.Min(action.PlannedSegmentCount, commit.MaxSegmentsPerAction)` ＝ min(6,4)=4 ⇒ `:116 key.SegmentIndex >= ceiling` 对 index 4、5 命中 → `:118 SegmentOutcome.No(SegmentCountExceeded)` → `R:208-209 refusals.Add` → `R:214 refusals.Count > 0` ⇒ **Disposition=HistoryRejected**（:217），`R:218 HistoryEntries = appended`＝4 段。所以 @671 首翻行 :677 的预期 Actual＝`HistoryRejected`，**不是工单 §二 m′ 猜的 `Immediate`**（工单那句"家族形状＝波 43 trx-g2 的 Expected: ContractRejected / Actual: Immediate"只是消息族类比；本刀按盘面字节重推为 HistoryRejected，登记为对工单事前论证的更正，见 §八）。
- **n′ 摘门后的下游**：`rejections` 由 1→0 ⇒ R:137 放行 ⇒ R:150 ImmediateBranch：`slice="answered him"`、`PlannedSegmentCount=3` ⇒ ceiling=min(3,4)=3，index 0/1/2 全收（`DeliveryLedger.cs:130` 空文本面不触发，逐段 4/4/4 字符）⇒ 0 refusals ⇒ Disposition=Immediate。@698 首翻行＝:705 `Assert.Single(result.Rejections)`（后随 :706/:707 在同一格里不再执行，xUnit 首失败即止，**同一枚红，不另计**）。
- **夹具默认面**（判据 :40/:57/:82/:89/:102 现文）：`Request()`＝storyVersion 7／ownershipEpoch 1／processedBaseline 100／maxSegments 4、Conversation="room-1"（:18）、Batch="batch-1"（:19）、InstanceId=Alpha（:29）；`Payloads()`＝advisory ""、state ""、recent []、memory []、identity="Pry canon"、CurrentEvent=new NarrativeContextUnit("input:101", …)（:66）；`Draft()`＝actions 默认 `[SilenceAction()]`（:110）、intents 默认空、prose=Prose（:22）；`ImmediateAction()`＝quote="answered him"（在 Prose 里恰现 1 次）、segments 3、target "room-1"；`SilenceAction()`＝PlannedSegmentCount 0；`DeferredAction(intentId)`＝Mode Deferred、segments 0、DeferredIntentId=实参。

以下 §一.4／§一.5＝k′／l′ 闭合表，§一.5′（§一.5 之后编号顺延）＝m′／n′ 闭合表，§一.6＝o′ 的件 3′ 退回登记。**每表 56 行逐枚点名（枚数逐枚点名，禁止"各恰 N"合并写法）；"预期"列＝该枚在该轮突变态下红／不红；表落档先于该轮任何 `dotnet`；跑完不回头改预期列。**

### 一.4 k′ 闭合表（N:43 `if (!Assembly.InstanceId.Equals(Request.InstanceId))`→`if (false)`；见证格＝审计点号 #16＝名册 20 `Call_refuses_an_assembly_that_belongs_to_another_scope(scope:"instance")` @437）

预期红集＝**恰 1：名册 20＝@437[instance]**。断言帧 :447（`Assert.Throws<NarrativeContextAssemblyRejectedException>`），消息面值（按件5 解码后逐字符）＝`Assert.Throws() Failure: No exception was thrown\r\nExpected: typeof(Pry.Core.Narrative.NarrativeContextAssemblyRejectedException)`，性质＝断言级红（消息族盘上先例＝波 41 trx-d 同格另支，已在 §〇.5 件6 逐字符 AGREE）。推导：@442 `"instance"` 支把 `InstanceId` 换成 `ci-instance-beta`，Request 侧＝Alpha（:29/:43）⇒改前 N:43 true→:45 抛；改后 `if (false)` ⇒ 不抛，继续求值 N:50（ConversationId 两侧同 "room-1"：夹具只改 InstanceId，装配体 id 出处 K:171）与 N:57（BatchId 两侧同 "batch-1"：同依据）⇒两支仍为 false ⇒ 走 :64 `return this` ⇒ :447 `Assert.Throws()` 翻成"No exception was thrown"。

| 名册 | 格 | 预期 | 逐枚理由（可达路径级） |
|---|---|---|---|
| 1 | @183 Compiler_emits_the_eight_assembly_units… | 不红 | 体内 compile=1、run=0、validate=0（§A 行 1）：只 `Compile`＋装配面断言，不构造 `NarratorCall` ⇒ N:43 不求值。 |
| 2 | @198 Compiler_keeps_absent_sections… | 不红 | 同 @183：Compile-only，无 NarratorCall 入口。 |
| 3 | @210 Compiler_drops_an_optional_section… | 不红 | Compile-only；断言 State 段 Disposition／BudgetTokens（:217-:219），不经 :43。 |
| 4 | @225 Compiler_refuses_the_round_when_the_mandatory_prelude… | 不红 | Compile-only，断言 MandatoryExceedsBudget 面。 |
| 5 | @237 Compiler_refuses_a_window_that_leaves_no_positive_input_budget | 不红 | Compile-only（WindowTooSmall 面）。 |
| 6 | @247 Compiler_refuses_a_scene_without_identity… | 不红 | Compile-only（MandatoryContentMissing 面；`identity: ""` 在 Payloads 面）。 |
| 7 | @256 Compiler_refuses_a_current_event_from_another_conversation | 不红 | Compile-only；其 "room-2" 在 `Payloads(currentEventConversation:)`（:259），不是 NarratorCall 比较面。 |
| 8 | @266 Compiler_fails_loudly_when_the_reserved_directive… | 不红 | Compile-only（ReservedDirectiveDoesNotFit 面）。 |
| 9 | @281 Compiler_trims_recent_messages… | 不红 | Compile-only（unit_budget 段面）。 |
| 10 | @298 Compiler_removes_cross_conversation_units… | 不红 | Compile-only（cross_scope 段面；读 :294 的 RecentMessages.SourceRefs，与 :43 无关）。 |
| 11 | @313 Compiler_shrinks_the_memory_wallet… | 不红 | Compile-only（Memory 钱包面）。 |
| 12 | @328 Compiler_honours_the_unified_budget_inequality… | 不红 | Compile-only（预算不等式面）。 |
| 13 | @347 Compiler_rejects_a_negative_section_share | 不红 | Compile-only（InvalidBudget 面）。 |
| 14 | @357 Compiler_rejects_a_window_of_zero_tokens | 不红 | Compile-only（InvalidBudget 面）。 |
| 15 | @367 Compiler_carries_source_references… | 不红 | Compile-only；读 EmittedSourceRefs（:372-:375），不经 NarratorCall。 |
| 16 | @381 Round_calls_the_provider_exactly_once… | 不红 | 达 R:105 `.Validate()`，装配体三面 id 出处 K:171＝同 request ⇒ N:43 改前 false、改后仍 false；:387-:390 断言面（Silence／调用一次）不变。 |
| 17 | @394 Round_does_not_call_the_provider_when_the_context_is_rejected | 不红 | R:95 `assembly.Accepted=false` 即返 ContextRejected（:97-:101），R:105 根本不执行 ⇒ N:43 不求值；:401-:404 不变。 |
| 18 | @408 Second_call_on_the_same_round_budget_is_refused | 不红 | 体内只有 `NarratorCallBudget` 两次 `Consume()`（:410-:416），无 NarratorCall／无 RunAsync ⇒ 不到 N:43。 |
| 19 | @420 Call_refuses_an_assembly_that_still_carries_a_rejection | 不红 | 达 N:36（`!Assembly.Accepted`＝rejected 装配体）⇒ 在 N:43 **之前**抛，:430 断言 `error.Reason==WindowTooSmall` 出自 :40 的 ReasonCode 面，本轮未改。 |
| 20 | @437[instance] Call_refuses_an_assembly… | **红（恰此一枚·断言级）** | 表头推导：:442 支换 InstanceId，N:43 被摘后 N:50/N:57 仍 false ⇒ 不抛 ⇒ :447 `Assert.Throws()` 翻，帧 :447，消息＝`Assert.Throws() Failure: No exception was thrown` 族。 |
| 21 | @437[conversation] Call_refuses_an_assembly… | 不红 | :443 支只换 ConversationId="room-2"，InstanceId 仍＝Alpha ⇒ N:43 改前就为 false（同值两态）；改后仍由 :50 抛 ⇒ :447 与 :452（CallScopeMismatch）同场 Passed。正面凭据先例＝波 41 trx-d 同一份 TRX 里另两参数行 Passed。 |
| 22 | @437[batch] Call_refuses_an_assembly… | 不红 | :444 `_ =>` 支只换 BatchId="batch-2"：InstanceId 与 ConversationId 均未改 ⇒ N:43、N:50 两态皆 false，仍由 :57 抛 ⇒ 该参数行 Passed。 |
| 23 | @456 Round_never_rewrites_the_narration… | 不红 | 达 R:105（三面 id 同 request ⇒ N:43 恒 false）；Immediate 走通投递，:462-:467 不变。 |
| 24 | @471 Round_feeds_the_provider… | 不红 | 达 R:105 同上恒 false；断言读 LastCall 装配面（:478-:482），与 N:43 无涉。 |
| 25 | @488 Silence_round_produces_no_assistant_message… | 不红 | R:105 恒 false（同上）；动作 Silence ⇒ R:167 沉默返回面不变。 |
| 26 | @503 Deferred_round_registers_the_intent… | 不红 | R:105 恒 false；R:155 延迟支不变。 |
| 27 | @520 Draft_without_any_action… | 不红 | R:105 恒 false；唯一拒绝出自 V:139 空动作面，本轮未改 ⇒ :526 不变。 |
| 28 | @534 Malformed_response… | 不红 | 达 R:105 恒 false 后在 R:110 短路 ProviderRejected（Malformed），与 N:43 无关。 |
| 29 | @549 A_provider_that_ignores_cancellation… | 不红 | 同 @534 类：R:105 恒 false ⇒ R:110 短路（TimedOut）。 |
| 30 | @562 A_provider_that_honours_the_timeout_token… | 不红 | 同 @534 类（CancelsOnToken ⇒ R:110 短路）。 |
| 31 | @574 Provider_failure_becomes_a_named_error_shape… | 不红 | 同 @534 类（Throws ⇒ R:110 短路 ProviderFailed）。 |
| 32 | @587 Host_cancellation_stays_cancellation… | 不红 | R:105 恒 false ⇒ R:108 `InvokeOnceAsync` 抛 OperationCanceledException（:594 调用点），N:43 不在其路径上。 |
| 33 | @599 Deferred_intent_that_claims_to_be_fulfilled… | 不红 | R:105 恒 false；其唯一拒绝出自 V:150-:152 `NarrativeIntentPolicy`（未改）⇒ :606 不变。 |
| 34 | @614 Deferred_action_without_a_pointing_intent… | 不红 | R:105 恒 false；拒绝出自 V:240（Deferred 支，未改）⇒ :620 不变。 |
| 35 | @628 Immediate_action_binds_to_the_unique_slice… | 不红 | R:105 恒 false；Immediate 三面（V:200/:207/:214）均未改 ⇒ :634-:641 不变。 |
| 36 | @644 Quote_absent_from_this_narration… | 不红 | R:105 恒 false；拒绝出自 V:207-:211（ActionBindingMissing，未改）。 |
| 37 | @658 Ambiguous_quote… | 不红 | R:105 恒 false；拒绝出自 V:207 Bind 二现（未改）。 |
| 38 | @671 Immediate_action_over_the_host_segment_ceiling… | 不红 | R:105 恒 false；拒绝出自 V:200（本轮未改）⇒ :677-:680 不变。 |
| 39 | @684 An_over_split_slice_is_refused_by_the_existing_history_gate | 不红 | R:105 恒 false；HistoryRejected 出自 `DeliveryLedger.cs:115-:121`/TryAppend 面，与 N:43 无关。 |
| 40 | @698 Immediate_action_carrying_a_future_intent… | 不红 | R:105 恒 false；拒绝出自 V:214（本轮未改）⇒ :705 不变。 |
| 41 | @711 Action_targeting_another_conversation… | 不红 | R:105 恒 false；拒绝出自 V:191（未改）。 |
| 42 | @722 Perceived_reference_outside_the_batch… | 不红 | R:105 恒 false；拒绝出自 V:166-:170（未改）。 |
| 43 | @735 Watermark_beyond_the_batch_upper_bound… | 不红 | R:105 恒 false；拒绝出自 V:173-:176（未改）。 |
| 44 | @746 Round_refuses_a_narration_without_prose… | 不红 | R:105 恒 false；两条拒绝出自 V:131＋V:207（均未改）⇒ :752 不变。 |
| 45 | @761 Bound_action_keeps_a_reference_back… | 不红 | R:105 恒 false；断言读绑定偏移与 :772 的 EmittedSourceRefs（装配面），不经 N:43。 |
| 46 | @776 Draft_on_a_stale_story_version… | 不红 | R:105 恒 false 后由 R:121 之 `NarrativeRoundRunner.cs:238` 命中即返（本轮未改 R）⇒ :782-:786 不变。 |
| 47 | @790 Draft_from_a_fenced_writer_epoch… | 不红 | 同 @46 类：R:244 所有权代次命中即返，N:43 恒 false 面不变。 |
| 48 | @802 Draft_with_a_wrong_issue_baseline… | 不红 | 同 @46 类：R:250 签发基线命中即返。 |
| 49 | @813 Contiguous_split_reproduces_the_bound_slice_verbatim | 不红 | 仅调静态 `NarrativeRoundRunner.SplitContiguous`（体内 split=3、run=0）⇒ 不经 R:105／N:43。 |
| 50 | @832 Mixed_round_takes_the_immediate_branch… | 不红 | R:105 恒 false；混合轮 Immediate 支结果由 V/R 未改面决定 ⇒ :838-:843 不变。 |
| 51 | @851 Provider_side_cancellation_with_a_live_host_token… | 不红 | 自建 runner 的 `:860 RunAsync` 同样先过 R:105（id 恒等）⇒ 其后 R:110 短路 TimedOut，N:43 不参与。 |
| 52 | @890 Unified_budget_inequality_is_pinned… | 不红 | 纯记录面（手工构 `NarrativeContextAssembly` 断言 WithinUnifiedBudget），无 NarratorCall／无 RunAsync。 |
| 53 | @913 Whole_section_segments_carry_their_own_source_reference… | 不红 | Compile-only（读 :920/:925 的段 refs）。 |
| 54 | @934 Absent_sections_are_named_not_supplied… | 不红 | Compile-only（not_supplied 理由串面）。 |
| 55 | @952 Unit_run_losing_units_to_both_reasons… | 不红 | Compile-only（cross_scope_and_unit_budget 面）。 |
| 56 | @975 Unit_run_that_fits_no_unit_at_all… | 不红 | Compile-only（读 :987 RecentMessages 段 refs 为空面）。 |

k′ 表合计：红 1（名册 20＝@437[instance]）＋不红 55＝56，与 §〇.4 当场格数同面。

### 一.5 l′ 闭合表（N:57 `if (!string.Equals(Assembly.BatchId, Request.BatchId, StringComparison.Ordinal))`→`if (false)`；见证格＝审计点号 #18＝名册 22 `@437[batch]`）

预期红集＝**恰 1：名册 22＝@437[batch]**。帧 :447，消息面值＝与 k′ 同族 `Assert.Throws() Failure: No exception was thrown\r\nExpected: typeof(Pry.Core.Narrative.NarrativeContextAssemblyRejectedException)`，性质＝断言级红。推导：N:57 是三支里的**末端支**，摘掉它不会让前面任何支多读一次：@444 `_ =>` 支把 BatchId 换成 "batch-2"（Request 侧 "batch-1"，:19/:45），改前 true→:59 抛；改后 `if (false)` ⇒ 不抛 ⇒ 直接 :64 `return this`（其后无支）⇒ :447 翻。`instance`／`conversation` 两行的红不来自 N:57：前者出自 N:43（@442 换 InstanceId，N:43 本轮未改），后者出自 N:50（@443 换 ConversationId="room-2"，N:50 未改），二者都在到达 N:57 之前就已抛出 ⇒ 同场 Passed。

下表只写与 k′ 的**差异面**（可达性判据同 §一.4，落点从 N:43 换成 N:57 后逐枚重推）：

| 名册 | 格 | 预期 | 逐枚理由（可达路径级，按 N:57 重推） |
|---|---|---|---|
| 1〜15 | @183〜@367 十五枚 Compile-only | 不红 | 体内 run=0、validate=0（§A）：不构造 `NarratorCall` ⇒ N:57 不求值。逐枚＝1 @183、2 @198、3 @210、4 @225、5 @237、6 @247、7 @256、8 @266、9 @281、10 @298、11 @313、12 @328、13 @347、14 @357、15 @367（枚枚点名，理由同一句盘面事实：无 NarratorCall 入口）。 |
| 16 | @381 Round_calls_the_provider_exactly_once… | 不红 | 达 R:105；BatchId 出处 K:171＝request.BatchId ⇒ N:57 两态恒 false ⇒ :387-:390 不变。 |
| 17 | @394 Round_does_not_call_the_provider… | 不红 | R:95 即返 ContextRejected，R:105 不发生。 |
| 18 | @408 Second_call_on_the_same_round_budget… | 不红 | 无 NarratorCall 构造（只有 Budget）⇒ 不到 N:57。 |
| 19 | @420 Call_refuses_an_assembly_that_still_carries_a_rejection | 不红 | 抛点在 N:36（Accepted=false），**在 N:57 之前**，本轮未改 :36 ⇒ :430 的 WindowTooSmall 不变。 |
| 20 | @437[instance] Call_refuses_an_assembly… | 不红 | :442 换 InstanceId ⇒ N:43 仍 true 先抛（本轮未改 N:43）；该行 BatchId 两侧同值，N:57 摘否都与它无关 ⇒ Passed。 |
| 21 | @437[conversation] Call_refuses_an_assembly… | 不红 | :443 换 ConversationId ⇒ N:43 false、N:50 true 先抛（未改）；BatchId 两侧同 "batch-1" ⇒ Passed。 |
| 22 | @437[batch] Call_refuses_an_assembly… | **红（恰此一枚·断言级）** | 表头推导：N:57 被摘 ⇒ 三支全 false ⇒ `return this` ⇒ :447 翻，帧 :447，消息＝Throws 族。 |
| 23〜51 | @456／@471／@488／@503／@520／@534／@549／@562／@574／@587／@599／@614／@628／@644／@658／@671／@684／@698／@711／@722／@735／@746／@761／@776／@790／@802／@813／@832／@851 共 29 枚 | 不红 | 逐枚点名见本行枚举：其中 @813（名册 49）体内 run=0 只调静态 `SplitContiguous` ⇒ 不到 N:57；@394 已在名册 17；其余 27 枚都经 R:105，装配体 BatchId 恒等于 request.BatchId（K:171／K:300 两面同形）⇒ N:57 改前改后均为 false，各格断言面由 V/R 未改门决定。枚枚为：23 @456、24 @471、25 @488、26 @503、27 @520、28 @534、29 @549、30 @562、31 @574、32 @587、33 @599、34 @614、35 @628、36 @644、37 @658、38 @671、39 @684、40 @698、41 @711、42 @722、43 @735、44 @746、45 @761、46 @776、47 @790、48 @802、50 @832、51 @851。 |
| 52 | @890 Unified_budget_inequality… | 不红 | 纯记录面，无 NarratorCall／无 RunAsync。 |
| 53〜56 | @913／@934／@952／@975 四枚 Compile-only | 不红 | 枚枚点名：53 @913 读 :920/:925 段 refs；54 @934 读 not_supplied 串；55 @952 读 cross_scope_and_unit_budget；56 @975 读 :987 空 refs 面 ⇒ 全部不经 N:57。 |

l′ 表合计：红 1（名册 22＝@437[batch]）＋不红 55＝56。（表内 1〜15、23〜51、53〜56 三段是"逐枚点名"的紧凑排版：每一枚的名字与 @ 行都单独列出，理由句按同一盘面事实复用，未使用"各恰 N"合并计数写法。）

### 一.5′ m′ 闭合表（V:200 拆合取右半边→`if (action.PlannedSegmentCount < 1)`；见证格＝审计点号 #34＝名册 38 `Immediate_action_over_the_host_segment_ceiling_is_refused` @671）

预期红集＝**恰 1：名册 38＝@671**。断言帧 :677（`Assert.Equal(NarrativeRoundDisposition.ContractRejected, result.Disposition)`），消息面值（解码后逐字符）＝`Assert.Equal() Failure: Values differ\r\nExpected: ContractRejected\r\nActual:   HistoryRejected`，性质＝断言级红。推导（全部按盘面字节重推，见 §一.3 的"m′ 摘门后的下游"条）：:673 夹具 `ImmediateAction(segments: 6)`，request.MaxOutboundSegments＝4（:41 默认，零枚格传过 `maxSegments:`，§B2）⇒ 改前 `6 > 4` true ⇒ V:202 加 ActionSegmentCountInvalid ⇒ R:137 拦成 ContractRejected，格内 :677/:678/:679/:680 四条同场全绿；改后该行只剩 `< 1` ⇒ `6 < 1` false ⇒ 0 拒绝（:207 绑定成功、:214 null，两条本轮未改）⇒ R:150 ImmediateBranch ⇒ `DeliveryLedger.cs:115` ceiling=min(6,4)=4 ⇒ index 4/5 被拒（:118）⇒ `R:214-:218` Disposition=HistoryRejected ⇒ 首翻 :677。**@671 同格后随三条（:678/:679/:680）在 xUnit 首失败即止后不再执行，不另计枚。**

其余 55 枚为什么不连坐（逐枚点名；判据分三类：〔A〕不达 R:136；〔B〕达 R:136 但体内动作不是 Immediate；〔C〕达 Immediate 支但 `PlannedSegmentCount` 对 `> 4` 两态同为 false）：

| 名册 | 格 | 预期 | 逐枚理由 |
|---|---|---|---|
| 1〜15 | @183/@198/@210/@225/@237/@247/@256/@266/@281/@298/@313/@328/@347/@357/@367 | 不红 | 类〔A〕Compile-only（§A 里 run=0）：不经 `NarrativeRoundRunner` ⇒ V:200 不求值。逐枚列名同上表。 |
| 16 | @381 Round_calls_the_provider… | 不红 | 类〔B〕动作＝Draft() 默认 `[SilenceAction()]`（:110）⇒ V:197 switch 走 :248 沉默支，:199 Immediate 支不进 ⇒ V:200 不求值；PlannedSegmentCount 面（:79＝0）不变。 |
| 17 | @394 Round_does_not_call_the_provider… | 不红 | 类〔A〕R:95 即返 ContextRejected，R:136 不发生。 |
| 18 | @408 Second_call_on_the_same_round_budget… | 不红 | 类〔A〕体内仅 Budget.Consume，无 RunAsync。 |
| 19 | @420 Call_refuses_an_assembly_that_still_carries_a_rejection | 不红 | 类〔A〕直调 `NarratorCall.Validate()`，不经 NarrativeDraftValidator。 |
| 20/21/22 | @437[instance]／[conversation]／[batch] | 不红 | 类〔A〕同上：三枚参数行都只走 NarratorContracts 的作用域门（:43/:50/:57），本轮改的是 V 文件。 |
| 23 | @456 Round_never_rewrites_the_narration… | 不红 | 类〔C〕Immediate＋默认 segments=3 ⇒ 改前 `3>4` false、改后不评右半边，两态同 false；:462-:467（Immediate、3 条历史）不变。 |
| 24 | @471 Round_feeds_the_provider… | 不红 | 类〔B〕默认 Silence；且断言读装配面。 |
| 25 | @488 Silence_round… | 不红 | 类〔B〕`[SilenceAction()]`（:490）。 |
| 26 | @503 Deferred_round… | 不红 | 类〔B〕`DeferredAction("intent-1")`（:505）⇒ V:222 Deferred 支，V:200 不求值。 |
| 27 | @520 Draft_without_any_action… | 不红 | 类〔A〕`actions=[]`（§A 里 imm=0/def=0/sil=0）⇒ V:145 循环空转，V:200 不求值；唯一拒绝仍出自 V:139。 |
| 28〜31 | @534/@549/@562/@574 四枚 provider 形状格 | 不红 | 类〔A〕R:110 非 DraftProduced 即返 ProviderRejected，R:136 不发生（逐枚：28 Malformed、29 HangsPastTimeout、30 CancelsOnToken、31 Throws）。 |
| 32 | @587 Host_cancellation_stays_cancellation… | 不红 | 类〔A〕R:108 抛 OperationCanceledException，R:136 不发生。 |
| 33 | @599 Deferred_intent_that_claims… | 不红 | 类〔B〕Deferred 动作（:601）；其拒绝出自 V:150-:152 IntentPolicy（未改）⇒ :606-:610 不变。 |
| 34 | @614 Deferred_action_without_a_pointing_intent… | 不红 | 类〔B〕Deferred（:616）；拒绝出自 V:240（未改）。 |
| 35 | @628 Immediate_action_binds_to_the_unique_slice… | 不红 | 类〔C〕Immediate segments=3（:630 默认）⇒ 右半边两态 false；:634-:641 的三段投递面不变。 |
| 36 | @644 Quote_absent_from_this_narration… | 不红 | 类〔C→仍由绑定拒绝〕Immediate segments=3；拒绝出自 V:207-:211（ActionBindingMissing，未改）⇒ :650-:654 不变。 |
| 37 | @658 Ambiguous_quote… | 不红 | 类〔C〕Immediate segments=3（:661）；拒绝出自 V:207 Bind 二现（未改）。 |
| 38 | @671 Immediate_action_over_the_host_segment_ceiling… | **红（恰此一枚·断言级）** | 表头推导，首翻 :677。 |
| 39 | @684 An_over_split_slice… | 不红 | 类〔C〕Immediate `segments: 4`（:686）＝恰等上限 ⇒ 改前 `4 > 4` false、改后不评右半边 ⇒ V:200 两态同 0 拒绝；该格红与否全在 ledger 面（`DeliveryLedger.cs:115` ceiling=min(4,4)=4，其 SegmentStickerPayloadInvalid 出自 :130-:133 的空文本面，本轮未改）⇒ :690-:694 逐条不变。**工单点名的 @684 面已在此闭合。** |
| 40 | @698 Immediate_action_carrying_a_future_intent… | 不红 | 类〔C〕Immediate segments=3（:700 只加 DeferredIntentId）⇒ V:200 两态 false；其唯一拒绝出自 V:214（本轮未改）⇒ :705-:707 不变。 |
| 41 | @711 Action_targeting_another_conversation… | 不红 | 类〔C→仍由会话门拒绝〕Immediate segments=3、target "room-2"（:713）⇒ 拒绝出自 V:191（未改）。 |
| 42 | @722 Perceived_reference_outside_the_batch… | 不红 | 类〔B〕Silence（:724）；拒绝出自 V:166-:170。 |
| 43 | @735 Watermark_beyond_the_batch_upper_bound… | 不红 | 类〔B〕Silence（:737）；拒绝出自 V:173-:176。 |
| 44 | @746 Round_refuses_a_narration_without_prose… | 不红 | 类〔C〕Immediate segments=3（:748，prose="   "）⇒ V:200 两态 false；两条拒绝出自 V:131＋V:207（均未改）⇒ :752 的 2 枚计数不变。 |
| 45 | @761 Bound_action_keeps_a_reference_back… | 不红 | 类〔C〕Immediate segments=3（:763）⇒ 右半边两态 false；:767-:772 的绑定与装配面不变。 |
| 46 | @776 Draft_on_a_stale_story_version… | 不红 | 类〔A〕R:121 之 R:238 命中即返，**R:136 根本不发生** ⇒ V:200 不求值。 |
| 47 | @790 Draft_from_a_fenced_writer_epoch… | 不红 | 类〔A〕R:121 之 R:244 命中即返。 |
| 48 | @802 Draft_with_a_wrong_issue_baseline… | 不红 | 类〔A〕R:121 之 R:250 命中即返。 |
| 49 | @813 Contiguous_split_reproduces_the_bound_slice_verbatim | 不红 | 类〔A〕只调静态 `SplitContiguous`，无 RunAsync。 |
| 50 | @832 Mixed_round_takes_the_immediate_branch… | 不红 | 类〔C〕Immediate segments=3＋Deferred 一枚（:834）⇒ Immediate 那枚右半边两态 false，Deferred 那枚走 V:223/:229/:235/:240（未改）⇒ :838-:843 不变。 |
| 51 | @851 Provider_side_cancellation_with_a_live_host_token… | 不红 | 类〔A〕自建 provider ⇒ R:110 短路 TimedOut，R:136 不发生。 |
| 52 | @890 Unified_budget_inequality… | 不红 | 类〔A〕纯记录面。 |
| 53〜56 | @913/@934/@952/@975 | 不红 | 类〔A〕Compile-only，枚枚点名（53 读段 refs、54 读 not_supplied、55 读双理由串、56 读空 refs）。 |

m′ 表合计：红 1（名册 38＝@671）＋不红 55＝56。同函数下游 `:207`／`:214` 面已在类〔C〕逐枚点名：V:200 只是往 `rejections` 里 Add（非早退），摘右半边不改变 :207/:214 的求值次数，只可能改变 :137 的计数——两态计数不同的格仅 @671 一枚。

### 一.5″ n′ 闭合表（V:214 `if (action.DeferredIntentId is not null)`→`if (false)`；见证格＝审计点号 #36＝名册 40 `Immediate_action_carrying_a_future_intent_is_refused` @698）

预期红集＝**恰 1：名册 40＝@698**。断言帧 :705（`Assert.Single(result.Rejections)`），消息面值＝`Assert.Single() Failure: The collection was empty`（该族盘上先例＝波 41 trx-e、波 42 trx-g/h/i/j、波 43 trx-i2，已在 §〇.5 件6 逐字符 AGREE），性质＝断言级红。推导：:700 `ImmediateAction() with { DeferredIntentId = "intent-9" }`（Mode 仍 Immediate）⇒ 改前 V:214 true ⇒ V:216 加 IntentClaimedAsFact（该格唯一拒绝：segments=3 不越界、target=Conversation、绑定成功）⇒ :705 恰 1 绿；改后 `if (false)` ⇒ 0 拒绝 ⇒ R:137 放行 ⇒ R:150 ImmediateBranch 三面（ceiling=min(3,4)=3，逐段非空）⇒ Disposition=Immediate、Rejections 空 ⇒ :705 翻。后随 :706（读 `Rejections[0].Reason`）与 :707（读 Ledger）**在同一格里不再执行**（xUnit 首失败即止；且 :706 若执行会是整格异常红，本轮不必假定它，见 §六.5），同一格仍只算 1 枚红。

其余 55 枚（逐枚点名；判据类〔A〕不达 R:136、〔B〕达但动作非 Immediate 支（V:214 在 `case CommunicationMode.Immediate:` 体内，:199-:220）、〔C〕Immediate 但 `DeferredIntentId` 为 null ⇒ 两态同 false）：

| 名册 | 格 | 预期 | 逐枚理由 |
|---|---|---|---|
| 1〜15 | @183/@198/@210/@225/@237/@247/@256/@266/@281/@298/@313/@328/@347/@357/@367 | 不红 | 〔A〕Compile-only（§A run=0）：不经 runner ⇒ V:214 不求值。 |
| 16 | @381 Round_calls_the_provider… | 不红 | 〔B〕默认 SilenceAction（:110/:76-:80，DeferredIntentId 未置）。 |
| 17 | @394 Round_does_not_call_the_provider… | 不红 | 〔A〕R:95 即返。 |
| 18 | @408 Second_call_on_the_same_round_budget… | 不红 | 〔A〕无 RunAsync。 |
| 19 | @420 Call_refuses_an_assembly_that_still_carries_a_rejection | 不红 | 〔A〕直调 NarratorCall.Validate，不经 NarrativeDraftValidator。 |
| 20/21/22 | @437 三枚参数行 | 不红 | 〔A〕同上。 |
| 23 | @456 Round_never_rewrites_the_narration… | 不红 | 〔C〕ImmediateAction()（:458）不带 DeferredIntentId（:82-:87 形参面里该字段未置，record 默认 null）⇒ 两态 false。 |
| 24 | @471 Round_feeds_the_provider… | 不红 | 〔B〕默认 Silence。 |
| 25 | @488 Silence_round… | 不红 | 〔B〕Silence（:490）。 |
| 26 | @503 Deferred_round… | 不红 | 〔B〕DeferredAction（:505）⇒ 走 V:235/:240，V:214 在其后且属 Immediate 支，不求值。**工单 §〇 提醒的"Deferred 支不被多读"面在此闭合。** |
| 27 | @520 Draft_without_any_action… | 不红 | 〔A→实为无动作〕`actions=[]` ⇒ CheckAction 不进入。 |
| 28〜31 | @534/@549/@562/@574 | 不红 | 〔A〕R:110 短路 ProviderRejected。 |
| 32 | @587 Host_cancellation… | 不红 | 〔A〕R:108 抛出。 |
| 33 | @599 Deferred_intent_that_claims… | 不红 | 〔B〕Deferred（:601）；拒绝出自 IntentPolicy（V:150-:152，未改）。 |
| 34 | @614 Deferred_action_without_a_pointing_intent… | 不红 | 〔B〕Deferred（:616）；拒绝出自 V:240（未改）。 |
| 35 | @628 Immediate_action_binds_to_the_unique_slice… | 不红 | 〔C〕Immediate 不带意图（:630）⇒ V:214 两态 false；:634-:641 不变。 |
| 36 | @644 Quote_absent… | 不红 | 〔C〕Immediate（:646）不带 DeferredIntentId；拒绝出自 V:207-:211（未改）。 |
| 37 | @658 Ambiguous_quote… | 不红 | 〔C〕Immediate（:661）不带；拒绝出自 V:207 Bind 二现（未改）。 |
| 38 | @671 Immediate_action_over_the_host_segment_ceiling… | 不红 | 〔C〕Immediate（:673）不带 DeferredIntentId；拒绝出自 V:200（本轮未改）⇒ :677-:680 不变。 |
| 39 | @684 An_over_split_slice… | 不红 | 〔C〕Immediate（:686）不带；HistoryRejected 出自 ledger（未改）。 |
| 40 | @698 Immediate_action_carrying_a_future_intent… | **红（恰此一枚·断言级）** | 表头推导，首翻 :705。 |
| 41 | @711 Action_targeting_another_conversation… | 不红 | 〔C〕Immediate（:713）不带；拒绝出自 V:191（未改）。 |
| 42 | @722 Perceived_reference_outside_the_batch… | 不红 | 〔B〕Silence（:724）。 |
| 43 | @735 Watermark_beyond_the_batch_upper_bound… | 不红 | 〔B〕Silence（:737）。 |
| 44 | @746 Round_refuses_a_narration_without_prose… | 不红 | 〔C〕Immediate（:748）不带；两条拒绝出自 V:131＋V:207（均未改）⇒ :752 的 2 不变。 |
| 45 | @761 Bound_action_keeps_a_reference_back… | 不红 | 〔C〕Immediate（:763）不带；:767-:772 不变。 |
| 46 | @776 Draft_on_a_stale_story_version… | 不红 | 〔A〕R:238 命中即返，R:136 不发生（虽其动作是 Immediate，:778 不带意图）。 |
| 47 | @790 Draft_from_a_fenced_writer_epoch… | 不红 | 〔A〕R:244 命中即返。 |
| 48 | @802 Draft_with_a_wrong_issue_baseline… | 不红 | 〔A〕R:250 命中即返。 |
| 49 | @813 Contiguous_split… | 不红 | 〔A〕静态切片面。 |
| 50 | @832 Mixed_round_takes_the_immediate_branch… | 不红 | 〔C＋B〕Immediate 那枚不带 DeferredIntentId（:834），Deferred 那枚走 Deferred 支（V:235/:240 未改）⇒ 两态拒绝数同为 0，:838-:843 不变。 |
| 51 | @851 Provider_side_cancellation… | 不红 | 〔A〕R:110 短路。 |
| 52 | @890 Unified_budget_inequality… | 不红 | 〔A〕纯记录面。 |
| 53〜56 | @913/@934/@952/@975 | 不红 | 〔A〕Compile-only。 |

n′ 表合计：红 1（名册 40＝@698）＋不红 55＝56。

### 一.6 o′＝件 3′ 退回轮（K:99 值面清空 `SourceRefs = [payloads.IdentitySourceRef, payloads.CurrentEvent.SourceRef],`→`SourceRefs = [],`；见证格＝审计点号 #48＝名册 15 @367）——闭合不了，不起轮、定向额度原样退回

工单 §二 o′ 的起跑条件是：`:772` 那条必须由 lane 自己在 blob 上取到近聊段 refs 的出处才算闭合，取不到就按件 3′ 退回。本刀在 blob 上取到的事实与该条件相反：

- 读 `:772` 的格＝名册 45＝@761 `Bound_action_keeps_a_reference_back_to_the_input_it_answers`，其装配输入＝:765 的 `Payloads()`（**无参默认面**：advisory=""、recent=[]、memory=[]、state=""，见判据 :57-:74），体内 `srcRefs=1`（§A 行 45）。
- 该装配态下 `EmittedSourceRefs`（K:440＝所有段 `SourceRefs` 的 SelectMany）的出处只有 Identity 段的 K:99：Advisory 走 :136-:140 的 `NotSupplied("Advisory", …)`（:280-:286 不设 SourceRefs ⇒ record 默认 `[]`，:409），RecentMessages／Memory 的 `AddUnitRun`（:238-:276）在 units 为空时 refs 为空，其余节走 `NotSupplied`（:195-:198）。正面旁证＝判据 @198 那格自己断言"默认 Payloads 下恰 1 枚 Included 段且其 Name=="Identity""（:204-:206）。
- ⇒ 改前该格的 `"input:101"` 恰由 K:99 的第二项 `payloads.CurrentEvent.SourceRef`（:66 夹具＝`new NarrativeContextUnit("input:101", …)`）提供；K:99 清空后 `EmittedSourceRefs` 变为空表 ⇒ :772 `Assert.Contains("input:101", …)` 也翻。
- ⇒ o′ 的预期红集在盘面上是**两枚**：名册 15（@367，首翻 :372 的 `Expected: 4 / Actual: 2`）＋名册 45（@761，首翻 :772 的 Contains 族）。工单 §二 判定门＝每轮**恰 1**，这张表闭合不成"恰 1"，且不存在"形状不同仍只算 1 枚"的合并写法（件 9 已明令禁止）。

处置（按件 3′ 与工单 §二 o′ 的自述出口）：**不起 o′ 轮、不动字节、不施加突变**，该轮定向额度原样退回（`退回 ≠ 作废`，不占用作废额度，也不消解 #48 的欠账）。#48 需要的新配方＝"只被 @367 走"的值面突变，或先把 @761 那枚的 refs 出处从 K:99 迁开（后者属判据字节，本刀权限外）。未领账登记见 §七.9。

### 一.7 起跑计划（依 §一.6 退回后的形状）

固定跑序 k′→l′→m′→n′（o′ 退回不起），每轮＝预检（P125 起）→件8 落档命令→施加→class-scoped 定向跑→立刻数 `.trx`→读数→还原→核 `status` 行数与三面 SHA。§四.2 是硬门：**四轮＋退回轮的完整条件（"五轮全部起跑且全部有效"）不成立 ⇒ 收尾全量 F 不起**，构建合法跳过（§〇.6）。`dotnet` 预算＝定向 ≤5（本刀动用 4）、全量 ≤1（动用 0）、解决方案级构建 ≤1（动用 0）、总调用 ≤6（动用 4）。

---

## 二 逐轮读数（每轮跑完即落盘，不攒到最后一次写）

### 二.1 轮 k′＝N:43 实例面（波 22 审计 §四.1 T3 点号 #16）——有效，恰 1 枚红

本轮十一项（全部取自盘面产物，读数工具＝`node w44-trx2.js read out/trx-k/result-k.trx`，其件10 自证面同场打印 `SELFPROOF-VERDICT=ALL-AGREE`，16 枚定向面＋2 枚全量面逐一对账通过）：

1. **预检档**：`.local-notes/wave44-1029/out/preflight-P125.txt`（467 字节，整段逐字存档；`FOREGROUND: DeepSeek Harness pid=43268`、`gpu_util=19%`、`free_memory_gb=10.7`、**`VERDICT=BUILD_OK`**）。本刀预检编号自 P125 起，P124b 属门房、不入本刀账。
2. **命令档＋凭据数**：`out/cmd-k.txt`（507 字节，件8 于起跑前落档；`CMD-VERDICT=OK`，九项字符校验含 `--logger "trx;…"` 与 `--results-directory` 均在位；命令行 sha256=`b7f947cbeede6b9a…39caee`，265 字节）。跑完立刻数该目录：`out/trx-k/` 内 `.trx` 枚数＝**1**（`result-k.trx`，84667 字节）⇒ 有凭据，不进入"本轮无凭据"登记。
3. **施加前 SHA**：`3a33a8151e61c3396963dac451dffcb46acb2f02807f140d18f694d6a52a9f8c`（5536 字节）＝`git show 51f149d:src/Pry.Core/Narrative/NarratorContracts.cs` 的 SHA（G4 逐字符相等＝OK）。
4. **施加后 SHA**：`4b5ea7a0734558ba7bc9ed4d9c9f2fbca5ca1624ab8da5894d2ba5ff39132c0a`（5494 字节）＝件2 预合成那枚（G2 逐字节相等＝OK；G3：OLD 整行逐字符出现次数＝1、第 43 行逐字符等值＝OK；G5 写后与声明值相等＝OK）。改的行＝`        if (false)`，被换掉的行＝`        if (!Assembly.InstanceId.Equals(Request.InstanceId))`。通道＝`node w44-apply.js apply k`（六道守卫全绿，`RESULT=APPLY_OK`）。
5. **本轮计数（TRX `<Counters>` 真形）**：`total=56 executed=56 passed=55 failed=1 notExecuted=0`。**`total` 56 ＝本刀 §〇.2 自数方格数**（不是工单面值）。`NotExecuted` 开标签枚数＝0；`Failed` 开标签枚数＝1，与 `failed=1` 对账一致（`ResultSummary outcome="Failed"` 不算用例）。
6. **红面（名＋帧行＋解码后消息）**：`Pry.Core.Tests.NarratorPrototypeTests.Call_refuses_an_assembly_that_belongs_to_another_scope(scope: "instance")`；栈帧唯一锚＝`NarratorPrototypeTests.cs:line 447`（assertion-level，非载入级）；消息解码后逐字＝`Assert.Throws() Failure: No exception was thrown\r\nExpected: typeof(Pry.Core.Narrative.NarrativeContextAssemblyRejectedException)`。同方法另两参数行（`conversation`／`batch`）同场 **Passed**（各 55 枚 Passed 名之一）⇒ 只翻 I 支，与 §一.3 闭合表首行一致。
7. **还原后 SHA**：`3a33a8151e61c3396963dac451dffcb46acb2f02807f140d18f694d6a52a9f8c`（5536 字节）＝blob 原值，逐字节相等（G6＝OK，`RESULT=REVERT_OK`；还原字节源＝`git show 51f149d:…`，未用 `checkout`/`restore`）。
8. **载体 `git status --short` 行数**：施加后＝**1** 行且为 `M src/Pry.Core/Narrative/NarratorContracts.cs`（`tests/**` 行枚数＝0）；还原后＝**0** 行。
9. **载体 HEAD**：施加前后与还原后均＝`51f149d0b722e28a6566c93d19f48a94ca4c85df`，**未移动**（本刀无任何提交类动作）。
10. **判据文件三面**：`tests/Pry.Core.Tests/NarratorPrototypeTests.cs` 本轮未写一字节；跑前后 SHA 均＝基线 `4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`（46491 字节，CR＝0），名册行数 990、`Assert.Skip` 枚数 0 不变。
11. **轮判定**：**有效轮**（单红门＝恰 1；凭据＝TRX；`total` 与自数方格相符；三面与 HEAD 未漂）⇒ T3 点号 **#16**（`NarratorCall.Validate` 实例面逐轮比对）**已由本轮红证领下**：把 N:43 的实例比对摘掉后，唯一被点的正是"跨实例组装"那格，且它翻的是"该抛没抛"，不是别的面。

时间线配对（同一轮内先后关系由 mtime 支撑）：预检 06:26:23Z → 命令档 06:28:04Z → 施加 06:29:26Z → `src/Pry.Core/bin/Release/net10.0/Pry.Core.dll` 06:30:07Z（本轮因源码变更被重编）→ `result-k.trx` 06:30:09Z → 还原 06:31:26Z。`tests/…/bin/Release/net10.0/Pry.Core.Tests.dll` mtime＝04:52:20Z（本轮未重编，因判据字节未动）。

盘面状态注记（§一.7 口径）：还原后盘上的 `Pry.Core.dll` 仍是**本轮突变体编出的那枚**（06:30:07Z），本刀按预算跳过构建、未再编；故本刀只把它算作"成品在盘"，**不把它当作"与基线源码逐字节对应的成品面"**，也不以它支撑任何"现状"结论。

对 §一.3 闭合表的核对：预测红集＝{名册 20＝@437 `[InlineData("instance")]`}，预测不红＝其余 55；盘面实得＝{`…(scope: "instance")` 帧行 447}，failed=1、passed=55 ⇒ **逐格吻合，无级联、无多翻**，本轮不判作废。

### 二.2 轮 l′＝N:57 批次面（点号 #18）——有效，恰 1 枚红

1. **预检档**：`out/preflight-P126.txt`（466 字节，整段逐字存档；`gpu_util=0%`、`free_memory_gb=11.9`、**`VERDICT=BUILD_OK`**）。
2. **命令档＋凭据数**：`out/cmd-l.txt`（507 字节，起跑前落档，`CMD-VERDICT=OK`，九项字符校验全绿；命令行 sha256=`fa913d55193e8731…25e9e1`，265 字节，仅 `LogFileName`/`trx-l` 与 k 轮不同）。跑完立刻数：`out/trx-l/` 内 `.trx` 枚数＝**1**（`result-l.trx`，84576 字节）⇒ 有凭据。
3. **施加前 SHA**：`3a33a815…a9f8c`（5536 字节）＝blob（G4＝OK；同时反证 k 轮还原干净——工作副本又回到基线字节）。
4. **施加后 SHA**：`3ed287620ec893abfa3070245ffba39730a967231902e39d9045bc42f170bd94`（5466 字节）＝件2 预合成那枚（G2/G5＝OK；G3：OLD 整行出现次数＝1、第 57 行逐字符等值＝OK）。改的行＝`        if (false)`，被换掉的行＝`        if (!string.Equals(Assembly.BatchId, Request.BatchId, StringComparison.Ordinal))`。`node w44-apply.js apply l` → `RESULT=APPLY_OK`。
5. **本轮计数**：`total=56 executed=56 passed=55 failed=1 notExecuted=0`；`total` 与 §〇.2 自数方格数 56 相符；`Failed` 开标签＝1 与 `failed=1` 对账一致；`NotExecuted` 开标签＝0。
6. **红面**：`Pry.Core.Tests.NarratorPrototypeTests.Call_refuses_an_assembly_that_belongs_to_another_scope(scope: "batch")`；唯一栈锚＝`NarratorPrototypeTests.cs:line 447`（assertion-level）；消息解码后逐字＝`Assert.Throws() Failure: No exception was thrown\r\nExpected: typeof(Pry.Core.Narrative.NarrativeContextAssemblyRejectedException)`。同方法 `instance`／`conversation` 两参数行同场 Passed ⇒ 只翻批次支，与 §一.3 的 l′ 逐格表一致（`conversation` 面由 N:50 未改的行守着，`instance` 面由 N:43 未改的行守着）。
7. **还原后 SHA**：`3a33a815…a9f8c`（5536 字节）＝blob，逐字节相等（G6＝OK，`RESULT=REVERT_OK`）。
8. **载体 `status --short` 行数**：施加后 1 行（`M src/Pry.Core/Narrative/NarratorContracts.cs`，`tests/**`＝0）；还原后 0 行。
9. **载体 HEAD**：`51f149d0b722e28a6566c93d19f48a94ca4c85df` 未移动。
10. **判据文件三面**：本轮**未**当场单独打印判据 SHA。替代面＝工单 §五.3 明确认可的那面：`out/revert-l.txt` 打印 carrier `status --short`＝0 行（`tests/**` 行枚数＝0）＋HEAD＝`51f149d…` 未动 ⇒ 载体受控文件（含判据）与 blob 同值。直接打印面共三处：入刀（§〇.3，起跑门）、k 轮前后、n 轮后收盘（`out/inventory-final.txt` §5），三处逐字符＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`、46491 字节、CR＝0、990 行、`Assert.Skip`＝0。
11. **轮判定**：**有效轮** ⇒ T3 点号 **#18**（`NarratorCall.Validate` 批次面逐轮比对）**已由本轮红证领下**。

时间线配对：预检 06:34:10Z → 命令档 06:34:15Z → 施加 06:34:15Z → `Pry.Core.dll` 06:34:55Z（本轮重编，说明跑的是 l 突变体二进制）→ `result-l.trx` 06:34:57Z → 还原 06:36:11Z。判据程序集 dll mtime 仍＝04:52:20Z（未重编）。

对 §一.3 闭合表的核对：预测红集＝{名册 22＝@437 `[InlineData("batch")]`}；实得＝同格同帧行，failed=1/passed=55 ⇒ 逐格吻合，无级联，不判作废。

### 二.3 轮 m′＝V:200 上界半（点号 #34，连词只摘右半边）——有效，恰 1 枚红

1. **预检档**：`out/preflight-P127.txt`（466 字节，整段逐字存档；`gpu_util=0%`、`free_memory_gb=11.9`、**`VERDICT=BUILD_OK`**）。
2. **命令档＋凭据数**：`out/cmd-m.txt`（507 字节，起跑前落档，`CMD-VERDICT=OK`；命令行 sha256=`36ac3b43efda65a0…57e6c1`，265 字节）。跑完立刻数：`out/trx-m/` 内 `.trx` 枚数＝**1**（`result-m.trx`，84129 字节）。
3. **施加前 SHA**：`88ebf5c3e3c607e57825a5aa0c4868ec7704e36c360ed4f652706e06ac30b00c`（19338 字节）＝blob（G4＝OK）。
4. **施加后 SHA**：`6c5ec1e9618ecc47c65b955447e31d4a982d3e7a147c5b882c0f902b2cbbbecf`（19278 字节）＝件2 那枚（G2/G5＝OK；G3：OLD 整行出现次数＝1、第 200 行逐字符等值＝OK）。改：`                if (action.PlannedSegmentCount < 1 || action.PlannedSegmentCount > request.MaxOutboundSegments)` → `                if (action.PlannedSegmentCount < 1)`（只摘上界半，下界半仍在）。`RESULT=APPLY_OK`。
5. **本轮计数**：`total=56 executed=56 passed=55 failed=1 notExecuted=0`；`total` ＝自数方格 56；`Failed` 开标签＝1 ↔ `failed=1`；`NotExecuted`＝0。
6. **红面**：`Pry.Core.Tests.NarratorPrototypeTests.Immediate_action_over_the_host_segment_ceiling_is_refused`；唯一锚＝`NarratorPrototypeTests.cs:line 677`（assertion-level）；消息解码后逐字＝`Assert.Equal() Failure: Values differ\r\nExpected: ContractRejected\r\nActual:   HistoryRejected`。**这一枚的 Actual 面与工单 §二 事前面值（`Actual: Immediate`）不同，与本刀 §一.4 依盘面重推的预测逐字符相同**——即"上界半摘掉后契约校验不再产拒绝，改由 `DeliveryLedger.cs:115` 的封顶切片把多出的段裁掉，Round 走 `R:214-:218` 的 HistoryRejected 支"。工单面值的错位登记进 §八 新口径。
7. **还原后 SHA**：`88ebf5c3…30b00c`（19338 字节）＝blob，逐字节相等（G6＝OK，`RESULT=REVERT_OK`）。
8. **载体 `status --short` 行数**：施加后 1 行（`M src/Pry.Core/Narrative/NarrativeDraftValidator.cs`，`tests/**`＝0）；还原后 0 行。
9. **载体 HEAD**：`51f149d…` 未移动。
10. **判据文件三面**：本轮**未**当场单独打印判据 SHA。替代面＝`out/revert-m.txt`：carrier `status --short`＝0 行（`tests/**`＝0）＋HEAD 未动 ⇒ 判据与 blob 同值（工单 §五.3 口径）。
11. **轮判定**：**有效轮** ⇒ T3 点号 **#34**（Immediate 段数上界）**已由本轮红证领下**，且领的方式是"下界仍在、只摘上界"，因此这枚红同时把 §一.4 里那条对工单的修正验成盘面事实。

时间线配对：预检 06:37:10Z → 命令档 06:37:13.254Z → 施加 06:37:13.318Z → `Pry.Core.dll` 06:37:53Z（本轮重编）→ `result-m.trx` 06:37:54Z → 还原 06:38:32.690Z（`out/revert-m.txt` 自打时刻）。

对 §一.4 闭合表的核对：预测红集＝{名册 38＝@671}，且点名"名册 39＝@684（越界切片那格）不翻——它走 `segments: 4` 不超上限"、"名册 15/45 等 Compile-only 与 Silence 格不翻"；实得 failed＝1、passed＝55，红名与帧行逐字符＝预测 ⇒ 无级联，不判作废。

### 二.4 轮 n′＝V:214 半（点号 #36，Immediate 支里"把未来意图当既成事实"那道口）——有效，恰 1 枚红

1. **预检档**：`out/preflight-P128.txt`（466 字节，整段逐字存档；**`VERDICT=BUILD_OK`**）。本刀预检用到 P128 为止，P124b 未入账。
2. **命令档＋凭据数**：`out/cmd-n.txt`（507 字节，起跑前落档，`CMD-VERDICT=OK`；命令行 sha256=`e5a8f2144142697b…f11709`，265 字节）。跑完立刻数：`out/trx-n/` 内 `.trx` 枚数＝**1**（`result-n.trx`，83952 字节）。
3. **施加前 SHA**：`88ebf5c3…30b00c`（19338 字节）＝blob（G4＝OK，反证 m 轮还原干净）。
4. **施加后 SHA**：`c2657962758c35a77befe2f7fc5c33b7c4f7c1245df0e98d9e4d185d37e7633b`（19308 字节）＝件2 那枚（G2/G5＝OK；G3：OLD 整行出现次数＝1、第 214 行逐字符等值＝OK）。改：`                if (action.DeferredIntentId is not null)` → `                if (false)`，其下 :216 的 `Add(IntentClaimedAsFact)` 成为不可达码。`RESULT=APPLY_OK`。
5. **本轮计数**：`total=56 executed=56 passed=55 failed=1 notExecuted=0`；`Failed` 开标签＝1 ↔ `failed=1`；`NotExecuted`＝0。
6. **红面**：`Pry.Core.Tests.NarratorPrototypeTests.Immediate_action_carrying_a_future_intent_is_refused`；唯一锚＝`NarratorPrototypeTests.cs:line 705`（assertion-level，即体内第一条 `Assert.Single(draft.Rejections)`）；消息解码后逐字＝`Assert.Single() Failure: The collection was empty`。构建侧另有一枚 `warning CS0162`（:216 不可达码），它是编译器告警、不是用例，未进 `failed`／`notExecuted` 任何一格——件3 的净效表面已把这一面写在前头。
7. **还原后 SHA**：`88ebf5c3…30b00c`（19338 字节）＝blob（G6＝OK，`RESULT=REVERT_OK`）。
8. **载体 `status --short` 行数**：施加后 1 行（`M src/Pry.Core/Narrative/NarrativeDraftValidator.cs`）；还原后 0 行；`tests/**` 行四轮中出现次数＝**0**。
9. **载体 HEAD**：`51f149d…` 未移动。
10. **判据文件三面**：未写一字节。面＝**两侧都有**：前侧替代面＝`out/revert-m.txt`（status 0 行＋HEAD 未动 ⇒ 与 blob 同值），后侧直接打印面＝n 轮还原后两次当场取（`node` 直取判据字节与 `git show 51f149d` 逐字节对账，`4f1b2f6e…818f4`／46491 字节／CR 0／990 行／`Assert.Skip` 0）＋收盘清点 `out/inventory-final.txt` §5（盘上＝blob，相等＝true）。
11. **轮判定**：**有效轮** ⇒ T3 点号 **#36**（Immediate 动作不得夹带 DeferredIntentId）**已由本轮红证领下**。

时间线配对：预检 06:40:17Z → 命令档 06:40:52.729Z → 施加 06:40:52.791Z（`out/apply-n.txt`）→ `Pry.Core.dll` 06:41:31Z（本轮重编）→ `result-n.trx` 06:41:33Z → 还原 06:42:21.024Z（`out/revert-n.txt`）。

对 §一.5 闭合表的核对：预测红集＝{名册 40＝@698}；点名不红的同族格＝名册 34/@599 与名册 35/@614（走 Deferred 支 V:235/:240，未改）、名册 41/@711 与名册 44/@746（Immediate 但不带意图，拒绝出自 V:191/V:131＋V:207，均未改）、名册 50/@832（混合轮两态拒绝数仍同）；实得 failed＝1、passed＝55，红名＋帧行逐字符＝预测 ⇒ 无级联，不判作废。

---

## 三 收盘终态（原始面＝`out/inventory-final.txt`，2026-10-29T06:45:42Z 当场取；另有 n 轮后单独一次直取）

### 三.1 载体终态四件

- `status --short` 行数＝**0**（空；四轮全部还原干净）。
- `fd93ebe..HEAD` 提交数＝**2**（＝入刀时同一枚，四轮间未新增任何提交）。
- `rev-parse HEAD`＝`51f149d0b722e28a6566c93d19f48a94ca4c85df`（与入刀逐字符相同，HEAD 未动）。
- `git status --short -- tests` 空 ⇒ 判据路径在载体侧零未提交改动（工具代裁面）。

### 三.2 三枚被改产品文件＋判据文件的三面 SHA（盘上／blob(51f149d)／主树工作副本，另附 sha1 blob 面对账）

| 文件 | 盘上 SHA-256 | blob(51f149d) | 主树工作副本 | 三面 | sha1（盘上＝blob） |
| --- | --- | --- | --- | --- | --- |
| `src/Pry.Core/Narrative/NarratorContracts.cs` | `3a33a815…a9f8c`（5536B） | 同值 | 同值 | 等 | `1b76c574…66fde9` |
| `src/Pry.Core/Narrative/NarrativeDraftValidator.cs` | `88ebf5c3…30b00c`（19338B） | 同值 | 同值 | 等 | `44bb9df8…f904c8f` |
| `src/Pry.Core/Narrative/NarrativeContextCompiler.cs` | `86c30160…1d9842`（24457B） | 同值 | 同值 | 等 | `65a3d44a…8ce6e7` |
| `tests/Pry.Core.Tests/NarratorPrototypeTests.cs` | `4f1b2f6e…818f4`（46491B，CR 0，990 行，`Assert.Skip` 0） | 同值 | 同值 | 等 | `de196e28…20bc6d3` |

⇒ 三枚被改文件与判据文件全部回到 `51f149d` 原字节；本刀对判据字节零改动（硬线 3 未破）。主树终态当场读：`git status --short`＝**9 行**＝入刀那 8 行内容面逐字符不变（两枚 `M ` 暂存项、一枚 ` M` 的 `PROJECT_PLAN.md`（`diff --numstat HEAD` 仍＝`18	0`）、五枚 `??`）＋本报告自身新增的那一枚 `??` 行（硬线 1 允许的唯一主树写入）。他人资产一字未读、未改、未提交。

### 三.3 盘上二进制（§一.7 清洁度口径，必须明写）

`carrier/src/Pry.Core/bin/Release/net10.0/Pry.Core.dll`＝1,745,408 字节、mtime 06:41:31Z ⇒ 这枚是 **n′ 轮突变体（V:214→`if (false)`）编出来的那副**；三枚源码字节已回基线，两者**不一致**。本刀按预算未跑解决方案级 `dotnet build`（动用 0），因此**交回时盘上没有与基线源码逐字节对应的成品二进制**，本刀也不把任何 dll 当作"基线源码的成品面"来签（§一.7 口径）。`Pry.Core.Tests.dll` mtime 仍＝04:52:20Z（判据字节未动，四轮都没重编它）。

---

## 四 五枚判定汇总（本刀不裁状态／收口，见硬线 5）

| 轮 | 落点 | 点号 | 判定 | 依据面 |
| --- | --- | --- | --- | --- |
| k′ | `NarratorContracts.cs:43`→`if (false)` | **#16** | **有效·恰 1 红** | `out/trx-k/result-k.trx`（failed=1/passed=55/total=56/notExecuted=0），红名 `…(scope: "instance")` 帧行 :447 |
| l′ | `NarratorContracts.cs:57`→`if (false)` | **#18** | **有效·恰 1 红** | `out/trx-l/result-l.trx`，红名 `…(scope: "batch")` 帧行 :447 |
| m′ | `NarrativeDraftValidator.cs:200` 只摘上界半 | **#34** | **有效·恰 1 红** | `out/trx-m/result-m.trx`，红名 `Immediate_action_over_the_host_segment_ceiling_is_refused` 帧行 :677，Actual 面＝`HistoryRejected`（非工单面值 `Immediate`） |
| n′ | `NarrativeDraftValidator.cs:214`→`if (false)` | **#36** | **有效·恰 1 红** | `out/trx-n/result-n.trx`，红名 `Immediate_action_carrying_a_future_intent_is_refused` 帧行 :705，消息＝`Assert.Single() Failure: The collection was empty` |
| o′ | `NarrativeContextCompiler.cs:99`→`SourceRefs = [],` | #48 | **件3′ 退回（不起轮、不动字节、额度原样退回）** | §一.6：blob 面推得预期红＝名册 15（@367 :372）＋名册 45（@761 :772）两枚，闭合不成"恰 1"；`退回 ≠ 作废`，#48 欠账不减 |

- 作废轮＝**0**（无一轮出现多红／无凭据／还原失败）；退回轮＝**1**（o′）；有效轮＝**4**。
- 领下点号＝**#16／#18／#34／#36**（四枚）；未领＝**#48**（待新配方）；§六.4 那条 #32 的押账本刀未触碰、不减少。
- 口径按 §六.7 写死：四枚红的"定向面恰 1"**不等于**"这枚门只守一格"——m′／n′ 的门与波 14 格子共享消费者，**定向面 1、全场面待定**；本刀未起全量，故全场面一律留 §六。

### 四.2 硬门（工单 §四.2）

"五轮全部起跑且全部有效才起 F"——o′ 退回 ⇒ 条件不成立 ⇒ **收尾全量 F 不起**（动用 0，未用任何 `dotnet test Pry.slnx`）。本刀没有为凑 F 读数多弹防火墙（§六.3 未破）。

---

## 五 预算动用与三方对账

### 五.1 四类预算

| 预算项 | 上限 | 动用 | 说明 |
| --- | --- | --- | --- |
| 解决方案级 `dotnet build` | ≤1 | **0** | §〇.6 三段判定合法跳过；§三.3 已明写盘上二进制＝突变体所编，不作清洁面使用 |
| 解决方案级全量 `dotnet test Pry.slnx` | ≤1 | **0** | §四.2 硬门不过（o′ 退回），不起 F |
| 定向 `dotnet test --filter`（class-scoped） | ≤5 | **4** | k′／l′／m′／n′ 各一次；filter 逐字符＝`FullyQualifiedName~Pry.Core.Tests.NarratorPrototypeTests` |
| `dotnet` 总调用 | ≤6 | **4** | ＝0（build）＋0（F）＋4（定向） |
| HOLD 重跑 | ≤16 | **0** | 四次预检全部 `VERDICT=BUILD_OK`，一次 HOLD 都没遇到 |

### 五.2 三方对账（§四.6，不许省的那一条）

逐份 VERDICT 枚数＝**4**（P125/P126/P127/P128，四份文件里 `VERDICT=BUILD_OK` 各出现 1 次）＝ `dotnet` 实调次数 **4** ＝ 实跑轮数 **4**。三方齐。预检文件未占用 P124b（门房那份额外档 `out/preflight-P124b.txt` 留在目录里，不入本刀账，也不作为任何轮的开跑面）。

### 五.3 每轮 mtime 配对（预检 → 命令档 → 施加 → dll 重编 → `.trx` → 还原，UTC）

| 轮 | preflight | cmd-<轮> | apply | `Pry.Core.dll` | result-<轮>.trx | revert |
| --- | --- | --- | --- | --- | --- | --- |
| k | 06:26:23.712 | 06:28:04.848 | 06:29:26.507 | 06:30:07.746 | 06:30:09.993 | 06:31:26.327 |
| l | 06:34:10.784 | 06:34:15.545 | 06:34:15.599 | 06:34:55.632 | 06:34:57.129 | 06:36:11.821 |
| m | 06:37:10.008 | 06:37:13.254 | 06:37:13.318 | 06:37:53.294 | 06:37:54.778 | 06:38:32.690 |
| n | 06:40:17.041 | 06:40:52.729 | 06:40:52.791 | 06:41:31.900 | 06:41:33.395 | 06:42:21.024 |

每轮都是"预检在前、命令层次之、施加再其次、`.trx` 在重编之后、还原最后"，一一配对无缺项；`result-<轮>.trx` 与 `cmd-<轮>.txt` 的轮名逐字符对应（`LogFileName=result-<轮>.trx`，四枚 sha256 各异：k `b7f947cb…`、l `fa913d55…`、m `36ac3b43…`、n `e5a8f214…`，各 265 字节）。

### 五.4 盘上凭据清点

`.trx` 总数＝**4**（每目录恰 1 枚：`trx-k/` 84667B、`trx-l/` 84576B、`trx-m/` 84129B、`trx-n/` 83952B）；结果目录枚数＝**4**（`out/trx-k|l|m|n`）；`trx-o` 不存在（退回轮不该有凭据目录）；文件名含 `-voided-` 的＝**0** 枚（本刀无作废轮，也就没有留档作废面）；每轮 `total`＝56 ＝ §〇.4 当场格数 56，四枚对账全过（无一枚以工单面值入场）。

---

## 六 未验证面（按 §7 工程标准只登记不虚构）

1. **全板／跨类面**：四枚门在解决方案级全量下会同响多少格——**未验证**（本刀只跑 class-scoped 定向面，F 未起）。m′／n′ 与波 14 格子的同门共享只按盘面消费链推定，未取实测面。
2. **`NarrativeTurnPipeline` 端到端面**：k′／l′ 的 `NarratorCall.Validate()` 在产品里唯一消费点＝`NarrativeRoundRunner.cs:105`（`git grep` 面见 `out/closure-scan.txt` §B10/B11）；流水线侧 `:219` 那枚 `.Validate()` 是 `NarrativeRequest`，不经这两行——该结论取自字节面，未做全量实测。
3. **o′ 值面的三处消费者**：`EmittedSourceRefs`（K:440）、`Segment.SourceRefs`（K:409 默认 `[]`）、`AddUnitRun` 的 refs（K:275）——本刀只证到"默认 `Payloads()` 下 :772 的 `input:101` 唯一出处是 K:99 第二项"，**@367 的 `:375` 两枚同值 refs 在清空后的逐枚形状未验证**（没跑该轮，也不许在 stdout 找）。#48 因此留欠账。
4. **与基线源码逐字节对应的盘上二进制**：未验证，且按 §三.3 已知不成立（盘上那枚编自 n′ 突变体）。下一把刀若沿用"成品在盘即跳构建"的三段判定，**必须先重编或换面**，否则它跑的是本刀 n′ 突变体二进制——这条属当场要交接的风险面，已写进 §七.4。
5. **跨轮顺序效应**：四轮在同一 dll 目录上先后覆盖重编，每轮 `.trx` 与该轮 dll mtime 一一配对（§五.3），但"若换序是否仍恰 1"未验证。
6. **主树他人资产**：`M ` 两枚与 ` M`（`18/0`）一枚、`??` 若干，本刀未读内容、未改、未提交；其对判定文件的影响＝未验证（不在本刀范围）。

---

## 七 交回守门人核的事（工单 §五 九项，逐件给当场读数，本刀不代裁）

1. **五轮各自实到红枚数与红名／帧行／消息族是否逐字符等于 §一 落档闭合表；哪轮作废、哪轮退回**：k′＝1（`…(scope: "instance")` :447 `Assert.Throws() Failure: No exception was thrown&#xD;&#xA;Expected: typeof(Pry.Core.Narrative.NarrativeContextAssemblyRejectedException)`）＝表；l′＝1（`…(scope: "batch")` :447 同族消息）＝表；m′＝1（`Immediate_action_over_the_host_segment_ceiling_is_refused` :677 `…Expected: ContractRejected&#xD;&#xA;Actual:   HistoryRejected`）＝**本刀 §一.4 修正后的表**，与工单 §二 的 `Actual: Immediate` 面值**不**逐字符相等；n′＝1（`Immediate_action_carrying_a_future_intent_is_refused` :705 `Assert.Single() Failure: The collection was empty`）＝表；o′＝**退回**（件3′ 闭合不成恰 1，未起轮，额度原样退回，`退回≠作废`）。作废轮＝0。
2. **每轮 `cmd-<轮>.txt` 与跑后 `.trx` 枚数**：`cmd-k.txt`/1、`cmd-l.txt`/1、`cmd-m.txt`/1、`cmd-n.txt`/1（四份均 `CMD-VERDICT=OK`，`--logger "trx;…"` 与 `--results-directory` 字符校验在档；逐条数值见 §五.3/§五.4）。无"本轮无凭据"登记。
3. **判据 SHA 每轮两侧＋出场面是否当场打印**：直接打印面＝入刀（§〇.3）、k′ 前后各一次、n′ 后两次（还原后直取＋`out/inventory-final.txt` §5）；l′／m′ 两侧未单独打印，**替代面**＝`out/revert-l.txt`、`out/revert-m.txt` 的 carrier `status --short`＝0 行（`tests/**` 行枚数＝0）＋HEAD 未动 ⇒ blob 同值。四轮后判据 SHA＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`（与工单基准逐字符相同）。
4. **载体终态三件＋三枚文件三面 SHA**：§三.1／§三.2。补一条本刀自发的交接风险面：盘上 `Pry.Core.dll`＝n′ 突变体所编（§三.3），源码三面已回基线 ⇒ **二者不一致**。
5. **收尾全量（若起了）**：**未起**（§四.2），故无四份求和式、无 `NotExecuted` 逐字符点名面——两条历史跳过格（`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`）只在 §〇.5 仪器自证里作为历史全量面复算过，不属本刀读数。
6. **盘上 `.trx` 枚数／结果目录枚数／有无 `-voided-`／每轮 `total` 与当场格数对账**：4／4／0／四轮全等 56（§五.4）。
7. **四类预算动用与上限；预检 mtime 与 run mtime 一一配对**：§五.1／§五.2／§五.3。`dotnet` 4/6，HOLD 0/16。
8. **每枚红的性质＋件9 下游消费者枚举表落点**：四枚**全部是断言级红**（每枚都有 `in …NarratorPrototypeTests.cs:line N` 帧锚，N＝447/447/677/705，无整格异常红、无 `NotExecuted`、无载入级面）。件9 枚举表＝§一.3 前的"件9 消费面枚举"段（k′／l′＝`NarrativeRoundRunner.cs:105` 唯一产品消费点；m′＝`DeliveryLedger.cs:115` 封顶切片＋`NarrativeRoundRunner.cs:214-:218` 的 HistoryRejected 支；n′＝`NarrativeDraftValidator.cs:145-:152` 拒绝累加器＋`:160` 返回面；o′＝K:409/K:440/K:275 三处值面消费）。
9. **未领账与判不清面逐枚登记**：#48（o′，退回，需"只被 @367 走"的值面配方，或把 @761 的 refs 出处从 K:99 迁开——后者属判据字节，本刀权限外）；m′ 的 @684 面＝定向面实测**不响**（名册 39 在场仍 Passed），工单 §六.1 要的"先穷举方向类突变"本刀已在 §一.4 表内按五种形状走了一遍，@684 的响面属全量口径，判不清留待 F；k′ 的短路序＝盘面读得 `N:36→N:43→N:50→N:57→N:64` 顺序短路（`out/closure-scan.txt` §B9），因此 k′/l′ 各自只翻一格的结论有字节面支撑，但"若同时摘两行会怎样"＝未验证（本刀严守一行一枚，未做双摘）；#32 的押账未动、未减（§六.4）。

---

## 八 新口径（本刀挣到的，供门房裁是否入册）

1. **工单 §二 m′ 的事前面值需按盘面改**：`Actual` 面在突变体下是 `HistoryRejected`，不是 `Immediate`。链路＝`DeliveryLedger.cs:115` 的 `Math.Min(PlannedSegmentCount, maxSegments)` 把 6 段裁到 4 段 ⇒ 契约面无拒绝 ⇒ `NarrativeRoundRunner.cs:214-:218` 的越界切片分支先命中。本轮红证已按修正面签署，`out/trx-m/result-m.trx` 是那枚错的工单预测的现场否证面。
2. **o′ 这类"值面清空"的闭合前提要加一条**：件3′ 要看的是**该值面的每一处消费者是否都只被见证格走到**。K:99 的 `CurrentEvent.SourceRef` 同时喂 `EmittedSourceRefs`（:772）与 Identity 段计数（:372/:375）⇒ 一枚清空两格同响 ⇒ 定向面必然不成恰 1。今后派值面清空刀，前置判据应写成"预期红的**消费者集合**恰一"，而不是"见证格恰一"。
3. **`/` 记法不是面值**（§〇.5 已把这条挣过一次）：进件10 自证表前必须先解盘上原始字节再落字面量；本轮四枚消息族（`Assert.Throws`／`Assert.Equal`／`Assert.Single` 三族）在盘上都是 CR+LF 双行且 `Actual:` 后两枚空格。
4. **"每轮 total 等于当场格数"是双向门**：它既否掉"少跑了一批"，也否掉"filter 漏到别的类"。四轮 `total=56` 与 §〇.4 的 53＋1×3 互检，是本刀唯一能证明 class-scoped 收紧成功的盘面面。
5. **还原后不重编＝盘上二进制与源码不一致**：这是本刀形状（4 轮定向＋构建跳过）的必然产物，应在工单 §四.1 的三段判定里补一句"若上一把刀跑过定向突变轮而未重编，段二不得算过"。
