using Oc.BinGrid.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Oc.BinGrid.Domain.Entities
{
    /// <summary>
    /// 网格交易参数配置与实时状态
    /// </summary>
    public class GridConfig 
    {
        /// <summary>
        /// 策略名称（便于管理）
        /// </summary>
        [Required]
        [MaxLength(64)]
        public string Name { get; set; } = "Default Grid Strategy";

        /// <summary>
        /// 交易标的（BTCUSDT）
        /// </summary>
        [Required]
        [MaxLength(16)]
        public string Symbol { get; set; }

        /// <summary>
        /// 市场类型：现货 Spot / 合约 Futures
        /// </summary>
        [Required]
        public MarketType Market { get; set; }

        /// <summary>
        /// 策略启用状态
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 基准价格（每次成交后更新）
        /// </summary>
        public decimal BasePrice { get; set; }

        /// <summary>
        /// 当前波段追踪到的局部最高价（用于计算回撤）
        /// </summary>
        public decimal TrackedHigh { get; set; }

        /// <summary>
        /// 当前波段追踪到的局部最低价（用于计算反弹）
        /// </summary>
        public decimal TrackedLow { get; set; }

        /// <summary>
        /// 交易方向控制
        /// LongOnly = 只做多
        /// ShortOnly = 只做空
        /// Both = 双向
        /// </summary>
        public OpenDirection Direction { get; set; } = OpenDirection.Both;

        /// <summary>
        /// 涨跌计算方式
        /// Percentage = 百分比模式
        /// PriceDiff = 固定价差模式
        /// </summary>
        public PriceChangeType ChangeType { get; set; } = PriceChangeType.Percentage;

        /// <summary>
        /// 单笔交易金额/数量
        /// </summary>
        public decimal TradeAmount { get; set; }

        /// <summary>
        /// 上涨触发阈值（触发卖出）
        /// Percentage 模式：0.02 = 2%
        /// PriceDiff 模式：200 = 上涨200 USDT
        /// </summary>
        public decimal UpThreshold { get; set; } = 0.02m;

        /// <summary>
        /// 上涨回落阈值（从高点回落触发卖出）
        /// Percentage 模式：0.005 = 0.5%
        /// </summary>
        public decimal? UpRetracement { get; set; } = 0.005m;

        /// <summary>
        /// 下跌触发阈值（触发买入）
        /// Percentage 模式：0.02 = 2%
        /// </summary>
        public decimal DownThreshold { get; set; } = 0.02m;

        /// <summary>
        /// 下跌反弹阈值（从低点反弹触发补仓）
        /// Percentage 模式：0.005 = 0.5%
        /// </summary>
        public decimal? DownRebound { get; set; } = 0.005m;

        /// <summary>
        /// 网格价格上限
        /// </summary>
        public decimal? UpperLimit { get; set; }

        /// <summary>
        /// 网格价格下限
        /// </summary>
        public decimal? LowerLimit { get; set; }

        /// <summary>
        /// 策略有效期（为空 = 永久）
        /// </summary>
        public DateTime? ExpireAt { get; set; }

        /// <summary>
        /// 状态最后更新时间
        /// </summary>
        public DateTime UpdateAt { get; set; }

        /// <summary>
        /// 策略创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 业务方法：成交后的一键状态重置
        /// </summary>
        public void UpdateStateAfterTrade(decimal executedPrice)
        {
            BasePrice = executedPrice;
            TrackedHigh = executedPrice;
            TrackedLow = executedPrice;
            UpdateAt = DateTime.UtcNow;
        }
    }
}
