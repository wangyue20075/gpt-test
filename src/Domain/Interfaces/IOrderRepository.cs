using Oc.BinGrid.Domain.Entities;

namespace Oc.BinGrid.Domain.Interfaces
{
    /// <summary>
    /// 订单仓储：记录所有 EaOrder 的历史与状态
    /// </summary>
    public interface IOrderRepository
    {
        Task SaveAsync(EaOrder order);
        Task<EaOrder?> GetByExchangeIdAsync(long exchangeOrderId);
        Task<List<EaOrder>> GetActiveOrdersAsync(string strategyName);
    }
}
