# Pry 前端职责边界审计报告

日期：2026-09-07
审计分支：`codex/frontend-from-scratch`
审计范围：`Pry.App`、`Pry.Client` 的使用方式、两套主窗口、前端服务层，以及 `docs/api-v1.md`、`docs/frontend-backend-boundary-report.md` 约束。
说明：工作区审计时存在其他协作者未提交的后端与模型运行时改动；本报告只评价当前文件快照，不将这些改动认领为前端提交。

## 1. 执行摘要

**总体判断：前后端业务分离主体成立，但前端尚未完全收敛到稳定 DTO/UI 状态边界。**

当前没有发现前端直接读写 SQLite、直接修改后端角色/贴纸清单、直接启动 `llama-server`、自行生成助手回复或写入长期记忆。会话、消息、角色、记忆、贴纸、媒体、模型选择和运行状态的最终权威仍在 API 后端。

审计发现：

- 2 项需要优先整改的边界问题；
- 2 项中期架构债务；
- 3 类合理的前端本机职责，不应误判为越界；
- 1 项经项目文档批准的部署例外。

在整改 P1/P2-1 之前，不建议对外宣称“前端完全只承担展示职责”；更准确的说法是“数据写入已 API 化，旧版投影与部分策略协调仍待收口”。

## 2. 审计基准

本报告采用以下边界：

- 前端负责布局、交互、草稿、焦点、主题预览、文件选择、录音采集、上传前提示、SSE 展示与可取消请求。
- 后端负责数据库、资源归属、策略上限、角色与贴纸持久化、回复规划、模型生命周期、最终校验与事务。
- `Pry.Client` + `Pry.Contracts` + `docs/api-v1.md` 是前端稳定调用面。
- “保存全部设置”依据既有协作规范应调用聚合 `PUT /api/v1/settings`，是否重载模型由后端判断。

## 3. 发现的问题

### P1：新版附件流程未读取后端媒体策略

证据：

- 旧版启动时会调用媒体策略 API，并交给附件草稿服务应用：`src/Pry.App/MainWindow.axaml.cs:149`。
- 新版附件入口直接遍历用户选择的所有文件并上传：`src/Pry.App/PryMessengerWindow.axaml.cs:704`。
- 新版窗口当前没有 `GetMediaPolicyAsync` 调用。

影响：

- 前端无法提前遵守 `maximumAttachmentsPerTurn`；用户可能上传完第 7 个及之后的文件，最终提交才被后端拒绝。
- 大文件警告阈值无法在选择阶段展示。
- 后端仍会最终拒绝，因此数据一致性没有失守，但用户体验与已约定的策略投影边界不一致。

整改建议：新版初始化时读取 `GET /api/v1/media/policy`，只用于 UI 限制和警告；后端继续执行最终校验。不得在新版重新硬编码数量或大小。

### P2-1：旧版设置快速路径让前端判断后端是否需要重载模型

证据：

- `SettingsSaveService.CanUsePreferencesOnly` 在客户端比较模型与媒体状态：`src/Pry.App/Services/SettingsSaveService.cs:48`。
- 旧版主保存按钮根据该比较结果选择偏好接口或聚合设置接口：`src/Pry.App/MainWindow.axaml.cs:1487`。
- 完整保存仍正确使用 `SaveSettingsRequest`：`src/Pry.App/Services/SettingsSaveService.cs:29`。

影响：

- 该快速路径解决了“只切换窗口样式却等待模型重载”的明显卡顿，但把“哪些变化要求运行时重载”的策略判断放到了前端。
- 它与现有边界报告中“主保存按钮必须使用聚合设置接口”的约定不一致。
- 后端未来增加影响重载的新字段时，前端比较函数可能漏判，形成协议隐式耦合。

整改建议：保留当前实现作为短期兼容，但由后端让 `PUT /api/v1/settings` 对无模型变化请求执行轻量刷新，或提供明确的 `reloadRequired/restartScope` 契约。后端具备该能力后删除 `CanUsePreferencesOnly`，前端不再推断运行时成本。

