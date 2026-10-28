# 2026-10-24｜HDS-007 第一片｜有界连续推进的隔离原型：同入口、冷却/静默、预算、长离线恢复与最小来源约束（第二十九波）

- 工单＝`docs/dispatch-hds-wave29-1024.md`（`HDS-WAVE29-1024`），任务编号＝`HDS-007`（登记处＝`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS-007 行；架构面依据＝v4 第 326 行那一格）。
- 执行方＝第二十九号冷启动 lane（隔离载体，detached @ `8e9c13d`）。**状态词与结案词不由本 lane 写**；本文只算本刀自己的等式并写明出处。
- 交付面＝`src/Pry.Core/Narrative/Advancement/`（新目录）与 `tests/Pry.Core.Tests/` 的新建文件 ＋ 本报告。**零生产调用方；本片不是"有界连续推进"全部，也不含接线。**

## 〇 入场与预检台账（当场取，不引用工单数字）

### 〇.1 必读六份（已逐份完整读完）

| 文档 | 取到的约束 |
|---|---|
| `docs/plans/README.md` | 权威工作规则 14 条；第 14 条把 `docs/agent-engineering-standards.md` 列为准绳；第 9 条的提交前三步（build／test／`git diff --check`）——纯文档刀不伪造读数 |
| `docs/plans/PROJECT_PLAN.md` | §4.2 的 HDS-007 行＝本刀工单行（"第一片进行中／守门人自起 lane"，落点刻意＝`src/Pry.Core/Narrative/Advancement/` 而非 `Runtime/`；产品类型名不带工单编号；费用/预算一律从入参传入）；§4.2 的 HDS-004／005 行写明两者都是**隔离原型、零生产调用方**，且 HDS-004 行明写"延迟意图到期再叙事属 HDS-007 轴"；§9 最新三条 entry＝10-24 16:2x 波 29 出闸、16:0x 波 28 收口（基线顶到 1357）、15:1x 波 28 出闸 |
| `docs/agent-engineering-standards.md` | 全文 10 节＋总原则。交付形态按 §10 四项（问题与根因／实际改动／验证结果／剩余问题）；§7"单元测试全部通过"不能单独作为完成证明，必须区分静态检查／模拟测试／自动化测试／真实环境／人工验收；§3 禁止固定返回值、吞异常、伪造成功、只让单次复现成功；§4 最小充分修改；§9 优先跑针对性检查再决定是否全量 |
| `docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md` | 第 326 行 HDS-007 格（建议 Owner＝Runtime；交付与验收＝"用户事件、到期意图、后台场景同入口；冷却、静默、费用/调用预算、长离线恢复"；前置＝"004/005；006 最小来源约束"）；§13 里程碑 M3"连续角色体验"要求 007/008/009 且"费用、隔离、纠错与取消有证据"；§14.1 那条"普通输入、主动/到期事件都进入同一主叙事契约；不存在第二个后台回复生成器"＝本刀 §一.1 的架构出处 |
| `src/Pry.Core/Memory/Hds006UnifiedMemoryRetrieval.cs` ＋ `Hds006EvidenceIdentity.cs` | **只读语义**：一个入口＋一个入参形状、每条没返回的行带自己的原因、来源三态（`Live`/`Invalidated`/`Missing`）且 Missing 带固定原因文本、`Invalidate` 每次调用让版本号前进一步（撤销的条件之一）、参数全走入参（`ResultLimit` 无默认值）。本刀**不引用它的类型、不复制它的字面量**（含那句 `source_never_registered`）；来源缺失这一面在新链形状上自建（§一.5） |
| `docs/legacy-chain-deprecation.md` | §4 四条要求（处置表／打包形状门／判据账重基线／数据继承），第 2／3／4 项照旧欠；§5 明写新链今天没有生产调用方。**本刀不碰删除片**，也不主张"旧链可删"或"不进包已做到" |

### 〇.2 入场读数（逐条原样，`date` 与 git 均当场取；归档＝`.local-notes/wave29-1024/out/entry-readings.txt`）

```
$ date
Sat Oct 24 16:16:07     2026

$ git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD
2c681a447fe8de28b05fdd7acf4130a10a5f545e

$ git status --short --branch
## codex/ui-main-window-redesign
M  docs/dispatch-tip1020-02-memory-group-judgment.md
M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs
?? .qoder/
?? docs/character-learning-safety.patch
?? docs/research/HDS_INTERLUDE.md
?? docs/research/HDS_QQ_RESEARCH_INDEX.md
?? docs/research/QQ_BRIDGE.md

$ git log --oneline -3
2c681a4 docs(plans,wave29): 预置第二十九波工单 HDS-WAVE29-1024（…在飞 1…）
8e9c13d docs(plan,sprint,eval,wave28): 收第二十八波 HDS-006 第一片…
db37f09 feat(HDS-006,wave28): 归位第二十八波 HDS-006 第一片隔离原型…

$ git worktree list | wc -l
74（入场前）→ 75（本刀建载体后）
```

- 主树线尾＝`2c681a4`（＝工单自身那枚纯文档提交）；工单 §〇.3 指定的基点＝`8e9c13d`。
- 他人资产核对＝上面 2 枚 `M ` 暂存项 ＋ 5 项 `??`：本刀**不读内容、不改、不提交、不 `stash`／`clean`／`rm`**。
- 主工作树零 `git` 写操作（无 `commit`／`add`／`checkout`／`reset`／`cherry-pick`／`merge`／`tag`／`push`／`apply`），本刀只建 1 个 worktree（`git worktree add --detach`，只新增不动任何 ref）。

### 〇.3 载体自证三件事（归档＝`out/carrier-selfproof.txt`）

```
$ git -C .local-notes/qa-wave29-1024/carrier rev-parse HEAD
8e9c13dfd9ad8acc40c227cefac606c27108525a        ← 等于工单 §〇.3 指定的基点全号

$ git -C .local-notes/qa-wave29-1024/carrier status --short | wc -l
0                                                  ← 入场即干净

$ sha256sum src/Pry.Core/Memory/Hds006EvidenceStore.cs   # 在载体里
c5e5892d7f239d98b61389f9d70576354ad76e7e430f1c5ae09fc454269659b4
```

- 这枚 sha256 与第二十八波报告 §八.2／§八.5 那行"还原基准值"逐字符相同 ⇒ 载体基点上的 HDS-006 产品字节＝波 28 归位落地那一枚，未被后续纯文档提交改动（本刀只读语义、一字不碰）。

### 〇.4 在册基线起点（当场从波 28 报告 §八.7 读，不从工单抄）

- 出处＝`docs/eval/reports/2026-10-24-hds006-evidence-identity-retrieval-prototype.md` §八.7"归位与顶账"那行的原文读数：**顶账＝1357／executed 1355／failed 0／跳过 2**（分项 Client 108／App 233／Api 280／Core **736**、Core executed 734）。
- 起点读数＝**1357／executed 1355／failed 0／跳过 2**。
- 本刀新格数 N＝由我自己的新判据文件当场逐标记数（见 §二 顶账），出口读数＝起点＋N，右端必须由收尾那四份原始 TRX 当场求和取出，不用"条数＝断言数"换算。**本刀零轮次、四份 TRX 不存在 ⇒ 出口读数未取**（§三.1／§三.2），起点 1357 在本刀之后仍是波 28 那个数，本刀不动它、不顶它。
- 两条跳过（在册口径，本刀不新增跳过）在波 28 报告按 `outcome="NotExecuted"` 点名＝`Configured_llama_server_requires_the_child_environment_key`／`Configured_sense_voice_model_can_run_offline`；`notExecuted` 计数器恒 0，不看。

### 〇.5 焦点预检台账（每一次 `dotnet` 前必须一枚，整段归档到 `out/preflight-<ID>.txt`）

| 轮次 | 预检文件 | VERDICT | free_memory_gb | 紧随其后的 `dotnet` |
|---|---|---|---|---|
| P1 | `out/preflight-P1.txt` | **HOLD**（16:24:31，当场 `LastWriteTime` 取） | 10.9 | **零次**——本轮不起跑 |
| P2（HOLD 后第 1 次重跑） | `out/preflight-P2.txt` | **HOLD**（16:29:22） | 10.8 | **零次**——本轮不起跑 |
| P3（HOLD 后第 2 次重跑） | `out/preflight-P3.txt` | **HOLD**（16:34:07） | 10.7 | **零次**——本轮不起跑 |
| P4（HOLD 后第 3 次重跑） | `out/preflight-P4.txt` | **HOLD**（16:39:17，距 P3 整 310 秒） | 10.9 | **零次**——本轮不起跑 |
| P5（HOLD 后第 4 次重跑） | `out/preflight-P5.txt` | **HOLD**（16:47:33，距 P4 496 秒） | 10.8 | **零次**——本轮不起跑 |
| P6（HOLD 后第 5 次重跑） | `out/preflight-P6.txt` | **HOLD**（16:52:34，距 P5 301 秒） | 10.9 | **零次**——本轮不起跑 |
| P7（HOLD 后第 6 次重跑） | `out/preflight-P7.txt` | **HOLD**（16:57:35，距 P6 301 秒） | 10.8 | **零次**——本轮不起跑 |
| P8（HOLD 后第 7 次重跑） | `out/preflight-P8.txt` | **HOLD**（17:02:36，距 P7 301 秒） | 10.9 | **零次**——本轮不起跑 |
| P9（HOLD 后第 8 次＝最后一次重跑） | `out/preflight-P9.txt` | **HOLD**（17:07:37，距 P8 301 秒） | 11.1 | **零次**——本轮不起跑 |
| （逐轮追加） | | | | |

**P2 的间隔自我登记（如实，不粉）**：P1 落盘 16:24:31、P2 落盘 16:29:22 ⇒ 间隔 **4 分 51 秒**，短于工单 §〇.7 的"相邻两次不短于 5 分钟"口径 **9 秒**。P2 这枚预检本身仍是 `HOLD`、未起跑任何 `dotnet`，所以没有产生"在不该起跑的窗口里起跑"的后果；但间隔不达标这一条按实记账，后续每一枚重跑前都先读上一枚预检文件的 `LastWriteTime` 算间隔、够 5 分钟才跑（P3 最早 16:34:22）。这枚缺陷登记在 §六 自我记账第 1 条，不销账。

**P3 的间隔同样没达标（第二枚，如实登记）**：我按"P3 最早 16:34:22"排了 70 秒等待，但 `date` 打印 16:34:06、文件落盘 16:34:07 ⇒ 距 P2 只有 **4 分 45 秒**，短 15 秒。起因是等待时长由估算的上一枚时间起算，没有先读盘上 `LastWriteTime` 再算——**这条正是我上一段写的正确做法，我没执行到**。P3 结果仍是 `HOLD`、紧随其后零 `dotnet`，无越窗起跑。P4 起改为：先取上一枚文件的 `LastWriteTime`，再加 300 秒与现值之差作为等待量。两枚间隔缺陷一并登记在 §六 自我记账第 1 条。

P1 整段（16:24:31 当场原样，归档＝`out/preflight-P1.txt`）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: msedge pid=5424 class=Chrome_WidgetWin_1 style=0x160B0000 rect=0,0,2560x1440 screen=2560x1440 covers=True borderless=True title=??????????????10? ??????????_????_bilibili - ??
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=15532, TextInputHost pid=21228, explorer pid=13364
gpu_util=15% used=1738MB
free_memory_gb=10.9
VERDICT=HOLD
  reason: focused window is a borderless full-screen app (msedge)
```

- 读到 `HOLD` ⇒ 不起跑、不请示、不改预检判据、不绕过。本轮之后先做零 `dotnet` 的活：载体提交（`ed7995c` 首交付＋`77a7d91` 判据形状更正＋`663d2c9` 起跑前两处自我纠错，第三枚落在 P2–P4 那三个 `HOLD` 窗口里，见 §六 自我记账第 4/5 条）、七枚突变文本在临时副本上合成并 `sha256sum` 预证（§三.0，线尾变更后已全部重合成）、报告 §一／§二／§三.0／§四／§五／§六 落盘。
- `HOLD` 之后的重跑额度上限＝累计 8 次、相邻两次不短于 5 分钟（本刀第 1 次重跑登记为 P2）。

P2 整段（16:29:22 当场原样，归档＝`out/preflight-P2.txt`；HOLD 后第 1 次重跑，剩余额度 7）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: msedge pid=5424 class=Chrome_WidgetWin_1 style=0x160B0000 rect=0,0,2560x1440 screen=2560x1440 covers=True borderless=True title=??????????????10? ??????????_????_bilibili - ?? 
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=15532, TextInputHost pid=21228, explorer pid=13364
gpu_util=9% used=1724MB
free_memory_gb=10.8
VERDICT=HOLD
  reason: focused window is a borderless full-screen app (msedge)
```

- P2 仍是同一原因（前台是边框less 全屏 msedge，pid 5424 与 P1 同一枚窗口，标题未变）⇒ 依旧零 `dotnet`。P2 之后继续做零 `dotnet` 的活：§三.1 轮次模板、四枚产品文件的旧链名／数字门槛复检、§四 归位配方与 §五 未验证清单先落盘（出口读数那几格留空待跑）。

P3 整段（16:34:07 当场原样，归档＝`out/preflight-P3.txt`；HOLD 后第 2 次重跑，剩余额度 6）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: msedge pid=5424 class=Chrome_WidgetWin_1 style=0x160B0000 rect=0,0,2560x1440 screen=2560x1440 covers=True borderless=True title=??????????????10? ??????????_????_bilibili - ?? 
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=15532, TextInputHost pid=21228, explorer pid=13364
gpu_util=7% used=1723MB
free_memory_gb=10.7
VERDICT=HOLD
  reason: focused window is a borderless full-screen app (msedge)
```

- 三枚预检（P1／P2／P3）读到的都是**同一枚** msedge 进程（pid 5424）、同一份边框less 全屏矩形（`0,0,2560x1440`＝整屏）、同一标题 ⇒ 作者正在看片，这是资源/焦点形状，不是本刀能改的东西。P3 之后做完的零 `dotnet` 活＝§三.1／§三.2 占位、§四（含按路径归位配方与产品面自证复检）、§五（九条未验证）、§六（对照表＋自我记账）、七枚探针与两份还原基准的 `sha256sum` 复证（16:32:22，逐枚与 §三.0 表内值一致）。

P4 整段（16:39:17 当场原样，归档＝`out/preflight-P4.txt`；HOLD 后第 3 次重跑，间隔 310 秒达标，剩余额度 5）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: msedge pid=5424 class=Chrome_WidgetWin_1 style=0x160B0000 rect=0,0,2560x1440 screen=2560x1440 covers=True borderless=True title=??????????????10? ??????????_????_bilibili - ?? 
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=15532, TextInputHost pid=21228, explorer pid=13364
gpu_util=3% used=1703MB
free_memory_gb=10.9
VERDICT=HOLD
  reason: focused window is a borderless full-screen app (msedge)
```

- P4 与前两枚同源（同一 pid／同一矩形）。P4 之后到 P5 之间做的是**逐字节重读产品码与判据**那一轮（不跑 `dotnet`）：查出 §六 自我记账第 4、5 那两条（`BuildReadout` 回显空白来源、冷却格观察日志期望值漏算），落载体第三枚提交 `663d2c9`，再把七枚探针按新线尾重合成＋`sha256sum`、把 §一／§三.0／§四／§六 的数字与哈希全部重取一遍。

P5 整段（16:47:33，归档＝`out/preflight-P5.txt`；HOLD 后第 4 次重跑，间隔 496 秒达标）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: msedge pid=5424 class=Chrome_WidgetWin_1 style=0x160B0000 rect=0,0,2560x1440 screen=2560x1440 covers=True borderless=True title=??????????????10? ??????????_????_bilibili - ?? 
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=15532, TextInputHost pid=21228, explorer pid=13364
gpu_util=3% used=1703MB
free_memory_gb=10.8
VERDICT=HOLD
  reason: focused window is a borderless full-screen app (msedge)
```

P6 整段（16:52:34，归档＝`out/preflight-P6.txt`；HOLD 后第 5 次重跑，间隔 301 秒达标）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: msedge pid=5424 class=Chrome_WidgetWin_1 style=0x160B0000 rect=0,0,2560x1440 screen=2560x1440 covers=True borderless=True title=??????????????10? ??????????_????_bilibili - ?? 
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=15532, TextInputHost pid=21228, explorer pid=13364
gpu_util=3% used=1748MB
free_memory_gb=10.9
VERDICT=HOLD
  reason: focused window is a borderless full-screen app (msedge)
```

P7 整段（16:57:35，归档＝`out/preflight-P7.txt`；HOLD 后第 6 次重跑，间隔 301 秒达标；标题换成了另一支片子但窗口形状未变）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: msedge pid=5424 class=Chrome_WidgetWin_1 style=0x160B0000 rect=0,0,2560x1440 screen=2560x1440 covers=True borderless=True title=???2_????_bilibili - ?? - Microsoft? Edge
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=15532, TextInputHost pid=21228, explorer pid=13364
gpu_util=17% used=1756MB
free_memory_gb=10.8
VERDICT=HOLD
  reason: focused window is a borderless full-screen app (msedge)
```

- P1–P7 七枚预检**同一原因、同一枚 msedge 进程**（pid 5424 全程未换），`free_memory_gb` 一直在 10.7–10.9 的健康区间——拦下来的只有"焦点被全屏占用"这一条，不是机器资源。P5–P7 之间做的零 `dotnet` 活＝§六 自我记账第 4–7 条落盘、§三.0 的 R7 事前论证更正与换法、七枚探针重合成＋`sha256sum`、`out/restore-baselines.txt` 与 `out/product-face.txt` 各追加一版线尾现值、轮次跑法脚本 `.local-notes/wave29-1024/round.py`（只做"覆盖→跑→按 HEAD blob 还原→核 SHA 与 `status`→数 TRX→读 Counters→点失败节点"这一条流水线，不含任何预检之外的自动化）。

P8 整段（17:02:36，归档＝`out/preflight-P8.txt`；HOLD 后第 7 次重跑，间隔 301 秒达标）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: msedge pid=5424 class=Chrome_WidgetWin_1 style=0x160B0000 rect=0,0,2560x1440 screen=2560x1440 covers=True borderless=True title=???2_????_bilibili - ?? - Microsoft? Edge
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=15532, TextInputHost pid=21228, explorer pid=13364
gpu_util=13% used=1739MB
free_memory_gb=10.9
VERDICT=HOLD
  reason: focused window is a borderless full-screen app (msedge)
```

P9 整段（17:07:37，归档＝`out/preflight-P9.txt`；**HOLD 后第 8 次＝额度内最后一次重跑**，间隔 301 秒达标）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: msedge pid=5424 class=Chrome_WidgetWin_1 style=0x160B0000 rect=0,0,2560x1440 screen=2560x1440 covers=True borderless=True title=???2_????_bilibili - ?? - Microsoft? Edge
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=15532, TextInputHost pid=21228, explorer pid=13364
gpu_util=6% used=1742MB
free_memory_gb=11.1
VERDICT=HOLD
  reason: focused window is a borderless full-screen app (msedge)
```

**台账终局（工单 §〇.7 那句"8 次之后仍 `HOLD` ⇒ 带着'本刀零 `dotnet` 起跑'的交付回来，那是合法交付"就是这一格）**：

- 预检共取 9 枚（P1–P9），`HOLD` 9 枚，`BUILD_OK` 0 枚；"HOLD 之后的重跑"用掉 **8/8**，间隔除 §六 自我记账第 1 条那两枚（P2 短 9 秒、P3 短 15 秒）之外全部 ≥301 秒。
- 因此**本刀一次 `dotnet` 都没有起跑**：解决方案级构建 0/1、定向 0/8、解决方案级全量 0/1，盘上没有任何 `bin/`、`obj/`、`.trx` 产物由本刀产生（§〇.6 的"已用"三列全为 0）。
- 没有起跑 ⇒ 没有红／无红读数可交：§三.0 那七枚突变**全部停留在"临时副本已合成、SHA 已登记、未施加到载体产品文件"的状态**；载体里四枚文件自始至终只有 `ed7995c`／`77a7d91`／`663d2c9` 三枚提交写过一次，从未被突变覆盖过，`git status --short` 全程 0 行。
- 没有起跑 ⇒ 出口读数（1357＋23＝1380 那条等式的右端）**未取**，本刀不自封任何在册基线；起点 1357 仍只成立于波 28 §八.7 那四份 TRX 的当场求和（§〇.4）。
- 未越窗、未绕过、未改判据：9 枚 `HOLD` 里我没有动过预检脚本一个字节、没有停过任何进程、没有在 `HOLD` 窗口内跑过哪怕一枚 `dotnet --info`。


- 口径按第二十八波 §七.2／守门人 §八.4.2 通用化那条执行：**预检本身不设上限，只有"HOLD 之后的重跑"设 ≤8 次、相邻两次不短于 5 分钟**；`HOLD` 时不起跑、不请示、不改判据、不绕过，先把零 `dotnet` 的活做完。

### 〇.6 本刀预算与实际用量（用满即停）

| 面 | 上限 | 计划 | 已用 |
|---|---|---|---|
| 解决方案级构建 | ≤1 | 1（B1） | **0** |
| 解决方案级全量 | ≤1 | 1（F 收尾） | **0** |
| 定向 | ≤8 | G0 绿基线＋R1–R6＝7 枚，第 8 枚给 R7 或复取 | **0** |

- 三枚"已用"都是 0，且**按盘上产物核**：`.local-notes/wave29-1024/` 下 `.trx` 0 枚、`trx-*` 结果目录 0 个、载体里 `bin`/`obj` 0 枚（17:08:48 当场数，命令与输出原样贴在 §三.1）。没起跑的原因不是额度不够，是九枚预检全 `HOLD`（§〇.5 台账终局段）。
- 跑法固定写法＝`dotnet test <目标> -c Release [--no-build 仅收尾全量] --results-directory "<绝对路径>" --logger trx`；每轮独立结果目录、不复用；跑完先 `ls *.trx` 数枚数再读 `<Counters>`。突变轮一律不带 `--no-build`；收尾全量按波 28 §七.3／§八.4.3 口径：只有在它之前完成过一次重建才带 `--no-build`，否则让它自行增量重建。

### 〇.7 文件面与命名口径（本刀自我约束）

- 只新建：`src/Pry.Core/Narrative/Advancement/**`（新目录）＋ `tests/Pry.Core.Tests/` 一枚判据新文件；既有文件零字节持久化改动。
- **新产品类型名不带工单编号**（用领域名：`Advancement*`／`DueIntent*`），不写 `Hds007*`。
- 新文件里不出现旧链类型名（`ReplyPlanner`／`IdleBackoffController`／`TurnGates`／`ReplyGate`／`CharacterIntelligenceGateway`）；源码扫描格自带阳性探针。
- 原型内不写任何数字门槛、不把冷却/静默/预算的默认值写成源码常量（全走入参，参数无默认值）。

## 一 这一片造了什么（工单 §一 五条逐条对形）

载体＝`.local-notes/qa-wave29-1024/carrier`（detached @ 基点 `8e9c13d`），三枚载体提交：

- **`ed7995c5c61bc8277f90cc88ee8e78bc46e9b20b`**＝4 枚新文件首交付；
- **`77a7d918b609b93e168edb8b8bb35893e95c5a51`**＝判据文件一枚断言形状更正（把"不重复也不漏"那格的补投集合改成与顺序无关的排序断言，顺序归因留给"登记顺序"那格，产品三文件一字未动）；
- **`663d2c9d78d7f607137f10cd2af09069b0a4f841`**＝**线尾**，起跑前两处自我纠错（`ed7995c` 之后、任何 `dotnet` 之前，全程零 `dotnet`）：① 协调器 `BuildReadout` 不再把不可指认的那段空白回显成 `SourceId`（来源缺失时读数里也没有来源，§一.5 的 006 面才自洽，判据 `An_advancement_whose_source_cannot_be_pointed_to…` 里那句 `Assert.Null(blankSource.SourceId)` 也才有成立语义）；② 冷却那格的观察日志期望值补上第一次生效那行（原判据漏算，会在绿基线轮虚报红）。两条都记在 §六 自我记账第 4、5 条。

线尾三枚产品文件与判据文件的 blob SHA-256（`git cat-file blob HEAD:…`，16:43:27 当场）见 §三.0 末段与 `out/restore-baselines.txt`；§三.0 那七枚探针在 16:43 按线尾字节**全部重新合成并重取 `sha256sum`**（`DueIntentRegistry.cs` 没被这三枚提交动过，所以 R5 的预合值与首版一致）。

文件面＝**4 枚新建，0 枚既有文件修改**：

| 新文件 | 行数 | 管哪一条 | 关键形状 |
|---|---|---|---|
| `src/Pry.Core/Narrative/Advancement/AdvancementContracts.cs` | 147 | §一 全五条的入参与读数 | `AdvancementTriggerKind{UserEvent,DueIntent,BackgroundScene}`；`AdvancementOutcome{Advanced,DeferredByCooldown,DeferredByQuiet,StoppedByBudget,RejectedForStaleEffect,FailedForMissingSource}`（六值各有独立原因文本，没有"什么都不说"的出口）；`AdvancementWindow(NowUtc,CooldownWindow,QuietWindowStartUtc,QuietWindowEndUtc)`、`AdvancementBudget(TotalCalls,ConsumedCalls,TotalCost,ConsumedCost,RequiredCallsPerAdvancement,RequiredCostPerAdvancement)` **全部由调用方给值、无默认值**；`AdvancementRequest` 三类触发各带自己的引用字段 |
| `src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs` | 172 | §一.1／§一.2／§一.3／§一.4 迟到面／§一.5 | 唯一公开入口 `Advance(AdvancementRequest)`；闸序＝来源可指认 → 冷却 → 静默 → 预算（调用维度、费用维度各自的 `<` 比较）→ 推进代次 CAS；读数点名 `Kind`/`TriggerReference`/`DeferredBy`/`EarliestNextAdvanceUtc`/`ConsumedCalls`/`RemainingCalls`/`ConsumedCost`/`RemainingCost`/`StoppedBudgetDimension`/`ExpectedEffectVersion`/`ObservedEffectVersion`；被推迟的意图进 `PendingDeferrals`，成功后离表；两个失败出口各留一行观察日志；`BuildReadout` 里来源不可指认时 `SourceId`／`SourceVersion` 一律给 `null`（`663d2c9`） |
| `src/Pry.Core/Narrative/Advancement/DueIntentRegistry.cs` | 87 | §一.4 补投面 | `RecoverAfterClockJump(jumpedToUtc)`：`DueUtc <= jumpedToUtc` 且此前没补投过的，按 `RegisteredSequence` 小→大逐条带走并当场标已补投；没到点的进 `StillPending`；`Registrations` 保留全部登记（不漏）；重复登记同一个意图键＝抛明确异常，不静默合并 |
| `tests/Pry.Core.Tests/BoundedAdvancementPrototypeTests.cs` | 494 | §二 | 23 枚节点（见 §二 顶账） |

五条逐条对形：

