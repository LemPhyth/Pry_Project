# HDS-009 第一片·第五刀（波 37）报告：波 33 那 7 枚 M 系格的第二换手（取红的手＝本 lane，判据字节零写入）＋三条断言级子账（M2b／M5b／M7b）＋一轮收尾全量（HDS-WAVE37-1028）

- 工单：`docs/dispatch-hds-wave37-1028.md`（本刀唯一权威工单）
- 执行方：冷启动 lane（载体＝沿用第三十三波那棵 `.local-notes/qa-wave33-1028/carrier`，未新建 worktree）
- 性质：证据刀·续跑（零新增判据格、零产品字节终态、十轮零判据字节写入、零落地、零归位、零顶账）
- 本刀只跑：M1→M2→M2b→M3→M4→M5→M5b→M6→M7→M7b→全部还原核六枚 SHA→F（收尾全量）；A 类／C 类一枚不重跑（波 33–36 已双结的账不署别人拿到的红）

> 状态词、结案词、在册基线判定、划账由守门人写，本 lane 不代写、不自封。四句话照旧不写＝新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链。

## 〇 开工前必读与入场

### 〇.1 开工前必读（全部读完整页；本节只登记"读了什么"，不引用其数字作为本刀读数）

1. `docs/dispatch-hds-wave37-1028.md`（本刀唯一权威工单，逐字读完：载体、预算 `dotnet` ≤11／定向 ≤10／全量 ≤1／构建 ≤1（可跳过）、HOLD 重跑 ≤16 且间隔 ≥5 分钟、跑序 M1→M2→M2b→M3→M4→M5→M5b→M6→M7→M7b→核 SHA→F、十轮各自预期恰响格名／红点／红形状／枚数、盘上预合成文本位置与十枚 SHA、报告路径、§一.4 仪器九面自证清单、§一.7 字节复制通道与判据路径硬拒凭据、§四 四本押账、§七 十一条不许走的捷径）。
2. `docs/plans/README.md`（现行工作规则权威副本 14 条）与 `docs/plans/PROJECT_PLAN.md`：§4.2 的 HDS-009 行（第 82 行，登记"波 33 那 7 枚 M 系就地改押第三十七波"）＋§9 最新一条 entry（在册版本＝「收第三十五波」：十轮只跑到 3/10＝合法 partial、HOLD 额度 8/8 用尽⇒F 未跑 1406 不复采；「收第三十六波」的登记在波 36 报告守门人 §九 里，报告文件当时仍未跟踪——主树 `docs/plans/PROJECT_PLAN.md` 工作副本带着他人 18 行在途研究登记（RES-RETC-001／RES-SURF-001），本刀只读未碰）。
3. `docs/agent-engineering-standards.md`（全文；交付形态按 §10 四项：问题与根因／实际改动／验证结果／剩余问题）。
4. `docs/eval/reports/2026-10-28-hds009-state-and-learning-prototype.md` §三.0（M1–M7 原始配方与原始论证七行；其行号是判据整形前的，本刀行号一律用 `51f149d` 侧现值）。
5. `docs/eval/reports/2026-10-28-hds009-wave36-red-handover-remainder.md`（上一把十一项写法＋守门人 §九.0 仪器口径／§九.1 七件判定（含 §九.1.3 的 1406 第二次同值复采）／§九.2 终账 19／7／0／§九.3 三处形状判定／§九.5 押账）——本报告按同一写法。
6. `src/Pry.Core/Narrative/StateAndLearning/` 五枚产品文件（逐行读：`StateLayering.cs` 102 行／`NarrativeAuthority.cs` 46 行／`DeliveryAttribution.cs` 94 行／`ExperienceMaterial.cs` 57 行／`LegacyInheritance.cs` 78 行；本刀要改坏的就是它们）。
7. `tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs`（判据本体 445 行、26 枚格；**本刀对它零字节写入**——本刀全部签账资格的唯一凭据）。
8. `.local-notes/wave37-1028/mutation-prep/`（守门人预合成十枚突变文本）与 `.local-notes/wave37-1028/gk37-prep.js`（复合脚本，含 `SCAN_GATE_TOKENS` 守卫与 M5b 行号自检打印）——本刀**只读、只按字节复制应用，不重造、不改写任何 replacement**。

### 〇.2 入场读数（当场取，未引用工单数字）

- `date`：`Wed Oct 28 22:57:24 2026`（第一次取）／`Wed Oct 28 23:00:56 2026`（写本节时复取）
- `git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD`：`fa6d24f85393abd7cc7d940537547951729a909e`（＝第三十五波收口文档刀线尾；本刀入场至交付期间主树线尾未再被观察到移动，出场复取见 §六）
- `git status --short --branch`（主树原样）：
  ```
  ## codex/ui-main-window-redesign
  M  docs/dispatch-tip1020-02-memory-group-judgment.md
   M docs/plans/PROJECT_PLAN.md
  M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs
  ?? .qoder/
  ?? docs/character-learning-safety.patch
  ?? docs/dispatch-hds-wave37-1028.md
  ?? docs/eval/reports/2026-10-28-hds009-wave36-red-handover-remainder.md
  ?? docs/research/HDS_INTERLUDE.md
  ?? docs/research/HDS_QQ_RESEARCH_INDEX.md
  ?? docs/research/QQ_BRIDGE.md
  ```
  两枚 `M ` 暂存项（`docs/dispatch-tip1020-02-memory-group-judgment.md`、`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`）与全部 `??`＝他人资产：本刀不读内容、不改、不提交、不 `stash`／`clean`／`rm`。` M docs/plans/PROJECT_PLAN.md` 是在册总计划的在途登记（他人 18 行研究条目），不属本刀文件面，只读未碰。（`?? docs/dispatch-hds-wave37-1028.md` 与 `?? docs/eval/reports/…wave36…md` ＝本刀工单与上一把报告，出闸时未跟踪，本刀同样不 `add` 不提交。）
- `git worktree list`（与本刀有关的两行；余 70+ 条均为他人 worktree，未动）：主树 `C:/Users/24782/Desktop/Pry_Project  fa6d24f [codex/ui-main-window-redesign]`；载体 `C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave33-1028/carrier  51f149d (detached HEAD)`＝本刀沿用，**未新建 worktree**。
- `git log --oneline -3`（主树）：`fa6d24f`（收第三十五波）→ `a955f22`（收第三十四波）→ `9a15e2a`（收第三十三波）。

### 〇.3 载体入场自证（右端一律当场 `git rev-parse HEAD` 取，不预写 SHA）

- `git -C <carrier> rev-parse HEAD` = `51f149d0b722e28a6566c93d19f48a94ca4c85df` ✓（等于工单指定线尾）
- `git -C <carrier> status --short` = **0 行**（当场 `| wc -l`＝0，输出为空）✓
- `git -C <carrier> log --oneline fd93ebe..HEAD` = **2 行** ✓（`1e02205` 落六枚新文件＋`51f149d` 一处判据整形）；本刀零提交，出场仍应是这 2 行。
- `git -C <carrier> diff --stat fd93ebe HEAD -- src tests scripts Pry.slnx Directory.Build.props` = **6 files changed, 822 insertions(+), 0 deletions** ✓（逐枚行数 94／57／78／46／102／445）
- 主树侧那条 0 行式子（右端＝当场 `git rev-parse HEAD`＝`fa6d24f85393abd7cc7d940537547951729a909e`）：`git -C <main> diff --stat 51f149d fa6d24f -- src tests scripts Pry.slnx Directory.Build.props` = **0 行**（当场跑，输出为空，`| wc -l`＝0）✓ ⇒ 载体树与主线归位后的产品／判据字节逐字符相同，本刀在载体里取到的红就是主线那棵树的红。
- EOL 形状复核（决定"按字节还原"能否成立）：五枚产品文件载体工作副本 `grep -c $'\r'` 全＝**0** ⇒ 工作副本字节＝blob 字节，还原写成 blob 原字节即等价于 checkout 形状。

### 〇.4 六枚文件入场 SHA-256 基准（当场 `sha256sum` 三侧各取一次：载体工作副本＝`git show 51f149d:<path>` blob＝主树工作副本；三侧逐字符相同才起跑）

| 文件（相对路径） | 主树工作副本＝载体工作副本＝`51f149d` blob（三侧同值） |
|---|---|
| `src/Pry.Core/Narrative/StateAndLearning/StateLayering.cs` | `af727a9c04355d853f756a8cd6d22e374ac889042c847f99906784e0bb20ca3a` |
| `src/Pry.Core/Narrative/StateAndLearning/NarrativeAuthority.cs` | `000f2fc8bfd5d73ba7863029c8acafd7aa9df92ef11c910ccda3a28db3f13c97` |
| `src/Pry.Core/Narrative/StateAndLearning/DeliveryAttribution.cs` | `5858a75da3ed70e35f93782e1d760c87eea3feeecdafcdd645b27f69ea66f0ab` |
| `src/Pry.Core/Narrative/StateAndLearning/ExperienceMaterial.cs` | `289fc89be087fa35bace12b33805060562a210764a9774102b5a2875fcd55169` |
| `src/Pry.Core/Narrative/StateAndLearning/LegacyInheritance.cs` | `6a1f50999e7caf1d6d29cc07bbe752f89ea17851999013c00b167e590f3498ab` |
| 判据 `tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs` | `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc` |

判据文件全程零字节写入＝本刀十轮（七枚 M 格）能签第二换手的唯一凭据。判据字节的 last-touch 在场：主树侧 `git log -1 -- <判据>`＝`e04e7e6`（守门人按路径落地的波 33 代码刀）、载体侧 `51f149d`（波 33 的判据整形）——两者都不是本刀这只手。

### 〇.5 在册基线起点（当场从波 36 报告守门人 §九.1.3 读到的原句，不做加法）

- 波 36 报告守门人 §九.1.3 原文：「total 1406／executed 1404／failed 0／两份 `NotExecuted` 点名与波 28–35 同名同枚，仪器判定 `SAME_AS_BASELINE_1406=true`。**这是 1406 的第二次同值复采**（第一次＝波 34）」；更早＝波 33 把 1380 顶到 1406。本刀 §〇.5 起点＝**1406／executed 1404／failed 0／跳过 2**（分项 Client 108／App 233／Api 280／Core 785、Core executed 783）。
- 本刀**新格数＝0**（工单 §二/§五.6 明令，净新增格数必须 0），所以 F 轮出口读数应当**仍是 1406**：读到别的数先怀疑树没还原干净，不改判据、不改基线、照实登记并作废那一轮。

### 〇.6 取数仪器与它的自证（口径⓪：先证明仪器读得对，再采信它的读数）

仪器＝本 lane 自写 `.local-notes/wave37-1028/w37-count.py`（不复用 wave36 的 `w36-count.py`／`w36-mutate.py` 或更早任何一把的实现；读过 `w36-mutate.py` 与 `gk37-prep.js` 作通道参考，写入通道＝本刀自写 `w37-apply.py`）。计数口径＝① 只数 `<UnitTestResult … outcome="Failed">` 的**开标签**（逐 occurrence 找 `<UnitTestResult`，引号感知扫到标签尾 `>`，从属性段取 `outcome`／`testName`；自闭合与成对写法同一切法，全程不用开—闭配对正则做计数）；② 反向锚定面（face B）＝逐 occurrence 找 `outcome="Failed"` 回溯最近的 `<` 判标签名（专抓 `<ResultSummary outcome="Failed">` 这类不算用例的结点），A≠B 即打印 `SELF-CHECK MISMATCH`；红数与同份 `<Counters failed>` 不等即打印 `DISAGREE`；③ 红格名先剥全限定前缀与参数表（`split('(')[0].split('.').pop()`）再回判据源码验"方法名＋`(`"真实存在；④ 消息／栈行号从该结点的 `<Output>` 体抽取并解 `&#xD;` 等数字实体，红点行号＝TRX 里 `StateAndLearningPrototypeTests.cs:line N` 首帧（异常轮另记产品帧行号）。

