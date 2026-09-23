# Pry 更新记录

## Unreleased

### Added

- 死端口上的重连从此只付一次握手的钱（**DEF-P1WALK-02**，UI-027 / 走查 ③b）：③b 超档的那十几秒不在退避阶梯里，而在操作系统——这台机器上一次被拒的回环 `connect` 要 2.046–2.055 秒（RST 已经回来、`connect` 完成压在 TCP 层，裸 `Socket.ConnectAsync` 复现，同机连正在监听的端口 0.001 秒），八轮重连就是十几秒纯等待，而"这个端口有没有监听者"本机监听表 0–2 毫秒就能答。于是 `PryBackendClient.OpenEventStreamAsync` 在**上一次订阅因连接被拒失败**且目标是**字面量回环地址**时先查一次 `IPGlobalProperties.GetActiveTcpListeners`（新 `Pry.Client.LoopbackEndpointLiveness`）：没人监听就不再打出这次必然被拒的握手，报的是**同一种失败**（`HttpRequestException` 内层一个真的 `SocketException(ConnectionRefused)`），所以 `PryErrorPresenter` 仍落在"本地核心没有启动成功"那一档。三条反向约束逐条落地：轮数与阶梯一字未动（连续 8 次、500 毫秒起倍增 15 秒封顶仍是 `PLT-002`/`PLT-006` 的判据本体，省的是握手不是轮数）；连接被拒**没有**变成快速失败（每个客户端第一次订阅永远走真连接，答不了的一律答"有人监听"——主机名要先解析、非回环可能是远端、通配地址命中、查询本身失败都走这一档，后端重启在同一端口上时监听表立刻重新出现该端口、下一次照旧真连）；界面与发现路径的地址仍写 `127.0.0.1`（`localhost` 双栈会让一次被拒的连接付两份钱，本机实测 4.10 秒）。半开那条判据不受影响：有人接、永不作答的端口一直待在监听表里，判定仍归响应头预算与存活看门狗。判据 12 例 `DeadLoopbackReconnectTests`（真 socket，不打桩）：第一次真打、后面几次不再打的来历判据、八轮仍八轮且阶梯原样、端口复活后收到真事件、静默监听仍按预算报超时、`IsListening` 的六种回答面（回环精确地址、通配、死端口、主机名、非回环、端口越界）；耗时那条按"本机第一次真握手的成本"现算上界而不是写死秒数，因此在握手瞬时的机器上不会假红。证伪四个方向各点一条：抽掉探测 ⇒ 第二次尝试的来历判错（整类 6 秒→22 秒）、把探测改成"记过就拒" ⇒ 端口复活那条转红、连第一次也省 ⇒ 来历判据在 `attempt == 1` 转红、内层错误码换掉 ⇒ 界面档位转红。已落地主工作区，未提交；③b 的 ≤55 秒由 QA 从新提交复跑走查判定。

- 配了数据目录的独立后端从此**真的能被界面发现**（F-1，PLAT-005 M0-d 读侧，走查 ③ 的唯一阻塞项）：描述符的写侧一直认 `Pry:DataDirectory`，读侧却是 `App.axaml.cs` 里的一处 `RuntimeDiscovery.TryConnectAsync()` **无参**调用，于是永远只认 `PryProtocol.DefaultDataDirectory`。分叉的代价不是报错而是静默退化——界面看不见那个已在跑的后端，转而起第二个内嵌宿主去争同一份数据目录，而"断一次后端"这类注入也就无法在隔离目录里做。修法是把目录传进那一处调用并让读写共用**同一个解析点**：`BackendApplication.CreateHostBuilder()` 建一次宿主配置根，发现与内嵌都用它（`BuildAsync` 增一个接收 builder 的重载，旧的 `args` 重载转为薄委托），界面侧收敛成新入口 `Pry.App/Services/BackendLocator`，它对 `Pry.Client` 不暴露可空目录参数——不传目录就编译不过。跨进程唯一的共享通道是环境变量 `Pry__DataDirectory`（`--Pry:DataDirectory` 只抵达被启动的那一个进程），文档自此写明这一点。判据 5 例：`BackendDiscoveryDirectoryTests` 真起一个只带环境变量、不带目录参数的 `dotnet exec Pry.Api.dll` 子进程，用界面那个入口解析出的目录读到它的 `runtime.json` 并带着文件里的端口与令牌连上（证伪：解析点改成自认默认值 ⇒ 该例在 7 毫秒处转红，不用等子进程）；`DiscoveryDataDirectoryBoundaryTests` 三条源码门分别钉"漏传实参""界面另建发现入口 / 自己读键名或默认值""客户端层复制目录推断"（证伪：把 `App.axaml.cs` 换成 HEAD 那份无参写法 ⇒ 两条门同时点到 `App.axaml.cs:47`；在 `Pry.Client` 里放一处 `configuration["Pry:DataDirectory"]` ⇒ 第三条转红）。

