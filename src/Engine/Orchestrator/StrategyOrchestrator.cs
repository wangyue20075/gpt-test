using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Oc.BinGrid.Domain.Enums;
using Oc.BinGrid.Domain.Interfaces;
using Oc.BinGrid.Domain.ValueObjects;
using Oc.BinGrid.Domain.Values;
using Oc.BinGrid.Engine.Interfaces;
using Oc.BinGrid.Engine.Strategies;
using Oc.BinGrid.Engine.Strategies.Grid;
using System.Collections.Concurrent;
using System.Threading.Channels;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Engine.Orchestrator;

public class StrategyOrchestrator : ISingletonDependency
{
    private readonly ConcurrentDictionary<string, Channel<TickData>> _symbolPipes = new();
    private readonly ConcurrentDictionary<string, IStrategy> _strategies = new();
    private readonly IOrderMonitorService _monitor;
    private readonly StrategyRecoveryService _recoveryService;
    private readonly ILogger<StrategyOrchestrator> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _config;

    private ILookup<string, IStrategy> _strategyRoute;
    private bool _isRunning;
    private readonly CancellationTokenSource _cts = new();

    public StrategyOrchestrator(
        ILogger<StrategyOrchestrator> logger,
        IOrderMonitorService monitor,
        StrategyRecoveryService recoveryService,
        IServiceProvider serviceProvider,
        IConfiguration config)
    {
        _logger = logger;
        _monitor = monitor;
        _recoveryService = recoveryService;
        _serviceProvider = serviceProvider;
        _config = config;
        _strategyRoute = new Dictionary<string, IStrategy>().ToLookup(s => s.Key, s => s.Value);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning) return;

        _logger.LogInformation("🚀 策略引擎启动中...");

        // 1. 加载配置并实例化
        await LoadStrategiesFromConfigAsync();
        if (!_strategies.Any())
        {
            _logger.LogWarning("未发现有效策略配置，引擎空转。");
            return;
        }

        // 2. 预构建路由表，提升 Processor 查找速度
        _strategyRoute = _strategies.Values.ToLookup(s => s.Symbol);

        // 3. 执行统一恢复与启动序列
        // 注意：RecoverAllAsync 内部应负责调用策略的 StartAsync
        await _recoveryService.RecoverAllAsync(_strategies.Values.Cast<StrategyBase>());

        // 4. 开启策略内部逻辑
        //await Task.WhenAll(_strategies.Values.Select(s => s.StartAsync()));

        // 启动定时监控打印任务
        await Task.Run(() => StartPositionReportingLoopAsync(_cts.Token));

        _isRunning = true;
        _logger.LogInformation("✅ 引擎就绪。当前运行策略数: {Count}", _strategies.Count);
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("正在关闭策略引擎...");
        _isRunning = false;
        _cts.Cancel(); // 通知所有处理器停止

        // 1. 关闭所有管道写入器
        foreach (var pipe in _symbolPipes.Values)
        {
            pipe.Writer.TryComplete();
        }

        // 2. 停止所有策略
        await Task.WhenAll(_strategies.Values.Select(s => s.StopAsync()));

        _logger.LogInformation("策略引擎已安全停止。");
    }

    /// <summary>
    /// 价格更新入口：负责按 Symbol 将数据分发到对应管道
    /// </summary>
    public void OnPriceUpdate(TickData tick)
    {
        if (!_isRunning) return;

        // 获取该币种独占的管道，不存在则创建
        var channel = _symbolPipes.GetOrAdd(tick.Symbol, symbol =>
        {
            var pipe = Channel.CreateUnbounded<TickData>(new UnboundedChannelOptions
            {
                // 保证单币种时序严格性
                SingleReader = true,
                // 避免在写入线程直接跑后续逻辑，提高分发速度
                AllowSynchronousContinuations = false
            });

            // 为该管道启动独立消费者
            _ = Task.Run(() => StartSymbolProcessorAsync(symbol, pipe.Reader), _cts.Token);
            return pipe;
        });

        channel.Writer.TryWrite(tick);
    }

    /// <summary>
    /// 持仓审计报告：每 5 分钟打印一次当前所有策略的持仓情况，帮助监控整体风险暴露
    /// </summary>
    private async Task StartPositionReportingLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), ct);

                _logger.LogInformation("--- 📊 全局头寸审计报告 [{Time}] ---", DateTime.Now.ToString("T"));

                foreach (var strategy in _strategies.Values)
                {
                    var positions = strategy.GetActivePositionSnapshots();
                    if (!positions.Any()) continue;

                    _logger.LogInformation("【策略: {Name} | 交易对: {Symbol}】", strategy.Name, strategy.Symbol);

                    // 打印每一层的明细
                    foreach (var pos in positions)
                    {
                        _logger.LogInformation(
                            "  └─ 层级: {Id} | 入场: {Entry:F2} | 现价: {Cur:F2} | 盈亏: {Pnl:F2} ({Rate:P2})",
                            pos.PositionId, pos.EntryPrice, pos.CurrentPrice, pos.ProfitLoss, pos.ProfitLossRate);
                    }

                    // 汇总该策略的总头寸
                    decimal totalPnl = positions.Sum(p => p.ProfitLoss);
                    _logger.LogInformation("  TOTAL => 持仓层数: {Count} | 总浮盈: {TotalPnl:F2}", positions.Count, totalPnl);
                }
                _logger.LogInformation("------------------------------------------");
            }
            catch (Exception ex) { _logger.LogError(ex, "审计报表任务异常"); }
        }
    }


    #region 内部方法

    /// <summary>
    /// 币种处理器：ETH 逻辑在这里跑，DOGE 逻辑在另一个线程跑，互不干扰
    /// </summary>
    private async Task StartSymbolProcessorAsync(string symbol, ChannelReader<TickData> reader)
    {
        _logger.LogInformation("⚡ [管道开启] {Symbol} 处理器", symbol);
        // 获取该币种的所有策略（利用预构建的 Lookup 达到 O(1) 性能）
        var targets = _strategyRoute[symbol].ToList();

        try
        {
            while (await reader.WaitToReadAsync(_cts.Token))
            {
                while (reader.TryRead(out var tick))
                {
                    // A. 监控层对账（即使策略崩溃，监控也应尝试工作）
                    try
                    {
                        // Monitor 内部通常不需要 await，因为它是基于内存状态判断撤单
                        _ = _monitor.OnTickAsync(tick.Symbol, tick.Price);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "监控检查异常: {Symbol}", symbol);
                    }

                    // B. 执行该币种下的所有策略
                    foreach (var strategy in targets)
                    {
                        if (strategy.State == StrategyState.Faulted) continue; // 跳过熔断的策略

                        try
                        {
                            await strategy.OnTickAsync(tick);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "策略执行异常: {Id}", strategy.Id);
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "致命错误：币种管道 {Symbol} 已崩溃", symbol);
        }
    }

    private async Task LoadStrategiesFromConfigAsync()
    {
        var gridConfigs = _config.GetSection("Strategies").Get<List<GridSetting>>();
        if (gridConfigs == null) return;

        foreach (var setting in gridConfigs)
        {
            var strategy = ActivatorUtilities.CreateInstance<GridStrategy>(_serviceProvider, setting);
            if (!_strategies.TryAdd(strategy.Id, strategy))
            {
                _logger.LogWarning("忽略重复策略: {Id}", strategy.Id);
            }
        }
    }

    #endregion
}
