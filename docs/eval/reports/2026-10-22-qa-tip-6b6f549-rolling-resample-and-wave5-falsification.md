# eval-QA-TIP1022-1022｜线尾 `6b6f549` 独立滚动复采＋第五波三枚内容提交证伪（2026-10-22）

- 报告 id：`eval-QA-TIP1022-1022`
- 角色：冷启动 QA／守门人复采 Agent（非第五波实现方、非上一轮读数者）
- 工单依据：`docs/dispatch-hds-wave5-20261022.md`（全局约束第 3、3a、9 条口径逐条执行）
- 来路：对线尾 `6b6f549` 做**独立滚动复采**——在干净 detached 载体上复现门禁读数，并对第五波三枚内容提交（`2eba867`／`e31e4da`／`dc5b010`）做当场证伪。主工作树零改动、零提交；本轮产物只有本报告与 `.local-notes/qa-tip-1022*` 下的运行目录。

---

## ① 载体与入场

- 载体：`git worktree add --detach .local-notes/qa-tip-1022 6b6f549`（自 `C:\Users\24782\Desktop\Pry_Project` 起建）。
- `git -C .local-notes/qa-tip-1022 rev-parse HEAD` ＝ **`6b6f5498ea8d32153d4d61de5bca04a6e1f32715`**，detached，与工作树线尾同枚。
- 入场干净证明：建载体后与每一步还原后 `git -C .local-notes/qa-tip-1022 status --short` 均输出 **0 行**。
- 主工作树入场读数（规则 1）：分支 `codex/ui-main-window-redesign`，他人名下两枚已暂存文件（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）与全部未跟踪文件（`docs/research/*.md`、`docs/character-learning-safety.patch`、`.qoder/`）全程未 stage、未回退、未触碰；退场时复核形状与入场逐字相同。
- 模型／运行库／DB／素材不在 git 内，载体合法缺如——这正是 ② 里两条环境跳过的成因。
- 焦点预检（第 3a 条，每次可能弹防火墙的动作前各跑一次 `game-preflight4-1022.ps1`，全部原样采证）：
  - 全量轮之前：`FOREGROUND: steamwebhelper … covers=False borderless=False`、`gpu_util=20%`、`free_memory_gb=8.2`、**`VERDICT=BUILD_OK`**；
  - M1 前：`FOREGROUND: PickerHost …`、`free_memory_gb=7.3`、`BUILD_OK`；M2 前：`7.2 GB`、`BUILD_OK`；M3 前：`7.1 GB`、`BUILD_OK`。四次无一 HOLD。
- 内存（`Get-CimInstance Win32_OperatingSystem` → FreePhysicalMemory/1MB）：
  - 构建前 **8.32 GB**（15:31 前后）；
  - 全量运行中三点采样：**5.63 GB（15:32:56）／6.50 GB（15:33:17）／7.27 GB（15:33:38）**；
  - 全量＋两把 PowerShell 仪器跑完后 **7.28 GB**（15:34:46）。

## ② 构建与一次全量读数（防火墙预算＝解决方案构建 1、全量 1，实际用量 1 和 1）

一次 `dotnet build Pry.slnx -c Release`（载体内）：**0 警告 0 错误**，产出 **9 枚程序集**（Contracts／Core／Client／Api／App＋四枚测试项目，逐行 `-> *.dll` 数到 9，等式成立，读数不作废）。

一次 `dotnet test Pry.slnx -c Release --no-build --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-tip-1022-run1" --logger trx`：exit=0。**先数目录**：`.local-notes/qa-tip-1022-run1` 内 `.trx` 恰 **4** 份，随后逐份读 `<Counters>`（`notExecuted` 计数器照旧恒 0，跳过只按 `outcome="NotExecuted"` 点名）：

