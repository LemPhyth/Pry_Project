# Pry 后端拆分方案

## 当前状态

原桌面进程同时承担 Avalonia UI、SQLite 持久化、模型进程、语音、文件访问和对话编排。`MainWindow.axaml.cs` 是主要耦合点。`Pry.Core` 已包含可复用的领域模型、数据库和推理能力，但此前没有网络边界。

## 目标边界

```text
Pry.App / 其他客户端
        │ HTTP / JSON（后续聊天使用 SSE）
        ▼
Pry.Client ── 类型安全 HTTP/SSE 客户端
        │
Pry.Contracts ── 稳定 DTO（不含运行逻辑）
        │
Pry.Api
  Controllers：HTTP、状态码、DTO
  Application Services：校验、用例编排、资源边界
        │
        ▼
Pry.Core
  MemoryDatabase / Conversation / Inference / Domain Models
        │
        ├── SQLite: %LOCALAPPDATA%/PryCompanion/memory.db
        └── 本地或 OpenAI-compatible 模型
```

已迁移会话、消息、文件夹、长期记忆、聊天回合、角色、贴纸、外观媒体及模型配置的 HTTP 边界，保留原数据库格式，因而不需要数据迁移。模型进程注册表会按模型及其运行参数复用实例；文字和视觉选择相同模型时不会重复加载。桌面启动投影也改由 API 查询，受管背景、头像和贴纸通过仅接受 `/api/v1/` 相对地址的客户端缓存下载。桌面端不再直接读写 SQLite、角色/贴纸清单、偏好配置或后端模型配置。

## 职责规则

- 客户端只负责展示、输入和本地交互状态，不直接打开 SQLite、启动模型或决定消息角色。
- API 负责输入校验、资源存在性、业务错误、持久化和最终服务端时间。
- API 负责按可配置保留期回收无外观、角色、贴纸或消息引用的托管媒体；客户端不得直接执行存储清理。
- Core 不依赖 HTTP 或 Avalonia，继续承载领域与基础能力。
- Contracts 只描述跨进程数据；Client 负责 HTTP、Problem Details 和 SSE，不访问数据库或模型。与 Core 存储和界面共用的形状（`ChatMessage`、`ConversationRoom`、`RuntimeState`、`TurnTakingSettings` 等）以 `Pry.Contracts` 里的声明为唯一来源，依赖方向是 `Pry.Core → Pry.Contracts`（PLAT-005，2026-09-21）；Core 内部改名自此不能静默改变线协议，Client 的公开签名也不得再出现 Core 类型。
- 当前产品是单机单用户应用，API 只监听回环：独立进程默认 `127.0.0.1:5078`（`Pry:Url` 可改），内嵌宿主改绑 `127.0.0.1:0` 由 Kestrel 自选端口并写出 `runtime.json`（PLAT-005 M0-d）。界面启动**先发现后内嵌**：`Pry.App` 先用 `Pry.Client.RuntimeDiscovery` 接上本机已有后端，接不上才起自己的宿主，因此客户端与测试代码里没有备用端口，读回的端口与令牌只来自那一份文件。回环不区分用户，因此"谁能连"由本机会话令牌决定，而不是由"谁能访问 127.0.0.1"决定。若允许局域网或公网访问，必须先增加认证、授权、TLS、限流和来源策略。

## 日志与敏感字段（PLAT-006）

- Core 只有注入口：`MemoryDatabase` 与 `LlamaServerManager` 接受可空 `ILogger`，默认 `NullLogger`。Core 不注册 provider、不决定日志去哪里，也不新增自有日志抽象。
- 落盘位置与保留代数只由 `Pry.Api` 的滚动文件 sink 决定（`<DataDirectory>/logs/`，见 `docs/api-v1.md` 的"后端日志"节）。
- 遮蔽发生在渲染之后、落盘之前，且**只有一套规则**：子进程 stderr 的密钥在离开 `LlamaServerManager` 时已遮，`LogRedactor` 在 sink 出口再按注册值与字段形态遮一遍；同一文本走两条规则时，漏掉的那条就是泄露点。
- 日志不暴露 API 未暴露的东西：不写数据库绝对路径、消息正文、命令行或任何令牌明文。判据要求 `[redacted]` **出现**，而不只是"秘密不在里面"——后者可能只是压根没写。