### P2-2：经典前端仍依赖 Core 领域模型作为视图状态

证据：

- `Pry.App` 直接引用 `Pry.Core`：`src/Pry.App/Pry.App.csproj:15`。
- 经典窗口持有 `CharacterDefinition`、`StickerDefinition`、`UserPreferences` 等 Core 类型：`src/Pry.App/MainWindow.axaml.cs:59-67`。
- `BackendProjectionService` 把 API DTO 重新组装为 Core 领域对象：`src/Pry.App/Services/BackendProjectionService.cs:19-30`、`:68-75`。
- 角色列表启动时逐个请求完整角色：`src/Pry.App/Services/BackendProjectionService.cs:22`。

影响：

- 没有绕过 API 写数据，因此不是数据层越权；但 UI 与 Core 内部模型形状绑定，后端领域模型调整会直接传播到前端。
- DTO → Core → UI 的二次投影增加映射遗漏风险；例如此前窗口模式字段就曾在投影中丢失。
- 逐角色详情请求和逐贴纸媒体下载增加经典窗口启动成本。

整改建议：新版继续只持有 Contracts DTO 或前端专用 ViewModel；经典窗口逐步把 `BackendProjectionService` 输出改为 `Pry.App.ViewModels`，最终移除对 `Pry.Core.Inference/Memory/TurnTaking` 命名空间的直接使用。不要为此一次性重写经典窗口。

### P3：新版窗口仍是过大的协调器

证据：

- `PryMessengerWindow.axaml.cs` 同时管理会话列表、消息渲染、SSE、草稿、附件、语音、贴纸、角色选择、设置、窗口生命周期和快捷键。
- 该文件还直接完成媒体下载与位图解码，例如背景、头像和消息媒体：`src/Pry.App/PryMessengerWindow.axaml.cs:122`、`:146`、`:339`。

影响：

- 这不是后端职责越界，但已经越过合理的页面协调器体量，后续容易出现竞态、事件未解绑和多人编辑冲突。
- 两套窗口重复实现 API 状态处理，行为可能继续分叉。

整改建议：以小步方式抽取 `ConversationDraftStore`、`ConversationListController`、`MessageTimelineController`、`MediaPreviewLoader` 和 `ShortcutRouter`；页面只编排视图状态。抽取对象不得持久化业务数据或复制后端规则。

## 4. 未发现的越界

以下高风险行为在当前前端中未发现：

- SQLite 或其他数据库客户端依赖；
- `MemoryDatabase`、`JsonConfiguration.SaveAsync` 等后端持久化调用；
- `Process.Start`、`LlamaServerManager` 等模型进程控制；
- 前端写入助手消息或长期记忆数据库；
- 前端直接读取模型目录、GGUF 路径或 API Key；
- 为会话列表逐会话请求消息以计算最后消息。新版已使用后端 `lastMessagePreview/lastMessageAt` 投影。

## 5. 合理的前端本机职责

以下文件操作属于客户端设备交互，不构成业务越界：

- 用户通过文件选择器选择头像、背景、贴纸和附件后，前端打开文件流并上传；例如 `PryMessengerCharacterWindow.cs:123`、`PryMessengerStickerWindow.cs:93`。
- 录音写入系统临时目录，上传完成后删除；不作为业务数据源。
- `BackendMediaCache` 下载 API 资源到临时缓存用于 Avalonia/Skia 解码，并在释放时清理；缓存不是权威数据。
- 会话输入文字与附件草稿按会话保存在前端进程内；关闭应用后不持久化。
- 快捷键语法和表单范围在前端提前验证；后端仍执行最终校验。

## 6. 部署例外：Pry.App 内嵌 Pry.Api

