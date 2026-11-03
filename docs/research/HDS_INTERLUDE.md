# HDS-Interlude 源码研究：持续剧本驱动的 Roleplay

研究日期：2026-10-20。仓库：[ISEKAI-MomoiCore/HDS-Interlude](https://github.com/ISEKAI-MomoiCore/HDS-Interlude)。锁定提交：`1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090`（2026-10-20 16:42:28 +08:00）。package.json 版本：`1.0.1-rc57`。

面向 Pry 的 Architecture / Runtime / Intelligence / Memory / Platform / Client / QA 七个 Agent。本文记录实际阅读的源码、隔离抽样测试和由此提出的 Pry 建议。没有执行真实 QQ 或模型交互，没有复制上游源码、人设或用户截图进 Pry。

## 1. 结论与项目定位

**HDS 的核心是持续叙事：模型作为主角生活的写作者，在一段连贯剧情中决定角色注意到了什么、要不要回应，以及具体说什么；宿主负责约束时间、身份、状态提交和真实投递。**

它是 Koishi 插件形态的时间感知、持久化 Roleplay / 生活叙事系统。不是普通的“每来一条消息直接补一句回复”，也不是独立模型训练框架。对 Pry 最有价值的是：角色当前处境与动机如何进入生成，以及如何把剧本、意图和真实送达分开。

“给一些情景就能获得较好的初始表现”有源码机制支撑，但不应扩大成“任何模型、任何设定、没有历史都能稳定复现截图”。本次没有取得截图对应的模型、设定、完整上下文、延迟和未精选样本。

### 与 MaiCore 的区别应怎样理解

| 维度 | HDS-Interlude | 既有 MaiCore 研究结论 |
|---|---|---|
| 生成中心 | 连续生活剧本与主角处境；主叙事调用同时产生故事和通信决策 | Scheduler / Gate / Planner / Replyer 等分工的社交角色运行链 |
| 初始表现来源 | 预训练模型能力 + Canon/初始关系 + 写作规则 + 当下场景 | 预训练模型能力 + 人设/上下文 + 社交调度与表达/行为机制 |
| 长期变化 | 剧本、事实、关系、意图与非破坏性 Overlay 积累 | 记忆与行为/表达经验的持续更新 |
| 是否必须先训练权重 | 本次所读生产链没有训练权重前置步骤 | 既有研究也没有证明在线权重微调是启动前置条件 |

因此，不宜表述为“HDS 不训练，而 MaiBot 必须长期训练才能工作”。既有 MaiCore 报告中的“学习”主要涉及行为/表达经验库的积累与选择，不能自动解释为模型权重训练。本次没有重新阅读 MaiCore 最新提交，不作当前版本横向跑分。

## 2. 两张截图能说明什么

用户提供的效果图展现了：短句、多气泡、调侃、拒绝提供照片、围绕游戏和头发延续话题，以及被纠正后继续接话。这些是值得做成 Pry 质量观察维度的表现。

但截图不能独立证明：

- 角色最初只给了少量情景，或模型没有收到更完整的人设与示例；
- “记错了”来自长期记忆读取，而不是模型顺着当前对话补写；
- 同样效果在低参数量本地模型、多人长聊或重启后依旧成立；
- 展示中的生活陈述对应真实外部行为。

从源码推断，HDS 的短期效果与其“写角色自己的生活，再把聊天当作其中的行动”路线一致。这里是机制解释，不是截图效果的因果实验。

## 3. 架构和核心调用链

```text
Koishi 消息中间件
  → InterludeService.receive / receiveGroup
  → 授权、端点/参与者解析、入站去重、保存真实输入
  → TurnEngine：短时合并、revision、在途请求失效
  → 故事快照与上下文装配
  → 七段式 Context Compiler
  → Narrator：主叙事模型请求
  → 规范化 / ScriptCommit 结构校验
  → 剧本、状态、投递账本与意图持久化
  → 统一发送路径、打字与分段调度
  → 每段结果回写
  → 后台整理、到期意图、生活推进
```

入口与真实调用见：[插件入口][entry]、[私聊入口][receive]、[回合执行体][flush]。群聊有独立输入缓冲和资格门，但汇入同一叙事与投递体系。

### 3.1 Canon 与持续生活

初始设定提供角色、世界、关系与风格，之后保留原始剧本、活跃场景、事实、未完成事项和渐进变化。用户消息不再独占角色的世界：角色当下是否忙碌、是否在意、是否愿意开口可以成为生成依据。

生产系统提示词实际从 living-script 写作指导开始，附加格式/时间/现实性合约，以及管理员可配置的写作风格。另有模型家族与 lite 等特化分支。因此，“提供情景”背后仍有相当多宿主提示与结构工作。[写作基调][writing]、[生产提示词][prompt]、[模型特化][specialty]

这条路线能减少纯助手式“有问必答”的倾向，但是否自然、是否过度戏剧化仍由模型、设定和历史共同决定。

### 3.2 单次主叙事统一决策

一个主回合可以同时形成：

- 本时段已经发生的角色生活与心理叙事；
- 是否注意到当前输入、立即回复、延迟回复或沉默；
- 事实与状态变化候选、剧情余波、未来意图；
- 面向真实聊天渠道的通信内容。

这减少了“先单独规划，再由另一个无足够状态的回复器补一句”的语义断裂。但不是“整个系统永远只调用一次 LLM”：压缩、时间导演、Alter、Embedding、视觉等存在额外调用路径。[模型请求][narrator]

对 Pry 的启发是统一角色处境与可见表达的因果依据，不是必须将所有 Core 职责合并为一个大类或一个无限大的 Prompt。

## 4. 上下文与记忆：不是只有人设 Prompt

七段式上下文的实际字段为：

| 段 | 主要材料 |
|---|---|
| storyIdentity | 初始设定 |
| relevantEstablishedEpisodes | 原始近期剧本、场景、事实/记忆、演化、召回原文和近期真实交换 |
| currentSceneEvidence | 带来源的当前场景导航 |
| ongoingThreads | 参与者、剧情余波、承诺、联系证据链、未发送草稿、投递现实 |
| availableNearFuture | 近期日程、计划、到期意图 |
| incomingEvent | 本轮用户/群聊事件和通道能力 |
| authoringWindow | 阶段、时间窗口、继续写作边界与可选氛围/行动条件 |

[Context Compiler][compiler]

这里有三项可单独借鉴：

1. **原始材料是证据，摘要是导航。** 召回可返回原文邻域；SceneFrame 和 lifeHandoff 不另造一份覆盖原始剧本的真相。lifeHandoff 校验引用必须出现在本轮正文，但引用存在只证明来源，不证明自然语言推理正确。[原文导航][recall]、[交接引用][handoff]
2. **事实有认知身份。** knowledge 区分观察、转述、信念、提议、条件和确认；不能仅凭角色自己写出的叙述，就把另一方的承诺认定为已成立。[知识证据][knowledge]
3. **不同检索用途保留位置。** 活跃事实检索结合语义与叙事质量，并为已完成的重要事件、未完成承诺保留通道。Embedding 有失败降级，不是唯一记忆入口。[事实检索][memory]

cache-first 方式调整稳定前缀顺序并加入最近交换，适合研究前缀缓存。实际费用、缓存命中率和质量本次未测，不能保证它对 Pry 本地模型有同等收益。

### 4.1 Overlay 演化也需要积累

角色、关系、世界、Perspective 等变化以非破坏性 Overlay 候选保存；应用门槛包含置信度、新证据、回合/日期、冷却等，重大变化另受独立场景证据约束。[演化门槛][overlay]

因此，“初始人味不依赖先养很久”和“长期关系完全不需要积累”是两回事。后者不是本次源码支持的结论。

### 4.2 对 Pry 非常重要的隔离差异

当前 HDS 采用共享 canonical 主剧本、多参与者关系分支。**长期事实是故事级全局叙事证据，检索并不按旧 participantId 字段隔离；原始私聊材料另有可见性限制。** 隐藏原文也不意味着派生事实完全隔离。[事实检索][memory]、[共享架构说明][arch]

这是该项目的产品选择，不能直接判成漏洞；但与 Pry 的 User × Character 实例隔离要求不同。Pry 如吸收类似结构，事实、证据、召回、压缩、缓存和写回都必须沿用自己的作用域，不能仅靠提示词要求模型保密。

## 5. 时间、主动行为与打断

### 5.1 不只是一条定时主动消息

Scheduler 维护每故事最早到期唤醒、独占任务去重与失败冷却；Urge 可结合对话密度和生活节奏调节推进频率；Schedule Preplan 提供近期计划；Agency 则处理日程负荷、隐私空间、设备可用性等行动条件。[Scheduler][schedule]、[Urge][urge]、[近期日程][preplan]、[Agency][agency]

这些机制让主动联系可以来自角色当前生活。但全部搬进 Pry 会增加虚构生活模拟、后台调用和维护成本。Pry 可以先吸收“当前处境、未完成动机、静默许可、到期意图”这些小结构；是否引入复杂日程属于另一个产品决定。

### 5.2 首条提交是重要边界

TurnEngine 在首条回复提交前允许新输入使旧请求作废；执行体返回后复查 revision、obsolete/discarded、暂停和重置状态。新输入引起的替代可以重组批次，管理清空引起的 discarded 不应复活旧输入。[TurnEngine][turn]、[提交前复查][flush]

这首先是**逻辑失效与禁止旧结果提交**。不能只看到“取消旧请求”就断言底层网络请求已终止、旧请求的 token 费用一定节省。

默认完整响应与实验性提前首泡路径也有差异：后者在首条有效回复提前送达后，后续 JSON 收尾失败不会再自动 failover 重发可见回复，而是保留既有结果、安排缺失剧本恢复。本次只验证了相关抽样测试，没有验证真实流式服务商。[运行边界][arch]

## 6. 剧本、意图与真实送达必须分开

ScriptCommit 的结构校验检查时间窗、事件身份、顺序、气泡可重建性等；它不是文采评分器，也不是任意叙事真实性证明器。[校验][validator]

原始剧本行附带 deliveryActions，按 message / media / face / reaction 等 segment 记录结果。delivered/cancelled 不被迟到结果反转，failed 可重试；部分送达由聚合状态表达。[提交持久化][persist]、[投递账本][ledger]、[气泡投递元数据][delivery]

对 Pry 很有价值的是：

- “模型写她发了消息”不等于“用户收到消息”；
- 未发草稿、被打断的内容、未来计划不能进入已说出口历史；
- 故事状态与真实用户承诺要保留证据身份；
- 后台补偿与分段执行只能有一个发送归属。

**该设计仍不提供端到端 exactly-once 保证。** 平台已接受但回执或本地记账丢失时仍有不确定性；故事串行队列和 SQLite 写队列也不等于多进程分布式锁。[上游明确的边界与相应实现说明][arch]

## 7. 维护成本与未完全验证处

- InterludeService 仍同时协调故事、模型、存储、记忆、通道与投递；TurnEngine/Scheduler 只抽走部分状态管理。不要把“有模块名”误写为整个业务实现已经解耦。
- 单一主叙事承担较多文学与结构任务，对模型结构化输出和长上下文能力有要求。lite/家族特化说明作者在处理差异，但本次没有证明 Pry 2B 级模型能承担同等任务。
- 生活自动推进会生成额外剧本并可能触发后台调用；延迟、费用、故事重复和过度戏剧化都需实测。
- 小说中的生活事实属于设定世界；用户真实经历与真实通信必须另有证据，避免把自动续写写成用户现实记忆。
- 当前是 rc57；版本名本身不能证明稳定。本次独立检出的 typecheck 未通过，见下一节。
- 一些扩展已有表或设计材料，不等于运行闭环完成。本文不将作品表、跨渠道扩展等全部列为已验收产品能力。

## 8. 实际验证

环境：Windows，Node.js `v24.20.0`；独立临时检出。安装依赖时禁用生命周期脚本，显式补装 Koishi peer；没有修改上游受跟踪源码。未配置模型测试凭据，未连接 QQ/DSH。

| 检查 | 结果与含义 |
|---|---|
| 12 文件抽样 | 60 passed，0 failed，4 skipped |
| 3 文件运行时/投递回归 | 58 passed，0 failed |
| npm run typecheck | **失败**：tsconfig.json 扩展仓库外 ../../tsconfig.base.json，本次独立检出没有该文件；随后出现目标/模块解析等派生诊断 |
| 真实模型表现 | 未验证；4 项真实模型测试因凭据未设置跳过 |
| 完整 Koishi/QQ、长期连续性、性能与费用 | 未验证 |

第一组：

```text
turn-engine, scheduler, scene-frame, script-commit-adapter,
lived-writing, memory-continuity, p1-memory-navigation,
positive-narrative-prompt, typing-floor, streaming-reply,
history-recall-guards, model-behavior
```

均对应 `test/<name>.test.ts`。第二组为 `review-delivery-regressions`、`review-runtime-regressions`、`supersede-repro`。

开始时隔离安装缺少 Koishi peer，5 个文件未能加载；补齐后得到上述最终结果。这不是产品断言失败。相反，typecheck 的外部基础配置缺口仍保留，未通过临时伪造配置掩盖。[类型配置][typeconfig]、[真实模型测试入口][modeltest]

测试包含纯函数、宿主桩、生产方法调用和提示词结构断言。**提示词中存在某项规则，不等于真实模型已经稳定遵守。** 118 项通过只代表这次抽样，不能作为全套验收数量。

## 9. Pry 取舍与七角色阅读

| 取舍 | 内容 |
|---|---|
| 优先作为设计参考 | 当前角色处境、动机与回复共同生成；沉默合法；输入合并与过期结果失效；已发送/计划/草稿分离 |
| 结合既有系统改造 | 七段上下文的语义分区、认知证据标签、未完成承诺通道、原文邻域召回 |
| 单独评估 | 将叙事支架作为可选生成策略，与现有 Behavior/Expression 链做受控对照 |
| 暂不据此扩大范围 | 完整生活模拟、共享世界、多参与者关系、天气/QQ 空间/复杂日程 |
| 不沿用 | 故事级全局事实可见性；大型宿主协调类；把虚构生活陈述当用户现实事实 |

建议的对照实验不是“接上后觉得像人”，而是固定模型和人设后比较：普通角色 Prompt、轻量场景支架、完整叙事链。共同测量短句自然度、拒绝与主体性、纠错、虚构用户事实、连续性、首泡延迟、token 与长间隔恢复。截图只作为风格观察参考，不充当预期答案。

| Agent | 优先阅读 | 重点 |
|---|---|---|
| Architecture | 3、4.2、6、7 | 叙事主链、模块责任、实例隔离与实际交付边界 |
| Runtime | 3、5、6 | 回合替代、首条提交、延迟气泡与失败补偿 |
| Intelligence | 1、2、3.1、4.1 | 初始主体性如何建立，初始表现与长期成长分开 |
| Memory | 4、6 | 原文、信念、事实、承诺和投递证据；作用域必须重新设计 |
| Platform | 3、6、8 | Koishi 适配、端点身份、回执与独立构建复现 |
| Client | 2、5、6 | 分段显示与真实发送、中断体验；不展示内部剧本为用户事实 |
| QA | 2、7、8、9 | Prompt 断言与模型实测的区别、固定模型对照、假事实反例 |

## 10. 许可与研究边界

根 LICENSE 为 AGPL-3.0 文本，package.json 声明 AGPL-3.0。它不在 Pry 当前直接复用白名单内。本文只给机制分析与源码定位，不将上游源码、提示词正文或人设复制进 Pry。[许可][license]

本研究没有修改 Pry 产品实现，没有执行提交或真实账号联动。研究完成不等于决定将叙事生活模拟加入当前产品范围。

## 固定提交来源

[entry]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/index.ts#L613
[receive]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/service.ts#L2540
[turn]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/turn-engine.ts#L51
[flush]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/service.ts#L4414
[compiler]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/script/context-compiler.ts#L18
[narrator]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/narrator.ts#L997
[writing]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/script/lived-writing.ts
[specialty]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/specialization.ts
[prompt]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/narrator.ts#L1815
[persist]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/turn-persistence.ts
[validator]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/script/validator.ts
[ledger]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/script/delivery-ledger.ts
[delivery]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/delivery.ts
[memory]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/service.ts#L6474
[knowledge]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/script/knowledge-evidence.ts
[handoff]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/script/life-handoff.ts
[overlay]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/service.ts#L11002
[schedule]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/scheduler.ts
[urge]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/urge.ts
[agency]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/agency.ts
[arch]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/docs/ARCHITECTURE.md#L109
[modeltest]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/test/model-behavior.test.ts
[typeconfig]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/tsconfig.json
[license]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/LICENSE
[preplan]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/schedule-preplan.ts
[database]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/database.ts
[recall]: https://github.com/ISEKAI-MomoiCore/HDS-Interlude/blob/1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090/src/script/recall-navigation.ts

