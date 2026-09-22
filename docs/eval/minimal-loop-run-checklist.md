# Phase 1 最小闭环走查清单（"能跑通"的判据）

制定：EVAL-001（QA / Evaluation）　日期：2026-09-22　§3 于同日按 Core 两项核实结果改判（见该节末"QA 裁定记录"）；同日第七轮走查后补 §3.5/§3.6 与 ① 的一条禁行项
依据：**作者 2026-09-22 裁定，Phase 1 出口条件变更为"最小程度的能跑"**——"使用后能得到答复：Client 启动后能通过 Core 调用模型，模型生成回复后 Core 能够保存并且返回给 Client，其他的先不管"（口径参照旧 v0.0.x 发行版：在作者机器上跑得起来）。
本清单即该裁定的可执行形式。**它取代第六轮判定报告里的阻塞清单作为 Phase 1 的结案判据**；被移出出口条件的项一律记 `NotApplicable`（写明原因），永久场景 ID 全部保留、不删不复用。

## 判定规则

| 步骤 | 权重 |
|---|---|
| ① 一句话得到回复 | **必需** |
| ② 重启后历史仍在 | **必需** |
| ③ 断一次后端不卡死 | **必需** |
| ④ 以上发生在一个可指认的提交上 | **必需** |

四条全过 ⇒ Phase 1 **通过**（工程层）；任一条不过 ⇒ 记 Fail 并指名 Owner。跑之前必须先读第 0 节，跑完按第 6 节表格逐格填证据，**不接受"我看着没问题"**。

## 0. 预备（3 分钟，不做完不要往下走）

```powershell
# 0.1 干净数据目录（先确认里面没有你正在用的库；要保留就把旧目录改名而不是删）
Rename-Item "$env:LOCALAPPDATA\PryCompanion" "PryCompanion.keep-$(Get-Date -Format yyyyMMdd-HHmm)" -ErrorAction SilentlyContinue
dotnet build Pry.slnx -c Release
```

记录三件事，后面的证据要靠它们对账：

1. **本次用哪一档模型**：本地 `llama-server`（GGUF 权重名）还是远端 HTTP Provider（**只记 Provider 名与模型名，绝不记密钥**）。
2. **HEAD**：`git rev-parse --short HEAD`。
3. **数据目录**：`%LOCALAPPDATA%\PryCompanion`（库里是 `memory.db`，独立进程发现文件是 `runtime.json`）。

> 边界：模型密钥与本地权重由作者本机提供，**QA 不读取、不代填、不写入任何文档**。若这一步起不来，记 Blocked 并注明"缺可用模型档位"，不要改用假响应糊过去——那等于把本次走查的主判据删掉。

## 1. ① 启动 → 发一句话 → 得到回复

**操作**：`dotnet run --project src/Pry.App -c Release --no-build`（或跑已构建的 exe）。等主窗口出现 → 新建/进入一个房间 → 输入一句有上下文的话，例如"我叫小王，我周三要去医院复诊，帮我记一下" → 发送。

**必须观察到**（缺一即 Fail）：
- 窗口状态从"发送中"走到结束，**没有出现"模型回复失败"或"事件连接中断，正在重连…"常驻不动**；
- 界面上出现**属于这条输入的一句答复**，且答复内容能体现它真的读到了这句话（复述/追问"复诊"或"小王"皆可，不要求固定台词）；
- 用户消息与 Agent 消息**各只出现一次**（重复渲染属 `PLT-009` 电平/事实混淆的现场形态）。

**禁行结果**：无限转圈；半截回复被当完整答复；发送态永不落下；错误文案只说"连接中断"而不区分"模型没答"与"连不上后端"；**模型的内部草稿/`<RESPONSE>` 信封被当作用户可见答复落库或渲染**（`DEF-P1WALK-01` 的形状，09-22 走查后补——原稿只有"半截回复"，拦不住反向情况）。

**证据**：
- 截图 → `docs/eval/walkthroughs/<runid>/01-reply.png`（要能看到输入气泡 + 答复气泡 + 状态栏）；
- 数据库侧对账（证明"Core 保存了"，不是界面自己画的）：

