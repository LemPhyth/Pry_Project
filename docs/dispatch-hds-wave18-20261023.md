# 第十八波工单·判据刀（零产品代码）：把第十三波留下的 9 组"落门没有单枚反证"与"放宽方向无人守"逐枚钉成会响的格，顺带最后一次尝试 W2 的可达配方

- 工单 id：`HDS-WAVE18-1023`
- 作者：守门人（架构／集成／发行）
- 出闸时间：2026-10-23 16:1x（**当场 `date` 取时点**）
- 载体基点：**`def0faf`**（当场 `git rev-parse HEAD` 复核）
- 执行方：冷启动 lane
- 性质：**纯判据刀，`src/` 一字不动**（出口判据：`git diff --stat <基点>..HEAD -- src` 必须为空，空不了就停刀回报）。
- 作者现令（逐字）：「你干脆能做的一次性全做了，直接大跨步开发疯狂挂子智能体就好了，我想快些结束这个项目」

---

## 〇 开工前必须当场做的三件事

1. `git -C C:/Users/24782/Desktop/Pry_Project status --short --branch`；他人名下未提交项（两枚暂存项＋`docs/research/*`＋`docs/character-learning-safety.patch`＋`.qoder/`）不读、不碰、不提交。
2. 建独立载体：`git -C C:/Users/24782/Desktop/Pry_Project worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave18-1023/carrier def0faf`；入场两行判据贴进报告。
3. 每次 `dotnet` 前跑 `.local-notes/game-preflight6-1022.ps1`：`BUILD_OK` 才起，`HOLD` 就停下等待。一次只起一个 `dotnet`；资源形状失败＝读数作废换目录重跑，两次同形状停刀。

### 〇.4 允许动什么

- **只允许**修改 `tests/Pry.Core.Tests/NarratorPrototypeTests.cs`、`tests/Pry.Core.Tests/NarrativeIdentityContractTests.cs`、`tests/Pry.Core.Tests/NarrativeStorageContractTests.cs`、`tests/Pry.Core.Tests/ContextWindowLoadTests.cs`、`tests/Pry.Core.Tests/ContextBudgetTests.cs` 里**新增**判据，以及在 `tests/Pry.Core.Tests/` 新增一份测试文件；
- 报告一份＝主树 `docs/eval/reports/2026-10-23-qa-wave18-unheard-gates.md`（id＝`eval-QA-WAVE18-1023`），按批次增量落盘。

### 〇.5 绝不允许（反向约束）

1. 改 `src/**` 任何字节（包括"顺手修个小 bug"——发现缺陷就登记，不动手）。
2. 删除、注释、跳过、放宽任何既有断言或既有格；既有格数口径＝`NarratorPrototypeTests` 49／`NarrativeIdentityContractTests` 62（`[Fact] 21`＋`[Theory] 4`＋`[InlineData] 41`）／`NarrativeCommitAndActionGuardTests` 16，本波只许往上加，加完要在报告里给新的等式。
3. 改任何数字门槛（`.7`／`.5`／`2400`／五档超时／`12`／`4096`／`128`／`512`）；不许为了拿到红而构造"只有测试里才成立"的假前提。
4. 自造同义判据凑数：一枚新格必须点名它守的是哪一处具体判定（文件＋行号＋那一句代码），并说清"改坏它今天本来零代价"的证据（当场 `grep`／读码）。
5. 零 tag／push／打包；不 merge 主树；不自封基线；不写状态词。

## 一 活单（逐条一行，先数盘上再动手）

1. **第十三波那 9 组"落门没有单枚反证"**：来源＝`docs/eval/reports/2026-10-23-hds003-narrator-prototype.md` §三.16 与 §五 待拆清单（含 `NarrativeRoundRunner.cs:313-321`／`:331-334`／`:327-330` 三处超时／取消支，以及那份报告逐组点名的其余落门）。逐组先当场重读那份报告的原文，再判断"这枚红今天能不能拿到"：**能拿到就取一枚单枚清洁红；拿不到就写清够不着的原因，并连"布尔返回门与 fallback"一起算**（前例：W2 就是这样一直够不着）。**不许**把"防御纵深的红"写成已证。
2. **放宽方向（正向 `→ None`／`→ 放行`）无人守**：第十二波登记的"13 枚出口只有收紧方向有实测红"那批，逐枚补方向对称格——收紧侧红过，放宽侧也必须有一枚会响。
3. **W2 最后一次可达性尝试**（口径照旧：分母 46／已付 45／欠 1＝W2）：`ContextWindowLoadTests.Every_downgrade_candidate_keeps_a_positive_input_budget` 至今无实测红。要拿到它，需要一个配方让 `ContextBuilder.cs:39` 返 true 同时降级候选的输入预算非正。**先推演、再实测**：推演出配方就取一枚单枚清洁红；推演不出就登记"仍无配方"，并把这一轮尝试过的形状写全（不许写"结构上永远够不着"，也不许写"不需要拆"）。
4. **新格必须真的会响**：每枚新格都要拿到单枚、单轮、清洁的实测红，十一项字段逐轮登记（载体全号／文件:行／改前→改后取自当场 `git diff`／`diff --stat`／完整跑法／TRX 文件名／红格全名＋消息＋锚点／还原方法／改前改后 SHA-256／还原后 `git status --short` 行数／结论）。**正向探针≠门在守**：写完一枚新格先证明"把它改坏会红"，否则登记为常绿门并交回守门人。
5. **构建告警零自造**：`xUnit2013`（`Assert.Equal(0/1, X.Count)` → `Assert.Empty`／`Assert.Single`）与 `xUnit1051`（接 CT 的方法没传令牌 → `TestContext.Current.CancellationToken`）这两类是历史高发；写完定向跑一次看构建读数，别把 46 条告警那种账留给守门人。注意：`Assert.Equal(2/3/4, X.Count)` 是**确切计数**判据，不许硬换成 `Single` 来消告警。

## 二 读数与交付

1. 载体一次构建 ⇒ **0 警告 0 错误**。
2. 定向若干轮（每轮一枚突变，跑前预检、跑完逐字节还原并双哈希对平）＋**收尾一轮全量**：`dotnet test Pry.slnx -c Release --no-build --results-directory "<绝对>" --logger trx` ⇒ `ls *.trx` 数到 4 再读 `<Counters>`，报四分项＋总数。只报读数，**不自封基线**；在册基线由守门人按"两只手"规矩处置。
3. TRX 纪律：每轮独立目录、突变轮零 `--no-build`、跳过按 `outcome="NotExecuted"` 点名（`notExecuted` 计数恒 0）。
4. 报告要有一节"改坏零代价清单复查"：本波新扫出来的零代价出口（如果有）逐枚点名并留给下一刀，**不在本波动产品码修它**。
5. 报告末尾给守门人"归位提示"：`git show --stat` 全号、每枚改动文件行数与 SHA-256、`git diff --stat def0faf..HEAD -- tests` 与 `-- src`（后者必须空）。

## 三 预算与停刀线

构建 1 次、全量 1 次、定向 ≤ 24 次；撞 150 轮上限或预检连续两次 `HOLD` ⇒ 停刀、增量报告落盘、如实回报"已取到几枚、还剩几枚"。**宁可少几枚也不要补写没有的证据。**
