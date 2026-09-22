# Phase 1 验收判定 · 第七轮（最小闭环走查）

Run ID: `eval-QA-P1WALK-0922`　日期: 2026-09-22 18:47–19:15 本地
判据: `docs/eval/minimal-loop-run-checklist.md`（作者 09-22 裁定"最小程度的能跑"的可执行形式）
前序: 第六轮 `eval-QA-P1ACC-0922`（00:40–01:25）→ 本轮是该裁定下的**第一次真机走查**
候选: 分支 `codex/ui-main-window-redesign`，HEAD **`f1c5118`**（与第六轮相同，期间无人提交）
工作区: `git status --porcelain` **183 条（51 改 + 132 未跟踪）**——与 Core 09-22 实数逐位一致；`git ls-files docs/eval` = **0**，`git ls-files scripts/measure-fixed-eval.ps1` = **0**
执行: EVAL-001（QA / Evaluation）。**本轮未修改任何产品代码、未修改任何产品参数。**

**结论：Phase 1 仍未结案，但不是因为跑不通。** 四条出口条件里 **① Pass、② Pass（含一项未手工验证，见 2.4）、③ Blocked、④ 待作者裁定**。①② 合起来说明作者裁定的那句话——"Client 启动后能通过 Core 调用模型，模型生成回复后 Core 能够保存并且返回给 Client"——**在本机真实成立**：36.9 秒完成一次带上下文的往返，落库 1 条 user + 1 条 assistant，杀掉重开后两条都还在、顺序不变、房间预览与最后一条一致。

③ 的 Blocked 与产品无关，是**两条我此前没预料到的可达性事实**（第 3 节），需要作者给一次授权或一次手工动作。④ 是提交面决定，本来就归作者。

同时本轮查出**一条新的产品缺陷**（`DEF-P1WALK-01`，严重度 High）：模型输出的内部草稿被当作用户可见答复落库并渲染。它**不在**四条出口条件的字面判据里，是否阻塞 Phase 1 由作者裁定——我不擅自把它升成门禁，也不隐瞒（见 1.4 与第 5 节）。

---

## 0. 预备（清单第 0 节三件事）

1. **模型档位**：本地 `llama-server`，主档 `discovered-023eafa58ef5e865` → `models/Qwen3-4B-RPG-Roleplay-V2/Qwen3-4B-RPG-Roleplay-V2-Q4_K_M.gguf`（2.3 GB，本机已有）。本轮另自动拉起 `qwen2.5-vl-3b-local`（`qwen2.5-vl-3b-instruct-q4_k_m.gguf`）。设备 `NVIDIA GeForce RTX 5070 Ti`，15037 MiB 空闲，CUDA0。**未读取、未记录任何密钥**（`LLAMA_API_KEY` 只出现在子进程环境，不在本档）。
2. **HEAD**：`f1c5118`。
3. **数据目录**：**未使用** `%LOCALAPPDATA%\PryCompanion`。走查跑在隔离目录 `.local-notes/p7/walk`（`Pry__DataDirectory` 注入），里面是**新建**的 `memory.db`（0 条历史）+ 从 live 目录**只读复制**的 `preferences.json` / `character.json` / `characters/` / 媒体目录。作者 live 库（32 条消息）全程未被写入，事后核对 `runtime.json present = False`、`memory.db present = True`，未被改动。

> 为什么不用清单第 0 节写的"把 live 目录改名"：改名会直接动作者正在用的库，而 `Pry__DataDirectory` 已能把**写侧**（`memory.db`、`preferences.json`、角色、日志、`runtime.json`）全部引开。代价是**读侧**（客户端发现）不跟着走——这条代价正是 ③ Blocked 的成因，记在第 3 节，属于本轮的真实产出之一。

环境事实（走查中实测，供后续复用）：

