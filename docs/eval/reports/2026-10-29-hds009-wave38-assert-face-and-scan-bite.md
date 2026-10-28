# HDS-009 第一片·第六刀（波 38）报告：补 `:245` 那半句断言面（M5c）＋两枚扫描格的反面探针（S1／S2）＋一轮收尾全量（HDS-WAVE38-1029）

- 工单：`docs/dispatch-hds-wave38-1029.md`（本刀唯一权威工单，逐字读完）
- 执行方：冷启动 lane（载体＝沿用第三十三波那棵 `.local-notes/qa-wave33-1028/carrier`，**未新建 worktree**）
- 性质：证据刀·续跑（净新增判据格 0、产品字节终态净写入 0、零落地、零归位、零顶账）
- 本刀只跑：M5c→S1→S2→全部还原并核六枚 SHA→F（收尾全量）；A 类／C 类／M1–M7／M2b／M7b 一枚不重跑（工单 §四.1）
- S1／S2 两轮**动了判据字节** ⇒ 按工单 §一.8 这两轮只结"扫描门不是常绿"的机制面，**绝不署名为第二换手**；本 lane 不自封"已有另一双手"

> 状态词、结案词、在册基线判定、划账由守门人写，本 lane 不代写。四句话照旧不写＝新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链。

## 〇 开工前必读与入场

### 〇.1 开工前必读（全部读完整页；本节只登记"读了什么"，不引用其数字作为本刀读数）

1. `docs/dispatch-hds-wave38-1029.md`（本刀唯一权威工单，逐字读完：载体＝沿用波 33 那棵、预算 `dotnet` ≤5／定向 ≤3／全量 ≤1／构建 ≤1（三段成立可跳过）、HOLD 重跑 ≤16 且间隔 ≥5 分钟、跑序 M5c→S1→S2→核六枚 SHA→F、三轮各自预期恰响格名／两列行号／红形状／枚数、盘上三枚预合成文本位置与 SHA、报告路径、§一.4 仪器九面自证清单与"抽帧不许锚 `at `"、§一.7 字节复制通道＋判据白名单守卫＋REFUSED 实证、§三.2 三条配方出处、§四 四本押账、§七 八条不许走的捷径含 §七.3 第三选项、§八 守门人预登记划账面）。
2. `docs/plans/README.md`（现行工作规则权威副本 14 条）与 `docs/plans/PROJECT_PLAN.md`：§4.2 的 HDS-009 行（第 82 行，在册文字把第六刀记为"`docs/dispatch-hds-wave38-1029.md` 在飞"）＋§9 最新三条 entry（收第三十七波／收第三十六波／收第三十五波，第 186／196／206 行起）。主树 `docs/plans/PROJECT_PLAN.md` 工作副本带着他人 18 行在途研究登记（` M` 状态），本刀只读未碰。
3. `docs/agent-engineering-standards.md`（全文；交付形态按 §10 四项：问题与根因／实际改动／验证结果／剩余问题；§7"没有条件完成的验证应标记为未验证"落到本刀 §五）。
4. `docs/eval/reports/2026-10-28-hds009-wave37-m-second-hand-red.md`（上一把十一项写法＋守门人 §九.0 仪器自证与两处仪器错／§九.1 七件判定／§九.2 划账（含断言级子账现值）／§九.3 两双手同场对账／§九.4 守门人自纠（M5b 文本写坏、行号混写、lane 那一难的裁定）／§九.5 押账——本刀三条配方全部从 §九.4 与 §九.5 派生，不在波 33 配方内）。
5. `docs/eval/reports/2026-10-28-hds009-state-and-learning-prototype.md` §三.0（M1–M7 原始配方与原始论证七行；本刀不重跑它们，只据其"消费点数枚数"的口径做 §三.0R 的独立复算）。
6. `docs/eval/reports/2026-10-28-hds009-wave36-red-handover-remainder.md`（守门人 §九.0 仪器口径／§九.1 七件判定／§九.2 划账／§九.3 三处形状判定（含 §九.3.2 把"探针句单独读数"这条欠账收窄但不销）／§九.5 押账——S1／S2 的"反面探针"缺口出处；该报告十一项写法与波 37 同形）。
7. `src/Pry.Core/Narrative/StateAndLearning/` 五枚产品文件（逐行读：`StateLayering.cs` 102 行／`NarrativeAuthority.cs` 46 行／`DeliveryAttribution.cs` 94 行／`ExperienceMaterial.cs` 57 行／`LegacyInheritance.cs` 78 行；本刀只临时改坏 `DeliveryAttribution.cs`，逐轮还原）。
8. `tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs`（判据本体 445 行、26 枚格；**本刀 S1／S2 两轮临时覆盖它并按 `51f149d` 的 blob 逐字节还原**——这两轮因此不签换手，见开头口径行）。
9. `.local-notes/wave38-1029/mutation-prep/`（守门人预合成三枚突变文本）与 `.local-notes/wave38-1029/gk38-prep.js`（含 `expectOld` 逐枚相等断言与"改前行内容"并排自证）——本刀**只读、只按字节复制应用，不重造、不改写任何 replacement**。

### 〇.2 入场读数（当场取，未引用工单数字）

- `date`：`Thu Oct 29 00:35:47 2026`（第一次取）
- `git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD`：`1d2c0332317be9abbe9742d92c2e3313324fbacf`（＝波 38 工单出闸那枚文档刀线尾；本刀入场至交付期间主树线尾是否再动，出场复取见 §六）
- `git status --short --branch`（主树原样）：
  ```
  ## codex/ui-main-window-redesign
  M  docs/dispatch-tip1020-02-memory-group-judgment.md
   M docs/plans/PROJECT_PLAN.md
  M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs
  ?? .qoder/
  ?? docs/character-learning-safety.patch
  ?? docs/research/HDS_INTERLUDE.md
  ?? docs/research/HDS_QQ_RESEARCH_INDEX.md
  ?? docs/research/QQ_BRIDGE.md
  ```
  两枚 `M ` 暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）与全部 `??`＝他人资产：本刀不读内容、不改、不提交、不 `stash`／`clean`／`rm`。` M docs/plans/PROJECT_PLAN.md` 是在册总计划的在途登记（他人 18 行研究条目），不属本刀文件面，只读未碰。
- `git worktree list`（与本刀有关的两行；余 70+ 条均为他人 worktree，未动）：主树 `C:/Users/24782/Desktop/Pry_Project 1d2c033 [codex/ui-main-window-redesign]`；载体 `C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave33-1028/carrier 51f149d (detached HEAD)`＝本刀沿用，**未新建 worktree**。
- `git log --oneline -3`（主树）：`1d2c033`（收第三十七波＋出闸第三十八波）→ `db064e6`（收第三十六波）→ `fa6d24f`（收第三十五波）。

### 〇.3 载体入场自证（右端一律当场 `git rev-parse HEAD` 取，不预写 SHA）

- `git -C <carrier> rev-parse HEAD` = `51f149d0b722e28a6566c93d19f48a94ca4c85df` ✓（等于工单指定线尾）
- `git -C <carrier> status --short` = **0 行**（当场 `| wc -l`＝0，输出为空）✓
- `git -C <carrier> log --oneline fd93ebe..HEAD` = **2 行** ✓（`1e02205` 落六枚新文件＋`51f149d` 一处判据整形）；本刀零提交，出场仍应是这 2 行。
- `git -C <carrier> diff --stat fd93ebe HEAD -- src tests scripts Pry.slnx Directory.Build.props` = **6 files changed, 822 insertions(+), 0 deletions** ✓（逐枚行数 94／57／78／46／102／445）
- 主树侧那条 0 行式子（右端＝当场 `git rev-parse HEAD`＝`1d2c0332317be9abbe9742d92c2e3313324fbacf`）：`git -C <main> diff --stat 51f149d 1d2c033 -- src tests scripts Pry.slnx Directory.Build.props` = **0 行**（当场跑，`| wc -l`＝0，输出为空）✓ ⇒ 载体树与主线归位后的产品／判据字节逐字符相同，本刀在载体里取到的红就是主线那棵树的红。
- 写入前 EOL 形状复核（决定"按字节还原"能否成立）：`git show 51f149d:` 的 blob 字节与载体工作副本逐枚 SHA 相同（见 §〇.4 三侧同值）⇒ 工作副本字节＝blob 字节，还原写回 blob 原字节即等价于 checkout 形状，本刀全程零 `checkout`／零 `restore`。

### 〇.4 六枚文件入场 SHA-256 基准（当场各取一次：载体工作副本＝`git show 51f149d:<path>` 的 blob＝主树工作副本；三侧逐字符相同才起跑）

| 文件（相对路径） | 主树工作副本 ＝ 载体工作副本 ＝ `51f149d` blob（三侧同值） |
|---|---|
| `src/Pry.Core/Narrative/StateAndLearning/StateLayering.cs` | `af727a9c04355d853f756a8cd6d22e374ac889042c847f99906784e0bb20ca3a` |
| `src/Pry.Core/Narrative/StateAndLearning/NarrativeAuthority.cs` | `000f2fc8bfd5d73ba7863029c8acafd7aa9df92ef11c910ccda3a28db3f13c97` |
| `src/Pry.Core/Narrative/StateAndLearning/DeliveryAttribution.cs` | `5858a75da3ed70e35f93782e1d760c87eea3feeecdafcdd645b27f69ea66f0ab` |
| `src/Pry.Core/Narrative/StateAndLearning/ExperienceMaterial.cs` | `289fc89be087fa35bace12b33805060562a210764a9774102b5a2875fcd55169` |
| `src/Pry.Core/Narrative/StateAndLearning/LegacyInheritance.cs` | `6a1f50999e7caf1d6d29cc07bbe752f89ea17851999013c00b167e590f3498ab` |
| 判据 `tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs` | `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc` |

三侧逐字符相同当场核过（脚本输出六枚×三侧全配）。判据那枚的基准值 `317d2171…` ＝波 35／36／37 全程对齐的那枚。S1／S2 两轮的 apply 侧 SHA 必然不等于此基准（这正是这两轮不签换手的凭据），revert 侧必须回到此基准；M5c 与 F 期间判据必须逐字节等于 `51f149d` 的 blob `317d2171264f…`。

### 〇.5 在册基线起点（当场从波 37 报告守门人 §九.2 读到的那个数，不做加法）

- 波 37 报告守门人 §九.2 原文：「在册基线：**1406／executed 1404／failed 0／跳过 2** 不动（第三次同值复采＝读数不是顶账）」；波 34 第一次、波 36 第二次同值复采。本刀 §〇.5 起点＝**1406／executed 1404／failed 0／跳过 2**（工单 §〇.5 分项另列 Client 108／App 233／Api 280／Core 785、Core executed 783——本刀不引用其数字，F 轮自己数）。
- 本刀**净新增格数＝0**（§二当场三计数为凭），所以 F 轮出口读数应当**仍是 1406**：读到别的数先怀疑树没还原干净，不改判据、不改基线、照实登记并作废那一轮。