```powershell
# 期望：用户消息 1 条 + Agent 消息 1 条，都在同一 conversation 下
sqlite3 "$env:LOCALAPPDATA\PryCompanion\memory.db" "select role, substr(content,1,40) from messages order by id desc limit 5;"
```

（`sqlite3` 不在 PATH 时，用任意 SQLite GUI 打开那个库，截同一张表。）

## 2. ② 关掉再开，历史还在

**操作**：完全退出应用（不是最小化）→ 确认 `runtime.json` 已随优雅退出被删除 → 重新启动 → 进同一个房间。

**必须观察到**：
- 第 1 步那两条消息**仍在**，顺序正确；
- 房间列表里那个房间的预览文字与最后一条一致（`ConversationPreviewText` 的现场判据）；
- **不会**把上一次会话的"发送中/正在重连"状态带进来；
- 新进程的 `runtime.json` 重新出现，端口/令牌与旧的无关（旧令牌此刻应当已失效）。

**禁行结果**：重启后消息消失（保存其实没落库）；重启后旧 Agent 回复被再投一次（游标/重放错误，`PLT-005`/`PLT-009` 的现场形态）；陈旧 `runtime.json` 让新实例连向死亡端口（`PLT-003`）。

**证据**：`02-restart-history.png` + `runtime.json` 存在性两条命令输出。

## 3. ③ 断一次后端，界面不卡死

**本步只有一种可执行形状**：UI 进程活着、后端宿主进程死了、事件流因此断掉。制造它必须先让后端**不在** UI 进程里——所以第 0 节之外的这条前置不能省。

### 3.1 连接拓扑与可选注入法

| 法 | 做法 | 是否算 ③ 的证据 | 依据 |
|---|---|---|---|
| **A′（唯一主形态）** | 先 `dotnet run --project src/Pry.Api -c Release --no-build` 起独立后端（它写 `%LOCALAPPDATA%\PryCompanion\runtime.json`）→ 再启动 `Pry.App`（`App.axaml.cs:47` 先跑 `RuntimeDiscovery.TryConnectAsync()`，命中即接独立后端）→ **确认接的是发现来的那条**（见 3.3）→ 任务管理器结束 `Pry.Api` 那个进程，UI 不动 | **是** | 杀完只剩远端 socket，UI 才有"断线"可判；同一条链顺带兑现 `PLT-003`（发现）与 `PLT-002`/`PLT-006`（重连与终止出口） |
| A（原写法，撤销） | "结束内嵌宿主所在的进程" | **否** | 内嵌时宿主与 UI **同进程**（`App.axaml.cs:63` 的 `BackendApplication.BuildAsync` + `_backend.StartAsync()`），杀进程＝连 UI 一起没了，那是步骤 ② 的形状，不是断线 |
| B（撤销） | 改 `Pry:Url` 指到不监听端口再启动 | **否** | `App.axaml.cs:64` 在内嵌分支**强制** `builder.Configuration["Pry:Url"] = "http://127.0.0.1:0"`，用户设的端口到不了 Kestrel；`:47` 的独立分支只认合法 `runtime.json`，不认配置 |
| C（重分类） | 拔网 / 关 Wi-Fi | **否，归 ①** | Client↔Backend 永远走回环，断外网不断本机事件流；它改变的是**出站 Provider**，所以判的是"模型没答"的文案分档，不判重连链 |
| D（重分类） | 结束 `llama-server` 子进程 | **否，归 ①** | 产生的是 `turn.failed` / Provider 失败，不是事件流断连。跑它时按 ① 的禁行结果看：**必须区分"模型没答"与"连不上后端"** |
| E（不要求真机） | 真半开：连上了但永不发响应头 | 走查记 `NotApplicable` | 同进程内嵌下无法安全制造；该形状已由自动化 `An_event_subscription_that_never_gets_headers_fails_on_the_local_budget` 判住，不为走查新造设施 |

> C/D 不是"降级为可选"：它们各自仍是**有效判据**，只是判的是 ① 的错误呈现分档。若顺手做了，证据写到 `01b-*`，别写进 ③。

### 3.2 阈值（QA 从被检代码实算；四条各自独立，不许互相顶）

