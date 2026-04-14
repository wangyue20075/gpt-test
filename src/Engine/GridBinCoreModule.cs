using Binance.Net.Clients;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

            // 从配置文件获取 API Key/Secret
            var account = configuration.GetSection("Binance").Get<GridAccount>()!;

            // 注册 BinanceRestClient 单例
            context.Services.AddSingleton(_ =>
            {
                return new BinanceRestClient(option =>
                {
                    //var apiKey = configuration["Binance:ApiKey"];
                    //var apiSecret = configuration["Binance:ApiSecret"];
                    option.ApiCredentials = new ApiCredentials(account.ApiKey, account.ApiSecret);
                });
            });

            // 注册 BinanceSocketClient 单例
            context.Services.AddSingleton(_ =>
            {
                return new BinanceSocketClient(option =>
                {
                    // 设置重连间隔为30秒
                    option.ReconnectInterval = TimeSpan.FromSeconds(30); 
                });
            });

            // 注册核心组件
            //context.Services.AddSingleton<OrderExecutor>();
            //context.Services.AddSingleton<RiskManager>();
            //context.Services.AddSingleton<StrategyManager>();
            //context.Services.AddSingleton<BotLauncher>();

        }
    }
}
