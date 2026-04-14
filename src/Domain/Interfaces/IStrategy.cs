using Oc.BinGrid.Domain.Values;

namespace Oc.BinGrid.Domain.Interfaces
{
    public interface IStrategy
    {
        string StrategyId { get; }
        string StrategyName { get; }
        bool IsRunning { get; }

        /// <summary>
        /// 初始化并启动策略
        /// </summary>
        Task StartAsync(CancellationToken ct);

        /// <summary>
        /// 停止策略并处理未完成订单
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// 接收行情推送的入口
        /// </summary>
        Task OnTickAsync(TickData tick);
    }
}