### 〇.6 取数仪器与写入仪器（口径⓪：先自证再用；两份都是本 lane 当场新写）

仪器文件＝`.local-notes/wave38-1029/w38-count.py`（取数）与 `.local-notes/wave38-1029/w38-apply.py`（写入）。**不复用 wave33／34／35／36／37 任何一份实现**；本刀只读过它们的报告文字与通道描述（波 37 §〇.6、波 36 §九.0、波 37 守门人 §九.0.2 那条"抽帧不许锚 `at `"的口径升级），代码一行没有拷贝，两份文件从零写起。

取数口径＝① 只数 `<UnitTestResult … outcome="Failed">` 的**开标签**：逐 occurrence 找 `<UnitTestResult`，引号感知扫到标签尾 `>`（自闭合 `…/>` 与成对 `…>…</UnitTestResult>` 同一切法，Core 的结果结点多为自闭合），从属性段取 `outcome`／`testName`；**全程不用开—闭配对正则做计数**；② 反向锚定面（face B）独立各数＝逐 occurrence 找 `outcome="Failed"` 回溯最近的 `<` 判标签名（`<ResultSummary>` 这类不算用例），A≠B 即打印 `SELF-CHECK MISMATCH`；③ 红数与同份 `<Counters failed>` 双向对账，不等即打印 `DISAGREE`；④ 红格名先剥全限定前缀与参数表（`split('(')[0].split('.')[-1]`）再回判据源码验"方法名＋`(`"真实存在；比对预期红名时**先剥前缀再比**；⑤ 抽栈帧**不锚 `at `**：`([\w.\-]+\.cs):(?:line |：行 )(\d+)`（TRX 原文形状＝`… in C:\…\File.cs:line 47`，`at` 后跟方法名）；⑥ 消息与 `NotExecuted` 结点同样按开标签点名。

**动第一次 `dotnet` 之前先拿盘上已知读数自证**（工单 §一.4 九面：波 33 `trx-M1`／`trx-M5`／`trx-M7` 各恰 1、波 33 `trx-F`＝4 份 failed 全 0 且求和 1406／1404／1404＋两条 `NotExecuted` 开标签点名、波 34 `trx-A1`＝恰 2、波 35 `trx-A3`＝恰 1、波 36 `trx-F`＝1406 同值、波 37 `trx-M2b`＝恰 1（红点 `:95`）、`trx-M7b`＝恰 1（红点 `:259`）、波 37 `trx-F`＝1406 同值）。整段输出存档 `out/instrument-selfproof.txt`，逐面判定见 §〇.6.1。

写入通道＝`apply <ID>` 把 `mutation-prep/<ID>_<basename>` 的**整文件字节**复制到载体目标路径（不重排、不改行尾、不手写 replacement；复制前先验盘上文本 SHA 等于工单 §三.0 声明值，不符即不写一个字节并 ABORT）；`revert <ID>` 把 `git show 51f149d:<path>` 的 blob 字节原样写回（零 `checkout`／零 `restore`）；`verify` 六枚逐字符对 blob。**守卫形态与波 37 相反**（工单 §一.7）：写入目标为判据文件时，只放行两种字节＝白名单 `S1`／`S2` 本体（SHA 必须等于各自预合成声明值）或还原通道的 `51f149d` blob（SHA 必须等于 §〇.4 基准）；其它任何 ID、第三枚文本、手写 replacement、任意字节 ⇒ `PermissionError: REFUSED`，一个字节都不写。守卫实证见 §〇.6.2。

### 〇.7 焦点预检与预算声明

- 预检脚本＝`.local-notes/game-preflight6-1022.ps1`（在盘，`-rw-r--r-- … 9675 Oct 22 21:15`）；每一次 `dotnet`（含每一枚突变轮与收尾全量）之前当场重取；编号从 **P91** 起（P1–P90 已被前十一把用掉）；整段输出贴进报告并归档 `out/preflight-P<编号>.txt`。只有 `VERDICT=BUILD_OK` 才起跑；读到 `HOLD`＝不起跑、不请示、不改判据、不绕过，先把零 `dotnet` 的活做完，HOLD 后重跑最多累计 **16** 次、相邻两次不短于 **5 分钟**（额度按刀计）。16 次后仍 HOLD ⇒ 带着"只跑到第 N 轮"的交付回来，收尾全量不跑＝基线不移动，合法交付。`free_memory_gb` 偏低或读数呈 SQLite `disk I/O`／`out of memory` 形态时那一轮读数作废（他机器资源形状，非断言失败），不动任何进程。
- 预算：解决方案级构建 ≤1；解决方案级全量 ≤1；定向 ≤3；`dotnet` 总调用上限＝**5**。三轮与 F 一律**不带 `--no-build`**。多一枚都不自己加（工单 §三.2）。
- **B1（解决方案级构建）跳过判定登记（三段当场核过）**：① §〇.3 主树侧 0 行式子成立 ✓（右端 `1d2c033` 当场现取）；② 载体 `src/Pry.Core/bin/Release/net10.0/Pry.Core.dll` 在盘 ✓（登记见 §六）；③ 六枚 SHA 自入场未变 ✓（§〇.4 三侧同值＋写入前零改动）。⇒ 按工单 §一.1 合法跳过 B1，构建预算用 **0/1**；三轮定向与收尾全量由 `dotnet test` 自行增量重建。作废轮（如有）结果目录一律改名留档 `trx-<轮>-voided-<预检号>/`，不清空、不删除、不重跑同名目录。
- 动第一次 `dotnet` 之前完成的零 `dotnet` 活（本节即凭据）：§〇 全部＋§二 三计数＋§三.0 三行论证逐枚盘上复核（§三.0R）＋三枚预合成文本当场 `sha256sum` 与工单 §三.0 逐字符对账（§三.0P）＋三枚文本对两枚扫描谓词的交叉面自扫＋**三枚文本的静态语法判读**（§三.0R.5，工单 §七.3 第三选项）＋仪器九面自证（§〇.6.1）＋写入仪器判据路径守卫的报错实证（§〇.6.2）。

### 〇.8 他人资产与 git 写边界声明

主树全程零 `git commit/add/checkout/reset/cherry-pick/merge/tag/push/apply/stash/clean/rm`；两枚 `M ` 暂存项与全部 `??` 未读不改。载体内只发生文件内容写入（三轮临时覆盖＝整文件字节复制）与逐轮按 `git show 51f149d:<path>` 的 blob 字节还原，本刀**零提交**（线尾仍应是 `51f149d`）。主树唯一新文件＝本报告，不 `git add`、不提交。本刀归位是空转：终态净写入 0 字节⇒无代码刀、`main` 不因本刀挪。

### 〇.6.1 仪器九面自证（动第一次 `dotnet` 之前跑，零 dotnet 调用；整段输出＝`out/instrument-selfproof.txt`，退出码 0）

自证只读盘上别人产出的 TRX，判据与 TRX 一个字节未动。九面各自展开成"结论字段逐项"的分项判定（红数／faceB==faceA／与 `<Counters failed>` 对账／红名回判据／红名逐字符／首帧行号／NotExecuted 枚数与点名），当场原文结论行：

```
SELFPROOF FACES=9 (each face expanded into 56 sub-readings) AGREED=56 DISAGREED=0 => ALL NINE FACES AGREE
```

九面全对（56 项分项读数零 DISAGREE），本刀仪器未被判坏才允许用它裁这一把。**两处我自己的仪器错（都按"仪器说读不出来先怀疑仪器"处置，判据与盘上 TRX 零字节改动，登记不涂改）**：① 首版 `decode()` 把十六进制实体按十进制解（`int('D')` ⇒ `ValueError`），崩在第一面产出任何结论之前，改 `int(m.group(1), 16)`；② 自证面里 `NotExecuted` 点名我误用"剥前缀"的短名去比对我写下的全名（三面 DISAGREE），改成按 TRX 原文全名逐字符比对——坏的是读数通道，不是盘上产物。另修一处格式化参数数目错（`"%s %s faceB==faceA" % (wave, rnd, …)`）。

### 〇.6.2 写入仪器判据路径守卫的报错实证（工单 §一.7 要的凭据；零 dotnet；整段原文＝`out/guard-probe.txt`，退出码 0）

调用命令＝`python w38-apply.py guard-probe`。四步：三枚**非白名单**写入尝试必须当场 REFUSED，第四枚正面控件（白名单内容配白名单 ID）必须放行、随即按 blob 还原。当场原文（前三枚 REFUSED 的报错体全文）：

```
probe 1: safe_write(tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs, b'GUARD PROBE - must never land', 'S3-hand-written')
CAPTURED PermissionError: REFUSED: S3-hand-written targets the criteria file tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs with non-whitelisted bytes (sha=8cc155ad9c64c76737cf9697018b375d0004d31dfa20de4ddbaf7bb6ce1f6f3b; whitelist=S1/S2 plus the 51f149d blob revert channel per dispatch 一.7); nothing was written.
probe 2: safe_write(tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs, <M5c prep bytes>, 'M5c')  # 第三枚文本走判据路径
CAPTURED PermissionError: REFUSED: M5c targets the criteria file tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs with non-whitelisted bytes (sha=4c3f6a61e20e2d3bb5254ee0ec03db487861c33b42c69fc986524da7e95c91bb; whitelist=S1/S2 plus the 51f149d blob revert channel per dispatch 一.7); nothing was written.
probe 3: safe_write(tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs, <S1 prep bytes>, 'S2')  # 白名单 ID 配错文本
CAPTURED PermissionError: REFUSED: S2 targets the criteria file tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs with non-whitelisted bytes (sha=3b55a3893025bc66c5b773e627959b120d9b71add11b80a76df4bdf154cb00a6; whitelist=S1/S2 plus the 51f149d blob revert channel per dispatch 一.7); nothing was written.
criteria_sha_before=317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc criteria_sha_after=317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc unchanged=True
probe 4 (positive control): safe_write(tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs, <S1 prep bytes>, 'S1') must be ALLOWED
ALLOWED as designed: criteria bytes now = S1 prep; sha=3b55a3893025bc66c5b773e627959b120d9b71add11b80a76df4bdf154cb00a6
restored to blob: sha=317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc baseline_match=True
status_short_lines_after_probe=0
```

判定：守卫**两面都被验过**＝非白名单内容（任意字节／第三枚文本／白名单 ID 配错文本）当场 REFUSED 且判据 SHA 前后同值（一个字节没落）；白名单内容（S1 文本配 S1 ID）确实放行——这条正面控件是为了不让自己写成一枚"恒拒的守卫"（恒拒＝常绿守卫，与它要防的常绿门同族）。probe 4 写进去的字节随后按 blob 还原，载体 `status --short` 复看仍 **0 行**，本刀此后三轮全部写入只走同一 `safe_write` 通道。

