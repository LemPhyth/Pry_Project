# Phase 1/2 门禁的两条地基刀复核（`DEF-P1WALK-03` · `DEF-P1WALK-04`）· 2026-09-29

- **Run ID**：`eval-QA-P1W0304-0929`
- **判定人**：QA / Evaluation（EVAL-001）
- **载体**：两条。①**主工作树，未提交**（HEAD 仍是 `1123be0`；`git status --short` 31 行里属于这两刀的是 26 份代码文件——`src/Pry.Api/Services/SseEventStream.cs` + 22 份 `tests/**` 改动 + 3 份新文件，其余 5 行是他人名下的 `CHANGELOG.md`／`docs/README.md`／`docs/engineering-pitfalls.md` 与两份未跟踪文档）。②**我自己从 `1123be0` 拉的 `--detach` worktree**，只往上叠这 26 份代码文件，不带任何文档行——本轮所有关键证伪都改在②上做，主工作树的产品文件一个字节都没被我写过（`SseEventStream.cs` 三次采样 sha1 恒为 `9649d610b55e6b137cd0dd7860593fea9f5a062c`）。
- **结论先写在最前面**：**两条卡点都是真修好了，而且是我自己逐条证伪过的，不是引用 Owner 的数字**。但是**没有可指认提交 ⇒ §8.1 段② 仍然不能签**，状态词停在 `已落地主工作区未提交`，§5.0 指针不动。本轮新登记两条 Low：`QA-P1W0304-01`（一条判据覆盖空档，附我能构造出的反证探针）与 `QA-P1W0304-02`（文档里一处突变读数按仓库现状对不上）。
- **与上一轮的关系**：上一轮（`eval-QA-P3STATE-0928`）我签不下去的原因是"没有提交载体"。这一轮载体条件没变，变的是**证据强度**：上轮的 13 处突变反证我只能读登记、不能引用（§8.2），本轮 -03/-04 的每一条红/绿都是我当场跑出来的。

---

## 1. 先说人话（作者只要这一段就够了）

这两刀修的不是"她今天能感觉到什么"，而是**"门禁的绿色读数能不能信"**。

- `-03`：后端事件流在客户端断开的那一刻，会先抛一个跟真实原因毫无关系的框架异常（`NotSupportedException`），把"用户断线了"这条真信息顶掉。后果是日志里一次正常断连和一次真故障长得一模一样，下次排查会被它带偏。现在改成：先把那笔还在飞的读取结算掉，再释放，取消原因原样出场。
- `-04`：21 个测试文件收尾时用了一个**进程级**的清理调用，它会顺手把**别的测试类正在用的**数据库连接句柄释放掉。表现是随机有别人的用例红，而且红堆栈里一行都不指向肇事者。这就是"并行连续 ≥2 次全绿"永远差一口气的根因之一。现在改成每个 fixture 只清自己那一条库的池，另加一条源码门：谁再写进程级清池，扫 `tests/**` 直接点名文件与行号。

**两刀对用户可见行为的影响：零。** 它们换来的是"753 全绿是可信的"。

## 2. 当场重采的读数（全部本轮重跑，不引用任何历史数字）

| 跑法 | 载体 | 范围 | 结果 | 退出码 |
|---|---|---|---|---|
| 全量 ×7 | 主工作树（混树） | 解决方案级 | **753 通过 / 0 失败 / 2 跳过（总 755）**，七次逐位相同（Client 92、App 183、Api 140、Core 338+2skip） | `dotnet test` 0 ×7、`measure-fixed-eval` 0 ×7 |
| 全量 ×2 | `1123be0` 干净 worktree + 仅这 26 份代码 | 解决方案级 | **753 / 0 / 2**，两次逐位相同 | test 0 ×2、`measure` 0 ×2 |
| 定向 | 干净 worktree | `EventStreamHeartbeatTests` + `ProcessWidePoolResetBoundaryTests` | **17 / 0** | 0 |
| 构建 | 两棵树各一次非增量 Release | 全解决方案 | **0 警告 / 0 错误** | 0 |
| 仓库扫描 | 主工作树 | `verify-repository` | **411 份跟踪文件通过** | 0 |
| 空白检查 | 主工作树 | `git diff --check` | 干净 | 0 |

加法关系我独立核对成立：Owner 登记 -03 加 3 条、-04 加 2 条，`748 + 3 + 2 = 753`，与两份 TRX 的分程序集读数一致；两条跳过是环境门（`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`、`CoreTests.Configured_sense_voice_model_can_run_offline`），与历轮同名。

