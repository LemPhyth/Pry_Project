# HDS 第八波派单（2026-10-22 深夜）｜第六波那 43 格里最大的两把：`faf8ecf` 剩下的 14 格（Core 11＋Api 3）＋`1edf69e` 的 3 格＝本波 17 格，只补"拆法原文"

派单人＝守门人（Architecture / Integration / Release）。执行方式＝守门人自起冷启动子 Agent；本工单是给执行方直接消费的，不交作者转派。
坐标系＝`docs/plans/PROJECT_PLAN.md` §4.2。**开工前必读 `docs/agent-engineering-standards.md`（作者 2026-10-22 全文给定的《Agent 工程协作规范》）与根目录 `AGENTS.md`／`docs/plans/README.md` 第 14 条**——本单与规范冲突时以规范为准，派单不得自行放宽。
本波**零产品代码、零新判据、零门槛改动**：第六波那条 lane 把九把刀 46 格拆响了，但报告没写出来，"改了哪一行、改前改后长什么样、还原凭据"全丢（见 `docs/dispatch-hds-wave6-20261022.md` §⑧ 与 `docs/eval/reports/2026-10-22-gatekeeper-reread-of-wave6-carrier-trx.md`）。第七波已经按同样的十一项字段补齐了 3 格（`eval-QA-WAVE7-1022` 件 3）。本波＝把剩下 43 格里数量最大的两把按同样标准补出来。

---

## ① 交付纪律（四条，违反任何一条＝这一批证据作废）

第七波按这四条派过一次，结果＝86 次工具调用、报告 454 行完整落盘、没撞上限也没超预算。第六波没写这四条，活干完了、证据随对话一起丢。

1. **报告文件在任何一次突变之前先建好**，并把 §〇（载体、入场读数、计划突变清单、预算声明、收尾预案）先写满再动第一枚。
2. **每跑完一批立刻把该批的表格追加进报告文件**，不要攒到最后一次写。
3. **感到轮次或预算不够时，先把已完成批次写进报告，再停手**——不许"再确认一遍"把剩下的预算烧光。
4. **十一项字段缺哪一项就明写缺哪一项**，不许用"已拆过"概括：① 载体提交号 ② 被改文件:行 ③ before→after 原文 ④ `git diff --stat` 数字 ⑤ 跑法整行命令 ⑥ TRX 文件名 ⑦ 红格用例全名＋消息要点 ⑧ 还原方式 ⑨ 还原后 SHA-256（与 `git show HEAD:<file> | sha256sum` 比对）⑩ 还原后 `git status --short` 行数 ⑪ 这一向的结论。

## ② 载体（已备好，照用）

- 路径＝`C:\Users\24782\Desktop\Pry_Project\.local-notes\qa-tip-1022`，detached HEAD＝`6b6f549`，`git status --short` 入场＝**0 行**，四份二进制已于 10-22 17:48 构建过（第七波收尾那一轮），可直接 `--no-build` 起跑。
- **已知且已登记的一格差异**：主线现在比这棵载体多出 `tests/Pry.Core.Tests/NarrativeStorageContractTests.cs` 的 `+19/−1`（第七波件 1 归位＝`4039a50`，恒真断言已换成可失败断言）。本波两把刀的判据都不在那个文件里 ⇒ **不要把它当漂移去追、不要重证它、也不要替它补任何东西**；只在报告 §〇 里照实写"载体较线尾少那一格已归位的判据改动"。
- 若要另建载体：先自线尾 `git worktree add --detach`，自己承担一次解决方案级构建，并写进交付说明。**禁止**在主工作树里改文件、跑构建、提交（唯一例外＝§⑤ 的报告落点）。

## ③ 本波的两把与判据面

