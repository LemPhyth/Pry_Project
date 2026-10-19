# 工单 `RT-005-MANAGE` · 让他看得见"她还记得但不再引用"的那些事（第 2 层 2-C 记忆管理显式入口）

> 状态：**已派工·待 Owner 认领（守门人 2026-10-16 按作者"派工单，然后告诉我应该让谁开工"写成）⇒ 2026-10-19 已认领·进行中（Memory & Context；作者当日只说一句「按照守门人的派单开工」，所指＝§0 那一格"现在缺的是有人认领"）**。Owner＝**Memory & Context**（`RT-005`／`MEM-005` 实现 Owner ＋ UI）；判定归 QA（从可指认提交复采）；第 2 层状态词与 `PROJECT_PLAN.md` §5.0 指针不由本角色推进。
>
> **2026-10-19（同日更晚）已入库**：本刀代码＋判据＋随刀文档形成一次可审查提交（分支 `codex/ui-main-window-redesign`，本地，未 push／未 tag／`main` 未动）。载体读数 **904／0／2**（Client 93／App 226／Api 188／Core 397 通过＋2 环境跳过；比在册 886 多的 18 恰为本刀新增判据 Core＋10／Api＋3／App＋5），**取自本角色主工作树、非干净 `--detach` 载体** ⇒ 权威复采与逐条独立反证归 **QA 从本提交建干净载体**，Owner 不自封基线。404／409 依 §1 定案＝契约赢（不存在／他角色 `404`，非孤立态与撞指纹各 `409`），`MemoryManagementEndpointTests` 的 `NotFound` 断言不必改回。随刀已写：`docs/api-v1.md`（confirm-keep／可选布尔／`status` 可见性）、`docs/memory-message-deletion-separation.md` §8 的 N8·N11 落地更正、`CHANGELOG.md` 本刀那一条。**未随本刀写**：`docs/character-learning-safety.md`（另有 lane 的 `character-learning-safety.patch` 在途，不代改）；`docs/full-scope-sprint.md` §3 第 2 层状态、`PROJECT_PLAN.md` §4.2 `RT-005` 行与 §5.0 指针、以及第 2 层"已落地"结案词仍**由作者落笔**，本刀只交实现与自证。N11 如实：数据层作用域缺陷已拆、HTTP 触达面待 RT-001 实例 id。
>
> **载体重基（2026-10-19 认领当场核）**：开工日线尾＝**`88eff2a`**，不是本单写成时的 `c608220`；两者之间隔着 `DEF-P3SEAM-04` 那一刀（6 文件，不碰记忆面，认领时 `git show --numstat` 与 `git status --short -- src tests` 双面点过名）。在册读数自此＝QA 从 `88eff2a` 四次复采的 **886／0／2**（Client 93／App 221／Api 185／Core 387＋2 环境跳过，`eval-QA-P3CLOSE-1019`）；下面那句"`c608220` 上守门人自跑＝876／0／2"自此仅作史保留。**照 §5 的规矩：本刀一切读数在自己载体上当场重取，不许在 886 上做加法。**
>
> 载体基线＝当日线尾 **`c608220`**（本工单写成时的 tip，`main` 已跟同一把）。**开工与出判都必须在自己的干净 `--detach` 载体上当场重取读数，不许做加法**：`c608220` 上守门人自跑＝876／0／2（Client 93／App 221／Api 179／Core 383＋2 环境跳过），**QA 尚未从该 SHA 复采**；在册经独立复采的读数仍停在 854（App 214／Api 164 那一份，出自 `dd91d2c`）——**同数不同项，引用必须写全四项**。
>
> **2026-10-19 14:2x–14:27 三条当场实测，都写在动手之前（后来者不必再挖）**：
> ① **契约文本早就在这张树的 `docs/api-v1.md` 里，且与本单 §2.2 不同**——`docs/api-v1.md:8`（未提交面，10-15 18:47 写入）已把 `POST /api/v1/memories/{id}/confirm-keep` 定成「`confidence` 抬到 `.7`、`origin='manual'`、不重建证据、成功 `200`＋更新后的 `MemoryDetailsResponse`，错误分三种：`404 resource_not_found`／`409 memory_not_orphaned`／`409 memory_fingerprint_taken`」，同一段还写了 `DELETE /api/v1/conversations/{id}?forgetExclusiveMemories=false`。**本单 §2.2 那句"不存在的 id 一律 409"与那段契约的 `404` 直接冲突**，且 §2.2 要的"作用域不符自成一个 code"在契约里没有。**这不是实现细节能自己吞掉**：按 §2.2 做就和已写死的对外契约不一致，按契约做就有一条完成标准不照字面成立。谁动手都要先把这一格挑明，别让它静默选边。**10-19 14:2x 实测：那一把已按契约写成 `404`**（`tests/Pry.Api.Tests/MemoryManagementEndpointTests.cs` 新那条同时断言"不存在的 id"和"别的角色的 id"都返回 `NotFound`）。**⇒ 本角色此处一条错账，就地更正**：我把这一格重新当成"等落笔的开放冲突"写了两遍，而**下面紧接的那句 10-19 最晚裁定早就把它判成"契约赢、按 `404` 定案"了**（`docs/api-v1.md` 是受跟踪的对外契约，改契约才需要单独明令，改工单不需要；完成标准 2 已按此更正）。所以那一把的 `NotFound` 断言**不必改回**，本刀也不必在这格加任何东西。教训与 §9 同日那条同源：动手前要把**工单当前的字面**读完整，不能拿自己上一轮的读法当现状。**落笔（2026-10-19 最晚）：契约赢，按 `404` 口径定案。** 依据＝作者当日原话「我不想思考了，你就继续给我派工单，该提交的提交吧，早点做完之后再回来大修」⇒ 守门人代拍推荐项；决定性理由有二：① `docs/api-v1.md` 是受跟踪的对外契约，改契约要单独明令，改工单不要；② 与既有 `DELETE /api/v1/memories/{id}` 那条"不泄漏其他角色是否拥有该 id"同构，"这条不存在"与"这条不该确认"分码才让界面不必靠文案区分。本单 §2 完成标准第 2 条已就地照此更正；那一把（`MemoryManagementEndpointTests` 的 `NotFound` 断言）不必改回。
>
> **② 认领后两分钟，这张单的后端半边被另一条 lane 在主工作树上写下来了**（14:22–14:23，7 个源文件／＋81 行：`SqliteMemoryRepository.cs` 新 `ConfirmKeepAsync`、`MemoryContracts.cs` 的 `ConfirmKeepOutcome`、`MemoriesController.cs` 的 `confirm-keep` 路由、`MemoryApplicationService.cs`、`ApiExceptionMiddleware.cs`、`ApiContracts.cs`、`PryBackendClient.cs`；14:27 再读，`tests/**` 也不再是零改动——Core 侧新增 6 条 confirm-keep 用例（＋86 行）、Api 侧 1 条端点用例（＋66→＋71 行）；14:30 三度读，`SqliteMemoryRepository.cs` mtime 14:22、Api 那条测试 mtime 14:30＝与当时时钟同分钟 ⇒ **那一把写的是后端＋测试，且此刻仍在实时落盘**）。界面（`PryMessengerMemoryWindow.cs`，mtime 仍 09-14）一行未动，会话删除那个可选布尔在 `src/` 实测零命中 ⇒ 单没做完，但**写者已不是单一写者**。本角色按 `AGENTS.md` 第 1 条就地停手，冲突原样上报，不覆盖、不回退、不顺手提交别人的半把刀。
>
> **③ 自此本刀的范围（等作者一句归谁之后才动）**＝形状 A 里**仍然空着的三格**：(a) 记忆页从 `GetMemoriesAsync`（遗留 `status='active'` SQL）换到 `GetMemoryDetailsAsync`，把 `orphaned`／`archived` 分组显示成"她正在用的"与"依据已被你删掉的"，并给后者接上"确认保留"；(b) 会话删除那个可选布尔（契约 `docs/api-v1.md` 已写成 `forgetExclusiveMemories=false`，`src/` 实测零命中 ⇒ **文档先于代码**，动手那一方要么补码要么改文档，不许放着）；(c) N8 级联的判据与 N11 的界面那半。后端那半**不由本刀重写**，等那一把成为可指认提交并由 QA 复采后再接。

