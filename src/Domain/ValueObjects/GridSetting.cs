namespace Oc.BinGrid.Domain.ValueObjects
{
    /// <summary>
    /// 网格策略配置参数
    /// </summary>
    /// <param name="Symbol">交易对名称 (如 BTCUSDT)</param>
    /// <param name="InitialPrice">初始运行基准价 (启动时的参考价)</param>
    /// <param name="GridGap">网格间距 (绝对金额，如 500 表示每隔 500 刀一格)</param>
    /// <param name="QuantityPerGrid">单格下单数量</param>
    /// <param name="MaxGrids">最大允许持仓格数 (防止单边行情无限补仓)</param>
    /// <param name="TakeProfit">止盈门槛比例 (如 0.02 表示盈利 2% 开启追踪)</param>
    /// <param name="CallbackRate">止盈回撤比例 (如 0.005 表示从高点回落 0.5% 平仓)</param>
    /// <param name="ReboundRate">买入反弹比例 (如 0.002 表示跌破网格线后反弹 0.2% 才买入)</param>
    public record GridSetting(
        string Symbol,
        decimal InitialPrice,
        decimal GridGap,
        decimal QuantityPerGrid,
        int MaxGrids,
        decimal TakeProfit = 0.02m,
        decimal CallbackRate = 0.005m,
        decimal ReboundRate = 0.002m
    )
    {
        // 可以在此处添加基础校验逻辑
        public void Validate()
        {
            if (GridGap <= 0) throw new ArgumentException("网格间距必须大于 0");
            if (MaxGrids <= 0) throw new ArgumentException("最大格数必须大于 0");
            if (QuantityPerGrid <= 0) throw new ArgumentException("单仓数量必须大于 0");
        }
    }
}
