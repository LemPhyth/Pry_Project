# HDS-WAVE44-1029｜波 13 D 类第四片（T3 余 11 枚里的五枚：三枚单行条件＋一枚合取半边＋一枚值面清空）——守门人工单

- 坐标系＝`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS-003；账本＝波 22 审计报告 `docs/eval/reports/2026-10-23-hds003-005-cell-evidence-audit.md` §四.1 的梯队 T3（该表逐行重数＝23 枚，波 41／42／43 已结 12 枚，本刀从余下 11 枚里开五枚）。
- 性质＝证据刀·换人复采。**零产品新字节（终态）、零判据字节、零新格、零新增数字门槛、零 UI、零生产接线、零打包。**
- 载体＝`.local-notes/qa-wave33-1028/carrier`（基点 `fd93ebe`／detached HEAD＝`51f149d`）。不新建 worktree、不新建产品或测试文件、主树零 git 写动作。
- 判据文件＝`tests/Pry.Core.Tests/NarratorPrototypeTests.cs`，本刀全程一枚字节不写；基准 SHA（`51f149d` blob＝载体工作副本＝主树工作副本，三面当场逐字符等值才准起跑）＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`。
- 报告落 `docs/eval/reports/2026-10-29-hds13-dclass-fourth-slice.md`（主树里本刀唯一新建文件，不提交、不 `git add`；守门人裁决节不由本刀写，本节号在本报告里保留为空白）。产物目录＝`.local-notes/wave44-1029/`。
- 上一把的教训照成本刀前置＝波 43 的 h′ 因"我出闸时没把早退 `return` 的下游消费者逐枚枚举"而按件 3′ 退回（那是合法交付，但 #32 因此仍欠）；本刀把这条升格为**件 9**，并把波 43 我自己那两处仪器错（`<Counters>` 形状、数字实体未解）升格为**件 10**。件 8（命令形状自检）已在波 43 首次实际使用并四面齐，照旧为硬前置。

## 〇 硬线（违反任一条＝当场停手交回，不许自行解释）

1. 主树禁止任何 `git commit`／`add`／`checkout`／`reset`／`cherry-pick`／`merge`／`tag`／`push`／`git apply`／`stash`／`clean`／`rm`。他人资产（两枚 `M ` 暂存项、一枚 ` M` 的 `PROJECT_PLAN.md`＝18/0、`??` 未跟踪文件）一字不读不改不提交。
2. 每一次 `dotnet` 之前先跑 `.local-notes/game-preflight6-1022.ps1` 并把整段输出归档为 `out/preflight-P1xx.txt`；`VERDICT=BUILD_OK` 才起跑，`HOLD` 就停下（等待重试按刀计 ≤16 枚，两次 HOLD 之间间隔 ≥5 分钟；连续 HOLD 交回＝合法交付，不许改判据、不许绕过、不许"再试一次就跑"）。
3. 判据字节不许写；断言不许放宽、不许删、不许注释掉；不许 `Assert.Skip`；不许为凑红新增数字门槛。载体 `git status --short` 全程出现的那一行必须永远是 `src/` 产品文件，出现 `tests/**`＝立即停手。
4. 唯一写入通道＝自写脚本（六道守卫：落点越界即拒、施加文本必须等于预合成那枚、被替换行与 manifest OLD 逐字符等值且整行出现恰 1 次、施加前目标仍等于 `blob(51f149d)`、写后 SHA 不等于声明值即自动还原、还原一律从 `git show 51f149d:<path>` 取字节再写文件）。不许手改文件，不许用 `checkout`/`restore`。
5. 本刀不裁归位、不写状态词／结案词、不自宣基线；归位与顶账由守门人按路径做。
6. **四句话照旧不许写**＝新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链。

## 一 先数盘（入场第一件事，全部当场从盘取，不引用本单或波 43 的数字当入场值）

