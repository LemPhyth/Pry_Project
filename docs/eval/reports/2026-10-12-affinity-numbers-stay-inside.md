# A 方案落地：亲密与心情的数值刻度只许活在内部（`QA-OUTLET-01`）· 2026-10-12

- **Run ID**：`eval-QA-OUTLET-1012`
- **判定人**：QA / Evaluation（EVAL-001）
- **载体**：`6f47f34` 的独立 `--detach` worktree `.local-notes/qa-affinity-outlet-1012`。跑前跑后 `git status --short` 只有 1 行，且是我本轮新建的那个判据文件（仍未跟踪）。零产品代码改动。
- **本轮在做什么**：执行作者 2026-10-12 的裁定 A。原话两句：「如果真的有数值系统也不能出现在后端」＋「我想要的回复效果是经过长期的记忆磨合出来的用户喜欢的回复，而不是用一些数值去调整的回复」。派工行是我自己登记的 `PROJECT_PLAN.md` §4.2 `QA-OUTLET-01`。
- **结论先写在最前面**：
  1. **出门口从"今天干净"变成"过不去"**。新增 1 个判据文件、4 条命名用例，把原先**一条判据都没有**的两面钉住：① 对外契约类型上不得有以内部信号命名的数值成员；② `Pry.Api`／`Pry.Client`／`Pry.App` 三层源码里不得出现内部信号的名字；③ 真正拥有这些信号的 `Pry.Core/{Intelligence,Expression,Runtime}` 不得持有任何日志器（`ILogger`／`Microsoft.Extensions.Logging`）。提示词那一面 09-28 已有三条判据，本轮一字未动、复跑仍绿。
  2. **三条守卫逐个突变反证，各红一次**，且各自点名到具体位置（`RuntimeState.FamiliarityScore:Double`、`Pry.App/QaMutationProbe.cs`、`src/Pry.Core/Intelligence/QaMutationProbe.cs`），还原后重新构建再取读数全绿。都不是空转。
  3. **顺带查出旧判据的一处空档**（`QA-OUTLET-02`，Low，建议 Owner＝Character Intelligence）：M1 期间我把 `public double? FamiliarityScore` 加回卡片格式，09-28 那条守源头的判据**仍然是绿的**。它的属性类型扫描只认 `int`／`double`／`float` 三种裸类型，可空数值直接绕过。不是本轮该改的东西，已按"只转达、不代改"登记。
  4. **作者要的效果本轮没有因此变近一步**：挡路的仍是"长期记忆磨合"那条通道没有生产者（`RT-004-DRAIN` 与第 2 层的记忆刀），不是这些数值本身。A 方案只保证数值被关在家里，不保证她学会说话。

---

## 1. 先说人话（作者只要这一段就够了）

- **能做到**：她心里那些"熟到几分、信任到几分、心情多好"的数字，现在有判据拦着了——想印到提示词、界面上，或者塞进接口返回体、写进日志，测试会当场红，而且是点名红。
- **做不到**：本轮没有让她"越聊越懂你"更远一步。那条通道（她学到你的口癖、你的偏好）到今天**还是没有生产者**，数字只是把位置让出来了而已。
- **挡路一件事**：`RT-004-DRAIN` 那一刀还没开工——学到的东西照样落不了地，队列 64 条满即丢。
- **要拍哪件**：本轮不需要你拍任何判据。仍然欠你的是老三样：`CHANGELOG.md` 三条要不要合一个提交、`0-4` 打包的 merge/tag/push 顺序、裁定 ③ 的落笔。

## 2. 出门口盘点：五个面，各自的判据落点

作者要求的是"数值不许出门"，所以先把他能看见的出口逐个列出来，再逐个对上判据——**这一节的用途是让下一轮 QA 能复算，不是自证**。

| 出门口 | A 方案允许的形态 | 谁在守 | 本轮动作 |
|---|---|---|---|
| 系统提示词（模型读到什么） | 中文描述，不含数字 | `CharacterIntelligenceTests` 的三条（`:454`／`:472`／`:484`，09-28 落地） | 一字未动；M1 还原后与本轮 4 条同场复跑 = 5 绿 |
| 角色卡 JSON（存档与分享出去的文件） | 只留地点／文字心情／当前目标 | 同上 `:484` + 旧卡兼容那条 | 未动；但见 §5 的 `double?` 空档 |
| 界面（`Pry.App`／`Pry.Client`） | 无 | **本轮新判据 ②** | 三层源码扫描，M2 红过一次 |
| 对外返回体（`Pry.Contracts` 的线格式） | 无 | **本轮新判据 ①** | 反射扫全部导出类型的数值成员，M1 红过一次 |
| 日志（任何一层把数值打印出来） | 无 | **本轮新判据 ③**（外加 ② 兜住外层） | 拥有信号的那三层不得持有日志器，M3 红过一次 |
| `character_state` 表的 REAL 列 | **允许**（这就是"家里"） | A 方案的边界本身 | 未动，本轮刻意不加判据去禁它 |

