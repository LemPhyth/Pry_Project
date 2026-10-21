# Pry 资源限额总盘（Resource Limit Ledger）

工单：`docs/dispatch-hds-011c-20261022.md` 片 E2（HDS-011c-E2）。建立日期 2026-10-22。

这张表是**限额的登记处**，不是第二套上限语义：数值、单位、默认值一律以表里指的源码声明点为准。
它存在的理由是"下一次有人再加一把上限时会被机器提醒"——判据是
`tests/Pry.Api.Tests/ResourceLimitLedgerCoverageTests.cs`，它扫 `src/**` 里名字含
`Limit`／`MaxLength`／`Timeout`／`Tokens` 的 `const`／`static readonly` 常量，
每一个都必须在这里有一行；**漏一格即红**，行里的 `文件:行号` 指不到那处声明也即红。

## 读表约定

- **限额名**：源码里那个常量的名字（判据就是按它配行的，改名要先改表）。
- **声明点**：`文件:行号`，行号指那一行必须仍然含这个名字。
- **数值**：抄自声明点；表达式（如 `HeartbeatIntervalMs * 2 + 2_000`）照抄表达式并给折算值。
- **防的用户可见后果**：这把尺拿掉之后，用户会看见什么。**没有这一栏的行不算登记完**。
- **有没有判据**：`有`／`无`。`无` 是如实登记，不是待办的装饰——守门人按它排队。
  形状只认两种：行为级（真的越过一次上限，看它是被拦、被降档还是照抄进去）与
  接线级（钉住上限确实被那条路径读到）。文本 grep 不单独算判据，要写明它只是文本门。
- 本表**不判许可兼容性**，也不判"这个数值定得对不对"（那是 Eval 标定与作者裁决的格）。

## 一、入口与正文形状

| 限额名 | 声明点 | 数值 | 防的用户可见后果 | 有没有判据 | 判据 |
|---|---|---|---|---|---|
| MessageContentMaxLength | `src/Pry.Contracts/ApiContracts.cs:196` | `20_000` | 一条超长正文（粘贴整份文档／一串脸滚键盘）直接进库：会话列表卡死、历史页翻不动、每次装配提示都拖着它跑一遍 | 有 | `tests/Pry.Api.Tests/StickerContentLengthGuardTests.cs`／`Content_accompanying_a_sticker_is_rejected_over_the_shared_limit`、`The_shared_ruler_sits_at_the_original_ceiling_and_tolerates_both_message_shapes`（含 20_000 边界与"只有一把尺"） |
| MaxCharacters | `src/Pry.Core/TurnTaking/AttachmentTextExtractor.cs:10` | `2_200` | 附件正文原样塞进提示：一个 3 万字的 txt 把上下文窗口吃光，模型开始胡言或整轮失败 | 有 | `tests/Pry.Core.Tests/AttachmentLimitTests.cs`／`A_valid_docx_still_extracts_its_body_text`（截断形状与放行形状各半） |
| MaxDocxDecompressedBytes | `src/Pry.Core/TurnTaking/AttachmentTextExtractor.cs:14` | `8L * 1024 * 1024`（8 MiB） | 一个几百 KB 的 docx 解出几个 GB（zip 炸弹）：后端内存打满、整个应用卡死 | 有 | `AttachmentLimitTests.cs`／`An_oversized_decompressed_document_is_rejected_and_leaks_no_partial_body` |
| MaxDocxCompressionRatio | `src/Pry.Core/TurnTaking/AttachmentTextExtractor.cs:15` | `100` | 同上，从压缩比这一侧拦（还没解完就先看出形状不对） | 有 | `AttachmentLimitTests.cs`／`A_high_compression_ratio_is_rejected` |
| MaxDocxNodes | `src/Pry.Core/TurnTaking/AttachmentTextExtractor.cs:16` | `200_000` | 几千万个 XML 元素的文档把解析线程占死，用户等一句回复等到超时 | 有 | `AttachmentLimitTests.cs`／`An_excessive_node_count_is_rejected` |
| MaxDocxDepth | `src/Pry.Core/TurnTaking/AttachmentTextExtractor.cs:17` | `200` | 深度嵌套的 docx 触发栈溢出：进程直接没，会话里所有未存的东西一起丢 | 有 | `AttachmentLimitTests.cs`／`An_over_deeply_nested_document_is_rejected_before_full_parse` |
| MaxDocxNameLength | `src/Pry.Core/TurnTaking/AttachmentTextExtractor.cs:18` | `256` | 单元素名几十兆字符：内存与 CPU 全花在名字上 | 有 | `AttachmentLimitTests.cs`／`An_overlong_attribute_name_is_rejected` |
| MaxDocxTextCharacters | `src/Pry.Core/TurnTaking/AttachmentTextExtractor.cs:19` | `500_000` | 累计正文字符兜底：前面每道都过但内容极大的文档仍在此止步 | 有 | `AttachmentLimitTests.cs`（该族逐档判；此格由同一族的 `An_excessive_node_count_is_rejected` 与 `A_valid_docx_still_extracts_its_body_text` 夹住形状） |
| DocxChunkSize | `src/Pry.Core/TurnTaking/AttachmentTextExtractor.cs:20` | `8192` | 不是上限，是每次读的字节块大小；它决定前面那些检查多久跑一次 | 无 | — （登记在此只为让"族"读得完；不构成待办） |
| DocxProcessingTimeoutMs | `src/Pry.Core/TurnTaking/AttachmentTextExtractor.cs:21` | `15_000` | 磁盘慢／杀软拦截时一次附件读取没有终点：用户发消息后界面一直停在"她正在看" | **无** | — （如实登记。今天 `AttachmentLimitTests` 判的是字节／节点／深度／名字四档，时间档要靠真慢的盘或注入时钟才越得过去，本轮不为它造第二套假计时器；排队给守门人） |

