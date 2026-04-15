namespace Oc.BinGrid.Domain.Values
{
    //public readonly struct TickData
    //{
    //    public required string Symbol { get; init; }
    //    public required decimal Price { get; init; }
    //    public decimal Quantity { get; init; }
    //    public DateTime ServerTime { get; init; }
    //    public DateTime LocalTime { get; init; }

    //    // 可选：计算延迟
    //    public TimeSpan Latency => LocalTime - ServerTime;
    //}

    public readonly record struct TickData(
        string Symbol,
        decimal Price,
        decimal Volume,
        DateTime Time
    );
}
