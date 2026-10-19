# 能力与插件（RT-007 第一期）

本文写的是**执行侧的形状**：一次 `CapabilityRequest` 从宿主哪儿进、在哪儿被判该不该做、结果从哪儿回。
Runtime 那一侧的等待、续接与过期结果围栏写在 `docs/character-runtime.md`，不在此重复。

状态词：**第一期已落地，零用户可见新行为**。第二期（会话里真有人发起能力请求、`Ask` 落到一个能点的确认）
**没有开工权**，等作者按 `docs/decision-request-protocol.md` 的四段式裁定。

## 1. 这一版解决的是什么

RT-002 之后，Runtime 会发能力请求、会等结果、会丢弃过期结果，但**没有人执行**：宿主收到请求就回一份
`capability_unavailable`。第一期补的是"有人执行"这一段的地基——名册、权限档、分发器、两个出厂能力、
插件能拿到的东西——同时保证**今天一次外部启动都不会发生**。

"零可见变化"不是叙述，是两条独立成立的读数：

1. 会话里没有任何一处生产 `CapabilityRequest`。发起意图来自
   `SituationSnapshot.AvailableCapabilityNames`，它在 `src/` 内**零生产者**（只有
   `tests/Pry.Core.Tests/CharacterIntelligenceTests.cs` 手工塞值），所以 `BehaviorPlanner` 的
   `capability:none_registered` 那道闸门今天恒关。
2. 两个出厂能力的描述符档位都是 `Ask`，而 `Ask` 在分发器里就停在
   `capability_approval_required`，连参数都不看。

## 2. 类型住在哪儿

契约 `docs/phase2-runtime-contract.md` §7.1 结论 3 与 `docs/phase1-platform-baseline.md` §5.1 要求能力类型归
`Pry.Core.Abstractions`。本次搬迁已执行（`src/Pry.Core/Abstractions/Capabilities.cs:80,87`）：

| 类型 | 位置 | 谁引用 |
|---|---|---|
| `CapabilityRequest` / `CapabilityResult` | `Abstractions/Capabilities.cs:80,87` | Runtime 的 `UseCapability` 意图与 `RuntimeEvent.Capability(...)` 工厂，只引用不重定义 |
| `CapabilityPermission`（`Allow`/`Ask`/`Deny`） | `:95` | 描述符与名册 |
| `CapabilityArgument` / `CapabilityDescriptor` / `CapabilityRunResult` | `:103,109,116` | 能力实现的返回值与参数表 |
| `ICapability` / `IPryPlugin` | `:127,139` | 出厂插件与名册 |
| `PluginContext` / `PluginStorage` | `:150,157` | 名册在注册时构造，插件只拿到这三样加一个日志器 |

一句偏离，写在代码里也写在这里：`TurnIdentity` 仍住 `Pry.Core.Runtime`，而 `CapabilityRequest` 需要它，
所以 `Abstractions` 里那一格用 `using Pry.Core.Runtime;` 取得。**同程序集内的命名空间引用，不是新的层依赖**；
把它搬过来反而会让 `Runtime` 反过来依赖 `Abstractions` 的类型定义。

`Pry.Core` 不依赖 Avalonia 也不依赖 HTTP（`AGENTS.md` 工作规则 5）。执行与进程启动全部在宿主侧。

## 3. 宿主的三件

- **名册** `src/Pry.Api/Capabilities/CapabilityRegistry.cs`：回答"有没有这一项"和"它今天的档位是什么"，
  **不执行任何东西**。注册时给每个插件一份 `PluginContext`（id、显示名、自己那一格存储、名为
  `plugin:<id>` 的日志器），`CreateCapabilities` **每项能力只调一次**。
  描述符没有 id、或超时不是正数的注册被丢弃并写明；同一 id 重复注册按**先来者留**，后来者记一条错误后丢弃
  ——为一个重名让整个后端起不来，代价是用户今天开不了这个程序，日志里看得见是谁占了名字。
- **分发器** `src/Pry.Api/Capabilities/CapabilityDispatcher.cs`：判定顺序是刻意的
  ——**先名册、再权限、再参数**。每条拒绝路径都产出一份 `CapabilityResult`（缺一份回执那一轮就永远停在
  `capability_pending`，界面上一个字都不出）。调用方令牌取消时重新抛 `OperationCanceledException`，
  不翻译成失败（PLAT-001）；能力自己超预算才是 `capability_timeout`。
- **出厂插件** `src/Pry.Api/Capabilities/DesktopPlugin.cs`：仓库里唯一的 `IPryPlugin`。
  **没有加载器**——插件市场与任意程序集发现写在 `docs/full-scope-sprint.md` §3 第 4 层的不做清单里。

接线只有两处，且都由 `Program.cs` 完成：`src/Pry.Api/Program.cs:80-86` 注册启动器、插件、名册、分发器；
`src/Pry.Api/Services/ConversationSessionService.cs:287` 把分发器交给输出通道。摘掉任一处接线的洞由
`tests/Pry.Api.Tests/CapabilitySeamWiringTests.cs` 的源码门守着，不靠叙述。

## 4. 配置键（宿主明令是唯一的放行方式）

| 键 | 形状 | 出厂 | 读不出来时 |
|---|---|---|---|
| `Pry:Capabilities:Permissions:<capabilityId>` | `allow` / `ask` / `deny`（大小写不敏感） | 无键 | 落回能力自己的**出厂档**，并记一条警告。拼错的值（例：`postive`）不会被当成 allow，也不会被当成 deny——那都是在替用户做一个他没做过的决定 |
| `Pry:Capabilities:AllowedApplications` | 字符串数组，每项是**完整可执行文件路径** | 空数组 | 空表就是判定结果：今天没有任何一条用户可见路径能拿到一个可执行文件路径，所以没有任何一条路径需要被放行 |