## 一 文件面自证（本刀实际写入清单）

- `.local-notes/wave38-1029/`（gitignored，本刀自建）：`w38-count.py`（取数仪器）、`w38-apply.py`（写入仪器：字节复制 apply／blob revert／verify／判据白名单守卫）、`w38-recheck.py`（§三.0R 复算与静态判读）、`out/`（预检、各轮 TRX、apply／run／count／revert 存档、仪器自证、守卫实证、终验）。`mutation-prep/` 三枚与 `gk38-prep.js` ＝守门人产物，本刀**只读未改**（未新建、未重排、未修 replacement）。
- 主树唯一新文件＝本报告 `docs/eval/reports/2026-10-29-hds009-wave38-assert-face-and-scan-bite.md`（不 `git add`、不提交）。
- 载体内：`src/Pry.Core/Narrative/StateAndLearning/DeliveryAttribution.cs`（M5c 临时覆盖）与 `tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs`（S1／S2 临时覆盖），逐轮按 blob 字节还原；终态相对 `51f149d` 净写入 0 字节。
- 未新建任何产品文件、未新建任何测试文件、未改动除 §三.0 三枚文本以外的任何判据内容、未碰 `scripts/**`／`Pry.slnx`／`Directory.Build.props`／其他 `docs/**`／主树任何文件；未重跑 A 类／C 类／M1–M7／M2b／M7b 任何一轮；未碰工单 §四 四本押账。

## 二 三计数自证（不许新增判据格）

判据文件（`51f149d` blob 与载体工作副本同值，§〇.4 已核）：`[Fact]` = **23**、`[Theory]` = **1**、`[InlineData` = **3** ⇒ 展开节点 23＋3＝26 枚格。与波 33／34／35／36／37 逐位相同 ⇒ 本刀净新增格数＝0。两枚预合成判据文本（S1／S2）当场复数同为 **23／1／3**（`out/recompute-3-0R.txt` §1）⇒ 这两轮即便字节被覆盖，格数与期望值集合也没动：S1／S2 只把两枚**仪器函数**弄坏，不改任何断言内容、不改 `[InlineData]` 期望值、不 `Assert.Skip`、不注释既有断言、不新增数字门槛。终态侧将在 §六 再当场复数一次。

## 三 定向轮（跑序固定 M5c→S1→S2→全部还原并核六枚 SHA→F；三条配方出处＝工单 §三.2，即波 37 守门人 §九.4.1 与波 36 §九.5／波 37 §九.5.2，不在波 33 配方内）

### 三.0 三轮预期（逐字承自工单 §三.0——**预期枚数／格名／红形状是假设不是结论**：实测与之不符 ⇒ 整轮作废、红不署、不追第二轮、不许把本节改成实测值。行号＝`51f149d` 侧现值；"断言语句起始行"与"预期 TRX 帧行"分两列，帧行只作辅证，主证＝testName＋消息）

定向跑法（工单 §一.6 逐字，只换结果目录名，在载体内跑，不带 `--no-build`）：

```
dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave38-1029/out/trx-<轮>" --logger trx
```

| 轮 | 类别 | 文件:行（干净侧原文） | 突变内容（守门人预合成） | 预期恰响格（前缀 `Pry.Core.Tests.StateAndLearningPrototypeTests.`） | 断言语句起始行 | 预期 TRX 帧行 | 预期消息形状 | 枚数 |
|---|---|---|---|---|---|---|---|---|
| M5c | 翻布尔（断言面补刀） | `DeliveryAttribution.cs:89`＝`            false);` | 整行替换为 `            true);`（收尾 `);` 保留） | `A_promise_send_proposes_a_commitment_but_proves_neither_consent_nor_completion` | `:245` | `:245` | `Assert.False() Failure` Expected False／Actual True，且 `:244`（`UserConsentProven`）保持通过 | 恰 1 |
| S1 | 弄坏扫描函数（反面探针） | 判据 `:418-419`＝`LegacyChainNamesFoundIn` 的表达式体 | 两行并成一行 `… => Enumerable.Empty<string>(); // [MUTATION S1 temporary, wave38] …`（净 −1 行） | `State_and_learning_prototype_sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites` | `:320`（语句体 `:320-323`） | `:320`；实测落 `:321`–`:323` 之间任一行＝同族映射差，如实登记不作废 | `Assert.Equal() Failure: Strings differ`，Expected＝七枚名字串，Actual＝**空串** | 恰 1 |
| S2 | 弄坏扫描函数（反面探针） | 判据 `:423-424`＝`SourceSideDefaultsFoundIn` 体内两条 `if (…) yield return …;` | 两行并成一行 `        yield break; // [MUTATION S2 temporary, wave38] …`（净 −1 行，迭代器体大括号保留） | `State_and_learning_prototype_declares_no_source_side_numeric_default` | `:344`（语句体 `:344-345`） | `:344`；同上族映射差处置同 S1 | `Assert.Equal() Failure: Strings differ` Expected `"const,static readonly"`／Actual＝**空串** | 恰 1 |

每轮都预期**恰 1 枚红**；2 枚以上⇒整轮作废、红不署、把多出来的红名逐枚登记，不追第二轮；**恰 0 枚红同样作废并登记**（尤其 S1／S2：弄坏谓词后那枚格仍不红＝探针句也没在守，是真缺陷，必须原样报上来而不是改预期）。

### 三.0P 盘上预合成文本 SHA 复核（零 dotnet；`sha256sum .local-notes/wave38-1029/mutation-prep/*` 当场原文）

```
4c3f6a61e20e2d3bb5254ee0ec03db487861c33b42c69fc986524da7e95c91bb  M5c_DeliveryAttribution.cs   （工单 §三.0 声明值逐字符相同；4621 字节）
3b55a3893025bc66c5b773e627959b120d9b71add11b80a76df4bdf154cb00a6  S1_StateAndLearningPrototypeTests.cs （逐字符相同；19861 字节）
5388104f3f0158bbb74c20f687cce16a5297924ded0544aa8d9c5405084ea4a3  S2_StateAndLearningPrototypeTests.cs （逐字符相同；19758 字节）
```

三枚全配。**这条对账不替代并排自证**（工单 §七.8／波 37 守门人 §九.4.1）：SHA 只证明复制到盘上的字节＝守门人声明的字节，不证明声明的东西是对的，所以 §三.0R 另做逐行 diff 与静态判读。

### 三.0R 本刀独立复算（零 dotnet；从干净树逐枚读消费点＋扫描谓词交叉面＋静态语法判读；整段输出＝`out/recompute-3-0R.txt`，只登记盘上事实，不改 §三.0 任何一字）

1. **三枚突变文本逐枚与 `51f149d` 的 blob 做 `diff`**（改面与工单 §三.0"突变内容"列逐枚相符）：
   - M5c＝`DeliveryAttribution.cs:89` 整行 `            false);` → `            true);`，**收尾 `);` 在场**（波 37 的 M5b 丢的就是它）；`:88`（`UserConsentProven`）保持 `false,` 不动 ⇒ 只打红 `:245`。
   - S1＝判据 `:418-419` 两行并成一行 `    private static IEnumerable<string> LegacyChainNamesFoundIn(string source) => Enumerable.Empty<string>(); // [MUTATION S1 temporary, wave38] …`，净 −1 行（446→445 行按 `split('\n')` 计，等价于 445→444 个内容行）。
   - S2＝判据 `:423-424` 两行并成一行 `        yield break; // [MUTATION S2 temporary, wave38] …`，净 −1 行，迭代器体 `{`（`:422`）与 `}`（`:424` 侧）保留。
   三枚的"改前行内容"我从 blob 现取并排比对过（`out/recompute-3-0R.txt` §3 打印 `:244/:245/:310/:320-323/:336/:344/:345/:418/:419/:423/:424` 与 `prod:83..90` 的干净侧原文），与 `gk38-prep.js` 的 `expectOld` 三处等值断言指向同一批行。
2. **消费点复算（决定"恰 1 枚"）**：判据里 `AttributePromise` 只 1 个读点（`:238`），其结果的 `:244`／`:245` 各断一枚布尔 ⇒ M5c 恰 1 枚、`:244` 因 `:88` 未动而保持通过。`LegacyChainNamesFoundIn` 只 2 个读点＝`:310`（`Assert.Empty`）与 `:323`（探针 `string.Join` 进 `:320` 的 `Assert.Equal`）⇒ 弄坏成恒空后 `:310` 恒过（这就是"常绿门"的样子）、`:320` 那句必红 ⇒ 恰 1 枚且红点必须落在 `:320`–`:323`。`SourceSideDefaultsFoundIn` 只 2 个读点＝`:336` 与 `:345`（`:344` 的 `Assert.Equal`）⇒ 同理恰 1 枚。两枚被弄坏的函数不参与其它 24 枚格：`LegacyChainNames` 数组只出现在 `:28`（声明）、`:419`（被弄坏的那行本身）；`PrototypeFiles` 只出现在 `:34`、`:308`、`:334`；`PrototypeSource` 只出现在 `:310`、`:336`、`:427`（定义）。
3. **扫描谓词交叉面**：三枚文本的改动行逐枚扫两枚扫描格的 token（七枚旧链类型名＋`const `＋`static readonly`）⇒ **命中 0**（`out/recompute-3-0R.txt` §4）。M5c 改的是被扫清单内的产品文件（`PrototypeFiles` 五枚含 `DeliveryAttribution.cs`），新行 `            true);` 不含任何 token ⇒ 两枚扫描格在 M5c 轮不连带红。S1／S2 改的是判据文件本身，而判据文件**不在** `PrototypeFiles` 清单里（被扫的只有五枚产品文件）⇒ 不被自己扫。
4. **`Enumerable.Empty<string>()` 的可用性**：判据干净侧 `:419` 已在用 `.Where(...)`（同属 `System.Linq`），`Directory.Build.props` 里 `ImplicitUsings=enable` ⇒ 不新增 using、不引入编译告警；测试工程无 `TreatWarningsAsErrors`。
5. **静态语法判读（工单 §七.3 第三选项的判据面）**：三枚逐枚判读结果＝**全部语法完整，没有一轮需要不起**——M5c：全文件括号 18/18、大括号 3/3 收支平衡，`:83..:89` 那段 `return new AttributionReadout(` 语句局部括号 1/1 闭合（这正是波 37 M5b 坏掉的形状）；S1：改后行去注释＝`private static IEnumerable<string> LegacyChainNamesFoundIn(string source) => Enumerable.Empty<string>();`＝单条表达式体方法语句、以 `;` 收尾，全文件括号 262/262、大括号 37/37 平衡（比干净侧各少 1 枚括号＝被并掉的那行带走一对，收支仍平）；S2：`{ … yield break; … }` 体内只剩 `yield break`（仍是迭代器方法，`yield break` 计入 yield），大括号 37/37、括号 259/259 平衡。**⇒ 第三选项（静态判读命中语法级缺陷就不起那一轮）本刀未动用**，定向额度 3 枚全部起跑。
6. **行号漂移面**：S1 改动行＝`:418`、S2＝`:423`，都在 `:415` 之后 ⇒ 本刀预期的四个行号（`244`／`245`／`320`／`344`）不因净 −1 行而漂移；M5c 改产品文件不移动判据行号。若实测红点落点比预期小 1，先怀疑自己抄错行而不是怀疑漂移。

