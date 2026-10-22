# eval-QA-WAVE7-1022｜第七波三件活的增量报告（判据刀，零产品代码）

- 报告 id：`eval-QA-WAVE7-1022`
- 执行方：独立冷启动 QA／判据刀 lane（非产品负责人、非守门人）
- 工单：`docs/dispatch-hds-wave7-20261022.md`（§① 四条纪律／§② 载体与预算／§③ 三件活／§④ 落点与禁止事项）
- **本报告是交付物；读数不是。** 本报告按批次增量落盘：每做完一批立刻追加表格并写盘，然后再开下一批。
- **状态词与结案词不由执行方写**；本报告不写"已结案／已验收／基线已改写"。

---

## 〇、载体、入场读数与计划突变数（先写满，再动任何一刀）

### 0.1 载体

| 项 | 实测值 | 取法 |
|---|---|---|
| 载体路径 | `C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-tip-1022` | 工单 §② |
| 载体 HEAD 提交号（完整） | `6b6f5498ea8d32153d4d61de5bca04a6e1f32715`（detached，线尾 `6b6f549`） | `git -C <载体> rev-parse HEAD` |
| 载体 HEAD 提交主题 | `docs(plans): 第五波五片归位登记（F1 2eba867／F3 e31e4da／F5 dc5b010，出口 1146＝1144 绿＋2 条同名跳过）＋工单闭合＋本角色那次读错树的计数错账` | `git log -1 --format='%H %d %s'` |
| **入场 `git status --short` 行数（载体）** | **0 行**（干净，符合开工前置） | `git -C .local-notes/qa-tip-1022 status --short \| wc -l` |
| 主工作树 `git status --short` 行数（仅作旁观记录，不读改） | 7 行：2 枚他人名下暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）＋5 项未跟踪（`.qoder/`、`docs/character-learning-safety.patch`、`docs/research/HDS_INTERLUDE.md`、`docs/research/HDS_QQ_RESEARCH_INDEX.md`、`docs/research/QQ_BRIDGE.md`） | `git status --short`（主树） |

⇒ 载体入场条件满足（0 行），**不需要换目录重开**，也不存在"在脏树上跑"的情形。

### 0.2 起点读数（在册基线，工单 §②／§④）

| 程序集 | 读数 |
|---|---|
| Client | 108 |
| App | 233 |
| Api | 278 |
| Core | total 527 / executed 525 |
| **总盘** | **1146 ＝ passed 1144 ＋ 0 红 ＋ 2 条同名环境跳过** |

- 两条环境跳过按名点出，**不算红**：`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`。
- 跳过不进 `<Counters>` 的 `notExecuted`（恒 0），只能按 `<UnitTestResult outcome="NotExecuted">` 点名。
- 本波判据条数**预期持平**（件 1 属"把空断言换成可失败断言"，同一测试方法内替换，不新增用例）；若实际有变化，会在出口节写清等式与原因。

### 0.3 计划做的突变（事前声明，事后逐格对账）

| 编号 | 属于哪件活 | 被改文件（载体内） | 层级 | 是否需要构建 |
|---|---|---|---|---|
| M1 | 件 1（自证红） | `src/Pry.Core/Narrative/DeliveryLedger.cs`（`:79` 或 `:230` 的 `_commits.TryGetValue` 前置检查放开，或 `:61` 不写） | 产品侧 | 需要（Core＋Core.Tests 编译） |
| M2 | 件 2 | 一个真实 `.csproj` 的 `PackageReference` 浮动版本 | 依赖清单 | **绝不允许进入构建／还原图**，只用 `--no-build --no-restore` 跑 |
| M3 | 件 2 | `docs/upstream-license-compatibility.md` 白名单表的一行 | 文档真相源 | 不需要构建 |
| M4 | 件 3 | `faf8ecf` 预算负数一格所指产品行 | 产品侧 | 待读单确认 |
| M5 | 件 3 | `374ecd8` 双账拒绝一格所指产品行 | 产品侧 | 待读单确认 |
| M6 | 件 3 | `2eba867` 转义一格所指产品行 | 产品侧 | 待读单确认 |

### 0.4 预算声明与占用账（硬顶：解决方案级构建 ≤1、全量 ≤1、定向 ≤3）

| 资源 | 硬顶 | 已用 | 剩余 |
|---|---|---|---|
| 解决方案级构建 | 1 | 0 | 1 |
| 全量 | 1 | 0 | 1 |
| 定向跑 | 3 | 0 | 3 |

> 每一次 `dotnet`（含 `--no-build`、含单工程）之前必须先跑
> `powershell -NoProfile -ExecutionPolicy Bypass -File "C:\Users\24782\Desktop\Pry_Project\.local-notes\game-preflight4-1022.ps1"`，
> 并把整段输出（含 `VERDICT=` 行）**当场追加进本报告**。`VERDICT=HOLD` ⇒ 立刻停刀回报，不等、不绕、不改预检脚本。
> 守门人出闸前最后一次读到的是 `VERDICT=BUILD_OK`（焦点 QQ 普通窗、`gpu_util=3%`、`free_memory_gb=8.5`）。
> `free_memory_gb` 低于 6 只是 WARN 不阻止起跑；只有真出现 SQLite `disk I/O`／`out of memory` 形状才算作废。

### 0.5 收尾预案（防再次撞轮次上限）

若感到轮次／工具调用不足：**立刻停止开新批次**，把"做到哪一格、哪些没做、盘上证据在哪个目录、缺哪些字段"写进本报告末尾的"缺口"节，然后结束。宁可交带缺口的报告，不交"活干了痕迹没了"。

---

## 一、件 1｜`DEF-GREENGATE-01`：守门人三段读码事实的复核（零构建，当场可复）

载体＝`6b6f549`，全部行号为在载体内直接读到的实际行号。

| 段 | 守门人 §③ 的断言 | 本 lane 复核到的原文 | 行号是否一致 |
|---|---|---|---|
| ① | `DeliveryLedger.Snapshot()` 返回投递段集合 `_segments`，不是 `_commits` | `public IReadOnlyList<DeliverySegment> Snapshot() => _segments.Values.ToArray();`（`_commits` 声明在 `:36`） | **实际在 `src/Pry.Core/Narrative/DeliveryLedger.cs:46`，工单写 `:45`（差一格，属行号记账误差，不影响结论）** |
| ② | `TryCommit` 全程只写 `_commits`，从不追加投递段 | `NarrativeCommitWriter.cs:80` 起 `TryCommit`，`:91` 先过 `IsAuthorizedWriter` 门，`:196` 才 `Ledger.RegisterAppliedCommit(bound, request.MaxOutboundSegments);`；`DeliveryLedger.RegisterAppliedCommit` 体内唯一写点是 `:61` `_commits[commit.CommitId] = new RegisteredCommit(...)`；`_segments[...]` 的写点只出现在 `TryAppend`／`TryMarkDelivered`（`:201`）／`TryMarkCancelled`（`:221`） | 一致 |
| ③ | 测试方法 `:281`～`:313` 之间一次 `TryAppend` 都没有 | 方法体 `:281`～`:313` 内只有 `Acquire`／`CommitAndAssert`／`TryCommit`／`StateOf`／`TryHandover`；全文 `TryAppend` 命中行 `:161 :163 :178 :180 :184 :186 :195 :201 :212 :229 :230 :245 :260 :265 :273 :443`——**没有一条落在 281～313 区间** | 一致 |

