# MEM-005 Memory & Context 实现交接

日期：2026-09-20。实施位置：`codex/memory-context`，从已提交 `3613a2a` 建立的独立 worktree。未提交、未合并、未发布。本报告描述该 worktree 的实际代码，不能用作主协作工作区已完成集成的结论。MEM-005 总任务保持进行中。

## Implemented

- 引入 `IMemoryRepository / IMemoryQuery / IMemoryWriteback / IContextBuilder`；关系状态、Behavior、Expression 不进入记忆表。
- 接入基线中的 ConversationService 和 ReplyPlanner。正常多气泡回复在最后一条投递事务里登记任务；流式路径与最终助手消息同事务入队。移除这两个旧路径的同步关键词写回。
- 实现证据、去重、明确更正、删除/清空、租约恢复、检索 trace、预算和管理 API；不实现 Runtime Scheduler、Behavior Planner 或 Agent Harness。
- 工作区里较新的 ProactiveMessageGenerator、MEM-004、RT-002/003 文件未复制、覆盖或提交，本分支没有它们；集成时需要单独迁移其调用点。

## Memory Schema

复用 `memories`，追加 scope_key、status、confidence、fingerprint、topic_key、origin、state_version、supersedes_id。类型保留既有 kind 兼容性；自动提取目前只有 preference 与 personal_context，其余 event/user_fact/relationship/promise 等可手工管理。

新增 memory_evidence、memory_tombstones、memory_candidates、memory_scopes、memory_scope_conversations、memory_turn_sources、memory_turn_deliveries。memory_schema=1 记录 Memory 组件版本，不抢占平台正在协调的全局 schema v8；未知组件版本拒绝启动。稳定 CharacterInstance ID 由平台供应，Memory 不自行推导用户身份。

scope 是不透明键并绑定 character_id。实例键必须由宿主明确绑定会话，同一会话不能绑两个作用域。测试覆盖同一 character 下两个实例隔离。现有 HTTP/聊天适配只使用 character:{id}；不是完整多用户实例产品。

证据包含真实 messageId、conversationId 和不超过 100 字的原始明确陈述。数据库 role 必须是 user，且提交、认领、落库时都读取实际消息。候选不能靠自己声明 attribution 或 confidence 获准。手工记忆 origin=manual，不假装有聊天证据；编辑后移除旧 evidence。

## Retrieval Logic

先做相关性门：关键词重合至少 .25，或可用语义余弦至少 .65；随后统一排序：

`0.35 × max(keyword, semantic) + 0.20 × keyword + 0.10 × recency + 0.15 × importance + 0.10 × conversation + 0.05 × relationship + 0.05 × confidence`。

总分至少 .35、confidence 至少 .5、status=active。同主题只取一条、最多六条，并受 token 预算约束。recency 按 updated_at/created_at，半衰期 event=14 天、preference=365 天、其他=90 天；没有把一次查询本身当成“事实更近”。关系相关性来自调用方只读信号，不修改关系状态。

首版关键词是英文词及中文双字片段；没有宣称已做中文分词/FTS 优化。IMemorySemanticSignal 是可选输入，必须对当前记录评分；未知/已删 index ID 被丢弃。适配器失败/两秒超时返回 DegradedKeywordOnly；数据库失败为 Unavailable；真正无相关项为 Empty。调用方取消向上传播。

没有长期结果缓存。检索检查 scope epoch；ContextBuilder 再读当前记录，排除删除、替换和旧正文，证据 excerpt 不进入 prompt。已经发送给模型的上下文无法撤回，运行中模型取消与发送前复核仍须 Runtime 集成。

## Writeback / Consolidation