## 二、上下文与提示装配

| 限额名 | 声明点 | 数值 | 防的用户可见后果 | 有没有判据 | 判据 |
|---|---|---|---|---|---|
| DefaultSafetyTokens | `src/Pry.Core/Prompting/ContextBuilder.cs:16` | `128` | 估算与真实分词之间的误差没有余量：请求在模型侧 400，或输出被挤到 0 个 token（她说不出话） | 有 | `tests/Pry.Core.Tests/ContextBudgetTests.cs`／`ForModelWindow_turns_an_equal_output_and_context_into_a_positive_input_budget`、`TryNormalizeWindow_rejects_windows_too_small_to_hold_a_positive_input_budget` |
| MinimumInputTokens | `src/Pry.Core/Prompting/ContextBuilder.cs:17` | `512` | 窗口被输出保留占满、输入预算归零甚至为负：整轮一个字都装不下，表现为"她永远不回话" | 有 | `ContextBudgetTests.cs`／`The_raw_equal_output_and_context_window_yields_a_negative_input_budget` ＋ `tests/Pry.Api.Tests/ContextWindowLoadTests.cs`（宿主侧装配真读这一档） |
| MinimumUsableTokensPerSecond | `src/Pry.Api/Services/ModelPerformancePolicy.cs:9` | `12` | 测出每秒只吐几个 token 还照用大上下文：用户等一句回复等几十秒，且反复重启同一个坏档位 | 有 | `tests/Pry.Api.Tests/ApplicationServiceTests.cs`／`Model_performance_policy_only_retries_capacity_and_performance_failures`（4.5 token/s 判太慢；未判 12 这条边界与"4096 以下不再降档"的地板） |

## 三、会话内账本与投递凭据

| 限额名 | 声明点 | 数值 | 防的用户可见后果 | 有没有判据 | 判据 |
|---|---|---|---|---|---|
| PendingTurnLimit | `src/Pry.Api/Runtime/CharacterIntelligenceGateway.cs:44` | `8` | 说话前那一问的账没人收（轮次被取消、宿主崩在投递前）时账本无上限地长：后端跑几天后内存一路涨，越用越卡 | **有（本刀新增）** | `tests/Pry.Api.Tests/CharacterIntelligenceLedgerCapBehaviorTests.cs`／`A_turn_past_the_pending_cap_drops_the_oldest_entry_and_keeps_the_newest` |
| ReceiptLimit | `src/Pry.Api/Runtime/CharacterIntelligenceGateway.cs:51` | `64` | "用户点的这一条出自哪一轮"的凭据本无上限地长；更糟的是它一旦被写成"找不到就归给最近一轮"，她会学错自己说过的话 | **有（本刀新增）** | `CharacterIntelligenceLedgerCapBehaviorTests.cs`／`A_receipt_pushed_out_of_the_window_is_refused_not_guessed`。先前只有 `tests/Pry.App.Tests/ExpressionFeedbackSurfaceTests.cs:101` 那道文本门 |