| TRX 文件 | `<Counters>` 原文（摘要） |
|---|---|
| `..._15_32_54.trx`（Client.Tests） | total=108 executed=108 passed=108 failed=0 |
| `..._15_32_54[1].trx`（Api.Tests） | total=278 executed=278 passed=278 failed=0 |
| `..._15_32_54[2].trx`（Core.Tests） | total=527 executed=525 passed=525 failed=0 |
| `..._15_32_56.trx`（App.Tests） | total=233 executed=233 passed=233 failed=0 |

- 等式：**总 1146 ＝ passed 1144 ＋ failed 0 ＋ 2 条环境跳过**；分项 **Client 108／App 233／Api 278／Core 527（executed 525）**——与守门人第五波归位登记的事前等式**逐位相同，一位不差**。
- 两条跳过按 `<UnitTestResult outcome="NotExecuted">` 点名：`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`、`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`——与在册同名同枚。
- 资源形状判定：运行中采到一次 **5.63 GB（<6 GB，触发 WARN 行）**；整份全量日志 grep `disk I/O|out of memory|database is locked` ＝ **0 命中**，无 SQLite 形状。按工单第 3a／9 条（WARN 本身不作废，作废条件是内存形状成真）与同日 §9 先例（5.5–5.8 GB 同形状判可用），**本轮读数判为可用**；附一条明账：本次派单文字把作废条件写成了"任一刻 <6 GB 即作废"，该字面口径若成立，守门人自己那轮 5.5–5.8 GB 的在册读数也同被判废——两个口径的分歧登记在此，请守门人裁决，本轮按仓库成文条文（工单原文）执行。

## ③ `measure-fixed-eval` 与 `verify-repository`

- `scripts/measure-fixed-eval.ps1 -ResultsDirectory "...\.local-notes\qa-tip-1022-run1"`：`Catalog valid: **76** permanent scenarios`；`Passed=1144; Failed=0; Other=2`；exit=0；`fixed-eval-coverage.json` 落在本轮专属目录（未复用他人 run 目录）。
- `scripts/verify-repository.ps1`（载体内跑）：`Repository verification passed for **541** tracked files.`；exit=0（条数是观察值，不是门槛）。

## ④ 独立证伪（定向跑预算上限 3 次，实际用量 3 次＝M1 一次、M2 一次、M3 空转一次）

### M1（`dc5b010`，行为级）——拿到红证 ✔

- 落点核实：修复形状在 `src/Pry.Api/Program.cs:153-157`——`writeLease.Acquire(...)` 现排在 `MemoryDatabase.InitializeAsync`（`:158`）与 `RecoverStalePlansAsync`（`:160`）之前，失败路径 `await app.DisposeAsync()` 后 rethrow（`:170`）。
- 突变：把 `Acquire` 一句从 try 首部移到 `RecoverStalePlansAsync` 与日志之后（≈缺陷原形状"两次写盘跑在取锁之前"）。载体内单点改动，单工程构建 `tests/Pry.Api.Tests`（0 警告 0 错误）。
- 定向跑 #1：`--filter FullyQualifiedName~RuntimeDescriptorWriteGateTests`（整类 5 格，`--results-directory ...qa-tip-1022-m1`）＝**失败 1／通过 4／总计 5**，红的正是点名格：
  - 用例：`Pry.Api.Tests.RuntimeDescriptorWriteGateTests.A_refused_second_host_leaves_the_first_instances_open_plan_and_pending_message_untouched`
  - 原文：`Assert.Equal() Failure: Strings differ / Expected: "1|open" / Actual: "1|cancelled"`，栈顶在 `RuntimeDescriptorWriteGateTests.cs:line 173`——与修复前红灯逐字同形。
  - 其余四格（两格 `ThrowsAsync` 形状、两格正常路径）在突变下照绿，证明红的不是邻格。
- 还原：`git checkout -- src/Pry.Api/Program.cs`（仅本 Agent 自己在载体内的改动）⇒ `git status --short` **0 行**。

