# 第 0 层 `0-3` 门禁复采 + 接缝守卫非空转 + `0-7` 判据 ⑤ 复判 · 2026-10-12

- **Run ID**：`eval-QA-P0GATE-1012`
- **判定人**：QA / Evaluation（EVAL-001）
- **载体**：**一个 SHA**。我从 `6f47f34`（`feat(runtime): connect the reply advisor and delivery report to character intelligence`）新建自己的 `--detach` worktree `.local-notes/qa-p0gate-1012/wt`，建完 `git status --short` **0 行**，本轮所有读数都出自这棵树。**没有复用守门人采样的 `.local-notes/p0-seam`**（同一批读数不能由两个角色在同一载体上重复声称，§5.0 判据「读数不得互顶」）。
- **本轮在做什么**：执行基准 `docs/full-scope-sprint.md` §3 第 0 层的 `0-3`（门禁两条地基刀的正式复采 ⇒ Phase 2 段② 的判定动作）与其后续刀 `0-7`（`DEF-P1WALK-01` 判据 ⑤ 的复判）。派工原话：守门人 §9 记录里的「① `0-3` QA 从 `6f47f34` 建干净载体复采并出正式报告号」。
- **结论先写在最前面**：
  1. **`0-3` 成立**：解决方案级**连续三次 763 通过 / 0 失败 / 2 跳过（总 765）**，三次分项逐位相同，零红例可列；`measure-fixed-eval.ps1` 三个结果目录各 `exit=0`；`-t:Rebuild` 构建三次各 **0 警告 / 0 错误**。⇒ 我签 **Phase 2 §8.1 段② 的门禁半边 = Pass**，`DEF-P1WALK-03`／`-04` 在可指认提交上结案。
  2. **`0-7` 成立**：`DEF-P1WALK-01` 判据 ⑤ 复判 = **Pass**，两侧同时零命中（界面 16 个文本元素、库侧 4 条消息行），**并且**我用四处突变证明拦住它的四条守卫都不是空转；守卫在真机上**真的开过火**（首轮模型输出被判为无可见答复 ⇒ 走沉默档，没有把草稿落到库里）。
  3. **接缝第一次在真机上出账**：`character_state` 1 行（`exchange_count` 从 1 走到 2）、`behavior_experience` 2 行。这是历轮走查从未有过的读数——以前这两张表恒为 0。
  4. **本轮新登记两条缺陷**：`DEF-P1WALK-07`（本地 4B 首轮冷启动时她一个字都没说，界面只剩一个 `……`，没有任何原因说明）、`DEF-P3SEAM-04`（那个 `……` 被当成一次"已投递的可学互动"记进了学习账本）。都不按结案阻塞看，Owner 见 §7。
  5. **我不写结案词**。§5.0 指针不动、Phase 1/2 的"结案"两字仍归作者与守门人；`0-4`（打包）要作者先给 merge/tag/push 的顺序。

---

## 1. 先说人话（作者只要这一段就够了）

- **能做到**：门禁的绿色读数现在**可信且可指认**——在一个 commit 上连跑三遍，763 条全绿、零红、跳过的还是原来那两条环境门。她这一层第一次有了真记忆：聊两轮，状态表和经验表各自长出了行。
- **做不到**：本地 4B 档**第一句话她没答上来**，屏幕上只有一个 `……`。第二句正常，而且答对了"你叫小王、周三要去复诊"。
- **挡路一件事**：那个 `……` 不只是难看——**她把它当成一次"我说完了、而且说成功了"的互动记进了经验账本**。学错了东西比不学更贵。
- **要拍哪件**：`0-4` 打包那刀的顺序（先把 `main` 落后与三个 `CHANGELOG.md` 条目处理掉，再谈 tag）。本轮不需要为它拍任何判据。

## 2. `0-3`：当场重采的门禁读数（不引用任何历史数字，包括守门人那一轮）

派工的反向约束逐条对账：

| 约束 | 本轮怎么做 |
|---|---|
| 不许引用混树数字充数 | 三跑全部在 `6f47f34` 的独立 `--detach` worktree 内；跑前跑后 `git status --short` 各 0 行 |
| `dotnet test` 必须带 `--logger trx`，四个程序集的 `LogFileName` 各自唯一 | 每跑一个**独立结果目录**（`g1`/`g2`/`g3`），四次 TRX 各 1 文件、无覆盖；TRX 用 `-LiteralPath` 读（`[1]` 后缀是通配符） |
| 每次读数一律 `-t:Rebuild` | 三次都是 `dotnet build Pry.slnx -c Release -t:Rebuild` 后再 `--no-build` 测试 |

