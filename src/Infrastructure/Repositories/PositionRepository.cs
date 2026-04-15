using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.Interfaces;
using Oc.BinGrid.Infrastructure.Db;
using SqlSugar;

namespace Oc.BinGrid.Infrastructure.Repositories
{
    public class PositionRepository : IPositionRepository
    {
        private readonly ISqlSugarClient _db;

        public PositionRepository(SqlSugarContext context)
        {
            _db = context.Db;
        }

        public Task<List<Position>> GetAllActiveAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Position> GetBySymbolAsync(string symbol)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Position position)
        {
            throw new NotImplementedException();
        }
    }
}
