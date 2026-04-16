namespace Oc.BinGrid.Domain.ValueObjects
{
    /// <summary>
    /// 交易所订单标准化响应（不可变值对象）
    /// </summary>
    public record OrderResponse(
        string OrderId,
        string Symbol,
        string Status,             // NEW, FILLED, CANCELED, etc.
        string Side,               // BUY, SELL
        decimal Price,             // 委托价格
        decimal Quantity,          // 委托数量
        decimal ExecutedQty,       // 累计成交数量
        decimal ExecutedPrice,     // 累计成交均价（重要：网格平仓依据）
        DateTime UpdateTime,       // 交易所最后更新时间
        string? ClientOrderId = null,
        string? OrderType = "LIMIT",
        decimal Fee = 0,           // 手续费
        string? FeeAsset = null,   // 手续费币种
        string? RejectReason = null
    )
    {
        /// <summary>
        /// 本次更新的成交金额（不计手续费）
        /// </summary>
        public decimal TotalAmount => ExecutedPrice * ExecutedQty;

        /// <summary>
        /// 订单是否已完全成交
        /// </summary>
        public bool IsFilled => Status == "FILLED";

        /// <summary>
        /// 订单是否已进入终态（不可再变更）
        /// </summary>
        public bool IsFinalized => Status is "FILLED" or "CANCELED" or "REJECTED" or "EXPIRED";

        /// <summary>
        /// 计算相对于委托价的滑点比例
        /// </summary>
        public decimal Slippage => Price > 0 ? Math.Abs(ExecutedPrice - Price) / Price : 0;
    }
}
