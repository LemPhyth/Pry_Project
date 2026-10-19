# 客户端错误呈现与事件流重连（UI-027）

负责人：Windows Client。代码位置：`src/Pry.Client/PryErrorPresentation.cs`、`src/Pry.Client/ConversationEventStreamRunner.cs`、`src/Pry.App/Services/NoticeContentFactory.cs`；第 6 节那半条自愈链在 `src/Pry.Client/PryBackendClient.cs` 与 `src/Pry.Client/RuntimeDiscovery.cs`。

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

- **禁令优先于分类**：`RetryForbidden`（服务端明确写了 `retryable:false`）先判，任何一档都不许留下"稍后重试"的口风。`null` 与 `true` 都不是禁令，旧服务端沉默不等于永久失败。这张表一字未因第 6 节改变：A′ 拓扑下"后端换了所以令牌旧了"那一档在请求发出去之前就被客户端消化掉了，走到这里的 401 仍然是真拒绝。
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

## 6. 换后端之后：活客户端自己接回来（`DEF-P1WALK-05`，`0-6`）

A′ 拓扑下后端是独立进程：它的会话令牌每个进程随机生成（`BackendSessionToken` = 32 字节 base64），重启后写在 `runtime.json` 里的那一枚必然和旧客户端头上挂的不一样。这一档服务端回 401 `urn:pry:error:unauthorized` 且 `retryable:false`，按第 2 节它是死档——但**"死"的应该是那枚陈旧令牌，不是这个客户端**。

| 形状 | 行为 |
|---|---|
| 401 + 本机端口仍有人监听 + 文件里的令牌确实变了 | 重读一次 `runtime.json`，用新入口**整体换掉** `HttpClient`，重发那一笔请求，并宣告 `SessionRestored` |
| 401 + 端口没人监听 | 不重读——读了也接不上。那一档仍由"连接被拒"与退避阶梯说话（第 5 节） |
| 401 + 重读出来令牌没变（或读不出可用入口） | 真拒绝：禁令、文案、零退避一概照旧（`PLT-002`／`PLT-006`） |
| 200 | 一个字节都不多看，不发第二次请求、不读文件 |

三个机关：

1. **会话挂在 `HttpClient` 实例上**，不是让调用方自己传来源。装配点只有一个（`RuntimeDiscovery.CreateClient`，首次连接、重读、测试都走它）；界面或任何装配点一旦需要"记得"传一个目录，漏传的那一半就静默失效——这是 `F-1` 那三条源码门钉的同一个缺陷形状。
2. **换的是整个客户端，不是就地改请求头**。`BaseAddress` 与 `DefaultRequestHeaders` 都没有同步保护，和在途请求并发改它们会把一次换后端演成随机错乱；旧实例不就地 `Dispose`，此刻还有请求挂在它上面，掐成"取消"同样是假事故。
3. **判据比的是"这一笔真发出去的那枚令牌"**，不是"文件里现在那枚"。并发时两笔都带着旧令牌出门，第一笔换好入口之后第二笔才回来判断；若写成"跟我现在挂的一样吗"，第二笔会把自己发早了的那声 401 读成真拒绝——假禁令。重读在 `sessionRecovery` 信号量里做，所以一次换入口只换一次、只宣告一次恢复。

事件面：`ReadEventsAsync` 也走同一层自愈，所以空窗期里的重连由 Runner 自己的阶梯接住。但 Runner 一旦按 8 轮走到终止就返回了，此后没有任何一条路会再订阅——所以界面必须挂 `SessionRestored` 重新起流。通知来自工作线程，房间与修订号只有在 UI 线程上读才是事实，而且只能走 `StartEventStream` 那一个入口（它负责 `Cancel` + `Dispose` 掉旧 CTS）。命令面自己会接回来、事件面靠界面重订阅，这两半缺任何一半都不算"还能不能说上话"。

内嵌拓扑（`EmbeddedBackend` 自己写 `X-Pry-Token`，没有发现过程）没有可重读的入口：`attemptedToken` 为 null，`RecoverOnceAsync` 一行都不多走。

**反向约束（本刀刻意没做的事）**：没给会话令牌加过期时间或轮转协议（那是 M0-d 的契约面，要动就走协议版本）；没让 `Pry.App` 解析 `runtime.json`；没用定时轮询描述符替代按需重读；没有放宽 `SessionTokenMiddleware` 的默认拒绝；没有把 `unauthorized` 整体改成可重试。

## 7. 界面位点覆盖

**已接入（20 处，活路径全部）**：`PryMessengerWindow.axaml.cs` 17（初始化、发送、编辑/重生成、删除、创建/打开对话、读取角色、停止回复、撤销、附件策略与上传、录音、语音识别、表情）、`PryMessengerWindow.Companion.cs` 2（动作读取、互动失败状态行）、`App.axaml.cs` 启动失败对话框 1。两条事件流的终止出口另由 `MessengerEventStreamController` 取同一张表。

