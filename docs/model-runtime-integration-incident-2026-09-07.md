# Pry 模型调用集成故障报告（2026-09-07）

面向：`Pry.App` / `Pry.Client` 前端开发者，`Pry.Api` / `Pry.Core` 后端开发者，以及后续接手集成问题的 AI 开发者。

## 1. 执行结论

本次“前端窗口无法连接后端模型回复”并不是 HTTP、DTO 或 SSE 协议断开。前端可以访问内嵌 API，模型调用失败发生在后端启动 `llama-server.exe` 的阶段。

直接原因是系统中残留了 5 个由 Pry 启动、但父进程已经不存在的 `llama-server.exe`。后端尝试再启动同一套 Qwen3.5-9B 多模态模型时，llama.cpp 需要在 CUDA0 上分配约 5080.88 MiB，随后报告：

```text
cudaMalloc failed: out of memory
failed to allocate CUDA0 buffer of size 5327685632
exiting due to model loading error
```

`llama-server.exe` 因此以退出码 1 结束。后端把异常折叠为 HTTP 500 和通用错误，前端又只能显示“消息没有发送”或“模型服务启动失败”，最终给用户造成“前后端没有对接”的印象。

清理 5 个孤儿模型进程后，同一运行库、模型、mmproj、上下文长度、GPU 层数和 CUDA0 参数在约 7 秒内成功加载；直接 OpenAI-compatible 调用返回“诊断成功”，新版窗口的内嵌 API 也能接受回合并写入助手消息。

## 2. 已验证事实

### 2.1 前端与 API 可达

- 当前分支：`codex/frontend-from-scratch`。
- Release 构建：0 警告、0 错误。
- 隔离启动的 `Pry.App` 建立了随机 `127.0.0.1` API 监听端口。
- `/health`、`/api/v1/runtime`、角色和会话接口均可访问。
- 回合提交到 `/api/v1/conversations/{id}/turns` 时能够进入后端控制器和会话服务。

因此，不能把本次故障描述为“前端连不上后端”。准确描述应为“API 已连接，但本地模型进程加载失败”。

### 2.2 后端失败位置

实际调用链为：

```text
PryMessengerWindow.SendAsync
  -> PryBackendClient.SubmitTurnAsync
  -> ChatController.Submit
  -> ConversationSessionService.GetAsync/CreateAsync
  -> BackendRuntime.GetComponentsAsync
  -> ModelProcessRegistry.GetAsync/StartAsync
  -> LlamaServerManager.StartAsync
  -> llama-server.exe 提前退出（exit code 1）
```

后端日志中的最终托管异常为：

```text
System.InvalidOperationException: 本地推理服务提前退出，代码 1。
```

只有直接运行相同的 llama.cpp 命令并读取 stderr，才能看到真正原因是 CUDA 显存分配失败。

### 2.3 清理后的恢复验证

- 精确核对并关闭了 5 个路径属于本项目 `runtime/llama-server.exe` 的孤儿进程。
- 相同模型参数重新启动成功。
- `/health` 返回 200。
- 非流式测试在 512 Token 上限内正常结束并输出“诊断成功”。
- 新版窗口内嵌 API 的回合提交返回 202，并产生用户及助手消息。
- 测试会话、隔离数据库、临时配置和诊断进程均已清理。
- 没有修改用户真实聊天数据库，也没有产生工作区代码改动。

## 3. 为什么现有界面误导用户

当前状态模型混合了三种不同事实：

1. Kestrel/API 是否可以访问；
2. 配置、角色和数据库是否加载成功；
3. 本地推理模型是否已经实际加载并可以生成。

`BackendRuntime.LoadConfigurationAsync` 完成后就把状态设为 `ready`，但模型是首次建立会话或提交回合时才延迟加载。因此启动时的 `ready` 并不等价于“模型已经可用”。

模型启动失败后，`BackendRuntime.Status` 只返回“运行时初始化失败，请查看本机服务日志”。`LlamaServerManager` 又没有保存 llama.cpp 的 stderr，所以普通用户实际上没有可查看的有效本机模型日志。

新版前端虽然区分了“本地 API 已连接”和“模型服务启动失败”，但回合失败仍主要呈现通用 HTTP 500 文案；SSE 的 `turn.failed` 事件也没有被转换为明确、持久的错误提示。

## 4. 前端必须落实的改进

### P0：准确呈现连接状态

前端必须分别显示：

- API 未连接；
- API 已连接、模型尚未加载；
- 模型正在加载；
- 模型已就绪；
- 模型加载失败；
- 模型生成中失败。

只要 `/health` 或其他 API 请求成功，就不得显示“后端连接失败”。模型失败应显示模型错误类别和可执行动作，例如“释放显存后重试”。

### P0：处理失败事件和提交失败

