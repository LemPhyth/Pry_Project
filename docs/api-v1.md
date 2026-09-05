# Pry Backend API v1

基础地址：`http://127.0.0.1:5078`。请求和响应使用 UTF-8 JSON，字段名采用 camelCase，枚举使用字符串。时间为 ISO 8601 UTC/带偏移时间。当前仅支持本机单用户模式，无认证。

## 通用错误

错误使用 `application/problem+json` 语义：

```json
{
  "type": "urn:pry:error:validation_error",
  "title": "请求参数不合法",
  "status": 400,
  "detail": "不能为空",
  "instance": "/api/v1/conversations",
  "code": "validation_error",
  "traceId": "0HN...",
  "field": "title"
}
```

- `400 validation_error`：字段或业务输入不合法。
- `404 resource_not_found`：会话、文件夹或记忆不存在。
- `500 internal_error`：未预期服务端错误；客户端展示通用提示并记录 `traceId`，不应显示内部堆栈。

## 健康检查

`GET /health`：正常返回 `200 Healthy`。

`GET /api/v1/runtime`：返回后端状态和当前文字/视觉模型 ID。`state` 为 `starting`、`loading_models`、`ready` 或 `failed`。模型按第一次聊天请求延迟加载；错误只返回安全提示，详细原因写入本机日志。

`GET /api/v1/runtime/compute-devices`：由后端调用本地推理运行时探测可用设备，返回稳定的设备 ID、名称和是否为集成显卡。桌面端不得直接启动 `llama-server` 执行硬件探测。

## 会话

### 查询列表

`GET /api/v1/conversations?limit=100`

`limit` 会被限制到 1–200。结果按置顶优先、最近更新优先排序。

### 查询单个会话

`GET /api/v1/conversations/{id}`

### 创建会话

`POST /api/v1/conversations`

```json
{ "characterId": "pry" }
```

`characterId` 可为 `null`。返回 `201`、`Location` 和完整 `ConversationRoom`。会话 ID 由服务端生成，客户端不得自行指定。

### 更新会话

`PATCH /api/v1/conversations/{id}`

```json
{
  "title": "新的标题",
  "isPinned": true,
  "folderId": "folder-...",
  "clearFolder": false
}
```

所有字段可选。移动到根目录时传 `{"clearFolder":true}`，不要用空字符串表示。返回更新后的资源。

### 删除会话

`DELETE /api/v1/conversations/{id}` → `204`。当前为物理删除，连同消息、回复计划及由消息关联的记忆一起按现有事务规则处理；客户端应二次确认。

## 消息

### 查询消息

`GET /api/v1/conversations/{id}/messages?limit=200`

`limit` 限制到 1–500，返回时间正序消息。

### 写入用户消息（迁移期接口）

`POST /api/v1/conversations/{id}/messages`

```json
{
  "role": "User",
  "content": "你好",
  "imagePath": null,
  "stickerId": null
}
```

只接受 `User`；`Assistant` 和 `System` 返回 400。当前不接受 `imagePath`，防止读取任意本机路径。文本最大 20,000 字符。返回 `201` 和持久化后的 `ChatMessage`。

此接口只保存消息，不触发模型回复。前端正式迁移时应使用待实现的聊天 SSE 端点，而不是先调用此端点再调用聊天端点，以免重复写入。

## 聊天回合与事件

前端应先建立事件连接，再提交回合，以便立即显示状态变化。SSE 断线重连时使用最后收到的序号：

`GET /api/v1/conversations/{id}/events?after=123`

`after=-1` 表示从订阅建立时的最新序号开始，只接收随后产生的新事件；桌面客户端切换会话时使用该模式，避免重放已渲染的气泡。

响应为 `text/event-stream`。服务端保留每个活跃会话最近 200 个事件；`id` 是单调递增序号，`event` 可能为：

- `session.ready`：后端会话与角色已就绪。
- `turn.state`：状态为 `UserPending`、`ModelThinking`、`AgentPending`、`AgentSending` 或 `Idle`。
- `message.created`：用户消息已持久化，或助手计划消息已送达。
- `turn.cancelled`：客户端已请求取消。
- `turn.failed`：模型调用失败；只返回稳定错误码和安全提示。

