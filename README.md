# Pry 本地虚拟陪伴助手

Pry 是一个 Windows 优先、本地优先的虚拟陪伴应用。当前版本提供 Messenger 与经典卡片两套 Avalonia 桌面界面、SQLite 对话与长期记忆、可编辑角色卡、附件和表情包、OpenAI-compatible 流式模型、本地 llama.cpp 文字/图片理解以及本地或在线语音识别。

## 当前状态

- 当前正式版本为 `v0.0.2`，项目已从最小原型进入可分发预览阶段。
- 业务数据、模型生命周期和服务端策略由内嵌 `Pry.Api` 负责；桌面端通过 `Pry.Client` 使用 HTTP/JSON/SSE，不直接读写 SQLite 或管理模型进程。
- Messenger 与经典窗口的集中人工验收已经完成，不再把历史上的笼统“待人工验收”视为当前阻塞项。后续发现的视觉、DPI、交互或设备问题会按独立可复现缺陷处理。
- 当前开发版已经接入正式 Pry 图标；现有 v0.0.2 下载包早于该改动，下一补丁版本重新打包后才会携带新图标。
- 尚未完成的主要工程项是统一下一发行基线、正式安装器与签名、长对话虚拟化、诊断导出和桌宠渲染。

当前任务和发行基线见 `PROJECT_PLAN.md`。开发者应先阅读 [前后端文档索引](docs/README.md)；已经发生过的集成故障及防复发规则见 [工程踩坑记录](docs/engineering-pitfalls.md)。

## 从源码构建

### 开发环境

- Windows 10/11 x64。当前为 Windows 优先开发，Linux/Android 只保留架构扩展空间。
- [Git](https://git-scm.com/download/win)。
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)。源码构建需要 SDK，只运行 `minimal` Release 则只需 ASP.NET Core Runtime。
- PowerShell 7 建议用于运行仓库脚本。

克隆仓库并还原依赖：

```powershell
git clone https://github.com/LemPhyth/Pry_Project.git
Set-Location Pry_Project
dotnet restore Pry.slnx
dotnet build Pry.slnx -c Release
dotnet test Pry.slnx -c Release --no-build
```

所有自动化测试位于 `tests/`，分别覆盖 Core、API、HTTP/SSE 客户端和桌面 UI 逻辑。模型、运行库、数据库和用户素材不会进入 Git。

普通构建和 CI 不依赖模型权重。若本机已经安装 SenseVoice，并希望额外验证真实离线识别链路，可显式指定模型目录后运行对应集成测试：

```powershell
$env:PRY_TEST_SENSEVOICE_MODEL_DIR = (Resolve-Path 'models/sensevoice-small-int8').Path
dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter Configured_sense_voice_model_can_run_offline
Remove-Item Env:PRY_TEST_SENSEVOICE_MODEL_DIR
```

### 启动桌面应用

```powershell
dotnet run --project src/Pry.App/Pry.App.csproj
```

`Pry.App` 是普通使用时唯一需要启动的程序。它会在当前桌面进程中自动启动一个仅监听随机回环端口的内嵌后端，再由 `Pry.Client` 连接；不需要另外启动 `Pry.Api`。

默认会读写 `%LOCALAPPDATA%\PryCompanion`。源码调试时如果不想使用日常聊天数据，可以先覆盖后端数据目录：

```powershell
$env:Pry__DataDirectory = Join-Path $PWD 'data/dev'
dotnet run --project src/Pry.App/Pry.App.csproj
```

`data/` 已被 Git 忽略。该设置至少会隔离 SQLite、偏好、角色和后端托管媒体；调试结束后可以移除当前 PowerShell 会话中的环境变量：

```powershell
Remove-Item Env:Pry__DataDirectory
```

### 安装本地推理环境

仓库不包含 llama.cpp 和模型权重。需要已验证的 CPU 入门组合时运行：

```powershell
# llama.cpp CPU 运行时 + Qwen3-1.7B Q4_K_M
.\scripts\install-local-runtime.ps1

# Hugging Face 主站连接困难时
.\scripts\install-local-runtime.ps1 -UseMirror

# 仅安装 llama.cpp，自行提供模型
.\scripts\install-local-runtime.ps1 -RuntimeOnly
```

