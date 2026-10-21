# HDS 第四波派单（2026-10-22）｜HDS-011c 拆开的第一把：依赖/许可的机械面 ＋ 资源限额总盘

> **执行状态（2026-10-22 守门人补记，本单不再需要冷启动执行）**：由守门人自起子 Agent 在隔离 worktree `.local-notes/hds-011c-1022` 跑完，三枚提交 `e554483`／`bca8b42`／`5593e1a` 已由守门人以 `git merge --ff-only` 归位为本线尾；出口读数（守门人当场逐份读四份 `.trx`）＝总 **1140**＝Core 527／Api 274（262→274＝本刀新判据 12 例）／App 233／Client 106，失败 0、通过 1138、两条环境跳过照旧同名；构建 1 次、全量 1 次。反证缺口（浮动版本与包名覆盖两条只能走内嵌样张）与三处维护税见 `docs/plans/PROJECT_PLAN.md` §9 同日 entry。

派单人：守门人（Architecture / Integration / Release）。坐标系＝`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS-011 行
（v4 §10 把 HDS-011 的范围写成「DOCX/文本限额、模型资源/网络边界、长历史和大记忆、依赖锁定、版本及许可证」；
AUD-01／04／06／07 四格已由前两把切片带走，本单接的是**依赖锁定＋版本/许可**与**限额总盘**这两格）。
作者 2026-10-22 的现行指令逐字：「继续派工」＋「你，还是自己起Agent吧，你这派单跟我组织Agent的方式完全不一样」
＋仍然生效的硬约束「你 下一次 不要 再 反复构建 触发那个防火墙 烦我打游戏 再这样 我要给你喂史了」。

基线起点＝**派单时的线尾**（本单出闸时代码面＝第三波归位后的 `6fc7dbb`；起笔前用 `git log --oneline -1` 当场取线尾，别抄号——
线尾之上可能只有文档提交，代码面与顶账以 `6fc7dbb` 为准）。

---

## 全局约束（与第三波同源，逐条可判红；完整条文见 `docs/dispatch-hds-carrier-20261022.md` §全局约束）

1. 开工前 `git status --short --branch`；主工作区索引里他人**已暂存**的文件不属于你（本轮实测＝`docs/dispatch-tip1020-02-memory-group-judgment.md` 1/1、
   `tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs` 63/0）——不 stage、不提交、不回退、不碰。
2. 独立 worktree 开工，先 `git rev-list --count <线尾>..HEAD` 数基线，落后就 `git merge --ff-only <线尾>` 追平。
3. **整个载体只允许一轮解决方案级完整构建＋一次全量测试。** 迭代只用单工程 `dotnet build`；反证一律定向跑
   （`dotnet test tests/<X>/<X>.csproj -c Release --no-build --filter "FullyQualifiedName~<用例名>"`，先 build 该测试工程）。
   **不要**用 `-t:Rebuild` 传解决方案/多项目——本机 SDK（10.0.200）会把它误解析成两个项目并报 MSB1008（第三波当场踩过），
   等效写法＝`dotnet build <单个 csproj> -c Release` ＋ `dotnet test <同一个 csproj> -c Release --no-build --filter ...`。
4. 出口读数唯一写法：`dotnet test Pry.slnx -c Release --no-build --results-directory "<绝对路径>" --logger trx`，
   跑完先数到**四份 `.trx`** 再逐份读 `<Counters>`；没有 TRX 就没有证据。最后一轮必须紧跟一次完整
   `dotnet build Pry.slnx -c Release`（0 警告 0 错误），**不许拿 `--no-build` 的旧 dll 充证据**——只重建产品工程而没重建测试工程
   会让旧测试二进制假红/假绿（第三波踩过一次，如实登记）。
5. 新判据先红后绿：每条都要当场拆一次它守的守卫，红例全名＋报错原文进交付说明；只跑绿＝没有证据。
6. 不许 `git tag`、不许 push、不许打包；Core 不依赖 Avalonia／HTTP；**不新增 NuGet 包**；模型、运行库、数据库、密钥、日志、
   用户导入内容不得提交；`%LOCALAPPDATA%/PryCompanion` 是禁区，连只读都不许当测试载体（一律临时目录，收尾走
   `SqliteTestPools.Clear`／`ClearAndDeleteRoot` 那扇门，别自己 `Directory.Delete` 裸删）。
7. 状态词与结案词不由实现方写；证据齐了也只填"已实现待验"。
8. **绝对不许**为了让扫描门变绿去改被扫描的真相源：不许动 `LICENSE`、`ASSETS_LICENSE.md`、`licenses/**` 的内容，
   不许给 csproj 加 `NoWarn`/`pragma`，不许把判据写成"文件不存在就跳过"（那是常绿门）。前置不成立时先修仪器，不修判据。

---

## 片 E1｜依赖与许可的机械面（**零产品代码**，落点＝新测试文件；本轮实测：`tests/` 里对 `.csproj`／`THIRD-PARTY-NOTICES.md` 的引用**零命中**）

现状（守门人当场读过，不是转述）：
- 代码适用 **AGPL-3.0-only**（作者 2026-10-20 裁决）；照搬白名单按项目点名＝`Mai-with-u/MaiBot`(MaiCore)、`satorijs/satori`、
  `ISEKAI-MomoiCore/HDS-Interlude`，权威表在 `docs/upstream-license-compatibility.md` §4.4 与
  `licenses/THIRD-PARTY-NOTICES.md`（`:59-62` 四行许可判定、`:74-78` 照搬来源表）。
- 已发生的照搬＝`src/Pry.Core/Runtime/` 下 5 个文件带 `// Ported from Mai-with-u/MaiBot@fb4a8bd9…` 头
  （`IdleBackoffController.cs`、`MessageIntervalStats.cs`、`ReplyNecessity.cs`、`TurnGates.cs`、`TurnScheduler.cs`），
  唯一的 upstream 凭据目录＝`licenses/upstream/MaiBot_fb4a8bd917d6fe373df9e3c22289d229e7f1b344/LICENSE`。
  Satori 与 HDS-Interlude 目前**只在白名单里、还没有代码进来**——这一点不能被读成"表错了"。
- 依赖侧：仓库里**没有** `Directory.Packages.props`、没有 `packages.locked.json`、没有 `nuget.config`，只有 `Directory.Build.props`；
  每个 `PackageReference` 各自写版本，没有任何机械面检查它们。

要交付什么（四条判据，都走"扫仓库文本/结构"的既有先例手法——参考 `tests/Pry.Api.Tests/ReleaseAuditabilityShapeTests.cs`、
`PlaceholderDefinitionStaysInCoreTests.cs`、`ProcessWidePoolResetBoundaryTests.cs`：从仓库根往下读文件、带正向探针、
每个扫描器必须自带"我确实扫到了东西"的自检）：
1. **凭据完整性**：凡源码里出现 `Ported from <repo>@<sha>` 一类的照搬头，其 `<repo>_<sha>` 目录必须在
   `licenses/upstream/` 下存在且内含 `LICENSE`；同时该文件必须在 `licenses/THIRD-PARTY-NOTICES.md` 的照搬来源表里有一行。
   反向也要：表里点名的每个 `src/` 文件必须真的带那行来源头（防止"表上写了、代码里的痕被删了"）。
2. **白名单外即红**：来源头里的仓库名不在 §4.4 四行名单内 ⇒ 判红。特别地，`AstrBot`／`Nekro-Agent`／`Private Companion`／`qq-bridge`
   这四个名字**只允许出现在"禁止照搬"的说明文字里**，出现在任何 `src/**` 的来源头即红。
3. **依赖版本不许浮动**：所有 `*.csproj` 的 `PackageReference` 必须有显式具体版本；出现 `Version="*"`、
   区间（`[`、`]`、`(,)`）或留空即红。附带一条正向样张自检（扫描器对一张故意写浮动的样张必须报红，对真实仓库必须报绿且**扫到的包数 > 0**）。
4. **第三方表覆盖还原面**：`licenses/THIRD-PARTY-NOTICES.md` 必须逐条覆盖仓库里出现的每一个 `PackageReference` 包名
   （这条是"总盘"，允许它在本轮只做到"包名覆盖"，不许顺手去判许可兼容性——那是法律判断，归作者）。

不许走的捷径：
- **不许在本单引入中央包管理或 lock 文件**（`Directory.Packages.props`／`packages.locked.json` 会改还原形状、牵动
  `scripts/package-release.ps1` 与运行库布局，且需要联网 restore）——那是另一格，见下面「不在本单范围」。
- 不许把扫描范围缩成"只扫 `src/Pry.Core/Runtime/`"来凑绿（第 1 条要扫全 `src/**`）；
- 不许用"文件读不到就 return"吞掉缺失（读不到就是红）；不许把包名比对做成大小写不敏感的模糊匹配；
- 不许改 `licenses/**`、`LICENSE`、`ASSETS_LICENSE.md`、`docs/upstream-license-compatibility.md` 的任何内容来让判据通过；
- 不许新增 NuGet 包，也不许升级任何现有包版本（版本号一动，本单的"不许浮动"判据就失去可比对象）。

## 片 E2｜资源限额总盘（先盘清，再**只补缺的那几格**；限额数值一个都不许改）

现状：限额散在至少这些位置，覆盖情况未知——`src/Pry.Contracts/ApiContracts.cs:196`（`MessageContentMaxLength = 20_000`，
入口校验有判据）、`src/Pry.Core/TurnTaking/AttachmentTextExtractor.cs:21` 一族（DOCX 解压/深度/时长，AUD-04 已带判据）、
`src/Pry.Core/Prompting/ContextBuilder.cs:16-17`（`DefaultSafetyTokens`／`MinimumInputTokens`）、
`src/Pry.Api/Runtime/CharacterIntelligenceGateway.cs:44,51`（`PendingTurnLimit = 8`／`ReceiptLimit = 64`）、
`src/Pry.Core/Memory/MemoryDatabase.ProtocolEvents.cs:219,223`（`MaxPerConversationLimit = 5_000`／`MaxTotalRowsLimit = 200_000`）、
`src/Pry.Contracts/PryProtocol.cs:47,63,69,80`（心跳/存活/请求预算/转写四档超时）、`src/Pry.Api/Capabilities/DesktopCapabilities.cs:21`（`TimeoutMs = 5_000`）、
`src/Pry.Core/Inference/WindowsProcessJob.cs`（作业对象：父进程退出即杀子进程）与 `LlamaHardwareDetector.cs:22,25`（探测超时）。

要交付什么：
1. 新建受跟踪文档 `docs/resource-limit-ledger.md`（总盘表）：每行＝限额名、`文件:行号`、数值、它防的用户可见后果、
   **有没有判据**、判据文件名与用例名。这张表是本单的第一产物，缺它＝没交付。
2. 表里"零判据"的格子**本轮至少补两条**行为判据（补哪两条由你按后果严重度选，写明为什么是这两条）：
   形状必须是行为级（真的把某个上限越过一次，看它是被拦、被降档、还是照抄进去），不许写成"grep 常量名"的文本门。
3. 一条**总盘自身的守卫**：扫描 `src/**` 里名字含 `Limit`/`MaxLength`/`Timeout`/`Tokens` 的 `const`/`static readonly` 常量，
   每一个都必须在 `docs/resource-limit-ledger.md` 里有一行；漏一格即红（这样下次再加限额就会撞上这张表）。
   这条守卫要自带正向探针（往表里临时少写一行时必须红——用测试内部的样张，不许真改文档来过门）。

不许走的捷径：
- **不许改任何限额数值**（`.5` 地板、`20_000`、`2400`、超时档位都属于"待标定/已由别的单裁定"的格，改数值＝越界）；
- 不许把"限额先于分配"这条既有原则削弱来让新判据通过；不许新增第二套上限语义（同一类内容同一把尺）；
- 不许为凑绿把某个限额写进表却标"无判据"然后又补一条永远绿的断言（无判据＝如实写"无"，由守门人登记排队）；
- 不许顺手实现"按内存/磁盘动态降档"这类新功能——本单只盘只补判据，不扩范围。

---

## 不在本单范围（守门人已登记，等作者点头）

引入中央包管理／`packages.locked.json`（真"依赖锁定"）：要改 `Directory.Build.props` 或各 csproj、要联网 restore、
会牵动 `scripts/package-release.ps1` 的产物清单。这一格单独等作者一句话，本单**不许**先动。

## 出口证据（一次跑齐）

1. 四份 `.trx` 的 `<Counters>` 全量读数：总／四分项／失败／点名跳过（跳过只按 `<UnitTestResult outcome="NotExecuted">` 点名，`notExecuted` 恒 0）。
2. **顶账表**（起点＝第三波归位后的代码面，读数由守门人 2026-10-22 当场逐份读那四份 TRX 采）：
   Core **527**（executed 525／passed 525）／Api **262**／App **233**／Client **106**＝总 **1128**，失败 0，通过 1126，
   两条环境跳过照旧同名（`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`）。
   写法＝"某分项 527→527＋N，N＝本刀新增 X 条（逐文件 `grep -c '[Fact]'` 再数 `[Theory]`/`[InlineData]`）"；
   四个分项对不上＝读数作废，如实登记回来找守门人。**不要为了"确认起点"先跑一轮全量**——起点就是上面这四个数。
3. 完整 `dotnet build Pry.slnx -c Release` 那轮的 **0 警告 0 错误**。
4. 每条新判据的红例全名＋报错原文（E1 四条各至少一次、E2 的总盘守卫与两条行为判据各至少一次）——
   扫描型判据的反证必须包含"把被扫的真相源故意改坏一次"（例如临时加一条浮动版本样张到测试内嵌样张里，
   **不许**真改仓库里的 csproj 来过门）。
5. `scripts/verify-repository.ps1`、`scripts/measure-fixed-eval.ps1 -ResultsDirectory <本轮专属目录>`（catalog 应仍 **76**；它会往该目录写
   `fixed-eval-coverage.json`，别复用别人的 run 目录）、`git diff --check` 的当场输出。
6. 交付说明逐条列出：动过的文件与为什么、构建/全量各几次（**期望＝1 和 1**）、没做到的点。

## 交付形态

- 代码/测试与文档分刀提交：`test:`（E1 四例、E2 判据）、`docs:`（`docs/resource-limit-ledger.md`）、必要时 `fix:` 零产品码就不写。
  提交信息点名片编号（HDS-011c-E1／HDS-011c-E2）。
- **不要自己合回主线**：留分支＋SHA 给守门人归位。文件面注意：E1/E2 都不许碰 `docs/plans/*` 与他人名下文件；
  命名前先 `ls tests/Pry.Api.Tests/ | grep -i "License\|Depend\|Limit"`，与既有
  `DiscoveryDataDirectoryBoundaryTests.cs`／`RuntimeDescriptorWriteGateTests.cs`（第三波新落）不要撞名。

## 排队（本波不开工）

1. 真·依赖锁定（中央包管理/lock 文件）——等作者点头。
2. **HDS-003** 新生成中心隔离原型——真实模型对照要占作者这台机器的算力或额度，动手前需作者点头。
3. HDS-004／005——开工前逐条读 `docs/narrative-open-items-memo.md`；硬前置＝U-2／U-3／U-11＋U-5 实例维（005）、U-9 与 HDS-003 的语义结论（004）。
4. `QA-TIP1020-02` 的实现仍悬在他人索引（63/0）；客户端路由判据 `2954dbc` 等索引清空由守门人合流——两件都不派给实现方。
5. QA 已欠三次独立复采（第一波三把＋第二波两把＋第三波四片）——复采轮要起构建，由作者定时。