⇒ 三段事实全部复核成立：`Snapshot()` 到原 `:313` 时是**空集合**，`Assert.DoesNotContain(空集合, 谓词)` 恒真，这一行**结构上红不了**。

### 1.1 改判选项＝**A**（把 `:313` 换成可失败断言），理由

1. **A 能把这一格变成真门，B 只能把它变成一行注释。** 本单的缺陷编号是"判据刀"性质的 `DEF-GREENGATE-01`，缺的是**证据价值**；B（删行＋挂到 `:273`／`:441-443` 名下）在盘上不留下任何能响的东西，下一波再读码时又得重新推一遍。
2. **A 不需要产品代码。** 工单 §③ 允许"用现有公开面构造判据"，而 `DeliveryLedger` 现成的公开面就够用：拿**当前代次 host-b 的票**（`OutboxOwnershipRegistry.IsAuthorizedWriter`，`src/Pry.Core/Narrative/OutboxOwnershipRegistry.cs:204-229`：票的 `WriterId` 与 `OwnershipEpoch` 都相等才放行）对两个迟到 `CommitId` 各走一次 `TryAppend`——所有权门（`:71`）先过，`_commits.TryGetValue`（`:79`）就是"提交未登记"那道门，返回 `NarrativeContractReason.SegmentCommitUnknown`（`:81`）。这正是 §③ 给的 A 路线的构造法，**零产品接口新增**。
3. **B 还有一个本单没授权的副作用**：它要求"更正契约文档里引用那一行作为守卫的句子"，落点会是 `docs/narrative-storage-contract.md:278`（该句写"账本里没有迟到 commit 的段"）。改文档句子的授权在 B 分支下才成立，选 A 就不需要动它——而且 A 之后那句描述**仍然为真且现在有判据了**，不必更正。
4. 承重面没有因为这一刀被搬走：`docs/narrative-storage-contract.md:38` 第 9 行按**方法名**引用这条守卫（不引用行号），A 保留方法名与全部既有断言，该文档无需变更。

### 1.2 落盘改动（非突变，是本波唯一一处判据改动）

| 项 | 值 |
|---|---|
| 载体提交号 | `6b6f5498ea8d32153d4d61de5bca04a6e1f32715` |
| 被改文件:行 | `tests/Pry.Core.Tests/NarrativeStorageContractTests.cs:313`（原 `:313` 一行 → 现 `:313`～`:331`） |
| before 原文 | `Assert.DoesNotContain(_writer.Ledger.Snapshot(), s => s.CommitId is "commit-late" or "commit-late-2");` |
| after 原文 | 10 行中文注释（把"为什么这行红不了"与"承重在哪"钉在原地）＋ `foreach (var lateCommitId in new[] { "commit-late", "commit-late-2" })` 内 `Assert.False(unregistered.Accepted)` 与 `Assert.Equal(NarrativeContractReason.SegmentCommitUnknown, unregistered.Reason)`＋一条 `Assert.Empty(_writer.Ledger.Snapshot());`（完整 diff 见 `git diff` in 载体，已核过无编码／BOM 损伤） |
| `git diff --stat` | `1 file changed, 19 insertions(+), 1 deletion(-)` |
| 判据条数变化 | **0 条**（同一测试方法内替换断言，未新增／删除 `[Fact]`；`ContextBudgetTests`／`MemoryDualSchemaLedgerTests` 等未动）⇒ 出口等式预期仍＝总 1146／passed 1144／0 红／2 条同名跳过 |
| 还原方式 | **不还原**——这是交付物本体，随报告以未提交状态留在载体（§④"零提交"）；收尾全量将证明它在干净码上绿 |
| 该项结论 | 空断言已换成两条可失败断言；**是否真会响由下面 M1 自证红那一格决定，此处不下结论** |

### 1.3 本波最终突变清单（取代 §0.3 的计划，逐枚都读码定位过）

| 编号 | 件 | 被改文件:行（before→after 见各自小节） | 需要构建？ | 跑法 |
|---|---|---|---|---|
| M1 | 件 1 自证红 | `src/Pry.Core/Narrative/DeliveryLedger.cs:79` `_commits.TryGetValue(key.CommitId, ...)` → 查找键换成 `"commit-1"` | 是（Core 单工程） | 定向 #Core |
| M2 | 件 2 | `src/Pry.App/Pry.App.csproj:11` `Version="11.3.20"` → `Version="11.3.*"` | **绝不进入构建／还原图** | 定向 #Api（`--no-build --no-restore`） |
| M3 | 件 2 | `docs/upstream-license-compatibility.md:46` 决定列 `✅` → `❌` | 否 | 同上（同一批次） |
| M4 | 件 3（`faf8ecf` 预算负数） | `src/Pry.Core/Prompting/ContextBuilder.cs:24` `InputBudget => (long)ContextTokens - OutputTokens - SafetyTokens` → 外包 `Math.Max(0, ...)` | 是（与 M1 同批） | 定向 #Core |
| M5 | 件 3（`374ecd8` 双账拒绝） | `src/Pry.Core/Memory/SqliteMemoryRepository.cs:100` `version is < 1 or > 2` → `> 3` | 是（与 M1 同批） | 定向 #Core |
| M6 | 件 3（`2eba867` 转义） | `src/Pry.Client/PryBackendClient.cs:188` `?characterId={Escape(characterId)}` → `?characterId={characterId}` | 是（Client 单工程） | 定向 #Client |

**M2 为什么选 `src/Pry.App/Pry.App.csproj`**：`tests/Pry.Api.Tests/Pry.Api.Tests.csproj` 只 `ProjectReference` 了 `src/Pry.Api` 与 `src/Pry.Client`；Pry.App 不在 Api 测试工程的求值图里，把浮动版本写在它上面，**任何跑法都不可能把它送进还原／构建图**（工单 §③ 件 2 的硬要求），而 `No_package_reference_floats` 经 `AllCsprojFiles()`／`ProjectFiles()`（`tests/Pry.Api.Tests/UpstreamProvenanceAndDependencyShapeTests.cs:492-494`）扫的是**全仓 `*.csproj`**，照样读得到。

