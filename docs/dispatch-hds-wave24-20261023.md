# 工单 HDS-WAVE24-1023｜把第十四波那 16 格的红换到另一双手上，顺带给第二十波补齐 6 格配方、给两枚成对门一次双格凭证

- 工单 id：`HDS-WAVE24-1023`
- 性质：**纯复采刀（证据刀）。零产品代码、零判据字节改动**——不新增一格、不删一格、不改一行 `src/` 或 `tests/`
- 任务编号：`HDS-003`／`HDS-004`／`HDS-005` 的**证据本体**（在册第二换手覆盖度）；登记处＝`docs/plans/PROJECT_PLAN.md` §4.2 那三行＋§9
- 出闸：守门人 2026-10-23 19:4x。作者原令逐字＝「我不想思考了，你就继续给我派工单，该提交的提交吧」＋「你，还是自己起Agent吧」
- 执行方：冷启动 lane（第二十四号载体）

## 〇 开工前必读与入场（缺一条就就地停刀登记，不要自扩范围）

1. **先读七份**：`docs/plans/README.md`、`docs/plans/PROJECT_PLAN.md`（§4.2 的 HDS-003／004／005 三行＋§9 最新三条 entry）、`docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md`、`docs/agent-engineering-standards.md`（全文，交付形态按 §10 四项）、`docs/legacy-chain-deprecation.md`、**`docs/eval/reports/2026-10-23-hds003-005-cell-evidence-audit.md`**（第二十二波那份，§三 那张 65 行表＋§四 梯队清单＋守门人 §五／§六／§七 就是本刀的任务书）、**`docs/eval/reports/2026-10-23-hds010-end-to-end-pipeline-prototype.md` §六**（那三轮"盘上有红、配方无档"的形状，本刀不许重犯）。
2. **入场读数当场贴进报告 §〇**（不许引用本工单的数字）：`date`、`git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD`、`git status --short --branch`。
3. **载体**：`git worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave24-1023/carrier <当场线尾全号>`。当场取线尾（`git rev-parse codex/ui-main-window-redesign`），先 `git rev-list --count HEAD..<线尾>` 核落后格数、非 0 就 `merge --ff-only` 追平再开工。**在册门禁基线现值＝1333／executed 1331／0／2**（分项 Client 108／App 233／Api 280／Core 712、executed 710），取于守门人 10-23 19:2x 的第二十三波归位轮——**本刀零新格，所以这一把不许动在册基线**；跑出来不等于 1333 就是有问题，先怀疑仪器再怀疑读数。
4. **他人资产**：主树那两枚 `M ` 暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）与全部 `??`＝别人的，不读内容、不改、不提交、不 `stash`／`clean`／`rm`。**共享主树里禁止任何 `git commit`／`cherry-pick`／`merge`／`tag`／`push`／FF `main`**；本刀根本不提交，交付物只有报告那一份文件＋`.local-notes` 里的仪器与 TRX。
5. **允许文件面（只有这些）**：① `.local-notes/wave24-1023/`（自建，gitignored）里的仪器副本与全部产物；② 报告 `docs/eval/reports/2026-10-23-hds003-005-second-hand-recount-prototype.md`。**`src/` 与 `tests/` 与 `docs/` 其余任何文件＝零字节**（要从第十四波那台 driver 起步，就 `cp` 一份到自己目录再改路径，不许原地改 `.local-notes/qa-wave14-1023/`——那是别人的证据）。
6. **禁止（逐条硬线，违反一条即作废本轮）**：
   1. **不改任何 `src/` 或 `tests/` 字节的持久化**：突变只发生在你自己的载体里、每枚跑完立刻 `git checkout --` 还原并核 SHA-256；**报告落盘面与载体线尾必须一字不差**。这条是本刀成立的前提——取红者不能是判据字节的作者，而你一旦改过判据就同时变成作者和取红者。
   2. 不许为拿红去修判据：不许放宽阈值、删断言、`Assert.Skip`、注释掉任何既有断言、改 `[InlineData]` 的期望值；拿不到的红一律写「未取到红」＋原因（锚点在当前树对不上／防御支／兜底支／结构上红不了），并写出承重断言实际落在哪一行。
   3. **突变轮一律不许 `--no-build`**（旧 dll ⇒ 突变根本没进二进制＝假绿，这正是第二十二波审出来的一条工单矛盾）。
   4. 不许引用 `.local-notes` 之外的历史 TRX 当本轮凭证；不许把批次红署成某一枚突变的归属（同轮多格红要按消息形状＋红点行号逐格归因）。
   5. 不新增数字门槛，也不改既有门槛（在册零改动清单＝检索 `.7`／`.5`、`2400`、五档超时、`MinimumUsableTokensPerSecond = 12`、`4096`、`DefaultSafetyTokens = 128`、`MinimumInputTokens = 512`）；不接 `src/Pry.Api`／`src/Pry.App`／`src/Pry.Client`／任何端点／任何界面；不碰 `%LOCALAPPDATA%\PryCompanion`；`licenses/**`、`licenses/THIRD-PARTY-NOTICES.md`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md` 一字不碰。
   6. 不写状态词与结案词，不自封基线；**不许写"65 格已换手""覆盖度已补齐"**——本刀最多减账 16＋6＋4＝26 格，减了多少逐格点名。
7. **焦点预检（这条会打断作者本人，违反＝本波作废）**：每一次 `dotnet`（含 build／test／任何工程，包括每一枚定向突变轮）之前先跑

   ```
   powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1
   ```

   整段输出贴进报告；只有 `VERDICT=BUILD_OK` 才起跑；读到 `HOLD` **就地停刀、不请示、不改预检判据、不绕过它**，并在报告里写清停在第几枚（续跑归下一手）。`free_memory_gb` 偏低或读数呈 SQLite `disk I/O`／`out of memory` 形态时那一轮读数作废（那是他机器的资源形状，不是断言失败），不动任何进程。
8. **预算（用满即停，不许自扩）**：解决方案级构建 **≤1**、解决方案级全量 **≤1**、**定向 ≤24**（活单① 16 ＋ 活单② 6 ＋ 活单③ 2；filter 那枚零成本验证算在活单③ 的 2 里，不通就退回逐类跑并登记）。每一枚定向轮自带一次工程级构建，那算定向跑次、不算构建额度。跑法固定写法＝`dotnet test <工程或解决方案> -c Release [--no-build 仅限全量] --results-directory "<绝对路径>" --logger trx`（短参 `-r` 会把结果拼进各工程 `bin` 导致"零测试仍 exit=0"；不给 `--logger trx` 就没有 TRX；**每轮独立结果目录、不许复用同名 `LogFileName`**）。跑完先数 `*.trx` 再读 `<Counters>`；**dotnet 次数只认盘上产物**。
9. **报告先建、逐批落盘**（150 轮硬上限）：动第一次 `dotnet` 前把 §〇 写满建文件，之后**每完成一批追加一次并把复采凭证逐轮写全**——第二十二波和第二十三波两把都是正文没跟上产物，本刀不许第三次。**十一项字段是每轮的最低落盘单位**（①文件:行 ②改前原文 ③改后原文 ④`git diff --stat` ⑤跑法整行 ⑥TRX 文件名 ⑦红格全名＋消息要点＋红点行号 ⑧还原命令 ⑨还原后 `git status --short` 行数 ⑩突变态与还原后 SHA-256 ⑪当轮预检整段）。

## 一 活单①：第十四波那 16 格换人复采（最便宜的一批，盘上现成仪器）

第十四波 `tests/Pry.Core.Tests/NarrativeCommitAndActionGuardTests.cs` 自 `d3fbac4` 归位后一字未动，`src/Pry.Core/Narrative/NarrativeDraftValidator.cs` 的 last-touch 是 `5145bde`（守门人 10-23 19:4x 实读 14 枚锚点 **14/14 HIT**：`R1 :306`／`R2 :308`／`R3 :309`／`R4 :266`／`A2 :200`／`A3 :214`／`A4 :223`／`A5 :229`／`A6 :249`／`C1 :387`／`C2 :389`／`C3 :395`／`C4 :403`／`C5 :410`），复采仪器也还在盘上。

- 复用＝`cp -r .local-notes/qa-wave14-1023/tools {你自己的目录}`，只改 `driver.sh` 顶部三行（`CARRIER`／`RUNS`／`TOOLS`），**不动原件**；spec 三件（`batch1.txt` 6 枚 R、`batch2.txt` 5 枚 A、`batch3.txt` 5 枚 C）逐枚照跑，`mutate.py` 的 NO-HIT 保护（`exit 7`）就是给锚点核对用的，触发即停并登记，不许改 spec 凑命中。
- 判据形状：filter 只跑那一枚类 ⇒ 每轮 `total=16`、**恰好该格红、其余 15 格绿**；红点行号按当前树实读（历史行位可能已因第十八／二十三波插入而下移，对不上就写"未取到红＋原因"）。
- 这一批要专门补的就是上一手缺的 **⑨⑩⑪**：还原后 `git status --short` 行数、突变态与还原后的 SHA-256 两侧、当轮预检整段——driver 本来就打印这些，缺的是把它们**逐轮抄进报告正文**，不是缺仪器。

## 二 活单②：第二十波 34 格里自选 6 格，把配方补成可署名的凭证

第二十三波用它的定向额度跑了 r1–r3，每轮"恰好一枚红、其余绿"，但 ①②③⑧⑨⑩⑪ **全部无档**（那三枚红格名守门人已从 TRX 直读并登记在其报告 §六：`InputBuffer_one_batch_yields_exactly_one_sendable_truth :97`／`Interruption_without_a_nameable_reason_is_rejected_not_guessed :147`／`Classifier_every_cue_maps_to_its_declared_class(ContinuationMarker→Continuation) :179`），"改坏了 `src/` 哪一行"不可考 ⇒ **那三格不签第二换手，34 格照旧全欠**。本活单就是把这 6 格做到能签。

- 自选 6 格，条件：① 批次／打断／分类／换形／节奏五个类型各至少一枚；② 优先选"这枚突变如果假了，用户可见后果最重"的那格；③ 逐格写清"为什么选它、守的是 `src/` 哪一行、放了哪枚单枚突变、红点落在哪一行、同轮其余格是否全绿"。
- 突变目标＝第二十波那五枚原型文件的判定行（`src/Pry.Core/Narrative/` 里的 `NarrativeInputBuffer`／`NarrativeInterruption`／`NarrativeInterruptClassifier`／`NarrativeBehaviorExpressionMapping`／`NarrativeReplyPacing`），**锚点先按当前树逐枚实读再下刀**；判据文件＝`tests/Pry.Core.Tests/NarrativeGapParityPrototypeTests.cs`（36 格，filter 用这一枚类，每轮 `total=36`）。
- `NarrativeReplyPacing.cs:91` 那枚防御支按第二十二波判定为"结构上红不了"，不要浪费轮次去撞它；要撞的话先把承重断言实际所在那一行写清楚再撞。

## 三 活单③：两枚成对门的一次双格凭证（先零成本验证 filter，不通就退回）

`NarrativeDraftValidator.cs:200` 同时把守波 14 #56 与波 13 #34，`:214`–`:218` 同时把守波 14 #57 与波 13 #36。历史上两枚类从未同跑（`run-A2`／`run-A3` 每份 TRX 只有 16 个节点，`run-m*` 每份只有 49 个节点）。

1. **第一步（1 轮，允许全绿）**：`dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~NarrativeCommitAndActionGuardTests|FullyQualifiedName~NarratorPrototypeTests" --results-directory "<绝对路径>" --logger trx` ⇒ 登记 TRX 节点数（预期 16＋49＝65，**这是预测不是结论，按名实数**）。filter 语法不认或节点数不符 ⇒ 退回逐类跑，如实登记"filter 不通"，不硬凑。
2. **第二步（2 轮）**：`A2`＝`:200` 把上界项削掉（`|| action.PlannedSegmentCount > request.MaxOutboundSegments` 那半没了）；`A3`＝delete `:214`–`:218`。每轮预期**两格红**（一类一格），逐格按消息形状＋红点行号归因。
3. **署名口径（这段不许省）**：波 14 那两格按 §〇 与活单① 同一套十一项字段即可署第二换手；**波 13 那两格要分两句写**——"这一轮确有另一双手的红"与"该格的历史凭证是否因 `66906d0` 改过判据字节而作废后重新成立"，两句不许并成一句，后者要另核该格判据字节在当前树与历史轮的一致性。

