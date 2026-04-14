using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.Enums;
using Oc.BinGrid.Domain.Interfaces;
using SqlSugar;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository, ITransientDependency
    {
        private readonly ISqlSugarClient _db;

        public OrderRepository(ISqlSugarClient db)
        {
            _db = db;
        }

        public async Task SaveAsync(EaOrder order)
        {
            var storage = _db.Storageable(new List<EaOrder> { order }).ToStorage();
            await storage.AsInsertable.ExecuteCommandAsync();
            await storage.AsUpdateable.ExecuteCommandAsync();
        }

        public Task<EaOrder?> GetByExchangeIdAsync(long exchangeOrderId)
        {
            return _db.Queryable<EaOrder>().FirstAsync(x => x.ExchangeOrderId == exchangeOrderId);
        }

        public Task<List<EaOrder>> GetActiveOrdersAsync(string strategyName)
        {
            return _db.Queryable<EaOrder>()
                .Where(x => x.StrategyName == strategyName)
                .Where(x => x.Status == OrderState.New || x.Status == OrderState.Submitted || x.Status == OrderState.PartiallyFilled)
                .OrderBy(x => x.CreationTime)
                .ToListAsync();
        }
    }
}
