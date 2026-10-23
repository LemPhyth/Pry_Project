# 第二十二波·HDS-WAVE22-1023｜第十三／十四波 65 枚判据格的"凭证归属盘查"（证据审计刀）

- 工单：`docs/dispatch-hds-wave22-20261023.md`（`HDS-WAVE22-1023`，性质＝证据审计刀，默认零 `dotnet`）
- 执行方：冷启动 lane（第二十二波）
- 交付物：本报告一份（主树、未跟踪）＋§四 那张后续复采清单
- 本刀不改任何代码、不改任何判据、不新增判据、不自封基线、不写状态词与结案词

---

## 〇 入场读数（全部当场实测，不引用工单数字）

### 〇.1 时间与线尾

```
$ date
Fri Oct 23 18:36:37     2026

$ git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD
033e7ed07725b5394ffa1d9476ae04d680723aa8
```

### 〇.2 主树状态（他人资产按硬线只数行不读内容）

`git -C C:/Users/24782/Desktop/Pry_Project status --short --branch` 原样：

```
## codex/ui-main-window-redesign
M  docs/dispatch-tip1020-02-memory-group-judgment.md
M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs
?? .qoder/
?? docs/character-learning-safety.patch
?? docs/research/HDS_INTERLUDE.md
?? docs/research/HDS_QQ_RESEARCH_INDEX.md
?? docs/research/QQ_BRIDGE.md
```

两枚 `M ` 暂存项与全部 `??` ＝别人名下资产，本刀不读内容、不改、不提交、不 `stash`／`clean`／`rm`。

### 〇.3 在册门禁现值（读 §9 当场取，不读工单）

工单写在 18:2x，那一把（第二十波归位＋线尾副本全量）在 18:3x 已落地并顶账，`docs/plans/PROJECT_PLAN.md` §9 第 184 行起那条守门人 entry 的盘上现值＝**1321／executed 1319／0 红／2 跳过**（分项 Client 108／App 233／Api 280／Core total 700、executed 698）。本刀**不动这一格在册基线、不自封基线**；工单 §〇／§九 那句"在册 1275／0／2"里照旧有效的是纪律那半句（本刀不动在册基线），数字改读 §9 的 18:3x entry。

### 〇.4 四处证据目录（先数再读，当场 `find` 实测）

| 来源 | 路径 | `.trx` 份数 | 目录内文件总数 |
|---|---|---|---|
| 第十三波载体与反证轮 | `.local-notes/qa-wave13-1023/` | **42** | 1951（含 `carrier` 检出树） |
| 第十四波载体与反证轮 | `.local-notes/qa-wave14-1023/` | **21** | 1796 |
| 十三＋十四合流复采 | `.local-notes/qa-wave134-merge-1023/` | **4** | 1781 |
| 十六／十七合流复采 | `.local-notes/qa-wave1617-merge-1023/` | **4** | 1795 |

合计 **71** 份原始结果文件（工单 §二 写的"67 份"＝前三处，`qa-wave1617-merge-1023/` 那 4 份它要求"数给你自己现场取"，实测＝4，落在 `run1-full/`，另有一份 `fixed-eval-coverage.json`）。

逐目录 TRX 分布（本刀当场数，用于后面按轮定位）：

- `qa-wave13-1023/runs/`＝36 个轮次目录、42 份 TRX：`run-clean` 1、`run-clean-b` 1、`run-m01`…`run-m15` 各 1（15）、`run-re-m01`…`run-re-m15` 各 1（15）、`run-full` 4、`run-fix-clean` 1、`run-fix-ledger` 1、`run-fix-full` 4。
- `qa-wave14-1023/runs/`＝17 个轮次目录、21 份 TRX：`run-clean` 1、`run-R1…R5`、`run-A1…A6`、`run-C1…C5` 各 1（16）、`run-full` 4。
- `qa-wave134-merge-1023/runs/run-full/`＝4 份（`…_15_54_49`／`…_15_54_50`／`…_15_54_50[1]`／`…_15_54_51`）。
- `qa-wave1617-merge-1023/run1-full/`＝4 份（`…_17_30_45`／`[1]`／`[2]`／`…_17_30_46`）；该目录的 `carrier` HEAD＝`91b2e906fc9600008994113072abb2c5d514c48d`、`git status --short`＝0 行。

第十三波另有 `out/` 128 份辅助产物（`diff-mNN.txt`、`diff-re-mNN.txt`、`preflight-*.txt`、`mNN.stdout.txt`、`build.txt`、`build-fix.txt`、`full.txt`、`full-fix.txt` 等）与 `pristine/` 干净态快照、`mut-round.sh`、`re-falsify-1023.sh`、`rounds-1023.txt`；第十四波有 `tools/`。这些是本刀 §三 字段②③④⑪ 的盘上出处。

### 〇.5 本刀的格清单从盘上取（不抄报告汇总）

- 第十三波 49 格＝`git show 290a603:tests/Pry.Core.Tests/NarratorPrototypeTests.cs` 当场数：`[Fact]` **46** ＋ `[Theory]` 1 ＋ `[InlineData` 3 ＝ 46＋3＝**49** 格（与 §9 17:5x／18:3x 两格口径一致）。该提交 `--stat`＝4 files changed, 1756 insertions(+)，测试文件 822 行。
- 第十四波 16 格＝`git show d3fbac4:tests/Pry.Core.Tests/NarrativeCommitAndActionGuardTests.cs` 当场数：`[Fact]` **16**、`[Theory]`／`[InlineData` 0 ＝ **16** 格；该提交 `--stat`＝1 file changed, 412 insertions(+)。
- 工作树当前值（只读计数，不作凭证）：`NarratorPrototypeTests.cs` `[Fact]`＝**53**（第十八波那 7 枚已随 `639a579` 归位进来，不属本刀 65 格范围）、`NarrativeCommitAndActionGuardTests.cs` `[Fact]`＝**16**（一字未动）。
- 65＝49＋16。

### 〇.6 硬线自证口径

本刀默认零 `dotnet`：入口读数起，截至本节落盘，`dotnet` 调用＝**0**；焦点预检未起跑过一次（因为一次 `dotnet` 都不需要）。收尾 §六 会把"调用次数只认盘上产物"那一格实测贴出。

（下面 〇B 是本刀入场后的第一次读盘更新，单独成段；之后进 §一。）

### 〇B 入场读数的中途更正（2026-10-23 18:5x 当场复采；§〇.1／〇.2 那份是 18:3x 的中途快照，不是入场值）

守门人在本刀跑途中把线尾往前顶了。当场复采：

```
$ date
Fri Oct 23 18:53:56     2026

$ git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD
8c2f3e4357260d7cfa651c477b319a7ef335c32b

$ git -C C:/Users/24782/Desktop/Pry_Project rev-parse main HEAD        # 两行同值
8c2f3e4357260d7cfa651c477b319a7ef335c32b
8c2f3e4357260d7cfa651c477b319a7ef335c32b

$ git -C C:/Users/24782/Desktop/Pry_Project status --short --branch
## codex/ui-main-window-redesign
M  docs/dispatch-tip1020-02-memory-group-judgment.md
M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs
?? .qoder/
?? docs/character-learning-safety.patch
?? docs/eval/reports/2026-10-23-hds003-005-cell-evidence-audit.md
?? docs/research/HDS_INTERLUDE.md
?? docs/research/HDS_QQ_RESEARCH_INDEX.md
?? docs/research/QQ_BRIDGE.md

$ git -C C:/Users/24782/Desktop/Pry_Project diff --stat 033e7ed..HEAD -- src tests
（空）

$ git -C C:/Users/24782/Desktop/Pry_Project diff --name-status 033e7ed..HEAD
A       docs/dispatch-hds-wave23-20261023.md
M       docs/full-scope-sprint.md
M       docs/plans/PROJECT_PLAN.md

$ git -C C:/Users/24782/Desktop/Pry_Project log -1 --format='%h %ci %s' 04a1f1b
04a1f1b 2026-10-23 18:41:48 +0800 docs(plans,wave23): 出闸第二十三波整合单……
```

一、**顶账那三把**＝`04a1f1b`（18:41 出闸第二十三波整合单）→`013bedf`（18:44 sprint §3 顶账，跟踪条数实测 594）→`8c2f3e4`（18:45 §4.2 那行登记第二十三波认领），`main` 与线尾同指 `8c2f3e4`。对盘面影响＝**零**：`git diff --stat 033e7ed..HEAD -- src tests` 为空，那三把只动 `docs/`。所以 §〇.5 的两枚格清单（`290a603` 的 49、`d3fbac4` 的 16）与 §三 要用的当前树行号都不因这把顶账漂移；工作树里那两枚 `M ` 与全部 `??` 仍与入场同值（只多了本刀那份未跟踪报告）。

二、**在册基线照 §9 的 18:3x entry，本刀不动**＝1321／executed 1319／0 红／2 跳过（Client 108／App 233／Api 280／Core total 700、executed 698）；§9 第 184 行起那条 18:4x 新 entry 第 186 行自己写的也是这个数（原话「在册基线照旧 **1321／0／2（Client 108／App 233／Api 280／Core 700、executed 1319）**，本刀不动它，lane 自跑数也不得有改写权」）。18:4x 那条**只写了派单与在飞，没写第二十三波已跑完**（第 186 行原话＝"这一把是**派单登记**，不是交付登记"），所以它不是交付登记、也没有第二次顶账。

三、**登记"派单转达的那笔 MEMORY.md 补记"与盘上的差**。派单原话让我"去读那张单，别按 §〇.1 那份入场值假定 `d67217f` 不在树上"，我照做了；结果是**那笔补记自己的五个值在本刀可及的盘上都取不到**（下表）。本刀按读法纪律把转达只当线索、不当证据，一律以盘上值写：

| 转达里的那笔说法 | 盘上实测 | 取法 |
|---|---|---|
| 工单的归位提交＝`d67217f`，是线尾祖先 | **盘上无该物**：`git cat-file -t d67217f` ⇒ `fatal: Not a valid object name d67217f`；`git rev-list --all` 逐枚 `grep -E '^(d67217f\|f57c54a\|826973b)'` 零命中 | 当场两条命令原样 |
| 线尾从 `033e7ed` 顶到 `f57c54a` | **`f57c54a` 盘上无该物**；线尾实测＝`8c2f3e4` | `rev-parse HEAD` |
| `main` FF 到 `826973b` | **`826973b` 盘上无该物**；`main` 实测与线尾同指 `8c2f3e4` | `rev-parse main HEAD` |
| PROJECT_PLAN §9 那条 20:4x（第二十三波出闸）entry | §9 的第二十三波 entry 标记＝**18:4x**（第 184 行）；全文 grep `20:4x` 只命中第六波那格旧 entry 里的 `20:02` 时刻，与第二十三波无关 | `grep -n` |
| wave23 报告＝`docs/eval/reports/2026-10-23-hds-wave23-chain-spine.md` 已入库 1429 行、`.local-notes/qa-wave23-1023/` 的 runs 目录有 TRX | **该报告文件盘上不存在**（`ls` ⇒ `No such file or directory`；`git log --all -- <该路径>` 零命中）；`.local-notes/qa-wave23-1023/` 只有一个 `carrier/`（HEAD＝`04a1f1b`、`git status --short`＝**0 行**），**该目录 `find -name '*.trx'` ＝ 0 份**，且**没有 `runs/` 这一层** | `ls`／`find`／`git log --all -- <路径>` 当场 |

