# RT-002 final verification（A–D，仅验证）— `eval-QA-RT002FINAL-0924`

- **角色**：QA / Evaluation Engineer（Qoder）。**日期**：2026-09-24。
- **被验对象**：作者派的四条——**A.** `NotAfter` 重命名后语义一致；**B.** malformed `ResponsePlan` sequence 会被拒绝；**C.** commit 成功、event publish 失败后「DB message 保留 / reload 能恢复 / 不产生重复 message」；**D.** 现有 RT-002 test suite 全绿。
- **载体**：`44e52fc` 的一次性 `--detach` worktree（`.local-notes/p2-0924-close`，跑前 `git status` 为空）。Release 构建 **0 警告 0 错误**。
- **边界（作者三条令全部守住）**：**零产品代码改动、零产品测试改动、未新增任何 feature**；两条探针只写进该 scratch worktree，跑完即 `rm` 并复核 `git status` 为空（探针不进主工作区、不进门禁）。非当前范围的发现**只登记 backlog**（§5），不作为 A–D 的阻塞。
- **诚实声明**：本轮不引用他轮数字充当自己的证据；下列每条数字都是本轮在同一载体上跑出来的。

## 0. 判定表

| 条 | 作者判据 | 判定 | 一句话证据 |
| --- | --- | --- | --- |
| A | `NotAfter` 重命名后语义一致 | **Pass（带一条限制）** | `src/`+`tests/`+`docs/` 内该字段 6 个使用点全部是"最早可投递时刻"、旧名零残留；限制：测试面**零条**直接断言该字段。 |
| B | malformed `ResponsePlan` sequence 会被拒绝 | **Pass（对 1.0 生产者）／不成立（对字面口径）** | 真实 SQLite：重复 `[0,0]` 与乱序 `[1,0]` **被拒**（前缀门 fail-closed）；跳号 `[0,2]` 与负号 `[-1,0]` **各投递 2 条并 close 计划**，不被拒 ⇒ 转 backlog `DEF-P2PLAN-01`。 |
| C | commit 成功 + publish 失败：留库 / 可恢复 / 不重复 | **Pass（三项全给，两处如实限制）** | 抛出型 sink 探针：`messages` 保留 1 行 `one`、`planned_messages` = `0/delivered , 1/cancelled`、`DeliveryReport=planned2/delivered1/abandoned1/delivery_failed`、重启清扫 0 条 open 计划、换库重建调度器后总数仍 1 ⇒ 不可能重发。 |
| D | 现有 RT-002 suite 全绿 | **Pass** | RT-002 范围 **131 通过 / 0 失败 / 0 跳过**（Core 101、Api 26、App 3、Client 1）；同载体解决方案级见 §4。 |

**⇒ A–D 四条没有一条把 RT-002 判停在"不能验收"上。** B 的两格不被拒是真发现，但它今天的生产者产不出那种形状（§2.3），按作者的"非当前范围只登记 backlog"处理；它同时不影响 §8 四条出口判据中任何一条的判定，也不推进 §5.0 阶段指针。

## 1. A — `NotAfter` 语义一致

**被验形状**：`PendingBubble(…, DateTimeOffset NotAfter, bool AnnounceTyping, DateTimeOffset? ExpiresAt = null)`（`src/Pry.Core/Runtime/Delivery.cs:4-5`）。全部使用点（本轮逐个读，不是搜出来就算）：

| 位置 | 用法 | 与"最早可投递时刻"是否一致 |
| --- | --- | --- |
| `Delivery.cs:33` | `AddRange` 后按 `left.NotAfter.CompareTo(right.NotAfter)` **升序**重排 | 一致：越早到点越早投。 |
| `Delivery.cs:47-49` | `TakeDue`：`if (_items[0].NotAfter > at)` ⇒ 未到期，`wait = NotAfter - at` | 一致：差值为正才等。 |
| `Delivery.cs:85` | `ShiftAll(delta)`：`WithNotAfter(NotAfter + delta)` | 一致：正向位移=推迟投递（注释同义："把未投递部分整体后移"）。 |
| `Delivery.cs:7` | 唯一 mutator `WithNotAfter` | 一致。 |
| `Delivery.cs:191` | `Enqueue` 里 `at += 节奏延迟` 逐条累加成 `NotAfter` | 一致：首条 = 现在 + 首条延迟。 |
| `RuntimeModels.cs` / 其余 | 该字段无其他读者 | — |

