# 工单 HDS-WAVE36-1028｜HDS-009 第一片第三刀的剩余七轮：把波 34 那 4 枚仍无红的格逐枚取红＋结清那条断言级子账＋用"注入样本"这条路拿下两枚扫描格的红证与第二换手（续跑证据刀，零产品字节终态、零归位、零顶账）

- 工单 id：`HDS-WAVE36-1028`
- 性质：**证据刀·续跑**（第三十五波只跑到 3/10 轮，HOLD 重跑额度 8/8 用尽；本刀接着跑剩下的七轮＋收尾全量，**复用同一棵载体、复用已经在盘上的十枚预合成突变文本，不重做前置**）
- 任务编号：`HDS-009`（登记处＝`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS-009 行；本刀的出处＝`docs/eval/reports/2026-10-28-hds009-wave35-red-proof-handover.md` 守门人 §九.2 的现账与 §九.5 的押账）
- 出闸：守门人 2026-10-28 22:2x。作者原令逐字＝「我不想思考了，你就继续给我派工单，该提交的提交吧」＋「你，还是自己起Agent吧」＋「继续工作，不要让我拍板啥的，告诉我现在是什么情况就行」＋「下一次不要再三构建触发那个防火墙烦我打游戏」＋「游戏基本都是全屏应用，看当前桌面焦点是不是全屏应用就行」
- 执行方：冷启动 lane（载体＝沿用第三十三波那棵，**不新建 worktree**）
- **这一刀对作者可见的东西＝零**：零新增判据格、产品字节终态净写入 0、界面与对话里什么都不会变；它填的是"改坏了没人知道"那一格里剩下的 4 枚无红格＋1 条断言级子账＋2 枚扫描格的换手。

## 〇 开工前必读与入场（缺一条就就地停刀登记，不要自扩范围）

1. **先读八份**：`docs/plans/README.md`、`docs/plans/PROJECT_PLAN.md`（§4.2 的 HDS-009 行＋§9 最新一条 entry）、`docs/agent-engineering-standards.md`（全文，交付形态按 §10 四项）、`docs/dispatch-hds-wave35-1028.md`（上一把的工单：§一 预算口径、§三.0 事前论证表＝本刀七轮的**权威论证原文**）、`docs/eval/reports/2026-10-28-hds009-wave35-red-proof-handover.md`（**必读 §三.0 表＋§三.A 三轮十一项写法＋守门人 §九.0／§九.1／§九.2／§九.3／§九.5**）、`src/Pry.Core/Narrative/StateAndLearning/` 那五枚产品文件（本刀要改坏的就是它们，逐行读）、`tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs`（判据本体 26 枚格；**本刀对它零字节写入**）、`.local-notes/wave35-1028/mutation-prep/` 与 `.local-notes/wave35-1028/w35-mutate.py`／`w35-count.py`（上一把留下的突变器与仪器，本刀可复用，也可自写，但**自写必须先按 §一.4 自证**）。
2. **入场读数当场贴进报告 §〇**（不许引用本工单的数字）：`date`、`git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD`、`git status --short --branch` 原样、`git worktree list`、`git log --oneline -3`。
3. **载体＝沿用第三十三波那棵**：`.local-notes/qa-wave33-1028/carrier`（基点 `fd93ebe`、线尾应为 `51f149d0b722e28a6566c93d19f48a94ca4c85df`）。入场自证四件贴进 §〇：① 载体 `rev-parse HEAD`；② 载体 `status --short`＝**0 行**；③ 载体 `log --oneline fd93ebe..HEAD`＝**2 行**；④ 载体 `diff --stat fd93ebe HEAD -- src tests scripts Pry.slnx Directory.Build.props`＝**6 files changed, 822 insertions(+), 0 deletions**。再加主树侧那条：`git -C C:/Users/24782/Desktop/Pry_Project diff --stat 51f149d $(git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD) -- src tests scripts Pry.slnx Directory.Build.props`＝**0 行**（右端当场取，不许在本工单里预写 SHA）。这条成立⇒载体树与主线归位后的产品／判据字节逐字符相同，本刀在载体里取到的红就是主线那棵树的红。
4. **六枚文件的入场 SHA-256 基准**：一律当场 `sha256sum` 三侧各取一次（载体工作副本＝`git show 51f149d:<path>` 的 blob＝主树工作副本），三侧逐字符相同才允许起跑；判据文件即"上一把全程对齐的那枚"。**判据文件全程不许有一个字节写入**＝本刀七轮能签第二换手的唯一凭据。
5. **在册基线起点＝当场从波 35 报告守门人 §九.2 读到的那个数**（波 33 归位后由守门人复采、波 34 同值复采过一次的 **1406／executed 1404／failed 0／跳过 2**，分项 Client 108／App 233／Api 280／Core 785、Core executed 783）。本刀**新格数＝0**，所以 F 轮出口读数应当**仍是 1406**：读到别的数先怀疑树没还原干净，不改判据、不改基线、照实登记并作废那一轮。
6. **他人资产**：主树那两枚 `M ` 暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）与全部 `??`＝别人的，不读内容、不改、不提交、不 `stash`／`clean`／`rm`。**共享主树里禁止任何 `git commit`／`add`／`checkout`／`reset`／`cherry-pick`／`merge`／`tag`／`push`／`git apply`**；写只许落在载体里（临时突变、逐轮按字节还原）＋你自己那一份报告文件。**本刀归位是空转**：终态净写入 0 字节⇒无代码刀、守门人不做按路径落地，`main` 也不因本刀挪。
7. **焦点预检（这条会打断作者本人，违反＝本波作废）**：每一次 `dotnet`（含每一枚突变轮与收尾全量）之前先跑

   ```
   powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1
   ```

   整段输出贴进报告并归档到 `.local-notes/wave36-1028/out/preflight-<ID>.txt`，**编号从 P72 起**（P1–P71 已被前九把用掉）。只有 `VERDICT=BUILD_OK` 才起跑。读到 `HOLD`：**不起跑、不请示、不改判据、不绕过**；先把零 `dotnet` 的活做完（写 §〇、复核 §三.0 那七行论证与盘上预合成文本的 SHA、把仪器自证跑掉），然后**可以重跑预检**——**预检本身不设上限，只对"HOLD 之后的重跑"设上限：最多累计 16 次、相邻两次不短于 5 分钟**（额度按"刀"计；这比上一把的 8 次宽，理由＝波 35 实测窗口短促、8 枚重跑约 45 分钟就吃光而只榨到 3 轮，预检脚本本身只读、不打扰作者，真正打扰他的是 `dotnet` 那一下弹窗。**HOLD 的语义一字未改**，不许把它读成"可以多试几次那就起跑吧"）。16 次后仍 `HOLD` ⇒ **带着"本刀零 `dotnet` 起跑"的交付回来，那是合法交付**，报告写清跑到哪一行、下一手从哪一行接着跑。**窗口一开就把能跑的轮次连着跑完**（每轮各自一枚预检，不许省），因为下一分钟游戏可能就回到前台。`free_memory_gb` 偏低或读数呈 SQLite `disk I/O`／`out of memory` 形态时那一轮读数作废（那是他机器的资源形状，不是断言失败），不动任何进程。

## 一 预算与口径

1. **预算**：解决方案级构建 **≤1**（若 §〇.3 那条 0 行式子成立、且载体 `src/Pry.Core/bin/Release/net10.0/Pry.Core.dll` 在盘、六枚 SHA 未变，**允许跳过并登记判定**——波 34／波 35 都这么做）；解决方案级全量 **≤1**；定向 **≤7**；`dotnet` 总调用上限＝**8**。七轮一律**不许带 `--no-build`**（自行增量重建是唯一能保证"跑的是当前源码"的写法；波 33 那把为这件事作废过一轮）。
2. **`全量 ≤1` 的语义写死＝"交付的读数只许来自 1 次解决方案级全量"**，不是"这台机器今天只准起一次全量"。作废轮（§一.3）的重跑不占这一格，但每轮全量都要逐轮登记：第几次调用、结果目录名、TRX 枚数、`<Counters>` 求和。
3. **作废轮的产物一律改名留档，不许清空重跑**：目录命名 `trx-<轮>-voided-<预检号>`。**盘上产物必须与真实轮数一一对应**；报告 §六 要有"盘上 `.trx` 枚数＝登记轮数×每轮枚数"的自校（本刀满额形状＝定向 7×1＋全量 1×4＝**11 枚**）。
4. **取数仪器（口径⓪：先自证再用）**：只数 `<UnitTestResult … outcome="Failed">` 的**开标签**并与同份 `<Counters failed>` 双向对账；`<ResultSummary>` 与 `<Output>` 不算用例；抽红格名／消息／行号**不许用"开标签—闭合标签配对"的正则**（那批结果结点绝大多数是自闭合 `…/>`）。**必须再加第三面**＝红格名先剥掉全限定前缀与 `[Theory]` 的参数表，再回判据源码按"方法名＋`(`"验真实存在。**起跑第一轮之前先拿盘上已知读数自证仪器**，七面全对才允许用它裁这一把：`.local-notes/wave33-1028/out/trx-M1`＝恰 1、`trx-M5`＝恰 1、`trx-M7`＝恰 1、`trx-F`＝4 份且 `failed` 全 0（total 求和应为 1406／executed 1404／passed 1404，两条 `NotExecuted` 按开标签点名）、`.local-notes/wave34-1028/out/trx-A1`＝恰 2、`trx-A6`＝恰 1（红名逐字符含 `(suppressed: True, deduplicated: False, rejected: False)`）、`.local-notes/wave35-1028/out/trx-A1|trx-A2|trx-A3`＝各恰 1（红点 `:64`／`:115`／`:195`）。任一不符＝仪器坏了，先修仪器，不改判据。
5. **报告先建、逐轮落盘**（150 轮硬上限，一夜三把栽在这格上）：动第一次 `dotnet` 前把 §〇 写满建文件（**报告路径＝`docs/eval/reports/2026-10-28-hds009-wave36-red-handover-remainder.md`，主树里唯一允许新建的文件，不提交、不 `git add`**），之后每完成一轮立刻把该轮十一项追加进报告再跑下一轮。十一项＝①文件:行 ②改前原文 ③改后原文 ④`git diff --stat` ⑤跑法整行 ⑥TRX 文件名 ⑦红格全名＋消息要点＋红点行号 ⑧还原命令 ⑨还原后 `git status --short` 行数 ⑩突变态与还原后 SHA-256＋**本轮判据文件 SHA-256（apply 侧与 revert 侧各一次）** ⑪当轮预检整段。缺 ⑨⑩⑪ 退回不补签。**⑩ 的"还原后应为"值按载体侧取**＝`git show 51f149d:<path>` 的 blob；判据每轮都要报且七轮全应等于 §〇.4 那个值。
6. **定向跑法固定**（逐字照抄只换结果目录名）：`dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave36-1028/out/trx-<轮>" --logger trx`。**过滤器只圈这一枚判据类**＝本刀 C 类的形状判定范围。

## 二 允许的文件面（超出即作废）

- ① `.local-notes/wave36-1028/`（自建，gitignored）里的报告草稿、仪器（可复用上一把的 `w35-mutate.py`／`w35-count.py`，也可自写）、manifest 与全部 TRX；
- ② 报告 `docs/eval/reports/2026-10-28-hds009-wave36-red-handover-remainder.md`（唯一一份可写文档）；
- ③ **载体内**的 `src/Pry.Core/Narrative/StateAndLearning/*.cs`——只作临时突变、逐轮按字节还原，终态相对 `51f149d` 净写入 0 字节；
- ④ **禁止**：新建任何产品文件、新建任何测试文件、**写 `tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs` 一个字节**、改 `scripts/**`／`Pry.slnx`／`Directory.Build.props`／其他 `docs/**`（除②那一枚）／主树任何文件；不许往既有测试文件加行（尤其他人名下那枚 `tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）。

## 三 本刀要跑的七轮（事前论证的权威原文＝波 35 报告 §三.0，本刀不重做、不改写）

### 三.0 七轮表（下表右端逐字符抄自波 35 报告 §三.0 与 `mutation-prep/` 盘上现值；**"预期枚数／格名"是假设不是结论**：实测形状与之不符⇒整轮作废，红不署、不追第二轮、不许把本节改成实测值）

| 轮 | 类别 | 文件:行（`51f149d` 侧） | 盘上预合成文本（`mutation-prep/`） | 突变态 SHA-256（本刀当场复算＝下表值才起跑） | 预期恰响格（逐字符，前缀 `Pry.Core.Tests.StateAndLearningPrototypeTests.`） | 枚数 |
|---|---|---|---|---|---|---|
| A4 | 行为 | `DeliveryAttribution.cs:60` 只摘合取第三支 `\|\| evidence.BodyRejected` | `A4_DeliveryAttribution.cs.cs` | `964070d22df97da4a061e90ebeeca9e9da441b663ea2b6a0f552914393b1e80f` | `A_delivered_body_that_was_suppressed_deduplicated_or_rejected_is_not_attribution_evidence(suppressed: False, deduplicated: False, rejected: True)` | 恰 1 |
| A5 | 行为 | `ExperienceMaterial.cs:52` 把 `match.Material` 换成 `null` | `A5_ExperienceMaterial.cs.cs` | `ae9235a2b950bab2020ff997bd3357691328189390652fe50a905881bf179b4a` | `Experience_within_the_input_validity_window_is_offered_as_optional_material` | 恰 1 |
| A6 | 文本面 | `ExperienceMaterial.cs:50` 把 `"experience_past_validity"` 改成另一串 | `A6_ExperienceMaterial.cs.cs` | `1fcfcd828b9a0d1977c8d5b576d55f19689292feac469d68bc4d9873782bbf5c` | `Experience_past_the_input_validity_window_is_not_offered` | 恰 1 |
| A7 | 行为 | `LegacyInheritance.cs:57` 把 `input.Source.SourceId` 换成 `input.StateKey` | `A7_LegacyInheritance.cs.cs` | `51c9cb9c29e8745f0e79e4468fc73ab3344afd11c3fca2428e7705ebe3ba9252` | `A_successful_inheritance_carries_its_source_reference_and_version` | 恰 1 |
| A8 | 文本面 | `LegacyInheritance.cs:53` 把 `"inherited_source_missing"` 改成另一串（**＝波 34 §九.3 那条断言级子账的方向**） | `A8_LegacyInheritance.cs.cs` | `2ff1fa21ed9e275401cffd7827954c0ea1aac48fd030d447d3b9e88dc2dd36a1` | `Inherited_state_without_a_pointable_source_fails_loudly_and_is_never_silently_dropped` | 恰 1 |
| C1 | 注入（红证＋换手） | `StateLayering.cs` 文末追加一行注释 `// MUTATION C1 (temporary, wave35): ReplyGenerationRenderer` | `C1_StateLayering.cs.cs` | `2ab20e48fcb6b47a507eb5d20210685d59ce54df187c8fcea8ec12ea2c84bb7c` | `State_and_learning_prototype_sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites` | 恰 1 |
| C2 | 注入（红证＋换手） | `StateLayering.cs` 文末追加一行注释 `// MUTATION C2 (temporary, wave35): const 7` | `C2_StateLayering.cs.cs` | `d9940f120f9791e1bb635599e953125c0d15a849756416a0cae8662fb82baba4` | `State_and_learning_prototype_declares_no_source_side_numeric_default` | 恰 1 |

**为什么 C1/C2 这一类能同时结"红证＋换手"**：波 34 的 B1/B2 只结红证，是因为那两轮改的是**判据文件自身**（取红那只手当轮写过判据字节＝自证）。本刀走反方向＝**判据字节零写入、把样本串注入被扫的产品文件**（那两个扫描函数按 `Contains` 连注释一起匹配），红点落在前半句 `Assert.Empty`、阳性探针句保持绿。凭据只有 §一.5 那个 ⑩ 项：七轮每一轮判据文件 SHA-256 都等于 `51f149d` 的 blob。**少一次 SHA 就不签那一格。**

**A8 要额外拿到的东西**：波 34 那轮 `Inherited_state_without_a_pointable_source…` 整格红是因为产品侧 `LegacyInheritance.cs:52` 抛 `NullReferenceException`，判据里那句拒因字面量断言（`:365`／`:367`）**从没单独响过**。本刀 A8 只改字符串常量、不改那个 null 路径，若实测红点落在 `:365` 或 `:367`（或二者所在的这条断言序列）才算把那条子账结清；若红点仍在异常那一侧，如实登记"子账未结、形状不符"，由守门人裁。**不许为了让它响而顺手改 `:52`**（那是另一枚突变，不在本刀额度内）。

**跑序固定＝A4→A5→A6→A7→A8（这五轮判据字节必须全程等于 blob 值）→C1→C2→全部还原并核六枚 SHA→F（收尾全量）。** 未跑满 A 类不许起 C 类，未跑满七轮不许起 F（这是波 35 那条"没跑 F 才是对的"的同一把尺）。

## 四 本刀**不**跑的账（押给下一把，不许顺手碰）

1. **A1／A2／A3 不许重跑**：那三枚格已由第三十五波那只手拿到红证与换手（波 35 报告 §三.A 三轮十一项＋守门人 §九.2），重跑＝把别人拿到的红署到自己名下。
2. **波 33 的 M1–M7 换人重取（7 枚格的第二换手）**：配方逐字见波 33 报告 §三.0（M1 `StateLayering.cs:74`／M2 `:92`／M3 `NarrativeAuthority.cs:37`／M4 `DeliveryAttribution.cs:64`／M5 `:88`（`AttributionReadout` 的第 5 枚实参 `UserConsentProven`，由 `false` 翻向 `true`；`:89` 是 `TaskCompletedProven`，**不许一起翻**）／M6 `LegacyInheritance.cs:77`／M7 `ExperienceMaterial.cs:42`）。**本刀不跑**（波 35 §九.3 那条排期改判：M1–M7 从第三十六波改押第三十七波，本刀先把波 35 未完的七轮跑完）。
3. 断言级子账另两条（`BoundedAdvancementPrototypeTests.cs:195`／`:172`，波 32 立的）属 007 那棵载体，与本刀无交集。
4. 波 28 那枚数值锁格（反证只能靠新增数字门槛，零门槛是作者定的，**不许为凑红造门槛**）、两套并行命名债、`PrototypeFiles` 写死五枚清单的扫描盲区、HDS-010 删除片、HDS-007 的生产接线——照旧挂着。

## 五 交回给守门人核的六件事（不在本 lane 权限内）

1. 每轮红格归属逐格点名到 TRX 的 `outcome="Failed"` 开标签节点；**批次红不许署给单枚突变**；本刀七轮**全部预期恰 1**，出现 2 枚以上即整轮作废并如实登记多出来的红名。
2. 载体终态：`status --short`＝**0 行**、`fd93ebe..HEAD` 仍只有波 33 那两枚提交、六枚 SHA 逐字符等于 §〇.4 的入场基准。
3. 收尾全量四份 TRX 先数满再求和，应当**仍＝1406／executed 1404／failed 0／跳过 2**（两条同名跳过按 `outcome="NotExecuted"` 点名＝`Configured_llama_server_requires_the_child_environment_key`／`Configured_sense_voice_model_can_run_offline`）。**本刀不顶账**，1406 不因这一把移动，也不许为顶账另起构建。
4. 作废轮登记是否齐（§一.3 改名留档＋`.trx` 枚数与轮数对账：满额＝定向 7×1＋全量 1×4＝11 枚；未跑部分逐枚点名差额去处）。
5. **七轮每一轮的判据文件 SHA-256 是否都等于 `51f149d` 的 blob**——这是本刀七轮全部有签账资格的唯一凭据，C1/C2 两枚尤其如此。
6. 有没有人偷偷新增判据、改产品终态字节、动数字门槛——**净新增格数必须 0**，报告 §二 自证 `[Fact]`／`[Theory]`／`[InlineData]` 三计数与上一把逐位相同（**23／1／3**）。

## 六 未验证（照旧逐条点名，不用"已完成"混过去）

真实模型对照（零读数，需作者一句"可以跑"）；**生产接线**（这 26 枚至今零生产调用方，要接的是 005 的落库面与 004 的生命周期，一律没接；接线时机＝U-9 残句「切换那一步她要不要做什么」仍未拍）；旧数据真实继承（零读取）；UI 可见行为（零读数）；生产库与 `%LOCALAPPDATA%\PryCompanion`（零接触）；待标定参数权威值（关系/心情各档阈值、经验有效期、点击归因窗口全部未定＝**特性不是缺陷**）；并发与断电（零读数）；旧链删除片与"构建时不把旧链打进包"那道门（**未开始**）；Release 序列点把 `Assert.False` 记到它上方那条多行调用起始行的机理（波 35 §九.3 留给本刀的仪器已知形状，不影响署名）；**换手终账不由本 lane 签**＝本刀若七轮全成，结清的应是"4 枚无红格的红证与换手＋1 条断言级子账＋2 枚扫描格的红证与换手"，波 33 那 7 枚 M 系仍欠另一双手的红（§四.2）。旧欠 **75 格**（波 13 余 47＋波 20 余 28）与本刀无交集、照旧不减；拆法原文照旧 46／45／1＝W2。**四句话照旧不许出现在任何页面**＝新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链。

## 七 不许走的捷径（逐条对照前几把踩过的坑）

1. 不许新增判据格、不许改断言、不许改 `[InlineData]` 期望值、不许 `Assert.Skip`、不许注释既有断言、不许改上游门——**这一刀的产物是红，不是格数**。
2. **本刀七轮一律不许写判据字节**（碰判据＝自证，C 类两枚的换手立刻白跑）。
3. 不许把"红不了"当结论而不试；也不许把"没试"写成"够不着"。
4. 不许合轮（一次一枚突变、一枚一轮）。
5. 不许带 `--no-build` 跑 A/C 轮或收尾全量。
6. 不许删作废轮产物；不许清空结果目录重跑。
7. 不许为顶账起构建（本刀根本不顶账）。
8. 不许在报告里替守门人写状态词、结案词，不许自封在册基线。
9. 不许碰他人未提交改动、暂存项、未跟踪资产；共享主树禁裸 `git commit`。
10. 不许顺手跑 §四 那四本押账（尤其不许重跑 A1–A3、不许碰 M1–M7）。
11. 不许因为"HOLD 重跑额度还有"就读成"可以再试一次起跑"——额度只约束**预检脚本**的重跑次数，`dotnet` 只在 `VERDICT=BUILD_OK` 之后起。

## 八 交回与划账（本 lane 写 §〇–§七，守门人追加 §九）

（守门人在此预登记划账面：七件事判定＝六件事＋A8 那条子账的红点落点判定；七轮的签账资格凭据（判据字节全程值）；C1/C2 两枚能否把"红证＋换手"两样一起结清；4 枚无红格的终账；波 33 那 7 枚 M 系的押账（第三十七波）；作废轮与 `.trx` 枚数对账；1406 是否同值复采；HOLD 重跑额度从 8 提到 16 这条仪器参数调整是否被用成放宽。）
