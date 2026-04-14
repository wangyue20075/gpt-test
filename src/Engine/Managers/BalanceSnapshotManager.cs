using Microsoft.Extensions.Logging;
using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.Interfaces;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Engine.Managers
{
    public class BalanceSnapshotManager : ITransientDependency
    {
        private readonly ILogger<BalanceSnapshotManager> _logger;
        private readonly IRepository<BalanceSnapshot, long> _balanceRepo;

        public BalanceSnapshotManager(
            ILogger<BalanceSnapshotManager> logger,
            IRepository<BalanceSnapshot, long> balanceRepo)
        {
            _logger = logger;
            _balanceRepo = balanceRepo;
        }

        public async Task<BalanceSnapshot?> GetLatestAsync()
        {
            var list = await _balanceRepo.GetListAsync();
            var latest = list.OrderByDescending(x => x.SnapshotTime).FirstOrDefault();
            _logger.LogDebug("Latest balance snapshot: {Snapshot}", latest);
            return latest;
        }

        public Task<bool> SaveAsync(BalanceSnapshot snapshot)
        {
            return _balanceRepo.InsertAsync(snapshot);
        }
    }
}