>
> 前置已满足：2-A（`bfcee82`）＋ 2-B（`c4e0206`，随 `eb01acf` 进主线）落地了 `orphaned` 中间态与 I1–I8；设计稿 `docs/memory-message-deletion-separation.md:247-248` 明写 **N8、N11 与 §6 末的 `scope_key='character:'||$c` 作用域缺陷"归 2-C（`RT-005`）"**。本工单就是那一刀。

## 0. 作者读这一段就够

- **先更正我上一版写错的一句话**（这句在只读那一页上，必须当场更正）：我写过"你今天还没有任何按钮能主动让她忘掉一项事实"。**这句不对。**界面上确实有一个删除按钮——聊天窗口左边"记忆"那一栏（`Ctrl+3`）→ 选中一条 → 右下角"删除"，按下去她就真的忘掉，而且**忘掉之后她再也学不回来**（同一句话她以后再听多少遍都不会重新变成事实——除非你亲手把它再打进记忆库，那算你新给的一条）。所以这一刀不是"补一个删除按钮"。
- **今天真正做不到的是另外三件事**：① **看不见"她已经没有依据的那几件"**——你删掉一条聊天记录之后，那条聊天里她学到的事实会转进一个"证据已删除"的中间状态，它不再被她引用，但**管理界面只显示"还在用"的那些，所以那一类在她的记忆库里今天对你完全隐形**；你既不知道她还记得，也没法把它恢复回去。② **没有"这个我记得，继续用"的入口**——把那条隐形的事实重新启用，需要一个人点的按钮，而这个按钮还不存在。③ **一键就永久生效**：聊天窗口那个"删除"没有确认步骤（旧外壳里那个管理窗是有确认的，但你每天开的是新界面）。这是一次不可逆操作，点错一次就再也教不回来。
- **做完后能做到**：记忆那一栏能分开看"她正在用的"和"依据已被你删掉的"；后者你可以一键重新启用（不重建证据，只是承认它、继续引用），也可以真的删掉；删除前会先问你一句；删掉整个会话时可以选"顺便只把在这个会话里学到的事实一起忘掉"（默认不勾）。
- **仍然做不到（别当成解决了）**：语义召回通道的阈值仍没标定——**"先接线后标定"那道前置门（`2-D`）一字不解除**，本刀不许把任何未标定阈值带进生产；她学你的**说话方式**那条线（第 1 层）与本刀无关；**没有可安装版本**（没 tag、没推远端）；本刀不碰口癖／行为模式的管理面，也不顺手修那条"省略号被记成一次成功对话"的老缺陷。
- **怎么算完成（一句人话）**：删掉一条唯一的聊天记录 → 记忆那一栏里还能看见那条事实，并标着"依据已删除" → 点"确认保留" → 她重新把这件事当回事（不用重启、不用重新讲一遍） → 关掉程序再打开，它还在，而且引用来源仍是空的；反过来，你明确删掉过的那一条，无论怎么撤销聊天记录或删除重放，都不会偷偷回来。