### M2（`e31e4da`，行为级）——拿到红证 ✔

- 落点核实：比较语义在 `src/Pry.Api/Services/ModelPerformancePolicy.cs:71-72`＝`profile.ContextSize > 4096 && metrics.TokensPerSecond is > 0 and < MinimumUsableTokensPerSecond`（常量 `:9` 的 12 一字未动）。**派单里给的 `src/Pry.Core/Inference/...` 路径有误**，工单预检一节写的 Api/Services 才是真相源——已按当场读码修正，不构成新范围。
- 突变：摘掉 `> 0` 那一半（`is > 0 and < 12` → `is < 12`），即"0 token/s 也会被判太慢"。
- 定向跑 #2：`--filter FullyQualifiedName~ModelPerformancePolicyBoundaryTests`（整类 3 格，目录 `...-m2`）＝**失败 1／通过 2／总计 3**：
  - 用例：`Pry.Api.Tests.ModelPerformancePolicyBoundaryTests.The_usable_speed_floor_flags_only_the_open_band_below_twelve`
  - 原文：`Assert.False() Failure / Expected: False / Actual: True`，栈顶 `ModelPerformancePolicyBoundaryTests.cs:line 27`——正是 `IsTooSlow(wideWindow, 0)` 该被 `> 0` 挡在门外的那格。地板格与第二把闸白名单格照绿。
- 还原：`git checkout -- src/Pry.Api/Services/ModelPerformancePolicy.cs` ⇒ `git status --short` **0 行**。

### M3（`2eba867`，客户端路由）——本轮无非空转证据 ✘（成因＝操作者程序错误，不是该点够不着）

- 落点核实（读码完成）：两条新钉路由在产品码里真实存在——`src/Pry.Client/PryBackendClient.cs:188`（`confirm-keep?characterId={Escape(characterId)}`）与 `:111-112`（`conversations/{Escape(id)}?forgetExclusiveMemories={...ToLowerInvariant()}`），`Escape`＝`Uri.EscapeDataString`（`:458`）；在册判据为 `MemoryManagementClientTests.Confirm_keep_is_an_empty_post_that_escapes_the_character_on_the_query`（断言 `a%26b` 样张，不是空格——遵守既有教训）与 `PryBackendClientTests.Delete_conversation_asks_for_the_cascade_in_lowercase_on_the_query`。
- **事故如实登记**：执行 M3 时我把"预检→改码→构建→定向跑"的顺序做成了"预检→构建→定向跑"，突变**没有落盘**就跑掉了第三次（也是最后一次）定向预算——该次读数 2/2 全绿是对**未改码**的合法树跑的，作为反证＝**空转**，只消耗了预算、不构成任何证据。
- 预算边界：派单明写"至多 3 次定向跑"，我没有授权起第 4 次，因此**不**在当场补做突变。宁可登记缺口，不谎称拆过。
- 待授权的一步（留给守门人）：突变点一行现成＝`:188` 的 `{Escape(characterId)}`→`{characterId}`，预期红在全名 `Pry.Client.Tests.MemoryManagementClientTests.Confirm_keep_is_an_empty_post_that_escapes_the_character_on_the_query` 的 `Assert.Equal(handler.Uri)`（`MemoryManagementClientTests.cs:39`），红样＝`a&b` 对 `a%26b`；单工程构建＋一次定向跑＋逐字节还原即可，载体与目录（`...-m3`）仍在原位。
- 载体最终态复核：`PryBackendClient.cs:188` 仍是原文（从未被改），`git -C .local-notes/qa-tip-1022 status --short` ＝ **0 行**。

## ⑤ 与前人读数的差异