**起跑第一轮之前先拿盘上九组已知读数自证**（工单 §一.4 的九面：波 33 `trx-M1`＝恰 1、`trx-M5`＝恰 1、`trx-M7`＝恰 1、`trx-F`＝4 份 failed 全 0 且 total 求和 1406／executed 1404／passed 1404＋两条 `NotExecuted` 按开标签点名、波 34 `trx-A1`＝恰 2、`trx-A6`＝恰 1（红名逐字符含 `(suppressed: True, deduplicated: False, rejected: False)`）、波 35 `trx-A1|A2|A3`＝各恰 1（红点 `:64`／`:115`／`:195`）、波 36 `trx-A4`…`trx-C2`＝各恰 1、波 36 `trx-F`＝1406 同值）。整段输出归档 `out/instrument-selfproof.txt`；九面任一不符＝先修仪器再起跑，不改判据。自证过程与结论见 §〇.6.1（跑完后就地追加）。

写入仪器＝`.local-notes/wave37-1028/w37-apply.py`：`apply <ID>` ＝**整文件字节复制** `mutation-prep/<ID>_<basename>` → 载体目标路径（不重排、不改行尾、不手写 replacement），复制后立即 `sha256sum` 并对照工单 §三.0 该轮 SHA；`revert <ID>` ＝把 `git show 51f149d:<path>` 的字节原样写回（零 `checkout`/`restore`）并核 §〇.4 基准；`verify` ＝六枚工作副本逐字符对 blob；**任何写入目标先过硬守卫＝判据路径一律 REFUSED（非零退出、不写一个字节）**。守卫活着的凭据（工单 §五.7）＝本刀拿一次"试图写判据路径"的调用真验出报错，整段原文见 §〇.6.2（跑完后就地追加）。

### 〇.7 焦点预检与预算声明

- 预检脚本＝`.local-notes/game-preflight6-1022.ps1`（在盘，`-rw-r--r-- … 9675 Oct 22 21:15`）；每一次 `dotnet`（含每一枚突变轮与收尾全量）之前当场重取；编号从 **P80** 起（P1–P79 已被前十把用掉）；整段输出贴进报告并归档 `out/preflight-P<编号>.txt`。只有 `VERDICT=BUILD_OK` 才起跑；读到 `HOLD`＝不起跑、不请示、不改判据、不绕过，先把零 `dotnet` 的活做完，HOLD 后重跑最多累计 **16** 次、相邻两次不短于 **5 分钟**（额度按刀计）。16 次后仍 HOLD ⇒ 带着"本刀零 `dotnet` 起跑"或"只跑到第 N 轮"的交付回来，收尾全量不跑＝基线不移动，合法交付。`free_memory_gb` 偏低或读数呈 SQLite `disk I/O`／`out of memory` 形态时那一轮读数作废（他机器资源形状，非断言失败），不动任何进程。
- 预算：解决方案级构建 ≤1；解决方案级全量 ≤1（语义＝交付的读数只许来自 1 次解决方案级全量）；定向 ≤10；`dotnet` 总调用上限＝**11**。十轮与 F 一律**不带 `--no-build`**；一次一枚突变一轮（M5 与 M5b 不合并）；定向轮不重跑第二轮。
- **B1（解决方案级构建）跳过判定登记**：§〇.3 主树侧 0 行式子成立 ✓（右端 `fa6d24f` 当场现取）；载体 `src/Pry.Core/bin/Release/net10.0/Pry.Core.dll` 在盘（`-rwxr-xr-x … 1745920 Oct 28 22:33`＝波 36 F 轮产物）；六枚 SHA 自入场未变（§〇.4 三侧同值）；载体 `status --short`＝0 行 ⇒ 按工单 §一.1 允许跳过 B1 并登记，构建预算用 0/1。每一轮定向与收尾全量自行增量重建。作废轮（如有）结果目录一律改名留档 `trx-<轮>-voided-<预检号>/`，不清空、不删除、不重跑同名目录。
- 动第一次 `dotnet` 之前完成的零 `dotnet` 活（本节即凭据）：§〇 全部＋§二 三计数＋§三.0 十行论证逐枚盘上复核（见 §三.0R）＋十枚预合成文本当场 `sha256sum` 与工单 §三.0 逐字符对账（见 §三.0P，十枚全配）＋十枚突变文本对两枚扫描谓词的交叉面自扫＋仪器九面自证（§〇.6.1）＋写入仪器判据路径守卫的报错实证（§〇.6.2）。

### 〇.8 他人资产与 git 写边界声明

主树全程零 `git commit/add/checkout/reset/cherry-pick/merge/tag/push/apply/stash/clean/rm`；两枚 `M ` 暂存项与全部 `??` 未读不改。载体内只发生文件内容写入（十轮临时突变＝整文件字节复制）与逐轮按 `git show 51f149d:<path>` 的 blob 字节还原，本刀**零提交**（线尾仍应是 `51f149d`）。主树唯一新文件＝本报告，不 `git add`、不提交。本刀归位是空转：终态净写入 0 字节⇒无代码刀、`main` 不因本刀挪。

### 〇.6.1 仪器九面自证（动第一次 `dotnet` 之前跑，零 dotnet 调用）

`python w37-count.py selfproof` 整段输出存档 `out/instrument-selfproof.txt`（退出码 0）。结论行与逐面判定（当场原文）：

```
FACE w33 trx-M1 ==1               => AGREE
FACE w33 trx-M5 ==1               => AGREE
FACE w33 trx-M7 ==1               => AGREE
FACE w33 trx-F 4files/1406        => AGREE
FACE w33 trx-F NotExecuted names by open tag => AGREE
FACE w34 trx-A1 ==2               => AGREE
FACE w34 trx-A6 ==1+params        => AGREE
FACE w35 trx-A1 ==1 line:64       => AGREE
FACE w35 trx-A2 ==1 line:115      => AGREE
FACE w35 trx-A3 ==1 line:195      => AGREE
FACE w36 trx-A4 ==1               => AGREE
FACE w36 trx-A5 ==1               => AGREE
FACE w36 trx-A6 ==1               => AGREE
FACE w36 trx-A7 ==1               => AGREE
FACE w36 trx-A8 ==1               => AGREE
FACE w36 trx-C1 ==1               => AGREE
FACE w36 trx-C2 ==1               => AGREE
FACE w36 trx-F 1406 same-value    => AGREE
SELFPROOF FACES=9 AGREED=9 DISAGREED=0 => ALL NINE FACES AGREE
```

九面（含 18 行分项面）全部一次通过，本刀仪器未被判坏；判据与盘上 TRX 一个字节未动（自证只读）。比对纪律两处已知坑本刀仪器内置绕开：红名回判据用剥前缀剥参数表的 `split('(')[0].split('.').pop()`（面 6 的 `(suppressed: True, deduplicated: False, rejected: False)` 逐字符含参数表面另用全名包含断言），计数不用开—闭配对正则（face A 开标签切面与 face B 反向锚定面独立各数，18 行全部 `faceA=faceB`、与 `<Counters failed>` `reconcile=OK`）。

### 〇.6.2 写入仪器判据路径守卫的报错实证（工单 §五.7 要的凭据；零 dotnet）

调用命令＝`python w37-apply.py guard-probe`，整段原文存档 `out/guard-probe.txt`（退出码 0）：

```
probe: attempting safe_write(tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs, b'GUARD PROBE - must never land', 'guard-probe')
CAPTURED PermissionError: REFUSED: guard-probe targets the criteria file tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs - wave37 lanes must not write criteria bytes (dispatch 二/七.2); nothing was written.
criteria_sha_before=317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc criteria_sha_after=317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc unchanged=True
```

守卫活着＝一次"试图写判据路径"的调用真验出 `PermissionError: REFUSED`（非零路径未走通、写入前抛出），前后判据 SHA 同值 `317d2171…`（等于 `51f149d` blob）⇒ 一个字节没落。十轮全部写入只走 `safe_write`，同一守卫。

## 一 文件面自证（本刀实际写入清单）

- `.local-notes/wave37-1028/`（gitignored，本刀自建，工单产物目录 `mutation-prep/` 与 `gk37-prep.js` 为守门人产物只读未改）：`w37-count.py`（取数仪器）、`w37-apply.py`（写入仪器：字节复制 apply／blob revert／verify／判据路径硬守卫）、`out/`（预检、各轮 TRX、apply／run／count／revert 存档、仪器自证、守卫实证、终验）。
- 主树唯一新文件＝本报告 `docs/eval/reports/2026-10-28-hds009-wave37-m-second-hand-red.md`。
- 载体内：仅 `src/Pry.Core/Narrative/StateAndLearning/*.cs` 的**临时**突变（十轮各自的宿主文件，整文件字节复制自 `mutation-prep/`），逐轮按 blob 字节还原；终态相对 `51f149d` 净写入 0 字节。
- 判据文件 `tests/Pry.Core.Tests/StateAndLearningPrototypeTests.cs`：**零字节写入**（十轮每一轮 ⑩ 项 apply 侧与 revert 侧各取一次 SHA-256，对齐 `317d2171…`）。
- 未新建任何产品文件、未新建任何测试文件、未碰 `scripts/**`／`Pry.slnx`／`Directory.Build.props`／其他 `docs/**`／主树任何文件；未重跑 A 类／C 类任何一轮；未碰 §四 四本押账。

## 二 三计数自证（不许新增判据格）

判据文件（载体侧入场当场 `grep -c`；主树侧与 `51f149d` blob 字节同一，见 §〇.4 三侧同值）：`[Fact]` = **23**、`[Theory]` = **1**、`[InlineData` = **3** ⇒ 展开节点 23＋3＝26 枚格。与波 33／34／35／36 逐位相同 ⇒ 本刀净新增格数＝0。本刀十轮只把整文件复制进五枚产品文件（逐轮还原），不改任何断言、不改 `[InlineData]` 期望值、不 `Assert.Skip`、不注释既有断言、不新增数字门槛、不新增判据格；终态侧将在 §六 再当场复数一次。

## 三 定向轮（跑序固定 M1→M2→M2b→M3→M4→M5→M5b→M6→M7→M7b→全部还原核六枚 SHA→F；M1–M7 配方原文＝波 33 报告 §三.0，M2b／M5b／M7b 出处＝本工单 §三.2）

### 三.0 十轮预期（逐字承自工单 §三.0——**预期枚数／格名／红形状是假设不是结论**：实测与之不符 ⇒ 整轮作废、红不署、不追第二轮、不许把本节改成实测值。行号＝`51f149d` 侧现值）

定向跑法（工单 §一.6 逐字，只换结果目录名，在载体内跑，不带 `--no-build`）：

```
dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave37-1028/out/trx-<轮>" --logger trx
```