### 三.P 焦点预检台账（逐枚当场登记，含 HOLD；每一次 `dotnet` 之前一枚，编号从 P91 起）

| 编号 | 时刻（归档文件 mtime） | VERDICT | 归档 | 处置 |
|---|---|---|---|---|
| P91 | 2026-10-29 00:44（当场取） | **BUILD_OK** | `out/preflight-P91.txt`（整段原文见 M5c 节⑪） | 放行 M5c（本刀 `dotnet` 计数 1）；前台＝QQ pid=27508 class=Chrome_WidgetWin_1 `covers=False borderless=False`、`free_memory_gb=9.4` |
| P92 | 2026-10-29 00:46（当场取） | **BUILD_OK** | `out/preflight-P92.txt`（整段原文＝S1 节末的 P92 原文块） | 放行 S1（`dotnet` 计数 2）；前台＝Godot pid=53448 class=Engine `covers=False borderless=False`、`free_memory_gb=12.6` |
| P93 | 2026-10-29 00:48（当场取） | **BUILD_OK** | `out/preflight-P93.txt`（整段原文＝S2 节末的 P93 原文块） | 放行 S2（`dotnet` 计数 3）；前台＝ChatGPT pid=16348 class=Chrome_WidgetWin_1 `covers=False borderless=False`、`free_memory_gb=9.9` |
| P94 | 2026-10-29 00:49（当场取） | **BUILD_OK** | `out/preflight-P94.txt`（整段原文＝§四 起头的 P94 原文块） | 放行收尾全量 F（`dotnet` 计数 4，末次调用）；前台＝ChatGPT pid=16348 class=Chrome_WidgetWin_1 `covers=False borderless=False`、`gpu_util=19% used=2281MB`、`free_memory_gb=9.9` |

四枚预检 VERDICT 分布＝`BUILD_OK` 4／`HOLD` **0** ⇒ **HOLD 重跑额度动用 0/16**（一次都没重跑，相邻 5 分钟间隔约束未被触发）；预检与 `dotnet` 一一配对（4 枚预检＝4 次 `dotnet`，无"跳过预检直接起构建"的形状）。台账"时刻"＝预检归档写入时刻（`date -r` 与文件 mtime 同源）。

## 三.M 定向轮逐轮十一项（一轮一节，跑完即追加；写入＝`w38-apply.py` 整文件字节复制自守门人 `mutation-prep/`，还原＝`git show 51f149d:<path>` blob 字节写回）

### M5c（翻布尔·`AttributionReadout` 第 6 枚实参 `TaskCompletedProven`；＝`:245` 那句断言的第一次单独响，断言级子账面）——预检 P91＝BUILD_OK

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `src/Pry.Core/Narrative/StateAndLearning/DeliveryAttribution.cs:89` |
| ② 改前原文 | `            false);`（`AttributePromise` 的 `return new AttributionReadout(` 第 6 枚位置实参＝`TaskCompletedProven`，同行带调用收尾 `);`；同一语句的第 5 枚实参 `:88`＝`            false,`＝`UserConsentProven`，本轮不动） |
| ③ 改后原文 | `            true);`（＝`mutation-prep/M5c_DeliveryAttribution.cs` 整文件字节复制，4621 字节，行数不变，收尾 `);` 在场） |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/StateAndLearning/DeliveryAttribution.cs | 2 +-`／` 1 file changed, 1 insertion(+), 1 deletion(-)`（存档 `out/m5c-apply.txt`；当轮载体 `status --short`＝**1 行** ` M` 该文件） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave38-1029/out/trx-M5c" --logger trx`（`dotnet` 计数 1；退出码 1＝有用例红，非构建失败；本轮日志 `grep -c no-build`＝**0**） |
| ⑥ TRX 文件名 | `out/trx-M5c/24782_PHYTHLINK_2026-10-29_00_45_20.trx`（该目录 `.trx` 枚数＝1） |
| ⑦ 红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.StateAndLearningPrototypeTests.A_promise_send_proposes_a_commitment_but_proves_neither_consent_nor_completion`（逐字符＝§三.0 预期）；消息＝`Assert.False() Failure`（Expected: False／Actual: True，＝§三.0 预期红形状）；红点＝TRX 帧 `StateAndLearningPrototypeTests.cs:line 245`（断言语句起始行 `:245` 与预期帧行 `:245` **两列同值命中，无映射差**；堆栈仅测试方法一帧＋两帧反射入口，无产品异常帧＝形状上正是承重那句断言自己响）；`:244`（`Assert.False(readout.UserConsentProven)`）**保持通过**＝本轮 `:88` 未动，实测该格只红这一句、消息面只出现 `TaskCompletedProven` 那一句的形状。仪器读数（`out/m5c-count.txt`）：`nodes=26 total=26 executed=26 passed=25 countersFailed=1 faceA=1 faceB=1 selfCheck=OK reconcile=OK notExecutedOpenTags=0 allRedNamesInCriteria=True` |
| ⑧ 还原命令 | `python w38-apply.py revert M5c`（内部＝`git show 51f149d:src/.../DeliveryAttribution.cs` 字节原样写回，零 `checkout`/零 `restore`；存档 `out/m5c-revert.txt`） |
| ⑨ 还原后 `git status --short` 行数 | `0` |
| ⑩ SHA-256 | 突变态实测 `4c3f6a61e20e2d3bb5254ee0ec03db487861c33b42c69fc986524da7e95c91bb` ＝工单 §三.0 M5c 值（match=True）；还原后 `5858a75da3ed70e35f93782e1d760c87eea3feeecdafcdd645b27f69ea66f0ab` ＝ §〇.4 基准（match=True）；**本轮判据字节未写入**：判据文件 apply 侧与 revert 侧两枚读数均＝ `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc`（＝`51f149d` 的 blob，逐字节等于基准 ✓） |
| ⑪ 当轮预检 | P91＝`VERDICT=BUILD_OK`（归档 `out/preflight-P91.txt`；整段原文见下） |

P91 整段输出原文：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: QQ pid=27508 class=Chrome_WidgetWin_1 style=0x14C70000 rect=-1825,234,993x802 screen=1920x1080 covers=False borderless=False title=QQ
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=27% used=2215MB
free_memory_gb=9.4
VERDICT=BUILD_OK
```

形状裁决＝实测恰 1 枚、格名逐字符等于 §三.0 事前论证、消息＝预期红形状（`Assert.False() Failure` Expected False／Actual True）、红点同时命中"断言语句起始行 `:245`"与"预期 TRX 帧行 `:245`"两列、且 `:244` 保持通过 ⇒ 与事前论证一致，本轮有效；判据字节全程等于基准 ⇒ 这一轮的凭据形态与波 37 那九枚一致（是否署名、`:245` 断言级子账是否划账由守门人裁，本 lane 不写状态词）。

（S1／S2 两轮见下，逐轮追加。）

### S1（弄坏扫描函数·`LegacyChainNamesFoundIn` 改恒空＝反面探针；**本轮动判据字节 ⇒ 不签换手**）——预检 P92＝BUILD_OK

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs:418-419`（两枚扫描仪器函数之一 `LegacyChainNamesFoundIn` 的表达式体） |
| ② 改前原文 | `    private static IEnumerable<string> LegacyChainNamesFoundIn(string source) =>` ／ `        LegacyChainNames.Where(name => source.Contains(name, StringComparison.Ordinal));`（两行，从 `51f149d` 的 blob 现取并排核对，见 `out/recompute-3-0R.txt` §3） |
| ③ 改后原文 | `    private static IEnumerable<string> LegacyChainNamesFoundIn(string source) => Enumerable.Empty<string>(); // [MUTATION S1 temporary, wave38] neuter the legacy-name scan; only the probe can catch it`（＝`mutation-prep/S1_StateAndLearningPrototypeTests.cs` 整文件字节复制，19861 字节，净 −1 行） |
| ④ `git diff --stat` | ` tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs | 3 +--`／` 1 file changed, 1 insertion(+), 2 deletions(-)`（存档 `out/s1-apply.txt`；当轮载体 `status --short`＝**1 行** ` M` 判据文件） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave38-1029/out/trx-S1" --logger trx`（`dotnet` 计数 2；退出码 1；日志 `grep -c no-build`＝**0**） |
| ⑥ TRX 文件名 | `out/trx-S1/24782_PHYTHLINK_2026-10-29_00_46_19.trx`（该目录 `.trx` 枚数＝1） |
| ⑦ 红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.StateAndLearningPrototypeTests.State_and_learning_prototype_sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites`（逐字符＝§三.0 预期）；消息＝`Assert.Equal() Failure: Strings differ` ↓(pos 0) Expected `"BehaviorPlanner,ReplyGenerationRenderer,CharacterS"···`（七枚名字串）／Actual **`""`（空串）** ＝§三.0 预期红形状（谓词被弄坏后扫描返回空）；红点＝TRX 帧 `StateAndLearningPrototypeTests.cs:line 320`（断言语句起始行 `:320` 与预期帧行 `:320` **两列同值命中，无映射差、无同族漂移**；堆栈仅测试方法一帧，无产品异常帧）。**机制面（工单 §八.3 要的形状）＝同一枚格内 `:310` 的 `Assert.Empty(LegacyChainNamesFoundIn(PrototypeSource(file)))` 恒过**：红点落在 `:320` 说明 `:308-311` 那个 `foreach` 里的五枚被扫文件全部"扫不出名字"而通过（弄坏后谓词恒返回空 ⇒ 门对真文本也不再咬），只有把已知含七枚名字的样本喂进去的那句探针断言（`:320-323`）当场红；其余 24 枚格全绿。仪器读数（`out/s1-count.txt`）：`nodes=26 total=26 executed=26 passed=25 countersFailed=1 faceA=1 faceB=1 selfCheck=OK reconcile=OK notExecutedOpenTags=0 allRedNamesInCriteria=True` |
| ⑧ 还原命令 | `python w38-apply.py revert S1`（内部＝`git show 51f149d:tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs` blob 字节原样写回，零 `checkout`/零 `restore`；存档 `out/s1-revert.txt`） |
| ⑨ 还原后 `git status --short` 行数 | `0` |
| ⑩ SHA-256 | 突变态实测 `3b55a3893025bc66c5b773e627959b120d9b71add11b80a76df4bdf154cb00a6` ＝工单 §三.0 S1 值（match=True）；还原后判据＝ `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc` ＝ §〇.4 基准（match=True，逐字节回到基准）。**本轮判据文件 SHA（apply 侧）＝ `3b55a3893025…` ≠基准、（revert 侧）＝ `317d2171264f…` ＝基准** ⇒ 按工单 §一.8：这一轮**只结"门不是常绿"的机制面，绝不署名为第二换手**（判据字节被本刀这只手写过） |
| ⑪ 当轮预检 | P92＝`VERDICT=BUILD_OK`（归档 `out/preflight-P92.txt`；整段原文：前台 Godot pid=53448 class=Engine `covers=False borderless=False`，`gpu_util=18% used=2270MB`，`free_memory_gb=12.6`） |

形状裁决＝实测恰 1 枚、格名逐字符等于 §三.0 事前论证、消息＝预期红形状（Strings differ／Actual 空串）、红点命中 `:320` 两列、且"弄坏谓词后 `Assert.Empty` 那一半恒过、探针那一半红"这一形状在盘上成立 ⇒ 与事前论证一致，本轮有效；本轮判据字节被覆盖 ⇒ 不署换手、不重跑波 36 的 C 类。

（S1 与 S2 之间无第二轮追打；S2 见下。）

P92 整段输出原文（`out/preflight-P92.txt` 逐字符照贴；`title=???? ? ???? (DEBUG)` 那串问号＝预检脚本经 Windows 控制台码页落盘的原样字节，非本刀改写）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: Godot pid=53448 class=Engine style=0x16CF0000 rect=190,422,956x599 screen=2560x1440 covers=False borderless=False title=???? ? ???? (DEBUG)
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=18% used=2270MB
free_memory_gb=12.6
VERDICT=BUILD_OK
```

