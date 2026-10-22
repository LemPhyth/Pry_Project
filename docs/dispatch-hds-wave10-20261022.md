# 派工单·第十波（三件证据债：一枚没人单独拆过的产品行 ＋ 一枚只能由守门人自己拿的红 ＋ 一条够不够得着的推演）

派发人：守门人（Architecture / Integration / Release）。认领人：冷启动 lane（本报告之外不得假设任何上下文）。
出闸时间：2026-10-22 22:29。坐标系：`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS 表＋§9 交接；本单不属于任何"阶段"，阶段指针已由作者 10-19 裁定移除。

## 〇 起点读数与挪树（先落盘，再碰任何代码）

1. 主树只做只读测量：`git -C C:/Users/24782/Desktop/Pry_Project status --short --branch`、`git log --oneline -3`、`git rev-parse HEAD`。**主工作树里你一个字都不许写**（唯一例外见 §六 交付物）。
2. 独立载体：`git -C <主树> worktree add --detach <主树>/.local-notes/qa-wave10-1022/carrier 2002210`，进载体后 `git status --short` 必须＝0 行、`git rev-parse HEAD` 必须等于 `2002210`（守门人出闸时实测的线尾）。把这两行原样贴进报告 §〇。**"起点成立"的判据是代码不是文档**：载体钉在 `2002210`，而主树此后可能再落守门人自己的纯文档登记刀——那不是你的起点缺陷。所以跑之前当场数一次 `git -C <主树> diff 2002210..<主树当前线尾> -- src tests`，**为空**＝起点成立（照旧跑），**不为空**＝有人动了产品码，停刀登记差异、不要在旧树上继续。
3. **起点格账从盘上取，不许做加减**（这是守门人 10-22 深夜自己踩过的坑，写进模板）：本单三处锚点你都要**当场读回该行原文**（`sed -n` 或 Read）并按下面逐条核对；守门人已在出闸前把每一行原文读过一遍（`src/Pry.Core/Prompting/ContextBuilder.cs` 在工作树里与 `git show ab9b3cf:` 侧逐字节相同，sha `c5da1e3e…bae3ef`）：

   | 锚点 | 盘上原文（行号以载体钉住的 `2002210` 为准；守门人出闸后实测 `git diff ab9b3cf..2002210 -- src tests`＝空，两格的 `src/`／`tests/` 逐字相同） |
   |---|---|
   | `src/Pry.Api/Services/ModelPerformancePolicy.cs:47` | `if (!ContextBudget.TryNormalizeWindow(contextSize, intended, out var ctx, out var output, out _))` |
   | `src/Pry.Core/Narrative/DeliveryLedger.cs:79` | `if (!_commits.TryGetValue(key.CommitId, out var commit))`（`:81` 是拒绝码 `SegmentCommitUnknown`） |
   | `src/Pry.Core/Prompting/ContextBuilder.cs:34`／`:36`／`:39` | `:34` `var capped = Math.Min(Math.Max(requestedOutputTokens, 0), Math.Max(0, contextTokens / 2));`；`:36` `if (ceiling < capped) capped = Math.Max(0, ceiling);`；`:39` `return contextTokens > 0 && inputBudget >= MinimumInputTokens;` |
   | 测试红点 | `tests/Pry.Api.Tests/ContextWindowLoadTests.cs:53`（T1）；`tests/Pry.Core.Tests/NarrativeStorageContractTests.cs:327`（T2），同法 `:270` 是那条必须保持绿的用例 |

   **两处前案记录与本单不一致，本单按盘上读数登记**：① 第八波记录里同一枚夹零既被写成 `ContextBuilder.cs:24`（§〇 更正段的原文）又被写成 `:34`（§十二 的 M-CB1），而盘上 `:24` ＝ `public long InputBudget => (long)ContextTokens - OutputTokens - SafetyTokens;`、`:34` ＝ 半窗 `Math.Min(Math.Max(...))` 那一行——你**在 §〇 顺带把这两行原文都读回并登记哪一行才是那次突变的目标**，不要择一采信；② 一页纸与第八波把调用方写成 `ModelProcessPolicy.WithContext`，真文件＝`src/Pry.Api/Services/ModelPerformancePolicy.cs`（方法 `WithContext:39`，兜底行 `:48`）。第九波 §〇.5 关于第 10 格那条（`NarrativeCommitWriter.cs:141–142` 与 `DeliveryLedger.cs:79` 是两条不同产品行）本波**不重做、也不领第二次凭证**。若盘上原文与本单不符，**就地登记更正**（写"工单说 X，盘上是 Y"），不涂改本单原文，也不据此停刀。

## 一 三件活

### T1｜W3 的第三人复采：那一枚"只由守门人自己拿过"的单点红

现状（照第八波 §十二 的登记，载体已不在、TRX 在盘上 `.local-notes/qa-wave8-1022/run8-gk-mw3-only/…19_07_54.trx`）：那一轮把 **M-CB1（`ContextBuilder.cs:34` 半窗夹）＋ M-CB6（`:35` 的 `- MinimumInputTokens`）＋ M-W3（`ModelPerformancePolicy.cs:47`）三枚同时放**，报出来的是 `ContextBudgetException`（抛出点 `Prompting/ContextBuilder.cs:46`、测试点 `:52`），于是**那一格里 M-W3 从来没被单独观察到**——按逐值算术那条红是前两枚的合力。守门人随后在同一载体**只放 M-W3 一枚**（`intended` → `intended / 4`）另跑一次，拿到干净红：Api total 278／failed **1**＝`Pry.Api.Tests.ContextWindowLoadTests.ForModelWindow_agrees_with_the_loader_on_the_default_profile`，消息 `Assert.Equal() Failure: Values differ / Expected: 8192 / Actual: 16384`，红点锚 `tests/Pry.Api.Tests/ContextWindowLoadTests.cs:53`。**那是守门人自证，不是执行方拿的。**

要你做：**在你自己这棵载体里只放 M-W3 一枚**（配方逐字照上：`:47` 的第二个实参 `intended` → `intended / 4`，其余一字不动），按名定向重跑那一格，回答"红的是不是且只是 W3 那一格"。

- 被改行原文先读回报 §〇；只改**判据指向的那个表达式本身**，不许注释、不许新建文件、不许顺手改文案。
- 同一轮里 **W1／W2／S1～S3 必须全绿**（守门人轮实测如此）；这就是这一枚的正向探针——若 W2 也红了，说明你带来的不止一枚突变，先停下登记。
- 预期红点若与上面那条不一致（行号、消息形状、failed 数），**如实登记差异**，并当场 `git log ab9b3cf..HEAD -- src tests` 与 `git log 6b6f549..ab9b3cf -- src tests` 各数一次贴进报告，说明是不是产品行漂移所致（本仓库口径：`git log -- <路径>` 的行数不是 dotnet 轮数，别混用）。
- 跑完逐字节还原（`git checkout -- src/Pry.Api/Services/ModelPerformancePolicy.cs`，之后 `git show HEAD:<file> | sha256sum` 与还原后文件 `sha256sum` 对平；HEAD 侧目标值＝`3f18ee19e64175fc2141f00b3ff3b2e787f15f6114469dbb2eea5c77337eaf52`），`git status --short`＝0 行。

### T2｜第三人复采第七波那枚"只由守门人拿过"的红

现状：`DeliveryLedger.cs:79` 把查找键换成 `"commit-1"` 之后，`Late_commit_from_a_lost_ownership_epoch_is_rejected_without_overwriting_anything` 的 `:327` 立刻红——这一枚是**守门人自证**（`eval-QA-WAVE7-1022` §4.1，载体 `6b6f549`，TRX `…_17_45_24.trx`）。按本仓库立的规矩，写判据的人自己拿的红要换一双手复判才算"既可响也不误报"。

要你做：**在你这棵载体里重复同一枚突变**（原文与配方见上一段，逐字照做：只换 `_commits.TryGetValue(key.CommitId, …)` 的查找键，`:81` 的拒绝码与文案一字不动），跑按名定向，确认：

1. 红点落在 `NarrativeStorageContractTests.cs:327`（不是 `:270`、不是别的行）；
2. **同批不许带别的突变**——这一枚必须单独一轮（单突变轮是清洁凭证，批次红只能证明"这几枚里至少一枚能打红这一格"）；
3. 附带的"不应红"清单：`Segments_cannot_be_appended_for_a_commit_that_never_applied`（`:270`）在这一枚下**必须保持绿**（第七波实测它绿，理由是那条用例从不提交、字典空、换键也查不到）。**这一条是正向探针**：如果它在你这轮也红了，说明你的突变改多了或跑到别的门上，先停下登记，不要继续。

### T3｜W2：先做零构建推演，有配方才跑（没配方就登记"够不着"）

背景：全仓 46 格拆法原文只剩这一格欠着（W2＝`Every_downgrade_candidate_keeps_a_positive_input_budget`）。**盘上原文（守门人出闸前当场读回；第八波 §十二 与一页纸把调用方写成 `ModelProcessPolicy`，真文件＝`src/Pry.Api/Services/ModelPerformancePolicy.cs`）**：

- `src/Pry.Core/Prompting/ContextBuilder.cs:34` `var capped = Math.Min(Math.Max(requestedOutputTokens, 0), Math.Max(0, contextTokens / 2));`（＝M-CB1 那枚半窗夹的目标行）
- `:35` `var ceiling = contextTokens - DefaultSafetyTokens - MinimumInputTokens;`（＝M-CB6 那枚去底线的目标行）
- `:36` `if (ceiling < capped) capped = Math.Max(0, ceiling);`（第三道夹子）
- `:37` `outputTokens = Math.Max(1, capped);`（下限 1）
- `:38` `inputBudget = (long)contextTokens - outputTokens - DefaultSafetyTokens;`
- `:39` `return contextTokens > 0 && inputBudget >= MinimumInputTokens;`
- 调用方：`src/Pry.Api/Services/ModelPerformancePolicy.cs:46` 降档候选 `Math.Min(source.MaxOutputTokens, Math.Max(512, contextSize / 2))`；`:47` 调 `TryNormalizeWindow`；**:48 兜底 `return source with { ContextSize = contextSize, MaxOutputTokens = 1 };`**

实测过的两种配方都绿：批1 单放 M-CB1、批2 放 M-CB1∧M-CB6。根因（执行方读码＋守门人复核）＝两枚夹子同拿掉后归一化走到 `:39` 返 **false**（0 ≥ 512 不成立），调用方立刻落 `:48` 的 `MaxOutputTokens = 1` 兜底，绕回来输入预算反而还是正数——**`:39` 是这道门的设计意图（防御纵深），不是漏判**。

要你做两件事，顺序不许倒：

1. **读码列出口**（零构建）：把 `TryNormalizeWindow`／`WithContext`／相关 `Math.Max`/`Math.Min` 夹子**每一个 return 条件**逐条列出来，写成一张表，回答"存不存在一组输入，使得 `:39` 返 true（于是调用方不走兜底）**同时**候选项的输入预算非正"。允许用只读算式推演，不许猜数字。
2. 只有推出**具体可达配方**（给定模型窗口、输出预算、`MinimumInputTokens` 的实际取值组合）才起一次定向跑取红；推不出就把"够不着"连同第 1 步那张表一起登记。

**硬规矩**：结论不许写成"这一格不需要拆"；要写成"没找到可达配方，被排除的出口是 X／Y／Z"。够不着也是一种交付，但不是结案。

## 二 每一格必须交齐的十一项字段

① 载体提交号；② 被改文件:行；③ 突变原文 before→after；④ `git diff --stat` 数字；⑤ 跑法整行命令（含 `--results-directory` 绝对长参与 `--logger trx`）；⑥ TRX 文件名；⑦ 红格用例全名＋消息要点（含红点行号，锚点写法 `文件名.cs:line N`）；⑧ 还原方式；⑨ 还原前后 SHA-256（逐字节对平）；⑩ 还原后 `git status --short` 行数（应为 0）；⑪ 结论：这一枚证明了什么、没证明什么、和哪一格共用过凭证。

## 三 突变纪律（本仓库踩过的那些坑，逐条适用）

- 突变必须移动**被断言的那件事本身**；注释掉那一行不等于把它拿掉（按文本扫的门连注释一起算）。
- 多分支修法按分支逐个撤销、逐个取红；还原前 `grep -c` 数一遍改动点是否齐。
- 还原一律字节级（`git checkout --` 或 `Copy-Item`），**不许用 `Set-Content -Encoding UTF8` 还原**（PS5.1 会补 BOM，等于制造第二次改动）。
- 被测脚本以 `exit N` 收尾时每次调用独立子进程；派生门禁脚本后先读回输出目录那一行再起跑。
- `dotnet test` 只认长参 `--results-directory <绝对路径>` ＋ `--logger trx`；跑完先 `ls *.trx` 数到预期份数再读 `<Counters>`，**没有 TRX 就等于没跑过**，控制台再绿也不算。
- 一次只放一枚突变（T2 尤其）；批次红的归属要读消息形状＋红点行号，不许直接署成某一枚。

## 四 预算（硬顶，用完即停并如实报超支）

解决方案级构建 **1**；全量 **1**（收尾那一轮，四分项必须逐位对上在册：Client 108／App 233／Api 278／Core 527＝executed 525＋2 条按名跳过的环境项）；按名定向 **≤3**；机动 **1**（只给 T3 的真跑或 T1 的差异复跑）。单工程构建算定向的隐式代价、另记不另算额度，但要在 §五 逐次列出。**每一轮全量顶账要顶得上一格**：在册 `verify-repository` ＝本单出闸那把提交落地后当场实测的 **552**（线尾 `a2d9f96`；本单自己那份工单就是新落的那一格，上一格是 551）；它是"当前跟踪文件条数"，每落一份文档就往上走，不是门槛变化。判据条数不许变——本波若 `[Fact]` 条数有任何变化（不该有）必须在报告里写等式与原因。

## 五 焦点预检（这一波起口径变更，务必读）

**每一次 `dotnet` 之前**（含单工程构建、含 `--no-build` 的 test）先跑：

```
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Users\24782\Desktop\Pry_Project\.local-notes\game-preflight6-1022.ps1"
```

整段输出（含 `VERDICT=` 行）原样贴 §五，一次都不许省。**现行仪器是 v6，不是 v5**：v5 会漏判"带标题栏样式位＋进程路径读不出来"的真·全屏游戏（2026-10-22 21:13 实测漏判 `helldivers2`），v6 只往上加了两条停手条件，v5 的判据一条没删。v6 双向实测过：无边框整屏 ⇒ HOLD；带标题栏整屏且路径可读 ⇒ BUILD_OK；真·全屏游戏前台 ⇒ HOLD。已知残留风险：某个**提权／别的用户**的合法最大化窗口可能被 v6-A 误判 HOLD——遇到就照 HOLD 停刀并登记，不许自行绕行。
`VERDICT=HOLD` ⇒ 就地停刀回报，不等几分钟再试、不换跑法绕过、不改预检脚本判据。`free_memory_gb<6` 只算 WARN，不作废读数；只有 SQLite `disk I/O`／`out of memory` 那种形状才算读数作废。

## 六 禁止事项

- 突变要改"判据指向的那个名字／那一行本身"；不许新建含该名字的文件骗文本门，不许用注释代表删除。
- 不许为了让仪器好看而放宽／删除／注释任何既有断言；不许动 `.5` 检索地板、`2400`、五档超时、`MinimumUsableTokensPerSecond = 12`、`4096` 地板、`docs/resource-limit-ledger.md` 的数值。唯一合法反证＝载体内临时改一行并逐字节还原。
- 不碰作者的任何进程；不把 `%LOCALAPPDATA%/PryCompanion` 当测试载体（只读探查也不算许可）；**不 `git tag`、不 `push`、不打包**；不动主工作树任何分支指针与 `refs/heads/*`。
- 主工作树里他人名下的两枚暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）不属于本单：不读、不改、不提交、不 `stash`、不 `clean`。`licenses/**`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md` 一字不许碰。
- **状态词与结案词不由执行方写**：报告里写事实与读数，不写"已结案／已验收／基线已改写"。在册基线的改写权在守门人的登记刀。
- 交付物按批次**增量落盘**（冷启动 lane 有轮次上限；撞上限前先数盘上原始产物再写结论，绝不替自己补写署名）。

## 七 交付物

唯一允许你写进主工作树的东西＝新文件 `docs/eval/reports/2026-10-22-qa-wave10-three-evidence-debts.md`（报告 id 建议 `eval-QA-WAVE10-1022`）。写完即停，不许 `git add`、不许提交。

报告结构（顺序照此，§〇 先落盘）：§〇 挪树输出＋三处锚点原文读回＋格账起点 → §一 T1（十一项字段＋与守门人轮的差异登记）→ §二 T2（含 `:270` 保持绿的正向探针证据）→ §三 T3（出口表＋结论取向）→ §四 收尾那一轮四分项与顶账 → §五 预算终账（构建／全量／定向／单工程构建／**逐次预检整段原文，一次都不许只留结论**）→ §六 偏差自报（哪条纪律差点没守住、哪里与本单有出入）。