**未接入，且各有各的门禁原因**：

| 位置 | 处数 | 为什么不在本轮 |
|---|---|---|
| `MainWindow.axaml.cs` | 10 | 第 0 层 `0-5` 反向约束："不许顺手碰 `MainWindow` 家族"，删除次序另裁 |
| Character / Sticker / Settings / Memory 四窗口 | 16 | 属 UI-021/022/COMP-006 名下文件，按 §5.0 是冻结面 |
| Proactive / ModelLibrary / InteractionProfile 三面板 | 6 | 同上 |

`tests/Pry.App.Tests/UserFacingNoticeBoundaryTests.cs` 的门是**显式清单**：先改干净、再登记，不把未覆盖的文件混进来凑绿灯。

## 8. 判据

| 判据 | 守什么 |
|---|---|
| `PryErrorPresentationTests`（Client） | 这张表本身该说什么、哪一档可重试 |
| `ConversationEventStreamRunnerTests`（Client） | Runner 的退避、续传、终止、消费端抛异常不重投 |
| `StaleSessionRecoveryTests`（Client，8 例） | 第 6 节四行形状逐条钉住：陈旧令牌能恢复、令牌没变仍是死禁令、端口空了不重读、200 不去找新入口；外加并发不误判、内嵌路径一字未动、事件面在放弃前重拨 |
| `BackendSwapRecoveryProcessTests`（Api，真进程） | 判据③：起后端 → 连接 → 硬杀 → 同端口换令牌重起 → **同一个客户端实例**请求 200 且 `POST /turns` 得到答复；正向探针＝重起后描述符里的令牌指纹确实变了（恢复取"打得通"，不取"读到了新文件"） |
| `SessionRestoredSurfaceTests`（App，源码门） | 全仓只有一个接线位点、必须回 UI 线程、重订阅只能走 `StartEventStream` 那一个入口、`MainWindow` 与旧轮次管理器不得自己挂 |
| `MessengerEventStreamControllerTests`（App） | 接线：四个回调听没听见、切房门、`conversation.read`、`turn.state` |
| `NoticeContentFactoryTests`（App） | 界面渲染出来的那一行是不是表里的句子、技术详情是否默认收起且带 `traceId`、禁令过界面是否还是死档 |
| `UserFacingNoticeBoundaryTests`（App） | 源码门：已接入文件不得再出现 `.Message`；带正向探针，扫描器自己看不见东西时不会静默常绿 |
| `SingleBanJudgeBoundaryTests`（Api） | 禁令判据不得在界面层各写一份 |

2026-10-14 本刀的突变反证（每次 `-t:Rebuild`，逐个点名转红后字节还原）：单个位点改回 `ex.Message`、保留签名把重载内部掏成异常原文、状态栏那一行改回原文、技术详情默认展开、启动窗口标题不再查表。

2026-10-19 本刀（`0-6`）的突变反证（同样每次 `-t:Rebuild`，逐个点名转红后字节还原）：撤掉自愈（Client 4 红 + 真进程 1 红）、把"令牌没变"也当成恢复＝`unauthorized` 整体放宽（1 红）、忽略监听表（1 红）、把按需改成每笔都读（1 红）、把"发早了的 401"读成真拒绝＝假禁令（1 红）、不宣告恢复（Client 4 红 + 真进程 1 红）、撤掉界面接线（3 红）、重订阅绕开那一个入口（1 红）、通知不回 UI 线程（1 红）。其中"每笔都读"第一版全绿——它暴露的不是产品码而是判据缺了"按需"的另一半，补上 `A_healthy_request_does_not_go_looking_for_a_new_entry` 后才压出红。

## 9. 已知边界

- 事件游标每会话只在内存，重启或重配置归零 ⇒ 补取靠整段历史兜住，真正的持久游标属 PLAT-005 的 M0-c。
- `RemoteConversationTurnManager` 刻意不挂 `Resumed`（追加渲染会重复画同一条，去重另记待办）。
- `SessionRestored` 只接在 messenger 窗口那一处；`MainWindow` 家族与 `RemoteConversationTurnManager` 都不自己重订阅。换后端时从那些入口起的流不会自愈——第 0 层 `0-5` 的反向约束（"不许顺手碰 `MainWindow` 家族"）还在，删除次序另裁。
- `ConversationEventStreamRunner.cs` 类注释自称"全仓库唯一读 `RetryForbidden` 的地方"，实际呈现表也读它（用于 `CanRetry`）。判据来源仍然只有一个属性，但这句话现在偏窄，改注释前要先确认 `SingleBanJudgeBoundaryTests` 的扫描范围。
- 启动失败对话框去掉了原来那句"请检查本地数据目录和程序资源是否完整"——它对协议不匹配、令牌失效这两档是错的猜测；处境由表里的档位给出，具体原因进技术详情。
