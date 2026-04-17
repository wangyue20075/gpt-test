using Oc.BinGrid.Domain.Interfaces;

namespace Oc.BinGrid.Domain.ValueObjects
{
    /// <summary>
    /// 订单监控任务上下文：定义了订单何时应该被撤销以及如何通知策略
    /// </summary>
    public class OrderWatchTask
    {
        /// <summary>
        /// 交易所返回的订单唯一标识
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// 交易对名称 (如 BTCUSDT)
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// 挂单方向 (BUY/SELL)
        /// </summary>
        public string Side { get; set; }

        /// <summary>
        /// 下单时的限价
        /// </summary>
        public decimal OrderPrice { get; set; }

        /// <summary>
        /// 下单时的数量
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// 任务创建时间（通常为下单时间）
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 超时阈值：超过此时间未成交则尝试撤单
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// 最大允许价格偏离度 (例如 0.01 表示 1%)
        /// 当前价与挂单价偏离超过此值时执行撤单
        /// </summary>
        public decimal MaxDeviation { get; set; } = 0.01m;

        /// <summary>
        /// 💡 核心引用：所属的策略实例
        /// 当监控到成交或撤销时，通过此引用回调策略的 OnOrderUpdateAsync
        /// </summary>
        public IStrategy OwnerStrategy { get; set; }

        /// <summary>
        /// 可选：附加元数据（如网格索引等，方便调试）
        /// </summary>
        public string Note { get; set; }
    }
}