1. **三类触发同入口**：三类只是 `AdvancementRequest.Kind` 的三个取值，共用 `Advance` 一个方法与一个入参形状；读数 `Kind`＋`TriggerReference` 点名"这一次是哪一类"。区分性＝夹具把三条引用字段同时给值（`ue-1`／`due-1`／`bg-1`），本类那条才被点名，探针各钉自己的绝对字面量；"没有第二条入口"由反射格钉（协调器上除属性访问器外只有一枚公开实例方法，名字逐字＝`Advance`）。
2. **冷却与静默各自可观察**：冷却用 `cooldownOpensAt > NowUtc`（窗口外沿那一瞬算放行），推迟读数 `DeferredBy = "cooldown"` ＋ `EarliestNextAdvanceUtc = 上次生效时刻 + 入参冷却窗口`；静默用 `NowUtc >= start && NowUtc < end`，推迟读数 `DeferredBy = "quiet"` ＋ 最早下一次＝窗口终点。两者都进 `PendingDeferrals`（推迟不是丢弃＝这张清单当场可数），成功后才离表。
3. **费用／调用预算＝从入参传入**：`remainingCalls < RequiredCallsPerAdvancement` 与 `remainingCost < RequiredCostPerAdvancement` 两道各自拦，读数点名 `StoppedBudgetDimension`（`calls`／`cost`）、已用与剩余；"预算为零"与"预算耗尽"在原型里不是两个分支而是同一道比较的两种入参取值，区分落在观察日志形状上（零预算那把从头到尾没有一行 `Advanced`）。本文件没有任何窗口/预算的默认值。
4. **长离线恢复**：登记处按 `RegisteredSequence` 顺序补投（不是到期时刻顺序——夹具故意让两者不同形）；同一次调用既补投也标已补投，第二次前跳不会再带走同一条；没到点的仍进 `StillPending`。迟到面＝推进代次 CAS：带着已被取代的 `ExpectedEffectVersion` 进来一律 `RejectedForStaleEffect`，原因文本 `advancement_never_took_effect`，且零写回（记录表与代次都不动）。
5. **006 最小来源约束**：`Source is null || IsNullOrWhiteSpace(SourceId)` ⇒ `FailedForMissingSource` ＋原因 `source_cannot_be_pointed_to`，并且**在观察日志留行**——"静默丢弃"在这份原型里没有出口；成功路径把来源标识与版本原样带进产出记录。本片不引用波 28 的任何类型、不复制它的任何字面量（含那句 Missing 原因文本），来源约束在新链形状上自建。

零生产调用方（当场 grep，非断言；归档＝`.local-notes/wave29-1024/out/zero-caller-greps.txt`）：

```
$ grep -rln "AdvancementCoordinator\|DueIntentRegistry" src/            # 只列出本刀自己的 2 枚新产品文件
src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs
src/Pry.Core/Narrative/Advancement/DueIntentRegistry.cs

$ grep -rln "AdvancementCoordinator\|DueIntentRegistry\|Narrative.Advancement" src/Pry.Api src/Pry.App src/Pry.Client src/Pry.Contracts | wc -l
0

$ grep -rln "Narrative.Advancement" tests/
tests/Pry.Core.Tests/BoundedAdvancementPrototypeTests.cs
```

本片**没有落表、没有接 004/005 的编排面、没有端点、没有界面**——工单 §〇 那句"本刀同样是原型对原型"照记。

## 二 判据与逐文件格数

当场逐标记数（`tests/Pry.Core.Tests/BoundedAdvancementPrototypeTests.cs`，注释里不写方括号属性名，数出来的就是真属性行）：

```
$ grep -c '^    \[Fact\]'        # 17
$ grep -c '^    \[Theory\]'      # 2
$ grep -c '^    \[InlineData'    # 6
$ grep -c 'public void'          # 19（17 枚 Fact 方法 ＋ 2 枚 Theory 方法）
```

| 归属 | 形状 | 节点数 |
|---|---|---|
| §一.1 | 1 枚 Theory 展开 3 条区分性探针（三类触发各钉自己的引用字面量＋记录里的身份）＋ 1 枚 Fact（协调器只有 `Advance` 一枚公开入口方法） | 3 ＋ 1 ＝ 4 |
| §一.2 | 4 枚 Fact（冷却窗口内推迟且点名最早下一次；窗口外沿那一瞬放行；静默期推迟且意图仍在待观察清单；闸门打开后补推并离表） | 4 |
| §一.3 | 4 枚 Fact（调用维度耗尽点名已用/剩余/维度；预算为零是另一种形状；费用维度单独拦；恰好花完剩余预算仍放行） | 4 |
| §一.4 | 3 枚 Fact（按时钟前跳按登记顺序补投；两次跳表不重复也不漏；没到点的仍待投）＋ 1 枚 Fact（迟到推进被拒且零写回） | 4 |
| §一.5 | 1 枚 Fact（来源缺失即明确失败并留在观察日志；同一格内同时试 null 与全空白两种来源）＋ 1 枚 Theory 展开 3 条探针（三类触发都在同一道来源闸上被拒）＋ 1 枚 Fact（成功产出把来源带进记录） | 1 ＋ 3 ＋ 1 ＝ 5 |
| 附带约束 | 2 枚 Fact（源码扫描：不出现旧链五个类型名；不声明任何 `const`／`static readonly` 的窗口或预算默认值；两格各带阳性探针） | 2 |
| **合计** | 17 Fact ＋ 2 Theory×3 | **N＝23** |

- 一条用例里的多枚断言**不记成多格**：23 是按属性行与 `InlineData` 行逐行数出来的，不是按断言数。
- 出口读数等式＝起点 **1357**（§〇.4 从波 28 报告 §八.7 当场读）＋ **23** ＝ **1380**，预期唯一去处＝Core 736 → 759；右端必须由收尾那四份原始 TRX 当场求和取出、登记在 §三.1 最后一轮——**本刀没拿到任何 `BUILD_OK` 窗口，这一枚右端未取**（§三.1 零轮次／§三.2 终账），等式只作为下一把的目标登记，不进任何在册数。
- 常绿形状自查（写完每一格先自查，再按 §三 逐枚取红）：
  - 没有"对空集合判不含某元素"的格。两处 `Assert.Empty` 判的是**应当没有产出的实体清单**（`ProducedRecords`／`PendingDeferrals`），并且同格另有存在性断言（观察日志 joined 字面量、`Assert.Single`）托住，摘掉写入就会响。
  - `The_advancement_coordinator_exposes_exactly_one_public_entry_method` 判的是反射取到的方法名联合串 == `"Advance"`——往产品文件加一枚公开方法它就红，不是恒真形状。
  - 两枚源码扫描格**自带阳性探针**：同一枚扫描函数被要求在一枚临时坏样本上逐字数出 `CharacterIntelligenceGateway,IdleBackoffController,ReplyGate,ReplyPlanner,TurnGates` 与 `const,static readonly`；扫描函数坏掉（恒返空）时探针先红。

## 三 反证（定向 ≤8）

### 三.0 事前论证（工单 §三：恰 1 枚红，除非事前点名"必然连带响哪几枚、为什么"）

七枚突变全部只改本刀自己新建的产品文件（`AdvancementCoordinator.cs`／`DueIntentRegistry.cs`），一次一枚一枚轮。**落笔载体之前**已在临时副本上按同一改法合成并 `sha256sum` 记号（第二十七波前置），副本留在 `.local-notes/wave29-1024/probe/`，预合值当场登记：

| 轮 | 文件:行 | 改法 | 副本 SHA-256（预合值） | 事前论证的期望红集 |
|---|---|---|---|---|
| R1 | `AdvancementCoordinator.cs:133` | `BackgroundScene => request.BackgroundSceneReference` → `=> request.DueIntentReference`（本类引用读错条＝三类触发成别名） | `7f207e75dde25d7e2164688f7fd76fe316df82f632dbe4c98c4f9b199616cc75` | **恰 1 枚**＝`Each_trigger_kind_enters_the_one_advancement_entry_and_names_its_own_reference(asked: BackgroundScene, expectedReference: "bg-1")`。另两条探针走未被改的臂；`Missing_source_is_refused_for_every_trigger_kind` 不判 `TriggerReference`；§一.2 那两枚静默格判的是 `due-1` |
| R2 | `AdvancementCoordinator.cs:59` | `cooldownEnd > request.Window.NowUtc` → `cooldownEnd >= ...`（冷却窗口外沿那一瞬被算进窗口＝方向类突变，不新增门槛） | `1ac7ad8e52a9f3080bf85329fb71aa26f9c0ea2e062ba3faeb00110c1ba86549` | **恰 1 枚**＝`An_intent_resubmitted_at_the_instant_the_cooldown_window_closes_advances_again`。窗口内的格本来就推迟；其余格的第二次请求都严格晚于上次生效时刻且冷却窗口传 `TimeSpan.Zero`（`Noon >= 12:05` 为假），不串味 |
| R3 | `AdvancementCoordinator.cs:78` | 删掉静默分支的 `_deferrals.Add(new AdvancementDeferral(... "quiet", quietEnd));`（推迟变成悄悄不见） | `9602e82e340c3a70227b4f152f5ebd34b68da27301f8d752e0da6bc0880abeee` | **恰 1 枚**＝`A_due_intent_inside_the_quiet_window_is_deferred_until_the_window_closes_and_stays_observable`（`Assert.Single(PendingDeferrals)` 读到空）。`A_quiet_deferred_intent_advances_once_...` 断的是"补推后离表"，本来期望空 ⇒ 仍绿 |
| R4 | `AdvancementCoordinator.cs:85` | `remainingCalls < request.Budget.RequiredCallsPerAdvancement` → `<=`（恰好花完剩余预算被误判成耗尽） | `d08e4c644fdf21d91b39b1c6ddaf8051b7b26c669e6e1490b74b3b7a4dcf6a70` | **恰 1 枚**＝`An_advancement_that_exactly_consumes_the_remaining_call_budget_is_allowed`。其余夹具的剩余调用要么远大于必需（`10`、`3` 对 `1`）要么本来就为 `0`（零预算与耗尽两格期望的就是拦），费用维度走另一道比较不受影响 |
| R5 | `DueIntentRegistry.cs:69` | 补投清单 `.OrderBy(...RegisteredSequence)` → `.OrderByDescending(...)`（方向类突变） | `32d41cf573b2c1a8f3a753f1f8982f09400feba19819e43af1309d19ab221156` | **恰 1 枚**＝`After_a_clock_forward_jump_due_intents_are_replayed_in_registration_order`。`Replay_delivers_each_due_intent_exactly_once_...` 的补投集合断言在 `77a7d91` 已改成排序无关形状（线尾判据字节），`StillPending` 清单只有一条、`Registrations` 用的是另一行 OrderBy |
| R6 | `AdvancementCoordinator.cs:50-51` | 删掉来源缺失分支的观察日志 `Add`（结局仍返回但不再留行＝静默丢弃的形状） | `af66ff565013e806f31c76a213d0940371cd011a9721b17426c690fe9d33953c` | **恰 1 枚**＝`An_advancement_whose_source_cannot_be_pointed_to_fails_with_a_named_reason_and_stays_in_the_log`（观察日志 joined 期望两行、实际空串）。三条来源探针不判日志 |
| R7（备用第 8 枚定向） | `DueIntentRegistry.cs:68` | 补投筛选 `x.Registration.DueUtc <= jumpedToUtc` → `x.Registration.DueUtc < jumpedToUtc`（到点那一瞬不算到点＝方向类突变，`<=`↔`<`，与 R2／R4 同族） | `970d3628f4ee8481897b6c1f80dc893f2700f43f2978809398932fd167ad7bec` | **恰 1 枚**＝`A_due_intent_that_has_not_reached_its_due_instant_is_left_pending_and_still_reported`（第二次跳到 `due` 那一瞬 `Assert.Single(atDue.Replayed)` 读到空）。顺序格那三条到点时刻（9/7/8 点）都严格早于 `Noon`，改法不动它们；"不重复也不漏"那格两次跳表（`Noon`、18:00）对 9:00／10:00 仍成立 |

- **R7 换法的事前更正**（落笔载体之前重推，不是事后追认）：首版 R7 计划删 `DueIntentRegistry.cs:73` 的 `entry.Replayed = true;`，本表首版写的"恰 1 枚红"**是事前论证里的实质错误**——那一行同时被三枚格的判据吃到：`Replay_delivers_each_due_intent_exactly_once…`（第二次跳表重复补投）、`After_a_clock_forward_jump_due_intents_are_replayed_in_registration_order`（`StillPending` 多带三条）、`A_due_intent_that_has_not_reached_its_due_instant…`（`atDue.StillPending` 不为空）＝一次响 3 枚；首版论证只查了第一次跳表、漏了第二次。现在换成同族但单点的 `<=`→`<`（上表 R7 行）。首版探针字节没删，留在 `probe/unrun-replay-mark-removed.cs`（`cdd18e3f94896756d37ea4d2df5b7ce289dbee8ed45ca1e4346759bafb2feba0`），**本刀不跑它**：8 枚定向额度给了 G0＋R1–R7 就没有复取位，三响的突变押给下一把或守门人（要跑就按"必然连带响上述三枚"事前点名）。
- 未列入本轮取红的格（照实登记、押下一把，不为了让它响去改判据）：`The_advancement_coordinator_exposes_exactly_one_public_entry_method`（红方向＝往产品文件加一枚公开入口方法，形状上是新增而非摘闸，本刀定向额度优先给五道闸）、`A_late_advancement_carrying_a_superseded_effect_version_is_refused_and_writes_nothing`（红方向已穷举：`request.ExpectedEffectVersion != _effectVersion` 改成自比较即可，属波 28 守门人 GK-R10 同族；额度只够七枚，这枚押下一把或守门人补拿）、两枚源码扫描格（红方向＝往产品文件塞旧链名或 `static readonly` 默认值，本刀不自残产品码取红，探针已证扫描面能数到）。
- 额度：定向 ≤8 ＝ G0 绿基线 ＋ R1–R6 六枚单放突变 ＝ 7 轮，留 1 枚（若六枚红集与事前论证逐字相符，第 8 枚用给 R7；形状不符则优先复取不符的那枚）。解决方案级构建 ≤1、全量 ≤1 各一次。
- 每轮跑法固定写法＝`dotnet test <目标> -c Release [--no-build 仅收尾全量] --results-directory "<绝对路径>" --logger trx`；每轮独立结果目录；跑完先 `ls *.trx` 数枚数再读 `<Counters>`；突变轮一律不带 `--no-build`。
- **单枚性预检**（零 `dotnet`，16:32 起做过三次，最近一次 16:43:40 在 `663d2c9` 的字节上重新合成；七枚探针与两份还原基准的 `sha256sum` 逐枚对上本表值，`base-coordinator.cs` 与线尾 blob `90947162…` 是同一字节）：`diff -u` 探针与还原基准，每枚都只有一个 hunk、只动一条语句——R1/R2/R4/R5 各 1 行改 1 行（`-`/`+` 各 1），R3/R7 各删 1 行（`-` 1 行无 `+`），R6 删的是同一条语句的 2 个物理行（`Add(…,…` 换行书写，逻辑上仍是 1 枚赋值/调用）。`base-coordinator.cs`／`base-registry.cs` 两枚还原基准的 `sha256sum` 与载体 HEAD blob 完全一致（`a7d457cc…`／`9bf4dc31…`），所以"逐字节还原"这条在跑之前就已经可核。

- ⑩ 的"还原后应为"值按第二十六波更正品径＝**该文件在载体 HEAD（线尾 `663d2c9`）的 blob**，当场 `git cat-file blob HEAD:<path>` 取（R1–R4、R6 目标文件＝`AdvancementCoordinator.cs`；R5、R7 目标文件＝`DueIntentRegistry.cs`；三版取值都在 `out/restore-baselines.txt`）：
  `AdvancementCoordinator.cs`＝`90947162fed797f8de98bd5130a11dfacc25886bfa035182dccaf395b9d32b73`（16:43:27 重取；线尾 `77a7d91` 的旧值 `a7d457cc…` 因 `663d2c9` 改了 `BuildReadout` 而作废，两枚都留在归档里作变更史）
  `DueIntentRegistry.cs`＝`9bf4dc31bd3d1c724a804f2dc01334806dada6a1c88861eb07f89fc0897ce5a1`（三枚提交都没动它，值自 `ed7995c` 起未变）
  `AdvancementContracts.cs`（非突变目标，只登记）＝`259474dcccbfb1f0327d92ba5c49e1e8ee35280c16abf6761088332695976ac0`（同上未变）
  判据文件＝线尾 `663d2c9` 的 blob `1fd944f94d399a8f742482c1f724f8afec150eac865cf06cdfa149d4926a200b`（本刀起跑后对 `tests/**` 零写入，所以线尾值就是运行期的判据字节）

### 三.1 轮次记录（十一项字段逐轮追加）

**本刀零轮次。** 工单 §〇.7 的 8 枚"HOLD 之后重跑"额度在 P9（17:07:37）用尽，九枚预检全部 `HOLD`、零枚 `BUILD_OK`，所以没有 B1、没有 G0、没有 R1–R7、没有收尾全量 F——十一项字段无从取，因为**没有一轮可以填**。盘上凭据（17:08:48 当场数）：

```
$ ls .local-notes/wave29-1024/out/          # 14 份，其中 9 份是 preflight-P1..P9.txt
carrier-selfproof.txt  entry-readings.txt  preflight-P1.txt … preflight-P9.txt
product-face.txt  restore-baselines.txt  zero-caller-greps.txt

$ find .local-notes/wave29-1024 -name "*.trx" | wc -l
0                                            ← 没有任何 TRX 产物

$ find .local-notes/qa-wave29-1024/carrier -maxdepth 4 -type d \( -name bin -o -name obj \) | wc -l
0                                            ← 载体里连构建输出目录都没生成过
```

所以"本刀 `dotnet` 调用次数＝0"不是自封，是三行数出来的：`.trx` 0 枚、`bin`/`obj` 0 枚、结果目录 0 个（`out/` 下没有 `trx-*` 目录）。突变轮十一项字段里的 ⑩（突变态与还原后 SHA-256）在本刀只有单向证据：七枚探针的**突变态** SHA 已在 §三.0 逐枚登记（临时副本上合成，16:43:40 在 `663d2c9` 字节上重合成），**还原后** SHA＝载体线尾 blob 三枚现值（§三.0 末段）——但这一枚字段的本意是"跑过之后还原"，本刀没跑，就不算反证轮，只算起跑前的准备工作。

> ← **下一把的第一轮记录写在这里**（B1／G0／R1–R7／F 各占一段，十一项字段齐全才算一轮；缺 ⑨⑩⑪ 按第二十二波口径退回）。

### 三.2 反证汇总（有红／无红终账）

| 面 | 数额 |
|---|---|
| 本刀新建判据格数 N | **23**（§二 顶账；`[Fact]` 17 ＋ `[Theory]` 2 × `InlineData` 3） |
| 已取到红的格 | **0**（零轮次，一枚都没有 TRX 凭据） |
| 未取到红的格 | **23＝全部**，照实登记、押下一把；不是"结构上红不了"，是**本刀没有起跑** |
| 已事前论证、探针已合成且 SHA 已登记的突变 | 7 枚（R1–R7，§三.0 表；每枚单点归因到一枚格，R5 的单点性靠 `77a7d91`，R7 靠 §三.0 顶部那次事前换法） |
| 已合成但**故意不跑**的突变 | 1 枚（`probe/unrun-replay-mark-removed.cs`，事前重推发现一次响 3 枚，理由与三枚格名写在 §三.0 顶部） |
| 预检 | 9 枚，全 `HOLD`；重跑额度 8/8 用尽 |
| `dotnet` | 构建 0/1、定向 0/8、全量 0/1（盘上产物为准，见 §三.1） |
| 出口读数 | **未取**。等式右端（应为 1357＋23＝1380、Core 736→759）留给拿到 `BUILD_OK` 的那一手，本刀不顶账、不自封基线 |

## 四 归位通道（给守门人，不在本 lane 权限内）

本 lane 在主工作树零 `git` 写操作（无 `commit`／`add`／`checkout`／`reset`／`cherry-pick`／`merge`／`tag`／`push`／`apply`），交付面全在载体里：

- 载体＝`C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave29-1024/carrier`，基点 `8e9c13d`（detached），三枚提交：`ed7995c…`（四枚新文件首交付）、`77a7d91…`（判据断言形状更正，使 R5 的红只落一枚格）、`663d2c9d78d7f607137f10cd2af09069b0a4f841`（**HEAD**，起跑前两处自我纠错：`BuildReadout` 不回显不可指认的来源＋冷却格观察日志期望值补全）。
- **归位按路径做**（工单 §四：别写 `git merge --ff-only`，本刀基点在入场后很可能已不是线尾）：
  ```
  git checkout 663d2c9d78d7f607137f10cd2af09069b0a4f841 -- \
    src/Pry.Core/Narrative/Advancement/AdvancementContracts.cs \
    src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs \
    src/Pry.Core/Narrative/Advancement/DueIntentRegistry.cs \
    tests/Pry.Core.Tests/BoundedAdvancementPrototypeTests.cs
  git commit --only -- src/Pry.Core/Narrative/Advancement/AdvancementContracts.cs \
    src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs \
    src/Pry.Core/Narrative/Advancement/DueIntentRegistry.cs \
    tests/Pry.Core.Tests/BoundedAdvancementPrototypeTests.cs
  ```
  共享脏树里必须 `--only` 显式列这四枚路径，否则会把他人名下两枚 `M ` 暂存项卷进提交（本刀没有读过它们的内容）。
- **产品面自证**（首版 16:27、线尾更正版 16:44 各取一次，两版都归档＝`out/product-face.txt`）：`git diff --stat 8e9c13d..HEAD -- src tests scripts Pry.slnx Directory.Build.props` 只出现上面四枚新建文件（`AdvancementContracts.cs` 147 行／`AdvancementCoordinator.cs` 172 行／`DueIntentRegistry.cs` 87 行／`BoundedAdvancementPrototypeTests.cs` 494 行，共 **900 行新增、0 行删改**——四枚全是 `+`，没有任何既有文件被改一行）；`git diff --check 8e9c13d..HEAD` 退出码 0；载体 `git status --short` ＝ **0 行**；`git log --oneline 8e9c13d..HEAD` ＝ 上面那三枚。
- 旧链名／工单编号名复检（零 `dotnet`，16:30 当场）：`grep -n "ReplyPlanner|IdleBackoffController|TurnGates|ReplyGate|CharacterIntelligenceGateway|Hds006|UnifiedMemory" src/Pry.Core/Narrative/Advancement/*.cs` ＝ 零命中；`grep -n "Wave29|1024|HDS007|Hds007"` ＝ 零命中；`grep -n "const |static readonly"` ＝ 零命中；带数字的行只有注释（引用工单条目号与 v4 行号）和 `checked(_effectVersion + 1)`（版本自增，不是门槛）。
- **载体终态（17:08:48 当场取）**：HEAD＝`663d2c9d78d7f607137f10cd2af09069b0a4f841`，`git status --short` ＝ **0 行**，`git diff --name-only` ＝ 空；四枚文件与三枚提交就是 §一 那张表，**七枚探针从头到尾没有覆盖过载体里的任何产品文件**（一次都没起跑，所以没有"突变态没还原"的风险面）。归位前守门人若想让本刀的格进在册基线，需要自己跑一次构建＋定向＋全量（本刀的额度与预检都已作废到"下一把"名下，见 §三.2 与末段"下一手"）。
- 交给守门人核的四件事（工单 §四）在本报告里的落点：① 红格归属逐枚点名到 TRX 的 `outcome="Failed"` 节点＝§三.1 各轮 ⑦ 项；② 常绿门自查＝§二末段；③ 收尾四份 TRX 先数后读＋求和＝§三.1 末轮（在册基线由那一把顶，本刀不自封）；④ §一 五条各有一枚会响的格＝§一 对形表＋§三.0 的红集归属。

## 五 未验证（逐条点名，不用"已完成"混过去）

0. **本刀 23 枚新格全部零读数**：九枚预检全 `HOLD`、`dotnet` 起跑 0 次（盘上产物为准，§三.1），所以这 23 枚格**一枚都没有执行过**，更谈不上有红／无红；§三.0 那七枚突变只是"探针已合成＋SHA 已登记"的准备状态，**不构成任何反证证据**。这条排在最前，是因为下面 1–9 条在"零起跑"的刀面前全部自动成立、不能拿来当交付面。相应地：出口读数未取、在册基线未顶（等式右端留给下一把）、`measure-fixed-eval.ps1` 未跑、`bin`/`obj` 未生成、载体里没有一行由构建产生的产物——**这些"没做"不是取舍，是预检闸拦下来的**。
1. **真实模型对照**：零读数。本刀没有调过任何模型端点，`Advance` 的产出记录只是内存里的形状；要跑对照需作者一句"可以跑"。
2. **生产接线**：本片**零生产调用方**（§一 末尾的 grep 三项：四枚新文件在 `src/Pry.Api`／`Pry.App`／`Pry.Client`／`Pry.Contracts` 里零引用，唯一引用者是本刀自己的判据文件）。HDS-007 的"连续推进"要接的是 004 生命周期与 005 原子提交的编排面与端点，本刀一律没接；接线时机＝**U-9 仍未拍**。
3. **UI 可见行为**：零读数，本刀未触碰任何界面文件。
4. **生产库**：零接触。`%LOCALAPPDATA%\PryCompanion` 未被当过载体，也没有任何 SQLite 读写；`AdvancementCoordinator` 的清单／日志全是进程内 `List`，重启即失。
5. **待标定参数权威值**：冷却时长、静默窗口、预算默认值（调用数与费用）**全部未定**——本刀只从入参取值，原型内没有一处数字门槛（§四 的 `const`／`static readonly` 零命中就是这条的自证）。真实值由作者拍。
6. **落库／幂等／恢复**：`ExpectedEffectVersion` 的 CAS 只在协调器自己的 `_effectVersion` 计数上演示，没有与 005 的版本列对齐；进程崩溃恢复、并发两个 `Advance` 调用（`List` 非线程安全）都没有取过读数。
7. **旧链删除片与"构建时不把旧链打进包"那道门**：**未开始**。本刀只保证了新文件不引用旧链类型名，没有减少旧链的任何调用面。**三句话照旧不许出现在任何页面**：新链已接线／旧链可删／不进包已做到。
8. **本刀新格的换人复采**：写判据的那只手不能同时是第二双手。本刀 23 枚新格**一枚都没有执行过**（§三.1 零轮次），所以既没有"有红"也没有"无红"的读数可交，全部 23 枚都还得由第二双手复采；§三.0 那份事前论证与探针只是交接材料，**不能替复采签字**。**换手欠账现值＝75 格**（波 28 交账面），与本刀 23 枚新格无交集，也不因本刀新增而减少——本刀欠的是"75 格之外再加 23 枚从未跑过的格"。
9. **`measure-fixed-eval.ps1` 覆盖率**：只在收尾全量的 4 份 TRX 数满并取完读数之后才跑；若因额度或 `HOLD` 未跑，如实登记为未验证，不拿别的读数冒充。

## 六 不许走的捷径（逐条对照）与自我记账

| 工单 §六 那条坑 | 本刀对照结果 |
|---|---|
| 1 把冷却/静默/预算数值写成源码常量凑能跑 | 未犯：`const`／`static readonly` 零命中，三道闸的窗口/预算/版本全部从 `AdvancementWindow`／`AdvancementBudget`／`ExpectedEffectVersion` 入参传入，参数无默认值 |
| 2 用"两条路径调同一构造所以恒等"当覆盖证据 | 未犯：§一.1 那枚 Theory 对三类触发各钉**各自的**字面量（`ue-1`／`due-1`／`bg-1`），R1 把 BackgroundScene 臂指向 `DueIntentReference` 时该格即红（区分性探针存在） |
| 3 把"红不了"当结论而不试 | 未犯，但要记一笔反向的错：§三.0 里四枚是方向类突变（R2 `>`→`>=`、R4 `<`→`<=`、R5 `OrderBy`→`OrderByDescending`、R7 `<=`→`<`），两枚是删赋值／登记行（R3、R6），一枚是改引用臂（R1）。**没有一枚格被我写成"结构上够不着"**；反过来说，我差点把一枚"一次响三枚"的突变（删 `entry.Replayed = true;`）当成单点突变跑下去——事前逐格重推时才发现漏了第二次跳表，这条记在 §六 自我记账第 6 条，换法过程写在 §三.0 |
| 4 读／提交别人的未提交改动 | 未犯：主树零 `git` 写；两枚 `M ` 暂存项与全部 `??` 未跟踪项没读过内容、没动；本刀产物只落在自己的载体与 `.local-notes/wave29-1024/` |
| 5 为顶基线单独再起一次解决方案级构建 | 未犯：解决方案级构建只计划 1 枚（B1）；收尾全量 F **不带** `--no-build`（前一轮是突变态重建，盘上 IL 不是还原态，波 28 §七.3 口径），它自带的增量重建不另计构建 |
| 6 在报告里替守门人写状态词/结案词、自封基线 | 未犯：本报告只写 §〇–§六；起点 1357 从波 28 §八.7 当场读（§〇.4），出口读数由收尾 4 份 TRX 当场求和登记在 §三.1，划账留给 §七 |