## 3. 四条命名用例（新文件 `tests/Pry.Core.Tests/AffinityNumbersStayInsideTests.cs`）

- `The_scanner_sees_the_leaks_it_claims_to_see` — 扫描器自证。正向 6 条（契约字段、日志模板里的小写 `trust`、带引号的键名、缩进后的赋值、加后缀的 `FamiliarityScore`、小写开头的 `trustPercent`）必须命中；反向 4 条必须不命中（`ReplyFrequency`、`IdleForMs` 是触发层数字不是亲密刻度；`Mood` 是 A 方案明确允许的**文字**心情；`/// Familiarity 与 Trust 只活在内部` 这种解释规则本身的注释不得被判成泄漏）。
- `No_wire_contract_carries_a_numeric_affinity_field` — 反射 `Pry.Contracts` 全部导出类型的公开属性，`Nullable.GetUnderlyingType` 先剥可空，命中"数值类型 + 名字含信号（大小写不敏感）"就点名。
- `No_outward_layer_names_an_internal_affinity_signal` — 逐层扫 `src/Pry.Api`／`Pry.Client`／`Pry.App` 的 `*.cs`（排除 `bin`／`obj`），每个层入口都带 `Directory.Exists` 断言，扫描面收缩就红而不是静默常绿。
- `The_layers_that_own_the_signals_have_no_way_to_log_them` — `src/Pry.Core/{Intelligence,Expression,Runtime}` 三层不得出现 `ILogger`／`Microsoft.Extensions.Logging`。

两处刻意的设计选择，写清楚免得下一轮读错：
- **词表**只放 10 个内部信号名，不收 `Interest`／`Fatigue`／`Bond`／`Mood`：它们是英文常用词，挂在噪声上的门禁等于没有门禁；心情的五个量改由第三条整体兜住（打印它的那一层没有日志通道，就不需要穷举名字）。
- **子串命中即算**、不做词边界豁免：外层连"认识这个信号"都不允许，所以 `FamiliarityScore` 也得红。`app.manifest` 里的 `trustInfo` 不在 `.cs` 扫描面内，不构成误报。

## 4. 三条突变反证（逐条红，字节还原后重建再取读数）

| 突变 | 放回去的东西 | 结果 | 还原 |
|---|---|---|---|
| M1 | `RuntimeState` 上加 `public double? FamiliarityScore { get; init; }` | 判据 ① 点名转红：`Assert.Empty() Failure: Collection was not empty` / `["RuntimeState.FamiliarityScore:Double"]` | `cp` 回写 + `cmp` 字节同一，重新 `-t:Rebuild` |
| M2 | 新 `src/Pry.App/QaMutationProbe.cs`，代码里 `public static double ReadTrust()` | 判据 ② 点名转红：`["Pry.App/QaMutationProbe.cs"]` | 删除该文件（本轮新建，可精确复原） |
| M3 | 新 `src/Pry.Core/Intelligence/QaMutationProbe.cs`，代码常量 `"ILogger"` | 判据 ③ 点名转红：`["src/Pry.Core/Intelligence/QaMutationProbe.cs"]` | 删除该文件 |
| M4（附带） | 同 M1，但只跑 09-28 那两条旧判据 | **两条都绿** ⇒ 见 §5 的 `double?` 空档，非我名下、不代改 | — |

三次还原后重跑：`AffinityNumbersStayInsideTests` 4 绿，与 `The_card_format_carries_no_numeric_affinity_field_to_leak` 同场 5 绿。

## 5. `QA-OUTLET-02`（新发现，Low，建议 Owner＝Character Intelligence）

`tests/Pry.Core.Tests/CharacterIntelligenceTests.cs:490-491` 的源头守卫写成 `p.PropertyType == typeof(int) || typeof(double) || typeof(float)`。M1 实测：把它声称防住的那类字段用**可空**形式加回来，这条不红。也就是说"卡片格式上不得有可供打分的刻度"目前只挡住了裸 `int`／`double`／`float`，`double?`／`int?`／`long`／`decimal` 全在门外。

- **本轮为什么不顺手改**：那个文件是 Character Intelligence 名下的判据文本，AGENTS 与 §5.0 的单一写者约束下我只转达。
- **建议的修法**：把类型判断换成 `IsNumeric` 口径（含 `Nullable.GetUnderlyingType` 剥壳），或直接复用本轮新判据的辅助方法。
- **残留风险（要如实说）**：我名下两条源码/反射判据是**按名字**认的。一个改名叫 `Warmth` 的 `double?` 卡片字段能同时绕过新旧两条。彻底的形状是"卡片格式不得有任何数值类型属性"，但那是 B 方案的一部分（作者没选），不在本轮范围。

## 6. 读数（同一载体，全部当场采集，不引用历史数字）