## Provider 失败与有界等待（PLAT-001）

- Core 不认识 HTTP。Provider 失败统一表达为 `ModelProviderException`（`Code`、`SafeMessage`、`Retryable`、`RetryAfter`、`DiagnosticDetails`），七类码见 `docs/api-v1.md`；状态码翻译只发生在 `ApiExceptionMiddleware` 一处，新增码必须同时补映射与文档。
- 归类只有一个入口 `ModelProviderError`。任何调用方不得再 `EnsureSuccessStatusCode()` 后凭异常类型猜原因——把 401 与 429 压成 500 正是那条路径。
- **调用方取消原样传播**，不包成 Provider 异常：`ConversationRuntime` 是按 `OperationCanceledException` 把这一轮判成"被打断"的，包装会把用户主动按下停止记成一次模型故障。取消的"可观测"要求是它必须以异常形式浮出来，而不是继续静默 `yield break`。
- 有界等待一共四条线：模型起头超时（远端 120 秒、本地 300 秒，本地这一档要覆盖 llama-server 单槽排队的合法等待）、流式回复空档 120 秒、本机客户端请求预算 32 秒、语音识别整请求 4 分钟。取值集中在 `ProviderTimeouts` 与 `PryProtocol`，不得各自复制字面量。本机这两档由 `PryBackendClient` **逐请求声明**（linked CTS），`HttpClient.Timeout` 只取更宽的 `RequestBudgetCeilingMs` 作兜底——它是客户端级设置，若小于最宽的声明档就会顶掉那一档（4 分钟语音预算曾被 32 秒顶掉，2026-09-21 第五轮修正）。
- `HttpClient.Timeout` 在使用 `ResponseHeadersRead` 时**只约束到响应头到达**，不约束随后的正文读取（本机实测）。它因此既敢设得有界而不打断长回复，也替代不了读取侧的空档判定；反过来，一个迟早要整体读进内存的响应不该用 `ResponseHeadersRead`，那样超时形同不存在。
- Provider 的错误体片段截到 2 KiB 并先遮掉本 Profile 的密钥，只进本机日志，绝不进响应体：Provider 常把请求头回显在错误里。
- `BoundedWaitBoundaryTests` 扫产品源码，禁止再出现 `Timeout = Timeout.InfiniteTimeSpan`：这一行曾在三个界面、一个内嵌入口和两个模型注册表里各写过一遍。

## 模型进程所有权

- `ModelProcessRegistry` 在一个后端生命周期内按模型与运行参数复用实例。
- Windows 本地模型进程加入 Pry 私有 Job Object，并启用 `kill-on-close`；后端正常释放、崩溃或被强制结束时由操作系统清理该 Job 中的模型进程。
- 后端不按进程名扫描或结束系统中的其他 `llama-server`，也不复用没有所有权证明的随机端口进程。
- llama.cpp stderr 始终异步排空，只保留有界诊断尾部；API 仅返回稳定错误码、安全摘要和是否可重试，详细启动输出留在本机日志。
- 客户端请求取消不会取消全局模型加载。失败的注册表项会移除，修复资源问题后可再次请求，或调用 `POST /api/v1/runtime/retry` 显式重置。

## 迁移顺序

1. 前端改用已实现的会话、消息、文件夹和记忆 API。
2. 桌面窗口已通过 `Pry.Client` 订阅 SSE，并调用角色、偏好、贴纸、模型选择、媒体及语音 API，只保留视图、录音采集、输入、导航和窗口生命周期。事件流的传输韧性也在 `Pry.Client`：`ConversationEventStreamRunner` 负责有界指数退避、按事件序号续订、重连后通知补取历史，以及"服务端写明 `retryable:false` 就到此为止"这一唯一禁行判据；界面只消费连接状态与终止出口，提示文案由 `PryErrorPresenter` 单点给出（UI-027 / PLAT-005 联合收口）。
3. 已删除 `MainWindow` 中无调用方的数据库、配置文件和模型编排代码。
4. 后续继续把大型窗口拆为可测试的视图控制器，并执行完整 UI 回归；这属于 M2 可维护性工作，不再是 API-003 的数据权威边界。

不在第一阶段暴露客户端本机路径，也不允许客户端写入 assistant/system 消息；这是后端成为数据权威所必需的边界。
