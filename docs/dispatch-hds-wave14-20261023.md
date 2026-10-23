# 第十四波工单·契约面待拆清单：把上一波登记下来的 16 枚"没人证过"的拒绝出口逐枚钉成会响的格，再逐枚取单枚清洁红

- 工单 id：`HDS-WAVE14-1023`
- 作者：守门人（架构／集成／发行）
- 出闸时间：2026-10-23 13:4x（当场 `date` 取时点）
- 载体基点：**`ac58245`**（第十二波收口入库后的线尾）
- 执行方：冷启动 lane
- 性质：**判据刀，零产品代码**。算法与凭证形状完全照第十二波（`docs/dispatch-hds-wave12-20261023.md` 与其收口报告 `docs/eval/reports/2026-10-23-qa-wave12-narrative-contract-unheard-exits.md`），先读那两本再动笔。
- 作者现令（逐字）：「你干脆能做的一次性全做了，直接大跨步开发疯狂挂子智能体就好了，我想快些结束这个项目」⇒ 本波与第十三／十五波**并行**。

---

## 〇 开工前必须当场做的三件事

1. `git -C C:/Users/24782/Desktop/Pry_Project status --short --branch`；他人名下两枚暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）**一律不碰**，不 `stash`、不 `clean`。
2. 建独立载体：`git -C C:/Users/24782/Desktop/Pry_Project worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave14-1023/carrier ac58245`；入场判据原样贴报告：`git status --short | wc -l` ＝ **0**，`git rev-parse HEAD` ＝ `ac58245` 全号。
3. **起点成立的代码面判据**：`git -C <主树> diff --name-only ac58245..<本工单那把提交> -- src tests` 必须为空（本波出闸是纯文档刀）。若非空，停刀回报。

### 〇.4 与另外两条 lane 的相处

第十三波在 `.local-notes/qa-wave13-1023/carrier` 动产品码（`src/Pry.Core/Narrative/` **新文件**），第十五波零构建。你：

- **只允许新增测试文件**（建议 `tests/Pry.Core.Tests/NarrativeCommitAndActionGuardTests.cs`，命名可自定）；**不碰** `NarrativeIdentityContractTests.cs`（那是第十二波刚归位的 62 格），不碰任何既有断言，不碰 `src/**`（除临时改坏再逐字节还原）。
- 全量轮若出现 SQLite `disk I/O`／`out of memory`／资源形状失败（非断言失败），判定读数作废、换新 TRX 目录重跑一次；两次同形状就停刀回报，**不动别人进程、不降判据**。一次只起一个 `dotnet`。

## 〇.5 上一波登记的空档（盘上出处＝第十二波报告 §六 待拆清单）

| 组 | 出口 | 我给的锚（必须当场 `sed -n '<a>,<b>p' | cat -n` 读回原文再动手，读不回就登记"锚点不符"并停该枚） |
|---|---|---|
| RequireText 抛异常型（5 枚） | `NarrativeGuard.RequireText` 被 `candidate.CandidateId`／`candidate.Claim`／`candidate.ProposedBy`／`intent.IntentId`／`intent.Topic` 调用 | `src/Pry.Core/Narrative/NarrativeDraftValidator.cs:306`、`:308`、`:309`、`:266`、`:267` |
| 动作侧（6 枚） | `NarrativeDraftValidator.CheckAction` 内 `switch (action.Mode)` 那三段的各条拒绝出口 | 同文件 `:189`、`:200`、`:214`、`:223`、`:229`、`:248` |
| 提交侧（5 枚） | `NarrativeCommitValidator` 的全部出口 | 同文件 `:387`、`:389`、`:395`、`:403`、`:410` |
| 不补格的一枚 | `CognitionKind.CharacterBelief` 在两政策体零条款 ⇒ 六来源全 `None`（`NarrativeDocuments.cs:138`） | **只登记，不补判据**（补了不构成任何出口的把守）；若要真正管它需要改契约，属产品决策，交回守门人 |

计数口径：5＋6＋5＝**16 枚**。守门人在一页纸 §0 写的是"13 枚"，那是粗略口径，**以本表为准**；这条更正由守门人收口时进 §9，你不改一页纸。

## 一 T1｜出口表做全（读码，零构建）

把上面三组逐枚列成表：行号＋条件原文＋拒绝理由（或抛出的异常类型）＋"被哪一条既有用例的哪一行命中／无命中"。命中判定必须给证据（测试文件名＋行号＋那条 `Assert` 的原文），不许用"应该有覆盖"。

- `RequireText` 那 5 枚是**抛异常**不是进 `rejections`——判据形状要按"当场抛、异常类型与消息点名"来写，别把它写成计数断言。
- 我点名的 16 枚若与盘上对不上，**以盘上为准**并在报告 §一 明写"工单点名的第 N 枚与盘上不符，实际是……"，不涂改本工单。
- 盘上如果还有我没点名的、同样"改坏零代价"的出口，一并列进表的"待拆（本波不做）"一栏，**不进本波预算**。

## 二 T2｜落判据（新增文件，不改不删任何既有断言）