- `dotnet build Pry.slnx -c Release -t:Rebuild` ⇒ **0 警告 / 0 错误**。
- 解决方案级 `dotnet test -c Release --no-build` **共 4 次跑法，全部 0 失败**，逐次分项相同：Client 92 / App 183 / Api 150 / Core 342 通过 + 2 跳过 ⇒ **767 通过 / 0 失败 / 2 跳过（总 769）**。
  - 前 2 次带 `--logger "trx;LogFileName=runA.trx"`：控制台四程序集全绿，但**TRX 被四个程序集覆盖成同一文件**，`measure` 只看见 Core（`Passed=342`、`exit=2`）。⇒ **这两次不计入正式配对读数**，只作为"无红例"的额外次数。
  - 后 2 次改用默认按次命名 + 各自独立结果目录（`runA`／`runB`）：四次 TRX 各 1 文件、无覆盖。
  - 那 2 条跳过仍是固定环境门 `Configured_sense_voice_model_can_run_offline` 与 `Configured_llama_server_requires_the_child_environment_key`。
  - 红例清单：**无**（4 次跑法零红；本轮三次突变的红都发生在还原之前的定向跑里，不在这 4 次之内）。
- `measure-fixed-eval.ps1` 对 `runA`／`runB` 各 **`exit=0`**（`Passed=767; Failed=0; Other=2`，目录仍 **75** 条永久场景）⇒ 本刀是**判据**，不是新场景，目录一字未动。
- `verify-repository.ps1` ⇒ **418** 个受跟踪文件通过（与该载体上轮基线相同；新文件未跟踪所以不在计数内，符合预期）。
- `git diff --check` 干净。`git status --short` = `?? tests/Pry.Core.Tests/AffinityNumbersStayInsideTests.cs` 一行。
- 数字对齐：767 = 763（`6f47f34` 的接缝基线）+ 本刀 4 条。

## 7. 反向约束自查（派工行里写死的"不许走哪几条捷径"）

| 约束 | 本轮 |
|---|---|
| 不改产品代码、不做 0~1→字符串的重构（那是没被选的 B 方案） | ✅ 零 `src/` 改动；三个突变文件已全部删除/还原 |
| 不代改 `docs/character-intelligence.md` 的不变量文本 | ✅ 未打开该文件的写路径 |
| 不放宽 09-28 的三条提示词判据 | ✅ 一字未动，且同场复跑绿 |
| 不把 A 方案读成"界面可以显示亲密数值" | ✅ 判据 ② 恰恰把界面钉死 |
| 不新增"主因必须是学习通道"这类判据 | ✅ 未写。那属于 `RT-004-DRAIN`／第 2 层记忆刀的出口 |
| schema 编号与 SQL | ✅ 零接触 |

## 8. 仪器自纠（同一个坑第二次记）

`--logger "trx;LogFileName=<固定名>"` 在**解决方案级**会让四个程序集覆盖同一个 TRX，`measure-fixed-eval.ps1` 于是只看得见最后写入的那一份，并且这次 `exit=2` 而不是红——**"退出码非 0"是这条链唯一的告警**。2026-09-28 已经记过一次同名坑，本轮又踩。往后的固定做法：跑全量一律 `--logger trx --results-directory <每次新建目录>`，不复用文件名。

## 9. 本轮同时登记的一条过程缺陷（`QA-ASKFORM-01`，Low）

作者 2026-10-12 那句话点出的不只是数值系统，还有一条流程问题："**你们没把话说成我听得懂的**"。我回查了 2026-09-28 那次递给他、换来"要的"二字的裁决请求，原话是：

> 留给你单独裁的一项：要不要连字段一起删（可另立一刀，不影响本刀）

主语是**字段与线格式**，四部分里最关键的"用户看得见的东西"整段缺席——那次请求违反 `AGENTS.md` 第 12 条与 `docs/decision-request-protocol.md`，责任在**提问侧**，不在拍板侧。本轮按作者的"可以"把这条登记成缺陷（不是重开那刀：删字段这件事本身与他的意图一致，A 方案已经确认过一次）。修法建议写进台账：递给作者之前先遮住所有内部编号与类型名，问"他还能说出自己在拍什么吗"；做不到就重写。该文件的现属主是守门人，QA 只转达、不代改。

## 10. 文件此刻在哪里，谁能动它

新判据**只存在于 QA 载体**：`.local-notes/qa-affinity-outlet-1012/tests/Pry.Core.Tests/AffinityNumbersStayInsideTests.cs`（未跟踪）。**我没有把它拷进主工作树** —— 主工作树是其他 lane 的在途面，多 4 条用例会让他们下一轮的解决方案级读数凭空 +4（§5.0"读数不得互顶"）。入库动作＝作者点头后由本角色把这 1 个文件放进 `tests/Pry.Core.Tests/`，在主树重跑一次全量取读数；本报告与 README 索引行在主树（`docs/eval/**`，同样未跟踪）。

## 11. 与 §5.0 的关系

不构成推进，也不改 Phase 1／2 的结案动作。本轮属 AGENTS 第 13 条"使用中暴露的问题当场回修"，范围严格是作者裁定的 A 方案本身（数值关在家里 + 补两面守卫），未扩功能。§5.0 指针不动。**未 stage、未 commit、未 push**；新判据文件与本报告都等作者点头再入库。