本刀自我记账（如实，不粉）：

1. **两枚预检重跑的间隔没达标（P2、P3）**：P1→P2 ＝ 4 分 51 秒（短 9 秒）、P2→P3 ＝ 4 分 45 秒（短 15 秒），都没满足"相邻两次不短于 5 分钟"。两枚预检读到的都还是 `HOLD`、其后都零 `dotnet` 起跑，所以没有"在不该起跑的窗口起跑"的实害，但**间隔判据确实被我违反了两次**，且第二次是在我已经写下"此后先读盘上 `LastWriteTime` 再算"之后发生的。P4 起的执行口径＝先读上一枚预检文件的 `LastWriteTime`、再算需要等待的秒数（`300 − (now − last)`）并向下取整到 0，等完再跑，跑完立刻用 `LastWriteTime` 复核实际间隔并写进 §〇.5。已登记在 §〇.5 两段。
2. **判据文件返工一枚提交**（`77a7d91`）：初版把"补投顺序"断言同时塞进两枚格，会让 R5 一次响两枚、破坏"恰 1 枚红"的归属。这条缺陷是**我自己造判据时造的**，在跑任何 `dotnet` 之前改掉，不占用突变轮；登记在此而不算反证轮。
3. **草稿期两枚自我否决的断言形状**（`"bg-1" is "bg-1" ? …` 一类的恒真写法、`ToString("yyyy-MM-ddTHH:mm:ss", null)` 那种依赖当前 `CultureInfo` 的写法）在提交前就已替换成绝对字面量与与区域性无关的形状（改判 `Assert.All(..., x => Assert.Equal(Noon, x.RecoveredAtUtc))` ＋ `"1,2,3"` 序号串）；未入库，登记备查。
4. **产品面一处语义不自洽（`663d2c9` 修）**：`BuildReadout` 原本把请求里那段空白 `SourceId` 原样回显进读数（`request.Source?.SourceId`），于是"来源缺失"那枚读数一边说 `FailedForMissingSource`、一边还带着一个看似可指认的 id；判据文件里我恰好按"缺失就没有来源"写了 `Assert.Null(blankSource.SourceId)`，两边对着干。这条是**逐字节重读产品码与判据**时查出来的，不是 `dotnet` 跑出来的（本刀至今零 `dotnet`）；改法＝读数构造里来源不可指认时 `SourceId`／`SourceVersion` 一律给 `null`，判据那句保持不变。登记为缺陷而非"判据写错了"，因为缺陷在产品面。
5. **判据面一处期望值漏算（`663d2c9` 修）**：冷却那格 `The_same_intent_resubmitted_inside_the_cooldown_window_…` 原本期望观察日志只有 `"DeferredByCooldown:intent-cool"` 一行，漏了第一次生效那行（协调器的成功出口也往观察日志写行，见 `AdvancementCoordinator.cs:121-122`），照原样会在**绿基线轮**虚报一枚红。改法＝期望值补成 `"Advanced:intent-cool|DeferredByCooldown:intent-cool"`。这条同样由重读取出；它同时说明一件事——本刀若没有这轮零 `dotnet` 的静态复核，G0 那枚定向轮会被一枚判据错账占掉，而定向额度只有 8 枚。
6. **§三.0 首版对"删 `entry.Replayed = true;`"的事前论证是错的**（错在漏查同一行的第二个消费点：跳到 `due` 那一刻之后的 `StillPending`）。发现后没有偷偷换判据凑"恰 1 枚"，而是把突变本身换成同族单点的 `<=`→`<`，并把错论证与未跑探针字节一起写在 §三.0 顶部。这条属于"事前论证可以当场推翻，但必须留痕"。
7. 上述 4、5 两处的连带后果已全部重取：线尾三枚 blob SHA-256、七枚探针的 `sha256sum`、`diff --stat` 产品面（900 行新增／0 行删改）都在 16:43–16:44 当场重跑命令取回并写进 §一／§三.0／§四，旧值作变更史保留在 `out/restore-baselines.txt` 与 `out/product-face.txt`。**没有任何一枚旧 SHA 被当作现值引用。**

**下一手从哪一行接着跑**：本报告 **§三.1 的第一行**（现在是"本刀零轮次"那段之后的空位——第一轮记录就该写在那里）。本刀的"HOLD 之后重跑"额度 **8/8 已用尽**（P2–P9），按工单 §〇.7"额度按刀计、不按日计"的口径，我这一把到此为止、**不得再跑第 10 枚预检**；接着跑的那一手是**新的一把**（或守门人裁定的同树补跑），它自带新的 8 枚重跑额度，起跑点＝在载体目录先取一枚新的预检并归档为 `out/preflight-P10.txt`（`powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1`）。读到 `BUILD_OK` 后的顺序与全部现成参数：B1＝`dotnet build Pry.slnx -c Release`（工作目录＝载体）→ G0＝`dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave29-1024/out/trx-G0" --logger trx` → R1–R7（每枚：`.local-notes/wave29-1024/probe/R<n>-*.cs` 覆盖 §三.0 表里那枚目标产品文件 → 同一形式的定向 test 到 `out/trx-R<n>` → 用 `git cat-file blob HEAD:<path>` 逐字节还原 → 核 `git status --short`＝0 行与 §三.0 末段线尾 SHA `90947162…`／`9bf4dc31…`）→ F＝`dotnet test Pry.slnx -c Release --results-directory "…/out/trx-FULL" --logger trx`（**不带** `--no-build`；先数满 4 份 `.trx` 再逐份读 `<Counters>` 求和，出口读数才是 1357＋23 的右端）→ 最后才 `scripts/measure-fixed-eval.ps1 -ResultsDirectory "…/out/trx-FULL"`。`.local-notes/wave29-1024/round.py` 已经封装好"覆盖→跑→还原→核 SHA 与 status→数 TRX→读 Counters→点 `outcome="Failed"` 节点"整条流水线，十一项字段里 ③④⑥⑦⑧⑨⑩ 的原始材料它直接打出来，⑪ 仍须由跑它的人手取预检。要跑的七枚突变与"恰 1 枚红"的事前论证在 §三.0；**已合成但本刀判定不该跑的第八枚突变**（删 `entry.Replayed = true;`，一次响 3 枚）也在那里点名了——若接手者要跑它，必须按 §三.0 那样事前点名连带响的三枚格名，不许把批次红署给单枚突变。

## 七 守门人划账（签名＝`eval-QA-WAVE29-GK-1024`，10-24 17:1x；§〇–§六 为 lane 原文，一字未改）

- 资格与署名面：本节由守门人写并署守门人的 id；本仓库所有 lane 与守门人共用同一个 git 身份 `LemPhyth`，归属只认提交号与工单，不认作者名。守门人这一把 `dotnet` 调用次数＝**0**（只跑了一次焦点预检，见下面第 5 条），所以本节的每一条判定都来自读盘与读码，不来自任何构建读数。

### 七.1 交付形态判定：**合法的零 `dotnet` 交付**，不是失败交付，也不是"活没干完"

三条独立盘上面证（我先数盘再读它的自述，§〇.5／§三.1 的表只作对照）：

1. `.local-notes/wave29-1024/out/preflight-P1.txt … P9.txt` 九枚逐份 `grep VERDICT` ＝ **9/9 HOLD**，无一枚 `BUILD_OK`；
2. 载体 `.local-notes/qa-wave29-1024/carrier` 内 `find -maxdepth 4 -type d \( -name bin -o -name obj \) | wc -l` ＝ **0**；
3. `find .local-notes/wave29-1024 .local-notes/qa-wave29-1024 -name "*.trx" | wc -l` ＝ **0**，且 `out/` 下没有任何 `trx-*` 结果目录。

⇒ 三件事互相对上，"本刀起跑 0 次"是盘上事实。拦它的是焦点闸，不是额度，也不是它的判断力——**HOLD 就是这条流水线里唯一正确的动作**。HOLD 的真实原因（P1–P9 与我 17:1x 那一枚同一形状）＝前台窗口是 `msedge` 的无边框全屏（`covers=True borderless=True`，整屏 2560×1440，标题是 bilibili 播放页），仪器 v6 按"焦点窗是全屏应用"判 `HOLD`。这属于**保守方向**（作者在看的可能是视频而不是对局），但作者的原话是"不要再三构建触发那个防火墙烦我打游戏"，而改判据去放行＝在他屏幕上赌一把，所以**不改判据、不加严、不绕过**，照 §〇.7 办事。

### 七.2 独立复核（不复用它 §二／§三.0 的汇总数，全部从载体 HEAD blob 当场重取）

| 复核项 | 我的取法 | 结果 |
|---|---|---|
| 新格数 N | `git show HEAD:tests/Pry.Core.Tests/BoundedAdvancementPrototypeTests.cs` 落临时文件后逐标记数：`^    [Fact]`＝17、`^    [Theory]`＝2、`^    [InlineData`＝6、`public void`＝19 | **23**＝17＋2×3，与 §二 表逐字相符；`public void` 19 对 17 Fact＋2 Theory 也自洽（不是"断言数＝格数"那种换算） |
| 七枚突变探针的预合值 | `sha256sum probe/R1…R7` 与 §三.0 表逐字符比 | **7/7 相符**：`7f207e75…`／`1ac7ad8e…`／`9602e82e…`／`d08e4c64…`／`32d41cf5…`／`af66ff56…`／`970d3628…`；未跑的那枚 `cdd18e3f…` 也在盘 |
| 还原基准（"还原后应为"那一侧） | `git cat-file blob HEAD:<path>` 四枚现值 | `AdvancementCoordinator.cs`＝`90947162…`、`DueIntentRegistry.cs`＝`9bf4dc31…`、`AdvancementContracts.cs`＝`259474dc…`、判据＝`1fd944f9…`，与 §三.0 末段 16:43:27 那版**逐字符同**；`out/restore-baselines.txt` 里保留的 `a7d457cc…`／`374dacb2…`／`2274e8da…` 三枚旧值作变更史，没有被当现值引用——**这正是第二十六波立的那条口径（突变文件被载体提交自己改过时，还原判据取该载体 HEAD-blob）第一次被 lane 自己在起跑前用对** |
| 产品面 | `git diff --stat 8e9c13d..HEAD -- src tests scripts Pry.slnx Directory.Build.props`；`git diff --check`；`git status --short \| wc -l` | 四枚**全是新文件**、共 900 行 `+`、0 行删改；`--check` 退出码 0；载体 0 行 ⇒ "既有文件零字节持久化改动"成立 |
| 三条命名/门槛约束 | 三枚产品文件逐枚 `grep -Ec`（旧链五类型名＋`Hds007`／`Wave29`）与 `grep -Ec`（`\bconst \b`／`\bstatic readonly`） | 六项计数**全 0**；`git ls-tree` 确认新目录只有那三枚产品文件 |
| 零生产调用方 | 复核它的三条 grep 形状（`src/` 只列出自己两枚；`Pry.Api`／`App`／`Client`／`Contracts` 计数 0；`tests/` 只有自己的判据文件） | 与 §一 末段一致，归档 `out/zero-caller-greps.txt` |

### 七.3 裁决：**本刀不做归位，在册基线照旧 1357／executed 1355／failed 0／跳过 2**

硬线出处＝工单 §二 那句"写完每一格先按 §三 的定向轮逐枚取红，拿不到红的格照实登记并押下一把"，加上第二十五波收口时立的通用口径（**字节写完不等于可归位**：没在"改坏产品码"之下响过的新格不许进主线，登记处＝`docs/plans/PROJECT_PLAN.md` §9 那条 10-23 的第二十五波 entry）：**一格新判据只要没在"改坏产品码"的情况下响过，就不许进主线**，否则它带着"有人守过"的台账进下一把，而实际上那 23 枚连一次都没执行过。所以：

- 四枚新文件**不落地**，字节留在载体提交 `663d2c9`（基点 `8e9c13d`，父链完整），另归档一份 `.local-notes/wave29-1024/out/wave29-code.patch` 作第二处副本（与第二十五波同法，防载体被后续刀清理）。
- 出口读数**未取**。`1357＋23＝1380`（Core 736→759）只是 §二 写的**目标等式**，不是出口读数，不进任何在册数；`measure-fixed-eval.ps1` 本把未跑（没有可指向的 TRX 目录，跑了就是 `throw`）。
- 换手：本刀 23 枚**没有一枚**有第二双手，因为它们没有第一双手的读数。§五 第 8 条那句"欠的是 75 格之外再加 23 枚从未跑过的格"我原样采信并顶进台账；**旧欠照旧 75 格（波 13 余 47＋波 20 余 28），不因本刀而减，也不许把"余 75 格"读成"剩下不需要复采"**。

### 七.4 两处自我记账的裁定（采信，但账不销）

1. **P2、P3 两枚预检重跑的间隔不达标**（短 9 秒、短 15 秒，且第二次是在它自己写下"先读盘上 `LastWriteTime` 再算"之后发生的）：后果面为零——两枚读到的仍是 `HOLD`、其后零 `dotnet`，没有"在不该起跑的窗口起跑"。裁定＝**如实登记、额度照旧算用尽（8/8）**，第三十波不因为它间隔有瑕而回收额度，也不许把这两枚从台账里抹掉。这条与第二十六波那把自报的 4m32s 同族，两次都是 lane 自报、都不粉饰。
2. **`663d2c9` 那两处起跑前自我纠错**（产品面 `BuildReadout` 回显不可指认的 `SourceId`＝语义不自洽；判据面冷却那格漏算第一次生效那行＝会在绿基线轮虚报红）：都是**逐字节重读产品码与判据**查出来的，不占突变轮、不算反证轮，也不掩盖成"没问题"。第 5 条那句附带事实我特别采信：**若没有这轮零 `dotnet` 的静态复核，定向额度 8 枚里会有一枚被一枚判据错账占掉**——这是"预检拦下来的时间不是空转"的第一个正面证据。

### 七.5 本刀两处值得登记的"新形状"（不是缺陷，是口径升级）

1. **七枚突变里没有一枚需要新增数字门槛**：四枚是方向类（R2 `>`→`>=`、R4 `<`→`<=`、R5 `OrderBy`→`OrderByDescending`、R7 `<=`→`<`），两枚删行（R3 删静默登记、R6 删观察日志），一枚改引用臂（R1）。**没有一枚格被写成"结构上够不着"**——这正是第二十八波我亲手证伪 lane 两条"够不着"之后立进第二十九波 §六.3 的那条口径，这一把第一次被执行到位。
2. **事前论证被自己当场推翻并留痕**：首版 R7（删 `entry.Replayed = true;`）我按表采信为"恰 1 枚红"，它重推后发现那一行有三个消费点＝一次响 3 枚，于是**没有偷偷改判据去凑单点**，而是换成同族单点的 `<=`→`<`，并把错论证与未跑的探针字节一起留在 §三.0 与 `probe/`。⇒ 通用口径进第二十九波工单 §七 与第三十波 §三：**三响的突变可以跑，但必须事前点名连带响的哪三枚格名；批次红不许署给单枚突变**（第二十二波立的口径，这次是在起跑前就用上了，代价从一轮构建变成一次读码）。

### 七.6 出闸第三十波（续跑刀）与它的前置

- 工单＝`docs/dispatch-hds-wave30-1024.md`（`HDS-WAVE30-1024`，出闸日＝今天，与第二十九波同日），性质＝**续跑刀**：复用现有载体 `663d2c9`，不重造原型、不新建产品文件、不改判据字节。
- 起跑前置＝**一次 `VERDICT=BUILD_OK` 的焦点预检窗口**，新的 8 枚 HOLD 重跑额度按"刀"计。读到 `HOLD` 就照 §〇.7 把零 `dotnet` 的活做完再交回，那是合法交付。
- 顺序与参数全部现成（本报告"下一手"那段就是它的作业单）：B1 一次解决方案级构建 → G0 绿基线定向 → R1–R7 逐枚定向（覆盖→跑→按 blob 还原→核 `status` 0 行与 `90947162…`／`9bf4dc31…`）→ F 收尾全量（**不带** `--no-build`，先数满 4 份 TRX 再逐份读 `<Counters>` 求和）→ 最后 `measure-fixed-eval.ps1 -ResultsDirectory`。
- **归位仍由守门人做**（按路径落地＋`commit --only`），lane 不许碰主树 ref；在册基线由拿到 F 那一轮求和的那一手顶，本把不动。
- 若第三十波仍连续 `HOLD` 到额度用尽：**它同样是合法交付，而扣住归位这件事照旧成立**——新格的准入判据是红证，不是"字节已经写好、只差一轮构建"。

## 八 第三十波轮次记录（续跑刀 `HDS-WAVE30-1024`）

本节由第三十号冷启动 lane 写。§〇–§六 是第二十九波 lane 原文、§七 是守门人划账（`eval-QA-WAVE29-GK-1024`），**本刀一字未改、未重排、未"顺手更正"**；本节若发现某句与本轮实测不符，只在这里点名并附盘上读数。本刀不写状态词／结案词、不自封在册基线、不碰主树 ref（归位由守门人做）。

### 八.0 入场自证与起跑前判据（全部当场取，2026-10-27 23:20–23:2x）

必读五份已逐份完整读完：工单 `docs/dispatch-hds-wave30-1024.md`（69 行全文）、上一把报告全文（482 行，含 §七）、`docs/dispatch-hds-wave29-1024.md`（含 §七 守门人划账）、`docs/agent-engineering-standards.md`（214 行全文，交付形态按其 §10 四项）、`docs/plans/PROJECT_PLAN.md` 的 §4.2 HDS-007 行（第 80 行）＋§9 最新两条（第 202 行＝10-24 17:1x 收第二十九波、第 213 行＝10-24 16:2x 出闸第二十九波）；另按 `AGENTS.md` 与 `docs/plans/README.md`（50 行全文，权威工作规则 14 条）的要求读了入口页。起点读数按工单 §〇.3 要求从第二十八波报告取，见本末。

主树入场读数（原样，本刀零 `git` 写）：

```
$ date
Tue Oct 27 23:22:23     2026

$ git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD
9ac43969bb9965cdcd439a2d87d4ca694ca254a7

$ git status --short --branch
## codex/ui-main-window-redesign
M  docs/dispatch-tip1020-02-memory-group-judgment.md
 M docs/plans/PROJECT_PLAN.md
M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs
?? .qoder/
?? docs/character-learning-safety.patch
?? docs/research/HDS_INTERLUDE.md
?? docs/research/HDS_QQ_RESEARCH_INDEX.md
?? docs/research/QQ_BRIDGE.md

$ git worktree list | wc -l
75
```

- 与上一把 §〇.2 那版入场读数相比多了两件事，都属他人所有、本刀一字不碰：线尾已从 `2c681a4` 前进到 `9ac4396`（第二十九波报告入库＋波 30 工单＋两枚文末空白整理），且 `docs/plans/PROJECT_PLAN.md` 现有一枚**未暂存**的 ` M` 改动（上一把入场时没有）。工单 §〇.5 点名的是两枚 `M ` 暂存项＋全部 `??`；这枚 ` M` 不在那三件名下，但它同样不在本刀文件面内 ⇒ 本刀对它零写、零提交、不 `stash`／`clean`／`rm`。
- **入场自证三条（工单 §〇.2）全部成立** ⇒ 不停刀：

```
$ git -C C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave29-1024/carrier rev-parse HEAD
663d2c9d78d7f607137f10cd2af09069b0a4f841      ← 与工单指定的全号逐字符相同

$ git -C .../carrier status --short | wc -l
0                                              ← 入场即干净

$ git -C .../carrier ls-tree -r --name-only HEAD -- src/Pry.Core/Narrative/Advancement tests/Pry.Core.Tests/BoundedAdvancementPrototypeTests.cs
src/Pry.Core/Narrative/Advancement/AdvancementContracts.cs
src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs
src/Pry.Core/Narrative/Advancement/DueIntentRegistry.cs
tests/Pry.Core.Tests/BoundedAdvancementPrototypeTests.cs      ← 四枚仍在

$ git -C .../carrier log --oneline -4
663d2c9 线尾 → 77a7d91 → ed7995c → 8e9c13d（基点，detached）
```

- **四枚"还原后应为"现值由本刀自己重取**（`git -C <载体> cat-file blob HEAD:<path> | sha256sum`，2026-10-27 23:21 当场），与工单 §〇.6 那四枚逐字符相符 ⇒ 可以按表跑：

| 文件 | 本刀重取值（载体 HEAD blob） | 工单 §〇.6 | 相符 |
|---|---|---|---|
| `AdvancementCoordinator.cs`（R1／R2／R3／R4／R6 目标） | `90947162fed797f8de98bd5130a11dfacc25886bfa035182dccaf395b9d32b73` | 同 | 是 |
| `DueIntentRegistry.cs`（R5／R7 目标） | `9bf4dc31bd3d1c724a804f2dc01334806dada6a1c88861eb07f89fc0897ce5a1` | 同 | 是 |
| `AdvancementContracts.cs`（非突变目标，只登记） | `259474dcccbfb1f0327d92ba5c49e1e8ee35280c16abf6761088332695976ac0` | 同 | 是 |
| `tests/Pry.Core.Tests/BoundedAdvancementPrototypeTests.cs`（判据，本刀全程零写入） | `1fd944f94d399a8f742482c1f724f8afec150eac865cf06cdfa149d4926a200b` | 同 | 是 |

- 盘上字节与探针也当场复对：`probe/base-coordinator.cs`＝`90947162…`、`probe/base-registry.cs`＝`9bf4dc31…`（与 HEAD blob 同一字节）；七枚探针 `sha256sum` 与 §三.0 表七枚预合值**逐字符相符**（`7f207e75…`／`1ac7ad8e…`／`9602e82e…`／`d08e4c64…`／`32d41cf5…`／`af66ff56…`／`970d3628…`），未跑那枚 `probe/unrun-replay-mark-removed.cs`＝`cdd18e3f…` 仍在盘、**本刀不跑**（定向额度已被 G0＋R1–R7 排满，无作废空额，工单 §一 末段条件不成立）。
- 单枚性用 `diff -u` 复对（零 `dotnet`）：R1／R2／R4／R5／R7 各 1 行换 1 行、R3 删 1 行、R6 删同一条语句的 2 个物理行，七枚全部单 hunk；目标行号与 §三.0 表逐字相符（`AdvancementCoordinator.cs` 133／59／78／85／50-51；`DueIntentRegistry.cs` 69／68）。
- **起点读数（从波 28 报告 §八.7 原文照抄，不从工单抄）**：出处＝`docs/eval/reports/2026-10-24-hds006-evidence-identity-retrieval-prototype.md:616` 那句原文「顶账＝**1357／executed 1355／failed 0／跳过 2**（分项 Client 108／App 233／Api 280／Core **736**、Core executed 734）」⇒ 本刀起点＝1357／executed 1355／failed 0／跳过 2。出口读数右端只由本刀收尾那四份原始 TRX 的 `<Counters>` 当场求和取出，**不用 `1357＋23＝1380` 预写**。
- 仪器与目录：`.local-notes/wave29-1024/round.py`（上一把留的"覆盖→跑→按 `git cat-file blob HEAD:` 还原→核 SHA 与 `status`→数 TRX→读 `<Counters>`→点 `outcome="Failed"` 节点"流水线，直接复用；⑤⑪ 由本刀手写）；各轮独立结果目录命名＝`out/trx-B1`／`trx-G0`／`trx-R1`…`trx-R7`／`trx-FULL`，不复用。入场时 `find .local-notes/wave29-1024 -name "*.trx" | wc -l`＝**0**（23:20 当场数）⇒ 下面每一枚 TRX 都由本刀产生。

本刀预算与用量（用满即停）：

| 面 | 上限 | 本刀计划 | 已用 |
|---|---|---|---|
| 解决方案级构建 | ≤1 | 1（B1，工作目录＝载体） | **1**（B1，exit 0） |
| 定向 | ≤8 | G0 绿基线＋R1–R7＝**正好 8 枚，无复取位** | **8**（G0 1 ＋ R1–R7 7，额度用满，无第 9 枚） |
| 解决方案级全量 | ≤1 | 1（F，**不带** `--no-build`） | **1**（F，exit 0，4 枚 TRX） |

⇒ `dotnet` 实际调用总次数＝**10**（盘上凭据：`out/build-B1.txt` ＋ `out/run-G0.txt`／`run-R1.txt`…`run-R7.txt`／`run-F.txt` 共 10 份，`.trx` 共 12 枚＝1＋7＋4，结果目录 9 个各不重复）；`measure-fixed-eval.ps1` 1 次（非 `dotnet`）；焦点预检 10 枚（P10–P19）。上一把的 §〇.6 那张表（三列全 0）是它自己时点的合法读数，本刀不改它，只在这里记本刀自己的用量。

焦点预检台账（编号沿用上一把，从 `P10` 起；`HOLD` 之后的重跑额度＝8 枚＝`P10`…`P17`，相邻两次不短于 5 分钟；每枚先读上一枚文件 `LastWriteTime` 再决定等多久，跑完用 `LastWriteTime` 复核实际间隔并写进本表）：

| 预检 | 归档文件 | VERDICT | 落盘时刻 | 距上一枚实际间隔 | 紧随其后的 `dotnet` |
|---|---|---|---|---|---|
| P10 | `out/preflight-P10.txt` | **BUILD_OK** | 2026-10-27 23:23:57（`LastWriteTime` 复核） | 本刀第一枚，无前枚 | **1 次**：B1 `dotnet build Pry.slnx -c Release`（exit 0） |
| P11 | `out/preflight-P11.txt` | **BUILD_OK** | 2026-10-27 23:25:30（`LastWriteTime` 复核） | 93 秒（非 HOLD 重跑，不受 5 分钟口径约束） | **1 次**：G0 `dotnet test tests/Pry.Core.Tests`（exit 0，TRX 1 枚） |
| P12 | `out/preflight-P12.txt` | **BUILD_OK** | 23:28:34 | 184 秒 | **1 次**：R1（exit 1＝有 1 枚红，TRX 1 枚） |
| P13 | `out/preflight-P13.txt` | **BUILD_OK** | 23:31:25 | 171 秒 | **1 次**：R2（exit 1，TRX 1 枚） |
| P14 | `out/preflight-P14.txt` | **BUILD_OK** | 23:33:02 | 97 秒 | **1 次**：R3（exit 1，TRX 1 枚） |
| P15 | `out/preflight-P15.txt` | **BUILD_OK** | 23:34:11 | 69 秒 | **1 次**：R4（exit 1，TRX 1 枚） |
| P16 | `out/preflight-P16.txt` | **BUILD_OK** | 23:35:22 | 71 秒 | **1 次**：R5（exit 1，TRX 1 枚） |
| P17 | `out/preflight-P17.txt` | **BUILD_OK** | 23:36:41 | 79 秒 | **1 次**：R6（exit 1，TRX 1 枚） |
| P18 | `out/preflight-P18.txt` | **BUILD_OK** | 23:37:49 | 68 秒 | **1 次**：R7（exit 1，TRX 1 枚） |
| P19 | `out/preflight-P19.txt` | **BUILD_OK** | 23:39:04 | 75 秒 | **1 次**：F `dotnet test Pry.slnx`（exit 0，TRX 4 枚） |
| （说明） | 十枚预检全部 `BUILD_OK`、`HOLD` 0 枚 ⇒ §〇.5 那 8 枚"HOLD 之后重跑"额度**未动用（0/8）**；上表"间隔"列是相邻两枚预检的实际间隔（`LastWriteTime` 复核），本刀没有 `HOLD` 重跑，故 5 分钟口径没有适用点，间隔仍照实登记。编号沿上一把 P1–P9 之后连续排（未另起 `P1`，两把台账不混）。 | | | | |


