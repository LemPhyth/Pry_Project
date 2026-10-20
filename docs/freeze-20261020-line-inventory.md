# 冻结登记 `FREEZE-20261020`（守门人 · AI 对话运行架构换代前的收口快照）

- **本文件是什么**：作者 2026-10-20 按 `docs/decision-request-protocol.md` 的四段式拍了「甲」⇒ 守门人执行"三步收口"的第 2 步（第 1 步＝收在飞的，见 §6；第 3 步＝等新架构那一句）。它是一份资产清点＋欠账清点，不是结案词、不是状态词，不新增也不放宽任何验收判据本体。状态词与结案词照旧不由 Agent 写。
- **谁该读**：新架构开工时的任何写者（含守门人自己）。作者不需要读这一页，他读 `docs/full-scope-sprint.md` §0。
- **为什么必须写在这里而不是只写在对话里**：`PROJECT_PLAN.md` 与 `AGENTS.md` 不受跟踪，从可指认提交建出来的干净副本看不见它们；换架构那一轮极可能重开 worktree，那时这一份文件是唯一的桥。

## 0. 裁定逐字与它没覆盖的部分

- 作者 2026-10-20 的原始一句（逐字）：「**能不能，一次做完，或者说，一次派单后，将整个项目暂时结案，我又找到了新的架构，整个项目的AI对话的运行架构都得换掉**」
- 守门人给出的甲项原文（明写为推荐项）：「停 `RT-006` 的行为经验刀，保留 `PLAT-003` 备份刀但押到新架构定形之后」
- 作者的回答（逐字）：「**甲**」
- ⇒ 自此生效的三句：功能刀一张不再派；`RT-006`＝暂停；`PLAT-003`＝押后但保留认领权（该 Owner 不必重新排队）。
- 作者没答的两项，本角色不代拍：
  - **乙（给冻结点打 tag）未获许可** ⇒ 本树不打 tag、不 push、不打包，这三件仍是作者的明令区，Agent 一律不自己碰。
  - 丙（新架构替换面的最小一句：进程拓扑／对话循环与协议／Provider 与模型层）仍未答 ⇒ 下面 §4 的"墙与皮"分层是守门人按今日读码给的推测，不是裁定；丙落笔后以丙为准。

## 1. 冻结点（当场取的时点；后续一律以 git 为准，不许引用本节当现状）

| 面 | 冻结时的读数 |
|---|---|
| 线 | `codex/ui-main-window-redesign`，守门人写入本文件前的线尾＝`8a69785` |
| 主线 | `main` 已 fast-follow 线尾（`git rev-list --count main..HEAD`＝0）；无 tag、无 push |
| 在册门禁 | 988 通过／0 失败／2 跳过（总 990），分项＝Client 101／App 233／Api 248／Core executed 406 passed 406＋2 条环境跳过；来源＝QA 2026-10-20 从 `e82499b` 建干净 `--detach` 副本连采七次、七次分项逐位相同（`eval-QA-TIP1020-1020`）；固定场景 catalog 76 条、`suiteVersion 1.4.0` |
| 两条环境跳过 | 只能按名点名（`notExecuted` 计数恒为 0）：`CoreTests.Configured_sense_voice_model_can_run_offline`、`LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key` |
| 工作树（冻结时） | 另有 lane 的两份文件在索引里未提交（`tests/Pry.App.Tests/MemoryManagementSurfaceTests.cs`、`docs/dispatch-tip1020-02-memory-group-judgment.md`），另有未跟踪产物 `docs/character-learning-safety.patch`；三者均属他人名下，本角色不碰、不提交、不回退 |

一句必须写清的读法：各 lane 的自跑数（904、911、939、982、986、990…）都只是各自载体的自跑，在册数的改写权只在 QA。若有人引用记忆或旧报告里的 886／876／854，那批数字已过期。

## 2. 这次冻结"停"掉了什么（逐张单点名）

