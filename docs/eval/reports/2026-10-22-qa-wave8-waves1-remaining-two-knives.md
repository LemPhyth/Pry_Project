# eval-QA-WAVE8-1022｜第八波两把刀的"拆法原文"增量报告（零产品代码、零新判据、零门槛改动）

- 报告 id：`eval-QA-WAVE8-1022`
- 执行方：守门人自起的冷启动执行 lane（代号 `wave8-lane`），无本轮对话上下文
- 工单：`docs/dispatch-hds-wave8-20261022.md`（§① 四条交付纪律／§② 载体／§③ 两把与判据面／§④ 跑法／§⑤ 起止读数／§⑥ 预算与弹窗／§⑦ 硬约束／§⑧ 交付物）
- **本报告是交付物；读数不是。** 按批次增量落盘：每跑完一批立刻把该批表格追加进本文件再开下一批。
- **状态词与结案词不由执行方写**：本报告只写事实与读数，不写"已结案／已验收／基线已改写"。
- **本轮由 `wave8b-lane` 续跑（2026-10-22 深夜），上一轮的 §〇～§十 保持原文一字不改**；续跑内容自下文 `§三补` 起。批次表与突变配方逐字复用上一轮 §0.5／§0.6／§3.2，仅把工单 §⑥／报告 §0.7 的焦点预检脚本由 `game-preflight4-1022.ps1` 换成 `game-preflight5-1022.ps1`（工单 §9.5 仪器更正：焦点落在桌面 shell 不再算 HOLD），其余纪律（每一次 `dotnet` 前必跑、整段输出贴进报告、`HOLD` 即停不绕行）一字不变。

---

## 〇、载体、入场读数、计划突变清单、预算声明、收尾预案

### 0.1 载体与入场读数（三次取时点，当场实测，不引用记忆）

| 项 | 实测值 | 取法 |
|---|---|---|
| 主工作树分支 | `## codex/ui-main-window-redesign` | `git -C C:\Users\24782\Desktop\Pry_Project status --short --branch` |
| 主工作树未提交项（旁观记录，一律不读改不提交） | 7 行：他人名下 2 枚暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）＋5 项未跟踪（`.qoder/`、`docs/character-learning-safety.patch`、`docs/research/HDS_INTERLUDE.md`、`docs/research/HDS_QQ_RESEARCH_INDEX.md`、`docs/research/QQ_BRIDGE.md`） | 同上 |
| 载体路径 | `C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-tip-1022` | 工单 §② |
| 载体 HEAD | `6b6f549`（完整 `6b6f5498ea8d32153d4d61de5bca04a6e1f32715`，detached） | `git -C <载体> rev-parse --short HEAD` |
| 载体 HEAD 提交主题 | `docs(plans): 第五波五片归位登记（F1 2eba867／F3 e31e4da／F5 dc5b010，出口 1146＝1144 绿＋2 条同名跳过）＋工单闭合＋本角色那次读错树的计数错账` | `git log -1 --format='%H %d %s'` |
| **载体入场 `git status --short` 行数** | **0 行**（满足工单 §②／开工前置，无需换目录） | `git -C <载体> status --short \| wc -l` |
| **载体入场 `git diff --stat`** | **空输出（0 文件）** | `git -C <载体> diff --stat` |
| 二进制新鲜度（现成，第七波收尾那一轮建的） | `src/Pry.Api/bin/Release/net10.0/Pry.Api.dll` ＝ 10-22 17:04；`tests/Pry.Core.Tests/bin/Release/net10.0/Pry.Core.Tests.dll` ＝ 10-22 17:44 | `ls -l` |

⇒ 入场条件（0 行且 HEAD＝`6b6f549`）成立，未做任何清理动作。

### 0.2 载体与线尾的那一格已知差异（照实登记，不追、不重证、不替它补）

主线现在比这棵载体多出 `tests/Pry.Core.Tests/NarrativeStorageContractTests.cs` 的 `+19/−1`（第七波件 1 归位＝`4039a50`，恒真断言已换成可失败断言）。本波两把刀的判据都不在那个文件里 ⇒ 按工单 §② 只登记不处理。**载体内该文件是 `6b6f549` 的原样**，所以载体跑出的 Core 条数仍应是 527／executed 525（第七波那条改判属"同一方法内替换断言"，不新增 `[Fact]`，两棵树条数相同）。

### 0.3 起点读数（工单 §⑤，在册基线）

| 程序集 | 读数 |
|---|---|
| Client | 108 |
| App | 233 |
| Api | 278 |
| Core | total 527／executed 525／passed 525 |
| **总盘** | **1146 ＝ passed 1144 ＋ 0 红 ＋ 2 条环境跳过** |

- 两条跳过按名点名＝`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`（**都在 `Pry.Core.Tests.dll` 名下**；`notExecuted` 计数器恒 0，只能按 `<UnitTestResult outcome="NotExecuted">` 点名）。
- catalog **76**；受跟踪文件数当前 **547**（本波在主线只落这一份报告 ⇒ 收尾顶账应为 **548**，属文档条数不是门槛）。
- 本波**零新判据、零删除判据** ⇒ 收尾复证必须还是上面这组数。

### 0.4 本波两把的格子归属（**当场从盘上第六波原始 TRX 取的点名，不是转述**）

