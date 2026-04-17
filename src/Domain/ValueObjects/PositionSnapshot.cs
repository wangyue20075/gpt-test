namespace Oc.BinGrid.Domain.ValueObjects
{
    public record PositionSnapshot
    {
        public string PositionId { get; init; }
        public string StrategyId { get; init; }
        public string Symbol { get; init; }
        public string Side { get; init; }
        public decimal EntryPrice { get; init; }
        public decimal Quantity { get; init; }
        public decimal CurrentPrice { get; init; }
        public decimal ProfitLoss { get; init; }
        public decimal ProfitLossRate { get; init; }
        public DateTime OpenedTime { get; init; }
    }
}
