using Microsoft.Extensions.Logging;
using Oc.BinGrid.Domain.Interfaces;
using Oc.BinGrid.Domain.ValueObjects;
using Oc.BinGrid.Engine.Interfaces;
using System.Collections.Concurrent;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Engine.Execution
{
    public class OrderMonitorService : IOrderMonitorService, ITransientDependency
    {
        private readonly ConcurrentDictionary<string, OrderWatchTask> _watchTasks = new();
        private readonly IExchangeGateway _gateway;
        private readonly IOrderRepository _orderRepo; // 注入仓储
        private readonly ILogger<OrderMonitorService> _logger;

        public OrderMonitorService(
            IExchangeGateway gateway,
            IOrderRepository orderRepo,
            ILogger<OrderMonitorService> logger)
        {
            _gateway = gateway;
            _orderRepo = orderRepo;
            _logger = logger;
        }

        public void Watch(OrderWatchTask task) => _watchTasks.TryAdd(task.OrderId.ToString(), task);

        /// <summary>
        /// [快速路径] 通过 Tick 数据检查超时和价格偏离
        /// </summary>
        public async Task OnTickAsync(string symbol, decimal currentPrice)
        {
            var activeTasks = _watchTasks.Values.Where(t => t.Symbol == symbol).ToList();
            foreach (var task in activeTasks)
            {
                if (CheckCancelCondition(task, currentPrice, out string reason))
                {
                    await ExecuteCancelActionAsync(task, reason);
                }
            }
        }

        /// <summary>
        /// [慢速路径] 主动对账：查询数据库 Pending 订单并向交易所同步状态
        /// 可由 Orchestrator 的定时心跳触发，或独立后台任务驱动
        /// </summary>
        public async Task SyncOrderStatusAsync()
        {
            // 1. 从数据库获取所有逻辑上的 Pending 订单 (作为补偿，防止内存丢失)
            // 注意：生产环境建议这里加缓存或只查内存中记录的任务
            foreach (var task in _watchTasks.Values)
            {
                try
                {
                    // 2. 调用交易所接口查询真实状态
                    var remoteOrder = await _gateway.GetOrderAsync(task.Symbol, task.OrderId.ToString());

                    if (remoteOrder == null) continue;

                    // 3. 如果状态发生变化（例如已成交或已取消）
                    if (remoteOrder.Status != "NEW" && remoteOrder.Status != "PARTIALLY_FILLED")
                    {
                        _logger.LogInformation("🔄 发现订单状态异步同步: {Id} -> {Status}", task.OrderId, remoteOrder.Status);

                        // 移除监控任务
                        _watchTasks.TryRemove(task.OrderId.ToString(), out _);

                        // 4. 同步给策略（策略会更新数据库并解锁状态位）
                        await task.OwnerStrategy.OnOrderUpdateAsync(new OrderResponse(
                            task.OrderId, task.Symbol, "", "CANCELED", task.OrderPrice, 0, DateTime.UtcNow));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "同步订单 {Id} 状态失败", task.OrderId);
                }
            }
        }

        private bool CheckCancelCondition(OrderWatchTask task, decimal currentPrice, out string reason)
        {
            reason = "";
            if (DateTime.UtcNow - task.CreateTime > task.Timeout)
            {
                reason = "Timeout";
                return true;
            }
            if (Math.Abs(currentPrice - task.OrderPrice) / task.OrderPrice > task.MaxDeviation)
            {
                reason = "Price Deviation";
                return true;
            }
            return false;
        }

        private async Task ExecuteCancelActionAsync(OrderWatchTask task, string reason)
        {
            _logger.LogWarning("🚨 发起撤单 [{Reason}]: Order {Id}", reason, task.OrderId);
            var success = await _gateway.CancelOrderAsync(task.Symbol, task.OrderId.ToString());
            if (success)
            {
                _watchTasks.TryRemove(task.OrderId.ToString(), out _);
                // 这里通常不需要手动模拟 CANCELED，因为撤单成功后 API 或推送会返回状态，
                // 此时调用 SyncOrderStatusAsync 或等待推送即可。
                // 但为了系统响应速度，模拟一个也行：
                await task.OwnerStrategy.OnOrderUpdateAsync(new OrderResponse(
                    task.OrderId, task.Symbol, "", "CANCELED", task.OrderPrice, 0, DateTime.UtcNow));
            }
        }
    }
}
