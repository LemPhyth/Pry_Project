# 工单 HDS-WAVE21-1023｜换人复判第十八波那 10 枚新格（9 组落门补 7＋正向补 3），并查它 23 轮反证有没有打在"临时格"上

- 工单 id：`HDS-WAVE21-1023`
- 任务编号：`HDS-002`／`HDS-003` 判据面欠账（在册「wave-13 那 9 组落门无单枚反证」＋拆法原文账外新添的 10 格）
- 性质：**判据复核刀，零产品代码净改动**（所有突变当场施加、当场按字节还原）
- 执行方：冷启动 lane（第十八波的**换人复核方**，不是原作者）
- 出闸：守门人 2026-10-23 17:5x；作者原令逐字＝「我不想思考了，你就继续给我派工单，该提交的提交吧」＋「直接大跨步开发疯狂挂子智能体」

## 〇 为什么有这一把（先把问题说清）

第十八波（载体分支 `codex/wave18-20261023`，唯一内容提交 `147aa191c712aa7d627d74543b044df86249160e`，父＝`def0faf`，217 insertions／0 deletions，只改两份既有测试文件）撞到 150 轮上限停机：报告 `docs/eval/reports/2026-10-23-qa-wave18-unheard-gates.md` 写到 §三 就断，**没有出口读数一节**，因此那 10 枚新格今天**没有归位凭证**，条数账一格未记。本刀不是重写它们，而是：

- 活单①＝**换人**给这 10 枚格各取一次"单枚、单轮、清洁红"；
- 活单②＝审它报告 §二／§三 那 23 轮（m18-01～m18-23）里，每轮压红的对象是**已落盘的判据**还是"当轮临时写进去又还原的格"——后一种不能算这 10 枚格的凭证；
- 活单③＝补齐它欠的出口读数（构建／全量／四份 TRX／catalog／跟踪条数），归位押在守门人手里，**不在本 lane 权限内**。

## 一 载体与入场（命令照抄，别自创）

```
git -C C:/Users/24782/Desktop/Pry_Project worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave21-1023/carrier e8a842a
cd C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave21-1023/carrier
git cherry-pick 147aa191c712aa7d627d74543b044df86249160e
git status --short | wc -l
git diff --stat def0faf..HEAD -- tests   # 只应见两份测试文件、217 insertions／0 deletions
```

- 起刀点 `e8a842a` ＝第十六＋十七波归位后的线尾（守门人当场核过 `147aa19^` 与 `e8a842a` 在这两份测试文件上**零漂移**，所以 cherry-pick 应当 0 冲突；若真出现冲突，**停下登记**，不要自己择边）。
- 入场三行判据当场贴：`date`、`git rev-parse HEAD`（cherry-pick 后的全号）、`git status --short | wc -l` ＝ 0。
- 主树那两枚他人 `M ` 暂存项与若干 `??` 与你无关：不读、不改、不提交、不 `stash`／`clean`／`rm`。**在共享主树里一律禁止任何 `git commit`／`cherry-pick`**，一切提交动作只发生在你自己的载体里，且归位归守门人。

## 二 硬边界（违反一条即作废本轮）

