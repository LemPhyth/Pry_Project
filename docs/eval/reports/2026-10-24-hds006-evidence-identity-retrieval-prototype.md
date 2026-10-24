# 2026-10-24｜HDS-006 第一片｜证据身份与统一检索的隔离原型（第二十八波）

- 工单＝`docs/dispatch-hds-wave28-20261024.md`（`HDS-WAVE28-1024`），任务编号＝`HDS-006`（登记处＝`docs/plans/PROJECT_PLAN.md` §4.2 第 77 行；架构面依据＝v4 第 325 行任务行、第 302 行 AUD-03 缺陷行、第 405 行"旧 RT-005/MEM-005/006 语义转 HDS-006"）。
- 执行方＝第二十八号冷启动 lane（隔离载体）。**状态词与结案词不由本 lane 写**；本文只算本刀自己的等式并写明出处。
- 交付面＝`src/Pry.Core/Memory/` 与 `tests/Pry.Core.Tests/` 的新建文件 ＋ 本报告。**零生产调用方；本片不是"记忆与删除全链"，也不含接线。**

## 〇 入场读数（当场取，不引用工单数字）

### 〇.1 必读六份（已逐份完整读完）

| 文档 | 取到的约束 |
|---|---|
| `docs/plans/README.md` | 权威工作规则 14 条；第 14 条把 `docs/agent-engineering-standards.md` 列为准绳 |
| `docs/plans/PROJECT_PLAN.md` | §4.2 第 77 行＝HDS-006 那行（"未认领／待认领；建议 Memory & Context；002；生产接线依赖 005"）；§4.3 第 93 行＝`RT-005`、`MEM-005/006` 的事实语义/管理/遗忘迁往 HDS-006，**检索 `.5` 下限不因重构放松**；§9 最新 entry＝10-24 15:0x 第二十七波收口（在册基线由那一把顶到 1339，归位改按路径落地） |
| `docs/agent-engineering-standards.md` | 全文 10 节。交付形态按 §10 四项（问题与根因／实际改动／验证结果／剩余问题）；§7"单元测试全部通过"不能单独作为完成证明；§3 禁止固定返回值／吞异常／只让单次复现成功 |
| `docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md` | 第 302 行 AUD-03（`SqliteMemoryRepository:433` 恢复状态的版本更新结果未检查，后续证据仍可覆盖→HDS-006）；第 325 行 HDS-006 交付格（现实/虚构/认知身份、统一检索、证据来源、摘要/向量/缓存失效、撤销 CAS、保留旧 `.5` 门）；第 405 行 RT-005/MEM-005/006 语义转 HDS-006。另读 §10.2（派生物是可重建导航，删除或失效必须同步更新）与 §10.2 后段（"撤销以状态版本和来源版本作条件恢复……撤销失败不得先永久弹掉唯一恢复记录"＝第 281 行） |
| `docs/narrative-storage-contract.md` | HDS-005／002 落库面契约；§2.2 第 5 点"证据引用随行"是本片要接的"证据身份"那一半；§7.2 的 `CognitionKind`／`CognitionOrigin` 是本片三身份的近邻形状（本片不复用其类型，只对齐语义，避免动他人字节） |
| `docs/legacy-chain-deprecation.md` | §4 四条要求（处置表／打包形状门／判据账重基线／数据继承），第 2／3／4 项照旧欠；**本刀不碰删除片**，原型存在的理由＝让"旧链可退役"那句话将来有地基——今天没有任何一页可以写它成立 |

### 〇.2 入场读数（逐条原样，`date` 与 git 均当场取）

```
$ date
Sat Oct 24 15:03:17     2026

$ git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD
d72e38e172eaac7a85dac2c09c1d471864fb06f6

$ git status --short --branch
## codex/ui-main-window-redesign
M  docs/dispatch-tip1020-02-memory-group-judgment.md
M  tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs
?? .qoder/
?? docs/character-learning-safety.patch
?? docs/research/HDS_INTERLUDE.md
?? docs/research/HDS_QQ_RESEARCH_INDEX.md
?? docs/research/QQ_BRIDGE.md

$ git log --oneline -3
d72e38e docs(plans,wave28): 预置第二十八波工单……
2d90650 docs(plan,sprint,ticket,eval,wave27): 收第二十七波续跑刀……
e7eb724 feat(HDS-003,HDS-010,wave25归位): 把第二十五波载体 24d1b6f 的三枚字节按路径落进线尾……

$ git worktree list
（74 行，完整原样归档＝.local-notes/wave28-1024/out/entry-readings.txt）
```

- 主树线尾＝`d72e38e`（＝工单自身那枚纯文档提交）；工单 §〇.3 指定的基点＝`2d90650`。
- 他人资产核对＝上面 2 枚 `M ` 暂存项 ＋ 5 项 `??`，本刀**不读内容、不改、不提交、不 `stash`／`clean`／`rm`**。
- 主工作树零 `git` 写操作：本刀只建过 1 个 worktree（`git worktree add --detach`，只新增不动任何 ref）。
- **入场后主树线尾由守门人另推了一枚（非本刀作为）**：第 15:2x 次读数核对时 `git -C C:/Users/24782/Desktop/Pry_Project rev-parse HEAD` 已＝`948935f0ee5e5d03cec54b946db2043c7731fab8`（在其上多出 `948935f docs(plan,eval,wave28)…` 一枚纯文档提交），`git merge-base --is-ancestor 2d90650 HEAD` ＝ YES。本刀的还原基准值一律按 §三.0 末段取自**载体 HEAD `f6589d5` 的 blob**，与主树线尾无关；主树那 2 枚 `M ` 与全部 `??` 行数未因本刀变动（现值＝8 行，含本刀那枚报告）。

### 〇.3 载体自证三件事

```
$ git -C .local-notes/qa-wave28-1024/carrier rev-parse HEAD
2d90650642a9240366882903c85586a6a3efcf55

$ git -C .local-notes/qa-wave28-1024/carrier status --short | wc -l
0

$ sha256sum src/Pry.Core/Narrative/Storage/NarrativeSqliteStore.cs   # 在载体里
6d1618f7c73501924a99591fb68b2810ae07307098b28ae978d98c37fcf2c099
```

### 〇.4 在册基线起点（当场从波 27 报告 §三.2 读，不从工单抄）

- 出处＝`docs/eval/reports/2026-10-24-hds003-010-red-proof-run2-and-final-gate.md` 第 76–82 行那四份 TRX 的 `<Counters>` 求和行原文：
  `Core 718/716/716/0`、`Client 108/108/108/0`、`Api 280/280/280/0`、`App 233/233/233/0` ⇒ **total 1339／executed 1337／passed 1337／failed 0／跳过 2**。
- 起点读数＝**1339／executed 1337／failed 0／跳过 2**（分项 Client 108／App 233／Api 280／Core 718，Core executed 716）。
- 本刀新格数 N＝由我自己的新判据文件逐文件当场数（见 §二 顶账），出口读数＝起点＋N，右端必须由我收尾那四份原始 TRX 当场求和取出。
- 两条跳过（在册口径，本刀不新增跳过）按 `outcome="NotExecuted"` 点名＝`Configured_llama_server_requires_the_child_environment_key`／`Configured_sense_voice_model_can_run_offline`；`notExecuted` 计数器恒 0，不看。
- **出口读数已由我收尾那四份原始 TRX 当场求和取出**（§三.1 F 段，先 `ls *.trx` 数出 4 枚再读 `<Counters>`）＝**1357／executed 1355／passed 1355／failed 0／跳过 2**，Core 分项 718→**736**；等式＝1339＋18。是否由这一把顶在册基线＝守门人的权利，本刀只报这一条等式与它的取数出处。

### 〇.5 焦点预检台账（每一次 `dotnet` 前必须一枚，整段归档）

| 轮次 | 预检文件 | VERDICT | free_memory_gb | 紧随其后的 `dotnet` |
|---|---|---|---|---|
| P1 → B1 | `.local-notes/wave28-1024/out/preflight-P1.txt` | BUILD_OK | 9.9 | `dotnet build Pry.slnx -c Release`（解决方案级，15.22 秒，0 错误 2 警告） |
| P2 → G0 | `.local-notes/wave28-1024/out/preflight-P2.txt` | BUILD_OK | 8.9 | `dotnet test tests/Pry.Core.Tests -c Release …trx-G0`（绿基线定向轮） |
| P3 → R1 | `out/preflight-P3.txt` | BUILD_OK | 9.3 | `dotnet test tests/Pry.Core.Tests -c Release …trx-R1`（身份闸突变） |
| P4 → R2 | `out/preflight-P4.txt` | BUILD_OK | 9.2 | `…trx-R2`（来源缺失静默丢弃突变） |
| P5 → R3 | `out/preflight-P5.txt` | BUILD_OK | 9.1 | `…trx-R3`（截断恒不成立突变） |
| P6 → R4 | `out/preflight-P6.txt` | BUILD_OK | 9 | `…trx-R4`（派生物 Kind 条件去掉突变） |
| P7 → R5 | `out/preflight-P7.txt` | BUILD_OK | 9.4 | `…trx-R5`（撤销来源版本闸失效突变） |
| P8 → R6 | `out/preflight-P8.txt` | BUILD_OK | 9.3 | `…trx-R6`（写回状态版本闸失效突变＝AUD-03 原形状） |
| P9 → F | `out/preflight-P9.txt` | BUILD_OK | 9.3 | `dotnet test Pry.slnx -c Release …trx-FULL`（收尾全量，未用 `--no-build`，理由见 §三.1 F 段） |

- P1 整段（15:2x 当场原样，归档＝`out/preflight-P1.txt`）：
  `FOREGROUND: msedge pid=5424 class=Chrome_WidgetWin_1 style=0x17CF0000 rect=-1928,96,1936x1048 screen=1920x1080 covers=True borderless=False title=Lecture 12 Geometry 3_????_bilibili ??? 4 ??? - `
  `UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=15532, TextInputHost pid=21228, explorer pid=13364`
  `gpu_util=24% used=1722MB` ／ `free_memory_gb=9.9` ／ `VERDICT=BUILD_OK`
- P2 整段（归档＝`out/preflight-P2.txt`）：
  `FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=416,41,1728x1309 screen=2560x1440 covers=False borderless=False title=?????`
  `UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=15532, TextInputHost pid=21228, explorer pid=13364`
  `gpu_util=8% used=1656MB` ／ `free_memory_gb=8.9` ／ `VERDICT=BUILD_OK`
- 两次预检之间无任何一轮作废：`free_memory_gb` 8.9／9.9 均属高位，读数无 SQLite `disk I/O`／`out of memory` 形态，本刀不动任何进程。
- 本刀预检累计次数上限＝8 次连续 `HOLD` 即停手交回零 `dotnet` 交付；相邻两次不短于 5 分钟。

### 〇.6 既有 `.5` 检索地板的唯一落点（当场实读，本刀不动它）

- `src/Pry.Core/Memory/MemoryQueryService.cs:22`：`… .Where(x => x.Status == "active" && x.Confidence >= .5 && x.ScopeKey == query.Scope.Key)` —— 这是盘上唯一的检索地板，它是**行内字面量而不是命名常量**。
- 因此工单 §一.3 那句"如果新链今天还没有那把地板，就把'没有地板'这件事登记成一条明写的事实与一条格"是本刀的适用分支：**本原型不设地板、不复制 `.5`、不新增任何数字门槛**，"没有地板"由判据格 `The_prototype_applies_no_retrieval_floor_the_existing_confidence_gate_would_have_applied` 钉成事实。§六.2 的在册零改动清单本刀一字未碰。

### 〇.7 AUD-03 锚点复算（v4:302 那句话在盘上的实形）

- `src/Pry.Core/Memory/SqliteMemoryRepository.cs:426` `RestoreDetachedMemoriesAsync` ⇒ 第 433 行起那条 `UPDATE memories … WHERE id=$id AND scope_key=$s AND state_version=$expected` 的 `await Execute(...)` **返回值当场不核**；紧随其后第 444–450 行仍无条件 `DELETE FROM memory_evidence` ＋ `INSERT` 证据行 ⇒ "恢复状态的版本更新结果未检查，后续证据仍可覆盖"逐字成立，锚点未漂移。
- 同文件第 397–402 行（脱链方向）则是 `if (await Execute(...) == 1) rewritten.Add(...)` ＝已核结果的形状。**本刀的新链形状照 :397 那一侧做，不照 :433 这一侧做。**

