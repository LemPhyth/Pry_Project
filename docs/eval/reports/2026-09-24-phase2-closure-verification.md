# Phase 2（Character Runtime / 触发层）结案标准验收 · `eval-QA-P2CLOSE-0924`

日期：2026-09-24　验收人：QA / Evaluation（EVAL-001）　Owner 归属：Character Runtime Owner（RT-002）
被验对象：`docs/phase2-runtime-contract.md` **§8 出口判据四条**（作者口径："Runtime 的阶段任务做完了……现在按照结案的标准来验收 Phase 2"）
承载提交：**`44e52fc`**（tip；含 `8e039f9` 触发层移植 + `e6f0848` 状态行 + `957002f` `DEF-P2TRIGGER-02` 夹具修复 + `44e52fc` §7 第 1 步宿主端口频率半边）
载体形状：`git worktree add --detach .local-notes/p2-0924-close 44e52fc`，开工前后 `git status --short` 均为空
本文签署的是 §8.1 的 **段②**（独立复跑 + 真机走查）。**`阶段结案` 另需段③（作者推进 §5.0 指针与 `DEF-*` 裁决）与段④（Release）**，二者不在 QA 权限内。

## 0. 结论

**入场券已全绿、`DEF-P2TRIGGER-02` 结案、§8 第 3 条那四条行为第一次拿到真机+真实模型证据。但按结案标准逐条给词，Phase 2 只能走到"QA 判定"，不能记 `阶段结案`：四条出口判据里 ④ 成立、③ 有限度成立、①② 的动作在作者手里。**

**就地更新（本轮末，作者 2026-09-24 落笔四条 ⇒ QA 只记录不代拍）**：① 由 `Blocked` 转 **`Pass`**（作者裁"**显式降级为 1.0 不做**"），② 仍 `Blocked` 但性质改变（作者裁"**保留量级，押 Eval 标定**"⇒ 从"无人拍"变成"有归属、有前置的待办"），§8.1 段③ 作者裁"**不推：等门禁连绿**"，四条 `DEF-*` 派工全部照本报告点名的 Owner 确认。裁定原文、生效前提与未落地部分见 §9。**Phase 2 的状态词因此仍停在 `QA 判定`，`阶段结案` 未成立——这一次不是因为 QA 判了 Blocked，而是因为段③ 的指针作者明令不推进。**

| §8 判据 | 判定 | 依据（本轮实测，非引用他轮） |
|---|---|---|
| ① 上游一致性：§3 每节有判定，`Pending` 要么关闭要么由作者显式降级 | **Blocked**（动作在作者） | §3 逐节判定 15 处齐备、无删除行（上轮已核，本轮复核 §6 表未变）；但 `DEV-03/04/05/06/08/09/10/11` 状态仍为"开"，其中 `DEV-03/04/05/06` 的类型是 `Pending`（`DEV-03` 即 §7 第 2 步的标定，它同时受"§5.1 参数箱不进发行默认"约束）—— 按 §8 第 1 条它们**必须被关闭或由作者显式降级**，QA 不代为落笔。另：§7 第 1 步 `IsGroupChat` 的"降级到后续阶段"结论 Owner 已写清、落笔权归作者 **⇒ 就地更新（作者本轮裁"显式降级为 1.0 不做"）：本条转 `Pass`。生效前提是 Owner 把 `docs/phase2-runtime-contract.md` §6 的 `DEV-03/04/05/06` 四行改写为"1.0 不实现、转入后续阶段（作者 2026-09-24 显式降级）"、§9 的群聊退避两行标 `NotApplicable`；文本未落地之前 QA 只记"裁定已给出、契约文本待改"。QA 提请复核一处（不阻塞本条判定）：`DEV-03` 是 §7 第 2 步的 Eval 标定，而 ② 的裁定是"押 Eval 标定轮"——两条裁定落在同一个动作上，Owner 改写时建议 `DEV-03` 写成"1.0 内不标定、由 ② 的标定轮承接"，否则 §7 第 2 步会在文档层被提前关掉** |
| ② 合规：`DEF-P2TRIGGER-01` 有作者裁决，§5"同值"行被标定替换或显式保留 | **Blocked**（作者 §5.1 押后） | §5.1 原文即"§8 出口判据第 2 条……本条整体仍未成立"；本轮复核该表述与工作区一致：`appsettings.json` 无新增字段、`TriggerMode` 默认仍 `MessageCount`、`ReplyFrequency` 默认 `1.0` ⇒ 参数箱仍"冻结在不可发行的位置上"，Owner 也确未把这箱值写进发行说明或配置模板 **⇒ 就地更新（作者本轮裁"保留量级，押 Eval 标定"）：本条仍 `Blocked`，但性质变了——动作不再是"等一个无人给的裁定"，而是"等一轮有归属、有判据的标定"。逐值由 Phase 2 发行前的 Eval 标定轮覆盖，§5 处置列需 Owner 按此改写并把该轮的准入条件写清（哪些值、什么语料、判据是什么）。QA 的连带义务：标定轮的永久 ID 由我 mint，不在本轮**。 |
| ③ 行为：真实模型节奏下"连发可合并、可不回复、插话取消旧输出、切会话不串状态"四条各有真机证据 | **Pass**（四条各有真机证据）＋两处如实限制 | §3 表：W1/W2/W3/W4 各一条，跑的是 `44e52fc` 编出的 `Pry.Api.exe` + 真 llama-server（`local-llama` / `qwen2.5-vl-3b-instruct-q4_k_m.gguf`）。限制一：**"可不回复"今天成立的是宿主频率端口，不是社交判断**（`ReplyGate` 轨迹逐条为 `transitional_no_social_judgement`，`BEH-007`/RT-003 判断链仍未接）；限制二：**内容侧判断不可评**，`DEF-P1WALK-01` 本轮 4 例复现污染了每一条落库答复（§5） |
| ④ 集成：RT-002 全部改动进入可指认提交，QA 从该提交的干净 worktree 复跑 §5.0 门禁 | **Pass** | 载体 `44e52fc`（tip、可指认、worktree `--detach` 且干净）；`dotnet build` 0/0；解决方案级**连续三次全绿 675/0/2**；`verify-repository.ps1` 394 个受跟踪文件；`git diff --check` 干净；`measure-fixed-eval.ps1` **exit=0**（§1） |

