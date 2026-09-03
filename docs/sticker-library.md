# 表情包库规范（v1）

预装表情放在 `src/Pry.App/Resources/Stickers/`，并登记到同目录的 `manifest.json`。程序不会允许模型通过文件路径选择图片，只接受清单内的稳定 ID。

```json
[
  {
    "id": "builtin_happy_01",
    "name": "开心挥手",
    "file": "happy_01.webp",
    "emotions": ["开心", "欢迎"],
    "situations": ["打招呼", "收到好消息"],
    "avoidWhen": ["严肃话题", "用户悲伤"],
    "intensity": 0.7,
    "interactionRole": "backchannel",
    "likelyBackchannel": true,
    "enabled": true
  }
]
```

要求：

- `id` 发布后保持稳定，避免历史消息找不到原表情。
- `file` 必须是清单目录内的相对路径。
- 支持 PNG、JPEG、WebP 和 GIF；动图效果取决于当前 UI 解码器。
- 内置表情由应用资源管理，用户不能删除。
- 用户导入内容复制到 `%LOCALAPPDATA%/PryCompanion/stickers/`，升级应用时不会覆盖。
- `emotions` 用于模型选择；`situations` 和 `avoidWhen` 用于减少语境不合适的选择。
- `intensity` 范围为 0–1，并与未来 Live2D、动作和语音表现共享。
- `interactionRole` 可为 `backchannel`、`reaction`、`content` 或 `interrupt`，用于判断用户表情是否打断角色。
