using Microsoft.Extensions.DependencyInjection;
using Oc.BinGrid.Host.Worker;
using Ocean.BinGrid.Core;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Ocean.BinGrid;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(GridBinCoreModule)
)]
public class GridHostModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHostedService<GridHostService>();
    }
}