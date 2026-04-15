using Microsoft.Extensions.Configuration;
using Oc.BinGrid.Domain.Entities;
using SqlSugar;

namespace Oc.BinGrid.Infrastructure.Db
{
    public class SqlSugarContext
    {
        public ISqlSugarClient Db { get; }

        public SqlSugarContext(IConfiguration config)
        {
            Db = new SqlSugarScope(new ConnectionConfig()
            {
                // 从 appsettings.json 读取连接字符串
                ConnectionString = config.GetConnectionString("Default"),
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = true,
                ConfigureExternalServices = new ConfigureExternalServices()
                {
                    // 核心：外部配置映射，解决 Domain 层不引用 SqlSugar 的问题
                    EntityService = (prop, column) =>
                    {
                        // 1. 统一主键约定：属性名为 Id 的自动设为主键
                        if (prop.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                        {
                            column.IsPrimarykey = true;
                            column.IsIdentity = true;
                        }

                        // 2. 精度处理：量化交易必须保证 decimal 不丢失精度
                        if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
                        {
                            column.DataType = "decimal(18, 8)";
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 初始化数据库表结构与高性能参数
        /// </summary>
        public void InitDb()
        {
            // 1. 开启高性能 WAL 模式（实现读写并行，EA 工具必开）
            Db.Ado.ExecuteCommand("PRAGMA journal_mode=WAL;");
            Db.Ado.ExecuteCommand("PRAGMA synchronous=NORMAL;");

            // 2. CodeFirst 检查并创建表
            // 注意：这里手动指定所有需要生成的领域实体
            Db.CodeFirst.InitTables(
                typeof(Position)
                //typeof(TradeOrder)
                //typeof(AssetBalance),
                //typeof(StrategyConfig),
                //typeof(TradeLog)
            );
        }
    }
}