再补一行同样的对照：**那笔补记的原文本身边上也没有**——`C:/Users/24782/.qoder/memory/MEMORY.md` 实测 18 行（是一份索引，逐条指向同目录 15 枚主题文件），对该文件与同目录全部文件 grep `d67217f`／`f57c54a`／`826973b`／`chain-spine`／`1429`／`第二十三波` **全部零命中**。所以这一组值只存在于派单转达，不存在于盘上；本刀不据此判任何人写错，只登记"取不到"。

**"归位／同树"那半句实质成立**，只是 hash 要换成盘上值：`git log --all --oneline -- docs/dispatch-hds-wave22-20261023.md` 唯一命中＝**`1cc76ca`（18:25:14）**，而 `git merge-base --is-ancestor 1cc76ca HEAD` ⇒ 真。**所以本刀读工单、读两份报告、读 `src/**`／`tests/**` 全在同一枚 `8c2f3e4` 树上，不存在"读不到的那张单"。** §9 的 18:3x entry 里守门人写的也是同一枚号（原话＝"主树起刀 `1cc76ca`（第二十二波工单出闸那一把文档刀）→ 代码刀 `59252aa` → 本登记刀"），与本刀实测同值。

四、**那笔补记提的"红格行号可能过期"这一风险，本刀的读法规格本来就不吃当前树行号**：§三 每一格的红点行号取自**第十三／十四波载体自己的 TRX 栈**（`NarratorPrototypeTests.cs:line N`／`NarrativeCommitAndActionGuardTests.cs:line N`，对应 `290a603`／`d3fbac4` 那份字节），不是当前主树行号；当前主树行号只在 §四 那张复采清单里作为"下一把该改哪一行"的落点用，并且是我在 `8c2f3e4` 树上当场实测的。**"这一格的红点在 290a603 那份文件里"与"这一格在当前树上第几行"是两个不同的事实，下面逐格分开写。** 另：第二十三波那张单的硬边界（§9 第 186 行原话）＝只许在 `src/Pry.Core/Narrative/` 与 `tests/Pry.Core.Tests/` **新建**文件＋自己那一份报告、**既有文件零字节改动**；即便它起跑，动的也不是这两枚测试文件的字节。它一旦把 TRX 落到盘上，本刀 §三 那张表的"盘上出处"集合就多一批新轮次，**归属仍按轮次与执行体分开写，不与本刀 65 格混算**（§9 18:4x entry 原话＝"两笔账不许并成一句"）。

五、MEMORY.md 那笔里"第十三／十四波那 65 格欠独立复采这一条一字未改、第二十二波那张单照旧要它"与本刀 §〇.3／§9 现值一致，无异议。

---

## 一 方法与仪器（本刀读的是什么、怎么读的、仪器先被怀疑过没有）

### 一.1 三件事实的取法

活单①（65 格逐格四问）与活单③（链条等式）全部只从**盘上原始产物**取：`.local-notes/` 下 71 份第十三／十四／合流 TRX（§〇.4 已数）＋后续波次的 11 组全量轮次 TRX＋`git show <归位提交>:<测试文件>` 的字节＋两份报告的 prose。活单②（复采清单）里"要改坏的 `src/` 那一行"取的是**当前主树 `8c2f3e4` 上的实测行号**（`grep -n`），不是两份报告里写的历史行号——历史行号在 66906d0 那把改名与第十八波归位之后会指错地方，下面点名。

一次 `dotnet` 都没起（§〇.6）。所有结论的取证深度＝"读盘上已有的 TRX 与字节"，凡盘上没有的，一律写"盘上无该物"，不去推测它跑过什么。

### 一.2 仪器（三份，都落在 `.local-notes/wave22-1023/`，不改仓内任何文件）

| 仪器 | 干什么 | 自校验 |
|---|---|---|
| `parse-trx.js` | 逐份 TRX 读 `<Counters …/>`（自闭合，不找 `</Counters>`）＋逐个 `walk` 所有 `<UnitTestResult`  occurrences，先测 `/>` 再决定要不要找闭合标签；非 Passed 的格抽 `testName`／`outcome`／`<Message>`／`<StackTrace>` 里的 `([\w.]+\.cs):line (\d+)`；`NotExecuted` 按名点出来 | 拿 `run-m02` 与 `raw grep` 对平：49 个节点＝46 Passed＋3 Failed，`<Counters failed="3">`，两边同值才敢用。另：标签末端用**引号感知**的 `findTagEnd`，因为 `testName` 里有 `"` 之外的 `>`（消息文本），裸正则会把属性表切错 |
| `analyze-cells.js` | 用 `run-clean`／`run-clean-b`／`wave14 run-clean` 建 49／16 的**格清单**（不是抄报告），再建"哪一轮把哪一格打红"的红图与逐轮视图 | 两条对平：`roster13(49)` 与 `git show 290a603` 的 `[Fact]46＋[Theory]1＋[InlineData]3` 同数；`run-clean-b` 与 `run-fix-clean` 两套名字集合 `set equal: true`。红图里出现的名字若有一个不在 65 清单内会被单列——实测 **0 例外** |
| `chain.js`／`chain2.js`／`setdiff.js` | 后续波次的每份全量 TRX 逐份读 counters，并按 `testName` 的 `Pry.<X>.Tests` 前缀分程序集（不信解决方案级默认名），再对指定几份 Core TRX 做 testName **集合差**，把"这一轮比上一轮多的那 N 个名字"逐名列出 | `chain2.js` 的每步求和都用四个独立文件当场相加，不做心算；`setdiff.js` 输出＝623/654/664/700 各步的加法名单（见 §五） |

仪器报"零命中／undefined"的纪律走了一遍：`chain2.js` 第一版用单行 `node -e` 内联时，Windows 路径反斜杠在 shell 里被吃掉导致 `names()` 返回 `undefined`（`TypeError: Cannot read properties of undefined`）。按规矩**先怀疑仪器**——改成写文件＋双反斜杠／`path.join`，而不是把结论改成"盘上没有"。该次故障发生在仪器里，没有发生在任何一份 TRX 上，所以 §三／§五 的读数不受它影响；重跑后 71 份全部有 counters 与节点计数。

### 一.3 每格的四个判据（口径先写死，免得逐格漂移）

1. **凭证**＝该格在某一份**带突变的**轮次 TRX 里被打红的那一份：给文件名＋`<Counters>` 的 `failed` 读数＋红格全名＋红点行号。干净轮（`run-clean*`）里的红**不算凭证**——那一轮没有"改坏"这个因，红了只能说明判据与实现当时不一致（第十三波 `run-clean` 那一格就是这种，见 §三.1 表最后一行）。
2. **一双手**＝写这一格判据的执行体 与 取这一格红的执行体 是否同一人。判据作者＝lane（两份报告各自 §二 落码、`98246cd`／`f07f1d2` 是 lane 载体提交）；取红者按轮次归属：`run-m01…m15`＝第十三波 lane（`mut-round.sh` 头注＝`HDS-WAVE13-1023 T3 …`），`run-re-m01…re-m15`＝守门人（`re-falsify-1023.sh` 头注原话＝"守门人修复轮：字节变了就得逐枚重取"），`run-R*／A*／C*`＝第十四波 lane（`tools/driver.sh`）。**但守门人那一手在 `66906d0` 里改过这两枚文件的字节（8 行 `src`＋100 行测试，`git diff --stat 98246cd 66906d0` 实测），所以在册口径里它不构成第二换手**——§9 17:5x 那条的原话＝"对第十三＋十四波那 65 格仍同一只手（守门人在载体里修过判据）"。本刀两种写法分开给：**盘上有另一执行体的红**（20 格）≠ **在册承认的第二换手**（0 格）。
3. **十一项字段**按**发布面（报告 prose）**与**盘上面（out/ 归档）**分别数，缺项点名。合并看时某项可能在盘上有档而报告没贴（第十三波 re-m 那 15 轮的 ⑪），也可能报告贴了而盘上无档（第十四波 16 轮的 ⑨⑪，见 §二.3）。本刀的分类按**报告发布面**给 A／B，把盘上面写成括注，因为工单 §三.3 问的是"那一轮的十一个字段齐不齐"，而凭证要交给下一把刀读。
4. **清洁度**＝那一轮是不是只放了这一枚突变。第十三波 15 轮全部满足"恰好一处命中"（`out/repl-mNN.txt` 的 `REPLACEMENTS=1`＋`CHANGED_LINES=2`，re-m 同）；其中 `m02`／`m08`／`m14` 一轮打出 3／2／4 格红——**一枚突变、多格红**，这一类本刀不写成"批次轮顺带红"（批次定义＝同一轮里放多枚突变，本刀在 71 份里没读到任何一份这样的轮次），也不写成"单枚单格清洁"，写成"单枚突变／红集 N 格（共用同一行门）"，逐格共享凭证、不单独署名。

### 一.4 术语的严格读法（本刀用词照工单 §二 与两条常驻纪律）

- **`<Counters …/>` 自闭合**，`notExecuted` 计数器在这 71 份里**恒 0**，跳过一律按 `outcome="NotExecuted"` 节点点名（全仓每轮固定那 2 格：`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`、`CoreTests.Configured_sense_voice_model_can_run_offline`）。所以"在册 1321/0/2"那两格跳过是从节点读的，不是从 counters 读的。
- **TRX 里 `.cs:line N` 是断言调用处**，不等于源码里 assert 文本那一行；第十四波报告自己就在每轮另注了"断言文本源码行"。本表两栏分开写：`TRX行`／（报告另注的）`源行`。
- **"盘上没有那份 TRX"与"读了那份 TRX 而它没把这格打红"不许互换**：本刀对 65 格里"从未红"的那些，写法一律＝**读了 42／21 份 TRX，其中 30／16 份带突变的轮次里该格都不是 Failed（读过且为否）**；只有"某一轮的归档根本不在盘上"才写"盘上无该物"（例：第十七波终局那轮四工程同名互相覆盖，盘上只有 Core 那一份，见 §五.3）。

---

## 二 两份报告的声称逐条回读原始 TRX（支持／不支持）

### 二.1 第十三波那份（`2026-10-23-hds003-narrator-prototype.md`，963 行）