1. **零产品代码净改动**：`src/**` 只允许"施加单枚突变→跑一轮→`git checkout -- <该文件>` 字节还原"；报告里每一轮都要有还原后的 SHA-256 与基线一致的证据。判据文件（那两份测试）**本刀一字不改**——不新增、不删除、不注释、不 `Skip`、不放宽任何断言或阈值。
2. **在册数字门槛零改动**：`.7`／`.5`／`2400`／五档超时／`MinimumUsableTokensPerSecond = 12`／`4096`／`DefaultSafetyTokens = 128`／`MinimumInputTokens = 512`。
3. 不接 API／Client／App，不碰 `%LOCALAPPDATA%\PryCompanion`，不新增 SQLite 依赖。
4. 不 `tag`／不 `push`／不打包／不 merge／不 FF `main`；许可与素材三文件（`licenses/**`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md`）一字不碰。
5. 不写状态词与结案词，不自封基线；拿不到的红一律写「未取到红」并写清原因（防御纵深／兜底支／住在禁改件里／结构上红不了）。**结构上红不了的断言要指出承重断言行**（前例：`DEF-GREENGATE-01`＝`DeliveryLedger.Snapshot()` 那格；第十三波 §三.16 那 9 组）。
6. **焦点预检**：每一次 `dotnet` 之前先在主树跑

```
powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1
```

   整段输出贴进报告；`VERDICT=BUILD_OK` 才起跑；读到 `HOLD` **就地停刀、不请示、不改预检判据**。`free_memory_gb` 偏低或读数呈 SQLite `disk I/O`／`out of memory` 形态时那轮读数作废（他机器的资源形状，不是断言失败），不许动他的进程。
7. **预算**：解决方案级构建 **≤1**、全量 **≤1**、定向 **≤12**（10 枚各一轮＋2 轮备用；用满即停，剩余格按"未取到红"登记）。跑法固定写法：`dotnet test <工程或解决方案> -c Release [--no-build] --results-directory "<绝对路径>" --logger trx`；短参 `-r` 会拼进 `bin` 目录导致"零测试仍 exit=0"；不给 `--logger trx` 就没有 TRX；**每轮独立结果目录、不要复用同名 `LogFileName`**（第十七波四工程同名 TRX 互相覆盖那坑）。跑完先数 `*.trx` 再读 `<Counters>`。
8. **报告先建、逐批落盘**（150 轮上限）：动第一次 `dotnet` 前把 §〇／§一 写满建文件 `docs/eval/reports/2026-10-23-hds002-003-wave18-cell-recount.md`，之后每批追加。交付形态按 `docs/agent-engineering-standards.md` §10 四项。

## 三 活单①：10 枚格逐枚点名（先读码定靶，再放单枚突变）

`147aa19` 的两份文件各补了多少、叫什么，**当场从盘上取**，不要抄本工单：

```
git show --stat 147aa191c712aa7d627d74543b044df86249160e
git diff def0faf..147aa19 -- tests/Pry.Core.Tests/NarrativeIdentityContractTests.cs
git diff def0faf..147aa19 -- tests/Pry.Core.Tests/NarratorPrototypeTests.cs
```

守门人先前读过的形状（供你核对，不一致以盘上为准并在报告里更正我）：`NarrativeIdentityContractTests` 62→65＝放宽侧 3 枚（含置信度区间闭合边界的两枚 `InlineData` 与"截止时间等于最早时间即放行"那枚）；`NarratorPrototypeTests` 49→56＝落门侧 7 枚。

每枚格要求：
- 先写"这枚格守的是 `src/` 哪一行／哪个判定分支"（文件:行，读码得出，不许猜）；
- 再放**只指向那一行**的单枚突变，跑一次该类，登记：恰好该格红＋其余格绿才算"单枚清洁红"；
- 十一项字段逐轮齐（①文件:行 ②改前原文 ③改后原文 ④`git diff --stat` ⑤跑法整行 ⑥TRX 文件名 ⑦红格全名＋消息要点＋红点行号 ⑧还原命令 ⑨还原后 `git status --short` 行数 ⑩突变态与还原后 SHA-256 ⑪当轮预检整段）；
- 同轮多红要按消息形状与红点行号归因，不得把"共享判定线"记成两枚新证；
- 一枚突变若同时打红多格，选**更窄的那枚配方**重放（前例：`NarrativeSqliteStore` R11 为隔离 D3 特意换形）。

## 四 活单②：审那 23 轮的"对象是不是临时格"

对第十八波报告 §二（m18-01～m18-17）与 §三（m18-18～m18-23）逐轮回答三问，并把结论列成表：

1. 那一轮的红格，判据本体在 `147aa19` 里（**已落盘**），还是该轮"先写入判据→跑红→又删掉"（**临时格**）？
2. 那一轮的突变对象是产品代码行，还是判据文件自身？
3. 那一轮之后载体 `git status --short` 是否归 0、还原 SHA 是否给了？

口径：**临时格不能算这 10 枚格的归位凭证**，也不许倒过来把它写成"这一轮白跑"——它仍是那条判定线的证据，只是归属要写对。第十八波报告本身没有把这两类分账，本刀负责把它分出来。

## 五 活单③：出口读数（缺哪格报哪格，不许留空）

1. 构建 0 警告 0 错误原样；2. 解决方案级全量一次，四份 TRX 的 `<Counters>` 逐份贴，两条跳过按 `outcome="NotExecuted"` 点名（`notExecuted` 计数器恒 0 不看）；3. 全量分项与线尾在册 `1275／0／2`（Client 108／App 233／Api 280／Core 654，executed 652）对账——**你的树上多了 `147aa19` 那 10 格，Core 应为 664、总 1285**，若不等，逐格找差在哪，不许报"差不多"；4. `scripts/measure-fixed-eval.ps1 -ResultsDirectory <本轮目录>`（带这个参数才跑，否则 throw 而 exit 码照旧 0）；5. `git diff --check` 干净；6. 收尾 `git status --short` ＝ 0 行、`git rev-parse HEAD` 仍是起刀后那一号；7. `dotnet` 调用次数只认盘上产物；8. 报告行数与 §三 表行数当场实数。

## 六 不许走的捷径

1. 不许为了让某格响就改判据、改阈值、加 `Skip`、注释掉别格；2. 不许把"全绿"当"这一轮没跑"的反证——要按 `<Counters>` 与红名点名；3. 不许用重试到绿、固定返回值或吞异常收尾；4. 不许引用第十八波报告里的推演当实测红；5. 不许在本刀里顺手做归位／FF／tag／push；6. 不许把第十八波那把写成"失败"或"作废"——它交付了 217 行判据与 23 轮记录，缺的是出口读数与归属分账，报告里按事实写。

## 七 仍未验证（照实列）

真实模型对照、生产接线、UI 可见行为、生产库对账；以及"这 10 枚格在**接线后**的用户可见后果"——没接线就没有，一律写未验证。

—— 完 ——
