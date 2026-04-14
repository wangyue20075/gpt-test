# binance grid trade

#### 介绍
币安网格交易策略。

#### 项目结构
```text
src/
├── Core/            # 通用基础能力
├── Domain/          # 领域模型、仓储接口、枚举和值对象
├── Engine/          # 网格策略与业务编排
├── Infrastructure/  # 交易所网关、存储实现与基础设施服务
└── Host/            # 启动入口与后台托管服务
```

#### 结构规范
1. 使用 `Directory.Build.props` 统一管理公共构建配置（目标框架、空引用、隐式 using 等）。
2. 使用 `Directory.Packages.props` 统一管理 NuGet 依赖版本。
3. 各模块 `*.csproj` 仅保留模块特有配置与依赖引用，减少重复。

#### 特性
1. 低买高卖
2. 固定交易数量
3. 适合震荡行情
4. 支持现货和 USDT 永续合约

#### 安装教程
1. xxxx
2. xxxx
3. xxxx

#### 使用说明
1. xxxx
2. xxxx
3. xxxx
