# qq-bridge 源码研究：让 Harness 通过工具参与 QQ

研究日期：2026-10-20。仓库：[Derpyu520/qq-bridge](https://github.com/Derpyu520/qq-bridge)。锁定提交：`a56164e70a0521a7829138a42eee76066fde2b8b`（2026-10-18 09:43:23 +08:00）。package.json 版本：`0.2.0-r4`，Node 要求 `>=22.13.0`。

面向 Pry 的七个协作 Agent。本文区分源码确认、离线测试与未来取舍，不把 README 的效果或版本适配声明直接当成本机验收结果。未运行真实 QQ、DSH、安装向导或账号登录。

## 1. 结论与路线定位

**qq-bridge 的特色是把 QQ 社交操作变为受限工具，并复用外部 DeepSeek Harness 的会话和工具循环；二代模式下，模型主动读、发、等、决定下一次如何被唤醒。**

它同时承担 OneBot 消息接入、DSH 会话映射、社交状态机、工具代理、控制台与运维，因此已经超出“消息格式转换器”。但核心任务执行能力来自外部 DSH，它自身并不是新的完整 Harness。[依赖声明][pkg]、[DSH 客户端][dsh]

也不能把它与 Satori 等价：Satori 研究聚焦通用跨平台 Bot 协议/远端对象；这里聚焦一个 QQ 网关到具体 Harness 的适配，以及其上的社交 Agent 行为。

| 系统 | 主要运行对象 | 模型的典型行动方式 |
|---|---|---|
| HDS-Interlude | 主角持续生活剧本 | 续写与结构化叙事决策，再由宿主投递 |
| MaiCore（沿用原研究） | 社交/角色运行时 | 调度、行为规划、表达与学习链 |
| qq-bridge 二代 | QQ 会话、未读窗口、唤醒条件 + 外部 DSH session | 通过工具查看消息、发送、等待、记忆、更新唤醒条件 |
| Satori（沿用原研究） | 平台账户、事件与远程 Bot 抽象 | 提供传输与动作协议，本身不规定角色智能 |

## 2. 部署与数据流

```text
QQ
 ↔ SnowLuma（外部安装、登录的第三方网关）
 ↔ OneBot v11：WS 入站 + HTTP 动作
 ↔ qq-bridge Node 进程
      ├─ 准入 / 会话映射 / 未读与唤醒 / 发送与审计
      ├─ 本地控制台及 Agent 状态 API
      └─ DSH Web API：Cookie + slash RPC + remote.mux
                           ↕
                   外部 DSH session / preset / 模型工具循环
                           ↕
                 stdio MCP 安全 QQ / host status / web search
                           ↕
                  回调 bridge 或 OneBot 动作
```

[网关初始化][gateway]、[入站处理][incoming]、[MCP 工具][tools]。

每个私聊/群聊有独立 DSH 会话映射并保存于本地 state。角色文件用于提示注入，仿真 preset 负责行为协议。独立会话有助于分开上下文，但不能据此证明进程、凭据、所有全局资料都完全隔离。

QQ 通道依赖第三方 SnowLuma/OneBot 网关，不应写成腾讯官方 QQ Bot API 适配；本次没有审计 SnowLuma 的原生 QQ 接入实现或确认其最新兼容性。

## 3. 模式差异：避免把几条链混为一谈

| 模式 | 入站与响应 | 工具/权限边界 |
|---|---|---|
| chat | 白名单聊天输入形成提示，完成文本可回传 | 安全聊天 preset |
| reserved（一代） | 本地观望、活跃、试探、退场等状态机决定参与；支持静默标记与特定拆句方式 | 安全聊天 preset + 本地社交调度 |
| reserved2（二代） | 消息进未读，满足条件后唤醒；模型通过工具完成真实收发 | qq-chat-v2、会话令牌、工具限制与桥接门控 |
| closed-agent | 仅 owner 私聊的远程任务入口 | 可使用完整 DSH preset；不能当成公共群聊配置 |

严格 preset 选择在非 closed-agent 模式下避免回退到带本地执行能力的默认 preset；缺失安全 preset 可以导致拒绝建会话。[preset 选择][presetselection]

源码接受 `simulation` 别名并存在下一代 SessionRouter，但当前 bridge 的主要分支仍使用历史模式名。**本次搜索生产 src/bridge.js 与脚本，没有找到对 src/core/session-router.mjs 的接线；该模块当前只有自己的测试导入。** 不能把它已通过的“每会话路由”测试，宣传为生产桥接已完成这轮重构。[独立路由模块][router]、[其测试][routertest]

## 4. 二代的核心循环

```text
收到真实 QQ 消息
  → 当前模式/白名单准入
  → 规范化引用、发送者、图片/表情等
  → appendSocialV2Message：顺序水位、未读、近期记录
  → 首次 bootstrap 或评估唤醒条件
  → 在外部 DSH 对应 session 投递唤醒提示
  → 模型按需调用：
       get_prompt / get_unread / get_recent
       send_message / reply / send_burst
       wait_for_messages
       mark_read / set_wake_config
       memory 等
  → 桥接校验令牌、目标、状态与额度
  → 真正发送、更新状态与下一次唤醒
```

[二代入站][v2in]、[触发条件][wakeconditions]、[唤醒执行][wake]。

### 4.1 真正的发送是工具行为

reserved2 的普通完成文本仅作内部输出，不会自动发送到 QQ。真实发送通过工具进入桥接发送链；其它模式也会记录工具发送成功，避免随后再自动转发相同回合。[不自动转发分支][noautoforward]、[发送工具][send]

对 Pry 的参考价值是：可以明确区分“模型生成了一段文本”“角色提出一个行动”“行动已经发生”。不过，Pry 没有必要为获得这种区分而引入完整 MCP Host 或把整个 Runtime 交给外部 Harness。

### 4.2 等待是显式操作

工具区分首答前短静默和回复后等待新消息；还有离开/潜水前的观察窗口。首答静默按最后输入重新计时，已花掉的阅读与思考时间可计入；“没人回复”与“对方还没说完”不是同一条件。[等待工具][wait]、[等待辅助][v2wait]、[真实 handler 的夹具测试][replytest]

提示词、工具建议与桥接硬限制共同影响行为，不能简单说“所有节奏完全由模型自由决定”。长轮询也不是永久无成本驻留：工具超时、DSH 回合限制、断连、唤醒租约和调用开销仍需维护。

对 Pry 可以吸收的语义是“等补充”“等回应”“决定暂不参与”，再由本地 Scheduler/ReplyGate 执行；不应直接移植固定五分钟观察作为桌面私聊的统一体验。

### 4.3 未读水位避免误清

模型查看到哪里，与后续又进来了哪些消息，需要有不同水位。throughSeq/readThroughSeq 校验限制已读提交，避免清除读取后才到达的新消息。源码仍保留省略 throughSeq 的旧兼容清空行为；因此不能把水位保证描述为所有调用绝对强制。[已读水位][readwater]、[工具约定][tools]

这是 Runtime/QA 很值得吸收的机制：确认已消费的批次，不能等于清空此刻全部 pending 输入。

### 4.4 唤醒与故障恢复并非“永远自行想起”

桥接仍负责忙碌状态、队列、定时器、回合结束跟踪、未设置下一次唤醒的提醒与默认兜底。模型忘记收尾、turn/end 丢失、离线队列恢复等，都有显式补救逻辑。[唤醒执行][wake]、[事件收集器][collect]、[故障回归][regressions]

模型自主选择的能力，依赖这些宿主状态和恢复路径，不是仅靠一段“像真人聊天”的指令。

## 5. 安全设计：可学习的机制与不能省略的限制

### 5.1 两层工具约束

仿真 preset 同时使用：

- 注册/继承层 restrict：隐藏已知危险全局工具；
- 执行层 guard：只允许指定 QQ、状态、搜索 MCP 命名空间和少量明确工具；
- 桥接端：模式准入、白名单、会话令牌、目标与内容审计。

[工具限制实现][guard]、[二代 preset][preset]。

未知或改名工具可能不在 restrict 表中；guard 仍提供执行兜底，但允许整个 MCP 前缀也意味着新增同命名空间工具需要继续审查。它是纵深约束，不是形式化沙箱隔离证明。

### 5.2 管理端与模型调用区分

MCP 通过本机 Agent API 访问二代状态，带调用标记与对应会话令牌；HTTP 层把这类调用与持有 consoleToken 的管理员区分，端点还校验具体会话。对未带有效会话令牌的调用有集中限制，减少误落到管理端权限的机会。[HTTP 门控][guardhttp]、[控制台处理][console]

模型可见字段投影也单独封装，避免把管理状态对象直接交给模型。[模型视图][modelview]

这些设计值得 Pry Platform 参考，但正则内容拦截、会话令牌和提示词都不能承诺消除任意 Prompt Injection 或恶意宿主插件的风险。

### 5.3 DSH 鉴权存在明确版本耦合

当前客户端会读取本机 DSH 凭据文件中的浏览器会话签名密钥，用 HMAC 在本地生成有期限、绑定 authority 的 Cookie；还有显式 launch token 交换与失败回退路径。[密钥读取][auth]、[Cookie 构造][cookie]

这没有证明它绕过 DSH 同用户本地安全边界；但它**依赖服务端内部密钥存储和 Cookie 格式，并取得相应浏览器会话权限**，不能当成稳定、最小权限的公开适配协议。

对 Pry，值得吸收的是凭据作用域、轮换、失败语义和权限区分；不建议借鉴读取宿主签名密钥这一具体集成方式。未来对外集成应定义明确的授权凭据，而不是承诺永久兼容另一应用的内部认证实现。

### 5.4 工具数据边界

safe-fetch 对 URL、DNS 结果、重定向、内网地址、字节和时间预算进行检查；搜索与媒体共用相关传输。[安全抓取][safefetch]

这些是实际代码机制。本次只做离线测试与源码阅读，没有进行完整网络攻击面审计。

## 6. DSH 协议适配的参考价值

当前适配包含：

- `/api/<namespace>/<method>` 的 slash RPC 与参数包装；
- `/api/remote.mux` 上的 session/follow 及交互事件；
- 提问/审批的对应回执；
- 断线后重放期望 follow 集合，并退役失效 session；
- session/projections 主路径读取 inbox，旧 session/control baseline 形状回退；
- 会话重置时清理桥接映射、旧任务、跟踪器与令牌。

[DSH 客户端][dsh]、[mux][mux]、[队列兼容][inbox]。

本次没有运行真实 DSH，所以只确认这些适配代码存在及所选夹具测试结果，不独立确认作者宣称的所有 DSH 版本都兼容。follow 重连也不自动等于无遗漏事件恢复或 exactly-once。

维护上的教训与 Satori 研究相通：文档、schema、认证、队列投影和事件形状都会演化，适配器应锁版本并保留可复现夹具。

## 7. Memory 与学习

二代存在会话级 activeTopics、pendingThoughts、memberImpressions 等 JSON 状态，提供相关工具读写、过期清理与容量边界。[记忆状态][memory]

黑话系统另外维护候选、确认、拒绝状态及语境证据，限制条目与字段，并可通过 learner/搜索补充解释。[黑话系统][slang]

这属于运行时资料积累与检索/注入，不能据此写成训练模型权重或证明关系学习已经成熟。它也不是 HDS 的持续剧本世界。

配置示例默认开启黑话自动研究。启用路径可能把从群聊提取的词和相关查询交给 DSH/模型或搜索后端；资料最终保存在本地不等于处理过程完全离线。[默认配置][config]、[搜索实现][search]

对 Pry Memory 的价值主要是：候选与确认分开、保留证据、区分读失败和空库、容量预算、过期和模型可见性。群聊共享黑话与用户私人记忆应继续分层。

## 8. 工程与部署 caveat

1. **bridge.js 很大。** 此快照约 11,775 物理行（包括注释），集中控制台、社交调度、发送、媒体与会话协调。它是机制参考，不适合整块移植成 Pry 的大 ChatService。
2. **生产架构和下一代模块并存。** SessionRouter 单独测试通过不代表生产已接线；不能把计划中的 QSH 改名、替换依赖或每会话模式写成全部完成。
3. **依赖有安装补丁。** postinstall 对 @snowluma 包的 ESM 相对导入进行机械修补；“npm 能安装”不等于无需适配第三方包。[安装补丁][patch]
4. **setup 脚本会修改宿主配置。** 会安装 preset/MCP 并写 DSH profile 等；研究时没有执行，不应在用户环境静默运行。[部署脚本][setup]
5. **没有验证 SnowLuma 原生链路。** 本次未登录 QQ，未运行桥接主进程，也未连接真实 DSH；真实媒体、审批、断线恢复另需端到端检查。
6. **吞吐与费用未测。** 长工具等待、自动唤醒、黑话研究和后台调用都可能产生资源成本；看板有计量机制不等于已核实计费价格或总成本。

## 9. 实际验证

环境：Windows，Node.js `v24.20.0`。临时检出用 npm ci 安装锁定依赖，显式禁用生命周期脚本。没有运行 setup、没有读取真实 DSH 凭据、没有发送 QQ 消息。

| 验证 | 结果 | 证据范围 |
|---|---|---|
| 4 个 Node test 文件 | 51 passed / 0 failed | 包含真实 bridge handler 的夹具测试，以及独立 SessionRouter 单测 |
| test-token-economy.mjs | 36/36 passed | 未读水位、模型视图、唤醒/暂停/离线队列等真实 handler 夹具 |
| test-hardening.mjs | 脚本 exit 0，36 条成功断言输出 | 资料清理、容量、读失败与现有价格表逻辑；不验证真实价格 |
| test-media-search.mjs | 脚本 exit 0，14 个 PASS 场景 | 媒体/搜索与地址切换桩；不访问真实搜索/CDN |

第一行文件：

```text
scripts/test-reply-wait.mjs
scripts/test-qq-preset-contract.mjs
scripts/test-audit-regressions.mjs
src/core/session-router.test.mjs
```

测试使用项目提供的 audit-bridge-harness：临时配置、文件、假 QQ/DSH peer，部分启动回环测试服务器。它没有导入并运行真实桥接主进程。[夹具][harness]、[token economy 场景][tokentest]

上述几类计数口径不同，本文不把脚本断言数与 Node test case 数相加作为统一总量。没有执行需要在线 DSH 的 test-dsh-auth、真实登录、完整安装或全套脚本。

## 10. Pry 应吸收什么

| 取舍 | 建议 |
|---|---|
| 优先作为 Runtime 参考 | 消费水位、等待补充/回应的区别、沉默作为合法行动、唤醒租约与失效恢复 |
| 优先作为 Platform 参考 | 最小工具面、模型/管理接口分离、会话凭据、模型可见 DTO、重连订阅与版本夹具 |
| 可作为未来可选能力 | 将有限外部动作暴露成 Capability；外部 IM 作为独立适配层 |
| 不直接沿用 | 读取宿主会话签名密钥；大型桥接协调类；把公共群聊和完整本机工具放同一 preset |
| 不因此加入当前范围 | DeepSeek Harness 强依赖、完整 MCP Host、公共 QQ 接入、长驻通用工具循环 |

对 Pry，最合理的吸收方式是把“读/等/说/暂不参与”转译为内部意图与 ResponsePlan，由自己的 Runtime 执行。这样可以研究社交行动选择，同时保持本地独立角色的生命周期所有权。

| Agent | 优先章节 | 重点 |
|---|---|---|
| Architecture | 1–3、6、8 | Harness 与桥接边界、计划/接线状态、维护成本 |
| Runtime | 4、6 | 等待语义、消费水位、忙碌租约和恢复 |
| Intelligence | 3、4、7 | 主体性来自模型选择与宿主规则共同作用，不能只堆反 AI 话术 |
| Memory | 4.3、7 | 未读和长期资料分层、证据、空库/失败区别 |
| Platform | 2、5、6 | 首要阅读对象：权限、凭据、工具面、版本兼容 |
| Client | 3、4.1、5.2 | 内部文本与真实外发、管理入口可见性 |
| QA | 5、8、9 | 夹具是否覆盖生产路径、模块存在但未接线、在线验收边界 |

## 11. 许可与复用边界

根 LICENSE 的项目代码部分为 MIT，后续第三方说明单独列出 DSH 与 SnowLuma 依赖边界；它不能被概括为“整个依赖栈都是 MIT”。其中关于 SnowLuma EULA、npm 许可文件缺失等，是该仓库作者的记录，本次没有重新审计上游授权链。[许可与第三方声明][license]

当前 host MCP 只提供状态探测，源码没有按旧能力去自动启动/停止 SnowLuma；研究也没有执行部署。[host 工具][host]

qq-bridge 不在 Pry 当前直接源码复用白名单内。本文仅作机制分析，不复制实现/preset/角色素材，不改变 Pry 代码、美术或上游许可规则。

## 固定提交来源

[pkg]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/package.json
[gateway]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/bridge.js#L11515
[incoming]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/bridge.js#L10517
[v2in]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/bridge.js#L10674
[noautoforward]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/bridge.js#L11358
[wake]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/bridge.js#L10080
[wakeconditions]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/bridge.js#L9846
[readwater]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/bridge.js#L9913
[collect]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/dsh-client.js#L932
[dsh]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/dsh-client.js#L296
[auth]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/dsh-client.js#L97
[cookie]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/dsh-client.js#L119
[inbox]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/dsh-client.js#L480
[mux]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/dsh-client.js#L646
[tools]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/mcp-snowluma-safe.js#L534
[send]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/mcp-snowluma-safe.js#L692
[wait]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/mcp-snowluma-safe.js#L760
[v2wait]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/v2-wait.js
[guard]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/plugins/qq-agent-presets/qq-tool-restrict.mjs#L174
[preset]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/plugins/qq-agent-presets/presets/qq-chat-v2.patch.yml
[presetselection]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/bridge.js#L2634
[console]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/bridge.js#L3083
[guardhttp]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/bridge.js#L3235
[modelview]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/qq-model-view.js
[memory]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/bridge.js#L8645
[slang]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/slang-learner.js
[search]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/web-search.js
[safefetch]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/safe-fetch.js
[router]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/core/session-router.mjs
[routertest]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/core/session-router.test.mjs
[patch]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/scripts/patch-snowluma-sdk.mjs
[setup]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/scripts/setup-dsh.mjs
[config]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/config.example.json
[license]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/LICENSE
[host]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/src/mcp-host-server.js
[harness]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/scripts/audit-bridge-harness.mjs
[regressions]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/scripts/test-audit-regressions.mjs
[tokentest]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/scripts/test-token-economy.mjs
[replytest]: https://github.com/Derpyu520/qq-bridge/blob/a56164e70a0521a7829138a42eee76066fde2b8b/scripts/test-reply-wait.mjs

