# HDS 第五波派单（2026-10-22）｜客户端边缘路由判据归位 ＋ 三处反证缺口补证 ＋ 限额表最后一格边界 ＋ 一把零构建文档刀 ＋ 双开写锁的取锁顺序缺陷

派单人：守门人（Architecture / Integration / Release）。执行方式＝**守门人自起子 Agent**（作者 2026-10-22 逐字「你，还是自己起Agent吧，你这派单跟我组织Agent的方式完全不一样」），工单是给冷启动执行用的，不交作者转派。
坐标系＝`docs/plans/PROJECT_PLAN.md` §4.2；本波不新开功能面，只做**五件已经暴露、且不依赖作者裁决**的事：把一把落后 46 格的判据接回来、把三处"只由内嵌样张反证过"的门补上真突变、把总盘最后一格能判的边界判掉、更正备忘里已过期的措辞、把出闸前预检新查出来的那条缺陷（`DEF-WRILEASE-ORDER-01`：第二个进程在被写锁拒掉之前已经改了共享库）修在它自己的那一刀里。

基线起点＝**派单时的线尾**（本单出闸时线尾＝第四波归位后的 `5593e1a`，其上另有守门人的登记提交；起笔前 `git log --oneline -1` 当场取线尾，别抄这里的号）。
**出口顶账起点当场写死＝Core 527／Api 274／App 233／Client 106＝总 1140，失败 0、通过 1138、两条环境跳过照旧同名**（守门人 2026-10-22 逐份读四份 `.trx` 采的）。**不要为了"确认起点"先跑一轮全量。**

**归位登记（守门人 2026-10-22 当晚）⇒ 本单五片全部闭合，不要再认领、不要再起构建**：片 F1＝`2eba867`、片 F3＝`e31e4da`（含总盘那一格一行）、片 F5＝`dc5b010`（`fix:`，本波唯一动产品码的一片）、片 F2＝零产品码且三处突变逐字节还原、片 F4 的两半在出闸时已由守门人做完并随工单入库。出口读数＝**总 1146＝1144 绿＋2 条环境跳过照旧同名、零红例**（Client 108／App 233／Api 278／Core 527），落在上面那个等式区间之内；顶账＝Client 106→108 是片 F1 那两条路由，Api 274→278 的四条差额＝片 F3 三格＋片 F5 一格，Core／App 一字未动（差额按整棵合流树一次数到，不是逐枚提交各测一遍）。一次解决方案级构建＋一次全量，都起在 v4 焦点预检读到 `BUILD_OK` 之后。逐条读数、两处工单更正（F2③ 的扫描面守卫会先红、总盘那一格授权改一行）与本角色自己那次"计数脚本读错了树"的错账＝`docs/plans/PROJECT_PLAN.md` §9 同日条。

---

## 全局约束（条文与第四波同源，逐条可判红；完整原文见 `docs/dispatch-hds-011c-20261022.md` §全局约束）