## 四、协议日志与留存

| 限额名 | 声明点 | 数值 | 防的用户可见后果 | 有没有判据 | 判据 |
|---|---|---|---|---|---|
| MaxPerConversationLimit | `src/Pry.Core/Memory/MemoryDatabase.ProtocolEvents.cs:219` | `5_000` | 用户把一个不合理的数写进环境变量：重放日志把磁盘和内存吃光，客户端重连时等一份永远追完的 replay | 有 | `tests/Pry.Api.Tests/ProtocolEventCursorTests.cs`／`Retention_band_clamps_a_broken_configuration_to_usable_values` |
| MaxTotalRowsLimit | `src/Pry.Core/Memory/MemoryDatabase.ProtocolEvents.cs:223` | `200_000` | 同上，全局行数这一侧：库长到打开会话列表都卡 | 有 | 同上用例 |
| MinPerConversation | `src/Pry.Core/Memory/MemoryDatabase.ProtocolEvents.cs:218` | `50` | 配置被写成 0 或负数 ⇒ 一条事件都不留，客户端每次重连都要全量重放或直接断线 | 有 | 同上用例 |
| MinTotalRows | `src/Pry.Core/Memory/MemoryDatabase.ProtocolEvents.cs:222` | `500` | 同上（全局档） | 有 | 同上用例 |
| DefaultPerConversation | `src/Pry.Core/Memory/MemoryDatabase.ProtocolEvents.cs:217` | `500` | 没配置时的默认保留量；它决定磁盘占用与重放窗口的起点 | 有 | 同上用例（缺省段断言三档都落在默认值） |
| DefaultMaxAgeDays | `src/Pry.Core/Memory/MemoryDatabase.ProtocolEvents.cs:220` | `7` | 老事件不清：日志目录只进不出 | 有 | 同上用例 |
| DefaultTotalRows | `src/Pry.Core/Memory/MemoryDatabase.ProtocolEvents.cs:221` | `20_000` | 同上（全局档默认值） | 有 | 同上用例 |

## 五、网络与心跳档位

