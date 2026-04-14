using Microsoft.Extensions.Hosting;
using Oc.BinGrid.Strategies;

namespace Oc.BinGrid.Host.Worker;

public class GridHostService : IHostedService
{
    private readonly BotLauncher _bot;

    public GridHostService(BotLauncher bot)
    {
        _bot = bot;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _bot.RunAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _bot.StopAsync();
    }
}