| 跑法 | 范围 | 结果 |
|---|---|---|
| 全量 run 1 | 解决方案级 | **763 / 0 / 2（总 765）**＝Client 92、App 183、Api 150、Core 338+2skip |
| 全量 run 2 | 解决方案级 | **763 / 0 / 2**，分项与 run 1 逐位相同 |
| 全量 run 3 | 解决方案级 | **763 / 0 / 2**，分项与上两次逐位相同 |
| 覆盖测量 ×3 | `scripts/measure-fixed-eval.ps1`（用**载体自己那份**脚本） | 各 `exit=0`，`Passed=763; Failed=0; Other=2`，"Catalog valid: 75 permanent scenarios" |
| 仓库扫描 | `scripts/verify-repository.ps1` | `exit=0`，**418 份受跟踪文件通过** |
| 空白检查 | `git diff --check` | `exit=0`，无输出 |
| 临时目录 | `%TEMP%\pry-*` 计数 | 跑前 **264** → 跑后 **264**（净零增） |

**同场列出的全部跑法次数**（纪律要求，含突变跑）：解决方案级 **3 次** + 定向 `CharacterIntelligenceSeamTests` **4 次** + 定向 `ModelReplyFormatTests` **6 次** = **13 次测试调用**，另有 1 次走查前的独立 `-t:Rebuild` 全量构建（0 警告 / 0 错误）。**每一个红例名**：解决方案级三次**零红**；定向跑的红例全部是我突变出来的、且只红我点名的那几条（§4、§5 逐条列名）。两条跳过恒为 `CoreTests.Configured_sense_voice_model_can_run_offline` 与 `LlamaServerSecurityTests.Configured_llama_server_requires_the_child_environment_key`，**没有把任何红例转成 skip**。

加法关系我独立核对：上轮基线 753 + 接缝刀新增 10 条 = **763**，与三份 TRX 的分程序集读数一致。构建日志的中文摘要在 PS5.1 的 `*>` 重定向里会串码，所以我用 UTF-16 解码后核对：三份各 `0 个警告 / 0 个错误`、`warning CS`/`error CS` 命中数 **0**。

## 3. 两条地基刀在 `6f47f34` 上就是我 09-29 证伪过的那批字节

`0-3` 要结案的判据本体是我上轮证伪的东西，所以必须确认载体上的字节没换过。sha1 逐位比对（我 09-29 记录的 ↔ 本轮载体 `git hash-object`）：

| 文件 | sha1 | 一致 |
|---|---|---|
| `src/Pry.Api/Services/SseEventStream.cs` | `9649d610b55e6b137cd0dd7860593fea9f5a062c` | 是 |
| `tests/Pry.Core.Tests/SqlitePoolCleanup.cs` | `9d1eccef64c83f4e8ac814733ceab192fe4fa004` | 是 |
| `tests/Pry.Api.Tests/SqlitePoolCleanup.cs` | `32e0877bd284405f930c7ee13d27b877608b11b7` | —（09-29 未留档，本轮记为基线） |
| `tests/Pry.Api.Tests/EventStreamHeartbeatTests.cs` | `09193e4517d65e591094a29e5b61493a8a78fba6` | —（同上） |
| `tests/Pry.Api.Tests/ProcessWidePoolResetBoundaryTests.cs` | `7a38998e289e92c05452069713da606e61b2e099` | —（同上） |

另三条非数字证据：① 全树 `tests/**/*.cs` 里**进程级 `ClearAllPools()` 的调用式为 0 处**（只有那条源码门的常量拼接里出现）；② 唯一的并行锁仍是 `tests/Pry.App.Tests/AssemblyInfo.cs` 的 `DisableTestParallelization = true`，本轮没被放宽；③ 三跑 `%TEMP%\pry-*` 净零增，说明"只清自己那一条池"在真实并行下不漏句柄。

⇒ **`DEF-P1WALK-03`／`-04` 的状态词从 `已提交待验` 转为 QA 侧结案**（结案动作本身仍由守门人/作者落笔）。

## 4. 接缝守卫的非空转证伪（三处突变，载体内，逐字节还原）

`6f47f34` 那 10 条新判据里，我挑**承重的三处**逐个突变。每次：`cp` 备份 → 只改一处 → 定向跑 `CharacterIntelligenceSeamTests` → `cp` 还原 → `cmp` 逐字节一致 → `git status` 0 行。

