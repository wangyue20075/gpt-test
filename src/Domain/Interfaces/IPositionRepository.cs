using Oc.BinGrid.Domain.Entities;
using Oc.BinGrid.Domain.Enums;

namespace Oc.BinGrid.Domain.Interfaces
{
    /// <summary>
    /// 仓位仓储：管理 EaPosition，支持多币种、多策略持仓
    /// </summary>
    public interface IPositionRepository
    {
        Task UpdateAsync(EaPosition position);
        Task<EaPosition?> GetPositionAsync(string strategyName, string symbol, PositionSide side);
    }
}
