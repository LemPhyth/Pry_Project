# 工单 HDS-WAVE34-1028｜HDS-009「状态与显式学习迁移」第一片第二刀：给第三十三波那 19 枚无红格逐枚配方向类突变（续跑证据刀，零产品字节、零归位、零顶账）

- 工单 id：`HDS-WAVE34-1028`
- 性质：**证据刀·续跑**（复用第三十三波那棵载体的产品码；**不许新增产品文件、不许改产品字节、不许动判据字节**（A 类轮次）、零新增数字门槛；**零落地、零归位、零顶账**）
- 任务编号：`HDS-009`（登记处＝`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS-009 行；本刀的判据依据＝该行的"第一片已归位"那句＋`docs/eval/reports/2026-10-28-hds009-state-and-learning-prototype.md` §五 的"无红格登记"那条与守门人 §七.1／§七.4／§七.5）
- 出闸：守门人 2026-10-28 19:4x。作者原令逐字＝「我不想思考了，你就继续给我派工单，该提交的提交吧」＋「你，还是自己起Agent吧」＋「继续工作，不要让我拍板啥的，告诉我现在是什么情况就行」＋「下一次不要再三构建触发那个防火墙烦我打游戏」＋「游戏基本都是全屏应用，看当前桌面焦点是不是全屏应用就行」
- 执行方：冷启动 lane（第三十四号载体＝**沿用第三十三波那棵**，不新建 worktree）
- **起跑前置**：一次 `VERDICT=BUILD_OK` 的焦点预检窗口。读到 `HOLD` 就按 §〇.6 办，不许起跑、不许请示、不许改预检判据。
- **这一刀对作者可见的东西＝零**。它不新增任何一格判据、不动任何一行产品码，所以界面上、对话里什么都不会变；它填的是"改坏了没人知道"那一格里第三十三波还没填完的 19 枚。

## 〇 开工前必读与入场（缺一条就就地停刀登记，不要自扩范围）

1. **先读七份**：`docs/plans/README.md`、`docs/plans/PROJECT_PLAN.md`（§4.2 的 HDS-009 行＋§9 最新两条 entry，最新一条是 10-28 19:4x 的第三十三波收口）、`docs/agent-engineering-standards.md`（全文，交付形态按 §10 四项）、`docs/dispatch-hds-wave33-1028.md`（上一把的工单，§一 那五条范围就是本刀要逐枚配红的那五条的出处）、`docs/eval/reports/2026-10-28-hds009-state-and-learning-prototype.md`（**§三.0 那张事前论证表＋§五＋守门人 §七 全节必读**：本刀的做法就是从它"没试不等于够不着"那句话来的）、`src/Pry.Core/Narrative/StateAndLearning/` 那五枚产品文件（本刀要改坏的就是它们，**逐行读**）、`tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs`（判据本体，26 枚格的格名与断言面都在里面）。
2. **入场读数当场贴进报告 §〇**（不许引用本工单的数字）：`date`、`git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD`、`git status --short --branch` 原样、`git worktree list`、`git log --oneline -3`。
3. **载体＝沿用第三十三波那棵**：`.local-notes/qa-wave33-1028/carrier`（基点 `fd93ebe`、线尾 `51f149d0b722e28a6566c93d19f48a94ca4c85df`）。**不许新建 worktree**，复用它的 `bin`／`obj` 正是省掉作者一次构建的地方。入场自证三件并贴进 §〇：① `git -C <carrier> rev-parse HEAD`＝上面那个全号；② `git -C <carrier> status --short`＝**0 行**；③ `git -C <carrier> diff --stat fd93ebe $(git rev-parse HEAD) -- src tests scripts Pry.slnx Directory.Build.props`＝只有第三十三波那六枚新文件、**+822/−0**。再加一件本刀专属：**主树侧**的 `git -C C:/Users/24782/Desktop/Pry_Project diff --stat 51f149d HEAD -- src tests scripts Pry.slnx Directory.Build.props`＝**0 行**（右端当场 `git rev-parse HEAD` 取，不许在本工单里预写 SHA——第三十一波 §十一.4 那条更正照旧生效）。这条 0 行式子成立 ⇒ 载体树与主线归位后的产品／判据字节逐字符相同，本刀在载体里取到的红就是主线那棵树的红。
4. **在册基线起点＝当场从上一把报告 §七.2 读到的那个数**（守门人 10-28 核过的是 **1406／executed 1404／failed 0／跳过 2**，分项 Client 108／App 233／Api 280／Core **785**、Core executed **783**）。本刀**新格数＝0**（§二 明令不许新增判据），所以出口读数应当**仍是 1406**：收尾那轮全量若读到别的数，先怀疑树没还原干净，不要改判据、不要改基线，照实登记并作废那一轮。
5. **他人资产**：主树那两枚 `M ` 暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）与全部 `??`＝别人的，不读内容、不改、不提交、不 `stash`／`clean`／`rm`。**共享主树里禁止任何 `git commit`／`add`／`checkout`／`reset`／`cherry-pick`／`merge`／`tag`／`push`／`git apply`**；写只许落在载体里（临时突变，逐轮还原）＋你自己那一份报告文件。**本刀归位是空转**：终态净写入 0 字节 ⇒ 没有代码刀、守门人不做按路径落地，`main` 也不因本刀挪。
6. **焦点预检（这条会打断作者本人，违反＝本波作废）**：每一次 `dotnet`（含每一枚突变轮、收尾全量）之前先跑

   ```
   powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1
   ```

   整段输出贴进报告并归档到 `.local-notes/wave34-1028/out/preflight-<ID>.txt`，**编号从 P49 起**（P1–P48 已被前七把用掉）。只有 `VERDICT=BUILD_OK` 才起跑。读到 `HOLD`：**不起跑、不请示、不改判据、不绕过**；先把零 `dotnet` 的活做完（写 §〇、逐枚读码、把八枚 A 类＋两枚 B 类的突变文本先在临时副本上合成并 `sha256sum` 记下），然后**可以重跑预检**——**预检本身不设上限，只对"HOLD 之后的重跑"设上限：最多累计 8 次、相邻两次不短于 5 分钟**（额度按"刀"计）。8 次后仍 `HOLD` ⇒ **带着"本刀零 `dotnet` 起跑"的交付回来，那是合法交付**，报告写清跑到哪一行、下一手从哪一行接着跑。`free_memory_gb` 偏低或读数呈 SQLite `disk I/O`／`out of memory` 形态时那一轮读数作废（那是他机器的资源形状，不是断言失败），不动任何进程。

## 一 预算与两条硬口径（本刀自己新立的，出闸前我在上一把报告 §七.3 里点过名）

1. **预算**：解决方案级构建 **≤1**（若 §〇.3 那条 0 行式子成立、且载体的 `bin/Release/net10.0/Pry.Core.dll` 在盘上，**允许跳过 B1 并登记判定**）；解决方案级全量 **≤1**；定向 **≤8（A 类）＋≤2（B 类）＝单把 ≤10**。`dotnet` 总调用上限＝1＋1＋10＝12。**A 类与 B 类一律不许带 `--no-build`**（自行增量重建是唯一能保证"跑的是当前源码"的写法）。
2. **`全量 ≤1` 的语义写死＝"交付的读数只许来自 1 次解决方案级全量"**，不是"这台机器今天只准起一次全量"。作废轮（§一.3 那种）的重跑**不占这一格**，但每一轮全量都要在报告里逐轮登记：第几次调用、结果目录名、TRX 枚数、`<Counters>` 求和。第三十三波为这件事超了 1 次，账记在守门人头上，不销也不重复罚。
3. **作废轮的产物一律改名留档，不许清空重跑**：目录命名 `trx-<轮>-voided-<预检号>`（例：`trx-F-voided-P47`）。**盘上产物必须与真实轮数一一对应**——第三十三波盘上只有 12 枚 `.trx` 而真实跑了 16 枚，那 4 枚被删掉的作废读数就是"少记一轮比记错数值更贵"的实例。收尾报告 §六 要有一行"盘上 `.trx` 枚数＝登记的轮数×每轮枚数"的自校。
4. **上一把那条 `--no-build` 教训照抄进本刀**（原文见第三十三波报告 §三.F 首段）：收尾那轮全量**一律不许带 `--no-build`**，除非它之前紧跟一次显式重建——源码还原 ≠ IL 还原，突变态程序集留在盘上时 `--no-build` 读的是没还原的树。
5. **取数仪器**：只数 `<UnitTestResult … outcome="Failed">` 的**开标签**并与同份 `<Counters failed>` 双向对账；`<ResultSummary>` 与 `<Output>` 不算用例；抽红格名／消息／行号**不许用"开标签—闭合标签配对"的正则**（Core 那 785 枚结果结点里绝大多数是自闭合 `…/>`，配对式会把行号与消息打印成空值而红数照样对得上）。**起跑第一轮之前先拿盘上已知读数自证仪器**，四格全对才允许用它裁这一把：`.local-notes/wave29-1024/out/trx-M1`＝恰 1、`trx-M5`＝恰 3、`trx-L6`＝恰 2、`../wave33-1028/out/trx-F`＝4 份且 `failed` 全 0（total 应为 108／785／280／233）。任一不符＝仪器坏了，先修仪器，不改判据。
6. **报告先建、逐轮落盘**（150 轮硬上限，一夜三把栽在这格上）：动第一次 `dotnet` 前把 §〇 写满建文件（**报告路径＝`docs/eval/reports/2026-10-28-hds009-wave34-red-proof-continuation.md`，主树里唯一允许新建的文件，不提交、不 `git add`**），之后每完成一轮立刻把该轮十一项追加进报告再跑下一轮。十一项＝①文件:行 ②改前原文 ③改后原文 ④`git diff --stat` ⑤跑法整行 ⑥TRX 文件名 ⑦红格全名＋消息要点＋红点行号 ⑧还原命令 ⑨还原后 `git status --short` 行数 ⑩突变态与还原后 SHA-256 ⑪当轮预检整段。缺 ⑨⑩⑪ 退回不补签。**⑩ 的"还原后应为"值按载体基点侧取**：产品文件＝`git show 51f149d:<path>` 的 blob；判据文件同法。

## 二 允许的文件面（超出即作废）

- ① `.local-notes/wave34-1028/`（自建，gitignored）里的仪器、突变 manifest 与全部 TRX；
- ② 报告 `docs/eval/reports/2026-10-28-hds009-wave34-red-proof-continuation.md`（唯一一份可写文档）；
- ③ **载体内**的 `src/Pry.Core/Narrative/StateAndLearning/*.cs` 与 `tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs`——**只作临时突变、逐轮按字节还原**，终态相对 `51f149d` 净写入 0 字节；
- ④ **禁止**：新建任何产品文件、新建任何测试文件、改 `scripts/**`／`Pry.slnx`／`Directory.Build.props`／`docs/**`（除②那一枚）／主树任何文件；**不许往既有测试文件加行**（尤其别人名下那枚 `tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）。

## 三 本刀要结清的账（19 枚无红格，格名逐字符取自判据文件）

上一把 26 枚格里 7 枚在 M1..M7 各拿到一次清洁红，**其余 19 枚只有"通过形状"**。lane 自己在 §五 写了"没写够不着、也没穷举方向类突变"——这一刀就是那笔账的续跑。**八枚 A 类方向我在出闸前逐枚读过产品码并数过被改那行的消费点**，见下表；你必须先把它抄进报告 §三.0 作事前论证（**先落盘再起跑**），再逐枚跑。**下表不是结论，是你的假设**：任何一轮实测形状与"预期枚数／格名"不符 ⇒ **整轮作废**（红不署、不追第二轮、不许把 §三.0 改成实测值）。

| 轮 | 类别 | 文件:行（`51f149d` 侧行号） | 改法（一句话） | 被改那行的消费点（为何恰这几枚） | 预期恰响格（逐字符） | 枚数 |
|---|---|---|---|---|---|---|
| A1 | 行为 | `NarrativeAuthority.cs:33-34` | 摘掉闸①（亲密／关系类）整段 `if` | 单独 `WritesIntimacyState=true` 的输入在两格里：`c-1` 那格与三面全给那格；摘后前者落到放行、后者被闸②接住换了拒因 | `A_narrative_candidate_may_not_write_intimacy_state`＋`A_candidate_touching_all_three_surfaces_is_caught_by_the_first_gate` | **恰 2** |
| A2 | 行为 | `NarrativeAuthority.cs:41-42` | 摘掉闸③（用户情绪）整段 `if` | 只有 `c-3`（单独 `DeclaresUserEmotion=true`）走到闸③；三面全给那格被闸①先拦、`c-1`/`c-2` 各被前两道拦 | `A_narrative_candidate_may_not_declare_the_users_emotion` | 恰 1 |
| A3 | 行为 | `StateLayering.cs:78-79` | 摘掉 `ReviseCanon` 的"作者侧也要可指认来源"那道闸 | 缺来源／空标识的 `ReviseCanon` 输入只在 `canon-2` 那格；带来源的两格（`canon-1`）结果不变仍 Applied | `An_author_side_canon_revision_without_a_source_is_refused_with_its_own_reason` | 恰 1 |
| A4 | 行为 | `DeliveryAttribution.cs:71` | 摘掉成功路径的 `_registry.Add(...)` 那一行（只留读数不入册） | 全判据只有 `body-3` 那格断 `Assert.Single(Registry)`；其余四格断 `Assert.Empty`，摘后仍为空 | `An_experience_candidate_is_recorded_only_when_delivery_and_a_click_are_both_present` | 恰 1 |
| A5 | 行为 | `DeliveryAttribution.cs:68-69` | 摘掉闸③（缺点击不得入册） | "投递但没点击"的输入只在 `body-4` 那格；`body-3` 有点击不受影响；`body-1` 被闸②拦、theory 三行被闸①拦 | `A_delivered_body_without_an_explicit_click_is_not_recorded` | 恰 1 |
| A6 | 行为 | `DeliveryAttribution.cs:60` | 只摘合取的第一支 `evidence.BodySuppressed \|\|`（留其余两支） | theory 三行各只置一枚排除位：摘 `Suppressed` 支只让第一行漏过闸，第二、三行仍被 `Deduplicated`／`Rejected` 拦下 | `A_delivered_body_that_was_suppressed_deduplicated_or_rejected_is_not_attribution_evidence(suppressed: True, deduplicated: False, rejected: False)` | 恰 1 |
| A7 | 行为 | `LegacyInheritance.cs:50-53` | 摘掉 `Import` 的"来源缺失即失败"整道闸 | 缺来源的输入出现在两格：`old-1`/`old-2` 那格（断 `Failed`）与三条输入那格（断 `"Imported,Failed,Imported"` 串）；带来源的 `old-3` 结果不变 | `Inherited_state_without_a_pointable_source_fails_loudly_and_is_never_silently_dropped`＋`Every_inherited_input_leaves_a_named_readout_rather_than_silently_vanishing` | **恰 2** |
| A8 | 行为 | `ExperienceMaterial.cs:56` | 把 `NotOffered` 工厂的第 5 枚实参（`CarriesSpeakingAuthority`）由 `false` 翻向 `true`（**不动第 2 枚 `Available`**） | 只有"经验不带说话裁决权"那格对**被拒读数**断 `CarriesSpeakingAuthority`；缺有效期那格与超期那格各只断 `Available`/`Reason`/`OptionalMaterial`；`offered` 走 `:52` 不受影响 | `Experience_never_carries_authority_over_whether_to_speak` | 恰 1 |
| B1 | 反面探针 | `StateAndLearningPrototypeTests.cs:419`（`LegacyChainNamesFoundIn`） | 把扫描函数弄成恒空（`Where(name => false)` 一类），**不改探针那句 `Assert.Equal`** | 被扫的五枚产品文件仍 `Assert.Empty` 通过；探针要求七枚名字按序全出 ⇒ 那格当场红 | `State_and_learning_prototype_sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites` | 恰 1 |
| B2 | 反面探针 | `StateAndLearningPrototypeTests.cs:423-424`（`SourceSideDefaultsFoundIn` 两条 `if` 的条件） | 把两条 `Contains` 的样本串换成必然不存在的 token，**不改 `Assert.Equal("const,static readonly", …)`** | 五枚产品文件仍 `Assert.Empty` 通过；探针要求两枚 token ⇒ 那格当场红 | `State_and_learning_prototype_declares_no_source_side_numeric_default` | 恰 1 |

**A6 那条要注意 TRX 里的节点名形状**：`[Theory]` 行的节点名带参数表，逐字符取形如 `…_evidence(suppressed: True, deduplicated: False, rejected: False)`；**同一方法的另两行保持绿**才是这一轮的合格形状，若三行一起红＝你摘的不是第一支（整轮作废）。

**B 类两轮的署名规则（不许含糊）**：它们动的是**判据自身**的字节，所以按波 31 立的口径**单列登记**——B1/B2 各结清那两枚扫描格的**红证**（证明"门在咬"、不是常绿门），但**不签第二换手**；A 类八轮全程判据字节为 `51f149d` 的 blob 值（每轮 ⑩ 项都要给出判据文件的 SHA-256 并等于该 blob），所以 A 类结清的格**同时拿到红证与第二换手**——这一把的写手不是第三十三波那一位，且对判据零写入。**跑序固定＝先全部 A 类（A1→A8），后 B 类（B1→B2），B 类还原并核 SHA 之后才允许收尾全量。**

## 四 结不到红的格怎么登记

19 减去 A 类结清的 **10 枚**（A1／A7 各是一枚突变结清两格）＝9 枚；再减去 B 类那两枚扫描格＝**7 枚本刀仍拿不到红**。这 7 枚我在出闸前逐枚读过码，**方向已在下表逐枚点名（含预期枚数）**，交给下一把执行；本刀不许顺手跑它们（额度已排满），也不许把它们写成"够不着"。**没试不等于够不着**——谁要登记"结构上够不着"，必须先对该格逐枚穷举过方向类突变（`>`↔`>=`、合取支单摘、赋值行删除、返回字面量翻转、参数换位），并且**先自己重推一遍**：波 28 lane 写过的两条"够不着"被守门人当场证伪，波 33 lane 干脆没写这句话。（下表第 8 行不是红方向，是那两枚扫描格的**换手**账，别混进红证数。）

| 无红格（逐字符） | 下一把的方向（我读码给出，非结论） | 预期枚数 |
|---|---|---|
| `An_author_side_canon_revision_with_a_pointable_source_is_applied` | `StateLayering.cs:82` 的 `"canon_revised_by_authority"` 字面量改写（文本面，身份照实登记不冒充行为突变） | 恰 1 |
| `An_overlay_with_source_and_version_is_recorded_and_leaves_the_canon_layer_untouched` | `StateLayering.cs:99` 的 `draft.Source.SourceId` 换成 `draft.OverlayKey`（入册带错来源） | 恰 1 |
| `A_delivered_body_that_was_suppressed_deduplicated_or_rejected_is_not_attribution_evidence(suppressed: False, deduplicated: True, rejected: False)` | 摘 `BodyDeduplicated` 那一支 | 恰 1 |
| `A_delivered_body_that_was_suppressed_deduplicated_or_rejected_is_not_attribution_evidence(suppressed: False, deduplicated: False, rejected: True)` | 摘 `BodyRejected` 那一支 | 恰 1 |
| `Experience_within_the_input_validity_window_is_offered_as_optional_material` | `ExperienceMaterial.cs:52` 的 `match.Material` 换成 `null`（存了却不把材料交出去） | 恰 1 |
| `Experience_past_the_input_validity_window_is_not_offered` | `ExperienceMaterial.cs:50` 的 `"experience_past_validity"` 字面量改写（文本面）；注意比较方向翻转是**三响**方向（`:49` 的 `>`→`<` 会同时打红窗口内／超期／权限三格），事前论证必须写"恰 3" | 恰 1 |
| `A_successful_inheritance_carries_its_source_reference_and_version` | `LegacyInheritance.cs:57` 的 `input.Source.SourceId` 换成 `input.StateKey` | 恰 1 |
| `State_and_learning_prototype_sources_carry_no_legacy_chain_names_…` 与 `…_declares_no_source_side_numeric_default` 的**第二换手** | 不是红方向问题：这两枚字节级门要另一双手跑一次定向绿轮（G0 形状）才能签；本刀因跑序固定（A→B）不签它们 | 属换手不属红证 |

其余押账（本刀不碰，也不许顺手碰）：波 28 那枚数值锁格（反证只能靠新增数字门槛，零门槛是作者定的）、两套并行命名债（`Hds006*` 前缀产品类型、契约 §7.2 词表尚无产品对应）、`PrototypeFiles` 写死五枚清单的盲区（将来新增第六枚产品文件不会自动进扫描面）、HDS-010 删除片、HDS-007 的生产接线与命名债。

## 五 交回给守门人核的六件事（不在本 lane 权限内）

1. 每一轮的红格归属逐格点名到 TRX 的 `outcome="Failed"` 开标签节点；**批次红不许署给单枚突变**——A1／A7 是**事前论证过的恰 2**，其余必须恰 1，形状不符即整轮作废。
2. 载体终态：`git -C <carrier> status --short`＝**0 行**，`git diff --stat fd93ebe HEAD -- src tests scripts Pry.slnx Directory.Build.props`＝**只有那六枚 +822/−0**（本刀零新提交＝载体线尾仍 `51f149d`），五枚产品文件与判据文件的 SHA-256 逐字符等于 §〇.3 的入场基准（`af727a9c`／`000f2fc8`／`5858a75d`／`289fc89b`／`6a1f5099`／判据 `317d2171`）。
3. 收尾全量四份 TRX 先数满再求和，应当**仍＝1406／executed 1404／failed 0／跳过 2**（两条同名跳过按 `outcome="NotExecuted"` 点名＝`Configured_llama_server_requires_the_child_environment_key`／`Configured_sense_voice_model_can_run_offline`）。**本刀不顶账**：1406 不因这一把移动，也不许为顶账另起构建。
4. 作废轮登记是否齐（§一.3：目录改名留档、`.trx` 枚数与轮数对账）。
5. A 类八轮每一轮的判据字节 SHA 是否都等于 `51f149d` 的 blob（这是 A 类能签第二换手的唯一凭据；少一次 SHA 就不签那一格）。
6. 有没有人偷偷新增判据、改产品字节、动数字门槛——**本刀净新增格数必须是 0**，报告 §二 要自证 `[Fact]`／`[Theory]`／`[InlineData]` 三计数与上一把逐位相同（23／1／3）。

## 六 未验证（照旧逐条点名，不用"已完成"混过去）

真实模型对照（零读数，需作者一句"可以跑"）；**生产接线**（这 26 枚至今零生产调用方，要接的是 005 的落库面与 004 的生命周期，一律没接；接线时机＝U-9 残句「切换那一步她要不要做什么」仍未拍）；旧数据真实继承（零读取）；UI 可见行为（零读数）；生产库与 `%LOCALAPPDATA%\PryCompanion`（零接触）；待标定参数权威值（关系/心情各档阈值、经验有效期、点击归因窗口全部未定＝**特性不是缺陷**）；并发与断电（零读数）；旧链删除片与"构建时不把旧链打进包"那道门（**未开始**）；**第二换手终账**＝26 枚里本刀 A 类结清的 **10 枚**签到手（跑它们的手不是判据作者、且判据字节全程为 `51f149d` 的 blob），B 类 2 枚不签，其余 **14 枚仍欠**——上一把 M 系那 7 枚的红是写判据的那一双手跑的，仍算欠换手。**本刀 A 类那 10 枚与上一把 M 系那 7 枚无交集**（出闸前我逐格名对过），所以下一手数账时不必去重，也别把两把的 10＋7 读成 17 枚换手。旧欠 **75 格**（波 13 余 47＋波 20 余 28）与本刀无交集、照旧不减；拆法原文照旧 46／45／1＝W2。**三句话照旧不许出现在任何页面**＝新链已接线／旧链可删／不进包已做到；另加上一把那句照旧不许写＝**"她的状态已经迁到新链"**。

## 七 不许走的捷径（逐条对照前几把踩过的坑）

1. 不许新增判据格、不许改断言、不许改 `[InlineData]` 期望值、不许 `Assert.Skip`、不许注释既有断言、不许改上游门——**这一刀的产物是红，不是格数**。
2. 不许为了让某格响去改它对应的**判据**（B 类那两枚是**反面探针**这一特殊类，别的格一律不许用这个办法）。
3. 不许把"红不了"当结论而不试（§四 明令穷举）。
4. 不许用"两条路径同构造所以恒等"当覆盖证据。
5. 不许合轮（一次一枚突变、一枚一轮）。
6. 不许带 `--no-build` 跑 A/B 轮或收尾全量（§一.1／§一.4）。
7. 不许删作废轮产物（§一.3）。
8. 不许为顶账起构建（本刀根本不顶账）。
9. 不许在报告里替守门人写状态词、结案词，也不许自封在册基线。
10. 不许碰他人未提交改动、暂存项、未跟踪资产；共享主树禁裸 `git commit`。

## 八 交回与划账（本 lane 写 §〇–§七，守门人追加 §九）

（守门人在此预登记划账面：六件事判定、A 类换手资格与 B 类不签的署名终账、结清／仍欠的红证终账、作废轮与 `.trx` 对账、1406 是否同值复采、下一把方向。）
