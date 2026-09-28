# Phase 1 最小闭环走查 · 第八轮（`DEF-P1WALK-02` 复判）

Run ID: `eval-QA-P1WALK-0923`　日期: 2026-09-23 19:21–19:4x 本地
前序: `0922`（第七轮 19:0x–20:0x，① Pass／② Pass／③ Blocked(F-1)／④ 待裁）→ 守门人 09-22 深夜～09-23 三次复验（③ 转"未通过"、④ 成立为 `3790df9`）→ **本轮**
候选: 分支 `codex/ui-main-window-redesign`，HEAD **`c94ce2c`**（Phase 1 可指认提交链 = `5361dab` + `3f69ce3` + `3790df9`；其后两格是纯 `docs:`）
工作区: **dirty，21 条（9 已修改 + 12 未跟踪）**。本轮被测的 `DEF-P1WALK-02` 那批 = `src/Pry.Client/LoopbackEndpointLiveness.cs`（新）、`src/Pry.Client/PryBackendClient.cs`（改）、`tests/Pry.Client.Tests/DeadLoopbackReconnectTests.cs`（新）+ `docs/api-v1.md` + `CHANGELOG.md` + `docs/engineering-pitfalls.md`；**同一棵树里还有他人 Phase 2 在途改动**（`src/Pry.Core/Runtime/IdleBackoffController.cs`、`MessageIntervalStats.cs`、`ReplyNecessity.cs`、`TurnGates.cs`、`ConversationRuntime.cs`、`RuntimePorts.cs`、`TurnScheduler.cs`、`tests/Pry.Core.Tests/RuntimeTrigger*.cs`、`docs/character-runtime.md`、`licenses/upstream/`，归 Character Runtime Owner 的 RT-002 触发层移植）。**本轮所有数字是"工作区"数字，不可归属到任何提交**——这正是 ④ 还在的原因。
环境: Windows 10.0.26200 x64 / .NET SDK 10.0.200 / `net10.0` / Release；跑门禁前 `Get-Process dotnet/testhost/Pry.*/llama-server` 逐个查过 `CommandLine`，只有我自己的 8 个 `MSBuild /nodemode:1` 复用节点，无同级构建或测试抢 CPU（③ 是计时判据，这一步不能省）。

**结论：① Pass（带 High 缺陷 `DEF-P1WALK-01`，第四次复现）／② Pass／③ 首次 Pass／④ 未成立 ⇒ Phase 1 仍未结案，但唯一未过的是"把这一批形成可指认提交后我从干净 worktree 复跑"，不再是产品行为。**

---

## 0. 预备

1. **模型档位**：本地 `llama-server`（`runtime/llama-server.exe`），文本档 `models/qwen3-1.7b-q4_k_m.gguf`（`-c 4096 -ngl 999 --reasoning on --device CUDA0`，端口 4024），另有一个视觉档 `models/qwen2.5-vl-3b-instruct-q4_k_m.gguf`（端口 4032）。**只记 Provider 名与模型名，不记密钥**（本机无远端密钥参与本轮）。
   与第七轮的档位不同（那轮是 `Qwen3-4B-RPG-Roleplay-V2-Q4_K_M`）⇒ ① 的耗时不可跨轮直接比较；③ 的计时路径不经过模型，可比。
2. **HEAD**：`c94ce2c`。
3. **数据目录**：新建隔离目录 `.local-notes/p8/walk5`，读写两侧都靠同一个环境变量 `Pry__DataDirectory` 跨进程共享（F-1 之后才成立，见清单 §3.5 本轮改写）。**作者 live 目录 `%LOCALAPPDATA%\PryCompanion` 全程零写入**，事后核对最新 mtime 仍是 09-22 19:09（上一轮他人授权运行留下的，不是本轮）。

## 1. ① 启动 → 发一句话 → 得到回复：**Pass**

