# Nekro-Agent：Code-as-Action + Sandbox / RPC

> **⚖️ 照搬边界（作者 2026-09-21 裁决，`DOC-005`）：本项目永久仅作机制与市场调研参考，源码文本不得进入 Pry 仓库或任何 worktree。**
> 原因不是"机制不像"，而是许可：上游是自定义的"Nekro Agent 开源协议 V1.1"，它把"基于本项目代码、文档、**设计、架构或概念**创建的新作品"都主张为派生作品，强制 GUI/发布物署名"Powered by NekroAgent"（字号≥主文字 80%），规模化商用需事先书面授权，违约须 15 日内删除全部副本。
> 因此本文件的用法限于：读机制描述（§5 的异步巩固调度、§6 的执行记录）→ Pry 独立实现；**不 clone、不 vendor、不逐行对照上游源码**；Prompt 文案、示例数据、截图、图标一律不取用。要改变本条只能由作者裁决。细则见 `docs/upstream-license-compatibility.md` §4.4。

来源：R5（2026-09-17），见 [README](README.md)。原对话指定仓库为 `KroMiose/nekro-agent`。以下是历史源码研究整理，不是当前版本复核。许可实测日期 2026-09-21，HEAD `8df1eced67cc9d6d4a98515ec98f3dbe8deda480`。

## 1. 定位与核心运行对象

Nekro 把 Code-execution Agent Job 作为核心：LLM 编写 Python 程序，程序进入 Docker Sandbox，现实能力由 Plugin RPC 等接口暴露。一次 Agent step 可以是一整段程序执行，区别于 AstrBot 以结构化 Tool Call 为主的循环。

它有 Persona、emotion 等扩展和较重的 Memory，但本报告确认的架构中心是任务执行，不是 MaiCore 式长期社交 Runtime。不能把它贬为“笨 Agent”，也不能因有防抖和人设就认为它已经等价实现 Pry 的角色连续性。

## 2. 历史源码观察：主调用链

```text
用户消息 → MessageService
→ pending / debounce / trigger 判定
→ run_agent()
→ 频道配置 + Persona + Plugin Prompt + Adapter Runtime Prompt + History
→ PromptCompiler → LLM
→ 解析模型生成的 Python → run_script.py
→ Docker Sandbox 执行
   ├─ 普通 Python 计算 / 文件 / 控制流
   └─ 动态 api_caller.py → Plugin RPC / Agent Method
→ 执行结果 / 停止类型
→ 必要时把结果交回 LLM → 再生成代码或结束
```

这个模型适合程序化组合能力，不意味着已证明适合 Pry 计划中的本地小模型。

## 3. Sandbox 与 RPC 边界

原研究在 `sandbox/runner.py` 观察到每次执行创建容器，挂载 `/app/shared`（可读写）、`/app/uploads`（只读）、`/app/.pip_cache`、`/app/packages`；当时配置为 512 MB、1 CPU、用户 nobody、bridge 网络，执行后自动移除。每个 `chat_key` 有共享目录和清理逻辑。

这些是历史实现参数，不是安全性保证或 Pry 推荐值。容器有网络、挂载和 Host RPC 出口，不能因用了 Docker 就推断完全隔离。

```text
启用的 Plugin Methods
→ 动态 api_caller.py / __extension_method_proxy
→ Sandbox 代码调用代理函数
→ pickle(method, args, kwargs)
→ HTTP POST /ext/rpc_exec + X-RPC-Token
→ Host 执行真实 Plugin Method
```

原报告确认了序列化、路由和 Token 的机制，没有完成反序列化、鉴权、沙盒逃逸或恶意插件安全审计。不可把它直接标记为“已验证安全 RPC”。

执行状态包含 NORMAL、ERROR、TIMEOUT、AGENT、MANUAL、MULTIMODAL_AGENT。Agent 类型方法可打印结果后以特殊退出状态将结果交回模型，原研究记录 exit(8) 与 exit(11) 分别对应 Agent / Multimodal Agent 路径。

`dynamic_importer()` 可为缺失包执行 pip 安装并持久到 `/app/packages`。这扩展了能力，同时引入依赖与执行环境管理需求；Pry 1.0 不需要跟随。

## 4. MessageService：任务调度，不是长期角色状态机

MessageService 维护 `running_tasks`、`debounce_timers`、`pending_messages`。消息在 `AI_DEBOUNCE_WAIT_SECONDS` 内聚合；已有任务时不立即开第二个，完成后若仍有 pending 可再开任务，并有 `cancel_agent_task()`。