## 四 出口读数（缺哪格报哪格，不许留空）

1. 解决方案级构建 0 警告 0 错误原样；2. 全量一次，四份 TRX 的 `<Counters>` 逐份贴文件名，两条跳过按 `<UnitTestResult … outcome="NotExecuted">` 点名（`notExecuted` 计数器恒 0 不看）；3. 对账等式**必须由那四份原始 TRX 当场求和写出来**，本刀零新格 ⇒ 应仍＝**1333／executed 1331／failed 0／跳过 2**（分项 Client 108／App 233／Api 280／Core 712、executed 710）；不等就逐格找差、点名差在哪一份文件、**不许自封新基线**；4. `scripts/measure-fixed-eval.ps1 -ResultsDirectory <本轮目录>`（不带这个参数它会 throw 而退出码照旧 0）；5. **`git diff --stat <线尾>..HEAD -- src tests scripts Pry.slnx Directory.Build.props` 原文＝空**（这一格是本刀的身份证明：只跑突变、只还原、不落任何字节）；6. 载体收尾 `git status --short`＝0 行且 `git rev-parse HEAD` 仍是入场登记的号（若你曾提交任何东西，即为本刀作废条件之一）；7. `dotnet` 调用次数只认盘上产物（`ls **/*.trx` 数跑次；workload 日志不作证据）；8. 报告总行数与各节行数当场实测；9. `git diff --check` 干净。