- Enqueue 只持久化有限消息 ID，不调用模型、不复制整段聊天。必须有真实用户输入和已持久化的助手投递 ID。
- 后台每五秒最多处理八个任务；普通任务等待二十分钟或累计八条 pending 输入，高信号明确陈述可提前处理。阈值以 ConsolidationPolicy 表达；桌面宿主目前用默认值，尚未开放设置 UI。
- hard cap 为 256 个 pending/claimed；显式提交返回 backpressure，投递内登记在满容量时放弃学习，不能无限堆积。没有让“高信号”绕过硬上限。队列仍以 Turn 为单位，当前提取不调用 LLM；跨 Turn 的模型批处理留待后续。
- scope/conversation/turn 唯一键保证重放；payload hash 检测同键不同输入。租约带随机 fencing token，120 秒到期可由新 worker 认领；旧 worker 不能提交或覆盖新租约。
- 最多三次尝试，失败延后 30 秒/120 秒，超过上限 failed；任务七天过期 abandoned。取消停止后依赖租约恢复，不把取消写成成功。任务与事实落库同一事务，terminal payload 清空。
- 规则提取只接受整条、单一第一人称“我喜欢/不喜欢/住在/叫…”陈述。引用、附件、条件句、第三方、行为指令和多主语句拒绝。第三方 extractor 的结果也必须通过同一确定性证据校验；目前这不是通用 LLM 抽取接口。
- 同 kind + 归一化正文 SHA-256 合并证据；confidence .90 起、独立证据 +.03、最高 .99。显式“更正/现在/不喜欢”按 topic 替换，旧条目标 superseded；消息 ID 水位防止较旧任务推翻新事实。更正置信度为规则值 1，不是模型自评。
- 删除采用硬删除正文/证据 + 无正文墓碑。scope epoch 使在途任务失效，forget_before 水位挡住清空后迟到登记的旧消息。自动再抽取同指纹仍拒绝；用户可以手工重建。此策略偏保守，删除一条也会放弃该作用域尚未学完的旧证据。

## Context Budget

ContextBuilder 是本分支两条回复路径唯一组装入口；PromptBuilder 保留为身份/格式渲染兼容层。优先级：

| 顺序 | 分区 | 默认上限 |
|---|---|---:|
| P0 | 身份、输出规则、当前输入 | 全部保留；放不下抛 ContextBudgetException |
| P1 | 当前状态摘要 | 240 |
| P2 | 当前会话最近消息 | 1400；从最旧开始丢弃 |
| P3 | Relevant Memories | 480、最多 6 条 |
| P4 | Behavior Reference | 200 |
| P5 | Expression Pattern | 160 |
| P6 | Capability Result | 240 |

输入总额 = 模型 context size − 输出预留 − 128 安全余量。各分区上限同时受剩余总额约束。整条记忆/分区放不下就丢弃并记录原因，不截掉否定词。输入 P0 过长时明确失败，不静默丢用户内容。近期历史没有“保底四条”承诺，预算不足可全部丢弃。

ITokenEstimator 默认用 UTF-8 字节数作为保守文本估计，每条消息另计八单位；这比设计稿的 CJK 启发式更保守，代价是少用上下文。不是实际 tokenizer，也不估算视觉 patch token；精确 tokenizer、图像预算与 Provider 实际上限需要平台适配。

## Persistence Changes

- 迁移在单个 SQLite 事务中增加列/表/索引/触发器并回填旧行；同文旧记录保留最小 ID 为 active，其余 archived。外键和 integrity_check 失败则回滚 Memory 迁移。
- 原 memories FTS 触发器继续维护正文。当前分支没有 embedding 表；与 MEM-004 合并时须保留其外键级联并复测失效/索引重建。测试里的语义信号是确定性替身，不是真实模型验收。
- 原消息删除仍连带删除来源记忆；新增 tombstone 后，撤销聊天删除只恢复聊天，不再复活这些记忆。没有擅自改成“删原文却保留派生事实”。
- **回滚**：执行前停宿主并由平台备份整个数据库；安全回滚是恢复备份并切回旧二进制。不能直接把升级后的库交给旧二进制：旧查询不识别 superseded，旧写入也不维护 scope。不得靠删新表/列“回滚”。本轮测试只用临时数据库，未迁移用户日常数据。
- 没有改全局 schema_info 的版本策略，平台合并时应把组件迁移纳入迁移注册表，避免多 Owner 同时认领 v8。

## Contracts Changed