| 报告声称 | 盘上实测 | 判定 |
|---|---|---|
| §三.1–三.15：15 轮逐枚单放，每轮点名红，"零取不到红"（§三.16.1） | `run-m01…m15` 15 份 TRX 逐份 counters：`m02`failed=3、`m08`failed=2、`m14`failed=4、其余 12 轮 failed=1，求和＝**21**；每轮 Failed 格名与报告点名的格名**逐格同名**；红点行号 15 轮全部与报告 §三 各条第 7 项一致（`:352/:243+404+430/:251/:261/:232/:271/:217/:289+320/:304/:202/:413/:425/:398/:540+555+582+569/:509`） | **支持**（含 §七.3 那条守门人自我更正"22→21"，本刀独立求和也是 21） |
| §三.16.3：按名定向预算 18 用尽＝干净态 3＋突变 15 | `runs/` 下带 `NarratorPrototypeTests` 过滤的轮次目录实测＝`run-clean`、`run-clean-b`、`run-m01…m15` ＝17 个单 TRX 目录＋§七 那 3 个修复轮（`run-fix-clean`／`run-fix-ledger`／`run-fix-full`）＋守门人 re-m 15 轮。lane 自己的 15＋2 干净＋1 次编译失败未落盘 ＝ 报告口径的 18 | **支持**（"18"是 lane 那把的账，不含守门人 re-m 的 15＋3） |
| §三.16 表：9 组新落门**没有单枚反证**（即时支分岔／沉默支／快照三行／`SplitContiguous`／三条超时取消／`Validate()` 三行作用域／K3 不等式自检／来源引用回指／段理由串） | 与红图一致：这 9 组对应的格（`Deferred/Immediate_…refused` 五格、`Silence_round_…`、`Round_never_rewrites…`、`Draft_on_a_stale…`／`Draft_from_a_fenced…`／`Draft_with_a_wrong…`、`Contiguous_split_…`、`Quote_absent…`／`Ambiguous_quote…`、`Call_refuses_an_assembly_that_belongs_to_another_scope(三行参数)`、`Compiler_carries_source_references…`、`Compiler_honours_the_unified_budget_inequality…` 等）在 30 份带突变 TRX 里**全部不是 Failed** | **支持**（报告自己登记的欠账与盘上零红一致，见 §三.1 的 D 类） |
| §七.3：15 枚在新字节上逐枚重取，15/15 拿到点名红，"零空档" | `run-re-m01…re-m15` 15 份 TRX counters＝与 lane 那 15 轮**同形状**（1/3/1/1/1/1/1/2/1/1/1/1/1/4/1），红格名与行号也与原表逐格同名同行 | **支持**（这是"另一执行体的红"，但不等于第二换手，理由见 §一.3.2） |
| §七.5.2：合流复采 1244/0/2＝1179＋49＋16 | `qa-wave134-merge-1023/runs/run-full/` 四份 TRX 实测：Core 623（executed 621）＋Client 108＋Api 280＋App 233＝**1244**、failed 0、2 格 NotExecuted；623−558＝**65**＝49＋16（558 的来源见 §五.1） | **支持** |
| §七.1：修的是 27 处 `xUnit1051`＋19 处 `Assert.Equal(0, X.Count)`→`Assert.Empty`，另 6 处保留；"没有一条断言被删／被注释／被跳过" | `git diff --stat 98246cd 66906d0`＝2 files, 54 insertions(+), 54 deletions(−)（**插删同数＝同判据换写法的形状**）；`[Fact]`／`[Theory]`／`[InlineData` 计数在 `98246cd`／`66906d0`／`290a603` 三枚提交上都是 **46／1／3**——**没有新增一格，也没有少一格** | **支持**，并据此否掉工单 §三.2 那句"第十三波有一格是守门人在载体里补的"（见 §二.4） |
| §二.9：`Immediate_action_binds_to_the_unique_slice_and_lands_three_segments` 在第一次干净起跑里红（`Expected: 3 / Actual: 1`，`:636`），lane 自判"我的新格自己写错了预期"，改 `Assert.Equal(1, …)` 并**补一格更硬的** `SegmentIndex` 逐一体 | `run-clean`（`…_14_50_15.trx`）实测：total 49／executed 49／failed 1，红格正是它，消息 `Assert.Equal() Failure: Values differ Expected: 3 Actual: 1`，`NarratorPrototypeTests.cs:line 636`；`run-clean-b`（`…_14_51_59.trx`）＝49/49/0 | **支持**（但该红无突变因，按 §一.3.1 不作凭证） |
| §四.5／§四.6：全量 1228／2 红（Api 同因两格）/2 跳过，Core 607 | `run-full` 四份实测：Core 607（605/0）、Client 108、**Api 280 里 failed=2**＝`ResourceLimitLedgerCoverageTests.Every_limit_shaped_constant_has_a_row_in_the_ledger`（`:50`）与`The_guard_sees_a_row_being_dropped_and_a_row_drifting_off_its_line`（`:110`）、App 233 ⇒ 求和 **1228／failed 2** | **支持**（这一轮的"红 2"就是报告自己写的在册 0 红没保住那一格） |

### 二.2 第十三波 re-m 那 15 轮的字段在哪一面（工单 §三.3 要逐轮点名缺项，先给共用结论）

- **报告发布面**：§七.3 那张表每行给了 ①文件:行、②③before→after（写成"`< 0` → `< 0 && x > 1000`"这一类）、counters（失败/通过/总计）、⑦红格全名＋红点行号（第 5 列"TRX 锚点"给的是行号，**没给 ⑥TRX 文件名**）。⑤跑法、⑧还原命令、⑨status 行数、⑩还原 SHA-256、⑪预检原文**只以 §七.3 表头那句前言批量覆盖**（原话＝"每轮 `FROM→TO` 命中恰好 1 处（`REPLACEMENTS=1`）、`CHANGED_LINES=2`、跑前预检 `BUILD_OK`、跑完逐字节 `cp` 回 pristine 并双哈希对平"＋末段"每轮跑完 `git status --short`＝2 行……载体收尾＝0 行"），**没有逐轮值**。⑦的**消息要点**那一段在 re-m 表里也没逐轮给（lane 的 §三 原表逐轮给了消息）。
- **盘上面**：`out/preflight-re-m01…15.txt`（15 份 ⑪）、`out/repl-re-mNN.txt`（`REPLACEMENTS=`，④的一半）、`out/diff-re-mNN.txt`（②③④的原文 diff）、`out/re-mNN.stdout.txt`（⑤⑥⑦的跑法 stdout）、`runs/run-re-mNN/` 各 1 份 TRX（⑥＋counters＋⑦）——**⑧⑨⑩三项在盘上无独立归档**（`mut-round.sh` 是把它们打到 stdout，而 stdout 只存了 `dotnet` 那一段，脚本自己的 echo 没落盘）。
- 结论口径：**"字段缺"＝报告发布面缺**；盘上有的那几项我在表里写成"盘上有档（文件名）"，盘上无档的写成"盘上无该物"，两者不互换。

### 二.3 第十四波那份（`2026-10-23-qa-wave14-narrative-commit-action-unheard.md`，385 行）

| 报告声称 | 盘上实测 | 判定 |
|---|---|---|
| §三 总结：16 轮红集**恰＝1 格**，无"改坏后无格能响"，16 枚均"第一次实测红" | `run-R1…R5/A1…A6/C1…C5` 16 份 TRX counters 全部＝total 16／executed 16／passed 15／**failed 1**；每轮那份唯一 Failed 节点名与报告"唯一红格"逐轮同名，红点行号 16 轮全部对上（`:37/:59/:81/:93/:105/:122/:133/:147/:161/:185/:209/:236/:248/:259/:277/:297`） | **支持** |
| §三 统一约定 ⑩：每轮载体 `git status --short` 行数＝1 | **盘上无该物**：`tools/` 只有 `driver.sh`＋`batch1-3.txt`＋`mutate.py`＋`trxsum.py`，`driver.sh` 第 46 行的 `git status --short | wc -l` 只 echo 到 stdout，stdout 未归档（无 `.log`）。报告以 prose 声称，盘上不可复核 | 登记为"发布面有、盘面无档" |
| §五：预检 19 次全 `VERDICT=BUILD_OK`、0 HOLD | **盘上无该物**：第十四波目录内没有 19 份预检日志（`find .local-notes/qa-wave14-1023 -name 'preflight*'` 零命中；只有 §2.2 贴进报告的那一份 clean 轮原文）。`driver.sh` 第 18 行确实每次都跑预检，但它 `echo "$PF"` 到 stdout，不落盘 | 登记为"发布面有、盘面无档"；本刀**不**据此判它没跑 |
| §一.1.1：C4（`:403`）与 C5（`:410`）同 `Reason` 且 `continue` 互斥 ⇒ 只有钉 Detail 才敏感 | 与红形一致：`run-C4` 那份消息＝`Assert.Collection() Failure: Item comparison failure ↓ (pos 1)`，其余 15 轮全是 `Assert.Single() Failure: The collection was empty`／`Assert.Throws() Failure: No exception was thrown` | **支持**（这解释 C4 的红形为什么独一格） |
| §〇.5：本波 16 枚与第十二波那 20 枚入账行号零重叠 ⇒ 均"第一次实测红" | 本刀不吃第十二波那张账（不在 65 格范围），只登记为报告自述；重叠与否不在本刀可证面 | 不判 |
| §4.2／§4.3：全量 1195／0 红／2 跳过、Core 574 | `run-full` 四份实测：Core 574（572 executed/failed 0）＋Client 108＋Api 280＋App 233＝**1195**／failed 0／NotExecuted 2 格同名 | **支持**；另：574＝558＋16（见 §五.1，与"wave14 载体不含 wave13 那 49 格"同一条事实） |
| §七（守门人归位）：`d3fbac4` 单文件 412 insertions | `git show --stat d3fbac4`＝1 file changed, 412 insertions(+)；`git show d3fbac4:tests/...` 实测 `[Fact]`=16、`[Theory]`/`[InlineData`=0 | **支持** |

### 二.4 工单本身与盘上的三处不合（登记，不改工单、不按工单的错值做结论）

1. **工单 §二"67 份"**＝只数了前三处目录；实测三处＝42＋21＋4＝67 与它一致，但它同时要求"第十六／十七波合流那 4 份数给你自己现场取"⇒ 本刀全局面＝**71** 份（§〇.4）。另 §五 的"总 1179→1244→1275"里 1179 那一档的出处不在 §二 那四处目录里，而在 `.local-notes/qa-wave12-close-1023/runs/full-resample-close/`（本刀当场补数，见 §五.1）。
2. **工单 §五 的链条标注与盘上不符**：原文＝"Core 623（`def0faf` 时点）→ 636（第十三波后）→ 641（第十四波后）→ 654（第十六／十七波后）"。**数字都对得上盘上 TRX，标签错了两个**：636 是**第十六波自测全量**那轮的 Core（`qa-wave16-1023/trx-archive/run-16-full`，17:23，总 1257），641 是**第十七波自测终局**那轮的 Core（`qa-wave17-1023/trx/final-full/final-full.trx`，只有 Core 那一份），二者都不是"第十三波后／第十四波后"。第十三波后是 607（总 1228 且带 2 枚 Api 红）、第十四波后是 574（总 1195）。§9 的 17:5x entry 那句"两条 lane 自跑（1257／1262）与合流读数逐位相容，Core 一条线 `623→636→641→654`"用的就是这条线，只是没标每档是谁。本刀**按盘上把每一档钉到文件名**（§五），不按工单的标签写。
3. **工单 §三.2 那句"第十三波有一格是守门人在载体里补的，报告里点名过"**：盘上不支持。`[Fact]`／`[Theory]`／`[InlineData` 在 `98246cd`（lane 那把）与 `66906d0`（守门人修复那把）与 `290a603`（归位）三处**同为 46／1／3**，`git diff --stat 98246cd 66906d0`＝54 插 54 删（插删同数）——守门人在载体里做的是**换写法与改名**，没补格。报告点名的"补一格更硬的 assert"在 §二.9，署名是 lane 自己（原文"我的新格自己写错了预期……并补一格更硬的"）。本刀逐格的 ② 不采用那句工单表述，采用 §一.3.2 的双口径（盘上有另一执行体的红 20 格／在册承认的第二换手 0 格）。

（下一批：§三 那张 65 行逐格四问表。）

---

## 三 活单①｜65 格逐格四问（表 65 行，编号 1–65 连续可数）

### 三.0 这张表的口径（先把图例与判据钉死，逐格不漂移）

