# 工单 HDS-WAVE35-1028｜HDS-009「状态与显式学习迁移」第一片第三刀：给第三十四波那 7 枚无红格逐枚取红，并用"往被扫产品文件注入样本"这条路结清两枚扫描格的第二换手（续跑证据刀，零产品字节终态、零归位、零顶账）

- 工单 id：`HDS-WAVE35-1028`
- 性质：**证据刀·续跑**（复用第三十三／三十四波那棵载体的产品码；**不许新增产品文件、不许动判据字节、产品字节只许"临时突变＋逐轮按字节还原"**；零新增数字门槛；**零落地、零归位、零顶账**）
- 任务编号：`HDS-009`（登记处＝`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS-009 行；本刀的出处＝`docs/eval/reports/2026-10-28-hds009-wave34-red-proof-continuation.md` 守门人 §九.2 的终账与 §九.3 那条断言级子账）
- 出闸：守门人 2026-10-28 20:4x。作者原令逐字＝「我不想思考了，你就继续给我派工单，该提交的提交吧」＋「你，还是自己起Agent吧」＋「继续工作，不要让我拍板啥的，告诉我现在是什么情况就行」＋「下一次不要再三构建触发那个防火墙烦我打游戏」＋「游戏基本都是全屏应用，看当前桌面焦点是不是全屏应用就行」
- 执行方：冷启动 lane（载体＝**沿用第三十三波那棵**，不新建 worktree）
- **起跑前置**：一次 `VERDICT=BUILD_OK` 的焦点预检窗口。读到 `HOLD` 就按 §〇.6 办，不许起跑、不许请示、不许改预检判据。
- **这一刀对作者可见的东西＝零**。它不新增任何一格判据、产品字节终态净写入 0，界面上、对话里什么都不会变；它填的是"改坏了没人知道"那一格里剩下的 7 枚，外加两枚至今只有自证红的扫描格。

## 〇 开工前必读与入场（缺一条就就地停刀登记，不要自扩范围）

1. **先读八份**：`docs/plans/README.md`、`docs/plans/PROJECT_PLAN.md`（§4.2 的 HDS-009 行＋§9 最新一条 entry）、`docs/agent-engineering-standards.md`（全文，交付形态按 §10 四项）、`docs/dispatch-hds-wave34-1028.md`（上一把的工单：§三 那张表就是本刀 A 类的**格式**样板，§四 那张表就是本刀要跑的**方向**出处）、`docs/eval/reports/2026-10-28-hds009-wave34-red-proof-continuation.md`（**§三 各轮十一项写法＋守门人 §九.0／§九.2／§九.3 必读**）、`docs/eval/reports/2026-10-28-hds009-state-and-learning-prototype.md` §三.0（波 33 的 M1–M7 事前论证表——那七枚红的配方本刀不重取，押给第三十六波，别顺手跑），`src/Pry.Core/Narrative/StateAndLearning/` 那五枚产品文件（本刀要改坏的就是它们，**逐行读**）、`tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs`（判据本体，26 枚格；**本刀对它零字节写入**）。
2. **入场读数当场贴进报告 §〇**（不许引用本工单的数字）：`date`、`git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD`、`git status --short --branch` 原样、`git worktree list`、`git log --oneline -3`。
3. **载体＝沿用第三十三波那棵**：`.local-notes/qa-wave33-1028/carrier`（基点 `fd93ebe`、线尾 `51f149d0b722e28a6566c93d19f48a94ca4c85df`）。**不许新建 worktree**，复用它的 `bin`／`obj` 正是省掉作者一次构建的地方。入场自证三件贴进 §〇：① 载体 `rev-parse HEAD`＝上面那个全号；② 载体 `status --short`＝**0 行**；③ 载体 `diff --stat fd93ebe HEAD -- src tests scripts Pry.slnx Directory.Build.props`＝只有波 33 那六枚新文件、**+822/−0**。再加一件本刀专属：**主树侧** `git -C C:/Users/24782/Desktop/Pry_Project diff --stat 51f149d HEAD -- src tests scripts Pry.slnx Directory.Build.props`＝**0 行**（右端当场 `git rev-parse HEAD` 取，不许在本工单里预写 SHA）。这条成立 ⇒ 载体树与主线归位后的产品／判据字节逐字符相同，本刀在载体里取到的红就是主线那棵树的红。
4. **六枚文件的入场 SHA-256 基准（本刀逐轮 ⑩ 项要复核对齐；一律当场 `sha256sum` 取，不许从本工单抄）**：五枚产品文件与判据文件当前值即波 34 报告 §〇.3 那六个前缀（`af727a9c`／`000f2fc8`／`5858a75d`／`289fc89b`／`6a1f5099`／判据 `317d2171`）。**判据文件全程不许有一个字节写入**＝本刀能签第二换手的唯一凭据。
5. **在册基线起点＝当场从上一把报告 §七.3／守门人 §九.1 读到的那个数**（守门人复采过的是 **1406／executed 1404／failed 0／跳过 2**，分项 Client 108／App 233／Api 280／Core **785**、Core executed **783**）。本刀**新格数＝0**（§二 明令），所以出口读数应当**仍是 1406**：收尾那轮若读到别的数，先怀疑树没还原干净，不改判据、不改基线、照实登记并作废那一轮。
6. **他人资产**：主树那两枚 `M ` 暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）与全部 `??`＝别人的，不读内容、不改、不提交、不 `stash`／`clean`／`rm`。**共享主树里禁止任何 `git commit`／`add`／`checkout`／`reset`／`cherry-pick`／`merge`／`tag`／`push`／`git apply`**；写只许落在载体里（临时突变、逐轮还原）＋你自己那一份报告文件。**本刀归位是空转**：终态净写入 0 字节 ⇒ 无代码刀、守门人不做按路径落地，`main` 也不因本刀挪。
7. **焦点预检（这条会打断作者本人，违反＝本波作废）**：每一次 `dotnet`（含每一枚突变轮与收尾全量）之前先跑

   ```
   powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1
   ```

   整段输出贴进报告并归档到 `.local-notes/wave35-1028/out/preflight-<ID>.txt`，**编号从 P60 起**（P1–P59 已被前八把用掉）。只有 `VERDICT=BUILD_OK` 才起跑。读到 `HOLD`：**不起跑、不请示、不改判据、不绕过**；先把零 `dotnet` 的活做完（写 §〇、逐枚读码、把 A/C 十枚突变文本先在临时副本上合成并 `sha256sum` 记下），然后**可以重跑预检**——**预检本身不设上限，只对"HOLD 之后的重跑"设上限：最多累计 8 次、相邻两次不短于 5 分钟**（额度按"刀"计）。8 次后仍 `HOLD` ⇒ **带着"本刀零 `dotnet` 起跑"的交付回来，那是合法交付**，报告写清跑到哪一行、下一手从哪一行接着跑。`free_memory_gb` 偏低或读数呈 SQLite `disk I/O`／`out of memory` 形态时那一轮读数作废（那是他机器的资源形状，不是断言失败），不动任何进程。

## 一 预算与口径（沿用波 34 那六条，一处收紧）

1. **预算**：解决方案级构建 **≤1**（若 §〇.3 那条 0 行式子成立、且载体 `bin/Release/net10.0/Pry.Core.dll` 在盘上且六枚 SHA 未变，**允许跳过并登记判定**——波 34 的 §〇.6 就是这么做的）；解决方案级全量 **≤1**；定向 **≤8（A 类）＋≤2（C 类）＝单把 ≤10**。`dotnet` 总调用上限＝**12**。A 类与 C 类一律**不许带 `--no-build`**（自行增量重建是唯一能保证"跑的是当前源码"的写法；波 33 那把为这件事作废过一轮）。
2. **`全量 ≤1` 的语义写死＝"交付的读数只许来自 1 次解决方案级全量"**，不是"这台机器今天只准起一次全量"。作废轮（§一.3）的重跑不占这一格，但每轮全量都要逐轮登记：第几次调用、结果目录名、TRX 枚数、`<Counters>` 求和。
3. **作废轮的产物一律改名留档，不许清空重跑**：目录命名 `trx-<轮>-voided-<预检号>`。**盘上产物必须与真实轮数一一对应**；报告 §六 要有"盘上 `.trx` 枚数＝登记轮数×每轮枚数"的自校。
4. **取数仪器（口径⓪：先自证再用）**：只数 `<UnitTestResult … outcome="Failed">` 的**开标签**并与同份 `<Counters failed>` 双向对账；`<ResultSummary>` 与 `<Output>` 不算用例；抽红格名／消息／行号**不许用"开标签—闭合标签配对"的正则**（那批结果结点绝大多数是自闭合 `…/>`，配对式会把行号与消息打印成空值而红数照样对得上）。**建议再加第三面**＝每个红格名回判据源码按方法名真实存在再验一遍（守门人这一把用的 `gk34-audit.js` 就是这么写的）。**起跑第一轮之前先拿盘上已知读数自证仪器**，六格全对才允许用它裁这一把：`.local-notes/wave33-1028/out/trx-M1`＝恰 1、`trx-M5`＝恰 1、`trx-M7`＝恰 1、`trx-F`＝4 份且 `failed` 全 0（total 应为 108／785／280／233）；再加本刀自己上一把的两份＝`.local-notes/wave34-1028/out/trx-A1`＝**恰 2**、`trx-A6`＝恰 1（红名必须逐字符含参数表 `(suppressed: True, deduplicated: False, rejected: False)`）。任一不符＝仪器坏了，先修仪器，不改判据。
5. **报告先建、逐轮落盘**（150 轮硬上限，一夜三把栽在这格上）：动第一次 `dotnet` 前把 §〇 写满建文件（**报告路径＝`docs/eval/reports/2026-10-28-hds009-wave35-red-proof-handover.md`，主树里唯一允许新建的文件，不提交、不 `git add`**），之后每完成一轮立刻把该轮十一项追加进报告再跑下一轮。十一项＝①文件:行 ②改前原文 ③改后原文 ④`git diff --stat` ⑤跑法整行 ⑥TRX 文件名 ⑦红格全名＋消息要点＋红点行号 ⑧还原命令 ⑨还原后 `git status --short` 行数 ⑩突变态与还原后 SHA-256 ⑪当轮预检整段。缺 ⑨⑩⑪ 退回不补签。**⑩ 的"还原后应为"值按载体基点侧取**＝`git show 51f149d:<path>` 的 blob；**判据文件同法且每轮都要报**（本刀十轮全应等于 `317d2171…`，这是 §三 那两枚 C 轮能签换手的唯一凭据）。
6. **定向跑法固定**（与上一把一致，逐字照抄只换结果目录名）：`dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave35-1028/out/trx-<轮>" --logger trx`。**过滤器只圈这一枚判据类**，别的类不执行＝本刀 C 类的形状判定范围（见 §三 末段那条明写）。

