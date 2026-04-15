using Oc.BinGrid.Domain.Enums;
using Oc.BinGrid.Domain.Values;
using System;
using System.Collections.Generic;
using System.Text;

namespace Oc.BinGrid.Domain.Aggregates.Strategy
{
    public class IndicatorContext
    {
        public string Symbol { get; }
        public MarketType MarketType { get; }
        public IReadOnlyList<TickData> Ticks { get; }

        public IndicatorContext(
            string symbol,
            MarketType marketType,
            IReadOnlyList<TickData> ticks)
        {
            Symbol = symbol;
            MarketType = marketType;
            Ticks = ticks;
        }
    }
}