- **格名**：波 13 行省略前缀 `Pry.Core.Tests.NarratorPrototypeTests.`，波 14 行省略前缀 `Pry.Core.Tests.NarrativeCommitAndActionGuardTests.`；省略只在这两枚前缀上做（当场实测：这两枚前缀分别覆盖各自全部格名，无第三种前缀混入）。Theory 行的参数原样写盘上形式（TRX 里是 `(scope: &quot;instance&quot;)` 这一种 HTML 转义写法，本表写 `(scope:"instance")`，两者是同一名）。
- **TRX 名**：全部 65 行的文件名共享同一段前缀 `24782_PHYTHLINK_2026-10-23_`，表内只写时刻段与目录（例：`run-m01/…_14_57_11.trx` 的全名＝`24782_PHYTHLINK_2026-10-23_14_57_11.trx`，路径＝`.local-notes/qa-wave13-1023/runs/run-m01/`）。这样写的原因：文件名唯一区分段就是时刻段，写全名会让每行多出 24 个字符而不增加任何信息。**本表没有把任何一份 TRX 的名字"用目录名代替"**——目录名与文件名都在 §〇.4 与 §三.4 的逐轮清单里成对给出。
- **红点行号**：一律＝该格 TRX `<StackTrace>` 里的 `NarratorPrototypeTests.cs:line N`／`NarrativeCommitAndActionGuardTests.cs:line N`，即**断言调用处**，且对应的是**载体那份字节**（波 13＝`290a603` 的 822 行版，波 14＝`d3fbac4` 的 412 行版），不是当前主树行号（当前树测试文件 989 行，第十八波 7 格插进来会漂）。两栏的分工在 §〇B.四已写。
- **被改坏那一行**（表里 `门` 列）：本刀在 `8c2f3e4` 树上当场读字节核对过——`src/Pry.Core/Narrative/NarrativeContextCompiler.cs`／`NarrativeRoundRunner.cs`／`NarratorContracts.cs`／`NarrativeDraftValidator.cs`／`DeliveryLedger.cs` 与 `git diff --stat 290a603 HEAD`／`ac58245 HEAD` **均为空**（这五枚文件自第十三波载体基点起一字未动），所以两份报告写的 `src` 行号在当前树上仍然指得到同一处；唯一在报告之间不一致的是 §七.3 那张 re-m 表（见下面 G13r 档）。
- **四类口径**（工单 §三 原口径，逐字照用）：A＝单枚清洁红且字段齐；B＝单枚清洁红但字段缺若干（缺项点名）；C＝只有批次轮红、无单枚轮；D＝盘上找不到该格的任何红凭证。本刀判"字段齐"用的是**最严的一档**：11 项都要有**逐轮**值（指针式、批量约定式、缩略式都算缺，缺项点名）。为免这一档把可读性做成"全都缺"，下面同时给一个宽档（A′）的计数，两个口径分开写、不互换。

**三档字段面（本刀按轮次系给出，逐格在表里引用档名）：**

| 档 | 覆盖轮次 | 逐轮有值 | 缺项（点名） |
|---|---|---|---|
| **G13** | `run-m01`…`run-m15`（第十三波 lane 突变轮） | ①（`§三.n` 第 2 项，文件:行）②③（第 3 项 before→after 原文，盘上 `rounds-1023.txt`＋`out/diff-mNN.txt` 同值）⑤ᵗ（指针：整行只在 §三.0 给一份，逐轮写"模板 `run-mNN`"）⑥（第 6 项全名＋`ls \| wc -l`＝1）⑦（第 7 项格全名＋消息要点＋TRX 红点行，counters 逐轮）⑧（第 8 项 `cp pristine/…`）⑨（第 10 项＝4 行）⑩（第 9 项还原后 SHA-256＝pristine 同值）⑪ᵈ（盘上 `out/preflight-mNN.txt` 整段原文；发布面只有 §五 表的四值缩略） | **④**＝原字段 `git diff --stat` 在该载体上无定义（四枚文件未跟踪，`git diff --stat` 恒空），报告以 `CHANGED_LINES=2`／`REPLACEMENTS=1` 替代并在 §三.0 登记口径差；**⑤** 无逐轮整行；**⑪** 发布面无逐轮原文 |
| **G13r** | `run-re-m01`…`run-re-m15`（守门人在 `66906d0` 重取） | ②③（`out/rounds-re-mNN.txt` 与 §七.3 表内 `FROM→TO`）、⑦的行号与 counters、红格名 | **①**：§七.3 表 15 行里 **13 行给的是 `diff -u` 的 hunk 起始行**（`@@ -L,7`）而不是被改坏那一行；本刀用盘上 30 份 `out/diff-*.txt` 逐份取"第一条 `-` 行的旧文件行号"校正，结果：m 与 re-m **同一枚锚、同一枚被改行**，且 30/30 份 `minusLines=1`（＝单枚在字节层可证）。校正后的被改行＝`:54/:62/:71/:78/:87/:110/:203/:246/:239/:284/:107/:36/:95/:115/:155`（盘上实测，见 §三.4）。**④⑤⑥⑧⑨⑩⑪** 全部只有 §七.3 表头那一句批量覆盖（"每轮 `FROM→TO` 命中恰好 1 处、`CHANGED_LINES=2`、跑前预检 `BUILD_OK`、跑完逐字节 `cp` 回 pristine 并双哈希对平"＋末段"每轮跑完 `git status --short`＝2 行"），**无逐轮值**；其中 **⑥** 发布面只到目录名 `runs/run-re-mNN`；**⑦** 的消息要点无逐轮值。盘上面：④（`out/repl-re-mNN.txt`＋`out/diff-re-mNN.txt`）、⑤⑥⑦（`out/re-mNN.stdout.txt`）、⑪（`out/preflight-re-mNN.txt` 15 份）**有档**；⑧⑨⑩ **盘上无该物**（`re-falsify-1023.sh` 把这三项 echo 到 stdout，stdout 只存了 `dotnet` 那一段） |
| **G14** | `run-R1…R5`／`run-A1…A6`／`run-C1…C5`（第十四波 lane 16 轮） | ①（表内 `:306`～`:307` 这一类行号；文件名在统一约定那句里给一次）②③（表内 before→after）④（表内 `--stat` 列，逐轮：`2 deletions`／`1 deletion`／`1 ins/1 del`／`5 deletions`／`6 deletions`）⑤ᵗ（统一约定给整行，逐轮换 `run-<id>`）⑥（表内 `run-R1/…15_32_00.trx`，唯一区分段齐）⑦（格名＋消息＋TRX 红点行，且另注"断言文本源码行"）⑧ᵗ（统一约定＝`git checkout -- <那一枚文件>`）⑩（表内"还原 sha 对平"列，逐轮 ✓ 值＝`88ebf5c3…b00c`） | **⑨**＝逐轮 `git status --short` 行数只有统一约定那句（＝1）＋批次总结那句，**盘上无该物**（`tools/driver.sh` 第 46 行把该数 echo 到 stdout，stdout 未归档）；**⑪**＝逐轮预检原文发布面与盘面**都无**（报告 §五 只给"19 次全 BUILD_OK"的批次句，目录内 `find -name 'preflight*'` 零命中）；④⑤⑧为整行/批量式（④逐轮有值，⑤⑧逐轮无值） |

**还有一条判据要先钉**：`run-clean`／`run-clean-b`／`run-fix-clean`／波 14 `run-clean`／各 `run-full`／`run-fix-full`／合流全量这些轮**都没有放突变**，所以它们的红按 §一.3.1 不作凭证；本刀在 71 份 TRX 里读到的**带突变轮次**＝15（m）＋15（re-m）＋16（波 14）＝**46 份**，这 46 份里没有任何一份同时放两枚突变（`minusLines=1` 与 `REPLACEMENTS=1` 双证），所以：

> **C 类（只有批次轮红、无单枚轮）在本 65 格里＝0 格。** 盘上不存在"批次轮顺带红"这种轮次；报告 §三.16／§六.2 里那些"没轮到拆"的门是**预算耗尽未跑**，不是"跑成批次红"。工单 §三 把 C 类列为可能类别，本刀取"实测 0 格"这一边，并把含糊登记在这里：C 的定义预设了批次突变轮的存在，而这两波的取证设计本身就排除了批次轮（每轮一枚），所以 C＝0 是设计的后果，不是覆盖率好的证据。

### 三.1 表（1–49＝第十三波 49 格；50–65＝第十四波 16 格）

`门` 列＝本刀在当前树实测到的被改坏那一行（`NarrativeContextCompiler.cs` 简称 `K:`、`NarrativeRoundRunner.cs` 简称 `R:`、`NarratorContracts.cs` 简称 `N:`、`NarrativeDraftValidator.cs` 简称 `V:`）。`类` 列括号里给宽档 A′（把 ④ 的替代口径、⑤⑧ 的整行指针、⑪ 的盘上整段都算作有值）时的归类。