### 〇.8 本刀预算与实际用量（用满即停）

| 面 | 上限 | 已用 | 记录 |
|---|---|---|---|
| 解决方案级构建 | ≤1 | **1** | B1＝`dotnet build Pry.slnx -c Release`（P1 BUILD_OK 窗口内），0 错误／2 警告，警告全部落在本刀新建的那枚判据文件；额度用完后本刀不再做任何解决方案级构建（收尾轮内那一次增量重建不是构建调用，理由见 §三.1 F 段） |
| 解决方案级全量 | ≤1 | **1** | F＝`dotnet test Pry.slnx -c Release …trx-FULL`，4 份 TRX 先数后读＝1357／1355／1355／0，跳过 2 |
| 定向突变轮 | ≤8 | **7** | G0（绿基线，非突变）＋ R1–R6（六枚单放突变，每枚独立结果目录）；**留 1 枚未用**＝六枚红集与 §三.0 事前论证逐字相符，无需复取。突变轮一律未用 `--no-build` |

## 一 这一片造了什么（工单 §一 五条逐条对形）

载体＝`.local-notes/qa-wave28-1024/carrier`（detached @ 基点 `2d90650`），载体提交＝**`f6589d5aeed5911c011a3a0c9c97a3fa47f2e745`**（5 枚新文件）＋**`612423b76d03df9f3130d1ba536e88bc75b79f5e`**（线尾，只改本刀那枚判据文件的 2 行以清掉 B1 的两枚 xUnit 分析器警告；产品文件一行未动）。
文件面＝**5 枚新建，0 枚既有文件修改**（`git diff --stat 2d90650..HEAD -- src tests scripts Pry.slnx Directory.Build.props` ＝ `5 files changed, 860 insertions(+)`，无 deletions；`git diff --check` 干净；载体 `git status --short`＝0 行）。
> 注：B1 的警告修复只碰我自己的新判据文件，`sha256sum` 复验三枚突变目标产品文件的值与 §三.0 那三枚"还原后应为"逐字相同（`4cd91256…`／`5d2ce928…`／`c5e5892d…`），所以 §三.0 末段按载体 HEAD blob 取的那三个还原基准值仍成立——只是该 HEAD 现为 `612423b`。

| 新文件 | 行数 | 管哪一条 | 关键形状 |
|---|---|---|---|
| `src/Pry.Core/Memory/Hds006EvidenceIdentity.cs` | 113 | §一.1／§一.2 | `Hds006EvidenceKind{Real,Fiction,Cognitive}`；`Hds006SourceLedger` 把来源问账做成三态读数（`Live`／`Invalidated`／`Missing`），Missing 的固定原因＝`source_never_registered`，绝不返回 null；`Invalidate` **每次调用都让来源账前进一步**（撤销的条件之一） |
| `src/Pry.Core/Memory/Hds006UnifiedMemoryRetrieval.cs` | 108 | §一.1／§一.2／§一.3 | 一个入口 `Retrieve(Hds006RetrievalRequest)`；排序规则＝`RecordedSequence` 新→旧、同序号按 `EvidenceId` 小→大；每条没返回的行带 `Hds006ExclusionReason{ContentMismatch,IdentityMismatch,SourceMissing,SourceInvalidated,BeyondLimit}` 之一，被截的那几条带它们各自的名次 |
| `src/Pry.Core/Memory/Hds006DerivedArtifactLedger.cs` | 132 | §一.4 | 摘要／向量／缓存三处各占一格，失效按 `(Kind, EvidenceId)` 双条件精确命中；可观察面有三处：正文被红action 成固定哨兵 `<redacted>`、读数点名原因、观察日志留下一条命名到具体派生物的记录 |
| `src/Pry.Core/Memory/Hds006EvidenceStore.cs` | 155 | §一.5 | `TryRewrite`／`TryDetach` 的结果**当场核**（照 `SqliteMemoryRepository.cs:397` 那一侧，不照 `:433` 那一侧）；`TryRestore` 以状态版本＋来源版本双条件恢复，任一不成立 ⇒ 返回 `StateVersionMismatch`／`SourceVersionMismatch`／`RowGone` 且**一个字段都不写**；恢复记录只在成功路径弹掉（v4:281 后半句） |
| `tests/Pry.Core.Tests/Hds006EvidenceIdentityRetrievalPrototypeTests.cs` | 352 | §二 | 18 枚节点（见 §二 顶账） |

零生产调用方（当场 grep，非断言）：

```
$ grep -rln "Hds006" src/            # 载体内，只列出产品侧自己的 4 枚新文件
$ grep -rln "Hds006" src/Pry.Api src/Pry.App src/Pry.Client src/Pry.Contracts
（0 命中）
$ grep -rln "Hds006" tests/          # 只有本刀那枚新判据文件
```

本片是"证据身份＋统一检索"的形状活：**没有落表、没有接 005 的落库面、没有端点、没有界面**。

## 二 判据与顶账

当场逐标记数（`tests/Pry.Core.Tests/Hds006EvidenceIdentityRetrievalPrototypeTests.cs`，注释里刻意不写方括号属性名，数出来的就是真属性行）：

```
$ grep -c '^    \[Fact\]'                 # 12
$ grep -c '^    \[Theory\]'               # 2
$ grep -c '^    \[InlineData'             # 6
$ grep -c 'public void'                   # 14（12 枚 Fact 方法 ＋ 2 枚 Theory 方法）
```

- 线尾 `612423b`（清警告那 2 行改动之后）**复数一次**：12／2／6／14 与上表逐字相同，属性行与 `InlineData` 行都没有增减 ⇒ **N＝18 不变**，等式不受清警告动作影响。

| 归属 | 形状 | 节点数 |
|---|---|---|
| §一.1 | 1 枚 Fact（三行不同证据）＋ 1 枚 Theory 展开 3 条区分性探针（同一正文、同一来源，只换身份，每条钉自己的绝对期望标识并数另一侧被排除的条数） | 1 ＋ 3 ＝ 4 |
| §一.2 | 2 枚 Fact（来源缺失带明确原因；来源失效带明确原因，两者不共用沉默出口） | 2 |
| §一.3 | 3 枚 Fact（排序逐字点名含一次同序号并列；截断逐条点名名次；"本原型没有 `.5` 地板"这条明写事实） | 3 |
| §一.4 | 1 枚 Theory 展开 3 条探针（分别打掉摘要／向量／缓存，另两处的正文必须仍读得到）＋ 2 枚 Fact（红action＋原因点名、观察日志恰一条） | 3 ＋ 2 ＝ 5 |
| §一.5 | 4 枚 Fact（状态版本拒且零写回；来源版本拒且零写回；撤销成功后旧条件写不进；被拒的撤销不弹掉恢复记录） | 4 |
| **合计** | 12 Fact ＋ 2 Theory×3 | **N＝18** |

- 一次用例里的多枚断言**没有**被记成多格（10-20 犯过一次）：上面的 18 是按属性行与 `InlineData` 行数逐行数的，不是按断言数。
- 出口读数等式＝起点 **1339**（§〇.4 从波 27 报告 §三.2 当场读）＋ **18** ＝ **1357**，唯一去处＝Core 718 → 736；右端由我收尾那四份原始 TRX 当场求和取出，登记在 §三 最后一轮。
- 常绿形状自查：18 枚里没有"对空集合判不含某元素"那种结构上红不了的格。§一.3 的 `Retrieval_orders_eligible_rows_...` 与 `Truncation_reports_...` 断的是非空集合与逐字顺序串；§一.2 那枚 Missing 格除了判 Hits 为空，还判 `Assert.Single(result.Exclusions)` ＋原因文本＋读数三件，不是只判"不含"。**§一.3 那枚 `The_prototype_applies_no_retrieval_floor_...` 判的是"这一行仍然被返回"（存在性断言）**，它钉的是"没有地板"这条事实，加地板就会红；本刀不取它的红证（见 §三.0 末段）。

## 三 反证轮次

### 三.0 事前论证（工单 §三：恰 1 枚红，除非事前点名"这一枚必然连带响哪几枚、为什么"）

六枚突变全部只改我自己新建的产品文件，一次一枚一枚轮；突变文本先在临时副本上合成并 `sha256sum` 记号（第二十七波前置），副本留在 `.local-notes/wave28-1024/probe/`：

| 轮 | 文件:行 | 改法 | 副本 SHA-256（预合值） | 事前论证的期望红集 |
|---|---|---|---|---|
| R1 | `Hds006UnifiedMemoryRetrieval.cs:71` | `row.Identity != request.Identity` → `row.Identity != row.Identity`（身份闸失效） | `fa8719e5…b3073` | **必连带 3 枚**＝`Retrieving_one_identity_never_carries_another` 的三条 `InlineData`（Real／Fiction／Cognitive）。理由：三行正文与来源完全相同、只有身份不同，闸一失效每次检索都会拿到 3 条命中，`Assert.Single(result.Hits)` 在三条探针上各响一次；其余格的数据要么是单身份、要么根本不过检索口 |
| R2 | `Hds006UnifiedMemoryRetrieval.cs:80-81` | 删掉 `exclusions.Add(... SourceMissing ...)` 两行（缺失变成静默丢弃） | `ce0fb3c6…c64ee` | **恰 1 枚**＝`Evidence_whose_source_was_never_registered_is_reported_as_source_missing_not_dropped`。只有那枚夹具用未登记来源；`continue` 保留所以命中仍为空，红的是"点名原因"那一半 |
| R3 | `Hds006UnifiedMemoryRetrieval.cs:97` | `rank > request.ResultLimit` → `rank > rank`（截断永不发生） | `511cce9c…e1a66e` | **恰 1 枚**＝`Truncation_reports_every_cut_row_with_the_rank_it_would_have_had`。排序那枚的 `ResultLimit=5` 大于其 4 条合格行，`Assert.Empty(result.Exclusions)` 不受影响；三条身份探针的 `ResultLimit=5` 只有 1 条合格行 |
| R4 | `Hds006DerivedArtifactLedger.cs:86` | `_entries.Where(x => x.EvidenceId == key.EvidenceId && x.Kind == key.Kind` 去掉 `&& x.Kind == key.Kind`（一处失效波及三处） | `9912cf11…4b0580` | **必连带 4 枚**＝`Invalidating_one_derived_artifact_leaves_the_other_two_live_and_readable` 三条 `InlineData` ＋ `Invalidation_appends_exactly_one_observable_entry_naming_the_artifact`。理由：去掉 Kind 后同一证据的三处派生物一起被失效，另两处不再是 Live，观察日志也从 1 条变成 3 条；`Derived_invalidation_redacts_the_payload_...` 只判被点名的那一处，仍绿 |
| R5 | `Hds006EvidenceStore.cs:129` | `!= record.ExpectedSourceVersion` → `!= ledger.Readout(record.SourceId).SourceVersion`（来源版本条件恒不响） | `cc77b4b6…de11c0` | **恰 1 枚**＝`Undo_whose_source_line_advanced_after_the_detach_is_refused_by_the_source_version`。状态版本闸在它之前且未被改；`Undo_against_a_row_that_moved_...` 与 `A_refused_undo_keeps_...` 都在第一道闸就返回，`After_a_successful_undo_...` 两处版本本来都对得上 |
| R6 | `Hds006EvidenceStore.cs:91` | `row.StateVersion != expectedStateVersion` → `row.StateVersion != row.StateVersion`（后续写不看版本＝AUD-03 原形状） | `103b3f98…48c2c70` | **恰 1 枚**＝`After_a_successful_undo_a_stale_write_cannot_overwrite_the_restored_content`。`A_refused_undo_keeps_the_recovery_record_available` 里那次改写带的期望版本与行相同，改写照样成立 ⇒ 撤销仍被状态版本闸拒，该格不串味 |

