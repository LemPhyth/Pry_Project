# HDS 第九波派单（2026-10-22 深夜）｜拆法原文欠账按盘上逐格重账＝22 格，其中 21 格要补证据、1 格（W2）已证够不着

派单人：守门人（Architecture / Integration / Release）。执行方式＝守门人自起冷启动子 Agent，本单由执行方直接消费，不交作者转派。
开工前必读：根目录 `AGENTS.md`、`docs/plans/README.md`、`docs/plans/PROJECT_PLAN.md`、`docs/agent-engineering-standards.md`（工程纪律与交付形态以规范为准，§10 四项是回复形态）。
坐标系＝`docs/plans/PROJECT_PLAN.md` §4.2。本波**零产品代码、零新判据、零门槛改动**：做的仍是把已经入库的守卫交给一双没参与过它们的眼睛，在盘上把"拆法原文"补出来。

> **本单的格数口径与第六～八波登记的不同，而且比"26 格"少 4 格。** 26 是 `43 − 17` 这种**摘要加减**得来的，从未逐格对过盘。本单改为**逐格对账**：分母＝第六波盘上五份红批 TRX 里去重后的 **46 格**（守门人当场用 `.local-notes/wave6-red-names-1022.js` 复数，脚本自己打印每份 TRX 的 `failed` 计数器与结果结点数，两者逐份相等：24／1／16／3／4＝48 个结果结点，去重后 46 格），减项＝第七波与第八波报告里**十一项字段齐全且能按消息＋读码判唯一因**的那些格（逐格点名，见 §③）。结论＝**已付 24 格、未付 22 格**，22 格里 **W2 一格的欠法不是"再跑一次"能还的**（§③ 末），所以真正要动手的是 **21 格**。这一处更正同时写进 `docs/dispatch-hds-wave8-20261022.md` §9.6 末尾与 `PROJECT_PLAN.md` §9，不靠改判据消账。

## ① 载体（复用现成的，但要把树挪到线尾；不要另建 worktree）

- 路径＝`C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-tip-1022`，当前 detached HEAD＝`6b6f549`，守门人 2026-10-22 深夜实测 `git status --short`＝**0 行**，机器上 `dotnet`／`testhost` 进程数＝**0**。
- **第 0 步（必做，且要在报告里贴出整段输出）**：
  `git -C "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-tip-1022" checkout --detach d2dbdee`
  然后 `git -C <载体> status --short`（必须＝0 行）与 `git -C <载体> rev-parse HEAD`（必须＝`d2dbdeedc1b8d0944122c115b79d816e81284b6c`）。
- 为什么要挪：`6b6f549` 比线尾少一格已归位的判据——`tests/Pry.Core.Tests/NarrativeStorageContractTests.cs` 在 `4039a50` 里把那条恒真的成员判定换成了会响的门（`+19/−1`，同一方法内替换，判据条数不变）。本单 §③ 的**第 10 格**（`Commit_built_on_a_stale_story_version_is_rejected_as_late`）要拆的就是这棵树上的现行代码；在旧树上拆＝拆一行已经不在线尾存在的代码，凭证无效。**除此以外的处置与第六～八波一致**：不许把这条漂移当回归去追、不许改任何在线尾已归位的判据。
- 挪树之后**不要**为"确认起点"跑一次全量：起点读数见 §②，直接进 §④ 的分组协议。收尾那一轮的解决方案级构建会重建全部差异，增量编译即可。
- 若要改用别的载体：先 `git worktree add --detach` 自线尾，自己承担一次解决方案级构建，并把这一点写进交付说明。**禁止**在主工作树里改文件、跑构建、提交（唯一例外见 §⑦ 的报告落点）。

## ② 起点读数（引用必须写全四项分项，不许只报总数）

Core total **527**／executed 525／passed 525／failed 0；Api **278**；App **233**；Client **108** ⇒ **总 1146＝passed 1144＋0 红＋2 条环境跳过**。两条跳过按 `outcome="NotExecuted"` 点名，均在 `Pry.Core.Tests.dll`：`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`。catalog **76**；`verify-repository` 在**主工作树**当前＝**549** 条受跟踪文件（这个数每落一份文档就顶一格，不是门槛）。本波不改任何条数 ⇒ 收尾复证必须还是这些数字，差一条就是问题，不是"差不多"。
**`notExecuted` 计数器恒 0**：跳过不进那一栏，只能逐条点结果结点，别把它读成"没有跳过"。

