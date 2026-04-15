namespace Oc.BinGrid.Domain.Entities
{
    /// <summary>
    /// 仓位实体
    /// </summary>
    public class Position
    {
        public int Id { get; set; }              // 数据库自增 ID
        public string Symbol { get; set; }
        public decimal EntryPrice { get; set; }  // 入场均价
        public decimal Quantity { get; set; }    // 持仓数量（正数为多，负数为空）
        public decimal UnrealizedPnL { get; set; } // 未实现盈亏

        // 领域逻辑：计算风险率
        public decimal GetRiskRatio(decimal currentPrice)
        {
            if (EntryPrice == 0) return 0;
            return Math.Abs(currentPrice - EntryPrice) / EntryPrice;
        }
    }
}
