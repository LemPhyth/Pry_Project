# 工单 HDS-WAVE38-1029｜HDS-009 第一片第六刀：补 `:245` 那半句断言面（M5c）＋两枚扫描格的反面探针（S1／S2）＋一轮收尾全量（证据刀·续跑，零落地、零归位、零顶账）

- 工单 id：`HDS-WAVE38-1029`
- 性质：**证据刀·续跑**（第三十七波把波 33 遗留的 7 枚格换手全部结清、26 枚终账到 **26／0／0**；这一把只收三件该波没拿到读数的东西＝`:245` 的单独断言面、两枚扫描格"把扫描函数本身弄坏"那一半、以及一次出口全量）
- 任务编号：`HDS-009`（登记处＝`docs/plans/PROJECT_PLAN.md` §4.2 的 HDS-009 行；本刀出处＝`docs/eval/reports/2026-10-28-hds009-wave37-m-second-hand-red.md` 守门人 §九.5 的押账与 §九.4 的自纠）
- 出闸：守门人 2026-10-29 00:5x。作者原令逐字＝「我不想思考了，你就继续给我派工单，该提交的提交吧」＋「你，还是自己起Agent吧」＋「继续工作，不要让我拍板啥的，告诉我现在是什么情况就行」＋「下一次不要再三构建触发那个防火墙烦我打游戏」＋「游戏基本都是全屏应用，看当前桌面焦点是不是全屏应用就行」
- 执行方：冷启动 lane（载体＝沿用第三十三波那棵，**不新建 worktree**）
- **这一刀对作者可见的东西＝零**：净新增判据格 0、产品字节终态净写入 0、界面与对话里什么都不会变。它填的是三条"从没单独响过"的断言面。

## 〇 开工前必读与入场（缺一条就就地停刀登记，不要自扩范围）

1. **先读八份**：`docs/plans/README.md`、`docs/plans/PROJECT_PLAN.md`（§4.2 的 HDS-009 行＋§9 最新一条 entry）、`docs/agent-engineering-standards.md`（全文，交付形态按 §10 四项）、`docs/eval/reports/2026-10-28-hds009-wave37-m-second-hand-red.md`（上一把十一项写法＋守门人 §九.0 仪器自证／§九.1 七件判定／§九.2 划账／§九.3 两双手同场对账／§九.4 守门人自纠／§九.5 押账——**本刀三条配方全部从 §九.4、§九.5 派生，不在波 33 配方内**）、`src/Pry.Core/Narrative/StateAndLearning/` 五枚产品文件（逐行读）、`tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs`（判据本体 445 行、26 枚格；**本刀只有 S1／S2 两轮临时覆盖它，逐轮还原**）、`.local-notes/wave38-1029/mutation-prep/`（守门人预合成的**三枚**突变文本）与 `.local-notes/wave38-1029/gk38-prep.js`（含 `expectOld` 逐枚相等断言，改前原文从 blob 现取）。
2. **入场读数当场贴进报告 §〇**（不许引用本工单的数字）：`date`、`git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD`、`git status --short --branch` 原样、`git worktree list`、`git log --oneline -3`。
3. **载体＝沿用第三十三波那棵**：`.local-notes/qa-wave33-1028/carrier`（基点 `fd93ebe`）。入场自证五件：① `rev-parse HEAD`；② `status --short`＝**0 行**；③ `log --oneline fd93ebe..HEAD`＝**2 行**；④ `diff --stat fd93ebe HEAD -- src tests scripts Pry.slnx Directory.Build.props`＝**6 files changed, 822 insertions(+), 0 deletions**；⑤ 主树侧 0 行式子 `git -C <main> diff --stat 51f149d $(git -C <main> rev-parse HEAD) -- src tests scripts Pry.slnx Directory.Build.props`＝**0 行**（右端当场取，**不许在本工单里预写 SHA**）。⑤成立⇒载体树与主线归位后的产品／判据字节逐字符相同，本刀在载体里取到的红就是主线那棵树的红。
4. **六枚文件入场 SHA-256 基准**：当场 `sha256sum` 三侧各取一次（载体工作副本＝`git show 51f149d:<path>` 的 blob＝主树工作副本），三侧逐字符相同才起跑。判据那枚的基准值＝波 35／36／37 全程对齐的那枚。
5. **在册基线起点＝当场从波 37 报告守门人 §九.2 读到的那个数**（1406／executed 1404／failed 0／跳过 2，波 34／36／37 已三次同值复采）。本刀**净新增格数＝0**，F 轮出口读数应当**仍是 1406**：读到别的数先怀疑树没还原干净，不改判据、不改基线、照实登记并作废那一轮。
6. **他人资产**：主树那两枚 `M ` 暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）与全部 `??`＝别人的，不读内容、不改、不提交、不 `stash`／`clean`／`rm`。**共享主树里禁止任何 `git commit`／`add`／`checkout`／`reset`／`cherry-pick`／`merge`／`tag`／`push`／`git apply`／`stash`／`clean`／`rm`**；写只许落在载体里（三轮临时覆盖、逐轮按字节还原）＋你自己那一份报告文件。**本刀归位是空转**：终态净写入 0 字节⇒无代码刀、`main` 不因本刀挪。
7. **焦点预检（这条会打断作者本人，违反＝本波作废）**：每一次 `dotnet`（含每一枚突变轮与收尾全量）之前先跑

   ```
   powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1
   ```

   整段输出贴进报告并归档 `.local-notes/wave38-1029/out/preflight-<ID>.txt`，**编号从 P91 起**（P1–P90 已被前十一把用掉）。只有 `VERDICT=BUILD_OK` 才起跑。读到 `HOLD`：**不起跑、不请示、不改判据、不绕过**；先把零 `dotnet` 的活做完（§〇 全部、§三.0R 的并排自证、仪器九面自证），然后**可以重跑预检**——预检本身不设上限，只对"HOLD 之后的重跑"设上限：**最多累计 16 次、相邻两次不短于 5 分钟**（额度按刀计）。16 次后仍 `HOLD` ⇒ **带着"本刀零 `dotnet` 起跑"或"只跑到第 N 轮"的交付回来，那是合法交付**，收尾全量不跑＝基线不移动，不是短处。`free_memory_gb` 偏低或读数呈 SQLite `disk I/O`／`out of memory` 形态时那一轮读数作废（他机器资源形状，非断言失败），**不动任何进程**。