两点必须如实标注（§5.0 判据 4「读数不得互顶」）：

1. **那 7 次是在混树上跑的**，同一工作树里还有 Intelligence 那几刀与 Owner 名下的文档行。它只证明"当前工作树不破门禁"，**不构成任何一刀的段②**。
2. **干净 worktree 那 2 次接近段② 但不等于段②**：worktree 是临时对象，不是可指认提交；文档行（CHANGELOG／陷阱第 27、28 条／台账）没有跟进去。契约要的载体是一个 SHA，QA 从那个 SHA 建 worktree 复采。

## 3. `-03`：我自己证伪的三件事（全部在干净载体上）

先把话说明白：本节每一个红/绿都是我当场改代码、当场跑、当场逐字节还原后重采的。Owner 名下的 M7/"取消心跳"/"给 `MoveNextAsync` 传取消"等形状**我没有重跑**，按契约 §8.2 一律不引用为 QA 证据。

1. **原始缺陷复现（最强的那一条）**：把 `src/Pry.Api/Services/SseEventStream.cs` 还原成 `1123be0` 自己那份（也就是修之前的形状），其余文件保持新判据 ⇒ **15 条里恰好 1 红**，`NotSupportedException`，栈顶落在 `FramesAsync` 释放内层游标那一步，红的正是新增的 `Abandoning_the_stream_after_one_heartbeat_frame_releases_the_in_flight_cursor`。⇒ 这条用例不是"碰运气绿"，它真的钉住了缺陷本身。
2. **判据 ① 的两半各有独立抓手**：`μ1` 只删"结算那一步"（`_ = await Task.WhenAny(pending)`）保留释放 ⇒ 1 红＝`Abandoning_…`；`μ2` 只删"释放那一步"（`await cursor.DisposeAsync()`）保留结算 ⇒ 1 红＝`Settling_the_advance_before_release_still_releases_the_cursor`，`Abandoning_…` 在 `μ2` 下是绿的。⇒ 与陷阱条目 ⑥ 自己写的"两条各守判据 ① 的一半、只删 Dispose 的假修法在第一条上不会红"逐字对上了——这句我本来只能当声明读，现在是测量。
3. **还原后 15/15 绿**，`SseEventStream.cs` sha1 回到 `9649d610…`（与主工作树那份逐位相同）。

### 3.1 本轮唯一的负面发现：有一条省事修法三条判据都抓不到（`QA-P1W0304-01`）

`μ3`＝把那行"结算但不取结果"换成 `await pending;`（也就是**在 `finally` 里观察那笔任务的结果**）。⇒ **15 条全绿**。

为什么三条新判据都放行它：断开那条用例里，内层认取消、那笔推进在 `finally` 跑到之前就已经落地，`inFlight` 槽位此时是 null，整个结算分支被跳过；放弃那条用例里推进不会失败，`await pending` 与 `WhenAny` 等价。⇒ 这条改动的**唯一**可观测差异出现在"那笔在飞的推进是以异常收场"时： shipped 形状把释放走完、`DisposeAsync` 干净返回；`μ3` 把内层异常从 `DisposeAsync` 里抛给释放方，并跳过 `cursor.DisposeAsync()`。而这恰好是本缺陷原本抱怨的那类事——释放路径顶替真正的离开原因。

我顺手确认这条空档**可测**（探针只在 worktree 里存在，已随 worktree 删掉，不进仓库）：内层游标交出心跳后挂在一次"会被置为失败"的等待上，测试先 `SetException` 再释放流，断言 `DisposeAsync` 不抛 ⇒ **`μ3` 下红**（`Assert.Null() Failure / Actual: System.InvalidOperationException: 模拟内层游标在飞行中失败`）、**shipped 形状下 16/16 绿**。给 Owner 的成文建议：把它固化成第四条判据（"在飞的推进失败也必须把游标释放掉"），shipped 代码一行都不用改。

一条口径提醒，免得探针被写成更弱的那一半：同一个用例里**"记账 `trace` 是否为 `released`"不区分两种形状**——内层主体自己抛出去时，它的 `finally` 就已经跑过了，两种形状都会记到 `released`。能区分的只有"`DisposeAsync` 本身干不干净"这一个断言。

边界说清楚：**这不是产品缺陷**，当前 `FramesAsync` 的形状是对的；登记的是一条**判据覆盖空档**，Low，不卡任何结案。

## 4. `-04`：我自己证伪的三件事