提交回合：`POST /api/v1/conversations/{id}/turns`

```json
{ "content": "你好", "stickerId": null, "immediate": false, "attachmentIds": [] }
```

返回 `202` 和 `{"messageId":123}`。该消息已经持久化；模型回复异步通过 SSE 到达。普通输入使用 `immediate:false` 以保留合并短输入的节奏，显式立即发送使用 `true`。同一会话的并发输入由后端现有回合状态机合并或打断。

取消当前回复：`POST /api/v1/conversations/{id}/turns/cancel` → `202`。取消是幂等操作；如果没有活动回复也不会创建数据。

## 托管媒体与附件

客户端不得提交磁盘路径。先用 `multipart/form-data` 上传，再把服务端返回的资源 ID 放入聊天回合的 `attachmentIds`。

`POST /api/v1/media`，表单字段名为 `file`。成功返回 `201`：

```json
{
  "id": "83ec...",
  "name": "说明.txt",
  "contentType": "text/plain",
  "size": 128,
  "kind": "Text",
  "createdAt": "2026-09-03T11:00:00Z",
  "downloadUrl": "/api/v1/media/83ec.../content"
}
```

约束：

- 不设置单文件硬上限；超过 10 MiB 时 JSON `warnings` 返回中文提示，响应头 `X-Pry-Upload-Warning` 返回稳定 ASCII 码 `large-file`。客户端应先读取 `GET /api/v1/media/policy`，在开始传输前根据 `warningThresholdBytes` 提醒用户。
- 每个聊天回合最多引用 `GET /api/v1/media/policy` 返回的 `maximumAttachmentsPerTurn` 个附件（当前为 6）。客户端不得硬编码不同的数量。
- 支持 PNG、JPEG、GIF、WebP、UTF-8 TXT/MD/CSV 和有效 DOCX。
- 后端同时检查扩展名和文件签名；不信任客户端声明的 Content-Type。
- 原始名称只用于显示与下载，磁盘文件名由服务端随机生成。
- 响应和聊天事件都不会包含服务端磁盘路径。
- 文件始终以流式方式落盘；UTF-8 校验同样分块执行，不会把大文本整体载入内存。用户仍需自行保证本机剩余磁盘空间。

下载：`GET /api/v1/media/{id}/content`，支持 Range 请求。不存在的资源返回 404。

上传策略：`GET /api/v1/media/policy`，当前 `maximumBytes` 为 `null`，表示没有应用层硬限制。

### 孤立媒体保留与清理

后端在启动后及每 6 小时执行一次孤立媒体清理。默认保留期为 7 天，可通过配置键 `Pry:Media:OrphanRetentionDays`（环境变量形式为 `Pry__Media__OrphanRetentionDays`）调整；有效范围为 1～365 天。

清理前会汇总当前背景、用户头像、角色头像、贴纸和消息图片所引用的规范化磁盘路径。只有同时满足以下条件的托管媒体才会删除其内容文件和 JSON 元数据：

- 上传时间早于保留期截止时间；
- 当前不在上述引用集合内；
- 文件仍位于后端托管媒体目录内。

文本、文档和语音上传不会作为长期聊天附件路径保存，处理完成后按同一保留期回收。清理失败只记录不含用户内容的警告日志，不阻止后端启动；成功删除时记录数量、保留天数和释放字节数。前端不得自行扫描或删除后端媒体目录。

## 消息分支变更

- `DELETE /api/v1/conversations/{conversationId}/messages/{messageId}`：用户消息会删除该消息及之后的分支；助手消息只删除自身。返回删除范围及数量。
- `POST /api/v1/conversations/{conversationId}/messages/{messageId}/regenerate`：只接受助手消息，先等待当前回合完全取消，再从对应用户消息重新生成，返回 `202`。
- `POST /api/v1/conversations/{conversationId}/mutations/undo`：撤销最近一次删除或重新生成分支，返回 `204`。
- `POST /api/v1/conversations/{conversationId}/listening-signals`：由后端按会话状态和 30 秒节流规则写入倾听提示，返回创建的助手消息。客户端不得自行伪造或持久化助手消息。