## 1. 今天的事实（逐条可指认，2026-10-16 实读）

| 环节 | 位置 | 现状 |
|---|---|---|
| 他每天开的窗口是哪个 | `src/Pry.App/App.axaml.cs:78`（`_mainWindow = new PryMessengerWindow(api)`）；记忆入口 `src/Pry.App/PryMessengerWindow.axaml:37`（`MemoriesNavButton`／`Ctrl+3`）→ `.axaml.cs:984-988` 装 `PryMessengerMemoryWindow` | 现行外壳＝聊天窗口；**旧 `MainWindow` 那套管理面不是启动路径**（`MainWindow.axaml.cs:1415,1418,1492` 另有按钮） |
| 删除按钮确实存在，但**没有确认** | `src/Pry.App/PryMessengerMemoryWindow.cs:28`（`Button("删除", DeleteAsync)`）、`:79-84`（直接 `DeleteMemoryAsync` → 状态写"已删除"） | 对照：旧外壳的 `src/Pry.App/Services/MemoryManagerDialog.cs:94-101` **有** `confirm(window,"删除长期记忆","确定永久删除这条记忆吗？")` ⇒ 同一动作两套界面安全级别不同，且他用的那套是弱的那套 |
| 删除为什么是永久的 | `src/Pry.Core/Memory/SqliteMemoryRepository.cs:117-122` 触发器 `memory_deletion_fence`（任何 `DELETE FROM memories` 都写 `memory_tombstones` ＋抬 `epoch`／`forget_before` ＋废弃在途任务）；`:123-124` `memory_prevent_undo_resurrection`；`:514-525` 学习/写回侧按 `forget_before` 水位与**同指纹墓碑**跳过 | 不是"删了记录"，是"这条事实此后**自动**学不回来"——`:523-525` 那道同指纹门只挡学习/写回侧，**手工重新录入同样文字是用户的新操作、仍然允许**（`docs/api-v1.md:7` 原话："手工重新创建是用户的新操作"）。`docs/api-v1.md:7` 与 `docs/memory-message-deletion-separation.md:198` 都按 D1 语义写着"需在 UI 明确二次确认"——**这条要求今天没落地** |
| 管理面走的是哪条读路径 | `PryMessengerMemoryWindow.cs:57` → `src/Pry.Client/PryBackendClient.cs:140` `GetMemoriesAsync` → `GET api/v1/memories`（`src/Pry.Api/Controllers/MemoriesController.cs:15`）→ `src/Pry.Api/Services/MemoryApplicationService.cs:39-47` → `src/Pry.Core/Memory/MemoryDatabase.cs:564` | 那条 SQL 带 **`status='active'`** ⇒ `orphaned` 与迁移时被标成 `archived` 的重复行（`SqliteMemoryRepository.cs:113-115`）在管理面**结构性不可见**；`docs/api-v1.md:8` 自己就写着"列表只显示 active" |
| 已有的"看得见"那半边 | `MemoriesController.cs:11-12` `GET api/v1/memories/details` → `MemoryApplicationService.cs:10-17`（返回 `Status`／`Confidence`／`Origin`／`Revision`／`TopicKey`／`Evidence{messageId,conversationId,excerpt}`）；仓储侧 `SqliteMemoryRepository.cs:174-183` **不过滤状态、作用域感知**；DTO `src/Pry.Contracts/MemoryManagementContracts.cs:5`；客户端方法 `PryBackendClient.cs:165-166` `GetMemoryDetailsAsync` | 读端点、契约类型、客户端方法**全都在**，唯独 **`src/Pry.App` 里零调用者**（当场 grep 只命中 Client 与 `tests/Pry.Client.Tests/MemoryManagementClientTests.cs:18`）⇒ 缺的是接线，不是新造一套读口 |
| 对外列表 DTO 没有状态字段 | `src/Pry.Contracts/CommonContracts.cs:141-143` `MemoryRecord(Id, CharacterId, Kind, Summary, Tags, Importance, SourceMessageId, CreatedAt, UpdatedAt, LastUsedAt)` | 设计稿 §6（`docs/memory-message-deletion-separation.md:196`）要求"`MemoryRecord` 需携带 `status`／`confidence`／`origin`"——**现有 DTO 一条都没有**；两条路线见 §4 |
| "确认保留"端点 | `src/Pry.Api/Controllers/MemoriesController.cs` 全文 6 个动作（details／clear／list／create／update／delete） | **不存在**。`docs/memory-message-deletion-separation.md:197` 已定形状：`orphaned → active`、`confidence` 上限 `.7`、`origin='manual'`、**不重建证据**、非孤立态返回 **409** 不静默成功 |
| 确认保留会撞的两个既有机关 | ① 缓存失效只挂在正文列上：触发器 `memory_manual_edit_fence` 是 `AFTER UPDATE OF summary,kind,tags,importance`（`SqliteMemoryRepository.cs:125-129`）⇒ **只改 `status`／`confidence` 不抬 `epoch`**；② 部分唯一索引 `ux_memory_fingerprint ON memories(scope_key,fingerprint) WHERE status='active'`（`:115`）⇒ 把一个孤立行抬回 `active` 时，若同指纹已有 active 行，是一条 **SqliteException 2067**（既有形状见 `MemoryApplicationService.cs:80-83` 的 `ResourceConflictException`） | 两条都是"实现时最容易当成没事"的形状：①不处理 ⇒ 点完按钮她并不会真的重新引用（要等缓存自然过期或重启）；②不处理 ⇒ 端点直接 500。**必须各自有一条判据点名。** |
| 检索侧的门 | `src/Pry.Core/Memory/MemoryQueryService.cs:22`（`Status=="active" && Confidence>=.5 && ScopeKey==query.Scope.Key`）；`SqliteMemoryRepository.cs:348-350`（无存活佐证 ⇒ `orphaned`＋`confidence=.4`） | I2 的"双重闸门"（状态＋置信地板）是设计，不是障碍：**不许为了让恢复后立刻命中而改这两处**。`.4 < .5 ≤ 恢复后上限 .7` 的算术本来就通 |
| 会话删除没有级联选项 | `src/Pry.Api/Controllers/ConversationsController.cs:19` `Delete(string id)` 只有路由参数；`docs/api-v1.md:176` 写"当前为物理删除…客户端应二次确认" | 设计稿 §6 第 4 行要的"同时删除仅在本会话学到的记忆"复选框＝**可选布尔参数＋DTO 验证**，今天零；对应判据 N8（`:227`） |
| 作用域缺陷今天能不能真的咬到人 | `src/Pry.Core/Memory/MemoryContracts.cs:6-8`（`ForCharacter` ⇒ `character:{id}`，注释："An instance scope must never be derived from a character id alone"）；`MemoryDatabase.cs:564,579,605` 三处硬编码 `scope_key='character:'\|\|$c`；`src/` 内**零** `instance:` 作用域生产者（当场 grep），只有测试在用（`MemoryDeletionSeparationTests.cs:15`、`MemoryContextTests.cs:14,144`） | ⇒ **诚实的严重度**：`orphaned` 之后"看不见"这个缺陷**今天真的存在**（legacy list 的状态过滤），而"实例作用域看不见"（N11）目前是**潜在**的——一旦按 `MemoryContracts.cs:5` 那句注释引入实例作用域，legacy 三处就同时看不见也删不掉。本刀要求"管理面统一改走作用域感知的仓储"，不许把 legacy 三处的硬编码再复制第四遍 |
| 既有判据资产 | `tests/Pry.Core.Tests/MemoryDeletionSeparationTests.cs` 13 例（N1–N7、N9、N10 ＋ `N5b`/`N5c`/`N6b`/`N12`）；`CoreTests.cs:230` 的 `orphaned` 断言；`tests/Pry.Api.Tests/MemoryManagementEndpointTests.cs:32,36,48-49`；`tests/Pry.Client.Tests/MemoryManagementClientTests.cs:18-20` | N8、N11 **确实没有实现也没有用例**（设计稿 `:247` 自陈）⇒ 这两条就是"今天必红"的现成锚点，不需要新造缺陷故事 |