## 一 预算与口径

1. **预算**：解决方案级构建 **≤1**（若 §〇.3 ⑤ 那条 0 行式子成立、且载体 `src/Pry.Core/bin/Release/net10.0/Pry.Core.dll` 在盘、六枚 SHA 未变，**允许跳过并登记判定**）；解决方案级全量 **≤1**；定向 **≤3**；`dotnet` 总调用上限＝**5**。三轮一律**不许带 `--no-build`**。
2. `全量 ≤1` 的语义＝"交付的读数只许来自 1 次解决方案级全量"。作废轮的重跑不占这一格，但每轮全量都要逐轮登记：第几次调用、结果目录名、TRX 枚数、`<Counters>` 求和。
3. **作废轮产物一律改名留档，不许清空重跑**：`trx-<轮>-voided-<预检号>`。**满额形状＝定向 3×1＋全量 1×4＝7 枚 `.trx`**；报告 §六 要有"盘上枚数＝登记轮数×每轮枚数"的自校，差额逐处点名去处。
4. **取数仪器（口径⓪：先自证再用）**：① 只数 `<UnitTestResult … outcome="Failed">` 的**开标签**，与同份 `<Counters failed>` 双向对账；② 反向锚定面抓 `outcome="Failed"` 回溯最近 `<` 判标签名（`<ResultSummary>` 不算用例）；③ 红名先剥全限定前缀与参数表（`split('(')[0].split('.').pop()`）再回判据验"方法名＋`(`"存在；④ 比对预期红名时**先剥前缀再比**；⑤ **抽栈帧不许锚 `at `**（波 37 实测：TRX 原文是 `… in C:\…\File.cs:line 47`，`at` 后面跟的是方法名不是文件名，锚 `at` 会全部抽不到帧），正确形状＝`([\w.\-]+\.cs):(?:line |：行 )(\d+)`。**起跑第一轮之前先拿盘上已知读数自证仪器**，至少九面全对才允许用它裁这一把：波 33 `trx-M1`＝恰 1、`trx-M5`＝恰 1、`trx-M7`＝恰 1、`trx-F`＝4 份 failed 全 0 且求和 1406／1404／1404＋两条 `NotExecuted` 开标签点名、波 34 `trx-A1`＝恰 2、波 35 `trx-A3`＝恰 1、波 36 `trx-F`＝1406 同值、波 37 `trx-M2b`＝恰 1（红点 `:95`）、`trx-M7b`＝恰 1（红点 `:259`）、`trx-F`＝1406 同值。任一不符＝仪器坏了，**先修仪器，不改判据**。
5. **报告先建、逐轮落盘**（150 轮硬上限）：动第一次 `dotnet` 前把 §〇 写满建文件（**报告路径＝`docs/eval/reports/2026-10-29-hds009-wave38-assert-face-and-scan-bite.md`，主树里唯一允许新建的文件，不提交、不 `git add`**），之后每完成一轮立刻追加该轮十一项再跑下一轮。十一项＝①文件:行 ②改前原文 ③改后原文 ④`git diff --stat` ⑤跑法整行 ⑥TRX 文件名 ⑦红格全名＋消息要点＋红点行号 ⑧还原命令 ⑨还原后 `git status --short` 行数 ⑩突变态与还原后 SHA-256＋**本轮判据文件 SHA-256（apply 侧与 revert 侧各一次）** ⑪当轮预检整段。缺 ⑨⑩⑪ 退回不补签。
6. **定向跑法固定**（逐字照抄只换结果目录名，载体内，无 `--no-build`）：`dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave38-1029/out/trx-<轮>" --logger trx`。
7. **突变写入通道**：三枚文本由守门人预合成在 `.local-notes/wave38-1029/mutation-prep/<ID>_<basename>`，lane **只许整文件字节复制**应用（不许重排、不许改行尾、不许手写 replacement）；应用后立即 `sha256sum`，**必须等于 §三.0 该轮 SHA**，不等即整轮作废并如实登记；还原本一律 `git show 51f149d:<path>` 取字节再写文件（不许 `checkout`/`restore`），还原后 SHA 必须等于 §〇.4 基准。**本刀的写入目标包含判据文件（S1／S2），所以守卫的形态与波 37 相反**：仪器必须**按 §三.0 的白名单**放行 `S1`／`S2` 那两枚文本对 `tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs` 的写入，同时对**其它任何**判据写入（第三枚文本、手写 replacement、其它 ID）一律 REFUSED；并拿一次"试图用非白名单内容写判据"的调用真验出报错（整段原文进报告 §〇.6.2），不许只声称。
8. **新增两枚格子的署名边界（这一条是本刀的命门）**：S1／S2 两轮里判据字节确实被覆盖过（apply 侧 SHA≠基准），所以按既有口径**这两轮只结"门不是常绿"这一条机制面，不签任何第二换手**；那两枚扫描格的换手已由波 36 的 C1／C2 结清，本刀不去重跑 C 类、也不把 S1／S2 的红署成交换证据。M5c 一轮判据字节全程等于基准，它结的是 `:245` 那句断言的**第一次单独响**（断言级子账），不是新的一枚格。