一条缺陷结案：**`DEF-P2TRIGGER-02`**（测试竞态）——本轮在 `44e52fc` 上连续三次全绿，该用例不再红。
三条新登记：**`DEF-P2TRIGGER-03`**（Low，观测性：门控轨迹把"退避再施加"写成"阈值不足"，Owner RT-002）、**`QA-P2CLOSE-01`**（Medium，QA 自有测量脚本的"删号即红"守卫可以在绿灯里失效，已修 + 反证）、**`DEF-P1WALK-01` 第 7 次复现**（High，Owner Character Intelligence，本轮 4 例）。
一处目录自我更正 + 目录升版：`BEH-008` 定稿前按实跑载体校准文本；新增 `PLT-014` ⇒ `suiteVersion` **1.2.0 → 1.3.0**（74 → 75 条）。

## 1. 入场券四条（全部在 `44e52fc` 的干净 worktree 上实测）

| 项 | 命令 | 结果 |
|---|---|---|
| 构建 | `dotnet build Pry.slnx -c Release` | **0 警告 0 错误**（13.25 s） |
| 测试（三次连续，独立证据目录） | `dotnet test Pry.slnx -c Release --no-build --logger "trx" --results-directory …run-{a,b,c}` | 三次均为 **675 通过 / 0 失败 / 2 跳过（总 677）**：Client 92、App 183、Api 132、Core 268+2skip（13:51:41 / 13:52:51 / 13:53:55 三份 TRX 各 4 个程序集文件） |
| 仓库校验 | `powershell -File scripts/verify-repository.ps1` | `Repository verification passed for 394 tracked files.`（exit 0；上轮 393，+1 即 `44e52fc` 新增的 `ReplyFrequencyHostPortTests.cs`） |
| 差异 | `git diff --check` | 干净 |

`scripts/measure-fixed-eval.ps1 -ResultsDirectory …run-c`：`Catalog valid: 75 permanent scenarios`、`Passed=675; Failed=0; Other=2`、**exit=0**；覆盖 **61 PartialPass / 14 Unmapped**、`missing=[]`（无任何映射指向不存在的用例）。
上轮 `Fail` 的唯一红（`RuntimeTriggerIntegrationTests.cs:88` 的 20 ms 墙钟竞态）在本载体上不再出现 ⇒ **`DEF-P2TRIGGER-02` 结案**。

