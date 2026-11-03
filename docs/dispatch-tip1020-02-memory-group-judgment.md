# 工单 `QA-TIP1020-02`（第 2 层 · 记忆页"分两组"的有用性判据）

- **状态词**：已派工·待认领 ⇒ **2026-10-20 已认领·进行中**（Owner＝Memory & Context 界面半边，认领人＝`RT-005-MANAGE`／2-C 同一写者；依据＝作者「按照守门人的派工进行工作」＋本轮「继续工作」；判定归 QA 从可指认提交复采，结案词归作者）。**单写者声明**：本刀只动 `tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`，`src/Pry.App/PryMessengerMemoryWindow.cs` 全程只读（反证时临时突变、`cp`＋`cmp` 逐字节还原，提交前对 `src/` 的 `git diff` 必须零行），`MainWindow.axaml.cs` 一行不碰；采 §2 形状①（谓词无恒假合取＋`_orphaned.ItemsSource` 钉成 `orphans`）。
- **严重度**：Low，**不卡任何结案**——界面今天照常分两组，本轮实测七次全绿；这条是**判据欠账，不是缺陷**。
- **载体基线**：开工当日先重取线尾。在册读数＝**988／0／2（总 990）**，分项＝Client 101／App 233／Api 248／Core executed 406 passed 406＋2 条环境跳过（`eval-QA-TIP1020-1020`）。本单不改写它。
- **授权来源（逐字）**：作者 2026-10-19「**我不想思考了，你就继续给我派工单，该提交的提交吧，早点做完之后再回来大修**」；缺陷事实与修法口径出自 QA 2026-10-20 报告 `docs/eval/reports/2026-10-20-tip-e82499b-merged-tree-rolling-revalidation-and-four-knife-falsification.md` §5（含三条不许走的捷径原文）。
- **刀的体量**：**判据刀，零产品代码**，预计 1 个既有测试文件内 1–2 条用例。

## 1. 事实表（守门人 2026-10-20 当场重读码，行号当日有效）

| # | 事实 | 位置 |
|---|---|---|
| F1 | 分组靠这句谓词：`var orphans = details.Where(x => x.Status == "orphaned")`，随后 `_orphaned.ItemsSource = orphans.Select(OrphanItem).ToArray();` | `src/Pry.App/PryMessengerMemoryWindow.cs:98`、`:102` |
| F2 | 守它的**只有文本形状门**：`Assert.Contains("Status == \"orphaned\"", source, StringComparison.Ordinal)`，同文件其余断言同样全是指向源码字符串的 `Contains` | `tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs:24`（及 `:26`、`:27`、`:42`、`:44`、`:53`） |
| F3 | QA 实测：在 F1 的谓词后接恒假合取（`.Where(x => x.Status == "orphaned" && false)`）**而形状串原样留在源码里** ⇒ **988 条全绿、零红**，第二组恒空而没有任何门报警 | 同上报告 §5（突变编号 M17） |
| F4 | 该测试文件自带正向探针 `The_scanners_see_the_shapes_they_claim_to_see`，**只证明扫描器认得这个形状，不证明形状与行为等价**；形状门自己的 docstring 明写"注释里出现同样字样也算" | `tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs` |
| F5 | 这一族（掏空本体而扫描门仍绿）＝`QA-TIP1015-01`，10-15／10-16 两份报告已写过成因，本单是它**第一次复现到记忆管理这条 lane** | `docs/eval/reports/` 同两份 |

## 2. 形状（二选一，认领人自定，QA 报告原文口径）

- **① 形状＋绑定双断言**：谓词字面量里**不得含恒假合取**（`&& false`、`&& 0 == 1` 之类），并且 `_orphaned.ItemsSource` 的赋值来源必须钉成 `orphans`（分组结果真的流到那个控件）。
- **② 最小渲染断言**：给界面喂两行明细，`Status="orphaned"` 的行必须落进第二组、`active` 的行必须落进第一组（能按既有测试基础设施无头构造该窗口的话，这是更强的形状）。

## 3. 完成标准（出判面，逐条可判）

1. **先红一次（本单的承重墙）**：把 F1 谓词按 F3 那样掏空（或在①下把 `ItemsSource` 改接别的来源），新用例**必须**当场变红；逐字节还原后必须变绿，且对 `src/` 的 `git diff` 零行。**这一步没做到＝本单没做完**，因为本单存在的唯一理由就是"上一次那样拆没人响"。
2. **门禁面**（口径见 §5）：`-t:Rebuild` 0/0；解决方案级连续两次全绿、分项逐位相同；`measure-fixed-eval` 各 `exit=0`、catalog 仍 **76**；`verify-repository` 通过；`git diff --check` 干净。
3. **登记面**：`CHANGELOG.md` 不加用户可见条目（行为不变）；不铸新永久场景；§9 写清突变方向与红例名。

## 4. 反向约束（开工前逐条读）

1. **不许为了让判据绿去改产品行为**——今天分两组是对的；这条是判据欠账不是缺陷（QA 报告原文）。
2. **不许把"注释里出现字样也算"当覆盖证据**（F4：门自己的 docstring 已明写这一限制，引用它当"已守"是错的）。
3. **不许用"再加一条 `Assert.Contains`"当修法**——同形第二次出现就是这条欠账本身的形状。
4. **不许把那条正向探针当已修**（F4 后半句：它证明扫描器认得形状，不证明形状有用）。
5. 单一写者：只动 `tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`（或该 lane 名下的界面判据文件），`src/Pry.App/PryMessengerMemoryWindow.cs` **只读**；`MainWindow.axaml.cs` 一行不碰（`AGENTS.md` 第 3 条）。
6. 与 `QA-TIP1019-02` 同 lane 但**不同刀**：两套断言面（Core 检索 / App 界面）各自成提交，不许混提。
7. 文档与代码分刀；未 push、未 tag、未打包。

## 5. 读数口径

与 `docs/dispatch-tip1019-02-retrieval-floor-judgment.md` §4 同一段（长参 `--results-directory` 绝对路径、每程序集唯一 `LogFileName`、跳过按 `<UnitTestResult outcome="NotExecuted">` 点名、引用总数必写全四项分项、不做加法、绝不动他人进程与结果目录）。

## 6. 归位

落地后由守门人在合流树重跑门禁并登记（§4.2／§9 ＋ `docs/full-scope-sprint.md` §0），`main` 跟线尾；**QA 从当时线尾复采并亲手重拆 M17**——那一次是这一格的验收，本单结案词不由本角色与本 lane 写。
