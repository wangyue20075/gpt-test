using Flurl.Http;
using Microsoft.Extensions.DependencyInjection;
using Oc.BinGrid.Domain.Interfaces;
using Oc.BinGrid.Infrastructure.Db;
using Oc.BinGrid.Infrastructure.Exchanges;
using Oc.BinGrid.Infrastructure.Repositories;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Oc.BinGrid.Infrastructure
{
    public class InfrastructureModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            // 配置 Flurl 使用 System.Text.Json 并处理精度（可选）
            FlurlHttp.Clients.WithDefaults(builder => builder
                .WithSettings(settings =>
                {
                    // 这里可以配置全局超时、头信息等
                    settings.Timeout = TimeSpan.FromSeconds(10);
                }));


            // 1. 注册上下文（单例）
            context.Services.AddSingleton<SqlSugarContext>();

            // 2. 注册具体仓储
            context.Services.AddTransient<IPositionRepository, PositionRepository>();
            context.Services.AddTransient<IOrderRepository, OrderRepository>();

            // 注册网关
            context.Services.AddTransient<IExchangeGateway, BinanceGateway>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            // 在程序启动后执行数据库初始化
            var dbContext = context.ServiceProvider.GetRequiredService<SqlSugarContext>();
#if DEBUG
            dbContext.InitDb();
#endif
        }
    }
}
