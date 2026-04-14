using Microsoft.Extensions.DependencyInjection;
using Oc.BinGrid.Domain.Interfaces;
using Oc.BinGrid.Domain.Values;

namespace Oc.BinGrid.Engine.Strategies.Grid
{
    public class GridStrategy : IStrategy
    {
        private readonly IServiceProvider _services;
        private readonly Dictionary<string, GridManager> _managers = new();

        public string StrategyId { get; } = Guid.NewGuid().ToString();
        public string StrategyName => "GridTrading";
        public bool IsRunning { get; private set; }

        public GridStrategy(IServiceProvider services) => _services = services;

        public async Task StartAsync(CancellationToken ct)
        {
            using var scope = _services.CreateScope();
            var configRepo = scope.ServiceProvider.GetRequiredService<IGridConfigRepository>();

            // 只加载属于“网格”类型的配置
            var configs = await configRepo.GetConfigsByTypeAsync(StrategyName);

            foreach (var config in configs)
            {
                var manager = ActivatorUtilities.CreateInstance<GridManager>(scope.ServiceProvider, config);
                _managers.Add(config.Symbol, manager);
            }
            IsRunning = true;
        }

        public async Task OnTickAsync(TickData tick)
        {
            if (_managers.TryGetValue(tick.Symbol, out var manager))
            {
                await manager.ExecuteTickAsync(tick.Price);
            }
        }

        public Task StopAsync()
        {
            IsRunning = false;
            return Task.CompletedTask;
        }
    }
}