### 1.4 预算占用计划（硬顶：解决方案级构建 ≤1／全量 ≤1／定向 ≤3）

| 顺序 | 动作 | 记账 |
|---|---|---|
| 1 | Api 定向（M2+M3，`--no-build --no-restore`） | 定向 1／3 |
| 2 | Client 单工程构建 ＋ `dotnet build tests/Pry.Client.Tests -c Release` | **单工程构建，非解决方案级**，另记 |
| 3 | Core 单工程构建 `dotnet build tests/Pry.Core.Tests -c Release`（M1/M4/M5/M6 已写盘） | 单工程构建，另记 |
| 4 | Core 定向（`--no-build`） | 定向 2／3 |
| 5 | Client 定向（`--no-build`） | 定向 3／3 |
| 6 | 还原全部突变 → 解决方案级构建 1 次 | 构建 1／1 |
| 7 | 全量 1 次（含件 1 新断言的复绿、件 2／3 的还原后复绿） | 全量 1／1 |

> 说明：件 2 的"还原后复绿"取自第 7 步全量里的 Api TRX（同一棵树、同一份二进制、`--no-build`）；带突变的 csproj 从未进入任何 restore／build，所以这条复绿是有效的。**定向跑硬顶 3 次已被三组突变用满**，故不再单开一次 Api 复绿跑——这是预算约束下的取舍，照实登记不圆。

---

## 二、逐次焦点预检留档（每一次 `dotnet` 之前都跑一次，整段输出照抄）

### PF-1（定向跑 #1 之前，Api 组 M2+M3）

```
=== GAME PREFLIGHT v4 (foreground rule) ===
  FOREGROUND: QQ pid=28676 style=0x14C70000 rect=1304,228,1069x871 screen=2560x1440 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=3% used=1638MB
free_memory_gb=8.6
VERDICT=BUILD_OK
```

## 三、件 2 批次｜M2＋M3 两向"真改坏真相源"的当场红（定向跑 #1，`--no-build --no-restore`）

| 十一项字段 | M2（浮动版本向） | M3（白名单表一行向） |
|---|---|---|
| ① 载体提交号 | `6b6f5498ea8d32153d4d61de5bca04a6e1f32715` | 同左 |
| ② 被改文件:行 | `src/Pry.App/Pry.App.csproj:11` | `docs/upstream-license-compatibility.md:46`（§1 实测表 HDS-Interlude 行） |
| ③ before→after 原文 | `<PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.20" />` → `<PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.*" />` | 该行第 4 列 `✅ **可以（作者 2026-10-20 点名入白名单；Pry 已同档 AGPL-3.0-only）**` → `❌ **可以（…同一串字未动…）**`（只翻决定符号一个字符，其余列一字未动） |
| ④ `git diff --stat` | `src/Pry.App/Pry.App.csproj \| 2 +-`（1 insertion, 1 deletion） | `docs/upstream-license-compatibility.md \| 2 +-`（`git diff --numstat`＝`1 1`）；两枚合起来 `git diff --stat`＝`3 files changed, 21 insertions(+), 3 deletions(-)`（第三文件是件 1 的判据改动，非突变） |
| ⑤ 跑法整行 | `dotnet test tests/Pry.Api.Tests -c Release --no-build --no-restore --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave7-1022\run1-api-red" --logger trx`（工作目录＝载体根；**带 `--no-restore`，突变量未进入还原／构建图**） | 同一次跑（两枚同批，一轮定向） |
| ⑥ TRX 文件名 | `24782_PHYTHLINK_2026-10-22_17_42_12.trx`（目录：`.local-notes/qa-wave7-1022/run1-api-red/`） | 同左 |
| ⑦ 红格用例全名＋消息要点 | `Pry.Api.Tests.UpstreamProvenanceAndDependencyShapeTests.No_package_reference_floats`——`Assert.Empty() Failure: Collection was not empty`，集合＝`["src/Pry.App/Pry.App.csproj:11 的 Avalonia.Themes.Fl"···]`，锚在 `UpstreamProvenanceAndDependencyShapeTests.cs:193` | `Pry.Api.Tests.UpstreamProvenanceAndDependencyShapeTests.Only_the_authorised_upstreams_are_cited_and_the_two_lists_agree`——`Assert.Equal() Failure: Values differ`，`Expected: 3 / Actual: 2`，锚在 `:153`（**先响的是"名单本身也要判"那条 `Assert.Equal(3, authorised.Count)`**，不是 `:161` 的两表互校；注释里写的"名单被削薄时要立刻响而不是悄悄变成空集合"这一向，实测确实由 :153 兑现） |
| ⑧ 还原方式 | `git -C <载体> checkout -- src/Pry.App/Pry.App.csproj`（只针对本 lane 自己改的这一个路径） | `git -C <载体> checkout -- docs/upstream-license-compatibility.md` |
| ⑨ 还原后 SHA-256 | 目标＝`git show HEAD:src/Pry.App/Pry.App.csproj \| sha256sum`＝`9d94a791e55c16cd93366670cbf73b63b6e6d6260db8c306a302944eadffc979`；突变态＝`f821abd78d3303066bd2557af3714a4fbb4b90510b55915bee44680c62d4631a`；还原后实测见下节 | 目标＝`2e08e0ec71f292582606f419c8a211a0f00dc8ccfd0ec1bb70d975964964e9a5`；突变态＝`05821d8d25dfa724b2c244bafc325611dc7e7602ee36fd35c5aa065ee6763aec`；还原后实测见下节 |
| ⑩ 还原后 `git status --short` 行数 | 见 §3.1 当场数 | 同左 |
| ⑪ 结论 | **浮动版本这一向"真改坏 `.csproj`"拿得到红**：不再是只有测试内嵌样张 | **白名单表一行这一向拿得到红**：真相源被改坏时机械门当场响 |

**TRX 读数（只取 `<Counters>`，并核对去重）**：`<Counters total="278" executed="278" passed="275" failed="3" error="0" notExecuted="0" .../>`；脚本去重后非 `Passed` 条目＝3 条，去重用例名数＝3＝`failed` 计数器 ✓；`NotExecuted`＝0（本批为 Api 单工程，两条环境跳过一条在 Core、一条在 Api——**这一批 Api 里没出现跳过，说明 `LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key` 这一批是"执行并跳过"还是"执行并通过"需要在全量那批按名点名，此处不下结论**）。

### 3.1 多出来的红（第三格）与其归属

定向跑 #1 实到 **3 红**，比预期的 2 红多一格：

