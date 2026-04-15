using Microsoft.Extensions.Logging;
using Oc.BinGrid.Domain.Values;
using Oc.BinGrid.Engine.Interfaces;
using Oc.BinGrid.Engine.Strategies;
using System.Threading.Channels;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Engine.Orchestrator;

public class StrategyOrchestrator : ISingletonDependency
{
    private readonly Channel<TickData> _tickChannel;
    private readonly IEnumerable<StrategyBase> _strategies;
    private readonly IOrderMonitorService _monitor;
    private readonly StrategyRecoveryService _recoveryService;
    private readonly ILogger<StrategyOrchestrator> _logger;
    private bool _isRunning;

    private ILookup<string, StrategyBase> _strategyRoute; // 路由表

    public StrategyOrchestrator(
        ILogger<StrategyOrchestrator> logger,
        IEnumerable<StrategyBase> strategies,
        IOrderMonitorService monitor,
        StrategyRecoveryService recoveryService
        )
    {
        _logger = logger;
        _strategies = strategies;
        _monitor = monitor;
        _recoveryService = recoveryService;

        // 优化配置
        _tickChannel = Channel.CreateUnbounded<TickData>(new UnboundedChannelOptions
        {
            SingleReader = true,
            AllowSynchronousContinuations = true
        });
    }

    /// <summary>
    /// 启动引擎，并确保所有策略完成初始化
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("检测到 {Count} 个已注册策略。", _strategies.Count());

        if (!_strategies.Any())
        {
            _logger.LogWarning("警告：未发现任何有效策略，引擎将空转！");
            return;
        }

        if (_isRunning) return;

        _logger.LogInformation("策略引擎正在启动...");

        // 1. 初始化策略
        await Task.WhenAll(_strategies.Select(s => s.InitializeAsync()));

        // 2. 构建路由表 (按 Symbol 分组，提高分发效率)
        _strategyRoute = _strategies.ToLookup(s => s.Symbol);

        // 3. 重要：恢复挂单监控
        // 假设你有一个 RecoveryService，或者在策略初始化中已经处理了
        await _recoveryService.RecoverActiveTasksAsync(_strategies);

        _isRunning = true;
        _ = Task.Run(async () => await ProcessLoopAsync(cancellationToken), cancellationToken);

        _logger.LogInformation("策略引擎已就绪，正在监听行情流...");
    }

    /// <summary>
    /// 停止引擎，优雅关闭通道
    /// </summary>
    public async Task StopAsync()
    {
        _tickChannel.Writer.TryComplete();

        // 优雅等待消费者处理完剩余 Tick (可选)
        int retry = 0;
        while (_isRunning && retry++ < 10)
        {
            await Task.Delay(500);
        }

        foreach (var strategy in _strategies)
        {
            await strategy.StopAsync();
        }

        _logger.LogInformation("策略引擎已停止。");
    }

    /// <summary>
    /// 价格更新接口，外部调用（如行情服务）将 Tick 数据推送到引擎
    /// </summary>
    public void OnPriceUpdate(TickData tick)
    {
        if (!_isRunning) return;

        // 极速入队
        if (!_tickChannel.Writer.TryWrite(tick))
        {
            _logger.LogWarning("队列溢出或已关闭，Tick 丢弃: {Symbol} {Price}", tick.Symbol, tick.Price);
        }
    }

    #region 内部方法

    private async Task ProcessLoopAsync(CancellationToken ct)
    {
        try
        {
            // 使用更高效的读取方式
            while (await _tickChannel.Reader.WaitToReadAsync(ct))
            {
                while (_tickChannel.Reader.TryRead(out var tick))
                {
                    // 1. 驱动订单监控 (检查是否需要撤单)
                    _ = _monitor.OnTickAsync(tick.Symbol, tick.Price);

                    await DispatchToStrategiesAsync(tick);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("策略引擎收到停止指令。");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "策略引擎崩溃！请检查底层通道状态。");
        }
        finally
        {
            _isRunning = false;
        }
    }

    private async Task DispatchToStrategiesAsync(TickData tick)
    {
        // 只分发给订阅了该 Symbol 的策略
        var targetStrategies = _strategyRoute[tick.Symbol];

        foreach (var strategy in targetStrategies)
        {
            if (!strategy.IsEnabled) continue;

            try
            {
                // 如果对延迟极度敏感，且策略间无资源争夺，可考虑不 await
                // 但为了逻辑顺序性，await 是最稳妥的
                await strategy.OnTickAsync(tick);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "策略 {Name} 执行异常", strategy.Name);
            }
        }
    }

    #endregion
}