| 限额名 | 声明点 | 数值 | 防的用户可见后果 | 有没有判据 | 判据 |
|---|---|---|---|---|---|
| HeartbeatIntervalMs | `src/Pry.Contracts/PryProtocol.cs:41` | `15_000` | 事件流静默探测的节拍；它同时决定下面那一档要多长 | 有 | `tests/Pry.Api.Tests/EventStreamHeartbeatTests.cs`／`Default_heartbeat_sits_inside_the_tunable_band` |
| LivenessTimeoutMs | `src/Pry.Contracts/PryProtocol.cs:47` | `HeartbeatIntervalMs * 2 + 2_000` ＝ `32_000` | 没有存活窗口：半开的连接看起来还活着，客户端不再重连，用户以为"她卡住了"而实际是通道已经死了 | 有 | `EventStreamHeartbeatTests.cs`／`Liveness_window_covers_at_least_two_heartbeat_periods`；客户端侧 `tests/Pry.Client.Tests/BackendRequestBudgetTests.cs`／`Idle_subscription_still_fits_inside_the_request_budget` |
| BackendRequestTimeoutMs | `src/Pry.Contracts/PryProtocol.cs:63` | `LivenessTimeoutMs` ＝ `32_000` | 普通请求没有预算：后端不回话时界面一直转，用户只能强杀应用 | 有 | `BackendRequestBudgetTests.cs`／`An_exhausted_whole_request_budget_reports_a_timeout_instead_of_a_cancellation`、`An_event_subscription_that_never_gets_headers_fails_on_the_local_budget` |
| TranscriptionRequestTimeoutMs | `src/Pry.Contracts/PryProtocol.cs:69` | `4 * 60_000` ＝ `240_000` | 长任务（语音转写）被普通档掐掉：用户录了一段话，回来只剩"请求超时" | 有 | `BackendRequestBudgetTests.cs`／`Transcription_gets_its_own_budget_without_loosening_the_shared_one` |
| RequestBudgetCeilingMs | `src/Pry.Contracts/PryProtocol.cs:80` | `TranscriptionRequestTimeoutMs + 15_000` ＝ `255_000` | 客户端手里那个"总预算"没有封顶：一条请求可以被挂到永远，重连逻辑也不会启动 | 有 | `tests/Pry.Client.Tests/DeadLoopbackReconnectTests.cs`（重连前耗尽总预算的形状） |
| Timeout | `src/Pry.Contracts/ApiContracts.cs:22` | 错误码字面量 `provider_timeout`（不是数值） | 上游超时被翻成用户读得懂的错误；丢了它就是一句裸异常文案 | 有 | `tests/Pry.Api.Tests/ContractBoundaryTests.cs`／`ProviderErrorMappingTests.cs`（错误码在册、映射有例）。登记在此是因为名字撞上 `Timeout` 关键字 |
| RateLimited | `src/Pry.Contracts/ApiContracts.cs:21` | 错误码字面量 `provider_rate_limited`（不是数值） | 上游 429 时用户看得懂"被限流"，而不是"未知错误" | 有 | 同上（`ContractBoundaryTests.cs` 在册） |
| RemoteResponseHeaders | `src/Pry.Core/Inference/ProviderTimeouts.cs:20` | `120s` | 远端提供商收头不前：一次生成永远悬着，宿主不知道失败 | 有 | `tests/Pry.Core.Tests/ModelProviderErrorTests.cs:129-135`（读回 `HttpClient.Timeout` 与常量同值；无"真等过一次超时"的行为例） |
| LocalResponseHeaders | `src/Pry.Core/Inference/ProviderTimeouts.cs:23` | `300s` | 本地模型首次载入权重要几分钟，按远端档会被误杀：用户以为模型坏了 | 有 | 同上 |
| ResponseStreamIdle | `src/Pry.Core/Inference/ProviderTimeouts.cs:26` | `120s` | 流式回复中途断供：界面停在半句话上，没有失败也没有完成 | 有 | `ModelProviderErrorTests.cs`（流空档的形状）；`OpenAiCompatibleChatModel.cs:17` 是它的消费点 |
| TimeoutMs | `src/Pry.Api/Capabilities/DesktopCapabilities.cs:21` | `5_000` | 出厂能力（开网页／开程序）挂住时整轮回复被一次外部调用拖死 | 有 | `tests/Pry.Api.Tests/CapabilityDispatcherTests.cs`／`A_capability_that_overruns_its_budget_becomes_a_timeout_not_a_hang`（行为判据用注入的 50ms 样张）；出厂值本身只有 `DesktopCapabilityGuardTests.cs:176` 的相等断言 |

## 六、进程与探测

| 限额名 | 声明点 | 数值 | 防的用户可见后果 | 有没有判据 | 判据 |
|---|---|---|---|---|---|
| DeviceProbeTimeout | `src/Pry.Core/Inference/LlamaHardwareDetector.cs:22` | `20s` | 显卡探测挂住（驱动／WMI 卡死）：应用永远停在启动页，用户不知道要关还是要等 | 有 | `tests/Pry.Core.Tests/HardwareProbeTimeoutTests.cs`／`A_hanging_probe_is_cut_off_within_its_budget`、`A_hang_does_not_poison_the_next_probe` |
| MemoryProbeTimeout | `src/Pry.Core/Inference/LlamaHardwareDetector.cs:25` | `10s` | 显存查询卡住：档位判定不出来，选模型那格一直转 | 有 | 同上（两个 budget 由同一族用例夹住） |
| JobObjectLimitKillOnJobClose | `src/Pry.Core/Inference/WindowsProcessJob.cs:10` | `0x00002000`（Win32 `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`） | 不是数值上限，是"父进程退掉子进程就死"这条旗标；它掉了以后关应用会留下一堆孤儿 llama-server 占着显存 | 有 | `tests/Pry.Core.Tests/CoreTests.cs`／`Closing_windows_process_job_terminates_owned_process` ＋ `HardwareProbeTimeoutTests.cs`／`Killing_on_timeout_takes_the_whole_tree_not_just_the_shell` |