| 观察 | 实测值 | 出处 |
|---|---|---|
| 内嵌后端端口 | `3674`（第一次进程）→ `7399`（重开后） | `app.out.log` / `runtime.json` |
| 首次请求拉起模型 | `POST /turns` 之后才 spawn，加载 **3719 ms**，**88.13 tok/s** | `app-filtered.log`（`ModelProcessRegistry`） |
| 上下文降级 | `started with reduced context 8192 after the requested context could not be allocated`（两个模型都触发） | 同上，`warn` |
| 持久层 | `schema 9 → 9`，47 ms | `api2.out.log` |

---

## 1. ① 一句话得到回复 —— **Pass**

### 1.1 操作与时间线

| 时刻（本地） | 动作 | 证据 |
|---|---|---|
| 18:47:36 | 启动 `Pry.App.exe`（隔离目录） | `walk/` 生成新库；`app.out.log` 有 `本地服务已连接` |
| — | 主窗口空状态，状态栏 `本地服务已连接` | `walkthroughs/eval-QA-P1WALK-0922/00-startup.png` |
| 19:01:xx | 输入框落定清单原句"我叫小王，我周三要去医院复诊，帮我记一下" | `01a-typed.png`（输入框内正是该句，无残留） |
| 19:02:02 | 点 `发送` | `send.time.txt`；DB `messages.id=1 created_at=11:02:02.801Z` |
| 19:02:08 | 状态栏进入 `Pry 正在思考…`，`发送` 置灰，出现 `停止回复` | `01b-thinking.png` |
| 19:02:39 | 答复落库并渲染 | DB `messages.id=2 created_at=11:02:39.684Z`；`01-reply.png` |

**端到端 36.9 秒**（含模型冷启动 3.7 秒与 691 字生成）。

### 1.2 必须观察项逐条核对

| 清单要求 | 结果 |
|---|---|
| 状态从"发送中"走到结束，无"模型回复失败"/"事件连接中断，正在重连…"常驻 | **满足**——`01-reply.png` 里 `停止回复` 已消失、`发送` 恢复可用、状态栏回到 `本地服务已连接` |
| 出现属于这条输入的答复，且答复体现它读到了这句话 | **满足（字面）**——答复内含"好的，小王！我记下了你周三要去医院复诊。记得带上你的病历卡…"，人名/时间/事项三项都对上了 |
| 用户消息与 Agent 消息各只出现一次 | **满足**——DB 全表 `count=2`，`role=user` 1 条 + `role=assistant` 1 条，同一 `conversation_id=be4f470f…`；界面上两个气泡各一 |

### 1.3 数据库侧对账（证明"Core 真的存了"）

```
messages: 2 rows
  id=1 conv=be4f470fb20249b3b7cbbc9463d81f16 role=user       source=user_input   ts=2026-09-22T11:02:02.8010432+00:00 len=20
  id=2 conv=be4f470fb20249b3b7cbbc9463d81f16 role=assistant  source=model_reply  ts=2026-09-22T11:02:39.6840865+00:00 len=691
conversations: 1 row, title=首条用户消息, last_read_message_id=2
memories: 0 rows
```

（读侧一律 `mode=ro`；`content` 只 dump 到 `.local-notes` 的临时文件用于判定，不入档。）

### 1.4 本轮查出的新缺陷 `DEF-P1WALK-01`（High）—— 不属四条出口条件，交作者裁定

**现象**：`messages.id=2` 的 691 字正文**主体是模型对系统提示词的自我分析**，而不是答复；真正的答复被包在未解析的信封里。落库文本原样结构（节选，Owner 定位用）：

```
以下是对上述内容的进一步分析和总结：

1. **角色定位**：你被设定为一个生活在幻想世界的魔界公主，…
2. **场景描述**：你目前处于一个群聊环境中，…
5. **JSON Schema**：你被要求按照指定的结构输出结果。…
7. **潜在挑战**：在群聊环境中记住并提醒特定用户的请求可能会比较困难，…</thinking><RESPONSE>{ "messages": [
    { "role": "user",      "content": "我叫小王，我周三要去医院复诊，帮我记一下" },
    { "role": "assistant", "content": "好的，小王！我记下了你周三要去医院复诊。记得带上你的病历卡和所有相关的医疗文件哦~ 🩺😊" }
] }</RESPONSE>
```