**给 Phase 1 的一条附带读数（不改变 Phase 1 判定）**：本轮是"同一载体连续 ≥2 次全绿"这一条出口条件的一次新证据——**连续 3 次**。但 `DEF-P1WALK-03`/`-04` 的实测竞态率（约 1/10 与加载下 3/3）意味着三次绿不能把那条件判为成立，也不许反过来用它结案；两条红仍归各自 Owner。

## 2. 两刀的反向约束复核（`957002f`、`44e52fc`）

上一轮 `Fail` 时我给派工单写了四条"不许走的捷径"，本轮逐条实读复核：

| 反向约束 | 复核结果 |
|---|---|
| 不许改判据让它变绿 | `957002f` 的 `git show --stat … -- src/` **为空**（零产品代码改动）；夹具改的是 `MinimumAverage` 20→200 ms 与 `WaitUntilAsync` 超时 150→2500 ms，判据字符串 `entry.Action == "trigger:Backoff"` 原样保留，并**新增**了一条 `Assert.Equal(1, harness.Store.AppendedInputs)`（收紧不放宽） |
| 不许用固定 sleep 掩盖竞态 | `WaitUntilAsync` 实现是 `while (!condition() && TickCount64 < deadline) await Task.Delay(10, …)`——轮询到条件成立即返回，超时只是上限；不是把 20 ms 换成 200 ms 的定长等待 |
| 不许把发行默认变"新行为已生效" | `44e52fc`：`RuntimePorts.cs:152` 默认仍 `ReplyTriggerMode.MessageCount`；`CommonContracts.cs` 的 `ReplyFrequency` 默认 `1.0`（= 有输入即成轮）；`src/Pry.Resources/appsettings.json` **未新增该字段**（本轮实读确认）。⇒ 未配置的用户链路行为与本刀之前逐位相同 |
| 不许把 §5 那箱参数写进对外默认 | 文档增量只有 `docs/api-v1.md` +5 行（字段、`0–1` 边界、`400` 语义），未出现任何"默认改成 `ReplyNecessity`"或上游同值表的默认值推荐 ⇒ 与 §5.1 一致 |

一处 Owner 自引入并已修的回归，QA 侧确认修得对：`ConversationSettingsEditor.BuildDraft()` 原从零构造记录，加字段后"保存一次面板就把频率静默重置回 1.0"，已改为从打开时的源记录 `with`，并由 `Pry.App.Tests.ConversationSettingsEditorTests.Saving_the_panel_keeps_a_frequency_the_panel_never_shows` 守住。这一条不在派工单里（是 `44e52fc` 自己带出来的形状），我按同一口径复核后计入 §1 的 App 182→183。

## 3. §8 第 3 条：四条行为的真机证据

统一条件：`Pry.Api.exe`（从 `44e52fc` worktree 构建，exe mtime 13:51:12）监听 `127.0.0.1:5095`，`Pry__DataDirectory` 指向隔离目录 `.local-notes/p2close/walk1`（从 live 目录复制 `preferences.json`/`character.json`/`characters`/`theme-media`/`media`/`character-media`，**不复制 `memory.db`**）；描述符 `pid=69208 port=5095 proto=1.0 schema=9 tokenLen=44 tokenSha8=8272B1D0 pidMatches=True`（只记长度与哈希，不记令牌）。模型：Provider `local-llama`、档位 `qwen2.5-vl-3b-instruct-q4_k_m.gguf`（注册名 `qwen2.5-vl-3b-local`，context 32768，RTX 5070 Ti/CUDA）。**作者 live 目录 `%LOCALAPPDATA%\PryCompanion` 跑前跑后逐文件 mtime 一致**（`memory.db` 仍 09-14 18:22:57、`-shm` 09-22 19:09:18、`-wal` 09-22 18:40:53，`runtime.json` 两处均不存在）。

