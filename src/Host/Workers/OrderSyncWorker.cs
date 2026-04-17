using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Oc.BinGrid.Engine.Interfaces;

namespace Oc.BinGrid.Host.Workers
{
    public class OrderSyncWorker : BackgroundService
    {
        private readonly IOrderMonitorService _monitor;
        private readonly ILogger<OrderSyncWorker> _logger;

        public OrderSyncWorker(IOrderMonitorService monitor, ILogger<OrderSyncWorker> logger)
        {
            _monitor = monitor;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("订单同步对账服务已启动（频率：20秒/次）");

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(20));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    // 🚀 这里调用！
                    // 它会遍历监控中的所有订单，去交易所确认是否已经成交或取消
                    await _monitor.SyncOrderStatusAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "对账循环发生异常");
                }
            }
        }
    }
}