**已核实的事实**（不是推断）：
- `source=model_reply`、`role=assistant` ⇒ 这段文本是**被当作正式答复持久化**的，不是某条内部日志串了表。
- `01-reply.png` 与 `02b-roomlist.png` ⇒ **界面逐字渲染**这段内容，房间列表预览也取到了它（`以下是对上述内容的进一步…`）。所以这不是"存得脏、显示干净"的两态问题，用户确实会读到。
- 传输链正常：`POST /conversations/{id}/turns → 202`，`GET /conversations/{id}/events?after=-1` 订阅在位（`app-filtered.log:45,432`）。**缺陷不在 `PLAT-005` 的事件流上**。
- `memories` 表 **0 行** ⇒ "帮我记一下"没有产出任何记忆条目。这条**不算 ① 的判据**（记忆抽取属 MEM/Phase 2），但和上面那段"分析自己在群聊里要记住并提醒用户"合起来看，指向同一个可疑面：**送进模型的提示词与拿回来的输出之间，缺少"按契约解析/剥离内部段"这一环**。

**归因（只指认，不代改）**：Character Intelligence / 提示词装配与输出解析线。两个候选根因需要 Owner 分辨，QA 不再往下挖：
- (a) 该轮装配用了带"按 JSON Schema 输出"要求的提示词模板，而 `Qwen3-4B-RPG-Roleplay-V2` 把它**当成待总结的文档**而不是待遵守的指令；
- (b) 模型确实按信封输出了，但 Core 侧没有 `</thinking>` / `<RESPONSE>` 的剥离与解包，于是整段原样落库。

**为什么我不直接判 ① Fail**：清单 ① 的三条"必须观察"字面全部满足（1.2），而"禁行结果"里最接近的一条是"**半截回复被当完整答复**"——本轮是它的**反向形状**（内部草稿被当完整答复），清单没写。按 `docs/eval/README.md` 的口径，判据缺口不能倒推成产品 Fail；但我把它写死在这里，**下一次跑清单必须能拦住它**（已补，见 6.2）。

---

## 2. ② 关掉再开，历史还在 —— **Pass**（2.4 有一项未手工验证）

### 2.1 退出方式与它的偏差（先说清楚）

实际用的是 `taskkill /T /F`（19:06:23，`kill.time.txt`），**非优雅退出**。原因见 2.4：本应用**没有**窗口内或可脚本化的优雅退出口。非优雅退出在这里反而是有用的对照——它把"陈旧描述符"分支跑出来了。

### 2.2 必须观察项逐条核对

| 清单要求 | 结果 | 证据 |
|---|---|---|
| 第 1 步两条消息仍在、顺序正确 | **满足** | 重开后 `messages` 仍 2 行，`id/role/source/created_at/len` 与重启前逐字段一致；`02-restart-history.png` |
| 房间列表预览与最后一条一致 | **满足** | `02b-roomlist.png`：`过往对话 / 1 个对话 · 0 条未读`，该房间预览 = `以下是对上述内容的进一步…`（即 assistant 那条的开头） |
| 不带进上次会话的"发送中/正在重连" | **满足** | 重开后输入框为空、`发送` 可用、状态栏 `本地服务已连接`、`0 条未读`、`last_read_message_id=2` |
| 新 `runtime.json` 重新出现，端口/令牌与旧的无关 | **满足** | 旧 `pid=29232 port=3674 tokenSha256[:8]=adfc55f9` → 新 `pid=14952 port=7399 tokenSha256[:8]=c51599bd`（**只记哈希前 8 位与长度 44，令牌值不入档**） |

**额外拿到的一条正向证据**（`PLAT-003` 的现场形态，清单没要求但正好判到）：新进程启动时对陈旧描述符发了告警并覆盖，原文逐字为——

