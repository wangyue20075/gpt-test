using Microsoft.Extensions.Logging;
using Oc.BinGrid.Entities;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Managers
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

        public async Task<BalanceSnapshot?> GetBalanceAsync()
        {
            var list = await _balanceRepo.GetPagedListAsync(0, 1, "SnapshotTime DESC", true);
            return list.FirstOrDefault();
        }
    }
}
