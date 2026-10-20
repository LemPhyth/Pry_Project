# Satori：跨平台 Bot Protocol 与 Remote Bot abstraction

> **⚖️ 照搬白名单内（作者 2026-09-21 裁决，`DOC-005`）：本项目允许照抄。**
> Satori 是 **MIT**，许可方向天然兼容：MIT 代码可以进 GPL 或 AGPL 作品，只要保留上游版权许可文本。Pry 代码许可现为 **AGPL-3.0-only**（2026-10-20 由 GPL-3.0-only 变更），对本项目同样不构成障碍。
> **照抄义务（缺任一不进集成基线）**：① 锁 commit 并读真源码（已核实 `satorijs/satori@c466fb01c5629a0d73f1c01205127b737b6fadd6`）；② 文件头写 `Ported from satorijs/satori@<sha>:<path>` 并保留上游 MIT 版权行；③ 上游 MIT 文本进 `licenses/upstream/`，并在 `licenses/THIRD-PARTY-NOTICES.md` 的照搬来源表登记一行。
> **但 1.0 的范围没有扩大**：白名单只解除法律门槛，不改变阶段取舍。按本文件 §6，当前吸收的是消息/资源引用分离、能力声明与事件信封这几类机制；**禁止**把 Satori 的 Element、Bot/Session 等上游类型或内部数据结构直接变成 Pry 契约（`docs/api-v1.md` 与 PLAT-005 的冻结节奏优先）。细则见 `docs/upstream-license-compatibility.md` §4、§7。

来源：R2（2026-09-15）、R3/R4（2026-09-16），见 [README](README.md)。以下版本、认证和 resume 观察仅对应原研究当时的源码；没有 commit SHA，也未在本次复核最新状态。许可实测日期 2026-09-21，`satorijs/satori` HEAD `c466fb01c5629a0d73f1c01205127b737b6fadd6`，GitHub 判定 **MIT**。

## 1. 定位与五层架构

Satori 统一 Bot 能力、Event 和 Message Representation，并可将远端平台 Bot 映射为本地 Proxy Bot。它不是 LLM、Memory 或 Character Runtime。

原研究沿 monorepo 中 protocol / element / core / server / adapters，以及远程 `adapter-satori`、QQ / Telegram Adapter、boilerplate 追踪。

```text
原生平台 API
↕ Platform Adapter
↕ Satori Core：Bot / Session
↕ Satori Server
↕ HTTP Command + WebSocket Event
↕ adapter-satori：SatoriBot Proxy / Session
↕ 远端应用（未来可为 Pry 边界适配器）
```

官方 TypeScript 宿主使用 Cordis。`@satorijs/core` 是 Cordis Service，Server 依赖 Cordis HTTP / Server / logger 等，`create-satori` 使用 `create-cordis`。可脱离 Koishi，不等于官方实现是无宿主的裸服务器。

## 2. 历史源码观察：核心调用链

### 连接与 Bot 发现

Client 通过 WebSocket 发送 IDENTIFY，携带 token 和上次 sequence number；Server 返回 READY，包含 logins 和 proxyUrls。Login 表达 Bot 的平台、用户、状态与 features；`SatoriAdapter.getBot()` 创建对应本地 `SatoriBot`。

原研究列出的 Opcode 为 EVENT、PING、PONG、IDENTIFY、READY、META；客户端有周期 PING，`WsClientBase` 有 retryTimes / retryInterval / retryLazy。历史 10 秒心跳值不作为 Pry 固定参数。

### 发送

```text
调用远程 SatoriBot 标准方法
→ adapter-satori 动态生成 HTTP RPC
→ POST /v1/message.create 等
   headers：satori-platform、satori-user-id
→ Server 按 platform + selfId 查 Bot
→ bot 对应方法（例如 createMessage）
→ 平台 MessageEncoder / 原生 API
```

### 接收

```text
原生事件 → Platform Adapter → Session
→ Bot.dispatch() → internal/session
→ Server → WebSocket EVENT
→ 远端 adapter-satori 重建 Session → 应用
```

Session 包含 platform、selfId、userId、channelId、guildId、messageId、timestamp、content、quote 等；便捷标识包括 `uid=platform:userId`、`cid=platform:channelId`、`sid=platform:selfId`。它们不能直接替代 Pry 的全局身份体系。

## 3. 能力、富消息与平台特有接口

Bot 根据实现的标准方法生成 `features: string[]`，Login 将其带给远端。可据此判断 message.delete / reaction.create 等能力，不需要仅按平台名字硬编码。能发现方法不等于所有调用在权限、账号或平台限制下都会成功。

Element Tree 表示 text、at、quote、img、audio、video、file、button、strong、code 等。内部 Element[] 可序列化为网络 content 字符串，再 parse 回树。平台 MessageEncoder 承担映射和降级；原研究观察 Telegram Encoder 对格式、媒体、按钮和引用有各自处理，不认识的元素可继续渲染 children。

`bot.internal.xxx(...)` 经 JS Proxy 形成远程 Internal API；`InternalRouter` 允许 Adapter 注册特殊路由。此设计是“标准接口 + 平台特有出口”，不能把特有接口反向泄漏到 Pry Character Domain。

## 4. 图片、上传与资源链路

主设计是消息携带资源引用，图片字节走 HTTP / Upload / Resource Proxy。