> `warn: Pry.Api.Services.RuntimeDescriptorService[0]`
> 发现上一实例遗留的 runtime.json（进程 29232 已不存在），本次启动将覆盖它；遗留文件的端口与令牌自此失效，读取方必须重新发现

### 2.4 未手工验证的一项：优雅退出删除 `runtime.json`

三条路都试过，都不通，且**没有一条是产品缺陷**：

1. **窗口 X**：`PryMessengerWindow.axaml.cs:54-58` 与 `MainWindow.axaml.cs:121` 都是 `Closing → args.Cancel = true; Hide()`——隐藏到托盘，进程不死。
2. **托盘"退出…"**（`App.axaml.cs:170-195` → `PrepareForExitAsync` → `DisposeBackendAsync` → `desktop.Shutdown()`）：这是**唯一**优雅出口，但入口只有系统托盘菜单项。QA 不驱动系统托盘（Win11 通知区域折叠面板会抢焦点，且作者机器上此刻有前台程序在跑，见 3.3）。
3. **给独立 `Pry.Api` 发 Ctrl+C**：在隔离目录 `walk2` 实测——`Now listening on: http://127.0.0.1:5099` + `runtime.json` 写入（`pid=24928 port=5099 proto=1.0 tokenLen=44`）✅；但 `taskkill`（不带 `/F`，即关闭请求）**没有**触发 `ApplicationStopping`（日志停在 `Application started`，`runtime.json` 仍在）；`AttachConsole`+`GenerateConsoleCtrlEvent` 返回 True 而进程不退（本机 PowerShell 控制台的已知限制）。

**处置**：这一项记 **未手工验证**，不记 Fail，也不因此判 ② Fail——它由自动化侧的 `PLAT-005` 描述符生命周期用例负责，走查不为它新造设施（与清单对形状 E 的处置同一条理由）。**顺带产出两条 finding**（第 5 节 F-2/F-3）。

---

## 3. ③ 断一次后端不卡死 —— **Blocked**

清单 §3.1 已把 A′ 定成唯一可执行形状。本轮确认它**在 QA 手上不可达**，原因是两条实测事实：

### 3.1 F-1：描述符的**写侧**认配置，**读侧**不认

- 写侧：`RuntimeDescriptorService.ResolveDataDirectory(configuration)` 读 `Pry:DataDirectory` ⇒ 本轮 `walk/runtime.json`、`walk2/runtime.json` 都正确落在隔离目录。
- 读侧：`App.axaml.cs:47` 调的是 `RuntimeDiscovery.TryConnectAsync()`——**无参** ⇒ 走 `PryProtocol.DefaultDataDirectory`（`%LOCALAPPDATA%\PryCompanion`）。`RuntimeDiscovery.TryConnectAsync(string? dataDirectory = null)` 的参数在 UI 路径上没人传。

⇒ 结论：**要让 UI 发现一个独立后端，那个后端必须把描述符写进作者的 live 目录**。`Pry__DataDirectory` 帮不上忙，`%LOCALAPPDATA%` 也无法按进程改定向。

### 3.2 为什么 QA 不自己对着 live 目录跑 A′

独立 `Pry.Api` 一旦指向 `%LOCALAPPDATA%\PryCompanion`，它会打开作者那份 32 条消息的 `memory.db`（迁移、WAL）并启动 `MediaCleanupService`。这不是"读一下"，是**把别人的数据目录变成一次实验的现场**。按 AGENTS.md 第 1 条与我的职责边界，这属于需要作者点头的动作，不自行执行。

### 3.3 环境侧的第二个约束（同时解释了本轮前两次截图失败）

