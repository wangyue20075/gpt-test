using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.Interfaces;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Managers
{
    public class GridConfigManager : ITransientDependency
    {
        private readonly IRepository<GridConfig, long> _configRepo;

        public GridConfigManager(IRepository<GridConfig, long> configRepo)
        {
            _configRepo = configRepo;
        }

        public async Task<List<GridConfig>> GetGridConfigs()
        {
            var list = await _configRepo.GetListAsync();
            return list;
        }

        public async Task<GridConfig?> GetGridConfigByIdAsync(long id)
        {
            return await _configRepo.GetByIdAsync(id);
        }

        public async Task<GridConfig> CreateGridConfigAsync(GridConfig config)
        {
            await _configRepo.InsertAsync(config);
            return config;
        }

        public async Task<GridConfig> UpdateGridConfigAsync(GridConfig config)
        {
            await _configRepo.UpdateAsync(config);
            return config;
        }

        public async Task UpdateGridBasePrice(string strategyName, decimal price)
        {
            var configList = await _configRepo.GetListAsync(x => x.Name == strategyName);
            var config = configList.FirstOrDefault();
            if (config != null)
            {
                config.BasePrice = price;
                await _configRepo.UpdateAsync(config);
            }
        }

        public async Task UpdateGridStateAsync(string name, decimal basePrice, decimal high, decimal low)
        {
            // 这里使用仓储更新数据库
            var config = await _configRepo.GetFirstAsync(x => x.Name == name);
            if (config != null)
            {
                config.BasePrice = basePrice;
                config.TrackedHigh = high;
                config.TrackedLow = low;
                config.UpdateAt = DateTime.UtcNow;
                await _configRepo.UpdateAsync(config);
            }
        }

        /// <summary>
        /// 当策略成交后，更新数据库中的实体状态
        /// </summary>
        public async Task UpdateGridStateAfterTradeAsync(string name, decimal executedPrice)
        {
            // 1. 从数据库获取当前的策略配置实体
            var config = await _configRepo.GetFirstAsync(x => x.Name == name);

            if (config != null)
            {
                // 2. 调用实体内部的业务方法更新状态
                // 这确保了 BasePrice, TrackedHigh, TrackedLow 同时被重置为成交价
                config.UpdateStateAfterTrade(executedPrice);

                // 3. 持久化到数据库
                await _configRepo.UpdateAsync(config);

                // ABP 框架会在 UnitOfWork 完成时自动提交事务
            }
        }
    }
}