- **无反向语义**：没有任何一处把 `NotAfter` 当"截止"用。"截止"语义在同一个 record 里由**另一个字段** `ExpiresAt` 承担（`Delivery.cs:323-324`：`_clock.GetUtcNow() >= expiry` ⇒ 放弃），两者方向相反且各在其位——这正是重命名后必须核对的那对易混项。
- **旧名零残留**：`NotBefore` / `NoEarlierThan` / `EarliestAt` / `DelayUntil` / `DueAt` 在 `src/`、`tests/`、`docs/` 全仓 **0 命中**。`deadline` 一词在 `Pry.Core/Runtime` 里只属于另外两个概念（`InputBuffer.cs:141-144` 的缓冲 starvation guard、`ConversationRuntime.cs:1228` 的一次 `Task.Delay`），与气泡无关；两条测试名里的 "deadline"（`RuntimeComponentTests`、`RuntimeRaceTests:49`）指的也是那两个概念。
- **文档侧无错配**：`docs/phase2-runtime-contract.md` 不提该字段名，因此不存在"契约写旧名、代码用新名"的形状。

**限制（一条，不翻判定）**：`NotAfter` 在 `tests/` 里 **0 命中**——重命名后的一致性目前**只有静态读码 + 间接行为用例**（投递顺序、打断后剩余气泡取消、候选到期不投递）支撑，没有一条断言直接落在该字段上；并且 `ShiftPending → ShiftAll → WithNotAfter` 这条链在 `src/` 内**没有调用者**（`ConversationRuntime` 只用 `Enqueue/CancelTurn/CancelAll/Pending/PendingCount/DeliveringTurn/DeliveredFor`），也就是说"用户仍在输入时推迟已排队气泡"这条语义今天既不接线也无测试。⇒ 记入 §5 backlog 第二行；A 本身仍判 Pass，因为"一致"是对现有使用点的性质判断，而不是对覆盖率的性质判断。

## 2. B — malformed `ResponsePlan` sequence

**方法**：一次性探针在**真实 `MemoryDatabase`（临时 SQLite 文件）**上跑，不经过 `Pry.Api`，也不改任何判据。形状：直接以 `[a,b]` 的 sequence 建计划，然后按 Runtime 的动作顺序逐条 `CommitRuntimeBubbleAsync`。

| 形状 | 入库申请 | 逐条 commit 结果 | 最终 assistant 行数 | 计划终态 |
| --- | --- | --- | --- | --- |
| 正常 `[0,1]` | Approved | `seq0→msg1`, `seq1→msg2` | 2 | `closed` |
| 重复 `[0,0]` | Approved（两行 sequence 都写进去） | `seq0→REFUSED`, `seq0→REFUSED` | **0** | `cancelled` |
| 乱序 `[1,0]` | Approved | `seq1→REFUSED`, `seq0→msg1` | 1 | `cancelled` |
| **跳号 `[0,2]`** | Approved | `seq0→msg1`, **`seq2→msg2`** | **2** | **`closed`** |
| **负号 `[-1,0]`** | Approved | **`seq-1→msg1`, `seq0→msg2`** | **2** | **`closed`** |

### 2.1 被拒的两格，拒在哪一层
守卫在 `MemoryDatabase.Runtime.cs:191-207` 的条件 `UPDATE`：`state='pending'` + **`NOT EXISTS` 更早且未 delivered 的行** + 计划 `status='open'` 且 epoch 对得上；`ExecuteNonQuery != 1` 即回滚返回 `null`。
- **乱序**：`seq1` 因为 `seq0` 还 pending 而被拒 ⇒ 前缀序在**落库级**成立，不依赖调用方自觉。
- **重复**：两条同 sequence 的行让那条 `UPDATE` 一次命中 **2 行** ⇒ `!= 1` ⇒ 回滚。**注意代价**：重复号会把整个计划**全拒**（连第一条正常气泡也发不出去），不是"只拒重复的那条"。方向是 fail-closed，符合契约 §7.1 的"宁可少发不多发"，但严重程度值得 Owner 知道。

### 2.2 不被拒的两格（本轮的真发现）
- **跳号**：守卫只看"有没有更早的未投递行"，`[0,2]` 里 `seq2` 的更早行 = `seq0`（已投递）⇒ 通过；`planned_count` 又是按 `messages.Count` 写的（`RuntimeAdapters.cs:45` 传 `messages.Count`），所以 2 行/2 计数自洽，**没有任何一处知道编号少了一格**。
- **负号**：`sequence < -1` 的行不存在 ⇒ `NOT EXISTS` 天然成立；`planned_messages` 也**没有** `CHECK (sequence >= 0)`，`(reply_plan_id, sequence)` 上只有普通索引 `ix_planned_messages_plan`（`MemoryDatabase.cs:115`），**不是 UNIQUE**。
- **入库申请层（`RequestRuntimeDeliveryAsync`）对五种形状一律批准**：它只校验 epoch，不校验 sequence 形状。⇒ "malformed 在入口被拒"这句话目前**不成立**。

