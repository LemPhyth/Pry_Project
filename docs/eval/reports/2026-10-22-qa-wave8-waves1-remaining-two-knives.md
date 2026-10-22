# eval-QA-WAVE8-1022｜第八波两把刀的"拆法原文"增量报告（零产品代码、零新判据、零门槛改动）

- 报告 id：`eval-QA-WAVE8-1022`
- 执行方：守门人自起的冷启动执行 lane（代号 `wave8-lane`），无本轮对话上下文
- 工单：`docs/dispatch-hds-wave8-20261022.md`（§① 四条交付纪律／§② 载体／§③ 两把与判据面／§④ 跑法／§⑤ 起止读数／§⑥ 预算与弹窗／§⑦ 硬约束／§⑧ 交付物）
- **本报告是交付物；读数不是。** 按批次增量落盘：每跑完一批立刻把该批表格追加进本文件再开下一批。
- **状态词与结案词不由执行方写**：本报告只写事实与读数，不写"已结案／已验收／基线已改写"。

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

