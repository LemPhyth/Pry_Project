# 派工单·第十一波（给"只松输入留底"那枚空档造一枚会响的门：两格新判据 ＋ M-CB6 的第一次单枚清洁红 ＋ W2 的兄弟格）

派发人：守门人（Architecture / Integration / Release）。认领人：冷启动 lane（本报告之外不得假设任何上下文）。
出闸时间：2026-10-23 01:0x。坐标系：`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS 表＋§9 交接；本单是**判据刀**（只加测试，零产品代码、零门槛数值）。开工必读：`docs/plans/README.md`、`docs/plans/PROJECT_PLAN.md`、`docs/agent-engineering-standards.md`、根目录 `AGENTS.md`。

## 〇 起点读数与挪树（先落盘，再碰任何代码）

1. 主树只做只读测量：`git -C C:/Users/24782/Desktop/Pry_Project status --short --branch`、`git log --oneline -3`、`git rev-parse HEAD`。**主工作树里你一个字都不许写**（唯一例外见 §六 交付物）。
2. 独立载体：`git -C C:/Users/24782/Desktop/Pry_Project worktree add --detach C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave11-1023/carrier 79b541e`。进载体后 `git status --short` 必须＝0 行、`git rev-parse HEAD` 必须等于 `79b541e`。把这两行原样贴进报告 §〇。**起点成立的判据是代码面**：`git -C <主树> diff 79b541e..<主树当前线尾> -- src tests` 必须为空（守门人出闸时实测＝空，`src`/`tests` 自 `4039a50` 之后未再动）。
3. 锚点原文（守门人 2026-10-23 01:03 当场 `cat -n` 读回，行号以 `79b541e` 为准；**你还要在 §〇 自己再读回一遍**）：

   | 位置 | 盘上原文 |
   |---|---|
   | `src/Pry.Core/Prompting/ContextBuilder.cs:34` | `        var capped = Math.Min(Math.Max(requestedOutputTokens, 0), Math.Max(0, contextTokens / 2));` |
   | `src/Pry.Core/Prompting/ContextBuilder.cs:35` | `        var ceiling = contextTokens - DefaultSafetyTokens - MinimumInputTokens;` |
   | `:36`／`:37`／`:38`／`:39` | `if (ceiling < capped) capped = Math.Max(0, ceiling);`／`outputTokens = Math.Max(1, capped);`／`inputBudget = (long)contextTokens - outputTokens - DefaultSafetyTokens;`／`return contextTokens > 0 && inputBudget >= MinimumInputTokens;` |
   | `src/Pry.Api/Services/ModelPerformancePolicy.cs:44～:46` | `var intended = contextSize == source.ContextSize`／`    ? source.MaxOutputTokens`／`    : Math.Min(source.MaxOutputTokens, Math.Max(512, contextSize / 2));` |
   | `:47`／`:48` | `if (!ContextBudget.TryNormalizeWindow(contextSize, intended, out var ctx, out var output, out _))`／`    return source with { ContextSize = contextSize, MaxOutputTokens = 1 };` |
   | 现有测试文件 | `tests/Pry.Api.Tests/ContextWindowLoadTests.cs`（现 59 行，三格：W1 `:19`、W2 `:33`、W3 `:50`）；常量在 `ContextBuilder.cs:16`（`DefaultSafetyTokens = 128`）与 `:17`（`MinimumInputTokens = 512`） |

4. **本单要补的那格空档（出闸前我读码实测到的现状，你要在 §〇 复述并逐条自证）**：第八波 §二 登记的 M-CB6（`ContextBuilder.cs:35` 去掉 `- MinimumInputTokens`）在**全仓单独放**时**一格都不红**（原文＝"单独不响任何格；与 M-CB1 合力才响 W2"），而第十波 T3 的 12 出口表证的是"在 `CreateCandidates` 现有那把降档梯子（最小 4096）的输入形状下，找不到能让 `:39` 返 true 同时候选输入预算非正的配方"。两件事合起来＝**"为输入留底"那一半今天没有任何一枚单点突变能打红任何格**。

## 一 三件活（顺序不许倒：先落绿、再取红、再还原复绿）

### T1｜新落两格 `[Fact]`（只加，不改不删任何既有断言）

落点＝`tests/Pry.Api.Tests/ContextWindowLoadTests.cs` 追加在 W3 之后，命名照该文件既有风格（动宾短语、说清守的是哪件事）。两格的**夹具与预期值**（守门人零构建推演，**不是已验证事实——你必须用 T2/T3 的实测红去证它**）：

- **N1｜4096 窗口那一格**：`new ModelProfile { Id = "m", DisplayName = "M", ContextSize = 4096, MaxOutputTokens = 4096 }`，`CreateCandidates(profile)`（`device` 省略＝null ⇒ `Classify` 走 `Cpu` 档 ⇒ `MaximumInitialContextSize = 4096` ⇒ `firstContext = min(4096, 4096) = 4096`，梯子那六个值全 ≥ 4096 ⇒ **候选恰好 1 枚**，请用 `Assert.Single` 把这个形状钉住）。干净代码下的推演：`intended = source.MaxOutputTokens = 4096`（因为 `contextSize == source.ContextSize`）→ `:34` 夹到 2048 → `:35` 天花板 3456 不小于 2048 → `output = 2048`、`input = 4096 − 2048 − 128 = 1920` → `:39` 真 ⇒ 候选 `(4096, 2048)`。要断言的三件事：`ContextSize == 4096`、`MaxOutputTokens == 2048`（＝半窗口那道夹**算出来的确切值**，不是 `≤`）、`ContextSize − MaxOutputTokens − DefaultSafetyTokens == 1920`。
- **N2｜1024 窗口那一格**：夹具 `ContextSize = 1024, MaxOutputTokens = 1024`，同样 `device` 省略（`min(1024, 4096) = 1024`，梯子全大于 ⇒ **恰好 1 枚**）。干净代码推演：`intended = 1024` → `:34` 夹到 512 → `:35` 天花板 `1024 − 128 − 512 = 384` 小于 512 → `:36` 削成 384 → `output = 384`、`input = 1024 − 384 − 128 = 512`（恰好等于地板）→ `:39` 真 ⇒ 候选 `(1024, 384)`。断言：`ContextSize == 1024`、`MaxOutputTokens == 384`、`input == 512`。
- 两格都要带**失败信息**（`Assert.Equal(expected, actual)` 之外，把窗口号写进消息或注释，让红格名字自己说明是哪把尺）。**不许**用"输出保留不等于 1"这种去认兜底的写法——那会把兜底本身钉成判据；要钉就钉确切换算值。
- 落完之后跑**一次定向**（干净态）：两格必须**绿**，同文件 W1／W2／W3 也必须绿。这一轮是"夹具形状与我推演一致"的凭证；若 N1 或 N2 在干净态就红，**先停下登记**（说明我的算术错了或候选数不是 1），不要改断言去凑绿。

### T2｜M-CB6 的单枚清洁红（本波真正要拿的那一格）

只放一枚：`ContextBuilder.cs:35` 的 `- MinimumInputTokens` 去掉 ⇒ `        var ceiling = contextTokens - DefaultSafetyTokens;`。其余一字不动。跑同一把定向过滤。

**预期（我的推演，实测为准）**：红格**恰好 1 条＝N2**，消息 `Expected: 384 / Actual: 1`（1024 窗口下 `input` 变成 384 → `:39` 假 → 走 `:48` 兜底 `MaxOutputTokens = 1`）。N1 必须**绿**（4096 窗口：`capped = 2048 ≤ ceiling 3968`，完全不受这枚影响）。W1／W2／W3 必须**绿**。

- 这一枚是**全仓第一次**拿到"只松输入留底"的单点清洁红——请把它当成新事实写进结论：此前它的登记是"单独不响任何格"。
- 拿不到这个红集（多响、不响、响在别的行）就**如实登记差异**，不要调断言、不要加第二枚去凑。

### T3｜双枚 M-CB1∧M-CB6 ＋ 收尾那一轮

还原 `:35`（逐字节，见 §三）后，两枚同放：`:34` → `        var capped = Math.Max(requestedOutputTokens, 0);`；`:35` → 去掉 `- MinimumInputTokens`。跑同一把定向过滤。

**预期**：N1 红（`(4096, 1)`，`Expected: 2048 / Actual: 1`）＋ N2 红（`(1024, 1)`，`Expected: 384 / Actual: 1`）＝**恰好 2 红**；W1／W3 绿；**W2 必须仍然绿**（`input = 4096−1−128 = 3967`、`1024−1−128 = 895`，兜底把它反手变成正数——这就是第十波 12 出口表那条恒等式的现场复证）。

然后：两枚逐字节还原（`git checkout --`）＋ SHA-256 对平 ＋ `git status --short` 只应剩你自己的测试改动那一行 ⇒ **一次解决方案级构建**（`dotnet build Pry.slnx -c Release`）＋ **一次全量**（`dotnet test Pry.slnx -c Release --no-build --results-directory "<绝对路径>" --logger trx`，跑完先 `ls *.trx` 数到四份再读 `<Counters>`）。预期总 **1148＝1146 绿＋2 条按名点出的环境跳过（照旧同名）＋0 红**，分项＝Client 108／App 233／**Api 280**／Core 527（executed 525）。**必须在报告里写出等式**：1146 ＋ 本波新增 2 ＝ 1148；若分项对不上（比如 App 数进 236）先按"仅工作副本存在的方法名"点名归属并明写**不并账**，不要用你的读数改写在册基线。

### 四 机动格（额度有才做，没有就登记"未做"）

单放 M-CB1 一枚（只 `:34`）⇒ 预期**只有 N1 红**（`Expected: 2048 / Actual: 3456`），N2 绿（这枚在 1024 窗口下与干净态同值，属" specificity 的另一半"）。这一向不做也不影响本波的结论；做了就把十一项字段补齐。

## 二 每一格必须交齐的十一项字段

① 载体提交号；② 被改文件:行；③ 突变原文 before→after（逐字，取自当场 `git diff`）；④ `git diff --stat` 数字；⑤ 跑法整行命令（含 `--results-directory` 绝对长参与 `--logger trx`，并写明**有没有**加 `--no-build`）；⑥ TRX 文件名；⑦ 红格用例全名＋消息要点（含红点行号，锚点写法 `文件名.cs:line N`，照抄 TRX 里那一行）；⑧ 还原方式；⑨ 还原前后 SHA-256（逐字节对平）；⑩ 还原后 `git status --short` 行数；⑪ 结论：这一枚证明了什么、没证明什么、和哪一格共用过凭证。

## 三 突变与还原纪律（本仓库踩过的那些坑，逐条适用）

- **突变轮绝不许 `--no-build`**：改的是 `src/`，不加重建就是拿旧二进制跑，读数全绿而假（前案：HDS-002 那一把的假门禁）。定向跑用隐式重建即可（单工程构建算定向的代价，另记不另算额度，但要在 §五 逐次列出）。
- 一次只放一枚（T2 尤其）；批次红的归属要读消息形状＋红点行号，不许直接署成某一枚。
- 还原一律字节级（`git checkout -- src/Pry.Core/Prompting/ContextBuilder.cs`），**不许用 `Set-Content -Encoding UTF8`**（PS5.1 补 BOM＝制造第二次改动）。事后 `git show HEAD:<file> | sha256sum` 与工作树 `sha256sum` 对平；`md5sum` 拿工作树与 HEAD 直接比是无效的（eol 归一化本来就让它不等）。
- 突变必须移动**被断言的那件事本身**；注释掉那一行不等于把它拿掉。
- `dotnet test` 只认长参 `--results-directory <绝对路径>` ＋ `--logger trx`；**没有 TRX 就等于没跑过**，控制台再绿也不算。
- 每批一个新证据目录（建议 `run1-clean`、`run2-mcb6`、`run3-pair`、`run4-mcb1`、`run5-full`），绝不复用目录（复用会覆盖上一轮证据）。

## 四 预算（硬顶，用完即停并如实报超支）

解决方案级构建 **1**（收尾那一轮）；全量 **1**（同一轮）；按名定向 **≤3**（T1 干净、T2、T3）；机动 **1**（只给 §四 那一向）。单工程构建（定向跑的隐式重建）另记不另算额度，但要在 §五 逐次列出。**顶账**：在册 `verify-repository` 现为 **553**（受跟踪文件条数，每落一份跟踪文档就往上走一格，不是门槛变化）；判据条数等式必须写（本波预期＋2：`grep -c "\[Fact\]" tests/Pry.Api.Tests/ContextWindowLoadTests.cs` 从 3 → 5，落码前后各数一次贴进报告）。

## 五 焦点预检（每一次 `dotnet` 之前，一次都不许省）

```
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Users\24782\Desktop\Pry_Project\.local-notes\game-preflight6-1022.ps1"
```

整段输出（含 `VERDICT=` 行）原样贴进报告 §五，不许只留结论。现行仪器是 **v6**（不是 v5：v5 会漏判"带标题栏样式位＋进程路径读不出"的真·全屏游戏）。`VERDICT=HOLD` ⇒ 就地停刀回报，不等几分钟再试、不换跑法绕过、不改预检脚本判据。守门人 2026-10-23 01:03 出闸前实测＝`BUILD_OK`（前台是普通窗口）。`free_memory_gb<6` 只算 WARN，不作废读数；只有 SQLite `disk I/O`／`out of memory` 那种形状才算读数作废。

## 六 交付物与禁止事项

**允许你写进主工作树的东西只有一件**：新文件 `docs/eval/reports/2026-10-23-qa-wave11-input-floor-reachable-cells.md`（报告 id 建议 `eval-QA-WAVE11-1023`）。

**测试改动怎么交回**：在**载体**里把 T1 那两格新测试落成一个提交——先 `git -C <载体> switch -c codex/wave11-20261023`，只 `git add tests/Pry.Api.Tests/ContextWindowLoadTests.cs`，提交信息 `test(api): 给"半窗口夹"与"输入留底"各钉一枚会响的确切换算格`；**不许提交突变**（提交前 `git diff` 里必须看不到 `src/` 的任何改动），不许 `git tag`、不许 `push`、不动 `refs/heads/main`。报告里写清这一把的分支名与提交号。

禁止事项（照旧，逐条适用）：
- 主工作树里除那一份新报告外**零写入**；他人名下两枚暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）不读、不改、不提交、不 `stash`、不 `clean`。
- 不许放宽／删除／注释**任何**既有断言；尤其**不许动 W2 那一格**（不许删它、不许把它的 `input > 0` 改成 `>= 512`、不许"顺手"重写它）。本波是**加**兄弟格，不是替它销账。
- 不许动任何门槛数值：`DefaultSafetyTokens = 128`、`MinimumInputTokens = 512`、检索地板 `.5`、`2400`、五档超时、`MinimumUsableTokensPerSecond = 12`、`4096` 地板、`docs/resource-limit-ledger.md` 的数值。唯一合法反证＝载体内临时改一行并逐字节还原。
- 不碰作者任何进程；不把 `%LOCALAPPDATA%/PryCompanion` 当测试载体（只读探查也不算许可）；`licenses/**`、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md` 一字不碰。
- **状态词与结案词不由执行方写**：报告写事实与读数，不写"已结案／已验收／基线已改写／W2 已补齐"。
- 交付物按批次**增量落盘**（冷启动 lane 有轮次上限；撞上限前先数盘上原始产物再写结论，绝不替自己补写署名）。

## 七 报告结构（顺序照此）

§〇 挪树输出＋锚点原文读回＋"起点成立"的代码面判据 → §一 T1（夹具形状实测：候选数、干净态绿）→ §二 T2（十一项字段＋"这是 M-CB6 第一次单枚红"的结论取向）→ §三 T3（双枚红集＝2、W2 仍绿的负向探针、收尾四分项与等式）→ §四 机动格（做/未做）→ §五 预算终账（构建／全量／定向／逐次单工程重建／**逐次预检整段原文**）→ §六 偏差自报（哪条纪律差点没守住、哪里与本单有出入、我的推演被实测证否了哪一条）。

**结论取向硬要求**：不许写"W2 已补齐"。允许写的是——"这一枚（M-CB6）今天有了会响的单点格 N2；W2 那一格本身仍然没有拿到实测红，且本波复证了它为什么红不了（兜底反手把输入变成正数，见第十波 12 出口表）"。
