using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.Enums;
using Oc.BinGrid.Domain.Interfaces;
using Oc.BinGrid.Infrastructure.Db;
using SqlSugar;

namespace Oc.BinGrid.Infrastructure.Repositories
{
    public class PositionRepository : IPositionRepository
    {
        private readonly SqlSugarContext _context;

        public PositionRepository(SqlSugarContext context)
        {
            _context = context;
        }

        private ISqlSugarClient Db => _context.Db;

        public async Task<GridPosition?> GetByIdAsync(
            string id,
            CancellationToken ct = default)
        {
            return await Db.Queryable<GridPosition>()
                .FirstAsync(x => x.Id == id);
        }

        public async Task<GridPosition?> GetByStrategyAsync(
            string strategyId,
            CancellationToken ct = default)
        {
            return await Db.Queryable<GridPosition>()
                .FirstAsync(x => x.StrategyId == strategyId &&
                                 x.Status == PositionStatusType.Open);
        }

        public async Task<GridPosition?> GetBySymbolAsync(
            string symbol,
            CancellationToken ct = default)
        {
            return await Db.Queryable<GridPosition>()
                .FirstAsync(x => x.Symbol == symbol &&
                                 x.Status == PositionStatusType.Open);
        }

        public async Task<IReadOnlyList<GridPosition>> GetOpenPositionsAsync(
            CancellationToken ct = default)
        {
            return await Db.Queryable<GridPosition>()
                .Where(x => x.Status == PositionStatusType.Open)
                .OrderBy(x => x.CreateTime, OrderByType.Desc)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<GridPosition>> GetHistoryByStrategyAsync(
            string strategyId,
            CancellationToken ct = default)
        {
            return await Db.Queryable<GridPosition>()
                .Where(x => x.StrategyId == strategyId)
                .OrderBy(x => x.CreateTime, OrderByType.Desc)
                .ToListAsync();
        }
    }
}