1. 主树 `git rev-parse HEAD`、`git status --short --branch` 原样逐行抄进报告 §〇（并数行数；`PROJECT_PLAN.md` 那枚 ` M` 的 `git diff --numstat HEAD` 应当＝18/0，非 18/0 即停手并交回）。
2. 载体四件：`rev-parse HEAD`／`status --short` 行数（≠0 行即停手）／`log --oneline fd93ebe..HEAD` 行数／`rev-parse --show-toplevel`。
3. `git worktree list` 行数。
4. 判据文件格数当场重取（载体侧现文）：`[Fact]`／`[Theory]`／`[InlineData]` 三枚计数与 `total = [Fact] + [Theory]×[InlineData]` 的等式；**定向跑每轮 `total` 必须等于这个当场值**（不许照抄上一把的 56）。`Assert.Skip` 计数、判据文件行数一并取。
5. 盘上已知读数表（**每一枚都是出表人 10-29 当场用 `.local-notes/wave43-1029/gk43-verify.js`／`gk43-wave41-faces.js` 从 TRX 重取的**，lane 仍要自己回读原始产物并按算术自取值；每条带四元限定＋行号种类，**@＝判据格起始行、: ＝ TRX 断言帧行**，两种口径不许混用不标）：
   - 波 41 `trx-a`｜`NarratorPrototypeTests.cs`｜定向突变轮｜恰 1，@520／:526，`Assert.Equal() Expected: ContractRejected / Actual: Silence`。
   - 波 41 `trx-b`｜同上｜同上｜恰 1，@599／:606，`… Expected: ContractRejected / Actual: Deferred`。
   - 波 41 `trx-d`｜同上｜同上｜恰 1，@437（`scope:"conversation"` 参数行）／:447，`Assert.Throws() Failure: No exception was thrown / Expected: typeof(Pry.Core.Narrative.NarrativeContextAssemblyRejectedException)`；同方法另两参数行同场 Passed。
   - 波 41 `trx-e`｜同上｜同上｜恰 1，@790／:796，`Assert.Single() Failure: The collection was empty`。
   - 波 42 `trx-g`／`trx-h`／`trx-i`／`trx-j`｜同上｜同上｜各恰 1，@658/:665、@722/:730、@735/:741、@802/:808，消息族全 `Assert.Single() Failure: The collection was empty`。
   - 波 43 `trx-f2`｜同上｜同上｜恰 1，@614／:620，`… Expected: ContractRejected / Actual: Deferred`。
   - 波 43 `trx-g2`｜同上｜同上｜恰 1，@776／:782，`… Expected: ContractRejected / Actual: Immediate`。
   - 波 43 `trx-i2`｜同上｜同上｜恰 1，@711／:717，`Assert.Single() Failure: The collection was empty`。
   - 波 43 `trx-j2`｜同上｜同上｜恰 1，@746／:752，`… Expected: 2 / Actual: 1`。
   - 波 40 `trx-M8p`｜同上｜定向 56 格｜恰 5，帧 :540,:555,:569,:582,:865（格起始 @534/@549/@562/@574/@851）。
   - 波 40 `trx-m08`｜同上｜同上｜恰 4，帧 :289,:320,:961,:981（格起始 @281/@313/@952/@975）。
   - 波 39 `trx-M2`｜同上｜同上｜恰 3；波 39 `trx-M8-voided-P103`｜同上｜作废轮留档｜恰 5。
   - 波 13 `run-full`｜四程序集｜解决方案级全量｜total 求和 1228、failed 2、`NotExecuted` 开标签 2。
   - 波 40 `trx-F`｜四程序集｜解决方案级全量｜total 1406、executed 1404、failed 0、`NotExecuted` 开标签 2（在册基线，本刀不复采即不移动）。
