using Microsoft.Extensions.Logging;
using Oc.BinGrid.Domain.Interfaces;
using Oc.BinGrid.Domain.ValueObjects;
using Oc.BinGrid.Engine.Interfaces;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Engine.Orchestrator
{
    /// <summary>
    /// 策略启动恢复数据
    /// </summary>
    public class StrategyRecoveryService : ITransientDependency
    {
        private readonly IOrderMonitorService _monitor;
        private readonly ILogger<StrategyRecoveryService> _logger;

        public StrategyRecoveryService(
            IOrderMonitorService monitor,
            ILogger<StrategyRecoveryService> logger)
        {
            _monitor = monitor;
            _logger = logger;
        }

        /// <summary>
        /// 执行完整的系统恢复流程
        /// </summary>
        public async Task RecoverAllAsync(IEnumerable<IStrategy> strategies)
        {
            var strategyList = strategies.ToList();
            if (!strategyList.Any()) return;

            _logger.LogInformation("🚀 启动全局策略恢复序列，目标策略数: {Count}", strategyList.Count);

            // 1. 触发策略内部恢复
            // 这里会调用 StrategyBase.RestoreAsync()
            // 内部逻辑：从 OrderRepo 加载挂单 -> 从 PositionRepo 加载持仓 -> 填充策略内存栈
            await Task.WhenAll(strategyList.Select(s => s.StartAsync()));

            // 2. 将挂单接管至监控服务 (OrderMonitorService)
            // 这一步必须在策略 Start 之后，因为 Start 才会填充 ActiveOrders 字典
            RegisterActiveOrdersToMonitor(strategyList);

            // 3. 强制执行冷启动对账补偿
            // 核心：查询交易所 API，校准停机期间发生的成交/撤单
            _logger.LogInformation("🔄 正在请求交易所 API 进行首轮状态对账...");
            await _monitor.SyncOrderStatusAsync();

            _logger.LogInformation("✅ 所有策略已完成状态接管并进入运行模式。");
        }

        /// <summary>
        /// 将策略已有的挂单重新挂载到监控管道中
        /// </summary>
        private void RegisterActiveOrdersToMonitor(List<IStrategy> strategies)
        {
            foreach (var strategy in strategies)
            {
                // 通过 StrategyBase 定义的 GetActiveOrders 获取已从 DB 恢复的挂单
                var orders = strategy.GetActiveOrders();

                foreach (var order in orders)
                {
                    _logger.LogDebug("接管策略 {Name} 的挂单监控: {OrderId}", strategy.Name, order.ExchangeOrderId);

                    _monitor.Watch(new OrderWatchTask
                    {
                        OrderId = order.ExchangeOrderId,
                        Symbol = order.Symbol,
                        Side = order.Side,
                        OrderPrice = order.Price,
                        Quantity = order.Qty,
                        CreateTime = order.CreateTime, // 保持原始时间以维持超时逻辑
                        Timeout = TimeSpan.FromSeconds(60),
                        MaxDeviation = 0.005m,
                        OwnerStrategy = strategy // 关联策略实例，确保回调正确
                    });
                }
            }
        }
    }
}
