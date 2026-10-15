# 关窗、收窗与真实退出（第 0 层 `0-5`）

权威裁定：作者 2026-10-12 裁 **A 托盘常驻**（记在 `docs/full-scope-sprint.md`）。本文只写"这条裁定在代码里长成什么样、用什么判据守住、真机怎么验"，不新增任何验收判据本体。

## 1. 三条关闭路径，只有最后一条是退出

| 路径 | 触发方式 | 必须发生 | 绝不能发生 |
|---|---|---|---|
| A 标题栏 × / Alt+F4 / 外部 `WM_CLOSE` | 用户点 ×，或别的进程给主窗口发 `WM_CLOSE` | 窗口收进托盘；进程活着；内嵌后端继续监听；`runtime.json` 继续对外发布；15 秒心跳与 SSE 事件流照旧 | 停宿主、退进程、删发现文件 |
| B 对已收起的窗口再发一次 `WM_CLOSE` | 外部进程重复请求关闭 | 仍然只是收窗 | 第二次请求被当成"用户其实想退出" |
| C 托盘 →「退出…」→ 确认框点「退出」 | **只能由人点**（原生托盘菜单没有可脚本触达的入口） | 窗口收尾 → 停内嵌宿主 → 收托盘图标 → 结束生命周期，按这个次序；优雅退出把 `runtime.json` 删掉 | 留一个没人认领的后端进程（孤儿），或图标还在但进程已经没了 |

收窗期间主动消息仍要工作，所以 A、B 两条路径不许注销事件流、不许停心跳——这是"收窗 ≠ 退出"真正用户可见的那一面。

## 2. 两块判据面

### 2.1 单元判据（`tests/Pry.App.Tests/`）

- `WindowLifecycleTests.cs` — 决策与次序本体。`src/Pry.App/Services/WindowLifecycle.cs` 是唯一的裁定出口：`WindowClosePolicy.ShouldHideToTray`（默认拦下关闭请求改成收窗，只有退出序列自己置起的 `exitApproved` 才算点头）、`TrayExit.IsConfirmed`（取消与"没选"都不算点头）、`TrayExit.RunAsync`（四步的次序与"逐步等待"，失败就停在失败处）。
- `TrayResidencyBoundaryTests.cs` — 源码门，扫的是三个"没人守着就会静默变坏"的形状：`Closing` 段不许出现任何只有退出才该做的事；两处托盘退出接线必须按 `PrepareForExitAsync → DisposeBackendAsync → 收托盘 → Shutdown` 的次序；`ShutdownMode.OnExplicitShutdown`、托盘图标 `IsVisible = true`、「退出…」菜单项这三件驻留前提不许掉；心跳与事件流那五条路径不许把窗口可见性写成"要不要跑"的门槛（唯一的白名单是 `MarkVisibleReadAsync` 的已读上报）。

这两块的分工是有意的：次序不变量由行为判据锁（coordinator 内部换序会红），接线形状由源码门锁（调用点少写一步会红）。抽 coordinator 不是为了好看——那四步原本是连着写的四行，删掉任何一步都不会让任何一条判据变红。

### 2.2 真机仪器（`scripts/verify-window-close-paths.ps1`）

单元判据证明形状，真机仪器证明"运行中的进程确实照这条裁定做"：它只看三个观测量——进程活着吗、回环端口还答 `GET /health` 吗、`runtime.json` 还在吗。

```powershell
# 必须给隔离数据目录；直播目录（%LOCALAPPDATA%\PryCompanion）及其任何子目录一律拒绝
powershell -ExecutionPolicy Bypass -File scripts\verify-window-close-paths.ps1 `
  -DataDirectory <repo>\.local-notes\window-close-paths\walk1
```

- A、B 两条路径全自动（`PostMessage WM_CLOSE`，按 pid 精确投递到本进程自己的顶层窗口，不碰前台别的程序）。A 只在"当场恰好只有一个可见顶层窗口"时才发第一次关闭；多于一个（例如开着桌宠）就记 `BLOCKED`，因为按下标猜一个可能关掉的正是裁定之外的那扇窗。B 发的是 A 刚收起的那一扇，不是"任何一个不可见窗口"。
- C 的**触发是人工的**（托盘菜单无法脚本触达），**判定全自动**。没人点就记 `BLOCKED`，不记 `Fail`，也不许为了绿灯把 `taskkill /F` 拿来冒充用户点头——那不是优雅退出，只会留下一份失效的发现文件。
- 拓扑自适应：`runtime.json` 里的 `pid` 等于本次界面进程＝内嵌；等于另一个活着的进程＝A′（独立后端）。A′ 下 C 的判据反过来——退出这一扇窗口不该、也不会删掉别人的发现文件，那个后端必须继续活着。
- 令牌只记长度与 SHA-256 前 8 位，值不入档。
- 退出码：`0` 全部 Pass，`1` 有 Fail，`2` 有 Blocked 或前置不成立。

**2026-10-15 首轮真机读数**（载体＝本刀，内嵌拓扑，隔离数据目录，端口 2151）：

| 判据 | 读数 |
|---|---|
| `preflight.backend_answers` | PASS |
| `A.window_hidden` / `A.process_alive` / `A.backend_still_serving` / `A.descriptor_still_published` | 四条 PASS：一次 `WM_CLOSE` 之后窗口没了、进程仍活、`/health` 仍答、`runtime.json` 仍在 |
| `B.second_close_still_hides` | PASS：同一扇窗口第二次被关仍然只是收窗 |
| `C.*` | 三条 BLOCKED——缺的那一步只能由人点托盘「退出…」，本机这一轮没有人点 |
| `Z.live_data_root_untouched` | PASS：作者的 live 目录最新写入时间跑前跑后同一值（`2026-09-22T11:09:18Z`） |

这一轮同时暴露了仪器自己的一个缺陷：发现文件是内嵌宿主写的，比 Avalonia 画完第一帧早得多，第一次跑（walk1）在窗口出现之前就取到了"可见顶层窗口 0 个"，于是 A 整段 BLOCKED。修法是等窗口，不是放宽判据——新增 `-WindowBudgetSeconds`（默认 30 秒），等到"恰好一个可见顶层窗口"才发第一次 `WM_CLOSE`；等不到仍然记 BLOCKED。walk1 那份读数保留在 `.local-notes/window-close-paths/walk1-evidence/`，作为"仪器会在界面没起来时拒绝判"的证据，不当作产品结论。

## 3. 这一刀的边界

- 行为逐字不变：抽 seam 的两处调用点与 `Closing` 判定都是等价改写，没有重排、没有加兜底、没有把任何一步改成"尽力而为"。判据锁的就是现状。
- `MainWindow` 家族（旧版双布局主窗口）不许顺手碰，受 `0-5` 反向约束；它的 `Closing` 仍然是内联的 `if (!_allowClose)`，源码门只对它做只读核对（文件在就判，不在就跳过）。
- 优雅退出会删除 `runtime.json`（`RuntimeDescriptorService.Clear`），但 Api 层目前没有一条判据钉住这件事——那属平台线（PLAT-005）名下文件，本刀不代写，登记为待办；在此之前，这条形状只有真机 C 判据能证。
- 主动消息在收窗期间是否真的投递成功，本仪器只证到"宿主仍可达、事件流与心跳仍在看不到窗口的地方跑"；主动消息本体要出判据需要已配置的模型与房间，归第 5 层走查与 Eval。