- 不在本轮取红的格（照实登记、押下一把，不为了让它响去改判据）：`The_prototype_applies_no_retrieval_floor_...`（要让它红只能往产品码里**新增**一个数字门槛，工单 §六.2 禁止，本刀不作弊）、`Invalidated_source_is_named_...`（与 R2 同分支族，取红会撞同一枚闸的另一种改法，本刀定向额度优先给五条覆盖）、`Derived_invalidation_redacts_...`（R4 已证该文件的命中条件会响）。除这三枚之外还有四枚未取红（`Same_sentence_under_the_three_identities_…`、`Retrieval_orders_eligible_rows_…`、`Undo_against_a_row_that_moved_…`、`A_refused_undo_keeps_…`），连同理由逐枚登记在 §五 第 9 条——总数口径以 §三.2 末段那行"11 枚有红／7 枚无红"为准。
- 额度：定向 ≤8，本刀计划 6 枚单放突变 ＋ 1 枚突变前的绿基线轮，**留 1 枚复取**；解决方案级构建 ≤1、全量 ≤1 各用 1 次。
- 每轮跑法固定写法＝`dotnet test <工程或解决方案> -c Release [--no-build 仅收尾全量] --results-directory "<绝对路径>" --logger trx`；每轮独立结果目录；跑完先 `ls *.trx` 数枚数再读 `<Counters>`。
- ⑩ 的"还原后应为"值按第二十六波更正品径＝**该文件在载体 HEAD 的 blob**，当场 `git show` 取（下值同时成立于 `f6589d5` 与线尾 `612423b`，因为第二枚提交只改判据文件；每轮还原后我仍按 `git show 612423b:<路径>` 现取现比）：
  `Hds006UnifiedMemoryRetrieval.cs`＝`4cd91256ea8f3c5b55d23fe550fcad3d5bc2f870d4d0f3089ca9ead32368104d`
  `Hds006DerivedArtifactLedger.cs`＝`5d2ce9281acfcac636d9b718eba782962dc393fb178325ee7906e2627ae2fde5`
  `Hds006EvidenceStore.cs`＝`c5e5892d7f239d98b61389f9d70576354ad76e7e430f1c5ae09fc454269659b4`

### 三.1 轮次记录（逐轮追加）

#### B1｜解决方案级构建（非突变轮；预检＝P1 BUILD_OK）

- ⑤ 跑法整行：`dotnet build Pry.slnx -c Release`（工作目录＝载体 `C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave28-1024/carrier`）
- 归档＝`C:/Users/24782/Desktop/Pry_Project/.local-notes/wave28-1024/out/build-B1.txt`
- 结果＝`已成功生成。 2 个警告 0 个错误`，用时 `00:00:15.22`。
- 两枚警告逐字（**都在本刀新建的那枚判据文件里，产品文件 0 警告，既有文件 0 警告**）：
  - `tests/Pry.Core.Tests/Hds006EvidenceIdentityRetrievalPrototypeTests.cs(59,9): warning xUnit2029: Do not use Assert.Empty to check if a value does not exist in a collection. Use Assert.DoesNotContain instead.`
  - `…(233,21): warning xUnit2031: Do not use a Where clause to filter before calling Assert.Single. Use the overload of Assert.Single that accepts a filtering function.`
- 处置＝在**我自己的新文件**里换写法清掉两枚警告（`Assert.DoesNotContain(expectedId, result.Exclusions.Select(x => x.EvidenceId))`／`Assert.Single(ledger.Observations, x => x.State == Hds006ArtifactState.Invalidated)`），断言语义不变、期望字面量一个没动、`[InlineData]` 一枚没改；载体第二枚提交＝`612423b`，产品三文件 SHA 复验与 §三.0 基准逐字相同。既有文件零字节改动：`git diff --stat 2d90650..HEAD -- src tests scripts Pry.slnx Directory.Build.props` 仍＝`5 files changed, 860 insertions(+)`。

#### G0｜绿基线定向轮（非突变轮；预检＝P2 BUILD_OK）

- ⑤ 跑法整行：`dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave28-1024/out/trx-G0" --logger trx`（未用 `--no-build`）
- ⑥ TRX：先数枚数＝`ls *.trx | wc -l` ＝ **1**，文件名＝`24782_PHYTHLINK_2026-10-24_15_26_35.trx`；再读 `<Counters>`：
  `<Counters total="736" executed="734" passed="734" failed="0" error="0" … notExecuted="0" />`
  控制台行＝`已通过! - 失败: 0，通过: 734，已跳过: 2，总计: 736，持续时间: 31 s - Pry.Core.Tests.dll (net10.0)`
- 顶账：`736 = 718（§〇.4 起点 Core 分项）+ 18（本刀新格）`；跳过 2 枚按 `outcome="NotExecuted"` 点名＝`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`／`CoreTests.Configured_sense_voice_model_can_run_offline`（在册那两枚，本刀未新增跳过）。`notExecuted` 计数器＝0，按口径不看。
- 本刀 18 枚节点在 TRX 里逐字点名到名（12 枚 Fact ＋ `Retrieving_one_identity_never_carries_another`×3 条 `asked/expectedId` ＋ `Invalidating_one_derived_artifact_leaves_the_other_two_live_and_readable`×3 条 `struck`）＝**18/18 全绿**，突变轮的红集才有底子。
- ⑩ 突变目标三文件当轮 SHA（＝载体 HEAD blob，与 §三.0 逐字相同）：`4cd91256…`／`5d2ce928…`／`c5e5892d…`；`git status --short`＝0 行。

（R1→R6 六枚单放突变已全部起跑并逐字节还原；F＝收尾解决方案级全量。以下每一枚都按 §〇.9 十一项字段登记。）

#### R1｜身份闸失效（预检＝P3 BUILD_OK，`out/preflight-P3.txt`）

- ① 文件:行＝`src/Pry.Core/Memory/Hds006UnifiedMemoryRetrieval.cs:71`（载体内，本刀自建文件）
- ② 改前原文＝`            if (row.Identity != request.Identity)`
- ③ 改后原文＝`            if (row.Identity != row.Identity)`
- ④ `git diff --stat`＝` src/Pry.Core/Memory/Hds006UnifiedMemoryRetrieval.cs | 2 +-`，`1 file changed, 1 insertion(+), 1 deletion(-)`；`git diff -U0` 只上面那一对 ± 行
- ⑤ 跑法整行＝`dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave28-1024/out/trx-R1" --logger trx`（未用 `--no-build`）
- ⑥ TRX：先数枚数 `ls *.trx | wc -l` ＝ **1**，文件名＝`24782_PHYTHLINK_2026-10-24_15_28_53.trx`；`<Counters total="736" executed="734" passed="731" failed="3" error="0" … notExecuted="0" />`
- ⑦ 红格（逐字取 `outcome="Failed"` 节点，**与 §三.0 事前论证的 3 枚红集完全一致，无第 4 枚**）：
  - `Pry.Core.Tests.Hds006EvidenceIdentityRetrievalPrototypeTests.Retrieving_one_identity_never_carries_another(asked: Real, expectedId: 11)`
  - `…Retrieving_one_identity_never_carries_another(asked: Fiction, expectedId: 12)`
  - `…Retrieving_one_identity_never_carries_another(asked: Cognitive, expectedId: 13)`
  消息要点＝`Assert.Single() Failure: The collection contained 3 items  Collection: [Hds006RetrievalHit { EvidenceId = 13, Identity = Cognitive, Rank = 1 }, … { 12, Fiction, 2 }, … { 11, Real, 3 }]`；红点行号＝`tests/Pry.Core.Tests/Hds006EvidenceIdentityRetrievalPrototypeTests.cs:line 54`（三枚同一行）
  ⇒ §一.1"检索一条不能顺带把另一条带出来"这半条**会响**。
- ⑧ 还原命令＝`git cat-file blob HEAD:src/Pry.Core/Memory/Hds006UnifiedMemoryRetrieval.cs > …cs.tmp && mv -f …tmp …cs`（纯文件写，载体与主树零 `git` 写操作）
- ⑨ 还原后 `git status --short` 行数＝**0**
- ⑩ 突变态 SHA-256＝`fa8719e533c709142a3f6d7959a4167dd761397554a220015bb035e8f30b3073`（与 §三.0 预合副本值逐字相同）／还原后 SHA-256＝`4cd91256ea8f3c5b55d23fe550fcad3d5bc2f870d4d0f3089ca9ead32368104d`（＝`git cat-file blob 612423b:该文件` 现取值，逐字相同）
- ⑪ 当轮预检整段（`out/preflight-P3.txt`，15:28:40）：
  `=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===`
  `  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=1339,259,820x802 screen=2560x1440 covers=False borderless=False title=QQ`
  `  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=15532, TextInputHost pid=21228, explorer pid=13364`
  `gpu_util=13% used=1612MB` ／ `free_memory_gb=9.3` ／ `VERDICT=BUILD_OK`

#### R2｜来源缺失变成静默丢弃（预检＝P4 BUILD_OK，`out/preflight-P4.txt`）

- ① 文件:行＝`src/Pry.Core/Memory/Hds006UnifiedMemoryRetrieval.cs:80-81`
- ② 改前原文＝
  `                exclusions.Add(new Hds006RetrievalExclusion(row.EvidenceId,`
  `                    Hds006ExclusionReason.SourceMissing, 0));`
- ③ 改后原文＝（两行删除，`continue` 保留 ⇒ 行仍被跳过，但不再点名原因）
- ④ `git diff --stat`＝` src/Pry.Core/Memory/Hds006UnifiedMemoryRetrieval.cs | 2 --`，`1 file changed, 2 deletions(-)`
- ⑤ 跑法整行＝`dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave28-1024/out/trx-R2" --logger trx`（未用 `--no-build`）
- ⑥ TRX：`ls *.trx | wc -l` ＝ **1**，文件名＝`24782_PHYTHLINK_2026-10-24_15_30_13.trx`；`<Counters total="736" executed="734" passed="733" failed="1">`
- ⑦ 红格（**恰 1 枚，与事前论证逐字一致**）＝`Pry.Core.Tests.Hds006EvidenceIdentityRetrievalPrototypeTests.Evidence_whose_source_was_never_registered_is_reported_as_source_missing_not_dropped`
  消息要点＝`Assert.Single() Failure: The collection was empty`；红点行号＝`tests/Pry.Core.Tests/Hds006EvidenceIdentityRetrievalPrototypeTests.cs:line 75`
  ⇒ §一.2"来源缺失＝明确失败原因，不是静默丢弃"这半条**会响**。同轮的 `Invalidated_source_is_named_…` 未串味（走的是 `SourceInvalidated` 那条分支，未被本枚改到）。
- ⑧ 还原命令＝`git cat-file blob HEAD:src/Pry.Core/Memory/Hds006UnifiedMemoryRetrieval.cs > /tmp/r2.cs && mv -f /tmp/r2.cs <该文件>`（纯文件写）
- ⑨ 还原后 `git status --short` 行数＝**0**
- ⑩ 突变态 SHA-256＝`ce0fb3c6dcecf6cbda062ab51ef57293ede35b98fba3a3fc270f219086fc64ee`（＝§三.0 预合副本值）／还原后 SHA-256＝`4cd91256ea8f3c5b55d23fe550fcad3d5bc2f870d4d0f3089ca9ead32368104d`（＝`git cat-file blob 612423b:该文件` 现取值）
- ⑪ 当轮预检整段（`out/preflight-P4.txt`，15:30:01）：
  `=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===`
  `  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=1339,259,820x802 screen=2560x1440 covers=False borderless=False title=QQ`
  `  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=15532, TextInputHost pid=21228, explorer pid=13364`
  `gpu_util=14% used=1630MB` ／ `free_memory_gb=9.2` ／ `VERDICT=BUILD_OK`

#### R3｜截断永不发生（预检＝P5 BUILD_OK，`out/preflight-P5.txt`）