## ③ 逐格账（分母 46＝盘上红集合；已付 24；未付 22）

### ③.1 已付的 24 格（凭证在报告里，本单不重跑，只列名供对账）

- 第八波 17 格（`docs/eval/reports/2026-10-22-qa-wave8-waves1-remaining-two-knives.md` §三补～§十二）：`ContextBudgetTests` 8 格全列、`AttachmentLimitTests` 4 格全列、`ContextWindowLoadTests` 的 `ForModelWindow_agrees_with_the_loader_on_the_default_profile`（W3，清洁凭证＝守门人单枚轮 `run8-gk-mw3-only`）与 `Loading_the_default_profile_at_full_context_yields_a_positive_input_budget`（W1）、`StickerContentLengthGuardTests` 3 格全列。
- 第七波 7 格（`docs/eval/reports/2026-10-22-qa-wave7-three-faces-with-incremental-report.md` §四／§五／§九）：`UpstreamProvenanceAndDependencyShapeTests` 3 格全列（M2 打红 `No_package_reference_floats` 与自探针格 `The_scanners_see_the_shapes_they_claim_to_ban`——该格归属在 §三.167 已按读码判为"同一真相源的第二道守卫"，不是两枚互相污染；M3 打红 `Only_the_authorised_upstreams_are_cited_and_the_two_lists_agree`）、`MemoryDualSchemaLedgerTests` 2 格全列（M5 目标格 `The_two_schema_ledgers_coexist…` ＋同因多响 `Both_refusals_travel_the_presentable_chain…`，§4.4 已按消息形状判为一枚突变的两处同因红）、`MemoryDeletionSeparationTests.N12_Component_version_two_is_recorded_and_an_unknown_version_refuses_startup`（同因，§4.4）、`MemoryManagementClientTests.Confirm_keep_is_an_empty_post_that_escapes_the_character_on_the_query`（M6，§五）。
- **口径明写**（这一条决定本单的分母，必须照用不许自创）：一格算"已付"＝它同时具备①被改文件:行与 before→after 原文、②`git diff --stat`、③跑法整行、④TRX 文件名、⑤红格全名＋消息要点、⑥还原方式与 SHA-256、⑦还原后 `git status --short` 行数；并且**该格在本批内能被消息形状＋读码判到唯一充分因**。批次红不天然等于"没归属"——判不到唯一因才退回未付（第八波 W3 就是这种：批2 里两枚 ContextBuilder 突变各自都能打红它，所以守门人另起单枚轮）。**"够不着"与"没拆"是两件事**：前者要有实测过的配方清单，后者不许写成前者。

### ③.2 未付的 22 格（本单要动手的 21 格＝前 21 行；第 22 行见 §③.3）