| 判据 | 阈值 | 推导（全部读源码，非估计） |
|---|---|---|
| **③a 首条可见结论**（状态栏出现"正在重连…"或明确报错） | **≤ 40 s** | 最坏是半开形状要等起头预算到点：`PryProtocol.LivenessTimeoutMs = HeartbeatIntervalMs * 2 + 2_000 = 32 s`（`src/Pry.Contracts/PryProtocol.cs:41,47`），40 s 是它 + 余量。A′（进程消失 → 连接立即被拒）下应当 **约 1 s**，实测若到十几秒要追问 |
| **③b 终止态**（停下并说明原因，即 Runner 的 `Faulted` 出口） | **≤ 55 s** | `ConversationStreamOptions.Default = (500 ms, 15 s, 8)`（`src/Pry.Client/ConversationEventStreamRunner.cs:18-19`）⇒ 放弃前须失败 8 次，而 `BackoffAsync` 在计数到 8 时**不再 sleep 直接终止**，故退避总时长 = `DelayForAttempt(1..7)` = `0.5+1+2+4+8+15+15 = ` **45.5 s**；A′ 下每次尝试本身近乎瞬时 ⇒ 45.5 s × 1.2 ≈ 55 s |
| **③c 半开形状终止态**（默认不跑） | **≤ 330 s** | 同一条 45.5 s 退避，但 8 次尝试各吃满 32 s 起头预算 ⇒ 45.5 + 256 = **301.5 s**，取整到 330 s。**不得用 ③b 的 55 s 判它**，那会把正确行为读成 Fail |
| **③d 禁令类**（426 协议不匹配 / 401 令牌被拒，服务端 `retryable:false`） | **第一次失败即终止，≤ 2 s，零退避** | `BackoffAsync` 的禁令分支排在计数与退避**之前**（同文件 `:110-117`，注释即判据）。超过 2 s 或看到任何一次"正在重连"⇒ `PLT-002` 现场 Fail |

**必须观察到**：
- ③a 与 ③b 依次成立，且终止态那一屏**说清是什么断了、下一步做什么**（文案出自 `PryErrorPresenter.Describe`，`src/Pry.Client/PryErrorPresentation.cs:41,46`——`Classify` 是 private，判文案要看 `Describe` 返回的 `PryUserError`；此处曾写错，Core 09-22 登记后由 QA 自改），不许是裸异常堆栈；
- ③d：禁令类**不重试**并给可执行下一步；
- 重启后端（或直接重跑 A′ 的前半）后**能继续对话**，且断线期间落库的回复在下次进入房间时出现。

**禁行结果**：无限期停在"正在重连"而无后续；把取消/正常关闭当成断连无限重连；把 426/401 当网络抖动重试。
（已知遗留、不翻 Fail：`RemoteConversationTurnManager` 那条追加渲染的循环刻意不挂 `Resumed`，那条路径上断线窗口的回复要等下次进房间才出现——见 `PLT-006` 的硬保留。走查默认走 `PryMessengerWindow`，若你正好切到那条路径，写进备注。）

**证据**：`03-disconnect.png`（终止态那一屏，含状态栏文案）+ **两个秒表数**（断开→首条可见结论；断开→终止态），分别对 ③a / ③b，还有一个"本次拓扑＝发现"的确认（3.3）+ 实际用到的法（A′）。

### 3.3 "确认接的是发现来的那条"怎么证

```powershell
Get-Content "$env:LOCALAPPDATA\PryCompanion\runtime.json"      # 端口/ pid
Get-Process -Name Pry.Api,Pry.App -ErrorAction SilentlyContinue | Select-Object Id,ProcessName,Path
```
`runtime.json` 里的 `pid` 应当等于那个独立 `Pry.Api` 进程，且**端口不是 5078 也无所谓**（内嵌分支只会用 `:0`）。若 UI 起的是一个 `:0` 端口、`pid` 指向 UI 自己，说明它回落到内嵌了——此时杀进程不构成 ③ 的判据，重来。

### 3.4 QA 裁定记录（本节为什么和初稿不同）

Core 在走查前核实出两项，**两项都成立，我按成立处理**：

