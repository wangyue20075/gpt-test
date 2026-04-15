using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Volo.Abp;

namespace Ocean.BinGrid;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        // ⭐ 计算项目根目录（Host目录）
        var projectRoot = Directory.GetParent(AppContext.BaseDirectory)!
            .Parent!   // Debug / Release
            .Parent!   // bin
            .Parent!   // Host
            .FullName;
        // ⭐ 设置当前工作目录为项目根
        Directory.SetCurrentDirectory(projectRoot);
        // ⭐ 创建 logs 目录
        Directory.CreateDirectory(Path.Combine(projectRoot, "Logs"));
        Directory.CreateDirectory(Path.Combine(projectRoot, "Data"));

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Volo.Abp", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Async(c => c.File("Logs/grid-.log", rollingInterval: RollingInterval.Day, fileSizeLimitBytes: 1024 * 1024 * 20, rollOnFileSizeLimit: true))
            .WriteTo.Async(c => c.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}"))
            .WriteTo.Async(c => c.Console())
            .CreateLogger();

        try
        {
            Log.Information("Starting BinGrid console host.");

            var builder = Host.CreateApplicationBuilder(args);

            // ⭐ 关键：修改 ContentRoot
            builder.Environment.ContentRootPath = projectRoot;
            builder.Configuration.SetBasePath(projectRoot);

            builder.Configuration.AddAppSettingsSecretsJson();
            builder.Logging.ClearProviders().AddSerilog();
            builder.ConfigureContainer(builder.Services.AddAutofacServiceProviderFactory());

            await builder.Services.AddApplicationAsync<GridHostModule>();
            var host = builder.Build();
            await host.InitializeAsync();
            await host.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            if (ex is HostAbortedException)
            {
                throw;
            }
            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