工单 §③ 的归属来自第六波复采报告。为免把"哪 8 格"记成感觉，本 lane 当场重数了 `%TEMP%\qa-wave6-1022\` 里那两份 TRX（守门人复采报告 §① 证实这批目录仍在盘上）：

- `%TEMP%\qa-wave6-1022\core-red\24782_PHYTHLINK_2026-10-22_16_37_49.trx`（`<Counters total="527" executed="525" passed="501" failed="24">`）里 `faf8ecf` 名下 12 格逐一点名：
  - `ContextBudgetTests`（8 格）＝`A_real_default_model_window_survives_assembly_with_positive_budget`、`Advisory_is_counted_into_used_and_reaches_the_model_verbatim`、`Advisory_reservation_is_protected_while_optional_sections_drop`、`An_advisory_beyond_its_reserved_share_fails_loudly_and_is_ledged`、`Assembly_of_a_non_positive_window_fails_with_a_distinguishable_budget_error`、`ForModelWindow_turns_an_equal_output_and_context_into_a_positive_input_budget`、`TryNormalizeWindow_accepts_real_model_windows(context: 32768, output: 32768)`、`TryNormalizeWindow_accepts_real_model_windows(context: 4096, output: 4096)`
  - `AttachmentLimitTests`（4 格）＝`A_high_compression_ratio_is_rejected`、`An_excessive_node_count_is_rejected`、`An_over_deeply_nested_document_is_rejected_before_full_parse`、`An_overlong_attribute_name_is_rejected`
- `%TEMP%\qa-wave6-1022\api-red\24782_PHYTHLINK_2026-10-22_17_03_14.trx`（`<Counters total="278" executed="278" passed="262" failed="16">`）里：
  - `faf8ecf`／`ContextWindowLoadTests`（3 格，该文件的全部三个方法）＝`Loading_the_default_profile_at_full_context_yields_a_positive_input_budget`、`Every_downgrade_candidate_keeps_a_positive_input_budget`、`ForModelWindow_agrees_with_the_loader_on_the_default_profile`
  - `1edf69e`／`StickerContentLengthGuardTests`（3 格）＝`Content_accompanying_a_sticker_is_rejected_over_the_shared_limit(entry: TurnSubmission)`、`Content_accompanying_a_sticker_is_rejected_over_the_shared_limit(entry: DirectMessage)`、`Both_message_entries_call_the_one_ruler_and_no_second_ceiling_is_declared`

**一格算术差（照实登记，不当已补也不当不需要拆）**：工单 §③ 把"已做过完整十一项"的那一格（`ContextBuilder.cs:24` 夹零 ⇒ `ContextBudgetTests.The_raw_equal_output_and_context_window_yields_a_negative_input_budget`，`eval-QA-WAVE7-1022` §4.2）算进了"红 8 格"里，因此写"剩 14 格"。盘上第六波 core-red 的 8 格点名里**没有**那一格（它在第七波自己那一枚 M4 下才响）。按 `8＝8` 逐格补 ⇒ 本波 Core 组实际要补的是 **12 格**（`ContextBudgetTests` 8 ＋ `AttachmentLimitTests` 4），比工单的 11 多一格；那一格 `The_raw_equal...` 本波**不重做**（工单 §③ 明令，`ContextBuilder.cs:24` 那一行本波一字不碰，见 §0.6 第 0 项）。

### 0.5 跑批分组（按程序集，不按"把"）与预算声明

| 组 | 程序集 | 本波补的格 | 红批数 | 计划定向跑 |
|---|---|---|---|---|
| Core 组 | `Pry.Core.Tests` | A 把 12 格＝`ContextBudgetTests` 8 ＋ `AttachmentLimitTests` 4 | 2 批（每批 ≤5 枚） | 红 2 ＋ 还原复绿 1 ＝ **3** |
| Api 组 | `Pry.Api.Tests` | A 把 3 格＝`ContextWindowLoadTests` ＋ B 把 3 格＝`StickerContentLengthGuardTests` | 2 批（每批 ≤4 枚） | 红 2 ＋ 还原复绿 1 ＝ **3** |
| **合计** | | **18 格** | | **定向 6／6（硬顶用满）** |

| 资源 | 硬顶（工单 §⑥） | 计划用量 | 声明 |
|---|---|---|---|
| 解决方案级构建 | 1 | 1（收尾那一轮） | 不超 |
| 全量 | 1 | 1（收尾那一轮） | 不超 |
| 定向跑 | 6 | 6 | **用满，不留备用**（工单给的"4＋2"是按"每组一批红"算的；本波两把的 18 格需要 4 批红才能保住逐格归属，故把 2 次备用折进红批） |
| 单工程构建 | 未设上限 | 至多 4（四次 `dotnet test` 的隐式构建：Core 红 ×2、Core 绿 ×1、Api ×3 中前两次带源码突变需重编） | 另立一册如实记数（§八） |

> **互扰预案（无备用预算下的处置方式）**：每批落盘前逐格写死"预期红集合"，且**同一批内任两枚突变的预期红集必须互斥**（§一／§二的三问表就是这张互斥性证明）。跑完先逐格对集合：
> ① 预期红而实绿＝该枚空转 ⇒ 单独重跑一次或登记够不着；
> ② 多出来的红＝按读码判"同因多响"还是"互扰"；判为互扰时**不再单开一次跑**（预算已用满），而是当场还原该批、在报告里把那一格降格为"未取到干净红、归属存疑"，绝不拿脏读数顶账。

### 0.6 计划突变清单（事前声明，事后逐格对账；全部只存在于载体工作树，交付前逐字节还原）

编号规则：`M-CB*`＝`src/Pry.Core/Prompting/ContextBuilder.cs`；`M-AT*`＝`src/Pry.Core/TurnTaking/AttachmentTextExtractor.cs`；`M-W*`＝`src/Pry.Api/Services/ModelPerformancePolicy.cs`；`M-S*`＝会话两个写入入口。

| 编号 | 把 | 被改文件:行 | 突变形状（层级） | 预期红格 | 所在批 |
|---|---|---|---|---|---|
| 0 | — | **`ContextBuilder.cs:24` 一字不碰**（工单 §③ 明令那一格已做过） | — | — | — |
| M-CB1 | A | `ContextBuilder.cs:34` | 去掉"输出保留不超过窗口一半"那道夹（`Math.Min(..., contextTokens / 2)` 拿掉） | `ContextBudgetTests` C1／C6／C7／C8 ＋ Api 的 W1 | Core 批1／Api 批1／Api 批2 |
| M-CB2 | A | `ContextBuilder.cs:115` | 把装配前的窗口门从"算出来的预算"改成"只看原始上下文数字"（`total <= 0` → `b.ContextTokens <= 0`） | C5 | Core 批1 |
| M-CB3 | A | `ContextBuilder.cs:142` | 决策份额不再计入 `used`（`used += advisoryCost` → `used += 0`） | C2 | Core 批1 |
| M-CB4 | A | `ContextBuilder.cs:125` | 分节自身份额那一半门消失（`cost > cap \|\| used + cost > total` → 只留后半） | C3 | Core 批1 |
| M-CB5 | A | `ContextBuilder.cs:137` | 决策预留那道拒绝的两个阈值各抬一千倍 | C4 | Core 批2 |
| M-AT1 | A | `AttachmentTextExtractor.cs:15` | 压缩比上限 `100` → `1_000_000` | A1 | Core 批2 |
| M-AT2 | A | `:16` | 节点上限 `200_000` → `20_000_000` | A2 | Core 批2 |
| M-AT3 | A | `:17` | 深度上限 `200` → `2000` | A3 | Core 批2 |
| M-AT4 | A | `:18` | 名／属性名长度上限 `256` → `4000` | A4 | Core 批2 |
| M-CB6 | A | `ContextBuilder.cs:35` | "为正输入留底"那一半从天花板里拿掉（`- MinimumInputTokens` 去掉） | **单独不响任何格**；与 M-CB1 合力才响 W2（见 §二 2.2 纵深声明） | Api 批2 |
| M-W3 | A | `ModelPerformancePolicy.cs:47` | 加载器换算输出保留时不再按请求值（`intended` → `intended / 4`）＝"装配与加载两把尺分家" | W3 | Api 批2 |
| M-S1 | B | `ConversationSessionService.cs:37` | 贴纸那一支回到缺陷形状（绕过共享尺） | S1 ＋ S3 | Api 批1 |
| M-S2 | B | `ConversationApplicationService.cs:57` | 同形状，另一个入口 | S2 ＋ S3 | Api 批2 |

**逐格的格名简写表**（后文一律用这套简写，全名在各批十一项字段里逐字写）：
C1＝`A_real_default_model_window_survives_assembly_with_positive_budget`；C2＝`Advisory_is_counted_into_used_and_reaches_the_model_verbatim`；C3＝`Advisory_reservation_is_protected_while_optional_sections_drop`；C4＝`An_advisory_beyond_its_reserved_share_fails_loudly_and_is_ledged`；C5＝`Assembly_of_a_non_positive_window_fails_with_a_distinguishable_budget_error`；C6＝`ForModelWindow_turns_an_equal_output_and_context_into_a_positive_input_budget`；C7／C8＝`TryNormalizeWindow_accepts_real_model_windows(context: 32768, output: 32768)`／`(context: 4096, output: 4096)`；A1＝`A_high_compression_ratio_is_rejected`；A2＝`An_excessive_node_count_is_rejected`；A3＝`An_over_deeply_nested_document_is_rejected_before_full_parse`；A4＝`An_overlong_attribute_name_is_rejected`；W1＝`Loading_the_default_profile_at_full_context_yields_a_positive_input_budget`；W2＝`Every_downgrade_candidate_keeps_a_positive_input_budget`；W3＝`ForModelWindow_agrees_with_the_loader_on_the_default_profile`；S1／S2＝`Content_accompanying_a_sticker_is_rejected_over_the_shared_limit(TurnSubmission／DirectMessage)`；S3＝`Both_message_entries_call_the_one_ruler_and_no_second_ceiling_is_declared`。

**禁区自查（落盘前逐项核对）**：不碰 `docs/resource-limit-ledger.md`；不碰 `.5` 检索地板、`2400`（`AdvisoryTokens` 默认值）、五档超时（含 `DocxProcessingTimeoutMs = 15_000`——本波四枚 `M-AT*` 只动压缩比／节点／深度／名长度四个计数常量，不动 `MaxDocxDecompressedBytes`、不动 `MaxDocxTextCharacters`、不动超时）、`MinimumUsableTokensPerSecond = 12`、`4096` 地板（含 `GpuPerformanceTier` 的 4096 与 `IsTooSlow` 的 `> 4096`，以及降档梯子）；不放宽／删除／注释掉任何既有断言；不新建任何文件当门；`M-AT4` 特意选 `4000` 而不是 `4096`，免得读数被误读成动了那块地板。

### 0.7 预检与弹窗纪律

每一次 `dotnet` 之前（含单工程构建、含 `--no-build` 的 test）先跑
`powershell -NoProfile -ExecutionPolicy Bypass -File "C:\Users\24782\Desktop\Pry_Project\.local-notes\game-preflight4-1022.ps1"`，
整段输出（含 `VERDICT=`）当场原样贴进本报告，标号 `PF-*`。`VERDICT=HOLD` ⇒ 就地停刀回报，不等、不绕、不改预检脚本。`free_memory_gb` 低于 6 只按 WARN 处理；只有真出现 SQLite `disk I/O`／`out of memory` 形状才按作废处理。

### 0.8 收尾预案（防再次撞轮次上限）

- 证据目录＝`C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave8-1022\`，每批一个新名：`run1-core-red`、`run2-core-red`、`run3-core-green`、`run4-api-red`、`run5-api-red2`、`run6-api-green`、`run7-full`。每批跑完先 `ls *.trx` 数份数，再读 `<Counters>`。
- 还原手段＝`git -C <载体> checkout -- <本 lane 自己改过的那些路径>`，事后与 `git show HEAD:<file> | sha256sum` 逐字节比对（不用 PS5.1 的 `Set-Content -Encoding UTF8`）。入场实测的 HEAD 侧 SHA-256 已先记在盘前：
  `ContextBuilder.cs`＝`c5da1e3e32775d1da5c1844e9d9227d39657aa2796f1301fd65349c9d5bae3ef`（与第七波 §4.2 那条一致 ✓）；
  `AttachmentTextExtractor.cs`＝`8406cfad1d12f890062537e5aebdeb4ef0166c3c88f2151fa1004722681e7ee4`；
  `ConversationSessionService.cs`＝`6e346ca3aef7930d281471a99d22725a4eb5898e0b4a955bd34847658229ed91`；
  `ConversationApplicationService.cs`＝`b94334acd9b20f635c364e51c0d67a8faa329b85703abfb47ecabf2c549fead0`；
  `ModelPerformancePolicy.cs`＝`3f18ee19e64175fc2141f00b3ff3b2e787f15f6114469dbb2eea5c77337eaf52`。
  载体工作树这五份文件入场哈希与 HEAD 逐字节相同（当场核过）⇒ 载体树是干净的，还原目标明确。
- 感到轮次不够：**立刻停止开新批次**，把已完成批次写进本文件，再在末尾"缺口"节写清做到哪一格、缺哪几格、盘上证据在哪个目录。宁可交带缺口的报告，不交"活干了痕迹没了"。

---

## 一、A 把预检三问（零构建读码；Core 组 12 格 ＋ Api 组 `ContextWindowLoadTests` 3 格）

产品调用链（读码取实际位置，载体 `6b6f549`）：

- `ContextBuilder.BuildAsync` 的生产入口＝`src/Pry.Core/Conversation/ConversationService.cs:27` 与 `src/Pry.Core/TurnTaking/ReplyPlanner.cs:64` 各经 `BuildConversationAsync`（`ContextBuilder.cs:84`）进来；`BuildConversationAsync:97` 用 `ContextBudget.ForModelWindow(profile.ContextSize, profile.MaxOutputTokens)` 换算预算。
- `ContextBudget.TryNormalizeWindow`（`ContextBuilder.cs:30`）另有两条生产入口＝`ForModelWindow:45` 与 `src/Pry.Api/Services/ConfigurationApplicationService.cs:626`（配置保存时校验联合预算）；`ModelPerformancePolicy.WithContext:47`（模型降档链）也调它。
- **一个生命周期里跑几回**：`ForModelWindow`／`TryNormalizeWindow` 每次装配跑一次（每条用户消息一轮装配一回），配置保存与模型降档链各按入口另计；`BuildAsync` 里的预算不等式（`:114-115`）与决策预留（`:132-143`）每轮装配各执行一次；`AttachmentTextExtractor.ReadDocxAsync` 只在带 DOCX 附件的那一轮里执行（`src/Pry.Api/Runtime/RuntimeAdapters.cs:82`、`src/Pry.Core/TurnTaking/ConversationTurnManager.cs:142`），一次附件一回。
- 归属到 A 把的产品行（工单 §③ 点名的两处）＝`src/Pry.Core/Prompting/ContextBuilder.cs`（归一化、`ForModelWindow`／`TryNormalizeWindow`）与 `src/Pry.Core/TurnTaking/AttachmentTextExtractor.cs`（解压限额那几处计数器）。

### 1.1 Core 组逐格三问（12 格）

| 格 | 谁调用这一行 | 一个生命周期跑几回 | 什么输入能走到"拒绝／越界"那一支 | 拆法（编号见 §0.6） |
|---|---|---|---|---|
| C6 `ForModelWindow_turns...` | 装配入口 `BuildConversationAsync:97` 每轮一次；`ForModelWindow:43` 内部第一步就是 `TryNormalizeWindow:34` 的半窗口夹 | 每次装配 1 回 | `ForModelWindow(32768, 32768)`（出厂 1.7B 档形状）——夹一拿掉，`capped` 直接取 32768，再被 `:35-36` 的天花板削到 32128 ⇒ `OutputTokens` 32128 ≠ 16384，且 `:69` 的"输出＋安全＋输入＝窗口"等式一侧也变 | M-CB1 |
| C7／C8 `TryNormalizeWindow_accepts_real_model_windows(32768/32768、4096/4096)` | 同上；这两行是 Theory 的逐行判据 | 同上 | 只有"请求输出 ≥ 半窗口"的两行会走到夹那一支：32768/32768 与 4096/4096；262144/32768（32768 ≤ 131072）与 32768/8192（8192 ≤ 16384）本来就没碰夹 ⇒ 拆掉夹只有那两行变、这两行仍绿（＝第六波盘上点名恰为 2 行） | M-CB1 |
| C1 `A_real_default_model_window_survives_assembly_with_positive_budget` | 同 C6 路径，且真的走一次 `BuildAsync` | 每次装配 1 回 | `ForModelWindow(32768, 32768)` 的输入预算被 M-CB1 从 16256 变成 512 ⇒ `Assert.Equal(16256, result.InputBudget)` 响；其余断言（`Contains("IDENTITY")`、末条 `"CURRENT"`）不依赖预算数值，所以响点唯一 | M-CB1 |
| C5 `Assembly_of_a_non_positive_window_fails_with_a_distinguishable_budget_error` | `BuildAsync:103` 直接吃 `new ContextBudget(32768, 32768)`（**不经**归一化） | 每次装配 1 回 | 32768/32768 ⇒ `:114` 算出 `total = -128`；`:115` 是"可区分的 WindowTooSmall"唯一守门。把这一行的判据从 `total` 换成 `b.ContextTokens` ⇒ 32768 > 0 不再拒，流程落到 `:118` 那条 `used > total`，抛出的是默认 `IdentityAndCurrentEvent` ⇒ 正是该行注释警告的"被身份超预算掩盖成误导语义" | M-CB2 |
| C2 `Advisory_is_counted_into_used_and_reaches_the_model_verbatim` | `BuildAsync:132-143` 决策预留段，每轮装配 1 回 | 每次装配 1 回 | 带非空 `Advisory`（该格用 23 个中文字 ≈ 70 字节）⇒ `:142` 是把这段计入 `used` 的唯一位置；改成 `used += 0` 后"带决策"与"不带决策"两次装配的 `InputTokens` 相等 ⇒ `Assert.True(withAdvisory.InputTokens > withoutAdvisory.InputTokens)` 响。`:137` 的两道阈值（70 ≪ 2400、107 ≤ 16256）都不参与拒绝，所以这格只由 `:142` 守 | M-CB3 |
| C3 `Advisory_reservation_is_protected_while_optional_sections_drop` | `BuildAsync` 的 `Section`（`:120-127`）＋决策逐字追加（`:190`） | 每次装配按分节数各 1 回 | 该格预算 800/100/100 ⇒ `total = 600`；`State` 给 300 个字符（约 300 字节）而 `StateTokens = 100` ⇒ 走的是 `:125` 前半"分节自身份额"那一支。拿掉前半后只剩 `used + cost > total`（237 + 316 = 553 ≤ 600）⇒ "当前状态"不再被丢 ⇒ `Assert.Contains(result.Dropped, d => d.Section == "当前状态")` 响 | M-CB4 |
| C4 `An_advisory_beyond_its_reserved_share_fails_loudly_and_is_ledged` | `BuildAsync:137` 的预留拒绝支 | 每次装配 1 回 | 该格预算 4096/512/128 ⇒ `total = 3456`，`AdvisoryTokens = 64`，`Advisory` 给 5000 个 `x` ⇒ `advisoryCost = 5001`。**读码要点**：这一行是两条子条件的合取防守面——只削前半（`5001 > 64·1000` 假）时后半 `37 + 5001 > 3456` 仍真 ⇒ 仍拒；只削后半时前半仍真 ⇒ 仍拒。要拿到红必须同一行两个阈值一起抬（M-CB5 抬一千倍），这也是"这一格靠纵深不靠单点"的实测形状 | M-CB5 |
| A1 `A_high_compression_ratio_is_rejected` | `ReadDocxAsync:67`（条目头）与 `:89`（实际字节复核）两处共用 `MaxDocxCompressionRatio` | 每个 DOCX 附件 1 回 | 20 万个重复 `a`＋`CompressionLevel.Optimal` ⇒ 压缩后几百字节，比值约千：`> 100` 拒。把上限抬到 `1_000_000` ⇒ 两支都不拒 ⇒ 正文照常进提示词，`Contains("超出安全解压限额")`／`Contains("压缩比上限")` 双双响 | M-AT1 |
| A2 `An_excessive_node_count_is_rejected` | `ParseWordBody:120` | 每个 DOCX 附件解析时逐节点累加 | 250 001 个 `<w:p/>`（Stored，比值 1 ⇒ 不会被 A1 的门先拒） ⇒ `++nodes > 200_000`。抬到 20 000 000 ⇒ 解析走到底、正文为空 ⇒ 限额串不出现 | M-AT2 |
| A3 `An_over_deeply_nested_document_is_rejected_before_full_parse` | `ParseWordBody:121` | 同上 | 300 层未闭合 `<w:p>`（Stored ⇒ 避开比值门），深度 302 > 200。抬到 2000 ⇒ 不再在"分配之前"拒，而是让 `XmlReader` 撞上不闭合标签自己抛 `XmlException` ⇒ 被 `ExtractForPromptAsync:37` 折成"读取失败"，`Contains("超出安全解压限额")`／`Contains("XML 深度上限")` 响。**这一格的形状恰好就是行名说的"before full parse"**：限额门失效后错误语义会从"超限拒绝"退化成"读取失败" | M-AT3 |
| A4 `An_overlong_attribute_name_is_rejected` | `ParseWordBody:127-128`（属性名；同一常量还被 `:122` 的元素名用） | 同上 | `w:`＋300 个 `a` 的属性名（Stored） ⇒ 长度 302 > 256。抬到 4000 ⇒ 放行；正文为空 ⇒ 限额串不出现 | M-AT4 |

**批内互斥性（同批任两枚的预期红集不相交，逐枚核对过）**：
批1＝{M-CB1→C1,C6,C7,C8}、{M-CB2→C5}、{M-CB3→C2}、{M-CB4→C3}——四枚的作用面分别是归一化半窗口夹、`:115` 窗口门、`:142` 计入、`:125` 分节份额门；C2 用 `ForModelWindow`（受 M-CB1 影响但按其形状仍绿：`total` 512 > used 107），C3／C5 用显式预算不碰 M-CB1；M-CB2 只在 `total ≤ 0` 的形状上改变行为（C5 专属，全仓只有 `ContextBudgetTests.cs:105` 断言 `WindowTooSmall`）；M-CB4 影响的分节丢弃在 Core 里另有两处断言面（`MemoryContextTests.cs:213` 那条、`MemoryFloorRecheckTests.cs:85` 那条），逐值算过：前者被 `used + cost > total` 兜住、后者断言的是 `stale_or_deleted` 记账，两枚都不响。
批2＝{M-CB5→C4}、{M-AT1→A1}、{M-AT2→A2}、{M-AT3→A3}、{M-AT4→A4}——四枚解压计数器彼此正交（比／节点／深度／名长度四个不同谓词，且各格的样本刻意用 Stored 避开比值门）；M-CB5 只碰决策预留阈值，Core 里唯一带超份额 `Advisory` 的格是 C4（全仓 `Advisory` 只出现在 `ContextBudgetTests.cs`）。

### 1.2 Api 组（A 把那 3 格）逐格三问

产品面＝`src/Pry.Api/Services/ModelPerformancePolicy.cs`（`CreateCandidates:11` 由 `src/Pry.Api/Services/ModelProcessRegistry.cs:41` 在每次模型启动尝试链里逐档消费；`WithContext:39` 每档跑一次，其中 `:47` 调 `ContextBudget.TryNormalizeWindow`＝与装配同一把尺）＋ `ContextBuilder.cs:34/:35`。

| 格 | 谁调用 | 跑几回 | 什么输入走到越界支 | 拆法 |
|---|---|---|---|---|
| W1 `Loading_the_default_profile_at_full_context_yields_a_positive_input_budget` | `CreateCandidates` → `WithContext` → `TryNormalizeWindow`；该格用 Balanced 档（RTX 4060，显存 8100）⇒ 初始窗口不被降档掩盖 | 每次启动尝试链每档 1 回 | 32768/32768（出厂 1.7B 档）⇒ `:28` 断言"输出保留 ≤ 半窗口"。M-CB1 后输出保留成 32128 ⇒ 该断言先响 | M-CB1（单枚即够） |
| W2 `Every_downgrade_candidate_keeps_a_positive_input_budget` | 同上；该格遍历 262144/262144 的**全部**降档候选 | 每档 1 回，共 7 档 | **读码要点（纵深）**：`:43` 断言的 `input > 0` 由**两条**子条件共同守——半窗口夹（`:34`）与"为正输入留底"（`:35-36` 的 `ceiling`）。只拆 `:34` ⇒ 第一档 262144 被 `ceiling` 削到 261504，`input = 512 > 0` 仍绿；只拆 `:35` ⇒ 半窗口夹照旧给 131072，仍绿。**两枚同放**才让第一档输出保留成 262016 ⇒ `input = 0` ⇒ 响 | M-CB1 ∧ M-CB6（Api 批2 实测；Api 批1 只有 M-CB1 ⇒ 该格**预期实绿**，这一份"仍然全绿"按工单 §③ 当证据采下来，并在此登记"单拆任一支够不着"） |
| W3 `ForModelWindow_agrees_with_the_loader_on_the_default_profile` | 同一行两把尺：`ForModelWindow`（装配侧）与 `WithContext:47`（加载侧） | 每次装配 1 回 ＋ 每档 1 回 | 该格断言两侧换算**相等**。M-CB1 两侧同变 ⇒ 仍相等（**单拆 `:34` 拿不到这一格的红**）；只有让加载器不再按请求的输出保留换算（`intended` → `intended / 4`）才分家 ⇒ `Assert.Equal(loaded.MaxOutputTokens, budget.OutputTokens)` 响（8192 vs 16384） | M-W3（并登记"M-CB1 单拆时 W3 仍全绿"这一向） |

**Api 批内互斥性**：批1＝{M-CB1→W1}、{M-S1→S1,S3}，两枚分属不同判据面（模型档位 vs 源码文本门），互斥 ✓；批2＝{M-CB1→W1}、{M-CB1∧M-CB6→W2}、{M-W3→W3}、{M-S2→S2,S3}——W1 的归属与批1 同因（同一枚 M-CB1，两批各响一次，照 §①.4 另记不合并），W2 登记为"合力格"，W3 由 M-W3 唯一命中（M-CB1/M-CB6 都不改变 `ctx`＝`contextSize`，也都不改变 `intended/4` 之后的等式），S2 由 M-S2 唯一命中，S3 在批1 已由 M-S1 单独命中过（所以不需要把 S3 的归属压在批2 的两枚上）。

---

## 二、B 把预检三问（`1edf69e`＝HDS-011b AUD-07，3 格，全在 Api 组）

产品调用链（读码）：会话正文有两个写入入口——
`src/Pry.Api/Services/ConversationSessionService.cs:34 SubmitAsync`（`POST conversations/{id}/turns`，`:37` 校验）与
`src/Pry.Api/Services/ConversationApplicationService.cs:51 AddMessageAsync`（`POST conversations/{id}/messages`，`:57` 校验）；
两行现在写的是**同一句**`ContractValidation.MessageContent(request.Content, requireText: request.StickerId is null)`，唯一尺在 `src/Pry.Contracts/ApiContracts.cs:196 MessageContentMaxLength = 20_000` 与 `:212-220 MessageContent`——长度判断刻意写在"带不带贴纸"分支**之前**（`:207-210` 注释就是这一刀的原始缺陷形状）。
一个生命周期里跑几回：每次用户提交一轮 / 每条直写消息各 1 回；`MessageContent` 每次写入 1 回。

| 格 | 谁调用 | 跑几回 | 什么输入能走到"拒绝"那一支 | 拆法 |
|---|---|---|---|---|
| S1 `Content_accompanying_a_sticker_is_rejected_over_the_shared_limit(entry: TurnSubmission)` | `SubmitAsync:37` | 每轮提交 1 回 | `content = 20 001 个"字"` ＋ `stickerId = "happy"` ⇒ 唯一拒点是共享尺；把该行退回缺陷形状（贴纸那一支只做 `?.Trim() ?? ""`）⇒ 超长文本不再被拒 ⇒ `Assert.ThrowsAsync<ApiValidationException>` 报"没有抛出/抛错类型"而响 | M-S1 |
| S2 `... (entry: DirectMessage)` | `AddMessageAsync:57` | 每条直写 1 回 | 同上，另一个入口 ⇒ 同一把尺在两处各接线一次，必须各拆一次才各自响 | M-S2 |
| S3 `Both_message_entries_call_the_one_ruler_and_no_second_ceiling_is_declared` | 源码文本门：`:121-124` 逐文件数那句共享调用的出现次数并禁止旧形状复活；`:127-140` 扫 `src/Pry.Api`／`src/Pry.Contracts` 里 `20_000` 的声明点唯一性 | 每次跑读源码文本 | 任一入口的那一行被换掉 ⇒ 该文件的出现次数从 1 变 0，且文件里出现被禁的 `request.Content?.Trim() ?? ""` ⇒ 同一枚突变即响（**行为断言守不住"只补一个入口"，这一格才存在**——测试自己的注释 `:11-15` 就这么写） | M-S1（批1 单枚命中，归属干净）；批2 由 M-S2 再命中一次 ⇒ 记为"同一格的两枚同因"，不合并、不省略 |

**B 把不做的两格（照实登记，不是"不需要拆"）**：`Plain_text_message_keeps_the_same_limit_it_always_had(TurnSubmission／DirectMessage)`、`The_shared_ruler_sits_at_the_original_ceiling_and_tolerates_both_message_shapes`、`Sticker_message_under_the_limit_is_stored_with_both_the_text_and_the_sticker` 不在第六波 3 格点名里 ⇒ 本波不拆。其中"上限数字唯一声明"那一支若要单独拿红，得改 `ApiContracts.cs:196` 的 `20_000` 本身，而那会把 `ResourceLimitLedgerCoverageTests`（另一把名下的限额总盘门）一起带响 ⇒ 属另一把的面，本波不动、不冒充。

**读码结论（预先声明，跑完对账）**：M-S1/M-S2 的形状是"把某一侧退回 AUD-07 的原始缺陷"，因此除目标格外只可能影响三处判据面——`Plain_text_*`（纯文本那一支仍走共享尺，逐值核对不响）、`Sticker_message_under_the_limit_*`（20 001 之外行为逐字等价：`Trim` 相同、`requireText` 语义相同）、以及 `ConversationSessionService.cs` 上另外两枚源码门（`CharacterIntelligenceSeamTests.cs:382`、`CapabilitySeamWiringTests.cs:161`，它们找的是网关／能力接线文本，与本行无涉）。

---

## 三、Core 组批1（M-CB1／M-CB2／M-CB3／M-CB4）——**已落盘、未起跑（预检 HOLD）**

### 3.1 停刀点（当场原文，未做任何绕行）

每一次 `dotnet` 之前先跑预检；本波第一次预检即 `VERDICT=HOLD` ⇒ 按工单 §⑥ 就地停刀回报。**本 lane 全程没有起过一次 `dotnet`**（没有构建、没有 test、没有 `--no-build` 跑），因此也没有任何 TRX、没有单工程构建、没有解决方案级构建、没有全量。

#### PF-1（Core 批1 定向跑之前，M-CB1／M-CB2／M-CB3／M-CB4 已写盘）

```
=== GAME PREFLIGHT v4 (foreground rule) ===
  FOREGROUND: explorer pid=13364 style=0x96000000 rect=-1920,0,4480x1440 screen=2560x1440 covers=True borderless=True title=Program Manager
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21228, NVIDIA Overlay pid=15532
gpu_util=5% used=1658MB
free_memory_gb=8.6
VERDICT=HOLD
  reason: focused window is a borderless full-screen app (explorer)
