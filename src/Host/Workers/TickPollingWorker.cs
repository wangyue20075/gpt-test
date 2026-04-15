using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Oc.BinGrid.Domain.Interfaces;
using Oc.BinGrid.Engine.Orchestrator;

namespace Oc.BinGrid.Host.Workers;

public class TickPollingWorker : BackgroundService
{
    private readonly IExchangeGateway _gateway;
    private readonly StrategyOrchestrator _engine;
    private readonly ILogger<TickPollingWorker> _logger;
    private const string Symbol = "BTCUSDT";

    public TickPollingWorker(
        IExchangeGateway gateway,
        StrategyOrchestrator engine,
        ILogger<TickPollingWorker> logger)
    {
        _gateway = gateway;
        _engine = engine;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Tick 轮询服务已启动，频率：5秒/次");

        // 使用 .NET 6+ 高性能 PeriodicTimer，比 Timer 更准确且对异步友好
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // 1. 获取最新价格
                var tick = await _gateway.GetLatestTickAsync(Symbol);

                // 2. 将 Tick 喂给引擎的 Channel
                // 这是高性能的关键：网关只管丢数据，不等待策略执行完
                _engine.OnPriceUpdate(tick);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取 Tick 数据失败");
            }
        }
    }
}
