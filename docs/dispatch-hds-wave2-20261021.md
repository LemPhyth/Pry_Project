# HDS 第二波派单（2026-10-21 深夜）

派单人：守门人（Architecture / Integration / Release）。坐标系＝`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS 任务表与
`docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md`。作者 2026-10-21 的现行指令逐字：
「我不想思考了，你就继续给我派工单，该提交的提交吧，早点做完之后再回来大修」＋「按照计划书进行哪里那么多废话」
＋「你 下一次 不要 再 反复构建 触发那个防火墙 烦我打游戏 再这样 我要给你喂史了」。
最后这句是**本轮新增的硬约束**，它改变的是"怎么跑证据"，不是"要不要证据"。

## 全局约束（本波派出的每一把共用，逐条可判红）

1. 开工前 `git status --short --branch`；索引里他人的**已暂存**文件（本轮实测＝`docs/dispatch-tip1020-02-memory-group-judgment.md` 1/1、
   `tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs` 63/0）不属于你，不 stage、不提交、不回退。
2. 独立 worktree 开工；**先数基线**：`git rev-list --count <你的起点>..HEAD`，落后就 `git merge --ff-only <线尾>` 追平再动手
   （HDS-011 那把落后 76 格、HDS-002 那把靠 rebase 追平——都不是意外，是常态）。
3. **一个载体只允许一轮"全解决方案"构建＋一次全量测试**（作者机器的 dotnet/testhost 会弹他机器的防火墙、打断他的对局）。
   不许"改一行跑一遍全量"式迭代。反证该怎么做才不违反这一条：**把全量那一轮留到最后一次**，之前的迭代只用
   `dotnet build <单个 csproj>`（不碰整解决方案）与读码推理；出口那轮之后的"先红后绿"用**定向**跑法——
   `dotnet test tests/<X>/<X>.csproj -c Release --filter "FullyQualifiedName~<用例名>"` 配单项目 `-t:Rebuild`，
   一次拆一道守卫，红例名与报错原文记进交付说明；**不许为了反证重跑全量**。
4. 出口读数的唯一写法：
   `dotnet test Pry.slnx -c Release --no-build --results-directory "<绝对路径>" --logger trx`，
   跑完先数到四份 `.trx` 再逐份读 `<Counters>`；**没有 TRX 就没有证据**（控制台"已通过!"不算）。
   短参 `-r` 会把结果写进 `bin\` 且四程序集零执行仍 exit 0。
5. **不许拿 `--no-build` 的旧二进制当出口证据**（本轮实测教训：HDS-002 的自报门禁里，上一把归位的 32 条核心测试与 5 条客户端测试
   一条都没执行过，`--no-build` 跑的是 rebase 前编出来的 dll）。最后一轮必须紧跟一次**完整** `dotnet build Pry.slnx -c Release`。
6. 新判据先红后绿：每条新断言都要当场拆一次它守的守卫并留红例名与报错原文；只跑绿＝没有证据。
7. Core 不依赖 Avalonia／HTTP；不新增 NuGet 包；模型、运行库、数据库、密钥、日志、用户导入内容不得提交；
   `%LOCALAPPDATA%/PryCompanion` 是禁区，只读都不许当测试载体。
8. 状态词与结案词不由实现方写：出口证据齐了也只填"已实现待验"；`已完成`／`结案`归作者与 QA。
9. 不许 `git tag`、不许 push、不许打包、不许改美术许可文件、不许动 `ASSETS_LICENSE.md`。

## HDS-011b｜表情附带文本的长度验证（AUD-07）＋那一行警告

Owner：Platform / API（写入者唯一，先登记）。前置：无（属已暴露旧缺陷，可独立修）。落点：`Pry.Api` 的两个输入校验点（都在 Api，不在 Core）。

要交付什么：
- 把贴纸／表情附带文本纳入既有消息长度上限的校验：现状是 `src/Pry.Api/Services/ConversationSessionService.cs:37-39` 与
  `src/Pry.Api/Services/ConversationApplicationService.cs:57` 两处都写成 `request.StickerId is null ? ContractValidation.Required(request.Content, "content", 20_000) : request.Content?.Trim() ?? ""`
  ——**带贴纸的那一支整段跳过长度验证**（上一把定位、未改）。
  修法是**让同一条尺量同一类内容**，不许新加第二套上限语义、不许把上限数字改松、不许只在某一入口补验（两个入口都得走同一个校验点）。
- 顺手带走上一把留下的那一行：`tests/Pry.Core.Tests/NarrativeStorageContractTests.cs:313` 的 xUnit2012 警告
  （`Assert.False()` 判集合成员 ⇒ 改 `Assert.DoesNotContain`）。只改这一行，不动 HDS-002 名下的其他内容与语义。
- 出口面要求"上限真的在起作用"：光断言"超长被拒"不够，必须有一条能抓住"把校验整段掏空"的反例（`QA-TIP1020-02` 那类形状）。

出口证据（**最后一轮全量构建＋全量测试**＋按第 3 条的定向反证，就得齐）：
- 合流树上四份 TRX 的 `<Counters>` 全量读数（总／分项／失败／点名跳过），跳过只按 `outcome="NotExecuted"` 点名；
- 本刀新判据逐文件 `[Fact]`／`[Theory]`+`[InlineData]` 计数，并与分项涨幅顶账；
- 至少两条反例的红例名与报错原文（一条打"上限掏空"、一条打"表情路径不验"）；
- `dotnet build` 那**一个警告归零**的读数（0 警告 0 错误）；
- `scripts/verify-repository.ps1` 与 `git diff --check` 的当场输出。

不许走的捷径：
- 不许把贴纸文本直接丢弃来"绕过"验证（那是改行为不是补判据）；不许为凑绿灯把上限调大；
- 不许只补客户端一层的验证而后端仍可绕过；不许顺手给表情路径加长度以外的新功能；
- 不许为消掉那行警告而加 `#pragma` 抑制或 `NoWarn`。

