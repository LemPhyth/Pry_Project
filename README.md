# Pry 本地虚拟陪伴助手原型

这是一个 Windows 优先、可扩展到 Linux/Android 的本地虚拟陪伴助手最小原型。当前包含 Avalonia 桌面 UI、固定角色定义、SQLite 对话与长期记忆、Prompt Builder、OpenAI-compatible 流式模型接口、图片消息入口、可管理的表情包库，以及 Embedding、图片生成、语音识别、Live2D 和 Agent 工具占位接口。

## 从源码运行

```powershell
dotnet restore Pry.slnx
dotnet run --project src/Pry.App/Pry.App.csproj
```

`Pry.App` 是普通使用时唯一需要启动的程序。它会在当前桌面进程中自动启动一个仅监听随机回环端口的内嵌后端，再由 `Pry.Client` 连接；不需要另外启动 `Pry.Api`。

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

独立后端用于 API 调试或其他客户端接入，本身没有桌面窗口：

```powershell
dotnet run --project src/Pry.Api/Pry.Api.csproj
```

独立后端默认仅监听 `http://127.0.0.1:5078`，复用原有 `%LOCALAPPDATA%/PryCompanion/memory.db`。不要在桌面程序运行时再启动独立后端并同时操作同一数据目录。接口约定见 [API v1](docs/api-v1.md)，拆分边界与迁移顺序见 [后端架构](docs/backend-architecture.md)。

跨进程 DTO 位于 `Pry.Contracts`，桌面端和未来桌宠共用的 HTTP/SSE 客户端位于 `Pry.Client`。迁移完成前请勿同时使用旧桌面业务路径和 API 修改同一会话。

应用数据保存在 `%LOCALAPPDATA%/PryCompanion/memory.db`。角色定义和模型配置位于输出目录的 `Resources` 中；正式角色内容尚需作者填写。

## 接入本地模型

将 `llama-server.exe`、GGUF 模型和匹配的 `mmproj` 放入配置指定位置；应用会自动启动当前文字模型的本地服务并等待模型就绪。原型目前不会自动下载大型模型。在线服务可通过兼容 `/v1/chat/completions` 的配置接入；API Key 从环境变量 `PRY_API_KEY_<模型ID>` 读取，不写入 JSON。

当前开发工作区已验证以下固定组合：

- llama.cpp `b10516` Windows x64 CPU
- `Qwen3-1.7B-Q4_K_M.gguf`
- 4K 上下文、思考模式关闭、OpenAI-compatible 流式接口

应用会为本地服务生成一次性随机 API Key，并只监听 `127.0.0.1`，避免其他网页直接调用本机模型。

在新的工作区中可运行以下脚本安装同一套经过验证的资源：

```powershell
.\scripts\install-local-runtime.ps1
```

如果 Hugging Face 主站连接困难，可使用 `-UseMirror`；无论下载源为何，脚本都会按官方 LFS SHA-256 校验模型，校验失败不会安装。

## 当前边界

- 已实现：桌面聊天界面、流式请求、图片上传、SQLite 历史、简单长期记忆、角色定义校验、模型能力路由。
- 已实现：内置与用户表情包目录、导入/编辑/删除、受约束的模型自主选择协议，以及与 Live2D 共用的情绪表现指令。
- 接口已预留：Embedding、图片生成、语音识别、Live2D、Agent 工具。
- 当前开发目录已包含本地模型、SenseVoice/sherpa-onnx 语音识别，以及可同时运行文字与图片模型的 CUDA llama.cpp 运行时。
- 尚未包含：正式安装器和最终角色美术素材。

## 许可与素材权利

Pry 的程序源代码及普通项目文档使用 [Apache License 2.0](LICENSE)。该许可允许使用、修改、分发和商业利用代码，但要求遵守协议中的版权、许可和通知义务。

Apache License 2.0 **不适用于** Pry 的原创角色设计、立绘、头像、桌宠动画、Logo、音频及其他美术素材。原创素材版权归作者所有；个人可以按照 [原创素材许可](ASSETS_LICENSE.md) 和 [非商业二次创作政策](FAN_CONTENT_POLICY.md) 进行非商业二次创作。商业使用、原始素材再分发及其他超出许可范围的行为需要事先取得书面授权。

模型、运行库、字体和第三方素材适用各自许可证，详情参见 [第三方声明](licenses/THIRD-PARTY-NOTICES.md)。