| 轮 | 类别 | 文件:行 | 预合成 SHA-256（工单 §三.0） | 预期恰响格（前缀 `Pry.Core.Tests.StateAndLearningPrototypeTests.`） | 预期红点与红形状 | 枚数 |
|---|---|---|---|---|---|---|
| M1 | 摘闸（行为） | `StateLayering.cs:73-75` | `e623855896877c9f120c7adea35c8eceae8ec1b246bccfc2b987cc402ab31c45` | `A_narrative_output_may_not_rewrite_the_stable_canon_layer` | `:50`，`Assert.Equal` Expected `"Rejected"`／Actual `"Applied"` | 恰 1 |
| M2 | 摘闸（行为） | `StateLayering.cs:90-94` | `23235ae5db95ce6ef19bdf97c2ddbfc1eae609c7293a72feb7a67a25a2c3c60a` | `An_overlay_without_a_pointable_source_and_version_is_refused` | 异常侧：测试帧 `:90`，产品帧漂到 `:95`，`NullReferenceException` | 恰 1 |
| M2b | 文本面 | `StateLayering.cs:94` | `75fef21f3b237cf4be81faba456a22326c4fd7aedb7bf0ba1d21552a69605972` | `An_overlay_without_a_pointable_source_and_version_is_refused` | `:95`，`Assert.Equal` Strings differ | 恰 1 |
| M3 | 摘闸（行为） | `NarrativeAuthority.cs:36-38` | `74e2d50523a6f6ead6e58147818dab98184240d890db2d6d69d9edea62fc8859` | `A_narrative_candidate_may_not_modify_the_author_boundary` | `:142`，`Assert.False() Failure` | 恰 1 |
| M4 | 摘闸（行为） | `DeliveryAttribution.cs:63-65` | `00307f0a4c7df3b070bf5d1f23f4efba204378473a218b765003f44139153e40` | `An_undelivered_body_is_not_attribution_evidence` | `:181`，`Assert.False() Failure` | 恰 1 |
| M5 | 翻布尔 | `DeliveryAttribution.cs:88` | `870064705c4220a714793dcc5b8741f0fdcba4ddc060d554855c165fd1bed96c` | `A_promise_send_proposes_a_commitment_but_proves_neither_consent_nor_completion` | `:244`，`Assert.False() Failure` | 恰 1 |
| M5b | 翻布尔 | `DeliveryAttribution.cs:89` | `676412b758cc121c316bd57953d676effb281775094378fdb227085f267c4b26` | `A_promise_send_proposes_a_commitment_but_proves_neither_consent_nor_completion` | `:245`，`Assert.False() Failure`（`:244` 保持通过） | 恰 1 |
| M6 | 翻判定 | `LegacyInheritance.cs:77` | `437bde00da315d2f923cbde83fb0004d1da62e5efe8b35ac45fe139302059a79` | `There_is_no_second_write_path_back_to_the_legacy_layer` | `:411`，`Assert.Equal` Expected `"Refused"`／Actual `"Written"` | 恰 1 |
| M7 | 摘闸（行为） | `ExperienceMaterial.cs:40-43` | `2269c0a7e764e05898e6c03f104f178a514f753bcc812a5079381ab524a74564` | `Reading_experience_as_optional_material_without_a_validity_window_fails_loudly` | 异常侧：测试帧 `:256`，产品帧漂到 `:46`，`InvalidOperationException`（Nullable 取值） | 恰 1 |
| M7b | 文本面 | `ExperienceMaterial.cs:43` | `3066dfc65a4c59ed2c38a010bbd344c869438f76b32bd4cb08e3387db796a49c` | `Reading_experience_as_optional_material_without_a_validity_window_fails_loudly` | `:259`，`Assert.Equal` Strings differ | 恰 1 |

### 三.0P 盘上预合成文本 SHA 复核（零 dotnet，动第一次 `dotnet` 之前当场 `sha256sum .local-notes/wave37-1028/mutation-prep/*`）

十枚盘上值与工单 §三.0 表**逐字符全配**：M1 `e623855896…`、M2 `23235ae5db…`、M2b `75fef21f3b…`、M3 `74e2d50523…`、M4 `00307f0a4c…`、M5 `870064705c…`、M5b `676412b758…`、M6 `437bde00da…`、M7 `2269c0a7e7…`、M7b `3066dfc65a…`（完整值见上表与 §六 存档 `out/mutation-prep-sha-verify.txt`）。

### 三.0R 本刀盘上复核（零 dotnet；只登记盘上事实，不改 §三.0 任何一字）

1. **十枚突变文本逐枚与 `51f149d` blob 做 diff**：改面与工单 §三.0"突变内容"列逐枚相符——M1 摘 `:73-75` 换一行注释；M2 摘 `:90-94` 换一行注释；M2b `:94` 拒因串加 `_wave37_alias` 后缀（行数不变）；M3 摘 `:36-38`；M4 摘 `:63-65`；M5 `:88` `false,`→`true,`（`:89` 不动）；M6 `:77` `=> false;`→`=> true;`；M7 摘 `:40-43` 换一行注释；M7b `:43` 拒因串加 `_wave37_alias` 后缀。**M5b 例外，盘上事实照实登记**：`gk37-prep.js` 用 `cut(blob, 89, 89, '            true,')` 整行替换 `:89`，原行是 `            false);`——替换后该行＝`            true,`，**`return new AttributionReadout(` 的收尾 `);` 不在**（盘上 M5b 文本 `:88..:90` ＝`false,`／`true,`／`    }`，`tail -c 200` 与逐行 diff 均如此）。静态读码判断＝该文本不是语法完整的 C#（不闭合括号 ⇒ 编译期 CS 错误而非运行时单格红）。本刀不改写预合成文本、不修 replacement，按工单 §一.7 只作字节复制应用与实测；若实测形状≠§三.0 预期（恰 1 枚红），该轮按工单判废、红不署、不追第二轮、如实登记。
2. **消费点复核（从干净树逐枚读判据）**：与工单 §三.1 一致——`ReviseCanon(…, true, …)` 只判据 `:47` 一处（M1 恰 1）；`WriteOverlay` 缺来源入口 `:90`／`:91` 同格、`:108` 来源齐（M2 恰 1，红在异常侧：`:90` 先抛，`Assert` 序列走不到）；`Assess` 单独 `ModifiesAuthorBoundary=true` 只 `:140`（M3 恰 1；`:164` 三面全给被第一道闸先拦）；`Admit` `BodyDelivered: false` 只 `:177`（M4 恰 1）；`AttributePromise` 全判据只 `:238` 一处、`:244`/`:245` 各断一枚布尔（M5/M5b 各恰 1 的预期来源）；`IsLegacyWritebackAllowed()` 唯一读点经 `AttemptWriteback`、唯一调用点 `:408`（M6 恰 1）；`ReadOptional(null)` 只 `:256`（M7 恰 1，异常侧：摘闸后 `:256` 走到产品原 `:49`＝漂移 `:46` 的 `validityWindow.Value` 抛出）；M2b/M7b 各打红 `:95`/`:259` 的 `Assert.Equal`。
3. **扫描谓词交叉面**：十枚预合成文本逐枚扫两枚扫描格的谓词 token（`LegacyChainNames` 七枚名字＋`const `＋`static readonly`）——**命中 0**（与守门人 `gk37-prep.js` 的 `SCAN_GATE_TOKENS=0` 独立复算一致；注：M5 与 M5b 的行内容 `true,`/`false,` 不含任何 token，注释行 `[MUTATION …]` 亦不含）⇒ 十轮的红只可能来自行为／文本面那格，不会连带打红两枚扫描格。存档 `out/scan-predicate-crosscheck.txt`。
4. **判据 `:99`／产品漂移行号核算**：M2 摘 `:90-94`（5 行→1 行，净 −4）⇒ 产品原 `:99` 漂至 `:95` ✓；M7 摘 `:40-43`（4 行→1 行，净 −3）⇒ 产品原 `:49` 漂至 `:46` ✓；均与工单 §三.0 预期一致。

### 三.P 焦点预检台账（逐枚当场登记，含 HOLD；每一次 `dotnet` 之前一枚，编号从 P80 起）

| 编号 | 时刻（归档文件 mtime） | VERDICT | 归档 | 处置 |
|---|---|---|---|---|
| P80 | 23:06:39 | **BUILD_OK** | `out/preflight-P80.txt`（整段原文见 M1 节⑪） | 放行 M1（本刀 `dotnet` 计数 1）；前台＝Godot pid=40976 class=Engine `covers=False`、`free_memory_gb=11.3` |
| P81 | 23:10:57 | **BUILD_OK** | `out/preflight-P81.txt`（原文见 M2 节⑪） | 放行 M2（计数 2）；Godot pid=53448 `covers=False` |
| P82 | 23:14:16 | **BUILD_OK** | `out/preflight-P82.txt`（原文见 M2b 节⑪） | 放行 M2b（计数 3）；前台 ChatGPT pid=16348 `covers=False` |
| P83 | 23:15:23 | **BUILD_OK** | `out/preflight-P83.txt`（原文见 M3 节⑪） | 放行 M3（计数 4）；前台 ChatGPT pid=16348 `covers=False` |
| P84 | 23:17:11 | **BUILD_OK** | `out/preflight-P84.txt`（原文见 M4 节⑪） | 放行 M4（计数 5）；Godot pid=40976 `covers=False` |
| P85 | 23:18:14 | **BUILD_OK** | `out/preflight-P85.txt`（原文见 M5 节⑪） | 放行 M5（计数 6）；Godot pid=53448 `covers=False` |
| P86 | 23:19:16 | **BUILD_OK** | `out/preflight-P86.txt`（原文见 M5b 节⑪） | 放行 M5b（计数 7）；该轮后来按工单判废（编译期失败，见 M5b 节） |
| P87 | 23:39:10 | **BUILD_OK** | `out/preflight-P87.txt`（原文见 M6 节⑪） | 放行 M6（计数 8）；Godot pid=40976 class=Engine `covers=False` |
| P88 | 23:49:16 | **BUILD_OK** | `out/preflight-P88.txt`（原文见 M7 节⑪） | 放行 M7（计数 9）；前台 msedge pid=28260 `covers=False` |
| P89 | 23:52:05 | **BUILD_OK** | `out/preflight-P89.txt`（原文见 M7b 节⑪） | 放行 M7b（计数 10）；前台 ChatGPT pid=16348 `covers=False` |
| P90 | 23:58:53 | **BUILD_OK** | `out/preflight-P90.txt`（原文见 F 节⑪） | 放行 F 收尾全量（计数 11）；前台 ChatGPT pid=16348 `covers=False` |

台账合计：11 枚预检全部 `VERDICT=BUILD_OK`，HOLD＝**0 次**（额度 16 未动用，不存在间隔计时问题）；每一次 `dotnet` 之前恰有一枚在场预检，无绕过、无复用以轮代检。

## 三.M 定向轮逐轮十一项（一轮一节，跑完即追加；宿主文件写入＝`w37-apply.py` 整文件字节复制自守门人 `mutation-prep/`，还原＝`git show 51f149d:<path>` blob 字节写回）

### M1（摘闸·稳定层对叙事产出免疫）——预检 P80＝BUILD_OK

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `src/Pry.Core/Narrative/StateAndLearning/StateLayering.cs:73-75` |
| ② 改前原文 | `        // 拒因一·稳定层对叙事产出免疫。摘掉这一行 → 只有"叙事不得改写稳定层"那格会红。` ／ `        if (request.FromNarrativeOutput)` ／ `            return new LayerReadout(request.CanonKey, "Rejected", "canon_immune_to_narrative_output", null);` |
| ③ 改后原文 | `        // [MUTATION M1] 摘掉"稳定层对叙事产出免疫"那道检查。`（＝`mutation-prep/M1_StateLayering.cs` 整文件字节复制，净 −3 行） |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/StateAndLearning/StateLayering.cs | 4 +---`／` 1 file changed, 1 insertion(+), 3 deletions(-)`（存档 `out/m1-apply.txt`；当轮载体 `status --short`＝1 行 ` M` 该文件） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave37-1028/out/trx-M1" --logger trx`（`dotnet` 计数 1） |
| ⑥ TRX 文件名 | `out/trx-M1/24782_PHYTHLINK_2026-10-28_23_07_27.trx` |
| ⑦ 红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.StateAndLearningPrototypeTests.A_narrative_output_may_not_rewrite_the_stable_canon_layer`（逐字符＝§三.0 预期）；消息＝`Assert.Equal() Failure: Strings differ`，Expected `"Rejected"`／Actual `"Applied"`（＝预期红形状）；红点＝TRX 测试帧 `StateAndLearningPrototypeTests.cs:line 47`（断言语句 `Assert.Equal("Rejected", readout.Outcome)` 在判据 `:50`——Release 序列点把红记在多行调用起始行＝工单 §六 与波 35 §九.3／波 36 §九.1.1 登记的仪器已知形状，归属由 testName＋消息定，不影响署名）。仪器读数（`out/m1-count.txt`）：`total=26 executed=26 passed=25 countersFailed=1 faceA=1 faceB=1 selfCheck=OK reconcile=OK notExecutedOpenTags=0 allRedNamesInCriteria=True` |
| ⑧ 还原命令 | `python w37-apply.py revert M1`（内部＝`git show 51f149d:src/.../StateLayering.cs` 字节原样写回，零 `checkout`/`restore`） |
| ⑨ 还原后 `git status --short` 行数 | `0` |
| ⑩ SHA-256 | 突变态实测 `e623855896877c9f120c7adea35c8eceae8ec1b246bccfc2b987cc402ab31c45` ＝工单 §三.0 M1 值（match=True）；还原后 `af727a9c04355d853f756a8cd6d22e374ac889042c847f99906784e0bb20ca3a` ＝ §〇.4 基准（match=True）；**本轮判据字节未写入**：判据文件 apply 侧与 revert 侧两枚读数均＝ `317d2171264f5fc72937716fb43d53182c97d08eb22d407f2f47952060ede4dc`（＝`51f149d` blob） |
| ⑪ 当轮预检 | P80＝`VERDICT=BUILD_OK`（23:06:38，归档 `out/preflight-P80.txt`；整段原文见下） |

P80 整段输出原文：

```
=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===
  FOREGROUND: Godot pid=40976 class=Engine style=0x16CF0000 rect=1359,118,1116x1227 screen=2560x1440 covers=False borderless=False title=???? ? ??????? (DEBUG)
  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=18308, TextInputHost pid=21788, explorer pid=7404