## 2. 完成标准（每条都要能先红一次）

1. **孤立记忆在管理面可见（今天必红）**：真 Api＋真 SQLite＋真删除路径——删掉某条记忆唯一的证据消息 ⇒ 管理面查询路径返回该行、带"依据已删除"标记，且**不因为 `status<>'active'` 被 SQL 过滤掉**。反证＝继续走 `MemoryDatabase.cs:564` 那条带 `status='active'` 的 legacy 读路径。
2. **"确认保留"端点按 §6 形状办事**：`orphaned → active`；`confidence` **不超过 `.7`**；`origin='manual'`；**不重建任何 `memory_evidence` 行**（断言证据条数不变）；错误语义按契约分码（10-19 最晚就地更正，契约赢）——不存在的 id／别人的角色 → **404 `resource_not_found`**（不泄漏其他角色是否拥有该 id），非孤立态（`active`／`superseded`／`archived`）→ **409 `memory_not_orphaned`**，同指纹已被占用 → **409 `memory_fingerprint_taken`**；三种都不许静默成功，也不许冒 500。
3. **确认保留立刻生效，不等重启**：恢复后该事实经 `MemoryQueryService` 那一面重新可召回（`.7 ≥ .5` 的算术成立），且**缓存/修订号路径按既有 `epoch` 机制失效**——要么扩展 `memory_manual_edit_fence` 的列清单，要么显式抬 `epoch`，**不许**改 `MemoryQueryService.cs:22` 的门来凑命中。
4. **同指纹已存在时不炸**：`active` 行已占用同指纹时确认保留另一条孤立行 ⇒ 走明确错误语义（对照 `MemoryApplicationService.cs:80-83`），不得冒 500，不得悄悄把两条并成一条（合并是另一个决定，不在本刀）。
5. **不可逆动作先问一句**：聊天窗口记忆页的"删除"必须经确认（沿用旧外壳 `MemoryManagerDialog.cs:96-97` 那个 `confirm` 形状即可），文案要说清"此后她不会再重新学到这句话"这一层，**不许**写成"删除记录"这种可逆暗示。
6. **N8 级联删除（今天必红）**：会话删除端点加**可选布尔**参数，**默认不勾**；勾上时只删"证据全部来自该会话"的记忆（走 D1 写墓碑），跨会话仍有佐证的**一条都不许消失**；不勾时行为与本刀开工前逐字节相同（全部转 `orphaned`、零墓碑，即既有 N7 的形状）。
7. **N11 作用域可达（今天必红）**：管理面的查询／确认保留／删除三条路径统一作用域感知——改走 `IMemoryRepository`（`SqliteMemoryRepository.cs:174-183` 已不过滤状态、按 `scope_key` 走），或让 legacy 端点显式接受 `scopeKey`。判据用实例作用域（`instance:*`）造一条孤立记忆，断言管理面查询路径**取得到、也确认得了**。
8. **撤销与显式删除的边界不破**：显式删过的记忆，在任何"撤销删除／重放旧轮次／确认保留"路径之后**仍然为空**（既有 `memory_prevent_undo_resurrection` 触发器与 N6 的对照不许弱化）；确认保留**只作用于孤立态**，不得成为第二套"复活已删记忆"的通道。
9. **契约与文档先行**（`AGENTS.md` 第 5 条）：先改 `docs/api-v1.md`（新端点、`status` 的对外可见性、会话删除那个可选布尔、404/409 分码语义——按 10-19 契约口径），再落 DTO＋服务端验证＋错误语义＋客户端处理＋测试；新错误语义按 code 分类（不存在／非孤立态／同指纹相撞各成一种），**不许都塞 500**。
10. **固定 Eval 对照**：完成时重跑 `scripts/measure-fixed-eval.ps1`，目录仍是 **75 条**永久场景 ⇒ 判据变多不是场景变多。
11. **既有 13 例一字不放宽**：`MemoryDeletionSeparationTests` 全绿，且 `CoreTests.cs:227-230` 那条补过的"`orphaned` 行仍存在"断言不得回退成 `Assert.Empty`（设计稿 §8 写的锚点 `CoreTests.cs:174-179`／`:176-177` 已因该文件行号漂移而过期，**当场以 `:227-230` 为准**）；I1–I8 的语义（尤其 I2 双闸门、I4 证据净化、I7 pending 不抬水位）不因管理面新增动作而重定义。