`Pry.App` 项目引用 `Pry.Api`（`Pry.App.csproj:18`），并在 `App.axaml.cs:40` 创建 ASP.NET Core 宿主。这在严格物理分层上意味着 UI 可执行项目承担后端宿主生命周期，但依据现有集成报告，这是 v0.0.1 明确采用的“单进程、逻辑前后端分离”部署方案。

因此本报告不把它判定为当前缺陷，但要求：

- 所有业务调用仍必须通过回环 HTTP 和 `PryBackendClient`；
- 窗口切换不得重建后端；
- 未来拆为独立进程时，应把宿主启动迁出 UI 项目，并补端口发现、认证、版本协商和进程所有权协议。

## 7. 整改优先级

1. **立即处理**：新版接入媒体策略 API，补附件数量与大文件提示测试。
2. **后端配合后处理**：让聚合设置接口跳过不必要的模型重载，随后删除前端 `CanUsePreferencesOnly` 策略判断。
3. **持续重构**：新版主窗口按 UI 控制器拆分，降低单文件协调复杂度。
4. **经典窗口维护期处理**：用前端 ViewModel 替换 Core 领域对象投影；不阻塞新版验收。

## 8. 验收清单

- 新版选择超过后端允许数量的附件时，在上传前阻止并说明限制来源。
- 仅切换窗口样式时，仍调用聚合设置接口且后端不重载模型。
- 两套界面均不读取数据库、模型目录或后端配置文件。
- 会话列表只消费服务端最后消息投影，无 N+1 消息请求。
- 角色、贴纸、记忆写操作全部经过 `PryBackendClient`。
- UI 控制器拆分后不引入新的本地业务持久化层。

## 9. 最终结论

项目已经实现“后端是业务数据与模型运行权威、前端通过 API 操作”的核心目标。当前主要问题不是重新混入数据库或 AI 推理，而是**前端仍在复制少量后端策略判断，并且经典 UI 继续依赖 Core 领域模型**。

建议将当前状态评定为：**逻辑分离合格，边界收口未完成**。完成 P1 和 P2-1 后，可提升为“前端职责基本清晰”；完成经典投影去 Core 化和新版窗口拆分后，才适合宣称两套 UI 都可以低成本独立替换。

## 10. 后端修复后的复核附录

复核日期：2026-09-07

复核说明：原报告提交 `5b1d61b` 早于同日后端边界治理和显卡自动调优改动。本附录以当前 API 契约重新标记前端待办；前文历史证据保留。

### 10.1 权威边界

本项目以后端为业务、数据和运行策略的主要权威：

- 后端负责持久化、事务、资源归属、最终校验、附件策略、模型选择、硬件分档、上下文降档、模型进程生命周期、回复规划和稳定错误码。
- 前端负责窗口、布局、渲染、本地化文案、文件选择、录音采集、临时草稿、上传前提示，以及 API/SSE 状态展示。
- 前端可以依据后端投影提前改善交互，但不得复制后端常量、推断是否应重载模型、调用 `nvidia-smi`、直接管理 `llama-server`，或将本地缓存作为业务权威。
- 出现契约缺口时，先补后端 DTO/API/文档，再由前端消费；不得在前端建立第二套业务实现。

### 10.2 前端必须整改

#### FEA-001：新版附件入口消费媒体策略

状态：已完成。新版窗口初始化时读取策略；策略不可用时禁止上传并提供可恢复提示，数量限制与大文件提醒均直接使用服务端投影。

- 初始化或首次打开附件入口前调用 `GET /api/v1/media/policy`。
- 使用 `maximumAttachmentsPerTurn` 在上传前限制数量。
- 使用 `warningThresholdBytes` 提示大文件；`maximumBytes=null` 表示没有硬性文件大小上限，不得自行增加上限。
- 请求失败时采用保守交互并显示可恢复错误，不能硬编码当前后端的 `6` 和 `10 MiB`。
- 后端仍执行最终签名、类型、数量和资源归属校验。

#### FEA-002：删除前端模型重载策略判断

状态：已完成。经典与新版设置均统一调用聚合接口，前端不再比较字段推断模型是否重载，并使用 `runtimeAction` 展示保存结果。

