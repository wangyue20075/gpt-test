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
            _logger.LogInformation("PersistenceWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var first = await _channel.Reader.ReadAsync(stoppingToken);
                    var batch = new List<object> { first };

                    while (batch.Count < 100 && _channel.Reader.TryRead(out var item))
                    {
                        batch.Add(item);
                    }

                    var grouped = batch.GroupBy(x => x.GetType()).ToList();
                    foreach (var group in grouped)
                    {
                        await ProcessTypedBatchAsync(group.Key, group.ToList());
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PersistenceWorker loop failed.");
                }
            }

            _logger.LogInformation("PersistenceWorker stopped.");
        }

        private async Task ProcessTypedBatchAsync(Type entityType, List<object> entities)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            try
            {
                await db.Ado.UseTranAsync(async () =>
                {
                    var storage = db.Storageable(entities).ToStorage();
                    await storage.AsInsertable.ExecuteCommandAsync();
                    await storage.AsUpdateable.ExecuteCommandAsync();
                });

                _logger.LogDebug("PersistenceWorker persisted {Count} records for {EntityType}.", entities.Count, entityType.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Persistence batch failed for {EntityType}", entityType.Name);
            }
        }
    }
}