## 二 允许的文件面（超出即作废）

- ① `.local-notes/wave35-1028/`（自建，gitignored）里的仪器、突变 manifest 与全部 TRX；
- ② 报告 `docs/eval/reports/2026-10-28-hds009-wave35-red-proof-handover.md`（唯一一份可写文档）；
- ③ **载体内**的 `src/Pry.Core/Narrative/StateAndLearning/*.cs`——**只作临时突变、逐轮按字节还原**，终态相对 `51f149d` 净写入 0 字节；
- ④ **禁止**：新建任何产品文件、新建任何测试文件、**写 `tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs` 一个字节**、改 `scripts/**`／`Pry.slnx`／`Directory.Build.props`／`docs/**`（除②那一枚）／主树任何文件；不许往既有测试文件加行（尤其他人名下那枚 `tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）。

## 三 本刀要结清的账（八枚 A ＋ 两枚 C）

### 三.0 事前论证（**先落盘再起跑**；下表是守门人从 `51f149d` 干净产品码按"被改那行有几个消费点"独立复算的，不是结论）

上一把 A 类八轮结清 10 枚、B 类两枚只结红证，**7 枚仍没拿到过红**；守门人 §九.3 另立一条断言级子账。八条方向逐枚如下。**任何一轮实测形状与"预期枚数／格名"不符 ⇒ 整轮作废**（红不署、不追第二轮、不许把 §三.0 改成实测值）。

| 轮 | 文件:行（`51f149d` 侧） | 改法（一句话） | 被改那行的消费点（为何恰这一枚） | 预期恰响格（逐字符） | 枚数 |
|---|---|---|---|---|---|
| A1 | `StateLayering.cs:82` | 把字面量 `"canon_revised_by_authority"` 改成另一串（文本面，身份照实登记不冒充行为突变） | 判据里该字面量只在 `:64` 出现一次（`Assert.Equal(…, readout.Reason)`），所在方法＝本行预期格；其余两枚 `ReviseCanon` 格断的是 `"Rejected"` 与另一枚拒因，不读这串 | `An_author_side_canon_revision_with_a_pointable_source_is_applied` | 恰 1 |
| A2 | `StateLayering.cs:99` | 入册那行的 `draft.Source.SourceId` 换成 `draft.OverlayKey`（来源身份记错） | 全判据只有 `:114-116` 那组断 `Assert.Single(layers.Overlays)` 加 `overlay.SourceId == "src-1"`／`SourceVersion == 4`；缺来源那格走 `:92` 的拒绝支不读这行；`Assert.Empty(Overlays)` 类那格不入册 | `An_overlay_with_source_and_version_is_recorded_and_leaves_the_canon_layer_untouched` | 恰 1 |
| A3 | `DeliveryAttribution.cs:60` | 只摘合取第二支 `evidence.BodyDeduplicated \|\|`（留第一、三支） | theory 三行各只置一枚排除位：摘 `Deduplicated` 支只让第二行漏过闸；上一把已实测同一位置摘第一支只响第一行＝同族形状已被证过一次 | `A_delivered_body_that_was_suppressed_deduplicated_or_rejected_is_not_attribution_evidence(suppressed: False, deduplicated: True, rejected: False)` | 恰 1 |
| A4 | `DeliveryAttribution.cs:60` | 只摘合取第三支 `\|\| evidence.BodyRejected`（留第一、二支） | 同上，只有第三行漏过闸；其余两行仍被留下的两支拦下 | `A_delivered_body_that_was_suppressed_deduplicated_or_rejected_is_not_attribution_evidence(suppressed: False, deduplicated: False, rejected: True)` | 恰 1 |
| A5 | `ExperienceMaterial.cs:52` | 把 `match.Material` 换成 `null`（存了却不把材料交出去） | 全判据只有 `:273` 那行断 `Assert.Equal("a warm exchange", readout.OptionalMaterial)`；`:260`／`:287` 两行本来就断 `Assert.Null`，走 `NotOffered` 支不读 `:52` | `Experience_within_the_input_validity_window_is_offered_as_optional_material` | 恰 1 |
| A6 | `ExperienceMaterial.cs:50` | 把字面量 `"experience_past_validity"` 改成另一串（文本面） | 判据里该字面量只在 `:286` 出现一次；同格那行 `Assert.Null(readout.OptionalMaterial)` 不受影响＝整格仍红、红点在 `:286` | `Experience_past_the_input_validity_window_is_not_offered` | 恰 1 |
| A7 | `LegacyInheritance.cs:57` | 把 `input.Source.SourceId` 换成 `input.StateKey`（继承带上错的来源） | 判据里 `:398-400` 那组断 `row.SourceId == "src-inherit"`；三条输入那格只断 Outcome 串（`:378-380`）、缺来源那格走 `:53` 的拒绝支 | `A_successful_inheritance_carries_its_source_reference_and_version` | 恰 1 |
| A8 | `LegacyInheritance.cs:53` | 把拒因字面量 `"inherited_source_missing"` 改成另一串（文本面） | 判据里该串出现两次（`:365` 与 `:367`），**两处都在同一枚方法内**（`:356` 声明），所以红格数＝1 而不是 2；`Every_inherited_input…` 那格只断 Outcome 串，不读这串 | `Inherited_state_without_a_pointable_source_fails_loudly_and_is_never_silently_dropped` | 恰 1 |
| C1 | `StateLayering.cs` **文末追加一行注释**：`// MUTATION C1 (temporary, wave35): ReplyGenerationRenderer` | 扫描函数 `LegacyChainNamesFoundIn`（判据 `:418-419`）按 `Contains` **故意连注释一起匹配**；`PrototypeSource` 读的就是这五枚产品文件，所以 `:310` 那句 `Assert.Empty` 当场红，`:320-323` 的探针句保持通过（它读的是临时样本文件，不是产品文件） | `State_and_learning_prototype_sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites` | 恰 1 |
| C2 | `StateLayering.cs` **文末追加一行注释**：`// MUTATION C2 (temporary, wave35): const 7` | `SourceSideDefaultsFoundIn`（判据 `:421-425`）匹配的字面量是 `"const "`（含尾空格）；`:336` 那句 `Assert.Empty` 当场红，`:344` 的探针句仍绿。注意上一把 B2 证明过"把 `Contains` 样本串换成不存在的 token"只会红探针句——本刀走的是**反方向**（把样本塞进被扫面），所以红点落在前半句、且**判据字节零写入** | `State_and_learning_prototype_declares_no_source_side_numeric_default` | 恰 1 |