| 突变 | 改在 | 应当红 | 实测红（点名） | 其余 |
|---|---|---|---|---|
| `A` 把交给模型的提示块丢掉 | `Advisor.AdviseAsync` 的 `PromptBlock:` 恒为 `null` | 提示块两处 | `The_block_she_hands_the_model_explains_the_turn_without_handing_over_any_number`、`The_block_she_decided_with_is_the_block_the_model_reads_before_writing_the_reply` | 8 绿 |
| `B` 让没投递成功的轮次也教学 | `ReportTurnEndAsync` 删掉终局档位判断 | 沉默/中断轮次 | `An_undelivered_turn_teaches_nothing` | 9 绿 |
| `C` 摘掉气泡记账 | 装饰器 `MessageDeliveredAsync` 不再抄正文 | 投递侧 | `A_delivered_turn_leaves_state_and_experience_where_the_product_left_nothing_before`、`Two_conversations_on_one_card_share_one_ledger_but_never_take_each_other_s_bubble` | 8 绿 |

`B` 的判别力值得单说：我只拆了"终局档位"那一半，`delivered.Length == 0` 那一半仍在，所以 `A_low_content_moment_she_chooses_to_skip_says_nothing_and_records_nothing` **不受影响**——两条各守一半，不是同一条断言的两次回声。还原后复跑 **10 / 0**（`D`）。⇒ **接缝的三条承重墙都有抓手**。

## 5. `0-7`：`DEF-P1WALK-01` 判据 ⑤ 的复判（守卫级）

派工原话是"QA 从可指认提交复跑走查 ①，界面与库侧同时零命中"，反向约束是**不许用"这一轮没复现"替代判据复判**。所以我把两半都做齐了：**守卫必须能红**（突变）＋**真机两侧零命中**（§6）。

对 `src/Pry.Core/TurnTaking/ModelReplyFormat.cs` 的四处突变（同一套 `cp`/`cmp` 纪律）：

| 突变 | 形状 | 实测红（点名） |
|---|---|---|
| `E1` 认不出时回落整段原文 | `OnlyInternalDraft` 改成 `PlainText, text` | `Unparsable_plan_never_falls_back_to_the_whole_raw_output`、`Shape_extraction_separates_plan_from_draft_from_prose`（14 绿） |
| `E2` 半截收尾标签不再算标签 | 删 `MatchName` 的裁断前缀分支 | `Half_opened_envelope_at_the_end_of_a_truncated_stream_yields_no_draft`（15 绿） |
| `E3` 超预算散文改成截断而非判空 | `ToVisiblePlainText` 取 `line[..max]` | `Prose_answer_over_the_message_budget_is_not_silently_truncated`（15 绿） |
| `E4` 第二条信封解析链 | 在 `src/Pry.App/` 放一份含标签的临时文件（不编译，只给扫描器看） | `No_other_layer_recognises_the_reply_envelope`（15 绿），删文件即回绿 |

`E1` 与 `E2` 各红一半 ⇒ "抽取失败不回落原文"这条不变量由两处独立守卫撑着；`E4` 顺带自证了那条归属门禁**看得见东西**（该用例内部还带一条正/负探针，门禁不会以"瞎了"冒充"干净"）。还原后重建复跑 **16 / 0**（`F`）。

⇒ **判据 ⑤ 复判 = Pass**：判据文本要求的"界面与库侧同时零命中"在 §6 成立，而支撑它的四条守卫**逐条被证明会红**。这不是"这轮没复现"。

## 6. 真机走查读数（注入法 A′，载体 = `6f47f34` 干净 worktree）