后端会在变更前等待正在运行的回复真正退出，防止删除完成后迟到的助手消息重新写入数据库。每个活跃会话最多保留最近 20 个内存撤销快照；服务重启后撤销栈不保留。

## 会话文件夹

- `GET /api/v1/conversation-folders`
- `POST /api/v1/conversation-folders`，正文 `{"name":"收藏"}`，返回 `201`
- `PATCH /api/v1/conversation-folders/{id}`，正文 `{"name":"新名称"}`，返回 `204`
- `DELETE /api/v1/conversation-folders/{id}`，返回 `204`；其中的会话移到根目录，不删除会话

名称不能为空，最大 100 字符。

## 长期记忆

### 查询

`GET /api/v1/memories?characterId=pry&query=红茶`

`characterId` 必填；`query` 可选。

### 创建

`POST /api/v1/memories`

```json
{
  "characterId": "pry",
  "kind": "preference",
  "summary": "用户喜欢红茶",
  "tags": "饮品,红茶",
  "importance": 0.8,
  "sourceMessageId": null
}
```

返回 `201`。`importance` 必须在 0–1；`kind` 最大 64 字符，`summary` 最大 4,000 字符。

### 全量更新与删除

- `PUT /api/v1/memories/{id}?characterId=pry`，正文包含 `kind`、`summary`、`tags`、`importance`
- `DELETE /api/v1/memories/{id}?characterId=pry` → `204`

`characterId` 同时作为资源归属约束，不能用一个角色 ID 修改另一个角色的记忆。

## 角色卡

- `GET /api/v1/characters`：角色摘要、当前选中状态和可选头像 URL。
- `GET /api/v1/characters/{id}`：完整角色设定；不包含磁盘路径。
- `POST /api/v1/characters`：由服务端生成角色 ID并保存角色卡。
- `PUT /api/v1/characters/{id}`：全量更新角色卡。
- `DELETE /api/v1/characters/{id}`：仅删除用户创建的角色卡。内置角色卡或仍被对话/长期记忆引用的角色返回 `409 resource_conflict`；客户端应提示用户先处理关联数据。
- `GET /api/v1/characters/{id}/avatar`：获取角色头像，支持 Range。

角色头像先通过媒体 API 上传，再在角色请求中传 `avatarMediaId`；传 `clearAvatar:true` 恢复默认头像。结构化角色必须提供 `identity`，Legacy 角色必须提供 `legacySystemPrompt`。配置写入使用临时文件替换；写入前会取消并排空活跃回合。

## 客户端偏好与外观

- `GET /api/v1/preferences`：返回用户资料、快捷键、回合节奏、桌宠偏好和不含路径的主题设置。
- `PATCH /api/v1/preferences`：局部更新上述偏好。
- `PUT /api/v1/appearance/media`：用托管媒体 ID 设置或清除背景及用户头像。
- `PUT /api/v1/settings`：原子应用偏好、外观媒体引用和模型选择，供“保存全部设置”使用。
- `PUT /api/v1/user-profile`：原子保存用户资料与用户头像，供独立的用户资料窗口使用。
- `GET /api/v1/appearance/background`
- `GET /api/v1/appearance/user-avatar`

背景和头像 URL 仅在资源存在时返回。客户端不能通过这些接口设置任意磁盘路径。

`PUT /api/v1/settings` 请求由三个对象组成：

```json
{
  "preferences": { "selectedCharacterId": "pry", "activeConversationId": "room-id" },
  "appearance": {
    "backgroundMediaId": null,
    "clearBackground": false,
    "userAvatarMediaId": null,
    "clearUserAvatar": false
  },
  "models": {
    "activeModelId": "qwen3-1.7b-local",
    "activeVisionModelId": null,
    "activeSpeechModelId": "sensevoice-local",
    "modelTunings": {}
  }
}
```

后端会先验证三个部分并解析媒体引用，然后只写入一次偏好并重载运行时；任何验证失败都不会应用其中任一配置。成功返回最终 `preferences` 和 `models` 投影。上传媒体是独立操作，未被设置引用的上传资源不会自动绑定到配置。