## 3. 反向约束（随单入库，不得放宽）

1. **不许另造第二套删除语义**（作者 2026-10-12 裁定 B 的原话承接句："不得另造第二套删除语义"，`docs/full-scope-sprint.md:91`）。删消息＝撤证据；删记忆＝D1 写墓碑；确认保留＝**状态迁移**，不是"反删除"。三者不许合并成一个按钮、一个端点或一条 SQL。
2. **不许在消息右键菜单里塞"让她忘掉这个"**（§4 的 C 案）。那正是把 D1／D2 重新粘回一个动作，正面撞裁定 B，且会让"删了聊天记录她还记得"这条已被判据钉住的语义重新变成用户猜。
3. **不许为凑绿灯放宽检索门或置信上限**：`MemoryQueryService.cs:22` 的 `>= .5`、`SqliteMemoryRepository.cs:348-350` 的 `.4`、设计稿的 `.7` 上限，都是既有判据本体。恢复后"立刻命中"要靠 `epoch` 失效，不靠改阈值。
4. **不许夹带 Stage 2**：`ON DELETE SET NULL` 的表重建、回填或"顺手清理既有墓碑"一律禁止（`docs/memory-message-deletion-separation.md:210` 明写：那会改变 D1 的历史保证）。若本刀真需要组件版本再升，按 `:207` 的形状扩 `memory_schema` 并保留"未知版本拒启动"，**不动**全局 `schema_info` v8。
5. **不许把未标定阈值带进生产**：第 2 层 `2-D` 的"先接线后标定"前置门一字不解除（`docs/memory-recall-calibration.md` §7.1、`docs/memory-context-contract.md` §15.3）。本刀只加状态迁移与一个可选布尔，**不加任何新阈值**，也不把语义打分的门挪进管理面。
6. **不与相邻缺陷合并处置**：`DEF-P3SEAM-04`（占位符被记成一次已投递的可学互动）、`FollowUpEvidence` 零生产者、口癖／行为模式的管理面、第 1 层认可入口——**各自有载体与反证**。同刀会把 §5.0 判据 4「读数不得互顶」直接破掉，也会让"这一刀到底结了什么"重新变成一句话糊过去。
7. **单一写者先登记再开工**（`AGENTS.md` 第 3 条）：`MainWindow.axaml.cs` 同一时间只允许一个任务编辑；`PryMessengerWindow.axaml.cs`／`.axaml`／`PryMessengerMemoryWindow.cs`／`Services/MemoryManagerDialog.cs` 是 UI-027 与 `0-5` 两刀的在途／刚入库面 ⇒ 开工前在 `PROJECT_PLAN.md` 台账声明写者，并把落点尽量收在记忆页自己的文件里。
8. **不许把内部刻度端出去**：`tests/Pry.Core.Tests/AffinityNumbersStayInsideTests.cs` 的 `Signals` 词表（`Familiarity`／`Trust`／`Closeness`／`Affinity`…）是三面硬门，本刀不得为"显示记忆强度"在对外契约类型或日志里引入那族名字；给用户看的文案写"依据已删除／她正在用"这种人话，**不要**把 `confidence` 数字当正文（记忆自己的数值只作筛选依据，不作展示卖点）。
9. **分层不许破**：Core 不依赖 Avalonia 或 HTTP；客户端不得新增对 SQLite 的直接依赖（读一切走 `api/v1/memories*`）；不在 Api 里写第二套作用域/指纹推导（`MemoryScope` 与 `Fingerprint` 的归属不变）。
10. **文档文本与代码分刀提交**：本刀的 `docs/api-v1.md` 文本属这条 lane；`CHANGELOG.md` 与 `docs/character-learning-safety.md`／`docs/engineering-pitfalls.md` 里有三条 lane 的在途文本，**不许混进同一把**（10-16 那条分刀边界：混提会抹平归属，也让"新 API 必须有文档"这条在载体上说不清）。