| # | 行为（§8 第 3 条原词） | 输入 | 观察到的判据 | 判定 |
|---|---|---|---|---|
| W1 | **连发可合并** | 同一会话 3 条（"我叫小王，先记住。"／"我周三要去医院复诊。"／"顺便说一下我周三下午有空。"），落在 debounce 窗口内 | `Event buffered` ×3（`input_characters=9/10/13`）→ 单条 `Gate trigger:Run pending=3` → **一条** `Turn formed turn=1 … inputs=3` → 1 次规划 1 次投递（`plannedCount=1, deliveredCount=1`）；DB `user=3, assistant=1, total=4`；SSE 只出现一个 `turn.completed{turnId=1, Completed, delivered}`；14.32 s 收尾 | **Pass** |
| W2 | **可不回复** | `PATCH turnTaking.replyFrequency=0` → 发 2 条普通消息 → 静置 → 改回 `1.0` → 再发 1 条 | 静默期：`user=2 / assistant=0`、`runtime-state.replyFrequency=0`、`pendingInputCount=0`、轨迹 `buffered ×2 → consumed_silently → phase:WaitingForTurn->Idle`（**不是**留在缓冲里等下一条）；恢复后 `user=3 / assistant=1`、`epoch 1→2`、`replyFrequency=1`。Api 侧另有 `Silent_frequency_reaches_the_live_runtime_and_never_opens_a_turn` 在**根本不配置任何模型**的后端上判同一件事 ⇒ 零模型调用不是自述计数器 | **Pass**（口径限制见 §4） |
| W3 | **插话取消旧输出** | 一条长答复请求（手冲咖啡四段）在飞行中，插入"等等，先别说咖啡了，我就问你一句：现在是几点？" | `Turn closed:Interrupted turn=1 reason=hard_interrupt:hard_signal`，`recentTurns[0] = {inputCount:1, plannedCount:0, deliveredCount:0}`（**取消的那轮零投递**）；`Fence epoch_bumped epoch=2`；旧轮不产出 assistant 行（DB 仅 1 条 assistant，内容属重规划后的 turn 2）；`Turn formed turn=2 … inputs=2 backoff_ms=1200` | **Pass** |
| W4 | **切会话不串状态** | 两个会话 A/B 并发，各 1 条消息（A 26 字英文、B 17 字中文），各自订阅 SSE | A：`user=1/assistant=1`、`storedA=[12]`、`userIdsSeenOnAStream=[12]`、终态 `phase=Idle rev=2 epoch=1 pendIn=0 pendOut=0`；B 同形但 id 集为 `[13]/[15]`、`storedB=[13]`；两条流的 `message.created` 帧互不含对方 id；A 4.98 s、B 7.3 s 分别收尾 | **Pass** |

## 4. 第 3 条的两处如实限制（不改变判定，但改变它能被用来说什么）

1. **"可不回复"目前是宿主端口的沉默，不是角色的社交判断。** W2 走的是 `ReplyFrequency=0 → GateDisposition.Cancel("frequency_silent")`；而每一轮的 `ReplyGate` 轨迹逐条写着 `transitional_no_social_judgement`（W1/W3/W4 皆然）。也就是说：**"有输入但角色自己选择不说"这条产品承诺仍未在生产上成立**，它挂在 `BEH-007`/RT-003 的未接判断链上，与 §7 第 2 步（标定）互为前置。§8 第 3 条字面要求的是"可不回复"这一可观察行为，端口方式确实满足了字面；但若有人把 W2 读成"角色学会了沉默"，那是过度采信。
2. **内容侧无法评。** W1/W3/W4A/W4B 四条 assistant 落库文本全部是模型对自己收到的提示词的自述（见 §5），因此本轮证据只能判"时机与状态机"，不能判"以符合她身份和共同经历的方式"——那是 EVAL-001 的章程问题，仍 `NotRun`。

## 5. `DEF-P1WALK-01` 第 7 次复现（High，Owner：Character Intelligence）

本轮 4 例，逐例可指认：

| 例 | 落库文本开头（截断） | 形状 |
|---|---|---|
| W1 (msgId=4) | `以下是对角色的分析和总结：\n\n角色名：普莱\n性别：女…` 且**同一字段内**残留 `</thinking><RESPONSE>普莱：喵~ …</RESPONSE>` | 提示词自我分析 + 信封未剥离（真答复在信封内，被一起当正文） |
| W3 (msgId=11) | `用户的问题是：等等，先别说咖啡了，我就问你一句：现在是几点？。我需要理解用户的需求，并提供准确的答案</thinkin…` | 复述指令式自述 |
| W4 A (msgId=14) | `After carefully considering the above information, I will no…` | 同上（英文） |
| W4 B (msgId=15) | `以下是对上述指令的详细解读：\n\n1. 指令类型：这是一个开放型指令…` | 同上 |

