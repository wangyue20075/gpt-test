using Microsoft.Extensions.Logging;
using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.Enums;
using Oc.BinGrid.Domain.Interfaces;
using Oc.BinGrid.Domain.ValueObjects;
using Oc.BinGrid.Engine.Interfaces;
using Oc.BinGrid.Engine.Strategies;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Engine.Orchestrator
{
    public class StrategyRecoveryService : ITransientDependency
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IRepository<TradeOrder, string> _orderRepo2;
        private readonly IOrderMonitorService _monitor;
        private readonly ILogger<StrategyRecoveryService> _logger;

        public StrategyRecoveryService(
            IRepository<TradeOrder, string> orderRepo2,
            IOrderRepository orderRepo,
            IOrderMonitorService monitor,
            ILogger<StrategyRecoveryService> logger)
        {
            _orderRepo = orderRepo;
            _monitor = monitor;
            _logger = logger;
        }

        public async Task RecoverActiveTasksAsync(IEnumerable<StrategyBase> strategies)
        {
            _logger.LogInformation("正在扫描数据库以恢复未完成的挂单...");

            // 1. 获取所有逻辑上处于 PENDING/NEW 状态的订单
            //var pendingOrders = await _orderRepo.GetListAsync(o => o.Status == OrderState.Submitted || o.Status == OrderState.PartiallyFilled);
            var pendingOrders = await _orderRepo.GetOpenOrdersAsync("");

            foreach (var order in pendingOrders)
            {
                // 2. 匹配对应的内存策略实例
                var strategy = strategies.FirstOrDefault(s => s.Id == order.StrategyId);

                if (strategy != null)
                {
                    _logger.LogInformation("恢复策略 {Name} 的挂单监控: {OrderId}", strategy.Name, order.Id);

                    // 3. 重新绑定策略内部的 ActiveOrderId 锁
                    //strategy.RestoreAsync(order.Id);

                    // 4. 重新挂载到监控服务
                    _monitor.Watch(new OrderWatchTask
                    {
                        OrderId = order.Id,
                        Symbol = order.Symbol,
                        OrderPrice = order.Price,
                        CreateTime = order.CreateTime,
                        OwnerStrategy = strategy,
                        Timeout = TimeSpan.FromSeconds(60), // 或从订单扩展字段读取
                        MaxDeviation = 0.01m
                    });
                }
            }
        }
    }
}