- ① 文件:行＝`src/Pry.Core/Memory/Hds006UnifiedMemoryRetrieval.cs:97`
- ② 改前原文＝`            if (rank > request.ResultLimit)`
- ③ 改后原文＝`            if (rank > rank)`（恒 false，入参上限不再参与判定；非固定返回值，比的是当场名次）
- ④ `git diff --stat`＝` src/Pry.Core/Memory/Hds006UnifiedMemoryRetrieval.cs | 2 +-`，`1 file changed, 1 insertion(+), 1 deletion(-)`
- ⑤ 跑法整行＝`dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave28-1024/out/trx-R3" --logger trx`（未用 `--no-build`）
- ⑥ TRX：`ls *.trx | wc -l` ＝ **1**，文件名＝`24782_PHYTHLINK_2026-10-24_15_31_31.trx`；`<Counters total="736" executed="734" passed="733" failed="1">`
- ⑦ 红格（**恰 1 枚，与事前论证一致**）＝`Pry.Core.Tests.Hds006EvidenceIdentityRetrievalPrototypeTests.Truncation_reports_every_cut_row_with_the_rank_it_would_have_had`
  消息要点＝`Assert.Equal() Failure: Strings differ  Expected: "54,53"  Actual: "54,53,52,51"  ↑ (pos 5)`——四条合格行全被返回，被截的那两条不再被点名；红点行号＝`…Hds006EvidenceIdentityRetrievalPrototypeTests.cs:line 139`
  同轮的 `Retrieval_orders_eligible_rows_by_recorded_sequence_then_evidence_id` 未响（该夹具 `ResultLimit=5` 大于其 4 条合格行，`Assert.Empty(result.Exclusions)` 不受本枚影响），与事前论证同形。
  ⇒ §一.3"哪几条被截、被截的为什么被截"当场可判这半条**会响**。
- ⑧ 还原命令＝`git cat-file blob HEAD:src/Pry.Core/Memory/Hds006UnifiedMemoryRetrieval.cs > /tmp/r3.cs && mv -f /tmp/r3.cs <该文件>`
- ⑨ 还原后 `git status --short` 行数＝**0**
- ⑩ 突变态 SHA-256＝`511cce9c695cf5ce061ea073907293625680205aa027317e9c14d6a976e1a66e`（＝§三.0 预合副本值）／还原后 SHA-256＝`4cd91256ea8f3c5b55d23fe550fcad3d5bc2f870d4d0f3089ca9ead32368104d`（＝`git cat-file blob 612423b:该文件`）
- ⑪ 当轮预检整段（`out/preflight-P5.txt`，15:31:18）：
  `=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===`
  `  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=1339,259,820x802 screen=2560x1440 covers=False borderless=False title=QQ`
  `  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=15532, TextInputHost pid=21228, explorer pid=13364`
  `gpu_util=6% used=1620MB` ／ `free_memory_gb=9.1` ／ `VERDICT=BUILD_OK`

#### R4｜一处派生物失效波及三处（预检＝P6 BUILD_OK，`out/preflight-P6.txt`）

- ① 文件:行＝`src/Pry.Core/Memory/Hds006DerivedArtifactLedger.cs:86`
- ② 改前原文＝`        var matched = _entries.Where(x => x.EvidenceId == key.EvidenceId && x.Kind == key.Kind`
- ③ 改后原文＝`        var matched = _entries.Where(x => x.EvidenceId == key.EvidenceId`（下一行 `&& x.State == …Live).ToList();` 原样保留 ⇒ 命中条件只看证据号）
- ④ `git diff --stat`＝` src/Pry.Core/Memory/Hds006DerivedArtifactLedger.cs | 2 +-`，`1 file changed, 1 insertion(+), 1 deletion(-)`
- ⑤ 跑法整行＝`dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave28-1024/out/trx-R4" --logger trx`（未用 `--no-build`）
- ⑥ TRX：`ls *.trx | wc -l` ＝ **1**，文件名＝`24782_PHYTHLINK_2026-10-24_15_32_46.trx`；`<Counters total="736" executed="734" passed="730" failed="4">`
- ⑦ 红格（**4 枚，与 §三.0 事前论证的"必连带 4 枚"逐字一致，无第 5 枚**）：
  - `…Hds006EvidenceIdentityRetrievalPrototypeTests.Invalidating_one_derived_artifact_leaves_the_other_two_live_and_readable(struck: Summary)` — 红点 `…Tests.cs:line 193`，消息＝`Assert.Equal() Failure: Values differ  Expected: Live  Actual: Invalidated`
  - `…Invalidating_one_derived_artifact_leaves_the_other_two_live_and_readable(struck: Vector)` — 同上 `:line 193`／`Expected: Live  Actual: Invalidated`
  - `…Invalidating_one_derived_artifact_leaves_the_other_two_live_and_readable(struck: Cache)` — 同上 `:line 193`／`Expected: Live  Actual: Invalidated`
  - `…Invalidation_appends_exactly_one_observable_entry_naming_the_artifact` — 红点 `…Tests.cs:line 233`，消息＝`Assert.Single() Failure: The collection contained 3 matching items`（观察日志由 1 条变 3 条＝"失效必须可观察"这一面的反面）
  ⇒ §一.4"三处派生物各自失效、另两处不受影响"与"失效可观察"两半**都会响**。同轮 `Derived_invalidation_redacts_the_payload_and_names_the_reason_in_the_readout` 仍绿（它只判被点名的那一处，与本枚改的命中条件不同路），与事前论证一致。
- ⑧ 还原命令＝`git cat-file blob HEAD:src/Pry.Core/Memory/Hds006DerivedArtifactLedger.cs > /tmp/r4.cs && mv -f /tmp/r4.cs <该文件>`
- ⑨ 还原后 `git status --short` 行数＝**0**
- ⑩ 突变态 SHA-256＝`9912cf11fbc6abaf2866d66a2d8b538fbf8989dd7609c10296e000af704b0580`（＝§三.0 预合副本值）／还原后 SHA-256＝`5d2ce9281acfcac636d9b718eba782962dc393fb178325ee7906e2627ae2fde5`（＝`git cat-file blob 612423b:该文件`）
- ⑪ 当轮预检整段（`out/preflight-P6.txt`，15:32:33）：
  `=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===`
  `  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=1339,259,820x802 screen=2560x1440 covers=False borderless=False title=QQ`
  `  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=15532, TextInputHost pid=21228, explorer pid=13364`
  `gpu_util=7% used=1619MB` ／ `free_memory_gb=9` ／ `VERDICT=BUILD_OK`

#### R5｜撤销只看状态版本、不看来源版本（预检＝P7 BUILD_OK，`out/preflight-P7.txt`）

- ① 文件:行＝`src/Pry.Core/Memory/Hds006EvidenceStore.cs:129`
- ② 改前原文＝`        if (ledger.Readout(record.SourceId).SourceVersion != record.ExpectedSourceVersion)`
- ③ 改后原文＝`        if (ledger.Readout(record.SourceId).SourceVersion != ledger.Readout(record.SourceId).SourceVersion)`（来源版本条件恒不成立；比的是当场读数，不是固定返回值）
- ④ `git diff --stat`＝` src/Pry.Core/Memory/Hds006EvidenceStore.cs | 2 +-`，`1 file changed, 1 insertion(+), 1 deletion(-)`
- ⑤ 跑法整行＝`dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave28-1024/out/trx-R5" --logger trx`（未用 `--no-build`）
- ⑥ TRX：`ls *.trx | wc -l` ＝ **1**，文件名＝`24782_PHYTHLINK_2026-10-24_15_34_17.trx`；`<Counters total="736" executed="734" passed="733" failed="1">`；该轮构建 `warning` 行数＝0
- ⑦ 红格（**恰 1 枚，与事前论证一致**）＝`Pry.Core.Tests.Hds006EvidenceIdentityRetrievalPrototypeTests.Undo_whose_source_line_advanced_after_the_detach_is_refused_by_the_source_version`
  消息要点＝`Assert.Equal() Failure: Values differ  Expected: SourceVersionMismatch  Actual: Restored`；红点行号＝`…Tests.cs:line 276`
  同轮 `Undo_against_a_row_that_moved_…`（第一道状态版本闸，未被本枚触及）与 `A_refused_undo_keeps_the_recovery_record_available`（同在第一道闸返回）、`After_a_successful_undo_…`（两处版本本来对得上）三枚均绿 ⇒ 本枚只摘掉来源这一道闸，不串味，与事前论证同形。
  ⇒ v4:302 AUD-03 那一课（"恢复要以状态版本**和来源版本**为条件"）在新链形状上**会响**。
- ⑧ 还原命令＝`git cat-file blob HEAD:src/Pry.Core/Memory/Hds006EvidenceStore.cs > /tmp/r5.cs && mv -f /tmp/r5.cs <该文件>`
- ⑨ 还原后 `git status --short` 行数＝**0**
- ⑩ 突变态 SHA-256＝`cc77b4b634ec4d5d5099661c40da5c9bd11764e9f014ddaddd8bf9edbade11c0`（＝§三.0 预合副本值）／还原后 SHA-256＝`c5e5892d7f239d98b61389f9d70576354ad76e7e430f1c5ae09fc454269659b4`（＝`git cat-file blob 612423b:该文件`）
- ⑪ 当轮预检整段（`out/preflight-P7.txt`，15:34:05）：
  `=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===`
  `  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=1339,259,820x802 screen=2560x1440 covers=False borderless=False title=QQ`
  `  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=15532, TextInputHost pid=21228, explorer pid=13364`
  `gpu_util=10% used=1612MB` ／ `free_memory_gb=9.4` ／ `VERDICT=BUILD_OK`

#### R6｜后续写入不看状态版本＝AUD-03 的原形状（预检＝P8 BUILD_OK，`out/preflight-P8.txt`）

- ① 文件:行＝`src/Pry.Core/Memory/Hds006EvidenceStore.cs:91`
- ② 改前原文＝`        if (row.StateVersion != expectedStateVersion) return false;`
- ③ 改后原文＝`        if (row.StateVersion != row.StateVersion) return false;`（写回闸恒不拦＝`SqliteMemoryRepository:433` 那条"版本更新结果未检查"在新链形状上的等价物）
- ④ `git diff --stat`＝` src/Pry.Core/Memory/Hds006EvidenceStore.cs | 2 +-`，`1 file changed, 1 insertion(+), 1 deletion(-)`
- ⑤ 跑法整行＝`dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave28-1024/out/trx-R6" --logger trx`（未用 `--no-build`）
- ⑥ TRX：`ls *.trx | wc -l` ＝ **1**，文件名＝`24782_PHYTHLINK_2026-10-24_15_35_45.trx`；`<Counters total="736" executed="734" passed="733" failed="1">`
- ⑦ 红格（**恰 1 枚，与事前论证一致**）＝`Pry.Core.Tests.Hds006EvidenceIdentityRetrievalPrototypeTests.After_a_successful_undo_a_stale_write_cannot_overwrite_the_restored_content`
  消息要点＝`Assert.False() Failure  Expected: False  Actual: True`（那次带着旧条件的写回**竟然成功了**＝已撤销内容可被后续证据覆盖）；红点行号＝`…Tests.cs:line 297`
  同轮 `A_refused_undo_keeps_the_recovery_record_available` 未响：它那次改写带的期望版本与行号本就相同，改写成立与否都不改变它随后被状态版本闸拒的结果，与事前论证同形。
  ⇒ v4:302 AUD-03 那句"后续证据仍可覆盖"在新链上有了一枚**会响**的格。
- ⑧ 还原命令＝`git cat-file blob HEAD:src/Pry.Core/Memory/Hds006EvidenceStore.cs > /tmp/r6.cs && mv -f /tmp/r6.cs <该文件>`
- ⑨ 还原后 `git status --short` 行数＝**0**（四枚产品文件 SHA 复验全部＝§三.0 基准：`4cd91256…`／`5d2ce928…`／`c5e5892d…`／`a8f69840…`）
- ⑩ 突变态 SHA-256＝`103b3f98352de8db9b45fee88c9fe43721700695e2d7f4971ff75212448c2c70`（＝§三.0 预合副本值）／还原后 SHA-256＝`c5e5892d7f239d98b61389f9d70576354ad76e7e430f1c5ae09fc454269659b4`（＝`git cat-file blob 612423b:该文件`）
- ⑪ 当轮预检整段（`out/preflight-P8.txt`，15:35:33）：
  `=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===`
  `  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=1339,259,820x802 screen=2560x1440 covers=False borderless=False title=QQ`
  `  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=15532, TextInputHost pid=21228, explorer pid=13364`
  `gpu_util=5% used=1650MB` ／ `free_memory_gb=9.3` ／ `VERDICT=BUILD_OK`
