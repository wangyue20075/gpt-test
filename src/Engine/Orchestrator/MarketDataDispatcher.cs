using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oc.BinGrid.Domain.Interfaces;
using Oc.BinGrid.Domain.ValueObjects;
using Oc.BinGrid.Domain.Values;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Engine.Orchestrator
{
    /// <summary>
    /// 行情分发器
    /// </summary>
    public class MarketDataDispatcher : ISingletonDependency
    {
        private readonly IExchangeGateway _gateway;
        private readonly StrategyOrchestrator _orchestrator;
        private readonly ILogger<MarketDataDispatcher> _logger;
        private readonly IReadOnlyList<string> _symbols;

        private readonly SemaphoreSlim _executionLock = new(1, 1);

        public MarketDataDispatcher(
            IExchangeGateway gateway,
            StrategyOrchestrator orchestrator,
            IConfiguration config,
            ILogger<MarketDataDispatcher> logger)
        {
            _gateway = gateway;
            _orchestrator = orchestrator;
            _logger = logger;

            _symbols = config.GetSection("Strategies")
                             .Get<List<GridSetting>>()
                             ?.Select(x => x.Symbol.ToUpper())
                             .Distinct()
                             .ToList()
                         ?? new List<string>();
        }

        public async Task DispatchAsync(CancellationToken ct)
        {
            if (!await _executionLock.WaitAsync(0, ct))
            {
                _logger.LogDebug("上一轮行情调度尚未完成，本轮跳过。");
                return;
            }

            try
            {
                if (_symbols.Count == 0)
                {
                    _logger.LogWarning("未发现任何需要调度的交易对。");
                    return;
                }

                _logger.LogDebug("开始执行行情调度，交易对数量: {Count}", _symbols.Count);

                var tasks = _symbols.Select(symbol => ProcessSymbolAsync(symbol, ct));
                await Task.WhenAll(tasks);
            }
            finally
            {
                _executionLock.Release();
            }
        }

        private async Task ProcessSymbolAsync(string symbol, CancellationToken ct)
        {
            try
            {
                var tick = await _gateway.GetLatestTickAsync(symbol);

                if (tick == null)
                {
                    _logger.LogWarning("获取行情为空，交易对: {Symbol}", symbol);
                    return;
                }

                _orchestrator.OnPriceUpdate((TickData)tick);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取行情失败，交易对: {Symbol}", symbol);
            }
        }
    }
}