## 4. 建议实施形状（推荐 A；B 只作局部妥协；C 不接受）

- **A（推荐）＝"管理面改走已有的 details 读口 ＋ 一条状态迁移端点"**：
  1. UI：`PryMessengerMemoryWindow` 的列表数据源从 `GetMemoriesAsync`（legacy，只 active）换成已有的 `GetMemoryDetailsAsync`（`PryBackendClient.cs:165-166`，**零改动即可用**），按状态分组或加一个筛选："她正在用的"／"依据已被你删掉的"；后者旁边给一个"确认保留"和一个"删除"，删除走确认。
  2. 端点：`POST api/v1/memories/{id}/confirm-keep?characterId=`（或等价形状），服务端先 `GetMemory` 校验归属与 `status='orphaned'`，抬状态同时写 `origin='manual'`、`confidence=MIN(现值,.7)`，**并让 `epoch` 前进**（扩展 `memory_manual_edit_fence` 的列清单，或在该路径显式 `UPDATE memory_scopes SET epoch=epoch+1`）；同指纹冲突按 §2.4 的错误语义。写路径照 create/update/delete 的先例进 `CompanionOperationCoordinator.EnterForegroundAsync`（对照 `MemoryApplicationService.cs:51,70,89`）。
  3. 作用域：该端点与列表都按 `MemoryScope` 走仓储（`SqliteMemoryRepository.cs:270-277` 已是作用域形状的删除），**legacy `MemoryDatabase.cs:564,579,605` 三处的 `'character:'||$c` 不再新增调用者**；能一次把管理面三条路径都搬到仓储就一次搬完，否则至少让新代码不走 legacy。
  4. 会话级联：`ConversationsController.cs:19` 加可选布尔（默认 `false`），服务端只挑"证据全部来自该会话"的行走 D1；DTO＋验证＋`docs/api-v1.md` 同批。
  5. 判据落点：Core 侧作用域与状态迁移用 `MemoryDeletionSeparationTests.cs:15` 那套 `instance:` 夹具（N11 现成）；Api 侧端点与错误语义扩 `MemoryManagementEndpointTests.cs`；界面那面照 `ExpressionFeedbackSurfaceTests` 的先例做**源码门**（"删除前必须有确认"这条要能被一条测试点名抓住，而不是靠 review 看出来）。
