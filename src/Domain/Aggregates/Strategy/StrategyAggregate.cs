using System;
using System.Collections.Generic;
using System.Text;

namespace Oc.BinGrid.Domain.Aggregates.Strategy
{
    public class StrategyAggregate
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }

        private readonly List<StrategySignal> _signals = new();
        public IReadOnlyCollection<StrategySignal> Signals => _signals;

        protected StrategyAggregate() { }

        public StrategyAggregate(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
        }

        public void AddSignal(StrategySignal signal)
        {
            _signals.Add(signal);
        }
    }
}