```

（`style=` 那一列的原始字符是 `0x96000000`，上面的代码块按脚本当场输出逐字符粘贴，未做任何改写；此处再单行留一份以防代码块折行：`FOREGROUND: explorer pid=13364 style=0x96000000 rect=-1920,0,4480x1440 screen=2560x1440 covers=True borderless=True title=Program Manager`。）

- 读数要点：焦点窗＝`explorer`（`Program Manager`），`covers=True`、`borderless=True` ⇒ 判据给的是 `HOLD`，理由行写的是"焦点窗是一个无边框全屏应用"。`free_memory_gb=8.6` 高于 6，**不是** WARN 触发的停刀，也不是 SQLite `disk I/O`／`out of memory` 形状；停刀纯粹由 `VERDICT=HOLD` 那一条令出。
- 未做的三件事（照工单 §⑥ 的反向约束，逐条声明）：没有"等三分钟再试"、没有换成更省事的跑法绕开预检、没有改 `game-preflight4-1022.ps1` 的任何判据（脚本一字未动，`git status --short` 在主线也只有本 lane 名下这一份新报告）。

### 3.2 这一批实际落盘了什么（写盘→还原全程当场实测；**没有跑，所以没有红**）

载体＝`6b6f549`（完整号 `6b6f5498ea8d32153d4d61de5bca04a6e1f32715`），四枚全在同一个文件、同一棵工作树里，一次放齐（符合工单 §④.2"同一组内一次放 3～4 枚"）。

`git diff --stat`（写盘后当场实测）＝

```
 src/Pry.Core/Prompting/ContextBuilder.cs | 8 ++++----
 1 file changed, 4 insertions(+), 4 deletions(-)
