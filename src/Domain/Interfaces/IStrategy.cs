using Oc.BinGrid.Domain.Enums;
using Oc.BinGrid.Domain.ValueObjects;
using Oc.BinGrid.Domain.Values;

namespace Oc.BinGrid.Domain.Interfaces
{
    /// <summary>
    /// 策略核心契约
    /// 定义 Engine 或 StrategySupervisor 如何与策略交互
    /// </summary>
    public interface IStrategy
    {
        /// <summary>
        /// 策略唯一标识
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 策略名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 交易对
        /// </summary>
        string Symbol { get; }

        /// <summary>
        /// 当前运行状态
        /// </summary>
        StrategyState State { get; }

        /// <summary>
        /// 已处理 Tick 数量
        /// </summary>
        long TotalTicksProcessed { get; }

        /// <summary>
        /// 最后一次 Tick 时间
        /// </summary>
        DateTime? LastTickTime { get; }

        /// <summary>
        /// 启动策略（加载状态、恢复订单等）
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// 停止策略（安全停机）
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// 行情驱动入口
        /// </summary>
        Task OnTickAsync(TickData tick);

        /// <summary>
        /// 订单状态更新回调
        /// </summary>
        Task OnOrderUpdateAsync(OrderResponse order);

        /// <summary>
        /// 系统启动后恢复状态
        /// </summary>
        Task RestoreAsync();
    }
}
