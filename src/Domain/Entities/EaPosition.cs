using Oc.BinGrid.Domain.Enums;

namespace Oc.BinGrid.Domain.Entities
{
    /// <summary>
    /// EA 策略逻辑仓位聚合根（支持 Spot / Futures）
    /// 职责：
    /// - 聚合单策略单品种仓位生命周期
    /// - 管理仓位数量 / 成本 / 盈亏 / 风控
    /// - 支持期货保证金与强平逻辑
    /// 
    /// 设计原则：
    /// - 所有仓位状态变更必须通过行为方法
    /// - 不允许外部直接修改核心字段（保持一致性）
    /// </summary>
    public class EaPosition
    {
        protected EaPosition() { }

        /// <summary>
        /// 构造：创建一个策略仓位实例
        /// </summary>
        public EaPosition(
            string strategyName,
            string symbol,
            MarketType market,
            PositionSide side)
        {
            StrategyName = strategyName;
            Symbol = symbol;
            Market = market;
            Side = side;

            Status = PositionStatusType.Opening;
            OpenTime = DateTime.UtcNow;
            UpdateTime = DateTime.UtcNow;
        }

        // ============================================================
        // ======================= 基础标识 ===========================
        // ============================================================

        /// <summary>
        /// 策略名称
        /// </summary>
        public string StrategyName { get; private set; } = string.Empty;

        /// <summary>
        /// 交易标的（BTCUSDT / ETHUSDT）
        /// </summary>
        public string Symbol { get; private set; } = string.Empty;

        /// <summary>
        /// 市场类型：现货 Spot / 合约 Futures
        /// </summary>
        public MarketType Market { get; private set; }

        /// <summary>
        /// 持仓方向：多头 Long / 空头 Short
        /// </summary>
        public PositionSide Side { get; private set; }

        /// <summary>
        /// 当前仓位状态（Opening / Open / Closed）
        /// </summary>
        public PositionStatusType Status { get; private set; }

        // ============================================================
        // =================== 仓位数量 & 成本 ========================
        // ============================================================

        /// <summary>
        /// 当前持仓数量（合约 = 张数 / 现货 = 币数量）
        /// </summary>
        public decimal Qty { get; private set; }

        /// <summary>
        /// 加权平均开仓成本价
        /// </summary>
        public decimal EntryPrice { get; private set; }

        /// <summary>
        /// 最新标记价格（行情推送）
        /// </summary>
        public decimal MarkPrice { get; private set; }

        /// <summary>
        /// 已实现盈亏（平仓后锁定）
        /// </summary>
        public decimal RealizedPnl { get; private set; }

        /// <summary>
        /// 未实现盈亏（浮动盈亏）
        /// </summary>
        public decimal UnrealizedPnl { get; private set; }

        /// <summary>
        /// 累计手续费（开仓 + 平仓 + 资金费率）
        /// </summary>
        public decimal Fee { get; private set; }


        // ============================================================
        // =================== 期货专属参数 ===========================
        // ============================================================

        /// <summary>
        /// 杠杆倍数（默认 1 = 现货模式）
        /// </summary>
        public int Leverage { get; private set; } = 1;

        /// <summary>
        /// 初始保证金（开仓占用资金）
        /// </summary>
        public decimal InitialMargin { get; private set; }

        /// <summary>
        /// 维持保证金（爆仓维持线）
        /// </summary>
        public decimal MaintenanceMargin { get; private set; }

        /// <summary>
        /// 强平价格（爆仓价）
        /// </summary>
        public decimal LiquidationPrice { get; private set; }

        /// <summary>
        /// 保证金模式：Cross（全仓） / Isolated（逐仓）
        /// </summary>
        public MarginType MarginMode { get; private set; } = MarginType.Cross;


        // ============================================================
        // =================== 风控 & 移动止盈止损 ====================
        // ============================================================

        /// <summary>
        /// 固定止损价
        /// </summary>
        public decimal StopLossPrice { get; private set; }

        /// <summary>
        /// 固定止盈价
        /// </summary>
        public decimal TakeProfitPrice { get; private set; }

        /// <summary>
        /// 持仓期间最高价格（多头追踪）
        /// </summary>
        public decimal HighestPrice { get; private set; }

        /// <summary>
        /// 持仓期间最低价格（空头追踪）
        /// </summary>
        public decimal LowestPrice { get; private set; }

        /// <summary>
        /// 追踪止盈层级（用于移动止盈策略）
        /// </summary>
        public int TrailLevel { get; private set; }


        // ============================================================
        // =================== 平仓与生命周期 =========================
        // ============================================================

