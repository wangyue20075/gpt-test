using Oc.BinGrid.Domain.ValueObjects;

namespace Oc.BinGrid.Engine.Interfaces
{
    public interface IOrderMonitorService
    {
        // 将订单加入监控队列
        void Watch(OrderWatchTask task);

        // 驱动监控逻辑（由编排器调用）
        Task OnTickAsync(string symbol, decimal currentPrice);
    }
}