| 组 | # | 未付格全名（去 `Pry.<X>.Tests.` 前缀） | 所属那把刀 | 被指的产品行（读码起点，不是你唯一能改的地方） |
|---|---|---|---|---|
| G1 Core | 1 | `DataBackupServiceTests.Secrets_logs_runtime_and_user_imports_never_enter_or_leave_the_bundle` | `8ef7d01`（HDS-001） | `src/Pry.Core/Backup/*.cs`（打包清单/排除集） |
| G1 Core | 2 | `DataRestoreServiceTests.A_bundle_from_a_future_schema_version_is_refused_before_anything_is_written` | `8ef7d01` | `src/Pry.Core/Backup/*Restore*`、`src/Pry.Core/Memory/SchemaVersionGuard.cs` |
| G1 Core | 3 | `SchemaVersionGuardTests.Initialize_refuses_a_database_registered_above_the_binaries_schema_version` | `8ef7d01` | `src/Pry.Core/Memory/SchemaVersionGuard.cs` |
| G1 Core | 4 | `MemorySchemaMigrationRegistryTests.Registry_is_named_unique_ordered_and_never_pins_a_future_version` | `8ef7d01` | `src/Pry.Core/Memory/MemoryDatabase.*.cs`、迁移登记表 |
| G1 Core | 5 | `NarrativeIdentityContractTests.Identity_must_be_a_host_issued_stable_token(candidate: "pry")` | `5145bde`（HDS-002） | `src/Pry.Core/Narrative/*.cs`（身份 token 校验） |
| G1 Core | 6 | `NarrativeIdentityContractTests.Identity_must_be_a_host_issued_stable_token(candidate: "pry-")` | `5145bde` | 同上（同一枚 Theory 的第二行：**两行都要各自判**，只红一行就照实登记哪一行没响） |
| G1 Core | 7 | `NarrativeStorageContractTests.A_story_version_ahead_of_the_ledger_is_rejected_too` | `5145bde`＋`e9a8055` | `src/Pry.Core/Narrative/DeliveryLedger.cs`、`StoryWriter` 的版次门 |
| G1 Core | 8 | `NarrativeStorageContractTests.Segment_text_must_be_taken_from_the_bound_narrative_verbatim` | `5145bde`＋`e9a8055` | 投递段绑定正文那一处比较（`DeliveryLedger`／`NarrativeBinding`） |
| G1 Core | 9 | `MemoryDeletionSeparationTests.N3_Deleting_one_of_three_supporting_messages_recalibrates_confidence_from_two` | `d2802ea`（片 B 另一把） | `src/Pry.Core/Memory/SqliteMemoryRepository.cs`（删除与置信度重标定那一段，**不是** `:100` 那枚版本门——`:100` 已被第七波 M5 付过） |
| G1 Core | 10 | `NarrativeStorageContractTests.Commit_built_on_a_stale_story_version_is_rejected_as_late` | `c148fbf`＋`5145bde` | 见 §③.4：这一格要**单独核**，并且必须说明它守的是线尾 `4039a50` 换进去的那段新断言（跑在 G1，属 `Pry.Core.Tests`） |
| G2 Api | 11 | `RuntimeDescriptorWriteGateTests.Second_host_on_the_same_directory_refuses_startup_without_touching_the_first_descriptor` | `b18ae27`（片 A） | `src/Pry.Api/Services/DataDirectoryWriteLease.cs`（`FileShare` 参数、`Acquire` 的 `IOException` 分支） |
| G2 Api | 12 | `RuntimeDescriptorWriteGateTests.Refusal_with_no_descriptor_yet_writes_none_and_a_successor_gets_the_lease_after_the_lock_is_released` | `a0f7bb1`／`452cd4c` | 同上＋`RuntimeDescriptorService.cs`（**已证够不着的那一向不要再追**＝"条件写复查失败后仍覆盖描述文件"，见 `docs/dispatch-hds-wave5-20261022.md`「出闸前预检」） |
| G2 Api | 13 | `RuntimeDescriptorWriteGateTests.A_refused_second_host_leaves_the_first_instances_open_plan_and_pending_message_untouched` | `dc5b010`（片 F5 `DEF-WRILEASE-ORDER-01`） | `RuntimeDescriptorService.cs` 里"取锁"与"取消在飞计划"的先后顺序 |
| G2 Api | 14 | `RuntimeDescriptorWriteGateTests.Exit_cleanup_never_deletes_a_descriptor_owned_by_a_successor` | `b18ae27` | 退出清理那一处的属主判定（`RetryAllowed`／属主比较） |
| G2 Api | 15 | `ResourceLimitLedgerCoverageTests.Every_limit_shaped_constant_has_a_row_in_the_ledger` | `bca8b42`（片 E2） | 真相源＝`docs/resource-limit-ledger.md`（临时删表里一行，**数值一字不改**） |
| G2 Api | 16 | `ResourceLimitLedgerCoverageTests.The_guard_sees_a_row_being_dropped_and_a_row_drifting_off_its_line` | `bca8b42`＋`5593e1a` | 同上（这一格本身是"守卫看得见"的自探针，突变要作用在真表上） |
| G2 Api | 17 | `CharacterIntelligenceLedgerCapBehaviorTests.A_receipt_pushed_out_of_the_window_is_refused_not_guessed` | `5593e1a` | 角色智能账本上限那把尺的产品侧 |
| G2 Api | 18 | `CharacterIntelligenceLedgerCapBehaviorTests.A_turn_past_the_pending_cap_drops_the_oldest_entry_and_keeps_the_newest` | `5593e1a` | 同上（pending cap 淘汰顺序） |
| G3 Client | 19 | `StorageGuardErrorPresentationTests.A_guarded_refusal_is_presented_verbatim_as_storage_error` | `8ef7d01` | `src/Pry.Client/PryErrorPresentation.cs`、`src/Pry.Contracts/UserPresentableErrorContract.cs` |
| G3 Client | 20 | `StorageGuardErrorPresentationTests.The_guard_arm_precedes_every_generic_bucket` | `8ef7d01` | 同上（判定顺序：守卫分支必须早于通用桶） |
| G3 Client | 21 | `PryBackendClientTests.Delete_conversation_asks_for_the_cascade_in_lowercase_on_the_query` | `2eba867`（片 F1 第二枚路由） | `src/Pry.Client/PryBackendClient.cs` 里 `api/v1/conversations/{id}` 那一枚路径的 query 拼接（`?cascade=true` 的大小写；**不是** 已付的转义那一格） |
| — | 22 | `ContextWindowLoadTests.Every_downgrade_candidate_keeps_a_positive_input_budget`（W2） | `faf8ecf` | **本波不派跑**，见 §③.3 |