脚本会将运行库放入 `runtime/`、模型放入 `models/`，并核对 SHA-256。自备 llama.cpp/CUDA 或模型时，请按 `src/Pry.Resources/appsettings.json` 中的相对路径放置，或在应用设置中新建自定义模型。

### 项目结构

| 目录 | 职责 |
|---|---|
| `src/Pry.App` | Avalonia 桌面窗口、本机交互与 UI 投影 |
| `src/Pry.Api` | 本地 HTTP/SSE 后端、配置与应用服务 |
| `src/Pry.Client` | 桌面端共用的 HTTP/SSE 客户端 |
| `src/Pry.Contracts` | 前后端传输约定 |
| `src/Pry.Core` | 对话、记忆、SQLite 和推理基础能力 |
| `src/Pry.Resources` | 内置角色、模型、贴纸等运行配置源 |
| `tests` | xUnit 自动化测试 |
| `scripts` | 仓库校验、环境安装和 Release 打包 |

所有前后端开发从 [文档索引](docs/README.md) 开始；详细边界见 [项目架构](docs/architecture.md)，HTTP 约定见 [API v1](docs/api-v1.md)。

## 选择 Release 发行包

v0.0.2 提供两种 Windows x64 ZIP，都不包含模型权重，也不包含聊天记录、用户素材、密钥或日志。