- 门禁读数：与守门人第五波归位登记（总 1146／1144 绿／0 红／2 同名跳过，分项 108／233／278／527-executed 525）**零差异**，逐位复现。catalog 76、`verify-repository` 541 亦与在册一致。
- 证伪面：第五波在册只采证了实现方自报的突变；本轮独立补到的增量＝M1、M2 两枚**由非实现方在干净 detached 载体上**当场拿红的行为级反证；差异项＝M3 本轮空转（上一行④），在册的 `2eba867` 提交信息自述做过四枚单点突变，但那是实现方历史声明，**本轮未独立复证**，不作本轮证据。
- 仪器差异：派单文字两处勘误已在当场读码纠正——M2 的 `ModelPerformancePolicy` 住在 `src/Pry.Api/Services/`（非 `src/Pry.Core/Inference/`）；内存作废条件按工单原文（3a＋9 条）判，与派单字面口径的分歧已登记在②。

## ⑥ 这一轮**没有**覆盖到什么（诚实清单）

- **M3（`2eba867`）本轮未拆**——空转一次、预算耗尽；该提交在册全绿的唯一独立凭据仍只有第五波实现方自述（未经本轮复证）。
- 本轮只拆了第五波三枚内容提交中的两枚；**其余八把刀**（第三波片 A 的 `AssertWriteAllowed` 那一向——在册已证"够不着"、第四波 E1 的浮动版本与许可表覆盖两条真突变、其余各波守卫）**本轮未拆**。其中浮动版本与许可表两条在第五波载体已由实现方真突变过，本轮同样**未独立复证**——把没拆的写成没拆。
- `ModelProcessRegistry.cs:63` 第二把闸的另一半（`attemptedProfile.ContextSize > 4096`）：无产品接缝，本轮依旧无行为判据（与 F3 在册注释一致，非新账）。
- `DocxProcessingTimeoutMs`（15 秒档）、U-3 代次持久化、真·依赖锁定：不在本轮范围，未触碰。
- 他人名下 `QA-TIP1020-02`（索引里 63/0 暂存项）：不属于本轮任何读数，一字未碰。
- 两条环境跳过（llama 子进程环境键、sense-voice 离线模型）：载体缺模型／运行库属合法缺如，本轮未造任何替代覆盖。

## ⑦ 结论

- **复采读数＝成立**：干净 detached 载体、构建 1／全量 1、四份 TRX 逐份读、等式 1146＝1144＋0＋2 逐位复现、两条跳过同名、catalog 76、跟踪面 541。运行中一次 5.63 GB 的 WARN 采样已如实登记，按工单第 3a／9 条与同日先例判为可用；该判法与派单字面口径的分歧留给守门人裁决。
- **证伪面＝三缺壹**：M1、M2 当场拿到与在册缺陷形状逐字同形的红证并逐字节还原（载体全程 0 行）；M3 因本 Agent 顺序错误空转，本轮无非空转证据，欠一次守门人授权的定向补跑（步骤与预期红已写在④，随时可执行）。
- **判定＝可签（附一条明账）**：第五波出口读数在独立载体上可复现，两枚核心行为提交（`e31e4da`、`dc5b010`）已由非实现方独立证伪；明账＝`2eba867` 的独立反证仍欠，签的效力不覆盖那一枚路由守卫的历史声明。
- **在册基线改写权**：本轮读数系当场数到且分项与第五波出口等式逐位相同，按仓库纪律具备把在册基线从 **988／0／2 改写为 1146／0／2（分项 108／233／278／527-executed 525，两条同名跳过）** 的资格；本 Agent 不改 `docs/plans/PROJECT_PLAN.md`（登记刀归守门人），此报告即改写给据。
- 载体 `.local-notes/qa-tip-1022` 原地保留（守门人查验用），最终态 `git status --short` ＝ 0 行；主工作树除本报告外零改动、零提交。

---

## ⑧ 守门人复核与补跑（2026-10-22，本报告归位刀同轮）

