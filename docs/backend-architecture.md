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
- Core 不依赖 HTTP 或 Avalonia，继续承载领域与基础能力。
- Contracts 只描述跨进程数据；Client 负责 HTTP、Problem Details 和 SSE，不访问数据库或模型。
- 当前产品是单机单用户应用，API 默认只监听 `127.0.0.1:5078`。若允许局域网或公网访问，必须先增加认证、授权、TLS、限流和来源策略。

## 迁移顺序

1. 前端改用已实现的会话、消息、文件夹和记忆 API。
2. 桌面窗口已通过 `Pry.Client` 订阅 SSE，并调用角色、偏好、贴纸、模型选择、媒体及语音 API，只保留视图、录音采集、输入、导航和窗口生命周期。
3. 已删除 `MainWindow` 中无调用方的数据库、配置文件和模型编排代码。
4. 后续继续把大型窗口拆为可测试的视图控制器，并执行完整 UI 回归；这属于 M2 可维护性工作，不再是 API-003 的数据权威边界。

不在第一阶段暴露客户端本机路径，也不允许客户端写入 assistant/system 消息；这是后端成为数据权威所必需的边界。
