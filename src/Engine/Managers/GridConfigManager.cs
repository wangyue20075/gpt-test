using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.Interfaces;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Engine.Managers
{
    public class GridConfigManager : ITransientDependency
    {
        private readonly IRepository<GridConfig, long> _configRepo;

        public GridConfigManager(IRepository<GridConfig, long> configRepo)
        {
            _configRepo = configRepo;
        }

        public Task<List<GridConfig>> GetGridConfigsAsync() => _configRepo.GetListAsync();

        public Task<GridConfig?> GetGridConfigByIdAsync(long id) => _configRepo.GetByIdAsync(id);

        public async Task<GridConfig> CreateGridConfigAsync(GridConfig config)
        {
            config.UpdateAt = DateTime.UtcNow;
            await _configRepo.InsertAsync(config);
            return config;
        }

        public async Task<GridConfig> UpdateGridConfigAsync(GridConfig config)
        {
            config.UpdateAt = DateTime.UtcNow;
            await _configRepo.UpdateAsync(config);
            return config;
        }

        public async Task UpdateGridStateAfterTradeAsync(string name, decimal executedPrice)
        {
            var config = await _configRepo.GetFirstAsync(x => x.Name == name);
            if (config is null)
            {
                return;
            }

            config.UpdateStateAfterTrade(executedPrice);
            await _configRepo.UpdateAsync(config);
        }
    }
}