- 内部 MemoryContracts.cs 定义作用域、真实投递回合、候选、租约、结果语义、trace、语义信号和 token estimator。
- ContextBuilder.cs 定义 ContextRequest/ContextBudget/ModelContext/IContextBuilder；Intelligence 现有过渡投影仍需在合流时删除或适配，不能永久保留两套 MemoryContext。
- 新增独立传输 DTO MemoryDetailsResponse/MemoryEvidenceResponse/ClearMemoriesRequest，Client 对应 GetMemoryDetailsAsync/ClearMemoriesAsync。
- HTTP 新增 GET /api/v1/memories/details、POST /api/v1/memories/clear；既有 DELETE 改为幂等，编辑撞同文返回 409。验证、错误及删除行为见 [API v1](api-v1.md)。

## Tests

独立 worktree Release 构建 0 警告/0 错误；全量 **218 通过、0 失败、1 跳过**（既有真实语音模型条件测试）。Core 41、API 17、Client 4、App 156。相对基线新增 27 个用例，修改一个旧撤销测试以验证防复活语义。

覆盖用户事实、第三方/引用/伪造拒绝、证据合并、幂等重放、明确更正与乱序完成、删除/清空及迟到写回、过期租约重启恢复、退避/耗尽/反压、无关排除、重要旧事件、语义降级/陈旧 ID、A/B 隔离和同角色实例隔离、预算、重复迁移、手工编辑去旧证据、回复不等待提取。注入数据库失败验证：队列登记失败时最后投递事务回滚，重试才能同时提交。真实随机回环 HTTP 测试管理端点，并验证客户端 DTO 编解码。

git diff --check / --cached --check 通过。仓库脚本使用 powershell -ExecutionPolicy Bypass 执行（只对该进程，不改系统策略），205 个受跟踪文件通过；新增文件另行扫描。无新增外部研究，未运行真实 Embedding/聊天权重，未做 UI 人工验收或纵向 dogfood。

## Recall Examples

| 输入/状态 | 当前问题 | 结果 |
|---|---|---|
| A 会话用户：“我喜欢后摇” | B 会话：“聊聊后摇音乐” | 召回“用户喜欢后摇”，管理面可见 A 的真实 messageId |
| “朋友说我喜欢后摇”或附件引用 | “后摇” | 不自动写成用户偏好 |
| “我住在上海” → “更正，我住在杭州” | “上海” | 旧条目 superseded，不召回 |
| 高重要度的旧“青海湖骑行” | “青海湖骑行” / “晚饭吃什么” | 前者可召回，后者为空 |
| 删除“后摇”且旧任务已认领 | 任务迟到、再次检索 | 旧任务拒写、旧结果不注入 |

## False Recall Risks

中文双字片段会产生偶然重合，.25/.65/.35 阈值未经真实语料标定。规则无法识别未标记的角色扮演、撒谎或隐含引用；只证明用户确实说过，不证明世界事实为真。提取范围刻意很窄，英文、复杂偏好、确认式证据与多数情景事件会漏记。近义句不自动合并，重复主题可能被保守丢弃。没有产品连续性/留存结论。

## Deferred Work

MEM-005 总任务仍在进行中：合流当前 MEM-004 和 RT-002、第三处主动消息 ContextBuilder 接线、真实 Embedding 适配和阈值 Eval、Intelligence MemoryContext 收敛、实例 ID/宿主接线、证据/清空前端 UI、FTS 候选生成与大规模记忆查询性能、关系信号接线、模型批量抽取、调度活动信号/last-consolidation/access log、版本化备份恢复。

集成顺序：先保留本分支 Memory 仓储与测试；平台合并迁移；Runtime 在新投递事务内登记真实输入及成功输出，仍只向 IMemoryQuery/IContextBuilder 请求上下文；最后替换现有 Embedding 和主动消息拼装点。任何合并都重跑删除后索引/缓存/队列不复活以及新 Runtime 插话/取消用例。不得直接复制本分支的旧版 ReplyPlanner、ConversationSessionService 或 MemoryDatabase 覆盖当前在途实现。