### S2（弄坏扫描函数·`SourceSideDefaultsFoundIn` 迭代器体改 `yield break`＝反面探针；**本轮动判据字节 ⇒ 不签换手**）——预检 P93＝BUILD_OK

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs:423-424`（两枚扫描仪器函数之二 `SourceSideDefaultsFoundIn` 的迭代器体内两条 `if … yield return …;`） |
| ② 改前原文 | `        if (source.Contains("const ", StringComparison.Ordinal)) yield return "const";` ／ `        if (source.Contains("static readonly", StringComparison.Ordinal)) yield return "static readonly";`（两行，从 `51f149d` 的 blob 现取并排核对，见 `out/recompute-3-0R.txt` §3） |
| ③ 改后原文 | `        yield break; // [MUTATION S2 temporary, wave38] neuter the source-side default scan; only the probe can catch it`（＝`mutation-prep/S2_StateAndLearningPrototypeTests.cs` 整文件字节复制，19758 字节，净 −1 行；迭代器体 `{`／`}` 保留，`yield break` 计入 yield ⇒ 仍是迭代器方法） |
| ④ `git diff --stat` | ` tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs | 3 +--`／` 1 file changed, 1 insertion(+), 2 deletions(-)`（存档 `out/s2-apply.txt`；当轮载体 `status --short`＝**1 行** ` M` 判据文件；`out/s2-diff.txt` 存整段 `git diff -U2` 原文） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave38-1029/out/trx-S2" --logger trx`（`dotnet` 计数 3；退出码 1＝有用例红，非构建失败；日志 `grep -c no-build`＝**0**） |
| ⑥ TRX 文件名 | `out/trx-S2/24782_PHYTHLINK_2026-10-29_00_49_13.trx`（该目录 `.trx` 枚数＝1） |
| ⑦ 红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.StateAndLearningPrototypeTests.State_and_learning_prototype_declares_no_source_side_numeric_default`（逐字符＝§三.0 预期）；消息＝`Assert.Equal() Failure: Strings differ` ↓(pos 0) Expected `"const,static readonly"`／Actual **`""`（空串）**＝§三.0 预期红形状（谓词被弄坏后扫描返回空）；红点＝TRX 帧 `StateAndLearningPrototypeTests.cs:line 344`（断言语句起始行 `:344` 与预期帧行 `:344` **两列同值命中，无映射差、无同族漂移**，未落到 `:345`；堆栈仅测试方法一帧＋两帧反射入口，无产品异常帧）。**机制面（工单 §八.3 要的形状）＝同一枚格内 `:336` 的 `Assert.Empty(SourceSideDefaultsFoundIn(PrototypeSource(file)))` 恒过**：红点落在 `:344` 说明 `:334-337` 那个 `foreach` 里的五枚被扫文件全部"扫不出侧默认"而通过（弄坏后迭代器立即 `yield break` ⇒ 门对真文本也不再咬），只有把已知含 `const `＋`static readonly` 的样本（`:342-343` 写出的临时文件）喂进去的那句探针断言（`:344-345`）当场红；其余 24 枚格全绿。仪器读数（`out/s2-count.txt`）：`nodes=26 total=26 executed=26 passed=25 countersFailed=1 faceA=1 faceB=1 selfCheck=OK reconcile=OK notExecutedOpenTags=0 allRedNamesInCriteria=True` |
| ⑧ 还原命令 | `python w38-apply.py revert S2`（内部＝`git show 51f149d:tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs` blob 字节原样写回，零 `checkout`/零 `restore`；存档 `out/s2-revert.txt`） |
| ⑨ 还原后 `git status --short` 行数 | `0` |
| ⑩ SHA-256 | 突变态实测 `5388104f3f0158bbb74c20f687cce16a5297924ded0544aa8d9c5405084ea4a3` ＝工单 §三.0 S2 值（match=True）；**本轮判据文件 SHA：apply 侧写前读数 `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc` ＝ §〇.4 基准（＝S1 已还原到位的现场实证），写后 `5388104f3f01…` ≠基准；revert 侧 `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc` ＝基准**（match=True，逐字节回到基准）。按工单 §一.8：这一轮**只结"门不是常绿"的机制面，绝不署名为第二换手**（判据字节被本刀这只手写过） |
| ⑪ 当轮预检 | P93＝`VERDICT=BUILD_OK`（归档 `out/preflight-P93.txt`；整段原文：前台 ChatGPT pid=16348 class=Chrome_WidgetWin_1 `covers=False borderless=False`，`gpu_util=16% used=2269MB`，`free_memory_gb=9.9`） |

形状裁决＝实测恰 1 枚、格名逐字符等于 §三.0 事前论证、消息＝预期红形状（Strings differ／Actual 空串）、红点命中 `:344` 两列（未漂移也未落 `:345`）、且"弄坏谓词后 `Assert.Empty` 那一半恒过、探针那一半红"这一形状在盘上成立 ⇒ 与事前论证一致，本轮有效；本轮判据字节被覆盖 ⇒ 不署换手。三轮之间无作废轮、无追打轮。

P93 整段输出原文（`out/preflight-P93.txt`）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: ChatGPT pid=16348 class=Chrome_WidgetWin_1 style=0x14C70000 rect=-1724,319,1445x811 screen=1920x1080 covers=False borderless=False title=ChatGPT
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=16% used=2269MB
free_memory_gb=9.9
VERDICT=BUILD_OK
```


## 四 收尾全量 F（跑序末位：全部还原并核六枚 SHA 之后才起）

**起 F 前的闸**：三轮定向全部按 blob 还原完毕，`python w38-apply.py verify` 当场复核六枚文件 SHA（存档 `out/final-verify.txt`）＝五枚产品文件与判据文件的 `git show 51f149d:<path>` blob 值与工作副本值**逐枚 match=True**，`verify_mismatches=0`（同 §〇.4 三侧同值的六个基准值，一字节不差）。载体 `git status --short`＝**0 行**。⇒ 工单"未跑满三轮不许起 F"与"还原并核六枚 SHA 之后才起"两条先后关系成立。

