using Microsoft.Extensions.Logging;
using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.Enums;
using Oc.BinGrid.Domain.Interfaces;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Engine.Managers
{
    public class TradeLogManager : ITransientDependency
    {
        private readonly ILogger<TradeLogManager> _logger;
        private readonly IRepository<TradeLog, long> _logRepo;

        public TradeLogManager(ILogger<TradeLogManager> logger, IRepository<TradeLog, long> logRepo)
        {
            _logger = logger;
            _logRepo = logRepo;
        }

        public async Task<TradeLog> RecordAsync(
            string strategy,
            string symbol,
            MarketType market,
            OrderSide side,
            decimal price,
            decimal qty,
            decimal fee,
            string feeAsset,
            bool isMaker,
            string orderId,
            string? positionId)
        {
            var log = new TradeLog(strategy, symbol, market, side, price, qty, fee, feeAsset, isMaker, orderId, positionId);
            await _logRepo.InsertAsync(log);

            _logger.LogInformation("Trade log recorded Strategy={Strategy} Symbol={Symbol} Side={Side}", strategy, symbol, side);
            return log;
        }

        public Task<List<TradeLog>> GetAllAsync() => _logRepo.GetListAsync();
    }
}