## 五 归位通道（给守门人，不在本 lane 权限内）

本刀**没有代码落盘面**：交付物＝那一份报告（＋`.local-notes` 里的仪器与 TRX 证据）。所以归位＝一轮纯文档刀、**零 `dotnet`**，不重跑门禁、不动在册基线。守门人只核三件事：① §四.5 那格是否真为空；② 逐轮十一项字段是否齐（缺 ⑨⑩⑪ 的轮次按第二十二波口径退回，不补签）；③ 减账格数逐格点名写进 §9 与 `docs/full-scope-sprint.md` §3——**在册第二换手覆盖度的改写权在守门人那一手**。

## 六 仍未验证（报告里必须有一节，逐条点名）

真实模型对照（零读数）、生产接线与切换（新链组合件 `NarrativeTurnPipeline` 零生产调用方）、UI 可见行为（零读数）、生产库（零接触）、待标定参数权威值、与 HDS-007 到期再叙事的关系；另加本刀特有的一条：**本刀只证明"这些门会被单枚改坏弄响"，不证明"产品今天行为正确"**——判据覆盖度≠功能正确性，也不许被读成旧链可删／不进包已做到（那两条仍不成立）。

## 七 不许走的捷径（每条都有前例）

1. 不许改判据凑红（含改 `[InlineData]` 期望值、把断言换方向）；2. 不许 `--no-build` 跑突变；3. 不许用固定返回值／吞异常／`Assert.Skip`／注释掉既有断言让红消失或让绿出现；4. 不许把你不复采的格写成"不需要复采"；5. 不许引用他人目录（`.local-notes/qa-wave14-1023/` 原件、第二十波与第二十三波那几处证据目录）的产物当自己的复采凭证——可以引用为"待复采的原始证据"，凭证必须出自你这一手的跑次；6. 不许在本刀里顺手做归位／FF／tag／push；7. 不许动主树那两枚他人暂存项，也不 `stash`／`clean`；8. 不许写"65 格已换手／覆盖度已补齐"；9. 不许为了省轮次把两枚突变放进同一轮然后各署一格。

—— 完 ——