1. **原稿"超过 40 s 无结论即 Fail"是我自己的 oracle 缺陷，不是产品缺陷。** 一条阈值同时被判成两件事："多快开始解释"（③a）与"多久彻底放弃"（③b）。后者由退避上限决定，而退避上限实算 **45.5 s > 40 s** ⇒ 按原稿，一个行为完全正确的客户端会在第 40 秒被判 Fail。这属"测试/清单错误"，按 `docs/eval/README.md` 发布门槛表的口径**不能用来推断产品错误**，所以我改的是清单的分档与阈值，**不是任何产品参数**。
2. **`ConversationStreamOptions.Default` 的 8 轮退避不动，这是对的。** 它是 `PLT-002`（禁行必须停 / 未宣告必须订满 8 轮）与 `PLT-006`（续订与恢复）的判据本体，15 例钉住；为凑 40 秒去改 `MaximumConsecutiveFailures` 或 `MaximumDelay` 等于反向删判据，我会把它记成产品侧 Fail 而不是"对齐清单"。
3. **原稿的 B 在内嵌拓扑下不成立，已撤销**；顺带把"杀 `llama-server`"从 ③ 挪到 ①（它产生 `turn.failed`，不是事件流断连）。这两条不是我推断的，是读 `App.axaml.cs:47,63-64` 与 `Program.cs:21` 得到的，且 Core 的复现结论与我读到的一致。
4. **一条留给作者的需求判断（不属本清单能判的范围）**：`Pry:Url` 目前只在**独立运行 `Pry.Api`** 时被尊重（`Program.cs:21`，默认 `http://127.0.0.1:5078`），内嵌时一律被 `:0` 覆盖。如果产品上希望"用户能指定端口"，那是新能力，要走 PLAT 线开任务，**不能**在走查里当成缺陷。

### 3.5 A′ 的可达性（09-22 第七轮实测，QA 手上跑不了）

A′ 是**唯一逻辑正确**的形状，但它要求独立后端把 `runtime.json` 写进 UI 会去读的那个目录，而：

- **写侧认配置**：`RuntimeDescriptorService.ResolveDataConfiguration`→ `Pry:DataDirectory`（`Pry__DataDirectory` 可注入）；
- **读侧不认**：`App.axaml.cs:47` 调 `RuntimeDiscovery.TryConnectAsync()` **无参** ⇒ 永远读 `PryProtocol.DefaultDataDirectory`（`%LOCALAPPDATA%\PryCompanion`），而 `%LOCALAPPDATA%` 不能按进程改定向。

⇒ 用隔离数据目录就跑不出"UI 发现独立后端"；要用 live 目录跑，就会让实验打开**作者本人**的 `memory.db` 并触发 `MediaCleanupService`。所以第七轮把 ③ 记 **Blocked（F-1）**，解阻只有两条路：作者授权 QA 对 live 目录跑一次，或作者本人跑一次把两个秒表数与终止态文案交给 QA。**不得**为了走通而把阈值、拓扑或本清单删掉。

### 3.6 真机 UI 注入方法学（有前台全屏程序的机器上必读）

- 截图用 `PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT=2)`，**不要**用屏幕区域拷贝——被前台程序遮挡时拷到的是别人的画面。
- 输入用 `PostMessage` 的 `WM_LBUTTONDOWN/UP`、`WM_CHAR`、`WM_KEYDOWN`（客户区坐标），**禁止** `SetForegroundWindow` + `SendKeys`：实测焦点会在 1 秒内被前台程序抢回，按键有落进别的程序的风险。
- 令牌等敏感字段只记"长度 + SHA256 前 8 位"，值不入档。
- 不要在脚本工作目录里放 `inspect.py` 之类的文件名——它会遮蔽标准库 `inspect`，在打印 traceback 时被导入并执行。

## 4. ④ 可指认的提交

**操作**：把 Phase 1 批次（含 `docs/eval/`、`scripts/measure-fixed-eval.ps1`、本清单与走查证据）形成**一个**可审查提交，然后：

```powershell
git status --short --branch          # 期望：Phase 1 相关条目为 0 或明确归属他人
dotnet build Pry.slnx -c Release
dotnet test Pry.slnx -c Release --no-build
git diff --check
```

