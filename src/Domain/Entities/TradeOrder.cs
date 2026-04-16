using Oc.BinGrid.Domain.Enums;

namespace Oc.BinGrid.Domain.Entities
{
    /// <summary>
    /// 交易订单实体：承载订单的完整生命周期与对账元数据
    /// </summary>
    public class TradeOrder
    {
        /// <summary>
        /// 系统内部主键 (雪花ID或GUID)
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString()[..8];

        /// <summary>
        /// 关联的网格仓位ID (GridPosition.Id)
        /// 用于建立订单与具体哪一格持仓的绑定关系
        /// </summary>
        public string PositionId { get; set; }

        /// <summary>
        /// 策略引用：确定该订单属于哪个策略实例
        /// </summary>
        public string StrategyId { get; set; }
        /// <summary>
        /// 策略名称
        /// </summary>
        public string StrategyName { get; set; }

        /// <summary>
        /// 交易所返回的真实 OrderId
        /// </summary>
        public string ExchangeOrderId { get; set; }

        /// <summary>
        /// 客户端自定义 ID (防止重复下单，支持撤单定位)
        /// </summary>
        public string ClientOrderId { get; set; }

        /// <summary>
        /// 交易对，如 BTCUSDT
        /// </summary>
        public string Symbol { get; set; }
        /// <summary>
        /// BUY / SELL
        /// </summary>
        public string Side { get; set; }
        /// <summary>
        /// LIMIT / MARKET
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// 时间有效性 (GTC, IOC, FOK)
        /// </summary>
        public string TimeInForce { get; set; }
        /// <summary>
        /// 委托价格
        /// </summary>
        public decimal Price { get; set; }
        /// <summary>
        /// 委托数量
        /// </summary>
        public decimal Qty { get; set; }
        /// <summary>
        /// 成交均价
        /// </summary>
        public decimal ExecPrice { get; set; }
        /// <summary>
        /// 已成交数量
        /// </summary>
        public decimal ExecQty { get; set; }
        /// <summary>
        /// 成交时间
        /// </summary>
        public DateTime? ExecTime { get; set; }
        /// <summary>
        /// 累计手续费
        /// </summary>
        public decimal Fee { get; set; }
        /// <summary>
        /// 核心状态：NEW, PARTIALLY_FILLED, FILLED, CANCELED, REJECTED, EXPIRED
        /// </summary>
        public OrderState Status { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// 撤销原因 (如: Timeout, Deviation, Manual)
        /// </summary>
        public string CancelReason { get; set; }

        /// <summary>
        /// 拒绝原因 (如: 余额不足, 价格偏离过大)
        /// </summary>
        public string RejectReason { get; set; }

        #region 业务辅助方法

        /// <summary>
        /// 是否已完全成交
        /// </summary>
        public bool IsFilled() => Status == OrderState.Filled;

        ///// <summary>
        ///// 是否已终结 (不再会发生变动)
        ///// </summary>
        //public bool IsFinalized() =>
        //    Status is "FILLED" or "CANCELED" or "REJECTED" or "EXPIRED";

        /// <summary>
        /// 计算成交总额 (不计手续费)
        /// </summary>
        public decimal TotalAmount => ExecPrice * ExecQty;

        #endregion
    }
}