- 语音识别那 4 分钟预算此前**从未真的生效**（QA-P2-04③ 的根因，PLAT-001）：`PryBackendClient` 把"普通请求 32 秒"与"转写 4 分钟"两档都写在同一个客户端级 `HttpClient.Timeout` 上，而一个客户端只能有一个数——装配点取的是 32 秒，于是一次长识别在 32 秒就被顶掉，报给用户的等待时间还是错的。现在做主的是**逐请求声明**：每个调用在自己的 linked CTS 上声明预算，`HttpClient.Timeout` 退居兜底并取 `RequestBudgetCeilingMs`（最宽一档加 15 秒调度余量），两个真装配点（内嵌宿主、`RuntimeDiscovery`）各有一条断言钉住"兜底必须宽于做主的那一档"。到点仍报 `TimeoutException` 而不是"用户自己关了请求"：预算到期与调用方取消在同一个出口靠 `token.IsCancellationRequested` 分家。那条到期路径此前零用例（CI 上等不满 4 分钟），现在预算可注入、150 毫秒即判到点，事件流"响应头一直不到达"另有一例。
- 活窗口那条事件流循环从此有判据（QA-P5-01，UI-027）：`Runner` 的四个回调怎么落到界面——`Resumed` 补取、`Faulted` 用呈现表覆盖状态栏、切房后整条丢弃——此前删掉任何一行门禁仍全绿。接线抽成 `src/Pry.App/Services/MessengerEventStreamController.cs`（本仓库对"窗口代码后置里判不掉的逻辑"的既有做法，因为仓库没有 headless Avalonia 设施），窗口侧只剩 `Dispatcher.UIThread` 投递。判据 13 例，其中两例跑真 `RunAsync` 而不是手调 handler：handler 级只会说"字段为空"，Runner 级说的是"界面没被告知"。证伪跑为逐个突变（删四个回调任一行、去掉三处切房门、不认 `conversation.read`、不渲染 `turn.state`），每一次都有对应用例转红。顺带收紧一处行为：连接状态变更如今也受切房门约束，旧流的"正在重连"不再盖掉新房间的状态栏。`EventStreamReconnectBanTests` 那 5 例保留但定位改标——它们驱动的 `RemoteConversationTurnManager` 只有 `MainWindow` 一个构造点，而 `MainWindow` 在全仓无任何构造点，属"同一判据的第二形状"而非活路径证据；`MainWindow` 与 `ThemeAppearanceEditor` 的删除次序在迁移文档 §8 第 2 条给出实施侧意见（作者决定删与不删）。
- `PLT-*` 场景编号在文档里只留一个事实源（QA-P5-02，PLAT-005 台账）：`docs/protocol-versioning-and-websocket-migration.md` §8 尾表用的还是 QA 采纳前的编号，同一个 `PLT-006` 在两份文档里指两件负责人都不同的事，本轮 QA 照它读差点把一项已落地的能力再压一轮。该表改为永久目录（`docs/eval/scenarios.json`，`suiteVersion 1.1.1`，12 条）的**派生视图**：每行补目录 `scenario` 文本与 `severity`，对齐约定写成"以 `scenario` 文本对齐、不以 ID 对齐"，并撤掉同表此前传播的"Runner 仍在 worktree"两条过期事实。
- 统一重连宿主 `Pry.Client.ConversationEventStreamRunner` 与错误呈现表 `PryErrorPresenter` 进主工作区，`src/Pry.App` 的两条事件流循环改为**只消费连接状态与终止出口**，上一轮临时补在循环里的两处 `when (ex.RetryForbidden)` 与文案类 `BackendBanNotice` 一并撤除。退避（500 毫秒起、倍增、15 秒封顶、连续 8 次放弃）、按最后投递序号续订、重连成功先补取一次历史、以及"服务端写明 `retryable:false` 就到此为止"全部由 Runner 单点判出，禁令分支排在计数与退避之前——顺序反了会把"永不重试"演成"再等 8 轮"。顺带修掉两条旧缺陷：`PryMessengerWindow` 原本固定 `after=-1`，重连时把整段事件日志重放而不是续传；两条循环的固定间隔（500 / 3000 毫秒）也统一成有上限的退避。界面向用户说的话收敛到呈现表：PLAT-001 的七类 Provider 码逐档给出可执行动作（改密钥 / 等一会儿 / 缩短上下文），禁行三档明说"已停止重连"和该做的那件事，服务端原文与 `traceId` 只出现在技术详情。共享词表随之搬家：`ProviderErrorCodes` 从 `Pry.Core.Inference` 移到 `Pry.Contracts`——客户端要按同一批码分档却不能依赖 Core，留在原地等于让界面抄七条字面量。判据共 51 例（Runner 15、呈现 31、界面端到端 5 例保留），其中"未宣告/可重试必须继续重连"与"禁行必须只发一次"两个方向同权重；防复发门 `SingleBanJudgeBoundaryTests` 扫产品源码，`Pry.Client` 之外再出现 `RetryForbidden` 即红（证伪跑：禁行分支反向后 10 例同时转红）。如实登记未覆盖处：`PryMessengerWindow` 剩下的 UI 线程投递与切房丢弃没有自动化（仓库无 headless Avalonia 设施），`RemoteConversationTurnManager` 刻意不挂补取钩子——它的回复走追加渲染，重放会把同一条画两遍。**（2026-09-21 第五轮返工更正）**：本条末尾"切房丢弃没有自动化"已过期——接线抽成 `MessengerEventStreamController` 后它有 13 例（见本节第一条），剩下的只有 `Dispatcher.UIThread` 投递本身；`RemoteConversationTurnManager` 那句仍然成立，其处置与 `MainWindow` 的删除次序一起记在迁移文档 §8 第 2 条。
- 第二个 Pry 窗口会接上已经在跑的后端（第四轮 Release Blockers 第 2 条，PLAT-005 M0-d 读侧 / 场景 `PLT-003`）：`runtime.json` 此前只有写侧——后端把 `{ pid, port, token, … }` 原子落盘，但仓库里没有读侧，界面永远自己内嵌起一个宿主。现在 `Pry.Client.RuntimeDiscovery` 补上另一半，判据是**不确定就不接**：文件缺席、半截 JSON、不是对象、端口越界、令牌为空、`pid` 已退出或就是本进程，一律返回"没有后端可用"而不是猜一个入口（陈旧文件里的端口可能已被同机另一个进程占走，连错比连不上更难诊断）；未知字段忽略，所以新旧构建的升级顺序不被读侧锁死；探测走豁免的 `GET /api/v1/info`，进程还活着但 HTTP 面已不再服务时同样不接。版本兼容**不在读侧判定**，仍由协商中间件说了算，避免出现第二处事实源。`Pry.App` 启动改为先发现后内嵌，界面层三处硬编码的 `http://127.0.0.1:5078/` 全部移除，改由 `PryBackendClient.Unattached()` 占位（指向端口 0：一次意外的探测请求只会失败，不会连上"恰好守在 5078 上的某个进程"）。代价如实说明：接上别人的后端时，退出本窗口不会关掉那个进程，退出提示自此按"是否拥有后端"分支。用例分两层共 14 例——`RuntimeDiscoveryTests`（13）判拒绝面与"存活但无人服务 ⇒ 拒连"、`RuntimeDiscoveryProcessTests`（1）真起一个 `dotnet exec Pry.Api.dll` 子进程，只凭那份文件连上并让受门端点返回数据；证伪运行显示抽掉令牌头那一行后进程级用例即红，它判的确实是"令牌来自文件且真的过门"。仍归 UI-027 的部分：`App.InitializeDesktopAsync` 这条真实启动顺序没有自动化覆盖。
- 界面会对"别再试了"停下来（QA-P2-01 后半，PLAT-005 / UI-027）：上一条把 `retryable` 送到客户端边界，这一条把它用起来。`PryBackendException.RetryForbidden` 成为唯一禁行判据——**只有服务端写了 `false` 才算禁令**，`null`（旧服务端、非 ProblemDetails、坏 JSON）与 `true` 都照既有策略继续重连；`src/Pry.App` 的两条事件流循环（`RemoteConversationTurnManager`、`PryMessengerWindow`）各在通用兜底之前拦下禁行，终止退避、不再重启请求，并给出说清怎么修的文案（协议不匹配→用同一个发行版启动 Pry；令牌被拒→完全退出再启动；未登记的禁行码→原样带出服务端 `code`）。此前这两种情形会被演成"网络不稳"：`426` 以 500 毫秒、断连提示以 3 秒固定间隔无限重连，用户永远等不到那句真正的原因。回归同时判两个方向——禁行的三条只发一次请求、只弹一条终止提示，而"服务端没宣告"与"服务端说可以重试"必须**继续**重连且不升级为终止；只测前一半，一个过严把网络抖动做成永久失败的守卫同样能全绿。统一重连宿主（UI-027 的 `PryErrorPresenter`）合流后接管同一判据，这两处分支即被其覆盖，不另立第二套错误状态源。
- `retryable` 自此真的走到客户端（QA-P2-01，PLAT-005）：`ApiProblemResponse` 补 `Retryable`（`bool?`）、`PryBackendException` 逐层透传。此前服务端在 `426 protocol_mismatch` 与 `401 unauthorized` 上都写了 `retryable: false`，`docs/api-v1.md` 也写明"界面必须停在明确的版本不匹配提示，不能当作网络故障无限重连"，但契约形状里没有这个字段，禁令在解析时被丢掉，界面于是把一个永不会变好的错误当普通断链以固定间隔重连。语义按三值处理而非布尔：`false` 是"再发一次也不会变好"的禁令，`null` 是"服务端没宣告"（旧服务端、非 ProblemDetails 响应、坏 JSON），消费方不得把 `null` 猜成 `false`（会把没听见宣告的界面钉死）也不得猜成 `true`（会把刚发出的禁令淹没）。事件流订阅者——真正执行重连的那一方——走的是同一个 `EnsureSuccessAsync` 出口，判据因此钉在 `ReadEventsAsync` 上而不是只测普通请求。
- 硬件探测有了截止时间（QA-P2-04②，PLAT-001）：`LlamaHardwareDetector` 此前对子进程的等待完全无界，而它持着一把单槽信号量——一次挂起不只让真机启动路径停在"检测设备"，之后每一次探测都排在它后面。现在设备探测预算 20 秒、显存查询 10 秒，到点只终止**自己刚拉起的那棵进程树**（绝不按进程名扫描系统里的其他 `llama-server`，那是模型进程所有权的边界），孙进程变孤儿继续写的事实由测试判掉。两条不猜的语义：探测超时不静默当成"这台机器没有设备"（那会让自动选择在 GPU 机器上悄悄退回 CPU 且无人知晓），调用方取消仍以 `OperationCanceledException` 浮出而不是被报成硬件故障。同时把"先等退出、后读两个流"改成并发等待——管道缓冲写满时那种写法会互锁成永久等待。
- 回环守卫补上判据（QA-P2-03，场景 `PLT-001`）：`Program.cs` 的非回环拒绝逻辑一直存在，迁移文档 §8 第 1 条却把它记成"现有行为回归"，而 `tests/` 里没有任何用例进入那条分支。现在 `0.0.0.0`、`10.0.0.5`、外部域名与**空串**四种 `Pry:Url` 都必须在 `BuildAsync` 内失败，且失败时数据目录尚未创建（拒绝发生在写库与建日志之前）；`127.0.0.1` 正向对照证明守卫不是无脑拒绝一切。文档那条措辞已更正。
- 模型 Provider 的失败分类与有界等待（PLAT-001）：Provider 侧失败不再是"一个 500 加一句稍后再试"。`Pry.Core` 新增 `ModelProviderException` 与七类稳定码（`provider_auth`、`provider_rate_limited`、`provider_timeout`、`provider_unavailable`、`provider_invalid_response`、`context_overflow`、`provider_cancelled`），归类单点入口 `ModelProviderError` 同时吃 HTTP 事实（状态码、`Retry-After`、有界错误体片段）与 .NET 异常类型；Core 不认识 HTTP，状态码翻译只在 `ApiExceptionMiddleware` 一处（502/429/504/503/400），`turn.failed.code` 与记忆向量降级状态改用同一批码，界面自此能区分"改密钥"与"稍后重试"。出站等待处处有界：模型起头超时远端 120 秒 / 本地 300 秒（本地那一档覆盖 llama-server 单槽排队的合法等待）、流式回复空档 120 秒判超时、本机请求预算**逐请求声明**（普通 32 秒且与心跳同源、语音识别 4 分钟）、共享的 `HttpClient.Timeout` 退为更宽的兜底 `RequestBudgetCeilingMs`——此前六处声明点写的是 `Timeout.InfiniteTimeSpan`，Provider 挂起时界面既等不到回复也等不到错误，现在由源码门禁（`BoundedWaitBoundaryTests`）禁止该写法复发。坏帧不再被当成"回复正常结束"，调用方主动取消仍原样以 `OperationCanceledException` 浮出（否则一轮正常打断会被记成模型故障），Provider 错误体截到 2 KiB 并先遮密钥、只进本机日志。
- 后端可诊断性地基（PLAT-006）：内嵌宿主此前只有 console sink，双击启动时日志等于不存在、崩溃后没有可读证据，现在自带滚动文件 sink（`<DataDirectory>/logs/pry-backend.log`，单档 5 MiB、保留 5 份、逐条 flush、`Pry:LogDirectory` / `Pry:LogFileMaximumBytes` / `Pry:LogFileCount` 可夹取配置），sink 自身故障只停用本条写入并一次性报告，不拖垮后端，也不持有常驻句柄。"秘密不进日志"从口号变成可判据：`LogRedactor` 在渲染之后、落盘之前遮蔽本机会话令牌与 `api_key` / `Bearer` / `sk-` / URL userinfo 形态（键名保留），Core 侧 `MemoryDatabase` 与 `LlamaServerManager` 新增可空 `ILogger` 注入口（默认 `NullLogger`，未接线调用方行为不变），子进程 stderr 的密钥在离开 `LlamaServerManager` 前就已被遮掉。上传不再是"无上限"：单文件硬上限默认 50 MiB（`Pry:MaximumUploadBytes` 可下调），超限在落盘前拒绝并返回 `413 urn:pry:error:payload_too_large`（带 `resource` 与 `maximumBytes`），`GET /api/v1/media/policy` 的 `maximumBytes` 自此发布真实数字，此前散在 `Program.cs` 与 `MediaController.cs` 两处、值各有不同的 `long.MaxValue` 天花板收敛为 `MediaUploadLimits` 单一来源，并由测试枚举每一个声明点。
- 本机会话令牌与进程发现（PLAT-005 M0-d）：后端启动时生成随机 32 字节令牌，并把 `{ pid, port, token, protocolVersion, schemaVersion, startedAt }` 原子写入 `<DataDirectory>/runtime.json`，优雅退出时删除；上一实例遗留的文件被覆盖并留一条崩溃恢复记录。HTTP 面改为**默认拒绝**——除 `GET /health` 与 `GET /api/v1/info` 两条豁免外，缺少或错出示 `X-Pry-Token` 的请求一律 `401 urn:pry:error:unauthorized`，比较使用等时算法，令牌明文不进响应也不进日志，且拒绝发生在业务写入之前。界面不再"先占一个空闲端口再交给后端去绑"，改由 Kestrel 绑定 `127.0.0.1:0` 并持有该 socket、端口从绑定结果读回，端口竞态窗口随之消失；`/api/v1/info` 的 `features[]` 自此包含 `session-token` 与 `runtime-discovery`。
- 协议版本协商地基（PLAT-005 M0）：新增 `GET /api/v1/info` 握手端点，`api/**` 响应带 `Pry-Protocol-Version`，客户端每请求带 `Pry-Client-Protocol`；`major` 不等或客户端 `minor` 高于服务端时返回 `426 protocol_mismatch` 且 `retryable: false`，握手端点本身永不拒绝，未带该头的旧客户端继续服务。版本常量单点定义于 `Pry.Contracts.PryProtocol`。
- SSE 事件流保活与半开连接检出（PLAT-005 M0-b）：会话事件流空闲满一个心跳周期时下发 `:ping` 注释帧，心跳周期与客户端存活窗口同源定义于 `PryProtocol`；`PryBackendClient.ReadEventsAsync` 改为"读到任意一行即算存活"，连续两个心跳周期无任何字节就抛 `TimeoutException` 交给上层重连，服务端不发也不关的半开连接不再让读取永久阻塞。轮次真正交付完成后补发 `turn.completed`，界面清"发送中"的死分支自此生效。服务端周期可用 `Pry:EventHeartbeatMs` 调节，并被强制夹在 1 秒到客户端存活窗口一半之间。
- 本地模型密钥改经子进程环境块 `LLAMA_API_KEY` 传递，不再进入进程命令行（PLAT-002）；保留回环绑定与 stderr 遮蔽，并保留嵌入模式的 `--embedding` 启动分支。
- 持久事件游标与显式过期（PLAT-005 M0-c）：会话事件写入 `Pry.Core` 新增的 `protocol_events` 日志（数据库结构 8→9），序号由服务端跨会话统一分配，进程重启、运行时重配置与空闲驱逐都不再作废客户端游标；游标越出保留窗口时服务端先下发一帧 `stream.resumed { reason: "buffer_expired", firstRetained, latestSequence }` 再照常补投，客户端不必靠"序号连续"自行判定丢事件。`session.ready`、`turn.state` 等电平类事件不入库、不被重放，帧格式与客户端义务不变。保留时长成为显式配置（每会话条数 / 年龄 / 总行数，越界夹取、配 0 不会退化成每连必报过期），删除消息或删除会话会在同一事务内遮蔽其事件行，明文不得在传输层留下副本；`/api/v1/info` 的 `features[]` 自此包含 `durable-event-cursor`。