P10 整段（23:23:57 当场原样，归档＝`out/preflight-P10.txt`；本刀入场后第一枚预检，读到 `BUILD_OK` ⇒ 允许起跑）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: explorer pid=7404 class=CabinetWClass style=0x14CF0000 rect=241,372,1082x910 screen=2560x1440 covers=False borderless=False title=C:\Users\24782\Desktop\AI_Act\AI_Act_Mod_Launche
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21788, NVIDIA Overlay pid=18308
gpu_util=0% used=2246MB
free_memory_gb=8.2
VERDICT=BUILD_OK
```

- 与上一把九枚的区别：前台窗是普通的资源管理器窗口（`covers=False borderless=False`，非整屏矩形），不是无边框全屏应用 ⇒ v6 放行。`free_memory_gb`＝**8.2**，比上一把台账的 10.7–11.1 低，但仪器自身的资源判据给的是 `BUILD_OK`；本刀照 §〇.5 只做一件事——若某一轮读数出现 SQLite `disk I/O`／`out of memory` 形态，那一轮读数作废并如实登记，不动任何进程。下面 G0 起每一枚预检的 `free_memory_gb` 逐枚记进台账。
- "HOLD 之后的重跑"额度：**尚未动用**（0/8，P10 一枚即 `BUILD_OK`）。

P11 整段（23:25:30 当场原样，归档＝`out/preflight-P11.txt`；G0 轮前的预检。距 P10 ＝ 93 秒；**这不是"HOLD 之后的重跑"**——P10 读到 `BUILD_OK`，工单 §〇.5 的 5 分钟间隔与 8 枚上限只约束 `HOLD` 之后的重跑，故不受该额度与间隔约束，间隔照实登记）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: QQ pid=27508 class=Chrome_WidgetWin_1 style=0x14C70000 rect=-1746,242,1215x802 screen=1920x1080 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21788, NVIDIA Overlay pid=18308, explorer pid=7404
gpu_util=5% used=2274MB
free_memory_gb=6.7
VERDICT=BUILD_OK
```

- 前台窗＝`QQ`（`covers=False borderless=False`，非整屏，副屏坐标 `-1746,242`）⇒ 放行。`free_memory_gb` 从 8.2 降到 **6.7**：仪器的资源判据仍给 `BUILD_OK`，本刀按 §〇.5 照跑，并在每一轮把 TRX 的 `failed`／`error` 计数与日志里的 `disk I/O`／`out of memory` 形态一起核；一旦出现该形态，那一轮读数作废并如实登记（G0 未见，`failed=0`、`error=0`）。


### 八.1 轮次记录（十一项字段逐轮追加）

#### B1｜解决方案级构建（额度 1/1 用满）

| 项 | 读数 |
|---|---|
| ① 文件:行 | **本轮无突变覆盖**（B1 是构建轮，工单 §一.1 顺序里的第一轮；四枚文件保持载体 HEAD 字节） |
| ② 改前原文 | N/A（零覆盖） |
| ③ 改后原文 | N/A |
| ④ `git diff --stat` | **0 行**（载体 `git diff --stat` 输出空）；`git status --short`＝**0 行** |
| ⑤ 跑法整行 | `dotnet build Pry.slnx -c Release`，工作目录＝`C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave29-1024/carrier`（由 `python .local-notes/wave29-1024/round.py B1 --build` 发起，脚本内 `cwd=CARRIER`） |
| ⑥ TRX 文件名＋`<Counters>` | **本轮不产 TRX**（构建无 `--logger trx` 语义）。盘上凭据＝`out/build-B1.txt` 全文 29 行，末三行读数原文＝`已成功生成。`／`    0 个警告`／`    0 个错误`，`已用时间 00:00:13.81`；`dotnet` 退出码＝**0** |
| ⑦ 红格全名／消息／行号 | 无（构建轮，零错误零警告，不存在失败节点） |
| ⑧ 还原命令 | 本轮无覆盖 ⇒ 无需还原；作为对照执行的复核＝`sha256sum` 直接取盘上三枚目标文件字节 |
| ⑨ 还原后 `git status --short` 行数 | **0 行**（构建之后当场数，构建产物 `bin`/`obj` 共 18 枚目录全在 gitignore 面内，未进 `status`） |
| ⑩ 突变态与还原后 SHA-256 | 无突变态；盘上现值＝`AdvancementCoordinator.cs` `90947162fed797f8de98bd5130a11dfacc25886bfa035182dccaf395b9d32b73`、`DueIntentRegistry.cs` `9bf4dc31bd3d1c724a804f2dc01334806dada6a1c88861eb07f89fc0897ce5a1`、判据 `1fd944f94d399a8f742482c1f724f8afec150eac865cf06cdfa149d4926a200b` —— 三枚与 §八.0 那枚"载体 HEAD blob 重取值"**逐字符相同** ⇒ 构建没有改动任何判据或产品字节 |
| ⑪ 当轮预检 | **P10**（`out/preflight-P10.txt`，23:23:57，`VERDICT=BUILD_OK`，`free_memory_gb=8.2`），整段原文见 §八.0 末尾 |

- B1 的判读意义：上一把那三枚提交（`ed7995c`→`77a7d91`→`663d2c9`）落的四枚文件**能编译**，这是它从未被验证过的一条（§〇.1 里波 29 的 §五.0 明写"零轮次"）。构建通过不等于 23 枚格绿，绿不绿只看 G0 那轮的 TRX。
- 顺带登记一条**与本刀实测不符的上一把原文**（工单 §〇.4 允许的更正方式＝在 §八 点名＋附盘上读数，不改原文）：§三.1 那句"载体里 `bin`/`obj` 0 枚"在 B1 之后变成 18 枚目录（`find . -maxdepth 4 -type d \( -name bin -o -name obj \) | wc -l`＝18，23:24 当场）。那行是上一把**它自己零起跑时点**的合法读数，本刀不判它错，只说明本刀已把它推进了一格。

#### G0｜绿基线定向（定向额度 1/8）

| 项 | 读数 |
|---|---|
| ① 文件:行 | **本轮无突变**（绿基线轮，四枚文件＝载体 HEAD 字节，入场时已核 §八.0） |
| ② 改前原文／③ 改后原文 | N/A（零覆盖；`git diff --stat` 本轮前后各数一次均 **0 行**，`git status --short` **0 行**） |
| ⑤ 跑法整行 | `dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave29-1024/out/trx-G0" --logger trx`（工作目录＝载体；由 `python .local-notes/wave29-1024/round.py G0` 发起） |
| ⑥ TRX 文件名＋`<Counters>` | 目录 `out/trx-G0` 内 `ls *.trx` 数满＝**1 枚**：`24782_PHYTHLINK_2026-10-27_23_25_42.trx`。`<Counters>` 原文逐属性＝`total=759`／`executed=757`／`passed=757`／`failed=0`／`error=0`／`notExecuted=0`／`aborted=0`／`inconclusive=0`／`warning=0`／`completed=0`／`disconnected=0`／`inProgress=0`／`notRunnable=0`／`passedButRunAborted=0`／`pending=0`／`timeout=0` |
| ⑦ 红格 | **零枚红**（`failed=0`、`error=0`；`outcome="Failed"` 节点计数 0）。按 `outcome="NotExecuted"` 点名的两枚＝`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key` 与 `Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`——与波 28 §八.7 那两条同名跳过逐字相同，**不是本刀的格**，`notExecuted` 计数器照旧恒 0 不看 |
| ⑧ 还原命令 | 本轮无覆盖 ⇒ 无需还原 |
| ⑨ `git status --short` 行数 | **0 行**（跑后当场数） |
| ⑩ SHA-256 | 无突变态；三枚目标文件盘上现值跑后仍＝`90947162…`／`9bf4dc31…`／`1fd944f9…`（与 §八.0 逐字符同）⇒ 判据字节本轮未被写过 |
| ⑪ 当轮预检 | **P11**（`out/preflight-P11.txt`，23:25:30，`VERDICT=BUILD_OK`，`free_memory_gb=6.7`），整段见下 |

**本刀 23 枚新格在 G0 的逐格读数（从同一枚 TRX 的 `outcome` 属性逐节点取，`testName` 含 `BoundedAdvancementPrototypeTests` 的节点计数＝23，全 `Passed`）**：

```
[Passed] Each_trigger_kind_enters_the_one_advancement_entry_and_names_its_own_reference(asked: UserEvent, expectedReference: "ue-1")
[Passed] Each_trigger_kind_enters_the_one_advancement_entry_and_names_its_own_reference(asked: DueIntent, expectedReference: "due-1")
[Passed] Each_trigger_kind_enters_the_one_advancement_entry_and_names_its_own_reference(asked: BackgroundScene, expectedReference: "bg-1")
[Passed] The_advancement_coordinator_exposes_exactly_one_public_entry_method
[Passed] The_same_intent_resubmitted_inside_the_cooldown_window_is_deferred_and_names_the_earliest_next_instant
[Passed] An_intent_resubmitted_at_the_instant_the_cooldown_window_closes_advances_again
[Passed] A_due_intent_inside_the_quiet_window_is_deferred_until_the_window_closes_and_stays_observable
[Passed] A_quiet_deferred_intent_advances_once_the_window_closes_and_leaves_the_pending_list
[Passed] An_exhausted_call_budget_stops_advancement_and_names_consumed_remaining_and_the_dimension
[Passed] A_budget_that_is_zero_at_entry_is_a_distinct_shape_from_one_that_ran_out
[Passed] A_cost_budget_running_out_stops_advancement_while_calls_are_still_available
[Passed] An_advancement_that_exactly_consumes_the_remaining_call_budget_is_allowed
[Passed] After_a_clock_forward_jump_due_intents_are_replayed_in_registration_order
[Passed] Replay_delivers_each_due_intent_exactly_once_across_two_clock_jumps_and_loses_none
[Passed] A_due_intent_that_has_not_reached_its_due_instant_is_left_pending_and_still_reported
[Passed] A_late_advancement_carrying_a_superseded_effect_version_is_refused_and_writes_nothing
[Passed] An_advancement_whose_source_cannot_be_pointed_to_fails_with_a_named_reason_and_stays_in_the_log
[Passed] Missing_source_is_refused_for_every_trigger_kind_with_the_same_named_failure(asked: UserEvent, requestId: "req-missing-ue")
[Passed] Missing_source_is_refused_for_every_trigger_kind_with_the_same_named_failure(asked: DueIntent, requestId: "req-missing-due")
[Passed] Missing_source_is_refused_for_every_trigger_kind_with_the_same_named_failure(asked: BackgroundScene, requestId: "req-missing-bg")
[Passed] A_successful_advancement_carries_the_source_reference_into_the_record_it_produces
[Passed] Advancement_prototype_sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites
[Passed] Advancement_prototype_declares_no_source_side_default_for_windows_or_budgets
count=23 byOutcome={"Passed":23}
```

- **工单 §一.1 对 G0 的期望（"那 23 枚全绿"）成立**，且 Core 面 `total=759`＝波 28 在册的 Core 736＋本刀 23 枚，等式与 §二 顶账形状一致（右端仍要以 F 那四份 TRX 当场求和为准，G0 只覆盖 Core 一份）。
- 关于"上一把原文与本轮实测是否相符"，本节做一次专门复核（工单 §〇.4 允许的动作＝在 §八 点名＋附盘上读数，不改原文）：**本刀没有点出任何一条需要更正的实质不符**。举一处最容易误判的为例——§三.0 的 R2 事前论证那句"其余格的第二次请求都严格晚于上次生效时刻且冷却窗口传 `TimeSpan.Zero`（`Noon >= 12:05` 为假）"：本刀复算 `Noon`＝12:00，`12:00 >= 12:05` 确实为假，且 R2 轮实测 `failed=1` 说明那批夹具在 `>` 与 `>=` 下同判 ⇒ 该句与盘上读数相符，不列为疑点。R1／R3／R4／R5／R6／R7 六轮的事前论证与实测红集**逐字相符**（每轮的对照见 §八.1 各轮末段）。唯一与本刀实测有差的一句是 §三.1 那句"载体里 `bin`/`obj` 0 枚"——那是上一把零起跑时点的合法读数，本刀 B1 之后为 18 枚，已按上文登记，不算它的错。


#### R1｜`AdvancementCoordinator.cs:133` 背景场景臂指向到期意图引用（定向额度 2/8）

十一项字段（②③ 逐字节取自载体与探针副本，④⑥⑦⑧⑨⑩ 的原始材料由 `round.py` 当场打印＝`out/w30-round-R1.txt`，⑤⑪ 本刀手写）：

| 项 | 读数 |
|---|---|
| ① 文件:行 | `src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs:133`（载体相对路径；绝对＝`C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave29-1024/carrier/src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs`） |
| ② 改前原文 | `        AdvancementTriggerKind.BackgroundScene => request.BackgroundSceneReference,` |
| ③ 改后原文 | `        AdvancementTriggerKind.BackgroundScene => request.DueIntentReference,` |
| ④ `git diff --stat` | `src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs \| 2 +-` → `1 file changed, 1 insertion(+), 1 deletion(-)`（单 hunk、单行换单行，与 §三.0 末段"单枚性预检"一致） |
| ⑤ 跑法整行 | `dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave29-1024/out/trx-R1" --logger trx`（工作目录＝载体；**未带** `--no-build`，突变轮一律自行重建）。发起命令＝`python .local-notes/wave29-1024/round.py R1 --probe "…/probe/R1-bg-arm-aliased.cs" --target "…/carrier/src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs"` |
| ⑥ TRX 文件名＋`<Counters>` | `out/trx-R1` 内 `ls` 数满＝`total=1 trx=1`：`24782_PHYTHLINK_2026-10-27_23_28_46.trx`。`<Counters>` 逐属性＝`total=759`／`executed=757`／`passed=756`／`failed=1`／`error=0`／`notExecuted=0`／其余（aborted／completed／disconnected／inProgress／inconclusive／notRunnable／passedButRunAborted／pending／timeout／warning）＝0 |
| ⑦ 红格全名＋消息要点＋红点行号 | 红格**恰 1 枚**，格名逐字＝`Pry.Core.Tests.BoundedAdvancementPrototypeTests.Each_trigger_kind_enters_the_one_advancement_entry_and_names_its_own_reference(asked: BackgroundScene, expectedReference: "bg-1")`。消息要点＝`Assert.Equal() Failure: Strings differ` / `Expected: "bg-1"` / `Actual: "due-1"`（＝背景场景那一臂读到的是到期意图引用，三类触发成别名）。**TRX 里的红点行号**（照抄 `StackTrace` 那一行）＝`…carrier\tests\Pry.Core.Tests\BoundedAdvancementPrototypeTests.cs:line 63`；**源码里该 assert 文本在哪行**＝判据文件第 63 行 `Assert.Equal(expectedReference, readout.TriggerReference);` ⇒ 两者相同（本刀另按工单要求分栏登记，相同不构成疑点） |
| ⑧ 还原命令 | `git -C <载体> cat-file blob HEAD:src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs > src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs`（由 `round.py` 以 `blob(rel)` 逐字节写回） |
| ⑨ 还原后 `git status --short` 行数 | **0 行**（`status --short lines after restore: 0 []`） |
| ⑩ SHA-256 | 覆盖前工作树＝`90947162…`（`== HEAD blob: True`）；**突变态**＝`7f207e75dde25d7e2164688f7fd76fe316df82f632dbe4c98c4f9b199616cc75`（与 §三.0 表 R1 预合值逐字符相同）；**还原后**＝`90947162fed797f8de98bd5130a11dfacc25886bfa035182dccaf395b9d32b73`（`== HEAD blob: True`，与 §八.0 本刀重取值相同）。判据文件本刀零写入，仍＝`1fd944f9…` |
| ⑪ 当轮预检 | **P12**（`out/preflight-P12.txt`，23:28:34，`VERDICT=BUILD_OK`，`free_memory_gb=7.3`），整段见下 |

P12 整段（R1 轮前预检，23:28:34 当场；距 P11 184 秒，非 HOLD 重跑故不受 5 分钟口径约束）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: QQ pid=27508 class=Chrome_WidgetWin_1 style=0x14C70000 rect=-1746,242,1215x802 screen=1920x1080 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21788, NVIDIA Overlay pid=18308, explorer pid=7404
gpu_util=3% used=2256MB
free_memory_gb=7.3
VERDICT=BUILD_OK
```

- **与 §三.0 事前论证的对照**：期望红集＝"恰 1 枚＝`Each_trigger_kind_enters_the_one_advancement_entry_and_names_its_own_reference(asked: BackgroundScene, expectedReference: "bg-1")`"，实测红集＝**同一枚、且只有这一枚**（`failed=1`，另两条 `UserEvent`／`DueIntent` 探针、`Missing_source_is_refused_for_every_trigger_kind` 三条、§一.2 那两枚静默格全在 `passed=756` 里）。⇒ **R1 形状相符，清洁红 1 枚**。
- 附一条仪器记账：`round.py` 的 `report_trx` 在打印失败消息时抛 `SyntaxError: prefix 'http' not found in prefix map`（它的 ElementTree xpath 写法在 Python 3.14 下不接受未加前缀的命名空间谓词）。崩溃点在该轮**全部关键动作之后**（覆盖→跑→还原→核 SHA 与 `status`→数 TRX→读 `<Counters>`→点名 `outcome="Failed"` 节点都已打印完毕，见 `out/w30-round-R1.txt` 前 20 行），缺的只有 ⑦ 的消息正文；本刀用自写的 `out/w30-failmsg.js` 直接从 TRX 取 `Message`/`StackTrace`，**没有改动上一把的仪器一个字节**，也没有因此重跑 `dotnet`（该轮 `dotnet` 仍只 1 次）。

#### R2｜`AdvancementCoordinator.cs:59` 冷却窗口外沿 `>`→`>=`（定向额度 3/8）

| 项 | 读数 |
|---|---|
| ① 文件:行 | `src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs:59` |
| ② 改前原文 | `        if (cooldownOpensAt is { } cooldownEnd && cooldownEnd > request.Window.NowUtc)` |
| ③ 改后原文 | `        if (cooldownOpensAt is { } cooldownEnd && cooldownEnd >= request.Window.NowUtc)` |
| ④ `git diff --stat` | `AdvancementCoordinator.cs \| 2 +-` → `1 file changed, 1 insertion(+), 1 deletion(-)`（单 hunk，方向类突变，无新增数字门槛） |
| ⑤ 跑法整行 | `dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave29-1024/out/trx-R2" --logger trx`（工作目录＝载体；未带 `--no-build`）。发起＝`python .local-notes/wave29-1024/round.py R2 --probe "…/probe/R2-cooldown-boundary-flipped.cs" --target "…/carrier/src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs"` |
| ⑥ TRX＋`<Counters>` | `out/trx-R2` 数满＝`total=1 trx=1`：`24782_PHYTHLINK_2026-10-27_23_31_30.trx`；`total=759`／`executed=757`／`passed=756`／`failed=1`／`error=0`／`notExecuted=0`／其余全 0 |
| ⑦ 红格 | 恰 1 枚＝`Pry.Core.Tests.BoundedAdvancementPrototypeTests.An_intent_resubmitted_at_the_instant_the_cooldown_window_closes_advances_again`。消息＝`Assert.Equal() Failure: Values differ` / `Expected: Advanced` / `Actual: DeferredByCooldown`（外沿那一瞬被算进窗口＝放行变成推迟）。TRX 红点行号照抄＝`…BoundedAdvancementPrototypeTests.cs:line 120`；源码 assert 文本行＝判据文件第 120 行 `Assert.Equal(AdvancementOutcome.Advanced, second.Outcome);` ⇒ 相同 |
| ⑧ 还原命令 | `git -C <载体> cat-file blob HEAD:src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs > 同路径` |
| ⑨ 还原后 `git status --short` 行数 | **0 行** |
| ⑩ SHA-256 | 覆盖前＝`90947162…`（== HEAD blob True）；突变态＝`1ac7ad8e52a9f3080bf85329fb71aa26f9c0ea2e062ba3faeb00110c1ba86549`（== §三.0 R2 预合值）；还原后＝`90947162fed797f8de98bd5130a11dfacc25886bfa035182dccaf395b9d32b73`（== HEAD blob True）。判据仍 `1fd944f9…` |
| ⑪ 当轮预检 | **P13**（`out/preflight-P13.txt`，23:31:25，`VERDICT=BUILD_OK`，`free_memory_gb=7.7`；距 P12 171 秒，非 HOLD 重跑） |

P13 整段：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: QQ pid=27508 class=Chrome_WidgetWin_1 style=0x14C70000 rect=-1746,242,1215x802 screen=1920x1080 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21788, NVIDIA Overlay pid=18308, explorer pid=7404
gpu_util=0% used=2259MB
free_memory_gb=7.7
VERDICT=BUILD_OK
```

- **与 §三.0 事前论证对照**：期望"恰 1 枚＝`An_intent_resubmitted_at_the_instant_the_cooldown_window_closes_advances_again`"，实测红集就是这一枚且只有这一枚（窗口内那格 `The_same_intent_resubmitted_inside_the_cooldown_window_…` 仍绿＝本来就期望推迟；§一.3 与 §一.4 那批第二次请求严格晚于上次生效时刻的格子不受 `>=` 影响）。⇒ **清洁红 1 枚，形状相符**。

#### R3｜`AdvancementCoordinator.cs:78` 删掉静默推迟的登记行（定向额度 4/8）

| 项 | 读数 |
|---|---|
| ① 文件:行 | `src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs:78` |
| ② 改前原文 | `            _deferrals.Add(new AdvancementDeferral(request.IntentKey, request.Kind, "quiet", quietEnd));` |
| ③ 改后原文 | （该行被删除，改后无对应行；`git diff --stat`＝`1 file changed, 1 deletion(-)`） |
| ④ `git diff --stat` | `AdvancementCoordinator.cs \| 1 -` → `1 file changed, 1 deletion(-)`（单 hunk，删 1 行无 `+`，与 §三.0 末段"R3 各删 1 行"一致） |
| ⑤ 跑法整行 | `dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave29-1024/out/trx-R3" --logger trx`（工作目录＝载体；未带 `--no-build`）。发起＝`python .local-notes/wave29-1024/round.py R3 --probe "…/probe/R3-quiet-deferral-not-recorded.cs" --target "…/AdvancementCoordinator.cs"` |
| ⑥ TRX＋`<Counters>` | `out/trx-R3` 数满＝`total=1 trx=1`：`24782_PHYTHLINK_2026-10-27_23_33_06.trx`；`total=759`／`executed=757`／`passed=756`／`failed=1`／`error=0`／`notExecuted=0`／其余全 0 |
| ⑦ 红格 | 恰 1 枚＝`Pry.Core.Tests.BoundedAdvancementPrototypeTests.A_due_intent_inside_the_quiet_window_is_deferred_until_the_window_closes_and_stays_observable`。消息＝`Assert.Single() Failure: The collection was empty`（推迟变成悄悄不见，待观察清单读空）。TRX 红点行号照抄＝`…BoundedAdvancementPrototypeTests.cs:line 143`；源码该行文本＝判据文件第 143 行 `        var deferral = Assert.Single(coordinator.PendingDeferrals);` ⇒ 相同 |
| ⑧ 还原命令 | `git -C <载体> cat-file blob HEAD:src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs > 同路径` |
| ⑨ 还原后 `git status --short` 行数 | **0 行** |
| ⑩ SHA-256 | 覆盖前＝`90947162…`（True）；突变态＝`9602e82e340c3a70227b4f152f5ebd34b68da27301f8d752e0da6bc0880abeee`（== §三.0 R3 预合值）；还原后＝`90947162…`（True）。判据仍 `1fd944f9…` |
| ⑪ 当轮预检 | **P14**（`out/preflight-P14.txt`，23:33:02，`VERDICT=BUILD_OK`，`free_memory_gb=7.4`；距 P13 97 秒，非 HOLD 重跑） |

P14 整段：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: QQ pid=27508 class=Chrome_WidgetWin_1 style=0x14C70000 rect=-1746,242,1215x802 screen=1920x1080 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21788, NVIDIA Overlay pid=18308, explorer pid=7404
gpu_util=11% used=2254MB
free_memory_gb=7.4
VERDICT=BUILD_OK
```

- **与 §三.0 事前论证对照**：期望"恰 1 枚＝`A_due_intent_inside_the_quiet_window_…_stays_observable`（`Assert.Single(PendingDeferrals)` 读到空）"，实测正是这一枚、消息正是 `The collection was empty`；事前论证的第二句"`A_quiet_deferred_intent_advances_once_...` 断的是补推后离表，本来期望空 ⇒ 仍绿"也被本轮 `passed=756` 证实（该格未进红集）。⇒ **清洁红 1 枚，形状相符**。

#### R4｜`AdvancementCoordinator.cs:85` 调用预算 `<`→`<=`（定向额度 5/8）

| 项 | 读数 |
|---|---|
| ① 文件:行 | `src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs:85` |
| ② 改前原文 | `        if (remainingCalls < request.Budget.RequiredCallsPerAdvancement)` |
| ③ 改后原文 | `        if (remainingCalls <= request.Budget.RequiredCallsPerAdvancement)` |
| ④ `git diff --stat` | `AdvancementCoordinator.cs \| 2 +-` → `1 file changed, 1 insertion(+), 1 deletion(-)`（单 hunk；方向类突变，未新增任何数字门槛） |
| ⑤ 跑法整行 | `dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave29-1024/out/trx-R4" --logger trx`（工作目录＝载体；未带 `--no-build`）。发起＝`python .local-notes/wave29-1024/round.py R4 --probe "…/probe/R4-call-budget-boundary-flipped.cs" --target "…/AdvancementCoordinator.cs"` |
| ⑥ TRX＋`<Counters>` | `out/trx-R4` 数满＝`total=1 trx=1`：`24782_PHYTHLINK_2026-10-27_23_34_16.trx`；`total=759`／`executed=757`／`passed=756`／`failed=1`／`error=0`／`notExecuted=0`／其余全 0 |
| ⑦ 红格 | 恰 1 枚＝`Pry.Core.Tests.BoundedAdvancementPrototypeTests.An_advancement_that_exactly_consumes_the_remaining_call_budget_is_allowed`。消息＝`Assert.Equal() Failure: Values differ` / `Expected: Advanced` / `Actual: StoppedByBudget`（恰好花完剩余预算被误判成耗尽）。TRX 红点行号照抄＝`…BoundedAdvancementPrototypeTests.cs:line 245`；源码该行文本＝判据文件第 245 行 `        Assert.Equal(AdvancementOutcome.Advanced, readout.Outcome);` ⇒ 相同 |
| ⑧ 还原命令 | `git -C <载体> cat-file blob HEAD:src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs > 同路径` |
| ⑨ 还原后 `git status --short` 行数 | **0 行** |
| ⑩ SHA-256 | 覆盖前＝`90947162…`（True）；突变态＝`d08e4c644fdf21d91b39b1c6ddaf8051b7b26c669e6e1490b74b3b7a4dcf6a70`（== §三.0 R4 预合值）；还原后＝`90947162…`（True）。判据仍 `1fd944f9…` |
| ⑪ 当轮预检 | **P15**（`out/preflight-P15.txt`，23:34:11，`VERDICT=BUILD_OK`，`free_memory_gb=7.3`；距 P14 69 秒，非 HOLD 重跑；前台窗已换成 `ChatGPT` 普通窗口，`covers=False borderless=False`） |

