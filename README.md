# Pry 本地虚拟陪伴助手原型

这是一个 Windows 优先、可扩展到 Linux/Android 的本地虚拟陪伴助手最小原型。当前包含 Avalonia 桌面 UI、固定角色定义、SQLite 对话与长期记忆、Prompt Builder、OpenAI-compatible 流式模型接口、图片消息入口、可管理的表情包库，以及 Embedding、图片生成、语音识别、Live2D 和 Agent 工具占位接口。

## 运行

```powershell
dotnet restore Pry.slnx
dotnet run --project src/Pry.App/Pry.App.csproj
```

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