| 发行包 | 包含内容 | 使用前需要 | 适合用户 |
|---|---|---|---|
| `Pry-v0.0.2-win-x64-lite.zip` | Pry、.NET/ASP.NET Core 运行时、CUDA llama.cpp 本地推理运行库 | 解压后直接启动；本地推理需另配模型 | 希望少安装环境、接受较大下载的用户 |
| `Pry-v0.0.2-win-x64-minimal.zip` | Pry 程序与必要应用依赖 | [.NET 10 ASP.NET Core Runtime x64](https://dotnet.microsoft.com/download/dotnet/10.0)；本地推理再安装 llama.cpp 和模型 | 已有运行环境、使用在线兼容 API，或希望自行管理依赖的用户 |

两种包解压后都从 `Pry.App.exe` 启动。`minimal` 启动时如提示缺少 `Microsoft.NETCore.App` 或 `Microsoft.AspNetCore.App` 10.x，请安装上表链接中 Windows x64 的 ASP.NET Core Runtime；不需要安装整套 .NET SDK。

使用本地模型时，在解压目录打开 PowerShell：

```powershell
# 安装已验证的 CPU llama.cpp 运行时和 Qwen3-1.7B 入门模型
.\scripts\install-local-runtime.ps1

# 只安装 llama.cpp，模型由用户自行配置
.\scripts\install-local-runtime.ps1 -RuntimeOnly
```

使用 OpenAI-compatible 在线服务时，可以跳过 llama.cpp 和本地模型安装，直接在应用设置中添加兼容服务。

当前两种包都是“免安装 ZIP”，不是安装器。程序文件可随解压目录删除，用户数据仍统一保存在 `%LOCALAPPDATA%\PryCompanion`，以便更新版本时继承聊天记录。便携数据模式计划在后续版本设计。

### Release 中为什么有很多 DLL

.NET/Avalonia 应用在发布时会把功能按程序集和原生库拆分，因此解压后看到很多 DLL 是正常的，不代表每个文件都是一个独立软件。

- `Pry.*.dll` 是 Pry 的前端、后端、客户端和核心代码。
- `Avalonia.*`、`SkiaSharp`、`HarfBuzzSharp` 等负责桌面 UI、文字和图形渲染。
- `Microsoft.Data.Sqlite`、`e_sqlite3` 负责本地数据。
- `onnxruntime`、`sherpa-onnx` 负责本地语音识别。
- `System.*`、`coreclr`、`hostfxr` 等为 `lite` 自包含版携带的 .NET 运行时。
- `runtime/` 中的 `llama`、`ggml`、`cublas`、`cudart` 等用于本地 CPU/GPU 推理，是自包含版体积的主要来源。

不要逐个删除看似用不到的 DLL；部分功能只在打开设置、语音识别或启动模型时动态加载，缺失后可能到那时才报错。需要小体积时应直接选择 `minimal` 包。

### 在本机生成 Release ZIP

```powershell
# 先执行 Release 构建、完整测试和仓库检查，再生成 lite + minimal
.\scripts\package-release.ps1 -Version 0.0.2
```

输出位于 `artifacts/releases/v0.0.2/`。默认不生成完整模型包；模型权重体积很大，而且需要单独审核许可与分发渠道。

独立后端用于 API 调试或其他客户端接入，本身没有桌面窗口：

```powershell
dotnet run --project src/Pry.Api/Pry.Api.csproj
```

独立后端默认仅监听 `http://127.0.0.1:5078`，复用原有 `%LOCALAPPDATA%/PryCompanion/memory.db`。不要在桌面程序运行时再启动独立后端并同时操作同一数据目录。接口约定见 [API v1](docs/api-v1.md)，拆分边界与迁移顺序见 [项目架构](docs/architecture.md)。

跨边界 DTO 位于 `Pry.Contracts`，桌面端和未来桌宠共用的 HTTP/SSE 客户端位于 `Pry.Client`。当前桌面业务读写已经统一通过 API；不得重新引入客户端直写 SQLite 或配置文件的路径。

应用数据保存在 `%LOCALAPPDATA%/PryCompanion/memory.db`。角色定义和模型配置位于输出目录的 `Resources` 中；正式角色内容尚需作者填写。

## 模型配置与安全

将 `llama-server.exe`、GGUF 模型和匹配的 `mmproj` 放入配置指定位置；应用会自动启动当前文字模型的本地服务并等待模型就绪。仓库和默认 Release 不会携带或自动下载大型模型。在线服务可通过兼容 `/v1/chat/completions` 的配置接入；API Key 从环境变量 `PRY_API_KEY_<模型ID>` 读取，不写入 JSON。

当前入门安装脚本使用以下已校验组合：

- llama.cpp `b10516` Windows x64 CPU
- `Qwen3-1.7B-Q4_K_M.gguf`
- OpenAI-compatible 流式接口

应用会为本地服务生成一次性随机 API Key，并只监听 `127.0.0.1`，避免其他网页直接调用本机模型。本地模型会按显卡能力和真实启动情况自动选择或降低上下文档位；不建议只为追求标称上限而强行设置超出本机能力的参数。

## 当前边界

- 已实现：两套桌面窗口、流式聊天、消息分支与撤销、附件、角色卡、长期记忆、表情包、主题、快捷键和用户资料。
- 已实现：本地/在线文字模型、独立视觉模型或原生多模态模型、本地/在线语音识别、模型进程复用与故障恢复。
- 当前部署仍是 `Pry.App` 单进程内嵌 `Pry.Api`；普通用户只启动 `Pry.App.exe`。退出桌面程序会同时关闭内嵌后端及其拥有的模型进程。
- API 只支持本机回环单用户模式。任何局域网或公网访问都必须先增加认证、授权、TLS、限流和来源策略。
- 开发者可在本机安装模型、SenseVoice/sherpa-onnx 和 CPU/CUDA llama.cpp；这些大型本机文件不进入 Git。
- 尚未完成：正式安装器和签名、长对话虚拟化、诊断导出、完整桌宠渲染及最终角色美术素材。

## 许可与素材权利

Pry 的程序源代码及普通项目文档使用 [Apache License 2.0](LICENSE)。该许可允许使用、修改、分发和商业利用代码，但要求遵守协议中的版权、许可和通知义务。

Apache License 2.0 **不适用于** Pry 的原创角色设计、立绘、头像、桌宠动画、Logo、音频及其他美术素材。原创素材版权归作者所有；个人可以按照 [原创素材许可](ASSETS_LICENSE.md) 和 [非商业二次创作政策](FAN_CONTENT_POLICY.md) 进行非商业二次创作。商业使用、原始素材再分发及其他超出许可范围的行为需要事先取得书面授权。

模型、运行库、字体和第三方素材适用各自许可证，详情参见 [第三方声明](licenses/THIRD-PARTY-NOTICES.md)。

版本变更见 [CHANGELOG](CHANGELOG.md)。