6. 与上一把不同面的检查：本刀锚点全部按载体 `51f149d` 现文重取，**审计表的"当前树行号"只是历史线索**（波 40／41／42 已实测 `R:297`／`R:327`／`D:130` 现在读作 `{`／`}`／枚举注释行）。锚点读到非预期文本＝换锚或登记为不可判，不许照抄历史行号下刀。
7. 载体 `bin/`／`obj/` 里的构建产物清洁度限定（波 43 实测过一整轮）：**同一枚路径在一把刀之内会换两副字节**——波 43 入场面＝`src/Pry.Core/bin/Release/net10.0/Pry.Core.dll` 1,745,408 字节/11:58（波 42 末轮 R:250 突变态的产物），出场复看＝1,745,920 字节/12:54（波 43 j′ 的 V:131 突变态产物），而源码字节两轮都已回 blob。因此"成品在盘"那段判定只允许支撑"文件存在＋体积合理"，**永远不许写成"清洁二进制"的凭据**；第一轮定向跑的自带增量编译会再换一副字节。

## 二 五枚范围（每枚＝一行既有门的单点突变，目标＝该门的按名见证格响一次）

| 轮 | 格（波 22 审计 §四.1 点号） | 要改坏的那一行（载体 `51f149d` 现文，逐字符） | 突变语义 | 守门人的事前论证（**非结论，lane 必须自己按当前 blob 重推并落闭合表**） |
|---|---|---|---|---|
| k′ | #16 `Call_refuses_an_assembly_that_belongs_to_another_scope(scope:"instance")`（格起始 @437） | `NarratorContracts.cs:43`＝`        if (!Assembly.InstanceId.Equals(Request.InstanceId))` | 条件换 `false` | 夹具 `:442` 只有 `"instance"` 那支改 `InstanceId = CharacterInstanceId.Create("ci-instance-beta")`，`:443`／`:444` 两支只改各自字段 ⇒ 摘掉第一支后 instance 行的 `Validate()` 不抛，@437 的 `:447 Assert.Throws()` 翻（消息形状有波 41 `trx-d` 同族盘上先例）。**必须点名的面**：N:43 是三支按序短路的第一支，改后 instance 行会继续求值 N:50／N:57 ⇒ 闭合表要写清那两支对该参数行仍为 false 的字节依据。另两参数行同场 Passed 属正面凭据（同一份 TRX 里逐节点读）。预期红集＝**恰 1** |
| l′ | #18 同上（`scope:"batch"` 参数行，@437） | `NarratorContracts.cs:57`＝`        if (!string.Equals(Assembly.BatchId, Request.BatchId, StringComparison.Ordinal))` | 条件换 `false` | 与 k′ 对称：`:444` 的 `_ =>` 默认臂给 `BatchId = "batch-2"`，其余两支不改批次字段。N:57 是第三支（末端），摘掉它不会让别的分支多读 ⇒ 预期恰 1＝@437[batch]／帧 :447；**但 lane 仍要逐枚点名 instance／conversation 两行为什么同场 Passed**（改前它们各自的红不来自这一行）。 |
| m′ | #34 `Immediate_action_over_the_host_segment_ceiling_is_refused`（@671） | `NarrativeDraftValidator.cs:200`＝`                if (action.PlannedSegmentCount < 1 || action.PlannedSegmentCount > request.MaxOutboundSegments)` | 拆合取右半边（保留 `< 1`，整行写成 `if (action.PlannedSegmentCount < 1)`） | 盘上 `segments:` 显式传参恰两处（`:673` 的 6 与 `:686` 的 4），默认夹具 3，`Request()` 的 `maxSegments`＝4 ⇒ 只有 `:673` 那条从"越界"变"合法"，@671 的 `:677` 先翻（家族形状＝波 43 `trx-g2` 的 `Expected: ContractRejected / Actual: Immediate`）。**必须点名的面**：@684（`:686`，segments 恰等上限）与同函数下游 `:207`／`:214` 会不会被多读；`:678`／`:679`／`:680` 在同一格里只算一枚红（红点取第一条）。 |
| n′ | #36 `Immediate_action_carrying_a_future_intent_is_refused`（@698） | `NarrativeDraftValidator.cs:214`＝`                if (action.DeferredIntentId is not null)` | 条件换 `false` | Immediate 支里带 `DeferredIntentId` 的构造点盘上恰一处（`:700` 的 `ImmediateAction() with { DeferredIntentId = "intent-9" }`）；`:93` 的 `DeferredAction(...)` 属 Deferred 支、`:214` 在其后不执行 ⇒ 改后 @698 的拒绝数 1→0，`:705 Assert.Single()` 翻。另一面 :707 `Assert.Empty(round.Ledger.Snapshot())` 在 :705 之后，同一格两条仍只算一枚红。预期＝恰 1 |
| o′ | #48 `Compiler_carries_source_references_into_every_emitted_section`（@367） | `NarrativeContextCompiler.cs:99`＝`            SourceRefs = [payloads.IdentitySourceRef, payloads.CurrentEvent.SourceRef],` | 值面清空（整行写成 `SourceRefs = [],`） | 产品侧消费者＝同文件 `:440` 的 `EmittedSourceRefs => [.. Segments.SelectMany(s => s.SourceRefs)]` 一处，**全仓 `src/` 除该文件外对 `SourceRefs` 零命中**（我 10-29 当场 grep）⇒ 编译器不校验它非空，改后不会让每次 `Compile` 抛。本类判据里 `EmittedSourceRefs` 出现 6 处＝`:372`／`:373`／`:374`／`:375`（同格 @367，第一条会翻：`Expected: 4 / Actual: 2`）、`:481`（读 `"advisory:block"`，来源是未改的 `:119`／`:133`）、`:772`（读 `"input:101"`，而 `:375` 自己断言改前恰现 2 次＝身份支＋近聊支 `:275` ⇒ 摘一份仍有 1 份，`Contains` 仍真）。**"恰 1"的前提＝这三面在起跑前全部闭合**，尤其 `:772` 那条必须由 lane 自己在 blob 上取到近聊段 refs 的出处才算闭合；取不到就按件 3′ 退回。 |