| 单 | 冻结后的状态词（守门人登记，非结案词） | 可逆性 |
|---|---|---|
| `docs/dispatch-rt006-behavior-outcome-from-clicks.md`（第 3 层·行为经验的取证来源） | 暂停·未开工：本单从未产生一行业务代码，暂停＝零损失；形状已按作者 2026-10-19 授权代拍在单内，重启不必重写工单 | 作者一句话即重启 |
| `docs/dispatch-plat003-backup-restore.md`（第 5 层·备份与恢复） | 押后·保留认领权：落点在 `src/Pry.Core/Memory/MemoryDatabase.cs` 与 SQLite 迁移登记，换运行架构不换数据库，这张单留得住。§2 那两件守门人修补（S1 本地档回环、S2 打包脚本形状门）已随合流轮 `2a76470` 入库、且 10-20 由 QA 从线尾复采结案（`QA-TIP1016-01`），与本单押后无关 | 作者一句话即恢复排队 |
| `docs/dispatch-tip1019-02-retrieval-floor-judgment.md`（第 2 层·检索地板 `.5` 的行为判据） | 照旧可认领：判据刀、零产品代码；它守的是"什么算她记得的事实"，属 Memory 层语义，不在本次要换的运行架构面上 | 若丙落在 Memory 检索面上，再就地更正 |
| `docs/dispatch-tip1019-03-client-route-judgment.md`（Windows Client 两个路由方法的客户端级判据） | 已落地、待归位：实现＝分支 `codex/tip1019-03-client-route` 的 `2954dbc`；守门人已在自己载体独立拆四向、每向恰好一条红，字节级还原通过；主线尚未含它 | 合流即可，无需重写 |
| `docs/dispatch-tip1020-02-memory-group-judgment.md`（App 记忆窗"分组"判据空转欠账） | 他人名下，本角色不动（该 lane 此刻正在写）。登记这一行只是为了：若新架构重写界面直连层，这条判据的落点文件可能变，欠账本身不消失 | 归该 lane |

## 3. 带得走的资产（换架构时这些不用重做，全部当场数到）

| 资产 | 数量与位置 | 为什么带得走 |
|---|---|---|
| 契约文本 | `docs/api-v1.md` | 端点语义、错误码、DTO 形状是需求说明书；换进程拓扑时它要么原样迁移要么显式作废，但里面的判据口径不会白写 |
| 固定场景对照表 | `docs/eval/scenarios.json`，76 条，`suiteVersion 1.4.0` | 场景是"用户视角她该做到什么"，与运行架构无关；`scripts/measure-fixed-eval.ps1` 读它 |
| 门禁脚本 | `scripts/` 共 6 只：`download-models.ps1`、`install-local-runtime.ps1`、`measure-fixed-eval.ps1`、`package-release.ps1`、`verify-repository.ps1`、`verify-window-close-paths.ps1` | 除 `package-release` 的产物形状外，全部是"从仓库外验证仓库"的仪器 |
| 历轮验收报告 | `docs/eval/reports/` 共 25 份（最新＝`2026-10-20-tip-e82499b-merged-tree-rolling-revalidation-and-four-knife-falsification.md`） | 它们是判据链的证据，不是代码；新架构开工时"谁验过什么"全靠它们顶账 |
| 工单本体 | `docs/dispatch-*.md` 共 9 张 | 每张自带事实表＋完成标准＋反向约束；那批"不许走哪几条捷径"是踩过的坑，与架构无关 |
| 许可与素材边界 | `LICENSE`（GPL-3.0-only）、`ASSETS_LICENSE.md`、`FAN_CONTENT_POLICY.md`、`docs/upstream-license-compatibility.md`、`licenses/` | 法律面，换架构一字不改；照搬白名单仍只有 MaiBot/MaiCore＋Satori |
| 判据规模（文件级度量） | 测试 `.cs` 共 147 个文件；`[Fact]` 标记＝Core 320／App 160／Api 194／Client 63（合计 737 处标记；9 个 csproj＝5 产品＋4 测试） | 注意一句防误读：`[Fact]` 标记数不等于在册 988（Theory 与 `InlineData` 会展开成多条执行用例）。跨架构迁移时只有 988 那个口径能顶账，标记数只用来回答"哪些文件存在" |

## 4. 随架构重写的界面（守门人按今日读码认的层，等丙落笔修正）

大概率是皮（换架构会重写，别把它当墙去守）：

- 内嵌后端宿主：本地 llama.cpp／llama-server 的启停、连接池键、健康探针与自愈重连（`0-6` 那条 lane 的落点）。
- Api 网关接线：`src/Pry.Api/Runtime/*` 的编排层（`CharacterIntelligenceGateway.cs` 一类），含轮次提交、取消语义、错误映射、事件流。
- 对话循环与协议：一轮从请求到投递的时序，以及"她到底说没上话"的判定接线（`DEF-P3SEAM-04` 的落点就在这层）。
- 界面里的直连调用：`PryMessengerWindow.axaml.cs`、`MainWindow.axaml.cs` 那两处删除入口一类"UI 直接拼 URL 打后端"的形状。
- `docs/api-v1.md` 的具体端点形状：语义可迁，路径与 DTO 大概率随协议重排；重排时要显式作废并留痕，不能默默改掉（"新接口必须有文档"这条规矩照旧）。

是墙（换架构也不该跟着动，动了要单独明令）：