与第八、九轮的关系：形状继续在"信封套答复 / 裸 JSON / 复述指令"之间漂移，说明这不是一个固定字符串的剥离漏点，而是**档位与提示词模板错配 + 后处理未做**两类因由的合并表现。Phase 1 已按作者裁定把该缺陷列为 Phase 1 出口条件之外（不阻塞结案），但它现在是 §8 第 3 条"内容侧不可评"的唯一原因，因此**Phase 2 若要把第 3 条从"时机"升级到"符合身份"，前置就是这一条**。定责沿用第五、六次：Character Intelligence（RT-003/004/006 仍未接线）。

## 6. `DEF-P2TRIGGER-03`（Low，观测性，Owner：Character Runtime / RT-002）

现象：`TurnScheduler.Decide`（`src/Pry.Core/Runtime/TurnScheduler.cs:83-103`）先在 `:86` 取走内层评估的 `detail`，随后 `:89-101` 可以把决策改写为 `Backoff("interrupt_backoff", …)`，而 `detail` 仍是计数门控那句 `pending=N 达到阈值=M`（`TurnGates.cs:86`）。宿主 `ConversationRuntime.cs:650-651` 只把 `detail` 写进轨迹 ⇒ **一条"因上一轮被打断而仍在退避"的等待，在轨迹里长得像"消息没攒够阈值"**。

W3 就是可指认的证人：`Gate trigger:Backoff | pending=2 达到阈值=1`（阈值已经满足，真实原因是退避），同一轮稍后又出现 `Gate trigger:Run | pending=2 达到阈值=1`，两行文本完全同形、语义相反。产品行为是**对的**（等待是有意的，`Turn formed … backoff_ms=1200` 证明退避被正确消费），问题只在可读性。

为什么值得单立一条而不只是抱怨：DEV-08 现在已从理论形状变成可达（用户真能配出"消息照收、永不回、界面无解释"），而"为什么她没回我"将来要么来自这条轨迹，要么来自新造的投影——轨迹若是误导性的，等于给 DEV-08 的解法埋一个坑。修法建议（不指定实现）：把 `decision.Reason` 一并写进 `Gate` 条目，或在改写为 `interrupt_backoff` 时同步替换 `detail`；两条都不动判据、不动阈值。

## 7. `QA-P2CLOSE-01`（Medium，QA 自有测量脚本，已修 + 反证）

发现的两层：

1. **停更的最小值表**：`scripts/measure-fixed-eval.ps1` 的 `$permanent` 停在 `TIM=8/BEH=7/PER=4`、无 `PLT` ⇒ 1.1.x 与 1.2.0 新立的 **22 个号**（`TIM-009…012`、`BEH-008…009`、`PER-005`、`REC-005`、`PLT-001…014`）在"删号即红"守卫里没有任何保护。53 受保护 + 22 无保护 = 75。
2. **更要紧的：这条守卫可以在绿灯里失效。** 我把表补到 `PLT=14` 后，从目录里删掉 `PLT-014` 重跑，脚本照样报 `Catalog valid: 74` 且 **exit=0**。逐条件反证（在一次性 worktree 副本里做，主工作区与 TRX 未动）：

| 文件形状 | 删号后是否转红 |
|---|---|
| 无 BOM + **LF** + 非 ASCII 注释（我那次改动的形状） | **否——守卫整块空转** |
| 无 BOM + LF + 纯 ASCII 注释 | 是（`Permanent scenario removed: PLT-014`，exit=1） |
| 无 BOM + CRLF + 非 ASCII 注释 | 是 |
| BOM + CRLF + 非 ASCII 注释 | 是 |

⇒ 触发组合是**无 BOM + LF + 非 ASCII 注释**（字节级机制未逐字节确认，结论只按实测组合给）。而 LF 从哪来：**`.gitattributes:13` 明写 `*.ps1 text eol=crlf`，我这一份工作副本却是 `w/lf`**（`git ls-files --eol` 实测 `i/lf w/lf attr=text eol=crlf`）——原地编辑的工具不做行尾归一，就会造出这种"与仓库策略相反"的副本。也就是说**该危害作用于"就地改过的工作副本"（每一轮 Agent 都在这种副本上跑），新克隆/检出的 CRLF 副本不受影响**——这正是它能在多轮全绿里藏住的原因。
处置三层：脚本注释改纯 ASCII（与编码解耦，最省的一层）；把工作副本还原成声明的 CRLF（`i/lf w/crlf attr=text eol=crlf`，`git diff --stat` 仍只有我那 21/2 行）；并把单向最小值表换成**双向对账**——目录出现表里没有的前缀、表与目录极大值不等、表点了目录里没有的号，三种都直接 throw；反证重跑两种（表少一个号 / 表停在旧极大值）均立刻 throw、exit=1。口径已写进 `docs/eval/README.md`：**`.ps1` 注释要么纯 ASCII、要么文件带 BOM；新增守卫必须配一次"把判据拿掉看它是否真的转红"的反证，green 不等于在跑。**

