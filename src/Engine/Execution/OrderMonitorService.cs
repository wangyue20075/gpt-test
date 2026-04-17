using Microsoft.Extensions.Logging;
using Oc.BinGrid.Domain.Interfaces;
using Oc.BinGrid.Domain.ValueObjects;
using Oc.BinGrid.Engine.Interfaces;
using System.Collections.Concurrent;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Engine.Execution;

/// <summary>
/// 订单监控服务：负责挂单的超时撤单、价格偏离撤单以及状态同步
/// </summary>
public class OrderMonitorService : IOrderMonitorService, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, OrderWatchTask> _watchTasks = new();
    private readonly IExchangeGateway _gateway;
    private readonly ILogger<OrderMonitorService> _logger;

    public OrderMonitorService(
        IExchangeGateway gateway,
        ILogger<OrderMonitorService> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    /// <summary>
    /// 注册一个订单进入监控列表
    /// </summary>
    public void Watch(OrderWatchTask task)
    {
        if (task == null || string.IsNullOrEmpty(task.OrderId)) return;

        _watchTasks[task.OrderId] = task;
        _logger.LogDebug("已挂载订单监控: {Symbol} | ID: {Id} | 价格: {Price}",
            task.Symbol, task.OrderId, task.OrderPrice);
    }

    /// <summary>
    /// [快速路径] 由行情驱动：毫秒级响应超时或偏离
    /// </summary>
    public async Task OnTickAsync(string symbol, decimal currentPrice)
    {
        // 这里的筛选是高效的，只处理当前变动币种的任务
        var tasksToProcess = _watchTasks.Values
            .Where(t => t.Symbol == symbol)
            .ToList();

        foreach (var task in tasksToProcess)
        {
            if (CheckCancelCondition(task, currentPrice, out string reason))
            {
                // 🚀 触发撤单：使用 _ = 异步执行，不阻塞行情管道继续处理后续 Tick
                _ = ExecuteCancelActionAsync(task, reason);
            }
        }
    }

    /// <summary>
    /// [兜底路径] 由定时 Worker 驱动：同步交易所真实状态，防止 WebSocket 丢包
    /// </summary>
    public async Task SyncOrderStatusAsync()
    {
        if (_watchTasks.IsEmpty) return;

        _logger.LogDebug("正在执行订单对账，当前监控中任务数: {Count}", _watchTasks.Count);

        // 遍历所有正在监控的订单（包括不同币种）
        foreach (var task in _watchTasks.Values)
        {
            try
            {
                // 1. 调用网关查询最新状态
                var response = await _gateway.GetOrderAsync(task.Symbol, task.OrderId);
                if (response == null) continue;

                // 2. 如果订单已经结束（成交、已撤销、已拒绝）
                if (response.IsFinalized)
                {
                    _logger.LogInformation("🔄 发现订单 {Id} 已终结，状态: {Status}", task.OrderId, response.Status);

                    // 移除监控
                    if (_watchTasks.TryRemove(task.OrderId, out _))
                    {
                        // 3. 通知策略实例：更新持仓、基准价并释放锁
                        await task.OwnerStrategy.OnOrderUpdateAsync(response);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("对账订单 {Id} 异常: {Msg}", task.OrderId, ex.Message);
            }
        }
    }

    /// <summary>
    /// 检查是否满足撤单条件
    /// </summary>
    private bool CheckCancelCondition(OrderWatchTask task, decimal currentPrice, out string reason)
    {
        reason = "";
        // 1. 超时检查 (针对限价单长时间不成交)
        if (DateTime.UtcNow - task.CreateTime > task.Timeout)
        {
            reason = $"超时(>{task.Timeout.TotalSeconds}s)";
            return true;
        }

        // 2. 价格偏离检查 (价格跑太远了，挂单已无意义，撤掉重新寻找机会)
        decimal deviation = Math.Abs(currentPrice - task.OrderPrice) / task.OrderPrice;
        if (deviation > task.MaxDeviation)
        {
            reason = $"价格偏离({deviation:P2})";
            return true;
        }

        return false;
    }

    /// <summary>
    /// 执行具体的撤单动作并通知策略
    /// </summary>
    private async Task ExecuteCancelActionAsync(OrderWatchTask task, string reason)
    {
        // 原子操作：确保撤单逻辑只触发一次
        if (!_watchTasks.ContainsKey(task.OrderId)) return;

        _logger.LogWarning("🚨 触发自动撤单 [{Reason}]: {Symbol} {Id}", reason, task.Symbol, task.OrderId);

        try
        {
            var success = await _gateway.CancelOrderAsync(task.Symbol, task.OrderId);

            // 无论撤单成功与否（可能撤单时恰好成交了），
            // 最终都要通过 SyncOrderStatusAsync 或此处进行状态闭环
            if (success)
            {
                if (_watchTasks.TryRemove(task.OrderId, out _))
                {
                    // 构造一个撤单成功的响应，通知策略释放 ActiveOrders 锁
                    var cancelResponse = new OrderResponse(
                        OrderId: task.OrderId,
                        Symbol: task.Symbol,
                        Status: "CANCELED",
                        Side: task.Side, // 建议在 Task 中记录 Side
                        Price: task.OrderPrice,
                        Quantity: 0,
                        ExecutedQty: 0,
                        ExecutedPrice: 0,
                        UpdateTime: DateTime.UtcNow
                    );

                    await task.OwnerStrategy.OnOrderUpdateAsync(cancelResponse);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行撤单动作失败: {Id}。将在对账循环中再次尝试。", task.OrderId);
        }
    }
}
