using Oc.BinGrid.Domain.Enums;

namespace Oc.BinGrid.Domain.Entities
{
    public class TradeLog
    {
        public TradeLog() { }

        public TradeLog(
            string strategy,
            string symbol,
            MarketType market,
            OrderSide side,
            decimal price,
            decimal qty,
            decimal fee,
            string feeAsset,
            bool isMaker,
            string orderId,
            string? positionId)
        {
            StrategyName = strategy;
            Symbol = symbol;
            Market = market;
            Side = side;

            Price = price;
            Qty = qty;

            Fee = fee;
            FeeAsset = feeAsset;
            IsMaker = isMaker;

            OrderId = orderId;
            PositionId = positionId;

            TradeTime = DateTime.UtcNow;
        }

        #region ===== 归属 =====

        public string StrategyName { get; private set; } = string.Empty;
        public string Symbol { get; private set; } = string.Empty;
        public MarketType Market { get; private set; }
        public OrderSide Side { get; private set; }

        #endregion

        #region ===== 成交信息 =====

        public decimal Price { get; private set; }
        public decimal Qty { get; private set; }
        public decimal Notional => Price * Qty;

        public decimal Fee { get; private set; }
        public string FeeAsset { get; private set; } = string.Empty;

        public bool IsMaker { get; private set; }

        #endregion

        #region ===== 关联关系 =====

        public string OrderId { get; private set; } = string.Empty;
        public string? PositionId { get; private set; }

        #endregion

        #region ===== PnL =====

        public decimal RealizedPnl { get; private set; }
        public decimal FeeInQuote { get; private set; }

        #endregion

        public DateTime TradeTime { get; private set; }
    }
}