- 持续 ConversationRuntime：合并连发、typing 等待、沉默闸门、插话取消、分段投递、旧结果隔离与遗留计划清扫；主动消息共用 Runtime 投递，并保持消息与配额原子提交。2026-09-21 已从隔离验证 worktree 并入主工作区共享基线，仍未提交、未形成发行候选。
- 运行时只读诊断接口（RT-002）：`GET /api/v1/conversations/{id}/runtime-state` 与 `.../runtime-trace`，供 Developer Mode 与 Eval 读取阶段、epoch、缓冲/待投递队列、退避、轮次终局与决策轨迹；读取不会创建或唤醒运行时，运行时被空闲驱逐时只回持久层 fencing 水位。负载与轨迹均不含消息正文或用户输入。

- 新增持久化已读水位、标记已读接口、会话及全局未读投影；阅读状态跨重启保留，支持并发新消息、删除/撤销和幂等重试。

- 新增后端主动话题调度：角色策略和活动心跳、已加载模型复用、前台操作取消、持久化冷却/每日限额/去重、原子消息投递和 SSE；默认关闭，前端通知体验待接入。

- 接入正式 Pry 应用图标，用于 Windows 可执行文件、两套主窗口、启动失败窗口和系统托盘。
- 新增工程踩坑与防复发记录，集中说明内嵌后端、模型进程、分支集成、CI 环境和前后端契约问题。
- 新增前后端统一文档索引，明确权威资料顺序、职责边界、接口流程和交付门禁。
- 新增后端角色互动配置与命令：内置配置用户覆盖、戳一戳和动作预设、持久化幂等、冷却、忙碌保护、消息来源及 SSE 表现事件。

