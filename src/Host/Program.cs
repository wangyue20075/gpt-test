using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using System.Text;
using Volo.Abp;

namespace Ocean.BinGrid;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        // 1. 强制控制台编码为 UTF-8，解决图标显示为 ?? 的问题
        //Console.OutputEncoding = Encoding.UTF8;

        //// 2. 路径处理：计算项目根目录（Host目录）
        //var workDir = Directory.GetParent(AppContext.BaseDirectory)!
        //    .Parent!   // Debug / Release
        //    .Parent!   // bin
        //    .Parent!   // Host
        //    .FullName;
        //Directory.SetCurrentDirectory(workDir);

        //// 3. 预创建必要目录
        //Directory.CreateDirectory(Path.Combine(workDir, "Logs"));
        //Directory.CreateDirectory(Path.Combine(workDir, "Data"));

        //// 4. 配置 Serilog
        //var logTemplate = "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}";
        //Log.Logger = new LoggerConfiguration()
        //    //.MinimumLevel.Debug()
        //    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        //    .MinimumLevel.Override("Volo.Abp", LogEventLevel.Warning)
        //    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
        //    .Enrich.FromLogContext()
        //    .WriteTo.Async(c => c.Console(outputTemplate: logTemplate))
        //    // 💡 异步文件日志
        //    .WriteTo.Async(c => c.File(
        //        path: "Logs/grid-.log",
        //        rollingInterval: RollingInterval.Day,
        //        outputTemplate: logTemplate,
        //        fileSizeLimitBytes: 1024 * 1024 * 20,
        //        rollOnFileSizeLimit: true))
        //    .CreateLogger();

        Console.OutputEncoding = Encoding.UTF8;
        var workDir = GetWorkDirectory();
        PrepareDirectories(workDir);

        ConfigureSerilog(workDir);

        try
        {
            Log.Information("🚀 量化交易系统启动中...");

            var builder = Host.CreateApplicationBuilder(args);

            // 5. 环境配置
            builder.Environment.ContentRootPath = workDir;
            builder.Configuration.SetBasePath(workDir);
            builder.Configuration.AddAppSettingsSecretsJson();

            // 6. 日志重构关键点
            // 先清除微软默认的 Console Provider，再注入 Serilog
            builder.Logging.ClearProviders();
            builder.Services.AddSerilog();

            // 7. 容器注入 (Autofac)
            builder.ConfigureContainer(builder.Services.AddAutofacServiceProviderFactory());
            await builder.Services.AddApplicationAsync<HostModule>();

            var host = builder.Build();
            await host.InitializeAsync();

            Log.Information("✅ 系统初始化完成，策略引擎已运行。");

            await host.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            if (ex is HostAbortedException) throw;

            Log.Fatal(ex, "❌ 系统异常终止！");
            return 1;
        }
        finally
        {
            Log.Information("🛑 系统退出，正在刷新日志缓冲区...");
            Log.CloseAndFlush();
        }
    }


    #region 私有方法

    private static string GetWorkDirectory()
    {
        var dir = Directory.GetParent(AppContext.BaseDirectory)!
            .Parent!
            .Parent!
            .Parent!
            .FullName;

        Directory.SetCurrentDirectory(dir);
        return dir;
    }

    private static void PrepareDirectories(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "Logs/system"));
        Directory.CreateDirectory(Path.Combine(root, "Logs/strategy"));
        Directory.CreateDirectory(Path.Combine(root, "Logs/trade"));
        Directory.CreateDirectory(Path.Combine(root, "Logs/market"));
        Directory.CreateDirectory(Path.Combine(root, "Logs/json"));
        Directory.CreateDirectory(Path.Combine(root, "Data"));
    }

    private static void ConfigureSerilog(string root)
    {
        var template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] " +
            "{Message:lj} {NewLine}{Exception}";

        //var logTemplate = "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}";

        Log.Logger = new LoggerConfiguration()
            // 🔥 全局最低级别
            .MinimumLevel.Information()
            // 屏蔽框架噪音
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Volo.Abp", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            // 增强字段
            .Enrich.FromLogContext()
            // 控制台（开发）
            .WriteTo.Async(a => a.Console(outputTemplate: template))
            // 1️⃣ 系统日志
            .WriteTo.Async(a => a.File(
                Path.Combine(root, "Logs/system/system-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: template,
                fileSizeLimitBytes: 20 * 1024 * 1024,
                rollOnFileSizeLimit: true))
            // 2️⃣ 策略日志
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(e => e.Properties.ContainsKey("Strategy"))
                .WriteTo.Async(a => a.File(
                    Path.Combine(root, "Logs/strategy/strategy-.log"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: template)))
            // 3️⃣ 交易日志
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(e => e.Properties.ContainsKey("Trade"))
                .WriteTo.Async(a => a.File(
                    Path.Combine(root, "Logs/trade/trade-.log"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: template)))
            // 4️⃣ 行情日志
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(e => e.Properties.ContainsKey("Market"))
                .WriteTo.Async(a => a.File(
                    Path.Combine(root, "Logs/market/market-.log"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: template)))
            // 5️⃣ JSON结构化日志（生产分析用）
            .WriteTo.Async(a => a.File(
                new JsonFormatter(),
                Path.Combine(root, "Logs/json/all-.json"),
                rollingInterval: RollingInterval.Day))
            .CreateLogger();
    }

    #endregion
}