1. **两份 helper 里每一个 `ClearPool` 都是承重的，不是装饰**。在干净载体上单独跑 `Pry.Core.Tests`（340 条）：
   - 基线（两份文件原样）⇒ **338 / 0 / 2**；
   - `μA` 删掉裸 `Data Source=` 那一档 ⇒ **9 红**（`ConversationReadTests`、`CoreTests`、`InjectedLoggingTests`、`ProactiveDeliveryTests`、`ProactivePolicyTests`、`ProtocolEventJournalTests` 六个类，其中 journal 一族占 4 条），错误全部是同一条 `System.IO.IOException : The process cannot access the file 'memory.db' because it is being used by another process`，抛出点全部在 fixture 的 `Dispose`；我跑了两遍，红的用例名集合逐位相同 ⇒ 确定性，不是竞态；
   - `μB` 删掉 `MemoryDatabase` 那条 builder 串 ⇒ **34 红**，同一异常族，覆盖面扩到六个类的几乎全部用例；
   - 逐字节还原 ⇒ 回到 **338 / 0 / 2**。
   ⇒ 结论：Core 版清两个键、Api 版清一个键，这个不对称是**被测出来的**，不是注释里说的。裸键那一档只对应用例里直连库做突变的那些点（`ConversationReadTests.cs:82,91`、`CoreTests.cs:136`、`ProtocolEventJournalTests.cs:21,50,266,300` 等，全部在 `Pry.Core.Tests`；`Pry.Api.Tests` 内我 grep 到零处裸键，故 Api 版少一档是对的）。
2. **源码门会点名**：向 `CoreTests.cs` 原样还原一处真进程级清池 ⇒ 门的 `No_test_source_resets_the_process_wide_sqlite_pool` 转红并给出文件与行号；删掉注入 ⇒ 转绿（2/2）。正向探针（判据常量与探针异源拼装、改坏常量时探针必须变红）在混树上跑过，读数 `Expected: "SqliteConnection.ClearAllPools()" / Actual: "…ClearAllPoolsQ()"`，如实标注载体。**替换是否逐点到位**我用完整调用式另数一遍：`src/` 与 `tests/` 的 `.cs` 源码里 `SqliteConnection.ClearAllPools()` 现在是 **0 处**（`bin`/`obj` 已排除），28 处旧调用点全部换成 `SqliteTestPools.Clear(...)`。同一份筛法换成子串 `ClearAllPools` 则是 **6 份文件 / 9 行**，逐行核过全是三类合法命中：解释"为什么不用它"的注释与文档引用 7 行（`CharacterLearningDeletionTests.cs:46`、`LoopbackBindingTests.cs:62`、`SqliteIntelligenceRepositoryConformanceTests.cs:57`、两份 `SqlitePoolCleanup.cs:7`、门自己的 `:7` 与 `:71`）、门内嵌的正向探针字符串 1 行（`:68`，内容是注释文本、不含 `SqliteConnection.` 前缀，所以不会被自己的判据抓到）、门的常量 1 行（`:17`，`"SqliteConnection." + "ClearAllPools" + "()"` 拼接，否则门会抓到它自己）。⇒ 这条门按**调用形状**判定是站得住的，上一轮我按子串数到"22 个文件"的那次误判，错因在这里被结构性地关掉了。
3. **残留仪器（发布行为的独立证据）**：干净载体连跑两次全量之后，`%TEMP%` 下 `pry-*` 目录从 271 → **264**（净减 7，零净增）。这一条用的是陷阱第 28 条自己指定的口径——`LoopbackBindingTests.Delete` 吞 `IOException` 是有意的，所以这一族**不能用红绿读数**，只能用目录计数。混树那 7 轮同样零净增。

### 4.1 `QA-P1W0304-02`（Low，文档准确性）

陷阱第 28 条 ⑥ 登记的 F3 写的是"把 Core helper 的连接串指去不存在的池键 ⇒ Core 五个 fixture 类 **26/33 红**"。我按最近可复现的两种形状量到的是 **9 红**（只删裸键档）与 **34 红**（只删 builder 档），异常文本与该行登记的一致（`IOException … memory.db`），但**分母 33 在仓库里没有对应物**（`Pry.Core.Tests` 是 340 条、六个类做清池），"五个 fixture 类"也与实测的六个不符。这不影响 -04 的判定成立——我自己那两条突变已经足够——但它是一条被当作证据引用的读数，别人复现不出来就会以为 helper 无效。修法只要一句：把 F3 的突变写成"删哪一档 / 保留哪一档"，并给出红的确切数量与用例名来源。Owner 名下文档，我不动。