**A6 的替代方向不要顺手用**：把 `ExperienceMaterial.cs:49` 的比较符 `>` 翻向 `<` 是**三响**方向（窗口内／超期／裁决权三格同时红）。本刀主用 A6 那条文本面配方（恰 1）；若你确实要跑比较符那一向，必须在 §三.0 里**事前点名那三枚格名并写"恰 3"**，事后不许把三响署给单枚格（波 30 立的口径）。

**C 类两轮的署名规则（这是本刀与上一把最大的不同，逐字读）**：波 34 的 B1/B2 之所以只结红证不签换手，是因为**那两轮改的是判据文件自身**（取红那只手当轮写过判据字节 ⇒ 自证）。本刀换一条路＝**判据字节零写入、往被扫的产品文件注入样本**（§〇.4 全程 `317d2171…` 就是凭据）。⇒ C1/C2 各为那两枚扫描格第一次拿到"非判据作者之手逼出的红"，**红证与换手两样同时结清**。代价要如实登记：注入串会同时进入别的按 `src/**/*.cs` 全仓扫描的门（例如 Api 那把资源限额台账覆盖门），但本刀十轮的过滤器只圈 `StateAndLearningPrototypeTests`（§一.6），那些类根本不执行，所以**形状判定限于本类之内**；F 轮跑前已逐字节还原，全仓门读到的仍是干净树。