- 显式处理 SSE `turn.failed`。
- 回合 POST 返回失败后再次读取运行时状态，显示后端提供的安全错误码和摘要。
- 失败后必须恢复发送按钮、取消按钮和“正在回复”状态。
- 不要只把错误放在短暂状态栏；需要可复制的诊断编号或详情入口。

### P1：允许用户重试

后端确认资源已经释放后，前端应提供“重新加载模型/重试回复”，不要求用户重启整个应用。重试必须走后端 API，前端不得自行枚举、启动或结束模型进程。

### 前端不得承担的职责

- 不直接读取模型路径、API Key 或 llama.cpp 命令行。
- 不直接判断或清理系统中的 `llama-server.exe`。
- 不根据本机进程列表推断后端状态。
- 不静默降低 GPU 层数或切换模型。

## 5. 后端必须落实的改进

### P0：保留安全、可诊断的模型 stderr

`LlamaServerManager` 不能只保留退出码。应持续异步排空 stdout/stderr，并保存有大小上限的环形日志，或者使用 llama.cpp 支持的日志文件参数写入受控的数据目录。

要求：

- 不能因重定向管道填满而阻塞推理；
- 不能把随机 API Key、用户 Prompt 或完整用户路径返回前端；
- 至少把 `cuda_out_of_memory`、`model_missing`、`runtime_missing`、`invalid_model`、`startup_timeout`、`process_exited` 分类出来；
- HTTP Problem Details 和 `/api/v1/runtime` 返回安全错误码、摘要及 traceId，完整细节留在本地日志。

### P0：明确模型进程所有权

当前进程注册表只在单个后端生命周期内去重，无法管理前一次应用异常退出遗留的实例。必须在以下两种模型中选择并实现清晰语义：

1. **子进程模式**：默认用 Windows Job Object 的 kill-on-close 语义，Pry 异常退出时模型随宿主清理。
2. **常驻模型宿主模式**：把“保留模型服务”实现为独立、可发现、可鉴权的模型宿主，记录 PID、启动时间、端口、模型键和所有权令牌；新后端可以安全复用或请求它退出。

不能继续使用“普通子进程脱离父进程后留在随机端口”的中间状态。启动时也不得仅按进程名批量结束第三方 `llama-server`；只能处理具有 Pry 所有权记录且身份匹配的实例。

### P0：失败后可恢复

`BackendRuntime.GetComponentsAsync` 当前捕获任意异常后把全局状态固定为 `failed`。释放显存后再次提交仍会立即失败，除非完整 reload。后端应提供显式的模型重试/重载操作，清除失败的 Lazy、错误状态和半初始化句柄，同时保证并发请求只触发一次重载。

### P1：拆分运行状态契约

建议把单一 `state` 拆成至少：

- `apiState`
- `configurationState`
- `modelState`
- `activeModelId`
- `errorCode`
- `safeMessage`
- `traceId`
- `retryable`

兼容期可以保留旧 `state` 字段，但前端不应再用它同时表示 API 和模型状态。

### P1：不要用 SSE 请求生命周期拥有全局模型初始化

当前 `EventsAsync` 使用 SSE 请求的取消令牌创建会话，并把同一个令牌一路传到全局模型初始化。切换房间、重建窗口或网络抖动都会取消 SSE。模型加载任务应由后端运行时自己的生命周期令牌拥有；客户端取消只能停止等待或取消订阅，不能把共享模型运行时标记为全局失败。

## 6. 联合验收矩阵

前后端完成修复后必须共同覆盖：

| 场景 | 后端预期 | 前端预期 |
|---|---|---|
| API 未启动 | 请求不可达 | 明确显示 API 未连接 |
| API 可达、模型未加载 | `modelState=not_loaded` | 显示按需加载，不宣称模型就绪 |
| 首次模型加载 | 单一启动任务 | 显示加载进度且允许安全取消等待 |
| CUDA OOM | `cuda_out_of_memory`、可重试 | 显示释放显存/降低 GPU 层数建议 |
| 切换会话时模型加载 | 模型任务继续或由后端安全取消 | 新会话正常订阅，不污染全局状态 |
| 应用正常退出 | 按用户选择停止或移交宿主 | 不留下无主随机端口进程 |
| 应用崩溃/强制结束 | Job Object 清理或常驻宿主接管 | 下次启动可以确定复用或恢复 |
| 文字与视觉选择同一模型 | 注册表只创建一个实例 | 显示同一模型被复用 |
| 释放资源后重试 | 不重启应用即可恢复 | 原消息可重新发送/重新回复 |

## 7. 临时恢复方式

在正式修复进程所有权之前，如果再次出现相同症状：

1. 先确认 `/health` 是否可达，不要立即判定前后端断联。
2. 查看 `/api/v1/runtime` 是否为模型失败。
3. 仅检查由 Pry 启动且路径、父进程、启动记录都能匹配的模型进程。
4. 关闭确认无主的 Pry 模型实例，重新启动应用。
5. 若仍失败，直接捕获 llama.cpp stderr；不要只凭退出码猜测。

