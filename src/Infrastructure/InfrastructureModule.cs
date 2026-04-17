using Flurl.Http;
using Microsoft.Extensions.DependencyInjection;
using Oc.BinGrid.Domain;
using Oc.BinGrid.Domain.Interfaces;
using Oc.BinGrid.Infrastructure.Db;
using Oc.BinGrid.Infrastructure.Exchanges;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Oc.BinGrid.Infrastructure
{
    [DependsOn(
        typeof(DomainModule) // 依赖 ABP 的 DDD 模块
    )]
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
