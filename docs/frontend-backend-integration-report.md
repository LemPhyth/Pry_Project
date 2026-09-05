# Pry 前后端集成故障报告（v0.0.1）

日期：2026-09-06
面向：Pry.App / Pry.Client 前端开发者、Pry.Api / Pry.Core 后端开发者

## 1. 结论

本次故障不是 HTTP DTO 或业务接口设计不兼容，而是桌面宿主方式改变后，启动生命周期、ASP.NET Core 控制器发现和本地运行资源没有一起完成集成。

Pry 已完成职责分层，但当前正式桌面程序仍采用“单进程、逻辑前后端分离”的部署方式：`Pry.App` 在自身进程中启动 `Pry.Api`，为它分配随机回环端口，再通过 `Pry.Client` 使用 HTTP/JSON/SSE。独立运行 `Pry.Api` 只用于 API 调试或其他客户端，本身不会创建桌面窗口。

## 2. 用户可见症状

故障分为三层，先后暴露：

1. 启动 `Pry.App.exe` 后进程存在，但没有窗口，也没有错误提示。
2. 修复窗口启动后，界面找不到偏好、角色、对话和模型数据，模型进入失败状态。
3. 点击设置按钮后应用立即退出，未显示可恢复的错误对话框。

## 3. 根因分析

### 3.1 UI 线程发生同步等待死锁

`App.OnFrameworkInitializationCompleted` 在 Avalonia UI 线程中用 `GetAwaiter().GetResult()` 同步等待 `BackendApplication.BuildAsync` 和 `StartAsync`。数据库初始化中的异步延续需要回到 UI 同步上下文，而 UI 线程正在等待该任务完成，形成互相等待。

故障状态具有迷惑性：进程仍然存活，但主窗口尚未创建，Kestrel 也没有开始监听。因此“任务管理器里有进程”不能作为后端已启动的证据。

### 3.2 内嵌宿主没有发现 Pry.Api 控制器

独立运行 `Pry.Api` 时，ASP.NET Core 的应用入口程序集就是 `Pry.Api`，MVC 可以自动发现其中的控制器。由 `Pry.App` 内嵌承载时，入口程序集变成 `Pry.App`；原注册代码只调用 `AddControllers()`，没有显式添加 `Pry.Api` 应用部件。

结果是显式映射的 `/health` 返回 200，但所有基于控制器的接口都返回 404，包括：

- `/api/v1/runtime`
- `/api/v1/preferences`
- `/api/v1/characters`
- `/api/v1/models`
- `/api/v1/runtime/compute-devices`

这解释了为什么健康检查看似正常，而前端数据投影仍然全部失败。

### 3.3 设置入口允许异步异常逃逸

设置按钮使用 `async void` 事件，打开窗口前首先请求计算设备列表。接口返回 404 后，`PryBackendException` 没有在事件边界捕获，最终由 Avalonia 调度器重新抛出并终止整个 CLR 进程。

Windows Application 日志中的 `.NET Runtime` 事件 1026 明确记录了调用链：

`PryBackendClient.GetComputeDevicesAsync` → `MainWindow.Settings_Click` → 未处理的 `PryBackendException: Not Found`。

### 3.4 前端工作树缺少本地推理运行库

`Pry_Project_frontend` 已通过 Junction 复用主工作树的 `models`，但没有同样提供被 Git 排除的 `runtime`。因此后端能够解析 GGUF 权重，却找不到配置中的 `runtime/llama-server.exe`，模型加载必然失败。

这属于开发环境资源部署问题，不应通过把大型二进制提交到 Git 解决。模型、CUDA/llama.cpp 运行库仍必须由安装脚本、发行包或明确的本地共享目录提供。

## 4. 已实施修复

### 桌面启动

- 将内嵌后端初始化投递到 Avalonia 消息循环，完整使用异步调用，消除 UI 线程同步等待。
- 后端启动成功后显式创建并显示主窗口。
- 后端构建或启动失败时显示可见的启动失败窗口，并安全释放已经创建的 HTTP/后端资源。

### API 宿主

- MVC 注册显式添加 `Pry.Api` 程序集作为 Application Part，使独立宿主与桌面内嵌宿主暴露同一组控制器端点。
- 新增内嵌宿主端点发现测试，至少锁定运行状态、计算设备和偏好三个前端启动关键接口。

### 前端容错

- 将设置按钮事件缩小为带顶层 `try/catch` 的入口，实际设置窗口创建由返回 `Task` 的方法负责。
- 后端版本或网络边界异常时，应用保留运行并显示错误信息，不再直接闪退。

### 开发环境与文档

- 为前端工作树建立被 `.gitignore` 排除的 `runtime` Junction，复用主工作树的 llama.cpp/CUDA 文件，不复制或提交大型运行库。
- README 明确普通用户只运行 `Pry.App`；独立 `Pry.Api` 没有 UI，不能被当作桌面启动程序。
- 统一项目版本为 `0.0.1`。

## 5. 验证结果

合并前分别在主工作树和前端工作树完成 Release 构建及完整测试。修复后的真实 Release 冒烟结果：

- 主窗口获得有效窗口句柄并保持响应。
- 内嵌 API 在随机 `127.0.0.1` 端口监听。
- 健康、运行状态、偏好、角色、模型和计算设备接口全部返回 200。
- 后端读取到原有 `%LOCALAPPDATA%/PryCompanion` 数据。
- 计算设备识别为 `CUDA0 / NVIDIA GeForce RTX 5070 Ti`。
- 文字与视觉共用的 Qwen3.5-9B 模型只启动一个实例，并达到 `ready`。
- 测试结束后确认没有遗留 Pry、Pry.Api 或 llama-server 进程。

## 6. 后续集成约束

前后端后续开发应共同遵守以下规则：

1. 区分“代码职责分离”和“进程部署分离”。当前 v0.0.1 是单进程内嵌 API；若改为真正独立后端，必须先设计端口发现、认证握手、版本协商、启动超时和进程所有权。
2. `/health` 只能证明 Web 主机可达，不能证明 MVC 控制器、数据投影或模型运行时可用。桌面冒烟至少还应请求 `/api/v1/runtime`、`preferences` 和 `models`。
3. 前端不得直接读取 SQLite、后端配置、模型路径或启动模型进程；所有业务数据继续以 API 返回为准。
4. 后端新增或移动控制器时，必须在独立宿主和 `Pry.App` 内嵌宿主两种入口下验证路由发现。
5. 所有 `async void` UI 事件必须在事件边界捕获异常，或只调用一个内部可等待、可测试的 `Task` 方法。
6. Release 包必须明确部署 `Resources` 与本地运行库；Git 工作树不得用“本机恰好存在文件”代替可复现的安装步骤。
7. 前端与内嵌后端来自同一构建产物。若未来允许连接外部后端，需要增加显式协议版本并在不兼容时给出可见提示。

## 7. v0.0.1 启动方式

普通桌面使用：

```powershell
dotnet run --project src/Pry.App/Pry.App.csproj -c Release
```

仅调试独立 API：

```powershell
dotnet run --project src/Pry.Api/Pry.Api.csproj -c Release
```

不要让桌面内嵌后端和独立 API 同时修改同一个 `%LOCALAPPDATA%/PryCompanion` 数据目录。