### Changed

- 按 QA-P6-01 的教训把"等某件事发生再判定"这一形状在全仓扫了一遍，修掉两处（**产品代码未改动**）：① `CoreTests.Closing_windows_process_job_terminates_owned_process` 判"关掉作业句柄会杀掉模型进程"，却从没断言关句柄那一刻进程还活着——子进程自己先退（`ping` 被拦截或不可用）就白送一个 Pass，而这条机制正是 `LlamaServerManager.cs:51` 用来阻止 llama-server 活过宿主的。第一版修法（`Attach` 之后立刻读 `HasExited`）被自己的证伪打掉：把子进程换成 `cmd /c exit` 仍 27 毫秒通过，因为 `Process.Start` 返回后的第一行几乎永远读到"还没退出"。最终写法是先给一段远短于子进程自身寿命的落定时间（500 毫秒对约 30 秒）再采样，同一证伪转红在 `:88`（549 毫秒）；平台门控也从静默 `return` 改为 `Assert.Skip`，非 Windows 上不再算一次通过。② `SessionTokenTests.ReadDescriptorAsync` 的 2 秒窗口判的是"后端写不写发现文件"，不是"两秒内写不写"，于是四程序集并行的磁盘 IO 会把它做成假红，放宽到 5 秒（永不写出的回归照样红）。同一轮排查如实清掉四组可疑点：两处 `WaitUntilAsync` 超时会 Fail、日志遮蔽的两例有"必须看见 `[redacted]`/持久层就绪"的正向对照（负向断言不因文件为空而过）、禁令用例的 1.5 秒窗口宽于首档 500 毫秒退避、`RuntimeDiscoveryProcessTests` 的 60 秒等待带"子进程先退即 Fail"。另有两处固定延时（`ConversationRuntimeTests:329` 的 80 毫秒、`RuntimeRaceTests:135` 的 50 毫秒）分别是假红与假绿方向，属 Character Runtime 名下文件，只登记不代改。门禁：Release 构建 0 警告 / 0 错误，解决方案级连续两次 **628 通过 / 0 失败 / 2 跳过**（跳过仍是原来那两条环境门控），`measure-fixed-eval.ps1` 两次退出码 0。教训补在 `docs/engineering-pitfalls.md` 第 24 条的"采样点本身"变体。