拓扑＝**发现**（不是内嵌）：先起独立 `Pry.Api.exe`（pid 22604，`Pry__Url=http://127.0.0.1:5097/`），描述符 `pid=22604 port=5097 proto=1.0 schema=9 tokenLen=44 tokenSha8=87955AB2`；再起 `Pry.App.exe`（pid 65400），**App 自己拥有的 Listen 端口数 = 0** ⇒ 它接的是发现来的那条。杀后端前后描述符归属都没变（见 3.3）。

时间线（点击"发送"记 t=0，19:27:04；采样 1 s）：

| t | 事件 |
|---|---|
| 0.02 s | 用户气泡上屏 |
| 1.12 / 4.31 / 12.78 / 18.14 / 21.32 s | 逐段刷新（含模型加载与生成） |
| **22.38 s** | 答复完整可见，此后**到 200.64 s 再无任何变化** |

**必须观察到的三条**：状态栏从发送态回到"本地服务已连接"、没有常驻"正在重连"；答复内容证明它真读到了这句话（`收到，周三复诊记得提醒我～`，复述了"周三复诊"）；用户消息与 Agent 消息各一条。
**库侧对账**（只读 URI `mode=ro`，`messages` 全表）：`id=1 role=user len=20`、`id=2 role=assistant len=42`，同一 `conversation_id=5e08e724…`，**只有两行**。⇒ "Core 保存了"不是界面自己画的。
**禁行结果核对**：无无限转圈、无半截答复、发送态已落下。
**但 ① 的禁行清单里那一条命中了**，见 1.4。
证据：`shots/w0-window.png`（空态）、`w2-typed.png`、`r01_t0.png`…`r07_t22.png`、`db-dump.txt`。

### 1.4 `DEF-P1WALK-01` 第四次复现（High，归 Character Intelligence，仍按未修计）

落库并渲染的助手内容是**裸 JSON 信封**：

```
{"message": "收到，周三复诊记得提醒我～", "emoji": "📅"}
```

与第七～九轮的形状**同族但表面不同**：那几轮是 `</thinking><RESPONSE>{…}</RESPONSE>` 整段泄漏，本轮 `thinking`/`RESPONSE` 标签不见了、只剩没被解析的 JSON 体。**如实记下这个差异，不做归因**：本轮工作区含他人 Phase 2 在途触发层，形状变化可能来自它，也可能来自档位不同（本轮 1.7B + `--reasoning on`），不能算成"已修一半"。判据面不变——**角色答复的容器/草稿信封在持久化边界上没被剥掉就落库并逐字渲染**，抽取判据归 Character Intelligence，Client/UI/Core 不得自带形状识别（我给这条缺陷立的边界，见 `PROJECT_PLAN.md` §9 守门人那条）。

## 2. ② 硬杀再开，历史仍在：**Pass**

| 判据 | 结果 |
|---|---|
| 两条消息仍在、顺序正确 | ✅ `w3-restarted-history.png` + 重启后 `db-dump.txt` 仍只有那两行（**没有**把上一条 Agent 回复再投一次） |
| 房间预览＝最后一条 | ✅ 侧栏"过往对话 1 个对话 · 0 条未读"，预览 `{"message": "收到，周三复…` |
| 不带进上一次"发送中/正在重连" | ✅ 状态栏＝"本地服务已连接" |
| `runtime.json` 重建、端口/令牌与旧的无关 | ✅ 旧 `pid=22604 port=5097 sha8=87955AB2` → 新 `pid=66188 port=10854 sha8=49B08D2B`，`pidIsNewApp=True` |