```

`git diff --numstat`＝`4  4  src/Pry.Core/Prompting/ContextBuilder.cs`；`git status --short`＝**1 行**且只有这一条预期文件 ⇒ 没有出现"改到别人/别的文件"的形状。

四枚的 before→after 原文（`git diff` 逐字抄，行号为 `6b6f549` 原文件行号）：

| 编号 | 行 | before（原文） | after（原文） |
|---|---|---|---|
| M-CB1 | `:34` | `        var capped = Math.Min(Math.Max(requestedOutputTokens, 0), Math.Max(0, contextTokens / 2));` | `        var capped = Math.Max(requestedOutputTokens, 0);` |
| M-CB2 | `:115` | `        if (total <= 0) throw new ContextBudgetException(ContextBudgetFailure.WindowTooSmall);` | `        if (b.ContextTokens <= 0) throw new ContextBudgetException(ContextBudgetFailure.WindowTooSmall);` |
| M-CB4 | `:125` | `            if (cost > cap \|\| used + cost > total) { dropped.Add(new(name, "budget")); return; }` | `            if (used + cost > total) { dropped.Add(new(name, "budget")); return; }` |
| M-CB3 | `:142` | `            used += advisoryCost;` | `            used += 0;` |

（四枚都是改在判据指向的那一行本身：夹掉一个表达式／换比较的左侧操作数／拿掉合取的一支／把计入量换成常量 0。没有注释掉任何行、没有删类型、没有改签名、没有新建任何文件、没有动 `:24` 的 `InputBudget`（工单 §③ 明令那一格不重做）、没有动 `2400`／`.5`／`4096`／五档超时／`MinimumUsableTokensPerSecond = 12`。）

### 3.3 这一批的十一项字段：拿到哪几项、缺哪几项（**缺项照实写缺项，不用"已拆过"概括**）

| 项 | 值 |
|---|---|
| ① 载体提交号 | `6b6f5498ea8d32153d4d61de5bca04a6e1f32715`（detached `6b6f549`）✓ 已有 |
| ② 被改文件:行 | `src/Pry.Core/Prompting/ContextBuilder.cs` 的 `:34`／`:115`／`:125`／`:142`（四枚）✓ 已有 |
| ③ before→after 原文 | 见 §3.2 表，逐字取自 `git diff` ✓ 已有 |
| ④ `git diff --stat` 数字 | `1 file changed, 4 insertions(+), 4 deletions(-)`；`--numstat`＝`4 4`；文件集合＝只有预期的那一份 ✓ 已有 |
| ⑤ 跑法整行命令 | **未取**——预检 HOLD，从未起跑。计划的整行＝`dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave8-1022\run1-core-red" --logger trx`（工作目录＝载体根，隐式构建即该组唯一一次构建） |
| ⑥ TRX 文件名 | **未取**（目录 `run1-core-red` 未创建，盘上没有任何 `.trx`） |
| ⑦ 红格用例全名＋消息要点 | **未取**——本波**没有任何一格拿到实测红**。§1.1 那张表里的预期红集（C1／C2／C3／C5／C6／C7／C8）与"预期实绿"（W2 在单枚 M-CB1 下、C7/C8 之外的两行 Theory）全部是**读码推演，未验证**，不得当证据引用 |
| ⑧ 还原方式 | `git checkout -- src/Pry.Core/Prompting/ContextBuilder.cs`（只在载体内、只针对本 lane 自己那四行所在的这一个文件）✓ 已执行 |
| ⑨ 还原后 SHA-256 | 还原前突变态＝`81eb3aaad7286650f380b4f6a35db2f98d2c2d9e4521bec3ec6039b864b89fa7`；还原后实测＝`c5da1e3e32775d1da5c1844e9d9227d39657aa2796f1301fd65349c9d5bae3ef`；`git show HEAD:src/Pry.Core/Prompting/ContextBuilder.cs \| sha256sum`＝`c5da1e3e32775d1da5c1844e9d9227d39657aa2796f1301fd65349c9d5bae3ef` ⇒ **逐字节一致 ✓**（也与第七波 §4.2 那条基线哈希同值）；未使用 PS5.1 的 `Set-Content -Encoding UTF8` |
| ⑩ 还原后 `git status --short` 行数 | **0 行**（`git diff --stat` 空、`git diff --check` 干净）✓ 已实测 |
| ⑪ 这一向的结论 | **未验证**：四枚突变的写盘形状与逐字节还原已经实测；"这四枚能不能各自把那七格打红"这一步**没有跑过，不作结论**。禁止把 §1.1 的读码推演写成红证 |

### 3.4 停刀时的资源与盘上状态

| 项 | 状态 |
|---|---|
| 解决方案级构建 | 0／1（未用） |
| 全量 | 0／1（未用） |
| 定向跑 | 0／6（未用） |
| 单工程构建 | 0 |
| 焦点预检 | **1 次**（PF-1，`VERDICT=HOLD`） |
| 载体状态 | detached `6b6f549`、`git status --short`＝**0 行**、`git diff --stat` 空 ⇒ 没有把改坏留给下一位 |
| 主工作树 | 只新增本文件一份未跟踪报告（`docs/eval/reports/2026-10-22-qa-wave8-waves1-remaining-two-knives.md`）；未 `git add`、未提交；他人名下两枚暂存项一字未读未改；其余未跟踪项一字未动 |
| 未创建的目录 | `C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave8-1022\`（含 `run1-core-red`～`run7-full` 七个计划中的证据目录，一个都没建） |
| 作者进程 | 全程未碰；`%LOCALAPPDATA%/PryCompanion` 未作为测试载体、连只读探查都没有 |

---

## 九、够不着的向与缺口（照实登记，不许把"没拆"写成"不需要拆"）

1. **本波 18 格里，0 格拿到实测红。** 阻塞事实＝第一次焦点预检 `VERDICT=HOLD`（焦点窗是无边框全屏的 `explorer`，`covers=True borderless=True`），按工单 §⑥ 就地停刀；未起任何 `dotnet` ⇒ 全部十一项字段的 ⑤⑥⑦ 三格（跑法／TRX／红格要点）在 18 格上**一律未到付**。
2. **已经补到盘上的只有一半原文**：四枚突变（M-CB1／M-CB2／M-CB3／M-CB4，覆盖 C1、C2、C3、C5、C6、C7、C8 共 7 格）的 ①②③④⑧⑨⑩ 七项有实测值，已在 §3.3 落盘；其余 **11 格**（C4、A1～A4、W1、W2、W3、S1～S3，对应 M-AT1～M-AT4、M-CB5、M-CB6、M-W3、M-S1、M-S2 以及 M-CB1 的 Api 侧）**连写盘都没做到**，因为停刀发生在第一批的起跑之前，第二批与两组还原复绿、收尾那一轮全部没开。（18 ＝ 已写盘 7 ＋ 未写盘 11。）
3. **读码侧的两条"够不着"预测，本波未获实测，不得引用为结论**：① W2（`Every_downgrade_candidate_keeps_a_positive_input_budget`）单拆 `:34` 那一支仍全绿，要 `:34` 与 `:35` 两枚合力才响（§1.2 的逐值推演）；② C4（`An_advisory_beyond_its_reserved_share_fails_loudly_and_is_ledged`）只削 `:137` 的份额那一支、或只削 `used + advisoryCost > total` 那一支，都仍会被另一支兜住 ⇒ 登记为"单枚够不着"，但**这两条都要靠实测才作数**，现按未验证挂账。
4. **一格算术差**（§0.4 已写）：工单 §③ 的"红 8 格"里不含 `The_raw_equal_output_and_context_window_yields_a_negative_input_budget`，那一格是第七波自己那枚 M4 打红的、已有完整十一项 ⇒ 本波按"8 格逐格补"执行，Core 组实际 12 格而非 11 格；那一格本波一字不碰（`ContextBuilder.cs:24`）。
5. **下一步（安全、不需本 lane 越权）**：任一时刻焦点窗不是无边框全屏应用时，重跑那一次预检得 `VERDICT=BUILD_OK`，再按 §0.5／§0.6 的批次表原样落盘 Core 批1 那四枚（before→after 见 §3.2，可逐字复用），跑 `run1-core-red`；此后依次是 Core 批2（M-CB5＋M-AT1～4）、Core 还原复绿、Api 批1（M-CB1＋M-S1）、Api 批2（M-CB1＋M-CB6＋M-W3＋M-S2）、Api 还原复绿、收尾解决方案级构建 1＋全量 1。定向 6 次的额度一次都没花。

## 十、结论（只写事实，不写状态词）

- 载体入场条件（`git status --short`＝0 行、HEAD＝`6b6f549`）当场核过；§〇／§一／§二 在动第一枚突变之前写满并落盘；第一次 `dotnet` 之前跑了预检，读到 `VERDICT=HOLD` ⇒ 就地停刀，一次 `dotnet` 也没起，预算 0／(1＋1＋6) 未动。
- Core 批1 的四枚突变真实写进过载体工作树（`1 file changed, 4 insertions(+), 4 deletions(-)`、只动预期文件），before→after 原文与突变态哈希已留档，随后 `git checkout --` 逐字节还原并与 `git show HEAD:<file>` 的 SHA-256 一致，还原后载体 `git status --short`＝0 行。
- 本波交付的"拆法原文"目前只到"改哪一行、改成什么、怎么还原、还原凭据"这一半；"这一行改坏后到底哪一格响、响在哪一行、消息形状是什么"这一半**未取**，18 格全部按未到付挂账。

---

# 以下为 `wave8b-lane` 续跑章节（§三补～§十一）

> 上一轮的 §〇～§十 为原文，一字未改。续跑把焦点预检换成 v5（工单 §9.5），据此在预检 `BUILD_OK` 下真实起跑，补出上一轮 §3.3 缺的十一项字段 ⑤⑥⑦（跑法／TRX／红格要点），并把连写盘都没做到的 11 格（Core 批2 的 C4／A1～A4 与 Api 的 W1／W2／W3／S1／S2／S3）逐批补出来。

## 三补、Core 组实测红（逐格十一项）

### 3补.1 Core 批1（M-CB1 `:34`／M-CB2 `:115`／M-CB4 `:125`／M-CB3 `:142`）——**已起跑、实测红 7 格**

四枚一次放齐（工单 §④.2 同一组 3～4 枚），逐字复用上一轮 §3.2 的 before→after。

| 项 | 值 |
|---|---|
| ① 载体提交号 | `6b6f5498ea8d32153d4d61de5bca04a6e1f32715`（detached `6b6f549`），入场 `git status --short`＝**0 行**（本轮当场自数，非引用上一轮）✓ |
| ② 被改文件:行 | `src/Pry.Core/Prompting/ContextBuilder.cs` 的 `:34`／`:115`／`:125`／`:142`（四枚，同一文件）✓ |
| ③ before→after 原文 | 见下方"批1 突变表"，逐字取自本轮 `git diff` ✓ |
| ④ `git diff --stat` 数字 | `1 file changed, 4 insertions(+), 4 deletions(-)`；`--numstat`＝`4  4  src/Pry.Core/Prompting/ContextBuilder.cs`；`git status --short`＝**1 行**且只有预期文件 ✓（与上一轮 §3.3 同值） |
| ⑤ 跑法整行命令 | `dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave8-1022\run1-core-red" --logger trx`，工作目录＝载体根；**未加** `--no-build`／`--no-restore`（隐式重建 Core，突变进二进制）✓ |
| ⑥ TRX 文件名 | `24782_PHYTHLINK_2026-10-22_18_46_02.trx`（`ls *.trx`＝1 份）✓ |
| ⑦ 红格用例全名＋消息要点 | 见"批1 红格逐格表"（7 格），全部只认 TRX 的 `<UnitTestResult outcome="Failed">` 与控制台原始消息要点 ✓ |
| ⑧ 还原方式 | `git checkout -- src/Pry.Core/Prompting/ContextBuilder.cs`（只在载体内、只针对本 lane 那四行所在文件；未用 PS5.1 `Set-Content -Encoding UTF8`）✓ 已执行 |
| ⑨ 还原后 SHA-256 | 突变态＝`81eb3aaad7286650f380b4f6a35db2f98d2c2d9e4521bec3ec6039b864b89fa7`（与上一轮 §3.3 同值）；还原后实测＝`c5da1e3e32775d1da5c1844e9d9227d39657aa2796f1301fd65349c9d5bae3ef`＝`git show HEAD:src/Pry.Core/Prompting/ContextBuilder.cs \| sha256sum` 同一值 ⇒ **逐字节一致 ✓** |
| ⑩ 还原后 `git status --short` 行数 | **0 行**（当场实测；注：批2 需在 `ContextBuilder.cs:137` 另放 M-CB5，故此处 0 行是"批1 单独还原"的读数，全组 0 行复证见 §四 run3） |
| ⑪ 这一向的结论 | **已验证**：M-CB1／M-CB2／M-CB3／M-CB4 四枚各把预期格打红，红格集合＝C1／C2／C3／C5／C6／C7／C8（7 格），与上一轮 §1.1 读码预测**逐格对上、无空转、无多余红**；两批同放不互扰（批1 未含 M-CB5／M-AT*）。 |

**批1 突变表**（before→after，行号为 `6b6f549` 原文件行号）：

| 编号 | 行 | before（原文） | after（原文） |
|---|---|---|---|
| M-CB1 | `:34` | `        var capped = Math.Min(Math.Max(requestedOutputTokens, 0), Math.Max(0, contextTokens / 2));` | `        var capped = Math.Max(requestedOutputTokens, 0);` |
| M-CB2 | `:115` | `        if (total <= 0) throw new ContextBudgetException(ContextBudgetFailure.WindowTooSmall);` | `        if (b.ContextTokens <= 0) throw new ContextBudgetException(ContextBudgetFailure.WindowTooSmall);` |
| M-CB4 | `:125` | `            if (cost > cap \|\| used + cost > total) { dropped.Add(new(name, "budget")); return; }` | `            if (used + cost > total) { dropped.Add(new(name, "budget")); return; }` |
| M-CB3 | `:142` | `            used += advisoryCost;` | `            used += 0;` |

