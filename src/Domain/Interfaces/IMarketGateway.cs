using Oc.BinGrid.Domain.Entities;

namespace Oc.BinGrid.Domain.Interfaces
{
    public interface IMarketGateway
    {
        // 发送订单：非阻塞，仅返回“请求已接收”的本地状态
        Task<object> PlaceOrderAsync(EaOrder order);

        // 撤单
        Task<object> CancelOrderAsync(long exchangeOrderId);

        // 获取当前市场快照 (用于行情初始化)
        Task<decimal> GetLatestPriceAsync(string symbol);
    }
}