```text
接收：平台资源
→ Adapter 生成 internal:... 等资源地址
→ Message Element（例如 img src）随事件传输
→ 客户端经 Gateway 的 /v1/internal/* 取资源

发送本地文件：Client
→ POST /v1/upload.create（multipart/form-data）
→ Server req.formData() → bot.createUpload(...)
→ 使用返回资源地址构造消息 → 平台 Encoder
```

`/v1/proxy/*` 用于代理已注册的 proxyUrls。`internal:` URI 不是任意客户端可以直接当本地文件读取的路径，需要对应资源解析/代理过程。

重要例外：Element 的 `createAssetFactory()` 接收 Buffer / ArrayBuffer 时可以产生 `base64://` 或 data URI。因而 Satori **并非协议上禁止内联图片**，应用使用方式仍可能把大字节塞进 JSON / WebSocket。

R4 给出的排障顺序是分析建议，原对话未提供一份已复现错误：

| 现象/位置 | 原研究建议检查 |
|---|---|
| message.content / img src 极长 | 是否使用 base64 或 data URI，而非资源引用 |
| `/v1/events` 断开、message too big | WebSocket 实现、代理、客户端的消息大小限制 |
| `/v1/upload.create` 返回 413 | 反向代理、HTTP server/parser、上传请求体限制 |
| Adapter 调平台 API 失败 | 目标平台文件限制、原生 API 错误 |
| `/v1/internal/*` 取资源失败 | HTTP 代理、超时、缓冲或下载限制 |

Pry 建议使用 ResourceId / URI 表示普通图片和文件，通过上传或流传输资源；不要因“协议支持 binary 转 Element”就将大图片内联到事件。

## 5. 必须保留的 Caveat

### Alpha 版本

原研究记录的默认分支版本：

| Package | 当时观察值 |
|---|---|
| `@satorijs/core` | `5.0.0-alpha.0` |
| `@satorijs/protocol` | `2.0.0-alpha.0` |
| `@satorijs/plugin-server` | `3.0.0-alpha.0` |
| `@satorijs/adapter-satori` | `2.0.0-alpha.0` |

这不是 2026-09-20 最新版本查询结果。未来接入前需要重新锁定并验证版本兼容。

### Resume：有设计，不等于已证明可靠补发

当时源码存在 sequence number、resumeTimeout、event buffer、遍历 buffer 补发和清理过期项；原研究**未找到正常事件写入 buffer 的代码**。这是待复核疑点，不能改写成“所有版本 resume 都坏了”，也不能删去后宣称无损重连。

不把 Satori 当 Guaranteed Delivery Bus。Pry 若有必须送达任务，应自行设计 Outbox、MessageId、Ack、Retry、TTL 等可靠性策略，并测试重连窗口和重复事件；这些是 Pry 建议，不是 Satori 已提供的保证。

### Auth / internal / proxy 路由

原观察：普通 REST 与 WebSocket IDENTIFY 支持一个可选 Bearer Token；未见完整多租户 token / Bot scope 权限模型；`/v1/internal/*` 与 `/v1/proxy/*` 没有调用前面的 `checkAuth()`。

这只是特定源码路径观察，尚不足以证明所有部署均可被未认证利用；不能省略也不能扩大成已确认漏洞公告。接入前应逐路由验证认证、代理限制与部署外围保护。原 Pry 建议包括 TLS、反向代理/防火墙、额外 Gateway Authentication、GatewayId / ClientId / Scoped Token / Allowed Bot IDs。

### 官方 QQ Adapter 不是个人 QQ 登录

原研究在 `@satorijs/adapter-qq` 看到 QQ Open Platform 的 `bots.qq.com/app/getAppAccessToken`、`api.sgroup.qq.com` 及 app id / secret / token 配置。因此不能理解为“登录普通个人 QQ 账号”。个人 QQ 接入需另行选择并验证实现与 Bridge；原报告提及 NapCat / OneBot11 / Milky 等方向，但没有证明某个组合已经完成端到端兼容。

Bot 登录/session 仍位于实际运行 Adapter 的机器，远程代理不会把登录迁到 Pry.Core，也不保证避免平台下线。

### 网络可达性

Client 主动访问 Server HTTP/WS Endpoint。Satori 不自动解决 NAT / 穿透；Webhook 的出站事件也不替代 Core 调 Bot 所需的命令通道。

## 6. 对 Pry 的建议：阶段与不照搬项

**1.0**：仅吸收富消息/资源分离、能力声明和边界适配思想；不要因此实现 Satori Gateway 或远程平台接入。

**未来**：`IExternalMessenger → Pry.SatoriClient → HTTP/WS → Gateway/Adapters`。适配器将 Satori Event 转为 Pry 自有 ExternalMessage / User / Channel / Capability；领域不依赖 Satori 类型。

**不照搬**：Satori 用户 ID 作为 PryUserId；sn 作为可靠消息保证；单 token 作为 Pry 全账号认证；上游 alpha 类型直接作为 Core 数据结构；把图片 URL 的可见性当作资源授权保证。

**验证入口**：掉线期间事件补发与去重、token 错误/缺失、internal/proxy 路由、能力不支持、资源不可达、大包各层定位、平台特有元素降级。均属后续验证任务，不是本次测试结果。