**本轮新增的一条现场证据（补第七轮那个缺口的后果面）**：③ 用 `taskkill /F /T` 硬杀后端后，`walk5/runtime.json` **仍留在原地、指向已死的 pid 22604**；重启的 App **没有**连向它，而是回落到内嵌（App 自有 Listen 端口数 = 1）并重写描述符。⇒ 陈旧发现文件被 pid 存活判据拒掉，这条在真机上第一次量到（代码位置 `src/Pry.Client/RuntimeDiscovery.cs:41`：`descriptor.Pid == Environment.ProcessId || !IsProcessAlive(descriptor.Pid)`）。
**仍未手工验证**（沿用第七轮的如实口径，不翻 Fail）：**优雅退出会删除 `runtime.json`**。删除路径在代码里存在（`src/Pry.Api/Services/RuntimeDescriptorService.cs:108` 的 `File.Delete(DescriptorPath)`，类注释即"启动完成后写、优雅退出时删"），但真机只有托盘菜单"退出…"这一条入口能走到它，QA 无法可靠点击通知区域；`Closing` 被 `args.Cancel = true; Hide()` 拦掉、`taskkill` 不带 `/F` 只发 `WM_CLOSE`，两条都不是优雅退出。**注意这一项的严重性已被上面那条现场证据降级**：残留文件的可观测危害（连向死端口）已由 pid 存活判据挡住并有真机证据，剩下的只是磁盘卫生。
顺带一条 `PLT-012` 方向的旁证（**不据此结案该场景**）：硬杀 App 与 `/F /T` 杀后端之后 `Get-Process llama-server` 计数为 **0**，没有孤儿推理进程。

## 3. ③ 断一次后端：**首次 Pass**（注入法 A′）

**③a 首条可见结论 = 0.01 s**（判据 ≤ 40 s）：`taskkill /F /T /PID 22604` 后第一帧采样（`shots/k01_t0.png`）输入框上方已出现**「事件连接中断，正在重连…」**。9.21 s 那帧（`k02_t9.png`）底部状态行变成「状态同步暂不可用，稍后自动重试」——**底部状态行不是连接状态迁移**，不拿它当 ③b（这条判据是上一轮误读的直接教训，本轮前置执行）。

**③b 终止态 = 47.44 s**（判据 ≤ 55 s）：`k03_t47.png`，文案原文——

> **Pry 的本地服务没有在本机监听。重启 Pry 通常可以恢复；反复失败请展开技术详情查看编号。**

出自 `PryErrorPresenter.Describe` 的 `LocalCoreUnavailable` 一档：说清了什么断了、下一步做什么，不是裸堆栈。此后到 **150.58 s 结束再无第 4 帧** ⇒ 收住了，没有无限重连。

**与推导对照**：`DEF-P1WALK-02` 实施侧给的是 `45.5 + 1 × 2.05 ≈ 47.6 s`，实测 **47.44 s**（差 0.11 s）。含义不止是"进了档"——**八轮退避仍然数满**（45.5 s 的阶梯一秒没少），被省掉的只是 7 次明知会被拒的握手。这正是那条反向约束要的形状，我在真机上量到了它，而不是只看用例。
**对照上一轮**：`3790df9` 上 ③b ∈ (56.65, 60.94] s（含模型值 59.9 s）⇒ 本轮把 12–13 s 的差额收回到阈值内，**且没有动 `ConversationStreamOptions.Default` 的 8 轮计数与 500 ms→15 s 阶梯**（该文件不在改动清单里，`git status` 可复核）。
**拓扑确认（清单 3.3）**：App 自有 Listen 端口全程 0；描述符到断开结束仍是后端那份（`pid=22604`，"unchanged owner = True"）⇒ 杀的是宿主、UI 未动，A′ 成立。
**③c** NotApplicable（同进程内嵌无法安全制造，已由 `A_listener_that_never_answers_is_still_judged_by_the_request_budget` 判住，且本轮新增一例判它**不受探测影响**）。**③d** 不在走查面（杀进程不产生禁行码），载体仍是 `ConversationEventStreamRunnerTests.A_banned_failure_stops_before_the_first_backoff`。

## 4. ④ 可指认的提交：**未成立**