**批1 红格逐格表**（TRX `outcome="Failed"` 全名＋消息要点，均出自 `ContextBudgetTests.cs`）：

| 简写 | 用例全名 | 消息要点 | 断言行 | 命中突变 |
|---|---|---|---|---|
| C1 | `Pry.Core.Tests.ContextBudgetTests.A_real_default_model_window_survives_assembly_with_positive_budget` | `Assert.Equal() Failure: Values differ`　Expected `16256`　Actual `512` | `ContextBudgetTests.cs:115` | M-CB1 |
| C2 | `Pry.Core.Tests.ContextBudgetTests.Advisory_is_counted_into_used_and_reaches_the_model_verbatim` | `Assert.True() Failure`　Expected `True`　Actual `False` | `:135` | M-CB3 |
| C3 | `Pry.Core.Tests.ContextBudgetTests.Advisory_reservation_is_protected_while_optional_sections_drop` | `Assert.Contains() Failure: Filter not matched in collection`　Collection `[DroppedSection { Section = RelevantMemories, Reason = scope_or_unavailable }]`（"当前状态"未被丢 ⇒ 落不到 Dropped 断言） | `:173` | M-CB4 |
| C5 | `Pry.Core.Tests.ContextBudgetTests.Assembly_of_a_non_positive_window_fails_with_a_distinguishable_budget_error` | `Assert.Equal() Failure: Values differ`　Expected `WindowTooSmall`　Actual `IdentityAndCurrentEvent` | `:105` | M-CB2 |
| C6 | `Pry.Core.Tests.ContextBudgetTests.ForModelWindow_turns_an_equal_output_and_context_into_a_positive_input_budget` | `Assert.Equal() Failure: Values differ`　Expected `16384`　Actual `32128` | `:64` | M-CB1 |
| C7 | `Pry.Core.Tests.ContextBudgetTests.TryNormalizeWindow_accepts_real_model_windows(context: 32768, output: 32768)` | `Assert.True() Failure`　Expected `True`　Actual `False` | `:92` | M-CB1 |
| C8 | `Pry.Core.Tests.ContextBudgetTests.TryNormalizeWindow_accepts_real_model_windows(context: 4096, output: 4096)` | `Assert.True() Failure`　Expected `True`　Actual `False` | `:92` | M-CB1 |

- 归属：M-CB1→{C1,C6,C7,C8}、M-CB2→{C5}、M-CB3→{C2}、M-CB4→{C3}；四枚互斥、无一格重复计数。
- TRX `<Counters total="527" executed="525" passed="518" failed="7" notExecuted="0">`；7 failed＝上表；剩 2 条＝`outcome="NotExecuted"` 的两枚环境跳过（`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`），与在册一致。executed 525＝518 passed＋7 failed；total 527＝executed 525＋2 跳过 ⇒ 条数与基线 Core 527 对平。

### 3补.2 Core 批2（M-CB5 `:137` 两阈值＋M-AT1～4 `:15`～`:18`）——**已起跑、实测红 5 格**

批1 已先逐字节还原（见 §3补.1 项⑩）后，本批只放 M-CB5（`ContextBuilder.cs:137`）＋ M-AT1～4（`AttachmentTextExtractor.cs:15`～`:18`）共 5 枚，两文件。

| 项 | 值 |
|---|---|
| ① 载体提交号 | `6b6f5498ea8d32153d4d61de5bca04a6e1f32715`（detached `6b6f549`）✓ |
| ② 被改文件:行 | `src/Pry.Core/Prompting/ContextBuilder.cs:137`（M-CB5，两阈值同抬）＋ `src/Pry.Core/TurnTaking/AttachmentTextExtractor.cs:15/:16/:17/:18`（M-AT1～4，四个计数上限）✓ |
| ③ before→after 原文 | 见下方"批2 突变表"，逐字取自本轮 `git diff` ✓ |
| ④ `git diff --stat` 数字 | `2 files changed, 5 insertions(+), 5 deletions(-)`；`--numstat`＝`1 1 ContextBuilder.cs` ＋ `4 4 AttachmentTextExtractor.cs`；`git status --short`＝**2 行**且只有这两份预期文件 ✓ |
| ⑤ 跑法整行命令 | `dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave8-1022\run2-core-red" --logger trx`，工作目录＝载体根；未加 `--no-build` ✓ |
| ⑥ TRX 文件名 | `24782_PHYTHLINK_2026-10-22_18_49_57.trx`（`ls *.trx`＝1 份）✓ |
| ⑦ 红格用例全名＋消息要点 | 见"批2 红格逐格表"（5 格），均出自 TRX `outcome="Failed"` ✓ |
| ⑧ 还原方式 | `git checkout -- src/Pry.Core/Prompting/ContextBuilder.cs src/Pry.Core/TurnTaking/AttachmentTextExtractor.cs`（只在载体内、只针对本 lane 这五枚所在的两个文件）——**执行与逐字节复证见 §四 run3**（同组还原正好覆盖本批两文件） |
| ⑨ 还原后 SHA-256 | 突变态实测＝`ContextBuilder.cs` `24a7d350dc8042fa404a0c99e7236fb633108cbebaa3567b4e77cc60c5433c4b`、`AttachmentTextExtractor.cs` `b4ad7682970f0f83307f7efd92830a84db1120b71448deefb1dc06364a843d78`；还原后 HEAD 侧目标值＝`ContextBuilder.cs` `c5da1e3e…bae3ef`、`AttachmentTextExtractor.cs` `8406cfad1d12f890062537e5aebdeb4ef0166c3c88f2151fa1004722681e7ee4`（§0.8 入场基线），逐字节复证在 §四给出 |
| ⑩ 还原后 `git status --short` 行数 | 本批单独还原后应为 0；全组 0 行复证见 §四 run3（还原两文件后跑绿那一轮） |
| ⑪ 这一向的结论 | **已验证**：M-CB5 把 C4 打红、M-AT1～4 各把 A1～A4 打红，红格集合＝C4／A1／A2／A3／A4（5 格），与预测**逐格对上、无空转、无多余红**；且批1 的 C1／C2／C3／C5／C6／C7／C8 在这一轮**全绿**（批1 已还原，两批隔离成立）。C4 的实测形状印证 §1.1 的读码要点：M-CB5 两阈值同抬才拿到红（"单侧抬仍被另一侧兜住"本轮未单独复测，仍是读码预测，不写成实测）。 |

**批2 突变表**（before→after，行号为 `6b6f549` 原文件行号）：

| 编号 | 行 | before（原文） | after（原文） |
|---|---|---|---|
| M-CB5 | `ContextBuilder.cs:137` | `            if (advisoryCost > b.AdvisoryTokens \|\| used + advisoryCost > total)` | `            if (advisoryCost > b.AdvisoryTokens * 1000 \|\| used + advisoryCost > total * 1000)` |
| M-AT1 | `AttachmentTextExtractor.cs:15` | `    private const long MaxDocxCompressionRatio = 100;` | `    private const long MaxDocxCompressionRatio = 1_000_000;` |
| M-AT2 | `:16` | `    private const int MaxDocxNodes = 200_000;` | `    private const int MaxDocxNodes = 20_000_000;` |
| M-AT3 | `:17` | `    private const int MaxDocxDepth = 200;` | `    private const int MaxDocxDepth = 2000;` |
| M-AT4 | `:18` | `    private const int MaxDocxNameLength = 256;` | `    private const int MaxDocxNameLength = 4000;` |

（四枚 `M-AT*` 只动压缩比／节点／深度／名长度四个计数常量，未动 `MaxDocxDecompressedBytes`、`MaxDocxTextCharacters`、`DocxProcessingTimeoutMs = 15_000`；`M-AT4` 取 `4000` 而非 `4096`，避开 `4096` 地板的误读。）

**批2 红格逐格表**（TRX `outcome="Failed"` 全名＋消息要点）：

| 简写 | 用例全名 | 消息要点 | 断言行 | 命中突变 |
|---|---|---|---|---|
| C4 | `Pry.Core.Tests.ContextBudgetTests.An_advisory_beyond_its_reserved_share_fails_loudly_and_is_ledged` | `Assert.Throws() Failure: No exception was thrown`　Expected `typeof(Pry.Core.Prompting.ContextBudgetException)`（预留门被抬到千倍后不再拒） | `ContextBudgetTests.cs:150` | M-CB5 |
| A1 | `Pry.Core.Tests.AttachmentLimitTests.A_high_compression_ratio_is_rejected` | `Assert.Contains() Failure: Sub-string not found`　String `[附件：test.docx]\naaaaaaa…`（解压正文漏进提示词）　Not found `超出安全解压限额` | `AttachmentLimitTests.cs:75` | M-AT1 |
| A2 | `Pry.Core.Tests.AttachmentLimitTests.An_excessive_node_count_is_rejected` | `Assert.Contains() Failure: Sub-string not found`　String `[附件：test.docx]\n`　Not found `超出安全解压限额` | `:100` | M-AT2 |
| A3 | `Pry.Core.Tests.AttachmentLimitTests.An_over_deeply_nested_document_is_rejected_before_full_parse` | `Assert.Contains() Failure: Sub-string not found`　String `[附件：test.docx，读取失败]`（限额门失效后退化成"读取失败"）　Not found `超出安全解压限额` | `:88` | M-AT3 |
| A4 | `Pry.Core.Tests.AttachmentLimitTests.An_overlong_attribute_name_is_rejected` | `Assert.Contains() Failure: Sub-string not found`　String `[附件：test.docx]\n`　Not found `超出安全解压限额` | `:110` | M-AT4 |

- 归属：M-CB5→{C4}、M-AT1→{A1}、M-AT2→{A2}、M-AT3→{A3}、M-AT4→{A4}；五个谓词（比／节点／深度／名长度＋预留阈值）彼此正交，无同因多响。
- TRX `<Counters total="527" executed="525" passed="520" failed="5" notExecuted="0">`；5 failed＝上表；两条 `NotExecuted` 环境跳过与批1 同（按名点名见 §四）。executed 525＝520 passed＋5 failed；total 527＝525＋2 跳过 ⇒ 与 Core 基线 527 对平。
- Core 组累计实测红＝批1 7 ＋ 批2 5 ＝ **12 格**（`ContextBudgetTests` 8 ＋ `AttachmentLimitTests` 4），覆盖 A 把在 Core 的全部 12 格，与工单更正后的 18 格盘（Core 12／Api 6）的 Core 半对齐。

## 四、Core 还原与复绿（`run3-core-green`）

批1 的还原已在 §3补.1 项⑧⑨⑩ 逐字节留档（`ContextBuilder.cs` 还原后＝`c5da1e3e…bae3ef`＝HEAD）；本节覆盖批2 两文件的最终还原与整组复绿。

| 项 | 值 |
|---|---|
| 还原方式 | `git checkout -- src/Pry.Core/Prompting/ContextBuilder.cs src/Pry.Core/TurnTaking/AttachmentTextExtractor.cs`（只在载体内、只针对本 lane 改过的这两份文件；未用 PS5.1 `Set-Content -Encoding UTF8`）✓ 已执行 |
| 还原后 SHA-256 比对 | `ContextBuilder.cs` 还原后 `c5da1e3e32775d1da5c1844e9d9227d39657aa2796f1301fd65349c9d5bae3ef` ＝ `git show HEAD:` 同值 ✓；`AttachmentTextExtractor.cs` 还原后 `8406cfad1d12f890062537e5aebdeb4ef0166c3c88f2151fa1004722681e7ee4` ＝ `git show HEAD:` 同值 ✓（两份均与 §0.8 入场基线哈希一致） |
| 还原后 `git status --short` 行数 | **0 行**；`git diff --check` 干净 ✓ |
| 复绿跑法整行命令 | `dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave8-1022\run3-core-green" --logger trx`（与红批同一条命令行；源码已还原，隐式重建出干净二进制） |
| 复绿 TRX 文件名 | `24782_PHYTHLINK_2026-10-22_18_51_57.trx` |
| 复绿 `<Counters>` | `total="527" executed="525" passed="525" failed="0" notExecuted="0"` |
| 环境跳过（按名点名） | `outcome="NotExecuted"` 恰 2 条＝`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`、`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`（都在 `Pry.Core.Tests.dll` 名下；`notExecuted` 计数器恒 0，只能按 `<UnitTestResult outcome="NotExecuted">` 点名） |
| 这一向的结论 | **已验证复绿**：还原后 Core 回到在册基线 total 527／executed 525／passed 525＋那两条按名跳过，0 红；批1＋批2 共 12 枚突变全部逐字节还原，载体 `git status --short`＝0 行 ⇒ 未把任何改坏留给下一位。 |

## 五、Api 批1（M-CB1 `ContextBuilder.cs:34` ＋ M-S1 `ConversationSessionService.cs:37`）——**已起跑、实测红 3 格**

两枚一次放齐（不同程序集判据面互斥）：M-CB1（Core 归一化半窗口夹）＋ M-S1（贴纸会话入口退回缺陷形状，取自 `1edf69e^`）。