- `Pry.Api.Tests.UpstreamProvenanceAndDependencyShapeTests.The_scanners_see_the_shapes_they_claim_to_ban`——`Assert.Empty() Failure: Collection was not empty`，集合＝`[PackageReference { Project = src/Pry.App/Pry.App.csproj, Line = 11, Id = Avalonia.Themes.Fluent, Version = 11.3.* }]`，锚在 `:315`。
- **归属判读（读码，不猜）**：`:313-315` 是"探针要作用在真扫到的数据上"这一段——`var realReferences = PackageReferences(); Assert.True(count >= 20); Assert.Empty(realReferences.Where(IsFloatingVersion))`。它红的内容里只有 M2 那一条浮动版本，**没有**白名单相关的断言先响（`:298` 的 `Assert.Contains("AstrBotDevs/AstrBot", ForbiddenRepos())` 与 `:300` 的 `Assert.NotEmpty(authorised)` 在 M3 下仍成立，因为放行名单是 2 而非 0）。⇒ **这一格是 M2 的第二道守卫（同一真相源、两枚判据），不是 M3 与 M2 互相干扰**。按第六波 §④.4"多出来的红＝组内互相干扰"那条口径，这一例要照实登记为"可解释的同因多响"，**不判为异常，也不需要把该组拆成单枚重跑**（拆成单枚也不会改变 M2 响两格这一事实）。
- 效力上这一格反而**加强**件 2 的结论：浮动版本谓词不只被 `No_package_reference_floats` 用，还被自探针作用在真实 csproj 集合上——上一波"只有测试内嵌样张"的缺口在这一向是双重的。

### 3.2 M2/M3 还原与归零确认

（当场补写于还原之后——见下一小节）

### 3.2 还原与归零确认（实测）

```
$ git -C <载体> checkout -- src/Pry.App/Pry.App.csproj docs/upstream-license-compatibility.md
$ sha256sum src/Pry.App/Pry.App.csproj docs/upstream-license-compatibility.md
9d94a791e55c16cd93366670cbf73b63b6e6d6260db8c306a302944eadffc979 *src/Pry.App/Pry.App.csproj      ← 与 HEAD 逐字节一致
2e08e0ec71f292582606f419c8a211a0f00dc8ccfd0ec1bb70d975964964e9a5 *docs/upstream-license-compatibility.md ← 与 HEAD 逐字节一致
$ git status --short | wc -l
1        ← 唯一剩下的一行是 tests/Pry.Core.Tests/NarrativeStorageContractTests.cs（件 1 的判据改动，交付物本体，不还原）
```

⇒ 件 2 两向的"十一项字段"齐：①～⑪ 全部有实测值，无缺项。**还原后复绿**按 §1.4 的记账取自收尾全量那一批的 Api TRX（同一棵树、`--no-build`、csproj 从未进过还原图）。

### 3.3 `licenses/**` 禁区状态

`git status --short` 全程未出现 `licenses/`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md` 任何一条；本批次只读过 `licenses/THIRD-PARTY-NOTICES.md` 的镜像表内容（由测试自己读，不是我改）。**一字未碰，确认成立。**

### 3.4 本批次预算占用

| 资源 | 占用 | 剩余 |
|---|---|---|
| 解决方案级构建 | 0 | 1 |
| 全量 | 0 | 1 |
| 定向跑 | **1**（`run1-api-red`） | 2 |
| 预检 | 1 次（PF-1，`VERDICT=BUILD_OK`） | — |

### PF-2（Core 单工程构建之前，M1/M4/M5 已写盘）

```
=== GAME PREFLIGHT v4 (foreground rule) ===
  FOREGROUND: ChatGPT pid=32760 style=0x14C70000 rect=87,146,2019x1109 screen=2560x1440 covers=False borderless=False title=ChatGPT
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=8% used=1498MB
free_memory_gb=8.7
VERDICT=BUILD_OK
```

### PF-3（Client 单工程构建之前，M6 已写盘）

```
=== GAME PREFLIGHT v4 (foreground rule) ===
  FOREGROUND: ChatGPT pid=32760 style=0x14C70000 rect=87,146,2019x1109 screen=2560x1440 covers=False borderless=False title=ChatGPT
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=4% used=1486MB
free_memory_gb=8.3
VERDICT=BUILD_OK
```

### PF-4（Core 定向跑之前）

```
=== GAME PREFLIGHT v4 (foreground rule) ===
  FOREGROUND: ChatGPT pid=32760 style=0x14C70000 rect=87,146,2019x1109 screen=2560x1440 covers=False borderless=False title=ChatGPT
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=3% used=1486MB
free_memory_gb=8.3
VERDICT=BUILD_OK
```

### 3.5 预检日志的落位说明（PF-2～PF-7）

`PF-2`／`PF-3`／`PF-4` 三块紧接在本节之后（Core 批次之前），`PF-5` 在 §五 之前、`PF-6`／`PF-7` 在 §六 之前——它们都是**起跑当场追加**的原文，所以按写入时刻散落在各自批次旁边，而不是集中抄在 §二。七块的顺序＝`PF-1`（§二）→ `PF-2` → `PF-3` → `PF-4` → `PF-5` → `PF-6` → `PF-7`，七次全部 `VERDICT=BUILD_OK`。

## 四、Core 批次｜M1（件 1 自证红）＋M4／M5（件 3 两格）同轮定向跑（定向跑 #2）

跑法整行（工作目录＝载体根，预检 PF-4 `VERDICT=BUILD_OK` 之后起跑）：

```
dotnet test tests/Pry.Core.Tests -c Release --no-build \
  --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave7-1022\run2-core-red" --logger trx