## 二 允许的文件面（超出即作废）

- ① `.local-notes/wave38-1029/`（自建，gitignored）：报告草稿、仪器、manifest 与全部 TRX；**`mutation-prep/` 三枚与 `gk38-prep.js` 是守门人产物，lane 只读**；
- ② 报告 `docs/eval/reports/2026-10-29-hds009-wave38-assert-face-and-scan-bite.md`（唯一一份可写文档）；
- ③ **载体内**的 `src/Pry.Core/Narrative/StateAndLearning/DeliveryAttribution.cs`（M5c）与 `tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs`（S1／S2）——只作临时覆盖、逐轮按字节还原，终态相对 `51f149d` 净写入 0 字节；
- ④ **禁止**：新建任何产品文件、新建任何测试文件、改动除 §三.0 三枚文本以外的任何判据内容、改 `scripts/**`／`Pry.slnx`／`Directory.Build.props`／其他 `docs/**`（除②）／主树任何文件；不许把 §三.0 的预期列改成实测值。

## 三 本刀要跑的三轮（预期面＝守门人从 `51f149d` 干净树推演；**"预期枚数／格名／红形状"是假设不是结论**：实测与之不符⇒整轮作废、红不署、不追第二轮）

### 三.0 三轮表（行号一律 `51f149d` 侧现值；"预期红点"按波 37 §九.4.2 升级拆成两列——断言语句起始行／预期 TRX 帧行，**帧行只作辅证，主证＝testName＋消息**）