判定门＝每轮**恰 1**；实到 ≠ 落档表 ⇒ 整轮作废（件 4），目标格不签、不重跑、不追认。

## 三 前置件（每件都要成品）

- **件 1＝改前整行并排自证**：五枚 OLD 现文与载体 blob 逐字符并排打印＋整行出现恰 1 次＋改前行内容等值断言；不符 ABORT 不写一字节。**SHA 对账不能替代并排自证。**
- **件 2＝突变文本预合成、零 `dotnet` 先核 SHA**：五枚改后文本落 `.local-notes/wave44-1029/mutation-prep/`，SHA-256 与字节数当场进报告 §一；每轮"施加后 SHA"必须逐字符等于该行。
- **件 3＝形状自检**：净括号偏移守恒（不是绝对数相等）＋全文件净圆括号／净大括号／中方括号收支不变＋行数不变。o′ 那枚改的是中方括号对，自检必须把 `[` `]` 的净偏移也算进去。
- **件 3′＝闭合点名门（硬门）**：每轮起跑前报告里必须已落档**闭合**的预期红集表＝逐枚点名谁会红（夹具如何走到被改行、翻掉的断言行号、消息形状）＋逐枚写明其余每一枚为什么不连坐（可达路径级理由，不许写"应该不影响"）。**枚数逐枚点名，禁止"各恰 N"合并写法。** 闭合不了 ⇒ 不起那一轮、定向额度原样退回、判不清面逐枚登记（波 41 的 c 轮、波 43 的 h′ 轮是先例）。禁止用"跑一把看响几枚"代替推演。**闭合表不许抄波 43 §一 的文本，必须本刀自己按载体 blob 重推落档。**
- **件 4＝多响处置**：见 §二 判定门。
- **件 5＝读数口径**：只数 `<UnitTestResult … outcome="Failed">` 开标签并与同份 `<Counters failed>` 双向对账；`<ResultSummary outcome="Failed">` 不算用例；计数器形状＝**单枚 `<Counters total="…" executed="…" passed="…" failed="…" notExecuted="…" />`，属性名小写**（波 43 我的仪器按 `<Counter outcome="X" count="N">` 取数导致四面全 `null` 集体误报，那是仪器面）；属性按名字取不按顺序、**取回的属性值与 `<Message>` 文本都要解 XML 实体，含数字实体 `&#xD;`／`&#xA;`**；栈帧锚点＝`in <路径>:line N`（TRX 里没有 `at ` 可锚，且路径含盘符冒号，切分不许用 `[^:]`）；跳过格只认 `outcome="NotExecuted"` 开标签；`testName` 是全限定＋参数表，回判方法名先剥括号。
- **件 6＝仪器自证**：取数仪器在裁本刀之前必须先对 §一.5 那批带四元限定的老读数复算并逐面 AGREE；任一面 MISMATCH ⇒ **先证明坏的是表还是仪器**，不许直接用它裁新读数。行号必须标种类（格起始／TRX 断言帧）。
- **件 7＝负向断言要指明见证面**：写"某选项没出现"之前先证明那个字段在这份归档里本来会出现（`run-*.txt` 是 stdout，不含命令行 ⇒ 那里"没有 `--no-build`"不是凭据；正面凭据是构建产出行 ` -> ….dll` 在场）。
- **件 8＝命令形状自检**：每一轮定向跑**之前**，把将要执行的完整命令行原样落档为 `out/cmd-<轮>.txt`，并逐字符核对含 `--logger trx` 与结果目录开关；跑完**立刻**当场打印该目录的 `.trx` 枚数，枚数＝0 ⇒ 本轮即刻登记为"无凭据轮"，不许回头往 stdout 找凭据、不许补跑第二轮救同一轮、不许为凑一次 F 多弹防火墙。（波 42 买下来、波 43 首次四面齐。）
- **件 9＝早退路径的下游消费者枚举（波 43 h′ 买下来的新硬门）**：凡被改的那行是一支早退 `return`／`throw`，或是一枚被多处读取的值／字段初值，事前论证必须先把它的**每一个下游消费者**逐枚列一遍并逐枚判连坐，然后才准写枚数预期。**"形状不同仍只算 1 枚"这句从此不许再写。** 本刀 o′ 就落在这一枚的射程里（`SourceRefs` 有三处判据消费者），k′ 的短路序也按这条处理。
- **件 10＝解码与计数器通道要被已知答案验一次（波 43 我自己两处仪器错买下来的）**：自证表里**至少含一枚"消息族面值"**（例如波 41 `trx-d` 的 `Assert.Throws() Failure: No exception was thrown / Expected: typeof(Pry.Core.Narrative.NarrativeContextAssemblyRejectedException)`，或波 43 四轮的任一族），并逐字符等值才算该面 AGREE；只报"红数对上了"不构成件 6 的通过。**"报了 AGREE"不等于"那个字段真被读过"。**

