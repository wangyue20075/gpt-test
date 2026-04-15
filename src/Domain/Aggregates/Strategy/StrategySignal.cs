using Oc.BinGrid.Domain.Enums;

namespace Oc.BinGrid.Domain.Aggregates.Strategy
{
    public class StrategySignal
    {
        public string Symbol { get; private set; }
        public SignalType SignalType { get; private set; }
        public decimal Strength { get; private set; }
        public DateTime Time { get; private set; }

        protected StrategySignal() { }

        public StrategySignal(string symbol, SignalType type, decimal strength)
        {
            Symbol = symbol;
            SignalType = type;
            Strength = strength;
            Time = DateTime.UtcNow;
        }
    }
}