| 轮 | 类别 | 文件:行（干净侧原文） | 突变内容（守门人预合成，盘上实测 SHA） | 预期恰响格（前缀 `Pry.Core.Tests.StateAndLearningPrototypeTests.`） | 断言语句起始行 | 预期 TRX 帧行 | 预期消息形状 | 枚数 |
|---|---|---|---|---|---|---|---|---|
| M5c | 翻布尔（断言面补刀） | `DeliveryAttribution.cs:89`＝`            false);` | 整行替换为 `            true);`（**收尾 `);` 保留**——波 37 的 M5b 就是把 `);` 丢了才作废） | `A_promise_send_proposes_a_commitment_but_proves_neither_consent_nor_completion` | `:245` | `:245`（单行断言，与波 37 M5 实测 `:244` 同形） | `Assert.False() Failure` Expected False／Actual True，且 `:244`（`UserConsentProven`）**保持通过** | 恰 1 |
| S1 | 弄坏扫描函数（反面探针） | 判据 `:418-419`＝`LegacyChainNamesFoundIn` 的表达式体（`LegacyChainNames.Where(name => source.Contains(name, StringComparison.Ordinal));`） | 两行并成一行 `… => Enumerable.Empty<string>(); // [MUTATION S1 temporary, wave38] …`（净 −1 行） | `State_and_learning_prototype_sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites` | `:320`（`Assert.Equal(` 起始行，语句体 `:320-323`） | `:320`；实测落 `:321`–`:323` 之间任一行＝同族映射差，如实登记不作废（主证＝testName＋消息） | `Assert.Equal() Failure: Strings differ`，Expected＝七枚名字串 `BehaviorPlanner,…,LearningOutcome`，Actual＝**空串**（谓词被弄坏后扫描返回空） | 恰 1 |
| S2 | 弄坏扫描函数（反面探针） | 判据 `:423-424`＝`SourceSideDefaultsFoundIn` 体内两条 `if (…) yield return …;` | 两行并成一行 `        yield break; // [MUTATION S2 temporary, wave38] …`（净 −1 行，迭代器体大括号保留） | `State_and_learning_prototype_declares_no_source_side_numeric_default` | `:344`（`Assert.Equal("const,static readonly",` 起始行，语句体 `:344-345`） | `:344`；同上族映射差处置同 S1 | `Assert.Equal() Failure: Strings differ` Expected `"const,static readonly"`／Actual＝**空串** | 恰 1 |

盘上实测 SHA（本工单出闸前 `sha256sum` 与 node `crypto` 两路各测一次，逐字符相同才下发）：

- `M5c_DeliveryAttribution.cs`＝`4c3f6a61e20e2d3bb5254ee0ec03db487861c33b42c69fc986524da7e95c91bb`（4621 字节）
- `S1_StateAndLearningPrototypeTests.cs`＝`3b55a3893025bc66c5b773e627959b120d9b71add11b80a76df4bdf154cb00a6`（19861 字节）
- `S2_StateAndLearningPrototypeTests.cs`＝`5388104f3f0158bbb74c20f687cce16a5297924ded0544aa8d9c5405084ea4a3`（19758 字节）

**跑序固定＝M5c→S1→S2→全部还原并核六枚 SHA（判据必须回到 §〇.4 基准值）→F（收尾全量）。** 未跑满三轮不许起 F。每轮都预期**恰 1 枚红**；2 枚以上⇒整轮作废、红不署、把多出来的红名逐枚登记，不追第二轮。**恰 0 枚红同样要作废并登记**——尤其 S1／S2：如果弄坏谓词后那枚格仍然不红，说明探针句也没在守（那才是真缺陷），必须原样报上来而不是改预期。

### 三.1 我为什么判这三枚"跑得动"（消费点复算，从干净树逐枚读；lane 要在 §三.0R 独立复算一遍）

