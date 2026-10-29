# HDS-WAVE43-1029｜波 13 D 类第三片（T3 余五枚换人复采，含 #30 带凭据补跑）——守门人工单

- 坐标系＝`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS-003；账本＝波 22 审计报告 `docs/eval/reports/2026-10-23-hds003-005-cell-evidence-audit.md` §四.1 的梯队 T3。
- 性质＝证据刀·换人复采。**零产品新字节（终态）、零判据字节、零新格、零新增数字门槛、零 UI、零生产接线、零打包。**
- 载体＝`.local-notes/qa-wave33-1028/carrier`（基点／detached HEAD＝`51f149d`）。不新建 worktree、不新建产品或测试文件、主树零 git 写动作。
- 判据文件＝`tests/Pry.Core.Tests/NarratorPrototypeTests.cs`，本刀全程一枚字节不写；基准 SHA（`51f149d` blob＝载体工作副本＝主树工作副本，三面当场逐字符等值才准起跑）＝`4f1b2f6ed68718f62522099d29933a256ad6fc7f4f66977cd277453a285818f4`。
- 报告落 `docs/eval/reports/2026-10-29-hds13-dclass-third-slice.md`（主树里本刀唯一新建文件，不提交、不 `git add`；守门人裁决节不由本刀写，本节号在本报告里保留为空白）。产物目录＝`.local-notes/wave43-1029/`。
- 上一把的教训照成本刀前置＝波 42 的 f 轮命令行漏 `--logger trx` ⇒ 该轮无凭据、#30 未领、收尾全量 F 被联动挡掉。**本刀新增件 8（命令形状自检）为硬前置。**

## 〇 硬线（违反任一条＝当场停手交回，不许自行解释）

1. 主树禁止任何 `git commit`／`add`／`checkout`／`reset`／`cherry-pick`／`merge`／`tag`／`push`／`git apply`／`stash`／`clean`／`rm`。他人资产（两枚 `M ` 暂存项、一枚 ` M` 的 `PROJECT_PLAN.md`、五枚以上未跟踪文件）一字不读不改不提交。
2. 每一次 `dotnet` 之前先跑 `.local-notes/game-preflight6-1022.ps1` 并把整段输出归档为 `out/preflight-P1xx.txt`；`VERDICT=BUILD_OK` 才起跑，`HOLD` 就停下（等待重试按刀计 ≤16 枚，两次 HOLD 之间间隔 ≥5 分钟；连续 HOLD 交回＝合法交付，不许改判据、不许绕过、不许"再试一次就跑"）。
3. 判据字节不许写；断言不许放宽、不许删、不许注释掉；不许 `Assert.Skip`；不许为凑红新增数字门槛。载体 `git status --short` 全程出现的那一行必须永远是 `src/` 产品文件，出现 `tests/**`＝立即停手。
4. 唯一写入通道＝自写脚本（六道守卫：落点越界即拒、施加文本必须等于预合成那枚、被替换行与 manifest OLD 逐字符等值且整行出现恰 1 次、施加前目标仍等于 `blob(51f149d)`、写后 SHA 不等于声明值即自动还原、还原一律从 `git show 51f149d:<path>` 取字节再写文件）。不许手改文件，不许用 `checkout`/`restore`。
5. 本刀不裁归位、不写状态词／结案词、不自宣基线；归位与顶账由守门人按路径做。
6. **四句话照旧不许写**＝新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链。

## 一 先数盘（入场第一件事，全部当场从盘取，不引用本单或波 42 的数字当入场值）

1. 主树 `git rev-parse HEAD`、`git status --short --branch` 原样逐行抄进报告 §〇（并数行数）。
2. 载体四件：`rev-parse HEAD`／`status --short` 行数（≠0 行即停手）／`log --oneline fd93ebe..HEAD` 行数／`rev-parse --show-toplevel`。
3. `git worktree list` 行数。
4. 判据文件格数当场重取（载体侧现文）：`[Fact]`／`[Theory]`／`[InlineData]` 三枚计数与 `total = [Fact] + [Theory]×[InlineData]` 的等式；**定向跑每轮 `total` 必须等于这个当场值**（不许照抄上一把的 56）。`Assert.Skip` 计数、判据文件行数一并取。
5. 盘上已知读数表（本单只给方向，**每一枚都要 lane 自己回读原始产物并按算术自取值，不许引用本节当结论**；每条必须带四元限定＋行号种类，**@＝判据文件里的格起始行、: ＝ TRX 断言帧行，两种口径不许混用不标**）：
   - 波 41 `trx-a`｜`NarratorPrototypeTests.cs`｜定向突变轮｜恰 1，@520／帧 :526。
   - 波 41 `trx-b`｜同上｜同上｜恰 1，@599／:606。
   - 波 41 `trx-d`｜同上｜同上｜恰 1，@437（`scope:"conversation"` 参数行，TRX 里是 `&quot;` 转义态）／:447；同方法另两参数行同场 Passed。
   - 波 41 `trx-e`｜同上｜同上｜恰 1，@790／:796。
   - 波 40 `trx-M8p`｜同上｜定向 56 格｜恰 5，帧 :540,:555,:569,:582,:865（格起始 @534/@549/@562/@574/@851）。
   - 波 40 `trx-m08`｜同上｜同上｜恰 4，帧 :289,:320,:961,:981（格起始 @281/@313/@952/@975）。
   - 波 42 `trx-g`｜同上｜同上｜恰 1，@658／:665，消息族 `Assert.Single() Failure: The collection was empty`。
   - 波 42 `trx-h`｜同上｜同上｜恰 1，@722／:730，同族。
   - 波 42 `trx-i`｜同上｜同上｜恰 1，@735／:741，同族。
   - 波 42 `trx-j`｜同上｜同上｜恰 1，@802／:808，同族。
   - 波 13 `run-full`｜四程序集｜解决方案级全量｜total 求和 1228、failed 2、`NotExecuted` 开标签 2。
   - 波 40 `trx-F`｜四程序集｜解决方案级全量｜total 1406、executed 1404、failed 0、`NotExecuted` 开标签 2（在册基线，本刀不复采即不移动）。
6. 与上一把不同面的检查：本刀锚点全部按载体 `51f149d` 现文重取，**审计表的"当前树行号"只是历史线索**（波 41／42 已实测 `R:297`／`R:327` 现在读作 `{`／`}`）。锚点读到非预期文本＝换锚或登记为不可判，不许照抄历史行号下刀。
7. 载体 `bin/`／`obj/` 里现存的构建产物停在波 42 轮 j 的突变态（源码字节已回 blob）。**这枚 dll 只支撑"成品在盘"那一段判定，不许当作"清洁二进制"的凭据写进任何自证**；第一轮定向跑的自带增量编译会把它洗回清洁态。

## 二 五枚范围（每枚＝一行既有门的单点突变，目标＝该门的按名见证格响一次）

| 轮 | 格（波 22 审计 §四.1 点号） | 要改坏的那一行（载体 `51f149d` 现文，逐字符） | 突变语义 | 守门人的事前论证（**非结论，lane 必须自己重推并落闭合表**） |
|---|---|---|---|---|
| f′ | #30 `Deferred_action_without_a_pointing_intent_is_refused`（格起始 @614） | `NarrativeDraftValidator.cs:240`＝`                else if (!content.Intents.Any(i => string.Equals(i.IntentId, action.DeferredIntentId, StringComparison.Ordinal)))` | 条件换 `false`（保留 `else if` 语法形状） | 波 42 已把这一枚锚点与施加点三面证明正确（`out/apply-f.txt` 的 SHA＝预合成值、`diff -U0` 只那一行、还原后 blob 同值），欠的只是运行凭据：那轮命令行漏 `--logger trx`。盘上 `DeferredAction(` 调用点四枚（:505／:601／:616／:834）一律传非空 `"intent-1"` ⇒ `:235` 的 null 支恒不执行；四枚夹具只 :616（＝格 @614）的 `intents` 为空 ⇒ 改后该拒绝消失、`Disposition` 由 `ContractRejected` 变 `Deferred`、@614 的 `:620` 翻。预期红集＝**恰 1 枚**，TRX 断言帧行 :620，消息族 `Assert.Equal() Expected: ContractRejected / Actual: Deferred`。**本轮的硬增量＝件 8** |
| g′ | #42 `Draft_on_a_stale_story_version_is_refused_before_any_history`（@776） | `NarrativeRoundRunner.cs:238`＝`        if (draft.StoryVersionBase != request.StoryVersion)` | 条件换 `false` | `CheckSnapshotFreshness` 三支的**第一支**（`:238` 故事版本／`:244` 代次＝波 41 已结清／`:250` 基线＝波 42 已结清），三支按顺序短路；默认夹具 `storyVersionBase = 7`（＝`Request()` 的故事版本），盘上只有 @776 传非 7。摘掉第一支后 @790／@802 那两枚各自仍在本支下游被拒 ⇒ 不连坐。预期＝恰 1 |
| h′ | #32 `Quote_absent_from_this_narration_rejects_the_action_without_host_invention`（@644） | `NarrativeDraftValidator.cs:71`＝`        if (occurrences == 0)` | 条件换 `false` | 与波 42 g 轮的 `:76`（`occurrences > 1`，@658 已结清）是同一函数的相邻两支；`occurrences == 0` 只有故意引用不存在的那枚夹具构造。**注意形状**：`:71` 是一支 `return`，摘掉它之后该用例继续往下走，红可能是断言级、也可能是整格异常红——形状不同仍只算 1 枚，但 lane 必须事前把"改后走到哪一行、消息什么形状"写进闭合表，跑完不许回头改预期列 |
| i′ | #37 `Action_targeting_another_conversation_is_refused`（@711） | `NarrativeDraftValidator.cs:191`＝`        if (!string.Equals(action.TargetConversationId, request.ConversationId, StringComparison.Ordinal))` | 条件换 `false` | 夹具里显式传 `target:` 非默认房间的枚数极少（盘上唯一一处 `target: "room-2"` 落 @711），其余动作默认 `TargetConversationId = Conversation`。**必须点名另一处**：`action.TargetConversationId` 在 `NarrativeDraftValidator.cs` 里出现 **2** 处（守门人当场 `grep -c` 取到 2），闭合表要写清另一处落在哪个分支、改后会不会同场被读到 |
| j′ | #40 `Round_refuses_a_narration_without_prose_instead_of_binding_to_nothing`（@746） | `NarrativeDraftValidator.cs:131`＝`        if (string.IsNullOrWhiteSpace(content.NarrativeProse))` | 条件换 `false` | 默认夹具 `Draft(..., prose = Prose)` 非空白，盘上只有 @746 传 `prose: "   "`；该格断言的是 `Assert.Equal(2, Rejections.Count)` ＋ [0]＝`NarrativeProseMissing`、[1]＝`ActionBindingMissing`，摘掉 `:131` 后计数变 1 ⇒ 本格翻。**连坐风险由我事前点名**：波 22 审计把 `:131` 标为"也是 #23 走的路径之一"，而 #23（@520）已在波 41 签账——lane 必须自己按当前 blob 推"@520 传的是默认非空白 prose 因而读不到 `:131`"这条路径并落表，闭合不了就按件 3′ 退回，不许拿"应该不影响"过关 |

判定门＝每轮**恰 1**；实到 ≠ 落档表 ⇒ 整轮作废（件 4），目标格不签、不重跑、不追认。

## 三 前置件（每件都要成品）

- **件 1＝改前整行并排自证**：五枚 OLD 现文与载体 blob 逐字符并排打印＋整行出现恰 1 次＋改前行内容等值断言；不符 ABORT 不写一字节。**SHA 对账不能替代并排自证。**
- **件 2＝突变文本预合成、零 `dotnet` 先核 SHA**：五枚改后文本落 `.local-notes/wave43-1029/mutation-prep/`，SHA-256 与字节数当场进报告 §一；每轮"施加后 SHA"必须逐字符等于该行。
- **件 3＝形状自检**：净括号偏移守恒（不是绝对数相等）＋全文件净圆括号／净大括号收支不变＋行数不变。
- **件 3′＝闭合点名门（硬门）**：每轮起跑前报告里必须已落档**闭合**的预期红集表＝逐枚点名谁会红（夹具如何走到被改行、翻掉的断言行号、消息形状）＋逐枚写明其余每一枚为什么不连坐（可达路径级理由，不许写"应该不影响"）。**枚数逐枚点名，禁止"各恰 N"合并写法。** 闭合不了 ⇒ 不起那一轮、定向额度原样退回、判不清面逐枚登记（波 41 的 c 轮是先例）。禁止用"跑一把看响几枚"代替推演。**闭合表不许抄波 42 §一 的文本，必须本刀自己按载体 blob 重推落档。**
- **件 4＝多响处置**：见 §二 判定门。
- **件 5＝读数口径**：只数 `<UnitTestResult … outcome="Failed">` 开标签并与同份 `<Counters failed>` 双向对账；`<ResultSummary outcome="Failed">` 不算用例；属性按名字取不按顺序、**取回的属性值要解 XML 实体**（波 42 的仪器自证就是栽在 `&quot;` 上）；栈帧锚点＝`in <路径>:line N`（TRX 里没有 `at ` 可锚，且路径含盘符冒号，切分不许用 `[^:]`）；跳过格只认 `outcome="NotExecuted"` 开标签；`testName` 是全限定＋参数表，回判方法名先剥括号。
- **件 6＝仪器自证（波 38 起，波 41／42 连续使用）**：取数仪器在裁本刀之前必须先对 §一.5 那批带四元限定的老读数复算并逐面 AGREE；任一面 MISMATCH ⇒ **先证明坏的是表还是仪器**，不许直接用它裁新读数。行号必须标种类（格起始／TRX 断言帧）。波 42 新登记的同族错照抄为纪律：仪器字段名写错会让八面数学全对的情况下集体报警——报警不等于盘坏了。
- **件 7＝负向断言要指明见证面**：写"某选项没出现"之前先证明那个字段在这份归档里本来会出现（`run-*.txt` 是 stdout，不含命令行 ⇒ 那里"没有 `--no-build`"不是凭据；正面凭据是构建产出行 ` -> ….dll` 在场）。
- **件 8＝命令形状自检（波 42 买下来的新硬门）**：每一轮定向跑**之前**，把将要执行的完整命令行原样落档为 `out/cmd-<轮>.txt`，并逐字符核对含 `--logger trx` 与结果目录开关；跑完**立刻**当场打印该目录的 `.trx` 枚数，枚数＝0 ⇒ 本轮即刻登记为"无凭据轮"，不许回头往 stdout 找凭据、不许补跑第二轮救同一轮、不许为凑一次 F 多弹防火墙。**上一把就是在这一枚上亏了 1 枚未领格＋1 次全量读数。**

## 四 预算（超限即停，不自我扩额）

1. 解决方案级 `dotnet build` ≤1（默认按三段判定合法跳过：0 行式子成立＋`Pry.Core.dll` 成品在盘＋三枚 SHA 三面未变；注意 §一.7 那条 dll 清洁度限定）。
2. 解决方案级全量 `dotnet test Pry.slnx -c Release` ≤1：**五轮全部起跑且全部有效才起**；任一轮作废、退回或无凭据 ⇒ 立即不起（波 42 的先例）。
3. 定向 `dotnet test --filter` ≤5。
4. `dotnet` 总调用 ≤6。
5. HOLD 重跑 ≤16（按刀计，两次之间 ≥5 分钟）。
6. 预检编号从 **P121** 起，逐份归档；"逐份 VERDICT 枚数＝`dotnet` 次数＝实跑轮数"三方对账不许省。

## 五 交回守门人核的事（逐件给当场读数，不代裁）

1. 五轮各自实到红枚数与红名／TRX 断言帧行／消息族，是否逐字符等于 §一 落档的闭合表；哪一轮作废、依据是什么。
2. 每轮的 `out/cmd-<轮>.txt` 与跑后 `.trx` 枚数（件 8 的两面），逐轮列出。
3. 判据文件 SHA 是否每轮两侧＋出场面都当场打印；没打印的轮次，替代面写清楚（0 行＋载体 HEAD 未动是 blob 同值的充分面，但要明写这是替代面）。
4. 载体终态三件（`status --short` 行数／`fd93ebe..HEAD` 行数／`rev-parse HEAD`）＋被改文件的三面 SHA。
5. 收尾全量（若起了）：四份先数满再逐份 `<Counters>` 相加，写出求和式；两条跳过按 `outcome="NotExecuted"` 开标签逐字符点名。
6. 盘上 `.trx` 枚数、结果目录枚数、有无 `-voided-`；每轮 `total` 与 §一.4 当场格数的对账。
7. 四类预算各自动用与上限。
8. 每一枚红的性质（断言级红／整格异常红，逐枚点名；h′ 轮尤其要如实登记形状）。
9. 未领账与判不清面逐枚登记（尤其 i′ 轮"另一处 `TargetConversationId`"的点名、j′ 轮 #23 不连坐的路径面）。

## 六 不许走的捷径

1. 不许把"目标格没响"写成"这枚门结构上够不着"而不试——先穷举方向类突变（改恒假／去掉取反／换常量／拆合取半边），再把剩下的登记为判不清（波 28 两条就是这样被证伪的）。
2. 不许用 `if (false)` 之外的形状去"顺手改宽"多条断言；一行就是突变的一行。
3. 不许为凑一次 F 读数多弹一次防火墙（§四.2 是硬条件）。
4. 不许拿本刀的绿去"顶替"上一把的欠账条目，也不许减少任何在册欠账（欠账只随签账减少）。
5. 不许在报告里写归位／顶账／基线移动。
6. **不许用 stdout 面签署任何一格**（波 42 §九.2 已把这条裁死：stdout 没有逐枚可枚举节点面，看不见多响）。

## 七 后面的刀（不在本刀范围）

T3 余下（#16／#18／#21／#28／#34／#35／#36／#45／#46／#48，其中 #45／#28 的历史行号已实测漂移、#34／#36 与波 14 成对同门＝一次全量两类同跑会两枚同响，那属全量面不属定向面）；T4 那 5 枚（#12／#19／#20／#41／#47＝要新增形状或夹具改造，另议）；#49（T5 拆门无人守）、#31（那只手写过的判据字节会作废凭证）；波 20 那 28 枚；`used + cost > total` 那一半；`:367`；探针正数面的单独读数；HDS-007 的 `:195`／`:172`；波 28 那枚数值锁格；`PrototypeFiles` 写死清单的覆盖盲区；两套并行命名债；HDS-010 删除片；HDS-003／007／009 的生产接线（属接线刀不属证据刀）。
