# GeForce GTX 10 系列以后模型自动调优目录

更新日期：2026-09-07。用途：Pry 本地模型首次启动的保守预设，不是游戏性能排行。

## 判定规则

后端优先读取 NVIDIA 驱动报告的实际总显存和当前空闲显存；型号目录只在显存不可读时兜底。同名 Laptop GPU 因整机功耗不同不按名称强行提升档位。首次配置仍须经过短生成基准，低于 12 token/s 或容量不足才继续降档。

| 档位 | 判定（优先按当前空闲显存） | 首次上下文上限 | 定位 |
|---|---:|---:|---|
| 1 `entry` | 少于约 4.8 GiB | 4K | GTX 10/16 入门卡、4GB Laptop GPU |
| 2 `basic` | 约 4.8–不足 7.5 GiB | 8K | 6GB 卡 |
| 3 `balanced` | 约 7.5–不足 10.5 GiB | 32K | 主流 8GB/10GB 卡 |
| 4 `high` | 约 10.5–不足 19.5 GiB | 128K | 11GB、12GB、16GB 卡 |
| 5 `extreme` | 约 19.5 GiB 及以上 | 256K | 24GB、32GB 卡 |

阈值保留少量容差，因为驱动报告值可能略低于包装容量。上下文上限还会受用户设置与模型本身支持上限约束。显存被其他程序占用时按空闲显存降档，因此不会仅因用户购买了高端型号就盲目启动最高配置。

## 桌面 GeForce 型号记录

以下显存为 NVIDIA 公版或标准配置；斜杠表示官方存在多个容量版本。

| 系列 | 型号与标准显存 |
|---|---|
| GTX 10（Pascal） | GTX 1050 2/3GB；1050 Ti 4GB；1060 3/5/6GB；1070 8GB；1070 Ti 8GB；1080 8GB；1080 Ti 11GB |
| GTX 16（Turing） | GTX 1630 4GB；1650 G5/G6 4GB；1650 SUPER 4GB；1660 6GB；1660 SUPER 6GB；1660 Ti 6GB |
| RTX 20（Turing） | RTX 2060 6/12GB；2060 SUPER 8GB；2070 8GB；2070 SUPER 8GB；2080 8GB；2080 SUPER 8GB；2080 Ti 11GB |
| RTX 30（Ampere） | RTX 3050 6/8GB；3060 8/12GB；3060 Ti 8GB；3070 8GB；3070 Ti 8GB；3080 10/12GB；3080 Ti 12GB；3090/3090 Ti 24GB |
| RTX 40（Ada） | RTX 4060 8GB；4060 Ti 8/16GB；4070 12GB；4070 SUPER 12GB；4070 Ti 12GB；4070 Ti SUPER 16GB；4080/4080 SUPER 16GB；4090 24GB |
| RTX 50（Blackwell） | RTX 5050 8GB；5060 8GB；5060 Ti 8/16GB；5070 12GB；5070 Ti 16GB；5080 16GB；5090 32GB |

GT 1010/1030 不属于 GTX 产品线。TITAN、Quadro/RTX PRO、数据中心卡，以及地区或 OEM 未列入 NVIDIA 标准 GeForce 对比表的特供型号不纳入本清单；实际显存探测仍可为它们分档。

## Laptop GPU 型号记录

| 系列 | 型号与常见官方标准显存 |
|---|---|
| GTX 10 Laptop | GTX 1050 2/4GB；1050 Ti 4GB；1060 3/6GB；1070 8GB；1080 8GB |
| GTX 16 Laptop | GTX 1650 4GB；1650 Ti 4GB；1660 Ti 6GB（含 Max-Q 命名变体） |
| RTX 20 Laptop | RTX 2050 4GB；2060 6GB；2070/2070 SUPER 8GB；2080/2080 SUPER 8GB（含 Max-Q 命名变体） |
| RTX 30 Laptop | RTX 3050 4/6GB；3050 Ti 4GB；3060 6GB；3070/3070 Ti 8GB；3080 8/16GB；3080 Ti 16GB |
| RTX 40 Laptop | RTX 4050 6GB；4060/4070 8GB；4080 12GB；4090 16GB |
| RTX 50 Laptop | RTX 5050/5060/5070 8GB；5070 Ti 12GB；5080 16GB；5090 24GB |

Laptop GPU 的实际性能还受约 35–150W 的整机功耗范围影响；Pry 因此以空闲显存做首次容量判断，再用真实生成速度校正，而不把桌面版和移动版同名型号视为等性能。

## 资料来源与维护

- NVIDIA GeForce 桌面显卡官方跨代对比：https://www.nvidia.com/en-us/geforce/graphics-cards/compare/
- NVIDIA GeForce Laptop GPU 官方对比：https://www.nvidia.com/en-au/geforce/laptops/compare/
- NVIDIA RTX 30 Laptop 官方对比：https://www.nvidia.com/en-sg/geforce/laptops/compare/30-series/
- NVIDIA CUDA GPU 架构/Compute Capability：https://developer.nvidia.com/cuda/gpus
- NVIDIA Legacy CUDA GPU 清单：https://developer.nvidia.com/cuda/gpus/legacy

每次新增 GeForce 系列时更新本表与型号后备规则；新增显存容量不需要修改型号表即可被主逻辑识别。