**跑序固定＝A1→A8 先跑完（这八轮判据字节必须全程等于 blob 值），再跑 C1→C2，C 类还原并核 SHA 之后才允许收尾全量。**

## 四 本刀**不**跑的账（押给下一把，不许顺手碰）

1. **波 33 的 M1–M7 换人重取（7 枚格的第二换手）**：那七枚的红当年由**写判据的那一双手**取到，且 `51f149d` 那次判据修订让 M7 连那次都不算；配方逐字见波 33 报告 §三.0（M1 `StateLayering.cs:74`／M2 `:92`／M3 `NarrativeAuthority.cs:37`／M4 `DeliveryAttribution.cs:64`／M5 `:88`（`AttributionReadout` 的第 5 枚实参 `UserConsentProven`，由 `false` 翻向 `true`；`:89` 是 `TaskCompletedProven`，不许一起翻）／M6 `LegacyInheritance.cs:77`／M7 `ExperienceMaterial.cs:42`）。本刀排不进额度（定向已排满 10），**押第三十六波**，另出一单。
2. 断言级子账另两条（`BoundedAdvancementPrototypeTests.cs:195`／`:172`，波 32 立的）属 007 那棵载体，与本刀无交集。
3. 波 28 那枚数值锁格（反证只能靠新增数字门槛，零门槛是作者定的，**不许为凑红造门槛**）、两套并行命名债、`PrototypeFiles` 写死五枚清单的扫描盲区、HDS-010 删除片、HDS-007 的生产接线——照旧挂着。

