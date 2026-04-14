namespace Oc.BinGrid.Domain.Enums
{
    /// <summary>
    /// 仓位状态枚举
    /// </summary>
    public enum PositionStatusType
    {
        /// <summary> 
        /// 开仓中（挂单） 
        /// </summary>
        Opening,
        /// <summary> 
        /// 持仓中 
        /// </summary>
        Open,
        /// <summary>
        /// 平仓中（挂单） 
        /// </summary>
        Closing,
        /// <summary> 
        /// 已平仓 
        /// </summary>
        Closed,
    }
}
