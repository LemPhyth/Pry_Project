# eval-GK-WAVE6-REREAD-1022｜第六波执行方撞到自己的轮次上限、报告没写 ⇒ 守门人直接复数它留在盘上的 TRX

日期：2026-10-22 深夜。角色：守门人（Architecture / Integration / Release）。
载体＝`.local-notes/qa-tip-1022`（detached `6b6f549`）。工单＝`docs/dispatch-hds-wave6-20261022.md`。

## 0. 这份文件是谁写的，以及它不能替代什么

执行方＝守门人自起的一条冷启动 QA lane（不带本轮对话上下文），按工单在干净载体上拆第一～四波那九把刀的守卫。**它在 150 轮上限处被系统截断**，最后一句自述是"Core group complete (3 dotnet runs). Now reading the Api group files…"——这句本身已经过时：盘上证据显示它把 Api 组和 Client 组也跑完了，还跑了收尾那一轮全量，**但没有写出工单 §⑦ 要求的那份报告**（`docs/eval/reports/2026-10-22-qa-wave6-rolling-resample-of-waves1-4.md` 至今不存在）。

所以本文件是**守门人对它留在盘上的原始产物的当场复数**，不是那份报告的替身。能引用的只有盘上有的东西：TRX 里的计数器与失败用例全名、文件时间戳、载体当前的 `git status`。**它改的每一行原文（before→after）、`git diff --stat` 数字、还原 SHA-256、逐次焦点预检的整段输出都不在盘上** ⇒ 那些格按第⑤节照实挂着，不当已补。

## ① 盘上现场（起跑前与截断后各测一次）

- 载体 `git status --short`＝**0 行**，HEAD 仍是 `6b6f549` ⇒ 它所有突变在截断前已逐字节还原，树里没有留坏码。
- 产物目录＝`%TEMP%\qa-wave6-1022\{core-red, core-green, core-red2, api-red, api-red2, api-green, client-red, client-green, finish}`，每个目录各 1 份 TRX（`finish`＝4 份），**九份目录全部有 `.trx`** ⇒ 这一条不像前两轮那样出现"控制台绿而盘上无凭据"。
- 该目录还留着它自己写的两份读数脚本（`readtrx.ps1`、`counters.ps1`），其中 `counters.ps1` 会按 `outcome="NotExecuted"` 点名跳过项——它把跳过与失败分开数，这一点对得上仓库纪律。
- 自 16:37 起至 17:08，`%TEMP%` 里有 **10 份 `Microsoft.NET.Workload_*.log`**（16:37:43、16:43:26、16:44:16、17:03:02、17:04:30、17:04:56、17:06:37、17:07:08、17:07:27、17:07:41），时间与九次测试批次一一对上，多出的一份（17:07:27）落在收尾全量（17:07:43）之前 16 秒，形状＝工单 §④ 收尾那一轮要求的解决方案级构建。⇒ **dotnet 调用次数＝定向 8 ＋构建 1 ＋全量 1，正好是 §⑤ 写死的上限，没有超支。**

## ② 九把的红集合逐格对账（守门人数过计数器，非转述）

每份 TRX 先证 `failed` 计数器＝`<UnitTestResult outcome="Failed">` 结点数＝去重后的用例全名数（第一轮我自己那台仪器多印了一格，因为连 `<UnitTest>` 节点一起匹配了；按结果结点重数后才用）：

| 组 | 跑次 | total/executed/passed/failed | 红格数 |
|---|---|---|---|
| Core | core-red | 527／525／501／24 | 24 |
| Core | core-red2（过滤到迁移登记表那一类，6 格） | 6／6／5／1 | 1 |
| Core | core-green | 527／525／525／0 | 0 |
| Api | api-red | 278／278／262／16 | 16 |
| Api | api-red2（过滤到写锁门那一类，5 格） | 5／5／2／3 | 3（其中 1 格是首批没到的第 4 格） |
| Api | api-green | 278／278／278／0 | 0 |
| Client | client-red | 108／108／104／4 | 4 |
| Client | client-green | 108／108／108／0 | 0 |