**派单状态（2026-10-21 深夜）**：本节已由守门人代派至一条隔离 worktree（Owner＝Platform / API，写入者唯一，本工单即登记处）；
出口证据未采齐前不写"已完成"，归位与状态词由守门人按上面的五项证据对账后再换。

**结果（2026-10-22 凌晨）**：该 lane 交付并已由守门人归位为本线尾 `c148fbf`（实现两把 rebase 自 `5304c7c`）。
五项出口证据逐条对上：合流等价树四份 TRX＝Api 258／Core 523／App 233／Client 106，总 1120／失败 0／通过 1118
（两条 `NotExecuted` 点名见 `PROJECT_PLAN.md` §9）；本刀新判据 7 条＝3 `[Fact]`＋2 `[Theory]`×2 `[InlineData]`，逐文件数过并与分项顶账；
两条反例（掏空长度判断＝4 红、恢复一个入口＝2 红）留了红例名与报错原文，且都用定向跑法、没重跑全量；
`dotnet build Pry.slnx -c Release` 0 警告 0 错误，`NarrativeStorageContractTests.cs:313` 那条 xUnit2012 同场消失；
`git diff --check` 干净。
守门人**没有**再跑一轮全量（作者硬约束，且本把 rebase 只补进两份文档、编译输入与 lane 建树同一）——这一条与它为什么成立，写在 §9 同一 entry。
两处不许读过头：源码门的"第二把尺"只认字面量 `20_000`；`verify-repository` 的 526 是 lane 在新测试文件入库前测的，归位后实测 527 格。
状态词＝**已实现待验**，改写权在 QA。全局约束 1～9 逐条未见违反；HDS-011c 由此解锁排队位。

## HDS-011c（排队，本波不开工）｜资源限额总盘与依赖/许可复采

HDS-011 的剩余范围。等 AUD-07 这把落地后再派，避免同文件撞位。

## 本波之后（排队顺序，不是开工许可）

1. **HDS-003 新生成中心隔离原型**——前置已随 HDS-002 归位解除，但它的真实模型对照要占作者这台机器的算力或 API 额度，
   **动手前需作者点头**（`PROJECT_PLAN.md` §4.2 同一行已登记）。
2. ~~`QA-TIP1019-02`（她"什么算事实"的检索地板零判据）——判据刀，零产品代码，不许改那个数字。已有工单
   `docs/dispatch-tip1019-02-retrieval-floor-judgment.md`。~~ **已于 2026-10-22 归位＝`9f88a0c`（已实现待验）**：
   第一道门 `.5` 现在有行为判据（突变 `>= .4` 当场压红），第二道门 `ContextBuilder.cs:164` 仍无判据、属未覆盖的防御纵深；
   读数由守门人从那条 lane 留在临时盘上的四份 TRX 独立采（1121／0／通过 1119）。
