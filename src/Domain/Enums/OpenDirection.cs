namespace Oc.BinGrid.Domain.Enums
{
    /// <summary>
    /// 开仓方向枚举
    /// </summary>
    [Flags]
    public enum OpenDirection
    {
        /// <summary>
        /// 只做多
        /// </summary>
        Buy = 1,
        /// <summary>
        /// 只做空
        /// </summary>
        Sell = 2,
        /// <summary>
        /// 双向
        /// </summary>
        Both = 3
    }
}