按工单 §③ 的"每把 ⇒ 判据文件"映射对账，**去重后共 46 格红，九把的每一份列名判据文件都至少红过一格，没有任何一把在盘上是全绿**：

1. `faf8ecf`（HDS-011 AUD-01／06／04）＝`ContextBudgetTests` 8 格 ＋ `AttachmentLimitTests` 4 格 ＋ `ContextWindowLoadTests` 3 格 ⇒ **15 格**。
2. `8ef7d01`（HDS-001 可恢复地基）＝`DataBackupServiceTests` 1 ＋ `DataRestoreServiceTests` 1 ＋ `SchemaVersionGuardTests` 1 ＋ `MemorySchemaMigrationRegistryTests` 1（第二批评过滤跑才拿到）＋ `StorageGuardErrorPresentationTests` 2（Client 面）⇒ **6 格**。
3. `5145bde`＋`e9a8055`＋`c148fbf`（HDS-002 身份与存储契约）＝`NarrativeIdentityContractTests` 2（含两条 host-token 形态的 Theory 行）＋ `NarrativeStorageContractTests` 3，其中「迟到提交按陈旧版本被拒」那一格确实红过——工单点名要单独核的那格在红集合里，但它是被哪一行改坏的没有留痕（见⑤）。
4. `1edf69e`（AUD-07 贴纸附带文本共用同一把尺）＝`StickerContentLengthGuardTests` **3 格**（含 `TurnSubmission` 与 `DirectMessage` 两个入口各一）。
5. `b18ae27`＋`a0f7bb1`＋`452cd4c`（`DEF-WRIGATE-01` 进程级独占写句柄）＝`RuntimeDescriptorWriteGateTests` **4 格全红**：首批 278 全量里 3 格红、第 4 格"没有描述文件时拒绝启动不写盘、锁释放后继任拿到租约"仍绿；第二批把它单独过滤出来跑（5 格）才拿到红。⇒ **这一条按 §④.4 的形状记作"多分支要逐个撤销逐个取红"的正例**，不是空转。
6. `374ecd8`＋`d2802ea`（`DEF-MEMDUAL-01` 库内两套结构版本账）＝`MemoryDualSchemaLedgerTests` 2 ＋ `MemoryDeletionSeparationTests` 2 ⇒ **4 格**。
7. `e554483`（片 E1 依赖与许可的机械面）＝`UpstreamProvenanceAndDependencyShapeTests` **3 格**（浮动版本谓词、白名单两表互校、"扫描器真的看得见它宣称禁止的形状"）。
8. `bca8b42`＋`5593e1a`（片 E2 资源限额总盘）＝`ResourceLimitLedgerCoverageTests` 2 ＋ `CharacterIntelligenceLedgerCapBehaviorTests` 2 ⇒ **4 格**。
9. `2eba867`（片 F1 客户端边缘两条路由＋转义）＝`MemoryManagementClientTests.Confirm_keep_is_an_empty_post_that_escapes_the_character_on_the_query` **红** ＋ `PryBackendClientTests.Delete_conversation_asks_for_the_cascade_in_lowercase_on_the_query` **红** ⇒ **2 格**。

**这一把最重要的效力**：上一轮那一格 `2eba867` 的红是**守门人本人**补拿的，按仓库纪律记作自证。本轮它由一条没参与过这九把实现的 lane 在同一台机器的干净副本上重新拿到，红格名与预告逐字相同 ⇒ **`2eba867` 的转义判据自此不再只是自证**。

另外 `client-red` 里有一格不在九把清单上＝`RuntimeDiscoveryTests.A_descriptor_without_a_usable_port_is_refused(because: "端口超出范围", port: 70000)`。按 §④.4 这属"多出来的红"＝组内另一次改坏顺带扫到的邻格；因为改坏原文没留痕，**这一格不追认为任何一把的覆盖，也不判为异常**。

