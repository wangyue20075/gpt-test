using Oc.BinGrid.Domain.Entities;

namespace Oc.BinGrid.Domain.Interfaces
{
    /// <summary>
    /// 仓位仓储接口
    /// </summary>
    public interface IPositionRepository
    {
        Task<Position> GetBySymbolAsync(string symbol);
        Task<List<Position>> GetAllActiveAsync();
        Task UpdateAsync(Position position);
    }
}