### 2.3 为什么仍不阻塞 RT-002
今天唯一的计划生产者产不出畸形形状：`ReplyPlannerComposer`（`RuntimeAdapters.cs:84`）把 `PlannedReplyMessage.Sequence` 原样搬过来，而 `ReplyPlanner` 是按 `index` 赋值（`ReplyPlanner.cs:100`、`:125-126`）；`ProactiveScheduler.cs:104` 只造单条 `Sequence = 0`；`RuntimeFakes.cs:216` 同理。**所以 1.0 发行链路上这条不可达**——按作者"发现非当前范围问题只登记 backlog"的令，转 §5 `DEF-P2PLAN-01`，不作为 B 的 Fail，也不阻塞 RT-002 与阶段指针。

### 2.4 顺带记录：单测 fake 与真实库对同一形状结论相反
`FakeRuntimeStore.CommitBubbleAsync`（`RuntimeFakes.cs:138`）要求 `sequence == row.NextSequence` **严格递增**，因此跳号/乱序/负号在**单元测试里全部"被拒"**，在**真实 SQLite 里跳号与负号"投递成功"**。⇒ 任何"已验证 malformed 会被拒"的结论如果只建立在 fake 上，就是**假绿**。这条既约束 `DEF-P2PLAN-01` 的结案判据（必须跑真实库），也解释了为什么 B 值得单列一节。

## 3. C — commit 成功、event publish 失败

**方法**：同一个探针文件里的抛出型 `IRuntimeOutputSink`（第一次 `MessageDeliveredAsync` 抛 `IOException`），后端仍是真实 `MemoryDatabase`；两张气泡、`RuntimeSettingsFactory.Fast()` 节奏。

实测（一次跑出的原始读数）：

```
publish_threw=True  report=planned2/delivered1/abandoned1/delivery_failed  failures=1  pending=0  typing=[on,off]
messages=1  content=one  planned=0/delivered , 1/cancelled  plan_status=cancelled
after_restart: swept=0  messages=2  content=one , fresh
```

三条子判据逐条：

1. **DB message 保留 — 成立**。消息行的写入与事件发布是两次独立动作：`CommitRuntimeBubbleAsync` 在自己的事务里同时把 `planned_messages` 置 `delivered` 并插 `messages` 行（`MemoryDatabase.Runtime.cs:186-245`），`_sink.MessageDeliveredAsync` 在其**之后**才调用（`Delivery.cs:331`）。发布抛出后 DB 里仍是那 1 行 `one`。
2. **reload 能恢复 — 成立**。重启动作 `SweepStaleRuntimePlansAsync` 扫到 **0** 条 open 计划（失败善后时 `AbandonRuntimePlanAsync` 已把计划置 `cancelled`，`MemoryDatabase.Runtime.cs:254-255`），历史读侧以 `messages` 为准（协议日志自己的注释：`MemoryDatabase.ProtocolEvents.cs:8` "后者永远以 messages 为准"）。换库重建调度器后新一条 `fresh` 正常投递，总行数 = 1 + 1。
3. **不产生重复 message — 成立**，而且有三重独立理由：① 队列已 `RemoveAt(0)`，失败的气泡不会重投；② `accounted` 标志在 commit 成功后置真，`catch` 里不会再为同一条记账（`Delivery.cs:328-338`）；③ 即使有迟到写者拿着旧 `permission` 重投，守卫的 `state='pending'` 与 `status='open'` 都不再满足 ⇒ 返回 `null`。

**两处如实限制（不改判定，改它能被用来说什么）**：
- **本轮剩余气泡会被一起取消**：一次瞬时发布失败等于截断这条回复（`abandoned=1`，reason `delivery_failed`）。这是 fail-closed 的既有设计（与 commit 失败同一条路径），但它对用户的可见后果是"话说了一半"——不属于 A–D 的判据，也不属本轮范围，只在 §5 第二行留痕。
- **已投递那条不会在 SSE 上自动补投**：`message.created` 是持久类事件（`VolatileEventTypes` 只排除 `session.ready`/`turn.state`），而活动侧的发布失败被 `ConversationEventLog.Publish` 自己吞掉并记 warning（`ConversationEventLog.cs:59-64`，注释原话"写不成即不曾存在……不是留洞"）。⇒ 客户端要看到那条气泡，需要重开会话/重新拉历史；"不留洞"这一点已由既有永久用例 `ProtocolEventJournalTests`（`Assert.DoesNotContain(rows, row => row.Sequence == doomedEvent)`）钉住，本轮不重复登记。

## 4. D — 现有 RT-002 suite 全绿

载体：`44e52fc` 干净 worktree，Release，`--no-build`（跑前刚构建过，探针文件已删除并确认程序集里 `Rt002ProbeTests` 零命中）。

