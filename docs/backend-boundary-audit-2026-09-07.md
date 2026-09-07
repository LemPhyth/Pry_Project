# Pry 后端职责边界审计报告

日期：2026-09-07

审计分支：`codex/frontend-from-scratch`
审计基线：`fbb755d`，同时审阅工作区尚未提交的模型自适应与新版前端改动

## 1. 结论

当前后端总体职责方向正确：会话、消息、长期记忆、角色、媒体、模型生命周期、推理、语音识别、配置校验和持久化均由后端负责；桌面端主要通过 `Pry.Client` 调用 HTTP/JSON/SSE。没有发现后端直接操作 Avalonia 控件、窗口布局、托盘、录音设备或文件选择器等严重前端职责越界。

本次发现的 4 项问题均已在同日修复：2 项前后端边界污染和 2 项后端内部职责混杂已经收敛。没有 P0 阻断项。

## 2. 审计范围与判定原则

审阅范围包括：

- `Pry.Api` 的 Controllers、Application Services、运行时和媒体服务；
- `Pry.Core` 的数据库、模型进程、推理、回复规划与领域模型；
- `Pry.Contracts`、`Pry.Client` 的依赖方向和公开 DTO；
- `Pry.App` 是否重新直接访问 SQLite、后端配置或模型进程；
- 2026-09-07 尚未提交的模型自动降档实现；
- `GET /api/v1/conversations` 的最后消息投影。

判定原则：后端可以保存和校验稳定的用户偏好，但不能决定控件结构、视觉文案或窗口行为；前端可以采集文件、录音和输入，但不能直接读写后端数据或管理模型进程。

## 3. 发现项与修复结果

### [P1] BE-BND-001：后端构建反向读取前端资源目录

证据：`src/Pry.Api/Pry.Api.csproj` 使用：

```xml
<Content Include="../Pry.App/Resources/**" Link="Resources/..." />
```

影响：

- `Pry.Api` 的可构建性依赖 `Pry.App` 的目录结构；
- 独立 API 发布物与桌面发布物不能分别定义资源所有权；
- 前端移动或重做资源目录可能无意中破坏后端启动；
- 形成 `Pry.App -> Pry.Api` 项目引用和 `Pry.Api -> Pry.App/Resources` 文件依赖的隐性环。

修复：资源源文件迁入中立的 `src/Pry.Resources`；Api 与 App 分别在构建时复制，后端不再读取前端目录。

### [P1] BE-BND-002：会话摘要包含后端生成的中文展示文案

证据：`MemoryDatabase.ListConversationsAsync` 和 `GetConversationAsync` 在 SQL 中直接生成 `[图片]`、`[表情]`，并写入 `LastMessagePreview`。

影响：

- 数据访问层承担本地化和 UI 文案职责；
- 未来英文界面、无障碍文案或不同客户端无法自行表达；
- `lastMessagePreview` 同时表示原始摘要和展示占位，字段语义不稳定。

修复：后端只返回经过长度限制和换行归一化的实际文本、`lastMessageKind`、`lastMessageRole`、`lastMessageAt`；媒体无正文时 `lastMessagePreview=null`。单查询投影保留。

### [P1] BE-INT-001：模型性能策略混入进程启动器

证据：工作区版本的 `LlamaServerManager.StartAsync` 在健康检查后直接发送推理基准，并以固定 `12 token/s` 判定失败；`ModelProcessRegistry` 同时持有固定上下文档位序列和降档循环。

这不是前端职责越界：设备选择、模型加载与自动降档应由后端负责。但当前是后端内部职责混杂：

- 进程管理器同时负责启动、健康检查、性能基准和产品体验阈值；
- 注册表同时负责实例复用和配置搜索策略；
- 实际采用的上下文档位没有进入稳定运行状态投影；
- 只按启动短基准调整，并未根据 CPU、内存、显存容量预选档位，也没有长期回复延迟统计；
- 所有可重试启动错误都可能触发逐档尝试，网络或运行库瞬态错误会造成不必要的重复启动。