3. 客户端路由判据 `2954dbc`（`QA-TIP1019-03`）——等主工作区索引清空后由守门人合流，不派给实现方。
4. `QA-TIP1020-02`——它那份实现此刻正悬在他人索引里（63/0），等它归位，不重复派。
5. HDS-004／005 的表与接线——依赖 002 的 §12 未决项 U-1…U-12，开工前逐条读。

## 备忘查出的两处已暴露缺口（**已于 2026-10-22 派发**：作者一句「继续派工」⇒ 两把合成一个载体、只允许一轮全量，见 `docs/dispatch-hds-carrier-20261022.md` 片 A／片 B。下面这节保留为缺陷定义，落点与判据以新单为准）

来源＝`docs/narrative-open-items-memo.md`；两处都由守门人当场读码复核过，不是转述。

**DEF-MEMDUAL-01｜同一个库里两套结构版本账，第二套的高版拒绝不走可呈现错误**
- 当前行为：`schema_info` 那套在遇到高于二进制版本时抛 `SchemaVersionTooNewException`（`src/Pry.Core/Memory/MemoryDatabase.cs:195-199` 的门，类型在 `SchemaVersionGuard.cs:15` 起，带标题、正文、`RetryAllowed=false`）；`memory_schema` 那套（`src/Pry.Core/Memory/SqliteMemoryRepository.cs:65-66`、`:99`）在 `:100` 对 `<1 or >2` 只抛裸 `InvalidOperationException`，不经那条呈现链。
- 用户可见后果：只有当两账分叉（迁移中途失败，或别的写路径只推进了其中一套）时，高版本库会绕过头道门，用户看到的不是"本机数据库来自更新版本的 Pry、重试不会改变结果"，而是一句没有来源的技术错误；界面按普通失败处理，会引导他反复启动。
- 项目影响：HDS-001 承诺的"未知高版保护"只在一条账上成立；第二道账不在 `SchemaVersionGuard` 覆盖面内。修法＝让第二套的高版（和低版）走同一个可呈现类型，并把两账的存在本身做成一条断言。
- 推荐：**修，但押到 QA 复采那一轮并跑**（同一轮全量能同时验 HDS-001 的既有判据与本刀新判据，拆开就是两次弹窗）。理由：拒写本身已有测试钉住（`tests/Pry.Core.Tests/MemoryDeletionSeparationTests.cs:273-288` 的 N12），缺的是呈现形状，属可见性缺陷，不是当下必然损坏数据。
- 不许走的捷径：不许把两账合成一账（那是迁移决定，属 HDS-005）；不许为了过门把可呈现判断写成文本匹配异常消息；不许改 `SchemaVersion` 那个数字；不许顺手加"自动降级旧库"的任何路径。

**DEF-WRIGATE-01｜第二宿主仍"记一条警告然后覆盖"描述文件**
- 当前行为：`src/Pry.Api/Services/RuntimeDescriptorService.cs:88-90` 在发现别的活进程留下的描述文件时，日志写「本次启动将覆盖它」，随后照样 `WriteAtomically`（`:63`）。
- 用户可见后果：同一数据目录被两个后端共用时，读取方（客户端）可能连到并非真正写者的那个宿主，表现为"连上了但说的不是她刚说的话"，且没有任何一处告诉用户"这个目录正在被两个程序同时写"。
- 项目影响：这正是 HDS-002 契约禁止的形状——`DataDirectoryWriteGate` 与 `OutboxOwnershipRegistry` 已经写好了判定，但生产路径没有取用；不补的话，新链一切"单写者"承诺在真实双宿主下都是纸面。
- 推荐：**修**，与 DEF-MEMDUAL-01 共用同一轮构建；落点＝初始化前先过 `DataDirectoryWriteGate`，所有权丢失后条件提交必须拒绝（HDS-002 工单原话）。若作者只想先要可见性不要行为变化，退一档＝只把"有两个宿主"呈现成一句人话并拒绝启动。
- 不许走的捷径：不许用命名 `Mutex` 之外的方式假装上锁而不做条件写；不许把 `runtime.json` 当所有权锁（它只负责发现）；不许在第二个宿主告警后继续写；不许改 `Pry.Core.Narrative` 里已归位的判定语义（那是契约名下文件，要改先登记写入者）。
