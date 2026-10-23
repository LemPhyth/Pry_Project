# 第十三波工单·HDS-003 新生成中心隔离原型：七段上下文装配器＋一次 Narrator 调用形状，不接生产库、不碰真实模型

- 工单 id：`HDS-WAVE13-1023`
- 作者：守门人（架构／集成／发行）
- 出闸时间：2026-10-23 13:4x（当场 `date` 取时点，不许引用本行号之外的钟）
- 载体基点：**`ac58245`**（第十二波收口入库后的线尾）
- 执行方：冷启动 lane（除本工单与 `AGENTS.md` 四份必读之外无上下文）
- 性质：**动产品代码的原型刀**。这是本项目的主干工作量，不是判据刀；交付物允许新文件，**禁止**改动已有契约的既有语义。
- 作者现令（逐字）：「你干脆能做的一次性全做了，直接大跨步开发疯狂挂子智能体就好了，我想快些结束这个项目」「重启了，你开始疯狂赶进度吧」⇒ 本波与第十四／十五波**并行**，同一工作树面上互不相干的文件面。

---

## 〇 开工前必须当场做的三件事

1. `git -C C:/Users/24782/Desktop/Pry_Project status --short --branch`——他人名下未提交项一律当别人的资产：不读内容、不改、不提交、不 `stash`、不 `clean`。已知他人名下两枚暂存项＝`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`，**本波一律不碰**。
2. 建独立载体（**不动主工作树**）：
   `git -C C:/Users/24782/Desktop/Pry_Project worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave13-1023/carrier ac58245`
   入场两行判据原样贴进报告：`git status --short | wc -l` ＝ **0**，`git rev-parse HEAD` ＝ `ac58245` 全号。
3. **另两条 lane 在飞**（第十四波＝契约面判据刀，载体 `.local-notes/qa-wave14-1023/carrier`；第十五波＝零构建验收盘点）。你与它们**共用一台机器**：
   - 全量轮若出现 SQLite `disk I/O`／`out of memory`／`资源`形状失败（不是断言失败），判定为**读数作废**，换新的 TRX 目录重跑一次；两次都同形状就停刀回报，**绝不动别人的进程、绝不降判据**。
   - 一次只起一个 `dotnet` 调用，不要并发自己起两轮。

### 〇.4 边界（这一波允许动什么、绝不允许动什么）

允许（且只在这些路径新增文件）：

- `src/Pry.Core/Narrative/` 下的**新文件**；
- `tests/Pry.Core.Tests/` 下的**新测试文件**；
- 你自己的报告一份（主树 `docs/eval/reports/2026-10-23-hds003-narrator-prototype.md`，按批次增量落盘）。

**绝不允许**：

