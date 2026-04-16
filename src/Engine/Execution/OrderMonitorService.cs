using Microsoft.Extensions.Logging;
using Oc.BinGrid.Domain.Interfaces;
using Oc.BinGrid.Domain.ValueObjects;
using Oc.BinGrid.Engine.Interfaces;
using System.Collections.Concurrent;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Engine.Execution;

public class OrderMonitorService : IOrderMonitorService, ITransientDependency
{
    private readonly ConcurrentDictionary<string, OrderWatchTask> _watchTasks = new();
    private readonly IExchangeGateway _gateway;
    private readonly ILogger<OrderMonitorService> _logger;
    private readonly DateTime _serviceStartTime = DateTime.UtcNow;

    public OrderMonitorService(
        IExchangeGateway gateway,
        ILogger<OrderMonitorService> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    public void Watch(OrderWatchTask task)
    {
        _watchTasks[task.OrderId] = task;
    }

    /// <summary>
    /// [快速路径] 由行情驱动：毫秒级响应超时或偏离
    /// </summary>
    public async Task OnTickAsync(string symbol, decimal currentPrice)
    {
        // 过滤该币种的活跃任务
        var tasks = _watchTasks.Values.Where(t => t.Symbol == symbol);
        foreach (var task in tasks)
        {
            if (CheckCancelCondition(task, currentPrice, out string reason))
            {
                // 异步撤单，不阻塞 Tick 循环
                _ = ExecuteCancelActionAsync(task, reason);
            }
        }
    }

    /// <summary>
    /// [补偿路径] 主动对账：校准 WebSocket 可能丢失的状态更新
    /// </summary>
    public async Task SyncOrderStatusAsync()
    {
        if (_watchTasks.IsEmpty) return;

        _logger.LogDebug("正在执行订单对账，当前监控中任务数: {Count}", _watchTasks.Count);

        foreach (var task in _watchTasks.Values)
        {
            try
            {
                // 1. 查询交易所真实状态
                var response = await _gateway.GetOrderAsync(task.Symbol, task.OrderId.ToString());
                if (response == null) continue;

                // 2. 检查是否进入终态 (Filled, Canceled, Rejected...)
                if (response.IsFinalized)
                {
                    _logger.LogInformation("🔄 对账发现订单 {Id} 已结束，状态: {Status}", task.OrderId, response.Status);

                    _watchTasks.TryRemove(task.OrderId.ToString(), out _);

                    // 3. 这里的 response 是交易所真实的，包含成交价和状态
                    // 直接撞回策略逻辑，策略会自动释放锁并更新数据库
                    await task.OwnerStrategy.OnOrderUpdateAsync(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("对账订单 {Id} 异常: {Msg}", task.OrderId, ex.Message);
            }
        }
    }

    private bool CheckCancelCondition(OrderWatchTask task, decimal currentPrice, out string reason)
    {
        reason = "";
        // 1. 超时检查
        if (DateTime.UtcNow - task.CreateTime > task.Timeout)
        {
            reason = $"Timeout(>{task.Timeout.TotalSeconds}s)";
            return true;
        }
        // 2. 价格偏离检查
        decimal deviation = Math.Abs(currentPrice - task.OrderPrice) / task.OrderPrice;
        if (deviation > task.MaxDeviation)
        {
            reason = $"Price Deviation({deviation:P2})";
            return true;
        }
        return false;
    }

    private async Task ExecuteCancelActionAsync(OrderWatchTask task, string reason)
    {
        // 防止同一个任务被重复触发撤单
        if (!_watchTasks.ContainsKey(task.OrderId.ToString())) return;

        _logger.LogWarning("🚨 触发自动撤单 [{Reason}]: Order {Id}", reason, task.OrderId);

        try
        {
            var success = await _gateway.CancelOrderAsync(task.Symbol, task.OrderId.ToString());
            if (success)
            {
                // 撤单成功后从监控列表移除
                _watchTasks.TryRemove(task.OrderId.ToString(), out _);

                // 通知策略：订单已撤销（此处构造一个标准的 Response）
                await task.OwnerStrategy.OnOrderUpdateAsync(new OrderResponse(
                    OrderId: task.OrderId.ToString(),
                    Symbol: task.Symbol,
                    Status: "CANCELED",
                    Side: "",
                    Price: task.OrderPrice,
                    Quantity: 0,
                    ExecutedQty: 0,
                    ExecutedPrice: 0,
                    UpdateTime: DateTime.UtcNow
                ));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行撤单动作失败: {Id}", task.OrderId);
        }
    }
}