同条 ⑥ 还有一句按今日现状对不上的计数："按子串 `ClearAllPools` 数会比真实调用点**多两处**"。今天的实测是**多九行 / 六份文件**（见上节逐行归类），因为门落地之后它自己的常量、探针字符串和注释都进了命中面。这句在"28 处调用还在"的时点是准确的，作为**当时的**读数不必改写；但它现在被当成筛法说明挂在防复发条目上，后来人按"两处"去核会对不上。建议补一句"本门落地后子串命中面扩大，以调用形状为准"。

## 5. 发行行为：这两刀动了什么、没动什么

| 事实 | 本轮当场证据 |
|---|---|
| `-03` 的生产消费点仍然只有一处 | 源码面 `grep -rn GetAsyncEnumerator src/ --include=*.cs` 只命中 `SseEventStream.cs`（其余命中全是 `bin`/`obj` 里的 dll，筛掉后一行），与陷阱第 27 条 ⑤ 的排查面一致，本刀没让它变宽 |
| `-04` 零产品代码改动 | 两刀的 `src/` 面只有 `SseEventStream.cs` 一份；其余 25 份全在 `tests/**` |
| 学习状态的**写入**仍然没有生产者 | 上一轮已核（`b66efc8` 接的是构造点与 purge），本刀没碰任何读路径；真机关掉再打开，心情/关系/口癖仍然归零 |
| `IReplyAdvisor` 仍走过渡实现 | `ConversationSessionService.cs` 仍注入 `TransitionalReplyAdvisor`，本刀未动 |

⇒ 谁把这两刀读成"她终于有记忆了"或"断连终于不难看了"，都是错的读法：**发行行为在这两刀上为零**，收益全在门禁可信度。

## 6. 段② 判定与下一步（动作在作者）

- **§8.1 段②：不签。不给任何阶段状态词。** 本轮状态词只到 `已落地主工作区未提交`（载体＝未提交的主工作树 + 我临时拉的 worktree，都不是可指认提交）。
- **Phase 1「并行连续 ≥2 次全绿」**：`-03`、`-04` 两条竞争者至此**都已修完且都被我独立证伪**，样本也在（混树 7 次 + 干净载体 2 次，全绿零红例，`measure` 九次 exit=0）。按纪律我不写成"成立"：判据要的是**一次可指认提交之后**的复采，而不是工作树上的读数。
- **建议的提交切法**（Owner 台账自己也这么分）：一刀 `-03`（`src/Pry.Api/Services/SseEventStream.cs` + `EventStreamHeartbeatTests.cs`）、一刀 `-04`（22 份测试 + 3 份新文件），文档行随各自那刀走。两刀文件面不相交，可以分两次点头。
- **我不做的事**：不 stage、不 commit、不 push、不挪 §5.0 指针、不动 Owner 名下的 `CHANGELOG.md`／`docs/engineering-pitfalls.md`／`PROJECT_PLAN.md` §4.2 台账行。作者点头后 QA 从新 SHA 建 `--detach` worktree 复采 ≥2 次，段② 才谈得上签。
- **`QA-P1W0304-01` 的落点**：建议随 -03 那一刀一起补第四条判据（探针形状我已在 §3.1 给出），补完不需要重新签段②；Owner 选择先提交也成立，因为 shipped 形状本身是对的，空档只在"下一次有人省事"时才会咬人。

## 7. QA 自己的边界与仪器自纠

- 本轮**零产品代码、零产品测试改动**：所有突变都落在 `.local-notes` 下的临时 worktree，探针文件随 worktree 一起销毁；主工作树 31 行状态与入场时逐行相同，`SseEventStream.cs` sha1 未变。
- worktree 删除时 `git worktree remove` 报 `Permission denied`（Windows 句柄未放），注册已由 `git worktree prune` 清干净，磁盘上剩一个不可见的空壳目录，不属仓库内容。
- 上一轮那条教训本轮又踩到一次并当场纠正：TRX 文件名里带 `[1]`，PowerShell 的 `Get-Content -Raw` 会把它当通配符 ⇒ 前几轮我一度把四份 TRX 读成两份；改用 `-LiteralPath` 后七次读数才逐位一致。**错的是我的仪器，不是代码**——这条与陷阱第 27 条 ③ 的"判据不许依赖赢比赛"是同一族：读数错了比读数少了更危险。
- `docs/decision-request-protocol.md` 仍未跟踪（`QA-P3STATE-02` 未结），本轮不新增重复登记。