`DEF-P1WALK-02` 那批**只到主工作区**（实施侧记录自己写明"只到主工作区，未提交"，完成标准 ①⑤ 待"新的可指认提交 + QA 从该提交的干净 worktree 重跑"）。本轮我跑的是脏树，且同一棵树里混着他人 Phase 2 在途改动 ⇒ 本轮数字**不可归属到 `c94ce2c`，也不可当作 `DEF-P1WALK-02` 的提交基线复现**。
**为什么不自己提交**：`git status` 的 21 条跨两个 Owner（Core 的 3+3 片 vs RT-002 的 11 片 + `licenses/upstream/`），我不会把别人的在途成果打包进自己名下的提交；上一轮 F-1 能入库是因为 Core 在 §9 明确把提交权交回（"F-1 这批可直接取走"），本轮没有这句话。**这一步是作者的决策，不是任何 Agent 的编码。**
取走清单（若授权）：`src/Pry.Client/LoopbackEndpointLiveness.cs`、`src/Pry.Client/PryBackendClient.cs`、`tests/Pry.Client.Tests/DeadLoopbackReconnectTests.cs`、`docs/api-v1.md`、`CHANGELOG.md`、`docs/engineering-pitfalls.md`、`docs/phase1-platform-baseline.md`（后两份含他人行，需按 hunk 取）。**明确不含** `src/Pry.Core/Runtime/*`、`tests/Pry.Core.Tests/RuntimeTrigger*`、`docs/character-runtime.md`、`licenses/upstream/`、`docs/character-learning-safety.patch`。

## 5. 门禁与判据复核（我自己跑，不复用实施侧数字）

| 命令 | 结果 |
|---|---|
| `dotnet build Pry.slnx -c Release` | **0 警告 0 错误** |
| `dotnet test Pry.slnx -c Release --no-build --logger trx --blame-hang-timeout 120s` | 连续两次 **671 通过 / 0 失败 / 2 跳过（总 673：Client 92、App 182、Api 129、Core 268+2skip）**；结果目录 `TestResults/qa-p8-0923-a`、`-b` |
| `scripts/measure-fixed-eval.ps1 -ResultsDirectory <a 与 b>` | 两次**退出码 0**，`Catalog valid: 68 permanent scenarios.`、`Passed=671; Failed=0; Other=2.` |
| `scripts/verify-repository.ps1` | 通过 **383** 个受跟踪文件 |
| `git diff --check` | 无输出 |

跳过两条仍是 `LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key` 与 `CoreTests.Configured_sense_voice_model_can_run_offline`（显式环境门控，按约定不计 Pass，也**没有把任何红例转 skip**）。

**上一轮我交给"下一轮 QA 实读的三处"，本轮逐条回答**：
1. **探测是否只在"上次被拒的同一回环端口"上生效**——是。状态面是 `PryBackendClient` 里一个 `private (string Host, int Port)? refusedLoopbackEndpoint`，只在 `catch (HttpRequestException) when (IsConnectionRefused)` 里赋值（沿 `InnerException` 找真的 `SocketException(ConnectionRefused)`），拿到任何响应头即清空；命中条件是 `refusedLoopbackEndpoint == target` **且** `!IsListening`。用例 `The_first_subscription_is_a_real_handshake_and_only_later_ones_are_skipped` 用 `attempt > 1 == skipped` 把"第一次必须真打"钉成判据。⇒ **不是快速失败**。
2. **12 例里有没有一例跑真 Runner**——有：`Eight_attempts_still_count_eight_and_keep_the_default_ladder` 用真 `PryBackendClient` + 真 `ConversationEventStreamRunner`，断言 `opens == 8` 且退避序列逐格等于字面量阶梯。接线那一行不再留在零判据面（`QA-P5-01` 的旧形状没有复现）。
3. **半开与"监听者回来了"各有用例**——`A_listener_that_never_answers_is_still_judged_by_the_request_budget`（③c 形状：端口一直在监听表里，探测无从插手，判定仍归响应头预算）与 `A_port_that_starts_listening_again_gets_a_real_handshake`（真 socket 重新服务真事件帧，下一次照旧握手）。

**一条顺带的现场校准**：`The_first_subscription_is_a_real_handshake…` 这一例在 TRX 里耗时 **2.0468 s**——它自己就把"本机一次真被拒握手 ≈ 2.05 s"量了一遍。上一轮那条归因（`.local-notes/p7/deadport`）今天仍成立，且现在有一条受版本控制的用例在每次门禁里复量它。

## 6. Findings

