using Binance.Net.Clients;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Oc.BinGrid.Domain;
using Oc.BinGrid.Infrastructure;
using Volo.Abp.Modularity;

namespace Oc.BinGrid.Engine
{
    [DependsOn(
        typeof(GridBinDomainModule),
        typeof(InfrastructureModule)
    )]
    public class GridBinCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            var account = configuration.GetSection("Binance").Get<GridAccount>() ?? new GridAccount();

            context.Services.AddSingleton(_ => new BinanceRestClient(option =>
            {
                option.ApiCredentials = new ApiCredentials(account.ApiKey, account.ApiSecret);
            }));

            context.Services.AddSingleton(_ => new BinanceSocketClient(option =>
            {
                option.ReconnectInterval = TimeSpan.FromSeconds(30);
            }));
        }
    }
}
