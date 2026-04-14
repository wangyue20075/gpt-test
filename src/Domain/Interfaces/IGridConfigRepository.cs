using Oc.BinGrid.Domain.Entities;

namespace Oc.BinGrid.Domain.Interfaces
{
    /// <summary>
    /// 策略仓储：保存 GridConfig，确保重启后网格能接力运行
    /// </summary>
    public interface IGridConfigRepository
    {
        Task SaveConfigAsync(GridConfig config);
        Task<GridConfig?> GetConfigAsync(string strategyName);

        Task<List<GridConfig>> GetAllActiveConfigsAsync();

        Task<List<GridConfig>> GetConfigsByTypeAsync(string strategyType);
    }
}