P15 整段：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: ChatGPT pid=16348 class=Chrome_WidgetWin_1 style=0x14C70000 rect=-1633,276,1285x811 screen=1920x1080 covers=False borderless=False title=ChatGPT
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21788, NVIDIA Overlay pid=18308, explorer pid=7404
gpu_util=0% used=2147MB
free_memory_gb=7.3
VERDICT=BUILD_OK
```

- **与 §三.0 事前论证对照**：期望"恰 1 枚＝`An_advancement_that_exactly_consumes_the_remaining_call_budget_is_allowed`"，实测正是这一枚。事前论证另两句"零预算与耗尽两格期望的就是拦（`remainingCalls` 为 0 时 `<`/`<=` 同判）""费用维度走另一道比较不受影响"同时被证实：`An_exhausted_call_budget_stops_…`、`A_budget_that_is_zero_at_entry_…`、`A_cost_budget_running_out_…` 三枚都在 `passed=756` 内。⇒ **清洁红 1 枚，形状相符**。

#### R5｜`DueIntentRegistry.cs:69` 补投顺序 `OrderBy`→`OrderByDescending`（定向额度 6/8）

| 项 | 读数 |
|---|---|
| ① 文件:行 | `src/Pry.Core/Narrative/Advancement/DueIntentRegistry.cs:69` |
| ② 改前原文 | `            .OrderBy(x => x.Registration.RegisteredSequence)` |
| ③ 改后原文 | `            .OrderByDescending(x => x.Registration.RegisteredSequence)` |
| ④ `git diff --stat` | `DueIntentRegistry.cs \| 2 +-` → `1 file changed, 1 insertion(+), 1 deletion(-)`（单 hunk；方向类突变） |
| ⑤ 跑法整行 | `dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave29-1024/out/trx-R5" --logger trx`（工作目录＝载体；未带 `--no-build`）。发起＝`python .local-notes/wave29-1024/round.py R5 --probe "…/probe/R5-replay-order-flipped.cs" --target "…/DueIntentRegistry.cs"` |
| ⑥ TRX＋`<Counters>` | `out/trx-R5` 数满＝`total=1 trx=1`：`24782_PHYTHLINK_2026-10-27_23_35_27.trx`；`total=759`／`executed=757`／`passed=756`／`failed=1`／`error=0`／`notExecuted=0`／其余全 0 |
| ⑦ 红格 | 恰 1 枚＝`Pry.Core.Tests.BoundedAdvancementPrototypeTests.After_a_clock_forward_jump_due_intents_are_replayed_in_registration_order`。消息＝`Assert.Equal() Failure: Strings differ` / `Expected: "intent-first,intent-second,intent-third"` / `Actual: "intent-third,intent-second,intent-first"`（pos 7 起反向）。TRX 红点行号照抄＝`…BoundedAdvancementPrototypeTests.cs:line 266`；源码该行文本＝判据文件第 266 行 `        Assert.Equal("intent-first,intent-second,intent-third",`（断言文本起始行，跨两行书写）⇒ 相同 |
| ⑧ 还原命令 | `git -C <载体> cat-file blob HEAD:src/Pry.Core/Narrative/Advancement/DueIntentRegistry.cs > 同路径` |
| ⑨ 还原后 `git status --short` 行数 | **0 行** |
| ⑩ SHA-256 | 覆盖前＝`9bf4dc31bd3d1c724a804f2dc01334806dada6a1c88861eb07f89fc0897ce5a1`（True）；突变态＝`32d41cf573b2c1a8f3a753f1f8982f09400feba19819e43af1309d19ab221156`（== §三.0 R5 预合值）；还原后＝`9bf4dc31…`（True，与 §八.0 本刀重取值相同）。判据仍 `1fd944f9…` |
| ⑪ 当轮预检 | **P16**（`out/preflight-P16.txt`，23:35:22，`VERDICT=BUILD_OK`，`free_memory_gb=7.9`；距 P15 71 秒，非 HOLD 重跑） |

P16 整段：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: ChatGPT pid=16348 class=Chrome_WidgetWin_1 style=0x14C70000 rect=-1633,276,1285x811 screen=1920x1080 covers=False borderless=False title=ChatGPT
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21788, NVIDIA Overlay pid=18308, explorer pid=7404
gpu_util=2% used=2135MB
free_memory_gb=7.9
VERDICT=BUILD_OK
```

- **与 §三.0 事前论证对照**：期望"恰 1 枚＝`After_a_clock_forward_jump_due_intents_are_replayed_in_registration_order`"，实测正是这一枚。这一轮同时**给上一把 `77a7d91` 那枚判据形状更正提供实测凭据**（§七.4 第 2 条与工单 §〇.4 说那处更正"是为了让 R5 的红只落一枚格"）：`Replay_delivers_each_due_intent_exactly_once_across_two_clock_jumps_and_loses_none` 本轮**没有红**（在 `passed=756` 内）＝那格的补投集合断言确实已与顺序无关；`StillPending` 只有一条、`Registrations` 用另一行 `OrderBy`（第 45/82 行未动）也一并证实。⇒ **清洁红 1 枚，形状相符，且上一把的形状更正被实测确认**。

#### R6｜`AdvancementCoordinator.cs:50-51` 删掉来源缺失的观察日志登记（定向额度 7/8）

| 项 | 读数 |
|---|---|
| ① 文件:行 | `src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs:50-51`（同一条 `Add(…)` 语句的两个物理行） |
| ② 改前原文 | `            _observations.Add(new AdvancementObservation(missing.Outcome, request.IntentKey,`<br>`                request.RequestId, missing.OutcomeReason));` |
| ③ 改后原文 | （两行均被删除，改后无对应行；`git diff --stat`＝`1 file changed, 2 deletions(-)`） |
| ④ `git diff --stat` | `AdvancementCoordinator.cs \| 2 --` → `1 file changed, 2 deletions(-)`（单 hunk，一条逻辑调用的 2 个物理行，与 §三.0 末段"R6 删的是同一条语句的 2 个物理行"逐字相符） |
| ⑤ 跑法整行 | `dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave29-1024/out/trx-R6" --logger trx`（工作目录＝载体；未带 `--no-build`）。发起＝`python .local-notes/wave29-1024/round.py R6 --probe "…/probe/R6-missing-source-not-logged.cs" --target "…/AdvancementCoordinator.cs"` |
| ⑥ TRX＋`<Counters>` | `out/trx-R6` 数满＝`total=1 trx=1`：`24782_PHYTHLINK_2026-10-27_23_36_45.trx`；`total=759`／`executed=757`／`passed=756`／`failed=1`／`error=0`／`notExecuted=0`／其余全 0 |
| ⑦ 红格 | 恰 1 枚＝`Pry.Core.Tests.BoundedAdvancementPrototypeTests.An_advancement_whose_source_cannot_be_pointed_to_fails_with_a_named_reason_and_stays_in_the_log`。消息＝`Assert.Equal() Failure: Strings differ` / `Expected: "FailedForMissingSource:intent-nosrc\|FailedForMissi"···` / `Actual: ""`（结局仍然返回，但观察日志一行不剩＝"静默丢弃"的形状）。TRX 红点行号照抄＝`…BoundedAdvancementPrototypeTests.cs:line 353`；源码该行文本＝判据文件第 353 行 `        Assert.Equal("FailedForMissingSource:intent-nosrc\|FailedForMissingSource:intent-nosrc",`（断言文本起始行，跨两行书写）⇒ 相同 |
| ⑧ 还原命令 | `git -C <载体> cat-file blob HEAD:src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs > 同路径` |
| ⑨ 还原后 `git status --short` 行数 | **0 行** |
| ⑩ SHA-256 | 覆盖前＝`90947162…`（True）；突变态＝`af66ff565013e806f31c76a213d0940371cd011a9721b17426c690fe9d33953c`（== §三.0 R6 预合值）；还原后＝`90947162…`（True）。判据仍 `1fd944f9…` |
| ⑪ 当轮预检 | **P17**（`out/preflight-P17.txt`，23:36:41，`VERDICT=BUILD_OK`，`free_memory_gb=7.7`；距 P16 79 秒，非 HOLD 重跑） |

P17 整段：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: ChatGPT pid=16348 class=Chrome_WidgetWin_1 style=0x14C70000 rect=-1633,276,1285x811 screen=1920x1080 covers=False borderless=False title=ChatGPT
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21788, NVIDIA Overlay pid=18308, explorer pid=7404
gpu_util=0% used=2107MB
free_memory_gb=7.7
VERDICT=BUILD_OK
```

- **与 §三.0 事前论证对照**：期望"恰 1 枚＝`An_advancement_whose_source_cannot_be_pointed_to…`（观察日志 joined 期望两行、实际空串）"，实测正是这一枚、消息正是 `Actual: ""`；事前论证那句"三条来源探针不判日志"也被证实——`Missing_source_is_refused_for_every_trigger_kind_…(asked: UserEvent/DueIntent/BackgroundScene)` 三枚都在 `passed=756` 内。⇒ **清洁红 1 枚，形状相符**。
- 编号说明（照实登记，避免与上一把台账混淆）：本刀的预检按"每一枚 `dotnet` 前一枚"逐轮取，P10 起连续读到 `BUILD_OK`，因此 §〇.5 那 8 枚"HOLD 之后的重跑"额度**一枚都没动用**（0/8），台账编号只是沿上一把 P1–P9 之后继续排（R7 用 P18、收尾全量用 P19），并未另起 `P1` 一套。

#### R7｜`DueIntentRegistry.cs:68` 补投筛选 `<=`→`<`（定向额度 8/8，额度用满）

| 项 | 读数 |
|---|---|
| ① 文件:行 | `src/Pry.Core/Narrative/Advancement/DueIntentRegistry.cs:68` |
| ② 改前原文 | `            .Where(x => x.Replayed is false && x.Registration.DueUtc <= jumpedToUtc)` |
| ③ 改后原文 | `            .Where(x => x.Replayed is false && x.Registration.DueUtc < jumpedToUtc)` |
| ④ `git diff --stat` | `DueIntentRegistry.cs \| 2 +-` → `1 file changed, 1 insertion(+), 1 deletion(-)`（单 hunk；方向类突变，`<=`↔`<` 同族于 R2／R4） |
| ⑤ 跑法整行 | `dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave29-1024/out/trx-R7" --logger trx`（工作目录＝载体；未带 `--no-build`）。发起＝`python .local-notes/wave29-1024/round.py R7 --probe "…/probe/R7-due-instant-flipped.cs" --target "…/DueIntentRegistry.cs"` |
| ⑥ TRX＋`<Counters>` | `out/trx-R7` 数满＝`total=1 trx=1`：`24782_PHYTHLINK_2026-10-27_23_37_54.trx`；`total=759`／`executed=757`／`passed=756`／`failed=1`／`error=0`／`notExecuted=0`／其余全 0 |
| ⑦ 红格 | 恰 1 枚＝`Pry.Core.Tests.BoundedAdvancementPrototypeTests.A_due_intent_that_has_not_reached_its_due_instant_is_left_pending_and_still_reported`。消息＝`Assert.Single() Failure: The collection was empty`（跳到 `due` 那一瞬不再算"到点"，`atDue.Replayed` 读空）。TRX 红点行号照抄＝`…BoundedAdvancementPrototypeTests.cs:line 307`；源码该行文本＝判据文件第 307 行 `        var replayed = Assert.Single(atDue.Replayed);` ⇒ 相同 |
| ⑧ 还原命令 | `git -C <载体> cat-file blob HEAD:src/Pry.Core/Narrative/Advancement/DueIntentRegistry.cs > 同路径` |
| ⑨ 还原后 `git status --short` 行数 | **0 行** |
| ⑩ SHA-256 | 覆盖前＝`9bf4dc31…`（True）；突变态＝`970d3628f4ee8481897b6c1f80dc893f2700f43f2978809398932fd167ad7bec`（== §三.0 R7 预合值，即上一把换法后那枚，非被推翻的 `cdd18e3f…`）；还原后＝`9bf4dc31…`（True）。判据仍 `1fd944f9…` |
| ⑪ 当轮预检 | **P18**（`out/preflight-P18.txt`，23:37:49，`VERDICT=BUILD_OK`，`free_memory_gb=8.1`；距 P17 68 秒，非 HOLD 重跑） |

P18 整段：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: ChatGPT pid=16348 class=Chrome_WidgetWin_1 style=0x14C70000 rect=-1633,276,1285x811 screen=1920x1080 covers=False borderless=False title=ChatGPT
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21788, NVIDIA Overlay pid=18308, explorer pid=7404
gpu_util=2% used=2105MB
free_memory_gb=8.1
VERDICT=BUILD_OK
```

- **与 §三.0 事前论证对照**：期望"恰 1 枚＝`A_due_intent_that_has_not_reached_its_due_instant_is_left_pending_and_still_reported`"，实测正是这一枚。事前论证里为那枚**未跑**突变（`cdd18e3f…`，删 `entry.Replayed = true;`）挡下的两枚连带格 `Replay_delivers_each_due_intent_exactly_once_…` 与 `After_a_clock_forward_jump_…` 本轮**都没红**（在 `passed=756` 内）⇒ R7 的"同族但单点"换法在实测上成立，上一把 §三.0 顶部那次事前更正**拿到了红证级的确认**。
- 定向额度到此**用满 8/8**（G0＋R1–R7）。按工单 §一.1／§〇.7 本刀**没有第 9 枚定向额度**，故那枚"一次响 3 枚"的未档探针本刀不跑（工单 §一 末段的例外条件"因作废空出一枚额度"未发生：七枚轮次全部形状相符，无一作废）。

#### F｜收尾解决方案级全量（全量额度 1/1 用满）

| 项 | 读数 |
|---|---|
| ① 文件:行 | **本轮无突变覆盖**（收尾全量；四枚文件＝载体 HEAD 字节，R7 之后已还原并核过） |
| ②③ 改前／改后原文 | N/A；`git diff --stat`＝**0 行**，`git status --short`＝**0 行**（轮前与轮后各数一次） |
| ⑤ 跑法整行 | `dotnet test Pry.slnx -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave29-1024/out/trx-F" --logger trx`（工作目录＝载体；**未带** `--no-build`——上一轮 R7 把突变态 IL 留在盘上，按波 28 §七.3／§八.4.3 口径让它自行增量重建）。发起＝`python .local-notes/wave29-1024/round.py F --full` |
| ⑥ TRX 数满后再逐份读 `<Counters>` | `ls` 数满＝`total=4 trx=4`（目录 `out/trx-F`）。逐份：`24782_PHYTHLINK_2026-10-27_23_39_08.trx`＝Client 面 `total=108`／`executed=108`／`passed=108`／`failed=0`；`…23_39_09.trx`＝Core 面 `total=759`／`executed=757`／`passed=757`／`failed=0`（同份内 `outcome="NotExecuted"` 两枚＝`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`、`CoreTests.Configured_sense_voice_model_can_run_offline`）；`…23_39_10.trx`＝App 面 `total=233`／`executed=233`／`passed=233`／`failed=0`；`…23_39_10[1].trx`＝Api 面 `total=280`／`executed=280`／`passed=280`／`failed=0`。四份归属由各份 `testName`／`className` 前缀逐字确认（`Pry.Client.Tests.*`／`Pry.Core.Tests.*`／`Pry.App.Tests.*`／`Pry.Api.Tests.*`） |
| ⑦ 红格 | **零枚红**：四份 `failed=0`、`error=0`，`outcome="Failed"` 节点计数 0 |
| ⑧ 还原命令 | 本轮无覆盖 ⇒ 无需还原；核对用命令＝`git -C <载体> cat-file blob HEAD:<path>` 与 `sha256sum <path>` 逐枚对 |
| ⑨ `git status --short` 行数 | **0 行** |
| ⑩ SHA-256（终态，四枚 HEAD blob 与盘上字节各取一次） | `AdvancementCoordinator.cs` `90947162fed797f8…`＝盘上同值；`DueIntentRegistry.cs` `9bf4dc31bd3d1c72…`＝盘上同值；`AdvancementContracts.cs` `259474dcccbfb1f0…`＝盘上同值；判据 `1fd944f94d399a8f…`＝盘上同值 ⇒ 四枚与 §八.0 入场重取值**逐字符相同**，本刀对判据字节零写入这条有终态凭据 |
| ⑪ 当轮预检 | **P19**（`out/preflight-P19.txt`，23:39:04，`VERDICT=BUILD_OK`，`free_memory_gb=8`；距 P18 75 秒，非 HOLD 重跑） |

