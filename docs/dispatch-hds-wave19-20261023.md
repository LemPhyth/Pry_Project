# 第十九波工单·HDS-010 第一片：旧主链逐文件处置表（零构建、只读、不删一字节代码）

- 工单 id：`HDS-WAVE19-1023`
- 作者：守门人（架构／集成／发行）
- 出闸时间：2026-10-23 16:33（当场 `date`）
- 载体基点：`d82cd2d182e3c433939588ac79f82f04cbeda151`
- 执行方：冷启动 lane（除本工单与 `AGENTS.md` 四份必读之外无上下文）
- 性质：零构建只读分析刀。全程不得调用任何 `dotnet`；不得改动 `src/`、`tests/`、`scripts/` 的任何一个字节。
- 作者裁定原文（本单的存在理由，逐字）：「旧的链路丢到废案里面，但是构建时候不要把旧链路打包进去，保存一份旧链路最后可以构建的快照」
- 裁定的登记处与执行读法：`docs/legacy-chain-deprecation.md` §1、§2（必读，本单不重述裁定、只把它变成一张可核对的表）

---

## 〇 开工前必须当场做的三件事

1. `git -C C:/Users/24782/Desktop/Pry_Project status --short --branch`——他人名下未提交项一律当别人的资产：不读内容、不改、不提交、不 `stash`、不 `clean`。已知他人名下两枚暂存项＝`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`，本波一律不碰。
2. 建独立载体（不动主工作树，也不建 worktree 外的第二份工作副本）：
   `git -C C:/Users/24782/Desktop/Pry_Project worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave19-1023/carrier d82cd2d`
   入场两行判据原样贴进报告：`git status --short | wc -l` ＝ **0**，`git rev-parse HEAD` ＝ 上面那个全号。
3. 先读四处再动笔：`docs/legacy-chain-deprecation.md` 全文；`docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md:86`、`:375`、`docs/plans/PROJECT_PLAN.md:37`、`:41`；`docs/narrative-open-items-memo.md` 的 U-2／U-3／U-9／U-11 四节；`src/Pry.Core/Narrative/` 目录清单（新链今天的实际形状）。
   入场读数另贴一行：`grep -rn "Pry.Core.Narrative" src/ --include=*.cs | grep -v '/obj/' | grep -v 'src/Pry.Core/Narrative/'` 的命中清单（这条决定「替代」这一步到底完成了没有，不许凭印象写）。

### 〇.4 边界

允许写的文件只有两份：

- 本报告 `docs/eval/reports/2026-10-23-hds010-legacy-chain-disposition.md`（按批次增量落盘，冷启动 lane 有 150 轮上限，先落表头与前几段再往后写）；
- 你自己的载体目录内临时文件（`git status --short` 里不许出现仓库路径）。

禁止：

