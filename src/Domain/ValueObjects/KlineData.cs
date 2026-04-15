namespace Oc.BinGrid.Domain.ValueObjects
{
    // K线数据 (Value Object)
    public record KlineData(DateTime OpenTime, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume);
}