P19 整段：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: ChatGPT pid=16348 class=Chrome_WidgetWin_1 style=0x14C70000 rect=-1633,276,1285x811 screen=1920x1080 covers=False borderless=False title=ChatGPT
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): TextInputHost pid=21788, NVIDIA Overlay pid=18308, explorer pid=7404
gpu_util=1% used=2118MB
free_memory_gb=8
VERDICT=BUILD_OK
```

- **出口读数（右端由上面四份原始 TRX 当场求和取出，不是 `1357＋23` 的减法预写）**：

```
在册（total）  ＝ 108 ＋ 759 ＋ 233 ＋ 280 ＝ 1380
executed      ＝ 108 ＋ 757 ＋ 233 ＋ 280 ＝ 1378
passed        ＝ 108 ＋ 757 ＋ 233 ＋ 280 ＝ 1378
failed        ＝   0 ＋   0 ＋   0 ＋   0 ＝ 0
跳过（按 outcome="NotExecuted" 点名）＝ 0 ＋ 2 ＋ 0 ＋ 0 ＝ 2 枚（同名同两枚，与波 28 §八.7 那两条逐字相同；`notExecuted` 计数器恒 0，不看）
分项          ＝ Client 108／App 233／Api 280／Core 759（Core executed 757）
```

- 与起点对照（起点＝§八.0 从波 28 报告 §八.7 原文照抄的 1357／executed 1355／failed 0／跳过 2）：差值＝23 枚在册、23 枚 executed，唯一去处＝Core **736 → 759**（＋23），与 §二 顶账表那格"本刀新格数 N＝23"自洽。**本刀不据此自封在册基线，也不写状态词**（工单 §二：归位与顶账由守门人做）。
- `measure-fixed-eval.ps1` 在四份 TRX 数满并取完读数**之后**才跑，且带 `-ResultsDirectory`（它向该目录写 `fixed-eval-coverage.json`）：整段输出＝`Catalog valid: 76 permanent scenarios. Mappings are partial evidence only.` ＋ `Passed=1378; Failed=0; Other=2.`，与上面求和逐字自洽（`Other=2`＝那两条同名跳过）；工作目录＝载体，`repoRoot` 因此解析到载体自己的 `docs/eval/scenarios.json` 与 `tests/**`，主树零写入。

### 八.2 本刀终账（有红／无红逐格点名）

**七枚突变的红格归属（逐枚点名到各自 TRX 的 `outcome="Failed"` 节点，七轮全部恰 1 枚红、无一形状不符）**：

| 轮 | 突变（文件:行／改法） | 实测红格（TRX 逐字） | 消息要点 | TRX 红点行＝源码 assert 行 | 与 §三.0 事前论证 |
|---|---|---|---|---|---|
| R1 | `AdvancementCoordinator.cs:133` 背景场景臂→`DueIntentReference` | `Each_trigger_kind_enters_the_one_advancement_entry_and_names_its_own_reference(asked: BackgroundScene, expectedReference: "bg-1")` | Expected `"bg-1"`／Actual `"due-1"` | 63＝63 | 相符（恰 1） |
| R2 | `:59` 冷却外沿 `>`→`>=` | `An_intent_resubmitted_at_the_instant_the_cooldown_window_closes_advances_again` | Expected `Advanced`／Actual `DeferredByCooldown` | 120＝120 | 相符（恰 1） |
| R3 | `:78` 删静默推迟登记行 | `A_due_intent_inside_the_quiet_window_is_deferred_until_the_window_closes_and_stays_observable` | `Assert.Single() Failure: The collection was empty` | 143＝143 | 相符（恰 1） |
| R4 | `:85` 调用预算 `<`→`<=` | `An_advancement_that_exactly_consumes_the_remaining_call_budget_is_allowed` | Expected `Advanced`／Actual `StoppedByBudget` | 245＝245 | 相符（恰 1） |
| R5 | `DueIntentRegistry.cs:69` `OrderBy`→`OrderByDescending` | `After_a_clock_forward_jump_due_intents_are_replayed_in_registration_order` | `"intent-first,intent-second,intent-third"` → `"intent-third,intent-second,intent-first"` | 266＝266 | 相符（恰 1；`77a7d91` 的形状更正被实测确认） |
| R6 | `:50-51` 删来源缺失观察日志 | `An_advancement_whose_source_cannot_be_pointed_to_fails_with_a_named_reason_and_stays_in_the_log` | Expected `"FailedForMissingSource:…\|…"`／Actual `""` | 353＝353 | 相符（恰 1） |
| R7 | `DueIntentRegistry.cs:68` 到点判定 `<=`→`<` | `A_due_intent_that_has_not_reached_its_due_instant_is_left_pending_and_still_reported` | `Assert.Single() Failure: The collection was empty` | 307＝307 | 相符（恰 1；两枚连带格未红，换法成立） |

**23 枚新格的覆盖终账（有清洁红 7 枚／无红 16 枚，无红逐格分类，不含"结构上够不着"这种未经穷举的说法）**：

| 归类 | 枚数 | 格名逐字 |
|---|---|---|
| 清洁红（恰 1 枚红、单枚归因到一轮突变） | **7** | 上表 R1–R7 那七枚格名 |
| 无红＝**定向额度用尽**（红方向已在 §三.0 事前论证点名，但额度只有 G0＋R1–R7＝8 枚，无复取位） | **1** | `A_late_advancement_carrying_a_superseded_effect_version_is_refused_and_writes_nothing`（红方向＝`request.ExpectedEffectVersion != _effectVersion` 改自比较，波 28 GK-R10 同族；§三.0 末段押给下一把或守门人，本刀照实登记，未挤占任何已排轮次） |
| 无红＝**定向额度用尽**（同上；红方向＝往产品文件加一枚公开入口方法，属新增而非摘闸） | **1** | `The_advancement_coordinator_exposes_exactly_one_public_entry_method`（本刀绿基线在 G0 已取到该格 Passed 读数；反证轮无额度） |
| 无红＝**本刀不自残产品码取红**（红方向＝往新文件塞旧链类型名／`static readonly` 默认值，正面撞上工单 §〇.7 与 §一.3 的零门槛约束） | **2** | `Advancement_prototype_sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites`、`Advancement_prototype_declares_no_source_side_default_for_windows_or_budgets`（两格各带**阳性探针**，G0 实测该探针在跑：两枚均 Passed；§二 末段那句"扫描函数坏掉时探针先红"这一形状本刀未取反证读数，押下一把） |
| 无红＝**已被同一轮连带覆盖、无需单独一轮**（同一条产品语句的另一个出口，本轮红集里没有它＝它守的是另一道闸） | **6** | `The_same_intent_resubmitted_inside_the_cooldown_window_is_deferred_and_names_the_earliest_next_instant`、`A_quiet_deferred_intent_advances_once_the_window_closes_and_leaves_the_pending_list`、`An_exhausted_call_budget_stops_advancement_and_names_consumed_remaining_and_the_dimension`、`A_budget_that_is_zero_at_entry_is_a_distinct_shape_from_one_that_ran_out`、`A_cost_budget_running_out_stops_advancement_while_calls_are_still_available`、`Replay_delivers_each_due_intent_exactly_once_across_two_clock_jumps_and_loses_none`——登记口径要如实：这六枚**不是"已证会响"**，它们在 R2／R3／R4／R5／R7 五轮里都保持绿＝只证明"那些改法不影响它们"，真正的红方向（各闸的独立摘除、以及 `entry.Replayed = true;` 那枚一次响三枚的突变）本刀**没有额度** |
| 无红＝**探针面/引用面格，红方向未排进本刀七枚** | **6** | `Each_trigger_kind_enters_the_one_advancement_entry_and_names_its_own_reference(asked: UserEvent, …)`、`(asked: DueIntent, …)` 两枚（R1 只打掉 BackgroundScene 那条臂；另两条臂的突变各需单独一轮，无额度）、`Missing_source_is_refused_for_every_trigger_kind_with_the_same_named_failure(asked: UserEvent/DueIntent/BackgroundScene)` 三枚（同一道来源闸的三个入参形状；R6 打的是日志行不是拒绝本身，故三枚全程绿）、`A_successful_advancement_carries_the_source_reference_into_the_record_it_produces`（红方向＝成功路径不写入 `SourceId`/`SourceVersion`，无额度） |
| **合计** | **7＋16＝23** | 与 §二 顶账的 N＝23 自洽 |

- 一句话账（不代守门人判）：**G0 全绿 23/23、七枚突变七枚清洁红、F 求和 1380／1378／0／2**；上面 16 枚无红格**一枚都没有被本刀写成红**。
- 未跑的那枚三响突变（`probe/unrun-replay-mark-removed.cs`，`cdd18e3f…`）本刀**未施加、未起跑**，盘上字节与 §三.0 登记值未变；工单 §一 末段的例外（"因作废空出一枚额度"）没发生——七轮全部形状相符，没有作废轮，所以也没有第 9 枚定向可用。

### 八.3 未验证（照抄上一把 §五 那九条并逐条更新）

0. **上一把的第 0 条在本刀已被替换**：它原写"本刀 23 枚新格全部零读数"。实测＝23 枚全部有 G0 读数（全 Passed），其中 7 枚有"改坏产品码之下响过"的红证（§八.2），其余 16 枚无红、原因逐格点名。**但这条的结论部分照旧有效**：本刀**不据此自封在册基线、不写归位**（工单 §二：归位与顶账由守门人做；§〇.4 起点 1357 只成立于波 28 那四份 TRX，本刀出口 1380 由本刀 §八.1 F 那四份 TRX 当场求和取出，两者之间的取舍不由本 lane 判）。
1. **真实模型对照**：零读数，本刀没调过任何模型端点；要跑需作者一句"可以跑"。
2. **生产接线**：本片仍**零生产调用方**（本刀未新增任何引用；§一 末段那三条 grep 形状本刀未重跑，因为零 `src/**` 写入，字节未变）。004 生命周期与 005 原子提交的编排面与端点一律没接；接线时机＝U-9 仍未拍。
3. **UI 可见行为**：零读数，本刀未触碰任何界面文件。
4. **生产库**：零接触。`%LOCALAPPDATA%\PryCompanion` 未被当过载体，无 SQLite 读写（F 那轮的 App/Api 面测试跑的是各自既有夹具，本刀未新增落库面）。
5. **待标定参数权威值**：冷却时长／静默窗口／预算默认值全部未定；本刀**没有"顺手补个数"**——七枚突变里四枚是方向类（`>`→`>=`、`<`→`<=`、`OrderBy`→`OrderByDescending`、`<=`→`<`）、两枚删行、一枚改引用臂，全部零新增数字门槛。
6. **落库／幂等／崩溃恢复／并发两枚 `Advance`**：仍零读数（`List` 非线程安全这条从未取过读数，本刀没跑并发轮）。
7. **旧链删除片与"构建时不把旧链打进包"那道门**：**未开始**，本刀未碰。三句话照旧不许出现：新链已接线／旧链可删／不进包已做到。
8. **本刀新格的换人复采**：本刀正是波 29 那 23 枚格的第一双执行手（判据由上一把写、红由本刀取），所以**第二换手仍押在下一把复采刀**；旧欠 75 格照旧不减。**换人复采与"红证"是两件事**：本刀的红证满足的是工单 §二 的归位凭据面，不构成换手凭据。
9. **`measure-fixed-eval.ps1` 覆盖率**：本刀**已跑**（F 之后、带 `-ResultsDirectory`），读数 `Catalog 76／Passed 1378／Failed 0／Other 2`，与四份 TRX 求和自洽；它只证"映射到的断言"，映射本身是 partial evidence（脚本自己那句话），不证产品行为正确。
10. **（本刀新增一条）焦点闸的资源面**：本刀 10 枚预检的 `free_memory_gb` 落在 6.7–8.2 区间（低于上一把台账的 10.7–11.1），仪器全部判 `BUILD_OK`；本刀逐轮核过 `failed`／`error` 计数与日志形态，**未出现** SQLite `disk I/O`／`out of memory` 那类读数，所以没有需要作废的轮次。这条登记为"读数形状"，不登记为"机器状态结论"。

### 八.4 交付面与归位边界（本 lane 只备材料）

- 本刀在共享主树的**唯一**写入＝本报告（工单 §〇.4 ①）；`src`／`tests`／`scripts`／`Pry.slnx`／`Directory.Build.props` 一字未写；主树 `git status --short --branch` 行数从入场 9 行变到 10 行＝**只多了本报告这一枚 ` M`**，他人那两枚 `M ` 暂存项与五枚 `??` 未跟踪资产本刀未读内容、未改、未提交、未 `stash`／`clean`／`rm`；` M docs/plans/PROJECT_PLAN.md` 是在飞的他人改动，本刀对它**零写入**（只按工单 §〇.1 读过 §4.2 的 HDS-007 行与 §9 最新两条，区分登记见 §八.5）。
- 主树零 `git` 写操作（无 `commit`／`add`／`checkout`／`reset`／`cherry-pick`／`merge`／`tag`／`push`／`apply`）；载体也零提交（本刀只在载体工作树上做临时覆盖与逐字节还原，载体线尾仍是 `663d2c9`）。
- 载体终态＝`git status --short` **0 行**、`git diff --stat` 0 行、四枚 HEAD blob 与盘上字节逐枚相同（§八.1 F 行 ⑩）。
- 仪器与全部原始凭据＝`.local-notes/wave29-1024/`（gitignored）：`out/preflight-P10.txt`…`P19.txt` 十枚、`out/build-B1.txt`、`out/run-G0.txt`／`run-R1..R7.txt`／`run-F.txt`、`out/w30-round-R*.txt`／`w30-round-F.txt`（runner 原始打印）、12 枚 `.trx`（G0 1＋R1–R7 7＋F 4）、`out/trx-F/fixed-eval-coverage.json`；本刀自写的取数脚本＝`out/w30-cells.js`、`out/w30-failmsg.js`、`out/w30-roundinfo.js`（上一把的 `round.py` 一字未改）。
- 交给守门人核的 §二 四件事的落点：① 红格归属＝§八.2 表（逐枚点名到 TRX 节点，七轮均恰 1 枚红，无批次红署名）；② 常绿门——本刀的实测就是这一件：**没有一枚常绿门被实测揭穿**（七枚突变各自只打掉一枚格，且 G0 23/23 绿；两枚源码扫描格带阳性探针、本轮读数 Passed）；③ 收尾四份 TRX 先数后读＋求和式＝§八.1 F 行与那段求和；④ §一 五条是否每条都拿到会响的格＝**五条全拿到**：§一.1→R1、§一.2→R2 与 R3（冷却／静默各一枚）、§一.3→R4、§一.4→R5 与 R7（补投顺序／到点判定）、§一.5→R6（来源缺失留行）。
- `dotnet` 实际调用次数＝**10 次**（B1 构建 1／定向 8／收尾全量 1，正好用满工单 §〇.7 的 ≤1／≤8／≤1），另有 `measure-fixed-eval.ps1` 一次（PowerShell，非 `dotnet`）；焦点预检 10 枚全部 `BUILD_OK`、`HOLD` 0 枚、"HOLD 之后重跑"额度 0/8。

### 八.5 文件面自证与盘上凭据清点（23:42 当场）

- **§〇–§七 原文一字不改的凭据**：`git diff --numstat -- docs/eval/reports/2026-10-24-hds007-bounded-advancement-prototype.md` 的**删除列恒为 0**（23:42 首测＝`498  0`，本节字段写完后的复测＝`507  0`；0 删除⇒ 上一把 §〇–§六 与守门人 §七 的既有行没有任何一行被改、重排或"顺手更正"，本刀全部是文件末尾追加。新增列随本刀继续落盘而增长，故这里给的是两次实测值而不是终值）。
- 主树 `git status --short --branch`＝**10 行**（入场 9 行 ＋ 本刀的这枚 ` M` 报告）；他人那两枚 `M ` 暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）与五枚 `??` 未跟踪资产本刀**未读内容**、未改、未提交、未 `stash`／`clean`／`rm`；` M docs/plans/PROJECT_PLAN.md` 是他人（守门人一线）的在飞改动，本刀**只按工单 §〇.1 读过它的 §4.2 HDS-007 行与 §9 最新两条**（这是必读面），对它零写入——终核凭据＝`git diff --name-only` 里属于本刀的只有本报告一枚（另一枚 ` M` 是 PROJECT_PLAN，本刀没动过一个字节，本刀对它的唯一动作是 Read/Grep 指定行段）。
- 盘上凭据清点（`git ls-files` 之外的仪器面）：`.trx` **12 枚**＝`trx-G0` 1 ＋ `trx-R1`…`trx-R7` 各 1 ＋ `trx-F` 4；结果目录 **9 个**（各轮独立、无复用，无 `trx-B1`，因 B1 是构建）；`dotnet` 日志 10 份＝`build-B1.txt` ＋ `run-G0.txt`／`run-R1.txt`…`run-R7.txt`／`run-F.txt`；预检 **10 枚**＝`preflight-P10.txt`…`preflight-P19.txt`；runner 原始打印 8 份＝`w30-round-R1.txt`…`w30-round-R7.txt` ＋ `w30-round-F.txt`；`out/trx-F/fixed-eval-coverage.json` 1 份（`measure-fixed-eval.ps1` 写在结果目录内）。
- 载体终态（23:40:17 当场）：HEAD 仍＝`663d2c9d78d7f607137f10cd2af09069b0a4f841`（本刀零提交、零 ref 写），`git status --short`＝**0 行**，`git diff --stat`＝0 行，四枚文件"盘上字节＝HEAD blob"逐枚相同（`90947162…`／`9bf4dc31…`／`259474dc…`／`1fd944f9…`）⇒ **没有任何一枚突变残留在树上**，判据字节全程未被写入。
- 本刀没有做的（照实登记，不写成做过）：未新建产品文件或判据文件；未追押下的 4 枚无红格（`The_advancement_coordinator_exposes_exactly_one_public_entry_method`、`A_late_advancement_carrying_a_superseded_effect_version_…`、两枚源码扫描格）；未跑那枚三响探针；未接生产；未碰旧链删除片；未碰 UI；未碰 `%LOCALAPPDATA%\PryCompanion`；未在 §9/§4.2 登记状态（那是守门人的账）；未跑 `git diff --check`／未提交（工作规则第 9 条那三步属归位那一手，且本刀的文件面只有本报告一枚，不自行提交）。

## 九 守门人划账（签名 `eval-QA-WAVE30-GK-1024`，2026-10-27 23:5x 当场，零 `dotnet`）

本节由守门人写。§〇–§六 是第二十九波 lane 原文、§七 是上一把守门人划账、§八 是第三十波 lane 原文，**三处一字未改**；本节只追加，并且下面每个读数都是我自己从盘上取的，不是对 lane 自述的复述。

### 九.0 时点更正（本刀与上一把都欠着的一条）

工单 `HDS-WAVE30-1024` 的出闸日写在文件名与正文里（2026-10-24 17:1x），lane 的起跑与交付在 **2026-10-27 23:20–23:42**，`date` 与全部 12 枚 TRX 的文件名时间戳、10 枚预检的 `LastWriteTime` 三处互相对上 ⇒ 跨了三天不是异常，是这把在队列里等了三天。本报告文件名仍叫 `2026-10-24-…`（§〇–§六 的落盘日），守门人此后所有登记一律按 **2026-10-27** 写日期，不再沿用出闸日。

### 九.1 工单 §二 那四件事，逐件用盘上凭据核

**核法**：我自己写了取数脚本 `.local-notes/wave29-1024/out/gk30-count.js`（只读 `.trx`），口径＝**只数 `<UnitTestResult` 开标签里带 `outcome="Failed"` 的那一枚**，并与同份的 `<Counters failed>` 对账；`<ResultSummary outcome="Failed">` 是文件级汇总、不是失败用例，不算数（这条是我自己先踩出来的，见 §九.4）。

1. **① 红格归属逐枚点名到 TRX 节点＝成立。** 七轮每轮 1 枚 TRX，`failedUnits=1` 且 `<Counters failed="1">` 同值（两侧独立）；七枚红格名逐字如下，与 lane §八.2 表**逐字符相同**，也与**上一把已提交的 §三.0 事前论证**（HEAD 版报告第 320–326 行）逐字符相同 ⇒ 归属是**先档后跑**，不是事后署名：

| 轮 | 我核到的红格（TRX `testName` 逐字） | 我核到的红点行 | `<Counters>` |
|---|---|---|---|
| R1 | `Each_trigger_kind_enters_the_one_advancement_entry_and_names_its_own_reference(asked: BackgroundScene, expectedReference: "bg-1")` | `:line 63` | 759／757／756／1 |
| R2 | `An_intent_resubmitted_at_the_instant_the_cooldown_window_closes_advances_again` | `:line 120` | 759／757／756／1 |
| R3 | `A_due_intent_inside_the_quiet_window_is_deferred_until_the_window_closes_and_stays_observable` | `:line 143` | 759／757／756／1 |
| R4 | `An_advancement_that_exactly_consumes_the_remaining_call_budget_is_allowed` | `:line 245` | 759／757／756／1 |
| R5 | `After_a_clock_forward_jump_due_intents_are_replayed_in_registration_order` | `:line 266` | 759／757／756／1 |
| R6 | `An_advancement_whose_source_cannot_be_pointed_to_fails_with_a_named_reason_and_stays_in_the_log` | `:line 353` | 759／757／756／1 |
| R7 | `A_due_intent_that_has_not_reached_its_due_instant_is_left_pending_and_still_reported` | `:line 307` | 759／757／756／1 |

  七轮全是**恰 1 枚红**、无批次红署给单枚突变；G0 同径核得 `failedUnits=0`、`passed=757`（23 枚新格全绿）；R7 那两枚连带格（`Replay_delivers_each_due_intent_exactly_once_…`／`After_a_clock_forward_jump_…`）在我数的 756 枚绿里，**没有**被算进红集 ⇒ lane §八.1 R7 那条"同族单点换法成立"是有盘面凭据的。
2. **② 有没有常绿门＝没有，而且这一件我自己验了最可疑的那两格。** 两枚源码扫描格（`Advancement_prototype_sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites`／`Advancement_prototype_declares_no_source_side_default_for_windows_or_budgets`）的形状是"扫描三枚产品文件＋自带阳性探针"，这种写法最容易写成空转门，所以我读了判据里那三处夹具（`BoundedAdvancementPrototypeTests.cs:39-42`／`:476-493`）：`PrototypeFiles` 是**写死的三枚文件名**（非空列表，循环必跑）；`PrototypeSource()` 在 `File.Exists` 上有 `Assert.True`，`RepositoryRoot()` 从 `AppContext.BaseDirectory` 上溯找 `Pry.slnx`、找不到也 `Assert.True` 红掉 ⇒ **路径或清单坏掉时这两格会红，不会绿**，不构成常绿门；两格各自的探针把五个旧链类型名与 `const`／`static readonly` 两种源侧默认值都点名断言了返回值形状，属真阳性对照。**仍欠的那一半**（把扫描函数本身弄坏、看探针是否先红）lane 自己登记没跑，押下一把（§九.5）。
3. **③ 收尾四份 TRX 先数后读＋求和式＝成立，我复算同值。** `out/trx-F/` 我 `ls` 数满 4 份，逐份 `<Counters>`＝108/108/108/0、759/757/757/0、233/233/233/0、280/280/280/0；求和 108＋759＋233＋280＝**1380**，executed 108＋757＋233＋280＝**1378**，failed **0**，跳过＝按 `outcome="NotExecuted"` 点名 **2 枚**（`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`、`CoreTests.Configured_sense_voice_model_can_run_offline`，与波 28 §八.7 那两条同名同枚；`notExecuted` 计数器恒 0，不看）。`measure-fixed-eval.ps1` 落盘的 `out/trx-F/fixed-eval-coverage.json` 我也读了：`suiteVersion 1.4.0`／`passed 1378`／`failed 0`／`other 2`／`evidenceDirectory` 指向 `trx-F`，与求和自洽；`Other=2` 就是那两条跳过，不是第三处来源。
4. **④ §一 五条是否每条都拿到会响的格＝五条全拿到**，并且我按"改动那一行是否真是该条范围的守门语句"对了一遍：三类触发同入口→R1（引用臂改写实测 Expected `bg-1`／Actual `due-1`）；冷却与静默各自可观察→R2（外沿 `>`→`>=`，Advanced→DeferredByCooldown）＋R3（删登记行，`Assert.Single()` 集合空）；费用-调用预算从入参传入→R4（`<`→`<=`，Advanced→StoppedByBudget，且七枚突变**零新增数字门槛**）；长离线恢复按时钟前跳补投不重复→R5（`OrderBy`→`OrderByDescending`）＋R7（到点判定 `<=`→`<`）；006 最小来源约束→R6（删来源缺失的观察日志登记行，Expected 那串 `FailedForMissingSource:…`／Actual 空）。

**签账资格（波 24／27／28 立的那条）**：取红那只手既不是判据字节的作者、也没改过一个判据字节。我这把的证明＝载体 `git log --oneline -- tests/Pry.Core.Tests/BoundedAdvancementPrototypeTests.cs` 三枚提交全属上一把（`ed7995c`／`77a7d91`／`663d2c9`）、**第三十波零新提交**（线尾仍 `663d2c9d78d7f607137f10cd2af09069b0a4f841`、`status --short` 0 行）、判据 SHA `1fd944f94d399a8f…` 入场自取值与 §八.1 F 行⑩终态值两侧同值。⇒ 资格成立。

### 九.2 归位裁定＝**归位**（与第二十九波那把的"不归位"正是同一条判据的两面）

第二十九波我扣住的唯一理由是"23 枚格一次都没跑过、七枚突变一枚都没响过"。这一把我自己数的 12 枚 TRX 把这个理由消掉了：**G0 全绿 23/23＋七枚单放突变七枚清洁红＋F 全量零红**，凭据面齐（§九.1）。字节写完不等于可归位；**在改坏产品码之下响过**才等于。

通道按工单 §二 原文（**没有**写 `merge --ff-only`，载体提交的父是 `8e9c13d`、线尾当时已 `9ac4396`，FF 数学上必被拒）：

```
git checkout 663d2c9d78d7f607137f10cd2af09069b0a4f841 -- src/Pry.Core/Narrative/Advancement tests/Pry.Core.Tests/BoundedAdvancementPrototypeTests.cs
git commit --only -m "feat(HDS-007,wave30): …" -- <那四枚路径>
```

落地后当场自证（全部 2026-10-27 23:5x 实测）：

- 提交＝`ba79ef1`，`4 files changed, 900 insertions(+)`，四枚全是 `create mode`。
- 四枚**盘上 SHA-256 与基准逐字符相同**：`AdvancementContracts.cs` `259474dcccbfb1f0…`／`AdvancementCoordinator.cs` `90947162fed797f8…`／`DueIntentRegistry.cs` `9bf4dc31bd3d1c72…`／判据 `1fd944f94d399a8f…`。
- **不为顶账另起构建的合法性证明**＝`git diff --stat 663d2c9d… HEAD -- src tests scripts Pry.slnx Directory.Build.props`＝**0 行** ⇒ 落地树与产出 1380 那棵树在这五面上逐字节相同（波 28 那条口径的第二次使用）。

### 九.3 顶账（三本，全部当场实测，不用加法预写）

1. **门槛基线（在册）自此＝1380／executed 1378／failed 0／跳过 2**。分项＝Client 108／App 233／Api 280／Core **759**（Core executed **757**）；等式＝上一在册 1357＋本刀 23 枚新格，唯一去处 Core 736→759。旧值 **1357／1339／1333／1321／1275／1179／1146／988 全部过期**，别再引用。两条同名跳过照旧按 `outcome="NotExecuted"` 点名，`notExecuted` 计数器不看。
2. **文件面条数**：4 枚新文件入库并暂存后当场实测 `git ls-files | wc -l`＝**621**（上一把 617；链 614→615→617→**621**，这一格的涨幅来自四枚产品/判据文件，不是文档）。catalog 仍 **76** 未动。
3. **换手账（这条必须跟基线一起说）**：这 23 枚第一次有**非判据作者的手**跑过（G0 23/23 有读数），其中 **7 枚**由那只手在改坏产品码之下取到清洁红 ⇒ 按 §九.1 末段资格判据，**这 7 枚的"另一双手"与"红证"两样同时结清**；lane 在 §八.3.8 写的"第二换手仍押在下一把复采刀"我这里改判一句：**7 枚已结，16 枚欠的不是换手、是红证**（无红＝没人把它在改坏码之下逼响过，逐格分类采信 lane §八.2 那张表：额度用尽 2＋不自残取红 2＋同轮未响 6＋红方向未排进额度 6，一枚也没被写成红）。**旧欠 75 格（波 13 余 47＋波 20 余 28）照旧不减**，拆法原文照旧 **46／45／1＝W2**。

### 九.4 仪器面三条登记（两处是我的错，一处是上一把留的坑）

1. **我自己的假阳性（不涂改原文，在这里点名）**：第一版取数脚本 `gk30-reds.js` 用 `indexOf('outcome="Failed"')` 找红例，命中了**文件级** `<ResultSummary outcome="Failed">`，又把全文最后一个 `<UnitTestResult` 当成那一枚失败用例 ⇒ 七轮全部报成"2 枚红"，第二枚名字是随文件顺序漂移的无关绿格（`CharacterIntelligenceTests.Unreliable_affect_readings_…` 那类）。改成"只数 `<UnitTestResult` 开标签内的 `outcome` 属性并与 `<Counters failed>` 对账"后＝每轮 1 枚、与 `<Counters>` 一致。**口径升级**：数红只认 `<UnitTestResult … outcome="Failed"…>` 这一个形状；`ResultSummary` 与 `<Output>` 里的错误文本都不算用例数；报告"恰 N 枚红"之前必须让 N 同时等于开标签计数与 `<Counters failed>`。
2. **上一把留下的 `round.py` 在 Python 3.14 下会抛 `SyntaxError`**（打印失败消息那一段），崩溃点落在**覆盖→跑→还原→核 SHA 与 status→数 TRX→读 `<Counters>` 全部完成之后**，所以七轮的还原与 SHA 凭据没受影响；lane 未改它一个字节、自写 `w30-failmsg.js`／`w30-roundinfo.js` 取消息，**零额外 `dotnet`**。这个处置是对的（改仪器不改判据）。登记为仪器缺陷：**下一把若还复用这棵载体，先修 `round.py` 的消息打印段，别在跑完七轮之后才发现消息取不到。**
3. **焦点闸这一把的形状**：10 枚预检（`P10`–`P19`）全部 `BUILD_OK`、`HOLD` 0 枚 ⇒ 8 枚"HOLD 之后重跑"额度 **0/8 未动用**；相邻预检间隔 68–184 秒（都不是 HOLD 重跑，5 分钟口径无适用点，间隔照实登记，与上一把那两处 9 秒／15 秒的自报不同）。`free_memory_gb` 落 6.7–8.2、比上一把台账的 10.7–11.1 低，但我逐轮核过 `failed`／`error` 计数与日志形态，**未出现** SQLite `disk I/O`／`out of memory`，故**没有一轮作废**——这条登记为读数形状，不登记为机器状态结论。守门人这一把**零 `dotnet`**（只跑只读命令与我自己的 `.trx` 解析脚本），作者机器没被打断。

### 九.5 押下一把（不撤回、不扩大，逐条点名）

1. 那 16 枚无红格按 lane §八.2 的分类接：**先追两枚"红方向已事前点名、只缺定向额度"的**（`A_late_advancement_carrying_a_superseded_effect_version_is_refused_and_writes_nothing`＝把 `request.ExpectedEffectVersion != _effectVersion` 改自比较，波 28 GK-R10 同族配方现成；`The_advancement_coordinator_exposes_exactly_one_public_entry_method`＝往产品文件加一枚公开入口方法，属**新增**而非摘闸）。
2. 两枚源码扫描格的**反面**：把 `LegacyChainNamesFoundIn`／`SourceSideDefaultsFoundIn` 自身弄坏（例如让它恒返空），看同格探针那句 `Assert.Equal` 是否先红——不红＝探针不咬、那格仍是常绿门，红＝这把才算把 §二 第②件事补完。
3. 那枚一次响 3 枚的未档探针 `probe/unrun-replay-mark-removed.cs`（`cdd18e3f…`，字节仍在盘、两把都没施加）：**跑它必须事前点名三枚格名**，批次红不许署给单枚突变（上一把立的口径）。
4. 波 28 押下的数值锁格（`The_prototype_applies_no_retrieval_floor_…`）仍押——反证方向只能新增数字门槛，而零门槛是作者定的，不许为凑红造门槛。
5. 两套并行的身份/类型命名债：四枚产品类型带 `Hds006` 工单号前缀（波 28 §八 已登记）、三身份枚举与契约 §7.2 的 `CognitionKind`／`CognitionOrigin` 并行——都属接线刀，不属证据刀。

### 九.6 归位不解除的未验证面（照抄 §八.3 那十条，一条不许并成"已完成"）

真实模型对照（零读数，需作者一句"可以跑"）；**生产接线**（这 23 枚至今**零生产调用方**，要接的是 004 生命周期与 005 原子提交的编排面与端点，一律没接，接线时机＝U-9 仍未拍）；UI 可见行为（零读数）；生产库与 `%LOCALAPPDATA%\PryCompanion`（零接触）；待标定参数权威值（冷却时长／静默窗口／预算默认值全部未定，原型内**一处数字门槛都没有**，这条是特性不是缺陷）；落库／幂等／崩溃恢复／并发两枚 `Advance`（`List` 非线程安全，从未取过读数）；旧链删除片与"构建时不把旧链打进包"那道门（**未开始**）；新格的第二双手已在 §九.3 结清 7 枚、其余按 §九.5 追。**三句话照旧不许出现在任何页面**：新链已接线／旧链可删／不进包已做到。

### 九.7 文件面与分刀边界

本刀主树写入面＝本报告（§九 追加）＋`docs/plans/PROJECT_PLAN.md`（§4.2 HDS-007 行＋§9 一条，**hunk 手术**：那枚未暂存的 ` M` 18/0 是在飞的他人改动＝2026-10-27 的 `RES-RETC-001`／`RES-SURF-001` 两条研究登记，我没提交、没读进自己的改动集、提交后复测仍 18/0 原样）＋`docs/full-scope-sprint.md` §0／§3；代码刀 `ba79ef1` 与文档刀分开，`git diff --cached --numstat` 复测他人两枚 `M ` 暂存项仍为 `1 1`／`63 0` 且**未进我的提交**；五枚 `??` 未跟踪资产未读内容、未改、未 `stash`／`clean`／`rm`。lane §八.5 那句"对本刀文件面外那枚 ` M` 零写入"我核过＝成立（它只 Read/Grep 过指定行段）。main 仍只挪 ref、零 `tag`／零 `push`／零打包。

## 十 第三十一波轮次记录（HDS-WAVE31-1028）

本节由第三十一号冷启动 lane（证据刀，零落地）在文件末尾追加。§〇–§九 是别人的原文与账，本刀一字未改；追加自证见本节末"本刀文件面"。所有时间按盘上时间写（起跑时 `date` ＝ 2026-10-28 12:33，与本轮 TRX／预检文件名时间戳对账）。

### 十.0 起跑自证

**〇.1 载体形状**：`git -C .local-notes/qa-wave29-1024/carrier status --short` ＝ **0 行**，`rev-parse HEAD` ＝ `663d2c9d78d7f607137f10cd2af09069b0a4f841`（2026-10-28 12:3x 当场）。成立，可起跑。

**〇.2 对账更正登记（任务派发时给定，非本 lane 自创）**：工单 §〇.2 写出闸时线尾为 `3044c1c`，出闸后被 `b263d7b`（纯文档派发刀）挪动；本 lane 按派发更正对**当前线尾**复核同一条式子。主树当时 `rev-parse HEAD` ＝ `b263d7b795ec99c9ac837d074d826342f74abba0`，命令与行数：

```
$ git -C C:/Users/24782/Desktop/Pry_Project diff --stat 663d2c9d78d7f607137f10cd2af09069b0a4f841 HEAD -- src tests scripts Pry.slnx Directory.Build.props
（0 行）
```

**0 行** ⇒ 载体字节＝线尾在五面上逐字节成立，本刀按 §〇.2 允许**跳过绿基线轮 G0**（23 枚格在 `663d2c9` 上已由第三十波 G0 取到全绿读数、其中 7 枚有清洁红，字节未变）。定向额度因此留 8 枚＝计划 M1–M7 ＋ 机动 1。

**〇.3 起点在册基线（从盘上取）**：出处＝本报告 §九.3 第 1 条原文：现值＝**1380／executed 1378／failed 0／跳过 2**，分项 Client 108／App 233／Api 280／Core total 759（executed 757）。本刀另用自写仪器对第三十波盘上 `out/trx-F` 四份 TRX 当场重求和复核＝`total=1380 executed=1378 passed=1378 failed=0`（四份 `RECONCILED=true`；两条跳过按 `outcome="NotExecuted"` 点名＝`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`／`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`）。本刀不移动这个数。

**〇.4 还原基准 SHA（全部由本 lane 当场从载体基点 `git cat-file blob HEAD:<path>` 自取，不是抄线尾、不是抄工单）**：

| 文件（载体相对） | 自取 SHA-256 全值 | 与工单 §〇.4 前缀 |
|---|---|---|
| `src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs` | `90947162fed797f8de98bd5130a11dfacc25886bfa035182dccaf395b9d32b73` | 相符（`90947162…`） |
| `src/Pry.Core/Narrative/Advancement/DueIntentRegistry.cs` | `9bf4dc31bd3d1c724a804f2dc01334806dada6a1c88861eb07f89fc0897ce5a1` | 相符（`9bf4dc31…`） |
| `src/Pry.Core/Narrative/Advancement/AdvancementContracts.cs`（本刀不动，形状对照） | `259474dcccbfb1f0327d92ba5c49e1e8ee35280c16abf6761088332695976ac0` | 相符（`259474dc…`） |
| `tests/Pry.Core.Tests/BoundedAdvancementPrototypeTests.cs`（仅 M3/M4 临时碰） | `1fd944f94d399a8f742482c1f724f8afec150eac865cf06cdfa149d4926a200b` | 相符（`1fd944f9…`） |

取法值处说明照工单要求写明：**本刀三处 SHA 对照（覆盖前自取／还原后复核／上表基准）全部取自载体基点 blob，抄的是载体值不是线尾值**。另核盘上工作副本字节＝HEAD blob（每轮 prep 时再当场对一次）。

**〇.5 编号更正登记**：焦点预检自 **P20** 起编号（P10–P19 属第三十波，本刀一枚未覆盖；归档目录＝`.local-notes/wave29-1024/out/preflight-P<nn>.txt`）。`HOLD` 重跑额度 ≤8 枚按刀计，本刀尚未用到第一枚。

**〇.6 仪器自证（先于任何 `dotnet` 完成，零 `dotnet`）**：自写取数与打印仪器 `.local-notes/wave29-1024/out/w31-trx.js`（node），口径＝只数 `<UnitTestResult … outcome="Failed" …>` 开标签并与同份 `<Counters failed>` 对账（`RECONCILED` 布尔）；跳过只按 `outcome="NotExecuted"` 点名。自证读数归档于 `out/w31-instrument-selfproof.txt`（trx-R7／trx-G0）与 `out/w31-instrument-selfproof-F.txt`（trx-F 四份）：
- 对第三十波已知恰 1 枚红的 `out/trx-R7/24782_PHYTHLINK_2026-10-27_23_37_54.trx`：取到 `failedOpenTags=1 == countersFailedAttr=1`、testName 逐字＝`Pry.Core.Tests.BoundedAdvancementPrototypeTests.A_due_intent_that_has_not_reached_its_due_instant_is_left_pending_and_still_reported`、消息取得到＝`Assert.Single() Failure: The collection was empty`、栈锚点取得到＝`.cs:line 307`、两条 NotExecuted 点名——与 §九.1 R7 行**逐字符相符** ⇒ 取数与打印两面都自证成立。
- 对 `out/trx-G0`（已知全绿 757 passed）：`failedOpenTags=0 == countersFailedAttr=0`。
- 对 `out/trx-F`（四份）：逐份 `RECONCILED=true`，求和 1380／1378／1378／0——即 §九.3 在册值的同值复算。
- **对 §九.4 第 2 条的一处更正登记**：本 lane 在 Python 3.14.5 下对 `round.py` 跑 `py_compile` 与其 `--help`/usage 路径**未复现 `SyntaxError`**（登记为"未复现"，不改原文不判上一把错；本刀不依赖该脚本，用自证过的 `w31-trx.js`＋`w31-round.js` 两枚自写仪器，符合 §〇.6"要么修要么自写"的第二条）。

**突变态预合 SHA（M2/M6/M7 为工单 §八.2 点名必补项；其余五枚一并预证，零 `dotnet`，合成器＝`out/w31-mutate.js`，清单＝`out/w31-mutation-manifest.txt`，全部单 hunk、LF 保持、锚点唯一）**：

| 轮 | 目标（载体相对） | 突变态 SHA-256 预合值 |
|---|---|---|
| M1 | `AdvancementCoordinator.cs` | `0b8542d0a462faa23cd22f4bb2eebb09c39d68c1498206e7ab52296f4c601bc3` |
| M2 | `AdvancementCoordinator.cs` | `f737db738cf802d431082b74b25945c78c9f73713803ba8732d4e3650836d699` |
| M3 | `BoundedAdvancementPrototypeTests.cs` | `f673566ba3f360cb69d7c3a33d750884caa3069d973e16027cc421462eba4980` |
| M4 | `BoundedAdvancementPrototypeTests.cs` | `95db0ad98fe11a54b237cadb9fb74ff390dbbd864eef6ae6b9d02004cb2552ea` |
| M5 | `DueIntentRegistry.cs` | `cdd18e3f94896756d37ea4d2df5b7ce289dbee8ed45ca1e4346759bafb2feba0`＝工单 §三 预合值逐字符相符（探针＝删 `:73` 单行，`diff -u` 复对成立；base 探针字节与载体 HEAD blob 同值） |
| M6 | `AdvancementCoordinator.cs` | `05e7d56962b333e4c04e527ef4a1ba249c881fcc5d2ec1cb691bf53be32b0ff1` |
| M7 | `AdvancementCoordinator.cs` | `b5693cd1f324f35314ca33d91685b9121c32dea166d7c9596746c77abf695362` |
| M8 | `AdvancementCoordinator.cs` | `7dffa0e906d94e47d5cda7676701c9148a2d5730b2e75904a971e33be16a2ae9` |

**格名逐字符预核（§三 末注要求的零 `dotnet` 一步，已做）**：八枚预期格名逐一对过判据文件盘上原文——M1 取 `:313` 原文 `A_late_advancement_carrying_a_superseded_effect_version_is_refused_and_writes_nothing`（工单表内自我更正确认：`superserved` 为误、`superseded` 为是）；M2＝`:74`／红点 `:83`；M3＝`:404`／探针红点 `:417`；M4＝`:427`／探针红点 `:439`；M5 三枚＝`:275`／`:255`／`:293`；M6 三 Theory 实例参数照 `:360-362`（`asked: UserEvent, requestId: "req-missing-ue"`／`DueIntent…req-missing-due`／`BackgroundScene…req-missing-bg`）＋`:335`；M7＝`:380`＋`Each_trigger_kind…`（`:49-51` 三实例，其中 BackgroundScene 那条为已付第二条因）；M8＝`:89`／红点 `:99`。TRX 的 `testName` 带 `Pry.Core.Tests.BoundedAdvancementPrototypeTests.` 前缀（仪器自证已见），下面逐轮记录一律照 TRX 形状写全。

**M8 的一处事前静态预读登记（先档后跑，不改 §三 表，不预判作废）**：按判据原文推读——`An_exhausted_call_budget_stops_advancement_and_names_consumed_remaining_and_the_dimension`（`:178`）第一次以 `NoQuietWindow(Noon)`（CooldownWindow＝零）推进成功，第二次 12:05 同意图；方向翻转后 `Noon < 12:05` 为真⇒该格预期也会红。即 M8 实测可能是 **2 枚红**（含那张无红格的第二条方向），而 §三 表写"恰 1 枚"。本 lane 不据此改表、不据此免跑：照跑照量，若实测 2 枚即按规则判该轮作废、"预期 vs 实测"两栏都逐字登记。

### 十.1 逐轮记录（M1…）

**轮 M1（2026-10-28 12:41 盘上时间，TRX 文件名时间戳与 `date` 对得上）**

| 字段 | 读数 |
|---|---|
| ① 轮名与目标文件 | `M1` → 载体 `src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs` |
| ② 预检 | `out/preflight-P21.txt`，`VERDICT=BUILD_OK`（P20＝B1 构建前那枚，亦 `BUILD_OK`） |
| ③ 改动原文 | 前：`        if (request.ExpectedEffectVersion != _effectVersion)` → 后：`        if (request.ExpectedEffectVersion != _effectVersion && request.RequestId is null)`（同行加永假合取，避开 CS1718） |
| ④ `git diff --stat` | `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤ 跑法 | 工作目录＝载体；`dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave29-1024/out/trx-M1" --logger trx`（无 `--no-build`；stdout 归档 `out/w31-run-M1.txt`，exit=1＝测试失败的非零码，构建 0 警告 0 错误在 `out/w31-build-B1.txt`） |
| ⑥ TRX | `trx-M1/24782_PHYTHLINK_2026-10-28_12_41_01.trx`，枚数＝**1 份文件**（先数后读） |
| ⑦ `<Counters>` | `total="759" executed="757" passed="756" failed="1"`（error/timeout/aborted/notExecuted 均 0） |
| ⑧ 红格 testName 逐字 | `Pry.Core.Tests.BoundedAdvancementPrototypeTests.A_late_advancement_carrying_a_superseded_effect_version_is_refused_and_writes_nothing` |
| ⑨ 红点与消息原文 | 红点＝判据 `:322`；消息＝`Assert.Equal() Failure: Values differ`／`Expected: RejectedForStaleEffect`／`Actual:   Advanced` |
| ⑩ SHA-256 三态 | 覆盖前自取 `90947162fed797f8…`（＝HEAD blob＝§〇.4 基准）；突变态 `0b8542d0a462faa23cd22f4bb2eebb09c39d68c1498206e7ab52296f4c601bc3`＝§十.0 预合值逐字符相符；还原后 `90947162fed797f8…` 相符；载体 `status --short` 还原后 **0 行**（`out/w31-round-M1-prep.txt`／`-restore.txt`） |
| ⑪ 结论 | **相符**——格名（含 `:313` 原文的 `superseded`）、恰 1 枚、红点 `:322`、消息形状四者逐字符等于 §三 预论证 ⇒ 新增凭证 **1 枚** |

数红两侧对账：开标签计数 1 ＝ `<Counters failed>` 1（`RECONCILED=true`）。跳过 2 枚按 NotExecuted 点名照旧（`Configured_llama_server_requires_the_child_environment_key`／`Configured_sense_voice_model_can_run_offline`），不计红。

**轮 M2（2026-10-28 12:42）**

| 字段 | 读数 |
|---|---|
| ① 轮名与目标文件 | `M2` → 载体 `src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs` |
| ② 预检 | `out/preflight-P22.txt`，`VERDICT=BUILD_OK` |
| ③ 改动原文 | `Advance` 收尾的 `    }` 之后同一行段新增两行（一空行＋一方法）：`    public bool HasRecords() => _records.Count > 0;`（新增文本，无删除行；非 special-name 公开实例方法） |
| ④ `git diff --stat` | `1 file changed, 2 insertions(+)` |
| ⑤ 跑法 | 工作目录＝载体；`dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave29-1024/out/trx-M2" --logger trx`（无 `--no-build`；stdout＝`out/w31-run-M2.txt`，exit=1；该日志内 grep 无任何 warning 输出＝构建无警告形状） |
| ⑥ TRX | `trx-M2/24782_PHYTHLINK_2026-10-28_12_42_21.trx`，枚数＝1 |
| ⑦ `<Counters>` | `total="759" executed="757" passed="756" failed="1"` |
| ⑧ 红格 testName 逐字 | `Pry.Core.Tests.BoundedAdvancementPrototypeTests.The_advancement_coordinator_exposes_exactly_one_public_entry_method` |
| ⑨ 红点与消息原文 | 红点＝判据 `:83`；消息＝`Assert.Equal() Failure: Strings differ`／`Expected: "Advance"`／`Actual:   "Advance,HasRecords"`／`↑ (pos 7)` |
| ⑩ SHA-256 三态 | 覆盖前自取＝`90947162…`（＝HEAD blob）；突变态 `f737db738cf802d431082b74b25945c78c9f73713803ba8732d4e3650836d699`＝§十.0 预合值逐字符相符（本枚工单未给预合值，按 §八.2 已在施加前于临时副本零 `dotnet` 预证）；还原后 `90947162…` 相符；`status --short` **0 行** |
| ⑪ 结论 | **相符**——恰 1 枚、格名、红点 `:83`、Expected/Actual 形状全等于 §三 预论证 ⇒ 新增凭证 **1 枚** |

对账：开标签 1 ＝ `<Counters failed>` 1。

**轮 M3（2026-10-28 12:43–12:44）——判据字节的临时突变，按 §一.3 单独归类**

| 字段 | 读数 |
|---|---|
| ① 轮名与目标文件 | `M3` → 载体 `tests/Pry.Core.Tests/BoundedAdvancementPrototypeTests.cs`（判据本体，§一.3 允许的唯一例外面） |
| ② 预检 | `out/preflight-P23.txt`，`VERDICT=BUILD_OK` |
| ③ 改动原文 | 前：`        LegacyChainNames.Where(name => source.Contains(name, StringComparison.Ordinal));` → 后：`        LegacyChainNames.Where(name => name.Contains(source, StringComparison.Ordinal));`（扫描引用面翻转；被扫 token 未注释、未绕开） |
| ④ `git diff --stat` | `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤ 跑法 | 工作目录＝载体；`dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave29-1024/out/trx-M3" --logger trx`（无 `--no-build`；stdout＝`out/w31-run-M3.txt`，exit=1） |
| ⑥ TRX | `trx-M3/24782_PHYTHLINK_2026-10-28_12_43_39.trx`，枚数＝1 |
| ⑦ `<Counters>` | `total="759" executed="757" passed="756" failed="1"` |
| ⑧ 红格 testName 逐字 | `Pry.Core.Tests.BoundedAdvancementPrototypeTests.Advancement_prototype_sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites` |
| ⑨ 红点与消息原文 | 红点＝判据 `:417`（探针那句 `Assert.Equal`）；消息＝`Assert.Equal() Failure: Strings differ`／`↓ (pos 0)`／`Expected: "CharacterIntelligenceGateway,IdleBackoffController"···`／`Actual: ""` |
| ⑩ SHA-256 三态 | 覆盖前自取＝`1fd944f9…`（＝HEAD blob＝§〇.4 基准）；突变态 `f673566ba3f360cb69d7c3a33d750884caa3069d973e16027cc421462eba4980`＝§十.0 预合值相符；**还原后 `1fd944f94d399a8f…` 逐字符相符（§一.3 第 1 条达成）**；`status --short` **0 行** |
| ⑪ 结论 | **相符**——恰 1 枚、格名逐字、红点 `:417`、Expected 五个旧链名／Actual `""`；且红落在探针句上＝`:408` 的 `Assert.Empty` 循环先走完了（产品文件逐枚查过仍无命中）——"只有把扫描器弄坏探针才响"的**门在咬**形状拿到 |

**归类（§一.3 第 2 条）：本枚红只署"门在咬（反面探针结清）"，不计入"改坏产品码之下响过"类，不作为第二换手签账依据。** 对账：开标签 1 ＝ `<Counters failed>` 1。

**轮 M4（2026-10-28 12:44–12:45）——判据字节的临时突变，按 §一.3 单独归类**

| 字段 | 读数 |
|---|---|
| ① 轮名与目标文件 | `M4` → 载体 `tests/Pry.Core.Tests/BoundedAdvancementPrototypeTests.cs`（判据本体） |
| ② 预检 | `out/preflight-P24.txt`，`VERDICT=BUILD_OK` |
| ③ 改动原文 | 两枚被扫 token 本身改掉（不是注释绕开）：前 `        if (source.Contains("const ", StringComparison.Ordinal)) yield return "const";` → 后 `        if (source.Contains("const_", StringComparison.Ordinal)) yield return "const";`；前 `        if (source.Contains("static readonly", StringComparison.Ordinal)) yield return "static readonly";` → 后 `        if (source.Contains("static_readonly", StringComparison.Ordinal)) yield return "static readonly";` |
| ④ `git diff --stat` | `1 file changed, 2 insertions(+), 2 deletions(-)`（同一 hunk，`:472`／`:473` 两行） |
| ⑤ 跑法 | 工作目录＝载体；`dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave29-1024/out/trx-M4" --logger trx`（无 `--no-build`；stdout＝`out/w31-run-M4.txt`，exit=1） |
| ⑥ TRX | `trx-M4/24782_PHYTHLINK_2026-10-28_12_44_55.trx`，枚数＝1 |
| ⑦ `<Counters>` | `total="759" executed="757" passed="756" failed="1"` |
| ⑧ 红格 testName 逐字 | `Pry.Core.Tests.BoundedAdvancementPrototypeTests.Advancement_prototype_declares_no_source_side_default_for_windows_or_budgets` |
| ⑨ 红点与消息原文 | 红点＝判据 `:439`（探针句）；消息＝`Assert.Equal() Failure: Strings differ`／`↓ (pos 0)`／`Expected: "const,static readonly"`／`Actual: ""` |
| ⑩ SHA-256 三态 | 覆盖前自取＝`1fd944f9…`（＝HEAD blob）；突变态 `95db0ad98fe11a54b237cadb9fb74ff390dbbd864eef6ae6b9d02004cb2552ea`＝§十.0 预合值相符；**还原后 `1fd944f9…` 逐字符相符**；`status --short` **0 行** |
| ⑪ 结论 | **相符**——恰 1 枚、格名逐字、红点 `:439`、Expected/Actual 形状等于 §三；`:431` 的 `Assert.Empty` 循环先走完保持绿＝同样拿到"门在咬"形状 |

**归类：反面探针类（§一.3），与 M3 同、不计入改坏产品码类、不作第二换手依据。** 对账：开标签 1 ＝ `<Counters failed>` 1。M3＋M4 两枚按工单要求与其余六枚分开列（见 §十.2）。

**轮 M5（2026-10-28 12:46）——批次红，跑前复述（§一.2.4）**：本枚预期一次响 3 枚，格名照 §三 表逐字复述＝`Replay_delivers_each_due_intent_exactly_once_across_two_clock_jumps_and_loses_none`、`After_a_clock_forward_jump_due_intents_are_replayed_in_registration_order`、`A_due_intent_that_has_not_reached_its_due_instant_is_left_pending_and_still_reported`；我打算报的红集＝这 3 枚、无其它，与上表核对＝相同，然后才起跑。

| 字段 | 读数 |
|---|---|
| ① 轮名与目标文件 | `M5` → 载体 `src/Pry.Core/Narrative/Advancement/DueIntentRegistry.cs` |
| ② 预检 | `out/preflight-P25.txt`，`VERDICT=BUILD_OK` |
| ③ 改动原文 | 删除 `:73` 一整行：`-            entry.Replayed = true;`（施加字节＝盘上探针 `probe/unrun-replay-mark-removed.cs` 原样） |
| ④ `git diff --stat` | `1 file changed, 1 deletion(-)` |
| ⑤ 跑法 | 工作目录＝载体；`dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave29-1024/out/trx-M5" --logger trx`（无 `--no-build`；stdout＝`out/w31-run-M5.txt`，exit=1） |
| ⑥ TRX | `trx-M5/24782_PHYTHLINK_2026-10-28_12_46_13.trx`，枚数＝1 |
| ⑦ `<Counters>` | `total="759" executed="757" passed="754" failed="3"` |
| ⑧ 红格 testName 逐字（3 枚，全部命中预点名集） | `Pry.Core.Tests.BoundedAdvancementPrototypeTests.Replay_delivers_each_due_intent_exactly_once_across_two_clock_jumps_and_loses_none`／`…After_a_clock_forward_jump_due_intents_are_replayed_in_registration_order`／`…A_due_intent_that_has_not_reached_its_due_instant_is_left_pending_and_still_reported` |
| ⑨ 红点与消息原文（三枚各自不同，符合 §三"消息各自不同"） | ①`Replay_delivers…` 红点 `:287`＝`Expected: "" / Actual: "intent-a,intent-b"`（第二次跳表仍吐出）；②`After_a_clock_forward_jump…` 红点 `:271`＝`Expected: "intent-future" / Actual: "intent-first,intent-second,intent-third,intent-fut"···`（没到点的仍被当作已补投之外全留在 StillPending）；③`A_due_intent…` 红点 `:309`＝`Expected: "" / Actual: "intent-boundary"`（到点补投后清单没清空） |
| ⑩ SHA-256 三态 | 覆盖前自取＝`9bf4dc31…`（＝HEAD blob）；突变态 `cdd18e3f94896756d37ea4d2df5b7ce289dbee8ed45ca1e4346759bafb2feba0` ＝ §三 预合值逐字符相符；还原后 `9bf4dc31…` 相符；`status --short` **0 行** |
| ⑪ 结论 | **相符**——恰 3 枚、逐枚格名与预点名集逐字符对上、三枚消息形状各自不同且与删标记的因果一致 ⇒ 批次红按 §一.2.4 署给本枚突变；**新增凭证 1 枚**（`Replay_delivers…`），`After_a_clock_forward_jump…`（波 30 R5 已付）与 `A_due_intent…`（波 30 R7 已付）各署"同一格的第二条因"，不重复领凭证、不撤销原归属 |

对账：开标签 3 ＝ `<Counters failed>` 3（`RECONCILED=true`）。探针 `cdd18e3f…` 至此从"未跑"转为"已跑已还"（两把欠的那枚结清）。

**轮 M6（2026-10-28 12:47–12:48）——批次红，跑前复述（§一.2.4）**：预期一次响 4 枚＝`Missing_source_is_refused_for_every_trigger_kind_with_the_same_named_failure` 的三个 Theory 实例（`asked: UserEvent`／`DueIntent`／`BackgroundScene`，`requestId` 照 TRX 逐字）＋`An_advancement_whose_source_cannot_be_pointed_to_fails_with_a_named_reason_and_stays_in_the_log`；预期消息形状＝`System.NullReferenceException`。复述与预点名集核对相同后起跑。

| 字段 | 读数 |
|---|---|
| ① 轮名与目标文件 | `M6` → 载体 `src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs` |
| ② 预检 | `out/preflight-P26.txt`，`VERDICT=BUILD_OK` |
| ③ 改动原文 | 前：`        if (request.Source is null \|\| string.IsNullOrWhiteSpace(request.Source.SourceId))` → 后：`        if (request.Source is null ? false : string.IsNullOrWhiteSpace(request.Source.SourceId))`（摘来源闸：空来源不再被拒） |
| ④ `git diff --stat` | `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤ 跑法 | 工作目录＝载体；`dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave29-1024/out/trx-M6" --logger trx`（无 `--no-build`；stdout＝`out/w31-run-M6.txt`，exit=1） |
| ⑥ TRX | `trx-M6/24782_PHYTHLINK_2026-10-28_12_47_30.trx`，枚数＝1 |
| ⑦ `<Counters>` | `total="759" executed="757" passed="753" failed="4"` |
| ⑧ 红格 testName 逐字（4 枚，全部命中预点名集；实例参数照 TRX 原样） | `Pry.Core.Tests.BoundedAdvancementPrototypeTests.Missing_source_is_refused_for_every_trigger_kind_with_the_same_named_failure(asked: UserEvent, requestId: "req-missing-ue")`／`(asked: DueIntent, requestId: "req-missing-due")`／`(asked: BackgroundScene, requestId: "req-missing-bg")`＋`Pry.Core.Tests.BoundedAdvancementPrototypeTests.An_advancement_whose_source_cannot_be_pointed_to_fails_with_a_named_reason_and_stays_in_the_log` |
| ⑨ 红点与消息原文 | 四枚消息逐字＝`System.NullReferenceException : Object reference not set to an instance of an object.`；栈的落点＝产品 `AdvancementCoordinator.cs:line 115`（`new AdvancementRecord(...)` 语句起始行，编译器把整句归到 115；§三 说的 `:116` 解引用表达式就在这句里）＋测试侧 `:368`（Theory）／`:339`（Fact）。**照 §三 要求原样登记为正面事实：来源闸摘掉后入口会崩（NRE），而不是明确拒绝——这不是仪器故障。** |
| ⑩ SHA-256 三态 | 覆盖前自取＝`90947162…`（＝HEAD blob）；突变态 `05e7d56962b333e4c04e527ef4a1ba249c881fcc5d2ec1cb691bf53be32b0ff1`＝§十.0 预合值相符（本枚工单未给预合值，施加前零 `dotnet` 预证）；还原后 `90947162…` 相符；`status --short` **0 行** |
| ⑪ 结论 | **相符**——恰 4 枚、逐枚格名与实例参数命中预点名集、消息形状逐字符＝`System.NullReferenceException` ⇒ **新增凭证 3 枚**（三个 Theory 实例）；`An_advancement_whose_source…`（波 30 R6 已付）署"同一格的第二条因"，不重复领凭证 |

对账：开标签 4 ＝ `<Counters failed>` 4。

**轮 M7（2026-10-28 12:48–12:49）——批次红，跑前复述（§一.2.4）**：预期一次响 4 枚＝`A_successful_advancement_carries_the_source_reference_into_the_record_it_produces`＋`Each_trigger_kind_enters_the_one_advancement_entry_and_names_its_own_reference` 的 `(asked: UserEvent, expectedReference: "ue-1")`／`(asked: DueIntent, expectedReference: "due-1")` 两个实例，第 4 枚 `(asked: BackgroundScene, expectedReference: "bg-1")` 为已付格的第二条因、不领新凭证。复述与预点名集核对相同后起跑。

| 字段 | 读数 |
|---|---|
| ① 轮名与目标文件 | `M7` → 载体 `src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs` |
| ② 预检 | `out/preflight-P27.txt`，`VERDICT=BUILD_OK` |
| ③ 改动原文 | 成功路径记录里的来源标识换成意图键（只改这一枚实参，`SourceVersion` 不动）：前：`            triggerReference!, request.Source.SourceId, request.Source.SourceVersion, effectVersion,` → 后：`            triggerReference!, request.IntentKey, request.Source.SourceVersion, effectVersion,` |
| ④ `git diff --stat` | `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤ 跑法 | 工作目录＝载体；`dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave29-1024/out/trx-M7" --logger trx`（无 `--no-build`；stdout＝`out/w31-run-M7.txt`，exit=1） |
| ⑥ TRX | `trx-M7/24782_PHYTHLINK_2026-10-28_12_48_46.trx`，枚数＝1 |
| ⑦ `<Counters>` | `total="759" executed="757" passed="753" failed="4"` |
| ⑧ 红格 testName 逐字（4 枚，全部命中预点名集） | `Pry.Core.Tests.BoundedAdvancementPrototypeTests.A_successful_advancement_carries_the_source_reference_into_the_record_it_produces`／`…Each_trigger_kind_enters_the_one_advancement_entry_and_names_its_own_reference(asked: UserEvent, expectedReference: "ue-1")`／`(asked: DueIntent, expectedReference: "due-1")`／`(asked: BackgroundScene, expectedReference: "bg-1")` |
| ⑨ 红点与消息原文 | 前两处红点＝判据 `:395`（`Expected: "src-record"／Actual: "intent-record"`）与 `:67`（三实例各 `Expected: "src-1"／Actual: "intent-probe"`）——与 §三 "Expected `src-record`／`src-1`，Actual 为该格 `intent` 串"相符 |
| ⑩ SHA-256 三态 | 覆盖前自取＝`90947162…`（＝HEAD blob）；突变态 `b5693cd1f324f35314ca33d91685b9121c32dea166d7c9596746c77abf695362`＝§十.0 预合值相符（本枚工单未给预合值，施加前零 `dotnet` 预证）；还原后 `90947162…` 相符；`status --short` **0 行** |
| ⑪ 结论 | **相符**——恰 4 枚、逐枚格名与实例参数命中预点名集、两处红点行与 Expected/Actual 形状相符 ⇒ **新增凭证 3 枚**（`A_successful_advancement…`＋两个 Theory 实例）；`(asked: BackgroundScene…)`（波 30 R1 已付）署"同一格的第二条因" |

对账：开标签 4 ＝ `<Counters failed>` 4。**至此计划七轮（M1–M7）全部相符 ⇒ 按 §五.2 机动位允许用于 M8。**

**轮 M8（机动位；2026-10-28 12:49–12:50）——作废轮，"预期 vs 实测"两栏逐字登记**

| 字段 | 读数 |
|---|---|
| ① 轮名与目标文件 | `M8` → 载体 `src/Pry.Core/Narrative/Advancement/AdvancementCoordinator.cs`（机动位按 §五.2 用于 M8，未用于复取，不存在两处同用） |
| ② 预检 | `out/preflight-P28.txt`，`VERDICT=BUILD_OK` |
| ③ 改动原文 | 前：`        if (cooldownOpensAt is { } cooldownEnd && cooldownEnd > request.Window.NowUtc)` → 后：`        if (cooldownOpensAt is { } cooldownEnd && cooldownEnd < request.Window.NowUtc)`（冷却闸方向翻转） |
| ④ `git diff --stat` | `1 file changed, 1 insertion(+), 1 deletion(-)` |
| ⑤ 跑法 | 工作目录＝载体；`dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave29-1024/out/trx-M8" --logger trx`（无 `--no-build`；stdout＝`out/w31-run-M8.txt`，exit=1） |
| ⑥ TRX | `trx-M8/24782_PHYTHLINK_2026-10-28_12_49_52.trx`，枚数＝1 |
| ⑦ `<Counters>` | `total="759" executed="757" passed="755" failed="2"` |
| ⑧ 红格 testName 逐字 | `Pry.Core.Tests.BoundedAdvancementPrototypeTests.The_same_intent_resubmitted_inside_the_cooldown_window_is_deferred_and_names_the_earliest_next_instant`＋`Pry.Core.Tests.BoundedAdvancementPrototypeTests.An_exhausted_call_budget_stops_advancement_and_names_consumed_remaining_and_the_dimension` |
| ⑨ 红点与消息原文 | 前者红点 `:99`＝`Expected: DeferredByCooldown／Actual:   Advanced`；后者红点 `:188`＝`Expected: StoppedByBudget／Actual:   DeferredByCooldown` |
| ⑩ SHA-256 三态 | 覆盖前自取＝`90947162…`（＝HEAD blob）；突变态 `7dffa0e906d94e47d5cda7676701c9148a2d5730b2e75904a971e33be16a2ae9`＝§十.0 预合值相符；还原后 `90947162…` 相符；`status --short` **0 行** |
| ⑪ 结论 | **作废（多响）**——§三 M8 行预论证＝"恰 1 枚；`An_intent_resubmitted_at_the_instant…` 保持绿"；实测＝**2 枚**（预点名那枚红于 `:99` ✓、外沿格如预判保持绿 ✓、但多出 §十.0 事前静态预读已登记的 `An_exhausted_call_budget…` 红于 `:188`）。枚数与"恰 1 枚"不符即属"预期与实测不符"，按 §三/§五.2 判该轮作废：**本轮两枚红都不领新凭证**，两格照旧留在无红账上；不复取（定向 8 枚额度至此用满，机动位已按其唯一合法用途用于 M8，不存在第二次）。差异登记为对下一把的实测事实：冷却翻转是一枚**两响方向**（`The_same_intent_resubmitted…`＋`An_exhausted_call_budget…` 同时红），§三 表当年只点了其中一枚 |

对账：开标签 2 ＝ `<Counters failed>` 2（仪器无误，红集差异是预论证漏点，不是读数漂移）。作废轮的"当时那份绿"照 §八.4 要求登记：同轮其余 755 枚绿、外沿格 `An_intent_resubmitted_at_the_instant_the_cooldown_window_closes_advances_again` 在翻转后仍 Passed（相等两侧都不成立）——与 §三 预判相符的那一半也一并留档。

**轮 F（收尾全量；2026-10-28 12:51）**

| 字段 | 读数 |
|---|---|
| ① 轮名与范围 | `F`＝`dotnet test Pry.slnx` 全解决方案，工作目录＝载体；前面是 M8 突变态的逐字节还原，按 §一.2.5 **不带 `--no-build`** 让其自行增量重建 |
| ② 预检 | `out/preflight-P29.txt`，`VERDICT=BUILD_OK` |
| ③ 改动原文 | 无——施加前全部还原完毕（载体 `git status --short` 起跑该轮时 0 行） |
| ④ `git diff --stat` | 0 行（无任何未还原文件） |
| ⑤ 跑法 | `dotnet test Pry.slnx -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave29-1024/out/trx-F31" --logger trx`（stdout＝`out/w31-run-F31.txt`，exit=0） |
| ⑥ TRX | 先 `ls` 数满 **4 份**再逐份读：`24782_PHYTHLINK_2026-10-28_12_51_15.trx`／`12_51_15[1].trx`（Api 与 Core 同秒收尾，VSTest 自动加 `[1]` 后缀，两枚都在盘）／`12_51_19.trx`／`12_51_22.trx` |
| ⑦ `<Counters>` 逐份原文 | Client `total=108 executed=108 passed=108 failed=0`；Core `total=759 executed=757 passed=757 failed=0`；App `total=233 executed=233 passed=233 failed=0`；Api `total=280 executed=280 passed=280 failed=0`。逐份 `RECONCILED=true`（开标签计数＝`failed` 属性，四份皆 0）。**求和式：108＋759＋233＋280＝1380；executed 108＋757＋233＋280＝1378；failed 0；跳过按 `outcome="NotExecuted"` 点名 2 枚＝`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`／`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`（与 §九.3 同名同枚）** |
| ⑧ 红格 | 无 |
| ⑨ 红点 | 无 |
| ⑩ 收尾 SHA 与形状 | 四枚文件盘上 SHA＝`90947162…`／`9bf4dc31…`／`259474dc…`／`1fd944f9…`，逐字符等于 §〇.4 自取基准（`out/w31-final-carrier-proof.txt`）；`git -C 载体 status --short`＝**0 行** |
| ⑪ 结论 | 门禁重新全绿＝这一把的手零漂移；**1380 拿到一份由另一双手在同一棵树上取到的同值复采读数（登记为同值复采，不改写在册数）**。`measure-fixed-eval.ps1` 只跑这一次、带 `-ResultsDirectory` 指向 `trx-F31`：`Catalog valid: 76`／`Passed=1378; Failed=0; Other=2`，`fixed-eval-coverage.json` 落在该目录内与求和自洽（`out/w31-measure-F31.txt`） |

### 十.2 终账（新增红证与剩余无红格逐格点名）

**新增红证＝11 枚**（工单 §三.1 预写形状是 12，差额恰是 M8 作废轮的 1 枚——按"形状不符照实登记"处理，不改判）：

- **改坏产品码之下响过类＝9 枚**：`A_late_advancement_carrying_a_superseded_effect_version_is_refused_and_writes_nothing`（M1）；`The_advancement_coordinator_exposes_exactly_one_public_entry_method`（M2）；`Replay_delivers_each_due_intent_exactly_once_across_two_clock_jumps_and_loses_none`（M5，批次中唯一新凭证）；`Missing_source_is_refused_for_every_trigger_kind_with_the_same_named_failure(asked: UserEvent, requestId: "req-missing-ue")`／`(asked: DueIntent, requestId: "req-missing-due")`／`(asked: BackgroundScene, requestId: "req-missing-bg")`（M6 三实例）；`A_successful_advancement_carries_the_source_reference_into_the_record_it_produces`／`Each_trigger_kind_enters_the_one_advancement_entry_and_names_its_own_reference(asked: UserEvent, expectedReference: "ue-1")`／`(asked: DueIntent, expectedReference: "due-1")`（M7 三枚）。
- **反面探针类（门在咬，结清）＝2 枚，单列**：`Advancement_prototype_sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites`（M3）；`Advancement_prototype_declares_no_source_side_default_for_windows_or_budgets`（M4）。按 §一.3 第 2 条，这两枚不计入上一类、不作为第二换手签账依据（施加它们的那两只手在这两轮动过判据字节，当轮资格判据不成立）。
- **已付格的第二条因（登记、不领凭证、不撤原归属）＝4 条**：`After_a_clock_forward_jump…`（M5，原＝波 30 R5）；`A_due_intent_that_has_not_reached…`（M5，原＝R7）；`An_advancement_whose_source_cannot_be_pointed_to…`（M6，原＝R6）；`Each_trigger_kind…(asked: BackgroundScene, expectedReference: "bg-1")`（M7，原＝R1）。
- **作废轮**：M8（预期"恰 1 枚"vs 实测 2 枚，多响）——该轮两枚红（含 `The_same_intent_resubmitted…` 与 `An_exhausted_call_budget…`）都不计凭证；正面收获＝实测钉死"冷却翻转是一枚两响方向"，交给下一把当预论证材料。

**剩余无红格＝5 枚，逐格点名并写缺哪类方向**（波 30 的 16 枚减本刀 11 枚）：

| 格 | 缺的红方向 |
|---|---|
| `The_same_intent_resubmitted_inside_the_cooldown_window_is_deferred_and_names_the_earliest_next_instant` | 方向已实测存在（M8 让它红了）——缺的是**把它与 `An_exhausted_call_budget…` 两枚一起事前点名的批次配方**再来一轮 |
| `An_exhausted_call_budget_stops_advancement_and_names_consumed_remaining_and_the_dimension` | 它自己的预算闸独立摘除方向（`:85` 那条比较的兜底支）本刀没跑；M8 的连带红随作废轮登记 |
| `A_budget_that_is_zero_at_entry_is_a_distinct_shape_from_one_that_ran_out` | 独立方向＝把"入参即零"与"跑光"合流的兜底支改动（同类单点改法），额度装不下 |
| `A_cost_budget_running_out_stops_advancement_while_calls_are_still_available` | 独立方向＝费用闸（`:94`）单摘，额度装不下 |
| `A_quiet_deferred_intent_advances_once_the_window_closes_and_leaves_the_pending_list` | 独立方向＝静默放行后清单移除那一步（`_deferrals.RemoveAll`）的单点摘除，额度装不下 |

（另：波 28 数值锁格 `The_prototype_applies_no_retrieval_floor_…` 照旧押——反证方向只能新增数字门槛，零门槛是作者定的，本刀未造门槛，一枚门槛都没新增。）

### 十.3 未验证与押下一把

- **未验证照旧一条不减**（§九.6 十条原文有效，本刀未触任何一条）：真实模型对照零读数；生产接线零调用方；UI 零读数；真实数据零接触；参数未标定＝特性不是缺陷；并发与断电零读数；旧链删除片与不进包那道门未开始。三句话照旧不许写＝新链已接线／旧链可删／不进包已做到。
- 本刀自身的面：**未验证**项＝无（每一轮十一项字段都有盘上凭据；唯一"未复现"登记＝§〇.6 对 `round.py` SyntaxError 的复验，已在十.0 更正登记）。
- 押下一把：①上表 5 枚各配独立单点或两响批次配方（M8 的两响实测结果可直接作预论证素材）；②归位、顶账、换手三本账本刀零动作（无新字节可落，出口结论＝**零落地、只结证据账**）；③在册 1380 不因本刀移动，同值复采读数已按 §二 登记为"同值复采"。
- `HOLD` 重跑额度使用＝**0/8**（十枚预检 P20–P29 全部一次 `BUILD_OK`）。

### 十.4 本刀文件面

- 主工作树写入＝**仅本报告这一节**（§十 追加；§〇–§九 一字未改，自证＝本节末 `git diff --numstat` 删除列 0）。
- 载体＝临时覆盖＋逐轮 `git cat-file blob HEAD:<path>` 还原，八轮后终态 `status --short` **0 行**、四枚 SHA 等于 §〇.4 基准（`out/w31-final-carrier-proof.txt`）；bin/obj 为构建产物，git 不跟踪。
- `.local-notes/wave29-1024/out/` 新增（全部新文件名，未覆盖任何既有文件）：`preflight-P20.txt`…`preflight-P29.txt`（10 枚）、`w31-build-B1.txt`、`w31-run-M1.txt`…`w31-run-M8.txt`、`w31-run-F31.txt`、`trx-M1`…`trx-M8`、`trx-F31`（各 1 份 TRX，F31 内 4 份＋coverage json）、`w31-mutate.js`／`w31-round.js`／`w31-trx.js`（仪器）、`w31-mutation-manifest.txt`、`w31-mutations/`（8 枚突变副本＋3 枚 base 副本）、`w31-instrument-selfproof.txt`／`w31-instrument-selfproof-F.txt`、`w31-round-M<n>-prep.txt`／`-restore.txt`（16 份）、`w31-trxparse-M<n>.txt`×8／`w31-trxparse-F31.txt`、`w31-final-carrier-proof.txt`、`w31-measure-F31.txt`。
- 探针 `probe/unrun-replay-mark-removed.cs` 施加后已还原探针原字节（未改写探针文件本身）。
- **零 git 写**：无 commit／add／stash／clean／reset／tag／push；他人两枚 `M ` 暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`／`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）、`docs/plans/PROJECT_PLAN.md` 的 ` M` 与五枚 `??` 未跟踪资产一字未动。
- 预算总账：`dotnet`＝**10/10**（B1 1＋定向 8＝M1–M8＋F 1；无 G0——§〇.2 对当前线尾 `b263d7b` 实测 0 行合法跳过）；`.trx` 本刀新增＝**12 枚**（8＋4）；结果目录新增＝**9 个**互不重复；焦点预检 10 枚全 `BUILD_OK`；`measure-fixed-eval.ps1` 1 次（带 `-ResultsDirectory`）。
- 交付复核（写完上一行与本行后当场重跑）：`git diff --numstat -- 本报告` 的**删除列＝0**（添加列为本节全部行数）；`git diff --check` 对本报告零输出；`ls out/trx-M*`＝8 个目录各 1 份 `.trx`、`out/trx-F31`＝4 份；`git -C 载体 status --short`＝0 行；§〇.4 四枚 SHA 终态复核逐字符相符（`out/w31-final-carrier-proof.txt`）。注：`out/preflight-P2.txt` 是第二十九波旧档，本刀预检面只有 `P20`–`P29` 十枚，未覆盖任何旧文件。

## 十一 守门人划账（第三十一波，`eval-QA-WAVE31-GK-1028`）

本节由守门人本人写，不是复述 lane 的自述。下面每一个数都是我在 2026-10-28 13:1x 之后**当场从盘上原始产物重取的**，取数脚本＝我自己写的 `.local-notes/wave29-1024/out/gk31-count.js`（不是 lane 的 `w31-trx.js`，两把尺同口径、各自独立跑，结论相同）。**本刀 `dotnet` 调用＝0 次**：这一把没有新字节要跑，也没有新字节要落，我只做三件事——数盘、复核 lane 的每个数、划账。

### 十一.0 取数口径（照工单 §〇.6 原文执行）

只数 `<UnitTestResult … outcome="Failed">` 这**一个开标签**形状里的 `outcome` 属性；`<ResultSummary outcome="Failed">` 是文件级汇总不算用例，`<Output>` 里的错误文本也不提供用例数；报"恰 N 枚红"之前 N 必须同时等于开标签计数与同份 `<Counters failed=…>`。跳过只按 `<UnitTestResult … outcome="NotExecuted">` 点名，`notExecuted` 计数器恒 0 不看。**十二份 TRX（M1–M8 各 1 份＋F 的 4 份）逐份 `reconcile=OK`，零 MISMATCH** ⇒ 这一把没有任何一次"仪器说没匹配、读数其实变了"的混淆。

### 十一.1 逐轮独立复核（lane §十.1 的每一项我重数一遍）

| 轮 | 盘上 TRX 文件名时间戳 | total／executed | `<Counters failed>`＝开标签数红 | 红格名（逐字符，去掉程序集前缀） | 我的归属裁定 |
|---|---|---|---|---|---|
| M1 | `12_41_01` | 759／757 | 1＝1 | `A_late_advancement_carrying_a_superseded_effect_version_is_refused_and_writes_nothing` | 新凭证；波 30 §九.5 第①条那枚"额度用尽"的格，方向＝给 `ExpectedEffectVersion` 闸加永假条件，与事前论证同格同名 |
| M2 | `12_42_21` | 759／757 | 1＝1 | `The_advancement_coordinator_exposes_exactly_one_public_entry_method` | 新凭证；方向＝往产品文件**新增**一枚公开入口方法（不是摘闸），§九.5 第①条另一半兑现 |
| M3 | `12_43_39` | 759／757 | 1＝1 | `Advancement_prototype_sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites` | 反面探针类，单列；红点＝判据 `:417` 探针那句 `Assert.Equal`（Expected＝五个旧链名、Actual 空串） |
| M4 | `12_44_55` | 759／757 | 1＝1 | `Advancement_prototype_declares_no_source_side_default_for_windows_or_budgets` | 反面探针类，单列；红点＝判据 `:439`（Expected `const,static readonly`、Actual 空串） |
| M5 | `12_46_13` | 759／757 | 3＝3 | `Replay_delivers_each_due_intent_exactly_once_across_two_clock_jumps_and_loses_none`＋`A_due_intent_that_has_not_reached_its_due_instant_is_left_pending_and_still_reported`＋`After_a_clock_forward_jump_due_intents_are_replayed_in_registration_order` | 批次红按 §一.2.4 署给删 `entry.Replayed = true;` 那一行；**新凭证 1 枚**，另两枚＝已付格的第二条因（原凭证＝波 30 R5／R7）；探针 `cdd18e3f…` 从"两把欠着的未跑"转为已跑已还 |
| M6 | `12_47_30` | 759／757 | 4＝4 | `An_advancement_whose_source_cannot_be_pointed_to_fails_with_a_named_reason_and_stays_in_the_log`＋`Missing_source_is_refused_for_every_trigger_kind_with_the_same_named_failure` 的 `req-missing-ue`／`req-missing-due`／`req-missing-bg` 三实例 | **新凭证 3 枚**，第一条已付格的第二条因（原＝波 30 R6）；lane 事前论证"摘掉来源闸之后入口不再拒绝而是抛 `NullReferenceException`"实测成立——按工单 §一.3 第 3 条这是要领的读数，不是仪器坏了 |
| M7 | `12_48_46` | 759／757 | 4＝4 | `A_successful_advancement_carries_the_source_reference_into_the_record_it_produces`＋`Each_trigger_kind_enters_the_one_advancement_entry_and_names_its_own_reference` 的 `ue-1`／`due-1`／`bg-1` | **新凭证 3 枚**，`bg-1` 那条＝已付格第二条因（原＝波 30 R1）；方向＝成功路径把 `SourceId` 换成 `IntentKey` |
| M8 | `12_49_52` | 759／757 | **2＝2** | `The_same_intent_resubmitted_inside_the_cooldown_window_is_deferred_and_names_the_earliest_next_instant`（红点 `:99`，Expected `DeferredByCooldown`／Actual `Advanced`）＋`An_exhausted_call_budget_stops_advancement_and_names_consumed_remaining_and_the_dimension`（红点 `:188`，Expected `StoppedByBudget`／Actual `DeferredByCooldown`） | **维持作废**。工单 §三 M8 行预论证＝"恰 1 枚"，盘上＝2 枚 ⇒ 形状不符即作废，两枚红都不领凭证，我也**不补签、不追第二轮**（定向 8 枚额度已用满，机动位已用于其唯一合法用途） |
| F | `12_51_15`／`12_51_15[1]`／`12_51_19`／`12_51_22` | 四份 `failed` 全 0 | 0＝0（四份） | 无红 | Client 108＋Core 759＋App 233＋Api 280＝**1380**；executed 108＋757＋233＋280＝**1378**；failed **0**；跳过 **2**（`Configured_llama_server_requires_the_child_environment_key`／`Configured_sense_voice_model_can_run_offline`） |

那两条同名跳过在 M1–M8 **每一轮**里都存在（我逐份点名过），不是收尾那轮才出现的，也不是本轮新增的。

### 十一.2 形状与资格复核

- 载体＝`.local-notes/qa-wave29-1024/carrier`，`rev-parse`＝`663d2c9`，`status --short`＝**0 行**（我当场重跑，不是引用 §十.4 那句自述）；`git log 663d2c9..HEAD`＝空 ⇒ lane 提交数 **0**，符合工单 §七"lane 一律不做提交"。
- 四枚文件盘上 SHA-256 **全值**复核（不取前缀）：`90947162fed797f8de98bd5130a11dfacc25886bfa035182dccaf395b9d32b73`／`9bf4dc31bd3d1c724a804f2dc01334806dada6a1c88861eb07f89fc0897ce5a1`／`259474dcccbfb1f0327d92ba5c49e1e8ee35280c16abf6761088332695976ac0`／`1fd944f94d399a8f742482c1f724f8afec150eac865cf06cdfa149d4926a200b`，逐字符等于 §〇.4 基准与 lane 的 `w31-final-carrier-proof.txt` ⇒ 八轮突变全部逐字节还原，本刀在盘上对产品码与判据码的**净写入＝0 字节**。
- 构建门禁＝`w31-build-B1.txt` 尾"已成功生成／0 个警告／0 个错误／用时 2.16 秒"，我 `grep -ci warning` 得 **0** ⇒ 工单 §一 M1 那条"避开 CS1718 自比较告警形状"的写法确实拿到零告警（本仓门禁＝构建零警告，这条是正面兑现不是绕开）。
- 预检面＝`preflight-P20.txt`…`preflight-P29.txt` 十枚逐份 `VERDICT=BUILD_OK`，`HOLD` **0 枚** ⇒ 按刀计的 8 枚重跑额度 **0/8** 未动用。盘上另有 `preflight-P2.txt` 一枚 `HOLD`，那是第二十九波旧档，本刀没覆盖它，也没引用它当读数。
- `measure-fixed-eval.ps1` 落盘读数（`w31-measure-F31.txt`）＝`Catalog valid: 76 permanent scenarios`／`Passed=1378; Failed=0; Other=2`，与四份 TRX 求和自洽；catalog 仍 **76**，未动。
- lane 交付形状＝`git diff --numstat` 对本报告删除列 **0**（添加列 255＝§十 全部行数），`git diff --check` 零输出，本节末我同样复测；`.trx` 本刀新增 12 枚（8＋4）、结果目录 9 个互不重复、`dotnet` 10/10 用满（B1 1＋定向 8＋F 1，无 G0）。
- 三本账的**取红手资格**（我自己数，不复用 lane 的结论）＝载体 `git log` 零新提交＋判据 SHA 入场自取与 F 轮终态同值（`1fd944f9…`）＋对判据文件有临时写入的只有 M3／M4 两轮，而这两轮我已按 §一.3 第 2 条单列排除 ⇒ 其余 9 枚新凭证满足"取红那只手既不是判据字节的作者、也没在当轮写过判据字节"。这条判据第三次被用成动作（波 24／波 27–28／本把）。

### 十一.3 划账裁决

1. **无归位、无代码刀、不顶账。** 理由不是"没跑"，而是"没有新字节可落"：§十一.2 第二、第六条合起来证明本刀净写入 0 字节、载体零提交。把"归位"用在这上面是空转；**出口结论＝零落地、只结证据账**（与工单 §六 的约定一致，lane 交回的就是这一件）。
2. **在册 1380 不移动。** 本刀 F 轮那份 1380／executed 1378／failed 0／跳过 2 登记为**同值复采**（第三十波已顶过账；同一棵树、同一个值，不改写在册数）。等式＝无。
3. **波 30 那 16 枚无红格：本刀结清 11 枚，剩 5 枚。** 11 枚拆法＝改坏产品码之下响过 **9 枚**（M1 1＋M2 1＋M5 1＋M6 3＋M7 3）＋反面探针类 **2 枚**（M3／M4，单列，不作为第二换手签账依据）；另 4 条"已付格的第二条因"登记而不领凭证、不撤原归属（`After_a_clock_forward_jump…`／`A_due_intent_that_has_not_reached…`／`An_advancement_whose_source_cannot_be_pointed_to…`／`Each_trigger_kind…(asked: BackgroundScene)`）。**工单 §三 预写的形状是 12 枚，差额恰是 M8 作废轮的那 1 枚**，按"形状不符照实登记、不改判"处理——我不为凑 12 追第二轮，也不把 §三 改写成"恰好 2 枚"。
4. **两条常绿门疑问结清**（这是本刀最有价值的一条，因为它把波 30 §九.5 第②条那半句还着的账还掉了）。第三十波我自查过那两枚扫描格的"路径坏掉会红不会绿"那一半，还欠"把扫描函数本身弄坏看探针先不先红"这一半；本刀 M3／M4 就是那一半：把被扫集合的匹配条件弄成恒不命中、把两枚被扫 token 本身改掉之后，**同一枚格子里探针那句 `Assert.Equal` 当场红**（`:417`／`:439`），而 `Assert.Empty` 的循环部分保持绿 ⇒ 扫描面真的在遍历、探针真的在咬。这两枚自此读作"不是常绿门"。
5. **M8 的正面收获入账**：实测钉死"冷却比较方向翻转是一枚**两响**方向"（`:99` 那格与 `:188` 那格同时红）。这条直接成为下一把做批次配方的预论证素材，比我工单 §三 那句"恰 1 枚"更接近盘上事实；同时它又是一次"派单手的事前论证被执行手纠正"（本仓第三次：波 28 两条够不着、波 29 §三 表、本把 §三 M8 行），所以 §一.2 那条"事前论证要写死枚数与格名"的纪律照旧，不因这次错而放宽。
6. **旧欠 75 格照旧不减**（波 13 余 47＋波 20 余 28，本刀一枚没碰）；拆法原文照旧 **46／45／1＝W2**；catalog 照旧 **76**。

### 十一.4 我自己的三处更正登记（不改写已提交文本）

1. **提交信息里那句"复测仍 18/0"不成立，实测＝18/11。** 第三十一波工单刀 `b263d7b` 的 message 写了"PROJECT_PLAN 用 hunk 手术……提交后原样还回工作树并复测仍 18/0"。提交后同一条 `git diff --numstat` 返回 **18 11**：HEAD 挪过一格之后，添加列那 18 行仍是他人的研究登记，而删除列那 11 行是**我自己刚提交进去的内容在工作树里被 revert 掉了**（1＝§4.2 的 HDS-007 行、9＝本刀那条 §9 entry 的 header＋8 枚 bullet、1＝波 30 那条 entry 的 header 因位置移动）。真正的"原样还回"证据从来不是 numstat，而是逐字节 SHA——工作树与 `.local-notes/wave31-plan-worktree.backup` 当时逐字节相同（`5e6c7242…`）。我不涂改那条已提交的文字，在这里更正，并把根因写成口径：**hunk 手术的还原目标是"提交后的新内容＋他人行"，不是"提交前的备份原样"**；后者会在工作树里留下一枚假回滚，下一把若从工作树做起就会把我的 §9 整条抹掉。本刀（收口刀）就按前者做，并在 §十一.6 自证还原后的 diff 恰为 **18/0**。
2. **工单 §〇.2 不该把 diff 式子的右端写死成某个 SHA。** 出闸时线尾是 `3044c1c`，派发刀自己一提交就把它挪成 `b263d7b`；lane 现场按**当前线尾**复核同一条式子并如实登记（§十.0 〇.2），这是对的。以后工单这一条改写成"右端当场 `git rev-parse HEAD` 取，且必须取在 §〇.1 那个 0 行状态之后"，不写死值。
3. **我上一把留给下一把的那条前置作废。** "复用同棵载体前先修 `round.py` 的失败消息打印段（Python 3.14 下抛 `SyntaxError`）"——我当场 `python -m py_compile` 与 `ast.parse` 各跑一次，在 **3.14.5** 下**均未复现**（lane 在 §十.0 〇.6 也独立登记了同一个"未复现"）。所以那两条结论要分开写：① 那条前置不该派（我把一次未留证据的观察当前置用了）；② 但 lane 这一把**仍然自写了取数仪器并对第三十波的已知读数做了自证**（§十.0 〇.6 三条：`trx-R7` 恰 1 枚红、`trx-G0` 全绿、`trx-F` 四份求和 1380），所以"不许为了省事沿用未自证的仪器"这条前置本身是达成了的。教训升成口径：**交给下一把的前置必须带可复现证据（命令＋版本＋错误原文）**，否则就是把推测当前置用。

### 十一.5 未验证面与押下一把

- **未验证面一条不减**（§八.3 十条原文有效，本刀是证据刀，没有触到任何一条）：真实模型对照零读数（需作者一句"可以跑"）；生产接线零调用方；UI 零读数；真实数据与 `%LOCALAPPDATA%\PryCompanion` 零接触；参数未标定＝特性不是缺陷（冷却时长／静默窗口／预算默认值全部未定，原型内至今一处数字门槛都没有）；并发两枚 `Advance` 与断电恢复零读数；旧链删除片与"不进包"那道门未开始。
- **三句话照旧不许出现**＝新链已接线／旧链可删／不进包已做到。
- **押下一把（直接抄 §十.2 那张表，每格配独立方向，配额与轮次另出工单）**：
  1. `A_cost_budget_running_out_stops_advancement_while_calls_are_still_available`＝费用闸单摘（`AdvancementCoordinator.cs:94` 那半句比较）。
  2. `An_exhausted_call_budget_stops_advancement_and_names_consumed_remaining_and_the_dimension`＝调用预算闸单摘（`:85` 那条的兜底支）。它已在 M8 连带响过一次，但那一轮作废 ⇒ 本格**仍无清洁凭证**，别把 M8 那次算进来。
  3. `A_budget_that_is_zero_at_entry_is_a_distinct_shape_from_one_that_ran_out`＝把"入参即零"与"跑光"合流的那条兜底支改动。
  4. `A_quiet_deferred_intent_advances_once_the_window_closes_and_leaves_the_pending_list`＝静默放行后 `_deferrals.RemoveAll` 那一步单点摘除。
  5. `The_same_intent_resubmitted_inside_the_cooldown_window_is_deferred_and_names_the_earliest_next_instant`＝**只能用两响批次配方**（M8 实测：冷却翻转同时响它和 `:188` 那格）；事前点名两枚格名、预论证枚数必须写"恰 2 枚"，写"恰 1 枚"会再次作废。
- 另三本照旧押着不动：波 28 那枚数值锁格 `The_prototype_applies_no_retrieval_floor_…`（反证方向只能靠新增数字门槛，零门槛是作者定的，我不为凑红造门槛）；两套并行命名债（四枚 `Hds006` 前缀产品类型＋三身份枚举与契约 §7.2 的 `CognitionKind`／`CognitionOrigin` 并存）属**接线刀不属证据刀**；HDS-010 删除片在"替代"拿到实测之前不可起跑，而"不进包"在不删码的前提下没有机制（发布链经 `src/Pry.App/Pry.App.csproj:16-19` 拉全部四枚程序集）。
- 下一把仍复用这棵 `663d2c9` 载体还是新建，由 §〇.2 那条式子**当场**重取后再裁；本刀不改写在册数，故下一把起点照旧 **1380／executed 1378／failed 0／跳过 2**。

### 十一.6 本刀（守门人收口刀）文件面与自证

- 写入＝本报告这一节（§十一 追加在文末，§〇–§十 一字未改）＋`docs/plans/PROJECT_PLAN.md`（§4.2 的 HDS-007 行收口＋§9 一条新 entry）＋`docs/full-scope-sprint.md`（§0 事实部分两句＋§3 一条密行）。**零 `dotnet`**；`git ls-files` 顶账在提交后当场实测（三处都是既有跟踪文件，不新增跟踪文件）。
- `PROJECT_PLAN` 的他人 18 行研究登记（两枚 `RES-*` 表格行＋两枚 §9 entry）不进本把提交：本把**先把我的内容铺在 HEAD 上提交，再把他人那 18 行原样铺回"提交后的新版本"**，与 `b263d7b` 那把的做法不同（差别就是 §十一.4 第 1 条）。自证＝提交后 `git diff --numstat -- docs/plans/PROJECT_PLAN.md` 必须读到 **18 0**（不再是 18/11），且他人两枚 `M ` 暂存项（`1 1`／`63 0`）与五枚 `??` 未跟踪资产一字未动。
- 交付复核（写完本节与上一节之后当场重跑）：`git diff --numstat -- 本报告` **删除列＝0**；`git diff --check` 对本报告零输出；`ls out/trx-M*`＝8 个目录各 1 份 `.trx`、`out/trx-F31`＝4 份；`git -C 载体 status --short`＝0 行；§十一.2 那四枚 SHA 全值终态复核逐字符相符。
- 报告号自此＝`eval-QA-WAVE31-GK-1028`（守门人划账），与 lane 的 `HDS-WAVE31-1028` 同场并存：**lane 的原文（§十）一字未改，我的复核（§十一）只追加**。
