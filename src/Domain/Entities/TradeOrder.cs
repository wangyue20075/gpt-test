namespace Oc.BinGrid.Domain.Entities
{
    /// <summary>
    /// 交易订单实体
    /// </summary>
    public class TradeOrder
    {
        public string OrderId { get; set; }          // 对应交易所 OrderId
        public string StrategyId { get; set; }       // 关联的策略 ID
        public string Symbol { get; set; }       // 交易对，如 BTCUSDT
        public decimal Price { get; set; }       // 委托价格
        public decimal Quantity { get; set; }    // 委托数量
        public string Side { get; set; }         // BUY / SELL
        public string Status { get; set; }       // NEW / FILLED / CANCELED
        public DateTime CreateTime { get; set; }
    }
}
