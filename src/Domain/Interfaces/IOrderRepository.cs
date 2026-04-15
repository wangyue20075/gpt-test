using Oc.BinGrid.Domain.Entities;

namespace Oc.BinGrid.Domain.Interfaces
{
    /// <summary>
    /// 订单仓储接口
    /// </summary>
    public interface IOrderRepository
    {
        Task InsertAsync(TradeOrder order);
        Task<List<TradeOrder>> GetRecentOrdersAsync(int count);
    }
}