- **M5c**：`AttributePromise` 全判据只在 `:238` 一处调用；`AttributionReadout` 的第 5／第 6 枚位置实参是 `:88`（`UserConsentProven`）／`:89`（`TaskCompletedProven`），`:88` 保持 `false` 时 `:244` 通过、`:89` 翻向只打红 `:245`。波 37 的 M5 已用同一格拿到 `:244` 的单独红，这轮补的是**同一格的另一句断言**，两枚不许合轮。
- **S1**：`LegacyChainNamesFoundIn` 在判据里只有两个读点——循环体 `:310`（`Assert.Empty`）与探针体 `:323`（`string.Join` 进 `Assert.Equal`）。把函数弄坏成恒空后：`:310` 恒过（这正是"常绿门"的样子），`:320` 那句 `Assert.Equal(七枚名字串, string.Join(",", 恒空))` 必然红。**所以这一轮的红点必须落在 `:320`–`:323` 那一句**，落在别处或 0 枚红都说明我对判据结构的理解错了。
- **S2**：`SourceSideDefaultsFoundIn` 同样两个读点——`:336`（`Assert.Empty`）与 `:345`（探针 `string.Join`）。`yield break` 后 `:336` 恒过、`:344` 那句必红。
- **交叉面**：两枚被弄坏的函数都不参与其它 24 枚格（`LegacyChainNames`／`PrototypeFiles` 两个数组只被这两枚格与 `PrototypeSource` 读）；`Enumerable.Empty<string>()` 需要 `System.Linq`，判据文件已在用 `.Where(...)` ⇒ 不新增 using、不引入编译告警。
- **行号漂移面（波 37 那四处映射差的正面处理）**：三枚突变文本改的行都在预期断言行**之下或同一层**（`:418`／`:423` 在 `:320`／`:344` 之后；`DeliveryAttribution.cs:89` 在产品侧不移动判据行号），且 S1／S2 各自净 −1 行发生在 `:415` 之后 ⇒ **本刀预期的四个行号不因行数变化而漂移**。若实测红点落点比预期小 1，先怀疑自己抄错行而不是怀疑漂移。

### 三.2 三条配方的出处＝守门人这两只手（不是波 33 配方，也不是执行方的范围）

`:245` 的补刀出自波 37 报告 §九.4.1（我写坏了 M5b 的文本）；S1／S2 出自波 36 §九.5 与波 37 §九.5.2（"两枚扫描格欠的是把扫描函数本身弄坏那一半"）。**这一把定向只有三轮、`dotnet` 只有 5 次预算，多一枚都不许自己加**；觉得还有该跑的，写进报告 §七 交回，别动手。

## 四 本刀**不**跑的账（押给下一把，不许顺手碰）

1. **A 类／C 类／M1–M7 与 M2b／M7b 一枚都不许重跑**：那 26 枚格的红证与换手在波 33–37 已结清，重跑＝把别人拿到的红署到自己名下。
2. `:367` 那半句照旧不追（`LegacyInheritance.cs:53` 同一行返回同时喂 `:365`／`:367`，任何单点文本面突变必先红 `:365`）——**按"这个夹具的输入形状下没有可达配方"签到，不签"结构上永远够不着"**；要拆成两个不同拒因＝改判据或扩产品语义，属接线刀。
3. 阳性探针的**单独读数**：S1／S2 会让探针句自己响一次，那是"探针在守"的证明；仍欠"探针句单独被跑出读数"的正数面（波 36 §九.3.2 那条），要新增判据才能结，本刀不新增。
4. `PrototypeFiles` 写死五枚清单的**覆盖盲区**（新加产品文件不在这个清单里就扫不到）、两套并行命名债（`Hds006*` 产品类型名＋契约 §7.2 的 `CognitionKind`／`CognitionOrigin`）、波 28 那枚数值锁格（**不许为凑红造门槛**）、HDS-007／HDS-009 的生产接线、HDS-010 删除片、**旧欠 75 格（波 13 余 47＋波 20 余 28）的换手账**——照旧挂着。

## 五 交回给守门人核的六件事（不在本 lane 权限内）

1. 三轮各自是否**恰 1 枚红**、红格全名是否逐字符等于 §三.0、红点是否落在 §三.0 那两列行号之一；**批次红不许署给单枚突变**。
2. M5c 一轮的判据 SHA（apply／revert 两枚）是否等于 §〇.4 基准；S1／S2 两轮的 apply 侧 SHA 是否等于各自预合成文本的实测值、revert 侧是否**回到基准**（这两轮按 §一.8 **不签换手**，只签机制面）。
3. 载体终态：`status --short`＝**0 行**、`fd93ebe..HEAD` 仍只有波 33 那两枚提交、六枚 SHA 逐字符等于 §〇.4（判据必须＝基准值）。
4. 收尾全量四份 TRX 先数满再求和，应当**仍＝1406／executed 1404／failed 0／跳过 2**（两条同名跳过按 `outcome="NotExecuted"` 点名）。**本刀不顶账**，也不许为顶账另起构建。
5. `.trx` 枚数与轮数对账（满额 7 枚；差额逐处点名去处）；三计数出场复看必须仍是 **23／1／3**（净新增 0）。
6. §一.7 那条白名单守卫的证明：是否拿"非白名单内容写判据"的调用真验出报错（整段原文），以及有没有人偷偷改断言、改 `[InlineData]` 期望值、`Assert.Skip`、注释既有断言、新增数字门槛。

