# 客户端错误呈现与事件流重连（UI-027）

负责人：Windows Client。代码位置：`src/Pry.Client/PryErrorPresentation.cs`、`src/Pry.Client/ConversationEventStreamRunner.cs`、`src/Pry.App/Services/NoticeContentFactory.cs`。

这份文档只写**今天跑着的这条链**。历史上记在 worktree 那份交接里的"半开连接察觉不到"已经闭掉，见第 4 节。

## 1. 三层的分工

```text
PryErrorPresenter      决定"说什么、还能不能再试"——全仓唯一出口
  └─ NoticeContentFactory  把那句话放进界面位点，技术详情默认收起、可复制
       └─ 窗口 / 对话框      只提供"哪件事没做成"的标题，不转述异常
ConversationEventStreamRunner  唯一的重连宿主：退避、续传、补取、终止
```

界面不得自己判错误形状。`ShowNoticeAsync(string title, Exception error)` 是失败位点的唯一入口；标题说处境，正文取 `PryUserError.Body`，HTTP 状态 / `code` / 服务端原文 / `traceId` 只进 `TechnicalDetail`。

## 2. 分类表

`PryBackendException`（稳定码 + HTTP 状态）与传输异常（`HttpRequestException` 及其 `SocketException` 内链）各自成档：

- **禁令优先于分类**：`RetryForbidden`（服务端明确写了 `retryable:false`）先判，任何一档都不许留下"稍后重试"的口风。`null` 与 `true` 都不是禁令，旧服务端沉默不等于永久失败。
- **Provider 七类逐档**（`ProviderErrorCodes`，住在 `Pry.Contracts`，Client 不得依赖 Core）：Auth / RateLimited / Timeout / InvalidResponse / ModelUnavailable / ContextOverflow / Cancelled。合并成一句"稍后重试"就是把"改密钥"说成"等一会儿"。
- **回环被拒**单独一档：内嵌后端与界面同进程，端口没监听或后端提前退出都表现为连接被拒 ⇒ "本地核心没有启动成功"。
- **漏档会掉进 `Unknown`**，兜底文案是泛化的。新增服务端错误码必须同轮扩这张表（`urn:pry:error:*`、`turn.failed.code` 与它是同一套契约）。

## 3. 重连规则

`ConversationEventStreamOptions.Default` = 首次 500 ms、指数翻倍、上限 15 s、连续 8 次失败后终止。终止与连接状态都通过回调交给界面，界面不再自己写循环。

| 事件 | 行为 |
|---|---|
| 连续输入 / 服务端静默 | 不算失败，不退避 |
| 连接异常断开 | 按 `ConversationEvent.Sequence` 以 `?after=` 续订，不整段重放 |
| 重连成功 | `Resumed` 回调整段补取历史；`MessengerEventStreamController` 先判切房，迟到的补取不许盖掉新房间 |
| 消费端（回调）抛异常 | 按断线处理。游标已先于投递推进，因此**不重投**同一条事件，缺失内容由整段历史补取兜住 |
| 服务端写明不可重试 | 在计数与退避**之前**判出，直接终止并给死档文案 |
| 流在收到任何事件前结束 | 计一次失败并退避，不静默退出 |

无 jitter：这是回环单连接、单进程的本地场景，多台机器同时惊群的假设不成立（PLAT-005 若引入外部通道需重新评估）。

## 4. 半开连接（已由 PLAT-005 M0-b 闭掉）

服务端 `PryProtocol.HeartbeatIntervalMs` = 15 s，`SseEventStream` 下发 `:ping` 注释帧；客户端 `PryBackendClient.ReadEventsAsync` 改为逐行存活窗口，超过 `LivenessTimeoutMs`（= 15 s × 2 + 2 s）无任何字节即抛 `TimeoutException`，Runner 据此判为断线并重连。半开不再需要等 TCP 自身超时。

订阅起头与流中静默同尺度：Kestrel 要等第一帧刷出才发响应头，空闲会话在心跳周期内没有可读的响应头。

## 5. 死端口不浪费一次握手

`Pry.Client.LoopbackEndpointLiveness`（`DEF-P1WALK-02`）：上次真被拒 + 字面量回环地址 + 本机监听表里没人监听 ⇒ 订阅前直接报**同形**失败，省掉一次必然被拒的握手。终止态走查的环境成本从 7 次降到 1 次，Runner 的 8 轮与阶梯一字未动。

## 6. 界面位点覆盖

**已接入（20 处，活路径全部）**：`PryMessengerWindow.axaml.cs` 17（初始化、发送、编辑/重生成、删除、创建/打开对话、读取角色、停止回复、撤销、附件策略与上传、录音、语音识别、表情）、`PryMessengerWindow.Companion.cs` 2（动作读取、互动失败状态行）、`App.axaml.cs` 启动失败对话框 1。两条事件流的终止出口另由 `MessengerEventStreamController` 取同一张表。

**未接入，且各有各的门禁原因**：

| 位置 | 处数 | 为什么不在本轮 |
|---|---|---|
| `MainWindow.axaml.cs` | 10 | 第 0 层 `0-5` 反向约束："不许顺手碰 `MainWindow` 家族"，删除次序另裁 |
| Character / Sticker / Settings / Memory 四窗口 | 16 | 属 UI-021/022/COMP-006 名下文件，按 §5.0 是冻结面 |
| Proactive / ModelLibrary / InteractionProfile 三面板 | 6 | 同上 |

`tests/Pry.App.Tests/UserFacingNoticeBoundaryTests.cs` 的门是**显式清单**：先改干净、再登记，不把未覆盖的文件混进来凑绿灯。

## 7. 判据

| 判据 | 守什么 |
|---|---|
| `PryErrorPresentationTests`（Client） | 这张表本身该说什么、哪一档可重试 |
| `ConversationEventStreamRunnerTests`（Client） | Runner 的退避、续传、终止、消费端抛异常不重投 |
| `MessengerEventStreamControllerTests`（App） | 接线：四个回调听没听见、切房门、`conversation.read`、`turn.state` |
| `NoticeContentFactoryTests`（App） | 界面渲染出来的那一行是不是表里的句子、技术详情是否默认收起且带 `traceId`、禁令过界面是否还是死档 |
| `UserFacingNoticeBoundaryTests`（App） | 源码门：已接入文件不得再出现 `.Message`；带正向探针，扫描器自己看不见东西时不会静默常绿 |
| `SingleBanJudgeBoundaryTests`（Api） | 禁令判据不得在界面层各写一份 |

2026-10-14 本刀的突变反证（每次 `-t:Rebuild`，逐个点名转红后字节还原）：单个位点改回 `ex.Message`、保留签名把重载内部掏成异常原文、状态栏那一行改回原文、技术详情默认展开、启动窗口标题不再查表。

## 8. 已知边界

- 事件游标每会话只在内存，重启或重配置归零 ⇒ 补取靠整段历史兜住，真正的持久游标属 PLAT-005 的 M0-c。
- `RemoteConversationTurnManager` 刻意不挂 `Resumed`（追加渲染会重复画同一条，去重另记待办）。
- `ConversationEventStreamRunner.cs` 类注释自称"全仓库唯一读 `RetryForbidden` 的地方"，实际呈现表也读它（用于 `CanRetry`）。判据来源仍然只有一个属性，但这句话现在偏窄，改注释前要先确认 `SingleBanJudgeBoundaryTests` 的扫描范围。
- 启动失败对话框去掉了原来那句"请检查本地数据目录和程序资源是否完整"——它对协议不匹配、令牌失效这两档是错的猜测；处境由表里的档位给出，具体原因进技术详情。
