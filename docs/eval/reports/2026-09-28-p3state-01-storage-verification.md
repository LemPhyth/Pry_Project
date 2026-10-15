# Phase 3 落库刀复核（`DEF-P3STATE-01`）· 2026-09-28

- **Run ID**：`eval-QA-P3STATE-0928`
- **判定人**：QA / Evaluation（EVAL-001）
- **载体**：**主工作树，未提交**。HEAD 仍是 `afecf1d`（Phase 3 第一刀：22 条一致性判据的抽象基类）。本刀的四个文件面全在 `git status` 里：`src/Pry.Core/Memory/MemoryDatabase.cs`（改）、`docs/character-intelligence.md` / `CHANGELOG.md` / `docs/README.md`（改）、`MemoryDatabase.Intelligence.cs`、`IntelligenceRepositories.cs`、`SqliteIntelligenceRepositoryConformanceTests.cs`（三个新文件均**未跟踪**）。
- **结论先写在最前面**：**没有可指认提交 ⇒ §8.1 段② 不能签**。本轮是"落地面复核 + 未接面盘点"，不是验收判定轮，不给任何阶段状态词。
- **与上一轮的关系**：上一轮（Phase 3 进度盘点）的结论是"存储层不存在——没有一张学习状态表"。本刀把它变成"存在"。本轮核的就是这一句，并且全部读数当场重采，不引用 Owner 的任何数字。

---

## 1. 当场重采的读数

构建：`dotnet build Pry.slnx -c Release` ⇒ **0 警告 / 0 错误**。

| 跑法 | 范围 | 结果 | 退出码 |
|---|---|---|---|
| 定向 | Intelligence 一致性两类 | **47 / 0**（in-memory 22 + SQLite 25） | 0 |
| 全量一次 | 解决方案级（混树） | **739 / 0 / 2**（Client 92、App 183、Api 132、Core 332+2skip） | build 0 / test 0，TRX 落在 `.local-notes/qa-p3state-0928/trx` |

两点必须如实标注：

1. **739 是在混树上跑的**，同一工作树里还有 RT-002 在途的 `-03` 代码刀、Owner 的契约改写、QA 自己名下的 `docs/eval/**`。按 §5.0 判据 4「读数不得互顶」，它只证明"当前工作树不破门禁"，**不构成任何一刀的段②**。Owner 在 §9 里也是这样登记的，且 +26 条的归因（本刀 25 + RT-002 在途 1）我本轮核对成立。
2. **那 13 处突变反证我没有重跑。** 我逐条读过其登记（批 A 四处点到 7 例、批 B 六处点到 9 例、把建表钩子抽掉 ⇒ 派生类 25/25 全红、其中 24 例报 `no such table`），被点到的用例名与判据名一一对得上；但按契约 §8.2，**别人声称的证伪不能引用为 QA 验过的证据**。本轮我独立验的只有下节三条。

## 2. 我独立验的三件事

1. **22 条继承断言没有被放宽**：`git diff HEAD -- tests/Pry.Core.Tests/IntelligenceRepositoryConformanceTests.cs` 输出为空，字节级零改动。这是作者派工时的原话约束（"断言不许放宽"），也是本刀唯一能"看着全绿其实什么都没测"的通道——它是关着的。
2. **分层边界守住**：`Pry.Core.Intelligence` 与 `Pry.Core.Expression` 内无 `Microsoft.Data.Sqlite` 引用，全部 SQL 收在 `Pry.Core.Memory`；四个适配器只做委托。
3. **没有新增第 22 处进程级清池**：全仓 `SqliteConnection.ClearAllPools()` 仍是 21 个文件。（**仪器自纠**：我第一次按子串 `ClearAllPools` 数到 22 个文件，一度判定本刀违反了派工的反向约束；收敛到完整调用式后是 21，第 22 个命中只是注释里写"不许用它"。错的是我的筛法，不是代码。）

`FlushAsync` 值得单独记一句好话：它用 `PRAGMA wal_checkpoint(TRUNCATE)` + `SqliteConnection.ClearPool(自己那条连接串)`，并在注释里写明"连接串三档参数必须与 `MemoryDatabase` 内部逐字相同，抄错一档这道 ClearPool 就变成谁也没清的空操作"。**这正是 `DEF-P1WALK-04`（进程级误杀并行用例句柄，先修地基两条之一）的反面写法**——一把还没接线的刀里，已经给出了那类红的正确解法示范。