## 六 未验证（照旧逐条点名，不用"已完成"混过去）

真实模型对照（零读数，需作者一句"可以跑"）；**生产接线**（这 26 枚至今零生产调用方，要接的 005 落库面与 004 生命周期一律没接；接线时机＝U-9 残句「切换那一步她要不要做什么」仍未拍）；旧数据真实继承（零读取）；UI 可见行为（零读数）；生产库与 `%LOCALAPPDATA%\PryCompanion`（零接触）；待标定参数权威值（关系／心情各档阈值、经验有效期、点击归因窗口全部未定＝**特性不是缺陷**）；并发与断电（零读数）；旧链删除片与"构建时不把旧链打进包"那道门（**未开始**）；`:367` 与阳性探针单独读数（§四.2／§四.3）；Release 序列点把红点记到多行调用起始行的机理（波 35／36／37 已知形状，不影响署名）；**本刀不签任何换手、不签 26 枚结案、不写状态词**。**四句话照旧不许出现在任何页面**＝新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链。旧欠 75 格照旧不减；拆法原文照旧 46／45／1＝W2。

## 七 不许走的捷径（逐条对照前十一把踩过的坑）

1. 不许新增判据格、不许改断言内容（S1／S2 只许把两个**仪器函数**弄坏，且必须用我预合成的那两枚文本）、不许改 `[InlineData]` 期望值、不许 `Assert.Skip`、不许注释既有断言、不许改上游门。
2. 不许手写 replacement；不许"顺手把 M5c 与 S1 合轮"；不许带 `--no-build`。
3. **静态判读命中"语法不完整"级缺陷时的第三选项**（波 37 新增）：若 §三.0R 的并排自证发现某枚预合成文本与它替换的那一行原文不吻合、或括号／大括号不闭合，**不起那一轮**——把缺陷面（盘上行号＋现文）逐字符登记进报告，定向额度原样退回，继续跑其余轮，收尾全量仍按"跑满能跑的轮次之后"起。**不要重复波 37 那次"明知语法坏还跑一轮再作废"**：那一枚 `dotnet` 占用的是作者的窗口。
4. 不许把 §三.0 的预期列改成实测值；实测与预期不符（含 0 枚红）⇒ 那一轮作废并如实登记。
5. 不许删作废轮产物、不许清空结果目录重跑、不许在报告里替守门人写状态词／结案词／自封在册基线。
6. 不许碰他人未提交改动、暂存项、未跟踪资产；共享主树禁裸 `git commit`。
7. 不许顺手跑 §四 那四本押账；**HOLD 重跑额度只约束预检脚本的重跑次数**，`dotnet` 只在 `VERDICT=BUILD_OK` 之后起。
8. 预合成文本的 SHA 对账**不能替代并排自证**（波 37 教训：SHA 只证明复制到盘上的字节＝我声明的字节，不证明我声明的东西是对的）。

## 八 交回与划账（本 lane 写 §〇–§七，守门人追加 §九）

（守门人在此预登记划账面：① 三轮形状与红名归属逐格点名，M5c 的红点必须落 `:245` 且 `:244` 保持通过；② 断言级子账现值更新＝已结 2 → **3**（`:95`／`:259`／`:245`），未结 3 → **2**（`:367`、探针正数面单独读数）；③ S1／S2 两枚扫描格的机制面判定＝"门不是常绿"能否成立（凭据必须包含"弄坏谓词后 `Assert.Empty` 那一半恒过、探针那一半红"这一形状），且**这两轮的判据字节被写过⇒不签换手**，这条边界要在 §九.2 明写；④ 1406 是否第四次同值复采（本刀不顶账）；⑤ `.trx` 枚数对账与构建跳过判定三段；⑥ 定向 3 轮／`dotnet` 5 次是否越预算，HOLD 额度是否被用成放宽；⑦ 第三选项（§七.3）这一把没用上就照实写"未动用"，不许读成"以后可以跑坏文本再作废"；⑧ 出闸后的账面目：HDS-009 第一片证据账在 §九.2 那句 26／0／0 之上只增加断言级子账，**不新增格子**。）