- 判"到期杀整棵子进程树"那条用例的前置条件从**时间窗口**改成**事件**（QA-P6-01，PLAT-001）：它原先把两件不同的事挂在同一条 250 毫秒预算上——"孙进程来得及写下第一行"与"探测在预算内被切断"。四个测试程序集并行时前者可以慢过 250 毫秒，于是产品侧在孙进程开始写之前就把树杀干净，用例红在自己那句"本用例没有判据可言"的前置断言上；第六轮解决方案级连红 2/2、隔离跑全绿暴露的正是这条。现在两轴分开：先在 3 秒窗口内轮询到"写下第一行"这个事件，再让 5 秒探测预算到点，判据仍是杀掉以后文件不再增长。等不到事件照旧报红（不放行、不 skip、前置断言原文保留）。上一轮"把 powershell 换成 cmd"只换了启动器量级、没换结构，这条是它的续作；证伪在新结构上重做一次（`entireProcessTree: false` ⇒ 该例在增长断言处转红，5538→5964 字节），教训记在 `docs/engineering-pitfalls.md` 第 24 条。产品代码未改动。

- 契约层依赖方向反转（PLAT-005）：`Pry.Contracts` 不再引用 `Pry.Core`，改为 `Pry.Core → Pry.Contracts`，契约程序集自此零依赖。18 个"既进 HTTP/SSE 响应、又被 Core 存储与界面直接使用"的类型（`ChatRole`、`ChatMessage`、`ConversationRoom`、`ConversationFolder`、`MemoryRecord`、`MessageSources`、`RuntimeState`、`TurnTakingSettings`、`TypingStyle`、`ModelCapabilities`、`ModelTuningPreferences`、`ShortcutSettings`、`UserProfilePreferences`、`ImageDisplayPreferences`、`DesktopPetPreferences`、`StickerSource`、`CharacterPromptMode`、`MainWindowLayoutModes`）的声明整体移入 `Pry.Contracts/CommonContracts.cs`，属性名、默认值与枚举拼写逐字不变，**线格式与数据库里已存的 JSON 均无变化**。此前 `Pry.Client` 经契约程序集传递看到 `Pry.Core` 并把其中类型用作公开返回值，现在客户端签名只剩契约类型。消费方以每项目一份 `GlobalContractUsings.cs` 导入契约命名空间，避免为一条 `using` 改动一批正被其他协作者编辑的界面文件。未采纳"另立一套传输 DTO + 在 Api 映射"的方案（同形两套会各自漂移，且要在约 20 个界面服务文件里长映射层），这是一处待复判取舍，见 `docs/phase1-platform-baseline.md` §2 规则 4 与 `docs/protocol-versioning-and-websocket-migration.md` §4.1 第 9 条更正记录；防复发门为 `tests/Pry.Api.Tests/ContractBoundaryTests.cs`。