## 3. 未接面：用户今天看不到任何变化

| 事实 | 证据（本轮当场） |
|---|---|
| 四个 SQLite 适配器在生产代码里**零构造点** | `grep -rn "new Sqlite.*Repository(" src/` 无输出；全仓唯一引用者是判据自己 |
| `PurgeAsync` 无人调用 | 接口 + in-memory + SQLite 三处实现，零调用点 |
| 真机仍走过渡实现，学到的东西不入库 | 宿主注入 `TransitionalReplyAdvisor`（上一轮已核，本刀未碰任何读路径） |
| 删卡对学习状态**既不清也不挡** | `CharacterHasReferencesAsync`（`src/Pry.Core/Memory/MemoryDatabase.cs:408-415`）的 EXISTS 只查 `conversations` 与 `memories`，五张新表不在其中；`DeleteCharacterAsync`（`src/Pry.Api/Services/ConfigurationApplicationService.cs:32-75`）里既无 purge 也不看这些表 |

⇒ **上一轮我写给作者的那句用户可见症状，本刀之后一字未变**：真机关掉再打开，她的心情、关系、学到的口癖仍然全部归零。这不是这批代码的缺陷——派工单明令"不得顺手接线"——但如果有人把这把刀读成"她终于有记忆了"，那是错的读法。发行行为在这把刀上为零。

## 4. 本轮新登记

### 4.1 `QA-P3STATE-01`（Low，文档准确性，Owner＝Core Platform）

`CHANGELOG.md` 新条目的第一句是面向用户的口径：**"学到的东西第一次有了活过重启的地方"**；而同一轮的 §9 登记里明写"本刀不主张『学到的东西已经在真机里活下来了』——那需要接线，归 RT-003 下一刀"。同一件事，两处文本给出相反的用户承诺（该条目正文其余部分确实把未接面登记得很完整，问题只在标题句）。修法只能是改措辞，不能靠补接线来追认——那会把派工单"不得顺手接线"的约束反过来违反。

### 4.2 `QA-P3STATE-02`（Low，载体隔离，Owner＝守门人／QA）

`docs/README.md` 新增一行链接 `docs/decision-request-protocol.md`，而该文件至今**未跟踪**（`git ls-files docs/decision-request-protocol.md` 无输出）。从任何提交建出的干净 worktree 里，这个索引指向一个不存在的文件。与 `QA-P2CLOSE2-01` 同族：**规则文本不在提交上 ⇒ 干净载体看不到当下的规则**。修法=作者点头后，README 那一行与该文件一起提交。

### 4.3 `QA-P3STATE-03`（High 于接线时成立，现 Low；Owner＝Character Intelligence，应并入 RT-003 缺口 ②）

台账把缺口 ② 写成"purge 调用点未接"。实测还差一半：**删除的门禁查询本身也看不见那五张表**。所以接线之后的第一次删卡，学习状态既不会被拦、也不会被清，直接成为永久孤儿行。

- 一处如实缓和：卡片 id 是 `character-{Guid}` 随机生成（`ConfigurationApplicationService.cs:22`），删除后不会复用 ⇒ 不会出现"新卡继承旧卡的记忆"这类状态污染，问题只在**数据留存／隐私**面，不在正确性面。
- 建议落成**配对约束**：RT-003 下一刀（第一次写入学习表的接线）**必须同批**接 purge 调用点并把五张表纳入删除判定。否则第一次写入就把孤儿行带进发行链，而那条链上的症状不可逆（用户已经删过的角色，其关系与经验仍在库里）。

### 4.4 上一轮报的两处 §4.2 台账错仍未动（Low，Owner＝守门人）

"Phase 3 已有 79 项固定 Eval"（实为单元测试计数，固定 Eval 目录是 `docs/eval/scenarios.json` 那 75 条永久 ID）与 Phase 3 行的"schema 当前为 v8"仍是一字未动；本刀的 §9 记录把 v10 写进了交接记录，于是同一份计划现在同时存在 v8 的旧陈述与 v10 的新事实。两处都是纯文本修订，但只能由登记方改。

