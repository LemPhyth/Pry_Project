# 旧主链退役登记（废案、不进发布包、可构建快照）

登记时间：2026-10-23 16:33（当场 `date` 取时点）。登记人：守门人（架构／集成／发行）。
本文是本仓库旧主链退役的唯一登记处；任务状态仍记在 `docs/plans/PROJECT_PLAN.md` §4.2 的 HDS-010 行。

## 1. 作者裁定（逐字）

> 旧的链路丢到废案里面，但是构建时候不要把旧链路打包进去，保存一份旧链路最后可以构建的快照

同时生效的既有裁定（不撤销）：v4 计划 `docs/plans/Pry_HDS_Rewrite_Architecture_Plan_v4.md:86` 要求旧窗口与旧回合管理器「在新路径替代并完成依赖核对后退役，不在最初原型中顺手删除」；`:375` 要求旧算法专属测试替换时「逐项写清旧假设退役／产品语义继续，不能为了让总数保持绿色删除失败测试」；`docs/plans/PROJECT_PLAN.md:37` 要求角色、聊天、记忆与关系数据「通过明确迁移继承，不清空数据库重来」；`:41` 要求「同一实例始终只有一个引擎拥有写入和投递权，不自动回落旧链」。

## 2. 我的执行读法（这一段是读法，不是原话）

1. 旧主链转为废案：停止向旧链新增功能，新链（`src/Pry.Core/Narrative/`）是唯一的功能去向。
2. 发布产物里不出现旧链：`scripts/package-release.ps1` 的 `dotnet publish` 链（`src/Pry.App/Pry.App.csproj:16-19` 连带 `Pry.Core`／`Pry.Contracts`／`Pry.Client`／`Pry.Api`）产出的 DLL 里不得再有旧主链的类型与入口。
3. 摘链之前先把「旧链最后可构建的那一枚」锚成可恢复的快照（本文件 §3）。
4. 切换按 v4 `:86` 的口径排顺序：先完成逐文件依赖核对与新路径替代，再摘；不做「先把旧链删了再补新链」的中间态，因为那期间发布包里没有可行的对话路径，而 `PROJECT_PLAN.md:37` 的数据迁移继承也还没落地。
5. 一次切换、不搞双链灰度并行（作者本次没有要求灰度）。若他要灰度或要「旧链保留到某个版本」，一句话我就改本条并重开 HDS-010 的排法。

## 3. 快照锚点

- 引用名（本地分支，未推送）：`archive/legacy-chain-last-buildable`
- 提交全号：`d82cd2d182e3c433939588ac79f82f04cbeda151`
- 树对象：`a6d6e605f16c52eb806c05f787261eede21436aa`
- 该枚在主线上的位置：`codex/ui-main-window-redesign` 的线尾，`main` 已 fast-forward 到同一枚。

为什么这一枚算「旧链最后可以构建」，证据链两条，本轮零 `dotnet` 调用：

1. `d3fbac4` 那枚的干净 `--detach` 副本跑过一轮全量，读数 1244／0 红／2 条按名环境跳过（Client 108／App 233／Api 280／Core 623，executed 621），载体与 TRX 见 `docs/eval/reports/2026-10-23-hds003-narrator-prototype.md` 与 §9 同日 entry。
2. `git diff --stat d3fbac4..d82cd2d -- src tests scripts Pry.slnx Directory.Build.props` 为空，即从那一枚到本快照锚点，产品码、判据、脚本、解决方案与构建属性五面字节相同；其间的三枚提交（`def0faf`／`44e13d4`／`d82cd2d`）全是文档。

恢复读法（任选，都不需要网络）：

```
git -C C:/Users/24782/Desktop/Pry_Project worktree add --detach <空目录> archive/legacy-chain-last-buildable
```

或 `git checkout d82cd2d`。快照是分支引用，不是标签；本仓库的标签发布权在作者，Agent 不代打（在册禁止项：`git tag`／`git push`／实际打包）。

后续若仍有动到旧链的刀在这个锚点之前落地，退役那一把开工前必须先把引用前移到当时线尾，并在本节补一行「实际摘链提交的前一枚＝……」，引用与实际删除点保持相邻。

## 4. 不进发布包：这条要求怎么变成可验收

退役刀必须同时交出下列四项，缺一不算「按裁定做完」：

1. 逐文件处置表：旧链候选（`src/Pry.Core/Runtime/`、`TurnTaking/`、`Prompting/`、`Conversation/`、`Intelligence/` 旧决策段，`src/Pry.Api/Runtime/`、`Services/` 的旧会话编排段，`src/Pry.App/` 旧窗口与旧回合管理器）每一枚写「退役／被新链替代／宿主继续保留」三态之一，并给调用方证据。此表由 `docs/dispatch-hds-wave19-20261023.md` 出，零构建。
2. 打包形状门：一条会响的判据，检查 publish 产物目录内不存在旧链类型名（不是数源码，是数盘上产出）。按本仓库纪律，新守卫必须先被故意打红一次；它现在天然是红的（旧链仍在产物里），所以它随删除刀同轮落地，取「删除前红／删除后绿」两枚凭证。
3. 判据账重基线：删除会带走旧链专属测试，门禁总数必然下降。必须在同一轮把新总数、四项分项与「哪些格子随旧假设退役」逐格点名登记，并按 v4 `:375` 写清每一格是「旧假设退役」还是「产品语义继续、已由新链等价格接管」。禁止为了让总数保持绿色而留死格，也禁止用「反正删了」掩盖没接手的语义。
4. 数据继承：旧链表（`messages`、`memories`、runtime/proactive 等段）与新叙事表的迁移步骤要落。`docs/narrative-open-items-memo.md` 的 U-2／U-3／U-11 仍是这一项的硬依赖，未拍未装。

## 5. 本轮已完成与明确未完成

已完成（当场可证）：

- 快照引用建立，见 §3 的三条盘上读数。
- 已证的死入口一枚：`src/Pry.Core/Conversation/ConversationService.cs` 在 `src/` 与 `tests/` 内除自身声明外零引用；该事实早在 `docs/character-intelligence.md:123` 就登记过，本次用 `grep` 复算仍然成立。它是旧链「边流式边落助手消息」的第二位生产者（对照 `docs/eval/reports/2026-10-23-hds003-narrator-prototype.md:139`），删除它不改变任何用户可见行为，属退役刀的第一片。

未做／未验证（不许当成已做）：

- 旧链没有从 `src/` 移除，也没有从今天的发布包里移除。今天若出包，包内仍是旧链在跑收发。
- 新链没有任何生产调用方（`src/Pry.Core/Narrative/` 装配器与 `NarrativeRoundRunner` 零生产接线；`grep -rn "Pry.Core.Narrative" src/` 在 `Conversation/`、`TurnTaking/`、`Intelligence/`、`Pry.Api` 零命中），因此「替代」这一步今天没达成。
- 真实模型对照零读数（等作者一句「可以跑」）。
- §4 第 2、3、4 项全部未开始，按顺序押在处置表之后。
