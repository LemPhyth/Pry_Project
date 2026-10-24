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




