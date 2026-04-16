using Oc.BinGrid.Domain.Entities;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Domain.Interfaces
{
    /// <summary>
    /// 订单仓储接口（支持多交易所、多策略、多币种）
    /// </summary>
    public interface IOrderRepository: IRepository<TradeOrder, string>, ITransientDependency
    {
        Task<TradeOrder?> GetByIdAsync(string id, CancellationToken ct = default);

        Task<IReadOnlyList<TradeOrder>> GetByStrategyAsync(
            string strategyId,
            CancellationToken ct = default);

        Task<IReadOnlyList<TradeOrder>> GetOpenOrdersAsync(
            string strategyId,
            CancellationToken ct = default);
    }
}