作者机器此刻主屏上有一个**前台运行的 `Cyberpunk2077.exe`（PID 17992，2560×1440）**持续抢焦点。因此：
- 屏幕区域截图取到的是游戏画面（`00-startup.png` 第一版 = 游戏内"正在进入新区域 市中心"）；
- QA **不使用** `SetForegroundWindow` + `SendKeys`：实测 `SetForegroundWindow` 后 1 秒焦点即被游戏收回（`restore.ps1` 逐秒采样：`t=1s fg=Pry → t=2s fg=游戏`），按键有落进游戏的风险。
- 改用 `PrintWindow(PW_RENDERFULLCONTENT)` 截图 + `PostMessage(WM_LBUTTONDOWN/UP, WM_CHAR, WM_KEYDOWN)` 注入，**全程不抢焦点、不碰游戏**，1.1 的时间线即由此得到。这条方法学写进第 6.3，供后续走查复用。

### 3.4 解阻需要作者做的（二选一，都很快）

- **A**：授权 QA 对 live 目录跑一次 A′（起独立 `Pry.Api` → 起 `Pry.App` → 确认接的是发现来的端口 → 结束 `Pry.Api`），并接受期间 `MediaCleanupService` 会碰他的库；或
- **B**：作者本人花 90 秒做同一件事，把两个秒表数（断开→首条可见结论 / 断开→终止态）和终止态那一屏的文案原文给 QA，QA 按 ③a≤40 s、③b≤55 s 判。

③a/③b/③c/③d 的阈值本轮**未被任何一步推翻**，也没有被拿去改产品参数。

---

## 4. ④ 可指认的提交 —— **待作者裁定**（QA 不自行提交）

- HEAD 仍是 `f1c5118`，`git log main..HEAD` 仍只有许可/素材两条 ⇒ **Phase 1 代码零提交**，第七轮如六轮。
- 提交面复核（与 Core 09-22 实数一致，无漂移）：`git status --porcelain` **183 = 51 改 + 132 未跟踪**；`git ls-files docs/eval` = **0**；`git ls-files scripts/measure-fixed-eval.ps1` = **0**；`docs/eval` 在 porcelain 里是**单条目录项**（整棵树未跟踪）。
- 本轮新增待入库面：`docs/eval/walkthroughs/eval-QA-P1WALK-0922/`（6 张 PNG，约 3.4 MB）。
- **门禁关系**：`QA-P6-01` 已结案，`REC-005` = Pass，故本条按"退出码 0"要求；本轮**没有重跑门禁**（走查不改代码，重跑属 ④ 提交前的动作，等提交决定一起做才有意义）。
- ⚠️ 一条必须随提交面一起决定的事：截图里含**二创美术素材**（角色背景图、头像、表情）。按 AGENTS.md 第 6/7 条与 `ASSETS_LICENSE.md`/`FAN_CONTENT_POLICY.md`，**这些 PNG 不能跟着 `docs/eval/**` 一起被"顺手打包"**。建议：要么提交 `docs/eval/**` 时排除 `walkthroughs/**/*.png`（改存文字化证据），要么逐张按素材政策过一遍。这个决定归作者，QA 不自选。

---

## 5. Findings 汇总（按 Owner 分派，QA 未改任何一条）

| ID | 严重度 | Owner | 事实 | 建议下一步 |
|---|---|---|---|---|
| `DEF-P1WALK-01` | High | Character Intelligence（提示词装配 / 输出解析） | 模型内部草稿（对系统提示词的 7 点分析 + `</thinking><RESPONSE>` 信封）被当 `model_reply` 落库并逐字渲染；同轮 `memories` 0 行 | 分辨根因 (a) 提示词被当成待总结文档 / (b) 缺信封剥离解包；补"内部段不得作为用户可见答复落库"的判据与用例 |
| F-1 | Medium | Client / PLAT-005 | `RuntimeDescriptorService` 写侧认 `Pry:DataDirectory`，`RuntimeDiscovery` 读侧（`App.axaml.cs:47` 无参调用）只认 `DefaultDataDirectory` ⇒ 配置数据目录的实例无法被 UI 发现 | 由作者裁定是否把发现目录纳入同一配置；裁定前 ③ 记 Blocked |
| F-2 | Low（可用性） | App / UI | 优雅退出唯一入口是托盘菜单项；窗口 X 只 `Hide()`，`RestartFrontendAsync` 不退出进程 ⇒ 无窗口内/可脚本化退出路径 | 是否加窗口内退出项属产品决定，QA 只登记 |
| F-3 | Low（运维语义） | Api / PLAT-003 | 独立 `Pry.Api` 收到 Windows 关闭请求（`taskkill` 无 `/F`）时不走 `ApplicationStopping`，描述符残留；下次启动的遗留告警文案正确 | 现状已被遗留检测兜住（2.2 有原文），记录即可；若要让关闭事件也走优雅路径，属新能力 |
| F-4 | 观察 | 作者裁定 | 两个本地模型都以 `reduced context 8192 after the requested context could not be allocated` 启动（16 GB 卡、15 GB 空闲） | 走查不判；若上下文档位是产品预期，值得单独看一眼 |