| # | 格名 | ①凭证（轮／TRX／`<Counters>` failed／红点行） | 门（当前树实测） | ②手别 | ③字段档 | ④清洁度 | 类（A′） |
|---|---|---|---|---|---|---|---|
| 1 | `Compiler_rejects_a_negative_section_share` | `run-m01/…_14_57_11.trx` f=1 → `:352`；重取 `run-re-m01/…_15_50_10.trx` f=1 → `:352` | `K:54` `.Any(x => x < 0)` | 判据＝lane；红＝lane(m01)＋守门人(re-m01)；在册口径仍同一只手 | G13＋G13r | 单枚轮（`minusLines=1`、`REPLACEMENTS=1`），红集＝1 格 | B（A′） |
| 2 | `Compiler_refuses_a_window_that_leaves_no_positive_input_budget` | `run-m02/…_14_57_24.trx` f=**3** → `:243`；re `…_15_50_16.trx` f=3 → `:243` | `K:62` `if (total <= 0)` | 同上（m02／re-m02） | G13＋G13r | 单枚突变／红集 3 格（同一行门 `:62` 的三枚见证），本格不独署 | B（A′） |
| 3 | `Compiler_refuses_a_scene_without_identity_instead_of_inventing_one` | `run-m03/…_14_57_40.trx` f=1 → `:251`；re `…_15_50_21.trx` | `K:71` `IsNullOrWhiteSpace(payloads.Identity)` | lane(m03)＋守门人(re-m03) | G13＋G13r | 单枚轮，红集 1 格 | B（A′） |
| 4 | `Compiler_refuses_a_current_event_from_another_conversation` | `run-m04/…_14_57_50.trx` f=1 → `:261`；re `…_15_50_27.trx` | `K:78` 会话比对那半条 `\|\| false)` | lane(m04)＋守门人 | G13＋G13r | 单枚轮，红集 1 格 | B（A′） |
| 5 | `Compiler_refuses_the_round_when_the_mandatory_prelude_does_not_fit` | `run-m05/…_14_58_47.trx` f=1 → `:232`；re `…_15_50_33.trx` | `K:87` `preludeCost > total` | lane(m05)＋守门人 | G13＋G13r | 单枚轮，红集 1 格 | B（A′） |
| 6 | `Compiler_fails_loudly_when_the_reserved_directive_does_not_fit` | `run-m06/…_14_58_59.trx` f=1 → `:271`；re `…_15_50_47.trx` | `K:110` 丢 `cost > budget.AdvisoryTokens` 半 | lane(m06)＋守门人 | G13＋G13r | 单枚轮，红集 1 格 | B（A′） |
| 7 | `Compiler_drops_an_optional_section_that_exceeds_its_own_share_and_counts_it_exactly` | `run-m07/…_14_59_09.trx` f=1 → `:217`；re `…_15_50_56.trx` | `K:203` `cost > cap \|\| …` | lane(m07)＋守门人 | G13＋G13r | 单枚轮，红集 1 格 | B（A′） |
| 8 | `Compiler_trims_recent_messages_only_at_whole_unit_boundaries` | `run-m08/…_14_59_19.trx` f=**2** → `:289`；re `…_15_51_03.trx` f=2 | `K:246` `wallet - cost < 0 \|\| …` | lane(m08)＋守门人 | G13＋G13r | 单枚突变／红集 2 格（与 #9 共用同一行门） | B（A′） |
| 9 | `Compiler_shrinks_the_memory_wallet_unit_by_unit` | `run-m08/…_14_59_19.trx` f=2 → `:320`；re `…_15_51_03.trx` | `K:246`＋`K:254`–`:256` | lane(m08)＋守门人 | G13＋G13r | 单枚突变／红集 2 格（与 #8 共用） | B（A′） |
| 10 | `Compiler_removes_cross_conversation_units_and_names_the_count` | `run-m09/…_14_59_29.trx` f=1 → `:304`；re `…_15_51_11.trx` | `K:239` 跨会话比对 → `is null` | lane(m09)＋守门人 | G13＋G13r | 单枚轮，红集 1 格 | B（A′） |
| 11 | `Compiler_keeps_absent_sections_as_not_supplied_instead_of_dropping_them` | `run-m10/…_14_59_41.trx` f=1 → `:202`；re `…_15_51_17.trx` | `K:284` `Disposition = NotSupplied` → `Dropped` | lane(m10)＋守门人 | G13＋G13r | 单枚轮，红集 1 格 | B（A′） |
| 12 | `Round_calls_the_provider_exactly_once_for_one_narration` | **无凭证轮**：读了 42 份波 13 TRX，其中 30 份带突变轮里本格都不是 Failed（读过且为否） | 正向钉 `R:105`＋`R:107` `budget.Consume()` | 无红可判手别 | 无凭证轮，11 项全无值 | 不适用 | D |
| 13 | `Round_does_not_call_the_provider_when_the_context_is_rejected` | `run-m02/…_14_57_24.trx` f=3 → `:404`；**另有独署一轮** `run-m13/…_15_01_49.trx` f=1 → `:398`（栈 `N:38`←`R:105`←测试 `:398`）；re-m02 `…_15_50_16.trx`、re-m13 `…_15_51_41.trx` | 独署门 `R:95` `if (!assembly.Accepted)` | lane(m02,m13)＋守门人(re-m02,re-m13) | G13＋G13r | m02＝共用凭证（红集 3）；m13＝单枚轮红集 1 格（本格有独署） | B（A′） |
| 14 | `Second_call_on_the_same_round_budget_is_refused` | `run-m11/…_15_00_03.trx` f=1 → `:413`；re `…_15_51_29.trx` | `N:107` `if (next != 1)` | lane(m11)＋守门人 | G13＋G13r | 单枚轮，红集 1 格 | B（A′） |
| 15 | `Call_refuses_an_assembly_that_still_carries_a_rejection` | `run-m02/…_14_57_24.trx` f=3 → `:430`；**独署** `run-m12/…_15_01_38.trx` f=1 → `:425`；re-m02 `…_15_50_16.trx`、re-m12 `…_15_51_35.trx` | 独署门 `N:36` `if (!Assembly.Accepted)` | lane(m02,m12)＋守门人 | G13＋G13r | m02 共用；m12 单枚轮红集 1 格 | B（A′） |
| 16 | `Call_refuses_an_assembly_that_belongs_to_another_scope(scope:"instance")` | **无凭证轮**（读过且为否，同上措辞） | `N:43` `!Assembly.InstanceId.Equals(...)` | 无红可判 | 无值 | 不适用 | D |
| 17 | `Call_refuses_an_assembly_that_belongs_to_another_scope(scope:"conversation")` | **无凭证轮**（读过且为否） | `N:50` | 无红可判 | 无值 | 不适用 | D |
| 18 | `Call_refuses_an_assembly_that_belongs_to_another_scope(scope:"batch")` | **无凭证轮**（读过且为否） | `N:57` | 无红可判 | 无值 | 不适用 | D |
| 19 | `Round_never_rewrites_the_narration_after_the_single_call` | **无凭证轮**（读过且为否） | 正向钉 `R:136`＋`DeliveryLedger.cs:143`–`:155` | 无红可判 | 无值 | 不适用 | D |
| 20 | `Round_feeds_the_provider_the_compiled_context_and_nothing_else` | **无凭证轮**（读过且为否） | 正向钉 `R:105`＋`N:30` | 无红可判 | 无值 | 不适用 | D |
| 21 | `Silence_round_produces_no_assistant_message_and_no_intent` | **无凭证轮**（读过且为否） | `R:167`–`:171`（沉默支落点，`Disposition = …Silence` 那枚字节在 `:169`） | 无红可判 | 无值 | 不适用 | D |
| 22 | `Deferred_round_registers_the_intent_without_touching_real_history` | `run-m15/…_15_02_19.trx` f=1 → `:509`；re `…_15_51_52.trx` | `R:155` `if (deferred.Length > 0)` → `> 1` | lane(m15)＋守门人 | G13＋G13r | 单枚轮，红集 1 格 | B（A′） |
| 23 | `Draft_without_any_action_is_rejected_rather_than_read_as_silence` | **无凭证轮**（读过且为否） | `V:139` `content.Actions.Count == 0`（＋`R:137`） | 无红可判 | 无值 | 不适用 | D |
| 24 | `Malformed_response_is_not_masquerading_as_a_chosen_silence` | `run-m14/…_15_02_01.trx` f=**4** → `:540`；re `…_15_51_47.trx` f=4 | `R:115` `Disposition = ProviderRejected` → `Silence` | lane(m14)＋守门人 | G13＋G13r | 单枚突变／红集 4 格（同一行落点四枚见证，#24–27 共用，不独署） | B（A′) |
| 25 | `A_provider_that_ignores_cancellation_still_exits_as_timeout` | `run-m14/…_15_02_01.trx` f=4 → `:555`；重取 `run-re-m14/…_15_51_47.trx` f=4 → `:555` | `R:115`（同上共用） | 同上 | G13＋G13r | 单枚突变／红集 4 格 | B（A′） |
| 26 | `A_provider_that_honours_the_timeout_token_exits_as_timeout_too` | `run-m14/…_15_02_01.trx` f=4 → `:569`；重取 `run-re-m14/…_15_51_47.trx` f=4 → `:569` | `R:115`（同上共用） | 同上 | G13＋G13r | 单枚突变／红集 4 格 | B（A′） |
| 27 | `Provider_failure_becomes_a_named_error_shape_and_writes_nothing` | `run-m14/…_15_02_01.trx` f=4 → `:582`；重取 `run-re-m14/…_15_51_47.trx` f=4 → `:582` | `R:115`（同上共用） | 同上 | G13＋G13r | 单枚突变／红集 4 格 | B（A′） |
| 28 | `Host_cancellation_stays_cancellation_and_is_not_converted_into_a_round` | **无凭证轮**（读过且为否） | `R:327`–`:330` | 无红可判 | 无值 | 不适用 | D |
| 29 | `Deferred_intent_that_claims_to_be_fulfilled_without_delivery_is_refused` | **无凭证轮**（读过且为否） | `V:287` `Status is Fulfilled && content.Actions.All(...)` | 无红可判 | 无值 | 不适用 | D |
| 30 | `Deferred_action_without_a_pointing_intent_is_refused` | **无凭证轮**（读过且为否） | `V:235` `if (action.DeferredIntentId is null)` | 无红可判 | 无值 | 不适用 | D |
| 31 | `Immediate_action_binds_to_the_unique_slice_and_lands_three_segments` | 只有一枚**干净轮**红：`run-clean/…_14_50_15.trx` f=1 → `:636`，消息 `Assert.Equal() Failure: Values differ Expected: 3 Actual: 1`；`run-clean-b/…_14_51_59.trx`＝49/49/0；带突变的 30 份里本格都不是 Failed | 正向钉 `R:184`–`:212`＋`DeliveryLedger.cs:66` | 判据＝lane；那枚红无突变因（lane 自述"我的新格自己写错了预期"，§二.9），改预期后又补了一格更硬的（`SegmentIndex` 逐一体） | 无凭证轮（干净轮红不作凭证），11 项全无值 | 不适用（无突变轮） | D（有特殊形状，见 §三.3） |
| 32 | `Quote_absent_from_this_narration_rejects_the_action_without_host_invention` | **无凭证轮**（读过且为否） | `V:71` `if (occurrences == 0)`（`NarrativeProseBinder.Bind`）＋`V:90` `Apply` | 无红可判 | 无值 | 不适用 | D |
| 33 | `Ambiguous_quote_is_rejected_instead_of_the_host_choosing_one` | **无凭证轮**（读过且为否） | `V:76` `if (occurrences > 1)` | 无红可判 | 无值 | 不适用 | D |
| 34 | `Immediate_action_over_the_host_segment_ceiling_is_refused` | **无凭证轮**（读过且为否） | `V:200` 上界项 `> request.MaxOutboundSegments` | 无红可判 | 无值 | 不适用 | D |
| 35 | `An_over_split_slice_is_refused_by_the_existing_history_gate` | **无凭证轮**（读过且为否） | `DeliveryLedger.cs:130`–`:133`（空正文空贴纸那判，条件在 `:130`、拒绝在 `:132`） | 无红可判 | 无值 | 不适用 | D |
| 36 | `Immediate_action_carrying_a_future_intent_is_refused` | **无凭证轮**（读过且为否） | `V:214` `if (action.DeferredIntentId is not null)` | 无红可判 | 无值 | 不适用 | D |
| 37 | `Action_targeting_another_conversation_is_refused` | **无凭证轮**（读过且为否） | `V:191` `!string.Equals(action.TargetConversationId, ...)` | 无红可判 | 无值 | 不适用 | D |
| 38 | `Perceived_reference_outside_the_batch_is_refused` | **无凭证轮**（读过且为否） | `V:167` `if (perceivedReason != NarrativeContractReason.None)` | 无红可判 | 无值 | 不适用 | D |
| 39 | `Watermark_beyond_the_batch_upper_bound_is_refused` | **无凭证轮**（读过且为否） | `V:173` `!content.ProcessedThrough.WithinBatch(...)` | 无红可判 | 无值 | 不适用 | D |
| 40 | `Round_refuses_a_narration_without_prose_instead_of_binding_to_nothing` | **无凭证轮**（读过且为否） | `V:131` `string.IsNullOrWhiteSpace(content.NarrativeProse)`（＋`R:136`） | 无红可判 | 无值 | 不适用 | D |
| 41 | `Bound_action_keeps_a_reference_back_to_the_input_it_answers` | **无凭证轮**（读过且为否） | `R:133`–`:135`＋段 `SourceRefs` | 无红可判 | 无值 | 不适用 | D |
| 42 | `Draft_on_a_stale_story_version_is_refused_before_any_history` | **无凭证轮**（读过且为否） | `R:238` `draft.StoryVersionBase != request.StoryVersion` | 无红可判 | 无值 | 不适用 | D |
| 43 | `Draft_from_a_fenced_writer_epoch_is_refused` | **无凭证轮**（读过且为否） | `R:244` `draft.OwnershipEpoch != request.OwnershipEpoch` | 无红可判 | 无值 | 不适用 | D |
| 44 | `Draft_with_a_wrong_issue_baseline_is_refused_as_late` | **无凭证轮**（读过且为否） | `R:250` `draft.ReceivedThroughSeqAtIssue != ...` | 无红可判 | 无值 | 不适用 | D |
| 45 | `Contiguous_split_reproduces_the_bound_slice_verbatim` | **无凭证轮**（读过且为否） | `R:297` `baseLength + (i < remainder ? 1 : 0)`（`SplitContiguous` `R:286`–`:303`） | 无红可判 | 无值 | 不适用 | D |
| 46 | `Compiler_emits_the_eight_assembly_units_of_the_disk_builder_in_its_order` | **无凭证轮**（读过且为否） | `K:29` `SegmentNames`＋`K:49`／`K:93` | 无红可判 | 无值 | 不适用 | D |
| 47 | `Compiler_rejects_a_window_of_zero_tokens` | **无凭证轮**（读过且为否） | `K:52` 首项 `budget.ContextTokens < 1`（同一行的另一半，m01 只拆了 `x < 0` 那半） | 无红可判 | 无值 | 不适用 | D |
| 48 | `Compiler_carries_source_references_into_every_emitted_section` | **无凭证轮**（读过且为否） | `K:99`／`K:253`／`K:294` | 无红可判 | 无值 | 不适用 | D |
| 49 | `Compiler_honours_the_unified_budget_inequality_with_exact_numbers` | **无凭证轮**（读过且为否） | `K:178` `if (!assembly.WithinUnifiedBudget)`＋`K:443`（`WithinUnifiedBudget =>`） | 无红可判 | 无值 | 不适用 | D |
| 50 | `Blank_candidate_id_throws_rather_than_asserting_without_a_subject` | `run-R1/…_15_32_00.trx` f=1 → `:37`，消息 `Assert.Throws() Failure: No exception was thrown` | `V:306`–`:307` `RequireText(CandidateId)` 整行删 | 判据＝lane（`d3fbac4` 那份字节至今未改）；红＝**只有 lane 一手**（盘上无第二执行体的红） | G14 | 单枚轮，红集恰 1 格 | B |
| 51 | `Blank_claim_throws_rather_than_asserting_an_empty_reality` | `run-R2/…_15_32_05.trx` f=1 → `:59` 同形 | `V:308` | 同上（只有 lane 一手） | G14 | 单枚轮，红集 1 格 | B |
| 52 | `Blank_proposed_by_throws_rather_than_anonymous_confirmation` | `run-R3/…_15_32_11.trx` f=1 → `:81` | `V:309`–`:310` | 同上 | G14 | 单枚轮，红集 1 格 | B |
| 53 | `Blank_intent_id_throws_rather_than_an_addressable_promise` | `run-R4/…_15_32_16.trx` f=1 → `:93` | `V:266` | 同上 | G14 | 单枚轮，红集 1 格 | B |
| 54 | `Blank_intent_topic_throws_rather_than_an_intention_about_nothing` | `run-R5/…_15_32_21.trx` f=1 → `:105` | `V:267` | 同上 | G14 | 单枚轮，红集 1 格 | B |
| 55 | `Blank_action_id_throws_before_any_scope_decision_is_made` | `run-A1/…_15_32_27.trx` f=1 → `:122` | `V:189` | 同上 | G14 | 单枚轮，红集 1 格 | B |
| 56 | `Immediate_action_segment_above_host_ceiling_is_refused` | `run-A2/…_15_32_47.trx` f=1 → `:133`，消息 `Assert.Single() Failure: The collection was empty` | `V:200` 上界成员（`< 1 \|\| > Max` → `< 1`） | 同上 | G14 | 单枚轮（从 `\|\|` 集合摘一枚成员），红集 1 格 | B |
| 57 | `Immediate_action_that_also_hangs_a_future_intent_is_refused` | `run-A3/…_15_32_53.trx` f=1 → `:147` | `V:214`–`:218` 整门删 | 同上 | G14 | 单枚轮，红集 1 格 | B |
| 58 | `Deferred_action_reserving_delivery_segments_is_refused` | `run-A4/…_15_32_59.trx` f=1 → `:161` | `V:223`–`:227` | 同上 | G14 | 单枚轮，红集 1 格 | B |
| 59 | `Deferred_action_carrying_immediate_prose_is_refused` | `run-A5/…_15_33_04.trx` f=1 → `:185` | `V:229`–`:233` | 同上 | G14 | 单枚轮，红集 1 格 | B |
| 60 | `Silence_action_reserving_a_segment_is_refused` | `run-A6/…_15_33_10.trx` f=1 → `:209` | `V:249` 去 `count != 0 \|\|` | 同上 | G14 | 单枚轮（摘 `\|\|` 首成员），红集 1 格 | B |
| 61 | `Blank_commit_id_throws_rather_than_a_hashless_commit_identity` | `run-C1/…_15_33_30.trx` f=1 → `:236` | `V:387` | 同上 | G14 | 单枚轮，红集 1 格 | B |
| 62 | `Commit_with_an_unsupported_contract_version_is_refused` | `run-C2/…_15_33_36.trx` f=1 → `:248` | `V:389`–`:393` | 同上 | G14 | 单枚轮，红集 1 格 | B |
| 63 | `Commit_without_host_content_hash_is_refused` | `run-C3/…_15_33_41.trx` f=1 → `:259` | `V:395`–`:399` | 同上 | G14 | 单枚轮，红集 1 格 | B |
| 64 | `Commit_intent_without_source_is_refused_by_both_layers` | `run-C4/…_15_33_46.trx` f=1 → `:277`，消息独一格＝`Assert.Collection() Failure: Item comparison failure ↓ (pos 1)` | `V:403`–`:408`（含 `continue`） | 同上 | G14 | 单枚轮，红集 1 格；**唯一靠 Detail 才敏感的凭证**（`:403` 与 `:410` 同 `Reason`，摘 `:403` 后 Reason 与条数都不变） | B |
| 65 | `Commit_intent_whose_source_is_neither_commit_nor_batch_is_refused` | `run-C5/…_15_33_52.trx` f=1 → `:297` | `V:410`–`:415` | 同上 | G14 | 单枚轮，红集 1 格 | B |