- 记忆的事实语义与检索地板：`MemoryQueryService.cs:22` 的 `Confidence >= .5` 与 `Status == "active"`。它回答"什么算她记得"，与谁跑推理无关。**参数标定属第 6 层，本轮不许动那个数字。**
- SQLite 持久层与 schema 迁移登记（`MemoryDatabase.cs`；`SchemaVersion` 当前写死 10，没有按版本号的迁移登记）——`PLAT-003` 的落点，换运行架构后照样要有数据库。
- 表达／行为学习的证据入口规矩：只从「就是这样／别这样说」两下明按钮取证，不从上下文猜用户态度（作者 2026-10-15 原话承重，本条不因换架构解除）。
- 许可与素材边界、模型／运行库／密钥／日志／用户数据不提交、§8 五条发布前检查、§2.4 范围冻结。

## 5. 仪器坑（这批是本轮实测踩到的，换架构的人第一天就会撞上）

1. `dotnet test` 的结果目录只认长参 `--results-directory <绝对路径>`。短参 `-r` 会把路径拼到各程序集 `bin\Release\net10.0\` 之后，四个程序集一条测试都没跑仍然 `exit=0`，而输出过滤器会吞掉报错。唯一可信证据＝TRX 文件存在，加上 `<Counters>` 非零。
2. `measure-fixed-eval.ps1` 不带 `-ResultsDirectory` 也不带 `-CatalogOnly` 会 `throw`，而门禁脚本里 `$ErrorActionPreference='Continue'` ⇒ 那句只打成红字、整段仍 `exit 0`，还会照样打印 `Catalog valid: 76`。正确跑法＝`-ResultsDirectory <本轮 TRX 目录>`（它只读 `*.trx`，不写目录，可复用同一份 run 证据，不必重跑套件）。
3. 派生门禁脚本后必须先读回 `$out`／输出目录那一行再起跑。sed 替换漏改一次，差点覆盖带红例读数的 run1 证据；受损或作废的场记一律换目录换名重跑，不当反证也不引用。
4. `.gitattributes` 钉了 `scripts/**`＝eol=crlf、`**/*.cs`＝eol/lf。在多行正则里用 `\n` 锚点会在 `.ps1` 里恒不命中 ⇒ 一条"看起来在守、实际常绿"的门。源码扫描门必须带正向探针。
5. 共享工作树里绝不裸 `git commit`。别人的文件已在索引时，一次裸提交会把它们一起吃进去（本轮另一条守门人会话就发生过，靠 `git reset --soft HEAD^` 退回来）。正确形状＝`git commit --only -m "<msg>" -- <path…>`；顺序不能反：`--only -- <paths> -m msg` 会把 `-m` 当 pathspec，报 `error: pathspec '-m' did not match any file(s)` 且什么都没提交。
6. worktree 文件的 md5 与 `git show HEAD:<file>` 可以合法不同（换行与属性过滤）。要证明"载体与提交同一"只有两条路：`git status --short` 该文件零行，加上 `git diff HEAD --quiet -- <file>`；或 `git show <sha>:<path> | cmp -s -`。
7. `verify-repository.ps1` 那个数是"当前跟踪文件条数"，每落一份文档就往上走（本登记落地＝489→490）。它是顶账用的刻度，不是门槛，别读成"门禁变严或变松"。
8. 他这台机器的可用内存会被 ComfyUI 吃掉约 10 GB。门禁成片出现 SQLite `disk I/O` 或 `out of memory`＝资源形状，不是断言失败：读数记作废，绝不动他的进程。
9. 本仓库那一页作者只读的文档有一个 CommonMark 加粗坑：`letter**甲**letter` 这种两侧都是文字形体的标记开不了口，星号会原样印给他看（10-20 刚为此清过一轮）。写加粗前先确认标记外侧是标点或空格。

## 6. 冻结时还欠的动作（都不是本角色的，逐条点名）

1. Memory & Context lane：索引里那两份文件要么提交、要么撤回。不解决＝主线永远合不了任何东西（这是本轮唯一的技术性阻塞）。
2. 守门人（本角色，等第 1 件条件成立就做）：把 `2954dbc`（`QA-TIP1019-03` 的客户端判据）合进线，形状由并行那轮守门人已定＝`git merge` 保留 `2954dbc`，不用 `git rebase --onto`。合流后请 QA 从新线尾总复采。
3. QA：下一次落地后从线尾建干净 `--detach` 副本复采＋独立证伪（在册 988 的改写权在他们手里）。
4. 作者：① 丙——新架构替换面的最小一句（进程拓扑／对话循环与协议／Provider 与模型层），这是当前唯一"守门人不能替你写"的输入；② 乙是否补授予（给冻结点打 tag，本轮未答＝未打）；③ 照旧欠着的三件明令——托盘「退出…」那一次人工点击、tag／push／打包、各阶段结案词。

## 7. 解冻条件

作者任一句话即解冻，代码零损失——本次暂停的两张单从未落过一行业务码，`RT-006` 的形状与 `PLAT-003` 的 §2 两件修补都已入库或已结案。新架构方案单等的只有 §6 第 4 条那句丙；拿到之后守门人按同一格式出方案，不再往要拆的地基上派功能刀。
