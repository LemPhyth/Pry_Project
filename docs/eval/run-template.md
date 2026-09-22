# QA 运行记录模板（非已执行结果）

Run ID / 日期 / QA / 候选版本 / commit / dirty snapshot manifest / 构建源 hash：待填写。
环境 / OS / .NET / 时区 / RuntimeSettings / Provider deadline：待填写。
角色卡 hash / 模型及权重 hash / Provider / Prompt & Context 版本 / 采样参数：待填写。
对照上一 Run ID / 是否改变模型、角色或数据：待填写。

## Coverage

逐场景 ID：层级（Unit / Integration / Runtime / Model / Desktop / Longitudinal）、Pass / Fail / Blocked / NotRun / Inconclusive / NotApplicable、实际证据、未覆盖约束、负责人。不要把用例数当场景通过率。

## New Scenarios

ID、可观察变化、Input、Initial State、Expected Decision、Expected Output Constraints、Forbidden Outcome；关联缺陷或脱敏反馈编号；现有永久场景保留情况。

## Regression Results

Build exit / warnings；各程序集 passed / failed / skipped；TRX 路径；故障重跑是否同一源快照。目录校验、仓库扫描、diff 检查。未完成构建不得引用旧程序集结果。

## Runtime Findings

实际状态序列；Timing P50/P95/max 和样本数；stale attempted/committed/displayed；pending 取消；Trigger→Candidate→Generate→Review→Send 的每个结点；恢复/重连可见历史。

## Behavior Findings

多轮问题链、过度解释/安慰、附和、DoNothing、acknowledgement、结束话题与推回请求是否合时宜；逐项证据，不只判内容真假。

## Expression Findings

身份锚点、关系阶段、Mood 对照；10轮重复、口癖冷却、严肃场景的表达选择；真实模型与规则测试分开。

## Memory Findings

| 模型/模式/场景组 | 查询数 | U Useful | F False | M Missing | U/(U+F) | U/(U+M) | 错误查询数/查询数 |
|---|---:|---:|---:|---:|---|---|---|
| 待评测 | NotRun | NotRun | NotRun | NotRun | N/A | N/A | N/A |

逐查询脱敏标注：queryId、scope、有效 expectedFactIds、输出 usefulFactIds、falseClaims（虚构/过期/已删/跨域/无关）、missingFactIds、evidenceIds。Memory CRUD 通过不得填成召回三项全部为 0。

## Critical / High / Medium / Low Issues

每项记录 ID / 严重度 / Product defect 或 Test defect 或 Coverage gap / 最短复现 / Expected vs Actual / 源文件与 hash / 影响 / Owner / 修复后固定回归 ID。推断和已验证事实分开。

## User Feedback Converted to Tests

反馈代号→脱敏合成复现→场景ID→失败断言→修复→下版复验→用户回访。没有反馈写“0；未收集”，不要把内部 QA 当作 early adopter。

## Release Blockers

当前结论：未评定 / 阻塞 / 可进入下一验收阶段 / 接受。列出必需而未验证的场景、已知 Critical/High、需要交接的 Owner。

## Deferred Eval

逐项 ID、原因、依赖任务、下次触发条件；1.0 必需能力不因延期变成 NotApplicable。说明真实模型、OS 强杀、备份恢复或纵向观察有没有执行。