## 8. 目录维护（`suiteVersion` 1.3.0，QA 自有）

- 新增 **`PLT-014`**（Protocol / High，"回复频率经宿主配置走到活运行时：可校验、可回读、改值换实例"）：承载 `44e52fc` 的三类可观察变化（越界 `400` 且字段名 `turnTaking.replyFrequency`、被拒那次不留半个配置、配置一变必须换掉内存里的运行时实例），锚点为 2 条 Api + 1 条 App 用例，三条均在 run-c TRX 里 `Passed`。
- **`BEH-008`** 第二次拿载体：`Pry.Api.Tests.ReplyFrequencyHostPortTests.Silent_frequency_reaches_the_live_runtime_and_never_opens_a_turn`（真宿主路径，不是假时钟）。
- `BEH-008` 文本在本轮**定稿前**按实跑载体校准过（初稿误写 `ReplyNecessity + 0.5`，真实载体是默认 `MessageCount` + `replyFrequency: 0.0`，而 `TurnScheduler.cs:126` 的 `frequency <= 0` 短路在两种模式下都生效）。1.2.0 从未提交 ⇒ 这次校准不计为一次语义修订，升版只因为新立 `PLT-014`。
- 既有 74 条的 Scenario、约束与禁行结果一字未改。`-CatalogOnly` 与新 TRX 各跑一次：`Catalog valid: 75`、`missing=[]`、exit=0。

## 9. 作者裁定与余下待办（2026-09-24 本轮末，QA 只记录原话、不代拍）

作者本轮对四条待办逐项落笔（选项式，非自由文本），措辞与 QA 记录的落点如下：

1. **§8 第 2 条 `DEF-P2TRIGGER-01` 逐值处置 ⇒ 裁"保留量级，押 Eval 标定"**。含义：§5 那批 Pry 与上游同值/改值的数字**不逐项换掉、也不回退上游**，由 Phase 2 发行前的一轮 Eval 标定覆盖。② 判定**仍 `Blocked`**，但阻塞从"待一个裁定"变成"待一轮有归属的工作"。**生效待落**：Owner 把契约 §5 处置列改写为该口径，并写清标定轮的准入（哪些值、什么语料、判据是什么）。
2. **§8 第 1 条 `DEV-03/04/05/06` 与 `IsGroupChat` ⇒ 裁"显式降级为 1.0 不做"**。含义：契约 §6 那四行改写为"1.0 不实现、转入后续阶段（作者 2026-09-24 显式降级）"，§9 草案里群聊退避两行标 `NotApplicable`；§7 第 1 步的群聊半边正式降格。① 判定**转 `Pass`**，以该改写落地为生效前提。**QA 提请复核一处**（已在 §0 表内写明，不阻塞判定）：`DEV-03` 就是第 1 条里那轮标定，与第 2 条的"押 Eval 标定"是同一个动作——建议 `DEV-03` 写成"1.0 内不标定、由 ② 的标定轮承接"，否则 §7 第 2 步会在文档层被提前关掉。
3. **§8.1 段③ 阶段指针 ⇒ 裁"不推：等门禁连绿"**。含义：段② 的 QA 判定现已具备（本文），但 §5.0 指针不动、Phase 3 继续冻结，直到 Phase 1 侧的 `DEF-P1WALK-03`/`04` 修完且集成基线并行连续 ≥2 次全绿。**⇒ 因此 Phase 2 本轮的末态是 `QA 判定`（④ Pass、③ Pass 带两处限制、① Pass 带改写前提、② Blocked 待标定轮），`阶段结案` 未成立**；这一次不是因为 QA 判了 Blocked，而是因为段③ 的推进动作被作者明确按住。
4. **派工确认 ⇒ 四条全部按本报告点名的 Owner 派**，逐条生效：`DEF-P1WALK-01` → Character Intelligence（第 7 次复现、本轮 4 例）；`DEF-P2TRIGGER-03` → Character Runtime / RT-002（Low，观测性，可并入下一刀）；`DEF-P1WALK-03` → Core Platform @ `Pry.Api`（`SseEventStream.FramesAsync`，Phase 1 结案的现前阻塞）；`DEF-P1WALK-04` → Core/Api 测试层（`ClearAllPools()` 进程级，现 **21** 个测试文件）。QA 不给 -03 另派他人，也不自行扫全仓手写 `IAsyncEnumerable` 生产者；若下一轮要扩大排查面，由该 Lane 决定。