- 许可变更：程序源代码与项目文档从 Apache-2.0 改为 **GPL-3.0-only**（2026-09-21），以便在许可兼容的前提下直接复用 MaiBot/MaiCore 的 Runtime 实现与 Satori 的边界设计。已按 Apache-2.0 分发的历史版本（v0.0.1、v0.0.2）既有权利不受追溯影响，变更只约束其后再分发。原创美术素材不适用代码许可证，其权利归属、禁止行为与二次创作政策的实质条款未因改约而变动；可复用上游与留痕要求见 `docs/upstream-license-compatibility.md` 与 `licenses/THIRD-PARTY-NOTICES.md`。
- 二次创作政策细化，并明确人工智能用途的边界：`FAN_CONTENT_POLICY.md` 扩为十节可判定规则（免授权创作清单、商业使用的判定口径、原始文件再分发禁令、署名与非官方要求、官方身份与标识、内容与声誉底线、人工智能、第三方内容与授权链、授权申请、违规处理与政策更新）。按作者裁决，用第三方工具或通用模型进行生成、修图、配音与免费发布全部允许，且与普通二创适用同一收益口径（平台被动收益允许）；但以原创素材训练、微调、LoRA、风格学习、数据集构建与素材模型训练仍需事先书面许可，委托或协助他人训练同样受限，数据集与专门模型权重的分发亦不放开。作者保留的两条明确禁止是：不得把 AI 生成的角色形象声明为官方内容，不得借批量生成规避政策。同时明确平台自带激励与广告分成的被动收益视为非商业，而针对作品的付费墙、会员档、定向打赏、约稿收费与任何实体或数字制品销售仍需事先书面许可；成人向内容须显著分级标注，且**禁止对呈现为未成年外形的角色进行任何性化描绘**。`ASSETS_LICENSE.md` 第 3、4 条同步：加入 AI 创作方式与平台被动收益的允许项、AI 训练的禁止项与不得声明官方条，链上禁令保留。同日第四版按作者补充裁决细化三处：① 付费约稿只约束收钱与公开承揽的一侧，粉丝出资委托视为非商业，并新增"不得把 Pry 原角色声称为个人原创设定"与"二创形象可自称创作、须注明基于 Pry"的归属口径；② 原创素材区分**素材**与**成品**，官方插画/宣传图/官方壁纸允许单张、未修改、带署名转载（不打包成自取合集），`ASSETS_LICENSE.md` 第 3 条相应增加允许项、第 4 条再分发项加入但书；③ 第 6 节内容底线改为红线／灰色两级——R18G 与极端内容（性暴力、血腥、虐待、猎奇）绝对禁止且主动下架，其余成人向内容不予授权但是否追索由作者自行决定，并在 `ASSETS_LICENSE.md` 第 4 条写明不追索不产生默示许可。 同日第五版：作者明确实体与数字制品销售**不设小批量或"个人手作"豁免**（理由是按数量放行会被电商平台商家以"个人少量制作"名义量产贩售），并把"AI 二创形象不得用作账号头像"从禁止项中移除——判定标准收敛为单一的"是否使公众误认官方来源"，二创形象因传播获得热度不构成违规，官方 Logo 仍不得用作头像或标识；`ASSETS_LICENSE.md` 第 5 节相应说明；两处公开写入授权联系邮箱 2478270568@qq.com。 同日第六版细化比赛与征稿活动：凭 Pry 二创作品参加同人赛、绘画征稿、主题企划、平台话题与激励活动获得奖金、奖品或流量分成**原则上视为非商业**，判据收敛为三条（奖励不与销量或付费人数挂钩、领奖不要求让渡商用／出版／独家权利、活动本身不是品牌或商品推广），金额高低不作为判据；需要事先书面许可的是**以让渡权利为条件的获奖**与**第三方把 Pry 当作卖点招集活动的主办方一侧**，参赛者不承担额外义务。第 8 节相应写明创作者无权把角色商用权利授予活动方。 同日对 `ASSETS_LICENSE.md` 与 `FAN_CONTENT_POLICY.md` 做书面语统一：全文改用中文引号、去除口语与内嵌裁决叙述，政策新增第 11 节版本与变更说明；同时修复四处内部缺陷——第 1 节免授权的前提范围由"第 2～7 节"更正为"第 2 至 8 节"、第 2 节"本节三条"的歧义引用改为明确指向、第 3 节说明政策署名文本与 `ASSETS_LICENSE.md` 第 5 条最低署名文本的关系、第 6 节明确灰色级条件失效时适用红线第 6 项且灰色级下作者通常仅就相关内容主张处理（与上位文件第 4 条末段一致）。除上述已登记的澄清外，各条许可范围未作变更。 同日第七版补两处裁决：人工智能免授权项加入主观善意标准（"该模型未以原创素材训练"以创作者合理不知且不应知为限，自行训练、委托训练与参与数据集构建不适用该豁免），并明确受托画师未获许可接单的责任由画师自行承担、不影响出资委托方依政策发布该成品的资格。
- 恢复按内容宽度呈现的角色回复与消息头像，保留连续工作区壁纸；统一角色、记忆、表情、设置、模型库、互动、主动话题和确认弹窗的排版、主题及按钮状态。
- 重设计桌面主窗口为统一的陪伴空间：顶部导航、按需展开的历史对话、角色入口、连续回复阅读区与独立输入区；原 Messenger / Card 偏好均打开新主窗口。
- 角色资料页增加字段标签和固定保存区；设置移除旧窗口切换入口。统一浅深主题与控件状态，自定义背景覆盖陪伴工作区，管理页面采用稳定阅读表面。
- 重写当前项目计划，仅保留有效架构结论、未完成任务、验证基线和发行顺序；累计式旧计划移入本机废案归档。
- 更新 README 的产品状态、前后端边界和剩余工程项；历史集中人工验收不再作为当前发布阻塞项。

