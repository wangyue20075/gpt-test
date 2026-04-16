using Oc.BinGrid.Domain.Enums;

namespace Oc.BinGrid.Domain.Entities
{
    /// <summary>
    /// 网格仓位实体：记录每一格买入到卖出的完整生命周期
    /// </summary>
    public class GridPosition
    {
        /// <summary>
        /// 唯一标识 (可以使用 GUID 或 雪花ID)
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString()[..8];

        /// <summary>
        /// 所属策略 ID (用于区分多策略实例)
        /// </summary>
        public string StrategyId { get; set; }

        /// <summary>
        /// 交易对 (如 BTCUSDT)
        /// </summary>
        public string Symbol { get; set; }

        public PositionStatusType Status { get; set; } = PositionStatusType.Opening;

        /// <summary>
        /// 仓位方向 (通常网格为 LONG)
        /// </summary>
        public string Side { get; set; } = "LONG";

        // --- 入场信息 ---

        /// <summary>
        /// 开仓价 (实际成交价)
        /// </summary>
        public decimal EntryPrice { get; set; }

        /// <summary>
        /// 持仓数量
        /// </summary>
        public decimal Qty { get; set; }

        /// <summary>
        /// 对应的开仓订单 ID
        /// </summary>
        public long EntryOrderId { get; set; }

        /// <summary>
        /// 开仓时间
        /// </summary>
        public DateTime EntryTime { get; set; }

        // --- 出场信息 ---

        /// <summary>
        /// 平仓价
        /// </summary>
        public decimal? ExitPrice { get; set; }

        /// <summary>
        /// 对应的平仓订单 ID
        /// </summary>
        public long? ExitOrderId { get; set; }

        /// <summary>
        /// 平仓时间
        /// </summary>
        public DateTime? ExitTime { get; set; }

        /// <summary>
        /// 是否已平仓
        /// </summary>
        public bool IsClosed { get; set; }

        // --- 财务统计 ---

        /// <summary>
        /// 累计支付的手续费 (买入+卖出)
        /// </summary>
        public decimal TotalFee { get; set; }

        /// <summary>
        /// 净盈亏 (扣除手续费后)
        /// </summary>
        public decimal NetPnL { get; set; }

        /// <summary>
        /// 收益率 (ExitPrice - EntryPrice) / EntryPrice
        /// </summary>
        public decimal Roi { get; set; }

        public DateTime CreateTime { get; set; }

        // --- 业务辅助 ---

        /// <summary>
        /// 更新平仓信息并计算盈亏
        /// </summary>
        public void Close(decimal exitPrice, long exitOrderId, decimal fee = 0)
        {
            ExitPrice = exitPrice;
            ExitOrderId = exitOrderId;
            ExitTime = DateTime.UtcNow;
            IsClosed = true;

            // 简单盈亏计算：(平仓价 - 开仓价) * 数量 - 手续费
            NetPnL = ((exitPrice - EntryPrice) * Qty) - (TotalFee + fee);
            TotalFee += fee;
            Roi = (exitPrice - EntryPrice) / EntryPrice;
        }
    }
}