1. 修改 `src/Pry.Core/Narrative/NarrativeDraftValidator.cs`、`NarrativeDocuments.cs`、`NarrativeContractReason.cs` 的任何既有成员、条件、阈值或理由枚举取值（这三枚是 HDS-002 契约面，第十二波刚把 17 处判定钉成会响的格，动它们＝拆别人的判据）。**只允许新增文件里引用它们。**
2. 不接生产库、不接 API/Client、不接 `ApplicationService`、不动 SQLite 仓储、不动 `ContextBuilder.cs`（那是旧链的装配器，本波只做**新链的隔离原型**，替换发生在 HDS-010）。
3. **真实模型对照不在本波**：作者尚未点头"可以跑"，也不许假设运行时/模型在位。所有 provider 交互只能走**注入的确定性假 provider**。真模型那半一律登记为"未验证"。
4. Core 不依赖 Avalonia 或 HTTP（`AGENTS.md` 第 5 条）。
5. 数字门槛零改动：`.7`、`.5`、`2400`、五档超时、`MinimumUsableTokensPerSecond = 12`、`4096`、`DefaultSafetyTokens = 128`、`MinimumInputTokens = 512`、`docs/resource-limit-ledger.md` 全部不碰。
6. 零 `git tag`／零 `git push`／零打包／零对 `refs/heads/main` 的改动；`%LOCALAPPDATA%/PryCompanion` 不是测试载体；`licenses/**`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md`、模型／运行库／密钥／日志／用户导入内容一律不提交。
7. 不写状态词、不写"结案"、不自封基线。在册基线现值＝**1179／0／2**（分项 Client 108／App 233／Api 280／Core 558 中 exec 556），你只报你在载体上读到的数。

## 一 T1｜先盘现状，再决定缺哪几段（读码，零构建）

v4 §13 那一行给 HDS-003 的交付是逐字的：**"七段 Compiler、一次 Narrator、原文行动绑定、沉默/延迟、真实本地小模型与 API 对照；不接生产库"**（`docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md:322`）。请当场读回并把清单贴进报告 §一：

1. `src/Pry.Core/Narrative/` 目录里**每一枚现有类型**（文件名＋公开类型＋职责一句话），特别是 `NarrativeRequest`／`NarrativeDraft`／`NarrativeAction`／`NarrativeIntent`／`KnowledgeCandidate`／`CommunicationMode`／两个政策体／`ContractRejection`。
2. 旧链的上下文装配实际有几段、段名叫什么：读 `src/Pry.Core/**/ContextBuilder.cs` 全文，逐段点名（这是"七段"这个数字的盘上出处，不许照抄 v4 的文字当作事实）。
3. 现有生产入口到回复生成的调用链有哪几跳（`Runtime`／`ApplicationService`／`BehaviorPlanner`／`ReplyGenerationRenderer` 里点到即可，不改）。
4. **差集**：对照第 1、2 条，明确"新链已有／新链缺"的清单。本波只做缺的那几段。

判据：第 4 条的每一项都要给"我凭什么说它缺"的证据（grep 结果或文件名），不许用"看起来没有"。

## 二 T2｜落原型（新文件，消费既有契约，不新造平行契约）

按 T1 差集实现，最小形状要求：

1. **七段上下文装配器**：新文件（建议 `NarrativeContextCompiler.cs`），输出一个可枚举的"段"序列（每段有名字、来源引用、预算或截断策略字段）。段数与段名**照 T1 从 `ContextBuilder.cs` 盘到的实际段**，一一对应；若你盘出来不是七段，**以盘上为准**并在报告 §二 明写"v4 说七段、盘上是 N 段，实际是……"，不涂改 v4。
2. **一次 Narrator 调用**：新接口（建议 `INarrator`）＋请求/结果类型。**同一次叙事只允许一次调用**：装配器产出的上下文喂进去，产出的 `NarrativeDraft` 直接交给既有 `NarrativeDraftValidator` 校验，**不许出现第二次独立改写或第二个后台生成器**（v4 §14.1 那条"不存在第二个后台回复生成器"是本波的形状判据）。
3. **沉默与延迟**：沉默＝结果里零动作且**不产生助手消息**；延迟＝只登记 `NarrativeIntent`，**不得提前进入真实历史**。用既有 `CommunicationMode` 与 `NarrativeIntent`，不新造状态。
4. **原文行动绑定**：动作正文必须来自当次叙事、能指回输入引用；已有 `CheckAction` 的出口**照用**，不要复制一份平行校验。
5. provider 只通过注入的假实现参与测试；假 provider 要能产出"合法 draft"／"坏 JSON"／"超时"三种形状里**至少两种**，供 T3 反证用。
6. 每条新行为都要有**可失败的确切断言**（计数＋点名理由），不许 `Assert.Contains` 单独充当判据、不许 `> 0`／`NotEmpty`、不许 `Skip=`。

落码前后各数一次并原样贴进报告：`grep -c '\[Fact' <新测试文件>`、`grep -c '\[Theory\|\[InlineData' <新测试文件>`。**不许预测总条数**——本波是原型，允许你按 T1 差集决定规模，但报告必须给出"每一枚新格守哪一条 T2 行为"的对照表。

**干净态必须全绿**（正向探针）：`--filter` 只跑你的新测试类，`<Counters>` 原样贴出，`failed` ＝ 0，新增每一格按名出现在通过列表里。这一步没绿不许进 T3。

## 三 T3｜新落门逐枚单放反证（一轮一枚）

对 T2 新落的**每一道判定门**（装配器的截断/预算判定、单次调用的强制点、沉默与延迟的分岔、绑定校验）逐枚改坏，取"单枚、单轮、清洁"的红：

- 一轮只放一枚；批次红不许署成某一枚的凭证；红集比预期多一格就照盘上登记并读消息形状判因，**不改断言、不放宽、不加第二枚去凑**。
- 取不到红（改坏后全仓仍绿）＝**这就是本波要登记的空档**，写"该出口改坏后无格能响"并附 `<Counters>` 原文，不许删格、不许写"不需要拆"。
- 还原＝`git checkout -- <具体文件>`（只写具体文件，绝不 `git checkout -- .`），还原后 `sha256sum <文件>` 与 `git show HEAD:<文件> | sha256sum` 逐字节对平，两个哈希都贴出来。**你新写的产品文件不在"还原"范围内**（那是交付物），还原只针对本轮临时改坏的那一枚。
- 每轮十一项字段齐全（载体提交号／被改文件:行／before→after 原文取自当场 `git diff`／当场 `git diff --stat`／跑法整行命令／TRX 文件名／红格全名＋消息要点＋TRX 红点行号（另起一栏写源码里 assert 文本在哪行，两者不等不构成疑点）／还原方式／还原前后 SHA-256／还原后 `git status --short` 行数／结论"证明了什么·没证明什么·与谁共用凭证"）。
- TRX 规则照旧：`--results-directory "<绝对路径>"` **长参** ＋ `--logger trx`，一批一名不复用目录；跑完先数 `ls *.trx` 再读 `<Counters>`。**突变轮绝不加 `--no-build`**。

## 四 T4｜收尾那一轮（顺序不许换）

全部突变逐字节还原并哈希对平 ⇒ 在载体上 `git switch -c codex/wave13-20261023` ⇒ 只 `git add` 你自己名下那些新文件（产品码新文件＋新测试文件）⇒ `git commit --only <同一批路径>`（主树里他人名下有暂存项，裸 `git commit` 会消费别人的索引项）⇒ 解决方案级构建一次 ⇒ 全量一次（`--no-build` 此时才允许，因为紧接着构建且载体干净）。

全量读数要求：`ls *.trx | wc -l` ＝ **4**，四份 `<Counters>` 逐份原样贴出，写出总等式与各分项（Api 280／Client 108／App 233 应逐位不变，只有 Core 因你的新格上涨）。**取不到就登记差异，不许动别人的格来凑。**

预算硬顶（超支停刀回报，不要自己追加）：解决方案级构建 **1**／全量 **1**／按名定向 **≤16**（T2 干净态 1 ＋ T3 逐枚各 1）／机动 **2**。定向轮的隐式单工程重建另记不另算。

## 五 每次起跑之前（这条不打折）

**每一次 `dotnet` 调用之前**，先跑焦点预检并把整段原文贴进报告：

```
powershell -NoProfile -ExecutionPolicy Bypass -File .local-notes/game-preflight6-1022.ps1
```

- `VERDICT=BUILD_OK` 才许起跑；**`HOLD` 就停下**，不请示、不变通、不换目录、不"先跑一个小的"，在该轮报告里写"HOLD，停刀"，继续做不需要构建的部分。
- 纯读码与纯文档不需要预检。

## 六 交付形状（守门人收口时按这些取证）

- 分支 `codex/wave13-20261023`，提交号全号；`git show --stat` 只应含你名下那几枚新文件。
- 既有三枚契约文件（`NarrativeDraftValidator.cs`／`NarrativeDocuments.cs`／`NarrativeContractReason.cs`）磁盘哈希与 `git show HEAD:` 逐字节对平（当场贴两个哈希）。
- 第十二波那 62 格（`NarrativeIdentityContractTests`）在你的载体上仍全绿（收尾全量里 Core 的红必须为 0）。
- 载体收尾 `git status --short` ＝ 0 行、`git stash list` ＝ 0 条。
- 报告交付物＝`docs/eval/reports/2026-10-23-hds003-narrator-prototype.md`，报告 id ＝ `eval-QA-WAVE13-1023`；写主树那份报告只写你自己那一个文件。
- 无法完成的部分**明写"未验证"**，尤其：真实模型对照、生产接线、UI 可见行为。不许把推演写成事实，不许用大量绿例掩盖某枚没取到红。