## 5. 这把刀对"地基"的意义（位置感，不是判定）

Phase 3 真正的难点从来不是字段，是判据。`afecf1d` 立了判据的抽象基类，本刀第一次让它跑在真 SQL 上并**证明它会红**（抽掉建表 ⇒ 25/25 全红）。这意味着此后任何一版落库实现，只要在时间序、版本并发、守卫子表往返、purge 计数这四条上偏离语义，就会当场变红，不必靠人盯文档段落——这是 §5.0"这一层地基站不站得住下一层"里实在的那部分。

剩下的一半仍然悬着：**接线那一刀不得为跑通宿主而放宽这 22 条继承断言**。派工单已经预先禁止（要动判据文本须由 Intelligence Owner 提出、QA 重判），所以那是一条有主的约束，不是本轮的欠账。

## 6. 下一轮的前置

1. 作者点头 ⇒ 本刀形成一个可指认提交 ⇒ QA 从该提交建 `--detach` 干净 worktree 复跑，那才轮到 §8.1 段②。同一动作顺带解决 `QA-P2CLOSE2-01`、`QA-P3STATE-02`（`docs/eval/**`、`scripts/measure-fixed-eval.ps1`、`docs/decision-request-protocol.md` 至今全部未跟踪）。
2. 段②轮我不重跑 13 处突变，但**抽查 2–3 处**：要求 Owner 当场演示，或我自己复现一次并字节级还原。
3. 先修地基两条（`DEF-P1WALK-03`、`-04`）Owner 与优先级不变；Phase 1／2 的结案动作仍是同一件事（等那两条修完后的连续两遍全绿）。本轮零产品代码改动、零 stage、零 commit、零 push、§5.0 指针未动。

---

## 7. 更正与状态迁移（QA 于 2026-09-29 追加，不改写上文任何技术判定）

上文写于"无提交载体"那一刻，那条前提已经变了，按时间顺序记清楚，免得后来人把本轮的免责读成现行状态：

1. **载体前提**：`b66efc8` 已入库（作者令"可以把三次提交做了"），本刀的四份新文件与 `MemoryDatabase.cs` 的改动自此在一个可指认提交上；现行 tip 是 `1123be0`。⇒ 上文"没有可指认提交 ⇒ 段② 不能签"这一条**前提解除**。
2. **但段② 仍未签**：`b66efc8` 之后同一天又落了两刀（`7347dfe`、`1123be0`）与 `DEF-P1WALK-03/-04` 两条地基改动，本刀自己的 §8.1 段② 复跑还没做。上文 §6 第 2 条欠的"抽查 2–3 处突变"仍然欠着，不在 2026-09-29 那一轮（`eval-QA-P1W0304-0929`，只核 `-03`/`-04`）的范围内。⇒ **本刀的现行状态词 = `已提交待验`，不是 `QA 判定`。**
3. **`QA-P3STATE-03`（接线时 High）已结案**：其孤儿行前提——"purge 无调用点、删卡不清学习状态"——随 `b66efc8` 消失。现行落点见 `ConfigurationApplicationService.cs`：`learning.PurgeAsync` 在 `sessions.ReconfigureAsync` **之前**、且在能把备份退回的那一段之内；五张新表**刻意不进** `CharacterHasReferencesAsync` 的 EXISTS（同文件注释已写明理由：学习状态一存在就永远存在，加进判定等于把角色卡变成删不掉）。`CharacterLearningDeletionTests` 三条用例（清空 / 拒绝删除不动任何东西 / 清理失败可重试）覆盖该形状。
4. **`QA-P3STATE-01`／`-02` 状态**：`-01`（CHANGELOG 标题句与 §9 免责句相反）随提交面已定，措辞是否改写归 Owner；`-02`（`docs/README.md` 链向未跟踪的 `docs/decision-request-protocol.md`）**仍未结**——该文件到 2026-09-29 依旧是 `??`，与 `QA-P2CLOSE2-01` 同族。
5. **上文 §3 的"用户可见症状一字未变"仍然成立**：`b66efc8` 接的是构造点与删除侧清理，**学习状态的写入仍无生产者**，真机关掉再打开，心情／关系／学到的口癖照样归零。这句话不要被"她已经落库了"读成已通过。