### ③.3 第 22 格（W2）的欠法：不是"再跑一次"能还的

第八波实测：单枚 M-CB1（`ContextBuilder.cs:34` 去掉半窗夹）与 M-CB1∧M-CB6（`:35` 再去掉 `- MinimumInputTokens`）两种配方都 `Passed`。根因（读码＋实测）＝`ContextBuilder.cs:39` 的返回门是 `inputBudget >= MinimumInputTokens(512)`，两枚夹子同拿掉后归一化返 **false**，`ModelPerformancePolicy.WithContext:48` 落兜底 `MaxOutputTokens = 1`，反而给出一个正输入预算 ⇒ 这一格是**设计性冗余（防御纵深）**：在产品侧现有形状下它结构上不会红。
本波处置＝**不重跑、不追红、不改判据、不删断言**。若你想为它取证，唯一合法的路是先提出一个"能同时让 `:39` 返 true 又让降级候选的输入预算非正"的具体配方，并在报告里先做零构建读码证明该配方可达；拿不出配方就照实登记"仍够不着，与第八波同因，本轮未再跑"。**不许为了让它响而放宽或删除任何既有断言**，那属于改判据而不是补证据。

### ③.4 第 10 格的单独核（`c148fbf` 那条集合成员判定）

第六波 §⑤.3 明登过：盘上红集合里有这一格，但没有证据说明改的就是那一行。`c148fbf` 是**测试侧**一行（`Assert.False(Snapshot().Any(...))` → `Assert.DoesNotContain(Snapshot(), ...)`），而线尾 `4039a50` 又把那一带整体换成了会响的新断言。所以这一格的"拆法原文"必须回答两个问题，缺一不可：
1. 把线尾那段新断言（`NarrativeStorageContractTests.cs` 里 `4039a50` 引入的 `TryAppend` 迟到提交判据＋`Assert.Empty(Snapshot())`）临时改坏 ⇒ 这一格或 §③.1 里第七波已付的 `Late_commit…` 那一格，谁响、消息是什么、锚在哪一行；
2. 同时说明这一格**自身**在产品侧守的是哪一行（`DeliveryLedger.cs:79` 的未登记提交门已被第七波守门人自证轮付过，别重复领同一份凭证）。
如果结论是"这一格与已付那格同因、不构成独立覆盖"，就照实登记这一格为**并入已付**，并在报告里写清并入哪一格、凭哪条读码——**不许**把它静默算成"已拆"。

## ④ 分组批量跑法（这一节是省弹窗的设计，照做而不是各自发挥）

三组＝G1 Core（第 1–10 格，共 10 格）、G2 Api（第 11–18 格，共 8 格）、G3 Client（第 19–21 格，共 3 格）⇒ 21 格全部分完，无漏项无重复。每组一轮：