**A 把（14 格）＝`faf8ecf`＝HDS-011 AUD-01／06／04：联合上下文预算先于分配、DOCX 解压限额**
- 判据面按第六波那份复采报告的归属＝`tests/Pry.Core.Tests/ContextBudgetTests.cs`（红 8 格）、`tests/Pry.Core.Tests/AttachmentLimitTests.cs`（红 4 格）、`tests/Pry.Api.Tests/ContextWindowLoadTests.cs`（红 3 格），合计 15 格里**已有 1 格做过完整十一项**（见下），本波补**剩下的 14 格**。
- 被指的产品行在 `src/Pry.Core/Prompting/ContextBuilder.cs`、`src/Pry.Core/TurnTaking/AttachmentTextExtractor.cs`。
- **已有一格做过完整十一项（本波不必重做）**：`ContextBuilder.cs:24` 夹零 ⇒ `ContextBudgetTests.The_raw_equal_output_and_context_window_yields_a_negative_input_budget`（`eval-QA-WAVE7-1022` §4.2）。本波＝补那一把**剩下的**格，同一文件的别的路径（归一化、`ForModelWindow`／`TryNormalizeWindow`、解压限额那几处计数器）。

**B 把（3 格）＝`1edf69e`＝HDS-011b AUD-07：贴纸附带文本与会话正文共用同一条限额**
- 判据＝`tests/Pry.Api.Tests/StickerContentLengthGuardTests.cs`（第六波红 3 格，全在这一把名下）。
- 产品在 `src/Pry.Api/Services/ConversationApplicationService.cs`、`ConversationSessionService.cs`、`src/Pry.Contracts/ApiContracts.cs`。

**跑批分组＝按程序集，不按"把"**：Core 组＝A 把里那 11 格（`ContextBudgetTests` 剩 7 格＋`AttachmentLimitTests` 4 格）；Api 组＝A 把的 `ContextWindowLoadTests` 3 格＋B 把 3 格。两把若在同一个产品行上共用一道门（读码时大概率会碰到），**按第七波 §4.4 的写法登记成"一枚突变的多处同因红"**，不许为了让格子数好看而重复拆同一行，也不许把不同门的格子合并成一格。

**逐格要求**：每一枚突变都要在 §④ 第 1 步的零构建预检里回答三问（这一行由谁调用、一个生命周期里跑几回、什么输入能让它走到"拒绝／越界"那一支）。**答不出第三问的，当场把"仍然全绿"当证据采下来＋登记这一向够不着，不要为了留下一抹红去追一个不可能存在的红。**

## ④ 跑法（照做，不要各自发挥）

1. **零构建预检**（读码，不起 `dotnet`）：先把两把全部格的三问写进报告 §一／§二，再决定哪些格值得拆。
2. **按组批量写盘**：同一组内一次放 3～4 枚突变（不许互扰的例外见第 4 步），逐枚记 `git diff --stat`（必须＝N 文件 N 行、且只有你预期的文件）。突变只许改语义、不许让工程编译不过（改常量、改比较方向、改分支条件；不许删类型、不许改签名、不许"注释掉那一行"代表"拿掉那一行"，也不许新建一个含该名字的文件去骗过按文本扫的门）。
3. **每组一次定向跑**（这一组的隐式构建即算这一组唯一一次构建）：
   `dotnet test tests/Pry.<X>.Tests -c Release --results-directory "<本轮专属绝对路径>" --logger trx`
   —— `--logger trx` 不许省；不给它时目录里一个 `.trx` 都不会有，而控制台照样打印"已通过!"，那份绿不可引用（本仓库踩过三次）。
4. **逐格读回**：红格集合必须与预期逐格对上。"预期红而实绿"＝该枚空转，单独重跑一次或登记够不着；"多出来的红"＝组内互扰，此后把该组拆成单枚逐次跑并在报告里说明。同因多响的格按第七波 §4.4 的写法另记，不合并、不省略。
5. **逐字节还原**（`git checkout -- <你改过的那些路径>`，只在载体内、只针对自己那些行），事后 SHA-256 与 `git show HEAD:<file> | sha256sum` 比对；PS5.1 的 `Set-Content -Encoding UTF8` 会补 BOM，不许当还原手段。
6. **还原后同组再跑一次，必须复绿**。
7. 收尾那一轮＝解决方案级 `dotnet build Pry.slnx -c Release` 一次＋全量 `dotnet test Pry.slnx -c Release --no-build --results-directory "<绝对路径>" --logger trx` 一次，先 `ls *.trx` 数到四份再读 `<Counters>`，并按 §⑤ 顶账。