---

## 6. 本轮对清单自身的三处修订（测试侧缺陷，不改产品）

### 6.1 ① 的"各只出现一次"要分两层
DB 层与渲染层要分别看：本轮 DB 2 行、界面 2 气泡，都对。但 `DEF-P1WALK-01` 说明**"渲染了一次正确内容"和"渲染的是答复"是两件事**。

### 6.2 ① 的禁行结果补一条
新增：**"模型的内部草稿/信封被当作用户可见答复落库或渲染"**（`DEF-P1WALK-01` 的形状）。原稿只有"半截回复被当完整答复"，拦不住反向情况——这是我自己的 oracle 缺口，与 §3.4 第 1 条同一类。

### 6.3 ③/② 的 UI 注入方法学（写死，避免下次重新踩）
真机走查在**有前台全屏程序**的机器上必须用 `PrintWindow(PW_RENDERFULLCONTENT)` 截图 + `PostMessage` 注入；**禁止** `SetForegroundWindow`+`SendKeys`（焦点会被抢回，按键可能落进别的程序）。另外：本轮把 `runtime.json` 的令牌只记成"长度 + SHA256 前 8 位"，这条也补进清单的证据格式。

---

## 7. §6 运行记录（清单第 6 节，逐格已填）

| 项 | 值 |
|---|---|
| Run ID | `eval-QA-P1WALK-0922` |
| HEAD | `f1c5118` |
| 模型档位 | 本地 `llama-server`：`Qwen3-4B-RPG-Roleplay-V2-Q4_K_M.gguf`（主档 `discovered-023eafa58ef5e865`）+ `qwen2.5-vl-3b-instruct-q4_k_m.gguf`；RTX 5070 Ti / CUDA0 / 上下文降至 8192。**不记密钥** |
| ① | **Pass** ＋ `walkthroughs/eval-QA-P1WALK-0922/00-startup.png`、`01a-typed.png`、`01b-thinking.png`、`01-reply.png` ＋ sqlite3 对账 **2 行**（1 user + 1 assistant，同 `conversation_id`）＋ 端到端 36.9 s ＋ 新缺陷 `DEF-P1WALK-01` |
| ② | **Pass** ＋ `02-restart-history.png`、`02b-roomlist.png` ＋ 描述符两条输出：旧 `pid=29232 port=3674 token=adfc55f9…(44)` / 新 `pid=14952 port=7399 token=c51599bd…(44)` ＋ 遗留告警原文（2.2）＋ **2.4 一项未手工验证** |
| ③ | **Blocked**（注入法 A′ 不可达，F-1）＋ ③a/③b 秒表 **未取** ＋ 解阻需作者做 A 或 B（3.4） |
| ③d | 未做（依赖 ③ 的拓扑前置） |
| ④ | **待裁定** ＋ HEAD `f1c5118`（Phase 1 代码零提交）＋ 干净 worktree 复现**未做**（无可指认提交即无从复现）＋ 截图素材许可待作者定 |
| 结论 | **Phase 1 未结案**。①② 已证成作者裁定的那句话；剩 ③（Blocked，需一次授权或一次手工动作）与 ④（需一次提交决定）。**无 Fail 项**；`DEF-P1WALK-01` 是否升级为出口条件由作者裁定 |
