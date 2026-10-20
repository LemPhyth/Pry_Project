# 工单 `QA-TIP1019-03`（第 0／2 层交界 · `Pry.Client` 两个新路由方法的客户端判据）

- **状态词**：已派工·待认领（守门人 2026-10-20 按作者授权落成白纸单；Owner＝Windows Client；判定归 QA 从可指认提交复采，结案词归作者）
- **严重度**：Low，**不卡任何结案**——这两个方法今天由 Api 侧与界面侧覆盖，功能没有坏；缺的是**归因速度**。
- **载体基线**：开工当日先重取线尾。在册读数＝**988／0／2（总 990）**，分项＝Client 101／App 233／Api 248／Core executed 406 passed 406＋2 条环境跳过（`eval-QA-TIP1020-1020`）。**Client 那 101 条含换后端自愈那把（`2711c2f`）加的客户端用例，与本单无关**，别把它读成"本账已被别人补掉"。
- **授权来源（逐字）**：作者 2026-10-19「**我不想思考了，你就继续给我派工单，该提交的提交吧，早点做完之后再回来大修**」。
- **刀的体量**：**判据刀，零产品代码**，预计 1 个既有测试文件内 2 条用例（复用文件里现成的 `CaptureHandler`）。

## 1. 事实表（守门人 2026-10-20 当场重读码，行号当日有效）

| # | 事实 | 位置 |
|---|---|---|
| F1 | `DeleteConversationAsync(string id, bool forgetExclusiveMemories = false, …)` 拼 `api/v1/conversations/{Escape(id)}?forgetExclusiveMemories=` ＋ `.ToString().ToLowerInvariant()` | `src/Pry.Client/PryBackendClient.cs:111-112` |
| F2 | `ConfirmKeepMemoryAsync(long id, string characterId, …)` 拼 `api/v1/memories/{id}/confirm-keep?characterId={Escape(characterId)}`，`HttpMethod.Post`、无请求体 | `src/Pry.Client/PryBackendClient.cs:187-188` |
| F3 | 客户端测试目录里今天**没有**任何用例指向这两个方法：`tests/Pry.Client.Tests/**` 的源码命中为零（只有编译产物里的 dll）；同 lane 的 `MemoryManagementClientTests` 只有一个 `[Fact]`，覆盖的是 `GetMemoryDetailsAsync` 的 `characterId` 转义与 `ClearMemoriesAsync` 的 `confirm` 体 | `tests/Pry.Client.Tests/MemoryManagementClientTests.cs:12-24` |
| F4 | 10-19 登记时的原话：`ConfirmKeepMemoryAsync`、`DeleteConversationAsync(…, forgetExclusiveMemories)` 只有 Api／界面覆盖，Client 计数本刀前后都＝93（当时口径） | `docs/dispatch-rt005-memory-management.md` §8 |
| F5 | 现成仪器就在同一个文件里：`CaptureHandler` 记录 `Uri` 与 `Body`，无需新建工装 | `tests/Pry.Client.Tests/MemoryManagementClientTests.cs:25+` |

## 2. 完成标准（出判面，逐条可判）

1. **两条纯 Client 用例**，各钉一头：
   - `confirm-keep`：断言 URI 恰为 `/api/v1/memories/{id}/confirm-keep?characterId=…`，且 `characterId` 里含 `&` 一类字符时按 URL 转义（形状照 F3 那条既有用例的 `a&b → a%26b`），方法是 `POST`、请求体为空。
   - `delete conversation`：断言查询串里 `forgetExclusiveMemories` 的值为**小写** `true`／`false`（两个方向各断一次），并且会话 id 走转义而不是裸拼。
2. **先红一次**：把 F1 的 `.ToLowerInvariant()` 摘掉 ⇒ 第二条必须红；把 F2 的 `Escape(characterId)` 换成裸插值 ⇒ 第一条必须红。逐字节还原后 `src/` 的 `git diff` 零行。
3. **门禁面**（口径见 §4）：`-t:Rebuild` 0/0；连续两次全绿、分项逐位相同；`measure-fixed-eval` 各 `exit=0`、catalog 仍 **76**；`verify-repository` 通过；`git diff --check` 干净。
4. **登记面**：`CHANGELOG.md` 不加条目（行为不变）；不铸新永久场景；§9 写清突变方向与红例名。

## 3. 反向约束（开工前逐条读）

1. **不许改产品码迁就测试**——`.ToString().ToLowerInvariant()` 那个写法是有意的（后端按小写解析布尔），不许为了少写一条断言把它换成三元或 `Json` 序列化。
2. **不许用源码扫描形状门**（`Assert.Contains("confirm-keep", source)` 之类）当判据——那是 `QA-TIP1020-02`／`QA-TIP1015-01` 那一族，本仓库已经两次证明它守不住本体。判据必须跑真的 `HttpClient` 管线（现成 `CaptureHandler` 即可）。
3. **不许新造第三套 URL 拼装**，也不许把两个方法合并成通用 helper；本单只加测试。
4. 单一写者：只动 `tests/Pry.Client.Tests/` 内自己名下文件；`src/Pry.Client/PryBackendClient.cs` **只读**（该文件是 10-19 双 lane 撞过行的热点）；`MainWindow.axaml.cs` 一行不碰。
5. **不许把 `unauthorized` 改成可重试**或顺手碰重连语义——那是 `0-6`／`PLAT-002`／`PLAT-006` 的禁行判据本体。
6. 文档与代码分刀；未 push、未 tag、未打包。

## 4. 读数口径

同 `docs/dispatch-tip1019-02-retrieval-floor-judgment.md` §4：长参 `--results-directory` 绝对路径（短参 `-r` 会让四个程序集零测试仍 `exit=0`）、每程序集唯一 `LogFileName`、跳过按 `<UnitTestResult outcome="NotExecuted">` 点名、引用总数必写全四项分项、不做加法、绝不动他人进程与结果目录。

## 5. 归位

落地后由守门人在合流树重跑门禁并登记（§4.2／§9 ＋ `docs/full-scope-sprint.md` §0），`main` 跟线尾；QA 从当时线尾复采时把这两条一起拆。本单结案词不由本角色与本 lane 写。
