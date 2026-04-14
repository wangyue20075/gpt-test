using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace Oc.BinGrid.Infrastructure.Services
{
    public class PersistenceWorker : BackgroundService
    {
        private readonly PersistenceChannel _channel;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PersistenceWorker> _logger;

        public PersistenceWorker(PersistenceChannel channel, IServiceProvider serviceProvider, ILogger<PersistenceWorker> logger)
        {
            _channel = channel;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //_logger.LogInformation("Persistence Worker 启动，支持仓位与订单的增量同步...");

            //while (await _channel.Reader.WaitToReadAsync(stoppingToken))
            //{
            //    var hedgeBuffer = new List<HedgePositionEntity>();
            //    var orderBuffer = new List<HedgeOrderEntity>();

            //    // 批量提取并分类 (最大批次设为 100 以提升 SQLite 事务效率)
            //    while (_channel.Reader.TryRead(out var item) && (hedgeBuffer.Count + orderBuffer.Count) < 100)
            //    {
            //        if (item is HedgePositionEntity h) hedgeBuffer.Add(h);
            //        else if (item is HedgeOrderEntity o) orderBuffer.Add(o);
            //    }

            //    // 并行执行批量落盘
            //    var tasks = new List<Task>();
            //    if (hedgeBuffer.Any()) tasks.Add(ProcessBatchAsync(hedgeBuffer));
            //    if (orderBuffer.Any()) tasks.Add(ProcessBatchAsync(orderBuffer));

            //    await Task.WhenAll(tasks);
            //}
        }

        private async Task ProcessBatchAsync<T>(List<T> entities) where T : class, new()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            try
            {
                // SQLite 强烈建议在事务中执行 Storageable
                await db.Ado.UseTranAsync(async () =>
                {
                    var storage = db.Storageable(entities).ToStorage();

                    // 执行自动插入和更新
                    await storage.AsInsertable.ExecuteCommandAsync();
                    await storage.AsUpdateable.ExecuteCommandAsync();

                    // 可选：打印日志确认操作详情
                    // _logger.LogDebug("[{Type}] 批量操作: 插入 {ins} 条, 更新 {upd} 条", 
                    //    typeof(T).Name, storage.InsertList.Count, storage.UpdateList.Count);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量入库失败 (类型: {Type})", typeof(T).Name);
            }
        }
    }
}