- 附记（照实登记）：这一枚的自比较写法让该轮构建临时打出 1 行 `warning CS1718: 对同一变量进行比较；是否希望比较其他变量?`（`Hds006EvidenceStore.cs(91,13)`，突变态独有）。还原后该文件 SHA 回到基准值，警告随之消失；本刀不把它当作产品面警告，也不为此改判据。

#### F｜解决方案级全量收尾轮（预检＝P9 BUILD_OK，`out/preflight-P9.txt`）

- ⑤ 跑法整行＝`dotnet test Pry.slnx -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave28-1024/out/trx-FULL" --logger trx`
- **本轮为何不带 `--no-build`（照实登记，不当成捷径）**：`--no-build` 只在那一版是合法选项，不是义务；R6 那轮把 `Pry.Core.dll` 造在了**突变态源码**上，随后我只还原了源码、没有再构建。若收尾全量沿用 `--no-build`，读到的仍是带着写回闸失效 IL 的旧 assembly，出口读数就不描述还原后的树。解决方案级构建额度（≤1）已由 B1 用掉、本刀不重复调用，所以让收尾这轮自己增量重建——四份 TRX 都出自还原后的树，`git status --short`＝0 行是当场佐证的。
- ⑥ TRX：先数枚数 `ls *.trx | wc -l` ＝ **4**，文件名＝
  `24782_PHYTHLINK_2026-10-24_15_37_16.trx`／`24782_PHYTHLINK_2026-10-24_15_37_16[1].trx`／`24782_PHYTHLINK_2026-10-24_15_37_20.trx`／`24782_PHYTHLINK_2026-10-24_15_37_25.trx`
- 逐份 `<Counters>`（先数后读，逐份点名列）：
  | TRX | 工程 | total | executed | passed | failed |
  |---|---|---|---|---|---|
  | `…15_37_16.trx` | `Pry.Client.Tests` | 108 | 108 | 108 | 0 |
  | `…15_37_16[1].trx` | `Pry.Core.Tests` | **736** | 734 | 734 | 0 |
  | `…15_37_20.trx` | `Pry.Api.Tests` | 280 | 280 | 280 | 0 |
  | `…15_37_25.trx` | `Pry.App.Tests` | 233 | 233 | 233 | 0 |
  | **当场求和（awk，不从工单抄）** | 四工程 | **1357** | **1355** | **1355** | **0** |
- 出口等式核对＝§〇.4 起点 **1339** ＋ §二 新格 **18** ＝ **1357** ✔；Core 分项 **718 → 736** ✔（正是那 18 枚的唯一去处）；executed 1355 ＝ 1357 − 2；跳过 2 枚按 `outcome="NotExecuted"` 逐字点名＝`Pry.Core.Tests.LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`／`Pry.Core.Tests.CoreTests.Configured_sense_voice_model_can_run_offline`（在册那两枚，本刀未新增跳过，`notExecuted` 计数器仍为 0 不看）。
- 该轮构建面：`grep -c warning run-FULL.txt` ＝ **0**，0 错误 ⇒ B1 那两枚警告修好后干净树复现无警告。
- 本刀 18 枚在收尾 Core 那份 TRX（`…15_37_16[1].trx`）里的复验：`grep -o 'name="Pry.Core.Tests.Hds006EvidenceIdentityRetrievalPrototypeTests.[^"]*"' | sort -u | wc -l` ＝ **18 个不同节点名**，且其中 `outcome="Failed"` ＝ **0 枚** ⇒ 出口那 1357 里的 18 枚确实是我这四份 TRX 里跑过并绿着的节点，不是纸面推算。
- `measure-fixed-eval.ps1`（载体那份脚本，`-ResultsDirectory` 指向上面那四枚 TRX，归档＝`out/measure-FULL.txt`）＝
  `Catalog valid: 76 permanent scenarios. Mappings are partial evidence only.` ／ `Passed=1355; Failed=0; Other=2.`
  与四份 TRX 求和**自洽**（Passed 1355＝executed 1355，Other 2＝两枚 NotExecuted，Catalog 76 未变）；证据文件落在 `.local-notes/wave28-1024/out/trx-FULL/fixed-eval-coverage.json`，主树 `docs/eval/fixed-eval-coverage.json` 一个字节未动（跑完后载体 `git status --short`＝0 行、主树仍 8 行且不含该文件）。
- ⑨ 本轮之后 `git status --short` 行数＝**0**（载体）／**8**（主树，＝他人 2 枚 `M ` ＋ 5 项 `??` ＋ 本刀那枚报告）
- ⑪ 当轮预检整段（`out/preflight-P9.txt`，15:37:05）：
  `=== GAME PREFLIGHT v6 (v5 rule + unreadable-path hold + game window-class hold) ===`
  `  FOREGROUND: QQ pid=28676 class=Chrome_WidgetWin_1 style=0x14C70000 rect=1339,259,820x802 screen=2560x1440 covers=False borderless=False title=QQ`
  `  UNFOCUSED-BORDERLESS-FULLSCREEN (informational only): NVIDIA Overlay pid=15532, TextInputHost pid=21228, explorer pid=13364`
  `gpu_util=6% used=1640MB` ／ `free_memory_gb=9.3` ／ `VERDICT=BUILD_OK`
- 预检台账口径照实登记：本刀 9 次预检 P1–P9 全部 `BUILD_OK`，**0 次 `HOLD`**，所以 §〇.7 那句"最多累计 8 次"读作 `HOLD` 之后的重跑额度（本刀用了 0 枚），不是对"每次 `dotnet` 前必跑一枚"设上限——否则一句"每次 dotnet 前必跑"与"≤8 次"在同一把刀里就互相排斥（本刀合法形状至少要 9 次）。本刀未因此绕开任何一次预检：9 枚整段全部落盘归档，逐一对应一次 `dotnet`。
- 额度终值：解决方案级构建 **1/1**、解决方案级全量 **1/1**、定向 **7/8**（G0 绿基线＋R1–R6 六枚单放突变；留 1 枚未用，按工单"形状不符时留 2 枚额度复取"的口径，六枚红集全部与事前论证逐字相符，无需复取）。`dotnet` 调用总次数＝**9**（build 1＋test 8），盘上产物＝`out/build-B1.txt`、`out/run-G0.txt`…`out/run-R6.txt`、`out/run-FULL.txt` 共 9 份运行日志，TRX 共 **11 份**（G0 1＋R1–R6 各 1＝6＋收尾 4；当场 `find out -name '*.trx' | wc -l` ＝ 11），预检整段归档 **9 份**（`preflight-P1.txt`…`preflight-P9.txt`）。


### 三.2 六枚反证的汇总对照（实际 vs §三.0 事前论证）

| 轮 | 闸（失效面） | 实际 failed 节点数 | 事前论证值 | 相符 | 红点行号 | 顶上工单 §一 哪一条 |
|---|---|---|---|---|---|---|
| R1 | 身份闸 | 3 | 3 | ✔ | `:line 54` | §一.1 |
| R2 | 来源缺失不点名 | 1 | 1 | ✔ | `:line 75` | §一.2 |
| R3 | 截断闸 | 1 | 1 | ✔ | `:line 139` | §一.3 |
| R4 | 派生物命中条件 | 4 | 4 | ✔ | `:line 193`×3／`:line 233` | §一.4 |
| R5 | 撤销的来源版本条件 | 1 | 1 | ✔ | `:line 276` | §一.5（v4:325 双版本条件） |
| R6 | 写回的状态版本条件 | 1 | 1 | ✔ | `:line 297` | §一.5（v4:302 AUD-03 原形状） |

- 六轮的 `total` 恒为 736、`executed` 恒 734、跳过恒 2、其余格全绿 ⇒ 每枚突变只摘它自己那一道闸，没有合轮、没有连带意外。留出的 1 枚复取额度未动用。
- 18 枚节点里本刀**取到红证的是 11 枚**（落在 7 个方法名上＝5 枚 Fact ＋ 2 枚 Theory 各展开 3 条：`Retrieving_one_identity_never_carries_another`×3、`Evidence_whose_source_was_never_registered_…`、`Truncation_reports_…`、`Invalidating_one_derived_artifact_leaves_the_other_two_live_and_readable`×3、`Invalidation_appends_exactly_one_observable_entry_…`、`Undo_whose_source_line_advanced_…`、`After_a_successful_undo_…`）；**没有红证的是 7 枚 Fact 节点**＝`Same_sentence_under_the_three_identities_…`、`Invalidated_source_is_named_…`、`Retrieval_orders_eligible_rows_…`、`The_prototype_applies_no_retrieval_floor_…`、`Derived_invalidation_redacts_the_payload_…`、`Undo_against_a_row_that_moved_…`、`A_refused_undo_keeps_the_recovery_record_available`，逐条登记在 §五 第 9 条并押下一把（其中 §三.0 末段事前点名 3 枚）。

## 四 产品面自证与归位通道

### 四.1 产品面自证（当场取，载体 `612423b` 线尾）

```
$ git -C <载体> diff --stat 2d90650..HEAD -- src tests scripts Pry.slnx Directory.Build.props
 src/Pry.Core/Memory/Hds006DerivedArtifactLedger.cs            | 132 +++++++
 src/Pry.Core/Memory/Hds006EvidenceIdentity.cs                 | 113 +++++++
 src/Pry.Core/Memory/Hds006EvidenceStore.cs                    | 155 ++++++++
 src/Pry.Core/Memory/Hds006UnifiedMemoryRetrieval.cs           | 108 +++++++
 .../Hds006EvidenceIdentityRetrievalPrototypeTests.cs          | 352 +++++++++++++
 5 files changed, 860 insertions(+)

$ git -C <载体> diff --check 2d90650..HEAD      # 退出码 0，无空白错误
$ git -C <载体> status --short | wc -l          # 0
```

- 只出现**本刀新建的 5 枚文件**，无 deletions、无既有文件改名、无 `scripts/**`／`Pry.slnx`／`Directory.Build.props`／`licenses/**`／`ASSETS_LICENSE.md`／`FAN_CONTENT_POLICY.md` 的任何身影。
- 数字门槛面：五枚新文件里**没有任何新增或改动的数字门槛**——检索上限是入参 `ResultLimit`（无默认值），版本判定只做**当场相等比较**（CAS），序号/置信度/成本全由夹具调用方给值；`.5` 地板只以 §〇.6 那行既有码为参照、在本链上不存在第二处。§六.2 在册清单（`.7`／`.5`／`2400`／五档超时／`12`／`4096`／`128`／`512`／`8`／`SegmentCount = 8`）逐字未碰。
- 主树面：本刀在主工作树**零** `git` 写操作（无 `commit`／`add`／`checkout`／`reset`／`stash`／`clean`／`rm`／`cherry-pick`／`merge`／`tag`／`push`／`apply`），唯一写入＝本报告文件；他人 2 枚 `M ` 暂存项与 5 项 `??` 未读内容、未改、未提交、未删（主树 `git status --short` 收尾仍＝8 行，多出的那 1 行就是本报告）。

### 四.2 归位通道（给守门人，不在本 lane 权限内）

按第二十七波更正的**按路径**落地，不用 `--ff-only`（本刀载体父提交＝`2d90650`，而主树线尾已＝`948935f`，FF 必失败）：

```
git -C C:/Users/24782/Desktop/Pry_Project checkout 612423b76d03df9f3130d1ba536e88bc75b79f5e -- \
  src/Pry.Core/Memory/Hds006EvidenceIdentity.cs \
  src/Pry.Core/Memory/Hds006UnifiedMemoryRetrieval.cs \
  src/Pry.Core/Memory/Hds006DerivedArtifactLedger.cs \
  src/Pry.Core/Memory/Hds006EvidenceStore.cs \
  tests/Pry.Core.Tests/Hds006EvidenceIdentityRetrievalPrototypeTests.cs
git -C C:/Users/24782/Desktop/Pry_Project commit --only -m "feat(HDS-006,wave28): <报告要点>" -- \
  <同上述五枚路径>
```