### 三.2 四类计数（当场数，不按比例推）

| 类 | 定义（工单 §三 原口径） | 本 65 格实测 | 行号 |
|---|---|---|---|
| **A** | 单枚清洁红且字段齐（11 项逐轮全有原始值） | **0** | 无 |
| **B** | 单枚清洁红但字段缺若干 | **35** | 波 13：#1–11、#13–15、#22、#24–27＝19 行；波 14：#50–65＝16 行 |
| **C** | 只有批次轮红、无单枚轮 | **0** | 盘上 46 份带突变 TRX 全部 `minusLines=1`＋`REPLACEMENTS=1`，不存在批次突变轮（§三.0 末段） |
| **D** | 盘上找不到该格的任何红凭证 | **30** | #12、#16–21、#23、#28–49（其中 #31 有干净轮红但不作凭证） |
| 合计 | | **65** | 1–65 连续 |

- 宽档（A′，把 ④ 的登记替代口径、⑤⑧ 的整行指针、⑪ 的盘上整段原文都算作"有值"）＝**A′ 19（波 13 那 19 格）／B′ 16（波 14 那 16 格）／C 0／D 30**。波 14 在宽档下仍留 B，因为它的 **⑨⑪ 两项连盘上档都没有**（§二.3 那两行"发布面有、盘面无档"），指针式与真缺式不同档。
- 两种口径下 D 都不变：30 格没有任何带突变的红可读。

### 三.3 两句不许互换的措辞，本刀逐格用在哪（工单 §三 硬要求）

1. **"读了那份 TRX 而它没把这格打红"（读过且为否）**——用在全部 29 格从未红的波 13 行（#12、#16–21、#23、#28–49 中除 #31）与波 14 的 0 格。取法＝那 30 份带突变 TRX（`run-m01…m15`＋`run-re-m01…re-m15`）逐份读全部 `<UnitTestResult>` 节点并记 outcome 图（仪器 `analyze-cells.js`，输出 `.local-notes/wave22-1023/cells-analysis.txt`）；每份都是 total 49／executed 49 的完整轮，所以"没红"是**这一轮跑过本格且本格 Passed**，不是"这一轮没跑本格"。
2. **"盘上没有那份 TRX"（无凭据可读）**——本刀在这 65 格里**没有一格需要用它**；这一句在本报告里只出现在 §五（第十七波终局那轮四工程同名覆盖，盘上只剩 Core 那一份）与 §二.3（波 14 的 ⑨⑪ 归档文件不存在）。**两者的差别不是修辞而是事实类型**，所以 §四 的复采清单里 30 格全按"读过且为否"写，不写"没有凭证"。
3. **#31 那一格是第三种形状**，本刀单列：波 13 `run-clean` 里它红过（读过、有红），但那一轮**没有突变因**，所以既不是"读过且为否"也不是"无凭据可读"，写死为：**"有该格的干净轮红、无该格的突变轮红"**；按工单口径归 D，并把它的干净轮红文件名与消息原样留在表里，不涂掉。

### 三.4 §七.3 那张 re-m 表的 ① 列校正（本刀盘上取值）

`node .local-notes/wave22-1023/difflines.js`（当场跑，输出＝本报告引用的原始读数）逐份读 `out/diff-mNN.txt` 与 `out/diff-re-mNN.txt`，取 hunk 头 `@@ -L,7 +L,7 @@` 与**第一条 `-` 行在旧文件里的行号**：

| 轮 | §七.3 写的"文件:行" | 盘上 hunk 起始 | 盘上被改坏那一行 | §三 原表（lane）写的行 | `minusLines` |
|---|---|---|---|---|---|
| re-m01 | `K:51` | 51 | **54** | `:54` | 1 |
| re-m02 | `:59` | 59 | **62** | `:62` | 1 |
| re-m03 | `:68` | 68 | **71** | `:71` | 1 |
| re-m04 | `:75` | 75 | **78** | `:77`–`:78` | 1 |
| re-m05 | `:84` | 84 | **87** | `:87` | 1 |
| re-m06 | `:107` | 107 | **110** | `:110` | 1 |
| re-m07 | `:200` | 200 | **203** | `:203` | 1 |
| re-m08 | `:243` | 243 | **246** | `:246` | 1 |
| re-m09 | `:236` | 236 | **239** | `:239` | 1 |
| re-m10 | `:281` | 281 | **284** | `:284` | 1 |
| re-m11 | `N:104` | 104 | **107** | `:107` | 1 |
| re-m12 | `N:36` | 33 | **36** | `:36` | 1 |
| re-m13 | `R:92` | 92 | **95** | `:95` | 1 |
| re-m14 | `:112` | 112 | **115** | `:115` | 1 |
| re-m15 | `R:155` | 152 | **155** | `:155` | 1 |

15 行里 **13 行给的是 hunk 起始行**（只有 re-m12、re-m15 两行给到了被改坏那一行）。这不影响红格名、counters、红点行号那三栏（本刀逐栏与 15 份 TRX 对平过，见 §二.1），但**它意味着照 §七.3 的 ① 去当前树改字节会改错行**——本刀把这一条写进 §四 的落点选择依据（复采一律用 lane 那份行号，也就是上表第四列）。

（下一批：§四 后续复采清单。）

---

## 四 活单②｜后续复采清单（B／C／D 按"一轮回响的可行性"排序；本刀一枚都没跑）

### 四.0 本节的预算事实与一条硬矛盾（先登记，免得下一把按不存在的许可起跑）