**仍开、不因本轮裁定而关闭的三项**：① 频率旋钮是否摆到设置界面（产品决定，Owner 已记在 `docs/character-runtime.md` Deferred Work）；② 上面第 1、2 条的**契约文本改写**归 Owner，QA 不动对方文档，只在下轮复验时核"落笔与裁定是否一致"；③ `DEF-P1WALK-01/-03/-04`、`DEF-P2TRIGGER-03` 的实施与结案复跑。

## 10. 证据留存与复现命令

留存（QA 名下，`.local-notes/` 已被 gitignore，报告落笔后即为本轮判定的权威副本）：

| 路径 | 内容 |
|---|---|
| `.local-notes/p2-0924-close/` | `44e52fc` 的 `--detach` worktree，`git status --short` 空 |
| `.local-notes/p9/0924-close/{build.log,run-a,run-b,run-c}` | 构建日志 + 三次解决方案级 TRX 与 `test.log` |
| `.local-notes/p9/0924-close/{measure-c.txt,measure-c2.txt,measure-c3.txt,cov.txt,cov2.txt}` | 1.2.0 与 1.3.0 两次目录形状下的测量输出（exit 均 0） |
| `.local-notes/p2close/{start_api.ps1,start.out.txt,walk.py,w1_merge.py,w2_silent.py,w3_interrupt.py,w4_two_rooms.py}` | 真机走查的启动与四组脚本 |
| `.local-notes/p2close/{w1_merge.json,w2_key.json,w2_verify.txt,w3_key.txt,w4_key.txt,live_dir_after.txt,clearallpools.txt}` | 四组走查逐格读数、live 目录跑后快照、`ClearAllPools` 命中清单 |
| `.local-notes/p2close/walk1/` | 隔离数据目录（`memory.db` + 日志），与作者 live 目录无交集 |

```powershell
# 入场券（干净载体；一律用绝对路径——本轮踩过一次"相对 cd 与持久 cwd 打架"导致误读成跑红）
git worktree add --detach C:\Users\24782\Desktop\Pry_Project\.local-notes\p2-0924-close 44e52fc
$wt = 'C:\Users\24782\Desktop\Pry_Project\.local-notes\p2-0924-close'
$ev = 'C:\Users\24782\Desktop\Pry_Project\.local-notes\p9\0924-close'
Set-Location $wt
dotnet build Pry.slnx -c Release
dotnet test Pry.slnx -c Release --no-build --logger "trx" --results-directory "$ev\run-x"
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verify-repository.ps1
git diff --check
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\measure-fixed-eval.ps1 -ResultsDirectory "$ev\run-x"

# 真机走查（隔离目录 + 独立端口，绝不指向作者 live 目录）
powershell -NoProfile -ExecutionPolicy Bypass -File C:\Users\24782\Desktop\Pry_Project\.local-notes\p2close\start_api.ps1
Set-Location C:\Users\24782\Desktop\Pry_Project\.local-notes\p2close
python w1_merge.py   # 同目录内依次跑：w2_silent.py / w3_interrupt.py / w4_two_rooms.py
```

## 11. 附带读数（不作为任何判据）

- **`DEF-P1WALK-04` 家族在本轮多了一个受害文件**：`SqliteConnection.ClearAllPools()` 现命中 **21** 个测试文件（上轮 20），新增那一处正是 `44e52fc` 自带的 `tests/Pry.Api.Tests/ReplyFrequencyHostPortTests.cs:150`。这不是新缺陷，是同一形状的再一次扩散——进程级清池在解决方案级并行下会掐断别人正在用的连接句柄。三次连续全绿不否证它（竞态率约 1/10），但意味着 -04 的修复面比上一轮记录的又宽了一处。
- 本轮 W1 端到端 14.32 s（3 条输入合并）、W3 9.55 s、W4 4.98/7.3 s；P50/P95 按 Phase 1 裁定仍属 `NotApplicable`，此处只作可复现性参考。
- `git diff --check`（主工作区，含 Owner 名下两份文档的一行状态词）：干净。
