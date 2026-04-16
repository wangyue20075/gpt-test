using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Oc.BinGrid.Core.Abstractions;
using Oc.BinGrid.Domain;
using Oc.BinGrid.Domain.Interfaces;
using Oc.BinGrid.Domain.ValueObjects;
using Oc.BinGrid.Engine.Interfaces;
using Oc.BinGrid.Engine.Strategies;
using Oc.BinGrid.Engine.Strategies.Grid;
using Volo.Abp.Modularity;

namespace Oc.BinGrid.Engine
{
    [DependsOn(
        typeof(GridBinDomainModule)
    )]
    public class EngineModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var btcGrid = new GridSetting(
                Symbol: "BTCUSDT",
                InitialPrice: 65000m,  // 当前市价
                GridGap: 800m,         // 每跌 800 刀补一仓
                QuantityPerGrid: 0.01m,
                MaxGrids: 10,          // 最多补 10 手
                TakeProfit: 0.015m,    // 利润达 1.5% 开启追踪止盈
                CallbackRate: 0.003m,  // 高点回落 0.3% 卖出
                ReboundRate: 0.002m    // 触线反弹 0.2% 买入
            );

            // 重点：显式注册为 StrategyBase，并指定实现
            context.Services.AddSingleton<StrategyBase>(sp =>
            {
                return new GridStrategy(
                    sp.GetRequiredService<ILogger<GridStrategy>>(),
                    btcGrid,
                    sp.GetRequiredService<IExchangeGateway>(),
                    sp.GetRequiredService<IOrderRepository>(),
                    sp.GetRequiredService<IPersistenceChannel>(),
                    sp.GetRequiredService<IOrderMonitorService>()
                );
            });
        }
    }
}
