# Pry 更新记录

## Unreleased

### Added

- 接入正式 Pry 应用图标，用于 Windows 可执行文件、两套主窗口、启动失败窗口和系统托盘。
- 新增工程踩坑与防复发记录，集中说明内嵌后端、模型进程、分支集成、CI 环境和前后端契约问题。
- 新增前后端统一文档索引，明确权威资料顺序、职责边界、接口流程和交付门禁。

### Changed

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