| 项 | 值 |
|---|---|
| ① 载体提交号 | `6b6f5498ea8d32153d4d61de5bca04a6e1f32715`（detached `6b6f549`）✓ |
| ② 被改文件:行 | `src/Pry.Core/Prompting/ContextBuilder.cs:34`（M-CB1）＋ `src/Pry.Api/Services/ConversationSessionService.cs:37`（M-S1）✓ |
| ③ before→after 原文 | M-CB1 `:34`：`        var capped = Math.Min(Math.Max(requestedOutputTokens, 0), Math.Max(0, contextTokens / 2));` → `        var capped = Math.Max(requestedOutputTokens, 0);`；M-S1 `:37`：`        var content = ContractValidation.MessageContent(request.Content, requireText: request.StickerId is null);` → 三行缺陷形状 `        var content = request.StickerId is null` / `            ? ContractValidation.Required(request.Content, "content", 20_000)` / `            : request.Content?.Trim() ?? "";` ✓（逐字取自本轮 `git diff`） |
| ④ `git diff --stat` 数字 | `2 files changed, 4 insertions(+), 2 deletions(-)`；`--numstat`＝`1 1 ContextBuilder.cs` ＋ `3 1 ConversationSessionService.cs`；`git status --short`＝**2 行**且只有这两份预期文件 ✓ |
| ⑤ 跑法整行命令 | `dotnet test tests/Pry.Api.Tests -c Release --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave8-1022\run4-api-red" --logger trx`，工作目录＝载体根；未加 `--no-build`（Core＋Api 随源码突变重建）✓ |
| ⑥ TRX 文件名 | `24782_PHYTHLINK_2026-10-22_18_54_10.trx` ✓ |
| ⑦ 红格用例全名＋消息要点 | 见"批1 红格逐格表"（3 格）；另采"纵深证据"＝W2／W3 本轮 `outcome="Passed"`（见下） |
| ⑧ 还原方式 | `ConversationSessionService.cs` 本批后 `git checkout` 逐字节还原（批2 用 M-S2 换另一入口）；`ContextBuilder.cs` 的 M-CB1 带入批2（批2 需在其上叠 M-CB6），其逐字节还原与哈希复证在 §七 run6 给出 |
| ⑨ 还原后 SHA-256 | 突变态实测＝`ContextBuilder.cs` `2ca80dbe05f823eee489e3015817c8d9ffcafa2b4fd63ec44a4548ee166e4c87`、`ConversationSessionService.cs` `b3d8e9598191042f0d0d8c200184ea3185f497224fedefd0ff8ebe0568f2b627`；HEAD 侧目标值＝`ContextBuilder.cs` `c5da1e3e…bae3ef`、`ConversationSessionService.cs` `6e346ca3aef7930d281471a99d22725a4eb5898e0b4a955bd34847658229ed91`（§0.8 入场基线），复证见对应还原点 |
| ⑩ 还原后 `git status --short` 行数 | `ConversationSessionService.cs` 单文件还原后＝HEAD 一致；Api 全组 0 行复证见 §七 run6 |
| ⑪ 这一向的结论 | **已验证**：M-CB1→W1、M-S1→S1＋S3，红格集合＝W1／S1／S3（3 格）与 §1.2／§二 预测逐格对上；且 **W2、W3 本批实绿**（`outcome="Passed"`），坐实 §1.2 的"单拆一支够不着／两把尺分家才分"纵深声明，这份"仍然全绿"按工单 §③ 当证据采下；S2（DirectMessage 入口）本批未动、仍绿；无多响、无空转。 |

**批1 红格逐格表**（TRX `outcome="Failed"` 全名＋消息要点）：

| 简写 | 用例全名 | 消息要点 | 断言行 | 命中突变 |
|---|---|---|---|---|
| W1 | `Pry.Api.Tests.ContextWindowLoadTests.Loading_the_default_profile_at_full_context_yields_a_positive_input_budget` | `Assert.True() Failure`　Expected `True`　Actual `False`（输出保留越过半窗口） | `ContextWindowLoadTests.cs:28` | M-CB1 |
| S1 | `Pry.Api.Tests.StickerContentLengthGuardTests.Content_accompanying_a_sticker_is_rejected_over_the_shared_limit(entry: TurnSubmission)` | `Assert.Throws() Failure: No exception was thrown`　Expected `typeof(Pry.Contracts.ApiValidationException)` | `StickerContentLengthGuardTests.cs:58`（经 `:159`） | M-S1 |
| S3 | `Pry.Api.Tests.StickerContentLengthGuardTests.Both_message_entries_call_the_one_ruler_and_no_second_ceiling_is_declared` | `Assert.Equal() Failure: Values differ`　Expected `1`　Actual `0`（会话入口那行共享调用计数从 1 变 0） | `:121` | M-S1 |

**同批"仍然全绿"证据（§1.2 纵深声明的实测）**：
- `Pry.Api.Tests.ContextWindowLoadTests.Every_downgrade_candidate_keeps_a_positive_input_budget`（W2）＝`outcome="Passed"`——单拆 M-CB1（`:34`）时天花板 `ceiling`（`:35`，含 `- MinimumInputTokens`）仍给正输入留底 ⇒ `input>0` 不破。
- `Pry.Api.Tests.ContextWindowLoadTests.ForModelWindow_agrees_with_the_loader_on_the_default_profile`（W3）＝`outcome="Passed"`——M-CB1 让装配侧与加载侧同变 ⇒ 两侧换算仍相等，未分家。
- `Pry.Api.Tests.StickerContentLengthGuardTests.Content_accompanying_a_sticker_is_rejected_over_the_shared_limit(entry: DirectMessage)`（S2）＝绿（本批未碰 `AddMessageAsync` 那一入口）。
- 另：`Plain_text_message_keeps_the_same_limit_it_always_had(TurnSubmission／DirectMessage)`、`Sticker_message_under_the_limit_is_stored_with_both_the_text_and_the_sticker` 均保持绿，与 §二 读码结论一致（纯文本一支仍走共享尺、贴纸不超限行为逐字等价）。

- TRX `<Counters total="278" executed="278" passed="275" failed="3" notExecuted="0">`；3 failed＝上表；Api 无环境跳过（两条 `NotExecuted` 属 Core 名下）⇒ executed=passed+failed=275+3=278=total，与 Api 基线 278 对平。

## 六、Api 批2（M-CB1 ＋ M-CB6 ＋ M-W3 ＋ M-S2）——**已起跑、实测红 3 格；W1／W2 实测绿（互扰／够不着，见 ⑪）**

四枚一次放齐（M-S1 已先还原、会话入口回干净，本批改用另一入口 M-S2）。M-CB1 从批1 带入，另叠 M-CB6（同函数 `:35`），再加 M-W3、M-S2。

| 项 | 值 |
|---|---|
| ① 载体提交号 | `6b6f5498ea8d32153d4d61de5bca04a6e1f32715`（detached `6b6f549`）✓ |
| ② 被改文件:行 | `ContextBuilder.cs:34`（M-CB1，带入）＋ `:35`（M-CB6，去掉 `- MinimumInputTokens`）＋ `ModelPerformancePolicy.cs:47`（M-W3，`intended`→`intended / 4`）＋ `ConversationApplicationService.cs:57`（M-S2，退回缺陷形状）✓ |
| ③ before→after 原文 | 见"批2 突变表"（逐字取自本轮 `git diff`）✓ |
| ④ `git diff --stat` 数字 | `3 files changed, 4 insertions(+), 4 deletions(-)`；`--numstat`＝`2 2 ContextBuilder.cs` ＋ `1 1 ModelPerformancePolicy.cs` ＋ `1 1 ConversationApplicationService.cs`；`git status --short`＝**3 行**且只有这三份预期文件 ✓ |
| ⑤ 跑法整行命令 | `dotnet test tests/Pry.Api.Tests -c Release --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave8-1022\run5-api-red2" --logger trx`，工作目录＝载体根；未加 `--no-build` ✓ |
| ⑥ TRX 文件名 | `24782_PHYTHLINK_2026-10-22_18_57_04.trx` ✓ |
| ⑦ 红格用例全名＋消息要点 | 见"批2 红格逐格表"（3 格）✓ |
| ⑧ 还原方式 | `git checkout -- src/Pry.Core/Prompting/ContextBuilder.cs src/Pry.Api/Services/ModelPerformancePolicy.cs src/Pry.Api/Services/ConversationApplicationService.cs`（执行与哈希见 §七 run6） |
| ⑨ 还原后 SHA-256 | 突变态实测＝`ContextBuilder.cs` `524e5c5a4635fa796fe41de89f35d10149eccf252ebce21ba336740b1a625aeb`、`ModelPerformancePolicy.cs` `08fa254f6b57bda4a40a4da61ddf0f4f3022e7d3d750294132da618fe071274c`、`ConversationApplicationService.cs` `defd7f4b6c51dbc97c10d48b5c002444d985f482a5b247d6ec4983839d90d25a`；HEAD 侧目标＝`ContextBuilder.cs` `c5da1e3e…bae3ef`、`ModelPerformancePolicy.cs` `3f18ee19e64175fc2141f00b3ff3b2e787f15f6114469dbb2eea5c77337eaf52`、`ConversationApplicationService.cs` `b94334acd9b20f635c364e51c0d67a8faa329b85703abfb47ecabf2c549fead0`（§0.8 入场基线），复证见 §七 |
| ⑩ 还原后 `git status --short` 行数 | Api 全组 0 行复证见 §七 run6 |
| ⑪ 这一向的结论 | **部分验证＋一格够不着**：M-W3→W3 红、M-S2→S2＋S3 红（本批 3 格实测红，红格集合⊂预期、无多响）；S1 本批绿（M-S1 已还原、会话入口干净，两入口隔离成立）。**但预期红的 W1、W2 本批实测绿**：M-CB1∧M-CB6 同放后，`TryNormalizeWindow` 在 `:39`（`inputBudget >= MinimumInputTokens`＝0≥512）判 false，`WithContext:47` 落到 `output=1` 兜底，反而给出正输入预算 ⇒ W1 的"输出保留≤半窗"与 W2 的"每档输入>0"都成立。**W1 的实测红在批1（run4，单 M-CB1）已拿到**；**W2 在两种配方下（批1 单 M-CB1、批2 合力）均实测绿 ⇒ 判为够不着／归属存疑**，按工单 §④.4 与报告 §0.5 互扰预案登记，定向额度已用满（5／6），不为凑红追跑。 |

**批2 突变表**（before→after）：

| 编号 | 行 | before（原文） | after（原文） |
|---|---|---|---|
| M-CB1 | `ContextBuilder.cs:34` | `        var capped = Math.Min(Math.Max(requestedOutputTokens, 0), Math.Max(0, contextTokens / 2));` | `        var capped = Math.Max(requestedOutputTokens, 0);` |
| M-CB6 | `:35` | `        var ceiling = contextTokens - DefaultSafetyTokens - MinimumInputTokens;` | `        var ceiling = contextTokens - DefaultSafetyTokens;` |
| M-W3 | `ModelPerformancePolicy.cs:47` | `        if (!ContextBudget.TryNormalizeWindow(contextSize, intended, out var ctx, out var output, out _))` | `        if (!ContextBudget.TryNormalizeWindow(contextSize, intended / 4, out var ctx, out var output, out _))` |
| M-S2 | `ConversationApplicationService.cs:57` | `        var content = ContractValidation.MessageContent(request.Content, requireText: request.StickerId is null);` | `        var content = request.StickerId is null ? ContractValidation.Required(request.Content, "content", 20_000) : request.Content?.Trim() ?? "";` |

**批2 红格逐格表**（TRX `outcome="Failed"`）：

| 简写 | 用例全名 | 消息要点 | 断言行 | 命中突变 |
|---|---|---|---|---|
| W3 | `Pry.Api.Tests.ContextWindowLoadTests.ForModelWindow_agrees_with_the_loader_on_the_default_profile` | 抛 `Pry.Core.Prompting.ContextBudgetException : The model window leaves no positive input budget after reserving output and safety.`（加载侧与装配侧换算分家，`ForModelWindow` 直接抛出） | 抛出点 `ContextBuilder.cs:46`，测试点 `ContextWindowLoadTests.cs:52` | M-W3 |
| S2 | `Pry.Api.Tests.StickerContentLengthGuardTests.Content_accompanying_a_sticker_is_rejected_over_the_shared_limit(entry: DirectMessage)` | `Assert.Throws() Failure: No exception was thrown`　Expected `typeof(Pry.Contracts.ApiValidationException)` | `StickerContentLengthGuardTests.cs:58`（经 `:166`） | M-S2 |
| S3 | `Pry.Api.Tests.StickerContentLengthGuardTests.Both_message_entries_call_the_one_ruler_and_no_second_ceiling_is_declared` | `Assert.Equal() Failure: Values differ`　Expected `1`　Actual `0`（本批由另一入口 `AddMessageAsync` 那行触发） | `:121` | M-S2 |

- S3 归属：批1（run4）由 M-S1 命中、批2（run5）由 M-S2 命中，两枚各命中一次＝"同一格的两枚同因"，按 §①.4／§二 另记不合并。
- 本批"实测绿"登记（不顶红账）：`Loading_the_default_profile_at_full_context_yields_a_positive_input_budget`（W1）＝`Passed`（M-CB6 把归一化打进 false→兜底 `output=1`，半窗判据反被满足）；`Every_downgrade_candidate_keeps_a_positive_input_budget`（W2）＝`Passed`（同一 false→兜底机制令每档输入仍为正，两枚合力够不着）；`Content_accompanying_a_sticker_is_rejected_over_the_shared_limit(entry: TurnSubmission)`（S1）＝`Passed`（M-S1 已还原）；`Plain_text_*`、`The_shared_ruler_*`、`Sticker_message_under_the_limit_*` 均绿。
- TRX `<Counters total="278" executed="278" passed="275" failed="3" notExecuted="0">`；3 failed＝上表；无环境跳过；executed=passed+failed=275+3=278=total ⇒ 与 Api 基线 278 对平。