1. 开工前 `git status --short --branch`；主工作区索引里他人**已暂存**的两枚文件不属于你（本轮实测＝`docs/dispatch-tip1020-02-memory-group-judgment.md` 1/1、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs` 63/0）——不 stage、不提交、不回退、不碰。
2. 独立 worktree 开工（守门人已备好就用，没备好就 `git worktree add .local-notes/<单名> -b codex/<单名> <线尾>`），先 `git rev-list --count <线尾>..HEAD` 数基线。
3. **整个载体只允许一轮解决方案级完整构建＋一次全量测试。** 迭代只用单工程 `dotnet build`；反证一律定向 `--filter` 跑。**不要**把 `-t:Rebuild` 传给解决方案／多项目（本机 SDK 10.0.200 误解析报 MSB1008）；等效写法＝单 csproj 构建＋同 csproj `--no-build --filter`。**这一条是本波存在的主要理由**：作者机器 10-22 已经弹过两轮，任何"再多跑一轮看看"都不许出现。
3a. **弹窗口径改了（作者 2026-10-22 两条现令，逐字：「你弹窗之前看看我有没有在运行游戏不就好了」＋「你不如这样判，游戏程序基本都是全屏应用，你在准备构建的时候先看看当前桌面窗口焦点是否为全屏应用，如果不是就可以构建测试」）**：从"当日一律不许弹"变成"**先看焦点再弹**"。任何一次可能弹防火墙的构建（包括单工程 `dotnet build`）之前，必须先跑
   `powershell.exe -NoProfile -ExecutionPolicy Bypass -File C:\Users\24782\Desktop\Pry_Project\.local-notes\game-preflight4-1022.ps1`
   并把整段输出原样贴进交付说明。`VERDICT=BUILD_OK` ⇒ 可以构建；`VERDICT=HOLD` ⇒ **就地停下并把 HOLD 那几行报给守门人**，不许"等三分钟再试"、不许换更省事的跑法绕过、更不许因为"我这一格还没验"就自行构建；`WARN`（GPU 高或可用物理内存 < 6 GB）**不阻止构建**，但内存那一格若成真（SQLite `disk I/O error`／`out of memory`）就按第 9 条把读数记作废。**判据是"焦点窗"而不是"有没有游戏在跑"**：没在焦点上的全屏窗只列信息、不算 HOLD。
4. 出口唯一写法＝`dotnet test Pry.slnx -c Release --no-build --results-directory "<绝对路径>" --logger trx`，跑完先数到四份 `.trx` 再逐份读 `<Counters>`；跳过只按 `<UnitTestResult outcome="NotExecuted">` 点名（`notExecuted` 恒 0）。最后一轮必须紧跟一次完整 `dotnet build Pry.slnx -c Release`（0 警告 0 错误），**不许拿旧 dll 充证据**。
5. 新判据先红后绿；**本波的特殊要求是"真突变"**：凡 F2 那三条，必须在载体自己的 worktree 里把被保护的**真实代码／真实真相源**临时改坏一次，采到红例全名＋报错原文后**逐字节还原**，并在交付说明里给出还原证据（`git diff --exit-code` 干净＋该文件 `git status` 不出现）。**载体专属 worktree 之内允许这种临时改坏；仓库里任何其他树不允许。**
6. 不许 `git tag`、不许 push、不许打包；不新增 NuGet 包、不升级任何包版本；**不改任何限额数值**；Core 不依赖 Avalonia／HTTP；`%LOCALAPPDATA%/PryCompanion` 是禁区（连只读都不许当测试载体）；模型／运行库／数据库／密钥／日志不得提交。
7. 不许动 `docs/plans/**`、他人名下文件；`LICENSE`／`ASSETS_LICENSE.md`／`licenses/**`／`docs/upstream-license-compatibility.md` 的内容**只读**（F2 的第②③条要改的是 csproj 与 notices 表——改完必须逐字节还原，且**不许**把它们的变化留在任何提交里）。
8. 状态词与结案词不由实现方写；证据齐了也只填"已实现待验"。不要自己合回主线，留分支＋SHA 给守门人归位。
9. 机器资源形状：门禁成片若出现 SQLite `disk I/O error`／`out of memory`，那是他机上 ComfyUI 占了约 10 GB 可用提交，**读数记作废并如实报告**，不要动任何进程、不要为此重跑第二轮全量。

---

## 出闸前预检（守门人 2026-10-22，零构建，全部当场读码所得）

跑那一轮之前把四片的前提逐条核过，核得到的写在这里，核不到的一律改判措辞——**这一节的唯一用途是不让当日唯一那轮构建花在追一个红不了的突变上**。

- F1 前提成立：`d7276aa` 的改动面＝两枚测试文件 25/0 与 19/0，零删除；钉住的三条路由在当前产品码里逐行对得上（`src/Pry.Api/Controllers/MemoriesController.cs:21` 的 `[HttpPost("{id:long}/confirm-keep")]`、`src/Pry.Client/PryBackendClient.cs:188` 拼的 `?characterId=`、`src/Pry.Api/Controllers/ConversationsController.cs:19` 的 `Delete(string id, [FromQuery] bool forgetExclusiveMemories = false…)`）。
- F3 前提成立，而且**不需要任何新产品接缝**：比较语义的原文在 `src/Pry.Api/Services/ModelPerformancePolicy.cs:71-72`＝`profile.ContextSize > 4096 && metrics.TokensPerSecond is > 0 and < MinimumUsableTokensPerSecond`。边界样本据此定死：12.0 判不慢、11.9 判慢、0 判不慢（那是 `> 0` 那一半，别漏）、`ContextSize` 恰为 4096 时配 4.5 token/s 必须判不慢（地板）。同一族还有第二把闸在 `src/Pry.Api/Services/ModelProcessRegistry.cs:63`，它自己又拿 `attemptedProfile.ContextSize > 4096` 判一次，`MayTryLowerContext`（`:74-75`）的错误码白名单也已存在——**两处闸各钉一格**，只钉 policy 那一处等于第二把闸没人守。
- F4.1 有实活可做，不是走过场：`docs/narrative-open-items-memo.md` 第 167 行那句"今天没有任何地方取目录锁"已被第三波片 A 证伪（`FileShare.None` 的 `runtime-write.lock`，`src/Pry.Api/Services/DataDirectoryWriteLease.cs:52` 取句柄，`RuntimeDescriptorService.cs:43` 在启动第一步就取）。同页另外两处受它牵连：第 29 行的计数口径（"落地面（表、锁、真进程）一条都还没进主线"——锁那一格已进）、第 49 行的"② 锁句柄形态自选（命名 `Mutex` 或 `FileShare.None` 都不改变 Core 判定）"（形态已选定，不再是自选）。改口径时**只改"已被既有代码回答到什么程度"**；U-3 的代次持久化照旧未决（`DataDirectoryWriteGate.cs:103` 仍是进程内 `Math.Max`，重启即归零），别把它顺手写成已答。
- F4.2 守门人已跑完一遍，**结论＝零对不上**：37 条"有判据"的行里点名的用例名全部住在该行所指的测试文件内；三处行号钉当场读回的内容与表中语义一致（`tests/Pry.Core.Tests/ModelProviderErrorTests.cs:129-135` 读回 `CreateModelClient(localLoopback: true)` 那一族、`tests/Pry.Api.Tests/DesktopCapabilityGuardTests.cs:176` 是 `Assert.Equal(CapabilityIds.TimeoutMs, x.TimeoutMs)`、`src/Pry.Core/Inference/OpenAiCompatibleChatModel.cs:17` 是 `streamIdleWindow ?? ProviderTimeouts.ResponseStreamIdle`）。载体这一格只做两件事：随 F3 把 `MinimumUsableTokensPerSecond` 那一行的判据栏补成新用例，以及复核上面两条行号钉没被 F1～F3 的移动冲掉。仪器留在 `.local-notes/ledger-crosscheck2-1022.js`（gitignored，一律不删）。
- F2 的第②③条前提成立：`tests/Pry.Api.Tests/UpstreamProvenanceAndDependencyShapeTests.cs:187`＝`No_package_reference_floats`、`:213`＝`The_third_party_table_names_every_restored_package`；`licenses/THIRD-PARTY-NOTICES.md:9` 就是 `| Avalonia | 11.3.20 | MIT | https://github.com/AvaloniaUI/Avalonia |` 那一行。
- 那两条突变的**生效面在盘上的文本**（已读码确认，不是推测）：`UpstreamProvenanceAndDependencyShapeTests.cs:459-464` 的 `PackageReferences()` 用 `File.ReadAllLines(project)` 直接读 csproj，`:452-454` 的 `NoticeComponents()` 直接读 `licenses/THIRD-PARTY-NOTICES.md`——两边都不读 `project.assets.json`、也不看已编译产物。所以改坏 csproj 的版本串或删掉表里一行之后，**定向 `--no-build --filter` 一次就能生效，不必再 restore、不碰网络**＝这两条突变不会再额外弹一次防火墙。还原同理只需逐字节写回。
- F2 的第①条**已被本预检改判**，改判后的写法见下。
- 第 3a 条那把预检仪器**已双向校准**（守门人 2026-10-22，两只方向都当场采到）：
  - **绿半＝作者实机桌面**：`VERDICT=BUILD_OK`，`primary_screen=2560x1440 windows_scanned=525`、`gpu_util=24%`、`free_memory_gb=6.4`；三枚全屏窗（TextInputHost／NVIDIA Overlay／explorer 的 Program Manager）全部判为 `work=True`，所以它们只列证据不算 HOLD。**一把只会恒绿的仪器没用，一把只会恒 HOLD 的仪器也没人会用**——v1 就是后者（Steam 客户端、Unity、QQ、GPU 26%、7.4 GB 全算成"在打游戏"），所以判据改成"装游戏的路径 ∨ 已知游戏进程名 ∨ 无 owner 的可见全屏无边框窗（非工作进程） ∨ GPU≥60%"四条，工作进程名单单独豁免。
  - **红半＝自己起的探针窗**：`.local-notes/compile-gpprobe-1022.ps1` 编出 `.local-notes/gpprobe.exe`——不可见（`Opacity=0.0`＋`WS_EX_LAYERED`）、点击穿透（`WS_EX_TRANSPARENT`）、不抢焦点（`WS_EX_NOACTIVATE`）、`ShowInTaskbar=true`（否则窗被挂在隐藏 owner 下面、`EnumWindows` 根本看不见它）、占满主屏、20 秒自关。同一台机器上同一次扫描立刻变成 `VERDICT=HOLD`，并点名 `gpprobe pid=15140 area=3686400 title=GP-PREFLIGHT-PROBE work=False`；测完只 `Stop-Process` 自己起的那一个 pid，不碰他任何进程。
  - **红半两次没压红，学到的是判据选错了而不是仪器坏了**：① `Process.MainWindowHandle` 看不见 popup 窗（它只认"主窗"概念）⇒ 换 `EnumWindows`；② 换完后仍绿，dump 出 `style=0x16010000`——**里面没有 WS_POPUP 位**，因为最大化后的无边框窗通常靠"没有 `WS_CAPTION`(0x00C00000) 也没有 `WS_THICKFRAME`(0x00040000)"表达自己，而不是靠 `WS_POPUP` ⇒ 判据换成那两位的**缺失**。这同属记忆里"正向探针≠门在守"那一族：一个只被样张反证过的检测器，跟只被内嵌样张反证过的门禁测试是同一件事。
- **v3 当日即被作者的第二条现令取代，换成 v4＝焦点判据**（逐字：「你不如这样判，游戏程序基本都是全屏应用，你在准备构建的时候先看看当前桌面窗口焦点是否为全屏应用，如果不是就可以构建测试」）。`game-preflight4-1022.ps1` 只问一件事：**当前 `GetForegroundWindow()` 那个窗**是不是"盖住它所在屏 ≥95% ＋ 既无 `WS_CAPTION` 也无 `WS_THICKFRAME`"，或它的进程是不是已知游戏／住在游戏安装目录；**不在焦点上的全屏窗只列信息、不算 HOLD**（v3 正是把这类常驻全屏窗当判据，才会把 TextInputHost、NVIDIA Overlay、explorer 和 GPU 26% 一起读成"在打游戏"）。双向读数当场都采到：**绿半**＝焦点是 ChatGPT 的普通窗（`style=0x14C70000 rect=650,186,1615x1109 covers=False borderless=False`）⇒ `VERDICT=BUILD_OK`；**红半**＝自起 `.local-notes/gpfocus.exe`（同样不可见、点击穿透，但**能拿焦点**）⇒ 焦点行当场变成 `FOREGROUND: gpfocus pid=56000 style=0x16010000 rect=0,0,2560x1440 covers=True borderless=True` ＋ `VERDICT=HOLD`，停掉自己起的 pid 后复位 `BUILD_OK`。
- **v4 的红半第一次也压不红，原因是 Windows 的前台锁**：非前台进程调 `SetForegroundWindow`／`SwitchToThisWindow` 会被系统拒绝，探针占满了屏幕却拿不到焦点（那一次读数是 `FOREGROUND: steamwebhelper … VERDICT=BUILD_OK`，而 `gpfocus` 只出现在"未聚焦全屏窗"那一行信息里——**这恰好证明信息行与判据行是分开的**）。要拿到真焦点得先 `keybd_event` 点一次 ALT、再 `AttachThreadInput` 挂到当前前台线程上调用、随后解挂。**这条对产品侧同样有用**：任何"我们希望把某个窗带到前台"的路径（托盘、通知、错误弹窗）在同一台机器上都会撞上同一道锁，所以"调了 `SetForegroundWindow`"不等于"到了前台"，判据要读回焦点。

---

## 片 F1｜客户端边缘两条路由判据接回来（**已有实现，只差归位**）

现状（守门人 2026-10-22 当场核过）：QA-TIP1019-03 那两条判据在分支 `codex/tip1019-03-client-route` 的 `2954dbc`，起点落后线尾 **46 格**，一直没归位（因为当时共享索引里有他人暂存）。守门人已把它 **rebase 到本单基线**并验过两处事实：

- rebase 干净、无冲突，产物＝分支 **`codex/tip1019-03-rebased-1022`** 的 **`d7276aa`**，改动面＝只有两枚测试文件（`tests/Pry.Client.Tests/MemoryManagementClientTests.cs` ＋25、`tests/Pry.Client.Tests/PryBackendClientTests.cs` ＋19，44 行全新增、0 删除）；
- 钉住的两条路由在当前产品码里**真的存在**：`src/Pry.Api/Controllers/MemoriesController.cs:21`（`[HttpPost("{id:long}/confirm-keep")]`）与 `src/Pry.Client/PryBackendClient.cs:188`（拼 `?characterId=`），以及 `src/Pry.Api/Controllers/ConversationsController.cs:19-21` 的 `Delete(string id, [FromQuery] … forgetExclusiveMemories)`。转义样张用的是 `a%26b`（不是空格——`Uri` 会把空格替回来，那是假判据）。

要做什么：把 `d7276aa` 取进载体分支（`git cherry-pick d7276aa` 或 `git merge --ff-only codex/tip1019-03-rebased-1022`，取决于它是否正好落在基线之上），随本波那一轮全量一起验。
预期顶账＝**Client 106→108**（＋2 例；逐文件 `grep -c '[Fact]'` 数出来再顶）。
反证（定向跑，不占全量）：临时把 `PryBackendClient.cs:188` 的 `confirm-keep` 拼串改错（或把 `characterId` 查询参数丢掉）⇒ 必须红在"点确认保留打到错路由"那一格；改回后逐字节还原。**不许**为了让判据过而放宽那条 `Assert.Equal`。
不许走的捷径：不许把判据改写成"包含子串就行"（`Assert.Contains`）——那正好放过"查询串丢了"这一类真缺陷；不许删掉 `forgetExclusiveMemories=false` 那一半（两半各自钉住"删记录不撤回她说过的话"的一个方向）。

## 片 F2｜三处"只由内嵌样张反证过"的门，补真突变（**零产品码变更**，还原是硬要求）

这三处的共同形状是：**门是好的，反证今天只做到了一半**——上一波要么红在了邻近的另一向，要么只能用测试内嵌样张证明"扫描器看得见那种写法"。本波把它们补齐。

1. **第三波片 A（`DataDirectoryWriteLease`）的"降级继续写"一向——本条按出闸前预检改判，目标从"补到红"换成"把够不着写清楚并留下证据"**。上一轮突变摘掉 `StartAsync` 里的取锁，红确实红了，但红在 fail-closed 的写路径（`runtime.json 未在后端启动后写出`），不是原缺陷那一向。本轮起笔前把这一行的可达性读实了，结论是：**单点改坏 `RuntimeDescriptorService.cs:78` 的 `_writeLease.AssertWriteAllowed();` 不会让任何在册用例变红**。事实链三条，交付说明里要各自复现一次——① 该行的调用方只有 `Write()`，而 `Write()` 只挂在 `ApplicationStarted`（`RuntimeDescriptorService.cs:47`），一次宿主生命周期只跑一回，紧跟在 `StartAsync` 的 `_writeLease.Acquire`（`:43`，前面只有一句 `ObserveDescriptorFacts()`）成功之后；② `DataDirectoryWriteGate` 的 `_held`／`_fencingEpoch` 是实例字段（`src/Pry.Core/Narrative/DataDirectoryWriteGate.cs:54-55`，全类无静态可变面），仓库里 `DataDirectoryWriteLease` 只有一个构造点（`RuntimeDescriptorService.cs:24` 的字段初始化），同进程内没有第二个持锁者能把第一个的租约 fence 掉；③ 因此走到那一行时 `_lease` 必然刚签发、`CheckConditionalWriteAllowed` 必然回 `None`（`DataDirectoryWriteLease.cs:84-95`），把守卫换成"记一条警告然后照写"在测试里等价于没改。**本条要做的是两件，都不许省**：先把这一处单点突变真做一次（只在载体自己的 worktree 内），定向跑 `tests/Pry.Api.Tests/RuntimeDescriptorWriteGateTests.cs` 全四格，**把"仍然全绿"这件事当证据采下来**（四格用例名＋各自结论原文），随后逐字节还原；再补一次对照突变＝把 `DataDirectoryWriteLease.Acquire` 的 `catch (IOException failure)`（`:60`）改成"记警告后返回、不抛"，那次必须红，红点预期在 `Second_host_on_the_same_directory_refuses_startup_without_touching_the_first_descriptor` 的 `Assert.ThrowsAsync` 那一行（第二实例不再抛），**不是**红在"字节不变"那一格——后者被前者挡在前面，这个"想看的红永远隔在另一格后面"的形状就是够不着的证明。两次读数齐了就在 `docs/plans/PROJECT_PLAN.md` §9 如实登记一句：契约 §10.2 第 3 条那次条件写复查在 Api 侧今天没有行为判据，要判它必须先在产品里造第二条写路径或注进门控／时钟接缝，那是新功能、不在本单。**不许**为了留下一个红例去放宽任何既有断言，也不许把这一条写成"已补"。
2. **片 E1 第 3 条（依赖版本不许浮动）**：上一轮只有内嵌样张红。本波在载体自己的 csproj 上做一次真突变＝临时把某个 `PackageReference` 的 `Version` 改成 `10.0.*`（挑一个不影响还原结果读的写法，改完立刻还原），定向跑 `UpstreamProvenanceAndDependencyShapeTests.No_package_reference_floats` 必须红且指名那一行；**该突变不得进任何提交**，还原后 `git diff --exit-code` 必须干净。
3. **片 E1 第 4 条（第三方表覆盖还原面）**：同样做一次真突变＝临时从 `licenses/THIRD-PARTY-NOTICES.md` 的直接依赖表里删掉一行（挑 `Avalonia` 那一行，它牵住三个包），定向跑 `The_third_party_table_names_every_restored_package` 必须红，且**红得点出那一族的每一个包名**；还原并逐字节核对。

交付：三条各自的**红例全名＋报错原文**、还原证据（`git status` 与 `git diff --exit-code` 的输出）、以及"这条突变红在哪一格、另一格为什么没跟着红"的一句话。
不许走的捷径：不许用"加一条新的内嵌样张断言"冒充真突变（那正是本波要补的缺口）；不许把突变写进提交或分支历史；不许为让红更"好看"去改判据文本；第 1 条若在还原前发现红的位置仍在 fail-closed 写路径而不在覆盖路径，**如实登记"这一向仍然够不着"**，不要圆成已补。

## 片 F3｜总盘最后一格能判的边界：`MinimumUsableTokensPerSecond`（数值 12 不许动）

现状：`docs/resource-limit-ledger.md` 第二节第三行登记 `src/Pry.Api/Services/ModelPerformancePolicy.cs:9` 的 `12`，判据栏写的是"`Model_performance_policy_only_retries_capacity_and_performance_failures`（4.5 token/s 判太慢；**未判 12 这条边界与"4096 以下不再降档"的地板**）"。也就是说今天只有"远离边界的读数"，边界本身没人钉。

要做什么：补**行为级**判据，至少两条：① 恰好落在门槛两侧的形状（按 `ModelPerformancePolicy` 的真实比较语义取样本——先读代码再写断言，不许猜 `>=`／`>`）；② 那条"窗口小于 4096 就不再往下降档"的地板，判它真的停在地板而不是继续降到 0（后果是用户看见"她永远不回话"）。
预期顶账＝Core 或 Api 的某一分项 ＋2～＋4（逐文件数出来再顶，别先写数）。
不许走的捷径：**不许改 `12`、`4096` 或任何相邻数值来让判据好写**；不许写成 grep 常量名的文本门（总盘明写那种不算判据）；若当场证明"这条边界在不新增产品接缝的前提下无法行为级判到"，就**如实登记**"无判据＋为什么"，并把那一格留在表里——**不许**为此在 Core 里新开时钟接缝（那是扩范围，工单外）。

## 片 F4｜零构建文档刀（不起一次 `dotnet`）

> **状态（守门人 2026-10-22 当场做完，随本单的文档刀入库）：两半都已执行完，carrier 不再重做，也不许再碰 `docs/narrative-open-items-memo.md` 与 `docs/resource-limit-ledger.md` 这两枚文件。** 下面是执行结果与仍然成立的要求。

1. 更正 `docs/narrative-open-items-memo.md` 里已过期的措辞：**U-2／U-3／U-12**（目录锁真实形态、目录代次与实例写权代次的联动、目录锁与未知高版拒写门的先后）三格要按已落地事实改口径——第三波片 A 已经落了**进程级独占写锁**（`runtime-write.lock`，`DataDirectoryWriteLease.Acquire` 在 `StartAsync` 第一步，`AssertWriteAllowed` 在每次写前复查；契约 §10.1 已改写为落地后事实），第四波片 B 把高版拒写走可呈现异常。**只改"已被既有代码回答到什么程度"和指认的行号**，不许新增未实现的承诺，不许动其余九格的口径。
   → 实际改出口径（守门人）：锁的**形态**确实已落地（`src/Pry.Api/Services/DataDirectoryWriteLease.cs:20/:52`、`RuntimeDescriptorService.cs:43` 取锁、`:78` 每次写前复查），但"在 `StartAsync` 第一步"这句作为**唯一缺口**已经不成立——它在 `Program.cs:5` 的 `RunAsync` 里，而第一次写盘在 `:143/:145` 的 `BuildAsync` 里，所以 U-2／U-12 改成"形态已定、取锁时机坐错位置"，并把那条缺陷登记为 `DEF-WRILEASE-ORDER-01` 交给片 F5。
2. 交叉核对 `docs/resource-limit-ledger.md` 判据栏里点名的**用例名与文件是否真的存在**（这张表的守卫门只钉限额常量的 `文件:行号`，不钉"判据那一列写的用例名"）。把对不上的逐条列进交付说明；若确有对不上，**只改表里那一格为真实形状**（写"无"也行），不许新增断言。
   → 实际核对结果（守门人，仪器＝`.local-notes/ledger-crosscheck2-1022.js`）：**37 行有判据的登记全部对得上，0 处不一致**（含 `src/` 侧的行号引用），因此这一格**不改表**；carrier 若重跑发现新的不一致，仍按上面那句只改那一格。

交付形态：F1～F3 各一刀（`test:`／`test:`／`test:`，提交信息点名 HDS-011d-F1…F3 或沿用 HDS-011c 编号由你定，但要能对上本单），F4 单独一刀（`docs:`），F5 单独一刀（`fix:`，产品码改动与它自己的新判据同刀）。F2 的突变**一律不进提交**。

## 片 F5｜把取锁提前到第一次写盘之前（`DEF-WRILEASE-ORDER-01`，本波唯一动产品码的一片）

缺陷事实（守门人 2026-10-22 当场读码所得，零构建）：**第二个进程在被写锁拒掉之前，已经改过那份共享库**。真实顺序＝`src/Pry.Api/Program.cs:4` 走 `BackendApplication.BuildAsync` → `:143` `MemoryDatabase.InitializeAsync`（`src/Pry.Core/Memory/MemoryDatabase.cs:33-34` 那句 `PRAGMA journal_mode=WAL` 改的是库头、`:37` 跑迁移清单）→ `:145` `ConversationRuntime.RecoverStalePlansAsync` → `src/Pry.Core/Memory/MemoryDatabase.Runtime.cs:281-285` 那两条 `UPDATE`（`:282` 先把 `state='pending'` 且 `reply_plan_id` 落在 open 计划里的 `planned_messages` 改成 `cancelled`，`:284` 再把全部 `status='open'` 的 `reply_plans` 改成 `cancelled`，**没有归属、没有 pid、没有代次过滤**；`:279` 那句早退只保证"库里没有 open 计划时不动笔"）→ 到 `Program.cs:5` 的 `app.RunAsync()` 才轮到 `RuntimeDescriptorService.StartAsync` 取租约并抛 `DataDirectoryWriteLockedException`。用户可见后果＝双开时**先开那个实例排好、还没发出去的那条消息被后开的取消掉**，而第一个进程收不到任何通知；同一形状让拒绝文案 `DataDirectoryWriteLease.cs:142-143` 那句"本次启动已经拒绝，没有写入或改动任何文件"只在"没碰 `runtime.json`"这一格成立。

要做什么（范围就这三件，一件都不许多）：

1. 把取锁动作提前到第一次写盘之前，落点在 `BackendApplication.BuildAsync` 里、`InitializeAsync`（`Program.cs:143`）之前。Core 的判定形状与 `SchemaVersionGuard` 的闸门位置一个字不动（AGENTS 第 5 条：Core 不依赖进程类型；`MemoryDatabase.cs:30-31` 那条"拒写门在任何写之前"的既有判据必须继续成立，`tests/Pry.Core.Tests/SchemaVersionGuardTests.cs:83` 守着它）。两处实现注意：`AddHostedService<RuntimeDescriptorService>()`（`Program.cs:118`）注册的实例今天要到 `RunAsync` 才被解析，提前取锁就要保证**同一个租约实例**被取锁方、宿主服务与呈现链共用（不要把锁再 `new` 一遍，也不要让第二次 `Acquire` 变成另一个持有者）；租约的归还路径（`StopAsync` 与 `Dispose` 双路径，`RuntimeDescriptorService.cs:52-65`）要照样幂等，否则取锁提前会把句柄留成泄漏。
2. 给被拒路径补一条**行为判据**：真造一条 `open` 的 `reply_plans` 与一条 `pending` 的 `planned_messages` 写进第一个实例的库，然后让第二个进程走真 `BuildAsync` 起步，断言"第二个被拒"与"那两行的状态一个字没变"两半。**这条判据在修复之前必须是红的**（红在计划已变 `cancelled`），修复之后才转绿——这就是本片自己的先红后绿。**不许**用 grep 那条 SQL 代替，也不许把断言写成"抛出异常就算过"（只判抛异常的话，把清扫留在原位也一样绿）。
3. 随取锁提前把 `tests/Pry.Api.Tests/RuntimeDescriptorWriteGateTests.cs` 里**两格**的拒绝点形状改过来：`Second_host_on_the_same_directory_refuses_startup_without_touching_the_first_descriptor`（`:46-47`）与 `Refusal_with_no_descriptor_yet_writes_none_and_a_successor_gets_the_lease_after_the_lock_is_released`（`:96-97`）今天把 `Assert.ThrowsAsync` 挂在 `StartAsync` 上，提前之后异常来自 `BackendApplication.BuildAsync`。另外两格（`Graceful_exit_clears_the_descriptor_and_the_next_host_can_write_its_own`、`Exit_cleanup_never_deletes_a_descriptor_owned_by_a_successor`）走 `StartAsync(directory)` 那个辅助方法（build＋start 两步），形状不用改，但它们**必须照样绿**——这两格正是"提前取锁不许把正常路径和后继实例焊死"的现成判据，别为它们加新断言。四格用例名一个字都不改。文案那句"没有写入或改动任何文件"**在提前之后才成为真话**——不许为了让旧断言通过而把它改软；另外要知道：今天没有任何一条在册判据钉住那句话（在册四格钉的是 Headline、`RetryAllowed` 和 Body 里"另一个 Pry"／"不会改变结果"两处子串），所以第 2 条那条行为判据就是它唯一的凭据，**不要**再为文案补一条 grep 文本门当作判据。

预期顶账＝**Api 274→275**（＋1 格新判据；改形状那两格不改变条数。逐文件 `grep -c '[Fact]'` 数出来再顶，别先写数）。
不许走的捷径：不许只在 `SweepStaleRuntimePlansAsync` 上加一层"归属过滤"就宣布修好（那仍然是第二个进程在先开实例的库上动笔）；不许把那次清扫整段删掉（它是崩溃恢复的既有行为，`Program.cs:144` 的注释与既有判据在册）；不许把锁挪进 `MemoryDatabase`（Core 边界）；本片**不改任何限额数值**，也不新增 NuGet 依赖。

### 片 F5 的落点提示（守门人当场读码所得，不是新范围，是为了让载体别踩这几处）

- **注册形状**：`AddHostedService<T>()`（`Program.cs:113-122` 一共八枚）只把 `T` 注册成 `IHostedService`（`TryAddEnumerable`），所以 `app.Services.GetRequiredService<RuntimeDescriptorService>()` 今天**解析不出来**；而枚举 `IEnumerable<IHostedService>` 会把 `BackendRuntime`／`MemoryEmbeddingService`／`ProactiveScheduler` 全部提前实例化，那是新的启动副作用，不在本单范围。干净写法＝把 `DataDirectoryWriteLease` 注册成自己的单例（`RuntimeDescriptorService.cs:24` 那个字段初始化改成构造函数注入），取锁方从 `app.Services` 解析这个单例并 `Acquire(...)`，宿主服务里仍然只有一份租约。
- **失败路径的句柄泄漏（本波最容易在这里做出假红）**：`Program.cs:134` 的 `builder.Build()` 已经把 `app` 和它的服务提供者建好了，取锁抛异常时没有任何人再持有 `app`，容器不会 Dispose，于是 `DataDirectoryWriteLease.cs:52` 那把 `FileShare.None` 句柄留在**同一个测试进程**里。`RuntimeDescriptorWriteGateTests` 在同目录上连续起停多枚实例（`:32`、`:110`、`:129`、`:135`、`:154`），一旦泄漏，后面的实例会撞自己留下的锁——报错看起来像判据红，实际是仪器漏了释放。所以提前取锁必须写成"取锁失败 ⇒ 先 `await app.DisposeAsync()` 再 rethrow"这一类形状；这条不是新增功能，是提前动作自带的义务。
- **三处现成的事实，别自己重新发明**：`DataDirectoryWriteLease.Acquire` 开头 `if (_lease is { } existing) return existing;`（`DataDirectoryWriteLease.cs:44`）已经把重复取锁收成同一份租约；`ObserveDescriptorFacts()`（`RuntimeDescriptorService.cs:116-128`）只读描述文件、不建目录不写文件，跟着提前没有副作用；数据目录只在 `DirectoryNotFoundException` 那一支才被创建（`DataDirectoryWriteLease.cs:55-58`），所以"取锁比 `InitializeAsync` 早"不会改变目录创建语义。
- **新判据的现成载体**：`RuntimeDescriptorWriteGateTests.cs:175-188` 的 `NewDirectory()` 与 `Configure()`、`:209-213` 的 `Cleanup()`（含 `SqliteTestPools.Clear`）直接复用，一律临时目录；两行遗留状态用 `MemoryDatabase` 直接写（`reply_plans` 里一条 `status='open'`、`planned_messages` 里一条 `state='pending'` 且 `reply_plan_id` 指向前者），不要经 HTTP；第一个实例用 `StartAsync(directory)` 起（它会写完描述文件），第二个用 `BackendApplication.BuildAsync(configure: builder => Configure(builder, directory), token: ...)` 起——修复之后异常就是从这一句冒出来，而**修复之前这一句会成功返回并把两行改成 cancelled**，那正是本片要的红灯。

## 出口证据（一次跑齐）

1. 四份 `.trx` 的 `<Counters>`：总／四分项／失败／点名跳过；起点＝527／274／233／106＝1140，顶账写法＝"某分项 X→X＋N，N＝本刀新增几例（逐文件 `grep -c '[Fact]'`，再数 `[Theory]`/`[InlineData]`）"。**五片全落时的期望等式**＝Client 106→108（F1 的＋2）、Api 274→275（F5 的＋1）、Core 527→529 或 Api 再＋2（F3 的边界与地板落在哪一分项由 `IsTooSlow` 的归属决定）⇒ 出口总量期望 **1145～1147、失败 0、点名跳过仍只有那两格同名环境跳过**。对不上就是数错了，或有人悄悄删了用例——先算清再写报告。
2. 完整 `dotnet build Pry.slnx -c Release` 那轮的 0 警告 0 错误。
3. F1／F3 每条新判据的红例全名＋原文；F2 三条真突变的红例全名＋原文＋还原证据。
4. `scripts/verify-repository.ps1`、`scripts/measure-fixed-eval.ps1 -ResultsDirectory <本轮专属目录>`（catalog 应仍 **76**；它会往该目录写 `fixed-eval-coverage.json`，别复用别人的 run 目录）、`git diff --check` 的当场输出。
5. 交付说明逐条列出：动过的文件与为什么、**构建／全量各几次（期望＝1 和 1）**、没做到的点、你认为薄弱的点。

## 不在本单范围（等作者，或押到下一轮）

- **真·依赖锁定**（中央包管理／`packages.locked.json`）：要联网 restore、牵动 `scripts/package-release.ps1`，属不好回退的一类；作者 10-22 明示"不要让我拍板"⇒ 守门人按推荐**押到第一次真打包之前**再做，本单不许动。
- HDS-003 的真实模型对照、HDS-004 的灰度时机（U-9）：要占他机器算力或额度，动手前仍需作者点头。
- `DocxProcessingTimeoutMs`（15 秒档）：要判它得给 Core 注时钟接缝或造真慢的盘＝新功能，不在本单。
- `QA-TIP1020-02` 的实现仍悬在他人索引（63/0），不派给实现方；QA 欠的九把独立复采由作者定构建时点。