不要把“删除工作流、模型文件或用户配置”作为恢复手段，也不要批量结束系统中所有名为 `llama-server` 的进程。

## 8. 与既有报告的关系

本报告是 [`frontend-backend-integration-report.md`](frontend-backend-integration-report.md) 的后续事故记录。前一份报告解决了 UI 启动死锁、内嵌控制器发现和运行库部署；本次报告专门处理模型进程生命周期、可观测性、错误语义与前端状态呈现。

## 9. 二次集成事故：后端已修复但前端分支未包含修复

### 症状

后端完成模型进程所有权、stderr、错误码和重试端点修复后，用户再次启动新版前端，仍然无法获得模型回复。

### 根因

这次不是新的运行时故障，而是分支没有汇合：

- 后端修复位于本地 `main` 的 `3da0c18 fix: own and recover local model processes`。
- 新版前端位于 `codex/frontend-from-scratch` 的 `a0b00ef`。
- 两个分支都从 `bc44a69` 分出；前端分支比共同基线多 10 个提交，后端分支单独多 1 个提交。
- 前端实际构建产物不包含 `3da0c18`，所以“后端已经修好”并不等于“当前正在运行的桌面程序已经使用修复后的后端”。

Pry 当前是单进程内嵌 API。前端与后端虽然在代码职责上分离，但最终必须由同一个 `Pry.App` 构建产物携带兼容的 `Pry.Client`、Contracts、API 和 Core。不能分别启动两个不同分支的 Release 并期待它们自动组合。

### 处理

- 将本地 `main` 合并到 `codex/frontend-from-scratch`。
- 合并提交：`e5d102a merge: integrate model runtime recovery`。
- 自动合并保留了前端自己的 `mainWindowLayoutMode` 契约，同时纳入后端新增的运行状态字段和重试端点。

### 验证

- Release 构建：0 警告、0 错误。
- 自动化测试：Core 14、API 13、Client 3、App 138，共 168 项全部通过。
- 真实 `Pry.App` 内嵌 API：`state=ready`、`modelState=ready`，无 `errorCode`。
- 临时回合提交返回 202，7 秒内收到 Assistant 消息，响应包含“诊断成功”。
- 强制结束测试宿主后，Job Object 自动结束所属 `llama-server`；没有遗留项目模型进程。

### 防复发要求

1. 前后端分别完成的提交必须先进入同一集成分支，再生成供用户测试的 `Pry.App`。
2. 每次交付测试包应显示或记录 Git commit，至少在诊断信息中包含应用、Contracts 和 API 的版本。
3. CI 除单项目测试外，应从目标集成提交构建一次完整桌面产物并执行内嵌 API 冒烟。
4. 报告“后端已修复”时必须同时给出：提交号、所在分支、前端目标分支是否已合并、最终桌面构建是否验证。
5. 用户反馈“又连不上”时，排查顺序固定为：运行产物 commit → 分支拓扑 → API 可达 → `modelState/errorCode` → 模型 stderr → SSE/消息投影。

## 10. 三次事故：模型服务就绪但思考回合没有正文

### 症状与根因

运行状态、模型健康检查和进程父子关系均正常，但真实消息只得到“……”或长时间停留在思考状态。旧配置给 Qwen3.5 的输出上限仅 512 token；思考内容先消耗完预算后，OpenAI-compatible 响应的 `content` 为空。单纯提高到官方推荐的 32K 输出后又暴露两个问题：思考流与 llama.cpp JSON Schema grammar 组合可能迟迟不进入最终正文；262K 上下文虽能在本机加载，实测生成仅约 4.5 token/s。

### 处理

- 内置模型采用官方能力与采样建议作为首选配置，并迁移完全等于旧内置默认值的历史参数。
- 不关闭思考，也不使用极小思考预算伪造响应速度；思考模型改由提示词约束 JSON，非思考模型保留服务端 schema grammar。
- 本地运行时在健康检查后做 8-token 极短基准；生成低于 12 token/s 时整体降低上下文档位。加载失败也使用同一降档序列。
- 回复解析兼容 `{"messages":[...]}`、根数组、字符串消息、缺省 `type` 和 Markdown 围栏。
- Messenger 前端收到 `turn.failed` 时解除发送锁并显示可重试状态。

### 实测

- Qwen3.5-9B Q4 + mmproj 在 RTX 5070 Ti 上可加载 262K，但短基准约 4.5 token/s；自动降到 131K 后约 71.6 token/s。
- 保留完整思考和模型输出上限后，临时内嵌 API 回合产生“思考链路正常”正文；临时对话随后删除。
- Release 构建与 171 项测试全部通过。