| ID | 级别 | Owner | 状态 | 摘要 |
|---|---|---|---|---|
| `DEF-P1WALK-02` | High | Core Platform & Extensibility | **本轮复判结案（待提交面）** | ③b 从 (56.65, 60.94] s 降到 **47.44 s**，进 ≤55 s 档；8 轮计数与阶梯一字未动、非快速失败、地址仍是 `127.0.0.1` 三条反向约束逐条核过 |
| `DEF-P1WALK-01` | High | Character Intelligence | **仍开（第四次复现）** | 裸 JSON 信封落库并逐字渲染；形状与上三轮不同但**不归因**（工作区含 Phase 2 在途） |
| F-5（本轮新） | Low | 走查清单（QA 自持） | 已改 | 清单 §3.5 写着"A′ 在 QA 手上跑不了（F-1）"——F-1 入库后该节过期，本轮改写为"已可达 + 复现命令" |
| F-6（本轮新） | Info | EVAL-001 台账 | 登记 | ① 的耗时随档位变化（1.7B 22.4 s vs 4B 36.9 s）⇒ 走查记录必须写档位，否则跨轮比较会误判成性能回归 |
| `QA-P6-03` / `QA-P6-04` | — | 作者裁定 | 仍开 | 不因 ③ 转 Pass 而自动关闭 |
| `PLT-010` / `PLT-012`、OS 级一次跑完 | — | §5.0 冻结 / 环境门控 | 仍 Blocked/NotRun | 继续阻塞"PLAT-005 已完成"这个标记本身；本轮的"无 llama-server 孤儿"只是旁证 |

## 7. 运行记录（清单第 6 节的填写结果）

| Run ID | `eval-QA-P1WALK-0923` |
|---|---|
| HEAD | `c94ce2c`（工作区 dirty 21 条，含他人 Phase 2 在途 ⇒ 数字不可归属提交） |
| 模型档位 | 本地 llama-server，`qwen3-1.7b-q4_k_m.gguf`（CUDA0，`--reasoning on`）；不记密钥 |
| ① | **Pass** ＋ `shots/w0-window.png`、`w2-typed.png`、`r01_t0`…`r07_t22.png`、`db-dump.txt` ＋ sqlite3 侧 2 行（1 user + 1 assistant）＋ 答复可见 22.38 s ＋ **命中 ① 禁行清单一条：`DEF-P1WALK-01`** |
| ② | **Pass** ＋ `shots/w3-restarted.png`、`w3-restarted-history.png` ＋ 描述符旧 `pid=22604 port=5097 sha8=87955AB2` / 新 `pid=66188 port=10854 sha8=49B08D2B` ＋ 优雅退出删除一项仍 NotVerified（代码路径 `RuntimeDescriptorService.cs:108`，真机 tray-only；残留危害已被 pid 存活判据挡住的现场证据见 2 节） |
| ③ | **Pass** ＋ 注入法 **A′**（先起独立 `Pry.Api` → App 走发现 → `/F /T` 只杀后端）＋ ③a **0.01 s** ＋ b **47.44 s**（判据 ≤ 55 s）＋ 终止态文案原文见 3 节 ＋ 拓扑＝发现（App listeners 全程 0、描述符归属未变）＋ 稳定至 150.58 s 无第 4 帧 |
| ③d | 未做（杀进程不产生禁行码；判据在 `ConversationEventStreamRunnerTests`） |
| ④ | **未成立** ＋ 无新提交（`DEF-P1WALK-02` 只到工作区）＋ 取走清单见 4 节 |
| 结论 | **Phase 1 未结案**；未过项只有 ④（提交面），产品行为面无 Fail |

## 8. 权限边界

未 stage / commit / push；未改 `src/` 与 `tests/` 任何一字，未改任何产品参数；只写 `docs/eval/**` 与本计划；未推进 §5.0 指针；不改任何 Owner 的自评状态；只终止自己启动的进程（`Pry.Api` pid 22604、`Pry.App` pid 65400 与 66188 及其 `llama-server` 子进程，事后计数 0）；作者 live 目录只读核对、确认未被写入；令牌只记长度与 SHA256 前 8 位。
