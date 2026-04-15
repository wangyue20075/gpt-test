using Microsoft.Extensions.DependencyInjection;
using Oc.BinGrid.Engine;
using Oc.BinGrid.Host.Workers;
using Oc.BinGrid.Infrastructure;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Ocean.BinGrid;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(EngineModule),
    typeof(InfrastructureModule)
)]
public class GridHostModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHostedService<TickPollingWorker>();
        context.Services.AddHostedService<StrategyWorker>();
        context.Services.AddHostedService<PersistenceWorker>();
    }
}