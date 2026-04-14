namespace Oc.BinGrid.Domain.Enums
{
    /// <summary>
    /// 订单状态枚举
    /// </summary>
    public enum OrderState
    {
        /// <summary>
        /// 新建（未发单），本地创建，还没发给交易所
        /// </summary>
        New,
        /// <summary>
        /// 委托中，已挂单，等待成交
        /// </summary>
        Submitted,
        /// <summary>
        /// 部分成交
        /// </summary>
        PartiallyFilled,
        /// <summary>
        /// 完全成交
        /// </summary>
        Filled,
        /// <summary>
        /// 已撤单，用户或系统撤单
        /// </summary>
        Canceled,
        /// <summary>
        /// 已拒单，参数错误 / 余额不足
        /// </summary>
        Rejected,
        /// <summary>
        /// 已过期，IOC/FOK 到期
        /// </summary>
        Expired
    }
}