数据目录沿用既有解析（`RuntimeDescriptorService.ResolveDataDirectory`，即 `Pry:DataDirectory`，缺省落
`PryProtocol.DefaultDataDirectory`）。插件存储格是 `<dataDirectory>/plugins/<pluginId>/`。

## 5. 错误语义

这些都是**结果码**，不是 HTTP 状态码：它们进 `CapabilityResult.ErrorCode`，Runtime 据此续接或静默。

| 码 | 谁产生 | 含义 |
|---|---|---|
| `capability_unavailable` | 输出通道（未接分发器的宿主） | 保持接入前的那份回执，逐字不变 |
| `capability_not_registered` | 分发器 | 名册里没有这一项 |
| `capability_denied` | 分发器 | 宿主明令 Deny |
| `capability_approval_required` | 分发器 | Ask 且还没人点头。**与 `capability_denied` 必须可分辨**：一个是"还没批准过"，一个是"明确不许" |
| `capability_argument_missing` | 分发器 | 必填参数缺失或全空白。权限判定已通过才会走到这里 |
| `capability_timeout` | 分发器 | 能力超出自己的 `TimeoutMs` 预算 |
| `capability_failed` | 分发器 | 能力抛异常。原文只进日志，不进她的回复 |
| `url_argument_missing` / `url_scheme_not_allowed` | `open_url` | 缺 `url`；或不是 http/https |
| `application_argument_missing` / `application_path_not_absolute` | `open_application` | 缺 `path`；或不是绝对路径 |
| `application_not_allowed` / `application_not_found` | `open_application` | 不在白名单；在白名单但本机没有。**顺序是刻意的**，反过来会把"这台机器上有没有某个程序"泄漏进一条错误码 |
| `capability_loop_not_supported` | Runtime（既有） | 一轮里第二次请求能力。第一期不做通用 Tool Loop |

## 6. 六条安全形状

1. **拒绝发生在系统调用之前**。参数校验与 scheme 白名单在 `IProcessLauncher` 之前完成，所以坏形状的判据是
   "启动器一次都没被调用"，不是"启动了但被拦下"。
2. `open_url` 只放 `http` / `https`。`file:` 会把本机目录端给浏览器，`javascript:` 把正文交给脚本引擎，
   裸路径等于"她说要打开什么就能打开什么"。
3. `open_application` 不接受参数列表，只接受一个完整路径——没有拼命令的缝。
4. **两个能力都不回显**它打开了什么。成功输出会作为"能力结果"进提示词，而链接里可以带访问令牌。
5. **审计行不带参数值**，只有 id、轮次坐标、档位与结论。日志会落盘、会被打包带走。
6. 插件拿不到数据库连接、`IServiceProvider`、密钥，也拿不到别人的存储格。

## 7. 判据与证伪（2026-10-19，载体 `.local-notes/rt007-capability`，起点 `88eff2a`）

门禁读数：`dotnet build Pry.slnx -c Release -t:Rebuild` 0 警告 0 错误；
四程序集 **939 通过／0 失败／2 跳过（总 941）**＝Client 93／App 221／Api 229／Core 398（396 通过＋2 跳过），
两条跳过仍是 `Configured_sense_voice_model_can_run_offline` 与 `Configured_llama_server_requires_the_child_environment_key`；
`measure-fixed-eval.ps1` 仍 75 条永久场景、`exit=0`（**本刀没有新铸永久 ID**）；
`verify-repository.ps1` 458 个受跟踪文件（新文件当时仍未受跟踪，故此数未涨）；`git diff --check` 干净。

新写的守卫逐个压过红：**22 次定向跑（另有 1 次搬回状态机的第一版在编译阶段就失败、没成跑，按规矩换文件名重做），20 处点名转红、2 处按预期保持全绿**，逐次跑法与红例名在
`.local-notes/gate-rt007-muts/*.trx`（载体之外）。三条如实分开记：

- **两处按预期保持全绿**：`PluginStorage` 的越界拒绝是双层守御（`..` 词法拒绝 + 折算后的前缀比较）。
  只删其中一层，那四条穿越判据仍全绿——**因为产品确实还安全**，另一层挡住了。两层都删才转红（4 条）。
  判据钉的是"出不出得去"这个契约，不钉每一层实现，这一条不记成缺陷。
- **一处第一版作废重做**：把两条 record 搬回 `Pry.Core.Runtime` 的第一版让 4 个文件编译失败
  （消费者仍从 `Abstractions` 取类型），那不是判据红。补齐"搬运者本来就要补的 using"之后重跑，
  命名空间判据转红，红点唯一。判据本体一字未改。
- **两处是证伪过程中补出来的判据**：`AllowedApplications` 这个配置键与"权限先于参数"的顺序，
  在补判据之前**删掉产品代码没有任何一条用例会红**。两处各加一条判据、各压红一次
  （配置键改成永远返回空表 → 只有新加那条红；参数校验提到权限之前 → 只有新加那条红）。
  这与 `QA-TIP1016-01` 是同族形状，本刀在自己名下当场关掉，没有留成登记。

## 8. 第二期缺的是什么

缺的不是代码量，是一句裁定：会话里要不要真有人发起能力请求（她能不能真的打开一个东西），
以及 `Ask` 落到哪里——一个能点的确认（要动 `Pry.App` 与设置面，受单一写者规则约束），
还是"没点头就拒绝"。裁定按 `docs/decision-request-protocol.md` 的四段式提，**在此之前不写第二期代码**。