## ⑤ 起点与出口读数（不许"差不多"）

Core 527（executed 525／passed 525）／Api 278／App 233／Client 108＝**总 1146＝passed 1144＋0 红＋2 条环境跳过**；两条跳过必须按名点名＝`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`（**两条都在 `Pry.Core.Tests.dll` 名下**，`notExecuted` 计数器恒 0，只能按 `<UnitTestResult outcome="NotExecuted">` 点名）；catalog **76**；受跟踪文件数当前＝**547**（每落一份文档就往上一格，别读成门槛变化）。本波不改任何条数 ⇒ 收尾复证必须还是这些数字。

## ⑥ 预算、停刀与弹窗纪律（作者 2026-10-22 现令，违反一次就要收回整单）

- 预算上限＝解决方案级构建 **1** 次（收尾那一轮）、全量 **1** 次、定向跑至多 **6** 次（两组红绿各一＝4，另留 2 次给互扰拆复跑）；单工程构建允许，但要在报告里单列一册如实记数。用完即停，把缺口如实交回。
- **每一次** `dotnet` 之前（含单工程构建、含 `--no-build` 的 test）先跑：
  `powershell -NoProfile -ExecutionPolicy Bypass -File "C:\Users\24782\Desktop\Pry_Project\.local-notes\game-preflight4-1022.ps1"`
  并把整段输出（含 `VERDICT=` 那行）原样贴进报告。`VERDICT=HOLD` ⇒ 就地停刀并回报；不许等三分钟再试、不许换更省事的跑法绕开、不许自己改预检脚本的判据。
- `free_memory_gb` 低于 6 只是 WARN，不阻止起跑；只有真出现 SQLite `disk I/O` 或 `out of memory` 形状才按作废处理。**不要自造比工单更严的作废判据。**
- 被测脚本以 `exit N` 收尾时，每一次调用都要独立子进程；同进程 `&` 调用会让被调脚本把复核外壳一起终止。

## ⑦ 硬约束（越线即作废）

- 零提交：本波不产生任何 `src/`／`tests/`／`docs/` 提交；所有改坏只允许存在于载体工作树并在报告前逐字节还原。
- 不许为了让仪器好看而放宽、删除、注释掉任何既有断言；不许改 `docs/resource-limit-ledger.md` 的数值；不许动 `.5` 检索地板、`2400`、五档超时、`MinimumUsableTokensPerSecond = 12`、`4096` 地板。
- 多分支的修法要按分支逐个撤销、逐个取红；还原前先数一遍改动点是否齐全。
- 不碰作者的任何进程；不把 `%LOCALAPPDATA%/PryCompanion` 当测试载体（只读探查也不算许可）；不 `git tag`、不 `push`、不打包。
- 主工作树里他人名下的两枚暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）不属于本单，任何情况下不读改、不提交、不 `stash`、不 `clean`。**在他人名下那枚文件被提交之前，任何"门禁条数"的对账都要先按方法名把它从两侧排除。**
- **状态词与结案词不由执行方写**：报告里写事实与读数，不写"已结案／已验收／基线已改写"。

## ⑧ 交付物

- 唯一允许你写进主工作树的东西＝新文件 `docs/eval/reports/2026-10-22-qa-wave8-waves1-remaining-two-knives.md`（报告 id＝`eval-QA-WAVE8-1022`）。写完即停，不许 `git add`、不许提交。
- 报告章节建议：§〇 载体与预算声明 → §一 A 组预检三问 → §二 B 组预检三问 → §三 A 组红批逐格十一项 → §四 A 组还原与复绿 → §五 B 组红批 → §六 B 组还原与复绿 → §七 收尾那一轮与顶账 → §八 预算终账 → §九 够不着的向与读码理由 → §十 结论（不写状态词）。
- 明写"哪些向够不着、为什么"（引用读码结论，不引用感觉）；**不许把"没拆"写成"不需要拆"**。
