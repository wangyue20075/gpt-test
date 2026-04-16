using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.Enums;
using Oc.BinGrid.Domain.Interfaces;
using Oc.BinGrid.Infrastructure.Db;
using SqlSugar;

namespace Oc.BinGrid.Infrastructure.Repositories
{
    public class OrderRepository : Repository<TradeOrder, string>, IOrderRepository
    {
        public OrderRepository(SqlSugarContext context) : base(context.Db)
        {
        }

        private ISqlSugarClient Db => base._db;

        /// <summary>
        /// 根据主键查询
        /// </summary>
        public async Task<TradeOrder?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            return await Db.Queryable<TradeOrder>().FirstAsync(x => x.Id == id);
        }

        /// <summary>
        /// 查询某策略全部订单
        /// </summary>
        public async Task<IReadOnlyList<TradeOrder>> GetByStrategyAsync(
            string strategyId,
            CancellationToken ct = default)
        {
            return await Db.Queryable<TradeOrder>()
                .Where(x => x.StrategyId == strategyId)
                .OrderBy(x => x.CreateTime, OrderByType.Desc)
                .ToListAsync();
        }

        /// <summary>
        /// 查询策略未完成订单
        /// </summary>
        public async Task<IReadOnlyList<TradeOrder>> GetOpenOrdersAsync(
            string strategyId,
            CancellationToken ct = default)
        {
            return await Db.Queryable<TradeOrder>()
                .Where(x => x.StrategyId == strategyId &&
                            x.Status != OrderState.Filled &&
                            x.Status != OrderState.Canceled)
                .OrderBy(x => x.CreateTime, OrderByType.Desc)
                .ToListAsync();
        }
    }
}