- **定向轮消耗＝0／6**。工单 §四 把 ≤6 轮定向**只**许用在一种情形：某格报告声称有单枚红、而按 §二 的读法在指定 TRX 里读不出该格红。本刀把 35 格声称的单枚红**逐格在指定 TRX 里读出来了**（§三.1 的 ①④ 栏，46 份带突变 TRX 全读），**矛盾一格都没有**，所以该许可情形未发生，一次都没用。焦点预检因此也没起跑过（§〇.6）。
- **一处工单内部的矛盾，本刀取"不起跑"这一边并登记**：工单 §一.7 把跑法钉死为 `dotnet test <工程> -c Release --no-build --results-directory … --logger trx`，而**任何改字节的突变轮在 `--no-build` 下跑到的仍是上一次构建的旧 dll**——突变不生效、红不可能出现。第十三波 15 轮与第十四波 16 轮都**明确不带 `--no-build`**（两份报告分别写"所有轮次都不带 `--no-build`"／"绝不带 `--no-build`"），因为它们都在改字节。于是：本清单里 65 枚（含第二换手复采）**每一枚都需要一次编译**，而工单给本刀的构建预算＝**0**。含糊处登记：这不影响本刀结论（本刀零起跑），影响的是**下一把的账**——复采清单要成立，必须带一次构建或改用带构建的定向写法，这归守门人决定，本刀不自扩范围、也不替他选。
- **不需要构建的那一种才真能用掉这 6 轮**：把已有 TRX 的 counters／红格名／红点行**再读一遍做双人复核**（纯读数，不碰字节）。本刀已经把这一层做完了（§二、§三），所以下面清单**不列**这种。

### 四.1 清单（梯队 T1→T5；每行给格名／要改坏的 `src/` 那一行／预计红格／是否需要重新构建／波次）

行号一律＝**当前主树 `8c2f3e4` 实测值**（§三.0 已证这五枚 `src` 文件自第十三波载体基点起一字未动，故报告历史行号与当前树同值；re-m 那份表除外，见 §三.4）。文件简称同 §三.0：`K:`＝`src/Pry.Core/Narrative/NarrativeContextCompiler.cs`、`R:`＝`NarrativeRoundRunner.cs`、`N:`＝`NarratorContracts.cs`、`V:`＝`NarrativeDraftValidator.cs`、`D:`＝`DeliveryLedger.cs`。

| 梯队 | # | 格名 | 要改坏的那一行（当前树） | 预计红格（本刀读码给，非实测） | 需重新构建 | 波次 |
|---|---|---|---|---|---|---|
| **T1**（零设计成本：门与 anchor 都已在盘上，只差**另一双手**＋把 ⑨⑪ 归档补上） | 50–65 全部 16 格 | 见 §三.1 #50–65 | 与 §三.1 `门` 列逐格同（`V:306`–`:307`、`:308`、`:309`–`:310`、`:266`、`:267`、`:189`、`:200`、`:214`–`:218`、`:223`–`:227`、`:229`–`:233`、`:249`、`:387`、`:389`–`:393`、`:395`–`:399`、`:403`–`:408`、`:410`–`:415`） | 每轮恰 1 格（＝本格；16 轮实测红集都是 1，盘上可复核） | 是（改字节） | 第十四波 |
| **T2**（anchor 现成，但要**非 `66906d0` 作者**的手才谈得上第二换手；判据字节已被守门人改过） | 1–11、13–15、22、24–27 共 19 格 | 与 §三.1 `门` 列同（`K:54/:62/:71/:78/:87/:110/:203/:246/:239/:284`、`N:107/:36`、`R:95/:115/:155`）；**用 §三.4 第四列的行号，不要用 §七.3 的** | m 轮原红集：`m02`＝3 格（#2/#13/#15）、`m08`＝2 格（#8/#9）、`m14`＝4 格（#24–27），其余各 1 格 | 是 | 第十三波 |
| **T3**（拆**一行既有门**即响，门与格基本一一对应） | 16 | `Call_refuses_an_assembly_that_belongs_to_another_scope(scope:"instance")` | `N:43` `if (!Assembly.InstanceId.Equals(Request.InstanceId))` | 1 格＝本格（Theory 只此参数行倒） | 是 | 第十三波 |
| T3 | 17 | 同上（`scope:"conversation"`） | `N:50` | 1 格＝本格 | 是 | 第十三波 |
| T3 | 18 | 同上（`scope:"batch"`） | `N:57` | 1 格＝本格 | 是 | 第十三波 |
| T3 | 21 | `Silence_round_produces_no_assistant_message_and_no_intent` | `R:169` `Disposition = NarrativeRoundDisposition.Silence,` | 1–2 格（本格为主；`R:52` 的 `Accepted` 也读这枚 `Disposition`，可能连坐正向格） | 是 | 第十三波 |
| T3 | 23 | `Draft_without_any_action_is_rejected_rather_than_read_as_silence` | `V:139` `if (content.Actions.Count == 0)` | 1 格＝本格 | 是 | 第十三波 |
| T3 | 28 | `Host_cancellation_stays_cancellation_and_is_not_converted_into_a_round` | `R:327` `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)`（拆 `when` 谓词） | 1 格＝本格（§三.16 登记的"三条超时/取消路径"之一） | 是 | 第十三波 |
| T3 | 29 | `Deferred_intent_that_claims_to_be_fulfilled_without_delivery_is_refused` | `V:287` `if (intent.Status is NarrativeIntentStatus.Fulfilled && content.Actions.All(...))` | 1 格＝本格 | 是 | 第十三波 |
| T3 | 30 | `Deferred_action_without_a_pointing_intent_is_refused` | `V:235` `if (action.DeferredIntentId is null)` | 1 格＝本格 | 是 | 第十三波 |
| T3 | 32 | `Quote_absent_from_this_narration_rejects_the_action_without_host_invention` | `V:71` `if (occurrences == 0)` | 1–2 格（本格；#31 若同轮跑到也可能连坐） | 是 | 第十三波 |
| T3 | 33 | `Ambiguous_quote_is_rejected_instead_of_the_host_choosing_one` | `V:76` `if (occurrences > 1)` | 1 格＝本格 | 是 | 第十三波 |
| T3 | 34 | `Immediate_action_over_the_host_segment_ceiling_is_refused` | `V:200` 上界项 `action.PlannedSegmentCount > request.MaxOutboundSegments` | **波 13 本格＋波 14 #56** 同门（见 §四.2 那条成对观察）：一次定向若只按第十三波类过滤＝1 格，两类同跑＝2 格 | 是 | 第十三波（与第十四波同门） |
| T3 | 36 | `Immediate_action_carrying_a_future_intent_is_refused` | `V:214` `if (action.DeferredIntentId is not null)` | **波 13 本格＋波 14 #57**（同门） | 是 | 第十三波（与第十四波同门） |
| T3 | 37 | `Action_targeting_another_conversation_is_refused` | `V:191` `if (!string.Equals(action.TargetConversationId, request.ConversationId, ...))` | 1 格＝本格 | 是 | 第十三波 |
| T3 | 38 | `Perceived_reference_outside_the_batch_is_refused` | `V:167` `if (perceivedReason != NarrativeContractReason.None)` | 1 格＝本格 | 是 | 第十三波 |
| T3 | 39 | `Watermark_beyond_the_batch_upper_bound_is_refused` | `V:173` `if (!content.ProcessedThrough.WithinBatch(request.BatchHighestSeq))` | 1 格＝本格 | 是 | 第十三波 |
| T3 | 40 | `Round_refuses_a_narration_without_prose_instead_of_binding_to_nothing` | `V:131` `if (string.IsNullOrWhiteSpace(content.NarrativeProse))` | 1–2 格（本格；`:131` 也是 #23 走的路径之一） | 是 | 第十三波 |
| T3 | 42 | `Draft_on_a_stale_story_version_is_refused_before_any_history` | `R:238` `if (draft.StoryVersionBase != request.StoryVersion)` | 1 格＝本格（§三.16 快照三行之一） | 是 | 第十三波 |
| T3 | 43 | `Draft_from_a_fenced_writer_epoch_is_refused` | `R:244` `if (draft.OwnershipEpoch != request.OwnershipEpoch)` | 1 格＝本格 | 是 | 第十三波 |
| T3 | 44 | `Draft_with_a_wrong_issue_baseline_is_refused_as_late` | `R:250` `if (draft.ReceivedThroughSeqAtIssue != request.ProcessedThroughAtIssue.Seq)` | 1 格＝本格 | 是 | 第十三波 |
| T3 | 45 | `Contiguous_split_reproduces_the_bound_slice_verbatim` | `R:297` `var take = baseLength + (i < remainder ? 1 : 0);`（余数分配拆成恒 `baseLength`） | 1 格＝本格（`SplitContiguous` `R:286`–`:303` 组里唯一可单点拆的一行） | 是 | 第十三波 |
| T3 | 46 | `Compiler_emits_the_eight_assembly_units_of_the_disk_builder_in_its_order` | `K:29` `SegmentNames` 那一段（相邻两项换序，或删一项） | 1–3 格（本格必红；#48 与 #2 若同轮跑到可能连坐） | 是 | 第十三波 |
| T3 | 48 | `Compiler_carries_source_references_into_every_emitted_section` | `K:99` `SourceRefs = [payloads.IdentitySourceRef, payloads.CurrentEvent.SourceRef],`（改空集；`K:253`／`K:294` 是同一判据的另两处） | 1–2 格（本格；#41 同跑时可能连坐） | 是 | 第十三波 |
| T3 | 35 | `An_over_split_slice_is_refused_by_the_existing_history_gate` | `D:130` `if (string.IsNullOrEmpty(segment.Text) && string.IsNullOrEmpty(segment.StickerId))` | 1 格＝本格（注意：`D:` 这枚门是**既有**历史门，拆它动的不是第十三波自造的门） | 是 | 第十三波 |
| **T4**（拆既有行会**连坐**或**根本不响**，要新增形状突变／夹具改造） | 12 | `Round_calls_the_provider_exactly_once_for_one_narration` | 无单点可拆：`R:107` `budget.Consume()` 拆掉红的是 #14，不是本格；本格要的是"把 `R:105` 的调用复制一次"这一**新增形状** | 1 格＝本格（新形状需先设计） | 是 | 第十三波 |
| T4 | 19 | `Round_never_rewrites_the_narration_after_the_single_call` | `R:136` 之后追加一次改写（拆 `R:136` 或 `D:143`–`:155` 都会连坐 #31／#35） | 1–3 格 | 是 | 第十三波 |
| T4 | 20 | `Round_feeds_the_provider_the_compiled_context_and_nothing_else` | 需要给 provider 塞一份"多出来的料"，`R:105`／`N:30` 都不是可拆的门 | 1 格（设计题） | 是 | 第十三波 |
| T4 | 41 | `Bound_action_keeps_a_reference_back_to_the_input_it_answers` | `R:133`–`:135`（`Apply` 之后把回指丢掉）——与 #48 同面，拆哪一处需先定"回指"这一判据归谁 | 1–2 格 | 是 | 第十三波 |
| T4 | 47 | `Compiler_rejects_a_window_of_zero_tokens` | `K:52` 首项 `budget.ContextTokens < 1`——**拆它本格不红**：窗口 0 会落到 `K:62` 的 `total <= 0` 仍被拒（本刀读码所得，未跑）；要取证得先造"窗口 0 但 `total > 0`"的夹具形状 | 设计题（本刀不预测红数） | 是 | 第十三波 |
| **T5**（**拆门无人守**：这枚自检没有点名格，属"要新增判据"，超出复采范围） | 49 | `Compiler_honours_the_unified_budget_inequality_with_exact_numbers` | `K:178` `if (!assembly.WithinUnifiedBudget)`／`K:443` `WithinUnifiedBudget =>` 两行都拆不动这格——第十三波 §三.16 自己登记过："**无独立点名格**；m05 里以副作用理由现形（`BudgetInequalityViolated`），但没有一格是按名钉这枚理由码的格——**这是本波自知的判据弱点**" | 预计 0 格（＝拆了不响，正是要补的那件事实） | 是（若走"补一格按名钉 `BudgetInequalityViolated`"那条路，还要新增测试文件） | 第十三波 |
| **特殊** | 31 | `Immediate_action_binds_to_the_unique_slice_and_lands_three_segments` | 它唯一的红是**干净轮**红（`run-clean/…_14_50_15.trx`，`Expected: 3 / Actual: 1`），lane 当场自判"我的新格自己写错了预期"并把预期从 3 改成 1（§二.9）。要拿凭证得对 `R:184`–`:212` 或 `D:66` 挑一枚**会让段数变成 1 或 3 以外**的单枚拆法 | 1 格＝本格（历史 `:636`，当前树该格位置已因第十八波插入而下移，**下刀前须按当前树重新读一次该行**） | 是 | 第十三波 |