`push_human_message()` 的触发条件包括显式 trigger_agent、Plugin FORCE_TRIGGER、Persona 名、is_tome、随机和内容触发；还检查频道 active、observe mode、quota、plugin signal。

它主要回答“何时启动 Agent Job”，不能与 MaiCore 对等待、回复频率、Planner、行为经验等持续状态的社交调度混为一谈。取消函数存在也不证明外部副作用可撤销。

## 5. Memory：比普通向量 Top-K 更完整

`services/memory` 观察到 consolidator、embedding_service、episode_aggregator、qdrant_manager、query_service、retriever、scheduler、semantic_writer、maintenance、rebuild。

### 写入与调度

消息更新 `pending_messages`、`last_message_at`、`last_consolidation_at`，依据消息数、时间和连续跳过情况计算 LOW / NORMAL / HIGH / URGENT 优先级，以 heap + semaphore 后台 consolidation，而非每条消息都同步抽取。

```text
Raw Messages
→ EpisodicConsolidator（LLM 抽取）
→ Memory Paragraph：content / summary / knowledge_type / entities / relations
→ Entity / Relation / Embedding
→ Episode Aggregator
```

knowledge_type 包括 conversation、preference、fact、experience、decision、emotion。这是语义、事件与实体关系记忆，不应简化为“用户事实表”。

### 检索与强化记录

`MemoryRetriever` 从 embedding → Qdrant 搜索起步，再结合段落、实体关系图、Episode 候选。effective weight 考虑基础权重/衰减、语义相似度、Episodic boost、近期 boost 后排序。原研究还看到 retrieval access / reinforcement log，以及 `DBMemReinforcementLog`、`TriggerSource`、`TargetType`。

报告没有完整保留评分公式、全部默认参数和效果评测，不能凭这些符号宣称某检索算法最优。

## 6. Plugin、Adapter 与执行记录

`NekroPlugin` 能参与初始化/清理、Prompt Injection、用户/系统消息、Sandbox Methods、Webhook、FastAPI Router、Plugin WebUI、配置、存储、后台任务、命令和 Enable/Disable 生命周期。它已是应用级扩展系统，远大于 Pry 1.0 的小插件目标。

`BaseAdapter` 定义 init/cleanup、forward_message、self/user/channel info、reaction、runtime prompt、dialog examples 等，并使用 chat_key。它主要是进程内平台边界，不能等同于 Satori 的跨进程 Remote Bot 协议。

`DBExecCode` 的历史观察涵盖代码、输出、停止类型、模型、执行/生成耗时、触发用户和错误信息，报告还提到名为 thought chain 的字段。借鉴点是可追踪执行；该字段名称不证明内容一定是模型真实内部推理，也不要求 Pry 采集推理链。

## 7. 对 Pry 的建议：借鉴与不照搬

**1.0 优先借鉴**：MemoryCandidateQueue / 自适应异步沉淀的思想；不要每次回复结束必然调用一次抽取模型；用可观察的 TurnTrace 记录触发、Gate、记忆召回、表达选择、模型调用、Capability 和耗时。

**不要照搬**：Docker + Python 作为 Core 默认执行环境、动态装包、pickle RPC、Qdrant 与完整实体关系图、整个插件开放面；不要把 512 MB / 1 CPU 等历史参数当安全默认配置。

MaiCore 的社交/人物/行为/表达记忆与 Nekro 的语义/事件/实体关系记忆可以互补。但 Pry 1.0 仍优先事实、偏好、长期事件和关系连续性，复杂图谱不因此提前进入范围。

**未来**：Code Agent + Sandbox 可放入可选高级 Capability / Harness 插件，通过受限 Capability RPC 接入 Core。模型可靠写代码、资源限制、执行恢复、权限和环境管理需独立验证。

## 8. Caveat 与验证入口

- 无固定 commit、完整安全审计或当前版本验证；不能把“受控出口”的架构意图写成已证明无风险。
- 本地小模型生成代码的可靠性属于待测判断，原研究没有模型基准结果。
- 后续关注任务排队/取消、超时与错误恢复、Memory backlog/并发/重复沉淀、检索权重解释、RPC 授权和审计记录。
- 原研究没有证明所有这些路径已经具有 Pry 所需的幂等、崩溃恢复与隐私日志约束。