- 供守门人核的四件事本刀先自陈：① 红格归属＝逐格点名到 TRX `outcome="Failed"` 节点（R1 3 枚、R2 1 枚、R3 1 枚、R4 4 枚、R5 1 枚、R6 1 枚＝**11 枚红次，落在 7 个不同方法名上**：`Retrieving_one_identity_never_carries_another`×3 条、`Evidence_whose_source_was_never_registered_…`、`Truncation_reports_…`、`Invalidating_one_derived_artifact_leaves_the_other_two_live_and_readable`×3 条、`Invalidation_appends_exactly_one_observable_entry_naming_the_artifact`、`Undo_whose_source_line_advanced_…`、`After_a_successful_undo_…`），无批次红署给单枚突变；② 常绿门自查见 §二 末段，18 枚内无"对空集合判不含"形状；③ 四份 TRX 求和 1357／1355／0 与 `measure-fixed-eval` 的 `Passed=1355; Failed=0; Other=2` 自洽，在册基线是否由这一把顶、顶成什么数＝守门人的权利，本刀只报等式 1339＋18＝1357；④ §一 五条**每条都有一枚会响的格**（§一.1→R1、§一.2→R2、§一.3→R3、§一.4→R4、§一.5→R5 与 R6）。
- 载体两枚提交的全号：`f6589d5aeed5911c011a3a0c9c97a3fa47f2e745`（5 枚新文件）、`612423b76d03df9f3130d1ba536e88bc75b79f5e`（线尾，只改本刀判据文件 2 行清警告）。按路径落地只需线尾 `612423b`。

## 五 未验证（逐条点名，不用"已完成"混过去）

1. **真实模型对照＝零读数**：本刀全部判据是内存形状断言，没有任何一枚调用真实模型或 llama 后端。
2. **生产接线＝零**：`grep -rln "Hds006" src/Pry.Api src/Pry.App src/Pry.Client src/Pry.Contracts` ＝ 0 命中；四枚产品类没有任何生产调用方，没接 005 的落库面、没端点、没后台任务。HDS-006 的"全链"因此**远未成立**，接线时机＝U-9 仍未拍，本刀不替谁拍。
3. **UI 可见行为＝零读数**：没起过界面进程，没看过任何一屏。
4. **生产库＝零接触**：`%LOCALAPPDATA%\PryCompanion` 未打开、未写、未当载体；本刀不建库、不迁表、不跑 `MemorySchemaMigrator`。
5. **待标定参数权威值**：`Hds006RetrievalRequest.ResultLimit` 在生产里该取几、置信度在身份之间如何折价、派生物重算的触发阈值——本刀一律由调用方入参承担，没有给出任何权威值，也没资格给。
6. **旧链删除片与"构建时不把旧链打进包"那道门＝未开始**：`docs/legacy-chain-deprecation.md` §4 第 2／3／4 项（处置表、打包形状门、判据账重基线、数据继承）照旧欠，第 1 项也没因本刀前进；本刀没读旧链的删除路径，也没证明任何一行旧码可删。
7. **`.5` 检索地板在新链上是否真的守住＝未验证**：本原型**没有地板**（§〇.6），`The_prototype_applies_no_retrieval_floor_…` 只钉住"今天没有地板"这条事实，不担保"新链守住了 `.5`"；要证这条得把新链接到真实检索口，那是接线刀的地盘。本刀也不为它造一个数去凑。
8. **本刀 18 枚新格的换人复采＝欠**：写判据的这只手不能同时当第二双手；这 18 枚全部押下一把复采刀。**本刀新格数第三次顶账＝18**（＝12 枚 `Fact` ＋ 2 枚 `Theory` 各展开 3 条 `InlineData`，按属性行逐行数、不按断言数；与 §二 与 G0/F 两份 TRX 的节点清单一致）。换手欠账现值＝**75 格**（10-24 15:0x MEMORY 台账口径），与本刀新格无交集，**不因本刀新增 18 枚而减少**。
9. **没有红证的 7 枚 Fact 照实登记**（§三.2 末段列名）：
   - `Same_sentence_under_the_three_identities_is_three_distinct_evidence_rows` 与 `Retrieval_orders_eligible_rows_by_recorded_sequence_then_evidence_id`：本刀六枚额度里没有一枚能单独打掉"记录即分行"或"排序键"而不撞上已取过红的那道闸（排序要红得改 `OrderByDescending(RecordedSequence)` 的比较键，那是新增数字门槛的另一条路）；押下一把。
   - `Invalidated_source_is_named_…`：与 R2 同族分支，R2 已证"原因必须点名"这条闸会响，但这枚自己的红没取到。
   - `The_prototype_applies_no_retrieval_floor_…`：要让它红只能往产品码里**新增**一个数字门槛＝工单 §六.2 禁止，本刀不作弊。
   - `Derived_invalidation_redacts_the_payload_…`：R4 已证同文件命中条件会响，但这枚单格的红未取。
   - `Undo_against_a_row_that_moved_…` 与 `A_refused_undo_keeps_the_recovery_record_available`：两枚都靠第一道状态版本闸，R6 摘的是**写回**那道闸、R5 摘的是**来源**那道闸，没有一枚专挑 `TryRestore` 里第一道闸（`:128`）；本刀定向额度已按 §三.0 的分配用给五条覆盖，这两枚押下一把（`TryRestore` 的 :128 一行是它们的下家）。
   这 7 枚在 G0 与 F 两轮里都是绿的，但**绿不等于已反证**；本刀不撤格、不改判据、不假装它们已被反证。

## 六 下一手逐字可粘贴配方

本刀九轮全部起跑、六枚突变全部还原，**没有"未起跑"的轮次**，所以这里给的是下一把刀的接手配方而不是本刀的补跑清单：

```
# 0) 每一次 dotnet 之前先跑预检，只有 VERDICT=BUILD_OK 才继续
powershell -NoProfile -ExecutionPolicy Bypass -File C:/Users/24782/Desktop/Pry_Project/.local-notes/game-preflight6-1022.ps1

# 1) 载体（主树零 git 写操作；基点用当时线尾全号）
git -C C:/Users/24782/Desktop/Pry_Project worktree add --detach \
  C:/Users/24782/Desktop/Pry_Project/.local-notes/qa-wave28-1024/carrier 2d90650

# 2) 复采本刀 18 枚（换手第二双手）：绿跑一次即可
dotnet test tests/Pry.Core.Tests -c Release \
  --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave28-1024/out/trx-RESYNC" --logger trx

# 3) 若要复现某枚红证（示例＝R6，AUD-03 原形状；跑完立刻按同法还原）
cp C:/Users/24782/Desktop/Pry_Project/.local-notes/wave28-1024/probe/R6-rewrite-version-check-inert.cs \
   src/Pry.Core/Memory/Hds006EvidenceStore.cs
dotnet test tests/Pry.Core.Tests -c Release \
  --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave28-1024/out/trx-R6-replay" --logger trx
git cat-file blob HEAD:src/Pry.Core/Memory/Hds006EvidenceStore.cs > /tmp/restore.cs && mv -f /tmp/restore.cs src/Pry.Core/Memory/Hds006EvidenceStore.cs
sha256sum src/Pry.Core/Memory/Hds006EvidenceStore.cs   # 应为 c5e5892d7f239d98b61389f9d70576354ad76e7e430f1c5ae09fc454269659b4
```

- 六枚突变副本与各自 SHA 仍在 `.local-notes/wave28-1024/probe/`：`R1 fa8719e533c709142a3f6d7959a4167dd761397554a220015bb035e8f30b3073`／`R2 ce0fb3c6dcecf6cbda062ab51ef57293ede35b98fba3a3fc270f219086fc64ee`／`R3 511cce9c695cf5ce061ea073907293625680205aa027317e9c14d6a976e1a66e`／`R4 9912cf11fbc6abaf2866d66a2d8b538fbf8989dd7609c10296e000af704b0580`／`R5 cc77b4b634ec4d5d5099661c40da5c9bd11764e9f014ddaddd8bf9edbade11c0`／`R6 103b3f98352de8db9b45fee88c9fe43721700695e2d7f4971ff75212448c2c70`。
- **本刀欠的下一手＝§五 第 8 条的换人复采 18 枚**，以及 §五 第 6、7 条那两片（旧链删除片＋打包门；`.5` 地板在新链上的把守）——都要在接线之后才有可判形状，本刀不越过。

## 七 本刀对工单的三处不接受／更正（照实登记，不自扩也不自缩）

1. **§一.3"原样引用既有常量"这句不可执行**：`.5` 在盘上是 `MemoryQueryService.cs:22` 的**行内字面量**，不是命名常量（§〇.6 已贴原文）。要"原样引用"就只能把 `>= .5` 复制进新链＝正撞工单自己那句"不许复制成第二处"与 §六.2。本刀因此走工单给的另一分支：不设地板、把"没有地板"明写成事实＋一条格（`The_prototype_applies_no_retrieval_floor_the_existing_confidence_gate_would_have_applied`），并把"新链守住 `.5`"登记为未验证（§五 第 7 条）。
2. **§〇.7 的"最多累计 8 次"与"每一次 `dotnet` 之前必须先跑"在本刀的合法形状下互斥**（build＋绿基线＋6 枚突变＋收尾全量＝9 次 `dotnet` ⇒ 9 枚预检）。本刀按上下文把 8 读作 `HOLD` 之后的**重跑**额度（本刀 9 次全 `BUILD_OK`，重跑额度动用 0 次），并保证没有为省预检而跳过任何一次：`preflight-P1…P9.txt` 九份整段全部落盘、逐一对应一次 `dotnet`（§〇.5 台账）。这条解读若守门人不认，责任在工单文字而非本刀绕闸。
3. **收尾全量没按 §〇.8 括注带 `--no-build`**：R6 之后盘上的 `Pry.Core.dll` 是突变态 IL，若沿用 `--no-build`，那 1357 读的是**没还原的树**，顶账就不成立。`--no-build` 在工单里是"仅收尾允许"的特例而非义务，本刀让收尾轮自行增量重建（未新增任何解决方案级构建调用，构建仍 1/1）。理由与后果完整登记在 §三.1 F 段。

B1 打出的 2 枚警告落在我自己的新判据文件里，清它们＝第二枚载体提交 `612423b`，按路径落地时以线尾为准。

## 八 守门人收口（签名 `eval-QA-WAVE28-GK-1024`，10-24 15:5x 全部当场取数）

裁定＝**这一把归位，出口读数由本把顶**。一句根据：工单 §一 五条每条都有一枚"改坏产品码会响"的格（lane 自取 11 枚红＋守门人补取 6 枚＝**17／18 有红证**），唯一无红的那枚是"新链今天没有 `.5` 地板"那条数值锁格，它的反证方向＝往产品码里新增一个被工单 §六.2 禁掉的数字门槛，因此按 §八.6 采成明写事实而不是把它算成常绿门。

### 八.1 lane 撞 150 轮上限的形状（先数盘上，再读自述）

上限通知（"The task may be incomplete"）到达时它自述"刚把六枚突变 SHA 归档、正准备写 §一／§二／§三.0"。盘上不是那样：报告 484 行、§〇–§七 齐（mtime 15:43:18）、载体两枚提交 `f6589d5`＋`612423b`、11 份 TRX、9 份预检整段、6 份突变副本全在。**通知文字按纪律只算转达，不算证据**；这一把的交付按盘上判定为完整，"撞上限"不等于"刀是空的"。

### 八.2 独立复核（不做加法，逐项从原始产物取）

| 复核项 | 我从盘上取到的值 | 与报告 |
|---|---|---|
| 载体提交 | `git log --oneline 2d90650..HEAD`＝2 枚（`f6589d5` 5 新文件 860 insertions／`612423b` 只改判据文件 2 行） | 相符 |
| 载体干净度 | `git status --short \| wc -l`＝**0**（起跑前与五轮复采后各数一次） | 相符 |
| 产品面自证 | `git diff --stat 2d90650..HEAD -- src tests scripts Pry.slnx Directory.Build.props`＝`5 files changed, 860 insertions(+)`，只有本刀新建五枚、无 deletions | 相符 |
| TRX 枚数 | 本刀 11 份＋守门人补采 5 份＝盘上 `find out -name '*.trx' \| wc -l`＝**16** | 相符（报告写 11 是它自己那九轮） |
| 收尾全量四份 `<Counters>` | `…15_37_16.trx` 108/108/108/0；`…15_37_16[1].trx` **736**/734/734/0；`…15_37_20.trx` 280/280/280/0；`…15_37_25.trx` 233/233/233/0 ⇒ 当场求和 **1357／executed 1355／passed 1355／failed 0** | 逐字相符 |
| 两条跳过 | 按 `outcome="NotExecuted"` 点名＝`Configured_llama_server_requires_the_child_environment_key`／`Configured_sense_voice_model_can_run_offline`（在册那两枚，本把未新增跳过） | 相符 |
| 九轮预检 | `preflight-P1…P9.txt` 九份**全部** `VERDICT=BUILD_OK`、0 次 HOLD | 相符 |
| 六枚突变副本 SHA | `R1 fa8719e5…`／`R2 ce0fb3c6…`／`R3 511cce9c…`／`R4 9912cf11…`／`R5 cc77b4b6…`／`R6 103b3f98…` 与 §三.0 预合值逐字符同 | 相符 |
| 还原基准四枚 SHA | `4cd91256…`／`5d2ce928…`／`c5e5892d…`／判据 `376af36c…`，与 `git cat-file blob 612423b:<该文件>` 现取值同 | 相符 |
| `measure-fixed-eval.ps1` | `Catalog valid: 76`／`Passed=1355; Failed=0; Other=2` | 与四份 TRX 自洽 |