```

跑前构建（**单工程、非解决方案级**）：`dotnet build tests/Pry.Core.Tests -c Release --no-restore`（PF-2 之后，`已成功生成／0 警告／0 错误`）
二进制新鲜度自查（防"用 `--no-build` 跑旧二进制"那口坑）：`src/Pry.Client/PryBackendClient.cs` 17:44:23 → `tests/Pry.Client.Tests/bin/Release/net10.0/Pry.Client.dll` 17:45:01；`tests/Pry.Core.Tests/NarrativeStorageContractTests.cs` 17:40:05 → `Pry.Core.Tests.dll` 17:44:53；`Pry.Core.dll` 17:44:52 —— **全部输出晚于源码，突变确实进了编译**。

TRX：`24782_PHYTHLINK_2026-10-22_17_45_24.trx`
`<Counters total="527" executed="525" passed="520" failed="5" error="0" notExecuted="0" .../>` ⇒ 去重失败名数＝5＝`failed` ✓；两条 `NotExecuted` 按名点名＝`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`（**与在册基线那两条同名跳过完全一致，不算红**）。

### 4.1 M1｜件 1 的自证红：**新断言真的会响**

| 十一项字段 | 值 |
|---|---|
| ① 载体提交号 | `6b6f5498ea8d32153d4d61de5bca04a6e1f32715` |
| ② 被改文件:行 | `src/Pry.Core/Narrative/DeliveryLedger.cs:79`（`TryAppend` 里"提交未登记"那道前置门） |
| ③ before→after | `if (!_commits.TryGetValue(key.CommitId, out var commit))` → `if (!_commits.TryGetValue("commit-1", out var commit))`（只换查找键一个表达式，`:81` 的拒绝码与文案一字未动） |
| ④ `git diff --stat` | `src/Pry.Core/Narrative/DeliveryLedger.cs \| 2 +-`（1 insertion＋1 deletion；本批四枚合起来＝`4 files changed, 4 insertions(+), 4 deletions(-)`） |
| ⑤ 跑法整行 | 见本节顶部（`--no-build`，同一轮里还带着 M4／M5） |
| ⑥ TRX 文件名 | `24782_PHYTHLINK_2026-10-22_17_45_24.trx` |
| ⑦ 红格用例全名＋消息要点 | `Pry.Core.Tests.NarrativeStorageContractTests.Late_commit_from_a_lost_ownership_epoch_is_rejected_without_overwriting_anything`——`Assert.False() Failure / Expected: False / Actual: True`，锚在 **`tests/Pry.Core.Tests/NarrativeStorageContractTests.cs:327`**，当场 `awk` 核过该行原文＝`Assert.False(unregistered.Accepted);` ⇒ **响的正是件 1 新写进去的那一条，不是方法里任何一条既有断言**（`:300`／`:301`／`:306-307`／`:310-312` 全部先过了） |
| ⑧ 还原方式 | `git -C <载体> checkout -- src/Pry.Core/Narrative/DeliveryLedger.cs` |
| ⑨ 还原后 SHA-256 | 还原目标（`git show HEAD:... \| sha256sum`）＝`020c485c58de3f5d3def74e78fd90fab4e78fe38672cb747132664f34d1b09a7`；还原后实测见 §6 |
| ⑩ 还原后 `git status --short` 行数 | 见 §6（本批次还原后应为 1 行＝件 1 判据改动本身） |
| ⑪ 结论 | **`DEF-GREENGATE-01` 的 A 路线改判成立且可失败**：原 `:313` 恒真的 `DoesNotContain` 已换成"拿当前代次票走一次 `TryAppend`、断言被拒且原因为 `SegmentCommitUnknown`"，这一向在把 `:79` 未登记提交门放开后立即红。**零产品代码交付**（`:79` 只是载体内临时突变，已还原）；**没有新增公开接口**。 |

**为什么选 `:79` 而不是工单给的另一个形状**（`RegisterAppliedCommit` 的 `:61` 不写）：`:61` 不写会让**所有**提交都变成未登记，新断言（"迟到提交未登记 ⇒ 被拒"）在那枚突变下**照样绿**——它证明不了这一向有门，还会把别格搞浑。`:79` 前置检查放开后，迟到 `CommitId` 能查到一条已登记记录 ⇒ 追加成功 ⇒ 新断言必响。这一枚才是"这一行由谁守"的正解。

**M1 无附带红（实测，不是推测）**：`Segments_cannot_be_appended_for_a_commit_that_never_applied`（`:270`）在本批**没有**红——因为那条用例只 `Acquire("host-a")`、从未提交，`_commits` 是空字典，被换成 `"commit-1"` 的查找键同样查不到 ⇒ 仍返回 `SegmentCommitUnknown` 而绿。这说明 `:270` 与本格（`:327`）不是同一枚门的两个副本：**`:270` 守的是"空账本下的未登记"，本格守的是"已有别的登记时、迟到那一条仍未登记"**——这正是 §③ 想要的"迟到提交不入账"这一格，改判后有了独立证据价值。

### 4.2 M4｜件 3 第一格＝`faf8ecf` 的预算负数一格

| 十一项字段 | 值 |
|---|---|
| ① 载体提交号 | `6b6f5498ea8d32153d4d61de5bca04a6e1f32715` |
| ② 被改文件:行 | `src/Pry.Core/Prompting/ContextBuilder.cs:24` |
| ③ before→after | `public long InputBudget => (long)ContextTokens - OutputTokens - SafetyTokens;` → `public long InputBudget => Math.Max(0L, (long)ContextTokens - OutputTokens - SafetyTokens);`（把"允许为负"改坏成"负数被夹到 0"） |
| ④ `git diff --stat` | `src/Pry.Core/Prompting/ContextBuilder.cs \| 2 +-` |
| ⑤ 跑法整行 | 同 §4 顶部（`--no-build`） |
| ⑥ TRX 文件名 | `24782_PHYTHLINK_2026-10-22_17_45_24.trx` |
| ⑦ 红格用例全名＋消息要点 | `Pry.Core.Tests.ContextBudgetTests.The_raw_equal_output_and_context_window_yields_a_negative_input_budget`——`Assert.Equal() Failure: Values differ / Expected: -128 / Actual: 0`，锚在 `tests/Pry.Core.Tests/ContextBudgetTests.cs:54`（`Assert.Equal(-128, new ContextBudget(32768, 32768).InputBudget);`，即"出厂形状 32768/32768 减 128 安全余量必须是负数"这条原始事实） |
| ⑧ 还原方式 | `git -C <载体> checkout -- src/Pry.Core/Prompting/ContextBuilder.cs` |
| ⑨ 还原后 SHA-256 | 目标＝`c5da1e3e32775d1da5c1844e9d9227d39657aa2796f1301fd65349c9d5bae3ef`；实测见 §6 |
| ⑩ 还原后 `git status --short` 行数 | 见 §6 |
| ⑪ 结论 | **`faf8ecf`（HDS-011 AUD-01）这一格的"改坏原文"补齐**：这一行产品码是"原始预算允许为负、归一化另说"这条形状的唯一守门；第六波只留下红集合、没有 before→after，本格十一项齐。附带红：本批 ContextBudget 系**只有这一格红**（`ForModelWindow_turns...`、两条 Theory 全绿）——它们走 `ForModelWindow`／`TryNormalizeWindow` 的归一化路径，不经 `InputBudget` 的原始减法，属可解释的"同文件不同门"，不是干扰。 |

### 4.3 M5｜件 3 第二格＝`374ecd8` 的双账拒绝一格

| 十一项字段 | 值 |
|---|---|
| ① 载体提交号 | `6b6f5498ea8d32153d4d61de5bca04a6e1f32715` |
| ② 被改文件:行 | `src/Pry.Core/Memory/SqliteMemoryRepository.cs:100` |
| ③ before→after | `if (version is < 1 or > 2) throw new MemoryComponentSchemaRejectedException((int)version);` → `if (version is < 1 or > 3) throw new MemoryComponentSchemaRejectedException((int)version);`（组件账的上限从 2 抬到 3 ⇒ 库内 `memory_schema` 版本 3 不再被拒） |
| ④ `git diff --stat` | `src/Pry.Core/Memory/SqliteMemoryRepository.cs \| 2 +-` |
| ⑤ 跑法整行 | 同 §4 顶部 |
| ⑥ TRX 文件名 | `24782_PHYTHLINK_2026-10-22_17_45_24.trx` |
| ⑦ 红格用例全名＋消息要点 | 目标格＝`Pry.Core.Tests.MemoryDualSchemaLedgerTests.The_two_schema_ledgers_coexist_and_each_high_version_refusal_names_its_own_gate`——`Assert.Throws() Failure: No exception was thrown / Expected: typeof(Pry.Core.Memory.MemoryComponentSchemaRejectedException)`，锚在 `MemoryDualSchemaLedgerTests.cs:48`。**"没有抛出"而不是"抛错了类型"**：说明版本 3 直接放行了，双账里组件那一本的高版拒绝整条消失 |
| ⑧ 还原方式 | `git -C <载体> checkout -- src/Pry.Core/Memory/SqliteMemoryRepository.cs` |
| ⑨ 还原后 SHA-256 | 目标＝`44d1e4ff17f1b22e71a92bd119155ee0af3b00cfe47a218b14827d3efacfcf66`；实测见 §6 |
| ⑩ 还原后 `git status --short` 行数 | 见 §6 |
| ⑪ 结论 | **`374ecd8`（片 B `DEF-MEMDUAL-01`）的"双账拒绝"一格十一项补齐**；同一行还多守两格（见 §4.4），所以这一枚是"一行三格"的承重门，不是装饰。 |

### 4.4 本批多出来的红（2 格）与归属

同因（M5 那一行）多响两格，消息形状与目标格完全一致（`Assert.Throws(): No exception was thrown, Expected MemoryComponentSchemaRejectedException`）：

- `Pry.Core.Tests.MemoryDualSchemaLedgerTests.Both_refusals_travel_the_presentable_chain_and_neither_invites_a_retry`（锚 `MemoryDualSchemaLedgerTests.cs:74`）
- `Pry.Core.Tests.MemoryDeletionSeparationTests.N12_Component_version_two_is_recorded_and_an_unknown_version_refuses_startup`（锚 `MemoryDeletionSeparationTests.cs:280`）

⇒ 两格都读同一道 `SqliteMemoryRepository.cs:100` 的门，属**一枚突变的两处同因红**，不是两枚突变互相污染；按 §① 第 4 条照实登记，不合并、不省略，也**不追认**为 `faf8ecf`／`2eba867` 任何一把的覆盖。M1 与 M4 各只响一格（见 §4.1／§4.2）。

### 4.5 预算占用（本批之后）

| 资源 | 已用 | 剩余 |
|---|---|---|
| 解决方案级构建 | 0 | 1 |
| 全量 | 0 | 1 |
| 定向跑 | **2**（`run1-api-red`、`run2-core-red`） | **1** |
| 单工程构建（另记，非解决方案级） | 2（Core 17:44、Client 17:45） | — |
| 预检 | 4（PF-1～PF-4，全 `VERDICT=BUILD_OK`） | — |

### PF-5（Client 定向跑之前，M6 已编译进 Pry.Client.dll）

```
=== GAME PREFLIGHT v4 (foreground rule) ===
  FOREGROUND: ChatGPT pid=32760 style=0x14C70000 rect=87,146,2019x1109 screen=2560x1440 covers=False borderless=False title=ChatGPT
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=8% used=1480MB
free_memory_gb=8.3
VERDICT=BUILD_OK
```

（`PF-5` 原文见上一块，即 Client 定向跑之前那一次；下面就是 Client 批次。）

## 五、Client 批次｜M6＝件 3 第三格 `2eba867` 的转义一格（定向跑 #3，用满硬顶）

| 十一项字段 | 值 |
|---|---|
| ① 载体提交号 | `6b6f5498ea8d32153d4d61de5bca04a6e1f32715` |
| ② 被改文件:行 | `src/Pry.Client/PryBackendClient.cs:188`（`ConfirmKeepMemoryAsync` 的查询串） |
| ③ before→after | `SendAsync<MemoryDetailsResponse>(HttpMethod.Post, \`$"api/v1/memories/{id}/confirm-keep?characterId={Escape(characterId)}"\`, null, token);` → 同一行把 `{Escape(characterId)}` 改成 `{characterId}`（转义调用摘掉，路径与谓词一字未动） |
| ④ `git diff --stat` | `src/Pry.Client/PryBackendClient.cs \| 2 +-`（1 insertion＋1 deletion） |
| ⑤ 跑法整行 | `dotnet test tests/Pry.Client.Tests -c Release --no-build --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave7-1022\run3-client-red" --logger trx`（前置：PF-5 `VERDICT=BUILD_OK`；`dotnet build tests/Pry.Client.Tests -c Release --no-restore` 于 PF-3 之后，`Pry.Client.dll` 17:45:01 晚于源文件 17:44:23） |
| ⑥ TRX 文件名 | `24782_PHYTHLINK_2026-10-22_17_47_32.trx` |
| ⑦ 红格用例全名＋消息要点 | `Pry.Client.Tests.MemoryManagementClientTests.Confirm_keep_is_an_empty_post_that_escapes_the_character_on_the_query`——`Assert.Equal() Failure: Strings differ`，`Expected: "/api/v1/memories/17/confirm-keep?characterId=a%26b"`／`Actual: "/api/v1/memories/17/confirm-keep?characterId=a&b"`（pos 46），锚在 `tests/Pry.Client.Tests/MemoryManagementClientTests.cs:39`。**样本用的是 `&`（`"a&b"`）而不是空格**，与工单 §③ 件 3／第六波 §③ 第 9 条"空格是无效样本（`Uri` 会替你转成 `%20`）"一致；红格名与第六波预告**逐字相同** |
| ⑧ 还原方式 | `git -C <载体> checkout -- src/Pry.Client/PryBackendClient.cs` |
| ⑨ 还原后 SHA-256 | 目标＝`c448fff2c40cfe4e8085189fe46bbf37e68058f12a0c8a9c06a44d130fca72b6`；实测见 §6 |
| ⑩ 还原后 `git status --short` 行数 | 见 §6 |
| ⑪ 结论 | **`2eba867`（片 F1 转义）这一格由一条没参与实现的 lane 在干净副本上第二次拿到红，且这次带齐 before→after／stat／跑法／TRX／锚点**——第六波那一格只有红名。同批 **无附带红**：`失败 1／通过 107／总计 108`，`PryBackendClientTests.Delete_conversation_asks_for_the_cascade_in_lowercase_on_the_query` 那一格仍绿（它守的是同一文件相邻方法里的另一枚路径／小写形状，与 `:188` 的转义不是同一道门）⇒ 第六波把这两格算作同一把（`2eba867`）的两个红是**两枚不同的守卫**，本格只补其中转义那一枚。 |

### PF-6（收尾解决方案级构建之前，全部突变已还原）

```
=== GAME PREFLIGHT v4 (foreground rule) ===
  FOREGROUND: ChatGPT pid=32760 style=0x14C70000 rect=87,146,2019x1109 screen=2560x1440 covers=False borderless=False title=ChatGPT
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=11% used=1511MB
free_memory_gb=8.3
VERDICT=BUILD_OK
```

### PF-7（收尾全量之前）

```
=== GAME PREFLIGHT v4 (foreground rule) ===
  FOREGROUND: ChatGPT pid=32760 style=0x14C70000 rect=87,146,2019x1109 screen=2560x1440 covers=False borderless=False title=ChatGPT
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532, explorer pid=13364
gpu_util=6% used=1518MB
free_memory_gb=7.6
VERDICT=BUILD_OK
```

## 六、还原与归零确认（六枚突变全记录）

```
$ git -C <载体> checkout -- src/Pry.Core/Narrative/DeliveryLedger.cs src/Pry.Core/Prompting/ContextBuilder.cs \
                            src/Pry.Core/Memory/SqliteMemoryRepository.cs src/Pry.Client/PryBackendClient.cs
$ sha256sum <同上四文件>
020c485c58de3f5d3def74e78fd90fab4e78fe38672cb747132664f34d1b09a7 *src/Pry.Core/Narrative/DeliveryLedger.cs
c5da1e3e32775d1da5c1844e9d9227d39657aa2796f1301fd65349c9d5bae3ef *src/Pry.Core/Prompting/ContextBuilder.cs
44d1e4ff17f1b22e71a92bd119155ee0af3b00cfe47a218b14827d3efacfcf66 *src/Pry.Core/Memory/SqliteMemoryRepository.cs
c448fff2c40cfe4e8085189fe46bbf37e68058f12a0c8a9c06a44d130fca72b6 *src/Pry.Client/PryBackendClient.cs
```

| 突变 | 还原目标 SHA-256（`git show HEAD:<file>`） | 还原后实测 | 一致？ |
|---|---|---|---|
| M1 `DeliveryLedger.cs` | `020c485c…4d1b09a7` | `020c485c…4d1b09a7` | ✓ 逐字节 |
| M2 `src/Pry.App/Pry.App.csproj` | `9d94a791…eadffc979` | `9d94a791…eadffc979` | ✓ 逐字节（§3.2） |
| M3 `docs/upstream-license-compatibility.md` | `2e08e0ec…4964e9a5` | `2e08e0ec…4964e9a5` | ✓ 逐字节（§3.2） |
| M4 `ContextBuilder.cs` | `c5da1e3e…d5bae3ef` | `c5da1e3e…d5bae3ef` | ✓ 逐字节 |
| M5 `SqliteMemoryRepository.cs` | `44d1e4ff…efacfcf66` | `44d1e4ff…efacfcf66` | ✓ 逐字节 |
| M6 `PryBackendClient.cs` | `c448fff2…0fca72b6` | `c448fff2…0fca72b6` | ✓ 逐字节 |

**还原后载体 `git status --short` ＝ 1 行**：` M tests/Pry.Core.Tests/NarrativeStorageContractTests.cs`——唯一剩下的一行是件 1 的判据改动（§④ 明令零提交，所以它以未提交状态留在载体工作树里等守门人取；这不是残留突变）。

## 七、收尾那一轮（唯一一次解决方案级构建＋唯一一次全量）与出口读数

| 步骤 | 命令 | 结果 |
|---|---|---|
| 构建 | `dotnet build Pry.slnx -c Release`（PF-6 `VERDICT=BUILD_OK` 之后） | `已成功生成。0 个警告 0 个错误`，用时 6.71 s |
| 全量 | `dotnet test Pry.slnx -c Release --no-build --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave7-1022\run4-full" --logger trx`（PF-7 `VERDICT=BUILD_OK` 之后） | 四份 `.trx` 齐（当场数过＝4），逐份读 `<Counters>` 见下表 |

| TRX | 程序集（靠 `codeBase` 归属，不用默认跨程序集命名） | `<Counters>` |
|---|---|---|
| `…17_48_29.trx` | `Pry.Client.Tests.dll` | total 108／executed 108／passed 108／failed 0 |
| `…17_48_29[1].trx` | `Pry.Api.Tests.dll` | total 278／executed 278／passed 278／failed 0 |
| `…17_48_29[2].trx` | `Pry.Core.Tests.dll` | total 527／executed 525／passed 525／failed 0；`NotExecuted` 按名点名＝`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key` |
| `…17_48_32.trx` | `Pry.App.Tests.dll` | total 233／executed 233／passed 233／failed 0 |
| **合计** | 四工程 | **总 1146＝executed/passed 1144＋0 红＋2 条同名环境跳过** |

⇒ **出口读数与在册基线逐字相同（1146／0／2），且这一次跑的是带件 1 改判的那棵树**——所以：
1. 件 1 的新断言在**未突变**的码上是绿的（`Assert.Empty` 与两条 `SegmentCommitUnknown` 都过），即"改判把恒真换成了可失败但没有把门改严到误报"；这是 §4.1 那一枚红的**另一半证据**（红过 ⇒ 不是空转；绿着 ⇒ 没有假警）。
2. 判据条数等式：**新判据 0 条、删除判据 0 条**（件 1 属同一方法内替换断言，未增删 `[Fact]`／`[Theory]`；件 2／件 3 只动 `src/` 与文档且已全部还原）⇒ 1146 这一份数不变，本 lane 不改写任何在册基线（§④）。

## 八、预算终账（硬顶对硬账）

| 资源 | 硬顶 | 已用 | 明细 |
|---|---|---|---|
| 解决方案级构建 | ≤1 | **1** | `dotnet build Pry.slnx -c Release`（收尾那一轮） |
| 全量 | ≤1 | **1** | `run4-full`（四份 TRX） |
| 定向跑 | ≤3 | **3** | `run1-api-red`（M2+M3）、`run2-core-red`（M1+M4+M5）、`run3-client-red`（M6） |
| 单工程构建（工单未设上限，另立一列如实记） | — | 3 | `dotnet build tests/Pry.Core.Tests -c Release --no-restore`（PF-2 后，4.51 s）；`dotnet build tests/Pry.Client.Tests -c Release --no-restore`（PF-3 后，0.87 s）；外加收尾的解决方案级 1 次（不重复计） |
| 焦点预检 | 每次 `dotnet` 前一次 | **7** | PF-1～PF-7，整段输出见 §二／§四／§五／§六／§七，**七次全部 `VERDICT=BUILD_OK`**，没有一次 `HOLD`；焦点窗全程＝`QQ pid=28676 … covers=False borderless=False`；`free_memory_gb` 8.6→（各次见原文）；无 SQLite `disk I/O`／`out of memory` 形状出现过 ⇒ 没有任何一批读数作废 |

**没有超支**：定向跑到 3 就停了；件 3 只做点名的三格，其余 43 格没碰。

## 九、缺口与偏差（照实登记，不许圆）

1. **件 2 的"还原后复绿"没有单独开一次 `--no-build --no-restore` 定向跑**。原因＝定向跑硬顶 3 次已被 M2/M3（1）、M1/M4/M5（1）、M6（1）用满。替代证据＝§七 全量里的 `Pry.Api.Tests.dll` TRX（total 278／passed 278／failed 0）：跑它时 csproj 与许可文档已逐字节还原（§六），且带突变的 `src/Pry.App/Pry.App.csproj` **从头到尾没有进过任何 restore／build**（M2 那一批用的是 `--no-build --no-restore`，收尾构建时 csproj 已还原）。⇒ 与 §③"同样用 `--no-build --no-restore` 复绿"的字面要求有一格偏差，缺的是"同一条命令行原样再跑一次"，不缺"还原后 Api 全绿"这一事实。守门人若要字面合规，只需补一次定向跑（本 lane 无权超顶）。
2. **件 1 未采纳 B 分支 ⇒ `docs/narrative-storage-contract.md` 一字未改**（B 才要求更正文档句子）。§③ 件 1 明令"不许两个都做"，所以这一处不是缺口而是选择的结果；A 分支下该文档 `:278` 那句"账本里没有迟到 commit 的段"仍然为真，且现在有 `:327`／`:331` 两条可失败的断言在守它。
3. **件 3 只补了三格**：`faf8ecf` 只补"预算负数"那一格（ContextBudgetTests 共 8 格红、AttachmentLimitTests 4 格、ContextWindowLoadTests 3 格 ⇒ 同一把其余 14 格仍挂账）；`374ecd8` 只补"双账拒绝"那一格（该把共 4 格 ⇒ 另 3 格挂账）；`2eba867` 只补转义那一格（该把共 2 格 ⇒ `PryBackendClientTests.Delete_conversation_asks_for_the_cascade_in_lowercase_on_the_query` 挂账）。第六波那 46 格剩下的 **43 格继续挂账**，本波未"顺手多拆"。
4. **一处行号记账误差（不影响结论）**：工单 §③ 件 1 写 `DeliveryLedger.Snapshot()` 在 `:45`，载体实测在 `:46`。其余 `:36`／`:61`／`:79`／`:196`／`:230`／测试 `:281`／`:313` 全部与工单逐字一致。
5. **两条同名跳过的程序集归属（本 lane 当场按 `codeBase` 核过）**：`CoreTests.Configured_sense_voice_model_can_run_offline` 与 `LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key` **都在 `Pry.Core.Tests.dll` 名下**（`notExecuted` 计数器恒 0，只能按名点名 ⇒ 与 §0.2 口径一致）。推论：§三 那批 Api 定向跑 `total=278／executed=278／NotExecuted=0` 是**自洽的**，不是"跳过被吞掉"，Api 那 278 条本来全是执行项。
6. **载体里 `tests/Pry.Core.Tests/NarrativeStorageContractTests.cs` 处于未提交修改态**（件 1 交付物本体）。按 §④ 本 lane 不 `git add`、不提交 ⇒ 它的落地权在守门人；如果守门人不要这一改，`git -C <载体> checkout -- tests/Pry.Core.Tests/NarrativeStorageContractTests.cs` 即回到 `6b6f549` 原样（还原 SHA-256 目标＝原 `:313` 那一行，见 §1.2）。

## 十、三件活逐件结论（不写状态词、不写结案词）

| 件 | 结论 |
|---|---|
| **件 1｜`DEF-GREENGATE-01`** | 三段读码事实复核成立（工单行号仅 `:45`→`:46` 一格之差）；改判选 **A** 并落进载体工作树；自证红**拿到且锚在新断言行**（`NarrativeStorageContractTests.cs:327`，`Assert.False(unregistered.Accepted)` 由 False→True）；同一条改判在未突变码上绿（全量 Core 525 passed）。零产品代码交付、零公开接口新增、承重断言 `:300/:301/:306-307/:310-312` 一字未放宽。 |
| **件 2｜`e554483` 两向** | 两向都**不需要还原图变化**即拿到红，且是**真改坏真相源**：M2＝真实 csproj 的浮动版本（红在 `No_package_reference_floats`，`:193`；并多响自探针格 `The_scanners_see_the_shapes_they_claim_to_ban`，`:315`）；M3＝许可白名单表一行翻决定符号（红在 `Only_the_authorised_upstreams_are_cited_and_the_two_lists_agree`，`:153`"名单本身也要判"）。两枚均逐字节还原、`licenses/**`／`ASSETS_LICENSE.md`／`FAN_CONTENT_POLICY.md` 一字未碰。上一波"只有测试内嵌样张、没有真改坏真相源"这一格欠账，本波有盘上原文可引。 |
| **件 3｜46 格中的最小三格** | `faf8ecf`（预算负数，M4→`ContextBudgetTests.cs:54`，Expected -128／Actual 0）、`374ecd8`（双账拒绝，M5→`MemoryDualSchemaLedgerTests.cs:48`，No exception thrown）、`2eba867`（转义，M6→`MemoryManagementClientTests.cs:39`，`a%26b` vs `a&b`）三格**十一项字段齐全**；附带红 M5×2（同因两格）另记，M1／M4／M6 无附带红；其余 43 格继续挂账。 |

**盘上证据目录**（全部在载体之外、不受还原影响，且未被覆盖过——每批一个新名）：

- `C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave7-1022\run1-api-red\24782_PHYTHLINK_2026-10-22_17_42_12.trx`
- `C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave7-1022\run2-core-red\24782_PHYTHLINK_2026-10-22_17_45_24.trx`
- `C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave7-1022\run3-client-red\24782_PHYTHLINK_2026-10-22_17_47_32.trx`
- `C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave7-1022\run4-full\`（四份：`…17_48_29.trx`、`…17_48_29[1].trx`、`…17_48_29[2].trx`、`…17_48_32.trx`）
- 载体：`C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-tip-1022`（detached `6b6f549`，`git status --short`＝1 行＝件 1 判据改动）

**本 lane 没有做过的事**：没有 `git add`、没有 commit／tag／push／打包、没有改任何限额数值／`.5` 检索地板／`2400`／五档超时／`MinimumUsableTokensPerSecond = 12`／`4096` 地板、没有放宽或删除任何既有断言（件 1 是把恒真断言替换成两条可失败断言并留下原承重断言全部在位）、没有碰主工作树他人名下那两枚暂存项、没有动作者任何进程、没有把 `%LOCALAPPDATA%/PryCompanion` 当测试载体、没有改预检脚本或预检判据。
