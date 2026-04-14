using Microsoft.Extensions.Logging;
using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.Enums;
using Oc.BinGrid.Domain.Interfaces;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Managers
{
    /// <summary>
    /// 成交流水领域管理器（PnL / Fee / 统计 / 策略归因）
    /// </summary>
    public class TradeLogManager : ITransientDependency
    {
        private readonly ILogger<TradeLogManager> _logger;
        private readonly IRepository<TradeLog, long> _logRepo;

        public TradeLogManager(
            ILogger<TradeLogManager> logger,
            IRepository<TradeLog, long> logRepo)
        {
            _logger = logger;
            _logRepo = logRepo;
        }

        #region ===== 记录成交 =====

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
            string? positionId,
            decimal realizedPnl = 0m,
            decimal feeInQuote = 0m)
        {
            var log = new TradeLog(
                strategy: strategy,
                symbol: symbol,
                market: market,
                side: side,
                price: price,
                qty: qty,
                fee: fee,
                feeAsset: feeAsset,
                isMaker: isMaker,
                orderId: orderId,
                positionId: positionId
            );

            SetPnl(log, realizedPnl, feeInQuote);

            await _logRepo.InsertAsync(log, autoSave: true);

            _logger.LogInformation("🧾 Trade Recorded: {Symbol} {Side} Qty={Qty} Price={Price} PnL={Pnl}",
                symbol, side, qty, price, realizedPnl);

            return log;
        }

        #endregion

        #region ===== PnL 注入 =====

        private void SetPnl(TradeLog log, decimal pnl, decimal feeQuote)
        {
            typeof(TradeLog).GetProperty(nameof(TradeLog.RealizedPnl))!
                .SetValue(log, pnl);

            typeof(TradeLog).GetProperty(nameof(TradeLog.FeeInQuote))!
                .SetValue(log, feeQuote);
        }

        #endregion

        #region ===== 查询 =====

        public Task<TradeLog?> GetAsync(long id)
            => _logRepo.FindAsync(id);

        public async Task<List<TradeLog>> GetByOrderAsync(string orderId)
        {
            var list = await _logRepo.GetListAsync();
            return list.Where(x => x.OrderId == orderId).ToList();
        }

        public async Task<List<TradeLog>> GetByPositionAsync(string positionId)
        {
            var list = await _logRepo.GetListAsync();
            return list.Where(x => x.PositionId == positionId).ToList();
        }

        public async Task<List<TradeLog>> GetByStrategyAsync(string strategy)
        {
            var list = await _logRepo.GetListAsync();
            return list.Where(x => x.StrategyName == strategy).ToList();
        }

        public async Task<List<TradeLog>> GetBySymbolAsync(string symbol)
        {
            var list = await _logRepo.GetListAsync();
            return list.Where(x => x.Symbol == symbol).ToList();
        }

        #endregion

        #region ===== 统计 / 分析 =====

        public async Task<decimal> GetTotalPnlAsync(string strategy)
        {
            var list = await GetByStrategyAsync(strategy);
            return list.Sum(x => x.RealizedPnl - x.FeeInQuote);
        }

        public async Task<decimal> GetDailyPnlAsync(DateTime dateUtc)
        {
            var list = await _logRepo.GetListAsync();

            return list
                .Where(x => x.TradeTime.Date == dateUtc.Date)
                .Sum(x => x.RealizedPnl - x.FeeInQuote);
        }

        public async Task<(decimal maker, decimal taker)> GetFeeStatsAsync()
        {
            var list = await _logRepo.GetListAsync();

            var maker = list.Where(x => x.IsMaker).Sum(x => x.FeeInQuote);
            var taker = list.Where(x => !x.IsMaker).Sum(x => x.FeeInQuote);

            return (maker, taker);
        }

        public async Task<Dictionary<string, decimal>> GetSymbolPnlMapAsync()
        {
            var list = await _logRepo.GetListAsync();

            return list
                .GroupBy(x => x.Symbol)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.RealizedPnl - x.FeeInQuote)
                );
        }

        #endregion

        #region ===== 清理 =====

        public async Task CleanupOldAsync(int keepDays = 90)
        {
            var cutoff = DateTime.UtcNow.AddDays(-keepDays);

            var list = await _logRepo.GetListAsync();

            var old = list.Where(x => x.TradeTime < cutoff).ToList();

            foreach (var t in old)
                await _logRepo.DeleteAsync(t);

            if (old.Count > 0)
                _logger.LogInformation("🧹 Cleaned {Count} old trade logs", old.Count);
        }

        #endregion
    }
}
