using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.Interfaces;
using SqlSugar;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Infrastructure.Repositories
{
    public class GridConfigRepository : IGridConfigRepository, ITransientDependency
    {
        private readonly ISqlSugarClient _db;

        public GridConfigRepository(ISqlSugarClient db)
        {
            _db = db;
        }

        public async Task SaveConfigAsync(GridConfig config)
        {
            config.UpdateAt = DateTime.UtcNow;
            var storage = _db.Storageable(new List<GridConfig> { config }).ToStorage();
            await storage.AsInsertable.ExecuteCommandAsync();
            await storage.AsUpdateable.ExecuteCommandAsync();
        }

        public Task<GridConfig?> GetConfigAsync(string strategyName)
        {
            return _db.Queryable<GridConfig>()
                .FirstAsync(x => x.Name == strategyName);
        }

        public Task<List<GridConfig>> GetAllActiveConfigsAsync()
        {
            return _db.Queryable<GridConfig>()
                .Where(x => x.IsEnabled)
                .OrderBy(x => x.UpdateAt, OrderByType.Desc)
                .ToListAsync();
        }

        public Task<List<GridConfig>> GetConfigsByTypeAsync(string strategyType)
        {
            return _db.Queryable<GridConfig>()
                .Where(x => x.IsEnabled && x.Name.Contains(strategyType))
                .ToListAsync();
        }
    }
}