**必须观察到**：`git log -1` 给出的 SHA 就是本次走查跑的那个；别人在**干净 worktree** checkout 该 SHA 后，第 1 步能重复出答复。
**为什么这条不能省**：v0.0.x 的经验是"在我机器上能跑"这句话没有可指认的对象，所以既不能证伪也不能复用。
**门禁与走查的关系**：`QA-P6-01`（探测用例并行下转红）已于 2026-09-22 由 Owner 按方向 A 修好、QA 复跑连续两次 628/0/2 全绿并复判 `REC-005` = **Pass**，故本条按"退出码 0"要求。通用口径仍写在这里：**只要 `measure-fixed-eval.ps1` 退出码为 1，那一轮的绿灯数字不可引用**，二者不能同时声称。
**提交前的范围核对（Core 09-22 实数，走查时须复核）**：183 条未提交 = 51 改 + 132 未跟踪（107 `.cs` / 17 `.md` / 1 `.ps1` / 1 `.patch`，无大文件，`data/` 已 gitignore）。**风险最高的一项是 `docs/eval/**`（68 个永久场景 + 六轮报告）与 `scripts/measure-fixed-eval.ps1` 受跟踪数为 0**——一次误 `git clean` 即清空六轮判定，属 EVAL-001，我按"优先于任何 Phase 2 新代码"提请作者。`docs/character-learning-safety.patch`、根目录 `Pry_1.0_Rewrite_Architecture_Plan_v3.md`、`.gitignore` 的 `Sample/` 行**归属其他 Owner，不在本条的提交面里**，不得顺手打包。

## 5. 移出出口条件的项（按作者裁定记 `NotApplicable`，ID 保留）

| 项 | 新状态 | 原因 |
|---|---|---|
| Timing / Behavior / Expression / Memory / Relationship / Capability 六类 Eval | NotApplicable（Phase 2） | 判据是"越来越像角色"，不属"能跑通" |
| 真实模型对照、P50/P95 | NotApplicable（Phase 2） | 同上 |
| `PLT-010` 大媒体引用、`PLT-012` 残留子进程 | 保持 Blocked / NotRun | §5.0 冻结与环境门控，不因本裁定解冻 |
| `PLT-005`/`PLT-007` 的 OS 级证据要求 | 不采纳为口径 | 目录 `scenario` 文本已满足；`QA-P6-02` 关闭，真机部分并入本清单第 1–3 步 |
| `QA-P6-01` 红门禁 | **已结案**（09-22 复判 `REC-005` = Pass） | 先降级为非阻塞项，Owner 随即按方向 A 修好；见 `reports/2026-09-22-qa-p6-01-probe-flake-handoff.md` |
| ③ 的真半开形状（E） | NotApplicable（走查） | 同进程内嵌无法安全制造；自动化用例已判该形状 |

## 6. 运行记录（跑完逐格填，空格不算跑过）

> **`eval-QA-P1WALK-0922` 已跑并逐格填毕**，记录在 `reports/2026-09-22-phase1-minimal-loop-walkthrough.md` 第 7 节（该节即本表的填写结果）。结果：**① Pass、② Pass（2.4 一项未手工验证）、③ Blocked（F-1）、④ 待作者裁定 ⇒ Phase 1 未结案，无 Fail 项**。下表留空作为**后续轮次的空白模板**。

| Run ID | `eval-QA-P1WALK-<MMDD>` |
|---|---|
| HEAD | |
| 模型档位（**不记密钥**） | |
| ① | Pass / Fail ＋ 证据路径 ＋ sqlite3 输出行数 |
| ② | Pass / Fail ＋ 证据路径 ＋ `runtime.json` 删除/重建两条输出 |
| ③ | Pass / Fail ＋ **用的哪种注入法（A′）** ＋ ③a 秒表 ＋ ③b 秒表 ＋ 终止态文案原文 |
| ③d（若顺手制造 426/401） | Pass / Fail / 未做 ＋ 是否零退避 |
| ④ | Pass / Fail ＋ 提交 SHA ＋ 干净 worktree 复现结论 |
| 结论 | Phase 1 通过（工程层） / 不通过 ＋ Fail 项 Owner |
