using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Oc.BinGrid.Domain.Interfaces;
using Oc.BinGrid.Engine.Strategies.Grid;

namespace Oc.BinGrid.Host.Worker
{
    public class GridBackgroundWorker : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly Dictionary<string, GridManager> _activeManagers = new();

        public GridBackgroundWorker(IServiceProvider services) => _services = services;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _services.CreateScope();
            var configRepo = scope.ServiceProvider.GetRequiredService<IGridConfigRepository>();
            var gateway = scope.ServiceProvider.GetRequiredService<IMarketGateway>();

            // 1. 从 SQLite 恢复所有启用的网格配置
            var configs = await configRepo.GetAllActiveConfigsAsync();

            foreach (var config in configs)
            {
                // 为每个配置创建 Manager 实例（DI 手动解析依赖）
                var manager = ActivatorUtilities.CreateInstance<GridManager>(scope.ServiceProvider, config);
                _activeManagers.Add(config.Symbol, manager);
            }

            // 2. 启动 WebSocket 监听行情
            // 假设 Infrastructure 提供了行情流
            await gateway.SubscribeMarketDataAsync(_activeManagers.Keys.ToList(), async tick =>
            {
                if (_activeManagers.TryGetValue(tick.Symbol, out var manager))
                {
                    await manager.ExecuteTickAsync(tick.Price);
                }
            });

            // 维持运行
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