预检 P94＝`VERDICT=BUILD_OK`（`dotnet` 计数 4，本刀末次 `dotnet` 调用；整段原文如下，归档 `out/preflight-P94.txt`）：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: ChatGPT pid=16348 class=Chrome_WidgetWin_1 style=0x14C70000 rect=-1724,319,1445x811 screen=1920x1080 covers=False borderless=False title=ChatGPT
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=19% used=2281MB
free_memory_gb=9.9
VERDICT=BUILD_OK
```

命令整行（在载体 `C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave33-1028/carrier` 内，无 `--no-build`）：

```
dotnet test Pry.slnx -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave38-1029/out/trx-F" --logger trx
```

退出码＝**0**（全绿），日志 `grep -c no-build`＝**0**，`warning`／`error` 字样计数＝**0/0**。日志头部实证这轮确实从还原后的干净字节重建：`Pry.Core -> …carrier\src\Pry.Core\bin\Release\net10.0\Pry.Core.dll` 与四枚 `*.Tests -> …dll` 逐行在场（⇒ §〇.7 的 B1 跳过判定没有造成"跑的是旧二进制"的形状）。

先数满再求和：`ls out/trx-F/*.trx | wc -l`＝**4**（`24782_PHYTHLINK_2026-10-29_00_50_19.trx`／`…_00_50_19[1].trx`／`…_00_50_20.trx`／`…_00_50_21.trx`）。控制台逐工程原文：Client `失败 0／通过 108／已跳过 0／总计 108`、App `0／233／0／233`、Api `0／280／0／280`、Core `0／783／2／785`。

仪器求和（`out/f-count.txt`，逐份先读 `<UnitTestResult … outcome>` 开标签与 `<Counters>`，再求和；两路互校 `selfCheck=OK`、`reconcile=OK`）：

```
AGG files=4 total=1406 executed=1404 passed=1404 countersFailedSum=0 faceA=0 faceB=0 selfCheck=OK reconcile=OK notExecutedOpenTags=2 allRedNamesInCriteria=True
```

| TRX | nodes | total | executed | passed | countersFailed | faceA／faceB | NotExecuted 开标签 |
|---|---|---|---|---|---|---|---|
| `…_00_50_19.trx`（Client） | 108 | 108 | 108 | 108 | 0 | 0／0 | 0 |
| `…_00_50_19[1].trx`（Core） | 785 | 785 | **783** | 783 | 0 | 0／0 | **2** |
| `…_00_50_20.trx`（Api） | 280 | 280 | 280 | 280 | 0 | 0／0 | 0 |
| `…_00_50_21.trx`（App） | 233 | 233 | 233 | 233 | 0 | 0／0 | 0 |
| **求和** | **1406** | **1406** | **1404** | **1404** | **0** | **0／0** | **2** |

两条 `outcome="NotExecuted"` 开标签按工单 §一.4 口径逐字点名（不是 `failed`，是环境门控跳过）：
1. `Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`
2. `Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`

**出场读数＝1406／executed 1404／failed 0／跳过 2**，与 §〇.5 起点读数同值（分项 Client 108／App 233／Api 280／Core 785、Core executed 783 亦同值）。**本刀不顶账**：这是第四次同值复采还是本刀自己的读数，由守门人按工单 §八.4 裁；本 lane 只登记"实测与起点同值、加法未做"。为顶账另起构建＝0 次（构建预算用 0/1，全程未动用）。

## 五 未验证／未结清单（逐一点名，本 lane 不代裁）

工单 §六 与 §四 那批与生俱来的未验证面，本刀跑完后**一条都没有变少**，逐条照旧：

1. 真实模型对照＝**零读数**（需作者一句"可以跑"）。
2. 生产接线＝这 26 枚格至今**零生产调用方**；要接的 005 落库面与 004 生命周期一律没接；接线时机＝U-9 残句「切换那一步她要不要做什么」仍未拍。
3. 旧数据真实继承＝**零读取**。
4. UI 可见行为＝**零读数**。
5. 生产库与 `%LOCALAPPDATA%\PryCompanion`＝**零接触**（本刀全程只在载体树内读写，且只读守门人 `mutation-prep/`）。
6. 待标定参数权威值（关系／心情各档阈值、经验有效期、点击归因窗口）＝**全部未定＝特性不是缺陷**。
7. 并发与断电＝**零读数**。
8. 旧链删除片与"构建时不把旧链打进包"那道门＝**未开始**。
9. `:367` 那半句＝**未追**（按工单 §四.2 的口径：只在"这个夹具的输入形状下没有可达配方"这一面上签到，"结构上永远够不着"仍未证；本刀未碰 `LegacyInheritance.cs:53`，也未碰 `:365`／`:367`）。
10. 阳性探针的**单独正数面读数**＝**仍欠**（工单 §四.3）：S1／S2 让探针句各自响了一次，那是"探针在守"的反面证明，不是"探针句单独被跑出读数"的正数面；那需要新增判据格，本刀**未新增**（三计数出场仍 23／1／3，见 §六）。
11. `PrototypeFiles` 写死五枚清单的覆盖盲区、两套并行命名债（`Hds006*` 产品类型名＋契约 §7.2 的 `CognitionKind`／`CognitionOrigin`）、波 28 那枚数值锁格、HDS-007／HDS-009 的生产接线、HDS-010 删除片、旧欠 75 格（波 13 余 47＋波 20 余 28）的换手账＝**照旧挂着，本刀一枚未碰**；拆法原文照旧 46／45／1＝W2，旧欠 75 格**未减**。
12. Release 序列点把红点记到多行调用起始行的机理＝**已知形状未证**（波 35／36／37 同族；本刀三轮实测红点均落在我事前登记的"断言语句起始行"同一行，未出现需要靠该机理救回来的漂移，但机理本身仍未证）。

本刀新增的边界性未决面（盘上事实，不裁）：

13. S1／S2 两轮的判据字节被本刀这只手写过 ⇒ 按工单 §一.8 这两轮**不署任何换手**，也不重跑波 36 的 C 类；M5c 那轮的判据字节全程等于 `51f149d` 的 blob（apply／revert 两侧读数同为基准值）。
14. 本刀未取任何 `.trx` 之外的性能／时间／覆盖率读数；波 36 起就欠的覆盖率面＝**零读数**。
15. 本刀**未签 26 枚结案、未签任何换手、未写状态词、未划账**；§三.0 的预期列未被实测改写（改一个字都不许，实际未改）。

（工单 §七.3 的第三选项＝静态判读命中语法级缺陷就不起那一轮：本刀**未动用**，三枚文本静态判读全部通过、三轮全部起跑，无作废轮、无退回的定向额度。）

## 六 盘上自审计（逐项对账，差账逐处点名不抹）

1. **`.trx` 枚数对账**：`find .local-notes/wave38-1029/out -name "*.trx" | wc -l` = **7** ＝ 登记轮数自校（定向 3 轮×1 枚 ＋ 收尾全量 1 轮×4 枚 ＝ **7**）✓ 无差额、无多枚、无 `voided` 目录（作废轮＝0，故 `trx-<轮>-voided-<预检号>/` 这类改名留档目录＝0 个；未清空／未删除／未在同名目录重跑过任何一轮）。逐目录点名：`trx-M5c`=1、`trx-S1`=1、`trx-S2`=1、`trx-F`=4。
2. **`dotnet` 调用计数**：**4/5**（M5c＝1、S1＝2、S2＝3、F＝4；无第五次）。分项额度：定向 **3/3**（无第四枚定向，未自加）、解决方案级全量 **1/1**、解决方案级构建 **0/1**。每枚 `dotnet` 之前当场一枚预检（P91–P94 一一配对），每轮日志 `grep -c no-build`＝**0**（三轮定向＋F 都是 0，无 `--no-build` 形状）。
3. **B1（解决方案级构建）跳过判定三段出场复核**：① 主树侧 0 行式子出场复跑成立 ✓（下条第 7 项，右端 `1d2c0332317be9abbe9742d92c2e3313324fbacf` 出场当场再取）；② 载体 `src/Pry.Core/bin/Release/net10.0/Pry.Core.dll` 在盘——在盘证据行＝`-rwxr-xr-x 1 24782 197609 1745920 Oct 29 00:46 …carrier/src/Pry.Core/bin/Release/net10.0/Pry.Core.dll`（mtime 落在本刀 S1 轮那次 `dotnet test` 的增量重建时刻，且 F 轮日志头部再次出现 `Pry.Core -> …Pry.Core.dll` ⇒ 被测二进制由当时树内字节重建，不存在"跑旧产物"的形状）✓；③ 六枚 SHA 自入场未变 ✓（第 5 项）。⇒ 跳过判定三段全部当场重取成立，构建预算用 **0/1** 合法；本刀未为顶账另起任何构建。
4. **预检 VERDICT 分布与 HOLD 动用**：`BUILD_OK` **4**／`HOLD` **0**；HOLD 后重跑额度动用 **0/16**（相邻 5 分钟间隔约束未被触发）；`free_memory_gb` 读数序列 9.4／12.6／9.9／9.9，无 SQLite `disk I/O`／`out of memory` 形态，无"因资源形状作废的读数"，未动任何进程。
5. **三轮判据文件 SHA-256 台账（apply 侧与 revert 侧各一次）＋终验**：

   | 轮 | 判据 apply 侧读数 | 是否等于基准 `317d2171264f…` | 判据 revert 侧读数 | 是否等于基准 | 被测文件突变态 SHA | 还原后 |
   |---|---|---|---|---|---|---|
   | M5c | `317d2171264f…`（本轮不写判据） | 是 ✓ | `317d2171264f…` | 是 ✓ | `4c3f6a61e20e…`（＝工单声明值） | `5858a75da3ed…` ✓ |
   | S1 | `3b55a3893025…`（本轮写入判据） | **否**（白名单内，§一.7） | `317d2171264f…` | 是 ✓ | 即判据本身 | 同上 ✓ |
   | S2 | 写前 `317d2171264f…`（＝S1 已还原实证）／写后 `5388104f3f01…` | 写后**否**（白名单内） | `317d2171264f…` | 是 ✓ | 即判据本身 | 同上 ✓ |
   | F | 判据与五枚产品文件全程＝基准（F 前 `verify` 六枚全 match=True） | 是 ✓ | — | — | 零写入 | — |

   终验（`out/final-verify.txt`）＝六枚 `blob=` 与 `work=` 逐枚同值、`verify_mismatches=0`：`af727a9c04355d85…`／`000f2fc8bfd5d73b…`／`5858a75da3ed70e3…`／`289fc89be087fa35…`／`6a1f50999e7caf1d…`／`317d2171264f5fc7…`。
6. **三计数出场复看**（还原后的载体判据工作副本当场重数，命令与 §二 同一枚）：`[Fact]` = **23**、`[Theory]` = **1**、`[InlineData` = **3** ⇒ 与入场值（§二）逐位相同 ⇒ **净新增格数＝0**；F 出口 `nodes` 求和 1406（其中该判据类 26 枚＝23＋3）与三计数自洽。另：`public void` 测试方法名面 23 枚，S1／S2 两枚预合成文本与之**逐字符同名**（本轮突变只弄坏两枚仪器函数体，未改任何格名／期望值／`[InlineData]`）⇒ 仪器在 S1／S2 轮用主树侧判据字节做的红名归属面（`allRedNamesInCriteria=True`）与载体侧突变字节的格名面同值，归属读数未受影响——这条是我自己读数口径的交叉面，登记备查。
7. **载体终态四行 ＋ 主树侧 0 行式子出场复跑**（右端一律当场取，不引用入场值）：
   - `git -C <carrier> rev-parse HEAD` = `51f149d0b722e28a6566c93d19f48a94ca4c85df`（未动，零提交）✓
   - `git -C <carrier> status --short | wc -l` = **0**（输出为空）✓ ⇒ 载体相对 `51f149d` 净写入 **0 字节**
   - `git -C <carrier> log --oneline fd93ebe..HEAD | wc -l` = **2** ＝入场值 ✓
   - 主树侧 0 行式子（右端出场现取 `git -C <main> rev-parse HEAD` = `1d2c0332317be9abbe9742d92c2e3313324fbacf`，与入场同值＝主树线尾本刀未动）：`git -C <main> diff --stat 51f149d 1d2c0332317be9abbe9742d92c2e3313324fbacf -- src tests scripts Pry.slnx Directory.Build.props | wc -l` = **0** ✓ ⇒ 出场时载体树与主线归位后的产品／判据字节仍逐字符相同。
8. **主树零 git 写与唯一新文件**：本刀在主树内执行的 git 命令全部为只读（`rev-parse`／`status`／`log`／`diff`／`show`／`worktree list`）；**零** `commit`／`add`／`checkout`／`reset`／`merge`／`cherry-pick`／`tag`／`push`／`apply`／`stash`／`clean`／`rm`。主树唯一新文件＝本报告 `docs/eval/reports/2026-10-29-hds009-wave38-assert-face-and-scan-bite.md`，**未 `git add`、未提交**（出场 `git status --short` 里它以 `??` 出现，即本刀唯一新增的一行）。
9. **他人资产出场复看**：出场主树 `git status --short` 与 §〇.2 入场逐行对照，**只差本报告那一行 `??`**；两枚 `M ` 暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）仍在暂存位、` M docs/plans/PROJECT_PLAN.md` 出场仍为 **18 行插入**（`git diff --stat HEAD -- docs/plans/PROJECT_PLAN.md` 原文：`1 file changed, 18 insertions(+)`）＝他人条目未被本刀改写；其余 5 枚 `??`（`.qoder/`、`docs/character-learning-safety.patch`、`docs/research/HDS_INTERLUDE.md`、`docs/research/HDS_QQ_RESEARCH_INDEX.md`、`docs/research/QQ_BRIDGE.md`）枚数与入场相同、未被读取内容／未被移动／未被删除。其余 70+ 条 worktree 未动，未新建任何 worktree。
10. **起跑前／出场三次独立盘面读数**（预检 VERDICT／载体 `status` 行数／`.trx` 枚数与目录名）：起 M5c 前＝P91 BUILD_OK、status 0 行（写入瞬时 1 行）、`.trx` 累计 0；起 S1 前＝P92 BUILD_OK、M5c 已还原后 status 0 行（写入瞬时 1 行）、累计 1（`trx-M5c`）；起 S2 前＝P93 BUILD_OK、status 0 行（写入瞬时 1 行）、累计 2（＋`trx-S1`）；起 F 前＝P94 BUILD_OK、三轮还原后 status **0 行**＋六枚 SHA 全配、累计 3（＋`trx-S2`）；出场＝累计 **7**（＋`trx-F` 四枚）、status **0 行**、HEAD 两值同入场。五处读数与登记轮数一一可复算，无未登记轮次、无未登记调用。
11. **仪器自证与守卫复看**：取数仪器动第一次 `dotnet` 之前对九个已知盘上读数自证＝**9 面 56 枚子读数全 AGREE**（`out/instrument-selfproof.txt`，`SELFPROOF FACES=9 … AGREED=56 DISAGREED=0`）；写入仪器的判据白名单守卫两面证伪过（三类非白名单内容当场 `REFUSED` 且判据 SHA 前后同值；白名单内容正面控件放行，避免写成"恒拒的常绿守卫"）——报错原文与 SHA 前后同值证据在 §〇.6.2 与 `out/guard-probe.txt`。三轮＋F 的全部取数与写入只用这两份文件，未经任何一次"改判据／改 TRX 来迁就仪器"的操作（仪器自身修过两处：hex 实体解码按 16 进制、`NotExecuted` 名面对照原始 `testName` 全名，修的都是仪器）。

差账＝**无**。本刀未越任何一条预算；未作废任何一轮；未追打第二轮；未改 §三.0 预期列。

## 七 交接（下一步归属；本节只交回事实，不裁账）

- 本刀跑到的位置＝工单指定满额形状：**M5c→S1→S2 三轮全部起跑**（无轮次因预检／额度／语法判读而不起，第三选项未动用），**全部还原并核六枚 SHA**（`out/final-verify.txt`，六枚 match=True、`verify_mismatches=0`）后起了**收尾全量 F**（`dotnet` 末次调用＝第 4 次，余量 1 次未用）。
- 三轮实测形状（盘上事实，是否署账由守门人按工单 §八 裁）：M5c＝恰 1 枚红，格名 `A_promise_send_proposes_a_commitment_but_proves_neither_consent_nor_completion`，帧 `:245`，`:244` 保持通过；S1＝恰 1 枚红，格名 `…sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites`，消息 `Strings differ`／Actual 空串，帧 `:320`；S2＝恰 1 枚红，格名 `…declares_no_source_side_numeric_default`，消息 `Strings differ`／Actual 空串，帧 `:344`。S1／S2 两轮的"`Assert.Empty` 那一半恒过、探针那一半红"形状均在同一枚格内当场成立（凭据＝红点落点 + 该格内另一句未响）。
- F 出口读数＝**1406／executed 1404／failed 0／跳过 2**，两条 `NotExecuted` 开标签逐字点名在 §四；与 §〇.5 入场起点读数同值。**本刀不顶账、不裁"第几次同值复采"、不写基线判定**。
- 守门人：按工单 §八 预登记的八条账面目裁三轮各自是否恰 1 枚红、M5c 的红点是否落 `:245` 且 `:244` 保持通过、S1／S2 的"门不是常绿"机制面能否成立（凭据形状＝弄坏谓词后 `Assert.Empty` 那一半恒过、探针那一半红）、这两轮判据字节被写过⇒不签换手这条边界要在 §九.2 明写、1406 是否第四次同值复采、`.trx` 对账与构建跳过三段、定向 3／`dotnet` 5 是否越预算、HOLD 额度是否被用成放宽、第三选项未动用要照实写、出闸后的账面目只增加断言级子账不新增格子。
- 本 lane 交付边界：报告＝主树唯一新文件（未提交）；载体净写入 0 字节终态（`status --short`＝0 行）；剩余额度按 §六 实测登记（`dotnet` 4/5、定向 3/3、全量 1/1、构建 0/1 跳过、HOLD 重跑 0/16）；§〇.6.2 那条判据路径守卫报错原文已归档并整段贴出（本刀未出现"写判据被拒"事件——所有判据写入都发生在白名单内的 S1／S2 轮）。
- 我觉得还该跑的东西（不自己动手，交回）：见 §五 第 9／10／14 条（`:367` 那半句、阳性探针的单独正数面读数、覆盖率面）与工单 §四 那四本押账——本刀一枚未碰。


## 九 守门人复判与划账（追加节，§〇–§七 一字未改；本节目的＝裁三轮各自能否署账、断言级子账现值、机制面签到哪一面、归位与押账）

### 九.0 我的仪器（从零写，先自证再用）与两处我自己的错

复判仪器＝`.local-notes/wave38-1029/gk38-verify.js`（node，本把新写；lane 的 `w38-count.py` 一行没读、没拷）。口径＝① 只数 `<UnitTestResult` 的**开标签**，逐 occurrence 引号感知扫到标签尾 `>`，自闭合 `…/>` 与成对 `…>…</UnitTestResult>` 同一切法，属性按**名字**取（不按顺序拼正则——见下面那条正面事实）；② 反向锚定面＝逐 occurrence 找 `outcome="Failed"` 回溯最近 `<` 判标签名，与①不等即报 `MISMATCH`；③ 红数与同份 `<Counters failed>` 双向对账；④ `nodes` 与 `total` 对账；⑤ 红名先剥全限定前缀与参数表再回判据验方法名存在；⑥ 抽帧不锚 `at `，形状＝`([\w.\-]+\.cs):(?:line |：行 )(\d+)`；⑦ 三条结论字段（红数／红名／消息／帧）各有一份已知答案，任何一面不符就整台仪器判坏、不许用它裁这一把。

**起跑前的自证从九面扩到十二面**（我把波 29 那两枚全 Core 跑与波 33 那三枚定向跑并列入表，另加波 33／36／37 三份 F）＝**12/12 AGREE**，整段输出存档 `out/gk38-verify.txt`。在此之前它两次把我拦下来，两次都是我的表错、不是扫描器错：

1. 我把"恰 3 枚红"记在 `w33/trx-M5` 名下 ⇒ 盘上实测 1 枚，且 `<Counters failed="1">` 与开标签同值、红名与帧 `:244` 齐全。真因＝**同名目录跨树**：`trx-M5` 在波 29（HDS-007 的 `BoundedAdvancementPrototypeTests`，恰 3）与波 33（HDS-009 的 `StateAndLearningPrototypeTests`，恰 1）两把里都存在；我引用"盘上已知读数"时只写了目录名，没写波次与判据文件。
2. 我给波 29 那两面按"突变轮 `notExec`＝0"去期望 ⇒ 盘上各 2 枚。真因＝那两份 TRX 是**完整程序集跑**（`total=759`），那两条环境门控跳过格只在完整程序集里在场；定向跑（`total=26`）里恒 0 是另一件事。

⇒ 两条口径升级进第三十九波：**已知读数表每一面必须带四元限定（波次＋判据文件名＋跑法〔定向／全量〕＋该面的结论字段值）**；**`notExec` 的期望值只能由跑法决定，不许由"这是突变轮"这个身份默认**。同族纪律第三次实例＝"引用前案凭证之前先读回那一格的跑法"——这次读错跑法的是我自己的自证表，不是别人的报告。

一条正面事实（证明坏的是我的表而不是仪器）：同一台仪器在波 29 那份文件上，`outcome="Failed"` 全文出现 2 次而反向面只算出 1（另一处在 `<ResultSummary>` 上），与 `<Counters failed="1">` 同值 ⇒ 波 30 立的"数红只数 `<UnitTestResult` 开标签、`ResultSummary` 不算用例"在这棵树上第三次现场生效。同一份文件还顺手给出一条：那两份 TRX 里 `testName` 排在 `outcome` **之前**，我另写的一条按顺序拼的正则恒 0 命中，而按属性名取值的仪器 759 个结点全抓得到 ⇒ **属性顺序也不许进判据**。

### 九.1 七件判定（逐件从盘上取，不引用 lane 的汇总句）

1. **三轮形状全对**：每轮 `nodes=26／total=26／executed=26／passed=25／countersFailed=1／faceA=faceB=1／notExec 开标签=0`，红名逐字符等于工单 §三.0 我出闸前落档的事前论证（**先档后跑**：我把报告 §三.0 表与工单 §三.0 表逐格对过，实测没有回头改预期列）；帧行 `:245`／`:320`／`:344` **同时命中**"断言语句起始行"与"预期 TRX 帧行"两列＝波 37 §九.4.2 那条"拆两列"升级的第一次实测，三处零映射差、零漂移；消息面逐枚等于预期红形状（M5c＝`Assert.False() Failure` Expected False／Actual True；S1＝`Strings differ`，Expected 七枚旧链名字串，Actual 空串；S2＝`Strings differ`，Expected `"const,static readonly"`，Actual 空串）。
2. **三枚预合成文本的并排自证由我亲手做了一遍**（波 37 我把 M5b 文本写坏之后新立的那条口径，第一次由我自己执行而不是由 lane 执行）：`git show 51f149d:<path>` 与盘上文本逐枚 `diff`＝改动面只有声明的那几行，M5c 只有 `:89` 且**收尾 `);` 在场**（这正是波 37 坏掉的那一枚），S1 只有 `:418-419` 并成 `:418`，S2 只有 `:423-424` 并成 `:423`；三枚 SHA 两路各测一次，逐字符等于工单 §三.0 声明值。
3. **M5c 的 `:244` 我判得比 lane 的措辞硬**：同一测试方法内第一条失败断言会抛出并终止执行，红点落在 `:245` 这件事**本身就证明** `:244` 已被执行且通过（同一方法更前的 `:241`／`:242` 同理），不需要另找一份"绿证"。⇒ `:245` 那句断言的**第一次单独响**成立；堆栈只有测试方法一帧＋两帧反射入口、无产品异常帧＝响的是承重那句断言自己。
4. **S1／S2 的机制面成立**：弄坏扫描函数后同一枚格里的 `Assert.Empty` 半边恒过（`:310`／`:336`，这正是常绿门的形状），探针半边当场红且 Actual 是空串；红点落在探针那句（`:320` 在循环体 `:310` **之后**＝五枚被扫产品文件全部跑完未抛）而不是循环体，这一条是本节要的形状。与波 34 的 B 类、波 36 的 C1／C2 合起来，两枚扫描格现在有三张面：**门咬真文本**（波 34＋波 36）／**门不因合法内容误报**（六轮随类通过）／**把仪器本身弄坏会被抓住**（本刀）。波 30 登记那句"仍欠把扫描函数本身弄坏那一半"自此结清。
5. **签账资格（第六次按同一把尺使用成动作）**：M5c 那只手全程未写判据字节——apply／revert 两侧读数都＝ `317d2171264f…`，与我当场 `sha256sum` 的载体工作副本同值，判据 last-touch 是 `e04e7e6`／`51f149d`＋`1e02205`，都不是本刀；S1／S2 两轮的判据字节被这只手写过（apply 侧 `3b55a389…`／`5388104f…`≠基准，revert 侧回基准）⇒ 按工单 §一.8 **只结机制面、绝不署第二换手**。另加一条我替 lane 收住的边界：M5c 的红**不新增换手记账**——`A_promise_send_…` 那枚格的第二换手已在波 37 结清，本刀结的是同一枚格里的**第二句断言**，格数与换手都不重复领（轮次口径≠格子口径）。
6. **收尾全量＝1406 的第四次同值复采**：四份先数满再逐份 `<Counters>` 当场求和＝108＋785＋280＋233＝**1406／executed 1404／failed 0**；两条跳过按 `outcome="NotExecuted"` 开标签逐字符点名＝`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline` 与 `Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`，与波 28–37 同名同枚；四份的 `notExecuted` 计数器仍恒 0、缺口只以开标签点名且 `total−executed=2` 自洽＝同一形状第四次复现（仪器口径，不是缺陷）。**本刀不顶账、不为顶账另起构建。**
7. **预算与盘面全对**：盘上 `.trx`＝7 枚＝定向 3×1＋全量 1×4，`voided` 目录 0 个（作废轮 0，无需留档）；`dotnet` 4/5、定向 3/3、全量 1/1、构建 0/1；四枚预检 P91–P94 我逐份 grep `VERDICT` 全 `BUILD_OK`、HOLD 重跑 0/16；预检与跑次按 mtime 一一配对（00:44:58＜00:45:20、00:46:00＜00:46:19、00:48:53＜00:49:13、00:49:54＜00:50:19），无"跳过预检起跑"的形状；四轮 run 日志 `no-build` 计数各 0；载体 `bin/Release/net10.0/Pry.Core.dll` 在盘（1,745,920 字节，mtime 00:46 落在本刀某轮增量重建里）⇒ 构建跳过判定三段当场成立；三计数出场 23／1／3（`public void` 24 枚＝23 枚 `[Fact]`＋1 枚 `[Theory]` 方法）⇒ **净新增格 0**。

### 九.2 划账（26 枚终账照旧；断言级子账按判据树分列，并就地更正我上一把的一句算术）

- **波 33 那 26 枚的终账不变＝26 枚红证＋换手双结／0 枚欠红证／0 枚欠换手**。本刀零新增格子，不动这本；`A_promise_send…` 那枚格本刀只是补了它内部第二句断言的读数。
- **断言级子账（新口径：按判据文件分列，不许跨树加总）**：
  - HDS-009 那棵（`StateAndLearningPrototypeTests.cs`）＝**已结 4／未结 2**：已结＝`:95`（波 37 M2b，Overlay 拒因字面量）、`:245`（本刀 M5c，`TaskCompletedProven`）、`:259`（波 37 M7b，经验有效期拒因）、`:365`（波 36 A8，来源缺失拒因）；未结＝`:367`（同一行返回同时喂 `:365`／`:367`，**本夹具无可达配方**）、探针正数面的单独读数（要新增判据才结）。
  - HDS-007 那棵（`BoundedAdvancementPrototypeTests.cs`）＝**未结 2**：`:195`／`:172`（波 32 立，身份＝**待找方向**，不是"够不着"）。
  - 合计**已结 4／未结 4**。
  - **我自己的错，就地更正不涂改**：波 37 报告 §九.2 那句写的是"已结 2／未结 3"，而同一句里列了四个名字（`:245`／`:195`／`:172`／`:367`），并且漏了波 36 A8 已结清的 `:365` ⇒ 错因两条＝① 把两棵判据树的欠账加进同一个格数、② 数名字时漏一枚。工单 §八.2 预登记的"已结 2→3、未结 3→2"沿用了同一份漏账，本刀按实测的八个名字重数，不按预登记的字面数。⇒ 可复用形式：**断言级子账的每一条必须带判据文件名，出账前逐枚点名再求和**（与"凡引用总数必须从盘上产物当场求和并写出求和式"同一条尺）。
- 其余账照旧不动＝旧欠 **75 格**（波 13 余 47＋波 20 余 28）不减、拆法原文 **46／45／1＝W2**、catalog **76**、跟踪文件条数见 §九.7、在册基线 **1406／executed 1404／failed 0／跳过 2** 不移动（旧值 1380／1357／1339／1333／1321／1275／1179／988 全过期）。

### 九.3 机制面为什么不能记成格子账，也不能顶替波 36 那两枚

S1／S2 那两枚红的来源是**判据自己的仪器函数被弄坏**，不是产品文本变坏。所以它的正确签到面是"这对门不是常绿"，不是"某个产品缺陷被抓住了"——机制面、格子面、换手面三条互不替代。反向同样成立：**本刀不许把波 36 的 C1／C2 降格或顶替**：C1／C2 结的是"门咬被扫产品面的真文本＋第二换手"（凭据＝消息面集合字面量逐字符等于当轮注入的那枚 token，且判据字节全程未动），本刀结的是"把扫描函数本身弄坏会被探针抓住"。两半各是一件事，合起来才是那两枚扫描格的完整证据形状；谁也不许拿对方那句"已结"去销自己的账。

### 九.4 归位判定＝零落地（同一条判据的第四次执行，与"字节写完不等于可归位"是一件事的两面）

终态净写入 0 字节：载体 `status --short`＝**0 行**、`fd93ebe..HEAD`＝恰 **2 行**（仍只有波 33 那两枚提交，本刀零提交）、六枚文件的载体工作副本 SHA 与 `git show 51f149d:<path>` 的 blob **逐枚同值**（我当场逐枚 `sha256sum` 重算，判据＝ `317d2171264f…`）。主树侧那条 0 行式子**两条腿当场都＝0 行**：`git diff --stat fd93ebe 7418615 -- src tests scripts Pry.slnx Directory.Build.props`＝0（派发刀只动文档）＋`git diff --stat 51f149d 1d2c033 -- <同一组路径>`＝0（归位后的主树产品面与产出 1406 那棵树逐字节相同）。⇒ 在树上做归位是**空转**（无新字节可落），与波 32／37 同形；本把唯一落地面＝文档（本报告＋下一把工单＋台账三处），`main` 只因文档刀挪 ref，基线不移动，构建预算不被"顶账"消耗。

### 九.5 押账与出闸（第三十九波＝旧欠 75 格的第一片）

- **探针正数面的单独读数：本刀裁"不单独落格"，并写清这是一条成本裁决不是"够不着"**。理由＝往 `StateAndLearningPrototypeTests.cs` 里加行＝**改过被守护文件的字节＝那 26 枚的既有反证凭证全部作废、必须逐枚重取**（波 25 的先例：我改一枚常量名并把 19 处断言换形状，就作废过重取 15 枚）。为结一条子账而净付 26 轮重取，是把欠账换成更大的欠账。⇒ 这条只能与**本来就要改这棵判据**的那把刀合并（接线刀，或 `:367` 那半句要拆成两个不同拒因的那把），单独立刀＝违规。工单 §四.3 那句"要新增判据才能结"照旧成立，本刀只是把"为什么不现在做"写成盘上事实。
- `:367` 照旧押，措辞纪律照波 37＝只签到"这个夹具的输入形状下没有可达配方"，不签"结构上永远够不着"；要拆得改判据或改产品语义，属接线刀。
- **第三十九波派旧欠 75 格的第一片**，派它的依据（按依赖与完整性排序，不写"最短路径"）：① 它是这本账里最大的一本（75 格），且**不依赖作者那句未拍的话**；② 接线刀要等作者拍（U-9 残句「切换那一步她要不要做什么」＝作者的话，我不拍也不催）；③ 真实模型对照要作者一句"可以跑"。波 32 的 `:195`／`:172` 不同树但同族，排进这一本的清点面（身份＝待找方向）。
- 照旧押着不动＝波 28 那枚数值锁格（零门槛是作者定的，不为凑红造门槛）、两套并行命名债（`Hds006*` 产品类型名＋契约 §7.2 的 `CognitionKind`／`CognitionOrigin`）、`PrototypeFiles` 写死五枚清单的覆盖盲区（登记给接线刀）、HDS-010 删除片（"替代"拿到实测前不可起跑）、HDS-007／HDS-009 的生产接线（属接线刀不属证据刀）。

### 九.6 未验证面（一条不减，且不许用"证据账结清"混过去）

真实模型对照＝零读数；生产接线＝这 26 枚至今**零真实调用方**；旧数据真实继承＝零读取；UI 可见行为＝零读数；生产库与 `%LOCALAPPDATA%\PryCompanion`＝零接触；待标定参数（关系／心情各档阈值、经验有效期、点击归因窗口）全部未定＝**特性不是缺陷**；并发与断电＝零读数；旧链删除片与"构建时不把旧链打进包"那道门＝未开始；覆盖率面＝零读数；Release 序列点把红点记到多行调用起始行的机理＝**已知形状未证**（本刀三轮实测都落在我事前登记的断言行上，没有一次需要靠该机理救回来，这不构成对该机理的证成）；`:367`／探针正数面／`:195`／`:172` 四条断言面未结。**四句话照旧不许写**＝新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链。

### 九.7 本把文件面与顶账

本把文件面＝新增工单（第三十九波）一枚＋本报告（lane 写 §〇–§七、守门人追加 §九，对本报告删除列恒 0）＋`PROJECT_PLAN.md` §4.2 的 HDS-009 行一处状态＋§9 一条 entry＋`full-scope-sprint.md` §0 一条事实句与 §3 一条密行。`PROJECT_PLAN.md` 用"同一处编辑施加到两版"的 hunk 手术（两份 added 行逐字符相同；提交后把含他人 18 行的那版原样铺回工作树并复测仍 18/0）。他人两枚 `M ` 暂存项（1 1／63 0）与全部未跟踪资产一字未动。顶账＝本报告与波 39 工单落盘暂存后当场 `git ls-files` 实测，不做加法预写。`main` 照旧只挪 ref、零 tag／push／打包；界面与对话今天零变化。