1. 每一枚无命中出口各造一条**会响**的判据。断言必须确切：计数＋逐条 `Reason` 的点名等值断言；抛异常型用 `Assert.Throws<…>` ＋消息里点名的字段/理由。**不许** `Assert.Contains` 单独充当判据、不许 `> 0`／`NotEmpty`／`Assert.ThrowsAny`、不许 `Skip=`。
2. 配方天然产生多条拒绝的，**逐条都钉**（计数＋各自理由）。
3. 正向行（合法输入 → 零拒绝）与反向行（改坏 → 该理由）成对，用来同时把守"收紧"与"放宽"两个方向；本波若只做了反向，报告里明写"放宽方向未验证"。
4. 阈值一个都不动（`.7`／`.5`／`2400`／五档超时／`12`／`4096`／`128`／`512`／`docs/resource-limit-ledger.md`）。
5. 落码前后各数一次并原样贴：`grep -c '\[Fact' <新文件>`、`grep -c '\[Theory\|\[InlineData' <新文件>`，并给出"新用例总条数"与它守的出口对照表。
6. **干净态必须全绿**（正向探针）：`--filter` 只跑你的新类，`<Counters>` 原样贴出，`failed` ＝ 0，新增每格按名点名。没绿不许进 T3。

## 三 T3｜逐枚单放突变，各取一枚"单枚、单轮、清洁"的红

对 T2 新钉的每一格，把**它守的那一枚**改坏（删整门，或从 `or`／`is not (…)` 集合摘成员；`RequireText` 那组＝把该次调用整行删掉或让它恒不抛）。每轮只放一枚。硬规矩照第十二波工单 §三逐条执行：批次红不许署名单枚；红集多一格照盘上登记并读消息形状判因；取不到红就登记"该出口改坏后无格能响"并附 `<Counters>`；既有格不重复领凭证（动笔前 `grep docs/eval/reports/ docs/dispatch-*.md` 该文件名的在册突变凭证，第十二波已把 `NarrativeDraftValidator.cs` 的 20 枚入账——你那 16 枚若与它们重叠，登记"同格第二条因"，不计入本波新格）；还原＝`git checkout -- <具体文件>`，前后 SHA-256 逐字节对平并两值都贴；TRX 长参＋`--logger trx`＋一名不复用，跑完先数 `ls *.trx`；**突变轮绝不加 `--no-build`**；每轮十一项字段齐全。

允许写突变驱动脚本（≤6 轮一份），但证据只能从脚本当场打印的原文里抄；**驱动打印 `skip` 或锚点没命中＝这一轮没有非空转证据**，必须登记并手改重跑（前案：`eol=crlf` 让按 `\n` 拼的锚点恒不命中）。

## 四 T4｜收尾那一轮（顺序不许换）

全部突变还原并哈希对平 ⇒ `git switch -c codex/wave14-20261023` ⇒ 只 `git add` 你自己那枚新测试文件（若确实需要动第二个文件，先停刀回报）⇒ `git commit --only <同一份路径>` ⇒ 解决方案级构建一次 ⇒ 全量一次（此时才允许 `--no-build`）。

读数要求：`ls *.trx | wc -l` ＝ **4**，四份 `<Counters>` 逐份原样贴出并写总等式。预期＝Api 280／Client 108／App 233 逐位不变，Core 因你的新格上涨；红 0（若出现 Client `DeadLoopbackReconnectTests` 那格的一枚红，那是第十二波在册的**未定性环境读数**，照盘上登记、不动它、不反复重试超过一次）。取不到就登记差异，不许动别人的格来凑。**在册基线 1179／0／2 不由你自封**，你只报读到的数。

预算硬顶（超支停刀回报）：解决方案级构建 **1**／全量 **1**／按名定向 **≤22**（干净态 1 ＋ 逐枚 16 ＋ 预留 5）／机动 **2**。

## 五 每次起跑之前（这条不打折）

每次 `dotnet` 之前先跑并把整段原文贴进报告：`powershell -NoProfile -ExecutionPolicy Bypass -File .local-notes/game-preflight6-1022.ps1`。`BUILD_OK` 才起跑；**`HOLD` 就停下**，不请示、不变通、不换目录、不"先跑一个小的"，在该轮写"HOLD，停刀"并继续做零构建部分。

## 六 边界与交付

1. 零产品代码：交付物里 `src/` 必须零改动（当场 `git diff --stat HEAD -- src` 自证为空，且三枚契约文件哈希与 `git show HEAD:` 对平）。
2. 不许放宽／删除／注释／跳过任何既有断言；不许加 `Skip=`；不许为了凑绿改别人的格。
3. 只在载体里写；主树唯一允许的本波笔迹＝你的报告，你在主树**不做任何 `git add`／`commit`**。
4. 零 `git tag`／零 `git push`／零打包／零对 `refs/heads/main` 的改动；`%LOCALAPPDATA%/PryCompanion` 不是载体；`licenses/**`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md` 一字不碰；模型／运行库／密钥／日志／用户导入内容不提交。
5. 报告按批次**增量落盘**＝`docs/eval/reports/2026-10-23-qa-wave14-narrative-commit-action-unheard.md`，报告 id ＝ `eval-QA-WAVE14-1023`。
6. 无法完成的部分**明写"未验证"**，不许把推演写成事实，也不许用大量绿例掩盖某枚没取到红。
7. 代码适用 `AGENTS.md` 第 14 条指向的 `docs/agent-engineering-standards.md`：交付标准是"用户实际能完成的行为"，编译过／测试绿本身不算交付完成。
