namespace Oc.BinGrid.Domain.Enums
{
    /// <summary>
    /// 订单业务行为：开多、平多、开空、平空
    /// </summary>
    public enum OrderAction
    {
        OpenLong,    // 现货买入 / 合约做多开仓
        CloseLong,   // 现货卖出 / 合约多单平仓
        OpenShort,   // 合约空单开仓 (现货不支持)
        CloseShort   // 合约空单平仓 (现货不支持)
    }
}