- **四份 TRX 由守门人独立重读一遍，与②逐位相同**：Client `total=108 executed=108 passed=108 failed=0`、Api `278/278/278/0`、Core `total=527 executed=525 passed=525 failed=0`、App `233/233/233/0` ⇒ **SUM 总 1146／passed 1144／failed 0**；两条 `outcome="NotExecuted"` 名字与②一致（`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`）。
- **仪器自抓一次（同族教训第四次）**：守门人第一版逐份读数用 `/<Counters[^>]*\/>/` 匹配，在 CRLF 的 TRX 上四个文件全部读出 `total=0`，差点把"这份报告的数不存在"当结论写进台账；改成先 `indexOf('<Counters')` 再 dump 原文才拿到上面那份数。⇒ **读数仪器的"零"必须先证明不是自己解析失败**，与本项目"四程序集零测试仍报 exit=0"那条同源。
- **内存作废口径裁决（②与⑥那处分歧）**：按工单第 3a＋9 条原文执行——作废条件是资源**形状**出现（SQLite `disk I/O`／`out of memory`／断言与资源无关的崩），一次 `5.63 GB` 的低采样只记 `WARN`。本轮日志零该形状、四条分项逐位对上事前等式 ⇒ **读数判为可用**。派单文字里"任一次低于 6 GB 即作废"那句是我写得比工单更严，**以工单为准**，该分歧就此闭合，不再回头作废这一轮。
- **M3 补跑（守门人授权＝定向跑预算第 4 次，超出派单上限一次；理由＝缺口明确、步骤现成、只起一个工程与一次定向测试）**：
  - 突变：载体 `src/Pry.Client/PryBackendClient.cs:188` 的 `?characterId={Escape(characterId)}` → `{characterId}`，`git -C .local-notes/qa-tip-1022 diff --stat` ＝ `1 file changed, 1 insertion(+), 1 deletion(-)`，全载体只此一行。
  - 结果：`dotnet test tests/Pry.Client.Tests/Pry.Client.Tests.csproj -c Release --results-directory ".local-notes\qa-tip-1022-m3" --logger trx` ⇒ **失败 1／通过 107／总计 108**，红点恰在预告那一格：`Pry.Client.Tests.MemoryManagementClientTests.Confirm_keep_is_an_empty_post_that_escapes_the_character_on_the_query`，`Assert.Equal() Failure: Strings differ (pos 46) Expected: "…characterId=a%26b" Actual: "…characterId=a&b"`，`MemoryManagementClientTests.cs:39`。同一次跑里其余 107 格全绿 ⇒ 这一枚转义只有这一格在守（＝④预告的形状，实测复现）。
  - 还原：只对我自己那一行用 `git checkout -- src/Pry.Client/PryBackendClient.cs`，SHA-256 与突变前的字节备份逐字节相同（`c448fff2…`），载体 `git status --short` ＝ **0 行**；随后重建 `Pry.Client` 工程（0 警告 0 错误），免得载体里留着被改坏的 `Pry.Client.dll` 骗到下一次 `--no-build`。
- **效力标注（防误读）**：M1、M2 的红由**非实现方**（本 QA Agent）在干净载体当场拿到；M3 的红由**守门人本人**补拿。按仓库那条"同角色不签非空转"的纪律，M3 这一枚记作**守门人自证**而不是"独立复证"，它的效力低于 M1／M2，下一轮 QA 若要签 `2eba867` 仍需自己拆一遍。三枚内容提交自此**都有当场红证**（三缺壹→三缺零），欠的是其中一枚的换人复判。
- **在册基线改写（由守门人执行，凭据＝本报告②＋上面这份逐位复读）**：`988／0／2` → **`1146／0／2`**，分项＝Client 108／App 233／Api 278／Core 527（executed 525 passed 525），两条同名环境跳过。改写只在**本轮覆盖的线尾 `6b6f549`** 上成立；`docs/plans/PROJECT_PLAN.md` §4.2、§9 与作者页 §0／§3 已同步。不 tag／不 push／不打包；状态词与结案词仍归作者。