        /// <summary>
        /// 是否已完全平仓
        /// </summary>
        public bool IsClosed { get; private set; }

        /// <summary>
        /// 平仓价格（最终成交价）
        /// </summary>
        public decimal ClosePrice { get; private set; }

        /// <summary>
        /// 平仓时间
        /// </summary>
        public DateTime? CloseTime { get; private set; }


        // ============================================================
        // ======================== 时间字段 ==========================
        // ============================================================

        /// <summary>
        /// 仓位创建时间
        /// </summary>
        public DateTime OpenTime { get; private set; }

        /// <summary>
        /// 仓位最后更新时间
        /// </summary>
        public DateTime UpdateTime { get; private set; }


        // ============================================================
        // ======================= 核心行为方法 =======================
        // ============================================================

        /// <summary>
        /// 增加仓位（首次开仓 / 加仓）
        /// 自动更新加权成本价与仓位状态
        /// </summary>
        public void AddPosition(decimal qty, decimal price, decimal fee = 0)
        {
            if (qty <= 0)
                throw new ArgumentException("qty must > 0");

            var newQty = Qty + qty;

            // 计算新的加权平均成本价
            EntryPrice = newQty == 0
                ? 0
                : (EntryPrice * Qty + price * qty) / newQty;

            Qty = newQty;
            Fee += fee;

            Status = PositionStatusType.Open;
            UpdateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 减仓 / 部分平仓 / 完全平仓
        /// 结算已实现盈亏
        /// </summary>
        public void ReducePosition(decimal qty, decimal price, decimal fee = 0)
        {
            if (qty <= 0 || qty > Qty)
                throw new InvalidOperationException("Invalid reduce qty");

            var pnl = CalculatePnl(price, qty);

            RealizedPnl += pnl;
            Fee += fee;

            Qty -= qty;

            // 如果仓位归零 → 关闭仓位
            if (Qty == 0)
                Close(price);

            UpdateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 刷新行情价格并更新浮动盈亏
        /// 同时更新价格极值（用于追踪止盈）
        /// </summary>
        public void UpdateMarkPrice(decimal price)
        {
            MarkPrice = price;
            UnrealizedPnl = CalculatePnl(price, Qty);

            // 记录价格极值
            HighestPrice = Math.Max(HighestPrice, price);
            LowestPrice = LowestPrice == 0 ? price : Math.Min(LowestPrice, price);

            UpdateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 盈亏计算（支持多空方向）
        /// </summary>
        private decimal CalculatePnl(decimal price, decimal qty)
        {
            var diff = Side == PositionSide.Long
                ? price - EntryPrice
                : EntryPrice - price;

            return diff * qty;
        }

        /// <summary>
        /// 期货风控：更新保证金 / 强平价
        /// </summary>
        public void UpdateFuturesRisk(decimal walletBalance)
        {
            if (Market != MarketType.Futures || Qty == 0)
                return;

            // 初始保证金 = 名义价值 / 杠杆
            InitialMargin = (Qty * EntryPrice) / Leverage;

            // 维持保证金（可按交易所规则微调）
            MaintenanceMargin = InitialMargin * 0.5m;

            // 强平价计算（简化公式）
            LiquidationPrice = Side == PositionSide.Long
                ? EntryPrice - (walletBalance / Qty)
                : EntryPrice + (walletBalance / Qty);
        }

        /// <summary>
        /// 设置止损 / 止盈价格
        /// </summary>
        public void SetRiskPrices(decimal sl, decimal tp)
        {
            StopLossPrice = sl;
            TakeProfitPrice = tp;
            UpdateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 完全平仓（仓位生命周期结束）
        /// </summary>
        private void Close(decimal price)
        {
            ClosePrice = price;
            CloseTime = DateTime.UtcNow;
            IsClosed = true;
            Status = PositionStatusType.Closed;
        }

        /// <summary>
        /// 投资回报率 ROI（包含已实现 + 浮动盈亏）
        /// </summary>
        public decimal Roi =>
            EntryPrice == 0 || Qty == 0
                ? 0
                : (RealizedPnl + UnrealizedPnl) / (EntryPrice * Qty);

        /// <summary>
        /// 可读性日志输出（策略监控 / 控制台 / UI）
        /// </summary>
        public override string ToString()
        {
            return $"[{StrategyName}] {Symbol} {Side} Qty:{Qty} Entry:{EntryPrice:F4} Mark:{MarkPrice:F4} " +
                   $"PnL:{RealizedPnl + UnrealizedPnl:F4} ROI:{Roi:P2} Liq:{LiquidationPrice:F4} High:{HighestPrice:F4} Low:{LowestPrice:F4}";
        }
    }
}
