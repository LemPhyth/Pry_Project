# 主动话题调度（COMP-005）

以下为后端已实现的主动话题契约。前端活动上报和通知体验由 COMP-006 接入。
`deliveryAvailable=true` 表示支持调度，允许保存 `enabled=true`，不表示此刻满足发送条件。
仅复用已经加载的文字模型；模型未加载时状态为 `model_not_loaded`，不会自动启动模型进程。
不采集按键正文或其他应用信息。

- `GET /api/v1/characters/{characterId}/proactive-policy`：200，未保存时返回 revision=0 的默认策略。
- `PUT /api/v1/characters/{characterId}/proactive-policy`：200，原子全量保存。
  请求 `expectedRevision`、`conversationId`、`enabled`、`idleSeconds`、`minimumIntervalSeconds`、
  `dailyLimit`、`timeZoneId`、`quietStartMinute`、`quietEndMinute`、`quietHoursEnabled`。
  响应去除 expectedRevision，增加 characterId、revision、deliveryAvailable。
  首次保存 expectedRevision=0；后续提交读取到的 revision；冲突为 `409 revision_conflict`，须重新读取再确认修改。
  保存必须指定存在且绑定该角色的会话；不存在为404，角色不匹配为 `409 target_mismatch`。
  目标归属和版本比较在同一 SQLite 写事务检查。删除目标会话级联移除此策略，重新创建会话不会自动继承。
- `PUT /api/v1/companion/activity/{clientId}`：200。请求 `sequence`（从1递增）、
  `conversationId`（可空）、`idleSeconds`（0–86400）、`isTyping`、`isRecording`、`isTranscribing`、`isInteractionOpen`。
  clientId 是客户端生成并复用的 GUID（N或D格式）；sequence 同键同内容重试返回原回执且不续期，
  乱序或同序不同内容为 `409 activity_sequence_conflict`。会话不存在为404。
  响应 clientId、sequence、expiresAt；建议每15秒报告，输入/录音/识别/面板/目标变化立即报告。
  心跳有效期45秒，空闲时长不超过后端连续观察到的时长；重启不会信任客户端报来的历史空闲。
- `DELETE /api/v1/companion/activity/{clientId}`：204，幂等注销。正常关闭客户端应注销。
  多客户端取最保守结果：任一忙碌或过期报告均阻止资格；过期报告需原客户端续报或显式注销，
  所有已登记客户端必须报告同一目标会话，空目标或不同目标均暂停。
  不把失联视为用户空闲。内存最多64个客户端，超出返回 `409 activity_capacity`。

默认值为关闭、空闲600秒、最短间隔7200秒、每天3条、Asia/Shanghai、22:00–08:00安静时段。
允许 idleSeconds=60–86400、minimumIntervalSeconds=60–604800、dailyLimit=1–20；
时区必须可由运行平台解析；分钟为0–1439，启用安静时段时起止不可相同。所有非法字段为400 validation_error。
这些为保守默认值；首版模型策略固定为 `already_loaded_only`。

数据库 schema v5 新增 proactive_policies（角色唯一键、会话外键、递增版本及完整策略）。
迁移可重复执行，不修改消息和用户内容。回滚到旧二进制可忽略新增表；无需删除数据库或回填旧数据。

## 调度和状态

- `GET /api/v1/characters/{characterId}/proactive-status`：200，返回 characterId、conversationId、
  state、lastOutcome、lastAttemptAt、nextAttemptAt、sentToday、awaitingUserReply、modelLoadPolicy。
  state 为 disabled、target_unavailable、activity_stale、user_busy、quiet_hours、awaiting_user_reply、
  daily_limit、minimum_interval、idle_threshold、model_not_loaded、generating 或 eligible。
  nextAttemptAt 是持久化间隔的下界，并非承诺发送时间；仍须通过其他资格判断。
- 后端每5秒检查，一次最多一个主动生成；最多120秒，输出仅一条1–500字符文字，空或过长输出不落库。
- 每次尝试先持久化唯一标识和下一次允许时间（当前最短间隔），失败/取消同样等待该间隔；无立即重试。
  成功、失败或取消结束后，间隔下界延长到结束时间加该次策略的最短间隔。
- 每日上限按策略时区的本地自然日统计实际成功投递的记录；修改策略、删除消息或会话不清空角色的频率记录。
- 上一条主动消息之后，须有用户向该角色的任意会话发送新消息才可继续；删除主动消息不解除此限制。
- 普通发送、互动、重生成、删除、撤销、语音转写、记忆修改、运行时重配及有效活动变化取消主动生成；普通空闲心跳不会反复取消。
- 生成期间持续检查活动有效期；投递前重新检查策略版本、会话角色、历史变化、活动和回合状态。
- 消息和成功记录在同一事务中提交，来源为 `proactive`。之后发布普通 `message.created` SSE。
  前端按 messageId 去重和更新摘要、未读，不本地插入助手消息、不抢焦点。
  进程退出前已提交但未送达 SSE 的消息由重连后历史查询恢复；SSE 不是持久化消息队列。
- 重启把遗留 generating 尝试标为 interrupted，不恢复旧生成、不补发；保留冷却，等待新活动心跳。
- 主动消息本身不提取长期记忆；可以删除和撤销删除，不支持重新生成（409 interaction_not_available）。

schema v6 新增角色调度状态和投递记录（不保存 Prompt 或生成失败正文）。记录不与消息做级联删除，
避免删除消息绕过频率限制；仅保留角色/会话/消息标识、时间和安全结果码。索引支持角色日计数。
回滚旧二进制会忽略新增表；无需删除数据库。只有活动心跳持续且用户明确启用的角色参与调度。

## 前端接入和验收

1. 读取策略和状态。总开关控制策略 enabled；deliveryAvailable 是接口能力，不是“模型已加载”。
2. 绑定当前角色会话后保存完整策略及 revision；409 revision_conflict 时重新读取并让用户确认，不能静默覆盖。
3. 每15秒上报必要活动；输入法组合归入 isTyping。切换会话、录音、识别、面板变化立即上报。
4. 只消费 message.created 和历史消息，以 messageId 去重；通知不抢焦点、不曝光长正文。
5. 重连刷新目标会话历史与摘要；不要只依赖内存 SSE 重放。应用退出时注销活动，不建立独立常驻模型进程。
6. model_not_loaded 时提示需要先正常使用文字模型；默认策略不会为主动话题加载文字、视觉或 Embedding 模型。

跨重启未读由 COMP-008 的持久化阅读接口提供，见 [API v1 已读与未读](api-v1.md#持久化已读与未读comp-008)。
前端使用会话 lastReadMessageId/unreadCount 和全局 unread-summary 恢复状态，在实际阅读后提交明确的 throughMessageId；
仅收到主动消息、预取历史或显示通知不能标记已读。此前 COMP-005 的 SSE 投递本身不提供持久化阅读语义。

验证覆盖：事务失败不产生消息、重复完成不重复投递、跨重启冷却、每日限额、用户未回应暂停、目标/版本变化、
空闲心跳保持、活动失联、前台操作取消、迟到输出丢弃、托管任务停止、历史及 SSE 来源一致。
模型调用使用本机 HTTP 替身服务验证完整请求参数、人设/记忆上下文和流式输出处理；未将该测试宣称为真实权重效果验收。
主动生成使用现有角色 Prompt、最近20条有效消息及关键词召回的长期记忆；为了不额外启动 Embedding 进程，
本路径首版不触发向量索引补建。真实模型措辞、系统通知和桌宠效果仍需前端接入后联合验收。