修复：新增 `ModelPerformancePolicy` 管理候选档位、速度阈值和可降档错误白名单；`LlamaServerManager` 仅启动、健康检查和测量。只有 `cuda_out_of_memory`、`memory_allocation_failed`、`context_performance_low` 会降档。运行状态新增 requested/effective context、基准速度和原因；活动回合中不会重启模型。

### [P2] BE-INT-002：部分 Controller 绕过应用服务直接访问持久化

证据：`FoldersController` 直接注入 `MemoryDatabase` 并执行校验、创建、重命名和删除；`MediaController` 直接注入 `MediaAssetStore`。

影响：控制器承担业务校验与用例编排，后续事务、审计日志、授权或幂等规则容易散落在 HTTP 层。`MediaAssetStore` 当前兼具仓储与应用服务语义，边界也不够明确。

修复：新增轻量 `ConversationFolderApplicationService`，Controller 只负责 HTTP 转换。`MediaAssetStore` 暂作为受管媒体应用门面保留，不新增重复抽象或大型依赖。

## 4. 未发现越界的部分

- `Pry.Core` 没有 Avalonia 或 ASP.NET Core 依赖；SQLite、模型客户端和本地推理进程属于当前项目定义的核心/基础能力。
- `Pry.Api` 没有引用 Avalonia，也不直接操纵桌面窗口或托盘。
- `Pry.Client` 只依赖 Contracts，不访问 SQLite、模型目录或进程。
- 当前新版前端没有实例化 `MemoryDatabase` 或启动 `llama-server`；文件读取主要用于用户主动选择的上传、预览、录音临时文件和客户端媒体缓存，属于客户端本机交互职责。
- 后端保存 `theme.mainWindowLayoutMode`、颜色和尺寸偏好不构成越界；后端只保存、校验和投影稳定值，具体控件树与渲染仍由前端决定。
- Windows Job Object、stderr 分类、模型重试、上下文自动降档和最后消息单查询投影均应由后端负责。

## 5. 依赖方向评估

当前显式项目引用为：

```text
Pry.App -> Pry.Api / Pry.Client / Pry.Contracts / Pry.Core
Pry.Api -> Pry.Contracts / Pry.Core
Pry.Client -> Pry.Contracts
Pry.Contracts -> Pry.Core
Pry.Core -> 无项目引用
```

资源修复后已经不存在 Api 从 App 目录复制文件的反向依赖。剩余结构性风险是 Contracts 大量复用 Core 领域对象，以及 App 因内嵌宿主直接引用 Api。短期单进程部署允许 `App -> Api`；中期建议让 Contracts 拥有真正的传输 DTO，逐步减少客户端对 Core 的直接依赖。该迁移应按端点逐步完成，不应一次性重写全部契约。

## 6. 前端接手说明

1. 会话副标题继续只读 `GET /api/v1/conversations`，不要为了摘要打开会话或请求消息列表。
2. 当 `lastMessagePreview` 有值时显示正文；为空且 `lastMessageKind=image` 时由前端本地化为“图片”，`sticker` 本地化为“表情”；四个字段都为空才是空会话。
3. 模型状态只读 `GET /api/v1/runtime`。可展示 `effectiveContextSize` 与 `measuredTokensPerSecond`，但前端不得依据硬件自行改上下文、启动/杀死 `llama-server` 或实现第二套降档循环。
4. `modelAdjustmentReason` 是机器可读稳定码；显示文案应由前端本地化。字段为 `null` 表示未降档或尚未加载。
5. `src/Pry.Resources` 是共享发布内容，不属于 UI；前端不要把运行配置搬回 `Pry.App/Resources`。

## 7. 验证状态

已新增媒体无正文摘要、模型策略错误白名单及系统内存不足分类测试。Release 构建 0 警告、0 错误；Core 17、API 15、Client 3、App 140，共 175 项测试通过；`git diff --check` 通过。

## 8. 总体判定

后端没有整体性职责越界，核心数据权威和模型所有权仍在正确一侧；当前问题属于少量展示语义泄漏、资源所有权反向耦合和后端内部服务职责过宽。按上述顺序治理即可，不需要改成微服务、不需要更换数据库或 ORM，也不应把模型调优和会话摘要重新交给前端。