### 八.3 工单 §四 那"四件事"的逐项判定

1. **红格归属**：六轮逐份 TRX 的 `outcome="Failed"` 节点名我对过，R1 恰 3 枚、R2／R3／R5／R6 各恰 1 枚、R4 恰 4 枚，全部与 §三.0 事前论证的节点名逐字相同；**没有批次红署给单枚突变**（工单 §六.6 的红线未碰）。⇒ 通过。
2. **常绿门**：18 枚里没有"对空集合判不含"的形状；§一.2 那枚 Missing 格除判 Hits 空还判 `Assert.Single(result.Exclusions)`＋原因文本＋读数三件；`The_prototype_applies_no_retrieval_floor_…` 判的是"那一行仍被返回"＝存在性断言，加门槛就会响（它无红的原因见 §八.6，不算常绿）。⇒ 通过。
3. **读数自洽**：见 §八.2 那两行（四份 TRX 求和 vs `measure-fixed-eval`）。⇒ 通过，顶账由本把做，见 §八.5。
4. **§一 五条各有一枚会响的格**：§一.1→R1（加守门人 R11）、§一.2→R2（加 R8）、§一.3→R3（加 R7）、§一.4→R4（加 R9）、§一.5→R5＋R6（加 R10）。⇒ 通过。

### 八.4 §七 三处不接受的裁定：三处**全部接受**，责任在工单文字

1. `.5` 确实是 `MemoryQueryService.cs:22` 的行内字面量（本把 `git show HEAD:src/Pry.Core/Memory/MemoryQueryService.cs` 复读到同一行），"原样引用既有常量"这句在本仓库不可执行；lane 走工单自给的另一分支（不设地板＋把"没有地板"钉成格＋登记未验证）是**正确形状**，采纳为通用口径：工单里"原样引用既有常量"以后一律写成"引用不到命名常量时，把缺失本身钉成一条格，不许复制字面量"。
2. §〇.7"最多累计 8 次"与"每次 `dotnet` 前必跑一枚"在同一条刀里互斥（本刀合法形状下限＝9 枚），lane 把 8 读作 HOLD 之后的重跑额度且九枚整段全落盘 ⇒ 采信，工单文字按 §八.4.1 的同一写法更正：**预检次数不设上限，`HOLD` 之后的重跑设上限**。
3. 收尾全量不带 `--no-build`：上一轮把 `Pry.Core.dll` 造在突变态源码上，沿用就会读到没还原的树。lane 的判断是对的，本把把它升为通用口径：**收尾全量只有在"上一轮之后已完成一次重建"时才允许 `--no-build`，否则让它自行增量重建**。

### 八.5 守门人亲手补拿的五轮反证（十一字段逐轮齐；突变副本先合成并 `sha256sum` 落盘、后起跑＝第二十七波前置）

资格自证：这五轮的判据字节**不在我的改动集内**——判据文件工作副本 SHA＝`376af36ccccf803414388c20c69222f46a794e070d3102b7e5a83fba3f620f75`＝`git cat-file blob 612423b:` 现取值，逐字符同；`git log -1 -- tests/Pry.Core.Tests/Hds006EvidenceIdentityRetrievalPrototypeTests.cs`＝`612423b`（lane 那两枚提交），我的五轮只临时改产品文件并按 blob 原样还原。工作树＝lane 的载体 `.local-notes/qa-wave28-1024/carrier`（detached @ 基点 `2d90650`，HEAD `612423b`），起跑前后 `git status --short` 均＝0 行。注意署名面：本仓库所有 lane 与守门人共用同一个 git 身份 `LemPhyth`，**归属只认提交号与工单，不认作者名**。

#### GK-R7｜排序并列时的次键反了（预检＝GK1 `BUILD_OK`，`out/preflight-GK1.txt`，15:50:21）

- ① 文件:行＝`src/Pry.Core/Memory/Hds006UnifiedMemoryRetrieval.cs:94`
- ② 改前原文＝`        foreach (var row in eligible.OrderByDescending(x => x.RecordedSequence).ThenBy(x => x.EvidenceId))`
- ③ 改后原文＝`        foreach (var row in eligible.OrderByDescending(x => x.RecordedSequence).ThenByDescending(x => x.EvidenceId))`（次键方向反掉，不新增任何数字门槛）
- ④ `git diff --stat`＝` 1 file changed, 1 insertion(+), 1 deletion(-)`
- ⑤ 跑法整行＝`dotnet test tests/Pry.Core.Tests -c Release --results-directory "C:/Users/24782/Desktop/Pry_Project/.local-notes/wave28-1024/out/trx-GKR7" --logger trx`（未用 `--no-build`）
- ⑥ TRX：`ls *.trx | wc -l`＝1，文件名＝`24782_PHYTHLINK_2026-10-24_15_50_38.trx`；`<Counters total="736" executed="734" passed="733" failed="1" notExecuted="0">`
- ⑦ 红格（**恰 1 枚，与起跑前推导一致**）＝`Pry.Core.Tests.Hds006EvidenceIdentityRetrievalPrototypeTests.Retrieval_orders_eligible_rows_by_recorded_sequence_then_evidence_id`；消息＝`Assert.Equal() Failure: Strings differ  Expected: "42,43,44,41"  Actual: "43,42,44,41"`；红点＝`…Tests.cs:line 121`。同轮另三枚排序／截断／地板格全绿（它们的夹具序号不并列，次键方向不受影响）⇒ 本枚只摘次键这一道，不串味。
  ⇒ 顶掉 §五 第 9 条里那句"排序要红得改比较键、那是新增数字门槛的另一条路"——**那条理由不成立**：把 `ThenBy` 反成 `ThenByDescending` 是一行方向翻转、与门槛无关，本把就是这样打红的。
- ⑧ 还原命令＝`git cat-file blob HEAD:src/Pry.Core/Memory/Hds006UnifiedMemoryRetrieval.cs > /tmp/gkr7.cs && mv -f /tmp/gkr7.cs <该文件>`
- ⑨ 还原后 `git status --short` 行数＝**0**
- ⑩ 突变态 SHA-256＝`efe82ad9e451e62576982ccff5e024767d3ca9748655f92d3179d47af45613e1`（＝`../gk-probe/GKR7-order-tiebreak-flipped.cs`，起跑前合成）／还原后＝`4cd91256ea8f3c5b55d23fe550fcad3d5bc2f870d4d0f3089ca9ead32368104d`（＝`git cat-file blob 612423b:该文件`）
- ⑪ 当轮预检整段（`out/preflight-GK1.txt`）：`FOREGROUND: msedge pid=5424 class=Chrome_WidgetWin_1 style=0x16CF0000 rect=972,269,1532x896 screen=2560x1440 covers=False borderless=False`／`gpu_util=29% used=1635MB`／`free_memory_gb=10.2`／`VERDICT=BUILD_OK`
- 附记（照实登记，不涂改）：本轮控制台逐行日志**未归档**——`tee` 我写成了相对路径 `../out/run-GKR7.txt`，载体里没有那层目录，`tee` 报错但 `dotnet test` 照常跑完并写出了 TRX。凭据以 TRX 的 `<Counters>`＋失败节点名为准（§〇.9 十一字段没有"控制台日志"这一项，本把不把它当缺口，也不补造一份）。从 GK-R8 起改用绝对路径重定向，`run-GKR8/9/10/11.txt` 四份在盘。

#### GK-R8｜来源失效不点名（预检＝GK2 `BUILD_OK`，15:51:29）

- ① 文件:行＝`src/Pry.Core/Memory/Hds006UnifiedMemoryRetrieval.cs:86-87`
- ② 改前原文＝`                exclusions.Add(new Hds006RetrievalExclusion(row.EvidenceId,` ＋ `                    Hds006ExclusionReason.SourceInvalidated, 0));`
- ③ 改后原文＝（两行删除，`continue` 原样保留 ⇒ 行仍被跳过，只是不再说为什么）
- ④ `git diff --stat`＝` 1 file changed, 2 deletions(-)`
- ⑤ 跑法整行＝`dotnet test tests/Pry.Core.Tests -c Release --results-directory "…/out/trx-GKR8" --logger trx`（未用 `--no-build`；归档＝`out/run-GKR8.txt`）
- ⑥ TRX：1 枚＝`24782_PHYTHLINK_2026-10-24_15_51_41.trx`；`total 736 / executed 734 / passed 733 / failed 1`
- ⑦ 红格（恰 1 枚）＝`…Invalidated_source_is_named_in_the_readout_and_its_row_is_excluded_with_that_reason`；消息＝`Assert.Single() Failure: The collection was empty`；红点＝`…Tests.cs:line 96`。同轮 R2 那枚 Missing 格不响（它摘的是另一条分支上的 Add），其余 16 枚绿 ⇒ "缺失"与"已失效"确实是两道各自点名的出口，不共用沉默分支——与 §一.2 那句话同形。
- ⑧ 还原命令＝同 GK-R7 的写法（同文件）
- ⑨ 还原后 `git status --short` 行数＝**0**
- ⑩ 突变态＝`78461ae4d3d36f3469f69dda7577424130912182caab5dac816730ebb4ad25d1`／还原后＝`4cd91256ea8f3c5b55d23fe550fcad3d5bc2f870d4d0f3089ca9ead32368104d`
- ⑪ 预检整段：`FOREGROUND: QQ pid=28676 … covers=False borderless=False`／`gpu_util=8% used=1635MB`／`free_memory_gb=9.7`／`VERDICT=BUILD_OK`

#### GK-R9｜失效后正文没被清掉（预检＝GK3 `BUILD_OK`，15:52:26）

- ① 文件:行＝`src/Pry.Core/Memory/Hds006DerivedArtifactLedger.cs:92`
- ② 改前原文＝`            entry.Payload = RedactedPayload;`
- ③ 改后原文＝（该行删除 ⇒ 状态翻成 Invalidated、原因与版本照走，正文原样留着＝"内部状态改了、盘外还读得到原文"那种失效）
- ④ `git diff --stat`＝` 1 file changed, 1 deletion(-)`
- ⑤ 跑法整行＝`dotnet test tests/Pry.Core.Tests -c Release --results-directory "…/out/trx-GKR9" --logger trx`（未用 `--no-build`；归档＝`out/run-GKR9.txt`）
- ⑥ TRX：1 枚＝`24782_PHYTHLINK_2026-10-24_15_52_38.trx`；`total 736 / executed 734 / passed 730 / failed 4`
- ⑦ 红格（**恰 4 枚，与起跑前推导一致**）＝`Invalidating_one_derived_artifact_leaves_the_other_two_live_and_readable(struck: Summary／Vector／Cache)` 三条 `InlineData`（红点均 `…Tests.cs:line 189`）＋`Derived_invalidation_redacts_the_payload_and_names_the_reason_in_the_readout`（红点 `…Tests.cs:line 210`）；四条消息同为 `Assert.Equal() Failure: Strings differ  Expected: "<redacted>"`。`Invalidation_appends_exactly_one_observable_entry_naming_the_artifact` 不响（它数的是观察日志，不判正文）⇒ 一轮四枚红是同一道闸的四个下游读数，不是合轮。
  ⇒ §五 第 9 条那句"R4 已证同文件命中条件会响，但这枚单格的红未取"从今天起有了它自己的红。
