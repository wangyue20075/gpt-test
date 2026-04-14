namespace Oc.BinGrid.Domain.Entities
{
    /// <summary>
    /// 余额快照
    /// </summary>
    public class BalanceSnapshot
    {
        /// <summary>
        /// 现货
        /// </summary>
        public decimal Spot { get; set; }
        /// <summary>
        /// 合约
        /// </summary>
        public decimal Futures { get; set; }
        /// <summary>
        /// 总余额
        /// </summary>
        public decimal Total { get; set; }
        /// <summary>
        /// 快照时间
        /// </summary>
        public DateTime SnapshotTime { get; set; }


        public override string ToString()
        {
            return $"Balance Snapshot | Spot={Spot:F2} | Futures={Futures:F2} | Total={Total:F2}";
        }
    }
}