## 七、Api 还原与复绿（`run6-api-green`）

批2 三文件还原，并对载体五份被改文件（`ContextBuilder.cs`／`AttachmentTextExtractor.cs`／`ConversationSessionService.cs`／`ModelPerformancePolicy.cs`／`ConversationApplicationService.cs`）逐一做逐字节复证。

| 项 | 值 |
|---|---|
| 还原方式 | `git checkout -- src/Pry.Core/Prompting/ContextBuilder.cs src/Pry.Api/Services/ModelPerformancePolicy.cs src/Pry.Api/Services/ConversationApplicationService.cs`（批1 的 `ConversationSessionService.cs` 已在进批2 前还原；未用 PS5.1 `Set-Content -Encoding UTF8`）✓ |
| 还原后 SHA-256 逐字节比对（五份，全部 work==HEAD） | `ContextBuilder.cs` `c5da1e3e32775d1da5c1844e9d9227d39657aa2796f1301fd65349c9d5bae3ef`✓；`AttachmentTextExtractor.cs` `8406cfad1d12f890062537e5aebdeb4ef0166c3c88f2151fa1004722681e7ee4`✓；`ConversationSessionService.cs` `6e346ca3aef7930d281471a99d22725a4eb5898e0b4a955bd34847658229ed91`✓；`ModelPerformancePolicy.cs` `3f18ee19e64175fc2141f00b3ff3b2e787f15f6114469dbb2eea5c77337eaf52`✓；`ConversationApplicationService.cs` `b94334acd9b20f635c364e51c0d67a8faa329b85703abfb47ecabf2c549fead0`✓（五值均与 §0.8 入场基线一致） |
| 还原后 `git status --short` 行数 | **0 行**；`git diff --check` 干净 ✓（此时载体全部突变已还原，Core 五格＋Api 五入口都回位） |
| 复绿跑法整行命令 | `dotnet test tests/Pry.Api.Tests -c Release --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave8-1022\run6-api-green" --logger trx`（与红批同命令行；源码已还原，重建出干净 Api） |
| 复绿 TRX 文件名 | `24782_PHYTHLINK_2026-10-22_18_59_33.trx` |
| 复绿 `<Counters>` | `total="278" executed="278" passed="278" failed="0" notExecuted="0"` |
| 六格回归复核 | `ContextWindowLoadTests` 三方法（W1/W2/W3）＋ `StickerContentLengthGuardTests` 的 S1／S2／S3 与 `Plain_text_*`（2 行）／`The_shared_ruler_*`／`Sticker_message_under_the_limit_*` 全部 `outcome="Passed"` |
| 这一向的结论 | **已验证复绿**：还原后 Api 回到在册基线 278／executed 278／passed 278，0 红、0 跳过；Api 两批共 5 枚突变全部逐字节还原，载体 `git status --short`＝0 行。 |

## 八、收尾那一轮与顶账（`run7-full`）

前置＝全部突变已还原、载体 `git status --short`＝**0 行**（§七 已逐字节复证五文件 work==HEAD），HEAD＝`6b6f549`。

### 8.1 解决方案级构建（1 次，硬顶用满）

- 跑法整行：`dotnet build Pry.slnx -c Release`，工作目录＝载体根；构建前 PF-8（见 §九补）＝`VERDICT=BUILD_OK`。
- 结果：`已成功生成。0 个警告，0 个错误`；构建出 Contracts／Client／Core／Api／App ＋ Client.Tests／Core.Tests／Api.Tests／App.Tests 九个产物。

### 8.2 全量（1 次，硬顶用满）

- 跑法整行：`dotnet test Pry.slnx -c Release --no-build --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave8-1022\run7-full" --logger trx`，工作目录＝载体根；跑前 PF-9＝`BUILD_OK`。
- `ls *.trx`＝**4 份**（非 5：控制台列出的四个结果文件即这四份）。解决方案级默认命名跨程序集撞名——`…_19_01_04.trx` 与 `…_19_01_04[1].trx` 同一秒落盘，程序集归属靠文件内 `Pry.<X>.Tests` 前缀逐份判定（下表）。

| TRX 文件名 | 归属（按文件内前缀判） | `<Counters>` |
|---|---|---|
| `24782_PHYTHLINK_2026-10-22_19_01_04.trx` | `Pry.Client.Tests` | total 108／executed 108／passed 108／failed 0 |
| `24782_PHYTHLINK_2026-10-22_19_01_06.trx` | `Pry.App.Tests` | total 233／executed 233／passed 233／failed 0 |
| `24782_PHYTHLINK_2026-10-22_19_01_03.trx` | `Pry.Api.Tests` | total 278／executed 278／passed 278／failed 0 |
| `24782_PHYTHLINK_2026-10-22_19_01_04[1].trx` | `Pry.Core.Tests` | total 527／executed 525／passed 525／failed 0（2 条 `NotExecuted` 见下） |

### 8.3 顶账（出口读数 vs §1／§0.3 在册基线，逐字对平）

| 项 | 出口实测 | 在册基线 | 差 |
|---|---|---|---|
| 总盘 total | 108＋233＋278＋527 ＝ **1146** | 1146 | 0 |
| passed | 108＋233＋278＋525 ＝ **1144** | 1144 | 0 |
| failed（红） | **0** | 0 | 0 |
| 环境跳过 | **2**（按名点名，均在 `Pry.Core.Tests` 名下） | 2 | 0 |
| Client / App / Api / Core | 108 / 233 / 278 / 527（525 executed、525 passed） | 同 | 0 |

- 两条跳过逐名＝`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`、`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`；四份 TRX 内 `outcome="Failed"` 计数＝**0**（`grep -l outcome="Failed"` 无命中）。
- App＝233（非主工作树脏副本的 236）：全量在**载体**（干净 `6b6f549`）上跑，载体内 `tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs` 是 HEAD 的 5 方法版，不含他人工作副本多出的 3 条 ⇒ 本轮无需按方法名做排除；那条排除只适用于"在主工作树脏副本上跑全量"的情形（记忆与工单 §0.1 已注）。
- 受跟踪文件数／catalog 76＝`measure-fixed-eval.ps1` 的仓库级门，不在本轮 `run7` 的 `dotnet test` 命令面内，本轮未起该脚本，故不在此复述其读数（本波零产品代码、零新判据、零门槛改动 ⇒ 测试条数不变已由此全量复证）。
- 出口读数与 §1 那组数**逐字相同**，无条数漂移需写等式解释。

## 九补、预算终账与 PF 全表

### 9补.1 预算终账（硬顶＝工单 §⑥）

| 资源 | 硬顶 | 计划 | 实耗 | 声明 |
|---|---|---|---|---|
| 解决方案级构建 | 1 | 1 | **1**（run7 前 `dotnet build Pry.slnx`，0 错 0 警） | 用满、不超 |
| 全量 | 1 | 1 | **1**（`run7-full`，`--no-build`） | 用满、不超 |
| 定向跑 | 6 | 6 | **6**＝run1-core-red／run2-core-red／run3-core-green／run4-api-red／run5-api-red2／run6-api-green | 用满、不超（W2 够不着未追跑，额度未突破） |
| 单工程构建（另立一册如实记数） | 无上限 | ≤4 | **5 次隐式重建**（`dotnet test` 自带）：run1/run2/run3 重建 Core，run4/run5/run6 重建 Api（含其 Core 依赖）；未单开 `dotnet build <proj>` | 如实记数，均为定向跑的组成部分，非额外构建刀 |
| 焦点预检 | 每次 dotnet 前必跑 | — | **9 次**（PF-1～PF-9，全部 `BUILD_OK`），脚本 v5 | 每次 `dotnet` 前各跑一次，见下表 |

### 9补.2 PF 全表（v5，整段输出要点＋逐次 VERDICT）

| 标号 | 在哪一次 `dotnet` 前 | 焦点窗 | covers/borderless | free_memory_gb | VERDICT |
|---|---|---|---|---|---|
| PF-1 | 入场后第一次（Core 批1 定向前） | `ChatGPT`（`Chrome_WidgetWin_1`，style `0x14C70000` 带标题栏） | False／False | 8.5 | **BUILD_OK** |
| PF-2 | run1 `dotnet test`（Core 批1）前 | `ChatGPT` | False／False | 8.8 | **BUILD_OK** |
| PF-3 | run2 `dotnet test`（Core 批2）前 | `ChatGPT` | False／False | 8.3 | **BUILD_OK** |
| PF-4 | run3 `dotnet test`（Core 复绿）前 | `ChatGPT` | False／False | 7.9 | **BUILD_OK** |
| PF-5 | run4 `dotnet test`（Api 批1）前 | `ChatGPT` | False／False | 7.6 | **BUILD_OK** |
| PF-6 | run5 `dotnet test`（Api 批2）前 | `ChatGPT` | False／False | 7.6 | **BUILD_OK** |
| PF-7 | run6 `dotnet test`（Api 复绿）前 | `QQ`（`Chrome_WidgetWin_1`） | False／False | 7.5 | **BUILD_OK** |
| PF-8 | `dotnet build Pry.slnx`（收尾）前 | `explorer`／`Progman`／`Program Manager` | True／True ⇒ v5 记 `DESKTOP-SHELL-FOCUS (informational only, does not hold)` | 7.3 | **BUILD_OK** |
| PF-9 | run7 `dotnet test --no-build`（全量）前 | `QQ` | False／False | 6.6 | **BUILD_OK** |

- **v4→v5 的实测对照（本波唯一变化的落点）**：PF-8 与上一轮 PF-1 焦点同为"桌面 shell／无边框覆盖全屏"，v4（工单 §9.1 原文）判 `HOLD`，v5（工单 §9.5 立版）在 `DESKTOP-SHELL-FOCUS` 例外下判 `BUILD_OK`；其余八次焦点是带标题栏的普通窗口（ChatGPT／QQ），两版都放行。⇒ 本波拿到 9/9 放行、无一次 HOLD、无停刀。
- 全程无 SQLite `disk I/O`／`out of memory` 形状；`free_memory_gb` 最低 6.6（仍高于 6，未触 WARN 停刀线）。
- 未做被禁的事：没有"等几分钟再试"、没有换跑法绕行、没有改 `game-preflight5-1022.ps1` 的任何判据、没有自造比工单更严的作废条件。

## 十补、够不着的向（不写"不需要拆"）

1. **W2 `ContextWindowLoadTests.Every_downgrade_candidate_keeps_a_positive_input_budget`——本波实测够不着／归属存疑（唯一未拿红的格）**。
   - 事实：批1（run4，单 M-CB1）实测 `Passed`；批2（run5，M-CB1∧M-CB6 同放）实测仍 `Passed`。
   - 根因（读码＋实测一致）：`ContextBuilder.cs:39` 的返回是 `contextTokens > 0 && inputBudget >= MinimumInputTokens`；两枚夹子（`:34` 半窗口、`:35` 的 `- MinimumInputTokens`）同拿掉后，被夹档的 `inputBudget` 跌到 0（< `MinimumInputTokens` 512）⇒ `TryNormalizeWindow` 返回 **false**，`ModelPerformancePolicy.WithContext:47` 落 `:48` 的兜底 `MaxOutputTokens = 1` ⇒ 该档输入预算反为正（`contextSize-1-128>0`）⇒ W2 的"每档输入>0"判据**成立、不响**。上一轮 §1.2 把 W2 预测成"两枚合力才响"的"合力格"，是只看 `:35/:36` 输出被削到 262016、未把 `:39` 布尔门与 `:48` 兜底链算进去的推演，**本轮实测证否**。
   - 处置：按工单 §④.4／§0.5 与派单续跑令——W2 两枚同放仍绿 ⇒ 登记够不着／归属存疑，不为凑红另起配方；定向硬顶 6 已用满（run1～run6），无备用额度做单枚重跑，故不重跑、不追。
   - 这一格"没拿到红"＝**未取到实测红**，不得写成"不需要拆"或"已拆过"；它仍是第六波那把名下待补的一格。
2. **W1 的归属限定**：W1 实测红只在**批1（run4，单 M-CB1）**拿到；批2（run5）加了 M-CB6 后 W1 反回 `Passed`（同一 `:39`→`:48` 兜底机制，见上）。因此 W1 计入实测红（一次，run4），批2 的"预期红而实绿"不是空转、是 M-CB1 与 M-CB6 在**同一函数相邻两行**上的互扰（M-CB6 令归一化早退到 false 分支，覆盖了 M-CB1 对 `outputTokens` 数值的影响）。登记为互扰，不顶红账、也不把批2 的 W1 绿当成"够不着"（它已在批1 响过）。
3. **未拆、且本波按 §二 明令不碰的格**：`ContextBuilder.cs:24` 夹零对应的 `The_raw_equal_output_and_context_window_yields_a_negative_input_budget`（第七波 §4.2 已完整十一项，本波一字不碰）；`StickerContentLengthGuardTests` 的 `Plain_text_*`／`The_shared_ruler_*`／`Sticker_message_under_the_limit_*` 三格不在第八波 18 格点名内 ⇒ 本波不拆（§二已声明，非"不需要拆"）；"上限数字唯一声明"要单拿得改 `ApiContracts.cs:196` 的 `20_000` 本身、会连带 `ResourceLimitLedgerCoverageTests`（另一把名下），属另一把的面 ⇒ 本波不动、不冒充。