- **B（局部妥协，不作主案）＝给 `MemoryRecord` 加 `status`／`confidence`／`origin`**：这是设计稿 §6 原本写的路线，但 legacy list 仍不带 `evidence`、仍要动一处 SQL 的状态过滤，且会同时改对外契约类型与所有消费者（含 93 例 Client 面）。**只有当 A 无法让"证据已删除"的说明文案落地时**才走 B，且必须同批更新 `docs/api-v1.md` 的兼容说明。
- **C（不接受）＝在聊天消息的右键菜单里加"让她忘掉这件事"**：见 §3.2。

## 5. 门禁与读数（现有纪律一条不省）

- 从**可指认提交**起算的干净 `--detach` worktree（基线＝当日 tip，本单写成时 `c608220`）；混树数字不得充当复采，也不得引用他人轮次数字。
- 每次读数一律 `dotnet build Pry.slnx -c Release -t:Rebuild`（改过码不重编＝跑在旧 DLL 上假绿，同族坑已记两次）。
- `dotnet test --logger trx` 且四个程序集 `LogFileName` 各自唯一、结果目录新建；随后 `scripts/measure-fixed-eval.ps1 -ResultsDirectory <显式新目录>`。**注意一条 10-16 当场踩过的仪器坑**：把 `$变量` 写进 bash 双引号里的 PowerShell 路径会被吞成空串 ⇒ `Resolve-Path` 抛 ItemNotFound、`exit=1`；那是**前置不成立不是判据红**，改显式路径重跑，不许碰阈值或断言。
- 只认逐个 TRX 文件的 `<Counters>`；解决方案级默认命名会跨程序集撞名。跳过项按 `outcome="NotExecuted"` 点名（`c608220` 上那 2 条＝`Configured_sense_voice_model_can_run_offline`、`Configured_llama_server_requires_the_child_environment_key`）。
- 引用总数必须写全四项分项；**不许写"旧总数＋N"**。报"几连绿"要同场列出全部跑法次数与每一个红例名。
- `verify-repository` 与 `git diff --check` 同批跑；跑门禁前先查同级 worktree 有没有他人的 `dotnet` 进程（`docs/engineering-pitfalls.md`）。
- 新判据先**故意红一次**；突变逐个点名转红、`cp`＋`cmp` 字节级还原后复跑。**守卫的扫描面本身也要被钉住**（`QA-TIP1015-01` 的同族教训）：源码门必须带"扫描器看得见坏形状"的正向探针。
- 真机那面：只用隔离数据目录（`Pry__DataDirectory`），**不碰他本机 `%LOCALAPPDATA%\PryCompanion`**；令牌只留长度＋SHA-256 前缀。
- 提交只 stage 自己名下文件；不 `git add -A`、不 `--amend`、不 `--no-verify`、不 push、不打 tag。