## v0.0.2（2026-09-07）

目标平台：Windows x64

发布形态：自包含 ZIP 与环境依赖 ZIP，均解压后运行 `Pry.App.exe`

## 结论

v0.0.2 的前后端**职责分离合格，但不是完全的进程、程序集或模型类型分离**。

- 合格部分：会话、消息、文件夹、记忆、媒体、配置、模型生命周期、推理与语音识别均由后端负责；前端通过 `Pry.Client` 使用 HTTP/JSON/SSE，没有直接读写 SQLite，也没有启动或管理 `llama-server`。
- 未完全分离部分：`Pry.App` 仍直接引用 `Pry.Api` 并在同一桌面进程内启动随机回环端口的后端；`Pry.Contracts` 仍引用 `Pry.Core` 的领域类型，经典前端也继续使用这些类型作为显示投影；`Pry.Core` 中仍包含 HTTP 模型适配器。
- 发布判断：这些剩余耦合不影响当前单体桌面发行包工作，但不能把前端 Release 与后端 Release 当成两个可独立启动后自动对接的产品。普通用户只应运行 `Pry.App.exe`。

## 本次边界收口

1. 后端运行资源迁至中立的 `src/Pry.Resources`，`Pry.Api` 不再反向复制 `Pry.App/Resources`。
2. 删除旧前端资源副本，避免同一模型配置出现两个权威来源。
3. 文件夹操作经应用服务进入数据库；会话媒体摘要的本地化文案由前端负责。
4. 模型性能档位、上下文降级和模型重载判断留在后端；前端只消费 DTO 投影。
5. 删除“保留模型服务并退出”的虚假选项。当前内嵌后端退出时必然关闭其拥有的模型进程。