1. **先做该组全部突变的零构建预检**（读码，不起 `dotnet`）：对每一枚回答三个问题——这一行由谁调用、一个生命周期里跑几回、什么输入能让它走到"拒绝／越界"那一支。答不出第三问的，当场改写成"把仍然全绿当证据采下来＋登记这一向够不着"，**不要**为了留下一抹红去追一个不可能存在的红。§〇 要在动任何突变之前写完，并逐格列出"我打算改哪一行、预期哪一格响"。
2. 把该组全部有效突变一次性写盘，逐枚记 `git diff --stat`（必须＝N 文件 N 行，且只有你预期的那些文件）。突变只许改语义、不许让工程编译不过（改常量、改比较方向、改分支条件；不要删类型、不要改签名）。
3. 该组一次定向跑（隐式构建即算这一组唯一一次构建）：
   `dotnet test tests/Pry.<X>.Tests -c Release --results-directory "<本轮专属绝对路径>" --logger trx`
   ——`--logger trx` 不许省；不给它时目录里一个 `.trx` 都不会有，而控制台照样打印"已通过!"，那份绿不可引用（本仓库踩过两次）。**结果目录只认长参 `--results-directory` 的绝对路径**，短参会把路径拼到各程序集 `bin\Release\net10.0\` 之后，四条测试都没跑仍 `exit=0`。
4. 逐格读回，红格集合必须与预期**逐格对上**："预期红而实绿"＝该枚为空转，登记够不着或改配方再来一次（改配方算进预算）；"多出来的红"＝先按消息形状＋读码判它是不是同因，判得出唯一因就按 §③.1 口径付掉并明写归属，判不出就把该格拆成**单枚轮**（每枚一次定向跑，用 §⑤ 的机动额度），并说明为什么批内不可分。
5. 逐字节还原（`git checkout -- <你改过的那些路径>`，只在载体内、只针对自己那一行；或事先二进制备份再 `Copy-Item` 还原），事后 SHA-256 与 `git show HEAD:<file>` 的哈希比对；PS5.1 的 `Set-Content -Encoding UTF8` 会补 BOM，不许拿它当还原手段。
6. **该组还原后不再单独开一次定向绿跑**（本波的省弹窗设计，与第六／八波不同，理由与替代证据要照实登记）：全部还原完成后由 §⑤ 的收尾全量那一轮充当"每组都复绿"的凭证，它比逐组绿跑覆盖面更大。若收尾那一轮里某组仍有红，就是那一组没还原干净——按事实登记，不要把责任推给"绿跑没开"。

收尾那一轮＝解决方案级 `dotnet build Pry.slnx -c Release` 一次＋全量 `dotnet test Pry.slnx -c Release --no-build --results-directory "<绝对路径>" --logger trx` 一次，先 `ls *.trx` 数到**四份**再读 `<Counters>`，并按 §② 顶账（四分项逐位对上＝1146＝1144 绿＋0 红＋2 条同名跳过）。

## ⑤ 预算、停刀与弹窗纪律（作者 2026-10-22 现令，违反一次就要收回整单）

- 预算上限＝解决方案级构建 **1** 次（收尾那一轮）、全量 **1** 次、定向跑 **3 次（三组各一）＋ 3 次机动**（机动只给两件事：§④.4 的单枚归属轮、§④.1 改配方后的复跑）。用完即停，把缺口如实交回，不要"再确认一遍"。**单工程构建是定向跑的隐式代价，另记不另算额度**，但要在预算表里如实列次数。
- **每一次** `dotnet` 之前（含单工程构建、含 `--no-build` 的 test）先跑：
  `powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Users\24782\Desktop\Pry_Project\.local-notes\game-preflight5-1022.ps1"`
  并把整段输出（含 `VERDICT=` 那行）原样贴进报告。`VERDICT=HOLD` ⇒ 就地停刀并回报；不许等三分钟再试、不许换更省事的跑法绕开、不许自己改预检脚本的判据。
  **v5 是第八波夜里新立的那把**（v4 会把"焦点落在桌面本身"误判成全屏应用，从而在你最不该被打扰的时候反而把你锁死），判据只多了一条例外＝焦点窗属于 `explorer` 且类名是 `Progman`／`WorkerW` 或标题是 `Program Manager` ⇒ 只打印 `DESKTOP-SHELL-FOCUS (informational only, does not hold)`，不解除任何 HOLD。三向实测见 `docs/dispatch-hds-wave8-20261022.md` §9.5。**这一条不构成对任何门槛的放宽**。