## 四 预算（超限即停，不自我扩额）

1. 解决方案级 `dotnet build` ≤1（默认按三段判定合法跳过：0 行式子成立＋`Pry.Core.dll` 成品在盘＋三枚 SHA 三面未变；注意 §一.7 那条清洁度限定）。
2. 解决方案级全量 `dotnet test Pry.slnx -c Release` ≤1：**五轮全部起跑且全部有效才起**；任一轮作废、退回或无凭据 ⇒ 立即不起（波 42／43 的先例）。
3. 定向 `dotnet test --filter` ≤5；filter 必须 class-scoped 到 `NarratorPrototypeTests`（m′／n′ 的门与波 14 的格子成对同门＝全场面会两枚同响，那属全量面不属定向面，口径要明写）。
4. `dotnet` 总调用 ≤6。
5. HOLD 重跑 ≤16（按刀计，两次之间 ≥5 分钟）。
6. 预检编号从 **P125** 起，逐份归档；"逐份 VERDICT 枚数＝`dotnet` 次数＝实跑轮数"三方对账不许省。
7. 跑序固定＝k′→l′→m′→n′→o′（每轮：预检→件 8 落档命令→施加→定向跑→立刻数 `.trx`→读数→还原→核 `status` 行数与三面 SHA），五轮全部有效才在还原态起 F；任何一轮退回 ⇒ 直接跳到"还原并核三面 SHA"再交回。

