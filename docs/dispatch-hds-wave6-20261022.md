# HDS 第六波派单（2026-10-22 深夜）｜第一～四波九把守卫的独立复采＋片 F1 那枚转义红的换人复判

派单人：守门人（Architecture / Integration / Release）。执行方式＝守门人自起冷启动子 Agent，工单是给执行方直接消费的，不交作者转派。
坐标系＝`docs/plans/PROJECT_PLAN.md` §4.2。本波**零产品代码、零新判据、零门槛改动**：做的是把已经入库的九把刀的守卫，交给一双没参与过它们的眼睛，在盘上重新拆一次。

---

## ① 载体（已备好，照用，不要另建）

- 路径＝`C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-tip-1022`，detached HEAD＝`6b6f549`，`git status --short` 入场＝**0 行**，四份二进制已于本轮 15:32 构建过（`.local-notes/qa-tip-1022-build.log`）。
- 为什么可以直接复用：守门人已当场证明 `git diff 6b6f549..a508246 -- src tests scripts docs/resource-limit-ledger.md Pry.slnx Directory.Build.props` **输出为空**——线尾 `a508246` 之后只动过文档，代码树与这个载体逐字节相同。省下的正是他机器上那一次构建。
- 若要改用别的载体：先 `git worktree add --detach` 自线尾，再自己承担一次解决方案级构建，并把这一点写进交付说明。**禁止**在主工作树里改文件、跑构建、提交（唯一例外见 §⑤ 的报告落点）。

## ② 起点读数（不要为确认起点先跑一轮全量）

Core 527（executed 525／passed 525）／Api 278／App 233／Client 108＝**总 1146＝passed 1144＋0 红＋2 条环境跳过**；两条跳过按名点名＝`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`；catalog **76**；`verify-repository` 在 `6b6f549` 这棵树上＝**541**。本波不改任何条数 ⇒ 收尾复证必须还是这些数字，差一条就是问题，不是"差不多"。

## ③ 九把清单与每把的守卫面（拆不到就照实登记，不许圆）