**清单合计＝30 行 D 类（T3 23／T4 5／T5 1／特殊 1）＋T1 16 格＋T2 19 格的换手复采＝65 格全部有下一把的去处。** T1／T2 两行是"一批一行"的写法（每行覆盖的是**同一动作：换一枚执行体，逐轮重跑已登记的那枚单点突变**），要逐格展开就是 §三.1 的 #1–11、#13–15、#22、#24–27 与 #50–65 那 35 行，落点与预计红集已逐格写在 §三.1 表里，本刀不在这里重抄第二遍。

### 四.2 三条会改变排序的观察（都来自盘上，供守门人定夺）

1. **第十四波那 16 格是现在最便宜的第二换手**：判据文件 `tests/Pry.Core.Tests/NarrativeCommitAndActionGuardTests.cs` 自 `d3fbac4` 归位后**一字未动**（`git diff --stat 290a603 HEAD --` 只把它列为新增的 412 行，无任何后续改动），`tools/driver.sh`＋`mutate.py`＋三份 spec（`batch1-3.txt`）都还在盘上，且 `src/Pry.Core/Narrative/NarrativeDraftValidator.cs` 自第十三波载体基点 `ac58245` 起未动。**另一双手重跑 16 轮 ⇒ 每轮只需补 ⑨⑪ 的归档（这两项盘面无档就是本刀读出来的）**，就能把这 16 格从"同一双手"里拿出来。第十三波那 19 格则卡在一个前置问题：`66906d0` 改过判据字节，所以取红者不能是该改动的作者——这一点是**在册口径**问题，不是盘上事实问题（盘上已有守门人的 20 格红，§一.3.2 双口径已分开写）。
2. **两枚门有成对见证**：`V:200` 同时把守波 13 #34 与波 14 #56，`V:214`–`:218` 同时把守波 13 #36 与波 14 #57。历史轮次各自只按自己的类过滤（`run-A2`／`run-A3` 每份 TRX 只有 16 个节点＝只跑了波 14 那一枚类；`run-m*` 每份只有 49 个节点＝只跑波 13），所以**同一枚突变今天能一次给两格凭证**，只是没人同跑过。下一把若要省轮次，`--filter "FullyQualifiedName~NarrativeCommitAndActionGuardTests|FullyQualifiedName~NarratorPrototypeTests"` 这种写法值得先做一次零成本验证（两枚测试文件同属 `tests/Pry.Core.Tests/Pry.Core.Tests.csproj`）；**本刀零 `dotnet`，没有验证过这条 filter，不当成结论**。
3. **T5 那一格（#49）与工单 §六.7 那条纪律相冲，本刀不裁**：给它补凭证的唯一路径是**新增一格按名钉 `BudgetInequalityViolated`**，而工单 §一.1 写"不改任何判据、不新增判据"。所以本刀把它列在清单末尾并写明"要的是新增判据，不是复采"，取"不动手、交守门人"这一边。

（下一批：§五 链条等式。）

## 五 链条等式（活单③：lane 撞线未交付，由守门人 10-23 19:3x 从盘上求和并署名）

仪器＝`.local-notes/chain-sums-1023.js`（node，只读 `.local-notes/` 下的 TRX 目录，不改仓内任何文件）。每环给目录名＋该目录 `*.trx` 份数＋从 `<Counters>` 当场求和的 Core 分项；加减一律写在"该环加减"列里并注明基点，不做心算。

| 环 | 证据目录（`.local-notes/` 下） | TRX 份数 | Core total／executed／failed | 该环加减与基点 |
|---|---|---|---|---|
| 1 | `qa-wave12-close-1023/runs/full-resample-close` | 4 | 558／556／0 | 起点＝第十二波收口后的全量 |
| 2 | `qa-wave13-1023/runs/run-full` | 4 | 607／605／0 | 558＋49（第十三波自跑，基点＝环 1） |
| 3 | `qa-wave14-1023/runs/run-full` | 4 | 574／572／0 | 558＋16（第十四波自跑，**与环 2 同基点并行**） |
| 4 | `qa-wave134-merge-1023/runs/run-full` | 4 | 623／621／0 | ＝558＋49＋16（13＋14 合流，守门人轮） |
| 5 | `qa-wave16-1023/trx-archive/run-16-full` | 4 | 636／634／0 | 623＋13（第十六波自跑） |
| 6 | `qa-wave17-1023/trx/final-full` | 1 | 641／639／0 | 623＋18（第十七波自跑，**与环 5 并行**；该目录只有 Core 那一份，不是全量，别拿它做总数） |
| 7 | `qa-wave1617-merge-1023/run1-full` | 4 | 654／652／0 | ＝623＋13＋18（16＋17 合流，守门人轮） |
| 8 | `qa-wave21-1023/runs/run-final-full` | 4 | 664／662／0 | 654＋10（第十八波归位） |
| 9 | `gate-wave20-1023/full` | 4 | 700／698／0 | 664＋36（第二十波归位）⇒ 在册总 **1321／1319／0／2** |
| 10 | `qa-wave23-1023/results/w23-run02-full` | 4 | 712／710／0 | 700＋12（第二十三波自跑）⇒ 本轮总 **1333／1331／0／2** |

全量四分项逐格核对（只有环 9／环 10 是解决方案级四份）：环 9＝Client 108＋App 233＋Api 280＋Core 700＝1321、executed 1319；环 10＝Client 108＋App 233＋Api 280＋Core 712＝**1333**、executed **1331**、failed **0**、`outcome="NotExecuted"` 节点 **2**（`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline` 与 `Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`，两份都在 Core 那枚文件里；四份的 `notExecuted` 计数器全为 0，照常驻纪律不看该计数器）。环 5–环 8 的四分项同样逐份读过，Client 108／App 233／Api 280 三环未动，只有 Core 在长。

### 五.1 这条链上有一处此前被写错的方向（工单 §〇 与本刀 §〇.3 都沿用了它）

工单与本刀入场那节把这条链写成一路加上去的序列，其中 574 排在 607 之后（差值 33）——**盘上没有任何一环给出 33 的加减**。实为：607＝558＋49（第十三波自跑）与 574＝558＋16（第十四波自跑）是**同一基点上的两枚并行读数**，合到线尾才出现 623＝558＋49＋16。本节按目录名与 `<Counters>` 逐环点名，不按序推导；引用链条时请以本表为准，工单 §〇 那个写法作废（工单原文照 §〇.6 的口径不改）。

## 六 撞线事实与守门人独立复核（活单①／② 的自述我没有采信，从原始 TRX 重取）

1. **撞线**：lane 最后一批落盘停在 §四 结尾那句「（下一批：§五 链条等式。）」，文件 410 行／71,462 字节；§五–§九（链条等式、出口读数、未验证、归位通道）未写，150 轮硬上限到点。盘上产物清点＝`.local-notes/wave22-1023/` 三份仪器＋四处证据目录共 71 份 TRX（42／21／4／4，本刀 §〇.4 当场 `find` 实测值）。**守门人没有替它补写任何一节内容并署它的名**；§五 由我署名，其余未交付项在本节登记为未交付。
2. **活单①（65 格四类）我独立重derive 过一遍**：仪器＝`.local-notes/wave22-verify-1023.js`＋`.local-notes/chain-audit-1023.js`（只读 §三 那张表＋46 份突变 TRX）。读数＝`ROWS=65 CLASS={"A":0,"B":35,"C":0,"D":30}`、`MUT_TRX=46 DISTINCT_MUT_REDS=35 B_without_red=[] D_with_red=[] red_not_in_table=[]`。⇒ 本刀那三条主声称（四类计数、B 那 35 格确有红、D 那 30 格确无红、且盘上没有一处红落在表外）**成立**，取证的手不是写表的手。
3. **双口径申明（这句不许被省略引用）**：盘上另有另一执行体的红 20 格（守门人的 `re-m01`…`re-m15`），**在册口径下这是 0 格第二换手**——因为 `66906d0`（守门人执笔）改过被守护判据的字节，按"取红者不能是该改动的作者"那条纪律，那 20 格不能算独立。两句话不可互换：**"盘上没人红过"是假的**，**"在册第二换手 0 格"是真的**。
4. 本刀 §三.4 那张 re-m ① 列校正（13／15 行写的是 hunk 起始行而非被改坏那一行）与三类字段档位（G13／G13r／G14）、第十四波缺 ⑨⑪ 两面归档——我复核过取值形状，原文照 lane，我不改一字。

## 七 对 §四.2 三条观察的裁定（守门人，10-23 19:3x）

1. **T1（第十四波 16 格）排进第二十四波第一梯队**：判据文件自 `d3fbac4` 归位后一字未动、驱动器与三份 spec 还在盘上，换一枚执行体重跑那 16 轮并把 ⑨⑪ 归档补齐，是当下最便宜的换手。第十三波那 19 格卡在在册口径（`66906d0` 的作者问题），**不因此作废**，归第二十五波另派。
2. **成对门（`V:200`／`V:214`）允许一轮双格凭证**，但归属必须按消息形状＋红点行号逐格点；两枚测试类同跑的 filter 写法（`FullyQualifiedName~A|FullyQualifiedName~B`）**lane 与我都没有验证过**，第二十四波工单要求先做一次零成本验证并登记，验证不过就退回逐类跑。
3. **T5 那格（#49，`BudgetInequalityViolated` 无人按名钉）我批准另起一把判据刀**：工单 §一.1 的"不新增判据"是对审计刀自身的约束，不是对全仓的约束，所以补一格按名钉那枚理由码不越权；**它不属于复采**，单列一波，不塞进复采轮的定向额度。
4. **特殊格 #31 押后**：它唯一的历史红是干净轮自判写错预期那一次，取凭证要先对当前树重读行号（第十八波插入使行位下移）——这条写进第二十五波工单的前置，不许按历史行号下刀。
5. **本刀的未交付面与状态**：§五 由守门人补署（活单③ 已交付），§〇／§一／§二／§三／§四 由 lane 交付并经我独立复核；**本报告不写状态词与结案词**，65 格的在册第二换手覆盖度在复采轮跑完前照旧＝0 格。
