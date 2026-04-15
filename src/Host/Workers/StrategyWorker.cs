using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Oc.BinGrid.Engine.Orchestrator;

namespace Oc.BinGrid.Host.Workers;

public class StrategyWorker : BackgroundService
{
    private readonly StrategyOrchestrator _engine;
    private readonly ILogger<StrategyWorker> _logger;

    public StrategyWorker(StrategyOrchestrator engine, ILogger<StrategyWorker> logger)
    {
        _engine = engine;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BackgroundService: 正在启动策略引擎...");

        // 1. 启动引擎内部的 Channel 消费者
        await _engine.StartAsync(stoppingToken);

        // 2. 这里可以执行一些初始化逻辑，比如订阅 WebSocket
        _logger.LogInformation("BackgroundService: 策略引擎已在后台运行。");

        // 保持服务运行，直到接收到停止信号
        while (!stoppingToken.IsCancellationRequested)
        {
            // 这里可以做一些健康检查或性能监控指标的记录
            await Task.Delay(10000, stoppingToken);
        }

        _logger.LogInformation("BackgroundService: 正在停止策略引擎...");
    }
}