- ⑧ 还原命令＝`git cat-file blob HEAD:src/Pry.Core/Memory/Hds006DerivedArtifactLedger.cs > /tmp/gkr9.cs && mv -f /tmp/gkr9.cs <该文件>`
- ⑨ 还原后 `git status --short` 行数＝**0**
- ⑩ 突变态＝`b6f13b3fc891dd26f515f2ca8ad13aab09bc43ac4056109a8f1a928111173403`／还原后＝`5d2ce9281acfcac636d9b718eba782962dc393fb178325ee7906e2627ae2fde5`
- ⑪ 预检整段：`FOREGROUND: PureRef pid=24824 class=Qt624QWindowIcon … covers=False borderless=False`／`gpu_util=10% used=1667MB`／`free_memory_gb=9.8`／`VERDICT=BUILD_OK`

#### GK-R10｜撤销的第一道闸（状态版本）恒不拦＝§五 第 9 条末段点名的 `:128`（预检＝GK4 `BUILD_OK`，15:53:30）

- ① 文件:行＝`src/Pry.Core/Memory/Hds006EvidenceStore.cs:128`
- ② 改前原文＝`        if (row.StateVersion != record.ExpectedStateVersion) return Hds006UndoOutcome.StateVersionMismatch;`
- ③ 改后原文＝`        if (row.StateVersion != row.StateVersion) return Hds006UndoOutcome.StateVersionMismatch;`（双条件 CAS 的第一道闸恒不成立）
- ④ `git diff --stat`＝` 1 file changed, 1 insertion(+), 1 deletion(-)`
- ⑤ 跑法整行＝`dotnet test tests/Pry.Core.Tests -c Release --results-directory "…/out/trx-GKR10" --logger trx`（未用 `--no-build`；归档＝`out/run-GKR10.txt`）
- ⑥ TRX：1 枚＝`24782_PHYTHLINK_2026-10-24_15_53_42.trx`；`total 736 / executed 734 / passed 732 / failed 2`
- ⑦ 红格（恰 2 枚）＝
  - `…Undo_against_a_row_that_moved_after_the_detach_is_refused_and_rewrites_nothing`，消息＝`Assert.Equal() Failure: Values differ  Expected: StateVersionMismatch  Actual: SourceVersionMismatch`，红点 `…Tests.cs:line 249`
  - `…A_refused_undo_keeps_the_recovery_record_available`，消息＝`Expected: StateVersionMismatch  Actual: Restored`，红点 `…Tests.cs:line 312` ⇒ **形状比消息更难看**：本该被拦的撤销真的把内容写了回去，还把唯一的恢复记录从清单里弹掉了（v4:281 后半句正面违反）。
  同轮 `Undo_whose_source_line_advanced_…` 不响（它期望的本来就是 SourceVersionMismatch，第一道闸失效后仍由第二道接住）、`After_a_successful_undo_…` 不响（两处版本本就对得上）⇒ 与 §三.0 的推导同形，本枚只摘第一道闸。
- ⑧ 还原命令＝`git cat-file blob HEAD:src/Pry.Core/Memory/Hds006EvidenceStore.cs > /tmp/gkr10.cs && mv -f /tmp/gkr10.cs <该文件>`
- ⑨ 还原后 `git status --short` 行数＝**0**
- ⑩ 突变态＝`6dd6855fa414c70d6b841d2d0c7d999666ced6e1ffcd52c9fd44de13c69150ce`／还原后＝`c5e5892d7f239d98b61389f9d70576354ad76e7e430f1c5ae09fc454269659b4`
- ⑪ 预检整段：`FOREGROUND: msedge pid=5424 … covers=True borderless=False`（副屏窗口，仪器按 v6 规则判 `BUILD_OK`；本把不改判据也不自行加严）／`gpu_util=8% used=1705MB`／`free_memory_gb=9.4`／`VERDICT=BUILD_OK`
- 附记：本轮构建临时打出 1 行 `warning CS1718: 对同一变量进行比较`（`Hds006EvidenceStore.cs(128)`，突变态独有，`out/run-GKR10.txt` 里 `warning` 行数＝1）；还原后该文件 SHA 回到基准，警告随之消失，不当产品面警告。

#### GK-R11｜入册时不携带身份（§一.1"记录即分行、身份随身"那一面的闸）（预检＝GK5 `BUILD_OK`，15:55:02）

- ① 文件:行＝`src/Pry.Core/Memory/Hds006EvidenceStore.cs:69`
- ② 改前原文＝`            Identity = draft.Identity,`
- ③ 改后原文＝（该行删除 ⇒ 行仍按标识入册，但 `RowState.Identity` 停在默认值 `Real`＝同一句话的另外两个身份在入册那一刻就被抹平）
- ④ `git diff --stat`＝` 1 file changed, 1 deletion(-)`
- ⑤ 跑法整行＝`dotnet test tests/Pry.Core.Tests -c Release --results-directory "…/out/trx-GKR11" --logger trx`（未用 `--no-build`；归档＝`out/run-GKR11.txt`）
- ⑥ TRX：1 枚＝`24782_PHYTHLINK_2026-10-24_15_55_16.trx`；`total 736 / executed 734 / passed 730 / failed 4`
- ⑦ 红格（**恰 4 枚，与起跑前推导一致**）＝`Same_sentence_under_the_three_identities_is_three_distinct_evidence_rows`（红点 `…Tests.cs:line 34`，`Assert.Equal() Failure` 落在 Fiction 那一行）＋`Retrieving_one_identity_never_carries_another(asked: Real, expectedId: 11)`／`(asked: Fiction, expectedId: 12)`／`(asked: Cognitive, expectedId: 13)` 三条（红点均 `…Tests.cs:line 54`；asked=Real 那条是"三条都回来了"，另两条是"一条都没回来"）。其余 14 枚绿：夹具里除这三身份外所有格都用 `Real` 入册，身份被抹平对它们不可见 ⇒ 一轮四枚同闸，不是合轮。
  ⇒ §五 第 9 条那句"本刀六枚额度里没有一枚能单独打掉'记录即分行'而不撞上已取过红的那道闸"补上了：**能，删掉入册时那一行就行**，红集＝`Same_sentence…`＋三条身份探针。
- ⑧ 还原命令＝`git cat-file blob HEAD:src/Pry.Core/Memory/Hds006EvidenceStore.cs > /tmp/gkr11.cs && mv -f /tmp/gkr11.cs <该文件>`
- ⑨ 还原后 `git status --short` 行数＝**0**；五枚交付文件 SHA 复验＝`a8f69840…`／`4cd91256…`／`5d2ce928…`／`c5e5892d…`／判据 `376af36c…`，逐字符等于 §八.2 那行基准
- ⑩ 突变态＝`97af5e7bee9be8c1b93f8aaf108cfe16af4fecaf203673e9de9f8844c4e408d2`／还原后＝`c5e5892d7f239d98b61389f9d70576354ad76e7e430f1c5ae09fc454269659b4`
- ⑪ 预检整段：与 GK4 同一枚 msedge 窗口、`covers=True`／`gpu_util=11% used=1687MB`／`free_memory_gb=9.7`／`VERDICT=BUILD_OK`
- 附记：本轮构建临时打出 1 行 `warning CS0649: 从未对字段"RowState.Identity"赋值`（突变态独有，`out/run-GKR11.txt`）；还原后消失。

五轮合计＝**11 枚红次、落在 6 个原先无红证的格上**（R7 1 枚、R8 1 枚、R9 4 枚、R10 2 枚、R11 4 枚；R11 里的三条身份探针是上一段 R1 已取过红的格，本把再拿一次＝同一格的第二双手）。守门人 `dotnet` 调用次数＝**5**（全部 `dotnet test` 定向，零解决方案级构建、零全量——按常驻纪律不为顶基线另起构建），预检 5 枚全部 `BUILD_OK`、0 次 HOLD，五份整段与五份 run 日志（GKR8–GKR11 四份）＋五份 TRX 落盘。

### 八.6 有红／无红的终账（覆盖 §三.2 末段与 §五 第 9 条）

- 有红证＝**17 枚**：lane 自取 11 枚（7 个方法名）＋守门人补取 6 枚（`Retrieval_orders_eligible_rows_…`、`Invalidated_source_is_named_…`、`Derived_invalidation_redacts_…`、`Undo_against_a_row_that_moved_…`、`A_refused_undo_keeps_the_recovery_record_available`、`Same_sentence_under_the_three_identities_…`）。
- 无红证＝**1 枚**＝`The_prototype_applies_no_retrieval_floor_the_existing_confidence_gate_would_have_applied`。它的红只存在于"往产品码里新增一个数字门槛"这个方向，而那个方向被工单 §六.2 与在册零改动清单禁掉；本把它**采成证据**登记＝"这条格是数值锁格，它的反证由接线那一把提供（把地板接上时它必须改绿，不许静默删格）"，与 `QA-TIP1019-02` 那族同形。**不撤格、不改判据、不为了让它响去造门槛。**
- 换手上：这 18 枚现在**都有第二双手**（守门人五轮每一轮都完整跑过 `Pry.Core.Tests` 的 736 个节点、判据字节一字未动）。旧欠 75 格与本把无交集，**照旧 75 格，不减**。

### 八.7 归位与顶账

- 落地＝按工单 §四 与第二十七波更正的**路径**通道（载体 `612423b` 的父是 `2d90650`，主树线尾已＝`948935f`，`merge --ff-only` 数学上不成立）：`git checkout 612423b -- <五枚路径>` ＋ `git commit --only -m "feat(HDS-006,wave28): …" -- <同五枚路径>`；他人 2 枚 `M ` 暂存项（1/1、63/0）与全部 `??` 前后各数一次，逐字节未变。
- 文档刀＝本报告＋`docs/plans/PROJECT_PLAN.md`（§4.2 HDS-006 行＋§9 entry）＋`docs/full-scope-sprint.md`（§0 大白话＋§3 刀序）＋本工单 §七 三处文字更正，与代码分刀提交。
- 顶账＝**1357／executed 1355／failed 0／跳过 2**（分项 Client 108／App 233／Api 280／Core **736**、Core executed 734）。出处与合法性：右端＝lane 收尾那四份原始 TRX 的 `<Counters>` 当场求和（本把 §八.2 逐份复核过），等式＝上在册 1339＋本刀 18；`src`／`tests`／`scripts`／`Pry.slnx`／`Directory.Build.props` 五面在落地铁与 lane 载体之间**零差异**（落地后当场 `git diff --stat 612423b HEAD -- <这五面>` 取空输出为凭），主树与线尾之间只有纯文档提交，故本把不为顶基线另起一次解决方案级构建；`measure-fixed-eval` 同场 `Catalog 76／Passed 1355／Failed 0／Other 2` 自洽。两条同名跳过按 `outcome="NotExecuted"` 逐字点名，`notExecuted` 计数器恒 0 不看。
- `main` 照旧 ref-only 快进（`merge-base --is-ancestor` 先确认是 fast-forward，全程不 switch／checkout／stash 主树）。

### 八.8 本把新发现，登记不扩范围（两条，都不在本刀工单里）

1. **产品类型名带着工单号**：四枚产品类型全部以 `Hds006` 前缀落在 `src/Pry.Core/Memory/`（`Hds006EvidenceKind`／`Hds006SourceLedger`／`Hds006UnifiedMemoryRetrieval`／`Hds006DerivedArtifactLedger`／`Hds006InMemoryEvidenceStore`）。以前的隔离原型用的是领域名（`Narrative*`、`Narrator*`），把验收编号焊进公开类型名会让接线那一把只能"沿用编号名"或"改名连带改判据"。本把**不改**（改名会碰判据字节、属于功能刀），押在接线刀一次做掉，并已在 §4.2 那行登记。
2. **身份词表与既有记忆链并行**：本片新建了三身份枚举，而 `docs/narrative-storage-contract.md` §7.2 已有 `CognitionKind`／`CognitionOrigin`；lane 在 §〇.1 明写"不复用其类型、只对齐语义，避免动他人字节"——这是正当的分刀理由，但**两套词表的合并是接线的必做项**，不合并就会出现"同一个认知在两条链上各有名字"。登记为未验证的第 10 条，押接线刀。