| 项 | 读数 |
|---|---|
| 模型档位 | **本地 llama-server**，`Qwen3-4B-RPG-Roleplay-V2-Q4_K_M.gguf`（2382 MB，CUDA / RTX 5070 Ti，ctx 32768）；另起 `qwen2.5-vl-3b-instruct-q4_k_m.gguf` 作视觉档。**未读取、未记录任何密钥** |
| 数据目录 | 新建 `.local-notes/qa-p0gate-1012/w12/walk`，两进程只靠环境变量 `Pry__DataDirectory` 共享。作者默认目录跑前跑后最新写入仍是 **2026-09-22 19:09**，`%LOCALAPPDATA%\PryCompanion\runtime.json` 全程 **False** |
| 拓扑＝发现（确认） | 后端 pid 43320 / 端口 5095 / 协议 1.0 / **schema 10** / 令牌长度 44（只记长度与 SHA-256 前 8 位 `CD720055`）；界面 pid 11632 自有 Listen 端口数 **0**；描述符 `pid` 全程是后端那一份 |
| ① 第一句 | 14:33:10 发出 → 14:33:29.7 计划落库 → 14:33:31.4 投递。**可见内容是占位符 `……`**（模型这轮没兑现出正文，走了沉默档）。`len(assistant)=2` |
| ① 第二句 | 14:38:05 发出 → 14:38:12.25 投递（**6.7 s**）：**"小王，您好！我帮您记录一下：您周三要去医院复诊。…"** ⇒ 答复内容证明她真的读到了第一句 |
| **库侧零命中** | `messages` 4 行，含信封标签（推理标签／答复信封／`"messages"`）**0 处**，逐字复读用户提示词 **0 处** |
| **界面零命中** | 末态 16 个 UIA 文本元素，含 `<`+标签名或 `{` 的 **0 处**（`PrintWindow(hwnd, hdc, 2)` 截图同场留档） |
| ②（顺带采，非本轮判据） | 硬杀界面后新进程 pid 42572 从同一目录读回**四条气泡、顺序正确**，房间标题预览是第一句用户输入，没带进陈旧的"发送中/重连"状态，界面 listeners 仍 0 |
| 学习侧首次出账 | `character_state` **1 行**：`exchange_count` 1→2、`state_version` 1→2、`familiarity` 0.132→0.144、心情三维有值；`behavior_experience` **2 行**：turn 4 `TopicContinuation`/`Ask`、turn 5 `DirectQuestion`/`ContinueTopic`，`outcome` 均 `Unknown`（反馈入口未接）；`memories` **0 行**、`behavior_pattern`／`expression_pattern` **0 行**（后者是设计：缺口 ④ `DrainAsync` 无生产者） |

**① 的判据怎么落**：按清单原文（"界面上出现属于这条输入的一句答复，且答复内容能体现它真的读到了这句话"），**第一句不成立、第二句成立**。我不把它折算成 Pass 也不折算成 Fail，而是登记为 §7 的 `DEF-P1WALK-07`：本轮 `6f47f34` 上 ① 的载体是第二句，第一句是占位符。与 09-30 第十一轮（同一档位、第一句即得答复、31.55–32.71 s）相比，差别在**首轮撞上了模型冷启动**（14:33:14 起服务、14:33:17 就绪，实际生成窗口只有约 6 s），n=1，不按系统性回归断言——但也不按"没复现"抹掉。

## 7. 本轮新登记（都不卡结案，Owner 已指名）

1. **`DEF-P1WALK-07`（Medium，Owner：Runtime / Provider 档位）**——本地档首轮请求落在模型服务冷启动窗口内，产出被判为"无可见答复"，界面只剩一个 `……`，**没有任何文案说明是她没说、还是后端没答**。用户可见后果：新开的对话第一句像没生效。推荐修法方向两条（冷启动预热 / 占位符给出可解释状态），**不在本轮裁定面**，按 `docs/decision-request-protocol.md` 需要作者拍时才提问。
2. **`DEF-P3SEAM-04`（Medium，Owner：Intelligence lane）**——那个 `……` 被 `ReportTurnEndAsync` 当成"已投递、有正文"的一次完成互动记进了账本：`behavior_experience` 第 1 行的 `turn_id` 正是占位符那一轮。用户可见后果：她从"自己没说上话"的一轮里学到"这样回应是有效的"，`exchange_count`/`familiarity` 也随之上涨。这是接缝自身判据的边界（`delivered.Length > 0` 把占位符算成了正文），不是 §4 突变能抓到的形状——**现有 10 条判据里没有任何一条区分"占位符"与"她真的说了话"**。
3. **`DEF-P3SEAM-01` 的真机佐证（不新登记）**——一条全新会话的**第一句**用户输入，落库时 `reply_plans.turn_id = 4`、`fence_epoch = 3`；第二句是 `turn_id = 5`、`fence_epoch = 7`。⇒ "一轮过两到三次闸"在生产路径上确实发生，不只是代码读出来的推断。该条仍按守门人 09-30 的登记待派工。

## 8. 判定表