## 五 交回给守门人核的六件事（不在本 lane 权限内）

1. 每轮红格归属逐格点名到 TRX 的 `outcome="Failed"` 开标签节点；**批次红不许署给单枚突变**；本刀十轮**全部预期恰 1**，出现 2 枚以上即整轮作废并如实登记多出来的红名（那可能是一条新发现，不是你的成果）。
2. 载体终态：`status --short`＝**0 行**、`fd93ebe..HEAD` 仍只有波 33 那两枚提交、六枚 SHA 逐字符等于 §〇.4 的入场基准。
3. 收尾全量四份 TRX 先数满再求和，应当**仍＝1406／executed 1404／failed 0／跳过 2**（两条同名跳过按 `outcome="NotExecuted"` 点名＝`Configured_llama_server_requires_the_child_environment_key`／`Configured_sense_voice_model_can_run_offline`）。**本刀不顶账**，1406 不因这一把移动，也不许为顶账另起构建。
4. 作废轮登记是否齐（§一.3 改名留档＋`.trx` 枚数与轮数对账：定向 10×1＋全量 1×4＝14 枚）。
5. **十轮每一轮的判据文件 SHA-256 是否都等于 `51f149d` 的 blob `317d2171…`**——这是本刀十轮全部有签账资格的唯一凭据；少一次 SHA 就不签那一格，C 类两枚尤其如此。
6. 有没有人偷偷新增判据、改产品终态字节、动数字门槛——**净新增格数必须 0**，报告 §二 自证 `[Fact]`／`[Theory]`／`[InlineData]` 三计数与上一把逐位相同（**23／1／3**）。