- `free_memory_gb` 低于 6 只是 WARN，不阻止起跑；只有真出现 SQLite `disk I/O` 或 `out of memory` 形状才按作废处理。**不要自造比工单更严的作废判据**。
- 报告按批次**增量落盘**（冷启动有 150 轮上限：每完成一组就先把那一组写进报告文件，再继续下一组），撞上限也要留下已写部分为真账。

## ⑥ 硬约束（越线即作废）

- 零提交：本波不产生任何 `src/`／`tests/`／`docs/` 提交；所有改坏只允许存在于载体工作树并在报告前逐字节还原（§① 第 0 步的那次 `checkout --detach` 除外，它是挪树不是提交）。
- 不许为了让仪器好看而放宽、删除、注释掉任何既有断言；不许改 `docs/resource-limit-ledger.md` 的数值；不许动 `.5` 检索地板、`2400`、五档超时、`MinimumUsableTokensPerSecond = 12`、`4096` 地板。
- 突变要改"判据指向的那个名字／那一行本身"；不许新建一个含该名字的文件去骗过按文本扫的门，也不许用"把那行注释掉"代表"把它拿掉"。
- 多分支的修法要按分支逐个撤销、逐个取红；还原前先 `grep -c` 数一遍改动点是否齐全。
- 不碰作者的任何进程；不把 `%LOCALAPPDATA%/PryCompanion` 当测试载体（只读探查也不算许可）；不 `git tag`、不 `push`、不打包；不许动主工作树任何分支指针与 `refs/heads/*`。
- 主工作树里他人名下的两枚暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）不属于本单，任何情况下不读改、不提交、不 `stash`、不 `clean`；`licenses/**`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md` 一字不许碰。
- **状态词与结案词不由执行方写**：报告里写事实与读数，不写"已结案／已验收／基线已改写"；在册基线的改写权在守门人的登记刀。本波若判据条数有变化（不该有）必须在报告里写出等式与原因。

## ⑦ 交付物

- 唯一允许你写进主工作树的东西＝新文件 `docs/eval/reports/2026-10-22-qa-wave9-twenty-one-cell-remaining-ledger.md`（报告 id 建议 `eval-QA-WAVE9-1022`）。写完即停，不许 `git add`、不许提交。
- 报告结构（按这个顺序，且 §〇 先落盘）：§〇 挪树输出＋逐格计划（我要改哪一行、预期哪一格响）→ §一/二/三＝G1／G2／G3 各组的十一项字段逐格表＋红集合与预期的对账 → §四 收尾那一轮的四个分项与顶账 → §五 预算终账（构建／全量／定向／单工程构建／逐次预检整段原文）→ §六 够不着清单（每一格附实测过的配方名与 TRX，不许用"不需要拆"）→ §七 偏差自报（哪一条纪律差点没守住、哪里与本单要求有出入）。
- 每一格都要有：载体提交号、被改文件:行、突变原文（before→after）、`git diff --stat` 数字、跑法整行命令、TRX 文件名、红格用例全名＋消息要点、还原方式与 SHA-256、还原后 `git status --short` 行数。
- 交付说明附：逐次预检整段输出、三组红批的读回、收尾那一轮的四个分项与顶账、载体归零证明（`git status --short`＝0 行＋HEAD＝`d2dbdee`）。

## ⑧ 执行结果（守门人登记，2026-10-22 深夜）

执行方＝冷启动子 Agent（`eval-QA-WAVE9-1022`，报告 315 行→复证后 8 章完整落盘）；本节只写守门人当场从盘上核过的事实，lane 原文一字未改。

- **§① 的挪树令兑现**：载体 HEAD＝`d2dbdeedc1b8d0944122c115b79d816e81284b6c`、`git status --short`＝**0 行**，第 0 步输出原样在报告 §〇.1；`git merge-base --is-ancestor 4039a50 d2dbdee` 由 lane 自证、守门人复算为真 ⇒ 第 10 格所需的那段线尾新断言确在现行树上。
- **21 格全付、无一空转**：四批红并集去重＝23 格＝§③.2 前 21 行逐格点名＋2 格 §七.2 自报的同因附带红（`MemoryDualSchemaLedgerTests` 的 :56／:86 两处平台账断言，被第 3 格的 `MemoryDatabase.cs:197` 顺带打红；第七波付的是同两方法的组件账半 :48／:74 ⇒ 不重复领凭证、也不撤销原归属）。逐轮计数器守门人逐份读回：Core 527／525／513／12、Api 批 278／278／272／6、Api 机动 278／278／275／3、Client 108／108／105／3。
- **拆法原文的质量这一回经得起逐行查**：21 枚被指产品行守门人当场在载体树上逐行验在（行号与文本见报告 §八.3，含 `DeliveryLedger.cs:151`、`NarrativeCommitWriter.cs:141`、`SqliteMemoryRepository.cs:396`、`Program.cs:157`、`RuntimeDescriptorService.cs:160`、`DataDirectoryWriteLease.cs:65`、`CharacterIntelligenceGateway.cs:140`／`:200` 等）；抽 6 份 SHA-256 首尾与自述逐字相同；机动轮三格的 StackTrace 里字面含 `DataDirectoryWriteLease.cs:65`，那枚改坏由栈直接证明而非消息推测。
- **W2 没有被违规重跑**：四批红集合里没有任何 `ContextWindowLoadTests` 格。`resource-limit-ledger.md` 只删行、数值未动，还原后该行在位。
- **出口读数与归位**：收尾四份 TRX＝Client 108／Api 278／Core 527-executed 525-passed 525／App 233 ⇒ **总 1146＝1144 绿＋0 红＋2 条同名跳过**，与在册基线逐字相同、判据条数变化 0 ⇒ **在册基线照旧，本波不改写**；`verify-repository` 随本刀 550→551（跟踪文件条数，非门槛）。产物只有 8 份 TRX、五个批次目录 ⇒ 无第五轮、无"多跑一轮求确认"。
- **两处按最严口径仍挂（不追认字面合规）**：① §五 只有 5 次预检整段原文，G1 那次只剩结论（与第六波同一条欠账形状）；② 本单 **§③.4 与 §⑥ 正面冲突**＝要执行方临时改坏一段既有断言、又禁止放宽或删除任何既有断言，**这条是工单作者的编写错误，不是执行方违规**；lane 选 §⑥、以产品侧 `:141` 突变＋读码回答那两问并自报偏差，守门人认这个处置。留下的未验一问＝"第七波 `4039a50` 那段新断言对第 10 格是否承重"，按读码为不承重（:281 与 :341 两个不同 [Fact]、各守不同产品行，实测 :141 突变下 :281 那格保持绿），但没有盘上反证；第七波那一格的红仍是守门人本人补拿的那一次。
- **一条方法更正（连带纠正守门人自己第六波的取证）**：`%TEMP%\Microsoft.NET.Workload_*.log` 不能当 dotnet 调用次数用——波九窗口 19:33～20:27 实测 76 份，而凌晨 00:0x／03:5x／22:3x 等无人跑测的时段同样每十分钟 10～30 份，属 SDK 噪声。第六波 §⑧"十份日志与九批次一一对上"那条推断自此降级为不可复证；调用次数只认 TRX 份数与产物时间戳。本波盘上能证＝测试 4 次＋全量 1 次＋解决方案级构建 1 次（§⑤ 上限 1／1／3＋3，机动 3 中用 1，未超支）；自述的 5 次单工程构建属定向跑隐式代价、不占额度、盘上证伪不了。
- **进程残留实测**：8 个 `dotnet` 全部起于 20:02:40（收尾构建那一刻）、合计工作集 31 MB、无 `testhost`、无作者名下 Pry／llama 进程 ⇒ 属 build-server 节点复用；守门人未强杀（不碰作者进程的纪律照守，且没有读数需要靠杀进程换取）。
- **本波之后拆法原文的账**：分母 46＝已付 45、欠 1＝W2（设计性冗余，还它需要一个能让 `ContextBuilder.cs:39` 返 true 同时降级候选预算非正的可达配方，没有配方就不派跑）。其余在册欠账不变＝`QA-TIP1020-02`（卡他人暂存项）、`QA-TIP1019-06` teardown 总清、`ModelProcessRegistry.cs:63` 第二道门的新缝、F2①"降级继续写"一向，另加 G1 预检原文 1 次。逐格证据与效力边界＝报告 §八（`eval-QA-WAVE9-1022` 同文件）。
