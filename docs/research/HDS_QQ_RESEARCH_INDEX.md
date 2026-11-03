# 2026-10-20 研究补充索引：HDS-Interlude 与 qq-bridge

本包供 Pry 的 Architecture / Runtime / Intelligence / Memory / Platform / Client / QA 七个 Agent 阅读。内容是源码研究与本地隔离抽样验证，不是开发排期、复用授权或产品效果验收。

## 文档

- [HDS_INTERLUDE.md](HDS_INTERLUDE.md)：持续剧本驱动的 Roleplay；分析情景、人设、处境、记忆、回合与真实投递如何协作。
- [QQ_BRIDGE.md](QQ_BRIDGE.md)：外部 DeepSeek Harness 通过受限工具参与 QQ；分析主动读/发/等、消费水位、唤醒与授权边界。

将本包 `docs/research/` 中三个 Markdown 放入 Pry 同名目录即可。此索引使用独立文件名，不覆盖已有研究包 README、对比矩阵或其他项目结论。

## 两条路线对 Pry 的价值

| 观察 | HDS | qq-bridge |
|---|---|---|
| 生成中心 | 模型写连续生活，聊天是其中的行动 | Harness 中的模型以工具参与会话 |
| 初始角色感 | Canon、初始关系、场景与写作合约支撑 | 人格提示 + 社交 preset + 当前消息快照 |
| 行动方式 | 剧本提交、延迟意图、宿主发送 | 读、发、等、标读、设置唤醒工具 |
| 长期材料 | 原文、事实、关系、演化、意图与计划 | 会话状态、话题、未说念头、成员印象与黑话 |
| 核心可借鉴点 | 角色处境与表达的因果一致，叙事和真实送达分离 | 消费水位、等待语义、受限工具、唤醒恢复 |
| 不宜直接沿用 | 全局故事事实可见性、完整生活模拟与大型协调类 | 宿主签名密钥耦合、外部 Harness 强依赖与大型桥接类 |
| 首要读者 | Intelligence、Runtime、Memory | Platform、Runtime、QA |

两者不能替代此前所有项目的参考价值：MaiCore 的社交 Runtime、AstrBot 的 Task/Harness、Satori 的平台协议、Nekro 的 Code-as-Action/Sandbox、Private Companion 的宿主陪伴层，以及 DSH Pet 的桌宠呈现工程仍分别有自己的位置。

## 固定快照

| 项目 | 提交 | package 版本 |
|---|---|---|
| HDS | `1bfdc8b2b8d92bfe1e72542d4e0ff8d7ffefa090` | 1.0.1-rc57 |
| qq-bridge | `a56164e70a0521a7829138a42eee76066fde2b8b` | 0.2.0-r4 |

源码引用均固定提交，后续版本需要重新核对。

## 证据与验证

- HDS：15 个测试文件，最终共 118 passed、0 failed、4 skipped；跳过的是需要真实模型凭据的测试。独立检出的 typecheck 失败，缺少仓库外基础 tsconfig，细节保留在项目报告中。
- qq-bridge：4 个 Node test 文件 51 passed；另有 token-economy 脚本 36/36，以及 hardening/media-search 两个脚本通过。不同计数口径不混加。
- 测试不连接真实 QQ/DSH/模型。没有复现截图效果，也没有运行完整套件、安装流程或长期性能/费用测试。
- 未修改 Pry 产品代码，未复制上游源码、提示词、人设、美术或用户图片。两个项目均不在 Pry 当前直接源码复用白名单内。

## 使用方式

先读项目的定位与证据边界，再按各文档末尾的七角色表定向阅读。提取机制时要同时带走失败语义、作用域和未验证处，不要只引用效果结论或类名。

研究发现可以成为未来设计与对照实验的依据；是否实施仍依 Pry 已登记范围和任务决定。2026-10-19 允许大跨步开发不等于自动扩大产品范围。