## 剩余架构债务

- P1：若未来要求真正独立部署，应新增受认证的后端进程、端口发现/握手和明确的生命周期所有权，然后移除 `Pry.App -> Pry.Api` 项目引用。
- P2：逐端点把传输 DTO 从 Core 领域对象中独立出来，使 `Pry.Contracts` 不再引用 `Pry.Core`。
- P2：把 OpenAI-compatible HTTP、llama.cpp 进程和语音适配器从 Core 领域层迁至后端基础设施层，以满足 Core 不依赖 HTTP 的长期边界。

上述工作均需要独立迁移和兼容测试，不在 v0.0.2 发布前进行高风险整体重写。

## 发行包

本次公开两种无模型 ZIP：

- `Pry-v0.0.2-win-x64-lite.zip`
- 包含自包含 .NET 桌面程序、内嵌 API、llama.cpp CUDA 运行必需文件、许可与说明。
- 不包含模型权重、数据库、日志、密钥、用户素材或本机配置。
- 解压后应用可以直接启动；本地聊天、图片理解和语音识别需按 `Resources/appsettings.json` 放置模型，或配置兼容服务。

- `Pry-v0.0.2-win-x64-minimal.zip`
- 不包含 .NET/ASP.NET Core、llama.cpp/CUDA 或模型，仅保留 Pry 程序和必要应用依赖。
- 需要系统安装 Windows x64 的 .NET 10 ASP.NET Core Runtime；使用本地模型时再执行 `scripts/install-local-runtime.ps1`。
- 使用在线 OpenAI-compatible 服务时可跳过本地推理环境安装。

两种包内都保留经 SHA-256 校验的安装/下载脚本。`install-local-runtime.ps1 -RuntimeOnly` 可只安装已验证的 CPU llama.cpp，不下载入门模型。

完整模型包暂不公开。当前预设权重合计约 11.38 GB，且单个 Qwen3.5 文件超过 6 GB；GitHub Release 每个附件必须小于 2 GiB。打包脚本保留显式 `-IncludeModels` 开关供本机校验，但默认且本次发布不会生成完整包。

## 构建与验证

标准命令：

```powershell
dotnet build Pry.slnx -c Release
dotnet test Pry.slnx -c Release --no-build
git diff --check
./scripts/package-release.ps1 -Version 0.0.2
```

自动化基线：Release 构建 0 警告、0 错误；Core 17、API 16、Client 3、App 156，共 192 项测试通过。

确认结果：两个 ZIP 均可完整列出并已生成 `SHA256SUMS.txt`；自包含包和环境依赖包分别从独立解压目录启动后，都获得可响应窗口与随机回环 API 监听；隔离数据目录正常创建 `memory.db`；包内没有模型和用户数据；程序文件版本为 `0.0.2.0`。