| 范围 | 过滤器 | 结果 |
| --- | --- | --- |
| Core | `ConversationRuntimeTests` \| `RuntimeComponentTests` \| `RuntimeRaceTests` \| `RuntimeTriggerTests` \| `RuntimeTriggerIntegrationTests` \| `ProactiveDeliveryTests` | **101 / 0 / 0**（31 s） |
| Api | `ConversationRuntimeEndpointTests` \| `ReplyFrequencyHostPortTests` \| `ProactiveSchedulerTests` \| `ProactivePolicyTests` \| `ProactiveGeneratorTests` | **26 / 0 / 0**（10 s） |
| App | `ConversationSettingsEditorTests` | **3 / 0 / 0** |
| Client | `ProactiveClientTests` | **1 / 0 / 0** |
| **RT-002 合计** | — | **131 通过 / 0 失败 / 0 跳过** |

解决方案级同载体复跑（不是 D 的必要项，采来给 §5.0 那条"并行连续 ≥2 次全绿"多一个数据点）：**1 次全绿 = 675 通过 / 0 失败 / 2 跳过**（Client 92、App 183、Api 132、Core 268+2skip；TRX `.local-notes/rt002final-results/full-rt002final.trx`）。**样本只有 1 次，因此本轮不据此宣称"连续 ≥2 次全绿"成立**；`DEF-P1WALK-03`/`04` 两条竞态仍在，Phase 1 与 Phase 2 的结案动作照旧等它们修完（作者裁定"不推：等门禁连绿"）。

## 5. Backlog（作者令：非当前范围问题只登记，不阻塞 RT-002）

1. **`DEF-P2PLAN-01`（Low，Owner＝Character Runtime / RT-002，不阻塞）**：`ResponsePlan.Messages[].Sequence` 的形状只在**前缀序**上被权威守卫，跳号与负号可在真实库投递成功，`planned_messages` 无 `CHECK`、`(reply_plan_id, sequence)` 非 UNIQUE、`RequestRuntimeDeliveryAsync` 不校验形状。**结案判据**：一条**跑真实 `MemoryDatabase`**（不是 `FakeRuntimeStore`，见 §2.4）的用例，把 `[0,2]` 与 `[-1,0]` 两形状的最终 assistant 行数与计划状态**固定成被文档化的语义**——两种收尾都可接受：要么存储真的拒（0 条），要么契约与 `RuntimePorts.cs` 注释明写"连续性由生产者保证、存储不校验"并与此一致；**选哪一种属 Owner 的设计判断，QA 不代拍**。**候选修复面**（仅记录，不是判据）：入库申请处一次线性校验（`sequence == index`）成本低、无需迁移；补 `CHECK`/`UNIQUE` 要动 schema，属另一次合法迁移窗口。**反向约束**：不得只改 fake 让单测变绿；不得为此新增输出通道或改 `IRuntimeOutputSink` 签名；不得为这一条 Low 缺陷单独插一次数据迁移。**台账落点**：`PROJECT_PLAN.md` §4.2 `DEF-P2PLAN-01` 行（**口径以该行为准**，本段与其差异处该条更保守）。
2. **观测/接线留痕（不立缺陷号，等轮到 Phase 3 再说）**：`DeliveryScheduler.ShiftPending` 链路无调用者、`NotAfter` 零直接断言（§1 限制）、一次瞬时发布失败会截断本轮剩余气泡（§3 限制）。三者都不改变 A–D 的判定。

## 6. 复现命令

```powershell
git worktree add --detach .local-notes/p2-0924-close 44e52fc
cd .local-notes/p2-0924-close
dotnet build Pry.slnx -c Release                       # 0 警告 0 错误
dotnet test tests/Pry.Core.Tests -c Release --no-build --filter "FullyQualifiedName~ConversationRuntimeTests|FullyQualifiedName~RuntimeComponentTests|FullyQualifiedName~RuntimeRaceTests|FullyQualifiedName~RuntimeTriggerTests|FullyQualifiedName~RuntimeTriggerIntegrationTests|FullyQualifiedName~ProactiveDeliveryTests"
dotnet test tests/Pry.Api.Tests -c Release --no-build --filter "FullyQualifiedName~ConversationRuntimeEndpointTests|FullyQualifiedName~ReplyFrequencyHostPortTests|FullyQualifiedName~ProactiveSchedulerTests|FullyQualifiedName~ProactivePolicyTests|FullyQualifiedName~ProactiveGeneratorTests"
dotnet test tests/Pry.App.Tests -c Release --no-build --filter "FullyQualifiedName~ConversationSettingsEditorTests"
dotnet test tests/Pry.Client.Tests -c Release --no-build --filter "FullyQualifiedName~ProactiveClientTests"
```

B/C 的探针源码不进仓库（作者令"仅验证"），其原始读数已逐行抄在 §0/§2/§3；探针文件与 scratch worktree 的清理记录见本轮台账 §9。
