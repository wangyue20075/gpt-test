using Microsoft.Extensions.Hosting;

namespace Oc.BinGrid.Host.Workers;

public class PersistenceWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.CompletedTask;
    }
}