## 6. 出判面与随刀登记

| 项 | 归属 |
|---|---|
| Owner 自证（判据＋突变反证＋干净载体读数） | Memory & Context |
| 从可指认提交复跑、出正式报告号 | QA / Evaluation |
| "记忆管理入口已落地"与第 2 层状态词的落笔 | 作者（守门人不代写结案词，§5.0 指针不由 Agent 挪） |

随刀文档（都在该 lane 名下）：`docs/api-v1.md`（新端点＋可选布尔＋`status` 的对外可见性）、`docs/memory-message-deletion-separation.md` §6/§8 里 N8·N11 的落地更正、`docs/character-learning-safety.md`、`CHANGELOG.md` 只写本刀那一条、`docs/full-scope-sprint.md` §3 第 2 层状态、`PROJECT_PLAN.md` §4.2 `RT-005` 行与 §9 交接记录。

## 7. 本刀不解除的门（照旧有效）

- **第 2 层 `2-D`**："先接线后标定"顺序不可颠倒（`docs/full-scope-sprint.md:93`）。本刀不给语义通道加阈值，也不因管理面落地而算作 `2-D` 推进。
- **第 6 层标定轮**仍是 Release 前置；参数标定押到最后一条排序指令不变。
- **第 1 层 `DEF-P4LEARN-01`** 已成可指认提交（`c608220`）但**QA 复采未跑**；本刀不代替那次复采，也不得借用它的读数。
- **`DEF-P3SEAM-04`**、**`QA-TIP1015-01`**（托盘常驻源码门不扫决策本体）、**`0-6`／`DEF-P1WALK-05`**（换后端恢复）、`0-2` 那 6 条判据的**非空转独立复判**——全部并列，不互为前置。
- **可安装版本**仍缺两样：那 7 份文档文本的分刀归位（含本刀要写的 `docs/api-v1.md`）、打包前置的两条（`0-4`）；tag 与 push 等作者明令，本单不含。
- §5.0 阶段指针不因本刀推进。
