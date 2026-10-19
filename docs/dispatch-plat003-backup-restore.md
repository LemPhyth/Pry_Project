# 工单 `PLAT-003-BACKUP-RESTORE`（第 5 层 · 备份/恢复/完整性/迁移注册表）＋同层两件守门人修补

- **状态词**：已派工·待认领（守门人 2026-10-19 最晚按作者授权开出；主单 Owner＝Core Platform／Persistence——历轮登记明写"schema 编号由其分配"；判定归 QA；结案词归作者）
- **授权来源（逐字）**：作者 2026-10-19「**我不想思考了，你就继续给我派工单，该提交的提交吧，早点做完之后再回来大修**」⇒ 第 5 层不再等逐层请示；本单纯文本里守门人代拍的范围即 `PROJECT_PLAN.md` §4.2 `PLAT-003` 行本体，**没有夹带任何新功能**。
- **载体基线**：开工当日重取线尾；在册读数＝886／0／2（Client 93／App 221／Api 185／Core 387+2skip），引用必写全四项。

## 1. 主单 `PLAT-003`：事实表（守门人 2026-10-19 当场读码）

| # | 事实 | 位置 |
|---|---|---|
| P1 | schema 版本是硬编码常量 `SchemaVersion = 10` | `src/Pry.Core/Memory/MemoryDatabase.cs:158` |
| P2 | 启动路径只有"读旧值→建表→写新值"一条直线（`:27`、`:145`），**没有按版本号的迁移注册表**；现存迁移步骤里能看到两条语句串在一次执行里没有事务包裹的形状 | `MemoryDatabase.cs:27,145,959-961` |
| P3 | 完整性检查只在记忆表迁移路径里手写了两句（外键检查＋integrity 失败抛），**启动面与发行面没有 `PRAGMA integrity_check` 的固定档位** | `src/Pry.Core/Memory/SqliteMemoryRepository.cs:137-140` |
| P4 | 备份／恢复：**整个 `src/` 零个** `Backup`／`Restore` 端点、服务或脚本（`CharacterStateService.RestoreAsync` 与 `ConfigurationApplicationService` 里的 `interactionBackup` 是"删卡失败回滚临时文件"，不是用户备份） | 当场 `grep -rn "Backup\|Restore" src/` 复核 |
| P5 | 义务来源：§2.4 的 1.0 清单含"导出/备份/恢复"；`PROJECT_PLAN.md` §2.3 迁移约束第 5 条要求**数据迁移必须保留角色、会话、记忆与设置** | 台账文本 |

### 主单完成标准

1. **备份/恢复有正式入口**：DTO＋验证＋错误语义＋`docs/api-v1.md` 文档位＋测试（AGENTS 第 5 条例子，一步不许缺）；备份物覆盖角色卡、设置、会话库、记忆库，恢复到隔离数据目录可用。
2. **迁移注册表**：`SchemaVersion` 从"改常量＋祈祷建表 IF NOT EXISTS 兜住"改为**按版本号的有序迁移表**，每条迁移自带事务与失败语义；从 `versionBefore` 到当前值的任意升级路径可测。
3. **完整性档位**：启动跑一次轻量 `PRAGMA integrity_check`（失败→明确错误面＋诊断导出路径，不许静默），备份与恢复各自完成后必查一次。
4. **"迁移保留四类数据"有判据**：真库升级前后角色／会话／记忆／设置四类计数与关键行逐字段对照的用例（§2.3 第 5 条从台账文字变成会红的测试）。
5. 零新阈值（`2-D` 门照旧）；不许夹带安装器/升级策略等第 5 层其余格——那是后续刀。

### 主单反向约束

- **不许**把备份文件写进 `runtime/`、`models/` 或任何被 `.gitignore` 保护以免泄密的目录语义里打擦边球（AGENTS 第 6 条：密钥/DB/用户导入内容不得提交——备份产物默认落用户数据目录，路径可配）。
- **不许**用"删库重建"冒充迁移；迁移失败必须可回滚或明示不可回滚（P2 那格欠的就是这句）。
- 恢复入口**不许**绕过会话令牌中间件；备份导出**不许**带出 `runtime.json` 或密钥。
- 单一写者：本刀写者＝Core Platform；`MemoryDatabase.cs` 与 `SqliteMemoryRepository.cs` 在 `2-C` 认领归位前不得同文件双写——**开工前先确认 `2-C` 那半边已提交或已归位**，否则本刀与它撞行，等归位完成再动。

## 2. 同层两件·守门人 lane 自认领（不占上单，各成一小刀）

- **S1（安全修补，10-19 只读审计的 Low 项）**：`ConfigurationApplicationService.cs:747-754` 的 `ValidateModelUrl` 只对 `openai-compatible` 强制"HTTPS 且非回环"，`local-llama` 档位可以填**远程 http**——本地后端名不副实，聊天内容会明文出境。修法＝`local-llama` 强制回环主机（`127.0.0.1`/`localhost`/`::1`），越界＝400＋错误语义＋文档一句话＋两条判据（填远程必红、回环保留必过）。零产品行为变更（正常用户本来就填本地地址）。
- **S2（`QA-TIP1016-01`，Medium，发行 lane）**：整条打包路径今天机械判据为零——删掉 `scripts/package-release.ps1:152` 的 `Assert-CleanWorkingTree` 调用，在册 886 条一条不红。修法＝给脚本自身加一道"形状门"（扫守卫调用存在、`sourceCommit` 仍来自 `git rev-parse HEAD`、缺运行库仍抛）＋为"发行可审计性"铸一条永久场景；**不需要也不许动产品代码**（原登记口径照旧）。

## 3. 认领与出判

上单认领＝本文件首行签名＋`PROJECT_PLAN.md` §4.2 `PLAT-003` 行状态词；S1/S2 由守门人在各自小刀入库时同场登记。出场三件套同 `RT-006-BEHAVIOR-OUTCOME-FROM-CLICKS` 工单 §5（§9 记录＋带分项的自跑读数＋QA 从 SHA 复采证伪）。
