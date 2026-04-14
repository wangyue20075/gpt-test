using Oc.BinGrid.Domain.Entities;
using SqlSugar;
using Volo.Abp.DependencyInjection;

namespace Oc.BinGrid.Infrastructure.DbCore
{
    public class GridBinDbContext : ISingletonDependency
    {
        private readonly ISqlSugarClient _db;

        public GridBinDbContext(ISqlSugarClient db)
        {
            _db = db;
        }

        public void EnsureCreated()
        {
            // 如果表不存在，SqlSugar 会自动根据实体创建表
            _db.CodeFirst.InitTables<BalanceSnapshot>();
            _db.CodeFirst.InitTables<EaOrder>();
            _db.CodeFirst.InitTables<EaPosition>();
            _db.CodeFirst.InitTables<GridConfig>();
            _db.CodeFirst.InitTables<TradeLog>();
        }
    }
}