## 五 交回守门人核的事（逐件给当场读数，不代裁）

1. 五轮各自实到红枚数与红名／TRX 断言帧行／消息族，是否逐字符等于 §一 落档的闭合表；哪一轮作废、哪一轮退回、依据是什么。
2. 每轮的 `out/cmd-<轮>.txt` 与跑后 `.trx` 枚数（件 8 的两面），逐轮列出。
3. 判据文件 SHA 是否每轮两侧＋出场面都当场打印；没打印的轮次，替代面写清楚（0 行＋载体 HEAD 未动是 blob 同值的充分面，但要明写这是替代面）。
4. 载体终态三件（`status --short` 行数／`fd93ebe..HEAD` 行数／`rev-parse HEAD`）＋三枚被改文件（`NarratorContracts.cs`／`NarrativeDraftValidator.cs`／`NarrativeContextCompiler.cs`）的三面 SHA。
5. 收尾全量（若起了）：四份先数满再逐份 `<Counters>` 相加，写出求和式；两条跳过按 `outcome="NotExecuted"` 开标签逐字符点名。
6. 盘上 `.trx` 枚数、结果目录枚数、有无 `-voided-`；每轮 `total` 与 §一.4 当场格数的对账。
7. 四类预算各自动用与上限；预检 mtime 与 run mtime 的一一配对。
8. 每一枚红的性质（断言级红／整格异常红，逐枚点名）；件 9 那面的下游消费者枚举表落在哪一节。
9. 未领账与判不清面逐枚登记（尤其 o′ 的 `EmittedSourceRefs` 三处消费者、k′ 的短路序、m′ 的 @684 面）。

## 六 不许走的捷径

1. 不许把"目标格没响"写成"这枚门结构上够不着"而不试——先穷举方向类突变（改恒假／去掉取反／换常量／拆合取半边／清空值面），再把剩下的登记为判不清（波 28 两条就是这样被证伪的）。
2. 不许用 `if (false)`／`[]` 之外的形状去"顺手改宽"多条断言；一行就是突变的一行。
3. 不许为凑一次 F 读数多弹一次防火墙（§四.2 是硬条件）。
4. 不许拿本刀的绿去"顶替"上一把的欠账条目，也不许减少任何在册欠账（欠账只随签账减少）；#32 的账仍押着，等"只被 @644 走"的那把新配方。
5. 不许在报告里写归位／顶账／基线移动。
6. **不许用 stdout 面签署任何一格**（波 42 §九.2 已把这条裁死：stdout 没有逐枚可枚举节点面，看不见多响）。
7. **不许把"class-scoped 定向面恰 1"写成"这枚门只守一格"**：m′／n′ 的门与波 14 的格子共享消费者，全场面会同响，口径必须写成"定向面 1、全场面待定"。

## 七 后面的刀（不在本刀范围）

T3 余下（#21／#28／#32／#35／#45／#46——其中 #28／#45 的历史行号已实测漂移要重锚，#35 的 `D:130` 同样漂移，#32 要"只被 @644 走"的新配方＝件 9 射程，#21 的 `R:169` 那枚 Disposition 值面被多处读取＝件 9 射程，#46 的 `SegmentNames` 顺序被 @183／@913／@934／@952／@975 五枚读取＝先穷举再决定派或撤）；T4 那 5 枚（#12／#19／#20／#41／#47＝要新增形状或夹具改造，另议）；#49（T5 拆门无人守）、#31（那只手写过的判据字节会作废凭证）；波 20 那 28 枚；`used + cost > total` 那一半；`:367`；探针正数面的单独读数；HDS-007 的 `:195`／`:172`；波 28 那枚数值锁格；`PrototypeFiles` 写死清单的覆盖盲区；两套并行命名债；HDS-010 删除片；HDS-003／007／009 的生产接线（属接线刀不属证据刀）。