快捷键格式、快捷键冲突、回复数量关系、打字延迟关系、桌宠缩放及其他数值范围均由后端最终校验。客户端可以重复这些检查以即时提示，但不得将客户端校验视为安全或一致性边界。

独立用户资料窗口应调用 `PUT /api/v1/user-profile`：

```json
{
  "profile": { "displayName": "你", "signature": "愿今天也有好心情" },
  "avatarMediaId": "已上传的媒体ID或null",
  "clearAvatar": false,
  "avatarDisplay": { "focusX": 0.5, "focusY": 0.5, "zoom": 1.0 }
}
```

后端先校验资料、媒体资源类型和头像显示参数，再单次保存并刷新内容。任一字段或媒体引用无效时，资料和头像均保持原值。清除头像时传 `clearAvatar:true`；不得先调用偏好接口再调用外观接口模拟该操作。

## 模型配置

- `GET /api/v1/models`：返回可选模型、能力、有效调参值和文字/视觉选中状态；不返回模型路径或密钥。
- `PUT /api/v1/models/selection`：全量设置文字、视觉、语音模型选择以及逐模型 `modelTunings`。
- `POST /api/v1/models/custom`：新增自定义本地或 OpenAI-compatible 模型。
- `PUT /api/v1/models/custom/{id}`：更新自定义模型。
- `DELETE /api/v1/models/custom/{id}`：删除未被选中的自定义模型；不会删除模型文件。
- `GET /api/v1/runtime`：读取当前运行状态。

模型选择或运行参数改变时，后端先排空会话、原子保存偏好，再释放旧模型进程。在线 API Key 始终来自环境变量，不进入请求或响应。

远程 OpenAI-compatible 地址必须使用 HTTPS，回环地址允许 HTTP，URL 不得包含用户名或密码。本地模型路径必须由用户通过系统文件选择器明确选择；后端要求绝对 `.gguf` 路径、文件存在且文件头为 `GGUF`。模型路径仅保存在本机偏好文件，不会通过 API 返回。

## 贴纸

- `GET /api/v1/stickers`
- `POST /api/v1/stickers`：正文包含已上传图片的 `mediaId`、名称和情绪标签。
- `PUT /api/v1/stickers/{id}`：更新用户贴纸名称、情绪标签和互动作用。
- `DELETE /api/v1/stickers/{id}`：删除用户贴纸；内置贴纸不可修改或删除。
- `GET /api/v1/stickers/{id}/content`

贴纸响应只包含内容 URL，不包含磁盘路径。互动作用只接受 `reaction`、`backchannel` 或 `topic`。

## 语音识别

- `GET /api/v1/speech/models`：语音模型列表、选中和可用状态；`custom` 区分内置配置与用户配置，客户端据此决定是否显示编辑/删除入口。
- `POST /api/v1/speech/models/custom`：创建自定义语音模型。
- `PUT /api/v1/speech/models/custom/{id}`：更新自定义语音模型。
- `DELETE /api/v1/speech/models/custom/{id}`：删除未被选中的自定义语音模型；当前选中模型返回 `409 resource_conflict`。
- `POST /api/v1/speech/transcriptions`：正文 `{"mediaId":"...","modelId":null}`。

录音由客户端完成，然后以 WAV 上传到媒体 API。识别由后端串行执行，返回 `text` 和实际 `modelId`；音频资源不能作为聊天附件。首版保持现有能力，只支持 16-bit PCM WAV，不引入隐式转码。
本地 `sherpa-onnx` 配置要求绝对模型目录，并验证 `model.int8.onnx` 与 `tokens.txt`；在线转写地址必须使用 HTTPS，回环开发地址可以使用 HTTP。响应不返回目录、服务地址或密钥。
客户端必须使用语音模型响应的 `available` 字段决定能否开始录音，不得尝试读取后端模型目录。

## 尚未冻结的接口

后端 v1 所需能力已经冻结。桌面客户端切换完成前，仍不得与 API 同时修改同一数据目录。