## 十一、结论（只写事实，不写状态词／结案词）

- 载体入场（`git status --short`＝0 行、HEAD＝`6b6f549`）当场自核；续跑把 §⑥／§0.7 的焦点预检脚本由 v4 换成 v5，9 次预检全部 `BUILD_OK`、无一次 `HOLD`、无停刀。
- 18 格中**拿到 17 格实测红（18 个红点名）**：Core 组 12 格全红（批1 C1／C2／C3／C5／C6／C7／C8 ＝ 7；批2 C4／A1／A2／A3／A4 ＝ 5）；Api 组 6 格中 5 格拿到红（W1 run4、W3 run5、S1 run4、S2 run5、S3 run4＋run5 两枚同因，S3 两个红点名）；**W2 一格够不着**（两种配方均实测绿，根因见 §十补.1）。
  - 逐格红点＝Core 12 ＋ Api 6（其中 S3 双计）＝18 个红点名；按不同格算＝Core 12 ＋ Api 5 ＝ **17 格**拿到过实测红，W2 未取红。（Api 响过的格集＝{W1, W3, S1, S2, S3}；未响＝{W2}。）
- 每一批红格集合与上一轮 §1.1／§1.2／§二 的读码预测逐格对过：Core 两批与 Api 两批均**无多余红（无脏互扰顶账）**；唯一的"预期红而实绿"是 Api 批2 的 W1（已在批1 响过，属同函数互扰）与 W2（够不着），据实登记、未降低任何断言、未拿脏读数顶账。
- 全部突变逐字节还原：五份被改文件还原后 SHA-256 与 `git show HEAD:<file>` 逐字一致；载体 `git status --short`＝**0 行**、`git diff --check` 干净；未用 PS5.1 `Set-Content -Encoding UTF8`。
- 收尾一轮＝解决方案级构建 1（0 错 0 警）＋ 全量 1（`--no-build`）；`run7-full` 内 `ls *.trx`＝4 份，按文件内 `Pry.<X>.Tests` 前缀归属；出口读数 **total 1146／passed 1144／0 红／2 条按名点名跳过**，分项 Client 108／App 233／Api 278／Core 527（executed 525／passed 525）——与 §1 在册基线逐字相同。
- 本轮未提交、未 `git add`、未 tag／push／打包；主工作树仅在本报告文件续写，他人名下两枚暂存项一字未读未改；作者进程未碰、`%LOCALAPPDATA%/PryCompanion` 未用作测试载体；未新建任何文件当门、未改 `docs/resource-limit-ledger.md`／`.5`／`2400`／五档超时／`MinimumUsableTokensPerSecond = 12`／`4096` 地板。


---

## 十二、守门人复证与一处归属更正（署名＝守门人，非 `wave8b-lane`；id＝`eval-GK-WAVE8B-1022`）

上面 §〇～§十一 是执行 lane 的原文，一字未改。这一节由守门人写并署守门人的 id（不替执行方补写、也不署它的名）。

### 12.1 我对 lane 交付的独立核账（不采信自述，逐份 TRX 自己读）

| 项 | 守门人实测 |
|---|---|
| 证据目录 | `.local-notes\qa-wave8-1022\` 七批齐全（`run1-core-red`～`run7-full`），`.trx` 共 **10 份**（六次定向各 1 份＋收尾全量 4 份） |
| `run1-core-red` | total 527／executed 525／passed 518／failed **7**；文件内 `outcome="Failed"` 的**不同用例名＝7**（与计数器相等）；2 条 `NotExecuted` 逐名＝SenseVoice 与 LlamaServer 那两条 |
| `run2-core-red` | total 527／executed 525／passed 520／failed **5**；不同红名＝5（`An_advisory_beyond_its_reserved_share_fails_loudly_and_is_ledged`＋`AttachmentLimitTests` 四格） |
| `run3-core-green` | total 527／executed 525／passed 525／failed **0** |
| `run4-api-red` | total 278／executed 278／passed 275／failed **3**＝W1（`Assert.True() Failure Expected: True Actual: False`）＋S1（`No exception was thrown`）＋S3（`Expected: 1 Actual: 0`）；**W2 与 W3 该轮实测 `Passed`**（与 §1.2 的"单拆够不着"一致） |
| `run5-api-red2` | total 278／executed 278／passed 275／failed **3**＝W3＋S2＋S3 |
| `run6-api-green` | total 278／executed 278／passed 278／failed **0** |
| `run7-full` | 四份按文件内 `codeBase` 归属＝Client 108／App 233／Api 278／Core 527（525 passed），合计 **total 1146／executed 1144／passed 1144／failed 0**，2 条按名跳过均在 `Pry.Core.Tests.dll` 名下 ⇒ 与在册基线逐字相同 |
| 载体还原 | 五份被改文件（`ContextBuilder.cs`／`AttachmentTextExtractor.cs`／`ModelPerformancePolicy.cs`／`ConversationSessionService.cs`／`ConversationApplicationService.cs`）work 侧 SHA-256 与 `git show HEAD:<file>` **逐字节相同**；`git -C <载体> status --short`＝**0 行**；HEAD 仍是 `6b6f549` |

### 12.2 一处归属错账（lane 写错、守门人核出，就地更正，不涂改它的原文）

§六 把 **W3** 的红写成"M-W3 唯一命中"，并在 §1.2 的批内互斥性里论证"M-CB1／M-CB6 都不改变 `ctx`，也都不改变 `intended/4` 之后的等式"。**这个论证漏算了 `ContextBuilder.cs:39` 那枚布尔返回门。** 盘上那条红的消息形状本身就不是等式不符，而是抛出：

```
Pry.Core.Prompting.ContextBudgetException : The model window leaves no positive input budget after reserving output and safety.
抛出点 ContextBuilder.cs:46（ForModelWindow 里 TryNormalizeWindow 返 false 之后的 throw），测试点 ContextWindowLoadTests.cs:52
```

逐值算术（`DefaultSafetyTokens = 128`、`MinimumInputTokens = 512`，见 `ContextBuilder.cs:16-17`）：批2 同放 M-CB1（`:34` 去掉半窗夹）＋M-CB6（`:35` 去掉 `- MinimumInputTokens`）后，`ForModelWindow(32768, 32768)` 走 `capped = 32640` ⇒ `inputBudget = 32768 − 32640 − 128 = 0` ⇒ `:39` 的 `inputBudget >= 512` 为 **false** ⇒ `:46` 抛出。也就是说**批2 那条 W3 红是 M-CB1∧M-CB6 产生的，M-W3 在那一批里根本没被观察到**（测试在 `:52` 就抛了，`Assert.Equal` 那两行没执行到）。这与 lane 自己在 §十补.1 用来证否 W1／W2 的是同一枚机制——它把这条机制用在了 W1／W2 上，却没有回头修正 W3 的归属。

### 12.3 守门人自证轮：单枚 M-W3 的清洁红（另立一批，如实记在硬顶之外）

| 项 | 值 |
|---|---|
| 载体与提交号 | `.local-notes\qa-tip-1022`，detached `6b6f549`，入场 `git status --short`＝0 行 |
| 被改文件:行 | `src/Pry.Api/Services/ModelPerformancePolicy.cs:47`（**仅此一枚**，M-CB1／M-CB6／M-S2 都不放） |
| before→after | `        if (!ContextBudget.TryNormalizeWindow(contextSize, intended, out var ctx, out var output, out _))` → `        if (!ContextBudget.TryNormalizeWindow(contextSize, intended / 4, out var ctx, out var output, out _))` |
| `git diff --stat`／`-U0` | `1 file changed, 1 insertion(+), 1 deletion(-)`；hunk 头 `@@ -47 +47 @@`，`git status --short`＝**1 行**且只有预期文件 |
| 突变态 SHA-256 | `08fa254f6b57bda4a40a4da61ddf0f4f3022e7d3d750294132da618fe071274c` ＝ lane §六⑨ 记录的同一值 ⇒ 两边放的是**同一字节状态**（可复算） |
| 焦点预检（本轮 2 次，脚本 v5） | 起 `dotnet` 前一次：`FOREGROUND: ChatGPT pid=32760 class=Chrome_WidgetWin_1 style=0x14C70000 … covers=False borderless=False`，`free_memory_gb=7.3`，**`VERDICT=BUILD_OK`**（另一次在改文件之前，同样 `BUILD_OK`） |
| 跑法整行 | `dotnet test tests/Pry.Api.Tests -c Release --results-directory "C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-wave8-1022\run8-gk-mw3-only" --logger trx`（工作目录＝载体根，未加 `--no-build`） |
| TRX | `24782_PHYTHLINK_2026-10-22_19_07_54.trx`（`codeBase` ＝ `…\tests\Pry.Api.Tests\bin\Release\net10.0\Pry.Api.Tests.dll`） |
| `<Counters>` | total 278／executed 278／passed 277／failed **1**／notExecuted 0；文件内不同红名＝**1** |
| 红格全名＋消息 | `Pry.Api.Tests.ContextWindowLoadTests.ForModelWindow_agrees_with_the_loader_on_the_default_profile`＝`Assert.Equal() Failure: Values differ　Expected: 8192　Actual: 16384`，断言行 `tests\Pry.Api.Tests\ContextWindowLoadTests.cs:53` |
| 同轮其他格 | W1／W2／S1／S2／S3 **全绿**（277 passed 里没有它们）⇒ 这一枚只打红 W3，归属干净 |
| 还原 | `git checkout -- src/Pry.Api/Services/ModelPerformancePolicy.cs`（只在载体内、只针对自己那一行）；还原后实测 `3f18ee19e64175fc2141f00b3ff3b2e787f15f6114469dbb2eea5c77337eaf52` ＝ `git show HEAD:<file>` 同值；`git status --short`＝**0 行** |

### 12.4 更正后的账（只改归属口径，不改格子数）

- **18 格中 17 格拿到"归属干净"的实测红**：Core 12 格（批1 七格＋批2 五格，逐枚单因，红集与预期逐格对上、无多响）；Api 侧 W1（run4，M-CB1 单枚）、S1（run4，M-S1）、S2（run5，M-S2）、S3（run4 由 M-S1、run5 由 M-S2 各命中一次＝同格两枚同因，另记不合并）、**W3＝由本轮 `run8-gk-mw3-only` 提供清洁凭证**（不是由批2 那条）。
- **批2 那条 W3 红不作废、改登记为"同一格的第二条因"**：M-CB1∧M-CB6 → `:39` false → `ForModelWindow:46` 抛出。它与 §十补.2 的 W1 互扰同源，属相邻两行突变在同一函数里的合力形状。
- **W2 仍＝够不着**（批1 单枚与批2 合力两种配方均实测 `Passed`，根因是 `:39` 布尔门＋`:48` 的 `MaxOutputTokens = 1` 兜底反而给出正输入预算）。这一格继续欠着，不写成"不需要拆"。
- **当日预算累计（守门人如实并账，不按刀数掩盖）**：解决方案级构建 **1**（lane run7 前那一次；本轮守门人未起解决方案级构建）；全量 **1**（run7）；定向跑 **7**＝lane 用满 6 ＋ 守门人自证 1（这一枚属第七波立的那条规矩："写判据的人自证不算，换人复判由守门人另起一次"）；隐式单工程构建 **6**（六次定向自带）＋**1**（守门人本轮）；焦点预检 **11** 次（lane 9 ＋ 守门人 2），全部 `BUILD_OK`，无一次 `HOLD`、无绕行、`game-preflight5-1022.ps1` 一字未改。
- **未做被禁的事**：零提交（这一节由守门人以文档刀落地，执行 lane 未提交）、零 `tag`／`push`／打包、零产品代码改动（主线 `src/` 与 `tests/` 本轮一字未变）、零断言放宽、零数字门槛改动；他人名下两枚暂存项一字未读未改；作者进程未碰；`%LOCALAPPDATA%/PryCompanion` 未用作载体。
- **加粗机械门的读数（如实登记，不粉饰也不越界去改别人的字）**：`node .local-notes/bold-flank-newonly-1021.js` 对本报告报 `NEW_DEFECTS＝1`，位置＝lane 原文 §十补.2 那一条里的加粗闭合处——闭合星号的前一个字符是全角右括号、后一个字符是汉字，这一形状在作者那页开不了口。本轮守门人只在自己的 §十二 里落字，按"不改执行方原文"的纪律**没有**去重写那一行，把缺陷挂在该文后续负责的分刀上；本轮四处改动文件中的另外三处（工单 §⑨、`docs/plans/PROJECT_PLAN.md`、`docs/full-scope-sprint.md`）实测 `NEW_DEFECTS＝0`，且正向对照（`.local-notes/bold-probe-1021.md`）仍报 `DEFECT_LINES=2`，说明门本身是活的、不是常绿。
