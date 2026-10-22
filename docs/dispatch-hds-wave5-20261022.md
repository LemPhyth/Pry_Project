# HDS 第五波派单（2026-10-22）｜客户端边缘路由判据归位 ＋ 三处反证缺口补证 ＋ 限额表最后一格边界 ＋ 一把零构建文档刀

派单人：守门人（Architecture / Integration / Release）。执行方式＝**守门人自起子 Agent**（作者 2026-10-22 逐字「你，还是自己起Agent吧，你这派单跟我组织Agent的方式完全不一样」），工单是给冷启动执行用的，不交作者转派。
坐标系＝`docs/plans/PROJECT_PLAN.md` §4.2；本波不新开功能面，只做**四件已经暴露、且不依赖作者裁决**的事：把一把落后 46 格的判据接回来、把三处"只由内嵌样张反证过"的门补上真突变、把总盘最后一格能判的边界判掉、更正备忘里已过期的措辞。

基线起点＝**派单时的线尾**（本单出闸时线尾＝第四波归位后的 `5593e1a`，其上另有守门人的登记提交；起笔前 `git log --oneline -1` 当场取线尾，别抄这里的号）。
**出口顶账起点当场写死＝Core 527／Api 274／App 233／Client 106＝总 1140，失败 0、通过 1138、两条环境跳过照旧同名**（守门人 2026-10-22 逐份读四份 `.trx` 采的）。**不要为了"确认起点"先跑一轮全量。**

---

## 全局约束（条文与第四波同源，逐条可判红；完整原文见 `docs/dispatch-hds-011c-20261022.md` §全局约束）

1. 开工前 `git status --short --branch`；主工作区索引里他人**已暂存**的两枚文件不属于你（本轮实测＝`docs/dispatch-tip1020-02-memory-group-judgment.md` 1/1、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs` 63/0）——不 stage、不提交、不回退、不碰。
2. 独立 worktree 开工（守门人已备好就用，没备好就 `git worktree add .local-notes/<单名> -b codex/<单名> <线尾>`），先 `git rev-list --count <线尾>..HEAD` 数基线。
3. **整个载体只允许一轮解决方案级完整构建＋一次全量测试。** 迭代只用单工程 `dotnet build`；反证一律定向 `--filter` 跑。**不要**把 `-t:Rebuild` 传给解决方案／多项目（本机 SDK 10.0.200 误解析报 MSB1008）；等效写法＝单 csproj 构建＋同 csproj `--no-build --filter`。**这一条是本波存在的主要理由**：作者机器 10-22 已经弹过两轮，任何"再多跑一轮看看"都不许出现。
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

1. 更正 `docs/narrative-open-items-memo.md` 里已过期的措辞：**U-2／U-3／U-12**（目录锁真实形态、目录代次与实例写权代次的联动、目录锁与未知高版拒写门的先后）三格要按已落地事实改口径——第三波片 A 已经落了**进程级独占写锁**（`runtime-write.lock`，`DataDirectoryWriteLease.Acquire` 在 `StartAsync` 第一步，`AssertWriteAllowed` 在每次写前复查；契约 §10.1 已改写为落地后事实），第四波片 B 把高版拒写走可呈现异常。**只改"已被既有代码回答到什么程度"和指认的行号**，不许新增未实现的承诺，不许动其余九格的口径。
2. 交叉核对 `docs/resource-limit-ledger.md` 判据栏里点名的**用例名与文件是否真的存在**（这张表的守卫门只钉限额常量的 `文件:行号`，不钉"判据那一列写的用例名"）。把对不上的逐条列进交付说明；若确有对不上，**只改表里那一格为真实形状**（写"无"也行），不许新增断言。

交付形态：F1～F3 各一刀（`test:`／`test:`／`test:`，提交信息点名 HDS-011d-F1…F3 或沿用 HDS-011c 编号由你定，但要能对上本单），F4 单独一刀（`docs:`）。F2 的突变**一律不进提交**。

## 出口证据（一次跑齐）

1. 四份 `.trx` 的 `<Counters>`：总／四分项／失败／点名跳过；起点＝527／274／233／106＝1140，顶账写法＝"某分项 X→X＋N，N＝本刀新增几例（逐文件 `grep -c '[Fact]'`，再数 `[Theory]`/`[InlineData]`）"。
2. 完整 `dotnet build Pry.slnx -c Release` 那轮的 0 警告 0 错误。
3. F1／F3 每条新判据的红例全名＋原文；F2 三条真突变的红例全名＋原文＋还原证据。
4. `scripts/verify-repository.ps1`、`scripts/measure-fixed-eval.ps1 -ResultsDirectory <本轮专属目录>`（catalog 应仍 **76**；它会往该目录写 `fixed-eval-coverage.json`，别复用别人的 run 目录）、`git diff --check` 的当场输出。
5. 交付说明逐条列出：动过的文件与为什么、**构建／全量各几次（期望＝1 和 1）**、没做到的点、你认为薄弱的点。

## 不在本单范围（等作者，或押到下一轮）

- **真·依赖锁定**（中央包管理／`packages.locked.json`）：要联网 restore、牵动 `scripts/package-release.ps1`，属不好回退的一类；作者 10-22 明示"不要让我拍板"⇒ 守门人按推荐**押到第一次真打包之前**再做，本单不许动。
- HDS-003 的真实模型对照、HDS-004 的灰度时机（U-9）：要占他机器算力或额度，动手前仍需作者点头。
- `DocxProcessingTimeoutMs`（15 秒档）：要判它得给 Core 注时钟接缝或造真慢的盘＝新功能，不在本单。
- `QA-TIP1020-02` 的实现仍悬在他人索引（63/0），不派给实现方；QA 欠的九把独立复采由作者定构建时点。
