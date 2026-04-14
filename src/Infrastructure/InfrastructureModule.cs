using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oc.BinGrid.Infrastructure.Services;
using SqlSugar;
using Volo.Abp.Modularity;

namespace Oc.BinGrid.Infrastructure
{
    public class InfrastructureModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();

            // 注入 SqlSugar
            context.Services.AddSingleton<ISqlSugarClient>(s =>
            {
                var connStr = configuration.GetConnectionString("Default");

                return new SqlSugarClient(new ConnectionConfig()
                {
                    ConnectionString = connStr,
                    DbType = DbType.Sqlite,
                    IsAutoCloseConnection = true,
                    InitKeyType = InitKeyType.Attribute
                });
            });

            // 后台保存数据入库服务
            context.Services.AddHostedService<PersistenceWorker>();
        }
    }
}
