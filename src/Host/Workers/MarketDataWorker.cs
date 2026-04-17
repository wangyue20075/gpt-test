using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Oc.BinGrid.Engine.Orchestrator;

namespace Oc.BinGrid.Host.Workers;

public class MarketDataWorker : BackgroundService
{
    private readonly ILogger<MarketDataWorker> _logger;
    private readonly MarketDataDispatcher _dispatcher;

    public MarketDataWorker(ILogger<MarketDataWorker> logger, MarketDataDispatcher dispatcher)
    {
        _logger = logger;
        _dispatcher = dispatcher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("行情轮询服务已启动。");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await _dispatcher.DispatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "行情调度循环发生异常。");
            }
        }

        _logger.LogInformation("行情轮询服务已停止。");
    }
}