1. `faf8ecf`＝HDS-011 AUD-01／06／04（联合上下文预算先于分配、DOCX 解压限额）。判据＝`tests/Pry.Core.Tests/ContextBudgetTests.cs`、`tests/Pry.Core.Tests/AttachmentLimitTests.cs`、`tests/Pry.Api.Tests/ContextWindowLoadTests.cs`；被指的产品行在 `src/Pry.Core/Prompting/ContextBuilder.cs`、`src/Pry.Core/TurnTaking/AttachmentTextExtractor.cs`、`src/Pry.Api/Services/ModelPerformancePolicy.cs`、`src/Pry.Api/Services/ConfigurationApplicationService.cs`。
2. `8ef7d01`＝HDS-001 可恢复地基（一致性备份、隔离恢复、未知高版保护）。判据＝`tests/Pry.Core.Tests/DataBackupServiceTests.cs`、`DataRestoreServiceTests.cs`、`MemorySchemaMigrationRegistryTests.cs`、`SchemaVersionGuardTests.cs`、`tests/Pry.Client.Tests/StorageGuardErrorPresentationTests.cs`；产品在 `src/Pry.Core/Backup/*.cs`、`src/Pry.Core/Memory/MemoryDatabase.*.cs`、`src/Pry.Client/PryErrorPresentation.cs`、`src/Pry.Contracts/UserPresentableErrorContract.cs`。
3. `5145bde`＋`e9a8055`＋`c148fbf`＝HDS-002 稳定身份与存储契约（纯领域类型＋契约文档）。判据＝`tests/Pry.Core.Tests/NarrativeIdentityContractTests.cs`、`NarrativeStorageContractTests.cs`；产品在 `src/Pry.Core/Narrative/*.cs`。注意 `c148fbf` 只改了一条迟到提交用例的集合成员判定，那一格单独核。
4. `1edf69e`＝HDS-011b AUD-07（贴纸附带文本与会话正文共用同一条限额）。判据＝`tests/Pry.Api.Tests/StickerContentLengthGuardTests.cs`；产品在 `src/Pry.Api/Services/ConversationApplicationService.cs`、`ConversationSessionService.cs`、`src/Pry.Contracts/ApiContracts.cs`。
5. `b18ae27`＋`a0f7bb1`＋`452cd4c`＝片 A `DEF-WRIGATE-01`（进程级独占写句柄把第二个宿主拒于启动之外）。判据＝`tests/Pry.Api.Tests/RuntimeDescriptorWriteGateTests.cs` 四格；产品在 `src/Pry.Api/Services/DataDirectoryWriteLease.cs`、`RuntimeDescriptorService.cs`。**已经证明够不着的一向不要再追**＝"条件写复查失败后仍然覆盖描述文件"（见 `docs/dispatch-hds-wave5-20261022.md` 的「出闸前预检」一节：`Write()` 只挂 `ApplicationStarted`、`DataDirectoryWriteGate` 无静态可变面、全仓只有一处租约构造点）。本把要拆的是另外几向＝`FileShare` 参数、`Acquire` 的 `IOException` 分支是否仍抛、`RetryAllowed` 判定、拒绝文案两处子串。
6. `374ecd8`＋`d2802ea`＝片 B `DEF-MEMDUAL-01`（库内两套结构版本账并存、高版走可呈现拒绝）。判据＝`tests/Pry.Core.Tests/MemoryDualSchemaLedgerTests.cs`、`MemoryDeletionSeparationTests.cs`；产品在 `src/Pry.Core/Memory/MemorySchemaMigrator.cs`、`SchemaVersionGuard.cs`、`SqliteMemoryRepository.cs`。
7. `e554483`＝片 E1 依赖与许可的机械面（四处留痕、白名单两表互校、浮动版本谓词、包名覆盖）。判据＝`tests/Pry.Api.Tests/UpstreamProvenanceAndDependencyShapeTests.cs`。**已登记缺口**＝浮动版本与包名覆盖两条此前只有测试内嵌样张、没有"真改坏真相源"那一向（当时怕碰别人的树）。本把**解禁**：允许在载体内临时把某个 `src/*/*.csproj` 的版本写成区间或 `10.0.*`、临时把 `docs/upstream-license-compatibility.md` 白名单表改一行，拆完逐字节还原并数 `git status --short` 归零；`licenses/**`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md` 仍然一字不许碰。
8. `bca8b42`＋`5593e1a`＝片 E2 资源限额总盘（唯一真源＋漏登记即红＋两处零判据格补判）。判据＝`tests/Pry.Api.Tests/ResourceLimitLedgerCoverageTests.cs`、`CharacterIntelligenceLedgerCapBehaviorTests.cs`；真相源＝`docs/resource-limit-ledger.md`。**数值一字不改**；反证方式是载体内临时删表里一行（该文件在这棵树里受跟踪，还原判据同上），证明"某把尺从账上消失 ⇒ 守卫红"。
9. `2eba867`＝片 F1 客户端边缘那两条路由 URL＋转义。这一枚此前由守门人本人补拿红＝按仓库纪律算**自证**，本把要由**非实现方、非守门人**的你在盘上重来一次：
   - 突变＝`src/Pry.Client/PryBackendClient.cs:188` 的 `?characterId={Escape(characterId)}` 改成 `?characterId={characterId}`；
   - 预期红格＝`Pry.Client.Tests.MemoryManagementClientTests.Confirm_keep_is_an_empty_post_that_escapes_the_character_on_the_query`（要点＝`Assert.Equal() Failure: Strings differ`，Expected 含 `a%26b`、Actual 含 `a&b`，锚在 `MemoryManagementClientTests.cs:39`）；
   - 样本必须用 `&` 这类"调用方不转义就没人替你转"的字符；**空格是无效样本**（`Uri` 解析会替你写成 `%20`，那条断言根本不守东西，本仓库 10-20 踩过）；
   - 顺带把同一把刀里另一条新路由 URL 的判据也拆一次（`api/v1/memories/{id}/confirm-keep` 与 `api/v1/conversations/{id}` 两枚路径，改动点在同一个文件的相邻方法）。

## ④ 分组批量跑法（这一节是省弹窗的设计，照做而不是各自发挥）

按程序集分三组：Core 组（第 1／2／3／6 把）、Api 组（第 1／4／5／7／8 把）、Client 组（第 2／9 把）。每组一轮：

1. 先做该组全部突变的**零构建预检**（读码，不起 `dotnet`）：对每一枚回答三个问题——这一行由谁调用、一个生命周期里跑几回、什么输入能让它走到"拒绝／越界"那一支。答不出第三问的，当场改写成"把仍然全绿当证据采下来＋登记这一向够不着"，**不要**为了留下一抹红去追一个不可能存在的红。
2. 把该组全部有效突变一次性写盘，逐枚记 `git diff --stat`（必须＝N 文件 N 行，且只有你预期的那些文件）。突变只许改语义、不许让工程编译不过（改常量、改比较方向、改分支条件；不要删类型、不要改签名）。
3. 该组一次定向跑（隐式构建即算这一组唯一一次构建）：
   `dotnet test tests/Pry.<X>.Tests -c Release --results-directory "<本轮专属绝对路径>" --logger trx`
   ——`--logger trx` 不许省；不给它时目录里一个 `.trx` 都不会有，而控制台照样打印"已通过!"，那份绿不可引用（本仓库踩过两次）。
4. 逐格读回，红格集合必须与预期**逐格对上**："预期红而实绿"＝该枚为空转，单独重跑一次或登记够不着；"多出来的红"＝组内突变互相干扰，此后把该组拆成单枚逐次跑，并在报告里说明。
5. 逐字节还原（`git checkout -- <你改过的那些路径>`，只在载体内、只针对自己那一行；或事先二进制备份再 `Copy-Item` 还原），事后 SHA-256 与备份比对；PS5.1 的 `Set-Content -Encoding UTF8` 会补 BOM，不许拿它当还原手段。
6. 还原后同组再跑一次，必须复绿。

收尾那一轮＝解决方案级 `dotnet build Pry.slnx -c Release` 一次＋全量 `dotnet test Pry.slnx -c Release --no-build --results-directory "<绝对路径>" --logger trx` 一次，先 `ls *.trx` 数到四份再读 `<Counters>`，并按 §② 顶账。

## ⑤ 预算、停刀与弹窗纪律（作者 2026-10-22 现令，违反一次就要收回整单）

- 预算上限＝解决方案级构建 **1** 次（收尾那一轮）、全量 **1** 次、定向跑至多 **8** 次（三组红绿各一＝6，另留 2 次给干扰拆复跑）。用完即停，把缺口如实交回，不要"再确认一遍"。
- **每一次** `dotnet` 之前（含单工程构建、含 `--no-build` 的 test）先跑：
  `powershell -NoProfile -ExecutionPolicy Bypass -File "C:\Users\24782\Desktop\Pry_Project\.local-notes\game-preflight4-1022.ps1"`
  并把整段输出（含 `VERDICT=` 那行）原样贴进交付说明。`VERDICT=HOLD` ⇒ 就地停刀并回报；不许等三分钟再试、不许换更省事的跑法绕开、不许自己改预检脚本的判据。
- `free_memory_gb` 低于 6 只是 WARN，不阻止起跑；只有真出现 SQLite `disk I/O` 或 `out of memory` 形状才按作废处理。**不要自造比工单更严的作废判据**（上一轮有人那样做过，读数差点被冤枉作废）。
- 被测脚本以 `exit N` 收尾时，每一次调用都要独立子进程；同进程 `&` 调用会让被调脚本把复核外壳一起终止。

## ⑥ 硬约束（越线即作废）

- 零提交：本波不产生任何 `src/`／`tests/`／`docs/` 提交；所有改坏只允许存在于载体工作树并在报告前逐字节还原。
- 不许为了让仪器好看而放宽、删除、注释掉任何既有断言；不许改 `docs/resource-limit-ledger.md` 的数值，不许动 `.5` 检索地板、`2400`、五档超时、`MinimumUsableTokensPerSecond = 12`、`4096` 地板。
- 突变要改"判据指向的那个名字／那一行本身"；不许新建一个含该名字的文件去骗过按文本扫的门，也不许用"把那行注释掉"代表"把它拿掉"（这类门按口径连注释一起匹配，注释掉＝仍然绿，那是作废的仪器不是反证）。
- 多分支的修法要按分支逐个撤销、逐个取红；还原前先 `grep -c` 数一遍改动点是否齐全。
- 不碰作者的任何进程；不把 `%LOCALAPPDATA%/PryCompanion` 当测试载体（只读探查也不算许可）；不 `git tag`、不 `push`、不打包。
- 主工作树里他人名下的两枚暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）不属于本单，任何情况下不读改、不提交、不 `stash`、不 `clean`。
- **状态词与结案词不由执行方写**：报告里写事实与读数，不写"已结案／已验收／基线已改写"；在册基线的改写权在守门人的登记刀。

## ⑦ 交付物

- 唯一允许你写进主工作树的东西＝新文件 `docs/eval/reports/2026-10-22-qa-wave6-rolling-resample-of-waves1-4.md`（报告 id 建议 `eval-QA-WAVE6-1022`）。写完即停，不许 `git add`、不许提交。
- 报告里每一格都要有：载体提交号、被改文件:行、突变原文（before→after）、`git diff --stat` 数字、跑法整行命令、TRX 文件名、红格用例全名＋消息要点、还原方式与 SHA-256、还原后 `git status --short` 行数。
- 明写"哪些向够不着、为什么"（引用读码结论，不要引用感觉）；不许把"没拆"写成"不需要拆"。
- 交付说明附：逐次预检整段输出、三组红批与绿批的读回、收尾那一轮的四个分项与顶账。
