# 工单 HDS-WAVE37-1028｜HDS-009 第一片第五刀：波 33 那 7 枚 M 系格的**第二换手**（取红的手不许是判据作者）＋三条断言级子账（M2b／M5b／M7b）＋一轮收尾全量（续跑证据刀，零产品字节终态、零归位、零顶账）

- 工单 id：`HDS-WAVE37-1028`
- 性质：**证据刀·续跑**（第三十六波已把波 35 未完的七轮跑满并结清那 4 枚无红格；这一把只处理波 33 遗留的最后一本账＝**7 枚格的红证全部出自写判据的那只手**，因此欠"另一双手"）
- 任务编号：`HDS-009`（登记处＝`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS-009 行；本刀出处＝`docs/eval/reports/2026-10-28-hds009-wave36-red-handover-remainder.md` 守门人 §九.2 的终账与 §九.5 的押账）
- 出闸：守门人 2026-10-28 23:0x。作者原令逐字＝「我不想思考了，你就继续给我派工单，该提交的提交吧」＋「你，还是自己起Agent吧」＋「继续工作，不要让我拍板啥的，告诉我现在是什么情况就行」＋「下一次不要再三构建触发那个防火墙烦我打游戏」＋「游戏基本都是全屏应用，看当前桌面焦点是不是全屏应用就行」
- 执行方：冷启动 lane（载体＝沿用第三十三波那棵，**不新建 worktree**）
- **这一刀对作者可见的东西＝零**：零新增判据格、产品字节终态净写入 0、界面与对话里什么都不会变；它填的是"这 7 枚格的红只有作者自己的手见过"这一本账，外加三条"整格红了但承重那句断言没单独响过"的子账。

## 〇 开工前必读与入场（缺一条就就地停刀登记，不要自扩范围）

1. **先读八份**：`docs/plans/README.md`、`docs/plans/PROJECT_PLAN.md`（§4.2 的 HDS-009 行＋§9 最新一条 entry）、`docs/agent-engineering-standards.md`（全文，交付形态按 §10 四项）、`docs/eval/reports/2026-10-28-hds009-state-and-learning-prototype.md`（**§三.0 那七行＝M1–M7 的原始配方与原始论证**，注意它的行号是判据整形前的）、`docs/eval/reports/2026-10-28-hds009-wave36-red-handover-remainder.md`（上一把的十一项写法＋守门人 §九.0／§九.1／§九.2／§九.3／§九.5）、`src/Pry.Core/Narrative/StateAndLearning/` 那五枚产品文件（本刀要改坏的就是它们，逐行读）、`tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs`（判据本体 26 枚格；**本刀对它零字节写入**——这条是本刀全部签账资格的唯一凭据）、`.local-notes/wave37-1028/mutation-prep/` 与 `.local-notes/wave37-1028/gk37-prep.js`（**守门人自己预合成的十枚突变文本与它们的 SHA，本刀只应用、不重造**）。
2. **入场读数当场贴进报告 §〇**（不许引用本工单的数字）：`date`、`git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD`、`git status --short --branch` 原样、`git worktree list`、`git log --oneline -3`。
3. **载体＝沿用第三十三波那棵**：`.local-notes/qa-wave33-1028/carrier`（基点 `fd93ebe`、线尾应为 `51f149d0b722e28a6566c93d19f48a94ca4c85df`）。入场自证四件贴进 §〇：① 载体 `rev-parse HEAD`；② 载体 `status --short`＝**0 行**；③ 载体 `log --oneline fd93ebe..HEAD`＝**2 行**；④ 载体 `diff --stat fd93ebe HEAD -- src tests scripts Pry.slnx Directory.Build.props`＝**6 files changed, 822 insertions(+), 0 deletions**。再加主树侧那条：`git -C C:/Users/24782/Desktop/Pry_Project diff --stat 51f149d $(git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD) -- src tests scripts Pry.slnx Directory.Build.props`＝**0 行**（右端当场取，不许在本工单里预写 SHA）。这条成立⇒载体树与主线归位后的产品／判据字节逐字符相同，本刀在载体里取到的红就是主线那棵树的红。
4. **六枚文件的入场 SHA-256 基准**：一律当场 `sha256sum` 三侧各取一次（载体工作副本＝`git show 51f149d:<path>` 的 blob＝主树工作副本），三侧逐字符相同才允许起跑；判据文件即"波 35／波 36 全程对齐的那枚"。**判据文件全程不许有一个字节写入。**
5. **在册基线起点＝当场从波 36 报告守门人 §九.1.3 读到的那个数**（1406／executed 1404／failed 0／跳过 2，波 34 与波 36 已两次同值复采）。本刀**新格数＝0**，所以 F 轮出口读数应当**仍是 1406**：读到别的数先怀疑树没还原干净，不改判据、不改基线、照实登记并作废那一轮。
6. **他人资产**：主树那两枚 `M ` 暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）与全部 `??`＝别人的，不读内容、不改、不提交、不 `stash`／`clean`／`rm`。**共享主树里禁止任何 `git commit`／`add`／`checkout`／`reset`／`cherry-pick`／`merge`／`tag`／`push`／`git apply`**；写只许落在载体里（临时突变、逐轮按字节还原）＋你自己那一份报告文件。**本刀归位是空转**：终态净写入 0 字节⇒无代码刀、守门人不做按路径落地，`main` 也不因本刀挪。
7. **焦点预检（这条会打断作者本人，违反＝本波作废）**：每一次 `dotnet`（含每一枚突变轮与收尾全量）之前先跑

   ```
   powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1
   ```

   整段输出贴进报告并归档到 `.local-notes/wave37-1028/out/preflight-<ID>.txt`，**编号从 P80 起**（P1–P79 已被前十把用掉）。只有 `VERDICT=BUILD_OK` 才起跑。读到 `HOLD`：**不起跑、不请示、不改判据、不绕过**；先把零 `dotnet` 的活做完（写 §〇、复核 §三.0 那十行论证与盘上预合成文本的 SHA、把仪器自证跑掉），然后**可以重跑预检**——**预检本身不设上限，只对"HOLD 之后的重跑"设上限：最多累计 16 次、相邻两次不短于 5 分钟**（额度按"刀"计；HOLD 的语义一字未改，不许把它读成"可以多试几次那就起跑吧"）。16 次后仍 `HOLD` ⇒ **带着"本刀零 `dotnet` 起跑"的交付回来，那是合法交付**，报告写清跑到哪一行、下一手从哪一行接着跑，**收尾全量不跑＝基线不移动，这不是短处**。窗口一开就把能跑的轮次连着跑完（每轮各自一枚预检，不许省）。`free_memory_gb` 偏低或读数呈 SQLite `disk I/O`／`out of memory` 形态时那一轮读数作废（那是他机器的资源形状，不是断言失败），不动任何进程。

## 一 预算与口径

1. **预算**：解决方案级构建 **≤1**（若 §〇.3 那条 0 行式子成立、且载体 `src/Pry.Core/bin/Release/net10.0/Pry.Core.dll` 在盘、六枚 SHA 未变，**允许跳过并登记判定**——波 34／35／36 都这么做）；解决方案级全量 **≤1**；定向 **≤10**；`dotnet` 总调用上限＝**11**。十轮一律**不许带 `--no-build`**（波 33 那把为这件事作废过一轮）。
2. **`全量 ≤1` 的语义写死＝"交付的读数只许来自 1 次解决方案级全量"**，不是"这台机器今天只准起一次全量"。作废轮（§一.3）的重跑不占这一格，但每轮全量都要逐轮登记：第几次调用、结果目录名、TRX 枚数、`<Counters>` 求和。
3. **作废轮的产物一律改名留档，不许清空重跑**：目录命名 `trx-<轮>-voided-<预检号>`。**盘上产物必须与真实轮数一一对应**；报告 §六 要有"盘上 `.trx` 枚数＝登记轮数×每轮枚数"的自校（本刀满额形状＝定向 10×1＋全量 1×4＝**14 枚**）。
4. **取数仪器（口径⓪：先自证再用）**：只数 `<UnitTestResult … outcome="Failed">` 的**开标签**并与同份 `<Counters failed>` 双向对账；`<ResultSummary>` 与 `<Output>` 不算用例；抽红格名／消息／行号**不许用"开标签—闭合标签配对"的正则**（那批结果结点绝大多数是自闭合 `…/>`）。**必须再加第三面**＝红格名先剥掉全限定前缀与 `[Theory]` 的参数表再回判据源码验"方法名＋`(`"真实存在；**第四面（波 36 新踩的）＝红名与事前论证比对时必须先剥前缀再比，拿全限定串去 `===` 判据里的裸方法名会得到"七轮全不匹配"的假象**。**起跑第一轮之前先拿盘上已知读数自证仪器**，至少九面全对才允许用它裁这一把：`.local-notes/wave33-1028/out/trx-M1`＝恰 1、`trx-M5`＝恰 1、`trx-M7`＝恰 1、`trx-F`＝4 份且 `failed` 全 0（total 求和 1406／executed 1404／passed 1404，两条 `NotExecuted` 按开标签点名）、`.local-notes/wave34-1028/out/trx-A1`＝恰 2、`trx-A6`＝恰 1（红名逐字符含 `(suppressed: True, deduplicated: False, rejected: False)`）、`.local-notes/wave35-1028/out/trx-A1|A2|A3`＝各恰 1（红点 `:64`／`:115`／`:195`）、`.local-notes/wave36-1028/out/trx-A4…trx-C2`＝各恰 1、`trx-F`＝1406 同值。任一不符＝仪器坏了，先修仪器，不改判据。
5. **报告先建、逐轮落盘**（150 轮硬上限，一夜三把栽在这格上）：动第一次 `dotnet` 前把 §〇 写满建文件（**报告路径＝`docs/eval/reports/2026-10-28-hds009-wave37-m-second-hand-red.md`，主树里唯一允许新建的文件，不提交、不 `git add`**），之后每完成一轮立刻把该轮十一项追加进报告再跑下一轮。十一项＝①文件:行 ②改前原文 ③改后原文 ④`git diff --stat` ⑤跑法整行 ⑥TRX 文件名 ⑦红格全名＋消息要点＋红点行号 ⑧还原命令 ⑨还原后 `git status --short` 行数 ⑩突变态与还原后 SHA-256＋**本轮判据文件 SHA-256（apply 侧与 revert 侧各一次）** ⑪当轮预检整段。缺 ⑨⑩⑪ 退回不补签。**⑩ 的"还原后应为"值按载体侧取**＝`git show 51f149d:<path>` 的 blob；判据每轮都要报且十轮全应等于 §〇.4 那个值。
6. **定向跑法固定**（逐字照抄只换结果目录名）：`dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave37-1028/out/trx-<轮>" --logger trx`。
7. **突变的写入通道（本刀与波 36 的唯一差别，必须照做）**：突变文本**由守门人预合成**在 `.local-notes/wave37-1028/mutation-prep/<ID>_<basename>`，lane **只许整文件字节复制**应用（`cp` 或 node `fs.copyFileSync`，不许重排、不许改行尾、不许手写 replacement）；应用后立刻 `sha256sum` 该路径，**必须等于 §三.0 表中该轮的 SHA**，不等即整轮作废并如实登记；还原一律 `git show 51f149d:<path>` 取字节再写文件（不许 `checkout`/`restore`），还原后 SHA 必须等于 §〇.4 基准。**lane 自己的仪器必须硬拒把判据路径当写入目标**（波 36 的 `w36-mutate.py` 有这条守卫，复用它或自写都要证明这条守卫活着：拿一次"试图写判据路径"的调用验它报错，不许只声称）。

## 二 允许的文件面（超出即作废）

- ① `.local-notes/wave37-1028/`（自建，gitignored）里的报告草稿、仪器、manifest 与全部 TRX；**`mutation-prep/` 十枚是守门人产物，lane 只读**；
- ② 报告 `docs/eval/reports/2026-10-28-hds009-wave37-m-second-hand-red.md`（唯一一份可写文档）；
- ③ **载体内**的 `src/Pry.Core/Narrative/StateAndLearning/*.cs`——只作临时突变、逐轮按字节还原，终态相对 `51f149d` 净写入 0 字节；
- ④ **禁止**：新建任何产品文件、新建任何测试文件、**写 `tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs` 一个字节**、改 `scripts/**`／`Pry.slnx`／`Directory.Build.props`／其他 `docs/**`（除②那一枚）／主树任何文件；不许把 §三.0 的突变文本改成"更干净的红形状"（那要动判据或动产品语义，两者都不在本刀范围）。

## 三 本刀要跑的十轮（M1–M7 配方原文＝波 33 报告 §三.0；M2b／M5b／M7b＝守门人从 `51f149d` 干净树新推的断言级方向，见 §三.2）

### 三.0 十轮表（**"预期枚数／格名／红形状"是假设不是结论**：实测形状与之不符⇒整轮作废，红不署、不追第二轮、不许把本节改成实测值。行号一律是 `51f149d` 侧现值，**不要抄波 33 报告里的行号**——那把跑在判据整形之前）

| 轮 | 类别 | 文件:行（干净侧原文） | 突变内容（守门人预合成） | 预合成文本 SHA-256 | 预期恰响格（逐字符，前缀 `Pry.Core.Tests.StateAndLearningPrototypeTests.`） | 预期红点（判据侧行）与红形状 | 枚数 |
|---|---|---|---|---|---|---|---|
| M1 | 摘闸（行为） | `StateLayering.cs:73-75`（拒因一 `FromNarrativeOutput`） | 整段换成一行 `// [MUTATION M1] …`（＝波 33 那只手用过的同一份字节） | `e623855896877c9f120c7adea35c8eceae8ec1b246bccfc2b987cc402ab31c45` | `A_narrative_output_may_not_rewrite_the_stable_canon_layer` | `:50`，`Assert.Equal` 文案 Expected `"Rejected"`／Actual `"Applied"` | 恰 1 |
| M2 | 摘闸（行为） | `StateLayering.cs:90-94`（Overlay 来源闸整道） | 整段换成一行注释（同一份字节） | `23235ae5db95ce6ef19bdf97c2ddbfc1eae609c7293a72feb7a67a25a2c3c60a` | `An_overlay_without_a_pointable_source_and_version_is_refused` | **异常侧**：测试帧 `:90`，产品帧漂到 `:95`（原 `:99` 的 `draft.Source.SourceId` 空引用），消息＝`NullReferenceException` | 恰 1 |
| M2b | 文本面 | `StateLayering.cs:94` 的 `"overlay_requires_pointable_source_and_version"` | 加 `_wave37_alias` 后缀（行数不变） | `75fef21f3b237cf4be81faba456a22326c4fd7aedb7bf0ba1d21552a69605972` | `An_overlay_without_a_pointable_source_and_version_is_refused` | `:95`，`Assert.Equal` 文案 Strings differ | 恰 1 |
| M3 | 摘闸（行为） | `NarrativeAuthority.cs:36-38`（闸②作者边界） | 整段换成一行注释 | `74e2d50523a6f6ead6e58147818dab98184240d890db2d6d69d9edea62fc8859` | `A_narrative_candidate_may_not_modify_the_author_boundary` | `:142`，`Assert.False() Failure` | 恰 1 |
| M4 | 摘闸（行为） | `DeliveryAttribution.cs:63-65`（闸②未投递） | 整段换成一行注释 | `00307f0a4c7df3b070bf5d1f23f4efba204378473a218b765003f44139153e40` | `An_undelivered_body_is_not_attribution_evidence` | `:181`，`Assert.False() Failure` | 恰 1 |
| M5 | 翻布尔 | `DeliveryAttribution.cs:88`（`AttributionReadout` 第 5 枚实参 `UserConsentProven`） | `false,` → `true,`（**只这一行**） | `870064705c4220a714793dcc5b8741f0fdcba4ddc060d554855c165fd1bed96c` | `A_promise_send_proposes_a_commitment_but_proves_neither_consent_nor_completion` | `:244`，`Assert.False() Failure` | 恰 1 |
| M5b | 翻布尔 | `DeliveryAttribution.cs:89`（第 6 枚实参 `TaskCompletedProven`） | `false,` → `true,`（**只这一行，:88 保持 false**） | `676412b758cc121c316bd57953d676effb281775094378fdb227085f267c4b26` | `A_promise_send_proposes_a_commitment_but_proves_neither_consent_nor_completion` | `:245`，`Assert.False() Failure`（`:244` 必须保持通过） | 恰 1 |
| M6 | 翻判定 | `LegacyInheritance.cs:77`（`IsLegacyWritebackAllowed() => false`） | `=> false;` → `=> true;` | `437bde00da315d2f923cbde83fb0004d1da62e5efe8b35ac45fe139302059a79` | `There_is_no_second_write_path_back_to_the_legacy_layer` | `:411`，`Assert.Equal` 文案 Expected `"Refused"`／Actual `"Written"` | 恰 1 |
| M7 | 摘闸（行为） | `ExperienceMaterial.cs:40-43`（有效期入参缺失即明确失败） | 整段换成一行注释 | `2269c0a7e764e05898e6c03f104f178a514f753bcc812a5079381ab524a74564` | `Reading_experience_as_optional_material_without_a_validity_window_fails_loudly` | **异常侧**：测试帧 `:256`，产品帧漂到 `:46`（原 `:49` 的 `validityWindow.Value`），消息＝`InvalidOperationException`（Nullable 取值） | 恰 1 |
| M7b | 文本面 | `ExperienceMaterial.cs:43` 的 `"validity_window_required_as_input"` | 加 `_wave37_alias` 后缀（行数不变） | `3066dfc65a4c59ed2c38a010bbd344c869438f76b32bd4cb08e3387db796a49c` | `Reading_experience_as_optional_material_without_a_validity_window_fails_loudly` | `:259`，`Assert.Equal` 文案 Strings differ | 恰 1 |

**跑序固定＝M1→M2→M2b→M3→M4→M5→M5b→M6→M7→M7b→全部还原并核六枚 SHA→F（收尾全量）。** 未跑满十轮不许起 F；十轮一律不许写判据字节（这是本刀七枚 M 格能签"第二换手"的唯一凭据，凭据只有 §一.5 那个 ⑩ 项：每轮判据文件 SHA-256 都等于 `51f149d` 的 blob）。**每轮都预期恰 1 枚红**；出现 2 枚以上⇒整轮作废、红不署、如实登记多出来的红名，不追第二轮。

### 三.1 我为什么判这七枚"另一双手跑得动"（消费点复算，从干净树逐枚读）

- **M1**：`ReviseCanon` 的 `FromNarrativeOutput=true` 输入在判据里只出现在 `:47` 那一格；`ReviseCanon` 的其余四个调用点（`:60`／`:73`／`:75`／`:105`）全部传 false，被摘那道闸对它们本就不触发 ⇒ 摘闸只动一格。
- **M2**：`WriteOverlay` 只有 `:90`（来源 null）与 `:91`（版本 null）两个入口缺来源，都在同一格；`:108` 那格来源齐，闸在不在都走 `_overlays.Add` ⇒ 只动一格。**注意 `:99` 会解引用 `draft.Source.SourceId`／`.SourceVersion.Value`**，所以摘闸的红是异常侧——这不是"红不了"，也不是判据坏，而是这一行的形状就长这样；断言侧由 M2b 单独拿。
- **M3**：`Assess` 的三个调用点 `:129`／`:140`／`:151`／`:164` 各只置一枚布尔；摘闸② 后 `("c-2", false, true, false)` 落到唯一放行出口 ⇒ `:142` 红；`:125`（亲密）与 `:160`（三面全给）第一道闸先命中，拒因不变 ⇒ 不红。
- **M4**：`Admit` 四个调用点里只有 `:177` 传 `BodyDelivered: false`；理论三行与 `:209`／`:224` 全传 true，被摘那道闸对它们是同一趟 ⇒ 只动一格。
- **M5／M5b**：`AttributePromise` 全判据只有 `:238` 一处调用（其余归因格走 `Admit`，不读这两行）；`:88`／`:89` 是记录 `AttributionReadout` 的第 5／第 6 枚位置实参（`BodyId, Admitted, Reason, CommitmentProposed, UserConsentProven, TaskCompletedProven`），各自单独翻向只打红 `:244`／`:245`。**两枚不许合并成一轮**：合翻只给一枚红而两行都没被单独证过。
- **M6**：`IsLegacyWritebackAllowed()` 的唯一读点是 `:67`，`AttemptWriteback` 的唯一调用点是判据 `:408`；三枚 `Import` 格不读它 ⇒ 只动一格。
- **M7**：`ReadOptional` 五个调用点里只有 `:256` 传 null 窗口；`:269`／`:283`／`:296`／`:297` 全传非 null，摘闸后走 `:49` 的比较（对它们语义不变）⇒ 只动一格。
- **两枚扫描格不受本刀影响**（`Assert.Empty` 的两条谓词只认 `LegacyChainNames` 那七枚名字与 `const `/`static readonly` 两个 token）：十枚预合成文本我当场扫过一遍，**命中 0 个**（`SCAN_GATE_TOKENS=0`），所以每轮的红只可能来自行为／文本面那一格。

### 三.2 M2b／M5b／M7b 这三枚是本工单作者新加的方向（不是扩执行方的范围，是扩我自己的排期）

波 34 到波 36 反复出现同一族形状：**整格因异常而红，不等于承重那句断言响过**。波 33 的 M2／M5／M7 三枚里，M2 与 M7 的红都在异常侧（`:99` 空引用、`:49` Nullable 取值），M5 只证了 `:244` 而 `:245` 从没单独响。所以这一把在七枚原配方之外**追加三枚"只改文本面／只翻另一枚布尔"的配方**，让那三句断言各自拿到一次单独的红。**代价照实登记**：定向从 7 轮增到 10 轮、`dotnet` 上限从 8 增到 11；**这三枚不是波 33 的配方**，所以波 36 §九.5 那句"配方逐字仍见波 33 报告 §三.0"对本刀只适用于 M1–M7，追加面的出处在我这只手。

## 四 本刀**不**跑的账（押给下一把，不许顺手碰）

1. **A 类与 C 类一枚都不许重跑**：波 33–36 那 19 枚双结账里的 13 枚已由别人的手拿到红证与换手，重跑＝把别人拿到的红署到自己名下。
2. **`:367` 那半句断言本刀不追**：`LegacyInheritance.cs:53` 的同一行返回同时喂判据 `:365` 与 `:367` 两次读数，任何单点文本面突变必然先红 `:365`、`:367` 因首断即抛而不执行。要让它单独响只能把两条输入拆成两个不同拒因（＝改判据或扩产品语义），两者都不在证据刀范围。**这条按"这个夹具的输入形状下没有可达配方"签到，不签"结构上永远够不着"**；处置押给接线刀或作者一句"值得拆"。
3. **阳性探针的单独读数**（波 36 §九.3.2 那条）本刀不补：它已由别的轮次随类通过六次，仍欠"单独跑出一次读数"，那要新增判据。
4. `PrototypeFiles` 写死五枚清单的扫描盲区、两套并行命名债（`Hds006*` 产品类型名＋契约 §7.2 的 `CognitionKind`／`CognitionOrigin`）、波 28 那枚数值锁格（**不许为凑红造门槛**）、HDS-007／HDS-009 的生产接线、HDS-010 删除片——照旧挂着，**属接线刀不属证据刀**。

## 五 交回给守门人核的七件事（不在本 lane 权限内）

1. 十轮各自的红是否**恰 1 枚**、红格全名是否逐字符等于 §三.0（含 M5b 那轮的 `:244` 保持绿）；**批次红不许署给单枚突变**。
2. **每轮的判据文件 SHA-256（apply 侧与 revert 侧共 20 枚读数）是否全等于 `51f149d` 的 blob**——这是七枚 M 格换手能否结清的唯一凭据，**少一次读数就不签那一格**。
3. 载体终态：`status --short`＝**0 行**、`fd93ebe..HEAD` 仍只有波 33 那两枚提交、六枚 SHA 逐字符等于 §〇.4 的入场基准。
4. 收尾全量四份 TRX 先数满再求和，应当**仍＝1406／executed 1404／failed 0／跳过 2**（两条同名跳过按 `outcome="NotExecuted"` 点名）。**本刀不顶账**，也不许为顶账另起构建。
5. 作废轮登记是否齐（§一.3 改名留档＋`.trx` 枚数与轮数对账：满额＝定向 10×1＋全量 1×4＝14 枚；未跑部分逐枚点名差额去处）。
6. 有没有人偷偷新增判据、改产品终态字节、动数字门槛——**净新增格数必须 0**，报告 §二 自证 `[Fact]`／`[Theory]`／`[InlineData]` 三计数与上一把逐位相同（**23／1／3**）。
7. §一.7 那条写入通道的证明：lane 是否拿"试图写判据路径"的调用真的验出报错（整段原文），而不只是在报告里声称。

## 六 未验证（照旧逐条点名，不用"已完成"混过去）

真实模型对照（零读数，需作者一句"可以跑"）；**生产接线**（这 26 枚至今零生产调用方，要接的是 005 的落库面与 004 的生命周期，一律没接；接线时机＝U-9 残句「切换那一步她要不要做什么」仍未拍）；旧数据真实继承（零读取）；UI 可见行为（零读数）；生产库与 `%LOCALAPPDATA%\PryCompanion`（零接触）；待标定参数权威值（关系/心情各档阈值、经验有效期、点击归因窗口全部未定＝**特性不是缺陷**）；并发与断电（零读数）；旧链删除片与"构建时不把旧链打进包"那道门（**未开始**）；`:367` 的独立读数（§四.2，本刀刻意不追）；阳性探针的单独读数；Release 序列点把红点记到多行调用起始行的机理（波 35 §九.3／波 36 §九.1.1 已知形状，不影响署名）；**换手终账不由本 lane 签**＝本刀若十轮全成，结清的应是"7 枚 M 格的第二换手＋3 条断言级子账"，别写成"26 枚全部结案"。旧欠 **75 格**（波 13 余 47＋波 20 余 28）与本刀无交集、照旧不减；拆法原文照旧 46／45／1＝W2。**四句话照旧不许出现在任何页面**＝新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链。

## 七 不许走的捷径（逐条对照前几把踩过的坑）

1. 不许新增判据格、不许改断言、不许改 `[InlineData]` 期望值、不许 `Assert.Skip`、不许注释既有断言、不许改上游门——**这一刀的产物是红，不是格数**。
2. **本刀十轮一律不许写判据字节**（碰判据＝自证，七枚 M 格的换手立刻白跑）。
3. **不许为了让 M2／M7 的红"更像断言侧"而改产品语义**（例如给 `:99` 补 `!` 或给 `:49` 补夹子）：异常侧就是这一行的形状，断言侧由 M2b／M7b 负责。
4. 不许把 §三.0 的预期列改成实测值；实测与预期不符⇒整轮作废并如实登记。
5. 不许合轮（一次一枚突变、一枚一轮；尤其 M5 与 M5b 不许并成一轮）。
6. 不许带 `--no-build` 跑 M 轮或收尾全量。
7. 不许删作废轮产物；不许清空结果目录重跑。
8. 不许为顶账起构建（本刀根本不顶账）。
9. 不许在报告里替守门人写状态词、结案词，不许自封在册基线。
10. 不许碰他人未提交改动、暂存项、未跟踪资产；共享主树禁裸 `git commit`。
11. 不许顺手跑 §四 那四本押账；**HOLD 重跑额度只约束预检脚本的重跑次数**，`dotnet` 只在 `VERDICT=BUILD_OK` 之后起。

## 八 交回与划账（本 lane 写 §〇–§七，守门人追加 §九）

（守门人在此预登记划账面：① 十轮形状与红名归属逐格点名；② 二十枚判据 SHA 读数是否齐＝七枚 M 格换手能否签的凭据；③ 三条断言级子账（`:95`／`:245`／`:259`）能否结清，以及 M2／M7 的异常侧红在"格子口径"上仍算不算已证（算——红证的定义是"在改坏产品码之下响过"）；④ 26 枚终账从 19／7／0 是否变到 26／0／0，**注意别把 M2／M2b、M5／M5b、M7／M7b 的三对读成六枚格**（同一格的第二条因，格子口径不重复计数）；⑤ 收尾全量 1406 是否第三次同值复采（本刀不顶账）；⑥ 作废轮与 14 枚 `.trx` 对账、构建 0/1 的跳过判定三段是否成立；⑦ 定向 10 轮／`dotnet` 11 次是否越预算，HOLD 额度 16 是否被用成放宽；⑧ `:367` 与阳性探针两条押账照旧不动。）