## ③ 收尾复证与顶账

`finish` 目录四份 TRX（守门人逐份读 `<Counters>`）：Client **108／108／0**、Api **278／278／0**、Core **total 527／executed 525／passed 525／failed 0**、App **233／233／0** ⇒ **总 1146＝passed 1144＋0 红＋2 条环境跳过**；两条按 `outcome="NotExecuted"` 点名＝`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`，与工单 §② 的起点读数**同名同数**。

本波零新判据 ⇒ 出口复证必须还是在册那份数，实测**零差**。**在册基线不因此改写，仍是 1146／0／2**（同一份数第二次由不同载体、不同人跑到，属复证加强，不属新读数）。

## ④ 这一轮的钱花在哪（对作者的那条现令负责）

- 作者 10-22 现令：构建前先看焦点窗，且不要反复弹。工单 §⑤ 据此把每一次 `dotnet` 前都要求跑预检并留档。
- 盘上能证的只有**调用次数＝10、时间窗＝16:37～17:08（41 分钟）**。**逐次预检的输出没有任何留档**（`.local-notes/` 在这段时间只有守门人自己的文件），所以"每一次起跑前都读了 `VERDICT`"这一条**无法从盘上证实，也无法证伪** ⇒ 记在第⑤节，不写成已合规。
- 省下的那一次构建是真的：载体复用的是 15:32 已建好的四份二进制（工单 §①），第一组批次直接在 16:37 起跑，起跑前没有一次"为了确认起点先跑全量"。

## ⑤ 这份复数不能替代的（明账，不圆）

1. **46 格红的改坏原文全部没有留痕**：工单 §⑦ 要求每格附 before→after、`git diff --stat`、跑法整行、还原 SHA-256、还原后 `git status` 行数。盘上只有红集合与绿复证。⇒ 本轮的红**可引用为"这些判据在一份干净树上被非实现方改坏过并当场报警"**，不可引用为"每一格都精确证明了 §③ 指的那一行产品码是唯一守它的门"。
2. **片 E1 那两向解禁后到底有没有真改坏真相源，无法证明**：§③ 第 7 把解禁了"临时改一枚 `.csproj` 的浮动版本／许可白名单表一行"。`UpstreamProvenanceAndDependencyShapeTests` 红 3 格与"只改测试内嵌样张"同样兼容（那些类本来就吃内嵌样张）。⇒ 这一向**仍算未补**，与上一轮的登记口径不变。
3. **`c148fbf` 那格"迟到提交的集合成员判定"单独核**：红集合里有那格，但没有证据说明改的是 `c148fbf` 那一行。⇒ 单独核这一条**未完成**。
4. **逐次焦点预检留档缺失**（见④）。
5. **执行方的报告本身不存在** ⇒ 工单 §⑦ 这个交付物按"未到付"登记，不因守门人写了这份复数而闭合。

## ⑥ 效力判定（判定＝可签的那一半，不含状态词与结案词）

- 九把守卫**在盘上都有非实现方拿到的当场红**，且三组还原后各自复绿、收尾全量与在册数逐位相同、载体归零 ⇒ 这一波的技术目的（"把已经入库的守卫交给另一双眼睛在盘上拆一次"）**达成**。
- 但达成的是**弱化版**：拆法原文缺失（⑤.1）。按仓库那条"正向探针≠门在守"，下一轮若要把这 46 格升成可精确引用的独立证伪，需要**换人重拆并把改坏原文当场落盘到报告**，或者由本 lane 的续跑把它已经写过的批注补出来。
- 在册基线一字未动＝**1146／0／2**；catalog **76**；`verify-repository` 随本刀落地从 **543** 顶到 **544**（受跟踪文件条数，不是门槛）。
- 零 `src/`、零 `tests/`、零 `docs/resource-limit-ledger.md` 改动；不 tag／不 push／不打包；**状态词与结案词不由本角色写**。
