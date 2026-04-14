using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.Enums;
using Oc.BinGrid.Domain.Interfaces;
using SqlSugar;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Infrastructure.Repositories
{
    public class PositionRepository : IPositionRepository, ITransientDependency
    {
        private readonly ISqlSugarClient _db;

        public PositionRepository(ISqlSugarClient db)
        {
            _db = db;
        }

        public async Task UpdateAsync(EaPosition position)
        {
            var storage = _db.Storageable(new List<EaPosition> { position }).ToStorage();
            await storage.AsInsertable.ExecuteCommandAsync();
            await storage.AsUpdateable.ExecuteCommandAsync();
        }

        public Task<EaPosition?> GetPositionAsync(string strategyName, string symbol, PositionSide side)
        {
            return _db.Queryable<EaPosition>()
                .FirstAsync(x => x.StrategyName == strategyName && x.Symbol == symbol && x.Side == side);
        }
    }
}