- `SettingsSaveService.CanUsePreferencesOnly` 和旧窗口按字段选择保存接口的逻辑属于后端策略复制。
- 所有“保存全部设置”应调用 `PUT /api/v1/settings`。后端现在比较文字/视觉模型选择与模型调参：无模型变化时返回 `runtimeAction=settings_applied` 并保留现有模型进程；需要重载时返回 `models_reloaded`。
- 删除 `CanUsePreferencesOnly`、相关字典比较和旧窗口保存分支；前端只使用成功响应刷新展示状态。

#### FEA-003：按最后消息类型本地化会话副标题

状态：已完成。正文优先，空正文的图片和表情分别显示本地化占位，四项投影为空时显示空会话提示；没有新增消息列表请求。

后端不再返回 `[图片]`、`[表情]` 等展示文案。前端必须按以下顺序渲染：

1. `lastMessagePreview` 非空：显示后端返回的实际正文摘要；
2. 摘要为空且 `lastMessageKind=image`：显示前端本地化的“图片”；
3. 摘要为空且 `lastMessageKind=sticker`：显示前端本地化的“表情”；
4. `lastMessagePreview`、`lastMessageRole`、`lastMessageKind`、`lastMessageAt` 均为空：显示“还没有消息”。

不得为生成副标题逐会话请求消息列表或恢复前端摘要缓存。当前由 `ConversationPreviewText` 统一处理展示语义，并覆盖图片、表情、正文和空会话测试。

#### FEA-004：只消费后端显卡分档结果

状态：新增可选展示能力，不阻塞基本聊天。

`GET /api/v1/runtime/compute-devices` 已新增：

- `totalMemoryMiB`
- `freeMemoryMiB`
- `performanceTier`（1–5）
- `performanceTierId`
- `recommendedContextSize`

前端可显示这些字段和本地化档位说明，但不得维护独立显卡型号表、读取驱动工具、修改分档阈值或自行启动性能测试。实际模型运行结果继续读取 `GET /api/v1/runtime` 的 `requestedContextSize`、`effectiveContextSize`、`measuredTokensPerSecond` 和 `modelAdjustmentReason`。

#### FEA-005：停止维护旧前端运行资源副本

状态：已完成构建引用迁移。`Pry.App.csproj` 已改为从中立的 `src/Pry.Resources` 复制发布内容，前端代码没有读取旧目录。

后端和桌面发布现在统一从 `src/Pry.Resources` 复制运行配置、内置角色和贴纸清单。`Pry.App/Resources` 不再是运行配置权威来源；前端不得将配置重新迁回该目录，也不得依赖其中的旧副本。

### 10.3 后续维护项

- 经典窗口逐步以 Contracts DTO 或前端 ViewModel 替代 Core 领域对象，不进行一次性大改。
- 产品边界决定：`PryMessengerWindow` 继续协调草稿、消息时间线渲染、媒体预览和快捷键，这些属于窗口表现职责，不因文件规模机械拆分。消息历史、权威顺序和时间线持久化仍由后端负责；若以后出现可独立测试或复用的明确收益，再做局部提取。
- `Pry.App` 内嵌 `Pry.Api` 仍是当前部署方式；窗口重建只能重建 UI，不得顺带停止或重启后端与模型进程。

### 10.4 前端整改验收

- 新版附件数量和大文件提醒完全来源于媒体策略响应。
- 图片、表情和空会话的副标题分别正确显示，且没有 N+1 消息请求。
- 前端不存在 `CanUsePreferencesOnly` 或同类模型重载推断；设置保存统一调用聚合接口并可展示 `runtimeAction`。
- 前端没有 `nvidia-smi`、`Process.Start`、`LlamaServerManager`、SQLite 或后端资源目录访问。
- 所有业务响应仍以 `Pry.Client`、`Pry.Contracts` 和 `docs/api-v1.md` 为唯一稳定调用面。