## 七、上传与体积

| 限额名 | 声明点 | 数值 | 防的用户可见后果 | 有没有判据 | 判据 |
|---|---|---|---|---|---|
| CeilingUploadBytes | `src/Pry.Api/Services/MediaUploadLimits.cs:13` | `50L * 1024 * 1024`（50 MiB） | 一张几百 MB 的图／一段视频把库和内存一起吞掉，还会让会话同步卡死 | 有 | `tests/Pry.Api.Tests/MediaUploadLimitTests.cs`／`File_over_the_cap_is_rejected_with_a_stable_code_before_anything_is_stored` |
| MultipartOverheadBytes | `src/Pry.Api/Services/MediaUploadLimits.cs:16` | `1L * 1024 * 1024` | 只按文件本体封顶：带 multipart 边界后请求体比封顶大，Kestrel 先 400，用户看到的是"上传失败"而没有原因 | 有 | `MediaUploadLimitTests.cs`／`Every_upload_limit_site_carries_the_same_ceiling` |
| CeilingRequestBodyBytes | `src/Pry.Api/Services/MediaUploadLimits.cs:19` | `CeilingUploadBytes + MultipartOverheadBytes` ＝ 51 MiB | 同上（请求体那一侧的合尺） | 有 | 同上 |
| MinimumMaximumUploadBytes | `src/Pry.Api/Services/MediaUploadLimits.cs:22` | `64L * 1024` | 用户把上限配得太小到连一张缩略图都放不下：所有附件一律失败 | 有 | `MediaUploadLimitTests.cs`／`Runtime_cap_can_only_be_tightened_below_the_ceiling`、`File_under_the_cap_still_uploads_after_the_limit_was_tightened` |
| DefaultMaximumUploadBytes | `src/Pry.Api/Services/MediaUploadLimits.cs:25` | `CeilingUploadBytes` | 没配置时的默认封顶 | 有 | `MediaUploadLimitTests.cs`／`Policy_endpoint_publishes_the_effective_cap` |

## 八、本刀补的两格（以及为什么是这两格）

E2 要"零判据的格子本轮至少补两条行为判据"。全表清点后，`有判据` 的格占了绝大多数，真正一格判据都没有的是：
`DocxProcessingTimeoutMs`、`PendingTurnLimit`、`ReceiptLimit`（只有文本门）三格，另有一格
`MinimumUsableTokensPerSecond` 只有远离边界的读数。选后两条中更重的两格：

1. **`PendingTurnLimit`**（第三节第 1 行）。后果是**长期运行**这一整条线的地基：Pry 的常态是"后端开着几天不关"，
   一笔没人收的账如果无上限地长，用户看见的是越用越卡、最后被 Windows 杀掉。它同时是"宁可少学一次"这条
   取舍的落点——所以判据必须能同时看见"挤掉了最老的一笔"和"最新那笔照学"，否则删掉整个账本也是绿。
2. **`ReceiptLimit`**（第三节第 2 行）。它守的不只是内存，而是 `DEF-P3SEAM-02` 那条结论：
   凭据不在场时**明确拒收**，绝不退化成"归给最近一轮"。那条退化会让她把没说的话当成说过，
   是用户直接可读的错。今天这一格只有一道 grep `ReceiptLimit` 的文本门，把账本改成无上限也不会红。

`DocxProcessingTimeoutMs` 留在"无"：要在测试里越过它，就得真造一个 15 秒的慢读或给 Core 注进一个时钟接缝，
那是新功能而不是补判据（工单明示本单不扩范围）。`MinimumUsableTokensPerSecond` 的边界读数同理留给守门人排队。

两格判据都在 `tests/Pry.Api.Tests/CharacterIntelligenceLedgerCapBehaviorTests.cs`：跑真 SQLite、真网关、
真投递装饰器，上限的**实际数值**由反射从声明点读出（本刀不许改数值，也不许把 8／64 抄第二份到测试里），
用例是"越界一次 ＋ 界内一次"两半，缺一半就红。