1. 改 `src/**`、`tests/**`、`scripts/**`、`Pry.slnx`、`Directory.Build.props`、任何 `.csproj`——本波一片代码都不删，删是第二片的活，要有构建凭证才能删。
2. 起任何 `dotnet build`／`dotnet test`／`dotnet publish`。这台机器上此刻另有三条 lane 在飞（第十六／十七／十八波，载体 `.local-notes/qa-wave16-1023/`、`qa-wave17-1023/`、`qa-wave18-1023/`），构建预算归它们；本波的产物是一张表，编译过不过与本波无关。
3. 动 `licenses/**`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md`、`docs/resource-limit-ledger.md`（只读可以，写不行）。
4. 动他人名下未提交项；动别人在飞载体的文件面（`src/Pry.Core/Narrative/**` 的新文件、`tests/Pry.Core.Tests/**` 的新文件）。本波对这些只读。
5. 写任何状态词或结案词（「已完成／已退役／可发布／已验证」这类）。「已证」只许用于你当场跑过命令并贴出读数的条目，其余一律写「未验证」。
6. 改任何数值门槛、放宽或删除任何既有断言——本波不改判据面，但**要逐格读它们**，读的时候不许改。

---

## 一 交付物：四张表，全带可复算证据

### 表 A：旧链候选逐文件处置

范围＝`src/Pry.Core/Runtime/`（15 枚）、`src/Pry.Core/TurnTaking/`（6）、`src/Pry.Core/Prompting/`（2）、`src/Pry.Core/Conversation/`（1）、`src/Pry.Core/Intelligence/`（12，逐枚判「旧先行决策段」还是「宿主继续保留」，`docs/plans/PROJECT_PLAN.md:30` 点名退出的只有 `BehaviorPlanner → ReplyGenerationRenderer → ReplyPlanner` 这条先行决策位置）、`src/Pry.Api/Runtime/`（2）、`src/Pry.Api/Services/` 里承载旧会话编排与投递的那批（逐枚判，32 枚全列）、`src/Pry.App/` 旧窗口与旧回合管理器（`MainWindow.axaml.cs`、`RemoteConversationTurnManager.cs`、`ConversationTurnManager` 调用段，对照 v4 `:86`）。

每行六列，缺一列算不合格：

| 文件 | 三态判定（退役／被新链替代／宿主继续保留） | 生产调用方清单（`grep` 命令原文＋命中文件，命中为空要写「0 命中」并贴命令） | 它守的产品语义（一句大白话） | 新链等价物（`src/Pry.Core/Narrative/` 里的类型＋file:line，没有就写「无」） | 关联判据格（`tests/` 里指向它的文件与 `[Fact]`／`[Theory]` 计数） |

判定口径按 v4 `:375`：每一行必须写清是「旧假设退役」还是「产品语义继续」；后者必须指出新链哪个类型接管、接管的格子在哪；指不出就写「产品语义继续，但新链尚无等价格＝缺口」，并汇总进表 C。**这张表里最有价值的产出是缺口清单，不是删除清单。**

### 表 B：死入口清单（零调用方，本波就能删的那批）

对每个候选贴出你实际跑的两条命令与输出：一条搜 `src/`（排除 `/obj/`），一条搜 `tests/` 与 `scripts/`。`docs/legacy-chain-deprecation.md` §5 已登记一枚：`src/Pry.Core/Conversation/ConversationService.cs`（登记时的读数＝`src/` 与 `tests/` 除自身声明外零引用）。你要复算它，并把同类候选找全。注意三条假阳性：

1. 同名不同类（`ConversationService` 与 `ConversationSessionService`／`ConversationApplicationService`／`ConversationFolderApplicationService` 是三枚不同类，搜前者的结果会被后者污染，必须按精确 token 过滤并贴过滤条件）。
2. `GlobalContractUsings.cs`／`using` 行与注释里的提及不算调用方，但字符串扫描会命中，逐条注明它是引用行、注释行还是调用行。
3. catalog（`scripts/measure-fixed-eval.ps1`）里的映射行、以及任何 `docs/**` 文档里的提及都不算生产调用方，但**必须列出**——删掉一个被 catalog 点名的类会打断覆盖映射，那是第二片要处理的账，不是本波能忽略的细节。

### 表 C：删除片的排法与前置

把表 A／B 收敛成「第二片该按什么顺序删、每一片需要哪一轮构建凭证」。每一片写：动的文件、连带消失的判据格数（四项分项：Core／Api／App／Client 各几格，按 `grep -c '\[Fact\]'` ＋ `grep -c '\[InlineData('` 的口径现场数，并贴命令）、需要新写的等价格清单（按 v4 `:375` 逐条对账）、该片的打包形状门要求（对照 `docs/legacy-chain-deprecation.md` §4 第 2 项）、以及该片是否依赖 U-2／U-3／U-11 的迁移步骤。**只排顺序与凭证要求，不许写「大概几万行、应该没问题」这种没有读数的话。**

### 表 D：作者必须知道的后果

一句大白话一行，列「如果第二片今天就做完，她机器上那个正在跑的程序会失去哪些能做的事」（对话、重生成、撤回、倾听信号、主动消息、表情、语音、贴纸、记忆管理等，逐项对着 `src/Pry.Api/Controllers/` 与 `src/Pry.App/` 的实际入口列，并标明该能力今天由旧链还是新链提供）。这一张表是交给作者看的，不许出现内部编号当主语。

## 二 反证要求（本波唯一的「红」）

零构建刀没有测试可跑，所以判据是**你的表必须能被别人复算**。两处自证，缺一处不算交付：

1. 正证：表 A 随机三行、表 B 全部行，把命令原文与输出一起贴，让守门人在同一基点上一条 `grep` 就能对上。
2. 反证（必须做一次，并在报告里单独一节）：故意拿一行写错的调用方清单（比如把 `ReplyPlanner` 的行填成 0 命中），验证你自己的表在按命令复算时**会暴露**——把复算命令与暴露出来的那行原样贴回，然后改对。这条是为了证明你的表不是抄出来的。

## 三 出口读数（原样贴命令与输出）

- `git -C <主树> status --short` 与开工那一行相同，差集只许是**你自己那一份报告**那一行 `??`（报告写主树路径，不写载体）；他人名下两枚暂存项 `M ` 逐字节不动。
- `git -C <载体> status --short | wc -l` ＝ 0（载体里不落任何文件，报告不进载体）。
- `git -C <载体> diff --stat d82cd2d -- src tests scripts Pry.slnx Directory.Build.props` ＝ 空（本波零产品码，这一条是硬出口）。
- 报告行数与四张表的行数：`wc -l` ＋ 每张表 `grep -c '^| '` 实数，写完当场数一遍再填，不许估。

## 四 硬线（本次放权不解除的部分，逐条照抄）

不 `git tag`、不 `git push`、不实际打包（`0-4`／`RT-008-PKG` 归作者下令）；托盘「退出…」那一次人工点击归作者；真实模型对照需作者一句「可以跑」，本波零读数；`licenses/**`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md` 一字不碰；数值门槛零改动（`.7`／`.5`／`2400`／五档超时／`MinimumUsableTokensPerSecond = 12`／`4096`／`DefaultSafetyTokens = 128`／`MinimumInputTokens = 512`）；模型、运行库、数据库、密钥、日志、用户导入内容、无权分发素材一律不提交；`%LOCALAPPDATA%\PryCompanion` 绝不作测试载体；不许放宽／删除／注释／跳过任何既有断言；没条件验证的一律写「未验证」。

起跑前照常驻纪律：`.local-notes/game-preflight6-1022.ps1` 先跑（本波不该起 `dotnet`，若你确实需要起一次，BUILD_OK 才起、HOLD 就停下回报）；一次只起一个 `dotnet`；共享机器上 SQLite `disk I/O`／`out of memory` 形状的失败读数为作废，换目录重跑，绝不动别人进程、绝不降判据。