gpu_util=16% used=2156MB
free_memory_gb=11.3
VERDICT=BUILD_OK
```

形状裁决＝实测恰 1 枚、格名与 §三.0 预论证逐字符相同、消息＝预期红形状（Expected "Rejected"／Actual "Applied"）⇒ 与事前论证一致，本轮有效（红是否署名由守门人裁，本 lane 不写状态词）。

### M2（摘闸·Overlay 来源闸整道；异常侧）——预检 P81＝BUILD_OK

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `src/Pry.Core/Narrative/StateAndLearning/StateLayering.cs:90-94` |
| ② 改前原文 | `        // 拒因二·覆盖层必须携带可指认来源与版本（来源标识非空 且 版本号在场）。` 起 5 行（`if (draft.Source is null … \|\| draft.Source.SourceVersion is null)` ＋ `return new LayerReadout(draft.OverlayKey, "Rejected", "overlay_requires_pointable_source_and_version", null);`） |
| ③ 改后原文 | `        // [MUTATION M2] 摘掉"覆盖层必须携带来源与版本"那道检查。`（＝`mutation-prep/M2_StateLayering.cs` 整文件字节复制，净 −4 行） |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/StateAndLearning/StateLayering.cs | 6 +-----`／` 1 file changed, 1 insertion(+), 5 deletions(-)`（存档 `out/m2-apply.txt`；当轮载体 `status --short`＝1 行 ` M`） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave37-1028/out/trx-M2" --logger trx`（`dotnet` 计数 2） |
| ⑥ TRX 文件名 | `out/trx-M2/24782_PHYTHLINK_2026-10-28_23_11_39.trx` |
| ⑦ 红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.StateAndLearningPrototypeTests.An_overlay_without_a_pointable_source_and_version_is_refused`（逐字符＝§三.0 预期）；消息＝`System.NullReferenceException : Object reference not set to an instance of an object.`（＝预期异常侧）；红点＝测试帧 `StateAndLearningPrototypeTests.cs:line 90`（＝预期），产品帧 TRX 原文 `StateLayering.cs:line 93`。**产品帧行号与 §三.0 预期"`:95`"差 2 行，登记两处盘上事实**：(a) 突变态 `:93` ＝ `_overlays.Add(new OverlayRecord(` 语句起始行，被解引用的 `draft.Source.SourceId` 在突变态 `:95`（原 `:99` 净 −4 漂移），Release 序列点把帧记到多行调用起始行＝工单 §六 与波 35 §九.3／波 36 §九.1.1 已登记的同一族已知形状；(b) 波 33 那只手的原始产物 `../wave33-1028/out/trx-M2` 用本刀仪器复测＝产品帧同为 `:93`、测试帧同为 `:90`（`out/m2-wave33-compare.txt`）——本刀读到的帧与判据作者当年读到的帧逐字符相同。仪器读数（`out/m2-count.txt`）：`total=26 executed=26 passed=25 countersFailed=1 faceA=1 faceB=1 selfCheck=OK reconcile=OK notExecutedOpenTags=0 allRedNamesInCriteria=True` |
| ⑧ 还原命令 | `python w37-apply.py revert M2`（`git show 51f149d:` blob 字节写回） |
| ⑨ 还原后 `git status --short` 行数 | `0` |
| ⑩ SHA-256 | 突变态实测 `23235ae5db95ce6ef19bdf97c2ddbfc1eae609c7293a72feb7a67a25a2c3c60a` ＝工单值（match=True）；还原后 `af727a9c0435…` ＝ §〇.4 基准（match=True）；判据 apply／revert 两侧两枚读数均＝ `317d2171264f…`（＝`51f149d` blob） |
| ⑪ 当轮预检 | P81＝`VERDICT=BUILD_OK`（23:10:56，归档 `out/preflight-P81.txt`；整段原文：Godot pid=53448 `covers=False`，`gpu_util=15% used=2132MB`，`free_memory_gb=12.3`） |

形状裁决＝实测恰 1 枚、格名逐字符相符、红在异常侧（NRE）且测试帧＝预期 `:90` ⇒ 与事前论证一致，本轮有效；产品帧 `:93` 对 `:95` 的映射差异如实登记如上（不改动 §三.0 任何一字），署名与裁账归守门人。

### M2b（文本面·Overlay 拒因字面量；＝断言级子账一）——预检 P82＝BUILD_OK

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `src/Pry.Core/Narrative/StateAndLearning/StateLayering.cs:94` |
| ② 改前原文 | `            return new LayerReadout(draft.OverlayKey, "Rejected", "overlay_requires_pointable_source_and_version", null);` |
| ③ 改后原文 | `            return new LayerReadout(draft.OverlayKey, "Rejected", "overlay_requires_pointable_source_and_version_wave37_alias", null);  // MUTATION M2b (temporary, wave37): text face of the overlay rejection reason`（＝`mutation-prep/M2b_StateLayering.cs` 整文件字节复制，行数不变） |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/StateAndLearning/StateLayering.cs | 2 +-`／` 1 file changed, 1 insertion(+), 1 deletion(-)`（存档 `out/m2b-apply.txt`；当轮 `status --short`＝1 行 ` M`） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave37-1028/out/trx-M2b" --logger trx`（`dotnet` 计数 3） |
| ⑥ TRX 文件名 | `out/trx-M2b/24782_PHYTHLINK_2026-10-28_23_15_15.trx` |
| ⑦ 红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.StateAndLearningPrototypeTests.An_overlay_without_a_pointable_source_and_version_is_refused`（逐字符＝§三.0 预期）；消息＝`Assert.Equal() Failure: Strings differ`（pos 44，Expected `"overlay_requires_pointable_source_and_version"`／Actual 带 `_wave37_alias` 后缀）；红点＝`StateAndLearningPrototypeTests.cs:line 95`＝`Assert.Equal("overlay_requires_pointable_source_and_version", nullSource.Reason)` 那句**拒因字面量断言**（＝预论证 `:95`，无映射差异；堆栈仅测试方法一帧、无产品异常帧——与 M2 异常侧相对，这一枚响的是断言面）。同格 `:94`（Expected "Rejected" outcome）先通过、`:96`／`:97` 同串断言在首断即抛后未执行。仪器读数（`out/m2b-count.txt`）：`total=26 executed=26 passed=25 countersFailed=1 faceA=1 faceB=1 selfCheck=OK reconcile=OK notExecutedOpenTags=0 allRedNamesInCriteria=True` |
| ⑧ 还原命令 | `python w37-apply.py revert M2b`（blob 字节写回） |
| ⑨ 还原后 `git status --short` 行数 | `0` |
| ⑩ SHA-256 | 突变态实测 `75fef21f3b237cf4be81faba456a22326c4fd7aedb7bf0ba1d21552a69605972` ＝工单值（match=True）；还原后 `af727a9c0435…` ＝基准（match=True）；判据 apply／revert 两侧均＝ `317d2171264f…`（＝blob） |
| ⑪ 当轮预检 | P82＝`VERDICT=BUILD_OK`（23:14:15，归档 `out/preflight-P82.txt`；整段原文：前台 ChatGPT pid=16348 `covers=False`、`free_memory_gb=11.2`） |

形状裁决＝实测恰 1 枚、格名逐字符相符、红点＝预论证 `:95` 的断言面、消息＝Strings differ ⇒ 与事前论证一致，本轮有效；本枚＝`:95` 那句断言第一次单独响（断言级子账的盘上形状，划账归守门人）。

### M3（摘闸·作者边界闸②）——预检 P83＝BUILD_OK

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `src/Pry.Core/Narrative/StateAndLearning/NarrativeAuthority.cs:36-38` |
| ② 改前原文 | `        // 闸②·作者边界不得由叙事改写。摘掉这一行 → 只有"叙事不得改作者边界"那格会红。` ／ `        if (attempt.ModifiesAuthorBoundary)` ／ `            return new AuthorityReadout(attempt.CandidateId, false, "author_boundary_is_not_narrator_mutable");` |
| ③ 改后原文 | `        // [MUTATION M3] 摘掉"作者边界不得由叙事改写"那道闸。`（＝`mutation-prep/M3_NarrativeAuthority.cs`，净 −3 行） |
| ④ `git diff --stat` | ` 1 file changed, 1 insertion(+), 3 deletions(-)`（存档 `out/m3-apply.txt`；当轮 `status --short`＝1 行 ` M` 该文件） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave37-1028/out/trx-M3" --logger trx`（`dotnet` 计数 4） |
| ⑥ TRX 文件名 | `out/trx-M3/24782_PHYTHLINK_2026-10-28_23_16_50.trx` |
| ⑦ 红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.StateAndLearningPrototypeTests.A_narrative_candidate_may_not_modify_the_author_boundary`（逐字符＝§三.0 预期）；消息＝`Assert.False() Failure`（Expected False／Actual True，`:142` 的 `Assert.False(readout.Allowed)`）；红点＝`StateAndLearningPrototypeTests.cs:line 142`（＝预论证 `:142`，无映射差异）。其余三面单点格与三面全给格未连带红（亲密闸在前、`c-2` 单点输入只此一格，与 §三.0R.2 复核一致）。仪器读数（`out/m3-count.txt`）：`total=26 executed=26 passed=25 countersFailed=1 faceA=1 faceB=1 selfCheck=OK reconcile=OK notExecutedOpenTags=0 allRedNamesInCriteria=True` |
| ⑧ 还原命令 | `python w37-apply.py revert M3`（blob 字节写回） |
| ⑨ 还原后 `git status --short` 行数 | `0` |
| ⑩ SHA-256 | 突变态实测 `74e2d50523a6f6ead6e58147818dab98184240d890db2d6d69d9edea62fc8859` ＝工单值（match=True）；还原后 `000f2fc8bfd5…` ＝基准（match=True）；判据 apply／revert 两侧均＝ `317d2171264f…`（＝blob） |
| ⑪ 当轮预检 | P83＝`VERDICT=BUILD_OK`（23:15:22，归档 `out/preflight-P83.txt`；整段原文：前台 ChatGPT pid=16348 `covers=False`、`free_memory_gb=11.9`） |

形状裁决＝实测恰 1 枚、格名与红形状逐字符相符 ⇒ 与事前论证一致，本轮有效。

### M4（摘闸·未投递闸②）——预检 P84＝BUILD_OK

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `src/Pry.Core/Narrative/StateAndLearning/DeliveryAttribution.cs:63-65` |
| ② 改前原文 | `        // 闸②·未投递的正文不得成为归因证据。摘掉这一行 → 只有"未投递不得归因"那格会红。` ／ `        if (!evidence.BodyDelivered)` ／ `            return NotAdmitted(evidence.BodyId, "body_never_delivered");` |
| ③ 改后原文 | `        // [MUTATION M4] 摘掉"未投递不得归因"那道闸。`（＝`mutation-prep/M4_DeliveryAttribution.cs`，净 −3 行） |
| ④ `git diff --stat` | ` 1 file changed, 1 insertion(+), 3 deletions(-)`（存档 `out/m4-apply.txt`；当轮 `status --short`＝1 行 ` M`） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave37-1028/out/trx-M4" --logger trx`（`dotnet` 计数 5） |
| ⑥ TRX 文件名 | `out/trx-M4/24782_PHYTHLINK_2026-10-28_23_17_22.trx` |
| ⑦ 红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.StateAndLearningPrototypeTests.An_undelivered_body_is_not_attribution_evidence`（逐字符＝§三.0 预期）；消息＝`Assert.False() Failure`（Expected False／Actual True，承重断言＝判据 `:181` 的 `Assert.False(readout.Admitted)`）；红点＝TRX 测试帧 `StateAndLearningPrototypeTests.cs:line 177`（`:177` 是多行调用 `attribution.Admit(new ExperienceCandidateEvidence(` 的起始行＝Release 序列点已知形状，波 36 §九.1 对 A4 `:195`／`:199` 同族已裁"归属由 testName＋消息定，行号只作辅证"）。theory 三行与入册／缺点击格未连带红（与 §三.0R.2 消费点复核一致）。仪器读数（`out/m4-count.txt`）：`total=26 executed=26 passed=25 countersFailed=1 faceA=1 faceB=1 selfCheck=OK reconcile=OK notExecutedOpenTags=0 allRedNamesInCriteria=True` |
| ⑧ 还原命令 | `python w37-apply.py revert M4`（blob 字节写回） |
| ⑨ 还原后 `git status --short` 行数 | `0` |
| ⑩ SHA-256 | 突变态实测 `00307f0a4c7df3b070bf5d1f23f4efba204378473a218b765003f44139153e40` ＝工单值（match=True）；还原后 `5858a75da3ed…` ＝基准（match=True）；判据 apply／revert 两侧均＝ `317d2171264f…`（＝blob） |
| ⑪ 当轮预检 | P84＝`VERDICT=BUILD_OK`（23:17:10，归档 `out/preflight-P84.txt`；整段原文：Godot pid=40976 `covers=False`、`free_memory_gb=11.7`） |

形状裁决＝实测恰 1 枚、格名与消息逐字符相符，红点行号落在多行调用起始行（预论证断言面 `:181`）＝已登记已知形状族 ⇒ 与事前论证一致，本轮有效。

### M5（翻布尔·`AttributionReadout` 第 5 枚实参 `UserConsentProven`）——预检 P85＝BUILD_OK

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `src/Pry.Core/Narrative/StateAndLearning/DeliveryAttribution.cs:88` |
| ② 改前原文 | `            false,`（`AttributePromise` 的 `return new AttributionReadout(` 第 5 枚位置实参＝`UserConsentProven`） |
| ③ 改后原文 | `            true,`（＝`mutation-prep/M5_DeliveryAttribution.cs` 整文件字节复制；`:89` 保持 `false);`） |
| ④ `git diff --stat` | ` 1 file changed, 1 insertion(+), 1 deletion(-)`（存档 `out/m5-apply.txt`；当轮 `status --short`＝1 行 ` M`） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave37-1028/out/trx-M5" --logger trx`（`dotnet` 计数 6） |
| ⑥ TRX 文件名 | `out/trx-M5/24782_PHYTHLINK_2026-10-28_23_18_26.trx` |
| ⑦ 红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.StateAndLearningPrototypeTests.A_promise_send_proposes_a_commitment_but_proves_neither_consent_nor_completion`（逐字符＝§三.0 预期）；消息＝`Assert.False() Failure`（Expected False／Actual True）；红点＝`StateAndLearningPrototypeTests.cs:line 244`（＝预论证 `:244`，无映射差异；`:245` 的 `TaskCompletedProven` 断言因首断即抛当轮未执行——那是 M5b 的方向）。仪器读数（`out/m5-count.txt`）：`total=26 executed=26 passed=25 countersFailed=1 faceA=1 faceB=1 selfCheck=OK reconcile=OK notExecutedOpenTags=0 allRedNamesInCriteria=True` |
| ⑧ 还原命令 | `python w37-apply.py revert M5`（blob 字节写回） |
| ⑨ 还原后 `git status --short` 行数 | `0` |
| ⑩ SHA-256 | 突变态实测 `870064705c4220a714793dcc5b8741f0fdcba4ddc060d554855c165fd1bed96c` ＝工单值（match=True）；还原后 `5858a75da3ed…` ＝基准（match=True）；判据 apply／revert 两侧均＝ `317d2171264f…`（＝blob） |
| ⑪ 当轮预检 | P85＝`VERDICT=BUILD_OK`（23:18:13，归档 `out/preflight-P85.txt`；整段原文：Godot pid=53448 `covers=False`、`free_memory_gb=11.3`） |

形状裁决＝实测恰 1 枚、格名／消息／红点逐字符相符 ⇒ 与事前论证一致，本轮有效。

### M5b（翻布尔·`AttributionReadout` 第 6 枚实参 `TaskCompletedProven`；＝断言级子账二）——预检 P86＝BUILD_OK——**本轮作废（编译期失败，未进测试阶段；工单判据：实测形状≠§三.0 预期 ⇒ 作废、红不署、不追第二轮）**

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `src/Pry.Core/Narrative/StateAndLearning/DeliveryAttribution.cs:89` |
| ② 改前原文 | `            false);`（`AttributePromise` 的 `return new AttributionReadout(` 第 6 枚位置实参＝`TaskCompletedProven`，同行带调用收尾 `);`） |
| ③ 改后原文 | `            true,`（＝`mutation-prep/M5b_DeliveryAttribution.cs` 整文件字节复制；**该 prep 文本此行不含收尾 `);`**——盘上事实见 §三.0R.1 事前登记的静态判读：`return new AttributionReadout(` 的括号在突变态不闭合，语法不完整） |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/StateAndLearning/DeliveryAttribution.cs | 2 +-`／` 1 file changed, 1 insertion(+), 1 deletion(-)`（存档 `out/m5b-apply.txt`；当轮 `status --short`＝1 行 ` M`） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave37-1028/out/trx-M5b" --logger trx`（`dotnet` 计数 7） |
| ⑥ TRX 文件名 | **无**——`dotnet test` 在编译期失败，未进入测试阶段，`--results-directory` 指向的目录未被创建；产物只有编译输出全文存档 `out/m5b-run.txt`（副本 `out/trx-M5b-voided-P86/m5b-voided-run.txt`，同目录 `README-voided.txt` 登记作废事实与命名） |
| ⑦ 红格全名＋消息要点＋红点行号 | **无红可登**。编译输出三行逐字：`DeliveryAttribution.cs(89,18): error CS1525: 表达式项“}”无效 […Pry.Core.csproj]`／`error CS1026: 应输入 ) […Pry.Core.csproj]`／`error CS1002: 应输入 ; […Pry.Core.csproj]`（路径为载体全路径，见 `out/m5b-run.txt`）。§三.0 预期（恰 1 枚 `A_promise_send_…` 红在 `:245`）**未被实测命中** ⇒ 按工单 §三 注与 §一 红线"实测与预期不符 ⇒ 那一轮作废、红不署、如实登记，不追第二轮"处置；不动预合成文本、不修 replacement、不起第二轮 |
| ⑧ 还原命令 | `python w37-apply.py revert M5b`（blob 字节写回；处置顺序盘上事实：当轮命令链因 dotnet 未创建结果目录致 `mv` 步失败，`&&` 链中断使还原未随链执行，随后以单独命令补跑还原——输出存档 `out/m5b-revert.txt`；`out/m5b-run.txt` 只含编译错误段、无测试阶段输出） |
| ⑨ 还原后 `git status --short` 行数 | `0` |
| ⑩ SHA-256 | 突变态实测 `676412b758cc121c316bd57953d676effb281775094378fdb227085f267c4b26` ＝工单 §三.0 M5b 值（match=True——**作废根因在工单预合成文本自身，不在本 lane 应用通道**：复制到盘上的字节与工单声明字节逐字节相同）；还原后 `5858a75da3ed…` ＝基准（match=True）；判据 apply／revert 两侧均＝ `317d2171264f…`（＝`51f149d` blob） |
| ⑪ 当轮预检 | P86＝`VERDICT=BUILD_OK`（23:19:16，归档 `out/preflight-P86.txt`；整段原文：前台 ChatGPT pid=16348 class=Chrome_WidgetWin_1 `covers=False`，`gpu_util=22% used=2201MB`，`free_memory_gb=12`） |

裁决＝本轮作废（编译期 CS1525/CS1026/CS1002，无 TRX、无红名、无署名对象）；`:245` 断言级子账**未结**，凭据面缺口（二十枚判据 SHA 读数中 M5b 的两枚仍齐——apply/revert 两侧判据字节均未动，作废发生在产品码侧）与处置归属由守门人裁。登记入 §六 对账：磁盘 `.trx` 总数因此＝13（9 枚有效定向轮×1＋F×4），与工单"全形状 14"差 1，差账即本作废轮产 0 枚，逐处点名不抹账。

### M6（翻判定·`IsLegacyWritebackAllowed() => false`→`=> true`）——预检 P87＝BUILD_OK

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `src/Pry.Core/Narrative/StateAndLearning/LegacyInheritance.cs:77` |
| ② 改前原文 | `    private static bool IsLegacyWritebackAllowed() => false;`（原文上行注释：`// 单向隔离的判定：新链不写回旧链那一层，恒为假。翻向这一行就是"第二条写入路径出现"的时刻。`） |
| ③ 改后原文 | `    private static bool IsLegacyWritebackAllowed() => true;`（＝`mutation-prep/M6_LegacyInheritance.cs` 整文件字节复制，行数不变） |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/StateAndLearning/LegacyInheritance.cs | 2 +-`／` 1 file changed, 1 insertion(+), 1 deletion(-)`（存档 `out/m6-apply.txt`；当轮 `status --short`＝1 行 ` M`） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave37-1028/out/trx-M6" --logger trx`（`dotnet` 计数 8） |
| ⑥ TRX 文件名 | `out/trx-M6/24782_PHYTHLINK_2026-10-28_23_39_55.trx` |
| ⑦ 红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.StateAndLearningPrototypeTests.There_is_no_second_write_path_back_to_the_legacy_layer`（逐字符＝§三.0 预期）；消息＝`Assert.Equal() Failure: Strings differ`（Expected `"Refused"`／Actual `"Written"`，pos 0，＝§三.0 预期文案）；红点＝测试帧 `StateAndLearningPrototypeTests.cs:line 408`。**帧行号与 §三.0 预期 `:411`（断言行）差 3 行，登记三处盘上事实**：(a) 突变态判据 `:411`＝`Assert.Equal("Refused", readout.Outcome);`，实测帧 `:408`＝同格多行调用 `var readout = inheritance.AttemptWriteback(new InheritedStateInput(` 起始行＝Release 序列点已知形状族（与 M2 `:93`、M4 `:177` 同族，工单 §六 预登记"行号只作辅证"）；(b) 波 33 那只手的原始产物 `../wave33-1028/out/trx-M6` 用本刀仪器复测＝帧同为 `:408`、消息逐字符同（`out/m6-wave33-compare.txt`）——本刀读到的与判据作者当年读到的是同一形状；(c) 三枚 `Import` 格与两枚扫描格未连带红（与 §三.0R.2 消费点复核一致：唯一读点 `:67`、唯一调用点 `:408`）。仪器读数（`out/m6-count.txt`）：`total=26 executed=26 passed=25 countersFailed=1 faceA=1 faceB=1 selfCheck=OK reconcile=OK notExecutedOpenTags=0 allRedNamesInCriteria=True` |
| ⑧ 还原命令 | `python w37-apply.py revert M6`（blob 字节写回） |
| ⑨ 还原后 `git status --short` 行数 | `0` |
| ⑩ SHA-256 | 突变态实测 `437bde00da315d2f923cbde83fb0004d1da62e5efe8b35ac45fe139302059a79` ＝工单值（match=True）；还原后 `6a1f50999e7c…` ＝ §〇.4 基准（match=True）；判据 apply／revert 两侧均＝ `317d2171264f…`（＝`51f149d` blob） |
| ⑪ 当轮预检 | P87＝`VERDICT=BUILD_OK`（归档文件 mtime 23:39:10，`out/preflight-P87.txt`；整段原文：前台 Godot pid=40976 class=Engine `covers=False`，`gpu_util=18% used=2333MB`，`free_memory_gb=13.1`） |

形状裁决＝实测恰 1 枚、格名与消息文案逐字符相符（Expected "Refused"／Actual "Written"＝§三.0 预期红形状），帧行号 `:408` 对预期 `:411` 的映射差异属已登记序列点族且与波 33 原始读数相同 ⇒ 与事前论证一致，本轮有效；署名与裁账归守门人。

### M7（摘闸·有效期入参缺失即明确失败；异常侧）——预检 P88＝BUILD_OK

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `src/Pry.Core/Narrative/StateAndLearning/ExperienceMaterial.cs:40-43` |
| ② 改前原文 | `        // 零数字门槛·有效期必须作为入参给：参数缺失即明确失败。摘掉这一行 →`／`        // 后续 validityWindow.Value 会在"缺有效期"那格的 null 输入上抛出，本格当场报警。`／`        if (validityWindow is null)`／`            return NotOffered(bodyId, "validity_window_required_as_input");`（盘上逐行核对＝干净树 `:40-43`，4 行整段） |
| ③ 改后原文 | `        // [MUTATION M7] 摘掉"有效期必须作为入参给"那道检查。`＋一行空行（＝`mutation-prep/M7_ExperienceMaterial.cs` 整文件字节复制，净 −3 行；被摘的是返回面闸，摘后 null 输入直走到原 `:49`＝漂移 `:46` 的 `validityWindow.Value`） |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/StateAndLearning/ExperienceMaterial.cs | 5 +----`／` 1 file changed, 1 insertion(+), 4 deletions(-)`（存档 `out/m7-apply.txt`；当轮 `status --short`＝1 行 ` M`） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave37-1028/out/trx-M7" --logger trx`（`dotnet` 计数 9） |
| ⑥ TRX 文件名 | `out/trx-M7/24782_PHYTHLINK_2026-10-28_23_50_33.trx` |
| ⑦ 红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.StateAndLearningPrototypeTests.Reading_experience_as_optional_material_without_a_validity_window_fails_loudly`（逐字符＝§三.0 预期）；消息＝`System.InvalidOperationException : Nullable object must have a value.`（＝预期异常侧，Nullable 取值）；红点＝测试帧 `StateAndLearningPrototypeTests.cs:line 256`（＝预论证 `:256`，逐字符命中）＋产品帧 `ExperienceMaterial.cs:line 46`＝预论证漂移值（原 `:49` 的 `validityWindow.Value`，净 −3 行漂移后 ＝`:46`，**与 §三.0 预期"漂到 `:46`"逐字符相符**）；堆栈另含 `System.Nullable\`1.get_Value()` 一帧。其余四枚 `ReadOptional` 调用格（判据 `:269`／`:283`／`:296`／`:297`，全传非 null 窗口）未连带红（与 §三.0R.2 复核一致）。仪器读数（`out/m7-count.txt`）：`total=26 executed=26 passed=25 countersFailed=1 faceA=1 faceB=1 selfCheck=OK reconcile=OK notExecutedOpenTags=0 allRedNamesInCriteria=True` |
| ⑧ 还原命令 | `python w37-apply.py revert M7`（blob 字节写回） |
| ⑨ 还原后 `git status --short` 行数 | `0` |
| ⑩ SHA-256 | 突变态实测 `2269c0a7e764e05898e6c03f104f178a514f753bcc812a5079381ab524a74564` ＝工单值（match=True）；还原后 `289fc89be087…` ＝ §〇.4 基准（match=True）；判据 apply／revert 两侧均＝ `317d2171264f…`（＝`51f149d` blob） |
| ⑪ 当轮预检 | P88＝`VERDICT=BUILD_OK`（归档文件 mtime 23:49:16，`out/preflight-P88.txt`；整段原文：前台 msedge pid=28260 class=Chrome_WidgetWin_1 `covers=False`，`gpu_util=17% used=2348MB`，`free_memory_gb=11`） |

形状裁决＝实测恰 1 枚、格名逐字符相符、异常侧双帧（测试 `:256`＋产品漂移 `:46`）与消息逐字符命中事前论证 ⇒ 本轮与论证一致；署名与裁账归守门人。

### M7b（文本面·有效期拒因字面量；＝断言级子账三）——预检 P89＝BUILD_OK

| 项 | 内容 |
|---|---|
| ① 文件:行（`51f149d` 侧） | `src/Pry.Core/Narrative/StateAndLearning/ExperienceMaterial.cs:43` |
| ② 改前原文 | `            return NotOffered(bodyId, "validity_window_required_as_input");`（干净树 `:43`，盘上逐行核对） |
| ③ 改后原文 | `            return NotOffered(bodyId, "validity_window_required_as_input_wave37_alias");  // MUTATION M7b (temporary, wave37): text face of the missing-window reason`（＝`mutation-prep/M7b_ExperienceMaterial.cs` 整文件字节复制，行数不变；闸本身保留，只改拒因字面量） |
| ④ `git diff --stat` | ` src/Pry.Core/Narrative/StateAndLearning/ExperienceMaterial.cs | 2 +-`／` 1 file changed, 1 insertion(+), 1 deletion(-)`（存档 `out/m7b-apply.txt`；当轮 `status --short`＝1 行 ` M`） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test tests/Pry.Core.Tests/Pry.Core.Tests.csproj -c Release --filter "FullyQualifiedName~StateAndLearningPrototypeTests" --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave37-1028/out/trx-M7b" --logger trx`（`dotnet` 计数 10） |
| ⑥ TRX 文件名 | `out/trx-M7b/24782_PHYTHLINK_2026-10-28_23_52_44.trx` |
| ⑦ 红格全名＋消息要点＋红点行号 | `Pry.Core.Tests.StateAndLearningPrototypeTests.Reading_experience_as_optional_material_without_a_validity_window_fails_loudly`（逐字符＝§三.0 预期）；消息＝`Assert.Equal() Failure: Strings differ`（pos 33，Expected `"validity_window_required_as_input"`／Actual 带 `_wave37_alias` 后缀；＝预论证文案面）；红点＝`StateAndLearningPrototypeTests.cs:line 259`＝`Assert.Equal("validity_window_required_as_input", readout.Reason);` 那句**拒因字面量断言**（＝预论证 `:259`，无映射差异；堆栈仅测试方法一帧、无产品异常帧——与 M7 异常侧相对，这一枚响的是断言面＝`:259` 第一次单独响）。同格 `:258`（`Assert.False(readout.Available)`，突变态闸保留仍返回 not-offered）先通过、`:260`（`Assert.Null(readout.OptionalMaterial)`）在首断即抛后未执行。仪器读数（`out/m7b-count.txt`）：`total=26 executed=26 passed=25 countersFailed=1 faceA=1 faceB=1 selfCheck=OK reconcile=OK notExecutedOpenTags=0 allRedNamesInCriteria=True` |
| ⑧ 还原命令 | `python w37-apply.py revert M7b`（blob 字节写回） |
| ⑨ 还原后 `git status --short` 行数 | `0` |
| ⑩ SHA-256 | 突变态实测 `3066dfc65a4c59ed2c38a010bbd344c869438f76b32bd4cb08e3387db796a49c` ＝工单值（match=True）；还原后 `289fc89be087…` ＝基准（match=True）；判据 apply／revert 两侧均＝ `317d2171264f…`（＝blob） |
| ⑪ 当轮预检 | P89＝`VERDICT=BUILD_OK`（归档文件 mtime 23:52:05，`out/preflight-P89.txt`；整段原文：前台 ChatGPT pid=16348 `covers=False`、`gpu_util=14% used=2262MB`、`free_memory_gb=12.5`） |

形状裁决＝实测恰 1 枚、格名逐字符相符、红点＝预论证 `:259` 的断言面、消息＝Strings differ ⇒ 与事前论证一致，本轮有效；本枚＝`:259` 那句断言第一次单独响（断言级子账的盘上形状，划账归守门人）。十轮到此跑满（M5b 一轮为跑满且作废）。

## 四 收尾全量 F（跑序末位：全部还原并核六枚 SHA 之后才起；预检 P90＝BUILD_OK）

全部还原核对＝`python w37-apply.py verify`（存档 `out/final-verify.txt`）：六枚文件（五枚产品＋一枚判据）载体工作副本逐字符对 `51f149d` blob 全配（`match=True`×6，`verify_mismatches=0`），起 F 之前成立。

| 项 | 内容 |
|---|---|
| ① 文件面 | 零写入轮：F 在十轮全部还原后的干净载体树上跑，本刀此节未再写任何产品/判据字节 |
| ②/③ 改前/改后 | 不适用（无突变）；替代凭据＝`out/final-verify.txt` 六枚 SHA 全配＋主树侧 0 行式子出场复跑（`out/exit-disk-readings.txt`） |
| ④ `git status --short`（载体） | `0` 行（verify 同场读数） |
| ⑤ 跑法整行（载体内，无 `--no-build`） | `dotnet test Pry.slnx -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave37-1028/out/trx-F" --logger trx`（`dotnet` 计数 11；退出码 0；全量语义＝本刀交付读数只来自这一次解决方案级全量） |
| ⑥ TRX 文件名（4 枚） | `out/trx-F/24782_PHYTHLINK_2026-10-28_23_59_38.trx`（Pry.Client.Tests）／`out/trx-F/24782_PHYTHLINK_2026-10-28_23_59_40.trx`（Pry.Api.Tests）／`out/trx-F/24782_PHYTHLINK_2026-10-28_23_59_40[1].trx`（Pry.Core.Tests）／`out/trx-F/24782_PHYTHLINK_2026-10-28_23_59_42.trx`（Pry.App.Tests） |
| ⑦ 计数读数 | 仪器三面对账（`out/f-count.txt`）：`files=4 total=1406 executed=1404 passed=1404 countersFailedSum=0 faceA=0 faceB=0 notExecutedOpenTags=2 allRedNamesInCriteria=True`；逐文件＝108/108、280/280、785/783（failed 0）、233/233。两枚 `NotExecuted` 开标签逐字符点名＝`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline` 与 `Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`（与波 33／36 的 F 面同名同形状；登记盘上事实：四份 `<Counters notExecuted>` 字段均为 `"0"`，缺口只以开标签点名，`total−executed=2` 自洽）。`total=1406` 为第三次同值复采读数——本刀不顶账，是否入账归守门人裁 |
| ⑧ 还原命令 | 不适用（F 无宿主写入） |
| ⑨ 还原后状态行数 | `0`（F 后载体 `status --short` 复看仍 0 行，`out/exit-disk-readings.txt`） |
| ⑩ SHA-256 | F 前后判据均＝ `317d2171264f…`（＝`51f149d` blob；主树侧出场复取同值，`out/exit-three-counts.txt` 附载体侧、主树侧两路各一）；六枚终态＝`out/final-verify.txt` 全配 |
| ⑪ 当轮预检 | P90＝`VERDICT=BUILD_OK`（归档文件 mtime 23:58:53，`out/preflight-P90.txt`；整段原文：前台 ChatGPT pid=16348 `covers=False`、`gpu_util=17% used=2364MB`、`free_memory_gb=12`） |

## 五 未验证／未结清单（逐一点名，本 lane 不代裁）

1. **M5b（`:245` 断言面）未拿到单独红**：该轮按工单判废（编译期 CS1525/CS1026/CS1002，无 TRX、无红名）。作废根因盘上证据＝`mutation-prep/M5b_DeliveryAttribution.cs` 的 `:89` 行 `            true,` 不带 `return new AttributionReadout(` 的收尾 `);`（§三.0R.1 事前登记＋`out/m5b-run.txt` 报错原文＋`out/trx-M5b-voided-P86/README-voided.txt`）；同时该盘上文本的 SHA ＝工单 §三.0 声明值（match=True，`out/m5b-apply.txt`）⇒ 缺陷在预合成文本自身、不在字节复制通道。重造文本与是否补排该轮归守门人；本 lane 未追第二轮。
2. **九枚有效定向轮的红是否署名"第二换手"、三对 M2／M2b、M5／M5b、M7／M7b 的格子口径不重复计数**——归守门人按工单 §八 预登记账面目裁；本 lane 只交形状事实（§三.M 各节⑦与裁决行）。
3. **帧行号映射差两处**（M2 产品帧 `:93` 对预期 `:95`；M6 测试帧 `:408` 对预期 `:411`）：均登记为 Release 序列点已知族且与波 33 原始读数逐字符相同（`out/m2-wave33-compare.txt`、`out/m6-wave33-compare.txt`）；"不影响署名"由工单 §六 预登记，最终裁量归守门人。
4. **M4 测试帧 `:177` 对断言面 `:181`** 同族差异，波 36 §九.1 先例在册，裁量同上归守门人。
5. **F 的 1406 读数是载体树（`51f149d` 线）的解决方案级形状**：主树线尾在本刀在飞期间由他人从 `fa6d24f` 挪至 `db064e6`（波 36 收口文档刀＋他刀），主树侧产品/判据字节与载体一致性以入场／出场两次 0 行式子为凭（§〇.3、`out/exit-disk-readings.txt`）；本刀读过的在途跟踪文件只有工单必读清单点名的 `docs/plans/PROJECT_PLAN.md`（只读零写），两枚 `M ` 暂存项与其余 `??` 未跟踪他人资产未读未改。
6. **§四 押账（工单）本刀一枚未动**：A／C 类重跑 0 次、`:367` 押账、阳性探针单独读数押账、接线欠账——均未触碰，也不在本刀凭据面。
7. **26 枚终账顶账**：本刀零顶账（三计数出场复看 23/1/3＝净新增 0，`out/exit-three-counts.txt`）；19/7/0 终账面是否挪动由守门人裁。

## 六 盘上自审计（逐项对账，差账逐处点名不抹）

- **`.trx` 计数**：工单全形状预期＝定向 10×1＋全量 1×4＝14。盘上实数＝**13**（`find out -name "*.trx" | wc -l`，清单见本节末）。差 1 枚的差账＝M5b 作废轮产 0（dotnet 编译期失败未创建结果目录；作废留档 `out/trx-M5b-voided-P86/` 内只有 `README-voided.txt`＋`m5b-voided-run.txt`，无 `.trx`、未删除未清空未重跑同名目录）。逐目录：trx-M1＝1、trx-M2＝1、trx-M2b＝1、trx-M3＝1、trx-M4＝1、trx-M5＝1、trx-M5b-voided-P86＝0、trx-M6＝1、trx-M7＝1、trx-M7b＝1、trx-F＝4。
- **`dotnet` 调用**：11/11 用满（定向 10 枚含作废的 M5b＝计数 1–10；F＝11）。无第二轮重试、无补跑、无一枚带 `--no-build`（各轮⑤整行可逐字符复核）；解决方案级构建 B1＝**0/1**（跳过判定三段登记于 §〇，每轮/全量由 dotnet test 自行增量重建）。
- **预检 VERDICT 分布**（当场 `grep -h VERDICT out/preflight-P8*.txt out/preflight-P90.txt | sort | uniq -c`）：`BUILD_OK`×**11**、HOLD×**0**；HOLD 重跑额度 16 动用 **0** 次。每次 `dotnet` 之前恰一枚在场 BUILD_OK 预检（P80–P90 与计数 1–11 一一对应，§三.P 台账）。
- **判据零写入凭据面**：二十枚读数（十轮 apply／revert 两侧各一）全部＝ `317d2171264f…`＝`51f149d` blob（各节⑩；出场复看主树／载体两路同值 `out/exit-three-counts.txt`＋`out/final-verify.txt`）。
- **三计数出场复看**：`[Fact]`＝23、`[Theory]`＝1、`[InlineData`＝3 ⇒ 26 枚格，与入场及波 33–36 逐位相同；净新增 0。
- **载体终态四行**（`out/exit-disk-readings.txt`，2026-10-29 00:01:06 当场取）：`rev-parse HEAD`＝`51f149d0b722…`（线尾未挪，本刀零提交）；`status --short`＝0 行；`log --oneline fd93ebe..HEAD`＝2 行（`1e02205`＋`51f149d`，与入场同）；主树侧 0 行式子以出场线尾 `db064e6…` 为右端复跑＝**0 行**。
- **主树终态**：本刀git 写操作执行记录＝**零**（无 commit/add/checkout/reset/stash/clean/rm/tag/push/merge/cherry-pick/apply——全程未调用，主树线尾移动系他刀在途）；两枚 `M ` 暂存项与全部 `??` 他人资产未读未改；主树唯一本刀新文件＝本报告 `?? docs/eval/reports/2026-10-28-hds009-wave37-m-second-hand-red.md`（未 `git add`、未提交）。
- **`.trx` 盘上清单**：`out/trx-M1/…23_07_27.trx`、`trx-M2/…23_11_39.trx`、`trx-M2b/…23_15_15.trx`、`trx-M3/…23_16_50.trx`、`trx-M4/…23_17_22.trx`、`trx-M5/…23_18_26.trx`、`trx-M6/…23_39_55.trx`、`trx-M7/…23_50_33.trx`、`trx-M7b/…23_52_44.trx`、`trx-F/`（4 枚，见 F 节⑥）。
- **报告自我核对（出闸前当场）**：本报告各节⑥引用的 TRX 文件名与运行日志`结果文件:`行及盘上文件名逐枚比对——M2b／M3／M4／M5 四节⑥曾误抄（`23_15_06`/`23_15_53`/`23_17_53`/`23_18_43`），已按盘上值改正为 `23_15_15`/`23_16_50`/`23_17_22`/`23_18_26`（改前误值在此留痕，不抹）；其余（M1／M2／M6／M7／M7b／F×4）逐字符配平。M7／M7b 两节②③⑦的判据／产品原文亦按干净树与 prep 文本当场逐行复核并就地修正过抄写偏差（盘上事实以 §三.M 各节现文与 `out/` 存档为准）。

## 七 交接（下一步归属）

- **守门人**：按工单 §八 预登记账面目裁 §三.M 九轮形状与红名归属、二十枚判据 SHA 凭据面是否足签七枚 M 格第二换手、三条断言级子账中 `:95`／`:259` 两枚的划账、`:245` 缺口的处置（重造 M5b 预合成文本需守门人亲手：现盘文本 `:89` 缺收尾 `);`，证据 `out/m5b-run.txt`、`gk37-prep.js` 的 `cut(blob(DeliveryAttribution.cs), 89, 89, '            true,')` 行）；F 的 1406 同值复采与 `notExecuted` 字段形状（§四⑦ 登记事实）一并裁。
- **本 lane 交付边界**：报告＝主树唯一新文件（未提交）；载体净写入 0 字节终态（`out/final-verify.txt`）；`dotnet` 预算 11/11 用尽，无任何后续定向轮在本 lane 剩余额度（0）。
- **后续任何刀若要补 `:245`**：出处与凭据面在本报告 §五.1 与 M5b 节，不在波 33 配方内（工单 §三.2 已声明追加面出处在守门人那只手）。

## 九 守门人划账（本刀唯一追加节；§〇–§八 一字未改，凭据见本节末的 numstat 面）

### 九.0 我的仪器与它的自证（口径⓪：先证明仪器读得对，再采信它的读数）

两把仪器都是我本刀自写，不复用 lane 的 `w37-count.py`／`w37-apply.py`，也不复用波 36 那把：

1. `.local-notes/wave37-1028/gk37-count.js`＝取数面。口径＝只数 `<UnitTestResult` **开标签**上的 `outcome="Failed"`（引号感知走到标签尾，自闭合与成对写法同一切法）；反向锚定面（face B）＝逐处找 `outcome="Failed"` 回溯最近的 `<` 判标签名（专抓 `<ResultSummary>` 这类不算用例的结点）；与同份 `<Counters failed>` 双向对账；红名先剥全限定前缀与参数表（`split('(')[0].split('.').pop()`）再回判据源码验"方法名＋`(`"真实存在。**起跑前先拿盘上九组已知读数自证＝`SELFPROOF AGREED=9/9`**（波 33 `trx-M1|M5|M7` 各恰 1 且红名逐字符、波 33 `trx-F`＝4 份 1406/1404/0、波 34 `trx-A1` 恰 2、波 35 `trx-A1|A3` 各恰 1、波 36 `trx-A4|trx-C2` 各恰 1、波 36 `trx-F` 同值含两条 `NotExecuted` 开标签点名）。
2. `.local-notes/wave37-1028/gk37-compare-hands.js`＝两双手同格对账面（见 §九.3）。

两处我自己仪器的缺陷，都按"仪器说读不出来先怀疑仪器"处置、判据与盘上 TRX 零字节改动：① 第一版 `countDir` 没把逐结点红汇进返回值 ⇒ `wave37` 模式在取 `res.reds[0]` 处抛 TypeError，崩点在它产出任何结论之前，补 `reds: parsed.flatMap(...)` 后重跑；② 抽栈帧的正则我一开始写成 `at (\S+\.cs):line`，**全部抽不到帧**（TRX 原文是 `… in C:\…\File.cs:line 47`，`at` 后面跟的是方法名不是文件名），改成 `([\w.\-]+\.cs):(?:line |：行 )(\d+)` 才有读数——口径升级＝抽帧不能锚 `at `，`at` 与 `.cs:line` 之间隔着方法名与 ` in <路径>`（这条进第三十八波）。

### 九.1 工单 §五 那七件事逐件判定（全部当场从盘上重取，右端不引用报告文字）

1. **十轮形状**：九枚有效轮**各恰 1 枚红**（我这边 `files=1 total=26 executed=26 failed=1 faceA=faceB=1 selfCheck reconcile` 九行齐），红名逐字符等于我出闸时写下的事前论证七＋二枚预期；没有一轮因"红多了两枚"作废。消息面逐枚等于预期红形状（M1 `Expected "Rejected"/Actual "Applied"`、M2 `NullReferenceException`、M2b／M7b `Strings differ`、M3／M4／M5 `Assert.False() Failure`、M6 `Expected "Refused"/Actual "Written"`、M7 `InvalidOperationException`＝Nullable 取值）。M5b 一轮作废（编译期 CS1525/CS1026/CS1002，`out/m5b-run.txt` 三枚码逐枚在场，测试阶段输出＝无）。
2. **二十枚判据 SHA 读数齐**：`out/{m1,m2,m2b,m3,m4,m5,m5b,m6,m7,m7b}-{apply,revert}.txt` 二十份存档逐份 grep 到基准值 `317d2171264f…`；反向 grep（同字段但不等于基准）**＝0 行**；另加三份终场存档（`final-verify`／`exit-three-counts`／`guard-probe`）同值。盘上现值当场四路各取一次＝主树工作副本／主树 `db064e6` 的 blob／载体工作副本／载体 `51f149d` 的 blob，四枚 SHA **逐字符相同**。签名资格面：判据字节的 last-touch 主树侧＝`e04e7e6`（波 33 内容，由我按路径落地）、载体侧＝`51f149d`＋`1e02205`（波 33），两只手都不是本刀这只手 ⇒ **签账资格第五次使用成动作**，且这次是"另一双手"这一格第一次真正结清而不是补档。
3. **载体终态**：`rev-parse HEAD`＝`51f149d0b722…`（线尾未挪，本刀零提交）、`status --short`＝**0 行**、`log --oneline fd93ebe..HEAD`＝**2 行**、六枚工作副本 SHA 逐字符等于 §〇.4 入场基准（五枚产品＋一枚判据，我这边六行全配）。主树侧那条 0 行式子以当场 `git rev-parse HEAD`＝`db064e6…` 为右端复跑＝**0 行** ⇒ 本刀对产品码与判据码净写入 0 字节，"归位"在其上是空转（与波 32 同形：跑满但没有新字节可落）。
4. **收尾全量**：4 份 TRX 先数满再逐份求和 `108＋280＋785＋233＝1406`／executed `108＋280＋783＋233＝1404`／failed 0／两条 `NotExecuted` 按开标签逐字符点名＝`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`（与波 28–36 同名同枚）。判定＝**1406 的第三次同值复采**（第一次波 34、第二次波 36），**在册基线不动**，本刀不顶账。四份 `<Counters notExecuted>` 字段仍为 `"0"`、缺口只以开标签点名且 `total−executed=2` 自洽＝同一形状第三次复现，登记为仪器口径而非缺陷。
5. **作废轮登记与 `.trx` 对账**：留档目录 `out/trx-M5b-voided-P86/` 内 2 个文件（`README-voided.txt`＋`m5b-voided-run.txt`）、`.trx` **0 枚**；`out/trx-M5b` 不存在（未创建、未清空、未重跑同名目录）；盘上 `.trx`＝**13**＝定向有效 9×1＋全量 1×4。与工单"满额形状 14"的差 1 枚去处逐处点名＝**M5b 产 0**，而这 0 是我那枚坏文本的必然结果，不是漏账也不是抹账。构建＝**0/1**（跳过判定三段当场复核成立：0 行式子 ✓、载体 `Pry.Core.dll` 在盘 ✓、六枚 SHA 未变 ✓），定向 **10/10** 用满（含作废那枚）、`dotnet` **11/11** 用满（`out/*-run.txt` 11 份，无一枚带 `--no-build`——全目录 grep `no-build` **0 命中**）；预检 **11 枚全 `BUILD_OK`**（P80–P90）、**HOLD 0 次／额度 16 未动用**，每一次 `dotnet` 之前恰一枚在场预检。
6. **净新增格数 0**：三计数当场复数 `[Fact]`23／`[Theory]`1／`[InlineData`3 ⇒ 展开 26 枚，与波 33／34／35／36 逐位相同；十枚预合成文本盘上 `sha256sum` 与工单 §三.0 表**逐字符全配**（含我那枚坏文本 `676412b7…`——**SHA 配上只证明字节复制通道干净，不证明文本正确**，这一点是本刀新边界，见 §九.4）。数字门槛新增 0 枚。
7. **写入通道的守卫实证在盘**：`out/guard-probe.txt` 原文给出 `CAPTURED PermissionError: REFUSED: … targets the criteria file …`＋前后判据 SHA 同值 `317d2171…` ⇒ 工单 §五.7 要的不是声称而是报错原文，这条**第一次由 lane 自己跑出可读的报错体**（波 36 那次我只核到"目标路径非判据"，这次是守卫主动拒绝）。

### 九.2 划账（终账，与断言级子账分列）

- **格子口径**：波 33 那 26 枚的终账由 **19／7／0 → 26／0／0**——26 枚每一枚同时具备"在改坏产品码之下响过的清洁红证"与"不是判据作者的那只手"。三对 M2／M2b、M5／M5b、M7／M7b 是**同一格的第二条因**，不是六枚格；本刀结的是 7 枚格的换手，**不写"26 枚全部结案"**（结案词还要等真实模型对照、生产接线、UI 读数那几面）。
- **断言级子账**（与格子口径分列的一条新线）：`:95`（M2b）与 `:259`（M7b）各拿到**第一次单独响**——两枚都是文本面（拒因字面量加 `_wave37_alias`），堆栈只有测试方法一帧、无产品异常帧，形状上就是"承重那句断言自己响"，**结清**；`:245`（M5b）**未结**，欠一枚语法完整的预合成文本＋一轮定向。加上波 32 那两条（HDS-007 的 `L1 :195`／`L3 :172`）与 §四.2 的 `:367`，这条子账现值＝**已结 2／未结 3**。
- **换手欠账**：HDS-009 第一片 **7 → 0**。旧欠 **75 格**（波 13 余 47＋波 20 余 28）照旧不减、与本刀无交集。
- **在册基线**：**1406／executed 1404／failed 0／跳过 2** 不动（第三次同值复采＝读数不是顶账）。旧值 1380／1357／1339 等全部过期。
- **顶账**：638 → **640**（本把新增两枚跟踪文件＝本报告＋第三十八波工单 `docs/dispatch-hds-wave38-1029.md`；数字＝两份落盘并暂存后当场 `git ls-files | wc -l` 实测，不是 638＋2 的预写）。链 638→640，`main` 只挪 ref。

### 九.3 两双手同场对账（本刀独有的正面事实，不是程序性合格）

同一枚格的红在两只手下的读数是否**同一份**，此前从没对过账。我用 `gk37-compare-hands.js` 把波 33（判据作者那只手）与波 37（另一双手）**都跑过的五枚轮**（M1／M2／M5／M6／M7）做了三面对逐字符比对（红名／消息全文／全部栈帧）：**五枚全 SAME**，其中 M2 的两帧 `StateLayering.cs:93 | StateAndLearningPrototypeTests.cs:90` 与 M7 的 `ExperienceMaterial.cs:46 | …:256` 连漂移后的产品帧都一字不差。意义＝"第二换手"结的不是手续而是**可重复性**：同一句断言在两只独立的手下给出同一份读数，这正是波 24 以来"换人"想换出来的东西。顺带这条比对把我出闸时写错的四处行号映射差判清（§九.4.2），因为比对基准是盘上原文而不是我的表。

### 九.4 我这两只手的错（照实登记，不涂改任何原文）

1. **M5b 那枚预合成文本是我写坏的**：干净树 `DeliveryAttribution.cs:89` 原文是 `            false);`（调用收尾的 `);` 在同一行），我的 `gk37-prep.js` 用 `cut(…, 89, 89, '            true,')` 整行替换 ⇒ 把 `);` 丢了，`return new AttributionReadout(` 的括号在突变态不闭合，编译期就炸。我的仪器当场把两份文本并排打印过：坏文本 `:88|false,` `:89|true,` `:90|}`，blob 同行 `:88|false,` `:89|false);`。**更深一层：我工单 §三.0 那格的"突变内容"写的是 `false,` → `true,`，说明我把 `:88` 的行内容当成 `:89` 看了一眼**——文本与表同源于同一个误读，所以 SHA 对账查不出来（SHA 只保证复制到盘上的字节＝我声明的字节）。后果＝`:245` 子账没结、11 枚 `dotnet` 里被这一轮占掉 1 枚。修法＝重造为 `            false);` → `            true);`，盘上实测新 SHA 后写进第三十八波工单（**不许预写 SHA**）。口径升级＝**预合成文本必须与它替换的那一行原文并排打印自证**（替换行的"改前原文"要从 blob 现取而不是从我记忆的表里抄），SHA 对账不能替代这一眼。
2. **工单"预期红点"那一列我混写了两种行号**：按"断言语句所在行"填了 M1 `:50`／M4 `:181`／M6 `:411`，而 TRX 记的是多行调用的起始行 `:47`／`:177`／`:408`；M2 的产品帧我写 `:95`、盘上 `:93`。四处全部落在波 35 §九.3／波 36 §九.1.1 已登记的 Release 序列点家族里，且 §九.3 证明两双手读到同一份帧 ⇒ **不影响署名**。但"事前论证"以后必须拆两列（断言面行／预期 TRX 帧行），否则每一把都要重新解释一遍映射差、每一把的 §五 都要挂一条"行号差待裁"。这条升级进第三十八波。
3. **lane 那一难是我造成的**：它在起跑前的静态判读里就把我那枚坏文本的语法缺陷读出来了（§三.0R.1），并且照实登记而不改口；但它手里只有两个合法选项——按红线跑一轮并作废，或不跑。它选择跑并作废、产物留档、不追第二轮、不动预合成文本一字。**裁定＝处置合规**（它无权重造文本，重造是我的活；它也没把 §三.0 的预期列改成实测值）。第三十八波补一条第三选项：事前静态判读命中"语法不完整"级缺陷 ⇒ 不起该轮、定向额度原样退回、报告登记缺陷面——省下的那一枚 `dotnet` 比一张作废读数值钱，因为它占用的是作者的窗口。

### 九.5 押下一把（第三十八波）与出闸形状

1. **补 `:245`**（唯一去处明确的一条）：重造 M5b 文本并盘上实测 SHA ⇒ 由另一双手跑那一轮，预期恰 1 枚红在 `:245` 且 `:244` 保持通过；本轮同样不许写判据字节。
2. **两枚扫描格的反面探针**（欠"把扫描函数本身弄坏"那一半）：同一枚被注入的阳性样本（波 36 的 C1／C2 通道）配一份"谓词被弄坏"的临时判据文本 ⇒ 预期**门不再咬**（该轮 0 枚红而样本仍在场），与波 36 那两枚"门会咬"合成两侧证明。**代价必须照写**：这一轮动判据字节，所以它**只结"门不是常绿"这一条机制面，不签任何第二换手**，逐字节还原后按 blob 核 SHA。
3. `:367`（同一行返回喂两句断言、无独立配方）与阳性探针的单独读数两条押账**照旧不动**；零门槛是作者定的，不为凑红造门槛，要拆输入形状属接线刀。
4. `PrototypeFiles` 写死清单的扫描盲区、两套并行命名债（`Hds006*` 产品类型名＋契约 §7.2 的 `CognitionKind`／`CognitionOrigin`）、波 28 那枚数值锁格、HDS-007／009 的生产接线、HDS-010 删除片——照旧挂着，**属接线刀不属证据刀**。
5. **HDS-009 第一片的证据账到此结清**（26／0／0）：之后的活按依赖性排序＝① 第一片那 26 枚接第一个真实调用方（产物是代码，仍按"红证先于落地、归位由守门人按路径做"），② 旧欠 75 格的换手刀，③ 真实模型对照（要作者一句"可以跑"）。**不写成"最短路径"或"尽快结案"**。

### 九.6 归位不解除的未验证面（照工单 §六 逐条列全，一句都不许混成"已完成"）

真实模型对照＝零读数（需作者一句"可以跑"）；**生产接线＝零调用方**（这 26 枚到今天没有任何一处产品代码走过它们，要接的 005 落库面与 004 生命周期一律没接，接线时机＝U-9 残句「切换那一步她要不要做什么」仍未拍）；旧数据真实继承＝零读取；UI 可见行为＝零读数；生产库与 `%LOCALAPPDATA%\PryCompanion`＝零接触；待标定参数权威值（关系／心情各档阈值、经验有效期、点击归因窗口）全部未定＝**特性不是缺陷**；并发与断电＝零读数；旧链删除片与"构建时不把旧链打进包"那道门＝**未开始**；`:367`、阳性探针单独读数、`:245`＝三条子账未结；Release 序列点把红点记到多行调用起始行的机理＝已知形状未证。**四句话照旧不许出现在任何页面**＝新链已接线／旧链可删／不进包已做到／她的状态已经迁到新链。

### 九.7 本刀对报告自身的形状凭据

追加面＝本 §九 一节，§〇–§八 一字未改；凭据＝`git diff --numstat` 对本报告的删除列恒 **0**、`git diff --check` 零输出（仪器输出段与 lane 原文里既有行尾空格照波 29 那条口径不删，删＝把读数改成不像读数的样子）；追加器自证 `after.startsWith(before)`；星号标记（连续两个星号）逐行成偶数（只判本刀插入行，不动既有奇数行）。