## 六 未验证（照旧逐条点名，不用"已完成"混过去）

真实模型对照（零读数，需作者一句"可以跑"）；**生产接线**（这 26 枚至今零生产调用方，要接的是 005 的落库面与 004 的生命周期，一律没接；接线时机＝U-9 残句「切换那一步她要不要做什么」仍未拍）；旧数据真实继承（零读取）；UI 可见行为（零读数）；生产库与 `%LOCALAPPDATA%\PryCompanion`（零接触）；待标定参数权威值（关系/心情各档阈值、经验有效期、点击归因窗口全部未定＝**特性不是缺陷**）；并发与断电（零读数）；旧链删除片与"构建时不把旧链打进包"那道门（**未开始**）；**换手终账不由本 lane 签**＝本刀若十轮全成，结清的应是"7 枚无红格的红证 ＋ 2 枚扫描格的红证与换手"，波 33 那 7 枚 M 系仍欠另一双手的红（§四.1）。旧欠 **75 格**（波 13 余 47＋波 20 余 28）与本刀无交集、照旧不减；拆法原文照旧 46／45／1＝W2。**四句话照旧不许出现在任何页面**＝新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链。

## 七 不许走的捷径（逐条对照前几把踩过的坑）

1. 不许新增判据格、不许改断言、不许改 `[InlineData]` 期望值、不许 `Assert.Skip`、不许注释既有断言、不许改上游门——**这一刀的产物是红，不是格数**。
2. **本刀十轮一律不许写判据字节**（波 34 那两枚反面探针是特殊类且代价已写进 §三 末段；本刀要的是"另一双手的红"，再碰判据＝自证，白跑）。
3. 不许把"红不了"当结论而不试；也不许把"没试"写成"够不着"。
4. 不许合轮（一次一枚突变、一枚一轮）。
5. 不许带 `--no-build` 跑 A/C 轮或收尾全量。
6. 不许删作废轮产物；不许清空结果目录重跑。
7. 不许为顶账起构建（本刀根本不顶账）。
8. 不许在报告里替守门人写状态词、结案词，不许自封在册基线。
9. 不许碰他人未提交改动、暂存项、未跟踪资产；共享主树禁裸 `git commit`。
10. 不许顺手跑 §四 那三本押账（尤其 M1–M7：那是第三十六波的活，跑进来＝把别人的事前论证偷成自己的红）。

## 八 交回与划账（本 lane 写 §〇–§七，守门人追加 §九）

（守门人在此预登记划账面：六件事判定、十轮的签账资格凭据（判据字节全程值）、C 类两枚能否把"红证＋换手"两样一起结清、7 枚无红格的终账、波 33 那 7 枚 M 系的押账、作废轮与 `.trx` 对账、1406 是否同值复采、第三十六波方向。）
