# 工单 HDS-WAVE20-1023｜补上旧链退役那 5 处缺口的等价的隔离原型（第十九波处置表片 4 的"先建后删"那一半）

- 工单 id：`HDS-WAVE20-1023`
- 任务编号：`HDS-010` 第一片后续（`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS-010 行；处置表＝`docs/eval/reports/2026-10-23-hds010-legacy-chain-disposition.md` 表 C 片 4）
- 性质：**动产品代码的隔离原型刀**。新建类型＋新建判据，**不接生产、不落库、不碰界面**。
- 执行方：冷启动 lane（第二十一号载体之后按波次号记「第二十波」）
- 出闸：守门人 2026-10-23 17:5x，作者原令逐字＝「我不懂啊，你干脆能做的一次全做了，直接大跨步开发疯狂挂子智能体就好了，我想快些结束这个项目」＋「旧的链路丢到废案里面，但是构建时候不要把旧链路打包进去，保存一份旧链路最后可以构建的快照」

## 〇 开工前必读与硬边界（缺一条就就地停刀登记，不要自己扩范围）

〇.1 **先读五份**：`docs/plans/README.md`、`docs/plans/PROJECT_PLAN.md`（§4.2 的 HDS-003／004／005／010 四行）、`docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md`、`docs/agent-engineering-standards.md`（全文，尤其 §10 交付形态）、`docs/legacy-chain-deprecation.md` 与本工单。处置表（上面 §〇 引的那份报告）的**表 C 片 4 与表 D** 是本刀的正面依据，逐行读。

〇.2 **入场读数**（当场实测、原样贴进报告 §〇，不许引用本工单的数字）：`git status --short --branch`、`git rev-parse HEAD`、`date`。主树此刻在 `e8a842a` 之上另有他人名下未提交项（两枚 `M ` 暂存文件与若干 `??`），**一律视为别人的资产：不读内容、不改、不提交、不 `stash`、不 `clean`、不 `rm`**。

〇.3 **载体**：`git worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave20-1023/carrier <线尾全号>`。**从当下线尾起**（开工当场 `git rev-parse codex/ui-main-window-redesign`），不许从 `def0faf` 或更旧的锚点起——那会把第十六／十七波刚落地的协调层与落库面拆掉。入场两行判据＝`git status --short | wc -l` 为 0、`git rev-parse HEAD` 等于你登记的线尾全号。

〇.4 **允许文件面（只有这些）**：
- `src/Pry.Core/Narrative/` 目录内的**新建**文件（建议命名见 §一）；
- `tests/Pry.Core.Tests/` 目录内的**新建**测试文件；
- 本报告 `docs/eval/reports/2026-10-23-hds010-gap-parity-prototype.md`。

〇.5 **禁止（逐条都是硬线，违反一条即作废本轮）**：
1. **不改任何既有文件**——包括 `src/Pry.Core/Narrative/` 既有十份契约／原型文件、第十六波刚落地的 `NarrativeLifecycleCoordinator.cs`、第十七波刚落地的 `Storage/` 两枚。接线是另一把刀的活，本刀只做"新类型＋等价格"，把"接进协调器"登记为**未做**。
2. 不接 `src/Pry.Api`／`src/Pry.App`／`src/Pry.Client`／任何端点／任何界面；不新增对 SQLite 的依赖；不碰 `%LOCALAPPDATA%\PryCompanion`；所有临时库／临时目录一律在系统临时目录下自建并收尾删除。
3. **不新造数字门槛，也不改既有门槛**。在册零改动清单＝检索 `.7`／`.5`、`2400`、五档超时、`MinimumUsableTokensPerSecond = 12`、`4096`、`DefaultSafetyTokens = 128`、`MinimumInputTokens = 512`。凡本刀需要的时间／节奏／批量参数：**一律从入参传入**；仓库里找不到权威值的，**登记为"待标定（押到标定轮）"并让入参必填**，不许在源码里写一个字面量常数顶替。
4. 不引用旧链四件（`BehaviorPlanner`／`ReplyGenerationRenderer`／`ReplyPlanner`／`ReplyNecessity`）；**读旧链代码取机制是允许的**（那是本仓库自己的 AGPL 代码），但新类型不得 `using`／构造／转发它们，并要有一枚源码扫描格自证（形状照第十六波的 T-sourcescan：自带阳性探针）。
5. 不 `git tag`、不 `git push`、不执行 `scripts/package-release.ps1`、不 merge、不 FF `main`；`licenses/**`、`licenses/THIRD-PARTY-NOTICES.md`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md` 一字不碰；模型／运行库／数据库／密钥／日志／用户导入内容／无权素材不得提交。
6. 不写状态词与结案词（"已完成"／"结案"归作者与 QA）；不自封基线；不改任何既有断言／阈值来凑绿；拿不到的红一律写"未取到红"。
7. **一切能验证的才写；不能验证的一律写「未验证」**，不许把推演写成实测。

〇.6 **焦点预检（这条会打断作者本人，违反＝本波作废）**：每一次 `dotnet`（含 build／test／任何工程）之前必须先在主树跑

```
powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1
```

把整段输出贴进报告；只有 `VERDICT=BUILD_OK` 才允许起跑；读到 `HOLD` **就地停刀、不请示、不改预检判据、不绕过它**，登记后结束本轮。`free_memory_gb` 低于 4 或读数呈 SQLite `disk I/O`／`out of memory` 形态时，那一轮读数作废（那是他机器的资源形状，不是断言失败），不动任何进程。

〇.7 **预算（用满即停，不许自扩）**：解决方案级构建 **≤1**（一个载体只构建一次）、全量 **≤1**、定向 **≤8**。省法＝判据先在脑内成形再写盘；每批定向跑一次覆盖多枚突变时按红名逐格归属。**跑法固定写法**（这三条坑都踩过）：`dotnet test Pry.slnx -c Release --no-build --results-directory "<绝对路径>" --logger trx`——短参 `-r` 会把结果拼进各工程 `bin` 目录导致"零测试仍 exit=0"；不给 `--logger trx` 目录里一个 TRX 都没有；**不同批次不要用同一个 `LogFileName`**（第十七波四工程同名 `final-full.trx` 互相覆盖，只剩一份）。跑完先数 `*.trx` 到四份再读 `<Counters>`。

〇.8 **报告先建、逐批落盘**（冷启动 lane 有 150 轮硬上限，撞上限时报告必须已经是可读的半成品）：动第一次 `dotnet` 之前先把 §〇 写满建文件；此后每完成一批就追加一次。交付形态按 `docs/agent-engineering-standards.md` §10 四项＝问题与根因／实际改动／验证结果／剩余问题。

## 一 任务本体：把处置表数的 5 处缺口各做成一个隔离类型

处置表 §一 与表 C 片 4 数出：新链今天在**用户可见能力**上还欠 5 件事，删除片因此不可起跑。本刀把这 5 件事各做成一个可判定的类型＋一组判据，全部在内存里闭环，不接线。

| # | 缺口（表 D 那行大白话） | 旧链现在的出处（**只读机制，不得引用**） | 本刀要交付的新类型（建议名，可自决但要在报告里点名） | 必须钉住的语义 |
|---|---|---|---|---|
| 1 | 你连着发几条，她攒成一轮再答 | `src/Pry.Core/Runtime/InputProbe.cs`、`TurnScheduler.cs` 的攒批段 | `NarrativeInputBuffer` | 同一会话的连续输入按到达次序归入**一枚**批次；封顶条件（条数／时限）全部走入参；到期或被顶掉时批次标识稳定可回放；同批次不产生两条可发送真相 |
| 2 | 你正在打字时她先停 | `Runtime/Interruption*`、`ConversationRuntime` 的打断段 | `NarrativeInterruption` | 打断请求只作废**尚未送达**的段；已送达段不可撤回（与 `DeliveryLedger` 既有终态语义一致，**不改它一字**）；打断与"新输入顶替"必须是两种可点名理由 |
| 3 | 她判你这句话是"接着说"还是"新话题" | `TurnGates.cs`／`ReplyNecessity.cs` 的形状判定段 | `NarrativeInterruptClassifier` | 分类结果是**枚举**且每值都有判据；"未知／不可判"必须是独立一档，不许折叠成"新话题"或"接着说" |
| 4 | 她的表情／口头禅会不会被场景挡 | `Intelligence/BehaviorPlanner.cs` 的行为决定产出段＋表达侧消费者 | `NarrativeBehaviorExpressionMapping` | 只做"行为决定 → 表达档位"的**换形**，不产文本；被场景挡时给出可点名的拒绝理由；行为决定本身缺失时不得默认放行（缺省档必须是显式枚举值） |
| 5 | 她说话一句一句按阅读节奏冒出来 | `TurnTaking/VisibleReply.cs`、`MessageIntervalStats.cs` 的节奏段 | `NarrativeReplyPacing` | 节奏只决定**下一段何时可发**，不决定内容；每段的"可发时刻"单调不减；速度参数从入参取（见 〇.5 第 3 条），无权威值就登记待标定 |

要求逐条：
- 五个类型都要能**只靠内存对象**跑完判定，不依赖 SQLite、不依赖网络、不依赖任何 Avalonia 类型（Core 不依赖 Avalonia／HTTP 这条架构线不许破）。
- 每个类型至少一组判据把"改坏了必须有人响"钉住：语义正例＋至少一条**拒绝侧**判据（理由码可点名）。
- 与既有契约面的关系要写清：本刀的类型是**新增词汇**，不得改 `NarrativeDocuments.cs`／`NarrativeCommitWriter.cs`／`DeliveryLedger.cs`／`OutboxOwnershipRegistry.cs` 的任何成员、条件、阈值或枚举取值；若确实需要既有类型开口子，**停下来登记为"押接线刀"，不要自己改**。

## 二 反证要求（每枚新落门都要响一次）

- 十一项字段逐轮登记（①文件:行 ②改前原文 ③改后原文 ④`git diff --stat` ⑤跑法整行 ⑥TRX 文件名 ⑦红格全名＋消息要点＋红点行号 ⑧还原命令 ⑨还原后 `git status --short` 行数 ⑩突变态与还原后 SHA-256 ⑪当轮预检整段输出）。
- 判据口径＝**单枚、单轮、清洁红**：一次只放一枚突变，恰好该格红、其余格绿；同一轮里多格同红要按消息形状与红点行号归因，不得合并计为新证。
- 拿不到的红按"未取到红"登记，并写清为什么（防御纵深／兜底支／住在禁改的既有件里）；**不许为了让它响就放宽断言、固定返回值、吞异常或 `Skip`／注释掉任何一格**。
- 结构上红不了的断言要当场指出（第八波 `DEF-GREENGATE-01`、第十三波那 9 组都是这类），指出承重断言实际落在哪一行。

## 三 出口读数要求（缺一格就报哪一格缺）

1. 构建读数（0 警告 0 错误原样）；2. 全量四份 TRX 的 `<Counters>` 逐份贴＋两条跳过按 `outcome="NotExecuted"` 点名（`notExecuted` 计数器恒 0，不看）；3. 定向轮次数与每轮红名清单；4. `git diff --stat <线尾>..HEAD -- src tests scripts Pry.slnx Directory.Build.props` 原文（应只见新建文件）；5. 载体 `git status --short` 收尾行数＝0、每轮还原后 SHA-256 与基线一致；6. `dotnet` 调用总次数（只认盘上产物）；7. 新增判据格数＝**逐文件数 `[Fact]`／`[Theory]`＋`[InlineData(` 之后，再说明与 TRX 分项的差**（本仓 Core 有 1 个抽象基类与 1 个 `MemberData` Theory，静态计数与 TRX 不等价；新格若用这两种形状必须点名）。

## 四 不许走的捷径（每条都有前例）

1. 不许"接个线"名义去改既有协调器／落库面文件；2. 不许把节奏／攒批参数写成源码常量凑一个能跑的数；3. 不许把"未知分类"折叠进某一档来少写一格判据；4. 不许用固定返回值／吞异常／`Assert.Skip` 让红消失；5. 不许按 `**` 奇偶或任何未证伪过的仪器去判文档形状；6. 不许替别人提交名下文件、不许裸 `git commit`（本角色在共享脏树里刚犯过一次，见 §9 同日 entry）；7. 不许把 §一 的 5 处缺口写成"已补齐用户可见能力"——**没接线就没有用户可见变化**，这句只能写成"隔离原型已就位、生产仍走旧链"。

## 五 仍未验证（报告里必须有一节，逐条点名）

真实模型对照（零读数）、生产接线（新类型零生产调用方）、UI 可见行为（零读数）、生产库（零接触）、与 HDS-007 到期再叙事的关系、待标定参数的权威值来源。

## 六 归位通道（给守门人，不在本 lane 权限内）

主树共享脏树里**不 `cherry-pick`**：在隔离载体 `git worktree add --detach` 后 cherry-pick，再把新文件按字节铺回主树并以 `git commit --only <显式路径>` 提交，出口用 `git show --stat` 数文件数。归位需一轮构建＋一轮全量，与守门人的复采轮合并起跑。

—— 完 ——
