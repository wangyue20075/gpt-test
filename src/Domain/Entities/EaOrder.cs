using Oc.BinGrid.Domain.Enums;

namespace Oc.BinGrid.Domain.Entities;

// <summary>
/// EA 订单聚合根（Spot / Futures 通用）
/// </summary>
public class EaOrder
{
    protected EaOrder() { }

    public EaOrder(
        string strategyName,
        string symbol,
        MarketType market,
        OrderAction action, // 改用业务意图
        decimal orderPrice,
        decimal orderQty,
        OrderType orderType,
        string? parentOrderId = null
    )
    {
        StrategyName = strategyName;
        Symbol = symbol;
        Market = market;
        Action = action;
        OrderPrice = orderPrice;
        OrderQty = orderQty;
        OrderType = orderType;
        ParentOrderId = parentOrderId;

        // 自动映射交易所底层方向
        ResolveMarketSide(market, action);

        Status = OrderState.New;
        CreationTime = DateTime.UtcNow;
        UpdateTime = CreationTime;
    }

    #region ===== 归属 =====

    /// <summary>策略名称</summary>
    public string StrategyName { get; private set; } = string.Empty;

    /// <summary>交易对</summary>
    public string Symbol { get; private set; } = string.Empty;

    /// <summary>市场类型 Spot / Futures</summary>
    public MarketType Market { get; private set; }

    /// <summary> 开多/平多/开空/平空 </summary>
    public OrderAction Action { get; private set; }

    /// <summary> 交易所物理方向：BUY / SELL </summary>
    public OrderSide Side { get; private set; }

    /// <summary> 是否为平仓单（合约平仓需要 ReduceOnly 标记） </summary>
    public bool IsReduceOnly { get; private set; }

    /// <summary>关联仓位ID</summary>
    public long? PositionId { get; private set; }

    /// <summary>父订单ID（网格 / 拆单 / 追单）</summary>
    public string? ParentOrderId { get; private set; }

    #endregion

    #region ===== 委托参数 =====

    /// <summary>LIMIT / MARKET</summary>
    public OrderType OrderType { get; private set; }

    /// <summary>委托价格</summary>
    public decimal OrderPrice { get; private set; }

    /// <summary>委托数量</summary>
    public decimal OrderQty { get; private set; }

    /// <summary>委托金额</summary>
    public decimal Notional => OrderPrice * OrderQty;

    #endregion

    #region ===== 成交数据 =====

    /// <summary>成交均价</summary>
    public decimal ExecPrice { get; private set; }

    /// <summary>成交数量</summary>
    public decimal ExecQty { get; private set; }

    /// <summary>累计手续费</summary>
    public decimal Fee { get; private set; }

    /// <summary>手续费币种</summary>
    public string FeeAsset { get; private set; } = string.Empty;

    /// <summary>是否 Maker 成交</summary>
    public bool IsMaker { get; private set; }

    /// <summary>交易所订单ID</summary>
    public long ExchangeOrderId { get; private set; }

    public OrderState Status { get; private set; }

    public DateTime CreationTime { get; private set; }

    public DateTime UpdateTime { get; private set; }

    public DateTime? FilledTime { get; private set; }

    public string CancelReason { get; private set; } = string.Empty;

    public string RejectReason { get; private set; } = string.Empty;

    #endregion

    #region ===== DDD 行为方法 =====

    /// <summary>
    /// 将业务意图翻译为交易所可理解的物理参数
    /// </summary>
    private void ResolveMarketSide(MarketType market, OrderAction action)
    {
        if (market == MarketType.Spot)
        {
            // 现货逻辑：开多=买，平多=卖，不支持空头
            Side = (action == OrderAction.OpenLong || action == OrderAction.CloseShort)
                   ? OrderSide.Buy : OrderSide.Sell;
            IsReduceOnly = false;

            if (action == OrderAction.OpenShort || action == OrderAction.CloseShort)
                throw new Exception("Spot market does not support Short actions directly.");
        }
        else
        {
            // 合约逻辑 (假设使用单向持仓模式 One-way Mode，这是最通用的)
            // 开多: BUY, 平多: SELL (ReduceOnly)
            // 开空: SELL, 平空: BUY (ReduceOnly)
            switch (action)
            {
                case OrderAction.OpenLong:
                    Side = OrderSide.Buy;
                    IsReduceOnly = false;
                    break;
                case OrderAction.CloseLong:
                    Side = OrderSide.Sell;
                    IsReduceOnly = true;
                    break;
                case OrderAction.OpenShort:
                    Side = OrderSide.Sell;
                    IsReduceOnly = false;
                    break;
                case OrderAction.CloseShort:
                    Side = OrderSide.Buy;
                    IsReduceOnly = true;
                    break;
            }
        }
    }

    /// <summary>订单已发送到交易所</summary>
    public void MarkSubmitted(long exchangeOrderId)
    {
        ExchangeOrderId = exchangeOrderId;
        Status = OrderState.Submitted;
        UpdateTime = DateTime.UtcNow;
    }

    public void SetPositionId(long positionId)
    {
        PositionId = positionId;
    }

    /// <summary>成交更新（支持部分成交）</summary>
    public void ApplyFill(decimal fillQty, decimal fillPrice, decimal fee, string feeAsset, bool isMaker)
    {
        var totalCost = (ExecPrice * ExecQty) + (fillPrice * fillQty);

        ExecQty += fillQty;

        ExecPrice = ExecQty == 0
            ? fillPrice
            : totalCost / ExecQty;

        Fee += fee;
        FeeAsset = feeAsset;
        IsMaker = isMaker;

        Status = ExecQty >= OrderQty
            ? OrderState.Filled
            : OrderState.PartiallyFilled;

        if (Status == OrderState.Filled)
            FilledTime = DateTime.UtcNow;

        UpdateTime = DateTime.UtcNow;
    }

    /// <summary>撤单</summary>
    public void Cancel(string reason = "")
    {
        Status = OrderState.Canceled;
        CancelReason = reason;
        UpdateTime = DateTime.UtcNow;
    }

    /// <summary>拒单 / 失败</summary>
    public void Reject(string reason)
    {
        Status = OrderState.Rejected;
        RejectReason = reason;
        UpdateTime = DateTime.UtcNow;
    }

    #endregion

    public override string ToString()
    {
        return $"[{StrategyName}] {Market}-{Action} {Symbol} @{OrderPrice} Qty:{OrderQty} | Status:{Status}";
    }
}
