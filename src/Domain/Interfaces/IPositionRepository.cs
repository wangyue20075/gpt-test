using Oc.BinGrid.Domain.Entities;

namespace Oc.BinGrid.Domain.Interfaces
{
    /// <summary>
    /// 持仓只读仓储接口（读模型）
    /// 写入统一走 PersistenceChannel
    /// </summary>
    public interface IPositionRepository
    {
        /// <summary>
        /// 根据主键查询
        /// </summary>
        Task<GridPosition?> GetByIdAsync(
            string id,
            CancellationToken ct = default);

        /// <summary>
        /// 查询某策略当前持仓
        /// </summary>
        Task<GridPosition?> GetByStrategyAsync(
            string strategyId,
            CancellationToken ct = default);

        /// <summary>
        /// 查询某交易对当前持仓
        /// </summary>
        Task<GridPosition?> GetBySymbolAsync(
            string symbol,
            CancellationToken ct = default);

        /// <summary>
        /// 查询所有未关闭持仓
        /// </summary>
        Task<IReadOnlyList<GridPosition>> GetOpenPositionsAsync(
            CancellationToken ct = default);

        /// <summary>
        /// 查询某策略历史持仓
        /// </summary>
        Task<IReadOnlyList<GridPosition>> GetHistoryByStrategyAsync(
            string strategyId,
            CancellationToken ct = default);
    }
}
