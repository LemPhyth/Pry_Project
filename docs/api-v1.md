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

响应为 `text/event-stream`。服务端保留每个活跃会话最近 200 个事件；`id` 是单调递增序号，`event` 可能为：

- `session.ready`：后端会话与角色已就绪。
- `turn.state`：状态为 `UserPending`、`ModelThinking`、`AgentPending`、`AgentSending` 或 `Idle`。
- `message.created`：用户消息已持久化，或助手计划消息已送达。
- `turn.cancelled`：客户端已请求取消。
- `turn.failed`：模型调用失败；只返回稳定错误码和安全提示。

提交回合：`POST /api/v1/conversations/{id}/turns`

```json
{ "content": "你好", "stickerId": null, "immediate": false }
```

返回 `202` 和 `{"messageId":123}`。该消息已经持久化；模型回复异步通过 SSE 到达。普通输入使用 `immediate:false` 以保留合并短输入的节奏，显式立即发送使用 `true`。同一会话的并发输入由后端现有回合状态机合并或打断。

取消当前回复：`POST /api/v1/conversations/{id}/turns/cancel` → `202`。取消是幂等操作；如果没有活动回复也不会创建数据。

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

## 尚未冻结的接口

以下能力仍在原桌面进程，暂不应由前端依赖虚构协议：附件上传与下载、角色卡管理、模型配置写入、语音识别、贴纸管理、用户偏好。其建议形态记录在 `backend-architecture.md`，实现并经过并发与失败测试后再纳入 v1。
