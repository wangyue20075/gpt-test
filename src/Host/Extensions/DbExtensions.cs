using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ocean.BinGrid.EntityFrameworkCore;

namespace Oc.BinGrid.Extensions
{
    public static class DbExtensions
    {
        public static void InitializeSqlite(this IHost host)
        {
            using var scope = host.Services.CreateAsyncScope();

            var db = scope.ServiceProvider.GetRequiredService<GridBinDbContext>();

            db.EnsureCreated();
        }
    }
}
