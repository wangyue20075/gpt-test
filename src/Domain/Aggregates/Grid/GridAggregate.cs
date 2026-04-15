using Oc.BinGrid.Domain.Enums;

namespace Oc.BinGrid.Domain.Aggregates.Grid
{
    public class GridAggregate
    {
        public Guid Id { get; private set; }
        public string Symbol { get; private set; }
        public GridMode Mode { get; private set; }
        public MarketType MarketType { get; private set; }
        public MarginType MarginType { get; private set; }

        public decimal UpperPrice { get; private set; }
        public decimal LowerPrice { get; private set; }
        public int GridCount { get; private set; }

        public bool IsRunning { get; private set; }

        private readonly List<GridOrder> _orders = new();
        public IReadOnlyCollection<GridOrder> Orders => _orders;

        private readonly List<GridLevel> _levels = new();
        public IReadOnlyCollection<GridLevel> Levels => _levels;

        protected GridAggregate() { }

        public GridAggregate(
            string symbol,
            MarketType marketType,
            decimal upper,
            decimal lower,
            int gridCount)
        {
            Id = Guid.NewGuid();
            Symbol = symbol;
            MarketType = marketType;
            UpperPrice = upper;
            LowerPrice = lower;
            GridCount = gridCount;

            BuildGridLevels();
        }

        private void BuildGridLevels()
        {
            var step = (UpperPrice - LowerPrice) / GridCount;

            for (int i = 0; i <= GridCount; i++)
            {
                _levels.Add(new GridLevel(
                    LowerPrice + step * i,
                    i
                ));
            }
        }

        public void Start()
        {
            if (IsRunning)
                throw new InvalidOperationException("Grid already running");

            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }
    }
}