| 项 | 判据 | 结论 |
|---|---|---|
| `0-3` 门禁复采 | 干净 worktree、连续 ≥2 次全绿、同场列出全部跑法与红例名 | **Pass**（3 次 763/0/2、零红、`measure` ×3 exit=0、`verify-repository` 418、`diff --check` 干净、`%TEMP%` 净零增） |
| `DEF-P1WALK-03` | 在飞游标在释放前结算，取消仍是取消 | **结案**（09-29 三处突变 + 本轮 sha1 逐位同一） |
| `DEF-P1WALK-04` | 每片测试只清自己那一条池 + 源码门 | **结案**（同上；本轮另证调用式 0 处、并行锁未放宽） |
| Phase 2 §8.1 **段② 的门禁半边** | QA 从可指认提交的干净 worktree 复跑门禁 | **Pass**（载体 `6f47f34`） |
| Phase 2 §8.1 段② 的真机半边 | §8 第 3 条真机证据 | 本轮顺带采到 ①（第二句）与 ②；**不折算成段② 真机半边结案**——③ 未在本载体复跑 |
| `0-7` `DEF-P1WALK-01` 判据 ⑤ | 界面与库侧同时零命中 | **Pass**（守卫级四处突变 + 真机两侧零命中 + 守卫真的开过火） |
| §5.0「并行连续 ≥2 次全绿」 | 同一可指认提交上 ≥2 次 | **读数成立**（3 次），**结案词不由 QA 写**；指针推进归作者/守门人 |
| `0-4` `RT-008-PKG` | — | **未开始**，前置是作者给 merge/tag/push 顺序与三条 `CHANGELOG.md` 入库 |

## 9. 本轮未做与刻意不做

- **③（断后端不卡死）未在 `6f47f34` 上复跑**：它的载体是 `6f01608`（第十一轮 ③a 1.68 s／③b 48.09 s 两次 ≤55 s）。本轮接缝刀改了宿主侧的投递与学习路径，但没碰 `Pry.Client` 的重连判据；如果要把 ③ 也搬到新载体上，那是一轮独立取证，我不混着声称。
- **`DEF-P3SEAM-02`／`-03` 仍开**：载体实读确认 `_scopes` 仍缓存 `Task`（首轮取消会永久污染该卡）、`RecordDeliveredBubble` 仍按"该会话最近一次被建议的那一轮"归属且**没有判据**。
- **不改任何产品代码、不 stage、不 commit、不 push、不动 §5.0 指针**。全部突变都在我自己的 `--detach` 载体里，跑完 `cmp` 还原、`git status` 回到 0 行。

## 10. 过程缺陷如实记

1. **一次假读数，我自己撞出来并重做**：`E4` 第一次跑用 `--no-build`，而当时的 dll 还是 `E3` 突变那一份 ⇒ 除了归属门禁之外多红了一条 `Prose_answer_over_the_message_budget_is_not_silently_truncated`。这正是台账里那条"中途改过码必须重新 build，`--no-build` 跑的是旧二进制"。**处置**：删探针 → 重建 → 复跑 16/0（`F`）→ 再放探针、对正确二进制跑（`E4b`）⇒ 只红归属门禁一条。第一次那次读数按作废处理，不引用为证据。
2. **一次界面脚本的仪器缺陷**：`w12_send.ps1` 把状态占位文案（`Pry 正在思考…`）误认成"新出现的答复文本"，15 秒静默窗口从 4.66 s 起算 ⇒ 采样在 19.71 s 提前退出，答复还没到就收工。改用"文本元素数量稳定 6 秒"的判据后拿到真实末态。**不影响产品结论**，但第一次那份末态不引用。
3. **描述符写入时机**：后端起来后 `runtime.json` 不是第一时间出现（`memory.db` 已建、端口已监听，描述符随后才落）。我的启动脚本因此在轮询里等它——**这是脚本的采样窗口，不是产品缺陷**（与第十一轮 `w9_api.ps1` 同一形状，已记过）。
4. **`taskkill /F /T` 在 Git Bash 下会被路径转换吃掉**（`/F` → `F:/`），本轮改用 `Stop-Process -Force`。走查用强制终止 ⇒ `runtime.json` 不会被删，这是 `/F` 的语义，**不是**清单第 2 节"优雅退出删文件"那条判据的证据；该子项本轮仍未手工验证。
5. **README 索引行与本报告都落在未跟踪/未提交面**：与 `QA-P2CLOSE2-01`／`QA-P3STATE-02` 同族——干净 worktree 看不到 QA 的判据文本与测量资产。等作者点头时，建议把 `docs/eval/**` 与三个 `CHANGELOG.md` 条目一起处理。
